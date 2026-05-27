using System.Threading.Tasks;
using QMC.Common.Motion;

using Alarms = QMC.Common.Alarms;
namespace QMC.CDT320.Ajin
{
    /// <summary>
    /// AJINEXTEK AXL 보드 기반 실축.
    /// BaseAxis 의 virtual 메서드를 override 하여 실제 AXM 함수를 호출.
    /// </summary>
    public class AjinAxis : BaseAxis
    {
        /// <summary>AXL 라이브러리의 축 번호 (0-based).</summary>
        public int AxisNo { get; }

        public AjinAxis(string name, int axisNo) : base(name)
        {
            AxisNo = axisNo;
            Config.IsSimulationMode = false;    // 실보드 모드
        }

        // ──────────────────────────────────────────
        //  서보 ON/OFF / 알람 리셋
        // ──────────────────────────────────────────

        public override void ServoOn()
        {
            if (!AjinSystem.IsOpen) return;
            if (IsAlarm) return;
            uint r = Axl.AxmSignalServoOn(AxisNo, AxtServo.ON);
            if (AxtReturn.IsSuccess(r)) IsServoOn = true;
        }

        public override void ServoOff()
        {
            Stop();
            if (!AjinSystem.IsOpen) return;
            Axl.AxmSignalServoOn(AxisNo, AxtServo.OFF);
            IsServoOn = false;
        }

        public override void ResetAlarm()
        {
            if (!AjinSystem.IsOpen) { base.ResetAlarm(); return; }
            Axl.AxmSignalServoAlarmReset(AxisNo, 1);
            // 실제 상태는 다음 UpdateStatus() 주기에서 읽힘
            IsAlarm = false;
            AlarmCode = 0;
        }

        // ──────────────────────────────────────────
        //  이동
        // ──────────────────────────────────────────

        public override async Task MoveAbsoluteAsync(double targetPos, double velocity = 0)
        {
            if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen) return;

            double vel = velocity > 0 ? velocity : Recipe.DefaultVelocity;
            CommandPosition = targetPos;
            CurrentVelocity = vel;
            IsMoving        = true;
            IsInPosition    = false;

            // 실 이동 명령
            Axl.AxmMoveStartPos(AxisNo, targetPos, vel, Recipe.Acceleration, Recipe.Deceleration);

            RaiseMoveStarted();

            // UpdateStatus() 가 IsMoving, IsInPosition, IsAlarm 을 갱신하는 동안 대기
            await WaitUntilMoveDone();
        }

        public override void Stop()
        {
            base.Stop();
            if (!AjinSystem.IsOpen) return;
            Axl.AxmMoveStop(AxisNo, Recipe.Deceleration);
        }

        public override void EStop()
        {
            base.EStop();
            if (!AjinSystem.IsOpen) return;
            Axl.AxmMoveEStop(AxisNo);
        }

        public override async Task HomeSearchAsync()
        {
            if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen) return;
            IsHomeDone   = false;
            IsMoving     = true;
            IsInPosition = false;

            Axl.AxmHomeSetStart(AxisNo);
            RaiseMoveStarted();

