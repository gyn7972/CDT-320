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
        private readonly bool _validationRecovery;

        public event Action<int, double> RetryRequested;
        public event Action<string, int, double> ManualApplyRequested;
        public event Action BuzzerStopRequested;
        public event Action CancelRequested;

        public BarcodeRecoveryDialog(BarcodeRecoveryRequest request)
        {
            InitializeComponent();
            BarcodeRecoveryRequest value = request ?? new BarcodeRecoveryRequest();
            _validationRecovery = value.ValidationRecovery;
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

            if (_validationRecovery)
            {
                Text = "웨이퍼 바코드 확인";
                lblHeader.Text = "웨이퍼 바코드 / 맵 확인";
                lblFailureTitle.Text = "확인 필요";
                txtFailure.Text = "LOT ID: " + (string.IsNullOrWhiteSpace(value.LotId) ? "없음" : value.LotId) +
                    Environment.NewLine + "읽은 바코드: " + (value.CurrentBarcode ?? "") +
                    (value.PrefixLength > 0 ? Environment.NewLine + "앞부분 비교: " + value.PrefixLength + "글자" : "") +
                    Environment.NewLine + Environment.NewLine + (value.FailureMessage ?? "");
                txtManualBarcode.Text = value.CurrentBarcode ?? "";
                grpRetry.Text = "현재 바코드로 다시 확인";
                lblRetryCount.Visible = false;
                numRetryCount.Visible = false;
                lblRetryStep.Visible = false;
                numRetryStep.Visible = false;
                retryLayout.SetColumn(btnRetry, 0);
                retryLayout.SetColumnSpan(btnRetry, 5);
                btnRetry.Text = "다시 확인";
                grpManual.Text = "웨이퍼 표시 확인 후 수동 입력";
                btnManualApply.Text = "입력값 검사";
                btnCloseRetry.Text = "작업 중단";
            }

            btnRetry.Click += btnRetry_Click;
            btnManualApply.Click += btnManualApply_Click;
            btnBuzzerStop.Click += btnBuzzerStop_Click;
            btnCloseRetry.Click += btnCloseRetry_Click;
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

        private void btnCloseRetry_Click(object sender, EventArgs e)
        {
            if (_validationRecovery)
                SubmitCancellation();
            else
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

            // 값/맵 검증창을 닫으면 중단한다. 닫기 동작으로 모션이나 재확인을 시작하지 않는다.
            if (_validationRecovery)
            {
                _decisionSubmitted = true;
                CancelRequested?.Invoke();
                return;
            }

            // 기존 판독 실패 복구창의 X/CLOSE는 자동 판독 Retry 동작을 유지한다.
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

        private void SubmitCancellation()
        {
            if (_decisionSubmitted)
                return;
            _decisionSubmitted = true;
            CancelRequested?.Invoke();
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
