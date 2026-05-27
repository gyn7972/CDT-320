using System;
using System.Threading.Tasks;
using QMC.Common.Motion.Ajin;

namespace QMC.Common.Motion
{
    /// <summary>
    /// AJINEXTEK AXL 보드 기반 실축 구현체.<br/>
    /// <see cref="BaseAxis"/> 의 virtual 메서드를 override 하여
    /// <see cref="Ajin.AXM"/> 정적 래퍼 함수로 실 모션 보드에 명령을 내린다.<br/>
    /// <list type="bullet">
    ///   <item><description><see cref="AjinMotionSystem.IsOpen"/> = false 이면 자동으로
    ///     시뮬레이션 모드(<see cref="AxisConfig.IsSimulationMode"/> = true)로 폴백한다.</description></item>
    ///   <item><description>10ms 주기 <see cref="UpdateStatus"/> 에서 실 보드 상태(위치/이동/알람/센서)를
    ///     <see cref="BaseAxis"/> 의 protected 필드에 반영한다.</description></item>
    /// </list>
    /// </summary>
    public class AjinAxis : BaseAxis
    {
        // ────────────────────────────────────────────
        //  공개 프로퍼티
        // ────────────────────────────────────────────

        /// <summary>AXL 라이브러리의 축 번호 (0-based).</summary>
        public int AxisNo { get; }

        // ────────────────────────────────────────────
        //  생성자
        // ────────────────────────────────────────────

        /// <summary>
        /// <paramref name="axisNo"/> 번 실축을 초기화한다.<br/>
        /// <see cref="AjinMotionSystem.IsOpen"/> 이 false 이거나 축번호가 무효하면
        /// 시뮬레이션 모드로 동작한다.
        /// </summary>
        public AjinAxis(string name, int axisNo) : base(name)
        {
            AxisNo = axisNo;
            Setup.AxisNo = axisNo;
            // 실보드가 열려있고 축이 유효하면 실모드, 아니면 시뮬레이션 폴백
            Config.IsSimulationMode = !IsReal();
        }

        /// <summary>
        /// JSON 축 정의에서 읽은 Setup/Config/Recipe 로 실축을 초기화한다.<br/>
        /// Config 에서 시뮬레이션 모드를 강제했거나 보드/축 번호가 유효하지 않으면 시뮬레이션 모드로 폴백한다.
        /// </summary>
        public AjinAxis(string name, AxisSetup setup, AxisConfig config, AxisRecipe recipe) : base(name)
        {
            if (setup != null) AxisDataMapper.Copy(setup, Setup);
            if (config != null) AxisDataMapper.Copy(config, Config);
            if (recipe != null) AxisDataMapper.Copy(recipe, Recipe);

            AxisNo = Setup.AxisNo;
            Config.IsSimulationMode = Config.IsSimulationMode || !IsReal();
        }

        /// <summary>실보드 사용 가능 여부.</summary>
        private bool IsReal()
        {
            if (!AjinMotionSystem.IsOpen) return false;
            // 시스템에서 인식된 축 개수 범위 내인지로 유효성 판단
            return AxisNo >= 0 && AxisNo < AjinMotionSystem.AxisCount;
        }

        // ────────────────────────────────────────────
        //  §1. 서보 / 알람
        // ────────────────────────────────────────────

        public override void ServoOn()
        {
            if (Config.IsSimulationMode) { base.ServoOn(); return; }
            if (IsAlarm) return;
            if (AXM.SetAmpEnabled(AxisNo, true) == 0)
                IsServoOn = true;
        }

        public override void ServoOff()
        {
            Stop();
            if (Config.IsSimulationMode) { base.ServoOff(); return; }
            AXM.SetAmpEnabled(AxisNo, false);
            IsServoOn = false;
        }

        public override void ResetAlarm()
        {
            if (Config.IsSimulationMode) { base.ResetAlarm(); return; }
            AXM.AlarmReset(AxisNo, true);
            IsAlarm   = false;
            AlarmCode = 0;
        }

        // ────────────────────────────────────────────
        //  §2. 이동
        // ────────────────────────────────────────────

        public override async Task MoveAbsoluteAsync(double targetPos, double velocity = 0)
        {
            if (Config.IsSimulationMode) { await base.MoveAbsoluteAsync(targetPos, velocity); return; }
            if (!IsServoOn || IsAlarm) return;

            double vel      = velocity > 0 ? velocity : Recipe.DefaultVelocity;
            CommandPosition = targetPos;
            CurrentVelocity = vel;
            IsMoving        = true;
            IsInPosition    = false;
            _currentMode    = MotionMode.Absolute;

            int ret = AXM.MovePosition(AxisNo, targetPos, vel,
                                       Recipe.Acceleration, Recipe.Deceleration);
            if (ret != 0)
            {
                IsMoving  = false;
                IsAlarm   = true;
                AlarmCode = (uint)ret;
                return;
            }

            RaiseMoveStarted();
            await WaitUntilMoveDoneReal();
        }

        public override void Stop()
        {
            base.Stop();
            if (Config.IsSimulationMode) return;
            AXM.Stop(AxisNo, Recipe.Deceleration);
        }

        public override void EStop()
        {
            base.EStop();
            if (Config.IsSimulationMode) return;
            AXM.StopEmergency(AxisNo);
        }

