using System;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class InputFeederPage
    {
        private Panel rootPanel;
        private Panel pnlWork;
        private Panel pnlCylinder;
        private Panel pnlInfo;
        private Label lblWorkHeader;
        private Label lblCylinderHeader;
        private Label lblInfoHeader;
        private Label lblExistCaption;
        private Label _lblExist;
        private Label lblClampCaption;
        private Label _lblClampState;
        private Label lblUpDownCaption;
        private Label _lblUpDownState;
        private Label lblFeederPosCaption;
        private Label _lblFeederPos;
        private Label _markRing;
        private Label lblRingCaption;
        private Label _markOverload;
        private Label lblOverloadCaption;

        private static readonly Color CdtPageBg = Color.FromArgb(0xC9, 0xCB, 0xCD);
        private static readonly Color CdtHeaderBg = Color.FromArgb(0xF0, 0x78, 0x00);
        private static readonly Color CdtLabelBg = Color.FromArgb(0xB8, 0xBC, 0xC2);
        private static readonly Color CdtValueBg = Color.FromArgb(0xA9, 0xAB, 0xAE);

        private void InitializeComponent()
        {
            this.rootPanel = new Panel();
            this.pnlWork = new Panel();
            this.pnlCylinder = new Panel();
            this.pnlInfo = new Panel();
            this.lblWorkHeader = CreateHeaderLabel("작업 정보");
            this.lblCylinderHeader = CreateHeaderLabel("FEEDER 실린더 정보");
            this.lblInfoHeader = CreateHeaderLabel("정보");
            this.lblExistCaption = CreateCaptionLabel("EXIST");
            this._lblExist = CreateValueLabel("--");
            this.lblClampCaption = CreateCaptionLabel("FEEDER CLAMP");
            this._lblClampState = CreateValueLabel("--");
            this.lblUpDownCaption = CreateCaptionLabel("FEEDER UP DOWN");
            this._lblUpDownState = CreateValueLabel("--");
            this.lblFeederPosCaption = CreateCaptionLabel("FEEDER AXIS Y");
            this._lblFeederPos = CreateValueLabel("0 um");
            this._markRing = CreateMarker();
            this.lblRingCaption = CreateSensorLabel("FEEDER RING CHECK");
            this._markOverload = CreateMarker();
            this.lblOverloadCaption = CreateSensorLabel("FEEDER OVERLOAD CHECK");
            this.rootPanel.SuspendLayout();
            this.pnlWork.SuspendLayout();
            this.pnlCylinder.SuspendLayout();
            this.pnlInfo.SuspendLayout();
            this.SuspendLayout();

            ConfigureSectionPanel(this.pnlWork, BorderStyle.FixedSingle);
            ConfigureSectionPanel(this.pnlCylinder, BorderStyle.FixedSingle);
            ConfigureSectionPanel(this.pnlInfo, BorderStyle.None);

            this.pnlWork.Controls.Add(this.lblExistCaption);
            this.pnlWork.Controls.Add(this._lblExist);
            this.pnlWork.Controls.Add(this.lblWorkHeader);

            this.pnlCylinder.Controls.Add(this.lblClampCaption);
            this.pnlCylinder.Controls.Add(this._lblClampState);
            this.pnlCylinder.Controls.Add(this.lblUpDownCaption);
            this.pnlCylinder.Controls.Add(this._lblUpDownState);
            this.pnlCylinder.Controls.Add(this.lblCylinderHeader);

            this.pnlInfo.Controls.Add(this.lblFeederPosCaption);
            this.pnlInfo.Controls.Add(this._lblFeederPos);
            this.pnlInfo.Controls.Add(this._markRing);
            this.pnlInfo.Controls.Add(this.lblRingCaption);
            this.pnlInfo.Controls.Add(this._markOverload);
            this.pnlInfo.Controls.Add(this.lblOverloadCaption);
            this.pnlInfo.Controls.Add(this.lblInfoHeader);

            this.rootPanel.BackColor = CdtPageBg;
            this.rootPanel.Controls.Add(this.pnlWork);
            this.rootPanel.Controls.Add(this.pnlCylinder);
            this.rootPanel.Controls.Add(this.pnlInfo);
            this.rootPanel.Dock = DockStyle.Fill;
            this.rootPanel.Margin = Padding.Empty;
            this.rootPanel.Padding = Padding.Empty;
            this.rootPanel.Resize += new EventHandler(this.RootPanel_Resize);

            this.lblExistCaption.SetBounds(10, 28, 204, 32);
            this._lblExist.SetBounds(214, 28, 210, 32);

            this.lblClampCaption.SetBounds(10, 28, 204, 32);
            this._lblClampState.SetBounds(214, 28, 236, 32);
            this.lblUpDownCaption.SetBounds(10, 60, 204, 32);
            this._lblUpDownState.SetBounds(214, 60, 236, 32);

            this.lblFeederPosCaption.SetBounds(16, 34, 318, 30);
            this._lblFeederPos.SetBounds(334, 34, 76, 30);
            this._markRing.SetBounds(16, 76, 16, 16);
            this.lblRingCaption.SetBounds(36, 70, 328, 28);
            this._markOverload.SetBounds(16, 108, 16, 16);
            this.lblOverloadCaption.SetBounds(36, 102, 328, 28);

            this.Controls.Add(this.rootPanel);
            this.BackColor = CdtPageBg;
            this.Name = "InputFeederPage";
            this.Size = new Size(1678, 900);
            this.rootPanel.ResumeLayout(false);
            this.pnlWork.ResumeLayout(false);
            this.pnlCylinder.ResumeLayout(false);
            this.pnlInfo.ResumeLayout(false);
            this.ResumeLayout(false);
            LayoutSections();
        }

        private void RootPanel_Resize(object sender, EventArgs e)
        {
            LayoutSections();
        }

        private void LayoutSections()
        {
            if (rootPanel == null) return;

            int width = Math.Max(1, rootPanel.ClientSize.Width);
            int height = Math.Max(1, rootPanel.ClientSize.Height);
            int topHeight = Math.Min(254, height);
            int halfWidth = width / 2;

            pnlWork.SetBounds(0, 0, halfWidth, topHeight);
            pnlCylinder.SetBounds(halfWidth, 0, width - halfWidth, topHeight);
            pnlInfo.SetBounds(0, topHeight, width, height - topHeight);
        }

        private static void ConfigureSectionPanel(Panel panel, BorderStyle borderStyle)
        {
            panel.BackColor = CdtPageBg;
            panel.BorderStyle = borderStyle;
            panel.Margin = Padding.Empty;
            panel.Padding = Padding.Empty;
        }

        private static Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                BackColor = CdtHeaderBg,
                Dock = DockStyle.Top,
                Font = new Font("Malgun Gothic", 8F, FontStyle.Bold),
                ForeColor = Color.White,
                Height = 24,
                Padding = new Padding(8, 0, 0, 0),
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Label CreateCaptionLabel(string text)
        {
            return new Label
            {
                BackColor = CdtLabelBg,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Malgun Gothic", 7F, FontStyle.Bold),
                ForeColor = Color.Black,
                Padding = new Padding(8, 0, 0, 0),
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Label CreateValueLabel(string text)
        {
            return new Label
            {
                BackColor = CdtValueBg,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 7F, FontStyle.Bold),
                ForeColor = Color.Black,
                Padding = new Padding(0, 0, 8, 0),
                Text = text,
                TextAlign = ContentAlignment.MiddleRight
            };
        }

        private static Label CreateSensorLabel(string text)
        {
            return new Label
            {
                BackColor = CdtLabelBg,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Malgun Gothic", 7F, FontStyle.Bold),
                ForeColor = Color.Black,
                Padding = new Padding(8, 0, 0, 0),
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Label CreateMarker()
        {
            return new Label
            {
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
        }
    }
}
