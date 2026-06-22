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
