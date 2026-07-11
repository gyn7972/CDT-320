using System;
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

        public PickerHeadDieDialog(PickerSequenceSide side, int pickerNo)
        {
            InitializeComponent();

            _side = side;
            _pickerNo = pickerNo;
            _pickerLocation = side == PickerSequenceSide.Front
                ? MaterialLocationKind.PickerFront
                : MaterialLocationKind.PickerRear;

            Text = (side == PickerSequenceSide.Front ? "Front" : "Rear") + " Picker Head #" + pickerNo + " Die";
            lblTitle.Text = Text;
            ConfigureStateUi();
            LoadDieInfo();
        }

        private void ConfigureStateUi()
        {
            lblResultTitle.Text = "State";
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Picker Head Die", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    "현재 Head의 Die 정보를 제거하시겠습니까?\r\n실제 Material 상태가 Unknown 위치로 변경됩니다.",
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

