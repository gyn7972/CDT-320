using System;
using System.Collections.Generic;
using QMC.Vision.Config;
using QMC.Vision.Modules;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 비동기 배치 검사 공용 코어 — INSPECTASYNC(그랩만 보관) / INSPECTRESULT(대기형 응답) 실행 엔진.
    /// <para>TCP 서버(<see cref="QMC.Vision.Comm.VisionTcpServer"/>)와 일반 시퀀서의
    /// DirectVisionCommandDispatcher 가 공유한다 — TCP시뮬/일반 시퀀서 동작 동일 보장.</para>
    /// <para>흐름(방식 B): 요청 즉시 STARTED → 백그라운드 그랩(모듈별 게이트로 직렬화, 실기 카메라 보호)
    /// → 보관 개수가 배치 크기(Bottom/Bin=FB그룹 콜렛4, Side=자기 카메라 콜렛4×채널2=8)에 도달하면
    /// 일괄 병렬 검사 자동 시작 → 결과요청 1회 = 완료까지 대기 후 데이터 응답 1회.</para>
    /// <para>8콜렛 순차 규약: Front 배치(전역픽커 1~4) 완료 후 Back 배치(5~8)가 진행되므로
    /// Bottom/Bin 은 사이클당 4장 배치가 2회(F→B) 돈다. 같은 chip_uid 그룹(측면 0°+90°)은
    /// 전 채널 완료 후 합산 판정(모두 PASS 여야 PASS)으로 1회 Complete.</para>
    /// </summary>
    public static class AsyncInspectCore
    {
        // ── 모듈별 그랩 게이트 — 연속 INSPECTASYNC 백그라운드 그랩 직렬화(실기 동시그랩 방지 + 순서 보존) ──
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

        /// <summary>배치 크기(보관 이미지 수) — Bottom/Bin=FB그룹당 콜렛 4장, Side=자기 카메라 콜렛 4 × 채널(0°/90°) 2 = 8장.
        /// 실기 촬영 순서(8콜렛 순차): Front 콜렛 1~4 Bottom → Front 카메라(콜렛당 0°→90°) →
        /// Back 콜렛 1~4 Bottom → Back 카메라. Front/Back 동시 촬영 없음(상호배제) —
        /// Side 모듈은 자기 그룹 콜렛만 담당하므로 배치 8장(4콜렛×2채널)이 유지된다.
        /// TODO: 머신 설정 승격.</summary>
        public static int ExpectedBatchCount(IVisionModule m)
            => IsSideModule(m) ? ColletAddress.ColletsPerGroup * 2 : ColletAddress.ColletsPerGroup;

        private static bool IsSideModule(IVisionModule m)
            => m?.Name != null && m.Name.IndexOf("Side", StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>비동기 검사 시작 — 즉시 "STARTED"/"fail:.." 반환, 그랩·검사는 백그라운드.
        /// picker: 전역 픽커 1~8(fb×4+콜렛). channel: 측면 0(0°)/1(90°), 없으면 -1.
        /// dieIndex: 픽업 순서 1-base(레시피 칩위치 매칭). 0=없음, -1=다이 없는 메뉴얼 테스트(맵/집계 생략).</summary>
        public static string Start(IVisionModule m, VisionSettings cfg, string insp,
                                   int picker, string chipUid, int dieIndex, int channel)
        {
            if (m == null) return "fail:no module";
            if (string.IsNullOrEmpty(insp)) return "fail:no inspector";
            if (!m.Inspectors.ContainsKey(insp)) return "fail:inspector not found";

            int ix = 0, iy = 0;
            if (dieIndex > 0 && !QMC.Vision.DieMaps.PickupOrderResolver.TryGetCell(dieIndex, out ix, out iy))
            { ix = 0; iy = 0; }   // 레시피 순서를 못 구하면 맵 표시만 생략(검사는 정상 진행)

            // 검사 사용 게이트 OFF → 그랩 없이 즉시 완료(스킵).
            if (VisionCommandCore.IsInspectionSkipped(m, insp))
            {
                ModuleResultStore.Record(m.Name, insp, true, "inspection=skip");
                AsyncMatchStore.Complete(m.Name, insp, chipUid, "PASS;inspection=skip");
                return "STARTED";
            }

            AsyncMatchStore.Start(m.Name, insp, chipUid);
            PendingGrabStore.NoteGrabStarted(m.Name);   // 안전망 게이트 — finally 에서 해제
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // 요청 1회 = 그랩 1장. 채널은 명령에서(측면 0°/90° — 트리거 각도별로 핸들러가 요청).
                    // 그랩 직렬화 + 채널/좌표 컨텍스트(측면 채널별 저장이미지 선택 등은 모듈 컨텍스트를 읽는다).
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
                        AsyncMatchStore.Fail(m.Name, insp, chipUid, g?.ErrorMessage ?? "grab");
                        return;
                    }
                    // 사본 대신 소유권 이전 — 고해상도(수백 MB) 복제 제거.
                    System.Drawing.Bitmap keep = g.DetachImage();
                    g.Dispose();
                    int n = PendingGrabStore.Add(m.Name, insp, chipUid, picker, ix, iy, channel, keep);

                    // 보관 개수가 배치 크기 도달 → 그 자리에서 일괄 병렬 처리 자동 시작(멱등).
                    if (n >= ExpectedBatchCount(m) && PendingGrabStore.TryBeginProcessing(m.Name))
                        ProcessPendingBatchParallel(m, cfg, insp);
                }
                catch (Exception ex) { AsyncMatchStore.Fail(m.Name, insp, chipUid, ex.Message); }
                finally { PendingGrabStore.NoteGrabEnded(m.Name); }
            });
            return "STARTED";
        }

        /// <summary>비동기 검사 결과 — 대기형 응답(요청 1회 = 데이터 응답 1회).
        /// 완료까지 최대 6s(클라이언트 IO 타임아웃 8s 미만) 대기 후 "1;PASS|FAIL;.."/"ERR;사유".
        /// 만료/미시작 시 "0"(구형 폴링 호환 — 재요청). 안전망(부분 배치 구제)은 유예 2s 경과 AND
        /// 진행 중 그랩 없음일 때만 발화 — 그랩 축적 중 조기 발화로 배치가 쪼개지는 것을 방지.</summary>
        public static string WaitResult(IVisionModule m, VisionSettings cfg, string insp, string chipUid)
        {
            if (m == null) return "fail:no module";
            if (string.IsNullOrEmpty(insp)) return "fail:no inspector";

            const int WaitMs = 6000;
            const int StepMs = 30;
            const int SafetyMs = 2000;
            bool safetyTried = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (;;)
            {
                var st = AsyncMatchStore.TryGet(m.Name, insp, chipUid, out string payload);
                switch (st)
                {
                    case AsyncMatchStore.State.Done:  return "1;" + payload;
                    case AsyncMatchStore.State.Error: return "ERR;" + payload;
                    case AsyncMatchStore.State.None:  return "0";   // 미시작(INSPECTASYNC 전) — 즉시 반환
                }
                if (!safetyTried && sw.ElapsedMilliseconds >= SafetyMs && !PendingGrabStore.HasInFlight(m.Name))
                {
                    safetyTried = true;
                    if (PendingGrabStore.TryBeginProcessing(m.Name))
                        ProcessPendingBatchParallel(m, cfg, insp);
                }
                if (sw.ElapsedMilliseconds >= WaitMs) return "0";
                System.Threading.Thread.Sleep(StepMs);
            }
        }

        /// <summary>보관된 그랩 전체를 '콜렛(전역 픽커 1~8)/채널별 영속 인스턴스'로 병렬 검사(공유 인스펙터 락 회피).
        /// 결과는 chip_uid 그룹 단위로 <see cref="AsyncMatchStore"/> 에 저장 — 같은 uid 의 항목(측면 2채널)은
        /// 전원 완료 후 합산 판정(모두 PASS 여야 PASS, t=최대값) 1회. Bin 배치검사의 x/y 오프셋은 보존.</summary>
        private static void ProcessPendingBatchParallel(IVisionModule m, VisionSettings cfg, string insp)
        {
            var items = PendingGrabStore.Take(m.Name);
            if (items.Count == 0) return;
            var template = m.Inspectors.TryGetValue(insp, out var t) ? t : null;

            var remain = new System.Collections.Concurrent.ConcurrentDictionary<string, int>(StringComparer.Ordinal);
            var anyFail = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>(StringComparer.Ordinal);
            var errors = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var tMax   = new System.Collections.Concurrent.ConcurrentDictionary<string, long>(StringComparer.Ordinal);
            var extras = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            foreach (var it in items) remain.AddOrUpdate(it.ChipUid ?? "", 1, (_k, v) => v + 1);

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    long curGen = InspectionResultStore.GenerationOf(m.Name);
                    System.Threading.Tasks.Parallel.For(0, items.Count, i =>
                    {
                        var it = items[i];
                        int picker = it.Picker > 0 ? it.Picker : (i + 1);
                        string uid = it.ChipUid ?? "";
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        try
                        {
                            // 웨이퍼 경계(Clear) 이전에 그랩된 잔여 항목 — 새 맵에 유령 셀이 생기므로 폐기.
                            if (it.Gen != curGen)
                            { errors[uid] = "stale wafer batch(폐기 — 새 웨이퍼 초기화 이후 도착)"; return; }
                            // 콜렛(전역 픽커)·채널별 '영속' 인스펙터 인스턴스(8콜렛 확정 정책) —
                            // 매 배치 생성 대신 ColletInspectorCache 재사용(속도), 파라미터는 레시피 1벌 공유.
                            if (!ColletInspectorCache.TryGet(m.Name, it.Insp, picker, it.Channel, out var ins))
                            { errors[uid] = "inspector create fail"; return; }

                            UnitContext.ApplyScale(ins, m.ScaleX, m.ScaleY);
                            if (template != null) VisionCommandCore.CopyInspectorConfig(template, ins);   // 레시피 파라미터 복제(공유 레시피 → 인스턴스 반영)

                            string res = VisionCommandCore.InspectOnImageExplicit(
                                m, cfg, it.Insp, ins, it.Image, it.ChipUid, picker, it.Channel, it.IndexX, it.IndexY);
                            sw.Stop();

                            if (res != null && (res.StartsWith("PASS") || res.StartsWith("FAIL")))
                            {
                                if (!res.StartsWith("PASS")) anyFail[uid] = true;
                                int sc = res.IndexOf(';');
                                if (sc >= 0) extras[uid] = res.Substring(sc + 1);   // Bin: "x=..;y=..;width=..;height=.."
                            }
                            else errors[uid] = res ?? "no result";
                        }
                        catch (Exception ex) { errors[uid] = ex.Message; }
                        finally
                        {
                            try { it.Image?.Dispose(); } catch { }
                            tMax.AddOrUpdate(uid, sw.ElapsedMilliseconds, (_k, v) => Math.Max(v, sw.ElapsedMilliseconds));
                            if (remain.AddOrUpdate(uid, 0, (_k, v) => v - 1) <= 0)
                                CompleteGroup(m.Name, it.Insp, uid, anyFail, errors, tMax, extras);
                        }
                    });
                }
                catch (Exception ex)
                {
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "InspectBatch",
                        m.Name + " 병렬 처리 실패: " + ex.Message);
                }
            });
        }

        /// <summary>chip_uid 그룹 완료 — 실행 오류가 있으면 ERR, 아니면 합산 판정으로 Complete.
        /// 규약 완료 포맷: "PASS|FAIL;x=..;y=..;t=..;score=" (x/y 는 Bin 오프셋 등 부가필드에서 추출, 없으면 빈 값).</summary>
        private static void CompleteGroup(string module, string insp, string uid,
            System.Collections.Concurrent.ConcurrentDictionary<string, bool> anyFail,
            System.Collections.Concurrent.ConcurrentDictionary<string, string> errors,
            System.Collections.Concurrent.ConcurrentDictionary<string, long> tMax,
            System.Collections.Concurrent.ConcurrentDictionary<string, string> extras)
        {
            if (errors.TryGetValue(uid, out string reason))
            {
                AsyncMatchStore.Fail(module, insp, uid, reason);
                return;
            }
            string verdict = anyFail.TryGetValue(uid, out bool f) && f ? "FAIL" : "PASS";
            long tms = tMax.TryGetValue(uid, out long tv) ? tv : 0;
            string x = "", y = "";
            if (extras.TryGetValue(uid, out string ex2) && !string.IsNullOrEmpty(ex2))
                foreach (var tok in ex2.Split(';'))
                {
                    if (tok.StartsWith("x=", StringComparison.OrdinalIgnoreCase)) x = tok.Substring(2);
                    else if (tok.StartsWith("y=", StringComparison.OrdinalIgnoreCase)) y = tok.Substring(2);
                }
            AsyncMatchStore.Complete(module, insp, uid, verdict + ";x=" + x + ";y=" + y + ";t=" + tms + ";score=");
        }
    }
}
