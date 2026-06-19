using System;
using System.Windows.Forms;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    internal partial class PickerHeadDieDialog : Form
    {
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
            LoadDieInfo();
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
                    cmbResult.SelectedItem = DieResult.Unknown.ToString();
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
                cmbResult.SelectedItem = _die.Result.ToString();
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
                DieResult result = ResolveSelectedResult();
                string message;
                bool ok = MaterialStateService.UpdatePickerDieManualState(
                    _pickerLocation,
                    _pickerNo,
                    result,
                    chkInputTarget.Checked,
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

        private DieResult ResolveSelectedResult()
        {
            try
            {
                string value = cmbResult.SelectedItem != null ? cmbResult.SelectedItem.ToString() : "";
                if (string.Equals(value, DieResult.Good.ToString(), StringComparison.OrdinalIgnoreCase))
                    return DieResult.Good;
                if (string.Equals(value, DieResult.NG.ToString(), StringComparison.OrdinalIgnoreCase))
                    return DieResult.NG;

                return DieResult.Unknown;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "UI", "PickerHeadDieDialog",
                    "Resolve selected die result failed: " + ex.Message + " - Failed");
                return DieResult.Unknown;
            }
            finally
            {
            }
        }
    }
}

