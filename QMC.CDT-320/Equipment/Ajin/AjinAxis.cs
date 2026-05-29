using System;
using System.Threading.Tasks;
using QMC.Common.Motion;
using QMC.Common.Motion.Ajin;

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

        public override async Task MoveAbsoluteAsync(double targetPos, double velocity = 0)
        {
            try
            {
                if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen)
                    return;

                UpdateStatus();

                double vel = velocity > 0 ? velocity : Config.DefaultVelocity;
                CommandPosition = targetPos;
                CurrentVelocity = vel;
                IsMoving = true;
                IsInPosition = false;

                int ret;
                lock (_sync)
                    ret = AXM.MovePosition(AxisNo, targetPos, vel, Config.Acceleration, Config.Deceleration);
                if (ret != 0)
                {
                    IsMoving = false;
                    IsAlarm = true;
                    AlarmCode = (uint)ret;
                    return;
                }

                RaiseMoveStarted();
                await WaitUntilMoveDone();
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                Alarms.AlarmManager.Raise(
                    Alarms.AlarmSeverity.Error,
                    "AX-MOVE-ABS",
                    Name,
                    ex.Message);
            }
            finally
            {
                UpdateStatus();
            }
        }

        public override async Task MoveRelativeAsync(double distance, double velocity = 0)
        {
            try
            {
                if (!AjinSystem.IsOpen)
                {
                    await base.MoveRelativeAsync(distance, velocity);
                    return;
                }

                UpdateStatus();
                double targetPos = ActualPosition + distance;
                await MoveAbsoluteAsync(targetPos, velocity);
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                Alarms.AlarmManager.Raise(
                    Alarms.AlarmSeverity.Error,
                    "AX-MOVE-REL",
                    Name,
                    ex.Message);
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

        public override async Task HomeSearchAsync()
        {
            if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen) return;

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
                return;
            }

            RaiseMoveStarted();

            int guard = 0;
            while (!IsHomeDone && !IsAlarm)
            {
                UpdateStatus();
                await Task.Delay(20).ContinueWith(_ => { });
                if (++guard > 3000) break;
            }

            IsMoving = false;
            IsInPosition = IsHomeDone;
            if (IsHomeDone) RaiseMoveCompleted();
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
                Alarms.AlarmManager.Raise(
                    Alarms.AlarmSeverity.Error,
                    "AX-JOG",
                    Name,
                    ex.Message);
            }
            finally
            {
                UpdateStatus();
            }
        }

        public override async Task MoveJogStepAsync(int direction, JogSpeedType speedType,
                                                   double stepDistance, double customVel = 0)
        {
            try
            {
                if (!IsServoOn || IsAlarm)
                    return;

                int jogDirection = direction < 0 ? -1 : 1;
                double vel = GetJogVelocity(speedType, customVel);
                double distance = jogDirection * Math.Abs(stepDistance);

                UpdateStatus();
                await MoveRelativeAsync(distance, vel);
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                Alarms.AlarmManager.Raise(
                    Alarms.AlarmSeverity.Error,
                    "AX-JOG-STEP",
                    Name,
                    ex.Message);
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
            if (!AjinSystem.IsOpen) return;

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

            lock (_sync)
            {
                AXM.GetCommandPosition(AxisNo, ref cmd);
                AXM.GetActualPosition(AxisNo, ref act);
                AXM.GetInMotion(AxisNo, ref mot);
                AXM.GetInPositionValue(AxisNo, ref inp);
                AXM.GetAmpFaultValue(AxisNo, ref fault);
                homeRet = AXM.GetHomeResult(AxisNo, ref homeResult);
                AXM.GetPositiveLimitValue(AxisNo, ref pel);
                AXM.GetNegativeLimitValue(AxisNo, ref mel);
                AXM.GetHomeSensorValue(AxisNo, ref org);
                servoRet = AXM.GetAmpEnabled(AxisNo, ref svOn);
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
                    Alarms.AlarmManager.Raise(
                        Alarms.AlarmSeverity.Error,
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
                Alarms.AlarmManager.Raise(
                    Alarms.AlarmSeverity.Error,
                    "LIMIT-HIT",
                    Name,
                    "Limit sensor reached [" + side + "] AxisNo=" + AxisNo);
            }

            if (servoRet == 0)
                IsServoOn = svOn;
        }

        private async Task WaitUntilMoveDone()
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
                if (++guard > 6000) break;
            }

            UpdateStatus();
        }
    }
}
