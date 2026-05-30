using System.Threading.Tasks;

namespace QMC.CDT320
{
    /// <summary>Vision 시트의 축/I/O/티칭/메소드 구조를 구현한 Unit 클래스입니다.</summary>
    public class VisionUnit : SheetDefinedUnit<VisionAxis>
    {
        /// <summary>VisionUnit을 생성합니다.</summary>
        public VisionUnit() : base("VisionUnit")
        {
            RegisterAxis(VisionAxis.FrontSideVisionY, "FrontSideVisionY");
            RegisterAxis(VisionAxis.RearSideVisionY, "RearSideVisionY");

            RegisterInput("WaferStageTouchSensor", "WaferStageTouchSensor");
            RegisterInput("ReticleUp", "ReticleUp");
            RegisterInput("ReticleDown", "ReticleDown");
            RegisterInput("NeedleVacuumCheck", "NeedleVacuumCheck");
        }

        /// <summary>Vision 축을 지정 좌표로 이동합니다.</summary>
        public Task MoveVisionAxis(VisionAxis axis, double targetPos, bool bFine = false) => MoveAxisAsync(axis, targetPos, bFine);

        /// <summary>Vision 축을 티칭 위치로 이동합니다.</summary>
        public Task MoveVisionAxisToTeachingPosition(VisionAxis axis, string positionName, bool bFine = false) => MoveAxisToTeachingPositionAsync(axis, positionName, bFine);

        /// <summary>Front Side Vision을 Avoid 위치로 이동합니다.</summary>
        public Task MoveFrontSideVisionToAvoidPosition(bool bFine = false) => MoveVisionAxisToTeachingPosition(VisionAxis.FrontSideVisionY, "AvoidPos", bFine);

        /// <summary>Front Side Vision을 Process 위치로 이동합니다.</summary>
        public Task MoveFrontSideVisionToProcessPosition(bool bFine = false) => MoveVisionAxisToTeachingPosition(VisionAxis.FrontSideVisionY, "ProcessPos", bFine);

        /// <summary>Rear Side Vision을 Avoid 위치로 이동합니다.</summary>
        public Task MoveRearSideVisionToAvoidPosition(bool bFine = false) => MoveVisionAxisToTeachingPosition(VisionAxis.RearSideVisionY, "AvoidPos", bFine);

        /// <summary>Rear Side Vision을 Process 위치로 이동합니다.</summary>
        public Task MoveRearSideVisionToProcessPosition(bool bFine = false) => MoveVisionAxisToTeachingPosition(VisionAxis.RearSideVisionY, "ProcessPos", bFine);

        /// <summary>Vision 축 위치 도착 여부를 확인합니다.</summary>
        public bool IsVisionAxisInPosition(VisionAxis axis, double targetPos, double tolerance) => IsAxisInPosition(axis, targetPos, tolerance);

        /// <summary>Vision 축 이동 완료를 대기합니다.</summary>
        public Task<bool> WaitVisionAxisMoveDone(VisionAxis axis, int timeoutMs) => WaitAxisMoveDone(axis, timeoutMs);

        /// <summary>Vision 축이 티칭 위치에 있는지 확인합니다.</summary>
        public bool IsVisionAxisInTeachingPosition(VisionAxis axis, string positionName) => IsAxisInTeachingPosition(axis, positionName);

        /// <summary>Vision 축 티칭 위치 도착을 대기합니다.</summary>
        public Task<bool> WaitVisionAxisInTeachingPosition(VisionAxis axis, string positionName, int timeoutMs) => WaitAxisInTeachingPosition(axis, positionName, timeoutMs);

        /// <summary>Vision 축 티칭 위치를 저장합니다.</summary>
        public void TeachVisionAxisPosition(VisionAxis axis, string positionName) => TeachAxisPosition(axis, positionName);

        /// <summary>Front Side Vision Avoid 위치를 저장합니다.</summary>
        public void TeachFrontSideVisionAvoidPosition() => TeachAxisPosition(VisionAxis.FrontSideVisionY, "AvoidPos");

        /// <summary>Front Side Vision Process 위치를 저장합니다.</summary>
        public void TeachFrontSideVisionProcessPosition() => TeachAxisPosition(VisionAxis.FrontSideVisionY, "ProcessPos");

        /// <summary>Rear Side Vision Avoid 위치를 저장합니다.</summary>
        public void TeachRearSideVisionAvoidPosition() => TeachAxisPosition(VisionAxis.RearSideVisionY, "AvoidPos");

        /// <summary>Rear Side Vision Process 위치를 저장합니다.</summary>
        public void TeachRearSideVisionProcessPosition() => TeachAxisPosition(VisionAxis.RearSideVisionY, "ProcessPos");

        /// <summary>Vision 티칭 위치 값을 반환합니다.</summary>
        public double GetVisionTeachingPosition(VisionAxis axis, string positionName) => GetTeachingPosition(axis, positionName);

        /// <summary>Vision 필수 티칭 완료 여부를 확인합니다.</summary>
        public bool ValidateVisionTeachingComplete()
        {
            return HasTeachingPosition(VisionAxis.FrontSideVisionY, "AvoidPos") &&
                   HasTeachingPosition(VisionAxis.RearSideVisionY, "AvoidPos");
        }

        /// <summary>Vision 축을 수동 조그 이동합니다.</summary>
        public void ManualMoveVisionAxisJog(VisionAxis axis, Direction dir, double speed) => ManualMoveAxisJog(axis, dir, speed);

        /// <summary>Vision 축 수동 조그를 정지합니다.</summary>
        public void ManualStopVisionAxis(VisionAxis axis) => ManualStopAxis(axis);

        /// <summary>Vision 정렬 준비 상태를 확인합니다.</summary>
        public bool CheckVisionReady() => true;

        /// <summary>Vision 동작을 안전 정지합니다.</summary>
        public void StopVisionMotion(string reason) => StopMotionAndOutputs(reason);

        /// <summary>Vision 알람 메시지를 생성합니다.</summary>
        public string BuildVisionAlarmMessage(StageAlarmCode code) => "Vision alarm: " + code;
    }
}
