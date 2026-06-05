using QMC.Common.Motion;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>구현 설명 주석입니다.</summary>
    public partial class AxisJogPadControl : UserControl
    {
        private BaseAxis _axisX;
        private BaseAxis _axisY;
        private BaseAxis _axisT;

        /// <summary>구현 설명 주석입니다.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public JogMoveOptionsControl MoveOptions { get; set; }

        /// <summary>구현 설명 주석입니다.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<double> SpeedProvider { get; set; }

        /// <summary>구현 설명 주석입니다.</summary>
        public AxisJogPadControl()
        {
            InitializeComponent();
            BindEvents();
            UpdateEnabledState();
        }

        /// <summary>구현 설명 주석입니다.</summary>
        public void BindAxes(BaseAxis x, BaseAxis y, BaseAxis t)
        {
            _axisX = x;
            _axisY = y;
            _axisT = t;
            UpdateEnabledState();
        }

        /// <summary>구현 설명 주석입니다.</summary>
        public void StopJog()
        {
            StopAxis(_axisX);
            StopAxis(_axisY);
            StopAxis(_axisT);
        }

        private void BindEvents()
        {
            BindJogButton(_btnXMinus, () => _axisX, -1);
            BindJogButton(_btnXPlus, () => _axisX, +1);
            BindJogButton(_btnYMinus, () => _axisY, -1);
            BindJogButton(_btnYPlus, () => _axisY, +1);
            BindJogButton(_btnTMinus, () => _axisT, -1);
            BindJogButton(_btnTPlus, () => _axisT, +1);
            _btnStop.Click += (s, e) => StopJog();
        }

        private void BindJogButton(Button button, Func<BaseAxis> axisProvider, int direction)
        {
            button.MouseDown += async (s, e) =>
            {
                if (e.Button != MouseButtons.Left)
                    return;

                var axis = axisProvider();
                MarkSelectedAxis(axis);

                if (IsStepMode)
                    await StepJogAsync(axis, direction);
                else
                    StartContinuousJog(axis, direction);
            };

            button.MouseUp += (s, e) =>
            {
                if (!IsStepMode)
                    StopAxis(axisProvider());
            };

            button.MouseLeave += (s, e) =>
            {
                if (!IsStepMode)
                    StopAxis(axisProvider());
            };
        }

        private void StartContinuousJog(BaseAxis axis, int direction)
        {
            if (!PrepareAxis(axis))
                return;

            try
            {
                axis.MoveJogContinuous(direction, SpeedType, GetSpeed());
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task StepJogAsync(BaseAxis axis, int direction)
        {
            if (!PrepareAxis(axis))
                return;

            try
            {
                await axis.MoveJogStepAsync(direction, SpeedType, AxisUnitConverter.FromDisplay(StepDistance, axis), GetSpeed());
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool PrepareAxis(BaseAxis axis)
        {
            if (axis == null || axis.IsAlarm)
                return false;

            if (!axis.IsServoOn)
                axis.ServoOn();

            return true;
        }

        private void StopAxis(BaseAxis axis)
        {
            try { axis?.StopJog(); } catch { }
        }

        private JogSpeedType SpeedType => MoveOptions != null ? MoveOptions.SpeedType : JogSpeedType.Fine;

        private bool IsStepMode => MoveOptions == null || MoveOptions.IsStepMode;

        private double StepDistance => MoveOptions != null ? MoveOptions.StepDistance : 1.0;

        private double GetSpeed()
        {
            if (SpeedProvider == null)
                return 0.0;

            try { return Math.Max(0.0, SpeedProvider()); }
            catch { return 0.0; }
        }

        private void UpdateEnabledState()
        {
            _btnXMinus.Enabled = _axisX != null;
            _btnXPlus.Enabled = _axisX != null;
            _btnYMinus.Enabled = _axisY != null;
            _btnYPlus.Enabled = _axisY != null;
            _btnTMinus.Enabled = _axisT != null;
            _btnTPlus.Enabled = _axisT != null;
            _btnStop.Enabled = _axisX != null || _axisY != null || _axisT != null;
        }

        private void MarkSelectedAxis(BaseAxis axis)
        {
            Mark(_btnXMinus, axis == _axisX);
            Mark(_btnXPlus, axis == _axisX);
            Mark(_btnYMinus, axis == _axisY);
            Mark(_btnYPlus, axis == _axisY);
            Mark(_btnTMinus, axis == _axisT);
            Mark(_btnTPlus, axis == _axisT);
        }

        private void Mark(Button button, bool selected)
        {
            button.BackColor = selected && button.Enabled
                ? Color.FromArgb(255, 210, 130)
                : SystemColors.Control;
        }
    }
}

