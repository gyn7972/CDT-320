using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Ui.Controls;
using QMC.CDT320.VisionComm;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// 비전 카메라 모니터 — Vision 의 모듈별 그랩 스트림(이미지+메타)을 받아 표시한다.
    /// 시퀀서/그랩이 돌면 그 프레임이 실시간으로 보이고, 메타(스케일/판정/결과)로 측정·오버레이까지 반영.
    /// 코드 전용 폼(Designer 없음).
    /// </summary>
    public sealed class VisionMonitorDialog : Form
    {
        // 모듈명 → 뷰어 포트 (VisionConfig 기준)
        private sealed class ModPort
        {
            public readonly string Name; public readonly int Port;
            public ModPort(string name, int port) { Name = name; Port = port; }
        }
        private static readonly ModPort[] Modules =
        {
            new ModPort("WaferVision (5200)",      5200),
            new ModPort("BottomInspection (5201)", 5201),
            new ModPort("BinVision (5203)",        5203),
            new ModPort("TopSideVision (5205)",    5205),
            new ModPort("BottomSideVision (5206)", 5206),
        };

        private readonly ComboBox _cbModule = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly TextBox  _txtHost  = new TextBox  { Width = 130 };
        private readonly Button   _btnConn  = new Button   { Text = "Connect", Width = 90 };
        private readonly Button   _btnStop  = new Button   { Text = "Disconnect", Width = 90, Enabled = false };
        private readonly Label    _lblStat  = new Label    { AutoSize = true, ForeColor = Color.DimGray, Text = "idle" };
        private readonly CameraViewBase _cam = new CameraViewBase { Dock = DockStyle.Fill, ShowToolbar = true };

        private VisionFrameClient _client;

        public VisionMonitorDialog()
        {
            Text = "Vision 카메라 모니터";
            Width = 1000; Height = 760;
            StartPosition = FormStartPosition.CenterParent;

            foreach (var m in Modules) _cbModule.Items.Add(m.Name);
            _cbModule.SelectedIndex = 0;
            _txtHost.Text = VisionHub.Host ?? "127.0.0.1";

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(6, 6, 6, 0), WrapContents = false };
            top.Controls.Add(new Label { Text = "Module", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
            top.Controls.Add(_cbModule);
            top.Controls.Add(new Label { Text = "Vision IP", AutoSize = true, Margin = new Padding(12, 8, 4, 0) });
            top.Controls.Add(_txtHost);
            top.Controls.Add(_btnConn);
            top.Controls.Add(_btnStop);
            top.Controls.Add(new Label { Text = "  ", AutoSize = true });
            top.Controls.Add(_lblStat);

            Controls.Add(_cam);
            Controls.Add(top);

            _btnConn.Click += (s, e) => Connect();
            _btnStop.Click += (s, e) => Disconnect();
        }

        private void Connect()
        {
            Disconnect();
            int port = Modules[Math.Max(0, _cbModule.SelectedIndex)].Port;
            string host = string.IsNullOrWhiteSpace(_txtHost.Text) ? "127.0.0.1" : _txtHost.Text.Trim();

            _client = new VisionFrameClient(host, port);
            _client.Frame  += OnFrame;
            _client.Status += OnStatus;
            _client.Start();

            _btnConn.Enabled = false; _btnStop.Enabled = true;
            _cbModule.Enabled = false; _txtHost.Enabled = false;
        }

        private void Disconnect()
        {
            try { if (_client != null) { _client.Frame -= OnFrame; _client.Status -= OnStatus; _client.Dispose(); } } catch { }
            _client = null;
            _btnConn.Enabled = true; _btnStop.Enabled = false;
            _cbModule.Enabled = true; _txtHost.Enabled = true;
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
                    _cam.MmPerPixelX = meta.ScaleX;  // 측정 mm 환산
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
            try { BeginInvoke(new Action(() => { _lblStat.Text = s; })); } catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Disconnect();
            base.OnFormClosing(e);
        }
    }
}
