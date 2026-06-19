using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Controls;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    internal enum PickerManualSequenceKind
    {
        Process,
        PickUp,
        Inspect,
        Bottom,
        Side,
        Place,
        Recover
    }

    internal sealed class PickerWorkInfoPageRuntime
    {
        private readonly PageBase _owner;
        private readonly PickerSequenceSide _side;
        private readonly Func<Form1> _getHost;
        private readonly Label _lblHeader;
        private readonly Label[] _headValueLabels;
        private readonly Label _lblColletChangeValue;
        private readonly Label _lblAutoPosValue;
        private readonly Label _lblColletCleaningValue;
        private readonly Label _lblColletCheckValue;
        private readonly Label _lblPickFailValue;
        private readonly Label _lblPlaceFailValue;
        private readonly Label _lblHeadZoneValue;
        private readonly Label _lblHeadProcessValue;
        private readonly Label[] _processFlowLabels;
        private readonly Label _lblProcessDetailValue;
        private readonly Label[] _colletUseTitleLabels;
        private readonly Label[] _colletUseValueLabels;
        private readonly IndicatorDot[] _vacuumDots;
        private readonly IndicatorDot[] _blowDots;
        private readonly IndicatorDot[] _flowDots;
        private readonly Label[] _vacuumLabels;
        private readonly Label[] _blowLabels;
        private readonly Label[] _flowLabels;
        private readonly DataGridView _axisGrid;
        private readonly Button _btnCountClear;
        private readonly ActionButton _btnInput;
        private readonly ActionButton _btnInspect;
        private readonly ActionButton _btnBottom;
        private readonly ActionButton _btnSide;
        private readonly ActionButton _btnOutput;
        private readonly ActionButton _btnStop;
        private readonly Control.ControlCollection _actionControls;
        private readonly System.Windows.Forms.Timer _timer;
        private PickerProcessSequence _stepSequence;
        private PickerPickUpSequence _pickUpStepSequence;
        private PickerBottomInspectionSequence _bottomInspectStepSequence;
        private PickerSideInspectionSequence _sideInspectStepSequence;
        private PickerPlaceSequence _placeStepSequence;
        private bool _manualSequenceRunning;
        private string _lastStableProcess = "AVOID";

        private sealed class RuntimeOffsetRowTag
        {
            public int PickerIndex { get; set; }
        }

        public PickerWorkInfoPageRuntime(
            PageBase owner,
            PickerSequenceSide side,
            Func<Form1> getHost,
            Label lblHeader,
            Label[] headValueLabels,
            Label lblColletChangeValue,
            Label lblAutoPosValue,
            Label lblColletCleaningValue,
            Label lblColletCheckValue,
            Label lblPickFailValue,
            Label lblPlaceFailValue,
            Label lblHeadZoneValue,
            Label lblHeadProcessValue,
            Label[] processFlowLabels,
            Label lblProcessDetailValue,
            Label[] colletUseTitleLabels,
            Label[] colletUseValueLabels,
            IndicatorDot[] vacuumDots,
            IndicatorDot[] blowDots,
            IndicatorDot[] flowDots,
            Label[] vacuumLabels,
            Label[] blowLabels,
            Label[] flowLabels,
            DataGridView axisGrid,
            Button btnCountClear,
            ActionButton btnInput,
            ActionButton btnInspect,
            ActionButton btnBottom,
            ActionButton btnSide,
            ActionButton btnOutput,
            ActionButton btnStop,
            Control.ControlCollection actionControls)
        {
            _owner = owner;
            _side = side;
            _getHost = getHost;
            _lblHeader = lblHeader;
            _headValueLabels = headValueLabels ?? new Label[0];
            _lblColletChangeValue = lblColletChangeValue;
            _lblAutoPosValue = lblAutoPosValue;
            _lblColletCleaningValue = lblColletCleaningValue;
            _lblColletCheckValue = lblColletCheckValue;
            _lblPickFailValue = lblPickFailValue;
            _lblPlaceFailValue = lblPlaceFailValue;
            _lblHeadZoneValue = lblHeadZoneValue;
            _lblHeadProcessValue = lblHeadProcessValue;
            _processFlowLabels = processFlowLabels ?? new Label[0];
            _lblProcessDetailValue = lblProcessDetailValue;
            _colletUseTitleLabels = colletUseTitleLabels ?? new Label[0];
            _colletUseValueLabels = colletUseValueLabels ?? new Label[0];
            _vacuumDots = vacuumDots ?? new IndicatorDot[0];
            _blowDots = blowDots ?? new IndicatorDot[0];
            _flowDots = flowDots ?? new IndicatorDot[0];
            _vacuumLabels = vacuumLabels ?? new Label[0];
            _blowLabels = blowLabels ?? new Label[0];
            _flowLabels = flowLabels ?? new Label[0];
            _axisGrid = axisGrid;
            _btnCountClear = btnCountClear;
            _btnInput = btnInput;
            _btnInspect = btnInspect;
            _btnBottom = btnBottom;
            _btnSide = btnSide;
            _btnOutput = btnOutput;
            _btnStop = btnStop;
            _actionControls = actionControls;

            WireEvents();
            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) =>
            {
                if (!PageBase.ShouldRefreshVisible(_owner))
                    return;

                Refresh();
            };
            _owner.HandleCreated += (s, e) => _timer.Start();
            _owner.HandleDestroyed += (s, e) =>
            {
                _timer.Stop();
                ResetStepSequence();
            };
            Refresh();
        }

        private string SideName
        {
            get { return _side == PickerSequenceSide.Front ? "Front Picker" : "Rear Picker"; }
        }

        private string LogCode
        {
            get { return _side == PickerSequenceSide.Front ? "FRONT-PICKER-PAGE" : "REAR-PICKER-PAGE"; }
        }

        private void WireEvents()
        {
            _btnInput.Click += async (s, e) => await RunSequenceAction(SideName + " PICK UP", SequenceRunMode.Auto, PickerManualSequenceKind.PickUp);
            _btnInspect.Click += async (s, e) => await RunSequenceAction(SideName + " INSPECT", SequenceRunMode.Auto, PickerManualSequenceKind.Inspect);
            _btnBottom.Click += async (s, e) => await RunSequenceAction(SideName + " BOTTOM", SequenceRunMode.Auto, PickerManualSequenceKind.Bottom);
            _btnSide.Click += async (s, e) => await RunSequenceAction(SideName + " SIDE", SequenceRunMode.Auto, PickerManualSequenceKind.Side);
            _btnOutput.Click += async (s, e) => await RunSequenceAction(SideName + " PLACE", SequenceRunMode.Auto, PickerManualSequenceKind.Place);
            _btnStop.Click += async (s, e) => await StopManualActionAsync();
            if (_btnCountClear != null)
                _btnCountClear.Click += (s, e) => ClearCounters();
        }

        public void Refresh()
        {
            try
            {
                Form1 host = _getHost();
                CDT320_Machine machine = host != null ? host.Machine : null;
                if (machine == null)
                {
                    SetEmptyMonitor();
                    return;
                }

                RefreshSummary(machine);
                RefreshAxisGrid(machine);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void SetEmptyMonitor()
        {
            for (int i = 0; i < _headValueLabels.Length; i++)
            {
                if (_headValueLabels[i] != null)
                    _headValueLabels[i].Text = "-";
            }
            _lblColletChangeValue.Text = "-";
            _lblAutoPosValue.Text = "-";
            _lblColletCleaningValue.Text = "-";
            _lblColletCheckValue.Text = "-";
            _lblPickFailValue.Text = "0 ea";
            _lblPlaceFailValue.Text = "0 ea";
            SetHeadZone("-");
            SetHeadProcess("-");
            SetProcessFlow("-");
            SetProcessDetail("-");
            for (int i = 0; i < _colletUseTitleLabels.Length; i++)
            {
                if (_colletUseTitleLabels[i] != null)
                {
                    _colletUseTitleLabels[i].Text = "#" + (i + 1) + " COLLET USE";
                    _colletUseTitleLabels[i].BackColor = Color.FromArgb(0xC8, 0xC8, 0xC8);
                    _colletUseTitleLabels[i].ForeColor = Color.Black;
                }
            }
            for (int i = 0; i < _colletUseValueLabels.Length; i++)
            {
                if (_colletUseValueLabels[i] != null)
                    _colletUseValueLabels[i].Text = "0 ea";
            }
            for (int i = 0; i < _vacuumDots.Length; i++)
                SetDot(_vacuumDots[i], false);
            for (int i = 0; i < _blowDots.Length; i++)
                SetDot(_blowDots[i], false);
            for (int i = 0; i < _flowDots.Length; i++)
                SetDot(_flowDots[i], false);
            for (int i = 0; i < _flowLabels.Length; i++)
            {
                if (_flowLabels[i] != null)
                    _flowLabels[i].Text = "HEAD FLOW #" + (i + 1) + " : OFF";
            }
        }

        private void RefreshSummary(CDT320_Machine machine)
        {
            bool cdaOk = ReadInput(GetCdaPressure(machine));
            bool vacuumOk = ReadInput(GetVacuumPressure(machine));
            bool simulationOrDryRun = IsSimulationOrDryRun(machine);

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                int index = pickerNo - 1;
                bool flow = ReadInput(GetFlow(machine, pickerNo));
                bool vacuum = ReadOutput(GetVacuum(machine, pickerNo));
                bool blow = ReadOutput(GetBlow(machine, pickerNo));
                DieMaterial pickedDie = GetPickedDieMaterial(pickerNo);
                bool hasPickedMaterial = pickedDie != null;
                bool vacuumDisplay = vacuum || flow || (simulationOrDryRun && hasPickedMaterial);
                bool flowDisplay = flow || (simulationOrDryRun && hasPickedMaterial);

                if (index < _headValueLabels.Length && _headValueLabels[index] != null)
                {
                    string headState = ResolveHeadState(machine, pickerNo, flow, pickedDie);
                    _headValueLabels[index].Text = headState;
                    _headValueLabels[index].ForeColor = IsPickedHeadState(headState) ? Color.Lime : Color.Black;
                }

                bool usePicker = UsePicker(machine, pickerNo);
                if (index < _colletUseTitleLabels.Length && _colletUseTitleLabels[index] != null)
                {
                    _colletUseTitleLabels[index].Text = "#" + pickerNo + (usePicker ? " COLLET USE" : " COLLET UNUSED");
                    _colletUseTitleLabels[index].BackColor = usePicker ? Color.FromArgb(0x00, 0xB0, 0x50) : Color.FromArgb(0x96, 0x96, 0x96);
                    _colletUseTitleLabels[index].ForeColor = usePicker ? Color.White : Color.Gainsboro;
                }
                if (index < _colletUseValueLabels.Length && _colletUseValueLabels[index] != null)
                {
                    _colletUseValueLabels[index].Text = GetColletUseCount(machine, pickerNo) + " ea";
                    _colletUseValueLabels[index].BackColor = Color.White;
                    _colletUseValueLabels[index].ForeColor = Color.Black;
                }

                if (index < _vacuumLabels.Length && _vacuumLabels[index] != null)
                    _vacuumLabels[index].Text = "HEAD VACUUM #" + pickerNo + " : " + (vacuumDisplay ? "ON" : "OFF");
                if (index < _blowLabels.Length && _blowLabels[index] != null)
                    _blowLabels[index].Text = "HEAD BLOW #" + pickerNo + " : " + (blow ? "ON" : "OFF");
                if (index < _flowLabels.Length && _flowLabels[index] != null)
                    _flowLabels[index].Text = "HEAD FLOW #" + pickerNo + " : " + (flowDisplay ? "ON" : "OFF");

                if (index < _vacuumDots.Length)
                    SetDot(_vacuumDots[index], vacuumDisplay);
                if (index < _blowDots.Length)
                    SetDot(_blowDots[index], blow);
                if (index < _flowDots.Length)
                    SetDot(_flowDots[index], flowDisplay);
            }

            _lblColletChangeValue.Text = cdaOk ? "READY" : "CHECK";
            _lblAutoPosValue.Text = IsGroupInPosition(machine, "AvoidPosition") ? "AVOID" : "MOVING";
            _lblColletCleaningValue.Text = vacuumOk ? "READY" : "CHECK";
            _lblColletCheckValue.Text = cdaOk && vacuumOk ? "READY" : "CHECK";
            _lblPickFailValue.Text = GetPickFailCount(machine) + " ea";
            _lblPlaceFailValue.Text = GetPlaceFailCount(machine) + " ea";
            string headZone = ResolveHeadZone(machine);
            string headProcess = ResolveHeadProcess(machine, headZone);
            bool pickerMoving = IsPickerMoving(machine);
            SetHeadZone(headZone);
            SetHeadProcess(headProcess);
            SetProcessFlow(headProcess);
            SetProcessDetail(ResolveProcessDetail(machine, headProcess, pickerMoving));

            if (!pickerMoving && IsStableProcess(headProcess))
                _lastStableProcess = NormalizeFlowProcess(headProcess);
        }

        private void ClearCounters()
        {
            try
            {
                Form1 host = _getHost();
                if (host == null || host.Machine == null)
                    return;

                DialogResult answer = QMC.Common.MessageDialog.Show(
                    _owner,
                    SideName + " count clear 진행하시겠습니까?",
                    SideName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                    return;

                if (_side == PickerSequenceSide.Front && host.Machine.PickerFrontUnit != null)
                    host.Machine.PickerFrontUnit.ResetWorkCounters();
                if (_side == PickerSequenceSide.Rear && host.Machine.PickerRearUnit != null)
                    host.Machine.PickerRearUnit.ResetWorkCounters();

                if (host.Controller != null)
                    host.Controller.SaveMachineRuntimeState(SideName + "WorkCounterClear");

                WriteEvent(SideName + " count clear.");
                Refresh();
            }
            catch (Exception ex)
            {
                WriteAlarm("Count clear failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(_owner, ex.Message, SideName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void RefreshAxisGrid(CDT320_Machine machine)
        {
            if (_axisGrid.Columns.Count == 0)
                return;

            ConfigureAxisGridColumns();

            if (_axisGrid.Rows.Count != 10)
            {
                _axisGrid.Rows.Clear();
                AddAxisRow("PICKER X", PickerAxis.PickerX);
                AddAxisRow("PICKER Y", PickerAxis.PickerY);
                AddAxisRow("PICKER T #1", PickerAxis.PickerT0);
                AddAxisRow("PICKER Z #1", PickerAxis.PickerZ0);
                AddAxisRow("PICKER T #2", PickerAxis.PickerT1);
                AddAxisRow("PICKER Z #2", PickerAxis.PickerZ1);
                AddAxisRow("PICKER T #3", PickerAxis.PickerT2);
                AddAxisRow("PICKER Z #3", PickerAxis.PickerZ2);
                AddAxisRow("PICKER T #4", PickerAxis.PickerT3);
                AddAxisRow("PICKER Z #4", PickerAxis.PickerZ3);
            }

            foreach (DataGridViewRow row in _axisGrid.Rows)
            {
                if (row.Tag == null)
                    continue;

                PickerAxis axisKey = (PickerAxis)row.Tag;
                BaseAxis axis = GetAxis(machine, axisKey);
                row.Cells["colCurrent"].Value = axis != null ? FormatAxisDisplay(axis.ActualPosition, axis) : "-";
            }
        }

        private void ConfigureAxisGridColumns()
        {
            try
            {
                foreach (DataGridViewColumn column in _axisGrid.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    column.Visible = column.Name == "colAxis" || column.Name == "colCurrent";
                    column.ReadOnly = true;
                }

                if (_axisGrid.Columns.Contains("colAxis"))
                {
                    _axisGrid.Columns["colAxis"].HeaderText = "Axis Name";
                    _axisGrid.Columns["colAxis"].Width = 210;
                }

                if (_axisGrid.Columns.Contains("colCurrent"))
                {
                    _axisGrid.Columns["colCurrent"].HeaderText = "Position";
                    _axisGrid.Columns["colCurrent"].Width = 180;
                    _axisGrid.Columns["colCurrent"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker 축 그리드 컬럼 설정 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private string FormatAxisDisplay(double nativeValue, BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return "-";

                return AxisUnitConverter.FormatDisplay(nativeValue, axis, "0.###", true);
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker 축 위치 단위 표시 실패: " + ex.Message);
                return nativeValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            }
            finally
            {
            }
        }

        private void AddAxisRow(string name, PickerAxis axis)
        {
            int rowIndex = _axisGrid.Rows.Add(name, "", "", "", "", "");
            _axisGrid.Rows[rowIndex].Tag = axis;
        }

        private void AddOffsetRow(string name, int pickerIndex)
        {
            int rowIndex = _axisGrid.Rows.Add(name, "", "", "", "", "");
            _axisGrid.Rows[rowIndex].Tag = new RuntimeOffsetRowTag { PickerIndex = pickerIndex };
        }

        private void RefreshOffsetRow(CDT320_Machine machine, DataGridViewRow row, int pickerIndex)
        {
            PickerAlignOffset offset = GetRuntimePickerOffset(machine, pickerIndex);
            BaseAxis xAxis = GetAxis(machine, PickerAxis.PickerX);
            BaseAxis yAxis = GetAxis(machine, PickerAxis.PickerY);

            row.Cells["colCurrent"].Value = offset != null
                ? "X=" + FormatAxisDisplay(offset.AlignOffsetX, xAxis) +
                  " / Y=" + FormatAxisDisplay(offset.AlignOffsetY, yAxis) +
                  " / T=" + offset.AlignOffsetT.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " deg"
                : "-";
        }

        private async Task RunSequenceAction(string actionName, SequenceRunMode mode)
        {
            await RunSequenceAction(actionName, mode, PickerManualSequenceKind.Process).ConfigureAwait(true);
        }

        private async Task RunSequenceAction(string actionName, SequenceRunMode mode, PickerManualSequenceKind kind)
        {
            IDisposable manualScope = null;
            bool showFailure = false;
            string exceptionMessage = null;
            try
            {
                Form1 host = _getHost();
                if (!ValidateManualSequenceHost(host, actionName))
                    return;

                if (_manualSequenceRunning)
                    return;
                SequenceStartMode startMode;
                if (!SelectManualSequenceStartMode(actionName, out startMode))
                    return;

                _manualSequenceRunning = true;
                SetButtonsEnabled(false);
                manualScope = host.Controller.EnterManualOperation();
                CancellationToken manualToken = host.Controller.ManualOperationToken;
                SequenceFailureStore.Clear();
                if (startMode == SequenceStartMode.Restart)
                    ResetStepSequence();

                WriteEvent(actionName + " 시작. 시작모드=" + FormatStartMode(startMode));

                Task<bool> actionTask = RunPickerSequenceTask(CreateContext(host), mode, kind, startMode, manualToken);
                Task cancelTask = WaitForCancellationAsync(manualToken);
                Task completed = await Task.WhenAny(actionTask, cancelTask).ConfigureAwait(true);
                if (completed == cancelTask)
                {
                    ObserveManualActionTask(actionTask, actionName);
                    WriteEvent(actionName + " 정지 요청으로 취소.");
                    return;
                }

                bool ok = await actionTask.ConfigureAwait(true);
                WriteEvent(actionName + " 결과=" + ok);
                if (!ok)
                    showFailure = true;
            }
            catch (OperationCanceledException)
            {
                WriteEvent(actionName + " 취소.");
            }
            catch (Exception ex)
            {
                WriteAlarm(actionName + " 실패: " + ex.Message);
                exceptionMessage = ex.Message;
            }
            finally
            {
                try
                {
                    if (manualScope != null)
                        manualScope.Dispose();

                    if (showFailure || !string.IsNullOrWhiteSpace(exceptionMessage))
                        ResetStepSequence();

                    TrySaveMaterialStateAfterManualSequence(actionName);
                }
                catch (Exception ex)
                {
                    WriteAlarm(actionName + " 종료 처리 중 오류: " + ex.Message);
                }
                finally
                {
                    _manualSequenceRunning = false;
                    SetButtonsEnabledSafe(true);
                    RefreshSafe();
                }
            }

            if (showFailure)
                ShowFailure(actionName);

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(_owner, exceptionMessage, SideName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool ValidateManualSequenceHost(Form1 host, string actionName)
        {
            if (host == null)
                return ShowManualSequenceHostError(actionName, "Form host is null.");

            if (host.Controller == null)
                return ShowManualSequenceHostError(actionName, "MachineController is null.");

            if (host.Machine == null)
                return ShowManualSequenceHostError(actionName, "Machine is null.");

            return true;
        }

        private bool ShowManualSequenceHostError(string actionName, string reason)
        {
            string message = actionName + " 실행 불가: " + reason;
            WriteAlarm(message);
            QMC.Common.MessageDialog.Show(_owner, message, SideName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private async Task<bool> RunPickerSequenceTask(
            MachineSequenceContext context,
            SequenceRunMode mode,
            PickerManualSequenceKind kind,
            SequenceStartMode startMode,
            CancellationToken ct)
        {
            try
            {
                PickerSequenceOptions options = BuildManualSequenceOptions(context, mode, startMode);

                int result = await RunManualPickerSequenceAsync(context, kind, options, ct).ConfigureAwait(false);
                return result == 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker sequence failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        public void ShowHeadDieDialog(int pickerNo)
        {
            try
            {
                using (PickerHeadDieDialog dialog = new PickerHeadDieDialog(_side, pickerNo))
                {
                    DialogResult result = dialog.ShowDialog(_owner);
                    if (result == DialogResult.OK)
                    {
                        MaterialStateService.TryFlushPendingSave(SideName + "HeadDieDialogUpdate");
                        Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker Head Die 정보 창 표시 실패: pickerNo=" + pickerNo + ", error=" + ex.Message);
                QMC.Common.MessageDialog.Show(
                    _owner,
                    "Picker Head Die 정보 창 표시 실패:\r\n" + ex.Message,
                    SideName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private PickerSequenceOptions BuildManualSequenceOptions(MachineSequenceContext context, SequenceRunMode mode, SequenceStartMode startMode)
        {
            PickerSequenceOptions options = PickerSequenceOptions.Default();
            options.RunMode = mode;
            options.StartMode = startMode;
            options.SimulateVisionResult = IsSimulationOrDryRun(context);
            return options;
        }

        private bool IsSimulationOrDryRun(MachineSequenceContext context)
        {
            try
            {
                if (AppSettingsStore.Current != null &&
                    (AppSettingsStore.Current.SimulationMode || AppSettingsStore.Current.DryRunMode))
                    return true;

                if (context != null && context.Controller != null && context.Controller.GlobalDryRun)
                    return true;

                CDT320_Machine machine = context != null ? context.Machine : null;
                return IsSimulationOrDryRun(machine);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsSimulationOrDryRun(CDT320_Machine machine)
        {
            try
            {
                if (AppSettingsStore.Current != null &&
                    (AppSettingsStore.Current.SimulationMode || AppSettingsStore.Current.DryRunMode))
                    return true;

                Form1 host = _getHost != null ? _getHost() : null;
                if (host != null && host.Controller != null && host.Controller.GlobalDryRun)
                    return true;

                if (machine == null)
                    return false;

                if (machine.InputStageUnit != null && machine.InputStageUnit.IsInputStageSimulationOrDryRun())
                    return true;

                if (_side == PickerSequenceSide.Front &&
                    machine.PickerFrontUnit != null &&
                    machine.PickerFrontUnit.Config != null &&
                    machine.PickerFrontUnit.Config.bDryRun)
                    return true;

                if (_side == PickerSequenceSide.Rear &&
                    machine.PickerRearUnit != null &&
                    machine.PickerRearUnit.Config != null &&
                    machine.PickerRearUnit.Config.bDryRun)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> RunManualPickerSequenceAsync(
            MachineSequenceContext context,
            PickerManualSequenceKind kind,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            if (options != null && options.RunMode == SequenceRunMode.Step)
                return await RunManualPickerStepSequenceAsync(context, kind, options, ct).ConfigureAwait(false);

            ResetStepSequence();

            switch (kind)
            {
                // 수동 픽업 시퀀스 실행
                case PickerManualSequenceKind.PickUp:
                    return await new PickerPickUpSequence(context, _side)
                        .RunAsync(ct, options)
                        .ConfigureAwait(false);

                // 수동 검사 시퀀스 실행
                case PickerManualSequenceKind.Inspect:
                    return await RunInspectionSequenceAsync(context, options, ct).ConfigureAwait(false);

                // 수동 Bottom 검사 시퀀스 실행
                case PickerManualSequenceKind.Bottom:
                    return await RunBottomInspectionSequenceAsync(context, options, ct).ConfigureAwait(false);

                // 수동 Side 검사 시퀀스 실행
                case PickerManualSequenceKind.Side:
                    return await RunSideInspectionSequenceAsync(context, options, ct).ConfigureAwait(false);

                // 수동 플레이스 시퀀스 실행
                case PickerManualSequenceKind.Place:
                    return await new PickerPlaceSequence(context, _side)
                        .RunAsync(ct, options)
                        .ConfigureAwait(false);

                // 수동 복구 시퀀스 실행
                case PickerManualSequenceKind.Recover:
                    return await RunRecoverSequenceAsync(context, options, ct).ConfigureAwait(false);

                default:
                    return await new PickerProcessSequence(context, _side)
                        .RunAsync(ct, options)
                        .ConfigureAwait(false);
            }
        }

        private async Task<int> RunManualPickerStepSequenceAsync(
            MachineSequenceContext context,
            PickerManualSequenceKind kind,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            switch (kind)
            {
                // 픽업 스텝 시퀀스 이어서 실행
                case PickerManualSequenceKind.PickUp:
                    if (_pickUpStepSequence == null || _pickUpStepSequence.IsComplete)
                        _pickUpStepSequence = new PickerPickUpSequence(context, _side);
                    return await RunPickUpStepAsync(options, ct).ConfigureAwait(false);

                // 검사 스텝 시퀀스 이어서 실행
                case PickerManualSequenceKind.Inspect:
                    return await RunInspectionStepAsync(context, options, ct).ConfigureAwait(false);

                // Bottom 검사 스텝 시퀀스 이어서 실행
                case PickerManualSequenceKind.Bottom:
                    if (_bottomInspectStepSequence == null || _bottomInspectStepSequence.IsComplete)
                        _bottomInspectStepSequence = new PickerBottomInspectionSequence(context, _side);
                    return await RunBottomInspectionStepAsync(options, ct).ConfigureAwait(false);

                // Side 검사 스텝 시퀀스 이어서 실행
                case PickerManualSequenceKind.Side:
                    if (_sideInspectStepSequence == null || _sideInspectStepSequence.IsComplete)
                        _sideInspectStepSequence = new PickerSideInspectionSequence(context, _side);
                    return await RunSideInspectionStepAsync(options, ct).ConfigureAwait(false);

                // 플레이스 스텝 시퀀스 이어서 실행
                case PickerManualSequenceKind.Place:
                    if (_placeStepSequence == null || _placeStepSequence.IsComplete)
                        _placeStepSequence = new PickerPlaceSequence(context, _side);
                    return await RunPlaceStepAsync(options, ct).ConfigureAwait(false);

                default:
                    if (_stepSequence == null || _stepSequence.IsComplete)
                        _stepSequence = new PickerProcessSequence(context, _side);
                    return await RunProcessStepAsync(options, ct).ConfigureAwait(false);
            }
        }

        private async Task<int> RunProcessStepAsync(PickerSequenceOptions options, CancellationToken ct)
        {
            int result = await _stepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (_stepSequence.IsComplete)
                _stepSequence = null;
            return result;
        }

        private async Task<int> RunPickUpStepAsync(PickerSequenceOptions options, CancellationToken ct)
        {
            int result = await _pickUpStepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (_pickUpStepSequence.IsComplete)
                _pickUpStepSequence = null;
            return result;
        }

        private async Task<int> RunInspectionStepAsync(
            MachineSequenceContext context,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            if (_bottomInspectStepSequence == null && _sideInspectStepSequence == null)
                _bottomInspectStepSequence = new PickerBottomInspectionSequence(context, _side);

            if (_bottomInspectStepSequence != null)
            {
                int bottomResult = await _bottomInspectStepSequence.RunAsync(ct, options).ConfigureAwait(false);
                if (bottomResult != 0)
                    return bottomResult;

                if (!_bottomInspectStepSequence.IsComplete)
                    return 0;

                _bottomInspectStepSequence = null;
                _sideInspectStepSequence = new PickerSideInspectionSequence(context, _side);
            }

            int sideResult = await _sideInspectStepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (_sideInspectStepSequence.IsComplete)
                _sideInspectStepSequence = null;
            return sideResult;
        }

        private async Task<int> RunBottomInspectionStepAsync(PickerSequenceOptions options, CancellationToken ct)
        {
            int result = await _bottomInspectStepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (_bottomInspectStepSequence.IsComplete)
                _bottomInspectStepSequence = null;
            return result;
        }

        private async Task<int> RunSideInspectionStepAsync(PickerSequenceOptions options, CancellationToken ct)
        {
            int result = await _sideInspectStepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (_sideInspectStepSequence.IsComplete)
                _sideInspectStepSequence = null;
            return result;
        }

        private async Task<int> RunPlaceStepAsync(PickerSequenceOptions options, CancellationToken ct)
        {
            int result = await _placeStepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (_placeStepSequence.IsComplete)
                _placeStepSequence = null;
            return result;
        }

        private async Task<int> RunInspectionSequenceAsync(
            MachineSequenceContext context,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            int bottomResult = await new PickerBottomInspectionSequence(context, _side)
                .RunAsync(ct, options)
                .ConfigureAwait(false);
            if (bottomResult != 0)
                return bottomResult;

            return await new PickerSideInspectionSequence(context, _side)
                .RunAsync(ct, options)
                .ConfigureAwait(false);
        }

        private async Task<int> RunBottomInspectionSequenceAsync(
            MachineSequenceContext context,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            return await new PickerBottomInspectionSequence(context, _side)
                .RunAsync(ct, options)
                .ConfigureAwait(false);
        }

        private async Task<int> RunSideInspectionSequenceAsync(
            MachineSequenceContext context,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            return await new PickerSideInspectionSequence(context, _side)
                .RunAsync(ct, options)
                .ConfigureAwait(false);
        }

        private async Task<int> RunRecoverSequenceAsync(
            MachineSequenceContext context,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            int placeResult = await new PickerPlaceSequence(context, _side)
                .RunAsync(ct, BuildRecoverOptions(options))
                .ConfigureAwait(false);
            if (placeResult != 0)
                return placeResult;

            return await MoveGroupToPositionAsync(
                context.Machine,
                "AvoidPosition",
                ct).ConfigureAwait(false);
        }

        private PickerSequenceOptions BuildRecoverOptions(PickerSequenceOptions source)
        {
            PickerSequenceOptions options = PickerSequenceOptions.Default();
            options.RunMode = source != null ? source.RunMode : SequenceRunMode.Auto;
            options.StartMode = source != null ? source.StartMode : SequenceStartMode.Resume;
            options.FineMove = true;
            options.MoveTimeoutMs = source != null ? source.MoveTimeoutMs : 30000;
            options.ResourceTimeoutMs = source != null ? source.ResourceTimeoutMs : 30000;
            options.PickerNo = source != null ? source.PickerNo : 0;
            options.VisionRetryCount = source != null ? source.VisionRetryCount : 3;
            options.SimulateVisionResult = source != null && source.SimulateVisionResult;
            return options;
        }

        private async Task<int> MoveGroupToPositionAsync(CDT320_Machine machine, string positionName, CancellationToken ct)
        {
            Dictionary<PickerAxis, double> targets = new Dictionary<PickerAxis, double>();
            PickerAxis[] axes = new PickerAxis[]
            {
                PickerAxis.PickerZ0,
                PickerAxis.PickerZ1,
                PickerAxis.PickerZ2,
                PickerAxis.PickerZ3,
                PickerAxis.PickerX,
                PickerAxis.PickerY,
                PickerAxis.PickerT0,
                PickerAxis.PickerT1,
                PickerAxis.PickerT2,
                PickerAxis.PickerT3
            };

            foreach (PickerAxis axis in axes)
                targets[axis] = GetTeachingPosition(machine, axis, positionName);

            ct.ThrowIfCancellationRequested();
            int result = _side == PickerSequenceSide.Front
                ? await machine.PickerFrontUnit.MovePickerAxes(targets, true, positionName).ConfigureAwait(false)
                : await machine.PickerRearUnit.MovePickerAxes(targets, true, positionName).ConfigureAwait(false);
            if (result != 0)
                return result;

            int timeout = 5000;
            bool done = _side == PickerSequenceSide.Front
                ? await machine.PickerFrontUnit.WaitPickerAxesMoveDone(targets.Keys, timeout).ConfigureAwait(false)
                : await machine.PickerRearUnit.WaitPickerAxesMoveDone(targets.Keys, timeout).ConfigureAwait(false);
            if (!done)
                return -11;

            foreach (KeyValuePair<PickerAxis, double> pair in targets)
            {
                bool inPosition = _side == PickerSequenceSide.Front
                    ? machine.PickerFrontUnit.IsPickerAxisInPosition(pair.Key, pair.Value, 10.0)
                    : machine.PickerRearUnit.IsPickerAxisInPosition(pair.Key, pair.Value, 10.0);
                if (!inPosition)
                    return -12;
            }

            return 0;
        }

        private MachineSequenceContext CreateContext(Form1 host)
        {
            return new MachineSequenceContext(host.Controller, new SequenceSignalBus());
        }

        private async Task StopManualActionAsync()
        {
            Form1 host = _getHost();
            if (host == null || host.Controller == null)
                return;

            try
            {
                host.Controller.CancelManualOperation();
                ResetStepSequence();
                CDT320_Machine machine = host.Machine;
                if (machine != null)
                {
                    if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null)
                        machine.PickerFrontUnit.StopPickerMotionAndOutputs("Manual stop");
                    if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null)
                        machine.PickerRearUnit.StopPickerMotionAndOutputs("Manual stop");
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("Manual stop failed: " + ex.Message);
            }
            finally
            {
                await Task.Delay(50).ConfigureAwait(true);
                _manualSequenceRunning = false;
                SetButtonsEnabledSafe(true);
                RefreshSafe();
            }
        }

        private void ResetStepSequence()
        {
            try
            {
                if (_stepSequence != null)
                    _stepSequence.Abort();
                if (_pickUpStepSequence != null)
                    _pickUpStepSequence.Abort();
                if (_bottomInspectStepSequence != null)
                    _bottomInspectStepSequence.Abort();
                if (_sideInspectStepSequence != null)
                    _sideInspectStepSequence.Abort();
                if (_placeStepSequence != null)
                    _placeStepSequence.Abort();
            }
            catch (Exception ex)
            {
                WriteAlarm("Step sequence reset failed: " + ex.Message);
            }
            finally
            {
                _stepSequence = null;
                _pickUpStepSequence = null;
                _bottomInspectStepSequence = null;
                _sideInspectStepSequence = null;
                _placeStepSequence = null;
            }
        }

        private static async Task WaitForCancellationAsync(CancellationToken ct)
        {
            if (!ct.CanBeCanceled)
            {
                await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
                return;
            }

            if (ct.IsCancellationRequested)
                return;

            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ct.Register(() => tcs.TrySetResult(0)))
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }

        private void ObserveManualActionTask(Task<bool> task, string actionName)
        {
            if (task == null)
                return;

            _ = ObserveManualActionTaskAsync(task, actionName);
        }

        private async Task ObserveManualActionTaskAsync(Task<bool> task, string actionName)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteEvent(actionName + " 정지 후 취소 완료.");
            }
            catch (Exception ex)
            {
                WriteAlarm(actionName + " 정지 후 종료 처리 중 오류: " + ex.Message);
            }
            finally
            {
            }
        }

        private void TrySaveMaterialStateAfterManualSequence(string actionName)
        {
            try
            {
                bool saved = MaterialStateService.TryNotifyAndSave("PickerManualSequenceFinally:" + actionName);
                if (!saved)
                    WriteAlarm(actionName + " 종료 시 Material 상태 저장 실패. 저장 파일과 Alarm/Event Log를 확인하세요.");
            }
            catch (Exception ex)
            {
                WriteAlarm(actionName + " 종료 시 Material 상태 저장 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ShowFailure(string actionName)
        {
            string message = SequenceFailureStore.BuildManualFailureMessage(
                actionName,
                actionName + " 실패\r\nAlarm/Event Log를 확인하세요.");
            QMC.Common.MessageDialog.Show(_owner, message, SideName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private bool SelectManualSequenceStartMode(string actionName, out SequenceStartMode startMode)
        {
            startMode = SequenceStartMode.Resume;

            string message =
                actionName + " 시작 방식을 선택하세요.\r\n\r\n" +
                "[예] 처음부터 시작\r\n" +
                "[아니오] 현재 스텝에서 진행";

            DialogResult result = QMC.Common.MessageDialog.Show(
                _owner,
                message,
                SideName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                startMode = SequenceStartMode.Restart;
                return true;
            }

            if (result == DialogResult.No)
            {
                startMode = SequenceStartMode.Resume;
                return true;
            }

            return false;
        }

        private string FormatStartMode(SequenceStartMode startMode)
        {
            return startMode == SequenceStartMode.Restart ? "처음부터" : "현재스텝";
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (_btnCountClear != null)
                _btnCountClear.Enabled = enabled;

            foreach (Control control in _actionControls)
            {
                if (ReferenceEquals(control, _btnStop))
                    control.Enabled = !enabled || _manualSequenceRunning;
                else
                    control.Enabled = enabled;
            }

            _btnStop.Enabled = !enabled || _manualSequenceRunning;
        }

        private void SetButtonsEnabledSafe(bool enabled)
        {
            try
            {
                if (_owner == null || _owner.IsDisposed)
                    return;

                if (_owner.InvokeRequired)
                {
                    _owner.Invoke(new Action(() => SetButtonsEnabled(enabled)));
                    return;
                }

                SetButtonsEnabled(enabled);
            }
            catch (Exception ex)
            {
                WriteAlarm("메뉴얼 시퀀스 버튼 상태 복구 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void RefreshSafe()
        {
            try
            {
                if (_owner == null || _owner.IsDisposed)
                    return;

                if (_owner.InvokeRequired)
                {
                    _owner.BeginInvoke(new Action(Refresh));
                    return;
                }

                Refresh();
            }
            catch (Exception ex)
            {
                WriteAlarm("메뉴얼 시퀀스 화면 갱신 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private string ResolveHeadState(CDT320_Machine machine, int pickerNo, bool flow)
        {
            return ResolveHeadState(machine, pickerNo, flow, null);
        }

        private string ResolveHeadState(CDT320_Machine machine, int pickerNo, bool flow, DieMaterial pickedDie)
        {
            if (!UsePicker(machine, pickerNo))
                return "DISABLE";

            DieMaterial die = pickedDie ?? GetPickedDieMaterial(pickerNo);
            if (die != null)
                return "PICK / " + FormatDieShortId(die.DieId);

            return flow ? "PICK(SENSOR)" : "EMPTY";
        }

        private static bool IsPickedHeadState(string headState)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(headState) &&
                       headState.StartsWith("PICK", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private DieMaterial GetPickedDieMaterial(int pickerNo)
        {
            try
            {
                MaterialLocationKind location = _side == PickerSequenceSide.Front
                    ? MaterialLocationKind.PickerFront
                    : MaterialLocationKind.PickerRear;

                return MaterialStateService.GetDieAtPicker(location, pickerNo);
            }
            catch (Exception ex)
            {
                WriteAlarm("픽커 Material 상태 조회 실패: pickerNo=" + pickerNo + ", error=" + ex.Message);
                return null;
            }
            finally
            {
            }
        }

        private static string FormatDieShortId(string dieId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dieId))
                    return "-";

                string value = dieId.Trim();
                int dieMarker = value.LastIndexOf("-D", StringComparison.OrdinalIgnoreCase);
                if (dieMarker >= 0 && dieMarker + 1 < value.Length)
                    return value.Substring(dieMarker + 1);

                return value.Length > 12 ? value.Substring(value.Length - 12) : value;
            }
            catch
            {
                return dieId ?? "-";
            }
            finally
            {
            }
        }

        private string ResolveHeadZone(CDT320_Machine machine)
        {
            try
            {
                if (machine == null)
                    return "-";

                string physicalZone = ResolveEncoderHeadZone(machine);
                return string.IsNullOrWhiteSpace(physicalZone) ? "UNKNOWN" : physicalZone;
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker Head Zone 판정 실패: " + ex.Message);
                return "UNKNOWN";
            }
            finally
            {
            }
        }

        private string ResolveEncoderHeadZone(CDT320_Machine machine)
        {
            try
            {
                if (machine == null)
                    return "UNKNOWN";

                string physicalZone = PickerZoneInterlockRules.ResolvePickerPhysicalZoneName(
                    machine,
                    _side == PickerSequenceSide.Front);

                return string.IsNullOrWhiteSpace(physicalZone) ? "UNKNOWN" : physicalZone;
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker Head Zone 엔코더 보조 판정 실패: " + ex.Message);
                return "UNKNOWN";
            }
            finally
            {
            }
        }

        private string ResolveActivePickerWorkZone()
        {
            try
            {
                PickerWorkZone zone;
                string owner;
                bool active = PickerZoneInterlockRules.TryGetPickerWorkArea(
                    _side == PickerSequenceSide.Front,
                    out zone,
                    out owner);

                if (!active)
                    return string.Empty;

                switch (zone)
                {
                    case PickerWorkZone.Input:
                        return "PICK";
                    case PickerWorkZone.Bottom:
                        return "INSPECT_B";
                    case PickerWorkZone.Side:
                        return "INSPECT_S";
                    case PickerWorkZone.Output:
                        return "PLACE";
                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker Head Zone 작업영역 판정 실패: " + ex.Message);
                return string.Empty;
            }
            finally
            {
            }
        }

        private bool IsPickerXInZone(CDT320_Machine machine, string positionName)
        {
            try
            {
                BaseAxis pickerX = GetAxis(machine, PickerAxis.PickerX);
                if (pickerX == null)
                    return false;

                double target = GetTeachingPosition(machine, PickerAxis.PickerX, positionName);
                double tolerance = pickerX.Config != null ? pickerX.Config.InPositionTolerance : 0.05;
                tolerance = Math.Max(tolerance, 0.05);

                return Math.Abs(pickerX.ActualPosition - target) <= tolerance;
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker Head Zone X축 위치 판정 실패: position=" + positionName + ", error=" + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private bool IsAnyPickerInDieBottomZone(CDT320_Machine machine)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.IsPickerInDieBottomZone(pickerNo))
                    return true;
                if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.IsPickerInDieBottomZone(pickerNo))
                    return true;
            }

            return false;
        }

        private bool IsAnyPickerInDieSideZone(CDT320_Machine machine)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.IsPickerInDieSideZone(pickerNo))
                    return true;
                if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.IsPickerInDieSideZone(pickerNo))
                    return true;
            }

            return false;
        }

        private bool IsAnyPickerInDiePickPosition(CDT320_Machine machine)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.IsPickerInDiePickPosition(pickerNo))
                    return true;
                if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.IsPickerInDiePickPosition(pickerNo))
                    return true;
            }

            return false;
        }

        private bool IsAnyPickerInDiePlacePosition(CDT320_Machine machine)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.IsPickerInDiePlacePosition(pickerNo))
                    return true;
                if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.IsPickerInDiePlacePosition(pickerNo))
                    return true;
            }

            return false;
        }

        private void SetHeadZone(string zone)
        {
            if (_lblHeadZoneValue == null)
                return;

            _lblHeadZoneValue.Text = string.IsNullOrEmpty(zone) ? "-" : zone;
            _lblHeadZoneValue.ForeColor = Color.White;

            switch (_lblHeadZoneValue.Text)
            {
                // 인풋 영역 표시
                case "INPUT":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(0, 128, 192);
                    break;
                // 작업 영역 표시
                case "PICK":
                case "INSPECT_B":
                case "INSPECT_S":
                case "PLACE":
                case "OUTPUT":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(217, 119, 6);
                    break;
                // Avoid 영역 표시
                case "AVOID":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(0, 176, 80);
                    break;
                // 이동 중 상태 표시
                case "MOVING":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(255, 192, 0);
                    _lblHeadZoneValue.ForeColor = Color.Black;
                    break;
                // 위치 미확인 상태 표시
                case "UNKNOWN":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(160, 160, 160);
                    break;
                default:
                    _lblHeadZoneValue.BackColor = Color.White;
                    _lblHeadZoneValue.ForeColor = Color.Black;
                    break;
            }
        }

        private string ResolveHeadProcess(CDT320_Machine machine, string encoderZone)
        {
            try
            {
                string activeWorkZone = ResolveActivePickerWorkZone();
                if (!string.IsNullOrWhiteSpace(activeWorkZone))
                    return ResolveHeadProcessFromZone(activeWorkZone);

                if (IsPickerMoving(machine))
                    return "MOVING";

                return ResolveHeadProcessFromZone(encoderZone);
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker 공정 상태 판정 실패: " + ex.Message);
                return "UNKNOWN";
            }
            finally
            {
            }
        }

        private string ResolveHeadProcessFromZone(string zone)
        {
            switch (zone)
            {
                case "AVOID":
                    return "AVOID";
                case "INPUT":
                case "PICK":
                    return "PICKUP";
                case "INSPECT_B":
                    return "INSPECT_B";
                case "INSPECT_S":
                    return "INSPECT_S";
                case "PLACE":
                case "OUTPUT":
                    return "PLACE";
                case "MOVING":
                    return "MOVING";
                case "UNKNOWN":
                    return "UNKNOWN";
                default:
                    return "-";
            }
        }

        private void SetHeadProcess(string process)
        {
            if (_lblHeadProcessValue == null)
                return;

            _lblHeadProcessValue.Text = string.IsNullOrEmpty(process) ? "-" : process;
            _lblHeadProcessValue.ForeColor = Color.White;

            switch (_lblHeadProcessValue.Text)
            {
                case "PICKUP":
                    _lblHeadProcessValue.BackColor = Color.FromArgb(0, 128, 192);
                    break;
                case "INSPECT_B":
                case "INSPECT_S":
                    _lblHeadProcessValue.BackColor = Color.FromArgb(217, 119, 6);
                    break;
                case "PLACE":
                    _lblHeadProcessValue.BackColor = Color.FromArgb(139, 92, 246);
                    break;
                case "AVOID":
                    _lblHeadProcessValue.BackColor = Color.FromArgb(0, 176, 80);
                    break;
                case "MOVING":
                    _lblHeadProcessValue.BackColor = Color.FromArgb(255, 192, 0);
                    _lblHeadProcessValue.ForeColor = Color.Black;
                    break;
                case "UNKNOWN":
                    _lblHeadProcessValue.BackColor = Color.FromArgb(160, 160, 160);
                    break;
                default:
                    _lblHeadProcessValue.BackColor = Color.White;
                    _lblHeadProcessValue.ForeColor = Color.Black;
                    break;
            }
        }

        private bool IsPickerMoving(CDT320_Machine machine)
        {
            try
            {
                PickerAxis[] axes = new PickerAxis[]
                {
                    PickerAxis.PickerX,
                    PickerAxis.PickerY,
                    PickerAxis.PickerT0,
                    PickerAxis.PickerT1,
                    PickerAxis.PickerT2,
                    PickerAxis.PickerT3,
                    PickerAxis.PickerZ0,
                    PickerAxis.PickerZ1,
                    PickerAxis.PickerZ2,
                    PickerAxis.PickerZ3
                };

                foreach (PickerAxis axis in axes)
                {
                    BaseAxis item = GetAxis(machine, axis);
                    if (item != null && item.IsMoving)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker 공정 이동 상태 판정 실패: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private string ResolveProcessDetail(CDT320_Machine machine, string process, bool moving)
        {
            string flowProcess = NormalizeFlowProcess(process);
            string encoderZone = ResolveEncoderHeadZone(machine);
            string encoderText = string.IsNullOrWhiteSpace(encoderZone)
                ? string.Empty
                : " / ENC=" + encoderZone;

            if (string.IsNullOrWhiteSpace(flowProcess) || flowProcess == "-")
                return string.IsNullOrWhiteSpace(encoderText) ? "-" : encoderText.TrimStart(' ', '/');

            if (flowProcess == "UNKNOWN")
                return "현재 위치 확인 필요" + encoderText;

            if (moving)
            {
                if (IsStableProcess(flowProcess) &&
                    IsStableProcess(_lastStableProcess) &&
                    !string.Equals(_lastStableProcess, flowProcess, StringComparison.OrdinalIgnoreCase))
                    return _lastStableProcess + " -> " + flowProcess + " 이동 중" + encoderText;

                return flowProcess + " 위치 이동 중" + encoderText;
            }

            switch (flowProcess)
            {
                case "AVOID":
                    return "AVOID 대기" + encoderText;
                case "PICKUP":
                    return "PICKUP 공정 진행 중" + encoderText;
                case "BOTTOM":
                    return "BOTTOM 검사 진행 중" + encoderText;
                case "SIDE":
                    return "SIDE 검사 진행 중" + encoderText;
                case "PLACE":
                    return "PLACE 공정 진행 중" + encoderText;
                default:
                    return flowProcess + encoderText;
            }
        }

        private void SetProcessFlow(string process)
        {
            string active = NormalizeFlowProcess(process);
            for (int i = 0; i < _processFlowLabels.Length; i++)
            {
                Label label = _processFlowLabels[i];
                if (label == null)
                    continue;

                bool selected = string.Equals(label.Text, active, StringComparison.OrdinalIgnoreCase);
                label.BackColor = selected ? ResolveProcessColor(active) : Color.FromArgb(105, 105, 105);
                label.ForeColor = selected && active == "SIDE" ? Color.Black : Color.White;
            }
        }

        private void SetProcessDetail(string detail)
        {
            if (_lblProcessDetailValue == null)
                return;

            _lblProcessDetailValue.Text = string.IsNullOrWhiteSpace(detail) ? "-" : detail;
            _lblProcessDetailValue.ForeColor = Color.Black;
            _lblProcessDetailValue.BackColor = detail != null && detail.Contains("이동 중")
                ? Color.FromArgb(255, 242, 204)
                : Color.White;
        }

        private string NormalizeFlowProcess(string process)
        {
            switch (process)
            {
                case "INPUT":
                case "PICK":
                case "PICKUP":
                    return "PICKUP";
                case "INSPECT":
                case "INSPECT_B":
                case "BOTTOM":
                    return "BOTTOM";
                case "INSPECT_S":
                case "SIDE":
                    return "SIDE";
                case "OUTPUT":
                case "PLACE":
                    return "PLACE";
                case "AVOID":
                case "UNKNOWN":
                case "MOVING":
                case "-":
                    return process;
                default:
                    return string.IsNullOrWhiteSpace(process) ? "-" : process;
            }
        }

        private bool IsStableProcess(string process)
        {
            string value = NormalizeFlowProcess(process);
            return value == "AVOID" ||
                   value == "PICKUP" ||
                   value == "BOTTOM" ||
                   value == "SIDE" ||
                   value == "PLACE";
        }

        private Color ResolveProcessColor(string process)
        {
            switch (process)
            {
                case "AVOID":
                    return Color.FromArgb(0, 176, 80);
                case "PICKUP":
                    return Color.FromArgb(0, 128, 192);
                case "BOTTOM":
                    return Color.FromArgb(217, 119, 6);
                case "SIDE":
                    return Color.FromArgb(255, 192, 0);
                case "PLACE":
                    return Color.FromArgb(139, 92, 246);
                default:
                    return Color.FromArgb(160, 160, 160);
            }
        }

        private bool IsGroupInPosition(CDT320_Machine machine, string positionName)
        {
            PickerAxis[] axes = new PickerAxis[] { PickerAxis.PickerX, PickerAxis.PickerY, PickerAxis.PickerT0, PickerAxis.PickerT1, PickerAxis.PickerT2, PickerAxis.PickerT3, PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            foreach (PickerAxis axis in axes)
            {
                double target = GetTeachingPosition(machine, axis, positionName);
                BaseAxis item = GetAxis(machine, axis);
                if (item == null || Math.Abs(item.ActualPosition - target) > 10.0)
                    return false;
            }
            return true;
        }

        private BaseAxis GetAxis(CDT320_Machine machine, PickerAxis axis)
        {
            if (machine == null)
                return null;
            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.Axes.ContainsKey(axis))
                return machine.PickerFrontUnit.Axes[axis];
            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.Axes.ContainsKey(axis))
                return machine.PickerRearUnit.Axes[axis];
            return null;
        }

        private double GetTeachingPosition(CDT320_Machine machine, PickerAxis axis, string positionName)
        {
            if (_side == PickerSequenceSide.Front)
                return machine.PickerFrontUnit.GetPickerTeachingPosition(axis, positionName);
            return machine.PickerRearUnit.GetPickerTeachingPosition(axis, positionName);
        }

        private PickerAlignOffset GetRuntimePickerOffset(CDT320_Machine machine, int pickerIndex)
        {
            if (machine == null || pickerIndex < 0)
                return null;

            if (_side == PickerSequenceSide.Front)
            {
                if (machine.PickerFrontUnit == null)
                    return null;

                return machine.PickerFrontUnit.GetRuntimePickerOffset(pickerIndex);
            }

            if (machine.PickerRearUnit == null)
                return null;

            return machine.PickerRearUnit.GetRuntimePickerOffset(pickerIndex);
        }

        private bool UsePicker(CDT320_Machine machine, int pickerNo)
        {
            int index = Math.Max(0, pickerNo - 1);
            if (_side == PickerSequenceSide.Front)
                return machine.PickerFrontUnit != null && machine.PickerFrontUnit.Config != null && machine.PickerFrontUnit.Config.UsePicker != null && index < machine.PickerFrontUnit.Config.UsePicker.Length && machine.PickerFrontUnit.Config.UsePicker[index];
            return machine.PickerRearUnit != null && machine.PickerRearUnit.Config != null && machine.PickerRearUnit.Config.UsePicker != null && index < machine.PickerRearUnit.Config.UsePicker.Length && machine.PickerRearUnit.Config.UsePicker[index];
        }

        private int GetColletUseCount(CDT320_Machine machine, int pickerNo)
        {
            int index = Math.Max(0, pickerNo - 1);
            if (_side == PickerSequenceSide.Front &&
                machine.PickerFrontUnit != null &&
                machine.PickerFrontUnit.ColletUseCounts != null &&
                index < machine.PickerFrontUnit.ColletUseCounts.Length)
            {
                return machine.PickerFrontUnit.ColletUseCounts[index];
            }

            if (_side == PickerSequenceSide.Rear &&
                machine.PickerRearUnit != null &&
                machine.PickerRearUnit.ColletUseCounts != null &&
                index < machine.PickerRearUnit.ColletUseCounts.Length)
            {
                return machine.PickerRearUnit.ColletUseCounts[index];
            }

            return 0;
        }

        private int GetPickFailCount(CDT320_Machine machine)
        {
            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null)
                return machine.PickerFrontUnit.PickFailCount;
            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null)
                return machine.PickerRearUnit.PickFailCount;
            return 0;
        }

        private int GetPlaceFailCount(CDT320_Machine machine)
        {
            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null)
                return machine.PickerFrontUnit.PlaceFailCount;
            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null)
                return machine.PickerRearUnit.PlaceFailCount;
            return 0;
        }

        private BaseDigitalInput GetFlow(CDT320_Machine machine, int pickerNo)
        {
            int index = Math.Max(0, pickerNo - 1);
            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.FlowChecks != null && index < machine.PickerFrontUnit.FlowChecks.Length)
                return machine.PickerFrontUnit.FlowChecks[index];
            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.FlowChecks != null && index < machine.PickerRearUnit.FlowChecks.Length)
                return machine.PickerRearUnit.FlowChecks[index];
            return null;
        }

        private BaseDigitalOutput GetVacuum(CDT320_Machine machine, int pickerNo)
        {
            int index = Math.Max(0, pickerNo - 1);
            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.Vacuums != null && index < machine.PickerFrontUnit.Vacuums.Length)
                return machine.PickerFrontUnit.Vacuums[index];
            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.Vacuums != null && index < machine.PickerRearUnit.Vacuums.Length)
                return machine.PickerRearUnit.Vacuums[index];
            return null;
        }

        private BaseDigitalOutput GetBlow(CDT320_Machine machine, int pickerNo)
        {
            int index = Math.Max(0, pickerNo - 1);
            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.Blows != null && index < machine.PickerFrontUnit.Blows.Length)
                return machine.PickerFrontUnit.Blows[index];
            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.Blows != null && index < machine.PickerRearUnit.Blows.Length)
                return machine.PickerRearUnit.Blows[index];
            return null;
        }

        private BaseDigitalInput GetCdaPressure(CDT320_Machine machine)
        {
            if (_side == PickerSequenceSide.Front)
                return machine.PickerFrontUnit != null ? machine.PickerFrontUnit.CdaTankPressureCheck : null;
            return machine.PickerRearUnit != null ? machine.PickerRearUnit.CdaTankPressureCheck : null;
        }

        private BaseDigitalInput GetVacuumPressure(CDT320_Machine machine)
        {
            if (_side == PickerSequenceSide.Front)
                return machine.PickerFrontUnit != null ? machine.PickerFrontUnit.VacuumTankPressureCheck : null;
            return machine.PickerRearUnit != null ? machine.PickerRearUnit.VacuumTankPressureCheck : null;
        }

        private static bool ReadInput(BaseDigitalInput input)
        {
            return input != null && input.IsOn;
        }

        private static bool ReadOutput(BaseDigitalOutput output)
        {
            return output != null && output.IsOn;
        }

        private static string FormatAxis(BaseAxis axis, string format, string unit)
        {
            if (axis == null)
                return "-";
            return axis.ActualPosition.ToString(format) + " " + unit;
        }

        private static void SetDot(IndicatorDot dot, bool on)
        {
            if (dot == null)
                return;
            dot.IsOn = on;
        }

        private void WriteEvent(string message)
        {
            EventLogger.Write(EventKind.Event, "QMC", LogCode, message);
        }

        private void WriteAlarm(string message)
        {
            EventLogger.Write(EventKind.Alarm, "QMC", LogCode, message);
        }
    }
}
