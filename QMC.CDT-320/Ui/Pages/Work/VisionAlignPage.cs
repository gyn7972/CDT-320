using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class VisionAlignPage : PageBase
    {
        public VisionAlignPage()
        {
            InitializeComponent();
            InitializeLanguageBindings();
            BuildLayout();
            ApplyRuntimeUi();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("work.visionAlign");
            lblHeader.Tag = "i18n:work.visionAlign";
        }

        /// <summary>좌 7 : 우 3 배치. 좌=비전 화면 그대로, 우=ACTION/RESULT 그룹박스.
        /// 상단 헤더는 그룹박스 아닌 현재 바 유지, 여백 최소화. 결과 라벨은 아웃풋 BIN/DIE INFO 스타일.</summary>
        private void BuildLayout()
        {
            foreach (Control b in new Control[] { btnAutoAlign, btnManualAlign, btnFirstMark, btnSecondMark, btnThetaMatch, btnXyMatch, btnSave, btnClose })
            {
                b.Dock = DockStyle.Fill;
                b.Margin = new Padding(3);
                b.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            }
            StyleResultLabelsLikeOutput();
        }

        /// <summary>RESULT 캡션/값 라벨을 아웃풋 전환 페이지 BIN/DIE INFO 그룹과 동일한 스타일로 통일.</summary>
        private void StyleResultLabelsLikeOutput()
        {
            Color capBack = Color.FromArgb(236, 238, 241);
            Color capFore = Color.FromArgb(70, 70, 70);
            Color valFore = Color.FromArgb(25, 29, 34);

            Label[] captions = { lblDeltaXCaption, lblDeltaYCaption, lblDeltaThetaCaption, lblScoreCaption };
            Label[] values = { lblDeltaXValue, lblDeltaYValue, lblDeltaThetaValue, lblScoreValue };

            foreach (Label c in captions)
            {
                if (c == null) continue;
                c.AutoEllipsis = true;
                c.BackColor = capBack;
                c.BorderStyle = BorderStyle.FixedSingle;
                c.Dock = DockStyle.Fill;
                c.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                c.ForeColor = capFore;
                c.Margin = new Padding(1);
                c.Padding = new Padding(6, 0, 0, 0);
                c.TextAlign = ContentAlignment.MiddleLeft;
            }
            foreach (Label v in values)
            {
                if (v == null) continue;
                v.AutoEllipsis = true;
                v.BackColor = Color.White;
                v.BorderStyle = BorderStyle.FixedSingle;
                v.Dock = DockStyle.Fill;
                v.Font = new Font("Consolas", 9F);
                v.ForeColor = valFore;
                v.Margin = new Padding(1);
                v.Padding = new Padding(0, 0, 6, 0);
                v.TextAlign = ContentAlignment.MiddleRight;
            }
        }
        // Keep Designer serialization declarative; register display resources after controls exist.
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this.lblHeader, "visionUi.visionRecipePage.lblVisionAlignKey.text");
            Lang.BindKey(this.lblCameraInfo, "visionUi.visionAlignPage.lblCameraInfo.text");
            Lang.BindKey(this.lblLive, "visionUi.visionAlignPage.lblLive.text");
            Lang.BindKey(this.grpAction, "visionUi.visionLinkPage._actionGroup.text");
            Lang.BindKey(this.grpResult, "visionUi.tpuVisionTestControl.btnResult.state3");
            Lang.BindKey(this.btnAutoAlign, "visionUi.visionAlignPage.btnAutoAlign.text");
            Lang.BindKey(this.btnManualAlign, "visionUi.visionAlignPage.btnManualAlign.text");
            Lang.BindKey(this.btnFirstMark, "visionUi.visionAlignPage.btnFirstMark.text");
            Lang.BindKey(this.btnSecondMark, "visionUi.visionAlignPage.btnSecondMark.text");
            Lang.BindKey(this.btnThetaMatch, "visionUi.visionAlignPage.btnThetaMatch.text");
            Lang.BindKey(this.btnXyMatch, "visionUi.visionAlignPage.btnXyMatch.text");
            Lang.BindKey(this.btnSave, "visionUi.visionCameraScaleDialog.btnSave.text");
            Lang.BindKey(this.btnClose, "visionUi.visionCameraCalibrationDialog.btnClose.text");
            Lang.BindKey(this.lblDeltaXCaption, "visionUi.visionAlignPage.lblDeltaXCaption.text");
            Lang.BindFormat(this.lblDeltaXValue, "visionUi.literal", (object)("0.000"));
            Lang.BindKey(this.lblDeltaYCaption, "visionUi.visionAlignPage.lblDeltaYCaption.text");
            Lang.BindFormat(this.lblDeltaYValue, "visionUi.literal", (object)("0.000"));
            Lang.BindKey(this.lblDeltaThetaCaption, "visionUi.visionAlignPage.lblDeltaThetaCaption.text");
            Lang.BindKey(this.lblDeltaThetaValue, "visionUi.visionAlignPage.lblDeltaThetaValue.text");
            Lang.BindKey(this.lblScoreCaption, "visionUi.visionAlignPage.lblScoreCaption.text");
            Lang.BindFormat(this.lblScoreValue, "visionUi.literal", (object)("0.0"));
        }
    }

    public partial class WaferMapOpenPage : PageBase
    {
        public WaferMapOpenPage()
        {
            InitializeComponent();
            InitializeLanguageBindings();
            ApplyRuntimeUi();
            LoadSampleMapList();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("work.waferMapOpen");
            lblHeader.Tag = "i18n:work.waferMapOpen";
        }

        private void LoadSampleMapList()
        {
            lbMapFiles.Items.Clear();
            lbMapFiles.Items.Add("Y482CB1_2026-04-24.map");
            lbMapFiles.Items.Add("Y482CB0_2026-04-23.map");
            lbMapFiles.Items.Add("SAMPLE.map");
        }
        // Keep Designer serialization declarative; register display resources after controls exist.
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this.lblHeader, "visionUi.visionAlignPage.lblHeader.text");
            Lang.BindKey(this.grpList, "visionUi.visionAlignPage.grpList.text");
        }
    }
}
