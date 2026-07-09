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
        public GeneralPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyGeneralLayout();
            LoadSettings();
            WireEvents();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.general");
            lblHeader.Tag = "i18n:set.general";
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

            grpAjin.Tag = "level:Maintenance";
        }

        private void ApplyGeneralLayout()
        {
            rootLayout.SuspendLayout();
            bodyLayout.SuspendLayout();
            logBtnLayout.SuspendLayout();
            try
            {
                rootLayout.Padding = Padding.Empty;
                rootLayout.Margin = Padding.Empty;
                lblHeader.Margin = Padding.Empty;

                rootLayout.ColumnCount = 2;
                rootLayout.ColumnStyles.Clear();
                rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                rootLayout.SetColumnSpan(lblHeader, 2);

                rootLayout.RowCount = 5;
                while (rootLayout.RowStyles.Count < 5)
                    rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                rootLayout.RowStyles[0].Height = 30F;
                rootLayout.RowStyles[1].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[1].Height = 304F;
                rootLayout.RowStyles[2].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[2].Height = 78F;
                rootLayout.RowStyles[3].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[3].Height = 40F;
                rootLayout.RowStyles[4].SizeType = SizeType.Percent;
                rootLayout.RowStyles[4].Height = 100F;

                bodyLayout.Margin = Padding.Empty;
                bodyLayout.Padding = Padding.Empty;
                bodyLayout.Dock = DockStyle.Fill;
                bodyLayout.ColumnStyles[1].SizeType = SizeType.Percent;
                bodyLayout.ColumnStyles[1].Width = 100F;
                SetBodyRowsHeight(34F);

                logBtnLayout.Margin = Padding.Empty;
                logBtnLayout.Padding = new Padding(0, 3, 0, 0);
                logBtnLayout.Dock = DockStyle.Fill;
                ConfigureLogButtonLayout();

                rootLayout.Controls.Remove(bodyLayout);
                rootLayout.Controls.Remove(logBtnLayout);

                var grpSetting = new GroupBox
                {
                    BackColor = System.Drawing.Color.White,
                    Dock = DockStyle.Fill,
                    Font = UiTheme.SectionFont,
                    ForeColor = System.Drawing.Color.FromArgb(35, 45, 57),
                    Margin = new Padding(0, 0, 0, 1),
                    Name = "grpSetting",
                    Padding = new Padding(1, 10, 1, 2),
                    TabIndex = 1,
                    TabStop = false,
                    Text = "SETTING"
                };

                var settingLayout = new TableLayoutPanel
                {
                    ColumnCount = 1,
                    Dock = DockStyle.Fill,
                    Margin = Padding.Empty,
                    Name = "settingLayout",
                    Padding = Padding.Empty,
                    RowCount = 1
                };
                settingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                settingLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 272F));

                settingLayout.Controls.Add(bodyLayout, 0, 0);
                grpSetting.Controls.Add(settingLayout);

                rootLayout.Controls.Add(grpSetting, 0, 1);
                rootLayout.SetColumn(grpSetting, 0);
                rootLayout.SetRow(grpAjin, 2);
                rootLayout.SetColumn(grpAjin, 0);
                grpAjin.Margin = new Padding(0, 0, 0, 2);
                grpAjin.Padding = new Padding(1, 10, 1, 2);
                rootLayout.Controls.Add(logBtnLayout, 0, 3);
                rootLayout.SetRow(logBtnLayout, 3);
                rootLayout.SetColumn(logBtnLayout, 0);
            }
            finally
            {
                logBtnLayout.ResumeLayout(false);
                bodyLayout.ResumeLayout(false);
                rootLayout.ResumeLayout(false);
            }
        }

        private void ConfigureLogButtonLayout()
        {
            logBtnLayout.ColumnCount = 2;
            logBtnLayout.ColumnStyles.Clear();
            logBtnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            logBtnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            logBtnLayout.RowCount = 1;
            logBtnLayout.RowStyles.Clear();
            logBtnLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            logBtnLayout.Controls.Remove(btnLogSettings);
            btnLogSettings.Dock = DockStyle.Fill;
            btnLogSettings.Margin = Padding.Empty;
            logBtnLayout.Controls.Add(btnLogSettings, 1, 0);
            logBtnLayout.SetColumnSpan(btnLogSettings, 1);
        }

        private void SetBodyRowsHeight(float height)
        {
            for (int i = 0; i < bodyLayout.RowStyles.Count && i < 8; i++)
            {
                bodyLayout.RowStyles[i].SizeType = SizeType.Absolute;
                bodyLayout.RowStyles[i].Height = height;
            }
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

            btnLogSettings.Click += (s, e) =>
            {
                using (var dlg = new QMC.CDT_320.Ui.Dialogs.LogSettingsDialog())
                    dlg.ShowDialog(FindForm());
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
