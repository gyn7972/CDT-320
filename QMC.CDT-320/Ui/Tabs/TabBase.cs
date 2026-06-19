using System;
using System.Collections.Generic;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;
using QMC.CDT_320.Ui.Util;

namespace QMC.CDT_320.Ui.Tabs
{
    /// <summary>
    /// 하단 6개 탭 UserControl의 공통 껍데기.
    /// 우측 사이드바 + 중앙 콘텐츠(서브 페이지 호스트) 구조.
    /// 사이드바 버튼 클릭 시 등록된 서브 페이지 UserControl로 콘텐츠를 교체한다.
    /// </summary>
    public partial class TabBase : UserControl
    {
        public Form1 Host { get; private set; }

        /// <summary>사이드바 key → 버튼</summary>
        protected readonly Dictionary<string, SidebarButton> SidebarButtons
            = new Dictionary<string, SidebarButton>();

        /// <summary>사이드바 key → 해당 서브 페이지 UserControl (lazy init).</summary>
        protected readonly Dictionary<string, Func<UserControl>> PageFactories
            = new Dictionary<string, Func<UserControl>>();

        /// <summary>이미 생성된 서브 페이지 캐시.</summary>
        protected readonly Dictionary<string, UserControl> PageCache
            = new Dictionary<string, UserControl>();

        private string _currentKey;

        public TabBase()
        {
            InitializeComponent();
            UiDoubleBuffer.Enable(this);
        }

        public virtual void AttachHost(Form1 host)
        {
            Host = host;
            // 첫 버튼이 있으면 기본 선택
            if (!string.IsNullOrEmpty(_currentKey)) ShowPage(_currentKey);
            else if (SidebarButtons.Count > 0)
            {
                foreach (var kv in SidebarButtons) { ShowPage(kv.Key); break; }
            }
        }

        // ──────────────────────────────────────────
        //  사이드바 구성 API
        // ──────────────────────────────────────────

        /// <summary>주 메뉴 버튼 추가. <paramref name="pageFactory"/>는 클릭 시 생성할 서브 페이지.
        /// <paramref name="toBottomArea"/>=true 인 첫 호출 시 시각적 구분선이 자동 삽입된다 (그 후 동일 스크롤 리스트에 이어 배치).</summary>
        protected SidebarButton AddSidebarButton(
            string i18nKey,
            UserLevel? minLevel      = null,
            Func<UserControl> pageFactory = null,
            bool toBottomArea        = false)
        {
            if (minLevel.HasValue) AccessPolicy.RegisterFeature(i18nKey, minLevel.Value);
            var btn = new SidebarButton
            {
                Text   = Lang.T(i18nKey),
                Tag    = minLevel.HasValue
                            ? ("i18n:" + i18nKey + ";level:" + minLevel.Value)
                            : ("i18n:" + i18nKey),
                Width  = UiTheme.SidebarWidth - 8,
                Margin = new Padding(0, 0, 0, 2)
            };

            string key = i18nKey;
            btn.Click += (s, e) => ShowPage(key);

            SidebarButtons[key] = btn;
            if (pageFactory != null) PageFactories[key] = pageFactory;

            // 모든 버튼은 단일 스크롤 패널(PnlSidebarButtons)에 추가한다.
            // toBottomArea=true 인 첫 버튼 직전에는 1회 한정으로 얇은 구분선을 삽입하여
            // 시각적으로 보조 메뉴 그룹임을 알린다 (이전엔 별도 Dock=Bottom 패널이라 큰 공백 발생).
            if (toBottomArea && !_secondaryGroupSeparatorAdded)
            {
                var sep = new Panel
                {
                    Width     = UiTheme.SidebarWidth - 8,
                    Height    = 2,
                    BackColor = System.Drawing.Color.FromArgb(0x33, 0x33, 0x33),
                    Margin    = new Padding(0, 6, 0, 6)
                };
                PnlSidebarButtons.Controls.Add(sep);
                _secondaryGroupSeparatorAdded = true;
            }
            PnlSidebarButtons.Controls.Add(btn);
            return btn;
        }

        private bool _secondaryGroupSeparatorAdded = false;

        /// <summary>사이드바에 섹션 구분선/공백 추가.</summary>
        protected void AddSidebarSpacer(int height = 18)
        {
            var sep = new Panel { Width = UiTheme.SidebarWidth - 8, Height = height, BackColor = UiTheme.SidebarBg };
            PnlSidebarButtons.Controls.Add(sep);
        }

