using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class AxisJogPopup : Form
    {
        private readonly List<BaseAxis> _axes;
        private readonly Timer _posTimer = new Timer();
        private bool _isStepJogging;
        private string _lastStepUnit = string.Empty;

        public AxisJogPopup(IEnumerable<BaseAxis> axes)
        {
            _axes = SortAxes(axes).ToList();

            InitializeComponent();
            BindAxes();
            WireEvents();

            rdoFine.Checked = true;
            rdoStep.Checked = true;
            nudStep.DecimalPlaces = 3;
            ApplyMoveModeUi();
            UpdateStepPresetUi();
            UpdateJogEnableByAxis();
            UpdatePositionOnce();

            _posTimer.Interval = 200;
            _posTimer.Tick += (s, e) => UpdatePositionOnce();
            Load += (s, e) => _posTimer.Start();
            FormClosed += (s, e) => _posTimer.Stop();
        }

        private BaseAxis SelectedAxis
        {
            get
            {
                var item = selectAxisList.SelectedItem as AxisListItem;
                return item != null ? item.Axis : null;
            }
        }

        private static IEnumerable<BaseAxis> SortAxes(IEnumerable<BaseAxis> axes)
        {
            return (axes ?? Enumerable.Empty<BaseAxis>())
                .Where(a => a != null)
                .OrderBy(a => a.Setup != null && a.Setup.AxisNo >= 0 ? a.Setup.AxisNo : int.MaxValue)
                .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase);
        }

        private void BindAxes()
        {
            selectAxisList.Items.Clear();
            foreach (var axis in _axes)
                selectAxisList.Items.Add(new AxisListItem(axis));
            if (selectAxisList.Items.Count > 0)
                selectAxisList.SelectedIndex = 0;
        }

        private void WireEvents()
        {
            selectAxisList.SelectedIndexChanged += (s, e) =>
            {
                UpdateStepPresetUi();
                UpdateJogEnableByAxis();
                UpdatePositionOnce();
            };

            rdoStep.CheckedChanged += (s, e) => ApplyMoveModeUi();
            rdoContinuous.CheckedChanged += (s, e) => ApplyMoveModeUi();

            foreach (var btn in new[] { btnXMinus, btnXPlus, btnYMinus, btnYPlus, btnZMinus, btnZPlus, btnTMinus, btnTPlus })
            {
                btn.MouseDown += JogButton_MouseDown;
                btn.MouseUp += JogButton_MouseUp;
                btn.MouseLeave += JogButton_MouseLeave;
            }

            btnStop.Click += (s, e) => StopJog();
            btnPrevIndex.Click += async (s, e) => await StepJogAsync(-1);
            btnNextIndex.Click += async (s, e) => await StepJogAsync(1);
            btnStep1.Click += (s, e) => SetStep(GetStepPreset(0));
            btnStep01.Click += (s, e) => SetStep(GetStepPreset(1));
            btnStep001.Click += (s, e) => SetStep(GetStepPreset(2));
            btnStep0001.Click += (s, e) => SetStep(GetStepPreset(3));
            btnStepZero.Click += (s, e) => SetStep(0m);
        }

        private void ApplyMoveModeUi()
        {
            bool step = rdoStep.Checked;
            nudStep.Enabled = step;
            btnStep1.Enabled = step;
            btnStep01.Enabled = step;
            btnStep001.Enabled = step;
            btnStep0001.Enabled = step;
            btnStepZero.Enabled = step;
        }

        private void UpdateJogEnableByAxis()
        {
            var axis = SelectedAxis;
            string text = AxisText(axis);
            bool isIndex = text.IndexOf("INDEX", StringComparison.OrdinalIgnoreCase) >= 0;

            if (axis == null)
            {
                SetAllJogButtons(false);
                return;
            }

            if (isIndex)
            {
                btnXMinus.Enabled = btnXPlus.Enabled = false;
                btnYMinus.Enabled = btnYPlus.Enabled = false;
                btnZMinus.Enabled = btnZPlus.Enabled = false;
                btnTMinus.Enabled = btnTPlus.Enabled = false;
                btnPrevIndex.Enabled = btnNextIndex.Enabled = true;
                btnStop.Enabled = true;
                return;
            }

            bool x = HasAxisLetter(text, "X");
            bool y = HasAxisLetter(text, "Y");
            bool z = HasAxisLetter(text, "Z");
            bool t = HasAxisLetter(text, "T");

            btnXMinus.Enabled = btnXPlus.Enabled = x;
            btnYMinus.Enabled = btnYPlus.Enabled = y;
            btnZMinus.Enabled = btnZPlus.Enabled = z;
            btnTMinus.Enabled = btnTPlus.Enabled = t;
            btnPrevIndex.Enabled = btnNextIndex.Enabled = false;
            btnStop.Enabled = true;
        }

        private void SetAllJogButtons(bool enabled)
        {
            foreach (var btn in new[] { btnXMinus, btnXPlus, btnYMinus, btnYPlus, btnZMinus, btnZPlus, btnTMinus, btnTPlus, btnStop, btnPrevIndex, btnNextIndex })
                btn.Enabled = enabled;
        }

        private async void JogButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) 
                return;

            var btn = sender as Button;
            if (btn == null || !btn.Enabled) 
                return;

            int direction = btn.Text.IndexOf("-", StringComparison.Ordinal) >= 0 ? -1 : 1;
            
            if (rdoContinuous.Checked)
                StartContinuousJog(direction);
            else
                await StepJogAsync(direction);
        }

        private void JogButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (rdoContinuous.Checked)
                StopJog();
        }

        private void JogButton_MouseLeave(object sender, EventArgs e)
        {
            if (rdoContinuous.Checked)
                StopJog();
        }

        private void StartContinuousJog(int direction)
        {
            try
            {
                var axis = SelectedAxis;
                if (axis == null)
                    return;

                if (!axis.IsServoOn)
                    axis.ServoOn();

                axis.MoveJogContinuous(direction, SpeedType());
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "JOG", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                UpdatePositionOnce();
            }
        }

        private async Task StepJogAsync(int direction)
        {
            try
            {
                if (_isStepJogging)
                    return;

                var axis = SelectedAxis;
                if (axis == null)
                    return;

                if (!axis.IsServoOn)
                    axis.ServoOn();

                axis.UpdateStatus();
                if (axis.IsMoving)
                    return;

                double stepDisplay = (double)nudStep.Value;
                if (stepDisplay <= 0)
                    return;

                double stepNative = AxisUnitConverter.FromDisplay(stepDisplay, axis);

                _isStepJogging = true;
                SetJogButtonsEnabled(false);
                await axis.MoveJogStepAsync(direction, SpeedType(), stepNative);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "JOG STEP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _isStepJogging = false;
                UpdateJogEnableByAxis();
                UpdatePositionOnce();
            }
        }

        private void StopJog()
        {
            try
            {
                var axis = SelectedAxis;
                if (axis != null)
                    axis.StopJog();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "JOG STOP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                UpdatePositionOnce();
            }
        }

        private void SetJogButtonsEnabled(bool enabled)
        {
            try
            {
                foreach (var btn in new[] { btnXMinus, btnXPlus, btnYMinus, btnYPlus, btnZMinus, btnZPlus, btnTMinus, btnTPlus, btnPrevIndex, btnNextIndex })
                    btn.Enabled = enabled && btn.Enabled;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private JogSpeedType SpeedType()
        {
            return rdoCoarse.Checked ? JogSpeedType.Coarse : JogSpeedType.Fine;
        }

        private void UpdatePositionOnce()
        {
            try
            {
                UpdateStepPresetUi();

                var axis = SelectedAxis;
                if (axis != null)
                    axis.UpdateStatus();

                if (axis == null)
                {
                    lblPosition.Text = "000";
                    return;
                }

                string unit = AxisUnitConverter.DisplayUnitFor(axis);
                double displayPos = AxisUnitConverter.ToDisplay(axis.ActualPosition, axis);
                string format = AxisUnitConverter.Normalize(unit) == AxisUnitConverter.Micrometer ? "0" : "0.###";
                lblPosition.Text = displayPos.ToString(format, CultureInfo.InvariantCulture) + " " + unit;
            }
            catch
            {
                lblPosition.Text = "ERR";
            }
            finally
            {
            }
        }

        private void SetStep(decimal value)
        {
            nudStep.Value = value;
        }

        private decimal GetStepPreset(int index)
        {
            try
            {
                var axis = SelectedAxis;
                string unit = axis == null ? AxisUnitConverter.Millimeter : AxisUnitConverter.DisplayUnitFor(axis);
                decimal[] values = AxisUnitConverter.Normalize(unit) == AxisUnitConverter.Micrometer
                    ? new[] { 1000m, 100m, 10m, 1m }
                    : new[] { 1m, 0.1m, 0.01m, 0.001m };
                return values[Math.Max(0, Math.Min(index, values.Length - 1))];
            }
            catch
            {
                return 1m;
            }
            finally
            {
            }
        }

        private void UpdateStepPresetUi()
        {
            try
            {
                var axis = SelectedAxis;
                string unit = axis == null ? AxisUnitConverter.Millimeter : AxisUnitConverter.DisplayUnitFor(axis);
                if (!string.Equals(_lastStepUnit, unit, StringComparison.OrdinalIgnoreCase))
                {
                    _lastStepUnit = unit;
                    SetStep(GetStepPreset(0));
                }

                btnStep1.Text = FormatStepPreset(GetStepPreset(0), unit);
                btnStep01.Text = FormatStepPreset(GetStepPreset(1), unit);
                btnStep001.Text = FormatStepPreset(GetStepPreset(2), unit);
                btnStep0001.Text = FormatStepPreset(GetStepPreset(3), unit);
                btnStepZero.Text = FormatStepPreset(0m, unit);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static string FormatStepPreset(decimal value, string unit)
        {
            try
            {
                return value.ToString("0.###", CultureInfo.InvariantCulture) + unit;
            }
            catch
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
            finally
            {
            }
        }

        private static bool HasAxisLetter(string name, string letter)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string s = name.ToUpperInvariant();
            string l = letter.ToUpperInvariant();

            if (Regex.IsMatch(s, Regex.Escape(l) + @"[0-9]*$"))
                return true;

            return Regex.IsMatch(s, @"(^|[^A-Z0-9])" + Regex.Escape(l) + @"[0-9]*([^A-Z0-9]|$)");
        }

        private static string AxisText(BaseAxis axis)
        {
            if (axis == null) return string.Empty;
            string display = axis.Setup != null ? axis.Setup.DisplayName : string.Empty;
            return (axis.Name + " " + display).Trim();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _posTimer.Dispose();
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
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
                int axisNo = Axis.Setup != null ? Axis.Setup.AxisNo : -1;
                string display = Axis.Setup != null && !string.IsNullOrWhiteSpace(Axis.Setup.DisplayName)
                    ? Axis.Setup.DisplayName
                    : Axis.Name;
                return axisNo.ToString("00", CultureInfo.InvariantCulture) + " - " + display;
            }
        }
    }
}

