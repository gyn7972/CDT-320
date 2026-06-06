using QMC.Common.Motion;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion.Ajin;
using QMC.CDT320.Interlocks;
using System;
using System.Threading.Tasks;

namespace QMC.CDT320.Ajin
{
    public class AjinAxis : BaseAxis
    {
        private readonly object _sync = new object();
        private int _motionDirection;

        public int AxisNo { get; }

        // 보드 raw enum 값 캐시. ReadSetupFromBoard 시 채워지고,
        // WriteSetupToBoard 시 모델 → AXL enum 매핑이 동일 카테고리이면 raw 를 그대로 재사용한다.
        // 모델 enum 종류수 < AXL enum 종류수 인 항목들의 정보 손실을 라운드트립에서 방지한다.
        private AXM.MotorOutputMethod? _rawPulseOutput;
        private AXM.EncoderInputMethod? _rawEncoderInput;
        private AXT_MOTION_PROFILE_MODE? _rawProfileMode;

        protected override bool UseInternalStatusUpdate
        {
            get { return false; }
        }

        private bool UseSimulation
        {
            get { return Config != null && Config.IsSimulationMode; }
        }

        private double AxisHomeTarget()
        {
            return Setup != null ? Setup.HomeOffset : 0.0;
        }

        private int FailAjinAxisNotReady(string action, double targetPosition, bool hasTarget)
        {
            string reason;
            if (!AjinSystem.IsOpen)
                reason = "AXL is not open.";
            else if (IsAlarm)
                reason = "Axis alarm is ON. AlarmCode=0x" + AlarmCode.ToString("X4");
            else if (!IsServoOn)
                reason = "Servo is OFF.";
            else
                reason = "Axis is not ready.";

            return FailMotion(-2, action, reason, targetPosition, hasTarget);
        }

        public bool CanUseSynchronizedAbsoluteMove
        {
            get { return !UseSimulation && AjinSystem.IsOpen; }
        }

        public AjinAxis(string name, int axisNo) : base(name)
        {
            AxisNo = axisNo;
            Config.IsSimulationMode = false;
        }

        public override void ServoOn()
        {
            if (UseSimulation)
            {
                base.ServoOn();
                return;
            }

            if (!AjinSystem.IsOpen || IsAlarm) return;
            int ret;
            lock (_sync)
                ret = AXM.SetAmpEnabled(AxisNo, true);
            if (ret == 0)
                IsServoOn = true;
        }

        public override void ServoOff()
        {
            if (UseSimulation)
            {
                base.ServoOff();
                return;
            }

            Stop();
            if (!AjinSystem.IsOpen) return;
            lock (_sync)
                AXM.SetAmpEnabled(AxisNo, false);
            IsServoOn = false;
        }

        public override void ResetAlarm()
        {
            _motionDirection = 0;

            if (UseSimulation || !AjinSystem.IsOpen)
            {
                base.ResetAlarm();
                return;
            }

            lock (_sync)
                AXM.AlarmReset(AxisNo, true);
            IsAlarm = false;
            AlarmCode = 0;
        }

        public override async Task<int> MoveAbsoluteAsync(double targetPos, double velocity = 0)
        {
            try
            {
                if (UseSimulation)
                    return await base.MoveAbsoluteAsync(targetPos, velocity);

                string interlockReason;
                if (!MotionGuardRuntime.VerifyAxisMove(this, targetPos, out interlockReason))
                    return FailMotion(-11, "ABS MOVE", interlockReason, targetPos, true);

                if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen)
                    return FailAjinAxisNotReady("ABS MOVE", targetPos, true);

                UpdateStatus();
                int limitCheck = CheckSoftLimitTarget(targetPos);
                if (limitCheck != 0)
                    return limitCheck;

                double vel = velocity > 0 ? velocity : Config.DefaultVelocity;
                CommandPosition = targetPos;
                CurrentVelocity = vel;
                IsMoving = true;
                IsInPosition = false;
                _motionDirection = targetPos > ActualPosition ? 1 : targetPos < ActualPosition ? -1 : 0;

                int ret;
                lock (_sync)
                {
                    AXM.SetAbsRelMode(AxisNo, true);
                    ret = AXM.MovePosition(AxisNo, targetPos, vel, Config.Acceleration, Config.Deceleration);
                }
                if (ret != 0)
                {
                    IsMoving = false;
                    IsAlarm = true;
                    AlarmCode = (uint)ret;
                    _motionDirection = 0;
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-MOVE-ABS",
                        Name,
                        "AXM.MovePosition failed. ret=0x" + ret.ToString("X4"));
                    return ret;
                }

