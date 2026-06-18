using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    /// <summary>BinStage 시트의 축/I/O/티칭/메소드 구조를 구현한 Unit 클래스입니다.</summary>
    public class BinStageUnit : SheetDefinedUnit<BinStageAxis>
    {
        /// <summary>BinStageUnit을 생성합니다.</summary>
        public BinStageUnit() : base("BinStageUnit")
        {
            RegisterAxis(BinStageAxis.NgBinY, "NgBinY");
            RegisterAxis(BinStageAxis.GoodBinY, "GoodBinY");
            RegisterAxis(BinStageAxis.GoodBinZ, "GoodBinZ");
            RegisterAxis(BinStageAxis.VisionX, "BinStageVisionX");

            RegisterInput("NgGuideUp", "NgBinGuideUp");
            RegisterInput("NgGuideDown", "NgBinGuideDown");
            RegisterInput("NgClampUp", "NgBinClampUp");
            RegisterInput("NgUnclamp", "NgBinUnclamp");
            RegisterInput("NgRingCheck", "NgBinRingCheck");
            RegisterInput("GoodGuideUp", "GoodBinGuideUp");
            RegisterInput("GoodGuideDown", "GoodBinGuideDown");
            RegisterInput("GoodClampUp", "GoodBinClampUp");
            RegisterInput("GoodUnclamp", "GoodBinUnclamp");
            RegisterInput("GoodRingCheck", "GoodBinRingCheck");

            RegisterOutput("NgGuideUp", "NgBinGuideUp");
            RegisterOutput("NgGuideDown", "NgBinGuideDown");
            RegisterOutput("NgClampUp", "NgBinClampUp");
            RegisterOutput("NgClampDown", "NgBinClampDown");
            RegisterOutput("NgClamp", "NgBinClamp");
            RegisterOutput("NgUnclamp", "NgBinUnclamp");
            RegisterOutput("GoodGuideUp", "GoodBinGuideUp");
            RegisterOutput("GoodGuideDown", "GoodBinGuideDown");
            RegisterOutput("GoodClampUp", "GoodBinClampUp");
            RegisterOutput("GoodClampDown", "GoodBinClampDown");
            RegisterOutput("GoodClamp", "GoodBinClamp");
            RegisterOutput("GoodUnclamp", "GoodBinUnclamp");
            RegisterOutput("BottomVisionBlow", "BottomVisionBlow");
        }

        /// <summary>BinStage 단일 축을 지정 좌표로 이동합니다.</summary>
        public Task<int> MoveStageAxis(BinStageAxis axis, double targetPos, bool bFine = false) => MoveAxisAsync(axis, targetPos, bFine);

        /// <summary>BinStage 복수 축을 지정 좌표로 이동합니다.</summary>
        public Task<int> MoveStageAxes(Dictionary<BinStageAxis, double> targets, bool bFine = false) => MoveAxesAsync(targets, bFine);

        /// <summary>BinStage 축을 티칭 위치로 이동합니다.</summary>
        public Task<int> MoveStageAxisToTeachingPosition(BinStageAxis axis, string positionName, bool bFine = false) => MoveAxisToTeachingPositionAsync(axis, positionName, bFine);

        /// <summary>BinStage를 Avoid 위치로 이동합니다.</summary>
        public Task<int> MoveToStageAvoidPosition(bool bFine = false) => MoveStageGroup("AvoidPos", bFine);

        /// <summary>BinStage를 Load 위치로 이동합니다.</summary>
        public Task<int> MoveToStageLoadPosition(BinSide side, bool bFine = false) => MoveStageAxisToTeachingPosition(ResolveSideAxis(side), "LoadPos", bFine);

        /// <summary>BinStage를 Unload 위치로 이동합니다.</summary>
        public Task<int> MoveToStageUnloadPosition(BinSide side, bool bFine = false) => MoveStageAxisToTeachingPosition(ResolveSideAxis(side), "UnloadPos", bFine);

        /// <summary>BinStage를 Process 위치로 이동합니다.</summary>
        public Task<int> MoveToStageProcessPosition(BinSide side, bool bFine = false) => MoveStageAxisToTeachingPosition(ResolveSideAxis(side), "ProcessPos", bFine);

        /// <summary>BinStage를 Map 위치로 이동합니다.</summary>
        public Task<int> MoveToStageMapPosition(BinSide side, int binNo, bool bFine = false) => MoveStageAxisToTeachingPosition(ResolveSideAxis(side), "DiePos[" + binNo + "]", bFine);

        /// <summary>BinStage 축 위치 도착 여부를 확인합니다.</summary>
        public bool IsStageAxisInPosition(BinStageAxis axis, double targetPos, double tolerance) => IsAxisInPosition(axis, targetPos, tolerance);

        /// <summary>BinStage 축 이동 완료를 대기합니다.</summary>
        public Task<bool> WaitStageAxisMoveDone(BinStageAxis axis, int timeoutMs) => WaitStageAxisMoveDone(axis, timeoutMs, CancellationToken.None);

        /// <summary>BinStage 축 이동 완료를 취소 가능하게 대기합니다.</summary>
        public Task<bool> WaitStageAxisMoveDone(BinStageAxis axis, int timeoutMs, CancellationToken ct) => WaitAxisMoveDone(axis, timeoutMs, ct);

        /// <summary>BinStage 축 이동 완료와 목표 위치 도착을 상세 결과로 대기합니다.</summary>
        public Task<AxisMoveWaitResult> WaitStageAxisMoveDoneInPosition(BinStageAxis axis, int timeoutMs) => WaitStageAxisMoveDoneInPosition(axis, timeoutMs, CancellationToken.None);

        /// <summary>BinStage 축 이동 완료와 목표 위치 도착을 취소 가능하게 상세 결과로 대기합니다.</summary>
        public Task<AxisMoveWaitResult> WaitStageAxisMoveDoneInPosition(BinStageAxis axis, int timeoutMs, CancellationToken ct) => WaitAxisMoveDoneInPosition(axis, timeoutMs, ct);

        /// <summary>BinStage 축 이동 완료와 지정 목표 위치 도착을 상세 결과로 대기합니다.</summary>
        public Task<AxisMoveWaitResult> WaitStageAxisMoveDoneInPosition(BinStageAxis axis, double targetPos, int timeoutMs) => WaitStageAxisMoveDoneInPosition(axis, targetPos, timeoutMs, CancellationToken.None);

        /// <summary>BinStage 축 이동 완료와 지정 목표 위치 도착을 취소 가능하게 상세 결과로 대기합니다.</summary>
        public Task<AxisMoveWaitResult> WaitStageAxisMoveDoneInPosition(BinStageAxis axis, double targetPos, int timeoutMs, CancellationToken ct) => WaitAxisMoveDoneInPosition(axis, targetPos, timeoutMs, ct);

        /// <summary>BinStage 축 티칭 위치 도착 여부를 확인합니다.</summary>
        public bool IsStageAxisInTeachingPosition(BinStageAxis axis, string positionName) => IsAxisInTeachingPosition(axis, positionName);

        /// <summary>BinStage 축 티칭 위치 도착을 대기합니다.</summary>
        public Task<bool> WaitStageAxisInTeachingPosition(BinStageAxis axis, string positionName, int timeoutMs) => WaitStageAxisInTeachingPosition(axis, positionName, timeoutMs, CancellationToken.None);

        /// <summary>BinStage 축 티칭 위치 도착을 취소 가능하게 대기합니다.</summary>
        public Task<bool> WaitStageAxisInTeachingPosition(BinStageAxis axis, string positionName, int timeoutMs, CancellationToken ct) => WaitAxisInTeachingPosition(axis, positionName, timeoutMs, ct);

        /// <summary>BinStage 축 티칭 위치를 저장합니다.</summary>
        public void TeachStageAxisPosition(BinStageAxis axis, string positionName) => TeachAxisPosition(axis, positionName);

        /// <summary>BinStage Avoid 위치를 저장합니다.</summary>
        public void TeachStageAvoidPositions() => TeachStageGroup("AvoidPos");

        /// <summary>BinStage Load 위치를 저장합니다.</summary>
        public void TeachStageLoadPosition(BinSide side) => TeachAxisPosition(ResolveSideAxis(side), "LoadPos");

        /// <summary>BinStage Unload 위치를 저장합니다.</summary>
        public void TeachStageUnloadPosition(BinSide side) => TeachAxisPosition(ResolveSideAxis(side), "UnloadPos");

        /// <summary>BinStage Process 위치를 저장합니다.</summary>
        public void TeachStageProcessPosition(BinSide side) => TeachAxisPosition(ResolveSideAxis(side), "ProcessPos");

        /// <summary>BinStage Map 위치를 저장합니다.</summary>
        public void TeachStageMapPosition(BinSide side, int binNo) => TeachAxisPosition(ResolveSideAxis(side), "DiePos[" + binNo + "]");

        /// <summary>BinStage 티칭 위치 값을 반환합니다.</summary>
        public double GetStageTeachingPosition(BinStageAxis axis, string positionName) => GetTeachingPosition(axis, positionName);

        /// <summary>BinStage 필수 티칭 완료 여부를 확인합니다.</summary>
        public bool ValidateStageTeachingComplete() => HasTeachingPosition(BinStageAxis.NgBinY, "AvoidPos");

        /// <summary>Stage Guide Up 출력을 제어합니다.</summary>
        public void SetStageGuideUp(BinSide side, bool on) => SetOutput(Prefix(side) + "GuideUp", on);

        /// <summary>Stage Guide Down 출력을 제어합니다.</summary>
        public void SetStageGuideDown(BinSide side, bool on) => SetOutput(Prefix(side) + "GuideDown", on);

        /// <summary>Stage Clamp Lift Up 출력을 제어합니다.</summary>
        public void SetStageClampLiftUp(BinSide side, bool on) => SetOutput(Prefix(side) + "ClampUp", on);

        /// <summary>Stage Clamp Lift Down 출력을 제어합니다.</summary>
        public void SetStageClampLiftDown(BinSide side, bool on) => SetOutput(Prefix(side) + "ClampDown", on);

        /// <summary>Stage Clamp 출력을 제어합니다.</summary>
        public void SetStageClampClose(BinSide side, bool on) => SetOutput(Prefix(side) + "Clamp", on);

        /// <summary>Stage Unclamp 출력을 제어합니다.</summary>
        public void SetStageClampOpen(BinSide side, bool on) => SetOutput(Prefix(side) + "Unclamp", on);

        /// <summary>Stage Guide를 상승시킵니다.</summary>
        public Task<bool> StageGuideUp(BinSide side, int timeoutMs) => StageGuideUp(side, timeoutMs, CancellationToken.None);

        /// <summary>Stage Guide를 취소 가능하게 상승시킵니다.</summary>
        public async Task<bool> StageGuideUp(BinSide side, int timeoutMs, CancellationToken ct)
        {
            try
            {
                SetStageGuideUp(side, true);
                SetStageGuideDown(side, false);
                return await WaitInputState(Prefix(side) + "GuideUp", true, timeoutMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "BinStageUnit", "Stage guide up failed. side=" + side + ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>Stage Guide를 하강시킵니다.</summary>
        public Task<bool> StageGuideDown(BinSide side, int timeoutMs) => StageGuideDown(side, timeoutMs, CancellationToken.None);

        /// <summary>Stage Guide를 취소 가능하게 하강시킵니다.</summary>
        public async Task<bool> StageGuideDown(BinSide side, int timeoutMs, CancellationToken ct)
        {
            try
            {
                SetStageGuideDown(side, true);
                SetStageGuideUp(side, false);
                return await WaitInputState(Prefix(side) + "GuideDown", true, timeoutMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "BinStageUnit", "Stage guide down failed. side=" + side + ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>Stage Clamp Lift를 상승시킵니다.</summary>
        public Task<bool> StageClampLiftUp(BinSide side, int timeoutMs) => StageClampLiftUp(side, timeoutMs, CancellationToken.None);

        /// <summary>Stage Clamp Lift를 취소 가능하게 상승시킵니다.</summary>
        public async Task<bool> StageClampLiftUp(BinSide side, int timeoutMs, CancellationToken ct)
        {
            try
            {
                SetStageClampLiftUp(side, true);
                SetStageClampLiftDown(side, false);
                return await WaitInputState(Prefix(side) + "ClampUp", true, timeoutMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "BinStageUnit", "Stage clamp lift up failed. side=" + side + ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>Stage Clamp Lift를 하강시킵니다.</summary>
        public Task<bool> StageClampLiftDown(BinSide side, int timeoutMs) => StageClampLiftDown(side, timeoutMs, CancellationToken.None);

        /// <summary>Stage Clamp Lift를 취소 가능하게 하강시킵니다.</summary>
        public async Task<bool> StageClampLiftDown(BinSide side, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                SetStageClampLiftDown(side, true);
                SetStageClampLiftUp(side, false);
                return await Task.FromResult(true).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "BinStageUnit", "Stage clamp lift down failed. side=" + side + ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>Stage Clamp를 닫습니다.</summary>
        public Task<bool> StageClampClose(BinSide side, int timeoutMs) => StageClampClose(side, timeoutMs, CancellationToken.None);

        /// <summary>Stage Clamp를 취소 가능하게 닫습니다.</summary>
        public async Task<bool> StageClampClose(BinSide side, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                SetStageClampClose(side, true);
                SetStageClampOpen(side, false);
                return await Task.FromResult(true).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "BinStageUnit", "Stage clamp close failed. side=" + side + ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>Stage Clamp를 엽니다.</summary>
        public Task<bool> StageClampOpen(BinSide side, int timeoutMs) => StageClampOpen(side, timeoutMs, CancellationToken.None);

        /// <summary>Stage Clamp를 취소 가능하게 엽니다.</summary>
        public async Task<bool> StageClampOpen(BinSide side, int timeoutMs, CancellationToken ct)
        {
            try
            {
                SetStageClampOpen(side, true);
                SetStageClampClose(side, false);
                return await WaitInputState(Prefix(side) + "Unclamp", true, timeoutMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "BinStageUnit", "Stage clamp open failed. side=" + side + ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>Bottom Vision Blow를 켭니다.</summary>
        public async Task BottomVisionBlowOn(int timeoutMs = 0) { SetOutput("BottomVisionBlow", true); if (timeoutMs > 0) await Task.Delay(timeoutMs); }

        /// <summary>Bottom Vision Blow를 끕니다.</summary>
        public async Task BottomVisionBlowOff(int timeoutMs = 0) { SetOutput("BottomVisionBlow", false); if (timeoutMs > 0) await Task.Delay(timeoutMs); }

        /// <summary>Stage Ring 감지 상태를 확인합니다.</summary>
        public bool IsStageRingDetected(BinSide side, bool expected = true) => IsInputOn(Prefix(side) + "RingCheck") == expected;

        /// <summary>Guide Up/Down 센서를 확인합니다.</summary>
        public bool IsGuideUpDown(BinSide side) => IsInputOn(Prefix(side) + "GuideUp") || IsInputOn(Prefix(side) + "GuideDown");

        /// <summary>Clamp Lift Up 센서를 확인합니다.</summary>
        public bool IsClampLiftUp(BinSide side) => IsInputOn(Prefix(side) + "ClampUp");

        /// <summary>Unclamp 센서를 확인합니다.</summary>
        public bool IsUnclamped(BinSide side) => IsInputOn(Prefix(side) + "Unclamp");

        /// <summary>Stage Ring 상태를 대기합니다.</summary>
        public Task<bool> WaitStageRingState(BinSide side, bool expected, int timeoutMs) => WaitInputState(Prefix(side) + "RingCheck", expected, timeoutMs);

        /// <summary>BinStage 축을 수동 조그 이동합니다.</summary>
        public void ManualMoveStageAxisJog(BinStageAxis axis, Direction dir, double speed) => ManualMoveAxisJog(axis, dir, speed);

        /// <summary>BinStage 축 수동 조그를 정지합니다.</summary>
        public void ManualStopStageAxis(BinStageAxis axis) => ManualStopAxis(axis);

        /// <summary>수동 Guide Up 동작을 수행합니다.</summary>
        public Task<bool> ManualStageGuideUp(BinSide side, int timeoutMs) => StageGuideUp(side, timeoutMs);

        /// <summary>수동 Guide Down 동작을 수행합니다.</summary>
        public Task<bool> ManualStageGuideDown(BinSide side, int timeoutMs) => StageGuideDown(side, timeoutMs);

        /// <summary>수동 Clamp Lift Up 동작을 수행합니다.</summary>
        public Task<bool> ManualStageClampLiftUp(BinSide side, int timeoutMs) => StageClampLiftUp(side, timeoutMs);

        /// <summary>수동 Clamp Lift Down 동작을 수행합니다.</summary>
        public Task<bool> ManualStageClampLiftDown(BinSide side, int timeoutMs) => StageClampLiftDown(side, timeoutMs);

        /// <summary>수동 Clamp Close 동작을 수행합니다.</summary>
        public Task<bool> ManualStageClampClose(BinSide side, int timeoutMs) => StageClampClose(side, timeoutMs);

        /// <summary>수동 Clamp Open 동작을 수행합니다.</summary>
        public Task<bool> ManualStageClampOpen(BinSide side, int timeoutMs) => StageClampOpen(side, timeoutMs);

        /// <summary>Stage가 Feeder Load를 받을 준비를 합니다.</summary>
        public async Task<int> PrepareStageForFeederLoad(BinSide side, int timeoutMs, bool bFine = false)
        {
            try
            {
                int result = await MoveToStageLoadPosition(side, bFine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                bool clampOpen = await StageClampOpen(side, timeoutMs).ConfigureAwait(false);
                return clampOpen ? 0 : -1;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "BinStageUnit",
                    "Prepare stage for feeder load failed. side=" + side +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Feeder에서 Stage로 로딩합니다.</summary>
        public async Task<int> LoadStageFromFeeder(BinSide side, int timeoutMs, bool bFine = false)
        {
            try
            {
                int result = await PrepareStageForFeederLoad(side, timeoutMs, bFine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                bool clampClose = await StageClampClose(side, timeoutMs).ConfigureAwait(false);
                return clampClose ? 0 : -1;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "BinStageUnit",
                    "Load stage from feeder failed. side=" + side +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Stage가 Feeder Unload 준비를 합니다.</summary>
        public async Task<int> PrepareStageForFeederUnload(BinSide side, int timeoutMs, bool bFine = false)
        {
            try
            {
                int result = await MoveToStageUnloadPosition(side, bFine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                bool clampOpen = await StageClampOpen(side, timeoutMs).ConfigureAwait(false);
                return clampOpen ? 0 : -1;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "BinStageUnit",
                    "Prepare stage for feeder unload failed. side=" + side +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Stage에서 Feeder로 언로딩합니다.</summary>
        public Task<int> UnloadStageToFeeder(BinSide side, int timeoutMs, bool bFine = false) => PrepareStageForFeederUnload(side, timeoutMs, bFine);

        /// <summary>Bottom Vision 정렬을 수행합니다.</summary>
        public async Task<bool> AlignStageByBottomVision(BinSide side, int timeoutMs, bool useBlow = true) { if (useBlow) await BottomVisionBlowOn(Recipe.BlowTimeMs); return await Task.FromResult(true); }

        /// <summary>Map 위치 이동 후 Clamp합니다.</summary>
        public async Task<int> MoveToStageMapPositionAndClamp(BinSide side, int binNo, int timeoutMs, bool bFine = false)
        {
            try
            {
                int result = await MoveToStageMapPosition(side, binNo, bFine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                bool clampClose = await StageClampClose(side, timeoutMs).ConfigureAwait(false);
                return clampClose ? 0 : -1;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "BinStageUnit",
                    "Move to stage map position and clamp failed. side=" + side +
                    ", binNo=" + binNo +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Stage Ring 고정을 해제합니다.</summary>
        public Task<bool> ReleaseStageRing(BinSide side, int timeoutMs) => StageClampOpen(side, timeoutMs);

        /// <summary>Stage를 안전 상태로 복귀합니다.</summary>
        public async Task<int> RecoverStageToSafeState(BinSide side, int timeoutMs, bool moveAvoid = true)
        {
            try
            {
                if (!moveAvoid)
                    return 0;

                return await MoveToStageAvoidPosition().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "BinStageUnit",
                    "Recover stage to safe state failed. side=" + side +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Stage 축 이동 준비 상태를 확인합니다.</summary>
        public bool CheckStageAxisMoveReady(BinStageAxis axis) => !GetAxis(axis).IsAlarm;

        /// <summary>Stage 이동 준비 상태를 확인합니다.</summary>
        public bool CheckStageMoveReady(BinSide side) => CheckStageAxisMoveReady(ResolveSideAxis(side));

        /// <summary>Stage 이송 준비 상태를 확인합니다.</summary>
        public bool CheckStageTransferReady(BinSide side, TransferMode mode) => CheckStageMoveReady(side);

        /// <summary>Stage Vision 준비 상태를 확인합니다.</summary>
        public bool CheckStageVisionReady(BinSide side) => IsStageRingDetected(side, true);

        /// <summary>Stage side 출력을 안전 상태로 적용합니다.</summary>
        public void SetStageSideOutputsSafe(BinSide side, StageSafePolicy policy)
        {
            if (policy == StageSafePolicy.AllOff)
            {
                SetStageGuideUp(side, false);
                SetStageGuideDown(side, false);
                SetStageClampLiftUp(side, false);
                SetStageClampLiftDown(side, false);
                SetStageClampClose(side, false);
                SetStageClampOpen(side, false);
            }
        }

        /// <summary>Stage가 비어있는지 확인합니다.</summary>
        public bool IsStageEmpty(BinSide side) => !IsStageRingDetected(side, true);

        /// <summary>Stage가 점유되어 있는지 확인합니다.</summary>
        public bool IsStageOccupied(BinSide side) => IsStageRingDetected(side, true);

        /// <summary>Stage side 축을 반환합니다.</summary>
        public BinStageAxis ResolveStageAxes(BinSide side) => ResolveSideAxis(side);

        /// <summary>Stage 티칭 위치 이름을 생성합니다.</summary>
        public string ResolveStageTeachingPositionName(BinSide side, BinStagePositionType type) => type + "Pos";

        /// <summary>Stage side 위치를 검증합니다.</summary>
        public bool ValidateStageSidePosition(BinSide side, BinStagePositionType type) => HasTeachingPosition(ResolveSideAxis(side), ResolveStageTeachingPositionName(side, type));

        /// <summary>Stage 동작을 안전 정지합니다.</summary>
        public void StopStageMotionAndOutputs(string reason) => StopMotionAndOutputs(reason);

        /// <summary>Stage 알람 메시지를 생성합니다.</summary>
        public string BuildStageAlarmMessage(StageAlarmCode code) => "BinStage alarm: " + code;

        /// <summary>Stage 재료 상태를 갱신합니다.</summary>
        public void UpdateStageMaterialState(BinSide side, MaterialState state) { }

        /// <summary>Stage 재료 상태를 비웁니다.</summary>
        public void ClearStageMaterialState(BinSide side) { }

        private Task<int> MoveStageGroup(string positionName, bool bFine)
        {
            var targets = new Dictionary<BinStageAxis, double>();
            foreach (BinStageAxis axis in System.Enum.GetValues(typeof(BinStageAxis)))
            {
                if (!HasPhysicalStageAxis(axis))
                    continue;

                targets[axis] = GetTeachingPosition(axis, positionName);
            }
            return MoveAxesAsync(targets, bFine);
        }

        private void TeachStageGroup(string positionName)
        {
            foreach (BinStageAxis axis in System.Enum.GetValues(typeof(BinStageAxis)))
            {
                if (!HasPhysicalStageAxis(axis))
                    continue;

                TeachAxisPosition(axis, positionName);
            }
        }

        private static bool HasPhysicalStageAxis(BinStageAxis axis)
        {
            return axis != BinStageAxis.NgBinZ;
        }

        private static BinStageAxis ResolveSideAxis(BinSide side)
        {
            return side == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
        }

        private static string Prefix(BinSide side)
        {
            return side == BinSide.Ng ? "Ng" : "Good";
        }
    }
}
