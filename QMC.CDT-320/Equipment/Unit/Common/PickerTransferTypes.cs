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

    [DataContract]
    public sealed class PickerPickUpMotionConfig
    {
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
