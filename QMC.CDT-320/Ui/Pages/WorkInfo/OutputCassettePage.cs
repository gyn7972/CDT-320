using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Dialogs;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class OutputCassettePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private Timer _timer;
        private bool _manualSequenceRunning;
        private SequenceStartMode _manualSequenceStartMode = SequenceStartMode.Resume;
        private CassetteMaterialRole _selectedCassetteRole = CassetteMaterialRole.Good1;
        private int _selectedMaterialSlot = -1;
        private const string LogSource = "OUTPUT-CASSETTE-PAGE";

        public OutputCassettePage()
        {
            InitializeComponent();
            WireEvents();

            _timer = new Timer { Interval = 200 };
            _timer.Tick += (s, e) =>
            {
                if (!ShouldRefreshVisible(this))
                    return;

                RefreshData();
            };
            VisibleChanged += (s, e) => { if (Visible) _timer.Start(); else _timer.Stop(); };
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private Form1 GetHost() => FindForm() as Form1;

        private void WireEvents()
        {
            btnPrev.Click += async (s, e) => await RunMotionAction("LIFTER PREV", host => MoveSlotAsync(host, -1));
            btnNext.Click += async (s, e) => await RunMotionAction("LIFTER NEXT", host => MoveSlotAsync(host, 1));
            btnReady.Click += async (s, e) => await RunMotionAction("LIFTER READY", LifterReadyAsync);
            // To do: [존 분리 스캔] GOOD/NG 액션 버튼 분리. GOOD은 상단 1단/2단 선택을 따르고 NG는 항상 NG 존.
            btnMap.Click += async (s, e) => await RunSequenceAction("GOOD BIN MAPPING", host => MapAsync(host, ResolveGoodTargetCassette()));
            btnMapNg.Click += async (s, e) => await RunSequenceAction("NG BIN MAPPING", host => MapAsync(host, TargetCassette.Ng));
            btnLoad.Click += async (s, e) => await RunSequenceAction("GOOD BIN LOADING", host => LoadAsync(host, ResolveGoodTargetCassette()));
            btnLoadNg.Click += async (s, e) => await RunSequenceAction("NG BIN LOADING", host => LoadAsync(host, TargetCassette.Ng));
            btnUnload.Click += async (s, e) => await RunSequenceAction("GOOD BIN UNLOADING", host => UnloadAsync(host, ResolveGoodTargetCassette()));
            btnUnloadNg.Click += async (s, e) => await RunSequenceAction("NG BIN UNLOADING", host => UnloadAsync(host, TargetCassette.Ng));
            btnStop.Click += async (s, e) => await StopManualActionAsync();

            // 카세트 교체: 장비가 정지된 상태에서만 수행한다(사용자 확정 2026-07-26).
            // 교체 준비 -> (작업자가 물리 교체) -> 교체 완료(해당 side 데이터만 초기화)
            // -> 문 닫고 START를 누르면 Ready에서 매핑이 다시 수행된다. 여기서는 매핑하지 않는다.
            btnCstExchange.Click += async (s, e) =>
                await RunSequenceAction("GOOD CST EXCHANGE", host => PrepareCassetteExchangeAsync(host, BinSide.Good));
            btnCstExchangeNg.Click += async (s, e) =>
                await RunSequenceAction("NG CST EXCHANGE", host => PrepareCassetteExchangeAsync(host, BinSide.Ng));
            btnCstClear.Click += (s, e) => CompleteCassetteExchange(BinSide.Good);
            btnCstClearNg.Click += (s, e) => CompleteCassetteExchange(BinSide.Ng);

            _good1CassetteView.SlotSelected += (s, e) => SelectMaterialSlot(CassetteMaterialRole.Good1, e.SlotIndex);
            _good2CassetteView.SlotSelected += (s, e) => SelectMaterialSlot(CassetteMaterialRole.Good2, e.SlotIndex);
            _ngCassetteView.SlotSelected += (s, e) => SelectMaterialSlot(CassetteMaterialRole.Ng1, e.SlotIndex);
            _good1CassetteView.SlotMoveRequested += async (s, e) => await MoveSlotFromContextMenuAsync(CassetteMaterialRole.Good1, e.SlotIndex);
            _good2CassetteView.SlotMoveRequested += async (s, e) => await MoveSlotFromContextMenuAsync(CassetteMaterialRole.Good2, e.SlotIndex);
            _ngCassetteView.SlotMoveRequested += async (s, e) => await MoveSlotFromContextMenuAsync(CassetteMaterialRole.Ng1, e.SlotIndex);
            _good1CassetteView.SlotDoubleClicked += async (s, e) => await MoveSlotFromDoubleClickAsync(CassetteMaterialRole.Good1, e.SlotIndex);
            _good2CassetteView.SlotDoubleClicked += async (s, e) => await MoveSlotFromDoubleClickAsync(CassetteMaterialRole.Good2, e.SlotIndex);
            _ngCassetteView.SlotDoubleClicked += async (s, e) => await MoveSlotFromDoubleClickAsync(CassetteMaterialRole.Ng1, e.SlotIndex);
            cmbDataOnlySource.DropDown += (s, e) => RebuildDataOnlySourceItems();
            cmbDataOnlySource.SelectedIndexChanged += (s, e) => OnDataOnlySourceChanged();
            cmbDataOnlyDest.DropDown += (s, e) => RebuildDataOnlyDestItems();
            btnDataOnlyMove.Click += (s, e) => ExecuteDataOnlyMove();
            btnDataOnlyDelete.Click += (s, e) => ExecuteDataOnlyDelete();
            materialDetailView.EditRequested += MaterialDetailView_EditRequested;
            materialDetailView.CreateDataRequested += MaterialDetailView_CreateDataRequested;
            materialDetailView.ClearDataRequested += MaterialDetailView_ClearDataRequested;
            materialDetailView.ClearAllDataRequested += MaterialDetailView_ClearAllDataRequested;
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
                SetActionButtonsEnabled(false);
                actionScope = host.Controller.BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "OutputCassettePage:" + actionName);
                SequenceFailureStore.Clear();
                bool ok = await action(host);
                if (!ok)
                {
                    showFailure = true;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
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
                    WriteAlarm("OUTPUT-CST-MANUAL-CLEANUP", "Output Cassette 수동 시컨스 정리 중 오류: " + ex.Message);
                }
                finally
                {
                    _manualSequenceRunning = false;
                    try { SetActionButtonsEnabled(true); } catch (Exception ex) { WriteAlarm("OUTPUT-CST-BUTTON-RESTORE", "Output Cassette 버튼 복구 실패: " + ex.Message); }
                    try { RefreshData(); } catch (Exception ex) { WriteAlarm("OUTPUT-CST-REFRESH", "Output Cassette 화면 갱신 실패: " + ex.Message); }
                }
            }

            if (showFailure)
            {
                QMC.Common.MessageDialog.Show(
                    this,
                    SequenceFailureStore.BuildManualFailureMessage(actionName, actionName + " 실패\r\nAlarm/Event Log를 확인하세요."),
                    "Output Cassette",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(this, "Output Cassette error:\r\n" + exceptionMessage, "Output Cassette", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                actionScope = host.Controller.BeginManualActionScope(ManualMotionScopeKind.SpeedOnly, "OutputCassettePageMotion:" + actionName);
                SequenceFailureStore.Clear();
                WriteEvent("OUTPUT-CST-MOTION", actionName + " start");
                int result = await action(host);
                WriteEvent("OUTPUT-CST-MOTION", actionName + " result=" + result);
                if (result != 0)
                {
                    // 실패를 팝업만 띄우고 로그를 남기지 않으면 사후 추적이 불가능하다. 경고 로그를 함께 남긴다.
                    RaiseWarning("OUTPUT-CST-MOTION-FAIL", actionName + " result=" + result);
                    failureMessage = SequenceFailureStore.BuildManualFailureMessage(
                        actionName,
                        actionName + " failed. result=" + result + "\r\nAlarm/Event Log를 확인하세요.");
                    showFailure = true;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
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
                    WriteAlarm("OUTPUT-CST-MOTION-CLEANUP", "Output Cassette 모션 정리 중 오류: " + ex.Message);
                }
                finally
                {
                    _manualSequenceRunning = false;
                    try { SetActionButtonsEnabled(true); } catch (Exception ex) { WriteAlarm("OUTPUT-CST-MOTION-BUTTON-RESTORE", "Output Cassette 모션 버튼 복구 실패: " + ex.Message); }
                    try { RefreshData(); } catch (Exception ex) { WriteAlarm("OUTPUT-CST-MOTION-REFRESH", "Output Cassette 모션 화면 갱신 실패: " + ex.Message); }
                }
            }

            if (showFailure)
                QMC.Common.MessageDialog.Show(this, failureMessage, "Output Cassette", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(this, exceptionMessage, actionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool ConfirmAction(string actionName)
        {
            return QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Output Cassette", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            btnPrev.Enabled = enabled;
            btnNext.Enabled = enabled;
            btnReady.Enabled = enabled;
            btnMap.Enabled = enabled;
            btnMapNg.Enabled = enabled;
            btnLoad.Enabled = enabled;
            btnLoadNg.Enabled = enabled;
            btnUnload.Enabled = enabled;
            btnUnloadNg.Enabled = enabled;
            btnCstExchange.Enabled = enabled;
            btnCstExchangeNg.Enabled = enabled;
            btnCstClear.Enabled = enabled;
            btnCstClearNg.Enabled = enabled;
            btnStop.Enabled = true;
        }

        private bool CanChangeOutputCassetteData(Form1 host, string actionName)
        {
            if (host == null || host.Controller == null)
            {
                QMC.Common.MessageDialog.Show(this,
                    "장비 제어기를 확인할 수 없어 Output Cassette 데이터를 변경할 수 없습니다.",
                    "Cassette Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                actionName + " 작업을 수행할 수 없습니다.\r\n" +
                "Auto/Manual/초기화/Ready 시퀀스가 완전히 정지된 뒤 다시 시도하세요.\r\n" +
                "status=" + status +
                ", sequenceRunning=" + controller.IsSequenceRunning +
                ", manualBusy=" + controller.IsManualBusy +
                ", readyRunning=" + controller.IsReadySequenceRunning,
                "Cassette Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private bool CanPrepareOutputCassetteExchange(BinSide side)
        {
            MaterialLocationKind stageLocation = side == BinSide.Ng
                ? MaterialLocationKind.OutputStageNg
                : MaterialLocationKind.OutputStageGood;
            WaferMaterial stageWafer = MaterialStateService.GetWaferAtLocation(stageLocation);
            if (stageWafer == null)
                return true;

            string sideName = side == BinSide.Ng ? "NG" : "GOOD";
            QMC.Common.MessageDialog.Show(this,
                sideName + " OutputStage에 진행 중인 Bin이 있어 카세트 교체를 준비할 수 없습니다.\r\n" +
                "해당 Bin을 기존 카세트로 먼저 Unload한 뒤 다시 시도하세요.\r\n" +
                "wafer=" + stageWafer.WaferId,
                "Cassette Exchange", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private bool CanClearOutputCassetteSideData(BinSide side)
        {
            if (!CanPrepareOutputCassetteExchange(side))
                return false;

            WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            if (feederWafer == null)
                return true;

            QMC.Common.MessageDialog.Show(this,
                "OutputFeeder에 진행 중인 Bin이 있어 카세트 데이터를 초기화할 수 없습니다.\r\n" +
                "Bin을 원래 카세트로 반납하고 Feeder를 안전 복귀한 뒤 다시 시도하세요.\r\n" +
                "wafer=" + feederWafer.WaferId,
                "Cassette Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private bool CanClearOutputCassetteDetailData(params BinSide[] sides)
        {
            Form1 host = GetHost();
            if (host != null && host.Controller != null &&
                host.Controller.Status == EquipmentStatus.CycleStopped)
            {
                QMC.Common.MessageDialog.Show(this,
                    "Cycle Stop 재개 정보가 남아 있어 Slot/Data All Clear를 수행할 수 없습니다.\r\n" +
                    "일반 STOP으로 전환한 뒤 다시 시도하세요.",
                    "Cassette Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (sides == null)
                return true;

            foreach (BinSide side in sides)
            {
                if (!CanClearOutputCassetteSideData(side))
                    return false;
            }

            return true;
        }

        private async Task StopManualActionAsync()
        {
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null)
                    return;

                await host.Controller.StopAsync();
            }
            catch
            {
            }
            finally
            {
            }
        }

        // 기존 조건: READY는 서보 ON만 수행(이동 없음), 홈서치는 별도 INIT 버튼.
        // 현재 기준: INIT 버튼은 제거하고 READY가 리프터 Z를 티칭된 Ready(Avoid) 위치로 실제 이동시킨다.
        //           이동은 기존 유닛 경로(MoveToBinCassetteAvoidPosition)를 사용해 MotionGuard/돌출 감시를 그대로 거친다.
        private async Task<int> LifterReadyAsync(Form1 host)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.OutputCassetteUnit : null;
            if (cassette == null || cassette.OutputLifterZ == null)
                return -1;

            cassette.OutputLifterZ.ServoOn();
            // 서보 ON 상태 반영 전에 이동 명령을 내리면 "Servo is OFF"로 즉시 실패하므로 반영을 기다린다.
            if (!await WaitAxisServoOnAsync(cassette.OutputLifterZ, 2000))
            {
                RecordLifterReadyFailure(cassette.OutputLifterZ, "OutputLifterZ 서보 ON이 확인되지 않았습니다.");
                return -2;
            }

            int result = await cassette.MoveToBinCassetteAvoidPosition();
            if (result != 0)
            {
                string detail = !string.IsNullOrWhiteSpace(cassette.LastBinLifterMoveFailureMessage)
                    ? cassette.LastBinLifterMoveFailureMessage
                    : "Ready(Avoid) 위치 이동이 실패했습니다. result=" + result;
                RecordLifterReadyFailure(cassette.OutputLifterZ, detail);
            }

            return result;
        }

        // READY 실패 사유를 로그/알람과 실패 팝업(SequenceFailureStore)에 구체적으로 남긴다.
        private void RecordLifterReadyFailure(BaseAxis axis, string detail)
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

                RaiseWarning("OUTPUT-CST-READY-FAIL", message);
                SequenceFailureStore.Record(
                    "OutputCassettePage.Manual",
                    "LifterReady",
                    "LifterReadyAsync",
                    "OUTPUT-CST-READY-FAIL",
                    LogSource,
                    message);
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-CST-READY-LOG", "Lifter ready failure logging failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static async Task<bool> WaitAxisServoOnAsync(BaseAxis axis, int timeoutMs)
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

        // To do: [존 분리 스캔] GOOD/NG 액션 분리 - 각 버튼이 대상 존을 명시해 실행한다.
        //        GOOD 대상은 상단 1단/2단 선택(_selectedCassetteRole)을 따르고, NG 버튼은 항상 NG 존이다.
        private TargetCassette ResolveGoodTargetCassette()
        {
            return _selectedCassetteRole == CassetteMaterialRole.Good2 ? TargetCassette.Good2 : TargetCassette.Good1;
        }

        // To do: [NG 스킵] UseNgCassette=false면 NG 대상 수동 구동을 차단한다.
        //        (NG 맵핑을 실행하면 등록 경로가 Ng1.IsEnabled를 다시 켜서 파라미터와 상태가 어긋난다)
        private bool GuardNgCassetteUsage(Form1 host, TargetCassette target, string actionName)
        {
            if (target != TargetCassette.Ng)
                return true;

            var cassette = host != null && host.Machine != null ? host.Machine.OutputCassetteUnit : null;
            if (cassette == null || cassette.Config == null || cassette.Config.UseNgCassette)
                return true;

            EventLogger.Write(EventKind.Alarm, "QMC", "OUTPUT-CST-NG-DISABLED",
                actionName + " 차단: NG 카세트 미사용 설정입니다. 레시피 CONFIG의 USE NG CASSETTE를 켠 후 실행하세요.");
            return false;
        }

        private async Task<bool> MapAsync(Form1 host, TargetCassette target)
        {
            if (!GuardNgCassetteUsage(host, target, "NG BIN MAPPING"))
                return false;

            var sequence = CreateOutputCassetteSequence(host);
            return await sequence.RunMappingAsync(host.Controller.ManualOperationToken, BuildCassetteOptions(host, _manualSequenceStartMode, target)) == 0;
        }

        private async Task<bool> LoadAsync(Form1 host, TargetCassette target)
        {
            if (!GuardNgCassetteUsage(host, target, "NG BIN LOADING"))
                return false;

            var sequence = CreateOutputCassetteSequence(host);
            return await sequence.RunLoadingAsync(host.Controller.ManualOperationToken, BuildCassetteOptions(host, _manualSequenceStartMode, target)) == 0;
        }

        private async Task<bool> UnloadAsync(Form1 host, TargetCassette target)
        {
            if (!GuardNgCassetteUsage(host, target, "NG BIN UNLOADING"))
                return false;

            var sequence = CreateOutputCassetteSequence(host);
            return await sequence.RunUnloadingAsync(host.Controller.ManualOperationToken, BuildCassetteOptions(host, _manualSequenceStartMode, target)) == 0;
        }

        /// <summary>
        /// 카세트 교체 준비: 선택한 side의 피더/리프터를 교체 위치로 보낸다.
        /// 자동 운전 중에는 수행할 수 없다(교체 시 장비는 정지 상태여야 한다).
        /// 사람이 수동으로 교체할 수도 있으므로 이 준비 동작은 선택 사항이다.
        /// </summary>
        private async Task<bool> PrepareCassetteExchangeAsync(Form1 host, BinSide side)
        {
            if (host == null || host.Controller == null)
                return false;

            if (host.Controller.Status == EquipmentStatus.AutoRunning)
            {
                QMC.Common.MessageDialog.Show(this,
                    "자동 운전 중에는 카세트를 교체할 수 없습니다.\r\n정지 후 다시 시도하세요.",
                    "Cassette Exchange", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!CanPrepareOutputCassetteExchange(side))
                return false;

            string sideLabel = side == BinSide.Ng ? "NG" : "GOOD";
            if (!ConfirmMaterialDataAction(
                sideLabel + " 카세트 교체 위치로 이동합니다.\r\n" +
                "① 피더가 Bin을 물고 있으면 카세트로 먼저 반납\r\n" +
                "② 피더 언클램프/리프트 다운 후 Avoid 복귀\r\n" +
                "③ 카세트 리프터를 로딩 위치로 이동\r\n\r\n진행할까요?"))
                return false;

            QMC.Common.Log.Write("Main", "SYSTEM", "OutputCassetteExchange",
                sideLabel + " 카세트 교체 준비를 시작합니다. - Start");

            // ① 피더가 Bin을 물고 있으면 원래 슬롯으로 반납한다.
            if (!await ReturnFeederBinBeforeExchangeAsync(host, side).ConfigureAwait(true))
                return false;

            var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());

            // ② 피더 안전 복귀(언클램프 -> 리프트 다운 -> Avoid).
            //    카세트를 물리적으로 뽑으려면 피더가 비어 있고 경로에서 비켜나 있어야 한다.
            OutputFeederSequenceOptions feederOptions = OutputFeederSequenceOptions.Default();
            feederOptions.Side = side;
            feederOptions.CassetteRole = ResolveExchangeCassetteRole(side);
            feederOptions.RunMode = SequenceRunMode.Manual;
            feederOptions.StartMode = SequenceStartMode.Restart;

            int result = await new OutputFeederSequence(ctx)
                .RunRecoverAsync(host.Controller.ManualOperationToken, feederOptions)
                .ConfigureAwait(true);
            if (result != 0)
                return false;

            // ③ 카세트 리프터를 로딩 위치로 이동한다.
            //    교체 높이는 별도 티칭 없이 레시피 로딩 포지션을 사용한다(사용자 확정 2026-07-26).
            //    OutputCassetteLoadingSequence: 카세트 감지 -> 자재 확인 -> 피더 Avoid 확인 -> 로딩 위치 이동.
            OutputCassetteSequenceOptions cassetteOptions = OutputCassetteSequenceOptions.Default();
            cassetteOptions.TargetCassette = side == BinSide.Ng ? TargetCassette.Ng : TargetCassette.Good1;
            cassetteOptions.RunMode = SequenceRunMode.Manual;
            cassetteOptions.StartMode = SequenceStartMode.Restart;

            result = await new OutputCassetteSequence(ctx)
                .RunLoadingAsync(host.Controller.ManualOperationToken, cassetteOptions)
                .ConfigureAwait(true);
            if (result != 0)
                return false;

            QMC.Common.Log.Write("Main", "SYSTEM", "OutputCassetteExchange",
                sideLabel + " 카세트 교체 준비를 완료했습니다. - Ok");

            QMC.Common.MessageDialog.Show(this,
                sideLabel + " 카세트가 교체 위치(로딩 포지션)로 이동했습니다.\r\n\r\n" +
                "① 카세트를 교체하세요.\r\n" +
                "② 교체 후 [" + sideLabel + " CST CLEAR]로 데이터를 초기화하세요.\r\n" +
                "③ 문을 닫고 START를 누르면 매핑부터 다시 진행됩니다.",
                "Cassette Exchange", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        private static CassetteMaterialRole ResolveExchangeCassetteRole(BinSide side)
        {
            return side == BinSide.Ng ? CassetteMaterialRole.Ng1 : CassetteMaterialRole.Good1;
        }

        /// <summary>
        /// 카세트 교체 준비 ① 단계. 피더가 Bin을 물고 있으면 원래 슬롯으로 반납한다.
        /// 반대 side의 Bin을 물고 있으면 그쪽을 먼저 정리해야 하므로 중단한다.
        /// 수동 UnloadToCassette(OutputFeederPage)와 동일하게 Place/Stage Area를 점유한 상태로 실행한다.
        /// </summary>
        private async Task<bool> ReturnFeederBinBeforeExchangeAsync(Form1 host, BinSide side)
        {
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            if (wafer == null)
                return true;

            CassetteMaterialRole role = wafer.SourceCassetteRole;
            BinSide waferSide = role == CassetteMaterialRole.Ng1 ? BinSide.Ng : BinSide.Good;
            if (waferSide != side)
            {
                QMC.Common.MessageDialog.Show(this,
                    "피더가 " + (waferSide == BinSide.Ng ? "NG" : "GOOD") + " Bin을 물고 있습니다.\r\n" +
                    "해당 Bin을 먼저 반납한 뒤 " + (side == BinSide.Ng ? "NG" : "GOOD") + " 카세트를 교체하세요.",
                    "Cassette Exchange", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int slot = wafer.SourceSlotNumber >= 0 ? wafer.SourceSlotNumber : 0;
            OutputFeederSequenceOptions options = OutputFeederSequenceOptions.Default();
            options.Side = side;
            options.CassetteRole = role;
            options.SlotIndex = slot;
            options.ExpectedWaferId = wafer.WaferId ?? "";
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = SequenceStartMode.Restart;

            var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            SequenceResourceKind stageResource = side == BinSide.Ng
                ? SequenceResourceKind.OutputNgStageArea
                : SequenceResourceKind.OutputGoodStageArea;

            using (SequenceResourceLease placeLease = await context.Resources.AcquireAsync(
                SequenceResourceKind.OutputPlaceArea,
                "OutputCassettePage.CassetteExchange",
                30000,
                host.Controller.ManualOperationToken).ConfigureAwait(true))
            {
                if (placeLease == null)
                {
                    EventLogger.Write(EventKind.Alarm, "QMC", "OUT-CST-EXCHANGE-PLACE-RESOURCE",
                        "카세트 교체 준비 Output Place Area 점유 실패. side=" + side);
                    return false;
                }

                using (SequenceResourceLease stageLease = await context.Resources.AcquireAsync(
                    stageResource,
                    "OutputCassettePage.CassetteExchange:" + side,
                    30000,
                    host.Controller.ManualOperationToken).ConfigureAwait(true))
                {
                    if (stageLease == null)
                    {
                        EventLogger.Write(EventKind.Alarm, "QMC", "OUT-CST-EXCHANGE-STAGE-RESOURCE",
                            "카세트 교체 준비 대상 Stage Area 점유 실패. side=" + side + ", resource=" + stageResource);
                        return false;
                    }

                    return await new OutputFeederSequence(context)
                        .RunUnloadToCassetteWithHeldResourcesAsync(
                            host.Controller.ManualOperationToken,
                            options,
                            placeLease,
                            stageLease).ConfigureAwait(true) == 0;
                }
            }
        }

        /// <summary>
        /// 카세트 교체 완료: 선택한 side의 Material 데이터만 초기화한다(반대편은 유지).
        /// 매핑은 여기서 하지 않는다 — 문을 닫고 START를 누르면 Ready 과정에서 다시 수행된다.
        /// </summary>
        private void CompleteCassetteExchange(BinSide side)
        {
            try
            {
                Form1 host = GetHost();
                if (!CanChangeOutputCassetteData(host, "Output Cassette " + side + " CLEAR"))
                    return;
                if (!CanClearOutputCassetteSideData(side))
                    return;

                string sideName = side == BinSide.Ng ? "NG" : "GOOD";
                string keepName = side == BinSide.Ng ? "GOOD" : "NG";
                if (!ConfirmMaterialDataAction(
                    sideName + " 카세트의 Material Data만 초기화합니다.\r\n" +
                    "(" + keepName + " 카세트 데이터는 유지됩니다)\r\n\r\n진행할까요?"))
                    return;
                if (!CanChangeOutputCassetteData(host, "Output Cassette " + sideName + " CLEAR"))
                    return;
                if (!CanClearOutputCassetteSideData(side))
                    return;

                if (!MaterialStateService.ClearOutputCassetteSideData(side))
                {
                    QMC.Common.MessageDialog.Show(this,
                        sideName + " 카세트 Material Data 초기화에 실패했습니다.",
                        "Cassette Exchange", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // RecipeChange 등 더 강한 기존 전체 준비 요청의 사유를 Side Clear가 덮어쓰면
                // Auto 준비 정책이 약화될 수 있다. 기존 요청이 없을 때만 새 요청을 등록하며,
                // 실제 선택 대상은 위 Clear가 false로 만든 Side별 IsMapped 상태로 판정한다.
                if (!host.Controller.IsOutputFullPreparationRequested)
                {
                    host.Controller.RequestOutputFullPreparation(
                        "CassetteExchangeClear:" + sideName,
                        host.Controller.ActiveRecipeName ?? string.Empty);
                }

                RefreshData();
                QMC.Common.MessageDialog.Show(this,
                    sideName + " 카세트 Material Data를 초기화했습니다.\r\n\r\n" +
                    "문을 닫고 START를 누르면 매핑부터 다시 진행됩니다.",
                    "Cassette Exchange", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this,
                    "카세트 교체 완료 처리 실패:\r\n" + ex.Message,
                    "Cassette Exchange", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private OutputCassetteSequence CreateOutputCassetteSequence(Form1 host)
        {
            var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            return new OutputCassetteSequence(ctx);
        }

        private OutputCassetteSequenceOptions BuildCassetteOptions(Form1 host, SequenceStartMode startMode)
        {
            return BuildCassetteOptions(host, startMode, ResolveTargetCassette(_selectedCassetteRole));
        }

        // To do: [존 분리 스캔] 버튼별 대상 존을 명시 전달하는 옵션 빌더.
        private OutputCassetteSequenceOptions BuildCassetteOptions(Form1 host, SequenceStartMode startMode, TargetCassette target)
        {
            var options = OutputCassetteSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = startMode;
            options.MoveTimeoutMs = ResolveManualMoveTimeoutMs(host);
            options.FineMove = false;
            options.TargetCassette = target;
            options.SlotIndex = _selectedMaterialSlot >= 0 ? _selectedMaterialSlot : 0;
            options.GoodLevelCount = host != null && host.Machine != null && host.Machine.OutputCassetteUnit != null && host.Machine.OutputCassetteUnit.Config != null
                ? Math.Max(1, Math.Min(2, host.Machine.OutputCassetteUnit.Config.SelectedCassetteLevel))
                : 2;
            return options;
        }

        private async Task<int> MoveSlotAsync(Form1 host, int delta)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.OutputCassetteUnit : null;
            if (cassette == null || cassette.Config == null)
                return -1;

            int slotCount = cassette.Config.SlotCount;
            if (slotCount <= 0)
                return -1;

            int currentSlot = _selectedMaterialSlot >= 0 ? _selectedMaterialSlot : 0;
            int targetSlot = Math.Max(0, Math.Min(slotCount - 1, currentSlot + delta));
            if (targetSlot == currentSlot)
                return 0;

            // PREV/NEXT도 우클릭/더블클릭과 같은 검증·실행 경로(MoveSpecificSlotAsync)를 사용한다.
            return await MoveSpecificSlotAsync(host, _selectedCassetteRole, targetSlot);
        }

        private void SelectMaterialSlot(CassetteMaterialRole role, int slotIndex)
        {
            _selectedCassetteRole = role;
            _selectedMaterialSlot = slotIndex;
            RefreshSelectedMaterialDetail();
            RefreshSelectedSlotState();
        }

        private async Task MoveSlotFromContextMenuAsync(CassetteMaterialRole role, int slotIndex)
        {
            SelectMaterialSlot(role, slotIndex);
            string actionName = "LIFT BIN MOVE " + GetCassetteRoleDisplay(role) + " / " + (slotIndex + 1).ToString("00");
            await RunMotionAction(actionName, host => MoveSpecificSlotAsync(host, role, slotIndex));
        }

        private async Task<int> MoveSpecificSlotAsync(Form1 host, CassetteMaterialRole role, int slotIndex)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.OutputCassetteUnit : null;
            if (cassette == null || cassette.Config == null)
                return -1;

            if (slotIndex < 0 || slotIndex >= cassette.Config.SlotCount)
                return -1;

            // 이동 전에 중앙 Resolver로 목표 유효성(FirstSlot/Pitch/단조 증가/소프트리밋)을 검증한다.
            // 시퀀스도 동일한 중앙 계산기(CalculateBinCassetteSlotTargetPosition)로 목표를 계산한다.
            TargetCassette target = ResolveTargetCassette(role);
            var resolve = cassette.ResolveManualBinCassetteSlotTarget(target, slotIndex);
            if (!resolve.IsValid)
            {
                RaiseWarning("OUTPUT-CST-SLOT-TARGET", "Slot target resolve failed. role=" + role +
                    ", slot=" + (slotIndex + 1).ToString("00") + ". " + resolve.FailureReason);
                SequenceFailureStore.Record(
                    "OutputCassettePage.Manual",
                    "SlotTarget",
                    "ResolveManualBinCassetteSlotTarget",
                    "OUTPUT-CST-SLOT-TARGET",
                    LogSource,
                    resolve.FailureReason);
                return -1;
            }

            WriteEvent("OUTPUT-CST-SLOT-TARGET",
                "Slot move target resolved. role=" + resolve.RoleName +
                ", slot=" + resolve.SlotNumber.ToString("00") +
                ", targetSource=" + resolve.TargetSourceText +
                ", target=" + resolve.TargetPosition.ToString("0.###") +
                ", current=" + (cassette.OutputLifterZ != null ? cassette.OutputLifterZ.ActualPosition.ToString("0.###") : "-"));

            SelectMaterialSlot(role, slotIndex);
            var sequence = CreateOutputCassetteSequence(host);
            // 기존 조건: 수동 단발 이동도 Resume로 시작 — 직전 실행이 Alarm/CycleStop이면 저장된 중간 Step부터 재개되었다.
            // 현재 기준: 새 수동 단발 요청은 항상 처음부터(Restart) 시작한다. Auto/공정 Resume 정책은 변경하지 않는다.
            var options = BuildCassetteOptions(host, SequenceStartMode.Restart);
            options.TargetCassette = target;
            options.SlotIndex = slotIndex;
            int result = await sequence.RunMoveSlotAsync(host.Controller.ManualOperationToken, options);
            if (result != 0)
            {
                RecordSlotMoveFailure(cassette, resolve, result, "Move slot sequence failed.");
                return result;
            }

            // 시퀀스가 완료를 보장하지만, 수동 슬롯 이동은 최종 InPosition/tolerance를 한 번 더 확인한다.
            string arrivalReason;
            if (!VerifyOutputLifterZArrival(cassette, resolve.TargetPosition, out arrivalReason))
            {
                RecordSlotMoveFailure(cassette, resolve, -1, arrivalReason);
                return -1;
            }

            return 0;
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
                    WriteEvent("OUTPUT-CST-DBLCLK-BLOCK",
                        "Slot double-click move blocked. role=" + requestRole +
                        ", slot=" + (requestSlotIndex + 1).ToString("00") + ". " + blockReason);
                    QMC.Common.MessageDialog.Show(
                        this,
                        "선택한 Cassette Slot에 실제 위치가 일치하는 Material 데이터가 없습니다.\r\n" +
                        "표시된 Material이 Feeder/Stage로 이동했는지 확인하십시오.\r\n\r\n사유: " + blockReason,
                        "Output Cassette",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var cassette = host.Machine.OutputCassetteUnit;
                if (cassette == null)
                    return;

                var resolve = cassette.ResolveManualBinCassetteSlotTarget(ResolveTargetCassette(requestRole), requestSlotIndex);
                if (!resolve.IsValid)
                {
                    RaiseWarning("OUTPUT-CST-DBLCLK-TARGET", "Slot double-click target resolve failed. " + resolve.FailureReason);
                    QMC.Common.MessageDialog.Show(this, resolve.FailureReason, "Output Cassette",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 더블클릭 전용 확인창(정확히 1회). 아래 RunMotionAction은 confirm=false로 호출해 중복 확인창을 막는다.
                string message =
                    "[" + resolve.RoleName + " / SLOT " + resolve.SlotNumber.ToString("00") + "]\r\n" +
                    "Bin: " + waferId + "\r\n" +
                    "목표: " + resolve.TargetPosition.ToString("0.###") + " mm (" + resolve.TargetSourceText + ")\r\n\r\n" +
                    "Cassette Z축이 실제로 이동합니다.\r\n해당 위치로 이동하시겠습니까?";
                DialogResult answer = QMC.Common.MessageDialog.Show(this, message, "Output Cassette",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    // 사용자 취소는 인터락 차단과 구분해 기록한다. Motion 0건, Material 변경 0건.
                    WriteEvent("OUTPUT-CST-DBLCLK-CANCEL",
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
                WriteAlarm("OUTPUT-CST-DBLCLK", "Slot double-click move failed: " + ex.Message);
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

                var wafer = snapshot.Wafers != null
                    ? snapshot.Wafers.FirstOrDefault(w => w != null &&
                          string.Equals(w.WaferId, slot.WaferId, StringComparison.OrdinalIgnoreCase))
                    : null;
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
                if (location == null || location.Kind != MaterialLocationKind.OutputCassette)
                {
                    reason = "Material 현재 위치가 Output Cassette가 아닙니다. waferId=" + wafer.WaferId +
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
        private static bool VerifyOutputLifterZArrival(OutputCassetteUnit cassette, double targetPosition, out string reason)
        {
            reason = string.Empty;
            var axis = cassette != null ? cassette.OutputLifterZ : null;
            if (axis == null)
            {
                reason = "OutputLifterZ axis is null.";
                return false;
            }

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance >= 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;
            double error = Math.Abs(axis.ActualPosition - targetPosition);
            if (axis.IsServoOn && !axis.IsAlarm && !axis.IsMoving && error <= tolerance)
                return true;

            reason = "OutputLifterZ arrival verify failed." +
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
        private void RecordSlotMoveFailure(OutputCassetteUnit cassette, CassetteSlotTargetResolveResult resolve, int resultCode, string detail)
        {
            try
            {
                var axis = cassette != null ? cassette.OutputLifterZ : null;
                double tolerance = axis != null && axis.Config != null && axis.Config.InPositionTolerance >= 0.0
                    ? axis.Config.InPositionTolerance
                    : 0.05;
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

                RaiseWarning("OUTPUT-CST-SLOT-MOVE-FAIL", message);
                SequenceFailureStore.Record(
                    "OutputCassettePage.Manual",
                    "SlotMove",
                    "MoveSpecificSlotAsync",
                    "OUTPUT-CST-SLOT-MOVE-FAIL",
                    LogSource,
                    message);
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-CST-SLOT-MOVE-LOG", "Slot move failure logging failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void RefreshData()
        {
            var host = GetHost();
            if (host?.Machine == null) return;

            var outputCassette = host.Machine.OutputCassetteUnit;
            lblElevatorPos.Text = AxisUnitConverter.FormatDisplay(outputCassette.OutputLifterZ.ActualPosition, outputCassette.OutputLifterZ, "0.###", true);
            dotGood1Check.IsOn = outputCassette.GoodBin8CassetteCheck0.IsOn || outputCassette.GoodBin12CassetteCheck0.IsOn;
            dotGood2Check.IsOn = outputCassette.GoodBin8CassetteCheck1.IsOn || outputCassette.GoodBin12CassetteCheck1.IsOn;
            dotNgCheck.IsOn = outputCassette.NgBin8CassetteCheck0.IsOn || outputCassette.NgBin8CassetteCheck1.IsOn ||
                              outputCassette.NgBin12CassetteCheck0.IsOn || outputCassette.NgBin12CassetteCheck1.IsOn;

            var driver = host.CassetteDriver;
            int slotCount = outputCassette != null && outputCassette.Config != null && outputCassette.Config.SlotCount > 0
                ? outputCassette.Config.SlotCount
                : 0;

            UpdateMaterialView(_good1CassetteView, slotCount, CassetteMaterialRole.Good1, ResolveSlots(outputCassette, TargetCassette.Good1, driver != null ? driver.OutputGood1Slots : null));
            UpdateMaterialView(_good2CassetteView, slotCount, CassetteMaterialRole.Good2, ResolveSlots(outputCassette, TargetCassette.Good2, driver != null ? driver.OutputGood2Slots : null));
            UpdateMaterialView(_ngCassetteView, slotCount, CassetteMaterialRole.Ng1, ResolveSlots(outputCassette, TargetCassette.Ng, driver != null ? driver.OutputNgSlots : null));

            RefreshSelectedSlotState();
            RefreshSelectedMaterialDetail();
        }

        private void RefreshSelectedSlotState()
        {
            lblSlotNoValue.Text = _selectedMaterialSlot >= 0
                ? GetCassetteRoleDisplay(_selectedCassetteRole) + " / " + (_selectedMaterialSlot + 1).ToString("00")
                : GetCassetteRoleDisplay(_selectedCassetteRole) + " / -";

            var snapshot = MaterialStorage.State;
            var cassette = snapshot != null && snapshot.Cassettes != null
                ? snapshot.Cassettes.FirstOrDefault(c => c.Role == _selectedCassetteRole)
                : null;
            var slot = cassette != null && cassette.Slots != null && _selectedMaterialSlot >= 0 && _selectedMaterialSlot < cassette.Slots.Count
                ? cassette.Slots[_selectedMaterialSlot]
                : null;
            var wafer = ResolveCassetteSlotWafer(snapshot, _selectedCassetteRole, _selectedMaterialSlot, slot);
            WaferMaterialState state = wafer != null ? WaferMaterialStateText.Normalize(wafer.State) : WaferMaterialState.Empty;
            lblSlotStateValue.Text = wafer != null ? WaferMaterialStateText.ToDisplayName(state) : "-";
            lblSlotStateValue.BackColor = System.Drawing.Color.White;   // 값 라벨은 흰색
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
            var slot = cassette != null && cassette.Slots != null && _selectedMaterialSlot >= 0 && _selectedMaterialSlot < cassette.Slots.Count
                ? cassette.Slots[_selectedMaterialSlot]
                : null;
            var wafer = ResolveCassetteSlotWafer(snapshot, _selectedCassetteRole, _selectedMaterialSlot, slot);

            materialDetailView.SetRows("WAFER MATERIAL", BuildWaferMaterialRows(cassette, slot, wafer));
        }

        private IEnumerable<MaterialDetailRow> BuildWaferMaterialRows(CassetteMaterial cassette, CassetteSlotMaterial slot, WaferMaterial wafer)
        {
            // Input Cassette와 동일하게 Mapping 완료 상태에서 Wafer ID/Lot ID/State/TapeFrame Spec을 수정할 수 있다.
            bool mapped = cassette != null && cassette.IsMapped;

            return new[]
            {
                Row("Selected", GetCassetteRoleDisplay(_selectedCassetteRole) + " / SLOT " + (_selectedMaterialSlot + 1).ToString("00")),
                Row("Cassette ID", cassette != null ? cassette.CassetteId : ""),
                Row("Cassette Mapped", cassette != null && cassette.IsMapped ? "Y" : "N"),
                Row("Slot", BuildSlotOccupancyText(slot, wafer)),
                Row("Wafer ID", wafer != null ? wafer.WaferId : "", "WaferId", mapped),
                Row("Lot ID", wafer != null ? wafer.CassetteLotId : (cassette != null ? cassette.CassetteLotId : ""), "CassetteLotId", mapped),
                Row("State", wafer != null ? WaferMaterialStateText.ToDisplayName(wafer.State) : "", "State", mapped),
                Row("Location", wafer != null && wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : ""),
                Row("Cassette Role", wafer != null ? wafer.SourceCassetteRole.ToString() : ""),
                Row("Cassette Slot", wafer != null && wafer.SourceSlotNumber >= 0 ? (wafer.SourceSlotNumber + 1).ToString("00") : ""),
                Row("Cassette Position", wafer != null ? FormatCassettePosition(wafer.CurrentCassetteSlotPosition) : ""),
                Row("TapeFrame Spec", wafer != null ? wafer.TapeFrameSpecName : "", "TapeFrameSpecName", mapped),
                Row("Updated", wafer != null ? wafer.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") : "")
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
                    RaiseWarning("OUTPUT-CST-MATERIAL-EDIT", "Material edit requested before mapping.");
                    QMC.Common.MessageDialog.Show(this, "Mapping 완료된 Cassette Slot에서만 Material을 수정할 수 있습니다.", "Material", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string newValue;
                if (!TryEditMaterialValue(e.Row, out newValue))
                    return;

                bool ok = MaterialStateService.UpdateWaferFieldInMappedCassette(
                    _selectedCassetteRole,
                    _selectedMaterialSlot,
                    e.Row.Key,
                    newValue);

                WriteEvent("OUTPUT-CST-MATERIAL", e.Row.Key + " update result=" + ok);
                if (!ok)
                {
                    RaiseWarning("OUTPUT-CST-MATERIAL-FAIL", e.Row.Key + " update failed.");
                    QMC.Common.MessageDialog.Show(this, "Material 값을 변경하지 못했습니다.", "Material", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                RefreshSelectedMaterialDetail();
                RefreshData();
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-CST-MATERIAL-EX", "Material edit failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Material", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private bool TryEditMaterialValue(MaterialDetailRow row, out string value)
        {
            value = row != null ? row.Value : "";
            if (row == null)
                return false;

            if (row.Key == "State")
            {
                var states = WaferMaterialStateText.DisplayNames;
                using (var dialog = new EnumPickerDialog("Bin State", states, row.Value))
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

        private void MaterialDetailView_CreateDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (_selectedMaterialSlot < 0)
                    return;
                if (!ConfirmMaterialDataAction("선택한 Output Cassette Slot에 Material Data를 생성하시겠습니까?"))
                    return;

                string waferId = BuildGeneratedOutputWaferId(_selectedCassetteRole, _selectedMaterialSlot);
                MaterialStateService.PutWaferInCassette(waferId, _selectedCassetteRole, _selectedMaterialSlot, ResolveCassetteLotId(_selectedCassetteRole), double.NaN, WaferMaterialState.Ready);
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Material Data 생성 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (!CanChangeOutputCassetteData(GetHost(), "Output Cassette Slot CLEAR"))
                    return;
                BinSide selectedSide = _selectedCassetteRole == CassetteMaterialRole.Ng1
                    ? BinSide.Ng
                    : BinSide.Good;
                if (!CanClearOutputCassetteDetailData(selectedSide))
                    return;
                if (!ConfirmMaterialDataAction("선택한 Output Cassette Slot의 Material Data를 초기화하시겠습니까?"))
                    return;
                if (!CanChangeOutputCassetteData(GetHost(), "Output Cassette Slot CLEAR"))
                    return;
                if (!CanClearOutputCassetteDetailData(selectedSide))
                    return;

                if (!MaterialStateService.ClearOutputCassetteSlotData(_selectedCassetteRole, _selectedMaterialSlot))
                {
                    QMC.Common.MessageDialog.Show(this,
                        "선택한 Output Cassette Slot의 Material Data 초기화에 실패했습니다.",
                        "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Material Data 초기화 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void MaterialDetailView_ClearAllDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!CanChangeOutputCassetteData(GetHost(), "Output Cassette DATA ALL CLEAR"))
                    return;
                if (!CanClearOutputCassetteDetailData(BinSide.Good, BinSide.Ng))
                    return;
                if (!ConfirmMaterialDataAction("Output Cassette의 모든 Material Data를 초기화하시겠습니까?"))
                    return;
                if (!CanChangeOutputCassetteData(GetHost(), "Output Cassette DATA ALL CLEAR"))
                    return;
                if (!CanClearOutputCassetteDetailData(BinSide.Good, BinSide.Ng))
                    return;

                if (!MaterialStateService.ClearOutputCassetteAllSlotData())
                {
                    QMC.Common.MessageDialog.Show(this,
                        "Output Cassette의 모든 Material Data 초기화에 실패했습니다.",
                        "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Material Data 전체 초기화 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private bool ConfirmMaterialDataAction(string message)
        {
            return QMC.Common.MessageDialog.Show(this, message, "Material Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private static IReadOnlyList<bool> ResolveSlots(OutputCassetteUnit unit, TargetCassette cassette, bool[] fallback)
        {
            if (unit != null && unit.SlotMap != null)
            {
                bool[] map;
                if (unit.SlotMap.TryGetValue(cassette, out map) && map != null && map.Length > 0)
                    return map;
            }

            return fallback;
        }

        private static void UpdateMaterialView(CassetteSlotView view, int slotCount, CassetteMaterialRole role, IReadOnlyList<bool> fallbackMap)
        {
            if (view == null)
                return;

            if (slotCount <= 0 && fallbackMap != null)
                slotCount = fallbackMap.Count;

            view.SetSlotCount(slotCount);
            view.UpdateMaterialSlots(BuildMaterialSlotItems(role, slotCount, fallbackMap));
        }

        private static IReadOnlyList<CassetteSlotDisplayItem> BuildMaterialSlotItems(CassetteMaterialRole role, int slotCount, IReadOnlyList<bool> fallbackMap)
        {
            var items = new List<CassetteSlotDisplayItem>();
            var snapshot = MaterialStorage.State;
            var cassette = snapshot != null && snapshot.Cassettes != null
                ? snapshot.Cassettes.FirstOrDefault(c => c.Role == role)
                : null;

            if (cassette != null)
            {
                cassette.EnsureSlots();
                if (slotCount <= 0)
                    slotCount = cassette.Slots.Count;
            }

            for (int i = 0; i < slotCount; i++)
            {
                bool fallbackHasWafer = fallbackMap != null && i < fallbackMap.Count && fallbackMap[i];
                CassetteSlotMaterial slot = cassette != null && cassette.Slots != null && i < cassette.Slots.Count
                    ? cassette.Slots[i]
                    : null;
                WaferMaterial wafer = ResolveCassetteSlotWafer(snapshot, role, i, slot);
                WaferMaterialState state = wafer != null ? WaferMaterialStateText.Normalize(wafer.State) : WaferMaterialState.Empty;
                bool hasWafer = ((slot != null && slot.HasWafer) || IsWaferInOutputTransferLocation(wafer) || fallbackHasWafer) &&
                                state != WaferMaterialState.Empty;

                items.Add(new CassetteSlotDisplayItem
                {
                    IsKnown = cassette != null || fallbackMap != null,
                    HasWafer = hasWafer,
                    WaferId = hasWafer && wafer != null ? wafer.WaferId : "",
                    State = hasWafer ? (wafer != null ? state : WaferMaterialState.Ready) : WaferMaterialState.Empty
                });
            }

            return items;
        }

        private static WaferMaterial ResolveCassetteSlotWafer(MaterialSnapshot snapshot, CassetteMaterialRole role, int slotIndex, CassetteSlotMaterial slot)
        {
            if (snapshot == null || snapshot.Wafers == null || slotIndex < 0)
                return null;

            if (slot != null && !string.IsNullOrWhiteSpace(slot.WaferId))
            {
                WaferMaterial slotWafer = snapshot.Wafers.FirstOrDefault(w => string.Equals(w.WaferId, slot.WaferId, StringComparison.OrdinalIgnoreCase));
                if (slotWafer != null)
                    return slotWafer;
            }

            return snapshot.Wafers.FirstOrDefault(w =>
                w != null &&
                w.SourceCassetteRole == role &&
                w.SourceSlotNumber == slotIndex &&
                WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty &&
                IsWaferInOutputTransferLocation(w));
        }

        private static bool IsWaferInOutputTransferLocation(WaferMaterial wafer)
        {
            if (wafer == null || wafer.CurrentLocation == null)
                return false;

            return wafer.CurrentLocation.Kind == MaterialLocationKind.OutputFeeder ||
                   wafer.CurrentLocation.Kind == MaterialLocationKind.OutputStageGood ||
                   wafer.CurrentLocation.Kind == MaterialLocationKind.OutputStageNg;
        }

        private static TargetCassette ResolveTargetCassette(CassetteMaterialRole role)
        {
            if (role == CassetteMaterialRole.Good2)
                return TargetCassette.Good2;
            if (role == CassetteMaterialRole.Ng1)
                return TargetCassette.Ng;
            return TargetCassette.Good1;
        }

        private static string GetCassetteRoleDisplay(CassetteMaterialRole role)
        {
            if (role == CassetteMaterialRole.Good2)
                return "GOOD2";
            if (role == CassetteMaterialRole.Ng1)
                return "NG";
            return "GOOD1";
        }

        private static int ResolveManualMoveTimeoutMs(Form1 host)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.OutputCassetteUnit : null;
            int configured = cassette != null && cassette.OutputLifterZ != null && cassette.OutputLifterZ.Setup != null ? cassette.OutputLifterZ.Setup.MoveTimeoutMs : 0;
            return configured > 0 ? configured : 3000;
        }

        private static string BuildSlotOccupancyText(CassetteSlotMaterial slot, WaferMaterial wafer)
        {
            if (wafer != null)
                return WaferMaterialStateText.ToDisplayName(wafer.State);
            if (slot != null && slot.HasWafer)
                return "HAS WAFER";
            return "EMPTY";
        }

        private static string FormatCassettePosition(double value)
        {
            return double.IsNaN(value) ? "" : value.ToString("0.###") + " mm";
        }

        private static MaterialDetailRow Row(string name, string value)
        {
            return new MaterialDetailRow
            {
                Name = name,
                Value = string.IsNullOrWhiteSpace(value) ? "-" : value,
                Editable = false
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

        private static string BuildGeneratedOutputWaferId(CassetteMaterialRole role, int slot)
        {
            return role.ToString().ToUpperInvariant() + "-S" + (slot + 1).ToString("00");
        }

        private static string ResolveCassetteLotId(CassetteMaterialRole role)
        {
            var snapshot = MaterialStorage.State;
            var cassette = snapshot != null && snapshot.Cassettes != null ? snapshot.Cassettes.FirstOrDefault(c => c.Role == role) : null;
            return cassette != null ? cassette.CassetteLotId : "";
        }

        private static void WriteAlarm(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Alarm, "UI", code, message);
            }
            catch
            {
            }
            finally
            {
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
                AddDataOnlyCassetteSourceItems(snapshot, CassetteMaterialRole.Good1);
                AddDataOnlyCassetteSourceItems(snapshot, CassetteMaterialRole.Good2);
                AddDataOnlyCassetteSourceItems(snapshot, CassetteMaterialRole.Ng1);
                AddDataOnlyStationSourceItem(snapshot, MaterialLocationKind.OutputFeeder);
                AddDataOnlyStationSourceItem(snapshot, MaterialLocationKind.OutputStageGood);
                AddDataOnlyStationSourceItem(snapshot, MaterialLocationKind.OutputStageNg);

                RestoreDataOnlySelection(cmbDataOnlySource, previous);
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-CST-DATAONLY-UI", "DATA ONLY source 목록 갱신 실패: " + ex.Message);
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
                RebuildDataOnlyDestItems();
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-CST-DATAONLY-UI", "DATA ONLY 선택 갱신 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void RebuildDataOnlyDestItems()
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
                    case MaterialLocationKind.OutputCassette:
                        AddDataOnlyStationDestItem(snapshot, MaterialLocationKind.OutputFeeder);
                        break;
                    case MaterialLocationKind.OutputFeeder:
                        AddDataOnlyCassetteDestItems(snapshot, CassetteMaterialRole.Good1);
                        AddDataOnlyCassetteDestItems(snapshot, CassetteMaterialRole.Good2);
                        AddDataOnlyCassetteDestItems(snapshot, CassetteMaterialRole.Ng1);
                        AddDataOnlyStationDestItem(snapshot, MaterialLocationKind.OutputStageGood);
                        AddDataOnlyStationDestItem(snapshot, MaterialLocationKind.OutputStageNg);
                        break;
                    case MaterialLocationKind.OutputStageGood:
                    case MaterialLocationKind.OutputStageNg:
                        AddDataOnlyStationDestItem(snapshot, MaterialLocationKind.OutputFeeder);
                        break;
                }

                RestoreDataOnlySelection(cmbDataOnlyDest, previous);
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-CST-DATAONLY-UI", "DATA ONLY destination 목록 갱신 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void AddDataOnlyCassetteDestItems(MaterialSnapshot snapshot, CassetteMaterialRole role)
        {
            var cassette = snapshot != null && snapshot.Cassettes != null
                ? snapshot.Cassettes.FirstOrDefault(c => c != null && c.Role == role)
                : null;
            if (cassette == null || cassette.Slots == null)
                return;

            for (int i = 0; i < cassette.Slots.Count; i++)
            {
                var slot = cassette.Slots[i];
                if (slot == null || slot.HasWafer || !string.IsNullOrWhiteSpace(slot.WaferId))
                    continue;

                var location = DataOnlyLocation.Cassette(role, i);
                cmbDataOnlyDest.Items.Add(new DataOnlyLocationItem(location, location.DisplayText + "  [EMPTY]"));
            }
        }

        private void AddDataOnlyStationDestItem(MaterialSnapshot snapshot, MaterialLocationKind kind)
        {
            bool occupied = snapshot != null && snapshot.Wafers != null && snapshot.Wafers.Any(w =>
                w != null &&
                w.CurrentLocation != null &&
                w.CurrentLocation.Kind == kind &&
                WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty);
            if (occupied)
                return;

            var location = DataOnlyLocation.Station(kind);
            cmbDataOnlyDest.Items.Add(new DataOnlyLocationItem(location, location.DisplayText + "  [EMPTY]"));
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
                : new List<WaferMaterial>();
            if (wafers.Count == 1)
                return wafers[0].WaferId;
            return wafers.Count == 0 ? "-" : "다중(" + wafers.Count + ")";
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
                    QMC.Common.MessageDialog.Show(this, "Source와 Destination을 먼저 선택하십시오.",
                        "DATA ONLY", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string expectedId = ResolveDataOnlyMaterialId(sourceItem.Location);
                string message =
                    "[DATA ONLY 이동]\r\n" +
                    "Source: " + sourceItem.Location.DisplayText + "\r\n" +
                    "Destination: " + destItem.Location.DisplayText + "\r\n" +
                    "Material ID: " + expectedId + "\r\n\r\n" +
                    "실물 장비는 움직이지 않습니다 (NO MOTION).\r\n" +
                    "Material 데이터만 이동하시겠습니까?";
                if (QMC.Common.MessageDialog.Show(this, message, "DATA ONLY",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    WriteEvent("OUTPUT-CST-DATAONLY-CANCEL", "DATA ONLY move canceled by user. source=" +
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
                    SyncOutputRuntimeProjection();
                    WriteEvent("OUTPUT-CST-DATAONLY-MOVE", "DATA ONLY move done. material=" + result.MaterialId +
                        ", source=" + result.SourceText + ", destination=" + result.DestinationText +
                        ", persisted=" + result.PersistenceSucceeded);
                    RefreshDataOnlyAfterChange();
                    QMC.Common.MessageDialog.Show(this,
                        "Material 데이터 이동이 완료되었습니다 (NO MOTION).\r\n" +
                        "Material ID: " + result.MaterialId + "\r\n" +
                        result.SourceText + " → " + result.DestinationText,
                        "DATA ONLY", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    RaiseWarning("OUTPUT-CST-DATAONLY-MOVE-FAIL", "DATA ONLY move failed. code=" + result.FailureCode +
                        ", reason=" + result.FailureMessage);
                    QMC.Common.MessageDialog.Show(this,
                        "Material 데이터 이동에 실패했습니다.\r\n" + result.FailureMessage,
                        "DATA ONLY", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-CST-DATAONLY-MOVE-EX", "DATA ONLY move failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "DATA ONLY", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    QMC.Common.MessageDialog.Show(this, "삭제할 위치를 Source에서 먼저 선택하십시오.",
                        "DATA ONLY", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string expectedId = ResolveDataOnlyMaterialId(sourceItem.Location);
                string message =
                    "[DATA ONLY 삭제]\r\n" +
                    sourceItem.Location.DisplayText + "의 Material [" + expectedId + "] 데이터를 삭제하시겠습니까?\r\n\r\n" +
                    "실물 장비는 움직이지 않으며, 실물이 제거되는 것도 아닙니다.";
                if (QMC.Common.MessageDialog.Show(this, message, "DATA ONLY",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    WriteEvent("OUTPUT-CST-DATAONLY-CANCEL", "DATA ONLY delete canceled by user. location=" +
                        sourceItem.Location.DisplayText + ", material=" + expectedId);
                    return;
                }

                var result = MaterialStateService.DeleteMaterialDataOnly(
                    sourceItem.Location,
                    expectedId == "-" ? "" : expectedId,
                    QMC.CDT_320.Ui.Security.UserSession.Name);

                if (result.Success)
                {
                    SyncOutputRuntimeProjection();
                    WriteEvent("OUTPUT-CST-DATAONLY-DELETE", "DATA ONLY delete done. material=" + result.MaterialId +
                        ", location=" + result.SourceText + ", persisted=" + result.PersistenceSucceeded);
                    RefreshDataOnlyAfterChange();
                    QMC.Common.MessageDialog.Show(this,
                        "Material 데이터 삭제가 완료되었습니다 (NO MOTION).\r\nMaterial ID: " + result.MaterialId,
                        "DATA ONLY", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    RaiseWarning("OUTPUT-CST-DATAONLY-DELETE-FAIL", "DATA ONLY delete failed. code=" + result.FailureCode +
                        ", reason=" + result.FailureMessage);
                    QMC.Common.MessageDialog.Show(this,
                        "Material 데이터 삭제에 실패했습니다.\r\n" + result.FailureMessage,
                        "DATA ONLY", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-CST-DATAONLY-DELETE-EX", "DATA ONLY delete failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "DATA ONLY", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _dataOnlyBusy = false;
            }
        }

        private void SyncOutputRuntimeProjection()
        {
            try
            {
                var host = GetHost();
                if (host == null || host.Machine == null)
                    return;

                var feeder = host.Machine.OutputFeederUnit;
                if (feeder != null)
                {
                    WaferMaterial feederWafer =
                        MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                    feeder.UpdateFeederMaterialState(
                        feederWafer == null ? MaterialState.Empty : MaterialState.Occupied);
                }

                var cassette = host.Machine.OutputCassetteUnit;
                if (cassette == null || cassette.Config == null)
                    return;

                CassetteMaterialRole[] roles =
                {
                    CassetteMaterialRole.Good1,
                    CassetteMaterialRole.Good2,
                    CassetteMaterialRole.Ng1
                };

                foreach (CassetteMaterialRole role in roles)
                {
                    TargetCassette target = ResolveTargetCassette(role);
                    for (int slotIndex = 0; slotIndex < cassette.Config.SlotCount; slotIndex++)
                    {
                        WaferMaterial wafer = MaterialStateService.GetWaferInCassette(role, slotIndex);
                        cassette.UpdateCassetteSlotState(
                            target,
                            slotIndex,
                            wafer != null ? SlotPresence.Exist : SlotPresence.Empty,
                            ProcessState.Ready);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-CST-DATAONLY-SYNC",
                    "DATA ONLY projection 동기화 실패: " + ex.Message);
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
                RebuildDataOnlyDestItems();
                var item = cmbDataOnlySource.SelectedItem as DataOnlyLocationItem;
                lblDataOnlyMaterialValue.Text = item != null ? ResolveDataOnlyMaterialId(item.Location) : "-";
                RefreshSelectedMaterialDetail();
                RefreshData();
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-CST-DATAONLY-REFRESH", "DATA ONLY 화면 갱신 실패: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}
