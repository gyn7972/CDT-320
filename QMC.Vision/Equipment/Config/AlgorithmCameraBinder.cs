using System;
using System.Text;
using QMC.Vision.Core;

using QMC.Common.Recipes;
namespace QMC.Vision.Config
{
    /// <summary>
    /// <see cref="AlgorithmCameraMapping"/> 한 개를 <see cref="ICamera"/> 인스턴스에 적용.
    /// Form1.Load 와 SettingsPage 의 "테스트/적용" 양쪽에서 동일 코드 사용.
    /// </summary>
    public static class AlgorithmCameraBinder
    {
        /// <summary>매핑 → 카메라 생성 + 파라미터 적용 + Open.
        /// Open/Apply 단계 실패는 호출자가 처리할 수 있도록 throw 하지 않음.
        /// <paramref name="openError"/> / <paramref name="applyError"/> 가 채워지면 알람 발생 권장.</summary>
        public static ICamera CreateAndApply(AlgorithmCameraMapping m,
                                             out string openError, out string applyError)
        {
            openError = null;
            applyError = null;
            if (m == null) throw new ArgumentNullException(nameof(m));
            var cam = CameraFactory.CreateById(m.CameraId);
            try { cam.Open(); } catch (Exception ex) { openError = ex.Message; }
            TryApplyParameters(cam, m, out applyError);
            return cam;
        }

        /// <summary>오버로드 — error out 변수 받지 않는 호출자용 (편의).</summary>
        public static ICamera CreateAndApply(AlgorithmCameraMapping m)
            => CreateAndApply(m, out _, out _);

        /// <summary>이미 생성된 카메라에 파라미터만 갱신 (오류 메시지 반환).</summary>
        public static bool TryApplyParameters(ICamera cam, AlgorithmCameraMapping m, out string error)
        {
            error = null;
            if (cam == null || m == null) { error = "camera or mapping is null"; return false; }
            var sb = new StringBuilder();
            // .mfs 베이스 적용(경로 지정 시) — 카메라 전체 노드값을 먼저 깔고, 아래 관리 파라미터가 그 위에 덮어쓴다.
            if (!string.IsNullOrWhiteSpace(m.MvsFeatureFilePath) && System.IO.File.Exists(m.MvsFeatureFilePath))
            {
                if (!cam.LoadFeatures(m.MvsFeatureFilePath, out var mfsErr))
                    sb.Append("mfs:" + mfsErr + "; ");
            }
            try { cam.ExposureUs           = m.ExposureUs; } catch (Exception ex) { sb.Append("Exposure:" + ex.Message + "; "); }
            try { cam.Gain                 = m.Gain;       } catch (Exception ex) { sb.Append("Gain:"     + ex.Message + "; "); }
            try { cam.AcquisitionFrameRate = m.FrameRate;  } catch (Exception ex) { sb.Append("FPS:"      + ex.Message + "; "); }
            try { cam.TriggerMode          = ParseTrigger(m.TriggerMode); } catch (Exception ex) { sb.Append("Trigger:" + ex.Message + "; "); }
            try { cam.PixelFormat          = ParsePixel(m.PixelFormat);   } catch (Exception ex) { sb.Append("Pixel:"   + ex.Message + "; "); }
            if (!m.IsRoiFull)
            {
                try { cam.Roi = m.ToRectangle(); } catch (Exception ex) { sb.Append("ROI:" + ex.Message + "; "); }
            }
            // 제네릭 노드 파라미터 — 카탈로그 순서대로(LineSelector 등 selector 가 먼저) 적용.
            // 사용자가 설정에서 지정(저장)한 노드만 적용하고, 미지정 노드는 카메라 현재값을 유지한다.
            foreach (var def in CameraNodeCatalog.All)
            {
                var v = m.GetNode(def.Node);
                if (v == null) continue;
                try { cam.SetParameterTyped(def.Node, def.Kind, v); }
                catch (Exception ex) { sb.Append(def.Node + ":" + ex.Message + "; "); }
            }
            if (sb.Length > 0) { error = sb.ToString().TrimEnd(' ', ';'); return false; }
            return true;
        }

        /// <summary>레거시 시그니처 — 오류 무시.</summary>
        public static void ApplyParameters(ICamera cam, AlgorithmCameraMapping m)
            => TryApplyParameters(cam, m, out _);

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
