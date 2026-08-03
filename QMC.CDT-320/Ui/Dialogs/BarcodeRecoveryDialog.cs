using System;
using System.ComponentModel;
using System.Windows.Forms;
using QMC.CDT320.Barcode;
using QMC.CDT320.VisionComm;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class BarcodeRecoveryDialog : Form
    {
        private bool _decisionSubmitted;
        private bool _cancellationClose;

        public event Action<int, double> RetryRequested;
        public event Action<string, int, double> ManualApplyRequested;
        public event Action BuzzerStopRequested;

        public BarcodeRecoveryDialog(BarcodeRecoveryRequest request)
        {
            InitializeComponent();
            BarcodeRecoveryRequest value = request ?? new BarcodeRecoveryRequest();
            lblChannelValue.Text = value.Channel == BarcodeReaderChannel.InputWafer
                ? "INPUT WAFER"
                : "OUTPUT BIN";
            lblMaterialValue.Text = string.IsNullOrWhiteSpace(value.MaterialId)
                ? "-"
                : value.MaterialId;
            lblInstanceValue.Text = string.IsNullOrWhiteSpace(value.MaterialInstanceId)
                ? "-"
                : value.MaterialInstanceId;
            txtFailure.Text = value.FailureMessage ?? "";
            numRetryCount.Value = ClampDecimal(value.RetryCount, numRetryCount.Minimum, numRetryCount.Maximum);
            numRetryStep.Value = ClampDecimal(
                Convert.ToDecimal(value.RetryStepMm),
                numRetryStep.Minimum,
                numRetryStep.Maximum);

            btnRetry.Click += btnRetry_Click;
            btnManualApply.Click += btnManualApply_Click;
            btnBuzzerStop.Click += btnBuzzerStop_Click;
            btnCloseRetry.Click += btnRetry_Click;
            FormClosing += BarcodeRecoveryDialog_FormClosing;
        }

        private static decimal ClampDecimal(decimal value, decimal minimum, decimal maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private void btnRetry_Click(object sender, EventArgs e)
        {
            SubmitRetry();
        }

        private void btnManualApply_Click(object sender, EventArgs e)
        {
            string barcode = BarcodeSerialAdapter.NormalizePayload(txtManualBarcode.Text);
            if (string.IsNullOrWhiteSpace(barcode) ||
                string.Equals(barcode, "WAFER-NULL-ID", StringComparison.OrdinalIgnoreCase))
            {
                QMC.Common.MessageDialog.Show(this, "수기로 적용할 유효한 Barcode를 입력하십시오.", "BARCODE");
                txtManualBarcode.Focus();
                txtManualBarcode.SelectAll();
                return;
            }

            if (_decisionSubmitted)
                return;
            _decisionSubmitted = true;
            ManualApplyRequested?.Invoke(
                barcode,
                Decimal.ToInt32(numRetryCount.Value),
                Decimal.ToDouble(numRetryStep.Value));
            Close();
        }

        private void btnBuzzerStop_Click(object sender, EventArgs e)
        {
            BuzzerStopRequested?.Invoke();
        }

        private void BarcodeRecoveryDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_cancellationClose || _decisionSubmitted ||
                e.CloseReason == CloseReason.ApplicationExitCall ||
                e.CloseReason == CloseReason.WindowsShutDown)
            {
                return;
            }

            // 작업자가 X 또는 CLOSE를 누르면 생산 계속이 아니라 자동 판독 Retry로 되돌립니다.
            _decisionSubmitted = true;
            RetryRequested?.Invoke(
                Decimal.ToInt32(numRetryCount.Value),
                Decimal.ToDouble(numRetryStep.Value));
        }

        private void SubmitRetry()
        {
            if (_decisionSubmitted)
                return;
            _decisionSubmitted = true;
            RetryRequested?.Invoke(
                Decimal.ToInt32(numRetryCount.Value),
                Decimal.ToDouble(numRetryStep.Value));
            Close();
        }

        public void CloseForCancellation()
        {
            if (IsDisposed)
                return;
            _cancellationClose = true;
            Close();
        }
    }
}
