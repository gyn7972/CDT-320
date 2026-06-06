using System;
using System.Collections.Generic;
using QMC.Common.IO;

namespace QMC.CDT320.Ajin
{
    public static class CylinderManager
    {
        private static readonly Dictionary<string, BaseCylinder> _items =
            new Dictionary<string, BaseCylinder>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, BaseCylinder> Items { get { return _items; } }

        public static void Initialize()
        {
            try
            {
                _items.Clear();
                CylinderSettingsStore.Load();

                foreach (CylinderDefault item in AjinIoCatalog.Cylinders)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.Name)) continue;
                    BaseCylinder cylinder = AjinFactory.CreateCylinder(item);
                    CylinderSettingsStore.Apply(cylinder);
                    _items[item.Name] = cylinder;
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        public static BaseCylinder Get(CylinderDefault catalog)
        {
            try
            {
                if (catalog == null)
                    return Get("UnregisteredCylinder");

                return Get(catalog.Name);
            }
            catch
            {
                return new SimCylinder(catalog == null ? "UnregisteredCylinder" : catalog.Name);
            }
            finally
            {
            }
        }

        public static BaseCylinder Get(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    name = "UnregisteredCylinder";

                BaseCylinder cylinder;
                if (_items.TryGetValue(name, out cylinder) && cylinder != null)
                    return cylinder;

                CylinderDefault catalog = AjinIoCatalog.FindCylinder(name);
                cylinder = catalog == null ? new SimCylinder(name) : AjinFactory.CreateCylinder(catalog);
                CylinderSettingsStore.Apply(cylinder);
                _items[name] = cylinder;
                return cylinder;
            }
            catch
            {
                return new SimCylinder(name);
            }
            finally
            {
            }
        }

        public static void ApplySettings()
        {
            try
            {
                foreach (BaseCylinder cylinder in _items.Values)
                    CylinderSettingsStore.Apply(cylinder);
            }
            catch
            {
            }
            finally
            {
            }
        }

        public static void ApplyMappings()
        {
            try
            {
                foreach (var pair in _items)
                {
                    AjinCylinder cylinder = pair.Value as AjinCylinder;
                    if (cylinder == null) continue;

                    CylMap map;
                    if (!AjinConfigStore.Current.Cylinders.TryGetValue(pair.Key, out map) || map == null)
                        continue;

                    cylinder.Rebind(
                        map.OutFwd,
                        map.OutBwd,
                        map.UseFwdInput ? map.InFwd : null,
                        map.UseBwdInput ? map.InBwd : null);
                    CylinderSettingsStore.Apply(cylinder);
                }
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
