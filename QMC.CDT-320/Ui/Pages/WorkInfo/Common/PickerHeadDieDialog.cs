using System;
using QMC.CDT_320.Ui.Localization;
using System.Windows.Forms;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    internal partial class PickerHeadDieDialog : Form
    {
        private enum PickerDieManualState
        {
            Wait,
            Good,
            Ng,
            Skip
        }

        private readonly PickerSequenceSide _side;
        private readonly int _pickerNo;
        private readonly MaterialLocationKind _pickerLocation;
        private DieMaterial _die;

        private void InitializeLanguageBindings()
        {
            Lang.BindKey(lblTitle, "workInfoUi.die.lblTitle");
            Lang.BindKey(lblDieIdTitle, "workInfoUi.die.lblDieIdTitle");
            Lang.BindKey(lblWaferTitle, "workInfoUi.die.lblWaferTitle");
            Lang.BindKey(lblSequenceTitle, "workInfoUi.die.lblSequenceTitle");
            Lang.BindKey(lblMapTitle, "workInfoUi.die.lblMapTitle");
            Lang.BindKey(lblLocationTitle, "workInfoUi.die.lblLocationTitle");
            Lang.BindKey(lblResultTitle, "workInfoUi.die.lblResultTitle");
            Lang.BindKey(lblNgCodeTitle, "workInfoUi.die.lblNgCodeTitle");
            Lang.BindKey(lblReasonTitle, "workInfoUi.die.lblReasonTitle");
            Lang.BindKey(btnApply, "workInfoUi.die.btnApply");
            Lang.BindKey(btnClear, "workInfoUi.die.btnClear");
            Lang.BindKey(btnClose, "workInfoUi.die.btnClose");
            Lang.BindChoices(cmbResult, WorkInfoText.Display);
        }

        public PickerHeadDieDialog(PickerSequenceSide side, int pickerNo)
        {
            InitializeComponent();
            InitializeLanguageBindings();

            _side = side;
            _pickerNo = pickerNo;
            _pickerLocation = side == PickerSequenceSide.Front
                ? MaterialLocationKind.PickerFront
                : MaterialLocationKind.PickerRear;

            string titleKey = side == PickerSequenceSide.Front ? "workInfoUi.die.frontTitle" : "workInfoUi.die.rearTitle";
            Lang.BindFormat(this, titleKey, pickerNo);
            Lang.BindFormat(lblTitle, titleKey, pickerNo);
            ConfigureStateUi();
            LoadDieInfo();
        }

        private void ConfigureStateUi()
        {
            Lang.BindKey(lblResultTitle, "workInfoUi.die.lblResultTitle");
            cmbResult.Items.Clear();
            cmbResult.Items.Add("WAIT / 대기");
            cmbResult.Items.Add("GOOD / 완료");
            cmbResult.Items.Add("NG / 불량");
            cmbResult.Items.Add("SKIP / 제외");
            cmbResult.SelectedIndex = 0;
            chkInputTarget.Visible = false;
            chkInputTarget.TabStop = false;
        }

        private void LoadDieInfo()
        {
            try
            {
                _die = MaterialStateService.GetDieAtPicker(_pickerLocation, _pickerNo);
                if (_die == null)
                {
                    lblDieIdValue.Text = "-";
                    lblWaferValue.Text = "-";
                    lblSequenceValue.Text = "-";
                    lblMapValue.Text = "-";
                    lblLocationValue.Text = "-";
                    cmbResult.SelectedItem = "WAIT / 대기";
                    chkInputTarget.Checked = false;
                    txtNgCode.Text = "";
                    txtReason.Text = "No die";
                    btnApply.Enabled = false;
                    btnClear.Enabled = false;
                    return;
                }

                lblDieIdValue.Text = string.IsNullOrWhiteSpace(_die.DieId) ? "-" : _die.DieId;
                lblWaferValue.Text = string.IsNullOrWhiteSpace(_die.WaferID_Input) ? "-" : _die.WaferID_Input;
                lblSequenceValue.Text = _die.InputSequenceNo.ToString();
                lblMapValue.Text = _die.Wafer_IndexX + " / " + _die.Wafer_IndexY;
                lblLocationValue.Text = _die.CurrentLocation != null ? _die.CurrentLocation.ToString() : "-";
                cmbResult.SelectedItem = ResolvePickerDieStateText(_die);
                chkInputTarget.Checked = _die.IsInputTarget;
                txtNgCode.Text = _die.NgCodes != null && _die.NgCodes.Count > 0 ? string.Join(",", _die.NgCodes.ToArray()) : "";
                txtReason.Text = "Manual picker head edit";
                btnApply.Enabled = true;
                btnClear.Enabled = true;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "UI", "PickerHeadDieDialog",
                    "Load picker head die failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("workInfoUi.die.lblTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            try
            {
                PickerDieManualState state = ResolveSelectedState();
                DieResult result = ResolveStateResult(state);
                bool isInputTarget = state != PickerDieManualState.Skip;
                string message;
                bool ok = MaterialStateService.UpdatePickerDieManualState(
                    _pickerLocation,
                    _pickerNo,
                    result,
                    isInputTarget,
                    txtNgCode.Text,
                    txtReason.Text,
                    out message);

                if (!ok)
                {
                    QMC.Common.MessageDialog.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MaterialStateService.TryFlushPendingSave("PickerHeadDieDialogApply");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "UI", "PickerHeadDieDialog",
                    "Apply picker head die failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult answer = QMC.Common.MessageDialog.Show(
                    this,
                    Lang.T("workInfoUi.die.clearConfirm"),
                    Text,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (answer != DialogResult.Yes)
                    return;

                string message;
                bool ok = MaterialStateService.ClearPickerDieMaterial(
                    _pickerLocation,
                    _pickerNo,
                    txtReason.Text,
                    out message);

                if (!ok)
                {
                    QMC.Common.MessageDialog.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MaterialStateService.TryFlushPendingSave("PickerHeadDieDialogClear");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "UI", "PickerHeadDieDialog",
                    "Clear picker head die failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private PickerDieManualState ResolveSelectedState()
        {
            try
            {
                string value = cmbResult.SelectedItem != null ? cmbResult.SelectedItem.ToString() : "";
                if (value.IndexOf("GOOD", StringComparison.OrdinalIgnoreCase) >= 0)
                    return PickerDieManualState.Good;
                if (value.IndexOf("NG", StringComparison.OrdinalIgnoreCase) >= 0)
                    return PickerDieManualState.Ng;
                if (value.IndexOf("SKIP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.IndexOf("제외", StringComparison.OrdinalIgnoreCase) >= 0)
                    return PickerDieManualState.Skip;

                return PickerDieManualState.Wait;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "UI", "PickerHeadDieDialog",
                    "Resolve selected die state failed: " + ex.Message + " - Failed");
                return PickerDieManualState.Wait;
            }
            finally
            {
            }
        }

        private static DieResult ResolveStateResult(PickerDieManualState state)
        {
            switch (state)
            {
                case PickerDieManualState.Good:
                    return DieResult.Good;
                case PickerDieManualState.Ng:
                    return DieResult.NG;
                case PickerDieManualState.Skip:
                case PickerDieManualState.Wait:
                default:
                    return DieResult.Unknown;
            }
        }

        private static string ResolvePickerDieStateText(DieMaterial die)
        {
            if (die == null)
                return "WAIT / 대기";

            if (!die.IsInputTarget)
                return "SKIP / 제외";
            if (die.Result == DieResult.Good)
                return "GOOD / 완료";
            if (die.Result == DieResult.NG)
                return "NG / 불량";

            return "WAIT / 대기";
        }
    }
}


namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    internal static class WorkInfoText
    {
        private static readonly System.Collections.Generic.Dictionary<string, string> Keys =
            new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal)
            {
                { " / 통계 Run·Unit 중첩 제외", "workInfoUi.logic.containersExcluded" },
                { " / 해당 기록은 표 표시 한도 밖에 있습니다.", "workInfoUi.logic.outsideGrid" },
                { ", 마지막 미완성 행 제외", "workInfoUi.logic.incompleteWarning" },
                { "ALL", "workInfoUi.choice.all" },
                { "APPLY", "workInfoUi.die.btnApply" },
                { "AUTO REFRESH", "workInfoUi.logic.chkAutoRefresh" },
                { "BOTTOM INSPECT", "workInfoUi.choice.bottom_inspect" },
                { "BOTTOM INTERVAL", "workInfoUi.choice.bottom_interval" },
                { "BOTTOM VISION->PITCH", "workInfoUi.choice.bottom_vision_pitch" },
                { "CATEGORY", "workInfoUi.logic.lblCategoryCaption" },
                { "CHART", "workInfoUi.logic.lblChartCaption" },
                { "CLEAR", "workInfoUi.die.btnClear" },
                { "CLEAR VIEW", "workInfoUi.logic.btnClearView" },
                { "CLOSE", "workInfoUi.die.btnClose" },
                { "CYCLE TIME", "workInfoUi.logic.tabCycle" },
                { "Canceled", "workInfoUi.choice.canceled" },
                { "Completed", "workInfoUi.choice.completed" },
                { "DATA", "workInfoUi.logic.lblSourceCaption" },
                { "Die ID", "workInfoUi.die.lblDieIdTitle" },
                { "Failed", "workInfoUi.choice.failed" },
                { "GOOD / 완료", "workInfoUi.choice.good" },
                { "IO", "workInfoUi.choice.io" },
                { "ITEM", "workInfoUi.logic.lblItemCaption" },
                { "LIVE", "workInfoUi.logic.lblDataSource" },
                { "Location", "workInfoUi.die.lblLocationTitle" },
                { "Logic", "workInfoUi.choice.logic" },
                { "Map X/Y", "workInfoUi.die.lblMapTitle" },
                { "Motion", "workInfoUi.choice.motion" },
                { "NG / 불량", "workInfoUi.choice.ng" },
                { "NG Code", "workInfoUi.die.lblNgCodeTitle" },
                { "OUTPUT RECEIVE", "workInfoUi.choice.output_receive" },
                { "Picker Head Die", "workInfoUi.die.lblTitle" },
                { "Process", "workInfoUi.choice.process" },
                { "RUN", "workInfoUi.logic.lblRunCaption" },
                { "Reason", "workInfoUi.die.lblReasonTitle" },
                { "Resource", "workInfoUi.choice.resource" },
                { "Run", "workInfoUi.choice.run" },
                { "Run 기록 불러오기", "workInfoUi.logic.phase.load" },
                { "Run 목록 확인", "workInfoUi.logic.phase.index" },
                { "SEQUENCE", "workInfoUi.choice.sequence" },
                { "SIDE 0 INSPECT", "workInfoUi.choice.side_0_inspect" },
                { "SIDE 0 INTERVAL", "workInfoUi.choice.side_0_interval" },
                { "SIDE 0->90 MOTION", "workInfoUi.choice.side_0_90_motion" },
                { "SIDE 90 INSPECT", "workInfoUi.choice.side_90_inspect" },
                { "SIDE 90 INTERVAL", "workInfoUi.choice.side_90_interval" },
                { "SKIP / 제외", "workInfoUi.choice.skip" },
                { "Sequence", "workInfoUi.die.lblSequenceTitle" },
                { "Skipped", "workInfoUi.choice.skipped" },
                { "State", "workInfoUi.die.lblResultTitle" },
                { "Step", "workInfoUi.choice.step" },
                { "Stopped", "workInfoUi.choice.stopped" },
                { "Success", "workInfoUi.choice.success" },
                { "UNIT FLOW", "workInfoUi.choice.unit_flow" },
                { "Unit", "workInfoUi.choice.unit" },
                { "Vision", "workInfoUi.choice.vision" },
                { "WAIT / 대기", "workInfoUi.choice.wait" },
                { "Wafer", "workInfoUi.die.lblWaferTitle" },
                { "Wait", "workInfoUi.choice.wait" },
                { "과거 택타임 기록을 불러오지 못했습니다.", "workInfoUi.logic.historyOpenFailed" },
                { "라인 맵 검증", "workInfoUi.picker.lineMapTitle" },
                { "라인 이동 시험", "workInfoUi.picker.lineMoveTitle" },
                { "선택한 Run을 불러오지 못했습니다.", "workInfoUi.logic.historyRunFailed" },
                { "시간 차트", "workInfoUi.logic.messageTitle" },
                { "실시간 메모리 기록", "workInfoUi.logic.lblFileInfo" },
                { "실시간 보기", "workInfoUi.logic.btnLiveView" },
                { "이전 택타임 기록 불러오기", "workInfoUi.logic.openTitle" },
                { "장비 객체를 찾을 수 없어 ContiNode 라인 맵 검증을 실행할 수 없습니다.", "workInfoUi.picker.lineMapNoMachine" },
                { "장비 객체를 찾을 수 없어 ContiNode 이동 시험을 실행할 수 없습니다.", "workInfoUi.picker.lineMoveNoMachine" },
                { "장비 타임라인", "workInfoUi.choice.timeline" },
                { "전면", "workInfoUi.picker.front" },
                { "전체 보기", "workInfoUi.logic.btnResetChart" },
                { "지난 기록 불러오기", "workInfoUi.logic.btnOpenHistory" },
                { "취소", "workInfoUi.logic.btnCancelHistory" },
                { "택타임 CSV (*.csv)|*.csv|모든 파일 (*.*)|*.*", "workInfoUi.logic.openFilter" },
                { "택타임 기록 대기 중", "workInfoUi.logic.lblSummary" },
                { "택타임 기록을 기다리는 중입니다.", "workInfoUi.logic.lblStatus" },
                { "택타임 추이", "workInfoUi.choice.trend" },
                { "현재 위치 확인 필요", "workInfoUi.picker.process.unknown" },
                { "현재 헤드의 다이 정보를 제거하시겠습니까?\r\n실제 자재 상태가 위치 불명으로 변경됩니다.", "workInfoUi.die.clearConfirm" },
                { "후면", "workInfoUi.picker.rear" },
            };

        public static string Display(string raw)
        {
            if (raw == null) return null;
            return Keys.TryGetValue(raw, out var key) ? Lang.T(key) : Lang.Display(raw);
        }

        public static string Format(string key, object[] values, params int[] translatedArguments)
        {
            object[] arguments = (object[])values.Clone();
            foreach (int index in translatedArguments)
                arguments[index] = Display(arguments[index] == null ? string.Empty : arguments[index].ToString());
            return Lang.Format(key, arguments);
        }

        public static void BindFormat(Control control, string key, object[] values, params int[] translatedArguments)
        {
            object[] snapshot = (object[])values.Clone();
            int[] translated = (int[])translatedArguments.Clone();
            Lang.BindDisplay(control, string.Empty, raw => Format(key, snapshot, translated));
        }

        public static string DisplayHeadState(string raw)
        {
            return raw != null && raw.StartsWith("PICK / ", StringComparison.Ordinal)
                ? Lang.Display("PICK") + raw.Substring(4) : Lang.Display(raw);
        }

        public static string DisplayProcessDetail(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            string detail = raw;
            string encoder = string.Empty;
            const string separator = " / ENC=";
            int encoderIndex = detail.LastIndexOf(separator, StringComparison.Ordinal);
            if (encoderIndex >= 0)
            {
                encoder = " / " + Lang.Display("ENC") + "=" + Lang.Display(detail.Substring(encoderIndex + separator.Length));
                detail = detail.Substring(0, encoderIndex);
            }
            else if (detail.StartsWith("ENC=", StringComparison.Ordinal))
                return Lang.Display("ENC") + "=" + Lang.Display(detail.Substring(4));

            if (detail == "현재 위치 확인 필요") return Lang.T("workInfoUi.picker.process.unknown") + encoder;
            if (detail.EndsWith(" 이동 중", StringComparison.Ordinal))
            {
                string movement = detail.Substring(0, detail.Length - " 이동 중".Length);
                int arrow = movement.IndexOf(" -> ", StringComparison.Ordinal);
                if (arrow >= 0)
                    return Lang.Format("workInfoUi.picker.process.transition", Lang.Display(movement.Substring(0, arrow)),
                        Lang.Display(movement.Substring(arrow + 4))) + encoder;
                if (movement.EndsWith(" 위치", StringComparison.Ordinal))
                    return Lang.Format("workInfoUi.picker.process.move", Lang.Display(movement.Substring(0, movement.Length - 3))) + encoder;
            }
            if (detail.EndsWith(" 대기", StringComparison.Ordinal))
                return Lang.Format("workInfoUi.picker.process.wait", Lang.Display(detail.Substring(0, detail.Length - 3))) + encoder;
            if (detail.EndsWith(" 공정 진행 중", StringComparison.Ordinal))
                return Lang.Format("workInfoUi.picker.process.running", Lang.Display(detail.Substring(0, detail.Length - " 공정 진행 중".Length))) + encoder;
            if (detail.EndsWith(" 검사 진행 중", StringComparison.Ordinal))
                return Lang.Format("workInfoUi.picker.process.inspect", Lang.Display(detail.Substring(0, detail.Length - " 검사 진행 중".Length))) + encoder;
            return Lang.Display(detail) + encoder;
        }
    }
}
