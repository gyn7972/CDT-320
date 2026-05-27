using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>
    /// 레시피 - INPUT/OUTPUT CASSETTE 공용 화면.
    /// 좌측 동작 버튼, 중앙 옵션/대기시간, 실린더 I/O, 우측 조그/속도 영역을 표시한다.
    /// </summary>
    public partial class CassetteRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private string _titleI18n = "INPUT/OUTPUT CASSETTE";

        public CassetteRecipePage() : this("INPUT/OUTPUT CASSETTE", null, null)
        {
        }

        public CassetteRecipePage(string titleI18n) : this(titleI18n, null, null)
        {
        }

        public CassetteRecipePage(string titleI18n, object unitConfig, string configSavePath)
        {
            InitializeComponent();
            ApplyRecipeTheme();

            _titleI18n = string.IsNullOrWhiteSpace(titleI18n) ? "INPUT/OUTPUT CASSETTE" : titleI18n;
            lblHeader.Text = _titleI18n;

            if (unitConfig != null)
                unitConfigGrid.BindConfig(unitConfig, configSavePath);
        }

        // TODO: 기능/이벤트 바인딩 로직 작성
        private void btnLoadingMove_Click(object sender, EventArgs e) { }
        private void btnUnloadingMove_Click(object sender, EventArgs e) { }
        private void btnReadyMove_Click(object sender, EventArgs e) { }
        private void btnSlotLoadingMove_Click(object sender, EventArgs e) { }
        private void btnSlotUnloadingMove_Click(object sender, EventArgs e) { }

        private void btnJogPlus_Click(object sender, EventArgs e) { }
        private void btnJogMinus_Click(object sender, EventArgs e) { }
        private void btnJogStop_Click(object sender, EventArgs e) { }
        private void trkSpeed_ValueChanged(object sender, EventArgs e)
        {
            lblSpeedValue.Text = $"{trkSpeed.Value} %";
        }

        private void ApplyRecipeTheme()
        {
            BackColor = UiTheme.MainBg;
            mainLayout.BackColor = UiTheme.MainBg;
            contentLayout.BackColor = UiTheme.MainBg;

            foreach (var panel in new[] { panelLeft, panelCenter, panelRight, actionSection, ioSection, optionRows, waitRows, jogSection, speedSection, ioSensor1Row, ioSensor2Row, ioProtrusionRow, ioMappingRow })
                panel.BackColor = UiTheme.OptionPanelBg;

            foreach (var label in new[] { lblHeader, lblActionTitle, lblIoTitle, lblWaitTitle, lblJogTitle, lblSpeedTitle })
            {
                label.BackColor = label == lblHeader ? UiTheme.StatusBarBg : UiTheme.OptionHeaderBg;
                label.ForeColor = label == lblHeader ? UiTheme.StatusBarFg : UiTheme.OptionHeaderFg;
                label.Font = UiTheme.SectionFont;
            }

            foreach (var button in new[] { btnLoadingMove, btnUnloadingMove, btnReadyMove, btnSlotLoadingMove, btnSlotUnloadingMove })
                button.Font = UiTheme.ButtonFont;

            foreach (var button in new[] { btnJogPlus, btnJogStop, btnJogMinus })
                button.Font = UiTheme.ButtonFont;

            ApplyLabelCellStyle();
        }

        private void ApplyLabelCellStyle()
        {
            var keyLabels = new[]
            {
                lblSensor1, lblSensor2, lblProtrusion, lblMapping,
                lblOptLoadingZKey, lblOptUnloadingZKey, lblOptReadyPosKey, lblOptMappingZKey,
                lblOptSlotPitchKey, lblOptCassetteGapKey, lblOptInchKey, lblOptStageKey,
                lblWaitKey, lblAxisName
            };

            foreach (var label in keyLabels)
            {
                label.BackColor = Color.Gainsboro;
                label.BorderStyle = BorderStyle.FixedSingle;
                label.Dock = DockStyle.Fill;
                label.Font = UiTheme.ButtonFont;
                label.Margin = Padding.Empty;
                label.Padding = new Padding(6, 0, 6, 0);
                label.TextAlign = ContentAlignment.MiddleLeft;
            }

            var valueLabels = new[]
            {
                lblOptLoadingZVal, lblOptUnloadingZVal, lblOptReadyPosVal, lblOptMappingZVal,
                lblOptSlotPitchVal, lblOptCassetteGapVal, lblOptInchVal, lblOptStageVal,
                lblWaitVal, lblSpeedHigh, lblSpeedMid, lblSpeedLow, lblSpeedValue
            };

            foreach (var label in valueLabels)
            {
                label.BackColor = UiTheme.ValueBoxBg;
                label.BorderStyle = BorderStyle.FixedSingle;
                label.Dock = DockStyle.Fill;
                label.Font = UiTheme.ValueFont;
                label.ForeColor = UiTheme.ValueBoxFg;
                label.Margin = Padding.Empty;
                label.Padding = new Padding(6, 0, 6, 0);
                label.TextAlign = ContentAlignment.MiddleRight;
            }
        }
    }
}

