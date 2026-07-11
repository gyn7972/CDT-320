using System;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT_320;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Settings - General.</summary>
    public partial class GeneralPage : PageBase
    {
        private bool _loadingSettings;

        public GeneralPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyGeneralLayout();
            LoadSettings();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.general");
            lblHeader.Tag = "i18n:set.general";

            lblLanguage.Text = Lang.T("set.gen.language");
            lblLanguage.Tag = "i18n:set.gen.language";
            lblBinArray.Text = Lang.T("set.gen.binArr");
            lblBinArray.Tag = "i18n:set.gen.binArr";
            lblVisionMatch.Text = Lang.T("set.gen.visionMatchErr");
            lblVisionMatch.Tag = "i18n:set.gen.visionMatchErr";
            lblSimulationMode.Text = "SIMULATION MODE";
            lblDryRunMode.Text = "DRY RUN MODE";
            lblDeveloperMode.Text = "DEVELOPER MODE";
            lblPickerMotionOnlyTestMode.Text = "PICKER MOTION ONLY TEST";
            lblUseVision.Text = "VISION USE";

            grpAjin.Tag = "level:Maintenance";
        }

        private void ApplyGeneralLayout()
        {
            grpSetting.ForeColor = System.Drawing.Color.FromArgb(35, 45, 57);
            grpSetting.Padding = new Padding(1, 10, 1, 2);
            grpAjin.Margin = new Padding(0, 0, 0, 2);
            grpAjin.Padding = new Padding(1, 10, 1, 2);
            logBtnLayout.Padding = new Padding(0, 3, 0, 0);
        }

        private void LoadSettings()
        {
            _loadingSettings = true;
            var cfg = AppSettingsStore.Current;

            try
            {
                _cbLang.Items.Clear();
                foreach (var code in Lang.Supported)
                    _cbLang.Items.Add(code);
                _cbLang.SelectedItem = Lang.Current;

                ResetEnableDisableItems(_cbBinArr);
                ResetEnableDisableItems(_cbVisionMatch);
                ResetEnableDisableItems(_cbSimulationMode);
                ResetEnableDisableItems(_cbDryRunMode);
                ResetEnableDisableItems(_cbDeveloperMode);
                ResetEnableDisableItems(_cbPickerMotionOnlyTestMode);
                ResetEnableDisableItems(_cbUseVision);

                _cbBinArr.SelectedIndex = cfg.BinArrayFile ? 0 : 1;
                _cbVisionMatch.SelectedIndex = cfg.VisionMatchError ? 0 : 1;
                _cbSimulationMode.SelectedIndex = cfg.SimulationMode ? 0 : 1;
                _cbDryRunMode.SelectedIndex = cfg.DryRunMode ? 0 : 1;
                _cbDeveloperMode.SelectedIndex = cfg.DeveloperMode ? 0 : 1;
                _cbPickerMotionOnlyTestMode.SelectedIndex = cfg.PickerMotionOnlyTestMode ? 0 : 1;
                _cbUseVision.SelectedIndex = cfg.UseVision ? 0 : 1;

                _cbAjin.Checked = cfg.UseAjin;
                _tbIrq.Text = cfg.AjinIrqNo.ToString();
            }
            finally
            {
                _loadingSettings = false;
            }
        }

        private static void ResetEnableDisableItems(ComboBox combo)
        {
            combo.Items.Clear();
            combo.Items.Add("ENABLE");
            combo.Items.Add("DISABLE");
        }

        private void ApplyRuntimeModeToHost()
        {
            var host = FindForm() as Form1;
            if (host != null)
                host.ApplyRuntimeMode();
        }

        private void _cbLang_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            var code = _cbLang.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(code)) return;
            Lang.SetLanguage(code);
            AppSettingsStore.Current.Language = code;
            AppSettingsStore.Save();
        }

        private void _cbBinArr_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            AppSettingsStore.Current.BinArrayFile = _cbBinArr.SelectedIndex == 0;
            AppSettingsStore.Save();
        }

        private void _cbVisionMatch_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            AppSettingsStore.Current.VisionMatchError = _cbVisionMatch.SelectedIndex == 0;
            AppSettingsStore.Save();
        }

        private void _cbSimulationMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            AppSettingsStore.Current.SimulationMode = _cbSimulationMode.SelectedIndex == 0;
            AppSettingsStore.Save();
            ApplyRuntimeModeToHost();
        }

        private void _cbDryRunMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            AppSettingsStore.Current.DryRunMode = _cbDryRunMode.SelectedIndex == 0;
            AppSettingsStore.Save();
            ApplyRuntimeModeToHost();
        }

        private void _cbDeveloperMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            AppSettingsStore.Current.DeveloperMode = _cbDeveloperMode.SelectedIndex == 0;
            AppSettingsStore.Save();
        }

        private void _cbPickerMotionOnlyTestMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            AppSettingsStore.Current.PickerMotionOnlyTestMode = _cbPickerMotionOnlyTestMode.SelectedIndex == 0;
            AppSettingsStore.Save();
        }

        private void _cbUseVision_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            bool use = _cbUseVision.SelectedIndex == 0;
            AppSettingsStore.Current.UseVision = use;
            AppSettingsStore.Save();
            // 비전 미사용 → 기존 연결을 끊어 미연결 상태로 동작(자동 시퀀스는 바이패스로 통과).
            if (!use)
                QMC.CDT320.VisionComm.VisionHub.DisconnectAll();
        }

        private void _cbAjin_CheckedChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            AppSettingsStore.Current.UseAjin = _cbAjin.Checked;
            AppSettingsStore.Save();
            ApplyRuntimeModeToHost();
        }

        private void _tbIrq_TextChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            int value;
            if (!int.TryParse(_tbIrq.Text, out value)) return;
            AppSettingsStore.Current.AjinIrqNo = value;
            AppSettingsStore.Save();
        }

        private void btnLogSettings_Click(object sender, EventArgs e)
        {
            using (var dlg = new QMC.CDT_320.Ui.Dialogs.LogSettingsDialog())
                dlg.ShowDialog(FindForm());
        }
    }
}
