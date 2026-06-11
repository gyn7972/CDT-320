using System;
using System.ComponentModel;
using System.Windows.Forms;
using QMC.Common.Recipes;
using QMC.Vision.Config;
using QMC.Vision.Ui;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 설정 페이지 — 좌측 사이드바(트리) + 우측 디테일 호스트.
    /// Stage 93 — Designer/Code 분리. 정적 chrome 은 .Designer.cs, 서브패널 인스턴스화·트리 구성·전환은 Code.
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        private CameraMappingPanel      _camPanel;
        private InspectionLightPanel    _lightPanel;      // Stage 69 — 검사별 조명 (C3a: 검사별 카메라 override 드롭, 조명만)
        private LightSystemSetupPage    _lightSetupPage;  // Stage 69 — 조명 시스템 Setup
        private Control                 _currentEditor;   // 검사 노드 편집기 캐시

        public SettingsPage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildDetailPanels();
            LoadAlgorithms();
        }

        /// <summary>우측 디테일 호스트에 들어가는 서브패널(다른 UserControl) 인스턴스화 — 런타임.</summary>
        private void BuildDetailPanels()
        {
            _camPanel = new CameraMappingPanel { Dock = DockStyle.Fill, Visible = false };
            _detailHost.Controls.Add(_camPanel);

            // C3a — 검사 노드 = 조명만(검사별 카메라 override 드롭). 카메라는 알고리즘 노드(상위) 매핑에서.
            _lightPanel = new InspectionLightPanel { Dock = DockStyle.Fill, Visible = false };
            _detailHost.Controls.Add(_lightPanel);

            // Stage 69 — 조명 시스템 Setup 페이지
            _lightSetupPage = new LightSystemSetupPage { Dock = DockStyle.Fill, Visible = false };
            _detailHost.Controls.Add(_lightSetupPage);
            // 검사 파라미터(ROI/임계)는 RecipePage(InspectorTargetPage) 그리드에서 편집 — SettingsPage 는 카메라/조명만.
        }

        private void LoadAlgorithms()
        {
            _tree.Nodes.Clear();
            var camRoot = _tree.Nodes.Add("camera-root", "■ 카메라 매핑");
            camRoot.NodeFont = UiTheme.SectionFont;
            foreach (var alg in VisionAlgorithm.All)
            {
                var algNode = camRoot.Nodes.Add("cam:" + alg, VisionAlgorithm.Label(alg));
                // 알고리즘 아래 검사 자식 노드 (cam:<alg>:<inspectionId>) — 선택 시 조명 편집.
                foreach (var insp in InspectionLabel.InspectionsOf(alg))
                    algNode.Nodes.Add("cam:" + alg + ":" + insp, InspectionLabel.Get(alg, insp));
            }

            // Stage 69 — 시스템 설정 그룹 + 조명 시스템 노드
            var sysRoot = _tree.Nodes.Add("sys-root", "■ 시스템 설정");
            sysRoot.NodeFont = UiTheme.SectionFont;
            sysRoot.Nodes.Add("sys:light", "조명 시스템");

            camRoot.Expand();
            sysRoot.Expand();
            _tree.SelectedNode = camRoot.Nodes[0];
        }

        private void Tree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var key = e.Node?.Name;
            if (string.IsNullOrEmpty(key)) return;

            if (key.StartsWith("cam:"))
            {
                var rest = key.Substring("cam:".Length);   // "<alg>" 또는 "<alg>:<inspectionId>"
                int idx = rest.IndexOf(':');
                if (idx < 0)
                    ShowCameraMapping(rest);
                else
                    ShowInspectionLight(rest.Substring(0, idx), rest.Substring(idx + 1));
            }
            else if (key == "sys:light")
            {
                ShowLightSystemSetup();
            }
            // 루트 노드 선택 시 마지막 화면 유지
        }

        private void ShowCameraMapping(string algorithm)
        {
            SwapEditor(_camPanel);
            _camPanel.SelectAlgorithm(algorithm);
        }

        private void ShowInspectionLight(string algorithm, string inspectionId)
        {
            // C2/C3a — 조명 SSOT=노드. 짧은 id(InspectionsOf) → GetAlgorithm 으로 노드 해석.
            SwapEditor(_lightPanel);
            var node = (FindForm() as Form1)?.ResolveModule(algorithm)?.GetAlgorithm(inspectionId);
            _lightPanel.SelectInspection(node, algorithm, inspectionId);
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

            // 영구 패널(_camPanel/_lightPanel/_lightSetupPage)은 재사용 — dispose 대상에서 제외.
            if (_currentEditor != null && _currentEditor != next
                && _currentEditor != _camPanel && _currentEditor != _lightPanel && _currentEditor != _lightSetupPage)
            {
                try { _detailHost.Controls.Remove(_currentEditor); _currentEditor.Dispose(); } catch { }
            }
            _currentEditor = next;
        }
    }
}
