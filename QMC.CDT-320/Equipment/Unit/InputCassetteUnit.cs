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
    public class InputCassetteSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }
        [DataMember] public double InPositionTolerance { get; set; }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            IsSimulationMode = false;
            InPositionTolerance = 0.05;
        }
    }

    [DataContract]
    public class InputCassetteConfig : IConfigData
    {
        [DataMember]public bool bDryRun { get; set; }
        [DataMember]public double LoadingPositionOffset { get; set; }
        [DataMember]public double UnloadingPositionOffset { get; set; }
        [DataMember]public double SlotPitch { get; set; }
        [DataMember]public int SlotCount { get; set; }
        [DataMember]public double ScanVelocity { get; set; }
        [DataMember]public int ScanSettleTimeMs { get; set; }
        [DataMember]public int ElevatorMoveTimeoutMs { get; set; }
        [DataMember]public int InchSelect { get; set; } // 0: 8Inch, 1: 12Inch
        [DataMember] public int SelectedCassetteLevel { get; set; } // 0: 1단, 1: 2단

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            bDryRun = false;
            LoadingPositionOffset = 0.00;
            UnloadingPositionOffset = 0.00;
            SlotPitch = 5.00;
            SlotCount = 25;
            ScanVelocity = 20.0;
            ScanSettleTimeMs = 100;
            ElevatorMoveTimeoutMs = 10000;
            InchSelect = 0;
            SelectedCassetteLevel = 0;
        }
    }
    
    [DataContract]
    public class InputCassetteRecipe : IRecipeData
    {
        [DataMember] public double AvoidPosition { get; set; }  //ReadyPosition.
        [DataMember] public double LoaingPosition { get; set; }
        [DataMember] public double UnloadingPosition { get; set; }
        [DataMember] public double FirstSlotPosition { get; set; }
        [DataMember] public double MappingStartPosition { get; set; }
        [DataMember] public double MappingEndPosition { get; set; }

        /// <summary>Mapping ???뺤젙??Slot蹂?Z ?꾩튂?낅땲?? 媛믪씠 ?놁쑝硫?double.NaN?쇰줈 ?좎??⑸땲??</summary>
        public double[] SlotPosition { get; private set; }

        /// <summary>Config.SlotCount??留욎떠 SlotPosition 踰꾪띁瑜??ъ깮?깊빀?덈떎.</summary>
        public void ResizeSlotPositions(int slotCount)
        {
            int count = Math.Max(0, slotCount);
            SlotPosition = new double[count];
            for (int i = 0; i < SlotPosition.Length; i++)
                SlotPosition[i] = double.NaN;
        }
        /// <summary>SlotPosition 踰꾪띁媛 吏??SlotCount? 媛숈?吏 蹂댁옣?⑸땲??</summary>
        public void EnsureSlotPositionBuffer(int slotCount)
        {
            int count = Math.Max(0, slotCount);
            if (SlotPosition == null || SlotPosition.Length != count)
                ResizeSlotPositions(count);
        }
        /// <summary>吏??Slot??Mapping ?꾩튂瑜?媛깆떊?⑸땲??</summary>
        public void UpdateSlotPosition(int slotIndex, double position)
        {
            if (SlotPosition == null || slotIndex < 0 || slotIndex >= SlotPosition.Length)
                throw new ArgumentOutOfRangeException("slotIndex");

            SlotPosition[slotIndex] = position;
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            AvoidPosition = 0.0;
            LoaingPosition = 150.0;
            UnloadingPosition = 150.0;
            FirstSlotPosition = 10.0;
            MappingStartPosition = 5.0;
            MappingEndPosition = 130.0;
            SlotPosition = Array.Empty<double>();
        }
    }

    public class InputCassetteUnit : BaseUnit<InputCassetteSetup, InputCassetteConfig, InputCassetteRecipe>
    {
        private readonly Dictionary<int, WaferSlotState> slotStates = new Dictionary<int, WaferSlotState>();

        public BaseAxis WaferLifterZ { get; private set; }

        public BaseDigitalInput Wafer8CassetteCheck0 { get; private set; }
        public BaseDigitalInput Wafer8CassetteCheck1 { get; private set; }
        public BaseDigitalInput Wafer12CassetteCheck0 { get; private set; }
        public BaseDigitalInput Wafer12CassetteCheck1 { get; private set; }
        public BaseDigitalInput WaferRingJutCheck { get; private set; }
        public BaseDigitalInput WaferMappingSensor { get; private set; }

        public BaseDigitalInput CassetteExistSensor { get { return Wafer8CassetteCheck0; } }
        public BaseDigitalInput ProtrusionSensor { get { return WaferRingJutCheck; } }
        public BaseDigitalInput WaferDetectSensor { get { return WaferMappingSensor; } }

        public IReadOnlyList<bool> WaferMap { get; private set; }

        public InputCassetteUnit() : base("WaferCassetteUnit")
        {
            WaferLifterZ = AjinFactory.CreateAxis("WaferLifterZ");
            Wafer8CassetteCheck0 = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.Wafer8CassetteCheck0);
            Wafer8CassetteCheck1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.Wafer8CassetteCheck1);
            Wafer12CassetteCheck0 = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.Wafer12CassetteCheck0);
            Wafer12CassetteCheck1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.Wafer12CassetteCheck1);
            WaferRingJutCheck = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferRingJUTCheck);
            WaferMappingSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferMapping);

            Components.Add(WaferLifterZ);
            Components.Add(Wafer8CassetteCheck0);
            Components.Add(Wafer8CassetteCheck1);
            Components.Add(Wafer12CassetteCheck0);
            Components.Add(Wafer12CassetteCheck1);
            Components.Add(WaferRingJutCheck);
            Components.Add(WaferMappingSensor);

            WaferMap = new List<bool>().AsReadOnly();
            EnsureSlotPositionBuffer();
        }

        public async Task MoveWaferLifterZ(double targetPos, bool bFine = false)
        {
            try
            {
                await MoveWithProtrusionWatch(targetPos, bFine ? Config.ScanVelocity * 0.5 : Config.ScanVelocity);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        /// <summary>지정한 조그 속도 모드로 Wafer Lifter Z축을 절대 위치로 이동합니다.</summary>
        public async Task MoveWaferLifterZ(double targetPos, JogSpeedType speedType, double customSpeed = 0)
        {
            try
            {
                double velocity = ResolveJogVelocity(speedType, customSpeed);
                await MoveWithProtrusionWatch(targetPos, velocity);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public Task MoveWaferLifterZToTeachingPosition(string positionName, bool bFine = false)
        {
            return MoveWaferLifterZ(GetTeachingPosition(positionName), bFine);
        }

        public Task MoveToWaferCassetteAvoidPosition(bool bFine = false)
        {
            return MoveWaferLifterZ(Recipe.AvoidPosition, bFine);
        }

        public Task MoveToWaferCassetteSlotPosition(int slotIndex, bool bFine = false)
        {
            return MoveWaferLifterZ(CalculateWaferCassetteSlotTargetPosition(slotIndex), bFine);
        }

        public Task MoveToWaferCassetteMappingStartPosition(bool bFine = false)
        {
            return MoveWaferLifterZ(Recipe.MappingStartPosition, bFine);
        }

        public Task MoveToWaferCassetteMappingEndPosition(bool bFine = false)
        {
            return MoveWaferLifterZ(Recipe.MappingEndPosition, bFine);
        }

        public bool IsWaferLifterZInPosition(double targetPos, double tolerance)
        {
            return Math.Abs(WaferLifterZ.ActualPosition - targetPos) <= tolerance;
        }

        public async Task<bool> WaitWaferLifterZMoveDone(int timeoutMs)
        {
            return await WaitUntilAsync(() => !WaferLifterZ.IsMoving && WaferLifterZ.IsInPosition && !WaferLifterZ.IsAlarm, timeoutMs);
        }

        public async Task<bool> WaitWaferLifterZInPosition(string positionName, int timeoutMs)
        {
            double target = GetTeachingPosition(positionName);
            return await WaitUntilAsync(() => IsWaferLifterZInPosition(target, Setup.InPositionTolerance), timeoutMs);
        }

        public bool IsWaferLifterZInAvoidPosition()
        {
            return IsWaferLifterZInPosition(Recipe.AvoidPosition, Setup.InPositionTolerance);
        }

        public bool IsWaferLifterZInSlotPosition(int slotIndex)
        {
            return IsWaferLifterZInPosition(CalculateWaferCassetteSlotTargetPosition(slotIndex), Setup.InPositionTolerance);
        }

        public void TeachWaferLifterZPosition(string positionName)
        {
            SetTeachingPosition(positionName, WaferLifterZ.ActualPosition);
        }

        public void TeachWaferLifterZAvoidPosition()
        {
            Recipe.AvoidPosition = WaferLifterZ.ActualPosition;
        }

        public void TeachWaferLifterZMappingStartPosition()
        {
            Recipe.MappingStartPosition = WaferLifterZ.ActualPosition;
        }

        public void TeachWaferLifterZMappingEndPosition()
        {
            Recipe.MappingEndPosition = WaferLifterZ.ActualPosition;
        }

        public double CalculateWaferCassetteSlotTargetPosition(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            EnsureSlotPositionBuffer();
            double mappedPosition = Recipe.SlotPosition[slotIndex];
            if (!double.IsNaN(mappedPosition))
                return mappedPosition;

            return CalculateNominalSlotPosition(slotIndex);
        }

        public bool ValidateWaferLifterZTeachingComplete()
        {
            return Config.SlotCount > 0 && Config.SlotPitch > 0.0 && Recipe.MappingEndPosition != Recipe.MappingStartPosition;
        }

        public async Task<bool> MoveToTeachingPositionAndVerify(string positionName, bool bFine = false)
        {
            await MoveWaferLifterZToTeachingPosition(positionName, bFine);
            return await WaitWaferLifterZInPosition(positionName, Config.ElevatorMoveTimeoutMs);
        }

        public bool IsWaferCassetteExist(int nSize)
        {
            if (nSize == 8)
                return Wafer8CassetteCheck0.IsOn || Wafer8CassetteCheck1.IsOn;
            if (nSize == 12)
                return Wafer12CassetteCheck0.IsOn || Wafer12CassetteCheck1.IsOn;
            return IsAnyCassetteSensorOn();
        }

        public bool IsWaferCassettePresentAll(int recipeSize)
        {
            if (recipeSize == 8)
                return Wafer8CassetteCheck0.IsOn && Wafer8CassetteCheck1.IsOn;
            if (recipeSize == 12)
                return Wafer12CassetteCheck0.IsOn && Wafer12CassetteCheck1.IsOn;
            return IsAnyCassetteSensorOn();
        }

        public bool IsWaferProtrusionDetected()
        {
            return WaferRingJutCheck.IsOn;
        }

        public bool IsWaferMapping()
        {
            return WaferMappingSensor.IsOn;
        }

        public async Task<bool> WaitWaferJutClear(int timeoutMs)
        {
            return await WaferRingJutCheck.WaitUntilStateAsync(false, timeoutMs);
        }

        public async Task<bool> WaitWaferMappingSensor(bool expected, int timeoutMs)
        {
            return await WaferMappingSensor.WaitUntilStateAsync(expected, timeoutMs);
        }

        public WaferCassetteSensorState GetWaferCassetteSensorState(int recipeSize)
        {
            return new WaferCassetteSensorState
            {
                Wafer8CassetteCheck0 = Wafer8CassetteCheck0.IsOn,
                Wafer8CassetteCheck1 = Wafer8CassetteCheck1.IsOn,
                Wafer12CassetteCheck0 = Wafer12CassetteCheck0.IsOn,
                Wafer12CassetteCheck1 = Wafer12CassetteCheck1.IsOn,
                WaferRingJutCheck = WaferRingJutCheck.IsOn,
                WaferMapping = WaferMappingSensor.IsOn,
                IsCassetteExist = IsWaferCassetteExist(recipeSize),
                IsSizeMatched = IsWaferCassettePresentAll(recipeSize)
            };
        }

        public void ManualMoveWaferLifterZJog(int direction, double speed)
        {
            try
            {
                WaferLifterZ.MoveJogContinuous(direction, JogSpeedType.Custom, speed);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        /// <summary>지정한 속도 모드로 Wafer Lifter Z축을 연속 조그 이동합니다.</summary>
        public void ManualMoveWaferLifterZJog(int direction, JogSpeedType speedType, double customSpeed = 0)
        {
            try
            {
                WaferLifterZ.MoveJogContinuous(direction, speedType, customSpeed);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public void ManualStopWaferLifterZ()
        {
            WaferLifterZ.StopJog();
        }

        public Task ManualMoveToWaferCassetteAvoidPosition(bool bFine = false)
        {
            return MoveToWaferCassetteAvoidPosition(bFine);
        }

        public Task ManualMoveToWaferCassetteSlotPosition(int slotIndex, bool bFine = false)
        {
            return MoveToWaferCassetteSlotPosition(slotIndex, bFine);
        }

        public Task ManualMoveToWaferCassetteMappingStartPosition(bool bFine = false)
        {
            return MoveToWaferCassetteMappingStartPosition(bFine);
        }

        public Task ManualMoveToWaferCassetteMappingEndPosition(bool bFine = false)
        {
            return MoveToWaferCassetteMappingEndPosition(bFine);
        }

        public async Task<bool> WaferScan(int timeoutMs = 0, bool bFine = false)
        {
            if (!CheckWaferCassetteMappingReady())
                return false;

            BeginWaferMapping();
            bool result = await ScanCassetteAsync(Config.SlotCount, Config.SlotPitch);
            EndWaferMapping();
            return result;
        }

        public async Task<bool> MoveToNextWaferSlot(bool bFine = false)
        {
            int slot = FindNextProcessWaferSlot();
            if (slot < 0)
                return false;

            await MoveToWaferCassetteSlotPosition(slot, bFine);
            return !WaferLifterZ.IsAlarm;
        }

        public async Task<bool> PrepareWaferCassetteForFeederLoad(int slotIndex, int timeoutMs, bool bFine = false)
        {
            if (!CheckWaferCassetteMoveReady())
                return false;

            await MoveToWaferCassetteSlotPosition(slotIndex, bFine);
            return await WaitWaferLifterZMoveDone(timeoutMs);
        }

        public async Task<bool> RecoverWaferCassetteToSafeState(int timeoutMs, bool moveAvoid = true)
        {
            if (!await WaitWaferJutClear(timeoutMs))
                return false;

            if (moveAvoid)
            {
                await MoveToWaferCassetteAvoidPosition();
                return await WaitWaferLifterZMoveDone(timeoutMs);
            }

            return true;
        }

        public bool CheckWaferCassetteMoveReady()
        {
            return !WaferLifterZ.IsAlarm && !IsWaferProtrusionDetected();
        }

        public bool CheckWaferCassetteTransferReady(TransferMode mode)
        {
            if (!CheckWaferCassetteMoveReady())
                return false;
            if (mode == TransferMode.Load || mode == TransferMode.Unload)
                return IsAnyCassetteSensorOn();
            return true;
        }

        public bool CheckWaferCassetteMappingReady()
        {
            return CheckWaferCassetteMoveReady() && IsAnyCassetteSensorOn() && ValidateWaferLifterZTeachingComplete();
        }

        public WaferCassetteMaterial GetWaferMaterialCassette()
        {
            var material = new WaferCassetteMaterial(Config.SlotCount);
            for (int i = 0; i < Config.SlotCount; i++)
            {
                WaferSlotState state;
                if (!slotStates.TryGetValue(i, out state))
                    state = new WaferSlotState { Presence = SlotPresence.Unknown, Process = ProcessState.Unknown };
                material.Slots.Add(state);
            }
            return material;
        }

        public bool HasMoreProcessWafer()
        {
            return FindNextProcessWaferSlot() >= 0;
        }

        public int FindNextProcessWaferSlot()
        {
            for (int i = 0; i < Config.SlotCount; i++)
            {
                WaferSlotState state;
                if (slotStates.TryGetValue(i, out state) &&
                    state.Presence == SlotPresence.Exist &&
                    (state.Process == ProcessState.Ready || state.Process == ProcessState.Unknown))
                    return i;
            }
            return -1;
        }

        public void UpdateWaferCassetteSlotState(int slotIndex, SlotPresence presence, ProcessState state)
        {
            ValidateSlotIndex(slotIndex);
            slotStates[slotIndex] = new WaferSlotState { Presence = presence, Process = state };
        }

        public void BeginWaferMapping()
        {
            slotStates.Clear();
            WaferMap = new List<bool>().AsReadOnly();
            Recipe.ResizeSlotPositions(Config.SlotCount);
        }

        public void EndWaferMapping()
        {
            var map = new List<bool>(WaferMap);
            for (int i = 0; i < map.Count; i++)
                UpdateWaferCassetteSlotState(i, map[i] ? SlotPresence.Exist : SlotPresence.Empty, ProcessState.Ready);
        }

        public void StopWaferCassetteMotion(string reason)
        {
            WaferLifterZ.Stop();
            Console.WriteLine("[STOP] '" + Name + "' " + reason);
        }

        public string BuildWaferCassetteAlarmMessage(WaferCassetteAlarmCode code)
        {
            switch (code)
            {
                case WaferCassetteAlarmCode.CassetteMissing: return "Wafer cassette is missing.";
                case WaferCassetteAlarmCode.SizeMismatch: return "Wafer cassette size mismatch.";
                case WaferCassetteAlarmCode.ProtrusionDetected: return "Wafer protrusion detected.";
                case WaferCassetteAlarmCode.MappingTimeout: return "Wafer mapping timeout.";
                case WaferCassetteAlarmCode.MoveTimeout: return "Wafer lifter Z move timeout.";
                case WaferCassetteAlarmCode.TeachingMissing: return "Wafer lifter Z teaching data is missing.";
                default: return "No wafer cassette alarm.";
            }
        }

        public async Task<bool> ScanCassetteAsync(int maxSlots, double slotPitch)
        {
            if (!IsAnyCassetteSensorOn())
            {
                Console.WriteLine("[ALARM] '" + Name + "' ScanCassette: cassette not detected.");
                return false;
            }

            var map = new List<bool>();
            await MoveToWaferCassetteMappingStartPosition();
            if (WaferLifterZ.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' ScanCassette: WaferLifterZ move failed at mapping start.");
                return false;
            }

            for (int i = 0; i < maxSlots; i++)
            {
                await MoveWaferLifterZ(CalculateNominalSlotPosition(i));
                if (WaferLifterZ.IsAlarm)
                {
                    Console.WriteLine("[ALARM] '" + Name + "' ScanCassette: WaferLifterZ move failed at slot " + i + ".");
                    return false;
                }

                await Task.Delay(Config.ScanSettleTimeMs).ContinueWith(_ => { });
                UpdateSlotPosition(i, WaferLifterZ.ActualPosition);
                map.Add(WaferMappingSensor.IsOn);
            }

            WaferMap = map.AsReadOnly();
            return true;
        }

        public Task MoveToTargetSlotAsync(double targetPosition)
        {
            return MoveWaferLifterZ(targetPosition);
        }

        private async Task MoveWithProtrusionWatch(double targetPosition, double velocity)
        {
            if (IsWaferProtrusionDetected())
            {
                WaferLifterZ.EStop();
                throw new InvalidOperationException("'" + Name + "' Move: protrusion sensor is ON.");
            }

            using (var cts = new CancellationTokenSource())
            {
                Task moveTask = WaferLifterZ.MoveAbsoluteAsync(targetPosition, velocity);
                Task<bool> watchTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        if (IsWaferProtrusionDetected())
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
                    WaferLifterZ.EStop();
                    cts.Cancel();
                    await moveTask.ContinueWith(_ => { });
                    throw new InvalidOperationException("'" + Name + "' Move: protrusion detected while moving.");
                }
            }

            if (WaferLifterZ.IsAlarm)
                throw new InvalidOperationException("'" + Name + "' Move: WaferLifterZ alarm.");
        }

        private double ResolveJogVelocity(JogSpeedType speedType, double customSpeed)
        {
            try
            {
                if (speedType == JogSpeedType.Coarse)
                    return WaferLifterZ.Config.JogCoarseVelocity;
                if (speedType == JogSpeedType.Fine)
                    return WaferLifterZ.Config.JogFineVelocity;
                if (customSpeed > 0)
                    return customSpeed;

                return WaferLifterZ.Config.JogFineVelocity;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private double GetTeachingPosition(string positionName)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) return Recipe.AvoidPosition;
            if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase)) return Recipe.MappingStartPosition;
            if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase)) return Recipe.MappingEndPosition;
            if (string.Equals(positionName, "FirstSlot", StringComparison.OrdinalIgnoreCase)) return Recipe.FirstSlotPosition;
            throw new ArgumentException("Unknown WaferLifterZ teaching position: " + positionName, "positionName");
        }

        private void SetTeachingPosition(string positionName, double position)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) Recipe.AvoidPosition = position;
            else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase)) Recipe.MappingStartPosition = position;
            else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase)) Recipe.MappingEndPosition = position;
            else if (string.Equals(positionName, "FirstSlot", StringComparison.OrdinalIgnoreCase)) Recipe.FirstSlotPosition = position;
            else throw new ArgumentException("Unknown WaferLifterZ teaching position: " + positionName, "positionName");
        }

        private bool IsAnyCassetteSensorOn()
        {
            return Wafer8CassetteCheck0.IsOn || Wafer8CassetteCheck1.IsOn ||
                   Wafer12CassetteCheck0.IsOn || Wafer12CassetteCheck1.IsOn;
        }

        private void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Config.SlotCount)
                throw new ArgumentOutOfRangeException("slotIndex", "Slot index is out of cassette range.");
        }

        /// <summary>Config.SlotCount??留욎떠 Recipe.SlotPosition 踰꾪띁瑜?蹂댁옣?⑸땲??</summary>
        public void EnsureSlotPositionBuffer()
        {
            Recipe.EnsureSlotPositionBuffer(Config.SlotCount);
        }

        /// <summary>Mapping ??SlotPosition 踰꾪띁瑜?珥덇린?뷀빀?덈떎.</summary>
        public void ResetSlotPositionsForMapping()
        {
            Recipe.ResizeSlotPositions(Config.SlotCount);
        }

        /// <summary>吏??Slot??Mapping ?꾩튂瑜?媛깆떊?⑸땲??</summary>
        public void UpdateSlotPosition(int slotIndex, double position)
        {
            ValidateSlotIndex(slotIndex);
            EnsureSlotPositionBuffer();
            Recipe.UpdateSlotPosition(slotIndex, position);
        }

        /// <summary>Mapping 寃곌낵媛 ?놁쓣 ???ъ슜??紐낅ぉ Slot ?꾩튂瑜?怨꾩궛?⑸땲??</summary>
        public double CalculateNominalSlotPosition(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return Recipe.FirstSlotPosition + Config.LoadingPositionOffset + (Config.SlotPitch * slotIndex);
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
