using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class RearPickerPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private TableLayoutPanel topLayout;
        private GroupBox grpState;
        private GroupBox grpCounters;
        private Label lblColletCleanTitle;
        private Label lblColletCleanValue;
        private GroupBox grpInfo;
        private GroupBox grpSensor;
        private TableLayoutPanel stateLayout;
        private TableLayoutPanel counterLayout;
        private TableLayoutPanel infoLayout;
        private TableLayoutPanel sensorLayout;
        private TableLayoutPanel actionPanel;
        private Button btnCountClear;
        private Label lblHeadZoneTitle;
        private Label lblHeadZoneValue;
        private Label lblHead1Title;
        private Label lblHead1Value;
        private Label lblHead2Title;
        private Label lblHead2Value;
        private Label lblHead3Title;
        private Label lblHead3Value;
        private Label lblHead4Title;
        private Label lblHead4Value;
        private Label lblColletChangeTitle;
        private Label lblColletChangeValue;
        private Label lblAutoPosTitle;
        private Label lblAutoPosValue;
        private Label lblColletCleaningTitle;
        private Label lblColletCleaningValue;
        private Label lblColletCheckTitle;
        private Label lblColletCheckValue;
        private Label lblPickFailTitle;
        private Label lblPickFailValue;
        private Label lblPlaceFailTitle;
        private Label lblPlaceFailValue;
        private Label lblCollet1UseTitle;
        private Label lblCollet1UseValue;
        private Label lblCollet2UseTitle;
        private Label lblCollet2UseValue;
        private Label lblCollet3UseTitle;
        private Label lblCollet3UseValue;
        private Label lblCollet4UseTitle;
        private Label lblCollet4UseValue;
        private Label lblProcessDetailTitle;
        private Label lblProcessDetailValue;
        private TableLayoutPanel headVacuum1Panel;
        private IndicatorDot dotHeadVacuum1;
        private Label lblHeadVacuum1;
        private TableLayoutPanel headVacuum2Panel;
        private IndicatorDot dotHeadVacuum2;
        private Label lblHeadVacuum2;
        private TableLayoutPanel headVacuum3Panel;
        private IndicatorDot dotHeadVacuum3;
        private Label lblHeadVacuum3;
        private TableLayoutPanel headVacuum4Panel;
        private IndicatorDot dotHeadVacuum4;
        private Label lblHeadVacuum4;
        private TableLayoutPanel headBlow1Panel;
        private IndicatorDot dotHeadBlow1;
        private Label lblHeadBlow1;
        private TableLayoutPanel headBlow2Panel;
        private IndicatorDot dotHeadBlow2;
        private Label lblHeadBlow2;
        private TableLayoutPanel headBlow3Panel;
        private IndicatorDot dotHeadBlow3;
        private Label lblHeadBlow3;
        private TableLayoutPanel headBlow4Panel;
        private IndicatorDot dotHeadBlow4;
        private Label lblHeadBlow4;
        private TableLayoutPanel axis1Panel;
        private Label lblAxis1Title;
        private Label lblAxis1Value;
        private TableLayoutPanel axis2Panel;
        private Label lblAxis2Title;
        private Label lblAxis2Value;
        private TableLayoutPanel axis3Panel;
        private Label lblAxis3Title;
        private Label lblAxis3Value;
        private TableLayoutPanel axis4Panel;
        private Label lblAxis4Title;
        private Label lblAxis4Value;
        private TableLayoutPanel axis5Panel;
        private Label lblAxis5Title;
        private Label lblAxis5Value;
        private TableLayoutPanel axis6Panel;
        private Label lblAxis6Title;
        private Label lblAxis6Value;
        private TableLayoutPanel axis7Panel;
        private Label lblAxis7Title;
        private Label lblAxis7Value;
        private TableLayoutPanel axis8Panel;
        private Label lblAxis8Title;
        private Label lblAxis8Value;
        private TableLayoutPanel axis9Panel;
        private Label lblAxis9Title;
        private Label lblAxis9Value;
        private TableLayoutPanel axis10Panel;
        private Label lblAxis10Title;
        private Label lblAxis10Value;
        private TableLayoutPanel materialPanel;
        private TableLayoutPanel materialHeaderLayout;
        private Label lblHeadDieTitle;
        private System.Windows.Forms.RadioButton btnHead1Select;
        private System.Windows.Forms.RadioButton btnHead2Select;
        private System.Windows.Forms.RadioButton btnHead3Select;
        private System.Windows.Forms.RadioButton btnHead4Select;
        private QMC.CDT_320.Ui.Controls.MaterialDetailView headDieDetailView;
        private ActionButton btnInput;
        private ActionButton btnInspect;
        private ActionButton btnBottom;
        private ActionButton btnSide;
        private ActionButton btnOutput;
        private ActionButton btnPickUpTest;
        private ActionButton btnAjinLineMapTest;
        private ActionButton btnAjinLineMoveTest;
        private ActionButton btnVisionBottomInspect;
        private ActionButton btnVisionFrontSide;
        private ActionButton btnVisionRearSide;
        private ActionButton btnStop;
        private TableLayoutPanel actionBar;
        private TableLayoutPanel actionRightPanel;
        private GroupBox grpAction;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.topLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpState = new System.Windows.Forms.GroupBox();
            this.stateLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeadZoneTitle = new System.Windows.Forms.Label();
            this.lblHeadZoneValue = new System.Windows.Forms.Label();
            this.lblHead1Title = new System.Windows.Forms.Label();
            this.lblHead1Value = new System.Windows.Forms.Label();
            this.lblHead2Title = new System.Windows.Forms.Label();
            this.lblHead2Value = new System.Windows.Forms.Label();
            this.lblHead3Title = new System.Windows.Forms.Label();
            this.lblHead3Value = new System.Windows.Forms.Label();
            this.lblHead4Title = new System.Windows.Forms.Label();
            this.lblHead4Value = new System.Windows.Forms.Label();
            this.lblColletChangeTitle = new System.Windows.Forms.Label();
            this.lblColletChangeValue = new System.Windows.Forms.Label();
            this.lblAutoPosTitle = new System.Windows.Forms.Label();
            this.lblAutoPosValue = new System.Windows.Forms.Label();
            this.lblColletCleaningTitle = new System.Windows.Forms.Label();
            this.lblColletCleaningValue = new System.Windows.Forms.Label();
            this.lblColletCheckTitle = new System.Windows.Forms.Label();
            this.lblColletCheckValue = new System.Windows.Forms.Label();
            this.grpCounters = new System.Windows.Forms.GroupBox();
            this.lblColletCleanTitle = new System.Windows.Forms.Label();
            this.lblColletCleanValue = new System.Windows.Forms.Label();
            this.counterLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblPickFailTitle = new System.Windows.Forms.Label();
            this.lblPickFailValue = new System.Windows.Forms.Label();
            this.lblPlaceFailTitle = new System.Windows.Forms.Label();
            this.lblPlaceFailValue = new System.Windows.Forms.Label();
            this.lblCollet1UseTitle = new System.Windows.Forms.Label();
            this.lblCollet1UseValue = new System.Windows.Forms.Label();
            this.lblCollet2UseTitle = new System.Windows.Forms.Label();
            this.lblCollet2UseValue = new System.Windows.Forms.Label();
            this.lblCollet3UseTitle = new System.Windows.Forms.Label();
            this.lblCollet3UseValue = new System.Windows.Forms.Label();
            this.lblCollet4UseTitle = new System.Windows.Forms.Label();
            this.lblCollet4UseValue = new System.Windows.Forms.Label();
            this.btnCountClear = new System.Windows.Forms.Button();
            this.grpInfo = new System.Windows.Forms.GroupBox();
            this.infoLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblProcessDetailTitle = new System.Windows.Forms.Label();
            this.lblProcessDetailValue = new System.Windows.Forms.Label();
            this.axis1Panel = new System.Windows.Forms.TableLayoutPanel();
            this.lblAxis1Title = new System.Windows.Forms.Label();
            this.lblAxis1Value = new System.Windows.Forms.Label();
            this.axis2Panel = new System.Windows.Forms.TableLayoutPanel();
            this.lblAxis2Title = new System.Windows.Forms.Label();
            this.lblAxis2Value = new System.Windows.Forms.Label();
            this.axis3Panel = new System.Windows.Forms.TableLayoutPanel();
            this.lblAxis3Title = new System.Windows.Forms.Label();
            this.lblAxis3Value = new System.Windows.Forms.Label();
            this.axis4Panel = new System.Windows.Forms.TableLayoutPanel();
            this.lblAxis4Title = new System.Windows.Forms.Label();
            this.lblAxis4Value = new System.Windows.Forms.Label();
            this.axis5Panel = new System.Windows.Forms.TableLayoutPanel();
            this.lblAxis5Title = new System.Windows.Forms.Label();
            this.lblAxis5Value = new System.Windows.Forms.Label();
            this.axis6Panel = new System.Windows.Forms.TableLayoutPanel();
            this.lblAxis6Title = new System.Windows.Forms.Label();
            this.lblAxis6Value = new System.Windows.Forms.Label();
            this.axis7Panel = new System.Windows.Forms.TableLayoutPanel();
            this.lblAxis7Title = new System.Windows.Forms.Label();
            this.lblAxis7Value = new System.Windows.Forms.Label();
            this.axis8Panel = new System.Windows.Forms.TableLayoutPanel();
            this.lblAxis8Title = new System.Windows.Forms.Label();
            this.lblAxis8Value = new System.Windows.Forms.Label();
            this.axis9Panel = new System.Windows.Forms.TableLayoutPanel();
            this.lblAxis9Title = new System.Windows.Forms.Label();
            this.lblAxis9Value = new System.Windows.Forms.Label();
            this.axis10Panel = new System.Windows.Forms.TableLayoutPanel();
            this.lblAxis10Title = new System.Windows.Forms.Label();
            this.lblAxis10Value = new System.Windows.Forms.Label();
            this.grpSensor = new System.Windows.Forms.GroupBox();
            this.sensorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.headVacuum1Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadVacuum1 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadVacuum1 = new System.Windows.Forms.Label();
            this.headVacuum2Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadVacuum2 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadVacuum2 = new System.Windows.Forms.Label();
            this.headVacuum3Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadVacuum3 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadVacuum3 = new System.Windows.Forms.Label();
            this.headVacuum4Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadVacuum4 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadVacuum4 = new System.Windows.Forms.Label();
            this.headBlow1Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadBlow1 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadBlow1 = new System.Windows.Forms.Label();
            this.headBlow2Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadBlow2 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadBlow2 = new System.Windows.Forms.Label();
            this.headBlow3Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadBlow3 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadBlow3 = new System.Windows.Forms.Label();
            this.headBlow4Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadBlow4 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadBlow4 = new System.Windows.Forms.Label();
            this.materialPanel = new System.Windows.Forms.TableLayoutPanel();
            this.materialHeaderLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeadDieTitle = new System.Windows.Forms.Label();
            this.btnHead1Select = new System.Windows.Forms.RadioButton();
            this.btnHead2Select = new System.Windows.Forms.RadioButton();
            this.btnHead3Select = new System.Windows.Forms.RadioButton();
            this.btnHead4Select = new System.Windows.Forms.RadioButton();
            this.headDieDetailView = new QMC.CDT_320.Ui.Controls.MaterialDetailView();
            this.grpAction = new System.Windows.Forms.GroupBox();
            this.actionBar = new System.Windows.Forms.TableLayoutPanel();
            this.actionPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnInput = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnInspect = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnBottom = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSide = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnOutput = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnPickUpTest = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAjinLineMapTest = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAjinLineMoveTest = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.actionRightPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnVisionBottomInspect = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnVisionFrontSide = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnVisionRearSide = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.topLayout.SuspendLayout();
            this.grpState.SuspendLayout();
            this.stateLayout.SuspendLayout();
            this.grpCounters.SuspendLayout();
            this.counterLayout.SuspendLayout();
            this.grpInfo.SuspendLayout();
            this.infoLayout.SuspendLayout();
            this.axis1Panel.SuspendLayout();
            this.axis2Panel.SuspendLayout();
            this.axis3Panel.SuspendLayout();
            this.axis4Panel.SuspendLayout();
            this.axis5Panel.SuspendLayout();
            this.axis6Panel.SuspendLayout();
            this.axis7Panel.SuspendLayout();
            this.axis8Panel.SuspendLayout();
            this.axis9Panel.SuspendLayout();
            this.axis10Panel.SuspendLayout();
            this.grpSensor.SuspendLayout();
            this.sensorLayout.SuspendLayout();
            this.headVacuum1Panel.SuspendLayout();
            this.headVacuum2Panel.SuspendLayout();
            this.headVacuum3Panel.SuspendLayout();
            this.headVacuum4Panel.SuspendLayout();
            this.headBlow1Panel.SuspendLayout();
            this.headBlow2Panel.SuspendLayout();
            this.headBlow3Panel.SuspendLayout();
            this.headBlow4Panel.SuspendLayout();
            this.materialPanel.SuspendLayout();
            this.materialHeaderLayout.SuspendLayout();
            this.grpAction.SuspendLayout();
            this.actionBar.SuspendLayout();
            this.actionPanel.SuspendLayout();
            this.actionRightPanel.SuspendLayout();
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
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 800);
            this.rootLayout.TabIndex = 0;
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
            this.lblHeader.Tag = "i18n:wi.rearHead";
            this.lblHeader.Text = "REAR PICKER";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.BackColor = System.Drawing.Color.White;
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.Controls.Add(this.topLayout, 0, 0);
            this.contentLayout.Controls.Add(this.grpSensor, 0, 1);
            this.contentLayout.Controls.Add(this.materialPanel, 1, 0);
            this.contentLayout.Controls.Add(this.grpAction, 0, 2);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.RowCount = 3;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1678, 770);
            this.contentLayout.TabIndex = 1;
            // 
            // topLayout
            // 
            this.topLayout.ColumnCount = 3;
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.topLayout.Controls.Add(this.grpState, 0, 0);
            this.topLayout.Controls.Add(this.grpCounters, 1, 0);
            this.topLayout.Controls.Add(this.grpInfo, 2, 0);
            this.topLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topLayout.Location = new System.Drawing.Point(0, 0);
            this.topLayout.Margin = new System.Windows.Forms.Padding(0);
            this.topLayout.Name = "topLayout";
            this.topLayout.RowCount = 1;
            this.topLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topLayout.Size = new System.Drawing.Size(839, 300);
            this.topLayout.TabIndex = 0;
            // 
            // grpState
            // 
            this.grpState.BackColor = System.Drawing.Color.White;
            this.grpState.Controls.Add(this.stateLayout);
            this.grpState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpState.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpState.Location = new System.Drawing.Point(0, 0);
            this.grpState.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3);
            this.grpState.Name = "grpState";
            this.grpState.Size = new System.Drawing.Size(332, 297);
            this.grpState.TabIndex = 0;
            this.grpState.TabStop = false;
            this.grpState.Text = "WORK INFO";
            // 
            // stateLayout
            // 
            this.stateLayout.ColumnCount = 2;
            this.stateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 61.39706F));
            this.stateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38.60294F));
            this.stateLayout.Controls.Add(this.lblHeadZoneTitle, 0, 0);
            this.stateLayout.Controls.Add(this.lblHeadZoneValue, 1, 0);
            this.stateLayout.Controls.Add(this.lblHead1Title, 0, 1);
            this.stateLayout.Controls.Add(this.lblHead1Value, 1, 1);
            this.stateLayout.Controls.Add(this.lblHead2Title, 0, 2);
            this.stateLayout.Controls.Add(this.lblHead2Value, 1, 2);
            this.stateLayout.Controls.Add(this.lblHead3Title, 0, 3);
            this.stateLayout.Controls.Add(this.lblHead3Value, 1, 3);
            this.stateLayout.Controls.Add(this.lblHead4Title, 0, 4);
            this.stateLayout.Controls.Add(this.lblHead4Value, 1, 4);
            this.stateLayout.Controls.Add(this.lblColletChangeTitle, 0, 5);
            this.stateLayout.Controls.Add(this.lblColletChangeValue, 1, 5);
            this.stateLayout.Controls.Add(this.lblAutoPosTitle, 0, 6);
            this.stateLayout.Controls.Add(this.lblAutoPosValue, 1, 6);
            this.stateLayout.Controls.Add(this.lblColletCleaningTitle, 0, 7);
            this.stateLayout.Controls.Add(this.lblColletCleaningValue, 1, 7);
            this.stateLayout.Controls.Add(this.lblColletCheckTitle, 0, 8);
            this.stateLayout.Controls.Add(this.lblColletCheckValue, 1, 8);
            this.stateLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stateLayout.Location = new System.Drawing.Point(3, 23);
            this.stateLayout.Name = "stateLayout";
            this.stateLayout.Padding = new System.Windows.Forms.Padding(2, 8, 2, 2);
            this.stateLayout.RowCount = 10;
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 0.009999F));
            this.stateLayout.Size = new System.Drawing.Size(326, 271);
            this.stateLayout.TabIndex = 0;
            // 
            // lblHeadZoneTitle
            // 
            this.lblHeadZoneTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadZoneTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadZoneTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadZoneTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblHeadZoneTitle.Location = new System.Drawing.Point(5, 8);
            this.lblHeadZoneTitle.Name = "lblHeadZoneTitle";
            this.lblHeadZoneTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadZoneTitle.Size = new System.Drawing.Size(191, 28);
            this.lblHeadZoneTitle.TabIndex = 0;
            this.lblHeadZoneTitle.Text = "HEAD ZONE";
            this.lblHeadZoneTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblHeadZoneValue
            // 
            this.lblHeadZoneValue.BackColor = System.Drawing.Color.White;
            this.lblHeadZoneValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadZoneValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadZoneValue.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeadZoneValue.Location = new System.Drawing.Point(202, 8);
            this.lblHeadZoneValue.Name = "lblHeadZoneValue";
            this.lblHeadZoneValue.Size = new System.Drawing.Size(119, 28);
            this.lblHeadZoneValue.TabIndex = 1;
            this.lblHeadZoneValue.Text = "-";
            this.lblHeadZoneValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblHead1Title
            // 
            this.lblHead1Title.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHead1Title.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead1Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead1Title.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblHead1Title.Location = new System.Drawing.Point(5, 36);
            this.lblHead1Title.Name = "lblHead1Title";
            this.lblHead1Title.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHead1Title.Size = new System.Drawing.Size(191, 28);
            this.lblHead1Title.TabIndex = 0;
            this.lblHead1Title.Text = "HEAD #1";
            this.lblHead1Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblHead1Value
            // 
            this.lblHead1Value.BackColor = System.Drawing.Color.White;
            this.lblHead1Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead1Value.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblHead1Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead1Value.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblHead1Value.Location = new System.Drawing.Point(202, 36);
            this.lblHead1Value.Name = "lblHead1Value";
            this.lblHead1Value.Size = new System.Drawing.Size(119, 28);
            this.lblHead1Value.TabIndex = 1;
            this.lblHead1Value.Text = "-";
            this.lblHead1Value.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblHead1Value.Click += new System.EventHandler(this.lblHead1Value_Click);
            // 
            // lblHead2Title
            // 
            this.lblHead2Title.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHead2Title.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead2Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead2Title.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblHead2Title.Location = new System.Drawing.Point(5, 64);
            this.lblHead2Title.Name = "lblHead2Title";
            this.lblHead2Title.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHead2Title.Size = new System.Drawing.Size(191, 28);
            this.lblHead2Title.TabIndex = 2;
            this.lblHead2Title.Text = "HEAD #2";
            this.lblHead2Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblHead2Value
            // 
            this.lblHead2Value.BackColor = System.Drawing.Color.White;
            this.lblHead2Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead2Value.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblHead2Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead2Value.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblHead2Value.Location = new System.Drawing.Point(202, 64);
            this.lblHead2Value.Name = "lblHead2Value";
            this.lblHead2Value.Size = new System.Drawing.Size(119, 28);
            this.lblHead2Value.TabIndex = 3;
            this.lblHead2Value.Text = "-";
            this.lblHead2Value.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblHead2Value.Click += new System.EventHandler(this.lblHead2Value_Click);
            // 
            // lblHead3Title
            // 
            this.lblHead3Title.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHead3Title.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead3Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead3Title.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblHead3Title.Location = new System.Drawing.Point(5, 92);
            this.lblHead3Title.Name = "lblHead3Title";
            this.lblHead3Title.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHead3Title.Size = new System.Drawing.Size(191, 28);
            this.lblHead3Title.TabIndex = 4;
            this.lblHead3Title.Text = "HEAD #3";
            this.lblHead3Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblHead3Value
            // 
            this.lblHead3Value.BackColor = System.Drawing.Color.White;
            this.lblHead3Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead3Value.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblHead3Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead3Value.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblHead3Value.Location = new System.Drawing.Point(202, 92);
            this.lblHead3Value.Name = "lblHead3Value";
            this.lblHead3Value.Size = new System.Drawing.Size(119, 28);
            this.lblHead3Value.TabIndex = 5;
            this.lblHead3Value.Text = "-";
            this.lblHead3Value.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblHead3Value.Click += new System.EventHandler(this.lblHead3Value_Click);
            // 
            // lblHead4Title
            // 
            this.lblHead4Title.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHead4Title.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead4Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead4Title.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblHead4Title.Location = new System.Drawing.Point(5, 120);
            this.lblHead4Title.Name = "lblHead4Title";
            this.lblHead4Title.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHead4Title.Size = new System.Drawing.Size(191, 28);
            this.lblHead4Title.TabIndex = 6;
            this.lblHead4Title.Text = "HEAD #4";
            this.lblHead4Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblHead4Value
            // 
            this.lblHead4Value.BackColor = System.Drawing.Color.White;
            this.lblHead4Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead4Value.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblHead4Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead4Value.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblHead4Value.Location = new System.Drawing.Point(202, 120);
            this.lblHead4Value.Name = "lblHead4Value";
            this.lblHead4Value.Size = new System.Drawing.Size(119, 28);
            this.lblHead4Value.TabIndex = 7;
            this.lblHead4Value.Text = "-";
            this.lblHead4Value.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblHead4Value.Click += new System.EventHandler(this.lblHead4Value_Click);
            // 
            // lblColletChangeTitle
            // 
            this.lblColletChangeTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblColletChangeTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletChangeTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletChangeTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblColletChangeTitle.Location = new System.Drawing.Point(5, 148);
            this.lblColletChangeTitle.Name = "lblColletChangeTitle";
            this.lblColletChangeTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblColletChangeTitle.Size = new System.Drawing.Size(191, 28);
            this.lblColletChangeTitle.TabIndex = 8;
            this.lblColletChangeTitle.Text = "COLLET CHANGE";
            this.lblColletChangeTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblColletChangeValue
            // 
            this.lblColletChangeValue.BackColor = System.Drawing.Color.White;
            this.lblColletChangeValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletChangeValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletChangeValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblColletChangeValue.Location = new System.Drawing.Point(202, 148);
            this.lblColletChangeValue.Name = "lblColletChangeValue";
            this.lblColletChangeValue.Size = new System.Drawing.Size(119, 28);
            this.lblColletChangeValue.TabIndex = 9;
            this.lblColletChangeValue.Text = "-";
            this.lblColletChangeValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblAutoPosTitle
            // 
            this.lblAutoPosTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblAutoPosTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAutoPosTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAutoPosTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblAutoPosTitle.Location = new System.Drawing.Point(5, 176);
            this.lblAutoPosTitle.Name = "lblAutoPosTitle";
            this.lblAutoPosTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblAutoPosTitle.Size = new System.Drawing.Size(191, 28);
            this.lblAutoPosTitle.TabIndex = 10;
            this.lblAutoPosTitle.Text = "AUTO POSITION";
            this.lblAutoPosTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAutoPosValue
            // 
            this.lblAutoPosValue.BackColor = System.Drawing.Color.White;
            this.lblAutoPosValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAutoPosValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAutoPosValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblAutoPosValue.Location = new System.Drawing.Point(202, 176);
            this.lblAutoPosValue.Name = "lblAutoPosValue";
            this.lblAutoPosValue.Size = new System.Drawing.Size(119, 28);
            this.lblAutoPosValue.TabIndex = 11;
            this.lblAutoPosValue.Text = "-";
            this.lblAutoPosValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblColletCleaningTitle
            // 
            this.lblColletCleaningTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblColletCleaningTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletCleaningTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletCleaningTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblColletCleaningTitle.Location = new System.Drawing.Point(5, 204);
            this.lblColletCleaningTitle.Name = "lblColletCleaningTitle";
            this.lblColletCleaningTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblColletCleaningTitle.Size = new System.Drawing.Size(191, 28);
            this.lblColletCleaningTitle.TabIndex = 12;
            this.lblColletCleaningTitle.Text = "COLLET CLEANING";
            this.lblColletCleaningTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblColletCleaningValue
            // 
            this.lblColletCleaningValue.BackColor = System.Drawing.Color.White;
            this.lblColletCleaningValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletCleaningValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletCleaningValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblColletCleaningValue.Location = new System.Drawing.Point(202, 204);
            this.lblColletCleaningValue.Name = "lblColletCleaningValue";
            this.lblColletCleaningValue.Size = new System.Drawing.Size(119, 28);
            this.lblColletCleaningValue.TabIndex = 13;
            this.lblColletCleaningValue.Text = "-";
            this.lblColletCleaningValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblColletCheckTitle
            // 
            this.lblColletCheckTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblColletCheckTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletCheckTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletCheckTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblColletCheckTitle.Location = new System.Drawing.Point(5, 232);
            this.lblColletCheckTitle.Name = "lblColletCheckTitle";
            this.lblColletCheckTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblColletCheckTitle.Size = new System.Drawing.Size(191, 28);
            this.lblColletCheckTitle.TabIndex = 14;
            this.lblColletCheckTitle.Text = "COLLET CHECK";
            this.lblColletCheckTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblColletCheckValue
            // 
            this.lblColletCheckValue.BackColor = System.Drawing.Color.White;
            this.lblColletCheckValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletCheckValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletCheckValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblColletCheckValue.Location = new System.Drawing.Point(202, 232);
            this.lblColletCheckValue.Name = "lblColletCheckValue";
            this.lblColletCheckValue.Size = new System.Drawing.Size(119, 28);
            this.lblColletCheckValue.TabIndex = 15;
            this.lblColletCheckValue.Text = "-";
            this.lblColletCheckValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // grpCounters
            // 
            this.grpCounters.BackColor = System.Drawing.Color.White;
            this.grpCounters.Controls.Add(this.counterLayout);
            this.grpCounters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCounters.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpCounters.Location = new System.Drawing.Point(338, 0);
            this.grpCounters.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.grpCounters.Name = "grpCounters";
            this.grpCounters.Size = new System.Drawing.Size(203, 297);
            this.grpCounters.TabIndex = 1;
            this.grpCounters.TabStop = false;
            this.grpCounters.Text = "COUNTER";
            // 
            // counterLayout
            // 
            this.counterLayout.ColumnCount = 2;
            this.counterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.counterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.counterLayout.Controls.Add(this.lblPickFailTitle, 0, 0);
            this.counterLayout.Controls.Add(this.lblPickFailValue, 1, 0);
            this.counterLayout.Controls.Add(this.lblPlaceFailTitle, 0, 1);
            this.counterLayout.Controls.Add(this.lblPlaceFailValue, 1, 1);
            this.counterLayout.Controls.Add(this.lblCollet1UseTitle, 0, 2);
            this.counterLayout.Controls.Add(this.lblCollet1UseValue, 1, 2);
            this.counterLayout.Controls.Add(this.lblCollet2UseTitle, 0, 3);
            this.counterLayout.Controls.Add(this.lblCollet2UseValue, 1, 3);
            this.counterLayout.Controls.Add(this.lblCollet3UseTitle, 0, 4);
            this.counterLayout.Controls.Add(this.lblCollet3UseValue, 1, 4);
            this.counterLayout.Controls.Add(this.lblCollet4UseTitle, 0, 5);
            this.counterLayout.Controls.Add(this.lblCollet4UseValue, 1, 5);
            this.counterLayout.Controls.Add(this.lblColletCleanTitle, 0, 6);
            this.counterLayout.Controls.Add(this.lblColletCleanValue, 1, 6);
            this.counterLayout.Controls.Add(this.btnCountClear, 0, 7);
            this.counterLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.counterLayout.Location = new System.Drawing.Point(3, 23);
            this.counterLayout.Name = "counterLayout";
            this.counterLayout.Padding = new System.Windows.Forms.Padding(2, 8, 2, 2);
            this.counterLayout.RowCount = 10;
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 0.009999002F));
            this.counterLayout.Size = new System.Drawing.Size(197, 271);
            this.counterLayout.TabIndex = 0;
            // 
            // lblColletCleanTitle
            //
            this.lblColletCleanTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblColletCleanTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletCleanTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletCleanTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblColletCleanTitle.Name = "lblColletCleanTitle";
            this.lblColletCleanTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblColletCleanTitle.Size = new System.Drawing.Size(112, 26);
            this.lblColletCleanTitle.TabIndex = 90;
            this.lblColletCleanTitle.Text = "LAST CLEAN";
            this.lblColletCleanTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblColletCleanValue
            //
            this.lblColletCleanValue.BackColor = System.Drawing.Color.White;
            this.lblColletCleanValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletCleanValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletCleanValue.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblColletCleanValue.Name = "lblColletCleanValue";
            this.lblColletCleanValue.Size = new System.Drawing.Size(75, 26);
            this.lblColletCleanValue.TabIndex = 91;
            this.lblColletCleanValue.Text = "-";
            this.lblColletCleanValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblPickFailTitle
            // 
            this.lblPickFailTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblPickFailTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPickFailTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickFailTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblPickFailTitle.Location = new System.Drawing.Point(5, 8);
            this.lblPickFailTitle.Name = "lblPickFailTitle";
            this.lblPickFailTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblPickFailTitle.Size = new System.Drawing.Size(109, 28);
            this.lblPickFailTitle.TabIndex = 0;
            this.lblPickFailTitle.Text = "PICK FAIL";
            this.lblPickFailTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPickFailValue
            // 
            this.lblPickFailValue.BackColor = System.Drawing.Color.White;
            this.lblPickFailValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPickFailValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickFailValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblPickFailValue.Location = new System.Drawing.Point(120, 8);
            this.lblPickFailValue.Name = "lblPickFailValue";
            this.lblPickFailValue.Size = new System.Drawing.Size(72, 28);
            this.lblPickFailValue.TabIndex = 1;
            this.lblPickFailValue.Text = "0 ea";
            this.lblPickFailValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPlaceFailTitle
            // 
            this.lblPlaceFailTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblPlaceFailTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPlaceFailTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceFailTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblPlaceFailTitle.Location = new System.Drawing.Point(5, 36);
            this.lblPlaceFailTitle.Name = "lblPlaceFailTitle";
            this.lblPlaceFailTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblPlaceFailTitle.Size = new System.Drawing.Size(109, 28);
            this.lblPlaceFailTitle.TabIndex = 2;
            this.lblPlaceFailTitle.Text = "PLACE FAIL";
            this.lblPlaceFailTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPlaceFailValue
            // 
            this.lblPlaceFailValue.BackColor = System.Drawing.Color.White;
            this.lblPlaceFailValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPlaceFailValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceFailValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblPlaceFailValue.Location = new System.Drawing.Point(120, 36);
            this.lblPlaceFailValue.Name = "lblPlaceFailValue";
            this.lblPlaceFailValue.Size = new System.Drawing.Size(72, 28);
            this.lblPlaceFailValue.TabIndex = 3;
            this.lblPlaceFailValue.Text = "0 ea";
            this.lblPlaceFailValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblCollet1UseTitle
            // 
            this.lblCollet1UseTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblCollet1UseTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet1UseTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet1UseTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCollet1UseTitle.Location = new System.Drawing.Point(5, 64);
            this.lblCollet1UseTitle.Name = "lblCollet1UseTitle";
            this.lblCollet1UseTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblCollet1UseTitle.Size = new System.Drawing.Size(109, 28);
            this.lblCollet1UseTitle.TabIndex = 4;
            this.lblCollet1UseTitle.Text = "#1 COLLET USE";
            this.lblCollet1UseTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCollet1UseValue
            // 
            this.lblCollet1UseValue.BackColor = System.Drawing.Color.White;
            this.lblCollet1UseValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet1UseValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet1UseValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCollet1UseValue.Location = new System.Drawing.Point(120, 64);
            this.lblCollet1UseValue.Name = "lblCollet1UseValue";
            this.lblCollet1UseValue.Size = new System.Drawing.Size(72, 28);
            this.lblCollet1UseValue.TabIndex = 5;
            this.lblCollet1UseValue.Text = "0 ea";
            this.lblCollet1UseValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblCollet2UseTitle
            // 
            this.lblCollet2UseTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblCollet2UseTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet2UseTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet2UseTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCollet2UseTitle.Location = new System.Drawing.Point(5, 92);
            this.lblCollet2UseTitle.Name = "lblCollet2UseTitle";
            this.lblCollet2UseTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblCollet2UseTitle.Size = new System.Drawing.Size(109, 28);
            this.lblCollet2UseTitle.TabIndex = 6;
            this.lblCollet2UseTitle.Text = "#2 COLLET USE";
            this.lblCollet2UseTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCollet2UseValue
            // 
            this.lblCollet2UseValue.BackColor = System.Drawing.Color.White;
            this.lblCollet2UseValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet2UseValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet2UseValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCollet2UseValue.Location = new System.Drawing.Point(120, 92);
            this.lblCollet2UseValue.Name = "lblCollet2UseValue";
            this.lblCollet2UseValue.Size = new System.Drawing.Size(72, 28);
            this.lblCollet2UseValue.TabIndex = 7;
            this.lblCollet2UseValue.Text = "0 ea";
            this.lblCollet2UseValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblCollet3UseTitle
            // 
            this.lblCollet3UseTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblCollet3UseTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet3UseTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet3UseTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCollet3UseTitle.Location = new System.Drawing.Point(5, 120);
            this.lblCollet3UseTitle.Name = "lblCollet3UseTitle";
            this.lblCollet3UseTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblCollet3UseTitle.Size = new System.Drawing.Size(109, 28);
            this.lblCollet3UseTitle.TabIndex = 8;
            this.lblCollet3UseTitle.Text = "#3 COLLET USE";
            this.lblCollet3UseTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCollet3UseValue
            // 
            this.lblCollet3UseValue.BackColor = System.Drawing.Color.White;
            this.lblCollet3UseValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet3UseValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet3UseValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCollet3UseValue.Location = new System.Drawing.Point(120, 120);
            this.lblCollet3UseValue.Name = "lblCollet3UseValue";
            this.lblCollet3UseValue.Size = new System.Drawing.Size(72, 28);
            this.lblCollet3UseValue.TabIndex = 9;
            this.lblCollet3UseValue.Text = "0 ea";
            this.lblCollet3UseValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblCollet4UseTitle
            // 
            this.lblCollet4UseTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblCollet4UseTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet4UseTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet4UseTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCollet4UseTitle.Location = new System.Drawing.Point(5, 148);
            this.lblCollet4UseTitle.Name = "lblCollet4UseTitle";
            this.lblCollet4UseTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblCollet4UseTitle.Size = new System.Drawing.Size(109, 28);
            this.lblCollet4UseTitle.TabIndex = 10;
            this.lblCollet4UseTitle.Text = "#4 COLLET USE";
            this.lblCollet4UseTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCollet4UseValue
            // 
            this.lblCollet4UseValue.BackColor = System.Drawing.Color.White;
            this.lblCollet4UseValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet4UseValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet4UseValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCollet4UseValue.Location = new System.Drawing.Point(120, 148);
            this.lblCollet4UseValue.Name = "lblCollet4UseValue";
            this.lblCollet4UseValue.Size = new System.Drawing.Size(72, 28);
            this.lblCollet4UseValue.TabIndex = 11;
            this.lblCollet4UseValue.Text = "0 ea";
            this.lblCollet4UseValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnCountClear
            // 
            this.counterLayout.SetColumnSpan(this.btnCountClear, 2);
            this.btnCountClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCountClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCountClear.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnCountClear.Location = new System.Drawing.Point(5, 177);
            this.btnCountClear.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.btnCountClear.Name = "btnCountClear";
            this.btnCountClear.Size = new System.Drawing.Size(187, 26);
            this.btnCountClear.TabIndex = 12;
            this.btnCountClear.Text = "COUNT CLEAR";
            // 
            // grpInfo
            // 
            this.grpInfo.BackColor = System.Drawing.Color.White;
            this.grpInfo.Controls.Add(this.infoLayout);
            this.grpInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpInfo.Location = new System.Drawing.Point(547, 0);
            this.grpInfo.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.grpInfo.Name = "grpInfo";
            this.grpInfo.Size = new System.Drawing.Size(289, 297);
            this.grpInfo.TabIndex = 2;
            this.grpInfo.TabStop = false;
            this.grpInfo.Text = "INFO";
            // 
            // infoLayout
            // 
            this.infoLayout.ColumnCount = 2;
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.infoLayout.Controls.Add(this.lblProcessDetailTitle, 0, 0);
            this.infoLayout.Controls.Add(this.lblProcessDetailValue, 1, 0);
            this.infoLayout.Controls.Add(this.axis1Panel, 0, 1);
            this.infoLayout.Controls.Add(this.axis2Panel, 0, 2);
            this.infoLayout.Controls.Add(this.axis3Panel, 0, 3);
            this.infoLayout.Controls.Add(this.axis4Panel, 0, 4);
            this.infoLayout.Controls.Add(this.axis5Panel, 0, 5);
            this.infoLayout.Controls.Add(this.axis6Panel, 0, 6);
            this.infoLayout.Controls.Add(this.axis7Panel, 0, 7);
            this.infoLayout.Controls.Add(this.axis8Panel, 0, 8);
            this.infoLayout.Controls.Add(this.axis9Panel, 0, 9);
            this.infoLayout.Controls.Add(this.axis10Panel, 0, 10);
            this.infoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoLayout.Location = new System.Drawing.Point(3, 23);
            this.infoLayout.Name = "infoLayout";
            this.infoLayout.Padding = new System.Windows.Forms.Padding(2, 8, 2, 2);
            this.infoLayout.RowCount = 12;
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.infoLayout.Size = new System.Drawing.Size(283, 271);
            this.infoLayout.TabIndex = 0;
            // 
            // lblProcessDetailTitle
            // 
            this.lblProcessDetailTitle.BackColor = System.Drawing.Color.White;
            this.lblProcessDetailTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProcessDetailTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblProcessDetailTitle.Location = new System.Drawing.Point(5, 8);
            this.lblProcessDetailTitle.Name = "lblProcessDetailTitle";
            this.lblProcessDetailTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblProcessDetailTitle.Size = new System.Drawing.Size(133, 23);
            this.lblProcessDetailTitle.TabIndex = 0;
            this.lblProcessDetailTitle.Text = "PROCESS";
            this.lblProcessDetailTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblProcessDetailValue
            // 
            this.lblProcessDetailValue.BackColor = System.Drawing.Color.White;
            this.lblProcessDetailValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProcessDetailValue.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblProcessDetailValue.Location = new System.Drawing.Point(144, 8);
            this.lblProcessDetailValue.Name = "lblProcessDetailValue";
            this.lblProcessDetailValue.Size = new System.Drawing.Size(134, 23);
            this.lblProcessDetailValue.TabIndex = 1;
            this.lblProcessDetailValue.Text = "-";
            this.lblProcessDetailValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // axis1Panel
            // 
            this.axis1Panel.ColumnCount = 2;
            this.infoLayout.SetColumnSpan(this.axis1Panel, 2);
            this.axis1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.axis1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.axis1Panel.Controls.Add(this.lblAxis1Title, 0, 0);
            this.axis1Panel.Controls.Add(this.lblAxis1Value, 1, 0);
            this.axis1Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axis1Panel.Location = new System.Drawing.Point(2, 32);
            this.axis1Panel.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.axis1Panel.Name = "axis1Panel";
            this.axis1Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axis1Panel.Size = new System.Drawing.Size(279, 21);
            this.axis1Panel.TabIndex = 1;
            // 
            // lblAxis1Title
            // 
            this.lblAxis1Title.BackColor = System.Drawing.Color.Black;
            this.lblAxis1Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis1Title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblAxis1Title.ForeColor = System.Drawing.Color.White;
            this.lblAxis1Title.Location = new System.Drawing.Point(3, 0);
            this.lblAxis1Title.Name = "lblAxis1Title";
            this.lblAxis1Title.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxis1Title.Size = new System.Drawing.Size(147, 21);
            this.lblAxis1Title.TabIndex = 0;
            this.lblAxis1Title.Text = "PICKER X";
            this.lblAxis1Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxis1Value
            // 
            this.lblAxis1Value.BackColor = System.Drawing.Color.White;
            this.lblAxis1Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxis1Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis1Value.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblAxis1Value.Location = new System.Drawing.Point(156, 0);
            this.lblAxis1Value.Name = "lblAxis1Value";
            this.lblAxis1Value.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxis1Value.Size = new System.Drawing.Size(120, 21);
            this.lblAxis1Value.TabIndex = 1;
            this.lblAxis1Value.Text = "-";
            this.lblAxis1Value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // axis2Panel
            // 
            this.axis2Panel.ColumnCount = 2;
            this.infoLayout.SetColumnSpan(this.axis2Panel, 2);
            this.axis2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.axis2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.axis2Panel.Controls.Add(this.lblAxis2Title, 0, 0);
            this.axis2Panel.Controls.Add(this.lblAxis2Value, 1, 0);
            this.axis2Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axis2Panel.Location = new System.Drawing.Point(2, 55);
            this.axis2Panel.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.axis2Panel.Name = "axis2Panel";
            this.axis2Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axis2Panel.Size = new System.Drawing.Size(279, 21);
            this.axis2Panel.TabIndex = 2;
            // 
            // lblAxis2Title
            // 
            this.lblAxis2Title.BackColor = System.Drawing.Color.Black;
            this.lblAxis2Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis2Title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblAxis2Title.ForeColor = System.Drawing.Color.White;
            this.lblAxis2Title.Location = new System.Drawing.Point(3, 0);
            this.lblAxis2Title.Name = "lblAxis2Title";
            this.lblAxis2Title.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxis2Title.Size = new System.Drawing.Size(147, 21);
            this.lblAxis2Title.TabIndex = 0;
            this.lblAxis2Title.Text = "PICKER Y";
            this.lblAxis2Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxis2Value
            // 
            this.lblAxis2Value.BackColor = System.Drawing.Color.White;
            this.lblAxis2Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxis2Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis2Value.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblAxis2Value.Location = new System.Drawing.Point(156, 0);
            this.lblAxis2Value.Name = "lblAxis2Value";
            this.lblAxis2Value.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxis2Value.Size = new System.Drawing.Size(120, 21);
            this.lblAxis2Value.TabIndex = 1;
            this.lblAxis2Value.Text = "-";
            this.lblAxis2Value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // axis3Panel
            // 
            this.axis3Panel.ColumnCount = 2;
            this.infoLayout.SetColumnSpan(this.axis3Panel, 2);
            this.axis3Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.axis3Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.axis3Panel.Controls.Add(this.lblAxis3Title, 0, 0);
            this.axis3Panel.Controls.Add(this.lblAxis3Value, 1, 0);
            this.axis3Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axis3Panel.Location = new System.Drawing.Point(2, 78);
            this.axis3Panel.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.axis3Panel.Name = "axis3Panel";
            this.axis3Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axis3Panel.Size = new System.Drawing.Size(279, 21);
            this.axis3Panel.TabIndex = 3;
            // 
            // lblAxis3Title
            // 
            this.lblAxis3Title.BackColor = System.Drawing.Color.Black;
            this.lblAxis3Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis3Title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblAxis3Title.ForeColor = System.Drawing.Color.White;
            this.lblAxis3Title.Location = new System.Drawing.Point(3, 0);
            this.lblAxis3Title.Name = "lblAxis3Title";
            this.lblAxis3Title.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxis3Title.Size = new System.Drawing.Size(147, 21);
            this.lblAxis3Title.TabIndex = 0;
            this.lblAxis3Title.Text = "PICKER T#1";
            this.lblAxis3Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxis3Value
            // 
            this.lblAxis3Value.BackColor = System.Drawing.Color.White;
            this.lblAxis3Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxis3Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis3Value.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblAxis3Value.Location = new System.Drawing.Point(156, 0);
            this.lblAxis3Value.Name = "lblAxis3Value";
            this.lblAxis3Value.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxis3Value.Size = new System.Drawing.Size(120, 21);
            this.lblAxis3Value.TabIndex = 1;
            this.lblAxis3Value.Text = "-";
            this.lblAxis3Value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // axis4Panel
            // 
            this.axis4Panel.ColumnCount = 2;
            this.infoLayout.SetColumnSpan(this.axis4Panel, 2);
            this.axis4Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.axis4Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.axis4Panel.Controls.Add(this.lblAxis4Title, 0, 0);
            this.axis4Panel.Controls.Add(this.lblAxis4Value, 1, 0);
            this.axis4Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axis4Panel.Location = new System.Drawing.Point(2, 101);
            this.axis4Panel.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.axis4Panel.Name = "axis4Panel";
            this.axis4Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axis4Panel.Size = new System.Drawing.Size(279, 21);
            this.axis4Panel.TabIndex = 4;
            // 
            // lblAxis4Title
            // 
            this.lblAxis4Title.BackColor = System.Drawing.Color.Black;
            this.lblAxis4Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis4Title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblAxis4Title.ForeColor = System.Drawing.Color.White;
            this.lblAxis4Title.Location = new System.Drawing.Point(3, 0);
            this.lblAxis4Title.Name = "lblAxis4Title";
            this.lblAxis4Title.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxis4Title.Size = new System.Drawing.Size(147, 21);
            this.lblAxis4Title.TabIndex = 0;
            this.lblAxis4Title.Text = "PICKER Z#1";
            this.lblAxis4Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxis4Value
            // 
            this.lblAxis4Value.BackColor = System.Drawing.Color.White;
            this.lblAxis4Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxis4Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis4Value.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblAxis4Value.Location = new System.Drawing.Point(156, 0);
            this.lblAxis4Value.Name = "lblAxis4Value";
            this.lblAxis4Value.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxis4Value.Size = new System.Drawing.Size(120, 21);
            this.lblAxis4Value.TabIndex = 1;
            this.lblAxis4Value.Text = "-";
            this.lblAxis4Value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // axis5Panel
            // 
            this.axis5Panel.ColumnCount = 2;
            this.infoLayout.SetColumnSpan(this.axis5Panel, 2);
            this.axis5Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.axis5Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.axis5Panel.Controls.Add(this.lblAxis5Title, 0, 0);
            this.axis5Panel.Controls.Add(this.lblAxis5Value, 1, 0);
            this.axis5Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axis5Panel.Location = new System.Drawing.Point(2, 124);
            this.axis5Panel.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.axis5Panel.Name = "axis5Panel";
            this.axis5Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axis5Panel.Size = new System.Drawing.Size(279, 21);
            this.axis5Panel.TabIndex = 5;
            // 
            // lblAxis5Title
            // 
            this.lblAxis5Title.BackColor = System.Drawing.Color.Black;
            this.lblAxis5Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis5Title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblAxis5Title.ForeColor = System.Drawing.Color.White;
            this.lblAxis5Title.Location = new System.Drawing.Point(3, 0);
            this.lblAxis5Title.Name = "lblAxis5Title";
            this.lblAxis5Title.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxis5Title.Size = new System.Drawing.Size(147, 21);
            this.lblAxis5Title.TabIndex = 0;
            this.lblAxis5Title.Text = "PICKER T#2";
            this.lblAxis5Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxis5Value
            // 
            this.lblAxis5Value.BackColor = System.Drawing.Color.White;
            this.lblAxis5Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxis5Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis5Value.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblAxis5Value.Location = new System.Drawing.Point(156, 0);
            this.lblAxis5Value.Name = "lblAxis5Value";
            this.lblAxis5Value.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxis5Value.Size = new System.Drawing.Size(120, 21);
            this.lblAxis5Value.TabIndex = 1;
            this.lblAxis5Value.Text = "-";
            this.lblAxis5Value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // axis6Panel
            // 
            this.axis6Panel.ColumnCount = 2;
            this.infoLayout.SetColumnSpan(this.axis6Panel, 2);
            this.axis6Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.axis6Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.axis6Panel.Controls.Add(this.lblAxis6Title, 0, 0);
            this.axis6Panel.Controls.Add(this.lblAxis6Value, 1, 0);
            this.axis6Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axis6Panel.Location = new System.Drawing.Point(2, 147);
            this.axis6Panel.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.axis6Panel.Name = "axis6Panel";
            this.axis6Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axis6Panel.Size = new System.Drawing.Size(279, 21);
            this.axis6Panel.TabIndex = 6;
            // 
            // lblAxis6Title
            // 
            this.lblAxis6Title.BackColor = System.Drawing.Color.Black;
            this.lblAxis6Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis6Title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblAxis6Title.ForeColor = System.Drawing.Color.White;
            this.lblAxis6Title.Location = new System.Drawing.Point(3, 0);
            this.lblAxis6Title.Name = "lblAxis6Title";
            this.lblAxis6Title.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxis6Title.Size = new System.Drawing.Size(147, 21);
            this.lblAxis6Title.TabIndex = 0;
            this.lblAxis6Title.Text = "PICKER Z#2";
            this.lblAxis6Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxis6Value
            // 
            this.lblAxis6Value.BackColor = System.Drawing.Color.White;
            this.lblAxis6Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxis6Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis6Value.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblAxis6Value.Location = new System.Drawing.Point(156, 0);
            this.lblAxis6Value.Name = "lblAxis6Value";
            this.lblAxis6Value.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxis6Value.Size = new System.Drawing.Size(120, 21);
            this.lblAxis6Value.TabIndex = 1;
            this.lblAxis6Value.Text = "-";
            this.lblAxis6Value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // axis7Panel
            // 
            this.axis7Panel.ColumnCount = 2;
            this.infoLayout.SetColumnSpan(this.axis7Panel, 2);
            this.axis7Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.axis7Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.axis7Panel.Controls.Add(this.lblAxis7Title, 0, 0);
            this.axis7Panel.Controls.Add(this.lblAxis7Value, 1, 0);
            this.axis7Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axis7Panel.Location = new System.Drawing.Point(2, 170);
            this.axis7Panel.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.axis7Panel.Name = "axis7Panel";
            this.axis7Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axis7Panel.Size = new System.Drawing.Size(279, 21);
            this.axis7Panel.TabIndex = 7;
            // 
            // lblAxis7Title
            // 
            this.lblAxis7Title.BackColor = System.Drawing.Color.Black;
            this.lblAxis7Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis7Title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblAxis7Title.ForeColor = System.Drawing.Color.White;
            this.lblAxis7Title.Location = new System.Drawing.Point(3, 0);
            this.lblAxis7Title.Name = "lblAxis7Title";
            this.lblAxis7Title.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxis7Title.Size = new System.Drawing.Size(147, 21);
            this.lblAxis7Title.TabIndex = 0;
            this.lblAxis7Title.Text = "PICKER T#3";
            this.lblAxis7Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxis7Value
            // 
            this.lblAxis7Value.BackColor = System.Drawing.Color.White;
            this.lblAxis7Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxis7Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis7Value.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblAxis7Value.Location = new System.Drawing.Point(156, 0);
            this.lblAxis7Value.Name = "lblAxis7Value";
            this.lblAxis7Value.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxis7Value.Size = new System.Drawing.Size(120, 21);
            this.lblAxis7Value.TabIndex = 1;
            this.lblAxis7Value.Text = "-";
            this.lblAxis7Value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // axis8Panel
            // 
            this.axis8Panel.ColumnCount = 2;
            this.infoLayout.SetColumnSpan(this.axis8Panel, 2);
            this.axis8Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.axis8Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.axis8Panel.Controls.Add(this.lblAxis8Title, 0, 0);
            this.axis8Panel.Controls.Add(this.lblAxis8Value, 1, 0);
            this.axis8Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axis8Panel.Location = new System.Drawing.Point(2, 193);
            this.axis8Panel.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.axis8Panel.Name = "axis8Panel";
            this.axis8Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axis8Panel.Size = new System.Drawing.Size(279, 21);
            this.axis8Panel.TabIndex = 8;
            // 
            // lblAxis8Title
            // 
            this.lblAxis8Title.BackColor = System.Drawing.Color.Black;
            this.lblAxis8Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis8Title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblAxis8Title.ForeColor = System.Drawing.Color.White;
            this.lblAxis8Title.Location = new System.Drawing.Point(3, 0);
            this.lblAxis8Title.Name = "lblAxis8Title";
            this.lblAxis8Title.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxis8Title.Size = new System.Drawing.Size(147, 21);
            this.lblAxis8Title.TabIndex = 0;
            this.lblAxis8Title.Text = "PICKER Z#3";
            this.lblAxis8Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxis8Value
            // 
            this.lblAxis8Value.BackColor = System.Drawing.Color.White;
            this.lblAxis8Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxis8Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis8Value.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblAxis8Value.Location = new System.Drawing.Point(156, 0);
            this.lblAxis8Value.Name = "lblAxis8Value";
            this.lblAxis8Value.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxis8Value.Size = new System.Drawing.Size(120, 21);
            this.lblAxis8Value.TabIndex = 1;
            this.lblAxis8Value.Text = "-";
            this.lblAxis8Value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // axis9Panel
            // 
            this.axis9Panel.ColumnCount = 2;
            this.infoLayout.SetColumnSpan(this.axis9Panel, 2);
            this.axis9Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.axis9Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.axis9Panel.Controls.Add(this.lblAxis9Title, 0, 0);
            this.axis9Panel.Controls.Add(this.lblAxis9Value, 1, 0);
            this.axis9Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axis9Panel.Location = new System.Drawing.Point(2, 216);
            this.axis9Panel.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.axis9Panel.Name = "axis9Panel";
            this.axis9Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axis9Panel.Size = new System.Drawing.Size(279, 21);
            this.axis9Panel.TabIndex = 9;
            // 
            // lblAxis9Title
            // 
            this.lblAxis9Title.BackColor = System.Drawing.Color.Black;
            this.lblAxis9Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis9Title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblAxis9Title.ForeColor = System.Drawing.Color.White;
            this.lblAxis9Title.Location = new System.Drawing.Point(3, 0);
            this.lblAxis9Title.Name = "lblAxis9Title";
            this.lblAxis9Title.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxis9Title.Size = new System.Drawing.Size(147, 21);
            this.lblAxis9Title.TabIndex = 0;
            this.lblAxis9Title.Text = "PICKER T#4";
            this.lblAxis9Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxis9Value
            // 
            this.lblAxis9Value.BackColor = System.Drawing.Color.White;
            this.lblAxis9Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxis9Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis9Value.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblAxis9Value.Location = new System.Drawing.Point(156, 0);
            this.lblAxis9Value.Name = "lblAxis9Value";
            this.lblAxis9Value.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxis9Value.Size = new System.Drawing.Size(120, 21);
            this.lblAxis9Value.TabIndex = 1;
            this.lblAxis9Value.Text = "-";
            this.lblAxis9Value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // axis10Panel
            // 
            this.axis10Panel.ColumnCount = 2;
            this.infoLayout.SetColumnSpan(this.axis10Panel, 2);
            this.axis10Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.axis10Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.axis10Panel.Controls.Add(this.lblAxis10Title, 0, 0);
            this.axis10Panel.Controls.Add(this.lblAxis10Value, 1, 0);
            this.axis10Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axis10Panel.Location = new System.Drawing.Point(2, 239);
            this.axis10Panel.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.axis10Panel.Name = "axis10Panel";
            this.axis10Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axis10Panel.Size = new System.Drawing.Size(279, 21);
            this.axis10Panel.TabIndex = 10;
            // 
            // lblAxis10Title
            // 
            this.lblAxis10Title.BackColor = System.Drawing.Color.Black;
            this.lblAxis10Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis10Title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblAxis10Title.ForeColor = System.Drawing.Color.White;
            this.lblAxis10Title.Location = new System.Drawing.Point(3, 0);
            this.lblAxis10Title.Name = "lblAxis10Title";
            this.lblAxis10Title.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxis10Title.Size = new System.Drawing.Size(147, 21);
            this.lblAxis10Title.TabIndex = 0;
            this.lblAxis10Title.Text = "PICKER Z#4";
            this.lblAxis10Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxis10Value
            // 
            this.lblAxis10Value.BackColor = System.Drawing.Color.White;
            this.lblAxis10Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxis10Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxis10Value.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblAxis10Value.Location = new System.Drawing.Point(156, 0);
            this.lblAxis10Value.Name = "lblAxis10Value";
            this.lblAxis10Value.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxis10Value.Size = new System.Drawing.Size(120, 21);
            this.lblAxis10Value.TabIndex = 1;
            this.lblAxis10Value.Text = "-";
            this.lblAxis10Value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // grpSensor
            // 
            this.grpSensor.BackColor = System.Drawing.Color.White;
            this.grpSensor.Controls.Add(this.sensorLayout);
            this.grpSensor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSensor.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpSensor.Location = new System.Drawing.Point(0, 303);
            this.grpSensor.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.grpSensor.Name = "grpSensor";
            this.grpSensor.Size = new System.Drawing.Size(836, 74);
            this.grpSensor.TabIndex = 3;
            this.grpSensor.TabStop = false;
            this.grpSensor.Text = "SENSOR STATE";
            // 
            // sensorLayout
            // 
            this.sensorLayout.ColumnCount = 8;
            this.sensorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.sensorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.sensorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.sensorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.sensorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.sensorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.sensorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.sensorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.sensorLayout.Controls.Add(this.headVacuum1Panel, 0, 0);
            this.sensorLayout.Controls.Add(this.headVacuum2Panel, 1, 0);
            this.sensorLayout.Controls.Add(this.headVacuum3Panel, 2, 0);
            this.sensorLayout.Controls.Add(this.headVacuum4Panel, 3, 0);
            this.sensorLayout.Controls.Add(this.headBlow1Panel, 4, 0);
            this.sensorLayout.Controls.Add(this.headBlow2Panel, 5, 0);
            this.sensorLayout.Controls.Add(this.headBlow3Panel, 6, 0);
            this.sensorLayout.Controls.Add(this.headBlow4Panel, 7, 0);
            this.sensorLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorLayout.Location = new System.Drawing.Point(3, 23);
            this.sensorLayout.Name = "sensorLayout";
            this.sensorLayout.Padding = new System.Windows.Forms.Padding(2, 4, 2, 2);
            this.sensorLayout.RowCount = 1;
            this.sensorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorLayout.Size = new System.Drawing.Size(830, 48);
            this.sensorLayout.TabIndex = 0;
            // 
            // headVacuum1Panel
            // 
            this.headVacuum1Panel.ColumnCount = 2;
            this.headVacuum1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.headVacuum1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headVacuum1Panel.Controls.Add(this.dotHeadVacuum1, 0, 0);
            this.headVacuum1Panel.Controls.Add(this.lblHeadVacuum1, 1, 0);
            this.headVacuum1Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headVacuum1Panel.Location = new System.Drawing.Point(5, 7);
            this.headVacuum1Panel.Name = "headVacuum1Panel";
            this.headVacuum1Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headVacuum1Panel.Size = new System.Drawing.Size(97, 36);
            this.headVacuum1Panel.TabIndex = 1;
            // 
            // dotHeadVacuum1
            // 
            this.dotHeadVacuum1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotHeadVacuum1.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadVacuum1.Location = new System.Drawing.Point(2, 12);
            this.dotHeadVacuum1.Margin = new System.Windows.Forms.Padding(2);
            this.dotHeadVacuum1.Name = "dotHeadVacuum1";
            this.dotHeadVacuum1.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadVacuum1.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadVacuum1.Size = new System.Drawing.Size(12, 12);
            this.dotHeadVacuum1.TabIndex = 0;
            // 
            // lblHeadVacuum1
            // 
            this.lblHeadVacuum1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadVacuum1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadVacuum1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadVacuum1.Font = new System.Drawing.Font("맑은 고딕", 7F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblHeadVacuum1.Location = new System.Drawing.Point(19, 0);
            this.lblHeadVacuum1.Name = "lblHeadVacuum1";
            this.lblHeadVacuum1.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblHeadVacuum1.Size = new System.Drawing.Size(75, 36);
            this.lblHeadVacuum1.TabIndex = 1;
            this.lblHeadVacuum1.Text = "VACUUM #1";
            this.lblHeadVacuum1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // headVacuum2Panel
            // 
            this.headVacuum2Panel.ColumnCount = 2;
            this.headVacuum2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.headVacuum2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headVacuum2Panel.Controls.Add(this.dotHeadVacuum2, 0, 0);
            this.headVacuum2Panel.Controls.Add(this.lblHeadVacuum2, 1, 0);
            this.headVacuum2Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headVacuum2Panel.Location = new System.Drawing.Point(108, 7);
            this.headVacuum2Panel.Name = "headVacuum2Panel";
            this.headVacuum2Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headVacuum2Panel.Size = new System.Drawing.Size(97, 36);
            this.headVacuum2Panel.TabIndex = 2;
            // 
            // dotHeadVacuum2
            // 
            this.dotHeadVacuum2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotHeadVacuum2.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadVacuum2.Location = new System.Drawing.Point(2, 12);
            this.dotHeadVacuum2.Margin = new System.Windows.Forms.Padding(2);
            this.dotHeadVacuum2.Name = "dotHeadVacuum2";
            this.dotHeadVacuum2.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadVacuum2.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadVacuum2.Size = new System.Drawing.Size(12, 12);
            this.dotHeadVacuum2.TabIndex = 0;
            // 
            // lblHeadVacuum2
            // 
            this.lblHeadVacuum2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadVacuum2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadVacuum2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadVacuum2.Font = new System.Drawing.Font("맑은 고딕", 7F, System.Drawing.FontStyle.Bold);
            this.lblHeadVacuum2.Location = new System.Drawing.Point(19, 0);
            this.lblHeadVacuum2.Name = "lblHeadVacuum2";
            this.lblHeadVacuum2.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblHeadVacuum2.Size = new System.Drawing.Size(75, 36);
            this.lblHeadVacuum2.TabIndex = 1;
            this.lblHeadVacuum2.Text = "VACUUM #2";
            this.lblHeadVacuum2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // headVacuum3Panel
            // 
            this.headVacuum3Panel.ColumnCount = 2;
            this.headVacuum3Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.headVacuum3Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headVacuum3Panel.Controls.Add(this.dotHeadVacuum3, 0, 0);
            this.headVacuum3Panel.Controls.Add(this.lblHeadVacuum3, 1, 0);
            this.headVacuum3Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headVacuum3Panel.Location = new System.Drawing.Point(211, 7);
            this.headVacuum3Panel.Name = "headVacuum3Panel";
            this.headVacuum3Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headVacuum3Panel.Size = new System.Drawing.Size(97, 36);
            this.headVacuum3Panel.TabIndex = 3;
            // 
            // dotHeadVacuum3
            // 
            this.dotHeadVacuum3.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotHeadVacuum3.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadVacuum3.Location = new System.Drawing.Point(2, 12);
            this.dotHeadVacuum3.Margin = new System.Windows.Forms.Padding(2);
            this.dotHeadVacuum3.Name = "dotHeadVacuum3";
            this.dotHeadVacuum3.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadVacuum3.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadVacuum3.Size = new System.Drawing.Size(12, 12);
            this.dotHeadVacuum3.TabIndex = 0;
            // 
            // lblHeadVacuum3
            // 
            this.lblHeadVacuum3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadVacuum3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadVacuum3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadVacuum3.Font = new System.Drawing.Font("맑은 고딕", 7F, System.Drawing.FontStyle.Bold);
            this.lblHeadVacuum3.Location = new System.Drawing.Point(19, 0);
            this.lblHeadVacuum3.Name = "lblHeadVacuum3";
            this.lblHeadVacuum3.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblHeadVacuum3.Size = new System.Drawing.Size(75, 36);
            this.lblHeadVacuum3.TabIndex = 1;
            this.lblHeadVacuum3.Text = "VACUUM #3";
            this.lblHeadVacuum3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // headVacuum4Panel
            // 
            this.headVacuum4Panel.ColumnCount = 2;
            this.headVacuum4Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.headVacuum4Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headVacuum4Panel.Controls.Add(this.dotHeadVacuum4, 0, 0);
            this.headVacuum4Panel.Controls.Add(this.lblHeadVacuum4, 1, 0);
            this.headVacuum4Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headVacuum4Panel.Location = new System.Drawing.Point(314, 7);
            this.headVacuum4Panel.Name = "headVacuum4Panel";
            this.headVacuum4Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headVacuum4Panel.Size = new System.Drawing.Size(97, 36);
            this.headVacuum4Panel.TabIndex = 4;
            // 
            // dotHeadVacuum4
            // 
            this.dotHeadVacuum4.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotHeadVacuum4.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadVacuum4.Location = new System.Drawing.Point(2, 12);
            this.dotHeadVacuum4.Margin = new System.Windows.Forms.Padding(2);
            this.dotHeadVacuum4.Name = "dotHeadVacuum4";
            this.dotHeadVacuum4.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadVacuum4.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadVacuum4.Size = new System.Drawing.Size(12, 12);
            this.dotHeadVacuum4.TabIndex = 0;
            // 
            // lblHeadVacuum4
            // 
            this.lblHeadVacuum4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadVacuum4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadVacuum4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadVacuum4.Font = new System.Drawing.Font("맑은 고딕", 7F, System.Drawing.FontStyle.Bold);
            this.lblHeadVacuum4.Location = new System.Drawing.Point(19, 0);
            this.lblHeadVacuum4.Name = "lblHeadVacuum4";
            this.lblHeadVacuum4.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblHeadVacuum4.Size = new System.Drawing.Size(75, 36);
            this.lblHeadVacuum4.TabIndex = 1;
            this.lblHeadVacuum4.Text = "VACUUM #4";
            this.lblHeadVacuum4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // headBlow1Panel
            // 
            this.headBlow1Panel.ColumnCount = 2;
            this.headBlow1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.headBlow1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headBlow1Panel.Controls.Add(this.dotHeadBlow1, 0, 0);
            this.headBlow1Panel.Controls.Add(this.lblHeadBlow1, 1, 0);
            this.headBlow1Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headBlow1Panel.Location = new System.Drawing.Point(417, 7);
            this.headBlow1Panel.Name = "headBlow1Panel";
            this.headBlow1Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headBlow1Panel.Size = new System.Drawing.Size(97, 36);
            this.headBlow1Panel.TabIndex = 5;
            // 
            // dotHeadBlow1
            // 
            this.dotHeadBlow1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotHeadBlow1.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadBlow1.Location = new System.Drawing.Point(2, 12);
            this.dotHeadBlow1.Margin = new System.Windows.Forms.Padding(2);
            this.dotHeadBlow1.Name = "dotHeadBlow1";
            this.dotHeadBlow1.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadBlow1.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadBlow1.Size = new System.Drawing.Size(12, 12);
            this.dotHeadBlow1.TabIndex = 0;
            // 
            // lblHeadBlow1
            // 
            this.lblHeadBlow1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadBlow1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadBlow1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadBlow1.Font = new System.Drawing.Font("맑은 고딕", 7F, System.Drawing.FontStyle.Bold);
            this.lblHeadBlow1.Location = new System.Drawing.Point(19, 0);
            this.lblHeadBlow1.Name = "lblHeadBlow1";
            this.lblHeadBlow1.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblHeadBlow1.Size = new System.Drawing.Size(75, 36);
            this.lblHeadBlow1.TabIndex = 1;
            this.lblHeadBlow1.Text = "BLOW #1";
            this.lblHeadBlow1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // headBlow2Panel
            // 
            this.headBlow2Panel.ColumnCount = 2;
            this.headBlow2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.headBlow2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headBlow2Panel.Controls.Add(this.dotHeadBlow2, 0, 0);
            this.headBlow2Panel.Controls.Add(this.lblHeadBlow2, 1, 0);
            this.headBlow2Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headBlow2Panel.Location = new System.Drawing.Point(520, 7);
            this.headBlow2Panel.Name = "headBlow2Panel";
            this.headBlow2Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headBlow2Panel.Size = new System.Drawing.Size(97, 36);
            this.headBlow2Panel.TabIndex = 6;
            // 
            // dotHeadBlow2
            // 
            this.dotHeadBlow2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotHeadBlow2.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadBlow2.Location = new System.Drawing.Point(2, 12);
            this.dotHeadBlow2.Margin = new System.Windows.Forms.Padding(2);
            this.dotHeadBlow2.Name = "dotHeadBlow2";
            this.dotHeadBlow2.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadBlow2.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadBlow2.Size = new System.Drawing.Size(12, 12);
            this.dotHeadBlow2.TabIndex = 0;
            // 
            // lblHeadBlow2
            // 
            this.lblHeadBlow2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadBlow2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadBlow2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadBlow2.Font = new System.Drawing.Font("맑은 고딕", 7F, System.Drawing.FontStyle.Bold);
            this.lblHeadBlow2.Location = new System.Drawing.Point(19, 0);
            this.lblHeadBlow2.Name = "lblHeadBlow2";
            this.lblHeadBlow2.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblHeadBlow2.Size = new System.Drawing.Size(75, 36);
            this.lblHeadBlow2.TabIndex = 1;
            this.lblHeadBlow2.Text = "BLOW #2";
            this.lblHeadBlow2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // headBlow3Panel
            // 
            this.headBlow3Panel.ColumnCount = 2;
            this.headBlow3Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.headBlow3Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headBlow3Panel.Controls.Add(this.dotHeadBlow3, 0, 0);
            this.headBlow3Panel.Controls.Add(this.lblHeadBlow3, 1, 0);
            this.headBlow3Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headBlow3Panel.Location = new System.Drawing.Point(623, 7);
            this.headBlow3Panel.Name = "headBlow3Panel";
            this.headBlow3Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headBlow3Panel.Size = new System.Drawing.Size(97, 36);
            this.headBlow3Panel.TabIndex = 7;
            // 
            // dotHeadBlow3
            // 
            this.dotHeadBlow3.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotHeadBlow3.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadBlow3.Location = new System.Drawing.Point(2, 12);
            this.dotHeadBlow3.Margin = new System.Windows.Forms.Padding(2);
            this.dotHeadBlow3.Name = "dotHeadBlow3";
            this.dotHeadBlow3.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadBlow3.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadBlow3.Size = new System.Drawing.Size(12, 12);
            this.dotHeadBlow3.TabIndex = 0;
            // 
            // lblHeadBlow3
            // 
            this.lblHeadBlow3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadBlow3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadBlow3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadBlow3.Font = new System.Drawing.Font("맑은 고딕", 7F, System.Drawing.FontStyle.Bold);
            this.lblHeadBlow3.Location = new System.Drawing.Point(19, 0);
            this.lblHeadBlow3.Name = "lblHeadBlow3";
            this.lblHeadBlow3.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblHeadBlow3.Size = new System.Drawing.Size(75, 36);
            this.lblHeadBlow3.TabIndex = 1;
            this.lblHeadBlow3.Text = "BLOW #3";
            this.lblHeadBlow3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // headBlow4Panel
            // 
            this.headBlow4Panel.ColumnCount = 2;
            this.headBlow4Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.headBlow4Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headBlow4Panel.Controls.Add(this.dotHeadBlow4, 0, 0);
            this.headBlow4Panel.Controls.Add(this.lblHeadBlow4, 1, 0);
            this.headBlow4Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headBlow4Panel.Location = new System.Drawing.Point(726, 7);
            this.headBlow4Panel.Name = "headBlow4Panel";
            this.headBlow4Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headBlow4Panel.Size = new System.Drawing.Size(99, 36);
            this.headBlow4Panel.TabIndex = 8;
            // 
            // dotHeadBlow4
            // 
            this.dotHeadBlow4.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotHeadBlow4.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadBlow4.Location = new System.Drawing.Point(2, 12);
            this.dotHeadBlow4.Margin = new System.Windows.Forms.Padding(2);
            this.dotHeadBlow4.Name = "dotHeadBlow4";
            this.dotHeadBlow4.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadBlow4.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadBlow4.Size = new System.Drawing.Size(12, 12);
            this.dotHeadBlow4.TabIndex = 0;
            // 
            // lblHeadBlow4
            // 
            this.lblHeadBlow4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadBlow4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadBlow4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadBlow4.Font = new System.Drawing.Font("맑은 고딕", 7F, System.Drawing.FontStyle.Bold);
            this.lblHeadBlow4.Location = new System.Drawing.Point(19, 0);
            this.lblHeadBlow4.Name = "lblHeadBlow4";
            this.lblHeadBlow4.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblHeadBlow4.Size = new System.Drawing.Size(77, 36);
            this.lblHeadBlow4.TabIndex = 1;
            this.lblHeadBlow4.Text = "BLOW #4";
            this.lblHeadBlow4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // materialPanel
            // 
            this.materialPanel.ColumnCount = 1;
            this.materialPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materialPanel.Controls.Add(this.materialHeaderLayout, 0, 0);
            this.materialPanel.Controls.Add(this.headDieDetailView, 0, 1);
            this.materialPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialPanel.Location = new System.Drawing.Point(842, 0);
            this.materialPanel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.materialPanel.Name = "materialPanel";
            this.materialPanel.RowCount = 2;
            this.contentLayout.SetRowSpan(this.materialPanel, 3);
            this.materialPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.materialPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materialPanel.Size = new System.Drawing.Size(836, 770);
            this.materialPanel.TabIndex = 1;
            // 
            // materialHeaderLayout
            // 
            this.materialHeaderLayout.ColumnCount = 5;
            this.materialHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materialHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.materialHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.materialHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.materialHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.materialHeaderLayout.Controls.Add(this.lblHeadDieTitle, 0, 0);
            this.materialHeaderLayout.Controls.Add(this.btnHead1Select, 1, 0);
            this.materialHeaderLayout.Controls.Add(this.btnHead2Select, 2, 0);
            this.materialHeaderLayout.Controls.Add(this.btnHead3Select, 3, 0);
            this.materialHeaderLayout.Controls.Add(this.btnHead4Select, 4, 0);
            this.materialHeaderLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialHeaderLayout.Location = new System.Drawing.Point(3, 3);
            this.materialHeaderLayout.Name = "materialHeaderLayout";
            this.materialHeaderLayout.RowCount = 1;
            this.materialHeaderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materialHeaderLayout.Size = new System.Drawing.Size(830, 46);
            this.materialHeaderLayout.TabIndex = 0;
            // 
            // lblHeadDieTitle
            // 
            this.lblHeadDieTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadDieTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeadDieTitle.Location = new System.Drawing.Point(3, 0);
            this.lblHeadDieTitle.Name = "lblHeadDieTitle";
            this.lblHeadDieTitle.Size = new System.Drawing.Size(544, 46);
            this.lblHeadDieTitle.TabIndex = 0;
            this.lblHeadDieTitle.Text = "HEAD DIE";
            this.lblHeadDieTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnHead1Select
            // 
            this.btnHead1Select.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnHead1Select.Checked = true;
            this.btnHead1Select.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHead1Select.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnHead1Select.Location = new System.Drawing.Point(553, 3);
            this.btnHead1Select.Name = "btnHead1Select";
            this.btnHead1Select.Size = new System.Drawing.Size(64, 40);
            this.btnHead1Select.TabIndex = 1;
            this.btnHead1Select.Text = "HEAD 1";
            // 
            // btnHead2Select
            // 
            this.btnHead2Select.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnHead2Select.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHead2Select.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnHead2Select.Location = new System.Drawing.Point(623, 3);
            this.btnHead2Select.Name = "btnHead2Select";
            this.btnHead2Select.Size = new System.Drawing.Size(64, 40);
            this.btnHead2Select.TabIndex = 2;
            this.btnHead2Select.Text = "HEAD 2";
            // 
            // btnHead3Select
            // 
            this.btnHead3Select.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnHead3Select.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHead3Select.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnHead3Select.Location = new System.Drawing.Point(693, 3);
            this.btnHead3Select.Name = "btnHead3Select";
            this.btnHead3Select.Size = new System.Drawing.Size(64, 40);
            this.btnHead3Select.TabIndex = 3;
            this.btnHead3Select.Text = "HEAD 3";
            // 
            // btnHead4Select
            // 
            this.btnHead4Select.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnHead4Select.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHead4Select.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnHead4Select.Location = new System.Drawing.Point(763, 3);
            this.btnHead4Select.Name = "btnHead4Select";
            this.btnHead4Select.Size = new System.Drawing.Size(64, 40);
            this.btnHead4Select.TabIndex = 4;
            this.btnHead4Select.Text = "HEAD 4";
            // 
            // headDieDetailView
            // 
            this.headDieDetailView.BackColor = System.Drawing.Color.White;
            this.headDieDetailView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headDieDetailView.Location = new System.Drawing.Point(0, 52);
            this.headDieDetailView.Margin = new System.Windows.Forms.Padding(0);
            this.headDieDetailView.Name = "headDieDetailView";
            this.headDieDetailView.ShowProcessTestDataButton = false;
            this.headDieDetailView.ShowInspectionClearButton = true;
            this.headDieDetailView.Size = new System.Drawing.Size(836, 718);
            this.headDieDetailView.TabIndex = 1;
            // 
            // grpAction
            // 
            this.grpAction.BackColor = System.Drawing.Color.White;
            this.grpAction.Controls.Add(this.actionBar);
            this.grpAction.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.grpAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAction.Location = new System.Drawing.Point(0, 383);
            this.grpAction.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
            this.grpAction.Name = "grpAction";
            this.grpAction.Size = new System.Drawing.Size(836, 306);
            this.grpAction.TabIndex = 2;
            this.grpAction.TabStop = false;
            this.grpAction.Text = "ACTION";
            // 
            // actionBar
            // 
            this.actionBar.ColumnCount = 1;
            this.actionBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionBar.Controls.Add(this.actionPanel, 0, 0);
            this.actionBar.Controls.Add(this.actionRightPanel, 0, 1);
            this.actionBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionBar.Location = new System.Drawing.Point(3, 23);
            this.actionBar.Margin = new System.Windows.Forms.Padding(0);
            this.actionBar.Padding = new System.Windows.Forms.Padding(3, 1, 3, 0);
            this.actionBar.Name = "actionBar";
            this.actionBar.RowCount = 2;
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 184F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionBar.Size = new System.Drawing.Size(830, 361);
            this.actionBar.TabIndex = 2;
            // 
            // actionPanel
            // 
            this.actionPanel.BackColor = System.Drawing.Color.White;
            this.actionPanel.ColumnCount = 2;
            this.actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionPanel.Controls.Add(this.btnInput, 0, 0);
            this.actionPanel.Controls.Add(this.btnInspect, 1, 0);
            this.actionPanel.Controls.Add(this.btnBottom, 0, 1);
            this.actionPanel.Controls.Add(this.btnSide, 1, 1);
            this.actionPanel.Controls.Add(this.btnOutput, 0, 2);
            this.actionPanel.Controls.Add(this.btnPickUpTest, 1, 2);
            this.actionPanel.Controls.Add(this.btnAjinLineMapTest, 0, 3);
            this.actionPanel.Controls.Add(this.btnAjinLineMoveTest, 1, 3);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionPanel.Location = new System.Drawing.Point(0, 0);
            this.actionPanel.Margin = new System.Windows.Forms.Padding(0);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.RowCount = 4;
            this.actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionPanel.Size = new System.Drawing.Size(830, 184);
            this.actionPanel.TabIndex = 0;
            // 
            // btnInput
            // 
            this.btnInput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInput.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInput.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnInput.ForeColor = System.Drawing.Color.White;
            this.btnInput.Location = new System.Drawing.Point(3, 3);
            this.btnInput.Name = "btnInput";
            this.btnInput.Size = new System.Drawing.Size(409, 40);
            this.btnInput.TabIndex = 0;
            this.btnInput.Text = "PICK UP";
            // 
            // btnInspect
            // 
            this.btnInspect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInspect.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInspect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInspect.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnInspect.ForeColor = System.Drawing.Color.White;
            this.btnInspect.Location = new System.Drawing.Point(418, 3);
            this.btnInspect.Name = "btnInspect";
            this.btnInspect.Size = new System.Drawing.Size(409, 40);
            this.btnInspect.TabIndex = 1;
            this.btnInspect.Text = "INSPECT";
            // 
            // btnBottom
            // 
            this.btnBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnBottom.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBottom.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnBottom.ForeColor = System.Drawing.Color.White;
            this.btnBottom.Location = new System.Drawing.Point(3, 49);
            this.btnBottom.Name = "btnBottom";
            this.btnBottom.Size = new System.Drawing.Size(409, 40);
            this.btnBottom.TabIndex = 2;
            this.btnBottom.Text = "BOTTOM";
            // 
            // btnSide
            // 
            this.btnSide.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSide.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSide.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnSide.ForeColor = System.Drawing.Color.White;
            this.btnSide.Location = new System.Drawing.Point(418, 49);
            this.btnSide.Name = "btnSide";
            this.btnSide.Size = new System.Drawing.Size(409, 40);
            this.btnSide.TabIndex = 3;
            this.btnSide.Text = "SIDE";
            // 
            // btnOutput
            // 
            this.btnOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnOutput.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOutput.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnOutput.ForeColor = System.Drawing.Color.White;
            this.btnOutput.Location = new System.Drawing.Point(3, 95);
            this.btnOutput.Name = "btnOutput";
            this.btnOutput.Size = new System.Drawing.Size(409, 40);
            this.btnOutput.TabIndex = 4;
            this.btnOutput.Text = "PLACE";
            // 
            // btnPickUpTest
            // 
            this.btnPickUpTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPickUpTest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPickUpTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPickUpTest.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnPickUpTest.ForeColor = System.Drawing.Color.White;
            this.btnPickUpTest.Location = new System.Drawing.Point(418, 95);
            this.btnPickUpTest.Name = "btnPickUpTest";
            this.btnPickUpTest.Size = new System.Drawing.Size(409, 40);
            this.btnPickUpTest.TabIndex = 5;
            this.btnPickUpTest.Text = "PICKUP TEST";
            // 
            // btnAjinLineMapTest
            // 
            this.btnAjinLineMapTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAjinLineMapTest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAjinLineMapTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAjinLineMapTest.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnAjinLineMapTest.ForeColor = System.Drawing.Color.White;
            this.btnAjinLineMapTest.Location = new System.Drawing.Point(3, 141);
            this.btnAjinLineMapTest.Name = "btnAjinLineMapTest";
            this.btnAjinLineMapTest.Size = new System.Drawing.Size(409, 40);
            this.btnAjinLineMapTest.TabIndex = 6;
            this.btnAjinLineMapTest.Text = "LINE MAP TEST";
            this.btnAjinLineMapTest.Click += new System.EventHandler(this.btnAjinLineMapTest_Click);
            // 
            // btnAjinLineMoveTest
            // 
            this.btnAjinLineMoveTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAjinLineMoveTest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAjinLineMoveTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAjinLineMoveTest.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnAjinLineMoveTest.ForeColor = System.Drawing.Color.White;
            this.btnAjinLineMoveTest.Location = new System.Drawing.Point(418, 141);
            this.btnAjinLineMoveTest.Name = "btnAjinLineMoveTest";
            this.btnAjinLineMoveTest.Size = new System.Drawing.Size(409, 40);
            this.btnAjinLineMoveTest.TabIndex = 7;
            this.btnAjinLineMoveTest.Text = "LINE MOVE TEST";
            this.btnAjinLineMoveTest.Click += new System.EventHandler(this.btnAjinLineMoveTest_Click);
            // 
            // actionRightPanel
            // 
            this.actionRightPanel.BackColor = System.Drawing.Color.White;
            this.actionRightPanel.ColumnCount = 2;
            this.actionRightPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionRightPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionRightPanel.Controls.Add(this.btnStop, 1, 0);
            this.actionRightPanel.Controls.Add(this.btnVisionBottomInspect, 0, 1);
            this.actionRightPanel.Controls.Add(this.btnVisionFrontSide, 1, 1);
            this.actionRightPanel.Controls.Add(this.btnVisionRearSide, 0, 2);
            this.actionRightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionRightPanel.Location = new System.Drawing.Point(0, 184);
            this.actionRightPanel.Margin = new System.Windows.Forms.Padding(0);
            this.actionRightPanel.Name = "actionRightPanel";
            this.actionRightPanel.RowCount = 4;
            this.actionRightPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionRightPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionRightPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionRightPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionRightPanel.Size = new System.Drawing.Size(830, 177);
            this.actionRightPanel.TabIndex = 1;
            // 
            // btnVisionBottomInspect
            // 
            this.btnVisionBottomInspect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnVisionBottomInspect.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnVisionBottomInspect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnVisionBottomInspect.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnVisionBottomInspect.ForeColor = System.Drawing.Color.White;
            this.btnVisionBottomInspect.Location = new System.Drawing.Point(3, 49);
            this.btnVisionBottomInspect.Name = "btnVisionBottomInspect";
            this.btnVisionBottomInspect.Size = new System.Drawing.Size(409, 40);
            this.btnVisionBottomInspect.TabIndex = 9;
            this.btnVisionBottomInspect.Text = "VISION: BOTTOM INSP";
            // 
            // btnVisionFrontSide
            // 
            this.btnVisionFrontSide.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnVisionFrontSide.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnVisionFrontSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnVisionFrontSide.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnVisionFrontSide.ForeColor = System.Drawing.Color.White;
            this.btnVisionFrontSide.Location = new System.Drawing.Point(418, 49);
            this.btnVisionFrontSide.Name = "btnVisionFrontSide";
            this.btnVisionFrontSide.Size = new System.Drawing.Size(409, 40);
            this.btnVisionFrontSide.TabIndex = 10;
            this.btnVisionFrontSide.Text = "VISION: FRONT SIDE";
            // 
            // btnVisionRearSide
            // 
            this.btnVisionRearSide.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnVisionRearSide.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnVisionRearSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnVisionRearSide.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnVisionRearSide.ForeColor = System.Drawing.Color.White;
            this.btnVisionRearSide.Location = new System.Drawing.Point(3, 95);
            this.btnVisionRearSide.Name = "btnVisionRearSide";
            this.btnVisionRearSide.Size = new System.Drawing.Size(409, 40);
            this.btnVisionRearSide.TabIndex = 11;
            this.btnVisionRearSide.Text = "VISION: REAR SIDE";
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(214)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(418, 3);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(409, 40);
            this.btnStop.TabIndex = 8;
            this.btnStop.Text = "STOP";
            // 
            // RearPickerPage
            // 
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "RearPickerPage";
            this.Size = new System.Drawing.Size(1678, 800);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.topLayout.ResumeLayout(false);
            this.grpState.ResumeLayout(false);
            this.stateLayout.ResumeLayout(false);
            this.grpCounters.ResumeLayout(false);
            this.counterLayout.ResumeLayout(false);
            this.grpInfo.ResumeLayout(false);
            this.infoLayout.ResumeLayout(false);
            this.axis1Panel.ResumeLayout(false);
            this.axis2Panel.ResumeLayout(false);
            this.axis3Panel.ResumeLayout(false);
            this.axis4Panel.ResumeLayout(false);
            this.axis5Panel.ResumeLayout(false);
            this.axis6Panel.ResumeLayout(false);
            this.axis7Panel.ResumeLayout(false);
            this.axis8Panel.ResumeLayout(false);
            this.axis9Panel.ResumeLayout(false);
            this.axis10Panel.ResumeLayout(false);
            this.grpSensor.ResumeLayout(false);
            this.sensorLayout.ResumeLayout(false);
            this.headVacuum1Panel.ResumeLayout(false);
            this.headVacuum2Panel.ResumeLayout(false);
            this.headVacuum3Panel.ResumeLayout(false);
            this.headVacuum4Panel.ResumeLayout(false);
            this.headBlow1Panel.ResumeLayout(false);
            this.headBlow2Panel.ResumeLayout(false);
            this.headBlow3Panel.ResumeLayout(false);
            this.headBlow4Panel.ResumeLayout(false);
            this.materialPanel.ResumeLayout(false);
            this.materialHeaderLayout.ResumeLayout(false);
            this.grpAction.ResumeLayout(false);
            this.actionBar.ResumeLayout(false);
            this.actionPanel.ResumeLayout(false);
            this.actionRightPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
