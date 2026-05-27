using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>설정 - GENERAL: 언어/빈 배열파일/비전 매칭 에러.</summary>
    public class GeneralPage : PageBase
    {
        private ComboBox _cbLang;
        private ComboBox _cbBinArr;
        private ComboBox _cbVisionMatch;

        public GeneralPage()
        {
            BuildBody();
        }

        private void BuildBody()
        {
            // 상하 자연스러운 흐름을 위해 Dock=Top 컨트롤을 "역순"으로 추가한다.
            //   (WinForms Dock=Top 은 z-order 가 낮을수록 위에 배치 — 마지막에 추가한 것이 최상단.)
            //   순서를 명확히 하기 위해 Controls.Add 순서를 다음과 같이 한다:
            //     ① ajinGroup  (아래)
            //     ② body       (가운데)
            //     ③ header     (맨 위)

            var cfg = AppSettingsStore.Current;

            // ── AJINEXTEK 그룹 (Dock=Top, 고정 높이 110)
            var ajinGroup = new GroupBox
            {
                Dock     = DockStyle.Top,
                Height   = 110,
                Padding  = new Padding(8, 4, 8, 8),
                Text     = "AJINEXTEK (실보드)  — 변경 후 재시작 필요",
                Font     = UiTheme.SectionFont,
                Tag      = "level:Maintenance"
            };
            var cbAjin = new CheckBox
            {
                Location  = new Point(16, 28), AutoSize = true,
                Text      = "UseAjin (AXL.dll)",
                Checked   = cfg.UseAjin,
                Font      = UiTheme.ButtonFont
            };
            var lblIrq = new Label { Location = new Point(16, 60), AutoSize = true, Text = "IRQ NO.", Font = UiTheme.ButtonFont };
            var tbIrq  = new TextBox { Location = new Point(90, 56), Size = new Size(80, 26), Text = cfg.AjinIrqNo.ToString(), Font = UiTheme.ValueFont };
            cbAjin.CheckedChanged += (s, e) =>
            {
                AppSettingsStore.Current.UseAjin = cbAjin.Checked;
                AppSettingsStore.Save();
            };
            tbIrq.TextChanged += (s, e) =>
            {
                if (int.TryParse(tbIrq.Text, out var v))
                {
                    AppSettingsStore.Current.AjinIrqNo = v;
                    AppSettingsStore.Save();
                }
            };
            ajinGroup.Controls.Add(cbAjin);
            ajinGroup.Controls.Add(lblIrq);
            ajinGroup.Controls.Add(tbIrq);
            Controls.Add(ajinGroup);

            // ── 일반 설정 body (Dock=Top, Height=140)
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Top, ColumnCount = 2, RowCount = 3, Height = 140,
                Padding = new Padding(8), BackColor = UiTheme.MainBg
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            for (int i = 0; i < 3; i++) body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            body.Controls.Add(MakeLabel("set.gen.language"), 0, 0);
            _cbLang = new ComboBox
            {
                Dock          = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = UiTheme.ValueFont,
                Margin        = new Padding(2)
            };
            foreach (var code in Lang.Supported) _cbLang.Items.Add(code);
            _cbLang.SelectedItem = Lang.Current;
            _cbLang.SelectedIndexChanged += (s, e) =>
            {
                var code = (string)_cbLang.SelectedItem;
                Lang.SetLanguage(code);
                AppSettingsStore.Current.Language = code;
                AppSettingsStore.Save();
            };
            body.Controls.Add(_cbLang, 1, 0);

            body.Controls.Add(MakeLabel("set.gen.binArr"), 0, 1);
            _cbBinArr = MakeEnableDisable();
            _cbBinArr.SelectedIndex = cfg.BinArrayFile ? 0 : 1;
            _cbBinArr.SelectedIndexChanged += (s, e) =>
            {
                AppSettingsStore.Current.BinArrayFile = _cbBinArr.SelectedIndex == 0;
                AppSettingsStore.Save();
            };
            body.Controls.Add(_cbBinArr, 1, 1);

            body.Controls.Add(MakeLabel("set.gen.visionMatchErr"), 0, 2);
            _cbVisionMatch = MakeEnableDisable();
            _cbVisionMatch.SelectedIndex = cfg.VisionMatchError ? 0 : 1;
            _cbVisionMatch.SelectedIndexChanged += (s, e) =>
            {
                AppSettingsStore.Current.VisionMatchError = _cbVisionMatch.SelectedIndex == 0;
                AppSettingsStore.Save();
            };
            body.Controls.Add(_cbVisionMatch, 1, 2);
            Controls.Add(body);

            // ── 섹션 헤더 (마지막에 추가 → 최상단)
            Controls.Add(CreateSectionHeader("common.setting"));
        }

        private static Label MakeLabel(string i18nKey) => new Label
        {
            Dock      = DockStyle.Fill,
            Text      = Lang.T(i18nKey),
            Tag       = "i18n:" + i18nKey,
            BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0),
            ForeColor = Color.Black,
            Font      = new Font("맑은 고딕", 9F),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(6, 0, 0, 0),
            Margin    = new Padding(2)
        };

        private static ComboBox MakeEnableDisable()
        {
            var cb = new ComboBox
            {
                Dock          = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = UiTheme.ValueFont,
                Margin        = new Padding(2)
            };
            cb.Items.Add("ENABLE");
            cb.Items.Add("DISABLE");
            cb.SelectedIndex = 0;
            return cb;
        }
    }
}
