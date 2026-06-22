using System;
using System.Linq;
using System.Text;
using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Modules;

namespace QMC.Vision.Comm
{
    /// <summary>
    /// 핸들러 명령 라인(<c>MODULE|CMD|args</c>) 1줄을 받아 응답 라인
    /// (<c>ACK|MODULE|CMD|result</c> / <c>ERR|MODULE|CMD|msg</c>)을 만든다.
    /// <para>
    /// TCP 서버(<see cref="VisionTcpServer"/>, 구 토폴로지)와 TCP 클라이언트
    /// (<see cref="VisionTcpClientLink"/>, 현 토폴로지=Vision 클라이언트) 가 <b>동일 구현을 공유</b>한다.
    /// 실제 명령 실행은 <see cref="VisionCommandCore"/> 로 위임 — mm 변환/로그/추적 일관.
    /// </para>
    /// </summary>
    public static class VisionCommandRouter
    {
        /// <summary>
        /// 명령 라인 1줄을 처리해 응답 라인 1줄을 반환한다.
        /// </summary>
        /// <param name="m">대상 모듈.</param>
        /// <param name="cfg">Vision 설정.</param>
        /// <param name="moduleName">이 채널이 담당하는 모듈명(요청 mod 와 일치해야 함).</param>
        /// <param name="line">수신한 요청 라인.</param>
        /// <param name="isCommandAllowed">RUN 게이트. null 이면 항상 허용. false 면 PING 외 거부.</param>
        public static string Process(IVisionModule m, VisionSettings cfg, string moduleName, string line, Func<bool> isCommandAllowed)
        {
            var parts = line.Split('|');
            string mod = parts.Length > 0 ? parts[0] : "";
            string cmd = parts.Length > 1 ? parts[1].ToUpperInvariant() : "";

            if (!string.Equals(mod, moduleName, StringComparison.OrdinalIgnoreCase) || m == null)
                return $"ERR|{mod}|{cmd}|unknown module";

            // RUN 게이트 — RUN 상태가 아니면 PING 외 명령 거부.
            if (cmd != "PING" && isCommandAllowed != null && !isCommandAllowed())
                return $"ERR|{mod}|{cmd}|not running (press RUN)";

            try
            {
                string resp;
                switch (cmd)
                {
                    case "PING":       resp = "OK";                  break;
                    case "EXPOSE":
                    case "GRAB":       resp = VisionCommandCore.Grab(m); break;
                    case "MATCH":      resp = DoMatch(m, cfg, parts); break;
                    case "INSPECT":    resp = DoInspect(m, cfg, parts); break;
                    case "TRAIN":      resp = DoTrain(m, parts);     break;
                    case "SCALE":      resp = DoScale(m, parts);     break;
                    case "ROT_CENTER": resp = DoRotCenter(m);        break;
                    case "DISTORT":    resp = DoDistort(m);          break;
                    case "CAM_SWITCH": resp = DoCamSwitch(parts);    break;
                    case "FOCUS_VAL":  resp = DoFocusVal(m);         break;
                    default:           resp = null;                  break;
                }
                if (resp == null) return $"ERR|{mod}|{cmd}|unknown command";
                return $"ACK|{mod}|{cmd}|{resp}";
            }
            catch (Exception ex)
            {
                return $"ERR|{mod}|{cmd}|{ex.Message}";
            }
        }

        // ── 명령 핸들러 (구 VisionTcpServer 와 동일) ───────────

        private static string DoMatch(IVisionModule m, VisionSettings cfg, string[] parts)
        {
            string finder  = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            return VisionCommandCore.Match(m, cfg, finder, chipUid);
        }

        private static string DoInspect(IVisionModule m, VisionSettings cfg, string[] parts)
        {
            string insp    = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            return VisionCommandCore.Inspect(m, cfg, insp, chipUid);
        }

        private static string DoTrain(IVisionModule m, string[] parts)
        {
            string finder = parts.Length > 2 ? parts[2] : "";
            return VisionCommandCore.Train(m, finder);
        }

        private static string DoScale(IVisionModule m, string[] parts)
        {
            if (parts.Length < 4) return "fail:need chipWmm chipHmm";
            if (!double.TryParse(parts[2], out var wMm)) return "fail:bad width";
            if (!double.TryParse(parts[3], out var hMm)) return "fail:bad height";
            if (!m.Calibrate(wMm, hMm, out var sx, out var sy, out var err))
                return "fail:" + err;
            // 모듈별 CameraConfig 스케일 갱신 + 영속(SSOT=모듈)
            var map = m.ExportCameraMapping();
            map.ScaleX = sx; map.ScaleY = sy;
            m.ImportCameraMapping(map);
            try { m.SaveSettings(); } catch { }
            return $"OK;scaleX={sx:F6};scaleY={sy:F6}";
        }

        private static string DoRotCenter(IVisionModule m)
        {
            if (!m.MeasureRotationalCenter(out var corners, out var err))
                return "fail:" + err;
            var sb = new StringBuilder("OK");
            for (int i = 0; i < corners.Count; i++)
                sb.Append($";x{i}={corners[i].X:F2};y{i}={corners[i].Y:F2}");
            return sb.ToString();
        }

        private static string DoDistort(IVisionModule m)
        {
            if (!m.LearnDistortion(out var err))
                return "fail:" + err;
            return "OK";
        }

        private static string DoCamSwitch(string[] parts)
        {
            if (parts.Length < 4) return "fail:need toolName liveOn";
            string toolName = parts[2];
            string liveOn   = parts[3];
            // 단일 카메라 모듈에서는 no-op.
            return $"OK;tool={toolName};live={liveOn}";
        }

        private static string DoFocusVal(IVisionModule m)
        {
            if (!m.MeasureFocus(out var rois, out var err))
                return "fail:" + err;
            return "OK;" + string.Join(";", rois.Select(p => $"{p.Key}={p.Value:F2}"));
        }
    }
}
