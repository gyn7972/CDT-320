using System.Collections.Generic;
using System.Threading.Tasks;

namespace QMC.CDT320
{
    /// <summary>WaferStage 시트의 축/I/O/티칭/메소드 구조를 구현한 Unit 클래스입니다.</summary>
    public class WaferStageUnit : SheetDefinedUnit<WaferStageAxis>
    {
        /// <summary>WaferStageUnit을 생성합니다.</summary>
        public WaferStageUnit() : base("WaferStageUnit")
        {
            RegisterAxis(WaferStageAxis.WaferY, "WaferStageY");
            RegisterAxis(WaferStageAxis.WaferT, "WaferStageT");
            RegisterAxis(WaferStageAxis.WaferExpandingZ, "WaferExpandingZ");
            RegisterAxis(WaferStageAxis.VisionX, "WaferVisionX");
            RegisterAxis(WaferStageAxis.NeedleX, "NeedleX");
            RegisterAxis(WaferStageAxis.NeedleZ, "NeedleZ");
            RegisterAxis(WaferStageAxis.EjectPinZ, "EjectPinZ");

            RegisterInput("WaferStageTouchSensor", "WaferStageTouchSensor");
            RegisterInput("ReticleUp", "ReticleUp");
            RegisterInput("ReticleDown", "ReticleDown");
            RegisterInput("ReticleFrontSideFw", "ReticleFrontSideFw");
            RegisterInput("ReticleFrontSideBw", "ReticleFrontSideBw");
            RegisterInput("ReticleRearSideFw", "ReticleRearSideFw");
            RegisterInput("ReticleRearSideBw", "ReticleRearSideBw");
            RegisterInput("NeedleVacuumCheck", "NeedleVacuumCheck");

            RegisterOutput("IonizerOn", "IonizerOn");
            RegisterOutput("ReticleUp", "ReticleUp");
            RegisterOutput("ReticleDown", "ReticleDown");
            RegisterOutput("ReticleFrontSideFw", "ReticleFrontSideFw");
            RegisterOutput("ReticleFrontSideBw", "ReticleFrontSideBw");
            RegisterOutput("ReticleRearSideFw", "ReticleRearSideFw");
            RegisterOutput("ReticleRearSideBw", "ReticleRearSideBw");
            RegisterOutput("NeedleVacuum", "NeedleVacuum");
            RegisterOutput("NeedleBlow", "NeedleBlow");
        }

        /// <summary>WaferStage 단일 축을 지정 좌표로 이동합니다.</summary>
        public Task MoveWaferStageAxis(WaferStageAxis axis, double targetPos, bool bFine = false) => MoveAxisAsync(axis, targetPos, bFine);

        /// <summary>WaferStage 복수 축을 지정 좌표로 이동합니다.</summary>
        public Task MoveWaferStageAxes(Dictionary<WaferStageAxis, double> targets, bool bFine = false) => MoveAxesAsync(targets, bFine);

        /// <summary>WaferStage 단일 축을 티칭 위치로 이동합니다.</summary>
        public Task MoveWaferStageAxisToTeachingPosition(WaferStageAxis axis, string positionName, bool bFine = false) => MoveAxisToTeachingPositionAsync(axis, positionName, bFine);

        /// <summary>WaferStage 전체 축을 Avoid 위치로 이동합니다.</summary>
        public Task MoveToWaferStageAvoidPosition(bool bFine = false) => MoveStageGroup("AvoidPos", bFine);

        /// <summary>WaferStage 전체 축을 Load 위치로 이동합니다.</summary>
        public Task MoveToWaferStageLoadPosition(bool bFine = false) => MoveStageGroup("LoadPos", bFine);

        /// <summary>WaferStage 전체 축을 Unload 위치로 이동합니다.</summary>
        public Task MoveToWaferStageUnloadPosition(bool bFine = false) => MoveStageGroup("UnloadPos", bFine);

        /// <summary>WaferStage 전체 축을 Process 위치로 이동합니다.</summary>
        public Task MoveToWaferStageProcessPosition(bool bFine = false) => MoveStageGroup("ProcessPos", bFine);

        /// <summary>WaferStage 전체 축을 안전 후퇴 위치로 이동합니다.</summary>
        public Task MoveToWaferStageSafeRetreatPosition(bool bFine = false) => MoveStageGroup("SafeRetreatPosition", bFine);

        /// <summary>WaferStage를 Die 위치로 이동합니다.</summary>
        public Task MoveToWaferStageDiePosition(int dieIndex, bool bFine = false) => MoveWaferStageAxisToTeachingPosition(WaferStageAxis.WaferY, "DiePos[" + dieIndex + "]", bFine);

