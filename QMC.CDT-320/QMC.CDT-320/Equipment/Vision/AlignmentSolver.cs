using System;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// 3 점 비전 ↔ 모터 좌표 매칭으로 어파인 매트릭스 산출.
    /// 310 의 DieTapeFrameAlignment + VisionCorrelation 동등 기능.
    /// </summary>
    public static class AlignmentSolver
    {
        /// <summary>
        /// 3 점 (px,py) ↔ (mx,my) 쌍으로 어파인 변환 mat (a,b,c,d,tx,ty) 산출.
        /// 수식:
        ///   mx = a*px + b*py + tx
        ///   my = c*px + d*py + ty
        /// 3 점 → 6 미지수 → 결정계.
        /// </summary>
        public static CoordinateMap Solve3Point(
            double[] pixelX, double[] pixelY,
            double[] motorX, double[] motorY,
            out string error)
        {
            error = null;
            if (pixelX == null || pixelX.Length < 3 ||
                pixelY == null || pixelY.Length < 3 ||
                motorX == null || motorX.Length < 3 ||
                motorY == null || motorY.Length < 3)
            { error = "need 3 points"; return null; }

            // 두 개의 3x3 선형계: M = [px py 1] · [a b tx]^T
            //                    Mp = [px py 1] · [c d ty]^T
            double det = Determinant3(
                pixelX[0], pixelY[0], 1,
                pixelX[1], pixelY[1], 1,
                pixelX[2], pixelY[2], 1);
            if (Math.Abs(det) < 1e-9) { error = "collinear pixel points"; return null; }
            double inv = 1.0 / det;

            // a, b, tx (mx 식 — Cramer's rule)
            double a  = inv * Determinant3(motorX[0], pixelY[0], 1, motorX[1], pixelY[1], 1, motorX[2], pixelY[2], 1);
            double b  = inv * Determinant3(pixelX[0], motorX[0], 1, pixelX[1], motorX[1], 1, pixelX[2], motorX[2], 1);
            double tx = inv * Determinant3(pixelX[0], pixelY[0], motorX[0], pixelX[1], pixelY[1], motorX[1], pixelX[2], pixelY[2], motorX[2]);

            // c, d, ty (my 식)
            double c  = inv * Determinant3(motorY[0], pixelY[0], 1, motorY[1], pixelY[1], 1, motorY[2], pixelY[2], 1);
            double d  = inv * Determinant3(pixelX[0], motorY[0], 1, pixelX[1], motorY[1], 1, pixelX[2], motorY[2], 1);
            double ty = inv * Determinant3(pixelX[0], pixelY[0], motorY[0], pixelX[1], pixelY[1], motorY[1], pixelX[2], pixelY[2], motorY[2]);

            return new CoordinateMap(a, b, c, d, tx, ty);
        }

        /// <summary>
        /// 3점에서 추가 정보: 회전각/스케일 추출 (Cognex CogCalibAffine 와 동등).
        /// </summary>
        public static (double scaleX, double scaleY, double rotationDeg) ExtractRotationScale(CoordinateMap m)
        {
            if (m == null) return (1, 1, 0);
            double sx = Math.Sqrt(m.A * m.A + m.C * m.C);
            double sy = Math.Sqrt(m.B * m.B + m.D * m.D);
            double rot = Math.Atan2(m.C, m.A) * 180.0 / Math.PI;
            return (sx, sy, rot);
        }

        private static double Determinant3(
            double a11, double a12, double a13,
            double a21, double a22, double a23,
            double a31, double a32, double a33)
        {
            return a11 * (a22 * a33 - a23 * a32)
                 - a12 * (a21 * a33 - a23 * a31)
                 + a13 * (a21 * a32 - a22 * a31);
        }
    }
}
