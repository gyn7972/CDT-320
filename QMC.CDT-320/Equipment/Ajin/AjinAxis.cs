using QMC.Common.Motion;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion.Ajin;
using System;
using System.Threading.Tasks;

namespace QMC.CDT320.Ajin
{
    public class AjinAxis : BaseAxis
    {
        private readonly object _sync = new object();

        public int AxisNo { get; }

        protected override bool UseInternalStatusUpdate
        {
            get { return false; }
        }

        public AjinAxis(string name, int axisNo) : base(name)
        {
            AxisNo = axisNo;
            Config.IsSimulationMode = false;
        }

        public override void ServoOn()
        {
            if (!AjinSystem.IsOpen || IsAlarm) return;
            int ret;
            lock (_sync)
                ret = AXM.SetAmpEnabled(AxisNo, true);
            if (ret == 0)
                IsServoOn = true;
        }

        public override void ServoOff()
        {
            Stop();
            if (!AjinSystem.IsOpen) return;
            lock (_sync)
                AXM.SetAmpEnabled(AxisNo, false);
            IsServoOn = false;
        }

        public override void ResetAlarm()
        {
            if (!AjinSystem.IsOpen)
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
                if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen)
                    return -2;

                UpdateStatus();

                double vel = velocity > 0 ? velocity : Config.DefaultVelocity;
                CommandPosition = targetPos;
                CurrentVelocity = vel;
                IsMoving = true;
                IsInPosition = false;

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
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-MOVE-ABS",
                        Name,
                        "AXM.MovePosition failed. ret=0x" + ret.ToString("X4"));
                    return ret;
                }

                RaiseMoveStarted();
                int waitRet = await WaitUntilMoveDone();
                return IsAlarm ? (int)AlarmCode : waitRet;
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
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
                if (!AjinSystem.IsOpen)
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

        public override void Stop()
        {
            base.Stop();
            if (!AjinSystem.IsOpen) return;
            lock (_sync)
                AXM.Stop(AxisNo, Config.Deceleration);
        }

        public override void EStop()
        {
            base.EStop();
            if (!AjinSystem.IsOpen) return;
            lock (_sync)
                AXM.StopEmergency(AxisNo);
        }

        public override async Task<int> HomeSearchAsync()
        {
            try
            {
                if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen)
                    return -2;

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
                    return 0;
                }
                return IsAlarm ? (int)AlarmCode : -1;
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
                if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen)
                    return;

                UpdateStatus();

                int jogDirection = direction < 0 ? -1 : 1;
                double vel = GetJogVelocity(speedType, customVel);
                double signedVel = jogDirection * Math.Abs(vel);
                CurrentVelocity = signedVel;
                IsMoving = true;
                IsInPosition = false;

                int ret;
                lock (_sync)
                    ret = AXM.MoveVelocity(AxisNo, signedVel, Config.Acceleration, Config.Deceleration);
                if (ret != 0)
                {
                    IsMoving = false;
                    IsAlarm = true;
                    AlarmCode = (uint)ret;
                    return;
                }

                RaiseMoveStarted();
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
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
                if (!IsServoOn || IsAlarm)
                    return -2;

                int jogDirection = direction < 0 ? -1 : 1;
                double vel = GetJogVelocity(speedType, customVel);
                double distance = jogDirection * Math.Abs(stepDistance);
                if (distance == 0)
                    return 0;

                UpdateStatus();
                if (IsMoving)
                    return -2;

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
            if (!AjinSystem.IsOpen)
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
                RaiseMoveCompleted();

            bool wasAlarm = IsAlarm;
            IsAlarm = fault;
            if (IsAlarm)
            {
                AlarmCode = AlarmCode == 0 ? 1u : AlarmCode;
                if (!wasAlarm)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-" + AxisNo,
                        Name,
                        "Servo alarm 0x" + AlarmCode.ToString("X4"));
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

