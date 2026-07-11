using QMC.CDT_320.Equipment.Vision;
using QMC.CDT320.VisionComm;
using QMC.Common.Ui.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    internal static class RecipeVisionPreview
    {
        public static IDisposable ShowSingle(Control parent, string title, int viewerPort)
        {
            RecipeVisionPreviewSession session = new RecipeVisionPreviewSession();
            session.ShowSingle(parent, title, viewerPort);
            return session;
        }

        public static IDisposable ShowVertical(Control parent, params RecipeVisionPreviewTile[] tiles)
        {
            RecipeVisionPreviewSession session = new RecipeVisionPreviewSession();
            session.ShowVertical(parent, tiles);
            return session;
        }
    }

    internal struct RecipeVisionPreviewTile
    {
        public RecipeVisionPreviewTile(string title, int viewerPort)
        {
            Title = title;
            ViewerPort = viewerPort;
        }

        public string Title { get; private set; }
        public int ViewerPort { get; private set; }
    }

    internal sealed class RecipeVisionPreviewSession : IDisposable
    {
        private readonly List<VisionViewerSource> _sources = new List<VisionViewerSource>();
        private readonly List<CameraViewBase> _cameras = new List<CameraViewBase>();

        public void ShowSingle(Control parent, string title, int viewerPort)
        {
            if (parent == null)
                return;

            parent.SuspendLayout();
            try
            {
                parent.Controls.Clear();
                parent.BackColor = Color.Black;
                parent.Padding = new Padding(0);
                parent.Controls.Add(CreateTile(title, viewerPort));
            }
            finally
            {
                parent.ResumeLayout(true);
            }
        }

        public void ShowVertical(Control parent, params RecipeVisionPreviewTile[] tiles)
        {
            if (parent == null || tiles == null || tiles.Length == 0)
                return;

            parent.SuspendLayout();
            try
            {
                parent.Controls.Clear();
                parent.BackColor = Color.Black;
                parent.Padding = new Padding(0);

                TableLayoutPanel layout = new TableLayoutPanel
                {
                    BackColor = Color.Black,
                    ColumnCount = 1,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    RowCount = tiles.Length
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                float rowPercent = 100F / tiles.Length;
                for (int i = 0; i < tiles.Length; i++)
                {
                    layout.RowStyles.Add(new RowStyle(SizeType.Percent, rowPercent));
                    layout.Controls.Add(CreateTile(tiles[i].Title, tiles[i].ViewerPort), 0, i);
                }

                parent.Controls.Add(layout);
            }
            finally
            {
                parent.ResumeLayout(true);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _cameras.Count; i++)
            {
                try { _cameras[i].StopLive(); } catch { }
            }

            for (int i = 0; i < _sources.Count; i++)
            {
                try { _sources[i].Dispose(); } catch { }
            }

            _cameras.Clear();
            _sources.Clear();
        }

        private Control CreateTile(string title, int viewerPort)
        {
            CameraViewBase camera = new CameraViewBase
            {
                BackColor = Color.Black,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Name = "camera" + NormalizeName(title),
                ShowToolbar = false
            };
            SetCameraInfoText(camera, BuildInfoText(title, "STAGE", 640, 480));
            AttachSource(camera, title, viewerPort);

            Panel tile = new Panel
            {
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Margin = new Padding(1),
                Padding = new Padding(0)
            };
            tile.Controls.Add(camera);
            return tile;
        }

        private void AttachSource(CameraViewBase camera, string title, int viewerPort)
        {
            if (camera == null || viewerPort <= 0)
                return;

            try
            {
                string host = string.IsNullOrWhiteSpace(VisionHub.Host) ? "127.0.0.1" : VisionHub.Host.Trim();
                VisionViewerSource source = new VisionViewerSource(host, viewerPort, 2000, null);
                source.FrameMeta += meta => OnFrameMeta(camera, title, meta);
                camera.AttachSource(source);
                camera.ShowLiveLabel = false;

                _sources.Add(source);
                _cameras.Add(camera);

                int[] pending = new int[1];
                camera.HandleCreated += (s, e) => StartGrabImageStream(source, camera, title, viewerPort, pending);
                if (camera.IsHandleCreated)
                    StartGrabImageStream(source, camera, title, viewerPort, pending);
            }
            catch
            {
            }
        }

        private void StartGrabImageStream(VisionViewerSource source, CameraViewBase camera, string title, int viewerPort, int[] pending)
        {
            if (source == null || camera == null || camera.IsDisposed)
                return;

            try
            {
                source.StartGrabImageStream(bmp => OnGrabImageFrame(camera, bmp, pending));
                LogGrabImageStarted(title, viewerPort);
            }
            catch (Exception ex)
            {
                LogGrabImageFailed(title, viewerPort, ex.Message);
            }
        }

        private void OnGrabImageFrame(CameraViewBase camera, Bitmap bmp, int[] pending)
        {
            if (bmp == null)
                return;

            if (pending == null)
            {
                try { bmp.Dispose(); } catch { }
                return;
            }

            if (Interlocked.CompareExchange(ref pending[0], 1, 0) != 0)
            {
                try { bmp.Dispose(); } catch { }
                return;
            }

            try
            {
                if (camera == null || camera.IsDisposed || !camera.IsHandleCreated)
                {
                    try { bmp.Dispose(); } catch { }
                    Interlocked.Exchange(ref pending[0], 0);
                    return;
                }

                camera.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        if (camera != null && !camera.IsDisposed)
                            camera.SetImage(bmp);
                    }
                    finally
                    {
                        try { bmp.Dispose(); } catch { }
                        Interlocked.Exchange(ref pending[0], 0);
                    }
                }));
            }
            catch
            {
                try { bmp.Dispose(); } catch { }
                Interlocked.Exchange(ref pending[0], 0);
            }
        }

        private static void LogGrabImageStarted(string title, int viewerPort)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    "VISION",
                    "VISION-GRAB-VIEW",
                    "Recipe Vision Grab 이미지 수신을 시작했습니다. title=" +
                    (title ?? string.Empty) + ", viewerPort=" + viewerPort);
            }
            catch { }
        }

        private static void LogGrabImageFailed(string title, int viewerPort, string reason)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    "VISION",
                    "VISION-GRAB-VIEW",
                    "Recipe Vision Grab 이미지 수신 시작 실패. title=" +
                    (title ?? string.Empty) + ", viewerPort=" + viewerPort +
                    ", reason=" + reason);
            }
            catch { }
        }

        private void OnFrameMeta(CameraViewBase camera, string title, VisionFrameMeta meta)
        {
            if (camera == null || camera.IsDisposed || meta == null)
                return;

            try
            {
                if (camera.InvokeRequired)
                {
                    camera.BeginInvoke((Action)(() => ApplyFrameMeta(camera, title, meta)));
                    return;
                }

                ApplyFrameMeta(camera, title, meta);
            }
            catch
            {
            }
        }

        private static void ApplyFrameMeta(CameraViewBase camera, string title, VisionFrameMeta meta)
        {
            if (camera == null || camera.IsDisposed || meta == null)
                return;

            string module = string.IsNullOrWhiteSpace(meta.Module) ? "STAGE" : meta.Module.Trim();
            SetCameraInfoText(camera, BuildInfoText(title, module, meta.Width, meta.Height));
            try { camera.SetVerdict(meta.Verdict, meta.VerdictPass); } catch { }
            try { camera.SetResultLines(meta.ResultLines); } catch { }
        }

        private static void SetCameraInfoText(CameraViewBase camera, string text)
        {
            try
            {
                if (camera != null)
                    camera.InfoText = text;
            }
            catch
            {
            }
        }

        private static string BuildInfoText(string title, string module, int width, int height)
        {
            string fixedTitle = string.IsNullOrWhiteSpace(title) ? "VISION" : title.Trim();
            string bodyTitle = string.IsNullOrWhiteSpace(module) ? "STAGE" : module.Trim();
            string size = width > 0 && height > 0 ? "W:" + width + " H:" + height : "W:640 H:480";
            return fixedTitle + "\r\n" + bodyTitle + "\r\n" + size;
        }

        private static string NormalizeName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "Vision";

            string value = text.Replace(" ", string.Empty).Replace("-", string.Empty);
            return value.Length == 0 ? "Vision" : value;
        }
    }
}
