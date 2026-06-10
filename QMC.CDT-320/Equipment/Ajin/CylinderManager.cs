using System;
using System.Collections.Generic;
using QMC.Common.IO;
using QMC.Common.Logging;

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
                    ApplyMappingToCylinder(item.Name, cylinder);
                    CylinderSettingsStore.Apply(cylinder);
                    _items[item.Name] = cylinder;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "CYL-MAP-INIT",
                    "Cylinder manager initialize failed: " + ex.Message);
                throw;
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
                ApplyMappingToCylinder(name, cylinder);
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
                    ApplyMappingToCylinder(pair.Key, pair.Value);
                    CylinderSettingsStore.Apply(pair.Value);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "CYL-MAP-APPLY",
                    "Cylinder mapping apply failed: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private static void ApplyMappingToCylinder(string name, BaseCylinder cylinder)
        {
            if (cylinder == null || string.IsNullOrWhiteSpace(name))
                return;

            CylMap map;
            if (AjinConfigStore.Current == null ||
                AjinConfigStore.Current.Cylinders == null ||
                !AjinConfigStore.Current.Cylinders.TryGetValue(name, out map) ||
                map == null)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "CYL-MAP-APPLY",
                    "Cylinder mapping not found. Default catalog may be used. name=" + name);
                return;
            }

            RebindCylinder(
                cylinder,
                map.OutFwd,
                map.OutBwd,
                map.UseFwdInput ? map.InFwd : null,
                map.UseBwdInput ? map.InBwd : null);

            EventLogger.Write(EventKind.Event, "QMC", "CYL-MAP-APPLY",
                "Cylinder mapping applied. name=" + name
                + ", fwdDO=" + Format(map.OutFwd, true)
                + ", bwdDO=" + Format(map.OutBwd, true)
                + ", fwdDI=" + Format(map.UseFwdInput ? map.InFwd : null, false)
                + ", bwdDI=" + Format(map.UseBwdInput ? map.InBwd : null, false));
        }

        private static void RebindCylinder(
            BaseCylinder cylinder,
            DioMap outFwd,
            DioMap outBwd,
            DioMap inFwd,
            DioMap inBwd)
        {
            AjinCylinder ajinCylinder = cylinder as AjinCylinder;
            if (ajinCylinder != null)
            {
                ajinCylinder.Rebind(outFwd, outBwd, inFwd, inBwd);
                return;
            }

            ReplaceCylinderIo(cylinder, outFwd, outBwd, inFwd, inBwd);
        }

        private static void ReplaceCylinderIo(
            BaseCylinder cylinder,
            DioMap outFwd,
            DioMap outBwd,
            DioMap inFwd,
            DioMap inBwd)
        {
            if (cylinder == null)
                return;

            AjinIoScanService scan = AjinIoScanService.Current;
            if (scan != null)
            {
                scan.UnregisterOutput(cylinder.OutFwd as AjinDigitalOutput);
                scan.UnregisterOutput(cylinder.OutBwd as AjinDigitalOutput);
                scan.UnregisterInput(cylinder.InFwd as AjinDigitalInput);
                scan.UnregisterInput(cylinder.InBwd as AjinDigitalInput);
            }

            var flags = System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.NonPublic;
            var type = typeof(BaseCylinder);

            type.GetProperty("OutFwd", flags).SetValue(
                cylinder,
                AjinFactory.CreateSharedDigitalOutput(cylinder.Name + "_OutFwd", outFwd, !AjinFactory.IsRealBoardReady));
            type.GetProperty("OutBwd", flags).SetValue(
                cylinder,
                AjinFactory.CreateSharedDigitalOutput(cylinder.Name + "_OutBwd", outBwd, !AjinFactory.IsRealBoardReady));
            type.GetProperty("InFwd", flags).SetValue(
                cylinder,
                AjinFactory.CreateSharedDigitalInput(cylinder.Name + "_InFwd", inFwd, !AjinFactory.IsRealBoardReady));
            type.GetProperty("InBwd", flags).SetValue(
                cylinder,
                AjinFactory.CreateSharedDigitalInput(cylinder.Name + "_InBwd", inBwd, !AjinFactory.IsRealBoardReady));

            cylinder.Setup.UseFwdSensor = inFwd != null;
            cylinder.Setup.UseBwdSensor = inBwd != null;

            if (scan != null)
            {
                scan.RegisterOutput(cylinder.OutFwd as AjinDigitalOutput);
                scan.RegisterOutput(cylinder.OutBwd as AjinDigitalOutput);
                scan.RegisterInput(cylinder.InFwd as AjinDigitalInput);
                scan.RegisterInput(cylinder.InBwd as AjinDigitalInput);
            }
        }

        private static string Format(DioMap map, bool output)
        {
            if (map == null)
                return "-";

            string address = output
                ? AjinIoCatalog.OutputAddress(map.Module, map.Bit)
                : AjinIoCatalog.InputAddress(map.Module, map.Bit);
            return address + "(M" + map.Module + ",B" + map.Bit + ")";
        }
    }
}
