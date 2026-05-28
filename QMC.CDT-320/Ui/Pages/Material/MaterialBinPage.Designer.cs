using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Material
{
    partial class MaterialBinPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private Label lblSummary;
        private TableLayoutPanel contentLayout;
        private GroupBox grpCodes;
        private GroupBox grpColors;
        private GroupBox grpTest;
        private TableLayoutPanel codeLayout;
        private TableLayoutPanel colorLayout;
        private TableLayoutPanel rightLayout;
        private TableLayoutPanel testLayout;
        private DataGridView _gridCodes;
        private DataGridView _gridColors;
        private Label lblTestNg;
        private TextBox _tbTestNg;
        private Button _btnTest;
        private Label _lblTestResult;
        private TableLayoutPanel buttonLayout;
        private Button _btnReset;
        private Button _btnSave;

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblSummary = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpCodes = new System.Windows.Forms.GroupBox();
            this.codeLayout = new System.Windows.Forms.TableLayoutPanel();
            this._gridCodes = new System.Windows.Forms.DataGridView();
            this.NgCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Bin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpColors = new System.Windows.Forms.GroupBox();
            this.colorLayout = new System.Windows.Forms.TableLayoutPanel();
            this._gridColors = new System.Windows.Forms.DataGridView();
            this.BinUpper = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Color = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.grpTest = new System.Windows.Forms.GroupBox();
            this.testLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTestNg = new System.Windows.Forms.Label();
            this._tbTestNg = new System.Windows.Forms.TextBox();
            this._btnTest = new System.Windows.Forms.Button();
            this._lblTestResult = new System.Windows.Forms.Label();
            this.buttonLayout = new System.Windows.Forms.TableLayoutPanel();
            this._btnReset = new System.Windows.Forms.Button();
            this._btnSave = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.grpCodes.SuspendLayout();
            this.codeLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._gridCodes)).BeginInit();
            this.rightLayout.SuspendLayout();
            this.grpColors.SuspendLayout();
            this.colorLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._gridColors)).BeginInit();
            this.grpTest.SuspendLayout();
            this.testLayout.SuspendLayout();
            this.buttonLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.lblSummary, 0, 1);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1400, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(3, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1394, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:material.bin";
            this.lblHeader.Text = "ºó ÄÚµå ¸ÅÇÎ";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSummary
            // 
            this.lblSummary.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSummary.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F, System.Drawing.FontStyle.Bold);
            this.lblSummary.ForeColor = System.Drawing.Color.White;
            this.lblSummary.Location = new System.Drawing.Point(3, 30);
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblSummary.Size = new System.Drawing.Size(1394, 36);
            this.lblSummary.TabIndex = 1;
            this.lblSummary.Text = "BinCode Mapping (NG code -> bin number, bin number -> color)";
            this.lblSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.contentLayout.Controls.Add(this.grpCodes, 0, 0);
            this.contentLayout.Controls.Add(this.rightLayout, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(3, 69);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(10);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1394, 828);
            this.contentLayout.TabIndex = 2;
            // 
            // grpCodes
            // 
            this.grpCodes.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpCodes.Controls.Add(this.codeLayout);
            this.grpCodes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCodes.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F, System.Drawing.FontStyle.Bold);
            this.grpCodes.Location = new System.Drawing.Point(13, 13);
            this.grpCodes.Name = "grpCodes";
            this.grpCodes.Size = new System.Drawing.Size(818, 802);
            this.grpCodes.TabIndex = 0;
            this.grpCodes.TabStop = false;
            this.grpCodes.Text = "NG Code -> Bin Number (priority by row order)";
            // 
            // codeLayout
            // 
            this.codeLayout.ColumnCount = 1;
            this.codeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.codeLayout.Controls.Add(this._gridCodes, 0, 0);
            this.codeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.codeLayout.Location = new System.Drawing.Point(3, 28);
            this.codeLayout.Name = "codeLayout";
            this.codeLayout.Padding = new System.Windows.Forms.Padding(10, 12, 10, 10);
            this.codeLayout.RowCount = 1;
            this.codeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.codeLayout.Size = new System.Drawing.Size(812, 771);
            this.codeLayout.TabIndex = 0;
            // 
            // _gridCodes
            // 
            this._gridCodes.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._gridCodes.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Consolas", 10F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            this._gridCodes.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this._gridCodes.ColumnHeadersHeight = 29;
            this._gridCodes.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.NgCode,
            this.Bin});
            this._gridCodes.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gridCodes.EnableHeadersVisualStyles = false;
            this._gridCodes.Font = new System.Drawing.Font("Consolas", 10F);
            this._gridCodes.Location = new System.Drawing.Point(13, 15);
            this._gridCodes.Name = "_gridCodes";
            this._gridCodes.RowHeadersVisible = false;
            this._gridCodes.RowHeadersWidth = 51;
            this._gridCodes.Size = new System.Drawing.Size(786, 743);
            this._gridCodes.TabIndex = 0;
            // 
            // NgCode
            // 
            this.NgCode.HeaderText = "NG Code";
            this.NgCode.MinimumWidth = 6;
            this.NgCode.Name = "NgCode";
            // 
            // Bin
            // 
            this.Bin.HeaderText = "Bin Number";
            this.Bin.MinimumWidth = 6;
            this.Bin.Name = "Bin";
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.grpColors, 0, 0);
            this.rightLayout.Controls.Add(this.grpTest, 0, 1);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(837, 13);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 2;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.rightLayout.Size = new System.Drawing.Size(544, 802);
            this.rightLayout.TabIndex = 1;
            // 
            // grpColors
            // 
            this.grpColors.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpColors.Controls.Add(this.colorLayout);
            this.grpColors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpColors.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F, System.Drawing.FontStyle.Bold);
            this.grpColors.Location = new System.Drawing.Point(3, 3);
            this.grpColors.Name = "grpColors";
            this.grpColors.Size = new System.Drawing.Size(538, 555);
            this.grpColors.TabIndex = 0;
            this.grpColors.TabStop = false;
            this.grpColors.Text = "Bin Number Range -> Color";
            // 
            // colorLayout
            // 
            this.colorLayout.ColumnCount = 1;
            this.colorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.colorLayout.Controls.Add(this._gridColors, 0, 0);
            this.colorLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.colorLayout.Location = new System.Drawing.Point(3, 28);
            this.colorLayout.Name = "colorLayout";
            this.colorLayout.Padding = new System.Windows.Forms.Padding(10, 12, 10, 10);
            this.colorLayout.RowCount = 1;
            this.colorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.colorLayout.Size = new System.Drawing.Size(532, 524);
            this.colorLayout.TabIndex = 0;
            // 
            // _gridColors
            // 
            this._gridColors.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._gridColors.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Consolas", 10F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            this._gridColors.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this._gridColors.ColumnHeadersHeight = 29;
            this._gridColors.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.BinUpper,
            this.Color});
            this._gridColors.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gridColors.EnableHeadersVisualStyles = false;
            this._gridColors.Font = new System.Drawing.Font("Consolas", 10F);
            this._gridColors.Location = new System.Drawing.Point(13, 15);
            this._gridColors.Name = "_gridColors";
            this._gridColors.RowHeadersVisible = false;
            this._gridColors.RowHeadersWidth = 51;
            this._gridColors.Size = new System.Drawing.Size(506, 496);
            this._gridColors.TabIndex = 0;
            // 
            // BinUpper
            // 
            this.BinUpper.HeaderText = "Bin <=";
            this.BinUpper.MinimumWidth = 6;
            this.BinUpper.Name = "BinUpper";
            // 
            // Color
            // 
            this.Color.HeaderText = "Color (#RRGGBB)";
            this.Color.MinimumWidth = 6;
            this.Color.Name = "Color";
            // 
            // grpTest
            // 
            this.grpTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpTest.Controls.Add(this.testLayout);
            this.grpTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpTest.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F, System.Drawing.FontStyle.Bold);
            this.grpTest.Location = new System.Drawing.Point(3, 564);
            this.grpTest.Name = "grpTest";
            this.grpTest.Size = new System.Drawing.Size(538, 235);
            this.grpTest.TabIndex = 1;
            this.grpTest.TabStop = false;
            this.grpTest.Text = "Test mapping";
            // 
            // testLayout
            // 
            this.testLayout.ColumnCount = 3;
            this.testLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.testLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.testLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.testLayout.Controls.Add(this.lblTestNg, 0, 0);
            this.testLayout.Controls.Add(this._tbTestNg, 1, 0);
            this.testLayout.Controls.Add(this._btnTest, 2, 0);
            this.testLayout.Controls.Add(this._lblTestResult, 0, 1);
            this.testLayout.Controls.Add(this.buttonLayout, 0, 2);
            this.testLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.testLayout.Location = new System.Drawing.Point(3, 28);
            this.testLayout.Name = "testLayout";
            this.testLayout.Padding = new System.Windows.Forms.Padding(10, 14, 10, 10);
            this.testLayout.RowCount = 3;
            this.testLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.testLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.testLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.testLayout.Size = new System.Drawing.Size(532, 204);
            this.testLayout.TabIndex = 0;
            // 
            // lblTestNg
            // 
            this.lblTestNg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTestNg.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this.lblTestNg.Location = new System.Drawing.Point(13, 14);
            this.lblTestNg.Name = "lblTestNg";
            this.lblTestNg.Size = new System.Drawing.Size(114, 34);
            this.lblTestNg.TabIndex = 0;
            this.lblTestNg.Text = "NG Codes";
            this.lblTestNg.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbTestNg
            // 
            this._tbTestNg.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbTestNg.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbTestNg.Location = new System.Drawing.Point(133, 17);
            this._tbTestNg.Name = "_tbTestNg";
            this._tbTestNg.Size = new System.Drawing.Size(276, 27);
            this._tbTestNg.TabIndex = 1;
            this._tbTestNg.Text = "ChippingTopOver,ForeignOver";
            // 
            // _btnTest
            // 
            this._btnTest.BackColor = System.Drawing.Color.White;
            this._btnTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnTest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnTest.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this._btnTest.Location = new System.Drawing.Point(415, 17);
            this._btnTest.Name = "_btnTest";
            this._btnTest.Size = new System.Drawing.Size(104, 28);
            this._btnTest.TabIndex = 2;
            this._btnTest.Text = "Test";
            this._btnTest.UseVisualStyleBackColor = false;
            // 
            // _lblTestResult
            // 
            this._lblTestResult.BackColor = System.Drawing.Color.WhiteSmoke;
            this._lblTestResult.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.testLayout.SetColumnSpan(this._lblTestResult, 3);
            this._lblTestResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblTestResult.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Bold);
            this._lblTestResult.Location = new System.Drawing.Point(13, 48);
            this._lblTestResult.Name = "_lblTestResult";
            this._lblTestResult.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblTestResult.Size = new System.Drawing.Size(506, 36);
            this._lblTestResult.TabIndex = 3;
            this._lblTestResult.Text = "(test result)";
            this._lblTestResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonLayout
            // 
            this.buttonLayout.ColumnCount = 2;
            this.testLayout.SetColumnSpan(this.buttonLayout, 3);
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 64F));
            this.buttonLayout.Controls.Add(this._btnReset, 0, 0);
            this.buttonLayout.Controls.Add(this._btnSave, 1, 0);
            this.buttonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonLayout.Location = new System.Drawing.Point(13, 87);
            this.buttonLayout.Name = "buttonLayout";
            this.buttonLayout.RowCount = 1;
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonLayout.Size = new System.Drawing.Size(506, 104);
            this.buttonLayout.TabIndex = 4;
            // 
            // _btnReset
            // 
            this._btnReset.BackColor = System.Drawing.Color.LightYellow;
            this._btnReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnReset.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this._btnReset.Location = new System.Drawing.Point(3, 3);
            this._btnReset.Name = "_btnReset";
            this._btnReset.Size = new System.Drawing.Size(176, 98);
            this._btnReset.TabIndex = 0;
            this._btnReset.Text = "Restore defaults";
            this._btnReset.UseVisualStyleBackColor = false;
            // 
            // _btnSave
            // 
            this._btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSave.Font = new System.Drawing.Font("¸¼Àº °íµñ", 11F);
            this._btnSave.ForeColor = System.Drawing.Color.White;
            this._btnSave.Location = new System.Drawing.Point(185, 3);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Size = new System.Drawing.Size(318, 98);
            this._btnSave.TabIndex = 1;
            this._btnSave.Text = "SAVE (Config/bin_codes.json)";
            this._btnSave.UseVisualStyleBackColor = false;
            // 
            // MaterialBinPage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "MaterialBinPage";
            this.Size = new System.Drawing.Size(1400, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.grpCodes.ResumeLayout(false);
            this.codeLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._gridCodes)).EndInit();
            this.rightLayout.ResumeLayout(false);
            this.grpColors.ResumeLayout(false);
            this.colorLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._gridColors)).EndInit();
            this.grpTest.ResumeLayout(false);
            this.testLayout.ResumeLayout(false);
            this.testLayout.PerformLayout();
            this.buttonLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private DataGridViewTextBoxColumn NgCode;
        private DataGridViewTextBoxColumn Bin;
        private DataGridViewTextBoxColumn BinUpper;
        private DataGridViewTextBoxColumn Color;
    }
}



