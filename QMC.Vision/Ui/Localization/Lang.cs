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
            A("set.general",          "GENERAL",          "GENERAL");
            A("set.cameraSetup",      "카메라 셋업",        "Camera Setup");
            A("set.lightSetup",       "조명 셋업",          "Light Setup");
            A("조명 셋업",            "조명 셋업",          "Light Setup");

            // GENERAL 페이지
            A("set.gen.language",     "언어 설정",         "Language");
            A("set.gen.provider",     "Provider",         "Provider");
            A("set.gen.backend",      "Vision Backend (재시작 후 반영)", "Vision Backend (applied after restart)");
            A("set.gen.cgxDiag",      "Cognex VisionPro 진단", "Cognex VisionPro diagnostics");
            A("set.gen.imageLog",     "이미지 로그 저장",   "Image log saver");
            A("set.gen.path",         "경로",              "Path");

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
