using System;
using System.Threading.Tasks;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// ?꾨줈?몄뒪 ?꾩뿭 Vision ?곌껐 ?덈툕. CDT-310 留ㅻ돱???ъ뼇 ??6 紐⑤뱢:
    /// Wafer (5100) / Inspection (5101) / Bin (5103) / Main (5104) / TopSide (5105) / BottomSide (5106)
    /// </summary>
    public static class VisionHub
    {
        public static VisionTcpClient Wafer       { get; private set; }
        public static VisionTcpClient Inspection  { get; private set; }
        public static VisionTcpClient Bin         { get; private set; }
        // Stage 43 ??留ㅻ돱???명솚 異붽? 梨꾨꼸
        public static VisionTcpClient Main        { get; private set; }
        public static VisionTcpClient TopSide     { get; private set; }
        public static VisionTcpClient BottomSide  { get; private set; }

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

            Wafer      = New("WaferVision",      host, waferPort);
            Inspection = New("BottomInspection", host, inspectionPort);
            Bin        = New("BinVision",        host, binPort);
            Main       = New("MainComm",         host, mainPort);
            TopSide    = New("TopSideVision",    host, topSidePort);
            BottomSide = New("BottomSideVision", host, bottomSidePort);

            // ?듭떖 3媛쒕쭔 await ???섎㉧吏 3媛쒕뒗 best-effort (?좏깮 ?곌껐)
            var rs = await Task.WhenAll(
                Wafer     .ConnectAsync(),
                Inspection.ConnectAsync(),
                Bin       .ConnectAsync());

            // Stage 43 ??Main/Side ??蹂꾨룄 fire-and-forget (?ㅽ뙣?대룄 ?듭떖 ?듭떊 ?곹뼢 X)
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

