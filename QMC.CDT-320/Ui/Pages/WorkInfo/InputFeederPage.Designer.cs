using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class InputFeederPage
    {
        private TableLayoutPanel rootPanel;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private GroupBox pnlInfo;
        private TableLayoutPanel infoLayout;
        private TableLayoutPanel leftInfo;
        private TableLayoutPanel rightInfo;
        private GroupBox grpAction;
        private TableLayoutPanel actionBar;
        private MaterialDetailView materialDetailView;
        private Label lblExistCaption;
        private Label _lblExist;
        private Label lblClampCaption;
        private Label _lblClampState;
        private Label lblUpDownCaption;
        private Label _lblUpDownState;
        private TableLayoutPanel lifterAxisPanel;
        private Label lblLifterAxisTitle;
        private Label _lblLifterPos;
        private TableLayoutPanel ringOverloadPanel;
        private Label lblRingCaption;
        private IndicatorDot dotRing;
        private Label lblOverloadCaption;
        private IndicatorDot dotOverload;
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
            this.pnlInfo = new System.Windows.Forms.GroupBox();
            this.infoLayout = new System.Windows.Forms.TableLayoutPanel();
            this.leftInfo = new System.Windows.Forms.TableLayoutPanel();
            this.lblExistCaption = new System.Windows.Forms.Label();
            this._lblExist = new System.Windows.Forms.Label();
            this.lblClampCaption = new System.Windows.Forms.Label();
            this._lblClampState = new System.Windows.Forms.Label();
            this.lblUpDownCaption = new System.Windows.Forms.Label();
            this._lblUpDownState = new System.Windows.Forms.Label();
            this.rightInfo = new System.Windows.Forms.TableLayoutPanel();
            this.lifterAxisPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblLifterAxisTitle = new System.Windows.Forms.Label();
            this._lblLifterPos = new System.Windows.Forms.Label();
            this.ringOverloadPanel = new System.Windows.Forms.TableLayoutPanel();
            this.dotRing = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblRingCaption = new System.Windows.Forms.Label();
            this.dotOverload = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblOverloadCaption = new System.Windows.Forms.Label();
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
            this.pnlInfo.SuspendLayout();
            this.infoLayout.SuspendLayout();
            this.leftInfo.SuspendLayout();
            this.rightInfo.SuspendLayout();
            this.lifterAxisPanel.SuspendLayout();
            this.ringOverloadPanel.SuspendLayout();
            this.grpAction.SuspendLayout();
            this.actionBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootPanel
            // 
            this.rootPanel.BackColor = System.Drawing.Color.White;
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
            this.lblHeader.Tag = "i18n:wi.inputFeeder";
            this.lblHeader.Text = "INPUT FEEDER";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.BackColor = System.Drawing.Color.White;
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.Controls.Add(this.pnlInfo, 0, 0);
            this.contentLayout.Controls.Add(this.grpAction, 0, 1);
            this.contentLayout.Controls.Add(this.materialDetailView, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.RowCount = 2;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1678, 870);
            this.contentLayout.TabIndex = 1;
            // 
            // pnlInfo
            // 
            this.pnlInfo.BackColor = System.Drawing.Color.White;
            this.pnlInfo.Controls.Add(this.infoLayout);
            this.pnlInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.pnlInfo.Location = new System.Drawing.Point(0, 0);
            this.pnlInfo.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3);
            this.pnlInfo.Name = "pnlInfo";
            this.pnlInfo.Size = new System.Drawing.Size(836, 297);
            this.pnlInfo.TabIndex = 0;
            this.pnlInfo.TabStop = false;
            this.pnlInfo.Text = "INFO";
            // 
            // infoLayout
            // 
            this.infoLayout.ColumnCount = 2;
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 77F));
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23F));
            this.infoLayout.Controls.Add(this.leftInfo, 0, 0);
            this.infoLayout.Controls.Add(this.rightInfo, 1, 0);
            this.infoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoLayout.Location = new System.Drawing.Point(3, 23);
            this.infoLayout.Name = "infoLayout";
            this.infoLayout.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.infoLayout.RowCount = 1;
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.infoLayout.Size = new System.Drawing.Size(830, 271);
            this.infoLayout.TabIndex = 0;
            // 
            // leftInfo
            // 
            this.leftInfo.ColumnCount = 2;
            this.leftInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.66093F));
            this.leftInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66.33907F));
            this.leftInfo.Controls.Add(this.lblExistCaption, 0, 0);
            this.leftInfo.Controls.Add(this._lblExist, 1, 0);
            this.leftInfo.Controls.Add(this.lblClampCaption, 0, 1);
            this.leftInfo.Controls.Add(this._lblClampState, 1, 1);
            this.leftInfo.Controls.Add(this.lblUpDownCaption, 0, 2);
            this.leftInfo.Controls.Add(this._lblUpDownState, 1, 2);
            this.leftInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftInfo.Location = new System.Drawing.Point(6, 4);
            this.leftInfo.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.leftInfo.Name = "leftInfo";
            this.leftInfo.RowCount = 3;
            this.leftInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.leftInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.leftInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.leftInfo.Size = new System.Drawing.Size(623, 263);
            this.leftInfo.TabIndex = 0;
            // 
            // lblExistCaption
            // 
            this.lblExistCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblExistCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExistCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblExistCaption.Location = new System.Drawing.Point(2, 2);
            this.lblExistCaption.Margin = new System.Windows.Forms.Padding(2);
            this.lblExistCaption.Name = "lblExistCaption";
            this.lblExistCaption.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblExistCaption.Size = new System.Drawing.Size(205, 83);
            this.lblExistCaption.TabIndex = 0;
            this.lblExistCaption.Text = "EXIST";
            this.lblExistCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblExist
            // 
            this._lblExist.BackColor = System.Drawing.Color.White;
            this._lblExist.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblExist.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblExist.Location = new System.Drawing.Point(211, 2);
            this._lblExist.Margin = new System.Windows.Forms.Padding(2);
            this._lblExist.Name = "_lblExist";
            this._lblExist.Size = new System.Drawing.Size(410, 83);
            this._lblExist.TabIndex = 1;
            this._lblExist.Text = "--";
            this._lblExist.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblClampCaption
            // 
            this.lblClampCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblClampCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblClampCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblClampCaption.Location = new System.Drawing.Point(2, 89);
            this.lblClampCaption.Margin = new System.Windows.Forms.Padding(2);
            this.lblClampCaption.Name = "lblClampCaption";
            this.lblClampCaption.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblClampCaption.Size = new System.Drawing.Size(205, 83);
            this.lblClampCaption.TabIndex = 4;
            this.lblClampCaption.Text = "FEEDER CLAMP";
            this.lblClampCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblClampState
            // 
            this._lblClampState.BackColor = System.Drawing.Color.White;
            this._lblClampState.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblClampState.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblClampState.Location = new System.Drawing.Point(211, 89);
            this._lblClampState.Margin = new System.Windows.Forms.Padding(2);
            this._lblClampState.Name = "_lblClampState";
            this._lblClampState.Size = new System.Drawing.Size(410, 83);
            this._lblClampState.TabIndex = 5;
            this._lblClampState.Text = "--";
            this._lblClampState.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblUpDownCaption
            // 
            this.lblUpDownCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblUpDownCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUpDownCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblUpDownCaption.Location = new System.Drawing.Point(2, 176);
            this.lblUpDownCaption.Margin = new System.Windows.Forms.Padding(2);
            this.lblUpDownCaption.Name = "lblUpDownCaption";
            this.lblUpDownCaption.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblUpDownCaption.Size = new System.Drawing.Size(205, 85);
            this.lblUpDownCaption.TabIndex = 6;
            this.lblUpDownCaption.Text = "FEEDER UP DOWN";
            this.lblUpDownCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblUpDownState
            // 
            this._lblUpDownState.BackColor = System.Drawing.Color.White;
            this._lblUpDownState.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblUpDownState.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblUpDownState.Location = new System.Drawing.Point(211, 176);
            this._lblUpDownState.Margin = new System.Windows.Forms.Padding(2);
            this._lblUpDownState.Name = "_lblUpDownState";
            this._lblUpDownState.Size = new System.Drawing.Size(410, 85);
            this._lblUpDownState.TabIndex = 7;
            this._lblUpDownState.Text = "--";
            this._lblUpDownState.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // rightInfo
            // 
            this.rightInfo.ColumnCount = 1;
            this.rightInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightInfo.Controls.Add(this.lifterAxisPanel, 0, 0);
            this.rightInfo.Controls.Add(this.ringOverloadPanel, 0, 1);
            this.rightInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightInfo.Location = new System.Drawing.Point(635, 4);
            this.rightInfo.Margin = new System.Windows.Forms.Padding(0);
            this.rightInfo.Name = "rightInfo";
            this.rightInfo.RowCount = 2;
            this.rightInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 79F));
            this.rightInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightInfo.Size = new System.Drawing.Size(189, 263);
            this.rightInfo.TabIndex = 1;
            // 
            // lifterAxisPanel
            // 
            this.lifterAxisPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lifterAxisPanel.ColumnCount = 1;
            this.lifterAxisPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.lifterAxisPanel.Controls.Add(this.lblLifterAxisTitle, 0, 0);
            this.lifterAxisPanel.Controls.Add(this._lblLifterPos, 0, 1);
            this.lifterAxisPanel.Location = new System.Drawing.Point(4, 4);
            this.lifterAxisPanel.Margin = new System.Windows.Forms.Padding(4);
            this.lifterAxisPanel.Name = "lifterAxisPanel";
            this.lifterAxisPanel.RowCount = 2;
            this.lifterAxisPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.lifterAxisPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lifterAxisPanel.Size = new System.Drawing.Size(181, 71);
            this.lifterAxisPanel.TabIndex = 0;
            // 
            // lblLifterAxisTitle
            // 
            this.lblLifterAxisTitle.BackColor = System.Drawing.Color.Black;
            this.lblLifterAxisTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLifterAxisTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblLifterAxisTitle.ForeColor = System.Drawing.Color.White;
            this.lblLifterAxisTitle.Location = new System.Drawing.Point(3, 0);
            this.lblLifterAxisTitle.Name = "lblLifterAxisTitle";
            this.lblLifterAxisTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblLifterAxisTitle.Size = new System.Drawing.Size(175, 24);
            this.lblLifterAxisTitle.TabIndex = 0;
            this.lblLifterAxisTitle.Text = "LIFTER AXIS Z";
            this.lblLifterAxisTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblLifterPos
            // 
            this._lblLifterPos.BackColor = System.Drawing.Color.White;
            this._lblLifterPos.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblLifterPos.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblLifterPos.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblLifterPos.Location = new System.Drawing.Point(3, 24);
            this._lblLifterPos.Name = "_lblLifterPos";
            this._lblLifterPos.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this._lblLifterPos.Size = new System.Drawing.Size(175, 47);
            this._lblLifterPos.TabIndex = 1;
            this._lblLifterPos.Text = "0.000 mm";
            this._lblLifterPos.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ringOverloadPanel
            // 
            this.ringOverloadPanel.BackColor = System.Drawing.Color.White;
            this.ringOverloadPanel.ColumnCount = 2;
            this.ringOverloadPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.ringOverloadPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ringOverloadPanel.Controls.Add(this.dotRing, 0, 0);
            this.ringOverloadPanel.Controls.Add(this.lblRingCaption, 1, 0);
            this.ringOverloadPanel.Controls.Add(this.dotOverload, 0, 1);
            this.ringOverloadPanel.Controls.Add(this.lblOverloadCaption, 1, 1);
            this.ringOverloadPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ringOverloadPanel.Location = new System.Drawing.Point(4, 83);
            this.ringOverloadPanel.Margin = new System.Windows.Forms.Padding(4);
            this.ringOverloadPanel.Name = "ringOverloadPanel";
            this.ringOverloadPanel.RowCount = 2;
            this.ringOverloadPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ringOverloadPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ringOverloadPanel.Size = new System.Drawing.Size(181, 176);
            this.ringOverloadPanel.TabIndex = 8;
            // 
            // dotRing
            // 
            this.dotRing.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotRing.BackColor = System.Drawing.Color.White;
            this.dotRing.Location = new System.Drawing.Point(8, 27);
            this.dotRing.Name = "dotRing";
            this.dotRing.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotRing.OnColor = System.Drawing.Color.LimeGreen;
            this.dotRing.Size = new System.Drawing.Size(34, 34);
            this.dotRing.TabIndex = 0;
            // 
            // lblRingCaption
            // 
            this.lblRingCaption.BackColor = System.Drawing.Color.White;
            this.lblRingCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRingCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblRingCaption.Location = new System.Drawing.Point(50, 0);
            this.lblRingCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblRingCaption.Name = "lblRingCaption";
            this.lblRingCaption.Size = new System.Drawing.Size(131, 88);
            this.lblRingCaption.TabIndex = 1;
            this.lblRingCaption.Text = "RING CHECK";
            this.lblRingCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dotOverload
            // 
            this.dotOverload.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotOverload.BackColor = System.Drawing.Color.White;
            this.dotOverload.Location = new System.Drawing.Point(8, 115);
            this.dotOverload.Name = "dotOverload";
            this.dotOverload.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotOverload.OnColor = System.Drawing.Color.Red;
            this.dotOverload.Size = new System.Drawing.Size(34, 34);
            this.dotOverload.TabIndex = 2;
            // 
            // lblOverloadCaption
            // 
            this.lblOverloadCaption.BackColor = System.Drawing.Color.White;
            this.lblOverloadCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOverloadCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblOverloadCaption.Location = new System.Drawing.Point(50, 88);
            this.lblOverloadCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblOverloadCaption.Name = "lblOverloadCaption";
            this.lblOverloadCaption.Size = new System.Drawing.Size(131, 88);
            this.lblOverloadCaption.TabIndex = 3;
            this.lblOverloadCaption.Text = "OVERLOAD CHECK";
            this.lblOverloadCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpAction
            // 
            this.grpAction.BackColor = System.Drawing.Color.White;
            this.grpAction.Controls.Add(this.actionBar);
            this.grpAction.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.grpAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAction.Location = new System.Drawing.Point(0, 702);
            this.grpAction.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
            this.grpAction.Name = "grpAction";
            this.grpAction.Size = new System.Drawing.Size(836, 168);
            this.grpAction.TabIndex = 1;
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
            this.actionBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionBar.Location = new System.Drawing.Point(3, 23);
            this.actionBar.Margin = new System.Windows.Forms.Padding(0);
            this.actionBar.Name = "actionBar";
            this.actionBar.Padding = new System.Windows.Forms.Padding(3, 1, 3, 0);
            this.actionBar.RowCount = 4;
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionBar.Size = new System.Drawing.Size(830, 142);
            this.actionBar.TabIndex = 0;
            // 
            // btnLoadFromCassette
            // 
            this.btnLoadFromCassette.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnLoadFromCassette.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoadFromCassette.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadFromCassette.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnLoadFromCassette.ForeColor = System.Drawing.Color.White;
            this.btnLoadFromCassette.Location = new System.Drawing.Point(6, 1);
            this.btnLoadFromCassette.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.btnLoadFromCassette.Name = "btnLoadFromCassette";
            this.btnLoadFromCassette.Size = new System.Drawing.Size(406, 44);
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
            this.btnLoadToStage.Location = new System.Drawing.Point(418, 1);
            this.btnLoadToStage.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.btnLoadToStage.Name = "btnLoadToStage";
            this.btnLoadToStage.Size = new System.Drawing.Size(406, 44);
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
            this.btnUnloadFromStage.Location = new System.Drawing.Point(6, 47);
            this.btnUnloadFromStage.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.btnUnloadFromStage.Name = "btnUnloadFromStage";
            this.btnUnloadFromStage.Size = new System.Drawing.Size(406, 44);
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
            this.btnUnloadToCassette.Location = new System.Drawing.Point(418, 47);
            this.btnUnloadToCassette.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.btnUnloadToCassette.Name = "btnUnloadToCassette";
            this.btnUnloadToCassette.Size = new System.Drawing.Size(406, 44);
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
            this.btnRecover.Location = new System.Drawing.Point(6, 93);
            this.btnRecover.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.btnRecover.Name = "btnRecover";
            this.btnRecover.Size = new System.Drawing.Size(406, 44);
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
            this.btnStop.Location = new System.Drawing.Point(418, 93);
            this.btnStop.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(406, 44);
            this.btnStop.TabIndex = 5;
            this.btnStop.Text = "STOP";
            // 
            // materialDetailView
            // 
            this.materialDetailView.BackColor = System.Drawing.Color.White;
            this.materialDetailView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialDetailView.Location = new System.Drawing.Point(842, 0);
            this.materialDetailView.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.materialDetailView.Name = "materialDetailView";
            this.contentLayout.SetRowSpan(this.materialDetailView, 2);
            this.materialDetailView.ShowProcessTestDataButton = false;
            this.materialDetailView.Size = new System.Drawing.Size(836, 870);
            this.materialDetailView.TabIndex = 2;
            // 
            // InputFeederPage
            // 
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootPanel);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "InputFeederPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootPanel.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.pnlInfo.ResumeLayout(false);
            this.infoLayout.ResumeLayout(false);
            this.leftInfo.ResumeLayout(false);
            this.rightInfo.ResumeLayout(false);
            this.lifterAxisPanel.ResumeLayout(false);
            this.ringOverloadPanel.ResumeLayout(false);
            this.grpAction.ResumeLayout(false);
            this.actionBar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
