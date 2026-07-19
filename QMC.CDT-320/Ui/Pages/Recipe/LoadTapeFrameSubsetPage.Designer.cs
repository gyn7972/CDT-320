using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class LoadTapeFrameSubsetPage
    {
        private TableLayoutPanel editorLayout;
        private GroupBox grpLoad;
        private TableLayoutPanel tlpLoad;
        private Label lblRole;
        private ComboBox _cbRole;
        private Label lblAlignPts;
        private NumericUpDown _nAlignPts;
        private Label lblAutoBarcode;
        private CheckBox _cbAutoBarcode;
        private Label lblAutoAlign;
        private CheckBox _cbAutoAlign;

        private void InitializeComponent()
        {
            this.editorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpLoad = new System.Windows.Forms.GroupBox();
            this.tlpLoad = new System.Windows.Forms.TableLayoutPanel();
            this.lblRole = new System.Windows.Forms.Label();
            this._cbRole = new System.Windows.Forms.ComboBox();
            this.lblAlignPts = new System.Windows.Forms.Label();
            this._nAlignPts = new System.Windows.Forms.NumericUpDown();
            this.lblAutoBarcode = new System.Windows.Forms.Label();
            this._cbAutoBarcode = new System.Windows.Forms.CheckBox();
            this.lblAutoAlign = new System.Windows.Forms.Label();
            this._cbAutoAlign = new System.Windows.Forms.CheckBox();
            this._editorPanel.SuspendLayout();
            this.editorLayout.SuspendLayout();
            this.grpLoad.SuspendLayout();
            this.tlpLoad.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nAlignPts)).BeginInit();
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
            this.editorLayout.Controls.Add(this.grpLoad, 0, 0);
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
            // grpLoad
            // 
            this.grpLoad.BackColor = System.Drawing.Color.White;
            this.grpLoad.Controls.Add(this.tlpLoad);
            this.grpLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLoad.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpLoad.Location = new System.Drawing.Point(0, 0);
            this.grpLoad.Margin = new System.Windows.Forms.Padding(0);
            this.grpLoad.Name = "grpLoad";
            this.grpLoad.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpLoad.Size = new System.Drawing.Size(539, 176);
            this.grpLoad.TabIndex = 0;
            this.grpLoad.TabStop = false;
            this.grpLoad.Text = "Load tape frame options";
            // 
            // tlpLoad
            // 
            this.tlpLoad.BackColor = System.Drawing.Color.White;
            this.tlpLoad.ColumnCount = 2;
            this.tlpLoad.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpLoad.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpLoad.Controls.Add(this.lblRole, 0, 0);
            this.tlpLoad.Controls.Add(this._cbRole, 1, 0);
            this.tlpLoad.Controls.Add(this.lblAlignPts, 0, 1);
            this.tlpLoad.Controls.Add(this._nAlignPts, 1, 1);
            this.tlpLoad.Controls.Add(this.lblAutoBarcode, 0, 2);
            this.tlpLoad.Controls.Add(this._cbAutoBarcode, 1, 2);
            this.tlpLoad.Controls.Add(this.lblAutoAlign, 0, 3);
            this.tlpLoad.Controls.Add(this._cbAutoAlign, 1, 3);
            this.tlpLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpLoad.Location = new System.Drawing.Point(6, 25);
            this.tlpLoad.Margin = new System.Windows.Forms.Padding(0);
            this.tlpLoad.Name = "tlpLoad";
            this.tlpLoad.RowCount = 5;
            this.tlpLoad.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpLoad.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpLoad.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpLoad.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpLoad.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpLoad.Size = new System.Drawing.Size(527, 145);
            this.tlpLoad.TabIndex = 0;
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
            this._cbRole.Size = new System.Drawing.Size(301, 28);
            this._cbRole.TabIndex = 2;
            // 
            // lblAlignPts
            // 
            this.lblAlignPts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAlignPts.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblAlignPts.Location = new System.Drawing.Point(3, 34);
            this.lblAlignPts.Name = "lblAlignPts";
            this.lblAlignPts.Size = new System.Drawing.Size(214, 34);
            this.lblAlignPts.TabIndex = 3;
            this.lblAlignPts.Text = "Alignment points";
            this.lblAlignPts.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nAlignPts
            // 
            this._nAlignPts.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nAlignPts.Font = new System.Drawing.Font("Consolas", 10F);
            this._nAlignPts.Location = new System.Drawing.Point(223, 39);
            this._nAlignPts.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._nAlignPts.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this._nAlignPts.Name = "_nAlignPts";
            this._nAlignPts.Size = new System.Drawing.Size(301, 27);
            this._nAlignPts.TabIndex = 4;
            // 
            // lblAutoBarcode
            // 
            this.lblAutoBarcode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAutoBarcode.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblAutoBarcode.Location = new System.Drawing.Point(3, 68);
            this.lblAutoBarcode.Name = "lblAutoBarcode";
            this.lblAutoBarcode.Size = new System.Drawing.Size(214, 34);
            this.lblAutoBarcode.TabIndex = 5;
            this.lblAutoBarcode.Text = "Barcode";
            this.lblAutoBarcode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbAutoBarcode
            // 
            this._cbAutoBarcode.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbAutoBarcode.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbAutoBarcode.Location = new System.Drawing.Point(223, 71);
            this._cbAutoBarcode.Name = "_cbAutoBarcode";
            this._cbAutoBarcode.Size = new System.Drawing.Size(301, 28);
            this._cbAutoBarcode.TabIndex = 6;
            this._cbAutoBarcode.Text = "Auto barcode read on load";
            // 
            // lblAutoAlign
            // 
            this.lblAutoAlign.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAutoAlign.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblAutoAlign.Location = new System.Drawing.Point(3, 102);
            this.lblAutoAlign.Name = "lblAutoAlign";
            this.lblAutoAlign.Size = new System.Drawing.Size(214, 34);
            this.lblAutoAlign.TabIndex = 7;
            this.lblAutoAlign.Text = "Alignment";
            this.lblAutoAlign.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbAutoAlign
            // 
            this._cbAutoAlign.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbAutoAlign.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbAutoAlign.Location = new System.Drawing.Point(223, 105);
            this._cbAutoAlign.Name = "_cbAutoAlign";
            this._cbAutoAlign.Size = new System.Drawing.Size(301, 28);
            this._cbAutoAlign.TabIndex = 8;
            this._cbAutoAlign.Text = "Auto alignment on load";
            // 
            // LoadTapeFrameSubsetPage
            // 
            this.Name = "LoadTapeFrameSubsetPage";
            this.Size = new System.Drawing.Size(1094, 742);
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this._editorPanel.ResumeLayout(false);
            this.editorLayout.ResumeLayout(false);
            this.grpLoad.ResumeLayout(false);
            this.tlpLoad.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nAlignPts)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
