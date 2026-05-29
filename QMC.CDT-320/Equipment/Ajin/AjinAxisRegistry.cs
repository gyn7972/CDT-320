using System;
using System.Collections.Generic;
using System.Linq;
using QMC.Common.Motion;

namespace QMC.CDT320.Ajin
{
    /// <summary>
    /// CDT-320 standard axis list provider.
    /// All UI pages and popups should use this source instead of walking unit trees.
    /// </summary>
    public static class AjinAxisRegistry
    {
        public static List<BaseAxis> GetOrderedAxes()
        {
            AjinFactory.RegisterConfiguredAxes();

            var axes = AjinFactory.AxisManager.GetAll()
                .Where(axis => axis != null && axis.Setup != null)
                .ToList();

            var byName = new Dictionary<string, BaseAxis>(StringComparer.OrdinalIgnoreCase);
            var byNo = new Dictionary<int, BaseAxis>();

            foreach (BaseAxis axis in axes)
            {
                string key = AjinAxisDefaults.ResolveName(axis.Name);
                if (!string.IsNullOrWhiteSpace(key) && !byName.ContainsKey(key))
                    byName.Add(key, axis);

                int axisNo = axis.Setup.AxisNo;
                if (axisNo >= 0 && !byNo.ContainsKey(axisNo))
                    byNo.Add(axisNo, axis);
            }

            var result = new List<BaseAxis>();
            foreach (AxisDefault definition in AjinAxisDefaults.All.OrderBy(axis => axis.Axis))
            {
                BaseAxis axis;
                if (!byName.TryGetValue(definition.AxisName, out axis))
                    byNo.TryGetValue(definition.Axis, out axis);

                if (axis != null && !result.Any(x => ReferenceEquals(x, axis)))
                    result.Add(axis);
            }

            return result;
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
    }
}
