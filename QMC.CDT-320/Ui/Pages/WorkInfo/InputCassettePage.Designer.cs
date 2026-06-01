using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class InputCassettePage
    {
        private System.ComponentModel.IContainer components = null;

        // Root / content
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;

        // Left
        private TableLayoutPanel leftLayout;
        private GroupBox grpActions;
        private TableLayoutPanel actionLayout;
        private TableLayoutPanel slotNavLayout;
        private ActionButton btnInit;
        private ActionButton btnReady;
        private ActionButton btnMap;
        private ActionButton btnLoad;
        private ActionButton btnUnload;
        private ActionButton btnPrev;
        private ActionButton btnNext;

        private GroupBox grpIo;
        private IoCylinderPanelControl ioCylinderPanel;

        // Center
        private TableLayoutPanel centerLayout;
        private GroupBox grpStatus;
        private ParameterGridControl statusParameterGrid;
        private GroupBox grpSlotMap;
        private Panel slotMapHost;
        private TableLayoutPanel lifterLayout;

        // Right
        private TableLayoutPanel rightLayout;
        private GroupBox grpJog;
        private TableLayoutPanel jogCompositeLayout;
        private JogPositionListControl jogPositionListControl;
        private JogAxisMoveControl jogAxisMoveControl;
        private GroupBox grpSpeed;
        private JogSpeedControl jogSpeedControl;

        // Dynamic slot row controls (populated at runtime by BuildSlotRows)
        private Label[] _slotIndexLbls;
        private Label[] _slotLeds;
        private Label[] _slotNameLbls;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpActions = new System.Windows.Forms.GroupBox();
            this.actionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnInit = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReady = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnMap = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnLoad = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUnload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.slotNavLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnPrev = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNext = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.grpIo = new System.Windows.Forms.GroupBox();
            this.ioCylinderPanel = new QMC.CDT_320.Ui.Controls.IoCylinderPanelControl();
            this.centerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpStatus = new System.Windows.Forms.GroupBox();
            this.statusParameterGrid = new QMC.CDT_320.Ui.Controls.ParameterGridControl();
            this.grpSlotMap = new System.Windows.Forms.GroupBox();
            this.slotMapHost = new System.Windows.Forms.Panel();
            this.lifterLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpJog = new System.Windows.Forms.GroupBox();
            this.jogCompositeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.jogPositionListControl = new QMC.CDT_320.Ui.Controls.JogPositionListControl();
            this.jogAxisMoveControl = new QMC.CDT_320.Ui.Controls.JogAxisMoveControl();
            this.grpSpeed = new System.Windows.Forms.GroupBox();
            this.jogSpeedControl = new QMC.CDT_320.Ui.Controls.JogSpeedControl();

            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.grpActions.SuspendLayout();
            this.actionLayout.SuspendLayout();
            this.slotNavLayout.SuspendLayout();
            this.grpIo.SuspendLayout();
            this.centerLayout.SuspendLayout();
            this.grpStatus.SuspendLayout();
            this.grpSlotMap.SuspendLayout();
            this.slotMapHost.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.grpJog.SuspendLayout();
            this.jogCompositeLayout.SuspendLayout();
            this.grpSpeed.SuspendLayout();
            this.SuspendLayout();

            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1694, 980);
            this.rootLayout.TabIndex = 0;

            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("Malgun Gothic", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.lblHeader.Tag = "i18n:workInfo.inputCassette";
            this.lblHeader.Text = "INPUT CASSETTE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // 
            // contentLayout
            // 
            this.contentLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(219)))), ((int)(((byte)(222)))));
            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 296F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 680F));
            this.contentLayout.Controls.Add(this.leftLayout, 0, 0);
            this.contentLayout.Controls.Add(this.centerLayout, 1, 0);
            this.contentLayout.Controls.Add(this.rightLayout, 2, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(1);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 1;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Controls.Add(this.grpActions, 0, 0);
            this.leftLayout.Controls.Add(this.grpIo, 0, 1);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 610F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // 
            // grpActions
            // 
            this.grpActions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpActions.Controls.Add(this.actionLayout);
            this.grpActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpActions.Font = new System.Drawing.Font("Malgun Gothic", 10F, System.Drawing.FontStyle.Bold);
            this.grpActions.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpActions.Name = "grpActions";
            this.grpActions.TabStop = false;
            this.grpActions.Text = "ACTION";

            // 
            // actionLayout
            // 
            this.actionLayout.ColumnCount = 1;
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionLayout.Controls.Add(this.btnInit, 0, 0);
            this.actionLayout.Controls.Add(this.btnReady, 0, 1);
            this.actionLayout.Controls.Add(this.btnMap, 0, 2);
            this.actionLayout.Controls.Add(this.btnLoad, 0, 3);
            this.actionLayout.Controls.Add(this.btnUnload, 0, 4);
            this.actionLayout.Controls.Add(this.slotNavLayout, 0, 5);
            this.actionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionLayout.Name = "actionLayout";
            this.actionLayout.Padding = new System.Windows.Forms.Padding(12, 18, 12, 12);
            this.actionLayout.RowCount = 7;
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // 
            // btnInit
            // 
            this.btnInit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(94)))), ((int)(((byte)(103)))));
            this.btnInit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInit.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnInit.ForeColor = System.Drawing.Color.White;
            this.btnInit.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnInit.Name = "btnInit";
            this.btnInit.TabIndex = 0;
            this.btnInit.Text = "LIFTER INIT";

            // 
            // btnReady
            // 
            this.btnReady.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(94)))), ((int)(((byte)(103)))));
            this.btnReady.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReady.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReady.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnReady.ForeColor = System.Drawing.Color.White;
            this.btnReady.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnReady.Name = "btnReady";
            this.btnReady.TabIndex = 1;
            this.btnReady.Text = "LIFTER READY";

            // 
            // btnMap
            // 
            this.btnMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(94)))), ((int)(((byte)(103)))));
            this.btnMap.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMap.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnMap.ForeColor = System.Drawing.Color.White;
            this.btnMap.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnMap.Name = "btnMap";
            this.btnMap.TabIndex = 2;
            this.btnMap.Text = "MAP / SCAN";

            // 
            // btnLoad
            // 
            this.btnLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(94)))), ((int)(((byte)(103)))));
            this.btnLoad.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoad.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnLoad.ForeColor = System.Drawing.Color.White;
            this.btnLoad.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.TabIndex = 3;
            this.btnLoad.Text = "LOAD WAFER";

            // 
            // btnUnload
            // 
            this.btnUnload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(94)))), ((int)(((byte)(103)))));
            this.btnUnload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUnload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUnload.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnUnload.ForeColor = System.Drawing.Color.White;
            this.btnUnload.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnUnload.Name = "btnUnload";
            this.btnUnload.TabIndex = 4;
            this.btnUnload.Text = "UNLOAD WAFER";

            // 
            // slotNavLayout
            // 
            this.slotNavLayout.ColumnCount = 2;
            this.slotNavLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.slotNavLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.slotNavLayout.Controls.Add(this.btnPrev, 0, 0);
            this.slotNavLayout.Controls.Add(this.btnNext, 1, 0);
            this.slotNavLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.slotNavLayout.Margin = new System.Windows.Forms.Padding(0);
            this.slotNavLayout.Name = "slotNavLayout";
            this.slotNavLayout.RowCount = 1;
            this.slotNavLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // 
            // btnPrev
            // 
            this.btnPrev.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(94)))), ((int)(((byte)(103)))));
            this.btnPrev.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrev.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPrev.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnPrev.ForeColor = System.Drawing.Color.White;
            this.btnPrev.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.TabIndex = 5;
            this.btnPrev.Text = "SLOT \u25C0";

            // 
            // btnNext
            // 
            this.btnNext.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(94)))), ((int)(((byte)(103)))));
            this.btnNext.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNext.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNext.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnNext.ForeColor = System.Drawing.Color.White;
            this.btnNext.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnNext.Name = "btnNext";
            this.btnNext.TabIndex = 6;
            this.btnNext.Text = "SLOT \u25B6";

            // 
            // grpIo
            // 
            this.grpIo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpIo.Controls.Add(this.ioCylinderPanel);
            this.grpIo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpIo.Font = new System.Drawing.Font("Malgun Gothic", 10F, System.Drawing.FontStyle.Bold);
            this.grpIo.Margin = new System.Windows.Forms.Padding(0);
            this.grpIo.Name = "grpIo";
            this.grpIo.TabStop = false;
            this.grpIo.Text = "CYLINDER && I/O";

            // 
            // ioCylinderPanel
            // 
            this.ioCylinderPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(205)))), ((int)(((byte)(205)))), ((int)(((byte)(205)))));
            this.ioCylinderPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioCylinderPanel.Margin = new System.Windows.Forms.Padding(0);
            this.ioCylinderPanel.Name = "ioCylinderPanel";

            // 
            // centerLayout
            // 
            this.centerLayout.ColumnCount = 1;
            this.centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerLayout.Controls.Add(this.grpStatus, 0, 0);
            this.centerLayout.Controls.Add(this.grpSlotMap, 0, 1);
            this.centerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centerLayout.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.centerLayout.Name = "centerLayout";
            this.centerLayout.RowCount = 2;
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 360F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // 
            // grpStatus
            // 
            this.grpStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpStatus.Controls.Add(this.statusParameterGrid);
            this.grpStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpStatus.Font = new System.Drawing.Font("Malgun Gothic", 10F, System.Drawing.FontStyle.Bold);
            this.grpStatus.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpStatus.Name = "grpStatus";
            this.grpStatus.TabStop = false;
            this.grpStatus.Text = "STATUS";

            // 
            // statusParameterGrid
            // 
            this.statusParameterGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.statusParameterGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusParameterGrid.Margin = new System.Windows.Forms.Padding(0);
            this.statusParameterGrid.Name = "statusParameterGrid";

            // 
            // grpSlotMap
            // 
            this.grpSlotMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpSlotMap.Controls.Add(this.slotMapHost);
            this.grpSlotMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSlotMap.Font = new System.Drawing.Font("Malgun Gothic", 10F, System.Drawing.FontStyle.Bold);
            this.grpSlotMap.Margin = new System.Windows.Forms.Padding(0);
            this.grpSlotMap.Name = "grpSlotMap";
            this.grpSlotMap.TabStop = false;
            this.grpSlotMap.Text = "SLOT MAP";

            // 
            // slotMapHost
            // 
            this.slotMapHost.AutoScroll = true;
            this.slotMapHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.slotMapHost.Controls.Add(this.lifterLayout);
            this.slotMapHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.slotMapHost.Margin = new System.Windows.Forms.Padding(0);
            this.slotMapHost.Name = "slotMapHost";
            this.slotMapHost.Padding = new System.Windows.Forms.Padding(12, 8, 12, 12);

            // 
            // lifterLayout (rows are populated dynamically by BuildSlotRows)
            // 
            this.lifterLayout.AutoSize = true;
            this.lifterLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.lifterLayout.ColumnCount = 3;
            this.lifterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.lifterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.lifterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lifterLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.lifterLayout.Margin = new System.Windows.Forms.Padding(0);
            this.lifterLayout.Name = "lifterLayout";

            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 2;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.rightLayout.Controls.Add(this.grpJog, 0, 0);
            this.rightLayout.Controls.Add(this.grpSpeed, 1, 0);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 1;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // 
            // grpJog
            // 
            this.grpJog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpJog.Controls.Add(this.jogCompositeLayout);
            this.grpJog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpJog.Font = new System.Drawing.Font("Malgun Gothic", 10F, System.Drawing.FontStyle.Bold);
            this.grpJog.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.grpJog.Name = "grpJog";
            this.grpJog.TabStop = false;
            this.grpJog.Text = "JOG";

            // 
            // jogCompositeLayout
            // 
            this.jogCompositeLayout.ColumnCount = 1;
            this.jogCompositeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogCompositeLayout.Controls.Add(this.jogPositionListControl, 0, 0);
            this.jogCompositeLayout.Controls.Add(this.jogAxisMoveControl, 0, 1);
            this.jogCompositeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogCompositeLayout.Margin = new System.Windows.Forms.Padding(0);
            this.jogCompositeLayout.Name = "jogCompositeLayout";
            this.jogCompositeLayout.RowCount = 2;
            this.jogCompositeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this.jogCompositeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // 
            // jogPositionListControl
            // 
            this.jogPositionListControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogPositionListControl.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.jogPositionListControl.Name = "jogPositionListControl";

            // 
            // jogAxisMoveControl
            // 
            this.jogAxisMoveControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.jogAxisMoveControl.ButtonAreaMaxHeight = 260;
            this.jogAxisMoveControl.ButtonAreaMaxWidth = 340;
            this.jogAxisMoveControl.ButtonAreaMinHeight = 190;
            this.jogAxisMoveControl.ButtonAreaMinWidth = 240;
            this.jogAxisMoveControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogAxisMoveControl.Margin = new System.Windows.Forms.Padding(0);
            this.jogAxisMoveControl.Name = "jogAxisMoveControl";
            this.jogAxisMoveControl.SpeedControl = null;

            // 
            // grpSpeed
            // 
            this.grpSpeed.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpSpeed.Controls.Add(this.jogSpeedControl);
            this.grpSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSpeed.Font = new System.Drawing.Font("Malgun Gothic", 10F, System.Drawing.FontStyle.Bold);
            this.grpSpeed.Margin = new System.Windows.Forms.Padding(0);
            this.grpSpeed.Name = "grpSpeed";
            this.grpSpeed.TabStop = false;
            this.grpSpeed.Text = "SPEED";

            // 
            // jogSpeedControl
            // 
            this.jogSpeedControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.jogSpeedControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogSpeedControl.Margin = new System.Windows.Forms.Padding(0);
            this.jogSpeedControl.Name = "jogSpeedControl";
            this.jogSpeedControl.SpeedPercent = 50;

            // 
            // InputCassettePage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "InputCassettePage";
            this.Size = new System.Drawing.Size(1694, 980);

            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.grpActions.ResumeLayout(false);
            this.actionLayout.ResumeLayout(false);
            this.slotNavLayout.ResumeLayout(false);
            this.grpIo.ResumeLayout(false);
            this.centerLayout.ResumeLayout(false);
            this.grpStatus.ResumeLayout(false);
            this.grpSlotMap.ResumeLayout(false);
            this.slotMapHost.ResumeLayout(false);
            this.slotMapHost.PerformLayout();
            this.rightLayout.ResumeLayout(false);
            this.grpJog.ResumeLayout(false);
            this.jogCompositeLayout.ResumeLayout(false);
            this.grpSpeed.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
