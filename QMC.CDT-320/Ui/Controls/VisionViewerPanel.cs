using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.VisionComm;
using QMC.Common.Ui.Controls;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// Vision PC 이미지 스트림 1개를 표시하는 공용 viewer panel.
    /// UI 생성/배치는 Designer에 두고, 이 파일은 source 연결과 overlay 표시만 담당한다.
    /// </summary>
    public sealed partial class VisionViewerPanel : UserControl
    {
        private int _port;
        private VisionTcpClient _commandClient;
        private VisionViewerSource _source;

        public VisionViewerPanel()
        {
            InitializeComponent();
        }

        public VisionViewerPanel(string host, int viewerPort, string title, VisionTcpClient commandClient = null)
            : this()
        {
            Configure(host, viewerPort, title, commandClient);
        }

        public void Configure(string host, int viewerPort, string title, VisionTcpClient commandClient)
        {
            string targetHost = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();
            _port = viewerPort;
            _commandClient = commandClient;
            _lblTitle.Text = (string.IsNullOrWhiteSpace(title) ? "이미지" : title) +
                             (viewerPort > 0 ? "  (뷰어 " + viewerPort + ")" : "  (뷰어 없음)");

            StopLive();
            try
            {
                if (_source != null)
                {
                    _source.FrameMeta -= OnMeta;
                    _source.Status -= OnStatus;
                    _source.Dispose();
                    _source = null;
                }
            }
            catch
            {
            }
            finally
            {
            }

            if (_port > 0)
            {
                _source = new VisionViewerSource(targetHost, _port, 2000, _commandClient);
                _source.FrameMeta += OnMeta;
                _source.Status += OnStatus;
                _cam.AttachSource(_source);
                _lblStat.Text = "대기 - Grab/Live를 누르면 연결합니다.";
            }
            else
            {
                _lblStat.Text = "뷰어 포트 없음";
            }
        }

        public void StartLive()
        {
            if (_port <= 0)
                return;

            try
            {
                _cam.StartLive();
            }
            catch
            {
            }
            finally
            {
            }
        }

        public void StopLive()
        {
            try
            {
                _cam.StopLive();
            }
            catch
            {
            }
            finally
            {
            }
        }

        public void SetVerdictText(string text, bool pass)
        {
            try
            {
                _cam.SetVerdict(text, pass);
            }
            catch
            {
            }
            finally
            {
            }
        }

        public void SetResultLines(string[] lines)
        {
            try
            {
                _cam.SetResultLines(lines);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void OnStatus(string message)
        {
            if (IsDisposed || !IsHandleCreated || string.IsNullOrEmpty(message))
                return;

        /// <summary>결과 라인(우측하단 텍스트) 오버레이 — 내부 카메라뷰로 위임.</summary>
        public void SetResultLines(string[] lines) { if (IsDisposed) return; try { _cam.SetResultLines(lines); } catch { } }
        /// <summary>판정(OK/NG, 우측상단) 오버레이 — 내부 카메라뷰로 위임.</summary>
        public void SetVerdictText(string text, bool pass) { if (IsDisposed) return; try { _cam.SetVerdict(text, pass); } catch { } }

        // ── 소스 상태 메시지(촬상 OK / READY 거부 등) → 상태줄 ──
        private void OnStatus(string s)
        {
            if (IsDisposed || !IsHandleCreated || string.IsNullOrEmpty(s)) return;
            try { BeginInvoke(new Action(() => { _lblStat.Text = s; })); } catch { }
        }

        private void OnMeta(VisionFrameMeta meta)
        {
            if (meta == null || IsDisposed || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(new Action(() => ApplyMeta(meta)));
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ApplyMeta(VisionFrameMeta meta)
        {
            try
            {
                _cam.MmPerPixelX = meta.ScaleX;
                _cam.MmPerPixelY = meta.ScaleY;
                _cam.InfoText = (meta.Module ?? string.Empty) + "\r\nW:" + meta.Width + " H:" + meta.Height;
                _cam.SetVerdict(meta.Verdict, meta.VerdictPass);
                _cam.SetResultLines(meta.ResultLines);
                _cam.SetOverlay(GetRoi(meta), GetMarks(meta));
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static RectangleF GetRoi(VisionFrameMeta meta)
        {
            if (meta == null || meta.RoiW <= 0 || meta.RoiH <= 0)
                return RectangleF.Empty;

            return new RectangleF(
                (float)meta.RoiX,
                (float)meta.RoiY,
                (float)meta.RoiW,
                (float)meta.RoiH);
        }

        /// <summary>메타의 ROI(검색/검사 영역, 이미지 좌표 top-left) → RectangleF. 없으면 Empty.</summary>
        private static System.Drawing.RectangleF RoiOf(VisionFrameMeta meta)
        {
            if (meta == null || meta.RoiW <= 0 || meta.RoiH <= 0) return System.Drawing.RectangleF.Empty;
            return new System.Drawing.RectangleF((float)meta.RoiX, (float)meta.RoiY, (float)meta.RoiW, (float)meta.RoiH);
        }

        private static List<OverlayMark> MarksOf(VisionFrameMeta meta)
        {
            if (meta == null || meta.Marks == null || meta.Marks.Length == 0)
                return null;

            var list = new List<OverlayMark>(meta.Marks.Length);
            // 각도/박스 크기까지 전달 → 핸들러 뷰어가 회전 박스(매칭/측면 결함)를 그린다.
            foreach (var m in meta.Marks) list.Add(new OverlayMark(m.X, m.Y, m.Score, m.Angle, m.BoxW, m.BoxH));
            return list;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                StopLive();
            }
            catch
            {
            }
            finally
            {
            }

            try
            {
                if (_source != null)
                {
                    _source.FrameMeta -= OnMeta;
                    _source.Status -= OnStatus;
                    _source.Dispose();
                    _source = null;
                }
            }
            catch
            {
            }
            finally
            {
                base.OnHandleDestroyed(e);
            }
        }
    }
}
