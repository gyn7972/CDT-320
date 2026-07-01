using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class OutputFeederPage
    {
        private TableLayoutPanel rootPanel;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private TableLayoutPanel leftStack;
        private GroupBox grpInfo;
        private TableLayoutPanel infoWrap;
        private TableLayoutPanel workLayout;
        private TableLayoutPanel axisYPanel;
        private TableLayoutPanel infoLayout;
        private GroupBox grpAction;
        private TableLayoutPanel actionBar;
        private MaterialDetailView materialDetailView;
        // WORK INFO value pairs
        private Label lblExistCaption;
        private Label _lblExist;
        private Label lblSideCaption;
        private Label _lblSide;
        private Label lblSlotCaption;
        private Label _lblSlot;
        private Label lblFeederPosCaption;
        private Label _lblFeederPos;
        private Label lblClampCaption;
        private Label _lblClampState;
        private Label lblUpDownCaption;
        private Label _lblUpDownState;
        private Label lblTargetCaption;
        private TableLayoutPanel targetSideLayout;
        private RadioButton rbTargetOk;
        private RadioButton rbTargetNg;
        // INFO sensor marks + wrappers
        private TableLayoutPanel sensorRingPanel;
        private Label _markRing;
        private Label lblRingCaption;
        private TableLayoutPanel sensorOverloadPanel;
        private Label _markOverload;
        private Label lblOverloadCaption;
        private TableLayoutPanel sensorUpPanel;
        private Label _markUp;
        private Label lblUpSensorCaption;
        private TableLayoutPanel sensorDownPanel;
        private Label _markDown;
        private Label lblDownSensorCaption;
        private TableLayoutPanel sensorUnclampPanel;
        private Label _markUnclamp;
        private Label lblUnclampSensorCaption;
        private TableLayoutPanel sensorGood1Panel;
        private Label _markGood1;
        private Label lblGood1Caption;
        private TableLayoutPanel sensorGood2Panel;
        private Label _markGood2;
        private Label lblGood2Caption;
        private TableLayoutPanel sensorNgPanel;
        private Label _markNg;
        private Label lblNgCaption;
        private TableLayoutPanel sensorProtrusionPanel;
        private Label _markProtrusion;
        private Label lblProtrusionCaption;
        private TableLayoutPanel sensorMappingPanel;
        private Label _markMapping;
        private Label lblMappingCaption;
        private TableLayoutPanel sensorNgBwPanel;
        private Label _markNgBw;
        private Label lblNgBwCaption;
        private TableLayoutPanel sensorNgLockPanel;
        private Label _markNgLock;
        private Label lblNgLockCaption;
        // ACTION buttons
        private ActionButton btnLoadFromCassette;
        private ActionButton btnLoadToStage;
        private ActionButton btnUnloadFromStage;
        private ActionButton btnUnloadToCassette;
        private ActionButton btnRecover;
        private ActionButton btnStop;

        private void InitializeComponent()
        {
            this.rootPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.leftStack = new System.Windows.Forms.TableLayoutPanel();
            this.grpInfo = new System.Windows.Forms.GroupBox();
            this.infoWrap = new System.Windows.Forms.TableLayoutPanel();
            this.workLayout = new System.Windows.Forms.TableLayoutPanel();
            this.axisYPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblExistCaption = new System.Windows.Forms.Label();
            this._lblExist = new System.Windows.Forms.Label();
            this.lblSideCaption = new System.Windows.Forms.Label();
            this._lblSide = new System.Windows.Forms.Label();
            this.lblSlotCaption = new System.Windows.Forms.Label();
            this._lblSlot = new System.Windows.Forms.Label();
            this.lblFeederPosCaption = new System.Windows.Forms.Label();
            this._lblFeederPos = new System.Windows.Forms.Label();
            this.lblClampCaption = new System.Windows.Forms.Label();
            this._lblClampState = new System.Windows.Forms.Label();
            this.lblUpDownCaption = new System.Windows.Forms.Label();
            this._lblUpDownState = new System.Windows.Forms.Label();
            this.lblTargetCaption = new System.Windows.Forms.Label();
            this.targetSideLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rbTargetOk = new System.Windows.Forms.RadioButton();
            this.rbTargetNg = new System.Windows.Forms.RadioButton();
            this.infoLayout = new System.Windows.Forms.TableLayoutPanel();
            this.sensorRingPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markRing = new System.Windows.Forms.Label();
            this.lblRingCaption = new System.Windows.Forms.Label();
            this.sensorOverloadPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markOverload = new System.Windows.Forms.Label();
            this.lblOverloadCaption = new System.Windows.Forms.Label();
            this.sensorUnclampPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markUnclamp = new System.Windows.Forms.Label();
            this.lblUnclampSensorCaption = new System.Windows.Forms.Label();
            this.sensorUpPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markUp = new System.Windows.Forms.Label();
            this.lblUpSensorCaption = new System.Windows.Forms.Label();
            this.sensorDownPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markDown = new System.Windows.Forms.Label();
            this.lblDownSensorCaption = new System.Windows.Forms.Label();
            this.sensorNgBwPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markNgBw = new System.Windows.Forms.Label();
            this.lblNgBwCaption = new System.Windows.Forms.Label();
            this.sensorMappingPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markMapping = new System.Windows.Forms.Label();
            this.lblMappingCaption = new System.Windows.Forms.Label();
            this.sensorProtrusionPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markProtrusion = new System.Windows.Forms.Label();
            this.lblProtrusionCaption = new System.Windows.Forms.Label();
            this.sensorNgLockPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markNgLock = new System.Windows.Forms.Label();
            this.lblNgLockCaption = new System.Windows.Forms.Label();
            this.sensorGood1Panel = new System.Windows.Forms.TableLayoutPanel();
            this._markGood1 = new System.Windows.Forms.Label();
            this.lblGood1Caption = new System.Windows.Forms.Label();
            this.sensorGood2Panel = new System.Windows.Forms.TableLayoutPanel();
            this._markGood2 = new System.Windows.Forms.Label();
            this.lblGood2Caption = new System.Windows.Forms.Label();
            this.sensorNgPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markNg = new System.Windows.Forms.Label();
            this.lblNgCaption = new System.Windows.Forms.Label();
            this.grpAction = new System.Windows.Forms.GroupBox();
            this.actionBar = new System.Windows.Forms.TableLayoutPanel();
            this.btnLoadFromCassette = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnLoadToStage = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUnloadFromStage = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUnloadToCassette = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnRecover = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.materialDetailView = new QMC.CDT_320.Ui.Controls.MaterialDetailView();
            this.rootPanel.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.leftStack.SuspendLayout();
            this.grpInfo.SuspendLayout();
            this.infoWrap.SuspendLayout();
            this.workLayout.SuspendLayout();
            this.axisYPanel.SuspendLayout();
            this.targetSideLayout.SuspendLayout();
            this.infoLayout.SuspendLayout();
            this.sensorRingPanel.SuspendLayout();
            this.sensorOverloadPanel.SuspendLayout();
            this.sensorUnclampPanel.SuspendLayout();
            this.sensorUpPanel.SuspendLayout();
            this.sensorDownPanel.SuspendLayout();
            this.sensorNgBwPanel.SuspendLayout();
            this.sensorMappingPanel.SuspendLayout();
            this.sensorProtrusionPanel.SuspendLayout();
            this.sensorNgLockPanel.SuspendLayout();
            this.sensorGood1Panel.SuspendLayout();
            this.sensorGood2Panel.SuspendLayout();
            this.sensorNgPanel.SuspendLayout();
            this.grpAction.SuspendLayout();
            this.actionBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootPanel
            // 
            this.rootPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.rootPanel.ColumnCount = 1;
            this.rootPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootPanel.Controls.Add(this.lblHeader, 0, 0);
            this.rootPanel.Controls.Add(this.contentLayout, 0, 1);
            this.rootPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootPanel.Location = new System.Drawing.Point(0, 0);
            this.rootPanel.Margin = new System.Windows.Forms.Padding(0);
            this.rootPanel.Name = "rootPanel";
            this.rootPanel.RowCount = 2;
            this.rootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootPanel.Size = new System.Drawing.Size(1678, 900);
            this.rootPanel.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1678, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:wi.outputFeeder";
            this.lblHeader.Text = "OUTPUT FEEDER";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.Controls.Add(this.leftStack, 0, 0);
            this.contentLayout.Controls.Add(this.materialDetailView, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1678, 870);
            this.contentLayout.TabIndex = 1;
            // 
            // leftStack
            // 
            this.leftStack.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.leftStack.ColumnCount = 1;
            this.leftStack.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftStack.Controls.Add(this.grpInfo, 0, 0);
            this.leftStack.Controls.Add(this.grpAction, 0, 1);
            this.leftStack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftStack.Location = new System.Drawing.Point(0, 0);
            this.leftStack.Margin = new System.Windows.Forms.Padding(0);
            this.leftStack.Name = "leftStack";
            this.leftStack.RowCount = 2;
            this.leftStack.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.leftStack.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftStack.Size = new System.Drawing.Size(839, 870);
            this.leftStack.TabIndex = 0;
            // 
            // grpInfo
            // 
            this.grpInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpInfo.Controls.Add(this.infoWrap);
            this.grpInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpInfo.Location = new System.Drawing.Point(0, 0);
            this.grpInfo.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3);
            this.grpInfo.Name = "grpInfo";
            this.grpInfo.Size = new System.Drawing.Size(836, 297);
            this.grpInfo.TabIndex = 0;
            this.grpInfo.TabStop = false;
            this.grpInfo.Text = "INFO";
            // 
            // infoWrap
            // 
            this.infoWrap.ColumnCount = 1;
            this.infoWrap.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.infoWrap.Controls.Add(this.workLayout, 0, 0);
            this.infoWrap.Controls.Add(this.infoLayout, 0, 1);
            this.infoWrap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoWrap.Location = new System.Drawing.Point(3, 23);
            this.infoWrap.Name = "infoWrap";
            this.infoWrap.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.infoWrap.RowCount = 2;
            this.infoWrap.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 139F));
            this.infoWrap.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.infoWrap.Size = new System.Drawing.Size(830, 271);
            this.infoWrap.TabIndex = 0;
            // 
            // workLayout
            // 
            this.workLayout.ColumnCount = 4;
            this.workLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 26F));
            this.workLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 26F));
            this.workLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23F));
            this.workLayout.Controls.Add(this.lblExistCaption, 0, 0);
            this.workLayout.Controls.Add(this._lblExist, 1, 0);
            this.workLayout.Controls.Add(this.lblSlotCaption, 0, 1);
            this.workLayout.Controls.Add(this._lblSlot, 1, 1);
            this.workLayout.Controls.Add(this.lblSideCaption, 0, 2);
            this.workLayout.Controls.Add(this._lblSide, 1, 2);
            this.workLayout.Controls.Add(this.lblTargetCaption, 0, 3);
            this.workLayout.Controls.Add(this.targetSideLayout, 1, 3);
            this.workLayout.Controls.Add(this.axisYPanel, 3, 0);
            this.workLayout.Controls.Add(this.lblClampCaption, 2, 2);
            this.workLayout.Controls.Add(this._lblClampState, 3, 2);
            this.workLayout.Controls.Add(this.lblUpDownCaption, 2, 3);
            this.workLayout.Controls.Add(this._lblUpDownState, 3, 3);
            this.workLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workLayout.Location = new System.Drawing.Point(8, 6);
            this.workLayout.Margin = new System.Windows.Forms.Padding(0);
            this.workLayout.Name = "workLayout";
            this.workLayout.RowCount = 4;
            this.workLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.workLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.workLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workLayout.Size = new System.Drawing.Size(814, 131);
            this.workLayout.TabIndex = 0;
            this.workLayout.SetColumnSpan(this.axisYPanel, 1);
            this.workLayout.SetRowSpan(this.axisYPanel, 2);
            // 
            // lblExistCaption
            // 
            this.lblExistCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblExistCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExistCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblExistCaption.Location = new System.Drawing.Point(2, 2);
            this.lblExistCaption.Margin = new System.Windows.Forms.Padding(2);
            this.lblExistCaption.Name = "lblExistCaption";
            this.lblExistCaption.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblExistCaption.Size = new System.Drawing.Size(207, 26);
            this.lblExistCaption.TabIndex = 0;
            this.lblExistCaption.Text = "EXIST";
            this.lblExistCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblExist
            // 
            this._lblExist.BackColor = System.Drawing.Color.White;
            this._lblExist.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblExist.Font = new System.Drawing.Font("Consolas", 9F);
            this._lblExist.Location = new System.Drawing.Point(213, 2);
            this._lblExist.Margin = new System.Windows.Forms.Padding(2);
            this._lblExist.Name = "_lblExist";
            this._lblExist.Size = new System.Drawing.Size(191, 26);
            this._lblExist.TabIndex = 1;
            this._lblExist.Text = "--";
            this._lblExist.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSideCaption
            // 
            this.lblSideCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblSideCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSideCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblSideCaption.Location = new System.Drawing.Point(408, 2);
            this.lblSideCaption.Margin = new System.Windows.Forms.Padding(2);
            this.lblSideCaption.Name = "lblSideCaption";
            this.lblSideCaption.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblSideCaption.Size = new System.Drawing.Size(207, 26);
            this.lblSideCaption.TabIndex = 2;
            this.lblSideCaption.Text = "SIDE";
            this.lblSideCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblSide
            // 
            this._lblSide.BackColor = System.Drawing.Color.White;
            this._lblSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblSide.Font = new System.Drawing.Font("Consolas", 9F);
            this._lblSide.Location = new System.Drawing.Point(619, 2);
            this._lblSide.Margin = new System.Windows.Forms.Padding(2);
            this._lblSide.Name = "_lblSide";
            this._lblSide.Size = new System.Drawing.Size(193, 26);
            this._lblSide.TabIndex = 3;
            this._lblSide.Text = "--";
            this._lblSide.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSlotCaption
            // 
            this.lblSlotCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblSlotCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblSlotCaption.Location = new System.Drawing.Point(2, 32);
            this.lblSlotCaption.Margin = new System.Windows.Forms.Padding(2);
            this.lblSlotCaption.Name = "lblSlotCaption";
            this.lblSlotCaption.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblSlotCaption.Size = new System.Drawing.Size(207, 26);
            this.lblSlotCaption.TabIndex = 4;
            this.lblSlotCaption.Text = "SLOT";
            this.lblSlotCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblSlot
            // 
            this._lblSlot.BackColor = System.Drawing.Color.White;
            this._lblSlot.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblSlot.Font = new System.Drawing.Font("Consolas", 9F);
            this._lblSlot.Location = new System.Drawing.Point(213, 32);
            this._lblSlot.Margin = new System.Windows.Forms.Padding(2);
            this._lblSlot.Name = "_lblSlot";
            this._lblSlot.Size = new System.Drawing.Size(191, 26);
            this._lblSlot.TabIndex = 5;
            this._lblSlot.Text = "--";
            this._lblSlot.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // axisYPanel
            //
            this.axisYPanel.ColumnCount = 1;
            this.axisYPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axisYPanel.Controls.Add(this.lblFeederPosCaption, 0, 0);
            this.axisYPanel.Controls.Add(this._lblFeederPos, 0, 1);
            this.axisYPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.axisYPanel.Location = new System.Drawing.Point(408, 4);
            this.axisYPanel.Margin = new System.Windows.Forms.Padding(4);
            this.axisYPanel.Name = "axisYPanel";
            this.axisYPanel.RowCount = 2;
            this.axisYPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.axisYPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axisYPanel.Size = new System.Drawing.Size(182, 71);
            this.axisYPanel.TabIndex = 6;
            //
            // lblFeederPosCaption
            //
            this.lblFeederPosCaption.BackColor = System.Drawing.Color.Black;
            this.lblFeederPosCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFeederPosCaption.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblFeederPosCaption.ForeColor = System.Drawing.Color.White;
            this.lblFeederPosCaption.Location = new System.Drawing.Point(3, 0);
            this.lblFeederPosCaption.Name = "lblFeederPosCaption";
            this.lblFeederPosCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblFeederPosCaption.Size = new System.Drawing.Size(176, 24);
            this.lblFeederPosCaption.TabIndex = 0;
            this.lblFeederPosCaption.Text = "FEEDER AXIS Y";
            this.lblFeederPosCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _lblFeederPos
            //
            this._lblFeederPos.BackColor = System.Drawing.Color.White;
            this._lblFeederPos.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblFeederPos.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblFeederPos.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblFeederPos.Location = new System.Drawing.Point(3, 24);
            this._lblFeederPos.Name = "_lblFeederPos";
            this._lblFeederPos.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this._lblFeederPos.Size = new System.Drawing.Size(176, 47);
            this._lblFeederPos.TabIndex = 1;
            this._lblFeederPos.Text = "0 um";
            this._lblFeederPos.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblClampCaption
            // 
            this.lblClampCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblClampCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblClampCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblClampCaption.Location = new System.Drawing.Point(2, 62);
            this.lblClampCaption.Margin = new System.Windows.Forms.Padding(2);
            this.lblClampCaption.Name = "lblClampCaption";
            this.lblClampCaption.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblClampCaption.Size = new System.Drawing.Size(207, 26);
            this.lblClampCaption.TabIndex = 8;
            this.lblClampCaption.Text = "FEEDER CLAMP";
            this.lblClampCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblClampState
            // 
            this._lblClampState.BackColor = System.Drawing.Color.White;
            this._lblClampState.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblClampState.Font = new System.Drawing.Font("Consolas", 9F);
            this._lblClampState.Location = new System.Drawing.Point(213, 62);
            this._lblClampState.Margin = new System.Windows.Forms.Padding(2);
            this._lblClampState.Name = "_lblClampState";
            this._lblClampState.Size = new System.Drawing.Size(191, 26);
            this._lblClampState.TabIndex = 9;
            this._lblClampState.Text = "--";
            this._lblClampState.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblUpDownCaption
            // 
            this.lblUpDownCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblUpDownCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUpDownCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblUpDownCaption.Location = new System.Drawing.Point(408, 62);
            this.lblUpDownCaption.Margin = new System.Windows.Forms.Padding(2);
            this.lblUpDownCaption.Name = "lblUpDownCaption";
            this.lblUpDownCaption.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblUpDownCaption.Size = new System.Drawing.Size(207, 26);
            this.lblUpDownCaption.TabIndex = 10;
            this.lblUpDownCaption.Text = "FEEDER UP DOWN";
            this.lblUpDownCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblUpDownState
            // 
            this._lblUpDownState.BackColor = System.Drawing.Color.White;
            this._lblUpDownState.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblUpDownState.Font = new System.Drawing.Font("Consolas", 9F);
            this._lblUpDownState.Location = new System.Drawing.Point(619, 62);
            this._lblUpDownState.Margin = new System.Windows.Forms.Padding(2);
            this._lblUpDownState.Name = "_lblUpDownState";
            this._lblUpDownState.Size = new System.Drawing.Size(193, 26);
            this._lblUpDownState.TabIndex = 11;
            this._lblUpDownState.Text = "--";
            this._lblUpDownState.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTargetCaption
            // 
            this.lblTargetCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblTargetCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTargetCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblTargetCaption.Location = new System.Drawing.Point(2, 92);
            this.lblTargetCaption.Margin = new System.Windows.Forms.Padding(2);
            this.lblTargetCaption.Name = "lblTargetCaption";
            this.lblTargetCaption.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblTargetCaption.Size = new System.Drawing.Size(207, 26);
            this.lblTargetCaption.TabIndex = 12;
            this.lblTargetCaption.Text = "TARGET";
            this.lblTargetCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // targetSideLayout
            // 
            this.targetSideLayout.BackColor = System.Drawing.Color.White;
            this.targetSideLayout.ColumnCount = 2;
            this.targetSideLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.targetSideLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.targetSideLayout.Controls.Add(this.rbTargetOk, 0, 0);
            this.targetSideLayout.Controls.Add(this.rbTargetNg, 1, 0);
            this.targetSideLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.targetSideLayout.Location = new System.Drawing.Point(213, 92);
            this.targetSideLayout.Margin = new System.Windows.Forms.Padding(2);
            this.targetSideLayout.Name = "targetSideLayout";
            this.targetSideLayout.RowCount = 1;
            this.targetSideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.targetSideLayout.Size = new System.Drawing.Size(599, 26);
            this.targetSideLayout.TabIndex = 13;
            // 
            // rbTargetOk
            // 
            this.rbTargetOk.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbTargetOk.Checked = true;
            this.rbTargetOk.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbTargetOk.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            this.rbTargetOk.Location = new System.Drawing.Point(1, 1);
            this.rbTargetOk.Margin = new System.Windows.Forms.Padding(1);
            this.rbTargetOk.Name = "rbTargetOk";
            this.rbTargetOk.Size = new System.Drawing.Size(297, 24);
            this.rbTargetOk.TabIndex = 0;
            this.rbTargetOk.TabStop = true;
            this.rbTargetOk.Text = "OK";
            this.rbTargetOk.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbTargetOk.UseVisualStyleBackColor = true;
            // 
            // rbTargetNg
            // 
            this.rbTargetNg.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbTargetNg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbTargetNg.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            this.rbTargetNg.Location = new System.Drawing.Point(300, 1);
            this.rbTargetNg.Margin = new System.Windows.Forms.Padding(1);
            this.rbTargetNg.Name = "rbTargetNg";
            this.rbTargetNg.Size = new System.Drawing.Size(298, 24);
            this.rbTargetNg.TabIndex = 1;
            this.rbTargetNg.Text = "NG";
            this.rbTargetNg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbTargetNg.UseVisualStyleBackColor = true;
            // 
            // infoLayout
            // 
            this.infoLayout.ColumnCount = 3;
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.infoLayout.Controls.Add(this.sensorRingPanel, 0, 0);
            this.infoLayout.Controls.Add(this.sensorOverloadPanel, 1, 0);
            this.infoLayout.Controls.Add(this.sensorUnclampPanel, 2, 0);
            this.infoLayout.Controls.Add(this.sensorUpPanel, 0, 1);
            this.infoLayout.Controls.Add(this.sensorDownPanel, 1, 1);
            this.infoLayout.Controls.Add(this.sensorNgBwPanel, 2, 1);
            this.infoLayout.Controls.Add(this.sensorMappingPanel, 0, 2);
            this.infoLayout.Controls.Add(this.sensorProtrusionPanel, 1, 2);
            this.infoLayout.Controls.Add(this.sensorNgLockPanel, 2, 2);
            this.infoLayout.Controls.Add(this.sensorGood1Panel, 0, 3);
            this.infoLayout.Controls.Add(this.sensorGood2Panel, 1, 3);
            this.infoLayout.Controls.Add(this.sensorNgPanel, 2, 3);
            this.infoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoLayout.Location = new System.Drawing.Point(8, 126);
            this.infoLayout.Margin = new System.Windows.Forms.Padding(0);
            this.infoLayout.Name = "infoLayout";
            this.infoLayout.RowCount = 4;
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.infoLayout.Size = new System.Drawing.Size(814, 137);
            this.infoLayout.TabIndex = 0;
            // 
            // sensorRingPanel
            // 
            this.sensorRingPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.sensorRingPanel.ColumnCount = 2;
            this.sensorRingPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.sensorRingPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorRingPanel.Controls.Add(this._markRing, 0, 0);
            this.sensorRingPanel.Controls.Add(this.lblRingCaption, 1, 0);
            this.sensorRingPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorRingPanel.Location = new System.Drawing.Point(2, 2);
            this.sensorRingPanel.Margin = new System.Windows.Forms.Padding(2);
            this.sensorRingPanel.Name = "sensorRingPanel";
            this.sensorRingPanel.RowCount = 1;
            this.sensorRingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorRingPanel.Size = new System.Drawing.Size(267, 30);
            this.sensorRingPanel.TabIndex = 0;
            // 
            // _markRing
            // 
            this._markRing.BackColor = System.Drawing.Color.Black;
            this._markRing.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markRing.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._markRing.AutoSize = false;
            this._markRing.Location = new System.Drawing.Point(4, 12);
            this._markRing.Margin = new System.Windows.Forms.Padding(0);
            this._markRing.Name = "_markRing";
            this._markRing.Size = new System.Drawing.Size(18, 18);
            this._markRing.TabIndex = 0;
            // 
            // lblRingCaption
            // 
            this.lblRingCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRingCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblRingCaption.Location = new System.Drawing.Point(29, 0);
            this.lblRingCaption.Name = "lblRingCaption";
            this.lblRingCaption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblRingCaption.Size = new System.Drawing.Size(235, 30);
            this.lblRingCaption.TabIndex = 1;
            this.lblRingCaption.Text = "RING CHECK";
            this.lblRingCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // sensorOverloadPanel
            // 
            this.sensorOverloadPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.sensorOverloadPanel.ColumnCount = 2;
            this.sensorOverloadPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.sensorOverloadPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorOverloadPanel.Controls.Add(this._markOverload, 0, 0);
            this.sensorOverloadPanel.Controls.Add(this.lblOverloadCaption, 1, 0);
            this.sensorOverloadPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorOverloadPanel.Location = new System.Drawing.Point(273, 2);
            this.sensorOverloadPanel.Margin = new System.Windows.Forms.Padding(2);
            this.sensorOverloadPanel.Name = "sensorOverloadPanel";
            this.sensorOverloadPanel.RowCount = 1;
            this.sensorOverloadPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorOverloadPanel.Size = new System.Drawing.Size(267, 30);
            this.sensorOverloadPanel.TabIndex = 1;
            // 
            // _markOverload
            // 
            this._markOverload.BackColor = System.Drawing.Color.Black;
            this._markOverload.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markOverload.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._markOverload.AutoSize = false;
            this._markOverload.Location = new System.Drawing.Point(4, 12);
            this._markOverload.Margin = new System.Windows.Forms.Padding(0);
            this._markOverload.Name = "_markOverload";
            this._markOverload.Size = new System.Drawing.Size(18, 18);
            this._markOverload.TabIndex = 0;
            // 
            // lblOverloadCaption
            // 
            this.lblOverloadCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOverloadCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblOverloadCaption.Location = new System.Drawing.Point(29, 0);
            this.lblOverloadCaption.Name = "lblOverloadCaption";
            this.lblOverloadCaption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblOverloadCaption.Size = new System.Drawing.Size(235, 30);
            this.lblOverloadCaption.TabIndex = 1;
            this.lblOverloadCaption.Text = "OVERLOAD CHECK";
            this.lblOverloadCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // sensorUnclampPanel
            // 
            this.sensorUnclampPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.sensorUnclampPanel.ColumnCount = 2;
            this.sensorUnclampPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.sensorUnclampPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorUnclampPanel.Controls.Add(this._markUnclamp, 0, 0);
            this.sensorUnclampPanel.Controls.Add(this.lblUnclampSensorCaption, 1, 0);
            this.sensorUnclampPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorUnclampPanel.Location = new System.Drawing.Point(544, 2);
            this.sensorUnclampPanel.Margin = new System.Windows.Forms.Padding(2);
            this.sensorUnclampPanel.Name = "sensorUnclampPanel";
            this.sensorUnclampPanel.RowCount = 1;
            this.sensorUnclampPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorUnclampPanel.Size = new System.Drawing.Size(268, 30);
            this.sensorUnclampPanel.TabIndex = 4;
            // 
            // _markUnclamp
            // 
            this._markUnclamp.BackColor = System.Drawing.Color.Black;
            this._markUnclamp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markUnclamp.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._markUnclamp.AutoSize = false;
            this._markUnclamp.Location = new System.Drawing.Point(4, 12);
            this._markUnclamp.Margin = new System.Windows.Forms.Padding(0);
            this._markUnclamp.Name = "_markUnclamp";
            this._markUnclamp.Size = new System.Drawing.Size(18, 18);
            this._markUnclamp.TabIndex = 0;
            // 
            // lblUnclampSensorCaption
            // 
            this.lblUnclampSensorCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUnclampSensorCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblUnclampSensorCaption.Location = new System.Drawing.Point(29, 0);
            this.lblUnclampSensorCaption.Name = "lblUnclampSensorCaption";
            this.lblUnclampSensorCaption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblUnclampSensorCaption.Size = new System.Drawing.Size(236, 30);
            this.lblUnclampSensorCaption.TabIndex = 1;
            this.lblUnclampSensorCaption.Text = "UNCLAMP SENSOR";
            this.lblUnclampSensorCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // sensorUpPanel
            // 
            this.sensorUpPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.sensorUpPanel.ColumnCount = 2;
            this.sensorUpPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.sensorUpPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorUpPanel.Controls.Add(this._markUp, 0, 0);
            this.sensorUpPanel.Controls.Add(this.lblUpSensorCaption, 1, 0);
            this.sensorUpPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorUpPanel.Location = new System.Drawing.Point(2, 36);
            this.sensorUpPanel.Margin = new System.Windows.Forms.Padding(2);
            this.sensorUpPanel.Name = "sensorUpPanel";
            this.sensorUpPanel.RowCount = 1;
            this.sensorUpPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorUpPanel.Size = new System.Drawing.Size(267, 30);
            this.sensorUpPanel.TabIndex = 2;
            // 
            // _markUp
            // 
            this._markUp.BackColor = System.Drawing.Color.Black;
            this._markUp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markUp.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._markUp.AutoSize = false;
            this._markUp.Location = new System.Drawing.Point(4, 12);
            this._markUp.Margin = new System.Windows.Forms.Padding(0);
            this._markUp.Name = "_markUp";
            this._markUp.Size = new System.Drawing.Size(18, 18);
            this._markUp.TabIndex = 0;
            // 
            // lblUpSensorCaption
            // 
            this.lblUpSensorCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUpSensorCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblUpSensorCaption.Location = new System.Drawing.Point(29, 0);
            this.lblUpSensorCaption.Name = "lblUpSensorCaption";
            this.lblUpSensorCaption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblUpSensorCaption.Size = new System.Drawing.Size(235, 30);
            this.lblUpSensorCaption.TabIndex = 1;
            this.lblUpSensorCaption.Text = "UP SENSOR";
            this.lblUpSensorCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // sensorDownPanel
            // 
            this.sensorDownPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.sensorDownPanel.ColumnCount = 2;
            this.sensorDownPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.sensorDownPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorDownPanel.Controls.Add(this._markDown, 0, 0);
            this.sensorDownPanel.Controls.Add(this.lblDownSensorCaption, 1, 0);
            this.sensorDownPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorDownPanel.Location = new System.Drawing.Point(273, 36);
            this.sensorDownPanel.Margin = new System.Windows.Forms.Padding(2);
            this.sensorDownPanel.Name = "sensorDownPanel";
            this.sensorDownPanel.RowCount = 1;
            this.sensorDownPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorDownPanel.Size = new System.Drawing.Size(267, 30);
            this.sensorDownPanel.TabIndex = 3;
            // 
            // _markDown
            // 
            this._markDown.BackColor = System.Drawing.Color.Black;
            this._markDown.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markDown.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._markDown.AutoSize = false;
            this._markDown.Location = new System.Drawing.Point(4, 12);
            this._markDown.Margin = new System.Windows.Forms.Padding(0);
            this._markDown.Name = "_markDown";
            this._markDown.Size = new System.Drawing.Size(18, 18);
            this._markDown.TabIndex = 0;
            // 
            // lblDownSensorCaption
            // 
            this.lblDownSensorCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDownSensorCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblDownSensorCaption.Location = new System.Drawing.Point(29, 0);
            this.lblDownSensorCaption.Name = "lblDownSensorCaption";
            this.lblDownSensorCaption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblDownSensorCaption.Size = new System.Drawing.Size(235, 30);
            this.lblDownSensorCaption.TabIndex = 1;
            this.lblDownSensorCaption.Text = "DOWN SENSOR";
            this.lblDownSensorCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // sensorNgBwPanel
            // 
            this.sensorNgBwPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.sensorNgBwPanel.ColumnCount = 2;
            this.sensorNgBwPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.sensorNgBwPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorNgBwPanel.Controls.Add(this._markNgBw, 0, 0);
            this.sensorNgBwPanel.Controls.Add(this.lblNgBwCaption, 1, 0);
            this.sensorNgBwPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorNgBwPanel.Location = new System.Drawing.Point(544, 36);
            this.sensorNgBwPanel.Margin = new System.Windows.Forms.Padding(2);
            this.sensorNgBwPanel.Name = "sensorNgBwPanel";
            this.sensorNgBwPanel.RowCount = 1;
            this.sensorNgBwPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorNgBwPanel.Size = new System.Drawing.Size(268, 30);
            this.sensorNgBwPanel.TabIndex = 10;
            // 
            // _markNgBw
            // 
            this._markNgBw.BackColor = System.Drawing.Color.Black;
            this._markNgBw.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markNgBw.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._markNgBw.AutoSize = false;
            this._markNgBw.Location = new System.Drawing.Point(4, 12);
            this._markNgBw.Margin = new System.Windows.Forms.Padding(0);
            this._markNgBw.Name = "_markNgBw";
            this._markNgBw.Size = new System.Drawing.Size(18, 18);
            this._markNgBw.TabIndex = 0;
            // 
            // lblNgBwCaption
            // 
            this.lblNgBwCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgBwCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNgBwCaption.Location = new System.Drawing.Point(29, 0);
            this.lblNgBwCaption.Name = "lblNgBwCaption";
            this.lblNgBwCaption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblNgBwCaption.Size = new System.Drawing.Size(236, 30);
            this.lblNgBwCaption.TabIndex = 1;
            this.lblNgBwCaption.Text = "NG BW";
            this.lblNgBwCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // sensorMappingPanel
            // 
            this.sensorMappingPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.sensorMappingPanel.ColumnCount = 2;
            this.sensorMappingPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.sensorMappingPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorMappingPanel.Controls.Add(this._markMapping, 0, 0);
            this.sensorMappingPanel.Controls.Add(this.lblMappingCaption, 1, 0);
            this.sensorMappingPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorMappingPanel.Location = new System.Drawing.Point(2, 70);
            this.sensorMappingPanel.Margin = new System.Windows.Forms.Padding(2);
            this.sensorMappingPanel.Name = "sensorMappingPanel";
            this.sensorMappingPanel.RowCount = 1;
            this.sensorMappingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorMappingPanel.Size = new System.Drawing.Size(267, 30);
            this.sensorMappingPanel.TabIndex = 9;
            // 
            // _markMapping
            // 
            this._markMapping.BackColor = System.Drawing.Color.Black;
            this._markMapping.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markMapping.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._markMapping.AutoSize = false;
            this._markMapping.Location = new System.Drawing.Point(4, 12);
            this._markMapping.Margin = new System.Windows.Forms.Padding(0);
            this._markMapping.Name = "_markMapping";
            this._markMapping.Size = new System.Drawing.Size(18, 18);
            this._markMapping.TabIndex = 0;
            // 
            // lblMappingCaption
            // 
            this.lblMappingCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMappingCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblMappingCaption.Location = new System.Drawing.Point(29, 0);
            this.lblMappingCaption.Name = "lblMappingCaption";
            this.lblMappingCaption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblMappingCaption.Size = new System.Drawing.Size(235, 30);
            this.lblMappingCaption.TabIndex = 1;
            this.lblMappingCaption.Text = "MAPPING";
            this.lblMappingCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // sensorProtrusionPanel
            // 
            this.sensorProtrusionPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.sensorProtrusionPanel.ColumnCount = 2;
            this.sensorProtrusionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.sensorProtrusionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorProtrusionPanel.Controls.Add(this._markProtrusion, 0, 0);
            this.sensorProtrusionPanel.Controls.Add(this.lblProtrusionCaption, 1, 0);
            this.sensorProtrusionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorProtrusionPanel.Location = new System.Drawing.Point(273, 70);
            this.sensorProtrusionPanel.Margin = new System.Windows.Forms.Padding(2);
            this.sensorProtrusionPanel.Name = "sensorProtrusionPanel";
            this.sensorProtrusionPanel.RowCount = 1;
            this.sensorProtrusionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorProtrusionPanel.Size = new System.Drawing.Size(267, 30);
            this.sensorProtrusionPanel.TabIndex = 8;
            // 
            // _markProtrusion
            // 
            this._markProtrusion.BackColor = System.Drawing.Color.Black;
            this._markProtrusion.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markProtrusion.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._markProtrusion.AutoSize = false;
            this._markProtrusion.Location = new System.Drawing.Point(4, 12);
            this._markProtrusion.Margin = new System.Windows.Forms.Padding(0);
            this._markProtrusion.Name = "_markProtrusion";
            this._markProtrusion.Size = new System.Drawing.Size(18, 18);
            this._markProtrusion.TabIndex = 0;
            // 
            // lblProtrusionCaption
            // 
            this.lblProtrusionCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProtrusionCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblProtrusionCaption.Location = new System.Drawing.Point(29, 0);
            this.lblProtrusionCaption.Name = "lblProtrusionCaption";
            this.lblProtrusionCaption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblProtrusionCaption.Size = new System.Drawing.Size(235, 30);
            this.lblProtrusionCaption.TabIndex = 1;
            this.lblProtrusionCaption.Text = "PROTRUSION";
            this.lblProtrusionCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // sensorNgLockPanel
            // 
            this.sensorNgLockPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.sensorNgLockPanel.ColumnCount = 2;
            this.sensorNgLockPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.sensorNgLockPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorNgLockPanel.Controls.Add(this._markNgLock, 0, 0);
            this.sensorNgLockPanel.Controls.Add(this.lblNgLockCaption, 1, 0);
            this.sensorNgLockPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorNgLockPanel.Location = new System.Drawing.Point(544, 70);
            this.sensorNgLockPanel.Margin = new System.Windows.Forms.Padding(2);
            this.sensorNgLockPanel.Name = "sensorNgLockPanel";
            this.sensorNgLockPanel.RowCount = 1;
            this.sensorNgLockPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorNgLockPanel.Size = new System.Drawing.Size(268, 30);
            this.sensorNgLockPanel.TabIndex = 11;
            // 
            // _markNgLock
            // 
            this._markNgLock.BackColor = System.Drawing.Color.Black;
            this._markNgLock.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markNgLock.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._markNgLock.AutoSize = false;
            this._markNgLock.Location = new System.Drawing.Point(4, 12);
            this._markNgLock.Margin = new System.Windows.Forms.Padding(0);
            this._markNgLock.Name = "_markNgLock";
            this._markNgLock.Size = new System.Drawing.Size(18, 18);
            this._markNgLock.TabIndex = 0;
            // 
            // lblNgLockCaption
            // 
            this.lblNgLockCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgLockCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNgLockCaption.Location = new System.Drawing.Point(29, 0);
            this.lblNgLockCaption.Name = "lblNgLockCaption";
            this.lblNgLockCaption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblNgLockCaption.Size = new System.Drawing.Size(236, 30);
            this.lblNgLockCaption.TabIndex = 1;
            this.lblNgLockCaption.Text = "NG LOCK";
            this.lblNgLockCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // sensorGood1Panel
            // 
            this.sensorGood1Panel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.sensorGood1Panel.ColumnCount = 2;
            this.sensorGood1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.sensorGood1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorGood1Panel.Controls.Add(this._markGood1, 0, 0);
            this.sensorGood1Panel.Controls.Add(this.lblGood1Caption, 1, 0);
            this.sensorGood1Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorGood1Panel.Location = new System.Drawing.Point(2, 104);
            this.sensorGood1Panel.Margin = new System.Windows.Forms.Padding(2);
            this.sensorGood1Panel.Name = "sensorGood1Panel";
            this.sensorGood1Panel.RowCount = 1;
            this.sensorGood1Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorGood1Panel.Size = new System.Drawing.Size(267, 31);
            this.sensorGood1Panel.TabIndex = 5;
            // 
            // _markGood1
            // 
            this._markGood1.BackColor = System.Drawing.Color.Black;
            this._markGood1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markGood1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._markGood1.AutoSize = false;
            this._markGood1.Location = new System.Drawing.Point(4, 12);
            this._markGood1.Margin = new System.Windows.Forms.Padding(0);
            this._markGood1.Name = "_markGood1";
            this._markGood1.Size = new System.Drawing.Size(18, 18);
            this._markGood1.TabIndex = 0;
            // 
            // lblGood1Caption
            // 
            this.lblGood1Caption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGood1Caption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGood1Caption.Location = new System.Drawing.Point(29, 0);
            this.lblGood1Caption.Name = "lblGood1Caption";
            this.lblGood1Caption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblGood1Caption.Size = new System.Drawing.Size(235, 31);
            this.lblGood1Caption.TabIndex = 1;
            this.lblGood1Caption.Text = "GOOD 1 CST";
            this.lblGood1Caption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // sensorGood2Panel
            // 
            this.sensorGood2Panel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.sensorGood2Panel.ColumnCount = 2;
            this.sensorGood2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.sensorGood2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorGood2Panel.Controls.Add(this._markGood2, 0, 0);
            this.sensorGood2Panel.Controls.Add(this.lblGood2Caption, 1, 0);
            this.sensorGood2Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorGood2Panel.Location = new System.Drawing.Point(273, 104);
            this.sensorGood2Panel.Margin = new System.Windows.Forms.Padding(2);
            this.sensorGood2Panel.Name = "sensorGood2Panel";
            this.sensorGood2Panel.RowCount = 1;
            this.sensorGood2Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorGood2Panel.Size = new System.Drawing.Size(267, 31);
            this.sensorGood2Panel.TabIndex = 6;
            // 
            // _markGood2
            // 
            this._markGood2.BackColor = System.Drawing.Color.Black;
            this._markGood2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markGood2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._markGood2.AutoSize = false;
            this._markGood2.Location = new System.Drawing.Point(4, 12);
            this._markGood2.Margin = new System.Windows.Forms.Padding(0);
            this._markGood2.Name = "_markGood2";
            this._markGood2.Size = new System.Drawing.Size(18, 18);
            this._markGood2.TabIndex = 0;
            // 
            // lblGood2Caption
            // 
            this.lblGood2Caption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGood2Caption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGood2Caption.Location = new System.Drawing.Point(29, 0);
            this.lblGood2Caption.Name = "lblGood2Caption";
            this.lblGood2Caption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblGood2Caption.Size = new System.Drawing.Size(235, 31);
            this.lblGood2Caption.TabIndex = 1;
            this.lblGood2Caption.Text = "GOOD 2 CST";
            this.lblGood2Caption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // sensorNgPanel
            // 
            this.sensorNgPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.sensorNgPanel.ColumnCount = 2;
            this.sensorNgPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.sensorNgPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorNgPanel.Controls.Add(this._markNg, 0, 0);
            this.sensorNgPanel.Controls.Add(this.lblNgCaption, 1, 0);
            this.sensorNgPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorNgPanel.Location = new System.Drawing.Point(544, 104);
            this.sensorNgPanel.Margin = new System.Windows.Forms.Padding(2);
            this.sensorNgPanel.Name = "sensorNgPanel";
            this.sensorNgPanel.RowCount = 1;
            this.sensorNgPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorNgPanel.Size = new System.Drawing.Size(268, 31);
            this.sensorNgPanel.TabIndex = 7;
            // 
            // _markNg
            // 
            this._markNg.BackColor = System.Drawing.Color.Black;
            this._markNg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markNg.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._markNg.AutoSize = false;
            this._markNg.Location = new System.Drawing.Point(4, 12);
            this._markNg.Margin = new System.Windows.Forms.Padding(0);
            this._markNg.Name = "_markNg";
            this._markNg.Size = new System.Drawing.Size(18, 18);
            this._markNg.TabIndex = 0;
            // 
            // lblNgCaption
            // 
            this.lblNgCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNgCaption.Location = new System.Drawing.Point(29, 0);
            this.lblNgCaption.Name = "lblNgCaption";
            this.lblNgCaption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblNgCaption.Size = new System.Drawing.Size(236, 31);
            this.lblNgCaption.TabIndex = 1;
            this.lblNgCaption.Text = "NG CST";
            this.lblNgCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpAction
            // 
            this.grpAction.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpAction.Controls.Add(this.actionBar);
            this.grpAction.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.grpAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAction.Location = new System.Drawing.Point(0, 303);
            this.grpAction.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
            this.grpAction.Name = "grpAction";
            this.grpAction.Size = new System.Drawing.Size(836, 168);
            this.grpAction.TabIndex = 2;
            this.grpAction.TabStop = false;
            this.grpAction.Text = "ACTION";
            // 
            // actionBar
            // 
            this.actionBar.ColumnCount = 2;
            this.actionBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionBar.Controls.Add(this.btnLoadFromCassette, 0, 0);
            this.actionBar.Controls.Add(this.btnLoadToStage, 1, 0);
            this.actionBar.Controls.Add(this.btnUnloadFromStage, 0, 1);
            this.actionBar.Controls.Add(this.btnUnloadToCassette, 1, 1);
            this.actionBar.Controls.Add(this.btnRecover, 0, 2);
            this.actionBar.Controls.Add(this.btnStop, 1, 2);
            this.actionBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.actionBar.Location = new System.Drawing.Point(3, 23);
            this.actionBar.Margin = new System.Windows.Forms.Padding(0);
            this.actionBar.Name = "actionBar";
            this.actionBar.RowCount = 3;
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.Size = new System.Drawing.Size(830, 138);
            this.actionBar.TabIndex = 0;
            // 
            // btnLoadFromCassette
            // 
            this.btnLoadFromCassette.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnLoadFromCassette.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoadFromCassette.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadFromCassette.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnLoadFromCassette.ForeColor = System.Drawing.Color.White;
            this.btnLoadFromCassette.Location = new System.Drawing.Point(3, 3);
            this.btnLoadFromCassette.Name = "btnLoadFromCassette";
            this.btnLoadFromCassette.Size = new System.Drawing.Size(409, 40);
            this.btnLoadFromCassette.TabIndex = 0;
            this.btnLoadFromCassette.Text = "CST -> FEEDER";
            // 
            // btnLoadToStage
            // 
            this.btnLoadToStage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnLoadToStage.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoadToStage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadToStage.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnLoadToStage.ForeColor = System.Drawing.Color.White;
            this.btnLoadToStage.Location = new System.Drawing.Point(418, 3);
            this.btnLoadToStage.Name = "btnLoadToStage";
            this.btnLoadToStage.Size = new System.Drawing.Size(409, 40);
            this.btnLoadToStage.TabIndex = 1;
            this.btnLoadToStage.Text = "FEEDER -> STAGE";
            // 
            // btnUnloadFromStage
            // 
            this.btnUnloadFromStage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnUnloadFromStage.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUnloadFromStage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUnloadFromStage.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnUnloadFromStage.ForeColor = System.Drawing.Color.White;
            this.btnUnloadFromStage.Location = new System.Drawing.Point(3, 49);
            this.btnUnloadFromStage.Name = "btnUnloadFromStage";
            this.btnUnloadFromStage.Size = new System.Drawing.Size(409, 40);
            this.btnUnloadFromStage.TabIndex = 2;
            this.btnUnloadFromStage.Text = "STAGE -> FEEDER";
            // 
            // btnUnloadToCassette
            // 
            this.btnUnloadToCassette.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnUnloadToCassette.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUnloadToCassette.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUnloadToCassette.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnUnloadToCassette.ForeColor = System.Drawing.Color.White;
            this.btnUnloadToCassette.Location = new System.Drawing.Point(418, 49);
            this.btnUnloadToCassette.Name = "btnUnloadToCassette";
            this.btnUnloadToCassette.Size = new System.Drawing.Size(409, 40);
            this.btnUnloadToCassette.TabIndex = 3;
            this.btnUnloadToCassette.Text = "FEEDER -> CST";
            // 
            // btnRecover
            // 
            this.btnRecover.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnRecover.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRecover.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRecover.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnRecover.ForeColor = System.Drawing.Color.White;
            this.btnRecover.Location = new System.Drawing.Point(3, 95);
            this.btnRecover.Name = "btnRecover";
            this.btnRecover.Size = new System.Drawing.Size(409, 40);
            this.btnRecover.TabIndex = 4;
            this.btnRecover.Text = "RECOVER";
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(214)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(418, 95);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(409, 40);
            this.btnStop.TabIndex = 5;
            this.btnStop.Text = "STOP";
            // 
            // materialDetailView
            // 
            this.materialDetailView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.materialDetailView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialDetailView.Location = new System.Drawing.Point(842, 0);
            this.materialDetailView.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.materialDetailView.Name = "materialDetailView";
            this.materialDetailView.ShowProcessTestDataButton = false;
            this.materialDetailView.Size = new System.Drawing.Size(836, 870);
            this.materialDetailView.TabIndex = 1;
            // 
            // OutputFeederPage
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this.rootPanel);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "OutputFeederPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootPanel.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.leftStack.ResumeLayout(false);
            this.grpInfo.ResumeLayout(false);
            this.infoWrap.ResumeLayout(false);
            this.workLayout.ResumeLayout(false);
            this.axisYPanel.ResumeLayout(false);
            this.targetSideLayout.ResumeLayout(false);
            this.infoLayout.ResumeLayout(false);
            this.sensorRingPanel.ResumeLayout(false);
            this.sensorOverloadPanel.ResumeLayout(false);
            this.sensorUnclampPanel.ResumeLayout(false);
            this.sensorUpPanel.ResumeLayout(false);
            this.sensorDownPanel.ResumeLayout(false);
            this.sensorNgBwPanel.ResumeLayout(false);
            this.sensorMappingPanel.ResumeLayout(false);
            this.sensorProtrusionPanel.ResumeLayout(false);
            this.sensorNgLockPanel.ResumeLayout(false);
            this.sensorGood1Panel.ResumeLayout(false);
            this.sensorGood2Panel.ResumeLayout(false);
            this.sensorNgPanel.ResumeLayout(false);
            this.grpAction.ResumeLayout(false);
            this.actionBar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
