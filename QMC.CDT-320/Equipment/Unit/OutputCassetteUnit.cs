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
using QMC.CDT320.Interlocks;

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
        // To do: [NG 스킵] NG 카세트 사용 여부 - false면 오토 시퀀스가 NG 공급/맵핑 요구를 건너뛴다.
        [DataMember] public bool UseNgCassette { get; set; }
        [DataMember] public double LoadingPositionOffset { get; set; }
        [DataMember] public double UnloadingPositionOffset { get; set; }
        [DataMember] public double UnloadReleaseLiftDistance { get; set; } = 1.00;
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
        // To do: [존 분리 스캔] 슬롯 벨리드 윈도우 반폭 비율(윈도우 = 명목 ± SlotPitch×비율, 0.5 미만). Input과 동일.
        [DataMember] public double MappingWindowRatio { get; set; }

        public OutputCassetteConfig()
        {
            SetDefaults();
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx) { SetDefaults(); }

        private void SetDefaults()
        {
            bDryRun = false;
            // 현재 기준: 기본은 NG 카세트 사용(기존 동작 유지).
            UseNgCassette = true;
            LoadingPositionOffset = 0.0;
            UnloadingPositionOffset = 0.0;
            UnloadReleaseLiftDistance = 1.00;
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
            MappingWindowRatio = 0.25;
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
        // 기존 필드: 전체 스택 단일 스캔 구간(레거시). 존별 분리 스캔으로 대체되어 미사용.
        [DataMember] public double MappingStartPosition { get; set; }
        [DataMember] public double MappingEndPosition { get; set; }

        // To do: [존 분리 스캔] Input 카세트와 동일 체계. NG(맨 아래)/Good1/Good2(맨 위) 존마다
        //        MappingStart(그 존 맨 아래 슬롯 앵커, 엔코더 큰쪽) ~ MappingEnd(그 존 맨 위 슬롯 지나 센서 OFF, 엔코더 작은쪽)를
        //        따로 티칭한다. OutputLifterZ 엔코더는 Input과 동일하게 위로 갈수록 감소(실측 티칭값으로 확인됨).
        //        FirstSlot(스타트 포지션) = 그 존 맨 위(01번) 슬롯의 배치(로딩) 절대 위치.
        //        (Good1First=기존 GoodFirstSlotPosition, NgFirst=기존 NGFirstSlotPosition 재사용, Good2First 신규)
        [DataMember] public double NgMappingStartPosition { get; set; }
        [DataMember] public double NgMappingEndPosition { get; set; }
        [DataMember] public double Good1MappingStartPosition { get; set; }
        [DataMember] public double Good1MappingEndPosition { get; set; }
        [DataMember] public double Good2MappingStartPosition { get; set; }
        [DataMember] public double Good2MappingEndPosition { get; set; }
        [DataMember] public double Good2FirstSlotPosition { get; set; }

        public OutputCassetteRecipe()
        {
            SetDefaults();
        }

        // 세 존(Good1/Good2/NG) 실측 맵핑 값을 전부 무효화하는 전체 재생성.
        // 새 매핑 시작(BeginMapping/ScanAll) 같은 의도적 전체 초기화 전용으로만 사용한다.
        public void ResizeSlotPositions(int slotCount)
        {
            int count = Math.Max(0, slotCount);
            GoodSlotPosition = CreateInvalidSlotPositions(count);
            Good2SlotPosition = CreateInvalidSlotPositions(count);
            NGSlotPosition = CreateInvalidSlotPositions(count);
        }

        public void EnsureSlotPositionBuffers(int slotCount)
        {
            int count = Math.Max(0, slotCount);
            // 크기가 다르면 전체 NaN 재생성하던 기존 동작은 단일 슬롯 갱신/조회만으로
            // 다른 존·다른 슬롯의 실측 맵핑 값까지 지웠다. 기존 값을 보존하고 크기 차이 영역만 무효화한다.
            GoodSlotPosition = ResizePreservingSlotPositions(GoodSlotPosition, count);
            Good2SlotPosition = ResizePreservingSlotPositions(Good2SlotPosition, count);
            NGSlotPosition = ResizePreservingSlotPositions(NGSlotPosition, count);
        }

        public void UpdateSlotPosition(TargetCassette cassette, int slotIndex, double position)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException("slotIndex");

            // slotIndex+1 크기로 강제 맞추면 그보다 큰 기존 버퍼가 축소되어 뒤쪽 슬롯 값이 소실된다. 확장만 허용한다.
            int currentCount = GoodSlotPosition != null ? GoodSlotPosition.Length : 0;
            EnsureSlotPositionBuffers(Math.Max(slotIndex + 1, currentCount));
            if (cassette == TargetCassette.Ng)
                NGSlotPosition[slotIndex] = position;
            else if (cassette == TargetCassette.Good2)
                Good2SlotPosition[slotIndex] = position;
            else
                GoodSlotPosition[slotIndex] = position;
        }

        private static double[] CreateInvalidSlotPositions(int count)
        {
            var values = new double[count];
            for (int i = 0; i < count; i++)
                values[i] = double.NaN;
            return values;
        }

        private static double[] ResizePreservingSlotPositions(double[] source, int count)
        {
            if (source != null && source.Length == count)
                return source;

            var values = CreateInvalidSlotPositions(count);
            if (source != null)
            {
                int copyCount = Math.Min(source.Length, count);
                for (int i = 0; i < copyCount; i++)
                    values[i] = source[i];
            }
            return values;
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
            // To do: [존 분리 스캔] 존별 티칭 기본값. 미티칭(0)이면 스캔 시 티칭 오류로 걸러진다.
            NgMappingStartPosition = 0.0;
            NgMappingEndPosition = 0.0;
            Good1MappingStartPosition = 0.0;
            Good1MappingEndPosition = 0.0;
            Good2MappingStartPosition = 0.0;
            Good2MappingEndPosition = 0.0;
            Good2FirstSlotPosition = 0.0;
            GoodSlotPosition = Array.Empty<double>();
            Good2SlotPosition = Array.Empty<double>();
            NGSlotPosition = Array.Empty<double>();
        }
    }

    public class OutputCassetteUnit : BaseUnit<OutputCassetteSetup, OutputCassetteConfig, OutputCassetteRecipe>, IUnitJogController
    {
        internal const string UnloadReleaseLiftTargetName = "OutputCassette.OutputLifterZ.UnloadReleaseLift";
        internal const double MinUnloadReleaseLiftDistanceMm = 0.001;
        internal const double MaxUnloadReleaseLiftDistanceMm = 2.0;

        internal static bool IsSameUnloadReleasePositionKey(double left, double right)
        {
            if (double.IsNaN(left) ||
                double.IsInfinity(left) ||
                double.IsNaN(right) ||
                double.IsInfinity(right))
            {
                return false;
            }

            return Math.Round(left, 3, MidpointRounding.AwayFromZero) ==
                   Math.Round(right, 3, MidpointRounding.AwayFromZero);
        }

        internal static bool IsUnloadReleasePositionMatch(
            double value,
            double expectedTarget,
            double alternateTarget,
            double tolerance)
        {
            if (!IsSameUnloadReleasePositionKey(value, expectedTarget))
                return false;

            double expectedError = Math.Abs(value - expectedTarget);
            double alternateError = Math.Abs(value - alternateTarget);
            return expectedError <= tolerance && expectedError < alternateError;
        }

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
        public CDT320_Machine Machine { get; private set; }

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

        public override void LoadSettings()
        {
            base.LoadSettings();

            // X088은 정상 시 물리 ON, 링 돌출 시 물리 OFF로 들어오는 NC 센서다.
            // 과거 개별 Setup 파일의 NO 설정이 카탈로그 극성을 되덮지 않도록 이 신호만 재확정한다.
            if (BinRingJutCheck != null && BinRingJutCheck.Setup != null)
                BinRingJutCheck.Setup.IsNormallyClosed = false;
        }

        public void BindMachine(CDT320_Machine machine)
        {
            Machine = machine;
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
            return await MoveBinLifterZ(target, speedType, customSpeed, true).ConfigureAwait(false);
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

        // 마지막 Bin Lifter Z 이동 실패 사유 — UI 실패 팝업에 합쳐 표시
        public string LastBinLifterMoveFailureMessage { get; private set; }

        public async Task<int> MoveBinLifterZ(double targetPos, bool bFine = false)
        {
            return await MoveBinLifterZ(targetPos, bFine, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> MoveBinLifterZ(double targetPos, bool bFine, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                double velocity = ResolveBinLifterZDefaultMoveVelocity();
                double acceleration = ResolveCassetteProfileAcceleration(velocity);
                double deceleration = ResolveCassetteProfileDeceleration(velocity);

                return await MoveWithProtrusionWatch(targetPos, velocity, acceleration, deceleration, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try { OutputLifterZ?.Stop(); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                LastBinLifterMoveFailureMessage = "Bin Lifter Z 이동 예외. target=" + targetPos + ", error=" + ex.Message;
                QMC.Common.Log.Write("Main", "MOTION", Name,
                    "Output cassette Z move failed. target=" + targetPos +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        internal async Task<int> MoveBinLifterZForUnloadRelease(
            double targetPos,
            bool bFine,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                string targetReason;
                if (!ValidateBinLifterZTargetPosition(targetPos, out targetReason))
                {
                    LastBinLifterMoveFailureMessage = targetReason;
                    QMC.Common.Log.Write("Main", "MOTION", Name, targetReason + " - Failed");
                    return -1;
                }

                string interlockReason;
                if (!OutputCassetteInterlockRules.VerifyUnloadReleaseLift(
                    Machine,
                    targetPos,
                    out interlockReason))
                {
                    LastBinLifterMoveFailureMessage = interlockReason;
                    QMC.Common.Log.Write("Main", "INTERLOCK", Name, interlockReason + " - Blocked");
                    return -11;
                }

                double velocity = ResolveBinLifterZDefaultMoveVelocity();
                using (MotionGuardRuntime.BeginAxisTeachingMove(
                    OutputLifterZ,
                    targetPos,
                    UnloadReleaseLiftTargetName))
                {
                    return await MoveWithProtrusionWatch(
                        targetPos,
                        velocity,
                        ResolveCassetteProfileAcceleration(velocity),
                        ResolveCassetteProfileDeceleration(velocity),
                        ct,
                        forceMove: true,
                        allowProtrusionForUnloadRelease: true).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                try { OutputLifterZ?.Stop(); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                LastBinLifterMoveFailureMessage =
                    "Bin Lifter Z unload release 이동 예외. target=" +
                    targetPos + ", error=" + ex.Message;
                QMC.Common.Log.Write(
                    "Main",
                    "MOTION",
                    Name,
                    LastBinLifterMoveFailureMessage + " - Failed");
                return -1;
            }
        }

        public async Task<int> MoveBinLifterZ(double targetPos, JogSpeedType speedType, double customSpeed = 0)
        {
            return await MoveBinLifterZ(targetPos, speedType, customSpeed, false).ConfigureAwait(false);
        }

        private async Task<int> MoveBinLifterZ(double targetPos, JogSpeedType speedType, double customSpeed, bool forceMove)
        {
            try
            {
                double velocity = UnitJogVelocityResolver.Resolve(OutputLifterZ, speedType, customSpeed);
                if (velocity <= 0.0)
                    velocity = OutputLifterZ != null && OutputLifterZ.Config != null ? OutputLifterZ.Config.JogFineVelocity : 1.0;

                return await MoveWithProtrusionWatch(
                    targetPos,
                    velocity,
                    UnitJogVelocityResolver.ResolveAcceleration(OutputLifterZ),
                    UnitJogVelocityResolver.ResolveDeceleration(OutputLifterZ),
                    CancellationToken.None,
                    forceMove).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LastBinLifterMoveFailureMessage = "Bin Lifter Z 조그 속도 이동 예외. target=" + targetPos + ", error=" + ex.Message;
                QMC.Common.Log.Write("Main", "MOTION", Name,
                    "Output Cassette Z 조그 속도 이동이 실패했습니다. target=" + targetPos +
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

        public Task<int> MoveBinLifterZToTeachingPosition(string positionName, JogSpeedType speedType, double customSpeed)
        {
            return MoveBinLifterZ(GetTeachingPosition(positionName), speedType, customSpeed);
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

        // To do: [언로드 오프셋] 피더가 처진 bin을 들고 카세트에 진입할 때 간섭하지 않도록
        //        슬롯 위치 + Config.UnloadingPositionOffset 로 이동한다. (Input UnloadToCassette와 동일 개념)
        public Task<int> MoveToBinCassetteUnloadOffsetPosition(TargetCassette cassette, int slotIndex, bool bFine = false)
        {
            return MoveBinLifterZ(CalculateBinCassetteSlotTargetPosition(cassette, slotIndex) + ResolveUnloadingPositionOffset(), bFine);
        }

        public double ResolveUnloadingPositionOffset()
        {
            return Config != null ? Config.UnloadingPositionOffset : 0.0;
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

                int waitCode = await WaitBinLifterZMoveDoneInPosition(OutputLifterZ.CommandPosition, timeoutMs, ct).ConfigureAwait(false);
                return waitCode == 0;
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

        // 기존 조건: AxisMoveWaitResult 반환 + settle 재확인 — 현재 기준: int(0=완료) 반환(R3).
        //           ScanSettleTimeMs는 매핑 스캔 진동 안정용 물리 대기로만 보존한다(C7).
        public async Task<int> WaitBinLifterZMoveDoneInPosition(double targetPos, int timeoutMs)
        {
            return await WaitBinLifterZMoveDoneInPosition(targetPos, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> WaitBinLifterZMoveDoneInPosition(double targetPos, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int timeout = timeoutMs > 0 ? timeoutMs : OutputLifterZ.Setup.MoveTimeoutMs;
                int waitCode = await OutputLifterZ.WaitMoveCompleteAsync(targetPos, timeout, ct).ConfigureAwait(false);
                if (waitCode != 0)
                    return waitCode;

                int settleMs = Config != null ? Config.ScanSettleTimeMs : 0;
                if (settleMs > 0)
                    await Task.Delay(settleMs, ct).ConfigureAwait(false);

                return 0;
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
                int waitCode = await WaitBinLifterZMoveDoneInPosition(target, timeout, ct).ConfigureAwait(false);
                return waitCode == 0;
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
        // 기존 티칭: 전체 스택 단일 스캔 구간(레거시, 미사용).
        public void TeachBinLifterZMappingStartPosition() { Recipe.MappingStartPosition = OutputLifterZ.ActualPosition; }
        public void TeachBinLifterZMappingEndPosition() { Recipe.MappingEndPosition = OutputLifterZ.ActualPosition; }

        // To do: [존 분리 스캔] 존별 스캔 구간 티칭. Start=그 존 맨 아래 슬롯 센서 중앙, End=그 존 맨 위 지나 센서 OFF 지점.
        public void TeachBinLifterZZoneMappingStartPosition(TargetCassette cassette)
        {
            if (cassette == TargetCassette.Ng) Recipe.NgMappingStartPosition = OutputLifterZ.ActualPosition;
            else if (cassette == TargetCassette.Good2) Recipe.Good2MappingStartPosition = OutputLifterZ.ActualPosition;
            else Recipe.Good1MappingStartPosition = OutputLifterZ.ActualPosition;
        }

        public void TeachBinLifterZZoneMappingEndPosition(TargetCassette cassette)
        {
            if (cassette == TargetCassette.Ng) Recipe.NgMappingEndPosition = OutputLifterZ.ActualPosition;
            else if (cassette == TargetCassette.Good2) Recipe.Good2MappingEndPosition = OutputLifterZ.ActualPosition;
            else Recipe.Good1MappingEndPosition = OutputLifterZ.ActualPosition;
        }

        public void TeachBinLifterZSlotBasePosition()
        {
            TeachBinLifterZFirstSlotPosition(ResolveActiveCassette());
        }

        public void TeachBinLifterZFirstSlotPosition(TargetCassette cassette)
        {
            SetFirstSlotPosition(cassette, OutputLifterZ.ActualPosition);
        }

        // To do: [존 분리 스캔] 존별 스캔 앵커/끝 조회. 엔코더 위로 갈수록 증가 → End > Start 여야 한다.
        public double ResolveZoneMappingStartPosition(TargetCassette cassette)
        {
            if (cassette == TargetCassette.Ng) return Recipe.NgMappingStartPosition;
            if (cassette == TargetCassette.Good2) return Recipe.Good2MappingStartPosition;
            return Recipe.Good1MappingStartPosition;
        }

        public double ResolveZoneMappingEndPosition(TargetCassette cassette)
        {
            if (cassette == TargetCassette.Ng) return Recipe.NgMappingEndPosition;
            if (cassette == TargetCassette.Good2) return Recipe.Good2MappingEndPosition;
            return Recipe.Good1MappingEndPosition;
        }

        // To do: [존 분리 스캔] 존 명목 위치. 슬롯 번호는 Input과 동일하게 local 0 = 존 맨 위(01번).
        // 기존 가정: 엔코더 위로 증가(+pitch) - 실제 티칭값(NG 아래=592 > Good1 위=248)으로 반증됨.
        // 현재 기준: OutputLifterZ도 Input과 동일하게 위로 갈수록 엔코더 감소.
        //           pos(local) = ZoneStart(맨 아래 슬롯) - pitch × (N-1-local). 맨 위(local 0) = Start - 12p.
        public double CalculateZoneSlotNominalPosition(TargetCassette cassette, int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            int lastIndex = Math.Max(0, Config.SlotCount - 1);
            return ResolveZoneMappingStartPosition(cassette) - (Config.SlotPitch * (lastIndex - slotIndex));
        }

        // To do: [존 분리 스캔] 존별 배치(로딩) 오프셋 = 존 FirstSlot(맨 위 01번 배치 위치 티칭) - 그 슬롯 검출 위치(실측 우선).
        public double ResolveZoneLoadingOffset(TargetCassette cassette)
        {
            double firstSlot = GetFirstSlotPosition(cassette);
            if (firstSlot <= 0.0)
                return 0.0;

            double referenceDetect = GetMappedSlotPosition(cassette, 0);
            if (double.IsNaN(referenceDetect))
                referenceDetect = CalculateZoneSlotNominalPosition(cassette, 0);

            return firstSlot - referenceDetect;
        }

        public double CalculateCassetteSlotTargetPosition(int slotIndex)
        {
            return CalculateBinCassetteSlotTargetPosition(ResolveActiveCassette(), slotIndex);
        }

        // To do: [존 분리 스캔] 배치 목표 = 검출 위치(실측 우선, 없으면 존 명목) + 존별 배치 오프셋.
        //        기존 공식(FirstSlot + pitch×slotIndex, slot0=맨 아래)은 번호 방향이 Input과 반대라 폐기.
        public double CalculateBinCassetteSlotTargetPosition(TargetCassette cassette, int slotIndex)
        {
            ValidateSlotIndex(slotIndex);

            double detect = GetMappedSlotPosition(cassette, slotIndex);
            if (double.IsNaN(detect))
                detect = CalculateZoneSlotNominalPosition(cassette, slotIndex);

            return detect + ResolveZoneLoadingOffset(cassette);
        }

        // 수동(PREV/NEXT/우클릭/더블클릭) 슬롯 물리 이동 전용 목표 계산.
        // 미티칭(FirstSlot/MappingStart)/맵핑 이상 시 조용히 offset 0이나 명목값으로 대체하지 않고
        // 사유와 함께 차단한다. Auto 경로의 CalculateBinCassetteSlotTargetPosition 동작은 변경하지 않는다.
        public CassetteSlotTargetResolveResult ResolveManualBinCassetteSlotTarget(TargetCassette cassette, int slotIndex)
        {
            string roleName = cassette == TargetCassette.Ng ? "NG" :
                              cassette == TargetCassette.Good2 ? "GOOD2" : "GOOD1";
            try
            {
                if (Config == null || Recipe == null)
                    return CassetteSlotTargetResolveResult.Fail(roleName, slotIndex, "카세트 Config/Recipe 데이터가 없습니다.");

                if (Config.SlotCount <= 0)
                    return CassetteSlotTargetResolveResult.Fail(roleName, slotIndex,
                        "카세트 SlotCount 설정이 올바르지 않습니다. slotCount=" + Config.SlotCount);

                if (slotIndex < 0 || slotIndex >= Config.SlotCount)
                    return CassetteSlotTargetResolveResult.Fail(roleName, slotIndex,
                        "슬롯 번호가 카세트 범위를 벗어났습니다. slot=" + (slotIndex + 1) + ", slotCount=" + Config.SlotCount);

                if (Config.SlotPitch <= 0.0)
                    return CassetteSlotTargetResolveResult.Fail(roleName, slotIndex,
                        "SlotPitch가 설정되지 않아 슬롯 목표를 계산할 수 없습니다. slotPitch=" + Config.SlotPitch);

                if (cassette == TargetCassette.Good2 && Config.SelectedCassetteLevel < 2)
                    return CassetteSlotTargetResolveResult.Fail(roleName, slotIndex,
                        "GOOD 카세트가 2단 구성이 아닌데 GOOD2 슬롯 이동이 요청되었습니다.");

                double firstSlot = GetFirstSlotPosition(cassette);
                if (firstSlot <= 0.0)
                    return CassetteSlotTargetResolveResult.Fail(roleName, slotIndex,
                        roleName + " First Slot Position이 티칭되지 않았습니다. 임의 오프셋으로 대체하지 않고 이동을 차단합니다. first=" + firstSlot);

                double anchor = ResolveZoneMappingStartPosition(cassette);
                if (anchor <= 0.0)
                    return CassetteSlotTargetResolveResult.Fail(roleName, slotIndex,
                        roleName + " Mapping Start Position이 티칭되지 않아 명목 슬롯 위치를 계산할 수 없습니다. start=" + anchor);

                Recipe.EnsureSlotPositionBuffers(Config.SlotCount);
                bool mapped = !double.IsNaN(GetMappedSlotPosition(cassette, slotIndex));

                // 해당 존 전체 목표를 계산해 NaN/Infinity와 단조 증가(슬롯 번호 증가 → 엔코더 증가)를 검증한다.
                double previous = double.NaN;
                double slot01 = double.NaN;
                double target = double.NaN;
                for (int i = 0; i < Config.SlotCount; i++)
                {
                    double value = CalculateBinCassetteSlotTargetPosition(cassette, i);
                    if (double.IsNaN(value) || double.IsInfinity(value))
                        return CassetteSlotTargetResolveResult.Fail(roleName, slotIndex,
                            roleName + " SLOT " + (i + 1).ToString("00") + " 목표 계산 값이 유효하지 않습니다. value=" + value);

                    if (i > 0 && value <= previous)
                        return CassetteSlotTargetResolveResult.Fail(roleName, slotIndex,
                            roleName + " 슬롯 목표가 단조 증가하지 않습니다. 맵핑/티칭 값을 확인하십시오. slot=" +
                            (i + 1).ToString("00") + ", prev=" + previous.ToString("F3") + ", value=" + value.ToString("F3"));

                    previous = value;
                    if (i == 0)
                        slot01 = value;
                    if (i == slotIndex)
                        target = value;
                }

                string softLimitReason;
                if (!ValidateBinLifterZTargetPosition(target, out softLimitReason))
                    return CassetteSlotTargetResolveResult.Fail(roleName, slotIndex,
                        roleName + " 슬롯 목표가 축 이동 허용 범위를 벗어났습니다. " + softLimitReason);

                return new CassetteSlotTargetResolveResult
                {
                    IsValid = true,
                    FailureReason = string.Empty,
                    RoleName = roleName,
                    SlotIndex = slotIndex,
                    FirstSlotPosition = firstSlot,
                    SlotPitch = Config.SlotPitch,
                    TargetPosition = target,
                    Slot01Position = slot01,
                    TargetSource = mapped ? CassetteSlotTargetSource.Mapped : CassetteSlotTargetSource.Nominal
                };
            }
            catch (Exception ex)
            {
                return CassetteSlotTargetResolveResult.Fail(roleName, slotIndex,
                    "슬롯 목표 계산 중 예외가 발생했습니다: " + ex.Message);
            }
        }

        // OutputLifterZ 소프트리밋 검사(Input의 ValidateWaferLifterZTargetPosition과 동일 계약).
        private bool ValidateBinLifterZTargetPosition(double targetPos, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (OutputLifterZ == null || OutputLifterZ.Setup == null)
                {
                    reason = "OutputLifterZ axis/setup is null. target=" + targetPos;
                    return false;
                }
                if (!OutputLifterZ.Setup.SoftLimitEnabled)
                    return true;

                bool inRange = targetPos <= OutputLifterZ.Setup.SoftLimitPlus &&
                               targetPos >= OutputLifterZ.Setup.SoftLimitMinus;
                if (inRange)
                    return true;

                reason = "OutputLifterZ target is out of soft limit. target=" + targetPos +
                         ", softMinus=" + OutputLifterZ.Setup.SoftLimitMinus +
                         ", softPlus=" + OutputLifterZ.Setup.SoftLimitPlus;
                return false;
            }
            catch (Exception ex)
            {
                reason = "OutputLifterZ target validation failed: " + ex.Message + ". target=" + targetPos;
                return false;
            }
            finally
            {
            }
        }

        public bool ValidateBinLifterZTeachingComplete()
        {
            string reason;
            return ValidateBinLifterZTeachingComplete(out reason);
        }

        // To do: [존 분리 스캔] 대상 존(GOOD/NG)만 티칭 검증 - GOOD 동작 시 NG 미티칭으로 막히지 않도록 분리.
        public bool ValidateBinLifterZZoneTeachingComplete(bool ngTarget, out string reason)
        {
            reason = string.Empty;

            if (Config == null || Recipe == null)
            {
                reason = "Output cassette config/recipe is null.";
                return false;
            }

            if (Config.SlotCount <= 0 || Config.SlotPitch <= 0.0)
            {
                reason = "SlotCount/SlotPitch is invalid. SlotCount=" + Config.SlotCount + ", SlotPitch=" + Config.SlotPitch;
                return false;
            }

            // 현재 기준: 엔코더 위로 갈수록 감소 → End(맨 위 지나)는 Start(맨 아래)보다 작아야 한다.
            if (ngTarget)
            {
                if (Recipe.NgMappingStartPosition <= 0.0 || Recipe.NgMappingEndPosition <= 0.0 ||
                    Recipe.NgMappingEndPosition >= Recipe.NgMappingStartPosition)
                {
                    reason = "NG zone mapping start/end teaching is invalid. start=" + Recipe.NgMappingStartPosition +
                             ", end=" + Recipe.NgMappingEndPosition + " (end must be above start = smaller encoder).";
                    return false;
                }

                return true;
            }

            if (Recipe.Good1MappingStartPosition <= 0.0 || Recipe.Good1MappingEndPosition <= 0.0 ||
                Recipe.Good1MappingEndPosition >= Recipe.Good1MappingStartPosition)
            {
                reason = "Good1 zone mapping start/end teaching is invalid. start=" + Recipe.Good1MappingStartPosition +
                         ", end=" + Recipe.Good1MappingEndPosition + " (end must be above start = smaller encoder).";
                return false;
            }

            if (Config.SelectedCassetteLevel >= 2 &&
                (Recipe.Good2MappingStartPosition <= 0.0 || Recipe.Good2MappingEndPosition <= 0.0 ||
                 Recipe.Good2MappingEndPosition >= Recipe.Good2MappingStartPosition))
            {
                reason = "Good2 zone mapping start/end teaching is invalid. start=" + Recipe.Good2MappingStartPosition +
                         ", end=" + Recipe.Good2MappingEndPosition + " (end must be above start = smaller encoder).";
                return false;
            }

            return true;
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

            // 기존 검증: 전체 스택 단일 스캔 + FirstSlot 파생 격자 기준(레거시). 존 분리 스캔으로 대체.
            // To do: [존 분리 스캔] 존별 Start/End 티칭 유효성 검증. 엔코더 위로 증가 → End > Start.
            //        스택 순서(아래→위): NG → Good1 → Good2(2단 구성 시).
            //        (전체 검증 - GOOD/NG 대상별 검증은 ValidateBinLifterZZoneTeachingComplete 사용)
            if (Recipe.NgMappingStartPosition <= 0.0 || Recipe.NgMappingEndPosition <= 0.0)
            {
                reason = "NG zone mapping start/end is not taught. start=" + Recipe.NgMappingStartPosition +
                         ", end=" + Recipe.NgMappingEndPosition;
                return false;
            }

            if (Recipe.NgMappingEndPosition >= Recipe.NgMappingStartPosition)
            {
                reason = "NG zone MappingEnd must be above(smaller than) MappingStart. start=" +
                         Recipe.NgMappingStartPosition + ", end=" + Recipe.NgMappingEndPosition;
                return false;
            }

            if (Recipe.Good1MappingStartPosition <= 0.0 || Recipe.Good1MappingEndPosition <= 0.0)
            {
                reason = "Good1 zone mapping start/end is not taught. start=" + Recipe.Good1MappingStartPosition +
                         ", end=" + Recipe.Good1MappingEndPosition;
                return false;
            }

            if (Recipe.Good1MappingEndPosition >= Recipe.Good1MappingStartPosition)
            {
                reason = "Good1 zone MappingEnd must be above(smaller than) MappingStart. start=" +
                         Recipe.Good1MappingStartPosition + ", end=" + Recipe.Good1MappingEndPosition;
                return false;
            }

            // 현재 기준: 엔코더 아래=큰 값 → NG(맨 아래) 존 값이 Good1보다 커야 한다.
            if (Recipe.NgMappingEndPosition <= Recipe.Good1MappingStartPosition)
            {
                reason = "NG zone must be below Good1 zone (larger encoder). NgEnd=" + Recipe.NgMappingEndPosition +
                         ", Good1Start=" + Recipe.Good1MappingStartPosition;
                return false;
            }

            if (Config.SelectedCassetteLevel >= 2)
            {
                if (Recipe.Good2MappingStartPosition <= 0.0 || Recipe.Good2MappingEndPosition <= 0.0)
                {
                    reason = "Good2 zone mapping start/end is not taught. start=" + Recipe.Good2MappingStartPosition +
                             ", end=" + Recipe.Good2MappingEndPosition;
                    return false;
                }

                if (Recipe.Good2MappingEndPosition >= Recipe.Good2MappingStartPosition)
                {
                    reason = "Good2 zone MappingEnd must be above(smaller than) MappingStart. start=" +
                             Recipe.Good2MappingStartPosition + ", end=" + Recipe.Good2MappingEndPosition;
                    return false;
                }

                if (Recipe.Good1MappingEndPosition <= Recipe.Good2MappingStartPosition)
                {
                    reason = "Good1 zone must be below Good2 zone (larger encoder). Good1End=" + Recipe.Good1MappingEndPosition +
                             ", Good2Start=" + Recipe.Good2MappingStartPosition;
                    return false;
                }
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
        public bool IsBinProtrusionDetected()
        {
            return !IsDryRunInput(BinRingJutCheck) && BinRingJutCheck.IsOn;
        }
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

        // To do: [존 분리 스캔] 단일 존(GOOD1/GOOD2/NG 선택) 스캔. Input과 동일한 윈도우 실시간 판정.
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

            return await CollectBinSlotOccupancyAsync(new[] { cassette }, ct).ConfigureAwait(false);
        }

        // To do: [존 분리 스캔] GOOD 선택 스캔 = Good1 + (2단 구성 시) Good2를 아래→위 순서로 세그먼트 스캔.
        public async Task<bool> ScanGoodCassettesAsync(CancellationToken ct)
        {
            if (IsOutputCassetteHardwareBypassed())
            {
                BuildSimulatedBinMap(TargetCassette.Good1, Config.SlotCount, Config.SlotPitch);
                if (Config.SelectedCassetteLevel >= 2)
                    BuildSimulatedBinMap(TargetCassette.Good2, Config.SlotCount, Config.SlotPitch);
                return true;
            }

            var zones = Config.SelectedCassetteLevel >= 2
                ? new[] { TargetCassette.Good1, TargetCassette.Good2 }
                : new[] { TargetCassette.Good1 };
            return await CollectBinSlotOccupancyAsync(zones, ct).ConfigureAwait(false);
        }

        // To do: [존 분리 스캔] NG 선택 스캔.
        public async Task<bool> ScanNgCassetteAsync(CancellationToken ct)
        {
            if (IsOutputCassetteHardwareBypassed())
            {
                BuildSimulatedBinMap(TargetCassette.Ng, Config.SlotCount, Config.SlotPitch);
                return true;
            }

            return await CollectBinSlotOccupancyAsync(new[] { TargetCassette.Ng }, ct).ConfigureAwait(false);
        }

        // To do: [드라이런 데이터 생성] 시퀀스 레벨 하드웨어 바이패스(BypassHardware/GlobalDryRun 포함)에서 호출하는 시뮬 빈 맵 생성.
        //        유닛 내부 IsOutputCassetteHardwareBypassed()는 Config.bDryRun/시뮬레이션 모드만 판정하므로
        //        GENERAL 드라이런에서는 시퀀스가 이 메서드로 직접 시뮬 맵을 만들어야 한다. (Input의 BuildSimulatedWaferMap와 동일 역할)
        public void BuildSimulatedBinMaps(bool ngTarget)
        {
            if (ngTarget)
            {
                BuildSimulatedBinMap(TargetCassette.Ng, Config.SlotCount, Config.SlotPitch);
                return;
            }

            BuildSimulatedBinMap(TargetCassette.Good1, Config.SlotCount, Config.SlotPitch);
            if (Config.SelectedCassetteLevel >= 2)
                BuildSimulatedBinMap(TargetCassette.Good2, Config.SlotCount, Config.SlotPitch);
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

                // To do: [존 분리 스캔] 전체 스캔 = NG(맨 아래) → Good1 → Good2(2단 구성 시) 순 세그먼트.
                return await CollectBinSlotOccupancyAsync(BuildAllScanZones(), ct).ConfigureAwait(false);
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

                // To do: [존 분리 스캔] 각 존이 자기 시작점으로 접근하므로 현재 위치 무관. 전체 존 순차 스캔.
                return await CollectBinSlotOccupancyAsync(BuildAllScanZones(), ct).ConfigureAwait(false);
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

        // To do: [존 분리 스캔] 전체 스캔 존 목록(아래→위): NG → Good1 → Good2(2단 구성 시).
        private TargetCassette[] BuildAllScanZones()
        {
            return Config.SelectedCassetteLevel >= 2
                ? new[] { TargetCassette.Ng, TargetCassette.Good1, TargetCassette.Good2 }
                : new[] { TargetCassette.Ng, TargetCassette.Good1 };
        }

        // To do: [존 분리 스캔] Input과 동일한 윈도우 실시간 판정 + 존 세그먼트 스캔(엔코더 위로 증가 방향).
        //        존마다: 시작 = ZoneStart(맨 아래 슬롯 앵커) - 반 피치 → 끝 = ZoneEnd(맨 위 지나 센서 OFF 지점).
        //        매 샘플 "현재 센서 ON + 그 존 벨리드 윈도우"면 즉시 점유(이전 상태/엣지/디바운스 없음).
        //        윈도우 밖 ON은 무시 + 스트레치 로그. 존 사이 구간은 판정 없이 이동만 한다.
        private async Task<bool> CollectBinSlotOccupancyAsync(IReadOnlyList<TargetCassette> zones, CancellationToken ct)
        {
            double originalAcc = 0.0;
            double originalDec = 0.0;
            bool restoreScanProfile = false;
            try
            {
                ct.ThrowIfCancellationRequested();

                int slotCount = Config.SlotCount;
                double pitch = Config.SlotPitch;
                double ratio = Config.MappingWindowRatio;
                if (ratio <= 0.0 || ratio >= 0.5)
                    ratio = 0.25;
                double windowHalf = pitch > 0.0 ? pitch * ratio : 0.0;
                if (slotCount <= 0 || windowHalf <= 0.0)
                    return FailMappingScanBool("OUT-CST-MAP-WINDOW", "Slot valid window is invalid. pitch=" + FormatPosition(pitch));

                Log.Write("Main", "SYSTEM", "OutputCassetteUnit",
                    "Bin mapping scan window. halfWidth=" + FormatPosition(windowHalf) +
                    " (ratio=" + FormatPosition(ratio) + ") - Ok");

                double scanVelocity = ResolveBinLifterZScanVelocity();
                double scanAcceleration = ResolveCassetteProfileAcceleration(scanVelocity);
                double scanDeceleration = ResolveCassetteProfileDeceleration(scanVelocity);
                if (OutputLifterZ.Config != null)
                {
                    originalAcc = OutputLifterZ.Config.GetRawAcceleration();
                    originalDec = OutputLifterZ.Config.GetRawDeceleration();
                    OutputLifterZ.Config.Acceleration = scanAcceleration;
                    OutputLifterZ.Config.Deceleration = scanDeceleration;
                    restoreScanProfile = true;
                }

                Recipe.EnsureSlotPositionBuffers(slotCount);

                foreach (TargetCassette zone in zones)
                {
                    ct.ThrowIfCancellationRequested();

                    double zoneStart = ResolveZoneMappingStartPosition(zone);
                    double zoneEnd = ResolveZoneMappingEndPosition(zone);
                    // 현재 기준: 엔코더 위로 갈수록 감소(Input과 동일) → End(위) < Start(아래).
                    if (zoneStart <= 0.0 || zoneEnd <= 0.0 || zoneEnd >= zoneStart)
                        return FailMappingScanBool("OUT-CST-MAP-ZONE-TEACH",
                            "Zone mapping start/end teaching is invalid. zone=" + zone +
                            ", start=" + FormatPosition(zoneStart) + ", end=" + FormatPosition(zoneEnd) +
                            " (end must be above start = smaller encoder).");

                    var centers = new double[slotCount];
                    for (int i = 0; i < slotCount; i++)
                        centers[i] = CalculateZoneSlotNominalPosition(zone, i);

                    // 커버리지: ZoneEnd가 그 존 맨 위 슬롯(local 0) 윈도우를 지나야(더 작아야) 맨 위 웨이퍼가 검출된다.
                    double topSlot = centers[0];
                    if (zoneEnd > topSlot - (pitch * 0.5))
                        Log.Write("Main", "SYSTEM", "OutputCassetteUnit",
                            "Bin mapping scan end does not cover the top slot. zone=" + zone +
                            ", scanEnd=" + FormatPosition(zoneEnd) +
                            ", topSlot(local01)=" + FormatPosition(topSlot) +
                            ", requiredEnd<=" + FormatPosition(topSlot - pitch * 0.5) +
                            ". 해당 존 MappingEnd 티칭을 맨 위 슬롯보다 위로 다시 잡아야 합니다. - Check");

                    // 세그먼트 시작 = 존 앵커(맨 아래 슬롯) 반 피치 아래(엔코더 +, 윈도우 밖) → 시작 센서 ON도 윈도우 통과로 자연 점유.
                    double segmentStart = zoneStart + (pitch * 0.5);
                    int approach = await MoveBinLifterWatchedForMappingAsync(segmentStart, scanVelocity, "zone " + zone + " scan start", ct).ConfigureAwait(false);
                    if (approach != 0)
                        return false;

                    var occupied = new bool[slotCount];
                    var onMin = new double[slotCount];
                    var onMax = new double[slotCount];
                    for (int i = 0; i < slotCount; i++)
                    {
                        onMin[i] = double.NaN;
                        onMax[i] = double.NaN;
                    }

                    bool invalidZoneOn = false;
                    double invalidOnMin = double.NaN;
                    double invalidOnMax = double.NaN;

                    Task<int> moveTask = OutputLifterZ.MoveAbsoluteAsync(zoneEnd, scanVelocity);
                    while (!moveTask.IsCompleted)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (IsBinProtrusionDetected())
                        {
                            OutputLifterZ.EStop();
                            return FailMappingScanBool("OUT-CST-MAP-PROTRUSION", "Bin protrusion detected during mapping scan.");
                        }

                        ProcessBinMappingScanSample(OutputLifterZ.ActualPosition, BinMappingSensor.IsOn,
                            centers, windowHalf, occupied, onMin, onMax,
                            ref invalidZoneOn, ref invalidOnMin, ref invalidOnMax);

                        await Task.Delay(5, ct).ConfigureAwait(false);
                    }

                    int moveResult = await moveTask.ConfigureAwait(false);
                    if (moveResult != 0 || OutputLifterZ.IsAlarm)
                        return FailMappingScanBool("OUT-CST-MAP-END", "OutputLifterZ move failed during zone " + zone + " mapping scan.");

                    ProcessBinMappingScanSample(OutputLifterZ.ActualPosition, BinMappingSensor.IsOn,
                        centers, windowHalf, occupied, onMin, onMax,
                        ref invalidZoneOn, ref invalidOnMin, ref invalidOnMax);
                    FlushBinInvalidZoneStretchLog(ref invalidZoneOn, ref invalidOnMin, ref invalidOnMax);

                    // 이동 완료는 moveTask가 보장 — 여기서는 ScanSettleTimeMs 안정 대기 목적만 유지(C7).
                    int settleCode = await WaitBinLifterZMoveDoneInPosition(zoneEnd, OutputLifterZ.Setup.MoveTimeoutMs, ct).ConfigureAwait(false);
                    if (settleCode != 0)
                        return FailMappingScanBool(
                            "OUT-CST-MAP-END-MOVE",
                            "OutputLifterZ zone " + zone + " mapping end move/in-position wait failed. waitCode=" + settleCode +
                            ", reason=" + (OutputLifterZ.LastMotionFailureMessage ?? string.Empty));

                    // 존 결과 확정: 점유 슬롯은 ON 구간 중심을 실측으로 저장 + 슬롯별 로그.
                    var positions = new double[slotCount];
                    for (int i = 0; i < slotCount; i++)
                    {
                        positions[i] = double.NaN;
                        if (!occupied[i])
                            continue;

                        positions[i] = (onMin[i] + onMax[i]) * 0.5;
                        Log.Write("Main", "SYSTEM", "OutputCassetteUnit",
                            "Bin mapping slot occupied. zone=" + zone + ", slot=" + (i + 1).ToString("00") +
                            ", measured=" + FormatPosition(positions[i]) +
                            ", nominal=" + FormatPosition(centers[i]) +
                            ", error=" + FormatPosition(Math.Abs(positions[i] - centers[i])) + " - Ok");
                    }

                    ApplyBinMappingResult(zone, occupied, positions);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                try { OutputLifterZ?.Stop(); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                return FailMappingScanBool("OUT-CST-MAP-COLLECT", "Bin slot occupancy scan failed: " + ex.Message);
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

        // To do: [존 분리 스캔] 세그먼트 접근 이동(판정 없이 프로트루전/알람 감시만).
        private async Task<int> MoveBinLifterWatchedForMappingAsync(double target, double velocity, string moveName, CancellationToken ct)
        {
            Task<int> moveTask = OutputLifterZ.MoveAbsoluteAsync(target, velocity);
            while (!moveTask.IsCompleted)
            {
                ct.ThrowIfCancellationRequested();
                if (IsBinProtrusionDetected())
                {
                    OutputLifterZ.EStop();
                    return FailMappingScan("OUT-CST-MAP-PROTRUSION", "Bin protrusion detected while moving to " + moveName + ".");
                }

                await Task.Delay(5, ct).ConfigureAwait(false);
            }

            int moveResult = await moveTask.ConfigureAwait(false);
            if (moveResult != 0 || OutputLifterZ.IsAlarm)
                return FailMappingScan("OUT-CST-MAP-MOVE", "OutputLifterZ move failed. moveName=" + moveName + ", target=" + FormatPosition(target));

            // 이동 완료는 moveTask가 보장 — 여기서는 ScanSettleTimeMs 안정 대기 목적만 유지(C7).
            int settleCode = await WaitBinLifterZMoveDoneInPosition(target, OutputLifterZ.Setup.MoveTimeoutMs, ct).ConfigureAwait(false);
            if (settleCode != 0)
                return FailMappingScan(
                    "OUT-CST-MAP-MOVE",
                    "OutputLifterZ " + moveName + " move/in-position wait failed. waitCode=" + settleCode +
                    ", reason=" + (OutputLifterZ.LastMotionFailureMessage ?? string.Empty));

            return 0;
        }

        // To do: [존 분리 스캔] 샘플 1건 처리 - 현재 상태만 본다. ON + 윈도우면 즉시 점유(OFF/엣지/디바운스 없음).
        private void ProcessBinMappingScanSample(
            double position,
            bool sensorOn,
            double[] centers,
            double windowHalf,
            bool[] occupied,
            double[] onMin,
            double[] onMax,
            ref bool invalidZoneOn,
            ref double invalidOnMin,
            ref double invalidOnMax)
        {
            if (!sensorOn)
            {
                FlushBinInvalidZoneStretchLog(ref invalidZoneOn, ref invalidOnMin, ref invalidOnMax);
                return;
            }

            int slot = -1;
            double bestError = double.MaxValue;
            for (int i = 0; i < centers.Length; i++)
            {
                double error = Math.Abs(position - centers[i]);
                if (error <= windowHalf && error < bestError)
                {
                    bestError = error;
                    slot = i;
                }
            }

            if (slot < 0)
            {
                if (double.IsNaN(invalidOnMin) || position < invalidOnMin)
                    invalidOnMin = position;
                if (double.IsNaN(invalidOnMax) || position > invalidOnMax)
                    invalidOnMax = position;
                invalidZoneOn = true;
                return;
            }

            FlushBinInvalidZoneStretchLog(ref invalidZoneOn, ref invalidOnMin, ref invalidOnMax);

            if (double.IsNaN(onMin[slot]) || position < onMin[slot])
                onMin[slot] = position;
            if (double.IsNaN(onMax[slot]) || position > onMax[slot])
                onMax[slot] = position;

            occupied[slot] = true;
        }

        // To do: [존 분리 스캔] invalid ON 스트레치 로그(시작~끝~중심) - 캘리브레이션용 실측 데이터.
        private void FlushBinInvalidZoneStretchLog(ref bool invalidZoneOn, ref double invalidOnMin, ref double invalidOnMax)
        {
            if (!invalidZoneOn)
                return;

            if (!double.IsNaN(invalidOnMin) && !double.IsNaN(invalidOnMax))
                Log.Write("Main", "SYSTEM", "OutputCassetteUnit",
                    "Bin mapping sensor ON in invalid zone (ignored). from=" + FormatPosition(invalidOnMin) +
                    ", to=" + FormatPosition(invalidOnMax) +
                    ", center=" + FormatPosition((invalidOnMin + invalidOnMax) * 0.5) +
                    ", span=" + FormatPosition(invalidOnMax - invalidOnMin) + " - Check");

            invalidZoneOn = false;
            invalidOnMin = double.NaN;
            invalidOnMax = double.NaN;
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

                // 첫장은 무조건 감지가됨. 
                //if (previous)
                //    return FailMappingScanList("OUT-CST-MAP-SENSOR-ON", "Mapping sensor is ON at mapping start. Check mapping start position.");

                double scanVelocity = ResolveBinLifterZScanVelocity();
                double scanAcceleration = ResolveCassetteProfileAcceleration(scanVelocity);
                double scanDeceleration = ResolveCassetteProfileDeceleration(scanVelocity);

                if (OutputLifterZ.Config != null)
                {
                    originalAcc = OutputLifterZ.Config.GetRawAcceleration();
                    originalDec = OutputLifterZ.Config.GetRawDeceleration();
                    OutputLifterZ.Config.Acceleration = scanAcceleration;
                    OutputLifterZ.Config.Deceleration = scanDeceleration;
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

                // 이동 완료는 moveTask가 보장 — 여기서는 ScanSettleTimeMs 안정 대기 목적만 유지(C7).
                int settleCode = await WaitBinLifterZMoveDoneInPosition(Recipe.MappingEndPosition, OutputLifterZ.Setup.MoveTimeoutMs, ct).ConfigureAwait(false);
                if (settleCode != 0)
                    return FailMappingScanList(
                        "OUT-CST-MAP-END-MOVE",
                        "OutputLifterZ mapping end move/in-position wait failed. waitCode=" + settleCode +
                        ", reason=" + (OutputLifterZ.LastMotionFailureMessage ?? string.Empty));

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

                int result = await MoveBinLifterZ(Recipe.MappingEndPosition, false, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

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

                int result = await MoveBinLifterZ(Recipe.MappingStartPosition, false, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

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
                    // 버퍼가 존별 값을 보존하므로, 스캔한 존은 전 슬롯을 갱신해
                    // 빈 슬롯에 이전 카세트의 실측 값이 남지 않게 한다. (다른 존은 건드리지 않음)
                    if (slotMap[i] && i < slotPositions.Length && !double.IsNaN(slotPositions[i]))
                        Recipe.UpdateSlotPosition(cassette, i, slotPositions[i]);
                    else
                        Recipe.UpdateSlotPosition(cassette, i, double.NaN);

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
                    // To do: [존 분리 스캔] 시뮬 위치도 존 명목식(local 0=맨 위) 기준으로 생성한다.
                    double position = CalculateZoneSlotNominalPosition(cassette, i);
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

        private int FailMappingScan(string alarmCode, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", "OutputCassetteMapping", message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, alarmCode, Name, message);
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

        // To do: [언로드 오프셋] 피더 배출 준비 - 슬롯 위치 + UnloadingPositionOffset 로 이동해
        //        처진 bin과 카세트 셸프의 간섭을 피한다. (PrepareBinCassetteForFeederLoad의 배출 대응)
        public async Task<int> PrepareBinCassetteForFeederUnload(TargetCassette cassette, int slotIndex, int timeoutMs, bool bFine = false)
        {
            try
            {
                if (!CheckBinLifterZMoveReady())
                    return -1;

                int result = await MoveToBinCassetteUnloadOffsetPosition(cassette, slotIndex, bFine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FailMappingScan(
                    "OUT-CST-FEEDER-UNLOAD-EXCEPTION",
                    "Output cassette feeder unload preparation failed. cassette=" + cassette +
                    ", slot=" + slotIndex + ", error=" + ex.Message);
            }
            finally
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputCassette",
                    "PrepareBinCassetteForFeederUnload finished. cassette=" + cassette +
                    ", slot=" + slotIndex +
                    ", unloadOffset=" + ResolveUnloadingPositionOffset().ToString("0.###"));
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
                    // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
                    int result = await MoveToBinCassetteAvoidPosition().ConfigureAwait(false);
                    if (result != 0)
                        return result;
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
            // 기존 조건: 전체 존 티칭 검증 - GOOD 맵핑이 NG 미티칭/존 순서로 막히는 문제가 있었다.
            //if (!ValidateBinLifterZTeachingComplete(out teachingReason))
            // 현재 기준: [존 분리 스캔] 대상 존만 티칭 검증한다.
            if (!ValidateBinLifterZZoneTeachingComplete(cassette == TargetCassette.Ng, out teachingReason))
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

        /// <summary>
        /// 매핑 스캔 전용 속도. 레시피(Output Cassette) SCAN/JOG VELOCITY 값을 사용한다.
        /// 매핑 이외의 이동에는 사용하지 않는다(사용자 확정 2026-07-26).
        /// </summary>
        private double ResolveBinLifterZScanVelocity()
        {
            double velocity = Config != null && Config.ScanVelocity > 0.0 ? Config.ScanVelocity : 0.0;
            if (velocity <= 0.0 && OutputLifterZ != null && OutputLifterZ.Config != null)
                // [정정 2026-07-26] 스케일 적용값 — 원본 유출 차단.
                velocity = OutputLifterZ.Config.GetDefaultVel();
            return velocity > 0.0 ? velocity : 1.0;
        }

        /// <summary>
        /// 매핑이 아닌 일반 리프터 이동 속도(슬롯 이동/Avoid 복귀/카세트 교체 등).
        /// 축 기본 속도(Config.DefaultVelocity)를 사용한다.
        /// 기존에는 매핑용 ScanVelocity가 모든 이동에 적용되어 일반 이동까지 느려졌다.
        /// </summary>
        private double ResolveBinLifterZDefaultMoveVelocity()
        {
            // [머지 정합 2026-07-27] 서버 규칙: 모션 경로는 스케일 적용값(GetDefaultVel)만 읽는다.
            // 원본 DefaultVelocity 직접 읽기는 컴파일 타임 차단됨(AxisConfig protected get).
            double velocity = OutputLifterZ != null && OutputLifterZ.Config != null
                ? OutputLifterZ.Config.GetDefaultVel()
                : 0.0;
            if (velocity <= 0.0)
                velocity = Config != null && Config.ScanVelocity > 0.0 ? Config.ScanVelocity : 0.0;
            return velocity > 0.0 ? velocity : 1.0;
        }

        private static double ResolveCassetteProfileAcceleration(double velocity)
        {
            return Math.Max(1.0, Math.Abs(velocity) * 10.0);
        }

        private static double ResolveCassetteProfileDeceleration(double velocity)
        {
            return Math.Max(1.0, Math.Abs(velocity) * 10.0);
        }

        private async Task<int> MoveWithProtrusionWatch(double targetPosition, double velocity, CancellationToken ct)
        {
            return await MoveWithProtrusionWatch(targetPosition, velocity, 0.0, 0.0, ct).ConfigureAwait(false);
        }

        private async Task<int> MoveWithProtrusionWatch(
            double targetPosition,
            double velocity,
            double acceleration,
            double deceleration,
            CancellationToken ct,
            bool forceMove = false,
            bool allowProtrusionForUnloadRelease = false)
        {
            double oldAcceleration = 0.0;
            double oldDeceleration = 0.0;
            bool useCustomAcceleration = false;
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!allowProtrusionForUnloadRelease &&
                    IsBinProtrusionDetected())
                {
                    LastBinLifterMoveFailureMessage = "돌출 센서 감지로 이동 차단. target=" + targetPosition;
                    OutputLifterZ.EStop();
                    QMC.Common.Log.Write("Main", "MOTION", Name,
                        "Output cassette Z move blocked. Protrusion sensor is ON. target=" + targetPosition + " - Failed");
                    return -1;
                }

                oldAcceleration = OutputLifterZ.Config != null ? OutputLifterZ.Config.GetRawAcceleration() : 0.0;
                oldDeceleration = OutputLifterZ.Config != null ? OutputLifterZ.Config.GetRawDeceleration() : 0.0;
                useCustomAcceleration = OutputLifterZ.Config != null && acceleration > 0.0 && deceleration > 0.0;
                if (useCustomAcceleration)
                {
                    OutputLifterZ.Config.Acceleration = acceleration;
                    OutputLifterZ.Config.Deceleration = deceleration;
                }

                Task<int> moveTask;
                if (forceMove)
                {
                    using (BaseAxis.BeginForceMoveScope())
                    {
                        moveTask = OutputLifterZ.MoveAbsoluteAsync(targetPosition, velocity);
                    }
                }
                else
                {
                    moveTask = OutputLifterZ.MoveAbsoluteAsync(targetPosition, velocity);
                }
                while (!moveTask.IsCompleted)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!allowProtrusionForUnloadRelease &&
                        IsBinProtrusionDetected())
                    {
                        LastBinLifterMoveFailureMessage = "이동 중 돌출 센서 감지로 정지. target=" + targetPosition;
                        OutputLifterZ.EStop();
                        QMC.Common.Log.Write("Main", "MOTION", Name,
                            "Output cassette Z move stopped. Protrusion detected while moving. target=" + targetPosition + " - Failed");
                        return -1;
                    }

                    await Task.Delay(10, ct).ConfigureAwait(false);
                }

                int moveResult = await moveTask.ConfigureAwait(false);
                if (useCustomAcceleration)
                {
                    OutputLifterZ.Config.Acceleration = oldAcceleration;
                    OutputLifterZ.Config.Deceleration = oldDeceleration;
                    useCustomAcceleration = false;
                }

                if (moveResult != 0 || OutputLifterZ.IsAlarm)
                {
                    LastBinLifterMoveFailureMessage = "Bin Lifter Z 이동 명령 실패. result=" + moveResult +
                        ", alarm=" + OutputLifterZ.IsAlarm + ", target=" + targetPosition;
                    QMC.Common.Log.Write("Main", "MOTION", Name,
                        "Output cassette Z move command failed. result=" + moveResult +
                        ", velocity=" + velocity + ". " + BuildOutputLifterZState(targetPosition) + " - Failed");
                    return moveResult != 0 ? moveResult : -1;
                }

                // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수(moveTask)가 완료를 보장하므로 제거(R3).
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
                if (useCustomAcceleration && OutputLifterZ != null && OutputLifterZ.Config != null)
                {
                    OutputLifterZ.Config.Acceleration = oldAcceleration;
                    OutputLifterZ.Config.Deceleration = oldDeceleration;
                }
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
            // To do: [존 분리 스캔] 존별 스캔 구간 티칭 키.
            if (string.Equals(positionName, "NgMappingStart", StringComparison.OrdinalIgnoreCase)) return Recipe.NgMappingStartPosition;
            if (string.Equals(positionName, "NgMappingEnd", StringComparison.OrdinalIgnoreCase)) return Recipe.NgMappingEndPosition;
            if (string.Equals(positionName, "Good1MappingStart", StringComparison.OrdinalIgnoreCase)) return Recipe.Good1MappingStartPosition;
            if (string.Equals(positionName, "Good1MappingEnd", StringComparison.OrdinalIgnoreCase)) return Recipe.Good1MappingEndPosition;
            if (string.Equals(positionName, "Good2MappingStart", StringComparison.OrdinalIgnoreCase)) return Recipe.Good2MappingStartPosition;
            if (string.Equals(positionName, "Good2MappingEnd", StringComparison.OrdinalIgnoreCase)) return Recipe.Good2MappingEndPosition;
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
            // To do: [존 분리 스캔] 존별 스캔 구간 티칭 키.
            else if (string.Equals(positionName, "NgMappingStart", StringComparison.OrdinalIgnoreCase)) Recipe.NgMappingStartPosition = position;
            else if (string.Equals(positionName, "NgMappingEnd", StringComparison.OrdinalIgnoreCase)) Recipe.NgMappingEndPosition = position;
            else if (string.Equals(positionName, "Good1MappingStart", StringComparison.OrdinalIgnoreCase)) Recipe.Good1MappingStartPosition = position;
            else if (string.Equals(positionName, "Good1MappingEnd", StringComparison.OrdinalIgnoreCase)) Recipe.Good1MappingEndPosition = position;
            else if (string.Equals(positionName, "Good2MappingStart", StringComparison.OrdinalIgnoreCase)) Recipe.Good2MappingStartPosition = position;
            else if (string.Equals(positionName, "Good2MappingEnd", StringComparison.OrdinalIgnoreCase)) Recipe.Good2MappingEndPosition = position;
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

        // 기존 방식: Good2 First는 Good1First - Level2Offset - span 파생값이었다.
        // 현재 기준: [존 분리 스캔] Good2도 독립 티칭 필드를 직접 사용한다(Level2Offset 미사용).
        private double GetGood2FirstSlotPosition()
        {
            return Recipe.Good2FirstSlotPosition;
        }

        private void SetGood2FirstSlotPosition(double position)
        {
            Recipe.Good2FirstSlotPosition = position;
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
