using System;
using System.Drawing;

namespace QMC.Common.Ui.Controls
{
    /// <summary>
    /// <see cref="CameraViewBase"/> 내장 툴바(Grab/Live)의 영상 소스 추상.
    /// 프로젝트별 카메라/모듈을 이 인터페이스로 감싸 CameraView 를 비의존(범용)으로 유지한다.
    /// </summary>
    public interface ICameraViewSource
    {
        /// <summary>단발 그랩 — 호출자가 소유(표시 후 Dispose)하는 비트맵 반환. 실패 시 null.</summary>
        Bitmap GrabFrame();

        /// <summary>라이브(연속) 그랩 지원 여부.</summary>
        bool SupportsLive { get; }

        /// <summary>라이브 시작 — 프레임마다 onFrame(소유 비트맵) 호출(백그라운드 스레드일 수 있음).
        /// CameraView 가 UI 마샬링 후 표시·Dispose 한다.</summary>
        void StartLive(Action<Bitmap> onFrame);

        /// <summary>라이브 정지.</summary>
        void StopLive();
    }
}
