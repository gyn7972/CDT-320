using System;
using System.Collections.Generic;
using System.Linq;
using QMC.Common;
using QMC.Common.IO;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT320.Materials
{
    public static partial class MaterialStateService
    {
        // 사용자 승인: 실제 다이를 보유한 Admin 확인 건만 생산용 FLOW ON으로 인정한다.
        // _stateSync로 Material과 함께 보호하며, Snapshot/Config/Recipe에는 저장하지 않는다.
        private static readonly Dictionary<int, PickerFlowRecoveryRequest> _pickerFlowRecoveries =
            new Dictionary<int, PickerFlowRecoveryRequest>();
        private static readonly HashSet<PickerProductFlowCheck> _pickerProductFlowChecks =
            new HashSet<PickerProductFlowCheck>();
        private static long _pickerFlowRecoveryEpoch;

        public sealed class PickerFlowRecoveryRequest
        {
            public MaterialLocationKind PickerLocation { get; internal set; }
            public int PickerNo { get; internal set; }
            public string DieId { get; internal set; }
            public string InputWaferInstanceId { get; internal set; }
            public string ApprovedBy { get; internal set; }
            internal MaterialSnapshot Snapshot;
            internal DieMaterial Die;
            internal MaterialLocation Location;
            internal CDT320_Machine Machine;
            internal BaseDigitalInput Input;
            internal long Epoch;
            internal bool Used;
        }

        // 픽업 FLOW 확인은 Material 전이보다 먼저 시작할 수 있다. 요청 당시 다이를 고정한다.
        // UI 승인이 먼저 끝나고 Place가 바로 완료되더라도, 이미 승인된 기존 대기만 성공으로 회수한다.
        // 이 증거는 해당 대기 객체에만 남고 다음 픽업/다른 다이의 신호로 사용되지 않는다.
        internal sealed class PickerProductFlowCheck : IDisposable
        {
            internal MaterialSnapshot Snapshot;
            internal DieMaterial Die;
            internal string DieId;
            internal string WaferInstanceId;
            internal MaterialLocationKind Location;
            internal int PickerNo;
            internal bool Approved;
            internal CDT320_Machine ApprovedMachine;
            internal BaseDigitalInput ApprovedInput;
            internal long Epoch;

            public void Dispose()
            {
                lock (_stateSync)
                    _pickerProductFlowChecks.Remove(this);
            }
        }

        public static bool TryPreparePickerFlowRecovery(
            CDT320_Machine machine, bool globalDryRun, MaterialLocationKind location, int pickerNo,
            out PickerFlowRecoveryRequest request, out string reason)
        {
            request = null;
            lock (_stateSync)
            {
                BaseDigitalInput input;
                if (!CheckPickerFlowRecoveryAdmission(machine, globalDryRun, location, pickerNo, out input, out reason))
                    return false;

                DieMaterial die = FindUniquePickerFlowDieNoLock(location, pickerNo);
                if (die == null || string.IsNullOrWhiteSpace(die.DieId))
                {
                    reason = "해당 픽커의 현재 Die Material을 하나로 확인할 수 없습니다.";
                    return false;
                }

                request = new PickerFlowRecoveryRequest
                {
                    PickerLocation = location,
                    PickerNo = pickerNo,
                    DieId = die.DieId,
                    InputWaferInstanceId = die.InputWaferInstanceId ?? "",
                    ApprovedBy = UserSession.Name,
                    Snapshot = State,
                    Die = die,
                    Location = die.CurrentLocation,
                    Machine = machine,
                    Input = input,
                    Epoch = _pickerFlowRecoveryEpoch
                };
                reason = "";
                return true;
            }
        }

        public static bool TryApprovePickerFlowRecovery(
            PickerFlowRecoveryRequest request, CDT320_Machine machine, bool globalDryRun, out string reason)
        {
            lock (_stateSync)
            {
                if (request == null || request.Used || request.Epoch != _pickerFlowRecoveryEpoch)
                {
                    reason = "만료되었거나 이미 사용한 FLOW 복구 요청입니다. 현재 상태를 다시 확인해 주세요.";
                    return false;
                }

                BaseDigitalInput input;
                if (!CheckPickerFlowRecoveryAdmission(machine, globalDryRun, request.PickerLocation,
                    request.PickerNo, out input, out reason))
                    return false;

                if (!string.Equals(request.ApprovedBy, UserSession.Name, StringComparison.Ordinal) ||
                    !ReferenceEquals(request.Machine, machine) || !ReferenceEquals(request.Input, input) ||
                    !IsPickerFlowRecoveryMaterialCurrentNoLock(request))
                {
                    reason = "확인창 표시 후 사용자 또는 픽커의 Die Material이 변경되었습니다. 다시 확인해 주세요.";
                    return false;
                }

                // 실제 흡착은 작업자가 확인했다는 전제의 1회성 승인이다. 센서/진공 출력은 쓰지 않는다.
                Log.Write(LogLevel.AboveNormal, "Main", "PickerFlowRecovery",
                    "현재 Die의 FLOW 강제 ON을 승인했습니다. " + DescribePickerFlowRecovery(request) +
                    ", 실제FLOW=OFF, 해제조건=해당DiePlace완료 - Ok");
                request.Used = true;
                _pickerFlowRecoveries[PickerFlowRecoveryKey(request.PickerLocation, request.PickerNo)] = request;
                foreach (PickerProductFlowCheck check in _pickerProductFlowChecks)
                {
                    if (check.Epoch == request.Epoch && ReferenceEquals(check.Snapshot, request.Snapshot) &&
                        ReferenceEquals(check.Die, request.Die) && check.Location == request.PickerLocation &&
                        check.PickerNo == request.PickerNo && SameFlowDieIdentity(check.DieId, check.WaferInstanceId, request.Die))
                    {
                        check.Approved = true;
                        check.ApprovedMachine = machine;
                        check.ApprovedInput = input;
                    }
                }
                reason = "";
                return true;
            }
        }

        public static bool IsPickerFlowRecoveryActive(
            CDT320_Machine machine, bool globalDryRun, MaterialLocationKind location, int pickerNo,
            string expectedDieId = null, DieMaterial expectedDie = null)
        {
            lock (_stateSync)
            {
                PickerFlowRecoveryRequest request;
                if (!_pickerFlowRecoveries.TryGetValue(PickerFlowRecoveryKey(location, pickerNo), out request))
                    return false;

                BaseDigitalInput input;
                if (!IsRealPickerFlowEquipment(machine, globalDryRun, location, pickerNo, out input) ||
                    !ReferenceEquals(request.Machine, machine) || !ReferenceEquals(request.Input, input) ||
                    !IsPickerFlowRecoveryMaterialCurrentNoLock(request))
                {
                    ClearPickerFlowRecoveryNoLock(location, pickerNo, "현재 Material 또는 실장비 모드 변경");
                    return false;
                }

                return (expectedDie == null || ReferenceEquals(expectedDie, request.Die)) &&
                    (expectedDieId == null || string.Equals(request.DieId, expectedDieId, StringComparison.OrdinalIgnoreCase));
            }
        }

        internal static PickerProductFlowCheck BeginPickerProductFlowCheck(
            MaterialLocationKind location, int pickerNo, string dieId)
        {
            lock (_stateSync)
            {
                DieMaterial die = string.IsNullOrWhiteSpace(dieId) || State == null || State.Dies == null
                    ? null : State.Dies.FirstOrDefault(d => d != null &&
                        string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                var check = new PickerProductFlowCheck
                {
                    Snapshot = State,
                    Die = die,
                    DieId = dieId,
                    WaferInstanceId = die != null ? die.InputWaferInstanceId ?? "" : "",
                    Location = location,
                    PickerNo = pickerNo,
                    Epoch = _pickerFlowRecoveryEpoch
                };
                _pickerProductFlowChecks.Add(check);
                return check;
            }
        }

        internal static bool IsPickerProductFlowCheckApproved(
            PickerProductFlowCheck check, CDT320_Machine machine, bool globalDryRun)
        {
            if (check == null || check.Die == null)
                return false;
            lock (_stateSync)
            {
                BaseDigitalInput input;
                if (!_pickerProductFlowChecks.Contains(check) || check.Epoch != _pickerFlowRecoveryEpoch ||
                    !ReferenceEquals(check.Snapshot, State) ||
                    !SameFlowDieIdentity(check.DieId, check.WaferInstanceId, check.Die) ||
                    !IsRealPickerFlowEquipment(machine, globalDryRun, check.Location, check.PickerNo, out input))
                    return false;

                if (check.Approved)
                    return ReferenceEquals(check.ApprovedMachine, machine) && ReferenceEquals(check.ApprovedInput, input);
                return IsPickerFlowRecoveryActive(
                    machine, globalDryRun, check.Location, check.PickerNo, check.DieId, check.Die);
            }
        }

        internal static bool TryPreparePickerForNewFlowCheck(
            MaterialLocationKind location, int pickerNo, out string reason)
        {
            lock (_stateSync)
            {
                if (State != null && State.Dies != null && State.Dies.Any(d =>
                    d != null && d.CurrentLocation != null &&
                    d.CurrentLocation.Kind == location && d.CurrentLocation.PickerNo == pickerNo))
                {
                    reason = "해당 픽커에 Die Material이 남아 있습니다. picker=" + location + "/P" + pickerNo;
                    return false;
                }
                ClearPickerFlowRecoveryNoLock(location, pickerNo, "다음 픽업 전");
                reason = "";
                return true;
            }
        }

        public static void ClearPickerFlowRecovery(MaterialLocationKind location, int pickerNo, string reason)
        {
            lock (_stateSync)
                ClearPickerFlowRecoveryNoLock(location, pickerNo, reason);
        }

        public static void ClearAllPickerFlowRecoveries(string reason)
        {
            lock (_stateSync)
            {
                _pickerFlowRecoveryEpoch++;
                foreach (PickerFlowRecoveryRequest request in _pickerFlowRecoveries.Values.ToArray())
                    ClearPickerFlowRecoveryNoLock(request.PickerLocation, request.PickerNo, reason);
            }
        }

        private static void PrunePickerFlowRecoveriesNoLock(string reason)
        {
            if (_pickerFlowRecoveries.Count == 0)
                return;
            foreach (PickerFlowRecoveryRequest request in _pickerFlowRecoveries.Values.ToArray())
            {
                if (!IsPickerFlowRecoveryMaterialCurrentNoLock(request))
                    ClearPickerFlowRecoveryNoLock(request.PickerLocation, request.PickerNo, reason);
            }
        }

        private static void ClearPickerFlowRecoveryNoLock(MaterialLocationKind location, int pickerNo, string reason)
        {
            int key = PickerFlowRecoveryKey(location, pickerNo);
            PickerFlowRecoveryRequest request;
            if (!_pickerFlowRecoveries.TryGetValue(key, out request))
                return;
            _pickerFlowRecoveries.Remove(key);
            Log.Write(LogLevel.AboveNormal, "Main", "PickerFlowRecovery",
                "현재 Die의 FLOW 강제 ON을 해제했습니다. " + DescribePickerFlowRecovery(request) +
                ", 사유=" + reason + " - Ok");
        }

        private static bool CheckPickerFlowRecoveryAdmission(
            CDT320_Machine machine, bool globalDryRun, MaterialLocationKind location, int pickerNo,
            out BaseDigitalInput input, out string reason)
        {
            input = null;
            if (!UserSession.Has(UserLevel.Admin))
            {
                reason = "FLOW 강제 ON은 Admin 권한에서만 사용할 수 있습니다.";
                return false;
            }
            if (!IsRealPickerFlowEquipment(machine, globalDryRun, location, pickerNo, out input))
            {
                reason = "FLOW 강제 ON은 실장비의 P1~P4 입력에서만 사용할 수 있습니다. Sim/DryRun은 기존 판정을 사용합니다.";
                return false;
            }
            if (input.IsOn)
            {
                reason = "실제 FLOW가 이미 ON입니다. 강제 ON이 필요하지 않습니다.";
                return false;
            }
            reason = "";
            return true;
        }

        private static bool IsRealPickerFlowEquipment(
            CDT320_Machine machine, bool globalDryRun, MaterialLocationKind location, int pickerNo,
            out BaseDigitalInput input)
        {
            input = null;
            AppSettings settings = AppSettingsStore.Current;
            if (machine == null || globalDryRun || settings == null || !settings.UseAjin ||
                settings.BypassHardware || settings.SimulationMode || settings.DryRunMode ||
                pickerNo < 1 || pickerNo > 4)
                return false;

            if (location == MaterialLocationKind.PickerFront)
            {
                var unit = machine.PickerFrontUnit;
                if (unit == null || unit.Config == null || unit.Setup == null ||
                    unit.Config.IsSimulationMode || unit.Setup.IsSimulationMode ||
                    unit.FlowChecks == null || unit.FlowChecks.Length < pickerNo)
                    return false;
                input = unit.FlowChecks[pickerNo - 1];
            }
            else if (location == MaterialLocationKind.PickerRear)
            {
                var unit = machine.PickerRearUnit;
                if (unit == null || unit.Config == null || unit.Setup == null ||
                    unit.Config.IsSimulationMode || unit.Setup.IsSimulationMode ||
                    unit.FlowChecks == null || unit.FlowChecks.Length < pickerNo)
                    return false;
                input = unit.FlowChecks[pickerNo - 1];
            }
            return input != null && input.Config != null &&
                !input.Config.IsSimulationMode && !input.Config.IgnoreWaits;
        }

        private static bool IsPickerFlowRecoveryMaterialCurrentNoLock(PickerFlowRecoveryRequest request)
        {
            return request.Epoch == _pickerFlowRecoveryEpoch && ReferenceEquals(request.Snapshot, State) &&
                ReferenceEquals(request.Die, FindUniquePickerFlowDieNoLock(request.PickerLocation, request.PickerNo)) &&
                ReferenceEquals(request.Location, request.Die.CurrentLocation) &&
                SameFlowDieIdentity(request.DieId, request.InputWaferInstanceId, request.Die);
        }

        private static DieMaterial FindUniquePickerFlowDieNoLock(MaterialLocationKind location, int pickerNo)
        {
            if (State == null || State.Dies == null)
                return null;
            DieMaterial found = null;
            foreach (DieMaterial die in State.Dies)
            {
                if (die == null || die.CurrentLocation == null ||
                    die.CurrentLocation.Kind != location || die.CurrentLocation.PickerNo != pickerNo)
                    continue;
                if (found != null)
                    return null;
                found = die;
            }
            return found;
        }

        private static bool SameFlowDieIdentity(string dieId, string waferInstanceId, DieMaterial die)
        {
            return die != null &&
                string.Equals(dieId, die.DieId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(waferInstanceId ?? "", die.InputWaferInstanceId ?? "", StringComparison.OrdinalIgnoreCase);
        }

        private static int PickerFlowRecoveryKey(MaterialLocationKind location, int pickerNo)
        {
            return ((int)location * 10) + pickerNo;
        }

        private static string DescribePickerFlowRecovery(PickerFlowRecoveryRequest request)
        {
            return "승인자=" + request.ApprovedBy + ", picker=" + request.PickerLocation + "/P" + request.PickerNo +
                ", die=" + request.DieId + ", waferInstance=" + request.InputWaferInstanceId;
        }
    }
}
