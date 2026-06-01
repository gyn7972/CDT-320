using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    public class InputFeederSetup : ISetupData
    {
        public double AvoidPosition { get; set; } = 0.0;
        public double ExchangePositionY { get; set; } = 150.0;
        public double LoadReadyPosition { get; set; } = 20.0;
        public double CassetteLoadBasePosition { get; set; } = 30.0;
        public double CassetteLoadPitch { get; set; } = 5.0;
        public double WaferPickupPosition { get; set; } = 100.0;
        public double StagePutPosition { get; set; } = 160.0;
        public double StagePickPosition { get; set; } = 170.0;
        public double CassetteLoadPosition { get; set; } = 30.0;
        public double CassetteUnloadPosition { get; set; } = 30.0;
        public double CassetteExchangePosition { get; set; } = 150.0;
        public double StageLoadPosition { get; set; } = 160.0;
        public double StageBarcodePosition { get; set; } = 100.0;
        public double StageUnloadPosition { get; set; } = 170.0;
        public double InPositionTolerance { get; set; } = 0.05;
    }

    public class InputFeederConfig : IConfigData
    {
        public bool IsSimulationMode { get; set; } = true;
    }

    public class InputFeederRecipe : IRecipeData
    {
        public double MoveVelocity { get; set; } = 50.0;
        public int FeederMoveTimeoutMs { get; set; } = 5000;
        public int CylinderTimeoutMs { get; set; } = 1000;
    }

    public enum WaferFeederPositionType
    {
        Avoid,
        CassetteLoad,
        CassetteUnload,
        CassetteExchange,
        StageLoad,
        StageBarcode,
        StageUnload
    }

    public class InputFeederUnit : BaseUnit<InputFeederSetup, InputFeederConfig, InputFeederRecipe>
    {
        private readonly Dictionary<string, double> positionSnapshots = new Dictionary<string, double>();

        public BaseAxis FeederY { get; private set; }

        public BaseDigitalInput WaferFeederUpSensor { get; private set; }
        public BaseDigitalInput WaferFeederDownSensor { get; private set; }
        public BaseDigitalInput WaferFeederClampSensor { get; private set; }
        public BaseDigitalInput WaferFeederRingCheckSensor { get; private set; }
        public BaseDigitalInput WaferFeederOverloadSensor { get; private set; }
        public BaseDigitalInput WaferStage8RingCheckSensor { get; private set; }
        public BaseDigitalInput WaferStage12RingCheckSensor { get; private set; }

        public BaseDigitalInput WaferClampedSensor { get { return WaferFeederClampSensor; } }

        public BaseCylinder InputFeederLift { get; private set; }
        public BaseCylinder InputFeederClamp { get; private set; }
        public BaseCylinder FeederUpDownCyl { get { return InputFeederLift; } }
        public BaseCylinder FeederClampCyl { get { return InputFeederClamp; } }

        public BaseDigitalOutput InputFeederLiftUpCyl { get { return InputFeederLift.OutFwd; } }
        public BaseDigitalOutput InputFeederLiftDownCyl { get { return InputFeederLift.OutBwd; } }
        public BaseDigitalOutput InputFeederClampCyl { get { return InputFeederClamp.OutFwd; } }
        public BaseDigitalOutput InputFeederUnClampCyl { get { return InputFeederClamp.OutBwd; } }
        public BaseDigitalOutput WaferFeederUpOut { get { return InputFeederLiftUpCyl; } }
        public BaseDigitalOutput WaferFeederDownOut { get { return InputFeederLiftDownCyl; } }
        public BaseDigitalOutput WaferFeederClampOut { get { return InputFeederClampCyl; } }
        public BaseDigitalOutput WaferFeederUnclampOut { get { return InputFeederUnClampCyl; } }

        public InputFeederUnit() : base("WaferFeederUnit")
        {
            FeederY = AjinFactory.CreateAxis("FeederY");
            WaferFeederUpSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederUp);
            WaferFeederDownSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederDown);
            WaferFeederClampSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederUpClamp);
            WaferFeederRingCheckSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederRingCheck);
            WaferFeederOverloadSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederOverloadCheck);
            WaferStage8RingCheckSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferStage8RingCheck);
            WaferStage12RingCheckSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferStage12RingCheck);
            InputFeederLift = CylinderManager.Get(AjinIoCatalog.CylinderRefs.InputFeederLift);
            InputFeederClamp = CylinderManager.Get(AjinIoCatalog.CylinderRefs.InputFeederClamp);

            Components.Add(FeederY);
            Components.Add(WaferFeederUpSensor);
            Components.Add(WaferFeederDownSensor);
            Components.Add(WaferFeederClampSensor);
            Components.Add(WaferFeederRingCheckSensor);
            Components.Add(WaferFeederOverloadSensor);
            Components.Add(WaferStage8RingCheckSensor);
            Components.Add(WaferStage12RingCheckSensor);
            Components.Add(InputFeederLift);
            Components.Add(InputFeederClamp);
        }

        public Task<int> MoveWaferFeederY(double targetPos, bool bFine = false)
        {
            return MoveWaferFeederYAsync(targetPos, bFine);
        }

        public async Task<int> MoveWaferFeederYAsync(double targetPos, bool bFine = false)
        {
            try
            {
                if (!CheckWaferFeederYMoveReady())
                    return RaiseFeederAlarm("WF-Y-READY", "WaferFeederY is not ready to move.");

                if (!ValidateWaferFeederYTargetPosition(targetPos))
                    return RaiseFeederAlarm("WF-Y-SOFT-LIMIT", "WaferFeederY target is out of soft limit. target=" + targetPos);

                EventLogger.Write(EventKind.Event, "QMC", "WF-Y-MOVE", "Move WaferFeederY target=" + targetPos);
                int result = await FeederY.MoveAbsoluteAsync(targetPos, bFine ? Recipe.MoveVelocity * 0.5 : Recipe.MoveVelocity);
                if (result != 0 || FeederY.IsAlarm)
                    return RaiseFeederAlarm("WF-Y-MOVE", "WaferFeederY move failed. result=" + result + ", alarm=" + FeederY.IsAlarm);

                return 0;
            }
            catch (Exception ex)
            {
                return RaiseFeederAlarm("WF-Y-MOVE-EX", "WaferFeederY move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        public Task<int> MoveWaferFeederYToTeachingPosition(string positionName, bool bFine = false)
        {
            return MoveWaferFeederYToTeachingPositionAsync(positionName, bFine);
        }

        public async Task<int> MoveWaferFeederYToTeachingPositionAsync(string positionName, bool bFine = false)
        {
            try
            {
                return await MoveWaferFeederYAsync(GetTeachingPosition(positionName), bFine);
            }
            catch (Exception ex)
            {
                return RaiseFeederAlarm("WF-TEACH-MOVE", "WaferFeederY teaching move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public Task<int> MoveToWaferFeederAvoidPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.AvoidPosition, bFine);
        }

        public Task<int> MoveToWaferFeederLoadReadyPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.LoadReadyPosition, bFine);
        }

        public Task<int> MoveToWaferFeederCassetteLoadPosition(int slotIndex, bool bFine = false)
        {
            return MoveWaferFeederY(CalculateWaferFeederCassetteLoadPosition(slotIndex), bFine);
        }

        public Task<int> MoveToWaferFeederCassetteUnloadPosition(int slotIndex, bool bFine = false)
        {
            return MoveWaferFeederY(CalculateWaferFeederCassetteUnloadPosition(slotIndex), bFine);
        }

        public Task<int> MoveToWaferFeederBarcodePosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.StageBarcodePosition, bFine);
        }

        public Task<int> MoveToWaferFeederStageLoadPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.StageLoadPosition, bFine);
        }

        public Task<int> MoveToWaferFeederStageUnloadPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.StageUnloadPosition, bFine);
        }

        public Task<int> MoveToWaferFeederExchangePosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.CassetteExchangePosition, bFine);
        }

        public Task<int> MoveToWaferFeederCassetteLoadPositionAsync(int slotIndex, bool bFine = false)
        {
            return MoveWaferFeederYAsync(CalculateWaferFeederCassetteLoadPosition(slotIndex), bFine);
        }

        public Task<int> MoveToWaferFeederCassetteUnloadPositionAsync(int slotIndex, bool bFine = false)
        {
            return MoveWaferFeederYAsync(CalculateWaferFeederCassetteUnloadPosition(slotIndex), bFine);
        }

        public Task<int> MoveToWaferFeederBarcodePositionAsync(bool bFine = false)
        {
            return MoveWaferFeederYAsync(Setup.StageBarcodePosition, bFine);
        }

        public Task<int> MoveToWaferFeederStageLoadPositionAsync(bool bFine = false)
        {
            return MoveWaferFeederYAsync(Setup.StageLoadPosition, bFine);
        }

        public Task<int> MoveToWaferFeederStageUnloadPositionAsync(bool bFine = false)
        {
            return MoveWaferFeederYAsync(Setup.StageUnloadPosition, bFine);
        }

        public Task<int> MoveToWaferFeederExchangePositionAsync(bool bFine = false)
        {
            return MoveWaferFeederYAsync(Setup.CassetteExchangePosition, bFine);
        }

        public Task<int> MoveToWaferFeederWaferPickupPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.WaferPickupPosition, bFine);
        }

        public Task<int> MoveToWaferFeederStagePutPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.StagePutPosition, bFine);
        }

        public Task<int> MoveToWaferFeederStagePickPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.StagePickPosition, bFine);
        }

        public async Task<bool> WaitWaferFeederYMoveDone(int timeoutMs)
        {
            return await WaitUntilAsync(() => !FeederY.IsMoving && FeederY.IsInPosition && !FeederY.IsAlarm, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederYInPosition(string positionName, int timeoutMs)
        {
            double target = GetTeachingPosition(positionName);
            return await WaitUntilAsync(() => IsWaferFeederYInPosition(target, Setup.InPositionTolerance), timeoutMs);
        }

        public bool IsWaferFeederYInPosition(double targetPos, double tolerance)
        {
            return Math.Abs(FeederY.ActualPosition - targetPos) <= tolerance;
        }

        public bool IsWaferFeederInAvoidPosition()
        {
            return IsWaferFeederYInPosition(Setup.AvoidPosition, Setup.InPositionTolerance);
        }

        public bool IsWaferFeederInCassetteLoadPosition(int slotIndex)
        {
            return IsWaferFeederYInPosition(CalculateWaferFeederCassetteLoadPosition(slotIndex), Setup.InPositionTolerance);
        }

        public bool IsWaferFeederInCassetteUnloadPosition(int slotIndex)
        {
            return IsWaferFeederYInPosition(CalculateWaferFeederCassetteUnloadPosition(slotIndex), Setup.InPositionTolerance);
        }

        public bool IsWaferFeederInStageLoadPosition()
        {
            return IsWaferFeederYInPosition(Setup.StageLoadPosition, Setup.InPositionTolerance);
        }

        public bool IsWaferFeederInStageUnloadPosition()
        {
            return IsWaferFeederYInPosition(Setup.StageUnloadPosition, Setup.InPositionTolerance);
        }

        public bool IsWaferFeederInBarcodePosition()
        {
            return IsWaferFeederYInPosition(Setup.StageBarcodePosition, Setup.InPositionTolerance);
        }

        public bool IsWaferFeederInExchangePosition()
        {
            return IsWaferFeederYInPosition(Setup.CassetteExchangePosition, Setup.InPositionTolerance);
        }

        public bool IsWaferFeederInLoadReadyPosition()
        {
            return IsWaferFeederYInPosition(Setup.LoadReadyPosition, Setup.InPositionTolerance);
        }

        public bool IsWaferFeederUp()
        {
            return WaferFeederUpSensor.IsOn;
        }

        public bool IsWaferFeederDown()
        {
            return WaferFeederDownSensor.IsOn;
        }

        public bool IsWaferFeederClamp()
        {
            return !WaferFeederClampSensor.IsOn;
        }

        public bool IsWaferFeederUnclamp()
        {
            return WaferFeederClampSensor.IsOn;
        }

        public bool IsWaferFeederRingCheck()
        {
            return WaferFeederRingCheckSensor.IsOn;
        }

        public bool IsWaferFeederRingDetected(bool expected = true)
        {
            return WaferFeederRingCheckSensor.IsOn == expected;
        }

        public bool IsWaferFeederOverload()
        {
            return WaferFeederOverloadSensor.IsOn;
        }

        public bool IsWaferFeederUnclamped()
        {
            return WaferFeederClampSensor.IsOn;
        }

        public void TeachWaferFeederYPosition(string positionName)
        {
            SetTeachingPosition(positionName, FeederY.ActualPosition);
            EventLogger.Write(EventKind.Event, "QMC", "WF-TEACH", "Teaching saved: " + positionName + "=" + FeederY.ActualPosition);
        }

        public void TeachWaferFeederAvoidPosition()
        {
            Setup.AvoidPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederLoadReadyPosition()
        {
            Setup.LoadReadyPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederCassetteLoadBasePosition()
        {
            Setup.CassetteLoadBasePosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederWaferPickupPosition()
        {
            Setup.WaferPickupPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederStagePutPosition()
        {
            Setup.StagePutPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederStagePickPosition()
        {
            Setup.StagePickPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederYCassetteLoadPosition()
        {
            Setup.CassetteLoadPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederYCassetteUnloadPosition()
        {
            Setup.CassetteUnloadPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederYStageLoadPosition()
        {
            Setup.StageLoadPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederYStageUnloadPosition()
        {
            Setup.StageUnloadPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederYBarcodePosition()
        {
            Setup.StageBarcodePosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederYExchangePosition()
        {
            Setup.CassetteExchangePosition = FeederY.ActualPosition;
        }

        public double CalculateWaferFeederCassetteLoadPosition(int slotIndex)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException("slotIndex");
            return Setup.CassetteLoadPosition + (Setup.CassetteLoadPitch * slotIndex);
        }

        public double CalculateWaferFeederCassetteUnloadPosition(int slotIndex)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException("slotIndex");
            return Setup.CassetteUnloadPosition + (Setup.CassetteLoadPitch * slotIndex);
        }

        public bool ValidateWaferFeederTeachingComplete()
        {
            return Setup.CassetteLoadPitch > 0.0 &&
                   Setup.CassetteLoadPosition != Setup.AvoidPosition &&
                   Setup.StageLoadPosition != Setup.StageUnloadPosition;
        }

        public async Task<int> MoveToTeachingPositionAndVerify(string positionName, bool bFine = false)
        {
            int result = await MoveWaferFeederYToTeachingPosition(positionName, bFine);
            if (result != 0)
                return result;

            if (!await WaitWaferFeederYInPosition(positionName, Recipe.FeederMoveTimeoutMs))
                return RaiseFeederAlarm("WF-TEACH-INPOS", "WaferFeederY teaching position timeout: " + positionName);

            return 0;
        }

        public Task<int> SetWaferFeederUpDown(bool up)
        {
            return SetWaferFeederUpDownAsync(up, Recipe.CylinderTimeoutMs);
        }

        public async Task<int> SetWaferFeederUpDownAsync(bool up, int timeoutMs)
        {
            try
            {
                if (IsWaferFeederOverload())
                    return RaiseFeederAlarm("WF-LIFT-OVERLOAD", "WaferFeeder overload is on.");

                bool ok = up ? await InputFeederLift.MoveFwdAsync() : await InputFeederLift.MoveBwdAsync();
                if (!ok)
                    return RaiseFeederAlarm(up ? "WF-LIFT-UP" : "WF-LIFT-DOWN", "WaferFeeder lift cylinder move failed.");

                bool sensorOk = up ? await WaitWaferFeederUp(timeoutMs) : await WaitWaferFeederDown(timeoutMs);
                if (!sensorOk)
                    return RaiseFeederAlarm(up ? "WF-LIFT-UP-TIMEOUT" : "WF-LIFT-DOWN-TIMEOUT", "WaferFeeder lift sensor timeout.");

                return 0;
            }
            catch (Exception ex)
            {
                return RaiseFeederAlarm("WF-LIFT-EX", "WaferFeeder lift exception: " + ex.Message);
            }
            finally
            {
            }
        }

        public Task<int> SetWaferFeederClamp(bool clamp)
        {
            return SetWaferFeederClampAsync(clamp, Recipe.CylinderTimeoutMs);
        }

        public async Task<int> SetWaferFeederClampAsync(bool clamp, int timeoutMs)
        {
            try
            {
                bool ok = clamp ? await InputFeederClamp.MoveFwdAsync() : await InputFeederClamp.MoveBwdAsync();
                if (!ok)
                    return RaiseFeederAlarm(clamp ? "WF-CLAMP" : "WF-UNCLAMP", "WaferFeeder clamp cylinder move failed.");

                bool sensorOk = clamp
                    ? await WaitWaferFeederClamp(timeoutMs)
                    : await WaitWaferFeederUnclamp(timeoutMs);
                if (!sensorOk)
                    return RaiseFeederAlarm(clamp ? "WF-CLAMP-TIMEOUT" : "WF-UNCLAMP-TIMEOUT", "WaferFeeder clamp sensor timeout.");

                return 0;
            }
            catch (Exception ex)
            {
                return RaiseFeederAlarm("WF-CLAMP-EX", "WaferFeeder clamp exception: " + ex.Message);
            }
            finally
            {
            }
        }

        public void SetWaferFeederLiftUpOutput(bool on)
        {
            SetExclusiveOutput(InputFeederLiftUpCyl, InputFeederLiftDownCyl, on, "WF-LIFT-UP-OUT");
        }

        public void SetWaferFeederLiftDownOutput(bool on)
        {
            SetExclusiveOutput(InputFeederLiftDownCyl, InputFeederLiftUpCyl, on, "WF-LIFT-DOWN-OUT");
        }

        public void SetWaferFeederClampOutput(bool on)
        {
            SetExclusiveOutput(InputFeederClampCyl, InputFeederUnClampCyl, on, "WF-CLAMP-OUT");
        }

        public void SetWaferFeederUnclampOutput(bool on)
        {
            SetExclusiveOutput(InputFeederUnClampCyl, InputFeederClampCyl, on, "WF-UNCLAMP-OUT");
        }

        public async Task<bool> WaitWaferFeederUp(int timeoutMs)
        {
            return await WaferFeederUpSensor.WaitUntilStateAsync(true, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederDown(int timeoutMs)
        {
            return await WaferFeederDownSensor.WaitUntilStateAsync(true, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederClamp(int timeoutMs)
        {
            return await WaferFeederClampSensor.WaitUntilStateAsync(false, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederUnclamp(int timeoutMs)
        {
            return await WaferFeederClampSensor.WaitUntilStateAsync(true, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederRingClear(int timeoutMs)
        {
            return await WaferFeederRingCheckSensor.WaitUntilStateAsync(false, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederRingState(bool expected, int timeoutMs)
        {
            return await WaferFeederRingCheckSensor.WaitUntilStateAsync(expected, timeoutMs);
        }

        public bool CheckWaferFeederOverloadClear()
        {
            return !WaferFeederOverloadSensor.IsOn;
        }

        public void ManualMoveWaferFeederYJog(int direction, double speed)
        {
            FeederY.MoveJogContinuous(direction, JogSpeedType.Custom, speed);
        }

        public void ManualStopWaferFeederY()
        {
            FeederY.StopJog();
        }

        public Task<int> ManualMoveToWaferFeederAvoidPosition(bool bFine = false)
        {
            return MoveToWaferFeederAvoidPosition(bFine);
        }

        public Task<int> ManualMoveToWaferFeederCassetteLoadPosition(int slotIndex, bool bFine = false)
        {
            return MoveToWaferFeederCassetteLoadPosition(slotIndex, bFine);
        }

        public Task<int> ManualMoveToWaferFeederCassetteUnloadPosition(int slotIndex, bool bFine = false)
        {
            return MoveToWaferFeederCassetteUnloadPosition(slotIndex, bFine);
        }

        public Task<int> ManualMoveToWaferFeederLoadReadyPosition(bool bFine = false)
        {
            return MoveToWaferFeederLoadReadyPosition(bFine);
        }

        public Task<int> ManualMoveToWaferFeederStageLoadPosition(bool bFine = false)
        {
            return MoveToWaferFeederStageLoadPosition(bFine);
        }

        public Task<int> ManualMoveToWaferFeederStageUnloadPosition(bool bFine = false)
        {
            return MoveToWaferFeederStageUnloadPosition(bFine);
        }

        public Task<int> ManualMoveToWaferFeederBarcodePosition(bool bFine = false)
        {
            return MoveToWaferFeederBarcodePosition(bFine);
        }

        public Task<int> ManualMoveToWaferFeederExchangePosition(bool bFine = false)
        {
            return MoveToWaferFeederExchangePosition(bFine);
        }

        public Task<int> ManualWaferFeederLiftUp(int timeoutMs)
        {
            return SetWaferFeederUpDownAsync(true, timeoutMs);
        }

        public Task<int> ManualWaferFeederLiftDown(int timeoutMs)
        {
            return SetWaferFeederUpDownAsync(false, timeoutMs);
        }

        public Task<int> ManualWaferFeederClamp(int timeoutMs)
        {
            return SetWaferFeederClampAsync(true, timeoutMs);
        }

        public Task<int> ManualWaferFeederUnclamp(int timeoutMs)
        {
            return SetWaferFeederClampAsync(false, timeoutMs);
        }

        public async Task<int> LoadWaferFromCassetteToFeeder(int slotIndex, int timeoutMs, bool bFine = false)
        {
            if (!CheckWaferFeederTransferReady(TransferMode.Load))
                return RaiseFeederAlarm("WF-LOAD-READY", "WaferFeeder load transfer is not ready.");

            int result = await MoveToWaferFeederCassetteLoadPosition(slotIndex, bFine);
            if (result != 0)
                return result;

            if (!await WaitWaferFeederYMoveDone(timeoutMs))
                return RaiseFeederAlarm("WF-LOAD-Y-TIMEOUT", "WaferFeeder cassette load position timeout.");

            result = await SetWaferFeederUpDown(true);
            if (result != 0) return result;

            result = await SetWaferFeederClamp(true);
            if (result != 0) return result;

            if (!await WaitWaferFeederClamp(timeoutMs))
                return RaiseFeederAlarm("WF-LOAD-CLAMP-TIMEOUT", "WaferFeeder clamp sensor timeout.");

            return 0;
        }

        public async Task<int> MoveWaferFeederToStagePutPosition(int timeoutMs, bool bFine = false)
        {
            if (!HasWaferOnFeeder())
                return RaiseFeederAlarm("WF-STAGE-PUT-EMPTY", "WaferFeeder has no wafer.");

            int result = await MoveToWaferFeederStageLoadPosition(bFine);
            if (result != 0)
                return result;

            if (!await WaitWaferFeederYMoveDone(timeoutMs))
                return RaiseFeederAlarm("WF-STAGE-PUT-TIMEOUT", "WaferFeeder stage put position timeout.");

            return 0;
        }

        public async Task<int> MoveWaferFeederToStagePickPosition(int timeoutMs, bool bFine = false)
        {
            int result = await MoveToWaferFeederStageUnloadPosition(bFine);
            if (result != 0)
                return result;

            if (!await WaitWaferFeederYMoveDone(timeoutMs))
                return RaiseFeederAlarm("WF-STAGE-PICK-TIMEOUT", "WaferFeeder stage pick position timeout.");

            return 0;
        }

        public async Task<int> ReturnWaferFromFeederToCassette(int slotIndex, int timeoutMs, bool bFine = false)
        {
            int result = await MoveToWaferFeederCassetteUnloadPosition(slotIndex, bFine);
            if (result != 0)
                return result;

            if (!await WaitWaferFeederYMoveDone(timeoutMs))
                return RaiseFeederAlarm("WF-RETURN-Y-TIMEOUT", "WaferFeeder cassette unload position timeout.");

            result = await SetWaferFeederClamp(false);
            if (result != 0) return result;

            result = await SetWaferFeederUpDown(false);
            if (result != 0) return result;

            if (!await WaitWaferFeederUnclamp(timeoutMs))
                return RaiseFeederAlarm("WF-RETURN-UNCLAMP-TIMEOUT", "WaferFeeder unclamp sensor timeout.");

            return 0;
        }

        public async Task<int> RecoverWaferFeederToSafeState(int timeoutMs, bool unclamp = true)
        {
            int result;
            if (unclamp)
            {
                result = await SetWaferFeederClamp(false);
                if (result != 0) return result;
            }

            result = await SetWaferFeederUpDown(false);
            if (result != 0) return result;

            result = await MoveToWaferFeederAvoidPosition();
            if (result != 0)
                return result;

            if (!await WaitWaferFeederYMoveDone(timeoutMs))
                return RaiseFeederAlarm("WF-RECOVER-TIMEOUT", "WaferFeeder avoid position timeout.");

            return 0;
        }

        public bool CheckWaferFeederMoveReady()
        {
            return !FeederY.IsAlarm && !WaferFeederOverloadSensor.IsOn;
        }

        public bool CheckWaferFeederYMoveReady()
        {
            try
            {
                return FeederY != null &&
                       FeederY.IsServoOn &&
                       !FeederY.IsAlarm &&
                       !FeederY.IsMoving &&
                       !WaferFeederOverloadSensor.IsOn;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public bool CheckWaferFeederTransferReady(TransferMode mode)
        {
            if (!CheckWaferFeederMoveReady())
                return false;
            if (mode == TransferMode.Load)
                return IsWaferFeederDown() && IsWaferFeederUnclamp();
            if (mode == TransferMode.Unload)
                return HasWaferOnFeeder();
            return true;
        }

        public bool CheckWaferCassetteLoadReady(int slotIndex, TransferMode mode)
        {
            if (slotIndex < 0)
                return false;
            return CheckWaferFeederTransferReady(mode) && ValidateWaferFeederTeachingComplete();
        }

        public WaferFeederProcessState GetWaferFeederProcessState()
        {
            if (FeederY.IsAlarm || WaferFeederOverloadSensor.IsOn)
                return WaferFeederProcessState.Alarm;
            if (FeederY.IsMoving)
                return WaferFeederProcessState.Moving;
            if (HasWaferOnFeeder())
                return WaferFeederProcessState.HasWafer;
            return WaferFeederProcessState.Empty;
        }

        public bool HasWaferOnFeeder()
        {
            return WaferFeederRingCheckSensor.IsOn;
        }

        public bool IsWaferFeederSafe()
        {
            return !WaferFeederOverloadSensor.IsOn && !FeederY.IsAlarm;
        }

        public bool InterlockBeforeJog()
        {
            return IsWaferFeederSafe();
        }

        public bool ValidateWaferFeederBeforePickup(TransferPointType type)
        {
            if (!IsWaferFeederSafe())
                return false;
            if (type == TransferPointType.Cassette)
                return IsWaferFeederDown() || IsWaferFeederInCassetteLoadPosition(0);
            return true;
        }

        public void RecordWaferFeederPositionSnapshot(string key)
        {
            positionSnapshots[key] = FeederY.ActualPosition;
        }

        public string GetWaferFeederTransferState()
        {
            try
            {
                return "Up=" + IsWaferFeederUp()
                    + ", Down=" + IsWaferFeederDown()
                    + ", Unclamp=" + IsWaferFeederUnclamp()
                    + ", Ring=" + IsWaferFeederRingCheck()
                    + ", Overload=" + IsWaferFeederOverload()
                    + ", MoveReady=" + CheckWaferFeederYMoveReady();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "WF-STATE", "WaferFeeder state read failed: " + ex.Message);
                return "Unknown";
            }
            finally
            {
            }
        }

        public bool IsWaferFeederEmpty()
        {
            return IsWaferFeederRingDetected(false);
        }

        public bool IsWaferFeederOccupied()
        {
            return IsWaferFeederRingDetected(true);
        }

        public async Task<int> MoveToExchangePositionAsync()
        {
            int result = await SetWaferFeederUpDown(true);
            if (result != 0)
            {
                Console.WriteLine("[ALARM] '" + Name + "' MoveToExchangePosition: feeder up/down timeout.");
                return result;
            }

            result = await SetWaferFeederClamp(true);
            if (result != 0)
            {
                Console.WriteLine("[ALARM] '" + Name + "' MoveToExchangePosition: feeder clamp timeout.");
                return result;
            }

            bool clamped = await WaitWaferFeederClamp(Recipe.CylinderTimeoutMs);
            if (!clamped)
            {
                await SetWaferFeederClamp(false);
                await SetWaferFeederUpDown(false);
                Console.WriteLine("[ALARM] '" + Name + "' MoveToExchangePosition: wafer clamp sensor timeout.");
                return RaiseFeederAlarm("WF-EX-CLAMP-TIMEOUT", "WaferFeeder exchange clamp sensor timeout.");
            }

            result = await MoveToWaferFeederExchangePositionAsync();
            if (result != 0)
                return result;

            if (FeederY.IsAlarm)
                return RaiseFeederAlarm("WF-EX-Y-ALARM", "WaferFeederY alarm after exchange move.");

            return 0;
        }

        public async Task<int> RetractFeederAsync()
        {
            int result = await SetWaferFeederClamp(false);
            if (result != 0)
            {
                Console.WriteLine("[ALARM] '" + Name + "' RetractFeeder: feeder unclamp timeout.");
                return result;
            }

            bool released = await WaitWaferFeederUnclamp(Recipe.CylinderTimeoutMs);
            if (!released)
            {
                Console.WriteLine("[ALARM] '" + Name + "' RetractFeeder: wafer clamp sensor did not turn OFF.");
                return RaiseFeederAlarm("WF-RET-UNCLAMP-TIMEOUT", "WaferFeeder retract unclamp sensor timeout.");
            }

            result = await MoveToWaferFeederAvoidPosition();
            if (result != 0)
                return result;

            if (FeederY.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' RetractFeeder: FeederY origin move failed.");
                return RaiseFeederAlarm("WF-RET-Y-ALARM", "WaferFeederY alarm after retract move.");
            }

            result = await SetWaferFeederUpDown(false);
            if (result != 0)
            {
                Console.WriteLine("[ALARM] '" + Name + "' RetractFeeder: feeder down timeout.");
                return result;
            }

            return 0;
        }

        private bool ValidateWaferFeederYTargetPosition(double targetPos)
        {
            try
            {
                if (FeederY == null || FeederY.Setup == null) return false;
                if (!FeederY.Setup.SoftLimitEnabled) return true;
                return targetPos <= FeederY.Setup.SoftLimitPlus && targetPos >= FeederY.Setup.SoftLimitMinus;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "WF-Y-VALIDATE", "WaferFeederY target validate failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private void SetExclusiveOutput(BaseDigitalOutput target, BaseDigitalOutput opposite, bool on, string code)
        {
            try
            {
                if (target == null) return;
                if (on && opposite != null)
                    opposite.Off();

                if (on)
                    target.On();
                else
                    target.Off();

                EventLogger.Write(EventKind.Event, "QMC", code, target.Name + "=" + (on ? "ON" : "OFF"));
            }
            catch (Exception ex)
            {
                RaiseFeederAlarm(code, "WaferFeeder output command failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int RaiseFeederAlarm(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Alarm, "QMC", code, message);
                AlarmManager.Raise(AlarmSeverity.Error, code, Name, message);
            }
            catch
            {
            }
            finally
            {
            }

            return -1;
        }

        private double GetTeachingPosition(string positionName)
        {
            try
            {
                string key = NormalizeTeachingPositionName(positionName);
                if (string.Equals(key, "Avoid", StringComparison.OrdinalIgnoreCase)) return Setup.AvoidPosition;
                if (string.Equals(key, "CassetteLoad", StringComparison.OrdinalIgnoreCase)) return Setup.CassetteLoadPosition;
                if (string.Equals(key, "CassetteUnload", StringComparison.OrdinalIgnoreCase)) return Setup.CassetteUnloadPosition;
                if (string.Equals(key, "CassetteExchange", StringComparison.OrdinalIgnoreCase)) return Setup.CassetteExchangePosition;
                if (string.Equals(key, "StageLoad", StringComparison.OrdinalIgnoreCase)) return Setup.StageLoadPosition;
                if (string.Equals(key, "StageBarcode", StringComparison.OrdinalIgnoreCase)) return Setup.StageBarcodePosition;
                if (string.Equals(key, "StageUnload", StringComparison.OrdinalIgnoreCase)) return Setup.StageUnloadPosition;

                if (string.Equals(key, "Exchange", StringComparison.OrdinalIgnoreCase)) return Setup.ExchangePositionY;
                if (string.Equals(key, "LoadReady", StringComparison.OrdinalIgnoreCase)) return Setup.LoadReadyPosition;
                if (string.Equals(key, "CassetteLoadBase", StringComparison.OrdinalIgnoreCase)) return Setup.CassetteLoadBasePosition;
                if (string.Equals(key, "WaferPickup", StringComparison.OrdinalIgnoreCase)) return Setup.WaferPickupPosition;
                if (string.Equals(key, "StagePut", StringComparison.OrdinalIgnoreCase)) return Setup.StagePutPosition;
                if (string.Equals(key, "StagePick", StringComparison.OrdinalIgnoreCase)) return Setup.StagePickPosition;
            }
            catch
            {
                throw;
            }
            finally
            {
            }

            throw new ArgumentException("Unknown WaferFeederY teaching position: " + positionName, "positionName");
        }

        private void SetTeachingPosition(string positionName, double position)
        {
            try
            {
                string key = NormalizeTeachingPositionName(positionName);
                if (string.Equals(key, "Avoid", StringComparison.OrdinalIgnoreCase)) Setup.AvoidPosition = position;
                else if (string.Equals(key, "CassetteLoad", StringComparison.OrdinalIgnoreCase)) Setup.CassetteLoadPosition = position;
                else if (string.Equals(key, "CassetteUnload", StringComparison.OrdinalIgnoreCase)) Setup.CassetteUnloadPosition = position;
                else if (string.Equals(key, "CassetteExchange", StringComparison.OrdinalIgnoreCase)) Setup.CassetteExchangePosition = position;
                else if (string.Equals(key, "StageLoad", StringComparison.OrdinalIgnoreCase)) Setup.StageLoadPosition = position;
                else if (string.Equals(key, "StageBarcode", StringComparison.OrdinalIgnoreCase)) Setup.StageBarcodePosition = position;
                else if (string.Equals(key, "StageUnload", StringComparison.OrdinalIgnoreCase)) Setup.StageUnloadPosition = position;
                else if (string.Equals(key, "Exchange", StringComparison.OrdinalIgnoreCase)) Setup.ExchangePositionY = position;
                else if (string.Equals(key, "LoadReady", StringComparison.OrdinalIgnoreCase)) Setup.LoadReadyPosition = position;
                else if (string.Equals(key, "CassetteLoadBase", StringComparison.OrdinalIgnoreCase)) Setup.CassetteLoadBasePosition = position;
                else if (string.Equals(key, "WaferPickup", StringComparison.OrdinalIgnoreCase)) Setup.WaferPickupPosition = position;
                else if (string.Equals(key, "StagePut", StringComparison.OrdinalIgnoreCase)) Setup.StagePutPosition = position;
                else if (string.Equals(key, "StagePick", StringComparison.OrdinalIgnoreCase)) Setup.StagePickPosition = position;
                else throw new ArgumentException("Unknown WaferFeederY teaching position: " + positionName, "positionName");
            }
            finally
            {
            }
        }

        private string NormalizeTeachingPositionName(string positionName)
        {
            try
            {
                string key = positionName ?? string.Empty;
                key = key.Replace("1_WaferFeederY.", string.Empty);
                key = key.Replace("WaferFeederY.", string.Empty);
                key = key.Replace("Position", string.Empty);
                key = key.Replace("Pos", string.Empty);
                key = key.Replace("Casstte", "Cassette");
                key = key.Trim();
                return key;
            }
            catch
            {
                return positionName ?? string.Empty;
            }
            finally
            {
            }
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs)
        {
            int elapsed = 0;
            while (timeoutMs <= 0 || elapsed < timeoutMs)
            {
                if (condition())
                    return true;

                await Task.Delay(10).ContinueWith(_ => { });
                elapsed += 10;
            }
            return condition();
        }
    }
}
