using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class OutputFeederPage
    {
        private TableLayoutPanel rootPanel;
        private Label lblHeader;
        private TableLayoutPanel topLayout;
        private TableLayoutPanel axisPanel;
        private TableLayoutPanel ringPanel;
        private TableLayoutPanel overloadPanel;
        private TableLayoutPanel outputSignalPanel;
        private TableLayoutPanel contentLayout;
        private GroupBox pnlWork;
        private GroupBox pnlCylinder;
        private TableLayoutPanel workLayout;
        private TableLayoutPanel cylinderLayout;
        private MaterialDetailView materialDetailView;
        private Label lblFeederPosCaption;
        private Label _lblFeederPos;
        private Label _markRing;
        private Label lblRingCaption;
        private Label _markOverload;
        private Label lblOverloadCaption;
        private Label _markGood1;
        private Label lblGood1Caption;
        private Label _markGood2;
        private Label lblGood2Caption;
        private Label _markNg;
        private Label lblNgCaption;
        private Label _markProtrusion;
        private Label lblProtrusionCaption;
        private Label _markMapping;
        private Label lblMappingCaption;
        private Label _markNgBw;
        private Label lblNgBwCaption;
        private Label _markNgLock;
        private Label lblNgLockCaption;
        private Label lblExistCaption;
        private Label _lblExist;
        private Label lblSideCaption;
        private Label _lblSide;
        private Label lblSlotCaption;
        private Label _lblSlot;
        private Label lblTargetCaption;
        private TableLayoutPanel targetSideLayout;
        private RadioButton rbTargetOk;
        private RadioButton rbTargetNg;
        private Label lblClampCaption;
        private Label _lblClampState;
        private Label lblUpDownCaption;
        private Label _lblUpDownState;
        private Label _markUp;
        private Label lblUpSensorCaption;
        private Label _markDown;
        private Label lblDownSensorCaption;
        private Label _markUnclamp;
        private Label lblUnclampSensorCaption;
        private FlowLayoutPanel actionPanel;
        private TableLayoutPanel actionBar;
        private FlowLayoutPanel actionRightPanel;
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
            this.topLayout = new System.Windows.Forms.TableLayoutPanel();
            this.axisPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblFeederPosCaption = new System.Windows.Forms.Label();
            this._lblFeederPos = new System.Windows.Forms.Label();
            this.ringPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markRing = new System.Windows.Forms.Label();
            this.lblRingCaption = new System.Windows.Forms.Label();
            this.overloadPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markOverload = new System.Windows.Forms.Label();
            this.lblOverloadCaption = new System.Windows.Forms.Label();
            this.outputSignalPanel = new System.Windows.Forms.TableLayoutPanel();
            this._markGood1 = new System.Windows.Forms.Label();
            this.lblGood1Caption = new System.Windows.Forms.Label();
            this._markGood2 = new System.Windows.Forms.Label();
            this.lblGood2Caption = new System.Windows.Forms.Label();
            this._markNg = new System.Windows.Forms.Label();
            this.lblNgCaption = new System.Windows.Forms.Label();
            this._markProtrusion = new System.Windows.Forms.Label();
            this.lblProtrusionCaption = new System.Windows.Forms.Label();
            this._markMapping = new System.Windows.Forms.Label();
            this.lblMappingCaption = new System.Windows.Forms.Label();
            this._markNgBw = new System.Windows.Forms.Label();
            this.lblNgBwCaption = new System.Windows.Forms.Label();
            this._markNgLock = new System.Windows.Forms.Label();
            this.lblNgLockCaption = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.pnlWork = new System.Windows.Forms.GroupBox();
            this.workLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblExistCaption = new System.Windows.Forms.Label();
            this._lblExist = new System.Windows.Forms.Label();
            this.lblSideCaption = new System.Windows.Forms.Label();
            this._lblSide = new System.Windows.Forms.Label();
            this.lblSlotCaption = new System.Windows.Forms.Label();
            this._lblSlot = new System.Windows.Forms.Label();
            this.lblTargetCaption = new System.Windows.Forms.Label();
            this.targetSideLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rbTargetOk = new System.Windows.Forms.RadioButton();
            this.rbTargetNg = new System.Windows.Forms.RadioButton();
            this.pnlCylinder = new System.Windows.Forms.GroupBox();
            this.cylinderLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblClampCaption = new System.Windows.Forms.Label();
            this._lblClampState = new System.Windows.Forms.Label();
            this.lblUpDownCaption = new System.Windows.Forms.Label();
            this._lblUpDownState = new System.Windows.Forms.Label();
            this._markUp = new System.Windows.Forms.Label();
            this.lblUpSensorCaption = new System.Windows.Forms.Label();
            this._markDown = new System.Windows.Forms.Label();
            this.lblDownSensorCaption = new System.Windows.Forms.Label();
            this._markUnclamp = new System.Windows.Forms.Label();
            this.lblUnclampSensorCaption = new System.Windows.Forms.Label();
            this.materialDetailView = new QMC.CDT_320.Ui.Controls.MaterialDetailView();
            this.actionPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.actionBar = new System.Windows.Forms.TableLayoutPanel();
            this.actionRightPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnLoadFromCassette = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnLoadToStage = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUnloadFromStage = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUnloadToCassette = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnRecover = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rootPanel.SuspendLayout();
            this.topLayout.SuspendLayout();
            this.axisPanel.SuspendLayout();
            this.ringPanel.SuspendLayout();
            this.overloadPanel.SuspendLayout();
            this.outputSignalPanel.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.pnlWork.SuspendLayout();
            this.workLayout.SuspendLayout();
            this.targetSideLayout.SuspendLayout();
            this.pnlCylinder.SuspendLayout();
            this.cylinderLayout.SuspendLayout();
            this.actionPanel.SuspendLayout();
            this.actionBar.SuspendLayout();
            this.actionRightPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootPanel
            // 
            this.rootPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.rootPanel.ColumnCount = 1;
            this.rootPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootPanel.Controls.Add(this.lblHeader, 0, 0);
            this.rootPanel.Controls.Add(this.topLayout, 0, 1);
            this.rootPanel.Controls.Add(this.contentLayout, 0, 2);
            this.rootPanel.Controls.Add(this.actionBar, 0, 3);
            this.rootPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootPanel.Location = new System.Drawing.Point(0, 0);
            this.rootPanel.Name = "rootPanel";
            this.rootPanel.RowCount = 4;
            this.rootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 143F));
            this.rootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.rootPanel.Size = new System.Drawing.Size(1678, 900);
            this.rootPanel.TabIndex = 0;
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
            this.lblHeader.Size = new System.Drawing.Size(1672, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:wi.outputFeeder";
            this.lblHeader.Text = "OUTPUT FEEDER";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // topLayout
            // 
            this.topLayout.ColumnCount = 4;
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 280F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 280F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topLayout.Controls.Add(this.axisPanel, 0, 0);
            this.topLayout.Controls.Add(this.ringPanel, 1, 0);
            this.topLayout.Controls.Add(this.overloadPanel, 2, 0);
            this.topLayout.Controls.Add(this.outputSignalPanel, 3, 0);
            this.topLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topLayout.Location = new System.Drawing.Point(3, 33);
            this.topLayout.Name = "topLayout";
            this.topLayout.Padding = new System.Windows.Forms.Padding(8);
            this.topLayout.RowCount = 1;
            this.topLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topLayout.Size = new System.Drawing.Size(1672, 104);
            this.topLayout.TabIndex = 1;
            // 
            // axisPanel
            // 
            this.axisPanel.ColumnCount = 1;
            this.axisPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axisPanel.Controls.Add(this.lblFeederPosCaption, 0, 0);
            this.axisPanel.Controls.Add(this._lblFeederPos, 0, 1);
            this.axisPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axisPanel.Location = new System.Drawing.Point(12, 12);
            this.axisPanel.Margin = new System.Windows.Forms.Padding(4);
            this.axisPanel.Name = "axisPanel";
            this.axisPanel.RowCount = 2;
            this.axisPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.axisPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axisPanel.Size = new System.Drawing.Size(252, 80);
            this.axisPanel.TabIndex = 0;
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
            this.lblFeederPosCaption.Size = new System.Drawing.Size(246, 24);
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
            this._lblFeederPos.Size = new System.Drawing.Size(246, 56);
            this._lblFeederPos.TabIndex = 1;
            this._lblFeederPos.Text = "0 um";
            this._lblFeederPos.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ringPanel
            // 
            this.ringPanel.ColumnCount = 2;
            this.ringPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.ringPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ringPanel.Controls.Add(this._markRing, 0, 0);
            this.ringPanel.Controls.Add(this.lblRingCaption, 1, 0);
            this.ringPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ringPanel.Location = new System.Drawing.Point(272, 12);
            this.ringPanel.Margin = new System.Windows.Forms.Padding(4);
            this.ringPanel.Name = "ringPanel";
            this.ringPanel.RowCount = 1;
            this.ringPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ringPanel.Size = new System.Drawing.Size(272, 80);
            this.ringPanel.TabIndex = 1;
            // 
            // _markRing
            // 
            this._markRing.BackColor = System.Drawing.Color.Black;
            this._markRing.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markRing.Dock = System.Windows.Forms.DockStyle.Fill;
            this._markRing.Location = new System.Drawing.Point(10, 30);
            this._markRing.Margin = new System.Windows.Forms.Padding(10, 30, 10, 30);
            this._markRing.Name = "_markRing";
            this._markRing.Size = new System.Drawing.Size(26, 20);
            this._markRing.TabIndex = 0;
            // 
            // lblRingCaption
            // 
            this.lblRingCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblRingCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRingCaption.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblRingCaption.Location = new System.Drawing.Point(49, 0);
            this.lblRingCaption.Name = "lblRingCaption";
            this.lblRingCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblRingCaption.Size = new System.Drawing.Size(220, 80);
            this.lblRingCaption.TabIndex = 1;
            this.lblRingCaption.Text = "FEEDER RING CHECK";
            this.lblRingCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // overloadPanel
            // 
            this.overloadPanel.ColumnCount = 2;
            this.overloadPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.overloadPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.overloadPanel.Controls.Add(this._markOverload, 0, 0);
            this.overloadPanel.Controls.Add(this.lblOverloadCaption, 1, 0);
            this.overloadPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.overloadPanel.Location = new System.Drawing.Point(552, 12);
            this.overloadPanel.Margin = new System.Windows.Forms.Padding(4);
            this.overloadPanel.Name = "overloadPanel";
            this.overloadPanel.RowCount = 1;
            this.overloadPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.overloadPanel.Size = new System.Drawing.Size(272, 80);
            this.overloadPanel.TabIndex = 2;
            // 
            // _markOverload
            // 
            this._markOverload.BackColor = System.Drawing.Color.Black;
            this._markOverload.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markOverload.Dock = System.Windows.Forms.DockStyle.Fill;
            this._markOverload.Location = new System.Drawing.Point(10, 30);
            this._markOverload.Margin = new System.Windows.Forms.Padding(10, 30, 10, 30);
            this._markOverload.Name = "_markOverload";
            this._markOverload.Size = new System.Drawing.Size(26, 20);
            this._markOverload.TabIndex = 0;
            // 
            // lblOverloadCaption
            // 
            this.lblOverloadCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblOverloadCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOverloadCaption.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblOverloadCaption.Location = new System.Drawing.Point(49, 0);
            this.lblOverloadCaption.Name = "lblOverloadCaption";
            this.lblOverloadCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblOverloadCaption.Size = new System.Drawing.Size(220, 80);
            this.lblOverloadCaption.TabIndex = 1;
            this.lblOverloadCaption.Text = "FEEDER OVERLOAD CHECK";
            this.lblOverloadCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // outputSignalPanel
            // 
            this.outputSignalPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.outputSignalPanel.ColumnCount = 4;
            this.outputSignalPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.outputSignalPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.outputSignalPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.outputSignalPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.outputSignalPanel.Controls.Add(this._markGood1, 0, 0);
            this.outputSignalPanel.Controls.Add(this.lblGood1Caption, 1, 0);
            this.outputSignalPanel.Controls.Add(this._markGood2, 2, 0);
            this.outputSignalPanel.Controls.Add(this.lblGood2Caption, 3, 0);
            this.outputSignalPanel.Controls.Add(this._markNg, 0, 1);
            this.outputSignalPanel.Controls.Add(this.lblNgCaption, 1, 1);
            this.outputSignalPanel.Controls.Add(this._markProtrusion, 2, 1);
            this.outputSignalPanel.Controls.Add(this.lblProtrusionCaption, 3, 1);
            this.outputSignalPanel.Controls.Add(this._markMapping, 0, 2);
            this.outputSignalPanel.Controls.Add(this.lblMappingCaption, 1, 2);
            this.outputSignalPanel.Controls.Add(this._markNgBw, 2, 2);
            this.outputSignalPanel.Controls.Add(this.lblNgBwCaption, 3, 2);
            this.outputSignalPanel.Controls.Add(this._markNgLock, 0, 3);
            this.outputSignalPanel.Controls.Add(this.lblNgLockCaption, 1, 3);
            this.outputSignalPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputSignalPanel.Location = new System.Drawing.Point(829, 9);
            this.outputSignalPanel.Margin = new System.Windows.Forms.Padding(1);
            this.outputSignalPanel.Name = "outputSignalPanel";
            this.outputSignalPanel.Padding = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.outputSignalPanel.RowCount = 4;
            this.outputSignalPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.outputSignalPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.outputSignalPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.outputSignalPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.outputSignalPanel.Size = new System.Drawing.Size(834, 86);
            this.outputSignalPanel.TabIndex = 3;
            // 
            // _markGood1
            // 
            this._markGood1.BackColor = System.Drawing.Color.Black;
            this._markGood1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markGood1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._markGood1.Location = new System.Drawing.Point(9, 7);
            this._markGood1.Margin = new System.Windows.Forms.Padding(6);
            this._markGood1.Name = "_markGood1";
            this._markGood1.Size = new System.Drawing.Size(16, 9);
            this._markGood1.TabIndex = 0;
            // 
            // lblGood1Caption
            // 
            this.lblGood1Caption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGood1Caption.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblGood1Caption.Location = new System.Drawing.Point(34, 1);
            this.lblGood1Caption.Name = "lblGood1Caption";
            this.lblGood1Caption.Size = new System.Drawing.Size(380, 21);
            this.lblGood1Caption.TabIndex = 1;
            this.lblGood1Caption.Text = "GOOD 1 CST";
            this.lblGood1Caption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _markGood2
            // 
            this._markGood2.BackColor = System.Drawing.Color.Black;
            this._markGood2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markGood2.Dock = System.Windows.Forms.DockStyle.Fill;
            this._markGood2.Location = new System.Drawing.Point(423, 7);
            this._markGood2.Margin = new System.Windows.Forms.Padding(6);
            this._markGood2.Name = "_markGood2";
            this._markGood2.Size = new System.Drawing.Size(16, 9);
            this._markGood2.TabIndex = 2;
            // 
            // lblGood2Caption
            // 
            this.lblGood2Caption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGood2Caption.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblGood2Caption.Location = new System.Drawing.Point(448, 1);
            this.lblGood2Caption.Name = "lblGood2Caption";
            this.lblGood2Caption.Size = new System.Drawing.Size(380, 21);
            this.lblGood2Caption.TabIndex = 3;
            this.lblGood2Caption.Text = "GOOD 2 CST";
            this.lblGood2Caption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _markNg
            // 
            this._markNg.BackColor = System.Drawing.Color.Black;
            this._markNg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markNg.Dock = System.Windows.Forms.DockStyle.Fill;
            this._markNg.Location = new System.Drawing.Point(9, 28);
            this._markNg.Margin = new System.Windows.Forms.Padding(6);
            this._markNg.Name = "_markNg";
            this._markNg.Size = new System.Drawing.Size(16, 9);
            this._markNg.TabIndex = 4;
            // 
            // lblNgCaption
            // 
            this.lblNgCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgCaption.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNgCaption.Location = new System.Drawing.Point(34, 22);
            this.lblNgCaption.Name = "lblNgCaption";
            this.lblNgCaption.Size = new System.Drawing.Size(380, 21);
            this.lblNgCaption.TabIndex = 5;
            this.lblNgCaption.Text = "NG CST";
            this.lblNgCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _markProtrusion
            // 
            this._markProtrusion.BackColor = System.Drawing.Color.Black;
            this._markProtrusion.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markProtrusion.Dock = System.Windows.Forms.DockStyle.Fill;
            this._markProtrusion.Location = new System.Drawing.Point(423, 28);
            this._markProtrusion.Margin = new System.Windows.Forms.Padding(6);
            this._markProtrusion.Name = "_markProtrusion";
            this._markProtrusion.Size = new System.Drawing.Size(16, 9);
            this._markProtrusion.TabIndex = 6;
            // 
            // lblProtrusionCaption
            // 
            this.lblProtrusionCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProtrusionCaption.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblProtrusionCaption.Location = new System.Drawing.Point(448, 22);
            this.lblProtrusionCaption.Name = "lblProtrusionCaption";
            this.lblProtrusionCaption.Size = new System.Drawing.Size(380, 21);
            this.lblProtrusionCaption.TabIndex = 7;
            this.lblProtrusionCaption.Text = "PROTRUSION";
            this.lblProtrusionCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _markMapping
            // 
            this._markMapping.BackColor = System.Drawing.Color.Black;
            this._markMapping.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markMapping.Dock = System.Windows.Forms.DockStyle.Fill;
            this._markMapping.Location = new System.Drawing.Point(9, 49);
            this._markMapping.Margin = new System.Windows.Forms.Padding(6);
            this._markMapping.Name = "_markMapping";
            this._markMapping.Size = new System.Drawing.Size(16, 9);
            this._markMapping.TabIndex = 8;
            // 
            // lblMappingCaption
            // 
            this.lblMappingCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMappingCaption.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblMappingCaption.Location = new System.Drawing.Point(34, 43);
            this.lblMappingCaption.Name = "lblMappingCaption";
            this.lblMappingCaption.Size = new System.Drawing.Size(380, 21);
            this.lblMappingCaption.TabIndex = 9;
            this.lblMappingCaption.Text = "MAPPING";
            this.lblMappingCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _markNgBw
            // 
            this._markNgBw.BackColor = System.Drawing.Color.Black;
            this._markNgBw.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markNgBw.Dock = System.Windows.Forms.DockStyle.Fill;
            this._markNgBw.Location = new System.Drawing.Point(423, 49);
            this._markNgBw.Margin = new System.Windows.Forms.Padding(6);
            this._markNgBw.Name = "_markNgBw";
            this._markNgBw.Size = new System.Drawing.Size(16, 9);
            this._markNgBw.TabIndex = 10;
            // 
            // lblNgBwCaption
            // 
            this.lblNgBwCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgBwCaption.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNgBwCaption.Location = new System.Drawing.Point(448, 43);
            this.lblNgBwCaption.Name = "lblNgBwCaption";
            this.lblNgBwCaption.Size = new System.Drawing.Size(380, 21);
            this.lblNgBwCaption.TabIndex = 11;
            this.lblNgBwCaption.Text = "NG BW";
            this.lblNgBwCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _markNgLock
            // 
            this._markNgLock.BackColor = System.Drawing.Color.Black;
            this._markNgLock.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markNgLock.Dock = System.Windows.Forms.DockStyle.Fill;
            this._markNgLock.Location = new System.Drawing.Point(9, 70);
            this._markNgLock.Margin = new System.Windows.Forms.Padding(6);
            this._markNgLock.Name = "_markNgLock";
            this._markNgLock.Size = new System.Drawing.Size(16, 9);
            this._markNgLock.TabIndex = 12;
            // 
            // lblNgLockCaption
            // 
            this.lblNgLockCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgLockCaption.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNgLockCaption.Location = new System.Drawing.Point(34, 64);
            this.lblNgLockCaption.Name = "lblNgLockCaption";
            this.lblNgLockCaption.Size = new System.Drawing.Size(380, 21);
            this.lblNgLockCaption.TabIndex = 13;
            this.lblNgLockCaption.Text = "NG LOCK";
            this.lblNgLockCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 320F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Controls.Add(this.pnlWork, 0, 0);
            this.contentLayout.Controls.Add(this.pnlCylinder, 1, 0);
            this.contentLayout.Controls.Add(this.materialDetailView, 2, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(3, 143);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1672, 658);
            this.contentLayout.TabIndex = 2;
            // 
            // pnlWork
            // 
            this.pnlWork.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.pnlWork.Controls.Add(this.workLayout);
            this.pnlWork.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlWork.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.pnlWork.Location = new System.Drawing.Point(15, 11);
            this.pnlWork.Name = "pnlWork";
            this.pnlWork.Size = new System.Drawing.Size(254, 636);
            this.pnlWork.TabIndex = 0;
            this.pnlWork.TabStop = false;
            this.pnlWork.Text = "WORK INFO";
            // 
            // workLayout
            // 
            this.workLayout.ColumnCount = 2;
            this.workLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.workLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.workLayout.Controls.Add(this.lblExistCaption, 0, 0);
            this.workLayout.Controls.Add(this._lblExist, 1, 0);
            this.workLayout.Controls.Add(this.lblSideCaption, 0, 1);
            this.workLayout.Controls.Add(this._lblSide, 1, 1);
            this.workLayout.Controls.Add(this.lblSlotCaption, 0, 2);
            this.workLayout.Controls.Add(this._lblSlot, 1, 2);
            this.workLayout.Controls.Add(this.lblTargetCaption, 0, 3);
            this.workLayout.Controls.Add(this.targetSideLayout, 1, 3);
            this.workLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.workLayout.Location = new System.Drawing.Point(3, 23);
            this.workLayout.Name = "workLayout";
            this.workLayout.Padding = new System.Windows.Forms.Padding(8);
            this.workLayout.RowCount = 4;
            this.workLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.workLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.workLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.workLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.workLayout.Size = new System.Drawing.Size(248, 164);
            this.workLayout.TabIndex = 0;
            // 
            // lblExistCaption
            // 
            this.lblExistCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblExistCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExistCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblExistCaption.Location = new System.Drawing.Point(11, 8);
            this.lblExistCaption.Name = "lblExistCaption";
            this.lblExistCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblExistCaption.Size = new System.Drawing.Size(114, 36);
            this.lblExistCaption.TabIndex = 0;
            this.lblExistCaption.Text = "EXIST";
            this.lblExistCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblExist
            // 
            this._lblExist.BackColor = System.Drawing.Color.White;
            this._lblExist.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblExist.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblExist.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblExist.Location = new System.Drawing.Point(131, 8);
            this._lblExist.Name = "_lblExist";
            this._lblExist.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this._lblExist.Size = new System.Drawing.Size(106, 36);
            this._lblExist.TabIndex = 1;
            this._lblExist.Text = "--";
            this._lblExist.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblSideCaption
            // 
            this.lblSideCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblSideCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSideCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblSideCaption.Location = new System.Drawing.Point(11, 44);
            this.lblSideCaption.Name = "lblSideCaption";
            this.lblSideCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblSideCaption.Size = new System.Drawing.Size(114, 36);
            this.lblSideCaption.TabIndex = 2;
            this.lblSideCaption.Text = "SIDE";
            this.lblSideCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblSide
            // 
            this._lblSide.BackColor = System.Drawing.Color.White;
            this._lblSide.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblSide.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblSide.Location = new System.Drawing.Point(131, 44);
            this._lblSide.Name = "_lblSide";
            this._lblSide.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this._lblSide.Size = new System.Drawing.Size(106, 36);
            this._lblSide.TabIndex = 3;
            this._lblSide.Text = "--";
            this._lblSide.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblSlotCaption
            // 
            this.lblSlotCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblSlotCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblSlotCaption.Location = new System.Drawing.Point(11, 80);
            this.lblSlotCaption.Name = "lblSlotCaption";
            this.lblSlotCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblSlotCaption.Size = new System.Drawing.Size(114, 36);
            this.lblSlotCaption.TabIndex = 4;
            this.lblSlotCaption.Text = "SLOT";
            this.lblSlotCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblSlot
            // 
            this._lblSlot.BackColor = System.Drawing.Color.White;
            this._lblSlot.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblSlot.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblSlot.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblSlot.Location = new System.Drawing.Point(131, 80);
            this._lblSlot.Name = "_lblSlot";
            this._lblSlot.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this._lblSlot.Size = new System.Drawing.Size(106, 36);
            this._lblSlot.TabIndex = 5;
            this._lblSlot.Text = "--";
            this._lblSlot.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTargetCaption
            // 
            this.lblTargetCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblTargetCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTargetCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblTargetCaption.Location = new System.Drawing.Point(11, 116);
            this.lblTargetCaption.Name = "lblTargetCaption";
            this.lblTargetCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblTargetCaption.Size = new System.Drawing.Size(114, 40);
            this.lblTargetCaption.TabIndex = 6;
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
            this.targetSideLayout.Location = new System.Drawing.Point(131, 119);
            this.targetSideLayout.Name = "targetSideLayout";
            this.targetSideLayout.RowCount = 1;
            this.targetSideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.targetSideLayout.Size = new System.Drawing.Size(106, 34);
            this.targetSideLayout.TabIndex = 7;
            // 
            // rbTargetOk
            // 
            this.rbTargetOk.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbTargetOk.Checked = true;
            this.rbTargetOk.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbTargetOk.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            this.rbTargetOk.Location = new System.Drawing.Point(3, 3);
            this.rbTargetOk.Name = "rbTargetOk";
            this.rbTargetOk.Size = new System.Drawing.Size(47, 28);
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
            this.rbTargetNg.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            this.rbTargetNg.Location = new System.Drawing.Point(56, 3);
            this.rbTargetNg.Name = "rbTargetNg";
            this.rbTargetNg.Size = new System.Drawing.Size(47, 28);
            this.rbTargetNg.TabIndex = 1;
            this.rbTargetNg.Text = "NG";
            this.rbTargetNg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbTargetNg.UseVisualStyleBackColor = true;
            // 
            // pnlCylinder
            // 
            this.pnlCylinder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.pnlCylinder.Controls.Add(this.cylinderLayout);
            this.pnlCylinder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCylinder.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.pnlCylinder.Location = new System.Drawing.Point(275, 11);
            this.pnlCylinder.Name = "pnlCylinder";
            this.pnlCylinder.Size = new System.Drawing.Size(314, 636);
            this.pnlCylinder.TabIndex = 1;
            this.pnlCylinder.TabStop = false;
            this.pnlCylinder.Text = "FEEDER CYLINDER INFO";
            // 
            // cylinderLayout
            // 
            this.cylinderLayout.ColumnCount = 2;
            this.cylinderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.cylinderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cylinderLayout.Controls.Add(this.lblClampCaption, 0, 0);
            this.cylinderLayout.Controls.Add(this._lblClampState, 1, 0);
            this.cylinderLayout.Controls.Add(this.lblUpDownCaption, 0, 1);
            this.cylinderLayout.Controls.Add(this._lblUpDownState, 1, 1);
            this.cylinderLayout.Controls.Add(this._markUp, 0, 2);
            this.cylinderLayout.Controls.Add(this.lblUpSensorCaption, 1, 2);
            this.cylinderLayout.Controls.Add(this._markDown, 0, 3);
            this.cylinderLayout.Controls.Add(this.lblDownSensorCaption, 1, 3);
            this.cylinderLayout.Controls.Add(this._markUnclamp, 0, 4);
            this.cylinderLayout.Controls.Add(this.lblUnclampSensorCaption, 1, 4);
            this.cylinderLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.cylinderLayout.Location = new System.Drawing.Point(3, 23);
            this.cylinderLayout.Name = "cylinderLayout";
            this.cylinderLayout.Padding = new System.Windows.Forms.Padding(8);
            this.cylinderLayout.RowCount = 5;
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.cylinderLayout.Size = new System.Drawing.Size(308, 188);
            this.cylinderLayout.TabIndex = 0;
            // 
            // lblClampCaption
            // 
            this.lblClampCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblClampCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblClampCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblClampCaption.Location = new System.Drawing.Point(11, 8);
            this.lblClampCaption.Name = "lblClampCaption";
            this.lblClampCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblClampCaption.Size = new System.Drawing.Size(144, 36);
            this.lblClampCaption.TabIndex = 0;
            this.lblClampCaption.Text = "FEEDER CLAMP";
            this.lblClampCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblClampState
            // 
            this._lblClampState.BackColor = System.Drawing.Color.White;
            this._lblClampState.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblClampState.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblClampState.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblClampState.Location = new System.Drawing.Point(161, 8);
            this._lblClampState.Name = "_lblClampState";
            this._lblClampState.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this._lblClampState.Size = new System.Drawing.Size(136, 36);
            this._lblClampState.TabIndex = 1;
            this._lblClampState.Text = "--";
            this._lblClampState.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblUpDownCaption
            // 
            this.lblUpDownCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblUpDownCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUpDownCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblUpDownCaption.Location = new System.Drawing.Point(11, 44);
            this.lblUpDownCaption.Name = "lblUpDownCaption";
            this.lblUpDownCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblUpDownCaption.Size = new System.Drawing.Size(144, 36);
            this.lblUpDownCaption.TabIndex = 2;
            this.lblUpDownCaption.Text = "FEEDER UP DOWN";
            this.lblUpDownCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblUpDownState
            // 
            this._lblUpDownState.BackColor = System.Drawing.Color.White;
            this._lblUpDownState.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblUpDownState.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblUpDownState.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblUpDownState.Location = new System.Drawing.Point(161, 44);
            this._lblUpDownState.Name = "_lblUpDownState";
            this._lblUpDownState.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this._lblUpDownState.Size = new System.Drawing.Size(136, 36);
            this._lblUpDownState.TabIndex = 3;
            this._lblUpDownState.Text = "--";
            this._lblUpDownState.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _markUp
            // 
            this._markUp.BackColor = System.Drawing.Color.Black;
            this._markUp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markUp.Dock = System.Windows.Forms.DockStyle.Left;
            this._markUp.Location = new System.Drawing.Point(28, 87);
            this._markUp.Margin = new System.Windows.Forms.Padding(20, 7, 6, 7);
            this._markUp.Name = "_markUp";
            this._markUp.Size = new System.Drawing.Size(18, 18);
            this._markUp.TabIndex = 4;
            // 
            // lblUpSensorCaption
            // 
            this.lblUpSensorCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUpSensorCaption.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblUpSensorCaption.Location = new System.Drawing.Point(161, 80);
            this.lblUpSensorCaption.Name = "lblUpSensorCaption";
            this.lblUpSensorCaption.Size = new System.Drawing.Size(136, 32);
            this.lblUpSensorCaption.TabIndex = 5;
            this.lblUpSensorCaption.Text = "UP SENSOR";
            this.lblUpSensorCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _markDown
            // 
            this._markDown.BackColor = System.Drawing.Color.Black;
            this._markDown.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markDown.Dock = System.Windows.Forms.DockStyle.Left;
            this._markDown.Location = new System.Drawing.Point(28, 119);
            this._markDown.Margin = new System.Windows.Forms.Padding(20, 7, 6, 7);
            this._markDown.Name = "_markDown";
            this._markDown.Size = new System.Drawing.Size(18, 18);
            this._markDown.TabIndex = 6;
            // 
            // lblDownSensorCaption
            // 
            this.lblDownSensorCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDownSensorCaption.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblDownSensorCaption.Location = new System.Drawing.Point(161, 112);
            this.lblDownSensorCaption.Name = "lblDownSensorCaption";
            this.lblDownSensorCaption.Size = new System.Drawing.Size(136, 32);
            this.lblDownSensorCaption.TabIndex = 7;
            this.lblDownSensorCaption.Text = "DOWN SENSOR";
            this.lblDownSensorCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _markUnclamp
            // 
            this._markUnclamp.BackColor = System.Drawing.Color.Black;
            this._markUnclamp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._markUnclamp.Dock = System.Windows.Forms.DockStyle.Left;
            this._markUnclamp.Location = new System.Drawing.Point(28, 151);
            this._markUnclamp.Margin = new System.Windows.Forms.Padding(20, 7, 6, 7);
            this._markUnclamp.Name = "_markUnclamp";
            this._markUnclamp.Size = new System.Drawing.Size(18, 22);
            this._markUnclamp.TabIndex = 8;
            // 
            // lblUnclampSensorCaption
            // 
            this.lblUnclampSensorCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUnclampSensorCaption.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblUnclampSensorCaption.Location = new System.Drawing.Point(161, 144);
            this.lblUnclampSensorCaption.Name = "lblUnclampSensorCaption";
            this.lblUnclampSensorCaption.Size = new System.Drawing.Size(136, 36);
            this.lblUnclampSensorCaption.TabIndex = 9;
            this.lblUnclampSensorCaption.Text = "UNCLAMP SENSOR";
            this.lblUnclampSensorCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // materialDetailView
            // 
            this.materialDetailView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.materialDetailView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialDetailView.Location = new System.Drawing.Point(595, 11);
            this.materialDetailView.Name = "materialDetailView";
            this.materialDetailView.Size = new System.Drawing.Size(1062, 636);
            this.materialDetailView.TabIndex = 2;
            // 
            // actionPanel
            // 
            this.actionPanel.AutoScroll = true;
            this.actionPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.actionPanel.Controls.Add(this.btnLoadFromCassette);
            this.actionPanel.Controls.Add(this.btnLoadToStage);
            this.actionPanel.Controls.Add(this.btnUnloadFromStage);
            this.actionPanel.Controls.Add(this.btnUnloadToCassette);
            this.actionPanel.Controls.Add(this.btnRecover);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionPanel.Margin = new System.Windows.Forms.Padding(0);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.actionPanel.TabIndex = 0;
            this.actionPanel.WrapContents = false;
            //
            // actionBar
            //
            this.actionBar.ColumnCount = 2;
            this.actionBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 54F));
            this.actionBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 46F));
            this.actionBar.Controls.Add(this.actionPanel, 0, 0);
            this.actionBar.Controls.Add(this.actionRightPanel, 1, 0);
            this.actionBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionBar.Margin = new System.Windows.Forms.Padding(0);
            this.actionBar.Name = "actionBar";
            this.actionBar.RowCount = 1;
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionBar.TabIndex = 3;
            //
            // actionRightPanel
            //
            this.actionRightPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.actionRightPanel.Controls.Add(this.btnStop);
            this.actionRightPanel.AutoSize = true;
            this.actionRightPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.actionRightPanel.Dock = System.Windows.Forms.DockStyle.Right;
            this.actionRightPanel.Margin = new System.Windows.Forms.Padding(0);
            this.actionRightPanel.Name = "actionRightPanel";
            this.actionRightPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.actionRightPanel.TabIndex = 1;
            this.actionRightPanel.WrapContents = false;
            // 
            // btnLoadFromCassette
            // 
            this.btnLoadFromCassette.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnLoadFromCassette.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoadFromCassette.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnLoadFromCassette.ForeColor = System.Drawing.Color.White;
            this.btnLoadFromCassette.Location = new System.Drawing.Point(18, 18);
            this.btnLoadFromCassette.Margin = new System.Windows.Forms.Padding(6);
            this.btnLoadFromCassette.Name = "btnLoadFromCassette";
            this.btnLoadFromCassette.Size = new System.Drawing.Size(132, 60);
            this.btnLoadFromCassette.TabIndex = 0;
            this.btnLoadFromCassette.Text = "CST -> FEEDER";
            // 
            // btnLoadToStage
            // 
            this.btnLoadToStage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnLoadToStage.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoadToStage.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnLoadToStage.ForeColor = System.Drawing.Color.White;
            this.btnLoadToStage.Location = new System.Drawing.Point(210, 18);
            this.btnLoadToStage.Margin = new System.Windows.Forms.Padding(6);
            this.btnLoadToStage.Name = "btnLoadToStage";
            this.btnLoadToStage.Size = new System.Drawing.Size(132, 60);
            this.btnLoadToStage.TabIndex = 1;
            this.btnLoadToStage.Text = "FEEDER -> STAGE";
            // 
            // btnUnloadFromStage
            // 
            this.btnUnloadFromStage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnUnloadFromStage.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUnloadFromStage.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnUnloadFromStage.ForeColor = System.Drawing.Color.White;
            this.btnUnloadFromStage.Location = new System.Drawing.Point(402, 18);
            this.btnUnloadFromStage.Margin = new System.Windows.Forms.Padding(6);
            this.btnUnloadFromStage.Name = "btnUnloadFromStage";
            this.btnUnloadFromStage.Size = new System.Drawing.Size(132, 60);
            this.btnUnloadFromStage.TabIndex = 2;
            this.btnUnloadFromStage.Text = "STAGE -> FEEDER";
            // 
            // btnUnloadToCassette
            // 
            this.btnUnloadToCassette.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnUnloadToCassette.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUnloadToCassette.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnUnloadToCassette.ForeColor = System.Drawing.Color.White;
            this.btnUnloadToCassette.Location = new System.Drawing.Point(594, 18);
            this.btnUnloadToCassette.Margin = new System.Windows.Forms.Padding(6);
            this.btnUnloadToCassette.Name = "btnUnloadToCassette";
            this.btnUnloadToCassette.Size = new System.Drawing.Size(132, 60);
            this.btnUnloadToCassette.TabIndex = 3;
            this.btnUnloadToCassette.Text = "FEEDER -> CST";
            // 
            // btnRecover
            // 
            this.btnRecover.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnRecover.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRecover.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnRecover.ForeColor = System.Drawing.Color.White;
            this.btnRecover.Location = new System.Drawing.Point(786, 18);
            this.btnRecover.Margin = new System.Windows.Forms.Padding(6);
            this.btnRecover.Name = "btnRecover";
            this.btnRecover.Size = new System.Drawing.Size(132, 60);
            this.btnRecover.TabIndex = 4;
            this.btnRecover.Text = "RECOVER";
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(214)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(978, 18);
            this.btnStop.Margin = new System.Windows.Forms.Padding(6);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(132, 60);
            this.btnStop.TabIndex = 5;
            this.btnStop.Text = "STOP";
            // 
            // OutputFeederPage
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this.rootPanel);
            this.Name = "OutputFeederPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootPanel.ResumeLayout(false);
            this.topLayout.ResumeLayout(false);
            this.axisPanel.ResumeLayout(false);
            this.ringPanel.ResumeLayout(false);
            this.overloadPanel.ResumeLayout(false);
            this.outputSignalPanel.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.pnlWork.ResumeLayout(false);
            this.workLayout.ResumeLayout(false);
            this.targetSideLayout.ResumeLayout(false);
            this.pnlCylinder.ResumeLayout(false);
            this.cylinderLayout.ResumeLayout(false);
            this.actionPanel.ResumeLayout(false);
            this.actionRightPanel.ResumeLayout(false);
            this.actionBar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
