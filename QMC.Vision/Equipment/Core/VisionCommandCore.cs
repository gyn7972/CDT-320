using System;
using System.Diagnostics;
using System.Linq;
using QMC.Vision.Config;
using QMC.Vision.Modules;

namespace QMC.Vision.Core
{
    /// <summary>
    /// GRAB/MATCH/INSPECT/TRAIN 명령의 공통 처리부.
    /// <para>핸들러 구동(<c>VisionTcpServer</c>)과 자체 시퀀서(<c>DirectVisionCommandDispatcher</c>)가
    /// 동일 구현을 공유한다 — mm 변환·이미지로그·자재추적·데이터로그를 한 곳에서 일관되게 수행.</para>
    /// chipUid 가 있으면(핸들러 실자재 흐름) 로그/추적을 수행하고, 비어있거나 "Manual" 이면 측정/시뮬로 보고 생략.
    /// </summary>
    public static class VisionCommandCore
    {
        /// <summary>그랩/알고리즘 소요시간 로그 ON/OFF(병목 측정용). 운영 중 끄려면 false.</summary>
        public static bool TimingLogEnabled = true;

        /// <summary>1장 그랩. "w=..;h=..;frame=.." 또는 "fail:..".</summary>
        public static string Grab(IVisionModule m)
        {
            if (m == null) return "fail:no module";
            var swGrab = Stopwatch.StartNew();
            using (var g = m.Grab())
            {
                swGrab.Stop();
                LogTiming(m.Name, "GRAB", "", swGrab.ElapsedMilliseconds, -1);
                return g != null && g.IsSuccess
                    ? $"w={g.Width};h={g.Height};frame={g.FrameNumber}"
                    : "fail:" + (g?.ErrorMessage ?? "grab");
            }
        }

        /// <summary>패턴 매칭(동기) — 그랩 + 알고리즘. ReturnMmCoordinates 면 mm 좌표 반환. chipUid 있으면 이미지로그.</summary>
        public static string Match(IVisionModule m, VisionSettings cfg, string finderId, string chipUid)
        {
            if (m == null) return "fail:no module";
            if (string.IsNullOrEmpty(finderId)) return "fail:no finder";
            if (!m.Finders.TryGetValue(finderId, out var f)) return "fail:finder not found";
            var swGrab = Stopwatch.StartNew();
            using (var g = m.GrabForTool(finderId))
            {
                swGrab.Stop();
                if (g == null || !g.IsSuccess)
                {
                    LogTiming(m.Name, "MATCH", finderId, swGrab.ElapsedMilliseconds, -1);
                    return "fail:" + (g?.ErrorMessage ?? "grab");
                }
                var swAlgo = Stopwatch.StartNew();
                string res = MatchOnImage(m, cfg, finderId, f, g.Image, chipUid);
                swAlgo.Stop();
                LogTiming(m.Name, "MATCH", finderId, swGrab.ElapsedMilliseconds, swAlgo.ElapsedMilliseconds);
                return res;
            }
        }

