using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    partial class MaterialValueEditDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private Label lblFieldTitle;
        private Label lblFieldValue;
        private TextBox txtValue;
        private FlowLayoutPanel buttonPanel;
        private Button btnOk;
        private Button btnCancel;

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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblFieldTitle = new System.Windows.Forms.Label();
            this.lblFieldValue = new System.Windows.Forms.Label();
            this.txtValue = new System.Windows.Forms.TextBox();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblTitle, 0, 0);
            this.rootLayout.Controls.Add(this.lblFieldTitle, 0, 1);
            this.rootLayout.Controls.Add(this.lblFieldValue, 1, 1);
            this.rootLayout.Controls.Add(this.txtValue, 0, 2);
            this.rootLayout.Controls.Add(this.buttonPanel, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(12);
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(420, 210);
            this.rootLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.rootLayout.SetColumnSpan(this.lblTitle, 2);
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(15, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(390, 42);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "MATERIAL VALUE EDIT";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblFieldTitle
            // 
            this.lblFieldTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFieldTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblFieldTitle.Location = new System.Drawing.Point(15, 54);
            this.lblFieldTitle.Name = "lblFieldTitle";
            this.lblFieldTitle.Size = new System.Drawing.Size(104, 34);
            this.lblFieldTitle.TabIndex = 1;
            this.lblFieldTitle.Text = "Field";
            this.lblFieldTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblFieldValue
            // 
            this.lblFieldValue.BackColor = System.Drawing.Color.White;
            this.lblFieldValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFieldValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFieldValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblFieldValue.Location = new System.Drawing.Point(125, 54);
            this.lblFieldValue.Name = "lblFieldValue";
            this.lblFieldValue.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblFieldValue.Size = new System.Drawing.Size(280, 34);
            this.lblFieldValue.TabIndex = 2;
            this.lblFieldValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtValue
            // 
            this.rootLayout.SetColumnSpan(this.txtValue, 2);
            this.txtValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtValue.Font = new System.Drawing.Font("Consolas", 12F);
            this.txtValue.Location = new System.Drawing.Point(15, 91);
            this.txtValue.Name = "txtValue";
            this.txtValue.Size = new System.Drawing.Size(390, 26);
            this.txtValue.TabIndex = 3;
            // 
            // buttonPanel
            // 
            this.rootLayout.SetColumnSpan(this.buttonPanel, 2);
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Controls.Add(this.btnOk);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonPanel.Location = new System.Drawing.Point(15, 135);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.buttonPanel.Size = new System.Drawing.Size(390, 60);
            this.buttonPanel.TabIndex = 4;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnCancel.Location = new System.Drawing.Point(287, 11);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 38);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOk
            // 
            this.btnOk.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnOk.Location = new System.Drawing.Point(181, 11);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(100, 38);
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // MaterialValueEditDialog
            // 
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(420, 210);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MaterialValueEditDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Material Value Edit";
            this.rootLayout.ResumeLayout(false);
            this.rootLayout.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
