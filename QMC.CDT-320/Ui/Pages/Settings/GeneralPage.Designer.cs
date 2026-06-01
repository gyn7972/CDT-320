namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class GeneralPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel bodyLayout;
        private System.Windows.Forms.Label lblLanguage;
        private System.Windows.Forms.Label lblBinArray;
        private System.Windows.Forms.Label lblVisionMatch;
        private System.Windows.Forms.ComboBox _cbLang;
        private System.Windows.Forms.ComboBox _cbBinArr;
        private System.Windows.Forms.ComboBox _cbVisionMatch;
        private System.Windows.Forms.GroupBox grpAjin;
        private System.Windows.Forms.TableLayoutPanel ajinLayout;
        private System.Windows.Forms.CheckBox _cbAjin;
        private System.Windows.Forms.Label lblIrq;
        private System.Windows.Forms.TextBox _tbIrq;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.bodyLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblLanguage = new System.Windows.Forms.Label();
            this.lblBinArray = new System.Windows.Forms.Label();
            this.lblVisionMatch = new System.Windows.Forms.Label();
            this._cbLang = new System.Windows.Forms.ComboBox();
            this._cbBinArr = new System.Windows.Forms.ComboBox();
            this._cbVisionMatch = new System.Windows.Forms.ComboBox();
            this.grpAjin = new System.Windows.Forms.GroupBox();
            this.ajinLayout = new System.Windows.Forms.TableLayoutPanel();
            this._cbAjin = new System.Windows.Forms.CheckBox();
            this.lblIrq = new System.Windows.Forms.Label();
            this._tbIrq = new System.Windows.Forms.TextBox();
            this.rootLayout.SuspendLayout();
            this.bodyLayout.SuspendLayout();
            this.grpAjin.SuspendLayout();
            this.ajinLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.bodyLayout, 0, 1);
            this.rootLayout.Controls.Add(this.grpAjin, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1416, 980);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Location = new System.Drawing.Point(8, 8);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1400, 28);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "SETTING";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // bodyLayout
            // 
            this.bodyLayout.ColumnCount = 2;
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.bodyLayout.Controls.Add(this.lblLanguage, 0, 0);
            this.bodyLayout.Controls.Add(this.lblBinArray, 0, 1);
            this.bodyLayout.Controls.Add(this.lblVisionMatch, 0, 2);
            this.bodyLayout.Controls.Add(this._cbLang, 1, 0);
            this.bodyLayout.Controls.Add(this._cbBinArr, 1, 1);
            this.bodyLayout.Controls.Add(this._cbVisionMatch, 1, 2);
            this.bodyLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bodyLayout.Location = new System.Drawing.Point(8, 40);
            this.bodyLayout.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.bodyLayout.Name = "bodyLayout";
            this.bodyLayout.Padding = new System.Windows.Forms.Padding(8);
            this.bodyLayout.RowCount = 3;
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.bodyLayout.Size = new System.Drawing.Size(1400, 132);
            this.bodyLayout.TabIndex = 1;
            // 
            // lblLanguage
            // 
            this.lblLanguage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblLanguage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLanguage.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLanguage.Location = new System.Drawing.Point(10, 10);
            this.lblLanguage.Margin = new System.Windows.Forms.Padding(2);
            this.lblLanguage.Name = "lblLanguage";
            this.lblLanguage.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblLanguage.Size = new System.Drawing.Size(176, 36);
            this.lblLanguage.TabIndex = 0;
            this.lblLanguage.Text = "Language";
            this.lblLanguage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBinArray
            // 
            this.lblBinArray.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblBinArray.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinArray.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBinArray.Location = new System.Drawing.Point(10, 50);
            this.lblBinArray.Margin = new System.Windows.Forms.Padding(2);
            this.lblBinArray.Name = "lblBinArray";
            this.lblBinArray.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblBinArray.Size = new System.Drawing.Size(176, 36);
            this.lblBinArray.TabIndex = 1;
            this.lblBinArray.Text = "Bin Array File";
            this.lblBinArray.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVisionMatch
            // 
            this.lblVisionMatch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblVisionMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionMatch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblVisionMatch.Location = new System.Drawing.Point(10, 90);
            this.lblVisionMatch.Margin = new System.Windows.Forms.Padding(2);
            this.lblVisionMatch.Name = "lblVisionMatch";
            this.lblVisionMatch.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblVisionMatch.Size = new System.Drawing.Size(176, 36);
            this.lblVisionMatch.TabIndex = 2;
            this.lblVisionMatch.Text = "Vision Match Error";
            this.lblVisionMatch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbLang
            // 
            this._cbLang.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbLang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbLang.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._cbLang.Location = new System.Drawing.Point(190, 10);
            this._cbLang.Margin = new System.Windows.Forms.Padding(2);
            this._cbLang.Name = "_cbLang";
            this._cbLang.Size = new System.Drawing.Size(1200, 28);
            this._cbLang.TabIndex = 3;
            // 
            // _cbBinArr
            // 
            this._cbBinArr.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbBinArr.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbBinArr.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._cbBinArr.Location = new System.Drawing.Point(190, 50);
            this._cbBinArr.Margin = new System.Windows.Forms.Padding(2);
            this._cbBinArr.Name = "_cbBinArr";
            this._cbBinArr.Size = new System.Drawing.Size(1200, 28);
            this._cbBinArr.TabIndex = 4;
            // 
            // _cbVisionMatch
            // 
            this._cbVisionMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbVisionMatch.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbVisionMatch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._cbVisionMatch.Location = new System.Drawing.Point(190, 90);
            this._cbVisionMatch.Margin = new System.Windows.Forms.Padding(2);
            this._cbVisionMatch.Name = "_cbVisionMatch";
            this._cbVisionMatch.Size = new System.Drawing.Size(1200, 28);
            this._cbVisionMatch.TabIndex = 5;
            // 
            // grpAjin
            // 
            this.grpAjin.Controls.Add(this.ajinLayout);
            this.grpAjin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAjin.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.grpAjin.Location = new System.Drawing.Point(8, 180);
            this.grpAjin.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpAjin.Name = "grpAjin";
            this.grpAjin.Padding = new System.Windows.Forms.Padding(8);
            this.grpAjin.Size = new System.Drawing.Size(1400, 112);
            this.grpAjin.TabIndex = 2;
            this.grpAjin.TabStop = false;
            this.grpAjin.Text = "AJINEXTEK";
            // 
            // ajinLayout
            // 
            this.ajinLayout.ColumnCount = 3;
            this.ajinLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.ajinLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.ajinLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.ajinLayout.Controls.Add(this._cbAjin, 0, 0);
            this.ajinLayout.Controls.Add(this.lblIrq, 1, 0);
            this.ajinLayout.Controls.Add(this._tbIrq, 2, 0);
            this.ajinLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.ajinLayout.Location = new System.Drawing.Point(8, 31);
            this.ajinLayout.Name = "ajinLayout";
            this.ajinLayout.RowCount = 1;
            this.ajinLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.ajinLayout.Size = new System.Drawing.Size(1384, 36);
            this.ajinLayout.TabIndex = 0;
            // 
            // _cbAjin
            // 
            this._cbAjin.AutoSize = true;
            this._cbAjin.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbAjin.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._cbAjin.Location = new System.Drawing.Point(3, 3);
            this._cbAjin.Name = "_cbAjin";
            this._cbAjin.Size = new System.Drawing.Size(174, 30);
            this._cbAjin.TabIndex = 0;
            this._cbAjin.Text = "UseAjin (AXL.dll)";
            this._cbAjin.UseVisualStyleBackColor = true;
            // 
            // lblIrq
            // 
            this.lblIrq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblIrq.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblIrq.Location = new System.Drawing.Point(183, 0);
            this.lblIrq.Name = "lblIrq";
            this.lblIrq.Size = new System.Drawing.Size(74, 36);
            this.lblIrq.TabIndex = 1;
            this.lblIrq.Text = "IRQ NO.";
            this.lblIrq.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbIrq
            // 
            this._tbIrq.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbIrq.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._tbIrq.Location = new System.Drawing.Point(263, 3);
            this._tbIrq.Name = "_tbIrq";
            this._tbIrq.Size = new System.Drawing.Size(1118, 27);
            this._tbIrq.TabIndex = 2;
            // 
            // GeneralPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "GeneralPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.bodyLayout.ResumeLayout(false);
            this.grpAjin.ResumeLayout(false);
            this.ajinLayout.ResumeLayout(false);
            this.ajinLayout.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
