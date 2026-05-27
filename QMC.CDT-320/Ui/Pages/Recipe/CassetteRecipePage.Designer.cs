using QMC.CDT_320.Ui.Controls;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class CassetteRecipePage
    {
        private IContainer components = null;

        private Panel panelLeft;
        private Panel panelCenter;
        private Panel panelRight;

        private Label lblHeader;
        private Label lblLeftTitle;
        private Label lblCenterTitle;
        private Label lblRightTitle;

        private ActionButton btnLoadingMove;
        private ActionButton btnUnloadingMove;
        private ActionButton btnReadyMove;

        // Left - extra buttons
        private ActionButton btnSlotLoadingMove;
        private ActionButton btnSlotUnloadingMove;

        // Left - IO area
        private Label lblIoTitle;
        private IndicatorDot dotSensor1;
        private IndicatorDot dotSensor2;
        private IndicatorDot dotProtrusion;
        private IndicatorDot dotMapping;
        private Label lblSensor1;
        private Label lblSensor2;
        private Label lblProtrusion;
        private Label lblMapping;

        // Center - option rows
        private Label lblOptLoadingZKey;
        private Label lblOptLoadingZVal;
        private Label lblOptUnloadingZKey;
        private Label lblOptUnloadingZVal;
        private Label lblOptReadyPosKey;
        private Label lblOptReadyPosVal;
        private Label lblOptMappingZKey;
        private Label lblOptMappingZVal;
        private Label lblOptSlotPitchKey;
        private Label lblOptSlotPitchVal;
        private Label lblOptCassetteGapKey;
        private Label lblOptCassetteGapVal;
        private Label lblOptInchKey;
        private Label lblOptInchVal;
        private Label lblOptStageKey;
        private Label lblOptStageVal;

        // Center - wait section
        private Label lblWaitTitle;
        private Label lblWaitKey;
        private Label lblWaitVal;

        // Right - jog area
        private Label lblJogTitle;
        private Label lblAxisName;
        private Button btnJogPlus;
        private Button btnJogMinus;
        private Button btnJogStop;

        // Right - speed area
        private Label lblSpeedTitle;
        private Label lblSpeedLow;
        private Label lblSpeedMid;
        private Label lblSpeedHigh;
        private TrackBar trkSpeed;
        private Label lblSpeedValue;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new Container();

            this.panelLeft = new Panel();
            this.panelCenter = new Panel();
            this.panelRight = new Panel();

            this.lblHeader = new Label();
            this.lblLeftTitle = new Label();
            this.lblCenterTitle = new Label();
            this.lblRightTitle = new Label();

            this.btnLoadingMove = new ActionButton();
            this.btnUnloadingMove = new ActionButton();
            this.btnReadyMove = new ActionButton();

            this.btnSlotLoadingMove = new ActionButton();
            this.btnSlotUnloadingMove = new ActionButton();

            this.lblIoTitle = new Label();
            this.dotSensor1 = new IndicatorDot();
            this.dotSensor2 = new IndicatorDot();
            this.dotProtrusion = new IndicatorDot();
            this.dotMapping = new IndicatorDot();
            this.lblSensor1 = new Label();
            this.lblSensor2 = new Label();
            this.lblProtrusion = new Label();
            this.lblMapping = new Label();

            this.lblOptLoadingZKey = new Label();
            this.lblOptLoadingZVal = new Label();
            this.lblOptUnloadingZKey = new Label();
            this.lblOptUnloadingZVal = new Label();
            this.lblOptReadyPosKey = new Label();
            this.lblOptReadyPosVal = new Label();
            this.lblOptMappingZKey = new Label();
            this.lblOptMappingZVal = new Label();
            this.lblOptSlotPitchKey = new Label();
            this.lblOptSlotPitchVal = new Label();
            this.lblOptCassetteGapKey = new Label();
            this.lblOptCassetteGapVal = new Label();
            this.lblOptInchKey = new Label();
            this.lblOptInchVal = new Label();
            this.lblOptStageKey = new Label();
            this.lblOptStageVal = new Label();

            this.lblWaitTitle = new Label();
            this.lblWaitKey = new Label();
            this.lblWaitVal = new Label();

            this.lblJogTitle = new Label();
            this.lblAxisName = new Label();
            this.btnJogPlus = new Button();
            this.btnJogMinus = new Button();
            this.btnJogStop = new Button();

            this.lblSpeedTitle = new Label();
            this.lblSpeedLow = new Label();
            this.lblSpeedMid = new Label();
            this.lblSpeedHigh = new Label();
            this.trkSpeed = new TrackBar();
            this.lblSpeedValue = new Label();

            this.SuspendLayout();

            // CassetteRecipePage
            this.AutoScaleMode = AutoScaleMode.None;
            this.BackColor = Color.White;
            this.Name = "CassetteRecipePage";
            this.Size = new Size(1360, 920);

            // lblHeader
            this.lblHeader.BackColor = Color.FromArgb(64, 64, 64);
            this.lblHeader.ForeColor = Color.White;
            this.lblHeader.Font = new Font("¸¼Àº °íµñ", 11F, FontStyle.Bold);
            this.lblHeader.Location = new Point(0, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Size = new Size(1360, 30);
            this.lblHeader.Text = "INPUT/OUTPUT CASSETTE";
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHeader.Padding = new Padding(10, 0, 0, 0);

            // panelLeft
            this.panelLeft.BorderStyle = BorderStyle.FixedSingle;
            this.panelLeft.Location = new Point(8, 36);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Size = new Size(240, 860);
            this.panelLeft.BackColor = Color.WhiteSmoke;

            // panelCenter
            this.panelCenter.BorderStyle = BorderStyle.FixedSingle;
            this.panelCenter.Location = new Point(260, 36);
            this.panelCenter.Name = "panelCenter";
            this.panelCenter.Size = new Size(600, 860);
            this.panelCenter.BackColor = Color.WhiteSmoke;

            // panelRight
            this.panelRight.BorderStyle = BorderStyle.FixedSingle;
            this.panelRight.Location = new Point(880, 36);
            this.panelRight.Name = "panelRight";
            this.panelRight.Size = new Size(460, 860);
            this.panelRight.BackColor = Color.WhiteSmoke;

            // lblLeftTitle
            this.lblLeftTitle.BackColor = Color.Orange;
            this.lblLeftTitle.ForeColor = Color.Black;
            this.lblLeftTitle.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Bold);
            this.lblLeftTitle.Location = new Point(0, 0);
            this.lblLeftTitle.Name = "lblLeftTitle";
            this.lblLeftTitle.Size = new Size(238, 26);
            this.lblLeftTitle.Text = "µ¿ÀÛ";
            this.lblLeftTitle.TextAlign = ContentAlignment.MiddleLeft;
            this.lblLeftTitle.Padding = new Padding(6, 0, 0, 0);

            // lblCenterTitle
            this.lblCenterTitle.BackColor = Color.Orange;
            this.lblCenterTitle.ForeColor = Color.Black;
            this.lblCenterTitle.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Bold);
            this.lblCenterTitle.Location = new Point(0, 0);
            this.lblCenterTitle.Name = "lblCenterTitle";
            this.lblCenterTitle.Size = new Size(598, 26);
            this.lblCenterTitle.Text = "¿É¼Ç";
            this.lblCenterTitle.TextAlign = ContentAlignment.MiddleLeft;
            this.lblCenterTitle.Padding = new Padding(6, 0, 0, 0);

            // lblRightTitle
            this.lblRightTitle.BackColor = Color.Orange;
            this.lblRightTitle.ForeColor = Color.Black;
            this.lblRightTitle.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Bold);
            this.lblRightTitle.Location = new Point(0, 0);
            this.lblRightTitle.Name = "lblRightTitle";
            this.lblRightTitle.Size = new Size(458, 26);
            this.lblRightTitle.Text = "Á¶±×/¼Óµµ";
            this.lblRightTitle.TextAlign = ContentAlignment.MiddleLeft;
            this.lblRightTitle.Padding = new Padding(6, 0, 0, 0);

            // left buttons
            this.btnLoadingMove.Location = new Point(10, 40);
            this.btnLoadingMove.Name = "btnLoadingMove";
            this.btnLoadingMove.Size = new Size(218, 44);
            this.btnLoadingMove.Text = "LOADING À§Ä¡ ÀÌµ¿";

            this.btnUnloadingMove.Location = new Point(10, 92);
            this.btnUnloadingMove.Name = "btnUnloadingMove";
            this.btnUnloadingMove.Size = new Size(218, 44);
            this.btnUnloadingMove.Text = "UNLOADING À§Ä¡ ÀÌµ¿";

            this.btnReadyMove.Location = new Point(10, 144);
            this.btnReadyMove.Name = "btnReadyMove";
            this.btnReadyMove.Size = new Size(218, 44);
            this.btnReadyMove.Text = "ÁØºñ À§Ä¡ ÀÌµ¿";

            this.btnSlotLoadingMove.Location = new Point(10, 380);
            this.btnSlotLoadingMove.Name = "btnSlotLoadingMove";
            this.btnSlotLoadingMove.Size = new Size(218, 44);
            this.btnSlotLoadingMove.Text = "SLOT À§Ä¡ ÀÌµ¿ (LOADING)";

            this.btnSlotUnloadingMove.Location = new Point(10, 432);
            this.btnSlotUnloadingMove.Name = "btnSlotUnloadingMove";
            this.btnSlotUnloadingMove.Size = new Size(218, 44);
            this.btnSlotUnloadingMove.Text = "SLOT À§Ä¡ ÀÌµ¿ (UNLOADING)";

            // lblIoTitle
            this.lblIoTitle.BackColor = Color.Orange;
            this.lblIoTitle.ForeColor = Color.Black;
            this.lblIoTitle.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Bold);
            this.lblIoTitle.Location = new Point(0, 500);
            this.lblIoTitle.Name = "lblIoTitle";
            this.lblIoTitle.Size = new Size(238, 26);
            this.lblIoTitle.Text = "½Ç¸°´õ & I/O";
            this.lblIoTitle.TextAlign = ContentAlignment.MiddleLeft;
            this.lblIoTitle.Padding = new Padding(6, 0, 0, 0);

            // dots
            this.dotSensor1.Location = new Point(10, 540);
            this.dotSensor1.Name = "dotSensor1";
            this.dotSensor1.Size = new Size(12, 12);
            this.dotSensor1.OnColor = Color.LimeGreen;

            this.dotSensor2.Location = new Point(10, 566);
            this.dotSensor2.Name = "dotSensor2";
            this.dotSensor2.Size = new Size(12, 12);
            this.dotSensor2.OnColor = Color.LimeGreen;

            this.dotProtrusion.Location = new Point(10, 592);
            this.dotProtrusion.Name = "dotProtrusion";
            this.dotProtrusion.Size = new Size(12, 12);
            this.dotProtrusion.OnColor = Color.LimeGreen;

            this.dotMapping.Location = new Point(10, 618);
            this.dotMapping.Name = "dotMapping";
            this.dotMapping.Size = new Size(12, 12);
            this.dotMapping.OnColor = Color.LimeGreen;

            // sensor labels
            this.lblSensor1.Location = new Point(28, 534);
            this.lblSensor1.Name = "lblSensor1";
            this.lblSensor1.Size = new Size(198, 22);
            this.lblSensor1.Text = "CASSETTE °¨Áö SENSOR 1";
            this.lblSensor1.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblSensor1.BorderStyle = BorderStyle.FixedSingle;
            this.lblSensor1.TextAlign = ContentAlignment.MiddleLeft;
            this.lblSensor1.Padding = new Padding(6, 0, 0, 0);
            this.lblSensor1.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            this.lblSensor2.Location = new Point(28, 560);
            this.lblSensor2.Name = "lblSensor2";
            this.lblSensor2.Size = new Size(198, 22);
            this.lblSensor2.Text = "CASSETTE °¨Áö SENSOR 2";
            this.lblSensor2.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblSensor2.BorderStyle = BorderStyle.FixedSingle;
            this.lblSensor2.TextAlign = ContentAlignment.MiddleLeft;
            this.lblSensor2.Padding = new Padding(6, 0, 0, 0);
            this.lblSensor2.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            this.lblProtrusion.Location = new Point(28, 586);
            this.lblProtrusion.Name = "lblProtrusion";
            this.lblProtrusion.Size = new Size(198, 22);
            this.lblProtrusion.Text = "µ¹Ãâ °¨Áö SENSOR";
            this.lblProtrusion.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblProtrusion.BorderStyle = BorderStyle.FixedSingle;
            this.lblProtrusion.TextAlign = ContentAlignment.MiddleLeft;
            this.lblProtrusion.Padding = new Padding(6, 0, 0, 0);
            this.lblProtrusion.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            this.lblMapping.Location = new Point(28, 612);
            this.lblMapping.Name = "lblMapping";
            this.lblMapping.Size = new Size(198, 22);
            this.lblMapping.Text = "¸ÊÇÎ¼¾¼­";
            this.lblMapping.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblMapping.BorderStyle = BorderStyle.FixedSingle;
            this.lblMapping.TextAlign = ContentAlignment.MiddleLeft;
            this.lblMapping.Padding = new Padding(6, 0, 0, 0);
            this.lblMapping.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            // Center option rows
            // row1
            this.lblOptLoadingZKey.Location = new Point(10, 40);
            this.lblOptLoadingZKey.Name = "lblOptLoadingZKey";
            this.lblOptLoadingZKey.Size = new Size(200, 26);
            this.lblOptLoadingZKey.Text = "LOADING Z";
            this.lblOptLoadingZKey.BackColor = Color.Gainsboro;
            this.lblOptLoadingZKey.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptLoadingZKey.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptLoadingZKey.Padding = new Padding(6, 0, 0, 0);
            this.lblOptLoadingZKey.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            this.lblOptLoadingZVal.Location = new Point(220, 40);
            this.lblOptLoadingZVal.Name = "lblOptLoadingZVal";
            this.lblOptLoadingZVal.Size = new Size(360, 26);
            this.lblOptLoadingZVal.Text = "121070 um";
            this.lblOptLoadingZVal.BackColor = Color.White;
            this.lblOptLoadingZVal.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptLoadingZVal.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptLoadingZVal.Padding = new Padding(6, 0, 0, 0);
            this.lblOptLoadingZVal.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            // row2
            this.lblOptUnloadingZKey.Location = new Point(10, 72);
            this.lblOptUnloadingZKey.Name = "lblOptUnloadingZKey";
            this.lblOptUnloadingZKey.Size = new Size(200, 26);
            this.lblOptUnloadingZKey.Text = "UNLOADING Z";
            this.lblOptUnloadingZKey.BackColor = Color.Gainsboro;
            this.lblOptUnloadingZKey.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptUnloadingZKey.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptUnloadingZKey.Padding = new Padding(6, 0, 0, 0);
            this.lblOptUnloadingZKey.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            this.lblOptUnloadingZVal.Location = new Point(220, 72);
            this.lblOptUnloadingZVal.Name = "lblOptUnloadingZVal";
            this.lblOptUnloadingZVal.Size = new Size(360, 26);
            this.lblOptUnloadingZVal.Text = "117000 um";
            this.lblOptUnloadingZVal.BackColor = Color.White;
            this.lblOptUnloadingZVal.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptUnloadingZVal.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptUnloadingZVal.Padding = new Padding(6, 0, 0, 0);
            this.lblOptUnloadingZVal.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            // row3
            this.lblOptReadyPosKey.Location = new Point(10, 104);
            this.lblOptReadyPosKey.Name = "lblOptReadyPosKey";
            this.lblOptReadyPosKey.Size = new Size(200, 26);
            this.lblOptReadyPosKey.Text = "READY POSITION";
            this.lblOptReadyPosKey.BackColor = Color.Gainsboro;
            this.lblOptReadyPosKey.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptReadyPosKey.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptReadyPosKey.Padding = new Padding(6, 0, 0, 0);
            this.lblOptReadyPosKey.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            this.lblOptReadyPosVal.Location = new Point(220, 104);
            this.lblOptReadyPosVal.Name = "lblOptReadyPosVal";
            this.lblOptReadyPosVal.Size = new Size(360, 26);
            this.lblOptReadyPosVal.Text = "129771 um";
            this.lblOptReadyPosVal.BackColor = Color.White;
            this.lblOptReadyPosVal.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptReadyPosVal.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptReadyPosVal.Padding = new Padding(6, 0, 0, 0);
            this.lblOptReadyPosVal.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            // row4
            this.lblOptMappingZKey.Location = new Point(10, 136);
            this.lblOptMappingZKey.Name = "lblOptMappingZKey";
            this.lblOptMappingZKey.Size = new Size(200, 26);
            this.lblOptMappingZKey.Text = "MAPPING Z";
            this.lblOptMappingZKey.BackColor = Color.Gainsboro;
            this.lblOptMappingZKey.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptMappingZKey.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptMappingZKey.Padding = new Padding(6, 0, 0, 0);
            this.lblOptMappingZKey.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            this.lblOptMappingZVal.Location = new Point(220, 136);
            this.lblOptMappingZVal.Name = "lblOptMappingZVal";
            this.lblOptMappingZVal.Size = new Size(360, 26);
            this.lblOptMappingZVal.Text = "104745 um";
            this.lblOptMappingZVal.BackColor = Color.White;
            this.lblOptMappingZVal.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptMappingZVal.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptMappingZVal.Padding = new Padding(6, 0, 0, 0);
            this.lblOptMappingZVal.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            // row5
            this.lblOptSlotPitchKey.Location = new Point(10, 168);
            this.lblOptSlotPitchKey.Name = "lblOptSlotPitchKey";
            this.lblOptSlotPitchKey.Size = new Size(200, 26);
            this.lblOptSlotPitchKey.Text = "SLOT PITCH";
            this.lblOptSlotPitchKey.BackColor = Color.Gainsboro;
            this.lblOptSlotPitchKey.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptSlotPitchKey.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptSlotPitchKey.Padding = new Padding(6, 0, 0, 0);
            this.lblOptSlotPitchKey.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            this.lblOptSlotPitchVal.Location = new Point(220, 168);
            this.lblOptSlotPitchVal.Name = "lblOptSlotPitchVal";
            this.lblOptSlotPitchVal.Size = new Size(360, 26);
            this.lblOptSlotPitchVal.Text = "19000 um";
            this.lblOptSlotPitchVal.BackColor = Color.White;
            this.lblOptSlotPitchVal.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptSlotPitchVal.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptSlotPitchVal.Padding = new Padding(6, 0, 0, 0);
            this.lblOptSlotPitchVal.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            // row6
            this.lblOptCassetteGapKey.Location = new Point(10, 200);
            this.lblOptCassetteGapKey.Name = "lblOptCassetteGapKey";
            this.lblOptCassetteGapKey.Size = new Size(200, 26);
            this.lblOptCassetteGapKey.Text = "Ä«¼¼Æ® °£°Ý";
            this.lblOptCassetteGapKey.BackColor = Color.Gainsboro;
            this.lblOptCassetteGapKey.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptCassetteGapKey.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptCassetteGapKey.Padding = new Padding(6, 0, 0, 0);
            this.lblOptCassetteGapKey.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            this.lblOptCassetteGapVal.Location = new Point(220, 200);
            this.lblOptCassetteGapVal.Name = "lblOptCassetteGapVal";
            this.lblOptCassetteGapVal.Size = new Size(360, 26);
            this.lblOptCassetteGapVal.Text = "59000 um";
            this.lblOptCassetteGapVal.BackColor = Color.White;
            this.lblOptCassetteGapVal.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptCassetteGapVal.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptCassetteGapVal.Padding = new Padding(6, 0, 0, 0);
            this.lblOptCassetteGapVal.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            // row7
            this.lblOptInchKey.Location = new Point(10, 232);
            this.lblOptInchKey.Name = "lblOptInchKey";
            this.lblOptInchKey.Size = new Size(200, 26);
            this.lblOptInchKey.Text = "8ÀÎÄ¡ or 12ÀÎÄ¡";
            this.lblOptInchKey.BackColor = Color.Gainsboro;
            this.lblOptInchKey.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptInchKey.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptInchKey.Padding = new Padding(6, 0, 0, 0);
            this.lblOptInchKey.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            this.lblOptInchVal.Location = new Point(220, 232);
            this.lblOptInchVal.Name = "lblOptInchVal";
            this.lblOptInchVal.Size = new Size(360, 26);
            this.lblOptInchVal.Text = "12ÀÎÄ¡";
            this.lblOptInchVal.BackColor = Color.White;
            this.lblOptInchVal.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptInchVal.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptInchVal.Padding = new Padding(6, 0, 0, 0);
            this.lblOptInchVal.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            // row8
            this.lblOptStageKey.Location = new Point(10, 264);
            this.lblOptStageKey.Name = "lblOptStageKey";
            this.lblOptStageKey.Size = new Size(200, 26);
            this.lblOptStageKey.Text = "Ä«¼¼Æ® 2´Ü È°¼ºÈ­";
            this.lblOptStageKey.BackColor = Color.Gainsboro;
            this.lblOptStageKey.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptStageKey.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptStageKey.Padding = new Padding(6, 0, 0, 0);
            this.lblOptStageKey.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            this.lblOptStageVal.Location = new Point(220, 264);
            this.lblOptStageVal.Name = "lblOptStageVal";
            this.lblOptStageVal.Size = new Size(360, 26);
            this.lblOptStageVal.Text = "1´Ü";
            this.lblOptStageVal.BackColor = Color.White;
            this.lblOptStageVal.BorderStyle = BorderStyle.FixedSingle;
            this.lblOptStageVal.TextAlign = ContentAlignment.MiddleLeft;
            this.lblOptStageVal.Padding = new Padding(6, 0, 0, 0);
            this.lblOptStageVal.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            // wait title
            this.lblWaitTitle.BackColor = Color.Orange;
            this.lblWaitTitle.ForeColor = Color.Black;
            this.lblWaitTitle.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Bold);
            this.lblWaitTitle.Location = new Point(0, 304);
            this.lblWaitTitle.Name = "lblWaitTitle";
            this.lblWaitTitle.Size = new Size(598, 26);
            this.lblWaitTitle.Text = "´ë±â½Ã°£";
            this.lblWaitTitle.TextAlign = ContentAlignment.MiddleLeft;
            this.lblWaitTitle.Padding = new Padding(6, 0, 0, 0);

            // wait row
            this.lblWaitKey.Location = new Point(10, 336);
            this.lblWaitKey.Name = "lblWaitKey";
            this.lblWaitKey.Size = new Size(200, 26);
            this.lblWaitKey.Text = "ÀÌµ¿ ÈÄ ´ë±â½Ã°£";
            this.lblWaitKey.BackColor = Color.Gainsboro;
            this.lblWaitKey.BorderStyle = BorderStyle.FixedSingle;
            this.lblWaitKey.TextAlign = ContentAlignment.MiddleLeft;
            this.lblWaitKey.Padding = new Padding(6, 0, 0, 0);
            this.lblWaitKey.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            this.lblWaitVal.Location = new Point(220, 336);
            this.lblWaitVal.Name = "lblWaitVal";
            this.lblWaitVal.Size = new Size(360, 26);
            this.lblWaitVal.Text = "100 ms";
            this.lblWaitVal.BackColor = Color.White;
            this.lblWaitVal.BorderStyle = BorderStyle.FixedSingle;
            this.lblWaitVal.TextAlign = ContentAlignment.MiddleLeft;
            this.lblWaitVal.Padding = new Padding(6, 0, 0, 0);
            this.lblWaitVal.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Regular);

            // lblJogTitle
            this.lblJogTitle.BackColor = Color.Orange;
            this.lblJogTitle.ForeColor = Color.Black;
            this.lblJogTitle.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Bold);
            this.lblJogTitle.Location = new Point(10, 60);
            this.lblJogTitle.Name = "lblJogTitle";
            this.lblJogTitle.Size = new Size(300, 26);
            this.lblJogTitle.Text = "Á¶±× ¿îÀü";
            this.lblJogTitle.TextAlign = ContentAlignment.MiddleLeft;
            this.lblJogTitle.Padding = new Padding(6, 0, 0, 0);

            // lblAxisName
            this.lblAxisName.BackColor = Color.Gainsboro;
            this.lblAxisName.BorderStyle = BorderStyle.FixedSingle;
            this.lblAxisName.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Bold);
            this.lblAxisName.Location = new Point(10, 94);
            this.lblAxisName.Name = "lblAxisName";
            this.lblAxisName.Size = new Size(300, 28);
            this.lblAxisName.Text = "AXIS Z";
            this.lblAxisName.TextAlign = ContentAlignment.MiddleCenter;

            // btnJogPlus
            this.btnJogPlus.Location = new Point(10, 136);
            this.btnJogPlus.Name = "btnJogPlus";
            this.btnJogPlus.Size = new Size(300, 56);
            this.btnJogPlus.Text = "JOG +";
            this.btnJogPlus.UseVisualStyleBackColor = true;
            this.btnJogPlus.Click += new EventHandler(this.btnJogPlus_Click);

            // btnJogStop
            this.btnJogStop.Location = new Point(10, 198);
            this.btnJogStop.Name = "btnJogStop";
            this.btnJogStop.Size = new Size(300, 44);
            this.btnJogStop.Text = "STOP";
            this.btnJogStop.UseVisualStyleBackColor = true;
            this.btnJogStop.Click += new EventHandler(this.btnJogStop_Click);

            // btnJogMinus
            this.btnJogMinus.Location = new Point(10, 248);
            this.btnJogMinus.Name = "btnJogMinus";
            this.btnJogMinus.Size = new Size(300, 56);
            this.btnJogMinus.Text = "JOG -";
            this.btnJogMinus.UseVisualStyleBackColor = true;
            this.btnJogMinus.Click += new EventHandler(this.btnJogMinus_Click);

            // lblSpeedTitle
            this.lblSpeedTitle.BackColor = Color.Orange;
            this.lblSpeedTitle.ForeColor = Color.Black;
            this.lblSpeedTitle.Font = new Font("¸¼Àº °íµñ", 9F, FontStyle.Bold);
            this.lblSpeedTitle.Location = new Point(330, 60);
            this.lblSpeedTitle.Name = "lblSpeedTitle";
            this.lblSpeedTitle.Size = new Size(118, 26);
            this.lblSpeedTitle.Text = "¼Óµµ";
            this.lblSpeedTitle.TextAlign = ContentAlignment.MiddleLeft;
            this.lblSpeedTitle.Padding = new Padding(6, 0, 0, 0);

            // trkSpeed
            this.trkSpeed.Location = new Point(360, 95);
            this.trkSpeed.Name = "trkSpeed";
            this.trkSpeed.Orientation = Orientation.Vertical;
            this.trkSpeed.Size = new Size(45, 620);
            this.trkSpeed.Minimum = 0;
            this.trkSpeed.Maximum = 100;
            this.trkSpeed.TickFrequency = 10;
            this.trkSpeed.Value = 50;
            this.trkSpeed.ValueChanged += new EventHandler(this.trkSpeed_ValueChanged);

            // lblSpeedHigh
            this.lblSpeedHigh.Location = new Point(412, 95);
            this.lblSpeedHigh.Name = "lblSpeedHigh";
            this.lblSpeedHigh.Size = new Size(36, 20);
            this.lblSpeedHigh.Text = "H";
            this.lblSpeedHigh.TextAlign = ContentAlignment.MiddleCenter;

            // lblSpeedMid
            this.lblSpeedMid.Location = new Point(412, 398);
            this.lblSpeedMid.Name = "lblSpeedMid";
            this.lblSpeedMid.Size = new Size(36, 20);
            this.lblSpeedMid.Text = "M";
            this.lblSpeedMid.TextAlign = ContentAlignment.MiddleCenter;

            // lblSpeedLow
            this.lblSpeedLow.Location = new Point(412, 695);
            this.lblSpeedLow.Name = "lblSpeedLow";
            this.lblSpeedLow.Size = new Size(36, 20);
            this.lblSpeedLow.Text = "L";
            this.lblSpeedLow.TextAlign = ContentAlignment.MiddleCenter;

            // lblSpeedValue
            this.lblSpeedValue.BackColor = Color.White;
            this.lblSpeedValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblSpeedValue.Location = new Point(330, 726);
            this.lblSpeedValue.Name = "lblSpeedValue";
            this.lblSpeedValue.Size = new Size(118, 26);
            this.lblSpeedValue.Text = "50 %";
            this.lblSpeedValue.TextAlign = ContentAlignment.MiddleCenter;

            // ±âÁ¸ ÁÂÃø ¹öÆ° click ÀÌº¥Æ® ¿¬°á
            this.btnLoadingMove.Click += new EventHandler(this.btnLoadingMove_Click);
            this.btnUnloadingMove.Click += new EventHandler(this.btnUnloadingMove_Click);
            this.btnReadyMove.Click += new EventHandler(this.btnReadyMove_Click);
            this.btnSlotLoadingMove.Click += new EventHandler(this.btnSlotLoadingMove_Click);
            this.btnSlotUnloadingMove.Click += new EventHandler(this.btnSlotUnloadingMove_Click);


            // panelLeft Controls
            this.panelLeft.Controls.Add(this.lblLeftTitle);
            this.panelLeft.Controls.Add(this.btnLoadingMove);
            this.panelLeft.Controls.Add(this.btnUnloadingMove);
            this.panelLeft.Controls.Add(this.btnReadyMove);
            this.panelLeft.Controls.Add(this.btnSlotLoadingMove);
            this.panelLeft.Controls.Add(this.btnSlotUnloadingMove);
            this.panelLeft.Controls.Add(this.lblIoTitle);
            this.panelLeft.Controls.Add(this.dotSensor1);
            this.panelLeft.Controls.Add(this.dotSensor2);
            this.panelLeft.Controls.Add(this.dotProtrusion);
            this.panelLeft.Controls.Add(this.dotMapping);
            this.panelLeft.Controls.Add(this.lblSensor1);
            this.panelLeft.Controls.Add(this.lblSensor2);
            this.panelLeft.Controls.Add(this.lblProtrusion);
            this.panelLeft.Controls.Add(this.lblMapping);

            // panelCenter Controls
            this.panelCenter.Controls.Add(this.lblCenterTitle);
            this.panelCenter.Controls.Add(this.lblOptLoadingZKey);
            this.panelCenter.Controls.Add(this.lblOptLoadingZVal);
            this.panelCenter.Controls.Add(this.lblOptUnloadingZKey);
            this.panelCenter.Controls.Add(this.lblOptUnloadingZVal);
            this.panelCenter.Controls.Add(this.lblOptReadyPosKey);
            this.panelCenter.Controls.Add(this.lblOptReadyPosVal);
            this.panelCenter.Controls.Add(this.lblOptMappingZKey);
            this.panelCenter.Controls.Add(this.lblOptMappingZVal);
            this.panelCenter.Controls.Add(this.lblOptSlotPitchKey);
            this.panelCenter.Controls.Add(this.lblOptSlotPitchVal);
            this.panelCenter.Controls.Add(this.lblOptCassetteGapKey);
            this.panelCenter.Controls.Add(this.lblOptCassetteGapVal);
            this.panelCenter.Controls.Add(this.lblOptInchKey);
            this.panelCenter.Controls.Add(this.lblOptInchVal);
            this.panelCenter.Controls.Add(this.lblOptStageKey);
            this.panelCenter.Controls.Add(this.lblOptStageVal);
            this.panelCenter.Controls.Add(this.lblWaitTitle);
            this.panelCenter.Controls.Add(this.lblWaitKey);
            this.panelCenter.Controls.Add(this.lblWaitVal);

            // panelRight Controls
            this.panelRight.Controls.Add(this.lblRightTitle);
            this.panelRight.Controls.Add(this.lblJogTitle);
            this.panelRight.Controls.Add(this.lblAxisName);
            this.panelRight.Controls.Add(this.btnJogPlus);
            this.panelRight.Controls.Add(this.btnJogStop);
            this.panelRight.Controls.Add(this.btnJogMinus);
            this.panelRight.Controls.Add(this.lblSpeedTitle);
            this.panelRight.Controls.Add(this.trkSpeed);
            this.panelRight.Controls.Add(this.lblSpeedHigh);
            this.panelRight.Controls.Add(this.lblSpeedMid);
            this.panelRight.Controls.Add(this.lblSpeedLow);
            this.panelRight.Controls.Add(this.lblSpeedValue);

            // CassetteRecipePage Controls
            this.Controls.Add(this.lblHeader);
            this.Controls.Add(this.panelLeft);
            this.Controls.Add(this.panelCenter);
            this.Controls.Add(this.panelRight);

            this.ResumeLayout(false);
        }
    }
}