namespace QMC.CDT_320.Ui.Controls
{
    partial class CassetteSlotView
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label summaryLabel;
        private System.Windows.Forms.Panel scrollPanel;
        private System.Windows.Forms.TableLayoutPanel slotLayout;

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && (components != null))
                    components.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.summaryLabel = new System.Windows.Forms.Label();
            this.scrollPanel = new System.Windows.Forms.Panel();
            this.slotLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rootLayout.SuspendLayout();
            this.scrollPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.rootLayout.Controls.Add(this.titleLabel);
            this.rootLayout.Controls.Add(this.summaryLabel);
            this.rootLayout.Controls.Add(this.scrollPanel);
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.rootLayout.Size = new System.Drawing.Size(200, 100);
            this.rootLayout.TabIndex = 0;
            // 
            // titleLabel
            // 
            this.titleLabel.Location = new System.Drawing.Point(3, 0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(100, 20);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Title";
            // 
            // summaryLabel
            // 
            this.summaryLabel.Location = new System.Drawing.Point(3, 20);
            this.summaryLabel.Name = "summaryLabel";
            this.summaryLabel.Size = new System.Drawing.Size(100, 20);
            this.summaryLabel.TabIndex = 1;
            this.summaryLabel.Text = "summary";
            // 
            // scrollPanel
            // 
            this.scrollPanel.Controls.Add(this.slotLayout);
            this.scrollPanel.Location = new System.Drawing.Point(3, 43);
            this.scrollPanel.Name = "scrollPanel";
            this.scrollPanel.Size = new System.Drawing.Size(194, 54);
            this.scrollPanel.TabIndex = 2;
            // 
            // slotLayout
            // 
            this.slotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.slotLayout.Location = new System.Drawing.Point(0, 0);
            this.slotLayout.Name = "slotLayout";
            this.slotLayout.Size = new System.Drawing.Size(203, 77);
            this.slotLayout.TabIndex = 0;
            // 
            // CassetteSlotView
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "CassetteSlotView";
            this.Size = new System.Drawing.Size(209, 123);
            this.rootLayout.ResumeLayout(false);
            this.scrollPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
