using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Ui.Controls;
using QMC.CDT320.VisionComm;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// Vision 뷰어(이미지 스트림) 1개를 표시하는 재사용 패널.
    /// <para>
    /// 명령 채널과 별개인 뷰어 포트에 <see cref="VisionFrameClient"/>로 접속해 프레임(이미지+메타)을 받아
    /// <see cref="CameraViewBase"/>에 그린다(스케일/판정/결과라인/오버레이 마크 반영). 수신 전용 — 모션과 무관.
    /// 핸들이 만들어지면 자동 시작, Dispose 시 정리. 코드 전용 컨트롤(Designer 없음).
    /// </para>
    /// </summary>
    public sealed class VisionViewerPanel : UserControl
    {
        private readonly CameraViewBase _cam = new CameraViewBase { Dock = DockStyle.Fill, ShowToolbar = true };
        private readonly Label _lblStat  = new Label { Dock = DockStyle.Top, Height = 22, ForeColor = Color.DimGray, Text = "이미지 idle", Padding = new Padding(6, 3, 0, 0) };
        private readonly Label _lblTitle = new Label { Dock = DockStyle.Top, Height = 22, Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold), Padding = new Padding(6, 3, 0, 0) };

        private readonly string _host;
        private readonly int _port;
        private VisionFrameClient _client;

        public VisionViewerPanel(string host, int port, string title)
        {
            _host = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();
            _port = port;
            _lblTitle.Text = (string.IsNullOrWhiteSpace(title) ? "이미지" : title) +
                             (port > 0 ? "  (뷰어 " + port + ")" : "  (뷰어 없음)");

            // Fill(_cam)을 먼저 추가해 뒤쪽 z-order에 두고, Top 라벨을 나중에 추가한다.
            Controls.Add(_cam);
            Controls.Add(_lblStat);
            Controls.Add(_lblTitle);

            HandleCreated += (s, e) => StartStream();
        }

        public void StartStream()
        {
            if (_port <= 0) { _lblStat.Text = "뷰어 포트 없음"; return; }
            StopStream();
            try
            {
                _client = new VisionFrameClient(_host, _port);
                _client.Frame  += OnFrame;
                _client.Status += OnStatus;
                _client.Start();
            }
            catch (Exception ex)
            {
                _lblStat.Text = "이미지 시작 실패: " + ex.Message;
            }
        }

        public void StopStream()
        {
            try
            {
                if (_client != null)
                {
                    _client.Frame  -= OnFrame;
                    _client.Status -= OnStatus;
                    _client.Dispose();
                }
            }
            catch { }
            _client = null;
        }

        // 백그라운드 수신 스레드 → UI 마샬링
        private void OnFrame(VisionFrameMeta meta, Bitmap bmp)
        {
            if (bmp == null) return;
            if (IsDisposed || !IsHandleCreated) { bmp.Dispose(); return; }
            try { BeginInvoke(new Action(() => ApplyFrame(meta, bmp))); }
            catch { try { bmp.Dispose(); } catch { } }
        }

        private void ApplyFrame(VisionFrameMeta meta, Bitmap bmp)
        {
            try
            {
                _cam.SetImage(bmp);                  // 내부 복제
                if (meta != null)
                {
                    _cam.MmPerPixelX = meta.ScaleX;
                    _cam.MmPerPixelY = meta.ScaleY;
                    _cam.InfoText = (meta.Module ?? "") + "\r\nW:" + meta.Width + " H:" + meta.Height;
                    _cam.SetVerdict(meta.Verdict, meta.VerdictPass);
                    _cam.SetResultLines(meta.ResultLines);
                    _cam.SetOverlay(System.Drawing.RectangleF.Empty, MarksOf(meta));
                }
            }
            finally { bmp.Dispose(); }
        }

        private static List<OverlayMark> MarksOf(VisionFrameMeta meta)
        {
            if (meta?.Marks == null || meta.Marks.Length == 0) return null;
            var list = new List<OverlayMark>(meta.Marks.Length);
            foreach (var m in meta.Marks) list.Add(new OverlayMark(m.X, m.Y, m.Score));
            return list;
        }

        private void OnStatus(string s)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke(new Action(() => { _lblStat.Text = "이미지: " + s; })); } catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                StopStream();
            base.Dispose(disposing);
        }
    }
}
