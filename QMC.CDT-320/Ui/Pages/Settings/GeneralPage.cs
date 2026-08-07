using System;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Sequencing;
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
            lblWaferCompleteRunMode.Text = "WAFER COMPLETE RUN MODE";
            lblUseVision.Text = "VISION USE";
            lblUseRealVisionInSimulation.Text = "REAL VISION IN SIMULATION";
            lblSkipRunReviewInSimulation.Text = "SKIP RUN REVIEW (SIM)";
            lblPickRuntimeOffset.Text = "PICK RUNTIME OFFSET";
            lblPlaceRuntimeOffset.Text = "PLACE RUNTIME OFFSET";

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
                ResetEnableDisableItems(_cbUseRealVisionInSimulation);
                ResetEnableDisableItems(_cbSkipRunReviewInSimulation);
                ResetEnableDisableItems(_cbPickRuntimeOffset);
                ResetEnableDisableItems(_cbPlaceRuntimeOffset);
                _cbWaferCompleteRunMode.Items.Clear();
                _cbWaferCompleteRunMode.Items.Add("CONTINUE");
                _cbWaferCompleteRunMode.Items.Add("STOP AFTER DRAIN");

                _cbBinArr.SelectedIndex = cfg.BinArrayFile ? 0 : 1;
                _cbVisionMatch.SelectedIndex = cfg.VisionMatchError ? 0 : 1;
                _cbSimulationMode.SelectedIndex = cfg.SimulationMode ? 0 : 1;
                _cbDryRunMode.SelectedIndex = cfg.DryRunMode ? 0 : 1;
                _cbDeveloperMode.SelectedIndex = cfg.DeveloperMode ? 0 : 1;
                _cbPickerMotionOnlyTestMode.SelectedIndex = cfg.PickerMotionOnlyTestMode ? 0 : 1;
                _cbUseVision.SelectedIndex = cfg.UseVision ? 0 : 1;
                _cbUseRealVisionInSimulation.SelectedIndex = cfg.UseRealVisionInSimulation ? 0 : 1;
                _cbSkipRunReviewInSimulation.SelectedIndex = cfg.SkipInputStageRunReviewInSimulation ? 0 : 1;
                _cbWaferCompleteRunMode.SelectedIndex = cfg.WaferCompleteRunMode == WaferCompleteRunMode.StopAfterDrain
                    ? 1
                    : 0;

                _cbAjin.Checked = cfg.UseAjin;
                _tbIrq.Text = cfg.AjinIrqNo.ToString();

                // 런타임 보정 사용 유무는 AppSettings가 아니라 각 보정 스토어(JSON)에 저장된다.
                _cbPickRuntimeOffset.SelectedIndex = PickRuntimeOffsetService.IsEnabled ? 0 : 1;
                _cbPlaceRuntimeOffset.SelectedIndex = PlaceRuntimeOffsetService.IsEnabled ? 0 : 1;
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

        private bool RejectRuntimeModeChangeWhileRunning(string settingName)
        {
            Form1 host = FindForm() as Form1;
            MachineController controller = host != null ? host.Controller : null;
            if (controller == null)
                return false;

            EquipmentStatus status = controller.Status;
            bool running = controller.IsSequenceRunning ||
                           controller.IsManualBusy ||
                           status == EquipmentStatus.AutoRunning ||
                           status == EquipmentStatus.ManualRunning ||
                           status == EquipmentStatus.Initializing;
            if (!running)
                return false;

            LoadSettings();
            QMC.Common.MessageDialog.Show(
                "장비 동작 중에는 " + (settingName ?? "운전 모드") +
                " 설정을 변경할 수 없습니다. 동작을 정지한 뒤 다시 시도하십시오.");
            return true;
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
            if (RejectRuntimeModeChangeWhileRunning("SIMULATION MODE")) return;
            AppSettingsStore.Current.SimulationMode = _cbSimulationMode.SelectedIndex == 0;
            AppSettingsStore.Save();
            ApplyRuntimeModeToHost();
        }

        private void _cbDryRunMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            if (RejectRuntimeModeChangeWhileRunning("DRY RUN MODE")) return;
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

        private void _cbWaferCompleteRunMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;

            AppSettingsStore.Current.WaferCompleteRunMode = _cbWaferCompleteRunMode.SelectedIndex == 1
                ? WaferCompleteRunMode.StopAfterDrain
                : WaferCompleteRunMode.Continue;
            AppSettingsStore.Save();
        }

        private void _cbUseVision_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            if (RejectRuntimeModeChangeWhileRunning("VISION USE")) return;
            bool use = _cbUseVision.SelectedIndex == 0;
            AppSettingsStore.Current.UseVision = use;
            AppSettingsStore.Save();
            // 비전 미사용 → 기존 연결을 끊어 미연결 상태로 동작(자동 시퀀스는 바이패스로 통과).
            if (!use)
                QMC.CDT320.VisionComm.VisionHub.DisconnectAll();
        }

        private void _cbUseRealVisionInSimulation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            if (RejectRuntimeModeChangeWhileRunning("REAL VISION IN SIMULATION")) return;
            AppSettingsStore.Current.UseRealVisionInSimulation = _cbUseRealVisionInSimulation.SelectedIndex == 0;
            AppSettingsStore.Save();
        }

        // 시뮬레이션에서만 적용되는 옵션이므로 운전 중 변경을 막지 않는다(실장비 동작에 영향 없음).
        private void _cbSkipRunReviewInSimulation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            AppSettingsStore.Current.SkipInputStageRunReviewInSimulation = _cbSkipRunReviewInSimulation.SelectedIndex == 0;
            AppSettingsStore.Save();
        }

        private void _cbAjin_CheckedChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            if (RejectRuntimeModeChangeWhileRunning("AJIN USE")) return;
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

        private void _cbPickRuntimeOffset_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            PickRuntimeOffsetService.SetEnabled(_cbPickRuntimeOffset.SelectedIndex == 0);
        }

        private void _cbPlaceRuntimeOffset_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            PlaceRuntimeOffsetService.SetEnabled(_cbPlaceRuntimeOffset.SelectedIndex == 0);
        }

        private void btnResetPickRuntimeOffset_Click(object sender, EventArgs e)
        {
            DialogResult answer = QMC.Common.MessageDialog.Show(
                "Pick 런타임 보정 필터(8세트 X/Y/T)를 모두 0으로 초기화합니다.\n계속하시겠습니까?",
                "PICK RUNTIME OFFSET RESET",
                MessageBoxButtons.YesNo);
            if (answer != DialogResult.Yes)
                return;

            PickRuntimeOffsetService.ResetAll();
            QMC.Common.MessageDialog.Show("Pick 런타임 보정 필터를 초기화했습니다.");
        }

        private void btnResetPlaceRuntimeOffset_Click(object sender, EventArgs e)
        {
            DialogResult answer = QMC.Common.MessageDialog.Show(
                "Place 런타임 보정 필터(8세트 X/Y/T)를 모두 0으로 초기화합니다.\n계속하시겠습니까?",
                "PLACE RUNTIME OFFSET RESET",
                MessageBoxButtons.YesNo);
            if (answer != DialogResult.Yes)
                return;

            PlaceRuntimeOffsetService.ResetAll();
            QMC.Common.MessageDialog.Show("Place 런타임 보정 필터를 초기화했습니다.");
        }

        private void btnRuntimeOffsetMonitor_Click(object sender, EventArgs e)
        {
            using (var dlg = new QMC.CDT_320.Ui.Dialogs.RuntimeOffsetMonitorDialog())
                dlg.ShowDialog(FindForm());
        }
    }
}
