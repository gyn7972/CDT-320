using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    partial class HelpTextDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private TextBox txtContent;
        private TableLayoutPanel buttonLayout;
        private Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.txtContent = new System.Windows.Forms.TextBox();
            this.buttonLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnClose = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.buttonLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblTitle, 0, 0);
            this.rootLayout.Controls.Add(this.txtContent, 0, 1);
            this.rootLayout.Controls.Add(this.buttonLayout, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(12, 12);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.rootLayout.Size = new System.Drawing.Size(800, 576);
            this.rootLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(800, 46);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "HELP";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtContent
            // 
            this.txtContent.BackColor = System.Drawing.Color.White;
            this.txtContent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtContent.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.txtContent.Location = new System.Drawing.Point(0, 54);
            this.txtContent.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
            this.txtContent.Multiline = true;
            this.txtContent.Name = "txtContent";
            this.txtContent.ReadOnly = true;
            this.txtContent.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtContent.Size = new System.Drawing.Size(800, 456);
            this.txtContent.TabIndex = 1;
            this.txtContent.WordWrap = true;
            // 
            // buttonLayout
            // 
            this.buttonLayout.ColumnCount = 2;
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 132F));
            this.buttonLayout.Controls.Add(this.btnClose, 1, 0);
            this.buttonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonLayout.Location = new System.Drawing.Point(0, 518);
            this.buttonLayout.Margin = new System.Windows.Forms.Padding(0);
            this.buttonLayout.Name = "buttonLayout";
            this.buttonLayout.RowCount = 1;
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonLayout.Size = new System.Drawing.Size(800, 58);
            this.buttonLayout.TabIndex = 2;
            // 
            // btnClose
            // 
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnClose.Location = new System.Drawing.Point(676, 8);
            this.btnClose.Margin = new System.Windows.Forms.Padding(8);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(116, 42);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // HelpTextDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(824, 600);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(720, 520);
            this.Name = "HelpTextDialog";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "HELP";
            this.rootLayout.ResumeLayout(false);
            this.rootLayout.PerformLayout();
            this.buttonLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
