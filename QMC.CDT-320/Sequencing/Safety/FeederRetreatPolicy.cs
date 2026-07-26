using System;
using System.Threading;
using System.Threading.Tasks;

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
        public static async Task<FeederRetreatResult> RetreatToAvoidAsync(
            IFeederRetreatTarget feeder,
            int ioTimeoutMs,
            int moveTimeoutMs,
            CancellationToken ct)
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

            // 2) Lift Up 전 파손 방지 확인. 자재를 물고 있으면 절대 올리지 않는다.
            if (!feeder.IsUnclamped() || !feeder.IsEmpty())
            {
                return FeederRetreatResult.Failed(FeederRetreatPhase.LiftUpBlocked, -1,
                    "Lift Up 차단: 자재를 보유했거나 Unclamp가 확인되지 않았습니다. " +
                    "자재를 문 상태로 Lift Up 하면 피더가 파손됩니다. unclamped=" + feeder.IsUnclamped() +
                    ", empty=" + feeder.IsEmpty() + ". " + feeder.DescribeState());
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
    }
}
