using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class PickupSubsetPage
    {
        private TableLayoutPanel editorLayout;
        private GroupBox grpTarget;
        private GroupBox grpCorner;
        private GroupBox grpDirection;
        private GroupBox grpPattern;
        private TableLayoutPanel targetLayout;
        private TableLayoutPanel cornerLayout;
        private TableLayoutPanel directionLayout;
        private TableLayoutPanel patternLayout;
        private RadioButton _rbWafer;
        private RadioButton _rbBin;
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
            this.grpTarget = new GroupBox();
            this.grpCorner = new GroupBox();
            this.grpDirection = new GroupBox();
            this.grpPattern = new GroupBox();
            this.targetLayout = new TableLayoutPanel();
            this.cornerLayout = new TableLayoutPanel();
            this.directionLayout = new TableLayoutPanel();
            this.patternLayout = new TableLayoutPanel();
            this._rbWafer = new RadioButton();
            this._rbBin = new RadioButton();
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
            this.grpTarget.SuspendLayout();
            this.grpCorner.SuspendLayout();
            this.grpDirection.SuspendLayout();
            this.grpPattern.SuspendLayout();
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
            // editorLayout  (좌측 50%만 사용, 그룹박스 세로 배치 + 균등 여백)
            //
            this.editorLayout.BackColor = System.Drawing.Color.White;
            this.editorLayout.ColumnCount = 2;
            this.editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.editorLayout.Controls.Add(this.grpTarget, 0, 0);
            this.editorLayout.Controls.Add(this.grpCorner, 0, 2);
            this.editorLayout.Controls.Add(this.grpDirection, 0, 4);
            this.editorLayout.Controls.Add(this.grpPattern, 0, 6);
            this.editorLayout.Dock = DockStyle.Fill;
            this.editorLayout.Location = new Point(8, 12);
            this.editorLayout.Margin = new Padding(0);
            this.editorLayout.Padding = new Padding(0);
            this.editorLayout.RowCount = 7;
            this.editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            this.editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            this.editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            this.editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            this.editorLayout.Size = new Size(1078, 656);
            this.editorLayout.TabIndex = 0;
            //
            // grpTarget
            //
            this.grpTarget.BackColor = System.Drawing.Color.White;
            this.grpTarget.Controls.Add(this.targetLayout);
            this.grpTarget.Dock = DockStyle.Fill;
            this.grpTarget.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpTarget.Margin = new Padding(0);
            this.grpTarget.Name = "grpTarget";
            this.grpTarget.Padding = new Padding(6, 2, 6, 6);
            this.grpTarget.TabStop = false;
            this.grpTarget.Text = "Pickup target";
            //
            // grpCorner
            //
            this.grpCorner.BackColor = System.Drawing.Color.White;
            this.grpCorner.Controls.Add(this.cornerLayout);
            this.grpCorner.Dock = DockStyle.Fill;
            this.grpCorner.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpCorner.Margin = new Padding(0);
            this.grpCorner.Name = "grpCorner";
            this.grpCorner.Padding = new Padding(6, 2, 6, 6);
            this.grpCorner.TabStop = false;
            this.grpCorner.Text = "Start corner";
            //
            // grpDirection
            //
            this.grpDirection.BackColor = System.Drawing.Color.White;
            this.grpDirection.Controls.Add(this.directionLayout);
            this.grpDirection.Dock = DockStyle.Fill;
            this.grpDirection.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpDirection.Margin = new Padding(0);
            this.grpDirection.Name = "grpDirection";
            this.grpDirection.Padding = new Padding(6, 2, 6, 6);
            this.grpDirection.TabStop = false;
            this.grpDirection.Text = "Pickup direction";
            //
            // grpPattern
            //
            this.grpPattern.BackColor = System.Drawing.Color.White;
            this.grpPattern.Controls.Add(this.patternLayout);
            this.grpPattern.Dock = DockStyle.Fill;
            this.grpPattern.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpPattern.Margin = new Padding(0);
            this.grpPattern.Name = "grpPattern";
            this.grpPattern.Padding = new Padding(6, 2, 6, 6);
            this.grpPattern.TabStop = false;
            this.grpPattern.Text = "Pickup pattern";
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
            // radio buttons
            //
            this._rbWafer.Appearance = Appearance.Button;
            this._rbWafer.Checked = true;
            this._rbWafer.Dock = DockStyle.Fill;
            this._rbWafer.FlatStyle = FlatStyle.Flat;
            this._rbWafer.Font = UiTheme.ButtonFont;
            this._rbWafer.Text = "WAFER INPUT";
            this._rbWafer.TextAlign = ContentAlignment.MiddleCenter;
            this._rbWafer.CheckedChanged += new System.EventHandler(this.PickupTarget_CheckedChanged);
            this._rbBin.Appearance = Appearance.Button;
            this._rbBin.Dock = DockStyle.Fill;
            this._rbBin.FlatStyle = FlatStyle.Flat;
            this._rbBin.Font = UiTheme.ButtonFont;
            this._rbBin.Text = "BIN OUTPUT";
            this._rbBin.TextAlign = ContentAlignment.MiddleCenter;
            this._rbBin.CheckedChanged += new System.EventHandler(this.PickupTarget_CheckedChanged);
            this._rbTL.Appearance = Appearance.Button;
            this._rbTL.Dock = DockStyle.Fill;
            this._rbTL.FlatStyle = FlatStyle.Flat;
            this._rbTL.Font = UiTheme.ButtonFont;
            this._rbTL.Text = "Top Left";
            this._rbTL.TextAlign = ContentAlignment.MiddleCenter;
            this._rbTL.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbTR.Appearance = Appearance.Button;
            this._rbTR.Dock = DockStyle.Fill;
            this._rbTR.FlatStyle = FlatStyle.Flat;
            this._rbTR.Font = UiTheme.ButtonFont;
            this._rbTR.Text = "Top Right";
            this._rbTR.TextAlign = ContentAlignment.MiddleCenter;
            this._rbTR.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbBL.Appearance = Appearance.Button;
            this._rbBL.Dock = DockStyle.Fill;
            this._rbBL.FlatStyle = FlatStyle.Flat;
            this._rbBL.Font = UiTheme.ButtonFont;
            this._rbBL.Text = "Bottom Left";
            this._rbBL.TextAlign = ContentAlignment.MiddleCenter;
            this._rbBL.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbBR.Appearance = Appearance.Button;
            this._rbBR.Dock = DockStyle.Fill;
            this._rbBR.FlatStyle = FlatStyle.Flat;
            this._rbBR.Font = UiTheme.ButtonFont;
            this._rbBR.Text = "Bottom Right";
            this._rbBR.TextAlign = ContentAlignment.MiddleCenter;
            this._rbBR.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbHoriz.Appearance = Appearance.Button;
            this._rbHoriz.Dock = DockStyle.Fill;
            this._rbHoriz.FlatStyle = FlatStyle.Flat;
            this._rbHoriz.Font = UiTheme.ButtonFont;
            this._rbHoriz.Padding = new Padding(0);
            this._rbHoriz.Text = "Horizontal";
            this._rbHoriz.TextAlign = ContentAlignment.MiddleCenter;
            this._rbHoriz.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbVert.Appearance = Appearance.Button;
            this._rbVert.Dock = DockStyle.Fill;
            this._rbVert.FlatStyle = FlatStyle.Flat;
            this._rbVert.Font = UiTheme.ButtonFont;
            this._rbVert.Padding = new Padding(0);
            this._rbVert.Text = "Vertical";
            this._rbVert.TextAlign = ContentAlignment.MiddleCenter;
            this._rbVert.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbStraight.Appearance = Appearance.Button;
            this._rbStraight.Checked = true;
            this._rbStraight.Dock = DockStyle.Fill;
            this._rbStraight.FlatStyle = FlatStyle.Flat;
            this._rbStraight.Font = UiTheme.ButtonFont;
            this._rbStraight.Padding = new Padding(0);
            this._rbStraight.Text = "Straight";
            this._rbStraight.TextAlign = ContentAlignment.MiddleCenter;
            this._rbStraight.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbZigZag.Appearance = Appearance.Button;
            this._rbZigZag.Dock = DockStyle.Fill;
            this._rbZigZag.FlatStyle = FlatStyle.Flat;
            this._rbZigZag.Font = UiTheme.ButtonFont;
            this._rbZigZag.Padding = new Padding(0);
            this._rbZigZag.Text = "ZigZag";
            this._rbZigZag.TextAlign = ContentAlignment.MiddleCenter;
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
            this.grpTarget.ResumeLayout(false);
            this.grpCorner.ResumeLayout(false);
            this.grpDirection.ResumeLayout(false);
            this.grpPattern.ResumeLayout(false);
            this.editorLayout.ResumeLayout(false);
            this._editorPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
