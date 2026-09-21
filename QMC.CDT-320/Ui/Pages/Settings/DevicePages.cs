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

        private void InitializeLanguageBindings()
        {
            Lang.BindKey(grpInputBarcode, "settingsUi.caption.inputWaferBarcode");
            Lang.BindKey(chkInputUse, "settingsUi.caption.useInputWaferBarcode");
            Lang.BindKey(lblInputPort, "settingsUi.caption.serialPortCom");
            Lang.BindKey(lblInputBaud, "settingsUi.caption.baudRate");
            Lang.BindKey(lblInputTimeout, "settingsUi.caption.readTimeoutMs");
            Lang.BindKey(lblInputRetry, "settingsUi.caption.retryCount");
            Lang.BindKey(lblInputRetryStep, "settingsUi.caption.yRetryStepMm");
            Lang.BindKey(btnInputTest, "settingsUi.caption.testRead");
            Lang.BindKey(grpOutputBarcode, "settingsUi.caption.outputBinBarcode");
            Lang.BindKey(chkOutputUse, "settingsUi.caption.useOutputBinBarcode");
            Lang.BindKey(lblOutputPort, "settingsUi.caption.serialPortCom");
            Lang.BindKey(lblOutputBaud, "settingsUi.caption.baudRate");
            Lang.BindKey(lblOutputTimeout, "settingsUi.caption.readTimeoutMs");
            Lang.BindKey(lblOutputRetry, "settingsUi.caption.retryCount");
            Lang.BindKey(lblOutputRetryStep, "settingsUi.caption.yRetryStepMm");
            Lang.BindKey(btnOutputTest, "settingsUi.caption.testRead");
            Lang.BindKey(btnSaveBarcode, "settingsUi.caption.saveBarcodeSettings");
            Lang.BindKey(lblPortTitle, "settingsUi.caption.port");
            Lang.BindKey(lblBaudTitle, "settingsUi.caption.baudRate");
            Lang.BindKey(lblDataBitsTitle, "settingsUi.caption.dataBits");
            Lang.BindKey(lblParityTitle, "settingsUi.caption.parity");
            Lang.BindKey(lblStopBitsTitle, "settingsUi.caption.stopBits");
            Lang.BindKey(lblHeadCharTitle, "settingsUi.caption.headChar");
            Lang.BindKey(lblTailCharTitle, "settingsUi.caption.tailChar");
            Lang.BindKey(lblTimeoutTitle, "settingsUi.caption.readTimeout");
            Lang.BindKey(lblRetryTitle, "settingsUi.caption.retryCount");
            Lang.BindKey(btnConnect, "settingsUi.caption.connect");
            Lang.BindKey(btnTestRead, "settingsUi.caption.testRead");
            Load += (sender, args) => Lang.Apply(this);
        }

        public BarcodeReaderPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
            InitializeLanguageBindings();
            btnSaveBarcode.Click += btnSaveBarcode_Click;
            btnInputConnect.Click += (s, e) => ToggleConnection(true);
            btnOutputConnect.Click += (s, e) => ToggleConnection(false);
            btnInputTest.Click += async (s, e) => await TestReadAsync(true);
            btnOutputTest.Click += async (s, e) => await TestReadAsync(false);
            Lang.BindFormat(lblLastResult, "settingsUi.barcode.lastResult", string.Empty);
            LoadSettingsToUi();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.barcode");
            lblHeader.Tag = "i18n:set.barcode";
            Lang.BindKey(lblInputTrigger, "settingsUi.barcode.trigger");
            Lang.BindKey(lblOutputTrigger, "settingsUi.barcode.trigger");
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
                Lang.BindFormat(lblLastResult, "settingsUi.barcode.lastResult", string.Empty);
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
                SetResult("settingsUi.barcode.saved", true);
        }

        private bool SaveSettingsFromUi()
        {
            if (_loadingSettings)
                return false;

            string triggerError;
            if (!QMC.CDT320.VisionComm.Nlv5201BarcodeReader.TryValidateTriggerCommand(
                    txtInputTrigger.Text,
                    out triggerError))
            {
                SetResult("settingsUi.barcode.inputTriggerError", false, triggerError);
                return false;
            }
            if (!QMC.CDT320.VisionComm.Nlv5201BarcodeReader.TryValidateTriggerCommand(
                    txtOutputTrigger.Text,
                    out triggerError))
            {
                SetResult("settingsUi.barcode.outputTriggerError", false, triggerError);
                return false;
            }

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
                SetResult("settingsUi.barcode.disconnected", true, current.ReaderName);
                UpdateConnectionButtons();
                return;
            }

            if (!SaveSettingsFromUi())
                return;
            current = ResolveReader(inputChannel);
            bool connected = current != null && current.TryOpen();
            SetResult(
                connected ? "settingsUi.barcode.connected" : "settingsUi.barcode.connectionFailed",
                connected,
                current != null ? current.ReaderName : (inputChannel ? "INPUT WAFER" : "OUTPUT BIN"));
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
                SetResult("settingsUi.barcode.readerFailed", false, inputChannel ? "INPUT WAFER" : "OUTPUT BIN");
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
                SetResult("settingsUi.barcode.waiting", true, reader.ReaderName);
                string value = await reader.ReadAsync(timeoutMs);
                SetResult(
                    string.IsNullOrWhiteSpace(value)
                        ? "settingsUi.barcode.readTimeout"
                        : "settingsUi.barcode.readResult",
                    !string.IsNullOrWhiteSpace(value), reader.ReaderName, value);
            }
            catch (Exception ex)
            {
                SetResult("settingsUi.barcode.readFailed", false, reader.ReaderName, ex.Message);
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
                Lang.Format("settingsUi.message.blockedWhileRunning", SettingsUiText.Display(action ?? "Barcode 설정 변경")));
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
            Lang.BindKey(btnInputConnect, input != null && input.IsConnected ? "settingsUi.caption.disconnect" : "settingsUi.caption.connect");
            Lang.BindKey(btnOutputConnect, output != null && output.IsConnected ? "settingsUi.caption.disconnect" : "settingsUi.caption.connect");
        }

        private void SetResult(string key, bool success, params object[] args)
        {
            Lang.BindDisplay(lblLastResult, key, resourceKey =>
                Lang.Format("settingsUi.barcode.lastResult", Lang.Format(resourceKey, args)));
            lblLastResult.ForeColor = success ? Color.LimeGreen : Color.OrangeRed;
        }
    }

    /// <summary>Settings - zoom lens.</summary>
    public partial class ZoomLensPage : PageBase
    {
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(grpInputVision, "settingsUi.caption.inputVision");
            Lang.BindKey(grpOutputVision, "settingsUi.caption.outputVision");
            Lang.BindKey(grpLowerVision, "settingsUi.caption.lowerVision");
            Lang.BindKey(grpBottomVision, "settingsUi.caption.bottomVision");
            Lang.BindKey(grpSideVisionFront, "settingsUi.caption.sideVisionFront");
            Lang.BindKey(grpSideVisionRear, "settingsUi.caption.sideVisionRear");
            foreach (var table in new[] { inputVisionLayout, outputVisionLayout, lowerVisionLayout, bottomVisionLayout, sideVisionFrontLayout, sideVisionRearLayout })
            {
                foreach (System.Windows.Forms.Control caption in table.Controls)
                    if (caption is System.Windows.Forms.Label || caption is System.Windows.Forms.Button)
                        Lang.BindDisplay(caption, caption.Text, SettingsUiText.Display);
            }
            Load += (sender, args) => Lang.Apply(this);
        }

        public ZoomLensPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
            InitializeLanguageBindings();
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
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(grpSensor1, "settingsUi.caption.sensor1");
            Lang.BindKey(grpSensor2, "settingsUi.caption.sensor2");
            foreach (var table in new[] { sensor1Layout, sensor2Layout })
            {
                foreach (System.Windows.Forms.Control caption in table.Controls)
                    if (caption is System.Windows.Forms.Label || caption is System.Windows.Forms.Button)
                        Lang.BindDisplay(caption, caption.Text, SettingsUiText.Display);
            }
            Load += (sender, args) => Lang.Apply(this);
        }

        public HeightSensorPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
            InitializeLanguageBindings();
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
