using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Core;

namespace QMC.Vision.Ui.Dialogs
{
    /// <summary>
    /// 측정 기반 스케일 캘리브레이션 다이얼로그.
    /// 공용 CameraView(휠 줌/가운데드래그 팬/측정)로 칩 이미지를 보여주고,
    /// [가로 측정]·[세로 측정] 버튼을 눌렀을 때만 두 점 측정 → 입력한 칩 mm와 측정 px로 Scale X/Y 산출.
    /// </summary>
    public partial class ScaleCalibrationDialog : Form
    {
        private enum Axis { None, Width, Height }
        private Axis _axis = Axis.None;

        private double _pxW;
        private double _pxH;

        public double ResultScaleX { get; private set; }
        public double ResultScaleY { get; private set; }
        public double ResultChipWidthMm  { get; private set; }
        public double ResultChipHeightMm { get; private set; }

        public ScaleCalibrationDialog(Bitmap image, double chipWidthMm, double chipHeightMm)
        {
            InitializeComponent();
            if (image != null) _pic.SetImage(image);
            _txtW.Text = chipWidthMm  > 0 ? chipWidthMm.ToString("0.####")  : "";
            _txtH.Text = chipHeightMm > 0 ? chipHeightMm.ToString("0.####") : "";
            _pic.MeasureCompleted += OnMeasured;
        }

        private void OnMeasureWidthClick(object sender, EventArgs e)
        {
            _axis = Axis.Width;
            _pic.BeginMeasure();
            _info.Text = "칩 가로 — 좌/우 끝 두 점을 클릭하세요. (휠=확대/축소, 가운데드래그=이동)";
        }

        private void OnMeasureHeightClick(object sender, EventArgs e)
        {
            _axis = Axis.Height;
            _pic.BeginMeasure();
            _info.Text = "칩 세로 — 상/하 끝 두 점을 클릭하세요. (휠=확대/축소, 가운데드래그=이동)";
        }

        // 두 점 측정이 끝났을 때만 호출됨(버튼으로 BeginMeasure 한 경우). 끝나면 측정 모드 해제.
        private void OnMeasured(double px)
        {
            if (_axis == Axis.Width)       { _pxW = px; _lblWpx.Text = px.ToString("F1") + " px"; }
            else if (_axis == Axis.Height) { _pxH = px; _lblHpx.Text = px.ToString("F1") + " px"; }
            else return;   // 버튼 없이 들어온 측정은 무시(안전)

            _axis = Axis.None;
            // EndMeasure 호출하지 않음 — 측정선 유지(완료 후 클릭은 CameraView 가 무시).
            _info.Text = "측정 완료. 가로/세로 모두 측정 후 [계산 & 적용].";
        }

        private void OnFitClick(object sender, EventArgs e) => _pic.ZoomFit();

        private void OnOkClick(object sender, EventArgs e)
        {
            double wMm, hMm;
            if (!double.TryParse((_txtW.Text ?? "").Trim(), out wMm) || wMm <= 0)
            { Warn("칩 가로(mm)를 올바르게 입력하세요."); return; }
            if (!double.TryParse((_txtH.Text ?? "").Trim(), out hMm) || hMm <= 0)
            { Warn("칩 세로(mm)를 올바르게 입력하세요."); return; }
            if (_pxW <= 0) { Warn("[가로 측정]으로 칩 가로 픽셀을 먼저 측정하세요."); return; }
            if (_pxH <= 0) { Warn("[세로 측정]으로 칩 세로 픽셀을 먼저 측정하세요."); return; }

            ResultScaleX = wMm / _pxW;
            ResultScaleY = hMm / _pxH;
            ResultChipWidthMm  = wMm;
            ResultChipHeightMm = hMm;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Warn(string msg)
            => MessageBox.Show(this, msg, "스케일 캘리브레이션", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
