using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Tabs;

namespace QMC.CDT_320
{
    /// <summary>하단 메인 탭 종류입니다.</summary>
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
        internal OperationPanelMonitorService OpPanelMonitor { get; private set; }
        internal QMC.CDT320.Alarms.AlarmResponseService AlarmResponse { get; private set; }
        internal string CurrentRecipeName { get; private set; }
        private QMC.CDT320.Recipes.RecipeProject _currentRecipe;
        private readonly Dictionary<object, bool> _unitDryRunOverrides = new Dictionary<object, bool>();
        private bool _materialSnapshotRestored;
        private bool _applicationExitRequested;
        private bool _topDoorClosed = true;

        /// <summary>상단 프로젝트명을 현재 레시피 파일명으로 갱신합니다.</summary>
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
                QMC.CDT320.Ajin.CylinderManager.ApplySettings();
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

        internal void ApplyRuntimeMode()
        {
            try
            {
                var cfg = AppSettingsStore.Current ?? AppSettingsStore.Load();
                bool bypassHardware = cfg.BypassHardware;
                AjinFactory.UseRealBoard = cfg.UseAjin && !bypassHardware;
                if (AjinFactory.UseRealBoard && !AjinSystem.IsOpen)
                    AjinSystem.Open(cfg.AjinIrqNo);

                bool forceSimulation = bypassHardware || !AjinFactory.IsRealBoardReady;
                bool dryRun = cfg.DryRunMode && !forceSimulation;
                bool dataBypass = cfg.DryRunMode || forceSimulation;

                if (Machine != null)
                {
                    ApplyUnitDryRunMode(Machine, dataBypass);

                    List<BaseAxis> axes = CurrentAxes();
                    if (forceSimulation)
                    {
                        foreach (BaseAxis axis in axes)
                        {
                            if (axis == null || axis.Config == null) continue;
                            axis.Config.IsSimulationMode = true;
                        }
                    }
                    else
                    {
                        AjinFactory.ApplyPersistedAxisValues(axes);
                    }

                    foreach (BaseDigitalInput input in EnumerateInputs(Machine))
                    {
                        if (forceSimulation)
                            AjinFactory.ApplyInputSimulation(input, true);
                        else if (dryRun)
                            AjinFactory.ApplyInputDryRun(input, true);
                        else
                            AjinFactory.ApplyInputPersistedSimulation(input);
                    }

                    foreach (BaseDigitalOutput output in EnumerateOutputs(Machine))
                    {
                        if (forceSimulation || dryRun)
                            AjinFactory.ApplyOutputSimulation(output, forceSimulation);
                        else
                            AjinFactory.ApplyOutputPersistedSimulation(output);
                    }

                    foreach (BaseCylinder cylinder in EnumerateCylinders(Machine))
                    {
                        if (forceSimulation)
                            AjinFactory.ApplyCylinderSimulation(cylinder, true);
                        else if (dryRun)
                            AjinFactory.ApplyCylinderDryRun(cylinder, true);
                        else
                            QMC.CDT320.Ajin.CylinderSettingsStore.Apply(cylinder);
                    }

                    if (!forceSimulation && !dryRun)
                        ApplyUnitLocalRuntimeModes(Machine);
                }

                if (Controller != null)
                {
                    Controller.GlobalDryRun = dataBypass;
                    Controller.DryRun = dataBypass || (_currentRecipe != null && _currentRecipe.DryRun);
                }

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    UserSession.Name,
                    "RUNTIME-MODE",
                    $"Simulation={cfg.SimulationMode}, DryRun={cfg.DryRunMode}, UseAjin={cfg.UseAjin}, HardwareBypass={bypassHardware}");
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    UserSession.Name,
                    "RUNTIME-MODE",
                    "Runtime mode apply failed: " + ex.Message);
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
                _currentRecipe = QMC.CDT320.Recipes.RecipeStore.Load(CurrentRecipeName);
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

