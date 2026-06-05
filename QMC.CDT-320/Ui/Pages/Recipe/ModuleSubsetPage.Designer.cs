using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class ModuleSubsetPage
    {
        private TableLayoutPanel editorLayout;
        private Label lblPickPlaceHeader;
        private Label lblPickRetry;
        private NumericUpDown _nPickRetry;
        private Label lblPickDelay;
        private NumericUpDown _nPickDelay;
        private Label lblPlaceDelay;
        private NumericUpDown _nPlaceDelay;
        private Label lblColletHeader;
        private Label lblColletEnable;
        private CheckBox _cbColletEnable;
        private Label lblColletInterval;
        private NumericUpDown _nColletInterval;
        private Label lblInspectionHeader;
        private Label lblBottomInspection;
        private CheckBox _cbBottomInspect;
        private Label lblPlacementInspection;
        private CheckBox _cbPlacementInspect;

        private void InitializeComponent()
        {
            this.editorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblPickPlaceHeader = new System.Windows.Forms.Label();
            this.lblPickRetry = new System.Windows.Forms.Label();
            this._nPickRetry = new System.Windows.Forms.NumericUpDown();
            this.lblPickDelay = new System.Windows.Forms.Label();
            this._nPickDelay = new System.Windows.Forms.NumericUpDown();
            this.lblPlaceDelay = new System.Windows.Forms.Label();
            this._nPlaceDelay = new System.Windows.Forms.NumericUpDown();
            this.lblColletHeader = new System.Windows.Forms.Label();
            this.lblColletEnable = new System.Windows.Forms.Label();
            this._cbColletEnable = new System.Windows.Forms.CheckBox();
            this.lblColletInterval = new System.Windows.Forms.Label();
            this._nColletInterval = new System.Windows.Forms.NumericUpDown();
            this.lblInspectionHeader = new System.Windows.Forms.Label();
            this.lblBottomInspection = new System.Windows.Forms.Label();
            this._cbBottomInspect = new System.Windows.Forms.CheckBox();
            this.lblPlacementInspection = new System.Windows.Forms.Label();
            this._cbPlacementInspect = new System.Windows.Forms.CheckBox();
            this._editorPanel.SuspendLayout();
            this.editorLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nPickRetry)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPickDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPlaceDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nColletInterval)).BeginInit();
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
            this.editorLayout.Controls.Add(this.lblPickPlaceHeader, 0, 0);
            this.editorLayout.Controls.Add(this.lblPickRetry, 0, 1);
            this.editorLayout.Controls.Add(this._nPickRetry, 1, 1);
            this.editorLayout.Controls.Add(this.lblPickDelay, 0, 2);
            this.editorLayout.Controls.Add(this._nPickDelay, 1, 2);
            this.editorLayout.Controls.Add(this.lblPlaceDelay, 0, 3);
            this.editorLayout.Controls.Add(this._nPlaceDelay, 1, 3);
            this.editorLayout.Controls.Add(this.lblColletHeader, 0, 4);
            this.editorLayout.Controls.Add(this.lblColletEnable, 0, 5);
            this.editorLayout.Controls.Add(this._cbColletEnable, 1, 5);
            this.editorLayout.Controls.Add(this.lblColletInterval, 0, 6);
            this.editorLayout.Controls.Add(this._nColletInterval, 1, 6);
            this.editorLayout.Controls.Add(this.lblInspectionHeader, 0, 7);
            this.editorLayout.Controls.Add(this.lblBottomInspection, 0, 8);
            this.editorLayout.Controls.Add(this._cbBottomInspect, 1, 8);
            this.editorLayout.Controls.Add(this.lblPlacementInspection, 0, 9);
            this.editorLayout.Controls.Add(this._cbPlacementInspect, 1, 9);
            this.editorLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.editorLayout.Location = new System.Drawing.Point(8, 12);
            this.editorLayout.Margin = new System.Windows.Forms.Padding(0);
            this.editorLayout.Name = "editorLayout";
            this.editorLayout.Padding = new System.Windows.Forms.Padding(10);
            this.editorLayout.RowCount = 10;
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.Size = new System.Drawing.Size(1078, 360);
            this.editorLayout.TabIndex = 0;
            // 
            // lblPickPlaceHeader
            // 
            this.lblPickPlaceHeader.BackColor = System.Drawing.Color.LightYellow;
            this.lblPickPlaceHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.editorLayout.SetColumnSpan(this.lblPickPlaceHeader, 2);
            this.lblPickPlaceHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickPlaceHeader.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F, System.Drawing.FontStyle.Bold);
            this.lblPickPlaceHeader.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblPickPlaceHeader.Location = new System.Drawing.Point(13, 10);
            this.lblPickPlaceHeader.Name = "lblPickPlaceHeader";
            this.lblPickPlaceHeader.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblPickPlaceHeader.Size = new System.Drawing.Size(1052, 30);
            this.lblPickPlaceHeader.TabIndex = 0;
            this.lblPickPlaceHeader.Text = "Pick / place parameters";
            this.lblPickPlaceHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPickRetry
            // 
            this.lblPickRetry.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickRetry.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this.lblPickRetry.Location = new System.Drawing.Point(13, 40);
            this.lblPickRetry.Name = "lblPickRetry";
            this.lblPickRetry.Size = new System.Drawing.Size(214, 34);
            this.lblPickRetry.TabIndex = 1;
            this.lblPickRetry.Text = "Pick retry count";
            this.lblPickRetry.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nPickRetry
            // 
            this._nPickRetry.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPickRetry.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPickRetry.Location = new System.Drawing.Point(233, 43);
            this._nPickRetry.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this._nPickRetry.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nPickRetry.Name = "_nPickRetry";
            this._nPickRetry.Size = new System.Drawing.Size(832, 27);
            this._nPickRetry.TabIndex = 2;
            this._nPickRetry.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblPickDelay
            // 
            this.lblPickDelay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickDelay.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this.lblPickDelay.Location = new System.Drawing.Point(13, 74);
            this.lblPickDelay.Name = "lblPickDelay";
            this.lblPickDelay.Size = new System.Drawing.Size(214, 34);
            this.lblPickDelay.TabIndex = 3;
            this.lblPickDelay.Text = "Pick delay (ms)";
            this.lblPickDelay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nPickDelay
            // 
            this._nPickDelay.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPickDelay.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPickDelay.Location = new System.Drawing.Point(233, 77);
            this._nPickDelay.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nPickDelay.Name = "_nPickDelay";
            this._nPickDelay.Size = new System.Drawing.Size(832, 27);
            this._nPickDelay.TabIndex = 4;
            // 
            // lblPlaceDelay
            // 
            this.lblPlaceDelay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceDelay.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this.lblPlaceDelay.Location = new System.Drawing.Point(13, 108);
            this.lblPlaceDelay.Name = "lblPlaceDelay";
            this.lblPlaceDelay.Size = new System.Drawing.Size(214, 34);
            this.lblPlaceDelay.TabIndex = 5;
            this.lblPlaceDelay.Text = "Place delay (ms)";
            this.lblPlaceDelay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nPlaceDelay
            // 
            this._nPlaceDelay.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPlaceDelay.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPlaceDelay.Location = new System.Drawing.Point(233, 111);
            this._nPlaceDelay.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nPlaceDelay.Name = "_nPlaceDelay";
            this._nPlaceDelay.Size = new System.Drawing.Size(832, 27);
            this._nPlaceDelay.TabIndex = 6;
            // 
            // lblColletHeader
            // 
            this.lblColletHeader.BackColor = System.Drawing.Color.LightYellow;
            this.lblColletHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.editorLayout.SetColumnSpan(this.lblColletHeader, 2);
            this.lblColletHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletHeader.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F, System.Drawing.FontStyle.Bold);
            this.lblColletHeader.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblColletHeader.Location = new System.Drawing.Point(13, 142);
            this.lblColletHeader.Name = "lblColletHeader";
            this.lblColletHeader.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblColletHeader.Size = new System.Drawing.Size(1052, 30);
            this.lblColletHeader.TabIndex = 7;
            this.lblColletHeader.Text = "Collet cleaning";
            this.lblColletHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblColletEnable
            // 
            this.lblColletEnable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletEnable.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this.lblColletEnable.Location = new System.Drawing.Point(13, 172);
            this.lblColletEnable.Name = "lblColletEnable";
            this.lblColletEnable.Size = new System.Drawing.Size(214, 34);
            this.lblColletEnable.TabIndex = 8;
            this.lblColletEnable.Text = "Collet cleaning";
            this.lblColletEnable.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbColletEnable
            // 
            this._cbColletEnable.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbColletEnable.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this._cbColletEnable.Location = new System.Drawing.Point(233, 175);
            this._cbColletEnable.Name = "_cbColletEnable";
            this._cbColletEnable.Size = new System.Drawing.Size(832, 28);
            this._cbColletEnable.TabIndex = 9;
            this._cbColletEnable.Text = "Collet cleaning enable";
            // 
            // lblColletInterval
            // 
            this.lblColletInterval.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletInterval.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this.lblColletInterval.Location = new System.Drawing.Point(13, 206);
            this.lblColletInterval.Name = "lblColletInterval";
            this.lblColletInterval.Size = new System.Drawing.Size(214, 34);
            this.lblColletInterval.TabIndex = 10;
            this.lblColletInterval.Text = "Cleaning interval";
            this.lblColletInterval.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nColletInterval
            // 
            this._nColletInterval.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nColletInterval.Font = new System.Drawing.Font("Consolas", 10F);
            this._nColletInterval.Location = new System.Drawing.Point(233, 209);
            this._nColletInterval.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this._nColletInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nColletInterval.Name = "_nColletInterval";
            this._nColletInterval.Size = new System.Drawing.Size(832, 27);
            this._nColletInterval.TabIndex = 11;
            this._nColletInterval.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblInspectionHeader
            // 
            this.lblInspectionHeader.BackColor = System.Drawing.Color.LightYellow;
            this.lblInspectionHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.editorLayout.SetColumnSpan(this.lblInspectionHeader, 2);
            this.lblInspectionHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInspectionHeader.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F, System.Drawing.FontStyle.Bold);
            this.lblInspectionHeader.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblInspectionHeader.Location = new System.Drawing.Point(13, 240);
            this.lblInspectionHeader.Name = "lblInspectionHeader";
            this.lblInspectionHeader.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblInspectionHeader.Size = new System.Drawing.Size(1052, 30);
            this.lblInspectionHeader.TabIndex = 12;
            this.lblInspectionHeader.Text = "Inspection enable";
            this.lblInspectionHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBottomInspection
            // 
            this.lblBottomInspection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBottomInspection.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this.lblBottomInspection.Location = new System.Drawing.Point(13, 270);
            this.lblBottomInspection.Name = "lblBottomInspection";
            this.lblBottomInspection.Size = new System.Drawing.Size(214, 34);
            this.lblBottomInspection.TabIndex = 13;
            this.lblBottomInspection.Text = "Bottom inspection";
            this.lblBottomInspection.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbBottomInspect
            // 
            this._cbBottomInspect.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbBottomInspect.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this._cbBottomInspect.Location = new System.Drawing.Point(233, 273);
            this._cbBottomInspect.Name = "_cbBottomInspect";
            this._cbBottomInspect.Size = new System.Drawing.Size(832, 28);
            this._cbBottomInspect.TabIndex = 14;
            this._cbBottomInspect.Text = "Bottom vision inspection";
            // 
            // lblPlacementInspection
            // 
            this.lblPlacementInspection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlacementInspection.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this.lblPlacementInspection.Location = new System.Drawing.Point(13, 304);
            this.lblPlacementInspection.Name = "lblPlacementInspection";
            this.lblPlacementInspection.Size = new System.Drawing.Size(214, 46);
            this.lblPlacementInspection.TabIndex = 15;
            this.lblPlacementInspection.Text = "Placement inspection";
            this.lblPlacementInspection.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbPlacementInspect
            // 
            this._cbPlacementInspect.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbPlacementInspect.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this._cbPlacementInspect.Location = new System.Drawing.Point(233, 307);
            this._cbPlacementInspect.Name = "_cbPlacementInspect";
            this._cbPlacementInspect.Size = new System.Drawing.Size(832, 40);
            this._cbPlacementInspect.TabIndex = 16;
            this._cbPlacementInspect.Text = "Placement bin inspection";
            // 
            // ModuleSubsetPage
            // 
            this.Name = "ModuleSubsetPage";
            this.Size = new System.Drawing.Size(1094, 742);
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this._editorPanel.ResumeLayout(false);
            this.editorLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nPickRetry)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPickDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPlaceDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nColletInterval)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
