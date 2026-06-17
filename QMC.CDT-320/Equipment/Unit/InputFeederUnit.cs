using QMC.CDT320.Ajin;
using QMC.CDT320.Interlocks;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;
using QMC.CDT320.Materials;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
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
        public WaferMaterial CurrentWaferMaterial { get; private set; }
        public string CurrentWaferId { get { return CurrentWaferMaterial != null ? CurrentWaferMaterial.WaferId : ""; } }
        public BaseAxis FeederY { get; private set; }

        public BaseDigitalInput WaferFeederUpSensor { get; private set; }
        public BaseDigitalInput WaferFeederDownSensor { get; private set; }
        public BaseDigitalInput WaferFeederClampSensor { get; private set; }
        public BaseDigitalInput WaferFeederRingCheckSensor { get; private set; }
        public BaseDigitalInput WaferFeederOverloadSensor { get; private set; }
        public BaseDigitalInput WaferFeeder8RingCheckSensor { get; private set; }
        public BaseDigitalInput WaferFeeder12RingCheckSensor { get; private set; }
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
            WaferFeederClampSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederUnClamp);
            WaferFeederRingCheckSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederRingCheck);
            WaferFeederOverloadSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederOverloadCheck);
            WaferFeeder8RingCheckSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeeder8RingCheck);
            WaferFeeder12RingCheckSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeeder12RingCheck);
            InputFeederLift = CylinderManager.Get(AjinIoCatalog.CylinderRefs.InputFeederLift);
            InputFeederClamp = CylinderManager.Get(AjinIoCatalog.CylinderRefs.InputFeederClamp);

            Components.Add(FeederY);
            Components.Add(WaferFeederUpSensor);
            Components.Add(WaferFeederDownSensor);
            Components.Add(WaferFeederClampSensor);
            Components.Add(WaferFeederRingCheckSensor);
            Components.Add(WaferFeederOverloadSensor);
            Components.Add(WaferFeeder8RingCheckSensor);
            Components.Add(WaferFeeder12RingCheckSensor);
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
                string readyReason;
                if (!CheckWaferFeederYMoveReady(out readyReason))
                    return RaiseFeederAlarm("WF-Y-READY", "InputFeederY is not ready to move. " + readyReason);

                if (!ValidateWaferFeederYTargetPosition(targetPos))
                    return RaiseFeederAlarm("WF-Y-SOFT-LIMIT", "InputFeederY target is out of soft limit. target=" + targetPos);

                if (IsWaferFeederYInPosition(targetPos, ResolveWaferFeederYInPositionTolerance()))
                {
                    EventLogger.Write(EventKind.Event, "QMC", "WF-Y-MOVE",
                        "InputFeederY already in position. target=" + targetPos + ". " + GetWaferFeederTransferState());
                    return 0;
                }

                EventLogger.Write(EventKind.Event, "QMC", "WF-Y-MOVE", "Move InputFeederY target=" + targetPos);
                int result = await FeederY.MoveAbsoluteAsync(targetPos, ResolveWaferFeederYMoveVelocity(bFine));
                if (result != 0 || FeederY.IsAlarm)
                    return RaiseFeederAlarm("WF-Y-MOVE", "InputFeederY move failed. result=" + result + ", alarm=" + FeederY.IsAlarm);

                AxisMoveWaitResult waitResult = await WaitWaferFeederYMoveDoneInPosition(targetPos, ResolveWaferFeederYMoveTimeoutMs()).ConfigureAwait(false);
                if (!waitResult.Success)
                    return RaiseFeederAlarm(
                        AxisMoveWaiter.ResolveAlarmCode("WF-Y-MOVE", waitResult),
                        "InputFeederY move/in-position wait failed. target=" + targetPos + ". " +
                        AxisMoveWaiter.FormatResult(waitResult, GetWaferFeederTransferState()));

                return 0;
            }
            catch (Exception ex)
            {
                return RaiseFeederAlarm("WF-Y-MOVE-EX", "InputFeederY move exception: " + ex.Message);
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
                double targetPosition = GetTeachingPosition(positionName);
                return await MoveWaferFeederYNamedPositionAsync(targetPosition, positionName, bFine);
            }
            catch (Exception ex)
            {
                return RaiseFeederAlarm("WF-TEACH-MOVE", "InputFeederY teaching move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public Task<int> MoveToWaferFeederAvoidPosition(bool bFine = false)
        {
            return MoveWaferFeederYNamedPositionAsync(Recipe.AvoidPosition, "InputFeederY.AvoidPosition", bFine);
        }

        public Task<int> MoveToWaferFeederCassetteLoadPosition(int slotIndex, bool bFine = false)
        {
            return MoveWaferFeederYNamedPositionAsync(
                CalculateWaferFeederCassetteLoadPosition(slotIndex),
                "InputFeederY.CassetteLoadPosition",
                bFine);
        }

        public Task<int> MoveToWaferFeederCassetteUnloadPosition(int slotIndex, bool bFine = false)
        {
            return MoveWaferFeederYNamedPositionAsync(
                CalculateWaferFeederCassetteUnloadPosition(slotIndex),
                "InputFeederY.CassetteUnloadPosition",
                bFine);
        }

        public Task<int> MoveToWaferFeederBarcodePosition(bool bFine = false)
        {
            return MoveWaferFeederYNamedPositionAsync(Recipe.WaferBarcodePosition, "InputFeederY.WaferBarcodePosition", bFine);
        }

        public Task<int> MoveToWaferFeederStageLoadPosition(bool bFine = false)
        {
            return MoveWaferFeederYNamedPositionAsync(Recipe.WaferLoadPosition, "InputFeederY.WaferLoadPosition", bFine);
        }

        public Task<int> MoveToWaferFeederStageLoadAvoidPosition(bool bFine = false)
        {
            return MoveWaferFeederYNamedPositionAsync(Recipe.WaferLoadAvoidPosition, "InputFeederY.WaferLoadAvoidPosition", bFine);
        }

        public Task<int> MoveToWaferFeederStageUnloadPosition(bool bFine = false)
        {
            return MoveWaferFeederYNamedPositionAsync(Recipe.WaferUnloadPosition, "InputFeederY.WaferUnloadPosition", bFine);
        }

        public Task<int> MoveToWaferFeederStageUnloadAvoidPosition(bool bFine = false)
        {
            return MoveWaferFeederYNamedPositionAsync(Recipe.WaferUnloadAvoidPosition, "InputFeederY.WaferUnloadAvoidPosition", bFine);
        }

        public Task<int> MoveToWaferFeederExchangePosition(bool bFine = false)
        {
            return MoveWaferFeederYNamedPositionAsync(Recipe.CassetteExchangePosition, "InputFeederY.CassetteExchangePosition", bFine);
        }

        public Task<int> MoveToWaferFeederCassetteLoadPositionAsync(int slotIndex, bool bFine = false)
        {
            return MoveToWaferFeederCassetteLoadPosition(slotIndex, bFine);
        }

        public Task<int> MoveToWaferFeederCassetteUnloadPositionAsync(int slotIndex, bool bFine = false)
        {
            return MoveToWaferFeederCassetteUnloadPosition(slotIndex, bFine);
        }

        public Task<int> MoveToWaferFeederBarcodePositionAsync(bool bFine = false)
        {
            return MoveToWaferFeederBarcodePosition(bFine);
        }

        public Task<int> MoveToWaferFeederStageLoadPositionAsync(bool bFine = false)
        {
            return MoveToWaferFeederStageLoadPosition(bFine);
        }

        public Task<int> MoveToWaferFeederStageLoadAvoidPositionAsync(bool bFine = false)
        {
            return MoveToWaferFeederStageLoadAvoidPosition(bFine);
        }

        public Task<int> MoveToWaferFeederStageUnloadPositionAsync(bool bFine = false)
        {
            return MoveToWaferFeederStageUnloadPosition(bFine);
        }

        public Task<int> MoveToWaferFeederStageUnloadAvoidPositionAsync(bool bFine = false)
        {
            return MoveToWaferFeederStageUnloadAvoidPosition(bFine);
        }

        public Task<int> MoveToWaferFeederExchangePositionAsync(bool bFine = false)
        {
            return MoveToWaferFeederExchangePosition(bFine);
        }

        private async Task<int> MoveWaferFeederYNamedPositionAsync(double targetPosition, string targetName, bool bFine)
        {
            using (MotionGuardRuntime.BeginAxisTeachingMove(FeederY, targetPosition, targetName))
            {
                return await MoveWaferFeederYAsync(targetPosition, bFine);
            }
        }

        public async Task<bool> WaitWaferFeederYMoveDone(int timeoutMs)
        {
            AxisMoveWaitResult waitResult = await WaitWaferFeederYMoveDoneInPosition(FeederY.CommandPosition, timeoutMs).ConfigureAwait(false);
            return waitResult.Success;
        }

        public async Task<AxisMoveWaitResult> WaitWaferFeederYMoveDoneInPosition(double targetPos, int timeoutMs)
        {
            return await AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                FeederY,
                targetPos,
                ResolveWaferFeederYInPositionTolerance(),
                timeoutMs > 0 ? timeoutMs : ResolveWaferFeederYMoveTimeoutMs(),
                0).ConfigureAwait(false);
        }

        public async Task<bool> WaitWaferFeederYInPosition(string positionName, int timeoutMs)
        {
            double target = GetTeachingPosition(positionName);
            AxisMoveWaitResult waitResult = await WaitWaferFeederYMoveDoneInPosition(target, timeoutMs).ConfigureAwait(false);
            return waitResult.Success;
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

        public bool IsWaferFeederInStageLoadAvoidPosition()
        {
            return IsWaferFeederYInPosition(Recipe.WaferLoadAvoidPosition, ResolveWaferFeederYInPositionTolerance());
        }

        public bool IsWaferFeederYInStageLoadPosition()
        {
            return IsWaferFeederInStageLoadPosition();
        }

        public bool IsWaferFeederInStageUnloadPosition()
        {
            return IsWaferFeederYInPosition(Recipe.WaferUnloadPosition, ResolveWaferFeederYInPositionTolerance());
        }

        public bool IsWaferFeederInStageUnloadAvoidPosition()
        {
            return IsWaferFeederYInPosition(Recipe.WaferUnloadAvoidPosition, ResolveWaferFeederYInPositionTolerance());
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

        public bool IsWaferFeederYInHomePosition()
        {
            return IsWaferFeederYInPosition(0.0, ResolveWaferFeederYInPositionTolerance());
        }

        public bool IsWaferFeederUp()
        {
            if (ShouldReadCylinderStateForDryRun(InputFeederLift))
                return InputFeederLift.IsFwd;

            if (IsWaferFeederLiftDryRunStateUnknown())
                return false;

            return WaferFeederUpSensor.IsOn;
        }

        public bool IsWaferFeederDown()
        {
            if (ShouldReadCylinderStateForDryRun(InputFeederLift))
                return InputFeederLift.IsBwd;

            if (IsWaferFeederLiftDryRunStateUnknown())
                return true;

            return WaferFeederDownSensor.IsOn;
        }

        public bool IsWaferFeederClamp()
        {
            if (ShouldReadCylinderStateForDryRun(InputFeederClamp))
                return InputFeederClamp.IsFwd;

            return !WaferFeederClampSensor.IsOn;
        }

        public bool IsWaferFeederUnclamp()
        {
            if (ShouldReadCylinderStateForDryRun(InputFeederClamp))
                return InputFeederClamp.IsBwd;

            return WaferFeederClampSensor.IsOn;
        }

        public bool IsWaferFeederRingCheck()
        {
            if (IsWaferFeederSimulationOrDryRun())
                return HasWaferOnFeeder();

            return WaferFeederRingCheckSensor.IsOn;
        }

        public bool IsWaferFeederRingDetected(bool expected = true)
        {
            if (IsWaferFeederSimulationOrDryRun())
                return HasWaferOnFeeder() == expected;

            return WaferFeederRingCheckSensor.IsOn == expected;
        }

        public bool IsWaferFeederOverload()
        {
            if (IsWaferFeederSimulationOrDryRun())
                return false;

            if (WaferFeederOverloadSensor == null)
                return false;

            // Overload input is NC: ON means normal, OFF means overload.
            return !WaferFeederOverloadSensor.IsOn;
        }

        public bool IsWaferFeederUnclamped()
        {
            if (ShouldReadCylinderStateForDryRun(InputFeederClamp))
                return InputFeederClamp.IsBwd;

            return WaferFeederClampSensor.IsOn;
        }

        public bool IsWaferFeederRingDetected(int size, bool expected = true)
        {
            try
            {
                BaseDigitalInput sensor = size <= 8 ? WaferFeeder8RingCheckSensor : WaferFeeder12RingCheckSensor;
                if (IsWaferFeederSimulationOrDryRun())
                    return HasWaferOnFeeder() == expected;
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
            string reason;
            return ValidateWaferFeederTeachingComplete(out reason);
        }

        public bool ValidateWaferFeederTeachingComplete(out string reason)
        {
            reason = string.Empty;
            var reasons = new List<string>();

            if (Recipe == null)
            {
                reasons.Add("Recipe is null.");
            }
            else
            {
                if (Recipe.CassetteLoadPosition == Recipe.AvoidPosition)
                    reasons.Add("CassetteLoadPosition equals AvoidPosition. value=" + Recipe.CassetteLoadPosition);

                if (Recipe.CassetteUnloadPosition == Recipe.AvoidPosition)
                    reasons.Add("CassetteUnloadPosition equals AvoidPosition. value=" + Recipe.CassetteUnloadPosition);

                if (Recipe.WaferLoadPosition == Recipe.WaferUnloadPosition)
                {
                    EventLogger.Write(
                        EventKind.Warning,
                        "QMC",
                        "WF-Y-TEACHING",
                        "WaferLoadPosition equals WaferUnloadPosition, but input feeder transfer will continue. value=" +
                        Recipe.WaferLoadPosition);
                }
            }

            if (reasons.Count == 0)
                return true;

            reason = string.Join(" ", reasons.ToArray());
            return false;
        }

        public bool ValidateWaferFeederYTeachingComplete()
        {
            string reason;
            return ValidateWaferFeederTeachingComplete(out reason);
        }

        public bool ValidateWaferFeederYTeachingComplete(out string reason)
        {
            return ValidateWaferFeederTeachingComplete(out reason);
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
                return RaiseFeederAlarm("WF-TEACH-INPOS", "InputFeederY teaching position timeout: " + positionName);

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
            return await SetWaferFeederUpDownAsync(up, timeoutMs, CancellationToken.None);
        }

        public async Task<int> SetWaferFeederUpDownAsync(bool up, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (IsWaferFeederOverload())
                    return RaiseFeederAlarm("WF-LIFT-OVERLOAD", "WaferFeeder overload is detected.");

                if (up && !IsWaferFeederEmpty())
                    return RaiseFeederAlarm("WF-LIFT-UP-WAFER-DETECTED", "WaferFeeder wafer is already detected before lift up.");

                bool ok = up
                    ? await InputFeederLift.MoveFwdAsync(ct)
                    : await InputFeederLift.MoveBwdAsync(ct);
                if (!ok)
                    return RaiseFeederAlarm(up ? "WF-LIFT-UP" : "WF-LIFT-DOWN", "WaferFeeder lift cylinder move failed.");

                ApplyWaferFeederLiftSensorSimulation(up);
                bool sensorOk = up
                    ? await AwaitWithCancellation(WaitWaferFeederUp(timeoutMs), ct)
                    : await AwaitWithCancellation(WaitWaferFeederDown(timeoutMs), ct);
                if (!sensorOk)
                    return RaiseFeederAlarm(up ? "WF-LIFT-UP-TIMEOUT" : "WF-LIFT-DOWN-TIMEOUT", "WaferFeeder lift sensor timeout.");

                return 0;
            }
            catch (OperationCanceledException)
            {
                StopWaferFeederCylinderOutputs(InputFeederLift);
                throw;
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
            return await SetWaferFeederClampAsync(clamp, timeoutMs, CancellationToken.None);
        }

        public async Task<int> SetWaferFeederClampAsync(bool clamp, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                bool ok = clamp
                    ? await InputFeederClamp.MoveFwdAsync(ct)
                    : await InputFeederClamp.MoveBwdAsync(ct);
                if (!ok)
                    return RaiseFeederAlarm(clamp ? "WF-CLAMP" : "WF-UNCLAMP", "WaferFeeder clamp cylinder move failed.");

                ApplyWaferFeederClampSensorSimulation(clamp);

                bool sensorOk = clamp
                    ? await AwaitWithCancellation(WaitWaferFeederClamp(timeoutMs), ct)
                    : await AwaitWithCancellation(WaitWaferFeederUnclamp(timeoutMs), ct);
                if (!sensorOk)
                    return RaiseFeederAlarm(clamp ? "WF-CLAMP-TIMEOUT" : "WF-UNCLAMP-TIMEOUT", "WaferFeeder clamp sensor timeout.");

                return 0;
            }
            catch (OperationCanceledException)
            {
                StopWaferFeederCylinderOutputs(InputFeederClamp);
                throw;
            }
            catch (Exception ex)
            {
                return RaiseFeederAlarm("WF-CLAMP-EX", "WaferFeeder clamp exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private static async Task<T> AwaitWithCancellation<T>(Task<T> task, CancellationToken ct)
        {
            if (task == null)
                return default(T);

            if (task.IsCompleted)
                return await task.ConfigureAwait(false);

            Task cancelTask = Task.Delay(Timeout.Infinite, ct);
            Task completed = await Task.WhenAny(task, cancelTask).ConfigureAwait(false);
            if (!ReferenceEquals(completed, task))
                ct.ThrowIfCancellationRequested();

            return await task.ConfigureAwait(false);
        }

        private static void StopWaferFeederCylinderOutputs(BaseCylinder cylinder)
        {
            try
            {
                if (cylinder == null)
                    return;

                if (cylinder.OutFwd != null)
                    cylinder.OutFwd.Write(false);
                if (cylinder.OutBwd != null)
                    cylinder.OutBwd.Write(false);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ApplyWaferFeederLiftSensorSimulation(bool up)
        {
            if (!ShouldApplyInputSimulation(WaferFeederUpSensor) &&
                !ShouldApplyInputSimulation(WaferFeederDownSensor))
                return;

            SimulateInputIfAllowed(WaferFeederUpSensor, up);
            SimulateInputIfAllowed(WaferFeederDownSensor, !up);
        }

        private void ApplyWaferFeederClampSensorSimulation(bool clamp)
        {
            if (!ShouldApplyInputSimulation(WaferFeederClampSensor))
                return;

            // WaferFeederClampSensor is B-contact style in this unit:
            // false = clamp, true = unclamp.
            SimulateInputIfAllowed(WaferFeederClampSensor, !clamp);
        }

        private bool ShouldBypassInputWaitInSimulation(BaseDigitalInput input)
        {
            return IsWaferFeederSimulationOrDryRun() && !CanSimulateInput(input);
        }

        private bool ShouldApplyInputSimulation(BaseDigitalInput input)
        {
            return CanSimulateInput(input) &&
                   (IsWaferFeederSimulationOrDryRun() ||
                    IsCylinderSimulation(InputFeederLift) ||
                    IsCylinderSimulation(InputFeederClamp));
        }

        private static bool CanSimulateInput(BaseDigitalInput input)
        {
            return input != null && input.Config != null && input.Config.IsSimulationMode;
        }

        private static bool IsCylinderSimulation(BaseCylinder cylinder)
        {
            return cylinder != null && cylinder.Config != null && cylinder.Config.IsSimulationMode;
        }

        private static bool ShouldReadCylinderStateForDryRun(BaseCylinder cylinder)
        {
            return cylinder != null && cylinder.Config != null &&
                   cylinder.Config.IsSimulationMode;
        }

        private bool IsWaferFeederLiftDryRunStateUnknown()
        {
            return IsWaferFeederSimulationOrDryRun() &&
                   InputFeederLift != null &&
                   InputFeederLift.Config != null &&
                   InputFeederLift.Config.IgnoreInputWaits &&
                   WaferFeederUpSensor != null &&
                   WaferFeederDownSensor != null &&
                   !WaferFeederUpSensor.IsOn &&
                   !WaferFeederDownSensor.IsOn;
        }

        private static void SimulateInputIfAllowed(BaseDigitalInput input, bool state)
        {
            if (CanSimulateInput(input))
                input.SimulateInput(state);
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
            if (ShouldBypassInputWaitInSimulation(WaferFeederUpSensor))
                return true;
            return await WaferFeederUpSensor.WaitUntilStateAsync(true, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederDown(int timeoutMs)
        {
            if (ShouldBypassInputWaitInSimulation(WaferFeederDownSensor))
                return true;
            return await WaferFeederDownSensor.WaitUntilStateAsync(true, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederClamp(int timeoutMs)
        {
            if (ShouldBypassInputWaitInSimulation(WaferFeederClampSensor))
                return true;
            return await WaferFeederClampSensor.WaitUntilStateAsync(false, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederUnclamp(int timeoutMs)
        {
            if (ShouldBypassInputWaitInSimulation(WaferFeederClampSensor))
                return true;
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
            return !IsWaferFeederOverload();
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

        public Task<int> ManualMoveToWaferFeederStageUnloadAvoidPosition(bool bFine = false)
        {
            return MoveToWaferFeederStageUnloadAvoidPosition(bFine);
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

        public async Task<int> LoadWaferFromCassetteToFeeder(
            int slotIndex,
            int timeoutMs,
            bool bFine = false,
            bool useBarcode = false,
            CassetteMaterialRole cassetteRole = CassetteMaterialRole.Input1)
        {
            return await LoadWaferFromCassetteToFeeder(slotIndex, timeoutMs, bFine, useBarcode, cassetteRole, CancellationToken.None);
        }

        public async Task<int> LoadWaferFromCassetteToFeeder(
            int slotIndex,
            int timeoutMs,
            bool bFine,
            bool useBarcode,
            CassetteMaterialRole cassetteRole,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!CheckWaferFeederTransferReady(TransferMode.Load))
                return RaiseFeederAlarm("WF-LOAD-READY", "WaferFeeder load transfer is not ready.");

            WaferMaterial wafer = MaterialStateService.GetWaferInCassette(cassetteRole, slotIndex);
            if (wafer == null)
                return RaiseFeederAlarm("WF-LOAD-DATA", "Cassette wafer data was not found. role=" + cassetteRole + ", slot=" + slotIndex);

            int result = await PrepareWaferFeederForCassetteLoadAsync(timeoutMs, ct);
            if (result != 0)
                return result;

            ct.ThrowIfCancellationRequested();
            result = await MoveToWaferFeederCassetteLoadPosition(slotIndex, bFine);
            if (result != 0)
                return result;

            result = await WaitWaferFeederYMoveDoneInPositionOrAlarm(
                Recipe.CassetteLoadPosition,
                timeoutMs,
                "WF-LOAD-Y",
                "WaferFeeder cassette load position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await SetWaferFeederUpDownAsync(true, ResolveLiftTimeoutMs(true), ct);
            if (result != 0) return result;

            result = await SetWaferFeederClampAsync(true, ResolveClampTimeoutMs(true), ct);
            if (result != 0) return result;

            if (!await AwaitWithCancellation(WaitWaferFeederClamp(timeoutMs), ct))
                return RaiseFeederAlarm("WF-LOAD-CLAMP-TIMEOUT", "WaferFeeder clamp sensor timeout.");

            if (!IsWaferFeederSimulationOrDryRun())
            {
                if (!IsWaferFeederRingDetected(true))
                    return RaiseFeederAlarm("WF-LOAD-RING", "WaferFeeder ring was not detected after cassette load.");
            }
           

            SetCurrentWaferMaterial(wafer);
            MaterialStateService.MoveWaferToInputFeeder(wafer);

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

            result = await WaitWaferFeederYMoveDoneInPositionOrAlarm(
                Recipe.WaferBarcodePosition,
                timeoutMs,
                "WF-BARCODE",
                "WaferFeeder barcode position",
                CancellationToken.None).ConfigureAwait(false);
            if (result != 0)
            {
                return string.Empty;
            }

            return string.Empty;
        }

        public async Task<int> LoadWaferFromFeederToStage(int size, int timeoutMs, bool bFine = false, bool useVacuum = true)
        {
            return await LoadWaferFromFeederToStage(size, timeoutMs, bFine, useVacuum, CancellationToken.None);
        }

        public async Task<int> LoadWaferFromFeederToStage(int size, int timeoutMs, bool bFine, bool useVacuum, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!CheckWaferStageReady(size, TransferMode.Load))
                return RaiseFeederAlarm("WF-STAGE-LOAD-READY", "WaferStage load transfer is not ready.");

            int result = await MoveToWaferFeederStageLoadPosition(bFine);
            if (result != 0)
                return result;

            result = await WaitWaferFeederYMoveDoneInPositionOrAlarm(
                Recipe.WaferLoadPosition,
                timeoutMs,
                "WF-STAGE-LOAD-Y",
                "WaferFeeder stage load position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await SetWaferFeederClampAsync(false, timeoutMs, ct);
            if (result != 0)
                return result;

            result = await SetWaferFeederUpDownAsync(false, timeoutMs, ct);
            if (result != 0)
                return result;

            if (!await AwaitWithCancellation(WaitWaferFeederRingState(false, timeoutMs), ct))
                return RaiseFeederAlarm("WF-STAGE-LOAD-RING", "WaferFeeder ring remained after stage load.");

            WaferMaterial wafer = CurrentWaferMaterial;
            if (wafer != null)
                MaterialStateService.MoveWaferToInputStage(wafer);
            ClearCurrentWaferMaterial();
            return 0;
        }

        public async Task<int> UnloadWaferFromStageToFeeder(int size, int timeoutMs, bool bFine = false)
        {
            return await UnloadWaferFromStageToFeeder(size, timeoutMs, bFine, CancellationToken.None);
        }

        public async Task<int> UnloadWaferFromStageToFeeder(int size, int timeoutMs, bool bFine, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!CheckWaferStageReady(size, TransferMode.Unload))
                return RaiseFeederAlarm("WF-STAGE-UNLOAD-READY", "WaferStage unload transfer is not ready.");

            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (wafer == null)
                return RaiseFeederAlarm("WF-STAGE-UNLOAD-DATA", "InputStage wafer data was not found.");

            int result = await MoveToWaferFeederStageUnloadPosition(bFine);
            if (result != 0)
                return result;

            result = await WaitWaferFeederYMoveDoneInPositionOrAlarm(
                Recipe.WaferUnloadPosition,
                timeoutMs,
                "WF-STAGE-UNLOAD-Y",
                "WaferFeeder stage unload position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await SetWaferFeederUpDownAsync(true, timeoutMs, ct);
            if (result != 0)
                return result;

            result = await SetWaferFeederClampAsync(true, timeoutMs, ct);
            if (result != 0)
                return result;

            if (!await AwaitWithCancellation(WaitWaferFeederRingState(true, timeoutMs), ct))
                return RaiseFeederAlarm("WF-STAGE-UNLOAD-RING", "WaferFeeder ring was not detected after stage unload.");

            SetCurrentWaferMaterial(wafer);
            MaterialStateService.MoveWaferToInputFeeder(wafer);
            return 0;
        }

        public async Task<int> UnloadWaferFromFeederToCassette(int slotIndex, int timeoutMs, bool bFine = false)
        {
            return await UnloadWaferFromFeederToCassette(slotIndex, timeoutMs, bFine, CancellationToken.None);
        }

        public async Task<int> UnloadWaferFromFeederToCassette(int slotIndex, int timeoutMs, bool bFine, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!CheckWaferCassetteReady(slotIndex, TransferMode.Unload))
                return RaiseFeederAlarm("WF-CST-UNLOAD-READY", "WaferCassette unload transfer is not ready.");

            int result = await MoveToWaferFeederCassetteUnloadPosition(slotIndex, bFine);
            if (result != 0)
                return result;

            result = await WaitWaferFeederYMoveDoneInPositionOrAlarm(
                Recipe.CassetteUnloadPosition,
                timeoutMs,
                "WF-RETURN-Y",
                "WaferFeeder cassette unload position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await SetWaferFeederClampAsync(false, ResolveClampTimeoutMs(false), ct);
            if (result != 0) return result;

            result = await SetWaferFeederUpDownAsync(false, ResolveLiftTimeoutMs(false), ct);
            if (result != 0) return result;

            if (!await AwaitWithCancellation(WaitWaferFeederUnclamp(timeoutMs), ct))
                return RaiseFeederAlarm("WF-RETURN-UNCLAMP-TIMEOUT", "WaferFeeder unclamp sensor timeout.");

            if (!await AwaitWithCancellation(WaitWaferFeederRingState(false, timeoutMs), ct))
                return RaiseFeederAlarm("WF-RETURN-RING", "WaferFeeder ring remained after cassette unload.");

            WaferMaterial wafer = CurrentWaferMaterial;
            if (wafer != null)
                MaterialStateService.PutWaferInCassette(
                    wafer.WaferId,
                    wafer.SourceCassetteRole,
                    slotIndex,
                    wafer.CassetteLotId,
                    wafer.SourceCassetteSlotPosition);
            ClearCurrentWaferMaterial();
            return 0;
        }

        public async Task<int> ReturnWaferFromFeederToCassette(int slotIndex, int timeoutMs, bool bFine = false)
        {
            return await UnloadWaferFromFeederToCassette(slotIndex, timeoutMs, bFine);
        }

        public async Task<int> ExchangeWaferFeederRingForNextSlot(int currentSlotIndex, int nextSlotIndex, int timeoutMs, bool bFine = false)
        {
            return await ExchangeWaferFeederRingForNextSlot(currentSlotIndex, nextSlotIndex, timeoutMs, bFine, CancellationToken.None);
        }

        public async Task<int> ExchangeWaferFeederRingForNextSlot(int currentSlotIndex, int nextSlotIndex, int timeoutMs, bool bFine, CancellationToken ct)
        {
            int result = await UnloadWaferFromFeederToCassette(currentSlotIndex, timeoutMs, bFine, ct);
            if (result != 0)
                return result;

            result = await LoadWaferFromCassetteToFeeder(nextSlotIndex, timeoutMs, bFine, false, CassetteMaterialRole.Input1, ct);
            if (result != 0)
                return result;

            ct.ThrowIfCancellationRequested();
            return await MoveToWaferFeederExchangePosition(bFine);
        }

        public async Task<int> RecoverWaferFeederToSafeState(int timeoutMs, bool moveAvoid = true)
        {
            return await RecoverWaferFeederToSafeState(timeoutMs, moveAvoid, CancellationToken.None);
        }

        public async Task<int> RecoverWaferFeederToSafeState(int timeoutMs, bool moveAvoid, CancellationToken ct)
        {
            int result;
            result = await SetWaferFeederClampAsync(false, ResolveClampTimeoutMs(false), ct);
            if (result != 0) return result;

            result = await SetWaferFeederUpDownAsync(false, ResolveLiftTimeoutMs(false), ct);
            if (result != 0) return result;

            if (moveAvoid)
            {
                ct.ThrowIfCancellationRequested();
                result = await MoveToWaferFeederAvoidPosition();
                if (result != 0)
                    return result;

                result = await WaitWaferFeederYMoveDoneInPositionOrAlarm(
                    Recipe.AvoidPosition,
                    timeoutMs,
                    "WF-RECOVER",
                    "WaferFeeder avoid position",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        public bool CheckWaferFeederMoveReady()
        {
            string reason;
            return CheckWaferFeederMoveReady(out reason);
        }

        public bool CheckWaferFeederMoveReady(out string reason)
        {
            reason = string.Empty;
            var reasons = new List<string>();

            if (FeederY == null)
            {
                reasons.Add("FeederY axis is null.");
            }
            else
            {
                if (FeederY.IsAlarm)
                    reasons.Add("FeederY alarm is ON. alarmCode=" + FeederY.AlarmCode);
            }

            if (!CheckWaferFeederOverloadClear())
                reasons.Add("WaferFeeder overload is detected. " + FormatInputState("Overload", WaferFeederOverloadSensor));

            if (reasons.Count == 0)
                return true;

            reason = string.Join(" ", reasons.ToArray()) + " " + GetWaferFeederTransferState();
            EventLogger.Write(EventKind.Warning, "QMC", "WF-MOVE-READY", reason);
            return false;
        }

        public bool CheckWaferFeederYMoveReady()
        {
            string reason;
            return CheckWaferFeederYMoveReady(out reason);
        }

        public bool CheckWaferFeederYMoveReady(out string reason)
        {
            reason = string.Empty;
            try
            {
                var reasons = new List<string>();

                if (FeederY == null)
                {
                    reasons.Add("FeederY axis is null.");
                }
                else
                {
                    if (!FeederY.IsServoOn)
                        reasons.Add("FeederY servo is OFF.");
                    if (FeederY.IsAlarm)
                        reasons.Add("FeederY alarm is ON. alarmCode=" + FeederY.AlarmCode);
                    if (FeederY.IsMoving)
                        reasons.Add("FeederY is moving.");
                }

                if (!CheckWaferFeederOverloadClear())
                    reasons.Add("WaferFeeder overload is detected. " + FormatInputState("Overload", WaferFeederOverloadSensor));

                if (reasons.Count == 0)
                    return true;

                reason = string.Join(" ", reasons.ToArray()) + " " + GetWaferFeederTransferState();
                EventLogger.Write(EventKind.Warning, "QMC", "WF-Y-MOVE-READY", reason);
                return false;
            }
            catch (Exception ex)
            {
                reason = "InputFeederY move ready check failed: " + ex.Message;
                EventLogger.Write(EventKind.Warning, "QMC", "WF-Y-MOVE-READY", reason);
                return false;
            }
            finally
            {
            }
        }

        public bool CheckWaferFeederTransferReady(TransferMode mode)
        {
            string reason;
            return CheckWaferFeederTransferReady(mode, out reason);
        }

        public bool CheckWaferFeederTransferReady(TransferMode mode, out string reason)
        {
            reason = string.Empty;

            if (!CheckWaferFeederMoveReady(out reason))
                return false;

            if (mode == TransferMode.Load)
            {
                if (!IsWaferFeederTransferDataEmpty())
                {
                    reason = "WaferFeeder data is not empty before load. " + BuildWaferFeederDataSummary();
                    EventLogger.Write(EventKind.Warning, "QMC", "WF-TRANSFER-READY", reason);
                    return false;
                }

                if (!IsWaferFeederEmpty())
                {
                    reason = "WaferFeeder sensor/data is not empty before load. " + GetWaferFeederTransferState();
                    EventLogger.Write(EventKind.Warning, "QMC", "WF-TRANSFER-READY", reason);
                    return false;
                }

                return true;
            }

            if (mode == TransferMode.Unload)
            {
                if (!IsWaferFeederTransferDataOccupied())
                {
                    reason = "WaferFeeder data is empty before unload. " + BuildWaferFeederDataSummary();
                    EventLogger.Write(EventKind.Warning, "QMC", "WF-TRANSFER-READY", reason);
                    return false;
                }

                if (!HasWaferOnFeeder())
                {
                    reason = "WaferFeeder wafer sensor/data does not indicate wafer before unload. " + GetWaferFeederTransferState();
                    EventLogger.Write(EventKind.Warning, "QMC", "WF-TRANSFER-READY", reason);
                    return false;
                }

                return true;
            }

            return true;
        }

        public async Task<int> PrepareWaferFeederForCassetteLoadAsync(int timeoutMs)
        {
            return await PrepareWaferFeederForCassetteLoadAsync(timeoutMs, CancellationToken.None);
        }

        public async Task<int> PrepareWaferFeederForCassetteLoadAsync(int timeoutMs, CancellationToken ct)
        {
            int result = await SetWaferFeederClampAsync(false, timeoutMs, ct);
            if (result != 0)
                return result;

            result = await SetWaferFeederUpDownAsync(false, timeoutMs, ct);
            if (result != 0)
                return result;

            if (!IsWaferFeederUnclamp())
                return RaiseFeederAlarm("WF-LOAD-UNCLAMP", "WaferFeeder must be unclamped before cassette load.");
            if (!IsWaferFeederDown())
                return RaiseFeederAlarm("WF-LOAD-DOWN", "WaferFeeder must be down before cassette load.");

            return 0;
        }

        public bool IsWaferFeederTransferDataEmpty()
        {
            return CurrentWaferMaterial == null &&
                   MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder) == null &&
                   CurrentMaterialState == MaterialState.Empty;
        }

        public bool IsWaferFeederTransferDataOccupied()
        {
            return CurrentWaferMaterial != null ||
                   MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder) != null ||
                   CurrentMaterialState != MaterialState.Empty &&
                   CurrentMaterialState != MaterialState.Error;
        }

        public bool CheckWaferCassetteReady(int slotIndex, TransferMode mode)
        {
            string reason;
            return CheckWaferCassetteReady(slotIndex, mode, out reason);
        }

        public bool CheckWaferCassetteReady(int slotIndex, TransferMode mode, out string reason)
        {
            reason = string.Empty;

            if (slotIndex < 0)
            {
                reason = "Slot index is invalid. slotIndex=" + slotIndex;
                EventLogger.Write(EventKind.Warning, "QMC", "WF-CST-READY", reason);
                return false;
            }

            if (!CheckWaferFeederTransferReady(mode, out reason))
            {
                reason = "WaferFeeder transfer ready check failed. mode=" + mode + ". " + reason;
                EventLogger.Write(EventKind.Warning, "QMC", "WF-CST-READY", reason);
                return false;
            }

            if (!ValidateWaferFeederYTeachingComplete(out reason))
            {
                reason = "WaferFeederY teaching check failed. " + reason;
                EventLogger.Write(EventKind.Warning, "QMC", "WF-CST-READY", reason);
                return false;
            }

            return true;
        }

        public bool CheckWaferStageReady(int size, TransferMode mode)
        {
            string reason;
            return CheckWaferStageReady(size, mode, out reason);
        }

        public bool CheckWaferStageReady(int size, TransferMode mode, out string reason)
        {
            reason = string.Empty;

            if (size != 8 && size != 12)
            {
                reason = "Wafer size is invalid. size=" + size;
                EventLogger.Write(EventKind.Warning, "QMC", "WF-STAGE-READY", reason);
                return false;
            }

            if (mode == TransferMode.Load)
            {
                if (!IsWaferFeederOccupied())
                {
                    reason = "WaferFeeder must be occupied before stage load. " + GetWaferFeederTransferState();
                    EventLogger.Write(EventKind.Warning, "QMC", "WF-STAGE-READY", reason);
                    return false;
                }

                if (!IsWaferFeederSimulationOrDryRun() && !IsWaferFeederRingDetected(size, false))
                {
                    reason = "WaferFeeder size ring sensor state is invalid before stage load. expected size=" + size +
                             ", expectedState=OFF. " + BuildWaferFeederRingSensorSummary();
                    EventLogger.Write(EventKind.Warning, "QMC", "WF-STAGE-READY", reason);
                    return false;
                }

                return true;
            }

            if (mode == TransferMode.Unload)
            {
                if (!IsWaferFeederEmpty())
                {
                    reason = "WaferFeeder must be empty before stage unload. " + GetWaferFeederTransferState();
                    EventLogger.Write(EventKind.Warning, "QMC", "WF-STAGE-READY", reason);
                    return false;
                }

                if (!IsWaferFeederSimulationOrDryRun() && !IsWaferFeederRingDetected(size, true))
                {
                    reason = "WaferFeeder size ring sensor state is invalid before stage unload. expected size=" + size +
                             ", expectedState=ON. " + BuildWaferFeederRingSensorSummary();
                    EventLogger.Write(EventKind.Warning, "QMC", "WF-STAGE-READY", reason);
                    return false;
                }

                return true;
            }

            return CheckWaferFeederMoveReady(out reason);
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
                // 피더 Avoid 위치명 반환
                case FeederPositionType.Avoid:
                    return "1_WaferFeederY.AvoidPos";
                // 카세트 로드 위치명 반환
                case FeederPositionType.CassetteLoad:
                    return "1_WaferFeederY.CassetteLoadPos";
                // 카세트 언로드 위치명 반환
                case FeederPositionType.CassetteUnload:
                    return "1_WaferFeederY.CassetteUnloadPos";
                // 바코드 위치명 반환
                case FeederPositionType.Barcode:
                    return "1_WaferFeederY.StageBarcodePos";
                // 스테이지 로드 위치명 반환
                case FeederPositionType.StageLoad:
                    return "1_WaferFeederY.StageLoadPos";
                // 스테이지 언로드 위치명 반환
                case FeederPositionType.StageUnload:
                    return "1_WaferFeederY.StageUnloadPos";
                // 카세트 교체 위치명 반환
                case FeederPositionType.Exchange:
                    return "1_WaferFeederY.CassetteExchangePos";
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public WaferFeederProcessState GetWaferFeederProcessState()
        {
            if (FeederY.IsAlarm || IsWaferFeederOverload())
                return WaferFeederProcessState.Alarm;
            if (FeederY.IsMoving)
                return WaferFeederProcessState.Moving;
            if (HasWaferOnFeeder())
                return WaferFeederProcessState.HasWafer;
            return WaferFeederProcessState.Empty;
        }

        public bool HasWaferOnFeeder()
        {
            if (IsWaferFeederSimulationOrDryRun())
                return IsWaferFeederTransferDataOccupied();

            return IsWaferFeederTransferDataOccupied() && IsWaferFeederRingDetected(true);
        }

        public bool IsWaferFeederSafe()
        {
            return CheckWaferFeederOverloadClear() && !FeederY.IsAlarm;
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
                    + ", Clamp=" + IsWaferFeederClamp()
                    + ", Data=" + CurrentMaterialState
                    + ", Wafer=" + CurrentWaferId
                    + ", Ring=" + IsWaferFeederRingCheck()
                    + ", Overload=" + IsWaferFeederOverload()
                    + ", FeederY=" + BuildFeederYAxisSummary();
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

        private string BuildFeederYAxisSummary()
        {
            if (FeederY == null)
                return "null";

            return "name=" + FeederY.Name +
                   ", servo=" + FeederY.IsServoOn +
                   ", alarm=" + FeederY.IsAlarm +
                   ", alarmCode=" + FeederY.AlarmCode +
                   ", moving=" + FeederY.IsMoving +
                   ", actual=" + FeederY.ActualPosition +
                   ", command=" + FeederY.CommandPosition +
                   ", inPosition=" + FeederY.IsInPosition;
        }

        private string BuildWaferFeederDataSummary()
        {
            try
            {
                WaferMaterial serviceWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
                return "Data: currentState=" + CurrentMaterialState +
                       ", currentWafer=" + (CurrentWaferMaterial != null ? CurrentWaferMaterial.WaferId : "-") +
                       ", serviceWafer=" + (serviceWafer != null ? serviceWafer.WaferId : "-") +
                       ", simOrDryRun=" + IsWaferFeederSimulationOrDryRun() + ".";
            }
            catch (Exception ex)
            {
                return "Data summary failed: " + ex.Message;
            }
            finally
            {
            }
        }

        private string BuildWaferFeederRingSensorSummary()
        {
            return "Sensors: ring=" + FormatInputState("Ring", WaferFeederRingCheckSensor) +
                   ", ring8=" + FormatInputState("Ring8", WaferFeeder8RingCheckSensor) +
                   ", ring12=" + FormatInputState("Ring12", WaferFeeder12RingCheckSensor) +
                   ", overload=" + FormatInputState("Overload", WaferFeederOverloadSensor) + ".";
        }

        private static string FormatInputState(string name, BaseDigitalInput input)
        {
            if (input == null)
                return name + "=NULL";

            return name + "=" + (input.IsOn ? "ON" : "OFF");
        }

        public bool IsWaferFeederEmpty()
        {
            if (IsWaferFeederSimulationOrDryRun())
                return IsWaferFeederTransferDataEmpty();

            return IsWaferFeederTransferDataEmpty() && IsWaferFeederRingDetected(false);
        }

        public bool IsWaferFeederOccupied()
        {
            return HasWaferOnFeeder();
        }

        public bool IsWaferFeederSimulationOrDryRun()
        {
            AppSettings settings = AppSettingsStore.Current;
            bool simulation = Setup != null && Setup.IsSimulationMode;
            bool dryRun = Config != null && Config.bDryRun;
            bool globalDryRun = settings != null && (settings.BypassHardware || settings.DryRunMode);
            return globalDryRun || simulation || dryRun;
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
                return RaiseFeederAlarm("WF-EX-Y-ALARM", "InputFeederY alarm after exchange move.");

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
                return RaiseFeederAlarm("WF-RET-Y-ALARM", "InputFeederY alarm after retract move.");
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
                EventLogger.Write(EventKind.Alarm, "QMC", "WF-STOP", Name, "WaferFeeder stop failed: " + ex.Message);
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
                    // 클램프 유지 안전 출력
                    case FeederSafePolicy.HoldClamp:
                        SetWaferFeederLiftUpOutput(false);
                        SetWaferFeederLiftDownOutput(true);
                        break;
                    // 현재 출력 상태 유지
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
                // 피더 Y축 알람 메시지
                case FeederAlarmCode.AxisAlarm:
                    return "InputFeederY axis alarm.";
                // 피더 Y축 이동 타임아웃 메시지
                case FeederAlarmCode.MoveTimeout:
                    return "InputFeederY move timeout.";
                // 피더 티칭 누락 메시지
                case FeederAlarmCode.TeachingMissing:
                    return "WaferFeeder teaching position is missing.";
                // 피더 인터락 메시지
                case FeederAlarmCode.Interlock:
                    return "WaferFeeder interlock condition is not satisfied.";
                // 피더 오버로드 메시지
                case FeederAlarmCode.Overload:
                    return "WaferFeeder overload is detected.";
                // 피더 링 감지 누락 메시지
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

        public void SetCurrentWaferMaterial(WaferMaterial wafer)
        {
            CurrentWaferMaterial = wafer;
            UpdateWaferFeederMaterialState(wafer == null ? MaterialState.Empty : MaterialState.Occupied);
        }

        public WaferMaterial TakeCurrentWaferMaterial()
        {
            WaferMaterial wafer = CurrentWaferMaterial;
            ClearCurrentWaferMaterial();
            return wafer;
        }

        public void ClearCurrentWaferMaterial()
        {
            CurrentWaferMaterial = null;
            UpdateWaferFeederMaterialState(MaterialState.Empty);
        }

        public void ClearWaferFeederMaterialState()
        {
            ClearCurrentWaferMaterial();
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
                EventLogger.Write(EventKind.Warning, "QMC", "WF-Y-VALIDATE", "InputFeederY target validate failed: " + ex.Message);
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
                EventLogger.Write(EventKind.Alarm, "QMC", code, Name, message);
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

        private async Task<int> WaitWaferFeederYMoveDoneInPositionOrAlarm(
            double target,
            int timeoutMs,
            string alarmPrefix,
            string description,
            CancellationToken ct)
        {
            AxisMoveWaitResult waitResult = await AwaitWithCancellation(
                WaitWaferFeederYMoveDoneInPosition(target, timeoutMs),
                ct).ConfigureAwait(false);

            if (waitResult.Success)
                return 0;

            return RaiseFeederAlarm(
                AxisMoveWaiter.ResolveAlarmCode(alarmPrefix, waitResult),
                description + " move/in-position wait failed. " +
                AxisMoveWaiter.FormatResult(waitResult, GetWaferFeederTransferState()));
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

            throw new ArgumentException("Unknown InputFeederY teaching position: " + positionName, "positionName");
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
                else throw new ArgumentException("Unknown InputFeederY teaching position: " + positionName, "positionName");
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
                key = key.Replace("InputFeederY.", string.Empty);
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
