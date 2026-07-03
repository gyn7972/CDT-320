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

            return $"OK;x={b.CenterX:F3};y={b.CenterY:F3};r={rOut:F3};score={b.Score:F3};width={image.Width};height={image.Height}";   // 항상 픽셀 + 이미지크기(px) — 핸들러가 mm 변환
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

            var r = ins.Inspect(image);
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
                if (mode != null)
                {
                    // 검출 박스(코너) → 픽커 패널 오버레이용으로 함께 전달.
                    System.Drawing.PointF[] box = (ins as PlacementGapInspector)?.LastCorners
                                              ?? (ins as BottomInspector)?.LastCorners
                                              ?? (ins as SideAppearanceInspector)?.LastCorners;
                    var ctx = new InspectCtx { Picker = ctxPicker, Channel = ctxChannel, IndexX = ctxIndexX, IndexY = ctxIndexY };
                    var storeItem = InspectionResultStore.FromResult(mode, ctx.Picker, ctx.Channel, ctx.IndexX, ctx.IndexY, r, image, box, geom);
                    InspectionResultStore.Record(storeItem);
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

            // INSPECT 응답을 '핸들러가 실제 소비하는 필드'로 검사기 타입별 분기:
            //  • Bin 배치검사(PlacementInspector=PlacementGapInspector) → PASS/FAIL + 픽셀 offset(x/y) + 이미지크기(px).
            //      핸들러(CheckPlacement→InspectCalibrated)가 PixelToMm 로 배치보정에 사용.
            //  • 표면/칩핑 검사(Surface/Chipping=Bottom/SideAppearanceInspector) → PASS/FAIL 만(핸들러는 IsPass만 소비).
            //      칩 측정 상세(Width/Chipping/Foreign mm)는 비전 자체 표시용(ModuleResultStore/팝업)으로만 유지.
            string verdict = r.IsPass ? "PASS" : "FAIL";
            if (ins is PlacementGapInspector)
            {
                var mpOff = m.ExportCameraMapping();
                double oxPx = ItemPx(r.Items, "Offset X", mpOff.ScaleX);   // 검사기 mm ÷ Scale = 원본 픽셀(Scale<=0이면 이미 px)
                double oyPx = ItemPx(r.Items, "Offset Y", mpOff.ScaleY);
                return $"{verdict};x={oxPx:F3};y={oyPx:F3};width={image.Width};height={image.Height}";
            }
            return verdict;
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
        /// 이후 각 시리즈 첫 샘플이 자동으로 최초값(점)이 된다.</summary>
        public static string FocusStart(string[] parts)
        {
            if (parts == null || parts.Length < 4) return "fail:need camera target";
            if (!AutoFocusStore.TryParseCamera(parts[2], out var cam)) return "fail:bad camera";
            if (!AutoFocusStore.TryParseTarget(parts[3], out var tgt)) return "fail:bad target";
            AutoFocusProcessor.WaitForDrain(2000);   // 이전 스캔 잔여 백그라운드 처리 정리 후 리셋
            AutoFocusStore.Start(cam, tgt);
            AutoFocusTactLog.MarkCycleStart(cam + "/" + tgt);
            return $"OK;camera={cam};target={tgt}";
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

            // 응답 = "그랩 완료" ACK. read loop 는 grab + 큐에 넣기만(저장만) 하고 즉시 ACK.
            // ROI 잘라내기(LockBits) + 채점은 전부 백그라운드 → ACK 지연 = grab 시간뿐.
            // 오토포커스 내부 grab — ExposureDone(EPD) 푸시 억제(응답 스트림 오염 방지). ExposureDone 은 grab 내에서 동기 발화.
            var swGrab = Stopwatch.StartNew();
            GrabResult g;
            SuppressExposurePush = true;
            try { g = m.Grab(); }
            finally { SuppressExposurePush = false; }
            swGrab.Stop();
            if (g == null || !g.IsSuccess)
            {
                string err = g != null ? g.ErrorMessage : "grab";
                if (g != null) g.Dispose();
                return "fail:" + err;
            }

            int imgW = g.Image != null ? g.Image.Width : 0;
            int imgH = g.Image != null ? g.Image.Height : 0;

            // 잘라낼 ROI 영역만 수집(픽셀 작업 없음, 즉시). 실제 crop/채점/Dispose 는 백그라운드.
            Roi[] afRois = AutoFocusRoiStore.GetRois(cam, tgt);
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
                LogTiming(m.Name, "FOCUS_VAL", tgt.ToString(), swGrab.ElapsedMilliseconds, 0);
                // 원본 g 의 소유권을 백그라운드로 이전(거기서 crop/채점 후 Dispose).
                AutoFocusProcessor.Enqueue(m.Name, cam, tgt, motorZ, isInitial, swGrab.ElapsedMilliseconds,
                    g, rects.ToArray(), series.ToArray(), imgW, imgH);
                return $"OK;z={motorZ.ToString("F4", inv)};pickup={pickup};init={(isInitial ? 1 : 0)};queued=1";
            }

            // ROI 미설정 → 전체 프레임 동기 채점(드문 폴백). 측면(pickup=0)은 시리즈 1로.
            double score = AutoFocusCore.Score(g.Image);
            g.Dispose();
            LogTiming(m.Name, "FOCUS_VAL", tgt.ToString(), swGrab.ElapsedMilliseconds, 0);
            int series0 = pickup >= 1 ? pickup : 1;
            AutoFocusStore.AddSample(cam, tgt, series0, motorZ, score, isInitial);
            return $"OK;z={motorZ.ToString("F4", inv)};score={score.ToString("F2", inv)};pickup={pickup};init={(isInitial ? 1 : 0)}";
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
        /// 응답(기존 ROT_CENTER 식 인덱스 키): "OK;p1z=&lt;bestZ&gt;;p1s=&lt;bestScore&gt;;p1n=&lt;n&gt;;p2z=...".
        /// pickupNo 지정 시 그 픽업만.
        /// </summary>
        public static string FocusBest(string[] parts)
        {
            if (parts == null || parts.Length < 4) return "fail:need camera target";
            if (!AutoFocusStore.TryParseCamera(parts[2], out var cam)) return "fail:bad camera";
            if (!AutoFocusStore.TryParseTarget(parts[3], out var tgt)) return "fail:bad target";

            int onlyPickup = -1;
            if (parts.Length > 4) int.TryParse(parts[4], out onlyPickup);
            if (onlyPickup == 0) onlyPickup = 1;   // 측면(pickup=0) → 단일 시리즈(1)

            // FOCUS_VAL 들이 백그라운드로 채점 중이므로, 누적이 모두 끝난 뒤(=처리 완료) best 를 회수한다.
            // 완료될 때까지 충분히 대기해야 불완전한 best 로 응답하지 않는다(처리 완료 후 ACK).
            AutoFocusProcessor.WaitForDrain(120000);

            var sess = AutoFocusStore.Get(cam, tgt);
            if (sess == null) return "fail:no session";

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder("OK");
            foreach (var row in sess.BuildBestTable())
            {
                if (onlyPickup >= 0 && row.PickupNo != onlyPickup) continue;
                int p = row.PickupNo;
                sb.Append(";p" + p + "z=" + row.BestMotorZ.ToString("F4", inv));
                sb.Append(";p" + p + "s=" + row.BestScore.ToString("F2", inv));
                sb.Append(";p" + p + "n=" + row.SampleCount);
            }
            AutoFocusTactLog.MarkCycleEnd(cam + "/" + tgt);
            return sb.ToString();
        }
    }
}