        /// <summary>Map 좌표를 기준으로 WaferStage 위치로 이동합니다.</summary>
        public Task MoveToWaferStageMapPosition(int row, int col, double theta, bool bFine = false) => MoveWaferStageAxis(WaferStageAxis.WaferY, CalculateWaferStageDieTarget(0, row, col, theta), bFine);

        /// <summary>Needle을 Pick 위치로 이동합니다.</summary>
        public Task MoveNeedleToPickPosition(bool bFine = false) => MoveWaferStageAxisToTeachingPosition(WaferStageAxis.NeedleZ, "ProcessPos", bFine);

        /// <summary>EjectPin을 공정 위치로 이동합니다.</summary>
        public Task MoveEjectPinToProcessPosition(bool bFine = false) => MoveWaferStageAxisToTeachingPosition(WaferStageAxis.EjectPinZ, "ProcessPos", bFine);

        /// <summary>WaferStage 축이 지정 좌표에 있는지 확인합니다.</summary>
        public bool IsWaferStageAxisInPosition(WaferStageAxis axis, double targetPos, double tolerance) => IsAxisInPosition(axis, targetPos, tolerance);

        /// <summary>WaferStage 축 이동 완료를 대기합니다.</summary>
        public Task<bool> WaitWaferStageAxisMoveDone(WaferStageAxis axis, int timeoutMs) => WaitAxisMoveDone(axis, timeoutMs);

        /// <summary>WaferStage 복수 축 이동 완료를 대기합니다.</summary>
        public async Task<bool> WaitWaferStageAxesMoveDone(IEnumerable<WaferStageAxis> axes, int timeoutMs)
        {
            foreach (var axis in axes)
                if (!await WaitAxisMoveDone(axis, timeoutMs)) return false;
            return true;
        }

        /// <summary>WaferStage 축이 티칭 위치에 있는지 확인합니다.</summary>
        public bool IsWaferStageAxisInTeachingPosition(WaferStageAxis axis, string positionName) => IsAxisInTeachingPosition(axis, positionName);

        /// <summary>WaferStage 축이 티칭 위치에 도착할 때까지 대기합니다.</summary>
        public Task<bool> WaitWaferStageAxisInTeachingPosition(WaferStageAxis axis, string positionName, int timeoutMs) => WaitAxisInTeachingPosition(axis, positionName, timeoutMs);

        /// <summary>WaferStage가 Avoid 위치인지 확인합니다.</summary>
        public bool IsWaferStageInAvoidPosition() => IsStageGroupInPosition("AvoidPos");

        /// <summary>WaferStage가 Load 위치인지 확인합니다.</summary>
        public bool IsWaferStageInLoadPosition() => IsStageGroupInPosition("LoadPos");

        /// <summary>WaferStage가 Unload 위치인지 확인합니다.</summary>
        public bool IsWaferStageInUnloadPosition() => IsStageGroupInPosition("UnloadPos");

        /// <summary>WaferStage가 Process 위치인지 확인합니다.</summary>
        public bool IsWaferStageInProcessPosition() => IsStageGroupInPosition("ProcessPos");

        /// <summary>WaferStage가 Die 위치인지 확인합니다.</summary>
        public bool IsWaferStageInDiePosition(int dieIndex) => IsAxisInTeachingPosition(WaferStageAxis.WaferY, "DiePos[" + dieIndex + "]");

        /// <summary>WaferStage 축 티칭 위치를 저장합니다.</summary>
        public void TeachWaferStageAxisPosition(WaferStageAxis axis, string positionName) => TeachAxisPosition(axis, positionName);

        /// <summary>WaferStage 전체 Avoid 위치를 저장합니다.</summary>
        public void TeachWaferStageAvoidPositions() => TeachStageGroup("AvoidPos");

        /// <summary>WaferStage 전체 Load 위치를 저장합니다.</summary>
        public void TeachWaferStageLoadPositions() => TeachStageGroup("LoadPos");

        /// <summary>WaferStage 전체 Unload 위치를 저장합니다.</summary>
        public void TeachWaferStageUnloadPositions() => TeachStageGroup("UnloadPos");

        /// <summary>WaferStage 전체 Process 위치를 저장합니다.</summary>
        public void TeachWaferStageProcessPositions() => TeachStageGroup("ProcessPos");

        /// <summary>WaferStage 전체 SafeRetreat 위치를 저장합니다.</summary>
        public void TeachWaferStageSafeRetreatPositions() => TeachStageGroup("SafeRetreatPosition");

        /// <summary>WaferStage Die 위치를 저장합니다.</summary>
        public void TeachWaferStageDiePosition(int dieIndex) => TeachAxisPosition(WaferStageAxis.WaferY, "DiePos[" + dieIndex + "]");

