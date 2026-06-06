using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using QMC.Vision.Cameras.Hik;
using QMC.Vision.Cameras.Sim;
using QMC.Vision.Core;

namespace QMC.Vision.Tools
{
    /// <summary>
    /// 헤드리스 CLI 카메라 연결 + 1장 그랩 테스트.
    /// 사용:  <c>QMC.Vision.exe --cam-test [--save &lt;dir&gt;]</c>
    /// <para>
    /// 종료 코드:
    ///   0 = PASS<br/>
    ///   2 = SDK 미로드 (HikMvsDll.IsLoaded=false)<br/>
    ///   3 = NO_DEVICE (Enumerate=0)<br/>
    ///   4 = SIM_FALLBACK (실카메라 테스트인데 SimCamera 로 떨어짐)<br/>
    ///   5 = Open 실패<br/>
    ///   6 = Grab 실패 또는 이미지 검증 실패
    /// </para>
    /// </summary>
    public static class CameraConnectTest
    {
        /// <summary>외부 진입점 — Program.Main 이 args 파싱 후 호출.</summary>
        /// <param name="saveDir">PNG 저장 디렉토리. null/empty 면 bin\Debug\Log\CamTest\</param>
        /// <returns>process exit code</returns>
        public static int Run(string saveDir)
        {
            string defaultDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "CamTest");
            string outDir = string.IsNullOrEmpty(saveDir) ? defaultDir : saveDir;
            try { Directory.CreateDirectory(outDir); } catch { /* 권한 문제는 저장 단계에서 다시 핸들링 */ }

            // ── 1. HikMvsDll 로드 확인 ──
            if (!HikMvsDll.IsLoaded)
            {
                Console.Out.WriteLine("SDK: NOT_LOADED");
                Console.Out.WriteLine("HINT: " + HikMvsDll.GetInstallHint().Replace("\n", " | "));
                return 2;
            }
            Console.Out.WriteLine("SDK: LOADED");
            Console.Out.WriteLine("VERSION: " + HikMvsDll.Version);

            // ── 2. Enumerate ──
            var devices = HikGigECamera.Enumerate();
            Console.Out.WriteLine("DEVICES: " + devices.Count);
            if (devices.Count == 0)
            {
                Console.Out.WriteLine("NO_DEVICE: GigE 카메라 미발견 (결선/IP/방화벽 확인)");
                return 3;
            }
            for (int i = 0; i < devices.Count; i++)
            {
                var d = devices[i];
                Console.Out.WriteLine($"DEVICE[{i}]: Id={d.Id} IP={d.IpAddress} Vendor={d.Vendor} Model={d.Model} SN={d.SerialNumber}");
            }

            // ── 3. 첫 번째 장치로 CreateById + Sim fallback 가드 ──
            var info = devices[0];
            ICamera cam = null;
            try
            {
                cam = CameraFactory.CreateById(info.Id);
                Console.Out.WriteLine("CREATE_TYPE: " + cam.GetType().Name);
                if (cam is SimCamera)
                {
                    Console.Out.WriteLine("SIM_FALLBACK: 실카메라 연결 실패 — CameraFactory 가 Sim 으로 떨어짐 (IP 불일치 / SDK 매칭 실패)");
                    try { cam.Dispose(); } catch { }
                    return 4;
                }

                // ── 4. Open ──
                try
                {
                    cam.Open();
                    Console.Out.WriteLine("OPEN: OK");
                }
                catch (Exception openEx)
                {
                    Console.Out.WriteLine("OPEN: FAIL — " + openEx.Message);
                    Console.Error.WriteLine(openEx.ToString());
                    try { cam.Dispose(); } catch { }
                    return 5;
                }

                // ── 5. Grab ──
                GrabResult grab = null;
                try
                {
                    grab = cam.Grab(3000);
                }
                catch (Exception grabEx)
                {
                    Console.Out.WriteLine("GRAB: EXCEPTION — " + grabEx.Message);
                    Console.Error.WriteLine(grabEx.ToString());
                    try { cam.Close(); cam.Dispose(); } catch { }
                    return 6;
                }

                // ── 6. 검증 ──
                if (grab == null || !grab.IsSuccess)
                {
                    Console.Out.WriteLine("GRAB: FAIL — " + (grab?.ErrorMessage ?? "null grab"));
                    try { grab?.Dispose(); cam.Close(); cam.Dispose(); } catch { }
                    return 6;
                }
                if (grab.Width <= 0 || grab.Height <= 0 || grab.Image == null)
                {
                    Console.Out.WriteLine($"GRAB: BAD_SIZE — W={grab.Width} H={grab.Height} ImageNull={(grab.Image == null)}");
                    try { grab.Dispose(); cam.Close(); cam.Dispose(); } catch { }
                    return 6;
                }
                Console.Out.WriteLine($"GRAB: OK W={grab.Width} H={grab.Height}");

                // 간단 무결성 — 픽셀 min/max 차이가 0 이면 WARN (FAIL 아님)
                string integrity = CheckIntegrity(grab.Image);
                Console.Out.WriteLine("INTEGRITY: " + integrity);

                // ── 7. PNG 저장 ──
                string fileName = $"camtest_{Sanitize(info.Id)}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string fullPath = Path.Combine(outDir, fileName);
                try
                {
                    using (var clone = new Bitmap(grab.Image))
                        clone.Save(fullPath, ImageFormat.Png);
                    Console.Out.WriteLine("SAVE: " + fullPath);
                }
                catch (Exception saveEx)
                {
                    Console.Out.WriteLine("SAVE: FAIL — " + saveEx.Message);
                    try { grab.Dispose(); cam.Close(); cam.Dispose(); } catch { }
                    return 6;
                }

                // ── 8. cleanup ──
                try { grab.Dispose(); } catch { }
                try { cam.Close(); } catch { }
                try { cam.Dispose(); } catch { }

                Console.Out.WriteLine($"PASS id={info.Id} ip={info.IpAddress} WxH={grab.Width}x{grab.Height} saved={fullPath}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("ERR: 예기치 못한 예외 — " + ex.Message);
                Console.Error.WriteLine(ex.ToString());
                try { cam?.Dispose(); } catch { }
                return 99;
            }
        }

        /// <summary>이미지 픽셀 min/max 가 동일하면 WARN, 그 외 OK. 표본 추출 100점.</summary>
        private static string CheckIntegrity(Bitmap bmp)
        {
            try
            {
                int w = bmp.Width, h = bmp.Height;
                int min = 255, max = 0;
                var rnd = new Random(0);
                for (int i = 0; i < 100; i++)
                {
                    int x = rnd.Next(w), y = rnd.Next(h);
                    var c = bmp.GetPixel(x, y);
                    int g = (c.R + c.G + c.B) / 3;
                    if (g < min) min = g;
                    if (g > max) max = g;
                }
                if (max - min < 2) return "WARN_UNIFORM min=" + min + " max=" + max + " (렌즈 캡/암실 가능)";
                return $"OK min={min} max={max} (range={max - min})";
            }
            catch (Exception ex) { return "WARN_PROBE_FAIL " + ex.Message; }
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "_";
            foreach (var ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_');
            return s;
        }
    }
}
