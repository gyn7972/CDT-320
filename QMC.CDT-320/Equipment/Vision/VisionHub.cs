using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Vision PC 6채널 연결 허브.
    /// Wafer(5100), BottomInspection(5101), Bin(5103), MainComm(5104),
    /// FrontSideVision(5105), RearSideVision(5106)을 관리한다.
    /// 전역 명령이나 레시피 동기화는 MainComm 채널을 사용한다.
    /// </summary>
    public static class VisionHub
    {
        private static readonly SemaphoreSlim ConnectGate = new SemaphoreSlim(1, 1);

        public static VisionTcpClient Wafer { get; private set; }
        public static VisionTcpClient Inspection { get; private set; }
        public static VisionTcpClient Bin { get; private set; }
        public static VisionTcpClient Main { get; private set; }
        public static VisionTcpClient FrontSideVision { get; private set; }
        public static VisionTcpClient RearSideVision { get; private set; }

        /// <summary>마지막으로 연결한 Vision PC Host(IP). Viewer stream 접속에도 사용한다.</summary>
        public static string Host { get; private set; }

        public static event Action ConnectionChanged;

        /// <summary>
        /// Vision PC가 RECIPEREQ를 보내면 호출된다.
        /// Form1에서 현재 활성 레시피를 BroadcastRecipeAsync로 응답하도록 연결한다.
        /// </summary>
        public static Action OnVisionRecipeRequest;

        public static bool AllConnected
        {
            get
            {
                return Wafer != null && Wafer.IsConnected &&
                       Inspection != null && Inspection.IsConnected &&
                       Bin != null && Bin.IsConnected;
            }
        }

        public static bool AnyConnected
        {
            get
            {
                return (Wafer != null && Wafer.IsConnected) ||
                       (Inspection != null && Inspection.IsConnected) ||
                       (Bin != null && Bin.IsConnected) ||
                       (Main != null && Main.IsConnected) ||
                       (FrontSideVision != null && FrontSideVision.IsConnected) ||
                       (RearSideVision != null && RearSideVision.IsConnected);
            }
        }

        public static async Task<bool> ConnectAllAsync(
            string host,
            int waferPort = 5100,
            int inspectionPort = 5101,
            int binPort = 5103,
            int mainPort = 5104,
            int frontSidePort = 5105,
            int rearSidePort = 5106)
        {
            if (IsVisionLinkBypassed())
            {
                DisconnectAll();
                Host = host;
                EventLogger.Write(EventKind.Event, "SYS", "VISION-CONN",
                    ResolveBypassReason() + " Vision PC 연결을 시도하지 않습니다.");
                return true;
            }

            await ConnectGate.WaitAsync().ConfigureAwait(false);
            try
            {
                DisconnectAll();
                Host = host;

                Wafer = New(VisionModuleNames.Wafer, host, waferPort);
                Inspection = New(VisionModuleNames.BottomInspection, host, inspectionPort);
                Bin = New(VisionModuleNames.Bin, host, binPort);
                Main = New(VisionModuleNames.Main, host, mainPort);
                if (Main != null)
                    Main.RecipeRequested += () => { try { OnVisionRecipeRequest?.Invoke(); } catch { } };
                FrontSideVision = New(VisionModuleNames.FrontSide, host, frontSidePort);
                RearSideVision = New(VisionModuleNames.RearSide, host, rearSidePort);

                bool[] connected = await Task.WhenAll(
                    Wafer.ConnectAsync(),
                    Inspection.ConnectAsync(),
                    Bin.ConnectAsync(),
                    Main.ConnectAsync(),
                    FrontSideVision.ConnectAsync(),
                    RearSideVision.ConnectAsync()).ConfigureAwait(false);

                EventLogger.Write(EventKind.Event, "SYS", "VISION-CONN",
                    "Wafer=" + connected[0] +
                    " Inspection=" + connected[1] +
                    " Bin=" + connected[2] +
                    " MainComm=" + connected[3] +
                    " FrontSideVision=" + connected[4] +
                    " RearSideVision=" + connected[5]);

                RaiseChanged();
                return AllConnected;
            }
            finally
            {
                ConnectGate.Release();
            }
        }

        public static void DisconnectAll()
        {
            try { Wafer?.Dispose(); } catch { }
            try { Inspection?.Dispose(); } catch { }
            try { Bin?.Dispose(); } catch { }
            try { Main?.Dispose(); } catch { }
            try { FrontSideVision?.Dispose(); } catch { }
            try { RearSideVision?.Dispose(); } catch { }
            Wafer = null;
            Inspection = null;
            Bin = null;
            Main = null;
            FrontSideVision = null;
            RearSideVision = null;
            RaiseChanged();
        }

        /// <summary>
        /// 현재 활성 레시피를 Vision MainComm 채널로 전송한다.
        /// Vision 연결 직후 또는 RECIPEREQ 수신 시 호출된다.
        /// </summary>
        public static async Task<bool> BroadcastRecipeAsync(int recipeNo, string recipeName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipeName))
                {
                    EventLogger.Write(EventKind.Alarm, "SYS", "VISION-RECIPE",
                        "레시피 이름이 없어 Vision 레시피 전송을 생략합니다. no=" + recipeNo);
                    return false;
                }

                VisionTcpClient main = Main;
                if (main == null || !main.IsConnected)
                {
                    EventLogger.Write(EventKind.Event, "SYS", "VISION-RECIPE",
                        "MainComm이 연결되지 않아 Vision 레시피 전송을 생략합니다. no=" + recipeNo + ", name=" + recipeName);
                    return false;
                }

                bool ok = await main.SendRecipeAsync(recipeNo, recipeName).ConfigureAwait(false);
                EventLogger.Write(ok ? EventKind.Event : EventKind.Alarm, "SYS", "VISION-RECIPE",
                    "Vision 레시피 전송. no=" + recipeNo + ", name=" + recipeName + ", result=" + (ok ? "ACK" : "NO-ACK"));
                return ok;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "SYS", "VISION-RECIPE",
                    "Vision 레시피 전송 실패. no=" + recipeNo + ", name=" + recipeName + ", error=" + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private static VisionTcpClient New(string module, string host, int port)
        {
            var client = new VisionTcpClient(module, host, port);
            client.ConnectionChanged += _ => RaiseChanged();
            client.Log += line =>
            {
                if (line == null)
                    return;

                // TX/RX 원문은 메모리 링버퍼에만 보관한다. 파일 로그에 계속 적으면 Auto 중 I/O 부하가 커진다.
                VisionCommLog.Add(line);

                bool isTxRx = line.IndexOf("] TX: ", StringComparison.Ordinal) >= 0 ||
                              line.IndexOf("] RX: ", StringComparison.Ordinal) >= 0;
                if (!isTxRx)
                    EventLogger.Write(EventKind.Event, "SYS", "VISION-" + module, line);
            };
            return client;
        }

        private static void RaiseChanged()
        {
            try { ConnectionChanged?.Invoke(); } catch { }
        }

        private static bool IsVisionLinkBypassed()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && !settings.UseVision;
        }

        private static string ResolveBypassReason()
        {
            AppSettings settings = AppSettingsStore.Current;
            if (settings != null && !settings.UseVision)
                return "\uBE44\uC804 \uBBF8\uC0AC\uC6A9 \uC124\uC815\uC774\uB77C";

            return "Vision \uC5F0\uACB0 \uBC14\uC774\uD328\uC2A4 \uC124\uC815\uC774\uB77C";
        }
    }
}
