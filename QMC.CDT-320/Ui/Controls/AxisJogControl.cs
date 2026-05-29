using QMC.Common.Motion;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>연결된 X/Y/Z/T 축을 선택하고 방향 패드로 조그 운전하는 공용 컨트롤입니다.</summary>
    public partial class AxisJogControl : UserControl
    {
        private BaseAxis _axisX;
        private BaseAxis _axisY;
        private BaseAxis _axisZ;
        private BaseAxis _axisT;
        private bool _updatingAxisSelector;

        /// <summary>현재 조그 속도를 반환하는 공급자입니다.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<double> SpeedProvider { get; set; }

        /// <summary>이전 인덱스 버튼을 눌렀을 때 발생합니다.</summary>

        /// <summary>다음 인덱스 버튼을 눌렀을 때 발생합니다.</summary>

        /// <summary>AxisJogControl을 생성합니다.</summary>
        public AxisJogControl()
        {
            InitializeComponent();
            BindButtonEvents();
            UpdateAxisBindings();
        }

        /// <summary>X/Y/Z/T 축을 바인딩하고 연결된 축만 조그 버튼을 활성화합니다.</summary>
        public void BindAxes(BaseAxis x, BaseAxis y, BaseAxis z, BaseAxis t)
        {
            _axisX = x;
            _axisY = y;
            _axisZ = z;
            _axisT = t;
            UpdateAxisBindings();
        }

        /// <summary>현재 선택된 축의 조그를 정지합니다.</summary>
        public void StopJog()
        {
            StopAxis(_axisX);
            StopAxis(_axisY);
            StopAxis(_axisZ);
            StopAxis(_axisT);
        }

        private void BindButtonEvents()
        {
            _axisSelector.SelectedIndexChanged += (s, e) => UpdateDirectionHighlight();

            _btnStep1.Click += (s, e) => SetStep(1000u);
            _btnStep01.Click += (s, e) => SetStep(100u);
            _btnStep001.Click += (s, e) => SetStep(10u);
            _btnStep0001.Click += (s, e) => SetStep(1u);
            _btnStepZero.Click += (s, e) => SetStep(0u);
            BindJogButton(_btnXMinus, () => _axisX, -1);
            BindJogButton(_btnXPlus, () => _axisX, +1);
            BindJogButton(_btnYMinus, () => _axisY, -1);
            BindJogButton(_btnYPlus, () => _axisY, +1);
            BindJogButton(_btnZMinus, () => _axisZ, -1);
            BindJogButton(_btnZPlus, () => _axisZ, +1);
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
                SelectAxis(axis);

                if (_rdoStep.Checked)
                    await StepJogAsync(axis, direction);
                else
                    StartContinuousJog(axis, direction);
            };

            button.MouseUp += (s, e) =>
            {
                if (_rdoContinuous.Checked)
                    StopAxis(axisProvider());
            };

            button.MouseLeave += (s, e) =>
            {
                if (_rdoContinuous.Checked)
                    StopAxis(axisProvider());
            };
        }

        private void SelectAxis(BaseAxis axis)
        {
            if (axis == null || _updatingAxisSelector)
                return;

            for (int i = 0; i < _axisSelector.Items.Count; i++)
            {
                var item = _axisSelector.Items[i] as AxisItem;
                if (item != null && item.Axis == axis)
                {
                    _axisSelector.SelectedIndex = i;
                    break;
                }
            }

            UpdateDirectionHighlight();
        }

        private void StartContinuousJog(BaseAxis axis, int direction)
        {
            if (!PrepareAxis(axis))
                return;

            try
            {
                axis.MoveJogContinuous(direction, GetJogSpeedType(), GetSpeed());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task StepJogAsync(BaseAxis axis, int direction)
        {
            if (!PrepareAxis(axis))
                return;

            try
            {
                await axis.MoveJogStepAsync(direction, GetJogSpeedType(), GetStepDistance(), GetSpeed());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private JogSpeedType GetJogSpeedType()
        {
            return _rdoCoarse.Checked ? JogSpeedType.Coarse : JogSpeedType.Fine;
        }

        private double GetSpeed()
        {
            double speed = 0.0;
            try
            {
                if (SpeedProvider != null)
                    speed = SpeedProvider();
            }
            catch { }

            return Math.Max(0.0, speed);
        }

        private double GetStepDistance()
        {
            return (double)_numStep.Value;
        }

        private void SetStep(decimal step)
        {
            if (step <= 0)
                step = _numStep.Minimum;

            _numStep.Value = Math.Min(_numStep.Maximum, Math.Max(_numStep.Minimum, step));
        }

        private void UpdateAxisBindings()
        {
            _updatingAxisSelector = true;
            _axisSelector.Items.Clear();
            AddAxisItem("X", _axisX);
            AddAxisItem("Y", _axisY);
            AddAxisItem("Z", _axisZ);
            AddAxisItem("T", _axisT);
            _axisSelector.SelectedIndex = _axisSelector.Items.Count > 0 ? 0 : -1;
            _updatingAxisSelector = false;

            _btnXMinus.Enabled = _axisX != null;
            _btnXPlus.Enabled = _axisX != null;
            _btnYMinus.Enabled = _axisY != null;
            _btnYPlus.Enabled = _axisY != null;
            _btnZMinus.Enabled = _axisZ != null;
            _btnZPlus.Enabled = _axisZ != null;
            _btnTMinus.Enabled = _axisT != null;
            _btnTPlus.Enabled = _axisT != null;
            _btnStop.Enabled = _axisSelector.Items.Count > 0;
            UpdateDirectionHighlight();
        }

        private void AddAxisItem(string key, BaseAxis axis)
        {
            if (axis != null)
                _axisSelector.Items.Add(new AxisItem(key, axis));
        }

        private void UpdateDirectionHighlight()
        {
            var selected = SelectedAxis;
            MarkButton(_btnXMinus, selected == _axisX);
            MarkButton(_btnXPlus, selected == _axisX);
            MarkButton(_btnYMinus, selected == _axisY);
            MarkButton(_btnYPlus, selected == _axisY);
            MarkButton(_btnZMinus, selected == _axisZ);
            MarkButton(_btnZPlus, selected == _axisZ);
            MarkButton(_btnTMinus, selected == _axisT);
            MarkButton(_btnTPlus, selected == _axisT);
        }

        private BaseAxis SelectedAxis
        {
            get
            {
                var item = _axisSelector.SelectedItem as AxisItem;
                return item != null ? item.Axis : null;
            }
        }

        private void MarkButton(Button button, bool selected)
        {
            button.BackColor = selected && button.Enabled
                ? Color.FromArgb(255, 210, 130)
                : SystemColors.Control;
        }

        private sealed class AxisItem
        {
            public AxisItem(string key, BaseAxis axis)
            {
                Key = key;
                Axis = axis;
            }

            public string Key { get; }

            public BaseAxis Axis { get; }

            public override string ToString()
            {
                return $"{Key} - {Axis.Name}";
            }
        }
    }
}