        /// <summary>이미 그랩된 이미지로 매칭 알고리즘 실행 — 동기 <see cref="Match"/> 와 비동기 MATCHASYNC
        /// (그랩 후 백그라운드 실행)가 공유. 마크/오버레이 저장 + mm 변환 + 이미지로그까지 동일 처리.</summary>
        public static string MatchOnImage(IVisionModule m, VisionSettings cfg, string finderId,
                                          IPatternFinder f, System.Drawing.Bitmap image, string chipUid)
        {
            if (m == null) return "fail:no module";
            if (f == null) return "fail:finder not found";
            if (image == null) return "fail:no image";

            var r = f.Match(image);
            if (r == null || !r.Success) return "fail:" + (r?.ErrorMessage ?? "no match");
            var b = r.Best;
            if (b == null) return "fail:no match";

            // 검출 마크(이미지 좌표) 저장 → 뷰어 메타로 핸들러 오버레이에 표시.
            try { ModuleResultStore.RecordMark(m.Name, finderId, b.CenterX, b.CenterY, b.Score); } catch { }

            double xOut = b.CenterX, yOut = b.CenterY;
            var map = m.ExportCameraMapping();   // 모듈별 스케일/좌표변환(SSOT=모듈 CameraConfig)
            if (map.ReturnMmCoordinates)
            {
                var scale = new VisionScale(map.ScaleX, map.ScaleY);
                var vec   = new CameraVector(map.InvertedX, map.InvertedY, map.IsRotated);
                VisionScale.ConvertPosition(scale, vec, image.Width, image.Height, b.CenterX, b.CenterY, out xOut, out yOut);
            }

            // θ 산출 모드 — Multi 이면 격자 전체 평균각으로 r 대체(Single=최근접 매칭 각도 b.AngleDeg).
            double rOut = b.AngleDeg;
            var node = m.GetAlgorithm(finderId);
            if (node?.Recipe is FinderAlgoRecipe fr && fr.AngleMode == DieAngleMode.Multi)
            {
                if (AlignAngleEstimator.TryEstimate(image, out double avgDeg))
                    rOut = avgDeg;
            }

            if (HasChip(chipUid))
                try { ImageLogSaver.Save(cfg, m.Name, finderId, chipUid, image); } catch { }

            // 오버레이 저장 — 핸들러 뷰어가 '찾은 위치/각/박스 + 검색 ROI' 를 영상 위에 표시(메타로 송출).
            try
            {
                double bw = f.TrainRoi?.Width  ?? 0.0;
                double bh = f.TrainRoi?.Height ?? 0.0;
                var marks = new System.Collections.Generic.List<MatchOverlayStore.Mark>();
                if (r.Instances != null)
                    foreach (var inst in r.Instances)
                        marks.Add(new MatchOverlayStore.Mark
                        {
                            X = inst.CenterX, Y = inst.CenterY,
                            Angle = inst.AngleDeg, Score = inst.Score,
                            BoxW = bw, BoxH = bh
                        });
                double rx = 0, ry = 0, rw = 0, rh = 0;
                var sr = f.SearchRoi;
                if (sr != null && sr.Width > 0 && sr.Height > 0)
                { rw = sr.Width; rh = sr.Height; rx = sr.CenterX - rw / 2.0; ry = sr.CenterY - rh / 2.0; }
                MatchOverlayStore.Record(m.Name, marks.ToArray(), rx, ry, rw, rh);
            }
            catch { }

            return $"OK;x={xOut:F3};y={yOut:F3};r={rOut:F3};score={b.Score:F3}";
        }

        /// <summary>외관/배치 검사. chipUid 있으면 MaterialTracker 누적 + 이미지/데이터 로그.</summary>
        public static string Inspect(IVisionModule m, VisionSettings cfg, string inspId, string chipUid)
        {
            if (m == null) return "fail:no module";
            if (string.IsNullOrEmpty(inspId)) return "fail:no inspector";
            if (!m.Inspectors.TryGetValue(inspId, out var ins)) return "fail:inspector not found";

            // 검사기별 '검사 사용' 게이트 — 레시피 UseInspection=false 면 이 검사를 건너뛴다(PASS 처리).
            if (IsInspectionSkipped(m, inspId))
            {
                ModuleResultStore.Record(m.Name, inspId, true, "inspection=skip");
                return "PASS;inspection=skip";
            }

            var swGrab = Stopwatch.StartNew();
            using (var g = m.GrabForTool(inspId))
            {
                swGrab.Stop();
                if (g == null || !g.IsSuccess)
                {
                    LogTiming(m.Name, "INSPECT", inspId, swGrab.ElapsedMilliseconds, -1);
                    return "fail:" + (g?.ErrorMessage ?? "grab");
                }
                var swAlgo = Stopwatch.StartNew();
                string res = InspectOnImage(m, cfg, inspId, ins, g.Image, chipUid);
                swAlgo.Stop();
                LogTiming(m.Name, "INSPECT", inspId, swGrab.ElapsedMilliseconds, swAlgo.ElapsedMilliseconds);
                return res;
            }
        }

        /// <summary>검사기별 '검사 사용' 게이트 — 레시피 UseInspection=false 면 true(스킵).</summary>
        public static bool IsInspectionSkipped(IVisionModule m, string inspId)
            => m?.GetAlgorithm(inspId)?.Recipe is InspectorAlgoRecipe ir && !ir.UseInspection;

