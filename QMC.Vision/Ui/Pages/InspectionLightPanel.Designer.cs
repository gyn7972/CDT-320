using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class InspectionLightPanel
    {
        private System.ComponentModel.IContainer components = null;

        private Label  _lblHeader;
        private Label  _lblWiring;
        private Panel  _bar;
        private Button _btnSave, _btnApply, _btnReset, _btnCancel;
        private Label  _lblStatus;
        private DataGridView _grid;
        private DataGridViewTextBoxColumn  _colCtrl;
        private DataGridViewTextBoxColumn  _colCh;
        private DataGridViewTextBoxColumn  _colName;
        private DataGridViewTextBoxColumn  _colLvl;
        private DataGridViewComboBoxColumn _colPg;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._lblHeader = new Label();
            this._lblWiring = new Label();
            this._bar = new Panel();
            this._btnSave = new Button();
            this._btnApply = new Button();
            this._btnReset = new Button();
            this._btnCancel = new Button();
            this._lblStatus = new Label();
            this._grid = new DataGridView();
            this._colCtrl = new DataGridViewTextBoxColumn();
            this._colCh = new DataGridViewTextBoxColumn();
            this._colName = new DataGridViewTextBoxColumn();
            this._colLvl = new DataGridViewTextBoxColumn();
            this._colPg = new DataGridViewComboBoxColumn();
            this._bar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this._grid).BeginInit();
            this.SuspendLayout();

            this.BackColor = UiTheme.MainBg;

            // _lblHeader
            this._lblHeader.Dock = DockStyle.Top;
            this._lblHeader.Height = 30;
            this._lblHeader.Text = "검사 조명";
            this._lblHeader.BackColor = UiTheme.StatusBarBg;
            this._lblHeader.ForeColor = Color.White;
            this._lblHeader.Font = UiTheme.SectionFont;
            this._lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            this._lblHeader.Padding = new Padding(10, 0, 0, 0);

            // _lblWiring
            this._lblWiring.Dock = DockStyle.Top;
            this._lblWiring.Height = 48;
            this._lblWiring.Font = UiTheme.ValueFont;
            this._lblWiring.ForeColor = Color.DarkSlateGray;
            this._lblWiring.BackColor = Color.WhiteSmoke;
            this._lblWiring.TextAlign = ContentAlignment.MiddleLeft;
            this._lblWiring.Padding = new Padding(10, 2, 0, 0);

            // _bar
            this._bar.Dock = DockStyle.Top;
            this._bar.Height = 40;
            this._bar.BackColor = UiTheme.MainBg;

            // 버튼들
            InitBtn(this._btnSave, "저장", 8, UiTheme.Accent, Color.White);
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);
            InitBtn(this._btnApply, "실행 적용", 104, Color.White, Color.Black);
            this._btnApply.Click += new System.EventHandler(this.OnApplyClick);
            InitBtn(this._btnReset, "초기화", 200, Color.White, Color.Black);
            this._btnReset.Click += new System.EventHandler(this.OnResetClick);
            InitBtn(this._btnCancel, "취소", 296, Color.White, Color.Black);
            this._btnCancel.Click += new System.EventHandler(this.OnCancelClick);
            this._bar.Controls.Add(this._btnSave);
            this._bar.Controls.Add(this._btnApply);
            this._bar.Controls.Add(this._btnReset);
            this._bar.Controls.Add(this._btnCancel);

            // _lblStatus
            this._lblStatus.Dock = DockStyle.Bottom;
            this._lblStatus.Height = 24;
            this._lblStatus.Font = UiTheme.ValueFont;
            this._lblStatus.ForeColor = Color.DarkSlateGray;
            this._lblStatus.Padding = new Padding(8, 2, 0, 0);

            // 컬럼 (Page 콤보 Items 는 런타임 BindFields)
            this._colCtrl.Name = "Ctrl"; this._colCtrl.HeaderText = "컨트롤러"; this._colCtrl.ReadOnly = true; this._colCtrl.FillWeight = 24;
            this._colCtrl.DefaultCellStyle.BackColor = Color.FromArgb(0xF2, 0xF2, 0xF2);
            this._colCh.Name = "Channel"; this._colCh.HeaderText = "Ch"; this._colCh.ReadOnly = true; this._colCh.FillWeight = 10;
            this._colCh.DefaultCellStyle.BackColor = Color.FromArgb(0xF2, 0xF2, 0xF2);
            this._colName.Name = "Name"; this._colName.HeaderText = "이름"; this._colName.ReadOnly = true; this._colName.FillWeight = 30;
            this._colName.DefaultCellStyle.BackColor = Color.FromArgb(0xF2, 0xF2, 0xF2);
            this._colLvl.Name = "Level"; this._colLvl.HeaderText = "Level"; this._colLvl.FillWeight = 18;
            this._colPg.Name = "Page"; this._colPg.HeaderText = "Page"; this._colPg.FillWeight = 12;

            // _grid
            this._grid.Dock = DockStyle.Fill;
            this._grid.RowHeadersVisible = false;
            this._grid.Font = UiTheme.ValueFont;
            this._grid.BackgroundColor = Color.White;
            this._grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.AllowUserToAddRows = false;
            this._grid.AllowUserToDeleteRows = false;
            this._grid.Columns.Add(this._colCtrl);
            this._grid.Columns.Add(this._colCh);
            this._grid.Columns.Add(this._colName);
            this._grid.Columns.Add(this._colLvl);
            this._grid.Columns.Add(this._colPg);
            this._grid.DataError += new DataGridViewDataErrorEventHandler(this.OnGridDataError);

            // InspectionLightPanel (원본 추가순서 + SetChildIndex(_grid,0))
            this.Controls.Add(this._lblHeader);
            this.Controls.Add(this._lblWiring);
            this.Controls.Add(this._bar);
            this.Controls.Add(this._lblStatus);
            this.Controls.Add(this._grid);
            this.Controls.SetChildIndex(this._grid, 0);
            this.Name = "InspectionLightPanel";
            this._bar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)this._grid).EndInit();
            this.ResumeLayout(false);
        }

        private void InitBtn(Button b, string text, int x, Color bg, Color fg)
        { b.Location = new Point(x, 4); b.Size = new Size(90, 32); b.Text = text; b.FlatStyle = FlatStyle.Flat; b.Font = UiTheme.ButtonFont; b.BackColor = bg; b.ForeColor = fg; }
    }
}