                RaiseMoveStarted();
                int waitRet = await WaitUntilMoveDone();
                if (waitRet == 0 && !IsAlarm)
                    _motionDirection = 0;
                if (IsAlarm)
                    return FailMotion((int)AlarmCode, "ABS MOVE", "Axis alarm occurred during move.", targetPos, true);
                if (waitRet != 0)
                    return FailMotion(waitRet, "ABS MOVE", "Move wait failed.", targetPos, true);
                ClearMotionFailure();
                return 0;
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                _motionDirection = 0;
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-MOVE-ABS",
                    Name,
                    ex.Message);
                return -1;
            }
            finally
            {
                UpdateStatus();
            }
        }

        public override async Task<int> MoveRelativeAsync(double distance, double velocity = 0)
        {
            try
            {
                if (UseSimulation || !AjinSystem.IsOpen)
                    return await base.MoveRelativeAsync(distance, velocity);

                UpdateStatus();
                double targetPos = ActualPosition + distance;
                return await MoveAbsoluteAsync(targetPos, velocity);
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-MOVE-REL",
                    Name,
                    ex.Message);
                return -1;
            }
            finally
            {
                UpdateStatus();
            }
        }

        public int ValidateSynchronizedAbsoluteMove(double targetPos, double velocity, out double resolvedVelocity)
        {
            resolvedVelocity = velocity > 0 ? velocity : Config.DefaultVelocity;

            if (!CanUseSynchronizedAbsoluteMove)
                return FailAjinAxisNotReady("SYNC ABS MOVE", targetPos, true);
            if (!IsServoOn || IsAlarm)
                return FailAjinAxisNotReady("SYNC ABS MOVE", targetPos, true);

            UpdateStatus();
            return CheckSoftLimitTarget(targetPos);
        }

        public void BeginSynchronizedAbsoluteMove(double targetPos, double velocity)
        {
            double resolvedVelocity = velocity > 0 ? velocity : Config.DefaultVelocity;
            CommandPosition = targetPos;
            CurrentVelocity = resolvedVelocity;
            IsMoving = true;
            IsInPosition = false;
            _motionDirection = targetPos > ActualPosition ? 1 : targetPos < ActualPosition ? -1 : 0;
            RaiseMoveStarted();
        }

        public void FailSynchronizedAbsoluteMove(int ret)
        {
            IsMoving = false;
            IsAlarm = true;
            AlarmCode = (uint)ret;
            _motionDirection = 0;
            AlarmManager.Raise(
                AlarmSeverity.Error,
                "AX-MOVE-MULTI",
                Name,
                "AXM.MoveMultiplePosition failed. ret=0x" + ret.ToString("X4"));
        }

        public async Task<int> WaitSynchronizedMoveDoneAsync()
        {
            int waitRet = await WaitUntilMoveDone();
            if (waitRet == 0 && !IsAlarm)
                _motionDirection = 0;
            return IsAlarm ? (int)AlarmCode : waitRet;
        }

        public override void Stop()
        {
            _motionDirection = 0;

            if (UseSimulation)
            {
                base.Stop();
                return;
            }

            base.Stop();
            if (!AjinSystem.IsOpen) return;
            lock (_sync)
                AXM.Stop(AxisNo, Config.Deceleration);
        }

        public override void EStop()
        {
            _motionDirection = 0;

            if (UseSimulation)
            {
                base.EStop();
                return;
            }

            base.EStop();
            if (!AjinSystem.IsOpen) return;
            lock (_sync)
                AXM.StopEmergency(AxisNo);
        }

        public override async Task<int> HomeSearchAsync()
        {
            try
            {
                if (UseSimulation)
                    return await base.HomeSearchAsync();

                string interlockReason;
                if (!MotionGuardRuntime.VerifyAxisHome(this, out interlockReason))
                    return FailMotion(-11, "HOME", interlockReason, AxisHomeTarget(), true);

                if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen)
                    return FailAjinAxisNotReady("HOME", AxisHomeTarget(), true);

                IsHomeDone = false;
                IsMoving = true;
                IsInPosition = false;

                int ret;
                lock (_sync)
                    ret = AXM.SetHomeStart(AxisNo);
                if (ret != 0)
                {
                    IsMoving = false;
                    IsAlarm = true;
                    AlarmCode = (uint)ret;
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-HOME",
                        Name,
                        "AXM.SetHomeStart failed. ret=0x" + ret.ToString("X4"));
                    return ret;
                }

                RaiseMoveStarted();

                int guard = 0;
                while (!IsHomeDone && !IsAlarm)
                {
                    UpdateStatus();
                    await Task.Delay(20).ContinueWith(_ => { });
                    if (++guard > 3000)
                    {
                        IsMoving = false;
                        AlarmManager.Raise(
                            AlarmSeverity.Error,
                            "AX-HOME",
                            Name,
                            "Home search timeout. AxisNo=" + AxisNo);
                        return -3;
                    }
                }

                IsMoving = false;
                IsInPosition = IsHomeDone;
                if (IsHomeDone)
                {
                    RaiseMoveCompleted();
                    ClearMotionFailure();
                    return 0;
                }
                if (IsAlarm)
                    return FailMotion((int)AlarmCode, "HOME", "Axis alarm occurred during home search.", AxisHomeTarget(), true);
                return FailMotion(-1, "HOME", "Home search failed before HomeDone.", AxisHomeTarget(), true);
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-HOME",
                    Name,
                    ex.Message);
                return -1;
            }
            finally
            {
                UpdateStatus();
            }
        }

        public override void SetPosition(double newPosition)
        {
            if (UseSimulation)
            {
                base.SetPosition(newPosition);
                return;
            }

            base.SetPosition(newPosition);
            if (!AjinSystem.IsOpen) return;
            lock (_sync)
            {
                AXM.SetCommandPosition(AxisNo, newPosition);
                AXM.SetActualPosition(AxisNo, newPosition);
            }
        }

        public override void MoveJogContinuous(int direction, JogSpeedType speedType, double customVel = 0)
        {
            try
            {
                if (UseSimulation)
                {
                    base.MoveJogContinuous(direction, speedType, customVel);
                    return;
                }

                if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen)
                {
                    FailAjinAxisNotReady("JOG", 0, false);
                    return;
                }

                double jogTarget = direction > 0 ? Setup.SoftLimitPlus : Setup.SoftLimitMinus;
                string interlockReason;
                if (!MotionGuardRuntime.VerifyAxisMove(this, jogTarget, out interlockReason))
                {
                    RecordMotionFailure(-11, "JOG", interlockReason, jogTarget, true);
                    return;
                }

                UpdateStatus();

                int jogDirection = direction < 0 ? -1 : 1;
                double vel = GetJogVelocity(speedType, customVel);
                double signedVel = jogDirection * Math.Abs(vel);
                CurrentVelocity = signedVel;
                IsMoving = true;
                IsInPosition = false;
                _motionDirection = jogDirection;

                int ret;
                lock (_sync)
                    ret = AXM.MoveVelocity(AxisNo, signedVel, Config.Acceleration, Config.Deceleration);
                if (ret != 0)
                {
                    IsMoving = false;
                    IsAlarm = true;
                    AlarmCode = (uint)ret;
                    _motionDirection = 0;
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-JOG",
                        Name,
                        "AXM.MoveVelocity failed. ret=0x" + ret.ToString("X4"));
                    return;
                }

                RaiseMoveStarted();
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                _motionDirection = 0;
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-JOG",
                    Name,
                    ex.Message);
            }
            finally
            {
                UpdateStatus();
            }
        }

        public override async Task<int> MoveJogStepAsync(int direction, JogSpeedType speedType,
                                                         double stepDistance, double customVel = 0)
        {
            try
            {
                if (UseSimulation)
                    return await base.MoveJogStepAsync(direction, speedType, stepDistance, customVel);

                if (!IsServoOn || IsAlarm)
                    return FailAjinAxisNotReady("JOG STEP", 0, false);

                int jogDirection = direction < 0 ? -1 : 1;
                double vel = GetJogVelocity(speedType, customVel);
                double distance = jogDirection * Math.Abs(stepDistance);
                if (distance == 0)
                    return 0;

                UpdateStatus();
                if (IsMoving)
                    return FailMotion(-2, "JOG STEP", "Axis is already moving.");

                return await MoveRelativeAsync(distance, vel);
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-JOG-STEP",
                    Name,
                    ex.Message);
                return -1;
            }
            finally
            {
                UpdateStatus();
            }
        }

        public override void StopJog()
        {
            _motionDirection = 0;

            if (UseSimulation || !AjinSystem.IsOpen)
            {
                base.StopJog();
                return;
            }

            lock (_sync)
                AXM.Stop(AxisNo, Config.Deceleration);
            base.StopJog();
            UpdateStatus();
        }

        public override void UpdateStatus()
        {
            if (Config.IsSimulationMode)
            {
                base.UpdateStatus();
                return;
            }
            if (!AjinSystem.IsOpen) 
                return;

            double cmd = 0;
            double act = 0;
            bool mot = false;
            bool inp = false;
            bool fault = false;
            bool pel = false;
            bool mel = false;
            bool org = false;
            bool svOn = false;
            var homeResult = AXT_MOTION_HOME_RESULT.HOME_SEARCHING;
            int homeRet;
            int servoRet;

            MOTION_INFO info = new MOTION_INFO();



            //외부 센서 및 모터 관련 신호 상태값: AXT_MOTION_QIMECHANICAL_SIGNAL_DEF 
            //    - [00001h]Bit 0, +Limit 급정지 신호 현재 상태 
            //    - [00002h] Bit 1, -Limit 급정지 신호 현재 상태 
            //    - [00004h]Bit 2, +limit 감속정지 현재 상태
            //    - [00008h]Bit 3, -limit 감속정지 현재 상태
            //    - [00010h]Bit 4, Alarm 신호 신호 현재 상태
            //    - [00020h]Bit 5, InPos 신호 현재 상태
            //    - [00040h]Bit 6, 비상 정지 신호(ESTOP) 현재 상태
            //    - [00080h]Bit 7, 원점 신호 헌재 상태
            //    - [00100h]Bit 8, Z 상 입력 신호 현재 상태
            //    - [00200h]Bit 9, ECUP 터미널 신호 상태
            //    - [00400h]Bit 10, ECDN 터미널 신호 상태
            //    - [00800h]Bit 11, EXPP 터미널 신호 상태
            //    - [01000h]Bit 12, EXMP 터미널 신호 상태
            //    - [02000h]Bit 13, SQSTR1 터미널 신호 상태
            //    - [04000h]Bit 14, SQSTR2 터미널 신호 상태
            //    - [08000h]Bit 15, SQSTP1 터미널 신호 상태
            //    - [10000h]Bit 16, SQSTP2 터미널 신호 상태
            //    - [20000h]Bit 17, MODE 터미널 신호 상태
            const uint PlustLimitMask = 0x00001;
            const uint MinusLimitMask = 0x00002;
            const uint PlustDecelStopMask = 0x00004;
            const uint MinusDecelStopMask = 0x00008;
            const uint AlarmMask = 0x00010;
            const uint InPositionMask = 0x00020;
            const uint EstopMask = 0x00040;
            const uint OriginMask = 0x00080;
            const uint ZPhaseMask = 0x00100;
            const uint EcupMask = 0x00200;
            const uint EcdnMask = 0x00400;
            const uint ExppMask = 0x00800;
            const uint ExmpMask = 0x01000;
            const uint Sqstr1Mask = 0x02000;
            const uint Sqstr2Mask = 0x04000;
            const uint Sqstp1Mask = 0x08000;
            const uint Sqstp2Mask = 0x10000;
            const uint ModeMask = 0x20000;



            lock (_sync)
            {
                info.uMask = 0x1F;
                AXM.GetMotionInfo(AxisNo, ref info);
                cmd = info.dCmdPos;
                act = info.dActPos;
                mot = (info.uMechSig & 0x1) != 0;
                inp = (info.uMechSig & InPositionMask) != 0;
                fault = (info.uMechSig & AlarmMask) != 0;
                pel = (info.uMechSig & PlustLimitMask) != 0;
                mel = (info.uMechSig & MinusLimitMask) != 0;
                org = (info.uMechSig & OriginMask) != 0;
                servoRet = AXM.GetAmpEnabled(AxisNo, ref svOn);
                homeRet = AXM.GetHomeResult(AxisNo, ref homeResult);
                // Todo : 구부장 아래 내용  AXM.GetMotionInfo(AxisNo, ref info); 이것으로 대체 되는 것들은 삭제 했음.
                // 주석 확인 했으면 아래 주석 삭제 할것.

                //AXM.GetCommandPosition(AxisNo, ref cmd);
                //AXM.GetActualPosition(AxisNo, ref act);
                //AXM.GetInMotion(AxisNo, ref mot);
                //AXM.GetInPositionValue(AxisNo, ref inp);
                //AXM.GetAmpFaultValue(AxisNo, ref fault);
                //AXM.GetPositiveLimitValue(AxisNo, ref pel);
                //AXM.GetNegativeLimitValue(AxisNo, ref mel);
                //AXM.GetHomeSensorValue(AxisNo, ref org);

            }

            ApplyReadStatus(cmd, act, mot, inp, fault, homeRet, homeResult, pel, mel, org, servoRet, svOn);
        }

        private void ApplyReadStatus(
            double cmd,
            double act,
            bool mot,
            bool inp,
            bool fault,
            int homeRet,
            AXT_MOTION_HOME_RESULT homeResult,
            bool pel,
            bool mel,
            bool org,
            int servoRet,
            bool svOn)
        {
            CommandPosition = cmd;

            double prev = ActualPosition;
            ActualPosition = act;
            if (prev != act)
                RaisePositionChanged();

            bool wasMoving = IsMoving;
            IsMoving = mot;
            IsInPosition = inp;
            if (wasMoving && !IsMoving && IsInPosition)
            {
                _motionDirection = 0;
                RaiseMoveCompleted();
            }

            bool wasAlarm = IsAlarm;
            bool softLimitPositive = Setup != null && Setup.SoftLimitEnabled &&
                ActualPosition >= Setup.SoftLimitPlus && _motionDirection > 0;
            bool softLimitNegative = Setup != null && Setup.SoftLimitEnabled &&
                ActualPosition <= Setup.SoftLimitMinus && _motionDirection < 0;
            bool hardLimitPositive = pel && _motionDirection > 0;
            bool hardLimitNegative = mel && _motionDirection < 0;

            IsAlarm = fault || softLimitPositive || softLimitNegative || hardLimitPositive || hardLimitNegative;
            if (IsAlarm)
            {
                if (fault)
                    AlarmCode = AlarmCode == 0 ? 1u : AlarmCode;
                else if (softLimitPositive)
                    AlarmCode = 10;
                else if (softLimitNegative)
                    AlarmCode = 11;
                else if (hardLimitPositive)
                    AlarmCode = 20;
                else if (hardLimitNegative)
                    AlarmCode = 21;

                if (!wasAlarm)
                {
                    RaiseAxisAlarmForCurrentStatus(fault, softLimitPositive, softLimitNegative, hardLimitPositive, hardLimitNegative);
                }
            }
            else
            {
                AlarmCode = 0;
            }

            if (homeRet == 0)
                IsHomeDone = homeResult == AXT_MOTION_HOME_RESULT.HOME_SUCCESS;

            bool wasPel = Sensor_PEL;
            bool wasMel = Sensor_MEL;
            Sensor_PEL = pel;
            Sensor_MEL = mel;
            Sensor_ORG = org;
            if ((Sensor_PEL && !wasPel) || (Sensor_MEL && !wasMel))
            {
                string side = Sensor_PEL ? "PEL(+)" : "MEL(-)";
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "LIMIT-HIT",
                    Name,
                    "Limit sensor reached [" + side + "] AxisNo=" + AxisNo);
            }

            if (servoRet == 0)
                IsServoOn = svOn;
        }

        private int CheckSoftLimitTarget(double targetPos)
        {
            try
            {
                if (Setup == null || !Setup.SoftLimitEnabled)
                    return 0;

                if (targetPos > Setup.SoftLimitPlus)
                {
                    RaiseSoftLimitAlarm(10, "positive", targetPos, Setup.SoftLimitPlus);
                    return 10;
                }

                if (targetPos < Setup.SoftLimitMinus)
                {
                    RaiseSoftLimitAlarm(11, "negative", targetPos, Setup.SoftLimitMinus);
                    return 11;
                }

                return 0;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "AX-SOFT-LIMIT-CHECK", Name, ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private void RaiseSoftLimitAlarm(uint alarmCode, string side, double position, double limit)
        {
            try
            {
                IsMoving = false;
                IsInPosition = false;
                IsAlarm = true;
                AlarmCode = alarmCode;
                _motionDirection = 0;

                string message = "Soft limit reached (" + side + "). Position=" +
                    position.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                    ", Limit=" +
                    limit.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                    ", AxisNo=" + AxisNo;

                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    alarmCode == 10 ? "AX-SOFT-LIMIT-P" : "AX-SOFT-LIMIT-N",
                    Name,
                    message);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void RaiseAxisAlarmForCurrentStatus(
            bool fault,
            bool softLimitPositive,
            bool softLimitNegative,
            bool hardLimitPositive,
            bool hardLimitNegative)
        {
            try
            {
                if (fault)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-" + AxisNo,
                        Name,
                        "Servo alarm 0x" + AlarmCode.ToString("X4"));
                    return;
                }

                if (softLimitPositive || softLimitNegative)
                {
                    string side = softLimitPositive ? "positive" : "negative";
                    double limit = softLimitPositive ? Setup.SoftLimitPlus : Setup.SoftLimitMinus;
                    string message = "Soft limit reached (" + side + "). Position=" +
                        ActualPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        ", Limit=" +
                        limit.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        ", AxisNo=" + AxisNo;

                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        softLimitPositive ? "AX-SOFT-LIMIT-P" : "AX-SOFT-LIMIT-N",
                        Name,
                        message);
                    return;
                }

                if (hardLimitPositive || hardLimitNegative)
                {
                    string side = hardLimitPositive ? "PEL(+)" : "MEL(-)";
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "LIMIT-HIT",
                        Name,
                        "Limit sensor reached [" + side + "] AxisNo=" + AxisNo);
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        /// <summary>
        /// 보드에서 모니터링 전용 라이브 값을 한 번에 읽어 <see cref="AxisLiveStatus"/> 로 반환한다.<br/>
        /// 시뮬레이션이거나 보드가 닫혀 있으면 <c>null</c>.
        /// </summary>
        public AxisLiveStatus ReadLiveStatus()
        {
            if (Config.IsSimulationMode || !AjinSystem.IsOpen) return null;

            var s = new AxisLiveStatus();
            try
            {
                lock (_sync)
                {
                    AXM.MotorOutputMethod outMethod = 0;
                    AXM.EncoderInputMethod encMethod = 0;
                    ActiveLevel zLvl = 0, srvLvl = 0;
                    double maxVel = 0, unit = 0;
                    int pulse = 0;
                    AXM.GetOutputMethod(AxisNo, ref outMethod);
                    AXM.GetEncoderMethod(AxisNo, ref encMethod);
                    AXM.GetZPhaseLevel(AxisNo, ref zLvl);
                    AXM.GetAmpEnableLevel(AxisNo, ref srvLvl);
                    AXM.GetMaxVelocity(AxisNo, ref maxVel);
                    AXM.GetMoveUnitPerPulse(AxisNo, ref unit, ref pulse);
                    s.OutputMethod = outMethod;
                    s.EncoderMethod = encMethod;
                    s.ZPhaseLevel = zLvl;
                    s.ServoOnLevel = srvLvl;
                    s.MaxVelocity = maxVel;
                    s.MoveUnit = unit;
                    s.PulsePerUnit = pulse;

                    bool inpEnable = false, inpValue = false;
                    ActiveLevel inpLvl = 0;
                    AXM.GetInPositionEnable(AxisNo, ref inpEnable);
                    AXM.GetInPositionLevel(AxisNo, ref inpLvl);
                    AXM.GetInPositionValue(AxisNo, ref inpValue);
                    s.InPositionEnabled = inpEnable;
                    s.InPositionLevel = inpLvl;
                    s.InPositionValue = inpValue;

                    MotorEventAction posAct = 0, negAct = 0;
                    ActiveLevel posLvl = 0, negLvl = 0;
                    bool posVal = false, negVal = false;
                    double swPos = 0, swNeg = 0;
                    AXM.GetPositiveLimitAction(AxisNo, ref posAct);
                    AXM.GetPositiveLimitLevel(AxisNo, ref posLvl);
                    AXM.GetPositiveLimitValue(AxisNo, ref posVal);
                    AXM.GetNegativeLimitAction(AxisNo, ref negAct);
                    AXM.GetNegativeLimitLevel(AxisNo, ref negLvl);
                    AXM.GetNegativeLimitValue(AxisNo, ref negVal);
                    AXM.GetPositivePosition(AxisNo, ref swPos);
                    AXM.GetNegativePosition(AxisNo, ref swNeg);
                    s.PositiveLimitAction = posAct;
                    s.PositiveLimitLevel = posLvl;
                    s.PositiveLimitValue = posVal;
                    s.NegativeLimitAction = negAct;
                    s.NegativeLimitLevel = negLvl;
                    s.NegativeLimitValue = negVal;
                    s.SoftLimitPositive = swPos;
                    s.SoftLimitNegative = swNeg;

                    ActiveLevel ampFaultLvl = 0, ampResetLvl = 0, homeLvl = 0;
                    bool ampFaultVal = false, homeVal = false;
                    AXM.GetAmpFaultLevel(AxisNo, ref ampFaultLvl);
                    AXM.GetAmpFaultValue(AxisNo, ref ampFaultVal);
                    AXM.GetAmpResetLevel(AxisNo, ref ampResetLvl);
                    AXM.GetHomeSensorLevel(AxisNo, ref homeLvl);
                    AXM.GetHomeSensorValue(AxisNo, ref homeVal);
                    s.AmpFaultLevel = ampFaultLvl;
                    s.AmpFaultValue = ampFaultVal;
                    s.AmpResetLevel = ampResetLvl;
                    s.HomeSensorLevel = homeLvl;
                    s.HomeSensorValue = homeVal;
                }

                s.IsAlarm = IsAlarm;
                s.AlarmCode = AlarmCode;
                s.ActualPosition = ActualPosition;
                s.CommandPosition = CommandPosition;
                s.PositionError = CommandPosition - ActualPosition;
                return s;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(
                    AlarmSeverity.Warning,
                    "AX-READ-LIVE",
                    Name,
                    "ReadLiveStatus failed: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 보드에서 setup/config 에 매핑되는 파라미터를 읽어 현재 축의
        /// <see cref="AxisSetup"/> 및 <see cref="AxisConfig"/> 에 덮어쓴다.<br/>
        /// .mot 파일 LoadParameters 후 보드 → 모델 동기화 용도.
        /// </summary>
        /// <returns>적용 성공 여부.</returns>
        public bool ReadSetupFromBoard()
        {
            if (Config.IsSimulationMode || !AjinSystem.IsOpen) return false;

            try
            {
                AxisConfig c = Config;
                AxisSetup setup = Setup;

                lock (_sync)
                {
                    // Home velocities / accelerations
                    double v1 = 0, v2 = 0, vLast = 0, vIndex = 0, a1 = 0, a2 = 0;
                    if (AXM.GetHomeVelocity(AxisNo, ref v1, ref v2, ref vLast, ref vIndex, ref a1, ref a2) == 0)
                    {
                        c.HomeFirstVelocity = v1;
                        c.HomeSecondVelocity = v2;
                        c.HomeThirdVelocity = vLast;
                        c.HomeLastVelocity = vLast;
                        c.HomeIndexSearchVelocity = vIndex;
                        c.HomeFirstAcceleration = a1;
                        c.HomeSecondAcceleration = a2;
                        c.HomeVelocity = v1;
                    }

                    // Home method
                    HomeDirection hDir = HomeDirection.Ccw;
                    HomeSignal hSig = HomeSignal.HomeSensor;
                    HomeZPhase hZ = 0;
                    double hClr = 0, hOff = 0;
                    if (AXM.GetHomeMethod(AxisNo, ref hDir, ref hSig, ref hZ, ref hClr, ref hOff) == 0 && setup != null)
                    {
                        setup.HomeDirection = hDir;
                        setup.HomeSignal = hSig;
                        setup.HomeOffset = hOff;
                    }

                    // Max velocity
                    double maxVel = 0;
                    if (AXM.GetMaxVelocity(AxisNo, ref maxVel) == 0 && maxVel > 0)
                        c.MaxVelocity = maxVel;

                    // Unit / pulse
                    double unit = 0; int pulse = 0;
                    if (AXM.GetMoveUnitPerPulse(AxisNo, ref unit, ref pulse) == 0 && pulse > 0 && setup != null)
                        setup.PulsesPerUnit = pulse / (unit > 0 ? unit : 1.0);

                    // Signal levels
                    if (setup != null)
                    {
                        ActiveLevel srvLvl = 0, ampFault = 0, ampReset = 0, posLvl = 0, negLvl = 0;
                        AXM.GetAmpEnableLevel(AxisNo, ref srvLvl);
                        AXM.GetAmpFaultLevel(AxisNo, ref ampFault);
                        AXM.GetAmpResetLevel(AxisNo, ref ampReset);
                        AXM.GetPositiveLimitLevel(AxisNo, ref posLvl);
                        AXM.GetNegativeLimitLevel(AxisNo, ref negLvl);
                        setup.ServoOnLevel = srvLvl;
                        setup.AlarmLevel = ampFault;
                        setup.AlarmResetLevel = ampReset;
                        setup.PositiveLimitLevel = posLvl;
                        setup.NegativeLimitLevel = negLvl;

                        // Soft limits
                        double swPos = 0, swNeg = 0;
                        if (AXM.GetPositivePosition(AxisNo, ref swPos) == 0)
                            setup.SoftLimitPlus = swPos;
                        if (AXM.GetNegativePosition(AxisNo, ref swNeg) == 0)
                            setup.SoftLimitMinus = swNeg;

                        // Soft limit Enable 플래그
                        try
                        {
                            uint swUse = 0;
                            if (AXM.GetSoftLimitEnable(AxisNo, ref swUse) == 0)
                                setup.SoftLimitEnabled = swUse != 0;
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Warning, "AX-READ-SETUP", Name,
                                "SoftLimit enable read failed: " + ex.Message);
                        }

                        // Pulse out method (AXL 10 종류 → 프로젝트 PulseOutput 3 종류로 축약 매핑)
                        try
                        {
                            AXM.MotorOutputMethod outMethod = AXM.MotorOutputMethod.OneHighLowHigh;
                            if (AXM.GetOutputMethod(AxisNo, ref outMethod) == 0)
                            {
                                _rawPulseOutput = outMethod;
                                setup.PulseOutput = MapPulseOutput(outMethod);
                            }
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Warning, "AX-READ-SETUP", Name,
                                "PulseOutput read failed: " + ex.Message);
                        }

                        // Encoder input method (AXL 8 종류 → 프로젝트 EncoderInput 3 종류로 축약 매핑)
                        try
                        {
                            AXM.EncoderInputMethod encMethod = AXM.EncoderInputMethod.ObverseUpDownMode;
                            if (AXM.GetEncoderMethod(AxisNo, ref encMethod) == 0)
                            {
                                _rawEncoderInput = encMethod;
                                setup.EncoderInput = MapEncoderInput(encMethod);
                            }
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Warning, "AX-READ-SETUP", Name,
                                "EncoderInput read failed: " + ex.Message);
                        }

                        // Inposition level (Low/High만 매핑, Used/Unused는 보존)
                        try
                        {
                            ActiveLevel inpLvl = ActiveLevel.Low;
                            if (AXM.GetInPositionLevel(AxisNo, ref inpLvl) == 0)
                                setup.InPosition = inpLvl == ActiveLevel.High ? InPosition.High : InPosition.Low;
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Warning, "AX-READ-SETUP", Name,
                                "InPosition level read failed: " + ex.Message);
                        }

                        // Stop mode (AXL: 0=EMG, 1=Slowdown) - 리밋 정지 모드 기준
                        try
                        {
                            uint stopRaw = 0;
                            if (AXM.GetLimitStopMode(AxisNo, ref stopRaw) == 0)
                                setup.StopMode = stopRaw == 0 ? StopMode.Emergency : StopMode.DecelStop;
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Warning, "AX-READ-SETUP", Name,
                                "StopMode read failed: " + ex.Message);
                        }

                        // Emergency stop level
                        try
                        {
                            uint estopMode = 0;
                            ActiveLevel estopLvl = ActiveLevel.Low;
                            if (AXM.GetSignalStop(AxisNo, ref estopMode, ref estopLvl) == 0)
                                setup.EmergencyLevel = estopLvl;
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Warning, "AX-READ-SETUP", Name,
                                "EmergencyLevel read failed: " + ex.Message);
                        }

                        // Profile mode (AXL 0~4 → AxisProfileMode 2종류로 축약: Trapezoid/SCurve)
                        try
                        {
                            uint profRaw = 0;
                            if (AXM.GetProfileModeRaw(AxisNo, ref profRaw) == 0)
                            {
                                _rawProfileMode = (AXT_MOTION_PROFILE_MODE)profRaw;
                                setup.ProfileMode = profRaw <= 1 ? AxisProfileMode.Trapezoid : AxisProfileMode.SCurve;
                            }
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Warning, "AX-READ-SETUP", Name,
                                "ProfileMode read failed: " + ex.Message);
                        }

                        // Acc/Dec Jerk %
                        try
                        {
                            double accJerk = 0, decJerk = 0;
                            if (AXM.GetAccelerationJerk(AxisNo, ref accJerk) == 0)
                                setup.AccJerkPercent = (int)Math.Round(accJerk);
                            if (AXM.GetDecelerationJerk(AxisNo, ref decJerk) == 0)
                                setup.DecJerkPercent = (int)Math.Round(decJerk);
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Warning, "AX-READ-SETUP", Name,
                                "Jerk read failed: " + ex.Message);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(
                    AlarmSeverity.Warning,
                    "AX-READ-SETUP",
                    Name,
                    "ReadSetupFromBoard failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 현재 축의 <see cref="AxisSetup"/> / <see cref="AxisConfig"/> 값을 보드에 기록한다.<br/>
        /// SaveSpeedRows / Apply 흐름에서 호출되어 모델 → 보드 방향 동기화를 보장한다.
        /// 시뮬레이션 모드이거나 보드가 닫혀 있으면 아무 일도 하지 않고 false 를 반환한다.
        /// </summary>
        /// <returns>적용 성공 여부.</returns>
        public bool WriteSetupToBoard()
        {
            if (Config == null || Config.IsSimulationMode || !AjinSystem.IsOpen) return false;

            try
            {
                AxisConfig c = Config;
                AxisSetup setup = Setup;

                lock (_sync)
                {
                    // Move unit / pulse - 정수 캐스팅 시 손실되지 않도록 분모를 1 로 가정한다.
                    if (setup != null && setup.PulsesPerUnit > 0)
                    {
                        try { AXM.SetMoveUnitPerPulse(AxisNo, 1, (int)System.Math.Round(setup.PulsesPerUnit)); }
                        catch (Exception ex) { LogWriteWarn("MoveUnitPerPulse", ex); }
                    }

                    // Max velocity
                    if (c.MaxVelocity > 0)
                    {
                        try { AXM.SetMaxVelocity(AxisNo, c.MaxVelocity); }
                        catch (Exception ex) { LogWriteWarn("MaxVelocity", ex); }
                    }

                    // Home velocity / acceleration
                    try
                    {
                        AXM.SetHomeVelocity(AxisNo,
                            c.HomeFirstVelocity,
                            c.HomeSecondVelocity,
                            c.HomeLastVelocity,
                            c.HomeIndexSearchVelocity,
                            c.HomeFirstAcceleration,
                            c.HomeSecondAcceleration);
                    }
                    catch (Exception ex) { LogWriteWarn("HomeVelocity", ex); }

                    if (setup != null)
                    {
                        // Home method
                        try
                        {
                            AXM.SetHomeMethod(AxisNo,
                                setup.HomeDirection,
                                setup.HomeSignal,
                                HomeZPhase.None,
                                0.0,
                                setup.HomeOffset);
                        }
                        catch (Exception ex) { LogWriteWarn("HomeMethod", ex); }

                        // Signal levels
                        try { AXM.SetAmpEnableLevel(AxisNo, setup.ServoOnLevel); } catch (Exception ex) { LogWriteWarn("ServoOnLevel", ex); }
                        try { AXM.SetAmpFaultLevel(AxisNo, setup.AlarmLevel); } catch (Exception ex) { LogWriteWarn("AlarmLevel", ex); }
                        try { AXM.SetAmpResetLevel(AxisNo, setup.AlarmResetLevel); } catch (Exception ex) { LogWriteWarn("AlarmResetLevel", ex); }
                        try { AXM.SetPositiveLimitLevel(AxisNo, setup.PositiveLimitLevel); } catch (Exception ex) { LogWriteWarn("PositiveLimitLevel", ex); }
                        try { AXM.SetNegativeLimitLevel(AxisNo, setup.NegativeLimitLevel); } catch (Exception ex) { LogWriteWarn("NegativeLimitLevel", ex); }

                        // Soft limits (Use + Pos/Neg 통합 적용)
                        try { AXM.SetSoftLimits(AxisNo, setup.SoftLimitEnabled, setup.SoftLimitPlus, setup.SoftLimitMinus); }
                        catch (Exception ex) { LogWriteWarn("SoftLimits", ex); }

                        // Pulse out / Encoder input (모델 enum → AXL enum 매핑, 라운드트립 시 raw 보존)
                        try
                        {
                            AXM.MotorOutputMethod outValue;
                            if (_rawPulseOutput.HasValue && MapPulseOutput(_rawPulseOutput.Value) == setup.PulseOutput)
                                outValue = _rawPulseOutput.Value;
                            else
                                outValue = MapPulseOutputToAxl(setup.PulseOutput);
                            AXM.SetOutputMethod(AxisNo, outValue);
                        }
                        catch (Exception ex) { LogWriteWarn("PulseOutput", ex); }
                        try
                        {
                            AXM.EncoderInputMethod encValue;
                            if (_rawEncoderInput.HasValue && MapEncoderInput(_rawEncoderInput.Value) == setup.EncoderInput)
                                encValue = _rawEncoderInput.Value;
                            else
                                encValue = MapEncoderInputToAxl(setup.EncoderInput);
                            AXM.SetEncoderMethod(AxisNo, encValue);
                        }
                        catch (Exception ex) { LogWriteWarn("EncoderInput", ex); }

                        // Inposition level (Low/High만 적용)
                        try
                        {
                            if (setup.InPosition == InPosition.Low || setup.InPosition == InPosition.High)
                                AXM.SetInPositionLevel(AxisNo, setup.InPosition);
                        }
                        catch (Exception ex) { LogWriteWarn("InPosition", ex); }

                        // Limit stop mode (Emergency=0, DecelStop=1)
                        try { AXM.SetLimitStopMode(AxisNo, setup.StopMode == StopMode.Emergency ? 0u : 1u); }
                        catch (Exception ex) { LogWriteWarn("LimitStopMode", ex); }

                        // Emergency stop signal level
                        try
                        {
                            uint estopMode = setup.StopMode == StopMode.Emergency ? 0u : 1u;
                            uint estopLvl = setup.EmergencyLevel == ActiveLevel.High ? 1u : 0u;
                            AXM.SetSignalStop(AxisNo, estopMode, estopLvl);
                        }
                        catch (Exception ex) { LogWriteWarn("EmergencyLevel", ex); }

                        // Profile mode (모델 2종 → AXL 5종 매핑, 라운드트립 시 raw 보존)
                        try
                        {
                            AXT_MOTION_PROFILE_MODE prof;
                            bool rawIsTrap = _rawProfileMode.HasValue && (uint)_rawProfileMode.Value <= 1;
                            bool modelIsTrap = setup.ProfileMode == AxisProfileMode.Trapezoid;
                            if (_rawProfileMode.HasValue && rawIsTrap == modelIsTrap)
                                prof = _rawProfileMode.Value;
                            else
                                prof = modelIsTrap
                                    ? AXT_MOTION_PROFILE_MODE.SYM_TRAPEZOIDE_MODE
                                    : AXT_MOTION_PROFILE_MODE.SYM_S_CURVE_MODE;
                            AXM.SetProfileMode(AxisNo, prof);
                        }
                        catch (Exception ex) { LogWriteWarn("ProfileMode", ex); }

                        // Acc/Dec Jerk %
                        try { AXM.SetAccelerationJerk(AxisNo, setup.AccJerkPercent); } catch (Exception ex) { LogWriteWarn("AccJerk", ex); }
                        try { AXM.SetDecelerationJerk(AxisNo, setup.DecJerkPercent); } catch (Exception ex) { LogWriteWarn("DecJerk", ex); }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(
                    AlarmSeverity.Warning,
                    "AX-WRITE-SETUP",
                    Name,
                    "WriteSetupToBoard failed: " + ex.Message);
                return false;
            }
        }

        private void LogWriteWarn(string field, Exception ex)
        {
            AlarmManager.Raise(AlarmSeverity.Warning, "AX-WRITE-SETUP", Name,
                field + " write failed: " + ex.Message);
        }

        /// <summary>
        /// 프로젝트 PulseOutput(3 종) → AXL MotorOutputMethod(8 종) 역매핑.
        /// 모델은 정보가 적으므로 AXL 의 대표값을 선택한다.
        /// </summary>
        private static AXM.MotorOutputMethod MapPulseOutputToAxl(PulseOutput m)
        {
            switch (m)
            {
                case PulseOutput.TwoPulse_High_CCW_CW: return AXM.MotorOutputMethod.TwoCcwCwHigh;
                case PulseOutput.TwoPulse_Low_CCW_CW: return AXM.MotorOutputMethod.TwoCcwCwLow;
                default: return AXM.MotorOutputMethod.OneHighLowHigh; // AB_Phase 등은 1펄스 대표값
            }
        }

        /// <summary>
        /// 프로젝트 EncoderInput(3 종) → AXL EncoderInputMethod(8 종) 역매핑.
        /// </summary>
        private static AXM.EncoderInputMethod MapEncoderInputToAxl(EncoderInput m)
        {
            switch (m)
            {
                case EncoderInput.Normal: return AXM.EncoderInputMethod.ObverseSqr4Mode;
                case EncoderInput.Reverse_SQR4: return AXM.EncoderInputMethod.ReverseSqr4Mode;
                default: return AXM.EncoderInputMethod.ReverseUpDownMode;
            }
        }

        /// <summary>
        /// AXL 의 펄스 출력 방식(10 종류)을 프로젝트 PulseOutput(3 종류)으로 축약 매핑한다.
        /// 1펄스(One*) 계열은 AB_Phase 가 가장 가깝고, TwoCwCcw* 계열은 보드 기본값 TwoPulse_High_CCW_CW 와 매칭.
        /// </summary>
        private static PulseOutput MapPulseOutput(AXM.MotorOutputMethod m)
        {
            switch (m)
            {
                case AXM.MotorOutputMethod.TwoCcwCwHigh:
                case AXM.MotorOutputMethod.TwoCwCcwHigh:
                    return PulseOutput.TwoPulse_High_CCW_CW;
                case AXM.MotorOutputMethod.TwoCcwCwLow:
                case AXM.MotorOutputMethod.TwoCwCcwLow:
                    return PulseOutput.TwoPulse_Low_CCW_CW;
                default:
                    return PulseOutput.AB_Phase;
            }
        }

        /// <summary>
        /// AXL 의 엔코더 입력 방식(8 종류)을 프로젝트 EncoderInput(3 종류)으로 축약 매핑한다.
        /// </summary>
        private static EncoderInput MapEncoderInput(AXM.EncoderInputMethod m)
        {
            switch (m)
            {
                case AXM.EncoderInputMethod.ObverseUpDownMode:
                case AXM.EncoderInputMethod.ObverseSqr1Mode:
                case AXM.EncoderInputMethod.ObverseSqr2Mode:
                case AXM.EncoderInputMethod.ObverseSqr4Mode:
                    return EncoderInput.Normal;
                case AXM.EncoderInputMethod.ReverseSqr4Mode:
                    return EncoderInput.Reverse_SQR4;
                default:
                    return EncoderInput.Reverse;
            }
        }

        private async Task<int> WaitUntilMoveDone()
        {
            int guard = 0;
            bool detectedMotion = false;
            while (!IsAlarm)
            {
                UpdateStatus();
                if (IsMoving)
                    detectedMotion = true;

                if (detectedMotion && !IsMoving && IsInPosition)
                    break;

                if (!detectedMotion && guard > 20 && IsInPosition)
                    break;

                await Task.Delay(10).ContinueWith(_ => { });
                if (++guard > 6000)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-MOVE-WAIT",
                        Name,
                        "Move wait timeout. AxisNo=" + AxisNo);
                    UpdateStatus();
                    return -3;
                }
            }

            UpdateStatus();
            return IsAlarm ? (int)AlarmCode : 0;
        }
    }
}

