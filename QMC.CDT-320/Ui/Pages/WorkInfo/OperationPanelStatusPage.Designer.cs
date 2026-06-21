using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class OperationPanelStatusPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private GroupBox grpButtons;
        private GroupBox grpLamps;
        private GroupBox grpResources;
        private GroupBox grpTower;
        private GroupBox grpIonizer;
        private TableLayoutPanel buttonLayout;
        private TableLayoutPanel lampLayout;
        private TableLayoutPanel towerLayout;
        private TableLayoutPanel resourceLayout;
        private TableLayoutPanel ionizerLayout;
        private IndicatorDot _dotStart;
        private IndicatorDot _dotStop;
        private IndicatorDot _dotReset;
        private IndicatorDot _dotEmgF;
        private IndicatorDot _dotEmgL;
        private IndicatorDot _dotEmgR;
        private IndicatorDot _ledStartLamp;
        private IndicatorDot _ledStopLamp;
        private IndicatorDot _ledResetLamp;
        private IndicatorDot _tlRed;
        private IndicatorDot _tlYellow;
        private IndicatorDot _tlGreen;
        private IndicatorDot _ledBuzzer;
        private IndicatorDot _dotCda1;
        private IndicatorDot _dotCda2;
        private IndicatorDot _dotVac1;
        private IndicatorDot _dotVac2;
        private IndicatorDot _dotVac3;
        private IndicatorDot _dotVac4;
        private IndicatorDot _dotIonizer;
        private Label lblStart;
        private Label lblStop;
        private Label lblReset;
        private Label lblEmgF;
        private Label lblEmgL;
        private Label lblEmgR;
        private Label lblStartLamp;
        private Label lblStopLamp;
        private Label lblResetLamp;
        private Label lblTlRed;
        private Label lblTlYellow;
        private Label lblTlGreen;
        private Label lblBuzzer;
        private Label lblCda1;
        private Label lblCda2;
        private Label lblVac1;
        private Label lblVac2;
        private Label lblVac3;
        private Label lblVac4;
        private Label lblIonizer;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpButtons = new System.Windows.Forms.GroupBox();
            this.buttonLayout = new System.Windows.Forms.TableLayoutPanel();
            this._dotStart = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblStart = new System.Windows.Forms.Label();
            this._dotStop = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblStop = new System.Windows.Forms.Label();
            this._dotReset = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblReset = new System.Windows.Forms.Label();
            this._dotEmgF = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblEmgF = new System.Windows.Forms.Label();
            this._dotEmgL = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblEmgL = new System.Windows.Forms.Label();
            this._dotEmgR = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblEmgR = new System.Windows.Forms.Label();
            this.grpLamps = new System.Windows.Forms.GroupBox();
            this.lampLayout = new System.Windows.Forms.TableLayoutPanel();
            this._ledStartLamp = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblStartLamp = new System.Windows.Forms.Label();
            this._ledStopLamp = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblStopLamp = new System.Windows.Forms.Label();
            this._ledResetLamp = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblResetLamp = new System.Windows.Forms.Label();
            this.grpResources = new System.Windows.Forms.GroupBox();
            this.resourceLayout = new System.Windows.Forms.TableLayoutPanel();
            this._dotCda1 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblCda1 = new System.Windows.Forms.Label();
            this._dotCda2 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblCda2 = new System.Windows.Forms.Label();
            this._dotVac1 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblVac1 = new System.Windows.Forms.Label();
            this._dotVac2 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblVac2 = new System.Windows.Forms.Label();
            this._dotVac3 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblVac3 = new System.Windows.Forms.Label();
            this._dotVac4 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblVac4 = new System.Windows.Forms.Label();
            this.grpTower = new System.Windows.Forms.GroupBox();
            this.towerLayout = new System.Windows.Forms.TableLayoutPanel();
            this._tlRed = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblTlRed = new System.Windows.Forms.Label();
            this._tlYellow = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblTlYellow = new System.Windows.Forms.Label();
            this._tlGreen = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblTlGreen = new System.Windows.Forms.Label();
            this._ledBuzzer = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblBuzzer = new System.Windows.Forms.Label();
            this.grpIonizer = new System.Windows.Forms.GroupBox();
            this.ionizerLayout = new System.Windows.Forms.TableLayoutPanel();
            this._dotIonizer = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblIonizer = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.grpButtons.SuspendLayout();
            this.buttonLayout.SuspendLayout();
            this.grpLamps.SuspendLayout();
            this.lampLayout.SuspendLayout();
            this.grpResources.SuspendLayout();
            this.resourceLayout.SuspendLayout();
            this.grpTower.SuspendLayout();
            this.towerLayout.SuspendLayout();
            this.grpIonizer.SuspendLayout();
            this.ionizerLayout.SuspendLayout();
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
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(3, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1394, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:wi.opPanelStatus";
            this.lblHeader.Text = "OPERATION PANEL STATUS";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.contentLayout.Controls.Add(this.grpButtons, 0, 0);
            this.contentLayout.Controls.Add(this.grpLamps, 1, 0);
            this.contentLayout.Controls.Add(this.grpResources, 2, 0);
            this.contentLayout.Controls.Add(this.grpTower, 1, 1);
            this.contentLayout.Controls.Add(this.grpIonizer, 2, 1);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.contentLayout.Location = new System.Drawing.Point(3, 33);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(8);
            this.contentLayout.RowCount = 2;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 162F));
            this.contentLayout.Size = new System.Drawing.Size(1394, 360);
            this.contentLayout.TabIndex = 1;
            // 
            // grpButtons
            // 
            this.grpButtons.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpButtons.Controls.Add(this.buttonLayout);
            this.grpButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpButtons.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpButtons.Location = new System.Drawing.Point(11, 11);
            this.grpButtons.Name = "grpButtons";
            this.grpButtons.Size = new System.Drawing.Size(453, 172);
            this.grpButtons.TabIndex = 0;
            this.grpButtons.TabStop = false;
            this.grpButtons.Text = "OPERATION BUTTONS (DI)";
            // 
            // buttonLayout
            // 
            this.buttonLayout.ColumnCount = 2;
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonLayout.Controls.Add(this._dotStart, 0, 0);
            this.buttonLayout.Controls.Add(this.lblStart, 1, 0);
            this.buttonLayout.Controls.Add(this._dotStop, 0, 1);
            this.buttonLayout.Controls.Add(this.lblStop, 1, 1);
            this.buttonLayout.Controls.Add(this._dotReset, 0, 2);
            this.buttonLayout.Controls.Add(this.lblReset, 1, 2);
            this.buttonLayout.Controls.Add(this._dotEmgF, 0, 3);
            this.buttonLayout.Controls.Add(this.lblEmgF, 1, 3);
            this.buttonLayout.Controls.Add(this._dotEmgL, 0, 4);
            this.buttonLayout.Controls.Add(this.lblEmgL, 1, 4);
            this.buttonLayout.Controls.Add(this._dotEmgR, 0, 5);
            this.buttonLayout.Controls.Add(this.lblEmgR, 1, 5);
            this.buttonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonLayout.Location = new System.Drawing.Point(3, 28);
            this.buttonLayout.Name = "buttonLayout";
            this.buttonLayout.Padding = new System.Windows.Forms.Padding(12, 18, 12, 8);
            this.buttonLayout.RowCount = 7;
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.buttonLayout.Size = new System.Drawing.Size(447, 141);
            this.buttonLayout.TabIndex = 0;
            // 
            // _dotStart
            // 
            this._dotStart.BackColor = System.Drawing.Color.Transparent;
            this._dotStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotStart.Location = new System.Drawing.Point(15, 24);
            this._dotStart.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotStart.Name = "_dotStart";
            this._dotStart.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotStart.OnColor = System.Drawing.Color.LimeGreen;
            this._dotStart.Size = new System.Drawing.Size(22, 14);
            this._dotStart.TabIndex = 0;
            // 
            // lblStart
            // 
            this.lblStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStart.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStart.Location = new System.Drawing.Point(43, 18);
            this.lblStart.Name = "lblStart";
            this.lblStart.Size = new System.Drawing.Size(389, 26);
            this.lblStart.TabIndex = 1;
            this.lblStart.Text = "START";
            this.lblStart.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotStop
            // 
            this._dotStop.BackColor = System.Drawing.Color.Transparent;
            this._dotStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotStop.Location = new System.Drawing.Point(15, 50);
            this._dotStop.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotStop.Name = "_dotStop";
            this._dotStop.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotStop.OnColor = System.Drawing.Color.LimeGreen;
            this._dotStop.Size = new System.Drawing.Size(22, 14);
            this._dotStop.TabIndex = 2;
            // 
            // lblStop
            // 
            this.lblStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStop.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStop.Location = new System.Drawing.Point(43, 44);
            this.lblStop.Name = "lblStop";
            this.lblStop.Size = new System.Drawing.Size(389, 26);
            this.lblStop.TabIndex = 3;
            this.lblStop.Text = "STOP";
            this.lblStop.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotReset
            // 
            this._dotReset.BackColor = System.Drawing.Color.Transparent;
            this._dotReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotReset.Location = new System.Drawing.Point(15, 76);
            this._dotReset.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotReset.Name = "_dotReset";
            this._dotReset.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotReset.OnColor = System.Drawing.Color.LimeGreen;
            this._dotReset.Size = new System.Drawing.Size(22, 14);
            this._dotReset.TabIndex = 4;
            // 
            // lblReset
            // 
            this.lblReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblReset.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblReset.Location = new System.Drawing.Point(43, 70);
            this.lblReset.Name = "lblReset";
            this.lblReset.Size = new System.Drawing.Size(389, 26);
            this.lblReset.TabIndex = 5;
            this.lblReset.Text = "RESET";
            this.lblReset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotEmgF
            // 
            this._dotEmgF.BackColor = System.Drawing.Color.Transparent;
            this._dotEmgF.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotEmgF.Location = new System.Drawing.Point(15, 102);
            this._dotEmgF.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotEmgF.Name = "_dotEmgF";
            this._dotEmgF.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotEmgF.OnColor = System.Drawing.Color.Red;
            this._dotEmgF.Size = new System.Drawing.Size(22, 14);
            this._dotEmgF.TabIndex = 6;
            // 
            // lblEmgF
            // 
            this.lblEmgF.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmgF.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblEmgF.Location = new System.Drawing.Point(43, 96);
            this.lblEmgF.Name = "lblEmgF";
            this.lblEmgF.Size = new System.Drawing.Size(389, 26);
            this.lblEmgF.TabIndex = 7;
            this.lblEmgF.Text = "EMG Front";
            this.lblEmgF.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotEmgL
            // 
            this._dotEmgL.BackColor = System.Drawing.Color.Transparent;
            this._dotEmgL.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotEmgL.Location = new System.Drawing.Point(15, 128);
            this._dotEmgL.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotEmgL.Name = "_dotEmgL";
            this._dotEmgL.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotEmgL.OnColor = System.Drawing.Color.Red;
            this._dotEmgL.Size = new System.Drawing.Size(22, 14);
            this._dotEmgL.TabIndex = 8;
            // 
            // lblEmgL
            // 
            this.lblEmgL.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmgL.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblEmgL.Location = new System.Drawing.Point(43, 122);
            this.lblEmgL.Name = "lblEmgL";
            this.lblEmgL.Size = new System.Drawing.Size(389, 26);
            this.lblEmgL.TabIndex = 9;
            this.lblEmgL.Text = "EMG Left";
            this.lblEmgL.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotEmgR
            // 
            this._dotEmgR.BackColor = System.Drawing.Color.Transparent;
            this._dotEmgR.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotEmgR.Location = new System.Drawing.Point(15, 154);
            this._dotEmgR.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotEmgR.Name = "_dotEmgR";
            this._dotEmgR.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotEmgR.OnColor = System.Drawing.Color.Red;
            this._dotEmgR.Size = new System.Drawing.Size(22, 14);
            this._dotEmgR.TabIndex = 10;
            // 
            // lblEmgR
            // 
            this.lblEmgR.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmgR.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblEmgR.Location = new System.Drawing.Point(43, 148);
            this.lblEmgR.Name = "lblEmgR";
            this.lblEmgR.Size = new System.Drawing.Size(389, 26);
            this.lblEmgR.TabIndex = 11;
            this.lblEmgR.Text = "EMG Rear";
            this.lblEmgR.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpLamps
            // 
            this.grpLamps.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpLamps.Controls.Add(this.lampLayout);
            this.grpLamps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLamps.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpLamps.Location = new System.Drawing.Point(470, 11);
            this.grpLamps.Name = "grpLamps";
            this.grpLamps.Size = new System.Drawing.Size(453, 172);
            this.grpLamps.TabIndex = 1;
            this.grpLamps.TabStop = false;
            this.grpLamps.Text = "OPERATION LAMPS (DO)";
            // 
            // lampLayout
            // 
            this.lampLayout.ColumnCount = 2;
            this.lampLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.lampLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lampLayout.Controls.Add(this._ledStartLamp, 0, 0);
            this.lampLayout.Controls.Add(this.lblStartLamp, 1, 0);
            this.lampLayout.Controls.Add(this._ledStopLamp, 0, 1);
            this.lampLayout.Controls.Add(this.lblStopLamp, 1, 1);
            this.lampLayout.Controls.Add(this._ledResetLamp, 0, 2);
            this.lampLayout.Controls.Add(this.lblResetLamp, 1, 2);
            this.lampLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lampLayout.Location = new System.Drawing.Point(3, 28);
            this.lampLayout.Name = "lampLayout";
            this.lampLayout.Padding = new System.Windows.Forms.Padding(12, 18, 12, 8);
            this.lampLayout.RowCount = 7;
            this.lampLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.lampLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.lampLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.lampLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.lampLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.lampLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.lampLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.lampLayout.Size = new System.Drawing.Size(447, 141);
            this.lampLayout.TabIndex = 0;
            // 
            // _ledStartLamp
            // 
            this._ledStartLamp.BackColor = System.Drawing.Color.Transparent;
            this._ledStartLamp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._ledStartLamp.Location = new System.Drawing.Point(15, 24);
            this._ledStartLamp.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._ledStartLamp.Name = "_ledStartLamp";
            this._ledStartLamp.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._ledStartLamp.OnColor = System.Drawing.Color.LimeGreen;
            this._ledStartLamp.Size = new System.Drawing.Size(22, 14);
            this._ledStartLamp.TabIndex = 0;
            // 
            // lblStartLamp
            // 
            this.lblStartLamp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStartLamp.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStartLamp.Location = new System.Drawing.Point(43, 18);
            this.lblStartLamp.Name = "lblStartLamp";
            this.lblStartLamp.Size = new System.Drawing.Size(389, 26);
            this.lblStartLamp.TabIndex = 1;
            this.lblStartLamp.Text = "START LAMP";
            this.lblStartLamp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ledStopLamp
            // 
            this._ledStopLamp.BackColor = System.Drawing.Color.Transparent;
            this._ledStopLamp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._ledStopLamp.Location = new System.Drawing.Point(15, 50);
            this._ledStopLamp.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._ledStopLamp.Name = "_ledStopLamp";
            this._ledStopLamp.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._ledStopLamp.OnColor = System.Drawing.Color.LimeGreen;
            this._ledStopLamp.Size = new System.Drawing.Size(22, 14);
            this._ledStopLamp.TabIndex = 2;
            // 
            // lblStopLamp
            // 
            this.lblStopLamp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStopLamp.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStopLamp.Location = new System.Drawing.Point(43, 44);
            this.lblStopLamp.Name = "lblStopLamp";
            this.lblStopLamp.Size = new System.Drawing.Size(389, 26);
            this.lblStopLamp.TabIndex = 3;
            this.lblStopLamp.Text = "STOP LAMP";
            this.lblStopLamp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ledResetLamp
            // 
            this._ledResetLamp.BackColor = System.Drawing.Color.Transparent;
            this._ledResetLamp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._ledResetLamp.Location = new System.Drawing.Point(15, 76);
            this._ledResetLamp.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._ledResetLamp.Name = "_ledResetLamp";
            this._ledResetLamp.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._ledResetLamp.OnColor = System.Drawing.Color.LimeGreen;
            this._ledResetLamp.Size = new System.Drawing.Size(22, 14);
            this._ledResetLamp.TabIndex = 4;
            // 
            // lblResetLamp
            // 
            this.lblResetLamp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblResetLamp.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblResetLamp.Location = new System.Drawing.Point(43, 70);
            this.lblResetLamp.Name = "lblResetLamp";
            this.lblResetLamp.Size = new System.Drawing.Size(389, 26);
            this.lblResetLamp.TabIndex = 5;
            this.lblResetLamp.Text = "RESET LAMP";
            this.lblResetLamp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpResources
            // 
            this.grpResources.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpResources.Controls.Add(this.resourceLayout);
            this.grpResources.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpResources.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpResources.Location = new System.Drawing.Point(929, 11);
            this.grpResources.Name = "grpResources";
            this.grpResources.Size = new System.Drawing.Size(454, 172);
            this.grpResources.TabIndex = 2;
            this.grpResources.TabStop = false;
            this.grpResources.Text = "RESOURCE SENSORS";
            // 
            // resourceLayout
            // 
            this.resourceLayout.ColumnCount = 2;
            this.resourceLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.resourceLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.resourceLayout.Controls.Add(this._dotCda1, 0, 0);
            this.resourceLayout.Controls.Add(this.lblCda1, 1, 0);
            this.resourceLayout.Controls.Add(this._dotCda2, 0, 1);
            this.resourceLayout.Controls.Add(this.lblCda2, 1, 1);
            this.resourceLayout.Controls.Add(this._dotVac1, 0, 2);
            this.resourceLayout.Controls.Add(this.lblVac1, 1, 2);
            this.resourceLayout.Controls.Add(this._dotVac2, 0, 3);
            this.resourceLayout.Controls.Add(this.lblVac2, 1, 3);
            this.resourceLayout.Controls.Add(this._dotVac3, 0, 4);
            this.resourceLayout.Controls.Add(this.lblVac3, 1, 4);
            this.resourceLayout.Controls.Add(this._dotVac4, 0, 5);
            this.resourceLayout.Controls.Add(this.lblVac4, 1, 5);
            this.resourceLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resourceLayout.Location = new System.Drawing.Point(3, 28);
            this.resourceLayout.Name = "resourceLayout";
            this.resourceLayout.Padding = new System.Windows.Forms.Padding(12, 18, 12, 8);
            this.resourceLayout.RowCount = 7;
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.resourceLayout.Size = new System.Drawing.Size(448, 141);
            this.resourceLayout.TabIndex = 0;
            // 
            // _dotCda1
            // 
            this._dotCda1.BackColor = System.Drawing.Color.Transparent;
            this._dotCda1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotCda1.Location = new System.Drawing.Point(15, 24);
            this._dotCda1.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotCda1.Name = "_dotCda1";
            this._dotCda1.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotCda1.OnColor = System.Drawing.Color.Cyan;
            this._dotCda1.Size = new System.Drawing.Size(22, 14);
            this._dotCda1.TabIndex = 0;
            // 
            // lblCda1
            // 
            this.lblCda1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCda1.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCda1.Location = new System.Drawing.Point(43, 18);
            this.lblCda1.Name = "lblCda1";
            this.lblCda1.Size = new System.Drawing.Size(390, 26);
            this.lblCda1.TabIndex = 1;
            this.lblCda1.Text = "MAIN CDA 1";
            this.lblCda1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotCda2
            // 
            this._dotCda2.BackColor = System.Drawing.Color.Transparent;
            this._dotCda2.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotCda2.Location = new System.Drawing.Point(15, 50);
            this._dotCda2.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotCda2.Name = "_dotCda2";
            this._dotCda2.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotCda2.OnColor = System.Drawing.Color.Cyan;
            this._dotCda2.Size = new System.Drawing.Size(22, 14);
            this._dotCda2.TabIndex = 2;
            // 
            // lblCda2
            // 
            this.lblCda2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCda2.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCda2.Location = new System.Drawing.Point(43, 44);
            this.lblCda2.Name = "lblCda2";
            this.lblCda2.Size = new System.Drawing.Size(390, 26);
            this.lblCda2.TabIndex = 3;
            this.lblCda2.Text = "MAIN CDA 2";
            this.lblCda2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotVac1
            // 
            this._dotVac1.BackColor = System.Drawing.Color.Transparent;
            this._dotVac1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotVac1.Location = new System.Drawing.Point(15, 76);
            this._dotVac1.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotVac1.Name = "_dotVac1";
            this._dotVac1.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotVac1.OnColor = System.Drawing.Color.LimeGreen;
            this._dotVac1.Size = new System.Drawing.Size(22, 14);
            this._dotVac1.TabIndex = 4;
            // 
            // lblVac1
            // 
            this.lblVac1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVac1.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblVac1.Location = new System.Drawing.Point(43, 70);
            this.lblVac1.Name = "lblVac1";
            this.lblVac1.Size = new System.Drawing.Size(390, 26);
            this.lblVac1.TabIndex = 5;
            this.lblVac1.Text = "VACUUM 1";
            this.lblVac1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotVac2
            // 
            this._dotVac2.BackColor = System.Drawing.Color.Transparent;
            this._dotVac2.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotVac2.Location = new System.Drawing.Point(15, 102);
            this._dotVac2.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotVac2.Name = "_dotVac2";
            this._dotVac2.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotVac2.OnColor = System.Drawing.Color.LimeGreen;
            this._dotVac2.Size = new System.Drawing.Size(22, 14);
            this._dotVac2.TabIndex = 6;
            // 
            // lblVac2
            // 
            this.lblVac2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVac2.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblVac2.Location = new System.Drawing.Point(43, 96);
            this.lblVac2.Name = "lblVac2";
            this.lblVac2.Size = new System.Drawing.Size(390, 26);
            this.lblVac2.TabIndex = 7;
            this.lblVac2.Text = "VACUUM 2";
            this.lblVac2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotVac3
            // 
            this._dotVac3.BackColor = System.Drawing.Color.Transparent;
            this._dotVac3.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotVac3.Location = new System.Drawing.Point(15, 128);
            this._dotVac3.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotVac3.Name = "_dotVac3";
            this._dotVac3.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotVac3.OnColor = System.Drawing.Color.LimeGreen;
            this._dotVac3.Size = new System.Drawing.Size(22, 14);
            this._dotVac3.TabIndex = 8;
            // 
            // lblVac3
            // 
            this.lblVac3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVac3.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblVac3.Location = new System.Drawing.Point(43, 122);
            this.lblVac3.Name = "lblVac3";
            this.lblVac3.Size = new System.Drawing.Size(390, 26);
            this.lblVac3.TabIndex = 9;
            this.lblVac3.Text = "VACUUM 3";
            this.lblVac3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotVac4
            // 
            this._dotVac4.BackColor = System.Drawing.Color.Transparent;
            this._dotVac4.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotVac4.Location = new System.Drawing.Point(15, 154);
            this._dotVac4.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotVac4.Name = "_dotVac4";
            this._dotVac4.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotVac4.OnColor = System.Drawing.Color.LimeGreen;
            this._dotVac4.Size = new System.Drawing.Size(22, 14);
            this._dotVac4.TabIndex = 10;
            // 
            // lblVac4
            // 
            this.lblVac4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVac4.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblVac4.Location = new System.Drawing.Point(43, 148);
            this.lblVac4.Name = "lblVac4";
            this.lblVac4.Size = new System.Drawing.Size(390, 26);
            this.lblVac4.TabIndex = 11;
            this.lblVac4.Text = "VACUUM 4";
            this.lblVac4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpTower
            // 
            this.grpTower.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpTower.Controls.Add(this.towerLayout);
            this.grpTower.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpTower.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpTower.Location = new System.Drawing.Point(470, 189);
            this.grpTower.Name = "grpTower";
            this.grpTower.Size = new System.Drawing.Size(453, 160);
            this.grpTower.TabIndex = 3;
            this.grpTower.TabStop = false;
            this.grpTower.Text = "TOWER LAMP + BUZZER";
            // 
            // towerLayout
            // 
            this.towerLayout.ColumnCount = 2;
            this.towerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.towerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.towerLayout.Controls.Add(this._tlRed, 0, 0);
            this.towerLayout.Controls.Add(this.lblTlRed, 1, 0);
            this.towerLayout.Controls.Add(this._tlYellow, 0, 1);
            this.towerLayout.Controls.Add(this.lblTlYellow, 1, 1);
            this.towerLayout.Controls.Add(this._tlGreen, 0, 2);
            this.towerLayout.Controls.Add(this.lblTlGreen, 1, 2);
            this.towerLayout.Controls.Add(this._ledBuzzer, 0, 3);
            this.towerLayout.Controls.Add(this.lblBuzzer, 1, 3);
            this.towerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.towerLayout.Location = new System.Drawing.Point(3, 28);
            this.towerLayout.Name = "towerLayout";
            this.towerLayout.Padding = new System.Windows.Forms.Padding(12, 18, 12, 8);
            this.towerLayout.RowCount = 7;
            this.towerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.towerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.towerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.towerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.towerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.towerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.towerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.towerLayout.Size = new System.Drawing.Size(447, 129);
            this.towerLayout.TabIndex = 0;
            // 
            // _tlRed
            // 
            this._tlRed.BackColor = System.Drawing.Color.Transparent;
            this._tlRed.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tlRed.Location = new System.Drawing.Point(15, 24);
            this._tlRed.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._tlRed.Name = "_tlRed";
            this._tlRed.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._tlRed.OnColor = System.Drawing.Color.Red;
            this._tlRed.Size = new System.Drawing.Size(22, 14);
            this._tlRed.TabIndex = 0;
            // 
            // lblTlRed
            // 
            this.lblTlRed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTlRed.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblTlRed.Location = new System.Drawing.Point(43, 18);
            this.lblTlRed.Name = "lblTlRed";
            this.lblTlRed.Size = new System.Drawing.Size(389, 26);
            this.lblTlRed.TabIndex = 1;
            this.lblTlRed.Text = "RED";
            this.lblTlRed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tlYellow
            // 
            this._tlYellow.BackColor = System.Drawing.Color.Transparent;
            this._tlYellow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tlYellow.Location = new System.Drawing.Point(15, 50);
            this._tlYellow.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._tlYellow.Name = "_tlYellow";
            this._tlYellow.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._tlYellow.OnColor = System.Drawing.Color.Goldenrod;
            this._tlYellow.Size = new System.Drawing.Size(22, 14);
            this._tlYellow.TabIndex = 2;
            // 
            // lblTlYellow
            // 
            this.lblTlYellow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTlYellow.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblTlYellow.Location = new System.Drawing.Point(43, 44);
            this.lblTlYellow.Name = "lblTlYellow";
            this.lblTlYellow.Size = new System.Drawing.Size(389, 26);
            this.lblTlYellow.TabIndex = 3;
            this.lblTlYellow.Text = "YELLOW";
            this.lblTlYellow.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tlGreen
            // 
            this._tlGreen.BackColor = System.Drawing.Color.Transparent;
            this._tlGreen.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tlGreen.Location = new System.Drawing.Point(15, 76);
            this._tlGreen.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._tlGreen.Name = "_tlGreen";
            this._tlGreen.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._tlGreen.OnColor = System.Drawing.Color.LimeGreen;
            this._tlGreen.Size = new System.Drawing.Size(22, 14);
            this._tlGreen.TabIndex = 4;
            // 
            // lblTlGreen
            // 
            this.lblTlGreen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTlGreen.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblTlGreen.Location = new System.Drawing.Point(43, 70);
            this.lblTlGreen.Name = "lblTlGreen";
            this.lblTlGreen.Size = new System.Drawing.Size(389, 26);
            this.lblTlGreen.TabIndex = 5;
            this.lblTlGreen.Text = "GREEN";
            this.lblTlGreen.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ledBuzzer
            // 
            this._ledBuzzer.BackColor = System.Drawing.Color.Transparent;
            this._ledBuzzer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._ledBuzzer.Location = new System.Drawing.Point(15, 102);
            this._ledBuzzer.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._ledBuzzer.Name = "_ledBuzzer";
            this._ledBuzzer.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._ledBuzzer.OnColor = System.Drawing.Color.Magenta;
            this._ledBuzzer.Size = new System.Drawing.Size(22, 14);
            this._ledBuzzer.TabIndex = 6;
            // 
            // lblBuzzer
            // 
            this.lblBuzzer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBuzzer.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblBuzzer.Location = new System.Drawing.Point(43, 96);
            this.lblBuzzer.Name = "lblBuzzer";
            this.lblBuzzer.Size = new System.Drawing.Size(389, 26);
            this.lblBuzzer.TabIndex = 7;
            this.lblBuzzer.Text = "BUZZER";
            this.lblBuzzer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpIonizer
            // 
            this.grpIonizer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpIonizer.Controls.Add(this.ionizerLayout);
            this.grpIonizer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpIonizer.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpIonizer.Location = new System.Drawing.Point(929, 189);
            this.grpIonizer.Name = "grpIonizer";
            this.grpIonizer.Size = new System.Drawing.Size(454, 160);
            this.grpIonizer.TabIndex = 4;
            this.grpIonizer.TabStop = false;
            this.grpIonizer.Text = "IONIZER";
            // 
            // ionizerLayout
            // 
            this.ionizerLayout.ColumnCount = 2;
            this.ionizerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ionizerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ionizerLayout.Controls.Add(this._dotIonizer, 0, 0);
            this.ionizerLayout.Controls.Add(this.lblIonizer, 1, 0);
            this.ionizerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ionizerLayout.Location = new System.Drawing.Point(3, 28);
            this.ionizerLayout.Name = "ionizerLayout";
            this.ionizerLayout.Padding = new System.Windows.Forms.Padding(12, 18, 12, 8);
            this.ionizerLayout.RowCount = 7;
            this.ionizerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.ionizerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.ionizerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.ionizerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.ionizerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.ionizerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.ionizerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.ionizerLayout.Size = new System.Drawing.Size(448, 129);
            this.ionizerLayout.TabIndex = 0;
            // 
            // _dotIonizer
            // 
            this._dotIonizer.BackColor = System.Drawing.Color.Transparent;
            this._dotIonizer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dotIonizer.Location = new System.Drawing.Point(15, 24);
            this._dotIonizer.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this._dotIonizer.Name = "_dotIonizer";
            this._dotIonizer.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotIonizer.OnColor = System.Drawing.Color.Cyan;
            this._dotIonizer.Size = new System.Drawing.Size(22, 14);
            this._dotIonizer.TabIndex = 0;
            // 
            // lblIonizer
            // 
            this.lblIonizer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblIonizer.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblIonizer.Location = new System.Drawing.Point(43, 18);
            this.lblIonizer.Name = "lblIonizer";
            this.lblIonizer.Size = new System.Drawing.Size(390, 26);
            this.lblIonizer.TabIndex = 1;
            this.lblIonizer.Text = "IONIZER OK";
            this.lblIonizer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // OperationPanelStatusPage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "OperationPanelStatusPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.grpButtons.ResumeLayout(false);
            this.buttonLayout.ResumeLayout(false);
            this.grpLamps.ResumeLayout(false);
            this.lampLayout.ResumeLayout(false);
            this.grpResources.ResumeLayout(false);
            this.resourceLayout.ResumeLayout(false);
            this.grpTower.ResumeLayout(false);
            this.towerLayout.ResumeLayout(false);
            this.grpIonizer.ResumeLayout(false);
            this.ionizerLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}

