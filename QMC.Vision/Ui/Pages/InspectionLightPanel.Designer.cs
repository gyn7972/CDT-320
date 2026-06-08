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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this._lblHeader = new System.Windows.Forms.Label();
            this._lblWiring = new System.Windows.Forms.Label();
            this._bar = new System.Windows.Forms.Panel();
            this._btnSave = new System.Windows.Forms.Button();
            this._btnApply = new System.Windows.Forms.Button();
            this._btnReset = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._lblStatus = new System.Windows.Forms.Label();
            this._grid = new System.Windows.Forms.DataGridView();
            this.Ctrl = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Channel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Level = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Page = new System.Windows.Forms.DataGridViewComboBoxColumn();
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
            this._lblHeader.Text = "검사 조명";
            this._lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblWiring
            // 
            this._lblWiring.BackColor = System.Drawing.Color.WhiteSmoke;
            this._lblWiring.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblWiring.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblWiring.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblWiring.Location = new System.Drawing.Point(0, 40);
            this._lblWiring.Name = "_lblWiring";
            this._lblWiring.Padding = new System.Windows.Forms.Padding(10, 2, 0, 0);
            this._lblWiring.Size = new System.Drawing.Size(761, 48);
            this._lblWiring.TabIndex = 1;
            this._lblWiring.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _bar
            // 
            this._bar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._bar.Controls.Add(this._btnSave);
            this._bar.Controls.Add(this._btnApply);
            this._bar.Controls.Add(this._btnReset);
            this._bar.Controls.Add(this._btnCancel);
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
            // _btnApply
            // 
            this._btnApply.BackColor = System.Drawing.Color.White;
            this._btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnApply.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnApply.ForeColor = System.Drawing.Color.Black;
            this._btnApply.Location = new System.Drawing.Point(104, 4);
            this._btnApply.Name = "_btnApply";
            this._btnApply.Size = new System.Drawing.Size(90, 32);
            this._btnApply.TabIndex = 1;
            this._btnApply.Text = "실행 적용";
            this._btnApply.UseVisualStyleBackColor = false;
            this._btnApply.Click += new System.EventHandler(this.OnApplyClick);
            // 
            // _btnReset
            // 
            this._btnReset.BackColor = System.Drawing.Color.White;
            this._btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnReset.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnReset.ForeColor = System.Drawing.Color.Black;
            this._btnReset.Location = new System.Drawing.Point(200, 4);
            this._btnReset.Name = "_btnReset";
            this._btnReset.Size = new System.Drawing.Size(90, 32);
            this._btnReset.TabIndex = 2;
            this._btnReset.Text = "초기화";
            this._btnReset.UseVisualStyleBackColor = false;
            this._btnReset.Click += new System.EventHandler(this.OnResetClick);
            // 
            // _btnCancel
            // 
            this._btnCancel.BackColor = System.Drawing.Color.White;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCancel.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnCancel.ForeColor = System.Drawing.Color.Black;
            this._btnCancel.Location = new System.Drawing.Point(296, 4);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(90, 32);
            this._btnCancel.TabIndex = 3;
            this._btnCancel.Text = "취소";
            this._btnCancel.UseVisualStyleBackColor = false;
            this._btnCancel.Click += new System.EventHandler(this.OnCancelClick);
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
            this._grid.AllowUserToAddRows = false;
            this._grid.AllowUserToDeleteRows = false;
            this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.BackgroundColor = System.Drawing.Color.White;
            this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Ctrl,
            this.Channel,
            this.Name,
            this.Level,
            this.Page});
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.Font = new System.Drawing.Font("Consolas", 10F);
            this._grid.Location = new System.Drawing.Point(0, 118);
            this._grid.Name = "_grid";
            this._grid.RowHeadersVisible = false;
            this._grid.Size = new System.Drawing.Size(761, 324);
            this._grid.TabIndex = 4;
            this._grid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.OnGridDataError);
            // 
            // Ctrl
            // 
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(242)))), ((int)(((byte)(242)))));
            this.Ctrl.DefaultCellStyle = dataGridViewCellStyle1;
            this.Ctrl.FillWeight = 24F;
            this.Ctrl.HeaderText = "컨트롤러";
            this.Ctrl.Name = "Ctrl";
            this.Ctrl.ReadOnly = true;
            // 
            // Channel
            // 
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(242)))), ((int)(((byte)(242)))));
            this.Channel.DefaultCellStyle = dataGridViewCellStyle2;
            this.Channel.FillWeight = 10F;
            this.Channel.HeaderText = "Ch";
            this.Channel.Name = "Channel";
            this.Channel.ReadOnly = true;
            // 
            // Name
            // 
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(242)))), ((int)(((byte)(242)))));
            this.Name.DefaultCellStyle = dataGridViewCellStyle3;
            this.Name.FillWeight = 30F;
            this.Name.HeaderText = "이름";
            this.Name.Name = "Name";
            this.Name.ReadOnly = true;
            // 
            // Level
            // 
            this.Level.FillWeight = 18F;
            this.Level.HeaderText = "Level";
            this.Level.Name = "Level";
            // 
            // Page
            // 
            this.Page.FillWeight = 12F;
            this.Page.HeaderText = "Page";
            this.Page.Name = "Page";
            // 
            // InspectionLightPanel
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this._grid);
            this.Controls.Add(this._lblHeader);
            this.Controls.Add(this._lblWiring);
            this.Controls.Add(this._bar);
            this.Controls.Add(this._lblStatus);
            this.Name = "InspectionLightPanel";
            this.Size = new System.Drawing.Size(761, 466);
            this._bar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this.ResumeLayout(false);

        }

        private DataGridViewTextBoxColumn Ctrl;
        private DataGridViewTextBoxColumn Channel;
        private DataGridViewTextBoxColumn Name;
        private DataGridViewTextBoxColumn Level;
        private DataGridViewComboBoxColumn Page;
    }
}
