using System;
using System.Collections.Generic;
using System.ComponentModel;
using QMC.CDT_320.Ui.Pages;
using QMC.CDT_320.Ui.Pages.Settings;
using QMC.CDT_320.Ui.Security;
using QMC.CDT320.Ajin;
using QMC.Common.IO;

namespace QMC.CDT_320.Ui.Tabs
{
    public partial class SettingsTab : TabBase
    {
        public SettingsTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            SetSidebarHeader("tab.settings");

            const UserLevel op = UserLevel.Operator;
            const UserLevel en = UserLevel.Engineer;
            const UserLevel mt = UserLevel.Maintenance;

            // ── 주 메뉴 (디자이너 버튼 등록) ──
            RegisterSidebarButton(BtnGeneral,     "set.general",   op, () => new GeneralPage());
            RegisterSidebarButton(BtnMotion,      "set.motion",    mt, () => new MotionPage());
            RegisterSidebarButton(BtnIoControl,   "set.digitalLink", mt, () => new IoControlPage());
            RegisterSidebarButton(BtnDigital,     "set.digital",   mt, () => new IoListPage("set.digital",
                new[] { "INDEX", "MODULE", "SYMBOL", "BOARD", "BIT", "DESCRIPTION", "SIM", "STATE" }, CatalogRows.Digital));
            RegisterSidebarButton(BtnCylinder,    "set.cylinder",  mt, () => new IoListPage("set.cylinder",
                new[] { "INDEX", "MODULE", "NAME", "FWD DO", "BWD DO", "FWD DI", "BWD DI", "SIM", "STATE" }, CatalogRows.Cylinder));
            RegisterSidebarButton(BtnLamp,        "set.lamp",      en, () => new IoListPage("set.lamp",
                new[] { "INDEX", "MODULE", "NAME", "DO", "SIM", "STATE" }, CatalogRows.Lamp));
            RegisterSidebarButton(BtnSwitch,      "set.switch",    en, () => new IoListPage("set.switch",
                new[] { "INDEX", "MODULE", "NAME", "DI", "SIM", "STATE" }, CatalogRows.Switch));
            RegisterSidebarButton(BtnLightSource, "set.lightSource", en, () => new IoListPage("set.lightSource",
                new[] { "INDEX", "NAME", "PORT", "LEVEL" }, CatalogRows.Light));

            // ?? 蹂댁“ 硫붾돱 ??
            RegisterSidebarButton(BtnBarcode,      "set.barcode",      en, () => new BarcodeReaderPage());
            RegisterSidebarButton(BtnZoomLens,     "set.zoomLens",     en, () => new ZoomLensPage());
            RegisterSidebarButton(BtnHeightSensor, "set.heightSensor", en, () => new HeightSensorPage());
            RegisterSidebarButton(BtnSimulator,    "set.simulator",    en, () => new SimulatorLinkPage());
            RegisterSidebarButton(BtnVisionLink,   "set.visionLink",   en, () => new VisionLinkPage());

            // Self-Test 다이얼로그 버튼 — 페이지가 아닌 즉시 팝업
            RegisterSidebarButton(BtnSelfTest, "set.selfTest", en, () => new PlaceholderPage("set.selfTest"));
            BtnSelfTest.Click += (s, e) =>
            {
                var host = FindForm() as Form1;
                using (var dlg = new Dialogs.SystemSelfTestDialog(host))
                    dlg.ShowDialog(host);
            };

            // Stage 19 — Alarm Master 페이지
            RegisterSidebarButton(BtnAlarmMaster, "settings.alarmMaster", en, () => new AlarmMasterPage());

            // Stage 59 — Position Teaching 페이지 (시퀀스 위치 티칭)
            RegisterSidebarButton(BtnTeach,       "set.teach",       en, () => new PositionTeachingPage());
            RegisterSidebarButton(BtnAxisSetup,   "set.axisSetup",   en, () => new AxisSetupPage());
            RegisterSidebarButton(BtnCameraSetup, "set.cameraSetup", en, () => new CameraSetupPage());
            RegisterSidebarButton(BtnLightSetup,  "set.lightSetup",  en, () => new LightControllerPage());

            // Stage 4 — Remote Viewer 다이얼로그
            RegisterSidebarButton(BtnRemoteViewer, "settings.remoteViewer", mt,
                () => new PlaceholderPage("settings.remoteViewer"));
            BtnRemoteViewer.Click += (s, e) =>
            {
                var host = FindForm() as Form1;
                using (var dlg = new Dialogs.RemoteViewerDialog(host))
                    dlg.ShowDialog(host);
            };
        }

        private static class CatalogRows
        {
            public static string[][] Digital()
            {
                var rows = new List<string[]>();
                foreach (var item in AjinIoCatalog.DigitalInputs)
                    rows.Add(DioRow(item, false));
                foreach (var item in AjinIoCatalog.DigitalOutputs)
                    rows.Add(DioRow(item, true));
                return rows.ToArray();
            }

            public static string[][] Cylinder()
            {
                var rows = new List<string[]>();
                for (int i = 0; i < AjinIoCatalog.Cylinders.Length; i++)
                {
                    var item = AjinIoCatalog.Cylinders[i];
                    CylMap map;
                    AjinConfigStore.Current.Cylinders.TryGetValue(item.Name, out map);
                    rows.Add(new[]
                    {
                        (i + 1).ToString(),
                        item.UnitName,
                        item.Name,
                        Format(map != null ? map.OutFwd : null, item.OutFwd, true),
                        Format(map != null ? map.OutBwd : null, item.OutBwd, true),
                        Format(map != null && map.UseFwdInput ? map.InFwd : null, item.InFwd, false),
                        Format(map != null && map.UseBwdInput ? map.InBwd : null, item.InBwd, false),
                        CylinderSettingsStore.Simulation(item.Name, !AjinFactory.IsRealBoardReady) ? "ON" : "OFF",
                        CylinderState(map, item)
                    });
                }
                return rows.ToArray();
            }

