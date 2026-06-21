using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Sequencing;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class ManualSequenceDialog : Form
    {
        private readonly MachineController _controller;
        private bool _busy;

        public ManualSequenceDialog(MachineController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            InitializeComponent();
            WireEvents();
        }

        private void WireEvents()
        {
            btnInputLoad.Click += async delegate { await RunManualProcessAsync("INPUT LOAD", _controller.RunManualInputLoadAsync).ConfigureAwait(true); };
            btnInputUnload.Click += async delegate { await RunManualProcessAsync("INPUT UNLOAD", _controller.RunManualInputUnloadAsync).ConfigureAwait(true); };
            btnOutputLoad.Click += async delegate { await RunManualProcessAsync("OUTPUT LOAD", _controller.RunManualOutputLoadAsync).ConfigureAwait(true); };
            btnOutputUnload.Click += async delegate { await RunManualProcessAsync("OUTPUT UNLOAD", _controller.RunManualOutputUnloadAsync).ConfigureAwait(true); };
            btnPickUp.Click += async delegate { await RunPickerProcessAsync("PickUp", "PICK UP").ConfigureAwait(true); };
            btnBottom.Click += async delegate { await RunPickerProcessAsync("Bottom", "BOTTOM").ConfigureAwait(true); };
            btnSide.Click += async delegate { await RunPickerProcessAsync("Side", "SIDE").ConfigureAwait(true); };
            btnPlace.Click += async delegate { await RunPickerProcessAsync("Place", "PLACE").ConfigureAwait(true); };
            btnAllStep.Click += async delegate { await RunUnitStepAsync(SequenceUnitKind.All, "ALL STEP").ConfigureAwait(true); };
            btnClose.Click += delegate { Close(); };
        }

        private async Task RunManualProcessAsync(string label, Func<Task<int>> action)
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                statusLabel.Text = label + " 실행 중...";

                int result = await action().ConfigureAwait(true);
                if (result == 0)
                {
                    statusLabel.Text = label + " 완료.";
                    return;
                }

                ShowFailure(string.IsNullOrWhiteSpace(_controller.LastActionFailureMessage)
                    ? label + " 실행 실패"
                    : _controller.LastActionFailureMessage);
            }
            catch (Exception ex)
            {
                ShowError(label + " 실행 중 예외가 발생했습니다. " + ex.Message);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private async Task RunUnitStepAsync(SequenceUnitKind unit, string label)
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                statusLabel.Text = label + " 실행 중...";

                int result = unit == SequenceUnitKind.All
                    ? await _controller.RunProcessSequenceStepAsync().ConfigureAwait(true)
                    : await _controller.RunManualSequenceUnitStepAsync(unit).ConfigureAwait(true);

                if (result == 0)
                {
                    statusLabel.Text = label + " 실행 요청 완료.";
                    return;
                }

                ShowFailure(string.IsNullOrWhiteSpace(_controller.LastActionFailureMessage)
                    ? "Manual 시퀀스 실행 실패"
                    : _controller.LastActionFailureMessage);
            }
            catch (Exception ex)
            {
                ShowError("Manual 시퀀스 실행 중 예외가 발생했습니다. " + ex.Message);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private async Task RunPickerProcessAsync(string processName, string label)
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                PickerSequenceSide side = rbRearPicker.Checked ? PickerSequenceSide.Rear : PickerSequenceSide.Front;
                statusLabel.Text = side + " " + label + " 실행 중...";

                int result = await _controller.RunManualPickerProcessAsync(side, processName).ConfigureAwait(true);
                if (result == 0)
                {
                    statusLabel.Text = side + " " + label + " 완료.";
                    return;
                }

                ShowFailure(string.IsNullOrWhiteSpace(_controller.LastActionFailureMessage)
                    ? "Picker Manual 공정 실행 실패"
                    : _controller.LastActionFailureMessage);
            }
            catch (Exception ex)
            {
                ShowError("Picker Manual 공정 실행 중 예외가 발생했습니다. " + ex.Message);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private void ShowFailure(string message)
        {
            statusLabel.Text = message;
            QMC.Common.MessageDialog.Show(this, message, "Manual Sequence", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ShowError(string message)
        {
            statusLabel.Text = message;
            QMC.Common.MessageDialog.Show(this, message, "Manual Sequence", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            foreach (Control control in Controls)
                SetButtonsEnabledRecursive(control, enabled);
        }

        private static void SetButtonsEnabledRecursive(Control control, bool enabled)
        {
            if (control is Button)
                control.Enabled = enabled;

            foreach (Control child in control.Controls)
                SetButtonsEnabledRecursive(child, enabled);
        }
    }
}
