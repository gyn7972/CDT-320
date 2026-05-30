using QMC.Common.Motion;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>단일 축을 +/STOP/- 버튼으로 조그 운전하는 공용 컨트롤입니다.</summary>
    public partial class AxisJogLineControl : UserControl
    {
        private BaseAxis _axis;
        private string _axisCaption = "AXIS\r\nNAME";

        /// <summary>디자이너와 화면에 표시할 축 이름입니다.</summary>
        public string AxisCaption
        {
            get { return _axisCaption; }
            set
            {
                _axisCaption = string.IsNullOrWhiteSpace(value) ? "AXIS\r\nNAME" : value;
                _lblAxisName.Text = _axisCaption;
            }
        }

        /// <summary>조그 이동 옵션 컨트롤입니다.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public JogMoveOptionsControl MoveOptions { get; set; }

        /// <summary>사용자 지정 속도 공급자입니다.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<double> SpeedProvider { get; set; }

        /// <summary>AxisJogLineControl을 생성합니다.</summary>
        public AxisJogLineControl()
        {
            InitializeComponent();
            BindEvents();
            UpdateEnabledState();
        }

        /// <summary>조그 대상 축을 바인딩합니다.</summary>
        public void BindAxis(string axisName, BaseAxis axis)
        {
            _axis = axis;
            AxisCaption = axisName;
            UpdateEnabledState();
        }

        /// <summary>현재 축의 조그 이동을 정지합니다.</summary>
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
                _axis.MoveJogContinuous(direction, SpeedType, GetSpeed());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task StepJogAsync(int direction)
        {
            if (!PrepareAxis())
                return;

            try
            {
                await _axis.MoveJogStepAsync(direction, SpeedType, StepDistance, GetSpeed());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void UpdateEnabledState()
        {
            bool enabled = _axis != null;
            _btnPlus.Enabled = enabled;
            _btnMinus.Enabled = enabled;
            _btnStop.Enabled = enabled;
        }
    }
}
