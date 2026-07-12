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

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
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
            sb.AppendLine("index,file,img_w,img_h,verdict,res_w_mm,res_h_mm,load_ms,inspect_ms,total_ms,worker");
            foreach (var r in records.Where(r => r != null))
                sb.AppendLine(string.Join(",",
                    r.Index, r.File, r.Width, r.Height, r.Verdict,
                    r.ResWmm.ToString("F4"), r.ResHmm.ToString("F4"),
                    r.LoadMs.ToString("F1"), r.InspectMs.ToString("F1"),
                    (r.LoadMs + r.InspectMs).ToString("F1"), r.Worker));

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
