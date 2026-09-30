using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class UnloadTapeFrameSubsetPage
    {
        private TableLayoutPanel editorLayout;
        private GroupBox grpUnload;
        private TableLayoutPanel tlpUnload;
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
            this.grpUnload = new System.Windows.Forms.GroupBox();
            this.tlpUnload = new System.Windows.Forms.TableLayoutPanel();
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
            this.grpUnload.SuspendLayout();
            this.tlpUnload.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nUpper)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nLower)).BeginInit();
            this.SuspendLayout();
            // 
            // _editorPanel
            // 
            this._editorPanel.BackColor = System.Drawing.Color.White;
            this._editorPanel.Controls.Add(this.editorLayout);
            this._editorPanel.Size = new System.Drawing.Size(1094, 676);
            // 
            // _lblProject
            // 
            this._lblProject.Size = new System.Drawing.Size(794, 36);
            // 
            // editorLayout
            // 
            this.editorLayout.BackColor = System.Drawing.Color.White;
            this.editorLayout.ColumnCount = 2;
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.Controls.Add(this.grpUnload, 0, 0);
            this.editorLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editorLayout.Location = new System.Drawing.Point(8, 12);
            this.editorLayout.Margin = new System.Windows.Forms.Padding(0);
            this.editorLayout.Name = "editorLayout";
            this.editorLayout.RowCount = 2;
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 176F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.editorLayout.Size = new System.Drawing.Size(1078, 656);
            this.editorLayout.TabIndex = 0;
            // 
            // grpUnload
            // 
            this.grpUnload.BackColor = System.Drawing.Color.White;
            this.grpUnload.Controls.Add(this.tlpUnload);
            this.grpUnload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpUnload.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpUnload.Location = new System.Drawing.Point(0, 0);
            this.grpUnload.Margin = new System.Windows.Forms.Padding(0);
            this.grpUnload.Name = "grpUnload";
            this.grpUnload.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpUnload.Size = new System.Drawing.Size(539, 176);
            this.grpUnload.TabIndex = 0;
            this.grpUnload.TabStop = false;
            this.grpUnload.Text = "Unload tape frame options";
            // 
            // tlpUnload
            // 
            this.tlpUnload.BackColor = System.Drawing.Color.White;
            this.tlpUnload.ColumnCount = 2;
            this.tlpUnload.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpUnload.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpUnload.Controls.Add(this.lblRole, 0, 0);
            this.tlpUnload.Controls.Add(this._cbRole, 1, 0);
            this.tlpUnload.Controls.Add(this.lblGapInspection, 0, 1);
            this.tlpUnload.Controls.Add(this._cbGapInsp, 1, 1);
            this.tlpUnload.Controls.Add(this.lblGapUpper, 0, 2);
            this.tlpUnload.Controls.Add(this._nUpper, 1, 2);
            this.tlpUnload.Controls.Add(this.lblGapLower, 0, 3);
            this.tlpUnload.Controls.Add(this._nLower, 1, 3);
            this.tlpUnload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpUnload.Location = new System.Drawing.Point(6, 20);
            this.tlpUnload.Margin = new System.Windows.Forms.Padding(0);
            this.tlpUnload.Name = "tlpUnload";
            this.tlpUnload.RowCount = 5;
            this.tlpUnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpUnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpUnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpUnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpUnload.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpUnload.Size = new System.Drawing.Size(527, 150);
            this.tlpUnload.TabIndex = 0;
            // 
            // lblRole
            // 
            this.lblRole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRole.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblRole.Location = new System.Drawing.Point(3, 0);
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
            this._cbRole.Location = new System.Drawing.Point(223, 5);
            this._cbRole.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._cbRole.Name = "_cbRole";
            this._cbRole.Size = new System.Drawing.Size(301, 23);
            this._cbRole.TabIndex = 2;
            // 
            // lblGapInspection
            // 
            this.lblGapInspection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGapInspection.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGapInspection.Location = new System.Drawing.Point(3, 34);
            this.lblGapInspection.Name = "lblGapInspection";
            this.lblGapInspection.Size = new System.Drawing.Size(214, 34);
            this.lblGapInspection.TabIndex = 3;
            this.lblGapInspection.Text = "Gap inspection";
            this.lblGapInspection.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbGapInsp
            // 
            this._cbGapInsp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbGapInsp.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbGapInsp.Location = new System.Drawing.Point(223, 37);
            this._cbGapInsp.Name = "_cbGapInsp";
            this._cbGapInsp.Size = new System.Drawing.Size(301, 28);
            this._cbGapInsp.TabIndex = 4;
            this._cbGapInsp.Text = "Gap inspection enabled after place";
            // 
            // lblGapUpper
            // 
            this.lblGapUpper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGapUpper.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGapUpper.Location = new System.Drawing.Point(3, 68);
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
            this._nUpper.Location = new System.Drawing.Point(223, 73);
            this._nUpper.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._nUpper.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this._nUpper.Name = "_nUpper";
            this._nUpper.Size = new System.Drawing.Size(301, 23);
            this._nUpper.TabIndex = 6;
            // 
            // lblGapLower
            // 
            this.lblGapLower.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGapLower.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGapLower.Location = new System.Drawing.Point(3, 102);
            this.lblGapLower.Name = "lblGapLower";
            this.lblGapLower.Size = new System.Drawing.Size(214, 34);
            this.lblGapLower.TabIndex = 7;
            this.lblGapLower.Text = "Gap lower limit (mm)";
            this.lblGapLower.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nLower
            // 
            this._nLower.DecimalPlaces = 4;
            this._nLower.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nLower.Font = new System.Drawing.Font("Consolas", 10F);
            this._nLower.Location = new System.Drawing.Point(223, 107);
            this._nLower.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._nLower.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this._nLower.Name = "_nLower";
            this._nLower.Size = new System.Drawing.Size(301, 23);
            this._nLower.TabIndex = 8;
            // 
            // UnloadTapeFrameSubsetPage
            // 
            this.Name = "UnloadTapeFrameSubsetPage";
            this.Size = new System.Drawing.Size(1094, 742);
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this._editorPanel.ResumeLayout(false);
            this.editorLayout.ResumeLayout(false);
            this.grpUnload.ResumeLayout(false);
            this.tlpUnload.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nUpper)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nLower)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
