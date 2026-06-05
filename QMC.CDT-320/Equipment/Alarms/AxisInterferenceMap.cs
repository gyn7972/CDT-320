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
            //map.AddGroup("InputCassette", "WaferLifterZ", "FeederY");
            //map.AddGroup("InputStage", "FeederY", "StageY", "StageT", "ExpanderZ", "CameraX", "CameraZ", "NeedleBlockX", "NeedleZ", "EjectPinZ");
            //map.AddGroup("TransferFront", "LeftArm_ArmX", "LeftArm_ArmY", "LeftArm_SideVisionY",
            //    "LeftArm_Picker1_Z", "LeftArm_Picker1_T", "LeftArm_Picker2_Z", "LeftArm_Picker2_T",
            //    "LeftArm_Picker3_Z", "LeftArm_Picker3_T", "LeftArm_Picker4_Z", "LeftArm_Picker4_T",
            //    "StageY", "StageT", "ExpanderZ", "GoodStage_StageY", "GoodStage_StageZ", "NgStage_StageY", "NgStage_StageZ");
            //map.AddGroup("TransferRear", "RightArm_ArmX", "RightArm_ArmY", "RightArm_SideVisionY",
            //    "RightArm_Picker1_Z", "RightArm_Picker1_T", "RightArm_Picker2_Z", "RightArm_Picker2_T",
            //    "RightArm_Picker3_Z", "RightArm_Picker3_T", "RightArm_Picker4_Z", "RightArm_Picker4_T",
            //    "StageY", "StageT", "ExpanderZ", "GoodStage_StageY", "GoodStage_StageZ", "NgStage_StageY", "NgStage_StageZ");
            //map.AddGroup("OutputStage", "GoodStage_StageY", "GoodStage_StageZ", "NgStage_StageY", "NgStage_StageZ", "OutputStage_BinCameraX",
            //    "LeftArm_ArmX", "LeftArm_ArmY", "RightArm_ArmX", "RightArm_ArmY", "BinFeederY", "BinLifterZ", "OutputUnloader_FeederY");
            //map.AddGroup("OutputCassette", "BinLifterZ", "BinFeederY", "OutputUnloader_FeederY");
            //map.AddGroup("PostPnp", "PostPnp_Z", "LeftArm_ArmX", "LeftArm_ArmY", "RightArm_ArmX", "RightArm_ArmY");

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
