using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.CDT320;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Dialogs;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class InputCassettePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private System.Windows.Forms.Timer _refreshTimer;
        private bool _manualSequenceRunning;
        private SequenceStartMode _manualSequenceStartMode = SequenceStartMode.Resume;
        private CassetteSlotView _cassetteSlotView;
        private CassetteSlotView _cassetteSlotViewLevel2;
        private CassetteMaterialRole _selectedCassetteRole = CassetteMaterialRole.Input1;
        private int _selectedMaterialSlot = -1;
        private const string LogSource = "INPUT-CASSETTE-PAGE";

        public InputCassettePage()
        {
            try
            {
                InitializeComponent();
                BindLocalizedCaptions();
                BindDesignerControls();
                WireEvents();

                _refreshTimer = new System.Windows.Forms.Timer { Interval = 200 };
                _refreshTimer.Tick += (s, e) =>
                {
                    if (!ShouldRefreshVisible(this))
                        return;

                    RefreshFromMachine();
                };
                VisibleChanged += (s, e) => { if (Visible) _refreshTimer.Start(); else _refreshTimer.Stop(); };
                HandleDestroyed += (s, e) => _refreshTimer.Stop();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-INIT", "InputCassettePage initialize failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("message.title.inputCassette"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindLocalizedCaptions()
        {
            Lang.Bind(lblHeader, "INPUT CASSETTE");
            Lang.Bind(grpSlotState, "SLOT STATE");
            Lang.Bind(lblSlotNoTitle, "Slot No");
            Lang.Bind(btnPrev, "PREV");
            Lang.Bind(btnNext, "NEXT");
            Lang.Bind(lblLifterAxisTitle, "LIFTER AXIS Z");
            Lang.Bind(lblSlotStateTitle, "State");
            Lang.Bind(btnReady, "LIFTER READY");
            Lang.Bind(grpLifter, "LIFTER");
            Lang.Bind(lblLegendReadyText, "READY");
            Lang.Bind(lblLegendEmptyText, "EMPTY");
            Lang.Bind(lblLegendWorkingText, "WORKING");
            Lang.Bind(lblLegendFinishText, "FINISH");
            Lang.Bind(lblLegendWorkReadyText, "WORK READY");
            Lang.Bind(grpAction, "ACTION");
            Lang.Bind(btnMap, "LIFT WAFER MAPPING");
            Lang.Bind(btnLoad, "LIFT WAFER LOADING");
            Lang.Bind(btnUnload, "LIFT WAFER UNLOADING");
            Lang.Bind(btnStop, "STOP");
            Lang.Bind(btnCstExchange, "CST EXCHANGE");
            Lang.Bind(btnCstClear, "CST CLEAR");
            Lang.Bind(grpDataOnly, "MATERIAL DATA ONLY");
            Lang.Bind(lblDataOnlyWarning, "DATA ONLY / NO MOTION — 실물 위치를 확인한 후 Material 데이터만 이동/삭제하십시오. 장비는 움직이지 않습니다.");
            Lang.Bind(lblDataOnlySourceTitle, "SOURCE");
            Lang.Bind(lblDataOnlyDestTitle, "DEST");
            Lang.Bind(lblDataOnlyMaterialTitle, "MATERIAL");
            Lang.Bind(btnDataOnlyMove, "MOVE DATA");
            Lang.Bind(btnDataOnlyDelete, "DELETE DATA");
        }

        private void BindDesignerControls()
        {
            try
            {
                _cassetteSlotView = cassetteSlotView;
                _cassetteSlotViewLevel2 = cassetteSlotViewLevel2;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-BIND", "Designer control bind failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void WireEvents()
        {
            try
            {
                btnPrev.Click += async (s, e) => await RunMotionAction("LIFTER PREV", host => MoveSlotAsync(host, -1));
                btnNext.Click += async (s, e) => await RunMotionAction("LIFTER NEXT", host => MoveSlotAsync(host, +1));
                btnReady.Click += async (s, e) => await RunMotionAction("LIFTER READY", LifterReadyAsync);
                btnMap.Click += async (s, e) => await RunSequenceAction("LIFT WAFER MAPPING", MapAsync);
                btnLoad.Click += async (s, e) => await RunSequenceAction("LIFT WAFER LOADING", LoadAsync);
                btnUnload.Click += async (s, e) => await RunSequenceAction("LIFT WAFER UNLOADING", UnloadAsync);
                btnStop.Click += async (s, e) => await StopManualActionAsync();
                // [P1 2026-08-21] 카세트 교체: 준비(리프터 로딩 위치 이동) -> (작업자 물리 교체)
                // -> CST CLEAR(데이터 초기화) -> 문 닫고 START 시 매핑부터 재진행(Output 페이지 모델 미러).
                btnCstExchange.Click += async (s, e) => await RunCstExchangeActionAsync();
                btnCstClear.Click += (s, e) => CompleteInputCassetteExchange();

                if (cassetteSlotView != null)
                    cassetteSlotView.SlotSelected += (s, e) => SelectMaterialSlot(CassetteMaterialRole.Input1, e.SlotIndex);
                if (cassetteSlotViewLevel2 != null)
                    cassetteSlotViewLevel2.SlotSelected += (s, e) => SelectMaterialSlot(CassetteMaterialRole.Input2, e.SlotIndex);
                if (cassetteSlotView != null)
                    cassetteSlotView.SlotMoveRequested += async (s, e) => await MoveSlotFromContextMenuAsync(CassetteMaterialRole.Input1, e.SlotIndex);
                if (cassetteSlotViewLevel2 != null)
                    cassetteSlotViewLevel2.SlotMoveRequested += async (s, e) => await MoveSlotFromContextMenuAsync(CassetteMaterialRole.Input2, e.SlotIndex);
                if (cassetteSlotView != null)
                    cassetteSlotView.SlotDoubleClicked += async (s, e) => await MoveSlotFromDoubleClickAsync(CassetteMaterialRole.Input1, e.SlotIndex);
                if (cassetteSlotViewLevel2 != null)
                    cassetteSlotViewLevel2.SlotDoubleClicked += async (s, e) => await MoveSlotFromDoubleClickAsync(CassetteMaterialRole.Input2, e.SlotIndex);
                if (cmbDataOnlySource != null)
                {
                    cmbDataOnlySource.DropDown += (s, e) => RebuildDataOnlySourceItems();
                    cmbDataOnlySource.SelectedIndexChanged += (s, e) => OnDataOnlySourceChanged();
                }
                if (cmbDataOnlyDest != null)
                    cmbDataOnlyDest.DropDown += (s, e) => RebuildDataOnlyDestItems(false);
                if (btnDataOnlyMove != null)
                    btnDataOnlyMove.Click += (s, e) => ExecuteDataOnlyMove();
                if (btnDataOnlyDelete != null)
                    btnDataOnlyDelete.Click += (s, e) => ExecuteDataOnlyDelete();
                if (materialDetailView != null)
                {
                    materialDetailView.ShowProcessTestDataButton = true;
                    materialDetailView.EditRequested += MaterialDetailView_EditRequested;
                    materialDetailView.CreateProcessTestDataRequested += MaterialDetailView_CreateProcessTestDataRequested;
                    materialDetailView.CreateDataRequested += MaterialDetailView_CreateDataRequested;
                    materialDetailView.ClearDataRequested += MaterialDetailView_ClearDataRequested;
                    materialDetailView.ClearAllDataRequested += MaterialDetailView_ClearAllDataRequested;
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-EVENT", "Event bind failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("message.title.inputCassette"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private Form1 GetHost() => FindForm() as Form1;

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
                SetActionButtonsEnabled(false);
                actionScope = host.Controller.BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "InputCassettePage:" + actionName);
                SequenceFailureStore.Clear();
                WriteEvent("INPUT-CST-ACTION", actionName + " start");
                bool ok = await action(host);
                WriteEvent("INPUT-CST-ACTION", actionName + " result=" + ok);
                if (!ok)
                {
                    RaiseWarning("INPUT-CST-CONDITION", actionName + " condition failed.");
                    showFailure = true;
                }
            }
            catch (OperationCanceledException)
            {
                WriteEvent("INPUT-CST-CANCEL", actionName + " canceled.");
            }
            catch (QMC.CDT320.ManualActionBlockedException ex)
            {
                // 수동 시작 거부는 장비 이상이 아니므로 알람을 올리지 않는다(알람은 전체 축 EStop 유발).
                EventLogger.Write(EventKind.Warning, "UI", "INPUT-CST-ACTION-BLOCKED", actionName + " blocked: " + ex.Message);
                QMC.Common.MessageDialog.Show(
                    this,
                    Lang.Format("message.manual.blocked", ex.Message),
                    Lang.T("message.title.inputCassette"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-ACTION-EX", actionName + " failed: " + ex.Message);
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
                    WriteAlarm("INPUT-CST-MANUAL-CLEANUP", "Input Cassette 수동 시컨스 정리 중 오류: " + ex.Message);
                }
                finally
                {
                    _manualSequenceRunning = false;
                    try { SetActionButtonsEnabled(true); } catch (Exception ex) { WriteAlarm("INPUT-CST-BUTTON-RESTORE", "Input Cassette 버튼 복구 실패: " + ex.Message); }
                    try { RefreshFromMachine(); } catch (Exception ex) { WriteAlarm("INPUT-CST-REFRESH", "Input Cassette 화면 갱신 실패: " + ex.Message); }
                }
            }

            if (showFailure)
            {
                QMC.Common.MessageDialog.Show(
                    this,
                    SequenceFailureStore.BuildManualFailureMessage(actionName, Lang.T("message.inputCassette.conditionsFailed")),
                    Lang.T("message.title.inputCassette"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(this, Lang.Format("message.inputCassette.lotPortError", exceptionMessage), Lang.T("message.title.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // confirm=false: 더블클릭 슬롯 이동처럼 전용 확인창을 이미 거친 경우 공통 확인창을 중복 표시하지 않는다.
        private async Task RunMotionAction(string actionName, Func<Form1, Task<int>> action, bool confirm = true)
        {
            IDisposable actionScope = null;
            bool showFailure = false;
            string failureMessage = null;
            string exceptionMessage = null;
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null || action == null)
                    return;
                if (_manualSequenceRunning)
                    return;
                if (confirm && !ConfirmAction(actionName))
                    return;

                _manualSequenceRunning = true;
                SetActionButtonsEnabled(false);
                actionScope = host.Controller.BeginManualActionScope(ManualMotionScopeKind.SpeedOnly, "InputCassettePageMotion:" + actionName);
                SequenceFailureStore.Clear();
                WriteEvent("INPUT-CST-MOTION", actionName + " start");
                int result = await action(host);
                WriteEvent("INPUT-CST-MOTION", actionName + " result=" + result);
                if (result != 0)
                {
                    RaiseWarning("INPUT-CST-MOTION-FAIL", actionName + " result=" + result);
                    failureMessage = SequenceFailureStore.BuildManualFailureMessage(actionName, Lang.Format("message.manual.failedResult", Lang.Display(actionName), result));
                    showFailure = true;
                }
            }
            catch (OperationCanceledException)
            {
                WriteEvent("INPUT-CST-MOTION-CANCEL", actionName + " canceled.");
            }
            catch (QMC.CDT320.ManualActionBlockedException ex)
            {
                // 수동 시작 거부는 장비 이상이 아니므로 알람을 올리지 않는다(알람은 전체 축 EStop 유발).
                EventLogger.Write(EventKind.Warning, "UI", "INPUT-CST-MOTION-BLOCKED", actionName + " blocked: " + ex.Message);
                QMC.Common.MessageDialog.Show(
                    this,
                    Lang.Format("message.manual.blocked", ex.Message),
                    Lang.T("message.title.inputCassette"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-MOTION-EX", actionName + " failed: " + ex.Message);
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
                    WriteAlarm("INPUT-CST-MOTION-CLEANUP", "Input Cassette 모션 정리 중 오류: " + ex.Message);
                }
                finally
                {
                    _manualSequenceRunning = false;
                    try { SetActionButtonsEnabled(true); } catch (Exception ex) { WriteAlarm("INPUT-CST-MOTION-BUTTON-RESTORE", "Input Cassette 모션 버튼 복구 실패: " + ex.Message); }
                    try { RefreshFromMachine(); } catch (Exception ex) { WriteAlarm("INPUT-CST-MOTION-REFRESH", "Input Cassette 모션 화면 갱신 실패: " + ex.Message); }
                }
            }

            if (showFailure)
                QMC.Common.MessageDialog.Show(this, failureMessage, Lang.T("message.title.inputCassette"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(this, exceptionMessage, Lang.Display(actionName), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool ConfirmAction(string actionName)
        {
            try
            {
                DialogResult result = QMC.Common.MessageDialog.Show(this, Lang.Format("message.manual.confirm", Lang.Display(actionName)), Lang.T("message.title.inputCassette"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return result == DialogResult.Yes;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-CONFIRM", "Confirm failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            try
            {
                btnPrev.Enabled = enabled;
                btnNext.Enabled = enabled;
                btnReady.Enabled = enabled;
                btnMap.Enabled = enabled;
                btnLoad.Enabled = enabled;
                btnUnload.Enabled = enabled;
                if (btnCstExchange != null)
                    btnCstExchange.Enabled = enabled;
                if (btnCstClear != null)
                    btnCstClear.Enabled = enabled;
                if (btnStop != null)
                    btnStop.Enabled = true;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-BUTTON", "Button enable failed: " + ex.Message);
            }
            finally
            {
            }
        }

        // 작업자가 State를 바꾸는 동안 Auto/Manual/초기화 시퀀스가 같은 자재를 갱신하면
        // 중앙 Material State와 Unit 슬롯 projection이 서로 다른 상태로 남을 수 있으므로 완전 정지 상태에서만 허용한다.
        private bool CanChangeInputCassetteMaterialState(Form1 host)
        {
            if (host == null || host.Controller == null)
            {
                QMC.Common.MessageDialog.Show(this,
                    Lang.T("message.inputCassette.stateControllerMissing"),
                    Lang.T("message.title.waferState"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            MachineController controller = host.Controller;
            EquipmentStatus status = controller.Status;
            bool blocked = _manualSequenceRunning ||
                           controller.IsManualBusy ||
                           controller.IsSequenceRunning ||
                           controller.IsReadySequenceRunning ||
                           status == EquipmentStatus.AutoRunning ||
                           status == EquipmentStatus.ManualRunning ||
                           status == EquipmentStatus.Initializing;
            if (!blocked)
                return true;

            QMC.Common.MessageDialog.Show(this,
                Lang.Format("message.inputCassette.stateBusy", status, controller.IsSequenceRunning, controller.IsManualBusy, controller.IsReadySequenceRunning),
                Lang.T("message.title.waferState"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private async Task StopManualActionAsync()
        {
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null)
                    return;

                WriteEvent("INPUT-CST-STOP", "Manual action stop requested.");
                await host.Controller.StopAsync();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-STOP-EX", "Manual action stop failed: " + ex.Message);
            }
            finally
            {
            }
        }

        // 기존 조건: READY는 서보 ON만 수행(이동 없음), 홈서치는 별도 INIT 버튼.
        // 현재 기준: INIT 버튼은 제거하고 READY가 리프터 Z를 티칭된 Ready(Avoid) 위치로 실제 이동시킨다.
        //           이동은 기존 유닛 경로(MoveToWaferCassetteAvoidPosition)를 사용해 MotionGuard/돌출 감시를 그대로 거친다.
        private async Task<int> LifterReadyAsync(Form1 host)
        {
            try
            {
                var cassette = host != null && host.Machine != null ? host.Machine.InputCassetteUnit : null;
                var feeder = host != null && host.Machine != null ? host.Machine.InputFeederUnit : null;
                if (cassette == null || feeder == null)
                    return -1;

                cassette.InputLifterZ.ServoOn();
                feeder.FeederY.ServoOn();
                // 서보 ON 상태 반영 전에 이동 명령을 내리면 "Servo is OFF"로 즉시 실패하므로 반영을 기다린다.
                if (!await WaitAxisServoOnAsync(cassette.InputLifterZ, 2000))
                {
                    RecordLifterReadyFailure(cassette.InputLifterZ, "InputLifterZ 서보 ON이 확인되지 않았습니다.");
                    return -2;
                }

                int result = await cassette.MoveToWaferCassetteAvoidPosition();
                if (result != 0)
                    RecordLifterReadyFailure(cassette.InputLifterZ, "Ready(Avoid) 위치 이동이 실패했습니다. result=" + result);
                return result;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-READY-MOVE", "Lifter ready failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        // READY 실패 사유를 로그/알람과 실패 팝업(SequenceFailureStore)에 구체적으로 남긴다.
        private void RecordLifterReadyFailure(QMC.Common.Motion.BaseAxis axis, string detail)
        {
            try
            {
                string message = "LIFTER READY 실패. " + detail +
                    (axis != null
                        ? " (servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                          ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                          ", pos=" + axis.ActualPosition.ToString("0.###") +
                          (!string.IsNullOrWhiteSpace(axis.LastMotionFailureMessage) ? ", last=" + axis.LastMotionFailureMessage : "") + ")"
                        : "");

                RaiseWarning("INPUT-CST-READY-FAIL", message);
                SequenceFailureStore.Record(
                    "InputCassettePage.Manual",
                    "LifterReady",
                    "LifterReadyAsync",
                    "INPUT-CST-READY-FAIL",
                    LogSource,
                    message);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-READY-LOG", "Lifter ready failure logging failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static async Task<bool> WaitAxisServoOnAsync(QMC.Common.Motion.BaseAxis axis, int timeoutMs)
        {
            if (axis == null)
                return false;

            DateTime start = DateTime.UtcNow;
            while (!axis.IsServoOn)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds >= timeoutMs)
                    return false;
                await Task.Delay(50);
            }

            return true;
        }

        private async Task<bool> MapAsync(Form1 host)
        {
            try
            {
                if (!ValidateInputCassetteManualCondition(host, true))
                    return false;

                var sequence = CreateInputCassetteSequence(host);
                return await sequence.RunMappingAsync(host.Controller.ManualOperationToken, BuildCassetteOptions(host, _manualSequenceStartMode)) == 0;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-MAP", "Mapping failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private async Task<bool> LoadAsync(Form1 host)
        {
            try
            {
                if (!ValidateInputCassetteManualCondition(host, false))
                    return false;

                var sequence = CreateInputCassetteSequence(host);
                return await sequence.RunLoadingAsync(host.Controller.ManualOperationToken, BuildCassetteOptions(host, _manualSequenceStartMode)) == 0;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-LOAD", "Loading failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private async Task<bool> UnloadAsync(Form1 host)
        {
            try
            {
                if (!ValidateInputCassetteManualCondition(host, false))
                    return false;

                var sequence = CreateInputCassetteSequence(host);
                return await sequence.RunUnloadingAsync(host.Controller.ManualOperationToken, BuildCassetteOptions(host, _manualSequenceStartMode)) == 0;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-UNLOAD", "Unloading failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        // [P1 2026-08-21] INPUT CST EXCHANGE 전용 래퍼.
        // RunSequenceAction을 쓰지 않는 이유: 그 경로는 결과 false에 RaiseWarning(AlarmManager.Raise)을
        // 걸어 사용자 거부(자동운전 중/자재 있음/취소)까지 알람(전축 정지)으로 승격시킨다.
        // 교체 준비의 거부는 안내로 끝나야 하므로 알람 없는 전용 흐름으로 감싼다.
        // 시작 모드도 묻지 않는다 — 교체 준비는 항상 Restart 고정.
        private async Task RunCstExchangeActionAsync()
        {
            const string actionName = "INPUT CST EXCHANGE";
            IDisposable actionScope = null;
            string failureMessage = null;
            string exceptionMessage = null;
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null)
                    return;
                if (_manualSequenceRunning)
                    return;
                if (!CanChangeInputCassetteData(host, actionName))
                    return;
                if (!CanPrepareInputCassetteExchange(Lang.T("message.inputCassette.exchangeCannotPrepare")))
                    return;
                if (QMC.Common.MessageDialog.Show(this,
                    Lang.T("message.inputCassette.exchangeConfirm"),
                    Lang.T("message.title.cassetteExchange"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                _manualSequenceRunning = true;
                SetActionButtonsEnabled(false);
                actionScope = host.Controller.BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "InputCassettePage:" + actionName);
                SequenceFailureStore.Clear();
                WriteEvent("INPUT-CST-EXCHANGE", actionName + " start");
                bool ok = await PrepareInputCassetteExchangeCoreAsync(host);
                WriteEvent("INPUT-CST-EXCHANGE", actionName + " result=" + ok);
                if (!ok)
                {
                    failureMessage = SequenceFailureStore.BuildManualFailureMessage(
                        actionName,
                        Lang.Format("message.manual.failed", Lang.Display(actionName)));
                }
            }
            catch (OperationCanceledException)
            {
                WriteEvent("INPUT-CST-EXCHANGE", actionName + " canceled.");
            }
            catch (QMC.CDT320.ManualActionBlockedException ex)
            {
                // 수동 시작 거부는 장비 이상이 아니므로 알람을 올리지 않는다(알람은 전체 축 EStop 유발).
                EventLogger.Write(EventKind.Warning, "UI", "INPUT-CST-EXCHANGE-BLOCKED", actionName + " blocked: " + ex.Message);
                QMC.Common.MessageDialog.Show(
                    this,
                    Lang.Format("message.manual.blocked", ex.Message),
                    Lang.T("message.title.cassetteExchange"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-EXCHANGE-EX", actionName + " failed: " + ex.Message);
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
                    WriteAlarm("INPUT-CST-EXCHANGE-CLEANUP", "Input Cassette 교체 준비 정리 중 오류: " + ex.Message);
                }
                finally
                {
                    _manualSequenceRunning = false;
                    try { SetActionButtonsEnabled(true); } catch (Exception ex) { WriteAlarm("INPUT-CST-BUTTON-RESTORE", "Input Cassette 버튼 복구 실패: " + ex.Message); }
                    try { RefreshFromMachine(); } catch (Exception ex) { WriteAlarm("INPUT-CST-REFRESH", "Input Cassette 화면 갱신 실패: " + ex.Message); }
                }
            }

            if (!string.IsNullOrWhiteSpace(failureMessage))
                QMC.Common.MessageDialog.Show(this, failureMessage, Lang.T("message.title.cassetteExchange"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(this, exceptionMessage, Lang.Display(actionName), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // 카세트 교체 준비 본체: 서보 ON 보장(LIFTER READY 패턴) -> 수동 조건 확인(기존 LOADING과 동일)
        // -> 로딩 시퀀스(피더 위치 확인 -> 리프터 로딩 위치 이동, 교체 높이는 별도 티칭 없이
        // 레시피 로딩 포지션 재사용 — Output 2026-07-26 확정과 동일 정책).
        private async Task<bool> PrepareInputCassetteExchangeCoreAsync(Form1 host)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.InputCassetteUnit : null;
            var feeder = host != null && host.Machine != null ? host.Machine.InputFeederUnit : null;
            if (cassette == null || feeder == null)
            {
                SequenceFailureStore.Record(
                    "InputCassettePage.Manual",
                    "CstExchange",
                    "PrepareInputCassetteExchangeCoreAsync",
                    "INPUT-CST-EXCHANGE-UNIT",
                    LogSource,
                    "InputCassette/InputFeeder 유닛을 찾을 수 없습니다.");
                return false;
            }

            QMC.Common.Log.Write("Main", "SYSTEM", "InputCassetteExchange",
                "INPUT 카세트 교체 준비를 시작합니다. - Start");

            cassette.InputLifterZ.ServoOn();
            feeder.FeederY.ServoOn();
            if (!await WaitAxisServoOnAsync(cassette.InputLifterZ, 2000))
            {
                RecordLifterReadyFailure(cassette.InputLifterZ, "InputLifterZ 서보 ON이 확인되지 않았습니다.");
                return false;
            }

            if (!ValidateInputCassetteManualCondition(host, false))
                return false;

            var sequence = CreateInputCassetteSequence(host);
            int result = await sequence.RunLoadingAsync(
                host.Controller.ManualOperationToken,
                BuildCassetteOptions(host, SequenceStartMode.Restart));
            if (result != 0)
                return false;

            QMC.Common.Log.Write("Main", "SYSTEM", "InputCassetteExchange",
                "INPUT 카세트 교체 준비를 완료했습니다. - Ok");

            QMC.Common.MessageDialog.Show(this,
                Lang.T("message.inputCassette.exchangeReady"),
                Lang.T("message.title.cassetteExchange"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        // 컨트롤러 상태 가드 — Output CanChangeOutputCassetteData 미러(2026-08-18 지시서 §2-2).
        private bool CanChangeInputCassetteData(Form1 host, string actionName)
        {
            if (host == null || host.Controller == null)
            {
                QMC.Common.MessageDialog.Show(this,
                    Lang.T("message.inputCassette.dataControllerMissing"),
                    Lang.T("message.title.cassetteData"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            MachineController controller = host.Controller;
            EquipmentStatus status = controller.Status;
            bool blocked = _manualSequenceRunning ||
                           controller.IsManualBusy ||
                           controller.IsSequenceRunning ||
                           controller.IsReadySequenceRunning ||
                           status == EquipmentStatus.AutoRunning ||
                           status == EquipmentStatus.ManualRunning ||
                           status == EquipmentStatus.Initializing;
            if (!blocked)
                return true;

            QMC.Common.MessageDialog.Show(this,
                Lang.Format("message.cassette.dataBusy", Lang.Display(actionName), status, controller.IsSequenceRunning, controller.IsManualBusy, controller.IsReadySequenceRunning),
                Lang.T("message.title.cassetteData"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // 진행 중 자재 가드 — InputFeeder/InputStage에 웨이퍼가 있으면 교체/초기화를 차단한다
        // (자동 반납은 이번 버튼의 범위 밖 — 반납/제거 후 재시도 안내).
        private bool CanPrepareInputCassetteExchange(string blockedTitleMessage)
        {
            WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
            if (feederWafer != null)
            {
                QMC.Common.MessageDialog.Show(this,
                    Lang.Format("message.inputCassette.feederOccupied", blockedTitleMessage, feederWafer.WaferId),
                    Lang.T("message.title.cassetteExchange"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            WaferMaterial stageWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (stageWafer != null)
            {
                QMC.Common.MessageDialog.Show(this,
                    Lang.Format("message.inputCassette.stageOccupied", blockedTitleMessage, stageWafer.WaferId),
                    Lang.T("message.title.cassetteExchange"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        // 완료된 INPUT 카세트만 교체 초기화한다. 다음 START에서 매핑을 다시 수행한다.
        private void CompleteInputCassetteExchange()
        {
            const string actionName = "INPUT CST CLEAR";
            try
            {
                // 버튼 클릭 시 먼저 의사를 확인하고, 예를 선택한 경우에만 검사/초기화를 시작한다.
                if (QMC.Common.MessageDialog.Show(this,
                    Lang.T("message.inputCassette.clearConfirm"),
                    Lang.T("message.title.inputCassetteClear"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    WriteEvent("INPUT-CST-CST-CLEAR", "사용자가 INPUT 카세트 초기화를 취소했습니다.");
                    return;
                }

                // 확인창 이후 운전 진입을 막고 실입력/Material 재검사부터 저장까지 보호한다.
                Form1 host = GetHost();
                if (!CanChangeInputCassetteData(host, actionName))
                    return;
                string clearReason;
                if (!TryCompleteInputCassetteClear(host, out clearReason))
                {
                    ShowInputCassetteClearFailure(clearReason);
                    return;
                }

                WriteEvent("INPUT-CST-CST-CLEAR", "cleared and saved.");
                RefreshSelectedMaterialDetail();
                RefreshFromMachine();
                QMC.Common.MessageDialog.Show(this,
                    Lang.T("message.inputCassette.clearComplete"),
                    Lang.T("message.title.cassetteExchange"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                WriteWarning("INPUT-CST-CST-CLEAR", "카세트 교체 완료 처리 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this,
                    Lang.Format("message.cassette.exchangeFailed", ex.Message),
                    Lang.T("message.title.cassetteExchange"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool TryCompleteInputCassetteClear(Form1 host, out string reason)
        {
            IDisposable operationScope;
            if (!host.Controller.TryBeginInputCassetteClearOperation(out operationScope, out reason))
                return false;

            using (operationScope)
            {
                bool mutationStarted = false;
                try
                {
                    mutationStarted = true;
                    if (!MaterialStateService.ClearInputCassetteForExchange(out reason))
                    {
                        mutationStarted = false;
                        return false;
                    }
                    if (!MaterialStateService.TryFlushPendingSave("InputCassetteCstClear"))
                    {
                        reason = "INPUT 카세트 데이터는 메모리에서 초기화했지만 저장하지 못했습니다. " +
                                 "START를 차단했습니다. 프로그램을 재시작하지 말고 저장 오류를 해결한 후 CST CLEAR를 다시 실행하십시오.\r\n" +
                                 EmptyToDash(MaterialSnapshotStore.LastSaveFailureReason);
                        host.Controller.ReportInputCassetteClearCompletion(false, reason);
                        return false;
                    }
                    if (!SyncInputRuntimeProjection())
                    {
                        reason = "INPUT 카세트 데이터는 저장했지만 유닛 상태 동기화에 실패했습니다. " +
                                 "START를 차단했습니다. 로그를 확인하고 CST CLEAR를 다시 실행하십시오.";
                        host.Controller.ReportInputCassetteClearCompletion(false, reason);
                        return false;
                    }
                    host.Controller.ReportInputCassetteClearCompletion(true, "");
                    return true;
                }
                catch (Exception ex)
                {
                    reason = "INPUT 카세트 초기화 처리 중 오류가 발생했습니다. error=" + ex.Message;
                    if (mutationStarted)
                    {
                        reason += "\r\nSTART를 차단했습니다. 현재 데이터와 저장 상태를 확인한 후 CST CLEAR를 다시 실행하십시오.";
                        host.Controller.ReportInputCassetteClearCompletion(false, reason);
                    }
                    return false;
                }
            }
        }

        private void ShowInputCassetteClearFailure(string reason)
        {
            WriteWarning("INPUT-CST-CST-CLEAR", "INPUT 카세트 초기화 미완료. reason=" + reason);
            QMC.Common.MessageDialog.Show(this,
                Lang.Format("message.inputCassette.clearIncomplete", reason),
                Lang.T("message.title.cassetteExchange"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private InputCassetteSequence CreateInputCassetteSequence(Form1 host)
        {
            try
            {
                var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                return new InputCassetteSequence(ctx);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private InputCassetteSequenceOptions BuildCassetteOptions(Form1 host, SequenceStartMode startMode)
        {
            var options = InputCassetteSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = startMode;
            options.MoveTimeoutMs = ResolveManualMoveTimeoutMs(host);
            options.FineMove = false;
            return options;
        }

        private void SelectMaterialSlot(CassetteMaterialRole role, int slotIndex)
        {
            try
            {
                _selectedCassetteRole = role;
                _selectedMaterialSlot = slotIndex;
                RefreshSelectedMaterialDetail();
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-SLOT", "Slot select failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task MoveSlotFromContextMenuAsync(CassetteMaterialRole role, int slotIndex)
        {
            SelectMaterialSlot(role, slotIndex);
            string actionName = "LIFT WAFER MOVE " + GetCassetteRoleDisplay(role) + " / " + (slotIndex + 1).ToString("00");
            await RunMotionAction(actionName, host => MoveSpecificSlotAsync(host, role, slotIndex));
        }

        private static bool ValidateInputCassetteManualCondition(Form1 host, bool mapping)
        {
            string reason;
            if (ValidateInputCassetteManualCondition(host, mapping, out reason))
                return true;

            string kind = mapping ? "Mapping" : "ManualMove";
            string message = "Input Cassette manual condition failed. kind=" + kind + ". " + reason;
            WriteAlarm("INPUT-CST-MANUAL-CONDITION", message);
            SequenceFailureStore.Record(
                "InputCassettePage.Manual",
                kind,
                "ValidateInputCassetteManualCondition",
                "INPUT-CST-MANUAL-CONDITION",
                LogSource,
                message);

            return false;
        }

        private static bool ValidateInputCassetteManualCondition(Form1 host, bool mapping, out string reason)
        {
            reason = string.Empty;

            if (host == null)
            {
                reason = "Form host is null.";
                return false;
            }

            if (host.Machine == null)
            {
                reason = "Machine is null.";
                return false;
            }

            var cassette = host != null && host.Machine != null ? host.Machine.InputCassetteUnit : null;
            if (cassette == null)
            {
                reason = "InputCassetteUnit is null.";
                return false;
            }

            if (cassette.Config == null)
            {
                reason = "InputCassette config is null.";
                return false;
            }

            if (cassette.Recipe == null)
            {
                reason = "InputCassette recipe is null.";
                return false;
            }

            if (cassette.Config.SlotCount <= 0)
            {
                reason = "InputCassette SlotCount is invalid. slotCount=" + cassette.Config.SlotCount;
                return false;
            }

            if (!cassette.CheckWaferCassetteMoveReady(out reason))
            {
                reason = "InputCassette move ready check failed. " + reason;
                return false;
            }

            if (IsHardwareBypassed(host))
                return true;

            int cassetteSize = MaterialStateService.ResolveWaferSizeInch(cassette.Config.InchSelect);
            if (!cassette.IsWaferCassettePresentAll(cassetteSize))
            {
                reason = "Input cassette is not fully detected. Both cassette sensors must be ON. cassetteSize=" + cassetteSize + ". " + BuildCassetteSensorSummary(cassette);
                return false;
            }

            if (mapping && !cassette.CheckWaferCassetteMappingReady(out reason))
            {
                reason = "InputCassette mapping ready check failed. " + reason;
                return false;
            }

            return true;
        }

        private static string BuildCassetteSensorSummary(InputCassetteUnit cassette)
        {
            if (cassette == null)
                return "Sensors: cassette=NULL.";

            return "Sensors: 8inch[0]=" + FormatInputState(cassette.Wafer8CassetteCheck0) +
                   ", 8inch[1]=" + FormatInputState(cassette.Wafer8CassetteCheck1) +
                   ", 12inch[0]=" + FormatInputState(cassette.Wafer12CassetteCheck0) +
                   ", 12inch[1]=" + FormatInputState(cassette.Wafer12CassetteCheck1) +
                   ", protrusion=" + FormatInputState(cassette.WaferRingJutCheck) + ".";
        }

        private static string FormatInputState(QMC.Common.IO.BaseDigitalInput input)
        {
            if (input == null)
                return "NULL";

            return input.IsOn ? "ON" : "OFF";
        }

        private static int ResolveManualMoveTimeoutMs(Form1 host)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.InputCassetteUnit : null;
            int configured = cassette != null ? cassette.ResolveWaferLifterZMoveTimeoutMs() : 0;
            return configured > 0 ? configured : 3000;
        }

        private static bool IsHardwareBypassed(Form1 host)
        {
            var settings = QMC.CDT320.AppSettingsStore.Current;
            return (settings != null && settings.BypassHardware) ||
                   (host != null && host.Controller != null && host.Controller.GlobalDryRun);
        }

        private void RefreshSelectedMaterialDetail()
        {
            if (materialDetailView == null)
                return;

            if (_selectedMaterialSlot < 0)
            {
                materialDetailView.Clear();
                return;
            }

            var snapshot = MaterialStorage.State;
            var cassette = snapshot != null && snapshot.Cassettes != null
                ? snapshot.Cassettes.FirstOrDefault(c => c.Role == _selectedCassetteRole)
                : null;
            var slot = cassette != null && cassette.Slots != null &&
                       _selectedMaterialSlot >= 0 && _selectedMaterialSlot < cassette.Slots.Count
                ? cassette.Slots[_selectedMaterialSlot]
                : null;
            var wafer = ResolveCassetteSlotWafer(snapshot, _selectedCassetteRole, _selectedMaterialSlot, slot);

            materialDetailView.SetRows("WAFER MATERIAL", BuildWaferMaterialRows(cassette, slot, wafer));
        }

        private System.Collections.Generic.IEnumerable<MaterialDetailRow> BuildWaferMaterialRows(CassetteMaterial cassette, CassetteSlotMaterial slot, WaferMaterial wafer)
        {
            bool mapped = cassette != null && cassette.IsMapped;
            string specName = wafer != null ? wafer.TapeFrameSpecName : "";
            var spec = !string.IsNullOrEmpty(specName) ? MaterialSpecs.FindFrame(specName) : null;

            return new[]
            {
                Row("Selected", _selectedCassetteRole + " / SLOT " + (_selectedMaterialSlot + 1).ToString("00"), "", false),
                Row("Cassette ID", cassette != null ? cassette.CassetteId : "", "", false),
                Row("Cassette Mapped", cassette != null && cassette.IsMapped ? "Y" : "N", "", false),
                Row("Slot", BuildSlotOccupancyText(slot, wafer, mapped), "", false),
                Row("Wafer ID", wafer != null ? wafer.WaferId : "", "WaferId", mapped),
                Row("Lot ID", wafer != null ? wafer.CassetteLotId : (cassette != null ? cassette.CassetteLotId : ""), "CassetteLotId", mapped),
                // 빈 슬롯의 State 편집으로 GetOrCreateWaferInMappedCassette가 자재를 암묵 생성하지 않게 한다.
                // 자재 생성은 DATA CREATE에서만 수행하고, 여기서는 실제로 표시된 Wafer 상태만 변경한다.
                Row("State", wafer != null ? WaferMaterialStateText.ToDisplayName(wafer.State) : "", "State", mapped && wafer != null),
                Row("Location", wafer != null && wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "", "", false),
                Row("Cassette Role", wafer != null ? wafer.SourceCassetteRole.ToString() : "", "", false),
                Row("Cassette Slot", wafer != null && wafer.SourceSlotNumber >= 0 ? (wafer.SourceSlotNumber + 1).ToString("00") : "", "", false),
                Row("Cassette Position", wafer != null ? FormatCassettePosition(wafer.CurrentCassetteSlotPosition) : "", "", false),
                Row("TapeFrame Spec", specName, "TapeFrameSpecName", mapped),
                Row("Spec Grid X", spec != null ? spec.DieMapX.ToString() : "", "", false),
                Row("Spec Grid Y", spec != null ? spec.DieMapY.ToString() : "", "", false),
                Row("Spec Pitch Gap X", spec != null ? spec.PitchX.ToString("0.###") : "", "", false),
                Row("Spec Pitch Gap Y", spec != null ? spec.PitchY.ToString("0.###") : "", "", false),
                Row("Spec Diameter", spec != null ? spec.OuterDiameterMm.ToString("0.###") : "", "", false),
                Row("Die Spec", spec != null ? spec.DieSpecName : "", "", false),
                Row("Updated", wafer != null ? wafer.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") : "", "", false)
            };
        }

        private static MaterialDetailRow Row(string name, string value, string key, bool editable)
        {
            return new MaterialDetailRow
            {
                Name = name,
                Value = value,
                Key = key,
                Editable = editable
            };
        }

        private void MaterialDetailView_EditRequested(object sender, MaterialDetailEditEventArgs e)
        {
            try
            {
                if (e == null || e.Row == null)
                    return;

                var snapshot = MaterialStorage.State;
                var cassette = snapshot != null && snapshot.Cassettes != null
                    ? snapshot.Cassettes.FirstOrDefault(c => c.Role == _selectedCassetteRole)
                    : null;

                if (cassette == null || !cassette.IsMapped)
                {
                    RaiseWarning("INPUT-CST-MATERIAL-EDIT", "Material edit requested before mapping.");
                    QMC.Common.MessageDialog.Show(this, Lang.T("message.material.mappedSlotRequired"), Lang.T("message.title.material"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool isStateEdit = e.Row.Key == "State";
                if (isStateEdit && !CanChangeInputCassetteMaterialState(GetHost()))
                    return;

                string newValue;
                if (!TryEditMaterialValue(e.Row, out newValue))
                    return;

                bool ok;
                if (isStateEdit)
                {
                    // 상태 선택창이 열린 동안 시퀀스가 시작됐을 수 있으므로 실제 반영 직전에 다시 확인한다.
                    if (!CanChangeInputCassetteMaterialState(GetHost()))
                        return;

                    CassetteSlotMaterial slot = cassette.Slots != null &&
                                                  _selectedMaterialSlot >= 0 &&
                                                  _selectedMaterialSlot < cassette.Slots.Count
                        ? cassette.Slots[_selectedMaterialSlot]
                        : null;
                    WaferMaterial targetWafer = ResolveCassetteSlotWafer(
                        snapshot,
                        _selectedCassetteRole,
                        _selectedMaterialSlot,
                        slot);
                    if (targetWafer == null)
                    {
                        RaiseWarning("INPUT-CST-MATERIAL-STATE-NO-DATA",
                            "Wafer 상태 변경 대상 Material이 없습니다. role=" + _selectedCassetteRole +
                            ", slot=" + (_selectedMaterialSlot + 1));
                        QMC.Common.MessageDialog.Show(this,
                            Lang.T("message.material.slotEmptyForState"),
                            Lang.T("message.title.waferState"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string waferId = targetWafer.WaferId;
                    string beforeState = WaferMaterialStateText.ToDisplayName(
                        WaferMaterialStateText.Normalize(targetWafer.State));
                    string location = targetWafer.CurrentLocation != null
                        ? targetWafer.CurrentLocation.ToString()
                        : "-";

                    // 상태만 변경하는 전용 API를 사용한다. InputFeeder/InputStage에 나가 있는 Wafer의
                    // CurrentLocation과 원본 슬롯 포인터를 카세트 위치로 되돌리지 않는다.
                    ok = MaterialStateService.UpdateWaferStateOnly(
                        waferId,
                        newValue,
                        QMC.CDT_320.Ui.Security.UserSession.Name);

                    bool projectionSynchronized = ok && SyncInputRuntimeProjection();
                    bool persisted = ok &&
                        MaterialStateService.TryFlushPendingSave("InputCassetteManualStateUpdate");

                    WriteEvent("INPUT-CST-MATERIAL-STATE",
                        "Wafer 상태 변경. material=" + waferId +
                        ", role=" + _selectedCassetteRole +
                        ", slot=" + (_selectedMaterialSlot + 1) +
                        ", state=" + beforeState + "->" + newValue +
                        ", location=" + location +
                        ", projectionSynchronized=" + projectionSynchronized +
                        ", persisted=" + persisted +
                        ", result=" + ok);

                    if (ok && (!projectionSynchronized || !persisted))
                    {
                        RaiseWarning("INPUT-CST-MATERIAL-STATE-CONSISTENCY",
                            "Wafer 상태는 메모리에 반영됐지만 Unit 동기화 또는 Snapshot 저장에 실패했습니다. " +
                            "material=" + waferId +
                            ", projectionSynchronized=" + projectionSynchronized +
                            ", persisted=" + persisted);
                        QMC.Common.MessageDialog.Show(this,
                            Lang.T("message.material.waferStateSaveFailed"),
                            Lang.T("message.title.waferStateSaveFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        RefreshSelectedMaterialDetail();
                        RefreshFromMachine();
                        return;
                    }
                }
                else
                {
                    // Wafer ID/LOT/TapeFrame 등 기존 편집 경로는 이번 작업에서 변경하지 않는다.
                    ok = MaterialStateService.UpdateWaferFieldInMappedCassette(
                        _selectedCassetteRole,
                        _selectedMaterialSlot,
                        e.Row.Key,
                        newValue);
                }

                WriteEvent("INPUT-CST-MATERIAL", e.Row.Key + " update result=" + ok);
                if (!ok)
                {
                    RaiseWarning("INPUT-CST-MATERIAL-FAIL", e.Row.Key + " update failed.");
                    QMC.Common.MessageDialog.Show(this, Lang.T("message.material.valueChangeFailed"), Lang.T("message.title.material"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                RefreshSelectedMaterialDetail();
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-MATERIAL-EX", "Material edit failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("message.title.material"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void MaterialDetailView_CreateDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (_selectedMaterialSlot < 0)
                    return;

                if (!ConfirmMaterialDataAction(Lang.T("message.material.slotCreateConfirm")))
                    return;

                var wafer = MaterialStateService.GetOrCreateWaferInMappedCassette(_selectedCassetteRole, _selectedMaterialSlot, "");
                if (wafer == null)
                {
                    RaiseWarning("INPUT-CST-DATA-CREATE", "Material data create failed. Mapping is required.");
                    QMC.Common.MessageDialog.Show(this, Lang.T("message.material.mappedSlotCreateRequired"), Lang.T("message.title.material"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                WriteEvent("INPUT-CST-DATA-CREATE", "slot=" + _selectedCassetteRole + "/" + (_selectedMaterialSlot + 1).ToString("00") + ", wafer=" + wafer.WaferId);
                RefreshSelectedMaterialDetail();
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-DATA-CREATE-EX", "Material data create failed: " + ex.Message);
            }
        }

        private void MaterialDetailView_CreateProcessTestDataRequested(object sender, EventArgs e)
        {
            try
            {
                string reason;
                if (!CanCreateProcessTestData(out reason))
                {
                    QMC.Common.MessageDialog.Show(this, reason, Lang.T("message.title.processTestData"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!ConfirmMaterialDataAction(
                    Lang.T("message.material.processTestCreateConfirm")))
                    return;

                var host = GetHost();
                ClearProcessTestRuntimeState();
                string message;
                InputStageUnit inputStage = host.Controller.Machine != null ? host.Controller.Machine.InputStageUnit : null;
                // 실장비 테스트용: 카세트 유닛을 함께 넘겨 슬롯별 카세트 포지션까지 실제 맵핑과 동일하게 저장한다.
                InputCassetteUnit inputCassette = host.Controller.Machine != null ? host.Controller.Machine.InputCassetteUnit : null;
                OutputCassetteUnit outputCassette = host.Controller.Machine != null ? host.Controller.Machine.OutputCassetteUnit : null;
                bool ok = MaterialStateService.CreateProcessTestDataSet(inputStage, inputCassette, outputCassette, out message);
                if (ok && host.Controller.Machine != null && host.Controller.Machine.InputCassetteUnit != null)
                    SynchronizeInputCassetteSlotStates(host.Controller.Machine.InputCassetteUnit);
                WriteEvent("INPUT-CST-PROCESS-TEST-DATA", message + ", result=" + ok);
                if (!ok)
                {
                    RaiseWarning("INPUT-CST-PROCESS-TEST-DATA", message);
                    QMC.Common.MessageDialog.Show(this, message, Lang.T("message.title.processTestData"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                QMC.Common.MessageDialog.Show(this, message, Lang.T("message.title.processTestData"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshSelectedMaterialDetail();
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-PROCESS-TEST-DATA-EX", "공정 테스트 Data 생성 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, Lang.Format("message.material.processTestCreateFailed", ex.Message), Lang.T("message.title.processTestData"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void SynchronizeInputCassetteSlotStates(InputCassetteUnit cassette)
        {
            try
            {
                if (cassette == null)
                    return;

                string summary;
                if (!cassette.TrySynchronizeSlotProjectionFromMaterialState(out summary))
                    throw new InvalidOperationException(summary);

                WriteEvent("INPUT-CST-SLOT-SYNC",
                    "Input Cassette slot projection 동기화 완료. " + summary);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Input Cassette slot 상태 동기화 실패: " + ex.Message, ex);
            }
            finally
            {
            }
        }

        private void ClearProcessTestRuntimeState()
        {
            try
            {
                InputCameraPreInspectionCoordinator.Clear(PickerSequenceSide.Front);
                InputCameraPreInspectionCoordinator.Clear(PickerSequenceSide.Rear);
                SequenceResumeStore.ClearAll();
                SequenceFailureStore.Clear();
                WriteEvent("INPUT-CST-PROCESS-TEST-RUNTIME-CLEAR",
                    "공정 테스트 Data 생성 전 InputCamera 선행검사 허가와 시퀀스 재개 상태를 초기화했습니다.");
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-PROCESS-TEST-RUNTIME-CLEAR-EX",
                    "공정 테스트 Data 런타임 상태 초기화 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void MaterialDetailView_ClearDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (_selectedMaterialSlot < 0)
                    return;

                if (!ConfirmMaterialDataAction(Lang.T("message.material.slotClearConfirm")))
                    return;

                // 전체 초기화와 동일 정책: 차단/저장실패를 구분하고 사유를 그대로 표시한다.
                string clearReason;
                bool cleared = MaterialStateService.ClearInputCassetteSlotData(
                    _selectedCassetteRole, _selectedMaterialSlot, out clearReason);
                bool saved = cleared && MaterialStateService.TryFlushPendingSave("InputCassetteSlotDataClear");
                WriteEvent("INPUT-CST-DATA-CLEAR",
                    "slot=" + _selectedCassetteRole + "/" + (_selectedMaterialSlot + 1).ToString("00") +
                    ", cleared=" + cleared + ", saved=" + saved + ", reason=" + (clearReason ?? ""));
                if (!cleared)
                {
                    QMC.Common.MessageDialog.Show(
                        this,
                        Lang.Format("message.inputCassette.slotClearBlocked", (string.IsNullOrWhiteSpace(clearReason) ? Lang.T("message.reason.none") : clearReason)),
                        Lang.T("message.title.materialData"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                else if (!saved)
                {
                    QMC.Common.MessageDialog.Show(
                        this,
                        Lang.Format("message.cassette.slotClearSaveFailed", EmptyToDash(QMC.CDT320.Materials.MaterialSnapshotStore.LastSaveFailureReason)),
                        Lang.T("message.title.materialData"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                RefreshSelectedMaterialDetail();
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-DATA-CLEAR-EX", "Material data clear failed: " + ex.Message);
            }
        }

        private void MaterialDetailView_ClearAllDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmMaterialDataAction(Lang.T("message.inputCassette.allClearConfirm")))
                    return;

                // [사용자 확정 2026-08-17] "초기화 차단"과 "저장 실패"를 구분해 사유를 화면에 그대로 표시한다.
                //   기존에는 두 경우가 같은 문구("저장 파일까지 초기화하지 못했습니다")로 표시돼,
                //   실제로는 사전검사에서 막혀 저장을 시도조차 안 한 경우에도 저장 실패로 오인됐다.
                string clearReason;
                bool cleared = MaterialStateService.ClearInputCassetteAllSlotData(out clearReason);
                bool saved = cleared && MaterialStateService.TryFlushPendingSave("InputCassetteAllDataClear");
                WriteEvent("INPUT-CST-DATA-ALL-CLEAR",
                    "cleared=" + cleared + ", saved=" + saved + ", reason=" + (clearReason ?? ""));
                if (!cleared)
                {
                    QMC.Common.MessageDialog.Show(
                        this,
                        Lang.Format("message.inputCassette.allClearBlocked", (string.IsNullOrWhiteSpace(clearReason) ? Lang.T("message.reason.none") : clearReason)),
                        Lang.T("message.title.materialData"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                else if (!saved)
                {
                    QMC.Common.MessageDialog.Show(
                        this,
                        Lang.Format("message.inputCassette.allClearSaveFailed", EmptyToDash(QMC.CDT320.Materials.MaterialSnapshotStore.LastSaveFailureReason)),
                        Lang.T("message.title.materialData"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                RefreshSelectedMaterialDetail();
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-DATA-ALL-CLEAR-EX", "Material data all clear failed: " + ex.Message);
            }
        }

        /// <summary>저장 실패 사유가 비어 있을 때 화면에 빈 칸이 나오지 않게 한다.</summary>
        private static string EmptyToDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? Lang.T("message.reason.checkSaveLog") : value;
        }

        private bool ConfirmMaterialDataAction(string message)
        {
            return QMC.Common.MessageDialog.Show(this, message, Lang.T("message.title.materialData"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private bool CanCreateProcessTestData(out string reason)
        {
            reason = string.Empty;
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null)
                {
                    reason = "MachineController가 준비되지 않아 공정 테스트 Data를 생성할 수 없습니다.";
                    return false;
                }

                if (AlarmManager.HasActive)
                {
                    reason = "현재 알람 상태입니다. 알람 해제 후 공정 테스트 Data를 생성하세요.";
                    return false;
                }

                if (_manualSequenceRunning || host.Controller.IsManualBusy)
                {
                    reason = "수동 동작이 실행 중입니다. 완료 후 공정 테스트 Data를 생성하세요.";
                    return false;
                }

                if (host.Controller.IsSequenceRunning || host.Controller.Status == EquipmentStatus.AutoRunning)
                {
                    reason = "시퀀스 실행 중에는 공정 테스트 Data를 생성할 수 없습니다.";
                    return false;
                }

                if (host.Controller.Status == EquipmentStatus.Alarm)
                {
                    reason = "장비 상태가 Alarm입니다. 알람 해제 후 공정 테스트 Data를 생성하세요.";
                    return false;
                }

                if (!IsProcessTestTransferPathEmpty(host.Controller.Machine, out reason))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                reason = "공정 테스트 Data 생성 조건 확인 실패: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private static bool IsProcessTestTransferPathEmpty(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            if (machine == null ||
                machine.InputFeederUnit == null ||
                machine.OutputFeederUnit == null ||
                machine.PickerFrontUnit == null ||
                machine.PickerRearUnit == null)
            {
                reason = "이송 유닛 상태를 확인할 수 없어 공정 테스트 Data 생성을 차단합니다.";
                return false;
            }

            WaferMaterial inputFeederWafer =
                MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
            bool inputFeederEmpty = machine.InputFeederUnit.IsWaferFeederEmpty();
            bool inputFeederDetected = machine.InputFeederUnit.IsWaferFeederRingDetected(true);
            if (inputFeederWafer != null || !inputFeederEmpty)
            {
                reason = "InputFeeder에 잔류 Wafer가 있어 공정 테스트 Data를 생성할 수 없습니다. " +
                         "Material=" + (inputFeederWafer != null ? inputFeederWafer.WaferId : "") +
                         ", EmptyContract=" + inputFeederEmpty +
                         ", RingDetected=" + inputFeederDetected + ". 먼저 Wafer를 안전하게 회수하세요.";
                return false;
            }

            WaferMaterial outputFeederWafer =
                MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            bool outputFeederEmpty = machine.OutputFeederUnit.IsFeederEmpty();
            bool outputFeederDetected = machine.OutputFeederUnit.IsFeederRingDetected(true);
            if (outputFeederWafer != null || !outputFeederEmpty)
            {
                reason = "OutputFeeder에 잔류 Bin이 있어 공정 테스트 Data를 생성할 수 없습니다. " +
                         "Material=" + (outputFeederWafer != null ? outputFeederWafer.WaferId : "") +
                         ", EmptyContract=" + outputFeederEmpty +
                         ", RingDetected=" + outputFeederDetected + ". 먼저 Bin을 안전하게 회수하세요.";
                return false;
            }

            int frontPickerCount = machine.PickerFrontUnit.Vacuums != null
                ? machine.PickerFrontUnit.Vacuums.Length
                : 0;
            for (int pickerNo = 1; pickerNo <= frontPickerCount; pickerNo++)
            {
                DieMaterial die = MaterialStateService.GetDieAtPicker(
                    MaterialLocationKind.PickerFront,
                    pickerNo);
                bool vacuumOn =
                    machine.PickerFrontUnit.Vacuums[pickerNo - 1] != null &&
                    machine.PickerFrontUnit.Vacuums[pickerNo - 1].IsOn;
                if (die != null || vacuumOn)
                {
                    reason = "FrontPicker에 잔류 Die 또는 Vacuum ON 상태가 있어 공정 테스트 Data를 생성할 수 없습니다. " +
                             "Picker=" + pickerNo +
                             ", Die=" + (die != null ? die.DieId : "") +
                             ", VacuumOn=" + vacuumOn + ". 먼저 Die를 안전하게 회수하세요.";
                    return false;
                }
            }

            int rearPickerCount = machine.PickerRearUnit.Vacuums != null
                ? machine.PickerRearUnit.Vacuums.Length
                : 0;
            for (int pickerNo = 1; pickerNo <= rearPickerCount; pickerNo++)
            {
                DieMaterial die = MaterialStateService.GetDieAtPicker(
                    MaterialLocationKind.PickerRear,
                    pickerNo);
                bool vacuumOn =
                    machine.PickerRearUnit.Vacuums[pickerNo - 1] != null &&
                    machine.PickerRearUnit.Vacuums[pickerNo - 1].IsOn;
                if (die != null || vacuumOn)
                {
                    reason = "RearPicker에 잔류 Die 또는 Vacuum ON 상태가 있어 공정 테스트 Data를 생성할 수 없습니다. " +
                             "Picker=" + pickerNo +
                             ", Die=" + (die != null ? die.DieId : "") +
                             ", VacuumOn=" + vacuumOn + ". 먼저 Die를 안전하게 회수하세요.";
                    return false;
                }
            }

            return true;
        }

        private bool TryEditMaterialValue(MaterialDetailRow row, out string value)
        {
            value = row != null ? row.Value : "";
            if (row == null)
                return false;

            if (row.Key == "State")
            {
                var states = WaferMaterialStateText.DisplayNames;
                using (var dialog = new EnumPickerDialog("Wafer State", states, row.Value))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return false;
                    value = dialog.SelectedValue;
                    return true;
                }
            }

            if (row.Key == "TapeFrameSpecName")
            {
                var specs = MaterialSpecs.Data != null && MaterialSpecs.Data.Frames != null
                    ? MaterialSpecs.Data.Frames.Select(f => f.Name).Where(n => !string.IsNullOrEmpty(n)).ToArray()
                    : new string[0];

                using (var dialog = new EnumPickerDialog("TapeFrame Spec", specs, row.Value))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return false;
                    value = dialog.SelectedValue;
                    return true;
                }
            }

            using (var dialog = new MaterialValueEditDialog(row.Name, row.Value))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return false;
                value = dialog.ValueText;
                return true;
            }
        }

        private async Task<int> MoveSlotAsync(Form1 host, int delta)
        {
            try
            {
                var loader = host != null && host.Machine != null ? host.Machine.InputCassetteUnit : null;
                if (loader == null || loader.Config == null)
                    return -1;

                int slotCount = loader.Config.SlotCount;
                if (slotCount <= 0)
                    return -1;

                int currentSlot = _selectedMaterialSlot >= 0 ? _selectedMaterialSlot : (host.Controller != null ? host.Controller.CurrentInputSlot : -1);
                if (currentSlot < 0)
                    currentSlot = delta >= 0 ? 0 : slotCount - 1;

                int targetSlot = Math.Max(0, Math.Min(slotCount - 1, currentSlot + delta));
                if (targetSlot == currentSlot)
                    return 0;

                return await MoveSpecificSlotAsync(host, _selectedCassetteRole, targetSlot);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-SLOT-MOVE", "Slot move failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveSpecificSlotAsync(Form1 host, CassetteMaterialRole role, int slotIndex)
        {
            try
            {
                var loader = host != null && host.Machine != null ? host.Machine.InputCassetteUnit : null;
                if (loader == null || loader.Config == null)
                    return -1;

                if (slotIndex < 0 || slotIndex >= loader.Config.SlotCount)
                    return -1;

                // 이동 전 준비 조건(서보/알람/이동 중/돌출/카세트 감지)을 확인한다.
                if (!ValidateInputCassetteManualCondition(host, false))
                    return -1;

                // 기존 조건: Material 저장 위치(TryGetCassetteSlotPosition)를 물리 목표로 직접 사용 —
                //           First/Pitch/Mapping 변경 후 오래된 값일 수 있다.
                // 현재 기준: 중앙 계산기(Resolver)로 현재 티칭/맵핑 기준 목표를 재계산하고
                //           유효성(FirstSlot/Pitch/단조 증가/소프트리밋)을 통과해야 이동한다.
                var resolve = loader.ResolveManualWaferCassetteSlotTarget(role, slotIndex);
                if (!resolve.IsValid)
                {
                    RaiseWarning("INPUT-CST-SLOT-TARGET", "Slot target resolve failed. role=" + role +
                        ", slot=" + (slotIndex + 1).ToString("00") + ". " + resolve.FailureReason);
                    SequenceFailureStore.Record(
                        "InputCassettePage.Manual",
                        "SlotTarget",
                        "ResolveManualWaferCassetteSlotTarget",
                        "INPUT-CST-SLOT-TARGET",
                        LogSource,
                        resolve.FailureReason);
                    return -1;
                }

                double targetPosition = resolve.TargetPosition;
                WriteEvent("INPUT-CST-SLOT-TARGET",
                    "Slot move target resolved. role=" + resolve.RoleName +
                    ", slot=" + resolve.SlotNumber.ToString("00") +
                    ", targetSource=" + resolve.TargetSourceText +
                    ", target=" + targetPosition.ToString("0.###") +
                    ", current=" + (loader.InputLifterZ != null ? loader.InputLifterZ.ActualPosition.ToString("0.###") : "-"));

                SelectMaterialSlot(role, slotIndex);
                // 기존 조건: Fine(미세 조그 속도) — 현재 기준: 수동 슬롯 이동은 Coarse(일반 조그 속도)로 구동한다.
                int moveResult = await loader.MoveWaferLifterZ(targetPosition, JogSpeedType.Coarse, 0.0);
                if (moveResult != 0)
                {
                    RecordSlotMoveFailure(loader, resolve, moveResult, "Move command failed.");
                    return moveResult;
                }

                // 이동 함수가 완료를 보장하지만(R3), 수동 슬롯 이동은 최종 InPosition/tolerance를 한 번 더 확인한다.
                string arrivalReason;
                if (!VerifyInputLifterZArrival(loader, targetPosition, out arrivalReason))
                {
                    RecordSlotMoveFailure(loader, resolve, -1, arrivalReason);
                    return -1;
                }

                RefreshSelectedMaterialDetail();
                return 0;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-SLOT-MOVE", "Slot move failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        // 더블클릭 슬롯 물리 이동: "실제 카세트 안에 있는" Material이 있을 때만 전용 확인창(1회)을 거쳐 이동한다.
        // 물리 이동만 수행하며 Material 데이터는 변경하지 않는다.
        private async Task MoveSlotFromDoubleClickAsync(CassetteMaterialRole role, int slotIndex)
        {
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null || host.Machine == null)
                    return;

                // 연속 더블클릭/연타 재진입 방지. (RunMotionAction에서도 재검사한다.)
                if (_manualSequenceRunning)
                    return;

                // 요청 순간의 Role/Slot을 로컬로 고정해 새로고침/선택 변경에 영향받지 않게 한다.
                CassetteMaterialRole requestRole = role;
                int requestSlotIndex = slotIndex;

                SelectMaterialSlot(requestRole, requestSlotIndex);

                string waferId;
                string blockReason;
                if (!TryGetCassetteMaterialForPhysicalMove(requestRole, requestSlotIndex, out waferId, out blockReason))
                {
                    WriteEvent("INPUT-CST-DBLCLK-BLOCK",
                        "Slot double-click move blocked. role=" + requestRole +
                        ", slot=" + (requestSlotIndex + 1).ToString("00") + ". " + blockReason);
                    QMC.Common.MessageDialog.Show(
                        this,
                        Lang.Format("message.cassette.slotLocationMismatch", blockReason),
                        Lang.T("message.title.inputCassette"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var loader = host.Machine.InputCassetteUnit;
                if (loader == null)
                    return;

                var resolve = loader.ResolveManualWaferCassetteSlotTarget(requestRole, requestSlotIndex);
                if (!resolve.IsValid)
                {
                    RaiseWarning("INPUT-CST-DBLCLK-TARGET", "Slot double-click target resolve failed. " + resolve.FailureReason);
                    QMC.Common.MessageDialog.Show(this, resolve.FailureReason, Lang.T("message.title.inputCassette"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 더블클릭 전용 확인창(정확히 1회). 아래 RunMotionAction은 confirm=false로 호출해 중복 확인창을 막는다.
                string message =
                    Lang.Format("message.cassette.moveWaferConfirm", resolve.RoleName, resolve.SlotNumber.ToString("00"), waferId, resolve.TargetPosition.ToString("0.###"), resolve.TargetSourceText);
                DialogResult answer = QMC.Common.MessageDialog.Show(this, message, Lang.T("message.title.inputCassette"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    // 사용자 취소는 인터락 차단과 구분해 기록한다. Motion 0건, Material 변경 0건.
                    WriteEvent("INPUT-CST-DBLCLK-CANCEL",
                        "Slot double-click move canceled by user. role=" + resolve.RoleName +
                        ", slot=" + resolve.SlotNumber.ToString("00") +
                        ", waferId=" + waferId +
                        ", target=" + resolve.TargetPosition.ToString("0.###"));
                    return;
                }

                string actionName = "SLOT DBL-CLICK MOVE " + GetCassetteRoleDisplay(requestRole) + " / " + resolve.SlotNumber.ToString("00");
                await RunMotionAction(actionName, host2 => MoveSpecificSlotAsync(host2, requestRole, requestSlotIndex), false);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-DBLCLK", "Slot double-click move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        // 더블클릭 물리 이동용 Material 실위치 판정.
        // 슬롯 표시(투영)만으로 판단하지 않고 CurrentLocation이 실제 해당 Cassette Role/Slot인 경우만 인정한다.
        private static bool TryGetCassetteMaterialForPhysicalMove(
            CassetteMaterialRole role,
            int slotIndex,
            out string waferId,
            out string reason)
        {
            waferId = string.Empty;
            reason = string.Empty;
            try
            {
                var snapshot = MaterialStorage.State;
                var cassette = snapshot != null && snapshot.Cassettes != null
                    ? snapshot.Cassettes.FirstOrDefault(c => c != null && c.Role == role)
                    : null;
                if (cassette == null)
                {
                    reason = "카세트 상태 데이터가 없습니다. role=" + role;
                    return false;
                }

                if (!cassette.IsMapped)
                {
                    reason = "카세트가 Mapping 완료 상태가 아닙니다.";
                    return false;
                }

                var slot = cassette.Slots != null && slotIndex >= 0 && slotIndex < cassette.Slots.Count
                    ? cassette.Slots[slotIndex]
                    : null;
                if (slot == null)
                {
                    reason = "슬롯 데이터가 없습니다. slot=" + (slotIndex + 1).ToString("00");
                    return false;
                }

                if (!slot.HasWafer)
                {
                    reason = "슬롯 점유(HasWafer) 표시가 없습니다.";
                    return false;
                }

                if (string.IsNullOrEmpty(slot.WaferId))
                {
                    reason = "슬롯 Material ID가 비어 있습니다.";
                    return false;
                }

                var wafer = ResolveCassetteSlotWafer(snapshot, role, slotIndex, slot);
                if (wafer == null)
                {
                    reason = "슬롯이 가리키는 Material 객체가 없습니다. waferId=" + slot.WaferId;
                    return false;
                }

                if (WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty)
                {
                    reason = "Material 상태가 EMPTY입니다. waferId=" + wafer.WaferId;
                    return false;
                }

                var location = wafer.CurrentLocation;
                if (location == null || location.Kind != MaterialLocationKind.InputCassette)
                {
                    reason = "Material 현재 위치가 Input Cassette가 아닙니다. waferId=" + wafer.WaferId +
                             ", location=" + (location != null ? location.ToString() : "NULL");
                    return false;
                }

                if (location.CassetteRole != role)
                {
                    reason = "Material 현재 Cassette Role이 선택 Role과 다릅니다. waferId=" + wafer.WaferId +
                             ", current=" + location.CassetteRole + ", selected=" + role;
                    return false;
                }

                if (location.SlotNumber != slotIndex)
                {
                    reason = "Material 현재 Slot이 선택 Slot과 다릅니다. waferId=" + wafer.WaferId +
                             ", current=" + (location.SlotNumber + 1).ToString("00") +
                             ", selected=" + (slotIndex + 1).ToString("00");
                    return false;
                }

                waferId = wafer.WaferId;
                return true;
            }
            catch (Exception ex)
            {
                reason = "Material 실위치 판정 중 예외가 발생했습니다: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        // 수동 슬롯 이동 완료 판정: Servo ON, Alarm OFF, Moving OFF, 목표 tolerance 도달을 확인한다.
        private static bool VerifyInputLifterZArrival(InputCassetteUnit loader, double targetPosition, out string reason)
        {
            reason = string.Empty;
            var axis = loader != null ? loader.InputLifterZ : null;
            if (axis == null)
            {
                reason = "InputLifterZ axis is null.";
                return false;
            }

            double tolerance = loader.ResolveWaferLifterZInPositionTolerance();
            double error = Math.Abs(axis.ActualPosition - targetPosition);
            if (axis.IsServoOn && !axis.IsAlarm && !axis.IsMoving && error <= tolerance)
                return true;

            reason = "InputLifterZ arrival verify failed." +
                     " target=" + targetPosition.ToString("0.###") +
                     ", actual=" + axis.ActualPosition.ToString("0.###") +
                     ", command=" + axis.CommandPosition.ToString("0.###") +
                     ", error=" + error.ToString("0.###") +
                     ", tolerance=" + tolerance.ToString("0.###") +
                     ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                     ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                     ", moving=" + (axis.IsMoving ? "ON" : "OFF") +
                     ", inPosition=" + (error <= tolerance ? "ON" : "OFF");
            return false;
        }

        // 수동 슬롯 이동 실패 상세(Role/Slot/Target/Actual/Command/Servo/Alarm/Moving/InPosition)를 로그와 실패 팝업에 남긴다.
        private void RecordSlotMoveFailure(InputCassetteUnit loader, CassetteSlotTargetResolveResult resolve, int resultCode, string detail)
        {
            try
            {
                var axis = loader != null ? loader.InputLifterZ : null;
                double tolerance = loader != null ? loader.ResolveWaferLifterZInPositionTolerance() : 0.0;
                string message =
                    "Slot move failed. role=" + resolve.RoleName +
                    ", slot=" + resolve.SlotNumber.ToString("00") +
                    ", targetSource=" + resolve.TargetSourceText +
                    ", target=" + resolve.TargetPosition.ToString("0.###") +
                    ", actual=" + (axis != null ? axis.ActualPosition.ToString("0.###") : "-") +
                    ", command=" + (axis != null ? axis.CommandPosition.ToString("0.###") : "-") +
                    ", result=" + resultCode +
                    ", servo=" + (axis != null && axis.IsServoOn ? "ON" : "OFF") +
                    ", alarm=" + (axis != null && axis.IsAlarm ? "ON" : "OFF") +
                    ", moving=" + (axis != null && axis.IsMoving ? "ON" : "OFF") +
                    ", inPosition=" + (axis != null && Math.Abs(axis.ActualPosition - resolve.TargetPosition) <= tolerance ? "ON" : "OFF") +
                    ". " + detail;

                RaiseWarning("INPUT-CST-SLOT-MOVE-FAIL", message);
                SequenceFailureStore.Record(
                    "InputCassettePage.Manual",
                    "SlotMove",
                    "MoveSpecificSlotAsync",
                    "INPUT-CST-SLOT-MOVE-FAIL",
                    LogSource,
                    message);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-SLOT-MOVE-LOG", "Slot move failure logging failed: " + ex.Message);
            }
            finally
            {
            }
        }

        // ===== DATA ONLY (장비 무동작) Material 데이터 이동/삭제 =====
        // 이 영역의 handler는 Motion/Sequence/Cylinder/Vacuum/IO를 절대 호출하지 않는다.
        // 데이터 변경은 MaterialStateService의 원자적 API가 수행한다.

        private bool _dataOnlyBusy;

        private sealed class DataOnlyLocationItem
        {
            public DataOnlyLocationItem(DataOnlyLocation location, string text)
            {
                Location = location;
                Text = text;
            }

            public DataOnlyLocation Location { get; private set; }

            public string Text { get; private set; }

            public override string ToString()
            {
                return Text;
            }
        }

        private void RebuildDataOnlySourceItems()
        {
            try
            {
                var previous = cmbDataOnlySource.SelectedItem as DataOnlyLocationItem;
                cmbDataOnlySource.Items.Clear();

                var snapshot = MaterialStorage.State;
                AddDataOnlyCassetteSourceItems(snapshot, CassetteMaterialRole.Input1);
                AddDataOnlyCassetteSourceItems(snapshot, CassetteMaterialRole.Input2);
                AddDataOnlyStationSourceItem(snapshot, MaterialLocationKind.InputFeeder);
                AddDataOnlyStationSourceItem(snapshot, MaterialLocationKind.InputStage);

                RestoreDataOnlySelection(cmbDataOnlySource, previous);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-DATAONLY-UI", "DATA ONLY source 목록 갱신 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void AddDataOnlyCassetteSourceItems(MaterialSnapshot snapshot, CassetteMaterialRole role)
        {
            var cassette = snapshot != null && snapshot.Cassettes != null
                ? snapshot.Cassettes.FirstOrDefault(c => c != null && c.Role == role)
                : null;
            if (cassette == null || cassette.Slots == null)
                return;

            for (int i = 0; i < cassette.Slots.Count; i++)
            {
                var slot = cassette.Slots[i];
                if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                    continue;

                var location = DataOnlyLocation.Cassette(role, i);
                cmbDataOnlySource.Items.Add(new DataOnlyLocationItem(location, location.DisplayText + "  [" + slot.WaferId + "]"));
            }
        }

        private void AddDataOnlyStationSourceItem(MaterialSnapshot snapshot, MaterialLocationKind kind)
        {
            if (snapshot == null || snapshot.Wafers == null)
                return;

            var wafers = snapshot.Wafers
                .Where(w => w != null &&
                            w.CurrentLocation != null &&
                            w.CurrentLocation.Kind == kind &&
                            WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty)
                .ToList();
            if (wafers.Count == 0)
                return;

            var location = DataOnlyLocation.Station(kind);
            string idText = wafers.Count == 1 ? wafers[0].WaferId : "다중(" + wafers.Count + ")";
            cmbDataOnlySource.Items.Add(new DataOnlyLocationItem(location, location.DisplayText + "  [" + idText + "]"));
        }

        private void OnDataOnlySourceChanged()
        {
            try
            {
                var item = cmbDataOnlySource.SelectedItem as DataOnlyLocationItem;
                lblDataOnlyMaterialValue.Text = item != null ? ResolveDataOnlyMaterialId(item.Location) : "-";
                RebuildDataOnlyDestItems(true);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-DATAONLY-UI", "DATA ONLY 선택 갱신 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void RebuildDataOnlyDestItems(bool preferOriginalCassetteSlot = false)
        {
            try
            {
                var previous = cmbDataOnlyDest.SelectedItem as DataOnlyLocationItem;
                cmbDataOnlyDest.Items.Clear();

                var sourceItem = cmbDataOnlySource.SelectedItem as DataOnlyLocationItem;
                if (sourceItem == null)
                    return;

                var snapshot = MaterialStorage.State;
                switch (sourceItem.Location.Kind)
                {
                    case MaterialLocationKind.InputCassette:
                        AddDataOnlyStationDestItem(snapshot, MaterialLocationKind.InputFeeder);
                        break;
                    case MaterialLocationKind.InputFeeder:
                        AddDataOnlyCassetteDestItems(snapshot, CassetteMaterialRole.Input1);
                        AddDataOnlyCassetteDestItems(snapshot, CassetteMaterialRole.Input2);
                        AddDataOnlyStationDestItem(snapshot, MaterialLocationKind.InputStage);
                        break;
                    case MaterialLocationKind.InputStage:
                        AddDataOnlyStationDestItem(snapshot, MaterialLocationKind.InputFeeder);
                        break;
                }

                if (preferOriginalCassetteSlot &&
                    TrySelectOriginalCassetteDestination(snapshot, sourceItem))
                {
                    return;
                }

                RestoreDataOnlySelection(cmbDataOnlyDest, previous);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-DATAONLY-UI", "DATA ONLY destination 목록 갱신 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void AddDataOnlyCassetteDestItems(MaterialSnapshot snapshot, CassetteMaterialRole role)
        {
            if (role == CassetteMaterialRole.Input2 &&
                !IsSecondInputCassetteLevelEnabled())
            {
                return;
            }

            var cassette = snapshot != null && snapshot.Cassettes != null
                ? snapshot.Cassettes.FirstOrDefault(c => c != null && c.Role == role)
                : null;
            // 기존 조건: cassette.IsEnabled/IsPresent/IsMapped가 모두 참이어야 목록에 넣었다.
            //            → 미장착/미맵핑 카세트를 고르면 Destination 목록이 통째로 비어 데이터 정렬이 불가능했다.
            // 현재 기준: DATA ONLY는 유저가 장비 실물에 데이터를 맞추는 도구이므로 카세트 활성 상태로 목록을 비우지 않는다.
            if (cassette == null || cassette.Slots == null)
                return;

            // 기존 조건: 빈 슬롯만 목록에 넣었다(slot.HasWafer 또는 WaferId가 있으면 skip).
            //            → 사용 중인 카세트인데도 데이터를 넘길 대상 슬롯이 화면에서 사라져 수동 복구가 불가능했다.
            // 현재 기준: 사용 중인 카세트의 슬롯은 전부 표시하고, 점유 여부를 라벨에 표시한다.
            //            실제 점유 충돌은 실행 시점에 MaterialStateService가 판정한다.
            for (int i = 0; i < cassette.Slots.Count; i++)
            {
                var slot = cassette.Slots[i];
                if (slot == null)
                    continue;

                bool occupied = slot.HasWafer || !string.IsNullOrWhiteSpace(slot.WaferId);
                string occupancyText = occupied
                    ? (!string.IsNullOrWhiteSpace(slot.WaferId) ? slot.WaferId : "HAS WAFER")
                    : "EMPTY";

                var location = DataOnlyLocation.Cassette(role, i);
                cmbDataOnlyDest.Items.Add(new DataOnlyLocationItem(location, location.DisplayText + "  [" + occupancyText + "]"));
            }
        }

        private bool IsSecondInputCassetteLevelEnabled()
        {
            var host = GetHost();
            InputCassetteUnit cassette =
                host != null && host.Machine != null
                    ? host.Machine.InputCassetteUnit
                    : null;
            return cassette != null &&
                   cassette.ResolveCassetteLevelCount() >= 2;
        }

        private bool TrySelectOriginalCassetteDestination(
            MaterialSnapshot snapshot,
            DataOnlyLocationItem sourceItem)
        {
            if (snapshot == null ||
                snapshot.Wafers == null ||
                sourceItem == null ||
                sourceItem.Location == null ||
                (sourceItem.Location.Kind != MaterialLocationKind.InputFeeder &&
                 sourceItem.Location.Kind != MaterialLocationKind.InputStage))
            {
                return false;
            }

            var wafers = snapshot.Wafers
                .Where(w => w != null &&
                            w.CurrentLocation != null &&
                            w.CurrentLocation.Kind == sourceItem.Location.Kind &&
                            WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty)
                .ToList();
            if (wafers.Count != 1)
                return false;

            WaferMaterial wafer = wafers[0];
            if ((wafer.SourceCassetteRole != CassetteMaterialRole.Input1 &&
                 wafer.SourceCassetteRole != CassetteMaterialRole.Input2) ||
                wafer.SourceSlotNumber < 0)
            {
                return false;
            }

            DataOnlyLocation preferred =
                DataOnlyLocation.Cassette(
                    wafer.SourceCassetteRole,
                    wafer.SourceSlotNumber);
            foreach (object candidate in cmbDataOnlyDest.Items)
            {
                var item = candidate as DataOnlyLocationItem;
                if (item != null && item.Location.IsSameAs(preferred))
                {
                    cmbDataOnlyDest.SelectedItem = candidate;
                    return true;
                }
            }

            return false;
        }

        private void AddDataOnlyStationDestItem(MaterialSnapshot snapshot, MaterialLocationKind kind)
        {
            // 기존 조건: 점유된 스테이션은 목록에서 제외했다(occupied면 return).
            //            → 피더/스테이지에 데이터가 남아 있으면 그쪽으로 정렬할 방법이 없었다.
            // 현재 기준: 점유 여부를 라벨에 표시하고 목록에는 항상 넣는다. 점유 시 실행 단계에서 교환된다.
            var location = DataOnlyLocation.Station(kind);
            string occupancyText = ResolveDataOnlyMaterialId(location);
            cmbDataOnlyDest.Items.Add(new DataOnlyLocationItem(
                location,
                location.DisplayText + "  [" + (occupancyText == "-" ? "EMPTY" : occupancyText) + "]"));
        }

        private static void RestoreDataOnlySelection(ComboBox combo, DataOnlyLocationItem previous)
        {
            if (previous == null)
                return;

            foreach (object candidate in combo.Items)
            {
                var item = candidate as DataOnlyLocationItem;
                if (item != null && item.Location.IsSameAs(previous.Location))
                {
                    combo.SelectedItem = candidate;
                    return;
                }
            }
        }

        private static string ResolveDataOnlyMaterialId(DataOnlyLocation location)
        {
            var snapshot = MaterialStorage.State;
            if (snapshot == null || location == null)
                return "-";

            if (location.IsCassette)
            {
                var cassette = snapshot.Cassettes != null
                    ? snapshot.Cassettes.FirstOrDefault(c => c != null && c.Role == location.CassetteRole)
                    : null;
                var slot = cassette != null && cassette.Slots != null &&
                           location.SlotIndex >= 0 && location.SlotIndex < cassette.Slots.Count
                    ? cassette.Slots[location.SlotIndex]
                    : null;
                return slot != null && !string.IsNullOrWhiteSpace(slot.WaferId) ? slot.WaferId : "-";
            }

            var wafers = snapshot.Wafers != null
                ? snapshot.Wafers.Where(w => w != null &&
                        w.CurrentLocation != null &&
                        w.CurrentLocation.Kind == location.Kind &&
                        WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty).ToList()
                : new System.Collections.Generic.List<WaferMaterial>();
            if (wafers.Count == 1)
                return wafers[0].WaferId;
            return wafers.Count == 0 ? "-" : "다중(" + wafers.Count + ")";
        }

        private static string ResolveDataOnlyMaterialStateText(string materialId)
        {
            MaterialSnapshot snapshot = MaterialStorage.State;
            WaferMaterial wafer = snapshot != null && snapshot.Wafers != null
                ? snapshot.Wafers.FirstOrDefault(w =>
                    w != null &&
                    string.Equals(w.WaferId, materialId, StringComparison.OrdinalIgnoreCase))
                : null;
            return wafer != null
                ? WaferMaterialStateText.ToDisplayName(WaferMaterialStateText.Normalize(wafer.State))
                : "-";
        }

        private void ExecuteDataOnlyMove()
        {
            if (_dataOnlyBusy)
                return;

            try
            {
                _dataOnlyBusy = true;

                var sourceItem = cmbDataOnlySource.SelectedItem as DataOnlyLocationItem;
                var destItem = cmbDataOnlyDest.SelectedItem as DataOnlyLocationItem;
                if (sourceItem == null || destItem == null)
                {
                    QMC.Common.MessageDialog.Show(this, Lang.T("message.dataOnly.selectEndpoints"),
                        Lang.T("message.title.dataOnly"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string expectedId = ResolveDataOnlyMaterialId(sourceItem.Location);

                // 현재 기준: Destination이 점유된 경우 그 자재가 Source 위치로 교환되므로 확인창에 명시한다.
                string destOccupantId = ResolveDataOnlyMaterialId(destItem.Location);
                string swapNotice = destOccupantId != "-" && !string.Equals(destOccupantId, expectedId, StringComparison.OrdinalIgnoreCase)
                    ? Lang.Format("message.dataOnly.swapNotice", destOccupantId, sourceItem.Location.DisplayText)
                    : "";

                string message =
                    Lang.Format("message.dataOnly.moveConfirm", sourceItem.Location.DisplayText, destItem.Location.DisplayText, expectedId, swapNotice);
                if (QMC.Common.MessageDialog.Show(this, message, Lang.T("message.title.dataOnly"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    WriteEvent("INPUT-CST-DATAONLY-CANCEL", "DATA ONLY move canceled by user. source=" +
                        sourceItem.Location.DisplayText + ", destination=" + destItem.Location.DisplayText +
                        ", material=" + expectedId);
                    return;
                }

                var result = MaterialStateService.MoveMaterialDataOnly(
                    sourceItem.Location,
                    destItem.Location,
                    expectedId == "-" ? "" : expectedId,
                    QMC.CDT_320.Ui.Security.UserSession.Name);

                if (result.Success)
                {
                    SyncInputRuntimeProjection();
                    RefreshDataOnlyAfterChange();
                    WriteEvent("INPUT-CST-DATAONLY-MOVE", "DATA ONLY move done. material=" + result.MaterialId +
                        ", source=" + result.SourceText + ", destination=" + result.DestinationText +
                        ", state=" + ResolveDataOnlyMaterialStateText(result.MaterialId) +
                        ", swapped=" + (string.IsNullOrWhiteSpace(result.SwappedMaterialId) ? "-" : result.SwappedMaterialId) +
                        ", swappedTo=" + (string.IsNullOrWhiteSpace(result.SwappedToText) ? "-" : result.SwappedToText) +
                        ", persisted=" + result.PersistenceSucceeded);

                    // 실장비에서는 메모리 이동이 성공해도 Snapshot 저장이 실패할 수 있다.
                    // 저장 실패를 완료로 표시하면 재기동 후 이전 위치가 복원되므로 Auto 시작 전 알람으로 차단한다.
                    if (!result.PersistenceSucceeded)
                    {
                        RaiseWarning("INPUT-CST-DATAONLY-SAVE-FAIL",
                            "DATA ONLY 위치 변경은 반영됐지만 Material Snapshot 저장에 실패했습니다. " +
                            "프로그램을 재시작하거나 Auto를 시작하지 말고 저장 경로를 확인하십시오. material=" +
                            result.MaterialId);
                        QMC.Common.MessageDialog.Show(this,
                            Lang.T("message.dataOnly.saveFailed"),
                            Lang.T("message.title.dataOnlySaveFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    QMC.Common.MessageDialog.Show(this,
                        Lang.Format("message.dataOnly.moveComplete", result.MaterialId, ResolveDataOnlyMaterialStateText(result.MaterialId), result.SourceText, result.DestinationText, (string.IsNullOrWhiteSpace(result.SwappedMaterialId)
                            ? ""
                            : Lang.Format("message.dataOnly.swapComplete", result.SwappedMaterialId, result.SwappedToText, ResolveDataOnlyMaterialStateText(result.SwappedMaterialId)))),
                        Lang.T("message.title.dataOnly"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    RaiseWarning("INPUT-CST-DATAONLY-MOVE-FAIL", "DATA ONLY move failed. code=" + result.FailureCode +
                        ", reason=" + result.FailureMessage);
                    QMC.Common.MessageDialog.Show(this,
                        Lang.Format("message.dataOnly.moveFailed", result.FailureMessage),
                        Lang.T("message.title.dataOnly"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-DATAONLY-MOVE-EX", "DATA ONLY move failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("message.title.dataOnly"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _dataOnlyBusy = false;
            }
        }

        private void ExecuteDataOnlyDelete()
        {
            if (_dataOnlyBusy)
                return;

            try
            {
                _dataOnlyBusy = true;

                var sourceItem = cmbDataOnlySource.SelectedItem as DataOnlyLocationItem;
                if (sourceItem == null)
                {
                    QMC.Common.MessageDialog.Show(this, Lang.T("message.dataOnly.selectDeleteSource"),
                        Lang.T("message.title.dataOnly"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string expectedId = ResolveDataOnlyMaterialId(sourceItem.Location);
                string message =
                    Lang.Format("message.dataOnly.deleteConfirm", sourceItem.Location.DisplayText, expectedId);
                if (QMC.Common.MessageDialog.Show(this, message, Lang.T("message.title.dataOnly"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    WriteEvent("INPUT-CST-DATAONLY-CANCEL", "DATA ONLY delete canceled by user. location=" +
                        sourceItem.Location.DisplayText + ", material=" + expectedId);
                    return;
                }

                var result = MaterialStateService.DeleteMaterialDataOnly(
                    sourceItem.Location,
                    expectedId == "-" ? "" : expectedId,
                    QMC.CDT_320.Ui.Security.UserSession.Name);

                if (result.Success)
                {
                    SyncInputRuntimeProjection();
                    WriteEvent("INPUT-CST-DATAONLY-DELETE", "DATA ONLY delete done. material=" + result.MaterialId +
                        ", location=" + result.SourceText + ", persisted=" + result.PersistenceSucceeded);
                    RefreshDataOnlyAfterChange();
                    QMC.Common.MessageDialog.Show(this,
                        Lang.Format("message.dataOnly.deleteComplete", result.MaterialId),
                        Lang.T("message.title.dataOnly"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    RaiseWarning("INPUT-CST-DATAONLY-DELETE-FAIL", "DATA ONLY delete failed. code=" + result.FailureCode +
                        ", reason=" + result.FailureMessage);
                    QMC.Common.MessageDialog.Show(this,
                        Lang.Format("message.dataOnly.deleteFailed", result.FailureMessage),
                        Lang.T("message.title.dataOnly"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-DATAONLY-DELETE-EX", "DATA ONLY delete failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("message.title.dataOnly"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _dataOnlyBusy = false;
            }
        }

        // 중앙 상태 기준으로 Input Feeder/Stage runtime projection(로컬 캐시)을 재동기화한다.
        private bool SyncInputRuntimeProjection()
        {
            try
            {
                var host = GetHost();
                if (host == null || host.Machine == null)
                    return false;

                var feeder = host.Machine.InputFeederUnit;
                if (feeder != null)
                {
                    var feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
                    if (feederWafer != null)
                        feeder.SetCurrentWaferMaterial(feederWafer);
                    else
                        feeder.ClearCurrentWaferMaterial();
                }

                var stage = host.Machine.InputStageUnit;
                if (stage != null)
                {
                    var stageWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                    if (stageWafer != null)
                        stage.SetCurrentWaferMaterial(stageWafer);
                    else
                        stage.ClearCurrentWaferMaterial();
                }

                SynchronizeInputCassetteSlotStates(host.Machine.InputCassetteUnit);
                return true;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-RUNTIME-SYNC", "Input runtime projection 동기화 실패: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private void RefreshDataOnlyAfterChange()
        {
            try
            {
                RebuildDataOnlySourceItems();
                RebuildDataOnlyDestItems(false);
                var item = cmbDataOnlySource.SelectedItem as DataOnlyLocationItem;
                lblDataOnlyMaterialValue.Text = item != null ? ResolveDataOnlyMaterialId(item.Location) : "-";
                RefreshSelectedMaterialDetail();
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-DATAONLY-REFRESH", "DATA ONLY 화면 갱신 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void RefreshFromMachine()
        {
            try
            {
                var host = GetHost();
                if (host == null || host.Machine == null)
                    return;

                var loader = host.Machine.InputCassetteUnit;
                var ctrl = host.Controller;
                int slotCount = loader.Config != null && loader.Config.SlotCount > 0 ? loader.Config.SlotCount : 0;

                if (_lifterPosLabel != null)
                {
                    _lifterPosLabel.Text = AxisUnitConverter.FormatDisplay(loader.InputLifterZ.ActualPosition, loader.InputLifterZ, "0.###", true);
                }

                if (dotCassetteCheck1 != null)
                    dotCassetteCheck1.IsOn = IsSelectedCassetteLevel(loader, 0);
                if (dotCassetteCheck2 != null)
                    dotCassetteCheck2.IsOn = IsSelectedCassetteLevel(loader, 1);

                UpdateCassetteLevelLabels(loader);

                // To do: [맵핑 재설계] 표시 폴백은 선택된 단의 슬라이스를 사용한다(flat 배치: 앞=2단, 뒤=1단).
                var map = loader.GetLevelWaferMapView(_selectedCassetteRole == CassetteMaterialRole.Input2 ? 2 : 1);
                int curSlot = ResolveDisplayedSlot(ctrl);
                if (lblSlotNoValue != null)
                    lblSlotNoValue.Text = curSlot >= 0 ? GetCassetteRoleDisplay(_selectedCassetteRole) + " / " + (curSlot + 1).ToString("00") : GetCassetteRoleDisplay(_selectedCassetteRole) + " / -";
                if (lblSlotStateValue != null)
                {
                    string waferId;
                    WaferMaterialState state;
                    bool hasWafer;
                    bool known;
                    ResolveDisplayedSlotState(_selectedCassetteRole, curSlot, map, out waferId, out state, out hasWafer, out known);
                    Color stateColor = known ? GetStateColor(state) : Color.White;   // 미지정 상태 값은 흰색
                    lblSlotStateValue.Text = known ? Lang.Display(BuildStateText(state, waferId, false)) : "-";
                    lblSlotStateValue.BackColor = stateColor;
                    lblSlotStateValue.ForeColor = stateColor == Color.Navy ? Color.White : Color.Black;
                }

                RefreshCassetteLevelViews(loader, map, curSlot, slotCount);
                RefreshSelectedMaterialDetail();
            }
            catch (Exception ex)
            {
                WriteWarning("INPUT-CST-REFRESH", "Refresh failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void UpdateCassetteLevelLabels(QMC.CDT320.InputCassetteUnit loader)
        {
            int level = GetSelectedCassetteLevel(loader);
            if (lblCassetteCheck1 != null)
            {
                lblCassetteCheck1.Text = "1단 사용";
                lblCassetteCheck1.BackColor = Color.White;
            }
            if (lblCassetteCheck2 != null)
            {
                bool useLevel2 = level >= 2;
                lblCassetteCheck2.Text = useLevel2 ? "2단 사용" : "2단 미사용";
                lblCassetteCheck2.BackColor = useLevel2 ? Color.White : Color.FromArgb(0xD0, 0xD0, 0xD0);
            }
        }

        private int ResolveDisplayedSlot(QMC.CDT320.MachineController ctrl)
        {
            try
            {
                if (_selectedMaterialSlot >= 0)
                    return _selectedMaterialSlot;
                return ctrl != null ? ctrl.CurrentInputSlot : -1;
            }
            catch
            {
                return -1;
            }
            finally
            {
            }
        }

        private void ResolveDisplayedSlotState(
            CassetteMaterialRole role,
            int curSlot,
            System.Collections.Generic.IReadOnlyList<bool> fallbackMap,
            out string waferId,
            out WaferMaterialState state,
            out bool hasWafer,
            out bool known)
        {
            waferId = "";
            state = WaferMaterialState.Empty;
            hasWafer = false;
            known = false;
            try
            {
                if (curSlot < 0)
                    return;

                if (TryGetSlotMaterial(role, curSlot, out waferId, out state, out hasWafer))
                {
                    known = true;
                    return;
                }

                hasWafer = fallbackMap != null && curSlot >= 0 && curSlot < fallbackMap.Count && fallbackMap[curSlot];
                known = hasWafer;
                state = hasWafer ? WaferMaterialState.Ready : WaferMaterialState.Empty;
                waferId = "";
            }
            catch (Exception ex)
            {
                WriteWarning("INPUT-CST-SLOT-STATE", "Slot state resolve failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void RefreshCassetteLevelViews(QMC.CDT320.InputCassetteUnit loader, System.Collections.Generic.IReadOnlyList<bool> map, int curSlot, int slotCount)
        {
            int levelCount = GetConfiguredCassetteLevelCount(loader);
            // To do: [맵핑 재설계] WaferMap flat 배치가 (앞=2단, 뒤=1단)로 바뀌어 레벨별 뷰 슬라이스를 유닛에서 받는다.
            var level1Items = BuildMaterialSlotItems(CassetteMaterialRole.Input1, slotCount, loader != null ? loader.GetLevelWaferMapView(1) : map);
            var level2Items = BuildMaterialSlotItems(CassetteMaterialRole.Input2, slotCount, levelCount >= 2 && loader != null ? loader.GetLevelWaferMapView(2) : null);

            ApplyCassetteLevelLayout(levelCount);
            UpdateCassetteLevelView(_cassetteSlotView, "INPUT CASSETTE 1단", true, slotCount, level1Items);
            UpdateCassetteLevelView(_cassetteSlotViewLevel2, "INPUT CASSETTE 2단", levelCount >= 2, slotCount, level2Items);
        }

        private static bool IsSelectedCassetteLevel(QMC.CDT320.InputCassetteUnit loader, int level)
        {
            if (level == 0)
                return true;
            return GetConfiguredCassetteLevelCount(loader) >= 2;
        }

        private static int GetSelectedCassetteLevel(QMC.CDT320.InputCassetteUnit loader)
        {
            return GetConfiguredCassetteLevelCount(loader);
        }

        private static int GetConfiguredCassetteLevelCount(QMC.CDT320.InputCassetteUnit loader)
        {
            int configured = loader != null && loader.Config != null ? loader.Config.SelectedCassetteLevel : 0;
            return configured >= 2 ? 2 : 1;
        }

        private void ApplyCassetteLevelLayout(int levelCount)
        {
            if (cassetteLevelLayout == null || cassetteLevelLayout.ColumnStyles.Count < 2)
                return;

            bool useLevel2 = levelCount >= 2;
            cassetteLevelLayout.ColumnStyles[0].SizeType = SizeType.Percent;
            cassetteLevelLayout.ColumnStyles[0].Width = useLevel2 ? 50F : 100F;
            cassetteLevelLayout.ColumnStyles[1].SizeType = useLevel2 ? SizeType.Percent : SizeType.Absolute;
            cassetteLevelLayout.ColumnStyles[1].Width = useLevel2 ? 50F : 0F;

            if (_cassetteSlotView != null)
                _cassetteSlotView.Visible = true;
            if (_cassetteSlotViewLevel2 != null)
                _cassetteSlotViewLevel2.Visible = useLevel2;
        }

        private static void UpdateCassetteLevelView(CassetteSlotView view, string title, bool active, int slotCount, System.Collections.Generic.IReadOnlyList<CassetteSlotDisplayItem> items)
        {
            if (view == null)
                return;

            view.Title = title;
            view.EmptyColor = CassetteSlotView.EmptyStateColor;
            view.Enabled = active;
            view.SetSlotCount(slotCount);
            view.UpdateMaterialSlots(active ? items : null);
        }

        private static System.Collections.Generic.IReadOnlyList<CassetteSlotDisplayItem> BuildMaterialSlotItems(
            CassetteMaterialRole role,
            int slotCount,
            System.Collections.Generic.IReadOnlyList<bool> fallbackMap)
        {
            var snapshot = MaterialStorage.State;
            var cassette = snapshot != null && snapshot.Cassettes != null
                ? snapshot.Cassettes.FirstOrDefault(c => c.Role == role)
                : null;
            bool mapped = cassette != null && cassette.IsMapped;

            var items = new CassetteSlotDisplayItem[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                bool fallbackHasWafer = fallbackMap != null && i < fallbackMap.Count && fallbackMap[i];
                // 웨이퍼 상태(READY 등)는 Material Data가 있을 때만 부여한다.
                // 센서 웨이퍼맵(fallbackMap)은 "슬롯을 표시할지" 판단(IsKnown)에만 쓰고 상태로 승격하지 않는다.
                // 맵핑 전/Material Data 삭제 후에는 준비된 자재가 없으므로 EMPTY로 보여야 한다.
                // (이전에는 센서만으로 READY로 칠해, DATA ALL CLEAR 후 IsMapped=false 조기 반환과 겹쳐
                //  "상태 READY + 웨이퍼 ID 공백"으로 표시됐다.)
                items[i] = new CassetteSlotDisplayItem
                {
                    IsKnown = mapped || fallbackHasWafer,
                    HasWafer = false,
                    State = WaferMaterialState.Empty
                };
            }

            if (cassette == null || !cassette.IsMapped)
                return items;

            // [P5 2026-08-24] LOT맵 캐시/BIN 합계 기반 슬롯 다이 수 표시는 삭제 — 맵 파일명=바코드(1:1)라
            // 바코드를 읽기 전에는 어느 슬롯이 어느 맵인지 알 수 없다. 실측 DieIds 수(맵 적용 후)만 표시한다.
            int count = Math.Min(slotCount, cassette.Slots != null ? cassette.Slots.Count : 0);
            for (int i = 0; i < count; i++)
            {
                var slot = cassette.Slots[i];
                var wafer = ResolveCassetteSlotWafer(snapshot, role, i, slot);

                WaferMaterialState state = wafer != null ? WaferMaterialStateText.Normalize(wafer.State) : WaferMaterialState.Empty;
                bool hasWafer = ((slot != null && slot.HasWafer) || IsWaferInInputTransferLocation(wafer)) && state != WaferMaterialState.Empty;
                items[i].HasWafer = hasWafer;
                items[i].WaferId = hasWafer && wafer != null ? wafer.WaferId : (hasWafer && slot != null ? slot.WaferId : "");
                items[i].State = hasWafer ? state : WaferMaterialState.Empty;
                items[i].DieCount = hasWafer && wafer != null && wafer.DieIds != null && wafer.DieIds.Count > 0
                    ? (int?)wafer.DieIds.Count
                    : null;
            }

            return items;
        }

        // [P5 2026-08-24] ResolveSlotDieCount/LogSlotDieCountFailureOnce 삭제 — LOT맵 캐시 기반
        // 슬롯 다이 수 표시가 "바코드=파일명" 전환으로 근거를 잃어 실측 DieIds 표시(인라인)로 대체됐다.

        private static WaferMaterial ResolveCassetteSlotWafer(MaterialSnapshot snapshot, CassetteMaterialRole role, int slotIndex, CassetteSlotMaterial slot)
        {
            if (snapshot == null || snapshot.Wafers == null || slotIndex < 0)
                return null;

            if (slot != null && !string.IsNullOrWhiteSpace(slot.WaferInstanceId))
            {
                return snapshot.Wafers.FirstOrDefault(w =>
                    w != null &&
                    string.Equals(
                        w.WaferInstanceId ?? "",
                        slot.WaferInstanceId,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        w.WaferId ?? "",
                        slot.WaferId ?? "",
                        StringComparison.OrdinalIgnoreCase));
            }

            if (slot != null && !string.IsNullOrWhiteSpace(slot.WaferId))
            {
                List<WaferMaterial> legacyCandidates = snapshot.Wafers
                    .Where(w =>
                        w != null &&
                        string.Equals(
                            w.WaferId ?? "",
                            slot.WaferId,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (legacyCandidates.Count == 1)
                    return legacyCandidates[0];
                return null;
            }

            return snapshot.Wafers.FirstOrDefault(w =>
                w != null &&
                w.SourceCassetteRole == role &&
                w.SourceSlotNumber == slotIndex &&
                WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty &&
                IsWaferInInputTransferLocation(w));
        }

        private static bool IsWaferInInputTransferLocation(WaferMaterial wafer)
        {
            if (wafer == null || wafer.CurrentLocation == null)
                return false;

            return wafer.CurrentLocation.Kind == MaterialLocationKind.InputFeeder ||
                   wafer.CurrentLocation.Kind == MaterialLocationKind.InputStage;
        }

        private static string BuildSlotOccupancyText(CassetteSlotMaterial slot, WaferMaterial wafer, bool mapped)
        {
            if (slot != null && slot.HasWafer)
                return "HAS WAFER";
            if (IsWaferInInputTransferLocation(wafer))
                return wafer.CurrentLocation.Kind.ToString();
            return mapped && slot != null ? "EMPTY" : "";
        }

        private static bool TryGetSlotMaterial(CassetteMaterialRole role, int slotIndex, out string waferId, out WaferMaterialState state, out bool hasWafer)
        {
            waferId = "";
            state = WaferMaterialState.Empty;
            hasWafer = false;
            try
            {
                var snapshot = MaterialStorage.State;
                var cassette = snapshot != null && snapshot.Cassettes != null
                    ? snapshot.Cassettes.FirstOrDefault(c => c.Role == role)
                    : null;
                var slot = cassette != null && cassette.Slots != null && slotIndex >= 0 && slotIndex < cassette.Slots.Count
                    ? cassette.Slots[slotIndex]
                    : null;
                if (slot == null)
                    return false;

                var wafer = ResolveCassetteSlotWafer(snapshot, role, slotIndex, slot);
                waferId = wafer != null ? wafer.WaferId : (slot.WaferId ?? "");
                // Material Data(wafer)가 없으면 상태는 EMPTY다. slot.HasWafer만으로 READY로 보이면
                // Data 삭제 후에도 준비된 것처럼 표시된다(물리 존재는 SLOT 항목의 HAS WAFER로 별도 표시).
                state = wafer != null ? WaferMaterialStateText.Normalize(wafer.State) : WaferMaterialState.Empty;
                hasWafer = (slot.HasWafer || IsWaferInInputTransferLocation(wafer)) && state != WaferMaterialState.Empty;
                return cassette != null && cassette.IsMapped;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static string GetCassetteRoleDisplay(CassetteMaterialRole role)
        {
            try
            {
                return role == CassetteMaterialRole.Input2 ? "2단" : "1단";
            }
            catch
            {
                return "1단";
            }
            finally
            {
            }
        }

        private static string BuildStateText(WaferMaterialState state, string waferId, bool includeWaferId)
        {
            try
            {
                WaferMaterialState normalized = WaferMaterialStateText.Normalize(state);
                string stateText = WaferMaterialStateText.ToDisplayName(normalized);
                if (!includeWaferId || normalized == WaferMaterialState.Empty || string.IsNullOrWhiteSpace(waferId))
                    return stateText;
                return stateText + " / " + waferId;
            }
            catch
            {
                return "EMPTY";
            }
            finally
            {
            }
        }

        private static string FormatCassettePosition(double position)
        {
            try
            {
                if (double.IsNaN(position))
                    return "";
                return position.ToString("0.###") + " mm";
            }
            catch
            {
                return "";
            }
            finally
            {
            }
        }

        private static Color GetStateColor(WaferMaterialState state)
        {
            try
            {
                switch (WaferMaterialStateText.Normalize(state))
                {
                    // READY 슬롯 색상
                    case WaferMaterialState.Ready:
                        return CassetteSlotView.ReadyStateColor;
                    // WORK READY 슬롯 색상
                    case WaferMaterialState.WorkReady:
                        return CassetteSlotView.WorkReadyStateColor;
                    // WORKING 슬롯 색상
                    case WaferMaterialState.Working:
                        return CassetteSlotView.WorkingStateColor;
                    // FINISH 슬롯 색상
                    case WaferMaterialState.Finish:
                        return CassetteSlotView.FinishStateColor;
                    default:
                        return CassetteSlotView.EmptyStateColor;
                }
            }
            catch
            {
                return CassetteSlotView.EmptyStateColor;
            }
            finally
            {
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
            }
            catch (Exception ex)
            {
                WriteWarning("INPUT-CST-DISPOSE", "Dispose failed: " + ex.Message);
            }
            finally
            {
                base.OnHandleDestroyed(e);
            }
        }

        private static void WriteEvent(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Event, "UI", code, message);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void WriteWarning(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Warning, "UI", code, message);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void WriteAlarm(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Alarm, "UI", code, message);
                AlarmManager.Raise(AlarmSeverity.Error, code, LogSource, message);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void RaiseWarning(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Warning, "UI", code, message);
                AlarmManager.Raise(AlarmSeverity.Error, code, LogSource, message);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
