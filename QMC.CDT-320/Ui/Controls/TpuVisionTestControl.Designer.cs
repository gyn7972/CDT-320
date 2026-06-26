namespace QMC.CDT_320.Ui.Controls
{
    partial class TpuVisionTestControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel commandLayout;
        private System.Windows.Forms.Button btnExpose;
        private System.Windows.Forms.Button btnResult;
        private System.Windows.Forms.Label lblExpose;
        private System.Windows.Forms.Label lblResult;
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
            this.btnResult = new System.Windows.Forms.Button();
            this.lblExpose = new System.Windows.Forms.Label();
            this.lblResult = new System.Windows.Forms.Label();
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
            this.rootLayout.Size = new System.Drawing.Size(1080, 560);
            this.rootLayout.TabIndex = 0;
            // 
            // commandLayout
            // 
            this.commandLayout.ColumnCount = 2;
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.commandLayout.Controls.Add(this.btnExpose, 0, 0);
            this.commandLayout.Controls.Add(this.lblExpose, 1, 0);
            this.commandLayout.Controls.Add(this.btnResult, 0, 1);
            this.commandLayout.Controls.Add(this.lblResult, 1, 1);
            this.commandLayout.Controls.Add(this.lblHint, 0, 3);
            this.commandLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.commandLayout.Location = new System.Drawing.Point(0, 0);
            this.commandLayout.Margin = new System.Windows.Forms.Padding(0);
            this.commandLayout.Name = "commandLayout";
            this.commandLayout.Padding = new System.Windows.Forms.Padding(12);
            this.commandLayout.RowCount = 4;
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.commandLayout.Size = new System.Drawing.Size(450, 560);
            this.commandLayout.TabIndex = 0;
            this.commandLayout.SetRowSpan(this.lblResult, 2);
            this.commandLayout.SetColumnSpan(this.lblHint, 2);
            // 
            // btnExpose
            // 
            this.btnExpose.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnExpose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExpose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExpose.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this.btnExpose.ForeColor = System.Drawing.Color.White;
            this.btnExpose.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this.btnExpose.Name = "btnExpose";
            this.btnExpose.TabIndex = 0;
            this.btnExpose.Text = "EXPOSE";
            this.btnExpose.UseVisualStyleBackColor = false;
            // 
            // btnResult
            // 
            this.btnResult.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnResult.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResult.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this.btnResult.ForeColor = System.Drawing.Color.White;
            this.btnResult.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this.btnResult.Name = "btnResult";
            this.btnResult.TabIndex = 2;
            this.btnResult.Text = "RESULT";
            this.btnResult.UseVisualStyleBackColor = false;
            // 
            // lblExpose
            // 
            this.lblExpose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExpose.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblExpose.ForeColor = System.Drawing.Color.DimGray;
            this.lblExpose.Name = "lblExpose";
            this.lblExpose.TabIndex = 1;
            this.lblExpose.Text = "EXPOSE ACK 대기";
            this.lblExpose.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblResult
            // 
            this.lblResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblResult.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblResult.ForeColor = System.Drawing.Color.DimGray;
            this.lblResult.Name = "lblResult";
            this.lblResult.TabIndex = 3;
            this.lblResult.Text = "결과 대기";
            this.lblResult.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // lblHint
            // 
            this.lblHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblHint.Name = "lblHint";
            this.lblHint.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblHint.TabIndex = 4;
            this.lblHint.Text = "Vision 테스트 버튼입니다. 모션 구동은 하지 않습니다.";
            // 
            // viewer
            // 
            this.viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.viewer.Location = new System.Drawing.Point(450, 0);
            this.viewer.Margin = new System.Windows.Forms.Padding(0);
            this.viewer.Name = "viewer";
            this.viewer.Size = new System.Drawing.Size(630, 560);
            this.viewer.TabIndex = 1;
            // 
            // TpuVisionTestControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.Name = "TpuVisionTestControl";
            this.Size = new System.Drawing.Size(1080, 560);
            this.rootLayout.ResumeLayout(false);
            this.commandLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
