using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    public enum PickerPickUpSeparateMode
    {
        Simultaneous = 0,
        NeedleFirst = 1,
        PickerFirst = 2
    }

    public enum PickerPickUpZMotionMode
    {
        Detailed = 0,
        SimpleZDownVacuumUp = 1
    }

    public enum PickerPickUpTransferMotionMode
    {
        Default = 0,
        ContiSegmentedPickUp = 2
    }

    public enum PickerBottomFlyingZDownMode
    {
        Off = 0,
        DownDistance = 1,
        ToBottomPosition = 2
    }

    public enum PickerPlaceMotionMode
    {
        Default = 0,
        ContiSegmentedPlace = 2
    }

    [DataContract]
    public sealed class PickerPickUpMotionConfig
    {
        public const double MinimumPickerSafeForWaferStageDistance = 2.0;

        [DataMember] public PickerPickUpZMotionMode MotionMode { get; set; } = PickerPickUpZMotionMode.Detailed;
        [DataMember] public PickerPickUpTransferMotionMode TransferMotionMode { get; set; } = PickerPickUpTransferMotionMode.Default;
        [DataMember] public int TransferContiCoordinate { get; set; } = 2;
        [DataMember] public int TransferContiTimeoutMs { get; set; } = 5000;
        [DataMember] public double TransferContiMaxTravelDistance { get; set; } = 45.0;
        [DataMember] public double TransferContiPickerYMaxCorrectionDistance { get; set; } = 1.5;
        [DataMember] public double TransferContiXYMidRatio { get; set; } = 0.5;
        [DataMember] public double TransferContiSplineCurvePercent { get; set; } = 100.0;
        [DataMember] public double TransferContiMaxVelocity { get; set; } = 500.0;
        [DataMember] public double TransferContiMaxAcceleration { get; set; } = 5000.0;
        [DataMember] public double TransferContiMaxDeceleration { get; set; } = 5000.0;
        [DataMember] public bool TransferContiUseGlobalSpeedScale { get; set; } = true;
        [DataMember] public double TransferContiNode0SpeedPercent { get; set; } = 20.0;
        [DataMember] public double TransferContiNode1SpeedPercent { get; set; } = 100.0;
        [DataMember] public double TransferContiNode2SpeedPercent { get; set; } = 100.0;
        [DataMember] public double TransferContiNode3SpeedPercent { get; set; } = 20.0;
        [DataMember] public double PickerZPrePickDistance { get; set; } = 1.0;
        [DataMember] public double PickerZSlowApproachSpeedPercent { get; set; } = 1.0;
        [DataMember] public double PickerZSyncLiftDistance { get; set; } = 2.0;
        [DataMember] public double PickerZSyncLiftVelocity { get; set; } = 5.0;
        [DataMember] public double PickerZSyncLiftAcceleration { get; set; } = 100.0;
        [DataMember] public double PickerZSyncLiftDeceleration { get; set; } = 100.0;
        [DataMember] public double PickerZSeparateDistance { get; set; } = 1.0;
        [DataMember] public double PickerZSeparateSpeedPercent { get; set; } = 1.0;
        [DataMember] public double PickerZAvoidReturnSpeedPercent { get; set; } = 10.0;
        [DataMember] public double PickerSafeForWaferStageDistance { get; set; } = MinimumPickerSafeForWaferStageDistance;
        [DataMember] public PickerPickUpSeparateMode SeparateMode { get; set; } = PickerPickUpSeparateMode.Simultaneous;
        [DataMember] public int VacuumOnBeforePickDelayMs { get; set; } = 0;
        [DataMember] public int NeedleVacuumOffSettleBeforeXYMs { get; set; } = 100;
        [DataMember] public int SyncLiftSettleMs { get; set; } = 0;
        [DataMember] public int PickSettleMs { get; set; } = 0;

        // Legacy values are kept only for reading old config files.
        [DataMember] public double PickerZSlowApproachVelocity { get; set; } = 0.0;
        [DataMember] public double PickerZSlowApproachAcceleration { get; set; } = 0.0;
        [DataMember] public double PickerZSlowApproachDeceleration { get; set; } = 0.0;
        [DataMember] public double SyncLiftDistance { get; set; } = 0.0;
        [DataMember] public double SyncLiftSpeedPercent { get; set; } = 0.0;
        [DataMember] public double NeedleSeparateDistance { get; set; } = 0.0;
        [DataMember] public double PickerSeparateDistance { get; set; } = 0.0;
        [DataMember] public double SeparateSpeedPercent { get; set; } = 0.0;
        [DataMember] public double PickerZSeparateVelocity { get; set; } = 0.0;
        [DataMember] public double PickerZSeparateAcceleration { get; set; } = 0.0;
        [DataMember] public double PickerZSeparateDeceleration { get; set; } = 0.0;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            TransferContiSplineCurvePercent = 100.0;
            TransferContiUseGlobalSpeedScale = true;
            NeedleVacuumOffSettleBeforeXYMs = 100;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            Ensure();
        }

        public void Ensure()
        {
            if (PickerZSyncLiftDistance <= 0.0 && SyncLiftDistance > 0.0)
                PickerZSyncLiftDistance = SyncLiftDistance;
            if (PickerZSeparateDistance <= 0.0 && PickerSeparateDistance > 0.0)
                PickerZSeparateDistance = PickerSeparateDistance;
            if (PickerZSlowApproachSpeedPercent <= 0.0 && PickerZSlowApproachVelocity > 0.0)
                PickerZSlowApproachSpeedPercent = 1.0;
            if (PickerZSyncLiftVelocity <= 0.0 && SyncLiftSpeedPercent > 0.0)
                PickerZSyncLiftVelocity = 5.0;
            if (PickerZSeparateSpeedPercent <= 0.0 && SeparateSpeedPercent > 0.0)
                PickerZSeparateSpeedPercent = SeparateSpeedPercent;
            if (PickerZSeparateSpeedPercent <= 0.0 && PickerZSeparateVelocity > 0.0)
                PickerZSeparateSpeedPercent = 1.0;

            if (TransferMotionMode != PickerPickUpTransferMotionMode.Default &&
                TransferMotionMode != PickerPickUpTransferMotionMode.ContiSegmentedPickUp)
            {
                TransferMotionMode = PickerPickUpTransferMotionMode.Default;
            }

            if (TransferContiCoordinate <= 0)
                TransferContiCoordinate = 2;
            if (TransferContiTimeoutMs <= 0)
                TransferContiTimeoutMs = 5000;
            TransferContiMaxTravelDistance = NormalizePositive(TransferContiMaxTravelDistance, 45.0);
            TransferContiPickerYMaxCorrectionDistance = NormalizePositive(TransferContiPickerYMaxCorrectionDistance, 1.5);
            TransferContiXYMidRatio = NormalizeRatio(TransferContiXYMidRatio, 0.5);
            TransferContiSplineCurvePercent = NormalizeSplineCurvePercent(TransferContiSplineCurvePercent, 100.0);
            TransferContiMaxVelocity = NormalizePositive(TransferContiMaxVelocity, 500.0);
            TransferContiMaxAcceleration = NormalizePositive(TransferContiMaxAcceleration, 5000.0);
            TransferContiMaxDeceleration = NormalizePositive(TransferContiMaxDeceleration, 5000.0);
            TransferContiNode0SpeedPercent = NormalizePercent(TransferContiNode0SpeedPercent, 20.0);
            TransferContiNode1SpeedPercent = NormalizePercent(TransferContiNode1SpeedPercent, 100.0);
            TransferContiNode2SpeedPercent = NormalizePercent(TransferContiNode2SpeedPercent, 100.0);
            TransferContiNode3SpeedPercent = NormalizePercent(TransferContiNode3SpeedPercent, 20.0);

            PickerZPrePickDistance = NormalizeDistance(PickerZPrePickDistance);
            PickerZSlowApproachSpeedPercent = NormalizePercent(PickerZSlowApproachSpeedPercent, 1.0);
            PickerZSyncLiftDistance = NormalizeDistance(PickerZSyncLiftDistance);
            PickerZSyncLiftVelocity = NormalizePositive(PickerZSyncLiftVelocity, 5.0);
            PickerZSyncLiftAcceleration = NormalizePositive(PickerZSyncLiftAcceleration, 100.0);
            PickerZSyncLiftDeceleration = NormalizePositive(PickerZSyncLiftDeceleration, 100.0);
            PickerZSeparateDistance = NormalizeDistance(PickerZSeparateDistance);
            PickerZSeparateSpeedPercent = NormalizePercent(PickerZSeparateSpeedPercent, 1.0);
            PickerZAvoidReturnSpeedPercent = NormalizePercent(PickerZAvoidReturnSpeedPercent, 10.0);
            PickerSafeForWaferStageDistance = NormalizePickerSafeForWaferStageDistance(PickerSafeForWaferStageDistance);

            if (VacuumOnBeforePickDelayMs < 0)
                VacuumOnBeforePickDelayMs = 0;
            if (NeedleVacuumOffSettleBeforeXYMs < 0)
                NeedleVacuumOffSettleBeforeXYMs = 0;
            if (NeedleVacuumOffSettleBeforeXYMs > 60000)
                NeedleVacuumOffSettleBeforeXYMs = 60000;
            if (SyncLiftSettleMs < 0)
                SyncLiftSettleMs = 0;
            if (PickSettleMs < 0)
                PickSettleMs = 0;
        }

        public double GetTransferContiNodeVelocity(int nodeIndex)
        {
            double velocity = TransferContiMaxVelocity * GetTransferContiNodeRatio(nodeIndex);
            return TransferContiUseGlobalSpeedScale ? MotionSpeedScale.ApplyDefaultVelocityScale(velocity) : velocity;
        }

        public double GetTransferContiNodeAcceleration(int nodeIndex)
        {
            double acceleration = TransferContiMaxAcceleration * GetTransferContiNodeRatio(nodeIndex);
            return TransferContiUseGlobalSpeedScale ? MotionSpeedScale.ApplyDefaultAccelerationScale(acceleration) : acceleration;
        }

        public double GetTransferContiNodeDeceleration(int nodeIndex)
        {
            double deceleration = TransferContiMaxDeceleration * GetTransferContiNodeRatio(nodeIndex);
            return TransferContiUseGlobalSpeedScale ? MotionSpeedScale.ApplyDefaultAccelerationScale(deceleration) : deceleration;
        }

        public static double NormalizePercent(double percent, double fallback)
        {
            if (double.IsNaN(percent) || double.IsInfinity(percent) || percent <= 0.0)
                percent = fallback;
            if (percent < 1.0)
                return 1.0;
            if (percent > 100.0)
                return 100.0;
            return percent;
        }

        public static double NormalizePositive(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
                return fallback;
            return value;
        }

        public static double NormalizePickerSafeForWaferStageDistance(double distance)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance))
                return MinimumPickerSafeForWaferStageDistance;
            return Math.Max(MinimumPickerSafeForWaferStageDistance, distance);
        }

        public static double NormalizeSplineCurvePercent(double percent, double fallback)
        {
            if (double.IsNaN(percent) || double.IsInfinity(percent))
                percent = fallback;
            if (percent < 0.0)
                return 0.0;
            if (percent > 200.0)
                return 200.0;
            return percent;
        }

        private static double NormalizeDistance(double distance)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0.0)
                return 0.0;
            return distance;
        }

        private double GetTransferContiNodeRatio(int nodeIndex)
        {
            double percent;
            switch (nodeIndex)
            {
                case 0: percent = TransferContiNode0SpeedPercent; break;
                case 1: percent = TransferContiNode1SpeedPercent; break;
                case 2: percent = TransferContiNode2SpeedPercent; break;
                case 3: percent = TransferContiNode3SpeedPercent; break;
                default: percent = TransferContiNode3SpeedPercent; break;
            }

            return NormalizePercent(percent, 1.0) / 100.0;
        }

        private static double NormalizeRatio(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                value = fallback;
            if (value < 0.0)
                return 0.0;
            if (value > 1.0)
                return 1.0;
            return value;
        }
    }

    [DataContract]
    public sealed class PickerBottomInspectionMotionConfig
    {
        [DataMember] public PickerBottomFlyingZDownMode FlyingZDownMode { get; set; } = PickerBottomFlyingZDownMode.Off;
        [DataMember] public double FlyingZDownDistance { get; set; } = 2.0;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            Ensure();
        }

        public void Ensure()
        {
            FlyingZDownDistance = NormalizeDistance(FlyingZDownDistance);
        }

        public double ResolveFlyingZDownTarget(double avoid, double bottom)
        {
            Ensure();

            switch (FlyingZDownMode)
            {
                case PickerBottomFlyingZDownMode.ToBottomPosition:
                    return bottom;

                case PickerBottomFlyingZDownMode.DownDistance:
                    return ResolveFlyingZDownDistanceTarget(avoid, bottom);

                case PickerBottomFlyingZDownMode.Off:
                default:
                    return avoid;
            }
        }

        private double ResolveFlyingZDownDistanceTarget(double avoid, double bottom)
        {
            double distance = NormalizeDistance(FlyingZDownDistance);
            if (distance <= 0.0)
                return avoid;

            double delta = bottom - avoid;
            double total = Math.Abs(delta);
            if (total <= 0.0001)
                return bottom;

            if (distance >= total)
                return bottom;

            return avoid + Math.Sign(delta) * distance;
        }

        public static double NormalizeDistance(double distance)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0.0)
                return 0.0;
            return distance;
        }
    }

    [DataContract]
    public sealed class PickerPlaceMotionConfig
    {
        [DataMember] public PickerPlaceMotionMode MotionMode { get; set; } = PickerPlaceMotionMode.ContiSegmentedPlace;
        [DataMember] public int ContiCoordinate { get; set; } = 1;
        [DataMember] public int ContiTimeoutMs { get; set; } = 5000;
        [DataMember] public double ContiMaxTravelDistance { get; set; } = 45.0;
        [DataMember] public double ContiZ1Step1Clearance { get; set; } = 2.0;
        [DataMember] public double ContiZ1Step2Clearance { get; set; } = 2.0;
        [DataMember] public double ContiNearAvoidDistance { get; set; } = 1.0;
        [DataMember] public double ContiXYMidRatio { get; set; } = 0.5;
        [DataMember] public double ContiSplineCurvePercent { get; set; } = 100.0;
        [DataMember] public double ContiOverDrive { get; set; } = 0.03;
        [DataMember] public double PlaceZOverDrive { get; set; } = 0.0;
        [DataMember] public int PlaceReleaseDwellMs { get; set; } = 0;
        [DataMember] public int PlaceBlowDelayMs { get; set; } = 100;
        [DataMember] public double ContiTapeThicknessFallback { get; set; } = 0.0;
        [DataMember] public double ContiDieThicknessFallback { get; set; } = 0.0;
        [DataMember] public double ContiMaxVelocity { get; set; } = 500.0;
        [DataMember] public double ContiMaxAcceleration { get; set; } = 5000.0;
        [DataMember] public double ContiMaxDeceleration { get; set; } = 5000.0;
        [DataMember] public bool ContiUseGlobalSpeedScale { get; set; } = true;
        [DataMember] public double ContiNode0SpeedPercent { get; set; } = 1.0;
        [DataMember] public double ContiNode1SpeedPercent { get; set; } = 20.0;
        [DataMember] public double ContiNode2SpeedPercent { get; set; } = 100.0;
        [DataMember] public double ContiNode3SpeedPercent { get; set; } = 100.0;
        [DataMember] public double ContiNode4SpeedPercent { get; set; } = 1.0;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            ContiSplineCurvePercent = 100.0;
            ContiUseGlobalSpeedScale = true;
            PlaceBlowDelayMs = 100;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            Ensure();
        }

        public void Ensure()
        {
            if (MotionMode != PickerPlaceMotionMode.Default &&
                MotionMode != PickerPlaceMotionMode.ContiSegmentedPlace)
            {
                MotionMode = PickerPlaceMotionMode.ContiSegmentedPlace;
            }

            if (ContiCoordinate <= 0)
                ContiCoordinate = 1;
            if (ContiTimeoutMs <= 0)
                ContiTimeoutMs = 5000;
            ContiMaxTravelDistance = PickerPickUpMotionConfig.NormalizePositive(ContiMaxTravelDistance, 45.0);
            ContiZ1Step1Clearance = NormalizeNonNegative(ContiZ1Step1Clearance);
            ContiZ1Step2Clearance = NormalizeNonNegative(ContiZ1Step2Clearance);
            ContiNearAvoidDistance = NormalizeNonNegative(ContiNearAvoidDistance);
            ContiXYMidRatio = NormalizeRatio(ContiXYMidRatio, 0.5);
            ContiSplineCurvePercent = PickerPickUpMotionConfig.NormalizeSplineCurvePercent(ContiSplineCurvePercent, 100.0);
            ContiOverDrive = NormalizeNonNegative(ContiOverDrive);
            PlaceZOverDrive = NormalizeFinite(PlaceZOverDrive);
            if (PlaceReleaseDwellMs < 0)
                PlaceReleaseDwellMs = 0;
            if (PlaceBlowDelayMs < 0)
                PlaceBlowDelayMs = 0;
            ContiTapeThicknessFallback = NormalizeNonNegative(ContiTapeThicknessFallback);
            ContiDieThicknessFallback = NormalizeNonNegative(ContiDieThicknessFallback);
            ContiMaxVelocity = PickerPickUpMotionConfig.NormalizePositive(ContiMaxVelocity, 500.0);
            ContiMaxAcceleration = PickerPickUpMotionConfig.NormalizePositive(ContiMaxAcceleration, 5000.0);
            ContiMaxDeceleration = PickerPickUpMotionConfig.NormalizePositive(ContiMaxDeceleration, 5000.0);
            ContiNode0SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(ContiNode0SpeedPercent, 1.0);
            ContiNode1SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(ContiNode1SpeedPercent, 20.0);
            ContiNode2SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(ContiNode2SpeedPercent, 100.0);
            ContiNode3SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(ContiNode3SpeedPercent, 100.0);
            ContiNode4SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(ContiNode4SpeedPercent, 1.0);
        }

        public double GetContiNodeVelocity(int nodeIndex)
        {
            double velocity = ContiMaxVelocity * GetContiNodeRatio(nodeIndex);
            return ContiUseGlobalSpeedScale ? MotionSpeedScale.ApplyDefaultVelocityScale(velocity) : velocity;
        }

        public double GetContiNodeAcceleration(int nodeIndex)
        {
            double acceleration = ContiMaxAcceleration * GetContiNodeRatio(nodeIndex);
            return ContiUseGlobalSpeedScale ? MotionSpeedScale.ApplyDefaultAccelerationScale(acceleration) : acceleration;
        }

        public double GetContiNodeDeceleration(int nodeIndex)
        {
            double deceleration = ContiMaxDeceleration * GetContiNodeRatio(nodeIndex);
            return ContiUseGlobalSpeedScale ? MotionSpeedScale.ApplyDefaultAccelerationScale(deceleration) : deceleration;
        }

        private double GetContiNodeRatio(int nodeIndex)
        {
            double percent;
            switch (nodeIndex)
            {
                case 0: percent = ContiNode0SpeedPercent; break;
                case 1: percent = ContiNode1SpeedPercent; break;
                case 2: percent = ContiNode2SpeedPercent; break;
                case 3: percent = ContiNode3SpeedPercent; break;
                case 4: percent = ContiNode4SpeedPercent; break;
                default: percent = ContiNode4SpeedPercent; break;
            }

            return PickerPickUpMotionConfig.NormalizePercent(percent, 1.0) / 100.0;
        }

        private static double NormalizeNonNegative(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
                return 0.0;
            return value;
        }

        private static double NormalizeFinite(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0.0;
            return value;
        }

        private static double NormalizeRatio(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                value = fallback;
            if (value < 0.0)
                return 0.0;
            if (value > 1.0)
                return 1.0;
            return value;
        }
    }

    public class BottomVisionOffset
    {
        public int PickerNo { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public double OffsetT { get; set; }
        // Bottom 완료 결과의 중심 Offset(mm). Side 카메라 각도별 보정 전용이며 Place OffsetX/Y와 분리한다.
        public double BottomCenterOffsetX { get; set; }
        public double BottomCenterOffsetY { get; set; }
        public bool HasBottomCenterOffset { get; set; }
        public double SideVisionYOffset { get; set; }
        public double PickerZOffset { get; set; }
        public bool HasSideInspectionCorrection { get; set; }
        public bool IsOk { get; set; }
        public string Raw { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public class SideVisionResult
    {
        public int PickerNo { get; set; }
        public bool Side1Ok { get; set; }
        public bool Side2Ok { get; set; }
        public bool Side3Ok { get; set; }
        public bool Side4Ok { get; set; }
        public string Raw { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool IsAllOk
        {
            get { return Side1Ok && Side2Ok && Side3Ok && Side4Ok; }
        }
    }

    public interface IVisionTpuClient
    {
        Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs = 1000);
        Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<bool> StartBottomInspectAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<BottomVisionOffset> WaitBottomMResultAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<BottomVisionOffset> WaitBottomFinalResultAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<BottomVisionOffset> WaitBottomResultAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<BottomVisionOffset> GetBottomResultAsync(int pickerNo, int timeoutMs = 5000);
        Task<BottomVisionOffset> GetBottomResultAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs = 5000);
        Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs, CancellationToken ct);
        Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs = 1000);
        Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs, CancellationToken ct);
        Task<bool> StartSideInspectAsync(int pickerNo, int angleDeg, int timeoutMs, CancellationToken ct);
        Task<SideVisionResult> WaitSideResultAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        void AbandonPendingInspection(int pickerNo, string reason);
        Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs = 5000);
        Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs, CancellationToken ct);
    }

    public sealed class PickerRuntimeTool
    {
        private readonly Func<PickerRuntimeToolSetup> _setupFactory;
        private readonly Func<PickerRuntimeToolRecipe> _recipeFactory;
        private readonly Action _vacuumOn;
        private readonly Action _vacuumOff;
        private readonly Action _blowOn;
        private readonly Action _blowOff;

        public PickerRuntimeTool(
            BaseAxis pickerZ,
            BaseAxis pickerT,
            Func<PickerRuntimeToolSetup> setupFactory,
            Func<PickerRuntimeToolRecipe> recipeFactory,
            Action vacuumOn,
            Action vacuumOff,
            Action blowOn,
            Action blowOff)
        {
            PickerZ = pickerZ;
            PickerT = pickerT;
            _setupFactory = setupFactory;
            _recipeFactory = recipeFactory;
            _vacuumOn = vacuumOn;
            _vacuumOff = vacuumOff;
            _blowOn = blowOn;
            _blowOff = blowOff;
        }

        public BaseAxis PickerZ { get; private set; }
        public BaseAxis PickerT { get; private set; }
        public PickerRuntimeToolSetup Setup { get { return _setupFactory(); } }
        public PickerRuntimeToolRecipe Recipe { get { return _recipeFactory(); } }

        public void VacuumOn() { _vacuumOn(); }
        public void VacuumOff() { _vacuumOff(); }
        public void BlowOn() { _blowOn(); }
        public void BlowOff() { _blowOff(); }
    }

    public sealed class PickerRuntimeToolSetup
    {
        public double ColletOffsetX { get; set; }
        public double ColletOffsetY { get; set; }
        public double PickupPosition { get; set; }
        public double WaitPosition { get; set; }
        public double PlacePosition { get; set; }
    }

    public sealed class PickerRuntimeToolRecipe
    {
        public double ZVelocity { get; set; }
        public double ThetaVelocity { get; set; }
        public int VacuumSettleMs { get; set; }
        public double PickLiftPosition { get; set; }
        public int PickLiftWaitMs { get; set; }
        public int PlaceDelayMs { get; set; }
    }
}
