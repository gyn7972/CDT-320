using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class PickupSubsetPage
    {
        private TableLayoutPanel editorLayout;
        private TableLayoutPanel settingsLayout;
        private TableLayoutPanel previewLayout;
        private Label lblPreviewCaption;
        private PickupPreviewPanel previewPanel;
        private Label lblTargetCaption;
        private Label lblCornerCaption;
        private Label lblDirectionCaption;
        private Label lblPatternCaption;
        private Panel separator;
        private Panel separator2;
        private Panel separator3;
        private TableLayoutPanel targetLayout;
        private TableLayoutPanel cornerLayout;
        private TableLayoutPanel directionLayout;
        private TableLayoutPanel patternLayout;
        private ChipToggle _rbWafer;
        private ChipToggle _rbBin;
        private RadioButton _rbTL;
        private RadioButton _rbTR;
        private RadioButton _rbBL;
        private RadioButton _rbBR;
        private RadioButton _rbHoriz;
        private RadioButton _rbVert;
        private RadioButton _rbStraight;
        private RadioButton _rbZigZag;

        private void InitializeComponent()
        {
            this.editorLayout = new TableLayoutPanel();
            this.settingsLayout = new TableLayoutPanel();
            this.previewLayout = new TableLayoutPanel();
            this.lblPreviewCaption = new Label();
            this.previewPanel = new PickupPreviewPanel();
            this.lblTargetCaption = new Label();
            this.lblCornerCaption = new Label();
            this.lblDirectionCaption = new Label();
            this.lblPatternCaption = new Label();
            this.separator = new Panel();
            this.separator2 = new Panel();
            this.separator3 = new Panel();
            this.targetLayout = new TableLayoutPanel();
            this.cornerLayout = new TableLayoutPanel();
            this.directionLayout = new TableLayoutPanel();
            this.patternLayout = new TableLayoutPanel();
            this._rbWafer = new ChipToggle();
            this._rbBin = new ChipToggle();
            this._rbTL = new RadioButton();
            this._rbTR = new RadioButton();
            this._rbBL = new RadioButton();
            this._rbBR = new RadioButton();
            this._rbHoriz = new RadioButton();
            this._rbVert = new RadioButton();
            this._rbStraight = new RadioButton();
            this._rbZigZag = new RadioButton();
            this._editorPanel.SuspendLayout();
            this.editorLayout.SuspendLayout();
            this.settingsLayout.SuspendLayout();
            this.previewLayout.SuspendLayout();
            this.targetLayout.SuspendLayout();
            this.cornerLayout.SuspendLayout();
            this.directionLayout.SuspendLayout();
            this.patternLayout.SuspendLayout();
            this.SuspendLayout();
            //
            // _editorPanel
            //
            this._editorPanel.BackColor = System.Drawing.Color.White;
            this._editorPanel.Controls.Add(this.editorLayout);
            this._editorPanel.Size = new Size(1094, 676);
            //
            // _lblProject
            //
            this._lblProject.Size = new Size(794, 36);
            //
            // editorLayout  (좌=설정 / 우=픽업 경로 프리뷰)
            //
            this.editorLayout.BackColor = System.Drawing.Color.White;
            this.editorLayout.ColumnCount = 2;
            this.editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 540F));
            this.editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.editorLayout.Controls.Add(this.settingsLayout, 0, 0);
            this.editorLayout.Controls.Add(this.previewLayout, 1, 0);
            this.editorLayout.Dock = DockStyle.Fill;
            this.editorLayout.Location = new Point(8, 12);
            this.editorLayout.Margin = new Padding(0);
            this.editorLayout.Padding = new Padding(0);
            this.editorLayout.RowCount = 1;
            this.editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.editorLayout.Size = new Size(1078, 656);
            this.editorLayout.TabIndex = 0;
            //
            // settingsLayout  (라벨(좌)+옵션(우), 그룹마다 구분선. 상단 마스터(타겟)=칩 / 하위=라디오)
            //
            this.settingsLayout.BackColor = System.Drawing.Color.White;
            this.settingsLayout.ColumnCount = 3;
            this.settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116F));
            this.settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 392F));
            this.settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.settingsLayout.Controls.Add(this.lblTargetCaption, 0, 0);
            this.settingsLayout.Controls.Add(this.targetLayout, 1, 0);
            this.settingsLayout.Controls.Add(this.separator, 0, 1);
            this.settingsLayout.Controls.Add(this.lblCornerCaption, 0, 2);
            this.settingsLayout.Controls.Add(this.cornerLayout, 1, 2);
            this.settingsLayout.Controls.Add(this.separator2, 0, 3);
            this.settingsLayout.Controls.Add(this.lblDirectionCaption, 0, 4);
            this.settingsLayout.Controls.Add(this.directionLayout, 1, 4);
            this.settingsLayout.Controls.Add(this.separator3, 0, 5);
            this.settingsLayout.Controls.Add(this.lblPatternCaption, 0, 6);
            this.settingsLayout.Controls.Add(this.patternLayout, 1, 6);
            this.settingsLayout.Dock = DockStyle.Fill;
            this.settingsLayout.Margin = new Padding(0);
            this.settingsLayout.Padding = new Padding(0);
            this.settingsLayout.RowCount = 8;
            this.settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            this.settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            this.settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108F));
            this.settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            this.settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            this.settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            this.settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            this.settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.settingsLayout.SetColumnSpan(this.separator, 3);
            this.settingsLayout.SetColumnSpan(this.separator2, 3);
            this.settingsLayout.SetColumnSpan(this.separator3, 3);
            this.settingsLayout.TabIndex = 0;
            //
            // previewLayout  (캡션 + 경로 패널)
            //
            this.previewLayout.BackColor = System.Drawing.Color.White;
            this.previewLayout.ColumnCount = 1;
            this.previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.previewLayout.Controls.Add(this.lblPreviewCaption, 0, 0);
            this.previewLayout.Controls.Add(this.previewPanel, 0, 1);
            this.previewLayout.Dock = DockStyle.Fill;
            this.previewLayout.Margin = new Padding(12, 0, 0, 0);
            this.previewLayout.Padding = new Padding(0);
            this.previewLayout.RowCount = 2;
            this.previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            this.previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.previewLayout.TabIndex = 1;
            //
            // lblPreviewCaption
            //
            this.lblPreviewCaption.Dock = DockStyle.Fill;
            this.lblPreviewCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblPreviewCaption.ForeColor = System.Drawing.Color.FromArgb(0x88, 0x88, 0x88);
            this.lblPreviewCaption.Name = "lblPreviewCaption";
            this.lblPreviewCaption.Text = "Pickup path preview  (S = start, E = end)";
            this.lblPreviewCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // previewPanel
            //
            this.previewPanel.BackColor = System.Drawing.Color.White;
            this.previewPanel.BorderStyle = BorderStyle.FixedSingle;
            this.previewPanel.Dock = DockStyle.Fill;
            this.previewPanel.Margin = new Padding(0, 0, 8, 8);
            this.previewPanel.Name = "previewPanel";
            this.previewPanel.Paint += new PaintEventHandler(this.previewPanel_Paint);
            //
            // caption labels
            //
            this.lblTargetCaption.Dock = DockStyle.Fill;
            this.lblTargetCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblTargetCaption.ForeColor = System.Drawing.Color.FromArgb(0x22, 0x2A, 0x35);
            this.lblTargetCaption.Margin = new Padding(0, 0, 8, 0);
            this.lblTargetCaption.Name = "lblTargetCaption";
            this.lblTargetCaption.Text = "Pickup target";
            this.lblTargetCaption.TextAlign = ContentAlignment.MiddleLeft;
            this.lblCornerCaption.Dock = DockStyle.Fill;
            this.lblCornerCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCornerCaption.ForeColor = System.Drawing.Color.FromArgb(0x33, 0x33, 0x33);
            this.lblCornerCaption.Margin = new Padding(0, 0, 8, 0);
            this.lblCornerCaption.Name = "lblCornerCaption";
            this.lblCornerCaption.Text = "Start corner";
            this.lblCornerCaption.TextAlign = ContentAlignment.MiddleLeft;
            this.lblDirectionCaption.Dock = DockStyle.Fill;
            this.lblDirectionCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblDirectionCaption.ForeColor = System.Drawing.Color.FromArgb(0x33, 0x33, 0x33);
            this.lblDirectionCaption.Margin = new Padding(0, 0, 8, 0);
            this.lblDirectionCaption.Name = "lblDirectionCaption";
            this.lblDirectionCaption.Text = "Pickup direction";
            this.lblDirectionCaption.TextAlign = ContentAlignment.MiddleLeft;
            this.lblPatternCaption.Dock = DockStyle.Fill;
            this.lblPatternCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblPatternCaption.ForeColor = System.Drawing.Color.FromArgb(0x33, 0x33, 0x33);
            this.lblPatternCaption.Margin = new Padding(0, 0, 8, 0);
            this.lblPatternCaption.Name = "lblPatternCaption";
            this.lblPatternCaption.Text = "Pickup pattern";
            this.lblPatternCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // separators (그룹 구분선)
            //
            this.separator.BackColor = System.Drawing.Color.FromArgb(0xD5, 0xD9, 0xDE);
            this.separator.Dock = DockStyle.Top;
            this.separator.Height = 1;
            this.separator.Margin = new Padding(0, 7, 8, 7);
            this.separator.Name = "separator";
            this.separator2.BackColor = System.Drawing.Color.FromArgb(0xD5, 0xD9, 0xDE);
            this.separator2.Dock = DockStyle.Top;
            this.separator2.Height = 1;
            this.separator2.Margin = new Padding(0, 7, 8, 7);
            this.separator2.Name = "separator2";
            this.separator3.BackColor = System.Drawing.Color.FromArgb(0xD5, 0xD9, 0xDE);
            this.separator3.Dock = DockStyle.Top;
            this.separator3.Height = 1;
            this.separator3.Margin = new Padding(0, 7, 8, 7);
            this.separator3.Name = "separator3";
            //
            // targetLayout
            //
            this.targetLayout.BackColor = System.Drawing.Color.White;
            this.targetLayout.ColumnCount = 2;
            this.targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.targetLayout.Dock = DockStyle.Fill;
            this.targetLayout.Margin = new Padding(0);
            this.targetLayout.RowCount = 1;
            this.targetLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.targetLayout.Controls.Add(this._rbWafer, 0, 0);
            this.targetLayout.Controls.Add(this._rbBin, 1, 0);
            //
            // cornerLayout
            //
            this.cornerLayout.BackColor = System.Drawing.Color.White;
            this.cornerLayout.ColumnCount = 2;
            this.cornerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.cornerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.cornerLayout.Dock = DockStyle.Fill;
            this.cornerLayout.Margin = new Padding(0);
            this.cornerLayout.RowCount = 2;
            this.cornerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.cornerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.cornerLayout.Controls.Add(this._rbTL, 0, 0);
            this.cornerLayout.Controls.Add(this._rbTR, 1, 0);
            this.cornerLayout.Controls.Add(this._rbBL, 0, 1);
            this.cornerLayout.Controls.Add(this._rbBR, 1, 1);
            //
            // directionLayout
            //
            this.directionLayout.BackColor = System.Drawing.Color.White;
            this.directionLayout.ColumnCount = 2;
            this.directionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.directionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.directionLayout.Dock = DockStyle.Fill;
            this.directionLayout.Margin = new Padding(0);
            this.directionLayout.RowCount = 1;
            this.directionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.directionLayout.Controls.Add(this._rbHoriz, 0, 0);
            this.directionLayout.Controls.Add(this._rbVert, 1, 0);
            //
            // patternLayout
            //
            this.patternLayout.BackColor = System.Drawing.Color.White;
            this.patternLayout.ColumnCount = 2;
            this.patternLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.patternLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.patternLayout.Dock = DockStyle.Fill;
            this.patternLayout.Margin = new Padding(0);
            this.patternLayout.RowCount = 1;
            this.patternLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.patternLayout.Controls.Add(this._rbStraight, 0, 0);
            this.patternLayout.Controls.Add(this._rbZigZag, 1, 0);
            //
            // 마스터 타겟 = 부드러운 라운드 칩 (ChipToggle 자체 그리기)
            //
            this._rbWafer.Checked = true;
            this._rbWafer.Dock = DockStyle.Fill;
            this._rbWafer.Font = UiTheme.ButtonFont;
            this._rbWafer.Margin = new Padding(2);
            this._rbWafer.Text = "WAFER INPUT";
            this._rbWafer.CheckedChanged += new System.EventHandler(this.PickupTarget_CheckedChanged);
            this._rbBin.Dock = DockStyle.Fill;
            this._rbBin.Font = UiTheme.ButtonFont;
            this._rbBin.Margin = new Padding(2);
            this._rbBin.Text = "BIN OUTPUT";
            this._rbBin.CheckedChanged += new System.EventHandler(this.PickupTarget_CheckedChanged);
            //
            // 하위 옵션 = 실제 라디오(동그라미) + 화살표 아이콘
            //
            this._rbTL.Dock = DockStyle.Fill;
            this._rbTL.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._rbTL.Margin = new Padding(2);
            this._rbTL.Padding = new Padding(6, 0, 0, 0);
            this._rbTL.Text = "↖  Top Left";
            this._rbTL.TextAlign = ContentAlignment.MiddleLeft;
            this._rbTL.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbTR.Dock = DockStyle.Fill;
            this._rbTR.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._rbTR.Margin = new Padding(2);
            this._rbTR.Padding = new Padding(6, 0, 0, 0);
            this._rbTR.Text = "↗  Top Right";
            this._rbTR.TextAlign = ContentAlignment.MiddleLeft;
            this._rbTR.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbBL.Dock = DockStyle.Fill;
            this._rbBL.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._rbBL.Margin = new Padding(2);
            this._rbBL.Padding = new Padding(6, 0, 0, 0);
            this._rbBL.Text = "↙  Bottom Left";
            this._rbBL.TextAlign = ContentAlignment.MiddleLeft;
            this._rbBL.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbBR.Dock = DockStyle.Fill;
            this._rbBR.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._rbBR.Margin = new Padding(2);
            this._rbBR.Padding = new Padding(6, 0, 0, 0);
            this._rbBR.Text = "↘  Bottom Right";
            this._rbBR.TextAlign = ContentAlignment.MiddleLeft;
            this._rbBR.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbHoriz.Dock = DockStyle.Fill;
            this._rbHoriz.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._rbHoriz.Margin = new Padding(2);
            this._rbHoriz.Padding = new Padding(6, 0, 0, 0);
            this._rbHoriz.Text = "→  Horizontal";
            this._rbHoriz.TextAlign = ContentAlignment.MiddleLeft;
            this._rbHoriz.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbVert.Dock = DockStyle.Fill;
            this._rbVert.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._rbVert.Margin = new Padding(2);
            this._rbVert.Padding = new Padding(6, 0, 0, 0);
            this._rbVert.Text = "↓  Vertical";
            this._rbVert.TextAlign = ContentAlignment.MiddleLeft;
            this._rbVert.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbStraight.Checked = true;
            this._rbStraight.Dock = DockStyle.Fill;
            this._rbStraight.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._rbStraight.Margin = new Padding(2);
            this._rbStraight.Padding = new Padding(6, 0, 0, 0);
            this._rbStraight.Text = "→  Straight";
            this._rbStraight.TextAlign = ContentAlignment.MiddleLeft;
            this._rbStraight.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbZigZag.Dock = DockStyle.Fill;
            this._rbZigZag.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._rbZigZag.Margin = new Padding(2);
            this._rbZigZag.Padding = new Padding(6, 0, 0, 0);
            this._rbZigZag.Text = "⇄  ZigZag";
            this._rbZigZag.TextAlign = ContentAlignment.MiddleLeft;
            this._rbZigZag.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            //
            // PickupSubsetPage
            //
            this.Name = "PickupSubsetPage";
            this.Size = new Size(1094, 742);
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this.patternLayout.ResumeLayout(false);
            this.directionLayout.ResumeLayout(false);
            this.cornerLayout.ResumeLayout(false);
            this.targetLayout.ResumeLayout(false);
            this.previewLayout.ResumeLayout(false);
            this.settingsLayout.ResumeLayout(false);
            this.editorLayout.ResumeLayout(false);
            this._editorPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