        public override async Task HomeSearchAsync()
        {
            if (Config.IsSimulationMode) { await base.HomeSearchAsync(); return; }
            if (!IsServoOn || IsAlarm) return;

            _currentMode = MotionMode.Homing;
            IsHomeDone   = false;
            IsMoving     = true;
            IsInPosition = false;

            int ret = AXM.SetHomeStart(AxisNo);
            if (ret != 0)
            {
                IsMoving  = false;
                IsAlarm   = true;
                AlarmCode = (uint)ret;
                return;
            }

            RaiseMoveStarted();

            // 홈 완료까지 폴링 (UpdateStatus 가 IsHomeDone / IsAlarm 갱신)
            int guard = 0;
            int maxGuard = Math.Max(1, Setup.HomeTimeoutMs / 20);
            while (!IsHomeDone && !IsAlarm)
            {
                await Task.Delay(20).ContinueWith(_ => { });
                if (++guard > maxGuard) break;
            }

            IsMoving     = false;
            IsInPosition = IsHomeDone;
            _currentMode = MotionMode.None;
            if (IsHomeDone) RaiseMoveCompleted();
        }

        public override void SetPosition(double newPosition)
        {
            base.SetPosition(newPosition);
            if (Config.IsSimulationMode) return;
            AXM.SetCommandPosition(AxisNo, newPosition);
            AXM.SetActualPosition (AxisNo, newPosition);
        }

        public override void MoveJogContinuous(int direction, JogSpeedType speedType, double customVel = 0)
        {
            if (Config.IsSimulationMode)
            {
                base.MoveJogContinuous(direction, speedType, customVel);
                return;
            }
            if (!IsServoOn || IsAlarm) return;

            double vel      = GetJogVelocity(speedType, customVel);
            CurrentVelocity = vel;
            IsMoving        = true;
            IsInPosition    = false;
            _currentMode    = MotionMode.Jog;
            _jogDirection   = direction;

            AXM.MoveVelocity(AxisNo, direction * vel,
                             Recipe.Acceleration, Recipe.Deceleration);
            RaiseMoveStarted();
        }

        public override void StopJog()
        {
            if (Config.IsSimulationMode) { base.StopJog(); return; }
            AXM.Stop(AxisNo, Recipe.Deceleration);
            base.StopJog();
        }

        // ────────────────────────────────────────────
        //  §3. 10ms 폴링 ? 실 보드에서 상태 읽기
        // ────────────────────────────────────────────

        public override void UpdateStatus()
        {
            if (Config.IsSimulationMode) { base.UpdateStatus(); return; }
            if (!AjinMotionSystem.IsOpen) return;

            try
            {
                // 위치
                double cmd = 0, act = 0;
                AXM.GetCommandPosition(AxisNo, ref cmd);
                AXM.GetActualPosition (AxisNo, ref act);
                CommandPosition = cmd;
                double prev = ActualPosition;
                ActualPosition = act;
                if (prev != act) RaisePositionChanged();

                // 이동 / 인포지션 / 알람 → AxisState 로 일괄 조회
                var state = AxisState.Idle;
                AXM.GetAxisState(AxisNo, ref state);
                bool wasMoving = IsMoving;
                IsMoving = state == AxisState.Moving || state == AxisState.Stopping;

                bool faulted = false;
                AXM.GetAmpFaultValue(AxisNo, ref faulted);
                bool wasAlarm = IsAlarm;
                IsAlarm = faulted || state == AxisState.Error;
                if (IsAlarm && !wasAlarm)
                    AlarmCode = AlarmCode == 0 ? 1u : AlarmCode;

                // 인포지션: 보드 별 차이 → "이동 종료 && 비알람" 으로 근사
                IsInPosition = !IsMoving && !IsAlarm;
                if (wasMoving && !IsMoving && IsInPosition)
                    RaiseMoveCompleted();

                // 홈 완료
                var hr = AXT_MOTION_HOME_RESULT.HOME_SEARCHING;
                if (AXM.GetHomeResult(AxisNo, ref hr) == 0)
                    IsHomeDone = hr == AXT_MOTION_HOME_RESULT.HOME_SUCCESS;

                // 센서 (Limit / Home)
                bool pel = false, mel = false, org = false;
                AXM.GetPositiveLimitValue(AxisNo, ref pel);
                AXM.GetNegativeLimitValue(AxisNo, ref mel);
                AXM.GetHomeSensorValue   (AxisNo, ref org);
                Sensor_PEL = pel;
                Sensor_MEL = mel;
                Sensor_ORG = org;

                // 서보 ON 동기화
                bool svOn = false;
                if (AXM.GetAmpEnabled(AxisNo, ref svOn) == 0)
                    IsServoOn = svOn;
            }
            catch
            {
                // 폴링 중 예외는 무시 (다음 주기에 재시도)
            }
        }

        /// <summary>실보드 이동 완료(IsMoving=false) 또는 알람 발생까지 대기.</summary>
        private async Task WaitUntilMoveDoneReal()
        {
            int guard = 0;
            int maxGuard = Math.Max(1, Setup.MoveTimeoutMs / 10);
            while (IsMoving && !IsAlarm)
            {
                await Task.Delay(10).ContinueWith(_ => { });
                if (++guard > maxGuard) break;
            }
        }
    }
}
