using System;
using System.Collections.Generic;
using System.Windows.Forms;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Localization
{
    /// <summary>
    /// 간단한 in-memory 다국어 테이블 — 핸들러 QMC.CDT_320.Ui.Localization.Lang 정렬.
    /// Current 언어를 변경하면 LanguageChanged 이벤트가 발생하고,
    /// 구독 중인 컨트롤은 <see cref="Apply"/> 를 호출해 Text 를 갱신한다.
    /// <para>
    /// 사용 예:
    ///   Lang.T("tab.work")                → 현재 언어의 "작업" / "Work" / "工作"
    ///   Lang.SetLanguage("en")            → 영어로 전환
    ///   Lang.Apply(this)                  → 하위 컨트롤 Tag가 "i18n:key" 인 것의 Text 치환
    /// </para>
    /// </summary>
    public static class Lang
    {
        public const string Ko = "ko";
        public const string En = "en";
        public const string Zh = "zh-CN";
        public const string Ja = "ja";

        public static event Action LanguageChanged;

        public static string Current { get; private set; } = Ko;

        /// <summary>지원 언어 목록. Ui.Settings 화면에서 콤보박스로 노출.</summary>
        public static readonly string[] Supported = new[] { Ko, En, Zh, Ja };

        // key → { "ko": "...", "en": "...", "zh-CN": "...", "ja": "..." }
        private static readonly Dictionary<string, Dictionary<string, string>> _map
            = new Dictionary<string, Dictionary<string, string>>();

        static Lang()
        {
            Register();
            RegisterChinese();
            RegisterJapanese();
        }

        public static void SetLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang) || !HasLanguage(lang)) return;
            if (Current == lang) return;
            Current = lang;
            LanguageChanged?.Invoke();
        }

        public static bool HasLanguage(string lang)
        {
            foreach (var s in Supported) if (s == lang) return true;
            return false;
        }

        /// <summary>키에 해당하는 현재 언어 문자열. 없으면 영어→한국어 fallback, 그것도 없으면 키 자체 반환.</summary>
        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (_map.TryGetValue(key, out var d))
            {
                if (d.TryGetValue(Current, out var s)) return s;
                if (d.TryGetValue(En, out var efallback)) return efallback;
                if (d.TryGetValue(Ko, out var kfallback)) return kfallback;
            }
            return key;
        }

        /// <summary>비전 알고리즘 표시명(언어 적용). 사전 미등록 시 Common 기본 라벨로 폴백.</summary>
        public static string Algo(string key)
        {
            string k = "algo." + (key ?? "");
            string v = T(k);
            return v == k ? QMC.Common.Recipes.VisionAlgorithm.Label(key) : v;
        }

        /// <summary>검사(finder/inspector) 표시명(언어 적용). 사전 미등록 시 Common 기본 라벨로 폴백.</summary>
        public static string Inspection(string algorithm, string inspectionId)
        {
            string k = "insp." + (inspectionId ?? "");
            string v = T(k);
            return v == k ? QMC.Common.Recipes.InspectionLabel.Get(algorithm, inspectionId) : v;
        }

        /// <summary>
        /// 컨트롤 트리를 순회하며 Tag 가 "i18n:KEY" 로 시작하면 해당 키의 번역문을 Text 에 반영.
        /// BottomMenuButton 은 Label 프로퍼티를, Form/Label/Button/그 외는 Text 를 갱신.
        /// </summary>
        public static void Apply(Control root)
        {
            if (root == null) return;
            foreach (Control c in EnumerateAll(root))
            {
                if (c.Tag is string tag && tag.StartsWith("i18n:"))
                {
                    string key = tag.Substring(5);
                    int sep = key.IndexOf(';');
                    if (sep >= 0) key = key.Substring(0, sep);
                    string val = T(key);
                    if (c is BottomMenuButton bmb) bmb.Label = val;
                    else c.Text = val;
                    c.Invalidate();
                }
            }
            if (root is Form f && f.Tag is string ft && ft.StartsWith("i18n:"))
                f.Text = T(ft.Substring(5));
        }

        private static IEnumerable<Control> EnumerateAll(Control root)
        {
            yield return root;
            foreach (Control c in root.Controls)
                foreach (var child in EnumerateAll(c))
                    yield return child;
        }

        // ───────────────────────────────────────
        //  기본 번역 키 등록 (한국어 / 영어)
        // ───────────────────────────────────────
        private static void Register()
        {
            A("app.title",            "CDT-320 VISION",   "CDT-320 VISION");

            // 하단 탭
            A("tab.work",             "작업",              "Work");
            A("tab.workInfo",         "작업 정보",         "Work Info");
            A("tab.history",          "이력",              "History");
            A("tab.recipe",           "레시피",            "Recipe");
            A("tab.settings",         "설정",              "Settings");
            A("tab.user",             "사용자",            "User");
            A("tab.exit",             "종료",              "Exit");

            // 상단 헤더
            A("header.user",          "사용자",            "User");
            A("header.time",          "시간",              "Time");

            // 설정 탭 / 사이드바
            A("set.section",          "설정",              "Settings");
            A("설정",                 "설정",              "Settings");   // SettingsTab.SetSidebarHeader("설정") 호환
            A("set.general",          "일반",              "GENERAL");
            A("set.cameraSetup",      "카메라 셋업",        "Camera Setup");
            A("set.lightSetup",       "조명 셋업",          "Light Setup");
            A("조명 셋업",            "조명 셋업",          "Light Setup");

            // 작업(Work) 탭 — OperationPage
            A("work.monitorHeader",   "작업 — 모니터링",    "Work — Monitoring");
            A("work.grabWait",        "Grab 대기",         "Awaiting Grab");
            A("work.main",            "MAIN",             "MAIN");

            // 이력(History) 탭 — DataLogPage
            A("hist.tab.data",        "Data Log",         "Data Log");
            A("hist.tab.log",         "Log",              "Log");
            A("hist.tab.alarm",       "Alarm",            "Alarm");
            A("hist.tab.utility",     "Utility",          "Utility");
            A("hist.queryRefresh",    "조회 / 새로고침",    "Query / Refresh");
            A("hist.csvExport",       "CSV 내보내기",       "Export CSV");
            A("hist.activeOnly",      "활성 알람만",        "Active alarms only");
            A("hist.empty",           "표시할 데이터가 없습니다 — 검사·이벤트·알람이 누적되면 표시됩니다.",
                                      "No data to display — inspection, event, and alarm records appear here as they accumulate.");
            A("hist.openDataFolder",  "Data Log 폴더 열기", "Open Data Log Folder");
            A("hist.openLogFolder",   "Event Log 폴더 열기","Open Event Log Folder");
            A("hist.refreshAll",      "전체 새로고침",      "Refresh All");
            A("hist.export",          "내보내기",          "Export");
            A("hist.noDataFile",      "선택한 날짜의 Data Log 파일이 없습니다.", "No Data Log file for the selected date.");
            A("hist.exportFail",      "내보내기 실패: ",    "Export failed: ");
            A("hist.openFolderFail",  "폴더 열기 실패: ",   "Failed to open folder: ");

            // 비전 알고리즘 표시명 (사이드바 탭)
            A("algo.Wafer",            "웨이퍼 비전",       "Wafer Vision");
            A("algo.Bin",              "빈 비전",           "Bin Vision");
            A("algo.BottomInspection", "바텀 검사",         "Bottom Inspection");
            A("algo.FrontSide",        "앞쪽 측면 검사",     "Front Side Inspection");
            A("algo.RearSide",         "뒤쪽 측면 검사",     "Rear Side Inspection");
            // 검사(finder/inspector) 표시명 (세팅 버튼)
            A("insp.EjectPinFinder",          "이젝트 핀",   "Eject Pin");
            A("insp.ReticleFinder",           "레티클",      "Reticle");
            A("insp.AlignDieFinder",          "얼라인 다이", "Align Die");
            A("insp.FirstReferenceFinder",    "첫째 기준",   "First Reference");
            A("insp.SecondReferenceFinder",   "둘째 기준",   "Second Reference");
            A("insp.DieFinder",               "다이",        "Die");
            A("insp.ScaleFinder",             "스케일",      "Scale");
            A("insp.PlacementInspector",      "안착 검사",   "Placement");
            A("insp.ColletFinder",            "콜렛",        "Collet");
            A("insp.SurfaceInspector",        "표면",        "Surface");
            A("insp.FocusFinder",             "포커스",      "Focus");
            A("insp.DistortionCompensation",  "왜곡 보정",   "Distortion Comp.");
            A("insp.DieEdgeFinder",           "다이 에지",   "Die Edge");
            A("insp.TopSurfaceInspector",     "앞쪽 면",     "Front Surface");
            A("insp.TopChippingInspector",    "앞쪽 칩핑",   "Front Chipping");
            A("insp.BottomSurfaceInspector",  "뒤쪽 면",     "Rear Surface");
            A("insp.BottomChippingInspector", "뒤쪽 칩핑",   "Rear Chipping");

            // 레시피(Recipe) 탭 — RecipePage / 타깃 페이지 / 조명 패널
            A("rec.projItem",         "프로젝트 (레시피)",  "Project (Recipe)");
            A("rec.sideModule",       "모듈 (Module)",     "Module");
            A("rec.finderInspector",  "Finder / Inspector","Finder / Inspector");
            A("rec.saveRecipe",       "레시피 저장 (품목별)","Save Recipe (per part)");
            A("rec.projHdr",          "레시피 (품목)",      "Recipe (Part)");
            A("rec.new",              "New",              "New");
            A("rec.copy",             "Copy",             "Copy");
            A("rec.saveAs",           "Save As",          "Save As");
            A("rec.commonHdr",        "공통 설정 (품목)",   "Common Settings (Part)");
            A("rec.commonSave",       "공통 저장 (품목)",   "Save Common (Part)");
            A("rec.hdrPrefix",        "Recipe",           "Recipe");
            A("rec.hdrProject",       "프로젝트 (품목)",    "Project (Part)");
            A("rec.commonSaved",      "공통 저장됨",        "Common saved");
            // 공통 그리드 라벨
            A("rec.partId",           "품목 (Part ID)",    "Part (Part ID)");
            A("rec.saveGoodImg",      "양품 이미지 저장",   "Save Good Image");
            A("rec.useContam",        "오염검사 사용",      "Use Contamination Inspection");
            A("rec.imageSavePath",    "이미지 저장 경로",   "Image Save Path");
            // CRUD / 다이얼로그
            A("rec.newName",          "새 레시피(품목) 이름","New recipe (part) name");
            A("rec.copyNoCurrent",    "복사할 현재 레시피가 없습니다.", "No current recipe to copy.");
            A("rec.copyNewName",      "레시피 복사 — 새 이름","Copy recipe — new name");
            A("rec.copySuffix",       "_복사",             "_copy");
            A("rec.copyDiffName",     "원본과 다른 이름을 입력하세요.", "Enter a name different from the source.");
            A("rec.copyExists",       "같은 이름의 레시피가 이미 있습니다.", "A recipe with the same name already exists.");
            A("rec.copyFail",         "복사 실패: ",        "Copy failed: ");
            A("rec.copyNoSrc",        "원본 폴더 없음",     "Source folder not found");
            A("rec.saveAsName",       "다른 이름으로 저장(품목)", "Save As (part)");
            A("rec.delDefault",       "default 레시피는 삭제할 수 없습니다.", "The default recipe cannot be deleted.");
            A("rec.delConfirm",       "레시피 삭제?",       "Delete recipe?");
            // 타깃 페이지 (Vision/Inspector)
            A("rec.roiCtrl",          "ROI 제어",          "ROI Control");
            A("rec.inspLight",        "검사 조명",          "Inspection Light");
            A("rec.inspResult",       "검사 결과",          "Inspection Result");
            A("rec.saveImg",          "이미지 저장",        "Save Image");
            A("rec.clearResult",      "결과 Clear",        "Clear Result");
            A("rec.measure",          "측정",              "Measure");
            A("col.item",             "항목",              "Item");
            A("col.value",            "값",                "Value");
            A("col.result",           "결과",              "Result");
            // 타깃 상태 메시지
            A("rec.liveStop",         "LIVE 정지: ",       "LIVE stopped: ");
            A("rec.noSaveNode",       "저장 대상 노드 없음", "No save target node");
            A("rec.targetSaved",      "타깃 저장됨 — ",     "Target saved — ");
            A("rec.targetSaveFail",   "타깃 저장 실패: ",   "Target save failed: ");
            A("rec.imgNone",          "이미지저장: 이미지 없음 (GRAB/LOAD 먼저)", "Save image: no image (GRAB/LOAD first)");
            A("rec.imgSaveOk",        "이미지 저장 OK: ",   "Image saved OK: ");
            A("rec.imgSaveFail",      "이미지 저장 실패: ", "Image save failed: ");
            A("rec.measureOnHint",    "측정 ON — 첫 점, 둘째 점을 클릭. (다시 누르면 OFF)", "Measure ON — click first then second point. (press again to turn OFF)");
            A("rec.measureOff",       "측정 OFF",          "Measure OFF");
            A("rec.measureMenu",      "측정 (Measure)",    "Measure");
            A("rec.zoomNone",         "Zoom: 이미지 없음 (GRAB/LOAD 먼저)", "Zoom: no image (GRAB/LOAD first)");
            A("rec.customZoom",       "Custom (Zoom 창)",  "Custom (Zoom window)");
            // 조명 패널 (InspectionLightPanel)
            A("rec.applyRun",         "실행 적용",          "Apply (run)");
            A("rec.reset",            "초기화",            "Reset");
            A("rec.ctrl",             "컨트롤러",          "Controller");
            A("rec.lightLoadFail",    "설정 불러올 수 없음 — 검사 노드 미해결", "Cannot load settings — inspection node unresolved");
            A("rec.lightUnassigned",  "컨트롤러/페이지 미지정 — [설정 > 검사]에서 이 검사에 컨트롤러/페이지를 지정하세요.",
                                      "Controller/page unassigned — assign one for this inspection in [Settings > Inspection].");
            A("rec.lightAssignPrefix","지정: ",            "Assigned: ");
            A("rec.lightAssignSuffix","   — 지정 변경은 [설정 > 검사]", "   — change assignment in [Settings > Inspection]");
            A("rec.lightUnassignShort","컨트롤러/페이지 미지정 — [설정 > 검사]에서 지정 후 사용", "Controller/page unassigned — assign in [Settings > Inspection] first");
            A("rec.lightSaveNoNode",  "저장 불가 — 검사 노드 미해결", "Cannot save — inspection node unresolved");
            A("rec.lightSaveExc",     "저장 예외: ",       "Save exception: ");
            A("rec.lightApplyReject", "적용 거부 — 컨트롤러/페이지 미지정", "Apply rejected — controller/page unassigned");
            A("rec.lightApplyNoCtrl", "적용 거부 — 등록된 컨트롤러 없음 (조명 연결 필요)", "Apply rejected — no registered controller (light connection required)");
            A("rec.lightApplyExc",    "적용 예외: ",       "Apply exception: ");
            A("rec.lightReset",       "초기화 — 모든 채널 Level 0 (저장 시 조명 미사용)", "Reset — all channels Level 0 (light unused on save)");
            A("rec.lightSavedFmt",    "저장 완료 — 노드 [{0}] 점등 {1}채널", "Saved — node [{0}], {1} channels lit");
            A("rec.lightAppliedFmt",  "적용 완료 — {0}/{1} 컨트롤러 병렬 ({2})", "Applied — {0}/{1} controllers in parallel ({2})");

            // GENERAL 페이지
            A("set.gen.language",     "언어 설정",         "Language");
            A("set.gen.provider",     "Provider",         "Provider");
            A("set.gen.backend",      "Vision Backend (재시작 후 반영)", "Vision Backend (applied after restart)");
            A("set.gen.cgxDiag",      "Cognex VisionPro 진단", "Cognex VisionPro diagnostics");
            A("set.gen.imageLog",     "이미지 로그 저장",   "Image log saver");
            A("set.gen.path",         "경로",              "Path");
            A("set.gen.simAuto",      "Sim 자동 실행",      "Sim Auto Run");
            A("set.gen.backendVer",   "백엔드 버전",        "Backend Version");
            A("set.gen.imgEnable",    "이미지 로그 사용",   "Enable Image Log");
            A("set.gen.imgPath",      "이미지 저장 경로",   "Image Save Path");
            A("set.gen.dataRoot",     "데이터 저장 루트",   "Data Root");
            A("set.gen.cgxResult",    "Cognex 진단",        "Cognex Diagnostics");
            A("set.gen.ocvResult",    "OpenCV 진단",        "OpenCV Diagnostics");
            A("common.load",          "불러오기",          "Load");
            A("common.test",          "테스트",            "Test");
            A("set.gen.secLang",      "언어 / 실행 모드",   "Language / Run Mode");
            A("set.gen.secImg",       "이미지 로그",        "Image Log");
            A("set.gen.secData",      "데이터 저장 경로 (재시작 후 반영)", "Data Root (applied after restart)");
            A("set.gen.cgxOut",       "진단 결과",          "Diagnostics");

            // 카메라 셋업 (CameraMappingPanel)
            A("set.cam.title",        "카메라 매핑",        "Camera Mapping");
            A("set.cam.camId",        "카메라 ID",          "Camera ID");
            A("set.cam.discover",     "카메라 검색",        "Discover");
            A("set.cam.secParam",     "카메라 파라미터",    "Camera Parameters");
            A("set.cam.secScale",     "스케일 / 캘리브레이션", "Scale / Calibration");
            A("set.cam.secLight",     "조명 컨트롤러 / 페이지 지정", "Light Controller / Page");
            A("set.cam.milDcf",       "MIL DCF 직접 지정",  "MIL DCF (manual)");
            A("set.cam.dcfPath",      "DCF 경로",           "DCF Path");
            A("set.cam.browse",       "찾기",               "Browse");
            A("set.cam.reset",        "기본값 복원",        "Reset");
            A("set.cam.apply",        "실행 모듈에 적용",   "Apply to module");
            A("set.cam.scaleCalc",    "스케일 계산",        "Calc Scale");
            // 카메라 파라미터 그리드 항목
            A("set.cam.exposure",     "노출(Exposure)",     "Exposure");
            A("set.cam.gain",         "게인(Gain)",         "Gain");
            A("set.cam.frameRate",    "프레임레이트",       "Frame rate");
            A("set.cam.trigMode",     "트리거 모드",        "Trigger Mode");
            A("set.cam.trigSrc",      "트리거 소스",        "Trigger Source");
            A("set.cam.pixFmt",       "픽셀 포맷",          "Pixel format");
            A("set.cam.delayGrab",    "그랩 전 지연",       "Delay before grab");
            A("set.cam.roiOffX",      "ROI Offset X",       "ROI Offset X");
            A("set.cam.roiOffY",      "ROI Offset Y",       "ROI Offset Y");
            A("set.cam.roiW",         "ROI 너비",           "ROI Width");
            A("set.cam.roiH",         "ROI 높이",           "ROI Height");
            // 스케일/캘리브레이션 그리드 항목
            A("set.cam.scaleX",       "스케일 X",           "Scale X");
            A("set.cam.scaleY",       "스케일 Y",           "Scale Y");
            A("set.cam.invX",         "X 반전",             "Inverted X");
            A("set.cam.invY",         "Y 반전",             "Inverted Y");
            A("set.cam.rot90",        "90° 회전",           "Rotate 90°");
            A("set.cam.returnMm",     "결과 mm 반환",       "Return mm");
            A("set.cam.chipW",        "칩 가로",            "Chip width");
            A("set.cam.chipH",        "칩 세로",            "Chip height");
            A("set.gen.secLang",      "언어 / 실행 모드",   "Language / Run Mode");
            A("set.gen.secImg",       "이미지 로그",        "Image Log");
            A("set.gen.secData",      "데이터 저장 경로 (재시작 후 반영)", "Data Root (applied after restart)");
            A("set.gen.cgxOut",       "진단 결과",          "Diagnostics");

            // 공통
            A("common.ok",            "확인",              "OK");
            A("common.cancel",        "취소",              "Cancel");
            A("common.apply",         "적용",              "Apply");
            A("common.save",          "저장",              "Save");
            A("common.open",          "열기",              "Open");
            A("common.delete",        "삭제",              "Delete");
            A("common.enable",        "ENABLE",           "ENABLE");
            A("common.disable",       "DISABLE",          "DISABLE");
            A("common.refresh",       "새로고침",          "Refresh");
            A("common.runTest",       "테스트 실행",        "Run test");
            A("common.browse",        "찾아보기...",        "Browse...");
            A("common.setting",       "설정",              "Setup");
            A("common.ready",         "READY",            "READY");
            A("common.live",          "LIVE",             "LIVE");
            A("common.date",          "일자",              "Date");
            A("common.name",          "이름",              "Name");
        }

        private static void A(string key, string ko, string en)
        {
            _map[key] = new Dictionary<string, string> { { Ko, ko }, { En, en } };
        }

        /// <summary>중국어 번역 — 핵심 키만. 누락 키는 영어→한국어 fallback.</summary>
        private static void RegisterChinese()
        {
            Z("app.title",            "CDT-320 视觉");
            Z("tab.work",             "工作");
            Z("tab.workInfo",         "工作信息");
            Z("tab.history",          "历史");
            Z("tab.recipe",           "配方");
            Z("tab.settings",         "设置");
            Z("tab.user",             "用户");
            Z("tab.exit",             "退出");

            Z("header.user",          "用户");
            Z("header.time",          "时间");

            Z("set.section",          "设置");
            Z("설정",                 "设置");
            Z("set.general",          "通用");
            Z("set.cameraSetup",      "相机设置");
            Z("set.lightSetup",       "光源设置");
            Z("조명 셋업",            "光源设置");

            Z("work.monitorHeader",   "工作 — 监控");
            Z("work.grabWait",        "等待采集");
            Z("work.main",            "主");
            Z("common.ready",         "就绪");
            Z("common.live",          "实时");
            Z("common.date",          "日期");
            Z("common.name",          "名称");

            Z("algo.Wafer",            "晶圆视觉");
            Z("algo.Bin",              "Bin 视觉");
            Z("algo.BottomInspection", "底部检查");
            Z("algo.FrontSide",        "前侧检查");
            Z("algo.RearSide",         "后侧检查");
            Z("insp.EjectPinFinder",          "顶针");
            Z("insp.ReticleFinder",           "标线");
            Z("insp.AlignDieFinder",          "对位芯片");
            Z("insp.FirstReferenceFinder",    "第一基准");
            Z("insp.SecondReferenceFinder",   "第二基准");
            Z("insp.DieFinder",               "芯片");
            Z("insp.ScaleFinder",             "比例");
            Z("insp.PlacementInspector",      "贴装检查");
            Z("insp.ColletFinder",            "吸嘴");
            Z("insp.SurfaceInspector",        "表面");
            Z("insp.FocusFinder",             "对焦");
            Z("insp.DistortionCompensation",  "畸变校正");
            Z("insp.DieEdgeFinder",           "芯片边缘");
            Z("insp.TopSurfaceInspector",     "前表面");
            Z("insp.TopChippingInspector",    "前崩边");
            Z("insp.BottomSurfaceInspector",  "后表面");
            Z("insp.BottomChippingInspector", "后崩边");

            Z("rec.projItem",         "项目 (配方)");
            Z("rec.sideModule",       "模块");
            Z("rec.saveRecipe",       "保存配方 (按品目)");
            Z("rec.projHdr",          "配方 (品目)");
            Z("rec.new",              "新建");
            Z("rec.copy",             "复制");
            Z("rec.saveAs",           "另存为");
            Z("rec.commonHdr",        "通用设置 (品目)");
            Z("rec.commonSave",       "保存通用 (品目)");
            Z("rec.hdrProject",       "项目 (品目)");
            Z("rec.commonSaved",      "通用已保存");
            Z("rec.partId",           "品目 (Part ID)");
            Z("rec.saveGoodImg",      "保存良品图像");
            Z("rec.useContam",        "使用污染检查");
            Z("rec.imageSavePath",    "图像保存路径");
            Z("rec.roiCtrl",          "ROI 控制");
            Z("rec.inspLight",        "检查照明");
            Z("rec.inspResult",       "检查结果");
            Z("rec.saveImg",          "保存图像");
            Z("rec.clearResult",      "清除结果");
            Z("rec.measure",          "测量");
            Z("col.item",             "项目");
            Z("col.value",            "值");
            Z("col.result",           "结果");
            Z("rec.applyRun",         "执行应用");
            Z("rec.reset",            "重置");
            Z("rec.ctrl",             "控制器");

            Z("hist.tab.data",        "数据日志");
            Z("hist.tab.log",         "日志");
            Z("hist.tab.alarm",       "报警");
            Z("hist.tab.utility",     "工具");
            Z("hist.queryRefresh",    "查询 / 刷新");
            Z("hist.csvExport",       "导出 CSV");
            Z("hist.activeOnly",      "仅活动报警");
            Z("hist.empty",           "暂无数据 — 检查、事件、报警记录累积后将在此显示。");
            Z("hist.openDataFolder",  "打开数据日志文件夹");
            Z("hist.openLogFolder",   "打开事件日志文件夹");
            Z("hist.refreshAll",      "全部刷新");
            Z("hist.export",          "导出");
            Z("hist.noDataFile",      "所选日期没有数据日志文件。");
            Z("hist.exportFail",      "导出失败: ");
            Z("hist.openFolderFail",  "打开文件夹失败: ");

            Z("set.gen.language",     "语言设置");
            Z("set.gen.provider",     "Provider");
            Z("set.gen.backend",      "视觉后端 (重启后生效)");
            Z("set.gen.cgxDiag",      "Cognex VisionPro 诊断");
            Z("set.gen.imageLog",     "图像日志保存");
            Z("set.gen.path",         "路径");

            Z("common.ok",            "确认");
            Z("common.cancel",        "取消");
            Z("common.apply",         "应用");
            Z("common.save",          "保存");
            Z("common.enable",        "启用");
            Z("common.disable",       "禁用");
            Z("common.refresh",       "刷新");
            Z("common.runTest",       "运行测试");
            Z("common.browse",        "浏览...");
        }

        private static void Z(string key, string zh)
        {
            if (!_map.TryGetValue(key, out var d))
            {
                d = new Dictionary<string, string>();
                _map[key] = d;
            }
            d[Zh] = zh;
        }

        /// <summary>일본어 번역 — 반도체 산업 표준.</summary>
        private static void RegisterJapanese()
        {
            J("app.title",            "CDT-320 ビジョン");
            J("tab.work",             "ワーク");
            J("tab.workInfo",         "ワーク情報");
            J("tab.history",          "履歴");
            J("tab.recipe",           "レシピ");
            J("tab.settings",         "設定");
            J("tab.user",             "ユーザー");
            J("tab.exit",             "終了");

            J("header.user",          "ユーザー");
            J("header.time",          "時刻");

            J("set.section",          "設定");
            J("설정",                 "設定");
            J("set.general",          "一般");
            J("set.cameraSetup",      "カメラ設定");
            J("set.lightSetup",       "照明設定");
            J("조명 셋업",            "照明設定");

            J("work.monitorHeader",   "ワーク — モニタリング");
            J("work.grabWait",        "グラブ待機");
            J("work.main",            "メイン");
            J("common.ready",         "レディ");
            J("common.live",          "ライブ");
            J("common.date",          "日付");
            J("common.name",          "名前");

            J("algo.Wafer",            "ウェハビジョン");
            J("algo.Bin",              "ビンビジョン");
            J("algo.BottomInspection", "ボトム検査");
            J("algo.FrontSide",        "前面側検査");
            J("algo.RearSide",         "背面側検査");
            J("insp.EjectPinFinder",          "イジェクトピン");
            J("insp.ReticleFinder",           "レチクル");
            J("insp.AlignDieFinder",          "アラインダイ");
            J("insp.FirstReferenceFinder",    "第一基準");
            J("insp.SecondReferenceFinder",   "第二基準");
            J("insp.DieFinder",               "ダイ");
            J("insp.ScaleFinder",             "スケール");
            J("insp.PlacementInspector",      "装着検査");
            J("insp.ColletFinder",            "コレット");
            J("insp.SurfaceInspector",        "表面");
            J("insp.FocusFinder",             "フォーカス");
            J("insp.DistortionCompensation",  "歪み補正");
            J("insp.DieEdgeFinder",           "ダイエッジ");
            J("insp.TopSurfaceInspector",     "前面");
            J("insp.TopChippingInspector",    "前面チッピング");
            J("insp.BottomSurfaceInspector",  "背面");
            J("insp.BottomChippingInspector", "背面チッピング");

            J("rec.projItem",         "プロジェクト (レシピ)");
            J("rec.sideModule",       "モジュール");
            J("rec.saveRecipe",       "レシピ保存 (品目別)");
            J("rec.projHdr",          "レシピ (品目)");
            J("rec.new",              "新規");
            J("rec.copy",             "コピー");
            J("rec.saveAs",           "名前を付けて保存");
            J("rec.commonHdr",        "共通設定 (品目)");
            J("rec.commonSave",       "共通保存 (品目)");
            J("rec.hdrProject",       "プロジェクト (品目)");
            J("rec.commonSaved",      "共通保存済み");
            J("rec.partId",           "品目 (Part ID)");
            J("rec.saveGoodImg",      "良品画像保存");
            J("rec.useContam",        "汚染検査を使用");
            J("rec.imageSavePath",    "画像保存パス");
            J("rec.roiCtrl",          "ROI 制御");
            J("rec.inspLight",        "検査照明");
            J("rec.inspResult",       "検査結果");
            J("rec.saveImg",          "画像保存");
            J("rec.clearResult",      "結果クリア");
            J("rec.measure",          "測定");
            J("col.item",             "項目");
            J("col.value",            "値");
            J("col.result",           "結果");
            J("rec.applyRun",         "実行適用");
            J("rec.reset",            "リセット");
            J("rec.ctrl",             "コントローラ");

            J("hist.tab.data",        "データログ");
            J("hist.tab.log",         "ログ");
            J("hist.tab.alarm",       "アラーム");
            J("hist.tab.utility",     "ユーティリティ");
            J("hist.queryRefresh",    "照会 / 更新");
            J("hist.csvExport",       "CSV エクスポート");
            J("hist.activeOnly",      "アクティブアラームのみ");
            J("hist.empty",           "表示するデータがありません — 検査・イベント・アラームが蓄積されると表示されます。");
            J("hist.openDataFolder",  "データログフォルダを開く");
            J("hist.openLogFolder",   "イベントログフォルダを開く");
            J("hist.refreshAll",      "全て更新");
            J("hist.export",          "エクスポート");
            J("hist.noDataFile",      "選択した日付のデータログファイルがありません。");
            J("hist.exportFail",      "エクスポート失敗: ");
            J("hist.openFolderFail",  "フォルダを開けませんでした: ");

            J("set.gen.language",     "言語設定");
            J("set.gen.provider",     "Provider");
            J("set.gen.backend",      "ビジョンバックエンド (再起動後反映)");
            J("set.gen.cgxDiag",      "Cognex VisionPro 診断");
            J("set.gen.imageLog",     "画像ログ保存");
            J("set.gen.path",         "パス");

            J("common.ok",            "確認");
            J("common.cancel",        "キャンセル");
            J("common.apply",         "適用");
            J("common.save",          "保存");
            J("common.enable",        "有効");
            J("common.disable",       "無効");
            J("common.refresh",       "更新");
            J("common.runTest",       "テスト実行");
            J("common.browse",        "参照...");
        }

        private static void J(string key, string ja)
        {
            if (!_map.TryGetValue(key, out var d))
            {
                d = new Dictionary<string, string>();
                _map[key] = d;
            }
            d[Ja] = ja;
        }
    }
}
