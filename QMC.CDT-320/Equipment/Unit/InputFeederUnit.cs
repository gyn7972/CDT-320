using QMC.CDT320.Ajin;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace QMC.CDT320
{
    [DataContract]
    public class InputFeederSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            IsSimulationMode = false;
        }
    }

    [DataContract]
    public class InputFeederConfig : IConfigData
    {
        [DataMember] public bool bDryRun { get; set; }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            bDryRun = false;
        }
    }

    [DataContract]
    public class InputFeederRecipe : IRecipeData
    {
        [DataMember] public double AvoidPosition { get; set; }
        [DataMember] public double CassetteLoadPosition { get; set; }
        [DataMember] public double CassetteUnloadPosition { get; set; }
        [DataMember] public double CassetteExchangePosition { get; set; }
        [DataMember] public double WaferLoadAvoidPosition { get; set; }
        [DataMember] public double WaferLoadPosition { get; set; }
        [DataMember] public double WaferUnloadAvoidPosition { get; set; }
        [DataMember] public double WaferUnloadPosition { get; set; }
        [DataMember] public double WaferBarcodePosition { get; set; }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            AvoidPosition = 0.0;
            CassetteLoadPosition = 0.0;
            CassetteUnloadPosition = 0.0;
            CassetteExchangePosition = 0.0;
            WaferLoadAvoidPosition = 0.0;
            WaferLoadPosition = 0.0;
            WaferUnloadAvoidPosition = 0.0;
            WaferUnloadPosition = 0.0;
            WaferBarcodePosition = 0.0;
        }
    }

    public enum WaferFeederPositionType
    {
        Avoid,
        CassetteLoad,
        CassetteUnload,
        CassetteExchange,
        WaferLoadAvoid,
        WaferLoad,
        WaferUnloadAvoid,
        WaferUnload,
        WaferBarcode
    }

    public class InputFeederUnit : BaseUnit<InputFeederSetup, InputFeederConfig, InputFeederRecipe>, IUnitJogController
    {
        private readonly Dictionary<string, double> positionSnapshots = new Dictionary<string, double>();

        public MaterialState CurrentMaterialState { get; private set; }
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
            CurrentMaterialState = MaterialState.Empty;
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

        public bool CanHandleJogAxis(BaseAxis axis)
        {
            return axis != null && ReferenceEquals(axis, FeederY);
        }

        public Task<int> JogStepAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed,
            double axisStepDistance)
        {
            if (!CanHandleJogAxis(axis))
                return Task.FromResult(-1);

            double signedDistance = (direction < 0 ? -1.0 : 1.0) * Math.Abs(axisStepDistance);
            double target = FeederY.ActualPosition + signedDistance;
            return MoveWaferFeederY(target, speedType == JogSpeedType.Fine);
        }

        public Task<int> JogContinuousAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed)
        {
            if (!CanHandleJogAxis(axis))
                return Task.FromResult(-1);

            double speed = UnitJogVelocityResolver.Resolve(axis, speedType, customSpeed);
            ManualMoveWaferFeederYJog(direction, speed);
            return Task.FromResult(0);
        }

        public Task<int> StopJogAsync(BaseAxis axis)
        {
            if (!CanHandleJogAxis(axis))
                return Task.FromResult(-1);

            ManualStopWaferFeederY();
            return Task.FromResult(0);
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
                int result = await FeederY.MoveAbsoluteAsync(targetPos, ResolveWaferFeederYMoveVelocity(bFine));
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
            return MoveWaferFeederY(Recipe.AvoidPosition, bFine);
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
            return MoveWaferFeederY(Recipe.WaferBarcodePosition, bFine);
        }

        public Task<int> MoveToWaferFeederStageLoadPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Recipe.WaferLoadPosition, bFine);
        }

        public Task<int> MoveToWaferFeederStageUnloadPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Recipe.WaferUnloadPosition, bFine);
        }

        public Task<int> MoveToWaferFeederExchangePosition(bool bFine = false)
        {
            return MoveWaferFeederY(Recipe.CassetteExchangePosition, bFine);
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
            return MoveWaferFeederYAsync(Recipe.WaferBarcodePosition, bFine);
        }

        public Task<int> MoveToWaferFeederStageLoadPositionAsync(bool bFine = false)
        {
            return MoveWaferFeederYAsync(Recipe.WaferLoadPosition, bFine);
        }

        public Task<int> MoveToWaferFeederStageUnloadPositionAsync(bool bFine = false)
        {
            return MoveWaferFeederYAsync(Recipe.WaferUnloadPosition, bFine);
        }

        public Task<int> MoveToWaferFeederExchangePositionAsync(bool bFine = false)
        {
            return MoveWaferFeederYAsync(Recipe.CassetteExchangePosition, bFine);
        }

        public async Task<bool> WaitWaferFeederYMoveDone(int timeoutMs)
        {
            return await WaitUntilAsync(() => !FeederY.IsMoving && FeederY.IsInPosition && !FeederY.IsAlarm, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederYInPosition(string positionName, int timeoutMs)
        {
            double target = GetTeachingPosition(positionName);
            return await WaitUntilAsync(() => IsWaferFeederYInPosition(target, ResolveWaferFeederYInPositionTolerance()), timeoutMs);
        }

        public bool IsWaferFeederYInPosition(double targetPos, double tolerance)
        {
            return Math.Abs(FeederY.ActualPosition - targetPos) <= tolerance;
        }

        public bool IsWaferFeederInAvoidPosition()
        {
            return IsWaferFeederYInPosition(Recipe.AvoidPosition, ResolveWaferFeederYInPositionTolerance());
        }

        public bool IsWaferFeederYInAvoidPosition()
        {
            return IsWaferFeederInAvoidPosition();
        }

        public bool IsWaferFeederInCassetteLoadPosition(int slotIndex)
        {
            return IsWaferFeederYInPosition(CalculateWaferFeederCassetteLoadPosition(slotIndex), ResolveWaferFeederYInPositionTolerance());
        }

        public bool IsWaferFeederYInCassetteLoadPosition()
        {
            return IsWaferFeederYInPosition(Recipe.CassetteLoadPosition, ResolveWaferFeederYInPositionTolerance());
        }

        public bool IsWaferFeederInCassetteUnloadPosition(int slotIndex)
        {
            return IsWaferFeederYInPosition(CalculateWaferFeederCassetteUnloadPosition(slotIndex), ResolveWaferFeederYInPositionTolerance());
        }

        public bool IsWaferFeederYInCassetteUnloadPosition()
        {
            return IsWaferFeederYInPosition(Recipe.CassetteUnloadPosition, ResolveWaferFeederYInPositionTolerance());
        }

        public bool IsWaferFeederInStageLoadPosition()
        {
            return IsWaferFeederYInPosition(Recipe.WaferLoadPosition, ResolveWaferFeederYInPositionTolerance());
        }

        public bool IsWaferFeederYInStageLoadPosition()
        {
            return IsWaferFeederInStageLoadPosition();
        }

        public bool IsWaferFeederInStageUnloadPosition()
        {
            return IsWaferFeederYInPosition(Recipe.WaferUnloadPosition, ResolveWaferFeederYInPositionTolerance());
        }

        public bool IsWaferFeederYInStageUnloadPosition()
        {
            return IsWaferFeederInStageUnloadPosition();
        }

        public bool IsWaferFeederInBarcodePosition()
        {
            return IsWaferFeederYInPosition(Recipe.WaferBarcodePosition, ResolveWaferFeederYInPositionTolerance());
        }

        public bool IsWaferFeederYInBarcodePosition()
        {
            return IsWaferFeederInBarcodePosition();
        }

        public bool IsWaferFeederInExchangePosition()
        {
            return IsWaferFeederYInPosition(Recipe.CassetteExchangePosition, ResolveWaferFeederYInPositionTolerance());
        }

        public bool IsWaferFeederYInExchangePosition()
        {
            return IsWaferFeederInExchangePosition();
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

        public bool IsWaferStageRingDetected(int size, bool expected = true)
        {
            try
            {
                BaseDigitalInput sensor = size <= 8 ? WaferStage8RingCheckSensor : WaferStage12RingCheckSensor;
                return sensor != null && sensor.IsOn == expected;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public void TeachWaferFeederYPosition(string positionName)
        {
            SetTeachingPosition(positionName, FeederY.ActualPosition);
            EventLogger.Write(EventKind.Event, "QMC", "WF-TEACH", "Teaching saved: " + positionName + "=" + FeederY.ActualPosition);
        }

        public void TeachWaferFeederAvoidPosition()
        {
            Recipe.AvoidPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederYAvoidPosition()
        {
            TeachWaferFeederAvoidPosition();
        }

        public void TeachWaferFeederYCassetteLoadPosition()
        {
            Recipe.CassetteLoadPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederYCassetteUnloadPosition()
        {
            Recipe.CassetteUnloadPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederYStageLoadPosition()
        {
            Recipe.WaferLoadPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederYStageUnloadPosition()
        {
            Recipe.WaferUnloadPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederYBarcodePosition()
        {
            Recipe.WaferBarcodePosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederYExchangePosition()
        {
            Recipe.CassetteExchangePosition = FeederY.ActualPosition;
        }

        public double CalculateWaferFeederCassetteLoadPosition(int slotIndex)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException("slotIndex");
            return Recipe.CassetteLoadPosition;
        }

        public double CalculateWaferFeederCassetteUnloadPosition(int slotIndex)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException("slotIndex");
            return Recipe.CassetteUnloadPosition;
        }

        public bool ValidateWaferFeederTeachingComplete()
        {
            return Recipe.CassetteLoadPosition != Recipe.AvoidPosition &&
                   Recipe.CassetteUnloadPosition != Recipe.AvoidPosition &&
                   Recipe.WaferLoadPosition != Recipe.WaferUnloadPosition;
        }

        public bool ValidateWaferFeederYTeachingComplete()
        {
            return ValidateWaferFeederTeachingComplete();
        }

        public double GetWaferFeederYTeachingPosition(string positionName)
        {
            return GetTeachingPosition(positionName);
        }

        public async Task<int> MoveToTeachingPositionAndVerify(string positionName, bool bFine = false)
        {
            int result = await MoveWaferFeederYToTeachingPosition(positionName, bFine);
            if (result != 0)
                return result;

            if (!await WaitWaferFeederYInPosition(positionName, ResolveWaferFeederYMoveTimeoutMs()))
                return RaiseFeederAlarm("WF-TEACH-INPOS", "WaferFeederY teaching position timeout: " + positionName);

            return 0;
        }

        public Task<int> SetWaferFeederUpDown(bool up)
        {
            return SetWaferFeederUpDownAsync(up, ResolveLiftTimeoutMs(up));
        }

        public Task<int> WaferFeederLiftUp(int timeoutMs)
        {
            return SetWaferFeederUpDownAsync(true, timeoutMs);
        }

        public Task<int> WaferFeederLiftDown(int timeoutMs)
        {
            return SetWaferFeederUpDownAsync(false, timeoutMs);
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
            return SetWaferFeederClampAsync(clamp, ResolveClampTimeoutMs(clamp));
        }

        public Task<int> WaferFeederClamp(int timeoutMs)
        {
            return SetWaferFeederClampAsync(true, timeoutMs);
        }

        public Task<int> WaferFeederUnclamp(int timeoutMs)
        {
            return SetWaferFeederClampAsync(false, timeoutMs);
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

        public async Task<bool> WaitWaferFeederUnclamped(int timeoutMs)
        {
            return await WaitWaferFeederUnclamp(timeoutMs);
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

        public void ManualMoveWaferFeederYJog(Direction dir, double speed)
        {
            FeederY.MoveJogContinuous((int)dir, JogSpeedType.Custom, speed);
        }

        public void ManualMoveWaferFeederYJog(int direction, double speed)
        {
            ManualMoveWaferFeederYJog(direction < 0 ? Direction.Minus : Direction.Plus, speed);
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
            return await LoadWaferFromCassetteToFeeder(slotIndex, timeoutMs, bFine, false);
        }

        public async Task<int> LoadWaferFromCassetteToFeeder(int slotIndex, int timeoutMs, bool bFine = false, bool useBarcode = false)
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

            if (!IsWaferFeederRingDetected(true))
                return RaiseFeederAlarm("WF-LOAD-RING", "WaferFeeder ring was not detected after cassette load.");

            UpdateWaferFeederMaterialState(MaterialState.Occupied);

            if (useBarcode)
            {
                string barcode = await ReadWaferFeederBarcode(timeoutMs, bFine, 2);
                if (string.IsNullOrEmpty(barcode))
                    return RaiseFeederAlarm("WF-BARCODE", "WaferFeeder barcode read failed.");
            }

            return 0;
        }

        public async Task<string> ReadWaferFeederBarcode(int timeoutMs, bool bFine = false, int retry = 2)
        {
            if (!HasWaferOnFeeder())
            {
                RaiseFeederAlarm("WF-BARCODE-EMPTY", "WaferFeeder has no wafer for barcode read.");
                return string.Empty;
            }

            int result = await MoveToWaferFeederBarcodePosition(bFine);
            if (result != 0)
                return string.Empty;

            if (!await WaitWaferFeederYMoveDone(timeoutMs))
            {
                RaiseFeederAlarm("WF-BARCODE-TIMEOUT", "WaferFeeder barcode position timeout.");
                return string.Empty;
            }

            return string.Empty;
        }

        public async Task<int> LoadWaferFromFeederToStage(int size, int timeoutMs, bool bFine = false, bool useVacuum = true)
        {
            if (!CheckWaferStageReady(size, TransferMode.Load))
                return RaiseFeederAlarm("WF-STAGE-LOAD-READY", "WaferStage load transfer is not ready.");

            int result = await MoveToWaferFeederStageLoadPosition(bFine);
            if (result != 0)
                return result;

            if (!await WaitWaferFeederYMoveDone(timeoutMs))
                return RaiseFeederAlarm("WF-STAGE-LOAD-Y-TIMEOUT", "WaferFeeder stage load position timeout.");

            result = await WaferFeederUnclamp(timeoutMs);
            if (result != 0)
                return result;

            result = await WaferFeederLiftDown(timeoutMs);
            if (result != 0)
                return result;

            if (!await WaitWaferFeederRingState(false, timeoutMs))
                return RaiseFeederAlarm("WF-STAGE-LOAD-RING", "WaferFeeder ring remained after stage load.");

            ClearWaferFeederMaterialState();
            return 0;
        }

        public async Task<int> UnloadWaferFromStageToFeeder(int size, int timeoutMs, bool bFine = false)
        {
            if (!CheckWaferStageReady(size, TransferMode.Unload))
                return RaiseFeederAlarm("WF-STAGE-UNLOAD-READY", "WaferStage unload transfer is not ready.");

            int result = await MoveToWaferFeederStageUnloadPosition(bFine);
            if (result != 0)
                return result;

            if (!await WaitWaferFeederYMoveDone(timeoutMs))
                return RaiseFeederAlarm("WF-STAGE-UNLOAD-Y-TIMEOUT", "WaferFeeder stage unload position timeout.");

            result = await WaferFeederLiftUp(timeoutMs);
            if (result != 0)
                return result;

            result = await WaferFeederClamp(timeoutMs);
            if (result != 0)
                return result;

            if (!await WaitWaferFeederRingState(true, timeoutMs))
                return RaiseFeederAlarm("WF-STAGE-UNLOAD-RING", "WaferFeeder ring was not detected after stage unload.");

            UpdateWaferFeederMaterialState(MaterialState.Occupied);
            return 0;
        }

        public async Task<int> UnloadWaferFromFeederToCassette(int slotIndex, int timeoutMs, bool bFine = false)
        {
            if (!CheckWaferCassetteReady(slotIndex, TransferMode.Unload))
                return RaiseFeederAlarm("WF-CST-UNLOAD-READY", "WaferCassette unload transfer is not ready.");

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

            if (!await WaitWaferFeederRingState(false, timeoutMs))
                return RaiseFeederAlarm("WF-RETURN-RING", "WaferFeeder ring remained after cassette unload.");

            ClearWaferFeederMaterialState();
            return 0;
        }

        public async Task<int> ReturnWaferFromFeederToCassette(int slotIndex, int timeoutMs, bool bFine = false)
        {
            return await UnloadWaferFromFeederToCassette(slotIndex, timeoutMs, bFine);
        }

        public async Task<int> ExchangeWaferFeederRingForNextSlot(int currentSlotIndex, int nextSlotIndex, int timeoutMs, bool bFine = false)
        {
            int result = await UnloadWaferFromFeederToCassette(currentSlotIndex, timeoutMs, bFine);
            if (result != 0)
                return result;

            result = await LoadWaferFromCassetteToFeeder(nextSlotIndex, timeoutMs, bFine, false);
            if (result != 0)
                return result;

            return await MoveToWaferFeederExchangePosition(bFine);
        }

        public async Task<int> RecoverWaferFeederToSafeState(int timeoutMs, bool moveAvoid = true)
        {
            int result;
            result = await SetWaferFeederClamp(false);
            if (result != 0) return result;

            result = await SetWaferFeederUpDown(false);
            if (result != 0) return result;

            if (moveAvoid)
            {
                result = await MoveToWaferFeederAvoidPosition();
                if (result != 0)
                    return result;

                if (!await WaitWaferFeederYMoveDone(timeoutMs))
                    return RaiseFeederAlarm("WF-RECOVER-TIMEOUT", "WaferFeeder avoid position timeout.");
            }

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
                return IsWaferFeederDown() && IsWaferFeederUnclamp() && IsWaferFeederEmpty();
            if (mode == TransferMode.Unload)
                return HasWaferOnFeeder();
            return true;
        }

        public bool CheckWaferCassetteReady(int slotIndex, TransferMode mode)
        {
            if (slotIndex < 0)
                return false;
            return CheckWaferFeederTransferReady(mode) && ValidateWaferFeederYTeachingComplete();
        }

        public bool CheckWaferStageReady(int size, TransferMode mode)
        {
            if (size != 8 && size != 12)
                return false;
            if (mode == TransferMode.Load)
                return IsWaferFeederOccupied() && IsWaferStageRingDetected(size, false);
            if (mode == TransferMode.Unload)
                return IsWaferFeederEmpty() && IsWaferStageRingDetected(size, true);
            return CheckWaferFeederMoveReady();
        }

        public bool ValidateWaferFeederPosition(FeederPositionType type)
        {
            try
            {
                double position = GetWaferFeederYTeachingPosition(ResolveWaferFeederTeachingPositionName(type));
                return ValidateWaferFeederYTargetPosition(position);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public string ResolveWaferFeederTeachingPositionName(FeederPositionType type)
        {
            switch (type)
            {
                case FeederPositionType.Avoid:
                    return "1_WaferFeederY.AvoidPos";
                case FeederPositionType.CassetteLoad:
                    return "1_WaferFeederY.CassetteLoadPos";
                case FeederPositionType.CassetteUnload:
                    return "1_WaferFeederY.CassetteUnloadPos";
                case FeederPositionType.Barcode:
                    return "1_WaferFeederY.StageBarcodePos";
                case FeederPositionType.StageLoad:
                    return "1_WaferFeederY.StageLoadPos";
                case FeederPositionType.StageUnload:
                    return "1_WaferFeederY.StageUnloadPos";
                case FeederPositionType.Exchange:
                    return "1_WaferFeederY.CassetteExchangePos";
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
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

            bool clamped = await WaitWaferFeederClamp(ResolveClampTimeoutMs(true));
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

            bool released = await WaitWaferFeederUnclamp(ResolveClampTimeoutMs(false));
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

        public void StopWaferFeederMotionAndOutputs(string reason)
        {
            try
            {
                try { FeederY?.StopJog(); } catch { }
                try { FeederY?.Stop(); } catch { }
                SetWaferFeederOutputsSafe(FeederSafePolicy.AllOff);
                EventLogger.Write(EventKind.Event, "QMC", "WF-STOP", "WaferFeeder stopped. reason=" + reason);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "WF-STOP", "WaferFeeder stop failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public void SetWaferFeederOutputsSafe(FeederSafePolicy policy)
        {
            try
            {
                switch (policy)
                {
                    case FeederSafePolicy.HoldClamp:
                        SetWaferFeederLiftUpOutput(false);
                        SetWaferFeederLiftDownOutput(true);
                        break;
                    case FeederSafePolicy.HoldCurrent:
                        break;
                    default:
                        SetWaferFeederLiftUpOutput(false);
                        SetWaferFeederLiftDownOutput(false);
                        SetWaferFeederClampOutput(false);
                        SetWaferFeederUnclampOutput(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                RaiseFeederAlarm("WF-SAFE-OUTPUT", "WaferFeeder safe output command failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public string BuildWaferFeederAlarmMessage(FeederAlarmCode code)
        {
            switch (code)
            {
                case FeederAlarmCode.AxisAlarm:
                    return "WaferFeederY axis alarm.";
                case FeederAlarmCode.MoveTimeout:
                    return "WaferFeederY move timeout.";
                case FeederAlarmCode.TeachingMissing:
                    return "WaferFeeder teaching position is missing.";
                case FeederAlarmCode.Interlock:
                    return "WaferFeeder interlock condition is not satisfied.";
                case FeederAlarmCode.Overload:
                    return "WaferFeeder overload input is on.";
                case FeederAlarmCode.RingMissing:
                    return "WaferFeeder ring was not detected.";
                default:
                    return "WaferFeeder alarm.";
            }
        }

        public void UpdateWaferFeederMaterialState(MaterialState state)
        {
            try
            {
                CurrentMaterialState = state;
                EventLogger.Write(EventKind.Event, "QMC", "WF-MATERIAL", "WaferFeeder material state=" + state);
            }
            catch
            {
            }
            finally
            {
            }
        }

        public void ClearWaferFeederMaterialState()
        {
            UpdateWaferFeederMaterialState(MaterialState.Empty);
        }

        private double ResolveWaferFeederYMoveVelocity(bool bFine)
        {
            try
            {
                if (FeederY == null || FeederY.Config == null)
                    return 0.0;

                if (bFine && FeederY.Config.JogFineVelocity > 0.0)
                    return FeederY.Config.JogFineVelocity;

                return FeederY.Config.DefaultVelocity;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private int ResolveWaferFeederYMoveTimeoutMs()
        {
            try
            {
                if (FeederY != null && FeederY.Setup != null && FeederY.Setup.MoveTimeoutMs > 0)
                    return FeederY.Setup.MoveTimeoutMs;
            }
            catch
            {
            }
            finally
            {
            }

            return 60000;
        }

        private double ResolveWaferFeederYInPositionTolerance()
        {
            try
            {
                if (FeederY != null && FeederY.Config != null && FeederY.Config.InPositionTolerance >= 0.0)
                    return FeederY.Config.InPositionTolerance;
            }
            catch
            {
            }
            finally
            {
            }

            return 0.05;
        }

        private int ResolveLiftTimeoutMs(bool up)
        {
            try
            {
                if (InputFeederLift != null && InputFeederLift.Recipe != null)
                {
                    int timeoutMs = up ? InputFeederLift.Recipe.FwdTimeoutMs : InputFeederLift.Recipe.BwdTimeoutMs;
                    if (timeoutMs > 0)
                        return timeoutMs;
                }
            }
            catch
            {
            }
            finally
            {
            }

            return 3000;
        }

        private int ResolveClampTimeoutMs(bool clamp)
        {
            try
            {
                if (InputFeederClamp != null && InputFeederClamp.Recipe != null)
                {
                    int timeoutMs = clamp ? InputFeederClamp.Recipe.FwdTimeoutMs : InputFeederClamp.Recipe.BwdTimeoutMs;
                    if (timeoutMs > 0)
                        return timeoutMs;
                }
            }
            catch
            {
            }
            finally
            {
            }

            return 3000;
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
                if (string.Equals(key, "Avoid", StringComparison.OrdinalIgnoreCase)) return Recipe.AvoidPosition;
                if (string.Equals(key, "CassetteLoad", StringComparison.OrdinalIgnoreCase)) return Recipe.CassetteLoadPosition;
                if (string.Equals(key, "CassetteUnload", StringComparison.OrdinalIgnoreCase)) return Recipe.CassetteUnloadPosition;
                if (string.Equals(key, "CassetteExchange", StringComparison.OrdinalIgnoreCase)) return Recipe.CassetteExchangePosition;
                if (string.Equals(key, "WaferLoadAvoid", StringComparison.OrdinalIgnoreCase)) return Recipe.WaferLoadAvoidPosition;
                if (string.Equals(key, "WaferLoad", StringComparison.OrdinalIgnoreCase)) return Recipe.WaferLoadPosition;
                if (string.Equals(key, "WaferUnloadAvoid", StringComparison.OrdinalIgnoreCase)) return Recipe.WaferUnloadAvoidPosition;
                if (string.Equals(key, "WaferUnload", StringComparison.OrdinalIgnoreCase)) return Recipe.WaferUnloadPosition;
                if (string.Equals(key, "WaferBarcode", StringComparison.OrdinalIgnoreCase)) return Recipe.WaferBarcodePosition;

                if (string.Equals(key, "StageLoad", StringComparison.OrdinalIgnoreCase)) return Recipe.WaferLoadPosition;
                if (string.Equals(key, "StageBarcode", StringComparison.OrdinalIgnoreCase)) return Recipe.WaferBarcodePosition;
                if (string.Equals(key, "StageUnload", StringComparison.OrdinalIgnoreCase)) return Recipe.WaferUnloadPosition;
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
                if (string.Equals(key, "Avoid", StringComparison.OrdinalIgnoreCase)) Recipe.AvoidPosition = position;
                else if (string.Equals(key, "CassetteLoad", StringComparison.OrdinalIgnoreCase)) Recipe.CassetteLoadPosition = position;
                else if (string.Equals(key, "CassetteUnload", StringComparison.OrdinalIgnoreCase)) Recipe.CassetteUnloadPosition = position;
                else if (string.Equals(key, "CassetteExchange", StringComparison.OrdinalIgnoreCase)) Recipe.CassetteExchangePosition = position;
                else if (string.Equals(key, "WaferLoadAvoid", StringComparison.OrdinalIgnoreCase)) Recipe.WaferLoadAvoidPosition = position;
                else if (string.Equals(key, "WaferLoad", StringComparison.OrdinalIgnoreCase)) Recipe.WaferLoadPosition = position;
                else if (string.Equals(key, "WaferUnloadAvoid", StringComparison.OrdinalIgnoreCase)) Recipe.WaferUnloadAvoidPosition = position;
                else if (string.Equals(key, "WaferUnload", StringComparison.OrdinalIgnoreCase)) Recipe.WaferUnloadPosition = position;
                else if (string.Equals(key, "WaferBarcode", StringComparison.OrdinalIgnoreCase)) Recipe.WaferBarcodePosition = position;
                else if (string.Equals(key, "StageLoad", StringComparison.OrdinalIgnoreCase)) Recipe.WaferLoadPosition = position;
                else if (string.Equals(key, "StageBarcode", StringComparison.OrdinalIgnoreCase)) Recipe.WaferBarcodePosition = position;
                else if (string.Equals(key, "StageUnload", StringComparison.OrdinalIgnoreCase)) Recipe.WaferUnloadPosition = position;
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
