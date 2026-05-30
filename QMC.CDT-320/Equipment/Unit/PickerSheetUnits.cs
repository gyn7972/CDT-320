using System.Collections.Generic;
using System.Threading.Tasks;

namespace QMC.CDT320
{
    /// <summary>PickerFront/PickerRear 시트 공통 구현입니다.</summary>
    public abstract class PickerSheetUnitBase : SheetDefinedUnit<PickerAxis>
    {
        private readonly string _side;

        /// <summary>Picker 시트 Unit을 생성합니다.</summary>
        protected PickerSheetUnitBase(string unitName, string side) : base(unitName)
        {
            _side = side;
            RegisterAxis(PickerAxis.PickerX, side + "PickerX");
            RegisterAxis(PickerAxis.PickerY, side + "PickerY");
            RegisterAxis(PickerAxis.PickerT0, side + "PickerT0");
            RegisterAxis(PickerAxis.PickerZ0, side + "PickerZ0");
            RegisterAxis(PickerAxis.PickerT1, side + "PickerT1");
            RegisterAxis(PickerAxis.PickerZ1, side + "PickerZ1");
            RegisterAxis(PickerAxis.PickerT2, side + "PickerT2");
            RegisterAxis(PickerAxis.PickerZ2, side + "PickerZ2");
            RegisterAxis(PickerAxis.PickerT3, side + "PickerT3");
            RegisterAxis(PickerAxis.PickerZ3, side + "PickerZ3");

            RegisterInput("CdaTankPressure", side + "PickerCdaTankPressureCheck");
            RegisterInput("VacuumTankPressure", side + "PickerVacuumTankPressureCheck");
            for (int i = 0; i < 8; i++)
            {
                RegisterInput("Flow" + i, side + "Picker" + i + "FlowCheck");
                RegisterOutput("Vacuum" + i, side + "Picker" + i + "Vacuum");
                RegisterOutput("Blow" + i, side + "Picker" + i + "Blow");
            }
        }

        /// <summary>Picker 단일 축을 지정 좌표로 이동합니다.</summary>
        public Task MovePickerAxis(PickerAxis axis, double targetPos, bool bFine = false) => MoveAxisAsync(axis, targetPos, bFine);

        /// <summary>Picker 복수 축을 지정 좌표로 이동합니다.</summary>
        public Task MovePickerAxes(Dictionary<PickerAxis, double> targets, bool bFine = false) => MoveAxesAsync(targets, bFine);

        /// <summary>Picker 단일 축을 티칭 위치로 이동합니다.</summary>
        public Task MovePickerAxisToTeachingPosition(PickerAxis axis, string positionName, bool bFine = false) => MoveAxisToTeachingPositionAsync(axis, positionName, bFine);

        /// <summary>Picker를 Avoid 위치로 이동합니다.</summary>
        public Task MoveToPickerAvoidPosition(bool bFine = false) => MovePickerGroup("AvoidPos", bFine);

        /// <summary>Picker를 Load 위치로 이동합니다.</summary>
        public Task MoveToPickerLoadPosition(bool bFine = false) => MovePickerGroup("LoadPos", bFine);

        /// <summary>Picker를 Unload 위치로 이동합니다.</summary>
        public Task MoveToPickerUnloadPosition(bool bFine = false) => MovePickerGroup("UnloadPos", bFine);

        /// <summary>Picker를 안전 후퇴 위치로 이동합니다.</summary>
        public Task MoveToPickerSafeRetreatPosition(bool bFine = false) => MovePickerGroup("SafeRetreatPosition", bFine);

        /// <summary>Picker를 Die Pick 위치로 이동합니다.</summary>
        public Task MoveToPickerDiePickPosition(int colletIndex, bool bFine = false) => MovePickerAxisToTeachingPosition(PickerAxis.PickerX, "DiePickPos[" + colletIndex + "]", bFine);

        /// <summary>Picker를 Die Process 위치로 이동합니다.</summary>
        public Task MoveToPickerDieProcessPosition(int colletIndex, bool bFine = false) => MovePickerAxisToTeachingPosition(PickerAxis.PickerX, "DieProcessPos[" + colletIndex + "]", bFine);

