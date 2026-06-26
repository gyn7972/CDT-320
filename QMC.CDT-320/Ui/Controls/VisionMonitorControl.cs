using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;
using QMC.Common.Ui.Controls;

namespace QMC.CDT_320.Ui.Controls
{
    public sealed partial class VisionMonitorControl : UserControl
    {
        private sealed class ModulePort
        {
            public readonly string Name;
            public readonly int Port;

            public ModulePort(string name, int port)
            {
                Name = name;
                Port = port;
            }

            public override string ToString()
            {
                return Name + " (" + Port + ")";
            }
        }

        private VisionFrameClient _client;

        public VisionMonitorControl()
        {
            InitializeComponent();

            foreach (ModulePort module in BuildModules())
                cbModule.Items.Add(module);

            if (cbModule.Items.Count > 0)
                cbModule.SelectedIndex = 0;

            txtHost.Text = IsDesignerMode() ? "127.0.0.1" : (VisionHub.Host ?? "127.0.0.1");

            btnConnect.Click += (s, e) => Connect();
            btnDisconnect.Click += (s, e) => Disconnect();
        }

        private static bool IsDesignerMode()
        {
            return LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        }

        private static ModulePort[] BuildModules()
        {
            if (IsDesignerMode())
            {
                return new[]
                {
                    new ModulePort("WaferVision", VisionViewerPorts.DefaultWafer),
                    new ModulePort("BottomInspection", VisionViewerPorts.DefaultBottomInspection),
                    new ModulePort("BinVision", VisionViewerPorts.DefaultBin),
                    new ModulePort("TopSideVision", VisionViewerPorts.DefaultTopSide),
                    new ModulePort("BottomSideVision", VisionViewerPorts.DefaultBottomSide),
                };
            }

            return new[]
            {
                new ModulePort("WaferVision", VisionViewerPorts.Wafer),
                new ModulePort("BottomInspection", VisionViewerPorts.BottomInspection),
                new ModulePort("BinVision", VisionViewerPorts.Bin),
                new ModulePort("TopSideVision", VisionViewerPorts.TopSide),
                new ModulePort("BottomSideVision", VisionViewerPorts.BottomSide),
            };
        }

        public void Disconnect()
        {
            try
            {
                if (_client != null)
                {
                    _client.Frame -= OnFrame;
                    _client.Status -= OnStatus;
                    _client.Dispose();
                }
            }
            catch
            {
            }
            finally
            {
                _client = null;
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
                cbModule.Enabled = true;
                txtHost.Enabled = true;
            }
        }

        private void Connect()
        {
            Disconnect();

            ModulePort selected = cbModule.SelectedItem as ModulePort;
            if (selected == null)
            {
                lblStatus.Text = "모듈을 선택하세요.";
                return;
            }

            string host = string.IsNullOrWhiteSpace(txtHost.Text) ? "127.0.0.1" : txtHost.Text.Trim();

            _client = new VisionFrameClient(host, selected.Port);
            _client.Frame += OnFrame;
            _client.Status += OnStatus;
            _client.Start();

            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            cbModule.Enabled = false;
            txtHost.Enabled = false;
            lblStatus.Text = "연결 중...";
        }

        private void OnFrame(VisionFrameMeta meta, Bitmap bitmap)
        {
            if (bitmap == null)
                return;

            if (IsDisposed || !IsHandleCreated)
            {
                bitmap.Dispose();
                return;
            }

            try
            {
                BeginInvoke(new Action(() => ApplyFrame(meta, bitmap)));
            }
            catch
            {
                try { bitmap.Dispose(); } catch { }
            }
        }

        private void ApplyFrame(VisionFrameMeta meta, Bitmap bitmap)
        {
            try
            {
                cameraView.SetImage(bitmap);
                if (meta != null)
                {
                    cameraView.MmPerPixelX = meta.ScaleX;
                    cameraView.MmPerPixelY = meta.ScaleY;
                    cameraView.InfoText = (meta.Module ?? string.Empty) + "\r\nW:" + meta.Width + " H:" + meta.Height;
                    cameraView.SetVerdict(meta.Verdict, meta.VerdictPass);
                    cameraView.SetResultLines(meta.ResultLines);
                    cameraView.SetOverlay(System.Drawing.RectangleF.Empty, BuildMarks(meta));
                }
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        private static List<OverlayMark> BuildMarks(VisionFrameMeta meta)
        {
            if (meta == null || meta.Marks == null || meta.Marks.Length == 0)
                return null;

            var marks = new List<OverlayMark>(meta.Marks.Length);
            foreach (FrameMark mark in meta.Marks)
                marks.Add(new OverlayMark(mark.X, mark.Y, mark.Score, mark.Angle, mark.BoxW, mark.BoxH));

            return marks;
        }

        private void OnStatus(string status)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(new Action(() => { lblStatus.Text = status; }));
            }
            catch
            {
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Disconnect();
            base.OnHandleDestroyed(e);
        }
    }
}
