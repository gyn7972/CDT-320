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
        // 압축 보관본(Log\Archive) 보존일수 선택지 — 0 은 무기한 보관(OFF).
        // (원본 로그는 14일 고정 유예 후 자동 압축 — LogRetentionService.RawKeepDays)
        private static readonly int[] ArchiveKeepChoices = { 0, 90, 180, 365 };

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
            lblSimulationMode.Text = "SIMULATION MODE";
            lblDryRunMode.Text = "DRY RUN MODE";
            lblDeveloperMode.Text = "DEVELOPER MODE";
            lblPickerMotionOnlyTestMode.Text = "PICKER MOTION ONLY TEST";
            lblUseVision.Text = "VISION USE";
            lblFileLogHistory.Text = "LOG HISTORY VIEW";
            lblArchiveKeep.Text = "ARCHIVE KEEP DAYS";

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
            ResetEnableDisableItems(_cbSimulationMode);
            ResetEnableDisableItems(_cbDryRunMode);
            ResetEnableDisableItems(_cbDeveloperMode);
            ResetEnableDisableItems(_cbPickerMotionOnlyTestMode);
            ResetEnableDisableItems(_cbUseVision);
            ResetEnableDisableItems(_cbFileLogHistory);

            _cbBinArr.SelectedIndex = cfg.BinArrayFile ? 0 : 1;
            _cbVisionMatch.SelectedIndex = cfg.VisionMatchError ? 0 : 1;
            _cbSimulationMode.SelectedIndex = cfg.SimulationMode ? 0 : 1;
            _cbDryRunMode.SelectedIndex = cfg.DryRunMode ? 0 : 1;
            _cbDeveloperMode.SelectedIndex = cfg.DeveloperMode ? 0 : 1;
            _cbPickerMotionOnlyTestMode.SelectedIndex = cfg.PickerMotionOnlyTestMode ? 0 : 1;
            _cbUseVision.SelectedIndex = cfg.UseVision ? 0 : 1;
            _cbFileLogHistory.SelectedIndex = cfg.FileLogHistoryEnabled ? 0 : 1;

            // 압축 보관본 보존일수 — 고정 선택지(OFF/90/180/365). 목록에 없는 값이면 OFF(무기한)로 본다.
            _cbArchiveKeep.Items.Clear();
            foreach (int days in ArchiveKeepChoices)
                _cbArchiveKeep.Items.Add(days == 0 ? "OFF" : days + " DAYS");
            int idx = Array.IndexOf(ArchiveKeepChoices, cfg.ArchiveKeepDays);
            _cbArchiveKeep.SelectedIndex = idx >= 0 ? idx : 0;

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

            _cbSimulationMode.SelectedIndexChanged += (s, e) =>
            {
                AppSettingsStore.Current.SimulationMode = _cbSimulationMode.SelectedIndex == 0;
                AppSettingsStore.Save();
                ApplyRuntimeModeToHost();
            };

            _cbDryRunMode.SelectedIndexChanged += (s, e) =>
            {
                AppSettingsStore.Current.DryRunMode = _cbDryRunMode.SelectedIndex == 0;
                AppSettingsStore.Save();
                ApplyRuntimeModeToHost();
            };

            _cbDeveloperMode.SelectedIndexChanged += (s, e) =>
            {
                AppSettingsStore.Current.DeveloperMode = _cbDeveloperMode.SelectedIndex == 0;
                AppSettingsStore.Save();
            };

            _cbPickerMotionOnlyTestMode.SelectedIndexChanged += (s, e) =>
            {
                AppSettingsStore.Current.PickerMotionOnlyTestMode = _cbPickerMotionOnlyTestMode.SelectedIndex == 0;
                AppSettingsStore.Save();
            };

            _cbUseVision.SelectedIndexChanged += (s, e) =>
            {
                bool use = _cbUseVision.SelectedIndex == 0;
                AppSettingsStore.Current.UseVision = use;
                AppSettingsStore.Save();
                // 비전 미사용 → 기존 연결을 끊어 미연결 상태로 동작(자동 시퀀스는 바이패스로 통과).
                if (!use)
                    QMC.CDT320.VisionComm.VisionHub.DisconnectAll();
            };

            _cbFileLogHistory.SelectedIndexChanged += (s, e) =>
            {
                // 이력 탭 로그 화면 사용 여부 — 저장 즉시 반영되며, 이력 페이지를 다시 방문하는 순간 적용된다.
                AppSettingsStore.Current.FileLogHistoryEnabled = _cbFileLogHistory.SelectedIndex == 0;
                AppSettingsStore.Save();
            };

            _cbArchiveKeep.SelectedIndexChanged += (s, e) =>
            {
                // 압축 보관본 보존일수 — 다음 정리 주기(시작 30초 후 1회 + 24시간마다)부터 적용된다.
                int i = _cbArchiveKeep.SelectedIndex;
                if (i < 0 || i >= ArchiveKeepChoices.Length) return;
                AppSettingsStore.Current.ArchiveKeepDays = ArchiveKeepChoices[i];
                AppSettingsStore.Save();
            };

            _cbAjin.CheckedChanged += (s, e) =>
            {
                AppSettingsStore.Current.UseAjin = _cbAjin.Checked;
                AppSettingsStore.Save();
                ApplyRuntimeModeToHost();
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

        private void ApplyRuntimeModeToHost()
        {
            var host = FindForm() as Form1;
            if (host != null)
                host.ApplyRuntimeMode();
        }
    }
}
