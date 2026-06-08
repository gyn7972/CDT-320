using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class RecipePage
    {
        private System.ComponentModel.IContainer components = null;

        private Label            _hdr;
        private Panel            _bar;
        private Button           _btnSpc;
        private Button           _btnParams;
        private TableLayoutPanel _table;
        // _tree, _content 는 Code.cs(상태)에서도 쓰이므로 여기 선언
        private TreeView         _tree;
        private Panel            _content;
        private TableLayoutPanel _root;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._hdr = new Label();
            this._bar = new Panel();
            this._btnSpc = new Button();
            this._btnParams = new Button();
            this._table = new TableLayoutPanel();
            this._tree = new TreeView();
            this._content = new Panel();
            this._root = new TableLayoutPanel();
            this._bar.SuspendLayout();
            this._table.SuspendLayout();
            this._root.SuspendLayout();
            this.SuspendLayout();

            // _hdr (root TLP 0,0 셀 — Absolute 30)
            this._hdr.Dock = DockStyle.Fill;
            this._hdr.Height = 30;
            this._hdr.Text = "Recipe — Module";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = Color.White;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // _bar (root TLP 0,1 — Absolute 40)
            this._bar.Dock = DockStyle.Fill;
            this._bar.Height = 40;
            this._bar.BackColor = Color.WhiteSmoke;

            // _btnSpc
            this._btnSpc.Location = new Point(8, 4);
            this._btnSpc.Size = new Size(180, 32);
            this._btnSpc.Text = "SPC X-bar Chart";
            this._btnSpc.FlatStyle = FlatStyle.Flat;
            this._btnSpc.BackColor = Color.White;
            this._btnSpc.Font = UiTheme.ButtonFont;
            this._btnSpc.Click += new System.EventHandler(this.OnSpcClick);

            // _btnParams
            this._btnParams.Location = new Point(196, 4);
            this._btnParams.Size = new Size(220, 32);
            this._btnParams.Text = "Inspection Parameters…";
            this._btnParams.FlatStyle = FlatStyle.Flat;
            this._btnParams.BackColor = Color.White;
            this._btnParams.Font = UiTheme.ButtonFont;
            this._btnParams.Click += new System.EventHandler(this.OnParamsClick);

            this._bar.Controls.Add(this._btnSpc);
            this._bar.Controls.Add(this._btnParams);

            // _tree
            this._tree.Dock = DockStyle.Fill;
            this._tree.Font = UiTheme.ButtonFont;
            this._tree.BorderStyle = BorderStyle.FixedSingle;
            this._tree.AfterSelect += new TreeViewEventHandler(this.OnTreeAfterSelect);

            // _content
            this._content.Dock = DockStyle.Fill;
            this._content.BackColor = UiTheme.MainBg;

            // _table (Col0 Absolute 280 = tree, Col1 Percent 100 = content)
            this._table.Dock = DockStyle.Fill;
            this._table.ColumnCount = 2;
            this._table.RowCount = 1;
            this._table.BackColor = UiTheme.MainBg;
            this._table.Margin = Padding.Empty;
            this._table.Padding = Padding.Empty;
            this._table.ColumnStyles.Clear();
            this._table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f));
            this._table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._table.RowStyles.Clear();
            this._table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._table.Controls.Add(this._tree, 0, 0);
            this._table.Controls.Add(this._content, 1, 0);

            // _root (Row0 30=hdr, Row1 40=bar, Row2 100%=table)
            this._root.Dock = DockStyle.Fill;
            this._root.ColumnCount = 1;
            this._root.RowCount = 3;
            this._root.BackColor = UiTheme.MainBg;
            this._root.Margin = Padding.Empty;
            this._root.Padding = Padding.Empty;
            this._root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._root.Controls.Add(this._hdr, 0, 0);
            this._root.Controls.Add(this._bar, 0, 1);
            this._root.Controls.Add(this._table, 0, 2);

            // RecipePage
            this.Controls.Add(this._root);
            this.Load += new System.EventHandler(this.OnPageLoad);
            this.Name = "RecipePage";
            this._bar.ResumeLayout(false);
            this._table.ResumeLayout(false);
            this._root.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
