using System.Drawing;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 외관/배치/포커스 검사 공용.
    /// SurfaceInspector / PlacementInspector / Focus 등에서 구현.
    /// </summary>
    public interface IInspector
    {
        string Id { get; }
        Roi InspectionRoi { get; set; }
        InspectionResult Inspect(Bitmap image);
        void LoadParameters(string path);
        void SaveParameters(string path);
    }
}
