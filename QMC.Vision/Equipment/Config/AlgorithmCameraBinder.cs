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

        /// <summary>이미 생성된 카메라에 파라미터만 갱신 (오류 메시지 반환).
        /// .mfs 전체 로드는 자동으로 하지 않는다 — 사용자가 설정 화면의 [불러오기] 버튼을 누를 때만 적용한다.</summary>
        public static bool TryApplyParameters(ICamera cam, AlgorithmCameraMapping m, out string error)
        {
            error = null;
            if (cam == null || m == null) { error = "camera or mapping is null"; return false; }
            var sb = new StringBuilder();
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
            // 일반 노드: 사용자가 지정(저장)한 값만 적용(미지정은 카메라 현재값 유지).
            // IO Output(Strobe) 그룹: 저장값이 없어도 기본값으로 항상 적용 — MVS가 LineSelector를 Line0(입력)으로
            //   초기화하므로, 적용 때마다 Line1 선택 + 스트로브 설정을 결정적으로 다시 구성한다.
            foreach (var def in CameraNodeCatalog.All)
            {
                bool isIoOutput = string.Equals(def.Group, "IO Output(Strobe)", StringComparison.OrdinalIgnoreCase);
                var v = m.GetNode(def.Node);
                if (v == null)
                {
                    if (!isIoOutput) continue;   // 비-IO 노드는 저장값 있을 때만
                    v = def.Default;             // IO Output 노드는 기본값으로라도 적용
                }
                if (string.IsNullOrEmpty(v)) continue;
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
