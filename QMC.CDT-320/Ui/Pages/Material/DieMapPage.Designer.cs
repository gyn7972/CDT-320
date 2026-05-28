using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Material
{
    partial class DieMapPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private DieMapView _view;
        private TableLayoutPanel rightLayout;
        private GroupBox grpParams;
        private TableLayoutPanel paramLayout;
        private NumericUpDown _nGridX;
        private NumericUpDown _nGridY;
        private NumericUpDown _nPitchX;
        private NumericUpDown _nPitchY;
        private NumericUpDown _nOriginX;
        private NumericUpDown _nOriginY;
        private ComboBox _cbRotate;
        private Label lblGridX;
        private Label lblGridY;
        private Label lblPitchX;
        private Label lblPitchY;
        private Label lblOriginX;
        private Label lblOriginY;
        private Label lblRotate;
        private GroupBox grpActions;
        private TableLayoutPanel actionLayout;
        private Button btnGenerate;
        private Button btnDemo;
        private Button btnLoad;
        private Button btnSave;
        private Label _lblStats;
        private Label _lblCellInfo;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this._view = new QMC.CDT320.Ui.Controls.DieMapView();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpParams = new System.Windows.Forms.GroupBox();
            this.paramLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblGridX = new System.Windows.Forms.Label();
            this._nGridX = new System.Windows.Forms.NumericUpDown();
            this.lblGridY = new System.Windows.Forms.Label();
            this._nGridY = new System.Windows.Forms.NumericUpDown();
            this.lblPitchX = new System.Windows.Forms.Label();
            this._nPitchX = new System.Windows.Forms.NumericUpDown();
            this.lblPitchY = new System.Windows.Forms.Label();
            this._nPitchY = new System.Windows.Forms.NumericUpDown();
            this.lblOriginX = new System.Windows.Forms.Label();
            this._nOriginX = new System.Windows.Forms.NumericUpDown();
            this.lblOriginY = new System.Windows.Forms.Label();
            this._nOriginY = new System.Windows.Forms.NumericUpDown();
            this.lblRotate = new System.Windows.Forms.Label();
            this._cbRotate = new System.Windows.Forms.ComboBox();
            this.grpActions = new System.Windows.Forms.GroupBox();
            this.actionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnDemo = new System.Windows.Forms.Button();
            this.btnLoad = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this._lblStats = new System.Windows.Forms.Label();
            this._lblCellInfo = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.grpParams.SuspendLayout();
            this.paramLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nOriginX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nOriginY)).BeginInit();
            this.grpActions.SuspendLayout();
            this.actionLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1400, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(3, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1394, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:material.diemap";
            this.lblHeader.Text = "¥Ÿ¿Ã ∏ ";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.contentLayout.Controls.Add(this._view, 0, 0);
            this.contentLayout.Controls.Add(this.rightLayout, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(3, 33);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(10);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1394, 864);
            this.contentLayout.TabIndex = 1;
            // 
            // _view
            // 
            this._view.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this._view.Caption = "Die Map";
            this._view.Dock = System.Windows.Forms.DockStyle.Fill;
            this._view.Location = new System.Drawing.Point(10, 10);
            this._view.Map = null;
            this._view.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this._view.Name = "_view";
            this._view.Size = new System.Drawing.Size(896, 844);
            this._view.TabIndex = 0;
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.grpParams, 0, 0);
            this.rightLayout.Controls.Add(this.grpActions, 0, 1);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(919, 13);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 3;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 330F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(462, 838);
            this.rightLayout.TabIndex = 1;
            // 
            // grpParams
            // 
            this.grpParams.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpParams.Controls.Add(this.paramLayout);
            this.grpParams.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpParams.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F, System.Drawing.FontStyle.Bold);
            this.grpParams.Location = new System.Drawing.Point(3, 3);
            this.grpParams.Name = "grpParams";
            this.grpParams.Size = new System.Drawing.Size(456, 324);
            this.grpParams.TabIndex = 0;
            this.grpParams.TabStop = false;
            this.grpParams.Text = "TapeFrame parameters";
            // 
            // paramLayout
            // 
            this.paramLayout.ColumnCount = 2;
            this.paramLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.paramLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.paramLayout.Controls.Add(this.lblGridX, 0, 0);
            this.paramLayout.Controls.Add(this._nGridX, 1, 0);
            this.paramLayout.Controls.Add(this.lblGridY, 0, 1);
            this.paramLayout.Controls.Add(this._nGridY, 1, 1);
            this.paramLayout.Controls.Add(this.lblPitchX, 0, 2);
            this.paramLayout.Controls.Add(this._nPitchX, 1, 2);
            this.paramLayout.Controls.Add(this.lblPitchY, 0, 3);
            this.paramLayout.Controls.Add(this._nPitchY, 1, 3);
            this.paramLayout.Controls.Add(this.lblOriginX, 0, 4);
            this.paramLayout.Controls.Add(this._nOriginX, 1, 4);
            this.paramLayout.Controls.Add(this.lblOriginY, 0, 5);
            this.paramLayout.Controls.Add(this._nOriginY, 1, 5);
            this.paramLayout.Controls.Add(this.lblRotate, 0, 6);
            this.paramLayout.Controls.Add(this._cbRotate, 1, 6);
            this.paramLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.paramLayout.Location = new System.Drawing.Point(3, 28);
            this.paramLayout.Name = "paramLayout";
            this.paramLayout.Padding = new System.Windows.Forms.Padding(18, 18, 18, 12);
            this.paramLayout.RowCount = 8;
            this.paramLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.paramLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.paramLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.paramLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.paramLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.paramLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.paramLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.paramLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.paramLayout.Size = new System.Drawing.Size(450, 293);
            this.paramLayout.TabIndex = 0;
            // 
            // lblGridX
            // 
            this.lblGridX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGridX.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F);
            this.lblGridX.Location = new System.Drawing.Point(21, 18);
            this.lblGridX.Name = "lblGridX";
            this.lblGridX.Size = new System.Drawing.Size(144, 36);
            this.lblGridX.TabIndex = 0;
            this.lblGridX.Text = "Grid X";
            this.lblGridX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nGridX
            // 
            this._nGridX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridX.Font = new System.Drawing.Font("Consolas", 10F);
            this._nGridX.Location = new System.Drawing.Point(171, 21);
            this._nGridX.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this._nGridX.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nGridX.Name = "_nGridX";
            this._nGridX.Size = new System.Drawing.Size(258, 27);
            this._nGridX.TabIndex = 1;
            this._nGridX.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // lblGridY
            // 
            this.lblGridY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGridY.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F);
            this.lblGridY.Location = new System.Drawing.Point(21, 54);
            this.lblGridY.Name = "lblGridY";
            this.lblGridY.Size = new System.Drawing.Size(144, 36);
            this.lblGridY.TabIndex = 2;
            this.lblGridY.Text = "Grid Y";
            this.lblGridY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nGridY
            // 
            this._nGridY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridY.Font = new System.Drawing.Font("Consolas", 10F);
            this._nGridY.Location = new System.Drawing.Point(171, 57);
            this._nGridY.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this._nGridY.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nGridY.Name = "_nGridY";
            this._nGridY.Size = new System.Drawing.Size(258, 27);
            this._nGridY.TabIndex = 3;
            this._nGridY.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // lblPitchX
            // 
            this.lblPitchX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchX.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F);
            this.lblPitchX.Location = new System.Drawing.Point(21, 90);
            this.lblPitchX.Name = "lblPitchX";
            this.lblPitchX.Size = new System.Drawing.Size(144, 36);
            this.lblPitchX.TabIndex = 4;
            this.lblPitchX.Text = "Pitch X (mm)";
            this.lblPitchX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nPitchX
            // 
            this._nPitchX.DecimalPlaces = 3;
            this._nPitchX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPitchX.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPitchX.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this._nPitchX.Location = new System.Drawing.Point(171, 93);
            this._nPitchX.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nPitchX.Name = "_nPitchX";
            this._nPitchX.Size = new System.Drawing.Size(258, 27);
            this._nPitchX.TabIndex = 5;
            this._nPitchX.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblPitchY
            // 
            this.lblPitchY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchY.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F);
            this.lblPitchY.Location = new System.Drawing.Point(21, 126);
            this.lblPitchY.Name = "lblPitchY";
            this.lblPitchY.Size = new System.Drawing.Size(144, 36);
            this.lblPitchY.TabIndex = 6;
            this.lblPitchY.Text = "Pitch Y (mm)";
            this.lblPitchY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nPitchY
            // 
            this._nPitchY.DecimalPlaces = 3;
            this._nPitchY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPitchY.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPitchY.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this._nPitchY.Location = new System.Drawing.Point(171, 129);
            this._nPitchY.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nPitchY.Name = "_nPitchY";
            this._nPitchY.Size = new System.Drawing.Size(258, 27);
            this._nPitchY.TabIndex = 7;
            this._nPitchY.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblOriginX
            // 
            this.lblOriginX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOriginX.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F);
            this.lblOriginX.Location = new System.Drawing.Point(21, 162);
            this.lblOriginX.Name = "lblOriginX";
            this.lblOriginX.Size = new System.Drawing.Size(144, 36);
            this.lblOriginX.TabIndex = 8;
            this.lblOriginX.Text = "Origin X (mm)";
            this.lblOriginX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nOriginX
            // 
            this._nOriginX.DecimalPlaces = 3;
            this._nOriginX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nOriginX.Font = new System.Drawing.Font("Consolas", 10F);
            this._nOriginX.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this._nOriginX.Location = new System.Drawing.Point(171, 165);
            this._nOriginX.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nOriginX.Name = "_nOriginX";
            this._nOriginX.Size = new System.Drawing.Size(258, 27);
            this._nOriginX.TabIndex = 9;
            // 
            // lblOriginY
            // 
            this.lblOriginY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOriginY.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F);
            this.lblOriginY.Location = new System.Drawing.Point(21, 198);
            this.lblOriginY.Name = "lblOriginY";
            this.lblOriginY.Size = new System.Drawing.Size(144, 36);
            this.lblOriginY.TabIndex = 10;
            this.lblOriginY.Text = "Origin Y (mm)";
            this.lblOriginY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nOriginY
            // 
            this._nOriginY.DecimalPlaces = 3;
            this._nOriginY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nOriginY.Font = new System.Drawing.Font("Consolas", 10F);
            this._nOriginY.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this._nOriginY.Location = new System.Drawing.Point(171, 201);
            this._nOriginY.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nOriginY.Name = "_nOriginY";
            this._nOriginY.Size = new System.Drawing.Size(258, 27);
            this._nOriginY.TabIndex = 11;
            // 
            // lblRotate
            // 
            this.lblRotate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRotate.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F);
            this.lblRotate.Location = new System.Drawing.Point(21, 234);
            this.lblRotate.Name = "lblRotate";
            this.lblRotate.Size = new System.Drawing.Size(144, 36);
            this.lblRotate.TabIndex = 12;
            this.lblRotate.Text = "Rotate";
            this.lblRotate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbRotate
            // 
            this._cbRotate.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbRotate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbRotate.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbRotate.Location = new System.Drawing.Point(171, 237);
            this._cbRotate.Name = "_cbRotate";
            this._cbRotate.Size = new System.Drawing.Size(258, 28);
            this._cbRotate.TabIndex = 13;
            // 
            // grpActions
            // 
            this.grpActions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpActions.Controls.Add(this.actionLayout);
            this.grpActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpActions.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F, System.Drawing.FontStyle.Bold);
            this.grpActions.Location = new System.Drawing.Point(3, 333);
            this.grpActions.Name = "grpActions";
            this.grpActions.Size = new System.Drawing.Size(456, 214);
            this.grpActions.TabIndex = 1;
            this.grpActions.TabStop = false;
            this.grpActions.Text = "Actions";
            // 
            // actionLayout
            // 
            this.actionLayout.ColumnCount = 2;
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionLayout.Controls.Add(this.btnGenerate, 0, 0);
            this.actionLayout.Controls.Add(this.btnDemo, 1, 0);
            this.actionLayout.Controls.Add(this.btnLoad, 0, 1);
            this.actionLayout.Controls.Add(this.btnSave, 1, 1);
            this.actionLayout.Controls.Add(this._lblStats, 0, 2);
            this.actionLayout.Controls.Add(this._lblCellInfo, 0, 3);
            this.actionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionLayout.Location = new System.Drawing.Point(3, 28);
            this.actionLayout.Name = "actionLayout";
            this.actionLayout.Padding = new System.Windows.Forms.Padding(10, 18, 10, 10);
            this.actionLayout.RowCount = 4;
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.actionLayout.Size = new System.Drawing.Size(450, 183);
            this.actionLayout.TabIndex = 0;
            // 
            // btnGenerate
            // 
            this.btnGenerate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this.btnGenerate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGenerate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGenerate.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F);
            this.btnGenerate.ForeColor = System.Drawing.Color.White;
            this.btnGenerate.Location = new System.Drawing.Point(13, 21);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(209, 38);
            this.btnGenerate.TabIndex = 0;
            this.btnGenerate.Text = "GENERATE";
            this.btnGenerate.UseVisualStyleBackColor = false;
            // 
            // btnDemo
            // 
            this.btnDemo.BackColor = System.Drawing.Color.White;
            this.btnDemo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDemo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDemo.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F);
            this.btnDemo.ForeColor = System.Drawing.Color.Black;
            this.btnDemo.Location = new System.Drawing.Point(228, 21);
            this.btnDemo.Name = "btnDemo";
            this.btnDemo.Size = new System.Drawing.Size(209, 38);
            this.btnDemo.TabIndex = 1;
            this.btnDemo.Text = "FILL DEMO RESULTS";
            this.btnDemo.UseVisualStyleBackColor = false;
            // 
            // btnLoad
            // 
            this.btnLoad.BackColor = System.Drawing.Color.White;
            this.btnLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoad.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F);
            this.btnLoad.ForeColor = System.Drawing.Color.Black;
            this.btnLoad.Location = new System.Drawing.Point(13, 65);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(209, 38);
            this.btnLoad.TabIndex = 2;
            this.btnLoad.Text = "LOAD JSON/CSV";
            this.btnLoad.UseVisualStyleBackColor = false;
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.White;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("∏º¿∫ ∞ÌµÒ", 11F);
            this.btnSave.ForeColor = System.Drawing.Color.Black;
            this.btnSave.Location = new System.Drawing.Point(228, 65);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(209, 38);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "SAVE CSV+JSON";
            this.btnSave.UseVisualStyleBackColor = false;
            // 
            // _lblStats
            // 
            this._lblStats.BackColor = System.Drawing.Color.WhiteSmoke;
            this._lblStats.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.actionLayout.SetColumnSpan(this._lblStats, 2);
            this._lblStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblStats.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblStats.Location = new System.Drawing.Point(13, 106);
            this._lblStats.Name = "_lblStats";
            this._lblStats.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblStats.Size = new System.Drawing.Size(424, 34);
            this._lblStats.TabIndex = 4;
            this._lblStats.Text = "(stats)";
            this._lblStats.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblCellInfo
            // 
            this._lblCellInfo.BackColor = System.Drawing.Color.WhiteSmoke;
            this._lblCellInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.actionLayout.SetColumnSpan(this._lblCellInfo, 2);
            this._lblCellInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblCellInfo.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblCellInfo.Location = new System.Drawing.Point(13, 140);
            this._lblCellInfo.Name = "_lblCellInfo";
            this._lblCellInfo.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblCellInfo.Size = new System.Drawing.Size(424, 34);
            this._lblCellInfo.TabIndex = 5;
            this._lblCellInfo.Text = "(click a cell to inspect)";
            this._lblCellInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // DieMapPage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "DieMapPage";
            this.Size = new System.Drawing.Size(1400, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.grpParams.ResumeLayout(false);
            this.paramLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nOriginX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nOriginY)).EndInit();
            this.grpActions.ResumeLayout(false);
            this.actionLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }

    }
}
