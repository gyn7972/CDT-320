namespace QMC.CDT_320.Ui.Controls
{
    partial class WaferVisionTestControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel commandLayout;
        private System.Windows.Forms.Button btnExpose;
        private System.Windows.Forms.Button btnCenter;
        private System.Windows.Forms.Button btnRef1;
        private System.Windows.Forms.Button btnRef2;
        private System.Windows.Forms.Button btnDieCheck;
        private System.Windows.Forms.Label lblExpose;
        private System.Windows.Forms.Label lblCenter;
        private System.Windows.Forms.Label lblRef1;
        private System.Windows.Forms.Label lblRef2;
        private System.Windows.Forms.Label lblDieCheck;
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
            this.btnCenter = new System.Windows.Forms.Button();
            this.btnRef1 = new System.Windows.Forms.Button();
            this.btnRef2 = new System.Windows.Forms.Button();
            this.btnDieCheck = new System.Windows.Forms.Button();
            this.lblExpose = new System.Windows.Forms.Label();
            this.lblCenter = new System.Windows.Forms.Label();
            this.lblRef1 = new System.Windows.Forms.Label();
            this.lblRef2 = new System.Windows.Forms.Label();
            this.lblDieCheck = new System.Windows.Forms.Label();
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
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 450F));
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
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.commandLayout.Controls.Add(this.btnExpose, 0, 0);
            this.commandLayout.Controls.Add(this.lblExpose, 1, 0);
            this.commandLayout.Controls.Add(this.btnCenter, 0, 1);
            this.commandLayout.Controls.Add(this.lblCenter, 1, 1);
            this.commandLayout.Controls.Add(this.btnRef1, 0, 2);
            this.commandLayout.Controls.Add(this.lblRef1, 1, 2);
            this.commandLayout.Controls.Add(this.btnRef2, 0, 3);
            this.commandLayout.Controls.Add(this.lblRef2, 1, 3);
            this.commandLayout.Controls.Add(this.btnDieCheck, 0, 4);
            this.commandLayout.Controls.Add(this.lblDieCheck, 1, 4);
            this.commandLayout.Controls.Add(this.lblSummary, 0, 5);
            this.commandLayout.Controls.Add(this.lblHint, 0, 6);
            this.commandLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.commandLayout.Location = new System.Drawing.Point(0, 0);
            this.commandLayout.Margin = new System.Windows.Forms.Padding(0);
            this.commandLayout.Name = "commandLayout";
            this.commandLayout.Padding = new System.Windows.Forms.Padding(12);
            this.commandLayout.RowCount = 7;
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.commandLayout.Size = new System.Drawing.Size(450, 600);
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
            this.btnCenter.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCenter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCenter.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this.btnCenter.ForeColor = System.Drawing.Color.White;
            this.btnCenter.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this.btnCenter.Name = "btnCenter";
            this.btnCenter.TabIndex = 2;
            this.btnCenter.Text = "ALIGN: Center";
            this.btnCenter.UseVisualStyleBackColor = false;
            this.btnRef1.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnRef1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRef1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRef1.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this.btnRef1.ForeColor = System.Drawing.Color.White;
            this.btnRef1.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this.btnRef1.Name = "btnRef1";
            this.btnRef1.TabIndex = 4;
            this.btnRef1.Text = "ALIGN: Ref1";
            this.btnRef1.UseVisualStyleBackColor = false;
            this.btnRef2.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnRef2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRef2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRef2.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this.btnRef2.ForeColor = System.Drawing.Color.White;
            this.btnRef2.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this.btnRef2.Name = "btnRef2";
            this.btnRef2.TabIndex = 6;
            this.btnRef2.Text = "ALIGN: Ref2";
            this.btnRef2.UseVisualStyleBackColor = false;
            this.btnDieCheck.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnDieCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDieCheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDieCheck.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this.btnDieCheck.ForeColor = System.Drawing.Color.White;
            this.btnDieCheck.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this.btnDieCheck.Name = "btnDieCheck";
            this.btnDieCheck.TabIndex = 8;
            this.btnDieCheck.Text = "DIE CHECK";
            this.btnDieCheck.UseVisualStyleBackColor = false;
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
            this.lblCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCenter.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCenter.ForeColor = System.Drawing.Color.DimGray;
            this.lblCenter.Name = "lblCenter";
            this.lblCenter.TabIndex = 3;
            this.lblCenter.Text = "센터 마크 정렬 대기";
            this.lblCenter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblRef1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRef1.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblRef1.ForeColor = System.Drawing.Color.DimGray;
            this.lblRef1.Name = "lblRef1";
            this.lblRef1.TabIndex = 5;
            this.lblRef1.Text = "기준1 마크 정렬 대기";
            this.lblRef1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblRef2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRef2.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblRef2.ForeColor = System.Drawing.Color.DimGray;
            this.lblRef2.Name = "lblRef2";
            this.lblRef2.TabIndex = 7;
            this.lblRef2.Text = "기준2 마크 정렬 대기";
            this.lblRef2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblDieCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieCheck.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblDieCheck.ForeColor = System.Drawing.Color.DimGray;
            this.lblDieCheck.Name = "lblDieCheck";
            this.lblDieCheck.TabIndex = 9;
            this.lblDieCheck.Text = "Die 검사 OK/NG 대기";
            this.lblDieCheck.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSummary.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSummary.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            this.lblSummary.ForeColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblSummary.TabIndex = 10;
            this.lblSummary.Text = "저장된 결과 없음";
            this.lblSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblHint.Name = "lblHint";
            this.lblHint.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblHint.TabIndex = 11;
            this.lblHint.Text = "Wafer Vision 통신 테스트입니다. 모션 구동은 하지 않습니다.";
            // 
            // viewer
            // 
            this.viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.viewer.Location = new System.Drawing.Point(450, 0);
            this.viewer.Margin = new System.Windows.Forms.Padding(0);
            this.viewer.Name = "viewer";
            this.viewer.Size = new System.Drawing.Size(630, 600);
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
