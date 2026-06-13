using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Controls;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
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
        private readonly Label[] _colletUseTitleLabels;
        private readonly Label[] _colletUseValueLabels;
        private readonly IndicatorDot[] _vacuumDots;
        private readonly IndicatorDot[] _blowDots;
        private readonly Label[] _vacuumLabels;
        private readonly Label[] _blowLabels;
        private readonly DataGridView _axisGrid;
        private readonly Button _btnCountClear;
        private readonly ActionButton _btnProcess;
        private readonly ActionButton _btnStep;
        private readonly ActionButton _btnInput;
        private readonly ActionButton _btnInspect;
        private readonly ActionButton _btnOutput;
        private readonly ActionButton _btnAvoid;
        private readonly ActionButton _btnStop;
        private readonly Control.ControlCollection _actionControls;
        private readonly System.Windows.Forms.Timer _timer;
        private PickerProcessSequence _stepSequence;
        private bool _manualSequenceRunning;

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
            Label[] colletUseTitleLabels,
            Label[] colletUseValueLabels,
            IndicatorDot[] vacuumDots,
            IndicatorDot[] blowDots,
            Label[] vacuumLabels,
            Label[] blowLabels,
            DataGridView axisGrid,
            Button btnCountClear,
            ActionButton btnProcess,
            ActionButton btnStep,
            ActionButton btnInput,
            ActionButton btnInspect,
            ActionButton btnOutput,
            ActionButton btnAvoid,
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
            _colletUseTitleLabels = colletUseTitleLabels ?? new Label[0];
            _colletUseValueLabels = colletUseValueLabels ?? new Label[0];
            _vacuumDots = vacuumDots ?? new IndicatorDot[0];
            _blowDots = blowDots ?? new IndicatorDot[0];
            _vacuumLabels = vacuumLabels ?? new Label[0];
            _blowLabels = blowLabels ?? new Label[0];
            _axisGrid = axisGrid;
            _btnCountClear = btnCountClear;
            _btnProcess = btnProcess;
            _btnStep = btnStep;
            _btnInput = btnInput;
            _btnInspect = btnInspect;
            _btnOutput = btnOutput;
            _btnAvoid = btnAvoid;
            _btnStop = btnStop;
            _actionControls = actionControls;

            WireEvents();
            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) => Refresh();
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
            _btnProcess.Click += async (s, e) => await RunSequenceAction(SideName + " PROCESS", SequenceRunMode.Auto);
            _btnStep.Click += async (s, e) => await RunSequenceAction(SideName + " STEP", SequenceRunMode.Step);
            _btnInput.Click += async (s, e) => await RunMoveAction(SideName + " INPUT POSITION", "InputAvoidPosition");
            _btnInspect.Click += async (s, e) => await RunMoveAction(SideName + " INSPECT POSITION", "BottomPosition");
            _btnOutput.Click += async (s, e) => await RunMoveAction(SideName + " OUTPUT POSITION", "OutputAvoidPosition");
            _btnAvoid.Click += async (s, e) => await RunMoveAction(SideName + " AVOID POSITION", "AvoidPosition");
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
        }

        private void RefreshSummary(CDT320_Machine machine)
        {
            bool cdaOk = ReadInput(GetCdaPressure(machine));
            bool vacuumOk = ReadInput(GetVacuumPressure(machine));

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                int index = pickerNo - 1;
                bool flow = ReadInput(GetFlow(machine, pickerNo));
                bool vacuum = ReadOutput(GetVacuum(machine, pickerNo));
                bool blow = ReadOutput(GetBlow(machine, pickerNo));

                if (index < _headValueLabels.Length && _headValueLabels[index] != null)
                {
                    _headValueLabels[index].Text = ResolveHeadState(machine, pickerNo, flow);
                    _headValueLabels[index].ForeColor = flow ? Color.Lime : Color.Black;
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
                    _vacuumLabels[index].Text = "HEAD VACUUM #" + pickerNo + " : " + (vacuum || flow ? "ON" : "OFF");
                if (index < _blowLabels.Length && _blowLabels[index] != null)
                    _blowLabels[index].Text = "HEAD BLOW #" + pickerNo + " : " + (blow ? "ON" : "OFF");

                if (index < _vacuumDots.Length)
                    SetDot(_vacuumDots[index], vacuum || flow);
                if (index < _blowDots.Length)
                    SetDot(_blowDots[index], blow);
            }

            _lblColletChangeValue.Text = cdaOk ? "READY" : "CHECK";
            _lblAutoPosValue.Text = IsGroupInPosition(machine, "AvoidPosition") ? "AVOID" : "MOVING";
            _lblColletCleaningValue.Text = vacuumOk ? "READY" : "CHECK";
            _lblColletCheckValue.Text = cdaOk && vacuumOk ? "READY" : "CHECK";
            _lblPickFailValue.Text = GetPickFailCount(machine) + " ea";
            _lblPlaceFailValue.Text = GetPlaceFailCount(machine) + " ea";
            SetHeadZone(ResolveHeadZone(machine));
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

            if (_axisGrid.Rows.Count == 0)
            {
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
                row.Cells["colServo"].Value = axis != null && axis.IsServoOn ? "ON" : "OFF";
                row.Cells["colHome"].Value = axis != null && axis.IsHomeDone ? "ON" : "OFF";
                row.Cells["colAlarm"].Value = axis != null && axis.IsAlarm ? "ON" : "OFF";
                row.Cells["colMoving"].Value = axis != null && axis.IsMoving ? "ON" : "OFF";
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
            catch
            {
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

        private async Task RunSequenceAction(string actionName, SequenceRunMode mode)
        {
            IDisposable manualScope = null;
            try
            {
                Form1 host = _getHost();
                if (host == null || host.Controller == null)
                    return;
                if (_manualSequenceRunning)
                    return;
                if (!ConfirmAction(actionName))
                    return;

                _manualSequenceRunning = true;
                SetButtonsEnabled(false);
                manualScope = host.Controller.EnterManualOperation();
                CancellationToken manualToken = host.Controller.ManualOperationToken;
                SequenceFailureStore.Clear();
                WriteEvent(actionName + " start");

                Task<bool> actionTask = RunPickerSequenceTask(CreateContext(host), mode, manualToken);
                Task cancelTask = WaitForCancellationAsync(manualToken);
                Task completed = await Task.WhenAny(actionTask, cancelTask).ConfigureAwait(true);
                if (completed == cancelTask)
                {
                    ObserveManualActionTask(actionTask, actionName);
                    WriteEvent(actionName + " canceled by stop.");
                    return;
                }

                bool ok = await actionTask.ConfigureAwait(true);
                WriteEvent(actionName + " result=" + ok);
                if (!ok)
                    ShowFailure(actionName);
            }
            catch (OperationCanceledException)
            {
                WriteEvent(actionName + " canceled.");
            }
            catch (Exception ex)
            {
                WriteAlarm(actionName + " failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(_owner, ex.Message, SideName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (manualScope != null)
                    manualScope.Dispose();
                _manualSequenceRunning = false;
                SetButtonsEnabled(true);
                Refresh();
            }
        }

        private async Task<bool> RunPickerSequenceTask(MachineSequenceContext context, SequenceRunMode mode, CancellationToken ct)
        {
            try
            {
                PickerSequenceOptions options = PickerSequenceOptions.Default();
                options.RunMode = mode;

                if (mode == SequenceRunMode.Step)
                {
                    if (_stepSequence == null || _stepSequence.IsComplete)
                        _stepSequence = new PickerProcessSequence(context, _side);

                    int stepResult = await _stepSequence.RunAsync(ct, options).ConfigureAwait(false);
                    if (_stepSequence.IsComplete)
                        _stepSequence = null;
                    return stepResult == 0;
                }

                ResetStepSequence();
                int result = await new PickerProcessSequence(context, _side).RunAsync(ct, options).ConfigureAwait(false);
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

        private async Task RunMoveAction(string actionName, string positionName)
        {
            IDisposable manualScope = null;
            try
            {
                Form1 host = _getHost();
                if (host == null || host.Controller == null || host.Machine == null)
                    return;
                if (_manualSequenceRunning)
                    return;
                if (!ConfirmAction(actionName))
                    return;

                _manualSequenceRunning = true;
                SetButtonsEnabled(false);
                manualScope = host.Controller.EnterManualOperation();
                CancellationToken token = host.Controller.ManualOperationToken;
                WriteEvent(actionName + " start");
                int result = await MoveGroupToPositionAsync(host.Machine, positionName, token).ConfigureAwait(true);
                WriteEvent(actionName + " result=" + result);
                if (result != 0)
                    QMC.Common.MessageDialog.Show(_owner, actionName + " 실패. result=" + result, SideName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (OperationCanceledException)
            {
                WriteEvent(actionName + " canceled.");
            }
            catch (Exception ex)
            {
                WriteAlarm(actionName + " failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(_owner, ex.Message, SideName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (manualScope != null)
                    manualScope.Dispose();
                _manualSequenceRunning = false;
                SetButtonsEnabled(true);
                Refresh();
            }
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
                ? await machine.PickerFrontUnit.MovePickerAxes(targets, true).ConfigureAwait(false)
                : await machine.PickerRearUnit.MovePickerAxes(targets, true).ConfigureAwait(false);
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
                SetButtonsEnabled(true);
            }
        }

        private void ResetStepSequence()
        {
            try
            {
                if (_stepSequence != null)
                    _stepSequence.Abort();
            }
            catch (Exception ex)
            {
                WriteAlarm("Step sequence reset failed: " + ex.Message);
            }
            finally
            {
                _stepSequence = null;
            }
        }

        private static Task WaitForCancellationAsync(CancellationToken ct)
        {
            if (!ct.CanBeCanceled)
                return Task.Delay(Timeout.Infinite);
            if (ct.IsCancellationRequested)
                return Task.FromResult(0);

            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            ct.Register(() => tcs.TrySetResult(0));
            return tcs.Task;
        }

        private void ObserveManualActionTask(Task<bool> task, string actionName)
        {
            if (task == null)
                return;

            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception != null ? t.Exception.GetBaseException() : null;
                    WriteAlarm(actionName + " finished after stop: " + (ex != null ? ex.Message : "unknown"));
                }
            });
        }

        private void ShowFailure(string actionName)
        {
            string message = SequenceFailureStore.BuildManualFailureMessage(
                actionName,
                actionName + " 실패\r\nAlarm/Event Log를 확인하세요.");
            QMC.Common.MessageDialog.Show(_owner, message, SideName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private bool ConfirmAction(string actionName)
        {
            return QMC.Common.MessageDialog.Show(_owner, actionName + " 진행하시겠습니까?", SideName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
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

        private string ResolveHeadState(CDT320_Machine machine, int pickerNo, bool flow)
        {
            if (!UsePicker(machine, pickerNo))
                return "DISABLE";
            return flow ? "PICK" : "EMPTY";
        }

        private string ResolveHeadZone(CDT320_Machine machine)
        {
            if (machine == null)
                return "-";

            BaseAxis pickerX = GetAxis(machine, PickerAxis.PickerX);
            BaseAxis pickerY = GetAxis(machine, PickerAxis.PickerY);
            if (pickerX == null)
                return "-";

            if (pickerX.IsMoving || (pickerY != null && pickerY.IsMoving))
                return "MOVING";

            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null)
            {
                if (machine.PickerFrontUnit.IsPickerInLoadPosition())
                    return "INPUT";
                if (IsAnyPickerInDiePickPosition(machine))
                    return "PICK";
                if (IsAnyPickerInDieProcessPosition(machine))
                    return "INSPECT";
                if (IsAnyPickerInDiePlacePosition(machine))
                    return "PLACE";
                if (machine.PickerFrontUnit.IsPickerInUnloadPosition())
                    return "OUTPUT";
                if (machine.PickerFrontUnit.IsPickerInAvoidPosition())
                    return "AVOID";
            }

            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null)
            {
                if (machine.PickerRearUnit.IsPickerInLoadPosition())
                    return "INPUT";
                if (IsAnyPickerInDiePickPosition(machine))
                    return "PICK";
                if (IsAnyPickerInDieProcessPosition(machine))
                    return "INSPECT";
                if (IsAnyPickerInDiePlacePosition(machine))
                    return "PLACE";
                if (machine.PickerRearUnit.IsPickerInUnloadPosition())
                    return "OUTPUT";
                if (machine.PickerRearUnit.IsPickerInAvoidPosition())
                    return "AVOID";
            }

            return "UNKNOWN";
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

        private bool IsAnyPickerInDieProcessPosition(CDT320_Machine machine)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.IsPickerInDieProcessPosition(pickerNo))
                    return true;
                if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.IsPickerInDieProcessPosition(pickerNo))
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
                case "INPUT":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(0, 128, 192);
                    break;
                case "PICK":
                case "INSPECT":
                case "PLACE":
                case "OUTPUT":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(217, 119, 6);
                    break;
                case "AVOID":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(0, 176, 80);
                    break;
                case "MOVING":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(255, 192, 0);
                    _lblHeadZoneValue.ForeColor = Color.Black;
                    break;
                case "UNKNOWN":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(160, 160, 160);
                    break;
                default:
                    _lblHeadZoneValue.BackColor = Color.White;
                    _lblHeadZoneValue.ForeColor = Color.Black;
                    break;
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
