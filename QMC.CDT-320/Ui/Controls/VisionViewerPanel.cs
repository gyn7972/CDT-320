using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Ui.Controls;
using QMC.CDT320.VisionComm;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// Vision 뷰어(이미지 스트림) 1개를 표시하는 재사용 패널 — Vision PC 카메라 UI와 동일 구성(별도 버튼 없음).
    /// <para>
    /// 뷰어 포트에 <see cref="VisionViewerSource"/>를 <see cref="CameraViewBase.AttachSource"/>로 연결한다.
    /// 카메라 컨트롤의 네이티브 툴바(Grab/Live/Stop/Save/Load/측정/맞춤/CrossLine/Mag)가 그대로 동작하며,
    /// <b>툴바 Grab</b>은 명령 채널로 Vision 에 촬상(EXPOSE)을 보내고(있으면) 그 결과 프레임을 표시한다.
    /// 허용/거부는 Vision 게이트가 결정: <b>READY(O) armed 면 거부</b>, 해제 상태면 촬상 가능(거부 시 상태줄에 표시).
    /// 프레임 메타(스케일/판정/결과/마크)는 오버레이로 표시. 수신/표시 전용 — 모션 무관. 코드 전용 컨트롤.
    /// </para>
    /// </summary>
    public sealed class VisionViewerPanel : UserControl
    {
        private readonly CameraViewBase _cam = new CameraViewBase { Dock = DockStyle.Fill, ShowToolbar = true };
        private readonly Label _lblTitle = new Label { AutoSize = true, Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold), Margin = new Padding(6, 7, 8, 0) };
        private readonly Label _lblStat  = new Label { AutoSize = true, ForeColor = Color.DimGray, Text = "idle", Margin = new Padding(8, 7, 0, 0) };

        private readonly int _port;
        private VisionViewerSource _source;

        /// <param name="commandClient">툴바 Grab 시 Vision 에 촬상(EXPOSE)을 보내는 명령 채널. null 이면 수동 수신만.</param>
        public VisionViewerPanel(string host, int viewerPort, string title, VisionTcpClient commandClient = null)
        {
            string h = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();
            _port = viewerPort;
            _lblTitle.Text = (string.IsNullOrWhiteSpace(title) ? "이미지" : title) +
                             (viewerPort > 0 ? "  (뷰어 " + viewerPort + ")" : "  (뷰어 없음)");

            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, WrapContents = false, Padding = new Padding(4, 2, 4, 2) };
            bar.Controls.Add(_lblTitle);
            bar.Controls.Add(_lblStat);

            // Fill(_cam)을 먼저 추가해 뒤쪽 z-order, Top(bar)을 나중에.
            Controls.Add(_cam);
            Controls.Add(bar);

            if (_port > 0)
            {
                _source = new VisionViewerSource(h, _port, 2000, commandClient);
                _source.FrameMeta += OnMeta;
                _source.Status += OnStatus;
                _cam.AttachSource(_source);   // 툴바 Grab/Live/Stop이 이 소스를 제어(접속·촬상은 누를 때).
                _lblStat.Text = "대기 — Grab/Live를 누르면 연결";
            }
            else
            {
                _lblStat.Text = "뷰어 포트 없음";
            }

            // 자동 연결하지 않는다. 사용자가 툴바 Grab/Live를 누를 때 접속한다.
        }

        /// <summary>외부에서 LIVE 시작(툴바 Live와 동일).</summary>
        public void StartLive() { if (_port > 0) { try { _cam.StartLive(); } catch { } } }
        /// <summary>외부에서 LIVE 정지(툴바 Stop과 동일).</summary>
        public void StopLive()  { try { _cam.StopLive(); } catch { } }

        /// <summary>외부(테스트 다이얼로그)에서 결과 라인을 영상 우측하단에 직접 표시.
        /// ApplyMeta 는 빈 메타로 이 값을 덮어쓰지 않는다.</summary>
        public void SetResultLines(string[] lines) { if (IsDisposed) return; try { _cam.SetResultLines(lines); } catch { } }
        public void SetVerdictText(string text, bool pass) { if (IsDisposed) return; try { _cam.SetVerdict(text, pass); } catch { } }

        // ── 소스 상태 메시지(촬상 OK / READY 거부 등) → 상태줄 ──
        private void OnStatus(string s)
        {
            if (IsDisposed || !IsHandleCreated || string.IsNullOrEmpty(s)) return;
            try { BeginInvoke(new Action(() => { _lblStat.Text = s; })); } catch { }
        }

        // 백그라운드 스레드(메타) → UI 마샬링하여 오버레이 반영
        private void OnMeta(VisionFrameMeta meta)
        {
            if (meta == null) return;
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke(new Action(() => ApplyMeta(meta))); } catch { }
        }

        private void ApplyMeta(VisionFrameMeta meta)
        {
            try
            {
                _cam.MmPerPixelX = meta.ScaleX;
                _cam.MmPerPixelY = meta.ScaleY;
                _cam.InfoText = (meta.Module ?? "") + "\r\nW:" + meta.Width + " H:" + meta.Height;
                // 메타에 값이 있을 때만 갱신 — 테스트에서 외부(SetResultLines)로 설정한 결과가 빈 메타에 덮이지 않게.
                if (!string.IsNullOrEmpty(meta.Verdict)) _cam.SetVerdict(meta.Verdict, meta.VerdictPass);
                if (meta.ResultLines != null && meta.ResultLines.Length > 0) _cam.SetResultLines(meta.ResultLines);
                _cam.SetOverlay(RoiOf(meta), MarksOf(meta));
            }
            catch { }
        }

        /// <summary>검색 ROI(이미지 px, 좌상단 기준) → 노란 박스 사각형. W/H 0 이면 Empty(미표시).</summary>
        private static System.Drawing.RectangleF RoiOf(VisionFrameMeta meta)
        {
            if (meta == null || meta.RoiW <= 0 || meta.RoiH <= 0) return System.Drawing.RectangleF.Empty;
            return new System.Drawing.RectangleF((float)meta.RoiX, (float)meta.RoiY, (float)meta.RoiW, (float)meta.RoiH);
        }

        private static List<OverlayMark> MarksOf(VisionFrameMeta meta)
        {
            if (meta?.Marks == null || meta.Marks.Length == 0) return null;
            var list = new List<OverlayMark>(meta.Marks.Length);
            foreach (var m in meta.Marks)
                list.Add(new OverlayMark(m.X, m.Y, m.Score, m.Angle, m.BoxW, m.BoxH));
            return list;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _cam.StopLive(); } catch { }
                try { if (_source != null) { _source.FrameMeta -= OnMeta; _source.Status -= OnStatus; _source.Dispose(); } } catch { }
                _source = null;
            }
            base.Dispose(disposing);
        }
    }
}
