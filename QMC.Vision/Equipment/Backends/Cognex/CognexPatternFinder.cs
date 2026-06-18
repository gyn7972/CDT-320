using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.Cognex
{
    /// <summary>
    /// Cognex CogPMAlignTool 기반 Pattern Finder.
    /// 동적 reflection 으로 호출 — 빌드 시 Cognex 어셈블리 의존 없음.
    /// 미로드/실패 시 OpenCvPatternFinder(BasicSad) 로 자동 fallback.
    /// </summary>
    public class CognexPatternFinder : IPatternFinder
    {
        public string Id { get; }
        public Roi SearchRoi { get; set; }
        public Roi TrainRoi  { get; set; }
        public Bitmap TrainImage { get; private set; }
        public double AcceptThreshold { get; set; } = 0.7;
        public int    MaxInstances    { get; set; } = 1;

        private readonly CognexBackend _be;
        private readonly OpenCv.OpenCvPatternFinder _fallback;

        // CogPMAlignTool 인스턴스 (dynamic). null = 미학습.
        private dynamic _pma;
        private bool    _trainSucceeded;

        public CognexPatternFinder(string id, CognexBackend be)
        {
            Id = id; _be = be;
            _fallback = new OpenCv.OpenCvPatternFinder(id, new OpenCv.OpenCvBackend());
            SearchRoi = new Roi { Name = id + ".Search", CenterX = 320, CenterY = 240, Width = 400, Height = 300 };
            TrainRoi  = new Roi { Name = id + ".Train",  CenterX = 320, CenterY = 240, Width = 100, Height = 100 };
        }

        public void Train(Bitmap image)
        {
            if (image == null) return;
            var rect = TrainRoi.BoundingBox;
            rect.Intersect(new Rectangle(0, 0, image.Width, image.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;
            TrainImage?.Dispose();
            TrainImage = image.Clone(rect, image.PixelFormat);

            // OpenCv fallback 도 항상 학습 (Cognex 실패시 안전망)
            _fallback.TrainRoi  = TrainRoi.Clone();
            _fallback.SearchRoi = SearchRoi.Clone();
            _fallback.Train(image);

            _trainSucceeded = false;
            if (!_be.CognexLoaded) return;

            try
            {
                var asms = _be.LoadedAssemblies;
                var pmaType = CognexInterop.GetType("Cognex.VisionPro.PMAlign.CogPMAlignTool", asms);
                if (pmaType == null) return;

                _pma = Activator.CreateInstance(pmaType);
                dynamic cogImage = CognexInterop.BitmapToICogImage(image, asms);
                dynamic trainRegion = CognexInterop.NewRectangle(rect.X, rect.Y, rect.Width, rect.Height, asms);

                // Pattern.TrainImage / TrainRegion 설정
                dynamic pattern = _pma.Pattern;
                pattern.TrainImage  = cogImage;
                pattern.TrainRegion = trainRegion;
                // Origin 자동 (학습 영역 중심)
                CognexInterop.TrySet(pattern, "Origin",
                    CognexInterop.NewRectangle(rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0, 0, 0, asms));

                pattern.Train();
                _trainSucceeded = true;
            }
            catch
            {
                // 라이선스/타입/런타임 오류 — 무시하고 fallback 으로
                _trainSucceeded = false;
                _pma            = null;
            }
        }

        public void LoadTrainImage(Bitmap pattern)
        {
            TrainImage?.Dispose();
            TrainImage = pattern != null ? (Bitmap)pattern.Clone() : null;   // null = 학습 패턴 제거
            _fallback.LoadTrainImage(pattern);   // OpenCV fallback 도 동기화
            if (pattern == null) { _trainSucceeded = false; _pma = null; return; }

            _trainSucceeded = false;
            if (!_be.CognexLoaded) return;
            try
            {
                var asms = _be.LoadedAssemblies;
                var pmaType = CognexInterop.GetType("Cognex.VisionPro.PMAlign.CogPMAlignTool", asms);
                if (pmaType == null) return;
                _pma = Activator.CreateInstance(pmaType);
                dynamic cogImage = CognexInterop.BitmapToICogImage(pattern, asms);
                dynamic region   = CognexInterop.NewRectangle(0, 0, pattern.Width, pattern.Height, asms);
                dynamic patt = _pma.Pattern;
                patt.TrainImage  = cogImage;
                patt.TrainRegion = region;   // 저장 패턴 전체 영역 학습
                patt.Train();
                _trainSucceeded = true;
            }
            catch { _trainSucceeded = false; _pma = null; }
        }

        public MatchResult Match(Bitmap image)
        {
            // Cognex 학습 성공 시에만 Cognex 사용, 그 외엔 fallback
            if (image == null) return MatchResult.Fail(Id, "no image");
            if (!_be.CognexLoaded || !_trainSucceeded || _pma == null)
                return _fallback.Match(image);

            try
            {
                var asms = _be.LoadedAssemblies;
                dynamic cogImage = CognexInterop.BitmapToICogImage(image, asms);
                _pma.InputImage = cogImage;

                var s = SearchRoi.BoundingBox;
                s.Intersect(new Rectangle(0, 0, image.Width, image.Height));
                dynamic searchRegion = CognexInterop.NewRectangle(s.X, s.Y, s.Width, s.Height, asms);
                _pma.SearchRegion = searchRegion;
                CognexInterop.TrySet(_pma, "RunParams",  _pma.RunParams);  // no-op (속성 접근만)

                // RunParams.AcceptThreshold / NumToFind 설정
                try { _pma.RunParams.AcceptThreshold = AcceptThreshold; } catch { }
                try { _pma.RunParams.ApproximateNumberToFind = MaxInstances; } catch { }

                _pma.Run();

                var result = new MatchResult { RoiName = Id, Success = true };
                dynamic results = _pma.Results;
                int idx = 0;
                if (results is IEnumerable enumer)
                {
                    foreach (dynamic r in enumer)
                    {
                        // GetPose() / Score / TranslationX / TranslationY / Rotation
                        double cx = 0, cy = 0, ang = 0, score = 0;
                        try
                        {
                            dynamic pose = r.GetPose();
                            cx = (double)pose.TranslationX;
                            cy = (double)pose.TranslationY;
                            ang = (double)pose.Rotation * (180.0 / Math.PI);   // rad → deg
                        }
                        catch
                        {
                            cx = (double)CognexInterop.TryGet(r, "TranslationX", 0.0);
                            cy = (double)CognexInterop.TryGet(r, "TranslationY", 0.0);
                            ang = (double)CognexInterop.TryGet(r, "Rotation", 0.0) * (180.0 / Math.PI);
                        }
                        try { score = (double)r.Score; }
                        catch { score = (double)CognexInterop.TryGet(r, "Score", 0.0); }

                        result.Instances.Add(new MatchInstance
                        {
                            Index    = idx++,
                            CenterX  = cx,
                            CenterY  = cy,
                            AngleDeg = ang,
                            Score    = score
                        });
                    }
                }

                if (result.Instances.Count == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "no match";
                }
                return result;
            }
            catch (Exception ex)
            {
                // 런타임 실패 — fallback 사용 (라이선스/도ngle 미체결 등)
                var fb = _fallback.Match(image);
                if (!fb.Success) fb.ErrorMessage = "Cognex run failed (" + ex.Message + "); fallback also failed";
                return fb;
            }
        }

    }
}
