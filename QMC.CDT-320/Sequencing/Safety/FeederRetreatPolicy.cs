using System;
using System.Threading;
using System.Threading.Tasks;
// [옵션 2026-07-29] Ready 전용 이탈 이동에서 축 소프트리밋과 Jog 속도 타입을 참조한다.
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing.Safety
{
    /// <summary>
    /// 피더 후퇴 단계. 실패 시 어느 단계에서 막혔는지 호출부가 알람 코드를 고르는 데 사용한다.
    /// </summary>
    public enum FeederRetreatPhase
    {
        None,
        Unclamp,
        LiftUpBlocked,
        BackOff,     // [옵션 2026-07-29] Unclamp 후 Lift Up 전 +방향 이탈 이동
        LiftUp,
        MoveAvoid,
        LiftDown
    }

    /// <summary>피더 후퇴 결과.</summary>
    public sealed class FeederRetreatResult
    {
        public bool Success { get; set; }
        public FeederRetreatPhase FailedPhase { get; set; }
        public int CommandResult { get; set; }
        public string Message { get; set; }

        public static FeederRetreatResult Ok()
        {
            return new FeederRetreatResult { Success = true, FailedPhase = FeederRetreatPhase.None, Message = "" };
        }

        public static FeederRetreatResult Failed(FeederRetreatPhase phase, int commandResult, string message)
        {
            return new FeederRetreatResult
            {
                Success = false,
                FailedPhase = phase,
                CommandResult = commandResult,
                Message = message ?? ""
            };
        }
    }

    /// <summary>
    /// 후퇴 대상 피더. Input/Output 유닛의 메서드 이름이 달라 어댑터로 감싼다.
    /// 구현은 이 파일 아래의 InputFeederRetreatTarget / OutputFeederRetreatTarget.
    /// </summary>
    /// <summary>피더 후퇴 공용 상수.</summary>
    internal static class FeederRetreatLimits
    {
        /// <summary>[옵션 2026-07-29] 이탈 이동 시 소프트리밋에서 남겨둘 여유(mm).</summary>
        internal const double SoftLimitMarginMm = 0.5;
    }

    public interface IFeederRetreatTarget
    {
        string Name { get; }

        bool IsUnclamped();
        bool IsEmpty();
        bool IsUp();
        bool IsDown();
        bool IsInAvoidPosition();
        bool IsOverloaded();
        string DescribeState();

        Task<int> UnclampAsync(int timeoutMs, CancellationToken ct);
        Task<int> SetLiftAsync(bool up, int timeoutMs, CancellationToken ct);

        /// <summary>Avoid 위치로 이동하고 완료까지 대기한다.</summary>
        Task<int> MoveToAvoidAsync(int timeoutMs, CancellationToken ct);

        /// <summary>
        /// [옵션 2026-07-29] 현재 위치에서 상대 이동하고 완료까지 대기한다. Ready 전용 이탈 이동에 쓴다.
        /// 지원하지 않는 구현은 null 을 돌려주면 정책이 이 단계를 건너뛴다.
        /// 구현 측에서 소프트리밋 안쪽으로 클램프할 책임을 진다.
        /// </summary>
        Func<double, int, CancellationToken, Task<int>> MoveRelativeAsync { get; }
    }

    /// <summary>
    /// 피더를 안전 대기 자세(Avoid + Lift Down)로 되돌리는 단일 규칙.
    ///
    /// 순서 (2026-07-27 확정, 생산 시퀀스와 동일):
    ///   Unclamp -> [보유 확인] -> Lift Up -> Y Avoid -> Lift Down
    ///
    /// 왜 이 순서인가:
    ///  - Lift Down 상태로 Y를 움직이면 스테이지에 놓인 자재를 누르고 긁는다.
    ///    (2026-07-27 현장 확인: 언로드 위치에서 정지 후 재시작 시 발생)
    ///    생산 시퀀스 OutputFeederLoadToStageSequence도
    ///    PrepareFeederLiftUp -> MoveFeederAvoidPosition -> PrepareFeederLiftDownAfterAvoid 순서다.
    ///  - 자재를 문 채로 Lift Up 하면 피더가 파손된다(사용자 확정 2026-07-27).
    ///    그래서 Lift Up 직전에 Unclamp 센서와 피더 공백을 다시 확인하고,
    ///    하나라도 아니면 올리지 않고 실패로 끝낸다(fail-closed).
    ///
    /// 이 규칙을 복사하지 말고 이 메서드를 호출할 것. 호출부는 SEQUENCE_MAP.md 3-1 표 참고.
    /// </summary>
    public static class FeederRetreatPolicy
    {
        /// <param name="backOffMm">
        /// [옵션 2026-07-29 / Ready 전용] Unclamp 후 Lift Up 전에 +방향으로 이탈시킬 거리(mm).
        /// 0 이면 수행하지 않는다(기본). Ready 시퀀스만 값을 넘기며, 생산/개별 Recover 경로는 0 이다.
        /// 대상 유닛이 MoveRelativeAsync 를 지원하지 않으면 조용히 건너뛴다.
        /// ★소프트리밋 주의★ InputFeederY 는 SoftLimitPlus=629.78 이고 Load/Unload 위치가 607.72 라
        /// +20mm 면 여유가 2.06mm 뿐이다. 실제 이동량은 유닛 구현에서 소프트리밋 안쪽으로 클램프된다.
        /// </param>
        public static async Task<FeederRetreatResult> RetreatToAvoidAsync(
            IFeederRetreatTarget feeder,
            int ioTimeoutMs,
            int moveTimeoutMs,
            CancellationToken ct,
            double backOffMm = 0.0)
        {
            if (feeder == null)
                return FeederRetreatResult.Failed(FeederRetreatPhase.None, -1, "피더 유닛이 없습니다.");

            ct.ThrowIfCancellationRequested();

            // 1) Unclamp: 명령 결과와 실제 센서 상태를 함께 확인한다.
            if (!feeder.IsUnclamped())
            {
                int unclampResult = await feeder.UnclampAsync(ioTimeoutMs, ct).ConfigureAwait(false);
                if (unclampResult != 0 || !feeder.IsUnclamped())
                {
                    return FeederRetreatResult.Failed(FeederRetreatPhase.Unclamp, unclampResult,
                        "Unclamp 실패. result=" + unclampResult + ". " + feeder.DescribeState());
                }
            }

            // 2) [사용자 지시 2026-07-29] 제품 보유 여부로 분기한다.
            //      · 보유 X → 곧바로 Lift Up (2-1 생략)
            //      · 보유 O → +방향으로 이탈시킨 뒤 다시 판정하고, 그때 비워졌으면 Lift Up
            //    자재를 문 상태로 Lift Up 하면 피더가 파손되므로, 최종적으로 "비어 있음"이
            //    확인되지 않으면 어떤 경우에도 올리지 않는다(아래 재판정에서 fail-closed).
            if (!feeder.IsUnclamped() || !feeder.IsEmpty())
            {
                // 2-1) [옵션 / Ready 전용] +방향 이탈 이동.
                //      backOffMm == 0 이면 시도하지 않고 기존처럼 즉시 차단한다
                //      (생산 시퀀스와 개별 Recover 경로는 항상 0 이므로 동작이 바뀌지 않는다).
                //      Lift Down 상태에서 Y 가 움직이는 유일한 구간이라 거리를 짧게 유지할 것.
                bool escaped = false;
                if (backOffMm > 0.0 && feeder.IsUnclamped() && feeder.MoveRelativeAsync != null)
                {
                    int backOffResult = await feeder.MoveRelativeAsync(backOffMm, moveTimeoutMs, ct).ConfigureAwait(false);
                    if (backOffResult != 0)
                        return FeederRetreatResult.Failed(FeederRetreatPhase.BackOff, backOffResult,
                            "제품 보유 상태에서 +방향 이탈 이동을 완료하지 못했습니다. " +
                            "요청 거리를 전부 이동하지 못하면 제품이 어중간하게 걸릴 수 있어 이동하지 않습니다. " +
                            "★피더와 제품 상태를 육안으로 확인한 뒤 제품을 제거하고 Material DATA를 CLEAR 한 다음 Ready 바랍니다.★ " +
                            "backOffMm=" + backOffMm.ToString("0.###") + ", result=" + backOffResult +
                            ". " + feeder.DescribeState());

                    // 이탈 후 재판정. 여전히 물고 있으면 올리지 않는다.
                    escaped = feeder.IsUnclamped() && feeder.IsEmpty();
                }

                if (!escaped)
                {
                    return FeederRetreatResult.Failed(FeederRetreatPhase.LiftUpBlocked, -1,
                        "Lift Up 차단: 자재를 보유했거나 Unclamp가 확인되지 않았습니다. " +
                        "자재를 문 상태로 Lift Up 하면 피더가 파손됩니다. " +
                        "★제품을 육안으로 확인해 제거하고 Material DATA를 CLEAR 한 뒤 Ready 바랍니다.★ " +
                        "unclamped=" + feeder.IsUnclamped() +
                        ", empty=" + feeder.IsEmpty() +
                        ", backOffMm=" + backOffMm.ToString("0.###") + ". " + feeder.DescribeState());
                }
            }

            // 3) Lift Up: 스테이지 위에서 정지했을 수 있으므로 Y 이동 전에 반드시 들어올린다.
            if (!feeder.IsUp())
            {
                int upResult = await feeder.SetLiftAsync(true, ioTimeoutMs, ct).ConfigureAwait(false);
                if (upResult != 0 || !feeder.IsUp())
                {
                    return FeederRetreatResult.Failed(FeederRetreatPhase.LiftUp, upResult,
                        "Lift Up 실패. result=" + upResult + ". " + feeder.DescribeState());
                }
            }

            // 4) Y Avoid 이동 및 완료 확인.
            if (!feeder.IsInAvoidPosition())
            {
                int moveResult = await feeder.MoveToAvoidAsync(moveTimeoutMs, ct).ConfigureAwait(false);
                if (moveResult != 0)
                {
                    return FeederRetreatResult.Failed(FeederRetreatPhase.MoveAvoid, moveResult,
                        "Avoid 이동 실패. result=" + moveResult + ". " + feeder.DescribeState());
                }
            }

            if (!feeder.IsInAvoidPosition() || feeder.IsOverloaded())
            {
                return FeederRetreatResult.Failed(FeederRetreatPhase.MoveAvoid, -1,
                    "Avoid 도착 확인 실패. inAvoid=" + feeder.IsInAvoidPosition() +
                    ", overload=" + feeder.IsOverloaded() + ". " + feeder.DescribeState());
            }

            // 5) Lift Down: Avoid 도착 후에만 내린다.
            if (!feeder.IsDown())
            {
                int downResult = await feeder.SetLiftAsync(false, ioTimeoutMs, ct).ConfigureAwait(false);
                if (downResult != 0 || !feeder.IsDown())
                {
                    return FeederRetreatResult.Failed(FeederRetreatPhase.LiftDown, downResult,
                        "Avoid 도착 후 Lift Down 실패. result=" + downResult + ". " + feeder.DescribeState());
                }
            }

            return FeederRetreatResult.Ok();
        }
    }

    /// <summary>InputFeederUnit 어댑터.</summary>
    public sealed class InputFeederRetreatTarget : IFeederRetreatTarget
    {
        private readonly InputFeederUnit _unit;
        private readonly Func<int, CancellationToken, Task<int>> _moveToAvoidOverride;

        /// <param name="moveToAvoidOverride">
        /// Avoid 이동을 호출부가 직접 처리해야 할 때 사용한다(예: Ready 복구의 Jog Coarse 속도).
        /// null이면 유닛 기본 Avoid 이동을 사용한다.
        /// </param>
        public InputFeederRetreatTarget(
            InputFeederUnit unit,
            Func<int, CancellationToken, Task<int>> moveToAvoidOverride = null)
        {
            _unit = unit;
            _moveToAvoidOverride = moveToAvoidOverride;
        }

        public string Name { get { return _unit != null ? _unit.Name : "InputFeeder"; } }

        public bool IsUnclamped() { return _unit != null && _unit.IsWaferFeederUnclamp(); }
        public bool IsEmpty() { return _unit != null && _unit.IsWaferFeederEmpty(); }
        public bool IsUp() { return _unit != null && _unit.IsWaferFeederUp(); }
        public bool IsDown() { return _unit != null && _unit.IsWaferFeederDown(); }
        public bool IsInAvoidPosition() { return _unit != null && _unit.IsWaferFeederInAvoidPosition(); }
        public bool IsOverloaded() { return _unit != null && _unit.IsWaferFeederOverload(); }
        public string DescribeState() { return _unit != null ? _unit.GetWaferFeederTransferState() : "InputFeeder=null"; }

        public Task<int> UnclampAsync(int timeoutMs, CancellationToken ct)
        {
            return _unit.SetWaferFeederClampAsync(false, timeoutMs, ct);
        }

        public Task<int> SetLiftAsync(bool up, int timeoutMs, CancellationToken ct)
        {
            return _unit.SetWaferFeederUpDownAsync(up, timeoutMs, ct);
        }

        public async Task<int> MoveToAvoidAsync(int timeoutMs, CancellationToken ct)
        {
            if (_moveToAvoidOverride != null)
                return await _moveToAvoidOverride(timeoutMs, ct).ConfigureAwait(false);

            int result = await _unit.MoveToWaferFeederAvoidPosition(false).ConfigureAwait(false);
            if (result != 0)
                return result;

            double target = _unit.Recipe != null ? _unit.Recipe.AvoidPosition : 0.0;
            return await _unit.WaitWaferFeederYMoveDoneInPosition(target, timeoutMs, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// [옵션 2026-07-29 / Ready 전용] 제품 보유 시 +방향 이탈 상대 이동.
        /// +방향으로 이동하면 카세트든 다른 위치든 제품을 놓게 된다(사용자 확인 2026-07-29).
        ///
        /// ★부분 이동 금지★ 요청 거리를 소프트리밋 안에서 전부 확보할 수 없으면 아예 움직이지 않는다.
        /// 절반만 움직이면 제품이 어중간하게 걸린 상태가 되어 오히려 위험하다.
        /// 확보 불가 시 음수 코드를 돌려 호출부가 BackOff 단계 실패로 처리하게 한다.
        /// </summary>
        public Func<double, int, CancellationToken, Task<int>> MoveRelativeAsync
        {
            get
            {
                return async (deltaMm, timeoutMs, ct) =>
                {
                    BaseAxis axis = _unit != null ? _unit.FeederY : null;
                    if (axis == null || deltaMm <= 0.0)
                        return -1;

                    double current = axis.ActualPosition;
                    double destination = current + deltaMm;

                    if (axis.Setup != null && axis.Setup.SoftLimitEnabled &&
                        destination > axis.Setup.SoftLimitPlus - FeederRetreatLimits.SoftLimitMarginMm)
                    {
                        QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "FeederRetreatBackOff",
                            "Ready 이탈 이동 중단: 요청 거리를 소프트리밋 안에서 확보할 수 없습니다(부분 이동 금지). axis=" +
                            axis.Name +
                            ", actual=" + current.ToString("0.###") +
                            ", 요청=" + deltaMm.ToString("0.###") +
                            ", 목표=" + destination.ToString("0.###") +
                            ", softLimitPlus=" + axis.Setup.SoftLimitPlus.ToString("0.###") +
                            ", margin=" + FeederRetreatLimits.SoftLimitMarginMm.ToString("0.###") + " - Failed");
                        return -2;
                    }

                    QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "FeederRetreatBackOff",
                        "Ready 이탈 이동(제품 보유). axis=" + axis.Name +
                        ", actual=" + current.ToString("0.###") +
                        ", 이동=" + deltaMm.ToString("0.###") +
                        ", 목표=" + destination.ToString("0.###") + "mm - Start");

                    int moveResult = await _unit.MoveWaferFeederY(destination, JogSpeedType.Coarse, 0.0).ConfigureAwait(false);
                    if (moveResult != 0)
                        return moveResult;

                    return await _unit.WaitWaferFeederYMoveDoneInPosition(destination, timeoutMs, ct).ConfigureAwait(false);
                };
            }
        }
    }

    /// <summary>OutputFeederUnit 어댑터.</summary>
    public sealed class OutputFeederRetreatTarget : IFeederRetreatTarget
    {
        private readonly OutputFeederUnit _unit;
        private readonly Func<int, CancellationToken, Task<int>> _moveToAvoidOverride;
        private readonly bool _bypassMaterialSensors;

        /// <param name="moveToAvoidOverride">
        /// Avoid 이동을 호출부가 직접 처리해야 할 때 사용한다(예: Ready 복구의 Jog Coarse 속도).
        /// </param>
        /// <param name="bypassMaterialSensors">
        /// 드라이런/시뮬레이션에서 자재 센서 판정을 건너뛴다. Unclamp 확인은 건너뛰지 않는다.
        /// </param>
        public OutputFeederRetreatTarget(
            OutputFeederUnit unit,
            Func<int, CancellationToken, Task<int>> moveToAvoidOverride = null,
            bool bypassMaterialSensors = false)
        {
            _unit = unit;
            _moveToAvoidOverride = moveToAvoidOverride;
            _bypassMaterialSensors = bypassMaterialSensors;
        }

        public string Name { get { return _unit != null ? _unit.Name : "OutputFeeder"; } }

        public bool IsUnclamped() { return _unit != null && _unit.IsFeederUnclamped(); }
        public bool IsEmpty() { return _bypassMaterialSensors || (_unit != null && _unit.IsFeederEmpty()); }
        public bool IsUp() { return _unit != null && _unit.IsFeederUp(); }
        public bool IsDown() { return _unit != null && _unit.IsFeederDown(); }
        public bool IsInAvoidPosition() { return _unit != null && _unit.IsBinFeederInAvoidPosition(); }
        public bool IsOverloaded() { return _unit != null && _unit.IsFeederOverload(); }
        public string DescribeState() { return _unit != null ? _unit.DescribeFeederCylinderState() : "OutputFeeder=null"; }

        public Task<int> UnclampAsync(int timeoutMs, CancellationToken ct)
        {
            return _unit.SetFeederClampAsync(false, timeoutMs, ct);
        }

        public Task<int> SetLiftAsync(bool up, int timeoutMs, CancellationToken ct)
        {
            return _unit.SetFeederUpDownAsync(up, timeoutMs, ct);
        }

        public async Task<int> MoveToAvoidAsync(int timeoutMs, CancellationToken ct)
        {
            if (_moveToAvoidOverride != null)
                return await _moveToAvoidOverride(timeoutMs, ct).ConfigureAwait(false);

            int result = await _unit.MoveToFeederAvoidPosition(false).ConfigureAwait(false);
            if (result != 0)
                return result;

            double target = _unit.Recipe != null ? _unit.Recipe.AvoidPosition : 0.0;
            return await _unit.WaitBinFeederYMoveDoneInPosition(target, timeoutMs, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// [옵션 2026-07-29 / Ready 전용] 입력측과 동일. ★부분 이동 금지★ —
        /// 요청 거리를 소프트리밋 안에서 전부 확보할 수 없으면 움직이지 않고 실패로 돌린다.
        /// </summary>
        public Func<double, int, CancellationToken, Task<int>> MoveRelativeAsync
        {
            get
            {
                return async (deltaMm, timeoutMs, ct) =>
                {
                    BaseAxis axis = _unit != null ? _unit.FeederY : null;
                    if (axis == null || deltaMm <= 0.0)
                        return -1;

                    double current = axis.ActualPosition;
                    double destination = current + deltaMm;

                    if (axis.Setup != null && axis.Setup.SoftLimitEnabled &&
                        destination > axis.Setup.SoftLimitPlus - FeederRetreatLimits.SoftLimitMarginMm)
                    {
                        QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "FeederRetreatBackOff",
                            "Ready 이탈 이동 중단: 요청 거리를 소프트리밋 안에서 확보할 수 없습니다(부분 이동 금지). axis=" +
                            axis.Name +
                            ", actual=" + current.ToString("0.###") +
                            ", 요청=" + deltaMm.ToString("0.###") +
                            ", 목표=" + destination.ToString("0.###") +
                            ", softLimitPlus=" + axis.Setup.SoftLimitPlus.ToString("0.###") +
                            ", margin=" + FeederRetreatLimits.SoftLimitMarginMm.ToString("0.###") + " - Failed");
                        return -2;
                    }

                    QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "FeederRetreatBackOff",
                        "Ready 이탈 이동(제품 보유). axis=" + axis.Name +
                        ", actual=" + current.ToString("0.###") +
                        ", 이동=" + deltaMm.ToString("0.###") +
                        ", 목표=" + destination.ToString("0.###") + "mm - Start");

                    int moveResult = await _unit.MoveBinFeederY(destination, JogSpeedType.Coarse, 0.0).ConfigureAwait(false);
                    if (moveResult != 0)
                        return moveResult;

                    return await _unit.WaitBinFeederYMoveDoneInPosition(destination, timeoutMs, ct).ConfigureAwait(false);
                };
            }
        }
    }
}
