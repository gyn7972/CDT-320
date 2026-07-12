using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Vision.Inspector;

namespace QMC.BottomInspectTest
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            // 헤드리스 배치 모드: BottomInspectTest.exe --auto <폴더> [쓰레드수] [리포트경로]
            // 폴더의 전체 이미지를 병렬 검사하고 이미지별 소요시간(ms)을 CSV 리포트로 남긴다.
            if (args != null && args.Length >= 2 && string.Equals(args[0], "--auto", StringComparison.OrdinalIgnoreCase))
            {
                try { return RunAuto(args); }
                catch (Exception ex)
                {
                    try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "BottomInspectTest_error.txt"), ex.ToString()); } catch { }
                    return 1;
                }
            }

            // 업스케일 정수화 등가성 검증: 다양한 크기/난수 입력에서 double 참조 구현과 바이트 완전 비교.
            if (args != null && args.Length >= 1 && string.Equals(args[0], "--upcheck", StringComparison.OrdinalIgnoreCase))
            {
                try { return RunUpscaleCheck(); }
                catch (Exception ex) { Console.WriteLine("upcheck error: " + ex); return 1; }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }

        /// <summary>Upscale2xBilinear(double 참조) vs Upscale2xBilinearFast(정수) 바이트 동등성 검증.</summary>
        private static int RunUpscaleCheck()
        {
            int[] sizes = { 1, 2, 3, 5, 8, 16, 33, 64, 101, 640 };
            int fail = 0, cases = 0;
            var rnd = new Random(12345);
            foreach (int w in sizes)
            {
                foreach (int h in sizes)
                {
                    for (int rep = 0; rep < 3; rep++)
                    {
                        var src = new byte[w * h];
                        rnd.NextBytes(src);
                        int rw, rh, fw, fh;
                        byte[] a = InspectionRunner.Upscale2xBilinear(src, w, h, out rw, out rh);
                        byte[] b = InspectionRunner.Upscale2xBilinearFast(src, w, h, out fw, out fh);
                        cases++;
                        if (rw != fw || rh != fh) { fail++; Console.WriteLine($"DIM MISMATCH {w}x{h}"); continue; }
                        for (int i = 0; i < a.Length; i++)
                        {
                            if (a[i] != b[i])
                            {
                                fail++;
                                Console.WriteLine($"BYTE MISMATCH {w}x{h} at {i}: ref={a[i]} fast={b[i]}");
                                break;
                            }
                        }
                    }
                }
            }
            // 대형 실치수 1건(장비 크롭 근사 6591x4995)
            {
                var src = new byte[6591 * 4995];
                rnd.NextBytes(src);
                int rw, rh, fw, fh;
                byte[] a = InspectionRunner.Upscale2xBilinear(src, 6591, 4995, out rw, out rh);
                byte[] b = InspectionRunner.Upscale2xBilinearFast(src, 6591, 4995, out fw, out fh);
                cases++;
                bool eq = rw == fw && rh == fh;
                if (eq) { for (long i = 0; i < a.LongLength; i++) { if (a[i] != b[i]) { eq = false; break; } } }
                if (!eq) { fail++; Console.WriteLine("BYTE MISMATCH large 6591x4995"); }
            }
            Console.WriteLine($"upcheck: cases={cases} fail={fail}");
            return fail == 0 ? 0 : 3;
        }

        private sealed class AutoRecord
        {
            public int Index;
            public string File;
            public int Width, Height;          // 이미지 px
            public string Verdict;
            public double ResWmm, ResHmm;
            public double LoadMs, InspectMs;
            public int Worker;
            // 확장 결과 필드(2026-07-12) — 치핑/이물 사이즈까지 결과 동일성 대조용
            public double Angle;
            public double ChpTop, ChpBottom, ChpLeft, ChpRight;
            public double Ch1Chip, Ch2Chip, MaxDefact;
            public double ForeignSize, ForeignArea;
            public int NChip, NForeign;
            public string DetailHash = "";
        }

        /// <summary>디펙 상세(치핑 깊이/길이/컨투어, 이물 사각형/크기/면적/판정, 코너/오프셋/각도)의 다이제스트.
        /// 개별 항목 문자열을 정렬 후 해시 — 목록 순서와 무관하게 '내용'이 1비트라도 다르면 값이 달라진다.</summary>
        private static string DetailDigest(BottomResult r)
        {
            var parts = new List<string>();
            if (r.ChippingInfos != null)
            {
                foreach (var ci in r.ChippingInfos)
                {
                    if (ci == null) continue;
                    var sb1 = new StringBuilder();
                    sb1.Append("C:").Append(ci.Depth.ToString("F6")).Append(',').Append(ci.Length.ToString("F6"));
                    if (ci.Contour != null)
                    {
                        sb1.Append(',').Append(ci.Contour.Count);
                        foreach (var pt in ci.Contour) sb1.Append(':').Append(pt.X.ToString("F2")).Append(',').Append(pt.Y.ToString("F2"));
                    }
                    parts.Add(sb1.ToString());
                }
            }
            if (r.ForeignInfos != null)
            {
                foreach (var fi in r.ForeignInfos)
                {
                    if (fi == null) continue;
                    parts.Add("F:" + fi.Rect.X.ToString("F2") + "," + fi.Rect.Y.ToString("F2") + ","
                        + fi.Rect.Width.ToString("F2") + "," + fi.Rect.Height.ToString("F2") + ","
                        + fi.SizeMm.ToString("F6") + "," + fi.Area + "," + (fi.IsNg ? 1 : 0));
                }
            }
            parts.Sort(StringComparer.Ordinal);
            var sb = new StringBuilder();
            sb.Append(r.Angle.ToString("F6")).Append('|');
            sb.Append(r.Offset.X.ToString("F4")).Append(',').Append(r.Offset.Y.ToString("F4")).Append('|');
            if (r.Corners != null) foreach (var c in r.Corners) sb.Append(c.X.ToString("F3")).Append(',').Append(c.Y.ToString("F3")).Append(';');
            sb.Append('|');
            foreach (var s in parts) sb.Append(s).Append('\n');
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hb = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return BitConverter.ToString(hb, 0, 8).Replace("-", "");
            }
        }

        /// <summary>헤드리스 배치 검사 — 이미지별 로드/검사 시간(ms) CSV 리포트 생성.</summary>
        private static int RunAuto(string[] args)
        {
            string folder = args[1];
            int threads = args.Length > 2 && int.TryParse(args[2], out int t) ? Math.Max(1, Math.Min(32, t)) : 8;
            string report = args.Length > 3 ? args[3] : Path.Combine(folder, "_inspect_report.csv");

            string[] exts = { ".png", ".bmp", ".jpg", ".jpeg", ".tif", ".tiff" };
            var files = Directory.EnumerateFiles(folder)
                .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (files.Count == 0) { File.WriteAllText(report, "no image files: " + folder); return 2; }

            // 배치 기본 파라미터 — 칩 스펙은 결과가 스펙아웃(null)으로 전부 끝나지 않도록 광폭(0.1~100mm)으로 설정.
            var p = new TestParameters
            {
                ChipWidthLower = 0.1, ChipWidthUpper = 100,
                ChipHeightLower = 0.1, ChipHeightUpper = 100,
                SaveInspectImages = false
            };

            var records = new AutoRecord[files.Count];
            var indices = new ConcurrentQueue<int>(Enumerable.Range(0, files.Count));
            var swTotal = Stopwatch.StartNew();

            var workers = new List<Task>();
            for (int wi = 0; wi < threads; wi++)
            {
                int workerId = wi + 1;
                workers.Add(Task.Run(() =>
                {
                    var inspector = InspectionRunner.CreateInspector(p);
                    int idx;
                    while (indices.TryDequeue(out idx))
                    {
                        var rec = new AutoRecord { Index = idx, File = Path.GetFileName(files[idx]), Worker = workerId };
                        try
                        {
                            var swLoad = Stopwatch.StartNew();
                            int w, h;
                            byte[] gray = InspectionRunner.LoadGray(files[idx], out w, out h);
                            if (p.ExpandInput2x) gray = InspectionRunner.Upscale2xBilinear(gray, w, h, out w, out h);
                            swLoad.Stop();
                            rec.Width = w; rec.Height = h;
                            rec.LoadMs = swLoad.Elapsed.TotalMilliseconds;

                            var bip = InspectionRunner.BuildParameter(p, gray, w, h, Path.GetTempPath());
                            var swIns = Stopwatch.StartNew();
                            BottomResult r = inspector.BottomInspect(bip);
                            swIns.Stop();
                            rec.InspectMs = swIns.Elapsed.TotalMilliseconds;
                            if (r == null) rec.Verdict = "NULL";
                            else
                            {
                                rec.Verdict = r.DefectCode == 0 ? "OK" : "NG" + r.DefectCode;
                                rec.ResWmm = r.Width; rec.ResHmm = r.Height;
                                rec.Angle = r.Angle;
                                rec.ChpTop = r.ChppingTopSize; rec.ChpBottom = r.ChppingBottomSize;
                                rec.ChpLeft = r.ChppingLeftSize; rec.ChpRight = r.ChppingRightSize;
                                rec.Ch1Chip = r.Channel1ChippingSize; rec.Ch2Chip = r.Channel2ChippingSize;
                                rec.MaxDefact = r.MaxDefactSize;
                                rec.ForeignSize = r.ForeingSize; rec.ForeignArea = r.ForeingArea;
                                rec.NChip = r.ChippingInfos != null ? r.ChippingInfos.Count : 0;
                                rec.NForeign = r.ForeignInfos != null ? r.ForeignInfos.Count : 0;
                                rec.DetailHash = DetailDigest(r);
                            }
                        }
                        catch (Exception ex)
                        {
                            rec.Verdict = "ERR:" + ex.Message.Replace(',', ';');
                        }
                        records[idx] = rec;
                    }
                }));
            }
            Task.WaitAll(workers.ToArray());
            swTotal.Stop();

            var sb = new StringBuilder();
            sb.AppendLine("index,file,img_w,img_h,verdict,res_w_mm,res_h_mm,load_ms,inspect_ms,total_ms,worker,"
                + "angle,chp_top,chp_bottom,chp_left,chp_right,ch1_chip,ch2_chip,max_defact,foreign_size,foreign_area,n_chip,n_foreign,detail_hash");
            foreach (var r in records.Where(r => r != null))
                sb.AppendLine(string.Join(",",
                    r.Index, r.File, r.Width, r.Height, r.Verdict,
                    r.ResWmm.ToString("F4"), r.ResHmm.ToString("F4"),
                    r.LoadMs.ToString("F1"), r.InspectMs.ToString("F1"),
                    (r.LoadMs + r.InspectMs).ToString("F1"), r.Worker,
                    r.Angle.ToString("F6"),
                    r.ChpTop.ToString("F6"), r.ChpBottom.ToString("F6"), r.ChpLeft.ToString("F6"), r.ChpRight.ToString("F6"),
                    r.Ch1Chip.ToString("F6"), r.Ch2Chip.ToString("F6"), r.MaxDefact.ToString("F6"),
                    r.ForeignSize.ToString("F6"), r.ForeignArea.ToString("F6"),
                    r.NChip, r.NForeign, r.DetailHash));

            var ok = records.Where(r => r != null && (r.Verdict == "OK" || r.Verdict.StartsWith("NG") || r.Verdict == "NULL")).ToList();
            sb.AppendLine();
            sb.AppendLine("# summary");
            sb.AppendLine("# files=" + files.Count + " threads=" + threads +
                          " total_s=" + swTotal.Elapsed.TotalSeconds.ToString("F2") +
                          " throughput_ms_per_img=" + (swTotal.Elapsed.TotalMilliseconds / files.Count).ToString("F1"));
            if (ok.Count > 0)
                sb.AppendLine("# inspect_ms avg=" + ok.Average(r => r.InspectMs).ToString("F1") +
                              " min=" + ok.Min(r => r.InspectMs).ToString("F1") +
                              " max=" + ok.Max(r => r.InspectMs).ToString("F1"));

            File.WriteAllText(report, sb.ToString(), Encoding.UTF8);
            return 0;
        }
    }
}
