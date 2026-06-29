using QMC.Common;
using QMC.Vision.Inspector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMc.Vision.Inspector
{
    public class FindLine
    {
        public double m_nStdevThreshold { get; set; }
        public double m_dStdEv { get; set; }
        public double m_dCutRatio { get; set; }
        public int m_nChippingThreshold { get; set; }
        public double PeekValueThreshold { get; set; }
        public int nChipOffset { get; set; }
        public double dFirtPeekValueThreshold { get; set; }
        public int m_nTopMargin { get; set; }
        public int m_nBottomMargin { get; set; }

        // Add configuration for outer-edge fitting
        public double TopKeepQuantile { get; set; } = 0.35; // keep lower residuals for Top/Left (Bottom uses1-Top)
        public int EnvelopeBinSize { get; set; } = 6; // x-binning for envelope extraction
        public bool UseOuterEnvelope { get; set; } = true; // optional outer-envelope anchoring
        public bool UseQuantileTrimming { get; set; } = true; // optional robust trimming
        public int FallbackGradientMin { get; set; } = 8; // minimal gradient for fallback pick

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
        private static double Clamp01(double value)
        {
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }


        public Line FindTopLineOfChip(int nWidth, int nHeight, byte[,] imageArray, bool bWhite,double ScanRate = 0.3)
        {
            Line lineTop = null;
            var bag = new ConcurrentBag<PointF>();
            
            if (ScanRate < 0.3 )
            {
                ScanRate = 0.3;
            }

            if(ScanRate > 1)
            {
                ScanRate = 0.99;
            }
            Parallel.For(0, nWidth, x =>
            {
                try
                {
                    if (bWhite)
                    {
                        int start = Math.Max(2, 10);
                        int end = Math.Min(nHeight - 3, (int)(nHeight * ScanRate) - 10);
                        for (int y = start; y <= end; y++)
                        {
                            if (imageArray[y, x] > m_nChippingThreshold &&
                                imageArray[y - 1, x] <= m_nChippingThreshold &&
                                imageArray[y - 2, x] <= m_nChippingThreshold)
                            {
                                bag.Add(new PointF(x, y));
                                break; // first crossing is outer edge
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            });

            var original = bag.OrderBy(t => t.X).ToList();
            List<PointF> listPoint = new List<PointF>(original);

            double dA = 0; double dB = 0;
            if (UseOuterEnvelope && listPoint.Count > 0)
            {
                var env = KeepOuterPerBin(listPoint, EnvelopeBinSize, takeMinY: true);
                if (env != null && env.Count >= 2)
                    listPoint = env;
            }

            TopKeepQuantile = Clamp01(TopKeepQuantile);
            if (UseQuantileTrimming)
            {
                var trimmed = ReMoveTop(out lineTop, listPoint, out dA, out dB);
                if (trimmed == null || trimmed.Count < 2)
                {
                    // fallback: try without trimming
                    listPoint = original;
                    GetTrendLine(listPoint, out dA, out dB, false);
                    lineTop = new Line(dA, dB);
                }
                else
                {
                    listPoint = trimmed;
                    GetTrendLine(listPoint, out dA, out dB, false);
                    lineTop = new Line(dA, dB);
                }
            }
            else
            {
                if (listPoint.Count < 2) listPoint = original;
                GetTrendLine(listPoint, out dA, out dB, false);
                lineTop = new Line(dA, dB);
            }

            return lineTop;
        }
        public Line FindBottomLineOfChip(int nWidth, int nHeight, byte[,] imageArray, bool bWhite)
        {
            Line lineBottom = null;
            var bag = new ConcurrentBag<PointF>();

            Parallel.For(0, nWidth, x =>
            {
                try
                {
                    if (bWhite)
                    {
                        int start = Math.Min(nHeight - 3, nHeight - 10);
                        int end = Math.Max(2, (int)(nHeight * 0.7) + 10);
                        for (int y = start; y >= end; y--)
                        {
                            if (imageArray[y, x] > m_nChippingThreshold &&
                                imageArray[y + 1, x] <= m_nChippingThreshold &&
                                imageArray[y + 2, x] <= m_nChippingThreshold)
                            {
                                bag.Add(new PointF(x, y));
                                break; // first crossing is outer edge
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            });

            var original = bag.OrderBy(t => t.X).ToList();
            if (original.Count < 2) return null;
            List<PointF> listPoint = new List<PointF>(original);

            double dA = 0; double dB = 0;
            if (UseOuterEnvelope && listPoint.Count > 0)
            {
                var env = KeepOuterPerBin(listPoint, EnvelopeBinSize, takeMinY: false);
                if (env != null && env.Count >= 2)
                    listPoint = env;
            }

            TopKeepQuantile = Clamp01(TopKeepQuantile);
            if (UseQuantileTrimming)
            {
                var trimmed = ReMoveBottomRight(out lineBottom, listPoint, out dA, out dB);
                if (trimmed == null || trimmed.Count < 2)
                {
                    listPoint = original;
                    GetTrendLine(listPoint, out dA, out dB, false);
                    lineBottom = new Line(dA, dB);
                }
                else
                {
                    listPoint = trimmed;
                    GetTrendLine(listPoint, out dA, out dB, false);
                    lineBottom = new Line(dA, dB);
                }
            }
            else
            {
                if (listPoint.Count < 2) listPoint = original;
                GetTrendLine(listPoint, out dA, out dB, false);
                lineBottom = new Line(dA, dB);
            }

            return lineBottom;

        }


        public Line FindLeftLineOfChip(int nWidth, int nHeight, byte[,] imageArray, Line lineTop, Line lineBottom, bool bWhite)
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



            Parallel.For(0, nHeight, y =>
            {
                if (bWhite)
                {
                    int nMax = 0;
                    int nMin = 255;
                    int nMaxX = 0;  // Peak의 실제 X 위치를 저장
                    bool bMax = false;
                    // 칩의 Left 부분을 검사 입니다.
                    List<PeekValue> listPeek = new List<PeekValue>();
                    bool bFind = false;
                    int end = (int)(nWidth * 0.3 - 10);
                    for (int x = 10; x < end; x++)
                    {

                        int nCurrentVale = imageArray[y, x];
                        int nNextValue = imageArray[y, x + 1];

                        if (bMax)
                        {
                            if (nCurrentVale - nNextValue <= 0)
                            {
                                nMax = nNextValue;
                                nMaxX = x + 1;  // 최대값 위치 업데이트
                                bMax = true;
                            }
                            else
                            {
                                bMax = false;
                            }

                            var value = nMax - nMin;
                            if (value > dFirtPeekValueThreshold)
                            {
                                bag.Add(new PointF(nMaxX, y));  // ✅ Peak의 실제 위치 사용
                                bFind = true;
                                break;
                            }
                            if (bMax == false)
                            {
                                listPeek.Add(new PeekValue(nMaxX, y, nMax - nMin));  // ✅ Peak 위치 저장
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
                                nMax = nCurrentVale;
                                nMaxX = x;  // 최대값 위치 초기화
                                bMax = true;
                            }

                            if (bMax)
                            {
                                if (listPeek.Count > 0)
                                {
                                    var peek = listPeek[listPeek.Count - 1];
                                    peek.dValue += nMax - nMin;
                                }
                            }
                        }

                        if (imageArray[y, x] > m_nChippingThreshold
                            && imageArray[y, x - 1] <= m_nChippingThreshold
                     && imageArray[y, x - 2] <= m_nChippingThreshold)
                        {
                            if (y <= nTop || y >= nBottom)
                                continue;

                            var v = listPeek.Where(t => t.dValue > PeekValueThreshold).OrderBy(t => t.nX).FirstOrDefault();
                            if (v != null && v.dValue > PeekValueThreshold)
                            {
                                bag.Add(new PointF(v.nX, v.nY));
                            }
                            else
                            {
                                bag.Add(new PointF(x, y));  // ✅ 정확한 경계 위치 사용
                            }
                            bFind = true;
                            break;
                        }
                    }
                    if (bFind == false)
                    {
                        var v = listPeek.Where(t => t.dValue > PeekValueThreshold).OrderBy(t => t.nX).FirstOrDefault();
                        if (v != null && v.dValue > PeekValueThreshold)
                        {
                            bag.Add(new PointF(v.nX, v.nY));
                        }
                    }
                }
                else
                {

                }
            });

            List<PointF> listPoint = bag.OrderBy(t => t.Y).ToList();
            for (int iter = 0; iter < listPoint.Count; iter++)
            {
                listPoint[iter] = new PointF(listPoint[iter].Y, listPoint[iter].X);
            }

            double dA = 0;
            double dB = 0;

            listPoint = ReMoveLeft(out lineLeft, listPoint, out dA, out dB);  // ✅ ReMoveLeft 호출
            if (listPoint.Count < 2) return null;

            GetTrendLine(listPoint, out dA, out dB, false);
            if(dA == 0)
            {
                lineLeft = new Line(double.PositiveInfinity, dB);
            }
            else
            {

                lineLeft = new Line(1 / dA, -dB / dA);
            }

            return lineLeft;
        }
        public Line FindRightLineOfChip(int nWidth, int nHeight, byte[,] imageArray, Line lineTop, Line lineBottom, bool bWhite)
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


            Parallel.For(0, nHeight, y =>
            {
                if (bWhite)
                {
                    int nMax = 0;
                    int nMin = 255;
                    int nMaxX = 0;  // Peak의 실제 X 위치를 저장
                    bool bMax = false;
                    // 칩의 Right 부분을 검사 합니다.
                    List<PeekValue> listPeek = new List<PeekValue>();
                    bool bFind = false;
                    int end = (int)(nWidth * 0.7 + 10);
                    for (int x = nWidth - 10; x > end; x--)
                    {

                        int nCurrentVale = imageArray[y, x];
                        int nNextValue = imageArray[y, x - 1];

                        if (bMax)
                        {
                            if (nCurrentVale - nNextValue <= 0)
                            {
                                nMax = nNextValue;
                                nMaxX = x - 1;  // 최대값 위치 업데이트
                                bMax = true;
                            }
                            else
                            {
                                bMax = false;
                            }

                            var value = nMax - nMin;
                            if (value > dFirtPeekValueThreshold)
                            {
                                bag.Add(new PointF(nMaxX, y));  // ✅ Peak의 실제 위치 사용
                                bFind = true;
                                break;
                            }
                            if (bMax == false)
                            {
                                listPeek.Add(new PeekValue(nMaxX, y, nMax - nMin));  // ✅ Peak 위치 저장
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
                                nMax = nCurrentVale;
                                nMaxX = x;  // 최대값 위치 초기화
                                bMax = true;
                            }

                            if (bMax)
                            {
                                if (listPeek.Count > 0)
                                {
                                    var peek = listPeek[listPeek.Count - 1];
                                    peek.dValue += nMax - nMin;
                                }
                            }
                        }

                        if (imageArray[y, x] > m_nChippingThreshold
                       && imageArray[y, x - 1] <= m_nChippingThreshold
                   && imageArray[y, x - 2] <= m_nChippingThreshold)
                        {
                            if (y <= nTop || y >= nBottom)
                                continue;
                            var v = listPeek.Where(t => t.dValue > PeekValueThreshold).OrderByDescending(t => t.nX).FirstOrDefault();
                            if (v != null && v.dValue > PeekValueThreshold)
                            {
                                bag.Add(new PointF(v.nX, v.nY));
                            }
                            else
                            {
                                bag.Add(new PointF(x, y));  // ✅ 정확한 경계 위치 사용
                            }
                            bFind = true;
                            break;
                        }
                    }
                    if (bFind == false)
                    {
                        var v = listPeek.Where(t => t.dValue > PeekValueThreshold).OrderByDescending(t => t.nX).FirstOrDefault();
                        if (v != null && v.dValue > PeekValueThreshold)
                        {
                            bag.Add(new PointF(v.nX, v.nY));
                        }
                    }
                }
                else
                {

                }
            });

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


        // Helper to compute percentile of a list of doubles
        private static double Percentile(List<double> values, double p)
        {
            if (values == null || values.Count == 0) return 0;
            if (p <= 0) return values.Min();
            if (p >= 1) return values.Max();
            var sorted = values.OrderBy(v => v).ToList();
            double pos = p * (sorted.Count - 1);
            int idx = (int)Math.Floor(pos);
            double frac = pos - idx;
            if (idx + 1 < sorted.Count)
                return sorted[idx] * (1 - frac) + sorted[idx + 1] * frac;
            return sorted[idx];
        }

        private List<PointF> ReMoveTop(out Line line, List<PointF> listPoint, out double dA, out double dB)
        {
            var temp = listPoint?.ToList() ?? new List<PointF>();
            if (temp.Count < 2)
            {
                dA = 0; dB = 0; line = new Line(0, 0); return listPoint;
            }
            line = new Line(0, 0);
            dA = 0; dB = 0;

            GetTrendLine(temp, out dA, out dB, false);
            double aLocal = dA, bLocal = dB;
            var residuals = temp.Select(p => p.Y - (aLocal * p.X + bLocal)).ToList();
            double cut = Percentile(residuals, 0.02);
            temp = temp.Where(p => (p.Y - (aLocal * p.X + bLocal)) >= cut).ToList();


            for (int iter = 0; iter < 3; iter++)
            {
                if (temp.Count < 2) break;
                GetTrendLine(temp, out dA, out dB, false);
                aLocal = dA;
                bLocal = dB;
                residuals = temp.Select(p => p.Y - (aLocal * p.X + bLocal)).ToList();
                cut = Percentile(residuals, TopKeepQuantile);
                var next = temp.Where(p => (p.Y - (aLocal * p.X + bLocal)) <= cut).ToList();
                if (next.Count < 2) break;
                temp = next;
            }
            GetTrendLine(temp, out dA, out dB, false);
            line = new Line(dA, dB);
            return temp;
        }

        private List<PointF> ReMoveLeft(out Line line, List<PointF> listPoint, out double dA, out double dB)
        {
            var temp = listPoint.ToList();
            line = new Line(0, 0);
            dA = 0; dB = 0;
            GetTrendLine(temp, out dA, out dB, false);
            double aLocal = dA, bLocal = dB;
            var residuals = temp.Select(p => p.Y - (aLocal * p.X + bLocal)).ToList();
            double cut = Percentile(residuals, 0.02); // configurable
            temp = temp.Where(p => (p.Y - (aLocal * p.X + bLocal)) >= cut).ToList();

            for (int iter = 0; iter < 3; iter++)
            {
                if (temp.Count < 2) break;
                GetTrendLine(temp, out dA, out dB, false);
                aLocal = dA;
                bLocal = dB;
                residuals = temp.Select(p => p.Y - (aLocal * p.X + bLocal)).ToList();
                cut = Percentile(residuals, TopKeepQuantile); // configurable
                temp = temp.Where(p => (p.Y - (aLocal * p.X + bLocal)) <= cut).ToList();
            }
            GetTrendLine(temp, out dA, out dB, false);
            line = new Line(dA, dB);
            return temp;
        }

        private List<PointF> ReMoveBottomRight(out Line line, List<PointF> listPoint, out double dA, out double dB)
        {
            var temp = listPoint?.ToList() ?? new List<PointF>();
            if (temp.Count < 2)
            {
                dA = 0; dB = 0; line = new Line(0, 0); return listPoint;
            }
            line = new Line(0, 0);
            dA = 0; dB = 0;

            GetTrendLine(temp, out dA, out dB, false);
            double aLocal = dA, bLocal = dB;
            var residuals = temp.Select(p => p.Y - (aLocal * p.X + bLocal)).ToList();
            double keepQ = 1 - Clamp01(TopKeepQuantile);
            double cut = Percentile(residuals, 0.98);
            temp = temp.Where(p => (p.Y - (aLocal * p.X + bLocal)) <= cut).ToList();


            for (int iter = 0; iter < 3; iter++)
            {
                if (temp.Count < 2) break;
                GetTrendLine(temp, out dA, out dB, false);
                aLocal = dA;
                bLocal = dB;
                residuals = temp.Select(p => p.Y - (aLocal * p.X + bLocal)).ToList();
                keepQ = 1 - Clamp01(TopKeepQuantile);
                cut = Percentile(residuals, keepQ);
                var next = temp.Where(p => (p.Y - (aLocal * p.X + bLocal)) >= cut).ToList();
                if (next.Count < 2) break;
                temp = next;
            }
            GetTrendLine(temp, out dA, out dB, false);
            line = new Line(dA, dB);
            return temp;
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
        public static void GetTrendLine(List<PointF> listPoint, out double dA, out double dB, bool bRemvePoint = true)
        {
            dA = 0;
            dB = 0;

            // 원본 listPoint를 수정하지 않기 위해 복사본을 만듭니다.
            List<PointF> pointsToProcess = new List<PointF>(listPoint);

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

        private static List<PointF> KeepOuterPerBin(List<PointF> points, int binSize, bool takeMinY)
        {
            if (points == null || points.Count == 0) return new List<PointF>();
            if (binSize < 1) binSize = 1;
            var result = new List<PointF>();
            var grouped = points.GroupBy(p => (int)(p.X / binSize));
            foreach (var g in grouped)
            {
                PointF chosen;
                if (takeMinY)
                    chosen = g.OrderBy(v => v.Y).First();
                else
                    chosen = g.OrderByDescending(v => v.Y).First();
                result.Add(chosen);
            }
            result.Sort((a, b) => a.X.CompareTo(b.X));
            return result;
        }

    }
}
