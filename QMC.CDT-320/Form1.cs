using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT_320.Ui;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Tabs;

namespace QMC.CDT_320
{
    /// <summary>구현 설명 주석입니다.</summary>
    public enum MainTab
    {
        Work      = 0,
        WorkInfo  = 1,
        History   = 2,
        Recipe    = 3,
        Settings  = 4,
        User      = 5
    }

    public partial class Form1 : Form
    {
        internal CDT320_Machine    Machine        { get; private set; }
        internal SimulatorBridge   Bridge         { get; private set; }
        internal MachineController Controller     { get; private set; }
        internal MotionMonitorService MotionMonitor { get; private set; }
        internal AjinIoScanService IoScan { get; private set; }
        internal string CurrentRecipeName { get; private set; }

        /// <summary>구현 설명 주석입니다.</summary>
        ///
        public void RefreshProjectName(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName)) fileName = "-";
                CurrentRecipeName = NormalizeRecipeName(fileName);
                if (lblProjectValue.InvokeRequired)
                    lblProjectValue.Invoke((Action)(() => lblProjectValue.Text = fileName));
                else
                    lblProjectValue.Text = fileName;
            }
            catch { }
        }

        internal void LoadMachineSettings()
        {
            try
            {
                Machine?.LoadSettings();
                ApplyMotionAxisDataToMachine();
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    "NONE",
                    "DATA-LOAD",
                    "Machine settings load failed: " + ex.Message);
            }
            finally
            {
            }
        }

        internal void SaveMachineSettings()
        {
            try
            {
                if (Machine != null && !Machine.SaveSettings())
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        UserSession.Name,
                        "DATA-SAVE",
                        "Machine settings save returned false.");
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "DATA-SAVE",
                    "Machine settings save failed: " + ex.Message);
            }
            finally
            {
            }
        }

        internal void LoadMachineRecipe(string recipeName)
        {
            try
            {
                if (Machine == null || string.IsNullOrWhiteSpace(recipeName))
                    return;

                CurrentRecipeName = NormalizeRecipeName(recipeName);
                Machine.LoadRecipe(recipeName);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "DATA-LOAD",
                    "Machine recipe load failed: " + recipeName + " / " + ex.Message);
            }
            finally
            {
            }
        }

        internal void SaveMachineRecipe(string recipeName)
        {
            try
            {
                if (Machine == null || string.IsNullOrWhiteSpace(recipeName))
                    return;

                CurrentRecipeName = NormalizeRecipeName(recipeName);
                if (!Machine.SaveRecipe(recipeName))
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        UserSession.Name,
                        "DATA-SAVE",
                        "Machine recipe save returned false: " + recipeName);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "DATA-SAVE",
                    "Machine recipe save failed: " + recipeName + " / " + ex.Message);
            }
            finally
            {
            }
        }

        private static string NormalizeRecipeName(string recipeName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipeName))
                    return string.Empty;

                recipeName = recipeName.Trim();
                if (recipeName.EndsWith(".Project", StringComparison.OrdinalIgnoreCase))
                    recipeName = recipeName.Substring(0, recipeName.Length - ".Project".Length);

                return recipeName;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
            }
        }

        private void BeginSimulatorAutoConnect(AppSettings cfg)
        {
            try
            {
                if (cfg == null || Bridge == null)
                    return;

                if (cfg.UseAjin && AjinSystem.IsOpen && !cfg.SimulatorAutoConnect)
                    return;

                BeginInvoke(new Action(async () =>
                {
                    await AutoConnectSimulatorAsync(cfg);
                }));
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    UserSession.Name,
                    "SIM-CONNECT",
                    "Simulator auto-connect begin failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async System.Threading.Tasks.Task AutoConnectSimulatorAsync(AppSettings cfg)
        {
            try
            {
                if (cfg == null || Bridge == null)
                    return;

                string host = string.IsNullOrWhiteSpace(cfg.SimulatorHost)
                    ? "127.0.0.1"
                    : cfg.SimulatorHost.Trim();
                int port = cfg.SimulatorPort > 0 ? cfg.SimulatorPort : 7001;

                await Bridge.ConnectAsync(host, port);
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    UserSession.Name,
                    "SIM-CONNECT",
                    "Simulator connected: " + host + ":" + port);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    UserSession.Name,
                    "SIM-CONNECT",
                    "Simulator connect failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>구현 설명 주석입니다.</summary>
        internal QMC.CDT320.Sim.SimCassetteDriver CassetteDriver { get; private set; }
        /// <summary>구현 설명 주석입니다.</summary>
        internal QMC.CDT320.Secs.SecsHost SecsHost { get; private set; }

        private WorkTab     _workTab;
        private WorkInfoTab _workInfoTab;
        private HistoryTab  _historyTab;
        private RecipeTab   _recipeTab;
        private SettingsTab _settingsTab;
        private UserTab     _userTab;
        private AxisJogPopup _jogPopup;
        private AxisPositionPopup _axisPositionPopup;

        private MainTab _currentTab = MainTab.Work;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 구현 보조 주석입니다.
            var cfg = AppSettingsStore.Load();
            if (!string.IsNullOrEmpty(cfg.Language)) Lang.SetLanguage(cfg.Language);
            QMC.Common.Alarms.AlarmManager.LanguageProvider = () => Lang.Current ?? "ko";

            // 구현 보조 주석입니다.
            QMC.CDT320.Ajin.AjinConfigStore.Load();
            QMC.CDT320.Ajin.AjinFactory.UseRealBoard = cfg.UseAjin;
            if (cfg.UseAjin) 
                QMC.CDT320.Ajin.AjinSystem.Open(cfg.AjinIrqNo);

            QMC.CDT320.Ajin.AjinFactory.RegisterConfiguredAxes();

            // 구현 보조 주석입니다.
            // Stage 43 ??6 梨꾨꼸: Wafer/Inspection/Bin + Main/TopSide/BottomSide
            if (cfg.VisionAutoConnect)
            {
                _ = QMC.CDT320.VisionComm.VisionHub.ConnectAllAsync(
                    cfg.VisionHost,
                    cfg.VisionWaferPort, cfg.VisionInspectionPort, cfg.VisionBinPort,
                    cfg.VisionMainPort,  cfg.VisionTopSidePort,    cfg.VisionBottomSidePort);
            }
            QMC.CDT320.VisionComm.VisionHub.ConnectionChanged += OnVisionHubChanged;

            // 구현 보조 주석입니다.
            Machine    = new CDT320_Machine();
            LoadMachineSettings();
            Bridge     = new SimulatorBridge(Machine);
            BeginSimulatorAutoConnect(cfg);
            Controller = new MachineController(Machine);
            alarmBanner.ClearRequested += async (s, args) =>
            {
                try
                {
                    if (Controller != null)
                        await Controller.ResetAlarmAsync();
                    else
                        QMC.Common.Alarms.AlarmManager.ClearAll();
                }
                catch
                {
                    QMC.Common.Alarms.AlarmManager.ClearAll();
                }
                finally
                {
                }
            };
            MotionMonitor = new MotionMonitorService();
            MotionMonitor.Start(CurrentAxes(), cfg.UseAjin ? 50 : 100);
            IoScan = new AjinIoScanService();
            IoScan.Start(EnumerateAjinInputs(Machine), EnumerateAjinOutputs(Machine), cfg.UseAjin ? 10 : 100, () => AjinSystem.IsOpen);

            // 구현 보조 주석입니다.
            try
            {
                SecsHost = new QMC.CDT320.Secs.SecsHost(5000);
                Controller.SecsHost = SecsHost;
            }
            catch { /* Optional startup failure ignored. */ }

            // 구현 보조 주석입니다.
            if (!cfg.UseAjin)
            {
                CassetteDriver = new QMC.CDT320.Sim.SimCassetteDriver(Machine.InputLoader, Machine.OutputUnloader);
            }
            Controller.StatusChanged += OnMachineStatusChanged;
            Controller.LogMessage    += s => QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, UserSession.Name, "CTRL", s);
            // 구현 보조 주석입니다.
            if (Program.AutoCycleCount > 0)
                Controller.LogMessage += s => Console.WriteLine("[CTRL] " + s);

            // 구현 보조 주석입니다.
            _workTab     = new WorkTab     { Dock = DockStyle.Fill, Visible = false };
            _workInfoTab = new WorkInfoTab { Dock = DockStyle.Fill, Visible = false };
            _historyTab  = new HistoryTab  { Dock = DockStyle.Fill, Visible = false };
            _recipeTab   = new RecipeTab   { Dock = DockStyle.Fill, Visible = false };
            _settingsTab = new SettingsTab { Dock = DockStyle.Fill, Visible = false };
            _userTab     = new UserTab     { Dock = DockStyle.Fill, Visible = false };

            _workTab    .AttachHost(this);
            _workInfoTab.AttachHost(this);
            _historyTab .AttachHost(this);
            _recipeTab  .AttachHost(this);
            _settingsTab.AttachHost(this);
            _userTab    .AttachHost(this);

            pnlContent.Controls.Add(_workTab);
            pnlContent.Controls.Add(_workInfoTab);
            pnlContent.Controls.Add(_historyTab);
            pnlContent.Controls.Add(_recipeTab);
            pnlContent.Controls.Add(_settingsTab);
            pnlContent.Controls.Add(_userTab);

            // Bottom navigation i18n tags.
            btnTabWork    .Tag = "i18n:tab.work";
            btnTabWorkInfo.Tag = "i18n:tab.workInfo";
            btnTabHistory .Tag = "i18n:tab.history";
            btnTabRecipe  .Tag = "i18n:tab.recipe";
            btnTabSettings.Tag = "i18n:tab.settings";
            btnTabUser    .Tag = "i18n:tab.user";
            btnTabExit    .Tag = "i18n:tab.exit";

            // Status bar i18n tags.
            lblMapMode        .Tag = "i18n:status.mapEmpty";
            lblProjectCaption .Tag = "i18n:status.project";
            lblBarcodeCaption .Tag = "i18n:status.barcode";
            lblBinCaption     .Tag = "i18n:status.bin";
            lblVision         .Tag = "i18n:status.vision";
            lblPick           .Tag = "i18n:status.pick";
            lblReference      .Tag = "i18n:status.reference";

            // ?ㅻ뜑
            lblTitle          .Tag = "i18n:app.title";
            lblUserCaption    .Tag = "i18n:header.user";
            lblTimeCaption    .Tag = "i18n:header.time";

            // 구현 보조 주석입니다.
            Lang.LanguageChanged    += OnLocalizationChanged;
            UserSession.UserChanged += OnUserChanged;

            // 구현 보조 주석입니다.
            OnLocalizationChanged();

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
#if DEBUG
            QMC.CDT_320.Ui.Security.UserSession.ForceSet(
                "admin", QMC.CDT_320.Ui.Security.UserLevel.Admin);
#endif
            OnUserChanged();

            // 구현 보조 주석입니다.
            try
            {
                var last = QMC.CDT320.Recipes.RecipeStore.LoadLastOrDefault();
                if (last != null)
                {
                    LoadMachineRecipe(last.FileName);
                    Controller.ApplyRecipeMode(last);
                    // 구현 보조 주석입니다.
                    RefreshProjectName(last.FileName);
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "NONE",
                        "RECIPE-LOAD",
                        "Project loaded: " + last.FileName + ".Project (auto on startup)");
                }
            }
            catch { /* Optional startup failure ignored. */ }

            // 구현 보조 주석입니다.
            timerClock.Start();
            UpdateClock();

            // Default page.
            ShowTab(MainTab.Work);

            // 구현 보조 주석입니다.
            if (!string.IsNullOrEmpty(Program.StartPage))
            {
                BeginInvoke(new Action(() =>
                {
                    var pairs = new (MainTab tab, TabBase tb)[]
                    {
                        (MainTab.Work,     _workTab),
                        (MainTab.WorkInfo, _workInfoTab),
                        (MainTab.History,  _historyTab),
                        (MainTab.Recipe,   _recipeTab),
                        (MainTab.Settings, _settingsTab),
                        (MainTab.User,     _userTab),
                    };
                    foreach (var p in pairs)
                    {
                        if (p.tb.TryShowPage(Program.StartPage))
                        {
                            ShowTab(p.tab);
                            return;
                        }
                    }
                    // 구현 보조 주석입니다.
                    ShowTab(MainTab.Settings);
                }));
            }

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            if (Program.AuditAll)
            {
                BeginInvoke(new Action(() =>
                {
                    var pairs = new (MainTab tab, TabBase tb)[]
                    {
                        (MainTab.Work,     _workTab),
                        (MainTab.WorkInfo, _workInfoTab),
                        (MainTab.History,  _historyTab),
                        (MainTab.Recipe,   _recipeTab),
                        (MainTab.Settings, _settingsTab),
                        (MainTab.User,     _userTab),
                    };
                    foreach (var p in pairs)
                    {
                        try { ShowTab(p.tab); } catch { }
                        Application.DoEvents();
                        try { p.tb.ShowAllPagesOnce(); } catch { }
                        Application.DoEvents();
                    }

                    if (Program.ClickTestAll)
                    {
                        // 구현 보조 주석입니다.
                        int tt = 0, ss = 0, ff = 0;
                        foreach (var p in pairs)
                        {
                            try
                            {
                                var (t, s, f) = p.tb.PerformClickAllPages();
                                tt += t; ss += s; ff += f;
                            }
                            catch { }
                            Application.DoEvents();
                        }
                        try
                        {
                            QMC.Common.Logging.EventLogger.Write(
                                QMC.Common.Logging.EventKind.Event,
                                QMC.CDT_320.Ui.Security.UserSession.Name,
                                "UI-CLICK-TEST-SUMMARY",
                                "tried=" + tt + " success=" + ss + " failed=" + ff);
                        }
                        catch { }
                    }

                    try { ShowTab(MainTab.Work); } catch { }
                }));
            }

            // 구현 보조 주석입니다.
            if (Program.AutoCycleCount > 0 || Program.AutoInitOnly)
            {
                int n = Program.AutoCycleCount;
                bool initOnly = Program.AutoInitOnly;
                bool keepOpen = Program.AutoCycleKeepOpen || initOnly;
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(8000);
                    try { await Controller.InitAsync(); } catch { }
                    if (initOnly) return; // GUI 유지: 사용자가 CYCLE RUN을 직접 클릭
                    await System.Threading.Tasks.Task.Delay(2000);
                    try { await Controller.CycleRunAsync(n); } catch { }
                    await System.Threading.Tasks.Task.Delay(Program.AutoCycleEndDelayMs);
                    if (!keepOpen)
                    {
                        try { BeginInvoke(new Action(() => Close())); } catch { }
                    }
                });
            }
        }

        /// <summary>
        ///
        /// </summary>
        public void ShowTab(MainTab tab)
        {
            _currentTab = tab;

            _workTab    .Visible = tab == MainTab.Work;
            _workInfoTab.Visible = tab == MainTab.WorkInfo;
            _historyTab .Visible = tab == MainTab.History;
            _recipeTab  .Visible = tab == MainTab.Recipe;
            _settingsTab.Visible = tab == MainTab.Settings;
            _userTab    .Visible = tab == MainTab.User;

            btnTabWork    .Selected = tab == MainTab.Work;
            btnTabWorkInfo.Selected = tab == MainTab.WorkInfo;
            btnTabHistory .Selected = tab == MainTab.History;
            btnTabRecipe  .Selected = tab == MainTab.Recipe;
            btnTabSettings.Selected = tab == MainTab.Settings;
            btnTabUser    .Selected = tab == MainTab.User;

            // Apply focus, permission, and localization to the selected tab.
            Control active = null;
            switch (tab)
            {
                case MainTab.Work:     active = _workTab;     break;
                case MainTab.WorkInfo: active = _workInfoTab; break;
                case MainTab.History:  active = _historyTab;  break;
                case MainTab.Recipe:   active = _recipeTab;   break;
                case MainTab.Settings: active = _settingsTab; break;
                case MainTab.User:     active = _userTab;     break;
            }
            if (active != null)
            {
                Lang.Apply(active);
                AccessControl.Apply(active);
            }
        }

        private void RequestApplicationExit()
        {
            try
            {
                using (var dialog = new MessageBoxYesNo())
                {
                    DialogResult result = dialog.ShowDialog(
                        "종료",
                        "프로그램을 종료하시겠습니까?",
                        this,
                        new[] { "예", "아니오" });

                    if (result == DialogResult.Yes)
                    {
                        Log.Write("Main", UserSession.Name, "RequestApplicationExit", "Application exit requested by user. - Ok");
                        QMC.Common.Logging.EventLogger.Write(
                            QMC.Common.Logging.EventKind.Event,
                            UserSession.Name,
                            "APP-EXIT",
                            "Application exit requested.");
                        Close();
                    }
                    else
                    {
                        Log.Write("Main", UserSession.Name, "RequestApplicationExit", "Application exit requested by user. - canceled");
                        QMC.Common.Logging.EventLogger.Write(
                            QMC.Common.Logging.EventKind.Event,
                            UserSession.Name,
                            "APP-EXIT",
                            "Application exit canceled.");
                    }
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "APP-EXIT",
                    "Exit confirmation failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OnLocalizationChanged()
        {
            Lang.Apply(this);
            // 구현 보조 주석입니다.
        }

        private void OnUserChanged()
        {
            lblUserValue.Text = UserSession.Name + " (" + UserSession.Level + ")";
            RefreshStateBig();

            // Apply permission state.
            AccessControl.Apply(this);
        }

        private void OnMachineStatusChanged(QMC.CDT320.MachineStatus status)
        {
            if (InvokeRequired) { BeginInvoke(new Action<QMC.CDT320.MachineStatus>(OnMachineStatusChanged), status); return; }
            RefreshStateBig();
        }

        private void OnVisionHubChanged()
        {
            if (InvokeRequired) { BeginInvoke(new Action(OnVisionHubChanged)); return; }
            // 구현 보조 주석입니다.
            var h = QMC.CDT320.VisionComm.VisionHub.AllConnected ? "O" : "X";
            lblBarcodeValue.Text = "VIS " + h;
            lblBarcodeValue.ForeColor = QMC.CDT320.VisionComm.VisionHub.AllConnected
                                            ? System.Drawing.Color.LightGreen
                                            : System.Drawing.Color.White;
        }

        private void RefreshStateBig()
        {
            var ms = Controller?.Status ?? QMC.CDT320.MachineStatus.Idle;
            lblStateBig.Text = ms.ToString().ToUpper();
            System.Drawing.Color c;
            switch (ms)
            {
                case QMC.CDT320.MachineStatus.Ready:       c = System.Drawing.Color.LightGreen;  break;
                case QMC.CDT320.MachineStatus.Running:     c = System.Drawing.Color.DeepSkyBlue; break;
                case QMC.CDT320.MachineStatus.Cycling:     c = UiTheme.LogoOrange;              break;
                case QMC.CDT320.MachineStatus.Initializing:c = System.Drawing.Color.Gold;        break;
                case QMC.CDT320.MachineStatus.Alarm:       c = System.Drawing.Color.IndianRed;   break;
                case QMC.CDT320.MachineStatus.Stopped:     c = System.Drawing.Color.Silver;      break;
                default:                                   c = System.Drawing.Color.White;       break;
            }
            lblStateBig.ForeColor = c;
        }

        private void TimerClock_Tick(object sender, EventArgs e) => UpdateClock();
        private void UpdateClock()
        {
            lblTimeValue.Text = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
        }

        private void ShowOrRestoreJogPopup(IWin32Window owner)
        {
            if (_jogPopup == null || _jogPopup.IsDisposed)
            {
                _jogPopup = new AxisJogPopup(CurrentAxes())
                {
                    StartPosition = FormStartPosition.CenterScreen,
                    ShowInTaskbar = true,
                    Owner = null
                };
                _jogPopup.Load += (s, e) => TaskbarHelper.SetAppId(_jogPopup.Handle, "CDT320.JogPanel");
                _jogPopup.FormClosed += (s, e) => { _jogPopup = null; };
                _jogPopup.FormClosing += (s, ev) =>
                {
                    if (ev.CloseReason == CloseReason.UserClosing)
                    {
                        ev.Cancel = true;
                        _jogPopup.Hide();
                    }
                };
            }

            ShowPopup(_jogPopup);
        }

        private void ShowOrRestoreAxisPositionPopup(IWin32Window owner)
        {
            if (_axisPositionPopup == null || _axisPositionPopup.IsDisposed)
            {
                _axisPositionPopup = new AxisPositionPopup(CurrentAxes(), MotionMonitor)
                {
                    StartPosition = FormStartPosition.CenterScreen,
                    ShowInTaskbar = true,
                    Owner = null
                };
                _axisPositionPopup.Load += (s, e) => TaskbarHelper.SetAppId(_axisPositionPopup.Handle, "CDT320.AxisPosition");
                _axisPositionPopup.FormClosed += (s, e) => { _axisPositionPopup = null; };
                _axisPositionPopup.FormClosing += (s, ev) =>
                {
                    if (ev.CloseReason == CloseReason.UserClosing)
                    {
                        ev.Cancel = true;
                        _axisPositionPopup.Hide();
                    }
                };
            }

            ShowPopup(_axisPositionPopup);
        }

        private static void ShowPopup(Form popup)
        {
            if (popup == null) return;
            if (!popup.Visible) popup.Show();
            if (popup.WindowState == FormWindowState.Minimized)
                popup.WindowState = FormWindowState.Normal;
            popup.BringToFront();
            popup.TopMost = true;
            popup.Activate();
        }

        private List<BaseAxis> CurrentAxes()
        {
            try
            {
                return AjinAxisRegistry.GetOrderedAxes(Machine);
            }
            catch
            {
                return AjinAxisRegistry.GetOrderedAxes();
            }
            finally
            {
            }
        }

        private void ApplyMotionAxisDataToMachine()
        {
            try
            {
                if (Machine == null) return;
                QMC.CDT320.Ajin.AjinAxisRegistry.GetOrderedAxes(Machine);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    "NONE",
                    "AXIS-DATA-APPLY",
                    "Motion axis data apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static IEnumerable<BaseAxis> EnumerateAxes(CDT320_Machine machine)
        {
            if (machine == null) yield break;
            foreach (var unit in machine.Units)
                foreach (var axis in EnumerateAxes(unit))
                    yield return axis;
        }

        private static IEnumerable<AjinDigitalInput> EnumerateAjinInputs(CDT320_Machine machine)
        {
            if (machine == null) yield break;
            foreach (var unit in machine.Units)
                foreach (var input in EnumerateAjinInputs(unit))
                    yield return input;
        }

        private static IEnumerable<AjinDigitalOutput> EnumerateAjinOutputs(CDT320_Machine machine)
        {
            if (machine == null) yield break;
            foreach (var unit in machine.Units)
                foreach (var output in EnumerateAjinOutputs(unit))
                    yield return output;
        }

        private static IEnumerable<BaseAxis> EnumerateAxes(BaseEquipmentNode node)
        {
            if (node == null) yield break;

            var axis = node as BaseAxis;
            if (axis != null)
            {
                yield return axis;
                yield break;
            }

            var prop = node.GetType().GetProperty("Components");
            if (prop == null) yield break;

            var components = prop.GetValue(node) as System.Collections.IEnumerable;
            if (components == null) yield break;

            foreach (BaseEquipmentNode child in components)
                foreach (var childAxis in EnumerateAxes(child))
                    yield return childAxis;
        }

        private static IEnumerable<AjinDigitalInput> EnumerateAjinInputs(BaseEquipmentNode node)
        {
            if (node == null) yield break;

            var input = node as AjinDigitalInput;
            if (input != null)
            {
                yield return input;
                yield break;
            }

            var cylinder = node as BaseCylinder;
            if (cylinder != null)
            {
                var inFwd = cylinder.InFwd as AjinDigitalInput;
                var inBwd = cylinder.InBwd as AjinDigitalInput;
                if (inFwd != null) yield return inFwd;
                if (inBwd != null) yield return inBwd;
                yield break;
            }

            var prop = node.GetType().GetProperty("Components");
            if (prop == null) yield break;

            var components = prop.GetValue(node) as System.Collections.IEnumerable;
            if (components == null) yield break;

            foreach (BaseEquipmentNode child in components)
                foreach (var childInput in EnumerateAjinInputs(child))
                    yield return childInput;
        }

        private static IEnumerable<AjinDigitalOutput> EnumerateAjinOutputs(BaseEquipmentNode node)
        {
            if (node == null) yield break;

            var output = node as AjinDigitalOutput;
            if (output != null)
            {
                yield return output;
                yield break;
            }

            var cylinder = node as BaseCylinder;
            if (cylinder != null)
            {
                var outFwd = cylinder.OutFwd as AjinDigitalOutput;
                var outBwd = cylinder.OutBwd as AjinDigitalOutput;
                if (outFwd != null) yield return outFwd;
                if (outBwd != null) yield return outBwd;
                yield break;
            }

            var prop = node.GetType().GetProperty("Components");
            if (prop == null) yield break;

            var components = prop.GetValue(node) as System.Collections.IEnumerable;
            if (components == null) yield break;

            foreach (BaseEquipmentNode child in components)
                foreach (var childOutput in EnumerateAjinOutputs(child))
                    yield return childOutput;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                AppSettingsStore.Current.Language = Lang.Current;
                AppSettingsStore.Save();
            }
            catch { }
            SaveMachineSettings();
            try { MotionMonitor?.Dispose(); } catch { }
            try { IoScan?.Dispose(); } catch { }
            try { if (_jogPopup != null && !_jogPopup.IsDisposed) _jogPopup.Dispose(); } catch { }
            try { if (_axisPositionPopup != null && !_axisPositionPopup.IsDisposed) _axisPositionPopup.Dispose(); } catch { }
            try { Bridge?.Dispose(); } catch { }
            try { QMC.CDT320.VisionComm.VisionHub.DisconnectAll(); } catch { }
            try { QMC.CDT320.Ajin.AjinSystem.Close(); } catch { }
            Lang.LanguageChanged    -= OnLocalizationChanged;
            UserSession.UserChanged -= OnUserChanged;
            base.OnFormClosing(e);
        }
    }
}