        /// <summary>Needle Pick 위치를 저장합니다.</summary>
        public void TeachNeedlePickPosition() => TeachAxisPosition(WaferStageAxis.NeedleZ, "ProcessPos");

        /// <summary>EjectPin 공정 위치를 저장합니다.</summary>
        public void TeachEjectPinProcessPosition() => TeachAxisPosition(WaferStageAxis.EjectPinZ, "ProcessPos");

        /// <summary>WaferStage 티칭 위치 값을 반환합니다.</summary>
        public double GetWaferStageTeachingPosition(WaferStageAxis axis, string positionName) => GetTeachingPosition(axis, positionName);

        /// <summary>WaferStage 필수 티칭이 완료되었는지 확인합니다.</summary>
        public bool ValidateWaferStageTeachingComplete() => HasTeachingPosition(WaferStageAxis.WaferY, "AvoidPos");

        /// <summary>WaferStage Die 목표 좌표를 계산합니다.</summary>
        public double CalculateWaferStageDieTarget(int dieIndex, int row, int col, double theta) => GetTeachingPosition(WaferStageAxis.WaferY, "DiePos[" + dieIndex + "]") + row + col + theta;

        /// <summary>Ionizer 출력을 제어합니다.</summary>
        public void SetIonizerOn(bool on) => SetOutput("IonizerOn", on);

        /// <summary>Ionizer를 켭니다.</summary>
        public async Task IonizerOn(int delayMs = 0) { SetIonizerOn(true); if (delayMs > 0) await Task.Delay(delayMs); }

        /// <summary>Ionizer를 끕니다.</summary>
        public void IonizerOff() => SetIonizerOn(false);

        /// <summary>Reticle Up 출력을 제어합니다.</summary>
        public void SetReticleUpOutput(bool on) => SetOutput("ReticleUp", on);

        /// <summary>Reticle Down 출력을 제어합니다.</summary>
        public void SetReticleDownOutput(bool on) => SetOutput("ReticleDown", on);

        /// <summary>Reticle을 상승시킵니다.</summary>
        public async Task<bool> ReticleUp(int timeoutMs) { SetOutput("ReticleUp", true); SetOutput("ReticleDown", false); return await WaitInputState("ReticleUp", true, timeoutMs); }

        /// <summary>Reticle을 하강시킵니다.</summary>
        public async Task<bool> ReticleDown(int timeoutMs) { SetOutput("ReticleDown", true); SetOutput("ReticleUp", false); return await WaitInputState("ReticleDown", true, timeoutMs); }

        /// <summary>Needle Vacuum 출력을 제어합니다.</summary>
        public void SetNeedleVacuum(bool on) => SetOutput("NeedleVacuum", on);

        /// <summary>Needle Blow 출력을 제어합니다.</summary>
        public void SetNeedleBlow(bool on) => SetOutput("NeedleBlow", on);

        /// <summary>Needle Vacuum 상태를 확인합니다.</summary>
        public bool IsNeedleVacuum() => IsInputOn("NeedleVacuumCheck");

        /// <summary>WaferStage 축을 수동 조그 이동합니다.</summary>
        public void ManualMoveWaferStageAxisJog(WaferStageAxis axis, Direction dir, double speed) => ManualMoveAxisJog(axis, dir, speed);

        /// <summary>WaferStage 축 수동 이동을 정지합니다.</summary>
        public void ManualStopWaferStageAxis(WaferStageAxis axis) => ManualStopAxis(axis);

        /// <summary>WaferStage 이동 전 공통 상태를 확인합니다.</summary>
        public bool CheckWaferStageMoveReady() => true;

        /// <summary>WaferStage 동작을 안전 정지합니다.</summary>
        public void StopWaferStageMotionAndOutputs(string reason) => StopMotionAndOutputs(reason);

        /// <summary>WaferStage 알람 메시지를 생성합니다.</summary>
        public string BuildWaferStageAlarmMessage(StageAlarmCode code) => "WaferStage alarm: " + code;

        private Task MoveStageGroup(string positionName, bool bFine)
        {
            var targets = new Dictionary<WaferStageAxis, double>();
            foreach (WaferStageAxis axis in System.Enum.GetValues(typeof(WaferStageAxis)))
                targets[axis] = GetTeachingPosition(axis, positionName);
            return MoveAxesAsync(targets, bFine);
        }

        private void TeachStageGroup(string positionName)
        {
            foreach (WaferStageAxis axis in System.Enum.GetValues(typeof(WaferStageAxis)))
                TeachAxisPosition(axis, positionName);
        }

        private bool IsStageGroupInPosition(string positionName)
        {
            foreach (WaferStageAxis axis in System.Enum.GetValues(typeof(WaferStageAxis)))
                if (!IsAxisInTeachingPosition(axis, positionName)) return false;
            return true;
        }
    }
}
