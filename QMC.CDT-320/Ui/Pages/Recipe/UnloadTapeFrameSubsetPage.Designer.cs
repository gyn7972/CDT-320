using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class UnloadTapeFrameSubsetPage
    {
        private TableLayoutPanel editorLayout;
        private Label lblUnloadHeader;
        private Label lblRole;
        private ComboBox _cbRole;
        private Label lblGapInspection;
        private CheckBox _cbGapInsp;
        private Label lblGapUpper;
        private NumericUpDown _nUpper;
        private Label lblGapLower;
        private NumericUpDown _nLower;

        private void InitializeComponent()
        {
            this.editorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblUnloadHeader = new System.Windows.Forms.Label();
            this.lblRole = new System.Windows.Forms.Label();
            this._cbRole = new System.Windows.Forms.ComboBox();
            this.lblGapInspection = new System.Windows.Forms.Label();
            this._cbGapInsp = new System.Windows.Forms.CheckBox();
            this.lblGapUpper = new System.Windows.Forms.Label();
            this._nUpper = new System.Windows.Forms.NumericUpDown();
            this.lblGapLower = new System.Windows.Forms.Label();
            this._nLower = new System.Windows.Forms.NumericUpDown();
            this._editorPanel.SuspendLayout();
            this.editorLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nUpper)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nLower)).BeginInit();
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
            this.editorLayout.Controls.Add(this.lblUnloadHeader, 0, 0);
            this.editorLayout.Controls.Add(this.lblRole, 0, 1);
            this.editorLayout.Controls.Add(this._cbRole, 1, 1);
            this.editorLayout.Controls.Add(this.lblGapInspection, 0, 2);
            this.editorLayout.Controls.Add(this._cbGapInsp, 1, 2);
            this.editorLayout.Controls.Add(this.lblGapUpper, 0, 3);
            this.editorLayout.Controls.Add(this._nUpper, 1, 3);
            this.editorLayout.Controls.Add(this.lblGapLower, 0, 4);
            this.editorLayout.Controls.Add(this._nLower, 1, 4);
            this.editorLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.editorLayout.Location = new System.Drawing.Point(8, 12);
            this.editorLayout.Margin = new System.Windows.Forms.Padding(0);
            this.editorLayout.Name = "editorLayout";
            this.editorLayout.Padding = new System.Windows.Forms.Padding(10);
            this.editorLayout.RowCount = 5;
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.Size = new System.Drawing.Size(1078, 190);
            this.editorLayout.TabIndex = 0;
            // 
            // lblUnloadHeader
            // 
            this.lblUnloadHeader.BackColor = System.Drawing.Color.LightYellow;
            this.lblUnloadHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.editorLayout.SetColumnSpan(this.lblUnloadHeader, 2);
            this.lblUnloadHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUnloadHeader.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F, System.Drawing.FontStyle.Bold);
            this.lblUnloadHeader.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblUnloadHeader.Location = new System.Drawing.Point(13, 10);
            this.lblUnloadHeader.Name = "lblUnloadHeader";
            this.lblUnloadHeader.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblUnloadHeader.Size = new System.Drawing.Size(1052, 30);
            this.lblUnloadHeader.TabIndex = 0;
            this.lblUnloadHeader.Text = "Unload tape frame options";
            this.lblUnloadHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRole
            // 
            this.lblRole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRole.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this.lblRole.Location = new System.Drawing.Point(13, 40);
            this.lblRole.Name = "lblRole";
            this.lblRole.Size = new System.Drawing.Size(214, 34);
            this.lblRole.TabIndex = 1;
            this.lblRole.Text = "Role";
            this.lblRole.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbRole
            // 
            this._cbRole.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbRole.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbRole.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbRole.Items.AddRange(new object[] {
            "Load",
            "GoodUnload",
            "NgUnload"});
            this._cbRole.Location = new System.Drawing.Point(233, 43);
            this._cbRole.Name = "_cbRole";
            this._cbRole.Size = new System.Drawing.Size(832, 28);
            this._cbRole.TabIndex = 2;
            // 
            // lblGapInspection
            // 
            this.lblGapInspection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGapInspection.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this.lblGapInspection.Location = new System.Drawing.Point(13, 74);
            this.lblGapInspection.Name = "lblGapInspection";
            this.lblGapInspection.Size = new System.Drawing.Size(214, 34);
            this.lblGapInspection.TabIndex = 3;
            this.lblGapInspection.Text = "Gap inspection";
            this.lblGapInspection.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbGapInsp
            // 
            this._cbGapInsp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbGapInsp.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this._cbGapInsp.Location = new System.Drawing.Point(233, 77);
            this._cbGapInsp.Name = "_cbGapInsp";
            this._cbGapInsp.Size = new System.Drawing.Size(832, 28);
            this._cbGapInsp.TabIndex = 4;
            this._cbGapInsp.Text = "Gap inspection enabled after place";
            // 
            // lblGapUpper
            // 
            this.lblGapUpper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGapUpper.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this.lblGapUpper.Location = new System.Drawing.Point(13, 108);
            this.lblGapUpper.Name = "lblGapUpper";
            this.lblGapUpper.Size = new System.Drawing.Size(214, 34);
            this.lblGapUpper.TabIndex = 5;
            this.lblGapUpper.Text = "Gap upper limit (mm)";
            this.lblGapUpper.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nUpper
            // 
            this._nUpper.DecimalPlaces = 4;
            this._nUpper.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nUpper.Font = new System.Drawing.Font("Consolas", 10F);
            this._nUpper.Location = new System.Drawing.Point(233, 111);
            this._nUpper.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this._nUpper.Name = "_nUpper";
            this._nUpper.Size = new System.Drawing.Size(832, 27);
            this._nUpper.TabIndex = 6;
            // 
            // lblGapLower
            // 
            this.lblGapLower.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGapLower.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this.lblGapLower.Location = new System.Drawing.Point(13, 142);
            this.lblGapLower.Name = "lblGapLower";
            this.lblGapLower.Size = new System.Drawing.Size(214, 38);
            this.lblGapLower.TabIndex = 7;
            this.lblGapLower.Text = "Gap lower limit (mm)";
            this.lblGapLower.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nLower
            // 
            this._nLower.DecimalPlaces = 4;
            this._nLower.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nLower.Font = new System.Drawing.Font("Consolas", 10F);
            this._nLower.Location = new System.Drawing.Point(233, 145);
            this._nLower.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this._nLower.Name = "_nLower";
            this._nLower.Size = new System.Drawing.Size(832, 27);
            this._nLower.TabIndex = 8;
            // 
            // UnloadTapeFrameSubsetPage
            // 
            this.Name = "UnloadTapeFrameSubsetPage";
            this.Size = new System.Drawing.Size(1094, 742);
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this._editorPanel.ResumeLayout(false);
            this.editorLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nUpper)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nLower)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
