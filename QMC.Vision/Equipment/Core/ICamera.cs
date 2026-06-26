using System;
using System.Drawing;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 카메라 추상화. 모든 비즈니스 코드는 이 인터페이스 하나로 카메라를 제어한다.
    /// 구현: SimCamera / HikGigECamera / (향후) HikUsb3Camera / BaslerCamera / CognexCamera 등.
    /// </summary>
    public interface ICamera : IDisposable
    {
        /// <summary>장치 정보 (enum 결과 + Open 후 상세).</summary>
        CameraInfo Info { get; }

        // ─── 상태 ───────────────────────────────────
        bool IsOpen     { get; }
        bool IsGrabbing { get; }

        // ─── 이벤트 ─────────────────────────────────
        /// <summary>Live / HW Trigger 등으로 새 프레임이 도착했을 때.</summary>
        event Action<GrabResult>          FrameReceived;
        /// <summary>연결 유실 / 재연결 등 상태 이벤트.</summary>
        event Action<CameraConnectionEvent> ConnectionChanged;
        /// <summary>센서 노출 종료 시점 (HW ExposureEnd 이벤트). 지원 카메라만 발화.
        /// 전송 완료(FrameReceived)보다 앞서 도착하므로, 이 신호로 다음 기구 동작을 앞당길 수 있다.</summary>
        event Action ExposureEnded;

        // ─── 라이프사이클 ───────────────────────────
        void Open();
        void Close();

        // ─── Grab ───────────────────────────────────
        /// <summary>단발 촬영 — 트리거 모드가 Continuous 이면 최신 프레임 반환.</summary>
        GrabResult Grab(int timeoutMs = 3000);

        /// <summary>연속 촬영 시작. 프레임마다 <see cref="FrameReceived"/> 발행.</summary>
        void StartLive();

        /// <summary>연속 촬영 중지.</summary>
        void StopLive();

        /// <summary>소프트웨어 트리거 발사 (<see cref="CameraTriggerMode.Software"/> 일 때).</summary>
        void TriggerSoftware();

        // ─── 파라미터 ───────────────────────────────
        double ExposureUs            { get; set; }
        double Gain                  { get; set; }
        double AcquisitionFrameRate  { get; set; }
        CameraTriggerMode  TriggerMode  { get; set; }
        CameraPixelFormat  PixelFormat  { get; set; }

        /// <summary>AOI (Area of Interest) — Width/Height/OffsetX/OffsetY.</summary>
        Rectangle Roi { get; set; }

        /// <summary>현재 AOI 해상도.</summary>
        Size Resolution { get; }

        // ─── 디버그 ────────────────────────────────
        /// <summary>백엔드별 원시 파라미터 접근 (GenICam 노드명 등).</summary>
        string GetRawParameter(string key);
        void   SetRawParameter(string key, string value);

        // ─── 제네릭 노드 파라미터 ─────────────────────
        /// <summary>타입을 명시해 임의 GenICam 노드를 적용한다(노드 카탈로그 기반 설정 적용용).
        /// 미지원 카메라/노드는 조용히 무시(로그 후)된다.</summary>
        void SetParameterTyped(string node, CameraParamKind kind, string value);

        // ─── MVS Feature(.mfs) 일괄 적용/저장 ─────────
        /// <summary>MVS Feature Save 파일(.mfs)의 전체 노드값을 카메라에 일괄 적용. 성공 true.
        /// 미지원 백엔드는 false + error 반환.</summary>
        bool LoadFeatures(string filePath, out string error);
        /// <summary>현재 카메라의 전체 노드값을 MVS Feature Save 파일(.mfs)로 저장. 성공 true.</summary>
        bool SaveFeatures(string filePath, out string error);
    }

    public enum CameraConnectionEvent { Opened, Closed, Lost, Reconnected, Error }

    /// <summary>제네릭 카메라 노드 값 타입 — GenICam 노드 set 시 호출 메서드(Float/Int/Enum/Bool/Command) 결정.</summary>
    public enum CameraParamKind { Float, Int, Enum, Bool, Command }
}
