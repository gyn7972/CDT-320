using System;
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
        /// <summary>1장 그랩. "w=..;h=..;frame=.." 또는 "fail:..".</summary>
        public static string Grab(IVisionModule m)
        {
            if (m == null) return "fail:no module";
            using (var g = m.Grab())
                return g != null && g.IsSuccess
                    ? $"w={g.Width};h={g.Height};frame={g.FrameNumber}"
                    : "fail:" + (g?.ErrorMessage ?? "grab");
        }

        /// <summary>패턴 매칭. ReturnMmCoordinates 면 mm 좌표 반환. chipUid 있으면 이미지로그.</summary>
        public static string Match(IVisionModule m, VisionSettings cfg, string finderId, string chipUid)
        {
            if (m == null) return "fail:no module";
            if (string.IsNullOrEmpty(finderId)) return "fail:no finder";
            if (!m.Finders.TryGetValue(finderId, out var f)) return "fail:finder not found";
            using (var g = m.GrabForTool(finderId))
            {
                if (g == null || !g.IsSuccess) return "fail:" + (g?.ErrorMessage ?? "grab");
                var r = f.Match(g.Image);
                if (r == null || !r.Success) return "fail:" + (r?.ErrorMessage ?? "no match");
                var b = r.Best;
                if (b == null) return "fail:no match";

                double xOut = b.CenterX, yOut = b.CenterY;
                var map = m.ExportCameraMapping();   // 모듈별 스케일/좌표변환(SSOT=모듈 CameraConfig)
                if (map.ReturnMmCoordinates)
                {
                    var scale = new VisionScale(map.ScaleX, map.ScaleY);
                    var vec   = new CameraVector(map.InvertedX, map.InvertedY, map.IsRotated);
                    VisionScale.ConvertPosition(scale, vec, g.Width, g.Height, b.CenterX, b.CenterY, out xOut, out yOut);
                }

                // θ 산출 모드 — Multi 이면 격자 전체 평균각으로 r 대체(Single=최근접 매칭 각도 b.AngleDeg).
                double rOut = b.AngleDeg;
                var node = m.GetAlgorithm(finderId);
                if (node?.Recipe is FinderAlgoRecipe fr && fr.AngleMode == DieAngleMode.Multi)
                {
                    if (AlignAngleEstimator.TryEstimate(g.Image, out double avgDeg))
                        rOut = avgDeg;
                }

                if (HasChip(chipUid))
                    try { ImageLogSaver.Save(cfg, m.Name, finderId, chipUid, g.Image); } catch { }

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
        }

        /// <summary>외관/배치 검사. chipUid 있으면 MaterialTracker 누적 + 이미지/데이터 로그.</summary>
        public static string Inspect(IVisionModule m, VisionSettings cfg, string inspId, string chipUid)
        {
            if (m == null) return "fail:no module";
            if (string.IsNullOrEmpty(inspId)) return "fail:no inspector";
            if (!m.Inspectors.TryGetValue(inspId, out var ins)) return "fail:inspector not found";

            // 검사기별 '검사 사용' 게이트 — 검사기 레시피(InspectorAlgoRecipe.UseInspection)=false 면 이 검사를 건너뛴다(PASS 처리).
            // 측면 Surface 검사기에서는 '오염검사 사용' 역할. 핸들러(TCP)·셀프 시퀀서가 공유하는 단일 지점이라 양쪽에 동일 적용된다.
            if (m.GetAlgorithm(inspId)?.Recipe is InspectorAlgoRecipe insRecipe && !insRecipe.UseInspection)
            {
                ModuleResultStore.Record(m.Name, inspId, true, "inspection=skip");
                return "PASS;inspection=skip";
            }

            using (var g = m.GrabForTool(inspId))
            {
                if (g == null || !g.IsSuccess) return "fail:" + (g?.ErrorMessage ?? "grab");
                var r = ins.Inspect(g.Image);
                if (r == null || r.Items == null) return "fail:inspect returned null";
                var items = string.Join(",", r.Items.Select(i => $"{i.Name}={i.Value}"));

                // 모듈별 최근 결과 저장 — 작업 모니터링 뷰가 OK/NG + 결과 라인 오버레이로 표시.
                ModuleResultStore.Record(m.Name, inspId, r.IsPass, items);

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

                        ImageLogSaver.Save(cfg, m.Name, inspId, chipUid, g.Image, r.IsPass);
                        DataLogSaver.SaveIfDieGapComplete(cfg, chipUid);
                    }
                    catch { }
                }

                return $"{(r.IsPass ? "PASS" : "FAIL")};{items}";
            }
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
    }
}
