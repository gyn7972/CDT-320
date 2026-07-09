using System;
using System.Collections.Generic;
using QMC.Vision.Config;
using QMC.Vision.Modules;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 비동기 검사 공용 코어 — INSPECTASYNC(그랩+즉시 검사) / INSPECTRESULT(대기형 응답) 실행 엔진.
    /// <para>TCP 서버(<see cref="QMC.Vision.Comm.VisionTcpServer"/>)와 일반 시퀀서의
    /// DirectVisionCommandDispatcher 가 공유한다 — TCP시뮬/일반 시퀀서 동작 동일 보장.</para>
    /// <para>흐름(즉시 처리, 2026-07-04 확정): 요청 즉시 STARTED(그랩 전 선응답 — 핸들러 빠른 스텝 이동용,
    /// 실제 촬상 완료 신호는 EPD 푸시) → 백그라운드 그랩(모듈별 게이트로 직렬화, 실기 카메라 보호)
    /// → 그랩 완료 즉시 해당 콜렛(전역 픽커 1~8)·채널의 '영속 인스턴스'로 곧바로 검사.
    /// 콜렛별 독립 인스턴스(8세트)를 가지므로 구(방식B)처럼 배치 4장을 모으지 않는다 —
    /// 검사끼리는 인스턴스가 달라 자연 병렬로 겹친다.</para>
    /// <para>같은 chip_uid 그룹(Side 0°+90° = 2건, Bottom/Bin = 1건)은 기대 수(<see cref="ExpectedPerUid"/>)
    /// 완료 시 합산 판정(모두 PASS 여야 PASS, t=최대값) 1회로 Complete. 결과요청 1회 = 완료까지 대기 후 응답 1회.</para>
    /// </summary>
    public static class AsyncInspectCore
    {
        // ── 모듈별 그랩 게이트 — 연속 INSPECTASYNC 백그라운드 그랩 직렬화(실기 동시그랩 방지 + 순서 보존).
        //    검사는 게이트 '밖'에서 실행 — 다음 그랩과 이전 검사가 겹친다(즉시 처리의 핵심).
        private static readonly Dictionary<string, object> _grabGates =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _gatesLock = new object();

        private static object GateOf(string module)
        {
            lock (_gatesLock)
            {
                if (!_grabGates.TryGetValue(module, out var g)) { g = new object(); _grabGates[module] = g; }
                return g;
            }
        }

        /// <summary>chip_uid 당 기대 결과 수 — Side=2(0°/90° 채널), Bottom/Bin=1.
        /// 콜렛 수와 무관(즉시 처리 — 배치 개념 없음).</summary>
        public static int ExpectedPerUid(IVisionModule m)
            => IsSideModule(m) ? 2 : 1;

        private static bool IsSideModule(IVisionModule m)
            => m?.Name != null && m.Name.IndexOf("Side", StringComparison.OrdinalIgnoreCase) >= 0;

        // ── chip_uid 그룹 집계(즉시 처리) — (모듈|검사기|uid) 키 ──
        private sealed class UidAgg
        {
            public int Remain;       // 남은 기대 결과 수(0 도달 시 합산 판정)
            public bool AnyFail;
            public string Error;     // 실행 오류(하나라도 있으면 ERR)
            public long TMaxMs;      // 알고리즘 소요 최대(ms)
            public string Extras;    // Bin 배치 오프셋 등 부가필드("x=..;y=..;..")
        }
        private static readonly Dictionary<string, UidAgg> _agg =
            new Dictionary<string, UidAgg>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _aggLock = new object();

        private static string AggKey(string module, string insp, string uid)
            => (module ?? "") + "|" + (insp ?? "") + "|" + (uid ?? "");

        /// <summary>비동기 검사 시작 — 즉시 "STARTED"/"fail:.." 반환(그랩 전 선응답), 그랩·검사는 백그라운드.
        /// picker: 전역 픽커 1~8(fb×4+콜렛). channel: 항상 0/1(Side 0°/90°, Bottom/Bin=0. 구형 수신만 -1).
        /// dieIndex: 픽업 순서 1-base(=결과 매칭 키 chipUid, 2026-07-06). 0=없음, -1=다이 없는 메뉴얼 테스트(맵/집계 생략).
        /// gridX/gridY: 핸들러가 와이어로 직접 내려준 웨이퍼 격자 인덱스(신형 "gridx;gridy") —
        /// 0 이상이면 그대로 사용(맵 조회 대체), 음수(구형)만 PickupOrderResolver 폴백.</summary>
        public static string Start(IVisionModule m, VisionSettings cfg, string insp,
                                   int picker, string chipUid, int dieIndex, int channel,
                                   int gridX = -1, int gridY = -1)
        {
            if (m == null) return "fail:no module";
            if (string.IsNullOrEmpty(insp)) return "fail:no inspector";
            if (!m.Inspectors.ContainsKey(insp)) return "fail:inspector not found";

            int ix = 0, iy = 0;
            if (gridX >= 0 && gridY >= 0)
            { ix = gridX; iy = gridY; }   // 신형 — 핸들러 grid 수신값 그대로(레시피 맵 조회 대체)
            else if (dieIndex > 0 && !QMC.Vision.DieMaps.PickupOrderResolver.TryGetCell(dieIndex, out ix, out iy))
            { ix = 0; iy = 0; }   // 구형 폴백 — 레시피 순서를 못 구하면 맵 표시만 생략(검사는 정상 진행)

            // 검사 사용 게이트 OFF → 그랩 없이 즉시 완료(스킵).
            if (VisionCommandCore.IsInspectionSkipped(m, insp))
            {
                ModuleResultStore.Record(m.Name, insp, true, "inspection=skip");
                AsyncMatchStore.Complete(m.Name, insp, chipUid, "PASS;inspection=skip");
                return "STARTED";
            }

            // uid 그룹 시작/참여 — 첫 요청이면 집계 생성 + Running 표시. 진행 중이면 리셋 없이 참여
            // (Side 는 0° 요청과 90° 요청이 시간차로 들어와 같은 uid 를 완성한다).
            string key = AggKey(m.Name, insp, chipUid);
            lock (_aggLock)
            {
                if (!_agg.TryGetValue(key, out var exist))
                {
                    // 채널 명시(운영 0/1)만 그룹 기대 수 적용 — 채널 없는 구형/수동(-1)은 단건 완결(대기 방지).
                    _agg[key] = new UidAgg { Remain = channel >= 0 ? ExpectedPerUid(m) : 1 };
                    AsyncMatchStore.Start(m.Name, insp, chipUid);
                }
            }

            long gen = InspectionResultStore.GenerationOf(m.Name);
            System.Threading.Tasks.Task.Run(() =>
            {
                System.Drawing.Bitmap keep = null;
                try
                {
                    // 요청 1회 = 그랩 1장. 그랩만 직렬화(카메라 보호), 채널/좌표 컨텍스트 주입.
                    GrabResult g;
                    lock (GateOf(m.Name))
                    {
                        bool setCtx = channel >= 0 || picker > 0;
                        if (setCtx) VisionCommandCore.SetInspectContext(m.Name, picker, channel, ix, iy);
                        try { g = m.GrabForTool(insp); }
                        finally { if (setCtx) VisionCommandCore.SetInspectContext(m.Name, 0, -1, 0, 0); }
                    }
                    if (g == null || !g.IsSuccess)
                    {
                        try { g?.Dispose(); } catch { }
                        FailUid(m.Name, insp, chipUid, g?.ErrorMessage ?? "grab");
                        return;
                    }
                    keep = g.DetachImage();   // 사본 대신 소유권 이전 — 고해상도 복제 제거
                    g.Dispose();

                    // ── 즉시 검사(배치 대기 없음) — 게이트 밖이라 다음 그랩과 병렬 ──
                    // 웨이퍼 경계(Clear) 이전에 시작된 잔여 요청 — 새 맵에 유령 셀이 생기므로 폐기.
                    if (gen != InspectionResultStore.GenerationOf(m.Name))
                    { FailUid(m.Name, insp, chipUid, "stale wafer(폐기 — 새 웨이퍼 초기화 이후 도착)"); return; }

                    // 콜렛(전역 픽커)·채널별 영속 인스턴스 — 파라미터는 레시피 1벌 공유(사용 직전 반사 복제).
                    if (!ColletInspectorCache.TryGet(m.Name, insp, picker, channel, out var ins))
                    { FailUid(m.Name, insp, chipUid, "inspector create fail"); return; }

                    UnitContext.ApplyScale(ins, m.ScaleX, m.ScaleY);
                    var template = m.Inspectors.TryGetValue(insp, out var t) ? t : null;
                    if (template != null) VisionCommandCore.CopyInspectorConfig(template, ins);

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    string res = VisionCommandCore.InspectOnImageExplicit(
                        m, cfg, insp, ins, keep, chipUid, picker, channel, ix, iy);
                    sw.Stop();

                    ApplyUidResult(m.Name, insp, chipUid, channel, res, sw.ElapsedMilliseconds);
                }
                catch (Exception ex) { FailUid(m.Name, insp, chipUid, ex.Message); }
                finally { try { keep?.Dispose(); } catch { } }
            });
            return "STARTED";
        }

        /// <summary>비동기 검사 결과 — 대기형 응답(요청 1회 = 데이터 응답 1회).
        /// 완료까지 최대 6s(클라이언트 IO 타임아웃 8s 미만) 대기 후 "1;PASS|FAIL;.."/"ERR;사유".
        /// 만료/미시작 시 "0"(구형 폴링 호환 — 재요청). 즉시 처리 방식이라 부분 배치 안전망은 불필요.</summary>
        public static string WaitResult(IVisionModule m, VisionSettings cfg, string insp, string chipUid)
        {
            if (m == null) return "fail:no module";
            if (string.IsNullOrEmpty(insp)) return "fail:no inspector";

            const int WaitMs = 6000;
            const int StepMs = 30;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (; ; )
            {
                var st = AsyncMatchStore.TryGet(m.Name, insp, chipUid, out string payload);
                switch (st)
                {
                    case AsyncMatchStore.State.Done: return "1;" + payload;
                    case AsyncMatchStore.State.Error: return "ERR;" + payload;
                    case AsyncMatchStore.State.None: return "0";   // 미시작(INSPECTASYNC 전) — 즉시 반환
                }
                if (sw.ElapsedMilliseconds >= WaitMs) return "0";
                System.Threading.Thread.Sleep(StepMs);
            }
        }

        /// <summary>uid 결과 1건 반영 — 기대 수 도달 시 합산 판정(모두 PASS 여야 PASS)으로 1회 Complete.
        /// 검사 부가필드(Bottom 원본 항목, Bin 배치 offset 등)는 완료 페이로드에 그대로 보존한다.</summary>
        private static void ApplyUidResult(string module, string insp, string uid, int channel, string res, long algoMs)
        {
            string extras = null; bool fail = false; string err = null;
            if (res != null && (res.StartsWith("PASS", StringComparison.Ordinal) || res.StartsWith("FAIL", StringComparison.Ordinal)))
            {
                if (!res.StartsWith("PASS", StringComparison.Ordinal)) fail = true;
                int sc = res.IndexOf(';');
                if (sc >= 0) extras = res.Substring(sc + 1);   // Bin: "x=..;y=..;width=..;height=.."
                if (!string.IsNullOrWhiteSpace(extras) && IsSideModuleName(module))
                    extras = PrefixExtras(extras, "ch" + channel.ToString() + "_");
            }
            else
            {
                err = res ?? "no result";
            }

            UidAgg done = null;
            string key = AggKey(module, insp, uid);
            lock (_aggLock)
            {
                if (!_agg.TryGetValue(key, out var agg))
                    return;   // 이미 실패 확정(FailUid) 등으로 제거된 그룹 — 결과 폐기
                if (fail) agg.AnyFail = true;
                if (err != null && agg.Error == null) agg.Error = err;
                if (algoMs > agg.TMaxMs) agg.TMaxMs = algoMs;
                if (extras != null) agg.Extras = CombineExtras(agg.Extras, extras);
                agg.Remain--;
                if (agg.Remain <= 0) { done = agg; _agg.Remove(key); }
            }
            if (done == null) return;

            if (done.Error != null)
            {
                AsyncMatchStore.Fail(module, insp, uid, done.Error);
                return;
            }
            string verdict = done.AnyFail ? "FAIL" : "PASS";
            string extrasPayload = NormalizeExtras(done.Extras);
            AsyncMatchStore.Complete(module, insp, uid, verdict + extrasPayload + ";algo_ms=" + done.TMaxMs);
        }

        private static string NormalizeExtras(string extras)
        {
            if (string.IsNullOrWhiteSpace(extras))
                return string.Empty;

            string value = extras.Trim();
            return value.StartsWith(";", StringComparison.Ordinal) ? value : ";" + value;
        }

        private static bool IsSideModuleName(string module)
        {
            return !string.IsNullOrWhiteSpace(module) &&
                   module.IndexOf("Side", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string CombineExtras(string current, string next)
        {
            if (string.IsNullOrWhiteSpace(current))
                return next ?? string.Empty;
            if (string.IsNullOrWhiteSpace(next))
                return current;

            return current.TrimEnd(';') + ";" + next.TrimStart(';');
        }

        private static string PrefixExtras(string extras, string prefix)
        {
            if (string.IsNullOrWhiteSpace(extras) || string.IsNullOrWhiteSpace(prefix))
                return extras;

            var tokens = extras.Split(';');
            var output = new List<string>();
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                int eq = token.IndexOf('=');
                if (eq <= 0)
                {
                    output.Add(token);
                    continue;
                }

                output.Add(prefix + token.Substring(0, eq).Trim() + token.Substring(eq));
            }

            return string.Join(";", output);
        }

        /// <summary>uid 그룹 실패 확정 — 집계 제거 + ERR 저장(핸들러 INSPECTRESULT 가 즉시 ERR 수신).
        /// 이후 같은 uid 의 남은 채널 결과는 폐기된다(그룹 재시작은 새 INSPECTASYNC 부터).</summary>
        private static void FailUid(string module, string insp, string uid, string reason)
        {
            lock (_aggLock) _agg.Remove(AggKey(module, insp, uid));
            AsyncMatchStore.Fail(module, insp, uid, reason);
        }
    }
}
