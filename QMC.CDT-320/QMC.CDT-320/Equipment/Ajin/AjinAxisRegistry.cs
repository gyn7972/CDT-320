using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QMC.Common;
using QMC.Common.Motion;

namespace QMC.CDT320.Ajin
{
    /// <summary>
    /// CDT-320 standard axis list provider.
    /// All UI pages and popups should use this source.
    /// </summary>
    public static class AjinAxisRegistry
    {
        public static List<BaseAxis> GetOrderedAxes()
        {
            return GetOrderedAxes(null);
        }

        public static List<BaseAxis> GetOrderedAxes(CDT320_Machine machine)
        {
            AjinConfig config = AjinConfigStore.Load();
            AjinFactory.RegisterConfiguredAxes();

            List<BaseAxis> registryAxes = AjinFactory.AxisManager.GetAll()
                .Where(axis => axis != null && axis.Setup != null)
                .ToList();
            List<BaseAxis> machineAxes = EnumerateMachineAxes(machine).ToList();

            List<BaseAxis> allAxes = machineAxes
                .Concat(registryAxes)
                .Where(axis => axis != null && axis.Setup != null)
                .Distinct()
                .ToList();

            AjinFactory.ApplyPersistedAxisValues(allAxes);
            return OrderAxes(config, machineAxes.Count > 0 ? machineAxes : allAxes, registryAxes);
        }

        private static List<BaseAxis> OrderAxes(AjinConfig config, IEnumerable<BaseAxis> preferredAxes, IEnumerable<BaseAxis> fallbackAxes)
        {
            var preferred = (preferredAxes ?? Enumerable.Empty<BaseAxis>())
                .Where(axis => axis != null && axis.Setup != null)
                .ToList();
            var fallback = (fallbackAxes ?? Enumerable.Empty<BaseAxis>())
                .Where(axis => axis != null && axis.Setup != null)
                .ToList();

            var result = new List<BaseAxis>();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedAxisNos = new HashSet<int>();

            foreach (var item in GetConfiguredAxisMaps(config))
            {
                string key = item.Key;
                AxisMap map = item.Value;
                int axisNo = map != null ? map.Axis : -1;
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (usedNames.Contains(key)) continue;
                if (axisNo >= 0 && usedAxisNos.Contains(axisNo)) continue;

                BaseAxis axis = FindAxis(preferred, key, axisNo);
                if (axis == null)
                    axis = FindAxis(fallback, key, axisNo);
                if (axis == null)
                    continue;

                ApplyRegistration(axis, key, map);
                result.Add(axis);
                usedNames.Add(key);
                if (axisNo >= 0)
                    usedAxisNos.Add(axisNo);
            }

            AjinFactory.ApplyPersistedAxisValues(result);
            return result;
        }

        private static IEnumerable<KeyValuePair<string, AxisMap>> GetConfiguredAxisMaps(AjinConfig config)
        {
            var axes = config != null && config.Axes != null
                ? config.Axes
                : new Dictionary<string, AxisMap>();

            var result = new List<KeyValuePair<string, AxisMap>>();
            foreach (var item in axes)
            {
                if (item.Value == null) continue;

                string key = AjinAxisDefaults.ResolveName(item.Key);
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase)) continue;

                result.Add(new KeyValuePair<string, AxisMap>(key, item.Value));
            }

            if (result.Count == 0)
            {
                foreach (AxisDefault axis in AjinAxisDefaults.All)
                {
                    result.Add(new KeyValuePair<string, AxisMap>(
                        axis.AxisName,
                        new AxisMap { Axis = axis.Axis, BoardNo = axis.BoardNo, ChannelNo = axis.ChannelNo }));
                }
            }