        /// <summary>Picker를 Die Place 위치로 이동합니다.</summary>
        public Task MoveToPickerDiePlacePosition(int colletIndex, bool bFine = false) => MovePickerAxisToTeachingPosition(PickerAxis.PickerX, "DiePlacePos[" + colletIndex + "]", bFine);

        /// <summary>Picker 축 위치 도착 여부를 확인합니다.</summary>
        public bool IsPickerAxisInPosition(PickerAxis axis, double targetPos, double tolerance) => IsAxisInPosition(axis, targetPos, tolerance);

        /// <summary>Picker 축 이동 완료를 대기합니다.</summary>
        public Task<bool> WaitPickerAxisMoveDone(PickerAxis axis, int timeoutMs) => WaitAxisMoveDone(axis, timeoutMs);

        /// <summary>Picker 축 티칭 위치 도착 여부를 확인합니다.</summary>
        public bool IsPickerAxisInTeachingPosition(PickerAxis axis, string positionName) => IsAxisInTeachingPosition(axis, positionName);

        /// <summary>Picker 축 티칭 위치 도착을 대기합니다.</summary>
        public Task<bool> WaitPickerAxisInTeachingPosition(PickerAxis axis, string positionName, int timeoutMs) => WaitAxisInTeachingPosition(axis, positionName, timeoutMs);

        /// <summary>Picker 축 티칭 위치를 저장합니다.</summary>
        public void TeachPickerAxisPosition(PickerAxis axis, string positionName) => TeachAxisPosition(axis, positionName);

        /// <summary>Picker Avoid 위치를 저장합니다.</summary>
        public void TeachPickerAvoidPositions() => TeachPickerGroup("AvoidPos");

        /// <summary>Picker Load 위치를 저장합니다.</summary>
        public void TeachPickerLoadPositions() => TeachPickerGroup("LoadPos");

        /// <summary>Picker Unload 위치를 저장합니다.</summary>
        public void TeachPickerUnloadPositions() => TeachPickerGroup("UnloadPos");

        /// <summary>Picker 안전 후퇴 위치를 저장합니다.</summary>
        public void TeachPickerSafeRetreatPositions() => TeachPickerGroup("SafeRetreatPosition");

        /// <summary>Picker Die Pick 위치를 저장합니다.</summary>
        public void TeachPickerDiePickPosition(int colletIndex) => TeachAxisPosition(PickerAxis.PickerX, "DiePickPos[" + colletIndex + "]");

        /// <summary>Picker Die Process 위치를 저장합니다.</summary>
        public void TeachPickerDieProcessPosition(int colletIndex) => TeachAxisPosition(PickerAxis.PickerX, "DieProcessPos[" + colletIndex + "]");

        /// <summary>Picker Die Place 위치를 저장합니다.</summary>
        public void TeachPickerDiePlacePosition(int colletIndex) => TeachAxisPosition(PickerAxis.PickerX, "DiePlacePos[" + colletIndex + "]");

        /// <summary>Picker Offset 위치를 저장합니다.</summary>
        public void TeachPickerOffsetPosition(PickerAxis axis) => TeachAxisPosition(axis, "OffsetPos");

        /// <summary>Picker 티칭 위치 값을 반환합니다.</summary>
        public double GetPickerTeachingPosition(PickerAxis axis, string positionName) => GetTeachingPosition(axis, positionName);

        /// <summary>Picker 필수 티칭 완료 여부를 확인합니다.</summary>
        public bool ValidatePickerTeachingComplete() => HasTeachingPosition(PickerAxis.PickerX, "AvoidPos");

        /// <summary>Picker Vacuum 출력을 제어합니다.</summary>
        public void SetPickerVacuum(int pickerNo, bool on) => SetOutput("Vacuum" + pickerNo, on);

        /// <summary>Picker Blow 출력을 제어합니다.</summary>
        public void SetPickerBlow(int pickerNo, bool on) => SetOutput("Blow" + pickerNo, on);

        /// <summary>Picker Vacuum을 켭니다.</summary>
        public void PickerVacuumOn(int pickerNo) => SetPickerVacuum(pickerNo, true);

        /// <summary>Picker Vacuum을 끕니다.</summary>
        public void PickerVacuumOff(int pickerNo) => SetPickerVacuum(pickerNo, false);

