using System;
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
    /// 인수 규칙: 미완료 Task만 인수한다. 완료된 세션은 "이미 주차/정지된 축 위치"일 뿐이므로
    /// 기존 위치 기반 판정(주차 페어 간격/인포지션)이 그대로 담당한다(stale 목표 오용 방지).
    /// </summary>
    internal static class VisionIndependentRetreatCoordinator
    {
        private sealed class RetreatSession
        {
            public Task<int> MoveTask;
            public double Target;
            public string Owner;
            public DateTime StartedAtUtc;
        }

        private static readonly object Sync = new object();
        private static RetreatSession _input;
        private static PickerSequenceSide _inputSide;
        private static RetreatSession _output;

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
            }

            ObserveReplacedSession(previous, "InputVisionX 독립 회피 세션 교체");
            WriteLog("InputVisionX 독립 회피 세션을 등록했습니다. side=" + side +
                     ", target=" + target.ToString("F6") +
                     ", owner=" + Safe(owner) + " - Ok");
        }

        /// <summary>
        /// 같은 side의 미완료 독립 회피 세션을 인수한다(스토어에서 제거).
        /// 완료된 세션은 결과를 관찰·제거만 하고 false를 반환한다(위치 기반 판정이 담당).
        /// </summary>
        public static bool TryAdoptInput(PickerSequenceSide side, out Task<int> moveTask, out double target)
        {
            moveTask = null;
            target = 0.0;

            RetreatSession completed = null;
            lock (Sync)
            {
                if (_input == null || _inputSide != side || _input.MoveTask == null)
                    return false;

                if (_input.MoveTask.IsCompleted)
                {
                    completed = _input;
                    _input = null;
                }
                else
                {
                    moveTask = _input.MoveTask;
                    target = _input.Target;
                    _input = null;
                }
            }

            if (completed != null)
            {
                ObserveReplacedSession(completed, "InputVisionX 독립 회피 완료 세션 정리");
                return false;
            }

            if (moveTask == null)
                return false;

            WriteLog("InputVisionX 독립 회피 세션을 픽업 시퀀스가 인수했습니다. side=" + side +
                     ", target=" + target.ToString("F6") + " - Ok");
            return true;
        }

        /// <summary>
        /// 허가 소비 검증(B1)용: 같은 side의 미완료 독립 회피 세션이 있으면 목표를 돌려준다.
        /// 세션을 제거하지 않는다.
        /// </summary>
        public static bool TryPeekActiveInput(PickerSequenceSide side, out double target)
        {
            lock (Sync)
            {
                target = 0.0;
                if (_input == null || _inputSide != side || _input.MoveTask == null || _input.MoveTask.IsCompleted)
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

        /// <summary>미완료 아웃풋 독립 회피 세션을 인수한다(스토어에서 제거). 완료 세션은 정리만 한다.</summary>
        public static bool TryAdoptOutput(out Task<int> moveTask, out double target)
        {
            moveTask = null;
            target = 0.0;

            RetreatSession completed = null;
            lock (Sync)
            {
                if (_output == null || _output.MoveTask == null)
                    return false;

                if (_output.MoveTask.IsCompleted)
                {
                    completed = _output;
                    _output = null;
                }
                else
                {
                    moveTask = _output.MoveTask;
                    target = _output.Target;
                    _output = null;
                }
            }

            if (completed != null)
            {
                ObserveReplacedSession(completed, "OutputVisionX 독립 회피 완료 세션 정리");
                return false;
            }

            if (moveTask == null)
                return false;

            WriteLog("OutputVisionX 독립 회피 세션을 Place 시퀀스가 인수했습니다. target=" +
                     target.ToString("F6") + " - Ok");
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
