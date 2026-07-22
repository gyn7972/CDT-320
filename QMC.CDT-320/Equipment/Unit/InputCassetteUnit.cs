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
        // To do: [맵핑 재설계] 슬롯 벨리드 윈도우 반폭 비율(윈도우 = 명목 ± SlotPitch×비율, 0.5 미만).
        //        실측 웨이퍼 안착 오차(≤1.4mm) 대비 여유를 두되 기구물 엣지(오차 2.0mm+)는 배제하도록 조정한다.
        [DataMember]public double MappingWindowRatio { get; set; }
        // To do: [맵핑 재설계] 점유 인정 최소 ON 이동거리[mm](디바운스, 스침성 반응 필터).
        [DataMember]public double MappingMinOnTravelMm { get; set; }
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
            // To do: [맵핑 재설계] 윈도우 반폭 기본 = pitch×0.25 (pitch 10.5 기준 ±2.625mm).
            //        실측 안착 오차 최대 1.4mm 대비 여유 유지, 기구물 엣지(2.0mm+)는 배제.
            MappingWindowRatio = 0.25;
            MappingMinOnTravelMm = 1.0;
        }
    }
    
    [DataContract]
    public class InputCassetteRecipe : IRecipeData
    {
        [DataMember] public double AvoidPosition { get; set; }  //ReadyPosition.
        [DataMember] public double LoaingPosition { get; set; }
        [DataMember] public double UnloadingPosition { get; set; }
        // To do: [레벨 분리 스캔] 1단/2단은 동시(연속) 맵핑하지 않고 레벨별로 따로 스캔한다.
        //        MappingStartPosition/MappingEndPosition = 1단 스캔 구간(앵커=1단 맨아래 슬롯).
        //        Level2MappingStartPosition/EndPosition   = 2단 스캔 구간(앵커=2단 맨아래 슬롯).
        //        FirstSlot(스타트 포지션)도 레벨별: Level1=1단 맨위 첫 제품 로딩, Level2=2단 맨위 첫 제품 로딩.
        //        (기존 FirstSlotPosition은 2단용으로 명칭 변경됨)
        [DataMember] public double Level2MappingStartPosition { get; set; }
        [DataMember] public double Level2MappingEndPosition { get; set; }
        [DataMember] public double Level1FirstSlotPosition { get; set; }
        [DataMember] public double Level2FirstSlotPosition { get; set; }
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
            MappingStartPosition = 5.0;
            MappingEndPosition = 130.0;
            // To do: [레벨 분리 스캔] 레벨별 티칭 기본값. 미티칭(0)이면 검증/커버리지에서 걸러진다.
            Level2MappingStartPosition = 0.0;
            Level2MappingEndPosition = 0.0;
            Level1FirstSlotPosition = 0.0;
            Level2FirstSlotPosition = 0.0;
            SlotPosition = Array.Empty<double>();
        }
    }

    public class InputCassetteUnit : BaseUnit<InputCassetteSetup, InputCassetteConfig, InputCassetteRecipe>, IUnitJogController
    {
        // To do: C4 - 슬롯 상태를 레벨별(1단/2단) dict로 관리한다. 외부 키=level(1/2), 내부 키=레벨 내 로컬 슬롯 인덱스.
        private readonly Dictionary<int, Dictionary<int, WaferSlotState>> levelSlotStates = new Dictionary<int, Dictionary<int, WaferSlotState>>();
        private readonly Dictionary<int, Dictionary<int, WaferSlotState>> mappingPreviousLevelSlotStates = new Dictionary<int, Dictionary<int, WaferSlotState>>();

        // To do: [맵핑 재설계] 슬롯 윈도우 판정 파라미터는 Config(MappingWindowRatio/MappingMinOnTravelMm)로 승격됨.
        //        아래 상수는 Config 값이 비정상(0 이하, ratio 0.5 이상)일 때의 안전 fallback이다.
        private const double MappingSlotWindowRatio = 0.25;
        private const double MappingSlotMinOnTravelMm = 1.0;

        // To do: [맵핑 재설계] 윈도우 반폭 비율 - Config 우선, 범위 밖이면 fallback 상수.
        private double ResolveMappingWindowRatio()
        {
            double ratio = Config != null ? Config.MappingWindowRatio : 0.0;
            if (ratio <= 0.0 || ratio >= 0.5)
                ratio = MappingSlotWindowRatio;
            return ratio;
        }

        // To do: [맵핑 재설계] 점유 인정 최소 ON 이동거리 - Config 우선, 0 이하면 fallback 상수.
        private double ResolveMappingMinOnTravel()
        {
            double travel = Config != null ? Config.MappingMinOnTravelMm : 0.0;
            return travel > 0.0 ? travel : MappingSlotMinOnTravelMm;
        }
        // 마지막 윈도우 판정 스캔 결과(점유/실측 중심 위치). ScanCassetteFromCurrentStartAsync가 소비한다.
        private bool[] lastScanSlotMap;
        private double[] lastScanSlotPositions;
        private IReadOnlyList<bool> mappingPreviousWaferMap;
        private double[] mappingPreviousSlotPositions;
        private bool mappingSnapshotActive;

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
            return MoveWaferLifterZ(target, speedType, customSpeed, true);
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
            return await MoveWaferLifterZ(targetPos, bFine, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> MoveWaferLifterZ(double targetPos, bool bFine, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                string targetReason;
                if (!ValidateWaferLifterZTargetPosition(targetPos, out targetReason))
                {
                    RaiseWaferCassetteConditionAlarm("IN-CST-LIFTER-TARGET", targetReason);
                    return -1;
                }

                string interlockReason;
                if (!CheckWaferLifterZInterlock(targetPos, MotionGuardMoveKind.AxisMove, out interlockReason))
                {
                    LastWaferLifterMoveFailureMessage = interlockReason;
                    return -11;
                }

                return await MoveWithProtrusionWatch(
                    targetPos,
                    ResolveWaferLifterZMoveVelocity(bFine),
                    ResolveWaferLifterZMoveAcceleration(bFine),
                    ResolveWaferLifterZMoveDeceleration(bFine),
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try { InputLifterZ?.Stop(); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                LastWaferLifterMoveFailureMessage = "Wafer Lifter Z 이동 예외. target=" + targetPos + ", error=" + ex.Message;
                Log.Write("Main", "MOTION", Name,
                    "Input cassette Z move failed. target=" + targetPos +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private bool CheckWaferLifterZInterlock(double targetPos, MotionGuardMoveKind moveKind, out string reason)
        {
            reason = string.Empty;

            try
            {
                bool allowed = InputCassetteInterlockRules.VerifyWaferLifterZ(
                    Machine,
                    targetPos,
                    moveKind,
                    out reason);

                if (allowed)
                    return true;

                if (string.IsNullOrWhiteSpace(reason))
                    reason = "InputLifterZ 이동 인터락 조건이 만족되지 않습니다.";

                RaiseWaferCassetteConditionAlarm("IN-CST-LIFTER-INTERLOCK", reason);
                return false;
            }
            catch (Exception ex)
            {
                reason = "InputLifterZ 이동 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message;
                RaiseWaferCassetteConditionAlarm("IN-CST-LIFTER-INTERLOCK-EX", reason);
                return false;
            }
            finally
            {
            }
        }

        /// <summary>지정한 조그 속도 모드로 Wafer Lifter Z축을 절대 위치로 이동합니다.</summary>
        public async Task<int> MoveWaferLifterZ(double targetPos, JogSpeedType speedType, double customSpeed = 0)
        {
            return await MoveWaferLifterZ(targetPos, speedType, customSpeed, false).ConfigureAwait(false);
        }

        private async Task<int> MoveWaferLifterZ(double targetPos, JogSpeedType speedType, double customSpeed, bool forceMove)
        {
            try
            {
                string targetReason;
                if (!ValidateWaferLifterZTargetPosition(targetPos, out targetReason))
                {
                    RaiseWaferCassetteConditionAlarm("IN-CST-LIFTER-TARGET", targetReason);
                    return -1;
                }

                // 인터락 사전검사는 실제 이동의 BaseAxis.MotionGuard 훅에서 1번 수행한다. 중복 호출하지 않는다.
                double velocity = ResolveJogVelocity(speedType, customSpeed);
                return await MoveWithProtrusionWatch(
                    targetPos,
                    velocity,
                    UnitJogVelocityResolver.ResolveAcceleration(InputLifterZ),
                    UnitJogVelocityResolver.ResolveDeceleration(InputLifterZ),
                    CancellationToken.None,
                    forceMove).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LastWaferLifterMoveFailureMessage = "Wafer Lifter Z 이동 예외. target=" + targetPos + ", error=" + ex.Message;
                Log.Write("Main", "MOTION", Name,
                    "Input cassette Z move failed. target=" + targetPos +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
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

        public async Task<int> MoveWaferLifterZToTeachingPosition(string positionName, JogSpeedType speedType, double customSpeed)
        {
            try
            {
                return await MoveWaferLifterZ(GetTeachingPosition(positionName), speedType, customSpeed).ConfigureAwait(false);
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

        // To do: level(1단/2단)을 받아 해당 레벨 로딩 위치로 이동한다. 기본 1단(기존 호출부 호환).
        public async Task<int> MoveToWaferCassetteSlotPosition(int slotIndex, bool bFine = false, int level = 1)
        {
            try
            {
                return await MoveWaferLifterZ(CalculateWaferCassetteSlotTargetPosition(slotIndex, level), bFine);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        // To do: 티칭 앵커(MappingStart=밑 슬롯 검출 위치)에서 웨이퍼가 센서를 가리므로,
        //        스캔 시작은 앵커보다 반 피치 아래(엔코더 +)에서 출발해 첫 웨이퍼 rising edge를 보장한다.
        public double ResolveMappingScanStartPosition()
        {
            double margin = Config != null && Config.SlotPitch > 0.0 ? Config.SlotPitch * 0.5 : 0.0;
            return Recipe.MappingStartPosition + margin;
        }

        // To do: MappingEnd는 슬롯 앵커가 아니라 "스캔 끝 경계"다. 2단 맨 위 슬롯보다 위(엔코더 작은 값)로
        //        여유 있게 티칭해야 하며, 티칭값 밖으로는 이동하지 않는다(축 한계 안전).
        //        커버리지 부족(맨 위 슬롯 명목이 MappingEnd보다 위)은 스캔 시작 시 경고 로그로 알린다.
        public double ResolveMappingScanEndPosition()
        {
            return Recipe.MappingEndPosition;
        }

        // To do: [레벨 분리 스캔] 해당 레벨의 MappingEnd가 그 레벨 맨 위 슬롯 윈도우를 지나는지 검사한다(부족하면 맨 위 웨이퍼 미검출).
        public bool IsMappingScanCoveringLevelTopSlot(int level, out string detail)
        {
            detail = string.Empty;
            try
            {
                double topSlotPosition = CalculateCassetteLevelSlotPosition(level, 0);
                double margin = Config != null && Config.SlotPitch > 0.0 ? Config.SlotPitch * 0.5 : 0.0;
                double requiredEnd = topSlotPosition - margin;   // 맨 위 슬롯 반 피치 위까지 필요
                double scanEnd = ResolveLevelMappingEndPosition(level);
                if (scanEnd <= requiredEnd)
                    return true;

                detail = "Mapping scan end does not cover the top slot. level=" + level +
                         ", scanEnd=" + scanEnd.ToString("0.###") +
                         ", topSlot(local01)=" + topSlotPosition.ToString("0.###") +
                         ", requiredEnd<=" + requiredEnd.ToString("0.###") +
                         ". 해당 레벨 MappingEnd 티칭을 맨 위 슬롯보다 위로 다시 잡아야 합니다.";
                return false;
            }
            catch (Exception ex)
            {
                detail = "Mapping scan coverage check failed: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        public async Task<int> MoveToWaferCassetteMappingStartPosition(bool bFine = false)
        {
            try
            {
                return await MoveWaferLifterZ(ResolveMappingScanStartPosition(), bFine);
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
            return await WaitWaferLifterZMoveDone(timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> WaitWaferLifterZMoveDone(int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                return await WaitWaferLifterZMoveDoneInPosition(InputLifterZ.CommandPosition, timeoutMs, ct).ConfigureAwait(false);
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
        public async Task<int> WaitWaferLifterZMoveDoneInPosition(double targetPos, int timeoutMs)
        {
            return await WaitWaferLifterZMoveDoneInPosition(targetPos, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> WaitWaferLifterZMoveDoneInPosition(double targetPos, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int waitCode = await InputLifterZ.WaitMoveCompleteAsync(targetPos, timeoutMs, ct).ConfigureAwait(false);
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

        public async Task<int> WaitWaferLifterZInPosition(string positionName, int timeoutMs)
        {
            return await WaitWaferLifterZInPosition(positionName, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> WaitWaferLifterZInPosition(string positionName, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                double target = GetTeachingPosition(positionName);
                return await WaitWaferLifterZMoveDoneInPosition(target, timeoutMs, ct).ConfigureAwait(false);
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

        public bool IsWaferLifterZInAvoidPosition()
        {
            return IsWaferLifterZInPosition(Recipe.AvoidPosition, ResolveWaferLifterZInPositionTolerance());
        }

        // To do: level(1단/2단) 대응. 기본 1단(기존 호출부 호환).
        public bool IsWaferLifterZInSlotPosition(int slotIndex, int level = 1)
        {
            return IsWaferLifterZInPosition(CalculateWaferCassetteSlotTargetPosition(slotIndex, level), ResolveWaferLifterZInPositionTolerance());
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

        // To do: [레벨 분리 스캔] 레벨별 스타트 포지션(그 레벨 맨 위 슬롯의 피더 로딩 절대 위치) 티칭.
        public void TeachWaferLifterZSlotBasePosition()
        {
            // 기존 호환: 파라미터 없는 티칭은 2단(FirstSlotPosition 명칭 변경분)으로 저장한다.
            TeachWaferLifterZLevelFirstSlotPosition(2);
        }

        public void TeachWaferLifterZLevelFirstSlotPosition(int level)
        {
            if (level >= 2)
                Recipe.Level2FirstSlotPosition = InputLifterZ.ActualPosition;
            else
                Recipe.Level1FirstSlotPosition = InputLifterZ.ActualPosition;
            EnsureSlotPositionBuffer();
        }

        // To do: [레벨 분리 스캔] 2단 스캔 구간 티칭.
        public void TeachWaferLifterZLevel2MappingStartPosition()
        {
            Recipe.Level2MappingStartPosition = InputLifterZ.ActualPosition;
        }

        public void TeachWaferLifterZLevel2MappingEndPosition()
        {
            Recipe.Level2MappingEndPosition = InputLifterZ.ActualPosition;
        }

        // To do: FirstSlotPosition의 기준 슬롯 = "최상위 레벨의 첫(맨 위) 슬롯" (2단 구성이면 2단 01번).
        //        사용자는 그 슬롯의 피더 로딩 절대 위치에서 TEACH 한다(예: 265.475).
        //        로딩 오프셋 = FirstSlotPosition - 기준 슬롯의 검출 위치.
        //        기준 슬롯 검출 위치는 맵핑 실측(SlotPosition)이 있으면 실측을, 없으면 명목값을 쓴다.
        //        (실측 기준이면 기준 슬롯의 로딩 위치는 티칭값과 정확히 일치하고, 나머지 슬롯은 피치만큼 따라간다.)
        // To do: [레벨 분리 스캔] 로딩 오프셋도 레벨별로 파생한다.
        //        기준 슬롯 = 해당 레벨의 맨 위 슬롯(local 0). 그 슬롯의 FirstSlot(스타트 포지션) 티칭값 - 검출 위치(실측 우선).
        //        해당 레벨 FirstSlot이 미티칭(<=0)이면 반대 레벨 오프셋으로 폴백한다(두 레벨 기구 편차가 작다는 가정).
        public double ResolveCassetteLoadingOffset(int level)
        {
            double firstSlot = level >= 2 ? Recipe.Level2FirstSlotPosition : Recipe.Level1FirstSlotPosition;
            if (firstSlot <= 0.0)
            {
                int otherLevel = level >= 2 ? 1 : 2;
                double otherFirst = otherLevel >= 2 ? Recipe.Level2FirstSlotPosition : Recipe.Level1FirstSlotPosition;
                if (ResolveCassetteLevelCount() >= 2 && otherFirst > 0.0)
                    return ResolveCassetteLoadingOffset(otherLevel);
                return 0.0;
            }

            int referenceFlat = ToFlatSlotIndex(level, 0);
            double referenceDetect = double.NaN;
            if (Recipe.SlotPosition != null && referenceFlat >= 0 && referenceFlat < Recipe.SlotPosition.Length)
                referenceDetect = Recipe.SlotPosition[referenceFlat];

            if (double.IsNaN(referenceDetect))
                referenceDetect = CalculateMappingSlotPosition(referenceFlat);

            return firstSlot - referenceDetect;
        }

        // To do: 로딩 목표 = 맵핑 "검출 위치"(실측 우선) + 해당 레벨 로딩 오프셋.
        public double CalculateWaferCassetteSlotTargetPosition(int slotIndex, int level = 1)
        {
            ValidateSlotIndex(slotIndex);
            EnsureSlotPositionBuffer();

            if (level < 1)
                level = 1;

            int flatIndex = ToFlatSlotIndex(level, slotIndex);
            double detectPosition;
            if (Recipe.SlotPosition != null && flatIndex >= 0 && flatIndex < Recipe.SlotPosition.Length &&
                !double.IsNaN(Recipe.SlotPosition[flatIndex]))
                detectPosition = Recipe.SlotPosition[flatIndex];   // 맵핑 검출 위치(실측)
            else
                detectPosition = CalculateMappingSlotPosition(flatIndex);   // 미맵핑 시 명목 검출 위치

            return detectPosition + ResolveCassetteLoadingOffset(level);   // 검출 위치 + 레벨별 로딩 오프셋
        }

        // To do: 웨이퍼 카세트 포지션/맵핑 결과 저장용 "로딩 위치"(검출 + 레벨별 오프셋)를 계산한다.
        public double CalculateCassetteLevelSlotLoadingPosition(int level, int slotIndex)
        {
            return CalculateCassetteLevelSlotPosition(level, slotIndex) + ResolveCassetteLoadingOffset(level);
        }

        public bool ValidateWaferLifterZTeachingComplete()
        {
            string reason;
            return ValidateWaferLifterZTeachingComplete(out reason);
        }

        public bool ValidateWaferLifterZTeachingComplete(out string reason)
        {
            reason = string.Empty;
            var reasons = new List<string>();

            if (Config == null)
            {
                reasons.Add("Config is null.");
            }
            else
            {
                if (Config.SlotCount <= 0)
                    reasons.Add("SlotCount is invalid. slotCount=" + Config.SlotCount);
                if (Config.SlotPitch <= 0.0)
                    reasons.Add("SlotPitch is invalid. slotPitch=" + Config.SlotPitch);
            }

            if (Recipe == null)
            {
                reasons.Add("Recipe is null.");
            }
            else
            {
                if (Recipe.MappingEndPosition == Recipe.MappingStartPosition)
                    reasons.Add("MappingStartPosition and MappingEndPosition are same. position=" + Recipe.MappingStartPosition);

                // To do: [레벨 분리 스캔] 2단 구성이면 2단 스캔 구간 티칭도 검증한다.
                if (ResolveCassetteLevelCount() >= 2)
                {
                    if (Recipe.Level2MappingStartPosition <= 0.0 || Recipe.Level2MappingEndPosition <= 0.0)
                        reasons.Add("Level2 mapping start/end is not taught. start=" + Recipe.Level2MappingStartPosition +
                                    ", end=" + Recipe.Level2MappingEndPosition);
                    else if (Recipe.Level2MappingEndPosition >= Recipe.Level2MappingStartPosition)
                        reasons.Add("Level2 MappingEnd must be above(smaller than) Level2 MappingStart. start=" +
                                    Recipe.Level2MappingStartPosition + ", end=" + Recipe.Level2MappingEndPosition);
                }
            }

            if (reasons.Count == 0)
                return true;

            reason = string.Join(" ", reasons.ToArray());
            return false;
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
            if (IsDryRunInput(Wafer8CassetteCheck0) ||
                IsDryRunInput(Wafer12CassetteCheck0))
                return true;

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
            if (IsDryRunInput(Wafer8CassetteCheck0) ||
                IsDryRunInput(Wafer12CassetteCheck0))
                return true;

            if (recipeSize == 8)
                return Wafer8CassetteCheck0.IsOn && Wafer8CassetteCheck1.IsOn;
            if (recipeSize == 12)
                return Wafer12CassetteCheck0.IsOn && Wafer12CassetteCheck1.IsOn;
            return IsAnyCassetteSensorOn();
        }

        public bool IsWaferProtrusionDetected()
        {
            if (IsDryRunInput(WaferRingJutCheck))
                return false;

            return WaferRingJutCheck.IsOn;
        }

        public bool IsWaferProtrusionDetectionSensor()
        {
            return IsWaferProtrusionDetected();
        }

        public bool IsWaferMapping()
        {
            if (IsDryRunInput(WaferMappingSensor))
                return false;

            return WaferMappingSensor.IsOn;
        }

        private static bool IsDryRunInput(BaseDigitalInput input)
        {
            return input != null && input.Config != null && input.Config.IgnoreWaits;
        }

        public async Task<int> WaitWaferJutClear(int timeoutMs)
        {
            return await WaitWaferJutClear(timeoutMs, CancellationToken.None);
        }

        public async Task<int> WaitWaferJutClear(int timeoutMs, CancellationToken ct)
        {
            try
            {
                return await WaferRingJutCheck.WaitUntilStateAsync(false, timeoutMs, ct) ? 0 : -1;
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
            return await WaitWaferMappingSensor(expected, timeoutMs, CancellationToken.None);
        }

        public async Task<int> WaitWaferMappingSensor(bool expected, int timeoutMs, CancellationToken ct)
        {
            try
            {
                return await WaferMappingSensor.WaitUntilStateAsync(expected, timeoutMs, ct) ? 0 : -1;
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
                // 인터락은 MoveJogContinuous 내부 BaseAxis.MotionGuard 훅에서 1번 수행한다.
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
                // 인터락은 MoveJogContinuous 내부 BaseAxis.MotionGuard 훅에서 1번 수행한다.
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
            return await WaferScan(timeoutMs, bFine, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> WaferScan(int timeoutMs, bool bFine, CancellationToken ct)
        {
            bool mappingStarted = false;
            bool mappingSucceeded = false;
            try
            {
                ct.ThrowIfCancellationRequested();
                string readyReason;
                if (!CheckWaferCassetteMappingReady(out readyReason))
                {
                    RaiseWaferCassetteConditionAlarm("IN-CST-SCAN-READY", "Wafer scan ready check failed. " + readyReason);
                    return -1;
                }

                BeginWaferMapping();
                mappingStarted = true;
                int result = await ScanCassetteAsync(ResolveMappingSlotCount(), Config.SlotPitch, ct).ConfigureAwait(false);
                mappingSucceeded = result == 0;
                return result;
            }
            catch (OperationCanceledException)
            {
                try { InputLifterZ?.Stop(); } catch { }
                throw;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (mappingStarted)
                {
                    if (mappingSucceeded)
                        EndWaferMapping();
                    else
                        RollbackWaferMapping();
                }
            }
        }

        public async Task<int> WaferScanFromCurrentStart(int timeoutMs = 0, bool bFine = false)
        {
            return await WaferScanFromCurrentStart(timeoutMs, bFine, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> WaferScanFromCurrentStart(int timeoutMs, bool bFine, CancellationToken ct)
        {
            bool mappingStarted = false;
            bool mappingSucceeded = false;
            try
            {
                ct.ThrowIfCancellationRequested();
                string readyReason;
                if (!CheckWaferCassetteMappingReady(out readyReason))
                {
                    RaiseWaferCassetteConditionAlarm("IN-CST-SCAN-READY", "Wafer scan from current start ready check failed. " + readyReason);
                    return -1;
                }

                BeginWaferMapping();
                mappingStarted = true;
                int result = await ScanCassetteFromCurrentStartAsync(ResolveMappingSlotCount(), Config.SlotPitch, timeoutMs, ct).ConfigureAwait(false);
                mappingSucceeded = result == 0;
                return result;
            }
            catch (OperationCanceledException)
            {
                try { InputLifterZ?.Stop(); } catch { }
                throw;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (mappingStarted)
                {
                    if (mappingSucceeded)
                        EndWaferMapping();
                    else
                        RollbackWaferMapping();
                }
            }
        }

        public async Task<int> MoveToNextWaferSlot(bool bFine = false)
        {
            try
            {
                // To do: [맵핑 재설계] 다음 처리 슬롯의 레벨(role)까지 받아 해당 레벨 로딩 위치로 이동한다.
                CassetteMaterialRole nextRole;
                int slot = FindNextProcessWaferSlot(out nextRole);
                if (slot < 0)
                {
                    RaiseWaferCassetteConditionAlarm("IN-CST-NEXT-SLOT", "No next process wafer slot was found.");
                    return -1;
                }

                int result = await MoveToWaferCassetteSlotPosition(slot, bFine, ResolveCassetteLevel(nextRole));
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

        // To do: level(1단/2단)을 받아 해당 레벨 슬롯 로딩 위치로 이동한다. 기본 1단(기존 호출부 호환).
        public async Task<int> PrepareWaferCassetteForFeederLoad(int slotIndex, int timeoutMs, bool bFine = false, int level = 1)
        {
            try
            {
                string readyReason;
                if (!CheckWaferCassetteMoveReady(out readyReason))
                {
                    RaiseWaferCassetteConditionAlarm("IN-CST-FEEDER-LOAD-READY", "Prepare cassette for feeder load ready check failed. " + readyReason);
                    return -1;
                }

                int result = await MoveToWaferCassetteSlotPosition(slotIndex, bFine, level);
                if (result != 0)
                    return result;

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

        public async Task<int> RecoverWaferCassetteToSafeState(int timeoutMs, bool moveAvoid = true)
        {
            try
            {
                if (await WaitWaferJutClear(timeoutMs) != 0)
                    return -1;

                if (moveAvoid)
                {
                    // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
                    int moveResult = await MoveToWaferCassetteAvoidPosition();
                    if (moveResult != 0)
                        return moveResult;

                    return 0;
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
            string reason;
            return CheckWaferCassetteMoveReady(out reason);
        }

        public bool CheckWaferCassetteMoveReady(out string reason)
        {
            reason = string.Empty;
            var reasons = new List<string>();

            if (InputLifterZ == null)
            {
                reasons.Add("InputLifterZ axis is null.");
            }
            else
            {
                if (!InputLifterZ.IsServoOn)
                    reasons.Add("InputLifterZ servo is OFF.");
                if (InputLifterZ.IsAlarm)
                    reasons.Add("InputLifterZ alarm is ON. alarmCode=" + InputLifterZ.AlarmCode);
                if (InputLifterZ.IsMoving)
                    reasons.Add("InputLifterZ is moving.");
            }

            if (IsWaferProtrusionDetected())
                reasons.Add("Wafer protrusion sensor is ON.");

            if (reasons.Count == 0)
                return true;

            reason = string.Join(" ", reasons.ToArray()) + " " + BuildCassetteSensorSummary();
            Log.Write("Main", "SYSTEM", "InputCassetteUnit", "Move ready check failed: " + reason + " - Check");
            return false;
        }

        public bool CheckWaferCassetteTransferReady(TransferMode mode)
        {
            string reason;
            return CheckWaferCassetteTransferReady(mode, out reason);
        }

        public bool CheckWaferCassetteTransferReady(TransferMode mode, out string reason)
        {
            reason = string.Empty;
            if (!CheckWaferCassetteMoveReady(out reason))
                return false;
            if (mode == TransferMode.Load || mode == TransferMode.Unload)
            {
                if (!IsAnyCassetteSensorOn())
                {
                    reason = "Input cassette is not detected for transfer. mode=" + mode + ". " + BuildCassetteSensorSummary();
                    Log.Write("Main", "SYSTEM", "InputCassetteUnit", "Transfer ready check failed: " + reason + " - Check");
                    return false;
                }
            }
            return true;
        }

        public bool CheckWaferCassetteMappingReady()
        {
            string reason;
            return CheckWaferCassetteMappingReady(out reason);
        }

        public bool CheckWaferCassetteMappingReady(out string reason)
        {
            reason = string.Empty;
            if (!CheckWaferCassetteMoveReady(out reason))
                return false;

            if (!IsAnyCassetteSensorOn())
            {
                reason = "Input cassette is not detected for mapping. " + BuildCassetteSensorSummary();
                Log.Write("Main", "SYSTEM", "InputCassetteUnit", "Mapping ready check failed: " + reason + " - Check");
                return false;
            }

            if (!ValidateWaferLifterZTeachingComplete(out reason))
            {
                reason = "Input cassette teaching data is not complete. " + reason;
                Log.Write("Main", "SYSTEM", "InputCassetteUnit", "Mapping ready check failed: " + reason + " - Check");
                return false;
            }

            return true;
        }

        public WaferCassetteMaterial GetWaferMaterialCassette()
        {
            return GetWaferMaterialCassette(1);
        }

        // To do: C4 - 레벨(1단/2단)별 슬롯 상태 material을 반환한다.
        public WaferCassetteMaterial GetWaferMaterialCassette(int level)
        {
            var material = new WaferCassetteMaterial(Config.SlotCount);
            for (int i = 0; i < Config.SlotCount; i++)
            {
                WaferSlotState state;
                if (!TryGetLevelSlotState(level, i, out state))
                    state = new WaferSlotState { Presence = SlotPresence.Unknown, Process = ProcessState.Unknown };
                material.Slots.Add(state);
            }
            return material;
        }

        public bool HasMoreProcessWafer()
        {
            return FindNextProcessWaferSlot() >= 0;
        }

        // To do: C4 - 완료 판정을 1단/2단 모두에 대해 수행한다. 설정 레벨 전체의 존재 웨이퍼가 Done/Ng여야 완료.
        public bool IsInputCassetteProcessComplete()
        {
            try
            {
                int slotCount = Config != null && Config.SlotCount > 0 ? Config.SlotCount : 0;
                if (slotCount <= 0)
                    return false;

                int levelCount = ResolveCassetteLevelCount();
                bool hasProcessWafer = false;

                for (int level = 1; level <= levelCount; level++)
                {
                    CassetteMaterialRole role = ResolveCassetteRole(level);
                    var levelStates = GetLevelSlotStates(level);
                    var cassette = MaterialStateService.State != null && MaterialStateService.State.Cassettes != null
                        ? MaterialStateService.State.Cassettes.FirstOrDefault(c => c.Role == role)
                        : null;

                    if (cassette != null && cassette.IsMapped)
                    {
                        cassette.EnsureSlots();
                        int count = Math.Min(slotCount, cassette.Slots.Count);
                        for (int i = 0; i < count; i++)
                        {
                            WaferSlotState slotState;
                            bool hasSlotState = levelStates.TryGetValue(i, out slotState);
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
                                WaferMaterial wafer = MaterialStateService.GetWaferInCassette(role, i);
                                if (wafer == null)
                                    return false;

                                if (WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Finish)
                                    return false;
                            }
                        }

                        continue;
                    }

                    // 미맵핑 레벨은 센서 기반 슬롯 상태만으로 판정한다.
                    for (int i = 0; i < slotCount; i++)
                    {
                        WaferSlotState state;
                        if (!levelStates.TryGetValue(i, out state) || state.Presence != SlotPresence.Exist)
                            continue;

                        hasProcessWafer = true;
                        if (state.Process != ProcessState.Done && state.Process != ProcessState.Ng)
                            return false;
                    }
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
            const string message = "입력 카세트의 모든 웨이퍼 작업이 완료되었습니다. 카세트를 교체하세요.";
            Log.Write("Main", "SYSTEM", string.IsNullOrWhiteSpace(source) ? code : source, message + " - Check");
        }

        public bool IsHaveMoreProcessWafer()
        {
            return HasMoreProcessWafer();
        }

        public int FindNextProcessWaferSlot()
        {
            CassetteMaterialRole role;
            return FindNextProcessWaferSlot(out role);
        }

        // To do: [맵핑 재설계] 처리 순서 = flat 오름차순(전체 맨 위부터 아래로).
        //        2단 구성: 2단 01(맨 위)→2단 13→1단 01→1단 13 순. "2단 첫번째 슬롯부터, 맨 위에서 아래로" 스펙.
        //        role(Input1/Input2)은 flat에서 파생한다.
        public int FindNextProcessWaferSlot(out CassetteMaterialRole role)
        {
            int total = ResolveMappingSlotCount();

            // 1차: 센서 기반 슬롯 상태(레벨별 dict)에서 flat 오름차순으로 Ready 탐색.
            for (int flat = 0; flat < total; flat++)
            {
                int level = ResolveLevelFromFlatIndex(flat);
                int local = ResolveLocalSlotFromFlatIndex(flat);
                WaferSlotState state;
                if (TryGetLevelSlotState(level, local, out state) &&
                    state != null &&
                    state.Presence == SlotPresence.Exist &&
                    (state.Process == ProcessState.Ready || state.Process == ProcessState.Unknown))
                {
                    role = ResolveCassetteRole(level);
                    return local;
                }
            }

            // 2차: 자재상태(Material) 기반으로 flat 오름차순 탐색.
            for (int flat = 0; flat < total; flat++)
            {
                int level = ResolveLevelFromFlatIndex(flat);
                int local = ResolveLocalSlotFromFlatIndex(flat);
                if (IsMaterialSlotProcessReady(level, local))
                {
                    UpdateWaferCassetteSlotState(level, local, SlotPresence.Exist, ProcessState.Ready);
                    role = ResolveCassetteRole(level);
                    return local;
                }
            }

            role = CassetteMaterialRole.Input1;
            return -1;
        }

        // To do: [맵핑 재설계] 자재상태에서 지정 (level, local) 슬롯이 처리 가능(Ready/WorkReady)한지 판정한다.
        private bool IsMaterialSlotProcessReady(int level, int localSlotIndex)
        {
            try
            {
                WaferSlotState slotState;
                if (TryGetLevelSlotState(level, localSlotIndex, out slotState) &&
                    slotState != null &&
                    slotState.Process != ProcessState.Ready &&
                    slotState.Process != ProcessState.Unknown)
                {
                    return false;
                }

                CassetteMaterialRole role = ResolveCassetteRole(level);
                var cassette = MaterialStateService.State != null && MaterialStateService.State.Cassettes != null
                    ? MaterialStateService.State.Cassettes.FirstOrDefault(c => c.Role == role)
                    : null;
                if (cassette == null || !cassette.IsMapped)
                    return false;

                cassette.EnsureSlots();
                if (localSlotIndex < 0 || localSlotIndex >= cassette.Slots.Count)
                    return false;

                var slot = cassette.Slots[localSlotIndex];
                if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                    return false;

                WaferMaterial wafer = MaterialStateService.GetWaferInCassette(role, localSlotIndex);
                if (wafer == null)
                    return false;

                WaferMaterialState state = WaferMaterialStateText.Normalize(wafer.State);
                return state == WaferMaterialState.Ready || state == WaferMaterialState.WorkReady;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "InputCassetteUnit",
                    "Material slot process-ready check failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public string BuildProcessWaferAvailabilitySummary()
        {
            try
            {
                int slotCount = Config != null && Config.SlotCount > 0 ? Config.SlotCount : 0;
                int sensorExist = 0;
                int sensorSelectable = 0;
                int sensorProcessing = 0;
                int sensorDone = 0;
                int sensorNg = 0;

                // To do: C4 - 진단 요약은 우선 1단 기준. 필요 시 레벨별 합산으로 확장.
                var summaryStates = GetLevelSlotStates(1);
                for (int i = 0; i < slotCount; i++)
                {
                    WaferSlotState slotState;
                    if (!summaryStates.TryGetValue(i, out slotState) || slotState == null)
                        continue;

                    if (slotState.Presence == SlotPresence.Exist)
                    {
                        sensorExist++;
                        if (slotState.Process == ProcessState.Ready || slotState.Process == ProcessState.Unknown)
                            sensorSelectable++;
                    }

                    if (slotState.Process == ProcessState.Processing)
                        sensorProcessing++;
                    else if (slotState.Process == ProcessState.Done)
                        sensorDone++;
                    else if (slotState.Process == ProcessState.Ng)
                        sensorNg++;
                }

                CassetteMaterial cassette = MaterialStateService.State != null && MaterialStateService.State.Cassettes != null
                    ? MaterialStateService.State.Cassettes.FirstOrDefault(c => c.Role == CassetteMaterialRole.Input1)
                    : null;
                if (cassette == null)
                {
                    return "Input1[missing=True, slots=" + slotCount +
                           ", sensorExist=" + sensorExist +
                           ", sensorSelectable=" + sensorSelectable +
                           ", sensorProcessing=" + sensorProcessing +
                           ", sensorDone=" + sensorDone +
                           ", sensorNg=" + sensorNg + "]";
                }

                cassette.EnsureSlots();
                int materialOccupied = 0;
                int materialReady = 0;
                int materialWorkReady = 0;
                int materialWorking = 0;
                int materialFinish = 0;
                int materialMissing = 0;

                foreach (CassetteSlotMaterial slot in cassette.Slots)
                {
                    if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                        continue;

                    materialOccupied++;
                    WaferMaterial wafer = MaterialStateService.GetWaferInCassette(CassetteMaterialRole.Input1, slot.SlotNumber);
                    if (wafer == null)
                    {
                        materialMissing++;
                        continue;
                    }

                    WaferMaterialState state = WaferMaterialStateText.Normalize(wafer.State);
                    if (state == WaferMaterialState.Ready)
                        materialReady++;
                    else if (state == WaferMaterialState.WorkReady)
                        materialWorkReady++;
                    else if (state == WaferMaterialState.Working)
                        materialWorking++;
                    else if (state == WaferMaterialState.Finish)
                        materialFinish++;
                }

                return "Input1[enabled=" + cassette.IsEnabled +
                       ", present=" + cassette.IsPresent +
                       ", mapped=" + cassette.IsMapped +
                       ", slots=" + cassette.Slots.Count +
                       ", sensorExist=" + sensorExist +
                       ", sensorSelectable=" + sensorSelectable +
                       ", sensorProcessing=" + sensorProcessing +
                       ", sensorDone=" + sensorDone +
                       ", sensorNg=" + sensorNg +
                       ", materialOccupied=" + materialOccupied +
                       ", materialReady=" + materialReady +
                       ", materialWorkReady=" + materialWorkReady +
                       ", materialWorking=" + materialWorking +
                       ", materialFinish=" + materialFinish +
                       ", materialMissing=" + materialMissing + "]";
            }
            catch (Exception ex)
            {
                return "Input1[summaryError=" + ex.Message + "]";
            }
            finally
            {
            }
        }

        // To do: C4 - 지정 레벨(1단/2단) material 상태에서 다음 처리 슬롯을 찾는다.
        private int FindNextProcessWaferSlotFromMaterialState(int level)
        {
            try
            {
                int slotCount = Config != null && Config.SlotCount > 0 ? Config.SlotCount : 0;
                if (slotCount <= 0)
                    return -1;

                CassetteMaterialRole role = ResolveCassetteRole(level);
                var cassette = MaterialStateService.State != null && MaterialStateService.State.Cassettes != null
                    ? MaterialStateService.State.Cassettes.FirstOrDefault(c => c.Role == role)
                    : null;
                if (cassette == null || !cassette.IsMapped)
                    return -1;

                cassette.EnsureSlots();
                var levelStates = GetLevelSlotStates(level);
                int count = Math.Min(slotCount, cassette.Slots.Count);
                for (int i = 0; i < count; i++)
                {
                    WaferSlotState slotState;
                    if (levelStates.TryGetValue(i, out slotState) &&
                        slotState.Process != ProcessState.Ready &&
                        slotState.Process != ProcessState.Unknown)
                    {
                        continue;
                    }

                    var slot = cassette.Slots[i];
                    if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                        continue;

                    WaferMaterial wafer = MaterialStateService.GetWaferInCassette(role, i);
                    if (wafer == null)
                        continue;

                    WaferMaterialState state = WaferMaterialStateText.Normalize(wafer.State);
                    if (state == WaferMaterialState.Ready || state == WaferMaterialState.WorkReady)
                    {
                        UpdateWaferCassetteSlotState(level, i, SlotPresence.Exist, ProcessState.Ready);
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

        // To do: C4 - 레벨별 슬롯 상태 접근 헬퍼.
        private Dictionary<int, WaferSlotState> GetLevelSlotStates(int level)
        {
            int lv = level >= 2 ? 2 : 1;
            Dictionary<int, WaferSlotState> states;
            if (!levelSlotStates.TryGetValue(lv, out states))
            {
                states = new Dictionary<int, WaferSlotState>();
                levelSlotStates[lv] = states;
            }
            return states;
        }

        private bool TryGetLevelSlotState(int level, int slotIndex, out WaferSlotState state)
        {
            return GetLevelSlotStates(level).TryGetValue(slotIndex, out state);
        }

        // To do: C4 - CassetteMaterialRole(Input1/Input2) ↔ level(1/2) 변환.
        internal static int ResolveCassetteLevel(CassetteMaterialRole role)
        {
            return role == CassetteMaterialRole.Input2 ? 2 : 1;
        }

        internal static CassetteMaterialRole ResolveCassetteRole(int level)
        {
            return level >= 2 ? CassetteMaterialRole.Input2 : CassetteMaterialRole.Input1;
        }

        public void UpdateWaferCassetteSlotState(int slotIndex, SlotPresence presence, ProcessState state)
        {
            UpdateWaferCassetteSlotState(1, slotIndex, presence, state);
        }

        // To do: C4 - 레벨 인지 슬롯 상태 갱신.
        public void UpdateWaferCassetteSlotState(int level, int slotIndex, SlotPresence presence, ProcessState state)
        {
            ValidateSlotIndex(slotIndex);
            GetLevelSlotStates(level)[slotIndex] = new WaferSlotState { Presence = presence, Process = state };
        }

        public void BeginWaferMapping()
        {
            CaptureWaferMappingSnapshot();
            // Mapping scan이 실패/취소되기 전까지 마지막 정상 map과 공정 상태를 유지한다.
            // 새 결과는 scan 완료 후 WaferMap에 한 번에 반영된다.
            EnsureSlotPositionBuffer();
        }

        // To do: C4 - WaferMap(flat)을 레벨별로 나눠 1단/2단 슬롯 상태를 모두 적용한다.
        //        [맵핑 재설계] flat 배치 = 앞쪽(0~N-1)이 2단(위 카세트), 뒤쪽(N~2N-1)이 1단. ToFlatSlotIndex로 통일.
        public void EndWaferMapping()
        {
            var map = new List<bool>(WaferMap);
            int slotCount = Config != null ? Config.SlotCount : 0;
            int levelCount = ResolveCassetteLevelCount();
            for (int level = 1; level <= levelCount; level++)
            {
                for (int i = 0; i < slotCount; i++)
                {
                    int flat = ToFlatSlotIndex(level, i);
                    if (flat >= map.Count)
                        continue;

                    WaferSlotState previous;
                    if (TryGetLevelSlotState(level, i, out previous) &&
                        previous != null &&
                        (previous.Process == ProcessState.Processing || previous.Process == ProcessState.Done))
                    {
                        continue;
                    }

                    UpdateWaferCassetteSlotState(level, i, map[flat] ? SlotPresence.Exist : SlotPresence.Empty, ProcessState.Ready);
                    if (!map[flat] && Recipe.SlotPosition != null && flat < Recipe.SlotPosition.Length)
                        Recipe.UpdateSlotPosition(flat, double.NaN);
                }
            }
        }

        // To do: C4 - 등록된 WaferMap 결과를 레벨별로 슬롯 상태에 반영한다.
        //        [맵핑 재설계] flat 배치 = 앞쪽이 2단. ToFlatSlotIndex로 통일.
        public void ApplyRegisteredWaferMappingState()
        {
            var map = new List<bool>(WaferMap ?? new List<bool>().AsReadOnly());
            int slotCount = Config != null ? Config.SlotCount : 0;
            int levelCount = ResolveCassetteLevelCount();
            for (int level = 1; level <= levelCount; level++)
            {
                CassetteMaterialRole role = ResolveCassetteRole(level);
                for (int i = 0; i < slotCount; i++)
                {
                    int flat = ToFlatSlotIndex(level, i);
                    if (flat >= map.Count)
                        continue;

                    if (!map[flat])
                    {
                        UpdateWaferCassetteSlotState(level, i, SlotPresence.Empty, ProcessState.Ready);
                        if (Recipe.SlotPosition != null && flat < Recipe.SlotPosition.Length)
                            Recipe.UpdateSlotPosition(flat, double.NaN);
                        continue;
                    }

                    WaferMaterial wafer = MaterialStateService.GetWaferInCassette(role, i);
                    ProcessState process = wafer != null && WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Finish
                        ? ProcessState.Done
                        : ProcessState.Ready;
                    UpdateWaferCassetteSlotState(level, i, SlotPresence.Exist, process);
                }
            }
        }

        // To do: [맵핑 재설계] UI 표시용 - WaferMap(flat)에서 지정 레벨의 슬롯 맵(local 0=맨 위)을 잘라 반환한다.
        public IReadOnlyList<bool> GetLevelWaferMapView(int level)
        {
            int slotCount = Config != null ? Config.SlotCount : 0;
            var view = new bool[Math.Max(0, slotCount)];
            var map = WaferMap;
            if (map == null || slotCount <= 0)
                return view;

            for (int i = 0; i < slotCount; i++)
            {
                int flat = ToFlatSlotIndex(level, i);
                view[i] = flat >= 0 && flat < map.Count && map[flat];
            }

            return view;
        }

        public void CommitWaferMapping()
        {
            mappingPreviousLevelSlotStates.Clear();
            mappingPreviousWaferMap = null;
            mappingPreviousSlotPositions = null;
            mappingSnapshotActive = false;
        }

        public void RollbackWaferMapping()
        {
            if (!mappingSnapshotActive)
                return;

            WaferMap = mappingPreviousWaferMap != null
                ? new List<bool>(mappingPreviousWaferMap).AsReadOnly()
                : new List<bool>().AsReadOnly();

            // To do: C4 - 레벨별 슬롯 상태 스냅샷을 복원한다.
            CopyLevelSlotStates(mappingPreviousLevelSlotStates, levelSlotStates);

            int positionCount = mappingPreviousSlotPositions != null ? mappingPreviousSlotPositions.Length : 0;
            Recipe.ResizeSlotPositions(positionCount);
            for (int i = 0; i < positionCount; i++)
                Recipe.UpdateSlotPosition(i, mappingPreviousSlotPositions[i]);

            CommitWaferMapping();
        }

        private void CaptureWaferMappingSnapshot()
        {
            if (mappingSnapshotActive)
                RollbackWaferMapping();

            mappingPreviousWaferMap = WaferMap != null
                ? new List<bool>(WaferMap).AsReadOnly()
                : new List<bool>().AsReadOnly();
            // To do: C4 - 레벨별 슬롯 상태 스냅샷을 저장한다.
            CopyLevelSlotStates(levelSlotStates, mappingPreviousLevelSlotStates);

            mappingPreviousSlotPositions = Recipe.SlotPosition != null
                ? (double[])Recipe.SlotPosition.Clone()
                : new double[0];
            mappingSnapshotActive = true;
        }

        // To do: C4 - 레벨별 슬롯 상태 dict 깊은 복사(스냅샷/복원 공용).
        private static void CopyLevelSlotStates(
            Dictionary<int, Dictionary<int, WaferSlotState>> source,
            Dictionary<int, Dictionary<int, WaferSlotState>> target)
        {
            target.Clear();
            foreach (KeyValuePair<int, Dictionary<int, WaferSlotState>> levelPair in source)
            {
                var copied = new Dictionary<int, WaferSlotState>();
                if (levelPair.Value != null)
                {
                    foreach (KeyValuePair<int, WaferSlotState> slotPair in levelPair.Value)
                    {
                        WaferSlotState state = slotPair.Value;
                        copied[slotPair.Key] = state != null
                            ? new WaferSlotState { Presence = state.Presence, Process = state.Process }
                            : null;
                    }
                }
                target[levelPair.Key] = copied;
            }
        }

        public void BuildSimulatedWaferMap()
        {
            BeginWaferMapping();

            int count = Config != null && Config.SlotCount > 0 ? ResolveMappingSlotCount() : 25;
            var map = new List<bool>(count);
            for (int i = 0; i < count; i++)
            {
                map.Add(true);
                UpdateSlotPosition(i, CalculateMappingSlotPosition(i));
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
                // 카세트 미감지 알람 메시지
                case WaferCassetteAlarmCode.CassetteMissing: return "Wafer cassette is missing.";
                // 카세트 사이즈 불일치 알람 메시지
                case WaferCassetteAlarmCode.SizeMismatch: return "Wafer cassette size mismatch.";
                // 웨이퍼 돌출 감지 알람 메시지
                case WaferCassetteAlarmCode.ProtrusionDetected: return "Wafer protrusion detected.";
                // 맵핑 타임아웃 알람 메시지
                case WaferCassetteAlarmCode.MappingTimeout: return "Wafer mapping timeout.";
                // 리프터 Z축 이동 타임아웃 알람 메시지
                case WaferCassetteAlarmCode.MoveTimeout: return "Wafer lifter Z move timeout.";
                // 리프터 Z축 티칭 누락 알람 메시지
                case WaferCassetteAlarmCode.TeachingMissing: return "Wafer lifter Z teaching data is missing.";
                default: return "No wafer cassette alarm.";
            }
        }

        public async Task<int> ScanCassetteAsync(int maxSlots, double slotPitch)
        {
            return await ScanCassetteAsync(maxSlots, slotPitch, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> ScanCassetteAsync(int maxSlots, double slotPitch, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
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

                int startResult = await MoveToWaferCassetteMappingStartAndVerifyAsync(ct);
                if (startResult != 0)
                    return startResult;

                return await ScanCassetteFromCurrentStartAsync(maxSlots, slotPitch, 0, ct);
            }
            catch (OperationCanceledException)
            {
                try { InputLifterZ?.Stop(); } catch { }
                throw;
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
            return await ScanCassetteFromCurrentStartAsync(maxSlots, slotPitch, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> ScanCassetteFromCurrentStartAsync(int maxSlots, double slotPitch, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
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

                // To do: [맵핑 재설계] 엣지 수집 후 매칭 방식 폐기. 스캔 중 실시간으로 "현재 위치가 벨리드한
                //        슬롯 윈도우 안인지" 판정해 그 자리에서 점유를 확정한다.
                //        - 한 슬롯에서 몇 번 센싱되든 점유 1건으로만 처리(슬롯별 bool).
                //        - 윈도우 밖(기구물 등 인벨리드 구간) 센싱은 무시하고 로그만 남긴다.
                //        - ON 구간이 인벨리드에서 시작해 벨리드 구간으로 이어져도 윈도우 통과 중 ON이면 정상 점유.
                bool[] slotMap;
                double[] slotPositions;
                int collectResult = await CollectMappingSlotOccupancyAsync(maxSlots, ct).ConfigureAwait(false);
                if (collectResult != 0)
                    return -1;

                slotMap = lastScanSlotMap;
                slotPositions = lastScanSlotPositions;
                if (slotMap == null || slotPositions == null || slotMap.Length != maxSlots)
                    return FailMappingScan("IN-CST-MAP-RESULT", "Mapping scan result buffer is invalid.");

                var map = new List<bool>(maxSlots);
                for (int i = 0; i < maxSlots; i++)
                {
                    map.Add(slotMap[i]);
                    if (slotMap[i])
                        UpdateSlotPosition(i, slotPositions[i]);
                }

                WaferMap = map.AsReadOnly();
                int occupiedCount = map.Count(x => x);
                Log.Write("Main", "SYSTEM", "InputCassetteMapping",
                    occupiedCount > 0
                        ? "Mapping scan completed. slots=" + occupiedCount + " - Ok"
                        : "Mapping scan completed with an empty cassette. slots=0 - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                try { InputLifterZ?.Stop(); } catch { }
                throw;
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

        private async Task<int> MoveToWaferCassetteMappingStartAndVerifyAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                int startResult = await MoveWaferLifterZ(Recipe.MappingStartPosition, false, ct).ConfigureAwait(false);
                if (startResult != 0 || InputLifterZ.IsAlarm)
                    return FailMappingScan("IN-CST-MAP-START", "InputLifterZ move failed at mapping start.");

                return 0;
            }
            catch (OperationCanceledException)
            {
                try { InputLifterZ?.Stop(); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                return FailMappingScan("IN-CST-MAP-START-EXCEPTION", "InputLifterZ mapping start move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        // To do: [레벨 분리 스캔] 윈도우 실시간 판정 + 레벨별 세그먼트 스캔.
        //        1단/2단은 동시(연속) 맵핑하지 않는다. 각 레벨은 자기 MappingStart(그 레벨 맨아래 슬롯 앵커)
        //        반 피치 아래에서 출발해 자기 MappingEnd까지만 스캔하고, 그 레벨의 슬롯 윈도우만 판정한다.
        //        SelectedCassetteLevel=1이면 1단만, =2면 1단 스캔 후 2단 스캔(아래→위 순서).
        //        카세트 사이(기구물) 구간은 세그먼트 사이 이동으로만 통과하며 판정하지 않는다.
        //        - 한 슬롯에서 몇 번 센싱되든 점유는 1건(슬롯별 bool). 윈도우 내 ON 즉시 점유(디바운스 없음).
        //        - 윈도우 밖 ON은 무시 + 스트레치 로그.
        //        - 각 레벨 맨아래 슬롯의 "시작부터 센서 ON"도 자연 점유(세그먼트 시작이 윈도우 밖 반 피치 아래).
        //        결과는 lastScanSlotMap / lastScanSlotPositions(ON 구간 중심 실측)에 저장한다.
        private async Task<int> CollectMappingSlotOccupancyAsync(int maxSlots, CancellationToken ct)
        {
            double originalAcc = 0.0;
            double originalDec = 0.0;
            bool restoreScanProfile = false;
            try
            {
                ct.ThrowIfCancellationRequested();
                lastScanSlotMap = null;
                lastScanSlotPositions = null;

                bool virtualSensor = IsVirtualMappingSensorMode();

                double pitch = Config != null ? Config.SlotPitch : 0.0;
                double windowHalf = pitch > 0.0 ? pitch * ResolveMappingWindowRatio() : 0.0;
                if (windowHalf <= 0.0)
                    return FailMappingScan("IN-CST-MAP-WINDOW", "Slot valid window is invalid. pitch=" + FormatPosition(pitch));

                // 현재 기준: 온트라벨(디바운스) 미사용 - 윈도우 내 ON 즉시 점유.
                Log.Write("Main", "SYSTEM", "InputCassetteUnit",
                    "Mapping scan window. halfWidth=" + FormatPosition(windowHalf) +
                    " (ratio=" + FormatPosition(ResolveMappingWindowRatio()) + ") - Ok");

                var centers = new double[maxSlots];
                for (int i = 0; i < maxSlots; i++)
                    centers[i] = CalculateMappingSlotPosition(i);

                var occupied = new bool[maxSlots];
                var onMin = new double[maxSlots];
                var onMax = new double[maxSlots];
                for (int i = 0; i < maxSlots; i++)
                {
                    onMin[i] = double.NaN;
                    onMax[i] = double.NaN;
                }

                bool invalidZoneOn = false;
                double invalidOnMin = double.NaN;
                double invalidOnMax = double.NaN;

                double scanVelocity = ResolveWaferLifterZConfigMoveVelocity();
                double scanAcceleration = ResolveCassetteProfileAcceleration(scanVelocity);
                double scanDeceleration = ResolveCassetteProfileDeceleration(scanVelocity);
                if (InputLifterZ.Config != null)
                {
                    originalAcc = InputLifterZ.Config.Acceleration;
                    originalDec = InputLifterZ.Config.Deceleration;
                    InputLifterZ.Config.Acceleration = scanAcceleration;
                    InputLifterZ.Config.Deceleration = scanDeceleration;
                    restoreScanProfile = true;
                }

                int slotCount = Config != null ? Config.SlotCount : 0;
                int levelCount = ResolveCassetteLevelCount();

                // 스캔 순서: 1단(아래) 먼저, 2단 구성이면 이어서 2단.
                for (int level = 1; level <= levelCount; level++)
                {
                    int flatLo = ToFlatSlotIndex(level, 0);
                    int flatHi = ToFlatSlotIndex(level, slotCount - 1);

                    double levelAnchor = ResolveLevelMappingStartPosition(level);
                    double levelEnd = ResolveLevelMappingEndPosition(level);
                    if (levelAnchor <= 0.0 || levelEnd <= 0.0 || levelEnd >= levelAnchor)
                        return FailMappingScan("IN-CST-MAP-LEVEL-TEACH",
                            "Level" + level + " mapping start/end teaching is invalid. start=" + FormatPosition(levelAnchor) +
                            ", end=" + FormatPosition(levelEnd) + " (end must be above start).");

                    // 세그먼트 시작 = 앵커(맨아래 슬롯) 반 피치 아래(윈도우 밖) → 맨아래 웨이퍼도 윈도우 통과로 자연 점유.
                    double segmentStart = levelAnchor + (pitch * 0.5);
                    int approachResult = await MoveLifterWatchedForMappingAsync(segmentStart, scanVelocity, "level" + level + " scan start", ct).ConfigureAwait(false);
                    if (approachResult != 0)
                        return approachResult;

                    // 커버리지: 레벨 End가 그 레벨 맨 위 슬롯 윈도우를 지나야 맨 위 웨이퍼가 검출된다.
                    string coverageDetail;
                    if (!IsMappingScanCoveringLevelTopSlot(level, out coverageDetail))
                        Log.Write("Main", "SYSTEM", "InputCassetteUnit", coverageDetail + " - Check");

                    // 인터락은 MoveAbsoluteAsync 내부 BaseAxis.MotionGuard 훅에서 1번 수행한다.
                    Task<int> moveTask = InputLifterZ.MoveAbsoluteAsync(levelEnd, scanVelocity);
                    while (!moveTask.IsCompleted)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (IsWaferProtrusionDetected())
                        {
                            InputLifterZ.EStop();
                            return FailMappingScan("IN-CST-MAP-PROTRUSION", "Wafer protrusion detected during mapping scan.");
                        }

                        if (virtualSensor)
                            ApplyVirtualMappingOccupancy(segmentStart, levelEnd, InputLifterZ.ActualPosition,
                                centers, windowHalf, occupied, onMin, onMax, flatLo, flatHi);
                        else
                            ProcessMappingScanSample(InputLifterZ.ActualPosition, WaferMappingSensor.IsOn,
                                centers, windowHalf, occupied, onMin, onMax,
                                ref invalidZoneOn, ref invalidOnMin, ref invalidOnMax, flatLo, flatHi);

                        await Task.Delay(1, ct).ConfigureAwait(false);
                    }

                    int moveResult = await moveTask;
                    if (moveResult != 0 || InputLifterZ.IsAlarm)
                        return FailMappingScan("IN-CST-MAP-END", "InputLifterZ move failed during level" + level + " mapping scan.");

                    // 마지막 샘플 반영(이동 완료 직후 상태) + 잔여 invalid 스트레치 마감.
                    if (virtualSensor)
                        ApplyVirtualMappingOccupancy(segmentStart, levelEnd, levelEnd,
                            centers, windowHalf, occupied, onMin, onMax, flatLo, flatHi);
                    else
                        ProcessMappingScanSample(InputLifterZ.ActualPosition, WaferMappingSensor.IsOn,
                            centers, windowHalf, occupied, onMin, onMax,
                            ref invalidZoneOn, ref invalidOnMin, ref invalidOnMax, flatLo, flatHi);

                    FlushInvalidZoneStretchLog(ref invalidZoneOn, ref invalidOnMin, ref invalidOnMax);

                    // 이동 완료는 moveTask가 보장 — 여기서는 ScanSettleTimeMs 안정 대기 목적만 유지(C7).
                    int settleCode = await WaitWaferLifterZMoveDoneInPosition(levelEnd, ResolveWaferLifterZMoveTimeoutMs(), ct).ConfigureAwait(false);
                    if (settleCode != 0)
                        return FailMappingScan(
                            "IN-CST-MAP-END-MOVE",
                            "InputLifterZ level" + level + " mapping end move/in-position wait failed. waitCode=" + settleCode +
                            ", reason=" + (InputLifterZ.LastMotionFailureMessage ?? string.Empty));
                }

                // 결과 확정: 점유 슬롯은 ON 구간 중심을 실측 검출 위치로 저장하고, 슬롯별 로그를 남긴다.
                var positions = new double[maxSlots];
                for (int i = 0; i < maxSlots; i++)
                {
                    positions[i] = double.NaN;
                    if (!occupied[i])
                    {
                        // 현재 기준: 온트라벨(디바운스) 미사용 - 윈도우 내 ON 즉시 점유이므로 여기 오는 미점유 슬롯은 ON 관측 자체가 없던 슬롯이다.
                        continue;
                    }

                    positions[i] = (onMin[i] + onMax[i]) * 0.5;
                    Log.Write("Main", "SYSTEM", "InputCassetteUnit",
                        "Mapping slot occupied. slot=" + (i + 1) +
                        " (level" + ResolveLevelFromFlatIndex(i) + "/" + (ResolveLocalSlotFromFlatIndex(i) + 1).ToString("00") + ")" +
                        ", measured=" + FormatPosition(positions[i]) +
                        ", nominal=" + FormatPosition(centers[i]) +
                        ", error=" + FormatPosition(Math.Abs(positions[i] - centers[i])) + " - Ok");
                }

                lastScanSlotMap = occupied;
                lastScanSlotPositions = positions;
                return 0;
            }
            catch (OperationCanceledException)
            {
                try { InputLifterZ?.Stop(); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                return FailMappingScan("IN-CST-MAP-COLLECT", "Mapping slot occupancy scan failed: " + ex.Message);
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

        // To do: [레벨 분리 스캔] 세그먼트 이동(스캔 판정 없이 감시만: 프로트루전/알람). 레벨 스캔 시작 위치 접근용.
        private async Task<int> MoveLifterWatchedForMappingAsync(double target, double velocity, string moveName, CancellationToken ct)
        {
            Task<int> moveTask = InputLifterZ.MoveAbsoluteAsync(target, velocity);
            while (!moveTask.IsCompleted)
            {
                ct.ThrowIfCancellationRequested();
                if (IsWaferProtrusionDetected())
                {
                    InputLifterZ.EStop();
                    return FailMappingScan("IN-CST-MAP-PROTRUSION", "Wafer protrusion detected while moving to " + moveName + ".");
                }

                await Task.Delay(5, ct).ConfigureAwait(false);
            }

            int moveResult = await moveTask;
            if (moveResult != 0 || InputLifterZ.IsAlarm)
                return FailMappingScan("IN-CST-MAP-MOVE", "InputLifterZ move failed. moveName=" + moveName + ", target=" + FormatPosition(target));

            // 이동 완료는 moveTask가 보장 — 여기서는 ScanSettleTimeMs 안정 대기 목적만 유지(C7).
            int settleCode = await WaitWaferLifterZMoveDoneInPosition(target, ResolveWaferLifterZMoveTimeoutMs(), ct).ConfigureAwait(false);
            if (settleCode != 0)
                return FailMappingScan(
                    "IN-CST-MAP-MOVE",
                    "InputLifterZ " + moveName + " move/in-position wait failed. waitCode=" + settleCode +
                    ", reason=" + (InputLifterZ.LastMotionFailureMessage ?? string.Empty));

            return 0;
        }

        // To do: [맵핑 재설계] 실센서 샘플 1건 처리. 센서 ON일 때 현재 위치가 속한 슬롯 윈도우를 찾아
        //        ON 관측 구간(min/max)을 누적하고 즉시 점유 확정.
        //        [레벨 분리 스캔] flatLo~flatHi 범위(현재 스캔 중인 레벨의 슬롯들)만 판정 대상으로 한다.
        //        윈도우 밖 ON은 무시하되 스트레치 시작~끝~중심을 로그로 남긴다(캘리브레이션용 실측 데이터).
        private void ProcessMappingScanSample(
            double position,
            bool sensorOn,
            double[] centers,
            double windowHalf,
            bool[] occupied,
            double[] onMin,
            double[] onMax,
            ref bool invalidZoneOn,
            ref double invalidOnMin,
            ref double invalidOnMax,
            int flatLo,
            int flatHi)
        {
            if (!sensorOn)
            {
                FlushInvalidZoneStretchLog(ref invalidZoneOn, ref invalidOnMin, ref invalidOnMax);
                return;
            }

            int slot = -1;
            double bestError = double.MaxValue;
            int lo = Math.Max(0, flatLo);
            int hi = Math.Min(centers.Length - 1, flatHi);
            for (int i = lo; i <= hi; i++)
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

            FlushInvalidZoneStretchLog(ref invalidZoneOn, ref invalidOnMin, ref invalidOnMax);

            if (double.IsNaN(onMin[slot]) || position < onMin[slot])
                onMin[slot] = position;
            if (double.IsNaN(onMax[slot]) || position > onMax[slot])
                onMax[slot] = position;

            // 기존 조건: 윈도우 내 ON 이동거리(onSpan)가 최소값 이상일 때만 점유 인정(디바운스).
            //if (!occupied[slot] && (onMax[slot] - onMin[slot]) >= ResolveMappingMinOnTravel())
            //    occupied[slot] = true;
            // 현재 기준: 온트라벨(디바운스) 없이 윈도우 안에서 센서 ON이 관측되면 즉시 점유로 확정한다.
            occupied[slot] = true;
        }

        // To do: [맵핑 재설계] invalid zone ON 스트레치가 끝났을 때(OFF 또는 윈도우 진입) 시작~끝~중심을 로그로 남긴다.
        //        윈도우가 실물과 어긋나 전부 invalid로 빠져도 이 로그로 실측 중심을 복원해 G/pitch를 보정할 수 있다.
        private void FlushInvalidZoneStretchLog(ref bool invalidZoneOn, ref double invalidOnMin, ref double invalidOnMax)
        {
            if (!invalidZoneOn)
                return;

            if (!double.IsNaN(invalidOnMin) && !double.IsNaN(invalidOnMax))
                Log.Write("Main", "SYSTEM", "InputCassetteUnit",
                    "Mapping sensor ON in invalid zone (ignored). from=" + FormatPosition(invalidOnMax) +
                    ", to=" + FormatPosition(invalidOnMin) +
                    ", center=" + FormatPosition((invalidOnMin + invalidOnMax) * 0.5) +
                    ", span=" + FormatPosition(invalidOnMax - invalidOnMin) + " - Check");

            invalidZoneOn = false;
            invalidOnMin = double.NaN;
            invalidOnMax = double.NaN;
        }

        // To do: [맵핑 재설계] 가상 센서(시뮬/드라이런) - 스캔 경로가 슬롯 중심을 통과하면 만재로 점유 처리(기존 시뮬 동작 유지).
        //        [레벨 분리 스캔] 현재 스캔 중인 레벨의 flat 범위만 처리한다.
        private void ApplyVirtualMappingOccupancy(
            double scanStart,
            double scanEnd,
            double currentPosition,
            double[] centers,
            double windowHalf,
            bool[] occupied,
            double[] onMin,
            double[] onMax,
            int flatLo,
            int flatHi)
        {
            int lo = Math.Max(0, flatLo);
            int hi = Math.Min(centers.Length - 1, flatHi);
            for (int i = lo; i <= hi; i++)
            {
                if (occupied[i])
                    continue;

                if (!IsPositionPassed(scanStart, scanEnd, currentPosition, centers[i], windowHalf))
                    continue;

                occupied[i] = true;
                onMin[i] = centers[i];
                onMax[i] = centers[i];
            }
        }

        // To do: [맵핑 재설계] 미사용 - 엣지 수집 방식은 윈도우 실시간 판정(CollectMappingSlotOccupancyAsync)으로 대체됨.
        //        기구물 반응으로 인벨리드 구간에서 ON이 시작되면 rising edge 위치가 슬롯과 어긋나는 문제가 있었다.
        private async Task<List<double>> CollectMappingSensorPositionsAsync(CancellationToken ct)
        {
            double originalAcc = 0.0;
            double originalDec = 0.0;
            bool restoreScanProfile = false;
            try
            {
                ct.ThrowIfCancellationRequested();
                var detectedPositions = new List<double>();
                bool virtualSensor = IsVirtualMappingSensorMode();
                double scanStartPosition = InputLifterZ.ActualPosition;
                bool[] virtualDetected = virtualSensor ? new bool[ResolveMappingSlotCount()] : null;
                bool previous = virtualSensor ? false : WaferMappingSensor.IsOn;

                // 첫장은 무조건 감지가됨.
                //if (!virtualSensor && previous)
                //    return FailMappingScanList("IN-CST-MAP-SENSOR-ON", "Mapping sensor is ON at mapping start. Check mapping start position.");

                // To do: 1단 맨 아래 슬롯은 mapping start와 같은 높이라, 웨이퍼가 있으면 시작부터 센서 ON이 정상이다.
                //        이 경우 rising edge가 발생하지 않아 검출 목록에서 유실되므로,
                //        시작 위치를 해당 슬롯의 검출로 직접 등록한다(이후에는 OFF→ON 엣지만 추가 검출).
                if (!virtualSensor && previous)
                {
                    AddDetectedMappingPosition(detectedPositions, scanStartPosition);
                    Log.Write("Main", "SYSTEM", "InputCassetteUnit",
                        "Mapping sensor ON at start. Registered as bottom slot detection. position=" +
                        FormatPosition(scanStartPosition) + " - Check");
                }

                double scanVelocity = ResolveWaferLifterZConfigMoveVelocity();
                double scanAcceleration = ResolveCassetteProfileAcceleration(scanVelocity);
                double scanDeceleration = ResolveCassetteProfileDeceleration(scanVelocity);

                if (InputLifterZ.Config != null)
                {
                    originalAcc = InputLifterZ.Config.Acceleration;
                    originalDec = InputLifterZ.Config.Deceleration;
                    InputLifterZ.Config.Acceleration = scanAcceleration;
                    InputLifterZ.Config.Deceleration = scanDeceleration;
                    restoreScanProfile = true;
                }

                // 인터락은 MoveAbsoluteAsync 내부 BaseAxis.MotionGuard 훅에서 1번 수행한다.
                // 차단 시 moveResult != 0 로 반환되어 아래 FailMappingScanList 로 처리된다.
                double scanEndPosition = ResolveMappingScanEndPosition();

                // 기존 커버리지 경고: 미사용 경로(레벨 분리 스캔으로 대체됨).
                //string coverageDetail;
                //if (!IsMappingScanCoveringTopSlot(out coverageDetail))
                //    Log.Write("Main", "SYSTEM", "InputCassetteUnit", coverageDetail + " - Check");
                Task<int> moveTask = InputLifterZ.MoveAbsoluteAsync(scanEndPosition, scanVelocity);
                while (!moveTask.IsCompleted)
                {
                    ct.ThrowIfCancellationRequested();
                    if (IsWaferProtrusionDetected())
                    {
                        InputLifterZ.EStop();
                        return FailMappingScanList("IN-CST-MAP-PROTRUSION", "Wafer protrusion detected during mapping scan.");
                    }

                    if (virtualSensor)
                    {
                        AddVirtualMappingDetections(
                            detectedPositions,
                            virtualDetected,
                            scanStartPosition,
                            scanEndPosition,
                            InputLifterZ.ActualPosition);
                    }
                    else
                    {
                        bool current = WaferMappingSensor.IsOn;
                        if (current && !previous)
                            AddDetectedMappingPosition(detectedPositions, InputLifterZ.ActualPosition);

                        previous = current;
                    }
                    await Task.Delay(5, ct).ConfigureAwait(false);
                }

                int moveResult = await moveTask;
                if (moveResult != 0 || InputLifterZ.IsAlarm)
                    return FailMappingScanList("IN-CST-MAP-END", "InputLifterZ move failed during mapping scan.");

                if (virtualSensor)
                    AddVirtualMappingDetections(
                        detectedPositions,
                        virtualDetected,
                        scanStartPosition,
                        scanEndPosition,
                        scanEndPosition);

                // 이동 완료는 moveTask가 보장 — 여기서는 ScanSettleTimeMs 안정 대기 목적만 유지(C7).
                int settleCode = await WaitWaferLifterZMoveDoneInPosition(scanEndPosition, ResolveWaferLifterZMoveTimeoutMs(), ct).ConfigureAwait(false);
                if (settleCode != 0)
                    return FailMappingScanList(
                        "IN-CST-MAP-END-MOVE",
                        "InputLifterZ mapping end move/in-position wait failed. waitCode=" + settleCode +
                        ", reason=" + (InputLifterZ.LastMotionFailureMessage ?? string.Empty));

                return detectedPositions;
            }
            catch (OperationCanceledException)
            {
                try { InputLifterZ?.Stop(); } catch { }
                throw;
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
                double minSpacing = Config.SlotPitch > 0.0 ? Config.SlotPitch * 0.8 : 0.0;
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

        private bool IsVirtualMappingSensorMode()
        {
            return Config != null && Config.bDryRun ||
                   InputLifterZ != null &&
                   InputLifterZ.Config != null &&
                   InputLifterZ.Config.IsSimulationMode;
        }

        private void AddVirtualMappingDetections(
            List<double> detectedPositions,
            bool[] virtualDetected,
            double scanStartPosition,
            double scanEndPosition,
            double currentPosition)
        {
            if (detectedPositions == null || virtualDetected == null)
                return;

            int count = Math.Min(virtualDetected.Length, ResolveMappingSlotCount());
            double tolerance = ResolveMappingPitchTolerance(Config != null ? Config.SlotPitch : 0.0);
            for (int i = 0; i < count; i++)
            {
                if (virtualDetected[i])
                    continue;

                double nominal = CalculateMappingSlotPosition(i);
                if (!IsPositionPassed(scanStartPosition, scanEndPosition, currentPosition, nominal, tolerance))
                    continue;

                AddDetectedMappingPosition(detectedPositions, nominal);
                virtualDetected[i] = true;
            }
        }

        private static bool IsPositionPassed(double start, double end, double current, double target, double tolerance)
        {
            if (tolerance < 0.0)
                tolerance = 0.0;

            double min = Math.Min(start, end);
            double max = Math.Max(start, end);
            if (target < min - tolerance || target > max + tolerance)
                return false;

            if (end >= start)
                return current + tolerance >= target;

            return current - tolerance <= target;
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
                // 니가 병신같이 작성해서 내가 짠다 아래 절대 변경 금지.
                // 변경 금지 블럭 시작.

                foreach (var pos in detectedPositions)
                {
                    double dCurrentPos = Recipe.MappingStartPosition - pos;
                    double dSlotPitch = Config.SlotPitch;
                    bool bFirstCasset = true;
                    if (Recipe.MappingStartPosition + (dSlotPitch * Config.SlotCount) >= pos)
                    {
                        dCurrentPos -= (dSlotPitch * Config.SlotCount);
                        dCurrentPos -= Config.Level2PositionOffset;
                        bFirstCasset = false;
                    }
                    
                    int nIndex = (int)(dCurrentPos % dSlotPitch);
                    if(bFirstCasset == false)
                    {
                        nIndex += Config.SlotCount;
                    }
                    slotMap[nIndex] = true;
                    slotPositions[nIndex] = pos; ;
                }
                // 변경금지 블럭 끝
                
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
                //Todo: 0.8 파라미터로 빼야함.
                double pitchTolerance = slotPitch > 0.0 ? slotPitch * 0.8 : 0.0;
                if (pitchTolerance > 0.0)
                    return Math.Max(tolerance, pitchTolerance);
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

        private int ResolveNearestMappingSlotIndex(double position, int maxSlots, double tolerance)
        {
            int matchedSlot = -1;
            double bestError = double.MaxValue;

            for (int i = 0; i < maxSlots; i++)
            {
                double nominal = CalculateMappingSlotPosition(i);
                double error = Math.Abs(position - nominal);
                if (error <= tolerance && error < bestError)
                {
                    matchedSlot = i;
                    bestError = error;
                }
            }

            return matchedSlot;
        }

        private int FailMappingScan(string alarmCode, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", "InputCassetteMapping", message + " - Failed");
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
                return ResolveWaferLifterZConfigMoveVelocity();
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private double ResolveWaferLifterZMoveAcceleration(bool bFine)
        {
            return ResolveCassetteProfileAcceleration(ResolveWaferLifterZMoveVelocity(bFine));
        }

        private double ResolveWaferLifterZMoveDeceleration(bool bFine)
        {
            return ResolveCassetteProfileDeceleration(ResolveWaferLifterZMoveVelocity(bFine));
        }

        private double ResolveWaferLifterZConfigMoveVelocity()
        {
            double velocity = Config != null && Config.ScanVelocity > 0.0 ? Config.ScanVelocity : 0.0;
            if (velocity <= 0.0 && InputLifterZ != null && InputLifterZ.Config != null)
                velocity = InputLifterZ.Config.DefaultVelocity;
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
            string reason;
            return ValidateWaferLifterZTargetPosition(targetPos, out reason);
        }

        private bool ValidateWaferLifterZTargetPosition(double targetPos, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (InputLifterZ == null || InputLifterZ.Setup == null)
                {
                    reason = "InputLifterZ axis/setup is null. target=" + targetPos;
                    return false;
                }
                if (!InputLifterZ.Setup.SoftLimitEnabled)
                    return true;

                bool inRange = targetPos <= InputLifterZ.Setup.SoftLimitPlus &&
                               targetPos >= InputLifterZ.Setup.SoftLimitMinus;
                if (inRange)
                    return true;

                reason = "InputLifterZ target is out of soft limit. target=" + targetPos +
                         ", softMinus=" + InputLifterZ.Setup.SoftLimitMinus +
                         ", softPlus=" + InputLifterZ.Setup.SoftLimitPlus;
                return false;
            }
            catch (Exception ex)
            {
                reason = "InputLifterZ target validation failed: " + ex.Message + ". target=" + targetPos;
                return false;
            }
            finally
            {
            }
        }

        private string BuildCassetteSensorSummary()
        {
            try
            {
                return "Sensors: 8inch[0]=" + FormatInputState(Wafer8CassetteCheck0) +
                       ", 8inch[1]=" + FormatInputState(Wafer8CassetteCheck1) +
                       ", 12inch[0]=" + FormatInputState(Wafer12CassetteCheck0) +
                       ", 12inch[1]=" + FormatInputState(Wafer12CassetteCheck1) +
                       ", protrusion=" + FormatInputState(WaferRingJutCheck) + ".";
            }
            catch (Exception ex)
            {
                return "Sensor summary failed: " + ex.Message;
            }
            finally
            {
            }
        }

        private static string FormatInputState(BaseDigitalInput input)
        {
            if (input == null)
                return "NULL";

            return input.IsOn ? "ON" : "OFF";
        }

        // 마지막 Wafer Lifter Z 이동 실패 사유 — UI 실패 팝업에 합쳐 표시
        public string LastWaferLifterMoveFailureMessage { get; private set; }

        private void RaiseWaferCassetteConditionAlarm(string code, string message)
        {
            LastWaferLifterMoveFailureMessage = message;
            try
            {
                Log.Write("Main", "ALARM", code, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, code, Name, message);
            }
            catch
            {
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
            return await MoveWithProtrusionWatch(targetPosition, velocity, CancellationToken.None).ConfigureAwait(false);
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
            bool forceMove = false)
        {
            double oldAcceleration = 0.0;
            double oldDeceleration = 0.0;
            bool useCustomAcceleration = false;
            try
            {
                ct.ThrowIfCancellationRequested();

                if (IsWaferProtrusionDetected())
                {
                    LastWaferLifterMoveFailureMessage = "돌출 센서 감지로 이동 차단. target=" + targetPosition;
                    InputLifterZ.EStop();
                    Log.Write("Main", "MOTION", Name,
                        "Input cassette Z move blocked. Protrusion sensor is ON. target=" + targetPosition + " - Failed");
                    return -1;
                }

                oldAcceleration = InputLifterZ.Config != null ? InputLifterZ.Config.Acceleration : 0.0;
                oldDeceleration = InputLifterZ.Config != null ? InputLifterZ.Config.Deceleration : 0.0;
                useCustomAcceleration = InputLifterZ.Config != null && acceleration > 0.0 && deceleration > 0.0;
                if (useCustomAcceleration)
                {
                    InputLifterZ.Config.Acceleration = acceleration;
                    InputLifterZ.Config.Deceleration = deceleration;
                }

                Task<int> moveTask;
                if (forceMove)
                {
                    using (BaseAxis.BeginForceMoveScope())
                    {
                        moveTask = InputLifterZ.MoveAbsoluteAsync(targetPosition, velocity);
                    }
                }
                else
                {
                    moveTask = InputLifterZ.MoveAbsoluteAsync(targetPosition, velocity);
                }
                while (!moveTask.IsCompleted)
                {
                    ct.ThrowIfCancellationRequested();

                    if (IsWaferProtrusionDetected())
                    {
                        LastWaferLifterMoveFailureMessage = "이동 중 돌출 센서 감지로 정지. target=" + targetPosition;
                        InputLifterZ.EStop();
                        Log.Write("Main", "MOTION", Name,
                            "Input cassette Z move stopped. Protrusion detected while moving. target=" + targetPosition + " - Failed");
                        return -1;
                    }

                    await Task.Delay(10, ct).ConfigureAwait(false);
                }

                int moveResult = await moveTask.ConfigureAwait(false);
                if (useCustomAcceleration)
                {
                    InputLifterZ.Config.Acceleration = oldAcceleration;
                    InputLifterZ.Config.Deceleration = oldDeceleration;
                    useCustomAcceleration = false;
                }

                if (moveResult != 0 || InputLifterZ.IsAlarm)
                {
                    LastWaferLifterMoveFailureMessage = "Wafer Lifter Z 이동 명령 실패. result=" + moveResult +
                        ", alarm=" + InputLifterZ.IsAlarm + ", target=" + targetPosition;
                    Log.Write("Main", "MOTION", Name,
                        "Input cassette Z move command failed. result=" + moveResult +
                        ", velocity=" + velocity + ", target=" + targetPosition + " - Failed");
                    return moveResult != 0 ? moveResult : -1;
                }

                // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수(moveTask)가 완료를 보장하므로 제거(R3).
                return 0;
            }
            catch (OperationCanceledException)
            {
                try { InputLifterZ?.Stop(); } catch { }
                throw;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (useCustomAcceleration && InputLifterZ != null && InputLifterZ.Config != null)
                {
                    InputLifterZ.Config.Acceleration = oldAcceleration;
                    InputLifterZ.Config.Deceleration = oldDeceleration;
                }
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
            // To do: [레벨 분리 스캔] MappingStart/End = 1단, Level2* = 2단, FirstSlot = 2단(명칭 변경 호환), Level1FirstSlot = 1단.
            if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase)) return Recipe.MappingStartPosition;
            if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase)) return Recipe.MappingEndPosition;
            if (string.Equals(positionName, "Level2MappingStart", StringComparison.OrdinalIgnoreCase)) return Recipe.Level2MappingStartPosition;
            if (string.Equals(positionName, "Level2MappingEnd", StringComparison.OrdinalIgnoreCase)) return Recipe.Level2MappingEndPosition;
            if (string.Equals(positionName, "FirstSlot", StringComparison.OrdinalIgnoreCase)) return Recipe.Level2FirstSlotPosition;
            if (string.Equals(positionName, "Level1FirstSlot", StringComparison.OrdinalIgnoreCase)) return Recipe.Level1FirstSlotPosition;
            throw new ArgumentException("Unknown InputLifterZ teaching position: " + positionName, "positionName");
        }

        private void SetTeachingPosition(string positionName, double position)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) Recipe.AvoidPosition = position;
            else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase)) Recipe.MappingStartPosition = position;
            else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase)) Recipe.MappingEndPosition = position;
            else if (string.Equals(positionName, "Level2MappingStart", StringComparison.OrdinalIgnoreCase)) Recipe.Level2MappingStartPosition = position;
            else if (string.Equals(positionName, "Level2MappingEnd", StringComparison.OrdinalIgnoreCase)) Recipe.Level2MappingEndPosition = position;
            else if (string.Equals(positionName, "FirstSlot", StringComparison.OrdinalIgnoreCase)) Recipe.Level2FirstSlotPosition = position;
            else if (string.Equals(positionName, "Level1FirstSlot", StringComparison.OrdinalIgnoreCase)) Recipe.Level1FirstSlotPosition = position;
            else throw new ArgumentException("Unknown InputLifterZ teaching position: " + positionName, "positionName");
        }

        private bool IsAnyCassetteSensorOn()
        {
            if (IsDryRunInput(Wafer8CassetteCheck0) ||
                IsDryRunInput(Wafer12CassetteCheck0) ||
                Config.bDryRun ||
                (InputLifterZ != null && InputLifterZ.Config != null && InputLifterZ.Config.IsSimulationMode))
                return true;

            return Wafer8CassetteCheck0.IsOn || Wafer8CassetteCheck1.IsOn ||
                   Wafer12CassetteCheck0.IsOn || Wafer12CassetteCheck1.IsOn;
        }

        private void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Config.SlotCount)
                throw new ArgumentOutOfRangeException("slotIndex", "Slot index is out of cassette range.");
        }

        private void ValidateMappingSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= ResolveMappingSlotCount())
                throw new ArgumentOutOfRangeException("slotIndex", "Slot index is out of cassette mapping range.");
        }

        public int ResolveCassetteLevelCount()
        {
            return Config != null && Config.SelectedCassetteLevel >= 2 ? 2 : 1;
        }

        public int ResolveMappingSlotCount()
        {
            int slotCount = Config != null && Config.SlotCount > 0 ? Config.SlotCount : 0;
            return slotCount * ResolveCassetteLevelCount();
        }

        /// <summary>Config.SlotCount에 맞춰 Recipe.SlotPosition 버퍼를 보장합니다.</summary>
        public void EnsureSlotPositionBuffer()
        {
            Recipe.EnsureSlotPositionBuffer(ResolveMappingSlotCount());
        }

        /// <summary>Mapping용 SlotPosition 버퍼를 초기화합니다.</summary>
        public void ResetSlotPositionsForMapping()
        {
            Recipe.ResizeSlotPositions(ResolveMappingSlotCount());
        }

        /// <summary>지정 Slot의 Mapping 위치를 갱신합니다.</summary>
        public void UpdateSlotPosition(int slotIndex, double position)
        {
            ValidateMappingSlotIndex(slotIndex);
            EnsureSlotPositionBuffer();
            Recipe.UpdateSlotPosition(slotIndex, position);
        }

        // 기존 함수: 구식 명목 위치 계산(미사용). FirstSlotPosition 레벨 분리로 참조만 정리.
        // 현재 기준: 명목 위치는 CalculateMappingSlotPosition(레벨별 앵커)을 사용한다.
        public double CalculateNominalSlotPosition(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return CalculateMappingSlotPosition(ToFlatSlotIndex(1, slotIndex));
        }

        // To do: [맵핑 재설계] 전역 flat 인덱스 체계.
        //        flat 0 = 2단 맨 위 슬롯(전체 최상단, 제품 시작), 아래로 갈수록 증가,
        //        flat total-1 = 1단 맨 아래 슬롯 = Recipe.MappingStartPosition 앵커(유일 앵커).
        //        2단 구성: 0~N-1 = 2단(위 카세트), N~2N-1 = 1단(아래 카세트).
        //        1단 전용: 0~N-1 = 1단(0=맨 위, N-1=맨 아래 앵커).
        //        (level, local) 표기에서 local 0 = 해당 레벨의 맨 위 슬롯(UI 01번).
        public int ToFlatSlotIndex(int level, int localSlotIndex)
        {
            ValidateSlotIndex(localSlotIndex);
            int slotCount = Config != null ? Config.SlotCount : 0;
            if (ResolveCassetteLevelCount() >= 2 && level < 2)
                return slotCount + localSlotIndex;   // 1단(아래 카세트)은 뒤쪽 flat
            return localSlotIndex;                    // 2단(위 카세트) 또는 1단 전용은 앞쪽 flat
        }

        // To do: [맵핑 재설계] flat 인덱스 → 레벨(1/2). 2단 구성에서 앞쪽 절반 = 2단.
        public int ResolveLevelFromFlatIndex(int flatIndex)
        {
            int slotCount = Config != null ? Config.SlotCount : 0;
            if (ResolveCassetteLevelCount() >= 2 && flatIndex < slotCount)
                return 2;
            return 1;
        }

        // To do: [맵핑 재설계] flat 인덱스 → 레벨 내 local 인덱스(0=그 레벨 맨 위).
        public int ResolveLocalSlotFromFlatIndex(int flatIndex)
        {
            int slotCount = Config != null && Config.SlotCount > 0 ? Config.SlotCount : 1;
            return flatIndex % slotCount;
        }

        // 기존 공식: 단일 앵커(MappingStart) + Level2PositionOffset(G)로 2단까지 연속 계산했다.
        //           카세트 간 거치 오차로 2단 격자가 통째로 어긋나는 문제가 있었다.
        // To do: [레벨 분리 스캔] 레벨별 자기 앵커로 독립 계산한다. G(Level2PositionOffset)는 명목식에서 미사용.
        //        1단: anchor = MappingStartPosition(1단 맨아래 슬롯), pos = anchor - m*p (m=그 레벨 밑에서 슬롯 수)
        //        2단: anchor = Level2MappingStartPosition(2단 맨아래 슬롯), 동일 공식.
        //        피치는 단일 SlotPitch 공유. MappingEnd들은 슬롯 앵커가 아니라 각 레벨 스캔 끝 경계.
        public double CalculateMappingSlotPosition(int mappingSlotIndex)
        {
            ValidateMappingSlotIndex(mappingSlotIndex);

            int slotCount = Config.SlotCount;
            int level = ResolveLevelFromFlatIndex(mappingSlotIndex);
            int local = ResolveLocalSlotFromFlatIndex(mappingSlotIndex);
            int slotsFromBottom = (slotCount - 1) - local;   // local 0 = 그 레벨 맨 위

            double anchor = ResolveLevelMappingStartPosition(level) + Config.LoadingPositionOffset;
            return anchor - (Config.SlotPitch * slotsFromBottom);
        }

        // To do: [레벨 분리 스캔] 레벨별 스캔 앵커(그 레벨 맨아래 슬롯 티칭값).
        public double ResolveLevelMappingStartPosition(int level)
        {
            return level >= 2 ? Recipe.Level2MappingStartPosition : Recipe.MappingStartPosition;
        }

        // To do: [레벨 분리 스캔] 레벨별 스캔 끝 경계(그 레벨 맨위 슬롯 지나 센서 OFF 지점 티칭값).
        public double ResolveLevelMappingEndPosition(int level)
        {
            return level >= 2 ? Recipe.Level2MappingEndPosition : Recipe.MappingEndPosition;
        }

        // To do: [맵핑 재설계] (level, local) 래퍼 - flat 변환 후 단일 공식 사용(기존 호출부 호환).
        public double CalculateCassetteLevelSlotPosition(int level, int slotIndex)
        {
            return CalculateMappingSlotPosition(ToFlatSlotIndex(level, slotIndex));
        }

        private static async Task<int> WaitUntilAsync(Func<bool> condition, int timeoutMs)
        {
            return await WaitUntilAsync(condition, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        private static async Task<int> WaitUntilAsync(Func<bool> condition, int timeoutMs, CancellationToken ct)
        {
            try
            {
                int elapsed = 0;
                while (timeoutMs <= 0 || elapsed < timeoutMs)
                {
                    if (condition())
                        return 0;

                    await Task.Delay(10, ct).ConfigureAwait(false);
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
