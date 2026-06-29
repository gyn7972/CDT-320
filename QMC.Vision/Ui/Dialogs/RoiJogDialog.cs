using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using QMC.Vision.Core;

namespace QMC.Vision.Ui.Dialogs
{
    /// <summary>
    /// ROI 크기·위치 조그 팝업. 현재 (카메라,타깃)의 ROI1~4 를 개별 선택해
    /// 조그 버튼으로 중심(X/Y)·크기(W/H)를 조절하고 <see cref="AutoFocusRoiStore"/> 에 저장한다.
    /// 변경 시 <see cref="RoiChanged"/> 이벤트로 호출자(패널)가 오버레이를 갱신한다.
    /// </summary>
    public partial class RoiJogDialog : Form
    {
        private readonly FocusCamera _camera;
        private readonly FocusTarget _target;
        private int _sel;          // 선택 ROI 인덱스(0~3)
        private double _step = 5;  // 조그 스텝(px)

        /// <summary>ROI 변경 시 발생 — 패널이 구독해 오버레이/그리드 갱신.</summary>
        public event Action RoiChanged;

        public RoiJogDialog(FocusCamera camera, FocusTarget target)
        {
            _camera = camera;
            _target = target;
            InitializeComponent();

            lblTitle.Text = "ROI 조그 — " + camera + " / " + target;

            btnR0.Click += (s, e) => Select(0);
            btnR1.Click += (s, e) => Select(1);
            btnR2.Click += (s, e) => Select(2);
            btnR3.Click += (s, e) => Select(3);

            btnS1.Click += (s, e) => SetStep(1);
            btnS5.Click += (s, e) => SetStep(5);
            btnS10.Click += (s, e) => SetStep(10);

            btnXm.Click += (s, e) => Jog(-_step, 0, 0, 0);
            btnXp.Click += (s, e) => Jog(+_step, 0, 0, 0);
            btnYm.Click += (s, e) => Jog(0, -_step, 0, 0);
            btnYp.Click += (s, e) => Jog(0, +_step, 0, 0);
            btnWm.Click += (s, e) => Jog(0, 0, -_step, 0);
            btnWp.Click += (s, e) => Jog(0, 0, +_step, 0);
            btnHm.Click += (s, e) => Jog(0, 0, 0, -_step);
            btnHp.Click += (s, e) => Jog(0, 0, 0, +_step);

            btnApply.Click += (s, e) => ApplyInput();
            btnClose.Click += (s, e) => Close();

            SetStep(5);
            Select(0);
        }

        private void SetStep(double step)
        {
            _step = step;
            btnS1.BackColor = step == 1 ? SystemColors.Highlight : Color.White;
            btnS5.BackColor = step == 5 ? SystemColors.Highlight : Color.White;
            btnS10.BackColor = step == 10 ? SystemColors.Highlight : Color.White;
            btnS1.ForeColor = step == 1 ? Color.White : Color.Black;
            btnS5.ForeColor = step == 5 ? Color.White : Color.Black;
            btnS10.ForeColor = step == 10 ? Color.White : Color.Black;
        }

        private void Select(int idx)
        {
            _sel = idx;
            var btns = new[] { btnR0, btnR1, btnR2, btnR3 };
            for (int i = 0; i < btns.Length; i++)
                btns[i].BackColor = i == idx ? Color.FromArgb(0x22, 0x22, 0x22) : Color.White;
            UpdateInfo();
        }

        private Roi GetOrDefault()
        {
            Roi r = AutoFocusRoiStore.GetRoi(_camera, _target, _sel);
            if (r != null && r.Width > 0 && r.Height > 0) return r;
            // 미설정 → 화면 중앙 기본 ROI(640x480 가정).
            return new Roi { CenterX = 320, CenterY = 240, Width = 80, Height = 60, Name = "ROI" + (_sel + 1) };
        }

        private void Jog(double dx, double dy, double dw, double dh)
        {
            try
            {
                Roi r = GetOrDefault();
                r.CenterX += dx;
                r.CenterY += dy;
                r.Width = Math.Max(8, r.Width + dw);
                r.Height = Math.Max(8, r.Height + dh);
                AutoFocusRoiStore.SetRoi(_camera, _target, _sel, r);
                UpdateInfo();
                RoiChanged?.Invoke();
            }
            catch { }
        }

        private void UpdateInfo()
        {
            Roi r = AutoFocusRoiStore.GetRoi(_camera, _target, _sel);
            var inv = CultureInfo.InvariantCulture;
            if (r != null && r.Width > 0)
            {
                lblInfo.Text = "ROI" + (_sel + 1) + "  X=" + r.CenterX.ToString("F0", inv) + "  Y=" + r.CenterY.ToString("F0", inv) +
                               "  W=" + r.Width.ToString("F0", inv) + "  H=" + r.Height.ToString("F0", inv);
                txtX.Text = r.CenterX.ToString("F0", inv);
                txtY.Text = r.CenterY.ToString("F0", inv);
                txtW.Text = r.Width.ToString("F0", inv);
                txtH.Text = r.Height.ToString("F0", inv);
            }
            else
            {
                lblInfo.Text = "ROI" + (_sel + 1) + "  미설정 — 값 입력 후 적용 또는 조그";
                Roi d = GetOrDefault();
                txtX.Text = d.CenterX.ToString("F0", inv);
                txtY.Text = d.CenterY.ToString("F0", inv);
                txtW.Text = d.Width.ToString("F0", inv);
                txtH.Text = d.Height.ToString("F0", inv);
            }
        }

        /// <summary>입력칸의 X/Y/W/H 값을 그대로 ROI 에 적용.</summary>
        private void ApplyInput()
        {
            try
            {
                var inv = CultureInfo.InvariantCulture;
                if (!double.TryParse(txtX.Text, NumberStyles.Float, inv, out double x) ||
                    !double.TryParse(txtY.Text, NumberStyles.Float, inv, out double y) ||
                    !double.TryParse(txtW.Text, NumberStyles.Float, inv, out double w) ||
                    !double.TryParse(txtH.Text, NumberStyles.Float, inv, out double h))
                {
                    lblInfo.Text = "입력값이 숫자가 아닙니다.";
                    return;
                }
                Roi r = GetOrDefault();
                r.CenterX = x; r.CenterY = y;
                r.Width = Math.Max(8, w); r.Height = Math.Max(8, h);
                AutoFocusRoiStore.SetRoi(_camera, _target, _sel, r);
                UpdateInfo();
                RoiChanged?.Invoke();
            }
            catch (Exception ex) { lblInfo.Text = "적용 실패: " + ex.Message; }
        }
    }
}
