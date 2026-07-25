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
            cmbPickerNo.SelectedIndex = 0;
            InitializeSpeedPercent();
            WireEvents();
        }

        // Manual Sequence 속도 %(디폴트 속도 대비)를 현재 설정값으로 표시하고 변경을 즉시 반영한다.
        // MotionSpeedScale.EffectiveScaleFactor가 이동 속도와 가감속을 항상 같은 배율로 스케일한다.
        private void InitializeSpeedPercent()
        {
            decimal current = (decimal)QMC.Common.Motion.MotionSpeedScale.ManualSequencePercent;
            if (current < numSpeedPercent.Minimum) current = numSpeedPercent.Minimum;
            if (current > numSpeedPercent.Maximum) current = numSpeedPercent.Maximum;
            numSpeedPercent.Value = current;
            numSpeedPercent.ValueChanged += NumSpeedPercent_ValueChanged;
        }

        private void NumSpeedPercent_ValueChanged(object sender, EventArgs e)
        {
            double percent = (double)numSpeedPercent.Value;
            QMC.Common.Motion.MotionSpeedScale.ManualSequencePercent = percent;
            QMC.Common.Log.Write("Main", "SYSTEM", "ManualSequenceSpeed",
                "Manual Sequence 속도 퍼센트를 변경했습니다. percent=" +
                QMC.Common.Motion.MotionSpeedScale.ManualSequencePercent.ToString("0.###") +
                " (이동 속도/가감속 동일 배율 적용) - Set");
        }

        // Output LOAD/UNLOAD 대상 side. GOOD/NG는 별개로 동작시킨다.
        private BinSide SelectedOutputSide()
        {
            return rbOutputNg.Checked ? BinSide.Ng : BinSide.Good;
        }

        private void WireEvents()
        {
            btnInputLoad.Click += async delegate { await RunManualProcessAsync("INPUT LOAD", _controller.RunManualInputLoadAsync).ConfigureAwait(true); };
            btnInputUnload.Click += async delegate { await RunManualProcessAsync("INPUT UNLOAD", _controller.RunManualInputUnloadAsync).ConfigureAwait(true); };
            btnOutputLoad.Click += async delegate
            {
                BinSide side = SelectedOutputSide();
                await RunManualProcessAsync("OUTPUT LOAD(" + side + ")", () => _controller.RunManualOutputLoadAsync(side)).ConfigureAwait(true);
            };
            btnOutputUnload.Click += async delegate
            {
                BinSide side = SelectedOutputSide();
                await RunManualProcessAsync("OUTPUT UNLOAD(" + side + ")", () => _controller.RunManualOutputUnloadAsync(side)).ConfigureAwait(true);
            };
            btnPickUp.Click += async delegate { await RunPickerProcessAsync("PickUp", "PICK UP").ConfigureAwait(true); };
            btnBottom.Click += async delegate { await RunPickerProcessAsync("Bottom", "BOTTOM").ConfigureAwait(true); };
            btnSide.Click += async delegate { await RunPickerProcessAsync("Side", "SIDE").ConfigureAwait(true); };
            btnPlace.Click += async delegate { await RunPickerProcessAsync("Place", "PLACE").ConfigureAwait(true); };
            btnPickUpZTest.Click += async delegate { await RunPickerPickUpZMotionTestAsync().ConfigureAwait(true); };
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

        private async Task RunPickerPickUpZMotionTestAsync()
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                PickerSequenceSide side = rbRearPicker.Checked ? PickerSequenceSide.Rear : PickerSequenceSide.Front;
                int pickerNo = ResolveSelectedPickerNo();
                string label = side + " PICK Z TEST #" + pickerNo;
                statusLabel.Text = label + " 실행 중...";

                int result = await _controller.RunManualPickerPickUpZMotionTestAsync(side, pickerNo).ConfigureAwait(true);
                if (result == 0)
                {
                    statusLabel.Text = label + " 완료.";
                    return;
                }

                ShowFailure(string.IsNullOrWhiteSpace(_controller.LastActionFailureMessage)
                    ? "PickUp Z 단독 테스트 실패"
                    : _controller.LastActionFailureMessage);
            }
            catch (Exception ex)
            {
                ShowError("PickUp Z 단독 테스트 중 예외가 발생했습니다. " + ex.Message);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private int ResolveSelectedPickerNo()
        {
            int pickerNo;
            if (cmbPickerNo.SelectedItem != null &&
                int.TryParse(cmbPickerNo.SelectedItem.ToString(), out pickerNo))
                return pickerNo;

            return 1;
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
