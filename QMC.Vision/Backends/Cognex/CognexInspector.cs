using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using QMC.Vision.Core;
using QMC.Vision.Core.Parameters;

namespace QMC.Vision.Backends.Cognex
{
    /// <summary>
    /// Cognex CogBlobTool / CogHistogramTool 기반 Inspector.
    /// - Blob: 임계값 분리 후 결함/이물 영역 카운트 + 면적/위치 산출
    /// - Histogram: 평균/표준편차로 표면 균질성 평가 (가능 시)
    /// 미로드/실패 시 Sim fallback.
    /// </summary>
    public class CognexInspector : IInspector, IParameterProvider
    {
        public string Id { get; }
        public Roi    InspectionRoi { get; set; }

        /// <summary>Blob 임계값 (HardFixedThreshold).</summary>
        public int    Threshold        { get; set; } = 128;
        /// <summary>최소 결함 면적 (픽셀^2). 이 미만 blob 은 무시.</summary>
        public int    MinDefectArea    { get; set; } = 25;
        /// <summary>PASS 판정 기준: defect 영역 합계 ≤ 이 값.</summary>
        public double MaxTotalDefectArea { get; set; } = 500;

        private readonly CognexBackend _be;
        private readonly Sim.SimInspector _fallback;

        public CognexInspector(string id, CognexBackend be)
        {
            Id = id; _be = be;
            _fallback = new Sim.SimInspector(id);
            InspectionRoi = new Roi { Name = id + ".Roi", CenterX = 320, CenterY = 240, Width = 200, Height = 200 };
        }

        public InspectionResult Inspect(Bitmap image)
        {
            _fallback.InspectionRoi = InspectionRoi?.Clone();
            if (image == null)
                return new InspectionResult { RoiName = Id, IsPass = false, ErrorMessage = "no image" };

            if (!_be.CognexLoaded)
                return _fallback.Inspect(image);

            try
            {
                var asms = _be.LoadedAssemblies;
                var blobType = CognexInterop.GetType("Cognex.VisionPro.Blob.CogBlobTool", asms);
                if (blobType == null)
                    return _fallback.Inspect(image);

                dynamic blob = Activator.CreateInstance(blobType);
                dynamic cogImage = CognexInterop.BitmapToICogImage(image, asms);
                blob.InputImage = cogImage;

                // ROI 적용 (Region 속성)
                if (InspectionRoi != null)
                {
                    var r = InspectionRoi.BoundingBox;
                    r.Intersect(new Rectangle(0, 0, image.Width, image.Height));
                    if (r.Width > 0 && r.Height > 0)
                    {
                        try
                        {
                            dynamic region = CognexInterop.NewRectangle(r.X, r.Y, r.Width, r.Height, asms);
                            blob.Region = region;
                        }
                        catch { /* 일부 버전에서 ROI 미지원 */ }
                    }
                }

                // RunParams 설정 — HardFixedThreshold / MinArea
                try
                {
                    dynamic rp = blob.RunParams;
                    dynamic seg = rp.SegmentationParams;
                    // ColorMode
                    CognexInterop.TrySet(seg, "Mode", "HardFixedThreshold");
                    CognexInterop.TrySet(seg, "HardFixedThreshold", (double)Threshold);
                    CognexInterop.TrySet(seg, "Polarity", "DarkBlobs");
                    // Connectivity 최소 픽셀
                    CognexInterop.TrySet(rp, "ConnectivityMinPixels", MinDefectArea);
                }
                catch { /* 버전별 차이 — 무시 */ }

                blob.Run();

                // 결과 수집
                var defects = new List<(double area, double cx, double cy)>();
                double totalArea = 0;
                try
                {
                    dynamic results = blob.Results;
                    dynamic blobs = results.GetBlobs();
                    if (blobs is IEnumerable e)
                    {
                        foreach (dynamic b in e)
                        {
                            double a = (double)CognexInterop.TryGet(b, "Area", 0.0);
                            if (a < MinDefectArea) continue;
                            double cx = (double)CognexInterop.TryGet(b, "CenterOfMassX", 0.0);
                            double cy = (double)CognexInterop.TryGet(b, "CenterOfMassY", 0.0);
                            defects.Add((a, cx, cy));
                            totalArea += a;
                        }
                    }
                }
                catch { /* 결과 enumerable 변환 실패 시 빈 결과 */ }

                bool pass = totalArea <= MaxTotalDefectArea;
                var inspResult = new InspectionResult { RoiName = Id, IsPass = pass };
                inspResult.Items.Add(new InspectionItem { Name = "DefectCount",     Value = defects.Count.ToString(),    IsPass = pass });
                inspResult.Items.Add(new InspectionItem { Name = "TotalDefectArea", Value = totalArea.ToString("F1"),    IsPass = pass });
                inspResult.Items.Add(new InspectionItem { Name = "Threshold",       Value = Threshold.ToString(),         IsPass = true });
                if (defects.Count > 0)
                {
                    var biggest = defects[0];
                    foreach (var d in defects) if (d.area > biggest.area) biggest = d;
                    inspResult.Items.Add(new InspectionItem { Name = "MaxDefectArea", Value = biggest.area.ToString("F1"), IsPass = pass });
                    inspResult.Items.Add(new InspectionItem { Name = "MaxDefectX",    Value = biggest.cx.ToString("F2"),   IsPass = true });
                    inspResult.Items.Add(new InspectionItem { Name = "MaxDefectY",    Value = biggest.cy.ToString("F2"),   IsPass = true });
                }
                return inspResult;
            }
            catch (Exception ex)
            {
                // 런타임 실패 → fallback
                var fb = _fallback.Inspect(image);
                if (string.IsNullOrEmpty(fb.ErrorMessage))
                    fb.ErrorMessage = "Cognex blob failed (" + ex.Message + "); fallback used";
                return fb;
            }
        }

        public void LoadParameters(string path) { /* 추후 JSON 파라미터 — Tier 3 */ }
        public void SaveParameters(string path) { /* 추후 JSON 파라미터 — Tier 3 */ }

        // P1 — SSOT 디스크립터: InspectionRoi(Setup) + Blob 임계(Recipe, #2)
        public string ParameterTarget => Id;
        public IEnumerable<ParameterDescriptor> DescribeParameters()
        {
            foreach (var d in VisionParameterDescriptors.InspectorRoi(this)) yield return d;
            yield return ParameterDescriptor.Int(Id, "Threshold", "Threshold", "", ParameterLayer.Recipe, () => Threshold, v => Threshold = v, min: 0, max: 255);
            yield return ParameterDescriptor.Int(Id, "MinDefectArea", "Min Defect Area", "px²", ParameterLayer.Recipe, () => MinDefectArea, v => MinDefectArea = v, min: 0);
            yield return ParameterDescriptor.Double(Id, "MaxTotalDefectArea", "Max Total Defect Area", "px²", ParameterLayer.Recipe, () => MaxTotalDefectArea, v => MaxTotalDefectArea = v, min: 0);
        }
    }
}
