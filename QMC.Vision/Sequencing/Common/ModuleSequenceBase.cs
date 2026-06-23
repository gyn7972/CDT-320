using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Vision.Config;
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

                // 시뮬 자체구동이 실제(핸들러) 흐름과 동일하게 chipUid 를 발급(옵션) — MaterialTracker/로그까지 태움.
                string chipUid = ResolveCycleChipUid();

                var failReasons = new System.Collections.Generic.List<string>();
                foreach (var step in CycleSteps())
                {
                    ct.ThrowIfCancellationRequested();
                    string cmd = step.Key, id = step.Value;
                    // 모듈에 없는 finder/inspector 단계는 조용히 skip(모듈별 순서 정의 시 안전).
                    if (cmd == "MATCH"   && id != null && !Module.Finders.ContainsKey(id))    continue;
                    if (cmd == "INSPECT" && id != null && !Module.Inspectors.ContainsKey(id)) continue;
                    string result = Context.Dispatch(Module, cmd, BuildDispatchArgs(id, chipUid));
                    LogStep(cmd, id ?? "", result);
                    string tgt = string.IsNullOrEmpty(id) ? cmd : id;
                    if (IsStepFail(result))
                    {
                        // 실행 실패(학습 없음·매칭 실패 등) — 사람이 읽기 쉬운 사유로 표시.
                        string reason = HumanReason(ReasonOf(result));
                        string line = "[SEQ-" + Name + "] " + tgt + " → NG (" + reason + ")";
                        WriteLog(Name, line);
                        Context.LogPublic(line);
                        failReasons.Add(tgt + " → " + reason);
                        continue;
                    }
                    // MATCH score 게이트 — AcceptThreshold 미만이면 NG(스코어 포함) 표시.
                    string ng = MatchNgIfBelowThreshold(cmd, id, result);
                    if (ng != null)
                    {
                        string line = "[SEQ-" + Name + "] " + tgt + " → NG (" + ng + ")";
                        WriteLog(Name, line);
                        Context.LogPublic(line);
                        failReasons.Add(tgt + " → NG " + ng);
                    }
                }

                // 실패 단계가 있으면 '왜 fail 인지'를 한 줄로 모아 표시(원인 파악 용이).
                if (failReasons.Count > 0)
                {
                    string summary = "[SEQ-" + Name + "] 사이클 NG — " + string.Join(", ", failReasons);
                    WriteLog(Name, summary);
                    Context.LogPublic(summary);
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
            // 실행 실패만 fail 로 본다. 실행 오류는 소문자 "fail:"(콜론) 또는 "ERR" 로 시작한다.
            // 검사 NG 판정은 대문자 "FAIL;"(세미콜론)로 반환되는 '정상 결과'이므로 실패로 오인하면 안 된다.
            bool fail = !string.IsNullOrEmpty(result) &&
                        (result.StartsWith("fail:", StringComparison.Ordinal) ||
                         result.StartsWith("ERR",   StringComparison.Ordinal));
            if (Mode == SequenceRunMode.Auto && !fail) return;   // Auto 정상 단계는 생략

            string line = "[SEQ-" + Name + "] " + cmd
                        + (string.IsNullOrEmpty(target) ? "" : "(" + target + ")")
                        + " = " + (result ?? "-");
            WriteLog(Name, line);        // 이력(EventLogger "SEQ")
            Context.LogPublic(line);     // 로그 싱크
        }

        /// <summary>이번 사이클 chipUid 결정 — SimEmitChipUid=true 면 합성 발급(실제 흐름과 동일), 아니면 빈 값(로그/추적 생략).</summary>
        private string ResolveCycleChipUid()
        {
            try
            {
                var cfg = VisionConfigStore.Current;
                if (cfg == null || !cfg.SimEmitChipUid) return string.Empty;
                return "SIM-" + Name + "-" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            }
            catch { return string.Empty; }
        }

        /// <summary>디스패치 인자 구성 — GRAB(id=null)은 null, 그 외는 [id] 또는 chipUid 포함 [id, chipUid].</summary>
        private static string[] BuildDispatchArgs(string id, string chipUid)
        {
            if (id == null) return null;
            return string.IsNullOrEmpty(chipUid) ? new[] { id } : new[] { id, chipUid };
        }

        /// <summary>디스패치 결과가 '실행 실패'인지 — "fail:" 또는 "ERR" 시작.
        /// (대문자 "FAIL;..."=검사 NG 는 정상 결과이므로 실패로 보지 않음.)</summary>
        private static bool IsStepFail(string result)
            => !string.IsNullOrEmpty(result) &&
               (result.StartsWith("fail:", StringComparison.Ordinal) ||
                result.StartsWith("ERR", StringComparison.Ordinal));

        /// <summary>실패 결과에서 사유만 추출("fail:no image or train" → "no image or train").</summary>
        private static string ReasonOf(string result)
        {
            if (string.IsNullOrEmpty(result)) return "unknown";
            if (result.StartsWith("fail:", StringComparison.Ordinal)) return result.Substring(5).Trim();
            return result.Trim();
        }

        /// <summary>백엔드 실패 사유를 운영자가 읽기 쉬운 문구로 변환. 미지의 사유는 원문 유지.</summary>
        private static string HumanReason(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "원인 미상";
            string r = raw.ToLowerInvariant();
            if (r.Contains("train"))     return "학습(Train) 없음 — TRAIN 필요";
            if (r.Contains("no match"))  return "매칭 실패(no match)";
            if (r.Contains("finder"))    return "파인더 미정의";
            if (r.Contains("inspector")) return "검사기 미정의";
            if (r.Contains("grab"))      return "그랩 실패(카메라 확인)";
            if (r.Contains("module"))    return "모듈 미지정";
            return raw;
        }

        /// <summary>MATCH 결과(OK;...score=)의 score 가 finder AcceptThreshold 미만이면 NG 문구 반환, 아니면 null.
        /// AcceptThreshold 가 0 이하이면 게이트하지 않는다.</summary>
        private string MatchNgIfBelowThreshold(string cmd, string id, string result)
        {
            if (cmd != "MATCH" || string.IsNullOrEmpty(id)) return null;
            if (result == null || !result.StartsWith("OK", StringComparison.Ordinal)) return null;
            if (Module == null || !Module.Finders.TryGetValue(id, out var f) || f == null) return null;
            double thr = f.AcceptThreshold;
            if (thr <= 0.0) return null;
            double score = ParseScore(result);
            if (score >= thr) return null;
            return "score=" + score.ToString("F3") + " < " + thr.ToString("F2");
        }

        /// <summary>"OK;x=..;score=0.159" 같은 결과에서 score 값을 파싱(없으면 0).</summary>
        private static double ParseScore(string result)
        {
            try
            {
                int i = result.IndexOf("score=", StringComparison.Ordinal);
                if (i < 0) return 0.0;
                string s = result.Substring(i + 6);
                int j = s.IndexOf(';');
                if (j >= 0) s = s.Substring(0, j);
                double v;
                double.TryParse(s, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out v);
                return v;
            }
            catch { return 0.0; }
        }

        /// <summary>실패 처리 — 로그 + Alarm 후 -1 반환.</summary>
        protected int Fail(string alarmCode, string message)
        {
            try
            {
                WriteLog(Name, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, alarmCode, Name, message);
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
