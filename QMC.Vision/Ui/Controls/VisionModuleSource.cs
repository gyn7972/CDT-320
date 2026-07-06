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

        // 생성 시(UI 스레드) SynchronizationContext 캡처 — GrabFrame 이 워커 스레드에서 호출돼도
        // 사용자 안내 팝업은 UI 스레드에서 뜨도록 마샬링한다(CameraViewBase 툴바 Grab 비동기화 대응).
        private readonly System.Threading.SynchronizationContext _uiCtx;

        public VisionModuleSource(IVisionModule m)
        {
            _m = m;
            _uiCtx = System.Threading.SynchronizationContext.Current;
        }

        /// <summary>현재 편집 중인 도구(Finder/Inspector)의 등록 id. 설정되면 툴바 Grab 이 GrabForTool 로
        /// 도구 전용 시뮬 저장이미지를 우선 사용한다. 비어있으면 모듈 Grab(카메라/모듈 저장이미지).</summary>
        public string ActiveToolId { get; set; }

        public Bitmap GrabFrame()
        {
            try
            {
                // 활성 도구의 '시뮬 저장이미지 사용' 여부 확인 — 카메라가 시뮬레이션일 때만 유효.
                //  - true  : 도구 전용 저장이미지 사용(GrabForTool). 경로에 이미지가 없으면 실패 → 팝업.
                //  - false : 라이브/카메라 그랩만 사용(실카메라는 항상 실제 촬상).
                var setup = string.IsNullOrEmpty(ActiveToolId)
                    ? null
                    : _m.GetAlgorithm(ActiveToolId)?.Setup as AlgoSetupBase;
                bool useSaved = setup != null && setup.SimUseSavedImage && _m.IsSimCameraMode;

                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "ToolbarGrab",
                    (_m?.Name ?? "?") + ": 툴바 Grab, ActiveToolId='" + (ActiveToolId ?? "(null)") + "', 시뮬저장이미지=" + useSaved); } catch { }

                // 활성 도구가 있으면 GrabForTool 경유 — 도구 전용 노출/조명(PrepareToolAcquisition)이 적용된다.
                //   (저장이미지 우선 순위는 GrabForTool 내부에서 동일하게 처리.)
                using (var g = !string.IsNullOrEmpty(ActiveToolId) ? _m.GrabForTool(ActiveToolId) : _m.Grab())
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
                        ShowWarningOnUi("시뮬 저장이미지를 불러올 수 없습니다.\r\n" + detail, "시뮬 이미지");
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[VisionModuleSource] GrabFrame 실패: " + ex.Message);
                return null;
            }
        }

        /// <summary>경고 팝업을 UI 스레드에서 표시 — 워커 스레드 호출 대응(컨텍스트 없으면 직접 표시).</summary>
        private void ShowWarningOnUi(string message, string title)
        {
            try
            {
                System.Threading.SendOrPostCallback show = _ =>
                {
                    try
                    {
                        QMC.Common.MessageDialog.Show(message, title,
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Warning);
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[VisionModuleSource] 팝업 표시 실패: " + ex.Message); }
                };
                if (_uiCtx != null) _uiCtx.Post(show, null);
                else show(null);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[VisionModuleSource] 팝업 마샬링 실패: " + ex.Message); }
        }

        public bool SupportsLive => _m?.Camera != null;

        public void StartLive(Action<Bitmap> onFrame)
        {
            var cam = _m?.Camera;
            if (cam == null) return;
            // 라이브 시작 전 촬상 준비 — 활성 도구(없으면 모듈 기본)의 노출 + 조명 적용.
            //   조명 컨트롤러는 동일 값이면(캐시 히트) 통신/안정화 대기를 생략한다.
            try { _m.PrepareToolAcquisition(ActiveToolId); } catch { }
            _onFrame = onFrame;
            _handler = r =>
            {
                if (r == null || !r.IsSuccess || r.Image == null) return;
                Bitmap b;
                try { b = (Bitmap)r.Image.Clone(); } catch { return; }
                _onFrame?.Invoke(b);
            };
            cam.FrameReceived += _handler;
            // MIL 카메라는 TriggerMode 세터를 호출하지 않는다 — StartLive 내부(EnsureContinuousLiveMode)가
            // 모드를 관리하며, 외부 트리거 쓰기는 VNP FrameRate 재계산 부작용만 유발(QMC.MilCameraTest 와 동일 경로).
            if (!(cam is QMC.Vision.Cameras.Mil.MilCamera))
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
