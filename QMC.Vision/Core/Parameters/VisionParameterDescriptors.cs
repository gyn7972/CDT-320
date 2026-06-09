using System.Collections.Generic;

namespace QMC.Vision.Core.Parameters
{
    /// <summary>
    /// finder/inspector 공통 디스크립터 빌더 — IPatternFinder/IInspector 인터페이스에서 직접 생성(3 백엔드 공용).
    /// 계층: docs/PARAMETER_LAYER_DECISIONS.md 확정 (#1 임계=Recipe, #6 Search ROI 일괄 Setup, Train ROI=Recipe).
    /// </summary>
    public static class VisionParameterDescriptors
    {
        /// <summary>패턴 finder 표준 파라미터(SearchRoi=Setup, TrainRoi=Recipe, AcceptThreshold/MaxInstances=Recipe).</summary>
        public static IEnumerable<ParameterDescriptor> Finder(IPatternFinder f)
        {
            string t = f.Id;
            // SearchRoi → Setup (#6 일괄 Setup; 타깃별 override 는 P3)
            yield return ParameterDescriptor.Double(t, "SearchRoi.CenterX", "Search X", "px", ParameterLayer.Setup, () => f.SearchRoi.CenterX, v => f.SearchRoi.CenterX = v);
            yield return ParameterDescriptor.Double(t, "SearchRoi.CenterY", "Search Y", "px", ParameterLayer.Setup, () => f.SearchRoi.CenterY, v => f.SearchRoi.CenterY = v);
            yield return ParameterDescriptor.Double(t, "SearchRoi.Width",   "Search W", "px", ParameterLayer.Setup, () => f.SearchRoi.Width,   v => f.SearchRoi.Width = v,   min: 0);
            yield return ParameterDescriptor.Double(t, "SearchRoi.Height",  "Search H", "px", ParameterLayer.Setup, () => f.SearchRoi.Height,  v => f.SearchRoi.Height = v,  min: 0);
            // TrainRoi → Recipe
            yield return ParameterDescriptor.Double(t, "TrainRoi.CenterX", "Train X", "px", ParameterLayer.Recipe, () => f.TrainRoi.CenterX, v => f.TrainRoi.CenterX = v);
            yield return ParameterDescriptor.Double(t, "TrainRoi.CenterY", "Train Y", "px", ParameterLayer.Recipe, () => f.TrainRoi.CenterY, v => f.TrainRoi.CenterY = v);
            yield return ParameterDescriptor.Double(t, "TrainRoi.Width",   "Train W", "px", ParameterLayer.Recipe, () => f.TrainRoi.Width,   v => f.TrainRoi.Width = v,   min: 0);
            yield return ParameterDescriptor.Double(t, "TrainRoi.Height",  "Train H", "px", ParameterLayer.Recipe, () => f.TrainRoi.Height,  v => f.TrainRoi.Height = v,  min: 0);
            // 임계 → Recipe (#1)
            yield return ParameterDescriptor.Double(t, "AcceptThreshold", "Accept Threshold", "", ParameterLayer.Recipe, () => f.AcceptThreshold, v => f.AcceptThreshold = v, min: 0, max: 1);
            yield return ParameterDescriptor.Int(t, "MaxInstances", "Max Instances", "", ParameterLayer.Recipe, () => f.MaxInstances, v => f.MaxInstances = v, min: 1);
        }

        /// <summary>inspector 공통 InspectionRoi(Setup).</summary>
        public static IEnumerable<ParameterDescriptor> InspectorRoi(IInspector i)
        {
            string t = i.Id;
            yield return ParameterDescriptor.Double(t, "InspectionRoi.CenterX", "Inspect X", "px", ParameterLayer.Setup, () => i.InspectionRoi.CenterX, v => i.InspectionRoi.CenterX = v);
            yield return ParameterDescriptor.Double(t, "InspectionRoi.CenterY", "Inspect Y", "px", ParameterLayer.Setup, () => i.InspectionRoi.CenterY, v => i.InspectionRoi.CenterY = v);
            yield return ParameterDescriptor.Double(t, "InspectionRoi.Width",   "Inspect W", "px", ParameterLayer.Setup, () => i.InspectionRoi.Width,   v => i.InspectionRoi.Width = v,   min: 0);
            yield return ParameterDescriptor.Double(t, "InspectionRoi.Height",  "Inspect H", "px", ParameterLayer.Setup, () => i.InspectionRoi.Height,  v => i.InspectionRoi.Height = v,  min: 0);
        }
    }
}
