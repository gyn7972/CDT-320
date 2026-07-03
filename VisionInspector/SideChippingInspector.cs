using QMC.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// 사이드 치핑 검사를 전담하는 클래스
    /// </summary>
    public class SideChippingInspector
    {
        private VisionConfig _visionConfig;
        private QMC_FindChippingNForeign _findChippingTool;

        public SideChippingInspector(VisionConfig config)
        {
            _visionConfig = config ?? throw new ArgumentNullException(nameof(config));
            _findChippingTool = new QMC_FindChippingNForeign();
        }

        /// <summary>
        /// 사이드 이미지에서 치핑 검사 수행
        /// </summary>
        public SideChippingResult InspectChipping(ref byte[,]  image, int imageWidth, int imageHeight, 
            SideInspectionParameter parameter)
        {
            var result = new SideChippingResult();
            
            try
            {
                _findChippingTool.SetChippingThreshold(parameter.Threshold);
                
                // 1. 칩의 상단 라인 검출
                Line topLine = _findChippingTool.FindTopLineOfChip(imageWidth, imageHeight, image, true,0.9);

                double angle = topLine.GetAngle();
                var (rotatedImage, newWidth, newHeight) = RotateImage(image, imageWidth, imageHeight, angle);

                // 회전된 이미지를 원본에 복사
                for (int y = 0; y < imageHeight; y++)
                {
                    for (int x = 0; x < imageWidth; x++)
                    {
                        image[y, x] = rotatedImage[y, x];
                    }
                }

                // 회전된 이미지로 라인 검출
                Line rotatedTopLine = _findChippingTool.FindTopLineOfChip(newWidth, newHeight, image, true,0.9);

                //Line bottomLine = _findChippingTool.FindBottomLineOfChip(imageWidth, imageHeight, image, true);

                double dOffset = parameter.ChipThickness / _visionConfig.SideVisionFront.PixelSizeWidthMm;
                
                Line bottomLine = new Line(rotatedTopLine.mA, rotatedTopLine.mB+ dOffset );

                if (rotatedTopLine == null || bottomLine == null)
                {
                    Log.Write("SideChippingInspector", "상단/하단 라인 검출 실패");
                    result.IsSuccess = false;
                    return result;
                }

                // 2. 치핑 마진 계산 — 스펙(ChippingDepth)이 아닌 칩 두께 기반.
                //    (스펙 유도 마진이면 스펙보다 깊은 칩핑이 '밝음 복귀'를 못 찾아 미검출되는 역설 발생)
                double pxHmm = _visionConfig.SideVisionFront.PixelSizeHeightMm;
                if (pxHmm <= 0) pxHmm = 0.003125;
                int chippingMargin = (int)Math.Max(8, parameter.ChipThickness / pxHmm);
                // 3. 상단 치핑 검사 (+스펙 초과 영역 수집 — 다중 칩핑 검출/실측 마커용)
                var chipRegions = new List<ChippingRegion>();
                double topChippingSize = InspectTopChipping(image, imageWidth, imageHeight, 
                    rotatedTopLine, bottomLine, parameter.Threshold, chippingMargin, chipRegions, parameter.ChippingDepth);

                // 4. 하단 치핑 검사
                ChippingInfo info = InspectBottomChipping(image, imageWidth, imageHeight, 
                    rotatedTopLine, bottomLine, parameter.Threshold, chippingMargin, chipRegions, parameter.ChippingDepth);
                double bottomChippingSize = info.Depth;
                // 5. 결과 설정
                result.TopChippingSize = topChippingSize;
                result.BottomChippingSize = bottomChippingSize;
                result.MaxChippingSize = Math.Max(topChippingSize, bottomChippingSize);
                result.ChippingRegions = chipRegions;
                result.IsSuccess = true;

                // 6. 스펙 판정
                double chippingSpec = parameter.ChippingDepth;
                
                result.IsDefect = result.MaxChippingSize > chippingSpec;

                if (result.IsDefect)
                {
                    string strFileName = "d:\\Log\\Image\\Side";

                    CDTInspector.IfNotExistMakeFolder(strFileName);

                    strFileName += "\\" + parameter.WaferID;
                    CDTInspector.IfNotExistMakeFolder(strFileName);

                    strFileName += "\\NG\\";
                    CDTInspector.IfNotExistMakeFolder(strFileName);

                    strFileName += "X_" + parameter.IndexX.ToString();
                    strFileName += "_Y_" + parameter.IndexY.ToString();
                    strFileName += DateTime.Now.Ticks.ToString();

                    int nMargin = 50;
                    int cropWidth = (int)(info.Contour.Max(t => t.X) - info.Contour.Min(t => t.X)) + nMargin * 2;
                    int cropHeight = (int)(info.Contour.Max(t => t.Y) - info.Contour.Min(t => t.Y)) + nMargin * 2;
                    int cropX = Math.Max((int)info.Contour.Min(t => t.X) - nMargin, 0);
                    int cropY = Math.Max((int)info.Contour.Min(t => t.Y) - nMargin, 0);

                    byte[,] croppedImage = new byte[cropHeight, cropWidth];
                    for (int y = 0; y < cropHeight; y++)
                    {
                        if (cropY + y >= imageHeight || y >= cropHeight) continue;
                        Buffer.BlockCopy(image, (cropY + y) * imageWidth + cropX, croppedImage, y * cropWidth, Math.Min(cropWidth, imageWidth - cropX));
                    }
                    SaveImage(croppedImage, cropWidth, cropHeight, info, strFileName);
                }
                else
                {
                    //string strFileName = "d:\\Log\\Image\\Side";

                    //CDTInspector.IfNotExistMakeFolder(strFileName);

                    //strFileName += "\\" + parameter.WaferID;
                    //CDTInspector.IfNotExistMakeFolder(strFileName);
                    //strFileName += "\\OK\\";
                    //CDTInspector.IfNotExistMakeFolder(strFileName);

                    //strFileName += "X_" + parameter.IndexX.ToString();
                    //strFileName += "_Y_" + parameter.IndexY.ToString();
                    //strFileName += DateTime.Now.Ticks.ToString();
                    //SaveImage(image, imageWidth, imageHeight, info, strFileName);
                }

                    Log.Write("SideChippingInspector",
                        $"검사 완료 - Top: {topChippingSize:F4}mm, Bottom: {bottomChippingSize:F4}mm, " +
                        $"Max: {result.MaxChippingSize:F4}mm, Spec: {chippingSpec:F4}mm, Defect: {result.IsDefect}");
            }
            catch (Exception ex)
            {
                Log.Write("SideChippingInspector", $"검사 중 오류: {ex.Message}");
                Log.Write(ex);
                result.IsSuccess = false;
            }

            return result;
        }

        private void SaveImage(byte[,] image, int width, int height, ChippingInfo info,string strFileName)
        {
            SaveImageHelper helper = new SaveImageHelper
            {
                ShiftImage = image,
                Width = width,
                Height = height,
                FileName = strFileName
            };
            int nMargin = 50;
            Task.Factory.StartNew((obj) =>
            {
                lock (this)
                {
                    double dsize = Math.Min(width, height);
                    
                }
                SaveImageHelper saveHelper = (SaveImageHelper)obj;

                string strOrginalFileName = saveHelper.FileName;
                byte[,] shiftImage = saveHelper.ShiftImage;
                int w = saveHelper.Width;
                int h = saveHelper.Height;
                string fileName = saveHelper.FileName;

                try
                {
                    if (shiftImage == null)
                        throw new ArgumentNullException(nameof(shiftImage));
                    if (string.IsNullOrWhiteSpace(fileName))
                        throw new ArgumentException("파일 이름이 올바르지 않습니다.", nameof(fileName));

                    // 8비트 인덱스 비트맵 생성
                    using (var bmp8bit = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format8bppIndexed))
                    {
                        // 회색조 팔레트 설정
                        var palette = bmp8bit.Palette;
                        for (int i = 0; i < 256; i++)
                        {
                            palette.Entries[i] = Color.FromArgb(i, i, i);
                        }
                        bmp8bit.Palette = palette;

                        // 픽셀 데이터 복사
                        var rect = new Rectangle(0, 0, w, h);
                        var bmpData = bmp8bit.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp8bit.PixelFormat);
                        try
                        {
                            int stride = bmpData.Stride;
                            unsafe
                            {
                                fixed (byte* pSrc = &shiftImage[0, 0])
                                {
                                    byte* ptr = (byte*)bmpData.Scan0;
                                    int rowBytes = Math.Max(w, stride); // stride가 w보다 클 수 있음
                                    for (int y = 0; y < h; y++)
                                    {
                                        Buffer.MemoryCopy(
                                            pSrc + y * w,      // 소스: shiftImage[y, 0]
                                            ptr + y * stride,  // 타겟: Bitmap의 y번째 라인
                                            rowBytes,          // 타겟 버퍼 크기
                                            w                  // 복사할 바이트 수
                                        );
                                    }
                                }
                            }
                        }
                        finally
                        {
                            bmp8bit.UnlockBits(bmpData);
                        }

                        // 24비트 컬러 비트맵으로 변환
                        //using (var bmp24bit = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                        Bitmap bmp24bit = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        {
                            using (Graphics g = Graphics.FromImage(bmp24bit))
                            {
                                // 8비트 비트맵을 그립니다.
                                g.DrawImage(bmp8bit, 0, 0);

                                // 사각형 그리기
                                using (Pen pen = new Pen(Color.Red, 2))
                                {
                                    g.DrawRectangle(pen, nMargin, nMargin, w - 2 * nMargin, h - 2 * nMargin);
                                }

                                // Width와 Height 텍스트 추가
                                using (Font font = new Font("Arial", 10))
                                using (Brush brush = new SolidBrush(Color.Red))
                                {
                                    double dWidth = w - nMargin * 2;
                                    double dHeight = h - nMargin * 2;
                                    dWidth *= _visionConfig.SideVisionFront.PixelSizeWidthMm  * 1000; // mm 단위로 변환
                                    dHeight *= _visionConfig.SideVisionFront.PixelSizeHeightMm  * 1000; // mm 단위로 변환
                                    // 텍스트 위치 조정
                                    string text = $"W: {dWidth:F1} um, H: {dHeight:F1} um";

                                    g.DrawString(text, font, brush, new PointF(50 + 5, nMargin - 30));
                                }
                            }

                            

                            string path = fileName  + ".png";
                            bmp24bit.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                            lock (this)
                            {
                                double dsize = Math.Min(width, height);
                               
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    // SaveImage(shiftImage, w, h, strOrginalFileName + "Retry_");
                }
                finally
                {
                    
                }
            }, helper);
        }

        /// <summary>
        /// 상단 치핑 검사
        /// </summary>
        private double InspectTopChipping(byte[,] image, int imageWidth, int imageHeight, 
            Line topLine, Line bottomLine, int threshold, int chippingMargin,
            List<ChippingRegion> regions = null, double specMM = 0)
        {
            double maxChippingSize = 0;
            List<double> listValue = new List<double>();
            List<int> listX = new List<int>(), listY0 = new List<int>(), listY1 = new List<int>();
            for (int x = 0; x < imageWidth; x++)
            {
                double startY = topLine.GetY(x);
                double endY = bottomLine.GetY(x);

                if (startY < 0 || endY >= imageHeight || startY >= endY)
                    continue;

                int topY = (int)startY;
                bool darkFound = false;
                int darkStartY = -1;
                
                // 상단에서 안쪽으로 스캔
                for (int y = topY; y < Math.Min(topY + chippingMargin, (int)endY); y++)
                {
                    if (y < 0 || y >= imageHeight) break;

                    byte pixelValue = image[y, x];

                    // 어두운 영역 감지 (치핑)
                    if (pixelValue < threshold && !darkFound)
                    {
                        darkFound = true;
                        darkStartY = y;
                    }
                    // 밝은 영역 복귀 (치핑 종료)
                    else if (pixelValue >= threshold && darkFound)
                    {
                        int chippingDepth = y - darkStartY;
                        if (chippingDepth > 0)
                        {
                            double chippingSizeMM = ConvertPixelToMM(chippingDepth);
                            listValue.Add(chippingSizeMM);
                            listX.Add(x); listY0.Add(darkStartY); listY1.Add(y);
                            if (chippingSizeMM > maxChippingSize)
                                maxChippingSize = chippingSizeMM;
                        }
                        darkFound = false;
                        break;
                    }
                }
            }
            if(listValue.Count < 15)
            {
                return 0;
            }
            maxChippingSize = 0;
            for (int iter = 15; iter < listValue.Count-15; iter ++)
            {
                if (listValue[iter] > maxChippingSize)
                    maxChippingSize = listValue[iter];
            }
            CollectChippingRegions(listValue, listX, listY0, listY1, specMM, true, regions);
            return maxChippingSize;
        }

        /// <summary>스펙(specMM) 초과 컬럼을 x-연속(간격≤5px) 그룹으로 묶어 칩핑 영역 목록에 추가 — 다중 칩핑 검출/실측 마커용.</summary>
        private void CollectChippingRegions(List<double> vals, List<int> xs, List<int> y0s, List<int> y1s,
            double specMM, bool isTop, List<ChippingRegion> regions)
        {
            if (regions == null || specMM <= 0 || vals.Count < 15) return;
            ChippingRegion cur = null; int lastX = int.MinValue; double curMax = 0;
            for (int i = 15; i < vals.Count - 15; i++)
            {
                if (vals[i] <= specMM) continue;
                int x = xs[i];
                int ya = Math.Min(y0s[i], y1s[i]), yb = Math.Max(y0s[i], y1s[i]);
                if (cur == null || x - lastX > 5)
                {
                    curMax = vals[i];
                    cur = new ChippingRegion { XStart = x, XEnd = x, X = x, StartY = ya, EndY = yb,
                                               SizeMM = curMax, IsFrontSide = isTop };
                    regions.Add(cur);
                }
                else
                {
                    cur.XEnd = x;
                    if (ya < cur.StartY) cur.StartY = ya;
                    if (yb > cur.EndY)   cur.EndY   = yb;
                    if (vals[i] > curMax) { curMax = vals[i]; cur.SizeMM = curMax; }
                    cur.X = (cur.XStart + cur.XEnd) / 2;
                }
                cur.DepthPixels = cur.EndY - cur.StartY;
                lastX = x;
            }
        }

        /// <summary>
        /// 하단 치핑 검사
        /// </summary>
        private ChippingInfo InspectBottomChipping(byte[,] image, int imageWidth, int imageHeight, 
            Line topLine, Line bottomLine, int threshold, int chippingMargin,
            List<ChippingRegion> regions = null, double specMM = 0)
        {
            double maxChippingSize = 0;

            List<ChippingInfo> listValue = new List<ChippingInfo>();
            List<double> bVals = new List<double>(); List<int> bXs = new List<int>(), bY0 = new List<int>(), bY1 = new List<int>();
            for (int x = 0; x < imageWidth; x++)
            {
                double startY = topLine.GetY(x);
                double endY = bottomLine.GetY(x);

                if (startY < 0 || endY >= imageHeight || startY >= endY)
                    continue;

                int bottomY = (int)endY;
                bool darkFound = false;
                int darkStartY = -1;
                int nScanDepth = 0;
                // 하단에서 안쪽으로 스캔
                for (int y = bottomY; y > Math.Min(bottomY - chippingMargin, (int)startY); y--)
                {
                    if (y < 0 || y >= imageHeight) break;

                    byte pixelValue = image[y, x];

                    // 어두운 영역 감지 (치핑)
                    if (pixelValue < threshold && !darkFound)
                    {
                        darkFound = true;
                        darkStartY = y;
                    }
                    // 밝은 영역 복귀 (치핑 종료)
                    else if (pixelValue >= threshold && darkFound )
                    {
                        int chippingDepth = darkStartY - y;
                        if (chippingDepth > 0)
                        {
                            double chippingSizeMM = ConvertPixelToMM(chippingDepth);

                            ChippingInfo chippingInfo = new ChippingInfo();
                            chippingInfo.Depth = chippingSizeMM;
                            chippingInfo.Contour.Add(new PointF(x, y));
                            chippingInfo.Contour.Add(new PointF(x, bottomY));
                            listValue.Add(chippingInfo);
                            bVals.Add(chippingSizeMM); bXs.Add(x); bY0.Add(y); bY1.Add(darkStartY);
                        }
                        darkFound = false;
                        break;
                    }
                    else
                    {
                        nScanDepth++;
                        if( nScanDepth > 100)
                        {
                            break;
                        }
                    }
                }
            }
            CollectChippingRegions(bVals, bXs, bY0, bY1, specMM, false, regions);
            ChippingInfo chippingInfoMax = new ChippingInfo();
            if (listValue.Count < 15)
            {
                return chippingInfoMax;
            }
            maxChippingSize = 0;
            int nMaxIndex = 0;
            for (int iter = 15; iter < listValue.Count - 15; iter++)
            {
                if (listValue[iter].Depth > maxChippingSize)
                {

                    maxChippingSize = listValue[iter].Depth;
                    chippingInfoMax = listValue[iter];
                    nMaxIndex = iter;
                }
            }
            int w = 0;
            
            for(int iter = 0 ; iter  < 100; iter ++)
            {
                int index = nMaxIndex - iter;
                if(index  < 0)
                {
                    break;
                }
                if (listValue[index].Depth > 0.02)
                {
                    w = iter;
                }
                else
                {
                    break; 
                }
            }
            int wL = 0;
            for (int iter = 0; iter < 100; iter++)
            {
                int index = nMaxIndex + iter;
                if (index >= listValue.Count )
                {
                    break;
                }
                if (listValue[index].Depth > 0.02)
                {
                    wL = iter;
                }
                else
                {
                    break;
                }
            }
            if(chippingInfoMax.Contour.Count > 0)
            {
                int xRef = (int)chippingInfoMax.Contour[0].X;
                var v = chippingInfoMax.Contour.ToList();
                chippingInfoMax.Contour.Clear();
                for (int iter = 0; iter < 2; iter++)
                {
                    chippingInfoMax.Contour.Add(new PointF(xRef - w, v[iter].Y));
                    chippingInfoMax.Contour.Add(new PointF(xRef + wL, v[iter].Y));
                }
            }
           

            

            return chippingInfoMax;
        }

        /// <summary>
        /// 치핑 마진을 픽셀 단위로 계산
        /// </summary>
        private int CalculateChippingMargin(double chippingDepthMM)
        {
            if (_visionConfig?.BottomVision == null)
                return 50; // 기본값

            int margin = (int)(chippingDepthMM / _visionConfig.SideVisionBack.PixelSizeHeightMm);
            return margin <= 0 ? 50 : margin;
        }

        /// <summary>
        /// 픽셀을 mm로 변환
        /// </summary>
        private double ConvertPixelToMM(int pixels)
        {
            if (_visionConfig?.SideVisionFront == null || pixels <= 0)
                return 0;
            _visionConfig.SideVisionFront.PixelSizeHeightMm = 0.003125;
            _visionConfig.SideVisionFront.PixelSizeWidthMm = 0.003125;
            return pixels * _visionConfig.SideVisionFront.PixelSizeHeightMm;
        }

        /// <summary>
        /// 이미지를 지정된 각도로 회전
        /// </summary>
        private (byte[,], int, int) RotateImage(byte[,] original, int width, int height, double angle)
        {
            // 회전된 크기는 원본 크기로 유지
            int newWidth = width;
            int newHeight = height;

            byte[,] rotated = new byte[newHeight, newWidth];

            double angleRadians = angle * Math.PI / 180.0;
            double centerX = newWidth / 2.0;
            double centerY = newHeight / 2.0;

            double cosAngle = Math.Cos(angleRadians);
            double sinAngle = Math.Sin(angleRadians);

            for (int newY = 0; newY < newHeight; newY++)
            {
                for (int newX = 0; newX < newWidth; newX++)
                {
                    // 새로운 좌표를 원본 좌표계로 변환
                    double relX = newX - centerX;
                    double relY = newY - centerY;

                    double origX = relX * cosAngle - relY * sinAngle + centerX;
                    double origY = relX * sinAngle + relY * cosAngle + centerY;

                    // bilinear interpolation
                    if (origX >= 0 && origX < width - 1 && origY >= 0 && origY < height - 1)
                    {
                        int x1 = (int)origX;
                        int y1 = (int)origY;
                        int x2 = x1 + 1;
                        int y2 = y1 + 1;

                        double fx = origX - x1;
                        double fy = origY - y1;

                        double val = (1 - fx) * (1 - fy) * original[y1, x1] +
                                     fx * (1 - fy) * original[y1, x2] +
                                     (1 - fx) * fy * original[y2, x1] +
                                     fx * fy * original[y2, x2];

                        rotated[newY, newX] = (byte)Math.Round(val);
                    }
                    else
                    {
                        rotated[newY, newX] = 0; // 배경
                    }
                }
            }

            return (rotated, newWidth, newHeight);
        }
    }

    /// <summary>
    /// 사이드 치핑 검사 결과
    /// </summary>
    public class SideChippingResult
    {
        /// <summary>
        /// 검사 성공 여부
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 불량 여부
        /// </summary>
        public bool IsDefect { get; set; }

        /// <summary>
        /// 상단 최대 치핑 크기 (mm)
        /// </summary>
        public double TopChippingSize { get; set; }

        /// <summary>
        /// 하단 최대 치핑 크기 (mm)
        /// </summary>
        public double BottomChippingSize { get; set; }

        /// <summary>
        /// 전체 최대 치핑 크기 (mm)
        /// </summary>
        public double MaxChippingSize { get; set; }

        public int XIndex = 0, YIndex = 0;
        /// <summary>
        /// 검출된 치핑 영역 목록
        /// </summary>
        public List<ChippingRegion> ChippingRegions { get; set; } = new List<ChippingRegion>();
    }

    /// <summary>
    /// 치핑 영역 정보
    /// </summary>
    public class ChippingRegion
    {
        /// <summary>
        /// X 좌표(영역 중심)
        /// </summary>
        public int X { get; set; }

        /// <summary>영역 가로 시작/끝(px) — 실측 폭 마커용</summary>
        public int XStart { get; set; }
        public int XEnd { get; set; }

        /// <summary>
        /// 시작 Y 좌표
        /// </summary>
        public int StartY { get; set; }

        /// <summary>
        /// 종료 Y 좌표
        /// </summary>
        public int EndY { get; set; }

        /// <summary>
        /// 치핑 깊이 (픽셀)
        /// </summary>
        public int DepthPixels { get; set; }

        /// <summary>
        /// 치핑 크기 (mm)
        /// </summary>
        public double SizeMM { get; set; }

        /// <summary>
        /// 상단 치핑 여부 (false면 하단)
        /// </summary>
        public bool IsFrontSide { get; set; }
    }
}