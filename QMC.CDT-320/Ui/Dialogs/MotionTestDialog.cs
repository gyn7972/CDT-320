using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.Common;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class MotionTestDialog : Form
    {
        private const int PositionRefreshMs = 200;
        private const int DefaultSettleMs = 20;

        private readonly List<BaseAxis> _axes;
        private readonly System.Windows.Forms.Timer _positionTimer = new System.Windows.Forms.Timer();
        private CancellationTokenSource _repeatCts;
        private Task _repeatTask;
        private int _completedLegs;
        private bool _bindingProfile;

        public MotionTestDialog(IEnumerable<BaseAxis> axes)
        {
            try
            {
                _axes = SortAxes(axes).ToList();
                InitializeComponent();
                InitializeRuntimeUi();
                BindAxes();
                WireEvents();

                _positionTimer.Interval = PositionRefreshMs;
                _positionTimer.Tick += (s, e) => RefreshAxisState();
                Load += (s, e) =>
                {
                    ReloadSelectedAxisDefaults();
                    _positionTimer.Start();
                };
                VisibleChanged += (s, e) =>
                {
                    if (Visible)
                        ReloadSelectedAxisDefaults();
                };
                FormClosing += MotionTestDialog_FormClosing;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "MOTION-TEST", "MotionTestDialog initialize failed: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private BaseAxis SelectedAxis
        {
            get
            {
                AxisListItem item = lstAxes.SelectedItem as AxisListItem;
                return item != null ? item.Axis : null;
            }
        }

        private bool IsRunning
        {
            get { return _repeatTask != null && !_repeatTask.IsCompleted; }
        }

        private static IEnumerable<BaseAxis> SortAxes(IEnumerable<BaseAxis> axes)
        {
            try
            {
                return (axes ?? Enumerable.Empty<BaseAxis>())
                    .Where(a => a != null)
                    .OrderBy(a => a.Setup != null && a.Setup.AxisNo >= 0 ? a.Setup.AxisNo : int.MaxValue)
                    .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return Enumerable.Empty<BaseAxis>();
            }
            finally
            {
            }
        }

        private void InitializeRuntimeUi()
        {
            try
            {
                gridProfile.Rows.Clear();
                gridProfile.Rows.Add("Velocity", "0", "");
                gridProfile.Rows.Add("Acceleration", "0", "");
                gridProfile.Rows.Add("Deceleration", "0", "");
                gridProfile.ClearSelection();
                lblStatus.Text = "Ready";
                UpdateRunButtons();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "MOTION-TEST", "Motion test UI initialize failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void BindAxes()
        {
            try
            {
                lstAxes.Items.Clear();
                foreach (BaseAxis axis in _axes)
                    lstAxes.Items.Add(new AxisListItem(axis));

                if (lstAxes.Items.Count > 0)
                    lstAxes.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "MOTION-TEST", "Axis bind failed: " + ex.Message);
                MessageDialog.Show(this, ex.Message, "Motion Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private void WireEvents()
        {
            try
            {
                lstAxes.SelectedIndexChanged += (s, e) =>
                {
                    if (IsRunning)
                    {
                        MessageDialog.Show(this, "Motion test is running. Stop first.", "Motion Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    ReloadSelectedAxisDefaults();
                };

                btnReloadDefault.Click += (s, e) => ReloadSelectedAxisDefaults();
                btnCaptureStart.Click += (s, e) => CaptureCurrentPosition(nudStartPosition);
                btnCaptureEnd.Click += (s, e) => CaptureCurrentPosition(nudEndPosition);
                btnSwap.Click += (s, e) => SwapStartEnd();
                btnMoveStart.Click += async (s, e) => await MoveSingleAsync(nudStartPosition.Value, "MoveStart");
                btnMoveEnd.Click += async (s, e) => await MoveSingleAsync(nudEndPosition.Value, "MoveEnd");
                btnServoOn.Click += (s, e) => ServoOnSelectedAxis();
                btnStop.Click += (s, e) => RequestStop(true);
                btnStartRepeat.Click += (s, e) => StartRepeat();
                btnClose.Click += (s, e) => Close();
                gridProfile.CellEndEdit += (s, e) => ValidateProfileGrid();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "MOTION-TEST", "Event bind failed: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private void ReloadSelectedAxisDefaults()
        {
            try
            {
                BaseAxis axis = SelectedAxis;
                if (axis == null)
                {
                    lblAxisName.Text = "-";
                    lblUnit.Text = "-";
                    SetProfileValues(0, 0, 0, "");
                    return;
                }

                RefreshAxisState();
                string unit = AxisUnitConverter.DisplayUnitFor(axis);
                lblAxisName.Text = DisplayNameWithNo(axis);
                lblUnit.Text = unit;
                nudStartPosition.Value = ClampDecimal(ToDecimal(AxisUnitConverter.ToDisplay(axis.ActualPosition, axis)), nudStartPosition);
                nudEndPosition.Value = ClampDecimal(ToDecimal(AxisUnitConverter.ToDisplay(axis.ActualPosition, axis)), nudEndPosition);

                AxisConfig config = axis.Config;
                SetProfileValues(
                    config != null ? AxisUnitConverter.ToDisplayVelocity(config.DefaultVelocity, axis) : 0.0,
                    config != null ? AxisUnitConverter.ToDisplayVelocity(config.Acceleration, axis) : 0.0,
                    config != null ? AxisUnitConverter.ToDisplayVelocity(config.Deceleration, axis) : 0.0,
                    unit);

                lblStatus.Text = "Default profile loaded.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "MOTION-TEST", "Load default profile failed: " + ex.Message);
                MessageDialog.Show(this, "Default profile load failed: " + ex.Message, "Motion Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private void SetProfileValues(double velocity, double acceleration, double deceleration, string unit)
        {
            try
            {
                _bindingProfile = true;
                EnsureProfileRows();
                SetProfileRow(0, velocity, unit + "/s");
                SetProfileRow(1, acceleration, unit + "/s2");
                SetProfileRow(2, deceleration, unit + "/s2");
            }
            finally
            {
                _bindingProfile = false;
            }
        }

        private void EnsureProfileRows()
        {
            while (gridProfile.Rows.Count < 3)
                gridProfile.Rows.Add("", "0", "");
            gridProfile.Rows[0].Cells[0].Value = "Velocity";
            gridProfile.Rows[1].Cells[0].Value = "Acceleration";
            gridProfile.Rows[2].Cells[0].Value = "Deceleration";
        }

        private void SetProfileRow(int row, double value, string unit)
        {
            gridProfile.Rows[row].Cells[1].Value = value.ToString("0.###", CultureInfo.InvariantCulture);
            gridProfile.Rows[row].Cells[2].Value = unit ?? "";
        }

        private void ValidateProfileGrid()
        {
            if (_bindingProfile)
                return;

            try
            {
                MotionTestProfile profile;
                string reason;
                if (!TryReadProfile(out profile, out reason))
                    lblStatus.Text = reason;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void CaptureCurrentPosition(NumericUpDown targetControl)
        {
            try
            {
                BaseAxis axis = SelectedAxis;
                if (axis == null || targetControl == null)
                    return;

                axis.UpdateStatus();
                decimal value = ToDecimal(AxisUnitConverter.ToDisplay(axis.ActualPosition, axis));
                targetControl.Value = ClampDecimal(value, targetControl);
                lblStatus.Text = "Position captured.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "MOTION-TEST", "Capture position failed: " + ex.Message);
                MessageDialog.Show(this, ex.Message, "Motion Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                RefreshAxisState();
            }
        }

        private void SwapStartEnd()
        {
            try
            {
                decimal start = nudStartPosition.Value;
                nudStartPosition.Value = nudEndPosition.Value;
                nudEndPosition.Value = start;
                lblStatus.Text = "Start/End swapped.";
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ServoOnSelectedAxis()
        {
            try
            {
                BaseAxis axis = SelectedAxis;
                if (axis == null)
                    return;

                axis.ServoOn();
                EventLogger.Write(EventKind.Event, "UI", "MOTION-TEST", axis.Name + " servo on requested.");
                lblStatus.Text = "Servo ON requested.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "MOTION-TEST", "Servo on failed: " + ex.Message);
                MessageDialog.Show(this, ex.Message, "Motion Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshAxisState();
            }
        }

        private async Task MoveSingleAsync(decimal displayTarget, string source)
        {
            if (IsRunning)
            {
                MessageDialog.Show(this, "Repeat test is running. Stop first.", "Motion Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BaseAxis axis = SelectedAxis;
            if (axis == null)
                return;

            MotionTestProfile profile;
            string reason;
            if (!TryReadProfile(out profile, out reason))
            {
                MessageDialog.Show(this, reason, "Motion Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                SetBusy(true);
                try
                {
                    double nativeTarget = AxisUnitConverter.FromDisplay((double)displayTarget, axis);
                    int result = await MoveAxisAndVerifyAsync(axis, nativeTarget, profile, source, cts.Token).ConfigureAwait(true);
                    lblStatus.Text = result == 0 ? "Move complete." : "Move failed. result=" + result;
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Alarm, "UI", "MOTION-TEST", source + " failed: " + ex.Message);
                    MessageDialog.Show(this, ex.Message, "Motion Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    SetBusy(false);
                    RefreshAxisState();
                }
            }
        }

        private void StartRepeat()
        {
            try
            {
                if (IsRunning)
                    return;

                BaseAxis axis = SelectedAxis;
                if (axis == null)
                    return;

                MotionTestProfile profile;
                string reason;
                if (!TryReadProfile(out profile, out reason))
                {
                    MessageDialog.Show(this, reason, "Motion Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                double start = AxisUnitConverter.FromDisplay((double)nudStartPosition.Value, axis);
                double end = AxisUnitConverter.FromDisplay((double)nudEndPosition.Value, axis);
                if (Math.Abs(start - end) <= ResolveTolerance(axis))
                {
                    MessageDialog.Show(this, "Start and End positions are the same.", "Motion Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (chkSoftLimitCheck.Checked && !CheckSoftLimit(axis, start, end, out reason))
                {
                    MessageDialog.Show(this, reason, "Motion Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _completedLegs = 0;
                _repeatCts = new CancellationTokenSource();
                _repeatTask = RunRepeatAsync(axis, start, end, profile, _repeatCts.Token);
                UpdateRunButtons();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "MOTION-TEST", "Start repeat failed: " + ex.Message);
                MessageDialog.Show(this, ex.Message, "Motion Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateRunButtons();
            }
            finally
            {
            }
        }

        private async Task RunRepeatAsync(BaseAxis axis, double start, double end, MotionTestProfile profile, CancellationToken ct)
        {
            int repeatCount = (int)nudRepeatCount.Value;
            int dwellMs = (int)nudDwellMs.Value;

            SetBusy(true);
            try
            {
                EnsureServoOn(axis);
                EventLogger.Write(EventKind.Event, "UI", "MOTION-TEST",
                    "Repeat start. axis=" + axis.Name +
                    ", start=" + start.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", end=" + end.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", repeat=" + repeatCount +
                    ", velocity=" + profile.Velocity.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", acc=" + profile.Acceleration.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", dec=" + profile.Deceleration.ToString("0.###", CultureInfo.InvariantCulture));

                int cycle = 0;
                while (!ct.IsCancellationRequested && (repeatCount == 0 || cycle < repeatCount))
                {
                    int result = await MoveAxisAndVerifyAsync(axis, start, profile, "MotionTestRepeatStart", ct).ConfigureAwait(true);
                    if (result != 0)
                    {
                        lblStatus.Text = "Start move failed. result=" + result;
                        break;
                    }

                    _completedLegs++;
                    UpdateCounterLabel(cycle, repeatCount);
                    if (dwellMs > 0)
                        await Task.Delay(dwellMs, ct).ConfigureAwait(true);

                    result = await MoveAxisAndVerifyAsync(axis, end, profile, "MotionTestRepeatEnd", ct).ConfigureAwait(true);
                    if (result != 0)
                    {
                        lblStatus.Text = "End move failed. result=" + result;
                        break;
                    }

                    _completedLegs++;
                    cycle++;
                    UpdateCounterLabel(cycle, repeatCount);
                    if (dwellMs > 0)
                        await Task.Delay(dwellMs, ct).ConfigureAwait(true);

                    if (chkStopOnAlarm.Checked && axis.IsAlarm)
                    {
                        lblStatus.Text = "Stopped by axis alarm.";
                        break;
                    }
                }

                if (ct.IsCancellationRequested)
                    lblStatus.Text = "Stopped.";
                else if (repeatCount > 0 && cycle >= repeatCount)
                    lblStatus.Text = "Repeat complete.";
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Stopped.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "MOTION-TEST", "Repeat failed: " + ex.Message);
                lblStatus.Text = "Repeat failed: " + ex.Message;
            }
            finally
            {
                SetBusy(false);
                if (_repeatCts != null)
                {
                    _repeatCts.Dispose();
                    _repeatCts = null;
                }
                _repeatTask = null;
                UpdateRunButtons();
                RefreshAxisState();
            }
        }

        private async Task<int> MoveAxisAndVerifyAsync(
            BaseAxis axis,
            double nativeTarget,
            MotionTestProfile profile,
            string source,
            CancellationToken ct)
        {
            MotionProfileSnapshot snapshot = null;
            try
            {
                ct.ThrowIfCancellationRequested();
                if (axis == null)
                    return -1;

                EnsureServoOn(axis);
                axis.UpdateStatus();
                if (axis.IsAlarm)
                    return -2;

                snapshot = MotionProfileSnapshot.Capture(axis);
                ApplyProfile(axis, profile);

                lblStatus.Text = source + " moving...";
                int result = await axis.MoveAbsoluteAsync(nativeTarget, profile.Velocity).ConfigureAwait(true);
                if (result != 0)
                {
                    EventLogger.Write(EventKind.Alarm, "UI", "MOTION-TEST",
                        source + " command failed. axis=" + axis.Name + ", result=" + result +
                        ", message=" + axis.LastMotionFailureMessage);
                    return result;
                }

                AxisMoveWaitResult wait = await AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                    axis,
                    nativeTarget,
                    ResolveTolerance(axis),
                    ResolveMoveTimeout(axis),
                    DefaultSettleMs,
                    ct).ConfigureAwait(true);
                if (wait == null || !wait.Success)
                {
                    EventLogger.Write(EventKind.Alarm, "UI", "MOTION-TEST",
                        source + " final wait failed. " +
                        AxisMoveWaiter.FormatResult(wait, AxisMoveWaiter.BuildAxisState(axis, nativeTarget, ResolveTolerance(axis))));
                    return wait != null ? wait.Code : -1;
                }

                EventLogger.Write(EventKind.Event, "UI", "MOTION-TEST",
                    source + " ok. axis=" + axis.Name +
                    ", target=" + nativeTarget.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", velocity=" + profile.Velocity.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", acc=" + profile.Acceleration.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", dec=" + profile.Deceleration.ToString("0.###", CultureInfo.InvariantCulture));
                return 0;
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (axis != null)
                        axis.Stop();
                }
                catch
                {
                }

                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "MOTION-TEST", source + " exception: " + ex.Message);
                return -1;
            }
            finally
            {
                if (snapshot != null)
                    snapshot.Restore();
                RefreshAxisState();
            }
        }

        private static void EnsureServoOn(BaseAxis axis)
        {
            if (axis != null && !axis.IsServoOn)
                axis.ServoOn();
        }

        private static void ApplyProfile(BaseAxis axis, MotionTestProfile profile)
        {
            if (axis == null || axis.Config == null || profile == null)
                return;

            axis.Config.DefaultVelocity = profile.Velocity;
            axis.Config.Acceleration = profile.Acceleration;
            axis.Config.Deceleration = profile.Deceleration;
        }

        private bool TryReadProfile(out MotionTestProfile profile, out string reason)
        {
            profile = null;
            reason = string.Empty;

            BaseAxis axis = SelectedAxis;
            if (axis == null)
            {
                reason = "Axis is not selected.";
                return false;
            }

            double velocity;
            double acceleration;
            double deceleration;
            if (!TryReadProfileRow(0, out velocity) ||
                !TryReadProfileRow(1, out acceleration) ||
                !TryReadProfileRow(2, out deceleration))
            {
                reason = "Profile value is invalid.";
                return false;
            }

            if (velocity <= 0.0 || acceleration <= 0.0 || deceleration <= 0.0)
            {
                reason = "Velocity/Acceleration/Deceleration must be greater than zero.";
                return false;
            }

            profile = new MotionTestProfile
            {
                Velocity = AxisUnitConverter.FromDisplayVelocity(velocity, axis),
                Acceleration = AxisUnitConverter.FromDisplayVelocity(acceleration, axis),
                Deceleration = AxisUnitConverter.FromDisplayVelocity(deceleration, axis)
            };
            return true;
        }

        private bool TryReadProfileRow(int rowIndex, out double value)
        {
            value = 0.0;
            try
            {
                EnsureProfileRows();
                object raw = gridProfile.Rows[rowIndex].Cells[1].Value;
                return double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool CheckSoftLimit(BaseAxis axis, double start, double end, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (axis == null || axis.Setup == null || !axis.Setup.SoftLimitEnabled)
                    return true;

                double min = Math.Min(start, end);
                double max = Math.Max(start, end);
                if (min < axis.Setup.SoftLimitMinus || max > axis.Setup.SoftLimitPlus)
                {
                    reason = "Target is outside soft limit. minus=" +
                             axis.Setup.SoftLimitMinus.ToString("0.###", CultureInfo.InvariantCulture) +
                             ", plus=" + axis.Setup.SoftLimitPlus.ToString("0.###", CultureInfo.InvariantCulture);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "Soft limit check failed: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private void RequestStop(bool showStatus)
        {
            try
            {
                if (_repeatCts != null && !_repeatCts.IsCancellationRequested)
                    _repeatCts.Cancel();

                BaseAxis axis = SelectedAxis;
                if (axis != null)
                    axis.Stop();

                if (showStatus)
                    lblStatus.Text = "Stop requested.";

                EventLogger.Write(EventKind.Event, "UI", "MOTION-TEST", "Motion test stop requested.");
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "MOTION-TEST", "Stop failed: " + ex.Message);
            }
            finally
            {
                UpdateRunButtons();
                RefreshAxisState();
            }
        }

        private void RefreshAxisState()
        {
            try
            {
                if (IsDisposed)
                    return;

                BaseAxis axis = SelectedAxis;
                if (axis == null)
                {
                    lblActual.Text = "-";
                    lblCommand.Text = "-";
                    lblAxisState.Text = "-";
                    return;
                }

                axis.UpdateStatus();
                string unit = AxisUnitConverter.DisplayUnitFor(axis);
                lblActual.Text = AxisUnitConverter.ToDisplay(axis.ActualPosition, axis).ToString("0.###", CultureInfo.InvariantCulture);
                lblCommand.Text = AxisUnitConverter.ToDisplay(axis.CommandPosition, axis).ToString("0.###", CultureInfo.InvariantCulture);
                lblAxisState.Text =
                    "Servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                    "  Moving=" + (axis.IsMoving ? "ON" : "OFF") +
                    "  InPos=" + (axis.IsInPosition ? "ON" : "OFF") +
                    "  Alarm=" + (axis.IsAlarm ? "ON" : "OFF");
                lblUnit.Text = unit;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "MOTION-TEST", "State refresh failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void SetBusy(bool busy)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(SetBusy), busy);
                return;
            }

            lstAxes.Enabled = !busy;
            btnStartRepeat.Enabled = !busy;
            btnMoveStart.Enabled = !busy;
            btnMoveEnd.Enabled = !busy;
            btnReloadDefault.Enabled = !busy;
            btnCaptureStart.Enabled = !busy;
            btnCaptureEnd.Enabled = !busy;
            btnSwap.Enabled = !busy;
            gridProfile.ReadOnly = busy;
            btnStop.Enabled = busy;
        }

        private void UpdateRunButtons()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateRunButtons));
                return;
            }

            bool running = IsRunning;
            btnStartRepeat.Enabled = !running;
            btnStop.Enabled = running;
            btnClose.Enabled = true;
        }

        private void UpdateCounterLabel(int completedCycle, int repeatCount)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, int>(UpdateCounterLabel), completedCycle, repeatCount);
                return;
            }

            lblCounter.Text = "Cycle " + completedCycle.ToString(CultureInfo.InvariantCulture) +
                              (repeatCount > 0 ? " / " + repeatCount.ToString(CultureInfo.InvariantCulture) : " / INF") +
                              "  Legs " + _completedLegs.ToString(CultureInfo.InvariantCulture);
        }

        private int ResolveMoveTimeout(BaseAxis axis)
        {
            try
            {
                int uiTimeout = (int)nudTimeoutMs.Value;
                if (uiTimeout > 0)
                    return uiTimeout;

                return axis != null && axis.Setup != null && axis.Setup.MoveTimeoutMs > 0
                    ? axis.Setup.MoveTimeoutMs
                    : 60000;
            }
            catch
            {
                return 60000;
            }
            finally
            {
            }
        }

        private static double ResolveTolerance(BaseAxis axis)
        {
            try
            {
                return axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                    ? axis.Config.InPositionTolerance
                    : 0.01;
            }
            catch
            {
                return 0.01;
            }
            finally
            {
            }
        }

        private static decimal ToDecimal(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0m;

            if (value > (double)decimal.MaxValue)
                return decimal.MaxValue;
            if (value < (double)decimal.MinValue)
                return decimal.MinValue;

            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        private static decimal ClampDecimal(decimal value, NumericUpDown control)
        {
            if (control == null)
                return value;
            if (value < control.Minimum)
                return control.Minimum;
            if (value > control.Maximum)
                return control.Maximum;
            return value;
        }

        private static string DisplayNameWithNo(BaseAxis axis)
        {
            try
            {
                int axisNo = axis != null && axis.Setup != null ? axis.Setup.AxisNo : -1;
                string display = axis != null && axis.Setup != null && !string.IsNullOrWhiteSpace(axis.Setup.DisplayName)
                    ? axis.Setup.DisplayName
                    : axis != null ? axis.Name : "AXIS";

                return (axisNo >= 0 ? axisNo.ToString("00", CultureInfo.InvariantCulture) + " - " : string.Empty) + display;
            }
            catch
            {
                return "AXIS";
            }
            finally
            {
            }
        }

        private void MotionTestDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                RequestStop(false);
                _positionTimer.Stop();
            }
            catch
            {
            }
            finally
            {
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    RequestStop(false);
                    _positionTimer.Dispose();
                    if (components != null)
                        components.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private sealed class AxisListItem
        {
            public AxisListItem(BaseAxis axis)
            {
                Axis = axis;
            }

            public BaseAxis Axis { get; private set; }

            public override string ToString()
            {
                return DisplayNameWithNo(Axis);
            }
        }

        private sealed class MotionTestProfile
        {
            public double Velocity { get; set; }
            public double Acceleration { get; set; }
            public double Deceleration { get; set; }
        }

        private sealed class MotionProfileSnapshot
        {
            private readonly BaseAxis _axis;
            private readonly double _velocity;
            private readonly double _acceleration;
            private readonly double _deceleration;

            private MotionProfileSnapshot(BaseAxis axis)
            {
                _axis = axis;
                _velocity = axis.Config.DefaultVelocity;
                _acceleration = axis.Config.Acceleration;
                _deceleration = axis.Config.Deceleration;
            }

            public static MotionProfileSnapshot Capture(BaseAxis axis)
            {
                if (axis == null || axis.Config == null)
                    return null;
                return new MotionProfileSnapshot(axis);
            }

            public void Restore()
            {
                if (_axis == null || _axis.Config == null)
                    return;

                _axis.Config.DefaultVelocity = _velocity;
                _axis.Config.Acceleration = _acceleration;
                _axis.Config.Deceleration = _deceleration;
            }
        }
    }
}
