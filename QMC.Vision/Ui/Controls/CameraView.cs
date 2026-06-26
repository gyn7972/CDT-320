using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Ui.Controls;
using QMC.Vision.Core;
using QMC.Vision.Modules;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 범용 <see cref="CameraViewBase"/>(QMC.Common) 의 비전 전용 어댑터.
    /// 표시/오버레이/툴바/측정/줌·팬 로직은 모두 base 에 있고, 여기서는 기존 호출부 호환을 위해
    /// GrabResult / Roi / MatchResult / IVisionModule 오버로드만 제공한다.
    /// </summary>
    public partial class CameraView : CameraViewBase
    {
        private IVisionModule _module;
        private VisionModuleSource _source;   // 툴바 Grab/Live 소스(활성 도구 id 전달용)
        private double _editAngle;   // ROI 드래그 시 보존할 각도(원본 Roi)

        public CameraView()
        {
            InfoForeColor = UiTheme.VisionInfoFg;
            base.RoiEdited += OnBaseRoiEdited;
        }

        /// <summary>ROI 드래그 종료 — Roi 타입으로 재노출(기존 호출부 호환).</summary>
        public new event Action<string, Roi> RoiEdited;

        private void OnBaseRoiEdited(string kind, System.Drawing.RectangleF rect)
        {
            var roi = new Roi
            {
                Name     = kind,
                CenterX  = rect.X + rect.Width  / 2.0,
                CenterY  = rect.Y + rect.Height / 2.0,
                Width    = rect.Width,
                Height   = rect.Height,
                AngleDeg = _editAngle
            };
            try { RoiEdited?.Invoke(kind, roi); } catch { }
        }

        /// <summary>그랩 결과 표시(기존 API).</summary>
        public void SetFrame(GrabResult r) { if (r != null && r.Image != null) SetImage(r.Image); }

        /// <summary>스케일/캘리브레이션 플래그(X반전/Y반전/90° 회전) → 표시 방향변환(<see cref="System.Drawing.RotateFlipType"/>).
        /// 90° 회전이면 가로/세로가 스왑된다. 8가지 조합 모두 단일 RotateFlipType 으로 표현 가능.</summary>
        public static System.Drawing.RotateFlipType OrientationFromFlags(bool invertedX, bool invertedY, bool rotated90)
        {
            if (!rotated90)
            {
                if (!invertedX && !invertedY) return System.Drawing.RotateFlipType.RotateNoneFlipNone;
                if ( invertedX && !invertedY) return System.Drawing.RotateFlipType.RotateNoneFlipX;
                if (!invertedX &&  invertedY) return System.Drawing.RotateFlipType.RotateNoneFlipY;
                return System.Drawing.RotateFlipType.RotateNoneFlipXY;
            }
            if (!invertedX && !invertedY) return System.Drawing.RotateFlipType.Rotate90FlipNone;
            if ( invertedX && !invertedY) return System.Drawing.RotateFlipType.Rotate90FlipX;
            if (!invertedX &&  invertedY) return System.Drawing.RotateFlipType.Rotate90FlipY;
            return System.Drawing.RotateFlipType.Rotate90FlipXY;
        }

        /// <summary>ROI 사각형 + 매칭 결과 오버레이(기존 API — 점+점수만).</summary>
        public void SetOverlay(Roi roi, MatchResult result)
        {
            base.SetOverlay(RectOf(roi), MarksFrom(result));
        }

        /// <summary>ROI 사각형 + 매칭 결과 오버레이(매칭 박스 표시) — boxW/boxH(이미지 px)는 보통 Train ROI 크기.
        /// 각 검출 인스턴스를 검출 각도로 회전한 박스 + 중심 + 점수로 그린다.</summary>
        public void SetOverlay(Roi roi, MatchResult result, double boxW, double boxH)
        {
            base.SetOverlay(RectOf(roi), MarksFrom(result, boxW, boxH));
        }

        /// <summary>Grab/Live 대상 모듈 지정(기존 API) — 내부적으로 ICameraViewSource 어댑터로 연결.</summary>
        public void AttachModule(IVisionModule module)
        {
            _module = module;
            _source = module == null ? null : new VisionModuleSource(module);
            AttachSource(_source);
        }

        /// <summary>툴바 Grab 이 사용할 활성 도구(Finder/Inspector) 등록 id 지정.
        /// 설정 시 툴바 Grab 이 그 도구 전용 시뮬 저장이미지(GrabForTool)를 우선 사용한다. null/빈값이면 모듈 Grab.</summary>
        public void SetActiveTool(string toolId)
        {
            if (_source != null) _source.ActiveToolId = toolId;
        }

        /// <summary>ROI 드래그 편집 진입(기존 API, Roi).</summary>
        public void BeginRoiDrag(string kind, Roi current)
        {
            _editAngle = current?.AngleDeg ?? 0;
            base.BeginRoiDrag(kind, RectOf(current));
        }

        /// <summary>측정 거리를 px 단위로 표시(스케일 무관). MmPerPixel=0 이면 base 가 "px"로 표기.
        /// 나중에 mm 가 필요하면 모듈 ScaleX/Y(ExportCameraMapping)를 주입해 전환한다.</summary>
        protected override void RefreshMeasureScale()
        {
            MmPerPixelX = 0; MmPerPixelY = 0;   // px 단위 측정
        }

        // ── 마우스 PX 좌표 표시(우클릭 메뉴 ON/OFF, 기본 OFF) ──
        private Point _cursorPt;
        private bool  _cursorIn;
        /// <summary>true 면 마우스 위치의 이미지 PX 좌표를 커서 옆에 상시 표시.</summary>
        public bool ShowCursorReadout { get; set; } = false;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!ShowCursorReadout) return;
            _cursorPt = e.Location; _cursorIn = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_cursorIn) { _cursorIn = false; if (ShowCursorReadout) Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawDetectOverlay(e);   // 검출(안착) 오버레이
            DrawColletOverlay(e);   // 콜렛 전용 오버레이(회전 사각형+중심십자+노란 라벨)
            if (!ShowCursorReadout || !_cursorIn || CurrentFrame == null) return;
            var dst = DisplayRect();
            if (dst.Width <= 0 || dst.Height <= 0 || !dst.Contains(_cursorPt)) return;

            int ix = (int)((_cursorPt.X - dst.Left) * (double)CurrentFrame.Width  / dst.Width);
            int iy = (int)((_cursorPt.Y - dst.Top)  * (double)CurrentFrame.Height / dst.Height);
            ix = Math.Max(0, Math.Min(CurrentFrame.Width  - 1, ix));
            iy = Math.Max(0, Math.Min(CurrentFrame.Height - 1, iy));

            int val = 0;   // 해당 픽셀 밝기(0~255) — Threshold 판단용
            try { var pc = CurrentFrame.GetPixel(ix, iy); val = (pc.R + pc.G + pc.B) / 3; } catch { }
            string txt = "X:" + ix + " Y:" + iy + "  V:" + val;
            var g = e.Graphics;
            using (var f = new Font("Consolas", 9.5f, FontStyle.Bold))
            {
                var sz = g.MeasureString(txt, f);
                float tx = _cursorPt.X + 14, ty = _cursorPt.Y + 2;
                if (tx + sz.Width + 6 > ClientSize.Width)  tx = _cursorPt.X - sz.Width - 14;
                if (ty + sz.Height + 2 > ClientSize.Height) ty = _cursorPt.Y - sz.Height - 4;
                using (var bg = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                    g.FillRectangle(bg, tx - 3, ty - 1, sz.Width + 6, sz.Height + 2);
                using (var br = new SolidBrush(Color.FromArgb(120, 230, 120)))
                    g.DrawString(txt, f, br, tx, ty);
            }
        }

        // ── 검출 오버레이(안착검사): 다이 외곽(4코너)+중심+판정 표시 ──
        private bool _detShow;
        private System.Drawing.RectangleF _detRoi;   // 이미지 좌표
        private PointF[] _detCorners;                 // 이미지 좌표
        private PointF _detCenter;
        private bool _detPass;
        private string _detText = "";

        /// <summary>검출 기하를 카메라에 오버레이(이미지 좌표). corners=4점.</summary>
        public void SetDetectOverlay(Roi roi, PointF[] cornersImg, PointF centerImg, bool pass, string text)
        {
            _detRoi = RectOf(roi);
            _detCorners = cornersImg;
            _detCenter = centerImg;
            _detPass = pass;
            _detText = text ?? "";
            _detShow = cornersImg != null && cornersImg.Length == 4;
            Invalidate();
        }

        /// <summary>검출 오버레이 제거.</summary>
        public void ClearDetectOverlay() { _detShow = false; Invalidate(); }

        private void DrawDetectOverlay(PaintEventArgs e)
        {
            if (!_detShow || CurrentFrame == null) return;
            var dst = DisplayRect();
            if (dst.Width <= 0 || dst.Height <= 0) return;
            double rx = (double)dst.Width / CurrentFrame.Width, ry = (double)dst.Height / CurrentFrame.Height;
            var g = e.Graphics;
            Color col = _detPass ? Color.Lime : Color.OrangeRed;

            if (_detRoi.Width > 0)
                using (var rp = new Pen(Color.FromArgb(200, 255, 160, 0), 1.5f))
                    g.DrawRectangle(rp, dst.Left + (float)(_detRoi.X * rx), dst.Top + (float)(_detRoi.Y * ry),
                                    (float)(_detRoi.Width * rx), (float)(_detRoi.Height * ry));

            var poly = new PointF[4];
            for (int i = 0; i < 4; i++)
                poly[i] = new PointF((float)(dst.Left + _detCorners[i].X * rx), (float)(dst.Top + _detCorners[i].Y * ry));
            using (var pen = new Pen(col, 2.5f)) g.DrawPolygon(pen, poly);

            float ccx = (float)(dst.Left + _detCenter.X * rx), ccy = (float)(dst.Top + _detCenter.Y * ry);
            using (var cb = new SolidBrush(Color.Lime)) g.FillEllipse(cb, ccx - 4, ccy - 4, 8, 8);

            string t = (_detPass ? "PASS" : "NG") + "  " + _detText;
            using (var f = new Font("Consolas", 11f, FontStyle.Bold))
            {
                var sz = g.MeasureString(t, f);
                float tx = dst.Left + (float)(_detRoi.X * rx) + 4, ty = dst.Top + (float)(_detRoi.Y * ry) + 4;
                using (var bg = new SolidBrush(Color.FromArgb(160, 0, 0, 0))) g.FillRectangle(bg, tx - 2, ty - 1, sz.Width + 4, sz.Height + 2);
                using (var tb = new SolidBrush(col)) g.DrawString(t, f, tb, tx, ty);
            }
        }

        // ── 콜렛 전용 오버레이: 검출 회전 사각형 + 중심 십자 + 노란 "중심/각도" 라벨 ──
        //    (테스트 프로그램 'Collet Finder' 와 동일 표기. 안착검사 detect 오버레이와 독립.)
        private bool       _colShow;
        private PointF[]   _colCorners;            // 이미지 좌표 4점
        private PointF     _colCenter;             // 이미지 좌표
        private string     _colLabel = "";
        private System.Drawing.RectangleF _colRoi; // 검색 ROI(이미지 좌표)
        private bool       _colOk = true;

        /// <summary>콜렛 검출 오버레이 — 회전 사각형 4코너(이미지 px)+중심+라벨. corners=4점이어야 표시.</summary>
        public void SetColletOverlay(Roi searchRoi, PointF[] cornersImg, PointF centerImg, string label, bool ok)
        {
            _colRoi     = RectOf(searchRoi);
            _colCorners = cornersImg;
            _colCenter  = centerImg;
            _colLabel   = label ?? "";
            _colOk      = ok;
            _colShow    = cornersImg != null && cornersImg.Length == 4;
            Invalidate();
        }

        /// <summary>콜렛 오버레이 제거.</summary>
        public void ClearColletOverlay() { _colShow = false; Invalidate(); }

        private void DrawColletOverlay(PaintEventArgs e)
        {
            if (!_colShow || CurrentFrame == null) return;
            var dst = DisplayRect();
            if (dst.Width <= 0 || dst.Height <= 0) return;
            double rx = (double)dst.Width / CurrentFrame.Width, ry = (double)dst.Height / CurrentFrame.Height;
            var g = e.Graphics;

            // 검색 ROI(초록 점선)
            if (_colRoi.Width > 0)
                using (var rp = new Pen(Color.FromArgb(170, 90, 200, 90), 1f)
                       { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    g.DrawRectangle(rp, dst.Left + (float)(_colRoi.X * rx), dst.Top + (float)(_colRoi.Y * ry),
                                    (float)(_colRoi.Width * rx), (float)(_colRoi.Height * ry));

            // 검출 사각형(초록 실선 — OK / 주황 — NG)
            var poly = new PointF[4];
            for (int i = 0; i < 4; i++)
                poly[i] = new PointF((float)(dst.Left + _colCorners[i].X * rx), (float)(dst.Top + _colCorners[i].Y * ry));
            using (var pen = new Pen(_colOk ? Color.Lime : Color.OrangeRed, 2.2f)) g.DrawPolygon(pen, poly);

            // 중심 빨간 십자(+)
            float ccx = (float)(dst.Left + _colCenter.X * rx), ccy = (float)(dst.Top + _colCenter.Y * ry);
            using (var cp = new Pen(Color.Red, 2f))
            {
                g.DrawLine(cp, ccx - 9, ccy, ccx + 9, ccy);
                g.DrawLine(cp, ccx, ccy - 9, ccx, ccy + 9);
            }

            // 노란 라벨(중심 아래, 가운데정렬, 어두운 배경) — "중심(X, Y)  각도 N°"
            if (!string.IsNullOrEmpty(_colLabel))
                using (var f = new Font("맑은 고딕", 10f, FontStyle.Bold))
                {
                    var sz = g.MeasureString(_colLabel, f);
                    float tx = ccx - sz.Width / 2f, ty = ccy + 12;
                    using (var bg = new SolidBrush(Color.FromArgb(175, 0, 0, 0)))
                        g.FillRectangle(bg, tx - 4, ty - 1, sz.Width + 8, sz.Height + 2);
                    using (var tb = new SolidBrush(Color.Yellow)) g.DrawString(_colLabel, f, tb, tx, ty);
                }
        }

        private static System.Drawing.RectangleF RectOf(Roi roi)
        {
            if (roi == null) return System.Drawing.RectangleF.Empty;
            var b = roi.BoundingBox;
            return new System.Drawing.RectangleF(b.X, b.Y, b.Width, b.Height);
        }

        private static List<OverlayMark> MarksFrom(MatchResult mr)
        {
            if (mr?.Instances == null) return null;
            var list = new List<OverlayMark>();
            foreach (var i in mr.Instances) list.Add(new OverlayMark(i.CenterX, i.CenterY, i.Score));
            return list;
        }

        private static List<OverlayMark> MarksFrom(MatchResult mr, double boxW, double boxH)
        {
            if (mr?.Instances == null) return null;
            var list = new List<OverlayMark>();
            foreach (var i in mr.Instances)
                list.Add(new OverlayMark(i.CenterX, i.CenterY, i.Score, i.AngleDeg, boxW, boxH));
            return list;
        }
    }
}
