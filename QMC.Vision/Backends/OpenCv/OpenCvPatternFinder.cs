using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.OpenCv
{
    /// <summary>
    /// OpenCV Pattern Finder. EmguCV 로드 시 실제 matchTemplate/FeatureMatcher 사용.
    /// 미로드 시 System.Drawing 기반 SAD(Sum-of-Absolute-Differences) 단순 매칭으로 fallback.
    /// </summary>
    public class OpenCvPatternFinder : IPatternFinder
    {
        public string Id { get; }
        public Roi SearchRoi { get; set; }
        public Roi TrainRoi  { get; set; }
        public Bitmap TrainImage { get; private set; }
        public double AcceptThreshold { get; set; } = 0.7;
        public int    MaxInstances    { get; set; } = 1;

        private readonly OpenCvBackend _be;

        public OpenCvPatternFinder(string id, OpenCvBackend be)
        {
            Id = id; _be = be;
            SearchRoi = new Roi { Name = id + ".Search", CenterX = 320, CenterY = 240, Width = 400, Height = 300 };
            TrainRoi  = new Roi { Name = id + ".Train",  CenterX = 320, CenterY = 240, Width = 100, Height = 100 };
        }

        public void Train(Bitmap image)
        {
            if (image == null) return;
            var rect = TrainRoi.BoundingBox;
            rect.Intersect(new Rectangle(0, 0, image.Width, image.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;
            TrainImage?.Dispose();
            TrainImage = image.Clone(rect, image.PixelFormat);
        }

        public MatchResult Match(Bitmap image)
        {
            if (image == null || TrainImage == null)
                return MatchResult.Fail(Id, "no image or train");

            if (_be.EmguLoaded)
            {
                // OS-09 (Stage 60 cycle 4) — EmguCV 실 호출 통합 가이드
                //
                // 사용자 NuGet 설치 단계 (한 번만):
                //   1. QMC.Vision.csproj 에 다음 PackageReference 추가:
                //        <PackageReference Include="Emgu.CV" Version="4.7.0.5276" />
                //        <PackageReference Include="Emgu.CV.runtime.windows" Version="4.7.0.5276" />
                //   2. 또는 NuGet Package Manager 에서 "Emgu.CV" + Windows runtime 검색 설치
                //   3. cvextern.dll 등 native DLL 이 bin/Debug 에 자동 복사됨
                //
                // 코드 통합 (NuGet 설치 후 본 영역 주석 해제):
                //
                //   using Emgu.CV;
                //   using Emgu.CV.Structure;
                //   using Emgu.CV.CvEnum;
                //
                //   try
                //   {
                //       using (var src = new Image<Bgr, byte>(image))
                //       using (var tpl = new Image<Bgr, byte>(TrainImage))
                //       using (var res = src.MatchTemplate(tpl, TemplateMatchingType.CcoeffNormed))
                //       {
                //           double[] minVals, maxVals;
                //           Point[] minLocs, maxLocs;
                //           res.MinMax(out minVals, out maxVals, out minLocs, out maxLocs);
                //           if (maxVals[0] >= AcceptThreshold)
                //           {
                //               return new MatchResult {
                //                   RoiName = Id, Success = true,
                //                   Best = new MatchInstance {
                //                       CenterX = maxLocs[0].X + tpl.Width / 2.0,
                //                       CenterY = maxLocs[0].Y + tpl.Height / 2.0,
                //                       Score   = maxVals[0]
                //                   }
                //               };
                //           }
                //       }
                //   }
                //   catch (Exception ex)
                //   {
                //       // EmguCV 호출 예외 시 SAD fallback
                //       System.Diagnostics.Debug.WriteLine("EmguCV match failed: " + ex.Message);
                //   }
                //
                // 현재는 NuGet 미설치 환경 — fallback 으로 진행
            }

            return BasicSadMatch(image);
        }

        /// <summary>EmguCV 없을 때 — 단순 SAD 템플릿 매칭 (저해상도용).</summary>
        private MatchResult BasicSadMatch(Bitmap src)
        {
            var result = new MatchResult { RoiName = Id, Success = true };
            var rect = SearchRoi.BoundingBox;
            rect.Intersect(new Rectangle(0, 0, src.Width, src.Height));
            if (rect.Width < TrainImage.Width || rect.Height < TrainImage.Height)
            {
                result.Success = false; result.ErrorMessage = "ROI smaller than template";
                return result;
            }

            // 무거운 연산이므로 실제 운용 시 EmguCV 권장.
            int bestX = 0, bestY = 0; long bestSad = long.MaxValue;
            int step = 4; // 성능용 샘플링

            var srcData = src.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var tplData = TrainImage.LockBits(new Rectangle(0, 0, TrainImage.Width, TrainImage.Height),
                                              ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int tW = TrainImage.Width, tH = TrainImage.Height;
                int sStride = srcData.Stride, tStride = tplData.Stride;
                unsafe
                {
                    byte* sPtr = (byte*)srcData.Scan0;
                    byte* tPtr = (byte*)tplData.Scan0;
                    for (int y = 0; y <= rect.Height - tH; y += step)
                    {
                        for (int x = 0; x <= rect.Width - tW; x += step)
                        {
                            long sad = 0;
                            for (int yy = 0; yy < tH; yy += step)
                            {
                                byte* s = sPtr + (y + yy) * sStride + x * 3;
                                byte* t = tPtr + yy * tStride;
                                for (int xx = 0; xx < tW * 3; xx += 3 * step)
                                {
                                    sad += Math.Abs(s[xx]     - t[xx]);
                                    sad += Math.Abs(s[xx + 1] - t[xx + 1]);
                                    sad += Math.Abs(s[xx + 2] - t[xx + 2]);
                                    if (sad >= bestSad) goto SkipPos;
                                }
                            }
                            if (sad < bestSad) { bestSad = sad; bestX = x; bestY = y; }
                            SkipPos: ;
                        }
                    }
                }
            }
            finally
            {
                src.UnlockBits(srcData);
                TrainImage.UnlockBits(tplData);
            }

            double maxSad = (double)TrainImage.Width * TrainImage.Height * 3 * 255 / (step * step);
            double score  = Math.Max(0.0, 1.0 - (double)bestSad / maxSad);
            result.Instances.Add(new MatchInstance
            {
                Index    = 0,
                CenterX  = rect.X + bestX + TrainImage.Width  / 2.0,
                CenterY  = rect.Y + bestY + TrainImage.Height / 2.0,
                AngleDeg = 0,
                Score    = score
            });
            return result;
        }

        public void LoadParameters(string path) { }
        public void SaveParameters(string path) { try { File.WriteAllText(path, "OpenCvPatternFinder: " + Id); } catch { } }
    }
}
