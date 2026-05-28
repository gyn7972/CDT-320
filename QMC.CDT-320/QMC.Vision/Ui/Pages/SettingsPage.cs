using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Recipes;
using QMC.Vision.Config;
using QMC.Vision.Ui.Editors;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 설정 페이지 — 좌측 사이드바(2 그룹) + 우측 디테일.
    /// <list type="bullet">
    ///   <item>카메라 매핑: 5 비전 모듈 (Wafer/Bin/BottomInsp/FrontSide/RearSide) ↔ 카메라 ID/파라미터</item>
    ///   <item>검사 알고리즘: 5 InspectionParameters (BottomInsp/SideInsp/DieGap/Distortion/VisionScale)</item>
    /// </list>
    /// </summary>
    public class SettingsPage : UserControl
    {
        private TreeView _tree;
        private Panel    _detailHost;

        private CameraMappingPanel      _camPanel;
        private InspectionOverridePanel _inspPanel;       // Stage 64 — 검사별 카메라 오버라이드
        private UserControl             _currentEditor;   // 검사 알고리즘 편집기 캐시 비활성

        public SettingsPage()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildLayout();
            LoadAlgorithms();
        }

        private void BuildLayout()
        {
            BackColor = UiTheme.MainBg;

            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 30,
                Text = "설정 — 비전 알고리즘별 카메라 + 검사 파라미터",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            // 좌측 사이드바
            var sidebar = new Panel
            {
                Dock = DockStyle.Left, Width = UiTheme.SidebarWidth + 20, BackColor = UiTheme.SidebarBg
            };
            Controls.Add(sidebar);

            var sideHdr = new Label
            {
                Dock = DockStyle.Top, Height = 28, Text = "  알고리즘 선택",
                BackColor = UiTheme.SidebarHeaderBg, ForeColor = UiTheme.SidebarHeaderFg,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft
            };
            sidebar.Controls.Add(sideHdr);

            _tree = new TreeView
            {
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.None,
                BackColor = UiTheme.SidebarBtnBg, ForeColor = UiTheme.SidebarBtnFg,
                Font = UiTheme.ButtonFont,
                ShowLines = false, ShowPlusMinus = true, ShowRootLines = true,
                HideSelection = false, FullRowSelect = true, ItemHeight = 28
            };
            _tree.AfterSelect += Tree_AfterSelect;
            sidebar.Controls.Add(_tree);
            sidebar.Controls.SetChildIndex(_tree,  0);
            sidebar.Controls.SetChildIndex(sideHdr, 1);

            // 우측 디테일
            _detailHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.MainBg };
            Controls.Add(_detailHost);
            Controls.SetChildIndex(_detailHost, 0);
            Controls.SetChildIndex(sidebar,    1);
            Controls.SetChildIndex(hdr,        2);

            _camPanel = new CameraMappingPanel { Dock = DockStyle.Fill, Visible = false };
            _detailHost.Controls.Add(_camPanel);

            _inspPanel = new InspectionOverridePanel { Dock = DockStyle.Fill, Visible = false };
            _inspPanel.OverrideChanged += (alg, insp) => RefreshInspectionNode(alg, insp);
            _detailHost.Controls.Add(_inspPanel);
        }

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

            camRoot.Expand();
            inspRoot.Expand();
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
            // 루트 노드 선택 시 마지막 화면 유지
        }

        private void ShowCameraMapping(string algorithm)
        {
            SwapEditor(_camPanel);
            _camPanel.SelectAlgorithm(algorithm);
        }

        private void ShowInspectionOverride(string algorithm, string inspectionId)
        {
            SwapEditor(_inspPanel);
            _inspPanel.SelectInspection(algorithm, inspectionId);
        }

        private void ShowInspectionEditor(string tool)
        {
            UserControl ed;
            switch (tool)
            {
                case "BottomInspection": ed = new BottomInspectionParameterEditor(); break;
                case "SideInspection":   ed = new SideInspectionParameterEditor();   break;
                case "DieGapInspection": ed = new DieGapInspectionParameterEditor(); break;
                case "Distortion":       ed = new DistortionParameterEditor();       break;
                case "VisionScale":      ed = new VisionScaleParameterEditor();      break;
                default: return;
            }
            ed.Dock = DockStyle.Fill;
            SwapEditor(ed);
        }

        private void SwapEditor(UserControl next)
        {
            foreach (Control c in _detailHost.Controls)
                c.Visible = false;
            if (!_detailHost.Controls.Contains(next)) _detailHost.Controls.Add(next);
            next.Visible = true;
            next.BringToFront();

            // 기존 inspection 편집기 메모리 정리 (새 인스턴스로 교체).
            // _camPanel / _inspPanel 은 영구 패널이므로 dispose 대상에서 제외.
            if (_currentEditor != null && _currentEditor != next
                && _currentEditor != _camPanel && _currentEditor != _inspPanel)
            {
                try { _detailHost.Controls.Remove(_currentEditor); _currentEditor.Dispose(); } catch { }
            }
            _currentEditor = next;
        }
    }
}
