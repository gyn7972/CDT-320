using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class InspectionLightAssignPanel
    {
        private System.ComponentModel.IContainer components = null;

        private Label  _lblHeader;
        private Label  _lblHelp;
        private Panel  _bar;
        private Button _btnSave;
        private Label  _lblStatus;
        private DataGridView _grid;
        private DataGridViewComboBoxColumn _colCtrl;
        private DataGridViewComboBoxColumn _colPage;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._lblHeader = new System.Windows.Forms.Label();
            this._lblHelp = new System.Windows.Forms.Label();
            this._bar = new System.Windows.Forms.Panel();
            this._btnSave = new System.Windows.Forms.Button();
            this._lblStatus = new System.Windows.Forms.Label();
            this._grid = new System.Windows.Forms.DataGridView();
            this._colCtrl = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this._colPage = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this._bar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this.SuspendLayout();
            //
            // _lblHeader
            //
            this._lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._lblHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._lblHeader.ForeColor = System.Drawing.Color.White;
            this._lblHeader.Location = new System.Drawing.Point(0, 88);
            this._lblHeader.Name = "_lblHeader";
            this._lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._lblHeader.Size = new System.Drawing.Size(761, 30);
            this._lblHeader.TabIndex = 0;
            this._lblHeader.Text = "검사 조명 — 컨트롤러/페이지 지정";
            this._lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _lblHelp
            //
            this._lblHelp.BackColor = System.Drawing.Color.WhiteSmoke;
            this._lblHelp.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblHelp.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._lblHelp.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblHelp.Location = new System.Drawing.Point(0, 40);
            this._lblHelp.Name = "_lblHelp";
            this._lblHelp.Padding = new System.Windows.Forms.Padding(10, 2, 0, 0);
            this._lblHelp.Size = new System.Drawing.Size(761, 48);
            this._lblHelp.TabIndex = 1;
            this._lblHelp.Text = "이 검사가 구동하는 컨트롤러/페이지를 지정합니다. 채널 레벨은 [레시피] 화면에서 편집(레벨 0=미사용). 행 추가/삭제로 지정 변경.";
            this._lblHelp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _bar
            //
            this._bar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._bar.Controls.Add(this._btnSave);
            this._bar.Dock = System.Windows.Forms.DockStyle.Top;
            this._bar.Location = new System.Drawing.Point(0, 0);
            this._bar.Name = "_bar";
            this._bar.Size = new System.Drawing.Size(761, 40);
            this._bar.TabIndex = 2;
            //
            // _btnSave
            //
            this._btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSave.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnSave.ForeColor = System.Drawing.Color.White;
            this._btnSave.Location = new System.Drawing.Point(8, 4);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Size = new System.Drawing.Size(90, 32);
            this._btnSave.TabIndex = 0;
            this._btnSave.Text = "저장";
            this._btnSave.UseVisualStyleBackColor = false;
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);
            //
            // _lblStatus
            //
            this._lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._lblStatus.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblStatus.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblStatus.Location = new System.Drawing.Point(0, 442);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Padding = new System.Windows.Forms.Padding(8, 2, 0, 0);
            this._lblStatus.Size = new System.Drawing.Size(761, 24);
            this._lblStatus.TabIndex = 3;
            //
            // _grid
            //
            this._grid.AllowUserToAddRows = true;
            this._grid.AllowUserToDeleteRows = true;
            this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.BackgroundColor = System.Drawing.Color.White;
            this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colCtrl,
            this._colPage});
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._grid.Location = new System.Drawing.Point(0, 118);
            this._grid.Name = "_grid";
            this._grid.RowHeadersVisible = false;
            this._grid.Size = new System.Drawing.Size(761, 324);
            this._grid.TabIndex = 4;
            this._grid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnCellEndEdit);
            this._grid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.OnGridDataError);
            this._grid.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.OnEditingControlShowing);
            this._grid.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.OnRowsRemoved);
            //
            // _colCtrl
            //
            this._colCtrl.FillWeight = 60F;
            this._colCtrl.HeaderText = "컨트롤러(Port)";
            this._colCtrl.Name = "ControllerPort";
            //
            // _colPage
            //
            this._colPage.FillWeight = 40F;
            this._colPage.HeaderText = "페이지";
            this._colPage.Name = "Page";
            //
            // InspectionLightAssignPanel
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this._grid);
            this.Controls.Add(this._lblHeader);
            this.Controls.Add(this._lblHelp);
            this.Controls.Add(this._bar);
            this.Controls.Add(this._lblStatus);
            this.Name = "InspectionLightAssignPanel";
            this.Size = new System.Drawing.Size(761, 466);
            this._bar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
