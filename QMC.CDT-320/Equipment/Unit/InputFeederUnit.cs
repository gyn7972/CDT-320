using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    public class InputFeederSetup : ISetupData
    {
        public double AvoidPosition { get; set; } = 0.0;
        public double ExchangePositionY { get; set; } = 150.0;
        public double LoadReadyPosition { get; set; } = 20.0;
        public double CassetteLoadBasePosition { get; set; } = 30.0;
        public double CassetteLoadPitch { get; set; } = 5.0;
        public double WaferPickupPosition { get; set; } = 100.0;
        public double StagePutPosition { get; set; } = 160.0;
        public double StagePickPosition { get; set; } = 170.0;
        public double InPositionTolerance { get; set; } = 0.05;
    }

    public class InputFeederConfig : IConfigData
    {
        public bool IsSimulationMode { get; set; } = true;
    }

    public class InputFeederRecipe : IRecipeData
    {
        public double MoveVelocity { get; set; } = 50.0;
        public int FeederMoveTimeoutMs { get; set; } = 5000;
        public int CylinderTimeoutMs { get; set; } = 1000;
    }

    public class InputFeederUnit : BaseUnit<InputFeederSetup, InputFeederConfig, InputFeederRecipe>
    {
        private readonly Dictionary<string, double> positionSnapshots = new Dictionary<string, double>();

        public BaseAxis FeederY { get; private set; }

        public BaseDigitalInput WaferFeederUpSensor { get; private set; }
        public BaseDigitalInput WaferFeederDownSensor { get; private set; }
        public BaseDigitalInput WaferFeederClampSensor { get; private set; }
        public BaseDigitalInput WaferFeederRingCheckSensor { get; private set; }
        public BaseDigitalInput WaferFeederOverloadSensor { get; private set; }

        public BaseDigitalInput WaferClampedSensor { get { return WaferFeederClampSensor; } }

        public BaseCylinder FeederUpDownCyl { get; private set; }
        public BaseCylinder FeederClampCyl { get; private set; }

        public BaseDigitalOutput WaferFeederUpOut { get { return FeederUpDownCyl.OutFwd; } }
        public BaseDigitalOutput WaferFeederDownOut { get { return FeederUpDownCyl.OutBwd; } }
        public BaseDigitalOutput WaferFeederClampOut { get { return FeederClampCyl.OutFwd; } }
        public BaseDigitalOutput WaferFeederUnclampOut { get { return FeederClampCyl.OutBwd; } }

        public InputFeederUnit() : base("WaferFeederUnit")
        {
            FeederY = AjinFactory.CreateAxis("FeederY");
            WaferFeederUpSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederUp);
            WaferFeederDownSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederDown);
            WaferFeederClampSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederUpClamp);
            WaferFeederRingCheckSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederRingCheck);
            WaferFeederOverloadSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeederOverloadCheck);
            FeederUpDownCyl = AjinFactory.CreateCylinder(AjinIoCatalog.CylinderRefs.WaferFeederUpDownCyl);
            FeederClampCyl = AjinFactory.CreateCylinder(AjinIoCatalog.CylinderRefs.WaferFeederClampCyl);

            Components.Add(FeederY);
            Components.Add(WaferFeederUpSensor);
            Components.Add(WaferFeederDownSensor);
            Components.Add(WaferFeederClampSensor);
            Components.Add(WaferFeederRingCheckSensor);
            Components.Add(WaferFeederOverloadSensor);
            Components.Add(FeederUpDownCyl);
            Components.Add(FeederClampCyl);
        }

        public async Task MoveWaferFeederY(double targetPos, bool bFine = false)
        {
            await FeederY.MoveAbsoluteAsync(targetPos, bFine ? Recipe.MoveVelocity * 0.5 : Recipe.MoveVelocity);
            if (FeederY.IsAlarm)
                throw new InvalidOperationException("'" + Name + "' MoveWaferFeederY: FeederY alarm.");
        }

        public Task MoveWaferFeederYToTeachingPosition(string positionName, bool bFine = false)
        {
            return MoveWaferFeederY(GetTeachingPosition(positionName), bFine);
        }

        public Task MoveToWaferFeederAvoidPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.AvoidPosition, bFine);
        }

        public Task MoveToWaferFeederLoadReadyPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.LoadReadyPosition, bFine);
        }

        public Task MoveToWaferFeederCassetteLoadPosition(int slotIndex, bool bFine = false)
        {
            return MoveWaferFeederY(CalculateWaferFeederCassetteLoadPosition(slotIndex), bFine);
        }

        public Task MoveToWaferFeederWaferPickupPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.WaferPickupPosition, bFine);
        }

        public Task MoveToWaferFeederStagePutPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.StagePutPosition, bFine);
        }

        public Task MoveToWaferFeederStagePickPosition(bool bFine = false)
        {
            return MoveWaferFeederY(Setup.StagePickPosition, bFine);
        }

        public async Task<bool> WaitWaferFeederYMoveDone(int timeoutMs)
        {
            return await WaitUntilAsync(() => !FeederY.IsMoving && FeederY.IsInPosition && !FeederY.IsAlarm, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederYInPosition(string positionName, int timeoutMs)
        {
            double target = GetTeachingPosition(positionName);
            return await WaitUntilAsync(() => IsWaferFeederYInPosition(target, Setup.InPositionTolerance), timeoutMs);
        }

        public bool IsWaferFeederYInPosition(double targetPos, double tolerance)
        {
            return Math.Abs(FeederY.ActualPosition - targetPos) <= tolerance;
        }

        public bool IsWaferFeederInAvoidPosition()
        {
            return IsWaferFeederYInPosition(Setup.AvoidPosition, Setup.InPositionTolerance);
        }

        public bool IsWaferFeederInCassetteLoadPosition(int slotIndex)
        {
            return IsWaferFeederYInPosition(CalculateWaferFeederCassetteLoadPosition(slotIndex), Setup.InPositionTolerance);
        }

        public bool IsWaferFeederInLoadReadyPosition()
        {
            return IsWaferFeederYInPosition(Setup.LoadReadyPosition, Setup.InPositionTolerance);
        }

        public bool IsWaferFeederUp()
        {
            return WaferFeederUpSensor.IsOn;
        }

        public bool IsWaferFeederDown()
        {
            return WaferFeederDownSensor.IsOn;
        }

        public bool IsWaferFeederClamp()
        {
            return WaferFeederClampSensor.IsOn;
        }

        public bool IsWaferFeederUnclamp()
        {
            return !WaferFeederClampSensor.IsOn;
        }

        public bool IsWaferFeederRingCheck()
        {
            return WaferFeederRingCheckSensor.IsOn;
        }

        public void TeachWaferFeederYPosition(string positionName)
        {
            SetTeachingPosition(positionName, FeederY.ActualPosition);
        }

        public void TeachWaferFeederAvoidPosition()
        {
            Setup.AvoidPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederLoadReadyPosition()
        {
            Setup.LoadReadyPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederCassetteLoadBasePosition()
        {
            Setup.CassetteLoadBasePosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederWaferPickupPosition()
        {
            Setup.WaferPickupPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederStagePutPosition()
        {
            Setup.StagePutPosition = FeederY.ActualPosition;
        }

        public void TeachWaferFeederStagePickPosition()
        {
            Setup.StagePickPosition = FeederY.ActualPosition;
        }

        public double CalculateWaferFeederCassetteLoadPosition(int slotIndex)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException("slotIndex");
            return Setup.CassetteLoadBasePosition + (Setup.CassetteLoadPitch * slotIndex);
        }

        public bool ValidateWaferFeederTeachingComplete()
        {
            return Setup.CassetteLoadPitch > 0.0 &&
                   Setup.LoadReadyPosition != Setup.AvoidPosition &&
                   Setup.StagePutPosition != Setup.StagePickPosition;
        }

        public async Task<bool> MoveToTeachingPositionAndVerify(string positionName, bool bFine = false)
        {
            await MoveWaferFeederYToTeachingPosition(positionName, bFine);
            return await WaitWaferFeederYInPosition(positionName, Recipe.FeederMoveTimeoutMs);
        }

        public async Task<bool> SetWaferFeederUpDown(bool up)
        {
            if (up)
                return await FeederUpDownCyl.MoveFwdAsync();
            return await FeederUpDownCyl.MoveBwdAsync();
        }

        public async Task<bool> SetWaferFeederClamp(bool clamp)
        {
            if (clamp)
                return await FeederClampCyl.MoveFwdAsync();
            return await FeederClampCyl.MoveBwdAsync();
        }

        public async Task<bool> WaitWaferFeederUp(int timeoutMs)
        {
            return await WaferFeederUpSensor.WaitUntilStateAsync(true, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederDown(int timeoutMs)
        {
            return await WaferFeederDownSensor.WaitUntilStateAsync(true, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederClamp(int timeoutMs)
        {
            return await WaferFeederClampSensor.WaitUntilStateAsync(true, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederUnclamp(int timeoutMs)
        {
            return await WaferFeederClampSensor.WaitUntilStateAsync(false, timeoutMs);
        }

        public async Task<bool> WaitWaferFeederRingClear(int timeoutMs)
        {
            return await WaferFeederRingCheckSensor.WaitUntilStateAsync(false, timeoutMs);
        }

        public void ManualMoveWaferFeederYJog(int direction, double speed)
        {
            FeederY.MoveJogContinuous(direction, JogSpeedType.Custom, speed);
        }

        public void ManualStopWaferFeederY()
        {
            FeederY.StopJog();
        }

        public Task ManualMoveToWaferFeederAvoidPosition(bool bFine = false)
        {
            return MoveToWaferFeederAvoidPosition(bFine);
        }

        public Task ManualMoveToWaferFeederCassetteLoadPosition(int slotIndex, bool bFine = false)
        {
            return MoveToWaferFeederCassetteLoadPosition(slotIndex, bFine);
        }

        public Task ManualMoveToWaferFeederLoadReadyPosition(bool bFine = false)
        {
            return MoveToWaferFeederLoadReadyPosition(bFine);
        }

        public async Task<bool> LoadWaferFromCassetteToFeeder(int slotIndex, int timeoutMs, bool bFine = false)
        {
            if (!CheckWaferFeederTransferReady(TransferMode.Load))
                return false;

            await MoveToWaferFeederCassetteLoadPosition(slotIndex, bFine);
            if (!await WaitWaferFeederYMoveDone(timeoutMs))
                return false;

            if (!await SetWaferFeederUpDown(true)) return false;
            if (!await SetWaferFeederClamp(true)) return false;
            return await WaitWaferFeederClamp(timeoutMs);
        }

        public async Task<bool> MoveWaferFeederToStagePutPosition(int timeoutMs, bool bFine = false)
        {
            if (!HasWaferOnFeeder())
                return false;

            await MoveToWaferFeederStagePutPosition(bFine);
            return await WaitWaferFeederYMoveDone(timeoutMs);
        }

        public async Task<bool> MoveWaferFeederToStagePickPosition(int timeoutMs, bool bFine = false)
        {
            await MoveToWaferFeederStagePickPosition(bFine);
            return await WaitWaferFeederYMoveDone(timeoutMs);
        }

        public async Task<bool> ReturnWaferFromFeederToCassette(int slotIndex, int timeoutMs, bool bFine = false)
        {
            await MoveToWaferFeederCassetteLoadPosition(slotIndex, bFine);
            if (!await WaitWaferFeederYMoveDone(timeoutMs))
                return false;

            if (!await SetWaferFeederClamp(false)) return false;
            if (!await SetWaferFeederUpDown(false)) return false;
            return await WaitWaferFeederUnclamp(timeoutMs);
        }

        public async Task<bool> RecoverWaferFeederToSafeState(int timeoutMs, bool unclamp = true)
        {
            if (unclamp && !await SetWaferFeederClamp(false))
                return false;

            if (!await SetWaferFeederUpDown(false))
                return false;

            await MoveToWaferFeederAvoidPosition();
            return await WaitWaferFeederYMoveDone(timeoutMs);
        }

        public bool CheckWaferFeederMoveReady()
        {
            return !FeederY.IsAlarm && !WaferFeederOverloadSensor.IsOn;
        }

        public bool CheckWaferFeederTransferReady(TransferMode mode)
        {
            if (!CheckWaferFeederMoveReady())
                return false;
            if (mode == TransferMode.Load)
                return IsWaferFeederDown() && IsWaferFeederUnclamp();
            if (mode == TransferMode.Unload)
                return HasWaferOnFeeder();
            return true;
        }

        public bool CheckWaferCassetteLoadReady(int slotIndex, TransferMode mode)
        {
            if (slotIndex < 0)
                return false;
            return CheckWaferFeederTransferReady(mode) && ValidateWaferFeederTeachingComplete();
        }

        public WaferFeederProcessState GetWaferFeederProcessState()
        {
            if (FeederY.IsAlarm || WaferFeederOverloadSensor.IsOn)
                return WaferFeederProcessState.Alarm;
            if (FeederY.IsMoving)
                return WaferFeederProcessState.Moving;
            if (HasWaferOnFeeder())
                return WaferFeederProcessState.HasWafer;
            return WaferFeederProcessState.Empty;
        }

        public bool HasWaferOnFeeder()
        {
            return WaferFeederClampSensor.IsOn || WaferFeederRingCheckSensor.IsOn;
        }

        public bool IsWaferFeederSafe()
        {
            return !WaferFeederOverloadSensor.IsOn && !FeederY.IsAlarm;
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

        public async Task<bool> MoveToExchangePositionAsync()
        {
            bool cylResult = await SetWaferFeederUpDown(true);
            if (!cylResult)
            {
                Console.WriteLine("[ALARM] '" + Name + "' MoveToExchangePosition: feeder up/down timeout.");
                return false;
            }

            cylResult = await SetWaferFeederClamp(true);
            if (!cylResult)
            {
                Console.WriteLine("[ALARM] '" + Name + "' MoveToExchangePosition: feeder clamp timeout.");
                return false;
            }

            bool clamped = await WaitWaferFeederClamp(Recipe.CylinderTimeoutMs);
            if (!clamped)
            {
                await SetWaferFeederClamp(false);
                await SetWaferFeederUpDown(false);
                Console.WriteLine("[ALARM] '" + Name + "' MoveToExchangePosition: wafer clamp sensor timeout.");
                return false;
            }

            await MoveWaferFeederY(Setup.ExchangePositionY);
            return !FeederY.IsAlarm;
        }

        public async Task<bool> RetractFeederAsync()
        {
            if (!await SetWaferFeederClamp(false))
            {
                Console.WriteLine("[ALARM] '" + Name + "' RetractFeeder: feeder unclamp timeout.");
                return false;
            }

            bool released = await WaitWaferFeederUnclamp(Recipe.CylinderTimeoutMs);
            if (!released)
            {
                Console.WriteLine("[ALARM] '" + Name + "' RetractFeeder: wafer clamp sensor did not turn OFF.");
                return false;
            }

            await MoveToWaferFeederAvoidPosition();
            if (FeederY.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' RetractFeeder: FeederY origin move failed.");
                return false;
            }

            if (!await SetWaferFeederUpDown(false))
            {
                Console.WriteLine("[ALARM] '" + Name + "' RetractFeeder: feeder down timeout.");
                return false;
            }

            return true;
        }

        private double GetTeachingPosition(string positionName)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) return Setup.AvoidPosition;
            if (string.Equals(positionName, "Exchange", StringComparison.OrdinalIgnoreCase)) return Setup.ExchangePositionY;
            if (string.Equals(positionName, "LoadReady", StringComparison.OrdinalIgnoreCase)) return Setup.LoadReadyPosition;
            if (string.Equals(positionName, "CassetteLoadBase", StringComparison.OrdinalIgnoreCase)) return Setup.CassetteLoadBasePosition;
            if (string.Equals(positionName, "WaferPickup", StringComparison.OrdinalIgnoreCase)) return Setup.WaferPickupPosition;
            if (string.Equals(positionName, "StagePut", StringComparison.OrdinalIgnoreCase)) return Setup.StagePutPosition;
            if (string.Equals(positionName, "StagePick", StringComparison.OrdinalIgnoreCase)) return Setup.StagePickPosition;
            throw new ArgumentException("Unknown WaferFeederY teaching position: " + positionName, "positionName");
        }

        private void SetTeachingPosition(string positionName, double position)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) Setup.AvoidPosition = position;
            else if (string.Equals(positionName, "Exchange", StringComparison.OrdinalIgnoreCase)) Setup.ExchangePositionY = position;
            else if (string.Equals(positionName, "LoadReady", StringComparison.OrdinalIgnoreCase)) Setup.LoadReadyPosition = position;
            else if (string.Equals(positionName, "CassetteLoadBase", StringComparison.OrdinalIgnoreCase)) Setup.CassetteLoadBasePosition = position;
            else if (string.Equals(positionName, "WaferPickup", StringComparison.OrdinalIgnoreCase)) Setup.WaferPickupPosition = position;
            else if (string.Equals(positionName, "StagePut", StringComparison.OrdinalIgnoreCase)) Setup.StagePutPosition = position;
            else if (string.Equals(positionName, "StagePick", StringComparison.OrdinalIgnoreCase)) Setup.StagePickPosition = position;
            else throw new ArgumentException("Unknown WaferFeederY teaching position: " + positionName, "positionName");
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
