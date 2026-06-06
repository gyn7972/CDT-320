using System.ComponentModel;
using QMC.CDT_320.Ui.Pages;
using QMC.CDT_320.Ui.Pages.Settings;
using QMC.CDT_320.Ui.Security;

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
            AddSidebarButton("set.digital",     mt, () => new IoListPage("set.digital",
                new[] { "INDEX", "SYMBOL", "BOARD", "BIT", "DESCRIPTION", "STATE" }, Seed.Digital()));
            AddSidebarButton("set.digitalLink", mt, () => new IoListPage("set.digitalLink",
                new[] { "INDEX", "SOURCE", "TARGET", "DESCRIPTION" }, Seed.Link()));
            AddSidebarButton("set.cylinder",    mt, () => new IoListPage("set.cylinder",
                new[] { "INDEX", "NAME", "FWD DO", "BWD DO", "FWD DI", "BWD DI", "STATE" }, Seed.Cylinder()));
            AddSidebarButton("set.lamp",        en, () => new IoListPage("set.lamp",
                new[] { "INDEX", "NAME", "DO", "STATE" }, Seed.Lamp()));
            AddSidebarButton("set.switch",      en, () => new IoListPage("set.switch",
                new[] { "INDEX", "NAME", "DI", "STATE" }, Seed.Switch()));
            AddSidebarButton("set.lightSource", en, () => new IoListPage("set.lightSource",
                new[] { "INDEX", "NAME", "PORT", "LEVEL" }, Seed.Light()));

            AddSidebarButton("set.barcode",      en, () => new BarcodeReaderPage(), toBottomArea: true);
            AddSidebarButton("set.zoomLens",     en, () => new ZoomLensPage(),       toBottomArea: true);
            AddSidebarButton("set.heightSensor", en, () => new HeightSensorPage(),   toBottomArea: true);
            AddSidebarButton("set.simulator",    en, () => new SimulatorLinkPage(),  toBottomArea: true);
            AddSidebarButton("set.visionLink",   en, () => new VisionLinkPage(),     toBottomArea: true);

            // Self-Test 다이얼로그 버튼 — 페이지가 아닌 즉시 팝업
            var btnSelf = AddSidebarButton("set.selfTest", en,
                () => new PlaceholderPage("set.selfTest"), toBottomArea: true);
            btnSelf.Click += (s, e) =>
            {
                var host = FindForm() as Form1;
                using (var dlg = new Dialogs.SystemSelfTestDialog(host))
                    dlg.ShowDialog(host);
            };

            // Stage 19 — Alarm Master 페이지
            AddSidebarButton("settings.alarmMaster", en, () => new AlarmMasterPage(), toBottomArea: true);

            // Stage 59 — Position Teaching 페이지 (시퀀스 위치 티칭)
            AddSidebarButton("set.teach",       en, () => new PositionTeachingPage(),  toBottomArea: true);
            AddSidebarButton("set.axisSetup",   en, () => new AxisSetupPage(),         toBottomArea: true);
            AddSidebarButton("set.cameraSetup", en, () => new CameraSetupPage(),       toBottomArea: true);
            AddSidebarButton("set.lightSetup",  en, () => new LightControllerPage(),   toBottomArea: true);

            // Stage 4 — Remote Viewer 다이얼로그
            var btnRemote = AddSidebarButton("settings.remoteViewer", mt,
                () => new PlaceholderPage("settings.remoteViewer"), toBottomArea: true);
            btnRemote.Click += (s, e) =>
            {
                var host = FindForm() as Form1;
                using (var dlg = new Dialogs.RemoteViewerDialog(host))
                    dlg.ShowDialog(host);
            };
        }

        private static class Seed
        {
            // Stage 59 — 메뉴얼(CDT-310/CDT-300) 기준 풀세트 시드
            public static string[][] Digital()
            {
                var L = new System.Collections.Generic.List<string[]>
                {
                    new[] { "1",  "X000", "DI0", "00", "START BUTTON",            "OFF" },
                    new[] { "2",  "X001", "DI0", "01", "STOP BUTTON",             "OFF" },
                    new[] { "3",  "X002", "DI0", "02", "RESET BUTTON",            "OFF" },
                    new[] { "4",  "X003", "DI0", "03", "EMG",                     "OFF" },
                    new[] { "5",  "X004", "DI0", "04", "OP EMG ON",               "OFF" },
                    new[] { "6",  "X007", "DI0", "07", "MAIN CDA 1 CHECK",        "ON"  },
                    new[] { "7",  "X008", "DI0", "08", "MAIN CDA 2 CHECK",        "ON"  },
                    new[] { "8",  "X009", "DI0", "09", "MAIN VACUUM 1 CHECK",     "ON"  },
                    new[] { "9",  "X010", "DI0", "10", "MAIN VACUUM 2 CHECK",     "ON"  },
                    new[] { "10", "X011", "DI0", "11", "MAIN VACUUM 3 CHECK",     "ON"  },
                    new[] { "11", "X012", "DI0", "12", "MAIN VACUUM 4 CHECK",     "ON"  },
                    new[] { "12", "X028", "DI0", "28", "WAFER FEEDER UP",         "OFF" },
                    new[] { "13", "X029", "DI0", "29", "WAFER FEEDER DOWN",       "ON"  },
                    new[] { "14", "X043", "DI1", "11", "FRONT PICKER CDA TANK",   "ON"  },
                    new[] { "15", "X044", "DI1", "12", "FRONT PICKER VAC TANK",   "ON"  },
                    new[] { "16", "X053", "DI1", "21", "REAR PICKER CDA TANK",    "ON"  },
                    new[] { "17", "X054", "DI1", "22", "REAR PICKER VAC TANK",    "ON"  },
                    new[] { "18", "X063", "DI1", "31", "NG BIN GUIDE UP",         "OFF" },
                    new[] { "19", "X064", "DI2", "00", "NG BIN GUIDE DOWN",       "ON"  },
                    new[] { "20", "X068", "DI2", "04", "GOOD BIN GUIDE UP",       "OFF" },
                    new[] { "21", "X069", "DI2", "05", "GOOD BIN GUIDE DOWN",     "ON"  },
                };
                // FRONT PICKER 0~7 FLOW CHECK
                for (int i = 0; i < 8; i++)
                    L.Add(new[] { (22+i).ToString(), "X" + (45+i).ToString("D3"), "DI1", (13+i).ToString("D2"), $"FRONT PICKER {i} FLOW", "OFF" });
                // REAR PICKER 0~7 FLOW CHECK
                for (int i = 0; i < 8; i++)
                    L.Add(new[] { (30+i).ToString(), "X" + (55+i).ToString("D3"), "DI1", (23+i).ToString("D2"), $"REAR PICKER {i} FLOW",  "OFF" });
                // DO
                L.Add(new[] { "38", "Y000", "DO0", "00", "START LAMP",       "OFF" });
                L.Add(new[] { "39", "Y001", "DO0", "01", "STOP LAMP",        "OFF" });
                L.Add(new[] { "40", "Y002", "DO0", "02", "RESET LAMP",       "OFF" });
                L.Add(new[] { "41", "Y003", "DO0", "03", "TL RED",           "OFF" });
                L.Add(new[] { "42", "Y004", "DO0", "04", "TL YELLOW",        "OFF" });
                L.Add(new[] { "43", "Y005", "DO0", "05", "TL GREEN",         "ON"  });
                L.Add(new[] { "44", "Y006", "DO0", "06", "BUZZER",           "OFF" });
                L.Add(new[] { "45", "Y016", "DO0", "16", "WAFER FEEDER UP",  "OFF" });
                L.Add(new[] { "46", "Y017", "DO0", "17", "WAFER FEEDER DOWN","OFF" });
                L.Add(new[] { "47", "Y026", "DO0", "26", "NG BIN GUIDE UP",  "OFF" });
                L.Add(new[] { "48", "Y027", "DO0", "27", "NG BIN GUIDE DOWN","OFF" });
                L.Add(new[] { "49", "Y032", "DO1", "00", "GOOD BIN GUIDE UP","OFF" });
                L.Add(new[] { "50", "Y033", "DO1", "01", "GOOD BIN GUIDE DOWN","OFF" });
                L.Add(new[] { "51", "Y044", "DO1", "12", "BOTTOM VISION BLOW ON", "OFF" });
                L.Add(new[] { "52", "Y046", "DO1", "14", "NEEDLE VACUUM",    "OFF" });
                for (int i = 0; i < 8; i++)
                    L.Add(new[] { (53+i).ToString(), "Y" + (48+i).ToString("D3"), "DO1", (16+i).ToString("D2"), $"FRONT PICKER {i} VACUUM", "OFF" });
                for (int i = 0; i < 8; i++)
                    L.Add(new[] { (61+i).ToString(), "Y" + (56+i).ToString("D3"), "DO1", (24+i).ToString("D2"), $"FRONT PICKER {i} BLOW",   "OFF" });
                for (int i = 0; i < 8; i++)
                    L.Add(new[] { (69+i).ToString(), "Y" + (64+i).ToString("D3"), "DO2", i.ToString("D2"),     $"REAR PICKER {i} VACUUM",   "OFF" });
                for (int i = 0; i < 8; i++)
                    L.Add(new[] { (77+i).ToString(), "Y" + (72+i).ToString("D3"), "DO2", (8+i).ToString("D2"), $"REAR PICKER {i} BLOW",     "OFF" });
                return L.ToArray();
            }
            public static string[][] Link() => new[]
            {
                new[] { "1", "Y016 WAFER FEEDER UP",   "X028 WAFER FEEDER UP",   "WAFER FEEDER UP cylinder" },
                new[] { "2", "Y017 WAFER FEEDER DOWN", "X029 WAFER FEEDER DOWN", "WAFER FEEDER DOWN cylinder" },
                new[] { "3", "Y026 NG BIN UP",         "X063 NG BIN GUIDE UP",   "NG BIN UP→GUIDE UP" },
                new[] { "4", "Y027 NG BIN DOWN",       "X064 NG BIN GUIDE DOWN", "NG BIN DOWN→GUIDE DOWN" },
                new[] { "5", "Y032 GOOD BIN UP",       "X068 GOOD BIN GUIDE UP", "GOOD BIN UP→GUIDE UP" },
                new[] { "6", "Y033 GOOD BIN DOWN",     "X069 GOOD BIN GUIDE DOWN", "GOOD BIN DOWN→GUIDE DOWN" },
                new[] { "7", "Y048 FRONT VAC0",        "X045 FRONT FLOW0",       "FRONT Picker 0 VAC→FLOW" },
                new[] { "8", "Y049 FRONT VAC1",        "X046 FRONT FLOW1",       "FRONT Picker 1 VAC→FLOW" },
            };
            public static string[][] Cylinder() => new[]
            {
                new[] { "1", "WAFER FEEDER UP/DOWN",  "Y016", "Y017", "X028", "X029", "DOWN" },
                new[] { "2", "NG BIN GUIDE",          "Y026", "Y027", "X063", "X064", "DOWN" },
                new[] { "3", "GOOD BIN GUIDE",        "Y032", "Y033", "X068", "X069", "DOWN" },
                new[] { "4", "BOTTOM VISION BLOW",    "Y044", "—",    "—",    "—",    "OFF" },
                new[] { "5", "NEEDLE VACUUM",         "Y046", "—",    "—",    "—",    "OFF" },
            };
            public static string[][] Lamp() => new[]
            {
                new[] { "1", "START LAMP", "Y000", "OFF" },
                new[] { "2", "STOP LAMP",  "Y001", "OFF" },
                new[] { "3", "RESET LAMP", "Y002", "OFF" },
                new[] { "4", "TL RED",     "Y003", "OFF" },
                new[] { "5", "TL YELLOW",  "Y004", "OFF" },
                new[] { "6", "TL GREEN",   "Y005", "ON"  },
                new[] { "7", "BUZZER",     "Y006", "OFF" },
            };
            public static string[][] Switch() => new[]
            {
                new[] { "1", "START SW",  "X000", "OFF" },
                new[] { "2", "STOP SW",   "X001", "OFF" },
                new[] { "3", "RESET SW",  "X002", "OFF" },
                new[] { "4", "EMG",       "X003", "OFF" },
                new[] { "5", "OP EMG ON", "X004", "OFF" },
            };
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
        }
    }
}