        /// <summary>시뮬레이션 카세트 드라이버입니다.</summary>
        internal QMC.CDT320.Sim.SimCassetteDriver CassetteDriver { get; private set; }
        /// <summary>SECS/HSMS Host 통신 객체입니다.</summary>
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
            WireShellNavigationEvents();
        }

        private void WireShellNavigationEvents()
        {
            btnTabWork.Click += (s, e) => ShowTab(MainTab.Work);
            btnTabWorkInfo.Click += (s, e) => ShowTab(MainTab.WorkInfo);
            btnTabHistory.Click += (s, e) => ShowTab(MainTab.History);
            btnTabRecipe.Click += (s, e) => ShowTab(MainTab.Recipe);
            btnAxisJog.Click += (s, e) => ShowOrRestoreJogPopup(this);
            btnAxisPosition.Click += (s, e) => ShowOrRestoreAxisPositionPopup(this);
            btnTabSettings.Click += (s, e) => ShowTab(MainTab.Settings);
            btnTabUser.Click += (s, e) => ShowTab(MainTab.User);
            btnTabExit.Click += (s, e) => RequestApplicationExit();
            btnDoorToggle.Click += (s, e) => ToggleDoorSimulationState();
            btnBuzzerStop.Click += (s, e) => StopBuzzerFromTopButton();
            UpdateTopCommandButtons();
        }

        private void ToggleDoorSimulationState()
        {
            _topDoorClosed = !_topDoorClosed;
            ApplyDoorSimulationState(_topDoorClosed);
            UpdateDoorToggleButton();

            QMC.Common.Logging.EventLogger.Write(
                QMC.Common.Logging.EventKind.Event,
                UserSession.Name,
                "TOP-DOOR",
                _topDoorClosed ? "Door simulation state changed: CLOSE" : "Door simulation state changed: OPEN");
        }

        private void StopBuzzerFromTopButton()
        {
            try
            {
                if (OpPanelMonitor != null)
                    OpPanelMonitor.StopBuzzer();
                else
                    Machine?.OpPanelUnit?.Buzzer?.Off();

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    UserSession.Name,
                    "TOP-BUZZER",
                    "Buzzer stop requested from top button.");
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    UserSession.Name,
                    "TOP-BUZZER",
                    "Buzzer stop failed: " + ex.Message);
            }
        }

        private void ApplyDoorSimulationState(bool closed)
        {
            if (Machine == null)
                return;

            int total = 0;
            int applied = 0;
            foreach (BaseDigitalInput input in EnumerateInputs(Machine))
            {
                if (!IsDoorCheckInput(input))
                    continue;

                total++;
                if (input.Config != null && input.Config.IsSimulationMode)
                {
                    input.SimulateInput(closed);
                    applied++;
                }
            }

            if (total == 0)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    UserSession.Name,
                    "TOP-DOOR",
                    "Door check inputs were not found.");
            }
            else if (applied == 0)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    UserSession.Name,
                    "TOP-DOOR",
                    "Door check inputs are hardware mode. Simulation injection skipped.");
            }
        }

        private static bool IsDoorCheckInput(BaseDigitalInput input)
        {
            if (input == null || string.IsNullOrEmpty(input.Name))
                return false;

            return string.Equals(input.Name, "LeftDoorCheck", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(input.Name, "RearDoorCheck", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(input.Name, "RightDoorCheck", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(input.Name, "WaferLifterDoorCheck", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(input.Name, "BinLifterDoorCheck", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateTopCommandButtons()
        {
            UpdateDoorToggleButton();
            btnBuzzerStop.BackColor = Color.FromArgb(92, 64, 34);
            btnBuzzerStop.ForeColor = Color.White;
        }

        private void UpdateDoorToggleButton()
        {
            if (btnDoorToggle == null)
                return;

            if (_topDoorClosed)
            {
                btnDoorToggle.Text = "DOOR\r\nCLOSE";
                btnDoorToggle.BackColor = Color.FromArgb(48, 92, 76);
                btnDoorToggle.FlatAppearance.BorderColor = Color.FromArgb(110, 150, 136);
            }
            else
            {
                btnDoorToggle.Text = "DOOR\r\nOPEN";
                btnDoorToggle.BackColor = Color.FromArgb(122, 46, 46);
                btnDoorToggle.FlatAppearance.BorderColor = Color.FromArgb(210, 90, 74);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            var cfg = AppSettingsStore.Load();
            if (!string.IsNullOrEmpty(cfg.Language)) Lang.SetLanguage(cfg.Language);
            QMC.Common.Alarms.AlarmManager.LanguageProvider = () => Lang.Current ?? "ko";

            QMC.CDT320.Ajin.AjinConfigStore.Load();
            QMC.CDT320.Ajin.AjinFactory.UseRealBoard = cfg.UseAjin && !cfg.BypassHardware;
            if (QMC.CDT320.Ajin.AjinFactory.UseRealBoard) 
                QMC.CDT320.Ajin.AjinSystem.Open(cfg.AjinIrqNo);
            QMC.CDT320.Ajin.IoSettingsStore.Load();
            QMC.CDT320.Ajin.CylinderManager.Initialize();

            QMC.CDT320.Ajin.AjinFactory.RegisterConfiguredAxes();

            // Stage 43 ??6 梨꾨꼸: Wafer/Inspection/Bin + Main/TopSide/BottomSide
            if (cfg.VisionAutoConnect)
            {
                _ = QMC.CDT320.VisionComm.VisionHub.ConnectAllAsync(
                    cfg.VisionHost,
                    cfg.VisionWaferPort, cfg.VisionInspectionPort, cfg.VisionBinPort,
                    cfg.VisionMainPort,  cfg.VisionTopSidePort,    cfg.VisionBottomSidePort);
            }
            QMC.CDT320.VisionComm.VisionHub.ConnectionChanged += OnVisionHubChanged;

            Machine    = new CDT320_Machine();
            LoadMachineSettings();
            ApplyRuntimeMode();
            Bridge     = new SimulatorBridge(Machine);
            BeginSimulatorAutoConnect(cfg);
            Controller = new MachineController(Machine);
            ApplyRuntimeMode();
            Controller.ApplyStartupMachineRuntimeState(cfg);
            AlarmResponse = new QMC.CDT320.Alarms.AlarmResponseService(Controller);
            AlarmResponse.Start();
            PromptMaterialRecoveryOnStartup();
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
            MotionMonitor.Start(CurrentAxes(), QMC.CDT320.Ajin.AjinFactory.UseRealBoard ? 50 : 100);
            IoScan = new AjinIoScanService();
            IoScan.Start(EnumerateInputs(Machine), EnumerateOutputs(Machine), QMC.CDT320.Ajin.AjinFactory.UseRealBoard ? 10 : 100, () => !AppSettingsStore.Current.BypassHardware && AjinSystem.IsOpen);
            OpPanelMonitor = new OperationPanelMonitorService(Machine, Controller);
            OpPanelMonitor.Start();
            ApplyDoorSimulationState(_topDoorClosed);
            UpdateTopCommandButtons();

            try
            {
                SecsHost = new QMC.CDT320.Secs.SecsHost(5000);
                Controller.SecsHost = SecsHost;
            }
            catch { /* Optional startup failure ignored. */ }

            if (!cfg.UseAjin)
            {
                CassetteDriver = new QMC.CDT320.Sim.SimCassetteDriver(
                    Machine.InputCassetteUnit,
                    Machine.InputFeederUnit,
                    Machine.OutputCassetteUnit,
                    Machine.OutputFeederUnit);
            }
            Controller.StatusChanged += OnEquipmentStatusChanged;
            Controller.LogMessage    += s => QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, UserSession.Name, "CTRL", s);
            if (Program.AutoCycleCount > 0)
                Controller.LogMessage += s => Console.WriteLine("[CTRL] " + s);

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

            Lang.LanguageChanged    += OnLocalizationChanged;
            UserSession.UserChanged += OnUserChanged;

            OnLocalizationChanged();

#if DEBUG
            QMC.CDT_320.Ui.Security.UserSession.ForceSet(
                "admin", QMC.CDT_320.Ui.Security.UserLevel.Admin);
#endif
            OnUserChanged();

            try
            {
                var last = QMC.CDT320.Recipes.RecipeStore.LoadLastOrDefault();
                if (last != null)
                {
                    LoadMachineRecipe(last.FileName);
                    _currentRecipe = last;
                    Controller.ApplyRecipeMode(last);
                    if (!_materialSnapshotRestored)
                        InitializeMaterialStateFromRecipe(last);
                    RefreshProjectName(last.FileName);
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "NONE",
                        "RECIPE-LOAD",
                        "Project loaded: " + last.FileName + ".Project (auto on startup)");
                }
            }
            catch { /* Optional startup failure ignored. */ }

            if (!_materialSnapshotRestored && MaterialStorage.State.Cassettes.Count == 0)
                MaterialStateService.InitializeForRecipe(1, 1, 25, 25);

            timerClock.Start();
            UpdateClock();

            // Default page.
            ShowTab(MainTab.Work);

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
                    ShowTab(MainTab.Settings);
                }));
            }

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

        private void PromptMaterialRecoveryOnStartup()
        {
            try
            {
                if (!MaterialSnapshotStore.Exists())
                {
                    Log.Write("Main", UserSession.Name, "MaterialRecovery", "Material snapshot does not exist. New empty Material state will be created. - Ok");
                    MaterialStateService.InitializeForRecipe(1, 1, 25, 25);
                    return;
                }

                var snapshot = MaterialSnapshotStore.Load();
                if (snapshot == null)
                {
                    Log.Write("Main", UserSession.Name, "MaterialRecovery", "Material snapshot load failed. New empty Material state will be created. - Failed");
                    QMC.Common.MessageDialog.Show(
                        this,
                        "저장된 Material 정보를 불러오지 못했습니다.\r\n새 Material 상태로 초기화합니다.\r\n\r\nFile: " + MaterialSnapshotStore.SnapshotPath,
                        "Material Recovery",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    MaterialStateService.InitializeForRecipe(1, 1, 25, 25);
                    return;
                }

                string savedAt = snapshot != null ? snapshot.SavedAt.ToString("yyyy-MM-dd HH:mm:ss") : "unknown";
                string lot = ResolveMaterialSnapshotLotId(snapshot);
                string recipe = ResolveMaterialSnapshotRecipeName(snapshot);
                string snapshotFileName = System.IO.Path.GetFileName(MaterialSnapshotStore.SnapshotPath);

                var message =
                    "이전에 저장된 Material 정보가 있습니다.\r\n\r\n" +
                    "저장 시간: " + savedAt + "\r\n" +
                    "Recipe: " + (string.IsNullOrEmpty(recipe) ? "-" : recipe) + "\r\n" +
                    "Lot: " + (string.IsNullOrEmpty(lot) ? "-" : lot) + "\r\n" +
                    "File: " + (string.IsNullOrEmpty(snapshotFileName) ? "-" : snapshotFileName) + "\r\n" +
                    "Path: " + MaterialSnapshotStore.SnapshotPath + "\r\n\r\n" +
                    "[예] 기존 Material 정보를 사용합니다.\r\n" +
                    "[아니오] 초기화 후 새 Material 상태로 시작합니다.";

                Log.Write("Main", UserSession.Name, "MaterialRecovery", "Material snapshot exists. Asking user to restore or initialize. - Start");
                var result = QMC.Common.MessageDialog.Show(
                    this,
                    message,
                    "Material Recovery",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    _materialSnapshotRestored = MaterialStorage.RestoreLastSnapshot();
                    if (!_materialSnapshotRestored)
                    {
                        Log.Write("Main", UserSession.Name, "MaterialRecovery", "Material snapshot restore failed. New empty Material state will be created. - Failed");
                        QMC.Common.MessageDialog.Show(
                            this,
                            "Material 정보 복구에 실패했습니다.\r\n새 Material 상태로 초기화합니다.\r\n\r\nFile: " + MaterialSnapshotStore.SnapshotPath,
                            "Material Recovery",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        MaterialStateService.InitializeForRecipe(1, 1, 25, 25);
                        return;
                    }

                    Log.Write("Main", UserSession.Name, "MaterialRecovery", "Material snapshot restored by user. - Ok");
                    return;
                }

                Log.Write("Main", UserSession.Name, "MaterialRecovery", "Material snapshot ignored by user. New empty Material state will be created. - Ok");
                MaterialStateService.InitializeForRecipe(1, 1, 25, 25);
            }
            catch (Exception ex)
            {
                Log.Write("Main", UserSession.Name, "MaterialRecovery", "Material recovery prompt failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(
                    this,
                    "Material 복구 선택 처리 중 오류가 발생했습니다.\r\n새 Material 상태로 초기화합니다.\r\n\r\n" + ex.Message,
                    "Material Recovery",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                MaterialStateService.InitializeForRecipe(1, 1, 25, 25);
            }
            finally
            {
            }
        }

        private static string ResolveMaterialSnapshotRecipeName(MaterialSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                    return "";

                if (!string.IsNullOrWhiteSpace(snapshot.RecipeName))
                    return snapshot.RecipeName.Trim();

                var project = QMC.CDT320.Recipes.RecipeStore.LoadLastOrDefault();
                return project != null && !string.IsNullOrWhiteSpace(project.FileName) ? project.FileName.Trim() : "";
            }
            catch
            {
                return "";
            }
            finally
            {
            }
        }

        private static string ResolveMaterialSnapshotLotId(MaterialSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                    return "";

                if (!string.IsNullOrWhiteSpace(snapshot.LotId))
                    return snapshot.LotId.Trim();

                if (snapshot.Cassettes != null)
                {
                    var cassette = snapshot.Cassettes.FirstOrDefault(c => c != null && !string.IsNullOrWhiteSpace(c.CassetteLotId));
                    if (cassette != null)
                        return cassette.CassetteLotId.Trim();
                }

                if (snapshot.Wafers != null)
                {
                    var wafer = snapshot.Wafers.FirstOrDefault(w => w != null && !string.IsNullOrWhiteSpace(w.CassetteLotId));
                    if (wafer != null)
                        return wafer.CassetteLotId.Trim();
                }
            }
            catch
            {
            }
            finally
            {
            }

            return "";
        }

        private void InitializeMaterialStateFromRecipe(QMC.CDT320.Recipes.RecipeProject recipe)
        {
            if (recipe == null) return;

            int inputLevels = recipe.InputCassetteLevelCount;
            int goodLevels = recipe.GoodCassetteLevelCount;
            if (inputLevels < 1 || inputLevels > 2) inputLevels = 1;
            if (goodLevels < 1 || goodLevels > 2) goodLevels = 1;

            MaterialStorage.InitializeDefaultState(inputLevels, goodLevels, 25, 25);
            MaterialStorage.State.RecipeName = recipe.FileName ?? "";
            MaterialStorage.State.LotId = recipe.LotId ?? "";
            MaterialStateService.NotifyAndSave("InitializeFromRecipe");
        }

        /// <summary>선택한 메인 탭을 화면에 표시합니다.</summary>
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
                // 작업 탭 활성화
                case MainTab.Work:     active = _workTab;     break;
                // 작업 정보 탭 활성화
                case MainTab.WorkInfo: active = _workInfoTab; break;
                // 이력 탭 활성화
                case MainTab.History:  active = _historyTab;  break;
                // 레시피 탭 활성화
                case MainTab.Recipe:   active = _recipeTab;   break;
                // 설정 탭 활성화
                case MainTab.Settings: active = _settingsTab; break;
                // 사용자 탭 활성화
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
                        _applicationExitRequested = true;
                        try
                        {
                            Close();
                        }
                        finally
                        {
                            if (!IsDisposed && !Disposing)
                            {
                                _applicationExitRequested = false;
                            }
                        }
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
            // 언어 변경 후 메인 폼 전체 표시 문구를 다시 적용합니다.
        }

        private void OnUserChanged()
        {
            lblUserValue.Text = UserSession.Name + " (" + UserSession.Level + ")";
            RefreshStateBig();

            // Apply permission state.
            AccessControl.Apply(this);
        }

        private void OnEquipmentStatusChanged(QMC.CDT320.EquipmentStatus status)
        {
            if (InvokeRequired) { BeginInvoke(new Action<QMC.CDT320.EquipmentStatus>(OnEquipmentStatusChanged), status); return; }
            RefreshStateBig();
        }

        private void OnVisionHubChanged()
        {
            if (InvokeRequired) { BeginInvoke(new Action(OnVisionHubChanged)); return; }
            // VisionHub 연결 상태를 상단 VIS 표시로 반영합니다.
            var h = QMC.CDT320.VisionComm.VisionHub.AllConnected ? "O" : "X";
            lblBarcodeValue.Text = "VIS " + h;
            lblBarcodeValue.ForeColor = QMC.CDT320.VisionComm.VisionHub.AllConnected
                                            ? System.Drawing.Color.LightGreen
                                            : System.Drawing.Color.White;
        }

        private void RefreshStateBig()
        {
            var ms = Controller?.Status ?? QMC.CDT320.EquipmentStatus.Idle;
            lblStateBig.Text = ms.ToString().ToUpper();
            System.Drawing.Color c;
            switch (ms)
            {
                // Ready 상태 색상
                case QMC.CDT320.EquipmentStatus.Ready:       c = System.Drawing.Color.LightGreen;  break;
                // ManualRunning 상태 색상
                case QMC.CDT320.EquipmentStatus.ManualRunning:     c = System.Drawing.Color.DeepSkyBlue; break;
                // AutoRunning 상태 색상
                case QMC.CDT320.EquipmentStatus.AutoRunning:     c = UiTheme.LogoOrange;              break;
                // Initializing 상태 색상
                case QMC.CDT320.EquipmentStatus.Initializing:c = System.Drawing.Color.Gold;        break;
                // Alarm 상태 색상
                case QMC.CDT320.EquipmentStatus.Alarm:       c = System.Drawing.Color.IndianRed;   break;
                // Stopped 상태 색상
                case QMC.CDT320.EquipmentStatus.Stopped:     c = System.Drawing.Color.Silver;      break;
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
                _jogPopup = new AxisJogPopup(CurrentAxes(), Machine)
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

        private void ApplyUnitLocalRuntimeModes(CDT320_Machine machine)
        {
            try
            {
                if (machine == null || machine.Units == null)
                    return;

                foreach (var unit in machine.Units)
                    ApplyNodeLocalRuntimeMode(unit, false, false);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    UserSession.Name,
                    "RUNTIME-MODE",
                    "Unit local mode apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ApplyNodeLocalRuntimeMode(BaseEquipmentNode node, bool parentSimulation, bool parentDryRun)
        {
            if (node == null)
                return;

            var binding = System.Reflection.BindingFlags.Instance |
                          System.Reflection.BindingFlags.Public |
                          System.Reflection.BindingFlags.NonPublic;

            object setup = GetPropertyValue(node, "Setup", binding);
            object config = GetPropertyValue(node, "Config", binding);
            bool hasDryRun = HasBoolProperty(config, "bDryRun", binding);
            bool localSimulation = GetBoolProperty(setup, "IsSimulationMode", binding) ||
                                   (!hasDryRun && GetBoolProperty(config, "IsSimulationMode", binding));
            bool localDryRun = GetBoolProperty(config, "bDryRun", binding);

            bool simulation = parentSimulation || localSimulation;
            bool dryRun = parentDryRun || localDryRun;

            BaseAxis axis = node as BaseAxis;
            if (axis != null)
            {
                if (axis.Config != null && (simulation || dryRun))
                    axis.Config.IsSimulationMode = simulation || axis is QMC.CDT320.SimAxis;
                return;
            }

            BaseDigitalInput input = node as BaseDigitalInput;
            if (input != null)
            {
                if (simulation)
                    AjinFactory.ApplyInputSimulation(input, true);
                else if (dryRun)
                    AjinFactory.ApplyInputDryRun(input, true);
                return;
            }

            BaseDigitalOutput output = node as BaseDigitalOutput;
            if (output != null)
            {
                if (simulation)
                    AjinFactory.ApplyOutputSimulation(output, true);
                else if (dryRun)
                    AjinFactory.ApplyOutputSimulation(output, false);
                return;
            }

            BaseCylinder cylinder = node as BaseCylinder;
            if (cylinder != null)
            {
                if (simulation)
                    AjinFactory.ApplyCylinderSimulation(cylinder, true);
                else if (dryRun)
                    AjinFactory.ApplyCylinderDryRun(cylinder, true);
                return;
            }

            var components = GetPropertyValue(node, "Components", binding) as System.Collections.IEnumerable;
            if (components == null)
                return;

            foreach (object child in components)
            {
                BaseEquipmentNode childNode = child as BaseEquipmentNode;
                if (childNode != null)
                    ApplyNodeLocalRuntimeMode(childNode, simulation, dryRun);
            }
        }

        private static object GetPropertyValue(object source, string propertyName, System.Reflection.BindingFlags binding)
        {
            if (source == null)
                return null;

            var prop = source.GetType().GetProperty(propertyName, binding);
            return prop != null ? prop.GetValue(source, null) : null;
        }

        private static bool HasBoolProperty(object source, string propertyName, System.Reflection.BindingFlags binding)
        {
            if (source == null)
                return false;

            var prop = source.GetType().GetProperty(propertyName, binding);
            return prop != null && prop.PropertyType == typeof(bool);
        }

        private static bool GetBoolProperty(object source, string propertyName, System.Reflection.BindingFlags binding)
        {
            if (source == null)
                return false;

            var prop = source.GetType().GetProperty(propertyName, binding);
            if (prop == null || prop.PropertyType != typeof(bool) || !prop.CanRead)
                return false;

            return (bool)prop.GetValue(source, null);
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

        private static IEnumerable<BaseDigitalInput> EnumerateInputs(CDT320_Machine machine)
        {
            if (machine == null) yield break;
            foreach (var unit in machine.Units)
                foreach (var input in EnumerateInputs(unit))
                    yield return input;
        }

        private void ApplyUnitDryRunMode(CDT320_Machine machine, bool dryRun)
        {
            try
            {
                if (machine == null || machine.Units == null)
                    return;

                foreach (var unit in machine.Units)
                    ApplyNodeDryRunMode(unit, dryRun);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    UserSession.Name,
                    "RUNTIME-MODE",
                    "Unit dry-run apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ApplyNodeDryRunMode(BaseEquipmentNode node, bool dryRun)
        {
            if (node == null) return;

            var binding = System.Reflection.BindingFlags.Instance |
                          System.Reflection.BindingFlags.Public |
                          System.Reflection.BindingFlags.NonPublic;
            var configProp = node.GetType().GetProperty("Config", binding);
            object config = configProp != null ? configProp.GetValue(node, null) : null;
            if (config != null)
            {
                var dryRunProp = config.GetType().GetProperty("bDryRun", binding);
                if (dryRunProp != null && dryRunProp.CanWrite && dryRunProp.PropertyType == typeof(bool))
                {
                    if (dryRun)
                    {
                        if (!_unitDryRunOverrides.ContainsKey(config))
                            _unitDryRunOverrides[config] = (bool)dryRunProp.GetValue(config, null);
                        dryRunProp.SetValue(config, true, null);
                    }
                    else
                    {
                        bool original;
                        if (_unitDryRunOverrides.TryGetValue(config, out original))
                        {
                            dryRunProp.SetValue(config, original, null);
                            _unitDryRunOverrides.Remove(config);
                        }
                    }
                }
            }

            var componentsProp = node.GetType().GetProperty("Components", binding);
            var components = componentsProp != null ? componentsProp.GetValue(node, null) as System.Collections.IEnumerable : null;
            if (components == null)
                return;

            foreach (object child in components)
            {
                BaseEquipmentNode childNode = child as BaseEquipmentNode;
                if (childNode != null)
                    ApplyNodeDryRunMode(childNode, dryRun);
            }
        }

        private static IEnumerable<BaseDigitalOutput> EnumerateOutputs(CDT320_Machine machine)
        {
            if (machine == null) yield break;
            foreach (var unit in machine.Units)
                foreach (var output in EnumerateOutputs(unit))
                    yield return output;
        }

        private static IEnumerable<BaseCylinder> EnumerateCylinders(CDT320_Machine machine)
        {
            if (machine == null) yield break;
            foreach (var unit in machine.Units)
                foreach (var cylinder in EnumerateCylinders(unit))
                    yield return cylinder;
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

        private static IEnumerable<BaseDigitalInput> EnumerateInputs(BaseEquipmentNode node)
        {
            if (node == null) yield break;

            var input = node as BaseDigitalInput;
            if (input != null)
            {
                yield return input;
                yield break;
            }

            var cylinder = node as BaseCylinder;
            if (cylinder != null)
            {
                if (cylinder.InFwd != null) yield return cylinder.InFwd;
                if (cylinder.InBwd != null) yield return cylinder.InBwd;
                yield break;
            }

            var prop = node.GetType().GetProperty("Components");
            if (prop == null) yield break;

            var components = prop.GetValue(node) as System.Collections.IEnumerable;
            if (components == null) yield break;

            foreach (BaseEquipmentNode child in components)
                foreach (var childInput in EnumerateInputs(child))
                    yield return childInput;
        }

        private static IEnumerable<BaseDigitalOutput> EnumerateOutputs(BaseEquipmentNode node)
        {
            if (node == null) yield break;

            var output = node as BaseDigitalOutput;
            if (output != null)
            {
                yield return output;
                yield break;
            }

            var cylinder = node as BaseCylinder;
            if (cylinder != null)
            {
                if (cylinder.OutFwd != null) yield return cylinder.OutFwd;
                if (cylinder.OutBwd != null) yield return cylinder.OutBwd;
                yield break;
            }

            var prop = node.GetType().GetProperty("Components");
            if (prop == null) yield break;

            var components = prop.GetValue(node) as System.Collections.IEnumerable;
            if (components == null) yield break;

            foreach (BaseEquipmentNode child in components)
                foreach (var childOutput in EnumerateOutputs(child))
                    yield return childOutput;
        }

        private static IEnumerable<BaseCylinder> EnumerateCylinders(BaseEquipmentNode node)
        {
            if (node == null) yield break;

            var cylinder = node as BaseCylinder;
            if (cylinder != null)
            {
                yield return cylinder;
                yield break;
            }

            var prop = node.GetType().GetProperty("Components");
            if (prop == null) yield break;

            var components = prop.GetValue(node) as System.Collections.IEnumerable;
            if (components == null) yield break;

            foreach (BaseEquipmentNode child in components)
                foreach (var childCylinder in EnumerateCylinders(child))
                    yield return childCylinder;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_applicationExitRequested)
            {
                e.Cancel = true;
                Log.Write("Main", UserSession.Name, "OnFormClosing", "Application close ignored. Use EXIT button.");
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    UserSession.Name,
                    "APP-CLOSE-BLOCKED",
                    "Application close ignored. Use EXIT button.");
                return;
            }

            try
            {
                AppSettingsStore.Current.Language = Lang.Current;
                AppSettingsStore.Save();
            }
            catch { }
            SaveMachineSettings();
            try { Controller?.SaveMachineRuntimeStateForApplicationClosing(); } catch { }
            try { AlarmResponse?.Dispose(); } catch { }
            try { OpPanelMonitor?.Dispose(); } catch { }
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

