namespace QMC.CDT_320.Ui.Controls
{
    partial class WaferVisionTestControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel commandLayout;
        private System.Windows.Forms.Button btnExpose;
        private System.Windows.Forms.Button btnCenterMatchAsync;
        private System.Windows.Forms.Button btnCenterMatchResult;
        private System.Windows.Forms.Button btnRef1MatchAsync;
        private System.Windows.Forms.Button btnRef1MatchResult;
        private System.Windows.Forms.Button btnRef2MatchAsync;
        private System.Windows.Forms.Button btnRef2MatchResult;
        private System.Windows.Forms.Button btnDieCheckMatchAsync;
        private System.Windows.Forms.Button btnDieCheckMatchResult;
        private System.Windows.Forms.Label lblExpose;
        private System.Windows.Forms.Label lblCenterMatchAsync;
        private System.Windows.Forms.Label lblCenterMatchResult;
        private System.Windows.Forms.Label lblRef1MatchAsync;
        private System.Windows.Forms.Label lblRef1MatchResult;
        private System.Windows.Forms.Label lblRef2MatchAsync;
        private System.Windows.Forms.Label lblRef2MatchResult;
        private System.Windows.Forms.Label lblDieCheckMatchAsync;
        private System.Windows.Forms.Label lblDieCheckMatchResult;
        private System.Windows.Forms.Label lblSummary;
        private System.Windows.Forms.Label lblHint;
        private QMC.CDT_320.Ui.Controls.VisionViewerPanel viewer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.commandLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnExpose = new System.Windows.Forms.Button();
            this.btnCenterMatchAsync = new System.Windows.Forms.Button();
            this.btnCenterMatchResult = new System.Windows.Forms.Button();
            this.btnRef1MatchAsync = new System.Windows.Forms.Button();
            this.btnRef1MatchResult = new System.Windows.Forms.Button();
            this.btnRef2MatchAsync = new System.Windows.Forms.Button();
            this.btnRef2MatchResult = new System.Windows.Forms.Button();
            this.btnDieCheckMatchAsync = new System.Windows.Forms.Button();
            this.btnDieCheckMatchResult = new System.Windows.Forms.Button();
            this.lblExpose = new System.Windows.Forms.Label();
            this.lblCenterMatchAsync = new System.Windows.Forms.Label();
            this.lblCenterMatchResult = new System.Windows.Forms.Label();
            this.lblRef1MatchAsync = new System.Windows.Forms.Label();
            this.lblRef1MatchResult = new System.Windows.Forms.Label();
            this.lblRef2MatchAsync = new System.Windows.Forms.Label();
            this.lblRef2MatchResult = new System.Windows.Forms.Label();
            this.lblDieCheckMatchAsync = new System.Windows.Forms.Label();
            this.lblDieCheckMatchResult = new System.Windows.Forms.Label();
            this.lblSummary = new System.Windows.Forms.Label();
            this.lblHint = new System.Windows.Forms.Label();
            this.viewer = new QMC.CDT_320.Ui.Controls.VisionViewerPanel();
            this.rootLayout.SuspendLayout();
            this.commandLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 520F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.commandLayout, 0, 0);
            this.rootLayout.Controls.Add(this.viewer, 1, 0);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 1;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1080, 600);
            this.rootLayout.TabIndex = 0;
            // 
            // commandLayout
            // 
            this.commandLayout.ColumnCount = 2;
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 225F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.commandLayout.Controls.Add(this.btnExpose, 0, 0);
            this.commandLayout.Controls.Add(this.lblExpose, 1, 0);
            this.commandLayout.Controls.Add(this.btnCenterMatchAsync, 0, 1);
            this.commandLayout.Controls.Add(this.lblCenterMatchAsync, 1, 1);
            this.commandLayout.Controls.Add(this.btnCenterMatchResult, 0, 2);
            this.commandLayout.Controls.Add(this.lblCenterMatchResult, 1, 2);
            this.commandLayout.Controls.Add(this.btnRef1MatchAsync, 0, 3);
            this.commandLayout.Controls.Add(this.lblRef1MatchAsync, 1, 3);
            this.commandLayout.Controls.Add(this.btnRef1MatchResult, 0, 4);
            this.commandLayout.Controls.Add(this.lblRef1MatchResult, 1, 4);
            this.commandLayout.Controls.Add(this.btnRef2MatchAsync, 0, 5);
            this.commandLayout.Controls.Add(this.lblRef2MatchAsync, 1, 5);
            this.commandLayout.Controls.Add(this.btnRef2MatchResult, 0, 6);
            this.commandLayout.Controls.Add(this.lblRef2MatchResult, 1, 6);
            this.commandLayout.Controls.Add(this.btnDieCheckMatchAsync, 0, 7);
            this.commandLayout.Controls.Add(this.lblDieCheckMatchAsync, 1, 7);
            this.commandLayout.Controls.Add(this.btnDieCheckMatchResult, 0, 8);
            this.commandLayout.Controls.Add(this.lblDieCheckMatchResult, 1, 8);
            this.commandLayout.Controls.Add(this.lblSummary, 0, 9);
            this.commandLayout.Controls.Add(this.lblHint, 0, 10);
            this.commandLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.commandLayout.Location = new System.Drawing.Point(0, 0);
            this.commandLayout.Margin = new System.Windows.Forms.Padding(0);
            this.commandLayout.Name = "commandLayout";
            this.commandLayout.Padding = new System.Windows.Forms.Padding(12);
            this.commandLayout.RowCount = 11;
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.commandLayout.Size = new System.Drawing.Size(520, 600);
            this.commandLayout.TabIndex = 0;
            this.commandLayout.SetColumnSpan(this.lblSummary, 2);
            this.commandLayout.SetColumnSpan(this.lblHint, 2);
            // 
            // buttons
            // 
            this.btnExpose.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnExpose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExpose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExpose.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this.btnExpose.ForeColor = System.Drawing.Color.White;
            this.btnExpose.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this.btnExpose.Name = "btnExpose";
            this.btnExpose.TabIndex = 0;
            this.btnExpose.Text = "GRAB";
            this.btnExpose.UseVisualStyleBackColor = false;
            this.btnExpose.Click += new System.EventHandler(this.btnExpose_Click);
            this.btnCenterMatchAsync.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnCenterMatchAsync.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCenterMatchAsync.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCenterMatchAsync.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnCenterMatchAsync.ForeColor = System.Drawing.Color.White;
            this.btnCenterMatchAsync.Margin = new System.Windows.Forms.Padding(3, 5, 12, 5);
            this.btnCenterMatchAsync.Name = "btnCenterMatchAsync";
            this.btnCenterMatchAsync.TabIndex = 2;
            this.btnCenterMatchAsync.Text = "ALIGN: Center INSPECT_SYNC 확인";
            this.btnCenterMatchAsync.UseVisualStyleBackColor = false;
            this.btnCenterMatchAsync.Click += new System.EventHandler(this.btnCenterMatchAsync_Click);
            this.btnCenterMatchResult.BackColor = System.Drawing.Color.FromArgb(82, 82, 82);
            this.btnCenterMatchResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCenterMatchResult.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCenterMatchResult.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnCenterMatchResult.ForeColor = System.Drawing.Color.White;
            this.btnCenterMatchResult.Margin = new System.Windows.Forms.Padding(3, 5, 12, 5);
            this.btnCenterMatchResult.Name = "btnCenterMatchResult";
            this.btnCenterMatchResult.TabIndex = 4;
            this.btnCenterMatchResult.Text = "ALIGN: Center INSPECT_SYNC + 적용";
            this.btnCenterMatchResult.UseVisualStyleBackColor = false;
            this.btnCenterMatchResult.Click += new System.EventHandler(this.btnCenterMatchResult_Click);
            this.btnRef1MatchAsync.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnRef1MatchAsync.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRef1MatchAsync.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRef1MatchAsync.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnRef1MatchAsync.ForeColor = System.Drawing.Color.White;
            this.btnRef1MatchAsync.Margin = new System.Windows.Forms.Padding(3, 5, 12, 5);
            this.btnRef1MatchAsync.Name = "btnRef1MatchAsync";
            this.btnRef1MatchAsync.TabIndex = 6;
            this.btnRef1MatchAsync.Text = "ALIGN: Ref1 INSPECT_SYNC 확인";
            this.btnRef1MatchAsync.UseVisualStyleBackColor = false;
            this.btnRef1MatchAsync.Click += new System.EventHandler(this.btnRef1MatchAsync_Click);
            this.btnRef1MatchResult.BackColor = System.Drawing.Color.FromArgb(82, 82, 82);
            this.btnRef1MatchResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRef1MatchResult.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRef1MatchResult.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnRef1MatchResult.ForeColor = System.Drawing.Color.White;
            this.btnRef1MatchResult.Margin = new System.Windows.Forms.Padding(3, 5, 12, 5);
            this.btnRef1MatchResult.Name = "btnRef1MatchResult";
            this.btnRef1MatchResult.TabIndex = 8;
            this.btnRef1MatchResult.Text = "ALIGN: Ref1 INSPECT_SYNC + 적용";
            this.btnRef1MatchResult.UseVisualStyleBackColor = false;
            this.btnRef1MatchResult.Click += new System.EventHandler(this.btnRef1MatchResult_Click);
            this.btnRef2MatchAsync.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnRef2MatchAsync.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRef2MatchAsync.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRef2MatchAsync.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnRef2MatchAsync.ForeColor = System.Drawing.Color.White;
            this.btnRef2MatchAsync.Margin = new System.Windows.Forms.Padding(3, 5, 12, 5);
            this.btnRef2MatchAsync.Name = "btnRef2MatchAsync";
            this.btnRef2MatchAsync.TabIndex = 10;
            this.btnRef2MatchAsync.Text = "ALIGN: Ref2 INSPECT_SYNC 확인";
            this.btnRef2MatchAsync.UseVisualStyleBackColor = false;
            this.btnRef2MatchAsync.Click += new System.EventHandler(this.btnRef2MatchAsync_Click);
            this.btnRef2MatchResult.BackColor = System.Drawing.Color.FromArgb(82, 82, 82);
            this.btnRef2MatchResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRef2MatchResult.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRef2MatchResult.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnRef2MatchResult.ForeColor = System.Drawing.Color.White;
            this.btnRef2MatchResult.Margin = new System.Windows.Forms.Padding(3, 5, 12, 5);
            this.btnRef2MatchResult.Name = "btnRef2MatchResult";
            this.btnRef2MatchResult.TabIndex = 12;
            this.btnRef2MatchResult.Text = "ALIGN: Ref2 INSPECT_SYNC + 적용";
            this.btnRef2MatchResult.UseVisualStyleBackColor = false;
            this.btnRef2MatchResult.Click += new System.EventHandler(this.btnRef2MatchResult_Click);
            this.btnDieCheckMatchAsync.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnDieCheckMatchAsync.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDieCheckMatchAsync.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDieCheckMatchAsync.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnDieCheckMatchAsync.ForeColor = System.Drawing.Color.White;
            this.btnDieCheckMatchAsync.Margin = new System.Windows.Forms.Padding(3, 5, 12, 5);
            this.btnDieCheckMatchAsync.Name = "btnDieCheckMatchAsync";
            this.btnDieCheckMatchAsync.TabIndex = 14;
            this.btnDieCheckMatchAsync.Text = "DIE CHECK INSPECT_SYNC 확인";
            this.btnDieCheckMatchAsync.UseVisualStyleBackColor = false;
            this.btnDieCheckMatchAsync.Click += new System.EventHandler(this.btnDieCheckMatchAsync_Click);
            this.btnDieCheckMatchResult.BackColor = System.Drawing.Color.FromArgb(82, 82, 82);
            this.btnDieCheckMatchResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDieCheckMatchResult.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDieCheckMatchResult.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnDieCheckMatchResult.ForeColor = System.Drawing.Color.White;
            this.btnDieCheckMatchResult.Margin = new System.Windows.Forms.Padding(3, 5, 12, 5);
            this.btnDieCheckMatchResult.Name = "btnDieCheckMatchResult";
            this.btnDieCheckMatchResult.TabIndex = 16;
            this.btnDieCheckMatchResult.Text = "DIE CHECK INSPECT_SYNC + 판정";
            this.btnDieCheckMatchResult.UseVisualStyleBackColor = false;
            this.btnDieCheckMatchResult.Click += new System.EventHandler(this.btnDieCheckMatchResult_Click);
            // 
            // labels
            // 
            this.lblExpose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExpose.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblExpose.ForeColor = System.Drawing.Color.DimGray;
            this.lblExpose.Name = "lblExpose";
            this.lblExpose.TabIndex = 1;
            this.lblExpose.Text = "노출 트리거 ACK 대기";
            this.lblExpose.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblCenterMatchAsync.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCenterMatchAsync.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblCenterMatchAsync.ForeColor = System.Drawing.Color.DimGray;
            this.lblCenterMatchAsync.Name = "lblCenterMatchAsync";
            this.lblCenterMatchAsync.TabIndex = 3;
            this.lblCenterMatchAsync.Text = "INSPECT_SYNC 대기";
            this.lblCenterMatchAsync.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblCenterMatchResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCenterMatchResult.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblCenterMatchResult.ForeColor = System.Drawing.Color.DimGray;
            this.lblCenterMatchResult.Name = "lblCenterMatchResult";
            this.lblCenterMatchResult.TabIndex = 5;
            this.lblCenterMatchResult.Text = "INSPECT_SYNC + 적용 대기";
            this.lblCenterMatchResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblRef1MatchAsync.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRef1MatchAsync.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblRef1MatchAsync.ForeColor = System.Drawing.Color.DimGray;
            this.lblRef1MatchAsync.Name = "lblRef1MatchAsync";
            this.lblRef1MatchAsync.TabIndex = 7;
            this.lblRef1MatchAsync.Text = "INSPECT_SYNC 대기";
            this.lblRef1MatchAsync.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblRef1MatchResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRef1MatchResult.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblRef1MatchResult.ForeColor = System.Drawing.Color.DimGray;
            this.lblRef1MatchResult.Name = "lblRef1MatchResult";
            this.lblRef1MatchResult.TabIndex = 9;
            this.lblRef1MatchResult.Text = "INSPECT_SYNC + 적용 대기";
            this.lblRef1MatchResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblRef2MatchAsync.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRef2MatchAsync.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblRef2MatchAsync.ForeColor = System.Drawing.Color.DimGray;
            this.lblRef2MatchAsync.Name = "lblRef2MatchAsync";
            this.lblRef2MatchAsync.TabIndex = 11;
            this.lblRef2MatchAsync.Text = "INSPECT_SYNC 대기";
            this.lblRef2MatchAsync.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblRef2MatchResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRef2MatchResult.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblRef2MatchResult.ForeColor = System.Drawing.Color.DimGray;
            this.lblRef2MatchResult.Name = "lblRef2MatchResult";
            this.lblRef2MatchResult.TabIndex = 13;
            this.lblRef2MatchResult.Text = "INSPECT_SYNC + 적용 대기";
            this.lblRef2MatchResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblDieCheckMatchAsync.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieCheckMatchAsync.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblDieCheckMatchAsync.ForeColor = System.Drawing.Color.DimGray;
            this.lblDieCheckMatchAsync.Name = "lblDieCheckMatchAsync";
            this.lblDieCheckMatchAsync.TabIndex = 15;
            this.lblDieCheckMatchAsync.Text = "INSPECT_SYNC 대기";
            this.lblDieCheckMatchAsync.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblDieCheckMatchResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieCheckMatchResult.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblDieCheckMatchResult.ForeColor = System.Drawing.Color.DimGray;
            this.lblDieCheckMatchResult.Name = "lblDieCheckMatchResult";
            this.lblDieCheckMatchResult.TabIndex = 17;
            this.lblDieCheckMatchResult.Text = "INSPECT_SYNC + 판정 대기";
            this.lblDieCheckMatchResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSummary.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSummary.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            this.lblSummary.ForeColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblSummary.TabIndex = 18;
            this.lblSummary.Text = "저장된 결과 없음";
            this.lblSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblHint.Name = "lblHint";
            this.lblHint.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblHint.TabIndex = 19;
            this.lblHint.Text = "모든 버튼은 INSPECT_SYNC 요청 후 request_id EPD와 RESULT까지 확인합니다. 적용/판정 버튼은 결과를 Handler 상태에도 반영하며 전체 택을 표시합니다.";
            // 
            // viewer
            // 
            this.viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.viewer.Location = new System.Drawing.Point(520, 0);
            this.viewer.Margin = new System.Windows.Forms.Padding(0);
            this.viewer.Name = "viewer";
            this.viewer.Size = new System.Drawing.Size(560, 600);
            this.viewer.TabIndex = 1;
            // 
            // WaferVisionTestControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.Name = "WaferVisionTestControl";
            this.Size = new System.Drawing.Size(1080, 600);
            this.rootLayout.ResumeLayout(false);
            this.commandLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
