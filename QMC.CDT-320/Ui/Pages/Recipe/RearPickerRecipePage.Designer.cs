using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class RearPickerRecipePage
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel contentLayout;
        private System.Windows.Forms.TableLayoutPanel centerLayout;
        private System.Windows.Forms.TableLayoutPanel leftLayout;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.GroupBox grpVision;
        private System.Windows.Forms.TabControl tabVision;
        private System.Windows.Forms.TabPage tabBottom;
        private System.Windows.Forms.TabPage tabSide;
        private System.Windows.Forms.Panel visionPanel;
        private System.Windows.Forms.Label lblVisionInfo;
        private System.Windows.Forms.TableLayoutPanel sideLayout;
        private System.Windows.Forms.Label lblVisionInfo2;
        private System.Windows.Forms.Label lblVisionInfo3;
        private System.Windows.Forms.GroupBox grpManual;
        private System.Windows.Forms.Panel manualPanel;
        private System.Windows.Forms.TableLayoutPanel manualLayout;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAvoidPosition;
        private QMC.CDT_320.Ui.Controls.ActionButton btnPickPosition;
        private QMC.CDT_320.Ui.Controls.ActionButton btnBottomPosition;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSidePosition;
        private QMC.CDT_320.Ui.Controls.ActionButton btnPlacePosition;
        private QMC.CDT_320.Ui.Controls.ActionButton btnDiePickPosition;
        private QMC.CDT_320.Ui.Controls.ActionButton btnDieBottomPosition;
        private QMC.CDT_320.Ui.Controls.ActionButton btnDieSidePosition;
        private QMC.CDT_320.Ui.Controls.ActionButton btnDiePlacePosition;
        private System.Windows.Forms.GroupBox grpOptions;
        private ParameterGridControl optionParameterGrid;
        private System.Windows.Forms.GroupBox grpWait;
        private ParameterGridControl waitParameterGrid;
        private System.Windows.Forms.GroupBox grpIo;
        private IoCylinderPanelControl ioCylinderPanel;
        private System.Windows.Forms.GroupBox grpJog;
        private System.Windows.Forms.TableLayoutPanel jogLayout;
        private JogPositionListControl jogPositionListControl;
        private JogAxisMoveControl jogAxisMoveControl;
        private System.Windows.Forms.GroupBox grpSpeed;
        private JogSpeedControl jogSpeedControl;

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
            this.centerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpVision = new System.Windows.Forms.GroupBox();
            this.tabVision = new System.Windows.Forms.TabControl();
            this.tabBottom = new System.Windows.Forms.TabPage();
            this.visionPanel = new System.Windows.Forms.Panel();
            this.lblVisionInfo = new System.Windows.Forms.Label();
            this.tabSide = new System.Windows.Forms.TabPage();
            this.sideLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblVisionInfo2 = new System.Windows.Forms.Label();
            this.lblVisionInfo3 = new System.Windows.Forms.Label();
            this.grpManual = new System.Windows.Forms.GroupBox();
            this.manualPanel = new System.Windows.Forms.Panel();
            this.manualLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnAvoidPosition = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnPickPosition = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnBottomPosition = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSidePosition = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnPlacePosition = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnDiePickPosition = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnDieBottomPosition = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnDieSidePosition = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnDiePlacePosition = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpOptions = new System.Windows.Forms.GroupBox();
            this.optionParameterGrid = new QMC.CDT_320.Ui.Controls.ParameterGridControl();
            this.grpWait = new System.Windows.Forms.GroupBox();
            this.waitParameterGrid = new QMC.CDT_320.Ui.Controls.ParameterGridControl();
            this.grpIo = new System.Windows.Forms.GroupBox();
            this.ioCylinderPanel = new QMC.CDT_320.Ui.Controls.IoCylinderPanelControl();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpJog = new System.Windows.Forms.GroupBox();
            this.jogLayout = new System.Windows.Forms.TableLayoutPanel();
            this.jogPositionListControl = new QMC.CDT_320.Ui.Controls.JogPositionListControl();
            this.jogAxisMoveControl = new QMC.CDT_320.Ui.Controls.JogAxisMoveControl();
            this.grpSpeed = new System.Windows.Forms.GroupBox();
            this.jogSpeedControl = new QMC.CDT_320.Ui.Controls.JogSpeedControl();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.centerLayout.SuspendLayout();
            this.grpVision.SuspendLayout();
            this.tabVision.SuspendLayout();
            this.tabBottom.SuspendLayout();
            this.visionPanel.SuspendLayout();
            this.tabSide.SuspendLayout();
            this.sideLayout.SuspendLayout();
            this.grpManual.SuspendLayout();
            this.manualPanel.SuspendLayout();
            this.manualLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.grpOptions.SuspendLayout();
            this.grpWait.SuspendLayout();
            this.grpIo.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.grpJog.SuspendLayout();
            this.jogLayout.SuspendLayout();
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
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1678, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "REAR PICKER";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 428F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 93F));
            this.contentLayout.Controls.Add(this.centerLayout, 0, 0);
            this.contentLayout.Controls.Add(this.grpJog, 1, 0);
            this.contentLayout.Controls.Add(this.rightLayout, 2, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(8);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1678, 870);
            this.contentLayout.TabIndex = 1;
            // 
            // centerLayout
            // 
            this.centerLayout.ColumnCount = 2;
            this.centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.centerLayout.Controls.Add(this.grpVision, 0, 0);
            this.centerLayout.Controls.Add(this.grpManual, 0, 1);
            this.centerLayout.Controls.Add(this.leftLayout, 1, 0);
            this.centerLayout.SetRowSpan(this.leftLayout, 2);
            this.centerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centerLayout.Location = new System.Drawing.Point(8, 8);
            this.centerLayout.Margin = new System.Windows.Forms.Padding(0);
            this.centerLayout.Name = "centerLayout";
            this.centerLayout.RowCount = 2;
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 56F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.centerLayout.Size = new System.Drawing.Size(683, 854);
            this.centerLayout.TabIndex = 0;
            // 
            // grpVision
            // 
            this.grpVision.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpVision.Controls.Add(this.tabVision);
            this.grpVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpVision.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpVision.Location = new System.Drawing.Point(4, 4);
            this.grpVision.Margin = new System.Windows.Forms.Padding(4);
            this.grpVision.Name = "grpVision";
            this.grpVision.Padding = new System.Windows.Forms.Padding(3, 2, 3, 3);
            this.grpVision.Size = new System.Drawing.Size(675, 470);
            this.grpVision.TabIndex = 0;
            this.grpVision.TabStop = false;
            this.grpVision.Text = "VISION";
            // 
            // tabVision
            // 
            this.tabVision.Controls.Add(this.tabBottom);
            this.tabVision.Controls.Add(this.tabSide);
            this.tabVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabVision.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.tabVision.Location = new System.Drawing.Point(3, 20);
            this.tabVision.Name = "tabVision";
            this.tabVision.SelectedIndex = 0;
            this.tabVision.Size = new System.Drawing.Size(669, 447);
            this.tabVision.TabIndex = 0;
            // 
            // tabBottom
            // 
            this.tabBottom.BackColor = System.Drawing.Color.Black;
            this.tabBottom.Controls.Add(this.visionPanel);
            this.tabBottom.Location = new System.Drawing.Point(4, 24);
            this.tabBottom.Name = "tabBottom";
            this.tabBottom.Padding = new System.Windows.Forms.Padding(3);
            this.tabBottom.Size = new System.Drawing.Size(661, 419);
            this.tabBottom.TabIndex = 0;
            this.tabBottom.Text = "BOTTOM";
            // 
            // visionPanel
            // 
            this.visionPanel.BackColor = System.Drawing.Color.Black;
            this.visionPanel.Controls.Add(this.lblVisionInfo);
            this.visionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.visionPanel.Location = new System.Drawing.Point(3, 3);
            this.visionPanel.Name = "visionPanel";
            this.visionPanel.Size = new System.Drawing.Size(655, 413);
            this.visionPanel.TabIndex = 0;
            // 
            // lblVisionInfo
            // 
            this.lblVisionInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionInfo.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            this.lblVisionInfo.ForeColor = System.Drawing.Color.Lime;
            this.lblVisionInfo.Location = new System.Drawing.Point(0, 0);
            this.lblVisionInfo.Name = "lblVisionInfo";
            this.lblVisionInfo.Padding = new System.Windows.Forms.Padding(14);
            this.lblVisionInfo.Size = new System.Drawing.Size(655, 413);
            this.lblVisionInfo.TabIndex = 0;
            this.lblVisionInfo.Text = "VISION VIEW";
            // 
            // tabSide
            // 
            this.tabSide.BackColor = System.Drawing.Color.Black;
            this.tabSide.Controls.Add(this.sideLayout);
            this.tabSide.Location = new System.Drawing.Point(4, 24);
            this.tabSide.Name = "tabSide";
            this.tabSide.Padding = new System.Windows.Forms.Padding(3);
            this.tabSide.Size = new System.Drawing.Size(661, 419);
            this.tabSide.TabIndex = 1;
            this.tabSide.Text = "SIDE";
            // 
            // sideLayout
            // 
            this.sideLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.sideLayout.ColumnCount = 1;
            this.sideLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sideLayout.Controls.Add(this.lblVisionInfo2, 0, 0);
            this.sideLayout.Controls.Add(this.lblVisionInfo3, 0, 1);
            this.sideLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sideLayout.Location = new System.Drawing.Point(3, 3);
            this.sideLayout.Name = "sideLayout";
            this.sideLayout.RowCount = 2;
            this.sideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sideLayout.Size = new System.Drawing.Size(655, 413);
            this.sideLayout.TabIndex = 0;
            // 
            // lblVisionInfo2
            // 
            this.lblVisionInfo2.BackColor = System.Drawing.Color.Black;
            this.lblVisionInfo2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionInfo2.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            this.lblVisionInfo2.ForeColor = System.Drawing.Color.Lime;
            this.lblVisionInfo2.Location = new System.Drawing.Point(0, 0);
            this.lblVisionInfo2.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.lblVisionInfo2.Name = "lblVisionInfo2";
            this.lblVisionInfo2.Padding = new System.Windows.Forms.Padding(14);
            this.lblVisionInfo2.Size = new System.Drawing.Size(655, 205);
            this.lblVisionInfo2.TabIndex = 0;
            this.lblVisionInfo2.Text = "SIDE VISION 1";
            // 
            // lblVisionInfo3
            // 
            this.lblVisionInfo3.BackColor = System.Drawing.Color.Black;
            this.lblVisionInfo3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionInfo3.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            this.lblVisionInfo3.ForeColor = System.Drawing.Color.Lime;
            this.lblVisionInfo3.Location = new System.Drawing.Point(0, 207);
            this.lblVisionInfo3.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.lblVisionInfo3.Name = "lblVisionInfo3";
            this.lblVisionInfo3.Padding = new System.Windows.Forms.Padding(14);
            this.lblVisionInfo3.Size = new System.Drawing.Size(655, 206);
            this.lblVisionInfo3.TabIndex = 1;
            this.lblVisionInfo3.Text = "SIDE VISION 2";
            // 
            // grpManual
            // 
            this.grpManual.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpManual.Controls.Add(this.manualPanel);
            this.grpManual.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpManual.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpManual.Location = new System.Drawing.Point(4, 482);
            this.grpManual.Margin = new System.Windows.Forms.Padding(4);
            this.grpManual.Name = "grpManual";
            this.grpManual.Padding = new System.Windows.Forms.Padding(3, 2, 3, 3);
            this.grpManual.Size = new System.Drawing.Size(675, 368);
            this.grpManual.TabIndex = 1;
            this.grpManual.TabStop = false;
            this.grpManual.Text = "MANUAL ACTION";
            // 
            // manualPanel
            // 
            this.manualPanel.AutoScroll = true;
            this.manualPanel.Controls.Add(this.manualLayout);
            this.manualPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.manualPanel.Location = new System.Drawing.Point(3, 20);
            this.manualPanel.Name = "manualPanel";
            this.manualPanel.Size = new System.Drawing.Size(669, 345);
            this.manualPanel.TabIndex = 0;
            // 
            // manualLayout
            // 
            this.manualLayout.AutoSize = true;
            this.manualLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.manualLayout.ColumnCount = 2;
            this.manualLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.manualLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.manualLayout.Controls.Add(this.btnAvoidPosition, 0, 0);
            this.manualLayout.Controls.Add(this.btnPickPosition, 1, 0);
            this.manualLayout.Controls.Add(this.btnBottomPosition, 0, 1);
            this.manualLayout.Controls.Add(this.btnSidePosition, 1, 1);
            this.manualLayout.Controls.Add(this.btnPlacePosition, 0, 2);
            this.manualLayout.Controls.Add(this.btnDiePickPosition, 1, 2);
            this.manualLayout.Controls.Add(this.btnDieBottomPosition, 0, 3);
            this.manualLayout.Controls.Add(this.btnDieSidePosition, 1, 3);
            this.manualLayout.Controls.Add(this.btnDiePlacePosition, 0, 4);
            this.manualLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.manualLayout.Location = new System.Drawing.Point(0, 0);
            this.manualLayout.Name = "manualLayout";
            this.manualLayout.Padding = new System.Windows.Forms.Padding(3);
            this.manualLayout.RowCount = 5;
            this.manualLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.manualLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.manualLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.manualLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.manualLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.manualLayout.Size = new System.Drawing.Size(669, 231);
            this.manualLayout.TabIndex = 0;
            //
            // btnAvoidPosition
            //
            this.btnAvoidPosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAvoidPosition.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAvoidPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAvoidPosition.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnAvoidPosition.ForeColor = System.Drawing.Color.White;
            this.btnAvoidPosition.Margin = new System.Windows.Forms.Padding(2);
            this.btnAvoidPosition.Name = "btnAvoidPosition";
            this.btnAvoidPosition.TabIndex = 0;
            this.btnAvoidPosition.Text = "AVOID POSITION";
            this.btnAvoidPosition.Click += new System.EventHandler(this.btnAvoidPosition_Click);
            //
            // btnPickPosition
            //
            this.btnPickPosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPickPosition.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPickPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPickPosition.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnPickPosition.ForeColor = System.Drawing.Color.White;
            this.btnPickPosition.Margin = new System.Windows.Forms.Padding(2);
            this.btnPickPosition.Name = "btnPickPosition";
            this.btnPickPosition.TabIndex = 1;
            this.btnPickPosition.Text = "PICK POSITION";
            this.btnPickPosition.Click += new System.EventHandler(this.btnPickPosition_Click);
            //
            // btnBottomPosition
            //
            this.btnBottomPosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnBottomPosition.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBottomPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBottomPosition.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnBottomPosition.ForeColor = System.Drawing.Color.White;
            this.btnBottomPosition.Margin = new System.Windows.Forms.Padding(2);
            this.btnBottomPosition.Name = "btnBottomPosition";
            this.btnBottomPosition.TabIndex = 2;
            this.btnBottomPosition.Text = "BOTTOM POSITION";
            this.btnBottomPosition.Click += new System.EventHandler(this.btnBottomPosition_Click);
            //
            // btnSidePosition
            //
            this.btnSidePosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSidePosition.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSidePosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSidePosition.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnSidePosition.ForeColor = System.Drawing.Color.White;
            this.btnSidePosition.Margin = new System.Windows.Forms.Padding(2);
            this.btnSidePosition.Name = "btnSidePosition";
            this.btnSidePosition.TabIndex = 3;
            this.btnSidePosition.Text = "SIDE POSITION";
            this.btnSidePosition.Click += new System.EventHandler(this.btnSidePosition_Click);
            //
            // btnPlacePosition
            //
            this.btnPlacePosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPlacePosition.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlacePosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlacePosition.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnPlacePosition.ForeColor = System.Drawing.Color.White;
            this.btnPlacePosition.Margin = new System.Windows.Forms.Padding(2);
            this.btnPlacePosition.Name = "btnPlacePosition";
            this.btnPlacePosition.TabIndex = 4;
            this.btnPlacePosition.Text = "PLACE POSITION";
            this.btnPlacePosition.Click += new System.EventHandler(this.btnPlacePosition_Click);
            //
            // btnDiePickPosition
            //
            this.btnDiePickPosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnDiePickPosition.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDiePickPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDiePickPosition.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnDiePickPosition.ForeColor = System.Drawing.Color.White;
            this.btnDiePickPosition.Margin = new System.Windows.Forms.Padding(2);
            this.btnDiePickPosition.Name = "btnDiePickPosition";
            this.btnDiePickPosition.TabIndex = 5;
            this.btnDiePickPosition.Text = "DIE PICK POSITION";
            this.btnDiePickPosition.Click += new System.EventHandler(this.btnDiePickPosition_Click);
            //
            // btnDieBottomPosition
            //
            this.btnDieBottomPosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnDieBottomPosition.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDieBottomPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDieBottomPosition.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnDieBottomPosition.ForeColor = System.Drawing.Color.White;
            this.btnDieBottomPosition.Margin = new System.Windows.Forms.Padding(2);
            this.btnDieBottomPosition.Name = "btnDieBottomPosition";
            this.btnDieBottomPosition.TabIndex = 6;
            this.btnDieBottomPosition.Text = "DIE BOTTOM POSITION";
            this.btnDieBottomPosition.Click += new System.EventHandler(this.btnDieBottomPosition_Click);
            //
            // btnDieSidePosition
            //
            this.btnDieSidePosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnDieSidePosition.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDieSidePosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDieSidePosition.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnDieSidePosition.ForeColor = System.Drawing.Color.White;
            this.btnDieSidePosition.Margin = new System.Windows.Forms.Padding(2);
            this.btnDieSidePosition.Name = "btnDieSidePosition";
            this.btnDieSidePosition.TabIndex = 7;
            this.btnDieSidePosition.Text = "DIE SIDE POSITION";
            this.btnDieSidePosition.Click += new System.EventHandler(this.btnDieSidePosition_Click);
            //
            // btnDiePlacePosition
            //
            this.btnDiePlacePosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnDiePlacePosition.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDiePlacePosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDiePlacePosition.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.btnDiePlacePosition.ForeColor = System.Drawing.Color.White;
            this.btnDiePlacePosition.Margin = new System.Windows.Forms.Padding(2);
            this.btnDiePlacePosition.Name = "btnDiePlacePosition";
            this.btnDiePlacePosition.TabIndex = 8;
            this.btnDiePlacePosition.Text = "DIE PLACE POSITION";
            this.btnDiePlacePosition.Click += new System.EventHandler(this.btnDiePlacePosition_Click);
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 1;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Controls.Add(this.grpOptions, 0, 0);
            this.leftLayout.Controls.Add(this.grpWait, 0, 1);
            this.leftLayout.Controls.Add(this.grpIo, 0, 2);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(691, 8);
            this.leftLayout.Margin = new System.Windows.Forms.Padding(0);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 3;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 478F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 153F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Size = new System.Drawing.Size(457, 854);
            this.leftLayout.TabIndex = 1;
            // 
            // grpOptions
            // 
            this.grpOptions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpOptions.Controls.Add(this.optionParameterGrid);
            this.grpOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpOptions.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpOptions.Location = new System.Drawing.Point(4, 4);
            this.grpOptions.Margin = new System.Windows.Forms.Padding(4);
            this.grpOptions.Name = "grpOptions";
            this.grpOptions.Padding = new System.Windows.Forms.Padding(3, 2, 3, 3);
            this.grpOptions.Size = new System.Drawing.Size(449, 470);
            this.grpOptions.TabIndex = 0;
            this.grpOptions.TabStop = false;
            this.grpOptions.Text = "OPTION";
            // 
            // optionParameterGrid
            // 
            this.optionParameterGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.optionParameterGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.optionParameterGrid.Location = new System.Drawing.Point(3, 20);
            this.optionParameterGrid.Margin = new System.Windows.Forms.Padding(0);
            this.optionParameterGrid.Name = "optionParameterGrid";
            this.optionParameterGrid.Size = new System.Drawing.Size(443, 447);
            this.optionParameterGrid.TabIndex = 0;
            // 
            // grpWait
            // 
            this.grpWait.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpWait.Controls.Add(this.waitParameterGrid);
            this.grpWait.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpWait.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpWait.Location = new System.Drawing.Point(4, 482);
            this.grpWait.Margin = new System.Windows.Forms.Padding(4);
            this.grpWait.Name = "grpWait";
            this.grpWait.Padding = new System.Windows.Forms.Padding(3, 2, 3, 3);
            this.grpWait.Size = new System.Drawing.Size(449, 145);
            this.grpWait.TabIndex = 1;
            this.grpWait.TabStop = false;
            this.grpWait.Text = "WAIT TIME";
            // 
            // waitParameterGrid
            // 
            this.waitParameterGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.waitParameterGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waitParameterGrid.Location = new System.Drawing.Point(3, 20);
            this.waitParameterGrid.Margin = new System.Windows.Forms.Padding(0);
            this.waitParameterGrid.Name = "waitParameterGrid";
            this.waitParameterGrid.Size = new System.Drawing.Size(443, 122);
            this.waitParameterGrid.TabIndex = 0;
            // 
            // grpIo
            // 
            this.grpIo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpIo.Controls.Add(this.ioCylinderPanel);
            this.grpIo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpIo.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpIo.Location = new System.Drawing.Point(4, 635);
            this.grpIo.Margin = new System.Windows.Forms.Padding(4);
            this.grpIo.Name = "grpIo";
            this.grpIo.Padding = new System.Windows.Forms.Padding(3, 2, 3, 3);
            this.grpIo.Size = new System.Drawing.Size(449, 215);
            this.grpIo.TabIndex = 2;
            this.grpIo.TabStop = false;
            this.grpIo.Text = "CYLINDER && I/O";
            // 
            // ioCylinderPanel
            // 
            this.ioCylinderPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(205)))), ((int)(((byte)(205)))), ((int)(((byte)(205)))));
            this.ioCylinderPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioCylinderPanel.Location = new System.Drawing.Point(3, 20);
            this.ioCylinderPanel.Margin = new System.Windows.Forms.Padding(0);
            this.ioCylinderPanel.Name = "ioCylinderPanel";
            this.ioCylinderPanel.Size = new System.Drawing.Size(443, 192);
            this.ioCylinderPanel.TabIndex = 0;
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.grpSpeed, 0, 0);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(1148, 8);
            this.rightLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 1;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(428, 854);
            this.rightLayout.TabIndex = 2;
            // 
            // grpJog
            // 
            this.grpJog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpJog.Controls.Add(this.jogLayout);
            this.grpJog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpJog.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpJog.Location = new System.Drawing.Point(4, 4);
            this.grpJog.Margin = new System.Windows.Forms.Padding(4);
            this.grpJog.Name = "grpJog";
            this.grpJog.Padding = new System.Windows.Forms.Padding(3, 2, 3, 3);
            this.grpJog.Size = new System.Drawing.Size(420, 846);
            this.grpJog.TabIndex = 0;
            this.grpJog.TabStop = false;
            this.grpJog.Text = "JOG OPERATION";
            // 
            // jogLayout
            // 
            this.jogLayout.ColumnCount = 1;
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogLayout.Controls.Add(this.jogPositionListControl, 0, 0);
            this.jogLayout.Controls.Add(this.jogAxisMoveControl, 0, 1);
            this.jogLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogLayout.Location = new System.Drawing.Point(3, 20);
            this.jogLayout.Name = "jogLayout";
            this.jogLayout.RowCount = 2;
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 136F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogLayout.Size = new System.Drawing.Size(414, 823);
            this.jogLayout.TabIndex = 0;
            // 
            // jogPositionListControl
            // 
            this.jogPositionListControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogPositionListControl.Location = new System.Drawing.Point(3, 3);
            this.jogPositionListControl.Name = "jogPositionListControl";
            this.jogPositionListControl.Size = new System.Drawing.Size(408, 130);
            this.jogPositionListControl.TabIndex = 0;
            this.jogPositionListControl.WrapColumnsWhenMany = true;
            // 
            // jogAxisMoveControl
            // 
            this.jogAxisMoveControl.AxisColumnsPerRow = 0;
            this.jogAxisMoveControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.jogAxisMoveControl.ButtonAreaMaxHeight = 92;
            this.jogAxisMoveControl.ButtonAreaMaxWidth = 132;
            this.jogAxisMoveControl.ButtonAreaMinHeight = 72;
            this.jogAxisMoveControl.ButtonAreaMinWidth = 112;
            this.jogAxisMoveControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogAxisMoveControl.LayoutMode = QMC.CDT_320.Ui.Controls.JogAxisMoveLayoutMode.AxisColumns;
            this.jogAxisMoveControl.Location = new System.Drawing.Point(3, 139);
            this.jogAxisMoveControl.Name = "jogAxisMoveControl";
            this.jogAxisMoveControl.ShowCurrentSpeedMode = true;
            this.jogAxisMoveControl.Size = new System.Drawing.Size(408, 681);
            this.jogAxisMoveControl.SpeedControl = null;
            this.jogAxisMoveControl.TabIndex = 1;
            // 
            // grpSpeed
            // 
            this.grpSpeed.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpSpeed.Controls.Add(this.jogSpeedControl);
            this.grpSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSpeed.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSpeed.Location = new System.Drawing.Point(1580, 12);
            this.grpSpeed.Margin = new System.Windows.Forms.Padding(4);
            this.grpSpeed.Name = "grpSpeed";
            this.grpSpeed.Padding = new System.Windows.Forms.Padding(3, 2, 3, 3);
            this.grpSpeed.Size = new System.Drawing.Size(86, 846);
            this.grpSpeed.TabIndex = 3;
            this.grpSpeed.TabStop = false;
            this.grpSpeed.Text = "SPEED";
            // 
            // jogSpeedControl
            // 
            this.jogSpeedControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.jogSpeedControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogSpeedControl.Location = new System.Drawing.Point(3, 20);
            this.jogSpeedControl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.jogSpeedControl.Name = "jogSpeedControl";
            this.jogSpeedControl.Size = new System.Drawing.Size(80, 823);
            this.jogSpeedControl.SpeedPercent = 50;
            this.jogSpeedControl.TabIndex = 0;
            // 
            // FrontPickerRecipePage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.rootLayout);
            this.Name = "RearPickerRecipePage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.centerLayout.ResumeLayout(false);
            this.grpVision.ResumeLayout(false);
            this.tabVision.ResumeLayout(false);
            this.tabBottom.ResumeLayout(false);
            this.visionPanel.ResumeLayout(false);
            this.tabSide.ResumeLayout(false);
            this.sideLayout.ResumeLayout(false);
            this.grpManual.ResumeLayout(false);
            this.manualPanel.ResumeLayout(false);
            this.manualPanel.PerformLayout();
            this.manualLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.grpOptions.ResumeLayout(false);
            this.grpWait.ResumeLayout(false);
            this.grpIo.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.grpJog.ResumeLayout(false);
            this.jogLayout.ResumeLayout(false);
            this.grpSpeed.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