            // 홈 완료까지 상태 폴링 (UpdateStatus 가 AxmHomeGetResult 읽음)
            int guard = 0;
            while (!IsHomeDone && !IsAlarm)
            {
                await Task.Delay(20).ContinueWith(_ => { });
                if (++guard > 3000) break; // 60초 타임아웃
            }
            IsMoving     = false;
            IsInPosition = IsHomeDone;
            if (IsHomeDone) RaiseMoveCompleted();
        }

        public override void SetPosition(double newPosition)
        {
            base.SetPosition(newPosition);
            if (!AjinSystem.IsOpen) return;
            Axl.AxmStatusSetCmdPos(AxisNo, newPosition);
            Axl.AxmStatusSetActPos(AxisNo, newPosition);
        }

        public override void MoveJogContinuous(int direction, JogSpeedType speedType, double customVel = 0)
        {
            if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen) return;
            double vel = GetJogVelocity(speedType, customVel);
            CurrentVelocity = vel;
            IsMoving        = true;
            IsInPosition    = false;
            Axl.AxmMoveVel(AxisNo, direction * vel, Recipe.Acceleration, Recipe.Deceleration);
            RaiseMoveStarted();
        }

        public override void StopJog()
        {
            if (!AjinSystem.IsOpen) { base.StopJog(); return; }
            Axl.AxmMoveStop(AxisNo, Recipe.Deceleration);
            base.StopJog();
        }

        // ──────────────────────────────────────────
        //  10ms 폴링 — 실보드에서 상태 읽어 BaseAxis 필드 갱신
        // ──────────────────────────────────────────

        public override void UpdateStatus()
        {
            if (Config.IsSimulationMode) { base.UpdateStatus(); return; }
            if (!AjinSystem.IsOpen)        return;

            // 위치
            double cmd = 0, act = 0;
            Axl.AxmStatusGetCmdPos(AxisNo, ref cmd);
            Axl.AxmStatusGetActPos(AxisNo, ref act);
            CommandPosition = cmd;
            double prev = ActualPosition;
            ActualPosition = act;
            if (prev != act) RaisePositionChanged();

            // 이동/인포지션
            int mot = 0, inp = 0;
            Axl.AxmStatusReadInMotion(AxisNo, ref mot);
            Axl.AxmStatusReadInPos   (AxisNo, ref inp);
            bool wasMoving = IsMoving;
            IsMoving     = mot != 0;
            IsInPosition = inp != 0;
            if (wasMoving && !IsMoving && IsInPosition)
                RaiseMoveCompleted();

            // 알람
            int alr = 0;
            Axl.AxmStatusReadAlarm(AxisNo, ref alr);
            bool wasAlarm = IsAlarm;
            IsAlarm = alr != 0;
            if (IsAlarm)
            {
                uint code = 0;
                Axl.AxmStatusReadAlarmCode(AxisNo, ref code);
                AlarmCode = code;
                if (!wasAlarm)
                    Alarms.AlarmManager.Raise(
                        Alarms.AlarmSeverity.Error,
                        "AX-" + AxisNo, Name, "Servo alarm 0x" + code.ToString("X4"));
            }

            // 홈 완료
            int hr = 0;
            if (AxtReturn.IsSuccess(Axl.AxmHomeGetResult(AxisNo, ref hr)))
                IsHomeDone = hr == 1; // AXT_RT_SUCCESS == HOMESEARCH_DONE; 사용 환경에 따라 수정

            // 센서
            int pel = 0, mel = 0, org = 0;
            Axl.AxmSignalReadLimit      (AxisNo, ref pel, ref mel);
            Axl.AxmSignalReadOriginLevel(AxisNo, ref org);
            // OS-12 (Stage 60 cycle 4) — Limit switch 변화 감지 시 LIMIT-HIT Raise
            bool wasPel = Sensor_PEL, wasMel = Sensor_MEL;
            Sensor_PEL = pel != 0;
            Sensor_MEL = mel != 0;
            Sensor_ORG = org != 0;
            if ((Sensor_PEL && !wasPel) || (Sensor_MEL && !wasMel))
            {
                string side = Sensor_PEL ? "PEL(+)" : "MEL(-)";
                Alarms.AlarmManager.Raise(
                    Alarms.AlarmSeverity.Error,
                    "LIMIT-HIT", Name,
                    "Limit 센서 도달 [" + side + "] AxisNo=" + AxisNo);
            }

            // Servo-on 상태 동기화
            int svOn = 0;
            if (AxtReturn.IsSuccess(Axl.AxmSignalIsServoOn(AxisNo, ref svOn)))
                IsServoOn = svOn != 0;
        }

        private async Task WaitUntilMoveDone()
        {
            int guard = 0;
            while (IsMoving && !IsAlarm)
            {
                await Task.Delay(10).ContinueWith(_ => { });
                if (++guard > 6000) break; // 60초 타임아웃
            }
        }
    }
}
