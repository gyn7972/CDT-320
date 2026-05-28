using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Settings - General.</summary>
    public partial class GeneralPage : PageBase
    {
        public GeneralPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            LoadSettings();
            WireEvents();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("common.setting");
            lblHeader.Tag = "i18n:common.setting";
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;

            lblLanguage.Text = Lang.T("set.gen.language");
            lblLanguage.Tag = "i18n:set.gen.language";
            lblBinArray.Text = Lang.T("set.gen.binArr");
            lblBinArray.Tag = "i18n:set.gen.binArr";
            lblVisionMatch.Text = Lang.T("set.gen.visionMatchErr");
            lblVisionMatch.Tag = "i18n:set.gen.visionMatchErr";

            grpAjin.Tag = "level:Maintenance";
        }

        private void LoadSettings()
        {
            var cfg = AppSettingsStore.Current;

            _cbLang.Items.Clear();
            foreach (var code in Lang.Supported)
                _cbLang.Items.Add(code);
            _cbLang.SelectedItem = Lang.Current;

            ResetEnableDisableItems(_cbBinArr);
            ResetEnableDisableItems(_cbVisionMatch);

            _cbBinArr.SelectedIndex = cfg.BinArrayFile ? 0 : 1;
            _cbVisionMatch.SelectedIndex = cfg.VisionMatchError ? 0 : 1;
            _cbAjin.Checked = cfg.UseAjin;
            _tbIrq.Text = cfg.AjinIrqNo.ToString();
        }

        private void WireEvents()
        {
            _cbLang.SelectedIndexChanged += (s, e) =>
            {
                var code = _cbLang.SelectedItem as string;
                if (string.IsNullOrWhiteSpace(code)) return;
                Lang.SetLanguage(code);
                AppSettingsStore.Current.Language = code;
                AppSettingsStore.Save();
            };

            _cbBinArr.SelectedIndexChanged += (s, e) =>
            {
                AppSettingsStore.Current.BinArrayFile = _cbBinArr.SelectedIndex == 0;
                AppSettingsStore.Save();
            };

            _cbVisionMatch.SelectedIndexChanged += (s, e) =>
            {
                AppSettingsStore.Current.VisionMatchError = _cbVisionMatch.SelectedIndex == 0;
                AppSettingsStore.Save();
            };

            _cbAjin.CheckedChanged += (s, e) =>
            {
                AppSettingsStore.Current.UseAjin = _cbAjin.Checked;
                AppSettingsStore.Save();
            };

            _tbIrq.TextChanged += (s, e) =>
            {
                int value;
                if (!int.TryParse(_tbIrq.Text, out value)) return;
                AppSettingsStore.Current.AjinIrqNo = value;
                AppSettingsStore.Save();
            };
        }

        private static void ResetEnableDisableItems(ComboBox combo)
        {
            combo.Items.Clear();
            combo.Items.Add("ENABLE");
            combo.Items.Add("DISABLE");
        }
    }
}
