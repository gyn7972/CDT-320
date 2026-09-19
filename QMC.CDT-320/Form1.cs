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
        // [종료 저장 2026-08-17] 폐루프 런타임 오프셋 종료 저장 1회 보장(종료 경로가 중복 호출돼도 안전).
        private bool _runtimeOffsetsSavedForExit;
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

                OnSynchronousParameterSaveSucceeded(null);
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
                    else
                    {
                        // [사용자 지시 2026-08-12] 보드 오픈(.mot 로드) 후 화면 PROFILE 설정
                        // (사다리꼴/SCurve + Acc/Dec Jerk %)과 INPOSITION 설정(High/Low/Unused)만
                        // 축별로 보드에 적용한다. 다른 Setup 항목은 쓰지 않는다(전면 Write 금지 정책 유지).
                        foreach (BaseAxis axis in axes)
                        {
                            var ajinAxis = axis as AjinAxis;
                            if (ajinAxis != null)
                            {
                                ajinAxis.ApplyProfileSetupToBoard();
                                ajinAxis.ApplyInPositionSetupToBoard();
                            }
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
            bool ignoredMaterialOnlyBlock;
            return TryValidateMachineRecipeChange(recipeName, out ignoredMaterialOnlyBlock, out reason);
        }

        // [강제 Recipe 변경 2026-08-09] materialOnlyBlock=true 면 물리 센서 제품 감지 없이
        // Material 데이터 잔재만으로 막힌 상태다. 작업자 확인 후 ForceClearInMachineMaterialForRecipeChange 로 진행할 수 있다.
        internal bool TryValidateMachineRecipeChange(
            string recipeName,
            out bool materialOnlyBlock,
            out string reason)
        {
            reason = string.Empty;
            materialOnlyBlock = false;
            if (Controller == null)
            {
                reason = "MachineController가 준비되지 않았습니다.";
                return false;
            }

            bool materialRecipeRestore;
            return Controller.TryValidateRecipeChange(
                NormalizeRecipeName(recipeName),
                out materialRecipeRestore,
                out materialOnlyBlock,
                out reason);
        }

        internal bool ForceClearInMachineMaterialForRecipeChange(string recipeName, out string detail)
        {
            detail = string.Empty;
            if (Controller == null)
            {
                detail = "MachineController가 준비되지 않았습니다.";
                return false;
            }

            return Controller.ForceClearInMachineMaterial(NormalizeRecipeName(recipeName), out detail);
        }

        internal bool LoadMachineRecipe(string recipeName)
        {
            return LoadMachineRecipe(recipeName, false);
        }

        // To do: [시작 레시피 자동 로드] startupAutoLoad는 기동 자동 적용 전용 - 알람 게이트만 면제된다.
        internal bool LoadMachineRecipe(string recipeName, bool startupAutoLoad)
        {
            // 기동 복원만 기존 Load 경로를 사용합니다. 일반 적용은 초기화/저장 보호 구간을 공유합니다.
            if (!startupAutoLoad)
            {
                bool cancelled;
                string reason;
                return TryApplyMachineRecipe(
                    new QMC.CDT320.Recipes.RecipeProject { FileName = recipeName }, false, out cancelled, out reason);
            }

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
                        startupAutoLoad,
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
            bool broadcastVision,
            QMC.CDT320.Recipes.RecipeProject preparedProject = null)
        {
            QMC.CDT320.Recipes.RecipeProject project = preparedProject ??
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
            bool cancelled;
            string reason;
            return TryApplyMachineRecipe(project, false, out cancelled, out reason);
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

                OnSynchronousParameterSaveSucceeded(normalizedRecipeName);
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

        private void AutoConnectBarcodeReaders()
        {
            if (Machine == null)
                return;

            // USE는 공정 적용 여부이며, 설정 화면 시험을 위해 두 채널의 COM Port는 시작 시 모두 엽니다.
            // 포트 Open만 수행하고 NLV-5201 판독 명령은 ReadAsync에서만 송신합니다.
            TryOpenBarcodeReaderAtStartup(Machine.WaferBarcodeReader);
            TryOpenBarcodeReaderAtStartup(Machine.BinBarcodeReader);
        }

        private static void TryOpenBarcodeReaderAtStartup(IBarcodeReader reader)
        {
            if (reader == null)
                return;

            try
            {
                if (reader.TryOpen())
                    return;

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    "SYS",
                    "BARCODE-AUTO-CONNECT",
                    reader.ReaderName + " 시작 자동 연결에 실패했습니다. 프로그램 기동은 계속합니다.");
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    "SYS",
                    "BARCODE-AUTO-CONNECT",
                    reader.ReaderName + " 시작 자동 연결 예외: " + ex.Message);
            }
        }

        private void CloseBarcodeReadersOnShutdown()
        {
            if (Machine == null)
                return;

            IBarcodeReader inputReader = Machine.WaferBarcodeReader;
            IBarcodeReader outputReader = Machine.BinBarcodeReader;
            CloseBarcodeReaderOnShutdown(inputReader);
            if (!ReferenceEquals(outputReader, inputReader))
                CloseBarcodeReaderOnShutdown(outputReader);
        }

        private static void CloseBarcodeReaderOnShutdown(IBarcodeReader reader)
        {
            if (reader == null)
                return;

            try
            {
                IDisposable disposable = reader as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
                else
                    reader.Close();
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    "SYS",
                    "BARCODE-CLOSE",
                    reader.ReaderName + " 종료 연결 해제 예외: " + ex.Message);
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
        // Live 중 Jog가 Wafer Vision scope를 재사용했는지. 재사용이면 Jog 정지 시 그 scope를
        // 반환하지 않는다(Live 유지). 2026-08-17 팀장님 지시.
        private bool _inputStageRunReviewJogReusedVisionScope;
        private bool _inputStageRunReviewOffsetPending;
        // Die 검출이 실제 비전 측정이 아니라 UseVision=false 시뮬레이션 경로로 만들어졌는지.
        // 운전자에게 "실제 검출이 아님"을 알리고, 시뮬 offset이 실측처럼 취급되지 않게 하려고 둔다.
        private bool _inputStageRunReviewDieDetectionSimulated;
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
            QMC.CDT320.Recipes.RecipeInputMapSource.ModeActivated += OnInputMapModeActivated;
            Disposed += (sender, args) => QMC.CDT320.Recipes.RecipeInputMapSource.ModeActivated -= OnInputMapModeActivated;
            // To do: [앱 아이콘] 메인 창 타이틀바/작업표시줄 아이콘 - exe에 박힌 로고 적용 (2026-08-05 지시).
            QMC.CDT_320.Ui.AppIcons.ApplyMainIcon(this);
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
            btnTabExit.Click += async (s, e) => await RequestApplicationExitAsync();
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
                btnDoorToggle.Text = Lang.Display("DOOR\r\nCLOSE");
                btnDoorToggle.BackColor = Color.FromArgb(48, 92, 76);
                btnDoorToggle.FlatAppearance.BorderColor = Color.FromArgb(110, 150, 136);
            }
            else
            {
                btnDoorToggle.Text = Lang.Display("DOOR\r\nOPEN");
                btnDoorToggle.BackColor = Color.FromArgb(122, 46, 46);
                btnDoorToggle.FlatAppearance.BorderColor = Color.FromArgb(210, 90, 74);
            }
        }

        // 장비 메인 기동 순서.
        // 주의: 아래 초기화는 앞 단계의 상태를 다음 단계가 사용하는 구조이므로 호출 순서를 변경하지 않는다.
        private void Form1_Load(object sender, EventArgs e)
        {
            #region 01. 디자인 모드 차단 및 공통 설정

            // Visual Studio 디자이너에서는 하드웨어와 통신 객체를 초기화하지 않는다.
            if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            var cfg = AppSettingsStore.Load();
            if (!string.IsNullOrEmpty(cfg.Language)) Lang.SetLanguage(cfg.Language);
            QMC.Common.Alarms.AlarmManager.LanguageProvider = () => Lang.Current ?? "ko";

            #endregion

            #region 02. AJIN 축, IO, 실린더 구성

            // Simulation(BypassHardware) 중에는 실보드를 열지 않는다.
            QMC.CDT320.Ajin.AjinConfigStore.Load();
            QMC.CDT320.Ajin.AjinFactory.UseRealBoard = cfg.UseAjin && !cfg.BypassHardware;
            if (QMC.CDT320.Ajin.AjinFactory.UseRealBoard) 
                QMC.CDT320.Ajin.AjinSystem.Open(cfg.AjinIrqNo);
            QMC.CDT320.Ajin.IoSettingsStore.Load();
            QMC.CDT320.Ajin.CylinderManager.Initialize();

            QMC.CDT320.Ajin.AjinFactory.RegisterConfiguredAxes();

            #endregion

            #region 03. Vision 6채널 통신 준비

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

            #endregion

            #region 04. Machine 생성 및 런타임 모드 적용

            // 장비 유닛을 생성한 뒤 저장된 축, IO, 실린더 설정을 적용한다.
            Machine    = new CDT320_Machine();
            LoadMachineSettings();
            AutoConnectBarcodeReaders();

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

            // 1차 적용: Controller 생성 전에 Machine의 축, IO, 실린더 운전 모드를 확정한다.
            ApplyRuntimeMode();
            Bridge     = new SimulatorBridge(Machine);
            BeginSimulatorAutoConnect(cfg);
            Controller = new MachineController(Machine);
            Controller.SetActiveRecipeName(ActiveRecipeName);

            // 2차 적용: 생성된 Controller의 DryRun/GlobalDryRun 상태까지 동기화한다.
            ApplyRuntimeMode();
            Controller.ApplyStartupMachineRuntimeState(cfg);

            #endregion

            #region 05. 알람 대응 및 Material/LOT 복구

            AlarmResponse = new QMC.CDT320.Alarms.AlarmResponseService(Controller);
            AlarmResponse.Start();

            // Material Snapshot 복구 여부를 먼저 결정한 뒤 활성 LOT을 복구한다.
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

            #endregion

            #region 06. 모션, 충돌, IO, 조작반 실시간 감시

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

            #endregion

            #region 07. 선택 통신 및 로컬 카세트 시뮬레이터

            // SECS는 선택 기능이므로 기동 실패가 메인 프로그램 시작을 막지 않는다.
            try
            {
                SecsHost = new QMC.CDT320.Secs.SecsHost(5000);
            }
            catch { /* Optional startup failure ignored. */ }

            // AJIN을 사용하지 않는 구성에서만 로컬 카세트/피더 센서 시뮬레이터를 생성한다.
            if (!cfg.UseAjin)
            {
                CassetteDriver = new QMC.CDT320.Sim.SimCassetteDriver(
                    Machine.InputCassetteUnit,
                    Machine.InputFeederUnit,
                    Machine.OutputCassetteUnit,
                    Machine.OutputFeederUnit);
            }

            #endregion

            #region 08. Controller 이벤트 및 운전 로그 연결

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
            #endregion

            #region 09. 메인 탭 생성 및 화면에 연결

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

            #endregion

            #region 10. 다국어 및 사용자 세션 초기화

            // 하단 메뉴 다국어 키
            btnTabWork    .Tag = "i18n:tab.work";
            btnTabWorkInfo.Tag = "i18n:tab.workInfo";
            btnTabHistory .Tag = "i18n:tab.history";
            btnTabRecipe  .Tag = "i18n:tab.recipe";
            btnTabSettings.Tag = "i18n:tab.settings";
            btnTabUser    .Tag = "i18n:tab.user";
            btnTabExit    .Tag = "i18n:tab.exit";

            // 상단 상태 표시 다국어 키
            lblMapMode        .Tag = "i18n:status.mapEmpty";
            lblProjectCaption .Tag = "i18n:status.project";
            lblBarcodeCaption .Tag = "i18n:status.barcode";
            lblBinCaption     .Tag = "i18n:status.bin";
            lblVision         .Tag = "i18n:status.vision";
            lblPick           .Tag = "i18n:status.pick";
            lblReference      .Tag = "i18n:status.reference";

            // PICK/REFERENCE 표시는 갱신하는 코드가 없어 항상 ON/OFF로 고정되어 오해를 준다.
            // 표시할 상태가 정의될 때까지 숨긴다(VISION은 실제 연결 상태로 동작하므로 유지).
            HideUnusedStatusIndicators();

            // 화면 버전은 어셈블리 버전에서 자동으로 표시한다(수동 문자열과 실제 빌드 불일치 방지).
            ApplyAssemblyVersionText();

            // 상단 헤더 다국어 키
            lblTitle          .Tag = "i18n:app.title";
            lblUserCaption    .Tag = "i18n:header.user";
            lblTimeCaption    .Tag = "i18n:header.time";
            Lang.Bind(btnTopAlarm, "ALARM");
            Lang.Bind(btnBuzzerStop, "BUZZER\r\nSTOP");

            Lang.LanguageChanged    += OnLocalizationChanged;
            UserSession.UserChanged += OnUserChanged;

            OnLocalizationChanged();

            // 임시 TEST 운전: Release 빌드도 Debug와 동일하게 시작 즉시 Admin 세션으로 진입한다.
            // 사용자 승인 후 정식 로그인 정책으로 복귀할 때 이 자동 로그인 호출을 다시 DEBUG 조건으로 제한한다.
            QMC.CDT_320.Ui.Security.UserSession.ForceSet(
                "admin", QMC.CDT_320.Ui.Security.UserLevel.Admin);
            OnUserChanged();

            #endregion

            #region 11. 마지막 Recipe 및 Material 기본 상태 적용

            // 활성 Recipe 이름은 LoadMachineRecipe가 성공한 경우에만 설정된다.
            // To do: [시작 레시피 자동 로드] 무조건 마지막 레시피를 적용하고, 실패하면 실제 알람으로 알린다.
            // 기존 조건: 기동 자동 적용이 알람 게이트에 걸려 조용히 실패했고(EventLogger 기록만),
            //            사용자는 레시피 없는 상태로 시작해 수동으로 열어야 했다(2026-08-05).
            // 현재 기준: startupAutoLoad=true로 알람 게이트만 면제해 적용을 시도하고,
            //            마커 없음/적용 실패 모두 AlarmManager 실제 알람으로 사용자에게 알린다.
            // PickUp 파라미터 Config→Recipe 스코프 전환 이관: 저장된 모든 레시피에 장비 Config
            // 현재값을 기록한다(값이 이미 있는 레시피는 불변). 아래 레시피 적용보다 먼저 실행해
            // 활성 레시피도 기록된 파일에서 로드되게 한다.
            QMC.CDT320.InputStageUnit.MigratePickUpMotionRecipeToAllRecipes(
                Machine != null ? Machine.InputStageUnit : null);

            // 픽커 T 공통값도 같은 시점에 전 레시피로 기록한다(값이 있는 레시피는 불변).
            QMC.CDT320.PickerFrontConfig pickerTConfigSource =
                Machine != null && Machine.PickerFrontUnit != null ? Machine.PickerFrontUnit.Config : null;
            if (pickerTConfigSource != null)
                pickerTConfigSource.EnsurePositionObjects();
            QMC.CDT320.PickerCommonTeachingT.MigrateAllRecipes(
                pickerTConfigSource != null ? pickerTConfigSource.PickerT0 : null);

            try
            {
                var last = QMC.CDT320.Recipes.RecipeStore.LoadLastOrDefault();
                if (last != null)
                {
                    if (LoadMachineRecipe(last.FileName, true))
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
                        QMC.Common.Alarms.AlarmManager.Raise(
                            QMC.Common.Alarms.AlarmSeverity.Error,
                            "RECIPE-STARTUP-BLOCK",
                            "Form1",
                            "기동 시 마지막 Recipe(" + last.FileName + ") 적용에 실패했습니다. " +
                            "[레시피 → 프로젝트]에서 Recipe를 열어 적용한 뒤 운전을 시작하십시오.");
                    }
                }
                else
                {
                    // 기존 조건: 마커가 없으면 아무 기록 없이 지나갔다 — 레시피 없는 상태를 사용자가 알 수 없었다.
                    QMC.Common.Alarms.AlarmManager.Raise(
                        QMC.Common.Alarms.AlarmSeverity.Error,
                        "RECIPE-STARTUP-MISSING",
                        "Form1",
                        "기동 시 불러올 마지막 Recipe가 없습니다. " +
                        "[레시피 → 프로젝트]에서 Recipe를 열어 적용한 뒤 운전을 시작하십시오.");
                }
            }
            catch { /* Optional startup failure ignored. */ }

            if (!_materialSnapshotRestored && MaterialStorage.State.Cassettes.Count == 0)
                InitializeFreshMaterialStateForStartup(null);

            // [시작 로드 2026-08-17, 팀장님 지시] 폐루프 학습값을 기동 시 명시적으로 읽어 로그로 확인한다.
            // 기존에도 첫 접근 시 지연 로드는 됐지만, 파일이 비었거나 깨져 값이 0으로 초기화돼도
            // 가동 중에야 드러났다. 여기서 읽어두면 "이번 가동이 어떤 값으로 시작했는지"가 로그에 남는다.
            LoadRuntimeOffsetsOnStartup();

            #endregion

            #region 12. 기본 화면

            timerClock.Start();
            UpdateClock();

            // 기본 진입 화면
            ShowTab(MainTab.Work);

            #endregion
        }

        private void PromptMaterialRecoveryOnStartup()
        {
            try
            {
                if (!MaterialSnapshotStore.Exists())
                {
                    Log.Write("Main", UserSession.Name, "MaterialRecovery", "Material snapshot does not exist. New empty Material state will be created. - Ok");
                    InitializeFreshMaterialStateForStartup(null);
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
                    InitializeFreshMaterialStateForStartup(null);
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
                        InitializeFreshMaterialStateForStartup(null);
                        return;
                    }

                    Log.Write("Main", UserSession.Name, "MaterialRecovery", "Material snapshot restored by user. - Ok");
                    return;
                }

                Log.Write("Main", UserSession.Name, "MaterialRecovery", "Material snapshot ignored by user. New empty Material state will be created. - Ok");
                InitializeFreshMaterialStateForStartup(null);
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
                InitializeFreshMaterialStateForStartup(null);
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
            if (recipe != null)
                InitializeFreshMaterialStateForStartup(recipe);
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

        private async System.Threading.Tasks.Task RequestApplicationExitAsync()
        {
            if (_parameterSaveExitInProgress) return;
            _parameterSaveExitInProgress = true;
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
                        if (!await FlushParameterSavesBeforeExitAsync())
                            return;
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
                _parameterSaveExitInProgress = false;
            }
        }

        private void OnLocalizationChanged()
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnLocalizationChanged));
                return;
            }
            Lang.Apply(this);
            // 별도 창은 메인 폼의 Controls에 포함되지 않으므로 열린 팝업에도 표시 문구를 적용한다.
            foreach (Form dialog in Application.OpenForms.Cast<Form>().ToArray())
                if (!ReferenceEquals(dialog, this)) Lang.Apply(dialog);
            UpdateDoorToggleButton();
            RefreshStateBig();
            lblUserValue.Text = UserSession.Name + " (" + Lang.Display(UserSession.Level.ToString()) + ")";
        }

        private void OnUserChanged()
        {
            lblUserValue.Text = UserSession.Name + " (" + Lang.Display(UserSession.Level.ToString()) + ")";
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
            SetTextIfChanged(lblStateBig, Lang.Display(FormatEquipmentStatus(ms)));
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
            UpdateProcessingMaterialIds();
        }

        /// <summary>
        /// 상단 "Wafer ID"/"Bin ID"에 현재 공정 진행 중인 자재 ID를 표시한다.
        /// - Wafer ID: InputStage의 웨이퍼(바코드를 읽었으면 바코드값, 아니면 웨이퍼 ID)
        /// - Bin ID  : OutputStage의 GOOD / NG Bin (두 개를 함께 표시)
        /// 해당 위치에 자재가 없으면(배출 완료) "-"로 되돌린다.
        /// </summary>
        private void UpdateProcessingMaterialIds()
        {
            try
            {
                string input;
                string good;
                string ng;
                if (!MaterialStateService.TryGetProcessingDisplayIds(out input, out good, out ng))
                    return;
                SetTextIfChanged(lblBarcodeValue, input);

                // NG 카세트를 쓰지 않는 장비/설정에서는 NG가 항상 "-"로만 보여 잡음이 되므로 GOOD만 표시한다.
                bool showNg = IsNgBinDisplayEnabled() || ng != "-";
                SetTextIfChanged(lblBinValue,
                    showNg ? "GOOD " + good + "   NG " + ng : "GOOD " + good);
            }
            catch
            {
                // 상단 표시 실패로 UI 타이머를 멈추지 않는다.
            }
        }

        /// <summary>표시할 상태가 정의되지 않은 상단 인디케이터(PICK/REFERENCE)를 숨긴다.</summary>
        private void HideUnusedStatusIndicators()
        {
            try
            {
                if (dotPick != null) dotPick.Visible = false;
                if (lblPick != null) lblPick.Visible = false;
                if (dotReference != null) dotReference.Visible = false;
                if (lblReference != null) lblReference.Visible = false;
            }
            catch
            {
            }
        }

        /// <summary>
        /// 상단 버전 라벨을 AssemblyFileVersion으로 표시한다(예: v0.1.0).
        /// AssemblyVersion은 참조 호환성 때문에 1.0.0.0으로 고정하고, 릴리스 표기는 FileVersion만 올린다(사용자 확정).
        /// </summary>
        private void ApplyAssemblyVersionText()
        {
            try
            {
                if (lblVersion == null)
                    return;

                string fileVersion = System.Diagnostics.FileVersionInfo
                    .GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    .FileVersion;
                if (string.IsNullOrWhiteSpace(fileVersion))
                    return;

                // 4자리(0.1.0.0) 중 뒤 Revision은 표기에서 제외해 v0.1.0 형태로 보여준다.
                string[] parts = fileVersion.Split('.');
                string text = parts.Length >= 3
                    ? "v" + parts[0] + "." + parts[1] + "." + parts[2]
                    : "v" + fileVersion;
                SetTextIfChanged(lblVersion, text);
            }
            catch
            {
            }
        }

        /// <summary>NG 카세트 사용 설정 여부. 설정을 못 읽으면 표시하는 쪽(true)으로 둔다.</summary>
        private bool IsNgBinDisplayEnabled()
        {
            try
            {
                var cassette = Machine != null ? Machine.OutputCassetteUnit : null;
                return cassette == null || cassette.Config == null || cassette.Config.UseNgCassette;
            }
            catch
            {
                return true;
            }
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
            CloseBarcodeReadersOnShutdown();
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

        /// <summary>
        /// 기동 시 폐루프 런타임 오프셋 3종을 명시적으로 로드하고 현재 값을 로그로 남긴다.
        /// 값 자체는 각 서비스가 지연 로드로도 읽지만, 여기서 한 번 읽어두면
        /// "이번 가동이 어떤 학습값으로 시작했는지"가 기동 로그에 남아 값 변화 추적이 가능해진다.
        /// </summary>
        private void LoadRuntimeOffsetsOnStartup()
        {
            try
            {
                LogRuntimeOffsetStartupSnapshot("Pick",
                    QMC.CDT320.Sequencing.PickRuntimeOffsetService.IsEnabled, QMC.CDT320.Sequencing.PickRuntimeOffsetService.GetSnapshot());
                LogRuntimeOffsetStartupSnapshot("Place",
                    QMC.CDT320.Sequencing.PlaceRuntimeOffsetService.IsEnabled, QMC.CDT320.Sequencing.PlaceRuntimeOffsetService.GetSnapshot());

                double pickerZFront1;
                QMC.CDT320.Sequencing.PickerZRuntimeOffsetService.GetOffset(
                    QMC.CDT320.Sequencing.PickerSequenceSide.Front, 1, out pickerZFront1);
                Log.Write("Main", UserSession.Name, "StartupRuntimeOffset",
                    "PickerZ 런타임 오프셋 로드. enabled=" + QMC.CDT320.Sequencing.PickerZRuntimeOffsetService.IsEnabled +
                    ", front P1 Z=" + pickerZFront1.ToString("F4") + " - Ok");
            }
            catch (Exception ex)
            {
                Log.Write("Main", UserSession.Name, "StartupRuntimeOffset",
                    "폐루프 런타임 오프셋 기동 로드 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
        }

        private void LogRuntimeOffsetStartupSnapshot(
            string kind,
            bool enabled,
            QMC.CDT320.Sequencing.RuntimeOffsetSnapshot[] rows)
        {
            int sampleCount = 0;
            var builder = new System.Text.StringBuilder();
            if (rows != null)
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    QMC.CDT320.Sequencing.RuntimeOffsetSnapshot row = rows[i];
                    if (row == null)
                        continue;

                    if (row.HasSample)
                        sampleCount++;

                    if (builder.Length > 0)
                        builder.Append(" / ");
                    builder.Append(row.Side).Append(" P").Append(row.PickerNo)
                        .Append(" X=").Append(row.X.ToString("F4"))
                        .Append(" Y=").Append(row.Y.ToString("F4"))
                        .Append(" T=").Append(row.T.ToString("F4"));
                }
            }

            Log.Write("Main", UserSession.Name, "StartupRuntimeOffset",
                kind + " 런타임 오프셋 로드. enabled=" + enabled +
                ", rows=" + (rows != null ? rows.Length : 0) +
                ", withSample=" + sampleCount +
                ", values=" + (builder.Length > 0 ? builder.ToString() : "-") + " - Ok");
        }

        /// <summary>
        /// 폐루프 런타임 오프셋 3종(Pick/Place/PickerZ)을 종료 직전 디스크에 확정한다.
        /// 실패해도 종료를 막지 않는다 — 학습값은 재학습 가능하고, 종료를 붙잡으면 장비 운용이 막힌다.
        /// 성공/실패는 각각 로그로 남겨 다음 가동 시 값이 왜 달라졌는지 추적할 수 있게 한다.
        /// </summary>
        private void SaveRuntimeOffsetsBeforeApplicationExit()
        {
            if (_runtimeOffsetsSavedForExit)
                return;

            _runtimeOffsetsSavedForExit = true;

            bool pickSaved = false;
            bool placeSaved = false;
            bool pickerZSaved = false;
            try
            {
                pickSaved = QMC.CDT320.Sequencing.PickRuntimeOffsetService.TryFlushPendingSave("ApplicationExit");
                placeSaved = QMC.CDT320.Sequencing.PlaceRuntimeOffsetService.TryFlushPendingSave("ApplicationExit");
                pickerZSaved = QMC.CDT320.Sequencing.PickerZRuntimeOffsetService.TryFlushPendingSave("ApplicationExit");
            }
            catch (Exception ex)
            {
                Log.Write("Main", UserSession.Name, "ApplicationExit",
                    "런타임 오프셋 종료 저장 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }

            bool allSaved = pickSaved && placeSaved && pickerZSaved;
            Log.Write("Main", UserSession.Name, "ApplicationExit",
                "폐루프 런타임 오프셋 종료 저장. pick=" + pickSaved +
                ", place=" + placeSaved +
                ", pickerZ=" + pickerZSaved +
                " - " + (allSaved ? "Ok" : "Check"));
            QMC.Common.Logging.EventLogger.Write(
                allSaved
                    ? QMC.Common.Logging.EventKind.Event
                    : QMC.Common.Logging.EventKind.Warning,
                UserSession.Name,
                "APP-EXIT-RUNTIME-OFFSET-SAVE",
                "Runtime offset save before application exit. pick=" + pickSaved +
                ", place=" + placeSaved + ", pickerZ=" + pickerZSaved);
        }

        private bool SaveMaterialStateBeforeApplicationExit()
        {
            try
            {
                if (_materialStateSavedForExit)
                    return true;

                // [종료 저장 2026-08-17, 팀장님 지시] 폐루프 학습값(Pick/Place/PickerZ 런타임 오프셋)을
                // 종료 시 반드시 디스크에 확정한다. 기존에는 Material만 저장되고 이 3종은 종료 경로가 없어,
                // 지연 저장(1000ms 무음) 창에 종료가 걸리면 최신 학습분이 유실됐다.
                // 학습값은 재학습 가능한 데이터이므로 실패해도 종료를 막지 않고 로그만 남긴다.
                SaveRuntimeOffsetsBeforeApplicationExit();

                // [종료 풀 저장 2026-08-28 팀장님 지시] 종료 저장은 SaveMaterialInspectionDetail 설정과
                // 무관하게 검사 측정값 상세를 전부 포함한다(재시작 시 미기록 다이 측정값 유실 방지).
                MaterialSnapshotStore.BeginApplicationExitFullSave();
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
