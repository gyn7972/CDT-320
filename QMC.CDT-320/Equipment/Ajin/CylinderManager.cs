using System;
using System.Collections.Generic;
using QMC.Common.IO;
using QMC.Common.Logging;

namespace QMC.CDT320.Ajin
{
    /// <summary>
    /// 카탈로그 이름을 기준으로 실린더 인스턴스를 관리하고, AjinConfig 매핑과 저장 설정을 적용합니다.
    /// </summary>
    public static class CylinderManager
    {
        // 정상 등록된 실린더를 이름별로 보관해 동일한 이름 요청에서 공유하는 인스턴스입니다.
        private static readonly Dictionary<string, BaseCylinder> _items =
            new Dictionary<string, BaseCylinder>(StringComparer.OrdinalIgnoreCase);

        // 이미 적용한 물리 IO 조합을 기록해 불필요한 Rebind를 방지합니다.
        private static readonly Dictionary<string, string> _appliedMappingKeys =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, BaseCylinder> Items { get { return _items; } }

        public static void Initialize()
        {
            // 초기화 순서: 저장 설정 로드 -> 카탈로그 객체 생성 -> IO 매핑 -> 동작 설정 적용.
            try
            {
                _items.Clear();
                _appliedMappingKeys.Clear();
                CylinderSettingsStore.Load();

                foreach (CylinderDefault item in AjinIoCatalog.Cylinders)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.Name)) continue;
                    BaseCylinder cylinder = AjinFactory.CreateCylinder(item);
                    ApplyMappingToCylinder(item.Name, cylinder, true);
                    CylinderSettingsStore.Apply(cylinder);
                    _items[item.Name] = cylinder;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "CYL-MAP-INIT", "CylinderManager",
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
                {
                    ApplyMappingToCylinder(name, cylinder, false);
                    CylinderSettingsStore.Apply(cylinder);
                    return cylinder;
                }

                CylinderDefault catalog = AjinIoCatalog.FindCylinder(name);
                cylinder = catalog == null ? new SimCylinder(name) : AjinFactory.CreateCylinder(catalog);
                ApplyMappingToCylinder(name, cylinder, true);
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
            // 현재 IO 연결은 유지하고 Simulation, 센서 사용 여부, Timeout 등 동작 설정만 다시 적용합니다.
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
            // AjinConfig의 현재 IO 매핑으로 모든 등록 실린더를 강제로 다시 연결한 뒤 설정을 적용합니다.
            try
            {
                foreach (var pair in _items)
                {
                    ApplyMappingToCylinder(pair.Key, pair.Value, true);
                    CylinderSettingsStore.Apply(pair.Value);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "CYL-MAP-APPLY", "CylinderManager",
                    "Cylinder mapping apply failed: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private static void ApplyMappingToCylinder(string name, BaseCylinder cylinder, bool force)
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

            string mappingKey = BuildMappingKey(map);
            string appliedKey;
            if (!force &&
                _appliedMappingKeys.TryGetValue(name, out appliedKey) &&
                string.Equals(appliedKey, mappingKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RebindCylinder(
                cylinder,
                map.OutFwd,
                map.OutBwd,
                map.UseFwdInput ? map.InFwd : null,
                map.UseBwdInput ? map.InBwd : null);

            _appliedMappingKeys[name] = mappingKey;

            EventLogger.Write(EventKind.Event, "QMC", "CYL-MAP-APPLY",
                "Cylinder mapping applied. name=" + name
                + ", fwdDO=" + Format(map.OutFwd, true)
                + ", bwdDO=" + Format(map.OutBwd, true)
                + ", fwdDI=" + Format(map.UseFwdInput ? map.InFwd : null, false)
                + ", bwdDI=" + Format(map.UseBwdInput ? map.InBwd : null, false)
                + ", actualFwdDO=" + Format(cylinder.OutFwd, true)
                + ", actualBwdDO=" + Format(cylinder.OutBwd, true)
                + ", actualFwdDI=" + Format(cylinder.InFwd, false)
                + ", actualBwdDI=" + Format(cylinder.InBwd, false));
        }

        private static string BuildMappingKey(CylMap map)
        {
            if (map == null)
                return string.Empty;

            return FormatKey(map.OutFwd)
                + "|" + FormatKey(map.OutBwd)
                + "|" + (map.UseFwdInput ? "F1:" : "F0:") + FormatKey(map.InFwd)
                + "|" + (map.UseBwdInput ? "B1:" : "B0:") + FormatKey(map.InBwd)
                + "|" + (map.SingleSolenoid ? "S1" : "S0");
        }

        private static string FormatKey(DioMap map)
        {
            if (map == null)
                return "-";

            return map.Module + ":" + map.Bit + ":" + (map.Nc ? "NC" : "NO");
        }

        private static void RebindCylinder(
            BaseCylinder cylinder,
            DioMap outFwd,
            DioMap outBwd,
            DioMap inFwd,
            DioMap inBwd)
        {
            // AjinCylinder는 전용 Rebind를 사용하고, 그 외 구현은 BaseCylinder의 IO를 교체합니다.
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
                AjinFactory.CreateCylinderDigitalOutput(cylinder.Name + "_OutFwd", outFwd, !AjinFactory.IsRealBoardReady));
            type.GetProperty("OutBwd", flags).SetValue(
                cylinder,
                AjinFactory.CreateCylinderDigitalOutput(cylinder.Name + "_OutBwd", outBwd, !AjinFactory.IsRealBoardReady));
            type.GetProperty("InFwd", flags).SetValue(
                cylinder,
                AjinFactory.CreateCylinderDigitalInput(cylinder.Name + "_InFwd", inFwd, !AjinFactory.IsRealBoardReady));
            type.GetProperty("InBwd", flags).SetValue(
                cylinder,
                AjinFactory.CreateCylinderDigitalInput(cylinder.Name + "_InBwd", inBwd, !AjinFactory.IsRealBoardReady));

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

        private static string Format(BaseDigitalOutput output, bool isOutput)
        {
            if (output == null)
                return "-";

            int module = output.Setup.ModuleNo;
            int bit = output.Setup.BitNo;
            string address = isOutput
                ? AjinIoCatalog.OutputAddress(module, bit)
                : AjinIoCatalog.InputAddress(module, bit);

            return output.Name + ":" + address + "(M" + module + ",B" + bit + ")";
        }

        private static string Format(BaseDigitalInput input, bool isOutput)
        {
            if (input == null)
                return "-";

            int module = input.Setup.ModuleNo;
            int bit = input.Setup.BitNo;
            string address = isOutput
                ? AjinIoCatalog.OutputAddress(module, bit)
                : AjinIoCatalog.InputAddress(module, bit);

            return input.Name + ":" + address + "(M" + module + ",B" + bit + ")";
        }
    }
}
