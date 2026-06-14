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
    public enum VisionSide
    {
        Front,
        Rear
    }

    public enum VisionPositionType
    {
        Avoid,
        FrontProcess,
        RearProcess,
        SafeRetreat,
        Calibration
    }

    [DataContract]
    public sealed class VisionSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }
    }

    [DataContract]
    public sealed class VisionConfig : IConfigData
    {
        [DataMember] public bool bDryRun { get; set; }

        public bool IsSimulationMode
        {
            get { return bDryRun; }
            set { bDryRun = value; }
        }
    }

    [DataContract]
    public sealed class VisionAxisPositions
    {
        [DataMember] public double AvoidPosition { get; set; }
        [DataMember] public double FrontProcessPosition { get; set; }
        [DataMember] public double RearProcessPosition { get; set; }
        [DataMember] public double SafeRetreatPosition { get; set; }
        [DataMember] public double CalibrationPosition { get; set; }
    }

    [DataContract]
    public sealed class VisionRecipe : IRecipeData
    {
        [DataMember] public VisionAxisPositions FrontSideVision { get; set; } = new VisionAxisPositions();
        [DataMember] public VisionAxisPositions RearSideVision { get; set; } = new VisionAxisPositions();
        [DataMember] public int MoveTimeoutMs { get; set; } = 5000;
        [DataMember] public int IoTimeoutMs { get; set; } = 1000;
        [DataMember] public int CaptureTimeoutMs { get; set; } = 5000;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsurePositionObjects();
        }

        public void EnsurePositionObjects()
        {
            if (FrontSideVision == null) FrontSideVision = new VisionAxisPositions();
            if (RearSideVision == null) RearSideVision = new VisionAxisPositions();
        }
    }

    public sealed class VisionIoState
    {
        public bool WaferStageTouchSensor { get; set; }
        public bool ReticleUp { get; set; }
        public bool ReticleDown { get; set; }
        public bool ReticleFrontSideForward { get; set; }
        public bool ReticleFrontSideBackward { get; set; }
        public bool ReticleRearSideForward { get; set; }
        public bool ReticleRearSideBackward { get; set; }
        public bool NeedleVacuum { get; set; }
    }

    public sealed class VisionAxisState
    {
        public double FrontSideVisionYPosition { get; set; }
        public double RearSideVisionYPosition { get; set; }
        public bool FrontSideVisionYMoving { get; set; }
        public bool RearSideVisionYMoving { get; set; }
        public bool FrontSideVisionYAlarm { get; set; }
        public bool RearSideVisionYAlarm { get; set; }
    }

    public class VisionUnit : BaseUnit<VisionSetup, VisionConfig, VisionRecipe>, IUnitJogController
    {
        private readonly Dictionary<VisionAxis, BaseAxis> axes = new Dictionary<VisionAxis, BaseAxis>();

        public BaseAxis FrontSideVisionY { get; private set; }
        public BaseAxis RearSideVisionY { get; private set; }

        public BaseDigitalInput WaferStageTouchSensor { get; private set; }
        public BaseDigitalInput ReticleUpSensor { get; private set; }
        public BaseDigitalInput ReticleDownSensor { get; private set; }
        public BaseDigitalInput ReticleFrontSideFwSensor { get; private set; }
        public BaseDigitalInput ReticleFrontSideBwSensor { get; private set; }
        public BaseDigitalInput ReticleRearSideFwSensor { get; private set; }
        public BaseDigitalInput ReticleRearSideBwSensor { get; private set; }
        public BaseDigitalInput NeedleVacuumSensor { get; private set; }

        public BaseCylinder ReticleLift { get; private set; }
        public BaseCylinder ReticleFrontSideSlide { get; private set; }
        public BaseCylinder ReticleRearSideSlide { get; private set; }
        public BaseDigitalOutput NeedleVacuumOutput { get; private set; }

        public VisionUnit() : base("VisionUnit")
        {
            FrontSideVisionY = RegisterAxis(VisionAxis.FrontSideVisionY, "FrontSideVisionY0");
            RearSideVisionY = RegisterAxis(VisionAxis.RearSideVisionY, "RearSideVisionY0");

            WaferStageTouchSensor = RegisterInput("WaferStageTouchSensor");
            ReticleUpSensor = RegisterInput("ReticleUp");
            ReticleDownSensor = RegisterInput("ReticleDown");
            ReticleFrontSideFwSensor = RegisterInput("ReticleFrontSideFw");
            ReticleFrontSideBwSensor = RegisterInput("ReticleFrontSideBw");
            ReticleRearSideFwSensor = RegisterInput("ReticleRearSideFw");
            ReticleRearSideBwSensor = RegisterInput("ReticleRearSideBw");
            NeedleVacuumSensor = RegisterInput("NeedleVacuum");

            ReticleLift = CylinderManager.Get(AjinIoCatalog.CylinderRefs.ReticleLift);
            ReticleFrontSideSlide = CylinderManager.Get(AjinIoCatalog.CylinderRefs.ReticleSideSlideFront);
            ReticleRearSideSlide = CylinderManager.Get(AjinIoCatalog.CylinderRefs.ReticleSideSlideRear);
            NeedleVacuumOutput = AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.NeedleVacuum);

            Components.Add(ReticleLift);
            Components.Add(ReticleFrontSideSlide);
            Components.Add(ReticleRearSideSlide);
            Components.Add(NeedleVacuumOutput);
        }

        public IReadOnlyDictionary<VisionAxis, BaseAxis> Axes { get { return axes; } }

        public bool CanHandleJogAxis(BaseAxis axis)
        {
            VisionAxis visionAxis;
            return TryResolveVisionAxis(axis, out visionAxis);
        }

        public async Task<int> JogStepAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed,
            double axisStepDistance)
        {
            VisionAxis visionAxis;
            if (!TryResolveVisionAxis(axis, out visionAxis))
                return -1;

            double signedDistance = (direction < 0 ? -1.0 : 1.0) * Math.Abs(axisStepDistance);
            double target = axis.ActualPosition + signedDistance;
            return await MoveVisionAxis(visionAxis, target, speedType == JogSpeedType.Fine);
        }

        public Task<int> JogContinuousAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed)
        {
            VisionAxis visionAxis;
            if (!TryResolveVisionAxis(axis, out visionAxis))
                return Task.FromResult(-1);

            double speed = UnitJogVelocityResolver.Resolve(axis, speedType, customSpeed);
            ManualMoveVisionAxisJog(visionAxis, direction < 0 ? Direction.Minus : Direction.Plus, speed);
            return Task.FromResult(0);
        }

        public Task<int> StopJogAsync(BaseAxis axis)
        {
            VisionAxis visionAxis;
            if (!TryResolveVisionAxis(axis, out visionAxis))
                return Task.FromResult(-1);

            ManualStopVisionAxis(visionAxis);
            return Task.FromResult(0);
        }

        private bool TryResolveVisionAxis(BaseAxis axis, out VisionAxis visionAxis)
        {
            foreach (KeyValuePair<VisionAxis, BaseAxis> pair in axes)
            {
                if (ReferenceEquals(axis, pair.Value))
                {
                    visionAxis = pair.Key;
                    return true;
                }
            }

            visionAxis = VisionAxis.FrontSideVisionY;
            return false;
        }

        public async Task<int> MoveVisionAxis(VisionAxis axis, double targetPos, bool bFine = false)
        {
            try
            {
                BaseAxis item = ResolveVisionAxis(axis);
                if (!CheckVisionAxisMoveReady(axis))
                    return RaiseVisionAlarm("VS-MOVE-READY", axis + " is not ready to move.");
                if (!ValidateVisionTargetPosition(item, targetPos))
                    return RaiseVisionAlarm("VS-SOFT-LIMIT", axis + " target is out of soft limit. target=" + targetPos);

                EventLogger.Write(EventKind.Event, "QMC", "VS-MOVE", axis + " target=" + targetPos);
                int result = await item.MoveAbsoluteAsync(targetPos, ResolveMoveVelocity(item, bFine));
                if (result != 0 || item.IsAlarm)
                    return RaiseVisionAlarm("VS-MOVE", axis + " move failed. result=" + result + ", alarm=" + item.IsAlarm);

                AxisMoveWaitResult waitResult = await WaitVisionAxisMoveDoneInPosition(
                    axis,
                    targetPos,
                    Recipe != null && Recipe.MoveTimeoutMs > 0 ? Recipe.MoveTimeoutMs : 5000).ConfigureAwait(false);
                if (!waitResult.Success)
                    return RaiseVisionAlarm(
                        AxisMoveWaiter.ResolveAlarmCode("VS-MOVE", waitResult),
                        axis + " move/in-position wait failed. target=" + targetPos + ". " +
                        AxisMoveWaiter.FormatResult(waitResult, axis.ToString()));

                return 0;
            }
            catch (Exception ex)
            {
                return RaiseVisionAlarm("VS-MOVE-EX", axis + " move exception: " + ex.Message);
            }
        }

        public async Task<int> MoveVisionAxes(Dictionary<VisionAxis, double> targets, bool bFine = false)
        {
            if (targets == null)
                return RaiseVisionAlarm("VS-MOVE-TARGET", "Vision move target collection is null.");

            List<Task<int>> tasks = new List<Task<int>>();
            foreach (KeyValuePair<VisionAxis, double> pair in targets)
                tasks.Add(MoveVisionAxis(pair.Key, pair.Value, bFine));

            int[] results = await Task.WhenAll(tasks);
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i] != 0)
                    return results[i];
            }
            return 0;
        }

        public Task<int> MoveVisionAxisToTeachingPosition(VisionAxis axis, string positionName, bool bFine = false)
        {
            return MoveVisionAxis(axis, GetVisionTeachingPosition(axis, positionName), bFine);
        }

        public Task<int> MoveToVisionAvoidPosition(bool bFine = false)
        {
            var targets = new Dictionary<VisionAxis, double>();
            targets[VisionAxis.FrontSideVisionY] = Recipe.FrontSideVision.AvoidPosition;
            targets[VisionAxis.RearSideVisionY] = Recipe.RearSideVision.AvoidPosition;
            return MoveVisionAxes(targets, bFine);
        }

        public Task<int> MoveToVisionProcessPosition(VisionSide side, bool bFine = false)
        {
            return MoveVisionAxis(ResolveVisionAxisType(side), GetVisionTeachingPosition(ResolveVisionAxisType(side), ResolveVisionTeachingPositionName(side, VisionPositionType.FrontProcess)), bFine);
        }

        public Task<int> MoveFrontSideVisionToAvoidPosition(bool bFine = false) { return MoveToFrontSideVisionAvoidPosition(bFine); }
        public Task<int> MoveRearSideVisionToAvoidPosition(bool bFine = false) { return MoveToRearSideVisionAvoidPosition(bFine); }
        public Task<int> MoveFrontSideVisionToProcessPosition(bool bFine = false) { return MoveToFrontSideVisionProcessPosition(bFine); }
        public Task<int> MoveRearSideVisionToProcessPosition(bool bFine = false) { return MoveToRearSideVisionProcessPosition(bFine); }

        public Task<int> MoveToFrontSideVisionAvoidPosition(bool bFine = false)
        {
            return MoveVisionAxis(VisionAxis.FrontSideVisionY, Recipe.FrontSideVision.AvoidPosition, bFine);
        }

        public Task<int> MoveToRearSideVisionAvoidPosition(bool bFine = false)
        {
            return MoveVisionAxis(VisionAxis.RearSideVisionY, Recipe.RearSideVision.AvoidPosition, bFine);
        }

        public Task<int> MoveToFrontSideVisionProcessPosition(bool bFine = false)
        {
            return MoveVisionAxis(VisionAxis.FrontSideVisionY, Recipe.FrontSideVision.FrontProcessPosition, bFine);
        }

        public Task<int> MoveToRearSideVisionProcessPosition(bool bFine = false)
        {
            return MoveVisionAxis(VisionAxis.RearSideVisionY, Recipe.RearSideVision.RearProcessPosition, bFine);
        }

        public Task<int> MoveFrontSideVisionToSafeRetreatPosition(bool bFine = false)
        {
            return MoveVisionAxis(VisionAxis.FrontSideVisionY, Recipe.FrontSideVision.SafeRetreatPosition, bFine);
        }

        public Task<int> MoveRearSideVisionToSafeRetreatPosition(bool bFine = false)
        {
            return MoveVisionAxis(VisionAxis.RearSideVisionY, Recipe.RearSideVision.SafeRetreatPosition, bFine);
        }

        public Task<int> MoveToFrontSideVisionCalibrationPosition(bool bFine = false)
        {
            return MoveVisionAxis(VisionAxis.FrontSideVisionY, Recipe.FrontSideVision.CalibrationPosition, bFine);
        }

        public Task<int> MoveToRearSideVisionCalibrationPosition(bool bFine = false)
        {
            return MoveVisionAxis(VisionAxis.RearSideVisionY, Recipe.RearSideVision.CalibrationPosition, bFine);
        }

        public bool IsVisionAxisInPosition(VisionAxis axis, double targetPos, double tolerance)
        {
            BaseAxis item = ResolveVisionAxis(axis);
            return Math.Abs(item.ActualPosition - targetPos) <= tolerance && !item.IsAlarm;
        }

        public async Task<bool> WaitVisionAxisMoveDone(VisionAxis axis, int timeoutMs)
        {
            AxisMoveWaitResult waitResult = await WaitVisionAxisMoveDoneInPosition(axis, timeoutMs).ConfigureAwait(false);
            return waitResult.Success;
        }

        public async Task<AxisMoveWaitResult> WaitVisionAxisMoveDoneInPosition(VisionAxis axis, int timeoutMs)
        {
            BaseAxis item = ResolveVisionAxis(axis);
            return await WaitVisionAxisMoveDoneInPosition(axis, item.CommandPosition, timeoutMs).ConfigureAwait(false);
        }

        public async Task<AxisMoveWaitResult> WaitVisionAxisMoveDoneInPosition(VisionAxis axis, double targetPos, int timeoutMs)
        {
            BaseAxis item = ResolveVisionAxis(axis);
            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;
            return await AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                item,
                targetPos,
                tolerance,
                timeoutMs,
                0).ConfigureAwait(false);
        }

        public async Task<bool> WaitVisionAxesMoveDone(IEnumerable<VisionAxis> targetAxes, int timeoutMs)
        {
            AxisMoveWaitResult waitResult = await WaitVisionAxesMoveDoneInPosition(targetAxes, timeoutMs).ConfigureAwait(false);
            return waitResult.Success;
        }

        public async Task<AxisMoveWaitResult> WaitVisionAxesMoveDoneInPosition(IEnumerable<VisionAxis> targetAxes, int timeoutMs)
        {
            if (targetAxes == null)
                return new AxisMoveWaitResult(AxisMoveWaitFailure.AxisMissing, "Vision target axis collection is null.", "axes=null");

            foreach (VisionAxis axis in targetAxes)
            {
                AxisMoveWaitResult waitResult = await WaitVisionAxisMoveDoneInPosition(axis, timeoutMs).ConfigureAwait(false);
                if (!waitResult.Success)
                    return waitResult;
            }

            return new AxisMoveWaitResult(AxisMoveWaitFailure.None, "All vision axes reached target position.", "axes=ok");
        }

        public bool IsVisionAxisInTeachingPosition(VisionAxis axis, string positionName)
        {
            BaseAxis item = ResolveVisionAxis(axis);
            return IsVisionAxisInPosition(axis, GetVisionTeachingPosition(axis, positionName), item.Config.InPositionTolerance);
        }

        public Task<bool> WaitVisionAxisInTeachingPosition(VisionAxis axis, string positionName, int timeoutMs)
        {
            return WaitUntilAsync(() => IsVisionAxisInTeachingPosition(axis, positionName), timeoutMs);
        }

        public bool IsVisionInAvoidPosition()
        {
            return IsFrontSideVisionYInAvoidPosition() && IsRearSideVisionYInAvoidPosition();
        }

        public bool IsVisionInProcessPosition(VisionSide side)
        {
            return side == VisionSide.Front ? IsFrontSideVisionYInProcessPosition() : IsRearSideVisionYInProcessPosition();
        }

        public bool IsFrontSideVisionYInAvoidPosition()
        {
            return IsVisionAxisInPosition(VisionAxis.FrontSideVisionY, Recipe.FrontSideVision.AvoidPosition, FrontSideVisionY.Config.InPositionTolerance);
        }

        public bool IsRearSideVisionYInAvoidPosition()
        {
            return IsVisionAxisInPosition(VisionAxis.RearSideVisionY, Recipe.RearSideVision.AvoidPosition, RearSideVisionY.Config.InPositionTolerance);
        }

        public bool IsFrontSideVisionYInProcessPosition()
        {
            return IsVisionAxisInPosition(VisionAxis.FrontSideVisionY, Recipe.FrontSideVision.FrontProcessPosition, FrontSideVisionY.Config.InPositionTolerance);
        }

        public bool IsRearSideVisionYInProcessPosition()
        {
            return IsVisionAxisInPosition(VisionAxis.RearSideVisionY, Recipe.RearSideVision.RearProcessPosition, RearSideVisionY.Config.InPositionTolerance);
        }

        public void TeachVisionAxisPosition(VisionAxis axis, string positionName)
        {
            SetVisionTeachingPosition(axis, positionName, ResolveVisionAxis(axis).ActualPosition);
            EventLogger.Write(EventKind.Event, "QMC", "VS-TEACH", axis + "." + positionName + "=" + ResolveVisionAxis(axis).ActualPosition);
        }

        public void TeachFrontSideVisionAvoidPosition() { Recipe.FrontSideVision.AvoidPosition = FrontSideVisionY.ActualPosition; EventLogger.Write(EventKind.Event, "QMC", "VS-TEACH", "FrontSideVision.AvoidPosition=" + FrontSideVisionY.ActualPosition); }
        public void TeachFrontSideVisionProcessPosition() { Recipe.FrontSideVision.FrontProcessPosition = FrontSideVisionY.ActualPosition; EventLogger.Write(EventKind.Event, "QMC", "VS-TEACH", "FrontSideVision.FrontProcessPosition=" + FrontSideVisionY.ActualPosition); }
        public void TeachRearSideVisionAvoidPosition() { Recipe.RearSideVision.AvoidPosition = RearSideVisionY.ActualPosition; EventLogger.Write(EventKind.Event, "QMC", "VS-TEACH", "RearSideVision.AvoidPosition=" + RearSideVisionY.ActualPosition); }
        public void TeachRearSideVisionProcessPosition() { Recipe.RearSideVision.RearProcessPosition = RearSideVisionY.ActualPosition; EventLogger.Write(EventKind.Event, "QMC", "VS-TEACH", "RearSideVision.RearProcessPosition=" + RearSideVisionY.ActualPosition); }

        public void TeachVisionAvoidPositions()
        {
            TeachFrontSideVisionAvoidPosition();
            TeachRearSideVisionAvoidPosition();
        }

        public void TeachVisionProcessPositions()
        {
            TeachFrontSideVisionProcessPosition();
            TeachRearSideVisionProcessPosition();
        }

        public void TeachVisionSafeRetreatPositions()
        {
            Recipe.FrontSideVision.SafeRetreatPosition = FrontSideVisionY.ActualPosition;
            Recipe.RearSideVision.SafeRetreatPosition = RearSideVisionY.ActualPosition;
        }

        public void TeachVisionCalibrationPositions()
        {
            Recipe.FrontSideVision.CalibrationPosition = FrontSideVisionY.ActualPosition;
            Recipe.RearSideVision.CalibrationPosition = RearSideVisionY.ActualPosition;
        }

        public double GetVisionTeachingPosition(VisionAxis axis, string positionName)
        {
            VisionAxisPositions positions = axis == VisionAxis.FrontSideVisionY ? Recipe.FrontSideVision : Recipe.RearSideVision;
            if (IsName(positionName, "AvoidPosition") || IsName(positionName, "AvoidPos")) return positions.AvoidPosition;
            if (IsName(positionName, "FrontProcessPosition") || IsName(positionName, "ProcessPos")) return positions.FrontProcessPosition;
            if (IsName(positionName, "RearProcessPosition")) return positions.RearProcessPosition;
            if (IsName(positionName, "SafeRetreatPosition")) return positions.SafeRetreatPosition;
            if (IsName(positionName, "CalibrationPosition") || IsName(positionName, "CalibrationPos")) return positions.CalibrationPosition;
            return 0.0;
        }

        public bool ValidateVisionTeachingComplete()
        {
            return Recipe.FrontSideVision.AvoidPosition != 0.0 &&
                   Recipe.RearSideVision.AvoidPosition != 0.0 &&
                   Recipe.FrontSideVision.FrontProcessPosition != 0.0 &&
                   Recipe.RearSideVision.RearProcessPosition != 0.0;
        }

        public string ResolveVisionTeachingPositionName(VisionSide side, VisionPositionType type)
        {
            if (type == VisionPositionType.Avoid) return "AvoidPosition";
            if (type == VisionPositionType.SafeRetreat) return "SafeRetreatPosition";
            if (type == VisionPositionType.Calibration) return "CalibrationPosition";
            return side == VisionSide.Front ? "FrontProcessPosition" : "RearProcessPosition";
        }

        public async Task<int> MoveToTeachingPositionAndVerify(VisionAxis axis, string positionName, bool bFine = false)
        {
            int result = await MoveVisionAxisToTeachingPosition(axis, positionName, bFine);
            if (result != 0)
                return result;

            bool ok = await WaitVisionAxisInTeachingPosition(axis, positionName, Recipe.MoveTimeoutMs);
            return ok ? 0 : -1;
        }

        public bool NoVisionIoOutputDefined()
        {
            return false;
        }

        public Task<bool> TriggerFrontSideVisionCapture()
        {
            return TriggerVisionCapture(VisionSide.Front, Recipe.CaptureTimeoutMs);
        }

        public Task<bool> TriggerRearSideVisionCapture()
        {
            return TriggerVisionCapture(VisionSide.Rear, Recipe.CaptureTimeoutMs);
        }

        public async Task<bool> TriggerVisionCapture(VisionSide side, int timeoutMs)
        {
            if (!CheckVisionInspectionReady(side))
            {
                RaiseVisionAlarm("VS-CAPTURE-READY", side + " vision is not ready to capture.");
                return false;
            }
            EventLogger.Write(EventKind.Event, "QMC", "VS-CAPTURE", side + " vision capture requested.");
            await Task.Delay(timeoutMs > 0 ? Math.Min(timeoutMs, 10) : 1);
            return true;
        }

        public bool IsVisionWaferStageTouchSensor(bool expected = true) { return WaferStageTouchSensor.IsOn == expected; }
        public bool IsVisionReticleUp() { return ReticleUpSensor.IsOn; }
        public bool IsVisionReticleDown() { return ReticleDownSensor.IsOn; }
        public bool IsVisionReticleFrontSideForward() { return ReticleFrontSideFwSensor.IsOn; }
        public bool IsVisionReticleFrontSideBackward() { return ReticleFrontSideBwSensor.IsOn; }
        public bool IsVisionReticleRearSideForward() { return ReticleRearSideFwSensor.IsOn; }
        public bool IsVisionReticleRearSideBackward() { return ReticleRearSideBwSensor.IsOn; }
        public bool IsVisionNeedleVacuumOk(bool expected = true) { return NeedleVacuumSensor.IsOn == expected || Config.IsSimulationMode || Setup.IsSimulationMode; }

        public Task<bool> WaitVisionReticleSafeState(VisionSide side, int timeoutMs)
        {
            return WaitUntilAsync(() => side == VisionSide.Front ? CheckVisionReticleFrontSideState() : CheckVisionReticleRearSideState(), timeoutMs);
        }

        public bool CheckVisionReticleFrontSideState()
        {
            return Config.IsSimulationMode || Setup.IsSimulationMode || IsVisionReticleFrontSideForward() != IsVisionReticleFrontSideBackward();
        }

        public bool CheckVisionReticleRearSideState()
        {
            return Config.IsSimulationMode || Setup.IsSimulationMode || IsVisionReticleRearSideForward() != IsVisionReticleRearSideBackward();
        }

        public bool CheckVisionNeedleSafeState()
        {
            return Config.IsSimulationMode || Setup.IsSimulationMode || !NeedleVacuumSensor.IsOn;
        }

        public bool CheckVisionInterlockInputs()
        {
            return CheckVisionNeedleSafeState() && !IsReticleStateConflict();
        }

        public void ManualMoveVisionAxisJog(VisionAxis axis, Direction dir, double speed)
        {
            ResolveVisionAxis(axis).MoveJogContinuous((int)dir, JogSpeedType.Custom, speed);
        }

        public void ManualStopVisionAxis(VisionAxis axis)
        {
            ResolveVisionAxis(axis).StopJog();
        }

        public Task<int> ManualMoveToVisionAvoidPosition(bool bFine = false) { return MoveToVisionAvoidPosition(bFine); }
        public Task<int> ManualMoveToFrontSideVisionAvoidPosition(bool bFine = false) { return MoveToFrontSideVisionAvoidPosition(bFine); }
        public Task<int> ManualMoveToRearSideVisionAvoidPosition(bool bFine = false) { return MoveToRearSideVisionAvoidPosition(bFine); }
        public Task<int> ManualMoveToFrontSideVisionProcessPosition(bool bFine = false) { return MoveToFrontSideVisionProcessPosition(bFine); }
        public Task<int> ManualMoveToRearSideVisionProcessPosition(bool bFine = false) { return MoveToRearSideVisionProcessPosition(bFine); }
        public Task<bool> ManualTriggerFrontVisionCapture() { return TriggerFrontSideVisionCapture(); }
        public Task<bool> ManualTriggerRearVisionCapture() { return TriggerRearSideVisionCapture(); }

        public async Task<bool> PrepareFrontSideVisionInspection(int timeoutMs, bool bFine = false)
        {
            if (!CheckVisionInspectionReady(VisionSide.Front))
            {
                RaiseVisionAlarm("VS-FRONT-READY", "Front vision inspection is not ready.");
                return false;
            }
            if (await MoveToFrontSideVisionProcessPosition(bFine) != 0)
                return false;
            bool ok = await WaitVisionReticleSafeState(VisionSide.Front, timeoutMs);
            if (!ok) RaiseVisionAlarm("VS-FRONT-RETICLE", "Front vision reticle safe state timeout.");
            return ok;
        }

        public async Task<bool> PrepareRearSideVisionInspection(int timeoutMs, bool bFine = false)
        {
            if (!CheckVisionInspectionReady(VisionSide.Rear))
            {
                RaiseVisionAlarm("VS-REAR-READY", "Rear vision inspection is not ready.");
                return false;
            }
            if (await MoveToRearSideVisionProcessPosition(bFine) != 0)
                return false;
            bool ok = await WaitVisionReticleSafeState(VisionSide.Rear, timeoutMs);
            if (!ok) RaiseVisionAlarm("VS-REAR-RETICLE", "Rear vision reticle safe state timeout.");
            return ok;
        }

        public async Task<bool> InspectFrontSideVision(int timeoutMs, bool bFine = false)
        {
            return await PrepareFrontSideVisionInspection(timeoutMs, bFine) && await TriggerVisionCapture(VisionSide.Front, timeoutMs);
        }

        public async Task<bool> InspectRearSideVision(int timeoutMs, bool bFine = false)
        {
            return await PrepareRearSideVisionInspection(timeoutMs, bFine) && await TriggerVisionCapture(VisionSide.Rear, timeoutMs);
        }

        public async Task<bool> InspectBothSideVision(int timeoutMs, bool bFine = false)
        {
            return await InspectFrontSideVision(timeoutMs, bFine) && await InspectRearSideVision(timeoutMs, bFine);
        }

        public async Task<bool> RecoverVisionToSafeState(int timeoutMs, bool moveAvoid = true)
        {
            StopVisionMotion("Recover");
            if (!moveAvoid)
                return true;
            return await MoveToVisionAvoidPosition() == 0 && await WaitVisionAxesMoveDone(new[] { VisionAxis.FrontSideVisionY, VisionAxis.RearSideVisionY }, timeoutMs);
        }

        public bool CheckVisionAxisMoveReady(VisionAxis axis)
        {
            BaseAxis item = ResolveVisionAxis(axis);
            return !item.IsAlarm && CheckVisionMoveReady();
        }

        public bool CheckVisionMoveReady()
        {
            return CheckVisionInterlockInputs();
        }

        public bool CheckVisionInspectionReady(VisionSide side)
        {
            return ValidateVisionTeachingComplete() &&
                   CheckVisionNeedleSafeState() &&
                   (side == VisionSide.Front ? CheckVisionReticleFrontSideState() : CheckVisionReticleRearSideState());
        }

        public bool CheckFrontVisionReady() { return CheckVisionInspectionReady(VisionSide.Front); }
        public bool CheckRearVisionReady() { return CheckVisionInspectionReady(VisionSide.Rear); }
        public bool CheckVisionReady() { return CheckVisionMoveReady(); }

        public VisionIoState GetVisionIoState()
        {
            return new VisionIoState
            {
                WaferStageTouchSensor = WaferStageTouchSensor.IsOn,
                ReticleUp = ReticleUpSensor.IsOn,
                ReticleDown = ReticleDownSensor.IsOn,
                ReticleFrontSideForward = ReticleFrontSideFwSensor.IsOn,
                ReticleFrontSideBackward = ReticleFrontSideBwSensor.IsOn,
                ReticleRearSideForward = ReticleRearSideFwSensor.IsOn,
                ReticleRearSideBackward = ReticleRearSideBwSensor.IsOn,
                NeedleVacuum = NeedleVacuumSensor.IsOn
            };
        }

        public VisionAxisState GetVisionAxisState()
        {
            return new VisionAxisState
            {
                FrontSideVisionYPosition = FrontSideVisionY.ActualPosition,
                RearSideVisionYPosition = RearSideVisionY.ActualPosition,
                FrontSideVisionYMoving = FrontSideVisionY.IsMoving,
                RearSideVisionYMoving = RearSideVisionY.IsMoving,
                FrontSideVisionYAlarm = FrontSideVisionY.IsAlarm,
                RearSideVisionYAlarm = RearSideVisionY.IsAlarm
            };
        }

        public BaseAxis ResolveVisionAxis(VisionSide side)
        {
            return ResolveVisionAxis(ResolveVisionAxisType(side));
        }

        public BaseAxis ResolveVisionAxis(VisionAxis axis)
        {
            BaseAxis item;
            if (!axes.TryGetValue(axis, out item))
                throw new ArgumentException("Unknown vision axis: " + axis);
            return item;
        }

        public void StopVisionMotion(string reason)
        {
            FrontSideVisionY.StopJog();
            RearSideVisionY.StopJog();
            EventLogger.Write(EventKind.Event, "QMC", "VS-STOP", "Vision stopped. reason=" + reason);
        }

        public string BuildVisionAlarmMessage(StageAlarmCode code)
        {
            return "Vision alarm: " + code;
        }

        private BaseAxis RegisterAxis(VisionAxis axis, string axisName)
        {
            BaseAxis item = AjinFactory.CreateAxis(axisName);
            axes[axis] = item;
            Components.Add(item);
            return item;
        }

        private BaseDigitalInput RegisterInput(string catalogName)
        {
            BaseDigitalInput item = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput(catalogName));
            Components.Add(item);
            return item;
        }

        private double ResolveMoveVelocity(BaseAxis axis, bool bFine)
        {
            if (bFine && axis.Config.JogFineVelocity > 0)
                return axis.Config.JogFineVelocity;
            return axis.Config.DefaultVelocity;
        }

        private bool ValidateVisionTargetPosition(BaseAxis axis, double targetPos)
        {
            if (!axis.Setup.SoftLimitEnabled)
                return true;
            return targetPos >= axis.Setup.SoftLimitMinus && targetPos <= axis.Setup.SoftLimitPlus;
        }

        private VisionAxis ResolveVisionAxisType(VisionSide side)
        {
            return side == VisionSide.Front ? VisionAxis.FrontSideVisionY : VisionAxis.RearSideVisionY;
        }

        private void SetVisionTeachingPosition(VisionAxis axis, string positionName, double position)
        {
            VisionAxisPositions positions = axis == VisionAxis.FrontSideVisionY ? Recipe.FrontSideVision : Recipe.RearSideVision;
            if (IsName(positionName, "AvoidPosition") || IsName(positionName, "AvoidPos")) positions.AvoidPosition = position;
            else if (IsName(positionName, "FrontProcessPosition") || IsName(positionName, "ProcessPos")) positions.FrontProcessPosition = position;
            else if (IsName(positionName, "RearProcessPosition")) positions.RearProcessPosition = position;
            else if (IsName(positionName, "SafeRetreatPosition")) positions.SafeRetreatPosition = position;
            else if (IsName(positionName, "CalibrationPosition") || IsName(positionName, "CalibrationPos")) positions.CalibrationPosition = position;
        }

        private int RaiseVisionAlarm(string code, string message)
        {
            EventLogger.Write(EventKind.Alarm, "QMC", code, message);
            AlarmManager.Raise(AlarmSeverity.Error, code, Name, message);
            return -1;
        }

        private bool IsReticleStateConflict()
        {
            if (Config.IsSimulationMode || Setup.IsSimulationMode)
                return false;

            return (ReticleUpSensor.IsOn && ReticleDownSensor.IsOn) ||
                   (ReticleFrontSideFwSensor.IsOn && ReticleFrontSideBwSensor.IsOn) ||
                   (ReticleRearSideFwSensor.IsOn && ReticleRearSideBwSensor.IsOn);
        }

        private static bool IsName(string source, string expected)
        {
            return string.Equals(source, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs)
        {
            DateTime start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
            {
                if (condition())
                    return true;
                await Task.Delay(10);
            }
            return condition();
        }
    }
}
