using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;

namespace QMC.CDT320
{
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
