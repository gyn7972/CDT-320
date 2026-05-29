using System.Collections.Generic;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    public class InputLoaderSetup : ISetupData
    {
        public double FirstSlotPosition { get; set; } = 10.0;
        public double ExchangePositionY { get; set; } = 150.0;
        public double CassetteSlotPitch { get; set; } = 5.0;
        public int CassetteSlotCount { get; set; } = 25;
    }

    public class InputLoaderConfig : IConfigData
    {
        public bool IsSimulationMode { get; set; } = true;
    }

    public class InputLoaderRecipe : IRecipeData
    {
        public double ScanVelocity { get; set; } = 20.0;
        public int ScanSettleTimeMs { get; set; } = 100;
        public int ElevatorMoveTimeoutMs { get; set; } = 10000;
        public int FeederMoveTimeoutMs { get; set; } = 5000;
    }

    public class InputLoaderUnit : BaseUnit<InputLoaderSetup, InputLoaderConfig, InputLoaderRecipe>
    {
        public WaferCassetteUnit WaferCassette { get; private set; }
        public WaferFeederUnit WaferFeeder { get; private set; }

        public BaseAxis WaferLifterZ { get { return WaferCassette.WaferLifterZ; } }
        public BaseAxis FeederY { get { return WaferFeeder.FeederY; } }

        public BaseDigitalInput CassetteExistSensor { get { return WaferCassette.CassetteExistSensor; } }
        public BaseDigitalInput ProtrusionSensor { get { return WaferCassette.ProtrusionSensor; } }
        public BaseDigitalInput WaferDetectSensor { get { return WaferCassette.WaferDetectSensor; } }
        public BaseDigitalInput WaferClampedSensor { get { return WaferFeeder.WaferClampedSensor; } }

        public BaseCylinder FeederUpDownCyl { get { return WaferFeeder.FeederUpDownCyl; } }
        public BaseCylinder FeederClampCyl { get { return WaferFeeder.FeederClampCyl; } }

        public IReadOnlyList<bool> WaferMap { get { return WaferCassette.WaferMap; } }

        public InputLoaderUnit() : base("InputLoaderUnit")
        {
            WaferCassette = new WaferCassetteUnit();
            WaferFeeder = new WaferFeederUnit();

            Components.Add(WaferCassette);
            Components.Add(WaferFeeder);
        }

        public Task<bool> ScanCassetteAsync(int maxSlots, double slotPitch)
        {
            SyncChildSettings();
            return WaferCassette.ScanCassetteAsync(maxSlots, slotPitch);
        }

        public Task MoveToTargetSlotAsync(double targetPosition)
        {
            SyncChildSettings();
            return WaferCassette.MoveToTargetSlotAsync(targetPosition);
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
            WaferCassette.Setup.FirstSlotPosition = Setup.FirstSlotPosition;
            WaferCassette.Setup.SlotPitch = Setup.CassetteSlotPitch;
            WaferCassette.Setup.SlotCount = Setup.CassetteSlotCount;
            WaferCassette.Config.IsSimulationMode = Config.IsSimulationMode;
            WaferCassette.Recipe.ScanVelocity = Recipe.ScanVelocity;
            WaferCassette.Recipe.ScanSettleTimeMs = Recipe.ScanSettleTimeMs;
            WaferCassette.Recipe.ElevatorMoveTimeoutMs = Recipe.ElevatorMoveTimeoutMs;

            WaferFeeder.Setup.ExchangePositionY = Setup.ExchangePositionY;
            WaferFeeder.Config.IsSimulationMode = Config.IsSimulationMode;
            WaferFeeder.Recipe.FeederMoveTimeoutMs = Recipe.FeederMoveTimeoutMs;
        }
    }
}
