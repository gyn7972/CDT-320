using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// 촬영(EPD) 종료 직후 비전 X가 스스로 시작한 "독립 회피" 세션의 공유 매체.
    /// - 인풋: InputCameraMarkInspectionSequence(선행검사)가 배치 마지막 EPD 직후 등록,
    ///   PickerPickUpSequence가 픽커 진입 준비 시 인수(팔로잉/연장/무시)한다.
    /// - 아웃풋: OutputPostPlaceInspectionQueue(후검사 워커)가 배치 EPD 직후 등록,
    ///   PickerPlaceSequence가 Place 진입 준비 시 인수한다. 워커는 등록 여부와 무관하게
    ///   자기 참조로 항상 await(병렬 배리어)하므로 소유권은 워커에 남는다(이중 await 안전).
    /// 설계 근거(2026-07-26 지시): 세션 상태는 시퀀싱 계층에 둔다 — SharedRailXMotionService는
    /// 무상태 정책 계층이므로 사용하지 않는다(InputVisionXPrePositionCoordinator 선례).
    /// 인수 규칙(사용자 승인 2026-07-26, 3번 개정): Task 상태와 무관하게 세션을 인수하고
    /// taskAlreadyCompleted 플래그를 함께 전달한다 — 이동 Task의 '완료'가 실제 축 도착을
    /// 보장하지 않는 사례(실장비 오탐 알람 3건)가 확인되어, 도착 동기화는 인수자가
    /// "실위치 도착 대기" 합성으로 수행한다(재명령 금지, 정지 확인 후 재기동).
    /// 소비 검증(B1) Peek 규칙: 세션 존재만 확인한다 — Task 상태도, 등록 사이드도 판정에
    /// 쓰지 않는다. 회피는 공용 비전 축의 물리 퇴장이므로 어느 사이드가 시작했든
    /// 방향 판정 + 도착 예정 목표 페어 간격은 동일하게 유효하다(상대 사이드 세션
    /// 덮어쓰기에 의한 오탐 차단). 인수(TryAdopt)는 사이드 일치를 계속 요구한다.
    /// </summary>
    internal static class VisionIndependentRetreatCoordinator
    {
        /// <summary>
        /// 회피 목표 산정 시 기존 Extra 클리어런스에 더하는 여유(mm) — 사용자 승인(2026-07-26, 4번).
        /// 최심 픽/플레이스 목표와 회피 목표의 최종 간격이 팔로잉 safetyGap과 정확히 같아지는
        /// 경계치(부동소수 오차·보정치 변화로 -21 타임아웃→R6 폴백 유발 가능)를 해소한다.
        /// 팔로잉 safetyGap 산정(TryGetFollowGapParameters)에는 더하지 않는다 — 회피만 깊어진다.
        /// </summary>
        internal const double RetreatTargetExtraMarginMm = 1.0;

        private sealed class RetreatSession
        {
            public Task<int> MoveTask;
            public double Target;
            public string Owner;
            public DateTime StartedAtUtc;
        }

        /// <summary>진단용 Task 상태 문자열.</summary>
        private static string DescribeTask(Task<int> task)
        {
            if (task == null)
                return "null";
            if (!task.IsCompleted)
                return "running";
            if (task.IsCanceled)
                return "canceled";
            if (task.IsFaulted)
                return "faulted";
            return "completed(result=" + task.Result + ")";
        }

        private static readonly object Sync = new object();
        private static RetreatSession _input;
        private static PickerSequenceSide _inputSide;
        private static RetreatSession _output;
        private static readonly List<Task<int>> ActiveInputRetreatTasks = new List<Task<int>>();
        private static string _inputRetreatFailure = string.Empty;

        // ---------- 인풋 (InputVisionX, side = 회피를 시작한 선행검사 측) ----------

        /// <summary>선행검사 EPD 직후 시작한 독립 회피 Task를 등록한다. 이전 세션은 관찰 후 덮어쓴다.</summary>
        public static void RegisterInput(PickerSequenceSide side, Task<int> moveTask, double target, string owner)
        {
            if (moveTask == null)
                return;

            RetreatSession previous;
            lock (Sync)
            {
                previous = _input;
                _input = new RetreatSession
                {
                    MoveTask = moveTask,
                    Target = target,
                    Owner = owner,
                    StartedAtUtc = DateTime.UtcNow
                };
                _inputSide = side;
                if (!ActiveInputRetreatTasks.Contains(moveTask))
                    ActiveInputRetreatTasks.Add(moveTask);
            }

            ObserveInputRetreatCompletion(moveTask, side, owner);

            ObserveReplacedSession(previous, "InputVisionX 독립 회피 세션 교체");
            WriteLog("InputVisionX 독립 회피 세션을 등록했습니다. side=" + side +
                     ", target=" + target.ToString("F6") +
                     ", owner=" + Safe(owner) + " - Ok");
        }

        /// <summary>
        /// 같은 side의 독립 회피 세션을 인수한다(스토어에서 제거).
        /// 기존 조건(2026-07-26 1·2차): 미완료 Task만 인수하고 완료 세션은 버렸다 — 이동 Task가
        /// 실제 축 도착 전에 완료 상태가 되는 사례(실장비 오탐 알람 3건)에서 픽업이 이동 중인
        /// 축에 재명령하는 위험이 있었다.
        /// 현재 기준(사용자 승인 2026-07-26, 3번): Task 상태와 무관하게 세션을 인수하고
        /// taskAlreadyCompleted 플래그를 함께 돌려준다 — 완료로 보이는 세션은 호출자가
        /// "실위치 도착 대기" 합성으로 동기화한다(재명령 금지).
        /// </summary>
        public static bool TryAdoptInput(
            PickerSequenceSide side,
            out Task<int> moveTask,
            out double target,
            out bool taskAlreadyCompleted)
        {
            moveTask = null;
            target = 0.0;
            taskAlreadyCompleted = false;

            string taskState;
            lock (Sync)
            {
                if (_input == null || _inputSide != side || _input.MoveTask == null)
                    return false;

                moveTask = _input.MoveTask;
                target = _input.Target;
                taskAlreadyCompleted = _input.MoveTask.IsCompleted;
                taskState = DescribeTask(_input.MoveTask);
                _input = null;
            }

            WriteLog("InputVisionX 독립 회피 세션을 픽업 시퀀스가 인수했습니다. side=" + side +
                     ", target=" + target.ToString("F6") +
                     ", taskState=" + taskState + " - Ok");
            return true;
        }

        /// <summary>
        /// [Cycle Stop 래치 2026-08-27, 팀장님 승인 A안] 자동 운전 run 시작 시 이전 run의 실패
        /// 래치를 리셋한다. 래치의 목적은 "이번 run의 회피 실패를 Cycle Stop에서 은폐하지 않는 것"
        /// 인데 수명이 앱 전체라서, 비상정지로 중단된 회피(2026-08-27 4152 사고 잔상)의 실패가
        /// 이후 모든 정상 Cycle Stop drain을 영구 실패시켰다(14:27:16/14:35:09 재현 확인).
        /// 리셋 시 이전 래치 내용을 로그 1줄로 남겨 추적성을 유지한다.
        /// </summary>
        public static void ResetInputRetreatFailure(string reason)
        {
            string previous;
            lock (Sync)
            {
                previous = _inputRetreatFailure;
                _inputRetreatFailure = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(previous))
            {
                WriteLog("이전 run의 InputVisionX 독립 회피 실패 래치를 리셋합니다. reason=" + Safe(reason) +
                         ", previous=" + previous + " - Check");
            }
        }

        /// <summary>
        /// 정상 Auto Cycle Stop 최종 배리어에서 등록된 InputVisionX 독립 회피 Task가 끝날 때까지 기다린다.
        /// 진행 중 모션을 취소하지 않으며, 등록 이후 발생한 실패는 성공 정지로 숨기지 않는다.
        /// 실패 래치의 수명은 run 단위 — 다음 run 시작 시 ResetInputRetreatFailure로 리셋된다.
        /// </summary>
        public static async Task<int> WaitInputRetreatsUntilIdleAsync(
            string reason,
            int timeoutMs,
            CancellationToken ct)
        {
            string safeReason = string.IsNullOrWhiteSpace(reason) ? "CycleStopDrain" : reason;
            int safeTimeoutMs = timeoutMs > 0 ? timeoutMs : 30000;
            DateTime startedAt = DateTime.UtcNow;
            bool waitLogged = false;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                int activeCount;
                string failure;
                lock (Sync)
                {
                    activeCount = ActiveInputRetreatTasks.Count;
                    failure = _inputRetreatFailure;
                }

                if (!string.IsNullOrWhiteSpace(failure))
                {
                    WriteLog(safeReason + " InputVisionX 독립 회피 실패 상태가 확인되었습니다. " +
                             "reason=" + failure + " - Failed");
                    return -1;
                }

                if (activeCount == 0)
                {
                    if (waitLogged)
                    {
                        WriteLog(safeReason + " InputVisionX 독립 회피 drain 완료. - Ok");
                    }
                    return 0;
                }

                if ((DateTime.UtcNow - startedAt).TotalMilliseconds >= safeTimeoutMs)
                {
                    WriteLog(safeReason + " InputVisionX 독립 회피 drain 시간 초과. active=" + activeCount +
                             ", timeoutMs=" + safeTimeoutMs + " - Failed");
                    return -1;
                }

                if (!waitLogged)
                {
                    waitLogged = true;
                    WriteLog(safeReason + " InputVisionX 독립 회피 완료를 기다립니다. active=" +
                             activeCount + " - Wait");
                }

                await Task.Delay(20, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 허가 소비 검증(B1)용: 독립 회피 세션이 있으면 목표를 돌려준다.
        /// 기존 조건(2026-07-26 1·2차): Task 미완료(IsCompleted=false)를 요구했다 — 이동 Task가
        /// 실제 축 도착 전에 완료 상태가 되는 사례에서 소비 검증이 결정적으로 오탐(3/3)했다.
        /// 현재 기준(사용자 승인 2026-07-26, 1번 + 검증 결과 반영): Task 상태도, 등록 사이드도
        /// 보지 않는다 — 회피는 공용 InputVisionX의 물리 퇴장이므로 어느 사이드가 시작했든
        /// 호출자의 방향 판정 + 도착 예정 목표 페어 간격 + 진입 존 인터락이 안전을 담당한다
        /// (상대 사이드 세션 덮어쓰기로 인한 사이드 불일치 오탐 경로 차단). 세션은 제거하지 않는다.
        /// </summary>
        public static bool TryPeekInputRetreatTarget(PickerSequenceSide side, out double target, out string sessionDetail)
        {
            lock (Sync)
            {
                target = 0.0;
                sessionDetail = _input == null
                    ? "session=none"
                    : "sessionSide=" + _inputSide +
                      ", peekSide=" + side +
                      ", target=" + _input.Target.ToString("F6") +
                      ", task=" + DescribeTask(_input.MoveTask) +
                      ", owner=" + Safe(_input.Owner);
                if (_input == null || _input.MoveTask == null)
                    return false;

                target = _input.Target;
                return true;
            }
        }

        /// <summary>
        /// C3 정리 경로: side의 세션을 제거하고 Task를 관찰한다. 이동 자체는 완료 보장형
        /// 명령(축 정지 시 실패 종료)이므로 여기서는 참조 정리와 unobserved 예외 방지만 담당한다.
        /// </summary>
        public static void CancelInput(PickerSequenceSide side, string reason)
        {
            RetreatSession current = null;
            lock (Sync)
            {
                if (_input != null && _inputSide == side)
                {
                    current = _input;
                    _input = null;
                }
            }

            if (current == null)
                return;

            ObserveReplacedSession(current, "InputVisionX 독립 회피 세션 취소 정리");
            WriteLog("InputVisionX 독립 회피 세션을 정리했습니다. side=" + side +
                     ", reason=" + Safe(reason) + " - Check");
        }

        // ---------- 아웃풋 (OutputVisionX, 단일 세션) ----------

        /// <summary>후검사 워커가 배치 EPD 직후 시작한 회피 Task를 등록한다(참조 공유 — 소유권은 워커 유지).</summary>
        public static void RegisterOutput(Task<int> moveTask, double target, string owner)
        {
            if (moveTask == null)
                return;

            RetreatSession previous;
            lock (Sync)
            {
                previous = _output;
                _output = new RetreatSession
                {
                    MoveTask = moveTask,
                    Target = target,
                    Owner = owner,
                    StartedAtUtc = DateTime.UtcNow
                };
            }

            ObserveReplacedSession(previous, "OutputVisionX 독립 회피 세션 교체");
            WriteLog("OutputVisionX 독립 회피 세션을 등록했습니다. target=" + target.ToString("F6") +
                     ", owner=" + Safe(owner) + " - Ok");
        }

        /// <summary>
        /// 아웃풋 독립 회피 세션을 인수한다(스토어에서 제거 — 워커의 배리어 await 소유권은 유지).
        /// 인풋과 동일(사용자 승인 2026-07-26, 3번): Task 상태와 무관하게 인수하고
        /// taskAlreadyCompleted 플래그를 돌려준다 — 완료로 보이는 세션은 호출자가
        /// "실위치 도착 대기" 합성으로 동기화한다(재명령 금지).
        /// </summary>
        public static bool TryAdoptOutput(
            out Task<int> moveTask,
            out double target,
            out bool taskAlreadyCompleted)
        {
            moveTask = null;
            target = 0.0;
            taskAlreadyCompleted = false;

            string taskState;
            lock (Sync)
            {
                if (_output == null || _output.MoveTask == null)
                    return false;

                moveTask = _output.MoveTask;
                target = _output.Target;
                taskAlreadyCompleted = _output.MoveTask.IsCompleted;
                taskState = DescribeTask(_output.MoveTask);
                _output = null;
            }

            WriteLog("OutputVisionX 독립 회피 세션을 Place 시퀀스가 인수했습니다. target=" +
                     target.ToString("F6") +
                     ", taskState=" + taskState + " - Ok");
            return true;
        }

        /// <summary>아웃풋 세션 제거(배치 실패/드레인 정리). 워커가 Task를 직접 await하므로 관찰만 수행.</summary>
        public static void ClearOutput(string reason)
        {
            RetreatSession current;
            lock (Sync)
            {
                current = _output;
                _output = null;
            }

            if (current == null)
                return;

            ObserveReplacedSession(current, "OutputVisionX 독립 회피 세션 정리");
            WriteLog("OutputVisionX 독립 회피 세션을 정리했습니다. reason=" + Safe(reason) + " - Check");
        }

        // ---------- 아웃풋 Place 진입 요구 좌표(회피 no-op 방지, 사용자 승인 2026-07-26) ----------
        //
        // 배경: 배치 EPD 시점의 최소 회피 계산은 "피커의 현재 위치"만 장애물로 본다. 그런데 그때
        //   다음 피커는 아직 사이드 촬영 존에 있어 Place 코리도어와 멀고, 그래서 요구 좌표가 이미
        //   충족돼 "현재 위치 유지(=0mm 이동)"로 해소됐다. 실제 회피는 다음 Place 시퀀스가 정확
        //   좌표로 연장할 때(=그 피커의 사이드 촬영 EPD 완료 후)에야 발행되어, 비전이 스테이지 위에
        //   5.2~7.0초 잔류했다(실장비 2026-07-26 05:58~06:00 4개 배치 전부).
        // 현재 기준: Place 시퀀스가 확정한 "Place 진입 요구 비전 좌표"를 여기에 게시하고, 큐가 배치
        //   EPD 시점에 그 값까지 미리 물러난다. 총 이동량은 같고 시점만 앞당겨진다(사이드 촬영과 병렬).
        //   다음 Place가 계산한 정확 좌표와의 차이는 기존 "연장 회피" 경로가 그대로 흡수한다.
        private static double _outputPlaceEntryTarget = double.NaN;
        private static string _outputPlaceEntryOwner;

        /// <summary>Place 시퀀스가 확정한 Place 진입 요구 비전 좌표를 게시한다.</summary>
        public static void RegisterOutputPlaceEntryTarget(double target, string owner)
        {
            if (double.IsNaN(target) || double.IsInfinity(target))
                return;

            lock (Sync)
            {
                _outputPlaceEntryTarget = target;
                _outputPlaceEntryOwner = owner;
            }
        }

        /// <summary>가장 최근에 게시된 Place 진입 요구 비전 좌표. 없으면 false.</summary>
        public static bool TryGetOutputPlaceEntryTarget(out double target, out string owner)
        {
            lock (Sync)
            {
                target = _outputPlaceEntryTarget;
                owner = _outputPlaceEntryOwner;
            }

            return !double.IsNaN(target);
        }

        // ---------- 아웃풋 OutputVisionX 명령권 중재 토큰 ----------
        // 배경(2026-08-17 23:20 PickerX PEL 사고 후속, 2026-08-18): Bottom/Side 검사 시퀀스의
        //   진입 게이트가 "후검사 예약 0건 + 카메라 유휴"일 때 스스로 카메라 회피를 명령하는
        //   유휴 폴백을 갖는다. 후검사 큐 워커와 픽커 시퀀스는 서로 다른 스레드라 "예약==0"
        //   판정과 회피 명령 발행 사이의 TOCTOU(그 사이 신규 예약 진입 → 큐 접근 명령과
        //   이중 명령)가 원자화로는 막히지 않는다. 대신 OutputVisionX 명령권 자체를 토큰으로
        //   직렬화한다: 큐 배치(접근~배치말 회피)와 픽커 유휴 회피가 토큰을 쥔 쪽만 명령한다.
        //   예약 카운터는 힌트로 강등 — 힌트가 낡아도 명령권이 직렬이라 누가 몇 초 기다리는지만
        //   달라지고 이중 명령은 없다. 플래그 조작만 lock 안에서 하고 모션은 항상 lock 밖.
        private static string _outputVisionCommandOwner;

        /// <summary>OutputVisionX 명령권 토큰 획득 시도. 미보유 또는 동일 소유자면 성공.</summary>
        public static bool TryAcquireOutputVisionCommand(string owner)
        {
            if (string.IsNullOrEmpty(owner))
                return false;

            lock (Sync)
            {
                if (_outputVisionCommandOwner != null &&
                    !string.Equals(_outputVisionCommandOwner, owner, StringComparison.Ordinal))
                    return false;

                _outputVisionCommandOwner = owner;
                return true;
            }
        }

        /// <summary>OutputVisionX 명령권 토큰 해제. 소유자가 일치할 때만 해제된다.</summary>
        public static void ReleaseOutputVisionCommand(string owner)
        {
            lock (Sync)
            {
                if (string.Equals(_outputVisionCommandOwner, owner, StringComparison.Ordinal))
                    _outputVisionCommandOwner = null;
            }
        }

        /// <summary>현재 토큰 소유자(진단/대기 사유 로그용). 미보유면 false.</summary>
        public static bool TryGetOutputVisionCommandOwner(out string owner)
        {
            lock (Sync)
            {
                owner = _outputVisionCommandOwner;
            }

            return owner != null;
        }

        /// <summary>등록된 아웃풋 독립 회피 Task가 아직 실행 중인지(유휴 폴백 자격 판정용).</summary>
        public static bool IsOutputRetreatTaskRunning()
        {
            lock (Sync)
            {
                return _output != null && _output.MoveTask != null && !_output.MoveTask.IsCompleted;
            }
        }

        // ---------- 공통 ----------

        private static void ObserveReplacedSession(RetreatSession session, string context)
        {
            if (session == null || session.MoveTask == null)
                return;

            session.MoveTask.ContinueWith(
                completed =>
                {
                    try
                    {
                        if (completed.IsFaulted && completed.Exception != null)
                        {
                            WriteLog(context + " 중 Task 예외를 관찰했습니다. error=" +
                                     completed.Exception.GetBaseException().Message + " - Failed");
                        }
                        else if (completed.IsCanceled)
                        {
                            WriteLog(context + " 중 Task 취소를 관찰했습니다. - Check");
                        }
                        else if (completed.Result != 0)
                        {
                            WriteLog(context + " 중 실패 결과를 관찰했습니다. result=" +
                                     completed.Result + " - Check");
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLog(context + " Task 관찰 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private static void ObserveInputRetreatCompletion(
            Task<int> task,
            PickerSequenceSide side,
            string owner)
        {
            if (task == null)
                return;

            task.ContinueWith(
                completed =>
                {
                    string failure = string.Empty;
                    try
                    {
                        if (completed.IsCanceled)
                        {
                            failure = "취소됨";
                        }
                        else if (completed.IsFaulted)
                        {
                            failure = completed.Exception != null
                                ? completed.Exception.GetBaseException().Message
                                : "faulted";
                        }
                        else if (completed.Result != 0)
                        {
                            failure = "result=" + completed.Result;
                        }
                    }
                    catch (Exception ex)
                    {
                        failure = ex.Message;
                    }

                    lock (Sync)
                    {
                        ActiveInputRetreatTasks.Remove(task);
                        if (!string.IsNullOrWhiteSpace(failure) &&
                            string.IsNullOrWhiteSpace(_inputRetreatFailure))
                        {
                            _inputRetreatFailure = "side=" + side +
                                ", owner=" + Safe(owner) +
                                ", " + failure;
                        }
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private static void WriteLog(string message)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "VisionIndependentRetreat", message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "VisionIndependentRetreat 로그 기록 실패. error=" + ex.Message);
            }
        }
    }
}
