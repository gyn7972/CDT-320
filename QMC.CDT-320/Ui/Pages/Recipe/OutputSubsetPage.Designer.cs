using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class OutputSubsetPage
    {
        private TableLayoutPanel editorLayout;
        private Label lblOutputHeader;
        private Label lblGoodMax;
        private NumericUpDown _nGoodMax;
        private Label lblNgMax;
        private NumericUpDown _nNgMax;
        private Label lblDiesPerWafer;
        private NumericUpDown _nDiesPerWafer;
        private Label lblWafersPerBatch;
        private NumericUpDown _nWafersPerBatch;
        private Label lblAutoBin;
        private CheckBox _cbAutoBin;
        private Label lblAlarmFull;
        private CheckBox _cbAlarmFull;
        private Label lblDefaultGood;
        private TextBox _tbDefaultGood;

        private void InitializeComponent()
        {
            this.editorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblOutputHeader = new System.Windows.Forms.Label();
            this.lblGoodMax = new System.Windows.Forms.Label();
            this._nGoodMax = new System.Windows.Forms.NumericUpDown();
            this.lblNgMax = new System.Windows.Forms.Label();
            this._nNgMax = new System.Windows.Forms.NumericUpDown();
            this.lblDiesPerWafer = new System.Windows.Forms.Label();
            this._nDiesPerWafer = new System.Windows.Forms.NumericUpDown();
            this.lblWafersPerBatch = new System.Windows.Forms.Label();
            this._nWafersPerBatch = new System.Windows.Forms.NumericUpDown();
            this.lblAutoBin = new System.Windows.Forms.Label();
            this._cbAutoBin = new System.Windows.Forms.CheckBox();
            this.lblAlarmFull = new System.Windows.Forms.Label();
            this._cbAlarmFull = new System.Windows.Forms.CheckBox();
            this.lblDefaultGood = new System.Windows.Forms.Label();
            this._tbDefaultGood = new System.Windows.Forms.TextBox();
            this._editorPanel.SuspendLayout();
            this.editorLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGoodMax)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nNgMax)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiesPerWafer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nWafersPerBatch)).BeginInit();
            this.SuspendLayout();
            // 
            // _editorPanel
            // 
            this._editorPanel.Controls.Add(this.editorLayout);
            this._editorPanel.Size = new System.Drawing.Size(1094, 676);
            // 
            // _lblProject
            // 
            this._lblProject.Size = new System.Drawing.Size(794, 36);
            // 
            // editorLayout
            // 
            this.editorLayout.ColumnCount = 2;
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 280F));
            this.editorLayout.Controls.Add(this.lblOutputHeader, 0, 0);
            this.editorLayout.Controls.Add(this.lblGoodMax, 0, 1);
            this.editorLayout.Controls.Add(this._nGoodMax, 1, 1);
            this.editorLayout.Controls.Add(this.lblNgMax, 0, 2);
            this.editorLayout.Controls.Add(this._nNgMax, 1, 2);
            this.editorLayout.Controls.Add(this.lblDiesPerWafer, 0, 3);
            this.editorLayout.Controls.Add(this._nDiesPerWafer, 1, 3);
            this.editorLayout.Controls.Add(this.lblWafersPerBatch, 0, 4);
            this.editorLayout.Controls.Add(this._nWafersPerBatch, 1, 4);
            this.editorLayout.Controls.Add(this.lblAutoBin, 0, 5);
            this.editorLayout.Controls.Add(this._cbAutoBin, 1, 5);
            this.editorLayout.Controls.Add(this.lblAlarmFull, 0, 6);
            this.editorLayout.Controls.Add(this._cbAlarmFull, 1, 6);
            this.editorLayout.Controls.Add(this.lblDefaultGood, 0, 7);
            this.editorLayout.Controls.Add(this._tbDefaultGood, 1, 7);
            this.editorLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.editorLayout.Location = new System.Drawing.Point(8, 12);
            this.editorLayout.Margin = new System.Windows.Forms.Padding(0);
            this.editorLayout.Name = "editorLayout";
            this.editorLayout.Padding = new System.Windows.Forms.Padding(10);
            this.editorLayout.RowCount = 8;
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.Size = new System.Drawing.Size(1078, 292);
            this.editorLayout.TabIndex = 0;
            // 
            // lblOutputHeader
            // 
            this.lblOutputHeader.BackColor = System.Drawing.Color.LightYellow;
            this.lblOutputHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.editorLayout.SetColumnSpan(this.lblOutputHeader, 2);
            this.lblOutputHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOutputHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblOutputHeader.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblOutputHeader.Location = new System.Drawing.Point(13, 10);
            this.lblOutputHeader.Name = "lblOutputHeader";
            this.lblOutputHeader.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblOutputHeader.Size = new System.Drawing.Size(1052, 30);
            this.lblOutputHeader.TabIndex = 0;
            this.lblOutputHeader.Text = "Plate size and output parameters";
            this.lblOutputHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGoodMax
            // 
            this.lblGoodMax.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodMax.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGoodMax.Location = new System.Drawing.Point(13, 40);
            this.lblGoodMax.Name = "lblGoodMax";
            this.lblGoodMax.Size = new System.Drawing.Size(214, 34);
            this.lblGoodMax.TabIndex = 1;
            this.lblGoodMax.Text = "Good plate max slots";
            this.lblGoodMax.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nGoodMax
            // 
            this._nGoodMax.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGoodMax.Font = new System.Drawing.Font("Consolas", 10F);
            this._nGoodMax.Location = new System.Drawing.Point(233, 43);
            this._nGoodMax.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nGoodMax.Name = "_nGoodMax";
            this._nGoodMax.Size = new System.Drawing.Size(832, 27);
            this._nGoodMax.TabIndex = 2;
            this._nGoodMax.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblNgMax
            // 
            this.lblNgMax.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgMax.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNgMax.Location = new System.Drawing.Point(13, 74);
            this.lblNgMax.Name = "lblNgMax";
            this.lblNgMax.Size = new System.Drawing.Size(214, 34);
            this.lblNgMax.TabIndex = 3;
            this.lblNgMax.Text = "NG plate max slots";
            this.lblNgMax.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nNgMax
            // 
            this._nNgMax.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nNgMax.Font = new System.Drawing.Font("Consolas", 10F);
            this._nNgMax.Location = new System.Drawing.Point(233, 77);
            this._nNgMax.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nNgMax.Name = "_nNgMax";
            this._nNgMax.Size = new System.Drawing.Size(832, 27);
            this._nNgMax.TabIndex = 4;
            this._nNgMax.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblDiesPerWafer
            // 
            this.lblDiesPerWafer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDiesPerWafer.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblDiesPerWafer.Location = new System.Drawing.Point(13, 108);
            this.lblDiesPerWafer.Name = "lblDiesPerWafer";
            this.lblDiesPerWafer.Size = new System.Drawing.Size(214, 34);
            this.lblDiesPerWafer.TabIndex = 5;
            this.lblDiesPerWafer.Text = "Dies per wafer";
            this.lblDiesPerWafer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nDiesPerWafer
            // 
            this._nDiesPerWafer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDiesPerWafer.Font = new System.Drawing.Font("Consolas", 10F);
            this._nDiesPerWafer.Location = new System.Drawing.Point(233, 111);
            this._nDiesPerWafer.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nDiesPerWafer.Name = "_nDiesPerWafer";
            this._nDiesPerWafer.Size = new System.Drawing.Size(832, 27);
            this._nDiesPerWafer.TabIndex = 6;
            this._nDiesPerWafer.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblWafersPerBatch
            // 
            this.lblWafersPerBatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWafersPerBatch.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblWafersPerBatch.Location = new System.Drawing.Point(13, 142);
            this.lblWafersPerBatch.Name = "lblWafersPerBatch";
            this.lblWafersPerBatch.Size = new System.Drawing.Size(214, 34);
            this.lblWafersPerBatch.TabIndex = 7;
            this.lblWafersPerBatch.Text = "Wafers per output batch";
            this.lblWafersPerBatch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nWafersPerBatch
            // 
            this._nWafersPerBatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nWafersPerBatch.Font = new System.Drawing.Font("Consolas", 10F);
            this._nWafersPerBatch.Location = new System.Drawing.Point(233, 145);
            this._nWafersPerBatch.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nWafersPerBatch.Name = "_nWafersPerBatch";
            this._nWafersPerBatch.Size = new System.Drawing.Size(832, 27);
            this._nWafersPerBatch.TabIndex = 8;
            this._nWafersPerBatch.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblAutoBin
            // 
            this.lblAutoBin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAutoBin.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblAutoBin.Location = new System.Drawing.Point(13, 176);
            this.lblAutoBin.Name = "lblAutoBin";
            this.lblAutoBin.Size = new System.Drawing.Size(214, 34);
            this.lblAutoBin.TabIndex = 9;
            this.lblAutoBin.Text = "Auto bin";
            this.lblAutoBin.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbAutoBin
            // 
            this._cbAutoBin.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbAutoBin.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbAutoBin.Location = new System.Drawing.Point(233, 179);
            this._cbAutoBin.Name = "_cbAutoBin";
            this._cbAutoBin.Size = new System.Drawing.Size(832, 28);
            this._cbAutoBin.TabIndex = 10;
            this._cbAutoBin.Text = "Auto bin transition enable";
            // 
            // lblAlarmFull
            // 
            this.lblAlarmFull.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAlarmFull.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblAlarmFull.Location = new System.Drawing.Point(13, 210);
            this.lblAlarmFull.Name = "lblAlarmFull";
            this.lblAlarmFull.Size = new System.Drawing.Size(214, 34);
            this.lblAlarmFull.TabIndex = 11;
            this.lblAlarmFull.Text = "Full alarm";
            this.lblAlarmFull.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbAlarmFull
            // 
            this._cbAlarmFull.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbAlarmFull.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbAlarmFull.Location = new System.Drawing.Point(233, 213);
            this._cbAlarmFull.Name = "_cbAlarmFull";
            this._cbAlarmFull.Size = new System.Drawing.Size(832, 28);
            this._cbAlarmFull.TabIndex = 12;
            this._cbAlarmFull.Text = "Alarm and cycle stop when plate full";
            // 
            // lblDefaultGood
            // 
            this.lblDefaultGood.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDefaultGood.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblDefaultGood.Location = new System.Drawing.Point(13, 244);
            this.lblDefaultGood.Name = "lblDefaultGood";
            this.lblDefaultGood.Size = new System.Drawing.Size(214, 38);
            this.lblDefaultGood.TabIndex = 13;
            this.lblDefaultGood.Text = "Default good cassette";
            this.lblDefaultGood.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbDefaultGood
            // 
            this._tbDefaultGood.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbDefaultGood.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbDefaultGood.Location = new System.Drawing.Point(233, 247);
            this._tbDefaultGood.Name = "_tbDefaultGood";
            this._tbDefaultGood.Size = new System.Drawing.Size(832, 27);
            this._tbDefaultGood.TabIndex = 14;
            // 
            // OutputSubsetPage
            // 
            this.Name = "OutputSubsetPage";
            this.Size = new System.Drawing.Size(1094, 742);
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this._editorPanel.ResumeLayout(false);
            this.editorLayout.ResumeLayout(false);
            this.editorLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGoodMax)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nNgMax)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiesPerWafer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nWafersPerBatch)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
