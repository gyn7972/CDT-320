using System;
using System.Windows.Forms;
using QMC.Vision.Core;
using QMC.Vision.Modules;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// Recipe — 좌측 트리(5 모듈: Wafer/Bin/Bottom/FrontSide/RearSide) → FinderPage/InspectorPage 전환.
    /// Form1 의 모듈 인스턴스를 공유(Load 이벤트에서 해석).
    /// Stage 93 — Designer/Code 분리. 정적 shell 은 .Designer.cs, 트리 채움/패널 전환은 Code.
    /// </summary>
    public partial class RecipePage : UserControl
    {
        public RecipePage()
        {
            InitializeComponent();
        }

        // ── 이벤트 핸들러 (Designer 에서 named 연결) ──
        private void OnSpcClick(object sender, EventArgs e) => ShowSpc();
        private void OnParamsClick(object sender, EventArgs e) => ShowParameterEditors();

        private void OnTreeAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is FinderLauncher fl) ShowFinder(fl.Module, fl.Finder);
            else if (e.Node.Tag is InspectorLauncher il) ShowInspector(il.Module, il.Inspector);
        }

        private void OnPageLoad(object sender, EventArgs e) => PopulateTree();

        private class FinderLauncher    { public VisionModule Module; public IPatternFinder Finder; }
        private class InspectorLauncher { public VisionModule Module; public IInspector     Inspector; }

        private void PopulateTree()
        {
            var host = FindForm() as Form1;
            if (host == null) return;

            _tree.Nodes.Clear();
            AddModuleNode("Wafer vision",             host.WaferMod);
            AddModuleNode("Bin vision",               host.BinMod);
            AddModuleNode("Bottom inspection vision", host.BottomMod);
            AddModuleNode("Front side inspection",    host.FrontSideMod);  // Stage 65 — 누락 모듈 추가
            AddModuleNode("Rear side inspection",     host.RearSideMod);   // Stage 65 — 누락 모듈 추가
            _tree.ExpandAll();
        }

        private void AddModuleNode(string label, VisionModule module)
        {
            if (module == null) return;
            var root = _tree.Nodes.Add(label + "  [" + module.Camera.Info.Id + "]");
            foreach (var kv in module.Finders)
                root.Nodes.Add(new TreeNode(kv.Key) { Tag = new FinderLauncher { Module = module, Finder = kv.Value } });
            foreach (var kv in module.Inspectors)
                root.Nodes.Add(new TreeNode(kv.Key) { Tag = new InspectorLauncher { Module = module, Inspector = kv.Value } });
        }

        private void ShowFinder(VisionModule mod, IPatternFinder f)
        {
            _content.Controls.Clear();
            _content.Controls.Add(new FinderPage(mod, f) { Dock = DockStyle.Fill });
        }

        private void ShowInspector(VisionModule mod, IInspector i)
        {
            _content.Controls.Clear();
            _content.Controls.Add(new InspectorPage(mod, i) { Dock = DockStyle.Fill });
        }

        private void ShowSpc()
        {
            _content.Controls.Clear();
            _content.Controls.Add(new SpcChartPage { Dock = DockStyle.Fill });
        }

        private void ShowParameterEditors()
        {
            _content.Controls.Clear();
            _content.Controls.Add(new Editors.ParameterEditorHost { Dock = DockStyle.Fill });
        }
    }
}
