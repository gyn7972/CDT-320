using QMC.CDT320.Ajin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320
{
    [DataContract]
    public class OutputFeederSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }

        public OutputFeederSetup()
        {
            SetDefaults();
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            IsSimulationMode = false;
        }
    }

    [DataContract]
    public class OutputFeederConfig : IConfigData
    {
        [DataMember] public bool bDryRun { get; set; }

        public OutputFeederConfig()
        {
            SetDefaults();
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            bDryRun = false;
        }
    }

    [DataContract]
    public class OutputFeederRecipe : IRecipeData
    {
        [DataMember] public double AvoidPosition { get; set; }
        [DataMember] public double GoodCassetteLoadPosition { get; set; }
        [DataMember] public double GoodCassetteUnloadPosition { get; set; }
        [DataMember] public double GoodCassetteExchangePosition { get; set; }
        [DataMember] public double GoodWaferLoadAvoidPosition { get; set; }
        [DataMember] public double GoodWaferLoadPosition { get; set; }
        [DataMember] public double GoodWaferUnloadAvoidPosition { get; set; }
        [DataMember] public double GoodWaferUnloadPosition { get; set; }
        [DataMember] public double GoodWaferBarcodePosition { get; set; }
        [DataMember] public double NGCassetteLoadPosition { get; set; }
        [DataMember] public double NGCassetteUnloadPosition { get; set; }
        [DataMember] public double NGCassetteExchangePosition { get; set; }
        [DataMember] public double NGWaferLoadAvoidPosition { get; set; }
        [DataMember] public double NGWaferLoadPosition { get; set; }
        [DataMember] public double NGWaferUnloadAvoidPosition { get; set; }
        [DataMember] public double NGWaferUnloadPosition { get; set; }
        [DataMember] public double NGWaferBarcodePosition { get; set; }

        public OutputFeederRecipe()
        {
            SetDefaults();
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            AvoidPosition = 0.0;
            GoodCassetteLoadPosition = 30.0;
            GoodCassetteUnloadPosition = 30.0;
            GoodCassetteExchangePosition = 0.0;
            GoodWaferLoadAvoidPosition = 0.0;
            GoodWaferLoadPosition = 150.0;
            GoodWaferUnloadAvoidPosition = 0.0;
            GoodWaferUnloadPosition = 150.0;
            GoodWaferBarcodePosition = 0.0;
            NGCassetteLoadPosition = 30.0;
            NGCassetteUnloadPosition = 30.0;
            NGCassetteExchangePosition = 0.0;
            NGWaferLoadAvoidPosition = 0.0;
            NGWaferLoadPosition = 200.0;
            NGWaferUnloadAvoidPosition = 0.0;
            NGWaferUnloadPosition = 200.0;
            NGWaferBarcodePosition = 0.0;
        }
    }

    public sealed class FeederTransferState
    {
        public bool IsUp { get; set; }
        public bool IsDown { get; set; }
        public bool IsUnclamped { get; set; }
        public bool HasRing { get; set; }
        public bool IsOverload { get; set; }
        public bool IsMoveReady { get; set; }
    }

    public class OutputFeederUnit : BaseUnit<OutputFeederSetup, OutputFeederConfig, OutputFeederRecipe>, IUnitJogController
    {
        private readonly Dictionary<string, double> _positionSnapshots = new Dictionary<string, double>();

        public MaterialState CurrentMaterialState { get; private set; }
        public BaseAxis FeederY { get; private set; }
        public BaseDigitalInput BinFeederUpSensor { get; private set; }
        public BaseDigitalInput BinFeederDownSensor { get; private set; }
        public BaseDigitalInput BinFeederUnclampSensor { get; private set; }
        public BaseDigitalInput BinFeederRingCheckSensor { get; private set; }
        public BaseDigitalInput BinFeederOverloadSensor { get; private set; }
        public BaseDigitalInput WaferClampedSensor { get { return BinFeederRingCheckSensor; } }
        public BaseCylinder FeederUpDownCyl { get; private set; }
        public BaseCylinder FeederClampCyl { get; private set; }
        public BaseDigitalOutput BinFeederUpOut { get { return FeederUpDownCyl.OutFwd; } }
        public BaseDigitalOutput BinFeederDownOut { get { return FeederUpDownCyl.OutBwd; } }
        public BaseDigitalOutput BinFeederClampOut { get { return FeederClampCyl.OutFwd; } }
        public BaseDigitalOutput BinFeederUnclampOut { get { return FeederClampCyl.OutBwd; } }

        public OutputFeederUnit() : base("BinFeederUnit")
        {
            CurrentMaterialState = MaterialState.Empty;
            FeederY = AjinFactory.CreateAxis("OutputFeederY");
            FeederY.Setup.SoftLimitPlus = 350.0;

            BinFeederUpSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinNFeederUp"));
            BinFeederDownSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinFeederDown"));
            BinFeederUnclampSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinFeederUnclamp"));
            BinFeederRingCheckSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinFeederRing"));
            BinFeederOverloadSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinFeederOverload"));
            FeederUpDownCyl = CylinderManager.Get(AjinIoCatalog.CylinderRefs.OutputFeederLift);
            FeederClampCyl = CylinderManager.Get(AjinIoCatalog.CylinderRefs.OutputFeederClamp);

            Components.Add(FeederY);
            Components.Add(BinFeederUpSensor);
            Components.Add(BinFeederDownSensor);
            Components.Add(BinFeederUnclampSensor);
            Components.Add(BinFeederRingCheckSensor);
            Components.Add(BinFeederOverloadSensor);
            Components.Add(FeederUpDownCyl);
            Components.Add(FeederClampCyl);

            InitializeOutputFeederSimulationInputs();
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
            return MoveBinFeederY(target, speedType == JogSpeedType.Fine);
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
            ManualMoveBinFeederYJog(direction, speed);
            return Task.FromResult(0);
        }

        public Task<int> StopJogAsync(BaseAxis axis)
        {
            if (!CanHandleJogAxis(axis))
                return Task.FromResult(-1);

            ManualStopBinFeederY();
            return Task.FromResult(0);
        }

        public Task<int> MoveBinFeederY(double targetPos, bool bFine = false)
        {
            return MoveBinFeederYAsync(targetPos, bFine);
        }

        public async Task<int> MoveBinFeederYAsync(double targetPos, bool bFine = false)
        {
            try
            {
                string readyReason;
                if (!CheckBinFeederYMoveReady(out readyReason))
                    return RaiseFeederAlarm("BF-Y-READY", "OutputFeederY 이동 준비 조건이 맞지 않습니다. " + readyReason);

                if (!ValidateBinFeederYTargetPosition(targetPos))
                    return RaiseFeederAlarm("BF-Y-SOFT-LIMIT", "OutputFeederY 목표 위치가 소프트 리미트를 벗어났습니다. target=" + targetPos);

                if (IsBinFeederYInPosition(targetPos, ResolveBinFeederYInPositionTolerance()))
                {
                    EventLogger.Write(EventKind.Event, "QMC", "BF-Y-MOVE",
                        "OutputFeederY가 이미 목표 위치에 있습니다. target=" + targetPos + ", " + DescribeBinFeederYMoveDoneState());
                    return 0;
                }

                EventLogger.Write(EventKind.Event, "QMC", "BF-Y-MOVE", "OutputFeederY 이동 시작. target=" + targetPos);
                int result = await FeederY.MoveAbsoluteAsync(targetPos, ResolveBinFeederYMoveVelocity(bFine));
                if (result != 0 || FeederY.IsAlarm)
                    return RaiseFeederAlarm(
                        "BF-Y-MOVE",
                        "OutputFeederY 이동 명령이 실패했습니다. result=" + result +
                        ", alarm=" + FeederY.IsAlarm +
                        FormatAxisLastMotionFailure());

                AxisMoveWaitResult waitResult = await WaitBinFeederYMoveDoneInPosition(targetPos, ResolveBinFeederYMoveTimeoutMs()).ConfigureAwait(false);
                if (!waitResult.Success)
                    return RaiseFeederAlarm(
                        AxisMoveWaiter.ResolveAlarmCode("BF-Y-MOVE", waitResult),
                        "OutputFeederY 이동 완료 확인이 실패했습니다. target=" + targetPos + ". " +
                        AxisMoveWaiter.FormatResult(waitResult, DescribeBinFeederYMoveDoneState()));

                return 0;
            }
            catch (Exception ex)
            {
                return RaiseFeederAlarm("BF-Y-MOVE-EX", "OutputFeederY 이동 중 예외가 발생했습니다. " + ex.Message);
            }
        }

        public Task<int> MoveBinFeederYToTeachingPosition(string positionName, bool bFine = false)
        {
            return MoveBinFeederYNamedPositionAsync(GetTeachingPosition(positionName), "OutputFeederY." + positionName, bFine);
        }

        public Task<int> MoveToFeederAvoidPosition(bool bFine = false) { return MoveBinFeederYNamedPositionAsync(Recipe.AvoidPosition, "OutputFeederY.AvoidPosition", bFine); }
        public Task<int> MoveToBinFeederAvoidPosition(bool bFine = false) { return MoveToFeederAvoidPosition(bFine); }

        public Task<int> MoveToFeederCassetteLoadPosition(BinSide side, int slotIndex, bool bFine = false)
        {
            ValidateSlotIndex(slotIndex);
            return MoveBinFeederYNamedPositionAsync(GetSidePosition(side, FeederPositionType.CassetteLoad), BuildFeederTargetName(side, "CassetteLoadPosition"), bFine);
        }

        public Task<int> MoveToBinFeederCassetteLoadPosition(int slotIndex, bool bFine = false)
        {
            return MoveToFeederCassetteLoadPosition(BinSide.Good, slotIndex, bFine);
        }

        public Task<int> MoveToFeederCassetteUnloadPosition(BinSide side, int slotIndex, bool bFine = false)
        {
            ValidateSlotIndex(slotIndex);
            return MoveBinFeederYNamedPositionAsync(GetSidePosition(side, FeederPositionType.CassetteUnload), BuildFeederTargetName(side, "CassetteUnloadPosition"), bFine);
        }

        public Task<int> MoveToFeederBarcodePosition(BinSide side, bool bFine = false)
        {
            return MoveBinFeederYNamedPositionAsync(GetSidePosition(side, FeederPositionType.Barcode), BuildFeederTargetName(side, "WaferBarcodePosition"), bFine);
        }

        public Task<int> MoveToFeederStageLoadPosition(BinSide side, bool bFine = false)
        {
            return MoveBinFeederYNamedPositionAsync(GetSidePosition(side, FeederPositionType.StageLoad), BuildFeederTargetName(side, "WaferLoadPosition"), bFine);
        }

        public Task<int> MoveToFeederStageLoadAvoidPosition(BinSide side, bool bFine = false)
        {
            return MoveBinFeederYNamedPositionAsync(GetStageLoadAvoidPosition(side), BuildFeederTargetName(side, "WaferLoadAvoidPosition"), bFine);
        }

        public Task<int> MoveToFeederStageUnloadPosition(BinSide side, bool bFine = false)
        {
            return MoveBinFeederYNamedPositionAsync(GetSidePosition(side, FeederPositionType.StageUnload), BuildFeederTargetName(side, "WaferUnloadPosition"), bFine);
        }

        public Task<int> MoveToFeederStageUnloadAvoidPosition(BinSide side, bool bFine = false)
        {
            return MoveBinFeederYNamedPositionAsync(GetStageUnloadAvoidPosition(side), BuildFeederTargetName(side, "WaferUnloadAvoidPosition"), bFine);
        }

        public Task<int> MoveToFeederExchangePosition(BinSide side, bool bFine = false)
        {
            double position = GetSidePosition(side, FeederPositionType.Exchange);
            if (Math.Abs(position) <= double.Epsilon)
                position = Recipe.AvoidPosition;
            return MoveBinFeederYNamedPositionAsync(position, BuildFeederTargetName(side, "CassetteExchangePosition"), bFine);
        }

        public Task<int> MoveToBinFeederCassetteInsertPosition(bool bFine = false)
        {
            return MoveToFeederExchangePosition(BinSide.Good, bFine);
        }

        public Task<int> MoveToBinFeederGoodStageExchangePosition(bool bFine = false)
        {
            return MoveToFeederStageLoadPosition(BinSide.Good, bFine);
        }

        public Task<int> MoveToBinFeederNgStageExchangePosition(bool bFine = false)
        {
            return MoveToFeederStageLoadPosition(BinSide.Ng, bFine);
        }

        public Task<int> MoveToBinFeederStageExchangePosition(TargetCassette cassette, bool bFine = false)
        {
            return MoveToFeederStageLoadPosition(ToBinSide(cassette), bFine);
        }

        private async Task<int> MoveBinFeederYNamedPositionAsync(double targetPosition, string targetName, bool bFine)
        {
            using (MotionGuardRuntime.BeginAxisTeachingMove(FeederY, targetPosition, targetName))
            {
                return await MoveBinFeederYAsync(targetPosition, bFine).ConfigureAwait(false);
            }
        }

        private static string BuildFeederTargetName(BinSide side, string suffix)
        {
            return "OutputFeederY." + (side == BinSide.Ng ? "NG" : "Good") + suffix;
        }

        public async Task<bool> WaitBinFeederYMoveDone(int timeoutMs)
        {
            return await WaitBinFeederYMoveDone(timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> WaitBinFeederYMoveDone(int timeoutMs, CancellationToken ct)
        {
            try
            {
                AxisMoveWaitResult waitResult = await WaitBinFeederYMoveDoneInPosition(FeederY.CommandPosition, timeoutMs, ct).ConfigureAwait(false);
                return waitResult.Success;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "OutputFeederWait",
                    "BinFeederY move wait failed. error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public async Task<AxisMoveWaitResult> WaitBinFeederYMoveDoneInPosition(double targetPos, int timeoutMs)
        {
            return await WaitBinFeederYMoveDoneInPosition(targetPos, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<AxisMoveWaitResult> WaitBinFeederYMoveDoneInPosition(double targetPos, int timeoutMs, CancellationToken ct)
        {
            try
            {
                AxisMoveWaitResult waitResult = await AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                    FeederY,
                    targetPos,
                    ResolveBinFeederYInPositionTolerance(),
                    timeoutMs > 0 ? timeoutMs : ResolveBinFeederYMoveTimeoutMs(),
                    0,
                    ct).ConfigureAwait(false);
                if (!waitResult.Success)
                    return waitResult;

                if (IsFeederOverload())
                    return new AxisMoveWaitResult(
                        AxisMoveWaitFailure.Alarm,
                        "BinFeeder overload is detected.",
                        waitResult.AxisState + ", overload=True");

                return waitResult;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "OutputFeederWait",
                    "BinFeederY move/in-position wait failed. target=" + targetPos +
                    ", error=" + ex.Message + " - Failed");
                return new AxisMoveWaitResult(
                    AxisMoveWaitFailure.Timeout,
                    "BinFeederY move wait exception: " + ex.Message,
                    DescribeBinFeederYMoveDoneState());
            }
            finally
            {
            }
        }

        public bool IsBinFeederYMoveDone()
        {
            return FeederY != null &&
                   !FeederY.IsMoving &&
                   FeederY.IsInPosition &&
                   !FeederY.IsAlarm &&
                   !IsFeederOverload();
        }

        public string DescribeBinFeederYMoveDoneState()
        {
            if (FeederY == null)
                return "FeederY=null";

            string actual = FeederY.ActualPosition.ToString("0.###", CultureInfo.InvariantCulture);
            string command = FeederY.CommandPosition.ToString("0.###", CultureInfo.InvariantCulture);
            string error = (FeederY.CommandPosition - FeederY.ActualPosition).ToString("0.###", CultureInfo.InvariantCulture);

            return "servo=" + FeederY.IsServoOn +
                   ", moving=" + FeederY.IsMoving +
                   ", inPositionFlag=" + FeederY.IsInPosition +
                   ", axisAlarm=" + FeederY.IsAlarm +
                   ", overload=" + IsFeederOverload() +
                   ", actual=" + actual +
                   ", command=" + command +
                   ", error=" + error;
        }

        public string DescribeBinFeederYLastMotionFailure()
        {
            return FormatAxisLastMotionFailure();
        }

        private string FormatAxisLastMotionFailure()
        {
            if (FeederY == null ||
                FeederY.LastMotionFailureCode == 0 ||
                string.IsNullOrWhiteSpace(FeederY.LastMotionFailureMessage))
                return string.Empty;

            return ", 마지막 이동 실패 원인=" + FeederY.LastMotionFailureMessage;
        }

        public string DescribeFeederCylinderState()
        {
            return "liftUp=" + IsFeederUp() +
                   ", liftDown=" + IsFeederDown() +
                   ", unclamp=" + IsFeederUnclamped() +
                   ", clamp=" + (!IsFeederUnclamped()) +
                   ", ringOn=" + IsFeederRingDetected(true) +
                   ", ringOff=" + IsFeederRingDetected(false) +
                   ", overload=" + IsFeederOverload();
        }

        public async Task<bool> WaitBinFeederYInPosition(string positionName, int timeoutMs)
        {
            double target = GetTeachingPosition(positionName);
            int timeout = timeoutMs > 0 ? timeoutMs : ResolveBinFeederYMoveTimeoutMs();
            AxisMoveWaitResult waitResult = await WaitBinFeederYMoveDoneInPosition(target, timeout).ConfigureAwait(false);
            return waitResult.Success;
        }

        public bool IsBinFeederYInPosition(double targetPos, double tolerance)
        {
            return Math.Abs(FeederY.ActualPosition - targetPos) <= tolerance;
        }

        public bool IsBinFeederYInAvoidPosition() { return IsBinFeederInAvoidPosition(); }
        public bool IsBinFeederInAvoidPosition() { return IsBinFeederYInPosition(Recipe.AvoidPosition, ResolveBinFeederYInPositionTolerance()); }
        public bool IsBinFeederYInCassetteLoadPosition(BinSide side) { return IsBinFeederYInPosition(GetSidePosition(side, FeederPositionType.CassetteLoad), ResolveBinFeederYInPositionTolerance()); }
        public bool IsBinFeederInCassetteLoadPosition(int slotIndex) { return IsBinFeederYInCassetteLoadPosition(BinSide.Good); }
        public bool IsBinFeederYInCassetteUnloadPosition(BinSide side) { return IsBinFeederYInPosition(GetSidePosition(side, FeederPositionType.CassetteUnload), ResolveBinFeederYInPositionTolerance()); }
        public bool IsBinFeederYInStageLoadPosition(BinSide side) { return IsBinFeederYInPosition(GetSidePosition(side, FeederPositionType.StageLoad), ResolveBinFeederYInPositionTolerance()); }
        public bool IsBinFeederYInStageLoadAvoidPosition(BinSide side) { return IsBinFeederYInPosition(GetStageLoadAvoidPosition(side), ResolveBinFeederYInPositionTolerance()); }
        public bool IsBinFeederYInStageUnloadPosition(BinSide side) { return IsBinFeederYInPosition(GetSidePosition(side, FeederPositionType.StageUnload), ResolveBinFeederYInPositionTolerance()); }
        public bool IsBinFeederYInStageUnloadAvoidPosition(BinSide side) { return IsBinFeederYInPosition(GetStageUnloadAvoidPosition(side), ResolveBinFeederYInPositionTolerance()); }
        public bool IsBinFeederYInBarcodePosition(BinSide side) { return IsBinFeederYInPosition(GetSidePosition(side, FeederPositionType.Barcode), ResolveBinFeederYInPositionTolerance()); }
        public bool IsBinFeederYInExchangePosition(BinSide side) { return IsBinFeederYInPosition(GetSidePosition(side, FeederPositionType.Exchange), ResolveBinFeederYInPositionTolerance()); }

        public bool IsFeederUp()
        {
            if (ShouldReadCylinderStateForSimulation(FeederUpDownCyl))
                return FeederUpDownCyl.IsFwd;

            return BinFeederUpSensor != null && BinFeederUpSensor.IsOn;
        }

        public bool IsFeederDown()
        {
            if (ShouldReadCylinderStateForSimulation(FeederUpDownCyl))
                return FeederUpDownCyl.IsBwd ||
                       (ShouldUseVirtualDryRunDefaults(FeederUpDownCyl) && IsCylinderStateUnknown(FeederUpDownCyl));

            return BinFeederDownSensor != null && BinFeederDownSensor.IsOn;
        }

        public bool IsFeederUnclamped()
        {
            if (ShouldReadCylinderStateForSimulation(FeederClampCyl))
                return FeederClampCyl.IsBwd ||
                       (ShouldUseVirtualDryRunDefaults(FeederClampCyl) &&
                        IsCylinderStateUnknown(FeederClampCyl) &&
                        IsFeederTransferDataEmpty());

            return BinFeederUnclampSensor != null && BinFeederUnclampSensor.IsOn;
        }

        public bool IsFeederOverload()
        {
            if (ShouldUseVirtualDryRunDefaults(null))
                return false;

            if (BinFeederOverloadSensor == null)
                return false;

            // Overload input is NC: ON means normal, OFF means overload.
            return !BinFeederOverloadSensor.IsOn;
        }

        public bool IsFeederRingDetected(bool expected = true)
        {
            if (IsOutputFeederSimulationOrDryRun())
                return IsFeederTransferDataOccupied() == expected;

            return BinFeederRingCheckSensor != null && BinFeederRingCheckSensor.IsOn == expected;
        }

        public bool IsFeederEmpty()
        {
            if (IsOutputFeederSimulationOrDryRun())
                return IsFeederTransferDataEmpty();

            return IsFeederTransferDataEmpty() && IsFeederRingDetected(false);
        }

        public bool IsFeederOccupied()
        {
            return HasWaferOnFeeder();
        }
        public bool IsBinFeederUp() { return IsFeederUp(); }
        public bool IsBinFeederDown() { return IsFeederDown(); }
        public bool IsBinFeederUnclamp() { return IsFeederUnclamped(); }
        public bool IsBinFeederRingCheck() { return IsFeederRingDetected(true); }
        public bool IsBinFeederClamp() { return !IsFeederUnclamped(); }
        public bool HasWaferOnFeeder()
        {
            if (IsOutputFeederSimulationOrDryRun())
                return IsFeederTransferDataOccupied();

            return IsFeederTransferDataOccupied() && IsFeederRingDetected(true);
        }

        public void TeachBinFeederYPosition(string positionName)
        {
            SetTeachingPosition(positionName, FeederY.ActualPosition);
            EventLogger.Write(EventKind.Event, "QMC", "BF-TEACH", "Teaching saved: " + positionName + "=" + FeederY.ActualPosition);
        }

        public void TeachBinFeederYAvoidPosition() { Recipe.AvoidPosition = FeederY.ActualPosition; }
        public void TeachBinFeederAvoidPosition() { TeachBinFeederYAvoidPosition(); }
        public void TeachBinFeederYCassetteLoadPosition(BinSide side) { SetSidePosition(side, FeederPositionType.CassetteLoad, FeederY.ActualPosition); }
        public void TeachBinFeederYCassetteUnloadPosition(BinSide side) { SetSidePosition(side, FeederPositionType.CassetteUnload, FeederY.ActualPosition); }
        public void TeachBinFeederYStageLoadPosition(BinSide side) { SetSidePosition(side, FeederPositionType.StageLoad, FeederY.ActualPosition); }
        public void TeachBinFeederYStageUnloadPosition(BinSide side) { SetSidePosition(side, FeederPositionType.StageUnload, FeederY.ActualPosition); }
        public void TeachBinFeederYBarcodePosition(BinSide side) { SetSidePosition(side, FeederPositionType.Barcode, FeederY.ActualPosition); }
        public void TeachBinFeederYExchangePosition(BinSide side) { SetSidePosition(side, FeederPositionType.Exchange, FeederY.ActualPosition); }
        public void TeachBinFeederGoodStageExchangePosition() { TeachBinFeederYStageLoadPosition(BinSide.Good); }
        public void TeachBinFeederNgStageExchangePosition() { TeachBinFeederYStageLoadPosition(BinSide.Ng); }
        public void TeachBinFeederCassetteInsertPosition() { TeachBinFeederYExchangePosition(BinSide.Good); }
        public void TeachBinFeederCassetteLoadBasePosition() { TeachBinFeederYCassetteLoadPosition(BinSide.Good); }

        public double CalculateBinFeederCassetteLoadPosition(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return Recipe.GoodCassetteLoadPosition;
        }

        public bool ValidateBinFeederTeachingComplete()
        {
            string reason;
            bool complete = ValidateBinFeederTeachingComplete(out reason);
            if (!complete)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "BF-TEACH-CHECK",
                    "OutputFeeder teaching check failed. " + reason);
            }

            return complete;
        }

        public bool ValidateBinFeederTeachingComplete(out string reason)
        {
            string goodReason;
            if (!ValidateBinFeederYTeachingComplete(BinSide.Good, out goodReason))
            {
                reason = goodReason;
                return false;
            }

            string ngReason;
            if (!ValidateBinFeederYTeachingComplete(BinSide.Ng, out ngReason))
            {
                reason = ngReason;
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool ValidateBinFeederYTeachingComplete(BinSide side)
        {
            string reason;
            bool complete = ValidateBinFeederYTeachingComplete(side, out reason);
            if (!complete)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "BF-TEACH-CHECK",
                    "OutputFeederY teaching check failed. " + reason);
            }

            return complete;
        }

        public bool ValidateBinFeederYTeachingComplete(BinSide side, out string reason)
        {
            reason = string.Empty;

            if (Recipe == null)
            {
                reason = "Recipe is null. side=" + side;
                return false;
            }

            double avoidPosition = Recipe.AvoidPosition;
            double cassetteLoad = GetSidePosition(side, FeederPositionType.CassetteLoad);
            double cassetteUnload = GetSidePosition(side, FeederPositionType.CassetteUnload);
            double stageLoad = GetSidePosition(side, FeederPositionType.StageLoad);
            double stageUnload = GetSidePosition(side, FeederPositionType.StageUnload);

            if (cassetteLoad == avoidPosition)
            {
                reason = "CassetteLoad teaching position is same as AvoidPosition. " +
                         BuildTeachingPositionText(side, cassetteLoad, cassetteUnload, stageLoad, stageUnload, avoidPosition);
                return false;
            }

            if (cassetteUnload == avoidPosition)
            {
                reason = "CassetteUnload teaching position is same as AvoidPosition. " +
                         BuildTeachingPositionText(side, cassetteLoad, cassetteUnload, stageLoad, stageUnload, avoidPosition);
                return false;
            }

            //이건 같아도 된다. 
            //if (stageLoad == stageUnload)
            //{
            //    reason = "StageLoad teaching position is same as StageUnload teaching position. " +
            //             BuildTeachingPositionText(side, cassetteLoad, cassetteUnload, stageLoad, stageUnload, avoidPosition);
            //    return false;
            //}

            return true;
        }

        private static string BuildTeachingPositionText(
            BinSide side,
            double cassetteLoad,
            double cassetteUnload,
            double stageLoad,
            double stageUnload,
            double avoidPosition)
        {
            return "side=" + side +
                   ", CassetteLoad=" + FormatTeachingPosition(cassetteLoad) +
                   ", CassetteUnload=" + FormatTeachingPosition(cassetteUnload) +
                   ", StageLoad=" + FormatTeachingPosition(stageLoad) +
                   ", StageUnload=" + FormatTeachingPosition(stageUnload) +
                   ", Avoid=" + FormatTeachingPosition(avoidPosition);
        }

        private static string FormatTeachingPosition(double position)
        {
            return position.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public double GetBinFeederYTeachingPosition(string positionName) { return GetTeachingPosition(positionName); }

        public async Task<int> MoveToTeachingPositionAndVerify(string positionName, bool bFine = false)
        {
            int result = await MoveBinFeederYToTeachingPosition(positionName, bFine);
            if (result != 0)
                return result;

            if (!await WaitBinFeederYInPosition(positionName, ResolveBinFeederYMoveTimeoutMs()))
                return RaiseFeederAlarm("BF-TEACH-INPOS", "OutputFeederY teaching position timeout: " + positionName);

            return 0;
        }

        public void SetFeederLiftUpOutput(bool on) { SetExclusiveOutput(BinFeederUpOut, BinFeederDownOut, on, "BF-LIFT-UP-OUT"); }
        public void SetFeederLiftDownOutput(bool on) { SetExclusiveOutput(BinFeederDownOut, BinFeederUpOut, on, "BF-LIFT-DOWN-OUT"); }
        public void SetFeederClampOutput(bool on) { SetExclusiveOutput(BinFeederClampOut, BinFeederUnclampOut, on, "BF-CLAMP-OUT"); }
        public void SetFeederUnclampOutput(bool on) { SetExclusiveOutput(BinFeederUnclampOut, BinFeederClampOut, on, "BF-UNCLAMP-OUT"); }

        public Task<int> SetBinFeederUpDown(bool up) { return SetFeederUpDownAsync(up, ResolveLiftTimeoutMs(up)); }
        public Task<int> FeederLiftUp(int timeoutMs) { return SetFeederUpDownAsync(true, timeoutMs); }
        public Task<int> FeederLiftDown(int timeoutMs) { return SetFeederUpDownAsync(false, timeoutMs); }
        public Task<int> ManualFeederLiftUp(int timeoutMs) { return FeederLiftUp(timeoutMs); }
        public Task<int> ManualFeederLiftDown(int timeoutMs) { return FeederLiftDown(timeoutMs); }

        public async Task<int> SetFeederUpDownAsync(bool up, int timeoutMs)
        {
            return await SetFeederUpDownAsync(up, timeoutMs, CancellationToken.None);
        }

        public async Task<int> SetFeederUpDownAsync(bool up, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (IsFeederOverload())
                    return RaiseFeederAlarm("BF-LIFT-OVERLOAD", "OutputFeeder overload is detected.");

                bool ok = up ? await FeederUpDownCyl.MoveFwdAsync(ct) : await FeederUpDownCyl.MoveBwdAsync(ct);
                if (!ok)
                    return RaiseFeederAlarm(up ? "BF-LIFT-UP" : "BF-LIFT-DOWN", "OutputFeeder lift cylinder move failed.");

                ApplyOutputFeederLiftSensorSimulation(up);

                bool sensorOk = up ? await WaitFeederUp(timeoutMs, ct) : await WaitFeederDown(timeoutMs, ct);
                if (!sensorOk)
                    return RaiseFeederAlarm(up ? "BF-LIFT-UP-TIMEOUT" : "BF-LIFT-DOWN-TIMEOUT", "OutputFeeder lift sensor timeout.");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return RaiseFeederAlarm("BF-LIFT-EX", "OutputFeeder lift exception: " + ex.Message);
            }
            finally
            {
            }
        }

        public Task<int> SetBinFeederClamp(bool clamp) { return SetFeederClampAsync(clamp, ResolveClampTimeoutMs(clamp)); }
        public Task<int> FeederClamp(int timeoutMs) { return SetFeederClampAsync(true, timeoutMs); }
        public Task<int> FeederUnclamp(int timeoutMs) { return SetFeederClampAsync(false, timeoutMs); }
        public Task<int> ManualFeederClamp(int timeoutMs) { return FeederClamp(timeoutMs); }
        public Task<int> ManualFeederUnclamp(int timeoutMs) { return FeederUnclamp(timeoutMs); }

        public async Task<int> SetFeederClampAsync(bool clamp, int timeoutMs)
        {
            return await SetFeederClampAsync(clamp, timeoutMs, CancellationToken.None);
        }

        public async Task<int> SetFeederClampAsync(bool clamp, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                bool ok = clamp ? await FeederClampCyl.MoveFwdAsync(ct) : await FeederClampCyl.MoveBwdAsync(ct);
                if (!ok)
                    return RaiseFeederAlarm(clamp ? "BF-CLAMP" : "BF-UNCLAMP", "OutputFeeder clamp cylinder move failed.");

                ApplyOutputFeederClampSensorSimulation(clamp);

                bool sensorOk = clamp ? await WaitFeederClamped(timeoutMs, ct) : await WaitFeederUnclamped(timeoutMs, ct);
                if (!sensorOk)
                    return RaiseFeederAlarm(clamp ? "BF-CLAMP-TIMEOUT" : "BF-UNCLAMP-TIMEOUT", "OutputFeeder clamp sensor timeout.");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return RaiseFeederAlarm("BF-CLAMP-EX", "OutputFeeder clamp exception: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<bool> WaitFeederUp(int timeoutMs)
        {
            return await WaitFeederUp(timeoutMs, CancellationToken.None);
        }

        public async Task<bool> WaitFeederUp(int timeoutMs, CancellationToken ct)
        {
            if (ShouldBypassInputWaitInSimulation(BinFeederUpSensor))
                return true;
            return await BinFeederUpSensor.WaitUntilStateAsync(true, timeoutMs, ct);
        }

        public async Task<bool> WaitFeederDown(int timeoutMs)
        {
            return await WaitFeederDown(timeoutMs, CancellationToken.None);
        }

        public async Task<bool> WaitFeederDown(int timeoutMs, CancellationToken ct)
        {
            if (ShouldBypassInputWaitInSimulation(BinFeederDownSensor))
                return true;
            return await BinFeederDownSensor.WaitUntilStateAsync(true, timeoutMs, ct);
        }

        public async Task<bool> WaitFeederClamped(int timeoutMs)
        {
            return await WaitFeederClamped(timeoutMs, CancellationToken.None);
        }

        public async Task<bool> WaitFeederClamped(int timeoutMs, CancellationToken ct)
        {
            if (ShouldBypassInputWaitInSimulation(BinFeederUnclampSensor))
                return true;
            return await BinFeederUnclampSensor.WaitUntilStateAsync(false, timeoutMs, ct);
        }

        public async Task<bool> WaitFeederUnclamped(int timeoutMs)
        {
            return await WaitFeederUnclamped(timeoutMs, CancellationToken.None);
        }

        public async Task<bool> WaitFeederUnclamped(int timeoutMs, CancellationToken ct)
        {
            if (ShouldBypassInputWaitInSimulation(BinFeederUnclampSensor))
                return true;
            return await BinFeederUnclampSensor.WaitUntilStateAsync(true, timeoutMs, ct);
        }

        public async Task<bool> WaitFeederRingState(bool expected, int timeoutMs)
        {
            return await WaitFeederRingState(expected, timeoutMs, CancellationToken.None);
        }

        public async Task<bool> WaitFeederRingState(bool expected, int timeoutMs, CancellationToken ct)
        {
            if (ShouldBypassInputWaitInSimulation(BinFeederRingCheckSensor))
                return IsFeederTransferDataOccupied() == expected;
            return await BinFeederRingCheckSensor.WaitUntilStateAsync(expected, timeoutMs, ct);
        }
        public async Task<bool> WaitBinFeederUp(int timeoutMs) { return await WaitFeederUp(timeoutMs); }
        public async Task<bool> WaitBinFeederDown(int timeoutMs) { return await WaitFeederDown(timeoutMs); }
        public async Task<bool> WaitBinFeederUnclamp(int timeoutMs) { return await WaitFeederUnclamped(timeoutMs); }
        public async Task<bool> WaitBinFeederClamp(int timeoutMs) { return await WaitFeederClamped(timeoutMs); }
        public async Task<bool> WaitBinFeederRingClear(int timeoutMs) { return await WaitFeederRingState(false, timeoutMs); }
        public async Task<bool> WaitBinFeederUp(int timeoutMs, CancellationToken ct) { return await WaitFeederUp(timeoutMs, ct); }
        public async Task<bool> WaitBinFeederDown(int timeoutMs, CancellationToken ct) { return await WaitFeederDown(timeoutMs, ct); }
        public async Task<bool> WaitBinFeederUnclamp(int timeoutMs, CancellationToken ct) { return await WaitFeederUnclamped(timeoutMs, ct); }
        public async Task<bool> WaitBinFeederClamp(int timeoutMs, CancellationToken ct) { return await WaitFeederClamped(timeoutMs, ct); }
        public async Task<bool> WaitBinFeederRingClear(int timeoutMs, CancellationToken ct) { return await WaitFeederRingState(false, timeoutMs, ct); }
        private void ApplyOutputFeederLiftSensorSimulation(bool up)
        {
            if (!ShouldApplyInputSimulation(BinFeederUpSensor) &&
                !ShouldApplyInputSimulation(BinFeederDownSensor))
                return;

            SimulateInputIfAllowed(BinFeederUpSensor, up);
            SimulateInputIfAllowed(BinFeederDownSensor, !up);
        }

        private void ApplyOutputFeederClampSensorSimulation(bool clamp)
        {
            if (!ShouldApplyInputSimulation(BinFeederUnclampSensor))
                return;

            SimulateInputIfAllowed(BinFeederUnclampSensor, !clamp);
        }

        private void InitializeOutputFeederSimulationInputs()
        {
            // Overload is NC, so the simulation default must be ON(normal).
            SimulateInputIfAllowed(BinFeederOverloadSensor, true);
        }

        private bool ShouldBypassInputWaitInSimulation(BaseDigitalInput input)
        {
            return IsOutputFeederSimulationOrDryRun() && !CanSimulateInput(input);
        }

        private bool ShouldApplyInputSimulation(BaseDigitalInput input)
        {
            return CanSimulateInput(input) &&
                   (IsOutputFeederSimulationOrDryRun() ||
                    IsCylinderSimulation(FeederUpDownCyl) ||
                    IsCylinderSimulation(FeederClampCyl));
        }

        private bool ShouldReadCylinderStateForSimulation(BaseCylinder cylinder)
        {
            return IsOutputFeederSimulationOrDryRun() &&
                   (IsCylinderSimulation(cylinder) || IsCylinderInputWaitIgnored(cylinder));
        }

        private bool ShouldUseVirtualDryRunDefaults(BaseCylinder cylinder)
        {
            AppSettings settings = AppSettingsStore.Current;
            bool appVirtual = settings != null && (settings.BypassHardware || settings.SimulationMode || settings.DryRunMode);
            bool cylinderSimulation = cylinder != null && cylinder.Config != null && cylinder.Config.IsSimulationMode;
            bool inputWaitIgnored = cylinder != null && cylinder.Config != null && cylinder.Config.IgnoreInputWaits;
            return appVirtual || cylinderSimulation || inputWaitIgnored || !AjinFactory.IsRealBoardReady;
        }

        private static bool CanSimulateInput(BaseDigitalInput input)
        {
            return input != null && input.Config != null && input.Config.IsSimulationMode;
        }

        private static bool IsCylinderSimulation(BaseCylinder cylinder)
        {
            return cylinder != null && cylinder.Config != null && cylinder.Config.IsSimulationMode;
        }

        private static bool IsCylinderStateUnknown(BaseCylinder cylinder)
        {
            return cylinder == null || (!cylinder.IsFwd && !cylinder.IsBwd);
        }

        private static bool IsCylinderInputWaitIgnored(BaseCylinder cylinder)
        {
            return cylinder != null && cylinder.Config != null && cylinder.Config.IgnoreInputWaits;
        }

        private static void SimulateInputIfAllowed(BaseDigitalInput input, bool state)
        {
            if (CanSimulateInput(input))
                input.SimulateInput(state);
        }

        public void ManualMoveBinFeederYJog(Direction dir, double speed) { FeederY.MoveJogContinuous((int)dir, JogSpeedType.Custom, speed); }
        public void ManualMoveBinFeederYJog(int direction, double speed) { ManualMoveBinFeederYJog(direction < 0 ? Direction.Minus : Direction.Plus, speed); }
        public void ManualStopBinFeederY() { FeederY.StopJog(); }
        public Task<int> ManualMoveToFeederAvoidPosition(bool bFine = false) { return MoveToFeederAvoidPosition(bFine); }
        public Task<int> ManualMoveToBinFeederAvoidPosition(bool bFine = false) { return ManualMoveToFeederAvoidPosition(bFine); }
        public Task<int> ManualMoveToFeederCassetteLoadPosition(BinSide side, int slotIndex, bool bFine = false) { return MoveToFeederCassetteLoadPosition(side, slotIndex, bFine); }
        public Task<int> ManualMoveToBinFeederCassetteLoadPosition(int slotIndex, bool bFine = false) { return MoveToBinFeederCassetteLoadPosition(slotIndex, bFine); }
        public Task<int> ManualMoveToFeederCassetteUnloadPosition(BinSide side, int slotIndex, bool bFine = false) { return MoveToFeederCassetteUnloadPosition(side, slotIndex, bFine); }
        public Task<int> ManualMoveToFeederStageLoadPosition(BinSide side, bool bFine = false) { return MoveToFeederStageLoadPosition(side, bFine); }
        public Task<int> ManualMoveToFeederStageUnloadPosition(BinSide side, bool bFine = false) { return MoveToFeederStageUnloadPosition(side, bFine); }
        public Task<int> ManualMoveToFeederBarcodePosition(BinSide side, bool bFine = false) { return MoveToFeederBarcodePosition(side, bFine); }
        public Task<int> ManualMoveToFeederExchangePosition(BinSide side, bool bFine = false) { return MoveToFeederExchangePosition(side, bFine); }
        public Task<int> ManualMoveToBinFeederCassetteInsertPosition(bool bFine = false) { return MoveToBinFeederCassetteInsertPosition(bFine); }

        public async Task<int> LoadFromCassetteToFeeder(BinSide side, int slotIndex, int timeoutMs, bool bFine = false, bool useBarcode = true)
        {
            if (!CheckFeederCassetteReady(side, slotIndex, TransferMode.Load))
                return RaiseFeederAlarm("BF-LOAD-READY", "OutputFeeder load transfer is not ready.");

            int result = await FeederUnclamp(timeoutMs);
            if (result != 0) return result;
            result = await FeederLiftDown(timeoutMs);
            if (result != 0) return result;

            result = await MoveToFeederCassetteLoadPosition(side, slotIndex, bFine);
            if (result != 0) return result;
            result = await WaitBinFeederYMoveDoneInPositionOrAlarm(
                GetSidePosition(side, FeederPositionType.CassetteLoad),
                timeoutMs,
                "BF-LOAD-Y",
                "OutputFeeder cassette load position",
                CancellationToken.None).ConfigureAwait(false);
            if (result != 0) return result;

            result = await FeederClamp(timeoutMs);
            if (result != 0) return result;
            if (!IsOutputFeederSimulationOrDryRun() && !IsFeederRingDetected(true))
                return RaiseFeederAlarm("BF-LOAD-RING", "OutputFeeder ring was not detected after cassette load.");

            UpdateFeederMaterialState(MaterialState.Occupied);
            if (useBarcode)
                await ReadFeederBarcode(side, timeoutMs, bFine, 2);
            return 0;
        }

        public async Task<int> LoadWaferFromCassetteToFeeder(int slotIndex, int timeoutMs, bool bFine = false)
        {
            return await LoadFromCassetteToFeeder(BinSide.Good, slotIndex, timeoutMs, bFine, false);
        }

        public async Task<string> ReadFeederBarcode(BinSide side, int timeoutMs, bool bFine = false, int retry = 2)
        {
            if (!IsFeederOccupied())
            {
                RaiseFeederAlarm("BF-BARCODE-EMPTY", "OutputFeeder has no ring for barcode read.");
                return string.Empty;
            }

            int result = await MoveToFeederBarcodePosition(side, bFine);
            if (result != 0)
                return string.Empty;

            result = await WaitBinFeederYMoveDoneInPositionOrAlarm(
                GetSidePosition(side, FeederPositionType.Barcode),
                timeoutMs,
                "BF-BARCODE",
                "OutputFeeder barcode position",
                CancellationToken.None).ConfigureAwait(false);
            if (result != 0)
            {
                return string.Empty;
            }

            return string.Empty;
        }

        public async Task<int> LoadWaferToStageFromFeeder(BinSide side, int timeoutMs, bool bFine = false, bool useVacuum = true)
        {
            if (!CheckFeederStageReady(side, TransferMode.Load))
                return RaiseFeederAlarm("BF-STAGE-LOAD-READY", "OutputStage load transfer is not ready.");

            if (IsFeederUnclamped())
                return RaiseFeederAlarm("BF-STAGE-LOAD-CLAMP", "OutputFeeder must already be clamped before stage load.");
            if (!IsFeederDown())
                return RaiseFeederAlarm("BF-STAGE-LOAD-DOWN", "OutputFeeder must already be down before stage load.");

            int result = await MoveToFeederStageLoadPosition(side, bFine);
            if (result != 0) return result;
            result = await WaitBinFeederYMoveDoneInPositionOrAlarm(
                GetSidePosition(side, FeederPositionType.StageLoad),
                timeoutMs,
                "BF-STAGE-LOAD-Y",
                "OutputFeeder stage load position",
                CancellationToken.None).ConfigureAwait(false);
            if (result != 0) return result;

            result = await FeederUnclamp(timeoutMs);
            if (result != 0) return result;

            result = await MoveToFeederStageLoadAvoidPosition(side, bFine);
            if (result != 0) return result;
            result = await WaitBinFeederYMoveDoneInPositionOrAlarm(
                GetStageLoadAvoidPosition(side),
                timeoutMs,
                "BF-STAGE-LOAD-AVOID-Y",
                "OutputFeeder stage load avoid position",
                CancellationToken.None).ConfigureAwait(false);
            if (result != 0) return result;

            if (!IsOutputFeederSimulationOrDryRun() && !await WaitFeederRingState(false, timeoutMs))
                return RaiseFeederAlarm("BF-STAGE-LOAD-RING", "OutputFeeder ring remained after stage load.");

            result = await FeederLiftUp(timeoutMs);
            if (result != 0) return result;

            result = await MoveToFeederAvoidPosition(bFine);
            if (result != 0) return result;
            result = await WaitBinFeederYMoveDoneInPositionOrAlarm(
                Recipe.AvoidPosition,
                timeoutMs,
                "BF-STAGE-LOAD-FEEDER-AVOID-Y",
                "OutputFeeder avoid position after stage load",
                CancellationToken.None).ConfigureAwait(false);
            if (result != 0) return result;

            result = await FeederLiftDown(timeoutMs);
            if (result != 0) return result;

            ClearFeederMaterialState();
            return 0;
        }

        public async Task<int> UnloadWaferFromStageToFeeder(BinSide side, int timeoutMs, bool bFine = false)
        {
            if (!CheckFeederStageReady(side, TransferMode.Unload))
                return RaiseFeederAlarm("BF-STAGE-UNLOAD-READY", "OutputStage unload transfer is not ready.");

            if (!IsFeederUnclamped())
            {
                int unclamp = await FeederUnclamp(timeoutMs);
                if (unclamp != 0) return unclamp;
            }

            if (!IsFeederDown())
            {
                int down = await FeederLiftDown(timeoutMs);
                if (down != 0) return down;
            }

            int result = await FeederLiftUp(timeoutMs);
            if (result != 0) return result;

            result = await MoveToFeederStageUnloadAvoidPosition(side, bFine);
            if (result != 0) return result;
            result = await WaitBinFeederYMoveDoneInPositionOrAlarm(
                GetStageUnloadAvoidPosition(side),
                timeoutMs,
                "BF-STAGE-UNLOAD-AVOID-Y",
                "OutputFeeder stage unload avoid position",
                CancellationToken.None).ConfigureAwait(false);
            if (result != 0) return result;

            result = await FeederLiftDown(timeoutMs);
            if (result != 0) return result;

            result = await MoveToFeederStageUnloadPosition(side, bFine);
            if (result != 0) return result;
            result = await WaitBinFeederYMoveDoneInPositionOrAlarm(
                GetSidePosition(side, FeederPositionType.StageUnload),
                timeoutMs,
                "BF-STAGE-UNLOAD-Y",
                "OutputFeeder stage unload position",
                CancellationToken.None).ConfigureAwait(false);
            if (result != 0) return result;

            result = await FeederClamp(timeoutMs);
            if (result != 0) return result;
            if (!IsOutputFeederSimulationOrDryRun() && !await WaitFeederRingState(true, timeoutMs))
                return RaiseFeederAlarm("BF-STAGE-UNLOAD-RING", "OutputFeeder ring was not detected after stage unload.");

            UpdateFeederMaterialState(MaterialState.Occupied);
            return 0;
        }

        public async Task<int> UnloadFeederToCassette(BinSide side, int slotIndex, int timeoutMs, bool bFine = false)
        {
            if (!CheckFeederCassetteReady(side, slotIndex, TransferMode.Unload))
                return RaiseFeederAlarm("BF-CST-UNLOAD-READY", "OutputFeeder cassette unload transfer is not ready.");

            if (IsFeederUnclamped())
                return RaiseFeederAlarm("BF-CST-UNLOAD-CLAMP", "OutputFeeder must already be clamped before cassette unload.");

            if (!IsFeederDown())
                return RaiseFeederAlarm("BF-CST-UNLOAD-DOWN", "OutputFeeder must already be down before cassette unload.");

            if (!IsOutputFeederSimulationOrDryRun() && !await WaitFeederRingState(true, timeoutMs))
                return RaiseFeederAlarm("BF-CST-UNLOAD-RING-DETECT", "OutputFeeder ring was not detected before cassette unload.");

            int result = await MoveToFeederCassetteUnloadPosition(side, slotIndex, bFine);
            if (result != 0) return result;
            result = await WaitBinFeederYMoveDoneInPositionOrAlarm(
                GetSidePosition(side, FeederPositionType.CassetteUnload),
                timeoutMs,
                "BF-CST-UNLOAD-Y",
                "OutputFeeder cassette unload position",
                CancellationToken.None).ConfigureAwait(false);
            if (result != 0) return result;

            result = await FeederUnclamp(timeoutMs);
            if (result != 0) return result;

            result = await MoveToFeederAvoidPosition(bFine);
            if (result != 0) return result;
            result = await WaitBinFeederYMoveDoneInPositionOrAlarm(
                Recipe.AvoidPosition,
                timeoutMs,
                "BF-CST-UNLOAD-AVOID-Y",
                "OutputFeeder cassette unload avoid position",
                CancellationToken.None).ConfigureAwait(false);
            if (result != 0) return result;

            if (!IsOutputFeederSimulationOrDryRun() && !await WaitFeederRingState(false, timeoutMs))
                return RaiseFeederAlarm("BF-CST-UNLOAD-RING", "OutputFeeder ring remained after cassette unload.");

            ClearFeederMaterialState();
            return 0;
        }

        public async Task<bool> ReturnWaferFromFeederToCassette(int slotIndex, int timeoutMs, bool bFine = false)
        {
            return await UnloadFeederToCassette(BinSide.Good, slotIndex, timeoutMs, bFine) == 0;
        }

        public async Task<int> MoveBinFeederToStageExchangePosition(TargetCassette cassette, int timeoutMs, bool bFine = false)
        {
            try
            {
                BinSide side = ToBinSide(cassette);
                int result = await MoveToFeederStageLoadPosition(side, bFine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await WaitBinFeederYMoveDoneInPositionOrAlarm(
                    GetSidePosition(side, FeederPositionType.StageLoad),
                    timeoutMs,
                    "BF-STAGE-EXCHANGE-Y",
                    "OutputFeeder stage exchange position",
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return RaiseFeederAlarm(
                    "BF-STAGE-EXCHANGE-EXCEPTION",
                    "OutputFeeder stage exchange position move failed. cassette=" + cassette +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExchangeFeederRingForNextSlot(BinSide side, int currentSlotIndex, int nextSlotIndex, int timeoutMs, bool bFine = false)
        {
            int result = await UnloadFeederToCassette(side, currentSlotIndex, timeoutMs, bFine);
            if (result != 0) return result;
            result = await LoadFromCassetteToFeeder(side, nextSlotIndex, timeoutMs, bFine, false);
            if (result != 0) return result;
            return await MoveToFeederExchangePosition(side, bFine);
        }

        public async Task<int> RecoverFeederToSafeState(int timeoutMs, bool moveAvoid = true)
        {
            try { FeederY.Stop(); } catch { }

            int result = await FeederUnclamp(timeoutMs);
            if (result != 0) return result;
            result = await FeederLiftDown(timeoutMs);
            if (result != 0) return result;

            if (moveAvoid)
            {
                result = await MoveToFeederAvoidPosition();
                if (result != 0) return result;
                result = await WaitBinFeederYMoveDoneInPositionOrAlarm(
                    Recipe.AvoidPosition,
                    timeoutMs,
                    "BF-RECOVER",
                    "OutputFeeder avoid position",
                    CancellationToken.None).ConfigureAwait(false);
                if (result != 0) return result;
            }

            return 0;
        }

        public async Task<int> RecoverBinFeederToSafeState(int timeoutMs, bool unclamp = true)
        {
            try
            {
                return await RecoverFeederToSafeState(timeoutMs, true).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return RaiseFeederAlarm("BF-RECOVER-EXCEPTION",
                    "OutputFeeder recover failed. error=" + ex.Message);
            }
            finally
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputFeeder",
                    "RecoverBinFeederToSafeState finished. unclamp=" + unclamp);
            }
        }

        public bool CheckBinFeederYMoveReady()
        {
            string reason;
            bool ready = CheckBinFeederYMoveReady(out reason);
            if (!ready)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "BF-Y-READY",
                    "OutputFeederY 이동 준비 확인이 실패했습니다. " + reason);
            }

            return ready;
        }

        public bool CheckBinFeederYMoveReady(out string reason)
        {
            reason = string.Empty;

            if (FeederY == null)
            {
                reason = "FeederY 객체가 없습니다.";
                return false;
            }

            if (!FeederY.IsServoOn)
            {
                reason = "FeederY 서보가 OFF입니다.";
                return false;
            }

            if (FeederY.IsAlarm)
            {
                reason = "FeederY 축 알람이 ON입니다.";
                return false;
            }

            if (FeederY.IsMoving)
            {
                reason = "FeederY가 이동 중입니다.";
                return false;
            }

            if (IsFeederOverload())
            {
                reason = "OutputFeeder 과부하 센서가 감지되었습니다. " + FormatInputState("Overload", BinFeederOverloadSensor);
                return false;
            }

            return true;
        }

        public bool CheckFeederMoveReady() { return CheckBinFeederYMoveReady(); }
        public bool CheckBinFeederMoveReady() { return CheckFeederMoveReady(); }

        public bool IsFeederTransferDataEmpty()
        {
            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) == null &&
                   CurrentMaterialState == MaterialState.Empty;
        }

        public bool IsFeederTransferDataOccupied()
        {
            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null ||
                   CurrentMaterialState != MaterialState.Empty &&
                   CurrentMaterialState != MaterialState.Error;
        }

        public bool IsOutputFeederSimulationOrDryRun()
        {
            AppSettings settings = AppSettingsStore.Current;
            bool simulation = Setup != null && Setup.IsSimulationMode;
            bool dryRun = Config != null && Config.bDryRun;
            bool globalDryRun = settings != null && (settings.BypassHardware || settings.DryRunMode);
            return globalDryRun || simulation || dryRun;
        }

        private static string FormatInputState(string name, BaseDigitalInput input)
        {
            if (input == null)
                return name + "=NULL";

            return name + "=" + (input.IsOn ? "ON" : "OFF");
        }

        public bool CheckFeederTransferReady(TransferMode mode)
        {
            string reason;
            bool ready = CheckFeederTransferReady(mode, out reason);
            if (!ready)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "BF-TRANSFER-READY",
                    "OutputFeeder transfer ready check failed. " + reason);
            }

            return ready;
        }

        public bool CheckFeederTransferReady(TransferMode mode, out string reason)
        {
            reason = string.Empty;

            string moveReason;
            if (!CheckBinFeederYMoveReady(out moveReason))
            {
                reason = moveReason;
                return false;
            }

            if (mode == TransferMode.Load)
            {
                if (!IsFeederDown())
                {
                    reason = "OutputFeeder lift is not DOWN.";
                    return false;
                }

                if (!IsFeederUnclamped())
                {
                    reason = "OutputFeeder clamp is not UNCLAMP.";
                    return false;
                }

                if (!IsFeederEmpty())
                {
                    reason = "OutputFeeder is not empty.";
                    return false;
                }

                return true;
            }

            if (mode == TransferMode.Unload)
            {
                if (!IsFeederOccupied())
                {
                    reason = "OutputFeeder does not have bin/material.";
                    return false;
                }

                return true;
            }

            return true;
        }

        public bool CheckBinFeederTransferReady(TransferMode mode) { return CheckFeederTransferReady(mode); }

        public bool CheckFeederCassetteReady(BinSide side, int slotIndex, TransferMode mode)
        {
            string reason;
            bool ready = CheckFeederCassetteReady(side, slotIndex, mode, out reason);
            if (!ready)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "BF-CST-READY",
                    "OutputFeeder cassette ready check failed. " + reason);
            }

            return ready;
        }

        public bool CheckFeederCassetteReady(BinSide side, int slotIndex, TransferMode mode, out string reason)
        {
            reason = string.Empty;

            if (slotIndex < 0)
            {
                reason = "Slot index is invalid. side=" + side + ", slotIndex=" + slotIndex + ", mode=" + mode;
                return false;
            }

            string teachingReason;
            if (!ValidateBinFeederYTeachingComplete(side, out teachingReason))
            {
                reason = teachingReason;
                return false;
            }

            string transferReason;
            if (!CheckFeederTransferReady(mode, out transferReason))
            {
                reason = transferReason;
                return false;
            }

            return true;
        }

        public bool CheckBinCassetteLoadReady(int slotIndex, TransferMode mode)
        {
            return CheckFeederCassetteReady(BinSide.Good, slotIndex, mode);
        }

        public bool CheckFeederStageReady(BinSide side, TransferMode mode)
        {
            string reason;
            bool ready = CheckFeederStageReady(side, mode, out reason);
            if (!ready)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "BF-STAGE-READY",
                    "OutputFeeder stage ready check failed. " + reason);
            }

            return ready;
        }

        public bool CheckFeederStageReady(BinSide side, TransferMode mode, out string reason)
        {
            reason = string.Empty;

            string teachingReason;
            if (!ValidateBinFeederYTeachingComplete(side, out teachingReason))
            {
                reason = teachingReason;
                return false;
            }

            if (mode == TransferMode.Load)
            {
                if (!IsFeederOccupied())
                {
                    reason = "OutputFeeder does not have bin/material for stage load. side=" + side;
                    return false;
                }

                return true;
            }

            if (mode == TransferMode.Unload)
            {
                if (!IsFeederEmpty())
                {
                    reason = "OutputFeeder is not empty for stage unload. side=" + side;
                    return false;
                }

                return true;
            }

            string moveReason;
            if (!CheckBinFeederYMoveReady(out moveReason))
            {
                reason = moveReason;
                return false;
            }

            return true;
        }

        public FeederTransferState GetFeederTransferState()
        {
            return new FeederTransferState
            {
                IsUp = IsFeederUp(),
                IsDown = IsFeederDown(),
                IsUnclamped = IsFeederUnclamped(),
                HasRing = IsFeederRingDetected(true),
                IsOverload = IsFeederOverload(),
                IsMoveReady = CheckBinFeederYMoveReady()
            };
        }

        public WaferFeederProcessState GetBinFeederProcessState()
        {
            if (FeederY.IsAlarm || IsFeederOverload()) return WaferFeederProcessState.Alarm;
            if (FeederY.IsMoving) return WaferFeederProcessState.Moving;
            if (IsFeederOccupied()) return WaferFeederProcessState.HasWafer;
            return WaferFeederProcessState.Empty;
        }

        public bool IsBinFeederSafe() { return !IsFeederOverload() && !FeederY.IsAlarm; }
        public bool InterlockBeforeJog() { return IsBinFeederSafe(); }
        public bool ValidateBinFeederBeforePickup(TransferPointType type)
        {
            if (!IsBinFeederSafe()) return false;
            if (type == TransferPointType.Cassette) return IsFeederDown() || IsBinFeederInCassetteLoadPosition(0);
            return true;
        }

        public bool ValidateFeederSidePosition(BinSide side, FeederPositionType type)
        {
            return ValidateBinFeederYTargetPosition(GetSidePosition(side, type));
        }

        public string ResolveFeederTeachingPositionName(BinSide side, FeederPositionType type)
        {
            string prefix = side == BinSide.Ng ? "NG" : "Good";
            switch (type)
            {
                // 피더 Avoid 위치명 반환
                case FeederPositionType.Avoid: return "35_BinFeederY.AvoidPos";
                // 카세트 로드 위치명 반환
                case FeederPositionType.CassetteLoad: return prefix + "CassetteLoadPosition";
                // 카세트 언로드 위치명 반환
                case FeederPositionType.CassetteUnload: return prefix + "CassetteUnloadPosition";
                // 바코드 위치명 반환
                case FeederPositionType.Barcode: return prefix + "WaferBarcodePosition";
                // 스테이지 로드 위치명 반환
                case FeederPositionType.StageLoad: return prefix + "WaferLoadPosition";
                // 스테이지 언로드 위치명 반환
                case FeederPositionType.StageUnload: return prefix + "WaferUnloadPosition";
                // 카세트 교체 위치명 반환
                case FeederPositionType.Exchange: return prefix + "CassetteExchangePosition";
                default: throw new ArgumentOutOfRangeException("type");
            }
        }

        public void RecordBinFeederPositionSnapshot(string key) { _positionSnapshots[key] = FeederY.ActualPosition; }

        public async Task<int> MoveToExchangePositionAsync()
        {
            int result = await FeederLiftUp(ResolveLiftTimeoutMs(true));
            if (result != 0) return result;
            result = await FeederClamp(ResolveClampTimeoutMs(true));
            if (result != 0) return result;
            return await MoveToFeederExchangePosition(BinSide.Good);
        }

        public async Task<int> RetractFeederAsync()
        {
            int result = await FeederUnclamp(ResolveClampTimeoutMs(false));
            if (result != 0) return result;
            result = await MoveToFeederAvoidPosition();
            if (result != 0) return result;
            return await FeederLiftDown(ResolveLiftTimeoutMs(false));
        }

        public void StopFeederMotionAndOutputs(string reason) { StopFeederMotionAndOutputs(reason, FeederSafePolicy.AllOff); }
        public void StopFeederMotionAndOutputs(string reason, FeederSafePolicy policy)
        {
            try
            {
                try { FeederY.StopJog(); } catch { }
                try { FeederY.Stop(); } catch { }
                SetFeederOutputsSafe(policy);
                EventLogger.Write(EventKind.Event, "QMC", "BF-STOP", "OutputFeeder stopped. reason=" + reason);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "BF-STOP", Name, "OutputFeeder stop failed: " + ex.Message);
            }
        }

        public void StopBinFeederMotion(string reason) { StopFeederMotionAndOutputs(reason); }

        public void SetFeederOutputsSafe(FeederSafePolicy policy)
        {
            switch (policy)
            {
                // 클램프 유지 안전 출력
                case FeederSafePolicy.HoldClamp:
                    SetFeederLiftUpOutput(false);
                    SetFeederLiftDownOutput(true);
                    break;
                // 현재 출력 상태 유지
                case FeederSafePolicy.HoldCurrent:
                    break;
                default:
                    SetFeederLiftUpOutput(false);
                    SetFeederLiftDownOutput(false);
                    SetFeederClampOutput(false);
                    SetFeederUnclampOutput(false);
                    break;
            }
        }

        public string BuildFeederAlarmMessage(FeederAlarmCode code)
        {
            switch (code)
            {
                // 피더 Y축 알람 메시지
                case FeederAlarmCode.AxisAlarm: return "OutputFeederY axis alarm.";
                // 피더 Y축 이동 타임아웃 메시지
                case FeederAlarmCode.MoveTimeout: return "OutputFeederY move timeout.";
                // 피더 티칭 누락 메시지
                case FeederAlarmCode.TeachingMissing: return "OutputFeeder teaching position is missing.";
                // 피더 인터락 메시지
                case FeederAlarmCode.Interlock: return "OutputFeeder interlock condition is not satisfied.";
                // 피더 오버로드 메시지
                case FeederAlarmCode.Overload: return "OutputFeeder overload input is OFF (NC input).";
                // 피더 링 감지 누락 메시지
                case FeederAlarmCode.RingMissing: return "OutputFeeder ring was not detected.";
                default: return "OutputFeeder alarm.";
            }
        }

        public void UpdateFeederMaterialState(MaterialState state)
        {
            CurrentMaterialState = state;
            EventLogger.Write(EventKind.Event, "QMC", "BF-MATERIAL", "OutputFeeder material state=" + state);
        }

        public void ClearFeederMaterialState() { UpdateFeederMaterialState(MaterialState.Empty); }

        private double ResolveBinFeederYMoveVelocity(bool bFine)
        {
            // Fine 이동은 JogFineVelocity 를 그대로 쓰고, 일반 이동만 DefaultVelocity 퍼센트 스케일을 적용한다.
            if (bFine && FeederY.Config.JogFineVelocity > 0.0)
                return FeederY.Config.JogFineVelocity;
            return MotionSpeedScale.ApplyDefaultVelocityScale(FeederY.Config.DefaultVelocity);
        }

        private int ResolveBinFeederYMoveTimeoutMs()
        {
            return FeederY.Setup.MoveTimeoutMs > 0 ? FeederY.Setup.MoveTimeoutMs : 60000;
        }

        private double ResolveBinFeederYInPositionTolerance()
        {
            return FeederY.Config.InPositionTolerance > 0.0 ? FeederY.Config.InPositionTolerance : 0.05;
        }

        private int ResolveLiftTimeoutMs(bool up)
        {
            return up ? FeederUpDownCyl.Recipe.FwdTimeoutMs : FeederUpDownCyl.Recipe.BwdTimeoutMs;
        }

        private int ResolveClampTimeoutMs(bool clamp)
        {
            return clamp ? FeederClampCyl.Recipe.FwdTimeoutMs : FeederClampCyl.Recipe.BwdTimeoutMs;
        }

        private bool ValidateBinFeederYTargetPosition(double targetPos)
        {
            if (FeederY == null || FeederY.Setup == null)
                return false;

            if (FeederY.Setup.SoftLimitPlus != 0.0 && targetPos > FeederY.Setup.SoftLimitPlus)
                return false;

            if (FeederY.Setup.SoftLimitMinus != 0.0 && targetPos < FeederY.Setup.SoftLimitMinus)
                return false;

            return true;
        }

        // 마지막 OutputFeederY 이동 실패 사유 — UI 실패 팝업에 합쳐 표시 (InputStage/InputFeeder와 동일 용도)
        public string LastBinFeederMoveFailureMessage { get; private set; }

        private int RaiseFeederAlarm(string code, string message)
        {
            LastBinFeederMoveFailureMessage = message;
            EventLogger.Write(EventKind.Alarm, "QMC", code, Name, message);
            AlarmManager.Raise(AlarmSeverity.Error, code, Name, message);
            Console.WriteLine("[ALARM] '" + Name + "' " + message);
            return -1;
        }

        private async Task<int> WaitBinFeederYMoveDoneInPositionOrAlarm(
            double target,
            int timeoutMs,
            string alarmPrefix,
            string description,
            CancellationToken ct)
        {
            AxisMoveWaitResult waitResult = await WaitBinFeederYMoveDoneInPosition(target, timeoutMs).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            if (waitResult.Success)
                return 0;

            return RaiseFeederAlarm(
                AxisMoveWaiter.ResolveAlarmCode(alarmPrefix, waitResult),
                description + " move/in-position wait failed. " +
                AxisMoveWaiter.FormatResult(waitResult, DescribeBinFeederYMoveDoneState()));
        }

        private void SetExclusiveOutput(BaseDigitalOutput onOutput, BaseDigitalOutput oppositeOutput, bool on, string code)
        {
            try
            {
                if (on)
                    oppositeOutput.Off();
                if (on) onOutput.On();
                else onOutput.Off();
            }
            catch (Exception ex)
            {
                RaiseFeederAlarm(code, "OutputFeeder output command failed: " + ex.Message);
            }
        }

        private double GetTeachingPosition(string positionName)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(positionName, "35_BinFeederY.AvoidPos", StringComparison.OrdinalIgnoreCase))
                return Recipe.AvoidPosition;
            if (string.Equals(positionName, "GoodStageExchange", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodWaferLoadPosition;
            if (string.Equals(positionName, "NgStageExchange", StringComparison.OrdinalIgnoreCase)) return Recipe.NGWaferLoadPosition;
            if (string.Equals(positionName, "CassetteInsert", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodCassetteExchangePosition;
            if (string.Equals(positionName, "CassetteLoadBase", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodCassetteLoadPosition;
            if (string.Equals(positionName, "GoodCassetteLoadPosition", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodCassetteLoadPosition;
            if (string.Equals(positionName, "GoodCassetteUnloadPosition", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodCassetteUnloadPosition;
            if (string.Equals(positionName, "GoodCassetteExchangePosition", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodCassetteExchangePosition;
            if (string.Equals(positionName, "GoodWaferLoadAvoidPosition", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodWaferLoadAvoidPosition;
            if (string.Equals(positionName, "GoodWaferLoadPosition", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodWaferLoadPosition;
            if (string.Equals(positionName, "GoodWaferUnloadAvoidPosition", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodWaferUnloadAvoidPosition;
            if (string.Equals(positionName, "GoodWaferUnloadPosition", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodWaferUnloadPosition;
            if (string.Equals(positionName, "GoodWaferBarcodePosition", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodWaferBarcodePosition;
            if (string.Equals(positionName, "NGCassetteLoadPosition", StringComparison.OrdinalIgnoreCase)) return Recipe.NGCassetteLoadPosition;
            if (string.Equals(positionName, "NGCassetteUnloadPosition", StringComparison.OrdinalIgnoreCase)) return Recipe.NGCassetteUnloadPosition;
            if (string.Equals(positionName, "NGCassetteExchangePosition", StringComparison.OrdinalIgnoreCase)) return Recipe.NGCassetteExchangePosition;
            if (string.Equals(positionName, "NGWaferLoadAvoidPosition", StringComparison.OrdinalIgnoreCase)) return Recipe.NGWaferLoadAvoidPosition;
            if (string.Equals(positionName, "NGWaferLoadPosition", StringComparison.OrdinalIgnoreCase)) return Recipe.NGWaferLoadPosition;
            if (string.Equals(positionName, "NGWaferUnloadAvoidPosition", StringComparison.OrdinalIgnoreCase)) return Recipe.NGWaferUnloadAvoidPosition;
            if (string.Equals(positionName, "NGWaferUnloadPosition", StringComparison.OrdinalIgnoreCase)) return Recipe.NGWaferUnloadPosition;
            if (string.Equals(positionName, "NGWaferBarcodePosition", StringComparison.OrdinalIgnoreCase)) return Recipe.NGWaferBarcodePosition;
            throw new ArgumentException("Unknown OutputFeederY teaching position: " + positionName, "positionName");
        }

        private void SetTeachingPosition(string positionName, double position)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(positionName, "35_BinFeederY.AvoidPos", StringComparison.OrdinalIgnoreCase))
                Recipe.AvoidPosition = position;
            else if (string.Equals(positionName, "GoodStageExchange", StringComparison.OrdinalIgnoreCase)) Recipe.GoodWaferLoadPosition = position;
            else if (string.Equals(positionName, "NgStageExchange", StringComparison.OrdinalIgnoreCase)) Recipe.NGWaferLoadPosition = position;
            else if (string.Equals(positionName, "CassetteInsert", StringComparison.OrdinalIgnoreCase)) Recipe.GoodCassetteExchangePosition = position;
            else if (string.Equals(positionName, "CassetteLoadBase", StringComparison.OrdinalIgnoreCase)) Recipe.GoodCassetteLoadPosition = position;
            else if (string.Equals(positionName, "GoodCassetteLoadPosition", StringComparison.OrdinalIgnoreCase)) Recipe.GoodCassetteLoadPosition = position;
            else if (string.Equals(positionName, "GoodCassetteUnloadPosition", StringComparison.OrdinalIgnoreCase)) Recipe.GoodCassetteUnloadPosition = position;
            else if (string.Equals(positionName, "GoodCassetteExchangePosition", StringComparison.OrdinalIgnoreCase)) Recipe.GoodCassetteExchangePosition = position;
            else if (string.Equals(positionName, "GoodWaferLoadAvoidPosition", StringComparison.OrdinalIgnoreCase)) Recipe.GoodWaferLoadAvoidPosition = position;
            else if (string.Equals(positionName, "GoodWaferLoadPosition", StringComparison.OrdinalIgnoreCase)) Recipe.GoodWaferLoadPosition = position;
            else if (string.Equals(positionName, "GoodWaferUnloadAvoidPosition", StringComparison.OrdinalIgnoreCase)) Recipe.GoodWaferUnloadAvoidPosition = position;
            else if (string.Equals(positionName, "GoodWaferUnloadPosition", StringComparison.OrdinalIgnoreCase)) Recipe.GoodWaferUnloadPosition = position;
            else if (string.Equals(positionName, "GoodWaferBarcodePosition", StringComparison.OrdinalIgnoreCase)) Recipe.GoodWaferBarcodePosition = position;
            else if (string.Equals(positionName, "NGCassetteLoadPosition", StringComparison.OrdinalIgnoreCase)) Recipe.NGCassetteLoadPosition = position;
            else if (string.Equals(positionName, "NGCassetteUnloadPosition", StringComparison.OrdinalIgnoreCase)) Recipe.NGCassetteUnloadPosition = position;
            else if (string.Equals(positionName, "NGCassetteExchangePosition", StringComparison.OrdinalIgnoreCase)) Recipe.NGCassetteExchangePosition = position;
            else if (string.Equals(positionName, "NGWaferLoadAvoidPosition", StringComparison.OrdinalIgnoreCase)) Recipe.NGWaferLoadAvoidPosition = position;
            else if (string.Equals(positionName, "NGWaferLoadPosition", StringComparison.OrdinalIgnoreCase)) Recipe.NGWaferLoadPosition = position;
            else if (string.Equals(positionName, "NGWaferUnloadAvoidPosition", StringComparison.OrdinalIgnoreCase)) Recipe.NGWaferUnloadAvoidPosition = position;
            else if (string.Equals(positionName, "NGWaferUnloadPosition", StringComparison.OrdinalIgnoreCase)) Recipe.NGWaferUnloadPosition = position;
            else if (string.Equals(positionName, "NGWaferBarcodePosition", StringComparison.OrdinalIgnoreCase)) Recipe.NGWaferBarcodePosition = position;
            else throw new ArgumentException("Unknown OutputFeederY teaching position: " + positionName, "positionName");
        }

        private double GetSidePosition(BinSide side, FeederPositionType type)
        {
            if (type == FeederPositionType.Avoid)
                return Recipe.AvoidPosition;

            bool ng = side == BinSide.Ng;
            switch (type)
            {
                // OK/NG 카세트 로드 위치 반환
                case FeederPositionType.CassetteLoad: return ng ? Recipe.NGCassetteLoadPosition : Recipe.GoodCassetteLoadPosition;
                // OK/NG 카세트 언로드 위치 반환
                case FeederPositionType.CassetteUnload: return ng ? Recipe.NGCassetteUnloadPosition : Recipe.GoodCassetteUnloadPosition;
                // OK/NG 바코드 위치 반환
                case FeederPositionType.Barcode: return ng ? Recipe.NGWaferBarcodePosition : Recipe.GoodWaferBarcodePosition;
                // OK/NG 스테이지 로드 위치 반환
                case FeederPositionType.StageLoad: return ng ? Recipe.NGWaferLoadPosition : Recipe.GoodWaferLoadPosition;
                // OK/NG 스테이지 언로드 위치 반환
                case FeederPositionType.StageUnload: return ng ? Recipe.NGWaferUnloadPosition : Recipe.GoodWaferUnloadPosition;
                // OK/NG 카세트 교체 위치 반환
                case FeederPositionType.Exchange: return ng ? Recipe.NGCassetteExchangePosition : Recipe.GoodCassetteExchangePosition;
                default: throw new ArgumentOutOfRangeException("type");
            }
        }

        private double GetStageLoadAvoidPosition(BinSide side)
        {
            return side == BinSide.Ng ? Recipe.NGWaferLoadAvoidPosition : Recipe.GoodWaferLoadAvoidPosition;
        }

        private double GetStageUnloadAvoidPosition(BinSide side)
        {
            return side == BinSide.Ng ? Recipe.NGWaferUnloadAvoidPosition : Recipe.GoodWaferUnloadAvoidPosition;
        }

        private void SetSidePosition(BinSide side, FeederPositionType type, double position)
        {
            bool ng = side == BinSide.Ng;
            switch (type)
            {
                // OK/NG 카세트 로드 위치 저장
                case FeederPositionType.CassetteLoad:
                    if (ng) Recipe.NGCassetteLoadPosition = position; else Recipe.GoodCassetteLoadPosition = position;
                    break;
                // OK/NG 카세트 언로드 위치 저장
                case FeederPositionType.CassetteUnload:
                    if (ng) Recipe.NGCassetteUnloadPosition = position; else Recipe.GoodCassetteUnloadPosition = position;
                    break;
                // OK/NG 바코드 위치 저장
                case FeederPositionType.Barcode:
                    if (ng) Recipe.NGWaferBarcodePosition = position; else Recipe.GoodWaferBarcodePosition = position;
                    break;
                // OK/NG 스테이지 로드 위치 저장
                case FeederPositionType.StageLoad:
                    if (ng) Recipe.NGWaferLoadPosition = position; else Recipe.GoodWaferLoadPosition = position;
                    break;
                // OK/NG 스테이지 언로드 위치 저장
                case FeederPositionType.StageUnload:
                    if (ng) Recipe.NGWaferUnloadPosition = position; else Recipe.GoodWaferUnloadPosition = position;
                    break;
                // OK/NG 카세트 교체 위치 저장
                case FeederPositionType.Exchange:
                    if (ng) Recipe.NGCassetteExchangePosition = position; else Recipe.GoodCassetteExchangePosition = position;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        private static BinSide ToBinSide(TargetCassette cassette)
        {
            return cassette == TargetCassette.Ng ? BinSide.Ng : BinSide.Good;
        }

        private static void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException("slotIndex");
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs)
        {
            return await WaitUntilAsync(condition, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs, CancellationToken ct)
        {
            int elapsed = 0;
            while (timeoutMs <= 0 || elapsed < timeoutMs)
            {
                if (condition())
                    return true;

                await Task.Delay(10, ct).ConfigureAwait(false);
                elapsed += 10;
            }
            return condition();
        }
    }
}
