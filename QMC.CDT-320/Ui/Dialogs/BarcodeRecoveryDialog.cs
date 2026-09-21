using QMC.CDT_320.Ui.Localization;
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
            InitializeLanguageBindings();
            BarcodeRecoveryRequest value = request ?? new BarcodeRecoveryRequest();
            _validationRecovery = value.ValidationRecovery;
            Lang.BindKey(lblChannelValue, value.Channel == BarcodeReaderChannel.InputWafer
                ? "extraDialog.barcode.inputWafer"
                : "extraDialog.barcode.outputBin");
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
                Lang.BindKey(this, "extraDialog.barcode.waferTitle");
                Lang.BindKey(lblHeader, "extraDialog.barcode.waferHeader");
                Lang.BindKey(lblFailureTitle, "extraDialog.barcode.needsReview");
                if (value.PrefixLength > 0)
                    Lang.BindFormat(txtFailure, "extraDialog.barcode.validationPrefixDetail", string.IsNullOrWhiteSpace(value.LotId) ? "-" : value.LotId, value.CurrentBarcode ?? "", value.PrefixLength, value.FailureMessage ?? "");
                else
                    Lang.BindFormat(txtFailure, "extraDialog.barcode.validationDetail", string.IsNullOrWhiteSpace(value.LotId) ? "-" : value.LotId, value.CurrentBarcode ?? "", value.FailureMessage ?? "");
                txtManualBarcode.Text = value.CurrentBarcode ?? "";
                Lang.BindKey(grpRetry, "extraDialog.barcode.retryCurrent");
                lblRetryCount.Visible = false;
                numRetryCount.Visible = false;
                lblRetryStep.Visible = false;
                numRetryStep.Visible = false;
                retryLayout.SetColumn(btnRetry, 0);
                retryLayout.SetColumnSpan(btnRetry, 5);
                Lang.BindKey(btnRetry, "extraDialog.barcode.checkAgain");
                Lang.BindKey(grpManual, "extraDialog.barcode.manualGuide");
                Lang.BindKey(btnManualApply, "extraDialog.barcode.checkInput");
                Lang.BindKey(btnCloseRetry, "extraDialog.barcode.abort");
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
                QMC.Common.MessageDialog.Show(this, Lang.T("extraDialog.barcode.enterValid"), Lang.T("extraDialog.barcode.messageTitle"));
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
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this, "extraDialog.barcode.title");
            Lang.BindKey(lblHeader, "extraDialog.barcode.header");
            Lang.BindKey(lblFailureTitle, "extraDialog.barcode.failureTitle");
            Lang.BindKey(grpRetry, "extraDialog.barcode.retryGroup");
            Lang.BindKey(btnRetry, "extraDialog.barcode.retryScan");
            Lang.BindKey(grpManual, "extraDialog.barcode.manualGroup");
            Lang.BindKey(btnManualApply, "extraDialog.barcode.manualApply");
            Lang.BindKey(btnCloseRetry, "extraDialog.barcode.closeRetry");
            Lang.BindKey(lblChannelTitle, "extraDialog.barcodeRecoveryDialog.lblChannelTitle.caption");
            Lang.BindKey(lblMaterialTitle, "extraDialog.barcodeRecoveryDialog.lblMaterialTitle.caption");
            Lang.BindKey(lblInstanceTitle, "extraDialog.barcodeRecoveryDialog.lblInstanceTitle.caption");
            Lang.BindKey(lblRetryCount, "extraDialog.barcodeRecoveryDialog.lblRetryCount.caption");
            Lang.BindKey(lblRetryStep, "extraDialog.barcodeRecoveryDialog.lblRetryStep.caption");
            Lang.BindKey(lblManualBarcode, "extraDialog.barcodeRecoveryDialog.lblManualBarcode.caption");
            Lang.BindKey(btnBuzzerStop, "extraDialog.barcodeRecoveryDialog.btnBuzzerStop.caption");
            Load += (sender, args) => Lang.Apply(this);
        }

    }
}
