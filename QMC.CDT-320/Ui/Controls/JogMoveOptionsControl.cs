using QMC.Common.Motion;
using System;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>조그 운전의 속도 타입, 이동 모드, 스텝 거리를 선택하는 공용 컨트롤입니다.</summary>
    public partial class JogMoveOptionsControl : UserControl
    {
        /// <summary>옵션 값이 변경될 때 발생합니다.</summary>
        public event EventHandler OptionsChanged;

        /// <summary>현재 선택된 조그 속도 타입입니다.</summary>
        public JogSpeedType SpeedType => _rdoCoarse.Checked ? JogSpeedType.Coarse : JogSpeedType.Fine;

        /// <summary>스텝 이동 모드인지 여부입니다.</summary>
        public bool IsStepMode => _rdoStep.Checked;

        /// <summary>스텝 이동 거리입니다.</summary>
        public double StepDistance => (double)_numStep.Value;

        /// <summary>JogMoveOptionsControl을 생성합니다.</summary>
        public JogMoveOptionsControl()
        {
            InitializeComponent();
            BindEvents();
        }

        private void BindEvents()
        {
            _rdoFine.CheckedChanged += OnOptionsChanged;
            _rdoCoarse.CheckedChanged += OnOptionsChanged;
            _rdoContinuous.CheckedChanged += OnOptionsChanged;
            _rdoStep.CheckedChanged += OnOptionsChanged;
            _numStep.ValueChanged += OnOptionsChanged;
            _btnStep1.Click += (s, e) => SetStep(1.0m);
            _btnStep01.Click += (s, e) => SetStep(0.1m);
            _btnStep001.Click += (s, e) => SetStep(0.01m);
            _btnStep0001.Click += (s, e) => SetStep(0.001m);
            _btnStepZero.Click += (s, e) => SetStep(0.0m);
        }

        private void SetStep(decimal step)
        {
            if (step <= 0)
                step = _numStep.Minimum;

            _numStep.Value = Math.Min(_numStep.Maximum, Math.Max(_numStep.Minimum, step));
        }

        private void OnOptionsChanged(object sender, EventArgs e)
        {
            OptionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
