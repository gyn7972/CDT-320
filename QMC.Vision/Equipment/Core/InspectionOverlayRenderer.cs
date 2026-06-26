using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 검사 종류별 전용 오버레이를 한 곳에서 그리는 공용 렌더러 — 작업 모니터(OperationPage)와
    /// 레시피 INSPECT 페이지가 동일 코드로 그린다(모든 Inspection 이 같은 구조로 자기 오버레이 보유).
    /// 입력은 <see cref="InspectionOverlayStore.Geom"/>(절대 이미지 좌표) + 화면변환 함수.
    /// </summary>
    public static class InspectionOverlayRenderer
    {
        public static void Draw(Graphics g, Func<PointF, PointF> toScreen, InspectionOverlayStore.Geom geom)
        {
            if (g == null || toScreen == null || geom == null) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            switch (geom.Kind)
            {
                case InspectionOverlayStore.OverlayKind.Bottom: DrawBottom(g, toScreen, geom); break;
                case InspectionOverlayStore.OverlayKind.Bin:    DrawBin(g, toScreen, geom);    break;
                default:                                        DrawSide(g, toScreen, geom);   break;
            }
        }

        private static PointF[] ToScreen(PointF[] pts, Func<PointF, PointF> toS)
        {
            if (pts == null) return null;
            var o = new PointF[pts.Length];
            for (int i = 0; i < pts.Length; i++) o[i] = toS(pts[i]);
            return o;
        }

        // ── Bottom(SurfaceInspector): 다이 박스 + 사이즈 라벨 + 칩핑(주황 사각)/이물(자홍 원) ──
        private static void DrawBottom(Graphics g, Func<PointF, PointF> toS, InspectionOverlayStore.Geom m)
        {
            using (var die     = new Pen(m.Pass ? Color.LimeGreen : Color.Orange, 2.5f))
            using (var chipPen = new Pen(Color.DarkOrange, 2f))
            using (var forPen  = new Pen(Color.Magenta, 2f))
            using (var corner  = new SolidBrush(Color.LimeGreen))
            using (var fChip   = new SolidBrush(Color.DarkOrange))
            using (var fFor    = new SolidBrush(Color.Magenta))
            using (var fLbl    = new Font("Consolas", 9f, FontStyle.Bold))
            {
                if (m.Corners != null && m.Corners.Length >= 4)
                {
                    var c = ToScreen(m.Corners, toS);
                    g.DrawPolygon(die, c);
                    foreach (var p in c) g.FillEllipse(corner, p.X - 3, p.Y - 3, 6, 6);
                    if (!string.IsNullOrEmpty(m.Caption))
                    {
                        var anchor = c[0];
                        var sz = g.MeasureString(m.Caption, fLbl);
                        using (var bg = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                            g.FillRectangle(bg, anchor.X, anchor.Y - sz.Height - 4, sz.Width + 6, sz.Height + 2);
                        g.DrawString(m.Caption, fLbl, m.Pass ? corner : fChip, anchor.X + 3, anchor.Y - sz.Height - 3);
                    }
                }
                DrawDefects(g, toS, m, chipPen, forPen, fChip, fFor, fLbl);
            }
        }

        // ── Bin(PlacementGap): 배치 박스 + 결함 ──
        private static void DrawBin(Graphics g, Func<PointF, PointF> toS, InspectionOverlayStore.Geom m)
        {
            using (var box     = new Pen(m.Pass ? Color.LimeGreen : Color.Orange, 2.5f))
            using (var chipPen = new Pen(Color.DarkOrange, 2f))
            using (var forPen  = new Pen(Color.Magenta, 2f))
            using (var fChip   = new SolidBrush(Color.DarkOrange))
            using (var fFor    = new SolidBrush(Color.Magenta))
            using (var fLbl    = new Font("Consolas", 9f, FontStyle.Bold))
            {
                if (m.Corners != null && m.Corners.Length >= 3)
                    g.DrawPolygon(box, ToScreen(m.Corners, toS));
                DrawDefects(g, toS, m, chipPen, forPen, fChip, fFor, fLbl);
            }
        }

        // ── Side(칩핑): 노랑 기준선 + 초록 실제 에지 프로파일 + 결함 ──
        private static void DrawSide(Graphics g, Func<PointF, PointF> toS, InspectionOverlayStore.Geom m)
        {
            if (m.RefCorners != null && m.RefCorners.Length >= 4)
                using (var refp = new Pen(Color.Gold, 1f))
                {
                    var c = ToScreen(m.RefCorners, toS);
                    g.DrawLine(refp, c[0], c[1]); g.DrawLine(refp, c[3], c[2]);
                }
            using (var green = new Pen(Color.LimeGreen, 2f))
            {
                if (m.TopProfile != null && m.TopProfile.Length >= 2) g.DrawLines(green, ToScreen(m.TopProfile, toS));
                if (m.BotProfile != null && m.BotProfile.Length >= 2) g.DrawLines(green, ToScreen(m.BotProfile, toS));
            }
            using (var chipPen = new Pen(Color.DarkOrange, 2f))
            using (var forPen  = new Pen(Color.Magenta, 2f))
            using (var fChip   = new SolidBrush(Color.DarkOrange))
            using (var fFor    = new SolidBrush(Color.Magenta))
            using (var fLbl    = new Font("Consolas", 9f, FontStyle.Bold))
                DrawDefects(g, toS, m, chipPen, forPen, fChip, fFor, fLbl);
        }

        // 결함 — 종류별(이물=자홍 원, 칩핑/기타=주황 사각) + 번호.
        private static void DrawDefects(Graphics g, Func<PointF, PointF> toS, InspectionOverlayStore.Geom m,
            Pen chipPen, Pen forPen, Brush fChip, Brush fFor, Font fLbl)
        {
            if (m.Defects == null) return;
            int ic = 1, ifn = 1;
            foreach (var d in m.Defects)
            {
                var p1 = toS(new PointF((float)(d.X - d.Width / 2), (float)(d.Y - d.Height / 2)));
                var p2 = toS(new PointF((float)(d.X + d.Width / 2), (float)(d.Y + d.Height / 2)));
                float ww = Math.Max(8, p2.X - p1.X), hh = Math.Max(8, p2.Y - p1.Y);
                if (string.Equals(d.Type, "Foreign", StringComparison.OrdinalIgnoreCase))
                {
                    g.DrawEllipse(forPen, p1.X, p1.Y, ww, hh);
                    g.DrawString("F" + (ifn++), fLbl, fFor, p1.X, p1.Y - 14);
                }
                else
                {
                    g.DrawRectangle(chipPen, p1.X, p1.Y, ww, hh);
                    g.DrawString("C" + (ic++), fLbl, fChip, p1.X, p1.Y - 14);
                }
            }
        }
    }
}
