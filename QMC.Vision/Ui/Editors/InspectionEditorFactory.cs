using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Editors
{
    /// <summary>
    /// P3 (G3) — InspectionParameters 편집기 팩토리. SettingsPage/ParameterEditorHost 의 중복 문자열 switch 대체.
    /// tool 키는 Core\Parameters\InspectionParamRegistry 와 동일(정규 매핑 단일 소스).
    /// </summary>
    public static class InspectionEditorFactory
    {
        private static readonly Dictionary<string, Func<UserControl>> _map =
            new Dictionary<string, Func<UserControl>>(StringComparer.OrdinalIgnoreCase)
            {
                { "BottomInspection", () => new BottomInspectionParameterEditor() },
                { "SideInspection",   () => new SideInspectionParameterEditor() },
                { "DieGapInspection", () => new DieGapInspectionParameterEditor() },
                { "Distortion",       () => new DistortionParameterEditor() },
                { "VisionScale",      () => new VisionScaleParameterEditor() },
            };

        public static UserControl Create(string tool)
            => tool != null && _map.TryGetValue(tool, out var f) ? f() : null;
    }
}
