using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    [DataContract]
    public class OutputCassetteSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }

        public OutputCassetteSetup()
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
    public class OutputCassetteConfig : IConfigData
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

        public OutputCassetteConfig()
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
    public class OutputCassetteRecipe : IRecipeData
    {
        [DataMember] public double AvoidPosition { get; set; }
        [DataMember] public double GoodLoaingPosition { get; set; }
        [DataMember] public double GoodUnloadingPosition { get; set; }
        [DataMember] public double GoodFirstSlotPosition { get; set; }
        [DataMember] public double[] GoodSlotPosition { get; private set; }
        [DataMember] public double[] Good2SlotPosition { get; private set; }
        [DataMember] public double NGLoaingPosition { get; set; }
        [DataMember] public double NGUnloadingPosition { get; set; }
        [DataMember] public double NGFirstSlotPosition { get; set; }
        [DataMember] public double[] NGSlotPosition { get; private set; }
        [DataMember] public double MappingStartPosition { get; set; }
        [DataMember] public double MappingEndPosition { get; set; }

        public OutputCassetteRecipe()
        {
            SetDefaults();
        }

        public void ResizeSlotPositions(int slotCount)
        {
            int count = Math.Max(0, slotCount);
            GoodSlotPosition = new double[count];
            Good2SlotPosition = new double[count];
            NGSlotPosition = new double[count];
            for (int i = 0; i < count; i++)
            {
                GoodSlotPosition[i] = double.NaN;
                Good2SlotPosition[i] = double.NaN;
                NGSlotPosition[i] = double.NaN;
            }
        }

        public void EnsureSlotPositionBuffers(int slotCount)
        {
            int count = Math.Max(0, slotCount);
            if (GoodSlotPosition == null || GoodSlotPosition.Length != count ||
                Good2SlotPosition == null || Good2SlotPosition.Length != count ||
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
            else if (cassette == TargetCassette.Good2)
                Good2SlotPosition[slotIndex] = position;
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
            Good2SlotPosition = Array.Empty<double>();
            NGSlotPosition = Array.Empty<double>();
        }
    }

    public class OutputCassetteUnit : BaseUnit<OutputCassetteSetup, OutputCassetteConfig, OutputCassetteRecipe>, IUnitJogController
    {
        private readonly Dictionary<TargetCassette, bool[]> _slotMap = new Dictionary<TargetCassette, bool[]>();
        private readonly Dictionary<TargetCassette, Dictionary<int, WaferSlotState>> _slotStates =
            new Dictionary<TargetCassette, Dictionary<int, WaferSlotState>>();

        public BaseAxis OutputLifterZ { get; private set; }
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

        public OutputCassetteUnit() : base("BinCassetteUnit")
        {
            OutputLifterZ = AjinFactory.CreateAxis("OutputLifterZ");
            OutputLifterZ.Setup.SoftLimitPlus = 400.0;

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

            Components.Add(OutputLifterZ);
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

        public bool CanHandleJogAxis(BaseAxis axis)
        {
            return axis != null && ReferenceEquals(axis, OutputLifterZ);
        }

        public async Task<int> JogStepAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed,
            double axisStepDistance)
        {
            if (!CanHandleJogAxis(axis))
                return -1;

            double signedDistance = (direction < 0 ? -1.0 : 1.0) * Math.Abs(axisStepDistance);
            double target = OutputLifterZ.ActualPosition + signedDistance;
            await MoveBinLifterZ(target, speedType == JogSpeedType.Fine);
            return 0;
        }

        public Task<int> JogContinuousAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed)
        {
            if (!CanHandleJogAxis(axis))
                return Task.FromResult(-1);

            double speed = UnitJogVelocityResolver.Resolve(axis, speedType, customSpeed);
            ManualMoveBinLifterZJog(direction, speed);
            return Task.FromResult(0);
        }

        public Task<int> StopJogAsync(BaseAxis axis)
        {
            if (!CanHandleJogAxis(axis))
                return Task.FromResult(-1);

            ManualStopBinLifterZ();
            return Task.FromResult(0);
        }

        public async Task<int> MoveBinLifterZ(double targetPos, bool bFine = false)
        {
            return await MoveBinLifterZ(targetPos, bFine, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> MoveBinLifterZ(double targetPos, bool bFine, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                // Fine 이동은 JogFineVelocity, 일반 이동은 DefaultVelocity 퍼센트 스케일 적용.
                // DefaultVelocity 가 없을 때의 ScanVelocity fallback 은 Mapping Scan 의도이므로 스케일하지 않는다.
                double velocity = bFine
                    ? OutputLifterZ.Config.JogFineVelocity
                    : MotionSpeedScale.ApplyDefaultVelocityScale(OutputLifterZ.Config.DefaultVelocity);
                if (velocity <= 0.0)
                    velocity = bFine ? Math.Max(1.0, Config.ScanVelocity * 0.5) : Config.ScanVelocity;

                return await MoveWithProtrusionWatch(targetPos, velocity, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try { OutputLifterZ?.Stop(); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "MOTION", Name,
                    "Output cassette Z move failed. target=" + targetPos +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        public Task<int> MoveBinLifterZToTeachingPosition(string positionName, bool bFine = false)
        {
            return MoveBinLifterZ(GetTeachingPosition(positionName), bFine);
        }

        public Task<int> MoveToCassetteAvoidPosition(bool bFine = false) { return MoveToBinCassetteAvoidPosition(bFine); }
        public Task<int> MoveToBinCassetteAvoidPosition(bool bFine = false) { return MoveBinLifterZ(Recipe.AvoidPosition, bFine); }

        public Task<int> MoveToCassetteSlotPosition(int slotIndex, bool bFine = false)
        {
            return MoveToBinCassetteSlotPosition(ResolveActiveCassette(), slotIndex, bFine);
        }

        public Task<int> MoveToBinCassetteSlotPosition(TargetCassette cassette, int slotIndex, bool bFine = false)
        {
            return MoveBinLifterZ(CalculateBinCassetteSlotTargetPosition(cassette, slotIndex), bFine);
        }

        public Task<int> MoveToCassetteMappingStartPosition(bool bFine = false) { return MoveToBinCassetteMappingStartPosition(bFine); }
        public Task<int> MoveToBinCassetteMappingStartPosition(bool bFine = false) { return MoveBinLifterZ(Recipe.MappingStartPosition, bFine); }

        public Task<int> MoveToCassetteMappingEndPosition(bool bFine = false) { return MoveToBinCassetteMappingEndPosition(bFine); }
        public Task<int> MoveToBinCassetteMappingEndPosition(bool bFine = false) { return MoveBinLifterZ(Recipe.MappingEndPosition, bFine); }

        public bool IsBinLifterZInPosition(double targetPos)
        {
            return IsBinLifterZInPosition(targetPos, OutputLifterZ.Config.InPositionTolerance);
        }

        public bool IsBinLifterZInPosition(double targetPos, double tolerance)
        {
            return Math.Abs(OutputLifterZ.ActualPosition - targetPos) <= tolerance;
        }

        public async Task<bool> WaitBinLifterZMoveDone(int timeoutMs)
        {
            return await WaitBinLifterZMoveDone(timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> WaitBinLifterZMoveDone(int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                AxisMoveWaitResult waitResult = await WaitBinLifterZMoveDoneInPosition(OutputLifterZ.CommandPosition, timeoutMs, ct).ConfigureAwait(false);
                return waitResult.Success;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<AxisMoveWaitResult> WaitBinLifterZMoveDoneInPosition(double targetPos, int timeoutMs)
        {
            return await WaitBinLifterZMoveDoneInPosition(targetPos, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<AxisMoveWaitResult> WaitBinLifterZMoveDoneInPosition(double targetPos, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int timeout = timeoutMs > 0 ? timeoutMs : OutputLifterZ.Setup.MoveTimeoutMs;
                double tolerance = OutputLifterZ != null && OutputLifterZ.Config != null ? OutputLifterZ.Config.InPositionTolerance : 0.05;
                return await AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                    OutputLifterZ,
                    targetPos,
                    tolerance,
                    timeout,
                    Config != null ? Config.ScanSettleTimeMs : 0,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<bool> WaitBinLifterZInPosition(string positionName, int timeoutMs)
        {
            return await WaitBinLifterZInPosition(positionName, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> WaitBinLifterZInPosition(string positionName, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                double target = GetTeachingPosition(positionName);
                int timeout = timeoutMs > 0 ? timeoutMs : OutputLifterZ.Setup.MoveTimeoutMs;
                AxisMoveWaitResult waitResult = await WaitBinLifterZMoveDoneInPosition(target, timeout, ct).ConfigureAwait(false);
                return waitResult.Success;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
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
            SetTeachingPosition(positionName, OutputLifterZ.ActualPosition);
        }

        public void TeachBinLifterZAvoidPosition() { Recipe.AvoidPosition = OutputLifterZ.ActualPosition; }
        public void TeachBinLifterZMappingStartPosition() { Recipe.MappingStartPosition = OutputLifterZ.ActualPosition; }
        public void TeachBinLifterZMappingEndPosition() { Recipe.MappingEndPosition = OutputLifterZ.ActualPosition; }

        public void TeachBinLifterZSlotBasePosition()
        {
            TeachBinLifterZFirstSlotPosition(ResolveActiveCassette());
        }

        public void TeachBinLifterZFirstSlotPosition(TargetCassette cassette)
        {
            SetFirstSlotPosition(cassette, OutputLifterZ.ActualPosition);
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
            string reason;
            return ValidateBinLifterZTeachingComplete(out reason);
        }

        public bool ValidateBinLifterZTeachingComplete(out string reason)
        {
            reason = string.Empty;

            if (Config == null)
            {
                reason = "Output cassette config is null.";
                return false;
            }

            if (Recipe == null)
            {
                reason = "Output cassette recipe is null.";
                return false;
            }

            if (Config.SlotCount <= 0)
            {
                reason = "SlotCount is invalid. SlotCount=" + Config.SlotCount;
                return false;
            }

            if (Config.SlotPitch <= 0.0)
            {
                reason = "SlotPitch is invalid. SlotPitch=" + Config.SlotPitch;
                return false;
            }

            if (Recipe.MappingEndPosition <= Recipe.MappingStartPosition)
            {
                reason = "MappingEndPosition must be greater than MappingStartPosition because OutputLifterZ encoder increases upward. MappingStart=" +
                         Recipe.MappingStartPosition + ", MappingEnd=" + Recipe.MappingEndPosition;
                return false;
            }

            if (Recipe.NGFirstSlotPosition <= Recipe.GoodFirstSlotPosition)
            {
                reason = "NG first slot must have a larger encoder value than Good first slot because NG cassette is physically below Good cassette. NGFirstSlot=" +
                         Recipe.NGFirstSlotPosition + ", GoodFirstSlot=" + Recipe.GoodFirstSlotPosition;
                return false;
            }

            double slotSpan = Config.SlotPitch * Math.Max(0, Config.SlotCount - 1);
            double lowestSlotPosition = Math.Min(Recipe.NGFirstSlotPosition, Recipe.GoodFirstSlotPosition);
            double highestSlotPosition = Math.Max(Recipe.NGFirstSlotPosition + slotSpan, Recipe.GoodFirstSlotPosition + slotSpan);

            if (Config.SelectedCassetteLevel >= 2)
            {
                double good2FirstSlotPosition = GetGood2FirstSlotPosition();
                double good2LastSlotPosition = good2FirstSlotPosition + slotSpan;
                if (good2LastSlotPosition >= Recipe.GoodFirstSlotPosition)
                {
                    reason = "Good2 cassette must be above Good1 cassette, so Good2 last slot encoder must be smaller than Good1 first slot encoder. Good2LastSlot=" +
                             good2LastSlotPosition + ", Good1FirstSlot=" + Recipe.GoodFirstSlotPosition +
                             ", Good2FirstSlot=" + good2FirstSlotPosition +
                             ", Level2Offset=" + Config.Level2PositionOffset;
                    return false;
                }

                lowestSlotPosition = Math.Min(lowestSlotPosition, good2FirstSlotPosition);
                highestSlotPosition = Math.Max(highestSlotPosition, good2LastSlotPosition);
            }

            if (Recipe.MappingStartPosition > lowestSlotPosition)
            {
                reason = "MappingStartPosition is above the lowest output cassette slot. MappingStart=" +
                         Recipe.MappingStartPosition + ", lowestSlot=" + lowestSlotPosition +
                         ", NGFirstSlot=" + Recipe.NGFirstSlotPosition + ", GoodFirstSlot=" + Recipe.GoodFirstSlotPosition;
                return false;
            }

            if (Recipe.MappingEndPosition < highestSlotPosition)
            {
                Log.Write(
                    "Main",
                    "SYSTEM",
                    "OutputCassetteMapping",
                    "MappingEndPosition is below the highest output cassette slot, but mapping will continue due to output lifter stroke limit. MappingEnd=" +
                    Recipe.MappingEndPosition + ", highestSlot=" + highestSlotPosition +
                    ", SlotCount=" + Config.SlotCount + ", SlotPitch=" + Config.SlotPitch +
                    ", GoodLevel=" + Config.SelectedCassetteLevel + " - Check");
            }

            return true;
        }

        public async Task<int> MoveToTeachingPositionAndVerify(string positionName, bool bFine = false)
        {
            try
            {
                int result = await MoveBinLifterZToTeachingPosition(positionName, bFine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                bool arrived = await WaitBinLifterZInPosition(positionName, OutputLifterZ.Setup.MoveTimeoutMs).ConfigureAwait(false);
                if (!arrived)
                {
                    Log.Write("Main", "SYSTEM", "OutputCassetteMove",
                        "Output lifter teaching position wait failed. position=" + positionName + " - Failed");
                    return -1;
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "OutputCassetteMove",
                    "Output lifter teaching position move failed. position=" + positionName +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
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
            int timeout = timeoutMs > 0 ? timeoutMs : OutputLifterZ.Setup.MoveTimeoutMs;
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
            if (IsDryRunInput(GoodBin8CassetteCheck0) ||
                IsDryRunInput(GoodBin12CassetteCheck0))
                return true;

            if (nSize == 8) return GoodBin8CassetteCheck0.IsOn || GoodBin8CassetteCheck1.IsOn;
            if (nSize == 12) return GoodBin12CassetteCheck0.IsOn || GoodBin12CassetteCheck1.IsOn;
            return IsAnyCassetteSensorOn(TargetCassette.Good1);
        }

        public bool IsNgBin(int nSize)
        {
            if (IsDryRunInput(NgBin8CassetteCheck0) ||
                IsDryRunInput(NgBin12CassetteCheck0))
                return true;

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
            if (cassette == TargetCassette.Ng &&
                (IsDryRunInput(NgBin8CassetteCheck0) || IsDryRunInput(NgBin12CassetteCheck0)))
                return true;

            if (cassette != TargetCassette.Ng &&
                (IsDryRunInput(GoodBin8CassetteCheck0) || IsDryRunInput(GoodBin12CassetteCheck0)))
                return true;

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

        // 시뮬레이션/DryRun에서는 물리 센서 대신 Unlock 출력 상태를 반영한다(출력 누르면 센서 ON).
        public bool IsNgBinBW()
        {
            if (IsOutputCassetteHardwareBypassed())
                return NgBinCassetteUnlockOut != null && NgBinCassetteUnlockOut.IsOn;
            return IsDryRunInput(NgBinCassetteBw) || NgBinCassetteBw.IsOn;
        }

        // 시뮬레이션/DryRun에서는 물리 센서 대신 Lock 출력 상태를 반영한다(출력 누르면 센서 ON).
        public bool IsNgBinLock()
        {
            if (IsOutputCassetteHardwareBypassed())
                return NgBinCassetteLockOut != null && NgBinCassetteLockOut.IsOn;
            return IsDryRunInput(NgBinCassetteLock) || NgBinCassetteLock.IsOn;
        }
        public bool IsBinProtrusionDetectionSensor() { return IsBinProtrusionDetected(); }
        public bool IsBinProtrusionDetected() { return !IsDryRunInput(BinRingJutCheck) && BinRingJutCheck.IsOn; }
        public bool IsBinMapping() { return !IsDryRunInput(BinMappingSensor) && BinMappingSensor.IsOn; }

        private static bool IsDryRunInput(BaseDigitalInput input)
        {
            return input != null && input.Config != null && input.Config.IgnoreWaits;
        }

        public async Task<bool> WaitNgBinLock(int timeoutMs)
        {
            return await WaitNgBinLock(timeoutMs, CancellationToken.None);
        }

        public async Task<bool> WaitNgBinLock(int timeoutMs, CancellationToken ct)
        {
            return await NgBinCassetteLock.WaitUntilStateAsync(true, timeoutMs, ct);
        }

        public async Task<bool> WaitBinJutClear(int timeoutMs)
        {
            return await WaitBinJutClear(timeoutMs, CancellationToken.None);
        }

        public async Task<bool> WaitBinJutClear(int timeoutMs, CancellationToken ct)
        {
            return await BinRingJutCheck.WaitUntilStateAsync(false, timeoutMs, ct);
        }

        public async Task<bool> WaitBinMappingSensor(bool expected, int timeoutMs)
        {
            return await WaitBinMappingSensor(expected, timeoutMs, CancellationToken.None);
        }

        public async Task<bool> WaitBinMappingSensor(bool expected, int timeoutMs, CancellationToken ct)
        {
            return await BinMappingSensor.WaitUntilStateAsync(expected, timeoutMs, ct);
        }

        public void ManualMoveBinLifterZJog(Direction dir, double speed)
        {
            int direction = dir == Direction.Plus ? 1 : -1;
            ManualMoveBinLifterZJog(direction, speed);
        }

        public void ManualMoveBinLifterZJog(int direction, double speed)
        {
            OutputLifterZ.MoveJogContinuous(direction, JogSpeedType.Custom, speed);
        }

        public void ManualStopBinLifterZ()
        {
            OutputLifterZ.StopJog();
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

        public async Task<int> MoveToNextSlot(bool bFine = false)
        {
            try
            {
                int slot = FindNextProcessBinSlot();
                if (slot < 0)
                    return -1;

                int result = await MoveToCassetteSlotPosition(slot, bFine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                bool done = await WaitBinLifterZMoveDone(OutputLifterZ.Setup.MoveTimeoutMs).ConfigureAwait(false);
                return done ? 0 : -1;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FailMappingScan("OUT-CST-NEXT-SLOT", "Output cassette next slot move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<bool> ScanCassetteAsync(TargetCassette cassette, int maxSlots, double slotPitch)
        {
            return await ScanCassetteAsync(cassette, maxSlots, slotPitch, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> ScanCassetteAsync(TargetCassette cassette, int maxSlots, double slotPitch, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (maxSlots <= 0 || slotPitch <= 0.0)
                return false;

            if (IsOutputCassetteHardwareBypassed())
            {
                BuildSimulatedBinMap(cassette, maxSlots, slotPitch);
                return true;
            }

            List<double> detectedPositions = await CollectBinMappingSensorPositionsAsync(cassette, maxSlots, slotPitch, true, ct);
            if (detectedPositions == null)
                return false;

            bool[] slotMap;
            double[] slotPositions;
            if (!BuildBinMappingResultFromDetectedPositions(cassette, detectedPositions, maxSlots, slotPitch, out slotMap, out slotPositions))
                return false;

            ApplyBinMappingResult(cassette, slotMap, slotPositions);
            return true;
        }

        public async Task<bool> ScanAllCassettesAsync()
        {
            return await ScanAllCassettesAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> ScanAllCassettesAsync(CancellationToken ct)
        {
            BeginMapping();
            try
            {
                ct.ThrowIfCancellationRequested();
                int maxSlots = Config.SlotCount;
                double slotPitch = Config.SlotPitch;
                if (maxSlots <= 0 || slotPitch <= 0.0)
                {
                    FailMappingScan("OUT-CST-MAP-CONFIG", "Output cassette mapping config is invalid.");
                    return false;
                }

                if (IsOutputCassetteHardwareBypassed())
                {
                    BuildSimulatedBinMap(TargetCassette.Ng, maxSlots, slotPitch);
                    BuildSimulatedBinMap(TargetCassette.Good1, maxSlots, slotPitch);
                    BuildSimulatedBinMap(TargetCassette.Good2, maxSlots, slotPitch);
                    return true;
                }

                if (!IsAnyCassetteSensorOn(TargetCassette.Good1))
                    return FailMappingScanBool("OUT-CST-MAP-GOOD-MISSING", "Good cassette is not detected.");

                if (!IsAnyCassetteSensorOn(TargetCassette.Ng))
                    return FailMappingScanBool("OUT-CST-MAP-NG-MISSING", "NG cassette is not detected.");

                List<double> detectedPositions = await CollectBinMappingSensorPositionsAsync(TargetCassette.Good1, maxSlots, slotPitch, true, ct).ConfigureAwait(false);
                if (detectedPositions == null)
                    return false;

                if (!ApplyDetectedBinMapping(TargetCassette.Ng, detectedPositions, maxSlots, slotPitch))
                    return false;

                if (!ApplyDetectedBinMapping(TargetCassette.Good1, detectedPositions, maxSlots, slotPitch))
                    return false;

                if (!ApplyDetectedBinMapping(TargetCassette.Good2, detectedPositions, maxSlots, slotPitch))
                    return false;

                return true;
            }
            finally
            {
                EndMapping();
            }
        }

        public async Task<bool> ScanAllCassettesFromCurrentStartAsync()
        {
            return await ScanAllCassettesFromCurrentStartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> ScanAllCassettesFromCurrentStartAsync(CancellationToken ct)
        {
            BeginMapping();
            try
            {
                ct.ThrowIfCancellationRequested();
                int maxSlots = Config.SlotCount;
                double slotPitch = Config.SlotPitch;
                if (maxSlots <= 0 || slotPitch <= 0.0)
                {
                    FailMappingScan("OUT-CST-MAP-CONFIG", "Output cassette mapping config is invalid.");
                    return false;
                }

                if (IsOutputCassetteHardwareBypassed())
                {
                    BuildSimulatedBinMap(TargetCassette.Ng, maxSlots, slotPitch);
                    BuildSimulatedBinMap(TargetCassette.Good1, maxSlots, slotPitch);
                    BuildSimulatedBinMap(TargetCassette.Good2, maxSlots, slotPitch);
                    int moved = await MoveToBinCassetteMappingEndAndVerifyAsync(ct).ConfigureAwait(false);
                    if (moved != 0)
                        return false;

                    return true;
                }

                if (!IsAnyCassetteSensorOn(TargetCassette.Good1))
                    return FailMappingScanBool("OUT-CST-MAP-GOOD-MISSING", "Good cassette is not detected.");

                if (!IsAnyCassetteSensorOn(TargetCassette.Ng))
                    return FailMappingScanBool("OUT-CST-MAP-NG-MISSING", "NG cassette is not detected.");

                List<double> detectedPositions = await CollectBinMappingSensorPositionsAsync(TargetCassette.Good1, maxSlots, slotPitch, false, ct).ConfigureAwait(false);
                if (detectedPositions == null)
                    return false;

                if (!ApplyDetectedBinMapping(TargetCassette.Ng, detectedPositions, maxSlots, slotPitch))
                    return false;

                if (!ApplyDetectedBinMapping(TargetCassette.Good1, detectedPositions, maxSlots, slotPitch))
                    return false;

                if (!ApplyDetectedBinMapping(TargetCassette.Good2, detectedPositions, maxSlots, slotPitch))
                    return false;

                return true;
            }
            catch (OperationCanceledException)
            {
                try { OutputLifterZ?.Stop(); } catch { }
                throw;
            }
            finally
            {
                EndMapping();
            }
        }

        private async Task<List<double>> CollectBinMappingSensorPositionsAsync(
            TargetCassette referenceCassette,
            int maxSlots,
            double slotPitch,
            bool moveToStart)
        {
            return await CollectBinMappingSensorPositionsAsync(referenceCassette, maxSlots, slotPitch, moveToStart, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task<List<double>> CollectBinMappingSensorPositionsAsync(
            TargetCassette referenceCassette,
            int maxSlots,
            double slotPitch,
            bool moveToStart,
            CancellationToken ct)
        {
            double originalAcc = 0.0;
            double originalDec = 0.0;
            bool restoreScanProfile = false;

            try
            {
                ct.ThrowIfCancellationRequested();
                if (!IsAnyCassetteSensorOn(referenceCassette))
                    return FailMappingScanList("OUT-CST-MAP-CST-MISSING", "Output cassette is not detected. cassette=" + referenceCassette);

                if (moveToStart)
                {
                    int startResult = await MoveToBinCassetteMappingStartAndVerifyAsync(ct).ConfigureAwait(false);
                    if (startResult != 0)
                        return null;
                }

                var detectedPositions = new List<double>();
                bool previous = BinMappingSensor.IsOn;
                if (previous)
                    return FailMappingScanList("OUT-CST-MAP-SENSOR-ON", "Mapping sensor is ON at mapping start. Check mapping start position.");

                double scanVelocity = Config.ScanVelocity > 0.0 ? Config.ScanVelocity : OutputLifterZ.Config.DefaultVelocity;
                if (scanVelocity <= 0.0)
                    scanVelocity = 1.0;

                if (OutputLifterZ.Config != null && (Config.ScanAcc > 0.0 || Config.ScanDec > 0.0))
                {
                    originalAcc = OutputLifterZ.Config.Acceleration;
                    originalDec = OutputLifterZ.Config.Deceleration;
                    if (Config.ScanAcc > 0.0)
                        OutputLifterZ.Config.Acceleration = Config.ScanAcc;
                    if (Config.ScanDec > 0.0)
                        OutputLifterZ.Config.Deceleration = Config.ScanDec;
                    restoreScanProfile = true;
                }

                Task<int> moveTask = OutputLifterZ.MoveAbsoluteAsync(Recipe.MappingEndPosition, scanVelocity);
                while (!moveTask.IsCompleted)
                {
                    ct.ThrowIfCancellationRequested();
                    if (IsBinProtrusionDetected())
                    {
                        OutputLifterZ.EStop();
                        return FailMappingScanList("OUT-CST-MAP-PROTRUSION", "Bin protrusion detected during mapping scan.");
                    }

                    bool current = BinMappingSensor.IsOn;
                    if (current && !previous)
                        AddDetectedBinMappingPosition(detectedPositions, OutputLifterZ.ActualPosition, slotPitch);

                    previous = current;
                    await Task.Delay(5, ct).ConfigureAwait(false);
                }

                int moveResult = await moveTask.ConfigureAwait(false);
                if (moveResult != 0 || OutputLifterZ.IsAlarm)
                    return FailMappingScanList("OUT-CST-MAP-END", "OutputLifterZ move failed during mapping scan.");

                AxisMoveWaitResult waitResult = await WaitBinLifterZMoveDoneInPosition(Recipe.MappingEndPosition, OutputLifterZ.Setup.MoveTimeoutMs, ct).ConfigureAwait(false);
                if (!waitResult.Success)
                    return FailMappingScanList(
                        ResolveBinLifterZMoveWaitAlarmCode("OUT-CST-MAP-END", waitResult.Failure),
                        "OutputLifterZ mapping end move/in-position wait failed. waitResult=" + waitResult.Code +
                        ", reason=" + waitResult.Reason + ". " + waitResult.AxisState);

                return detectedPositions;
            }
            catch (OperationCanceledException)
            {
                try { OutputLifterZ?.Stop(); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                return FailMappingScanList("OUT-CST-MAP-COLLECT", "Mapping sensor position collect failed: " + ex.Message);
            }
            finally
            {
                if (restoreScanProfile && OutputLifterZ != null && OutputLifterZ.Config != null)
                {
                    OutputLifterZ.Config.Acceleration = originalAcc;
                    OutputLifterZ.Config.Deceleration = originalDec;
                }
            }
        }

        private async Task<int> MoveToBinCassetteMappingEndAndVerifyAsync()
        {
            return await MoveToBinCassetteMappingEndAndVerifyAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task<int> MoveToBinCassetteMappingEndAndVerifyAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveBinLifterZ(Recipe.MappingEndPosition, true, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                AxisMoveWaitResult waitResult = await WaitBinLifterZMoveDoneInPosition(Recipe.MappingEndPosition, OutputLifterZ.Setup.MoveTimeoutMs, ct).ConfigureAwait(false);
                if (!waitResult.Success)
                    return FailMappingScan(
                        ResolveBinLifterZMoveWaitAlarmCode("OUT-CST-MAP-END", waitResult.Failure),
                        "OutputLifterZ mapping end move/in-position wait failed. waitResult=" + waitResult.Code +
                        ", reason=" + waitResult.Reason + ". " + waitResult.AxisState);

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FailMappingScan("OUT-CST-MAP-END", "OutputLifterZ move failed at mapping end: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveToBinCassetteMappingStartAndVerifyAsync()
        {
            return await MoveToBinCassetteMappingStartAndVerifyAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task<int> MoveToBinCassetteMappingStartAndVerifyAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveBinLifterZ(Recipe.MappingStartPosition, true, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                AxisMoveWaitResult waitResult = await WaitBinLifterZMoveDoneInPosition(Recipe.MappingStartPosition, OutputLifterZ.Setup.MoveTimeoutMs, ct).ConfigureAwait(false);
                if (!waitResult.Success)
                    return FailMappingScan(
                        ResolveBinLifterZMoveWaitAlarmCode("OUT-CST-MAP-START", waitResult.Failure),
                        "OutputLifterZ mapping start move/in-position wait failed. waitResult=" + waitResult.Code +
                        ", reason=" + waitResult.Reason + ". " + waitResult.AxisState);

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FailMappingScan("OUT-CST-MAP-START", "OutputLifterZ move failed at mapping start: " + ex.Message);
            }
            finally
            {
            }
        }

        private void AddDetectedBinMappingPosition(List<double> detectedPositions, double position, double slotPitch)
        {
            try
            {
                double minSpacing = slotPitch > 0.0 ? slotPitch * 0.5 : 0.0;
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

        private bool ApplyDetectedBinMapping(
            TargetCassette cassette,
            IReadOnlyList<double> detectedPositions,
            int maxSlots,
            double slotPitch)
        {
            bool[] slotMap;
            double[] slotPositions;
            if (!BuildBinMappingResultFromDetectedPositions(cassette, detectedPositions, maxSlots, slotPitch, out slotMap, out slotPositions))
                return false;

            ApplyBinMappingResult(cassette, slotMap, slotPositions);
            return true;
        }

        private bool BuildBinMappingResultFromDetectedPositions(
            TargetCassette cassette,
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
                double firstSlotPosition = GetFirstSlotPosition(cassette);
                double tolerance = ResolveMappingPitchTolerance(slotPitch);
                int previousSlot = -1;
                double previousPosition = double.NaN;

                foreach (double position in detectedPositions)
                {
                    int slotIndex = (int)Math.Round((position - firstSlotPosition) / slotPitch);
                    if (slotIndex < 0 || slotIndex >= maxSlots)
                        continue;

                    double nominalPosition = firstSlotPosition + (slotPitch * slotIndex);
                    double nominalError = Math.Abs(position - nominalPosition);
                    if (nominalError > tolerance)
                        continue;

                    if (slotMap[slotIndex])
                    {
                        FailMappingScan("OUT-CST-MAP-DUPLICATE", "Duplicate bin detection matched to the same slot. cassette=" + cassette + ", slot=" + (slotIndex + 1));
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
                                "OUT-CST-MAP-PITCH-CHECK",
                                "Mapping pitch check failed. cassette=" + cassette +
                                ", prevSlot=" + (previousSlot + 1) +
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
                FailMappingScan("OUT-CST-MAP-BUILD", "Mapping result build failed. cassette=" + cassette + ", error=" + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private void ApplyBinMappingResult(TargetCassette cassette, bool[] slotMap, double[] slotPositions)
        {
            try
            {
                Recipe.EnsureSlotPositionBuffers(Config.SlotCount);
                _slotMap[cassette] = slotMap;

                for (int i = 0; i < slotMap.Length; i++)
                {
                    if (slotMap[i] && i < slotPositions.Length && !double.IsNaN(slotPositions[i]))
                        Recipe.UpdateSlotPosition(cassette, i, slotPositions[i]);

                    SlotPresence presence = slotMap[i] ? SlotPresence.Exist : SlotPresence.Empty;
                    UpdateCassetteSlotState(cassette, i, presence, ProcessState.Ready);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void BuildSimulatedBinMap(TargetCassette cassette, int maxSlots, double slotPitch)
        {
            try
            {
                Recipe.EnsureSlotPositionBuffers(maxSlots);
                bool[] map = new bool[maxSlots];

                for (int i = 0; i < maxSlots; i++)
                {
                    map[i] = true;
                    double position = GetFirstSlotPosition(cassette) + (i * slotPitch);
                    Recipe.UpdateSlotPosition(cassette, i, position);
                    UpdateCassetteSlotState(cassette, i, SlotPresence.Exist, ProcessState.Ready);
                }

                _slotMap[cassette] = map;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private bool IsOutputCassetteHardwareBypassed()
        {
            return Config.bDryRun ||
                   Setup.IsSimulationMode ||
                   (OutputLifterZ != null && OutputLifterZ.Config != null && OutputLifterZ.Config.IsSimulationMode);
        }

        private double ResolveMappingPitchTolerance(double slotPitch)
        {
            try
            {
                double tolerance = OutputLifterZ != null && OutputLifterZ.Config != null
                    ? OutputLifterZ.Config.InPositionTolerance
                    : 0.0;
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

        private static string ResolveBinLifterZMoveWaitAlarmCode(string prefix, AxisMoveWaitFailure failure)
        {
            return AxisMoveWaiter.ResolveAlarmCode(prefix, failure);
        }

        private int FailMappingScan(string alarmCode, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", "OutputCassetteMapping", message + " - Failed");
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

        private bool FailMappingScanBool(string alarmCode, string message)
        {
            FailMappingScan(alarmCode, message);
            return false;
        }

        private List<double> FailMappingScanList(string alarmCode, string message)
        {
            FailMappingScan(alarmCode, message);
            return null;
        }

        private static string FormatPosition(double value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        public async Task<bool> StoreFullWaferAsync(OutputFeederUnit feeder, TargetCassette target, int slotIndex)
        {
            if (feeder == null)
                throw new ArgumentNullException("feeder");

            ValidateSlotIndex(slotIndex);
            BinSide side = ToBinSide(target);
            int timeoutMs = ResolveTransferTimeoutMs(feeder);

            int result = await feeder.UnloadWaferFromStageToFeeder(side, timeoutMs);
            if (result != 0)
                return false;

            result = await PrepareBinCassetteForFeederLoad(target, slotIndex, timeoutMs).ConfigureAwait(false);
            if (result != 0)
                return false;

            result = await feeder.UnloadFeederToCassette(side, slotIndex, timeoutMs);
            if (result != 0)
                return false;

            UpdateBinCassetteSlotState(target, slotIndex, true);
            return true;
        }

        public async Task<bool> SupplyEmptyWaferAsync(OutputFeederUnit feeder, TargetCassette source, int slotIndex)
        {
            if (feeder == null)
                throw new ArgumentNullException("feeder");

            ValidateSlotIndex(slotIndex);
            BinSide side = ToBinSide(source);
            int timeoutMs = ResolveTransferTimeoutMs(feeder);

            int result = await PrepareBinCassetteForFeederLoad(source, slotIndex, timeoutMs).ConfigureAwait(false);
            if (result != 0)
                return false;

            result = await feeder.LoadFromCassetteToFeeder(side, slotIndex, timeoutMs, false, false);
            if (result != 0)
                return false;

            result = await feeder.LoadWaferToStageFromFeeder(side, timeoutMs);
            if (result != 0)
                return false;

            UpdateBinCassetteSlotState(source, slotIndex, false);
            return true;
        }

        public async Task<bool> ExchangeWaferSequenceAsync(
            OutputFeederUnit feeder,
            TargetCassette storeTarget,
            int storeSlotIndex,
            TargetCassette supplySource,
            int supplySlotIndex)
        {
            if (!await StoreFullWaferAsync(feeder, storeTarget, storeSlotIndex))
                return false;

            return await SupplyEmptyWaferAsync(feeder, supplySource, supplySlotIndex);
        }

        public Task<int> PrepareCassetteForFeederLoad(int slotIndex, int timeoutMs, bool bFine = false)
        {
            return PrepareBinCassetteForFeederLoad(ResolveActiveCassette(), slotIndex, timeoutMs, bFine);
        }

        public async Task<int> PrepareBinCassetteForFeederLoad(TargetCassette cassette, int slotIndex, int timeoutMs, bool bFine = false)
        {
            try
            {
                if (!CheckBinLifterZMoveReady())
                    return -1;

                int result = await MoveToBinCassetteSlotPosition(cassette, slotIndex, bFine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                AxisMoveWaitResult waitResult = await WaitBinLifterZMoveDoneInPosition(CalculateBinCassetteSlotTargetPosition(cassette, slotIndex), timeoutMs).ConfigureAwait(false);
                if (!waitResult.Success)
                    return FailMappingScan(
                        ResolveBinLifterZMoveWaitAlarmCode("OUT-CST-FEEDER-LOAD", waitResult.Failure),
                        "Prepare bin cassette for feeder load move/in-position wait failed. waitResult=" + waitResult.Code +
                        ", reason=" + waitResult.Reason + ". " + waitResult.AxisState);

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FailMappingScan(
                    "OUT-CST-FEEDER-LOAD-EXCEPTION",
                    "Output cassette feeder load preparation failed. cassette=" + cassette +
                    ", slot=" + slotIndex + ", error=" + ex.Message);
            }
            finally
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputCassette",
                    "PrepareBinCassetteForFeederLoad finished. cassette=" + cassette +
                    ", slot=" + slotIndex);
            }
        }

        public Task<int> RecoverCassetteToSafeState(int timeoutMs, bool moveAvoid = true)
        {
            return RecoverBinCassetteToSafeState(timeoutMs, moveAvoid);
        }

        public async Task<int> RecoverBinCassetteToSafeState(int timeoutMs, bool moveAvoid = true)
        {
            try
            {
                if (!await WaitBinJutClear(timeoutMs).ConfigureAwait(false))
                    return -1;

                if (moveAvoid)
                {
                    int result = await MoveToBinCassetteAvoidPosition().ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    AxisMoveWaitResult waitResult = await WaitBinLifterZMoveDoneInPosition(Recipe.AvoidPosition, timeoutMs).ConfigureAwait(false);
                    if (!waitResult.Success)
                        return FailMappingScan(
                            ResolveBinLifterZMoveWaitAlarmCode("OUT-CST-RECOVER-AVOID", waitResult.Failure),
                            "Recover bin cassette to avoid position move/in-position wait failed. waitResult=" + waitResult.Code +
                            ", reason=" + waitResult.Reason + ". " + waitResult.AxisState);
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FailMappingScan(
                    "OUT-CST-RECOVER-EXCEPTION",
                    "Output cassette recover failed. error=" + ex.Message);
            }
            finally
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputCassette",
                    "RecoverBinCassetteToSafeState finished. moveAvoid=" + moveAvoid);
            }
        }

        public bool CheckBinLifterZMoveReady()
        {
            string reason;
            return CheckBinLifterZMoveReady(out reason);
        }

        public bool CheckBinLifterZMoveReady(out string reason)
        {
            reason = string.Empty;

            if (OutputLifterZ == null)
            {
                reason = "OutputLifterZ is null.";
                return false;
            }

            if (!OutputLifterZ.IsServoOn)
            {
                reason = "OutputLifterZ servo is OFF. " + BuildOutputLifterZState();
                return false;
            }

            if (OutputLifterZ.IsAlarm)
            {
                reason = "OutputLifterZ alarm is ON. " + BuildOutputLifterZState();
                return false;
            }

            if (OutputLifterZ.IsMoving)
            {
                reason = "OutputLifterZ is moving. " + BuildOutputLifterZState();
                return false;
            }

            if (IsBinProtrusionDetected())
            {
                reason = "Bin protrusion sensor is detected. " + BuildOutputCassetteSensorSummary();
                return false;
            }

            return true;
        }

        public bool CheckBinCassetteMoveReady() { return CheckBinLifterZMoveReady(); }

        public bool CheckBinCassetteMoveReady(out string reason) { return CheckBinLifterZMoveReady(out reason); }

        public bool CheckCassetteTransferReady(TransferMode mode)
        {
            return CheckBinCassetteTransferReady(ResolveActiveCassette(), mode);
        }

        public bool CheckCassetteTransferReady(TransferMode mode, out string reason)
        {
            return CheckBinCassetteTransferReady(ResolveActiveCassette(), mode, out reason);
        }

        public bool CheckBinCassetteTransferReady(TargetCassette cassette, TransferMode mode)
        {
            string reason;
            return CheckBinCassetteTransferReady(cassette, mode, out reason);
        }

        public bool CheckBinCassetteTransferReady(TargetCassette cassette, TransferMode mode, out string reason)
        {
            reason = string.Empty;

            string moveReason;
            if (!CheckBinLifterZMoveReady(out moveReason))
            {
                reason = moveReason;
                return false;
            }

            if (mode == TransferMode.Load || mode == TransferMode.Unload)
            {
                if (!IsAnyCassetteSensorOn(cassette))
                {
                    reason = "Output cassette sensor is not detected. cassette=" + cassette + ", mode=" + mode + ". " +
                             BuildOutputCassetteSensorSummary();
                    return false;
                }
            }

            return true;
        }

        public bool CheckCassetteMappingReady()
        {
            return CheckBinCassetteMappingReady(ResolveActiveCassette());
        }

        public bool CheckBinCassetteMappingReady(TargetCassette cassette)
        {
            string reason;
            return CheckBinCassetteMappingReady(cassette, out reason);
        }

        public bool CheckBinCassetteMappingReady(TargetCassette cassette, out string reason)
        {
            reason = string.Empty;

            string moveReason;
            if (!CheckBinLifterZMoveReady(out moveReason))
            {
                reason = moveReason;
                return false;
            }

            if (IsBinProtrusionDetected())
            {
                reason = "Output cassette product/protrusion sensor is detected before mapping. " +
                         BuildOutputCassetteSensorSummary();
                return false;
            }

            if (!IsAnyCassetteSensorOn(cassette))
            {
                reason = "Output cassette sensor is not detected for mapping. cassette=" + cassette + ". " +
                         BuildOutputCassetteSensorSummary();
                return false;
            }

            string teachingReason;
            if (!ValidateBinLifterZTeachingComplete(out teachingReason))
            {
                reason = "Output cassette lifter teaching is not complete. " + teachingReason;
                return false;
            }

            return true;
        }

        public bool CheckCassetteDirectionReady()
        {
            return !IsNgBinBW();
        }

        public string DescribeOutputLifterZState()
        {
            return BuildOutputLifterZState();
        }

        public string DescribeOutputLifterZState(double targetPosition)
        {
            return BuildOutputLifterZState(targetPosition);
        }

        private string BuildOutputLifterZState()
        {
            return BuildOutputLifterZState(double.NaN);
        }

        private string BuildOutputLifterZState(double targetPosition)
        {
            if (OutputLifterZ == null)
                return "OutputLifterZ=null";

            string state = "OutputLifterZ[name=" + OutputLifterZ.Name +
                           ", servo=" + (OutputLifterZ.IsServoOn ? "ON" : "OFF") +
                           ", alarm=" + (OutputLifterZ.IsAlarm ? "ON" : "OFF") +
                           ", alarmCode=" + OutputLifterZ.AlarmCode +
                           ", moving=" + (OutputLifterZ.IsMoving ? "Y" : "N") +
                           ", actual=" + OutputLifterZ.ActualPosition +
                           ", command=" + OutputLifterZ.CommandPosition;

            if (!double.IsNaN(targetPosition))
                state += ", target=" + targetPosition;

            if (OutputLifterZ.Config != null)
                state += ", tolerance=" + OutputLifterZ.Config.InPositionTolerance;

            if (!string.IsNullOrWhiteSpace(OutputLifterZ.LastMotionFailureMessage))
                state += ", lastMotionFailure=" + OutputLifterZ.LastMotionFailureMessage;

            return state + "]";
        }

        private string BuildOutputCassetteSensorSummary()
        {
            return "Sensors[" +
                   "Good8-0=" + FormatInputState(GoodBin8CassetteCheck0) +
                   ", Good8-1=" + FormatInputState(GoodBin8CassetteCheck1) +
                   ", Good12-0=" + FormatInputState(GoodBin12CassetteCheck0) +
                   ", Good12-1=" + FormatInputState(GoodBin12CassetteCheck1) +
                   ", Ng8-0=" + FormatInputState(NgBin8CassetteCheck0) +
                   ", Ng8-1=" + FormatInputState(NgBin8CassetteCheck1) +
                   ", Ng12-0=" + FormatInputState(NgBin12CassetteCheck0) +
                   ", Ng12-1=" + FormatInputState(NgBin12CassetteCheck1) +
                   ", NgBW=" + FormatInputState(NgBinCassetteBw) +
                   ", NgLock=" + FormatInputState(NgBinCassetteLock) +
                   ", Protrusion=" + FormatInputState(BinRingJutCheck) +
                   ", Mapping=" + FormatInputState(BinMappingSensor) +
                   "]";
        }

        private static string FormatInputState(QMC.Common.IO.BaseDigitalInput input)
        {
            if (input == null)
                return "null";

            return input.Name + "=" + (input.IsOn ? "ON" : "OFF");
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
            OutputLifterZ.Stop();
            Console.WriteLine("[STOP] '" + Name + "' " + reason);
        }

        public string BuildCassetteAlarmMessage(CassetteAlarmCode code)
        {
            switch (code)
            {
                // 카세트 미감지 알람 메시지
                case CassetteAlarmCode.CassetteMissing: return "Bin cassette is missing.";
                // 카세트 사이즈 불일치 알람 메시지
                case CassetteAlarmCode.SizeMismatch: return "Bin cassette size mismatch.";
                // BIN 돌출 감지 알람 메시지
                case CassetteAlarmCode.ProtrusionDetected: return "Bin protrusion detected.";
                // 맵핑 타임아웃 알람 메시지
                case CassetteAlarmCode.MappingTimeout: return "Bin mapping timeout.";
                // 리프터 Z축 이동 타임아웃 알람 메시지
                case CassetteAlarmCode.MoveTimeout: return "Bin lifter Z move timeout.";
                // 리프터 Z축 티칭 누락 알람 메시지
                case CassetteAlarmCode.TeachingMissing: return "Bin lifter Z teaching data is missing.";
                // NG 카세트 잠금 타임아웃 알람 메시지
                case CassetteAlarmCode.LockTimeout: return "NG bin cassette lock timeout.";
                default: return "No bin cassette alarm.";
            }
        }

        public Task<int> MoveToTargetSlotAsync(double targetPosition)
        {
            return MoveBinLifterZ(targetPosition);
        }

        private async Task<int> MoveWithProtrusionWatch(double targetPosition, double velocity, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (IsBinProtrusionDetected())
                {
                    OutputLifterZ.EStop();
                    QMC.Common.Log.Write("Main", "MOTION", Name,
                        "Output cassette Z move blocked. Protrusion sensor is ON. target=" + targetPosition + " - Failed");
                    return -1;
                }

                Task<int> moveTask = OutputLifterZ.MoveAbsoluteAsync(targetPosition, velocity);
                while (!moveTask.IsCompleted)
                {
                    ct.ThrowIfCancellationRequested();

                    if (IsBinProtrusionDetected())
                    {
                        OutputLifterZ.EStop();
                        QMC.Common.Log.Write("Main", "MOTION", Name,
                            "Output cassette Z move stopped. Protrusion detected while moving. target=" + targetPosition + " - Failed");
                        return -1;
                    }

                    await Task.Delay(10, ct).ConfigureAwait(false);
                }

                int moveResult = await moveTask.ConfigureAwait(false);
                if (moveResult != 0 || OutputLifterZ.IsAlarm)
                {
                    QMC.Common.Log.Write("Main", "MOTION", Name,
                        "Output cassette Z move command failed. result=" + moveResult +
                        ", velocity=" + velocity + ". " + BuildOutputLifterZState(targetPosition) + " - Failed");
                    return moveResult != 0 ? moveResult : -1;
                }

                AxisMoveWaitResult waitResult = await WaitBinLifterZMoveDoneInPosition(
                    targetPosition,
                    OutputLifterZ.Setup != null ? OutputLifterZ.Setup.MoveTimeoutMs : 10000,
                    ct).ConfigureAwait(false);
                if (!waitResult.Success)
                {
                    QMC.Common.Log.Write("Main", "MOTION", Name,
                        "Output cassette Z move wait/in-position failed. " +
                        AxisMoveWaiter.FormatResult(waitResult, "OutputLifterZ") + " - Failed");
                    return -1;
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                try { OutputLifterZ?.Stop(); } catch { }
                throw;
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
            if (string.Equals(positionName, "NgFirstSlot", StringComparison.OrdinalIgnoreCase)) return Recipe.NGFirstSlotPosition;
            if (string.Equals(positionName, "GoodFirstSlot", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodFirstSlotPosition;
            if (string.Equals(positionName, "Good1FirstSlot", StringComparison.OrdinalIgnoreCase)) return Recipe.GoodFirstSlotPosition;
            if (string.Equals(positionName, "Good2FirstSlot", StringComparison.OrdinalIgnoreCase)) return GetGood2FirstSlotPosition();
            throw new ArgumentException("Unknown OutputLifterZ teaching position: " + positionName, "positionName");
        }

        private void SetTeachingPosition(string positionName, double position)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) Recipe.AvoidPosition = position;
            else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase)) Recipe.MappingStartPosition = position;
            else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase)) Recipe.MappingEndPosition = position;
            else if (string.Equals(positionName, "NgFirstSlot", StringComparison.OrdinalIgnoreCase)) Recipe.NGFirstSlotPosition = position;
            else if (string.Equals(positionName, "GoodFirstSlot", StringComparison.OrdinalIgnoreCase)) Recipe.GoodFirstSlotPosition = position;
            else if (string.Equals(positionName, "Good1FirstSlot", StringComparison.OrdinalIgnoreCase)) Recipe.GoodFirstSlotPosition = position;
            else if (string.Equals(positionName, "Good2FirstSlot", StringComparison.OrdinalIgnoreCase)) SetGood2FirstSlotPosition(position);
            else throw new ArgumentException("Unknown OutputLifterZ teaching position: " + positionName, "positionName");
        }

        private double GetFirstSlotPosition(TargetCassette cassette)
        {
            switch (cassette)
            {
                // NG 카세트 첫 슬롯 위치 반환
                case TargetCassette.Ng: return Recipe.NGFirstSlotPosition;
                // GOOD 1단 첫 슬롯 위치 반환
                case TargetCassette.Good1: return Recipe.GoodFirstSlotPosition;
                // GOOD 2단 첫 슬롯 위치 반환
                case TargetCassette.Good2: return GetGood2FirstSlotPosition();
                default: throw new ArgumentOutOfRangeException("cassette");
            }
        }

        private void SetFirstSlotPosition(TargetCassette cassette, double position)
        {
            switch (cassette)
            {
                // NG 카세트 첫 슬롯 위치 저장
                case TargetCassette.Ng:
                    Recipe.NGFirstSlotPosition = position;
                    break;
                // GOOD 1단 첫 슬롯 위치 저장
                case TargetCassette.Good1:
                    Recipe.GoodFirstSlotPosition = position;
                    break;
                // GOOD 2단 첫 슬롯 위치 저장
                case TargetCassette.Good2:
                    SetGood2FirstSlotPosition(position);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("cassette");
            }
        }

        private double GetGood2FirstSlotPosition()
        {
            double level1FirstPosition = Recipe.GoodFirstSlotPosition;
            double slotSpan = Config.SlotPitch * Math.Max(0, Config.SlotCount - 1);
            double levelGap = Config.Level2PositionOffset;
            if (levelGap <= 0.0)
                levelGap = Config.SlotPitch > 0.0 ? Config.SlotPitch : 0.001;

            return level1FirstPosition - levelGap - slotSpan;
        }

        private void SetGood2FirstSlotPosition(double position)
        {
            double slotSpan = Config.SlotPitch * Math.Max(0, Config.SlotCount - 1);
            Config.Level2PositionOffset = Math.Max(0.0, Recipe.GoodFirstSlotPosition - slotSpan - position);
        }

        private double GetMappedSlotPosition(TargetCassette cassette, int slotIndex)
        {
            Recipe.EnsureSlotPositionBuffers(Config.SlotCount);
            double[] slots = cassette == TargetCassette.Ng
                ? Recipe.NGSlotPosition
                : cassette == TargetCassette.Good2 ? Recipe.Good2SlotPosition : Recipe.GoodSlotPosition;
            if (slots != null && slotIndex >= 0 && slotIndex < slots.Length)
                return slots[slotIndex];
            return double.NaN;
        }

        private bool IsAnyCassetteSensorOn(TargetCassette cassette)
        {
            if (IsOutputCassetteHardwareBypassed())
                return true;

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

        private static BinSide ToBinSide(TargetCassette cassette)
        {
            return cassette == TargetCassette.Ng ? BinSide.Ng : BinSide.Good;
        }

        private static int ResolveTransferTimeoutMs(OutputFeederUnit feeder)
        {
            if (feeder != null && feeder.FeederY != null && feeder.FeederY.Setup != null && feeder.FeederY.Setup.MoveTimeoutMs > 0)
                return feeder.FeederY.Setup.MoveTimeoutMs;

            return 60000;
        }

        private static void SetOutput(BaseDigitalOutput output, bool on)
        {
            if (on) output.On();
            else output.Off();
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs)
        {
            return await WaitUntilAsync(condition, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs, CancellationToken ct)
        {
            int elapsed = 0;
            while (timeoutMs <= 0 || elapsed < timeoutMs)
            {
                if (condition())
                    return true;

                await Task.Delay(10, ct).ConfigureAwait(false);
                elapsed += 10;
            }
            return condition();
        }
    }
}
