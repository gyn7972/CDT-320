using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
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
        private int _activePort;
        private bool _useSavedPixelScale;
        private double _savedPixelScaleX;
        private double _savedPixelScaleY;
        private double _savedWidthPixel;
        private double _savedHeightPixel;

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
                    new ModulePort(VisionModuleNames.Wafer, VisionViewerPorts.DefaultWafer),
                    new ModulePort(VisionModuleNames.BottomInspection, VisionViewerPorts.DefaultBottomInspection),
                    new ModulePort(VisionModuleNames.Bin, VisionViewerPorts.DefaultBin),
                    new ModulePort(VisionModuleNames.FrontSide, VisionViewerPorts.DefaultFrontSideVision),
                    new ModulePort(VisionModuleNames.RearSide, VisionViewerPorts.DefaultRearSideVision),
                };
            }

            return new[]
            {
                new ModulePort(VisionModuleNames.Wafer, VisionViewerPorts.Wafer),
                new ModulePort(VisionModuleNames.BottomInspection, VisionViewerPorts.BottomInspection),
                new ModulePort(VisionModuleNames.Bin, VisionViewerPorts.Bin),
                new ModulePort(VisionModuleNames.FrontSide, VisionViewerPorts.FrontSideVision),
                new ModulePort(VisionModuleNames.RearSide, VisionViewerPorts.RearSideVision),
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

            _activePort = selected.Port;
            RefreshSavedPixelScale();

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
                    // 핸들러 저장 스케일이 있으면 표시 해상도(meta.Width/Height)로 정규화해 사용(Vision 측정값과 일치),
                    // 없으면 Vision 이 보낸 meta.ScaleX(이미 다운스케일 보정됨)를 사용한다.
                    if (_useSavedPixelScale)
                    {
                        double scaleFactor = AppSettingsStore.Current != null ? AppSettingsStore.Current.ViewerMeasureScaleFactor : 1.0;
                        cameraView.MmPerPixelX = EffectiveSavedScale(_savedPixelScaleX, _savedWidthPixel, meta.Width, scaleFactor);
                        cameraView.MmPerPixelY = EffectiveSavedScale(_savedPixelScaleY, _savedHeightPixel, meta.Height, scaleFactor);
                    }
                    else
                    {
                        cameraView.MmPerPixelX = meta.ScaleX;
                        cameraView.MmPerPixelY = meta.ScaleY;
                    }
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

        /// <summary>선택 모듈 포트에 해당하는 핸들러 저장 픽셀 캘리브레이션(mm/px)을 읽어 캐시.
        /// 없거나 유효하지 않으면 저장 스케일 미사용(=meta.ScaleX 폴백).</summary>
        private void RefreshSavedPixelScale()
        {
            try
            {
                VisionCameraPixelCalibration camera = ResolveSavedPixelCalibration();
                if (camera == null || camera.PixelToMmX == 0 || camera.PixelToMmY == 0)
                {
                    _useSavedPixelScale = false;
                    return;
                }

                _savedPixelScaleX = camera.PixelToMmX;
                _savedPixelScaleY = camera.PixelToMmY;
                _savedWidthPixel = camera.ImageWidthPixel;
                _savedHeightPixel = camera.ImageHeightPixel;
                _useSavedPixelScale = true;
            }
            catch
            {
                _useSavedPixelScale = false;
            }
        }

        /// <summary>표시용 mm/px 산출 — 저장 스케일 × factor(다운스케일 계수).
        /// factor>0: 수동 계수(표시 mm/px = savedMmPerPx × factor, 예 5120→1600 이면 3.2).
        /// factor≤0: 자동 폴백(표시폭 displayPixel 기준 = savedMmPerPx × savedPixel / displayPixel). 정보 부족 시 저장값 그대로.</summary>
        private static double EffectiveSavedScale(double savedMmPerPx, double savedPixel, double displayPixel, double factor)
        {
            if (savedMmPerPx <= 0) return 0.0;
            if (factor > 0) return savedMmPerPx * factor;
            if (savedPixel > 0 && displayPixel > 0)
                return savedMmPerPx * savedPixel / displayPixel;
            return savedMmPerPx;
        }

        private VisionCameraPixelCalibration ResolveSavedPixelCalibration()
        {
            Form1 host = FindHostForm();
            if (host == null ||
                host.Machine == null ||
                host.Machine.VisionUnit == null ||
                host.Machine.VisionUnit.Config == null ||
                host.Machine.VisionUnit.Config.CalibrationData == null)
                return null;

            VisionCameraCalibrationData data = host.Machine.VisionUnit.Config.CalibrationData.Camera;
            if (data == null) return null;
            data.EnsureObjects();

            if (_activePort == VisionViewerPorts.BottomInspection) return data.BottomCamera;
            if (_activePort == VisionViewerPorts.Wafer) return data.InputCamera;
            if (_activePort == VisionViewerPorts.Bin) return data.OutputCamera;
            if (_activePort == VisionViewerPorts.FrontSideVision) return data.FrontSideCamera;
            if (_activePort == VisionViewerPorts.RearSideVision) return data.RearSideCamera;
            return null;
        }

        private Form1 FindHostForm()
        {
            Control current = this;
            while (current != null)
            {
                Form1 host = current as Form1;
                if (host != null) return host;
                current = current.Parent;
            }

            foreach (Form form in Application.OpenForms)
            {
                Form1 host = form as Form1;
                if (host != null) return host;
            }
            return null;
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
