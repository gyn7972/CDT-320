using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;

namespace QMC.CDT320
{
    [DataContract]
    public class InputCassetteSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            IsSimulationMode = false;
        }
    }

    [DataContract]
    public class InputCassetteConfig : IConfigData
    {
        [DataMember]public bool bDryRun { get; set; }
        [DataMember]public double LoadingPositionOffset { get; set; }
        [DataMember]public double UnloadingPositionOffset { get; set; }
        [DataMember]public double Level2PositionOffset { get; set; }
        [DataMember]public double SlotPitch { get; set; }
        [DataMember]public int SlotCount { get; set; }
        [DataMember]public double ScanVelocity { get; set; }
        [DataMember] public double ScanAcc { get; set; }
        [DataMember] public double ScanDec { get; set; }
        [DataMember]public int ScanSettleTimeMs { get; set; }
        [DataMember]public int InchSelect { get; set; } // 0: 8Inch, 1: 12Inch
        [DataMember] public int SelectedCassetteLevel { get; set; } // 1: 1단, 2: 2단 사용

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            bDryRun = false;
            LoadingPositionOffset = 0.00;
            UnloadingPositionOffset = 0.00;
            Level2PositionOffset = 59.00;
            SlotPitch = 5.00;
            SlotCount = 25;
            ScanVelocity = 20.0;
            ScanAcc = 0.0;
            ScanDec = 0.0;
            ScanSettleTimeMs = 100;
            InchSelect = 0;
            SelectedCassetteLevel = 1;
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
        [DataMember] public double[] SlotPosition { get; private set; }

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

    public class InputCassetteUnit : BaseUnit<InputCassetteSetup, InputCassetteConfig, InputCassetteRecipe>, IUnitJogController
    {
        private readonly Dictionary<int, WaferSlotState> slotStates = new Dictionary<int, WaferSlotState>();

        public BaseAxis InputLifterZ { get; private set; }

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
        public CDT320_Machine Machine { get; private set; }

        public InputCassetteUnit() : base("InputCassetteUnit")
        {
            InputLifterZ = AjinFactory.CreateAxis("InputLifterZ");
            Wafer8CassetteCheck0 = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.Wafer8CassetteCheck0);
            Wafer8CassetteCheck1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.Wafer8CassetteCheck1);
            Wafer12CassetteCheck0 = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.Wafer12CassetteCheck0);
            Wafer12CassetteCheck1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.Wafer12CassetteCheck1);
            WaferRingJutCheck = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferRingJUTCheck);
            WaferMappingSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferMapping);

            Components.Add(InputLifterZ);
            Components.Add(Wafer8CassetteCheck0);
            Components.Add(Wafer8CassetteCheck1);
            Components.Add(Wafer12CassetteCheck0);
            Components.Add(Wafer12CassetteCheck1);
            Components.Add(WaferRingJutCheck);
            Components.Add(WaferMappingSensor);

            WaferMap = new List<bool>().AsReadOnly();
            EnsureSlotPositionBuffer();
        }

        public void BindMachine(CDT320_Machine machine)
        {
            Machine = machine;
        }

        public bool CanHandleJogAxis(BaseAxis axis)
        {
            return axis != null && ReferenceEquals(axis, InputLifterZ);
        }

        public Task<int> JogStepAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed,
            double axisStepDistance)
        {
            if (!CanHandleJogAxis(axis))
                return Task.FromResult(-1);

            double signedDistance = (direction < 0 ? -1.0 : 1.0) * Math.Abs(axisStepDistance);
            double target = InputLifterZ.ActualPosition + signedDistance;
            return MoveWaferLifterZ(target, speedType, customSpeed);
        }

        public Task<int> JogContinuousAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed)
        {
            if (!CanHandleJogAxis(axis))
                return Task.FromResult(-1);

            return ManualMoveWaferLifterZJog(direction, speedType, customSpeed);
        }

        public Task<int> StopJogAsync(BaseAxis axis)
        {
            if (!CanHandleJogAxis(axis))
                return Task.FromResult(-1);

            return ManualStopWaferLifterZ();
        }

        public async Task<int> MoveWaferLifterZ(double targetPos, bool bFine = false)
        {
            try
            {
                if (!ValidateWaferLifterZTargetPosition(targetPos))
                    return -1;

                string interlockReason;
                if (!CheckWaferLifterZInterlock(targetPos, MotionGuardMoveKind.AxisMove, out interlockReason))
                    return -11;

                await MoveWithProtrusionWatch(targetPos, ResolveWaferLifterZMoveVelocity(bFine));
                return 0;
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
        public async Task<int> MoveWaferLifterZ(double targetPos, JogSpeedType speedType, double customSpeed = 0)
        {
            try
            {
                if (!ValidateWaferLifterZTargetPosition(targetPos))
                    return -1;

                string interlockReason;
                if (!CheckWaferLifterZInterlock(targetPos, MotionGuardMoveKind.AxisMove, out interlockReason))
                    return -11;

                double velocity = ResolveJogVelocity(speedType, customSpeed);
                await MoveWithProtrusionWatch(targetPos, velocity);
                return 0;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private bool CheckWaferLifterZInterlock(
            double targetPos,
            MotionGuardMoveKind moveKind,
            out string reason)
        {
            if (InputCassetteInterlockRules.CanMoveWaferLifterZ(Machine, targetPos, moveKind, out reason))
                return true;

            Log.Write("Main", "INTERLOCK", "InputCassette", reason + " - Blocked");
            AlarmManager.Raise(AlarmSeverity.Warning, "INTERLOCK", Name, reason);
            return false;
        }

        public async Task<int> MoveWaferLifterZToTeachingPosition(string positionName, bool bFine = false)
        {
            try
            {
                return await MoveWaferLifterZ(GetTeachingPosition(positionName), bFine);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> MoveToWaferCassetteAvoidPosition(bool bFine = false)
        {
            try
            {
                return await MoveWaferLifterZ(Recipe.AvoidPosition, bFine);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> MoveToWaferCassetteSlotPosition(int slotIndex, bool bFine = false)
        {
            try
            {
                return await MoveWaferLifterZ(CalculateWaferCassetteSlotTargetPosition(slotIndex), bFine);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> MoveToWaferCassetteMappingStartPosition(bool bFine = false)
        {
            try
            {
                return await MoveWaferLifterZ(Recipe.MappingStartPosition, bFine);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> MoveToWaferCassetteMappingEndPosition(bool bFine = false)
        {
            try
            {
                return await MoveWaferLifterZ(Recipe.MappingEndPosition, bFine);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public bool IsWaferLifterZInPosition(double targetPos, double tolerance)
        {
            return Math.Abs(InputLifterZ.ActualPosition - targetPos) <= tolerance;
        }

        public async Task<int> WaitWaferLifterZMoveDone(int timeoutMs)
        {
            try
            {
                return await WaitUntilAsync(() => !InputLifterZ.IsMoving && InputLifterZ.IsInPosition && !InputLifterZ.IsAlarm, timeoutMs);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> WaitWaferLifterZInPosition(string positionName, int timeoutMs)
        {
            try
            {
                double target = GetTeachingPosition(positionName);
                return await WaitUntilAsync(() => IsWaferLifterZInPosition(target, ResolveWaferLifterZInPositionTolerance()), timeoutMs);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public bool IsWaferLifterZInAvoidPosition()
        {
            return IsWaferLifterZInPosition(Recipe.AvoidPosition, ResolveWaferLifterZInPositionTolerance());
        }

        public bool IsWaferLifterZInSlotPosition(int slotIndex)
        {
            return IsWaferLifterZInPosition(CalculateWaferCassetteSlotTargetPosition(slotIndex), ResolveWaferLifterZInPositionTolerance());
        }

        public void TeachWaferLifterZPosition(string positionName)
        {
            SetTeachingPosition(positionName, InputLifterZ.ActualPosition);
        }

        public void TeachWaferLifterZAvoidPosition()
        {
            Recipe.AvoidPosition = InputLifterZ.ActualPosition;
        }

        public void TeachWaferLifterZMappingStartPosition()
        {
            Recipe.MappingStartPosition = InputLifterZ.ActualPosition;
        }

        public void TeachWaferLifterZMappingEndPosition()
        {
            Recipe.MappingEndPosition = InputLifterZ.ActualPosition;
        }

        public void TeachWaferLifterZSlotBasePosition()
        {
            Recipe.FirstSlotPosition = InputLifterZ.ActualPosition;
            EnsureSlotPositionBuffer();
            if (Recipe.SlotPosition != null && Recipe.SlotPosition.Length > 0)
                Recipe.UpdateSlotPosition(0, InputLifterZ.ActualPosition);
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

        public async Task<int> MoveToTeachingPositionAndVerify(string positionName, bool bFine = false)
        {
            try
            {
                int moveResult = await MoveWaferLifterZToTeachingPosition(positionName, bFine);
                if (moveResult != 0)
                    return moveResult;

                return await WaitWaferLifterZInPosition(positionName, ResolveWaferLifterZMoveTimeoutMs());
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public bool IsWaferCassetteExist(int nSize)
        {
            if (nSize == 8)
                return Wafer8CassetteCheck0.IsOn || Wafer8CassetteCheck1.IsOn;
            if (nSize == 12)
                return Wafer12CassetteCheck0.IsOn || Wafer12CassetteCheck1.IsOn;
            return IsAnyCassetteSensorOn();
        }

        public bool IsWaferCassette(int nSize)
        {
            return IsWaferCassetteExist(nSize);
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

        public bool IsWaferProtrusionDetectionSensor()
        {
            return IsWaferProtrusionDetected();
        }

        public bool IsWaferMapping()
        {
            return WaferMappingSensor.IsOn;
        }

        public async Task<int> WaitWaferJutClear(int timeoutMs)
        {
            try
            {
                return await WaferRingJutCheck.WaitUntilStateAsync(false, timeoutMs) ? 0 : -1;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> WaitWaferMappingSensor(bool expected, int timeoutMs)
        {
            try
            {
                return await WaferMappingSensor.WaitUntilStateAsync(expected, timeoutMs) ? 0 : -1;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
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

        public async Task<int> ManualMoveWaferLifterZJog(int direction, double speed)
        {
            try
            {
                string interlockReason;
                if (!CheckWaferLifterZInterlock(InputLifterZ.ActualPosition, MotionGuardMoveKind.AxisMove, out interlockReason))
                    return -11;

                InputLifterZ.MoveJogContinuous(direction, JogSpeedType.Custom, speed);
                await Task.CompletedTask;
                return 0;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> ManualMoveWaferLifterZJog(Direction dir, double speed)
        {
            return await ManualMoveWaferLifterZJog((int)dir, speed);
        }

        /// <summary>지정한 속도 모드로 Wafer Lifter Z축을 연속 조그 이동합니다.</summary>
        public async Task<int> ManualMoveWaferLifterZJog(int direction, JogSpeedType speedType, double customSpeed = 0)
        {
            try
            {
                string interlockReason;
                if (!CheckWaferLifterZInterlock(InputLifterZ.ActualPosition, MotionGuardMoveKind.AxisMove, out interlockReason))
                    return -11;

                InputLifterZ.MoveJogContinuous(direction, speedType, customSpeed);
                await Task.CompletedTask;
                return 0;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> ManualStopWaferLifterZ()
        {
            try
            {
                InputLifterZ.StopJog();
                await Task.CompletedTask;
                return 0;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> ManualMoveToWaferCassetteAvoidPosition(bool bFine = false)
        {
            try
            {
                return await MoveToWaferCassetteAvoidPosition(bFine);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> ManualMoveToWaferCassetteSlotPosition(int slotIndex, bool bFine = false)
        {
            try
            {
                return await MoveToWaferCassetteSlotPosition(slotIndex, bFine);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> ManualMoveToWaferCassetteMappingStartPosition(bool bFine = false)
        {
            try
            {
                return await MoveToWaferCassetteMappingStartPosition(bFine);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> ManualMoveToWaferCassetteMappingEndPosition(bool bFine = false)
        {
            try
            {
                return await MoveToWaferCassetteMappingEndPosition(bFine);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> WaferScan(int timeoutMs = 0, bool bFine = false)
        {
            try
            {
                if (!CheckWaferCassetteMappingReady())
                    return -1;

                BeginWaferMapping();
                int result = await ScanCassetteAsync(Config.SlotCount, Config.SlotPitch);
                EndWaferMapping();
                return result;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> WaferScanFromCurrentStart(int timeoutMs = 0, bool bFine = false)
        {
            try
            {
                if (!CheckWaferCassetteMappingReady())
                    return -1;

                BeginWaferMapping();
                int result = await ScanCassetteFromCurrentStartAsync(Config.SlotCount, Config.SlotPitch, timeoutMs);
                EndWaferMapping();
                return result;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> MoveToNextWaferSlot(bool bFine = false)
        {
            try
            {
                int slot = FindNextProcessWaferSlot();
                if (slot < 0)
                    return -1;

                int result = await MoveToWaferCassetteSlotPosition(slot, bFine);
                if (result != 0)
                    return result;

                return !InputLifterZ.IsAlarm ? 0 : -1;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> PrepareWaferCassetteForFeederLoad(int slotIndex, int timeoutMs, bool bFine = false)
        {
            try
            {
                if (!CheckWaferCassetteMoveReady())
                    return -1;

                int result = await MoveToWaferCassetteSlotPosition(slotIndex, bFine);
                if (result != 0)
                    return result;

                return await WaitWaferLifterZMoveDone(timeoutMs);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> RecoverWaferCassetteToSafeState(int timeoutMs, bool moveAvoid = true)
        {
            try
            {
                if (await WaitWaferJutClear(timeoutMs) != 0)
                    return -1;

                if (moveAvoid)
                {
                    int moveResult = await MoveToWaferCassetteAvoidPosition();
                    if (moveResult != 0)
                        return moveResult;

                    return await WaitWaferLifterZMoveDone(timeoutMs);
                }

                return 0;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public bool CheckWaferCassetteMoveReady()
        {
            return InputLifterZ != null &&
                   InputLifterZ.IsServoOn &&
                   !InputLifterZ.IsAlarm &&
                   !InputLifterZ.IsMoving &&
                   !IsWaferProtrusionDetected();
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

        public bool IsInputCassetteProcessComplete()
        {
            try
            {
                int slotCount = Config != null && Config.SlotCount > 0 ? Config.SlotCount : 0;
                if (slotCount <= 0)
                    return false;

                bool hasProcessWafer = false;
                var cassette = MaterialStateService.State != null && MaterialStateService.State.Cassettes != null
                    ? MaterialStateService.State.Cassettes.FirstOrDefault(c => c.Role == CassetteMaterialRole.Input1)
                    : null;

                if (cassette != null && cassette.IsMapped)
                {
                    cassette.EnsureSlots();
                    int count = Math.Min(slotCount, cassette.Slots.Count);
                    for (int i = 0; i < count; i++)
                    {
                        WaferSlotState slotState;
                        bool hasSlotState = slotStates.TryGetValue(i, out slotState);
                        var slot = cassette.Slots[i];
                        bool hasMaterialSlot = slot != null && slot.HasWafer && !string.IsNullOrWhiteSpace(slot.WaferId);

                        if (!hasMaterialSlot &&
                            (!hasSlotState || slotState.Presence != SlotPresence.Exist))
                        {
                            continue;
                        }

                        hasProcessWafer = true;

                        if (hasSlotState &&
                            slotState.Process != ProcessState.Done &&
                            slotState.Process != ProcessState.Ng)
                        {
                            return false;
                        }

                        if (hasMaterialSlot)
                        {
                            WaferMaterial wafer = MaterialStateService.GetWaferInCassette(CassetteMaterialRole.Input1, i);
                            if (wafer == null)
                                return false;

                            if (WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Finish)
                                return false;
                        }
                    }

                    return hasProcessWafer;
                }

                for (int i = 0; i < slotCount; i++)
                {
                    WaferSlotState state;
                    if (!slotStates.TryGetValue(i, out state) || state.Presence != SlotPresence.Exist)
                        continue;

                    hasProcessWafer = true;
                    if (state.Process != ProcessState.Done && state.Process != ProcessState.Ng)
                        return false;
                }

                return hasProcessWafer;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "InputCassetteUnit",
                    "Input cassette complete check failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public void RaiseInputCassetteCompleteAlarm(string source)
        {
            const string code = "IN-CST-CHANGE";
            const string message = "Input cassette processing is complete. Replace input cassette.";
            AlarmManager.Raise(AlarmSeverity.Warning, code, string.IsNullOrWhiteSpace(source) ? Name : source, message);
            Log.Write("Main", "ALARM", code, message + " - Check");
        }

        public bool IsHaveMoreProcessWafer()
        {
            return HasMoreProcessWafer();
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

            return FindNextProcessWaferSlotFromMaterialState();
        }

        private int FindNextProcessWaferSlotFromMaterialState()
        {
            try
            {
                int slotCount = Config != null && Config.SlotCount > 0 ? Config.SlotCount : 0;
                if (slotCount <= 0)
                    return -1;

                var cassette = MaterialStateService.State != null && MaterialStateService.State.Cassettes != null
                    ? MaterialStateService.State.Cassettes.FirstOrDefault(c => c.Role == CassetteMaterialRole.Input1)
                    : null;
                if (cassette == null || !cassette.IsMapped)
                    return -1;

                cassette.EnsureSlots();
                int count = Math.Min(slotCount, cassette.Slots.Count);
                for (int i = 0; i < count; i++)
                {
                    WaferSlotState slotState;
                    if (slotStates.TryGetValue(i, out slotState) &&
                        slotState.Process != ProcessState.Ready &&
                        slotState.Process != ProcessState.Unknown)
                    {
                        continue;
                    }

                    var slot = cassette.Slots[i];
                    if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                        continue;

                    WaferMaterial wafer = MaterialStateService.GetWaferInCassette(CassetteMaterialRole.Input1, i);
                    if (wafer == null)
                        continue;

                    WaferMaterialState state = WaferMaterialStateText.Normalize(wafer.State);
                    if (state == WaferMaterialState.Ready || state == WaferMaterialState.WorkReady)
                    {
                        UpdateWaferCassetteSlotState(i, SlotPresence.Exist, ProcessState.Ready);
                        return i;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "InputCassetteUnit",
                    "Find next process wafer slot from material state failed: " + ex.Message + " - Failed");
            }
            finally
            {
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

        public void BuildSimulatedWaferMap()
        {
            BeginWaferMapping();

            int count = Config != null && Config.SlotCount > 0 ? Config.SlotCount : 25;
            var map = new List<bool>(count);
            for (int i = 0; i < count; i++)
            {
                map.Add(true);
                UpdateSlotPosition(i, CalculateNominalSlotPosition(i));
            }

            WaferMap = map.AsReadOnly();
            EndWaferMapping();
        }

        public async Task<int> StopWaferCassetteMotion(string reason)
        {
            try
            {
                InputLifterZ.Stop();
                Console.WriteLine("[STOP] '" + Name + "' " + reason);
                await Task.CompletedTask;
                return 0;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
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

        public async Task<int> ScanCassetteAsync(int maxSlots, double slotPitch)
        {
            try
            {
                if (!IsAnyCassetteSensorOn())
                {
                    return FailMappingScan("IN-CST-MAP-CST-MISSING", "Cassette is not detected.");
                }

                if (maxSlots <= 0)
                {
                    return FailMappingScan("IN-CST-MAP-SLOT-COUNT", "Slot count is invalid.");
                }

                if (slotPitch <= 0.0)
                {
                    return FailMappingScan("IN-CST-MAP-PITCH", "Slot pitch is invalid.");
                }

                int startResult = await MoveToWaferCassetteMappingStartAndVerifyAsync();
                if (startResult != 0)
                    return startResult;

                return await ScanCassetteFromCurrentStartAsync(maxSlots, slotPitch);
            }
            catch (Exception ex)
            {
                FailMappingScan("IN-CST-MAP-EXCEPTION", "Mapping scan failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        public async Task<int> ScanCassetteFromCurrentStartAsync(int maxSlots, double slotPitch, int timeoutMs = 0)
        {
            try
            {
                if (!IsAnyCassetteSensorOn())
                {
                    return FailMappingScan("IN-CST-MAP-CST-MISSING", "Cassette is not detected.");
                }

                if (maxSlots <= 0)
                {
                    return FailMappingScan("IN-CST-MAP-SLOT-COUNT", "Slot count is invalid.");
                }

                if (slotPitch <= 0.0)
                {
                    return FailMappingScan("IN-CST-MAP-PITCH", "Slot pitch is invalid.");
                }

                if (Config.bDryRun || InputLifterZ.Config.IsSimulationMode)
                    return await ScanCassetteByNominalPositionFromCurrentStartAsync(maxSlots, timeoutMs);

                var detectedPositions = await CollectMappingSensorPositionsAsync();
                if (detectedPositions == null)
                    return -1;

                bool[] slotMap;
                double[] slotPositions;
                if (!BuildMappingResultFromDetectedPositions(detectedPositions, maxSlots, slotPitch, out slotMap, out slotPositions))
                    return -1;

                var map = new List<bool>(maxSlots);
                for (int i = 0; i < maxSlots; i++)
                {
                    map.Add(slotMap[i]);
                    if (slotMap[i])
                        UpdateSlotPosition(i, slotPositions[i]);
                }

                if (map.Count(x => x) <= 0)
                    return FailMappingScan("IN-CST-MAP-NO-WAFER", "No wafer was detected during mapping scan.");

                WaferMap = map.AsReadOnly();
                Log.Write("Main", "SYSTEM", "InputCassetteMapping", "Mapping scan completed. detected=" + detectedPositions.Count + ", slots=" + map.Count(x => x) + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                FailMappingScan("IN-CST-MAP-EXCEPTION", "Mapping scan failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> ScanCassetteByNominalPositionFromCurrentStartAsync(int maxSlots, int timeoutMs)
        {
            try
            {
                var map = new List<bool>();
                for (int i = 0; i < maxSlots; i++)
                {
                    UpdateSlotPosition(i, CalculateNominalSlotPosition(i));
                    map.Add(true);
                }

                int endResult = await MoveToWaferCassetteMappingEndPosition();
                if (endResult != 0 || InputLifterZ.IsAlarm)
                    return FailMappingScan("IN-CST-MAP-SIM-END", "InputLifterZ simulation move failed at mapping end.");

                int waitResult = await WaitWaferLifterZMoveDone(timeoutMs);
                if (waitResult != 0)
                    return FailMappingScan("IN-CST-MAP-SIM-END-WAIT", "InputLifterZ simulation mapping end move timeout.");

                bool inPosition = IsWaferLifterZInPosition(Recipe.MappingEndPosition, ResolveWaferLifterZInPositionTolerance());
                if (!inPosition)
                    return FailMappingScan("IN-CST-MAP-SIM-END-CHECK", "InputLifterZ simulation mapping end final position check failed.");

                WaferMap = map.AsReadOnly();
                Log.Write("Main", "SYSTEM", "InputCassetteMapping", "Simulation mapping scan completed. slots=" + map.Count + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                FailMappingScan("IN-CST-MAP-SIM-EXCEPTION", "Simulation mapping scan failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveToWaferCassetteMappingStartAndVerifyAsync()
        {
            try
            {
                int startResult = await MoveToWaferCassetteMappingStartPosition();
                if (startResult != 0 || InputLifterZ.IsAlarm)
                    return FailMappingScan("IN-CST-MAP-START", "InputLifterZ move failed at mapping start.");

                int waitResult = await WaitWaferLifterZMoveDone(ResolveWaferLifterZMoveTimeoutMs());
                if (waitResult != 0)
                    return FailMappingScan("IN-CST-MAP-START-WAIT", "InputLifterZ mapping start move timeout.");

                bool inPosition = IsWaferLifterZInPosition(Recipe.MappingStartPosition, ResolveWaferLifterZInPositionTolerance());
                if (!inPosition)
                    return FailMappingScan("IN-CST-MAP-START-CHECK", "InputLifterZ mapping start final position check failed.");

                return 0;
            }
            catch (Exception ex)
            {
                return FailMappingScan("IN-CST-MAP-START-EXCEPTION", "InputLifterZ mapping start move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<List<double>> CollectMappingSensorPositionsAsync()
        {
            double originalAcc = 0.0;
            double originalDec = 0.0;
            bool restoreScanProfile = false;
            try
            {
                var detectedPositions = new List<double>();
                bool previous = WaferMappingSensor.IsOn;
                if (previous)
                    return FailMappingScanList("IN-CST-MAP-SENSOR-ON", "Mapping sensor is ON at mapping start. Check mapping start position.");

                double scanVelocity = Config.ScanVelocity > 0.0 ? Config.ScanVelocity : InputLifterZ.Config.DefaultVelocity;
                if (scanVelocity <= 0.0)
                    scanVelocity = 1.0;

                if (InputLifterZ.Config != null && (Config.ScanAcc > 0.0 || Config.ScanDec > 0.0))
                {
                    originalAcc = InputLifterZ.Config.Acceleration;
                    originalDec = InputLifterZ.Config.Deceleration;
                    if (Config.ScanAcc > 0.0)
                        InputLifterZ.Config.Acceleration = Config.ScanAcc;
                    if (Config.ScanDec > 0.0)
                        InputLifterZ.Config.Deceleration = Config.ScanDec;
                    restoreScanProfile = true;
                }

                string interlockReason;
                if (!CheckWaferLifterZInterlock(Recipe.MappingEndPosition, MotionGuardMoveKind.AxisMove, out interlockReason))
                    return FailMappingScanList("IN-CST-MAP-INTERLOCK", interlockReason);

                Task<int> moveTask = InputLifterZ.MoveAbsoluteAsync(Recipe.MappingEndPosition, scanVelocity);
                while (!moveTask.IsCompleted)
                {
                    if (IsWaferProtrusionDetected())
                    {
                        InputLifterZ.EStop();
                        return FailMappingScanList("IN-CST-MAP-PROTRUSION", "Wafer protrusion detected during mapping scan.");
                    }

                    bool current = WaferMappingSensor.IsOn;
                    if (current && !previous)
                        AddDetectedMappingPosition(detectedPositions, InputLifterZ.ActualPosition);

                    previous = current;
                    await Task.Delay(5).ContinueWith(_ => { });
                }

                int moveResult = await moveTask;
                if (moveResult != 0 || InputLifterZ.IsAlarm)
                    return FailMappingScanList("IN-CST-MAP-END", "InputLifterZ move failed during mapping scan.");

                int waitResult = await WaitWaferLifterZMoveDone(ResolveWaferLifterZMoveTimeoutMs());
                if (waitResult != 0)
                    return FailMappingScanList("IN-CST-MAP-END-WAIT", "InputLifterZ mapping end move timeout.");

                bool inPosition = IsWaferLifterZInPosition(Recipe.MappingEndPosition, ResolveWaferLifterZInPositionTolerance());
                if (!inPosition)
                    return FailMappingScanList("IN-CST-MAP-END-CHECK", "InputLifterZ mapping end final position check failed.");

                return detectedPositions;
            }
            catch (Exception ex)
            {
                return FailMappingScanList("IN-CST-MAP-COLLECT", "Mapping sensor position collect failed: " + ex.Message);
            }
            finally
            {
                if (restoreScanProfile && InputLifterZ != null && InputLifterZ.Config != null)
                {
                    InputLifterZ.Config.Acceleration = originalAcc;
                    InputLifterZ.Config.Deceleration = originalDec;
                }
            }
        }

        private void AddDetectedMappingPosition(List<double> detectedPositions, double position)
        {
            try
            {
                double minSpacing = Config.SlotPitch > 0.0 ? Config.SlotPitch * 0.5 : 0.0;
                if (detectedPositions.Count > 0 && minSpacing > 0.0)
                {
                    double last = detectedPositions[detectedPositions.Count - 1];
                    if (Math.Abs(position - last) < minSpacing)
                        return;
                }

                detectedPositions.Add(position);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private bool BuildMappingResultFromDetectedPositions(
            IReadOnlyList<double> detectedPositions,
            int maxSlots,
            double slotPitch,
            out bool[] slotMap,
            out double[] slotPositions)
        {
            slotMap = new bool[maxSlots];
            slotPositions = new double[maxSlots];
            for (int i = 0; i < slotPositions.Length; i++)
                slotPositions[i] = double.NaN;

            try
            {
                double firstSlotPosition = CalculateNominalSlotPosition(0);
                double tolerance = ResolveMappingPitchTolerance(slotPitch);
                int previousSlot = -1;
                double previousPosition = double.NaN;

                foreach (double position in detectedPositions)
                {
                    int slotIndex = (int)Math.Round((position - firstSlotPosition) / slotPitch);
                    if (slotIndex < 0 || slotIndex >= maxSlots)
                    {
                        FailMappingScan("IN-CST-MAP-SLOT-MATCH", "Detected wafer position is outside slot range. position=" + FormatPosition(position));
                        return false;
                    }

                    if (slotMap[slotIndex])
                    {
                        FailMappingScan("IN-CST-MAP-DUPLICATE", "Duplicate wafer detection matched to the same slot. slot=" + (slotIndex + 1));
                        return false;
                    }

                    if (previousSlot >= 0)
                    {
                        int slotGap = Math.Abs(slotIndex - previousSlot);
                        double actualGap = Math.Abs(position - previousPosition);
                        double expectedGap = slotPitch * slotGap;
                        double error = Math.Abs(actualGap - expectedGap);
                        if (slotGap <= 0 || error > tolerance)
                        {
                            FailMappingScan(
                                "IN-CST-MAP-PITCH-CHECK",
                                "Mapping pitch check failed. prevSlot=" + (previousSlot + 1) +
                                ", slot=" + (slotIndex + 1) +
                                ", actualGap=" + FormatPosition(actualGap) +
                                ", expectedGap=" + FormatPosition(expectedGap) +
                                ", error=" + FormatPosition(error));
                            return false;
                        }
                    }

                    slotMap[slotIndex] = true;
                    slotPositions[slotIndex] = position;
                    previousSlot = slotIndex;
                    previousPosition = position;
                }

                return true;
            }
            catch (Exception ex)
            {
                FailMappingScan("IN-CST-MAP-BUILD", "Mapping result build failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private double ResolveMappingPitchTolerance(double slotPitch)
        {
            try
            {
                double tolerance = ResolveWaferLifterZInPositionTolerance();
                if (tolerance <= 0.0)
                    tolerance = slotPitch * 0.1;
                return Math.Max(tolerance, 0.001);
            }
            catch
            {
                return 0.001;
            }
            finally
            {
            }
        }

        private int FailMappingScan(string alarmCode, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", "InputCassetteMapping", message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, Name, message);
            }
            catch
            {
            }
            finally
            {
            }

            return -1;
        }

        private List<double> FailMappingScanList(string alarmCode, string message)
        {
            try
            {
                FailMappingScan(alarmCode, message);
            }
            catch
            {
            }
            finally
            {
            }

            return null;
        }

        private static string FormatPosition(double value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        public double ResolveWaferLifterZMoveVelocity(bool bFine)
        {
            try
            {
                if (InputLifterZ == null || InputLifterZ.Config == null)
                    return 0.0;

                if (bFine && InputLifterZ.Config.JogFineVelocity > 0.0)
                    return InputLifterZ.Config.JogFineVelocity;

                return InputLifterZ.Config.DefaultVelocity;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        public int ResolveWaferLifterZMoveTimeoutMs()
        {
            try
            {
                if (InputLifterZ != null && InputLifterZ.Setup != null && InputLifterZ.Setup.MoveTimeoutMs > 0)
                    return InputLifterZ.Setup.MoveTimeoutMs;
            }
            catch
            {
            }
            finally
            {
            }

            return 60000;
        }

        public double ResolveWaferLifterZInPositionTolerance()
        {
            try
            {
                if (InputLifterZ != null && InputLifterZ.Config != null && InputLifterZ.Config.InPositionTolerance >= 0.0)
                    return InputLifterZ.Config.InPositionTolerance;
            }
            catch
            {
            }
            finally
            {
            }

            return 0.05;
        }

        private bool ValidateWaferLifterZTargetPosition(double targetPos)
        {
            try
            {
                if (InputLifterZ == null || InputLifterZ.Setup == null)
                    return false;
                if (!InputLifterZ.Setup.SoftLimitEnabled)
                    return true;

                return targetPos <= InputLifterZ.Setup.SoftLimitPlus &&
                       targetPos >= InputLifterZ.Setup.SoftLimitMinus;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public async Task<int> MoveToTargetSlotAsync(double targetPosition)
        {
            try
            {
                return await MoveWaferLifterZ(targetPosition);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<int> MoveWithProtrusionWatch(double targetPosition, double velocity)
        {
            try
            {
                if (IsWaferProtrusionDetected())
                {
                    InputLifterZ.EStop();
                    throw new InvalidOperationException("'" + Name + "' Move: protrusion sensor is ON.");
                }

                int moveResult;
                using (var cts = new CancellationTokenSource())
                {
                    Task<int> moveTask = InputLifterZ.MoveAbsoluteAsync(targetPosition, velocity);
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
                        moveResult = await moveTask;
                    }
                    else
                    {
                        InputLifterZ.EStop();
                        cts.Cancel();
                        await moveTask.ContinueWith(_ => { });
                        throw new InvalidOperationException("'" + Name + "' Move: protrusion detected while moving.");
                    }
                }

                if (moveResult != 0 || InputLifterZ.IsAlarm)
                    throw new InvalidOperationException("'" + Name + "' Move: InputLifterZ alarm.");

                return 0;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private double ResolveJogVelocity(JogSpeedType speedType, double customSpeed)
        {
            try
            {
                if (speedType == JogSpeedType.Coarse)
                    return InputLifterZ.Config.JogCoarseVelocity;
                if (speedType == JogSpeedType.Fine)
                    return InputLifterZ.Config.JogFineVelocity;
                if (customSpeed > 0)
                    return customSpeed;

                return InputLifterZ.Config.JogFineVelocity;
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
            throw new ArgumentException("Unknown InputLifterZ teaching position: " + positionName, "positionName");
        }

        private void SetTeachingPosition(string positionName, double position)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) Recipe.AvoidPosition = position;
            else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase)) Recipe.MappingStartPosition = position;
            else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase)) Recipe.MappingEndPosition = position;
            else if (string.Equals(positionName, "FirstSlot", StringComparison.OrdinalIgnoreCase)) Recipe.FirstSlotPosition = position;
            else throw new ArgumentException("Unknown InputLifterZ teaching position: " + positionName, "positionName");
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

        public double CalculateCassetteLevelSlotPosition(int level, int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            if (level < 2)
                return CalculateWaferCassetteSlotTargetPosition(slotIndex);

            double level1LastPosition = CalculateWaferCassetteSlotTargetPosition(Config.SlotCount - 1);
            double levelGap = Config.Level2PositionOffset;
            if (levelGap <= 0.0)
                levelGap = Config.SlotPitch > 0.0 ? Config.SlotPitch : 0.001;

            return level1LastPosition + levelGap + (Config.SlotPitch * slotIndex);
        }

        private static async Task<int> WaitUntilAsync(Func<bool> condition, int timeoutMs)
        {
            try
            {
                int elapsed = 0;
                while (timeoutMs <= 0 || elapsed < timeoutMs)
                {
                    if (condition())
                        return 0;

                    await Task.Delay(10).ContinueWith(_ => { });
                    elapsed += 10;
                }

                return condition() ? 0 : -1;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }
    }
}
