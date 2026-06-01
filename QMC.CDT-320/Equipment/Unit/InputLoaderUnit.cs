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
        public InputCassetteUnit InputCassette { get; private set; }
        public InputFeederUnit WaferFeeder { get; private set; }

        public BaseAxis WaferLifterZ { get { return InputCassette.WaferLifterZ; } }
        public BaseAxis FeederY { get { return WaferFeeder.FeederY; } }

        public BaseDigitalInput CassetteExistSensor { get { return InputCassette.CassetteExistSensor; } }
        public BaseDigitalInput ProtrusionSensor { get { return InputCassette.ProtrusionSensor; } }
        public BaseDigitalInput WaferDetectSensor { get { return InputCassette.WaferDetectSensor; } }
        public BaseDigitalInput WaferClampedSensor { get { return WaferFeeder.WaferClampedSensor; } }

        public BaseCylinder FeederUpDownCyl { get { return WaferFeeder.InputFeederLift; } }
        public BaseCylinder FeederClampCyl { get { return WaferFeeder.InputFeederClamp; } }

        public IReadOnlyList<bool> WaferMap { get { return InputCassette.WaferMap; } }

        public InputLoaderUnit() : base("InputLoaderUnit")
        {
            InputCassette = new InputCassetteUnit();
            WaferFeeder = new InputFeederUnit();

            Components.Add(InputCassette);
            Components.Add(WaferFeeder);
        }

        public Task<int> ScanCassetteAsync(int maxSlots, double slotPitch)
        {
            SyncChildSettings();
            return InputCassette.ScanCassetteAsync(maxSlots, slotPitch);
        }

        public Task<int> MoveToTargetSlotAsync(double targetPosition)
        {
            SyncChildSettings();
            return InputCassette.MoveToTargetSlotAsync(targetPosition);
        }

        public Task<int> MoveToExchangePositionAsync()
        {
            SyncChildSettings();
            return WaferFeeder.MoveToExchangePositionAsync();
        }

        public Task<int> RetractFeederAsync()
        {
            SyncChildSettings();
            return WaferFeeder.RetractFeederAsync();
        }

        private void SyncChildSettings()
        {
        }
    }
}
