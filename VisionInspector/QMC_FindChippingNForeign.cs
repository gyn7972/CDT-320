using QMC.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Vision.Inspector
{
    // QMC_ResultChpping 클래스는 칩의 칩핑을 검사하고 결과를 리턴 하는 클래스를 구현 합니다.
    public class PeekValue
    {
        public int nX = 0;
        public int nY = 0;
        public double dValue = 0;
        public PeekValue(int x, int y, double value)
        {
            nX = x;
            nY = y;
            dValue = value;
        }
    }
    public class QMC_ResultChppingNForeign
    {
        //생성자를 구현 합니다.
        public QMC_ResultChppingNForeign()
        {
            LeftTop = new PointF(0, 0);
            RightTop = new PointF(100, 1);
            LeftBottom = new PointF(0, 100);
            RightBottom = new PointF(101, 101);
        }
        public byte[,] shiftimage;
        public byte[,] shiftSobelimage;

        public List<Region> m_RegionChipping = new List<Region>();
        public List<Region> m_RegionForeign = new List<Region>();
        public bool m_bChipping = false;
        public bool m_bForeign = false;
        public Line m_lineTop = null;
        public Line m_lineBottom = null;
        public Line m_lineLeft = null;
        public Line m_lineRight = null;

        public PointF LeftTop = new PointF(0, 0);
        public PointF RightTop = new PointF(0, 0);
        public PointF LeftBottom = new PointF(0, 0);
        public PointF RightBottom = new PointF(0, 0);

        public double w = 0;
        public double h = 0;

        public double GetAngle()
        {
            // 각 변의 벡터 계산
            var vecTop = new PointF(RightTop.X - LeftTop.X, RightTop.Y - LeftTop.Y);
            var vecBottom = new PointF(RightBottom.X - LeftBottom.X, RightBottom.Y - LeftBottom.Y);
            var vecLeft = new PointF(LeftBottom.X - LeftTop.X, LeftBottom.Y - LeftTop.Y);
            var vecRight = new PointF(RightBottom.X - RightTop.X, RightBottom.Y - RightTop.Y);

            // 각 변의 이상적인 각도(수평: 0도, 수직: 90도)와의 차이 계산
            double angleTop = Math.Atan2(vecTop.Y, vecTop.X) * 180.0 / Math.PI;
            double angleBottom = Math.Atan2(vecBottom.Y, vecBottom.X) * 180.0 / Math.PI;
            double angleLeft = Math.Atan2(vecLeft.Y, vecLeft.X) * 180.0 / Math.PI;
            double angleRight = Math.Atan2(vecRight.Y, vecRight.X) * 180.0 / Math.PI;

            // 각 변의 이상적인 각도
            double idealTop = 0.0;
            double idealBottom = 0.0;
            double idealLeft = 90.0;
            double idealRight = 90.0;

            // 각 변의 각도 차이(절대값)
            double diffTop = NormalizeAngle(angleTop - idealTop);
            double diffBottom = NormalizeAngle(angleBottom - idealBottom);
            double diffLeft = NormalizeAngle(angleLeft - idealLeft);
            double diffRight = NormalizeAngle(angleRight - idealRight);
            List<double> list = new List<double>();
            list.Add(diffTop);
            list.Add(diffBottom);
            list.Add(diffLeft);
            list.Add(diffRight);
            //list Min Max 차가 1도 이상이면 이상으로 판단
            if (list.Max() - list.Min() > 2.0)
            {
                return double.NaN; // 이상으로 판단
            }


            // 평균값 반환
            return (diffTop + diffBottom + diffLeft + diffRight) / 4.0;
        }

        // -180~180 범위로 각도 정규화
        private double NormalizeAngle(double angle)
        {
            while (angle > 180.0) angle -= 360.0;
            while (angle < -180.0) angle += 360.0;
            return angle;
        }
    }


    public class QMC_FindChippingNForeign
    {
        // 내부 Parallel.For 병렬도 상한(2026-07-12) — 동시 검사 개수에 맞춰 코어를 배분한다(CDTInspector 가 설정).
        // 각 인덱스(열/행)가 서로 독립 계산이라 분할 수가 바뀌어도 결과는 동일하다(기존에도 분할은 런타임 결정).
        public static int MaxParallelism = Environment.ProcessorCount;

        private static ParallelOptions ParallelOpts
        {
            get { return new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, MaxParallelism) }; }
        }

        /// <summary>피크 기록용 값 타입(2026-07-12) — 종전 PeekValue 클래스는 열/행마다 수백 개씩 힙 할당되어
        /// (검사 1건당 수백만 개) GC 정지가 스캔 시간을 지배했다. 필드/값/연산은 동일 — 결과 불변.</summary>
        private struct PeekValueS
        {
            public int nX;
            public int nY;
            public double dValue;
            public PeekValueS(int x, int y, double value) { nX = x; nY = y; dValue = value; }
        }

        /// <summary>LINQ Where+OrderBy 대체(2026-07-12) — listPeek 는 스캔 진행 방향으로 키(nY/nX)가 단조로
        /// 추가되므로 '임계 초과 중 최소(최대) 키 원소' = '삽입 순서상 첫 임계 초과 원소'다(OrderBy 는 안정 정렬,
        /// 키 단조). 선형 첫-일치 탐색은 기존과 동일한 원소를 반환하며 열/행마다의 정렬·열거자 할당이 사라진다.</summary>
        private static int FirstPeekOverIdx(List<PeekValueS> listPeek, double threshold)
        {
            for (int i = 0; i < listPeek.Count; i++)
            {
                if (listPeek[i].dValue > threshold) return i;
            }
            return -1;
        }

        /// <summary>밴드 전치(2026-07-12) — Top/Bottom 라인 탐색은 열 단위 세로 스캔이라 row-major 배열에서
        /// 접근 간격이 이미지 폭(13182B)이 되어 캐시 미스가 극심하다. 스캔 행 구간 [y0..y1]만 전치해 두면
        /// 열 스캔이 연속 메모리 접근이 된다. 픽셀 값과 판정 산술은 그대로 — 결과 동일. 버퍼는 BufferPool 재사용.</summary>
        private static unsafe byte[] TransposeBand(byte[,] img, int nWidth, int y0, int y1, out int bandH)
        {
            bandH = y1 - y0 + 1;
            byte[] tp = BufferPool.Rent(nWidth * bandH);
            fixed (byte* pSrcF = &img[0, 0])
            fixed (byte* pDstF = tp)
            {
                IntPtr srcPtr = (IntPtr)pSrcF, dstPtr = (IntPtr)pDstF;
                int w = nWidth, bh = bandH, yy0 = y0;
                const int TILE = 64;
                int tilesX = (w + TILE - 1) / TILE;
                Parallel.For(0, tilesX, ParallelOpts, tx =>
                {
                    unsafe
                    {
                        byte* src = (byte*)srcPtr;
                        byte* dst = (byte*)dstPtr;
                        int x0 = tx * TILE;
                        int x1 = Math.Min(x0 + TILE, w);
                        for (int yb = 0; yb < bh; yb += TILE)
                        {
                            int yend = Math.Min(yb + TILE, bh);
                            for (int y = yb; y < yend; y++)
                            {
                                byte* srow = src + (long)(yy0 + y) * w;
                                for (int x = x0; x < x1; x++)
                                {
                                    dst[(long)x * bh + y] = srow[x];
                                }
                            }
                        }
                    }
                });
            }
            return tp;
        }

        //칩의 엣지 부분 에서 검사 안하는 영역 좌,우,상,하를 설정 할수 있습니다.
        private int nRegionSize = 1;
        private int m_nLeftMargin = 0;
        private int m_nRightMargin = 0;
        private int m_nTopMargin = 0;
        private int m_nBottomMargin = 0;
        private int m_nLeftMarginForeign = 0;
        private int m_nRightMarginForeign = 0;
        private int m_nTopMarginForeign = 0;
        private int m_nBottomMarginForeign = 0;


        // 칩핑을 판정하기 위한 Gray Level의 Threshold를 설정 합니다.
        private int m_nChippingThreshold = 0;

        // 칩내부의 이물을 판정하기 위한 Gray Level의 Threshold를 설정 합니다.
        private int m_nForeignThreshold = 0;

        // 칩핑의 깊이가 설정한 값 이상이면 칩핑으로 판정 합니다.

        private int m_nChippingDepth = 10;

        private int m_nForeignSize = 10;

        private double m_dCutRatio = 2.5;
        // 칩의 내부에 검사를 하지 않는 영역을 사각형으로 설정 할수 있습니다.
        public Rectangle RectangleMask = new Rectangle(0, 0, 0, 0);
        // 생성자를 구현 합니다.
        public QMC_FindChippingNForeign()
        {
            m_nLeftMargin = 0;
            m_nRightMargin = 0;
            m_nTopMargin = 0;
            m_nBottomMargin = 0;
            m_nLeftMarginForeign = 0;
            m_nRightMarginForeign = 0;
            m_nTopMarginForeign = 0;
            m_nBottomMarginForeign = 0;
            m_nChippingThreshold = 0;

        }

        public void SetRectangleMask(int nLeft, int nTop, int nRight, int nBottom)
        {
            RectangleMask.X = nLeft;
            RectangleMask.Y = nTop;
            RectangleMask.Width = nRight - nLeft;
            RectangleMask.Height = nBottom - nTop;
        }


        // 포인트 리스트의 X 표준 편차를 구한다.
        private double GetStdevPointX(List<Point> points)
        {
            double dStdev = 0;
            double dSum = 0;
            double dSum2 = 0;
            int nCount = points.Count;
            foreach (var v in points)
            {
                dSum += v.X;
                dSum2 += v.X * v.X;
            }
            double dMean = dSum / nCount;
            dStdev = Math.Sqrt(dSum2 / nCount - dMean * dMean);
            return dStdev;
        }

        // 포인트 리스트의 X 표준 편차를 구한다.
        private double GetStdevPointX(List<PointF> points)
        {
            double dStdev = 0;
            double dSum = 0;
            double dSum2 = 0;
            int nCount = points.Count;
            foreach (var v in points)
            {
                dSum += v.X;
                dSum2 += v.X * v.X;
            }
            double dMean = dSum / nCount;
            dStdev = Math.Sqrt(dSum2 / nCount - dMean * dMean);
            return dStdev;
        }

        // 포인트 리스트의 Y 표준 편차를 구한다.

        private double GetStdevPointY(List<Point> points)
        {
            double dStdev = 0;
            double dSum = 0;
            double dSum2 = 0;
            int nCount = points.Count;
            foreach (var v in points)
            {
                dSum += v.Y;
                dSum2 += v.Y * v.Y;
            }
            double dMean = dSum / nCount;
            dStdev = Math.Sqrt(dSum2 / nCount - dMean * dMean);
            return dStdev;
        }
        private double GetStdevPointY(List<PointF> points)
        {
            double dStdev = 0;
            double dSum = 0;
            double dSum2 = 0;
            int nCount = points.Count;
            foreach (var v in points)
            {
                dSum += v.Y;
                dSum2 += v.Y * v.Y;
            }
            double dMean = dSum / nCount;
            dStdev = Math.Sqrt(dSum2 / nCount - dMean * dMean);
            return dStdev;
        }


        void OffsetXPoints(List<PointF> points, float dOffsetX)
        {
            for (int iter = 0; iter < points.Count; iter++)
            {
                points[iter] = new PointF(points[iter].X + dOffsetX, points[iter].Y);
            }
        }

        void OffsetYPoints(List<PointF> points, float dOffsetY)
        {
            for (int iter = 0; iter < points.Count; iter++)
            {
                points[iter] = new PointF(points[iter].X, points[iter].Y + dOffsetY);
            }
        }
        public double GetDistance(PointF pt1, PointF pt2)
        {
            double dX = pt1.X - pt2.X;
            double dY = pt1.Y - pt2.Y;
            return Math.Sqrt(dX * dX + dY * dY);
        }
        public double GetDistance(PointD pt1, PointD pt2)
        {
            double dX = pt1.X - pt2.X;
            double dY = pt1.Y - pt2.Y;
            return Math.Sqrt(dX * dX + dY * dY);
        }

        public static byte[,] Convert1DTo2D(byte[] input, int width, int height, int stride)
        {
            if (input.Length != stride * height)
                throw new ArgumentException("Input array length does not match the specified dimensions.");

            byte[,] output = new byte[width, height];

            for (int y = 0; y < height; y++)
            {
                Buffer.BlockCopy(input, y * stride, output, y * width, width);
            }

            return output;
        }
        // 칩의 엣지를 검사 하는 함수를 구현 합니다.
        public void Inspection(Bitmap image, out QMC_ResultChppingNForeign resultChppingNForeign)
        {
            resultChppingNForeign = new QMC_ResultChppingNForeign();
            resultChppingNForeign.m_bChipping = false;
            resultChppingNForeign.m_bForeign = false;
            resultChppingNForeign.m_RegionChipping = new List<Region>();
            resultChppingNForeign.m_RegionForeign = new List<Region>();


            //이미지의 크기를 구합니다.
            int nWidth = image.Width;
            int nHeight = image.Height;

            //이미지의 크기가 0이면 함수를 종료 합니다.
            if (nWidth == 0 || nHeight == 0)
                return;

            //이미지의 픽셀 데이터를 가져옵니다.
            Rectangle rect = new Rectangle(0, 0, nWidth, nHeight);
            System.Drawing.Imaging.BitmapData bmpData = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, image.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            int nBytes = Math.Abs(bmpData.Stride) * nHeight;

            byte[] rgbValues = new byte[nBytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, nBytes);

            //image.Dispose();

            int stride = bmpData.Stride;

            QMC_BlobTool blobTool = new QMC_BlobTool();
            for (int iter = 0; iter < rgbValues.Length; iter++)
            {
                rgbValues[iter] = (byte)(255 - rgbValues[iter]);
            }
            //blobTool.RemoveBrightBlob(rgbValues, nWidth, nHeight, stride, m_nChippingThreshold, 100000, m_nChippingThreshold);

            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, nBytes);
            image.UnlockBits(bmpData);
            //이미지의 픽셀 데이터를 2차원 배열로 변환 합니다.
            int nOffset = bmpData.Stride - nWidth;
            //byte[,] imageArray = Convert1DTo2D(rgbValues,nWidth,nHeight,stride);
            byte[,] imageArray = new byte[nHeight, nWidth]; // [y, x] 순서
            for (int y = 0; y < nHeight; y++)
            {
                Buffer.BlockCopy(rgbValues, y * stride, imageArray, y * nWidth, nWidth);
            }

            FindChipOutline(resultChppingNForeign, nWidth, nHeight, imageArray);
            Line lineTop, lineBottom, lineLeft, lineRight;
            return;
            //이미지에서 칩의 Bottom영역을 구해 온다.


            // 칩의 엣지 부분 에서 검사 안하는 영역 좌,우,상,하를 설정 할수 있습니다.
            //nTop += m_nTopMargin;
            //nBottom -= m_nBottomMargin;
            //nLeft += m_nLeftMargin;
            //nRight -= m_nRightMargin;

            //칩의 Top 부분을 검사합니다.

            bool bIsChipping = false;
            List<Region> listRegion = new List<Region>();
            InspectTopChipping(nWidth, nHeight, imageArray, lineTop, lineBottom, lineLeft, lineRight, out bIsChipping, out listRegion);
            if (bIsChipping == true)
            {
                resultChppingNForeign.m_bChipping = true;
                foreach (var v in listRegion)
                {
                    resultChppingNForeign.m_RegionChipping.Add(v);
                }

            }

            //칩의 Bottom 부분을 검사합니다.
            InspectBottomChipping(nWidth, nHeight, imageArray, lineTop, lineBottom, lineLeft, lineRight, out bIsChipping, out listRegion);

            if (bIsChipping == true)
            {
                resultChppingNForeign.m_bChipping = true;
                foreach (var v in listRegion)
                {
                    resultChppingNForeign.m_RegionChipping.Add(v);
                }
            }

            //칩의 Left 부분을 검사합니다.

            InspectLeftChipping(nWidth, nHeight, imageArray, lineTop, lineBottom, lineLeft, lineRight, out bIsChipping, out listRegion);

            if (bIsChipping == true)
            {
                resultChppingNForeign.m_bChipping = true;
                foreach (var v in listRegion)
                {
                    resultChppingNForeign.m_RegionChipping.Add(v);
                }
            }

            //칩의 Right 부분을 검사합니다.

            InspectRightChipping(nWidth, nHeight, imageArray, lineTop, lineBottom, lineLeft, lineRight, out bIsChipping, out listRegion);

            if (bIsChipping == true)
            {
                resultChppingNForeign.m_bChipping = true;

                foreach (var v in listRegion)
                {
                    resultChppingNForeign.m_RegionChipping.Add(v);
                }
            }

            bool bIsForeign = false;
            //칩의 내부를 검사합니다.
            InspectForeign(nWidth, nHeight, imageArray, lineTop, lineBottom, lineLeft, lineRight, out bIsForeign, out listRegion);
            if (bIsForeign == true)
            {
                if (bIsForeign == true)
                {
                    resultChppingNForeign.m_bForeign = true;

                    foreach (var v in listRegion)
                    {
                        resultChppingNForeign.m_RegionForeign.Add(v);
                    }
                }
            }

        }

        private static readonly bool bProfilePhases =
            Environment.GetEnvironmentVariable("QMC_PROFILE") == "1";

        public void FindChipOutline(QMC_ResultChppingNForeign resultChppingNForeign, int nWidth, int nHeight, byte[,] imageArray, bool bWhite = true)
        {
            Line lineTop = null, lineBottom = null, lineLeft = null, lineRight = null;

            var swTB = bProfilePhases ? System.Diagnostics.Stopwatch.StartNew() : null;
            // 병렬 Task 생성
            var topTask = Task.Run(() => FindTopLineOfChip(nWidth, nHeight, imageArray, bWhite));
            var bottomTask = Task.Run(() => FindBottomLineOfChip(nWidth, nHeight, imageArray, bWhite));

            // Top/Bottom이 끝나야 Left/Right를 구할 수 있으므로, 먼저 대기
            Task.WaitAll(topTask, bottomTask);
            double msTB = swTB != null ? swTB.Elapsed.TotalMilliseconds : 0;
            if (swTB != null) swTB.Restart();

            lineTop = topTask.Result;
            lineBottom = bottomTask.Result;

            var leftTask = Task.Run(() => FindLeftLineOfChip(nWidth, nHeight, imageArray, lineTop, lineBottom, bWhite));
            var rightTask = Task.Run(() => FindRightLineOfChip(nWidth, nHeight, imageArray, lineTop, lineBottom, bWhite));

            Task.WaitAll(leftTask, rightTask);
            if (swTB != null) Console.WriteLine(string.Format("[PROF2] topbottom={0:F1} leftright={1:F1}", msTB, swTB.Elapsed.TotalMilliseconds));

            lineLeft = leftTask.Result;
            lineRight = rightTask.Result;

            resultChppingNForeign.m_lineTop = lineTop;
            resultChppingNForeign.m_lineBottom = lineBottom;
            resultChppingNForeign.m_lineLeft = lineLeft;
            resultChppingNForeign.m_lineRight = lineRight;

            PointD ptLeftTop;
            PointD ptLeftBottom;
            PointD ptRightTop;
            PointD ptRightBottom;
            lineTop.GetCrossPoint(lineLeft, out ptLeftTop);
            lineTop.GetCrossPoint(lineRight, out ptRightTop);
            lineBottom.GetCrossPoint(lineLeft, out ptLeftBottom);
            lineBottom.GetCrossPoint(lineRight, out ptRightBottom);
            // 4개 Offset 병렬 계산
            //var expleftTask = Task.Run(() => ExpendedLeft(imageArray, ptLeftTop.Y, ptLeftBottom.Y, nWidth, nHeight, lineLeft, m_nForeignThreshold));
            //var exprightTask = Task.Run(() => ExpendedRight(imageArray, ptRightTop.Y, ptRightBottom.Y, nWidth, nHeight, lineLeft, m_nForeignThreshold));
            //var exptopTask = Task.Run(() => ExpendedTop(imageArray, ptLeftTop.Y, ptRightTop.Y, nWidth, nHeight, lineLeft, m_nForeignThreshold));
            //var expbottomTask = Task.Run(() => ExpendedBottom(imageArray, ptLeftBottom.Y, ptRightBottom.Y, nWidth, nHeight, lineLeft, m_nForeignThreshold));

            //Task.WaitAll(leftTask, rightTask, topTask, bottomTask);

            //double LeftOffset = expleftTask.Result;
            //double RightOffset = exprightTask.Result;
            //double TopOffset = exptopTask.Result;
            //double BottomOffset = expbottomTask.Result;

            // 칩핑 영역을 확장합니다.
            //ptLeftTop.X -= LeftOffset;
            //ptLeftBottom.X -= LeftOffset;
            //ptRightTop.X += RightOffset;
            //ptRightBottom.X += RightOffset;
            //ptLeftTop.Y -= TopOffset;
            //ptRightTop.Y -= TopOffset;
            //ptLeftBottom.Y += BottomOffset;
            //ptRightBottom.Y += BottomOffset;

            // 칩핑 영역의 좌표를 설정합니다.

            resultChppingNForeign.LeftTop = new PointF((float)ptLeftTop.X, (float)ptLeftTop.Y);
            resultChppingNForeign.RightTop = new PointF((float)ptRightTop.X, (float)ptRightTop.Y);
            resultChppingNForeign.LeftBottom = new PointF((float)ptLeftBottom.X, (float)ptLeftBottom.Y);
            resultChppingNForeign.RightBottom = new PointF((float)ptRightBottom.X, (float)ptRightBottom.Y);

            double dScaleh = 1;
            double dScalew = 1;

            double w = GetDistance(ptLeftTop, ptRightTop) * dScalew;
            double h = GetDistance(ptLeftTop, ptLeftBottom) * dScaleh;
            double w2 = GetDistance(ptLeftBottom, ptRightBottom) * dScalew;
            double h2 = GetDistance(ptRightTop, ptRightBottom) * dScaleh;


            double hh1 = lineTop.GetDistanceToPoint(resultChppingNForeign.LeftBottom);
            double hh2 = lineTop.GetDistanceToPoint(resultChppingNForeign.RightBottom);
            double hh3 = lineBottom.GetDistanceToPoint(resultChppingNForeign.LeftTop);
            double hh4 = lineBottom.GetDistanceToPoint(resultChppingNForeign.RightTop);
            double dAvgH = (hh1 + hh2 + hh3 + hh4) / 4.0;
            resultChppingNForeign.w = (w + w2) / 2;
            resultChppingNForeign.h = (h + h2) / 2;
        }
        int nDeltaTreshold = 5;
        private double ExpendedLeft(byte[,] imageArray, double startY, double EndY, int nWidth, int nHeight, Line lineLeft, int m_nForeignThreshold)
        {
            List<Point> listPoint = new List<Point>();
            int nCount = 0;
            if (startY < 0)
                startY = 0;
            if (EndY >= nHeight)
                EndY = nHeight - 1;

            for (int y = (int)startY; y < EndY; y++)
            {
                listPoint.Add(new Point((int)lineLeft.GetX(y), y));
            }
            Point ptOffset = new Point(0, 0);
            double dValue = GetAvgImage(imageArray, listPoint, ptOffset);
            ptOffset = new Point(-1, 0);
            double dNextValue = GetAvgImage(imageArray, listPoint, ptOffset);
            double dDelta = dValue - dNextValue;
            while (dDelta > nDeltaTreshold)
            {
                nCount++;
                ptOffset.X--;
                dValue = dNextValue;
                dNextValue = GetAvgImage(imageArray, listPoint, ptOffset);
                dDelta = dValue - dNextValue;
            }
            double dOffset = nCount - (nDeltaTreshold - dDelta) / nDeltaTreshold;
            return dOffset;
        }
        private double ExpendedRight(byte[,] imageArray, double startY, double EndY, int nWidth, int nHeight, Line lineLeft, int m_nForeignThreshold)
        {
            int nCount = 0;
            List<Point> listPoint = new List<Point>();
            if (startY < 0)
                startY = 0;
            if (EndY >= nHeight)
                EndY = nHeight - 1;
            for (int y = (int)startY; y < EndY; y++)
            {
                listPoint.Add(new Point((int)lineLeft.GetX(y), y));
            }
            Point ptOffset = new Point(0, 0);
            double dValue = GetAvgImage(imageArray, listPoint, ptOffset);
            ptOffset = new Point(1, 0);
            double dNextValue = GetAvgImage(imageArray, listPoint, ptOffset);
            double dDelta = dValue - dNextValue;
            while (dDelta > nDeltaTreshold)
            {
                nCount++;
                ptOffset.X++;
                dValue = dNextValue;
                dNextValue = GetAvgImage(imageArray, listPoint, ptOffset);
                dDelta = dValue - dNextValue;
            }

            double dOffset = nCount - (nDeltaTreshold - dDelta) / nDeltaTreshold;
            return dOffset;
        }
        private double ExpendedTop(byte[,] imageArray, double startY, double EndY, int nWidth, int nHeight, Line lineLeft, int m_nForeignThreshold)
        {
            int nCount = 0;
            List<Point> listPoint = new List<Point>();
            if (startY < 0)
                startY = 0;
            if (EndY >= nHeight)
                EndY = nHeight - 1;
            for (int y = (int)startY; y < EndY; y++)
            {
                listPoint.Add(new Point((int)lineLeft.GetX(y), y));
            }
            Point ptOffset = new Point(0, 0);
            double dValue = GetAvgImage(imageArray, listPoint, ptOffset);
            ptOffset = new Point(0, -1);
            double dNextValue = GetAvgImage(imageArray, listPoint, ptOffset);
            double dDelta = dValue - dNextValue;
            while (dDelta > nDeltaTreshold)
            {
                nCount++;
                ptOffset.Y--;
                dValue = dNextValue;
                dNextValue = GetAvgImage(imageArray, listPoint, ptOffset);
                dDelta = dValue - dNextValue;
            }
            double dOffset = nCount - (nDeltaTreshold - dDelta) / nDeltaTreshold;
            return dOffset;
        }
        private double ExpendedBottom(byte[,] imageArray, double startY, double EndY, int nWidth, int nHeight, Line lineLeft, int m_nForeignThreshold)
        {
            int nCount = 0;
            List<Point> listPoint = new List<Point>();
            if (startY < 0)
                startY = 0;
            if (EndY >= nHeight)
                EndY = nHeight - 1;
            for (int y = (int)startY; y < EndY; y++)
            {
                listPoint.Add(new Point((int)lineLeft.GetX(y), y));
            }
            Point ptOffset = new Point(0, 0);
            double dValue = GetAvgImage(imageArray, listPoint, ptOffset);
            ptOffset = new Point(0, 1);
            double dNextValue = GetAvgImage(imageArray, listPoint, ptOffset);
            double dDelta = dValue - dNextValue;
            while (dDelta > nDeltaTreshold)
            {
                nCount++;
                ptOffset.Y++;
                dValue = dNextValue;
                dNextValue = GetAvgImage(imageArray, listPoint, ptOffset);
                dDelta = dValue - dNextValue;
            }
            double dOffset = nCount - (nDeltaTreshold - dDelta) / nDeltaTreshold;
            return dOffset;
        }

        // 이미지 배열에서 주어진 포인트 리스트에 해당하는 픽셀의 평균 값을 계산합니다.


        private double GetAvgImage(byte[,] imageArray, List<Point> listPoint, Point ptOffset)
        {
            double dValue = 0;
            int nCount = 0;
            int width = imageArray.GetLength(1);
            int height = imageArray.GetLength(0);

            foreach (var v in listPoint)
            {
                int nX = v.X + ptOffset.X;
                int nY = v.Y + ptOffset.Y;
                if (nX < 0 || nX >= width || nY < 0 || nY >= height)
                    continue;
                dValue += imageArray[nY, nX];
                nCount++;
            }
            if (nCount > 0)
            {
                dValue /= nCount;
            }
            else
            {
                dValue = 0;
            }
            return dValue;
        }

        // 칩의 Top 부분 Chipping 유무를 검사 하는 함수를 구현 합니다.

        private void InspectTopChipping(int nWidth, int nHeight, byte[,] imageArray, Line lineTop, Line lineBottom, Line lineLeft, Line lineRight, out bool bIsChipping, out List<Region> region)
        {
            bIsChipping = false;
            region = new List<Region>();

            List<Point> listChippingRegionPoint = new List<Point>();
            Point pointLeftTop = new Point();
            Point pointRightTop = new Point();

            lineTop.GetCrossPoint(lineLeft, out pointLeftTop);
            lineTop.GetCrossPoint(lineRight, out pointRightTop);


            //칩의 Top 부분을 검사 합니다.
            int nLeft = Math.Max(0, pointLeftTop.X + m_nLeftMargin);
            int nRight = Math.Min(nWidth - 1, pointRightTop.X - m_nRightMargin);

            for (int x = nLeft; x < nRight; x++)
            {
                bool bFind = false;
                int nChippingDepth = 0;
                int nTop = Math.Max(0, (int)lineTop.GetY(x)) + m_nTopMargin;

                for (int y = nTop; y < nTop + m_nChippingDepth * 2; y++)
                {
                    if (imageArray[y, x] <= m_nChippingThreshold)
                    {
                        nChippingDepth++;
                    }
                    else
                    {
                        if (nChippingDepth > m_nChippingDepth)
                        {
                            bFind = true;
                            listChippingRegionPoint.Add(new Point(x, y));
                        }

                        break;
                    }
                }
            }



            //Region을 구성 합니다.
            if (listChippingRegionPoint.Count > 0)
            {
                foreach (var v in listChippingRegionPoint)
                {
                    int topY = 0;
                    topY = (int)lineTop.GetY(v.X);

                    region.Add(new Region(new Rectangle(v.X - nRegionSize, topY, nRegionSize * 2, Math.Abs(topY - v.Y))));
                }
                bIsChipping = true;
            }
        }





        // 칩의 Bottom 부분 Chipping 유무를 검사 하는 함수를 구현 합니다.

        private void InspectBottomChipping(int nWidth, int nHeight, byte[,] imageArray, Line lineTop, Line lineBottom, Line lineLeft, Line lineRight, out bool bIsChipping, out List<Region> region)
        {
            bIsChipping = false;
            region = new List<Region>();

            List<Point> listChippingRegionPoint = new List<Point>();
            Point pointLeftBottom = new Point();
            Point pointRightBottom = new Point();

            lineBottom.GetCrossPoint(lineLeft, out pointLeftBottom);
            lineBottom.GetCrossPoint(lineRight, out pointRightBottom);

            // 칩의 Bottom 부분을 검사 합니다.
            int nLeft = Math.Max(0, pointLeftBottom.X + m_nLeftMargin);
            int nRight = Math.Min(nWidth - 1, pointRightBottom.X - m_nRightMargin);

            for (int x = nLeft; x < nRight; x++)
            {
                bool bFind = false;
                int nChippingDepth = 0;
                int nBottom = Math.Min(nHeight - 1, (int)lineBottom.GetY(x)) - m_nBottomMargin;

                for (int y = nBottom; y >= nBottom - m_nChippingDepth * 2; y--)
                {
                    if (imageArray[y, x] <= m_nChippingThreshold)
                    {
                        nChippingDepth++;
                    }
                    else
                    {
                        if (nChippingDepth > m_nChippingDepth)
                        {
                            bFind = true;
                            listChippingRegionPoint.Add(new Point(x, y));
                        }

                        break;
                    }
                }
            }
            //Region을 구성 합니다.
            if (listChippingRegionPoint.Count > 0)
            {
                foreach (var v in listChippingRegionPoint)
                {
                    int nBottomY = 0;
                    nBottomY = (int)lineBottom.GetY(v.X);
                    region.Add(new Region(new Rectangle(v.X - nRegionSize, v.Y, nRegionSize * 2, Math.Abs(nBottomY - v.Y))));
                }
                bIsChipping = true;
            }

        }


        // 칩의 Left 부분 Chipping 유무를 검사 하는 함수를 구현 합니다.

        private void InspectLeftChipping(int nWidth, int nHeight, byte[,] imageArray, Line lineTop, Line lineBottom, Line lineLeft, Line lineRight, out bool bIsChipping, out List<Region> region)
        {
            bIsChipping = false;
            region = new List<Region>();

            List<Point> listChippingRegionPoint = new List<Point>();
            Point pointLeftTop = new Point();
            Point pointLeftBottom = new Point();

            lineLeft.GetCrossPoint(lineTop, out pointLeftTop);
            lineLeft.GetCrossPoint(lineBottom, out pointLeftBottom);


            //칩의 Left 부분을 검사 합니다.
            int nTop = Math.Max(0, pointLeftTop.Y + m_nTopMargin);
            int nBottom = Math.Min(nHeight - 1, pointLeftBottom.Y - m_nBottomMargin);


            for (int y = nTop; y < nBottom; y++)
            {
                bool bFind = false;
                int nChippingDepth = 0;
                int nLeft = Math.Max(0, (int)lineLeft.GetX(y)) + m_nLeftMargin;

                for (int x = nLeft; x < nLeft + m_nChippingDepth * 2; x++)
                {
                    if (imageArray[y, x] <= m_nChippingThreshold)
                    {
                        nChippingDepth++;
                    }
                    else
                    {
                        if (nChippingDepth > m_nChippingDepth)
                        {
                            bFind = true;
                            listChippingRegionPoint.Add(new Point(x, y));
                        }

                        break;
                    }
                }
            }
            //Region을 구성 합니다.
            if (listChippingRegionPoint.Count > 0)
            {
                foreach (var v in listChippingRegionPoint)
                {
                    int nLeftX = 0;
                    nLeftX = (int)lineLeft.GetX(v.Y);

                    region.Add(new Region(new Rectangle(nLeftX, v.Y - nRegionSize, Math.Abs(nLeftX - v.X), nRegionSize * 2)));
                }
                bIsChipping = true;
            }
        }

        // 칩의 Right 부분 Chipping 유무를 검사 하는 함수를 구현 합니다.

        private void InspectRightChipping(int nWidth, int nHeight, byte[,] imageArray, Line lineTop, Line lineBottom, Line lineLeft, Line lineRight, out bool bIsChipping, out List<Region> region)
        {
            bIsChipping = false;
            region = new List<Region>();

            List<Point> listChippingRegionPoint = new List<Point>();
            Point pointRightTop = new Point();
            Point pointRightBottom = new Point();

            lineRight.GetCrossPoint(lineTop, out pointRightTop);
            lineRight.GetCrossPoint(lineBottom, out pointRightBottom);

            // 칩의 Right 부분을 검사 합니다.
            int nTop = Math.Max(0, pointRightTop.Y + m_nTopMargin);
            int nBottom = Math.Min(nHeight - 1, pointRightBottom.Y - m_nBottomMargin);

            for (int y = nTop; y < nBottom; y++)
            {
                bool bFind = false;
                int nChippingDepth = 0;

                int nRight = Math.Min(nWidth - 1, (int)lineRight.GetX(y)) - m_nRightMargin;

                for (int x = nRight; x >= nRight - m_nChippingDepth * 2; x--)
                {
                    if (imageArray[y, x] <= m_nChippingThreshold)
                    {
                        nChippingDepth++;
                    }
                    else
                    {
                        if (nChippingDepth > m_nChippingDepth)
                        {
                            bFind = true;
                            listChippingRegionPoint.Add(new Point(x, y));
                        }

                        break;
                    }
                }
            }
            //Region을 구성 합니다.
            if (listChippingRegionPoint.Count > 0)
            {
                foreach (var v in listChippingRegionPoint)
                {
                    int nRightX = 0;

                    nRightX = (int)lineRight.GetX(v.Y);


                    region.Add(new Region(new Rectangle(v.X, v.Y - nRegionSize, Math.Abs(nRightX - v.X), nRegionSize * 2)));
                }
                bIsChipping = true;
            }
        }

        static private bool GetMaxValue(int nCurrentVale, int nNextValue, ref int Max)
        {

            if (nCurrentVale - nNextValue <= 0)
            {
                Max = nNextValue;
                return true;
            }
            return false;
        }
        static private bool GetMinValue(int nCurrentVale, int nNextValue, ref int Min)
        {

            if (nCurrentVale - nNextValue > 0)
            {
                Min = nNextValue;
                return true;
            }

            return false;
        }
        // 칩의 Top Line을 구한다 
        public double PeekValueThreshold = 50;
        public double dFisrtPeekValueThreshold = 100;
        public int nChipOffset = 1;
        public double dStdEv = 1;
        public Line FindTopLineOfChip(int nWidth, int nHeight, byte[,] imageArray, bool bWhite,double dMagin = 0.3)
        {
            Line lineTop = null;
            var bag = new ConcurrentBag<PointF>();

            // 스캔 밴드만 전치(2026-07-12) — 접근 인덱스 y-2..y+1, y∈[10,nEndY) → 밴드 [8..nEndY].
            // 밴드가 무효(루프 자체가 안 도는 크기)면 원래도 아무 점이 안 추가되므로 스캔 전체를 생략(동일 동작).
            int nEndYBand = Math.Max(10, (int)(nHeight * dMagin - 10));
            if (nEndYBand <= 10 || nEndYBand >= nHeight)
                return FitTopLine(bag, out lineTop);
            int bandH;
            byte[] tp = TransposeBand(imageArray, nWidth, 8, nEndYBand, out bandH);

            Parallel.For(0, nWidth, ParallelOpts, x =>
            {
                try
                {
                    if (bWhite)
                    {
                        bool bMax = false;
                        int nMax = 0;
                        int nMin = 255;

                        List<PeekValueS> listPeek = new List<PeekValueS>();
                        bool bFind = false;
                        int nEndY =Math.Max(10,(int)( nHeight * dMagin - 10));
                        int cb = x * bandH - 8;   // tp[cb + y] == imageArray[y, x] (밴드 내)

                        for (int y = 10; y < nEndY; y++)
                        {

                            int nCurrentVale = tp[cb + y];
                            int nNextValue = tp[cb + y + 1];

                            if (bMax)
                            {


                                if (nCurrentVale - nNextValue <= 0)
                                {
                                    nMax = nNextValue;
                                    bMax = true;
                                }
                                else
                                {
                                    bMax = false;
                                }

                                if (bMax == false)
                                {
                                    var value = nMax - nMin;
                                    if (value > dFisrtPeekValueThreshold)
                                    {
                                        bag.Add(new PointF(x, y));
                                        bFind = true;
                                        break;
                                    }
                                    listPeek.Add(new PeekValueS(x, y, nMax - nMin));
                                }

                            }
                            else
                            {


                                if (nCurrentVale - nNextValue > 0)
                                {
                                    nMin = nNextValue;
                                    bMax = false;
                                }
                                else
                                {
                                    bMax = true;
                                }
                                //   bMax = !GetMinValue(imageArray[y, x], imageArray[y + 1, x], ref nMin);


                                if (bMax)
                                {
                                    if (listPeek.Count > 0)
                                    {
                                        var peek = listPeek[listPeek.Count - 1];
                                        peek.dValue += nMax - nMin;
                                        listPeek[listPeek.Count - 1] = peek;   // struct 재기록(값 동일)

                                    }

                                }
                            }
                            if (tp[cb + y] > m_nChippingThreshold && tp[cb + y - 1] <= m_nChippingThreshold && tp[cb + y - 2] <= m_nChippingThreshold)
                            {

                                int vi = FirstPeekOverIdx(listPeek, PeekValueThreshold);
                                if (vi >= 0)
                                {
                                    bag.Add(new PointF(listPeek[vi].nX, listPeek[vi].nY));
                                }
                                else
                                {
                                    bag.Add(new PointF(x, y + nChipOffset));
                                }
                                bFind = true;
                                break;
                            }
                        }
                        if (bFind == false)
                        {
                            int vi = FirstPeekOverIdx(listPeek, PeekValueThreshold);
                            if (vi >= 0)
                            {
                                bag.Add(new PointF(listPeek[vi].nX, listPeek[vi].nY));
                            }
                        }
                    }
                    else
                    {

                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            });

            BufferPool.Return(tp);

            return FitTopLine(bag, out lineTop);

        }

        /// <summary>Top 라인 피팅(기존 FindTopLineOfChip 말미 코드 그대로 분리 — 밴드 무효 시 조기 경로와 공용).</summary>
        private Line FitTopLine(ConcurrentBag<PointF> bag, out Line lineTop)
        {
            List<PointF> listPoint = bag.OrderBy(t => t.X).ToList();


            double dA = 0;
            double dB = 0;
            listPoint = ReMoveTopLeft(out lineTop, listPoint, out dA, out dB);

            GetTrendLine(listPoint, out dA, out dB, false);



            lineTop = new Line(dA, dB);

            return lineTop;
        }

        double dRatioFirst = 0.98;
        double dRatioSecond = 0.9;
        private List<PointF> ReMoveTopLeft(out Line line, List<PointF> listPoint, out double dA, out double dB)
        {
            var orglistPoint = listPoint;
            GetTrendLine(listPoint, out dA, out dB);
            int nTotalCount = listPoint.Count;
            line = new Line(dA, dB);
            double dStdevY = line.GetErrorY(listPoint);
            var tempList = orglistPoint.ToList();
            int retry = 0;
            while (dStdevY > dStdEv  )
            {
                if (nTotalCount * 0.05 > tempList.Count)
                    break;
                //tempList = listPoint.ToList();
                line.RemoveListYMin(dStdevY * 2, ref tempList, dRatioFirst);
                GetTrendLine(tempList, out dA, out dB, false);

                line = new Line(dA, dB);
                dStdevY = line.GetErrorY(tempList);

                line.RemoveListYMax(dStdevY * (m_dCutRatio + 0.4), ref tempList, dRatioSecond);
                GetTrendLine(tempList, out dA, out dB, false);

                line = new Line(dA, dB);
                dStdevY = line.GetErrorY(tempList);
                retry++;
                if (retry > 10)
                    break;


            }
            // double dCutRatio = 0.002;
            // LowPassFilter(tempList, 1 - dCutRatio, dCutRatio);
            return tempList;
        }



        private List<PointF> ReMoveBottomRight(out Line line, List<PointF> listPoint, out double dA, out double dB)
        {
            var orglistPoint = listPoint;
            GetTrendLine(listPoint, out dA, out dB);

            int nTotalCount = listPoint.Count;
            line = new Line(dA, dB);
            double dStdevY = line.GetErrorY(listPoint);
            var tempList = orglistPoint.ToList();
            int retry = 0;
            while (dStdevY > dStdEv)
            {
                if (nTotalCount * 0.05 > tempList.Count)
                    break;
                //tempList = listPoint.ToList();
                line.RemoveListYMax(dStdevY * 2, ref tempList, dRatioFirst);
                GetTrendLine(tempList, out dA, out dB, false);
                line = new Line(dA, dB);
                dStdevY = line.GetErrorY(tempList);
                line.RemoveListYMin(dStdevY * 2, ref tempList, dRatioSecond);
                GetTrendLine(tempList, out dA, out dB, false);
                line = new Line(dA, dB);
                dStdevY = line.GetErrorY(tempList);
                retry++;
                if (retry > 10)
                    break;

            }
           

            return tempList;
        }


        //private List<PointF> ReMoveTopLeft(out Line line, List<PointF> listPoint, out double dA, out double dB)
        //{

        //    GetTrendLine(listPoint, out dA, out dB);

        //    line = new Line(dA, dB);
        //    double dStdevY = line.GetErrorY(listPoint);
        //    var tempList = listPoint.ToList();
        //    int retry = 0;
        //    while (dStdevY > this.nStdevThreshold)
        //    {
        //        //tempList = listPoint.ToList();
        //        line.RemoveListYMin(dStdevY, ref tempList);
        //        GetTrendLine(tempList, out dA, out dB, false);
        //        line = new Line(dA, dB);
        //        dStdevY = line.GetErrorY(tempList);
        //        retry++;
        //        if (retry > 4)
        //            break;


        //    }
        //    line.RemoveListYMax(-dStdevY * m_dCutRatio, ref tempList);
        //    return tempList;
        //}

        //private List<PointF> ReMoveBottomRight(out Line line, List<PointF> listPoint, out double dA, out double dB)
        //{

        //    GetTrendLine(listPoint, out dA, out dB);

        //    line = new Line(dA, dB);
        //    double dStdevY = line.GetErrorY(listPoint);
        //    var tempList = listPoint.ToList();
        //    int retry = 0;
        //    while (dStdevY > this.nStdevThreshold)
        //    {
        //        //tempList = listPoint.ToList();
        //        line.RemoveListYMax(dStdevY, ref tempList);
        //        GetTrendLine(tempList, out dA, out dB, false);
        //        line = new Line(dA, dB);
        //        dStdevY = line.GetErrorY(tempList);
        //        retry++;
        //        if (retry > 4)
        //            break;

        //    }

        //    line.RemoveListYMin(-dStdevY * m_dCutRatio, ref tempList);
        //    return tempList;
        //}
        private void LowPassFilter(List<PointF> tempList, double v1, double v2)
        {
            if (tempList == null || tempList.Count < 2)
            {
                return;
            }

            // Forward pass
            for (int i = 1; i < tempList.Count; i++)
            {
                float newY = (float)(tempList[i - 1].Y * v1 + tempList[i].Y * v2);
                tempList[i] = new PointF(tempList[i].X, newY);
            }
            // Backward pass
            for (int i = tempList.Count - 2; i >= 0; i--)
            {
                float newY = (float)(tempList[i + 1].Y * v1 + tempList[i].Y * v2);
                tempList[i] = new PointF(tempList[i].X, newY);
            }
        }
        // 칩의 Bottom Line을 구한다.
        int nStdevThreshold = 5;
        public Line FindBottomLineOfChip(int nWidth, int nHeight, byte[,] imageArray, bool bWhite)
        {
            Line lineBottom = null;
            var bag = new ConcurrentBag<PointF>();

            // 스캔 밴드만 전치(2026-07-12) — 접근 인덱스 y-1..y+2, y∈(nH*0.7+10, nH-10] → 밴드 [(int)(nH*0.7+10)-1 .. nH-8].
            // 루프가 아예 안 도는 크기면 원래도 점이 안 추가되므로 스캔 전체를 생략(동일 동작).
            if (!(nHeight - 10 > nHeight * 0.7 + 10) || nHeight < 24)
                return FitBottomLine(bag, out lineBottom);
            int bandY0 = Math.Max(0, (int)(nHeight * 0.7 + 10) - 1);
            int bandH;
            byte[] tp = TransposeBand(imageArray, nWidth, bandY0, nHeight - 8, out bandH);

            Parallel.For(0, nWidth, ParallelOpts, x =>
            {
                try
                {
                    if (bWhite)
                    {
                        bool bMax = false;
                        int nMax = 0;
                        int nMin = 255;
                        List<PeekValueS> listPeek = new List<PeekValueS>();
                        bool bFind = false;
                        int cb = x * bandH - bandY0;   // tp[cb + y] == imageArray[y, x] (밴드 내)
                        for (int y = nHeight - 10; y > nHeight * 0.7 + 10; y--)
                        {
                            int nCurrentVale = tp[cb + y];
                            int nNextValue = tp[cb + y - 1];


                            if (bMax)
                            {


                                if (nCurrentVale - nNextValue <= 0)
                                {
                                    nMax = nNextValue;
                                    bMax = true;
                                }
                                else
                                {
                                    bMax = false;
                                }

                                if (bMax == false)
                                {
                                    var value = nMax - nMin;
                                    if (value > dFisrtPeekValueThreshold)
                                    {
                                        bag.Add(new PointF(x, y));
                                        bFind = true;
                                        break;
                                    }
                                    listPeek.Add(new PeekValueS(x, y, nMax - nMin));
                                }

                            }
                            else
                            {


                                if (nCurrentVale - nNextValue > 0)
                                {
                                    nMin = nNextValue;
                                    bMax = false;
                                }
                                else
                                {
                                    bMax = true;
                                }
                                //   bMax = !GetMinValue(imageArray[y, x], imageArray[y + 1, x], ref nMin);


                                if (bMax)
                                {
                                    if (listPeek.Count > 0)
                                    {
                                        var peek = listPeek[listPeek.Count - 1];
                                        peek.dValue += nMax - nMin;
                                        listPeek[listPeek.Count - 1] = peek;   // struct 재기록(값 동일)

                                    }

                                }
                            }
                            if (tp[cb + y] > m_nChippingThreshold && tp[cb + y + 1] <= m_nChippingThreshold && tp[cb + y + 2] <= m_nChippingThreshold)
                            {
                                int vi = FirstPeekOverIdx(listPeek, PeekValueThreshold);
                                if (vi >= 0)
                                {
                                    bag.Add(new PointF(listPeek[vi].nX, listPeek[vi].nY));
                                }
                                else
                                {
                                    bag.Add(new PointF(x, y - nChipOffset));
                                }
                                bFind = true;
                                break;
                            }
                        }
                        if (bFind == false)
                        {
                            int vi = FirstPeekOverIdx(listPeek, PeekValueThreshold);
                            if (vi >= 0)
                            {
                                bag.Add(new PointF(listPeek[vi].nX, listPeek[vi].nY));
                            }
                        }
                    }
                    else
                    {
                        bool bMin = false;
                        int nMin = 255;
                        int nMax = 0;
                        List<PeekValueS> listPeek = new List<PeekValueS>();
                        int cb = x * bandH - bandY0;   // tp[cb + y] == imageArray[y, x] (밴드 내)
                        for (int y = nHeight - 10; y > nHeight * 0.7 + 10; y--)
                        {
                            if (bMin)
                            {
                                bMin = GetMinValue(tp[cb + y], tp[cb + y - 1], ref nMin);
                                if (bMin == false)
                                {
                                    listPeek.Add(new PeekValueS(x, y, nMax - nMin));
                                }
                            }
                            else
                            {
                                bMin = !GetMaxValue(tp[cb + y], tp[cb + y - 1], ref nMax);
                                if (bMin)
                                {
                                    if (listPeek.Count > 0)
                                    {
                                        var peek = listPeek[listPeek.Count - 1];
                                        peek.dValue += nMax - nMin;
                                        listPeek[listPeek.Count - 1] = peek;   // struct 재기록(값 동일)
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            });

            BufferPool.Return(tp);

            return FitBottomLine(bag, out lineBottom);

        }

        /// <summary>Bottom 라인 피팅(기존 FindBottomLineOfChip 말미 코드 그대로 분리 — 밴드 무효 시 조기 경로와 공용).</summary>
        private Line FitBottomLine(ConcurrentBag<PointF> bag, out Line lineBottom)
        {
            List<PointF> listPoint = bag.OrderBy(t => t.X).ToList();

            double dA = 0;
            double dB = 0;


            listPoint = ReMoveBottomRight(out lineBottom, listPoint, out dA, out dB);

            GetTrendLine(listPoint, out dA, out dB, false);
            lineBottom = new Line(dA, dB);

            return lineBottom;
        }

        // 칩의 Left Line을 구한다.

        private Line FindLeftLineOfChip(int nWidth, int nHeight, byte[,] imageArray, Line lineTop, Line lineBottom, bool bWhite)
        {
            Line lineLeft = null;
            var bag = new ConcurrentBag<PointF>();
            int nTop = Math.Max(0, (int)lineTop.GetY(0)) + m_nTopMargin;
            int nTop1 = Math.Max(0, (int)lineTop.GetY(nWidth - 1)) + m_nTopMargin;

            int nBottom = Math.Min(nHeight - 1, (int)lineBottom.GetY(0)) - m_nBottomMargin;
            int nBottom2 = Math.Min(nHeight - 1, (int)lineBottom.GetY(nWidth - 1)) - m_nBottomMargin;
            nBottom = Math.Max(nBottom, nBottom2);
            nTop = Math.Min(nTop, nBottom);
            nTop = Math.Max(nTop, 0);
            nBottom = Math.Max(0, nBottom);

            // 행 포인터 직접 접근(2026-07-12) — byte[,] 인덱싱의 이중 경계검사/곱셈 제거(같은 바이트를 같은
            // 순서로 읽음 — 결과 동일). fixed 는 Parallel.For 동기 완료까지 유지된다.
            unsafe
            {
            fixed (byte* pImgF = &imageArray[0, 0])
            {
            IntPtr imgPtr = (IntPtr)pImgF;
            Parallel.For(0, nHeight, ParallelOpts, y =>
            {
                if (bWhite)
                {
                    unsafe
                    {
                    byte* prow = (byte*)imgPtr + (long)y * nWidth;
                    int nMax = 0;
                    int nMin = 255;
                    bool bMax = false;
                    // 칩의 Left 부분을 검사 합니다.
                    List<PeekValueS> listPeek = new List<PeekValueS>();
                    bool bFind = false;
                    for (int x = 10; x < nWidth * 0.3 - 10; x++)
                    {

                        int nCurrentVale = prow[x];
                        int nNextValue = prow[x + 1];

                        if (bMax)
                        {


                            if (nCurrentVale - nNextValue <= 0)
                            {
                                nMax = nNextValue;
                                bMax = true;
                            }
                            else
                            {
                                bMax = false;
                            }

                            var value = nMax - nMin;
                            if (value > dFisrtPeekValueThreshold)
                            {
                                bag.Add(new PointF(x, y));
                                bFind = true;
                                break;
                            }
                            if (bMax == false)
                            {

                                listPeek.Add(new PeekValueS(x, y, nMax - nMin));
                            }

                        }
                        else
                        {


                            if (nCurrentVale - nNextValue > 0)
                            {
                                nMin = nNextValue;
                                bMax = false;
                            }
                            else
                            {
                                bMax = true;
                            }
                            //   bMax = !GetMinValue(imageArray[y, x], imageArray[y + 1, x], ref nMin);


                            if (bMax)
                            {
                                if (listPeek.Count > 0)
                                {
                                    var peek = listPeek[listPeek.Count - 1];
                                    peek.dValue += nMax - nMin;
                                    listPeek[listPeek.Count - 1] = peek;   // struct 재기록(값 동일)

                                }

                            }
                        }
                        if (prow[x] > m_nChippingThreshold && prow[x + 1] <= m_nChippingThreshold && prow[x + 2] <= m_nChippingThreshold)
                        {
                            if (y <= nTop || y >= nBottom)
                                continue;


                            int vi = FirstPeekOverIdx(listPeek, PeekValueThreshold);
                            if (vi >= 0)
                            {
                                bag.Add(new PointF(listPeek[vi].nX, listPeek[vi].nY));
                            }
                            else
                            {
                                bag.Add(new PointF(x + nChipOffset, y));
                            }
                            bFind = true;
                            break;
                        }
                    }
                    if (bFind == false)
                    {
                        int vi = FirstPeekOverIdx(listPeek, PeekValueThreshold);
                        if (vi >= 0)
                        {
                            bag.Add(new PointF(listPeek[vi].nX, listPeek[vi].nY));
                        }
                    }

                    }
                }
                else
                {

                }
            });
            }
            }

            List<PointF> listPoint = bag.OrderBy(t => t.Y).ToList();
            for (int iter = 0; iter < listPoint.Count; iter++)
            {
                listPoint[iter] = new PointF(listPoint[iter].Y, listPoint[iter].X);
            }

            double dA = 0;
            double dB = 0;


            listPoint = ReMoveTopLeft(out lineLeft, listPoint, out dA, out dB);

            GetTrendLine(listPoint, out dA, out dB, false);

            if( dA == 0 )
            {
                lineLeft = new Line(double.PositiveInfinity, dB);
            }
            else
            {
                lineLeft = new Line(1 / dA, -dB / dA);
            }


                return lineLeft;
        }

        // 칩의 Right Line을 구한다.

        private Line FindRightLineOfChip(int nWidth, int nHeight, byte[,] imageArray, Line lineTop, Line lineBottom, bool bWhite)
        {
            Line lineRight = null;
            var bag = new ConcurrentBag<PointF>();


            int nTop = Math.Max(0, (int)lineTop.GetY(0)) + m_nTopMargin;
            int nTop1 = Math.Max(0, (int)lineTop.GetY(nWidth - 1)) + m_nTopMargin;

            int nBottom = Math.Min(nHeight - 1, (int)lineBottom.GetY(0)) - m_nBottomMargin;
            int nBottom2 = Math.Min(nHeight - 1, (int)lineBottom.GetY(nWidth - 1)) - m_nBottomMargin;
            nBottom = Math.Max(nBottom, nBottom2);
            nTop = Math.Min(nTop, nBottom);
            nTop = Math.Max(nTop, 0);
            nBottom = Math.Max(0, nBottom);


            // 행 포인터 직접 접근(2026-07-12) — Left 와 동일한 근거(같은 바이트, 같은 순서 → 결과 동일).
            unsafe
            {
            fixed (byte* pImgF = &imageArray[0, 0])
            {
            IntPtr imgPtr = (IntPtr)pImgF;
            Parallel.For(0, nHeight, ParallelOpts, y =>
            {
                if (bWhite)
                {
                    unsafe
                    {
                    byte* prow = (byte*)imgPtr + (long)y * nWidth;
                    int nMax = 0;
                    int nMin = 255;
                    bool bMax = false;
                    // 칩의 Right 부분을 검사 합니다.
                    List<PeekValueS> listPeek = new List<PeekValueS>();
                    bool bFind = false;
                    for (int x = nWidth - 10; x > nWidth * 0.7 + 10; x--)
                    {

                        int nCurrentVale = prow[x];
                        int nNextValue = prow[x - 1];

                        if (bMax)
                        {


                            if (nCurrentVale - nNextValue <= 0)
                            {
                                nMax = nNextValue;
                                bMax = true;
                            }
                            else
                            {
                                bMax = false;
                            }
                            var value = nMax - nMin;
                            if (value > dFisrtPeekValueThreshold)
                            {
                                bag.Add(new PointF(x, y));
                                bFind = true;
                                break;
                            }
                            if (bMax == false)
                            {

                                listPeek.Add(new PeekValueS(x, y, nMax - nMin));
                            }

                        }
                        else
                        {


                            if (nCurrentVale - nNextValue > 0)
                            {
                                nMin = nNextValue;
                                bMax = false;
                            }
                            else
                            {
                                bMax = true;
                            }
                            //   bMax = !GetMinValue(imageArray[y, x], imageArray[y + 1, x], ref nMin);


                            if (bMax)
                            {
                                if (listPeek.Count > 0)
                                {
                                    var peek = listPeek[listPeek.Count - 1];
                                    peek.dValue += nMax - nMin;
                                    listPeek[listPeek.Count - 1] = peek;   // struct 재기록(값 동일)

                                }

                            }
                        }
                        if (prow[x] > m_nChippingThreshold && prow[x - 1] <= m_nChippingThreshold && prow[x - 2] <= m_nChippingThreshold)
                        {
                            if (y <= nTop || y >= nBottom)
                                continue;
                            int vi = FirstPeekOverIdx(listPeek, PeekValueThreshold);
                            if (vi >= 0)
                            {
                                bag.Add(new PointF(listPeek[vi].nX, listPeek[vi].nY));
                            }
                            else
                            {
                                bag.Add(new PointF(x + nChipOffset, y));
                            }
                            bFind = true;
                            break;
                        }
                    }
                    if (bFind == false)
                    {
                        int vi = FirstPeekOverIdx(listPeek, PeekValueThreshold);
                        if (vi >= 0)
                        {
                            bag.Add(new PointF(listPeek[vi].nX, listPeek[vi].nY));
                        }
                    }
                    }
                }
                else
                {

                }

            });
            }
            }

            double dA = 0;
            double dB = 0;



            List<PointF> listPoint = bag.OrderBy(t => t.Y).ToList();
            for (int iter = 0; iter < listPoint.Count; iter++)
            {
                listPoint[iter] = new PointF(listPoint[iter].Y, listPoint[iter].X);
            }
            listPoint = ReMoveBottomRight(out lineRight, listPoint, out dA, out dB);

            GetTrendLine(listPoint, out dA, out dB, false);

            if (dA == 0)
            {
                lineRight = new Line(double.PositiveInfinity, dB);
            }
            else
            {
                lineRight = new Line(1 / dA, -dB / dA);
            }
             

            return lineRight;
        }


        // Point의 List에서 앞뒤 10개의 Point를 제외한 나머지 Point를 이용하여 추세선을 구한다.

        private void GetTrendLine(List<Point> listPoint, out double dA, out double dB)
        {
            dA = 0;
            dB = 0;
            RemovePoint(listPoint);
            int nCount = listPoint.Count;
            if (nCount < 2)
                return;

            double dSumX = 0;
            double dSumY = 0;
            double dSumXX = 0;
            double dSumXY = 0;

            foreach (var v in listPoint)
            {
                dSumX += v.X;
                dSumY += v.Y;
                dSumXX += v.X * v.X;
                dSumXY += v.X * v.Y;
            }

            dA = (nCount * dSumXY - dSumX * dSumY) / (nCount * dSumXX - dSumX * dSumX);
            dB = (dSumXX * dSumY - dSumX * dSumXY) / (nCount * dSumXX - dSumX * dSumX);
        }
        public static void GetTrendLine(List<PointF> listPoint, out double dA, out double dB, bool bRemvePoint = true)
        {
            dA = 0;
            dB = 0;

            // 원본 listPoint를 수정하지 않기 위해 복사본을 만듭니다.
            List<PointF> pointsToProcess = listPoint;

            if (bRemvePoint)
            {
                RemovePoint(pointsToProcess);
            }
            if (pointsToProcess == null || pointsToProcess.Count < 2)
                return;

            double xrange = pointsToProcess.Max(t => t.X) - pointsToProcess.Min(t => t.X);
            double yrange = pointsToProcess.Max(t => t.Y) - pointsToProcess.Min(t => t.Y);

            // 모든 점이 동일한 경우, 계산을 중단합니다.
            if (xrange == 0 && yrange == 0)
                return;

            bool bIsVertical = false;
            // 수직에 가까운 선을 처리하기 위해 X와 Y를 바꿉니다.
            if (xrange < yrange)
            {
                for (int i = 0; i < pointsToProcess.Count; i++)
                {
                    pointsToProcess[i] = new PointF(pointsToProcess[i].Y, pointsToProcess[i].X);
                }
                bIsVertical = true;
            }

            int nCount = pointsToProcess.Count;
            if (nCount < 2)
                return;

            double dSumX = 0;
            double dSumY = 0;
            double dSumXX = 0;
            double dSumXY = 0;

            foreach (var v in pointsToProcess)
            {
                dSumX += v.X;
                dSumY += v.Y;
                dSumXX += v.X * v.X;
                dSumXY += v.X * v.Y;
            }

            double denominator = (nCount * dSumXX - dSumX * dSumX);
            // 0으로 나누는 예외를 방지합니다.
            if (Math.Abs(denominator) < 1e-9)
            {
                if (bIsVertical) // 원래 수직선이었던 경우
                {
                    dA = double.PositiveInfinity;
                    dB = pointsToProcess.Average(p => p.Y); // 원래 x좌표의 평균
                }
                else // 수평선인 경우
                {
                    dA = 0;
                    dB = pointsToProcess.Average(p => p.Y);
                }
                return;
            }

            dA = (nCount * dSumXY - dSumX * dSumY) / denominator;
            dB = (dSumXX * dSumY - dSumX * dSumXY) / denominator;

            if (bIsVertical)
            {

                double myA = dA;
                double myB = dB;

                // 0으로 나누는 예외를 다시 방지합니다.
                if (Math.Abs(myA) < 1e-9)
                {
                    dA = double.PositiveInfinity;
                    dB = pointsToProcess.Average(p => p.Y); // 원래 x좌표의 평균
                }
                else
                {
                    dA = 1 / myA;
                    dB = -myB / myA;
                }
            }
        }




        // Point의 List에서 앞뒤 10개의 Point를 제외 시키는 함수를 구현한다.

        public static void RemovePoint(List<Point> listPoint)
        {
            int nCount = listPoint.Count;
            if (nCount < 2)
                return;

            int nRemoveCount = listPoint.Count / 9;
            if (nCount < nRemoveCount * 2)
                return;

            for (int i = 0; i < nRemoveCount; i++)
            {
                listPoint.RemoveAt(0);
                listPoint.RemoveAt(listPoint.Count - 1);
            }
        }
        private static void RemovePoint(List<PointF> listPoint)
        {

            int nCount = listPoint.Count;
            if (nCount < 2)
                return;

            int nRemoveCount = listPoint.Count / 20;
            if (nCount < nRemoveCount * 2)
                return;

            for (int i = 0; i < nRemoveCount; i++)
            {
                listPoint.RemoveAt(0);
                listPoint.RemoveAt(listPoint.Count - 1);
            }
        }

        // 블랍을 구하는 함수를 구현 한다.

        public void GetBlob(int nWidth, int nHeight, byte[,] imageArray, int nThreshold, out List<Region> listRegion, int nBlobCount)
        {
            listRegion = new List<Region>();
            bool[,] bVisit = new bool[nWidth, nHeight];
            for (int y = 0; y < nHeight; y++)
            {
                for (int x = 0; x < nWidth; x++)
                {
                    bVisit[y, x] = false;
                }
            }

            for (int y = 0; y < nHeight; y++)
            {
                for (int x = 0; x < nWidth; x++)

                {
                    if (bVisit[y, x] == true)
                        continue;

                    if (imageArray[y, x] > nThreshold)
                        continue;

                    List<Point> listPoint = new List<Point>();
                    GetBlobPoint(nWidth, nHeight, imageArray, x, y, nThreshold, ref bVisit, ref listPoint);
                    if (listPoint.Count > nBlobCount)
                    {
                        int nLeft = listPoint.Min(t => t.X);
                        int nRight = listPoint.Max(t => t.X);
                        int nTop = listPoint.Min(t => t.Y);
                        int nBottom = listPoint.Max(t => t.Y);

                        listRegion.Add(new Region(new Rectangle(nLeft, nTop, nRight - nLeft, nBottom - nTop)));
                    }
                }
            }
        }

        // 블랍을 구하는 함수를 구현 한다.
        void GetBlobPoint(int nWidth, int nHeight, byte[,] imageArray, int x, int y, int nThreshold, ref bool[,] bVisit, ref List<Point> listPoint)
        {
            if (x < 0 || x >= nWidth)
                return;

            if (y < 0 || y >= nHeight)
                return;

            if (bVisit[y, x] == true)
                return;

            if (imageArray[y, x] > nThreshold)
                return;

            bVisit[y, x] = true;
            listPoint.Add(new Point(x, y));

            GetBlobPoint(nWidth, nHeight, imageArray, x - 1, y, nThreshold, ref bVisit, ref listPoint);
            GetBlobPoint(nWidth, nHeight, imageArray, x + 1, y, nThreshold, ref bVisit, ref listPoint);
            GetBlobPoint(nWidth, nHeight, imageArray, x, y - 1, nThreshold, ref bVisit, ref listPoint);
            GetBlobPoint(nWidth, nHeight, imageArray, x, y + 1, nThreshold, ref bVisit, ref listPoint);
        }

        void MaskImage(int nWidth, int nHeight, byte[,] imageArray, Line lineTop, Line lineBottom, Line lineLeft, Line lineRight)
        {
            for (int x = 0; x < nWidth; x++)
            {
                for (int y = 0; y < nHeight; y++)
                {
                    if (y < lineTop.GetY(x) + m_nTopMarginForeign)
                        imageArray[y, x] = 255;

                    if (y > lineBottom.GetY(x) - m_nBottomMarginForeign)
                        imageArray[y, x] = 255;

                    if (x < lineLeft.GetX(y) + m_nLeftMarginForeign)
                        imageArray[y, x] = 255;

                    if (x > lineRight.GetX(y) - m_nRightMarginForeign)
                        imageArray[y, x] = 255;
                }
            }

            MaskRectalgeImage(nWidth, nHeight, imageArray, RectangleMask);
        }
        void MaskRectalgeImage(int nWidth, int nHeight, byte[,] imageArray, Rectangle rect)
        {
            for (int x = rect.Left; x < rect.Right; x++)
            {
                for (int y = rect.Top; y < rect.Bottom; y++)

                {
                    imageArray[y, x] = 255;
                }
            }
        }

        // 이물 검사 함수를 구현 합니다.
        public void InspectForeign(int nWidth, int nHeight, byte[,] imageArray, Line lineTop, Line lineBottom, Line lineLeft, Line lineRight, out bool bIsForeign, out List<Region> listRegion)
        {
            bIsForeign = false;
            listRegion = new List<Region>();

            List<Point> listForeignRegionPoint = new List<Point>();
            Point pointLeftTop = new Point();
            Point pointRightTop = new Point();

            MaskImage(nWidth, nHeight, imageArray, lineTop, lineBottom, lineLeft, lineRight);
            lineTop.GetCrossPoint(lineLeft, out pointLeftTop);
            lineTop.GetCrossPoint(lineRight, out pointRightTop);

            //이미지에서 이물을 검사 합니다.

            GetBlob(nWidth, nHeight, imageArray, m_nForeignThreshold, out listRegion, m_nForeignSize);
            if (listRegion.Count > 0)
            {
                bIsForeign = true;
            }

        }



        // SetChippingThreshold 함수를 구현 합니다.
        public void SetChippingThreshold(int nChippingThreshold)
        {
            m_nChippingThreshold = nChippingThreshold;
        }

        // SetForeignThreshold 함수를 구현 합니다.
        public void SetForeignThreshold(int nForeignThreshold)
        {
            m_nForeignThreshold = nForeignThreshold;
        }

        // SetChippingDepth 함수를 구현 합니다.
        public void SetChippingDepth(int nChippingDepth)
        {
            m_nChippingDepth = nChippingDepth;
        }

        // SetForeignSize 함수를 구현 합니다.
        public void SetForeignSize(int nForeignSize)
        {
            m_nForeignSize = nForeignSize;
        }


        public int GetChippingDepth() { return m_nChippingDepth; }

        public int GetForeignSize() { return m_nForeignSize; }


        public int GetChippingThreshold() { return m_nChippingThreshold; }

        public int GetForeignThreshold() { return m_nForeignThreshold; }

        //SetMargin 함수들을 구현 합니다.

        public void SetLeftMargin(int nLeftMargin) { m_nLeftMargin = nLeftMargin; }

        public void SetRightMargin(int nRightMargin) { m_nRightMargin = nRightMargin; }

        public void SetTopMargin(int nTopMargin) { m_nTopMargin = nTopMargin; }

        public void SetBottomMargin(int nBottomMargin) { m_nBottomMargin = nBottomMargin; }

        public void SetLeftMaginForeign(int nLeftMarginForeign) { m_nLeftMarginForeign = nLeftMarginForeign; }

        public void SetRightMarginForeign(int nRightMarginForeign) { m_nRightMarginForeign = nRightMarginForeign; }

        public void SetTopMarginForeign(int nTopMarginForeign) { m_nTopMarginForeign = nTopMarginForeign; }

        public void SetBottomMarginForeign(int nBottomMarginForeign) { m_nBottomMarginForeign = nBottomMarginForeign; }


        public void SetChipMargin(int nLeftMargin, int nRightMargin, int nTopMargin, int nBottomMargin)
        {
            m_nLeftMargin = nLeftMargin;
            m_nRightMargin = nRightMargin;
            m_nTopMargin = nTopMargin;
            m_nBottomMargin = nBottomMargin;
        }

        public void SetForeignMargin(int nLeftMarginForeign, int nRightMarginForeign, int nTopMarginForeign, int nBottomMarginForeign)
        {
            m_nLeftMarginForeign = nLeftMarginForeign;
            m_nRightMarginForeign = nRightMarginForeign;
            m_nTopMarginForeign = nTopMarginForeign;
            m_nBottomMarginForeign = nBottomMarginForeign;
        }


        //GetMargin 함수들을 구현 합니다.

        public int GetLeftMargin() { return m_nLeftMargin; }

        public int GetRightMargin() { return m_nRightMargin; }

        public int GetTopMargin() { return m_nTopMargin; }

        public int GetBottomMargin() { return m_nBottomMargin; }


        public int GetLeftMarginForeign() { return m_nLeftMarginForeign; }

        public int GetRightMarginForeign() { return m_nRightMarginForeign; }

        public int GetTopMarginForeign() { return m_nTopMarginForeign; }

        public int GetBottomMarginForeign() { return m_nBottomMarginForeign; }




    }
}


