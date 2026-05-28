using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class PickupSubsetPage
    {
        private TableLayoutPanel pickupLayout;
        private Label lblCornerHeader;
        private Label lblDirectionHeader;
        private Label lblPatternHeader;
        private TableLayoutPanel cornerLayout;
        private TableLayoutPanel directionLayout;
        private TableLayoutPanel patternLayout;
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
            this.pickupLayout = new TableLayoutPanel();
            this.lblCornerHeader = new Label();
            this.lblDirectionHeader = new Label();
            this.lblPatternHeader = new Label();
            this.cornerLayout = new TableLayoutPanel();
            this.directionLayout = new TableLayoutPanel();
            this.patternLayout = new TableLayoutPanel();
            this._rbTL = new RadioButton();
            this._rbTR = new RadioButton();
            this._rbBL = new RadioButton();
            this._rbBR = new RadioButton();
            this._rbHoriz = new RadioButton();
            this._rbVert = new RadioButton();
            this._rbStraight = new RadioButton();
            this._rbZigZag = new RadioButton();
            this._editorPanel.SuspendLayout();
            this.pickupLayout.SuspendLayout();
            this.cornerLayout.SuspendLayout();
            this.directionLayout.SuspendLayout();
            this.patternLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // pickupLayout
            // 
            this.pickupLayout.ColumnCount = 1;
            this.pickupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 520F));
            this.pickupLayout.Dock = DockStyle.Top;
            this.pickupLayout.Margin = new Padding(0);
            this.pickupLayout.Padding = new Padding(10);
            this.pickupLayout.RowCount = 6;
            this.pickupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.pickupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116F));
            this.pickupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.pickupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            this.pickupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.pickupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            this.pickupLayout.Size = new Size(540, 410);
            // 
            // headers
            // 
            this.lblCornerHeader.BackColor = Color.LightYellow;
            this.lblCornerHeader.BorderStyle = BorderStyle.FixedSingle;
            this.lblCornerHeader.Dock = DockStyle.Fill;
            this.lblCornerHeader.Font = UiTheme.SectionFont;
            this.lblCornerHeader.ForeColor = Color.DarkSlateGray;
            this.lblCornerHeader.Padding = new Padding(8, 0, 0, 0);
            this.lblCornerHeader.Text = "Start corner";
            this.lblCornerHeader.TextAlign = ContentAlignment.MiddleLeft;
            this.lblDirectionHeader.BackColor = Color.LightYellow;
            this.lblDirectionHeader.BorderStyle = BorderStyle.FixedSingle;
            this.lblDirectionHeader.Dock = DockStyle.Fill;
            this.lblDirectionHeader.Font = UiTheme.SectionFont;
            this.lblDirectionHeader.ForeColor = Color.DarkSlateGray;
            this.lblDirectionHeader.Padding = new Padding(8, 0, 0, 0);
            this.lblDirectionHeader.Text = "Pickup direction";
            this.lblDirectionHeader.TextAlign = ContentAlignment.MiddleLeft;
            this.lblPatternHeader.BackColor = Color.LightYellow;
            this.lblPatternHeader.BorderStyle = BorderStyle.FixedSingle;
            this.lblPatternHeader.Dock = DockStyle.Fill;
            this.lblPatternHeader.Font = UiTheme.SectionFont;
            this.lblPatternHeader.ForeColor = Color.DarkSlateGray;
            this.lblPatternHeader.Padding = new Padding(8, 0, 0, 0);
            this.lblPatternHeader.Text = "Pickup pattern";
            this.lblPatternHeader.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cornerLayout
            // 
            this.cornerLayout.ColumnCount = 2;
            this.cornerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.cornerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.cornerLayout.Dock = DockStyle.Fill;
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
            this.directionLayout.ColumnCount = 1;
            this.directionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.directionLayout.Dock = DockStyle.Fill;
            this.directionLayout.RowCount = 2;
            this.directionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.directionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.directionLayout.Controls.Add(this._rbHoriz, 0, 0);
            this.directionLayout.Controls.Add(this._rbVert, 0, 1);
            // 
            // patternLayout
            // 
            this.patternLayout.ColumnCount = 1;
            this.patternLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.patternLayout.Dock = DockStyle.Fill;
            this.patternLayout.RowCount = 2;
            this.patternLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.patternLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.patternLayout.Controls.Add(this._rbStraight, 0, 0);
            this.patternLayout.Controls.Add(this._rbZigZag, 0, 1);
            // 
            // radio buttons
            // 
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
            this._rbHoriz.Padding = new Padding(16, 0, 0, 0);
            this._rbHoriz.Text = "Horizontal";
            this._rbHoriz.TextAlign = ContentAlignment.MiddleLeft;
            this._rbHoriz.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbVert.Appearance = Appearance.Button;
            this._rbVert.Dock = DockStyle.Fill;
            this._rbVert.FlatStyle = FlatStyle.Flat;
            this._rbVert.Font = UiTheme.ButtonFont;
            this._rbVert.Padding = new Padding(16, 0, 0, 0);
            this._rbVert.Text = "Vertical";
            this._rbVert.TextAlign = ContentAlignment.MiddleLeft;
            this._rbVert.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbStraight.Appearance = Appearance.Button;
            this._rbStraight.Checked = true;
            this._rbStraight.Dock = DockStyle.Fill;
            this._rbStraight.FlatStyle = FlatStyle.Flat;
            this._rbStraight.Font = UiTheme.ButtonFont;
            this._rbStraight.Padding = new Padding(16, 0, 0, 0);
            this._rbStraight.Text = "Straight";
            this._rbStraight.TextAlign = ContentAlignment.MiddleLeft;
            this._rbStraight.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            this._rbZigZag.Appearance = Appearance.Button;
            this._rbZigZag.Dock = DockStyle.Fill;
            this._rbZigZag.FlatStyle = FlatStyle.Flat;
            this._rbZigZag.Font = UiTheme.ButtonFont;
            this._rbZigZag.Padding = new Padding(16, 0, 0, 0);
            this._rbZigZag.Text = "ZigZag";
            this._rbZigZag.TextAlign = ContentAlignment.MiddleLeft;
            this._rbZigZag.CheckedChanged += new System.EventHandler(this.PickupRadio_CheckedChanged);
            // 
            // layout controls
            // 
            this.pickupLayout.Controls.Add(this.lblCornerHeader, 0, 0);
            this.pickupLayout.Controls.Add(this.cornerLayout, 0, 1);
            this.pickupLayout.Controls.Add(this.lblDirectionHeader, 0, 2);
            this.pickupLayout.Controls.Add(this.directionLayout, 0, 3);
            this.pickupLayout.Controls.Add(this.lblPatternHeader, 0, 4);
            this.pickupLayout.Controls.Add(this.patternLayout, 0, 5);
            this._editorPanel.Controls.Add(this.pickupLayout);
            this.Name = "PickupSubsetPage";
            this.Size = new Size(1094, 742);
            this.patternLayout.ResumeLayout(false);
            this.directionLayout.ResumeLayout(false);
            this.cornerLayout.ResumeLayout(false);
            this.pickupLayout.ResumeLayout(false);
            this._editorPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}