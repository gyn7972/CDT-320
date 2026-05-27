using System;
using QMC.Vision.Core;

namespace QMC.Vision.Config
{
    /// <summary>
    /// <see cref="AlgorithmCameraMapping"/> 한 개를 <see cref="ICamera"/> 인스턴스에 적용.
    /// Form1.Load 와 SettingsPage 의 "테스트/적용" 양쪽에서 동일 코드 사용.
    /// </summary>
    public static class AlgorithmCameraBinder
    {
        /// <summary>매핑 → 카메라 생성 + 파라미터 적용 + Open.
        /// 실패해도 호출자가 처리할 수 있도록 예외는 삼키지 않음(필요 시 try/catch 하라).</summary>
        public static ICamera CreateAndApply(AlgorithmCameraMapping m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            var cam = CameraFactory.CreateById(m.CameraId);
            try { cam.Open(); } catch { /* SDK 미설치/장치 없음 — Sim fallback 이미 적용됨 */ }
            try { ApplyParameters(cam, m); } catch { }
            return cam;
        }

        /// <summary>이미 생성된 카메라에 파라미터만 갱신.</summary>
        public static void ApplyParameters(ICamera cam, AlgorithmCameraMapping m)
        {
            if (cam == null || m == null) return;
            try { cam.ExposureUs           = m.ExposureUs; } catch { }
            try { cam.Gain                 = m.Gain;       } catch { }
            try { cam.AcquisitionFrameRate = m.FrameRate;  } catch { }
            try { cam.TriggerMode          = ParseTrigger(m.TriggerMode); } catch { }
            try { cam.PixelFormat          = ParsePixel(m.PixelFormat);   } catch { }
        }

        public static CameraTriggerMode ParseTrigger(string s)
        {
            if (string.IsNullOrEmpty(s)) return CameraTriggerMode.Software;
            CameraTriggerMode v;
            return Enum.TryParse(s, true, out v) ? v : CameraTriggerMode.Software;
        }

        public static CameraPixelFormat ParsePixel(string s)
        {
            if (string.IsNullOrEmpty(s)) return CameraPixelFormat.Mono8;
            CameraPixelFormat v;
            return Enum.TryParse(s, true, out v) ? v : CameraPixelFormat.Mono8;
        }
    }
}
