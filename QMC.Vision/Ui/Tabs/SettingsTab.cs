using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using QMC.Common.Recipes;          // VisionAlgorithm
using QMC.Vision.Ui;               // UiTheme
using QMC.Vision.Ui.Controls;      // SidebarButton
using QMC.Vision.Ui.Pages;         // ConfigurationPage / CameraMappingPanel / LightSystemSetupPage

namespace QMC.Vision.Ui.Tabs
{
    /// <summary>
    /// 설정 탭 — 핸들러 SettingsTab 정렬. 탭 사이드바(GENERAL / 카메라 셋업(모듈별) / 조명 셋업) +
    /// 콘텐츠 호스트. (구 SettingsPage 의 우측 사이드바 로직을 탭 레벨로 끌어올림)
    /// </summary>
    public class SettingsTab : TabBase
    {
        private ConfigurationPage    _configPanel;     // GENERAL — 환경설정 흡수
        private CameraMappingPanel   _camPanel;         // 카메라 셋업(모듈)
        private LightSystemSetupPage _lightSetupPage;   // 조명 셋업
        private CommLinkPage         _commPage;         // 통신(TCP) — 포트 편집 + 접속 상태
        private Control              _currentEditor;

        private readonly List<SidebarButton> _sideButtons = new List<SidebarButton>();

        public SettingsTab()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            SetSidebarHeader("설정");
            BuildDetailPanels();
            BuildSidebar();
        }

        private void BuildDetailPanels()
        {
            _configPanel = new ConfigurationPage { Dock = DockStyle.Fill, Visible = false };
            PnlContent.Controls.Add(_configPanel);

            _camPanel = new CameraMappingPanel { Dock = DockStyle.Fill, Visible = false };
            PnlContent.Controls.Add(_camPanel);

            _lightSetupPage = new LightSystemSetupPage { Dock = DockStyle.Fill, Visible = false };
            PnlContent.Controls.Add(_lightSetupPage);

            _commPage = new CommLinkPage { Dock = DockStyle.Fill, Visible = false };
            PnlContent.Controls.Add(_commPage);
        }

        private void BuildSidebar()
        {
            AddButton("GENERAL", (s, e) => { Select((SidebarButton)s); SwapEditor(_configPanel); });

            foreach (var alg in VisionAlgorithm.All)
            {
                string algKey = alg;
                AddButton(VisionAlgorithm.Label(alg),
                    (s, e) => { Select((SidebarButton)s); SwapEditor(_camPanel); _camPanel.SelectAlgorithm(algKey); });
            }

            AddButton("조명 셋업", (s, e) => { Select((SidebarButton)s); SwapEditor(_lightSetupPage); });

            AddButton("통신", (s, e) => { Select((SidebarButton)s); SwapEditor(_commPage); });

            if (_sideButtons.Count > 0)
            {
                Select(_sideButtons[0]);
                SwapEditor(_configPanel);
            }
        }

        private void AddButton(string text, EventHandler onClick)
        {
            var btn = new SidebarButton
            {
                Text          = text,
                Width         = UiTheme.SidebarWidth - 8,
                Height        = 46,
                ShowStatusDot = false,
                Margin        = new Padding(0, 0, 0, 2)
            };
            btn.Click += onClick;
            PnlSidebarButtons.Controls.Add(btn);
            _sideButtons.Add(btn);
        }

        private void Select(SidebarButton sel)
        {
            foreach (var b in _sideButtons) b.Selected = (b == sel);
        }

        private void SwapEditor(Control next)
        {
            foreach (Control c in PnlContent.Controls)
                c.Visible = false;
            if (!PnlContent.Controls.Contains(next)) PnlContent.Controls.Add(next);
            next.Visible = true;
            next.BringToFront();
            _currentEditor = next;
        }
    }
}
