using System;
using System.Threading.Tasks;
using QMC.CDT320.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// 프로세스 전역 Vision 연결 허브. CDT-310 매뉴얼 사양 — 6 모듈:
    /// Wafer (5100) / Inspection (5101) / Bin (5103) / Main (5104) / FrontSide (5105) / RearSide (5106)
    /// </summary>
    public static class VisionHub
    {
        public static VisionTcpClient Wafer       { get; private set; }
        public static VisionTcpClient Inspection  { get; private set; }
        public static VisionTcpClient Bin         { get; private set; }
        // Stage 43 — 매뉴얼 호환 추가 채널
        public static VisionTcpClient Main        { get; private set; }
        public static VisionTcpClient FrontSide     { get; private set; }
        public static VisionTcpClient RearSide  { get; private set; }

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
            (FrontSide?.IsConnected    ?? false) ||
            (RearSide?.IsConnected ?? false);

        public static async Task<bool> ConnectAllAsync(
            string host, int waferPort = 5100, int inspectionPort = 5101, int binPort = 5103,
            int mainPort = 5104, int frontSidePort = 5105, int rearSidePort = 5106)
        {
            DisconnectAll();

            Wafer      = New("WaferVision",      host, waferPort);
            Inspection = New("BottomInspection", host, inspectionPort);
            Bin        = New("BinVision",        host, binPort);
            Main       = New("MainComm",         host, mainPort);
            FrontSide    = New("FrontSideVision",    host, frontSidePort);
            RearSide = New("RearSideVision", host, rearSidePort);

            // 핵심 3개만 await — 나머지 3개는 best-effort (선택 연결)
            var rs = await Task.WhenAll(
                Wafer     .ConnectAsync(),
                Inspection.ConnectAsync(),
                Bin       .ConnectAsync());

            // Stage 43 — Main/Side 는 별도 fire-and-forget (실패해도 핵심 통신 영향 X)
            _ = Task.Run(async () =>
            {
                try { await Main.ConnectAsync();       } catch { }
                try { await FrontSide.ConnectAsync();    } catch { }
                try { await RearSide.ConnectAsync(); } catch { }
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
            try { FrontSide?.Dispose();    } catch { }
            try { RearSide?.Dispose(); } catch { }
            Wafer = Inspection = Bin = Main = FrontSide = RearSide = null;
            RaiseChanged();
        }

        private static VisionTcpClient New(string module, string host, int port)
        {
            var c = new VisionTcpClient(module, host, port);
            c.ConnectionChanged += _ => RaiseChanged();
            c.Log += s => EventLogger.Write(EventKind.Event, "SYS", "VISION-" + module, s);
            return c;
        }

        private static void RaiseChanged()
        {
            try { ConnectionChanged?.Invoke(); } catch { }
        }
    }
}
