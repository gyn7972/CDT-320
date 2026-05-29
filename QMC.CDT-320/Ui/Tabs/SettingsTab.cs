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

            AddSidebarButton("set.general",     op, () => new GeneralPage());
            AddSidebarButton("set.motion",      mt, () => new MotionPage());
            AddSidebarButton("set.ioControl",   mt, () => new IoControlPage());
            AddSidebarButton("set.digital",     mt, () => new IoListPage("set.digital",
                new[] { "INDEX", "SYMBOL", "BOARD", "BIT", "DESCRIPTION", "STATE" }, CatalogRows.Digital));
            AddSidebarButton("set.digitalLink", mt, () => new IoListPage("set.digitalLink",
                new[] { "INDEX", "SOURCE", "TARGET", "DESCRIPTION" }, CatalogRows.Link));
            AddSidebarButton("set.cylinder",    mt, () => new IoListPage("set.cylinder",
                new[] { "INDEX", "NAME", "FWD DO", "BWD DO", "FWD DI", "BWD DI", "STATE" }, CatalogRows.Cylinder));
            AddSidebarButton("set.lamp",        en, () => new IoListPage("set.lamp",
                new[] { "INDEX", "NAME", "DO", "STATE" }, CatalogRows.Lamp));
            AddSidebarButton("set.switch",      en, () => new IoListPage("set.switch",
                new[] { "INDEX", "NAME", "DI", "STATE" }, CatalogRows.Switch));
            AddSidebarButton("set.lightSource", en, () => new IoListPage("set.lightSource",
                new[] { "INDEX", "NAME", "PORT", "LEVEL" }, CatalogRows.Light));

            AddSidebarButton("set.barcode",      en, () => new BarcodeReaderPage(), toBottomArea: true);
            AddSidebarButton("set.zoomLens",     en, () => new ZoomLensPage(),       toBottomArea: true);
            AddSidebarButton("set.heightSensor", en, () => new HeightSensorPage(),   toBottomArea: true);
            AddSidebarButton("set.simulator",    en, () => new SimulatorLinkPage(),  toBottomArea: true);
            AddSidebarButton("set.visionLink",   en, () => new VisionLinkPage(),     toBottomArea: true);

            var btnSelf = AddSidebarButton("set.selfTest", en,
                () => new PlaceholderPage("set.selfTest"), toBottomArea: true);
            btnSelf.Click += (s, e) =>
            {
                var host = FindForm() as Form1;
                using (var dlg = new Dialogs.SystemSelfTestDialog(host))
                    dlg.ShowDialog(host);
            };

            AddSidebarButton("settings.alarmMaster", en, () => new AlarmMasterPage(), toBottomArea: true);

            AddSidebarButton("set.teach",       en, () => new PositionTeachingPage(),  toBottomArea: true);
            AddSidebarButton("set.axisSetup",   en, () => new AxisSetupPage(),         toBottomArea: true);
            AddSidebarButton("set.cameraSetup", en, () => new CameraSetupPage(),       toBottomArea: true);
            AddSidebarButton("set.lightSetup",  en, () => new LightControllerPage(),   toBottomArea: true);

            var btnRemote = AddSidebarButton("settings.remoteViewer", mt,
                () => new PlaceholderPage("settings.remoteViewer"), toBottomArea: true);
            btnRemote.Click += (s, e) =>
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

            public static string[][] Link()
            {
                var rows = new List<string[]>();
                int index = 1;
                foreach (var item in AjinIoCatalog.Cylinders)
                {
                    rows.Add(new[] { (index++).ToString(), Format(item.OutFwd, true), Format(item.InFwd, false), item.Name + " FWD" });
                    rows.Add(new[] { (index++).ToString(), Format(item.OutBwd, true), Format(item.InBwd, false), item.Name + " BWD" });
                }
                return rows.ToArray();
            }

            public static string[][] Cylinder()
            {
                var rows = new List<string[]>();
                for (int i = 0; i < AjinIoCatalog.Cylinders.Length; i++)
                {
                    var item = AjinIoCatalog.Cylinders[i];
                    rows.Add(new[]
                    {
                        (i + 1).ToString(),
                        item.Name,
                        Format(item.OutFwd, true),
                        Format(item.OutBwd, true),
                        Format(item.InFwd, false),
                        Format(item.InBwd, false),
                        CylinderState(item)
                    });
                }
                return rows.ToArray();
            }

            public static string[][] Lamp()
            {
                var rows = new List<string[]>();
                foreach (var item in AjinIoCatalog.DigitalOutputs)
                {
                    if (!IsLamp(item.Name)) continue;
                    rows.Add(new[] { item.No.ToString(), item.Name, item.Address, State(item, true) });
                }
                return rows.ToArray();
            }

            public static string[][] Switch()
            {
                var rows = new List<string[]>();
                foreach (var item in AjinIoCatalog.DigitalInputs)
                {
                    if (!IsSwitch(item.Name)) continue;
                    rows.Add(new[] { item.No.ToString(), item.Name, item.Address, State(item, false) });
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
                    item.Address,
                    (isOutput ? "DO " : "DI ") + item.Module,
                    item.Bit.ToString("00"),
                    item.Name,
                    State(item, isOutput)
                };
            }

            private static string CylinderState(CylinderDefault item)
            {
                bool fwd = IsOn(item.InFwd, false);
                bool bwd = IsOn(item.InBwd, false);
                if (fwd && !bwd) return "FWD";
                if (!fwd && bwd) return "BWD";
                if (fwd && bwd) return "BOTH";
                return "OFF";
            }

            private static string Format(DioDefault item, bool isOutput)
            {
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

            private static bool IsOn(DioDefault item, bool isOutput)
            {
                var service = AjinIoScanService.Current;
                if (service == null || item == null) return false;
                var snapshot = service.GetLatest(item.Module, item.Bit, isOutput);
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