            return result
                .OrderBy(item => item.Value != null ? item.Value.Axis : int.MaxValue)
                .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase);
        }

        private static BaseAxis FindAxis(IEnumerable<BaseAxis> axes, string key, int axisNo)
        {
            foreach (BaseAxis axis in axes ?? Enumerable.Empty<BaseAxis>())
            {
                if (axis == null || axis.Setup == null) continue;
                if (string.Equals(axis.Name, key, StringComparison.OrdinalIgnoreCase))
                    return axis;
            }

            foreach (BaseAxis axis in axes ?? Enumerable.Empty<BaseAxis>())
            {
                if (axis == null || axis.Setup == null) continue;
                string axisKey = AjinAxisDefaults.ResolveName(axis.Name);
                if (string.Equals(axisKey, key, StringComparison.OrdinalIgnoreCase))
                    return axis;
            }

            foreach (BaseAxis axis in axes ?? Enumerable.Empty<BaseAxis>())
            {
                if (axis == null || axis.Setup == null) continue;
                if (axisNo >= 0 && axis.Setup.AxisNo == axisNo)
                    return axis;
            }

            return null;
        }

        private static void ApplyRegistration(BaseAxis axis, string key, AxisMap map)
        {
            if (axis == null || axis.Setup == null) return;

            AxisDefault definition = AjinAxisDefaults.All.FirstOrDefault(x =>
                string.Equals(x.AxisName, key, StringComparison.OrdinalIgnoreCase));

            axis.Setup.DisplayName = definition != null ? definition.AxisName : key;
            axis.Setup.UnitName = definition != null ? definition.Module : axis.Setup.UnitName;
            if (map != null)
            {
                axis.Setup.AxisNo = map.Axis;
                axis.Setup.BoardNo = map.BoardNo;
            }

            if (definition != null && string.IsNullOrWhiteSpace(axis.Setup.Unit))
                axis.Setup.Unit = definition.Unit;
            if (AjinAxisDefaults.IsThetaAxis(key))
                axis.Setup.Unit = AxisUnitConverter.Degree;
        }

        public static Dictionary<string, BaseAxis> GetAxisMapByName()
        {
            var map = new Dictionary<string, BaseAxis>(StringComparer.OrdinalIgnoreCase);
            foreach (BaseAxis axis in GetOrderedAxes())
            {
                string key = AjinAxisDefaults.ResolveName(axis.Name);
                if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                    map.Add(key, axis);
            }
            return map;
        }

        private static IEnumerable<BaseAxis> EnumerateMachineAxes(CDT320_Machine machine)
        {
            if (machine == null) yield break;

            var visited = new HashSet<BaseEquipmentNode>();
            foreach (BaseEquipmentNode unit in machine.Units)
                foreach (BaseAxis axis in EnumerateAxes(unit, visited))
                    yield return axis;
        }

        private static IEnumerable<BaseAxis> EnumerateAxes(BaseEquipmentNode node, HashSet<BaseEquipmentNode> visited)
        {
            if (node == null || visited == null || visited.Contains(node))
                yield break;

            visited.Add(node);

            BaseAxis axis = node as BaseAxis;
            if (axis != null)
            {
                yield return axis;
                yield break;
            }

            foreach (BaseEquipmentNode child in EnumerateChildNodes(node))
                foreach (BaseAxis childAxis in EnumerateAxes(child, visited))
                    yield return childAxis;
        }

        private static IEnumerable<BaseEquipmentNode> EnumerateChildNodes(BaseEquipmentNode node)
        {
            if (node == null) yield break;

            PropertyInfo componentsProperty = node.GetType().GetProperty("Components");
            if (componentsProperty != null)
            {
                var components = componentsProperty.GetValue(node, null) as System.Collections.IEnumerable;
                if (components != null)
                {
                    foreach (object item in components)
                    {
                        BaseEquipmentNode child = item as BaseEquipmentNode;
                        if (child != null) yield return child;
                    }
                }
            }

            foreach (PropertyInfo property in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length > 0) continue;
                if (!typeof(BaseEquipmentNode).IsAssignableFrom(property.PropertyType)) continue;

                BaseEquipmentNode child = null;
                try { child = property.GetValue(node, null) as BaseEquipmentNode; } catch { }
                if (child != null) yield return child;
            }
        }
    }
}
