using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Vision.Core;

namespace QMC.Vision.Ui.Dialogs
{
    /// <summary>
    /// 처리(엣지 응답) 이미지 확인 팝업. "현재 프레임 처리" 로 현재 카메라 프레임을 받아
    /// <see cref="AutoFocusCore.BuildResponseImage"/> 로 알고리즘이 보는 응답 맵을 표시한다.
    /// 전체 또는 ROI1~4 별로 확인 가능.
    /// </summary>
    public partial class ProcessedImageDialog : Form
    {
        private readonly FocusCamera _camera;
        private readonly FocusTarget _target;
        private readonly Func<Bitmap> _frameProvider;
        private int _sel = -1;   // -1=전체, 0~3=ROI

        public ProcessedImageDialog(FocusCamera camera, FocusTarget target, Func<Bitmap> frameProvider)
        {
            _camera = camera;
            _target = target;
            _frameProvider = frameProvider;
            InitializeComponent();

            Text = "처리 이미지 — " + camera + " / " + target;

            btnAll.Click += (s, e) => { _sel = -1; Process(); };
            btnP0.Click += (s, e) => { _sel = 0; Process(); };
            btnP1.Click += (s, e) => { _sel = 1; Process(); };
            btnP2.Click += (s, e) => { _sel = 2; Process(); };
            btnP3.Click += (s, e) => { _sel = 3; Process(); };
            btnRefresh.Click += (s, e) => Process();
            btnApplyTh.Click += (s, e) => ApplyThreshold();
            btnClose.Click += (s, e) => Close();

            try
            {
                int cur = QMC.Vision.Config.VisionConfigStore.Current != null
                    ? QMC.Vision.Config.VisionConfigStore.Current.AutoFocusThreshold : 100;
                numTh.Value = Math.Max(0, Math.Min(255, cur));
            }
            catch { }
        }

        /// <summary>임계값을 config 에 저장하고 다시 처리.</summary>
        private void ApplyThreshold()
        {
            try
            {
                var cfg = QMC.Vision.Config.VisionConfigStore.Current;
                if (cfg != null)
                {
                    cfg.AutoFocusThreshold = (int)numTh.Value;
                    try { QMC.Vision.Config.VisionConfigStore.Save(); } catch { }
                }
                Process();
            }
            catch (Exception ex) { lblInfo.Text = "임계값 적용 실패: " + ex.Message; }
        }

        private void Process()
        {
            // 프레임 확보(UI 스레드)
            Bitmap frame = _frameProvider != null ? _frameProvider() : null;
            if (frame == null) { lblInfo.Text = "현재 프레임 없음 — 카메라 Grab/Live/Load 후 다시 시도."; return; }

            int afTh = QMC.Vision.Config.VisionConfigStore.Current != null
                ? QMC.Vision.Config.VisionConfigStore.Current.AutoFocusThreshold : 100;

            Rectangle? roiRect = null;
            string label;
            if (_sel < 0) label = "전체";
            else
            {
                Roi roi = AutoFocusRoiStore.GetRoi(_camera, _target, _sel);
                if (roi == null || roi.Width <= 0 || roi.Height <= 0)
                {
                    lblInfo.Text = "ROI" + (_sel + 1) + " 미설정 — 먼저 ROI를 지정하세요.";
                    try { frame.Dispose(); } catch { }
                    return;
                }
                roiRect = roi.BoundingBox;
                label = "ROI" + (_sel + 1);
            }

            // 큰 이미지 채점은 UI 를 멈추므로 백그라운드에서(단일 패스로 Score+이미지).
            SetButtons(false);
            lblInfo.Text = label + "  처리 중...";
            Task.Run(() =>
            {
                double score = 0;
                Bitmap resp = null;
                try { resp = AutoFocusCore.BuildResponseImage(frame, roiRect, afTh, out score); }
                catch { }
                finally { try { frame.Dispose(); } catch { } }

                try
                {
                    BeginInvoke((Action)(() =>
                    {
                        Image old = picProc.Image;
                        picProc.Image = resp;
                        if (old != null) old.Dispose();
                        lblInfo.Text = resp != null
                            ? label + "  Score=" + score.ToString("F1") + "  (임계값=" + afTh + ")"
                            : label + "  처리 실패 — 프레임/ROI 확인";
                        SetButtons(true);
                    }));
                }
                catch { }
            });
        }

        private void SetButtons(bool en)
        {
            try
            {
                btnAll.Enabled = en; btnP0.Enabled = en; btnP1.Enabled = en;
                btnP2.Enabled = en; btnP3.Enabled = en; btnRefresh.Enabled = en; btnApplyTh.Enabled = en;
            }
            catch { }
        }
    }
}