        /// <summary>Picker Blow를 켭니다.</summary>
        public async Task PickerBlowOn(int pickerNo, int timeoutMs = 0) { SetPickerBlow(pickerNo, true); if (timeoutMs > 0) await Task.Delay(timeoutMs); }

        /// <summary>Picker Blow를 끕니다.</summary>
        public void PickerBlowOff(int pickerNo) => SetPickerBlow(pickerNo, false);

        /// <summary>Picker Flow 감지 상태를 확인합니다.</summary>
        public bool IsPickerFlowDetected(int pickerNo, bool expected = true) => IsInputOn("Flow" + pickerNo) == expected;

        /// <summary>Picker CDA 압력 상태를 확인합니다.</summary>
        public bool IsPickerCdaPressureOk() => IsInputOn("CdaTankPressure");

        /// <summary>Picker Vacuum 압력 상태를 확인합니다.</summary>
        public bool IsPickerVacuumPressureOk() => IsInputOn("VacuumTankPressure");

        /// <summary>Picker Flow 상태를 대기합니다.</summary>
        public Task<bool> WaitPickerFlowState(int pickerNo, bool expected, int timeoutMs) => WaitInputState("Flow" + pickerNo, expected, timeoutMs);

        /// <summary>Picker 축을 수동 조그 이동합니다.</summary>
        public void ManualMovePickerAxisJog(PickerAxis axis, Direction dir, double speed) => ManualMoveAxisJog(axis, dir, speed);

        /// <summary>Picker 축 수동 조그를 정지합니다.</summary>
        public void ManualStopPickerAxis(PickerAxis axis) => ManualStopAxis(axis);

        /// <summary>Picker Pick 동작을 수행합니다.</summary>
        public async Task<bool> PickDie(int pickerNo, int timeoutMs, bool bFine = false)
        {
            await MoveToPickerDiePickPosition(pickerNo, bFine);
            PickerVacuumOn(pickerNo);
            return await WaitPickerFlowState(pickerNo, true, timeoutMs);
        }

        /// <summary>Picker Place 동작을 수행합니다.</summary>
        public async Task<bool> PlaceDie(int pickerNo, int timeoutMs, bool bFine = false)
        {
            await MoveToPickerDiePlacePosition(pickerNo, bFine);
            PickerVacuumOff(pickerNo);
            await PickerBlowOn(pickerNo, Recipe.BlowTimeMs);
            PickerBlowOff(pickerNo);
            return true;
        }

        /// <summary>Picker 이동 준비 상태를 확인합니다.</summary>
        public bool CheckPickerMoveReady() => IsPickerCdaPressureOk() || Config.IsSimulationMode;

        /// <summary>Picker 동작을 안전 정지합니다.</summary>
        public void StopPickerMotionAndOutputs(string reason) => StopMotionAndOutputs(reason);

        /// <summary>Picker 알람 메시지를 생성합니다.</summary>
        public string BuildPickerAlarmMessage(StageAlarmCode code) => _side + " picker alarm: " + code;

        private Task MovePickerGroup(string positionName, bool bFine)
        {
            var targets = new Dictionary<PickerAxis, double>();
            foreach (PickerAxis axis in System.Enum.GetValues(typeof(PickerAxis)))
                targets[axis] = GetTeachingPosition(axis, positionName);
            return MoveAxesAsync(targets, bFine);
        }

        private void TeachPickerGroup(string positionName)
        {
            foreach (PickerAxis axis in System.Enum.GetValues(typeof(PickerAxis)))
                TeachAxisPosition(axis, positionName);
        }
    }

    /// <summary>PickerFront 시트의 축/I/O/티칭/메소드 구조를 구현한 Unit 클래스입니다.</summary>
    public class PickerFrontUnit : PickerSheetUnitBase
    {
        /// <summary>PickerFrontUnit을 생성합니다.</summary>
        public PickerFrontUnit() : base("PickerFrontUnit", "Front") { }
    }

    /// <summary>PickerRear 시트의 축/I/O/티칭/메소드 구조를 구현한 Unit 클래스입니다.</summary>
    public class PickerRearUnit : PickerSheetUnitBase
    {
        /// <summary>PickerRearUnit을 생성합니다.</summary>
        public PickerRearUnit() : base("PickerRearUnit", "Rear") { }
    }
}
