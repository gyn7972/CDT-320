using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class LoadTapeFrameSubsetPage
    {
        private TableLayoutPanel editorLayout;
        private Label lblLoadHeader;
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
            this.lblLoadHeader = new System.Windows.Forms.Label();
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
            ((System.ComponentModel.ISupportInitialize)(this._nAlignPts)).BeginInit();
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
            this.editorLayout.Controls.Add(this.lblLoadHeader, 0, 0);
            this.editorLayout.Controls.Add(this.lblRole, 0, 1);
            this.editorLayout.Controls.Add(this._cbRole, 1, 1);
            this.editorLayout.Controls.Add(this.lblAlignPts, 0, 2);
            this.editorLayout.Controls.Add(this._nAlignPts, 1, 2);
            this.editorLayout.Controls.Add(this.lblAutoBarcode, 0, 3);
            this.editorLayout.Controls.Add(this._cbAutoBarcode, 1, 3);
            this.editorLayout.Controls.Add(this.lblAutoAlign, 0, 4);
            this.editorLayout.Controls.Add(this._cbAutoAlign, 1, 4);
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
            // lblLoadHeader
            // 
            this.lblLoadHeader.BackColor = System.Drawing.Color.LightYellow;
            this.lblLoadHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.editorLayout.SetColumnSpan(this.lblLoadHeader, 2);
            this.lblLoadHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLoadHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblLoadHeader.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblLoadHeader.Location = new System.Drawing.Point(13, 10);
            this.lblLoadHeader.Name = "lblLoadHeader";
            this.lblLoadHeader.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblLoadHeader.Size = new System.Drawing.Size(1052, 30);
            this.lblLoadHeader.TabIndex = 0;
            this.lblLoadHeader.Text = "Load tape frame options";
            this.lblLoadHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRole
            // 
            this.lblRole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRole.Font = new System.Drawing.Font("맑은 고딕", 11F);
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
            this._cbRole.Size = new System.Drawing.Size(832, 23);
            this._cbRole.TabIndex = 2;
            // 
            // lblAlignPts
            // 
            this.lblAlignPts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAlignPts.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblAlignPts.Location = new System.Drawing.Point(13, 74);
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
            this._nAlignPts.Location = new System.Drawing.Point(233, 77);
            this._nAlignPts.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this._nAlignPts.Name = "_nAlignPts";
            this._nAlignPts.Size = new System.Drawing.Size(832, 23);
            this._nAlignPts.TabIndex = 4;
            // 
            // lblAutoBarcode
            // 
            this.lblAutoBarcode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAutoBarcode.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblAutoBarcode.Location = new System.Drawing.Point(13, 108);
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
            this._cbAutoBarcode.Location = new System.Drawing.Point(233, 111);
            this._cbAutoBarcode.Name = "_cbAutoBarcode";
            this._cbAutoBarcode.Size = new System.Drawing.Size(832, 28);
            this._cbAutoBarcode.TabIndex = 6;
            this._cbAutoBarcode.Text = "Auto barcode read on load";
            // 
            // lblAutoAlign
            // 
            this.lblAutoAlign.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAutoAlign.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblAutoAlign.Location = new System.Drawing.Point(13, 142);
            this.lblAutoAlign.Name = "lblAutoAlign";
            this.lblAutoAlign.Size = new System.Drawing.Size(214, 38);
            this.lblAutoAlign.TabIndex = 7;
            this.lblAutoAlign.Text = "Alignment";
            this.lblAutoAlign.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbAutoAlign
            // 
            this._cbAutoAlign.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbAutoAlign.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbAutoAlign.Location = new System.Drawing.Point(233, 145);
            this._cbAutoAlign.Name = "_cbAutoAlign";
            this._cbAutoAlign.Size = new System.Drawing.Size(832, 32);
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
            ((System.ComponentModel.ISupportInitialize)(this._nAlignPts)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
