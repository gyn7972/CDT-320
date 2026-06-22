using System;
using System.Drawing;
using QMC.Common.Ui.Controls;
using QMC.Vision.Core;
using QMC.Vision.Modules;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// <see cref="IVisionModule"/> 를 범용 <see cref="ICameraViewSource"/> 로 감싸는 어댑터.
    /// CameraView 내장 툴바의 Grab/Live 가 비전 모듈에 의존하지 않도록 한다.
    /// </summary>
    internal sealed class VisionModuleSource : ICameraViewSource
    {
        private readonly IVisionModule _m;
        private Action<Bitmap> _onFrame;
        private Action<GrabResult> _handler;

        public VisionModuleSource(IVisionModule m) { _m = m; }

        /// <summary>현재 편집 중인 도구(Finder/Inspector)의 등록 id. 설정되면 툴바 Grab 이 GrabForTool 로
        /// 도구 전용 시뮬 저장이미지를 우선 사용한다. 비어있으면 모듈 Grab(카메라/모듈 저장이미지).</summary>
        public string ActiveToolId { get; set; }

        public Bitmap GrabFrame()
        {
            try
            {
                // 활성 도구의 '시뮬 저장이미지 사용' 여부 확인.
                //  - true  : 도구 전용 저장이미지 사용(GrabForTool). 경로에 이미지가 없으면 실패 → 팝업.
                //  - false : 라이브/카메라 그랩만 사용(_m.Grab()).
                var setup = string.IsNullOrEmpty(ActiveToolId)
                    ? null
                    : _m.GetAlgorithm(ActiveToolId)?.Setup as AlgoSetupBase;
                bool useSaved = setup != null && setup.SimUseSavedImage;

                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "ToolbarGrab",
                    (_m?.Name ?? "?") + ": 툴바 Grab, ActiveToolId='" + (ActiveToolId ?? "(null)") + "', 시뮬저장이미지=" + useSaved); } catch { }

                using (var g = useSaved ? _m.GrabForTool(ActiveToolId) : _m.Grab())
                {
                    if (g != null && g.IsSuccess && g.Image != null)
                        return (Bitmap)g.Image.Clone();

                    // 시뮬 저장이미지 사용인데 실패 — 대개 경로에 이미지가 없는 경우. 사용자에게 팝업으로 원인 안내.
                    if (useSaved)
                    {
                        string path = setup.SimSavedImagePath ?? "";
                        string detail = (g != null && !string.IsNullOrEmpty(g.ErrorMessage))
                            ? g.ErrorMessage
                            : ("경로: " + path);
                        try
                        {
                            QMC.Common.MessageDialog.Show(
                                "시뮬 저장이미지를 불러올 수 없습니다.\r\n" + detail,
                                "시뮬 이미지",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Warning);
                        }
                        catch { }
                    }
                    return null;
                }
            }
            catch { return null; }
        }

        public bool SupportsLive => _m?.Camera != null;

        public void StartLive(Action<Bitmap> onFrame)
        {
            var cam = _m?.Camera;
            if (cam == null) return;
            _onFrame = onFrame;
            _handler = r =>
            {
                if (r == null || !r.IsSuccess || r.Image == null) return;
                Bitmap b;
                try { b = (Bitmap)r.Image.Clone(); } catch { return; }
                _onFrame?.Invoke(b);
            };
            cam.FrameReceived += _handler;
            try { cam.TriggerMode = CameraTriggerMode.Continuous; } catch { }
            cam.StartLive();
        }

        public void StopLive()
        {
            var cam = _m?.Camera;
            if (cam == null) return;
            try { cam.StopLive(); } catch { }
            try { if (_handler != null) cam.FrameReceived -= _handler; } catch { }
            _handler = null;
        }
    }
}
