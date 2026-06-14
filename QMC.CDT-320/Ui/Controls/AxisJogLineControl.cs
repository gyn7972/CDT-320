using QMC.Common.Motion;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>단일 축 조그 이동을 위한 라인형 컨트롤입니다.</summary>
    public partial class AxisJogLineControl : UserControl
    {
        private BaseAxis _axis;
        private string _axisCaption = "AXIS\r\nNAME";

        /// <summary>화면에 표시할 축 이름입니다.</summary>
        public string AxisCaption
        {
            get { return _axisCaption; }
            set
            {
                _axisCaption = string.IsNullOrWhiteSpace(value) ? "AXIS\r\nNAME" : value;
                _lblAxisName.Text = _axisCaption;
            }
        }

        /// <summary>조그 모드와 이동 단위를 제공하는 옵션 컨트롤입니다.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public JogMoveOptionsControl MoveOptions { get; set; }

        /// <summary>현재 조그 속도를 제공하는 콜백입니다.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<double> SpeedProvider { get; set; }

        public AxisJogLineControl()
        {
            InitializeComponent();
            BindEvents();
            UpdateEnabledState();
        }

        /// <summary>조작할 축 객체와 표시 이름을 연결합니다.</summary>
        public void BindAxis(string axisName, BaseAxis axis)
        {
            _axis = axis;
            AxisCaption = axisName;
            UpdateEnabledState();
        }

        /// <summary>연결된 축의 조그 이동을 정지합니다.</summary>
        public void StopJog()
        {
            try { _axis?.StopJog(); } catch { }
        }

        private void BindEvents()
        {
            BindJogButton(_btnPlus, +1);
            BindJogButton(_btnMinus, -1);
            _btnStop.Click += (s, e) => StopJog();
        }

        private void BindJogButton(Button button, int direction)
        {
            button.MouseDown += async (s, e) =>
            {
                if (e.Button != MouseButtons.Left)
                    return;

                if (IsStepMode)
                    await StepJogAsync(direction);
                else
                    StartContinuousJog(direction);
            };

            button.MouseUp += (s, e) =>
            {
                if (!IsStepMode)
                    StopJog();
            };

            button.MouseLeave += (s, e) =>
            {
                if (!IsStepMode)
                    StopJog();
            };
        }

        private void StartContinuousJog(int direction)
        {
            if (!PrepareAxis())
                return;

            try
            {
                _axis.MoveJogContinuous(direction, SpeedType, GetAxisSpeed());
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task StepJogAsync(int direction)
        {
            if (!PrepareAxis())
                return;

            try
            {
                await _axis.MoveJogStepAsync(direction, SpeedType, AxisUnitConverter.FromDisplay(StepDistance, _axis), GetAxisSpeed());
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool PrepareAxis()
        {
            if (_axis == null || _axis.IsAlarm)
                return false;

            if (!_axis.IsServoOn)
                _axis.ServoOn();

            return true;
        }

        private JogSpeedType SpeedType => SpeedProvider != null ? JogSpeedType.Custom : (MoveOptions != null ? MoveOptions.SpeedType : JogSpeedType.Fine);

        private bool IsStepMode => MoveOptions == null || MoveOptions.IsStepMode;

        private double StepDistance => MoveOptions != null ? MoveOptions.StepDistance : 1.0;

        private double GetSpeed()
        {
            if (SpeedProvider == null)
                return 0.0;

            try { return Math.Max(0.0, SpeedProvider()); }
            catch { return 0.0; }
        }

        private double GetAxisSpeed()
        {
            return AxisUnitConverter.FromDisplayVelocity(GetSpeed(), _axis);
        }

        private void UpdateEnabledState()
        {
            bool enabled = _axis != null;
            _btnPlus.Enabled = enabled;
            _btnMinus.Enabled = enabled;
            _btnStop.Enabled = enabled;
        }
    }
}

