using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Vision.Config;
using QMC.Vision.Modules;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// 모듈 안의 도구(Finder 1개 또는 Inspector 1개)를 "개별 시퀀서"로 구동하는 단위.
    /// 모듈 전체를 한 번에 돌리는 <see cref="ModuleSequenceBase"/> 와 달리, 사용자가 도구를 골라
    /// 단독으로 Auto 연속/Step 1회 실행할 수 있다. 각 사이클은 자체 GRAB 후 해당 도구만 디스패치한다.
    /// 명령 실행은 <see cref="VisionSequenceContext.Dispatch"/>(공유 코어) 경유 — 실제 TCP 경로와 동일 결과.
    /// </summary>
    public sealed class ToolSequence
    {
        public ToolSequence(VisionSequenceContext ctx, SequenceModuleKind kind,
                            IVisionModule module, string moduleName, string cmd, string toolId)
        {
            Context    = ctx ?? throw new ArgumentNullException(nameof(ctx));
            Kind       = kind;
            Module     = module ?? throw new ArgumentNullException(nameof(module));
            ModuleName = string.IsNullOrWhiteSpace(moduleName) ? kind.ToString() : moduleName;
            Cmd        = (cmd ?? string.Empty).ToUpperInvariant();
            ToolId     = toolId ?? string.Empty;
            Name       = ModuleName + "." + ToolId;
            Status     = "-";
            LastResult = string.Empty;
        }

        public VisionSequenceContext Context { get; }
        public SequenceModuleKind Kind { get; }
        public IVisionModule Module { get; }
        public string ModuleName { get; }
        /// <summary>"MATCH"(Finder) 또는 "INSPECT"(Inspector).</summary>
        public string Cmd { get; }
        public string ToolId { get; }
        public string Name { get; }
        public bool IsFinder => Cmd == "MATCH";
        /// <summary>측면(앞/뒤) 검사 INSPECT — 픽커 1~4 분배(핸들러 TCP 픽커 인덱스 모사).</summary>
        private bool IsSideInspect()
            => !IsFinder && (Kind == SequenceModuleKind.TopSideVision || Kind == SequenceModuleKind.BottomSideVision);

        private bool IsBottomInspect()
            => !IsFinder && Kind == SequenceModuleKind.BottomInspection;

        public SequenceRunMode Mode { get; private set; } = SequenceRunMode.Auto;
        public int CycleIntervalMs { get; set; } = 500;

        // 측면 시뮬: 다이 인덱스(실제는 핸들러가 부여). X 고정 + Y 증가로 운영뷰처럼 다이 누적.
        private const int DieIndexX = 27;
        private int _dieSeq;
        private int _curPicker, _curDie;   // 직전 스텝의 픽업/다이(로그 표시용)

        /// <summary>직전 사이클 소요(ms).</summary>
        public double LastCycleMs { get; private set; }
        /// <summary>완료 사이클 수.</summary>
        public long CycleCount { get; private set; }
        /// <summary>직전 디스패치 원문 결과.</summary>
        public string LastResult { get; private set; }
        /// <summary>직전 판정("OK"/"NG"/"-").</summary>
        public string Status { get; private set; }

        public void Configure(SequenceRunMode mode)
        {
            Mode = mode;
            CycleIntervalMs = Context.CycleIntervalMs > 0 ? Context.CycleIntervalMs : CycleIntervalMs;
        }

        /// <summary>Auto 모드 연속 실행 — 취소까지 자체 그랩+도구 사이클을 반복한다.</summary>
        public async Task RunAutoAsync(CancellationToken ct)
        {
            try
            {
                Context.LogPublic("[SEQ] " + Name + " 도구 연속 실행 시작");
                while (!ct.IsCancellationRequested)
                {
                    await RunCycleAsync(ct, grab: true).ConfigureAwait(false);
                    await Task.Delay(CycleIntervalMs, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { /* 정상 정지 */ }
            catch (Exception ex) { Context.LogPublic("[SEQ] " + Name + " 연속 실행 실패: " + ex.Message); }
        }

        /// <summary>도구 1사이클: (옵션)GRAB → 해당 도구 디스패치 → 판정/로그/메트릭. 성공 0 / 실패 -1.</summary>
        public async Task<int> RunCycleAsync(CancellationToken ct, bool grab)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                ct.ThrowIfCancellationRequested();

                string chipUid = ResolveChipUid();

                if (grab)
                {
                    string g = Context.Dispatch(Module, "GRAB", null);
                    if (IsExecFail(g))
                    {
                        Status = "NG";
                        LastResult = g;
                        string gl = "[SEQ-" + Name + "] GRAB → NG (" + HumanReason(ReasonOf(g)) + ")";
                        WriteLog(Name, gl); Context.LogPublic(gl);
                        return -1;
                    }
                }

                ct.ThrowIfCancellationRequested();
                string[] args = string.IsNullOrEmpty(chipUid) ? new[] { ToolId } : new[] { ToolId, chipUid };

                string result;
                if (IsSideInspect())
                {
                    // 실제 동작: 픽업 4열이 X로 지나가며 한 스텝에 픽업 1개를 찍는다(다음 스텝 = 다음 픽업).
                    // 그 픽업을 Front/Back 카메라가 "동시" 촬영하고 각 카메라가 채널 0°/90° 2장 → 이 모듈은 ch1(0°)+ch2(90°).
                    int baseCh = (Kind == SequenceModuleKind.BottomSideVision) ? 2 : 0;  // 앞=Front(0/1), 뒤=Back(2/3)
                    int dieY = ++_dieSeq;                       // 이번 스텝 = 다이 1개(X 고정/Y 증가)
                    int picker = ((dieY - 1) % 4) + 1;          // 픽업 1→2→3→4 순환(4열을 차례로 통과)
                    _curPicker = picker; _curDie = dieY;        // 로그 표시용
                    string last = null;
                    for (int chOff = 0; chOff <= 1 && !ct.IsCancellationRequested; chOff++)   // ch1(0°)→ch2(90°)
                    {
                        QMC.Vision.Core.VisionCommandCore.SetInspectContext(picker, baseCh + chOff, DieIndexX, dieY);
                        last = Context.Dispatch(Module, Cmd, args);   // INSPECT=GrabForTool(채널별 시뮬 이미지)+검사
                    }
                    QMC.Vision.Core.VisionCommandCore.SetInspectContext(0, -1, 0, 0);   // 컨텍스트 리셋
                    result = last;
                }
                else if (IsBottomInspect())
                {
                    // 바텀도 픽업 1→2→3→4 순환(스텝당 다이 1개) — 운영뷰 맵/그리드가 픽업별로 누적되도록 컨텍스트 부여.
                    int dieY = ++_dieSeq;
                    int picker = ((dieY - 1) % 4) + 1;
                    _curPicker = picker; _curDie = dieY;
                    QMC.Vision.Core.VisionCommandCore.SetInspectContext(picker, -1, DieIndexX, dieY);
                    result = Context.Dispatch(Module, Cmd, args);
                    QMC.Vision.Core.VisionCommandCore.SetInspectContext(0, -1, 0, 0);   // 컨텍스트 리셋
                }
                else
                {
                    result = Context.Dispatch(Module, Cmd, args);
                }
                LastResult = result ?? string.Empty;

                int rc = Judge(result);
                await Task.Yield();
                return rc;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Status = "NG";
                string l = "[SEQ-" + Name + "] 사이클 실패: " + ex.Message;
                WriteLog(Name, l); Context.LogPublic(l);
                return -1;
            }
            finally { sw.Stop(); LastCycleMs = sw.Elapsed.TotalMilliseconds; CycleCount++; }
        }

        // ── 판정 ────────────────────────────────────────────────
        /// <summary>디스패치 결과를 판정하여 Status 설정 + 로그. 성공 0 / NG·실패 -1.</summary>
        private int Judge(string result)
        {
            // 1) 실행 실패(학습 없음·명령 오류 등) — "fail:" / "ERR".
            if (IsExecFail(result))
            {
                Status = "NG";
                string reason = HumanReason(ReasonOf(result));
                Log("NG (" + reason + ")");
                return -1;
            }
            // 2) MATCH score 게이트 — AcceptThreshold 미만이면 NG.
            string belowNg = MatchNgIfBelowThreshold(result);
            if (belowNg != null)
            {
                Status = "NG";
                Log("NG (" + belowNg + ")");
                return -1;
            }
            // 3) 검사 NG 판정(정상 결과) — "FAIL;..." (대문자/세미콜론).
            if (!string.IsNullOrEmpty(result) && result.StartsWith("FAIL", StringComparison.Ordinal))
            {
                Status = "NG";
                Log("NG (" + Trim(result) + ")");
                return -1;
            }
            // 4) 정상.
            Status = "OK";
            Log("OK (" + Trim(result) + ")");   // 정상 로그도 표시(Auto 포함). 추후 On/Off 토글로 제어 예정
            return 0;
        }

        private void Log(string verdict)
        {
            // 측면 INSPECT 는 어떤 픽업/다이였는지 함께 표시(운영뷰 대응).
            string pk = (IsSideInspect() || IsBottomInspect()) ? " [Picker " + _curPicker + " / Die " + _curDie + "]" : "";
            string line = "[SEQ-" + Name + "] " + (IsFinder ? "MATCH" : "INSPECT") + " " + ToolId + pk + " → " + verdict;
            WriteLog(Name, line);
            Context.LogPublic(line);
        }

        private static string Trim(string s)
        {
            if (string.IsNullOrEmpty(s)) return "-";
            return s.Length > 80 ? s.Substring(0, 80) : s;
        }

        /// <summary>이번 사이클 chipUid — SimEmitChipUid=true 면 합성 발급, 아니면 빈 값.</summary>
        private string ResolveChipUid()
        {
            try
            {
                var cfg = VisionConfigStore.Current;
                if (cfg == null || !cfg.SimEmitChipUid) return string.Empty;
                return "SIM-" + Name + "-" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            }
            catch { return string.Empty; }
        }

        /// <summary>실행 실패 여부 — "fail:" 또는 "ERR" 시작(대문자 "FAIL;"=검사 NG 는 제외).</summary>
        private static bool IsExecFail(string result)
            => !string.IsNullOrEmpty(result) &&
               (result.StartsWith("fail:", StringComparison.Ordinal) ||
                result.StartsWith("ERR", StringComparison.Ordinal));

        private static string ReasonOf(string result)
        {
            if (string.IsNullOrEmpty(result)) return "unknown";
            if (result.StartsWith("fail:", StringComparison.Ordinal)) return result.Substring(5).Trim();
            return result.Trim();
        }

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
            if (r.Contains("no image"))  return "이미지 없음 — 그랩 필요";
            return raw;
        }

        /// <summary>MATCH 결과 score 가 AcceptThreshold 미만이면 NG 문구, 아니면 null.</summary>
        private string MatchNgIfBelowThreshold(string result)
        {
            if (!IsFinder) return null;
            if (result == null || !result.StartsWith("OK", StringComparison.Ordinal)) return null;
            if (!Module.Finders.TryGetValue(ToolId, out var f) || f == null) return null;
            double thr = f.AcceptThreshold;
            if (thr <= 0.0) return null;
            double score = ParseScore(result);
            if (score >= thr) return null;
            return "score=" + score.ToString("F3") + " < " + thr.ToString("F2");
        }

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

        private static void WriteLog(string source, string message)
        {
            try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "SEQ", source, message); }
            catch { }
        }
    }
}
