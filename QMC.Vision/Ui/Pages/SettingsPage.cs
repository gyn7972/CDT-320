using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.Common.Recipes;
using QMC.Vision.Config;
using QMC.Vision.Core.Parameters;
using QMC.Vision.Ui;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 설정 페이지 — 좌측 사이드바(트리) + 우측 디테일 호스트.
    /// Stage 93 — Designer/Code 분리. 정적 chrome 은 .Designer.cs, 서브패널 인스턴스화·트리 구성·전환은 Code.
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        private CameraMappingPanel      _camPanel;
        private InspectionOverridePanel _inspPanel;       // Stage 64 — 검사별 카메라 오버라이드
        private InspectionLightPanel    _lightPanel;      // Stage 69 — 검사별 조명
        private TabControl              _inspTabs;        // Stage 69 — [카메라][조명] 탭
        private LightSystemSetupPage    _lightSetupPage;  // Stage 69 — 조명 시스템 Setup
        private Control                 _currentEditor;   // 검사 알고리즘 편집기 캐시
        // P4 — ② 검사 파라미터 그리드(에디터 5종 흡수). 런타임 단일 패널 재사용.
        private Panel                   _inspParamHost;
        private ParameterGridControl    _inspParamGrid;
        private Button                  _btnInspParamSave;
        private string                  _inspParamTarget;

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

            // Stage 69 — 검사 노드용 [카메라][조명] 탭
            _inspPanel = new InspectionOverridePanel { Dock = DockStyle.Fill };
            _inspPanel.OverrideChanged += OnOverrideChanged;
            _lightPanel = new InspectionLightPanel { Dock = DockStyle.Fill };
            _inspTabs = new TabControl { Dock = DockStyle.Fill, Visible = false };
            var tabCam   = new TabPage("카메라"); tabCam.Controls.Add(_inspPanel);
            var tabLight = new TabPage("조명");   tabLight.Controls.Add(_lightPanel);
            _inspTabs.TabPages.Add(tabCam);
            _inspTabs.TabPages.Add(tabLight);
            _detailHost.Controls.Add(_inspTabs);

            // Stage 69 — 조명 시스템 Setup 페이지
            _lightSetupPage = new LightSystemSetupPage { Dock = DockStyle.Fill, Visible = false };
            _detailHost.Controls.Add(_lightSetupPage);

            // P4 — ② 검사 파라미터 통일 그리드(에디터 5종 흡수). 그리드 + 하단 저장 바.
            _inspParamGrid = new ParameterGridControl { Dock = DockStyle.Fill };
            _btnInspParamSave = new Button
            {
                Text = "저장", Dock = DockStyle.Right, Width = 120,
                FlatStyle = FlatStyle.Flat, BackColor = UiTheme.Accent, ForeColor = Color.White, Font = UiTheme.SectionFont
            };
            _btnInspParamSave.Click += OnInspParamSave;
            var inspBar = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = UiTheme.MainBg };
            inspBar.Controls.Add(_btnInspParamSave);
            _inspParamHost = new Panel { Dock = DockStyle.Fill, Visible = false };
            _inspParamHost.Controls.Add(_inspParamGrid);
            _inspParamHost.Controls.Add(inspBar);
            _detailHost.Controls.Add(_inspParamHost);
        }

        private void OnInspParamSave(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_inspParamTarget)) ParameterStoreHost.Current?.SaveTarget(_inspParamTarget);
        }

        private void OnOverrideChanged(string alg, string insp) => RefreshInspectionNode(alg, insp);

        private void LoadAlgorithms()
        {
            AlgorithmCameraMapStore.Load();

            _tree.Nodes.Clear();
            var camRoot = _tree.Nodes.Add("camera-root", "■ 카메라 매핑");
            camRoot.NodeFont = UiTheme.SectionFont;
            foreach (var alg in VisionAlgorithm.All)
            {
                var algNode = camRoot.Nodes.Add("cam:" + alg, VisionAlgorithm.Label(alg));
                // Stage 64 — 알고리즘 아래 검사 자식 노드 (cam:<alg>:<inspectionId>)
                foreach (var insp in InspectionLabel.InspectionsOf(alg))
                    algNode.Nodes.Add("cam:" + alg + ":" + insp, InspNodeText(alg, insp));
            }

            var inspRoot = _tree.Nodes.Add("insp-root", "■ 검사 알고리즘");
            inspRoot.NodeFont = UiTheme.SectionFont;
            foreach (var t in InspectionTools)
                inspRoot.Nodes.Add("insp:" + t.Key, t.Value);

            // Stage 69 — 시스템 설정 그룹 + 조명 시스템 노드
            var sysRoot = _tree.Nodes.Add("sys-root", "■ 시스템 설정");
            sysRoot.NodeFont = UiTheme.SectionFont;
            sysRoot.Nodes.Add("sys:light", "조명 시스템");

            camRoot.Expand();
            inspRoot.Expand();
            sysRoot.Expand();
            _tree.SelectedNode = camRoot.Nodes[0];
        }

        /// <summary>검사 노드 라벨 — override 존재 시 " ●" 접미사.</summary>
        private static string InspNodeText(string alg, string insp)
        {
            string label = InspectionLabel.Get(alg, insp);
            var bm = AlgorithmCameraMapStore.Current?.Get(alg);
            bool hasOv = bm?.Inspections != null &&
                         bm.Inspections.Exists(o =>
                             string.Equals(o.InspectionId, insp, StringComparison.OrdinalIgnoreCase) && !o.IsEmpty());
            return hasOv ? label + "  ●" : label;
        }

        /// <summary>override 변경 후 해당 검사 노드의 ● 표시 갱신.</summary>
        private void RefreshInspectionNode(string alg, string insp)
        {
            var found = _tree.Nodes.Find("cam:" + alg + ":" + insp, true);
            if (found != null && found.Length > 0)
                found[0].Text = InspNodeText(alg, insp);
        }

        private static readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>> InspectionTools
            = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>
            {
                new System.Collections.Generic.KeyValuePair<string, string>("BottomInspection", "바텀 검사 파라미터"),
                new System.Collections.Generic.KeyValuePair<string, string>("SideInspection",   "측면 검사 파라미터"),
                new System.Collections.Generic.KeyValuePair<string, string>("DieGapInspection", "다이 갭 검사"),
                new System.Collections.Generic.KeyValuePair<string, string>("Distortion",       "왜곡 보정"),
                new System.Collections.Generic.KeyValuePair<string, string>("VisionScale",      "비전 스케일 캘리브"),
            };

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
                    ShowInspectionOverride(rest.Substring(0, idx), rest.Substring(idx + 1));
            }
            else if (key.StartsWith("insp:"))
            {
                var tool = key.Substring("insp:".Length);
                ShowInspectionEditor(tool);
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

        private void ShowInspectionOverride(string algorithm, string inspectionId)
        {
            // Stage 69 — 검사 노드 = [카메라][조명] 탭. 두 패널에 같은 검사 컨텍스트 주입.
            SwapEditor(_inspTabs);
            _inspPanel .SelectInspection(algorithm, inspectionId);
            _lightPanel.SelectInspection(algorithm, inspectionId);
        }

        private void ShowLightSystemSetup()
        {
            SwapEditor(_lightSetupPage);
        }

        private void ShowInspectionEditor(string tool)
        {
            // P4 — 에디터 5종 → 통일 그리드 흡수. 레지스트리로 정규 target 해석 후 store 질의.
            var entry = InspectionParamRegistry.ByTool(tool);
            var store = ParameterStoreHost.Current;
            if (entry == null || store == null) return;
            _inspParamTarget = entry.Target;
            _inspParamGrid.SetItems(store.GetByTarget(entry.Target)
                .Select(d => ParameterGridItem.FromDescriptor(d, store))
                .Where(x => x != null));
            SwapEditor(_inspParamHost);
        }

        private void SwapEditor(Control next)
        {
            foreach (Control c in _detailHost.Controls)
                c.Visible = false;
            if (!_detailHost.Controls.Contains(next)) _detailHost.Controls.Add(next);
            next.Visible = true;
            next.BringToFront();

            // 기존 inspection 편집기 메모리 정리 (새 인스턴스로 교체).
            // 영구 패널(_camPanel/_inspTabs/_lightSetupPage)은 dispose 대상에서 제외.
            if (_currentEditor != null && _currentEditor != next
                && _currentEditor != _camPanel && _currentEditor != _inspTabs && _currentEditor != _lightSetupPage
                && _currentEditor != _inspParamHost)
            {
                try { _detailHost.Controls.Remove(_currentEditor); _currentEditor.Dispose(); } catch { }
            }
            _currentEditor = next;
        }
    }
}
