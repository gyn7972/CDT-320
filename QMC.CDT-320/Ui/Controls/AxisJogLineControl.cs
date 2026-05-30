using QMC.Common.Motion;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>?⑥씪 異뺤쓣 +/STOP/- 踰꾪듉?쇰줈 議곌렇 ?댁쟾?섎뒗 怨듭슜 而⑦듃濡ㅼ엯?덈떎.</summary>
    public partial class AxisJogLineControl : UserControl
    {
        private BaseAxis _axis;
        private string _axisCaption = "AXIS\r\nNAME";

        /// <summary>?붿옄?대꼫? ?붾㈃???쒖떆??異??대쫫?낅땲??</summary>
        public string AxisCaption
        {
            get { return _axisCaption; }
            set
            {
                _axisCaption = string.IsNullOrWhiteSpace(value) ? "AXIS\r\nNAME" : value;
                _lblAxisName.Text = _axisCaption;
            }
        }

        /// <summary>議곌렇 ?대룞 ?듭뀡 而⑦듃濡ㅼ엯?덈떎.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public JogMoveOptionsControl MoveOptions { get; set; }

        /// <summary>?ъ슜??吏???띾룄 怨듦툒?먯엯?덈떎.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<double> SpeedProvider { get; set; }

        /// <summary>AxisJogLineControl???앹꽦?⑸땲??</summary>
        public AxisJogLineControl()
        {
            InitializeComponent();
            BindEvents();
            UpdateEnabledState();
        }

        /// <summary>議곌렇 ???異뺤쓣 諛붿씤?⑺빀?덈떎.</summary>
        public void BindAxis(string axisName, BaseAxis axis)
        {
            _axis = axis;
            AxisCaption = axisName;
            UpdateEnabledState();
        }

        /// <summary>?꾩옱 異뺤쓽 議곌렇 ?대룞???뺤??⑸땲??</summary>
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void UpdateEnabledState()
        {
            bool enabled = _axis != null;
            _btnPlus.Enabled = enabled;
            _btnMinus.Enabled = enabled;
            _btnStop.Enabled = enabled;
        }
    }
}

