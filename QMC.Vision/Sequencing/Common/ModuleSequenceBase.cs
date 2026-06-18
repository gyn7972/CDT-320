using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Vision.Modules;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// 비전 모듈 시퀀스의 공통 실행 기반 클래스. 핸들러 UnitSequenceBase 미러(유닛→모듈).
    /// Auto=연속 실행, Manual/Step=게이트마다 1사이클. 모션은 포함하지 않으며 명령은 디스패처(Context.Dispatch)로만 수행한다.
    /// </summary>
    public abstract class ModuleSequenceBase
    {
        private readonly SemaphoreSlim _stepGate = new SemaphoreSlim(0, int.MaxValue);
        private int _stepBusyOrQueued;

        protected ModuleSequenceBase(VisionSequenceContext ctx, SequenceModuleKind kind, IVisionModule module, string name)
        {
            Context = ctx ?? throw new ArgumentNullException(nameof(ctx));
            Kind = kind;
            Module = module;
            Name = string.IsNullOrWhiteSpace(name) ? kind.ToString() : name;
            Mode = SequenceRunMode.Auto;
            CycleIntervalMs = ctx.CycleIntervalMs;
        }

        public VisionSequenceContext Context { get; private set; }
        public SequenceModuleKind Kind { get; private set; }
        public IVisionModule Module { get; private set; }
        public string Name { get; private set; }
        public SequenceRunMode Mode { get; private set; }

        /// <summary>Auto 연속 실행 시 사이클 사이 대기(ms).</summary>
        public int CycleIntervalMs { get; set; } = 500;

        /// <summary>직전 모듈 사이클 소요(ms) — 시퀀서 메트릭.</summary>
        public double LastCycleMs { get; private set; }
        /// <summary>완료한 사이클 수.</summary>
        public long CycleCount { get; private set; }

        public void Configure(SequenceRunMode mode) { Mode = mode; }

        /// <summary>현재 모드에 따라 시퀀스를 실행한다. Auto=연속, Manual/Step=게이트마다 1사이클.</summary>
        public async Task RunAsync(CancellationToken ct)
        {
            try
            {
                if (Mode == SequenceRunMode.Auto)
                {
                    await ExecuteAutoAsync(ct).ConfigureAwait(false);
                    return;
                }

                Context.LogPublic("[SEQ] " + Name + " manual/step 게이트 대기");
                while (!ct.IsCancellationRequested)
                {
                    await _stepGate.WaitAsync(ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    try { await ExecuteStepAsync(ct).ConfigureAwait(false); }
                    finally { Interlocked.Exchange(ref _stepBusyOrQueued, 0); }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (SequenceStopException) { throw; }
            catch (Exception ex)
            {
                Fail("SEQ-" + Name + "-RUN", "시퀀스 실행 실패: " + ex.Message);
                throw;
            }
            finally { }
        }

        /// <summary>Manual/Step 모드에서 1사이클 실행 신호를 입력한다.</summary>
        public void StepUnit()
        {
            if (Interlocked.CompareExchange(ref _stepBusyOrQueued, 1, 0) == 0)
            {
                _stepGate.Release();
                return;
            }
            Context.LogPublic("[SEQ] " + Name + " step 무시: 직전 step 실행/대기 중");
        }

        /// <summary>Auto 모드 동작 — 기본은 사이클 연속 실행. 모듈별로 override 가능.</summary>
        protected virtual async Task ExecuteAutoAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await RunModuleCycleAsync(ct).ConfigureAwait(false);
                await Task.Delay(CycleIntervalMs, ct).ConfigureAwait(false);
            }
        }

        /// <summary>Manual/Step 모드 1단계 — 기본은 사이클 1회.</summary>
        protected virtual Task ExecuteStepAsync(CancellationToken ct)
        {
            return RunModuleCycleAsync(ct);
        }

        /// <summary>
        /// 모듈 1사이클: GRAB → 각 Finder MATCH → 각 Inspector INSPECT (공유 디스패처 경유).
        /// 성공 0 / 실패 -1. 모듈별 세부 순서가 필요하면 override 한다.
        /// </summary>
        protected virtual async Task<int> RunModuleCycleAsync(CancellationToken ct)
        {
            var _sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                ct.ThrowIfCancellationRequested();
                if (Module == null) return Fail("SEQ-" + Name + "-NOMOD", "모듈 미지정");

                foreach (var step in CycleSteps())
                {
                    ct.ThrowIfCancellationRequested();
                    string cmd = step.Key, id = step.Value;
                    // 모듈에 없는 finder/inspector 단계는 조용히 skip(모듈별 순서 정의 시 안전).
                    if (cmd == "MATCH"   && id != null && !Module.Finders.ContainsKey(id))    continue;
                    if (cmd == "INSPECT" && id != null && !Module.Inspectors.ContainsKey(id)) continue;
                    LogStep(cmd, id ?? "", Context.Dispatch(Module, cmd, id == null ? null : new[] { id }));
                }

                await Task.Yield();
                return 0;
            }
            catch (OperationCanceledException) { throw; }
            catch (SequenceStopException) { throw; }
            catch (Exception ex) { return Fail("SEQ-" + Name + "-CYCLE", "사이클 실패: " + ex.Message); }
            finally { _sw.Stop(); LastCycleMs = _sw.Elapsed.TotalMilliseconds; CycleCount++; }
        }

        /// <summary>1사이클의 명령 단계(순서). 기본은 GRAB → 전 Finder MATCH → 전 Inspector INSPECT(등록 순).
        /// 모듈별 실제 검사 순서(CDT-310)는 파생 클래스에서 override 한다.</summary>
        protected virtual IEnumerable<KeyValuePair<string, string>> CycleSteps()
        {
            yield return Step("GRAB", null);
            foreach (var id in Module.Finders.Keys.ToList())    yield return Step("MATCH", id);
            foreach (var id in Module.Inspectors.Keys.ToList())  yield return Step("INSPECT", id);
        }

        /// <summary>단계 생성 헬퍼.</summary>
        protected static KeyValuePair<string, string> Step(string cmd, string id)
            => new KeyValuePair<string, string>(cmd, id);

        /// <summary>단계 결과 로깅 — Manual/Step(테스트) 모드는 모든 단계, Auto 모드는 실패만 기록(연속실행 로그 폭주 방지).</summary>
        private void LogStep(string cmd, string target, string result)
        {
            bool fail = !string.IsNullOrEmpty(result) &&
                        (result.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         result.StartsWith("ERR", StringComparison.OrdinalIgnoreCase));
            if (Mode == SequenceRunMode.Auto && !fail) return;   // Auto 정상 단계는 생략

            string line = "[SEQ-" + Name + "] " + cmd
                        + (string.IsNullOrEmpty(target) ? "" : "(" + target + ")")
                        + " = " + (result ?? "-");
            WriteLog(Name, line);        // 이력(EventLogger "SEQ")
            Context.LogPublic(line);     // 로그 싱크
        }

        /// <summary>실패 처리 — 로그 + Alarm 후 -1 반환.</summary>
        protected int Fail(string alarmCode, string message)
        {
            try
            {
                WriteLog(Name, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, Name, message);
                Context.LogPublic("[SEQ-" + Name + "] FAIL " + alarmCode + " - " + message);
            }
            catch { }
            return -1;
        }

        protected static void WriteLog(string source, string message)
        {
            try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "SEQ", source, message); }
            catch { }
        }
    }
}
