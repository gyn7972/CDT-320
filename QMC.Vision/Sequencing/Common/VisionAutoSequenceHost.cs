using System;
using QMC.Vision.Config;
using QMC.Vision.Modules;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// Sim 자체 실행(핸들러 TCP 없이 비전이 자체 순차 실행)의 수명주기 호스트.
    /// 부팅 시 자동 시작하지 않음(STOP 기준) — 시작은 작업 화면 RUN 버튼(<see cref="Start"/>),
    /// 정지는 <see cref="Stop"/>. GENERAL 토글 OFF 시 <see cref="ApplyFromConfig"/> 가 정지시킨다.
    /// </summary>
    public sealed class VisionAutoSequenceHost
    {
        private readonly VisionMachine _machine;
        private readonly Action<string> _log;
        private AutoSequenceCoordinator _coordinator;

        /// <summary>시퀀스 로그 메시지(시작/정지/단계) — 시퀀서 테스트 페이지 등이 구독.</summary>
        public event Action<string> Message;

        public VisionAutoSequenceHost(VisionMachine machine, Action<string> log = null)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _log = log;
        }

        public bool IsRunning { get { return _coordinator != null && _coordinator.IsRunning; } }

        private void LogSink(string s) { try { _log?.Invoke(s); } catch { } try { Message?.Invoke(s); } catch { } }

        /// <summary>설정 변경 반영 — 자체실행 모드(SimAutoSequence)가 꺼지면 정지만 한다.
        /// 자동 시작은 하지 않음(STOP 기준). 시작은 사용자가 작업 화면 RUN 버튼으로 한다.</summary>
        public void ApplyFromConfig()
        {
            try
            {
                var cfg = VisionConfigStore.Current;
                bool selfRun = cfg != null && cfg.SimAutoSequence;
                if (!selfRun) Stop();   // 자체실행 비활성화 시 진행 중인 시퀀스 정지. 활성화여도 자동 시작 안 함.
            }
            catch (Exception ex)
            {
                _log?.Invoke("[SEQ] ApplyFromConfig 실패: " + ex.Message);
            }
        }

        /// <summary>지정 간격으로 전 모듈 자동 시퀀스를 시작한다(RUN 버튼). 기존 실행은 먼저 정지.</summary>
        public void Start(int cycleIntervalMs)
        {
            StartModules(SequenceModuleKind.All, SequenceRunMode.Auto, cycleIntervalMs);
        }

        /// <summary>선택 모듈 전체(그 안의 모든 도구)를 지정 모드로 시작한다(시퀀서 테스트 페이지용).</summary>
        public void StartModules(SequenceModuleKind kind, SequenceRunMode mode, int cycleIntervalMs)
        {
            try
            {
                Stop();
                if (kind == SequenceModuleKind.None) return;
                EnsureCoordinator(cycleIntervalMs);
                _coordinator.Start(kind, mode);
            }
            catch (Exception ex)
            {
                LogSink("[SEQ] 시퀀스 시작 실패: " + ex.Message);
            }
        }

        /// <summary>모듈 안의 특정 도구 하나만 지정 모드로 시작한다.</summary>
        public void StartTool(SequenceModuleKind kind, string toolId, SequenceRunMode mode, int cycleIntervalMs)
        {
            try
            {
                Stop();
                if (kind == SequenceModuleKind.None || string.IsNullOrEmpty(toolId)) return;
                EnsureCoordinator(cycleIntervalMs);
                _coordinator.StartTool(kind, toolId, mode);
            }
            catch (Exception ex)
            {
                LogSink("[SEQ] 도구 시작 실패: " + ex.Message);
            }
        }

        /// <summary>Step/Manual — 지정 도구를 1회 실행한다(코디네이터 없으면 생성).</summary>
        public void StepTool(SequenceModuleKind kind, string toolId, int cycleIntervalMs)
        {
            try { EnsureCoordinator(cycleIntervalMs); _coordinator.StepTool(kind, toolId); }
            catch (Exception ex) { LogSink("[SEQ] step 실패: " + ex.Message); }
        }

        /// <summary>Step/Manual — 모듈 순서상 '다음 도구'를 1회 실행한다(코디네이터 없으면 생성).</summary>
        public void StepNextTool(SequenceModuleKind kind, int cycleIntervalMs)
        {
            try { EnsureCoordinator(cycleIntervalMs); _coordinator.StepNextTool(kind); }
            catch (Exception ex) { LogSink("[SEQ] step 실패: " + ex.Message); }
        }

        /// <summary>코디네이터가 없으면 컨텍스트와 함께 생성한다(이미 있으면 유지).</summary>
        private void EnsureCoordinator(int cycleIntervalMs)
        {
            if (_coordinator != null) return;
            var ctx = new VisionSequenceContext(_machine, null, LogSink)
            {
                CycleIntervalMs = cycleIntervalMs > 0 ? cycleIntervalMs : 500
            };
            _coordinator = new AutoSequenceCoordinator(ctx);
        }

        /// <summary>도구별 메트릭 스냅샷(사이클 ms / 사이클 수 / 판정) — 미실행 시 빈 목록.</summary>
        public System.Collections.Generic.List<AutoSequenceCoordinator.ToolMetric> Metrics()
            => _coordinator?.Snapshot() ?? new System.Collections.Generic.List<AutoSequenceCoordinator.ToolMetric>();

        /// <summary>실행 중인 자동 시퀀스를 정지한다.</summary>
        public void Stop()
        {
            try { _coordinator?.Stop(); }
            catch { }
            _coordinator = null;
        }
    }
}
