using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>샘플 검출 결과(이미지 + 오버레이) 한 건.</summary>
    public class SampleChip
    {
        public Bitmap Image;
        public PointF[] Box;
        public PointF[] Marks;
        public string[] Lines;
        public string Verdict;
        public bool Pass;
    }

    /// <summary>
    /// 하드웨어 없이 뷰어를 채우기 위한 샘플 데이터 생성기 —
    /// 합성 칩 이미지(GDI), 모드별 추세 시리즈, 결과 그리드 행.
    /// 실제 데이터 바인딩(InspectionResultStore) 전까지 화면 확인/검출 시연용.
    /// </summary>
    public static class SampleData
    {
        private static readonly Random R = new Random(7);

        // ── 합성 칩 이미지 ──
        public static SampleChip MakeChip(InspectionMode mode, int picker)
        {
            int W = 380, H = 300;
            var bmp = new Bitmap(W, H);
            var sc = new SampleChip { Image = bmp, Pass = true };
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(28, 28, 30));

                if (mode == InspectionMode.Bottom)
                {
                    var pad = new Rectangle(64, 48, 252, 200);
                    using (var body = new SolidBrush(Color.FromArgb(14, 14, 16)))
                        g.FillRectangle(body, pad.X - 22, pad.Y - 18, pad.Width + 44, pad.Height + 36);
                    using (var br = new SolidBrush(Color.FromArgb(206, 206, 206)))
                        g.FillRectangle(br, pad);
                    double w = 8.069 + (R.NextDouble() - .5) * 0.004;
                    double h = 6.070 + (R.NextDouble() - .5) * 0.004;
                    sc.Box = Corners(pad);
                    sc.Lines = new[] { "Width : " + w.ToString("F4"), "Height : " + h.ToString("F4") };
                    sc.Verdict = "Good";
                }
                else if (mode == InspectionMode.Side)
                {
                    // 측면 — 검은 배경 위 밝은 가는 띠(엣지)
                    using (var br = new SolidBrush(Color.FromArgb(225, 225, 225)))
                        g.FillRectangle(br, 40, 150, 300, 14);
                    using (var soft = new SolidBrush(Color.FromArgb(90, 200, 200, 200)))
                        g.FillRectangle(soft, 40, 146, 300, 22);
                    sc.Box = null;
                    sc.Lines = new[] { "Front max : " + (0.003 + R.NextDouble() * 0.003).ToString("F4"),
                                       "Back max : " + (0.003 + R.NextDouble() * 0.004).ToString("F4") };
                    sc.Verdict = "Good";
                }
                else // Bin / Die gap
                {
                    var die = new Rectangle(70, 60, 240, 180);
                    using (var br = new SolidBrush(Color.FromArgb(20, 20, 22)))
                        g.FillRectangle(br, 0, 0, W, H);
                    using (var pen = new Pen(Color.FromArgb(120, 120, 120), 2))
                        g.DrawRectangle(pen, die);
                    sc.Box = Corners(die);
                    double mn = 0.282 + (R.NextDouble() - .5) * 0.01;
                    sc.Lines = new[] { "Top gap min : " + mn.ToString("F3"),
                                       "Top gap max : " + (mn + 0.004).ToString("F3") };
                    sc.Verdict = "Good";
                }
            }
            return sc;
        }

        // ── Side 4채널(Front ch1/2, Back ch1/2) 측면 이미지(4000×700 비율의 가는 스트립) ──
        public static Bitmap[] MakeSideChannels(int picker)
        {
            var imgs = new Bitmap[4];
            for (int c = 0; c < 4; c++)
            {
                int W = 600, H = 84;
                var bmp = new Bitmap(W, H);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(16, 16, 18));
                    int barY = H / 2 - 5 + (R.Next(-3, 3));
                    using (var soft = new SolidBrush(Color.FromArgb(70, 200, 200, 200)))
                        g.FillRectangle(soft, 40, barY - 3, W - 80, 16);
                    using (var br = new SolidBrush(Color.FromArgb(225, 225, 225)))
                        g.FillRectangle(br, 40, barY, W - 80, 9);
                    // 가끔 칩핑 노치(밝은 띠 위쪽 결손)
                    if (R.NextDouble() < 0.25)
                    {
                        int nx = 60 + R.Next(W - 160);
                        using (var bg = new SolidBrush(Color.FromArgb(16, 16, 18)))
                            g.FillRectangle(bg, nx, barY - 4, 14, 7);
                    }
                }
                imgs[c] = bmp;
            }
            return imgs;
        }

        private static PointF[] Corners(Rectangle r) => new[]
        {
            new PointF(r.Left, r.Top), new PointF(r.Right, r.Top),
            new PointF(r.Right, r.Bottom), new PointF(r.Left, r.Bottom)
        };

        // ── 추세 시리즈(차트) ──
        public static double[] Series(InspectionMode mode, int which, out double upper, out double lower, out string title, out Color color)
        {
            int n = 120;
            var v = new double[n];
            if (mode == InspectionMode.Bottom)
            {
                double c = which == 0 ? 8.070 : 6.070;
                upper = c + 0.012; lower = c - 0.012;
                title = which == 0 ? "너비 그래프 [mm]" : "높이 그래프 [mm]";
                color = which == 0 ? Color.FromArgb(0x37, 0x8A, 0xDD) : Color.FromArgb(0x1D, 0x9E, 0x75);
                for (int i = 0; i < n; i++) v[i] = c + (R.NextDouble() - .5) * 0.006;
            }
            else if (mode == InspectionMode.Side)
            {
                upper = 0.050; lower = -0.025;
                title = which == 0 ? "Front max chipping depth" : "Back max chipping depth";
                color = Color.FromArgb(0x1D, 0x9E, 0x75);
                for (int i = 0; i < n; i++) v[i] = 0.003 + (R.NextDouble()) * 0.006;
            }
            else
            {
                double c = 0.285;
                upper = 0.40; lower = 0.18;
                title = which == 0 ? "오른쪽 갭 그래프 [mm]" : "아래쪽 갭 그래프 [mm]";
                color = which == 0 ? Color.FromArgb(0x37, 0x8A, 0xDD) : Color.FromArgb(0x1D, 0x9E, 0x75);
                for (int i = 0; i < n; i++) v[i] = c + (R.NextDouble() - .5) * 0.012;
            }
            return v;
        }

        // ── 웨이퍼 맵 편차비(0~1) ──  중앙=0(흰색), 상/하한 근접=1(붉음)
        public static double[,] WaferRatios(int n = 24)
        {
            var m = new double[n, n];
            double cc = (n - 1) / 2.0, rad = n / 2.0;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    double dist = Math.Sqrt((c - cc) * (c - cc) + (r - cc) * (r - cc));
                    if (dist > rad - 0.5) { m[r, c] = double.NaN; continue; }
                    double edge = dist / rad;                       // 가장자리일수록 편차 큰 경향(중앙=흰색)
                    double v = Math.Pow(edge, 1.6) * (0.65 + 0.35 * R.NextDouble()) + R.NextDouble() * 0.06;
                    if (R.NextDouble() < 0.035) v = 0.85 + R.NextDouble() * 0.15;  // 드문 핫스팟
                    m[r, c] = Math.Min(1.0, v);
                }
            return m;
        }

        // ── 결과 그리드 행 ──
        public static List<string[]> Rows(InspectionMode mode, int count = 16)
        {
            var list = new List<string[]>();
            for (int i = 0; i < count; i++)
            {
                int ix = 22, iy = i + 1, pk = (i % 4) + 1;
                if (mode == InspectionMode.Bottom)
                    list.Add(new[] { ix.ToString(), iy.ToString(), pk.ToString(),
                        (8.070 + (R.NextDouble()-.5)*0.004).ToString("F4"),
                        (6.070 + (R.NextDouble()-.5)*0.004).ToString("F4"),
                        ((R.NextDouble()-.5)*0.8).ToString("F3"),
                        ((R.NextDouble()-.5)*0.2).ToString("F4"),
                        ((R.NextDouble()-.5)*0.2).ToString("F4") });
                else if (mode == InspectionMode.Side)
                    list.Add(new[] { ix.ToString(), iy.ToString(), pk.ToString(),
                        (R.NextDouble()*0.006).ToString("F4"),
                        (R.NextDouble()*0.006).ToString("F4") });
                else
                    list.Add(new[] { ix.ToString(), iy.ToString(), pk.ToString(),
                        (0.285+(R.NextDouble()-.5)*0.01).ToString("F3"),
                        (0.282+(R.NextDouble()-.5)*0.01).ToString("F3"),
                        (0.285+(R.NextDouble()-.5)*0.01).ToString("F3"),
                        ((R.NextDouble()-.5)*0.04).ToString("F4"),
                        ((R.NextDouble()-.5)*0.04).ToString("F4"),
                        ((R.NextDouble()-.5)*0.04).ToString("F3") });
            }
            return list;
        }
    }
}
