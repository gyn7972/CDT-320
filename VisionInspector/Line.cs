using QMc.Vision.Inspector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;


namespace QMC.Vision.Inspector
{
    public class PointScore
    {
        public PointF pt;
        public double Score = 0;
    }
    /// <summary>
    /// 직선 방정식 및 교점, 오차 계산 기능 제공
    /// </summary>
    public class Line
    {
        public double mA = 0;
        public double mB = 0;

        public Line(double dA, double dB)
        {
            mA = dA;
            mB = dB;
        }

        public Line()
        {
            mA = 0;
            mB = 0;
        }
        public Line(PointF point1, PointF point2)
        {
            if (point1.X == point2.X)
            {
                // 수직선 처리
                mA = double.PositiveInfinity; // 기울기를 무한대로 설정
                mB = point1.X; // X 좌표를 y절편 대신 저장
            }
            else if (point1.Y == point2.Y)
            {
                // 수평선 처리
                mA = 0; // 기울기를 0으로 설정
                mB = point1.Y; // Y 좌표를 y절편으로 설정
            }
            else
            {
                // 일반적인 경우
                mA = (point2.Y - point1.Y) / (point2.X - point1.X); // 기울기 계산
                mB = point1.Y - mA * point1.X; // y절편 계산
            }
        }
        public Line(List<PointF> listPoint)
        {
            FindLine.GetTrendLine(listPoint, out mA, out mB);


        }
        public double GetAngle()
        {
            //atan2 로 각도 계산

            return Math.Atan2(mA, 1) * (180 / Math.PI); // 라디안 -> 도 변환
        }
        public double GetY(double x)
        {
            return mA * x + mB;
        }

        public double GetX(double y)
        {
            if(mA == double.PositiveInfinity)
            {
                return y;
            }
            return (y - mB) / mA;
        }

        public bool GetCrossPoint(Line line, out Point point)
        {
            if (line == null)
            {
                point = new Point(0, 0);
                return false;
            }
            double dX, dY;
            if (!GetCrossPoint(line, out dX, out dY))
            {
                point = new Point(0, 0);
                return false; // 교점이 존재하지 않음
            }
            point = new Point((int)dX, (int)dY);
            return true; // 교점이 존재함

        }
        public bool GetCrossPoint(Line line, out PointD point)
        {
            
            double dX, dY;
            if (!GetCrossPoint(line, out dX, out dY))
            {
                point = new PointD(0, 0);
                return false; // 교점이 존재하지 않음
            }
            point = new PointD(dX, dY);
            return true; // 교점이 존재함
        }

        public bool GetCrossPoint(Line line, out PointF pointF)
        {

            double dX, dY;
            if (!GetCrossPoint(line, out dX, out dY))
            {
                pointF = new PointF(0, 0);
                return false; // 교점이 존재하지 않음
            }
            pointF = new PointF((float)dX, (float)dY);
            return true; // 교점이 존재함
        }

        public bool GetCrossPoint(Line line, out double dX, out double dY)
        {
            dX = 0;
            dY = 0;

            // 두 직선의 기울기가 같으면 평행하므로 교점이 없음 (동일한 직선 포함)
            if (mA == line.mA)
                return false;

            // Case 1: 현재 직선이 수직선일 경우 (x = mB)
            if (double.IsPositiveInfinity(mA))
            {
                dX = mB;
                // 상대방 직선이 수평선이면 y = line.mB
                if (line.mA == 0)
                {
                    dY = line.mB;
                }
                // 상대방 직선이 일반 직선이면 y = line.mA * x + line.mB
                else
                {
                    dY = line.mA * dX + line.mB;
                }
                return true;
            }

            // Case 2: 상대방 직선이 수직선일 경우 (x = line.mB)
            if (double.IsPositiveInfinity(line.mA))
            {
                dX = line.mB;
                // 현재 직선이 수평선이면 y = mB
                if (mA == 0)
                {
                    dY = mB;
                }
                // 현재 직선이 일반 직선이면 y = mA * x + mB
                else
                {
                    dY = mA * dX + mB;
                }
                return true;
            }

            // Case 3: 현재 직선이 수평선일 경우 (y = mB)
            if (mA == 0)
            {
                dY = mB;
                // 상대방 직선은 일반 직선 (수직, 수평, 평행은 위에서 처리됨)
                dX = (dY - line.mB) / line.mA;
                return true;
            }

            // Case 4: 상대방 직선이 수평선일 경우 (y = line.mB)
            if (line.mA == 0)
            {
                dY = line.mB;
                // 현재 직선은 일반 직선 (수직, 수평, 평행은 위에서 처리됨)
                dX = (dY - mB) / mA;
                return true;
            }

            // Case 5: 두 직선 모두 일반적인 경우
            dX = (line.mB - mB) / (mA - line.mA);
            dY = mA * dX + mB;
            return true;
        }

        public double GetErrorX(List<PointF> listPoint)
        {
            double dError = 0;
            foreach (var v in listPoint)
            {
                dError += Math.Pow(Math.Abs(v.X - GetX(v.Y)), 2);
            }
            return Math.Sqrt(dError / listPoint.Count);
        }

        public double GetErrorY(List<PointF> listPoint)
        {
            double dError = 0;
            foreach (var v in listPoint)
            {
                double dRef = GetY(v.X);
                dError += Math.Pow(Math.Abs(v.Y - dRef), 2);
            }
            return Math.Sqrt(dError / listPoint.Count);
        }

        public void RemoveListYMin(double dSigma, ref List<PointF> listPoint,double dRatio = 0.8)
        {
            var vList = listPoint;
            List<PointF> listPointNew = new List<PointF>();
            List<PointScore> pointScores = new List<PointScore>();
            foreach (var v in vList)
            {
                double dY = GetY(v.X);
                PointScore ps = new PointScore();
                ps.pt = v;
                ps.Score = dY + v.Y;
                pointScores.Add(ps);
            }
            if (pointScores.Count < 10)
            {
                return;
            }
            listPoint = pointScores.OrderByDescending(t => t.Score).Select(t => t.pt).Take((int)(pointScores.Count * dRatio)).ToList();

        }

        public void RemoveListYMax(double dSigma, ref List<PointF> listPoint, double dRatio = 0.8)
        {
           // return;
            // 아래 코드는 실제 동작하지 않음 (return 때문에)
            
            var vList = listPoint;
            List<PointF> listPointNew = new List<PointF>();
            List<PointScore> pointScores = new List<PointScore>();
            foreach (var v in vList)
            {
                double dY = GetY(v.X) ;
                PointScore ps= new PointScore();
                ps.pt = v;
                ps.Score = dY - v.Y;
                pointScores.Add(ps);
            }
            if(pointScores.Count < 10)
            {
                return;
            }
            listPoint = pointScores.OrderByDescending(t=>t.Score).Select(t=>t.pt).Take((int)(pointScores.Count* dRatio)).ToList();
            
        }

        public double GetDistanceToPoint(PointF point)
        {
            // Case 1: 수직선일 경우 (x = mB)
            if (double.IsPositiveInfinity(mA))
            {
                return Math.Abs(point.X - mB);
            }
            // Case 2: 수평선일 경우 (y = mB)
            else if (mA == 0)
            {
                return Math.Abs(point.Y - mB);
            }
            // Case 3: 일반적인 직선일 경우 (y = mA*x + mB  => mA*x - y + mB = 0)
            // 점과 직선 사이의 거리 공식을 사용합니다.
            else
            {
                return Math.Abs(mA * point.X - point.Y + mB) / Math.Sqrt(mA * mA + 1);
            }
        }
    }
}