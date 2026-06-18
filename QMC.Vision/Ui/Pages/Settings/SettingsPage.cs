using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using QMC.Common.Recipes;
using QMC.Vision.Config;
using QMC.Vision.Ui;
using QMC.Vision.Ui.Controls;
using QMC.Vision.Ui.Localization;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 설정 페이지 — 우측 사이드바(SidebarButton 리스트, 핸들러 SettingsTab 정렬) + 좌측 디테일 호스트.
    /// 사이드바: GENERAL(환경설정) / 카메라 셋업(모듈별) / 조명 셋업.
    /// 정적 chrome 은 .Designer.cs, 서브패널 인스턴스화·버튼 구성·전환은 Code.
    /// </summary>
    public partial class SettingsPage : PageBase
    {
        private ConfigurationPage    _configPanel;     // GENERAL — 환경설정(Provider/포트/로그 경로) 흡수
        private CameraMappingPanel   _camPanel;         // 카메라 셋업 + 조명 컨트롤러/페이지 지정(모듈)
        private LightSystemSetupPage _lightSetupPage;   // 조명 셋업(컨트롤러 정의)
        private Control              _currentEditor;    // 편집기 캐시

        private readonly List<SidebarButton> _sideButtons = new List<SidebarButton>();

        public SettingsPage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildDetailPanels();
            BuildSidebar();
            Lang.LanguageChanged += OnSettingsLangChanged;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { Lang.LanguageChanged -= OnSettingsLangChanged; } catch { }
            base.OnHandleDestroyed(e);
        }

        /// <summary>언어 변경 — 사이드바 버튼(i18n 태그) 재번역. 디테일 패널은 각자 핸들러로 갱신.</summary>
        private void OnSettingsLangChanged()
        {
            if (IsDisposed) return;
            try { BeginInvoke((Action)(() => Lang.Apply(this))); } catch { }
        }

        /// <summary>우측 디테일 호스트에 들어가는 서브패널 인스턴스화 — 런타임.</summary>
        private void BuildDetailPanels()
        {
            // GENERAL — 환경설정 흡수(앱 전역: Provider/포트/이미지·데이터 로그 경로).
            _configPanel = new ConfigurationPage { Dock = DockStyle.Fill, Visible = false };
            _detailHost.Controls.Add(_configPanel);

            // 모듈 노드 = 카메라 셋업 + 조명 컨트롤러/페이지 지정(모듈 Setup.LightPages).
            _camPanel = new CameraMappingPanel { Dock = DockStyle.Fill, Visible = false };
            _detailHost.Controls.Add(_camPanel);

            // 조명 셋업 페이지
            _lightSetupPage = new LightSystemSetupPage { Dock = DockStyle.Fill, Visible = false };
            _detailHost.Controls.Add(_lightSetupPage);
        }

        /// <summary>우측 사이드바 버튼 구성 — 핸들러 정렬(GENERAL / 카메라 셋업(모듈별) / 조명 셋업).</summary>
        private void BuildSidebar()
        {
            _btnHost.Controls.Clear();
            _sideButtons.Clear();

            // GENERAL (환경설정)
            AddSideButton(Lang.T("set.general"), "set.general", (s, e) => { SelectButton((SidebarButton)s); ShowGeneral(); });

            // 카메라 셋업 — 모듈별 (Vision 이 실제 사용하는 카메라만)
            foreach (var alg in VisionAlgorithm.All)
            {
                string algKey = alg;
                AddSideButton(Lang.Algo(alg), "algo." + alg,
                    (s, e) => { SelectButton((SidebarButton)s); ShowCameraMapping(algKey); });
            }

            // 조명 셋업
            AddSideButton(Lang.T("set.lightSetup"), "set.lightSetup", (s, e) => { SelectButton((SidebarButton)s); ShowLightSystemSetup(); });

            // 기본 선택 = GENERAL
            if (_sideButtons.Count > 0)
            {
                SelectButton(_sideButtons[0]);
                ShowGeneral();
            }
        }

        private void AddSideButton(string text, string i18nKey, EventHandler onClick)
        {
            var btn = new SidebarButton
            {
                Text          = text,
                Tag           = string.IsNullOrEmpty(i18nKey) ? null : "i18n:" + i18nKey,
                Width         = UiTheme.SidebarWidth - 8,
                Height        = 46,
                ShowStatusDot = false,
                Margin        = new Padding(0, 0, 0, 2)
            };
            btn.Click += onClick;
            _btnHost.Controls.Add(btn);
            _sideButtons.Add(btn);
        }

        private void SelectButton(SidebarButton sel)
        {
            foreach (var b in _sideButtons) b.Selected = (b == sel);
        }

        private void ShowGeneral()
        {
            SwapEditor(_configPanel);
        }

        private void ShowCameraMapping(string algorithm)
        {
            SwapEditor(_camPanel);
            _camPanel.SelectAlgorithm(algorithm);
        }

        private void ShowLightSystemSetup()
        {
            SwapEditor(_lightSetupPage);
        }

        private void SwapEditor(Control next)
        {
            foreach (Control c in _detailHost.Controls)
                c.Visible = false;
            if (!_detailHost.Controls.Contains(next)) _detailHost.Controls.Add(next);
            next.Visible = true;
            next.BringToFront();

            // 영구 패널(_configPanel/_camPanel/_lightSetupPage)은 재사용 — dispose 대상에서 제외.
            if (_currentEditor != null && _currentEditor != next
                && _currentEditor != _configPanel
                && _currentEditor != _camPanel && _currentEditor != _lightSetupPage)
            {
                try { _detailHost.Controls.Remove(_currentEditor); _currentEditor.Dispose(); } catch { }
            }
            _currentEditor = next;
        }
    }
}
