using System;
using QMC.CDT_320.Ui.Localization;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.Common.Motion;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class InputFeederPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private System.Windows.Forms.Timer _timer;
        private TableLayoutPanel _sequenceActions;
        private bool _manualSequenceRunning;
        private SequenceStartMode _manualSequenceStartMode = SequenceStartMode.Resume;
        private string _lastMaterialDisplayKey = "";
        private const string LogSource = "INPUT-FEEDER-PAGE";

        public InputFeederPage()
        {
            InitializeComponent();
            WireSequenceActionButtons();

            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) =>
            {
                if (!ShouldRefreshVisible(this))
                    return;

                RefreshFromMachine();
            };
            VisibleChanged += (s, e) => { if (Visible) _timer.Start(); else _timer.Stop(); };
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private Form1 GetHost() => FindForm() as Form1;

        private void WireSequenceActionButtons()
        {
            try
            {
                _sequenceActions = actionBar;
                btnLoadFromCassette.Click += async (s, e) => await RunSequenceAction(btnLoadFromCassette.Text, RunLoadFromCassetteAsync);
                btnLoadToStage.Click += async (s, e) => await RunSequenceAction(btnLoadToStage.Text, RunLoadToStageAsync);
                btnUnloadFromStage.Click += async (s, e) => await RunSequenceAction(btnUnloadFromStage.Text, RunUnloadFromStageAsync);
                btnUnloadToCassette.Click += async (s, e) => await RunSequenceAction(btnUnloadToCassette.Text, RunUnloadToCassetteAsync);
                btnRecover.Click += async (s, e) => await RunSequenceAction(btnRecover.Text, RunRecoverAsync);
                btnStop.Click += async (s, e) => await StopManualActionAsync();
                materialDetailView.CreateDataRequested += MaterialDetailView_CreateDataRequested;
                materialDetailView.ClearDataRequested += MaterialDetailView_ClearDataRequested;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-FEEDER-BUTTON", "Wire sequence buttons failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task RunSequenceAction(string actionName, Func<Form1, Task<bool>> action)
        {
            IDisposable actionScope = null;
            bool showFailure = false;
            string exceptionMessage = null;
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null || action == null)
                    return;
                if (_manualSequenceRunning)
                    return;
                if (!ConfirmAction(actionName))
                    return;
                if (!TryAskManualSequenceStartMode(actionName, out _manualSequenceStartMode))
                    return;

                _manualSequenceRunning = true;
                SetSequenceButtonsEnabled(false);
                actionScope = host.Controller.BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "InputFeederPage:" + actionName);
                SequenceFailureStore.Clear();
                WriteEvent("INPUT-FEEDER-ACTION", actionName + " start");
                bool ok = await action(host);
                WriteEvent("INPUT-FEEDER-ACTION", actionName + " result=" + ok);
                if (!ok)
                {
                    RaiseWarning("INPUT-FEEDER-FAIL", actionName + " failed.");
                    showFailure = true;
                }
            }
            catch (OperationCanceledException)
            {
                WriteEvent("INPUT-FEEDER-CANCEL", actionName + " canceled.");
            }
            catch (QMC.CDT320.ManualActionBlockedException ex)
            {
                // 수동 시작 거부는 장비 이상이 아니므로 알람을 올리지 않는다(알람은 전체 축 EStop 유발).
                EventLogger.Write(EventKind.Warning, "UI", "INPUT-FEEDER-ACTION-BLOCKED", actionName + " blocked: " + ex.Message);
                QMC.Common.MessageDialog.Show(
                    this,
                    Lang.Format("message.manual.blocked", ex.Message),
                    Lang.T("message.title.inputFeeder"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-FEEDER-ACTION-EX", actionName + " failed: " + ex.Message);
                exceptionMessage = ex.Message;
            }
            finally
            {
                try
                {
                    if (actionScope != null)
                        actionScope.Dispose();
                }
                catch (Exception ex)
                {
                    WriteAlarm("INPUT-FEEDER-MANUAL-CLEANUP", "Input Feeder 수동 시컨스 정리 중 오류: " + ex.Message);
                }
                finally
                {
                    _manualSequenceRunning = false;
                    try { SetSequenceButtonsEnabled(true); } catch (Exception ex) { WriteAlarm("INPUT-FEEDER-BUTTON-RESTORE", "Input Feeder 버튼 복구 실패: " + ex.Message); }
                    try { RefreshFromMachine(); } catch (Exception ex) { WriteAlarm("INPUT-FEEDER-REFRESH", "Input Feeder 화면 갱신 실패: " + ex.Message); }
                }
            }

            if (showFailure)
            {
                string message = SequenceFailureStore.BuildManualFailureMessage(
                    actionName,
                    Lang.Format("message.manual.failed", Lang.Display(actionName)));
                QMC.Common.MessageDialog.Show(this, message, Lang.T("message.title.inputFeeder"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(this, exceptionMessage, Lang.T("message.title.inputFeeder"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool ConfirmAction(string actionName)
        {
            return QMC.Common.MessageDialog.Show(this, Lang.Format("message.manual.confirm", Lang.Display(actionName)), Lang.T("message.title.inputFeeder"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void SetSequenceButtonsEnabled(bool enabled)
        {
            if (_sequenceActions == null)
                return;

            _sequenceActions.Enabled = true;
            foreach (Control control in _sequenceActions.Controls)
            {
                if (!ReferenceEquals(control, btnStop))
                    control.Enabled = enabled;
            }

            if (btnStop != null)
                btnStop.Enabled = true;
        }

        private async Task StopManualActionAsync()
        {
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null)
                    return;

                WriteEvent("INPUT-FEEDER-STOP", "Manual action stop requested.");
                await host.Controller.StopAsync();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-FEEDER-STOP-EX", "Manual action stop failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<bool> RunLoadFromCassetteAsync(Form1 host)
        {
            return await CreateSequence(host).RunLoadFromCassetteAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
        }

        private async Task<bool> RunLoadToStageAsync(Form1 host)
        {
            // 기존 조건: 보정 없는 BuildOptions 사용 - 피더에 웨이퍼가 이미 있으면(재개 상황)
            //           FindNext가 "다음 Ready 슬롯"을 반환해 피더 웨이퍼 원본 슬롯과 불일치 알람이 났다.
            // 현재 기준: 피더 웨이퍼의 원본 role/slot으로 옵션을 보정한다.
            return await CreateSequence(host).RunLoadToStageAsync(host.Controller.ManualOperationToken, BuildFeederWaferAwareOptions(host)) == 0;
        }

        private async Task<bool> RunUnloadFromStageAsync(Form1 host)
        {
            // 기존 조건: 보정 없는 BuildOptions 사용 - Stage에 웨이퍼가 있는 상태에서(STAGE->FEEDER)
            //           FindNext가 "다음 Ready 슬롯"을 반환해 Stage 웨이퍼 원본 슬롯과 불일치(IN-FEEDER-MATERIAL-SOURCE) 알람이 났다.
            // 현재 기준: Stage 웨이퍼의 원본 role/slot으로 옵션을 보정한다.
            // 기존 조건: return await CreateSequence(host).RunUnloadFromStageAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
            return await CreateSequence(host).RunUnloadFromStageAsync(host.Controller.ManualOperationToken, BuildStageWaferAwareOptions(host)) == 0;
        }

        private async Task<bool> RunUnloadToCassetteAsync(Form1 host)
        {
            return await CreateSequence(host).RunUnloadToCassetteAsync(host.Controller.ManualOperationToken, BuildUnloadToCassetteOptions(host)) == 0;
        }

        private async Task<bool> RunRecoverAsync(Form1 host)
        {
            return await CreateSequence(host).RunRecoverAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
        }

        private InputFeederSequence CreateSequence(Form1 host)
        {
            var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            return new InputFeederSequence(ctx);
        }

        private InputFeederSequenceOptions BuildOptions(Form1 host)
        {
            var options = InputFeederSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = _manualSequenceStartMode;
            options.SlotIndex = ResolveInputSlot(host);
            options.NextSlotIndex = options.SlotIndex;
            options.WaferSize = ResolveInputWaferSize(host);
            options.MoveTimeoutMs = ResolveMoveTimeoutMs(host);
            options.FineMove = false;
            AppSettings settings = AppSettingsStore.Current;
            options.UseBarcode = settings != null && settings.UseInputWaferBarcode;
            return options;
        }

        private InputFeederSequenceOptions BuildUnloadToCassetteOptions(Form1 host)
        {
            return BuildFeederWaferAwareOptions(host);
        }

        // To do: 피더에 웨이퍼가 있으면(재개 상황) 옵션 role/slot을 그 웨이퍼의 원본(SourceCassetteRole/SourceSlotNumber)으로 보정한다.
        //        기존 보정은 role이 옵션 기본값(Input1)과 같을 때만 슬롯을 맞춰서 2단(Input2) 웨이퍼가 커버되지 않았다.
        private InputFeederSequenceOptions BuildFeederWaferAwareOptions(Form1 host)
        {
            var options = BuildOptions(host);
            WaferMaterial wafer = ResolveFeederWaferMaterial();
            if (wafer != null &&
                wafer.SourceSlotNumber >= 0 &&
                (wafer.SourceCassetteRole == CassetteMaterialRole.Input1 || wafer.SourceCassetteRole == CassetteMaterialRole.Input2))
            {
                options.CassetteRole = wafer.SourceCassetteRole;
                options.SlotIndex = wafer.SourceSlotNumber;
                options.NextSlotIndex = options.SlotIndex;
            }

            return options;
        }

        // To do: Stage에 웨이퍼가 있으면(STAGE->FEEDER 수동 구동) 옵션 role/slot을 그 웨이퍼의 원본(SourceCassetteRole/SourceSlotNumber)으로 보정한다.
        //        시퀀스(MoveMaterialDataToFeeder)가 Stage 웨이퍼의 원본과 옵션을 비교하므로 FindNext 기반 기본 옵션은 불일치 알람을 유발한다.
        private InputFeederSequenceOptions BuildStageWaferAwareOptions(Form1 host)
        {
            var options = BuildOptions(host);
            WaferMaterial wafer = ResolveStageWaferMaterial(host);
            if (wafer != null &&
                wafer.SourceSlotNumber >= 0 &&
                (wafer.SourceCassetteRole == CassetteMaterialRole.Input1 || wafer.SourceCassetteRole == CassetteMaterialRole.Input2))
            {
                options.CassetteRole = wafer.SourceCassetteRole;
                options.SlotIndex = wafer.SourceSlotNumber;
                options.NextSlotIndex = options.SlotIndex;
            }

            return options;
        }

        // 시퀀스(InputFeederUnloadFromStageSequence.ResolveStageWafer)와 동일 소스로 Stage 웨이퍼를 조회한다.
        private WaferMaterial ResolveStageWaferMaterial(Form1 host)
        {
            var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
            if (stage != null && stage.CurrentWaferMaterial != null)
                return stage.CurrentWaferMaterial;

            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
        }

        private static int ResolveInputSlot(Form1 host)
        {
            var controller = host != null ? host.Controller : null;
            if (controller != null && controller.CurrentInputSlot >= 0)
                return controller.CurrentInputSlot;

            var cassette = host != null && host.Machine != null ? host.Machine.InputCassetteUnit : null;
            if (cassette == null)
                return 0;

            int slot = cassette.FindNextProcessWaferSlot();
            return slot >= 0 ? slot : 0;
        }

        private static int ResolveInputWaferSize(Form1 host)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.InputCassetteUnit : null;
            return MaterialStateService.ResolveWaferSizeInch(cassette != null && cassette.Config != null ? cassette.Config.InchSelect : 8);
        }

        private static int ResolveMoveTimeoutMs(Form1 host)
        {
            var feeder = host != null && host.Machine != null ? host.Machine.InputFeederUnit : null;
            if (feeder != null && feeder.FeederY != null && feeder.FeederY.Setup != null && feeder.FeederY.Setup.MoveTimeoutMs > 0)
                return feeder.FeederY.Setup.MoveTimeoutMs;
            return 10000;
        }

        private void RefreshFromMachine()
        {
            var host = GetHost();
            if (host?.Machine == null) return;
            var loader = host.Machine.InputFeederUnit;

            // INPUT FEEDER Y: 이 페이지의 담당 축인 InputFeederY 위치를 표시한다.
            // (이전에는 InputCassette의 InputLifterZ를 표시해 InputCassette 페이지와 중복되고
            //  Feeder 페이지에서 정작 FeederY 좌표를 볼 수 없었다.)
            if (loader != null && loader.FeederY != null)
                _lblLifterPos.Text = AxisUnitConverter.FormatDisplay(loader.FeederY.ActualPosition, loader.FeederY, "0.###", true);
            bool clamp = loader.IsWaferFeederClamp();
            bool unclamp = loader.IsWaferFeederUnclamp();
            _lblClampState.Text = clamp ? "CLAMP" : (unclamp ? "UNCLAMP" : "ERROR");
            _lblClampState.ForeColor = clamp || unclamp ? Color.Black : Color.Red;

            bool up = loader.IsWaferFeederUp();
            bool down = loader.IsWaferFeederDown();
            _lblUpDownState.Text = down ? "DOWN" : (up ? "UP" : "--");
            _lblUpDownState.ForeColor = up || down ? Color.Black : Color.Red;
            bool hasFeederWaferData = HasDisplayFeederWafer();
            bool hasFeederWafer = loader.IsWaferFeederSimulationOrDryRun()
                ? hasFeederWaferData
                : loader.HasWaferOnFeeder();
            _lblExist.Text = hasFeederWafer ? "WAFER" : "--";

            dotRing.IsOn = loader.IsWaferFeederSimulationOrDryRun()
                ? hasFeederWaferData
                : loader.WaferFeederRingCheckSensor.IsOn;
            dotOverload.IsOn = loader.IsWaferFeederOverload();

            RefreshMaterialDetail(false);
        }

        private bool HasDisplayFeederWafer()
        {
            try
            {
                WaferMaterial wafer = ResolveFeederWaferMaterial();
                return wafer != null &&
                       !string.IsNullOrWhiteSpace(wafer.WaferId) &&
                       WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Empty;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private void RefreshMaterialDetail(bool force)
        {
            if (materialDetailView == null)
                return;

            WaferMaterial wafer = ResolveFeederWaferMaterial();
            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId) ||
                WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty)
            {
                _lastMaterialDisplayKey = "";
                materialDetailView.Clear();
                materialDetailView.Visible = true;
                return;
            }

            string displayKey = BuildMaterialDisplayKey(wafer);
            if (!force && materialDetailView.Visible && string.Equals(displayKey, _lastMaterialDisplayKey, StringComparison.Ordinal))
                return;

            _lastMaterialDisplayKey = displayKey;
            materialDetailView.Visible = true;
            materialDetailView.SetRows("FEEDER MATERIAL", BuildFeederMaterialRows(wafer));
        }

        private WaferMaterial ResolveFeederWaferMaterial()
        {
            var host = GetHost();
            var feeder = host != null && host.Machine != null ? host.Machine.InputFeederUnit : null;
            if (feeder != null && feeder.CurrentWaferMaterial != null)
                return feeder.CurrentWaferMaterial;

            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
        }

        private void MaterialDetailView_CreateDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmMaterialDataAction(Lang.T("message.inputFeeder.createConfirm")))
                    return;

                var wafer = MaterialStateService.CreateWaferAtLocation(
                    MaterialLocationKind.InputFeeder,
                    "INPUT-FEEDER-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                    WaferMaterialState.WorkReady);

                var host = GetHost();
                var feeder = host != null && host.Machine != null ? host.Machine.InputFeederUnit : null;
                if (feeder != null)
                    feeder.SetCurrentWaferMaterial(wafer);

                WriteEvent("INPUT-FEEDER-DATA-CREATE", "wafer=" + (wafer != null ? wafer.WaferId : ""));
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-FEEDER-DATA-CREATE-EX", "Material data create failed: " + ex.Message);
            }
        }

        private void MaterialDetailView_ClearDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmMaterialDataAction(Lang.T("message.inputFeeder.clearConfirm")))
                    return;

                MaterialStateService.ClearWaferAtLocation(MaterialLocationKind.InputFeeder);
                var host = GetHost();
                var feeder = host != null && host.Machine != null ? host.Machine.InputFeederUnit : null;
                if (feeder != null)
                    feeder.ClearCurrentWaferMaterial();

                WriteEvent("INPUT-FEEDER-DATA-CLEAR", "Input feeder material data cleared.");
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-FEEDER-DATA-CLEAR-EX", "Material data clear failed: " + ex.Message);
            }
        }

        private bool ConfirmMaterialDataAction(string message)
        {
            return QMC.Common.MessageDialog.Show(this, message, Lang.T("message.title.materialData"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private static IEnumerable<MaterialDetailRow> BuildFeederMaterialRows(WaferMaterial wafer)
        {
            string specName = wafer != null ? wafer.TapeFrameSpecName : "";
            var spec = !string.IsNullOrEmpty(specName) ? MaterialSpecs.FindFrame(specName) : null;
            var location = wafer != null && wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";

            return new[]
            {
                Row("Unit", "Input Feeder", "", false),
                Row("Wafer ID", wafer != null ? wafer.WaferId : "", "", false),
                Row("State", wafer != null ? WaferMaterialStateText.ToDisplayName(wafer.State) : "", "", false),
                Row("TapeFrame Spec", specName, "", false),
                Row("Frame Size", spec != null ? spec.OuterDiameterMm.ToString("0.###") + " mm" : "", "", false),
                Row("Grid", spec != null ? spec.DieMapX + " x " + spec.DieMapY : "", "", false),
                Row("Die Spec", spec != null ? spec.DieSpecName : "", "", false),
                Row("Lot ID", wafer != null ? wafer.CassetteLotId : "", "", false),
                Row("Source Cassette", wafer != null ? wafer.SourceCassetteRole.ToString() : "", "", false),
                Row("Source Slot", wafer != null && wafer.SourceSlotNumber >= 0 ? (wafer.SourceSlotNumber + 1).ToString("00") : "", "", false),
                Row("Source Pos", wafer != null && !double.IsNaN(wafer.SourceCassetteSlotPosition) ? wafer.SourceCassetteSlotPosition.ToString("0.###") : "", "", false),
                Row("Current Loc", location, "", false),
                Row("DieMap ObjId", wafer != null ? wafer.DieMapFrameObjId : "", "", false),
                Row("Updated", wafer != null ? wafer.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") : "", "", false)
            };
        }

        private static string BuildMaterialDisplayKey(WaferMaterial wafer)
        {
            if (wafer == null)
                return "";

            string location = wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";
            return string.Join("|",
                wafer.WaferId ?? "",
                WaferMaterialStateText.ToDisplayName(wafer.State),
                wafer.TapeFrameSpecName ?? "",
                wafer.CassetteLotId ?? "",
                wafer.SourceCassetteRole.ToString(),
                wafer.SourceSlotNumber.ToString(),
                location,
                wafer.DieMapFrameObjId ?? "",
                wafer.UpdatedAt.Ticks.ToString());
        }

        private static MaterialDetailRow Row(string name, string value, string key, bool editable)
        {
            return new MaterialDetailRow
            {
                Name = name,
                Value = value,
                Key = key ?? "",
                Editable = editable
            };
        }

        private static void WriteEvent(string code, string message)
        {
            try { EventLogger.Write(EventKind.Event, "UI", code, message); } catch { }
        }

        private static void WriteAlarm(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Alarm, "UI", code, message);
                AlarmManager.Raise(AlarmSeverity.Error, code, LogSource, message);
            }
            catch { }
        }

        private static void RaiseWarning(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Warning, "UI", code, message);
                AlarmManager.Raise(AlarmSeverity.Error, code, LogSource, message);
            }
            catch { }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
