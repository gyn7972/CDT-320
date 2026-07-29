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
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Tabs;
using QMC.CDT_320.Ui.Util;

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
        internal QMC.CDT320.Interlocks.RealtimeCollisionSupervisor CollisionSupervisor { get; private set; }
        internal AjinIoScanService IoScan { get; private set; }
        internal OperationPanelMonitorService OpPanelMonitor { get; private set; }
        internal QMC.CDT320.Alarms.AlarmResponseService AlarmResponse { get; private set; }
        /// <summary>현재 장비에 실제로 적용된 활성 Recipe 이름입니다.</summary>
        internal string ActiveRecipeName { get; private set; }

        private QMC.CDT320.Recipes.RecipeProject _currentRecipe;
        private readonly Dictionary<object, bool> _unitDryRunOverrides = new Dictionary<object, bool>();
        private bool _materialSnapshotRestored;
        private string _materialSnapshotLotIdBeforeInitialization = "";
        private bool _applicationExitRequested;
        private bool _materialStateSavedForExit;
        private bool _topDoorClosed = true;

        /// <summary>상단 프로젝트명을 현재 레시피 파일명으로 갱신합니다.</summary>
        public void RefreshProjectName(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName)) fileName = "-";
                ActiveRecipeName = NormalizeRecipeName(fileName);
                Controller?.SetActiveRecipeName(ActiveRecipeName);
                if (lblProjectValue.InvokeRequired)
                    lblProjectValue.Invoke((Action)(() => SetTextIfChanged(lblProjectValue, fileName)));
                else
                    SetTextIfChanged(lblProjectValue, fileName);
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

        internal bool SaveMachineSettings()
        {
            try
            {
                if (Machine == null)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        UserSession.Name,
                        "DATA-SAVE",
                        "Machine settings save failed: Machine is null.");
                    return false;
                }

                if (!Machine.SaveSettings())
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        UserSession.Name,
                        "DATA-SAVE",
                        "Machine settings save returned false.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "DATA-SAVE",
                    "Machine settings save failed: " + ex.Message);
                return false;
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
                    AjinFactory.ApplyPersistedAxisValues(axes);
                    if (forceSimulation)
                    {
                        foreach (BaseAxis axis in axes)
                        {
                            if (axis == null || axis.Config == null) continue;
                            axis.Config.IsSimulationMode = true;
                        }
                    }

                    foreach (BaseDigitalInput input in EnumerateInputs(Machine))
                    {
                        if (forceSimulation)
                            AjinFactory.ApplyInputSimulation(input, true);
                        else if (dryRun)
                            AjinFactory.ApplyInputDryRun(input, true);
                        else
                            AjinFactory.ApplyInputSimulation(input, false);
                    }

                    foreach (BaseDigitalOutput output in EnumerateOutputs(Machine))
                    {
                        if (forceSimulation || dryRun)
                            AjinFactory.ApplyOutputSimulation(output, forceSimulation);
                        else
                            AjinFactory.ApplyOutputSimulation(output, false);
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
                    $"Simulation={cfg.SimulationMode}, DryRun={cfg.DryRunMode}, UseAjin={cfg.UseAjin}, HardwareBypass={bypassHardware}, UseVision={cfg.UseVision}, UseRealVisionInSimulation={cfg.UseRealVisionInSimulation}");
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

        internal bool TryValidateMachineRecipeChange(string recipeName, out string reason)
        {
            reason = string.Empty;
            if (Controller == null)
            {
                reason = "MachineController가 준비되지 않았습니다.";
                return false;
            }

            bool materialRecipeRestore;
            return Controller.TryValidateRecipeChange(
                NormalizeRecipeName(recipeName),
                out materialRecipeRestore,
                out reason);
        }

        internal bool LoadMachineRecipe(string recipeName)
        {
            IDisposable recipeApplyLease = null;
            try
            {
                if (Machine == null || string.IsNullOrWhiteSpace(recipeName))
                    return false;

                string normalizedRecipeName = NormalizeRecipeName(recipeName);
                if (Controller == null)
                    return false;

                bool materialRecipeRestore;
                string recipeChangeReason;
                if (!Controller.TryBeginRecipeApplyOperation(
                        normalizedRecipeName,
                        out materialRecipeRestore,
                        out recipeApplyLease,
                        out recipeChangeReason))
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        UserSession.Name,
                        "RECIPE-CHANGE-BLOCK",
                        recipeChangeReason);
                    return false;
                }

                return LoadMachineRecipeCore(
                    normalizedRecipeName,
                    materialRecipeRestore,
                    recipeChangeReason,
                    true);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "DATA-LOAD",
                    "Machine recipe load failed: " + recipeName + " / " + ex.Message);

                return false;
            }
            finally
            {
                if (recipeApplyLease != null)
                    recipeApplyLease.Dispose();
            }
        }

        private bool LoadMachineRecipeCore(
            string normalizedRecipeName,
            bool materialRecipeRestore,
            string recipeChangeReason,
            bool broadcastVision)
        {
            QMC.CDT320.Recipes.RecipeProject project =
                QMC.CDT320.Recipes.RecipeStore.Load(normalizedRecipeName);

            if (project == null)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "DATA-LOAD",
                    "Project recipe file not found: " + normalizedRecipeName);
                return false;
            }

            Machine.LoadRecipe(normalizedRecipeName);
            QMC.CDT320.VisionComm.VisionHub.InvalidateRecipeAcknowledgement(
                "HandlerRecipeApply:" + normalizedRecipeName);

            _currentRecipe = project;
            ActiveRecipeName = normalizedRecipeName;
            Controller.SetActiveRecipeName(ActiveRecipeName);
            Controller.NotifyRecipeConfigurationApplied();

            if (materialRecipeRestore)
            {
                if (!Controller.CompleteMaterialRecipeRestore(ActiveRecipeName))
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        UserSession.Name,
                        "RECIPE-RESTORE-FAIL",
                        "장비 내부 Material Recipe 복구 후 Output 전체교체 요청 해제에 실패했습니다. " +
                        "recipe=" + ActiveRecipeName);
                    return false;
                }

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    UserSession.Name,
                    "RECIPE-RESTORE",
                    recipeChangeReason ?? string.Empty);
            }

            if (broadcastVision)
                _ = QMC.CDT320.VisionComm.VisionHub.BroadcastRecipeAsync(ActiveRecipeName);

            return true;
        }

        internal bool ApplyMachineRecipe(QMC.CDT320.Recipes.RecipeProject project)
        {
            try
            {
                if (project == null || string.IsNullOrWhiteSpace(project.FileName))
                    return false;

                string recipeName = NormalizeRecipeName(project.FileName);

                if (!LoadMachineRecipe(recipeName))
                    return false;

                string recipeContextReason;
                if (Controller != null &&
                    !Controller.CompleteRecipeApplyContext(
                        recipeName,
                        out recipeContextReason))
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        UserSession.Name,
                        "RECIPE-CONTEXT-APPLY",
                        "Recipe 적용 후 Material 문맥 동기화 실패. recipe=" +
                        recipeName + ", detail=" + recipeContextReason);
                    return false;
                }

                Controller?.ApplyRecipeMode(_currentRecipe);
                // 레시피 수명과 생산 LOT 수명은 다르다.
                // LOT 완료 전에는 레시피 변경으로 Material의 활성 LOT ID가 바뀌지 않게 다시 동기화한다.
                QMC.CDT320.Lots.LotSessionService.SynchronizeActiveLotToMaterial("RecipeApply");

                QMC.CDT320.Recipes.RecipeStore.SaveLastProjectName(recipeName);
                AppSettingsStore.Current.LastProject = recipeName;
                AppSettingsStore.Save();

                RefreshProjectName(recipeName);

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    UserSession.Name,
                    "RECIPE-APPLY",
                    "Machine recipe applied: " + recipeName);

                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "RECIPE-APPLY",
                    "Machine recipe apply failed: " + project?.FileName + " / " + ex.Message);

                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 현재 활성 Recipe의 Unit 데이터를 저장한 후 다시 적용합니다.
        /// 선택만 된 비활성 Recipe에는 사용할 수 없습니다.
        /// Project 파일은 호출 전에 저장되어 있어야 합니다.
        /// </summary>
        internal bool SaveAndApplyActiveRecipe(
            QMC.CDT320.Recipes.RecipeProject project)
        {
            try
            {
                if (project == null || string.IsNullOrWhiteSpace(project.FileName))
                    return false;

                string recipeName = NormalizeRecipeName(project.FileName);

                if (string.IsNullOrWhiteSpace(ActiveRecipeName) ||
                    !string.Equals(
                        ActiveRecipeName,
                        recipeName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        UserSession.Name,
                        "RECIPE-SAVE-APPLY",
                        "비활성 Recipe의 Unit 데이터 저장을 차단했습니다. " +
                        "active=" + (ActiveRecipeName ?? "-") +
                        ", requested=" + recipeName);

                    return false;
                }

                bool materialRecipeRestore;
                string recipeChangeReason;
                if (Controller != null &&
                    !Controller.TryValidateRecipeChange(
                        recipeName,
                        out materialRecipeRestore,
                        out recipeChangeReason))
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        UserSession.Name,
                        "RECIPE-SAVE-APPLY-BLOCK",
                        recipeChangeReason);
                    return false;
                }

                if (!SaveMachineRecipe(recipeName))
                    return false;

                if (!ApplyMachineRecipe(project))
                    return false;

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    UserSession.Name,
                    "RECIPE-SAVE-APPLY",
                    "Active machine recipe saved and applied: " + recipeName);

                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "RECIPE-SAVE-APPLY",
                    "Active machine recipe save/apply failed: " +
                    project?.FileName + " / " + ex.Message);

                return false;
            }
            finally
            {
            }
        }

        internal bool SaveMachineRecipe(string recipeName)
        {
            //Save하고 Active 버튼 눌러야함. 그래야 현재 Recipe에 맞는 MaterialState가 저장됨. Save만 하면 이전 Recipe에 맞는 MaterialState가 저장됨.
            try
            {
                if (Machine == null || string.IsNullOrWhiteSpace(recipeName))
                    return false;

                string normalizedRecipeName = NormalizeRecipeName(recipeName);

                if (!Machine.SaveRecipe(normalizedRecipeName))
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        UserSession.Name,
                        "DATA-SAVE",
                        "Machine recipe save returned false: " + normalizedRecipeName);
                    return false;
                }

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    UserSession.Name,
                    "DATA-SAVE",
                    "Machine recipe saved: " + normalizedRecipeName);

                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "DATA-SAVE",
                    "Machine recipe save failed: " + recipeName + " / " + ex.Message);

                return false;
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
        private InputStageRunReviewDialog _inputStageRunReviewDialog;
        private int _inputStageRunReviewSessionGeneration;
        private int _inputStageRunReviewCleanedGeneration;
        private IDisposable _inputStageRunReviewJogScope;
        private BaseAxis _inputStageRunReviewJogAxis;
        private bool _inputStageRunReviewJogStartPending;
        private bool _inputStageRunReviewOffsetPending;
        private double _inputStageRunReviewPendingOffsetX;
        private double _inputStageRunReviewPendingOffsetY;
        private string _inputStageRunReviewPendingOffsetWaferId = string.Empty;
        private string _inputStageRunReviewPendingOffsetMappingRevision = string.Empty;
        private string _inputStageRunReviewPendingOffsetDieUid = string.Empty;
        private int _inputStageRunReviewPendingOffsetDieMapX;
        private int _inputStageRunReviewPendingOffsetDieMapY;
        private double _inputStageRunReviewPendingOffsetReferenceX;
        private double _inputStageRunReviewPendingOffsetReferenceY;
        private string _inputStageRunReviewPendingOffsetDraftSignature = string.Empty;
        private WaferVisionTestDialog _inputStageRunReviewVisionTestDialog;
        private IDisposable _inputStageRunReviewVisionTestScope;
        private IDisposable _inputStageRunReviewEmbeddedVisionScope;
        private bool _inputStageRunReviewEmbeddedVisionTransition;

        private MainTab _currentTab = MainTab.Work;
        private bool _mainTabShown;

        public Form1()
        {
            InitializeComponent();
            UiDoubleBuffer.Enable(this);
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
            btnVision.Click += (s, e) => QMC.CDT_320.Ui.Dialogs.VisionViewDialog.Open(this);
            btnTabSettings.Click += (s, e) => ShowTab(MainTab.Settings);
            btnTabUser.Click += (s, e) => ShowTab(MainTab.User);
            btnTabExit.Click += (s, e) => RequestApplicationExit();
            btnTopAlarm.Click += (s, e) => RaiseTestAlarmFromTopButton();
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

        private void RaiseTestAlarmFromTopButton()
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "TEST-ALARM",
                    "Top ALARM button clicked. TEST-ALARM will be raised.");

                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Critical,
                    "TEST-ALARM",
                    "Form1",
                    "상단 ALARM 버튼에 의해 테스트 알람이 발생했습니다.");
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    "UI",
                    "TEST-ALARM",
                    "Top ALARM button failed: " + ex.Message);
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
            btnTopAlarm.BackColor = Color.FromArgb(192, 57, 43);
            btnTopAlarm.ForeColor = Color.White;
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

            // Stage 43 - 6채널: Wafer/Inspection/Bin + Main/FrontSide/RearSide
            // Vision PC 가 레시피를 요청(RECIPEREQ)하면 현재 활성 레시피로 응답 — 핸들러가 먼저 안 보내도 Vision 이 능동 동기화.
            QMC.CDT320.VisionComm.VisionHub.OnVisionRecipeRequest = BroadcastCurrentRecipeToVision;

            if (cfg.VisionAutoConnect && cfg.UseVision)
            {
                _ = QMC.CDT320.VisionComm.VisionHub.ConnectAllAsync(
                    cfg.VisionHost,
                    cfg.VisionWaferPort, cfg.VisionInspectionPort, cfg.VisionBinPort,
                    cfg.VisionMainPort,  cfg.VisionFrontSidePort,    cfg.VisionRearSidePort);
            }
            QMC.CDT320.VisionComm.VisionHub.ConnectionChanged += OnVisionHubChanged;
            QMC.CDT320.VisionComm.VisionReconnectWatchdog.Start(BroadcastCurrentRecipeToVision);

            Machine    = new CDT320_Machine();
            LoadMachineSettings();
            // [주소 불일치 감시 2026-07-28] Setup 파일이 카탈로그 주소를 덮어쓴 채 조용히 운전되던 문제
            // (GoodBinRing/NgBinRing Bit 뒤바뀜)를 기동 시 로그로 드러낸다. 값은 고치지 않고 경고만 남긴다.
            QMC.CDT320.Ajin.AjinFactory.VerifyCatalogAddresses("Startup");
            // [실장비 시뮬 강제 해제 2026-07-29] ★실장비 미검증★
            // LoadMachineSettings() 안에서 EquipmentData\Config\*.json 이 실보드 포인트를 시뮬로
            // 되돌려놓는 경로가 있었다(AjinDigitalInput/Output.LoadSettings 주석 참조).
            // 각 포인트에서 이미 false 로 강제했고, 여기서는 합계만 로그로 남긴다.
            // 합계가 0 이 아니면 그동안 그만큼의 실신호가 죽어 있었다는 뜻이다.
            QMC.CDT320.Ajin.AjinDigitalInput.LogSimForcedRealSummary();
            QMC.CDT320.Ajin.AjinDigitalOutput.LogSimForcedRealSummary();
            ApplyRuntimeMode();
            Bridge     = new SimulatorBridge(Machine);
            BeginSimulatorAutoConnect(cfg);
            Controller = new MachineController(Machine);
            Controller.SetActiveRecipeName(ActiveRecipeName);
            ApplyRuntimeMode();
            Controller.ApplyStartupMachineRuntimeState(cfg);
            AlarmResponse = new QMC.CDT320.Alarms.AlarmResponseService(Controller);
            AlarmResponse.Start();
            PromptMaterialRecoveryOnStartup();
            // Material을 사용하지 않기로 선택해도 LOT은 작업자가 [LOT 완료]하기 전까지 유지한다.
            // 신규 활성 포인터를 우선하고, 도입 전 데이터는 초기화 직전 Snapshot LOT ID로 제한 복구한다.
            QMC.CDT320.Lots.LotSessionService.RestoreActiveLotOnStartup(
                _materialSnapshotLotIdBeforeInitialization);
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
            MotionMonitor.Start(CurrentAxes(), QMC.CDT320.Ajin.AjinFactory.UseRealBoard ? 50 : 250);
            CollisionSupervisor = new QMC.CDT320.Interlocks.RealtimeCollisionSupervisor(
                Machine,
                Controller != null && Controller.SharedRailX != null
                    ? Controller.SharedRailX.Config
                    : QMC.CDT320.Motion.SharedRailX.SharedRailXConfigStore.LoadOrCreateDefault());
            // 사용자 정책: 실시간 충돌 감지 시 전축 하드정지.
            CollisionSupervisor.SetStopAllAxesHandler(StopAllAxesForCollisionSupervisor);
            CollisionSupervisor.Start(10);
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
            Controller.OperatorMessageRequested += OnOperatorMessageRequested;
            Controller.InputStageRunReviewManualStateChanged += OnInputStageRunReviewManualStateChanged;
            if (Machine != null && Machine.InputStageUnit != null)
            {
                Machine.InputStageUnit.UserConfirmRequested += OnInputStageUserConfirmRequested;
                Machine.InputStageUnit.UserConfirmWaitEnded += OnInputStageUserConfirmWaitEnded;
                Machine.InputStageUnit.UserConfirmProcessingFailed += OnInputStageUserConfirmProcessingFailed;
            }
            Controller.LogMessage    += s =>
            {
                // 1순위: 시퀀스 스코프가 있으면 그 종류·유닛(SOURCE)·스텝(CODE)으로 정확히 분류.
                var seq = QMC.CDT320.Sequencing.SequenceLog.Current;
                if (seq != null)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        seq.Kind,
                        UserSession.Name,
                        seq.Step,
                        seq.Unit,
                        QMC.CDT320.Sequencing.SequenceLog.FormatWithCurrentContext("PublicLog", seq.Unit, s));
                    return;
                }
                // 2순위(폴백): 스코프 없는 직접 호출 경로는 메시지 접두어로 시퀀스 종류 추정(아니면 Event).
                var kind = QMC.CDT320.Sequencing.SequenceLog.ClassifyByMessage(s);
                QMC.Common.Logging.EventLogger.Write(kind, UserSession.Name, "CTRL", s);
            };
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

            // 임시 TEST 운전: Release 빌드도 Debug와 동일하게 시작 즉시 Admin 세션으로 진입한다.
            // 사용자 승인 후 정식 로그인 정책으로 복귀할 때 이 자동 로그인 호출을 다시 DEBUG 조건으로 제한한다.
            QMC.CDT_320.Ui.Security.UserSession.ForceSet(
                "admin", QMC.CDT_320.Ui.Security.UserLevel.Admin);
            OnUserChanged();

            try
            {
                var last = QMC.CDT320.Recipes.RecipeStore.LoadLastOrDefault();
                if (last != null)
                {
                    if (LoadMachineRecipe(last.FileName))
                    {
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
                    else
                    {
                        QMC.Common.Logging.EventLogger.Write(
                            QMC.Common.Logging.EventKind.Alarm,
                            "NONE",
                            "RECIPE-STARTUP-BLOCK",
                            "저장된 마지막 Recipe를 적용하지 못했습니다. 장비 내부 Material Recipe와 일치하는 Recipe를 확인하여 적용하십시오. " +
                            "lastProject=" + last.FileName);
                    }
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
                // 사용자가 아래에서 Material 초기화를 선택해도 LOT 복구 키는 잃지 않는다.
                // 실제 복원은 Running 이력과 일치하는 경우에만 LotSessionService가 수행한다.
                _materialSnapshotLotIdBeforeInitialization = lot;
                string recipe = ResolveMaterialSnapshotRecipeName(snapshot);
                string snapshotPath = string.IsNullOrWhiteSpace(MaterialSnapshotStore.LastLoadedPath)
                    ? MaterialSnapshotStore.SnapshotPath
                    : MaterialSnapshotStore.LastLoadedPath;
                string snapshotFileName = System.IO.Path.GetFileName(snapshotPath);
                int waferCount = CountMaterialSnapshotWafers(snapshot);
                int dieCount = CountMaterialSnapshotDies(snapshot);
                int pickerDieCount = CountMaterialSnapshotPickerDies(snapshot);

                var message =
                    "이전에 저장된 Material 정보가 있습니다.\r\n\r\n" +
                    "저장 시간: " + savedAt + "\r\n" +
                    "Recipe: " + (string.IsNullOrEmpty(recipe) ? "-" : recipe) + "\r\n" +
                    "Lot: " + (string.IsNullOrEmpty(lot) ? "-" : lot) + "\r\n" +
                    "Wafer: " + waferCount + " / Die: " + dieCount + "\r\n" +
                    "Picker 보유/예약 Die: " + pickerDieCount + "\r\n" +
                    "File: " + (string.IsNullOrEmpty(snapshotFileName) ? "-" : snapshotFileName) + "\r\n" +
                    "Path: " + snapshotPath + "\r\n\r\n" +
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
                    MaterialStorage.ReplaceState(snapshot);
                    MaterialStateService.RestoreInputStageDieMappingCompleteFromSavedMap("MaterialRecoveryInputStageDieMapRestore");
                    _materialSnapshotRestored = true;
                    if (!_materialSnapshotRestored)
                    {
                        Log.Write("Main", UserSession.Name, "MaterialRecovery", "Material snapshot restore failed. New empty Material state will be created. - Failed");
                        QMC.Common.MessageDialog.Show(
                            this,
                            "Material 정보 복구에 실패했습니다.\r\n새 Material 상태로 초기화합니다.\r\n\r\nFile: " + snapshotPath,
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
            return snapshot != null && !string.IsNullOrWhiteSpace(snapshot.LotId)
                ? snapshot.LotId.Trim()
                : "";
        }

        private static int CountMaterialSnapshotWafers(MaterialSnapshot snapshot)
        {
            try
            {
                return snapshot != null && snapshot.Wafers != null ? snapshot.Wafers.Count : 0;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
        }

        private static int CountMaterialSnapshotDies(MaterialSnapshot snapshot)
        {
            try
            {
                return snapshot != null && snapshot.Dies != null ? snapshot.Dies.Count : 0;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
        }

        private static int CountMaterialSnapshotPickerDies(MaterialSnapshot snapshot)
        {
            try
            {
                if (snapshot == null || snapshot.Dies == null)
                    return 0;

                int count = 0;
                foreach (var die in snapshot.Dies)
                {
                    if (die == null)
                        continue;

                    MaterialLocation location = die.CurrentLocation;
                    MaterialLocationKind kind = location != null ? location.Kind : MaterialLocationKind.Unknown;
                    if (kind == MaterialLocationKind.PickerFront ||
                        kind == MaterialLocationKind.PickerRear ||
                        die.PickedPickerNo > 0 ||
                        die.ReservedPickerNo > 0)
                    {
                        count++;
                    }
                }

                return count;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
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
            // Recipe에 저장된 과거 LOT ID를 새 LOT처럼 되살리지 않는다.
            // 활성 LOT이 있으면 그 ID만 유지하고, 없으면 명시적인 [LOT 시작] 전까지 빈 상태로 둔다.
            MaterialStorage.State.LotId = QMC.CDT320.Lots.LotSessionService.IsLotActive
                ? QMC.CDT320.Lots.LotSessionService.ActiveLotId
                : "";
            MaterialStateService.NotifyAndSave("InitializeFromRecipe");
        }

        /// <summary>선택한 메인 탭을 화면에 표시합니다.</summary>
        public void ShowTab(MainTab tab)
        {
            bool sameTab = _currentTab == tab;
            _currentTab = tab;

            SetVisibleIfChanged(_workTab,     tab == MainTab.Work);
            SetVisibleIfChanged(_workInfoTab, tab == MainTab.WorkInfo);
            SetVisibleIfChanged(_historyTab,  tab == MainTab.History);
            SetVisibleIfChanged(_recipeTab,   tab == MainTab.Recipe);
            SetVisibleIfChanged(_settingsTab, tab == MainTab.Settings);
            SetVisibleIfChanged(_userTab,     tab == MainTab.User);

            SetSelectedIfChanged(btnTabWork,     tab == MainTab.Work);
            SetSelectedIfChanged(btnTabWorkInfo, tab == MainTab.WorkInfo);
            SetSelectedIfChanged(btnTabHistory,  tab == MainTab.History);
            SetSelectedIfChanged(btnTabRecipe,   tab == MainTab.Recipe);
            SetSelectedIfChanged(btnTabSettings, tab == MainTab.Settings);
            SetSelectedIfChanged(btnTabUser,     tab == MainTab.User);

            if (sameTab && _mainTabShown)
                return;

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
                EnsureDefaultPageShown(active);
                Lang.Apply(active);
                AccessControl.Apply(active);
            }
            _mainTabShown = true;
        }

        private static void EnsureDefaultPageShown(Control active)
        {
            var tab = active as TabBase;
            if (tab != null)
                tab.EnsureDefaultPageShown();
        }

        private static void SetVisibleIfChanged(Control control, bool visible)
        {
            if (control != null && control.Visible != visible)
                control.Visible = visible;
        }

        private static void SetSelectedIfChanged(QMC.CDT_320.Ui.Controls.BottomMenuButton button, bool selected)
        {
            if (button != null && button.Selected != selected)
                button.Selected = selected;
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

        private void OnOperatorMessageRequested(string title, string message)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action<string, string>(OnOperatorMessageRequested), title, message);
                    return;
                }

                QMC.Common.MessageDialog.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Operator message display failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OnInputStageUserConfirmRequested()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnInputStageUserConfirmRequested));
                return;
            }

            InputStageUnit stage = Machine != null ? Machine.InputStageUnit : null;
            if (stage == null)
                return;

            if (_inputStageRunReviewDialog != null && !_inputStageRunReviewDialog.IsDisposed)
            {
                _inputStageRunReviewDialog.Activate();
                _inputStageRunReviewDialog.BringToFront();
                return;
            }

            InputStageRunReviewDialog dialog = null;
            try
            {
                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                QMC.CDT320.DieMaps.DieMap stageMap = MaterialStateService.BuildInputDieMapFromStageWafer();
                if (wafer == null || stageMap == null || stageMap.Entries == null || stageMap.Entries.Count == 0)
                    throw new InvalidOperationException("InputStage 사용자 확인 화면에 표시할 Wafer/Die Map 데이터가 없습니다.");

                bool alignComplete = wafer.HasInputStageAlignResult && wafer.HasInputStageThetaAlignResult;
                bool mappingComplete = wafer.HasInputStageDieMappingResult &&
                                       !wafer.InputStageDieMappingInvalidatedByAlignChange;
                QMC.CDT320.Recipes.RecipeProject project = _currentRecipe ??
                    QMC.CDT320.Recipes.RecipeStore.LoadLastOrDefault();
                QMC.CDT320.Recipes.PickupSubset pickup = project != null
                    ? (project.InputPickup ?? project.Pickup ?? new QMC.CDT320.Recipes.PickupSubset())
                    : new QMC.CDT320.Recipes.PickupSubset();
                string recipeName = project != null && !string.IsNullOrWhiteSpace(project.FileName)
                    ? project.FileName
                    : ActiveRecipeName;
                string mappingReference = !string.IsNullOrWhiteSpace(wafer.DieMapFrameObjId)
                    ? wafer.DieMapFrameObjId
                    : stageMap.FrameObjId;

                dialog = new InputStageRunReviewDialog();
                _inputStageRunReviewDialog = dialog;
                int sessionGeneration = ++_inputStageRunReviewSessionGeneration;
                ClearInputStageRunReviewPendingOffset();
                dialog.SetMode(InputStageRunReviewMode.MappingReview);
                dialog.SetPickupOptions(pickup);
                dialog.SetDieMap(stageMap);
                dialog.SetWorkflowState(
                    wafer.WaferId,
                    recipeName,
                    QMC.CDT320.VisionComm.VisionHub.Wafer != null &&
                    QMC.CDT320.VisionComm.VisionHub.Wafer.IsConnected,
                    alignComplete,
                    mappingReference,
                    mappingComplete,
                    "WAITING USER CONFIRM");
                dialog.SetAxisPositions(
                    stage.CameraX != null ? stage.CameraX.ActualPosition : 0.0,
                    stage.StageY != null ? stage.StageY.ActualPosition : 0.0,
                    stage.StageT != null ? stage.StageT.ActualPosition : 0.0);
                dialog.SetAxisPositionProvider(() => new double[]
                {
                    ReadCachedAxisPosition(stage.CameraX),
                    ReadCachedAxisPosition(stage.StageY),
                    ReadCachedAxisPosition(stage.StageT)
                });
                dialog.SetFailureDetail(
                    string.Empty,
                    "확인: 현재 Align/Die Mapping 결과로 Auto PickUp 공정을 계속합니다." + Environment.NewLine +
                    "취소: Picker Ready를 발행하지 않고 센터 검출/T Align부터 다시 수행한 뒤 Die Mapping과 확인을 반복합니다.");
                dialog.SetReviewValid(alignComplete && mappingComplete, "USER CONFIRM REQUIRED");
                dialog.SetAutoReviewMode(true);

                dialog.StartRunRequested += delegate
                {
                    if (Controller == null || !Controller.IsInputStageRunReviewManualActive)
                    {
                        dialog.RestoreAfterDecisionFailure("활성 InputStage Review Manual 세션이 없습니다.");
                        return;
                    }
                    if (Controller.IsInputStageRunReviewActionBusy)
                    {
                        dialog.RestoreAfterDecisionFailure("수동 동작 또는 Jog가 진행 중입니다. STOP 후 다시 확인하세요.");
                        return;
                    }
                    stage.ConfirmFromUi(BuildInputStageRunReviewResult(
                        dialog,
                        InputStageRunReviewDecision.ConfirmAndContinue));
                };
                dialog.AbortAutoRequested += delegate
                {
                    stage.ConfirmFromUi(BuildInputStageRunReviewResult(
                        dialog,
                        InputStageRunReviewDecision.RetryAlign));
                };
                dialog.AlignRetryRequested += delegate
                {
                    stage.ConfirmFromUi(BuildInputStageRunReviewResult(
                        dialog,
                        InputStageRunReviewDecision.RetryAlign));
                };
                dialog.MappingRetryRequested += delegate
                {
                    stage.ConfirmFromUi(BuildInputStageRunReviewResult(
                        dialog,
                        InputStageRunReviewDecision.RetryMapping));
                };
                dialog.SelectedDieMoveRequested += delegate
                {
                    RunInputStageReviewMoveSelectedDieAsync(dialog);
                };
                dialog.JogRequested += delegate(object sender, InputStageReviewJogEventArgs args)
                {
                    StartInputStageRunReviewJogAsync(dialog, args);
                };
                dialog.JogStopRequested += delegate
                {
                    StopInputStageRunReviewJogAsync(dialog, "Review Jog STOP");
                };
                dialog.ReviewActionStopRequested += delegate
                {
                    StopInputStageRunReviewActionAsync(dialog, "Review 수동 동작 STOP");
                };
                dialog.ThetaCorrectionRequested += delegate
                {
                    RunInputStageReviewThetaCorrectionAsync(dialog);
                };
                dialog.DieDetectionRequested += delegate
                {
                    RunInputStageReviewDieDetectionAsync(dialog);
                };
                dialog.OffsetApplyRequested += delegate
                {
                    ApplyInputStageRunReviewPendingOffset(dialog);
                };
                dialog.VisionTestRequested += delegate
                {
                    OpenInputStageRunReviewVisionTest(dialog);
                };
                dialog.WaferVisionControlStartRequested += delegate
                {
                    StartInputStageRunReviewEmbeddedVisionAsync(dialog);
                };
                dialog.WaferVisionControlStopRequested += delegate
                {
                    StopInputStageRunReviewEmbeddedVision(
                        dialog,
                        true,
                        "Wafer Vision Live/Grab 사용을 종료했습니다.");
                };
                dialog.BuzzerStopRequested += delegate
                {
                    StopRunReviewBuzzer();
                };

                InputStageRunReviewDialog sessionDialog = dialog;
                dialog.FormClosed += delegate
                {
                    CleanupInputStageRunReviewSessionAsync(sessionDialog, sessionGeneration);
                };

                StartRunReviewBuzzer();
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "Align/Die Mapping 사용자 확인 화면을 표시했습니다. wafer=" + (wafer.WaferId ?? "") + " - Wait");
                // Main UI 접근을 허용하기 위해 unowned modeless top-level 창으로 연다.
                // 정리는 FormClosed 기반 CleanupInputStageRunReviewSessionAsync 단일 경로에서 수행한다.
                dialog.Show();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "사용자 확인 화면 표시 실패: " + ex.Message + " - Failed");
                stage.FailUserConfirmFromUi("InputStage 사용자 확인 화면을 표시하지 못했습니다. " + ex.Message);
                if (dialog != null)
                    CleanupInputStageRunReviewSessionAsync(dialog, _inputStageRunReviewSessionGeneration);
            }
        }

        /// <summary>
        /// Review Modeless 세션의 단일 정리 경로입니다.
        /// FormClosed, Sequence 종료, 전역 STOP, Main Form 종료가 중복 호출해도 안전한 idempotent 구조입니다.
        /// </summary>
        private async void CleanupInputStageRunReviewSessionAsync(
            InputStageRunReviewDialog dialog,
            int sessionGeneration)
        {
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<InputStageRunReviewDialog, int>(
                        CleanupInputStageRunReviewSessionAsync), dialog, sessionGeneration);
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                        "Review Cleanup UI 전환 실패: " + ex.Message + " - Failed");
                }
                return;
            }

            if (_inputStageRunReviewCleanedGeneration >= sessionGeneration)
                return;
            _inputStageRunReviewCleanedGeneration = sessionGeneration;

            // 이미 새 Review 세션이 시작된 뒤 늦게 도착한 이전 세션 정리라면
            // 모션/TCS 관련 정리는 건너뛰고 이전 dialog 자원만 해제한다.
            bool isCurrentSession = _inputStageRunReviewSessionGeneration == sessionGeneration;

            try { EndRunReviewBuzzer(); } catch { }

            if (isCurrentSession)
            {
                try
                {
                    if (Controller != null)
                        Controller.CancelInputStageRunReviewAction();
                }
                catch { }
                StopInputStageRunReviewEmbeddedVision(
                    dialog,
                    false,
                    "Review 종료로 내장 Wafer Vision을 정지했습니다.");
                try
                {
                    await StopInputStageRunReviewJogCoreAsync().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                        "Review Cleanup Jog 정지 실패: " + ex.Message + " - Failed");
                }
                if (_inputStageRunReviewVisionTestDialog != null &&
                    !_inputStageRunReviewVisionTestDialog.IsDisposed)
                {
                    try
                    {
                        await _inputStageRunReviewVisionTestDialog.RequestClose().ConfigureAwait(true);
                    }
                    catch (Exception ex)
                    {
                        QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                            "Review Cleanup Vision Test 종료 대기 실패: " + ex.Message + " - Failed");
                    }
                }
                ClearInputStageRunReviewPendingOffset();
            }

            if (dialog != null)
            {
                try
                {
                    if (!dialog.IsDisposed)
                        dialog.Dispose();
                }
                catch { }
            }

            if (ReferenceEquals(_inputStageRunReviewDialog, dialog) &&
                _inputStageRunReviewSessionGeneration == sessionGeneration)
                _inputStageRunReviewDialog = null;
        }

        private void OnInputStageUserConfirmWaitEnded()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnInputStageUserConfirmWaitEnded));
                return;
            }

            InputStageRunReviewDialog dialog = _inputStageRunReviewDialog;
            if (dialog != null && !dialog.IsDisposed &&
                (Controller == null || !Controller.IsInputStageRunReviewManualActive))
            {
                // Vision Test owned form이 종료를 취소하는 동안 부모를 먼저 닫지 않는다.
                // ManualStateChanged 경로가 Vision 요청 종료를 await한 뒤 Review 창을 닫는다.
                if (_inputStageRunReviewVisionTestDialog != null &&
                    !_inputStageRunReviewVisionTestDialog.IsDisposed)
                    return;
                dialog.CloseFromSequence();
            }
        }

        private UserConfirmResult BuildInputStageRunReviewResult(
            InputStageRunReviewDialog dialog,
            InputStageRunReviewDecision decision)
        {
            var result = new UserConfirmResult
            {
                IsConfirmed = decision == InputStageRunReviewDecision.ConfirmAndContinue,
                Decision = decision,
                WaferId = dialog != null ? dialog.WaferId : string.Empty,
                MappingRevision = dialog != null ? dialog.MappingRevision : string.Empty,
                StartDieUid = dialog != null ? dialog.StartDieUid : string.Empty,
                StartDieIndex = dialog != null ? dialog.StartDieIndex : 0
            };

            if (dialog != null)
            {
                result.OrderedDieIds = dialog.OrderedDieIds.ToList();
                QMC.CDT320.DieMaps.DieMap draft = dialog.DraftDieMap;
                if (draft != null && draft.Entries != null)
                {
                    result.DieStates = draft.Entries
                        .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.DieUid))
                        .Select(entry => new InputStageRunReviewDieState
                        {
                            DieId = entry.DieUid,
                            IsTarget = entry.IsTarget,
                            Result = entry.Result,
                            BinCode = entry.BinCode,
                            HasPosition = !double.IsNaN(entry.PosX) && !double.IsInfinity(entry.PosX) &&
                                          !double.IsNaN(entry.PosY) && !double.IsInfinity(entry.PosY),
                            PositionX = entry.PosX,
                            PositionY = entry.PosY
                        })
                        .ToList();
                    result.HasMapOrigin = !double.IsNaN(draft.OriginX) && !double.IsInfinity(draft.OriginX) &&
                                          !double.IsNaN(draft.OriginY) && !double.IsInfinity(draft.OriginY);
                    result.MapOriginX = draft.OriginX;
                    result.MapOriginY = draft.OriginY;
                }
            }

            return result;
        }

        private void OnInputStageUserConfirmProcessingFailed(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(OnInputStageUserConfirmProcessingFailed), message);
                return;
            }

            InputStageRunReviewDialog dialog = _inputStageRunReviewDialog;
            if (dialog != null && !dialog.IsDisposed)
                dialog.RestoreAfterDecisionFailure(message);
        }

        private async void OnInputStageRunReviewManualStateChanged(bool active, string waferId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool, string>(OnInputStageRunReviewManualStateChanged), active, waferId);
                return;
            }

            if (active)
                return;

            // 이전 세션의 늦은 비활성 알림이 새 Review 세션 창을 닫지 않도록 방어한다.
            if (Controller != null && Controller.IsInputStageRunReviewManualActive)
                return;

            InputStageRunReviewDialog dialog = _inputStageRunReviewDialog;
            StopInputStageRunReviewJogAsync(dialog, "Review Manual 종료로 Jog를 정지했습니다.");
            StopInputStageRunReviewEmbeddedVision(
                dialog,
                false,
                "Review Manual 종료로 내장 Wafer Vision을 정지했습니다.");
            if (_inputStageRunReviewVisionTestDialog != null &&
                !_inputStageRunReviewVisionTestDialog.IsDisposed)
            {
                try
                {
                    await _inputStageRunReviewVisionTestDialog.RequestClose().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReviewVisionTest",
                        "Review Manual 종료 시 Vision Test 화면 종료 대기 실패: " + ex.Message + " - Failed");
                }
            }
            ClearInputStageRunReviewPendingOffset();
            if (dialog != null && !dialog.IsDisposed)
                dialog.CloseFromSequence();
        }

        private async void RunInputStageReviewMoveSelectedDieAsync(InputStageRunReviewDialog dialog)
        {
            DieMapEntry entry = dialog != null ? dialog.SelectedDie : null;
            if (entry == null)
            {
                if (dialog != null)
                    dialog.SetBusy(false, "이동할 Die를 먼저 선택하세요.");
                return;
            }

            DialogResult confirm = QMC.Common.MessageDialog.Show(
                dialog,
                "선택 Die의 Mapping 절대좌표로 이동하시겠습니까?\r\n" +
                "UID=" + (entry.DieUid ?? "") + "\r\n" +
                "X=" + entry.PosX.ToString("F6") + " mm, Y=" + entry.PosY.ToString("F6") + " mm",
                "InputStage Review",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            ClearInputStageRunReviewPendingOffset();

            await RunInputStageReviewOneShotAsync(
                dialog,
                "Move Selected Die",
                async (stage, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    // Map 절대좌표 이동은 Auto 속도가 아니라 Manual Sequence 화면에서 설정한
                    // Ready 속도(%)를 사용합니다. Review는 Auto와 병행될 수 있으므로
                    // 전역 READY Scope 없이 이 이동 명령에만 퍼센트를 적용합니다.
                    int result = await stage.MoveVisionPointSafelyAtReadySequenceSpeedAsync(
                        entry.PosX,
                        entry.PosY,
                        "InputStageRunReview.MoveSelectedDie").ConfigureAwait(false);
                    if (result != 0)
                        throw new InvalidOperationException("선택 Die 좌표 이동 실패. result=" + result);
                    return "선택 Die 좌표 이동을 완료했습니다. UID=" + (entry.DieUid ?? "");
                },
                true).ConfigureAwait(true);
        }

        /// <summary>
        /// Review 화면 Encoder 표시용으로 MotionMonitor 캐시 스냅샷을 우선 사용하고,
        /// 캐시가 없으면 축의 마지막 ActualPosition 값을 반환한다. 보드 I/O를 호출하지 않는다.
        /// </summary>
        private double ReadCachedAxisPosition(QMC.Common.Motion.BaseAxis axis)
        {
            if (axis == null)
                return 0.0;
            try
            {
                var snapshot = MotionMonitor != null ? MotionMonitor.GetLatest(axis) : null;
                return snapshot != null ? snapshot.ActualPosition : axis.ActualPosition;
            }
            catch
            {
                return axis.ActualPosition;
            }
        }

        private async System.Threading.Tasks.Task RunInputStageReviewOneShotAsync(
            InputStageRunReviewDialog dialog,
            string actionName,
            Func<InputStageUnit, System.Threading.CancellationToken, System.Threading.Tasks.Task<string>> action,
            bool allowEmbeddedVisionScopeReuse = false)
        {
            IDisposable workScope = null;
            string finalStatus = string.Empty;
            bool reusedEmbeddedVisionScope = false;
            try
            {
                if (dialog == null || dialog.IsDisposed || Controller == null ||
                    !Controller.IsInputStageRunReviewManualActive)
                    throw new InvalidOperationException("활성 InputStage Review Manual 세션이 없습니다.");
                if (Machine == null || Machine.InputStageUnit == null)
                    throw new InvalidOperationException("InputStage Unit이 없습니다.");

                reusedEmbeddedVisionScope =
                    allowEmbeddedVisionScopeReuse &&
                    !_inputStageRunReviewEmbeddedVisionTransition &&
                    _inputStageRunReviewEmbeddedVisionScope != null &&
                    dialog.IsWaferVisionControlActive;

                if (reusedEmbeddedVisionScope)
                {
                    string safetyReason;
                    if (!Controller.AreInputStageRunReviewPickersSafe(out safetyReason))
                    {
                        throw new InvalidOperationException(
                            "Review 선택 Die 이동 직전 Picker 안전 재확인에 실패했습니다. " + safetyReason);
                    }
                    dialog.SetWaferVisionMoveBusy(
                        true,
                        actionName + " 동작 중입니다. Live 영상은 유지되며 STOP으로 취소할 수 있습니다.");
                }
                else
                {
                    dialog.SetBusy(true, actionName + " 동작 중입니다. STOP으로 취소할 수 있습니다.");
                    workScope = await Controller.BeginInputStageRunReviewWorkAsync(
                        ManualMotionScopeKind.ProcessSequence,
                        actionName,
                        System.Threading.CancellationToken.None).ConfigureAwait(true);
                }

                System.Threading.CancellationToken actionToken =
                    Controller.InputStageRunReviewActionToken;
                actionToken.ThrowIfCancellationRequested();
                using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginManualSequenceProcessMove(
                    "InputStageRunReview." + actionName))
                {
                    finalStatus = await action(
                        Machine.InputStageUnit,
                        actionToken).ConfigureAwait(true);
                }
                dialog.SetAxisPositions(
                    Machine.InputStageUnit.CameraX != null ? Machine.InputStageUnit.CameraX.ActualPosition : 0.0,
                    Machine.InputStageUnit.StageY != null ? Machine.InputStageUnit.StageY.ActualPosition : 0.0,
                    Machine.InputStageUnit.StageT != null ? Machine.InputStageUnit.StageT.ActualPosition : 0.0);
            }
            catch (OperationCanceledException)
            {
                finalStatus = actionName + " 동작이 STOP/취소되었습니다.";
            }
            catch (Exception ex)
            {
                finalStatus = actionName + " 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReviewAction",
                    finalStatus + " - Failed");
            }
            finally
            {
                if (workScope != null)
                    workScope.Dispose();
                if (dialog != null && !dialog.IsDisposed)
                {
                    if (reusedEmbeddedVisionScope && dialog.IsWaferVisionControlActive)
                        dialog.SetWaferVisionMoveBusy(false, finalStatus);
                    else
                        dialog.SetBusy(false, finalStatus);
                }
            }
        }

        private async void StartInputStageRunReviewJogAsync(
            InputStageRunReviewDialog dialog,
            InputStageReviewJogEventArgs args)
        {
            IDisposable scope = null;
            try
            {
                if (dialog == null || args == null || Controller == null ||
                    !Controller.IsInputStageRunReviewManualActive || Machine == null || Machine.InputStageUnit == null)
                    return;

                if (_inputStageRunReviewJogScope != null || Controller.IsInputStageRunReviewActionBusy)
                {
                    dialog.SetBusy(false, "다른 Review 동작이 진행 중입니다. STOP 후 다시 시도하세요.");
                    return;
                }

                InputStageUnit stage = Machine.InputStageUnit;
                BaseAxis axis = args.Axis == InputStageReviewJogAxis.VisionX
                    ? stage.CameraX
                    : args.Axis == InputStageReviewJogAxis.WaferY
                        ? stage.StageY
                        : stage.StageT;
                if (axis == null)
                    throw new InvalidOperationException("Jog 대상 축이 없습니다. axis=" + args.Axis);

                JogSpeedType speedType;
                double customSpeed = 0.0;
                if (string.Equals(args.Speed, "Coarse", StringComparison.OrdinalIgnoreCase))
                {
                    speedType = JogSpeedType.Coarse;
                }
                else if (string.Equals(args.Speed, "Medium", StringComparison.OrdinalIgnoreCase))
                {
                    speedType = JogSpeedType.Custom;
                    customSpeed = axis.Config != null
                        ? Math.Max(0.000001, axis.Config.JogCoarseVelocity * 0.5)
                        : 0.5;
                }
                else
                {
                    speedType = JogSpeedType.Fine;
                }

                if (args.IsStepMode)
                {
                    // Step 모드: one-shot 이동 후 즉시 안전영역 scope를 반환한다.
                    await RunInputStageReviewOneShotAsync(
                        dialog,
                        "Jog Step:" + args.Axis,
                        async (stageUnit, token) =>
                        {
                            token.ThrowIfCancellationRequested();
                            int stepResult = await stageUnit.JogStepAsync(
                                axis,
                                args.Direction,
                                speedType,
                                customSpeed,
                                args.StepDistance).ConfigureAwait(false);
                            if (stepResult != 0)
                                throw new InvalidOperationException(
                                    "Step Jog 실패. axis=" + args.Axis + ", result=" + stepResult);
                            return args.Axis + " Step Jog(" +
                                   args.StepDistance.ToString("0.###") + ") 완료.";
                        }).ConfigureAwait(true);
                    return;
                }

                dialog.SetBusy(true, args.Axis + " Jog 시작 중입니다. 버튼을 놓거나 STOP을 누르세요.");
                _inputStageRunReviewJogStartPending = true;
                scope = await Controller.BeginInputStageRunReviewWorkAsync(
                    ManualMotionScopeKind.ProcessSequence,
                    "Jog:" + args.Axis,
                    System.Threading.CancellationToken.None).ConfigureAwait(true);

                System.Threading.CancellationToken actionToken = Controller.InputStageRunReviewActionToken;
                if (!_inputStageRunReviewJogStartPending ||
                    actionToken.IsCancellationRequested ||
                    !Controller.IsInputStageRunReviewManualActive)
                {
                    throw new OperationCanceledException(
                        "Jog 안전영역 대기 중 STOP/MouseUp 또는 Review 종료가 요청되었습니다.",
                        actionToken);
                }

                _inputStageRunReviewJogScope = scope;
                _inputStageRunReviewJogAxis = axis;
                _inputStageRunReviewJogStartPending = false;
                scope = null;

                int result;
                using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginManualSequenceProcessMove(
                    "InputStageRunReview.Jog:" + args.Axis))
                {
                    result = await stage.JogContinuousAsync(
                        axis,
                        args.Direction,
                        speedType,
                        customSpeed).ConfigureAwait(true);
                }
                if (result != 0)
                    throw new InvalidOperationException("Jog 명령 실패. axis=" + args.Axis + ", result=" + result);

                dialog.SetBusy(true, args.Axis + " Jog 중입니다. 버튼을 놓거나 STOP을 누르세요.");
            }
            catch (OperationCanceledException)
            {
                _inputStageRunReviewJogStartPending = false;
                if (scope != null)
                {
                    scope.Dispose();
                    scope = null;
                }
                await StopInputStageRunReviewJogCoreAsync().ConfigureAwait(true);
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetBusy(false, "Jog 시작이 취소되었습니다.");
            }
            catch (Exception ex)
            {
                _inputStageRunReviewJogStartPending = false;
                if (scope != null)
                    scope.Dispose();
                await StopInputStageRunReviewJogCoreAsync().ConfigureAwait(true);
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetBusy(false, "Jog 실패: " + ex.Message);
            }
        }

        private async void StopInputStageRunReviewJogAsync(
            InputStageRunReviewDialog dialog,
            string reason)
        {
            try
            {
                bool hasJog = _inputStageRunReviewJogStartPending ||
                              _inputStageRunReviewJogScope != null ||
                              _inputStageRunReviewJogAxis != null;
                if (!hasJog)
                    return;

                if (Controller != null)
                    Controller.CancelInputStageRunReviewAction();
                await StopInputStageRunReviewJogCoreAsync().ConfigureAwait(true);
                if (dialog != null && !dialog.IsDisposed)
                {
                    InputStageUnit stage = Machine != null ? Machine.InputStageUnit : null;
                    if (stage != null)
                    {
                        dialog.SetAxisPositions(
                            stage.CameraX != null ? stage.CameraX.ActualPosition : 0.0,
                            stage.StageY != null ? stage.StageY.ActualPosition : 0.0,
                            stage.StageT != null ? stage.StageT.ActualPosition : 0.0);
                    }
                    dialog.SetBusy(false, string.IsNullOrWhiteSpace(reason) ? "Jog를 정지했습니다." : reason);
                }
            }
            catch (Exception ex)
            {
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetBusy(false, "Jog 정지 실패: " + ex.Message);
            }
        }

        private async System.Threading.Tasks.Task StopInputStageRunReviewJogCoreAsync()
        {
            BaseAxis axis = _inputStageRunReviewJogAxis;
            IDisposable scope = _inputStageRunReviewJogScope;
            bool startPending = _inputStageRunReviewJogStartPending;
            _inputStageRunReviewJogAxis = null;
            _inputStageRunReviewJogScope = null;
            _inputStageRunReviewJogStartPending = false;

            try
            {
                InputStageUnit stage = Machine != null ? Machine.InputStageUnit : null;
                if (stage != null)
                {
                    if (axis != null)
                        await stage.StopJogAsync(axis).ConfigureAwait(true);
                    else if (startPending)
                    {
                        if (stage.CameraX != null) await stage.StopJogAsync(stage.CameraX).ConfigureAwait(true);
                        if (stage.StageY != null) await stage.StopJogAsync(stage.StageY).ConfigureAwait(true);
                        if (stage.StageT != null) await stage.StopJogAsync(stage.StageT).ConfigureAwait(true);
                    }
                }
            }
            finally
            {
                if (scope != null)
                    scope.Dispose();
            }
        }

        private async void StopInputStageRunReviewActionAsync(
            InputStageRunReviewDialog dialog,
            string reason)
        {
            try
            {
                if (Controller != null)
                    Controller.CancelInputStageRunReviewAction();

                if (_inputStageRunReviewVisionTestDialog != null &&
                    !_inputStageRunReviewVisionTestDialog.IsDisposed)
                {
                    await _inputStageRunReviewVisionTestDialog.RequestClose().ConfigureAwait(true);
                }

                StopInputStageRunReviewEmbeddedVision(
                    dialog,
                    true,
                    "Review STOP으로 내장 Wafer Vision을 정지했습니다.");
                await StopInputStageRunReviewJogCoreAsync().ConfigureAwait(true);
                if (dialog != null && !dialog.IsDisposed)
                {
                    bool busy = Controller != null && Controller.IsInputStageRunReviewActionBusy;
                    dialog.SetBusy(busy, string.IsNullOrWhiteSpace(reason)
                        ? "Review 수동 동작 정지를 요청했습니다."
                        : reason);
                }
            }
            catch (Exception ex)
            {
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetBusy(false, "Review 수동 동작 정지 실패: " + ex.Message);
            }
        }

        private async void RunInputStageReviewThetaCorrectionAsync(InputStageRunReviewDialog dialog)
        {
            InputStageUnit stage = Machine != null ? Machine.InputStageUnit : null;
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (stage == null || stage.StageT == null || wafer == null)
            {
                if (dialog != null)
                    dialog.SetBusy(false, "StageT 또는 InputStage Wafer 정보가 없습니다.");
                return;
            }

            double referenceT = stage.ResolveWaferAlignReferenceT();
            double correctedT = stage.StageT.ActualPosition;
            double offsetT = correctedT - referenceT;
            string limitReason = string.Empty;
            if (Math.Abs(offsetT) <= 0.000001 ||
                !stage.IsWaferAlignThetaOffsetWithinLimit(offsetT, out limitReason))
            {
                dialog.SetBusy(false,
                    Math.Abs(offsetT) <= 0.000001
                        ? "T 보정 Offset이 0이라 저장할 수 없습니다."
                        : limitReason);
                return;
            }

            DialogResult confirm = QMC.Common.MessageDialog.Show(
                dialog,
                "현재 StageT 위치를 T 보정값으로 저장하시겠습니까?\r\n" +
                "Reference=" + referenceT.ToString("F6") + "\r\n" +
                "Current=" + correctedT.ToString("F6") + "\r\n" +
                "Offset=" + offsetT.ToString("F6") +
                "\r\n저장 후 Die Mapping 재실행이 필요합니다.",
                "InputStage Review",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            ClearInputStageRunReviewPendingOffset();

            await RunInputStageReviewOneShotAsync(
                dialog,
                "T Correction",
                (inputStage, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    wafer.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage };
                    inputStage.SetCurrentWaferMaterial(wafer);
                    inputStage.ApplyWaferAlignThetaResult(referenceT, correctedT, offsetT);
                    MaterialStateService.SaveInputStageThetaAlignResult(
                        wafer,
                        referenceT,
                        correctedT,
                        offsetT);
                    return System.Threading.Tasks.Task.FromResult(
                        "T 보정값을 저장했습니다. Die Mapping을 다시 실행하세요. Offset=" + offsetT.ToString("F6"));
                }).ConfigureAwait(true);

            if (dialog != null && !dialog.IsDisposed && !wafer.HasInputStageDieMappingResult)
            {
                dialog.SetWorkflowState(
                    wafer.WaferId,
                    ActiveRecipeName,
                    QMC.CDT320.VisionComm.VisionHub.Wafer != null &&
                    QMC.CDT320.VisionComm.VisionHub.Wafer.IsConnected,
                    wafer.HasInputStageAlignResult && wafer.HasInputStageThetaAlignResult,
                    wafer.DieMapFrameObjId,
                    false,
                    "MAPPING REQUIRED");
                dialog.SetReviewValid(false, "MAPPING REQUIRED");
            }
        }

        private async void RunInputStageReviewDieDetectionAsync(InputStageRunReviewDialog dialog)
        {
            DieMapEntry entry = dialog != null ? dialog.SelectedDie : null;
            InputStageUnit stage = Machine != null ? Machine.InputStageUnit : null;
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (entry == null || stage == null || wafer == null)
            {
                if (dialog != null)
                    dialog.SetBusy(false, "다이 검출 기준 Die/Stage/Wafer 정보가 없습니다.");
                return;
            }

            string thetaReason;
            if (!MaterialStateService.IsInputStageThetaAlignComplete(wafer, out thetaReason))
            {
                dialog.SetBusy(false, thetaReason);
                return;
            }

            DialogResult confirm = QMC.Common.MessageDialog.Show(
                dialog,
                "현재 Vision 화면에서 선택 Die 중심을 검출하시겠습니까?\r\n" +
                "UID=" + (entry.DieUid ?? "") + "\r\n" +
                "기준 X=" + entry.PosX.ToString("F6") + ", Y=" + entry.PosY.ToString("F6"),
                "InputStage Review",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            ClearInputStageRunReviewPendingOffset();
            string detectedWaferId = dialog.WaferId;
            string detectedMappingRevision = dialog.MappingRevision;
            string detectedDieUid = entry.DieUid ?? string.Empty;
            int detectedDieMapX = entry.DieMapX;
            int detectedDieMapY = entry.DieMapY;
            double detectedReferenceX = entry.PosX;
            double detectedReferenceY = entry.PosY;
            string detectedDraftSignature = BuildInputStageRunReviewDraftSignature(dialog);
            double detectedOffsetX = 0.0;
            double detectedOffsetY = 0.0;
            bool detectionSucceeded = false;

            await RunInputStageReviewOneShotAsync(
                dialog,
                "Die Detection",
                async (inputStage, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    inputStage.ApplyWaferAlignThetaResult(
                        wafer.InputStageAlignReferenceT,
                        wafer.InputStageAlignCorrectedT,
                        wafer.InputStageAlignOffsetT);

                    if (inputStage.EjectPinZ != null && inputStage.Recipe != null && inputStage.Recipe.EjectPinZ != null)
                    {
                        int ejectResult = await inputStage.MoveInputStageAxis(
                            WaferStageAxis.EjectPinZ,
                            inputStage.Recipe.EjectPinZ.AvoidPosition,
                            JogSpeedType.Fine,
                            0.0).ConfigureAwait(false);
                        if (ejectResult != 0)
                            throw new InvalidOperationException("Die 검출 전 EjectPinZ Avoid 이동 실패. result=" + ejectResult);
                    }

                    double targetT;
                    if (inputStage.TryResolveWaferAlignThetaTarget(out targetT))
                    {
                        int thetaResult = await inputStage.MoveInputStageAxis(
                            WaferStageAxis.WaferT,
                            targetT,
                            JogSpeedType.Fine,
                            0.0).ConfigureAwait(false);
                        if (thetaResult != 0)
                            throw new InvalidOperationException("Die 검출 전 StageT 보정 위치 이동 실패. result=" + thetaResult);
                    }

                    double currentX = inputStage.CameraX.ActualPosition;
                    double currentY = inputStage.StageY.ActualPosition;
                    VisionAlignResult vision = await RequestInputStageRunReviewDieVisionAsync(
                        inputStage,
                        entry,
                        currentX,
                        currentY,
                        token).ConfigureAwait(false);
                    if (vision == null ||
                        double.IsNaN(vision.DeltaX) || double.IsInfinity(vision.DeltaX) ||
                        double.IsNaN(vision.DeltaY) || double.IsInfinity(vision.DeltaY))
                    {
                        throw new InvalidOperationException("InputPickDie Vision 검출 결과가 유효하지 않습니다.");
                    }

                    // Wafer 채널 라이브 Delta는 카메라 순수 오프셋(raw)이므로 InputToBottomOffset 감산 없이 그대로 사용한다.
                    double centerDeltaX = vision.DeltaX;
                    double centerDeltaY = -vision.DeltaY;
                    double detectedCenterX = currentX + centerDeltaX;
                    double detectedCenterY = currentY + centerDeltaY;
                    double offsetX = detectedCenterX - detectedReferenceX;
                    double offsetY = detectedCenterY - detectedReferenceY;
                    string offsetReason;
                    if (!inputStage.IsManualDieDetectOffsetWithinLimit(offsetX, offsetY, out offsetReason))
                        throw new InvalidOperationException(offsetReason);

                    int moveResult = await inputStage.MoveVisionPointSafelyAsync(
                        detectedCenterX,
                        detectedCenterY,
                        JogSpeedType.Fine,
                        0.0,
                        "InputStageRunReview.DieDetectionCenterMove").ConfigureAwait(false);
                    if (moveResult != 0)
                        throw new InvalidOperationException("검출 Die 중심 좌표 이동 실패. result=" + moveResult);

                    detectedOffsetX = offsetX;
                    detectedOffsetY = offsetY;
                    detectionSucceeded = true;
                    return "Die 검출 완료. Offset 적용 버튼으로 Draft Map에 반영하세요. X=" +
                           offsetX.ToString("F6") + ", Y=" + offsetY.ToString("F6");
                }).ConfigureAwait(true);

            if (detectionSucceeded && dialog != null && !dialog.IsDisposed)
            {
                _inputStageRunReviewOffsetPending = true;
                _inputStageRunReviewPendingOffsetX = detectedOffsetX;
                _inputStageRunReviewPendingOffsetY = detectedOffsetY;
                _inputStageRunReviewPendingOffsetWaferId = detectedWaferId;
                _inputStageRunReviewPendingOffsetMappingRevision = detectedMappingRevision;
                _inputStageRunReviewPendingOffsetDieUid = detectedDieUid;
                _inputStageRunReviewPendingOffsetDieMapX = detectedDieMapX;
                _inputStageRunReviewPendingOffsetDieMapY = detectedDieMapY;
                _inputStageRunReviewPendingOffsetReferenceX = detectedReferenceX;
                _inputStageRunReviewPendingOffsetReferenceY = detectedReferenceY;
                _inputStageRunReviewPendingOffsetDraftSignature = detectedDraftSignature;
            }
        }

        private async System.Threading.Tasks.Task<VisionAlignResult> RequestInputStageRunReviewDieVisionAsync(
            InputStageUnit stage,
            DieMapEntry entry,
            double currentX,
            double currentY,
            System.Threading.CancellationToken token)
        {
            bool connected = QMC.CDT320.VisionComm.VisionHub.Wafer != null &&
                             QMC.CDT320.VisionComm.VisionHub.Wafer.IsConnected;
            AppSettings settings = AppSettingsStore.Current;
            if (!connected || (settings != null && !settings.UseVision))
            {
                return new VisionAlignResult
                {
                    DeltaX = entry.PosX - currentX,
                    DeltaY = currentY - entry.PosY,
                    DeltaTheta = 0.0
                };
            }

            QMC.CDT320.VisionComm.MatchResultDto match = await QMC.CDT320.VisionComm.AutoVisionRequestService.MatchAsync(
                QMC.CDT320.VisionComm.AutoVisionChannel.Wafer,
                QMC.CDT320.VisionComm.VisionToolIds.Wafer.DieFinder,
                0,
                5000,
                token).ConfigureAwait(false);
            if (match == null || !match.Success)
                return null;

            VisionAlignResult bottom = QMC.CDT320.Calibration.VisionCameraCalibrationTransform.ToAlignResult(
                QMC.CDT320.VisionComm.AutoVisionChannel.Wafer,
                match,
                0.15);
            if (bottom == null)
                return null;

            QMC.CDT320.Calibration.VisionCameraPixelCalibration camera =
                QMC.CDT320.Calibration.VisionCameraCalibrationTransform.ResolveCamera(
                    null,
                    QMC.CDT320.VisionComm.AutoVisionChannel.Wafer) ??
                new QMC.CDT320.Calibration.VisionCameraPixelCalibration();
            camera.EnsureDefaults(320.0, 240.0, 0.001, 0.001);
            if (match.HasImageSize)
                camera.ApplyImageSize(match.ImageWidthPixel, match.ImageHeightPixel);

            return new VisionAlignResult
            {
                DeltaX = bottom.DeltaX,
                DeltaY = camera.PixelToMmOffsetY(match.Y),
                DeltaTheta = bottom.DeltaTheta,
                PitchX = bottom.PitchX,
                PitchY = bottom.PitchY
            };
        }

        private void ApplyInputStageRunReviewPendingOffset(InputStageRunReviewDialog dialog)
        {
            if (!_inputStageRunReviewOffsetPending || dialog == null)
            {
                if (dialog != null)
                    dialog.SetBusy(false, "적용할 Die Detection Offset이 없습니다.");
                return;
            }

            if (!string.Equals(dialog.WaferId, _inputStageRunReviewPendingOffsetWaferId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(dialog.MappingRevision, _inputStageRunReviewPendingOffsetMappingRevision, StringComparison.OrdinalIgnoreCase))
            {
                ClearInputStageRunReviewPendingOffset();
                dialog.SetBusy(false, "Die Detection 이후 Wafer/Mapping이 변경되어 Offset을 적용할 수 없습니다.");
                return;
            }

            string currentSignature = BuildInputStageRunReviewDraftSignature(dialog);
            if (!string.Equals(
                currentSignature,
                _inputStageRunReviewPendingOffsetDraftSignature,
                StringComparison.Ordinal))
            {
                ClearInputStageRunReviewPendingOffset();
                dialog.SetBusy(false,
                    "Die Detection 이후 Review Draft 상태/좌표/순서가 변경되어 Offset을 적용할 수 없습니다. 다시 검출하세요.");
                return;
            }

            DieMap draft = dialog.DraftDieMap;
            DieMapEntry referenceEntry = draft != null && draft.Entries != null
                ? draft.Entries.FirstOrDefault(candidate => candidate != null &&
                    string.Equals(
                        candidate.DieUid ?? string.Empty,
                        _inputStageRunReviewPendingOffsetDieUid,
                        StringComparison.OrdinalIgnoreCase))
                : null;
            if (referenceEntry == null ||
                referenceEntry.DieMapX != _inputStageRunReviewPendingOffsetDieMapX ||
                referenceEntry.DieMapY != _inputStageRunReviewPendingOffsetDieMapY ||
                Math.Abs(referenceEntry.PosX - _inputStageRunReviewPendingOffsetReferenceX) > 0.000000001 ||
                Math.Abs(referenceEntry.PosY - _inputStageRunReviewPendingOffsetReferenceY) > 0.000000001)
            {
                ClearInputStageRunReviewPendingOffset();
                dialog.SetBusy(false,
                    "Die Detection 기준 Die UID/Grid/좌표가 현재 Draft와 달라 Offset을 적용할 수 없습니다. 다시 검출하세요.");
                return;
            }

            DialogResult confirm = QMC.Common.MessageDialog.Show(
                dialog,
                "검출 Offset을 Review Draft 전체 Die 좌표에 적용하시겠습니까?\r\n" +
                "기준 UID=" + _inputStageRunReviewPendingOffsetDieUid +
                ", Grid=(" + _inputStageRunReviewPendingOffsetDieMapX +
                "," + _inputStageRunReviewPendingOffsetDieMapY + ")\r\n" +
                "기준 X=" + _inputStageRunReviewPendingOffsetReferenceX.ToString("F6") +
                ", Y=" + _inputStageRunReviewPendingOffsetReferenceY.ToString("F6") + "\r\n" +
                "X=" + _inputStageRunReviewPendingOffsetX.ToString("F6") +
                ", Y=" + _inputStageRunReviewPendingOffsetY.ToString("F6"),
                "InputStage Review",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            string reason;
            if (dialog.ApplyDraftCoordinateOffset(
                _inputStageRunReviewPendingOffsetX,
                _inputStageRunReviewPendingOffsetY,
                out reason))
            {
                ClearInputStageRunReviewPendingOffset();
            }
            else
            {
                dialog.SetBusy(false, reason);
            }
        }

        private static string BuildInputStageRunReviewDraftSignature(InputStageRunReviewDialog dialog)
        {
            if (dialog == null || dialog.DraftDieMap == null || dialog.DraftDieMap.Entries == null)
                return string.Empty;

            DieMap map = dialog.DraftDieMap;
            var text = new System.Text.StringBuilder();
            text.Append(map.FrameObjId ?? string.Empty).Append('|')
                .Append(map.DieMapX).Append('|').Append(map.DieMapY).Append('|')
                .Append(map.OriginX.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(map.OriginY.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(dialog.StartDieUid ?? string.Empty).Append('|')
                .Append(string.Join(",", dialog.OrderedDieIds ?? new List<string>())).Append('|');

            foreach (DieMapEntry entry in map.Entries
                .Where(candidate => candidate != null)
                .OrderBy(candidate => candidate.DieUid ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.DieMapY)
                .ThenBy(candidate => candidate.DieMapX))
            {
                text.Append(entry.DieUid ?? string.Empty).Append(':')
                    .Append(entry.Index).Append(':')
                    .Append(entry.DieMapX).Append(':').Append(entry.DieMapY).Append(':')
                    .Append(entry.PosX.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(':')
                    .Append(entry.PosY.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(':')
                    .Append(entry.IsTarget ? '1' : '0').Append(':')
                    .Append((int)entry.Result).Append(':').Append(entry.BinCode).Append(':')
                    .Append(entry.SequenceNo).Append(';');
            }

            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text.ToString());
                return Convert.ToBase64String(sha.ComputeHash(bytes));
            }
        }

        private void ClearInputStageRunReviewPendingOffset()
        {
            _inputStageRunReviewOffsetPending = false;
            _inputStageRunReviewPendingOffsetX = 0.0;
            _inputStageRunReviewPendingOffsetY = 0.0;
            _inputStageRunReviewPendingOffsetWaferId = string.Empty;
            _inputStageRunReviewPendingOffsetMappingRevision = string.Empty;
            _inputStageRunReviewPendingOffsetDieUid = string.Empty;
            _inputStageRunReviewPendingOffsetDieMapX = 0;
            _inputStageRunReviewPendingOffsetDieMapY = 0;
            _inputStageRunReviewPendingOffsetReferenceX = 0.0;
            _inputStageRunReviewPendingOffsetReferenceY = 0.0;
            _inputStageRunReviewPendingOffsetDraftSignature = string.Empty;
        }

        private async void StartInputStageRunReviewEmbeddedVisionAsync(
            InputStageRunReviewDialog dialog)
        {
            if (_inputStageRunReviewEmbeddedVisionTransition)
                return;

            if (Controller == null || !Controller.IsInputStageRunReviewManualActive)
            {
                if (dialog != null)
                    dialog.SetBusy(false, "활성 Review Manual 세션이 없어 Wafer Vision을 시작할 수 없습니다.");
                return;
            }

            if (_inputStageRunReviewVisionTestDialog != null &&
                !_inputStageRunReviewVisionTestDialog.IsDisposed)
            {
                dialog.SetBusy(false, "기존 Vision Test 화면을 먼저 종료하세요.");
                return;
            }

            if (_inputStageRunReviewEmbeddedVisionScope != null ||
                (dialog != null && dialog.IsWaferVisionControlActive))
            {
                dialog.SetBusy(true, "내장 Wafer Vision이 이미 사용 중입니다.");
                return;
            }

            if (Controller.IsInputStageRunReviewActionBusy ||
                _inputStageRunReviewVisionTestScope != null)
            {
                dialog.SetBusy(false, "다른 Review 수동 동작이 진행 중입니다. STOP 후 다시 시도하세요.");
                return;
            }

            if (QMC.CDT320.VisionComm.VisionViewerRegistry.IsStreaming(
                QMC.CDT_320.Equipment.Vision.VisionViewerPorts.Wafer))
            {
                dialog.SetBusy(false,
                    "다른 화면에서 Wafer Vision Live를 사용 중입니다. 해당 Live를 먼저 종료하세요.");
                return;
            }

            IDisposable scope = null;
            _inputStageRunReviewEmbeddedVisionTransition = true;
            try
            {
                dialog.SetBusy(true, "Wafer Vision 안전 영역을 확보하고 있습니다. STOP으로 취소할 수 있습니다.");
                scope = await Controller.BeginInputStageRunReviewWorkAsync(
                    ManualMotionScopeKind.ProcessSequence,
                    "Embedded Wafer Vision",
                    System.Threading.CancellationToken.None).ConfigureAwait(true);

                System.Threading.CancellationToken actionToken = Controller.InputStageRunReviewActionToken;
                if (dialog == null ||
                    dialog.IsDisposed ||
                    !Controller.IsInputStageRunReviewManualActive ||
                    actionToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(
                        "Wafer Vision 시작 전에 Review Manual 세션이 종료되었습니다.");
                }

                // Scope 대기 중 다른 화면이 Live를 시작했을 수 있으므로 명령 허용 직전에 다시 확인합니다.
                if (QMC.CDT320.VisionComm.VisionViewerRegistry.IsStreaming(
                    QMC.CDT_320.Equipment.Vision.VisionViewerPorts.Wafer))
                {
                    throw new InvalidOperationException(
                        "다른 화면에서 Wafer Vision Live를 사용 중입니다. 해당 Live를 먼저 종료하세요.");
                }

                _inputStageRunReviewEmbeddedVisionScope = scope;
                scope = null;
                if (!dialog.SetWaferVisionControlActive(
                    true,
                    "Wafer Vision 안전 영역을 확보했습니다. 상단 Live/Grab/측정 기능을 사용할 수 있습니다."))
                {
                    throw new InvalidOperationException("내장 Wafer Vision Viewer 구성에 실패했습니다.");
                }
                dialog.SetBusy(true,
                    "Wafer Vision 사용 중입니다. 종료 또는 STOP 후 다른 Review 동작을 실행하세요.");
                QMC.Common.Log.Write(
                    "Main",
                    UserSession.Name,
                    "InputStageRunReviewVision",
                    "내장 Wafer Vision 안전 Scope를 시작했습니다. - Start");
            }
            catch (OperationCanceledException)
            {
                if (scope != null)
                    scope.Dispose();
                DisposeInputStageRunReviewEmbeddedVisionScope();
                if (dialog != null && !dialog.IsDisposed)
                {
                    dialog.SetWaferVisionControlActive(false, string.Empty);
                    dialog.SetBusy(false, "Wafer Vision 시작이 STOP/취소되었습니다.");
                }
            }
            catch (Exception ex)
            {
                if (scope != null)
                    scope.Dispose();
                DisposeInputStageRunReviewEmbeddedVisionScope();
                if (dialog != null && !dialog.IsDisposed)
                {
                    dialog.SetWaferVisionControlActive(false, string.Empty);
                    dialog.SetBusy(false, "Wafer Vision 시작 실패: " + ex.Message);
                }
                QMC.Common.Log.Write(
                    "Main",
                    UserSession.Name,
                    "InputStageRunReviewVision",
                    "내장 Wafer Vision 시작 실패: " + ex.Message + " - Failed");
            }
            finally
            {
                _inputStageRunReviewEmbeddedVisionTransition = false;
            }
        }

        private void StopInputStageRunReviewEmbeddedVision(
            InputStageRunReviewDialog dialog,
            bool restoreViewer,
            string status)
        {
            try
            {
                // CAM_SWITCH OFF와 Viewer 수신 Thread 정지를 먼저 완료한 뒤 Resource Lease를 반환합니다.
                if (dialog != null && !dialog.IsDisposed)
                    dialog.StopWaferVision();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write(
                    "Main",
                    UserSession.Name,
                    "InputStageRunReviewVision",
                    "내장 Wafer Vision 정지 실패: " + ex.Message + " - Failed");
            }
            finally
            {
                DisposeInputStageRunReviewEmbeddedVisionScope();
                _inputStageRunReviewEmbeddedVisionTransition = false;
            }

            if (restoreViewer && dialog != null && !dialog.IsDisposed)
            {
                dialog.SetWaferVisionControlActive(false, status);
                bool busy = Controller != null && Controller.IsInputStageRunReviewActionBusy;
                dialog.SetBusy(busy, status);
            }
        }

        private void DisposeInputStageRunReviewEmbeddedVisionScope()
        {
            IDisposable scope = _inputStageRunReviewEmbeddedVisionScope;
            _inputStageRunReviewEmbeddedVisionScope = null;
            if (scope == null)
                return;

            try
            {
                scope.Dispose();
                QMC.Common.Log.Write(
                    "Main",
                    UserSession.Name,
                    "InputStageRunReviewVision",
                    "내장 Wafer Vision 안전 Scope를 종료했습니다. - End");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write(
                    "Main",
                    UserSession.Name,
                    "InputStageRunReviewVision",
                    "내장 Wafer Vision 안전 Scope 종료 실패: " + ex.Message + " - Failed");
            }
        }

        private async void OpenInputStageRunReviewVisionTest(InputStageRunReviewDialog dialog)
        {
            if (Controller == null || !Controller.IsInputStageRunReviewManualActive)
            {
                if (dialog != null)
                    dialog.SetBusy(false, "활성 Review Manual 세션이 없습니다.");
                return;
            }

            if (_inputStageRunReviewEmbeddedVisionScope != null ||
                (dialog != null && dialog.IsWaferVisionControlActive))
            {
                dialog.SetBusy(true, "내장 Wafer Vision을 먼저 종료한 뒤 Vision Test를 실행하세요.");
                return;
            }

            if (_inputStageRunReviewVisionTestDialog != null &&
                !_inputStageRunReviewVisionTestDialog.IsDisposed)
            {
                _inputStageRunReviewVisionTestDialog.BringToFront();
                _inputStageRunReviewVisionTestDialog.Activate();
                dialog.SetBusy(true, "Vision Test 화면이 열려 있습니다. 종료하거나 STOP을 누르세요.");
                return;
            }

            if (Controller.IsInputStageRunReviewActionBusy || _inputStageRunReviewVisionTestScope != null)
            {
                dialog.SetBusy(false, "다른 Review 수동 동작이 진행 중입니다. STOP 후 다시 시도하세요.");
                return;
            }

            IDisposable scope = null;
            try
            {
                dialog.SetBusy(true, "Vision Test 안전 영역을 확보하고 있습니다. STOP으로 취소할 수 있습니다.");
                scope = await Controller.BeginInputStageRunReviewWorkAsync(
                    ManualMotionScopeKind.ProcessSequence,
                    "Vision Test",
                    System.Threading.CancellationToken.None).ConfigureAwait(true);

                System.Threading.CancellationToken actionToken = Controller.InputStageRunReviewActionToken;
                if (dialog.IsDisposed ||
                    !Controller.IsInputStageRunReviewManualActive ||
                    actionToken.IsCancellationRequested)
                    throw new OperationCanceledException("Vision Test 시작 전에 Review Manual 세션이 종료되었습니다.");

                _inputStageRunReviewVisionTestScope = scope;
                scope = null;
                WaferVisionTestDialog visionDialog = WaferVisionTestDialog.OpenReview(
                    dialog,
                    Controller.InputStageRunReviewActionToken);
                _inputStageRunReviewVisionTestDialog = visionDialog;
                visionDialog.FormClosed += InputStageRunReviewVisionTestDialog_FormClosed;
                dialog.SetBusy(true, "Vision Test 화면이 열려 있습니다. 종료하거나 STOP을 누르세요.");
            }
            catch (OperationCanceledException)
            {
                if (scope != null)
                    scope.Dispose();
                DisposeInputStageRunReviewVisionTestScope();
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetBusy(false, "Vision Test 시작이 STOP/취소되었습니다.");
            }
            catch (Exception ex)
            {
                if (scope != null)
                    scope.Dispose();
                DisposeInputStageRunReviewVisionTestScope();
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetBusy(false, "Vision Test 시작 실패: " + ex.Message);
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReviewVisionTest",
                    "Vision Test 시작 실패: " + ex.Message + " - Failed");
            }
        }

        private void InputStageRunReviewVisionTestDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            WaferVisionTestDialog visionDialog = sender as WaferVisionTestDialog;
            if (visionDialog != null)
                visionDialog.FormClosed -= InputStageRunReviewVisionTestDialog_FormClosed;

            if (ReferenceEquals(_inputStageRunReviewVisionTestDialog, visionDialog))
                _inputStageRunReviewVisionTestDialog = null;
            DisposeInputStageRunReviewVisionTestScope();

            InputStageRunReviewDialog reviewDialog = _inputStageRunReviewDialog;
            if (reviewDialog != null && !reviewDialog.IsDisposed &&
                Controller != null && Controller.IsInputStageRunReviewManualActive)
            {
                reviewDialog.SetBusy(false, "Vision Test 화면을 종료했습니다.");
            }
            else if (reviewDialog != null && !reviewDialog.IsDisposed)
            {
                // Global STOP/Coordinator 종료 중에는 자식 창이 완전히 닫힌 뒤 부모 Review를 닫는다.
                reviewDialog.CloseFromSequence();
            }
        }

        private void DisposeInputStageRunReviewVisionTestScope()
        {
            IDisposable scope = _inputStageRunReviewVisionTestScope;
            _inputStageRunReviewVisionTestScope = null;
            if (scope != null)
            {
                try { scope.Dispose(); }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReviewVisionTest",
                        "Vision Test Review 작업 스코프 종료 실패: " + ex.Message + " - Failed");
                }
            }
        }

        private void StartRunReviewBuzzer()
        {
            try
            {
                if (OpPanelMonitor != null)
                    OpPanelMonitor.StartRunReviewBuzzer();
                else
                    Machine?.OpPanelUnit?.Buzzer?.On();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "사용자 확인 부저 시작 실패: " + ex.Message + " - Failed");
            }
        }

        private void StopRunReviewBuzzer()
        {
            try
            {
                if (OpPanelMonitor != null)
                    OpPanelMonitor.StopBuzzer();
                else
                    Machine?.OpPanelUnit?.Buzzer?.Off();

                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "사용자가 리뷰 화면에서 부저 정지를 요청했습니다. - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "사용자 확인 부저 정지 실패: " + ex.Message + " - Failed");
            }
        }

        private void EndRunReviewBuzzer()
        {
            try
            {
                if (OpPanelMonitor != null)
                    OpPanelMonitor.EndRunReviewBuzzer();
                else
                    Machine?.OpPanelUnit?.Buzzer?.Off();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "사용자 확인 부저 종료 처리 실패: " + ex.Message + " - Failed");
            }
        }

        private void OnVisionHubChanged()
        {
            if (InvokeRequired) { BeginInvoke(new Action(OnVisionHubChanged)); return; }
            // VisionHub 연결 상태를 상단 VIS 표시로 반영합니다.
            bool connected = QMC.CDT320.VisionComm.VisionHub.AllConnected;
            var h = connected ? "O" : "X";
            SetTextIfChanged(lblBarcodeValue, "VIS " + h);
            SetForeColorIfChanged(lblBarcodeValue,
                connected ? System.Drawing.Color.LightGreen : System.Drawing.Color.White);
            // 상단 VISION 점등도 실제 연결 상태에 동기화(끊기면 소등).
            if (dotVision != null) dotVision.IsOn = connected;
        }

        /// <summary>현재 활성 레시피 명칭을 Vision Main 채널로 재전송. 재연결 성공 시 + Vision 의 RECIPEREQ 요청 시 호출된다.</summary>
        private void BroadcastCurrentRecipeToVision()
        {
            try
            {
                string name = ActiveRecipeName;
                if (string.IsNullOrWhiteSpace(name) || name == "-")
                {
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "SYS", "VISION-RECIPE",
                        "Vision 레시피 요청 — 응답 스킵(활성 레시피 없음: ActiveRecipeName='" + (name ?? "null") + "'). 핸들러에서 레시피/프로젝트 로드 필요.");
                    return;
                }
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "SYS", "VISION-RECIPE",
                    "Vision 레시피 요청 → 응답: name=" + name);
                _ = QMC.CDT320.VisionComm.VisionHub.BroadcastRecipeAsync(name);
            }
            catch (Exception ex)
            {
                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "SYS", "VISION-RECIPE",
                    "Vision 레시피 요청 응답 실패: " + ex.Message); } catch { }
            }
        }

        private void RefreshStateBig()
        {
            var ms = Controller?.Status ?? QMC.CDT320.EquipmentStatus.Idle;
            SetTextIfChanged(lblStateBig, FormatEquipmentStatus(ms));
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
            SetForeColorIfChanged(lblStateBig, c);
        }

        private static string FormatEquipmentStatus(QMC.CDT320.EquipmentStatus status)
        {
            switch (status)
            {
                case QMC.CDT320.EquipmentStatus.AutoRunning:
                    return "AUTO RUN";
                case QMC.CDT320.EquipmentStatus.ManualRunning:
                    return "MANUAL";
                case QMC.CDT320.EquipmentStatus.CycleStopped:
                    return "CYCLE STOP";
                default:
                    return status.ToString().ToUpper();
            }
        }

        private void TimerClock_Tick(object sender, EventArgs e) => UpdateClock();
        private void UpdateClock()
        {
            SetTextIfChanged(lblTimeValue, DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss"));
        }

        private static void SetTextIfChanged(Control control, string text)
        {
            if (control == null)
                return;

            text = text ?? string.Empty;
            if (!string.Equals(control.Text, text, StringComparison.Ordinal))
                control.Text = text;
        }

        private static void SetForeColorIfChanged(Control control, System.Drawing.Color color)
        {
            if (control != null && control.ForeColor != color)
                control.ForeColor = color;
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

        // RealtimeCollisionSupervisor가 실시간 충돌 위험 감지 시 호출하는 전축 하드정지 동작(사용자 정책: 전축 하드정지).
        // 10ms 감시 루프에서 동기 호출되므로 빠르게 모든 축을 EStop 한다. 알람은 supervisor가 별도로 발생시킨다.
        private void StopAllAxesForCollisionSupervisor()
        {
            List<BaseAxis> axes;
            try
            {
                axes = CurrentAxes();
            }
            catch
            {
                axes = null;
            }

            if (axes == null)
                return;

            foreach (BaseAxis axis in axes)
            {
                try
                {
                    if (axis != null)
                        axis.EStop();
                }
                catch
                {
                }
            }
        }

        private void ApplyMotionAxisDataToMachine()
        {
            try
            {
                if (Machine == null) return;
                List<BaseAxis> axes = QMC.CDT320.Ajin.AjinAxisRegistry.GetOrderedAxes(Machine);
                QMC.CDT320.Ajin.AjinFactory.ApplyPersistedAxisValues(axes);
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
                else
                    AjinFactory.ApplyInputSimulation(input, false);
                return;
            }

            BaseDigitalOutput output = node as BaseDigitalOutput;
            if (output != null)
            {
                if (simulation)
                    AjinFactory.ApplyOutputSimulation(output, true);
                else if (dryRun)
                    AjinFactory.ApplyOutputSimulation(output, false);
                else
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
                else
                    QMC.CDT320.Ajin.CylinderSettingsStore.Apply(cylinder);
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

            if (!SaveMaterialStateBeforeApplicationExit())
            {
                e.Cancel = true;
                _applicationExitRequested = false;
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
            try
            {
                InputStageRunReviewDialog reviewDialog = _inputStageRunReviewDialog;
                if (reviewDialog != null && !reviewDialog.IsDisposed)
                {
                    if (Machine != null && Machine.InputStageUnit != null)
                    {
                        try
                        {
                            Machine.InputStageUnit.FailUserConfirmFromUi(
                                "애플리케이션 종료로 InputStage Review 대기를 종료했습니다.");
                        }
                        catch { }
                    }
                    reviewDialog.CloseFromSequence();
                }
            }
            catch { }
            try { AlarmResponse?.Dispose(); } catch { }
            try { OpPanelMonitor?.Dispose(); } catch { }
            try { CollisionSupervisor?.Dispose(); } catch { }
            try { MotionMonitor?.Dispose(); } catch { }
            try { IoScan?.Dispose(); } catch { }
            try { if (_jogPopup != null && !_jogPopup.IsDisposed) _jogPopup.Dispose(); } catch { }
            try { if (_axisPositionPopup != null && !_axisPositionPopup.IsDisposed) _axisPositionPopup.Dispose(); } catch { }
            try { Bridge?.Dispose(); } catch { }
            try { QMC.CDT320.VisionComm.VisionReconnectWatchdog.Stop(); } catch { }
            try { QMC.CDT320.VisionComm.VisionHub.DisconnectAll(); } catch { }
            try { QMC.CDT320.Ajin.AjinSystem.Close(); } catch { }
            try { QMC.Common.Logging.EventLogger.FlushPending(1000); } catch { }
            if (Controller != null)
            {
                Controller.OperatorMessageRequested -= OnOperatorMessageRequested;
                Controller.InputStageRunReviewManualStateChanged -= OnInputStageRunReviewManualStateChanged;
            }
            if (Machine != null && Machine.InputStageUnit != null)
            {
                Machine.InputStageUnit.UserConfirmRequested -= OnInputStageUserConfirmRequested;
                Machine.InputStageUnit.UserConfirmWaitEnded -= OnInputStageUserConfirmWaitEnded;
                Machine.InputStageUnit.UserConfirmProcessingFailed -= OnInputStageUserConfirmProcessingFailed;
            }
            Lang.LanguageChanged    -= OnLocalizationChanged;
            UserSession.UserChanged -= OnUserChanged;
            base.OnFormClosing(e);
        }

        private bool SaveMaterialStateBeforeApplicationExit()
        {
            try
            {
                if (_materialStateSavedForExit)
                    return true;

                bool saved = MaterialStateService.TryFlushPendingSave("ApplicationExit");
                if (saved)
                {
                    _materialStateSavedForExit = true;
                    Log.Write("Main", UserSession.Name, "ApplicationExit",
                        "Material state saved before application exit. file=" + MaterialSnapshotStore.SnapshotPath + " - Ok");
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        UserSession.Name,
                        "APP-EXIT-SAVE",
                        "Material state saved before application exit.");
                    return true;
                }

                string message =
                    "Material 상태 저장에 실패했습니다.\r\n" +
                    "저장되지 않은 작업 정보가 손실될 수 있습니다.\r\n\r\n" +
                    "그래도 프로그램을 종료하시겠습니까?\r\n\r\n" +
                    "파일: " + MaterialSnapshotStore.SnapshotPath;

                Log.Write("Main", UserSession.Name, "ApplicationExit",
                    "Material state save failed before application exit. file=" +
                    MaterialSnapshotStore.SnapshotPath + " - Failed");
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "APP-EXIT-SAVE-FAIL",
                    "Material state save failed before application exit. file=" +
                    MaterialSnapshotStore.SnapshotPath);

                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    message,
                    "종료",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    Log.Write("Main", UserSession.Name, "ApplicationExit",
                        "Application exit continued by user after material state save failure. file=" +
                        MaterialSnapshotStore.SnapshotPath + " - Check");
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Warning,
                        UserSession.Name,
                        "APP-EXIT-SAVE-SKIP",
                        "Application exit continued by user after material state save failure. file=" +
                        MaterialSnapshotStore.SnapshotPath);
                    return true;
                }

                Log.Write("Main", UserSession.Name, "ApplicationExit",
                    "Application exit canceled by user after material state save failure. file=" +
                    MaterialSnapshotStore.SnapshotPath + " - Canceled");
                return false;
            }
            catch (Exception ex)
            {
                string message =
                    "Material 상태 저장 중 예외가 발생했습니다.\r\n" +
                    "저장되지 않은 작업 정보가 손실될 수 있습니다.\r\n\r\n" +
                    "그래도 프로그램을 종료하시겠습니까?\r\n\r\n" +
                    "원인: " + ex.Message;

                Log.Write("Main", UserSession.Name, "ApplicationExit",
                    "Material state save exception before application exit: " + ex.Message + " - Failed");
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "APP-EXIT-SAVE-EXCEPTION",
                    "Material state save exception before application exit: " + ex.Message);

                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    message,
                    "종료",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    Log.Write("Main", UserSession.Name, "ApplicationExit",
                        "Application exit continued by user after material state save exception: " +
                        ex.Message + " - Check");
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Warning,
                        UserSession.Name,
                        "APP-EXIT-SAVE-EXCEPTION-SKIP",
                        "Application exit continued by user after material state save exception: " +
                        ex.Message);
                    return true;
                }

                Log.Write("Main", UserSession.Name, "ApplicationExit",
                    "Application exit canceled by user after material state save exception: " +
                    ex.Message + " - Canceled");
                return false;
            }
            finally
            {
            }
        }
    }
}
