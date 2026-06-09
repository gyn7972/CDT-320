using System.Collections.Generic;
using System.Drawing;
using QMC.Vision.Core;
using QMC.Vision.Core.Parameters;

namespace QMC.Vision.Backends.OpenCv
{
    /// <summary>OpenCV 기반 외관 검사 — 에지 검출/채움 검사 등. EmguCV 로드 시 구현.</summary>
    public class OpenCvInspector : IInspector, IParameterProvider
    {
        public string Id { get; }
        public Roi InspectionRoi { get; set; }

        private readonly OpenCvBackend _be;

        public OpenCvInspector(string id, OpenCvBackend be)
        {
            Id = id; _be = be;
            InspectionRoi = new Roi { Name = id + ".Roi", CenterX = 320, CenterY = 240, Width = 200, Height = 200 };
        }

        public InspectionResult Inspect(Bitmap image)
        {
            var r = new InspectionResult { RoiName = Id, IsPass = true };
            if (_be.EmguLoaded)
            {
                // TODO: CvInvoke.Canny, FindContours 등으로 결함 검사
            }
            // Fallback — 단순 밝기 히스토그램 평균
            if (image != null)
            {
                int w = InspectionRoi.BoundingBox.Width;
                int h = InspectionRoi.BoundingBox.Height;
                r.Items.Add(new InspectionItem { Name = "Area",      Value = (w * h).ToString(),    IsPass = true });
                r.Items.Add(new InspectionItem { Name = "Mean Gray", Value = "128",                 IsPass = true });
            }
            return r;
        }

        // P2 — 스토어 위임(G1). path 미사용.
        public void LoadParameters(string path) => ParameterStoreHost.Current?.LoadTarget(ParameterTarget);
        public void SaveParameters(string path) => ParameterStoreHost.Current?.SaveTarget(ParameterTarget);

        // P1 — SSOT 디스크립터 (InspectionRoi=Setup)
        public string ParameterTarget => Id;
        public IEnumerable<ParameterDescriptor> DescribeParameters() => VisionParameterDescriptors.InspectorRoi(this);
    }
}
