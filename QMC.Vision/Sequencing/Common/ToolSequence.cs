using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Vision.Config;
using QMC.Vision.DieMaps;
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
            => !IsFinder && (Kind == SequenceModuleKind.FrontSideVision || Kind == SequenceModuleKind.RearSideVision);

        private bool IsBottomInspect()
            => !IsFinder && Kind == SequenceModuleKind.BottomInspection;

        private bool IsBinInspect()
            => !IsFinder && Kind == SequenceModuleKind.BinVision;

        public SequenceRunMode Mode { get; private set; } = SequenceRunMode.Auto;
        public int CycleIntervalMs { get; set; } = 500;

        // Sim 다이 인덱스(실제는 핸들러가 부여). 레시피 픽업 순서(InputDieMap+Pickup)를 따라 다이 좌표를 발급한다.
        // 픽업 순서를 못 구하면 X 고정 + Y 증가 폴백(구 동작).
        private const int DieIndexX = 27;
        private int _dieSeq;
        private int _curPicker, _curDie;   // 직전 스텝의 픽업/다이(로그 표시용)
        private List<int[]> _pickupOrder;  // 픽업 순서대로의 다이 좌표 {DieMapX, DieMapY}
        private string _pickupOrderKey;    // 캐시 무효화 키(레시피 사양/픽업 옵션 변경 감지)
        private int _lastWaferPass = -1;   // 직전 웨이퍼 패스 번호((seq-1)/순서수). 바뀌면 새 웨이퍼.

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
                // 정지→재개는 _dieSeq 를 유지해 다음 다이부터 이어서 진행(리셋 안 함).
                // 픽업 한 바퀴(웨이퍼)를 다 돌고 다음 바퀴로 넘어갈 때만 새 웨이퍼로 보고 맵/차트를 초기화한다.
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
                    int baseCh = (Kind == SequenceModuleKind.RearSideVision) ? 2 : 0;  // 앞=Front(0/1), 뒤=Back(2/3)
                    int seq = ++_dieSeq;                        // 이번 스텝 = 픽업 순서 상의 다이 1개
                    MaybeClearForNewWafer(seq);                 // 픽업 한 바퀴 완료 → 다음 웨이퍼면 맵/차트 초기화
                    int picker = ((seq - 1) % 4) + 1;           // 픽업 1→2→3→4 순환(4 픽커 갱)
                    int ix, iy; NextPickupCell(seq, out ix, out iy);
                    _curPicker = picker; _curDie = seq;         // 로그 표시용
                    chipUid = ResolveChipUid(ix, iy);           // 다이 기준 chipUid(검사기 간 집계 → 데이터로그 완결)
                    args = string.IsNullOrEmpty(chipUid) ? new[] { ToolId } : new[] { ToolId, chipUid };
                    string last = null;
                    for (int chOff = 0; chOff <= 1 && !ct.IsCancellationRequested; chOff++)   // ch1(0°)→ch2(90°)
                    {
                        QMC.Vision.Core.VisionCommandCore.SetInspectContext(picker, baseCh + chOff, ix, iy);
                        last = Context.Dispatch(Module, Cmd, args);   // INSPECT=GrabForTool(채널별 시뮬 이미지)+검사
                    }
                    QMC.Vision.Core.VisionCommandCore.SetInspectContext(0, -1, 0, 0);   // 컨텍스트 리셋
                    result = last;
                }
                else if (IsBottomInspect() || IsBinInspect())
                {
                    // 바텀/Die gap(Bin)도 픽업 1→2→3→4 순환(스텝당 다이 1개) — 레시피 픽업 순서의 다이 좌표를 컨텍스트로 부여.
                    int seq = ++_dieSeq;
                    MaybeClearForNewWafer(seq);   // 픽업 한 바퀴 완료 → 다음 웨이퍼면 맵/차트 초기화
                    int picker = ((seq - 1) % 4) + 1;
                    int ix, iy; NextPickupCell(seq, out ix, out iy);
                    _curPicker = picker; _curDie = seq;
                    chipUid = ResolveChipUid(ix, iy);           // 다이 기준 chipUid(검사기 간 집계 → 데이터로그 완결)
                    args = string.IsNullOrEmpty(chipUid) ? new[] { ToolId } : new[] { ToolId, chipUid };
                    QMC.Vision.Core.VisionCommandCore.SetInspectContext(picker, -1, ix, iy);
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
            string pk = (IsSideInspect() || IsBottomInspect() || IsBinInspect()) ? " [Picker " + _curPicker + " / Die " + _curDie + "]" : "";
            string line = "[SEQ-" + Name + "] " + (IsFinder ? "MATCH" : "INSPECT") + " " + ToolId + pk + " → " + verdict;
            WriteLog(Name, line);
            Context.LogPublic(line);
        }

        private static string Trim(string s)
        {
            if (string.IsNullOrEmpty(s)) return "-";
            return s.Length > 80 ? s.Substring(0, 80) : s;
        }

        /// <summary>이번 사이클 chipUid(다이 미지정) — SimEmitChipUid=true 면 합성 발급, 아니면 빈 값.</summary>
        private string ResolveChipUid() => ResolveChipUid(int.MinValue, int.MinValue);

        /// <summary>
        /// 다이(IndexX/IndexY) 기준 chipUid — SimEmitChipUid=true 일 때 Bottom/Side/Bin 이 같은 다이를
        /// 같은 chipUid 로 보고하도록 다이 좌표 기반으로 발급한다. 그래야 MaterialTracker 가 한 다이로 집계하고
        /// DataLogSaver 가 실제 구동과 동일한 완결 행(vision_YYYYMMDD.csv)을 남긴다.
        /// 좌표가 없으면(파인더 등) 시각 기반 합성. SimEmitChipUid=false 면 빈 값(로그/추적 생략).
        /// </summary>
        private string ResolveChipUid(int ix, int iy)
        {
            try
            {
                var cfg = VisionConfigStore.Current;
                if (cfg == null || !cfg.SimEmitChipUid) return string.Empty;
                if (ix != int.MinValue && iy != int.MinValue)
                {
                    int pass = _lastWaferPass < 0 ? 0 : _lastWaferPass;
                    return "SIM-W" + pass + "-X" + ix + "-Y" + iy;   // 다이 단위 고정 → 검사기 간 집계 가능
                }
                return "SIM-" + Name + "-" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// 픽업 순서 상 seq(1-base) 번째 다이의 격자 좌표(IndexX/IndexY)를 돌려준다.
        /// 활성 레시피의 InputDieMap(없으면 웨이퍼 사양으로 생성) + Pickup 옵션으로 순서를 만들고,
        /// 순서를 넘어가면 처음부터 순환한다. 순서를 못 구하면 X 고정 + Y 증가 폴백.
        /// </summary>
        private void NextPickupCell(int seq, out int ix, out int iy)
        {
            var order = EnsurePickupOrder();
            if (order != null && order.Count > 0)
            {
                int idx = (seq - 1) % order.Count;
                if (idx < 0) idx += order.Count;
                ix = order[idx][0];
                iy = order[idx][1];
                return;
            }
            ix = DieIndexX;   // 폴백(구 동작)
            iy = seq;
        }

        /// <summary>활성 레시피 기준 픽업 순서 좌표 목록(캐시). 사양/픽업 옵션 변경 시 재생성.</summary>
        private List<int[]> EnsurePickupOrder()
        {
            try
            {
                var recipe = QMC.Vision.Core.ActiveRecipeContext.Current;
                string key = BuildPickupKey(recipe);
                if (_pickupOrder != null && key == _pickupOrderKey) return _pickupOrder;
                _pickupOrderKey = key;
                _pickupOrder = BuildPickupOrder(recipe);
                return _pickupOrder;
            }
            catch
            {
                return _pickupOrder;
            }
        }

        private static string BuildPickupKey(VisionMachineRecipe r)
        {
            if (r == null) return "";
            string map = r.InputDieMap != null && r.InputDieMap.Entries != null
                ? "M" + r.InputDieMap.Entries.Count + "_" + r.InputDieMap.CreatedAt.Ticks
                : "G" + r.WaferGridX + "x" + r.WaferGridY + "_" + r.WaferPitchX + "_" + r.WaferPitchY
                  + "_" + r.WaferOuterDiameterMm + "_" + r.WaferSideEdgeSkip + "_" + r.WaferTopBottomEdgeSkip;
            var p = r.Pickup ?? new PickupSubset();
            return map + "|" + (int)p.StartCorner + (int)p.Direction + (int)p.Pattern;
        }

        private static List<int[]> BuildPickupOrder(VisionMachineRecipe r)
        {
            var list = new List<int[]>();
            if (r == null) return list;
            DieMap map = (r.InputDieMap != null && r.InputDieMap.Entries != null && r.InputDieMap.Entries.Count > 0)
                ? r.InputDieMap
                : DieMapBuilder.GenerateCircleDieMap(r.WaferGridX, r.WaferGridY, r.WaferPitchX, r.WaferPitchY,
                    r.WaferOuterDiameterMm, r.WaferSideEdgeSkip, r.WaferTopBottomEdgeSkip, "WAFER");
            var ordered = PickupSequenceGenerator.Build(map, r.Pickup);
            foreach (var e in ordered)
                if (e != null) list.Add(new[] { e.DieMapX, e.DieMapY });
            return list;
        }

        /// <summary>
        /// 픽업 순서 한 바퀴(웨이퍼)를 다 돌고 다음 바퀴로 넘어가면 새 웨이퍼로 보고 해당 모드의
        /// 결과 스토어(맵/차트)를 초기화한다. 첫 진입은 초기화 없이 패스 번호만 기록.
        /// </summary>
        private void MaybeClearForNewWafer(int seq)
        {
            try
            {
                var order = EnsurePickupOrder();
                int count = order != null ? order.Count : 0;
                if (count <= 0) return;
                int pass = (seq - 1) / count;
                if (pass == _lastWaferPass) return;
                bool newWafer = _lastWaferPass >= 0 && pass > _lastWaferPass;   // 첫 진입(-1)은 초기화만
                _lastWaferPass = pass;
                if (!newWafer) return;
                string mode = StoreModeKey();
                if (mode == null) return;
                QMC.Vision.Core.InspectionResultStore.Clear(mode);
                Context.LogPublic("[SEQ] " + Name + " 새 웨이퍼 시작 — " + mode + " 맵/차트 초기화");
            }
            catch { /* 초기화 실패는 시퀀스 진행에 영향 주지 않도록 흡수 */ }
        }

        /// <summary>이 도구의 검사 모드 → 결과 스토어 키(Bottom/Side/Bin). 검사 도구가 아니면 null.</summary>
        private string StoreModeKey()
        {
            switch (Kind)
            {
                case SequenceModuleKind.BottomInspection: return QMC.Vision.Core.InspectionResultStore.Bottom;
                case SequenceModuleKind.FrontSideVision:
                case SequenceModuleKind.RearSideVision: return QMC.Vision.Core.InspectionResultStore.Side;
                case SequenceModuleKind.BinVision:        return QMC.Vision.Core.InspectionResultStore.Bin;
                default:                                  return null;
            }
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
