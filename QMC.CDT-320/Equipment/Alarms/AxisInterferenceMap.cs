using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.CDT320.Alarms
{
    public sealed class AxisInterferenceMap
    {
        private readonly Dictionary<string, HashSet<string>> _axisToGroup =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public static AxisInterferenceMap CreateDefault()
        {
            var map = new AxisInterferenceMap();

            //우선은 이거 막아 놓자.
            //map.AddGroup("InputCassette", "InputLifterZ", "FeederY");
            //map.AddGroup("InputStage", "FeederY", "StageY", "StageT", "ExpanderZ", "CameraX", "NeedleBlockX", "NeedleZ", "EjectPinZ");
            //map.AddGroup("TransferFront", "FrontPickerX", "FrontPickerY", "FrontSideVisionY0",
            //    "FrontPickerZ0", "FrontPickerT0", "FrontPickerZ1", "FrontPickerT1",
            //    "FrontPickerZ2", "FrontPickerT2", "FrontPickerZ3", "FrontPickerT3",
            //    "StageY", "StageT", "ExpanderZ", "OutputGoodStageY", "OutputGoodStageZ", "OutputNGStageY", "NgStage_StageZ");
            //map.AddGroup("TransferRear", "RearPickerX", "RearPickerY", "RearSideVisionY0",
            //    "RearPickerZ0", "RearPickerT0", "RearPickerZ1", "RearPickerT1",
            //    "RearPickerZ2", "RearPickerT2", "RearPickerZ3", "RearPickerT3",
            //    "StageY", "StageT", "ExpanderZ", "OutputGoodStageY", "OutputGoodStageZ", "OutputNGStageY", "NgStage_StageZ");
            //map.AddGroup("OutputStage", "OutputGoodStageY", "OutputGoodStageZ", "OutputNGStageY", "NgStage_StageZ", "OutputVisionX",
            //    "FrontPickerX", "FrontPickerY", "RearPickerX", "RearPickerY", "OutputFeederY", "OutputLifterZ", "OutputFeederY");
            //map.AddGroup("OutputCassette", "OutputLifterZ", "OutputFeederY", "OutputFeederY");
            //map.AddGroup("PostPnp", "PostPnp_Z", "FrontPickerX", "FrontPickerY", "RearPickerX", "RearPickerY");

            return map;
        }

        public void AddGroup(string groupName, params string[] axisNames)
        {
            try
            {
                if (axisNames == null)
                    return;

                var clean = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var axisName in axisNames)
                {
                    if (!string.IsNullOrWhiteSpace(axisName))
                        clean.Add(axisName.Trim());
                }

                foreach (var axisName in clean)
                {
                    HashSet<string> group;
                    if (!_axisToGroup.TryGetValue(axisName, out group))
                    {
                        group = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        _axisToGroup[axisName] = group;
                    }

                    foreach (var other in clean)
                        group.Add(other);
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        public IReadOnlyList<string> ResolveInterferenceAxes(string axisName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(axisName))
                    return new string[0];

                HashSet<string> group;
                if (_axisToGroup.TryGetValue(axisName, out group))
                    return group.ToList();

                return new[] { axisName };
            }
            catch
            {
                return new string[0];
            }
            finally
            {
            }
        }
    }
}