            public static string[][] Lamp()
            {
                var rows = new List<string[]>();
                foreach (var item in AjinIoCatalog.DigitalOutputs)
                {
                    if (!IsLamp(item.Name)) 
                        continue;

                    rows.Add(new[] { item.No.ToString(), item.UnitName, item.Name, item.Address, SimText(item.Name, true), State(item, true) });
                }
                return rows.ToArray();
            }

            public static string[][] Switch()
            {
                var rows = new List<string[]>();
                foreach (var item in AjinIoCatalog.DigitalInputs)
                {
                    if (!IsSwitch(item.Name)) continue;
                    rows.Add(new[] { item.No.ToString(), item.UnitName, item.Name, item.Address, SimText(item.Name, false), State(item, false) });
                }
                return rows.ToArray();
            }

            public static string[][] Light() => new[]
            {
                new[] { "1", "INPUT STAGE RING",      "COM1", "128" },
                new[] { "2", "BOTTOM VISION",         "COM2", "180" },
                new[] { "3", "SIDE VISION 1",         "COM2", "200" },
                new[] { "4", "SIDE VISION 2",         "COM2", "200" },
                new[] { "5", "BIN VISION",            "COM3", "140" },
                new[] { "6", "TOP SIDE VISION",       "COM3", "200" },
                new[] { "7", "BOTTOM SIDE VISION",    "COM3", "200" },
                new[] { "8", "ALIGN MARK ILLUM",      "COM1", "100" },
            };

            private static string[] DioRow(DioDefault item, bool isOutput)
            {
                return new[]
                {
                    (isOutput ? "DO-" : "DI-") + item.No,
                    item.UnitName,
                    item.Address,
                    (isOutput ? "DO " : "DI ") + item.Module,
                    item.Bit.ToString("00"),
                    item.Name,
                    SimText(item.Name, isOutput),
                    State(item, isOutput)
                };
            }

            private static string CylinderState(CylMap map, CylinderDefault item)
            {
                bool fwd = IsOn(map != null && map.UseFwdInput ? map.InFwd : null, item != null ? item.InFwd : null, false);
                bool bwd = IsOn(map != null && map.UseBwdInput ? map.InBwd : null, item != null ? item.InBwd : null, false);
                if (fwd && !bwd) return "FWD";
                if (!fwd && bwd) return "BWD";
                if (fwd && bwd) return "BOTH";
                return "OFF";
            }

            private static string Format(DioMap map, DioDefault fallback, bool isOutput)
            {
                if (map == null) return string.Empty;
                var catalog = isOutput
                    ? AjinIoCatalog.FindOutput(map.Module, map.Bit)
                    : AjinIoCatalog.FindInput(map.Module, map.Bit);
                string name = catalog != null ? catalog.Name : string.Empty;
                string address = !string.IsNullOrEmpty(map.Address)
                    ? map.Address
                    : isOutput
                        ? AjinIoCatalog.OutputAddress(map.Module, map.Bit)
                        : AjinIoCatalog.InputAddress(map.Module, map.Bit);
                return string.IsNullOrEmpty(name) ? address : address + " " + name;
            }

            private static string Format(DioDefault item, bool isOutput)
            {
                if (item == null) return string.Empty;
                var catalog = isOutput
                    ? AjinIoCatalog.FindOutput(item.Module, item.Bit)
                    : AjinIoCatalog.FindInput(item.Module, item.Bit);
                string name = catalog != null ? catalog.Name : string.Empty;
                string address = isOutput
                    ? AjinIoCatalog.OutputAddress(item.Module, item.Bit)
                    : AjinIoCatalog.InputAddress(item.Module, item.Bit);
                return string.IsNullOrEmpty(name) ? address : address + " " + name;
            }

            private static string State(DioDefault item, bool isOutput)
            {
                return IsOn(item, isOutput) ? "ON" : "OFF";
            }

            private static string SimText(string name, bool isOutput)
            {
                bool sim = isOutput
                    ? IoSettingsStore.OutputSimulation(name, !AjinFactory.IsRealBoardReady)
                    : IoSettingsStore.InputSimulation(name, !AjinFactory.IsRealBoardReady);
                return sim ? "ON" : "OFF";
            }

            private static bool IsOn(DioDefault item, bool isOutput)
            {
                var service = AjinIoScanService.Current;
                if (service == null || item == null) return false;
                var snapshot = service.GetLatest(item.Module, item.Bit, isOutput);
                return snapshot != null && snapshot.ErrorCode == 0 && snapshot.IsOn;
            }

            private static bool IsOn(DioMap map, DioDefault fallback, bool isOutput)
            {
                var service = AjinIoScanService.Current;
                if (service == null || map == null) return false;
                var snapshot = service.GetLatest(map.Module, map.Bit, isOutput);
                return snapshot != null && snapshot.ErrorCode == 0 && snapshot.IsOn;
            }

            private static bool IsLamp(string name)
            {
                if (Contains(name, "Clamp")) return false;
                return Contains(name, "Lamp") || Contains(name, "Tl") || Contains(name, "Buzzer");
            }

            private static bool IsSwitch(string name)
            {
                return Contains(name, "Button") || Contains(name, "Emg");// || Contains(name, "Door");
            }

            private static bool Contains(string text, string token)
            {
                return text != null && text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
    }
}
