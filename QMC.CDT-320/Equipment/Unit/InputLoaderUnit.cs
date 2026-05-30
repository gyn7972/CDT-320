using System.Collections.Generic;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    public class InputLoaderSetup : ISetupData
    {
    }

    public class InputLoaderConfig : IConfigData
    {
    }

    public class InputLoaderRecipe : IRecipeData
    {
    }

    public class InputLoaderUnit : BaseUnit<InputLoaderSetup, InputLoaderConfig, InputLoaderRecipe>
    {
        public InputCassetteUnit IndexCassette { get; private set; }
        public InputFeederUnit WaferFeeder { get; private set; }

        public BaseAxis WaferLifterZ { get { return IndexCassette.WaferLifterZ; } }
        public BaseAxis FeederY { get { return WaferFeeder.FeederY; } }

        public BaseDigitalInput CassetteExistSensor { get { return IndexCassette.CassetteExistSensor; } }
        public BaseDigitalInput ProtrusionSensor { get { return IndexCassette.ProtrusionSensor; } }
        public BaseDigitalInput WaferDetectSensor { get { return IndexCassette.WaferDetectSensor; } }
        public BaseDigitalInput WaferClampedSensor { get { return WaferFeeder.WaferClampedSensor; } }

        public BaseCylinder FeederUpDownCyl { get { return WaferFeeder.FeederUpDownCyl; } }
        public BaseCylinder FeederClampCyl { get { return WaferFeeder.FeederClampCyl; } }

        public IReadOnlyList<bool> WaferMap { get { return IndexCassette.WaferMap; } }

        public InputLoaderUnit() : base("InputLoaderUnit")
        {
            IndexCassette = new InputCassetteUnit();
            WaferFeeder = new InputFeederUnit();

            Components.Add(IndexCassette);
            Components.Add(WaferFeeder);
        }

        public Task<int> ScanCassetteAsync(int maxSlots, double slotPitch)
        {
            SyncChildSettings();
            return IndexCassette.ScanCassetteAsync(maxSlots, slotPitch);
        }

        public Task<int> MoveToTargetSlotAsync(double targetPosition)
        {
            SyncChildSettings();
            return IndexCassette.MoveToTargetSlotAsync(targetPosition);
        }

        public Task<bool> MoveToExchangePositionAsync()
        {
            SyncChildSettings();
            return WaferFeeder.MoveToExchangePositionAsync();
        }

        public Task<bool> RetractFeederAsync()
        {
            SyncChildSettings();
            return WaferFeeder.RetractFeederAsync();
        }

        private void SyncChildSettings()
        {
        }
    }
}
