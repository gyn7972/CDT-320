using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Ui.Controls;
using QMC.CDT320.VisionComm;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// Vision 뷰어(이미지 스트림) 1개를 표시하는 재사용 패널 — Vision PC 카메라 UI와 동일 구성.
    /// <para>
    /// UI(카메라뷰 + 제목/상태 바)는 Designer(.Designer.cs)에 있고, 여기에는 런타임 로직만 둔다.
    /// 사용 시 <see cref="Configure"/>로 host/포트/명령 채널을 주입한다(생성자는 매개변수 없음 — VS 디자이너 호환).
    /// 툴바 Grab 은 명령 채널로 Vision 에 촬상(EXPOSE)을 보내고(있으면) 결과 프레임을 표시한다(READY 게이트는 Vision 측).
    /// 메타(스케일/판정/결과/마크)는 오버레이로 표시. 수신/표시 전용 — 모션 무관.
    /// </para>
    /// </summary>
    public sealed partial class VisionViewerPanel : UserControl
    {
        private int _port;
        private VisionTcpClient _cmd;   // 툴바 Grab 시 Vision 에 촬상(EXPOSE) 명령. null 이면 수동 수신만.
        private VisionViewerSource _source;

        public VisionViewerPanel()
        {
            InitializeComponent();
        }

        /// <summary>편의 생성자 — UI 는 Designer(InitializeComponent)가 만들고, 런타임 인자는 즉시 Configure 한다. (기존 호출부 호환)</summary>
        public VisionViewerPanel(string host, int viewerPort, string title, VisionTcpClient commandClient = null) : this()
        {
            Configure(host, viewerPort, title, commandClient);
        }

        /// <summary>런타임 주입 — 뷰어 포트/제목/명령 채널 설정 후 소스 연결.</summary>
        public void Configure(string host, int viewerPort, string title, VisionTcpClient commandClient)
        {
            string h = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();
            _port = viewerPort;
            _cmd = commandClient;
            _lblTitle.Text = (string.IsNullOrWhiteSpace(title) ? "이미지" : title) +
                             (viewerPort > 0 ? "  (뷰어 " + viewerPort + ")" : "  (뷰어 없음)");

            StopLive();
            try { if (_source != null) { _source.FrameMeta -= OnMeta; _source.Status -= OnStatus; _source.Dispose(); _source = null; } } catch { }

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
        }

        /// <summary>외부에서 LIVE 시작(툴바 Live와 동일).</summary>
        public void StartLive() { if (_port > 0) { try { _cam.StartLive(); } catch { } } }
        /// <summary>외부에서 LIVE 정지(툴바 Stop과 동일).</summary>
        public void StopLive()  { try { _cam.StopLive(); } catch { } }

        /// <summary>오버레이 판정 텍스트 직접 설정(우상단 OK/NG). 메타와 별개로 호출부가 직접 그릴 때 사용.</summary>
        public void SetVerdictText(string text, bool pass) { try { _cam.SetVerdict(text, pass); } catch { } }
        /// <summary>오버레이 결과 라인 직접 설정(우하단).</summary>
        public void SetResultLines(string[] lines) { try { _cam.SetResultLines(lines); } catch { } }

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
                _cam.SetVerdict(meta.Verdict, meta.VerdictPass);
                _cam.SetResultLines(meta.ResultLines);
                _cam.SetOverlay(System.Drawing.RectangleF.Empty, MarksOf(meta));
            }
            catch { }
        }

        private static List<OverlayMark> MarksOf(VisionFrameMeta meta)
        {
            if (meta?.Marks == null || meta.Marks.Length == 0) return null;
            var list = new List<OverlayMark>(meta.Marks.Length);
            foreach (var m in meta.Marks) list.Add(new OverlayMark(m.X, m.Y, m.Score));
            return list;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { StopLive(); } catch { }
            try { if (_source != null) { _source.FrameMeta -= OnMeta; _source.Status -= OnStatus; _source.Dispose(); _source = null; } } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
