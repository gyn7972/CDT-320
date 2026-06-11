using System;
using System.Collections.Generic;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Localization
{
    /// <summary>
    /// 간단한 in-memory 다국어 테이블.
    /// Current 언어를 변경하면 LanguageChanged 이벤트가 발생하고,
    /// 구독 중인 컨트롤은 <see cref="Apply"/> 을 호출해 Text 를 갱신한다.
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
        public const string Zh = "zh-CN";    // Stage 11 — 중국어 추가
        public const string Ja = "ja";       // Stage 12 — 일본어 추가

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
            RegisterChinese();   // Stage 11
            RegisterJapanese();  // Stage 12
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
                // 중국어 미번역 → 영어 fallback
                if (d.TryGetValue(En, out var efallback)) return efallback;
                if (d.TryGetValue(Ko, out var kfallback)) return kfallback;
            }
            return key;
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
                    // 추가 메타 (예: "i18n:work.init;level:Operator") — 첫 ';' 까지만 키
                    int sep = key.IndexOf(';');
                    if (sep >= 0) key = key.Substring(0, sep);
                    string val = T(key);
                    if (c is BottomMenuButton bmb) bmb.Label = val;
                    else c.Text = val;
                    c.Invalidate();
                }
            }
            // 폼 자체 캡션
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
        //  기본 번역 키 등록 (확장은 이 함수만 추가하면 됨)
        // ───────────────────────────────────────
        private static void Register()
        {
            A("app.title",            "CDT-320",          "CDT-320");

            // 하단 탭
            A("tab.work",             "작업",              "Work");
            A("tab.workInfo",         "작업 정보",         "Work Info");
            A("tab.history",          "이력",              "History");
            A("tab.recipe",           "레시피",            "Recipe");
            A("tab.settings",         "설정",              "Settings");
            A("tab.user",             "사용자",            "User");
            A("tab.exit",             "종료",              "Exit");

            // 상단 헤더/상태 바
            A("header.user",          "사용자",            "User");
            A("header.time",          "시간",              "Time");
            A("header.state.none",    "NONE",             "NONE");
            A("status.mapEmpty",      "빈 맵",             "Empty Map");
            A("status.project",       "Project Name :",   "Project Name :");
            A("status.barcode",       "Barcode Name :",   "Barcode Name :");
            A("status.bin",           "1Bin :",           "1Bin :");
            A("status.vision",        "VISION",           "VISION");
            A("status.pick",          "PICK",             "PICK");
            A("status.reference",     "REFERENCE",        "REFERENCE");

            // 사이드바 (작업)
            A("work.init",            "초기화",            "Init");
            A("work.start",           "시작",              "Start");
            A("work.stop",            "정지",              "Stop");
            A("work.cycleRun",        "CYCLE RUN",        "CYCLE RUN");
            A("work.cycleStop",       "CYCLE STOP",       "CYCLE STOP");
            A("work.resetAlarm",      "RESET ALARM",      "RESET ALARM");
            A("work.shutdown",        "설비 종료",         "SHUTDOWN");
            A("work.estop",           "비상 정지",         "E-STOP");
            A("work.inputCst",        "INPUT CST STATUS", "INPUT CST STATUS");
            A("work.outputCst",       "OUTPUT CST STATUS","OUTPUT CST STATUS");
            A("work.colletMode",      "콜렛 교체모드",      "Collet Change Mode");
            A("work.needleMode",      "니들 유닛 교체모드", "Needle Unit Change Mode");
            A("work.selfCheckMode",   "자주검사 모드",      "Self-Check Mode");
            A("work.autoPosMode",     "자동위치 설정모드",  "Auto Position Setup Mode");
            A("work.colletCleanMode", "콜렛 클리닝 모드",   "Collet Cleaning Mode");
            A("work.posCheck",        "POSITION CHECK",   "POSITION CHECK");

            // 사이드바 (레시피 - 320: FRONT/REAR Head)
            A("recipe.section",       "레시피",            "Recipe");
            A("recipe.project",       "프로젝트",          "Project");
            A("recipe.inputCassette", "INPUT CASSETTE",   "INPUT CASSETTE");
            A("recipe.inputFeeder",   "INPUT FEEDER",     "INPUT FEEDER");
            A("recipe.inputStage",    "INPUT STAGE",      "INPUT STAGE");
            A("recipe.frontHead",     "FRONT HEAD",       "FRONT HEAD");
            A("recipe.rearHead",      "REAR HEAD",        "REAR HEAD");
            A("recipe.outputFeeder",  "OUTPUT FEEDER",    "OUTPUT FEEDER");
            A("recipe.outputCassette","OUTPUT CASSETTE",  "OUTPUT CASSETTE");
            A("recipe.outputStage",   "OUTPUT STAGE",     "OUTPUT STAGE");
            A("recipe.inputVision",   "INPUT VISION",     "INPUT VISION");
            A("recipe.bottomVision",  "BOTTOM VISION",    "BOTTOM VISION");
            A("recipe.sideVision",    "SIDE VISION",      "SIDE VISION");
            A("recipe.outputVision",  "OUTPUT VISION",    "OUTPUT VISION");
            A("recipe.inputCreate",   "INPUT CREATE",     "INPUT CREATE");
            A("recipe.outputCreate",  "OUTPUT CREATE",    "OUTPUT CREATE");
            A("recipe.forceControl",  "FORCE CONTROL",    "FORCE CONTROL");

            // 설정 탭
            A("set.section",          "설정",              "Settings");
            A("set.language",         "언어",              "Language");
            A("set.simulator",        "시뮬레이터 연결",     "Simulator Link");
            A("set.teach",            "위치 티칭",          "Position Teach");
            A("set.axisSetup",        "축 셋업",           "Axis Setup");
            A("set.cameraSetup",      "카메라 셋업",        "Camera Setup");
            A("set.lightSetup",       "조명 셋업",          "Light Setup");
            A("set.connect",          "연결",              "Connect");
            A("set.disconnect",       "해제",              "Disconnect");
            A("set.host",             "호스트",            "Host");
            A("set.port",             "포트",              "Port");
            A("set.connStatus",       "상태",              "Status");
            A("set.connected",        "연결됨",            "Connected");
            A("set.disconnected",     "연결되지 않음",      "Disconnected");

            // 공통
            A("common.caption",       "캡션",              "Caption");
            A("common.ok",            "확인",              "OK");
            A("common.cancel",        "취소",              "Cancel");
            A("common.apply",         "적용",              "Apply");
            A("common.log",           "로그",              "Log");
            A("common.clear",         "지우기",            "Clear");
            A("common.info",          "정보",              "Info");
            A("common.setting",       "설정",              "Setup");
            A("common.save",          "저장",              "Save");
            A("common.open",          "열기",              "Open");
            A("common.delete",        "삭제",              "Delete");
            A("common.prev",          "이전",              "Prev");
            A("common.next",          "다음",              "Next");
            A("common.add",           "추가",              "Add");
            A("common.update",        "수정",              "Update");
            A("common.enable",        "ENABLE",           "ENABLE");
            A("common.disable",       "DISABLE",          "DISABLE");
            A("common.incomplete",    "미완료",            "Incomplete");
            A("common.complete",      "완료",              "Complete");
            A("common.state",         "상태",              "State");
            A("common.live",          "Live",             "Live");

            // 작업 서브 페이지
            A("work.page.main",       "메인 화면",          "Main");
            A("work.page.inputMap",   "INPUT STAGE MAP",  "INPUT STAGE MAP");
            A("work.page.outputMap",  "OUTPUT STAGE MAP", "OUTPUT STAGE MAP");
            A("work.sec.visionView",  "비전 화면",          "Vision View");
            A("work.sec.workMap",     "작업 맵",            "Work Map");
            A("work.sec.workInfo",    "작업 정보",          "Work Info");
            A("work.sec.workTime",    "작업 시간",          "Work Time");
            A("work.workInfo.project","프로젝트 이름",      "Project Name");
            A("work.workInfo.pickFail","PICK 실패 수량",     "PICK Fail Qty");
            A("work.workInfo.placeFail","PLACE 실패 수량", "PLACE Fail Qty");
            A("work.workInfo.workBinQty","작업 BIN 수량",  "Work BIN Qty");
            A("work.workInfo.collet1Use","# 1 Collet 사용",  "#1 Collet Used");
            A("work.workInfo.collet2Use","# 2 Collet 사용",  "#2 Collet Used");
            A("work.workInfo.needleUse","NEEDLE 사용 횟수",  "Needle Used");
            A("work.workInfo.binArrMon","빈 배열 모니터링",  "Bin Array Monitor");
            A("work.workTime.load",   "부하 시간",          "Load Time");
            A("work.workTime.up",     "가동 시간",          "Up Time");
            A("work.workTime.contUp", "연속 가동 시간",     "Cont Up Time");
            A("work.workTime.normDown","통상 정지 시간",    "Norm Down Time");
            A("work.workTime.errDown","이상 정지 시간",     "Err Down Time");
            A("work.workTime.errCnt", "이상 정지 횟수",     "Err Down Count");
            A("work.workTime.recovery","이상 복귀 시간",    "Recovery Time");
            A("work.workTime.uph",    "UPH",              "UPH");
            A("work.workTime.mtbf",   "MTBF",             "MTBF");
            A("work.workTime.mttr",   "MTTR",             "MTTR");
            A("work.workTime.cycle",  "CYCLE TIME",       "CYCLE TIME");
            A("work.workTime.rate",   "가동률",            "Uptime %");
            A("work.workTime.lotId",  "작업중인 LOT ID",   "Active LOT ID");
            A("work.workTime.ccs",    "CCS 검수 확인",      "CCS Check");

            A("work.inputMapTransfer","INPUT 맵 전환",      "INPUT Map Switch");
            A("work.outputMapTransfer","OUTPUT 맵 전환",    "OUTPUT Map Switch");
            A("work.visionAlign",     "비전 얼라인",         "Vision Align");
            A("work.waferMapOpen",    "웨이퍼 맵 오픈",      "Wafer Map Open");
            A("work.dieMap",          "다이 맵",            "Die Map");
            A("work.colletCheckMode", "콜렛 확인 모드",       "Collet Check Mode");
            A("work.needlePosMode",   "니들 위치 확인모드",    "Needle Pos Check");

            // 310 이식 — 머터리얼 / 레시피 Subset
            A("recipe.binCode",       "빈 코드 매핑",         "Bin Code Map");
            A("recipe.dieSubset",     "다이 사양",           "Die Spec");
            A("recipe.moduleSubset",  "모듈 옵션",           "Module Options");
            A("recipe.outputSubset",  "출력 옵션",           "Output Options");
            A("recipe.pickupSubset",  "픽업 순서",           "Pickup Sequence");
            A("recipe.tapeFrameSubset","웨이퍼 사양",         "Wafer (Tape Frame) Spec");
            A("recipe.loadFrame",     "로드 웨이퍼",          "Load Frame");
            A("recipe.unloadFrame",   "언로드 웨이퍼",         "Unload Frame");
            A("material.bin",         "빈 코드 매핑",         "Bin Code Map");
            A("material.diemap",      "다이 맵",             "Die Map");

            // SelfTest 5 신규 (텍스트는 영문 통일 — Self-Test 행 그대로)
            A("selftest.binCode",     "BinCodeMap",          "BinCodeMap");
            A("selftest.dieMap",      "DieMap generator",    "DieMap generator");
            A("selftest.jobQueue",    "JobQueue",            "JobQueue");
            A("selftest.interlock",   "InterlockRegistry",   "InterlockRegistry");
            A("selftest.alignment",   "AlignmentSolver",     "AlignmentSolver");

            // Stage 4 — RemoteViewer + ActiveLot
            A("settings.remoteViewer","원격 뷰어",           "Remote Viewer");
            A("wi.activeLot",         "현재 LOT",            "Active Lot");
            A("wi.opPanelStatus",     "운전 패널 상태",       "OP Panel Status");
            A("wi.plateStatus",       "Plate 적재 현황",      "Plate Status");
            // Stage 19 — Alarm Master
            A("settings.alarmMaster", "알람 마스터",         "Alarm Master");

            // 작업 정보 서브
            A("wi.inputCassette",     "INPUT CASSETTE",   "INPUT CASSETTE");
            A("wi.inputFeeder",       "INPUT FEEDER",     "INPUT FEEDER");
            A("wi.inputStage",        "INPUT STAGE",      "INPUT STAGE");
            A("wi.frontHead",         "FRONT HEAD",       "FRONT HEAD");
            A("wi.rearHead",          "REAR HEAD",        "REAR HEAD");
            A("wi.outputStage",       "OUTPUT STAGE",     "OUTPUT STAGE");
            A("wi.outputFeeder",      "OUTPUT FEEDER",    "OUTPUT FEEDER");
            A("wi.outputCassette",    "OUTPUT CASSETTE",  "OUTPUT CASSETTE");
            A("wi.logic",             "LOGIC",            "LOGIC");
            A("wi.logicLogic",        "LOGIC",            "LOGIC");
            A("wi.logicTimechart",    "TIMECHART",        "TIMECHART");
            A("wi.slotState",         "슬롯 상태",          "Slot State");
            A("wi.slotNo",            "슬롯 번호",          "Slot No.");
            A("wi.lifter",            "LIFTER",           "LIFTER");
            A("wi.lifterInit",        "LIFTER 초기화",      "LIFTER Init");
            A("wi.lifterReady",       "LIFTER READY",     "LIFTER READY");
            A("wi.liftWaferMapping",  "LIFT WAFER MAPPING","LIFT WAFER MAPPING");
            A("wi.liftWaferLoading",  "LIFT WAFER LOADING","LIFT WAFER LOADING");
            A("wi.liftWaferUnloading","LIFT WAFER UNLOADING","LIFT WAFER UNLOADING");
            A("wi.legend.ready",      "READY",            "READY");
            A("wi.legend.empty",      "EMPTY",            "EMPTY");
            A("wi.legend.working",    "WORKING",          "WORKING");
            A("wi.legend.finish",     "FINISH",           "FINISH");
            A("wi.legend.workReady",  "WORK READY",       "WORK READY");
            A("wi.head.initAll",      "전체 초기화",        "Init All");
            A("wi.head.countClear",   "전체 COUNT CLEAR",  "Clear All Count");
            A("wi.head.colletChange", "COLLET CHANGE",    "COLLET CHANGE");
            A("wi.head.autoPos",      "AUTO POSITION",    "AUTO POSITION");
            A("wi.head.colletCleaning","COLLET CLEANING", "COLLET CLEANING");
            A("wi.head.colletCheck",  "COLLET CHECK",     "COLLET CHECK");
            A("wi.exist",             "EXIST",            "EXIST");
            A("wi.feederClamp",       "FEEDER CLAMP",     "FEEDER CLAMP");
            A("wi.feederUpDown",      "FEEDER UP DOWN",   "FEEDER UP DOWN");
            A("wi.stageExist",        "STAGE EXIST",      "STAGE EXIST");
            A("wi.stageAlign",        "STAGE ALIGN",      "STAGE ALIGN");
            A("wi.stageBarcode",      "STAGE BARCODE",    "STAGE BARCODE");
            A("wi.stageChipAlign",    "STAGE CHIP ALIGN", "STAGE CHIP ALIGN");
            A("wi.stageFinish",       "STAGE FINISH",     "STAGE FINISH");
            A("wi.needleUsing",       "NEEDLE USING COUNT","NEEDLE USING COUNT");
            A("wi.jellPadUsing",      "JELL PAD USING COUNT","JELL PAD USING COUNT");
            A("wi.expending",         "EXPENDING",        "EXPENDING");
            A("wi.needleUpDown",      "NEEDLE UP / DOWN", "NEEDLE UP / DOWN");
            A("wi.wfAlign",           "WAFER ALIGN",      "WAFER ALIGN");
            A("wi.wfBarcode",         "WAFER BARCODE",    "WAFER BARCODE");
            A("wi.head1",             "HEAD #1",          "HEAD #1");
            A("wi.head2",             "HEAD #2",          "HEAD #2");
            A("wi.headAxisT",         "HEAD AXIS T",      "HEAD AXIS T");
            A("wi.headVacuum",        "HEAD VACUUM",      "HEAD VACUUM");
            A("wi.headBlow",          "HEAD BLOW",        "HEAD BLOW");
            A("wi.pickFail",          "PICK 실패 수량",    "PICK Fail Qty");
            A("wi.placeFail",         "PLACE 실패 수량",   "PLACE Fail Qty");
            A("wi.collet1Use",        "# 1 Collet 사용",   "#1 Collet Used");
            A("wi.collet2Use",        "#2 Collet 사용",    "#2 Collet Used");
            A("wi.outStageZ",         "OUTPUT STAGE Z",   "OUTPUT STAGE Z");
            A("wi.outGoodCount",      "GOOD 카운트",       "GOOD Count");
            A("wi.outNgCount",        "NG 카운트",         "NG Count");
            A("wi.stageInit",         "STAGE INIT",       "STAGE INIT");
            A("wi.stageReady",        "STAGE READY",      "STAGE READY");
            A("stage.needleCylInfo",  "STAGE & NEEDLE 실린더 정보", "STAGE & NEEDLE Cyl Info");

            // 이력
            A("hist.alarm",           "알람",              "Alarm");
            A("hist.warning",         "경고",              "Warning");
            A("hist.event",           "이벤트",             "Event");
            A("hist.data",            "데이터",             "Data");
            A("hist.work",            "작업",               "Work");
            A("hist.msgEdit",         "MESSAGE 편집",      "MESSAGE Edit");
            A("hist.col.index",       "INDEX",            "INDEX");
            A("hist.col.date",        "DATE",             "DATE");
            A("hist.col.user",        "USER",             "USER");
            A("hist.col.code",        "CODE",             "CODE");
            A("hist.col.desc",        "DESCRIPTION",      "DESCRIPTION");

            // 레시피 서브
            A("recipe.lowerVision",   "LOWER VISION",     "LOWER VISION");
            A("recipe.inputMapCreate","INPUT MAP CREATE", "INPUT MAP CREATE");
            A("recipe.outputMapCreate","OUTPUT MAP CREATE","OUTPUT MAP CREATE");
            A("recipe.dieMapSetup",   "DIE MAP SETUP",    "DIE MAP SETUP");

            // 설정 서브
            A("set.general",          "GENERAL",          "GENERAL");
            A("set.motion",           "MOTION",           "MOTION");
            A("set.ioControl",        "DIGITAL LINK",     "DIGITAL LINK");
            A("set.digital",          "DIGITAL",          "DIGITAL");
            A("set.digitalLink",      "DIGITAL LINK",     "DIGITAL LINK");
            A("set.cylinder",         "CYLINDER",         "CYLINDER");
            A("set.lamp",             "LAMP",             "LAMP");
            A("set.switch",           "SWITCH",           "SWITCH");
            A("set.lightSource",      "LIGHT SOURCE",     "LIGHT SOURCE");
            A("set.barcode",          "BARCODE",          "BARCODE");
            A("set.zoomLens",         "ZOOM LENS",        "ZOOM LENS");
            A("set.heightSensor",     "HEIGHT SENSOR",    "HEIGHT SENSOR");
            A("set.visionLink",       "VISION 연결",       "Vision Link");
            A("set.selfTest",         "자가 진단",         "Self-Test");
            A("set.gen.language",     "언어 설정",         "Language");
            A("set.gen.binArr",       "빈 배열파일",        "Bin Array File");
            A("set.gen.visionMatchErr","비전 매칭 에러",    "Vision Match Error");

            // 다이얼로그 제목
            A("dlg.login",            "USER LOGIN",       "USER LOGIN");
            A("dlg.colletChange",     "콜렛 교체중",        "COLLET CHANGING");
            A("dlg.colletCleaning",   "콜렛 클리닝",         "COLLET CLEANING");
            A("dlg.needleChange",     "니들 유닛 교체중",    "NEEDLE UNIT CHANGING");
            A("dlg.pickFail",         "PICK 실패",         "PICK FAIL");
            A("dlg.placeFail",        "PLACE 실패",        "PLACE FAIL");
            A("dlg.visionAlignFail",  "VISION ALIGN FAIL","VISION ALIGN FAIL");
            A("dlg.alignMatchFail",   "얼라인 매칭 실패",    "ALIGN MATCH FAIL");
            A("dlg.alignConfirm",     "얼라인 확인 요청",    "ALIGN CONFIRM");
            A("dlg.barcodeConfirm",   "바코드 확인 요청",    "BARCODE CONFIRM");
            A("dlg.lotIdInput",       "LOT ID 입력",       "LOT ID INPUT");
            A("dlg.selfInspection",   "자주 검사",          "SELF INSPECTION");
            A("dlg.autoPos",          "자동셋팅 위치",      "AUTO POS SETUP");
            A("dlg.posCheck",         "POSITION CHECK",   "POSITION CHECK");
            A("dlg.ccsInspection",    "CCS INSPECTION",   "CCS INSPECTION");
        }

        private static void A(string key, string ko, string en)
        {
            _map[key] = new Dictionary<string, string> { { Ko, ko }, { En, en } };
        }

        /// <summary>중국어 번역 — 핵심 키만. 누락 키는 영어→한국어 fallback.</summary>
        private static void RegisterChinese()
        {
            Z("app.title",           "CDT-320 设备");
            Z("common.setting",      "设置");
            Z("common.delete",       "删除");
            Z("common.open",         "打开");
            Z("common.save",         "保存");
            Z("common.close",        "关闭");
            Z("common.cancel",       "取消");
            Z("common.ok",           "确认");

            // 탭
            Z("tab.work",            "工作");
            Z("tab.workInfo",        "工作信息");
            Z("tab.history",         "历史");
            Z("tab.recipe",          "配方");
            Z("tab.settings",        "设置");
            Z("tab.user",            "用户");
            Z("tab.exit",            "退出");

            // 작업 탭 액션
            Z("work.init",           "初始化");
            Z("work.start",          "开始");
            Z("work.stop",           "停止");
            Z("work.cycleRun",       "循环运行");
            Z("work.cycleStop",      "循环停止");
            Z("work.inputCst",       "输入卡匣");
            Z("work.outputCst",      "输出卡匣");
            Z("work.dieMap",         "晶圆图");
            Z("work.visionAlign",    "视觉对位");
            Z("work.waferMapOpen",   "打开晶圆图");

            // Recipe
            Z("recipe.project",      "项目");
            Z("recipe.binCode",      "Bin 码映射");
            Z("recipe.dieSubset",    "Die 规格");
            Z("recipe.tapeFrameSubset","晶圆规格");
            Z("recipe.loadFrame",    "上料晶圆");
            Z("recipe.unloadFrame",  "下料晶圆");

            // Settings
            Z("set.general",         "通用");
            Z("set.motion",          "运动");
            Z("set.digital",         "数字 I/O");
            Z("set.lamp",            "信号灯");
            Z("set.simulator",       "模拟器");
            Z("set.visionLink",      "视觉连接");
            Z("set.selfTest",        "自我诊断");
            Z("settings.remoteViewer","远程查看器");

            // 작업 정보
            Z("wi.activeLot",        "当前批次");
            Z("wi.opPanelStatus",    "操作面板状态");
            Z("wi.plateStatus",      "Plate 状态");
            Z("recipe.moduleSubset", "模块选项");
            Z("recipe.outputSubset", "输出选项");
            Z("wi.inputCassette",    "输入卡匣");
            Z("wi.outputCassette",   "输出卡匣");
            Z("wi.frontHead",        "前端头");
            Z("wi.rearHead",         "后端头");

            // SelfTest
            Z("selftest.binCode",    "Bin 码映射");
            Z("selftest.dieMap",     "晶圆图生成器");
            Z("selftest.jobQueue",   "作业队列");
            Z("selftest.interlock",  "联锁注册");
            Z("selftest.alignment",  "对位求解器");

            // Material
            Z("material.bin",        "Bin 码映射");
            Z("material.diemap",     "晶圆图");

            // 헤더
            Z("header.user",         "用户");
            Z("header.time",         "时间");

            // 상태바
            Z("status.mapEmpty",     "映射: 空");
            Z("status.project",      "项目");
            Z("status.barcode",      "条码");
            Z("status.bin",          "Bin");
            Z("status.vision",       "视觉");
            Z("status.pick",         "拾取");
            Z("status.reference",    "参考");
        }

        /// <summary>중국어 번역 추가. 한국어/영어는 RegisterByKey() 에서 이미 등록.</summary>
        private static void Z(string key, string zh)
        {
            if (!_map.TryGetValue(key, out var d))
            {
                d = new Dictionary<string, string>();
                _map[key] = d;
            }
            d[Zh] = zh;
        }

        /// <summary>일본어 번역 — 반도체 산업 표준 (Stage 12).</summary>
        private static void RegisterJapanese()
        {
            J("app.title",           "CDT-320 装置");
            J("common.setting",      "設定");
            J("common.delete",       "削除");
            J("common.open",         "開く");
            J("common.save",         "保存");
            J("common.close",        "閉じる");
            J("common.cancel",       "キャンセル");
            J("common.ok",           "確認");

            // 탭
            J("tab.work",            "ワーク");
            J("tab.workInfo",        "ワーク情報");
            J("tab.history",         "履歴");
            J("tab.recipe",          "レシピ");
            J("tab.settings",        "設定");
            J("tab.user",            "ユーザー");
            J("tab.exit",            "終了");

            // 작업 탭 액션
            J("work.init",           "初期化");
            J("work.start",          "開始");
            J("work.stop",           "停止");
            J("work.cycleRun",       "サイクル実行");
            J("work.cycleStop",      "サイクル停止");
            J("work.dieMap",         "ダイマップ");
            J("work.visionAlign",    "ビジョンアライン");
            J("work.waferMapOpen",   "ウェハマップを開く");

            // Recipe
            J("recipe.project",      "プロジェクト");
            J("recipe.binCode",      "Binコード");
            J("recipe.dieSubset",    "ダイ仕様");
            J("recipe.tapeFrameSubset","ウェハ仕様");
            J("recipe.loadFrame",    "ロードウェハ");
            J("recipe.unloadFrame",  "アンロードウェハ");

            // Settings
            J("set.general",         "一般");
            J("set.motion",          "モーション");
            J("set.simulator",       "シミュレータ");
            J("set.visionLink",      "ビジョン接続");
            J("set.selfTest",        "自己診断");
            J("settings.remoteViewer","リモートビューア");

            // 작업 정보
            J("wi.activeLot",        "アクティブロット");
            J("wi.opPanelStatus",    "操作パネル状態");
            J("wi.plateStatus",      "Plate ステータス");
            J("recipe.moduleSubset", "モジュールオプション");
            J("recipe.outputSubset", "出力オプション");
            J("wi.inputCassette",    "入力カセット");
            J("wi.outputCassette",   "出力カセット");
            J("wi.frontHead",        "フロントヘッド");
            J("wi.rearHead",         "リアヘッド");

            // SelfTest
            J("selftest.binCode",    "Binコード");
            J("selftest.dieMap",     "ダイマップ生成器");
            J("selftest.jobQueue",   "ジョブキュー");
            J("selftest.interlock",  "インターロック");
            J("selftest.alignment",  "アライメントソルバ");

            // Material
            J("material.bin",        "Binコード");
            J("material.diemap",     "ダイマップ");

            // 헤더 / 상태바
            J("header.user",         "ユーザー");
            J("header.time",         "時刻");
            J("status.project",      "プロジェクト");
            J("status.barcode",      "バーコード");
            J("status.bin",          "Bin");
            J("status.vision",       "ビジョン");
            J("status.pick",         "ピック");
            J("status.reference",    "リファレンス");
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