        protected void SetSidebarHeader(string i18nKey)
        {
            LblSidebarHeader.Tag  = "i18n:" + i18nKey;
            LblSidebarHeader.Text = Lang.T(i18nKey);
        }

        // ──────────────────────────────────────────
        //  서브 페이지 전환
        // ──────────────────────────────────────────

        /// <summary>Stage 60 R12 — 캐시된 모든 페이지에 대해 UiClickAuditor.PerformClickAll 호출.</summary>
        public (int tried, int success, int failed) PerformClickAllPages()
        {
            int t = 0, s = 0, f = 0;
            foreach (var kv in PageCache)
            {
                if (kv.Value == null) continue;
                var (tt, ss, ff) = QMC.CDT_320.Ui.Util.UiClickAuditor.PerformClickAll(kv.Value);
                t += tt; s += ss; f += ff;
                Application.DoEvents();
            }
            return (t, s, f);
        }

        /// <summary>해당 키가 이 탭에 속하면 ShowPage 호출 후 true 반환.</summary>
        public bool TryShowPage(string key)
        {
            if (string.IsNullOrEmpty(key) || !SidebarButtons.ContainsKey(key)) return false;
            ShowPage(key);
            return true;
        }

        /// <summary>
        /// Stage 60 — 모든 사이드바 페이지를 1회씩 순회한다. 각 페이지의 OnLoad 가 발생하므로
        /// PageBase 의 UiClickAuditor 가 자동 실행되어 dead-button 통계를 로그에 남긴다.
        /// 끝나면 첫 페이지로 돌아간다.
        /// </summary>
        public void ShowAllPagesOnce()
        {
            string firstKey = null;
            foreach (var kv in SidebarButtons)
            {
                if (firstKey == null) firstKey = kv.Key;
                try { ShowPage(kv.Key); } catch { }
                Application.DoEvents();
            }
            if (firstKey != null)
            {
                try { ShowPage(firstKey); } catch { }
            }
        }

        public void ShowPage(string key)
        {
            if (!SidebarButtons.ContainsKey(key)) return;
            if (string.Equals(_currentKey, key, StringComparison.Ordinal))
                return;

            // 버튼 선택 상태 갱신
            foreach (var kv in SidebarButtons)
            {
                bool selected = kv.Key == key;
                if (kv.Value.Selected != selected)
                    kv.Value.Selected = selected;
            }

            // 이전 페이지 숨기기
            if (_currentKey != null && PageCache.TryGetValue(_currentKey, out var prev))
            {
                if (prev.Visible)
                    prev.Visible = false;
            }

            _currentKey = key;

            // 페이지 획득 (lazy)
            if (!PageCache.TryGetValue(key, out var page))
            {
                if (PageFactories.TryGetValue(key, out var factory))
                {
                    page = factory();
                    if (page != null)
                    {
                        page.Dock    = DockStyle.Fill;
                        page.Visible = false;
                        UiDoubleBuffer.Enable(page);
                        PnlContent.Controls.Add(page);
                        PageCache[key] = page;
                        Lang.Apply(page);
                    }
                }
            }
            if (page != null)
            {
                if (!page.Visible)
                    page.Visible = true;
                AccessControl.Apply(page);
            }
        }

        /// <summary>디자이너에서 이미 생성·배치된 <see cref="SidebarButton"/>을 TabBase 인프라에 등록한다.
        /// (권한 태그 / 페이지 팩토리 / 클릭 핸들러만 연결하며, 컨트롤 자체의 부모 배치는 디자이너가 담당.)</summary>
        protected SidebarButton RegisterSidebarButton(
            SidebarButton btn,
            string i18nKey,
            UserLevel? minLevel           = null,
            Func<UserControl> pageFactory = null)
        {
            if (btn == null) return null;

            if (minLevel.HasValue) AccessPolicy.RegisterFeature(i18nKey, minLevel.Value);
            btn.Tag  = minLevel.HasValue
                          ? ("i18n:" + i18nKey + ";level:" + minLevel.Value)
                          : ("i18n:" + i18nKey);
            btn.Text = Lang.T(i18nKey);

            string key = i18nKey;
            btn.Click += (s, e) => ShowPage(key);

            SidebarButtons[key] = btn;
            if (pageFactory != null) PageFactories[key] = pageFactory;
            return btn;
        }
    }
}
