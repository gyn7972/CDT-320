using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        // 내부 grab(오토포커스 FOCUS_VAL 등) 중 ExposureDone(EPD) 푸시 억제 플래그(스레드별).
        // FOCUS_VAL 의 grab 은 ExposureDone 을 동기로 발화하는데, 그게 EPD 푸시로 ACK 응답 스트림에 끼어들면
        // 단순 요청/응답 클라이언트(테스트 SendRecv 등)가 EPD 를 응답으로 오인해 스트림이 어긋난다 → 억제.
        [ThreadStatic] private static bool _suppressExposurePush;
        /// <summary>내부 grab 중 EPD 푸시 억제 여부(스레드별). Comm 링크의 OnExposureDone 이 확인.</summary>
        public static bool SuppressExposurePush
        {
            get { return _suppressExposurePush; }
            set { _suppressExposurePush = value; }
        }

        // INSPECT 결과 태깅용 픽커/인덱스/채널 컨텍스트 — 모듈명 기준 in-process 스토어.
        // ThreadStatic 이 아니라 스토어로 두어 TCP 자체실행(서버가 다른 스레드에서 INSPECT 처리)에서도
        // 시퀀서가 넣은 컨텍스트를 그대로 읽는다(같은 프로세스, 모듈명 키 매칭). 와이어 포맷은 변경하지 않는다.
        private struct InspectCtx { public int Picker; public int Channel; public int IndexX; public int IndexY; }
        private static readonly System.Collections.Generic.Dictionary<string, InspectCtx> _inspectCtx =
            new System.Collections.Generic.Dictionary<string, InspectCtx>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _inspectCtxLock = new object();

        private static InspectCtx GetInspectCtx(string module)
        {
            if (string.IsNullOrEmpty(module)) return new InspectCtx { Channel = -1 };
            lock (_inspectCtxLock)
                return _inspectCtx.TryGetValue(module, out var c) ? c : new InspectCtx { Channel = -1 };
        }

        /// <summary>현재 INSPECT 채널(0~3: Front ch1/ch2, Back ch1/ch2; -1=미지정). 그랩이 채널별 시뮬 이미지 선택에 사용(모듈 기준).</summary>
        public static int CurrentInspectChannel(string module) => GetInspectCtx(module).Channel;

        /// <summary>다음 INSPECT 결과에 붙일 픽커(1~4)·다이 인덱스 설정(모듈 기준). picker=0 이면 이력에만 기록.</summary>
        public static void SetInspectContext(string module, int picker, int indexX, int indexY)
            => SetInspectContext(module, picker, -1, indexX, indexY);

        /// <summary>채널 지정 컨텍스트(Side 4채널: channel 0~3 — Front ch1/2, Back ch1/2, 그 외 -1) — 모듈명 키 저장.</summary>
        public static void SetInspectContext(string module, int picker, int channel, int indexX, int indexY)
        {
            if (string.IsNullOrEmpty(module)) return;
            lock (_inspectCtxLock)
                _inspectCtx[module] = new InspectCtx { Picker = picker, Channel = channel, IndexX = indexX, IndexY = indexY };
        }

        /// <summary>도구 미지정 그랩(GRAB/EXPOSE)에 쓸 기본 조명 도구 — 조명 레벨이 설정된 첫 도구(검사기 우선,
        /// 다음 파인더). 없으면 null(조명 유지, 노출만 모듈 기본). 통신 그랩도 '조명 → 그랩' 순서 보장용(2026-07-11).</summary>
        private static string DefaultLightToolOf(IVisionModule m)
        {
            try
            {
                foreach (var id in m.Inspectors.Keys)
                    if ((m.GetAlgorithm(id)?.Recipe as AlgoRecipeBase)?.LightSettings?.Count > 0) return id;
                foreach (var id in m.Finders.Keys)
                    if ((m.GetAlgorithm(id)?.Recipe as AlgoRecipeBase)?.LightSettings?.Count > 0) return id;
            }
            catch { }
            return null;
        }

        /// <summary>1장 그랩. "w=..;h=..;frame=.." 또는 "fail:..".</summary>
        public static string Grab(IVisionModule m)
        {
            if (m == null) return "fail:no module";
            // 레시피 카메라 노출 적용 후 그랩 — 직전 도구/포커스 그랩이 남긴 노출(예: 스캔용 30µs)이
            // 그대로 쓰이지 않게 한다. 도구 미지정(GRAB/EXPOSE)이라도 조명 설정이 있는 기본 도구의 조명을
            // 먼저 적용하고 그랩한다(컨트롤러 배치 캐시 히트면 통신 생략 = 무비용). 기본 도구 없으면 조명 유지.
            try { m.PrepareToolAcquisition(DefaultLightToolOf(m)); } catch { }
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

            var node = m.GetAlgorithm(finderId);

            // 콜렛 검출 분기 — 일반 콜렛(패턴매치) vs 플랫 콜렛 파인더(std-dev→blob→min-area-rect).
            //   UseFlatCollet=true 면 FlatColletFinder 로, 아니면 기존 finder.Match 로 검출한다.
            MatchResult r;
            if (node?.Recipe is ColletFinderRecipe cr && cr.UseFlatCollet)
            {
                var cc = node.Config as ColletFinderConfig;
                r = FlatColletFinder.Find(image, f.SearchRoi,
                                          cc?.FlatBlockSize ?? 7, cr.FlatStdDevThreshold, cr.FlatMinAreaPx,
                                          cc?.FlatUseCuda ?? false, cc?.FlatFastMode ?? false);
            }
            else
            {
                r = f.Match(image);
            }
            if (r == null || !r.Success)
            {
                // 실패도 작업 모니터링 뷰에 NG 로 표시(최근 결과 갱신).
                try { ModuleResultStore.Record(m.Name, finderId, false, "match=fail"); } catch { }
                return "fail:" + (r?.ErrorMessage ?? "no match");
            }
            var b = r.Best;
            if (b == null)
            {
                try { ModuleResultStore.Record(m.Name, finderId, false, "match=fail"); } catch { }
                return "fail:no match";
            }

            // 검출 마크(이미지 좌표) 저장 → 뷰어 메타로 핸들러 오버레이에 표시.
            try { ModuleResultStore.RecordMark(m.Name, finderId, b.CenterX, b.CenterY, b.Score); } catch { }

            double xOut = b.CenterX, yOut = b.CenterY;
            var map = m.ExportCameraMapping();   // 모듈별 스케일/좌표변환(SSOT=모듈 CameraConfig)
            if (map.ReturnMmCoordinates)
            {
                var scale = new VisionScale(map.ScaleX, map.ScaleY);
                var vec = new CameraVector(map.InvertedX, map.InvertedY, map.IsRotated);
                VisionScale.ConvertPosition(scale, vec, image.Width, image.Height, b.CenterX, b.CenterY, out xOut, out yOut);
            }

            // θ 산출 모드 — Multi 이면 격자 전체 평균각으로 r 대체(Single=최근접 매칭 각도 b.AngleDeg).
            double rOut = b.AngleDeg;
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
                double bw = f.TrainRoi?.Width ?? 0.0;
                double bh = f.TrainRoi?.Height ?? 0.0;
                var marks = new System.Collections.Generic.List<MatchOverlayStore.Mark>();
                if (r.Instances != null)
                    foreach (var inst in r.Instances)
                        marks.Add(new MatchOverlayStore.Mark
                        {
                            X = inst.CenterX,
                            Y = inst.CenterY,
                            Angle = inst.AngleDeg,
                            Score = inst.Score,
                            // 플랫 콜렛 등 검출 사각형 크기가 있으면 그 크기로(콜렛 전용 오버레이), 없으면 Train ROI.
                            BoxW = inst.BoxW > 0 ? inst.BoxW : bw,
                            BoxH = inst.BoxH > 0 ? inst.BoxH : bh
                        });
                double rx = 0, ry = 0, rw = 0, rh = 0;
                var sr = f.SearchRoi;
                if (sr != null && sr.Width > 0 && sr.Height > 0)
                { rw = sr.Width; rh = sr.Height; rx = sr.CenterX - rw / 2.0; ry = sr.CenterY - rh / 2.0; }
                MatchOverlayStore.Record(m.Name, marks.ToArray(), rx, ry, rw, rh);
            }
            catch { }

            // 모듈별 최근 결과 저장 — 작업 모니터링 뷰가 MATCH 결과값(위치/각/점수)도 라인으로 표시.
            try
            {
                ModuleResultStore.Record(m.Name, finderId, true,
                $"x={b.CenterX:F1};y={b.CenterY:F1};r={rOut:F2};score={b.Score:F3}");
            }
            catch { }

            return $"OK;x={b.CenterX:F3};y={b.CenterY:F3};r={rOut:F3};score={b.Score:F3};width={image.Width};height={image.Height}";   // 항상 픽셀 + 이미지크기(px) — 핸들러가 mm 변환
        }

        /// <summary>외관/배치 검사. chipUid 있으면 MaterialTracker 누적 + 이미지/데이터 로그.</summary>
        public static string Inspect(IVisionModule m, VisionSettings cfg, string inspId, string chipUid)
        {
            if (m == null) return "fail:no module";
            if (string.IsNullOrEmpty(inspId)) return "fail:no inspector";
            inspId = AsyncInspectCore.ResolveInspectorId(m, inspId);   // 핸들러 공용 id → 등록 id (측면=칩핑 검사기)
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

        /// <summary>이미 그랩된 이미지로 검사 실행 — 동기 <see cref="Inspect"/> 와 비동기 INSPECTASYNC 가 공유.
        /// 픽커/채널/인덱스는 모듈 공유 컨텍스트(<see cref="GetInspectCtx"/>)에서 읽는다(단일 경로).</summary>
        public static string InspectOnImage(IVisionModule m, VisionSettings cfg, string inspId,
                                            IInspector ins, System.Drawing.Bitmap image, string chipUid)
        {
            var c = GetInspectCtx(m?.Name);
            return InspectOnImageExplicit(m, cfg, inspId, ins, image, chipUid, c.Picker, c.Channel, c.IndexX, c.IndexY);
        }

        /// <summary>이미 그랩된 이미지로 검사 실행 — 픽커/채널/인덱스 컨텍스트를 '인자로' 받는 버전(병렬 배치용).
        /// 병렬 처리 시 픽커별로 컨텍스트가 달라야 하므로 모듈 공유 컨텍스트 대신 인자를 사용한다(각 픽커는 독립 인스펙터 인스턴스).
        /// 결과 저장 + (chipUid 있으면) 자재추적/이미지·데이터 로그까지 동일 처리.</summary>
        public static string InspectOnImageExplicit(IVisionModule m, VisionSettings cfg, string inspId,
                                            IInspector ins, System.Drawing.Bitmap image, string chipUid,
                                            int ctxPicker, int ctxChannel, int ctxIndexX, int ctxIndexY)
        {
            if (m == null) return "fail:no module";
            if (ins == null) return "fail:inspector not found";
            if (image == null) return "fail:no image";

            // 카메라 ScaleX/Y(mm/px)를 검사기에 주입 — 검사기는 항상 mm 계산·판정. 모드별 스케일 기록(표시 환산용).
            try
            {
                UnitContext.ApplyScale(ins, m.ScaleX, m.ScaleY);
                string um = InspectionResultStore.ModeOf(inspId) ?? InspectionResultStore.ModeOf(m.Name);
                if (um != null) UnitContext.SetModeScale(um, m.ScaleX, m.ScaleY);
            }
            catch { }

            // Bottom 외곽 종료(EventSearchDieEnd) XYT 푸시용 스레드 컨텍스트 —
            // CDTInspector.SearchDieEnd 가 이 스레드에서 동기 발화되므로 여기서 식별을 주입한다.
            BottomXytPushService.SetContext(m.Name, ctxPicker, ctxIndexX, ctxIndexY, chipUid);
            InspectionResult r;
            try { r = ins.Inspect(image); }
            finally { BottomXytPushService.ClearContext(); }
            if (r == null || r.Items == null) return "fail:inspect returned null";
            var items = string.Join(";", r.Items.Select(i => $"{i.Name}={i.Value}"));   // 핸들러 프로토콜: ; 구분

            // 모듈별 최근 결과 저장 — 작업 모니터링 뷰가 OK/NG + 결과 라인 오버레이로 표시.
            ModuleResultStore.Record(m.Name, inspId, r.IsPass, items);

            // 구조화 결과 스토어 — 작업화면 뷰어(Picker 이미지/추세/그리드)가 구독. 모드/픽커 태그.
            // 검사 종류별 오버레이 기하 — 모니터링 뷰와 팝업이 동일 렌더러(InspectionOverlayRenderer)로 그리도록 한 번만 생성.
            InspectionOverlayStore.Geom geom = null;
            try
            {
                if (ins is SideAppearanceInspector siG && siG.IsChippingRole && siG.LastValid)
                    geom = new InspectionOverlayStore.Geom { Kind = InspectionOverlayStore.OverlayKind.Side, TopProfile = siG.LastTopProfile, BotProfile = siG.LastBotProfile, RefCorners = siG.LastCorners, Defects = r.Defects, Pass = siG.LastPass };
                else if (ins is BottomInspector biG && biG.LastValid)
                    geom = new InspectionOverlayStore.Geom { Kind = InspectionOverlayStore.OverlayKind.Bottom, Corners = biG.LastCorners, Defects = r.Defects, Caption = BottomCaption(r), Pass = r.IsPass };
                else if (ins is PlacementGapInspector pgG && pgG.LastValid)
                    geom = new InspectionOverlayStore.Geom { Kind = InspectionOverlayStore.OverlayKind.Bin, Corners = pgG.LastCorners, Defects = r.Defects, Pass = r.IsPass };
            }
            catch { }

            try
            {
                string mode = InspectionResultStore.ModeOf(inspId) ?? InspectionResultStore.ModeOf(m.Name);
                if (mode == null)
                {
                    // 진단(MapTrace): 모드 미해석 → 스토어 기록 자체가 스킵되어 맵/차트에 안 나온다.
                    try
                    {
                        QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "MapTrace",
                            "Record 스킵 — mode 해석 실패(inspId=" + inspId + ", module=" + m.Name + ")");
                    }
                    catch { }
                }
                if (mode != null)
                {
                    // 검출 박스(코너) → 픽커 패널 오버레이용으로 함께 전달.
                    System.Drawing.PointF[] box = (ins as PlacementGapInspector)?.LastCorners
                                              ?? (ins as BottomInspector)?.LastCorners
                                              ?? (ins as SideAppearanceInspector)?.LastCorners;
                    var ctx = new InspectCtx { Picker = ctxPicker, Channel = ctxChannel, IndexX = ctxIndexX, IndexY = ctxIndexY };
                    var storeItem = InspectionResultStore.FromResult(mode, ctx.Picker, ctx.Channel, ctx.IndexX, ctx.IndexY, r, image, box, geom, m.Name);
                    InspectionResultStore.Record(storeItem);
                    // 진단(MapTrace): 기록 좌표/키 — Bottom 맵은 Width+Height 둘 다 있어야 셀이 생긴다.
                    try
                    {
                        QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "MapTrace",
                            "Record — mode=" + mode + ", insp=" + inspId + ", picker=" + ctx.Picker + ", ch=" + ctx.Channel +
                            ", ix=" + ctx.IndexX + ", iy=" + ctx.IndexY +
                            ", W/H=" + (storeItem.Values.ContainsKey("Width") && storeItem.Values.ContainsKey("Height") ? "O" : "X(맵 셀 미생성)") +
                            ", pass=" + storeItem.Pass);
                    }
                    catch { }
                    // 레시피 웨이퍼 사양의 마지막 다이 도달 시 날짜별 스냅샷 저장 — 실시간 데이터를 자체 누적(뷰어 이력 상한과 무관).
                    WaferDataSaver.Accumulate(mode, ctx.Picker, ctx.Channel, ctx.IndexX, ctx.IndexY, storeItem.Pass, storeItem.Values);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[VisionCommandCore] InspectionResultStore.Record 실패: " + ex.Message); }

            // 검사 ROI(노랑)만 매치 오버레이 스토어로 — 결함은 종류별 전용 렌더러(InspectionOverlayRenderer)가
            // Geom.Defects 로 그린다(중복 방지). 결함 마크는 더 이상 여기로 보내지 않음.
            try
            {
                double rx = 0, ry = 0, rw = 0, rh = 0;
                var sr = ins.InspectionRoi;
                if (sr != null && sr.Width > 0 && sr.Height > 0)
                { rw = sr.Width; rh = sr.Height; rx = sr.CenterX - rw / 2.0; ry = sr.CenterY - rh / 2.0; }
                MatchOverlayStore.Record(m.Name, new MatchOverlayStore.Mark[0], rx, ry, rw, rh);
            }
            catch { }

            // 검사 종류별 전용 오버레이 기하 → 모니터링 뷰/팝업이 동일 렌더러로 표시(위에서 만든 geom 재사용).
            try
            {
                if (geom != null) InspectionOverlayStore.Record(m.Name, geom);
                else InspectionOverlayStore.Clear(m.Name);
            }
            catch { }

            // (차트 상/하한은 NG 판정 스펙과 분리된 '차트 전용 Limit'(InspectorAlgoRecipe.Chart*Limit)을 사용하며,
            //  레시피 적용 시 AlgorithmNode 에서 ChartLimitStore 로 송출된다. 여기서는 판정 스펙을 차트로 보내지 않는다.)

            if (HasChip(chipUid))
            {
                try
                {
                    if (inspId.IndexOf("Surface", StringComparison.OrdinalIgnoreCase) >= 0
                        || inspId.IndexOf("Bottom", StringComparison.OrdinalIgnoreCase) >= 0)
                        MaterialTracker.ApplyBottom(chipUid, r);
                    else if (inspId.IndexOf("Side", StringComparison.OrdinalIgnoreCase) >= 0)
                        MaterialTracker.ApplySide(chipUid, r, cfg?.SideLocation);
                    else if (inspId.IndexOf("Placement", StringComparison.OrdinalIgnoreCase) >= 0
                          || inspId.IndexOf("DieGap", StringComparison.OrdinalIgnoreCase) >= 0
                          || inspId.IndexOf("Bin", StringComparison.OrdinalIgnoreCase) >= 0)
                        MaterialTracker.ApplyDieGap(chipUid, r);

                    ImageLogSaver.Save(cfg, m.Name, inspId, chipUid, image, r.IsPass);
                    DataLogSaver.SaveIfDieGapComplete(cfg, chipUid);
                }
                catch { }
            }

            // INSPECT 응답을 '핸들러가 실제 소비하는 필드'로 검사기 타입별 분기:
            //  • Bin 배치검사(PlacementInspector=PlacementGapInspector) → PASS/FAIL + 픽셀 offset(x/y) + 이미지크기(px).
            //      핸들러(CheckPlacement→InspectCalibrated)가 PixelToMm 로 배치보정에 사용.
            //  • Bottom 표면 검사(SurfaceInspector=BottomInspector) → PASS/FAIL + 원본 검사 항목.
            //      장비쪽 SideVisionY/PickerZ 보정은 로그 검증 전까지 연결하지 않고, 원본값만 내려보낸다.
            //  • Side 표면/칩핑 검사(SideAppearanceInspector) → PASS/FAIL + 원본 검사 항목.
            string verdict = r.IsPass ? "PASS" : "FAIL";
            if (ins is PlacementGapInspector)
            {
                var mpOff = m.ExportCameraMapping();
                return verdict + BuildPlacementInspectionPayload(r, image.Width, image.Height, mpOff.ScaleX, mpOff.ScaleY);
            }
            if (ins is BottomInspector)
                return verdict + BuildBottomInspectionPayload(r);
            if (ins is SideAppearanceInspector)
                return verdict + BuildSideInspectionPayload(r);

            return verdict;
        }

        /// <summary>
        /// Bottom SurfaceInspector 원본 결과를 통신 payload 로 노출한다.
        /// SideVisionY/PickerZ 보정 연결은 실장비 로그 확인 후 CDT 쪽 매핑에서 결정한다.
        /// </summary>
        private static string BuildBottomInspectionPayload(InspectionResult r)
        {
            if (r == null || r.Items == null || r.Items.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            AppendInspectionItem(sb, r, "Width", "bottom_width_mm");
            AppendInspectionItem(sb, r, "Height", "bottom_height_mm");
            AppendInspectionItem(sb, r, "Angle", "bottom_angle_deg");
            AppendInspectionItem(sb, r, "Offset X", "bottom_offset_x_mm");
            AppendInspectionItem(sb, r, "Offset Y", "bottom_offset_y_mm");
            AppendInspectionItems(sb, r, "bottom_item_");

            return sb.ToString();
        }

        private static string BuildSideInspectionPayload(InspectionResult r)
        {
            if (r == null || r.Items == null || r.Items.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            AppendInspectionItems(sb, r, "side_item_");
            return sb.ToString();
        }

        private static string BuildPlacementInspectionPayload(InspectionResult r, int imageWidth, int imageHeight, double scaleX, double scaleY)
        {
            var sb = new StringBuilder();
            double oxPx = r != null && r.Items != null ? ItemPx(r.Items, "Offset X", scaleX) : 0.0;
            double oyPx = r != null && r.Items != null ? ItemPx(r.Items, "Offset Y", scaleY) : 0.0;
            AppendKeyValue(sb, "x", oxPx.ToString("F3"));
            AppendKeyValue(sb, "y", oyPx.ToString("F3"));
            AppendKeyValue(sb, "width", imageWidth.ToString());
            AppendKeyValue(sb, "height", imageHeight.ToString());

            if (r == null || r.Items == null || r.Items.Count == 0)
                return sb.ToString();

            AppendInspectionItem(sb, r, "Offset X", "placement_offset_x_mm");
            AppendInspectionItem(sb, r, "Offset Y", "placement_offset_y_mm");
            AppendInspectionItem(sb, r, "Angle", "placement_angle_deg");
            AppendInspectionItems(sb, r, "placement_item_");
            return sb.ToString();
        }

        private static void AppendInspectionItems(StringBuilder sb, InspectionResult r, string prefix)
        {
            if (sb == null || r == null || r.Items == null)
                return;

            for (int i = 0; i < r.Items.Count; i++)
            {
                var item = r.Items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.Name))
                    continue;

                string key = (prefix ?? "item_") + NormalizePayloadKey(item.Name);
                AppendKeyValue(sb, key, item.Value);
                AppendKeyValue(sb, key + "_pass", item.IsPass ? "1" : "0");
            }
        }

        private static void AppendInspectionItem(StringBuilder sb, InspectionResult r, string itemName, string payloadKey)
        {
            if (sb == null || r == null || r.Items == null)
                return;

            var item = r.Items.FirstOrDefault(i => i != null && string.Equals(i.Name, itemName, StringComparison.OrdinalIgnoreCase));
            if (item == null)
                return;

            AppendKeyValue(sb, payloadKey, item.Value);
        }

        private static void AppendKeyValue(StringBuilder sb, string key, string value)
        {
            if (sb == null || string.IsNullOrWhiteSpace(key))
                return;

            sb.Append(';');
            sb.Append(key);
            sb.Append('=');
            sb.Append(SanitizePayloadValue(value));
        }

        private static string NormalizePayloadKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                    sb.Append(char.ToLowerInvariant(c));
                else
                    sb.Append('_');
            }

            while (sb.ToString().Contains("__"))
                sb.Replace("__", "_");

            return sb.ToString().Trim('_');
        }

        private static string SanitizePayloadValue(string value)
        {
            if (value == null)
                return string.Empty;

            return value.Replace(";", ",").Replace("|", "/").Trim();
        }

        /// <summary>인스펙터 설정(레시피 파라미터) 복제 — 병렬 배치용 신규 인스턴스에 모듈의 설정된 인스펙터 값을 반사 복사.
        /// 공개 read/write 프로퍼티만 복사한다(계산/출력 프로퍼티는 대개 private set 이라 자동 제외).
        /// InspectionRoi 는 참조 복사(검사 중 읽기전용이라 안전). CDT-310 코어 로직은 건드리지 않는다.</summary>
        public static void CopyInspectorConfig(IInspector src, IInspector dst)
        {
            if (src == null || dst == null || src.GetType() != dst.GetType()) return;
            foreach (var p in src.GetType().GetProperties(
                         System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (!p.CanRead || !p.CanWrite) continue;
                try { p.SetValue(dst, p.GetValue(src)); } catch { }
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
                // 학습 완료도 작업 모니터링 뷰 결과 라인으로 표시(통신/수동 공용 스토어).
                try { ModuleResultStore.Record(m.Name, finderId, true, "train=OK"); } catch { }
                return "OK";
            }
        }

        /// <summary>검사 items 에서 지정 항목(mm)을 픽셀로 역변환(÷scale). 항목 없으면 0. scale&lt;=0(미보정)이면 값 그대로(이미 px).</summary>
        private static double ItemPx(System.Collections.Generic.IEnumerable<InspectionItem> items, string name, double scale)
        {
            if (items == null) return 0.0;
            foreach (var it in items)
                if (it != null && string.Equals(it.Name, name, StringComparison.OrdinalIgnoreCase)
                    && double.TryParse(it.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v))
                    return v / (scale > 0 ? scale : 1.0);
            return 0.0;
        }

        private static bool HasChip(string chipUid)
            => !string.IsNullOrEmpty(chipUid)
               && !chipUid.Equals("Manual", StringComparison.OrdinalIgnoreCase);

        /// <summary>바텀 오버레이 사이즈 라벨("W .. H .. θ..") — 결과 항목에서 추출.</summary>
        private static string BottomCaption(InspectionResult r)
        {
            string Get(string n)
            {
                if (r?.Items != null) foreach (var it in r.Items) if (it.Name == n) return it.Value;
                return null;
            }
            string w = Get("Width"), h = Get("Height"), a = Get("Angle");
            if (w == null && h == null) return null;
            string s = "W " + (w ?? "?") + " H " + (h ?? "?");
            if (a != null) s += " θ" + a;
            return s;
        }

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

        // ── 오토포커스(FOCUS_START / FOCUS_VAL) 공유 처리 ──────────────────
        // 카메라(Bottom/Front/Back)는 핸들러가 명시(모듈명 SSOT=핸들러). Vision 은 프레임당 Score 만 계산.
        // 모터 Z 는 핸들러가 보내며 그래프 X 축, Score 는 그래프 Y 축이 된다.

        /// <summary>오토포커스 세션 시작(리셋). "FOCUS_START &lt;camera&gt; &lt;target&gt;".
        /// 이후 각 시리즈 첫 샘플이 자동으로 최초값(점)이 된다.
        /// <para>포커스 전용 노출(AutoFocusRoiStore.ExposureUs)이 지정되어 있으면 스캔 동안 쓰도록
        /// 카메라에 적용한다(스캔 종료 FOCUS_BEST 에서 레시피 설정으로 복원).</para></summary>
        public static string FocusStart(IVisionModule m, string[] parts)
        {
            if (parts == null || parts.Length < 4) return "fail:need camera target";
            if (!AutoFocusStore.TryParseCamera(parts[2], out var cam)) return "fail:bad camera";
            if (!AutoFocusStore.TryParseTarget(parts[3], out var tgt)) return "fail:bad target";
            WaitLastFocusGrab(m != null ? m.Name : null, 5000);   // 이전 스캔의 마지막 grab 전송/투입 완료 대기
            AutoFocusProcessor.WaitForDrain(2000);   // 이전 스캔 잔여 백그라운드 처리 정리 후 리셋
            AutoFocusStore.Start(cam, tgt);
            AutoFocusTactLog.MarkCycleStart(cam + "/" + tgt);
            PrepareFocusAcquisition(m, cam, tgt);
            return $"OK;camera={cam};target={tgt}";
        }

        /// <summary>포커스 촬상 준비 — FocusFinder 도구의 조명 + 노출 적용(컨트롤러 캐시 히트면 통신 생략) 후,
        /// 포커스 전용 노출(AutoFocusRoiStore.ExposureUs)이 지정돼 있으면 그 값으로 덮어쓴다.
        /// FOCUS_START(핸들러 스캔)와 포커스 페이지 [포커스 측정]이 공용으로 사용한다.</summary>
        public static void PrepareFocusAcquisition(IVisionModule m, FocusCamera cam, FocusTarget tgt)
        {
            try { m?.PrepareToolAcquisition("FocusFinder"); } catch { }
            ApplyFocusExposure(m, cam, tgt);
        }

        /// <summary>포커스 전용 노출 적용 — 지정(&gt;0)된 경우에만 카메라에 쓴다. 실패해도 스캔은 계속.</summary>
        private static void ApplyFocusExposure(IVisionModule m, FocusCamera cam, FocusTarget tgt)
        {
            try
            {
                double us = AutoFocusRoiStore.GetExposureUs(cam, tgt);
                if (us <= 0 || m?.Camera == null) return;
                m.Camera.ExposureUs = us;
                QMC.Vision.Comm.VisionCommLog.Add(
                    "[FOCUS] 스캔용 노출 적용 " + us.ToString("F0") + "µs (" + cam + "/" + tgt + ")");
            }
            catch (Exception ex)
            {
                QMC.Vision.Comm.VisionCommLog.Add("[FOCUS] 스캔용 노출 적용 실패: " + ex.Message);
            }
        }

        /// <summary>포커스 전용 노출을 썼던 스캔 종료 후 레시피 카메라 설정 복원. 실패해도 응답은 유지.</summary>
        private static void RestoreRecipeExposure(IVisionModule m, FocusCamera cam, FocusTarget tgt)
        {
            try
            {
                if (AutoFocusRoiStore.GetExposureUs(cam, tgt) <= 0 || m == null) return;
                m.ApplyCameraSettings();   // 레시피 노출/게인/프레임레이트 재적용(값은 카메라 유효 범위로 클램프됨)
                QMC.Vision.Comm.VisionCommLog.Add("[FOCUS] 스캔 종료 — 레시피 카메라 설정 복원 (" + cam + "/" + tgt + ")");
            }
            catch (Exception ex)
            {
                QMC.Vision.Comm.VisionCommLog.Add("[FOCUS] 레시피 카메라 설정 복원 실패: " + ex.Message);
            }
        }

        /// <summary>
        /// 한 위치의 포커스 Score 측정 + 세션 누적.
        /// "FOCUS_VAL &lt;motorZ&gt; &lt;camera&gt; &lt;target&gt; [pickupNo] [init]".
        /// <para>init(=1/INIT/TRUE) 이면 이 샘플을 최초값(점)으로 표시. 측면은 pickupNo=0.</para>
        /// 인자가 부족하면 구 4-ROI 측정(<see cref="IVisionModule.MeasureFocus"/>)으로 하위호환.
        /// </summary>
        public static string FocusValue(IVisionModule m, string[] parts)
        {
            if (m == null) return "fail:no module";

            // 하위호환: 인자 없는 FOCUS_VAL → 구 4-ROI 측정.
            if (parts == null || parts.Length < 5)
            {
                if (!m.MeasureFocus(out var rois, out var err))
                    return "fail:" + err;
                return "OK;" + string.Join(";", rois.Select(p => $"{p.Key}={p.Value:F2}"));
            }

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (!double.TryParse(parts[2], System.Globalization.NumberStyles.Float, inv, out double motorZ))
                return "fail:bad motorZ";
            if (!AutoFocusStore.TryParseCamera(parts[3], out var cam)) return "fail:bad camera";
            if (!AutoFocusStore.TryParseTarget(parts[4], out var tgt)) return "fail:bad target";
            int pickup = 0;
            if (parts.Length > 5) int.TryParse(parts[5], out pickup);
            bool isInitial = parts.Length > 6 && IsInitFlag(parts[6]);

            // ── 노출 종료 즉시 응답 ──
            // 그랩(노출→전송→버퍼 카피)을 워커로 분리하고, 카메라 ExposureEnd 이벤트가 오는 '즉시' ACK 를
            // 회신한다(EPD 푸시와 거의 동시). 영상 전송/카피/채점을 기다리지 않으므로 핸들러는 노출이
            // 끝나자마자 다음 Z 로 이동할 수 있다. 채점은 백그라운드 큐(AutoFocusProcessor), 결과는 FOCUS_BEST 회수.
            //
            // 직전 샘플의 전송/카피가 아직 진행 중이면 완료를 기다린다(카메라 단발 그랩 직렬화 —
            // 핸들러 이동+정착 시간이면 보통 끝나 있어 실질 대기 0).
            //WaitLastFocusGrab(m.Name, 5000);
            WaitLastFocusGrab(m.Name, 0);

            // 그랩 직전 FocusFinder 조명 보장(2026-07-13) — 종전엔 FOCUS_START 에서만 조명을 켜고
            // FOCUS_VAL 은 조명 없이 바로 grab 했다. FOCUS_START 누락/타 모듈의 조명 변경 시 잘못된
            // 조명으로 촬상될 수 있어, 각 FOCUS_VAL 이 스스로 조명을 보장한다(노출은 FOCUS_START 설정 유지).
            // 캐시 히트면 통신/안정화 대기 생략 → 스캔 중 반복 호출해도 무비용.
            try { m.EnsureToolLights("FocusFinder"); } catch { }

            var expEvt = new System.Threading.ManualResetEventSlim(false);
            Action<string> onExp = _n => { try { expEvt.Set(); } catch { } };
            m.ExposureDone += onExp;

            var swGrab = Stopwatch.StartNew();
            var grabTask = System.Threading.Tasks.Task.Run(() =>
            {
                try { return m.Grab(); }
                catch (Exception ex) { return GrabResult.Fail("grab 예외: " + ex.Message, m.Name); }
            });

            // 노출 종료 또는 그랩 종료(실패 포함) 중 먼저 오는 쪽까지만 대기.
            // (HW 노출 이벤트 미지원 카메라는 모듈 폴백이 그랩 완료 시점에 발화 → 기존 타이밍과 동일)
            try
            {
                System.Threading.WaitHandle.WaitAny(new System.Threading.WaitHandle[]
                    { expEvt.WaitHandle, ((IAsyncResult)grabTask).AsyncWaitHandle }, 5000);
            }
            finally { m.ExposureDone -= onExp; }
            long expMs = swGrab.ElapsedMilliseconds;

            // 그랩이 노출 전에 실패로 끝났으면 실패 응답(정상이면 아직 전송 중이라 미완료).
            if (grabTask.IsCompleted && (grabTask.Result == null || !grabTask.Result.IsSuccess))
            {
                string err = grabTask.Result != null ? grabTask.Result.ErrorMessage : "grab";
                try { grabTask.Result?.Dispose(); } catch { }
                return "fail:" + err;
            }

            // 백그라운드: 그랩 완료 시 ROI 잘라 채점 큐 투입 — 응답과 완전히 분리.
            Roi[] afRois = AutoFocusRoiStore.GetRois(cam, tgt);
            string modName = m.Name;
            double mz = motorZ;
            bool init0 = isInitial;
            int pickup0 = pickup;
            var scoreTask = grabTask.ContinueWith(t =>
            {
                GrabResult g = t.Status == System.Threading.Tasks.TaskStatus.RanToCompletion ? t.Result : null;
                try
                {
                    if (g == null || !g.IsSuccess || g.Image == null) { return; }

                    int imgW = g.Image.Width, imgH = g.Image.Height;
                    var rects = new System.Collections.Generic.List<System.Drawing.Rectangle>();
                    var series = new System.Collections.Generic.List<int>();
                    for (int i = 0; i < afRois.Length; i++)
                    {
                        Roi roi = afRois[i];
                        if (roi == null || roi.Width <= 0 || roi.Height <= 0) continue;
                        rects.Add(roi.BoundingBox);
                        series.Add(i + 1);
                    }

                    if (rects.Count > 0)
                    {
                        // 원본 g 소유권을 채점 큐로 이전(거기서 crop/채점/라이브 스코어 기록 후 Dispose).
                        AutoFocusProcessor.Enqueue(modName, cam, tgt, mz, init0, expMs,
                            g, rects.ToArray(), series.ToArray(), imgW, imgH);
                        g = null;
                    }
                    else
                    {
                        // ROI 미설정 → 전체 프레임 채점(드문 폴백). 측면(pickup=0)은 시리즈 1로.
                        double score = AutoFocusCore.Score(g.Image);
                        int series0 = pickup0 >= 1 ? pickup0 : 1;
                        AutoFocusStore.AddSample(cam, tgt, series0, mz, score, init0);
                        try
                        {
                            ModuleResultStore.Record(modName, "FOCUS", true,
                            "z=" + mz.ToString("F3", inv) + ";avgScore=" + score.ToString("F1", inv));
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { QMC.Vision.Comm.VisionCommLog.Add("[FOCUS] 백그라운드 채점 투입 실패: " + ex.Message); } catch { }
                }
                finally { try { g?.Dispose(); } catch { } }
            }, System.Threading.Tasks.TaskScheduler.Default);
            RegisterLastFocusGrab(m.Name, scoreTask);

            LogTiming(m.Name, "FOCUS_VAL", tgt.ToString(), expMs, 0);   // grab 칸 = 노출 종료까지(응답 시점)
            return $"OK;z={motorZ.ToString("F4", inv)};pickup={pickup};init={(isInitial ? 1 : 0)};queued=1";
        }

        // ── 모듈별 '마지막 FOCUS_VAL 그랩(전송+채점 투입)' 추적 — 노출종료 즉시 ACK 구조에서
        //    카메라 단발 그랩과 FOCUS_BEST 회수가 진행 중인 전송/투입과 겹치지 않게 직렬화한다. ──
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Threading.Tasks.Task> _lastFocusGrab
            = new System.Collections.Concurrent.ConcurrentDictionary<string, System.Threading.Tasks.Task>(StringComparer.OrdinalIgnoreCase);

        private static void RegisterLastFocusGrab(string module, System.Threading.Tasks.Task t)
        {
            if (!string.IsNullOrEmpty(module) && t != null) _lastFocusGrab[module] = t;
        }

        private static void WaitLastFocusGrab(string module, int timeoutMs)
        {
            try
            {
                System.Threading.Tasks.Task t;
                if (!string.IsNullOrEmpty(module) && _lastFocusGrab.TryGetValue(module, out t))
                    t.Wait(timeoutMs);
            }
            catch { /* 이전 그랩 실패는 해당 샘플 누락으로 이미 처리 — 다음 샘플 진행 */ }
        }

        /// <summary>콜렛 회전 중심(COC) — "MODULE|COC|START" = 누적 라이브 시작,
        /// "MODULE|COC|END" = 라이브 정지 + 누적 평균 영상의 대칭 중심(x,y) 계산·응답.
        /// 실제 누적/계산은 <see cref="ColletRotationCenterCore"/> 위임.</summary>
        public static string ColletRotationCenter(IVisionModule m, string[] parts)
        {
            if (m == null) return "fail:no module";
            string sub = parts != null && parts.Length > 2 ? parts[2].Trim().ToUpperInvariant() : "";
            switch (sub)
            {
                case "START": return ColletRotationCenterCore.Start(m);
                case "END":
                case "STOP":  return ColletRotationCenterCore.End(m);
                default:      return "fail:need START|END";
            }
        }

        /// <summary>init 인자 해석 — "1"/"INIT"/"TRUE"(대소문자 무시) 면 최초값.</summary>
        private static bool IsInitFlag(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            string v = s.Trim().ToUpperInvariant();
            return v == "1" || v == "INIT" || v == "TRUE";
        }

        /// <summary>
        /// 세션의 BEST 결과 조회(핸들러가 TCP 로 결과 회수).
        /// "FOCUS_BEST &lt;camera&gt; &lt;target&gt; [pickupNo]".
        /// 응답: "OK;bestZ=&lt;평균&gt;;bestScore=&lt;평균&gt;;roiN=&lt;개수&gt;;p1z=...;p1s=...;p1n=...;p2z=...".
        /// <para>bestZ/bestScore = 측정된 ROI(샘플 있는 것) Best 위치/점수의 <b>평균</b> — 핸들러 피드백 대표값.
        /// pickupNo 1~4 지정 시 그 ROI 만(콜렛 픽커별 스캔), 0/미지정 = 전체 ROI 평균(바텀 4-ROI 포커스).</para>
        /// </summary>
        public static string FocusBest(IVisionModule m, string[] parts)
        {
            if (parts == null || parts.Length < 4) return "fail:need camera target";
            if (!AutoFocusStore.TryParseCamera(parts[2], out var cam)) return "fail:bad camera";
            if (!AutoFocusStore.TryParseTarget(parts[3], out var tgt)) return "fail:bad target";

            // pickupNo(parts[4]) 인자는 하위호환으로 받기만 하고 무시한다(2026-07-08) —
            // 베스트 Z 는 항상 'ROI1~4(샘플 있는 것) Best Z 의 평균'으로 리턴한다.
            // (특정 ROI 하나만 회수하던 동작 제거 — 콜렛/다이 포커스 대표값 = 4-ROI 평균.)

            // FOCUS_VAL 들이 백그라운드로 채점 중이므로, 누적이 모두 끝난 뒤(=처리 완료) best 를 회수한다.
            // 완료될 때까지 충분히 대기해야 불완전한 best 로 응답하지 않는다(처리 완료 후 ACK).
            WaitLastFocusGrab(parts.Length > 0 && m != null ? m.Name : null, 10000);   // 마지막 grab 전송/투입 완료까지
            AutoFocusProcessor.WaitForDrain(120000);

            var sess = AutoFocusStore.Get(cam, tgt);
            if (sess == null) return "fail:no session";

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var rows = sess.BuildBestTable();

            // 대표 포커스 = ROI1~4(샘플 있는 것) Best 위치의 평균 — 핸들러 피드백 값.
            // 핸들러 파서(VisionFocusBestResult.Parse)가 bestZ/bestScore 키를 최우선으로 읽는다.
            double zSum = 0, sSum = 0; int used = 0;
            foreach (var row in rows)
            {
                if (row.SampleCount <= 0) continue;
                zSum += row.BestMotorZ; sSum += row.BestScore; used++;
            }

            var sb = new System.Text.StringBuilder("OK");
            if (used > 0)
            {
                sb.Append(";bestZ=" + (zSum / used).ToString("F4", inv));
                sb.Append(";bestScore=" + (sSum / used).ToString("F2", inv));
                sb.Append(";roiN=" + used);
                // 작업 모니터링 뷰에도 포커스 결과 표시 — 수동 [포커스 측정]과 동일 키("FOCUS").
                try
                {
                    if (m != null)
                        ModuleResultStore.Record(m.Name, "FOCUS", true,
                            "bestZ=" + (zSum / used).ToString("F4", inv)
                            + ";bestScore=" + (sSum / used).ToString("F2", inv) + ";roiN=" + used);
                }
                catch { }
            }
            foreach (var row in rows)   // ROI 별 상세(진단/그래프용) — 기존 필드 유지, 항상 전체 ROI 출력
            {
                int p = row.PickupNo;
                sb.Append(";p" + p + "z=" + row.BestMotorZ.ToString("F4", inv));
                sb.Append(";p" + p + "s=" + row.BestScore.ToString("F2", inv));
                sb.Append(";p" + p + "n=" + row.SampleCount);
            }
            AutoFocusTactLog.MarkCycleEnd(cam + "/" + tgt);
            RestoreRecipeExposure(m, cam, tgt);   // 스캔용 노출을 썼으면 레시피 설정으로 원복
            return sb.ToString();
        }
    }
}
