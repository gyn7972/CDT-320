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
            lblUseOutputGoodPickupCap.Text = "GOOD OUTPUT PICKUP CAP";
            lblUseVision.Text = "VISION USE";
            lblUseRealVisionInSimulation.Text = "REAL VISION IN SIMULATION";
            lblSkipRunReviewInSimulation.Text = "SKIP RUN REVIEW (SIM)";
            lblPickRuntimeOffset.Text = "PICK RUNTIME OFFSET";
            lblPlaceRuntimeOffset.Text = "PLACE RUNTIME OFFSET";
            lblPickerZRuntimeOffset.Text = "PICKER Z RUNTIME OFFSET";

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
                ResetEnableDisableItems(_cbUseOutputGoodPickupCap);
                ResetEnableDisableItems(_cbPickRuntimeOffset);
                ResetEnableDisableItems(_cbPlaceRuntimeOffset);
                ResetEnableDisableItems(_cbPickerZRuntimeOffset);
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
                _cbUseOutputGoodPickupCap.SelectedIndex = cfg.UseOutputGoodPickupCap ? 0 : 1;

                _cbAjin.Checked = cfg.UseAjin;
                _tbIrq.Text = cfg.AjinIrqNo.ToString();

                // [P3 2026-08-22] LOT 웨이퍼맵 네트워크 폴더(UNC). 비어 있으면 수신 기능 전체 꺼짐.
                _tbNetworkWaferMapFolder.Text = cfg.NetworkWaferMapFolder ?? "";
                // [P4 2026-08-22] LOT 네트워크 맵을 실제 다이맵으로 사용(시퀀스 적용) 스위치.
                _cbUseLotNetworkWaferMap.Checked = cfg.UseLotNetworkWaferMap;
                // [캠택맵 2026-08-27] 네트워크 맵 파일 포맷 — 명시 선택(자동 판별 없음). 기본 Rad.
                _cbNetworkWaferMapFormat.Items.Clear();
                _cbNetworkWaferMapFormat.Items.Add("RAD TXT (X= Y= B=)");
                _cbNetworkWaferMapFormat.Items.Add("CAMTEK (RowData)");
                _cbNetworkWaferMapFormat.SelectedIndex =
                    string.Equals((cfg.NetworkWaferMapFormat ?? "").Trim(), "Camtek",
                        StringComparison.OrdinalIgnoreCase) ? 1 : 0;

                // [비전 작업자 확인 2026-08-25] NG 라우팅 로트에서 비전 NG RESULT 작업자 확인 대기 상한(초).
                _tbVisionOperatorConfirmTimeoutSec.Text = cfg.VisionOperatorConfirmTimeoutSec.ToString();
                // [NG 라우팅 시뮬 테스트 2026-08-25] 시뮬 최종 판정 NG 주입 확률(%). 0=꺼짐.
                _tbSimulationVisionNgRatePercent.Text = cfg.SimulationVisionNgRatePercent.ToString();

                // 런타임 보정 사용 유무는 AppSettings가 아니라 각 보정 스토어(JSON)에 저장된다.
                _cbPickRuntimeOffset.SelectedIndex = PickRuntimeOffsetService.IsEnabled ? 0 : 1;
                _cbPlaceRuntimeOffset.SelectedIndex = PlaceRuntimeOffsetService.IsEnabled ? 0 : 1;
                _cbPickerZRuntimeOffset.SelectedIndex = PickerZRuntimeOffsetService.IsEnabled ? 0 : 1;
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

        // [P4 2026-08-22] LOT 네트워크 맵 사용 스위치 — 즉시 저장.
        private void _cbUseLotNetworkWaferMap_CheckedChanged(object sender, EventArgs e)
        {
            if (_loadingSettings)
                return;

            AppSettingsStore.Current.UseLotNetworkWaferMap = _cbUseLotNetworkWaferMap.Checked;
            AppSettingsStore.Save();

            if (!_cbUseLotNetworkWaferMap.Checked)
            {
                ResetPickupBinSelectionOnLotMapModeOff("UseLotNetworkWaferMap=OFF");
            }
            // [P5 2026-08-24] 맵 파일명=바코드(1:1)라 이 모드는 인풋 바코드 판독이 필수다.
            // 저장은 허용하되(설정 순서 자유) 바코드가 꺼져 있으면 즉시 안내한다 —
            // 강제는 런타임 알람(LOT-MAP-BARCODE-REQUIRED)이 담당한다.
            else if (!AppSettingsStore.Current.UseInputWaferBarcode)
            {
                QMC.Common.MessageDialog.Show(this,
                    "LOT 네트워크 웨이퍼맵 모드는 웨이퍼 바코드 판독이 필수입니다(맵 파일명 = 바코드).\r\n" +
                    "현재 INPUT WAFER 바코드가 꺼져 있습니다 — 설정 → BARCODE에서 'USE BARCODE'를 켜세요.\r\n" +
                    "켜지 않으면 Auto 진행 시 맵 적용 단계에서 알람 정지합니다.",
                    "LOT 웨이퍼맵", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // [캠택맵 2026-08-27] 네트워크 웨이퍼맵 포맷 — 변경 즉시 저장(USE 스위치 저장 패턴 미러) + 변경 로그 1줄.
        private void _cbNetworkWaferMapFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings)
                return;

            string value = _cbNetworkWaferMapFormat.SelectedIndex == 1 ? "Camtek" : "Rad";
            string old = AppSettingsStore.Current.NetworkWaferMapFormat ?? "";
            if (string.Equals(old, value, StringComparison.OrdinalIgnoreCase))
                return;

            AppSettingsStore.Current.NetworkWaferMapFormat = value;
            AppSettingsStore.Save();
            QMC.Common.Log.Write("Main", "SYSTEM", "GeneralPage",
                "네트워크 웨이퍼맵 포맷을 변경했습니다. " +
                (string.IsNullOrWhiteSpace(old) ? "(기본 Rad)" : old) + " → " + value + " - Ok");
        }

        // [P3 2026-08-22] 네트워크 웨이퍼맵 폴더 — 포커스 이탈 시 저장(기존 즉시 저장 컨벤션 미러).
        private void _tbNetworkWaferMapFolder_Leave(object sender, EventArgs e)
        {
            if (_loadingSettings)
                return;

            string value = (_tbNetworkWaferMapFolder.Text ?? "").Trim();
            if (string.Equals(AppSettingsStore.Current.NetworkWaferMapFolder ?? "", value, StringComparison.Ordinal))
                return;

            AppSettingsStore.Current.NetworkWaferMapFolder = value;
            AppSettingsStore.Save();

            if (string.IsNullOrWhiteSpace(value))
                ResetPickupBinSelectionOnLotMapModeOff("NetworkWaferMapFolder=EMPTY");
        }

        // [2차 검토수정 2026-08-23] LOT 네트워크 맵 모드가 꺼지는 전환에서 BIN 선택을 All로 복귀.
        // 남겨두면 필터가 상태 파일에 보이지 않게 잔존하다가 몇 주 뒤 모드를 다시 켠 순간
        // 이전 LOT의 Selected 필터가 무경고로 되살아나 다이를 조용히 걸러낸다.
        // Reset은 다음 웨이퍼의 맵 적용부터 반영되는 확장(All) 방향 변경이라 진행 중 시퀀스에 안전하다.
        private void ResetPickupBinSelectionOnLotMapModeOff(string trigger)
        {
            try
            {
                QMC.CDT320.Materials.MaterialStateService.ResetPickupBinSelectionToAll(
                    "LotNetworkMapModeOff:" + trigger);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "GeneralPage",
                    "LOT 맵 모드 해제 시 BIN 선택 초기화 실패: " + ex.Message + " - Fail");
            }
        }

        // [P3 2026-08-22] [연결 확인]: 폴더 존재+목록 조회를 타임아웃 안에 시도하고 결과를 알린다.
        // UNC 단절 시 UI가 얼지 않도록 백그라운드에서 검사한다.
        private async void btnNetworkWaferMapCheck_Click(object sender, EventArgs e)
        {
            _tbNetworkWaferMapFolder_Leave(sender, e);

            btnNetworkWaferMapCheck.Enabled = false;
            try
            {
                string detail = "";
                bool ok = await System.Threading.Tasks.Task.Run(() =>
                {
                    string checkDetail;
                    bool result = QMC.CDT320.Lots.LotWaferMapFetchService.TryCheckFolderAccessible(out checkDetail);
                    detail = checkDetail;
                    return result;
                });

                QMC.Common.MessageDialog.Show(this,
                    (ok ? "네트워크 웨이퍼맵 폴더에 접근할 수 있습니다.\r\n" : "네트워크 웨이퍼맵 폴더에 접근할 수 없습니다.\r\n") + detail,
                    "연결 확인", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "연결 확인 실패: " + ex.Message, "연결 확인",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnNetworkWaferMapCheck.Enabled = true;
            }
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

        private void _cbUseOutputGoodPickupCap_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;

            bool enabled = _cbUseOutputGoodPickupCap.SelectedIndex == 0;
            AppSettingsStore.Current.UseOutputGoodPickupCap = enabled;
            AppSettingsStore.Save();
            QMC.Common.Log.Write("Main", "SYSTEM", "GeneralPage",
                "GOOD 배출 픽업 캡 설정을 변경했습니다. enabled=" + enabled + " - Ok");
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

        // [비전 작업자 확인 2026-08-25] NG 라우팅 로트에서 비전 NG RESULT 작업자 확인 대기 상한(초).
        // 포커스 이탈 시 12~600초로 클램프해 저장한다. 잘못된 입력은 저장값으로 되돌린다.
        private void _tbVisionOperatorConfirmTimeoutSec_Leave(object sender, EventArgs e)
        {
            if (_loadingSettings)
                return;

            int value;
            if (!int.TryParse((_tbVisionOperatorConfirmTimeoutSec.Text ?? "").Trim(), out value))
            {
                _tbVisionOperatorConfirmTimeoutSec.Text =
                    AppSettingsStore.Current.VisionOperatorConfirmTimeoutSec.ToString();
                return;
            }

            value = Math.Max(12, Math.Min(600, value));
            _tbVisionOperatorConfirmTimeoutSec.Text = value.ToString();
            if (AppSettingsStore.Current.VisionOperatorConfirmTimeoutSec == value)
                return;

            AppSettingsStore.Current.VisionOperatorConfirmTimeoutSec = value;
            AppSettingsStore.Save();
        }

        // [NG 라우팅 시뮬 테스트 2026-08-25] 시뮬 최종 판정 NG 주입 확률(%). 0~100 클램프,
        // 잘못된 입력은 저장값으로 되돌린다. 실비전 경로에는 영향 없음(바이패스 분기 전용).
        private void _tbSimulationVisionNgRatePercent_Leave(object sender, EventArgs e)
        {
            if (_loadingSettings)
                return;

            int value;
            if (!int.TryParse((_tbSimulationVisionNgRatePercent.Text ?? "").Trim(), out value))
            {
                _tbSimulationVisionNgRatePercent.Text =
                    AppSettingsStore.Current.SimulationVisionNgRatePercent.ToString();
                return;
            }

            value = Math.Max(0, Math.Min(100, value));
            _tbSimulationVisionNgRatePercent.Text = value.ToString();
            if (AppSettingsStore.Current.SimulationVisionNgRatePercent == value)
                return;

            AppSettingsStore.Current.SimulationVisionNgRatePercent = value;
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

        // 기본 OFF — 실장비 방향 검증(클램프 축소 후 수렴 확인) 뒤에만 ON한다(지시 2026-08-14).
        private void _cbPickerZRuntimeOffset_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            PickerZRuntimeOffsetService.SetEnabled(_cbPickerZRuntimeOffset.SelectedIndex == 0);
        }

        // PICKER Z(BottomZ) 런타임 보정 리셋(2026-08-19 팀장님 지시) — PICK/PLACE 리셋과 동일 플로우.
        private void btnResetPickerZRuntimeOffset_Click(object sender, EventArgs e)
        {
            QMC.CDT_320.Ui.Dialogs.RuntimeOffsetMonitorDialog.ResetPickerZRuntimeOffsetsWithConfirm(
                FindForm() as Form1);
        }

        // 런타임 보정 필터 리셋은 모니터 다이얼로그와 동일 플로우(운전 중 금지 게이트 +
        // 리셋 전 학습값 요약 확인 + 리셋 전 값·실행자 로그)를 공유한다 — 2026-08-16 팀장님 통일 지시.
        private void btnResetPickRuntimeOffset_Click(object sender, EventArgs e)
        {
            QMC.CDT_320.Ui.Dialogs.RuntimeOffsetMonitorDialog.ResetRuntimeOffsetsWithConfirm(
                FindForm() as Form1, false);
        }

        private void btnResetPlaceRuntimeOffset_Click(object sender, EventArgs e)
        {
            QMC.CDT_320.Ui.Dialogs.RuntimeOffsetMonitorDialog.ResetRuntimeOffsetsWithConfirm(
                FindForm() as Form1, true);
        }

        private void btnRuntimeOffsetMonitor_Click(object sender, EventArgs e)
        {
            using (var dlg = new QMC.CDT_320.Ui.Dialogs.RuntimeOffsetMonitorDialog())
                dlg.ShowDialog(FindForm());
        }

        private void btnRuntimeFilterSettings_Click(object sender, EventArgs e)
        {
            using (var dlg = new QMC.CDT_320.Ui.Dialogs.RuntimeFilterSettingsDialog())
                dlg.ShowDialog(FindForm());
        }
    }
}
