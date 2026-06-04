using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    [DataContract]
    public class OutCassetteSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }

        public OutCassetteSetup()
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
    public class OutCassetteConfig : IConfigData
    {
        [DataMember] public bool bDryRun { get; set; }
        [DataMember] public double LoadingPositionOffset { get; set; }
        [DataMember] public double UnloadingPositionOffset { get; set; }
        [DataMember] public double Level2PositionOffset { get; set; }
        [DataMember] public double GOODNGPositionOffset { get; set; }
        [DataMember] public double SlotPitch { get; set; }
        [DataMember] public int SlotCount { get; set; }
        [DataMember] public int InchSelect { get; set; }
        [DataMember] public int SelectedCassetteLevel { get; set; }
        [DataMember] public double ScanVelocity { get; set; }
        [DataMember] public double ScanAcc { get; set; }
        [DataMember] public double ScanDec { get; set; }
        [DataMember] public int ScanSettleTimeMs { get; set; }

        public OutCassetteConfig()
        {
            SetDefaults();
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            bDryRun = false;
            LoadingPositionOffset = 0.0;
            UnloadingPositionOffset = 0.0;
            Level2PositionOffset = 59.0;
            GOODNGPositionOffset = 0.0;
            SlotPitch = 6.0;
            SlotCount = 25;
            InchSelect = 0;
            SelectedCassetteLevel = 1;
            ScanVelocity = 20.0;
            ScanAcc = 0.0;
            ScanDec = 0.0;
            ScanSettleTimeMs = 100;
        }
    }

    [DataContract]
    public class OutCassetteRecipe : IRecipeData
    {
        [DataMember] public double AvoidPosition { get; set; }
        [DataMember] public double GoodLoaingPosition { get; set; }
        [DataMember] public double GoodUnloadingPosition { get; set; }
        [DataMember] public double GoodFirstSlotPosition { get; set; }
        [DataMember] public double[] GoodSlotPosition { get; private set; }
        [DataMember] public double NGLoaingPosition { get; set; }
        [DataMember] public double NGUnloadingPosition { get; set; }
        [DataMember] public double NGFirstSlotPosition { get; set; }
        [DataMember] public double[] NGSlotPosition { get; private set; }
        [DataMember] public double MappingStartPosition { get; set; }
        [DataMember] public double MappingEndPosition { get; set; }

        public OutCassetteRecipe()
        {
            SetDefaults();
        }

        public void ResizeSlotPositions(int slotCount)
        {
            int count = Math.Max(0, slotCount);
            GoodSlotPosition = new double[count];
            NGSlotPosition = new double[count];
            for (int i = 0; i < count; i++)
            {
                GoodSlotPosition[i] = double.NaN;
                NGSlotPosition[i] = double.NaN;
            }
        }

        public void EnsureSlotPositionBuffers(int slotCount)
        {
            int count = Math.Max(0, slotCount);
            if (GoodSlotPosition == null || GoodSlotPosition.Length != count ||
                NGSlotPosition == null || NGSlotPosition.Length != count)
                ResizeSlotPositions(count);
        }

        public void UpdateSlotPosition(TargetCassette cassette, int slotIndex, double position)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException("slotIndex");

            EnsureSlotPositionBuffers(slotIndex + 1);
            if (cassette == TargetCassette.Ng)
                NGSlotPosition[slotIndex] = position;
            else
                GoodSlotPosition[slotIndex] = position;
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            AvoidPosition = 0.0;
            GoodLoaingPosition = 150.0;
            GoodUnloadingPosition = 150.0;
            GoodFirstSlotPosition = 80.0;
            NGLoaingPosition = 150.0;
            NGUnloadingPosition = 150.0;
            NGFirstSlotPosition = 10.0;
            MappingStartPosition = 5.0;
            MappingEndPosition = 304.0;
            GoodSlotPosition = Array.Empty<double>();
            NGSlotPosition = Array.Empty<double>();
        }
    }

    public class OutCassetteUnit : BaseUnit<OutCassetteSetup, OutCassetteConfig, OutCassetteRecipe>
    {
        private readonly Dictionary<TargetCassette, bool[]> _slotMap = new Dictionary<TargetCassette, bool[]>();
        private readonly Dictionary<TargetCassette, Dictionary<int, WaferSlotState>> _slotStates =
            new Dictionary<TargetCassette, Dictionary<int, WaferSlotState>>();

        public BaseAxis BinLifterZ { get; private set; }
        public BaseDigitalInput GoodBin8CassetteCheck0 { get; private set; }
        public BaseDigitalInput GoodBin8CassetteCheck1 { get; private set; }
        public BaseDigitalInput GoodBin12CassetteCheck0 { get; private set; }
        public BaseDigitalInput GoodBin12CassetteCheck1 { get; private set; }
        public BaseDigitalInput NgBin8CassetteCheck0 { get; private set; }
        public BaseDigitalInput NgBin8CassetteCheck1 { get; private set; }
        public BaseDigitalInput NgBin12CassetteCheck0 { get; private set; }
        public BaseDigitalInput NgBin12CassetteCheck1 { get; private set; }
        public BaseDigitalInput NgBinCassetteBw { get; private set; }
        public BaseDigitalInput NgBinCassetteLock { get; private set; }
        public BaseDigitalInput BinRingJutCheck { get; private set; }
        public BaseDigitalInput BinMappingSensor { get; private set; }
        public BaseDigitalOutput NgBinCassetteLockOut { get; private set; }
        public BaseDigitalOutput NgBinCassetteUnlockOut { get; private set; }

        public BaseDigitalInput CassetteExistSensor { get { return GoodBin8CassetteCheck0; } }
        public BaseDigitalInput ProtrusionSensor { get { return BinRingJutCheck; } }
        public BaseDigitalInput WaferDetectSensor { get { return BinMappingSensor; } }
        public IReadOnlyDictionary<TargetCassette, bool[]> SlotMap { get { return _slotMap; } }

        public OutCassetteUnit() : base("BinCassetteUnit")
        {
            BinLifterZ = AjinFactory.CreateAxis("BinLifterZ");
            BinLifterZ.Setup.SoftLimitPlus = 400.0;

            GoodBin8CassetteCheck0 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("GoodBin8CassetteCheck0"));
            GoodBin8CassetteCheck1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("GoodBin8CassetteCheck1"));
            GoodBin12CassetteCheck0 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("GoodBin12CassetteCheck0"));
            GoodBin12CassetteCheck1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("GoodBin12CassetteCheck1"));
            NgBin8CassetteCheck0 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("NgBin8CassetteCheck0"));
            NgBin8CassetteCheck1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("NgBin8CassetteCheck1"));
            NgBin12CassetteCheck0 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("NgBin12CassetteCheck0"));
            NgBin12CassetteCheck1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("NgBin12CassetteCheck1"));
            NgBinCassetteBw = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("NgBinCassetteBw"));
            NgBinCassetteLock = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("NgBinCassetteLock"));
            BinRingJutCheck = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinRingJUTCheck"));
            BinMappingSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinMapping"));
            NgBinCassetteLockOut = AjinFactory.CreateDigitalOutput(AjinIoCatalog.FindOutput("NgBinCassetteLock"));
            NgBinCassetteUnlockOut = AjinFactory.CreateDigitalOutput(AjinIoCatalog.FindOutput("NgBinCassetteUnlock"));

            Components.Add(BinLifterZ);
            Components.Add(GoodBin8CassetteCheck0);
            Components.Add(GoodBin8CassetteCheck1);
            Components.Add(GoodBin12CassetteCheck0);
            Components.Add(GoodBin12CassetteCheck1);
            Components.Add(NgBin8CassetteCheck0);
            Components.Add(NgBin8CassetteCheck1);
            Components.Add(NgBin12CassetteCheck0);
            Components.Add(NgBin12CassetteCheck1);
            Components.Add(NgBinCassetteBw);
            Components.Add(NgBinCassetteLock);
            Components.Add(BinRingJutCheck);
            Components.Add(BinMappingSensor);
            Components.Add(NgBinCassetteLockOut);
            Components.Add(NgBinCassetteUnlockOut);

            BeginMapping();
        }

        public async Task MoveBinLifterZ(double targetPos, bool bFine = false)
        {
            double velocity = bFine ? BinLifterZ.Config.JogFineVelocity : BinLifterZ.Config.DefaultVelocity;
            if (velocity <= 0.0)
                velocity = bFine ? Math.Max(1.0, Config.ScanVelocity * 0.5) : Config.ScanVelocity;

            await MoveWithProtrusionWatch(targetPos, velocity);
        }

        public Task MoveBinLifterZToTeachingPosition(string positionName, bool bFine = false)
        {
            return MoveBinLifterZ(GetTeachingPosition(positionName), bFine);
        }

        public Task MoveToCassetteAvoidPosition(bool bFine = false) { return MoveToBinCassetteAvoidPosition(bFine); }
        public Task MoveToBinCassetteAvoidPosition(bool bFine = false) { return MoveBinLifterZ(Recipe.AvoidPosition, bFine); }

        public Task MoveToCassetteSlotPosition(int slotIndex, bool bFine = false)
        {
            return MoveToBinCassetteSlotPosition(ResolveActiveCassette(), slotIndex, bFine);
        }

        public Task MoveToBinCassetteSlotPosition(TargetCassette cassette, int slotIndex, bool bFine = false)
        {
            return MoveBinLifterZ(CalculateBinCassetteSlotTargetPosition(cassette, slotIndex), bFine);
        }

        public Task MoveToCassetteMappingStartPosition(bool bFine = false) { return MoveToBinCassetteMappingStartPosition(bFine); }
        public Task MoveToBinCassetteMappingStartPosition(bool bFine = false) { return MoveBinLifterZ(Recipe.MappingStartPosition, bFine); }

        public Task MoveToCassetteMappingEndPosition(bool bFine = false) { return MoveToBinCassetteMappingEndPosition(bFine); }
        public Task MoveToBinCassetteMappingEndPosition(bool bFine = false) { return MoveBinLifterZ(Recipe.MappingEndPosition, bFine); }

        public bool IsBinLifterZInPosition(double targetPos)
        {
            return IsBinLifterZInPosition(targetPos, BinLifterZ.Config.InPositionTolerance);
        }

        public bool IsBinLifterZInPosition(double targetPos, double tolerance)
        {
            return Math.Abs(BinLifterZ.ActualPosition - targetPos) <= tolerance;
        }

        public async Task<bool> WaitBinLifterZMoveDone(int timeoutMs)
        {
            int timeout = timeoutMs > 0 ? timeoutMs : BinLifterZ.Setup.MoveTimeoutMs;
            return await WaitUntilAsync(() => !BinLifterZ.IsMoving && BinLifterZ.IsInPosition && !BinLifterZ.IsAlarm, timeout);
        }

        public async Task<bool> WaitBinLifterZInPosition(string positionName, int timeoutMs)
        {
            double target = GetTeachingPosition(positionName);
            int timeout = timeoutMs > 0 ? timeoutMs : BinLifterZ.Setup.MoveTimeoutMs;
            return await WaitUntilAsync(() => IsBinLifterZInPosition(target), timeout);
        }

        public bool IsBinLifterZInAvoidPosition()
        {
            return IsBinLifterZInPosition(Recipe.AvoidPosition);
        }

        public bool IsBinLifterZInSlotPosition(int slotIndex)
        {
            return IsBinLifterZInSlotPosition(ResolveActiveCassette(), slotIndex);
        }

        public bool IsBinLifterZInSlotPosition(TargetCassette cassette, int slotIndex)
        {
            return IsBinLifterZInPosition(CalculateBinCassetteSlotTargetPosition(cassette, slotIndex));
        }

        public void TeachBinLifterZPosition(string positionName)
        {
            SetTeachingPosition(positionName, BinLifterZ.ActualPosition);
        }

        public void TeachBinLifterZAvoidPosition() { Recipe.AvoidPosition = BinLifterZ.ActualPosition; }
        public void TeachBinLifterZMappingStartPosition() { Recipe.MappingStartPosition = BinLifterZ.ActualPosition; }
        public void TeachBinLifterZMappingEndPosition() { Recipe.MappingEndPosition = BinLifterZ.ActualPosition; }

        public void TeachBinLifterZSlotBasePosition()
        {
            TeachBinLifterZFirstSlotPosition(ResolveActiveCassette());
        }

        public void TeachBinLifterZFirstSlotPosition(TargetCassette cassette)
        {
            SetFirstSlotPosition(cassette, BinLifterZ.ActualPosition);
        }

        public double CalculateCassetteSlotTargetPosition(int slotIndex)
        {
            return CalculateBinCassetteSlotTargetPosition(ResolveActiveCassette(), slotIndex);
        }

        public double CalculateBinCassetteSlotTargetPosition(TargetCassette cassette, int slotIndex)
        {
            ValidateSlotIndex(slotIndex);

            double mapped = GetMappedSlotPosition(cassette, slotIndex);
            if (!double.IsNaN(mapped))
                return mapped;

            return GetFirstSlotPosition(cassette) + (Config.SlotPitch * slotIndex);
        }

        public bool ValidateBinLifterZTeachingComplete()
        {
            return Config.SlotCount > 0 &&
                   Config.SlotPitch > 0.0 &&
                   Recipe.MappingEndPosition != Recipe.MappingStartPosition;
        }

        public async Task<bool> MoveToTeachingPositionAndVerify(string positionName, bool bFine = false)
        {
            await MoveBinLifterZToTeachingPosition(positionName, bFine);
            return await WaitBinLifterZInPosition(positionName, BinLifterZ.Setup.MoveTimeoutMs);
        }

        public void SetNgBinCassetteLock(bool on)
        {
            if (on)
                NgBinCassetteUnlockOut.Off();
            SetOutput(NgBinCassetteLockOut, on);
        }

        public void SetNgBinCassetteUnlock(bool on)
        {
            if (on)
                NgBinCassetteLockOut.Off();
            SetOutput(NgBinCassetteUnlockOut, on);
        }

        public async Task<bool> NGBinLockCylinder(bool nLock, int timeoutMs = 0)
        {
            int timeout = timeoutMs > 0 ? timeoutMs : BinLifterZ.Setup.MoveTimeoutMs;
            if (nLock)
            {
                SetNgBinCassetteUnlock(false);
                SetNgBinCassetteLock(true);
                return await WaitNgBinLock(timeout);
            }

            SetNgBinCassetteLock(false);
            SetNgBinCassetteUnlock(true);
            return await WaitUntilAsync(() => !IsNgBinLock(), timeout);
        }

        public bool IsGoodBin(int nSize)
        {
            if (nSize == 8) return GoodBin8CassetteCheck0.IsOn || GoodBin8CassetteCheck1.IsOn;
            if (nSize == 12) return GoodBin12CassetteCheck0.IsOn || GoodBin12CassetteCheck1.IsOn;
            return IsAnyCassetteSensorOn(TargetCassette.Good1);
        }

        public bool IsNgBin(int nSize)
        {
            if (nSize == 8) return NgBin8CassetteCheck0.IsOn || NgBin8CassetteCheck1.IsOn;
            if (nSize == 12) return NgBin12CassetteCheck0.IsOn || NgBin12CassetteCheck1.IsOn;
            return IsAnyCassetteSensorOn(TargetCassette.Ng);
        }

        public bool IsBinCassetteExist(TargetCassette cassette, int nSize)
        {
            return cassette == TargetCassette.Ng ? IsNgBin(nSize) : IsGoodBin(nSize);
        }

        public bool IsBinCassettePresentAll(TargetCassette cassette, int recipeSize)
        {
            if (cassette == TargetCassette.Ng)
            {
                if (recipeSize == 8) return NgBin8CassetteCheck0.IsOn && NgBin8CassetteCheck1.IsOn;
                if (recipeSize == 12) return NgBin12CassetteCheck0.IsOn && NgBin12CassetteCheck1.IsOn;
            }
            else
            {
                if (recipeSize == 8) return GoodBin8CassetteCheck0.IsOn && GoodBin8CassetteCheck1.IsOn;
                if (recipeSize == 12) return GoodBin12CassetteCheck0.IsOn && GoodBin12CassetteCheck1.IsOn;
            }

            return IsAnyCassetteSensorOn(cassette);
        }

        public bool IsNgBinBW() { return NgBinCassetteBw.IsOn; }
        public bool IsNgBinLock() { return NgBinCassetteLock.IsOn; }
        public bool IsBinProtrusionDetectionSensor() { return IsBinProtrusionDetected(); }
        public bool IsBinProtrusionDetected() { return BinRingJutCheck.IsOn; }
        public bool IsBinMapping() { return BinMappingSensor.IsOn; }

        public async Task<bool> WaitNgBinLock(int timeoutMs)
        {
            return await NgBinCassetteLock.WaitUntilStateAsync(true, timeoutMs);
        }

        public async Task<bool> WaitBinJutClear(int timeoutMs)
        {
            return await BinRingJutCheck.WaitUntilStateAsync(false, timeoutMs);
        }

        public async Task<bool> WaitBinMappingSensor(bool expected, int timeoutMs)
        {
            return await BinMappingSensor.WaitUntilStateAsync(expected, timeoutMs);
        }

        public void ManualMoveBinLifterZJog(Direction dir, double speed)
        {
            int direction = dir == Direction.Plus ? 1 : -1;
            ManualMoveBinLifterZJog(direction, speed);
        }

        public void ManualMoveBinLifterZJog(int direction, double speed)
        {
            BinLifterZ.MoveJogContinuous(direction, JogSpeedType.Custom, speed);
        }

        public void ManualStopBinLifterZ()
        {
            BinLifterZ.StopJog();
        }

        public Task ManualMoveToCassetteAvoidPosition(bool bFine = false) { return ManualMoveToBinCassetteAvoidPosition(bFine); }
        public Task ManualMoveToBinCassetteAvoidPosition(bool bFine = false) { return MoveToBinCassetteAvoidPosition(bFine); }

        public Task ManualMoveToCassetteSlotPosition(int slotIndex, bool bFine = false)
        {
            return ManualMoveToBinCassetteSlotPosition(ResolveActiveCassette(), slotIndex, bFine);
        }

        public Task ManualMoveToBinCassetteSlotPosition(TargetCassette cassette, int slotIndex, bool bFine = false)
        {
            return MoveToBinCassetteSlotPosition(cassette, slotIndex, bFine);
        }

        public Task ManualMoveToCassetteMappingStartPosition(bool bFine = false) { return ManualMoveToBinCassetteMappingStartPosition(bFine); }
        public Task ManualMoveToBinCassetteMappingStartPosition(bool bFine = false) { return MoveToBinCassetteMappingStartPosition(bFine); }
        public Task ManualMoveToCassetteMappingEndPosition(bool bFine = false) { return ManualMoveToBinCassetteMappingEndPosition(bFine); }
        public Task ManualMoveToBinCassetteMappingEndPosition(bool bFine = false) { return MoveToBinCassetteMappingEndPosition(bFine); }

        public Task<bool> ManualNgBinLockCylinder(bool nLock)
        {
            return NGBinLockCylinder(nLock);
        }

        public Task<bool> BinScan(int timeoutMs = 0, bool bFine = false)
        {
            return ScanAllCassettesAsync();
        }

        public async Task<bool> MoveToNextSlot(bool bFine = false)
        {
            int slot = FindNextProcessBinSlot();
            if (slot < 0)
                return false;

            await MoveToCassetteSlotPosition(slot, bFine);
            return await WaitBinLifterZMoveDone(BinLifterZ.Setup.MoveTimeoutMs);
        }

        public async Task<bool> ScanCassetteAsync(TargetCassette cassette, int maxSlots, double slotPitch)
        {
            if (!IsAnyCassetteSensorOn(cassette))
            {
                Console.WriteLine("[ALARM] '" + Name + "' ScanCassette: cassette not detected. cassette=" + cassette);
                _slotMap[cassette] = new bool[0];
                return false;
            }

            if (maxSlots <= 0 || slotPitch <= 0.0)
                return false;

            Recipe.EnsureSlotPositionBuffers(maxSlots);
            bool[] map = new bool[maxSlots];
            double oldAcc = BinLifterZ.Config.Acceleration;
            double oldDec = BinLifterZ.Config.Deceleration;

            try
            {
                if (Config.ScanAcc > 0.0)
                    BinLifterZ.Config.Acceleration = Config.ScanAcc;
                if (Config.ScanDec > 0.0)
                    BinLifterZ.Config.Deceleration = Config.ScanDec;

                for (int i = 0; i < maxSlots; i++)
                {
                    double position = GetFirstSlotPosition(cassette) + (i * slotPitch);
                    await MoveBinLifterZ(position, true);
                    if (BinLifterZ.IsAlarm)
                    {
                        Console.WriteLine("[ALARM] '" + Name + "' ScanCassette: BinLifterZ move failed at slot " + i + ".");
                        return false;
                    }

                    await Task.Delay(Config.ScanSettleTimeMs).ContinueWith(_ => { });
                    map[i] = Config.bDryRun || BinLifterZ.Config.IsSimulationMode ? true : BinMappingSensor.IsOn;
                    Recipe.UpdateSlotPosition(cassette, i, BinLifterZ.ActualPosition);
                }
            }
            finally
            {
                BinLifterZ.Config.Acceleration = oldAcc;
                BinLifterZ.Config.Deceleration = oldDec;
            }

            _slotMap[cassette] = map;
            for (int i = 0; i < map.Length; i++)
                UpdateCassetteSlotState(cassette, i, map[i] ? SlotPresence.Exist : SlotPresence.Empty, ProcessState.Ready);

            return true;
        }

        public async Task<bool> ScanAllCassettesAsync()
        {
            BeginMapping();
            bool ok = true;
            ok &= await ScanCassetteAsync(TargetCassette.Ng, Config.SlotCount, Config.SlotPitch);
            ok &= await ScanCassetteAsync(TargetCassette.Good1, Config.SlotCount, Config.SlotPitch);
            ok &= await ScanCassetteAsync(TargetCassette.Good2, Config.SlotCount, Config.SlotPitch);
            EndMapping();
            return ok;
        }

        public Task<bool> PrepareCassetteForFeederLoad(int slotIndex, int timeoutMs, bool bFine = false)
        {
            return PrepareBinCassetteForFeederLoad(ResolveActiveCassette(), slotIndex, timeoutMs, bFine);
        }

        public async Task<bool> PrepareBinCassetteForFeederLoad(TargetCassette cassette, int slotIndex, int timeoutMs, bool bFine = false)
        {
            if (!CheckBinLifterZMoveReady())
                return false;

            await MoveToBinCassetteSlotPosition(cassette, slotIndex, bFine);
            return await WaitBinLifterZMoveDone(timeoutMs);
        }

        public Task<bool> RecoverCassetteToSafeState(int timeoutMs, bool moveAvoid = true)
        {
            return RecoverBinCassetteToSafeState(timeoutMs, moveAvoid);
        }

        public async Task<bool> RecoverBinCassetteToSafeState(int timeoutMs, bool moveAvoid = true)
        {
            if (!await WaitBinJutClear(timeoutMs))
                return false;

            if (moveAvoid)
            {
                await MoveToBinCassetteAvoidPosition();
                return await WaitBinLifterZMoveDone(timeoutMs);
            }

            return true;
        }

        public bool CheckBinLifterZMoveReady()
        {
            return BinLifterZ != null &&
                   BinLifterZ.IsServoOn &&
                   !BinLifterZ.IsAlarm &&
                   !BinLifterZ.IsMoving &&
                   !IsBinProtrusionDetected();
        }

        public bool CheckBinCassetteMoveReady() { return CheckBinLifterZMoveReady(); }

        public bool CheckCassetteTransferReady(TransferMode mode)
        {
            return CheckBinCassetteTransferReady(ResolveActiveCassette(), mode);
        }

        public bool CheckBinCassetteTransferReady(TargetCassette cassette, TransferMode mode)
        {
            if (!CheckBinLifterZMoveReady())
                return false;
            if (mode == TransferMode.Load || mode == TransferMode.Unload)
                return IsAnyCassetteSensorOn(cassette);
            return true;
        }

        public bool CheckCassetteMappingReady()
        {
            return CheckBinCassetteMappingReady(ResolveActiveCassette());
        }

        public bool CheckBinCassetteMappingReady(TargetCassette cassette)
        {
            return CheckBinLifterZMoveReady() &&
                   IsAnyCassetteSensorOn(cassette) &&
                   ValidateBinLifterZTeachingComplete();
        }

        public bool CheckCassetteDirectionReady()
        {
            return !IsNgBinBW();
        }

        public BinCassetteSensorState GetCassettePresenceState(int recipeSize)
        {
            return new BinCassetteSensorState
            {
                GoodBin8CassetteCheck0 = GoodBin8CassetteCheck0.IsOn,
                GoodBin8CassetteCheck1 = GoodBin8CassetteCheck1.IsOn,
                GoodBin12CassetteCheck0 = GoodBin12CassetteCheck0.IsOn,
                GoodBin12CassetteCheck1 = GoodBin12CassetteCheck1.IsOn,
                NgBin8CassetteCheck0 = NgBin8CassetteCheck0.IsOn,
                NgBin8CassetteCheck1 = NgBin8CassetteCheck1.IsOn,
                NgBin12CassetteCheck0 = NgBin12CassetteCheck0.IsOn,
                NgBin12CassetteCheck1 = NgBin12CassetteCheck1.IsOn,
                NgBinCassetteBw = NgBinCassetteBw.IsOn,
                NgBinCassetteLock = NgBinCassetteLock.IsOn,
                BinRingJutCheck = BinRingJutCheck.IsOn,
                BinMapping = BinMappingSensor.IsOn,
                IsGoodCassetteExist = IsGoodBin(recipeSize),
                IsNgCassetteExist = IsNgBin(recipeSize),
                IsSizeMatched = IsGoodBin(recipeSize) || IsNgBin(recipeSize)
            };
        }

        public BinCassetteMaterial GetMaterialCassette()
        {
            return GetMaterialCassette(ResolveActiveCassette());
        }

        public BinCassetteMaterial GetMaterialCassette(TargetCassette cassette)
        {
            var material = new BinCassetteMaterial(Config.SlotCount);
            Dictionary<int, WaferSlotState> states = GetSlotStates(cassette);
            for (int i = 0; i < Config.SlotCount; i++)
            {
                WaferSlotState state;
                if (!states.TryGetValue(i, out state))
                    state = new WaferSlotState { Presence = SlotPresence.Unknown, Process = ProcessState.Unknown };
                material.Slots.Add(state);
            }
            return material;
        }

        public bool IsHaveMoreProcessWafer() { return IsHaveMoreProcessBin(); }
        public bool IsHaveMoreProcessBin() { return FindNextProcessBinSlot() >= 0; }

        public int FindNextProcessBinSlot()
        {
            Dictionary<int, WaferSlotState> states = GetSlotStates(ResolveActiveCassette());
            for (int i = 0; i < Config.SlotCount; i++)
            {
                WaferSlotState state;
                if (states.TryGetValue(i, out state) &&
                    state.Presence == SlotPresence.Exist &&
                    (state.Process == ProcessState.Ready || state.Process == ProcessState.Unknown))
                    return i;
            }
            return -1;
        }

        public int FindFirstEmptySlot(TargetCassette cassette)
        {
            bool[] map;
            if (!_slotMap.TryGetValue(cassette, out map))
                return -1;

            for (int i = 0; i < map.Length; i++)
                if (!map[i]) return i;
            return -1;
        }

        public int FindFirstFullSlot(TargetCassette cassette)
        {
            bool[] map;
            if (!_slotMap.TryGetValue(cassette, out map))
                return -1;

            for (int i = 0; i < map.Length; i++)
                if (map[i]) return i;
            return -1;
        }

        public void UpdateCassetteSlotState(int slotIndex, SlotPresence presence, ProcessState state)
        {
            UpdateCassetteSlotState(ResolveActiveCassette(), slotIndex, presence, state);
        }

        public void UpdateCassetteSlotState(TargetCassette cassette, int slotIndex, SlotPresence presence, ProcessState state)
        {
            ValidateSlotIndex(slotIndex);
            GetSlotStates(cassette)[slotIndex] = new WaferSlotState { Presence = presence, Process = state };
            EnsureSlotMap(cassette);
            _slotMap[cassette][slotIndex] = presence == SlotPresence.Exist;
        }

        public void UpdateBinCassetteSlotState(TargetCassette cassette, int slotIndex, bool hasWafer)
        {
            UpdateCassetteSlotState(cassette, slotIndex, hasWafer ? SlotPresence.Exist : SlotPresence.Empty, ProcessState.Ready);
        }

        public void BeginMapping()
        {
            _slotMap[TargetCassette.Ng] = new bool[0];
            _slotMap[TargetCassette.Good1] = new bool[0];
            _slotMap[TargetCassette.Good2] = new bool[0];
            _slotStates[TargetCassette.Ng] = new Dictionary<int, WaferSlotState>();
            _slotStates[TargetCassette.Good1] = new Dictionary<int, WaferSlotState>();
            _slotStates[TargetCassette.Good2] = new Dictionary<int, WaferSlotState>();
            Recipe.ResizeSlotPositions(Config.SlotCount);
        }

        public void BeginBinMapping() { BeginMapping(); }
        public void EndMapping() { }
        public void EndBinMapping() { EndMapping(); }

        public void StopCassetteMotionAndOutputs(string reason)
        {
            StopBinCassetteMotion(reason);
            SetNgBinCassetteLock(false);
            SetNgBinCassetteUnlock(false);
        }

        public void StopBinCassetteMotion(string reason)
        {
            BinLifterZ.Stop();
            Console.WriteLine("[STOP] '" + Name + "' " + reason);
        }

        public string BuildCassetteAlarmMessage(CassetteAlarmCode code)
        {
            switch (code)
            {
                case CassetteAlarmCode.CassetteMissing: return "Bin cassette is missing.";
                case CassetteAlarmCode.SizeMismatch: return "Bin cassette size mismatch.";
                case CassetteAlarmCode.ProtrusionDetected: return "Bin protrusion detected.";
                case CassetteAlarmCode.MappingTimeout: return "Bin mapping timeout.";
                case CassetteAlarmCode.MoveTimeout: return "Bin lifter Z move timeout.";
                case CassetteAlarmCode.TeachingMissing: return "Bin lifter Z teaching data is missing.";
                case CassetteAlarmCode.LockTimeout: return "NG bin cassette lock timeout.";
                default: return "No bin cassette alarm.";
            }
        }

        public Task MoveToTargetSlotAsync(double targetPosition)
        {
            return MoveBinLifterZ(targetPosition);
        }

        private async Task MoveWithProtrusionWatch(double targetPosition, double velocity)
        {
            if (IsBinProtrusionDetected())
            {
                BinLifterZ.EStop();
                throw new InvalidOperationException("'" + Name + "' Move: protrusion sensor is ON.");
            }

            using (var cts = new CancellationTokenSource())
            {
                Task moveTask = BinLifterZ.MoveAbsoluteAsync(targetPosition, velocity);
                Task<bool> watchTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        if (IsBinProtrusionDetected())
                            return true;
                        await Task.Delay(10, cts.Token).ContinueWith(_ => { });
                    }
                    return false;
                }, cts.Token);

                Task first = await Task.WhenAny(moveTask, watchTask);
                if (first == moveTask)
                {
                    cts.Cancel();
                    await watchTask.ContinueWith(_ => { });
                }
                else
                {
                    BinLifterZ.EStop();
                    cts.Cancel();
                    await moveTask.ContinueWith(_ => { });
                    throw new InvalidOperationException("'" + Name + "' Move: protrusion detected while moving.");
                }
            }

            if (BinLifterZ.IsAlarm)
                throw new InvalidOperationException("'" + Name + "' Move: BinLifterZ alarm.");
        }

        private double GetTeachingPosition(string positionName)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) return Recipe.AvoidPosition;
            if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase)) return Recipe.MappingStartPosition;
            if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase)) return Recipe.MappingEndPosition;
            if (string.Equals(positionName, "NgFirstSlot", StringComparison.OrdinalIgnoreCase)) return Recipe.NGFirstSlotPosition;
            if (string.Equals(positionName, "GoodFirstSlot", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodFirstSlotPosition;
            if (string.Equals(positionName, "Good1FirstSlot", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodFirstSlotPosition;
            if (string.Equals(positionName, "Good2FirstSlot", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodFirstSlotPosition + Config.GOODNGPositionOffset;
            throw new ArgumentException("Unknown BinLifterZ teaching position: " + positionName, "positionName");
        }

        private void SetTeachingPosition(string positionName, double position)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) Recipe.AvoidPosition = position;
            else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase)) Recipe.MappingStartPosition = position;
            else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase)) Recipe.MappingEndPosition = position;
            else if (string.Equals(positionName, "NgFirstSlot", StringComparison.OrdinalIgnoreCase)) Recipe.NGFirstSlotPosition = position;
            else if (string.Equals(positionName, "GoodFirstSlot", StringComparison.OrdinalIgnoreCase)) Recipe.GoodFirstSlotPosition = position;
            else if (string.Equals(positionName, "Good1FirstSlot", StringComparison.OrdinalIgnoreCase)) Recipe.GoodFirstSlotPosition = position;
            else if (string.Equals(positionName, "Good2FirstSlot", StringComparison.OrdinalIgnoreCase)) Recipe.GoodFirstSlotPosition = position - Config.GOODNGPositionOffset;
            else throw new ArgumentException("Unknown BinLifterZ teaching position: " + positionName, "positionName");
        }

        private double GetFirstSlotPosition(TargetCassette cassette)
        {
            switch (cassette)
            {
                case TargetCassette.Ng: return Recipe.NGFirstSlotPosition;
                case TargetCassette.Good1: return Recipe.GoodFirstSlotPosition;
                case TargetCassette.Good2: return Recipe.GoodFirstSlotPosition + Config.GOODNGPositionOffset;
                default: throw new ArgumentOutOfRangeException("cassette");
            }
        }

        private void SetFirstSlotPosition(TargetCassette cassette, double position)
        {
            switch (cassette)
            {
                case TargetCassette.Ng:
                    Recipe.NGFirstSlotPosition = position;
                    break;
                case TargetCassette.Good1:
                    Recipe.GoodFirstSlotPosition = position;
                    break;
                case TargetCassette.Good2:
                    Recipe.GoodFirstSlotPosition = position - Config.GOODNGPositionOffset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("cassette");
            }
        }

        private double GetMappedSlotPosition(TargetCassette cassette, int slotIndex)
        {
            Recipe.EnsureSlotPositionBuffers(Config.SlotCount);
            double[] slots = cassette == TargetCassette.Ng ? Recipe.NGSlotPosition : Recipe.GoodSlotPosition;
            if (slots != null && slotIndex >= 0 && slotIndex < slots.Length)
                return slots[slotIndex];
            return double.NaN;
        }

        private bool IsAnyCassetteSensorOn(TargetCassette cassette)
        {
            if (cassette == TargetCassette.Ng)
            {
                return NgBin8CassetteCheck0.IsOn || NgBin8CassetteCheck1.IsOn ||
                       NgBin12CassetteCheck0.IsOn || NgBin12CassetteCheck1.IsOn;
            }

            return GoodBin8CassetteCheck0.IsOn || GoodBin8CassetteCheck1.IsOn ||
                   GoodBin12CassetteCheck0.IsOn || GoodBin12CassetteCheck1.IsOn;
        }

        private TargetCassette ResolveActiveCassette()
        {
            if (IsAnyCassetteSensorOn(TargetCassette.Ng))
                return TargetCassette.Ng;
            if (IsAnyCassetteSensorOn(TargetCassette.Good1))
                return TargetCassette.Good1;
            return TargetCassette.Good1;
        }

        private Dictionary<int, WaferSlotState> GetSlotStates(TargetCassette cassette)
        {
            Dictionary<int, WaferSlotState> states;
            if (!_slotStates.TryGetValue(cassette, out states) || states == null)
            {
                states = new Dictionary<int, WaferSlotState>();
                _slotStates[cassette] = states;
            }
            return states;
        }

        private void EnsureSlotMap(TargetCassette cassette)
        {
            bool[] map;
            if (!_slotMap.TryGetValue(cassette, out map) || map == null || map.Length != Config.SlotCount)
                _slotMap[cassette] = new bool[Config.SlotCount];
        }

        private void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Config.SlotCount)
                throw new ArgumentOutOfRangeException("slotIndex", "Slot index is out of bin cassette range.");
        }

        private static void SetOutput(BaseDigitalOutput output, bool on)
        {
            if (on) output.On();
            else output.Off();
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