        /// <summary>이미 그랩된 이미지로 검사 실행 — 동기 <see cref="Inspect"/> 와 비동기 INSPECTASYNC
        /// (그랩 후 백그라운드)가 공유. 결과 저장 + (chipUid 있으면) 자재추적/이미지·데이터 로그까지 동일 처리.</summary>
        public static string InspectOnImage(IVisionModule m, VisionSettings cfg, string inspId,
                                            IInspector ins, System.Drawing.Bitmap image, string chipUid)
        {
            if (m == null) return "fail:no module";
            if (ins == null) return "fail:inspector not found";
            if (image == null) return "fail:no image";

            var r = ins.Inspect(image);
            if (r == null || r.Items == null) return "fail:inspect returned null";
            var items = string.Join(",", r.Items.Select(i => $"{i.Name}={i.Value}"));

            // 모듈별 최근 결과 저장 — 작업 모니터링 뷰가 OK/NG + 결과 라인 오버레이로 표시.
            ModuleResultStore.Record(m.Name, inspId, r.IsPass, items);

            // 검출된 모든 결함 → 오버레이 스토어(메타로 송출) → 측면 영상에 결함 박스 전부 표시.
            try
            {
                if (r.Defects != null && r.Defects.Count > 0)
                {
                    var marks = new System.Collections.Generic.List<MatchOverlayStore.Mark>();
                    foreach (var d in r.Defects)
                        marks.Add(new MatchOverlayStore.Mark
                        { X = d.X, Y = d.Y, Angle = 0, Score = d.Area, BoxW = d.Width, BoxH = d.Height });
                    double rx = 0, ry = 0, rw = 0, rh = 0;
                    var sr = ins.InspectionRoi;
                    if (sr != null && sr.Width > 0 && sr.Height > 0)
                    { rw = sr.Width; rh = sr.Height; rx = sr.CenterX - rw / 2.0; ry = sr.CenterY - rh / 2.0; }
                    MatchOverlayStore.Record(m.Name, marks.ToArray(), rx, ry, rw, rh);
                }
            }
            catch { }

            if (HasChip(chipUid))
            {
                try
                {
                    if (inspId.IndexOf("Surface", StringComparison.OrdinalIgnoreCase) >= 0
                        || inspId.IndexOf("Bottom",  StringComparison.OrdinalIgnoreCase) >= 0)
                        MaterialTracker.ApplyBottom(chipUid, r);
                    else if (inspId.IndexOf("Side", StringComparison.OrdinalIgnoreCase) >= 0)
                        MaterialTracker.ApplySide(chipUid, r, cfg?.SideLocation);
                    else if (inspId.IndexOf("Placement", StringComparison.OrdinalIgnoreCase) >= 0
                          || inspId.IndexOf("DieGap",    StringComparison.OrdinalIgnoreCase) >= 0
                          || inspId.IndexOf("Bin",       StringComparison.OrdinalIgnoreCase) >= 0)
                        MaterialTracker.ApplyDieGap(chipUid, r);

                    ImageLogSaver.Save(cfg, m.Name, inspId, chipUid, image, r.IsPass);
                    DataLogSaver.SaveIfDieGapComplete(cfg, chipUid);
                }
                catch { }
            }

            return $"{(r.IsPass ? "PASS" : "FAIL")};{items}";
        }

        /// <summary>패턴 학습.</summary>
        public static string Train(IVisionModule m, string finderId)
        {
            if (m == null) return "fail:no module";
            if (string.IsNullOrEmpty(finderId)) return "fail:no finder";
            if (!m.Finders.TryGetValue(finderId, out var f)) return "fail:finder not found";
            using (var g = m.GrabForTool(finderId))
            {
                if (g == null || !g.IsSuccess) return "fail:" + (g?.ErrorMessage ?? "grab");
                f.Train(g.Image);
                return "OK";
            }
        }

        private static bool HasChip(string chipUid)
            => !string.IsNullOrEmpty(chipUid)
               && !chipUid.Equals("Manual", StringComparison.OrdinalIgnoreCase);

        /// <summary>그랩/알고리즘 소요시간(ms) 로그 — 병목(그랩 vs 연산)을 분리 측정한다.
        /// algoMs &lt; 0 이면 알고리즘 미수행(그랩 실패 등). 로깅 실패가 명령을 막지 않도록 best-effort.</summary>
        private static void LogTiming(string module, string op, string tool, long grabMs, long algoMs)
        {
            if (!TimingLogEnabled) return;
            try
            {
                string algoPart = algoMs < 0 ? "-" : (algoMs + "ms");
                long total = grabMs + (algoMs < 0 ? 0 : algoMs);
                string toolPart = string.IsNullOrEmpty(tool) ? "" : ("/" + tool);
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event, "VISION", "Timing",
                    $"{module}{toolPart} {op}: grab={grabMs}ms algo={algoPart} total={total}ms");
            }
            catch { /* 텔레메트리 로그 실패는 명령 처리에 영향 주지 않음(무시) */ }
        }
    }
}
