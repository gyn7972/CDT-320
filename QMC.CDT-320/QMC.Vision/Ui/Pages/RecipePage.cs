using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Core;
using QMC.Vision.Modules;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// Recipe — 좌측 트리(5 모듈: Wafer/Bin/Bottom/FrontSide/RearSide) → FinderPage/InspectorPage 전환.
    /// Form1 의 모듈 인스턴스를 공유(Load 이벤트에서 해석).
    /// (Stage 65 통합: 구 MaintenancePage 를 Recipe 로 개명, placeholder Recipe 삭제.)
    /// </summary>
    public class RecipePage : UserControl
    {
        private TreeView _tree;
        private Panel _content;

        public RecipePage()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 30, Text = "Recipe — Module",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            // Tool 바: SPC 차트 / 파라미터 에디터 진입 버튼
            var bar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.WhiteSmoke };
            var btnSpc = new Button
            {
                Location = new Point(8, 4), Size = new Size(180, 32),
                Text = "SPC X-bar Chart", FlatStyle = FlatStyle.Flat,
                BackColor = Color.White, Font = UiTheme.ButtonFont
            };
            btnSpc.Click += (s, e) => ShowSpc();
            bar.Controls.Add(btnSpc);

            var btnParams = new Button
            {
                Location = new Point(196, 4), Size = new Size(220, 32),
                Text = "Inspection Parameters…", FlatStyle = FlatStyle.Flat,
                BackColor = Color.White, Font = UiTheme.ButtonFont
            };
            btnParams.Click += (s, e) => ShowParameterEditors();
            bar.Controls.Add(btnParams);

            // ⚠ 다단계 Dock=Fill 안정성 보장 위해 TableLayoutPanel 사용:
            //     Column 0 (Absolute 280px) = TreeView, Column 1 (Percent 100%) = 콘텐트
            //     SplitContainer 의 SplitterDistance 초기화 타이밍 이슈 회피.
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2, RowCount = 1,
                BackColor = UiTheme.MainBg, Margin = Padding.Empty, Padding = Padding.Empty
            };
            table.ColumnStyles.Clear();
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            table.RowStyles.Clear();
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _tree = new TreeView
            {
                Dock = DockStyle.Fill, Font = UiTheme.ButtonFont,
                BorderStyle = BorderStyle.FixedSingle
            };
            table.Controls.Add(_tree, 0, 0);

            _content = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.MainBg };
            table.Controls.Add(_content, 1, 0);

            // 헤더/툴바/콘텐트를 루트 TableLayoutPanel(3행: 30 / 40 / 100%)로 배치.
            // Dock=Top + Dock=Fill 형제 혼용 시 z-order 에 따라 Fill 이 Top 영역을 덮어
            // TreeView 상단(Wafer vision)이 헤더에 가려지는 문제가 있어, 겹침이 원천 차단되는 TLP 로 구성.
            // 해상도가 바뀌어도 행 비율로 유동 배치됨.
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = UiTheme.MainBg, Margin = Padding.Empty, Padding = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            hdr.Dock   = DockStyle.Fill;
            bar.Dock   = DockStyle.Fill;
            table.Dock = DockStyle.Fill;
            root.Controls.Add(hdr,   0, 0);
            root.Controls.Add(bar,   0, 1);
            root.Controls.Add(table, 0, 2);
            Controls.Add(root);

            _tree.AfterSelect += (s, e) =>
            {
                if (e.Node.Tag is FinderLauncher fl) ShowFinder(fl.Module, fl.Finder);
                else if (e.Node.Tag is InspectorLauncher il) ShowInspector(il.Module, il.Inspector);
            };

            Load += (s, ev) => PopulateTree();
        }

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
