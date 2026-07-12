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

            if (ColletRotationCenterCore.IsRunning(m.Name) && cmd != "COC" && cmd != "PING")
                return $"ERR|{mod}|{cmd}|COC 회전 중심 측정 중에는 다른 Vision 명령을 실행할 수 없습니다.";

            // RUN 게이트 — RUN 상태가 아니면 명령 거부. 단, PING(상태확인)과 단발 그랩(EXPOSE/GRAB)은 면제:
            // 단발 그랩은 모션을 유발하지 않는 카메라 촬상이라 셋업/수동 테스트를 위해 RUN 아닐 때도 허용한다.
            if (!IsGateExemptCommand(cmd) && isCommandAllowed != null && !isCommandAllowed())
                return $"ERR|{mod}|{cmd}|not running (press RUN)";

            try
            {
                string resp;
                switch (cmd)
                {
                    case "PING":       resp = "OK";                  break;
                    case "EXPOSE":
                    case "GRAB":       resp = VisionCommandCore.Grab(m); break;
                    case "MATCHASYNC": resp = DoMatchAsync(m, cfg, parts); break;
                    case "MATCHRESULT":resp = DoMatchResult(m, parts); break;
                    case "TRAIN":      resp = DoTrain(m, parts);     break;
                    case "SCALE":      resp = DoScale(m, parts);     break;
                    case "ROT_CENTER": resp = DoRotCenter(m);        break;
                    case "DISTORT":    resp = DoDistort(m);          break;
                    case "CAM_SWITCH": resp = DoCamSwitch(m, parts); break;
                    case "FOCUS_START":resp = VisionCommandCore.FocusStart(m, parts); break;
                    case "FOCUS_VAL":  resp = VisionCommandCore.FocusValue(m, parts); break;
                    case "FOCUS_BEST": resp = VisionCommandCore.FocusBest(m, parts); break;
                    case "COC":        resp = VisionCommandCore.ColletRotationCenter(m, parts); break;   // 콜렛 회전 중심(START/END)
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

        /// <summary>RUN 게이트 면제 명령 — PING(상태확인)과 단발 그랩(EXPOSE/GRAB, 모션 없음·수동/셋업 테스트용).</summary>
        private static bool IsGateExemptCommand(string cmd)
            => cmd == "PING" || cmd == "EXPOSE" || cmd == "GRAB" || cmd == "CAM_SWITCH"
            || cmd == "MATCHASYNC" || cmd == "MATCHRESULT"
            || cmd == "COC"
            || cmd == "FOCUS_START" || cmd == "FOCUS_VAL" || cmd == "FOCUS_BEST";   // 오토포커스=셋업/캘리브레이션, RUN 아닐 때도 허용(그랩만, 모션은 핸들러 책임)

        /// <summary>비동기 매칭 시작 — 요청 즉시 STARTED를 돌려주고 그랩/알고리즘은 백그라운드에서 수행한다.</summary>
        private static string DoMatchAsync(IVisionModule m, VisionSettings cfg, string[] parts)
        {
            string finder = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            // 신형 고정 8파트(finder|fb|collet|die_index|channel|gridx;gridy) — 결과 매칭 키 = die_index(2026-07-06).
            if (ColletAddress.TryParseWire(parts, out _, out _, out int dieIndexKey, out _, out _, out _))
                chipUid = dieIndexKey.ToString();
            if (string.IsNullOrEmpty(finder))
                return "fail:no finder";

            AsyncMatchStore.Start(m.Name, finder, chipUid);
            if (!m.Finders.TryGetValue(finder, out var f))
            {
                AsyncMatchStore.Fail(m.Name, finder, chipUid, "finder not found: " + finder);
                return "STARTED";
            }

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var g = m.GrabForTool(finder);
                    if (g == null || !g.IsSuccess)
                    {
                        try { g?.Dispose(); } catch { }
                        AsyncMatchStore.Fail(m.Name, finder, chipUid, g?.ErrorMessage ?? "grab");
                        return;
                    }

                    try
                    {
                        string res = VisionCommandCore.MatchOnImage(m, cfg, finder, f, g.Image, chipUid);
                        if (res != null && res.StartsWith("OK;"))
                            AsyncMatchStore.Complete(m.Name, finder, chipUid, res.Substring(3));
                        else
                            AsyncMatchStore.Fail(m.Name, finder, chipUid, res ?? "no result");
                    }
                    finally
                    {
                        try { g.Dispose(); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    AsyncMatchStore.Fail(m.Name, finder, chipUid, ex.Message);
                }
            });
            return "STARTED";
        }

        /// <summary>비동기 매칭 결과 폴링 — 0(진행), 1;payload(완료), ERR;reason(실패).</summary>
        private static string DoMatchResult(IVisionModule m, string[] parts)
        {
            string finder = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            if (string.IsNullOrEmpty(finder))
                return "fail:no finder";

            var st = AsyncMatchStore.TryGet(m.Name, finder, chipUid, out string payload);
            switch (st)
            {
                case AsyncMatchStore.State.Done: return "1;" + payload;
                case AsyncMatchStore.State.Error: return "ERR;" + payload;
                case AsyncMatchStore.State.Running: return "0";
                default: return "0";
            }
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
            {
                try { ModuleResultStore.Record(m.Name, "SCALE", false, "fail:" + err); } catch { }
                return "fail:" + err;
            }
            // 모듈별 CameraConfig 스케일 갱신 + 영속(SSOT=모듈)
            var map = m.ExportCameraMapping();
            map.ScaleX = sx; map.ScaleY = sy;
            m.ImportCameraMapping(map);
            try { m.SaveSettings(); } catch { }
            // 작업 모니터링 뷰에 측정 결과 표시(최근 결과 라인).
            try { ModuleResultStore.Record(m.Name, "SCALE", true, $"scaleX={sx:F6};scaleY={sy:F6}"); } catch { }
            return $"OK;scaleX={sx:F6};scaleY={sy:F6}";
        }

        private static string DoRotCenter(IVisionModule m)
        {
            if (!m.MeasureRotationalCenter(out var corners, out var err))
            {
                try { ModuleResultStore.Record(m.Name, "ROT_CENTER", false, "fail:" + err); } catch { }
                return "fail:" + err;
            }
            var sb = new StringBuilder("OK");
            var items = new StringBuilder();
            for (int i = 0; i < corners.Count; i++)
            {
                sb.Append($";x{i}={corners[i].X:F2};y{i}={corners[i].Y:F2}");
                if (items.Length > 0) items.Append(';');
                items.Append($"x{i}={corners[i].X:F2};y{i}={corners[i].Y:F2}");
            }
            // 작업 모니터링 뷰에 측정 결과 표시(최근 결과 라인).
            try { ModuleResultStore.Record(m.Name, "ROT_CENTER", true, items.ToString()); } catch { }
            return sb.ToString();
        }

        private static string DoDistort(IVisionModule m)
        {
            if (!m.LearnDistortion(out var err))
            {
                try { ModuleResultStore.Record(m.Name, "DISTORT", false, "fail:" + err); } catch { }
                return "fail:" + err;
            }
            // 작업 모니터링 뷰에 측정 결과 표시(최근 결과 라인).
            try { ModuleResultStore.Record(m.Name, "DISTORT", true, "distortion=OK"); } catch { }
            return "OK";
        }

        private static string DoCamSwitch(IVisionModule m, string[] parts)
        {
            if (parts.Length < 4) return "fail:need toolName liveOn";
            string toolName = parts[2];
            string liveOn   = parts[3];
            bool on = liveOn == "1" || liveOn.Equals("on", StringComparison.OrdinalIgnoreCase)
                                    || liveOn.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (m == null || m.Camera == null) return "fail:camera not assigned";
            // 카메라 Live 시작/정지는 수 초 블록될 수 있어 명령 스레드에서 직접 호출하지 않는다(프리즈 방지).
            // 백그라운드로 던지고 즉시 ACK. 겹침은 MIL 내부 _liveCtl 로 직렬화(실제와 동일 경로).
            var cam = m.Camera;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // Live 시작 전, 선택된 도구(없으면 모듈)의 레시피 노출+조명을 적용 — 도구 노출>0이면 그 값,
                    //   아니면 모듈(설정) 기본 노출로 폴백. 레시피 페이지 그랩/라이브와 동일한 촬상 조건 보장.
                    if (on) { try { m.PrepareToolAcquisition(toolName); } catch { } }
                    if (on) cam.StartLive(); else cam.StopLive();
                }
                catch (Exception ex)
                {
                    try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION",
                        "CamSwitch", "Live " + (on ? "start" : "stop") + " 실패: " + ex.Message); } catch { }
                }
            });
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
