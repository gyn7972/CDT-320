using System;
using System.Threading.Tasks;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// 프로세스 전역 Vision 연결 허브. CDT-310 매뉴얼 기준 6 모듈:
    /// Wafer (5100) / Inspection (5101) / Bin (5103) / Main (5104) / TopSide (5105) / BottomSide (5106)
    /// 전역 명령(레시피 변경 등)은 Main(5104) 채널로 송신한다.
    /// </summary>
    public static class VisionHub
    {
        public static VisionTcpClient Wafer       { get; private set; }
        public static VisionTcpClient Inspection  { get; private set; }
        public static VisionTcpClient Bin         { get; private set; }
        // Stage 43 — 매뉴얼 호환을 위해 추가된 전역/측면 채널
        public static VisionTcpClient Main        { get; private set; }
        public static VisionTcpClient TopSide     { get; private set; }
        public static VisionTcpClient BottomSide  { get; private set; }

        /// <summary>마지막으로 연결한 Vision PC 호스트(IP). 뷰어 스트림 접속 등에 재사용.</summary>
        public static string Host { get; private set; }

        public static event Action ConnectionChanged;

        public static bool AllConnected =>
            Wafer      != null && Wafer.IsConnected &&
            Inspection != null && Inspection.IsConnected &&
            Bin        != null && Bin.IsConnected;

        public static bool AnyConnected =>
            (Wafer?.IsConnected      ?? false) ||
            (Inspection?.IsConnected ?? false) ||
            (Bin?.IsConnected        ?? false) ||
            (Main?.IsConnected       ?? false) ||
            (TopSide?.IsConnected    ?? false) ||
            (BottomSide?.IsConnected ?? false);

        public static async Task<bool> ConnectAllAsync(
            string host, int waferPort = 5100, int inspectionPort = 5101, int binPort = 5103,
            int mainPort = 5104, int topSidePort = 5105, int bottomSidePort = 5106)
        {
            DisconnectAll();
            Host = host;

            Wafer      = New("WaferVision",      host, waferPort);
            Inspection = New("BottomInspection", host, inspectionPort);
            Bin        = New("BinVision",        host, binPort);
            Main       = New("MainComm",         host, mainPort);
            TopSide    = New("TopSideVision",    host, topSidePort);
            BottomSide = New("BottomSideVision", host, bottomSidePort);

            // 핵심 3개만 await — 나머지 3개는 best-effort (선택 연결)
            var rs = await Task.WhenAll(
                Wafer     .ConnectAsync(),
                Inspection.ConnectAsync(),
                Bin       .ConnectAsync());

            // Stage 43 — Main/Side 등은 별도 fire-and-forget (실패해도 핵심 통신 영향 X)
            _ = Task.Run(async () =>
            {
                try { await Main.ConnectAsync();       } catch { }
                try { await TopSide.ConnectAsync();    } catch { }
                try { await BottomSide.ConnectAsync(); } catch { }
                RaiseChanged();
            });

            EventLogger.Write(EventKind.Event, "SYS", "VISION-CONN",
                $"Wafer={rs[0]} Inspection={rs[1]} Bin={rs[2]} (Main/Side: async)");

            RaiseChanged();
            return AllConnected;
        }

        public static void DisconnectAll()
        {
            try { Wafer?.Dispose();      } catch { }
            try { Inspection?.Dispose(); } catch { }
            try { Bin?.Dispose();        } catch { }
            try { Main?.Dispose();       } catch { }
            try { TopSide?.Dispose();    } catch { }
            try { BottomSide?.Dispose(); } catch { }
            Wafer = Inspection = Bin = Main = TopSide = BottomSide = null;
            RaiseChanged();
        }

        /// <summary>
        /// 활성 레시피 변경을 Vision 측에 통보한다. 전역 통신은 Main(5104) 채널로 보낸다.
        /// Main 미연결 시에는 통보를 건너뛰며, 실패해도 예외를 밖으로 던지지 않는다.
        /// </summary>
        public static async Task<bool> BroadcastRecipeAsync(int recipeNo, string recipeName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipeName))
                {
                    EventLogger.Write(EventKind.Alarm, "SYS", "VISION-RECIPE",
                        $"skip (empty name): No={recipeNo}");
                    return false;
                }

                var main = Main;
                if (main == null || !main.IsConnected)
                {
                    EventLogger.Write(EventKind.Event, "SYS", "VISION-RECIPE",
                        $"skip (Main not connected): No={recipeNo} Name={recipeName}");
                    return false;
                }

                bool ok = await main.SendRecipeAsync(recipeNo, recipeName);
                EventLogger.Write(ok ? EventKind.Event : EventKind.Alarm, "SYS", "VISION-RECIPE",
                    $"send No={recipeNo} Name={recipeName} -> {(ok ? "ACK" : "no-ack")}");
                return ok;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "SYS", "VISION-RECIPE",
                    $"send failed: No={recipeNo} Name={recipeName} / {ex.Message}");
                return false;
            }
        }

        private static VisionTcpClient New(string module, string host, int port)
        {
            var c = new VisionTcpClient(module, host, port);
            c.ConnectionChanged += _ => RaiseChanged();
            // 라인 단위 TX/RX 는 고빈도(6채널 × Vision 트래픽)라 파일 로그에 그대로 적재하면
            // 연결 후 디스크 I/O 폭주로 UI 가 느려진다. TX/RX 는 제외하고 연결/해제/오류만 기록한다.
            c.Log += s =>
            {
                if (s == null) return;
                // 모든 라인은 인메모리 통신 로그(패널 표시)로 — 파일이 아니라 저렴하다.
                VisionCommLog.Add(s);
                // TX/RX 는 고빈도라 파일(EventLogger)에는 적재하지 않는다(디스크 I/O 폭주 방지). 연결/해제/오류만 파일 기록.
                bool isTxRx = s.IndexOf("] TX: ", StringComparison.Ordinal) >= 0 ||
                              s.IndexOf("] RX: ", StringComparison.Ordinal) >= 0;
                if (!isTxRx)
                    EventLogger.Write(EventKind.Event, "SYS", "VISION-" + module, s);
            };
            return c;
        }

        private static void RaiseChanged()
        {
            try { ConnectionChanged?.Invoke(); } catch { }
        }
    }
}
