using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.Common.Motion;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui.Dialogs;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class InputStagePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private System.Windows.Forms.Timer _timer;
        private bool _manualSequenceRunning;
        private SequenceStartMode _manualSequenceStartMode = SequenceStartMode.Resume;
        private string _lastMaterialDisplayKey = "";
        private const string LogSource = "INPUT-STAGE-PAGE";

        public InputStagePage()
        {
            InitializeComponent();
            BindLocalizedCaptions();
            ConfigureInfoLayoutForReadableText();
            WireEvents();

            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) =>
            {
                if (!ShouldRefreshVisible(this))
                    return;

                RefreshFromMachine();
            };
            VisibleChanged += (s, e) => { if (Visible) _timer.Start(); else _timer.Stop(); };
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private void BindLocalizedCaptions()
        {
            Lang.Bind(lblHeader, "INPUT STAGE");
            Lang.Bind(grpState, "WORK INFO");
            Lang.Bind(lblStageExistTitle, "STAGE EXIST");
            Lang.Bind(lblStageAlignTitle, "STAGE ALIGN");
            Lang.Bind(lblStageAlignOffsetTitle, "ALIGN OFFSET");
            Lang.Bind(lblStageBarcodeTitle, "STAGE BARCODE");
            Lang.Bind(lblStageChipAlignTitle, "STAGE CHIP ALIGN");
            Lang.Bind(lblStageChipAlignOffsetTitle, "DIE MAP OFFSET");
            Lang.Bind(lblStageFinishTitle, "STAGE FINISH");
            Lang.Bind(grpCounters, "COUNTER");
            Lang.Bind(lblNeedleUsingTitle, "NEEDLE USING");
            Lang.Bind(lblJellPadUsingTitle, "JELL PAD USING");
            Lang.Bind(grpInfo, "INFO");
            Lang.Bind(lblStageAxisYTitle, "STAGE AXIS Y");
            Lang.Bind(lblStageAxisTTitle, "STAGE AXIS T");
            Lang.Bind(lblStageAxisZTitle, "STAGE AXIS Z");
            Lang.Bind(lblStageAxisXTitle, "VISION AXIS X");
            Lang.Bind(label1, "NEEDLE AXIS X");
            Lang.Bind(lblNeedleAxisZTitle, "NEEDLE AXIS Z");
            Lang.Bind(label3, "EJECT PIN AXIS Z");
            Lang.Bind(lblNeedleVacuum, "NEEDLE VACUUM");
            Lang.Bind(grpCylinder, "NEEDLE INFO");
            Lang.Bind(lblExpendingTitle, "EXPENDING");
            Lang.Bind(lblNeedleUpDownTitle, "NEEDLE UP/DOWN");
            Lang.Bind(grpAction, "ACTION");
            Lang.Bind(btnWfAlign, "WAFER ALIGN");
            Lang.Bind(btnWfBarcode, "WAFER BARCODE");
            Lang.Bind(btnPrepareLoad, "PREP LOAD");
            Lang.Bind(btnDieMapping, "DIE MAPPING");
            Lang.Bind(btnPrepareUnload, "PREP UNLOAD");
            Lang.Bind(btnMoveAvoid, "AVOID");
            Lang.Bind(btnVisionWafer, "VISION: WAFER");
            Lang.Bind(btnStop, "STOP");
        }

        private Form1 GetHost() => FindForm() as Form1;

        private void WireEvents()
        {
            btnPrepareLoad.Click += async (s, e) => await RunSequenceAction("INPUT STAGE PREP LOAD", RunPrepareLoadAsync);
            btnWfAlign.Click += async (s, e) => await RunSequenceAction("INPUT STAGE ALIGN", RunAlignAsync);
            btnDieMapping.Click += async (s, e) => await RunSequenceAction("INPUT STAGE DIE MAPPING", RunDieMappingAsync);
            btnWfBarcode.Click += async (s, e) => await RunSequenceAction("INPUT STAGE WAFER BARCODE", RunMapLoadAsync);
            btnPrepareUnload.Click += async (s, e) => await RunSequenceAction("INPUT STAGE PREP UNLOAD", RunPrepareUnloadAsync);
            btnMoveAvoid.Click += async (s, e) => await RunSequenceAction("INPUT STAGE AVOID", RunMoveAvoidAsync);
            btnVisionWafer.Click += (s, e) => WaferVisionTestDialog.Open(this);
            btnStop.Click += async (s, e) => await StopManualActionAsync();
            materialDetailView.CreateDataRequested += MaterialDetailView_CreateDataRequested;
            materialDetailView.ClearDataRequested += MaterialDetailView_ClearDataRequested;
        }

        private async Task RunSequenceAction(string actionName, Func<Form1, Task<bool>> action)
        {
            IDisposable actionScope = null;
            bool showFailure = false;
            string exceptionMessage = null;
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null || action == null)
                    return;
                if (_manualSequenceRunning)
                    return;
                if (!ConfirmAction(actionName))
                    return;
                if (!TryAskManualSequenceStartMode(actionName, out _manualSequenceStartMode))
                    return;

                _manualSequenceRunning = true;
                SetSequenceButtonsEnabled(false);
                actionScope = host.Controller.BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "InputStagePage:" + actionName);
                CancellationToken manualToken = host.Controller.ManualOperationToken;
                SequenceFailureStore.Clear();
                WriteEvent("INPUT-STAGE-ACTION", actionName + " start");
                Task<bool> actionTask = action(host);
                Task cancelTask = WaitForCancellationAsync(manualToken);
                Task completed = await Task.WhenAny(actionTask, cancelTask).ConfigureAwait(true);
                if (completed == cancelTask)
                {
                    ObserveManualActionTask(actionTask, actionName);
                    WriteEvent("INPUT-STAGE-CANCEL", actionName + " canceled by stop.");
                    return;
                }

                bool ok = await actionTask.ConfigureAwait(true);
                WriteEvent("INPUT-STAGE-ACTION", actionName + " result=" + ok);
                if (!ok)
                {
                    RaiseWarning("INPUT-STAGE-FAIL", actionName + " failed.");
                    showFailure = true;
                }
            }
            catch (OperationCanceledException)
            {
                WriteEvent("INPUT-STAGE-CANCEL", actionName + " canceled.");
            }
            catch (QMC.CDT320.ManualActionBlockedException ex)
            {
                // 수동 시작 거부는 장비 이상이 아니므로 알람을 올리지 않는다(알람은 전체 축 EStop 유발).
                EventLogger.Write(EventKind.Warning, "UI", "INPUT-STAGE-ACTION-BLOCKED", actionName + " blocked: " + ex.Message);
                QMC.Common.MessageDialog.Show(
                    this,
                    Lang.Format("message.manual.blocked", ex.Message),
                    Lang.T("message.title.inputStage"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-ACTION-EX", actionName + " failed: " + ex.Message);
                exceptionMessage = ex.Message;
            }
            finally
            {
                try
                {
                    if (actionScope != null)
                        actionScope.Dispose();
                }
                catch (Exception ex)
                {
                    WriteAlarm("INPUT-STAGE-MANUAL-CLEANUP", "Input Stage 수동 시컨스 정리 중 오류: " + ex.Message);
                }
                finally
                {
                    _manualSequenceRunning = false;
                    try { SetSequenceButtonsEnabled(true); } catch (Exception ex) { WriteAlarm("INPUT-STAGE-BUTTON-RESTORE", "Input Stage 버튼 복구 실패: " + ex.Message); }
                    try { RefreshFromMachine(); } catch (Exception ex) { WriteAlarm("INPUT-STAGE-REFRESH", "Input Stage 화면 갱신 실패: " + ex.Message); }
                    try { BeginRestoreSequenceButtons(); } catch (Exception ex) { WriteAlarm("INPUT-STAGE-BUTTON-RESTORE-DELAY", "Input Stage 버튼 지연 복구 실패: " + ex.Message); }
                }
            }

            if (showFailure)
            {
                string message = SequenceFailureStore.BuildManualFailureMessage(
                    actionName,
                    Lang.Format("message.manual.failed", Lang.Display(actionName)));
                QMC.Common.MessageDialog.Show(this, message, Lang.T("message.title.inputStage"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(this, exceptionMessage, Lang.T("message.title.inputStage"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static async Task WaitForCancellationAsync(CancellationToken ct)
        {
            if (!ct.CanBeCanceled)
            {
                await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
                return;
            }

            if (ct.IsCancellationRequested)
                return;

            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ct.Register(() => tcs.TrySetResult(0)))
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }

        private void ObserveManualActionTask(Task<bool> task, string actionName)
        {
            if (task == null)
                return;

            _ = ObserveManualActionTaskAsync(task, actionName);
        }

        private async Task ObserveManualActionTaskAsync(Task<bool> task, string actionName)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteEvent("INPUT-STAGE-ACTION-LATE-CANCEL", actionName + " 정지 후 취소 완료.");
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-ACTION-LATE-EX", actionName + " 정지 후 종료 처리 중 오류: " + ex.Message);
            }
            finally
            {
            }
        }

        private bool ConfirmAction(string actionName)
        {
            return QMC.Common.MessageDialog.Show(this, Lang.Format("message.manual.confirm", Lang.Display(actionName)), Lang.T("message.title.inputStage"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void SetSequenceButtonsEnabled(bool enabled)
        {
            if (actionBar == null)
                return;

            actionBar.Enabled = true;
            foreach (Control control in actionBar.Controls)
            {
                if (!ReferenceEquals(control, btnStop))
                    control.Enabled = enabled;
            }

            if (btnStop != null)
                btnStop.Enabled = true;
        }

        private void BeginRestoreSequenceButtons()
        {
            try
            {
                if (!IsHandleCreated)
                    return;

                BeginInvoke((Action)(() =>
                {
                    _manualSequenceRunning = false;
                    SetSequenceButtonsEnabled(true);
                    RefreshFromMachine();
                }));
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-BUTTON-RESTORE-EX", "Manual action button restore failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task StopManualActionAsync()
        {
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null)
                    return;

                WriteEvent("INPUT-STAGE-STOP", "Manual action stop requested.");
                await host.Controller.StopAsync();
                _manualSequenceRunning = false;
                SetSequenceButtonsEnabled(true);
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-STOP-EX", "Manual action stop failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<bool> RunPrepareLoadAsync(Form1 host)
        {
            return await RunPrepareLoadCoreAsync(host, "PREP LOAD").ConfigureAwait(true);
        }

        private async Task<bool> RunMapLoadAsync(Form1 host)
        {
            CancellationToken ct = host.Controller.ManualOperationToken;

            int feederResult = await LowerInputFeederForStageButtonAsync(
                host,
                ct,
                "WAFER BARCODE").ConfigureAwait(true);
            if (feederResult != 0)
                return false;

            int axisResult = await PrepareInputStageButtonProcessPlaneAsync(host, ct, "WAFER BARCODE").ConfigureAwait(true);
            if (axisResult != 0)
                return false;

            int readyResult = await RunReadyBeforeStagePrepareAsync(host, ct, "WAFER BARCODE").ConfigureAwait(true);
            if (readyResult != 0)
                return false;

            int stageResult = await CreateSequence(host).RunPrepareLoadAsync(ct, BuildOptions(host)).ConfigureAwait(true);
            return stageResult == 0;
        }

        private async Task<bool> RunPrepareLoadCoreAsync(Form1 host, string actionName)
        {
            CancellationToken ct = host.Controller.ManualOperationToken;

            int readyResult = await RunReadyBeforeStagePrepareAsync(host, ct, actionName).ConfigureAwait(true);
            if (readyResult != 0)
                return false;

            int feederResult = await LiftInputFeederUpForStagePrepareAsync(
                host,
                ct,
                actionName).ConfigureAwait(true);
            if (feederResult != 0)
                return false;

            int stageResult = await CreateSequence(host).RunPrepareLoadAsync(ct, BuildOptions(host)).ConfigureAwait(true);
            return stageResult == 0;
        }

        private async Task<bool> RunAlignAsync(Form1 host)
        {
            CancellationToken ct = host.Controller.ManualOperationToken;

            int feederResult = await LowerInputFeederForStageButtonAsync(
                host,
                ct,
                "ALIGN").ConfigureAwait(true);
            if (feederResult != 0)
                return false;

            int axisResult = await PrepareInputStageButtonProcessPlaneAsync(host, ct, "ALIGN").ConfigureAwait(true);
            if (axisResult != 0)
                return false;

            int readyResult = await RunReadyBeforeStagePrepareAsync(host, ct, "ALIGN").ConfigureAwait(true);
            if (readyResult != 0)
                return false;

            return await CreateSequence(host).RunAlignAsync(ct, BuildOptions(host)).ConfigureAwait(true) == 0;
        }

        private async Task<bool> RunDieMappingAsync(Form1 host)
        {
            CancellationToken ct = host.Controller.ManualOperationToken;

            int feederResult = await LowerInputFeederForStageButtonAsync(
                host,
                ct,
                "DIE MAPPING").ConfigureAwait(true);
            if (feederResult != 0)
                return false;

            int axisResult = await PrepareInputStageButtonProcessPlaneAsync(host, ct, "DIE MAPPING").ConfigureAwait(true);
            if (axisResult != 0)
                return false;

            int readyResult = await RunReadyBeforeStagePrepareAsync(host, ct, "DIE MAPPING").ConfigureAwait(true);
            if (readyResult != 0)
                return false;

            return await CreateSequence(host).RunDieMappingAsync(ct, BuildOptions(host)).ConfigureAwait(true) == 0;
        }

        private async Task<bool> RunPrepareUnloadAsync(Form1 host)
        {
            CancellationToken ct = host.Controller.ManualOperationToken;

            int readyResult = await RunReadyBeforeStagePrepareAsync(host, ct, "PREP UNLOAD").ConfigureAwait(true);
            if (readyResult != 0)
                return false;

            int feederResult = await LiftInputFeederUpForStagePrepareAsync(
                host,
                ct,
                "PREP UNLOAD").ConfigureAwait(true);
            if (feederResult != 0)
                return false;

            int stageResult = await CreateSequence(host).RunPrepareUnloadAsync(ct, BuildOptions(host)).ConfigureAwait(true);
            return stageResult == 0;
        }

        private async Task<bool> RunMoveAvoidAsync(Form1 host)
        {
            CancellationToken ct = host.Controller.ManualOperationToken;

            int feederResult = await LowerInputFeederForStageButtonAsync(
                host,
                ct,
                "AVOID").ConfigureAwait(true);
            if (feederResult != 0)
                return false;

            int axisResult = await PrepareInputStageButtonProcessPlaneAsync(host, ct, "AVOID").ConfigureAwait(true);
            if (axisResult != 0)
                return false;

            int readyResult = await RunReadyBeforeStagePrepareAsync(host, ct, "AVOID").ConfigureAwait(true);
            if (readyResult != 0)
                return false;

            return await CreateSequence(host).RunMoveAvoidAsync(ct, BuildOptions(host)).ConfigureAwait(true) == 0;
        }

        private InputStageSequence CreateSequence(Form1 host)
        {
            var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            return new InputStageSequence(ctx);
        }

        private async Task<int> RunReadyBeforeStagePrepareAsync(Form1 host, CancellationToken ct, string actionName)
        {
            try
            {
                if (host == null || host.Machine == null)
                {
                    WriteAlarm("INPUT-STAGE-PREP-READY-NO-MACHINE", actionName + " 전 Ready 시컨스를 실행할 장비 객체가 없습니다.");
                    return -1;
                }

                WriteEvent("INPUT-STAGE-PREP-READY-START", actionName + " 전 Ready 시컨스 시작.");
                var readySequence = new MachineReadySequence(host.Machine);
                int result = await readySequence.RunAsync(ct).ConfigureAwait(true);
                if (result != 0)
                {
                    string reason = string.IsNullOrWhiteSpace(readySequence.LastErrorMessage)
                        ? "result=" + result
                        : readySequence.LastErrorMessage;
                    WriteAlarm("INPUT-STAGE-PREP-READY-FAIL", actionName + " 전 Ready 시컨스 실패: " + reason);
                    return result;
                }

                WriteEvent("INPUT-STAGE-PREP-READY-OK", actionName + " 전 Ready 시컨스 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteEvent("INPUT-STAGE-PREP-READY-CANCEL", actionName + " 전 Ready 시컨스가 정지 요청으로 중단되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-PREP-READY-EX", actionName + " 전 Ready 시컨스 예외: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> LiftInputFeederUpForStagePrepareAsync(
            Form1 host,
            CancellationToken ct,
            string actionName)
        {
            try
            {
                InputFeederUnit feeder = host != null && host.Machine != null ? host.Machine.InputFeederUnit : null;
                if (feeder == null)
                {
                    WriteAlarm("INPUT-STAGE-PREP-FEEDER-MISSING", actionName + " Feeder Up 실패: InputFeeder 유닛을 찾을 수 없습니다.");
                    return -1;
                }

                if (feeder.IsWaferFeederUp())
                {
                    WriteEvent("INPUT-STAGE-PREP-FEEDER-UP-SKIP", actionName + " Feeder Up 생략: 이미 Up 상태입니다.");
                    return 0;
                }

                string reason;
                int timeoutMs = ResolveInputFeederLiftTimeoutMs(feeder);
                if (!feeder.CheckWaferFeederMoveReady(out reason))
                {
                    WriteAlarm("INPUT-STAGE-PREP-FEEDER-READY", actionName + " Feeder Up 차단: Feeder 준비 상태가 아닙니다. " + reason);
                    return -1;
                }

                WriteEvent("INPUT-STAGE-PREP-FEEDER-UP-START", actionName + " Feeder Up 시작. timeoutMs=" + timeoutMs);
                int result = await feeder.SetWaferFeederUpDownAsync(true, timeoutMs, ct).ConfigureAwait(true);
                if (result != 0 || !feeder.IsWaferFeederUp())
                {
                    WriteAlarm("INPUT-STAGE-PREP-FEEDER-UP-FAIL", actionName + " Feeder Up 실패. result=" + result + ", " + feeder.GetWaferFeederTransferState());
                    return result != 0 ? result : -1;
                }

                WriteEvent("INPUT-STAGE-PREP-FEEDER-UP-OK", actionName + " Feeder Up 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteEvent("INPUT-STAGE-PREP-FEEDER-UP-CANCEL", actionName + " Feeder Up이 정지 요청으로 중단되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-PREP-FEEDER-UP-EX", actionName + " Feeder Up 예외: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> LowerInputFeederForStageButtonAsync(
            Form1 host,
            CancellationToken ct,
            string actionName)
        {
            try
            {
                InputFeederUnit feeder = host != null && host.Machine != null ? host.Machine.InputFeederUnit : null;
                if (feeder == null)
                {
                    WriteAlarm("INPUT-STAGE-FEEDER-DOWN-MISSING", actionName + " Feeder Down 실패: InputFeeder 유닛을 찾을 수 없습니다.");
                    return -1;
                }

                // AVOID 확인을 Lift Down보다 먼저 수행한다.
                // 기존에는 내린 뒤에 AVOID를 확인해서, 피더가 InputStage 위에 있으면
                // 스테이지의 wafer를 누르고 긁은 뒤에야 알람이 났다(2026-07-27 Output 측 현장 확인, 동일 결함).
                int avoidResult = EnsureInputFeederAvoidForStageButton(feeder, actionName);
                if (avoidResult != 0)
                    return avoidResult;

                if (feeder.IsWaferFeederDown())
                {
                    WriteEvent("INPUT-STAGE-FEEDER-DOWN-SKIP", actionName + " Feeder Down 생략: 이미 Down 상태입니다.");
                    return 0;
                }

                string reason;
                int timeoutMs = ResolveInputFeederLiftTimeoutMs(feeder);
                if (!feeder.CheckWaferFeederMoveReady(out reason))
                {
                    WriteAlarm("INPUT-STAGE-FEEDER-DOWN-READY", actionName + " Feeder Down 차단: Feeder 준비 상태가 아닙니다. " + reason);
                    return -1;
                }

                WriteEvent("INPUT-STAGE-FEEDER-DOWN-START", actionName + " Feeder Down 시작. timeoutMs=" + timeoutMs);
                int result = await feeder.SetWaferFeederUpDownAsync(false, timeoutMs, ct).ConfigureAwait(true);
                if (result != 0 || !feeder.IsWaferFeederDown())
                {
                    WriteAlarm("INPUT-STAGE-FEEDER-DOWN-FAIL", actionName + " Feeder Down 실패. result=" + result + ", " + feeder.GetWaferFeederTransferState());
                    return result != 0 ? result : -1;
                }

                WriteEvent("INPUT-STAGE-FEEDER-DOWN-OK", actionName + " Feeder Down 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteEvent("INPUT-STAGE-FEEDER-DOWN-CANCEL", actionName + " Feeder Down이 정지 요청으로 중단되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-FEEDER-DOWN-EX", actionName + " Feeder Down 예외: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private int EnsureInputFeederAvoidForStageButton(InputFeederUnit feeder, string actionName)
        {
            try
            {
                if (feeder == null)
                {
                    WriteAlarm("INPUT-STAGE-FEEDER-AVOID-MISSING", actionName + " Feeder AVOID 확인 실패: InputFeeder 유닛을 찾을 수 없습니다.");
                    return -1;
                }

                if (!feeder.IsWaferFeederInAvoidPosition())
                {
                    WriteAlarm(
                        "INPUT-STAGE-FEEDER-AVOID",
                        actionName + " 시작 차단: InputFeederY가 AVOID 위치가 아닙니다. " + feeder.GetWaferFeederTransferState());
                    return -1;
                }

                WriteEvent("INPUT-STAGE-FEEDER-AVOID-OK", actionName + " Feeder AVOID 위치 확인 완료.");
                return 0;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-FEEDER-AVOID-EX", actionName + " Feeder AVOID 확인 예외: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> PrepareInputStageButtonProcessPlaneAsync(
            Form1 host,
            CancellationToken ct,
            string actionName)
        {
            try
            {
                InputStageUnit stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                if (stage == null || stage.Recipe == null)
                {
                    WriteAlarm("INPUT-STAGE-BUTTON-AXIS-MISSING", actionName + " 시작 전 InputStage 유닛 또는 레시피를 찾을 수 없습니다.");
                    return -1;
                }

                stage.Recipe.EnsurePositionObjects();

                int result = await MoveInputStageButtonAxisAsync(
                    stage,
                    ct,
                    actionName,
                    WaferStageAxis.NeedleZ,
                    stage.Recipe.NeedleZ.AvoidPosition,
                    "NeedleZ AVOID").ConfigureAwait(true);
                if (result != 0)
                    return result;

                result = await MoveInputStageButtonAxisAsync(
                    stage,
                    ct,
                    actionName,
                    WaferStageAxis.EjectPinZ,
                    stage.Recipe.EjectPinZ.AvoidPosition,
                    "EjectPinZ AVOID").ConfigureAwait(true);
                if (result != 0)
                    return result;

                result = await MoveInputStageButtonAxisAsync(
                    stage,
                    ct,
                    actionName,
                    WaferStageAxis.WaferExpandingZ,
                    stage.Recipe.WaferZ.ProcessPosition,
                    "ExpanderZ PROCESS").ConfigureAwait(true);
                if (result != 0)
                    return result;

                WriteEvent("INPUT-STAGE-BUTTON-AXIS-OK", actionName + " 시작 전 NeedleZ/EjectPinZ AVOID, ExpanderZ PROCESS 정렬 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteEvent("INPUT-STAGE-BUTTON-AXIS-CANCEL", actionName + " 시작 전 InputStage 축 정렬이 정지 요청으로 중단되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-BUTTON-AXIS-EX", actionName + " 시작 전 InputStage 축 정렬 예외: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageButtonAxisAsync(
            InputStageUnit stage,
            CancellationToken ct,
            string actionName,
            WaferStageAxis axis,
            double target,
            string label)
        {
            ct.ThrowIfCancellationRequested();

            WriteEvent(
                "INPUT-STAGE-BUTTON-AXIS-START",
                actionName + " 시작 전 " + label + " 이동 시작. target=" + target.ToString("F6"));

            int result = await stage.MoveInputStageAxis(axis, target, false).ConfigureAwait(true);
            if (result != 0)
            {
                string reason = string.IsNullOrWhiteSpace(stage.LastStageMoveFailureMessage)
                    ? "result=" + result
                    : stage.LastStageMoveFailureMessage;
                WriteAlarm("INPUT-STAGE-BUTTON-AXIS-FAIL", actionName + " 시작 전 " + label + " 이동 실패: " + reason);
                return result;
            }

            WriteEvent("INPUT-STAGE-BUTTON-AXIS-DONE", actionName + " 시작 전 " + label + " 이동 완료.");
            return 0;
        }

        private static int ResolveInputFeederLiftTimeoutMs(InputFeederUnit feeder)
        {
            try
            {
                if (feeder != null && feeder.InputFeederLift != null && feeder.InputFeederLift.Recipe != null)
                {
                    int timeoutMs = feeder.InputFeederLift.Recipe.FwdTimeoutMs;
                    if (timeoutMs > 0)
                        return timeoutMs;
                }
            }
            catch
            {
            }
            finally
            {
            }

            return 3000;
        }

        private InputStageSequenceOptions BuildOptions(Form1 host)
        {
            var options = InputStageSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = _manualSequenceStartMode;
            options.FineMove = false;
            options.RequireVisionAlign = false;
            options.RequireMapData = false;
            options.WaferId = ResolveWaferId(host);
            ApplyInputStageUnitParameters(host, options);
            return options;
        }

        private static void ApplyInputStageUnitParameters(Form1 host, InputStageSequenceOptions options)
        {
            try
            {
                var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                if (stage == null || stage.Config == null || options == null)
                    return;

                if (stage.Config.SequenceMoveTimeoutMs > 0)
                    options.MoveTimeoutMs = stage.Config.SequenceMoveTimeoutMs;
                if (stage.Config.AlignConvergenceThresholdDeg > 0.0)
                    options.AlignThetaToleranceDeg = stage.Config.AlignConvergenceThresholdDeg;
                if (stage.Config.AlignThetaCorrectionLimitDeg > 0.0)
                    options.AlignThetaCorrectionLimitDeg = stage.Config.AlignThetaCorrectionLimitDeg;
                if (stage.Config.MaxAlignIterations > 0)
                    options.AlignRetryCount = stage.Config.MaxAlignIterations;
                if (stage.Recipe != null && stage.Recipe.DieMap != null)
                {
                    if (!string.IsNullOrWhiteSpace(stage.Recipe.DieMap.VisionTargetId))
                        options.DieMapVisionTargetId = stage.Recipe.DieMap.VisionTargetId;
                    if (stage.Recipe.DieMap.VisionRetryCount > 0)
                        options.DieMapVisionRetryCount = stage.Recipe.DieMap.VisionRetryCount;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStagePage",
                    "InputStage sequence option parameter apply failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static string ResolveWaferId(Form1 host)
        {
            int slot = host != null && host.Controller != null ? host.Controller.CurrentInputSlot : -1;
            return slot >= 0 ? "INPUT-SLOT-" + (slot + 1).ToString("00") : "";
        }

        private void RefreshFromMachine()
        {
            try
            {
                var host = GetHost();
                var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                if (stage == null)
                    return;

                WaferMaterial currentWafer = stage.GetCurrentStageWaferMaterial();
                RestoreStageRuntimeFromSavedWafer(stage, currentWafer);
                bool hasWafer = stage.HasWaferOnStage();
                string resultModeReason;
                bool alignResultUsable = currentWafer == null ||
                    MaterialStateService.IsStoredInputStageResultModeUsable(
                        currentWafer,
                        false,
                        out resultModeReason);
                bool mappingResultUsable = currentWafer == null ||
                    !currentWafer.HasInputStageDieMappingResult ||
                    MaterialStateService.IsStoredInputStageResultModeUsable(
                        currentWafer,
                        true,
                        out resultModeReason);
                bool alignComplete = currentWafer != null
                    ? currentWafer.HasInputStageAlignResult && alignResultUsable
                    : (stage.PitchX != 0.0 || stage.PitchY != 0.0);
                bool dieMapComplete = currentWafer != null
                    ? currentWafer.HasInputStageDieMappingResult && mappingResultUsable
                    : stage.CurrentWaferMap != null;
                string stageFinishReason;
                bool stageFinishComplete = MaterialStateService.IsInputStageFinishComplete(out stageFinishReason);

                lblStageExistValue.Text = Lang.Display(hasWafer ? "WAFER" : "EMPTY");
                lblStageAlignValue.Text = Lang.Display(alignComplete ? "COMPLETE" : "INCOMPLETE");
                lblStageAlignOffsetValue.Text = currentWafer != null && currentWafer.HasInputStageAlignResult && alignResultUsable
                    ? FormatOffset(currentWafer.InputStageAlignOffsetX, currentWafer.InputStageAlignOffsetY)
                    : FormatOffset(stage.WaferAlignOffsetX, stage.WaferAlignOffsetY);
                lblStageBarcodeValue.Text = ResolveStageWaferId(stage, currentWafer);
                lblStageChipAlignValue.Text = Lang.Display(dieMapComplete ? "COMPLETE" : "INCOMPLETE");
                lblStageChipAlignOffsetValue.Text = currentWafer != null && currentWafer.HasInputStageDieMappingResult && mappingResultUsable
                    ? FormatOffset(currentWafer.InputStageDieMappingOffsetX, currentWafer.InputStageDieMappingOffsetY)
                    : FormatOffset(stage.DieMappingOffsetX, stage.DieMappingOffsetY);
                lblStageFinishValue.Text = Lang.Display(stageFinishComplete ? "COMPLETE" : "INCOMPLETE");

                lblVisionAxisXValue.Text = AxisUnitConverter.FormatDisplay(stage.CameraX.ActualPosition, stage.CameraX, "0.###", true);
                lblStageAxisTValue.Text = AxisUnitConverter.FormatDisplay(stage.StageT.ActualPosition, stage.StageT, "0.###", true);
                lblStageAxisYValue.Text = AxisUnitConverter.FormatDisplay(stage.StageY.ActualPosition, stage.StageY, "0.###", true);
                lblStageAxisZValue.Text = AxisUnitConverter.FormatDisplay(stage.ExpanderZ.ActualPosition, stage.ExpanderZ, "0.###", true);
                label2.Text = AxisUnitConverter.FormatDisplay(stage.NeedleBlockX.ActualPosition, stage.NeedleBlockX, "0.###", true);
                lblNeedleAxisZValue.Text = AxisUnitConverter.FormatDisplay(stage.NeedleZ.ActualPosition, stage.NeedleZ, "0.###", true);
                label4.Text = AxisUnitConverter.FormatDisplay(stage.EjectPinZ.ActualPosition, stage.EjectPinZ, "0.###", true);
                lblExpendingValue.Text = AxisUnitConverter.FormatDisplay(stage.ExpanderZ.ActualPosition, stage.ExpanderZ, "0.###", true);
                lblNeedleUpDownValue.Text = Lang.Display(stage.NeedleZ.IsMoving ? "MOVING" : "STOP");
                dotNeedleVacuum.IsOn = stage.IsInputStageSimulationOrDryRun() ? hasWafer : stage.NeedleVacuum.IsOn;
                RefreshMaterialDetail(false);
            }
            catch (Exception ex)
            {
                WriteWarning("INPUT-STAGE-REFRESH", "Refresh failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static void RestoreStageRuntimeFromSavedWafer(QMC.CDT320.InputStageUnit stage, WaferMaterial wafer)
        {
            try
            {
                if (stage == null || wafer == null)
                    return;

                string resultModeReason;
                if (!MaterialStateService.IsStoredInputStageResultModeUsable(
                        wafer,
                        wafer.HasInputStageDieMappingResult,
                        out resultModeReason))
                {
                    return;
                }

                stage.SetCurrentWaferMaterial(wafer);
                if (wafer.HasInputStageAlignResult)
                {
                    stage.ApplyWaferAlignResult(
                        wafer.InputStageAlignOriginX,
                        wafer.InputStageAlignOriginY,
                        wafer.InputStageAlignPitchX,
                        wafer.InputStageAlignPitchY,
                        wafer.InputStageAlignOffsetX,
                        wafer.InputStageAlignOffsetY);
                }

                if (wafer.HasInputStageDieMappingResult)
                {
                    var waferMap = MaterialStateService.BuildWaferMapDataFromWafer(wafer);
                    var dieMap = MaterialStateService.BuildDieMapFromWafer(wafer);
                    if (waferMap != null && dieMap != null)
                        stage.ApplyDieMappingResult(
                            waferMap,
                            dieMap.OriginX,
                            dieMap.OriginY,
                            dieMap.PitchX,
                            dieMap.PitchY,
                            wafer.InputStageDieMappingOffsetX,
                            wafer.InputStageDieMappingOffsetY);
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static string FormatOffset(double x, double y)
        {
            return "X" + x.ToString("0.###") + "/Y" + y.ToString("0.###");
        }

        private void RefreshMaterialDetail(bool force)
        {
            if (materialDetailView == null)
                return;

            WaferMaterial wafer = ResolveStageWaferMaterial();
            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId) ||
                WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty)
            {
                _lastMaterialDisplayKey = "";
                materialDetailView.Clear();
                materialDetailView.Visible = true;
                return;
            }

            string displayKey = BuildMaterialDisplayKey(wafer);
            if (!force && materialDetailView.Visible && string.Equals(displayKey, _lastMaterialDisplayKey, StringComparison.Ordinal))
                return;

            _lastMaterialDisplayKey = displayKey;
            materialDetailView.Visible = true;
            materialDetailView.SetRows("WAFER MATERIAL", BuildStageMaterialRows(wafer));
        }

        private WaferMaterial ResolveStageWaferMaterial()
        {
            var host = GetHost();
            var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
            if (stage != null)
                return stage.GetCurrentStageWaferMaterial();

            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
        }

        private void ConfigureInfoLayoutForReadableText()
        {
            try
            {
                ConfigureAxisPanel(stageAxisYPanel, lblStageAxisYTitle, lblStageAxisYValue);
                ConfigureAxisPanel(stageAxisTPanel, lblStageAxisTTitle, lblStageAxisTValue);
                ConfigureAxisPanel(tableLayoutPanel1, lblStageAxisZTitle, lblStageAxisZValue);
                ConfigureAxisPanel(stageAxisXPanel, lblStageAxisXTitle, lblVisionAxisXValue);
                ConfigureAxisPanel(tableLayoutPanel2, label1, label2);
                ConfigureAxisPanel(needleAxisZPanel, lblNeedleAxisZTitle, lblNeedleAxisZValue);
                ConfigureAxisPanel(tableLayoutPanel3, label3, label4);
                ConfigureStatusValueLabels();

                if (infoLayout != null)
                {
                    infoLayout.SuspendLayout();
                    infoLayout.ColumnStyles.Clear();
                    infoLayout.ColumnCount = 2;
                    infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    infoLayout.RowStyles.Clear();
                    infoLayout.RowCount = 4;
                    for (int i = 0; i < 4; i++)
                        infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
                    infoLayout.ResumeLayout();
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void ConfigureAxisPanel(TableLayoutPanel panel, Label title, Label value)
        {
            if (panel != null)
            {
                panel.RowStyles.Clear();
                panel.RowCount = 2;
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
                panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                panel.MinimumSize = new System.Drawing.Size(0, 56);
                panel.Margin = new Padding(4, 4, 4, 4);
            }

            if (title != null)
            {
                title.AutoSize = false;
                title.AutoEllipsis = true;
                title.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
                title.Padding = new Padding(6, 0, 0, 0);
                title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            }

            if (value != null)
            {
                value.AutoSize = false;
                value.AutoEllipsis = true;
                value.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
                value.Padding = new Padding(0, 0, 6, 0);
                value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            }
        }

        private void ConfigureStatusValueLabels()
        {
            ConfigureCompactValueLabel(lblStageExistValue);
            ConfigureCompactValueLabel(lblStageAlignValue);
            ConfigureCompactValueLabel(lblStageAlignOffsetValue);
            ConfigureCompactValueLabel(lblStageBarcodeValue);
            ConfigureCompactValueLabel(lblStageChipAlignValue);
            ConfigureCompactValueLabel(lblStageChipAlignOffsetValue);
            ConfigureCompactValueLabel(lblStageFinishValue);
            ConfigureCompactValueLabel(lblExpendingValue);
            ConfigureCompactValueLabel(lblNeedleUpDownValue);
        }

        private static void ConfigureCompactValueLabel(Label label)
        {
            if (label == null)
                return;

            label.AutoSize = false;
            label.AutoEllipsis = true;
            label.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        }

        private static string ResolveStageWaferId(QMC.CDT320.InputStageUnit stage, WaferMaterial currentWafer)
        {
            if (stage != null && stage.CurrentWaferMap != null && !string.IsNullOrWhiteSpace(stage.CurrentWaferMap.WaferId))
                return stage.CurrentWaferMap.WaferId;
            if (currentWafer != null && !string.IsNullOrWhiteSpace(currentWafer.WaferId))
                return currentWafer.WaferId;
            return "INCOMPLETE";
        }

        private void MaterialDetailView_CreateDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmMaterialDataAction(Lang.T("message.inputStage.createConfirm")))
                    return;

                var wafer = MaterialStateService.CreateWaferAtLocation(
                    MaterialLocationKind.InputStage,
                    "INPUT-STAGE-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                    WaferMaterialState.Working);

                var host = GetHost();
                var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                if (stage != null)
                    stage.SetCurrentWaferMaterial(wafer);

                WriteEvent("INPUT-STAGE-DATA-CREATE", "wafer=" + (wafer != null ? wafer.WaferId : ""));
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-DATA-CREATE-EX", "Material data create failed: " + ex.Message);
            }
        }

        private void MaterialDetailView_ClearDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmMaterialDataAction(Lang.T("message.inputStage.clearConfirm")))
                    return;

                bool cleared = MaterialStateService.ClearWaferAtLocation(MaterialLocationKind.InputStage);
                var host = GetHost();
                var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                if (stage != null)
                {
                    // Stage DATA CLEAR는 Material 포인터뿐 아니라 런타임 Align/Die Map도 비운다.
                    // 이 Map을 남기면 다음 Wafer가 이전 Mapping 화면/좌표를 재사용할 수 있다.
                    stage.ClearCurrentWaferMap();
                    stage.ClearCurrentWaferMaterial();
                }

                if (cleared && !MaterialStateService.TryFlushPendingSave("InputStageDataClear"))
                {
                    QMC.Common.MessageDialog.Show(
                        this,
                        Lang.T("message.inputStage.clearSaveFailed"),
                        Lang.T("message.title.materialData"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

                WriteEvent("INPUT-STAGE-DATA-CLEAR", "Input stage material data cleared. changed=" + cleared);
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-DATA-CLEAR-EX", "Material data clear failed: " + ex.Message);
            }
        }

        private bool ConfirmMaterialDataAction(string message)
        {
            return QMC.Common.MessageDialog.Show(this, message, Lang.T("message.title.materialData"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private static IEnumerable<MaterialDetailRow> BuildStageMaterialRows(WaferMaterial wafer)
        {
            string specName = wafer != null ? wafer.TapeFrameSpecName : "";
            var spec = !string.IsNullOrEmpty(specName) ? MaterialSpecs.FindFrame(specName) : null;
            var location = wafer != null && wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";

            return new[]
            {
                Row("Unit", "Input Stage", "", false),
                Row("Wafer ID", wafer != null ? wafer.WaferId : "", "", false),
                Row("State", wafer != null ? WaferMaterialStateText.ToDisplayName(wafer.State) : "", "", false),
                Row("TapeFrame Spec", specName, "", false),
                Row("Frame Size", spec != null ? spec.OuterDiameterMm.ToString("0.###") + " mm" : "", "", false),
                Row("Grid", spec != null ? spec.DieMapX + " x " + spec.DieMapY : "", "", false),
                Row("Die Spec", spec != null ? spec.DieSpecName : "", "", false),
                Row("Lot ID", wafer != null ? wafer.CassetteLotId : "", "", false),
                Row("Source Cassette", wafer != null ? wafer.SourceCassetteRole.ToString() : "", "", false),
                Row("Source Slot", wafer != null && wafer.SourceSlotNumber >= 0 ? (wafer.SourceSlotNumber + 1).ToString("00") : "", "", false),
                Row("Source Pos", wafer != null && !double.IsNaN(wafer.SourceCassetteSlotPosition) ? wafer.SourceCassetteSlotPosition.ToString("0.###") : "", "", false),
                Row("Current Loc", location, "", false),
                Row("DieMap ObjId", wafer != null ? wafer.DieMapFrameObjId : "", "", false),
                Row("Align Offset", wafer != null ? FormatOffset(wafer.InputStageAlignOffsetX, wafer.InputStageAlignOffsetY) : "", "", false),
                Row("DieMap Offset", wafer != null ? FormatOffset(wafer.InputStageDieMappingOffsetX, wafer.InputStageDieMappingOffsetY) : "", "", false),
                Row("Align Complete", wafer != null && wafer.HasInputStageAlignResult ? "Y" : "N", "", false),
                Row("DieMap Complete", wafer != null && wafer.HasInputStageDieMappingResult ? "Y" : "N", "", false),
                Row("Updated", wafer != null ? wafer.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") : "", "", false)
            };
        }

        private static string BuildMaterialDisplayKey(WaferMaterial wafer)
        {
            if (wafer == null)
                return "";

            string location = wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";
            return string.Join("|",
                wafer.WaferId ?? "",
                WaferMaterialStateText.ToDisplayName(wafer.State),
                wafer.TapeFrameSpecName ?? "",
                wafer.CassetteLotId ?? "",
                wafer.SourceCassetteRole.ToString(),
                wafer.SourceSlotNumber.ToString(),
                location,
                wafer.DieMapFrameObjId ?? "",
                wafer.HasInputStageAlignResult.ToString(),
                wafer.InputStageAlignOffsetX.ToString("0.######"),
                wafer.InputStageAlignOffsetY.ToString("0.######"),
                wafer.HasInputStageDieMappingResult.ToString(),
                wafer.InputStageDieMappingOffsetX.ToString("0.######"),
                wafer.InputStageDieMappingOffsetY.ToString("0.######"),
                wafer.UpdatedAt.Ticks.ToString());
        }

        private static MaterialDetailRow Row(string name, string value, string key, bool editable)
        {
            return new MaterialDetailRow
            {
                Name = name,
                Value = value,
                Key = key ?? "",
                Editable = editable
            };
        }

        private static void WriteEvent(string code, string message)
        {
            try { EventLogger.Write(EventKind.Event, "UI", code, message); } catch { }
        }

        private static void WriteWarning(string code, string message)
        {
            try { EventLogger.Write(EventKind.Warning, "UI", code, message); } catch { }
        }

        private static void WriteAlarm(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Alarm, "UI", code, message);
                AlarmManager.Raise(AlarmSeverity.Error, code, LogSource, message);
            }
            catch { }
        }

        private static void RaiseWarning(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Warning, "UI", code, message);
                AlarmManager.Raise(AlarmSeverity.Error, code, LogSource, message);
            }
            catch { }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
