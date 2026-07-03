using System;
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

    public enum PickerBottomFlyingZDownMode
    {
        Off = 0,
        DownDistance = 1,
        ToBottomPosition = 2
    }

    public enum PickerBottomFlyingZStartMode
    {
        Immediate = 0,
        DelayMs = 1,
        XRemainingDistance = 2
    }

    public enum PickerPlaceMotionMode
    {
        Default = 0,
        SynchronizedArrival = 1,
        ContiSegmentedPlace = 2
    }

    [DataContract]
    public sealed class PickerPickUpMotionConfig
    {
        [DataMember] public PickerPickUpZMotionMode MotionMode { get; set; } = PickerPickUpZMotionMode.Detailed;
        [DataMember] public double PickerZPrePickDistance { get; set; } = 1.0;
        [DataMember] public double PickerZSlowApproachSpeedPercent { get; set; } = 1.0;
        [DataMember] public double PickerZSyncLiftDistance { get; set; } = 0.5;
        [DataMember] public double PickerZSyncLiftVelocity { get; set; } = 5.0;
        [DataMember] public double PickerZSyncLiftAcceleration { get; set; } = 100.0;
        [DataMember] public double PickerZSyncLiftDeceleration { get; set; } = 100.0;
        [DataMember] public double PickerZSeparateDistance { get; set; } = 1.0;
        [DataMember] public double PickerZSeparateSpeedPercent { get; set; } = 1.0;
        [DataMember] public PickerPickUpSeparateMode SeparateMode { get; set; } = PickerPickUpSeparateMode.Simultaneous;
        [DataMember] public int VacuumOnBeforePickDelayMs { get; set; } = 0;
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

            PickerZPrePickDistance = NormalizeDistance(PickerZPrePickDistance);
            PickerZSlowApproachSpeedPercent = NormalizePercent(PickerZSlowApproachSpeedPercent, 1.0);
            PickerZSyncLiftDistance = NormalizeDistance(PickerZSyncLiftDistance);
            PickerZSyncLiftVelocity = NormalizePositive(PickerZSyncLiftVelocity, 5.0);
            PickerZSyncLiftAcceleration = NormalizePositive(PickerZSyncLiftAcceleration, 100.0);
            PickerZSyncLiftDeceleration = NormalizePositive(PickerZSyncLiftDeceleration, 100.0);
            PickerZSeparateDistance = NormalizeDistance(PickerZSeparateDistance);
            PickerZSeparateSpeedPercent = NormalizePercent(PickerZSeparateSpeedPercent, 1.0);

            if (VacuumOnBeforePickDelayMs < 0)
                VacuumOnBeforePickDelayMs = 0;
            if (PickSettleMs < 0)
                PickSettleMs = 0;
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

        private static double NormalizeDistance(double distance)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0.0)
                return 0.0;
            return distance;
        }
    }

    [DataContract]
    public sealed class PickerBottomInspectionMotionConfig
    {
        [DataMember] public PickerBottomFlyingZDownMode FlyingZDownMode { get; set; } = PickerBottomFlyingZDownMode.Off;
        [DataMember] public double FlyingZDownDistance { get; set; } = 2.0;
        [DataMember] public PickerBottomFlyingZStartMode FlyingZStartMode { get; set; } = PickerBottomFlyingZStartMode.XRemainingDistance;
        [DataMember] public int FlyingZStartDelayMs { get; set; } = 0;
        [DataMember] public double FlyingZStartXRemainingDistance { get; set; } = 5.0;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            Ensure();
        }

        public void Ensure()
        {
            FlyingZDownDistance = NormalizeDistance(FlyingZDownDistance);
            if (FlyingZStartDelayMs < 0)
                FlyingZStartDelayMs = 0;
            FlyingZStartXRemainingDistance = NormalizeDistance(FlyingZStartXRemainingDistance);
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
        [DataMember] public PickerPlaceMotionMode MotionMode { get; set; } = PickerPlaceMotionMode.Default;
        [DataMember] public int InterpolationCoordinate { get; set; } = 0;
        [DataMember] public double SynchronizedVelocity { get; set; } = 1.0;
        [DataMember] public double SynchronizedAcceleration { get; set; } = 10.0;
        [DataMember] public double SynchronizedDeceleration { get; set; } = 10.0;
        [DataMember] public int SynchronizedTimeoutMs { get; set; } = 5000;
        [DataMember] public double MaxSynchronizedTravelDistance { get; set; } = 37.0;
        [DataMember] public double ContiZ1Step1Clearance { get; set; } = 0.15;
        [DataMember] public double ContiZ1Step2Clearance { get; set; } = 0.15;
        [DataMember] public double ContiNearAvoidDistance { get; set; } = 1.0;
        [DataMember] public double ContiXYMidRatio { get; set; } = 0.5;
        [DataMember] public double ContiOverDrive { get; set; } = 0.05;
        [DataMember] public double ContiTapeThicknessFallback { get; set; } = 0.10;
        [DataMember] public double ContiDieThicknessFallback { get; set; } = 0.15;
        [DataMember] public double ContiNode0Velocity { get; set; } = 2.0;
        [DataMember] public double ContiNode0Acceleration { get; set; } = 20.0;
        [DataMember] public double ContiNode0Deceleration { get; set; } = 20.0;
        [DataMember] public double ContiNode1Velocity { get; set; } = 5.0;
        [DataMember] public double ContiNode1Acceleration { get; set; } = 50.0;
        [DataMember] public double ContiNode1Deceleration { get; set; } = 50.0;
        [DataMember] public double ContiNode2Velocity { get; set; } = 5.0;
        [DataMember] public double ContiNode2Acceleration { get; set; } = 50.0;
        [DataMember] public double ContiNode2Deceleration { get; set; } = 50.0;
        [DataMember] public double ContiNode3Velocity { get; set; } = 2.0;
        [DataMember] public double ContiNode3Acceleration { get; set; } = 20.0;
        [DataMember] public double ContiNode3Deceleration { get; set; } = 20.0;
        [DataMember] public double ContiNode4Velocity { get; set; } = 0.5;
        [DataMember] public double ContiNode4Acceleration { get; set; } = 10.0;
        [DataMember] public double ContiNode4Deceleration { get; set; } = 10.0;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            Ensure();
        }

        public void Ensure()
        {
            if (InterpolationCoordinate < 0)
                InterpolationCoordinate = 0;
            SynchronizedVelocity = PickerPickUpMotionConfig.NormalizePositive(SynchronizedVelocity, 1.0);
            SynchronizedAcceleration = PickerPickUpMotionConfig.NormalizePositive(SynchronizedAcceleration, 10.0);
            SynchronizedDeceleration = PickerPickUpMotionConfig.NormalizePositive(SynchronizedDeceleration, 10.0);
            if (SynchronizedTimeoutMs <= 0)
                SynchronizedTimeoutMs = 5000;
            MaxSynchronizedTravelDistance = PickerPickUpMotionConfig.NormalizePositive(MaxSynchronizedTravelDistance, 37.0);
            ContiZ1Step1Clearance = NormalizeNonNegative(ContiZ1Step1Clearance);
            ContiZ1Step2Clearance = NormalizeNonNegative(ContiZ1Step2Clearance);
            ContiNearAvoidDistance = NormalizeNonNegative(ContiNearAvoidDistance);
            ContiXYMidRatio = NormalizeRatio(ContiXYMidRatio, 0.5);
            ContiOverDrive = NormalizeNonNegative(ContiOverDrive);
            ContiTapeThicknessFallback = NormalizeNonNegative(ContiTapeThicknessFallback);
            ContiDieThicknessFallback = NormalizeNonNegative(ContiDieThicknessFallback);
            ContiNode0Velocity = PickerPickUpMotionConfig.NormalizePositive(ContiNode0Velocity, 2.0);
            ContiNode0Acceleration = PickerPickUpMotionConfig.NormalizePositive(ContiNode0Acceleration, 20.0);
            ContiNode0Deceleration = PickerPickUpMotionConfig.NormalizePositive(ContiNode0Deceleration, 20.0);
            ContiNode1Velocity = PickerPickUpMotionConfig.NormalizePositive(ContiNode1Velocity, 5.0);
            ContiNode1Acceleration = PickerPickUpMotionConfig.NormalizePositive(ContiNode1Acceleration, 50.0);
            ContiNode1Deceleration = PickerPickUpMotionConfig.NormalizePositive(ContiNode1Deceleration, 50.0);
            ContiNode2Velocity = PickerPickUpMotionConfig.NormalizePositive(ContiNode2Velocity, 5.0);
            ContiNode2Acceleration = PickerPickUpMotionConfig.NormalizePositive(ContiNode2Acceleration, 50.0);
            ContiNode2Deceleration = PickerPickUpMotionConfig.NormalizePositive(ContiNode2Deceleration, 50.0);
            ContiNode3Velocity = PickerPickUpMotionConfig.NormalizePositive(ContiNode3Velocity, 2.0);
            ContiNode3Acceleration = PickerPickUpMotionConfig.NormalizePositive(ContiNode3Acceleration, 20.0);
            ContiNode3Deceleration = PickerPickUpMotionConfig.NormalizePositive(ContiNode3Deceleration, 20.0);
            ContiNode4Velocity = PickerPickUpMotionConfig.NormalizePositive(ContiNode4Velocity, 0.5);
            ContiNode4Acceleration = PickerPickUpMotionConfig.NormalizePositive(ContiNode4Acceleration, 10.0);
            ContiNode4Deceleration = PickerPickUpMotionConfig.NormalizePositive(ContiNode4Deceleration, 10.0);
        }

        public double GetContiNodeVelocity(int nodeIndex)
        {
            switch (nodeIndex)
            {
                case 0: return ContiNode0Velocity;
                case 1: return ContiNode1Velocity;
                case 2: return ContiNode2Velocity;
                case 3: return ContiNode3Velocity;
                case 4: return ContiNode4Velocity;
                default: return ContiNode4Velocity;
            }
        }

        public double GetContiNodeAcceleration(int nodeIndex)
        {
            switch (nodeIndex)
            {
                case 0: return ContiNode0Acceleration;
                case 1: return ContiNode1Acceleration;
                case 2: return ContiNode2Acceleration;
                case 3: return ContiNode3Acceleration;
                case 4: return ContiNode4Acceleration;
                default: return ContiNode4Acceleration;
            }
        }

        public double GetContiNodeDeceleration(int nodeIndex)
        {
            switch (nodeIndex)
            {
                case 0: return ContiNode0Deceleration;
                case 1: return ContiNode1Deceleration;
                case 2: return ContiNode2Deceleration;
                case 3: return ContiNode3Deceleration;
                case 4: return ContiNode4Deceleration;
                default: return ContiNode4Deceleration;
            }
        }

        private static double NormalizeNonNegative(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
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
        public bool IsOk { get; set; }
    }

    public class SideVisionResult
    {
        public int PickerNo { get; set; }
        public bool Side1Ok { get; set; }
        public bool Side2Ok { get; set; }
        public bool Side3Ok { get; set; }
        public bool Side4Ok { get; set; }

        public bool IsAllOk
        {
            get { return Side1Ok && Side2Ok && Side3Ok && Side4Ok; }
        }
    }

    public interface IVisionTpuClient
    {
        Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs = 1000);
        Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs = 5000);
        Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs, CancellationToken ct);
        Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs = 1000);
        Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs, CancellationToken ct);
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
