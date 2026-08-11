using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
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
            LblSidebarHeader.BackColor = System.Drawing.Color.White;   // 작업정보 탭 헤더와 동일하게

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
            RemoveSettingsSidebarButton(BtnZoomLens);
            RemoveSettingsSidebarButton(BtnHeightSensor);
            RegisterSidebarButton(BtnSimulator,    "set.simulator",    en, () => new SimulatorLinkPage());
            RegisterSidebarButton(BtnVisionLink,   "set.visionLink",   en, () => new VisionLinkPage());

            // Self-Test 다이얼로그 버튼 — 페이지가 아닌 즉시 팝업
            RegisterSidebarButton(BtnSelfTest, "set.selfTest", en, () => new PlaceholderPage("set.selfTest"));
            BtnSelfTest.Click += (s, e) =>
            {
                var host = FindForm() as Form1;
                using (var dlg = new Dialogs.SystemSelfTestDialog(host))
                    ShowDialogCenteredOnContent(dlg);
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
                    ShowDialogCenteredOnContent(dlg);
            };

            // 숨김을 먼저 적용해야 CompactSettingsSidebar 의 좌표 계산이 숨긴 항목을 건너뛴다.
            HideUnimplementedSidebarButtons();
            CompactSettingsSidebar();
        }

        // [사이드바 정리 2026-08-10] 아직 사용하지 않는 사이드바 버튼을 화면에서만 숨긴다.
        //
        // - 등록(RegisterSidebarButton)은 그대로 두고 Visible 만 내린다.
        //   기능이 준비되면 아래 목록에서 해당 버튼만 빼면 배선 작업 없이 다시 나타난다.
        // - 이 탭은 FlowLayoutPanel 자동 배치가 아니라 CompactSettingsSidebar 가 좌표를 직접 계산한다.
        //   그 루프가 Visible=false 인 컨트롤을 건너뛰므로 빈칸은 생기지 않는다.
        // - 사용자 레벨 접근 제어(AccessControl)는 Enabled 만 조정하므로 이 숨김을 되돌리지 않는다.
        private void HideUnimplementedSidebarButtons()
        {
            Control[] hidden =
            {
                BtnSimulator,      // 시뮬레이터 연결
                BtnSelfTest,       // 자가 진단
                BtnAlarmMaster,    // 알람 마스터
                BtnTeach,          // 위치 티칭
                BtnRemoteViewer    // 원격 뷰어
            };

            foreach (Control control in hidden)
            {
                if (control != null)
                    control.Visible = false;
            }
        }

        private void ShowDialogCenteredOnContent(Form dialog)
        {
            if (dialog == null)
                return;

            Form owner = FindForm();
            CenterDialogOnContentBody(dialog);
            if (owner != null)
                dialog.ShowDialog(owner);
            else
                dialog.ShowDialog();
        }

        private void CenterDialogOnContentBody(Form dialog)
        {
            if (dialog == null || PnlContent == null || !PnlContent.IsHandleCreated)
                return;

            Rectangle bounds = PnlContent.RectangleToScreen(PnlContent.ClientRectangle);
            const int headerHeight = 30;
            if (bounds.Height > headerHeight)
            {
                bounds.Y += headerHeight;
                bounds.Height -= headerHeight;
            }

            int x = bounds.Left + ((bounds.Width - dialog.Width) / 2);
            int y = bounds.Top + ((bounds.Height - dialog.Height) / 2);

            Rectangle screen = Screen.FromControl(PnlContent).WorkingArea;
            x = Math.Max(screen.Left, Math.Min(x, screen.Right - dialog.Width));
            y = Math.Max(screen.Top, Math.Min(y, screen.Bottom - dialog.Height));

            dialog.StartPosition = FormStartPosition.Manual;
            dialog.Location = new Point(x, y);
        }

        private void RemoveSettingsSidebarButton(Control button)
        {
            if (button == null)
                return;

            PnlSidebarButtons.Controls.Remove(button);
            button.Visible = false;
        }

        private void CompactSettingsSidebar()
        {
            Control[] controls =
            {
                BtnGeneral,
                BtnMotion,
                BtnIoControl,
                BtnDigital,
                BtnCylinder,
                BtnLamp,
                BtnSwitch,
                BtnLightSource,
                PnlSecondarySeparator,
                BtnBarcode,
                BtnVisionLink,
                BtnAxisSetup,
                BtnCameraSetup,
                BtnLightSetup,

                // 이하 미구현(숨김) — HideUnimplementedSidebarButtons 목록과 같이 관리한다.
                // 지금은 Visible=false 라 아래 루프가 건너뛰고, 다시 보이게 하면 맨 아래에 배치된다.
                BtnSimulator,
                BtnSelfTest,
                BtnAlarmMaster,
                BtnTeach,
                BtnRemoteViewer
            };

            int y = 6;
            foreach (Control control in controls)
            {
                if (control == null || !control.Visible)
                    continue;

                if (control == PnlSecondarySeparator)
                {
                    y += 6;
                    control.Location = new Point(4, y);
                    control.Size = new Size(202, 2);
                    y += 8;
                    continue;
                }

                control.Location = new Point(4, y);
                control.Size = new Size(202, 46);
                y += 48;
            }
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
                new[] { "6", "FRONT SIDE VISION",       "COM3", "200" },
                new[] { "7", "REAR SIDE VISION",    "COM3", "200" },
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
                try
                {
                    BaseCylinder cylinder = item != null ? CylinderManager.Get(item.Name) : null;
                    if (cylinder != null)
                    {
                        string fwdLabel = "FWD";
                        string bwdLabel = "BWD";
                        CylinderItemSettings settings = CylinderSettingsStore.Get(item.Name);
                        if (settings != null)
                        {
                            if (!string.IsNullOrWhiteSpace(settings.FwdLabel)) fwdLabel = settings.FwdLabel.Trim();
                            if (!string.IsNullOrWhiteSpace(settings.BwdLabel)) bwdLabel = settings.BwdLabel.Trim();
                        }

                        if (cylinder.IsFwd && !cylinder.IsBwd) return fwdLabel;
                        if (!cylinder.IsFwd && cylinder.IsBwd) return bwdLabel;
                        if (cylinder.IsFwd && cylinder.IsBwd) return fwdLabel + "/" + bwdLabel;
                    }
                }
                catch
                {
                }

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
