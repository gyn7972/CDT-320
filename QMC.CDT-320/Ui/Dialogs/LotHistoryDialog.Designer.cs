namespace QMC.CDT_320.Ui.Dialogs
{
    partial class LotHistoryDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblActiveLot;
        private System.Windows.Forms.DataGridView gridLots;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLotId;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLotState;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLotRecipe;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLotStarted;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLotFinished;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLotProcessed;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLotGood;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLotNg;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLotYield;
        private System.Windows.Forms.TableLayoutPanel bottomLayout;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblActiveLot = new System.Windows.Forms.Label();
            this.gridLots = new System.Windows.Forms.DataGridView();
            this.colLotId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLotState = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLotRecipe = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLotStarted = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLotFinished = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLotProcessed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLotGood = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLotNg = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLotYield = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bottomLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHint = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLots)).BeginInit();
            this.bottomLayout.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.BackColor = System.Drawing.Color.White;
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblActiveLot, 0, 0);
            this.rootLayout.Controls.Add(this.gridLots, 0, 1);
            this.rootLayout.Controls.Add(this.bottomLayout, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(10);
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.rootLayout.Size = new System.Drawing.Size(1020, 560);
            this.rootLayout.TabIndex = 0;
            //
            // lblActiveLot
            //
            this.lblActiveLot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblActiveLot.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblActiveLot.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.lblActiveLot.Name = "lblActiveLot";
            this.lblActiveLot.TabIndex = 0;
            this.lblActiveLot.Text = "진행 중인 LOT: -";
            this.lblActiveLot.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // gridLots
            //
            this.gridLots.AllowUserToAddRows = false;
            this.gridLots.AllowUserToDeleteRows = false;
            this.gridLots.AllowUserToResizeRows = false;
            this.gridLots.BackgroundColor = System.Drawing.Color.White;
            this.gridLots.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.gridLots.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridLots.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colLotId,
            this.colLotState,
            this.colLotRecipe,
            this.colLotStarted,
            this.colLotFinished,
            this.colLotProcessed,
            this.colLotGood,
            this.colLotNg,
            this.colLotYield});
            this.gridLots.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridLots.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.gridLots.Font = new System.Drawing.Font("맑은 고딕", 9.5F);
            this.gridLots.Margin = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.gridLots.MultiSelect = false;
            this.gridLots.Name = "gridLots";
            this.gridLots.ReadOnly = true;
            this.gridLots.RowHeadersVisible = false;
            this.gridLots.RowTemplate.Height = 26;
            this.gridLots.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridLots.TabIndex = 1;
            //
            // LOT 이력 컬럼
            //
            this.colLotId.HeaderText = "LOT ID";
            this.colLotId.Name = "colLotId";
            this.colLotId.ReadOnly = true;
            this.colLotId.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLotId.FillWeight = 170F;
            this.colLotState.HeaderText = "상태";
            this.colLotState.Name = "colLotState";
            this.colLotState.ReadOnly = true;
            this.colLotState.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLotState.FillWeight = 70F;
            this.colLotRecipe.HeaderText = "레시피";
            this.colLotRecipe.Name = "colLotRecipe";
            this.colLotRecipe.ReadOnly = true;
            this.colLotRecipe.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLotRecipe.FillWeight = 150F;
            this.colLotStarted.HeaderText = "시작";
            this.colLotStarted.Name = "colLotStarted";
            this.colLotStarted.ReadOnly = true;
            this.colLotStarted.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLotStarted.FillWeight = 130F;
            this.colLotFinished.HeaderText = "종료";
            this.colLotFinished.Name = "colLotFinished";
            this.colLotFinished.ReadOnly = true;
            this.colLotFinished.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLotFinished.FillWeight = 130F;
            this.colLotProcessed.HeaderText = "처리";
            this.colLotProcessed.Name = "colLotProcessed";
            this.colLotProcessed.ReadOnly = true;
            this.colLotProcessed.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLotProcessed.FillWeight = 70F;
            this.colLotGood.HeaderText = "GOOD";
            this.colLotGood.Name = "colLotGood";
            this.colLotGood.ReadOnly = true;
            this.colLotGood.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLotGood.FillWeight = 70F;
            this.colLotNg.HeaderText = "NG";
            this.colLotNg.Name = "colLotNg";
            this.colLotNg.ReadOnly = true;
            this.colLotNg.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLotNg.FillWeight = 60F;
            this.colLotYield.HeaderText = "수율(%)";
            this.colLotYield.Name = "colLotYield";
            this.colLotYield.ReadOnly = true;
            this.colLotYield.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLotYield.FillWeight = 80F;
            //
            // bottomLayout
            //
            this.bottomLayout.ColumnCount = 3;
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.bottomLayout.Controls.Add(this.lblHint, 0, 0);
            this.bottomLayout.Controls.Add(this.btnRefresh, 1, 0);
            this.bottomLayout.Controls.Add(this.btnClose, 2, 0);
            this.bottomLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomLayout.Margin = new System.Windows.Forms.Padding(0);
            this.bottomLayout.Name = "bottomLayout";
            this.bottomLayout.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.bottomLayout.RowCount = 1;
            this.bottomLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomLayout.TabIndex = 2;
            //
            // lblHint
            //
            this.lblHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(110)))), ((int)(((byte)(110)))));
            this.lblHint.Name = "lblHint";
            this.lblHint.TabIndex = 0;
            this.lblHint.Text = "이력은 Log\\Lots 폴더에 LOT 별 JSON 파일로 저장되며 프로그램을 재시작해도 유지됩니다.";
            this.lblHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // btnRefresh
            //
            this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "새로고침";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            //
            // btnClose
            //
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnClose.Margin = new System.Windows.Forms.Padding(3);
            this.btnClose.Name = "btnClose";
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "닫기";
            this.btnClose.UseVisualStyleBackColor = true;
            //
            // LotHistoryDialog
            //
            this.AcceptButton = this.btnClose;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1020, 560);
            this.Controls.Add(this.rootLayout);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(880, 420);
            this.Name = "LotHistoryDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "LOT 진행 이력";
            this.rootLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridLots)).EndInit();
            this.bottomLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
