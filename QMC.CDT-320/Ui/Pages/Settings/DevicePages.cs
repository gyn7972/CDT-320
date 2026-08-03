using QMC.CDT_320.Ui.Localization;

using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Settings - barcode reader.</summary>
    public partial class BarcodeReaderPage : PageBase
    {
        private bool _loadingSettings;
        private bool _readerActionBusy;

        public BarcodeReaderPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
            btnSaveBarcode.Click += btnSaveBarcode_Click;
            btnInputConnect.Click += (s, e) => ToggleConnection(true);
            btnOutputConnect.Click += (s, e) => ToggleConnection(false);
            btnInputTest.Click += async (s, e) => await TestReadAsync(true);
            btnOutputTest.Click += async (s, e) => await TestReadAsync(false);
            LoadSettingsToUi();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.barcode");
            lblHeader.Tag = "i18n:set.barcode";
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);
            SettingsPageLayoutStyler.ApplyActionControl(btnInputConnect);
            SettingsPageLayoutStyler.ApplyActionControl(btnOutputConnect);
            SettingsPageLayoutStyler.ApplyActionControl(btnInputTest);
            SettingsPageLayoutStyler.ApplyActionControl(btnOutputTest);
            SettingsPageLayoutStyler.ApplyActionControl(btnSaveBarcode);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && !_readerActionBusy)
                LoadSettingsToUi();
        }

        private Form1 Host => FindForm() as Form1;

        private void LoadSettingsToUi()
        {
            try
            {
                _loadingSettings = true;
                AppSettings settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
                chkInputUse.Checked = settings.UseInputWaferBarcode;
                chkOutputUse.Checked = settings.UseOutputBinBarcode;
                numInputPort.Value = ClampDecimal(settings.WaferBarcodeSerialPort, numInputPort);
                numOutputPort.Value = ClampDecimal(settings.BinBarcodeSerialPort, numOutputPort);
                numInputBaud.Value = ClampDecimal(settings.WaferBarcodeSerialBaud, numInputBaud);
                numOutputBaud.Value = ClampDecimal(settings.BinBarcodeSerialBaud, numOutputBaud);
                numInputTimeout.Value = ClampDecimal(settings.InputBarcodeReadTimeoutMs, numInputTimeout);
                numOutputTimeout.Value = ClampDecimal(settings.OutputBarcodeReadTimeoutMs, numOutputTimeout);
                numInputRetry.Value = ClampDecimal(settings.InputBarcodeRetryCount, numInputRetry);
                numOutputRetry.Value = ClampDecimal(settings.OutputBarcodeRetryCount, numOutputRetry);
                numInputRetryStep.Value = ClampDecimal(settings.InputBarcodeRetryStepMm, numInputRetryStep);
                numOutputRetryStep.Value = ClampDecimal(settings.OutputBarcodeRetryStepMm, numOutputRetryStep);
                txtInputTrigger.Text = settings.InputBarcodeTriggerCommand ?? "";
                txtOutputTrigger.Text = settings.OutputBarcodeTriggerCommand ?? "";
                UpdateConnectionButtons();
            }
            finally
            {
                _loadingSettings = false;
            }
        }

        private static decimal ClampDecimal(double value, NumericUpDown control)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return control.Minimum;
            decimal converted;
            try { converted = Convert.ToDecimal(value); }
            catch { converted = control.Minimum; }
            return Math.Max(control.Minimum, Math.Min(control.Maximum, converted));
        }

        private void btnSaveBarcode_Click(object sender, EventArgs e)
        {
            if (RejectReaderActionWhileRunning("Barcode 설정 저장"))
                return;
            if (SaveSettingsFromUi())
                SetResult("Barcode settings saved. Reader instances reloaded.", true);
        }

        private bool SaveSettingsFromUi()
        {
            if (_loadingSettings)
                return false;

            IBarcodeReader previousInputReader = ResolveReader(true);
            IBarcodeReader previousOutputReader = ResolveReader(false);
            bool reopenInput = previousInputReader != null && previousInputReader.IsConnected;
            bool reopenOutput = previousOutputReader != null && previousOutputReader.IsConnected;

            AppSettings settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
            settings.UseInputWaferBarcode = chkInputUse.Checked;
            settings.UseOutputBinBarcode = chkOutputUse.Checked;
            settings.WaferBarcodeSerialPort = Decimal.ToInt32(numInputPort.Value);
            settings.BinBarcodeSerialPort = Decimal.ToInt32(numOutputPort.Value);
            settings.WaferBarcodeSerialBaud = Decimal.ToInt32(numInputBaud.Value);
            settings.BinBarcodeSerialBaud = Decimal.ToInt32(numOutputBaud.Value);
            settings.InputBarcodeReadTimeoutMs = Decimal.ToInt32(numInputTimeout.Value);
            settings.OutputBarcodeReadTimeoutMs = Decimal.ToInt32(numOutputTimeout.Value);
            settings.InputBarcodeRetryCount = Decimal.ToInt32(numInputRetry.Value);
            settings.OutputBarcodeRetryCount = Decimal.ToInt32(numOutputRetry.Value);
            settings.InputBarcodeRetryStepMm = Decimal.ToDouble(numInputRetryStep.Value);
            settings.OutputBarcodeRetryStepMm = Decimal.ToDouble(numOutputRetryStep.Value);
            settings.InputBarcodeTriggerCommand = (txtInputTrigger.Text ?? "").Trim();
            settings.OutputBarcodeTriggerCommand = (txtOutputTrigger.Text ?? "").Trim();
            AppSettingsStore.Save();

            if (Host != null && Host.Machine != null)
            {
                Host.Machine.ReloadBarcodeReaders();
                if (reopenInput && Host.Machine.WaferBarcodeReader != null)
                    Host.Machine.WaferBarcodeReader.TryOpen();
                if (reopenOutput && Host.Machine.BinBarcodeReader != null)
                    Host.Machine.BinBarcodeReader.TryOpen();
            }
            UpdateConnectionButtons();
            return true;
        }

        private void ToggleConnection(bool inputChannel)
        {
            if (RejectReaderActionWhileRunning("Barcode 연결 변경"))
                return;

            IBarcodeReader current = ResolveReader(inputChannel);
            if (current != null && current.IsConnected)
            {
                current.Close();
                SetResult(current.ReaderName + " disconnected.", true);
                UpdateConnectionButtons();
                return;
            }

            if (!SaveSettingsFromUi())
                return;
            current = ResolveReader(inputChannel);
            bool connected = current != null && current.TryOpen();
            SetResult(
                (current != null ? current.ReaderName : (inputChannel ? "INPUT WAFER" : "OUTPUT BIN")) +
                (connected ? " connected." : " connection failed."),
                connected);
            UpdateConnectionButtons();
        }

        private async Task TestReadAsync(bool inputChannel)
        {
            if (_readerActionBusy || RejectReaderActionWhileRunning("Barcode TEST READ"))
                return;

            if (!SaveSettingsFromUi())
                return;
            IBarcodeReader reader = ResolveReader(inputChannel);
            if (reader == null || !reader.TryOpen())
            {
                SetResult((inputChannel ? "INPUT WAFER" : "OUTPUT BIN") + " reader connection failed.", false);
                UpdateConnectionButtons();
                return;
            }

            SetReaderActionBusy(true);
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                int timeoutMs = inputChannel
                    ? settings.InputBarcodeReadTimeoutMs
                    : settings.OutputBarcodeReadTimeoutMs;
                SetResult(reader.ReaderName + " waiting for barcode...", true);
                string value = await reader.ReadAsync(timeoutMs);
                SetResult(
                    string.IsNullOrWhiteSpace(value)
                        ? reader.ReaderName + " TEST READ failed/timeout."
                        : reader.ReaderName + " TEST READ: " + value,
                    !string.IsNullOrWhiteSpace(value));
            }
            catch (Exception ex)
            {
                SetResult(reader.ReaderName + " TEST READ failed: " + ex.Message, false);
            }
            finally
            {
                SetReaderActionBusy(false);
                UpdateConnectionButtons();
            }
        }

        private IBarcodeReader ResolveReader(bool inputChannel)
        {
            CDT320_Machine machine = Host != null ? Host.Machine : null;
            if (machine == null)
                return null;
            return inputChannel ? machine.WaferBarcodeReader : machine.BinBarcodeReader;
        }

        private bool RejectReaderActionWhileRunning(string action)
        {
            MachineController controller = Host != null ? Host.Controller : null;
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

            QMC.Common.MessageDialog.Show(
                "장비 동작 중에는 " + (action ?? "Barcode 설정 변경") +
                "을 수행할 수 없습니다. 장비를 정지한 뒤 다시 시도하십시오.");
            return true;
        }

        private void SetReaderActionBusy(bool busy)
        {
            _readerActionBusy = busy;
            btnSaveBarcode.Enabled = !busy;
            btnInputConnect.Enabled = !busy;
            btnOutputConnect.Enabled = !busy;
            btnInputTest.Enabled = !busy;
            btnOutputTest.Enabled = !busy;
        }

        private void UpdateConnectionButtons()
        {
            IBarcodeReader input = ResolveReader(true);
            IBarcodeReader output = ResolveReader(false);
            btnInputConnect.Text = input != null && input.IsConnected ? "DISCONNECT" : "CONNECT";
            btnOutputConnect.Text = output != null && output.IsConnected ? "DISCONNECT" : "CONNECT";
        }

        private void SetResult(string message, bool success)
        {
            lblLastResult.Text = "Last Result : " + (message ?? "");
            lblLastResult.ForeColor = success ? Color.LimeGreen : Color.OrangeRed;
        }
    }

    /// <summary>Settings - zoom lens.</summary>
    public partial class ZoomLensPage : PageBase
    {
        public ZoomLensPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.zoomLens");
            lblHeader.Tag = "i18n:set.zoomLens";
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);
        }
    }

    /// <summary>Settings - height sensor.</summary>
    public partial class HeightSensorPage : PageBase
    {
        public HeightSensorPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.heightSensor");
            lblHeader.Tag = "i18n:set.heightSensor";
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);
        }
    }
}
