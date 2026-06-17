using System;
using System.Collections.Generic;
using System.Windows.Forms;
using QMC.Vision.Ui;
using QMC.Vision.Ui.Controls;
using QMC.Vision.Ui.Localization;
using QMC.Vision.Ui.Security;

namespace QMC.Vision.Ui.Tabs
{
    /// <summary>
    /// 하단 탭 UserControl의 공통 껍데기 — 핸들러 QMC.CDT_320.Ui.Tabs.TabBase 정렬.
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
        private bool _secondaryGroupSeparatorAdded = false;

        public TabBase()
        {
            InitializeComponent();
        }

        public virtual void AttachHost(Form1 host)
        {
            Host = host;
            if (!string.IsNullOrEmpty(_currentKey)) ShowPage(_currentKey);
            else if (SidebarButtons.Count > 0)
            {
                foreach (var kv in SidebarButtons) { ShowPage(kv.Key); break; }
            }
        }

        // ──────────────────────────────────────────
        //  사이드바 구성 API
        // ──────────────────────────────────────────

        /// <summary>주 메뉴 버튼 추가. pageFactory 는 클릭 시 생성할 서브 페이지.</summary>
        protected SidebarButton AddSidebarButton(
            string i18nKey,
            UserLevel? minLevel           = null,
            Func<UserControl> pageFactory = null,
            bool toBottomArea             = false)
        {
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

        /// <summary>캐시된 모든 페이지에 대해 UiClickAuditor.PerformClickAll 호출.</summary>
        public (int tried, int success, int failed) PerformClickAllPages()
        {
            int t = 0, s = 0, f = 0;
            foreach (var kv in PageCache)
            {
                if (kv.Value == null) continue;
                var (tt, ss, ff) = QMC.Vision.Ui.Util.UiClickAuditor.PerformClickAll(kv.Value);
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

        /// <summary>모든 사이드바 페이지를 1회씩 순회한 뒤 첫 페이지로 복귀.</summary>
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

            foreach (var kv in SidebarButtons)
                kv.Value.Selected = (kv.Key == key);

            if (_currentKey != null && PageCache.TryGetValue(_currentKey, out var prev))
                prev.Visible = false;

            _currentKey = key;

            if (!PageCache.TryGetValue(key, out var page))
            {
                if (PageFactories.TryGetValue(key, out var factory))
                {
                    page = factory();
                    if (page != null)
                    {
                        page.Dock    = DockStyle.Fill;
                        page.Visible = false;
                        PnlContent.Controls.Add(page);
                        PageCache[key] = page;
                        Lang.Apply(page);
                    }
                }
            }
            if (page != null)
            {
                page.Visible = true;
                AccessControl.Apply(page);
            }
        }

        /// <summary>디자이너에서 이미 생성·배치된 SidebarButton 을 TabBase 인프라에 등록.</summary>
        protected SidebarButton RegisterSidebarButton(
            SidebarButton btn,
            string i18nKey,
            UserLevel? minLevel           = null,
            Func<UserControl> pageFactory = null)
        {
            if (btn == null) return null;

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
