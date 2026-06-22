using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 다이 격자 전체의 평균 회전각(θ) 추정기 — AlignDie 의 Multi(전체) 검출 모드용.
    /// 격자는 90° 대칭(수평·수직 변)이므로 에지 그라디언트 방향을 4배각(4θ) 위상으로 누적해
    /// 두 변 군집이 같은 위상으로 보강되게 한 뒤 평균을 4로 나눠 [-45°,45°) 대표각을 얻는다.
    /// 작은 웨이퍼 정렬각(±수 도) 추정에 적합. 외부 라이브러리 불요(Bitmap LockBits + Sobel).
    /// </summary>
    public static class AlignAngleEstimator
    {
        /// <summary>격자 평균각(deg)을 추정한다. 강한 에지가 충분하면 true.</summary>
        /// <param name="src">입력 이미지</param>
        /// <param name="angleDeg">추정 각도(deg, 시계반대 양수, 약 [-45,45))</param>
        /// <param name="magThreshold">에지로 인정할 그라디언트 최소 크기(|gx|+|gy|)</param>
        public static bool TryEstimate(Bitmap src, out double angleDeg, int magThreshold = 36)
        {
            angleDeg = 0;
            if (src == null) return false;

            int w = src.Width, h = src.Height;
            if (w < 8 || h < 8) return false;

            // 32bpp 로 정규화해 픽셀 접근.
            Bitmap bmp = src;
            bool owned = false;
            if (src.PixelFormat != PixelFormat.Format32bppArgb)
            {
                bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp)) g.DrawImage(src, 0, 0, w, h);
                owned = true;
            }

            byte[] gray = new byte[w * h];
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* basePtr = (byte*)bd.Scan0;
                    int stride = bd.Stride;
                    for (int y = 0; y < h; y++)
                    {
                        byte* row = basePtr + y * stride;
                        int o = y * w;
                        for (int x = 0; x < w; x++)
                        {
                            byte* p = row + x * 4;     // BGRA
                            // 휘도(정수 근사): (R*77 + G*150 + B*29) >> 8
                            gray[o + x] = (byte)((p[2] * 77 + p[1] * 150 + p[0] * 29) >> 8);
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
                if (owned) bmp.Dispose();
            }

            // Sobel + 4θ 위상 누적(크기 가중).
            double sumC = 0, sumS = 0;
            long count = 0;
            for (int y = 1; y < h - 1; y++)
            {
                int o = y * w;
                for (int x = 1; x < w - 1; x++)
                {
                    int i = o + x;
                    int tl = gray[i - w - 1], tc = gray[i - w], tr = gray[i - w + 1];
                    int ml = gray[i - 1],                      mr = gray[i + 1];
                    int bl = gray[i + w - 1], bc = gray[i + w], br = gray[i + w + 1];

                    int gx = (tr + 2 * mr + br) - (tl + 2 * ml + bl);
                    int gy = (bl + 2 * bc + br) - (tl + 2 * tc + tr);
                    int mag = Math.Abs(gx) + Math.Abs(gy);
                    if (mag < magThreshold) continue;

                    double ang = Math.Atan2(gy, gx);   // 그라디언트 방향
                    sumC += mag * Math.Cos(4.0 * ang);
                    sumS += mag * Math.Sin(4.0 * ang);
                    count++;
                }
            }

            if (count < 50) return false;                 // 에지 부족 — 신뢰 불가
            double phase = Math.Atan2(sumS, sumC);        // 4θ 평균 위상
            angleDeg = phase * (180.0 / Math.PI) / 4.0;   // → θ(deg), 약 [-45,45)
            return true;
        }
    }
}
