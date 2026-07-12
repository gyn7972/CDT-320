using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using System.Xml.Linq;
namespace QMC.Vision.Inspector
{

    public class QMC_BlobTool
    {
        public class ImageData
        {
            public int X;
            public int Y;
            public byte Value;
            public ImageData()
            {
                X = 0;
                Y = 0;
                Value = 0;
            }
            public ImageData(int x, int y, byte value)
            {
                X = x;
                Y = y;
                Value = value;
            }
        }
        public QMC_BlobTool()
        {
            Threshold = 128;
            MinArea = 100;
            MaxArea = 1000000;
            BumpSize = 72;
            ROI = new Rectangle(0, 0, 0, 0);
            UseROI = false;
            IsFindBlobInROI = false;

        }

        public int BumpSize { get; set; }
        public int Threshold { get; set; }
        public int MinArea { get; set; }
        public int MaxArea { get; set; }
        public System.Drawing.Bitmap Image { get; set; }
        public Rectangle ROI { get; set; }
        public bool UseROI { get; set; }
        public bool IsFindBlobInROI { get; set; }
        private int _width = 100;
        private int _height = 100;
        
        Point[] stack = new Point[10000* 10000];

        public void SetROI(Rectangle roi)
        {
            ROI = new Rectangle((int)roi.Left, (int)roi.Top, (int)roi.Width, (int)roi.Height);
        }
        public void FindBlob()
        {
            FindBlobBright(Image, Threshold, MinArea);
        }
        public void FindBlob(System.Drawing.Bitmap bmp)
        {
            FindBlobBright(bmp, Threshold, MinArea);
        }
        public void FindBlob(Bitmap bmp, int nThreshold)
        {
            FindBlobBright(bmp, nThreshold, MinArea);
        }



        public double GetVeriance(byte[] image, int w, int h, int stride, int x, int y, int nRoiWidht, int nRoiHeight)
        {
            double dVeriance = 0;
            double dSum = 0;
            double dSum2 = 0;
            int nCount = 0;
            for (int j = y - nRoiHeight / 2; j <= y + nRoiHeight / 2; j++)
            {
                for (int i = x - nRoiWidht / 2; i <= x + nRoiWidht / 2; i++)
                {
                    if (i >= 0 && i < w && j >= 0 && j < h)
                    {
                        dSum += image[j * stride + i];
                        dSum2 += image[j * stride + i] * image[j * stride + i];
                        nCount++;
                    }
                }
            }
            if (nCount > 0)
            {
                double dMean = dSum / nCount;
                dVeriance = dSum2 / nCount - dMean * dMean;
            }
            return dVeriance;
        }
        public List<Point> GetContour(List<Point> points)
        {
            List<Point> listPoint = new List<Point>();

            if (points.Count < 10000)
            {
                listPoint = GetOutLinePoint(points);

            }
            else
            {
                //포인트의 좌우 상하 4포인트만 찾아 listPoint에 저장합니다.
                int xLeft = points.Min(p => p.X);
                int xRight = points.Max(p => p.X);
                int yTop = points.Min(p => p.Y);
                int yBottom = points.Max(p => p.Y);

                for (int x = xLeft; x <= xRight; x++)
                {
                    Point pt = new Point(x, yTop);
                    listPoint.Add(pt);
                }
                for (int y = yTop; y <= yBottom; y++)
                {
                    Point pt = new Point(xRight, y);
                    listPoint.Add(pt);
                }

                for (int x = xRight; x >= xLeft; x--)
                {
                    Point pt = new Point(x, yBottom);
                    listPoint.Add(pt);
                }
                for (int y = yBottom; y >= yTop; y--)
                {
                    Point pt = new Point(xLeft, y);
                    listPoint.Add(pt);
                }
            }
            return listPoint;
        }

        private List<Point> GetOutLinePoint(List<Point> points)
        {
            List<Point> contour = new List<Point>();

            points.OrderBy(t => t.X).GroupBy(x => x.X).ToList().ForEach(x =>
            {
                int v = x.Min(y => y.Y);
                int nx = x.Min(t => t.X);
                contour.Add(new Point(nx, (int)v));
            });
            //contour = contour.OrderBy(t => t.X).ToList();
            points.OrderByDescending(t => t.X).GroupBy(x => x.X).ToList().ForEach(x =>
            {
                int v = x.Max(y => y.Y);
                int nx = x.Max(t => t.X);
                contour.Add(new Point(nx, (int)v));
            });

            return contour;
        }

        //GetNextPoint를 구현 한다.

        public void FindFineBlob(byte[] image, int w, int h, int stride, int bumpSize, ref List<Point> points)
        {
            // points의 좌료 외곽쪽에 존제하는 Pixel을 찾아서 밝기가 가장 밝은 순으로 정렬합니다.
            // 정렬된 좌표를 points에 저장합니다.
            // points에 저장된 좌료의의 개수가 bumpSize와 같으면 함수를 종료합니다.
            // 재귀 호출을 반복문으로 변경하여 StackOverflow 방지 및 로직 개선

            while (points.Count != bumpSize)
            {
                int currentCount = points.Count;
                int nFillCount = bumpSize - currentCount;

                if (nFillCount == 0) return;

                List<ImageData> list = new List<ImageData>();

                if (nFillCount > 0)
                {
                    foreach (Point pt in points)
                    {
                        // pt옆의 8방향에 있는 좌표중 points에 속하지 않은 좌표를 찾아 list에 추가합니다.
                        Point[] neighbors = new Point[]
                        {
                            new Point(pt.X - 1, pt.Y), new Point(pt.X + 1, pt.Y),
                            new Point(pt.X, pt.Y - 1), new Point(pt.X, pt.Y + 1),
                            new Point(pt.X - 1, pt.Y - 1), new Point(pt.X + 1, pt.Y - 1),
                            new Point(pt.X - 1, pt.Y + 1), new Point(pt.X + 1, pt.Y + 1)
                        };

                        foreach (var n in neighbors)
                        {
                            SearchNearPoint(image, stride, points, ref list, n);
                        }
                    }

                    // 중복 제거 (여러 포인트가 같은 이웃을 공유할 수 있음) 후 정렬
                    var v = list.GroupBy(x => new { x.X, x.Y })
                                .Select(g => g.First())
                                .OrderByDescending(x => x.Value)
                                .ThenBy(x => x.X)
                                .ThenBy(x => x.Y)
                                .Take(nFillCount)
                                .ToList();

                    if (v.Count > 0)
                    {
                        foreach (var item in v)
                        {
                            points.Add(new Point(item.X, item.Y));
                        }
                    }
                    else
                    {
                        // 더 이상 확장할 수 없음 (고립됨)
                        break;
                    }
                }
                else
                {
                    // Shrink: 현재 포인트 중 가장 밝은 것만 남김
                    foreach (var pt in points)
                    {
                        if (pt.X >= 0 && pt.X < stride && pt.Y >= 0 && pt.Y < image.Length / stride)
                        {
                            ImageData data = new ImageData(pt.X, pt.Y, image[pt.Y * stride + pt.X]);
                            list.Add(data);
                        }
                    }

                    var v = list.OrderByDescending(x => x.Value)
                                .ThenBy(x => x.X)
                                .ThenBy(x => x.Y)
                                .Take(Math.Abs(bumpSize))
                                .ToList();

                    if (v.Count > 0)
                    {
                        points = v.Select(x => new Point(x.X, x.Y)).ToList();
                    }
                    // 축소 후 종료
                    break;
                }

                // 변화가 없으면 무한 루프 방지
                if (points.Count == currentCount)
                {
                    break;
                }
            }
        }

        private static void SearchNearPoint(byte[] image, int stride, List<Point> points, ref List<ImageData> list, Point pt)
        {
            if (points.Contains(pt) == false)
            {
                //pt가 이미지의 좌표 내에 있는지 확인 한다.
                //pt가 이미지의 좌표 내에 있으면 list에 추가한다.



                if (pt.X >= 0 && pt.X < stride && pt.Y >= 0 && pt.Y < image.Length / stride)
                {
                    ImageData data = new ImageData(pt.X, pt.Y, image[pt.Y * stride + pt.X]);
                    list.Add(data);
                }

            }
        }

        public void FindBlobBright(System.Drawing.Bitmap bmp, int nThreshold, int nMinSize)
        {
            Image = bmp;
            Threshold = nThreshold;
            MinArea = nMinSize;
            int nWidth = bmp.Width;
            int nHeight = bmp.Height;

            //이미지의 크기가 0이면 함수를 종료 합니다.
            if (nWidth == 0 || nHeight == 0)
                return;

            //이미지의 픽셀 데이터를 가져옵니다.
            Rectangle rect = new Rectangle(0, 0, nWidth, nHeight);
            System.Drawing.Imaging.BitmapData bmpData = Image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, Image.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            int nBytes = Math.Abs(bmpData.Stride) * nHeight;
            byte[] pImage = new byte[nBytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, pImage, 0, nBytes);
            int nSize = bmpData.Stride * nHeight;
            nWidth = bmpData.Stride;
            byte[] pBlob = new byte[nSize];
            //pBlob 을 0으로 초기화 합니다.
            for (int i = 0; i < nSize; i++)
                pBlob[i] = 0;




            List<List<Point>> listlistPoint = new List<List<Point>>();
            FindBlobBright(pImage, nWidth, nHeight, nWidth, Threshold, MinArea, 0, ref listlistPoint);
            //찾은 Blob의 갯수를 출력합니다.
            Console.WriteLine("FindBlob Count = " + listlistPoint.Count);
            //찾은 Blob의 갯수만큼 반복합니다.
            for (int i = 0; i < listlistPoint.Count; i++)
            {
                //Blob의 크기를 출력합니다.
                Console.WriteLine("Blob[" + i + "] Count = " + listlistPoint[i].Count);
                //Blob의 크기가 0이면 다음 Blob을 찾습니다.
                if (listlistPoint[i].Count == 0)
                    continue;
                //Blob의 크기가 0이 아니면 Blob의 크기만큼 반복합니다.
                List<Point> listConture = GetContour(listlistPoint[i]);
                for (int j = 0; j < listConture.Count; j++)
                {
                    //Blob의 좌표에 255를 넣어서 Blob을 표시합니다.
                    pImage[listConture[j].Y * nWidth + listConture[j].X] = 255;
                }
            }
            //Blob을 표시한 이미지를 출력합니다.
            System.Runtime.InteropServices.Marshal.Copy(pImage, 0, ptr, nBytes);

            Image.UnlockBits(bmpData);


        }
        public void MakeRoiMask(int w, int h, int stride, ref bool[] bVisit)
        {
            int left = 0;
            int right = w;
            int top = 0;
            int bottom = h;
            GetRoi(w, h, ref top, ref bottom, ref left, ref right);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x < left || x >= right || y < top || y >= bottom)
                        bVisit[y * stride + x] = true;
                }
            }
        }
        public int GetRoi(int w, int h, ref int top, ref int bottom, ref int left, ref int right)
        {
            int ret = 0;
            if (UseROI)
            {
                top = Math.Max(0, Math.Min(ROI.Top, h));
                bottom = Math.Max(0, Math.Min(ROI.Bottom, h));
                left = Math.Max(0, Math.Min(ROI.Left, w));
                right = Math.Max(0, Math.Min(ROI.Right, w));
            }
            return ret;
        }
        public void RemoveBrightBlob(byte[] image, int w, int h, int stride, int nThreshold, int nMinSize, int nThreshold1 )
        {
            
            List<List<Point>> listlistPoint = new List<List<Point>>();
            FindBlobBright(image, w, h, stride, nThreshold, 0, nThreshold1, ref listlistPoint);

            for (int i = 0; i < listlistPoint.Count; i++)
            {
                List<Point> listPoint = listlistPoint[i];
                if (listPoint.Count < nMinSize)
                {
                    foreach(var pt in listPoint)
                    {
                        image[pt.Y * stride + pt.X] = 0;
                    }
                }
            }
        }
        public void FindBumpBright(byte[] image, int w, int h, int stride, int nThreshold, int nMinSize, int nThreshold1, ref List<List<Point>> listlistPoint, List<Point> MaskPoints)
        {
            FindBlobBright(image, w, h, stride, nThreshold, nMinSize, nThreshold1, ref listlistPoint, MaskPoints);
            if (this.BumpSize > 0)
            {
                for (int iter = 0; iter < listlistPoint.Count; iter++)
                {
                    var listPoint = listlistPoint[iter];
                    FindFineBlob(image, w, h, stride, BumpSize, ref listPoint);
                    listlistPoint[iter] = listPoint;
                }
            }


            //FindFineBlob(image, w, h, stride, BumpSize, ref listPoint);
            //List<Point> listPoint2 = new List<Point>();
            //for (int i = 0; i < listPoint.Count; i++)
            //    listPoint2.Add(listPoint[i]);
        }
        public List<Point> MaskImageBright(byte[] image, int w, int h, int stride, int nThreshold, Rectangle mask)
        {
            List<Point> listPoint = new List<Point>();
            int nTop = (int)mask.Top;
            int nBottom = (int)mask.Bottom;
            int nLeft = (int)mask.Left;
            int nRight = (int)mask.Right;
            if (nTop < 0)
            {
                nTop = 0;
            }

            if (nBottom >= h)
            {
                nBottom = h - 1;
            }

            if (nTop >= nBottom)
            {
                return listPoint;
            }

            if (nLeft < 0)
            {
                nLeft = 0;
            }

            if (nRight >= w)
            {
                nRight = w - 1;
            }

            if (nLeft >= nRight)
            {
                return listPoint;
            }

            for (int y = nTop; y < nBottom; y++)
            {
                for (int x = nLeft; x < nRight; x++)
                {
                    if (image[y * stride + x] > nThreshold)
                    {
                        listPoint.Add(new Point(x, y));
                    }
                }
            }
            return listPoint;
        }


        public List<Point> MaskImageDark(byte[] image, int w, int h, int stride, int nThreshold, Rectangle mask)
        {

            List<Point> listPoint = new List<Point>();
            int nTop = (int)(mask.Top + 0.999999);
            int nBottom = (int)(mask.Bottom);
            int nLeft = (int)(mask.Left + 0.9999999);
            int nRight = (int)(mask.Right + 0.999999);
            if (nTop < 0)
            {
                nTop = 0;
            }

            if (nBottom >= h)
            {
                nBottom = h - 1;
            }

            if (nTop >= nBottom)
            {
                return listPoint;
            }

            if (nLeft < 0)
            {
                nLeft = 0;
            }

            if (nRight >= w)
            {
                nRight = w - 1;
            }

            if (nLeft >= nRight)
            {
                return listPoint;
            }

            for (int y = nTop; y < nBottom; y++)
            {
                for (int x = nLeft; x < nRight; x++)
                {
                    if (image[y * stride + x] < nThreshold)
                    {
                        listPoint.Add(new Point(x, y));
                    }
                }
            }
            return listPoint;
        }
        public void Dilate(byte[] img, int w, int h, int stride, int size, List<List<Point>> listlistPoint, byte value)
        {
            foreach (var points in listlistPoint)
            {
                foreach (var pt in points)
                {
                    for (int y = pt.Y - size; y <= pt.Y + size; y++)
                    {
                        for (int x = pt.X - size; x <= pt.X + size; x++)
                        {
                            if (x >= 0 && x < w && y >= 0 && y < h)
                            {
                                img[y * stride + x] = value;
                            }
                        }
                    }

                }
            }

        }
        public void MakeRoiMaskedImage(byte[] orgImage, int w, int h, int stride, out byte[] forigeImage, List<List<Point>> listlistPoint)
        {
            int ncount = stride * h;
            byte bValue = 0;

            byte[] forigeImageTemp = new byte[ncount];
            //Array.Copy(forigeImage, orgImage, ncount);
            if (this.UseROI)
            {
                int nWidth = this.ROI.Width;
                for (int y = this.ROI.Top; y <= this.ROI.Bottom; y++)
                {
                    int nFirstIndex = y * stride + this.ROI.Left;
                    Array.Copy(orgImage, nFirstIndex, forigeImageTemp, nFirstIndex, nWidth);
                }
            }
            else
            {

                Array.Copy(orgImage, forigeImageTemp, ncount);
            }
            if (listlistPoint != null)
            {
                for (int i = 0; i < listlistPoint.Count; i++)
                {
                    List<Point> listPoint = listlistPoint[i];
                    listPoint.GroupBy(x => x.X).ToList().ForEach(x =>
                    {
                        int yMin = x.Min(y => y.Y);
                        int YMax = x.Max(t => t.Y);
                        int xCurrent = x.Key;
                        for (int y = yMin; y < YMax; y++)
                        {
                            Point pt = new Point(xCurrent, y);
                            int nIndex = pt.Y * stride + pt.X;

                            forigeImageTemp[nIndex] = 0;
                        }
                    });


                    //for (int j = 0; j < listPoint.Count; j++)
                    //{
                    //    Point pt = listPoint[j];
                    //    int nIndex = pt.Y * stride + pt.X;
                    //    bumpImage[nIndex] = orgImage[nIndex];
                    //    forigeImage[nIndex] = 0;
                    //}
                }
            }
            forigeImage = forigeImageTemp;

        }
        public void FindBlobBrightForBump(byte[] image, int w, int h, int stride, int nThreshold, int nMinArea, ref List<List<Point>> listlistPoint, List<Point> searchPoints)
        {
            List<Point> defect = new List<Point>();
            //foreach (var v in searchPoints)
            //{
            //    int nIndex = v.X + v.Y * stride;
            //    if (nIndex < image.Length)
            //    {
            //        if(image[nIndex] >= nThreshold)
            //        {
            //            defect.Add(v);
            //        }
            //    }
            //}

            searchPoints.GroupBy(x => x.X).ToList().ForEach(x =>
            {
                int yMin = x.Min(y => y.Y);
                int YMax = x.Max(t => t.Y);
                int xCurrent = x.Key;
                if (xCurrent < 0 || xCurrent >= w)
                {
                    return;
                }
                if (yMin < 0 || YMax >= h)
                {
                    return;
                }
                if (this.ROI.Left > xCurrent || this.ROI.Right < xCurrent)
                {
                    return;
                }
                if (this.ROI.Top > yMin || this.ROI.Bottom < YMax)
                {
                    return;
                }
                for (int y = yMin; y <= YMax; y++)
                {
                    int nIndex = xCurrent + y * stride;
                    if (nIndex < image.Length)
                    {
                        if (image[nIndex] >= nThreshold)
                        {
                            defect.Add(new Point(xCurrent, y));
                        }
                    }
                }
            });
            if (defect.Count >= nMinArea)
            {
                listlistPoint.Add(defect);
            }
        }

        public void FindBlobBrightForBump(byte[] image, int w, int h, int stride, int nThreshold, int nMinArea, ref List<List<Point>> listlistPoint, List<List<Point>> searchPointsList)
        {
            listlistPoint = new List<List<Point>>();
            if (searchPointsList != null)
            {
                foreach (var v in searchPointsList)
                {

                    FindBlobBrightForBump(image, w, h, stride, nThreshold, nMinArea, ref listlistPoint, v);
                }
            }

        }
        public void FindNearBlob(byte[] image, int h, int w, int stride, int nThreshold, List<Point> points, out List<Point> pointsBlob, int nLinkDistance)
        {
            pointsBlob = new List<Point>();
            int right = points.Max(t => t.X);
            int height = points.Max(t => t.Y) - points.Min(t => t.Y);
            int top = points.Min(t => t.Y);
            int left = points.Min(t => t.X);
            int bottom = points.Min(t => t.Y);
            int widht = points.Max(t => t.X) - points.Min(t => t.X);
            for (int y = 1; y <= nLinkDistance + height; y++)
            {
                if (y + top < h)
                {
                    int nIndex = (y + top) * stride + right;
                    for (int x = 1; x <= nLinkDistance; x++)
                    {
                        if (x + right > 0 && x + right < w)
                        {
                            if (image[nIndex + x] >= nThreshold)
                            {
                                pointsBlob.Add(new Point(x + right, y + top));
                            }
                        }

                    }
                }
            }


            for (int y = 1; y <= nLinkDistance; y++)
            {
                if (y + bottom < h)
                {
                    int nIndex = (y + bottom) * stride + left;

                    for (int x = -nLinkDistance; x <= nLinkDistance; x++)
                    {
                        if (x + left > 0 && x + left < w)
                        {
                            if (image[nIndex + x] >= nThreshold)
                            {
                                pointsBlob.Add(new Point(x + left, y + bottom));
                            }

                        }
                    }
                }

            }






        }
        public void FindBlobBrightWithLink(byte[] image, int w, int h, int stride, int nThreshold, int nMinSize, int nThreshold1, ref List<List<Point>> listlistPoint, bool[] bVisit, int nLinkDistance = 1)
        {
            List<Point> listPoint = new List<Point>();
            listlistPoint.Clear();

            int left = 0;
            int right = w;
            int top = 0;
            int bottom = h;


            GetRoi(w, h, ref top, ref bottom, ref left, ref right);
            if (w != _width || h != _height)
            {
                _width = w;
                _height = h;
                stack = new Point[100000000];
            }

            for (int y = top; y < bottom; y++)
            {
                for (int x = left + nLinkDistance; x < right; x++)
                {
                    if (bVisit[y * stride + x] == true)
                        continue;
                    if (image[y * stride + x] < nThreshold)
                    {
                        bVisit[y * stride + x] = true;
                        continue;
                    }
                    
                    listPoint = new List<Point>();
                    FindBlobBright(image, w, h, stride, x, y, nThreshold, ref listPoint, ref bVisit, nLinkDistance);

                    MaxArea = 100000000;
                    if (listPoint.Count > nMinSize && listPoint.Count <= MaxArea)
                    {
                        listlistPoint.Add(listPoint);
                    }

                }
            }
        }
        public void FindBlobBright(byte[] image, int w, int h, int stride, int nThreshold, int nMinSize, int nThreshold1
            , ref List<List<Point>> listlistPoint, List<Point> MaskPoints = null, int nLinkDistance = 1)
        {
            listlistPoint = listlistPoint ?? new List<List<Point>>();
            listlistPoint.Clear();

            int left = 0;
            int right = w;
            int top = 0;
            int bottom = h;

            GetRoi(w, h, ref top, ref bottom, ref left, ref right);

            GCHandle handle = GCHandle.Alloc(image, GCHandleType.Pinned);
            try
            {
                using (var src = OpenCvSharp.Mat.FromPixelData(h, w, OpenCvSharp.MatType.CV_8UC1, handle.AddrOfPinnedObject(), stride))
                using (var binary = new OpenCvSharp.Mat())
                using (var labels = new OpenCvSharp.Mat())
                using (var stats = new OpenCvSharp.Mat())
                using (var centroids = new OpenCvSharp.Mat())
                {
                    OpenCvSharp.Cv2.Threshold(src, binary, nThreshold, 255, OpenCvSharp.ThresholdTypes.Binary);

                    if (UseROI)
                    {
                        using (OpenCvSharp.Mat roiMask = OpenCvSharp.Mat.Zeros(h, w, OpenCvSharp.MatType.CV_8UC1))
                        {
                            int roiWidth = Math.Max(0, right - left);
                            int roiHeight = Math.Max(0, bottom - top);
                            if (roiWidth > 0 && roiHeight > 0)
                            {
                                var roiRect = new OpenCvSharp.Rect(left, top, roiWidth, roiHeight);
                                roiMask[roiRect].SetTo(255);
                                OpenCvSharp.Cv2.BitwiseAnd(binary, roiMask, binary);
                            }
                            else
                            {
                                binary.SetTo(0);
                            }
                        }
                    }

                    if (MaskPoints != null && MaskPoints.Count > 0)
                    {
                        foreach (var pt in MaskPoints)
                        {
                            if (pt.X >= 0 && pt.X < w && pt.Y >= 0 && pt.Y < h)
                            {
                                binary.Set<byte>(pt.Y, pt.X, 0);
                            }
                        }
                    }

                    int count = OpenCvSharp.Cv2.ConnectedComponentsWithStats(binary, labels, stats, centroids, OpenCvSharp.PixelConnectivity.Connectivity8, OpenCvSharp.MatType.CV_32S);
                    if (count <= 1)
                    {
                        return;
                    }

                    bool[] valid = new bool[count];
                    bool hasValid = false;
                    for (int i = 1; i < count; i++)
                    {
                        int area = stats.Get<int>(i, (int)OpenCvSharp.ConnectedComponentsTypes.Area);
                        if (area >= nMinSize && area <= MaxArea)
                        {
                            valid[i] = true;
                            hasValid = true;
                        }
                    }

                    if (!hasValid)
                    {
                        return;
                    }

                    if (nLinkDistance <= 1)
                    {
                        // 고속 경로(2026-07-12): 링크 거리 1 이하면 모폴로지(Close)가 없으므로 유효 성분만 남긴
                        // 마스크를 다시 라벨링(두 번째 ConnectedComponents)해도 성분 구성·래스터 순서가 그대로다
                        // (라벨 번호만 재부여). 따라서 첫 라벨링 결과에서 바로 점을 수집하면 결과가 동일하고,
                        // 131MP 기준 마스크 재구성 1패스 + 재라벨링 + 재스캔(약 500ms)이 통째로 사라진다.
                        var fastBlobs = new List<Point>[count];
                        for (int i = 1; i < count; i++)
                        {
                            if (valid[i])
                            {
                                int area = stats.Get<int>(i, (int)OpenCvSharp.ConnectedComponentsTypes.Area);
                                fastBlobs[i] = new List<Point>(area);
                            }
                        }

                        unsafe
                        {
                            int* labelPtr = (int*)labels.Data;
                            long labelStep = labels.Step() / sizeof(int);
                            for (int y = 0; y < h; y++)
                            {
                                int* rowPtr = labelPtr + y * labelStep;
                                for (int x = 0; x < w; x++)
                                {
                                    int label = rowPtr[x];
                                    if (label > 0 && label < count && valid[label])
                                    {
                                        fastBlobs[label].Add(new Point(x, y));
                                    }
                                }
                            }
                        }

                        for (int i = 1; i < count; i++)
                        {
                            var blob = fastBlobs[i];
                            if (blob != null && blob.Count >= nMinSize)
                            {
                                listlistPoint.Add(blob);
                            }
                        }
                        return;
                    }

                    using (var filtered = new OpenCvSharp.Mat(binary.Size(), OpenCvSharp.MatType.CV_8UC1, OpenCvSharp.Scalar.All(0)))
                    {
                        unsafe
                        {
                            int* labelPtr = (int*)labels.Data;
                            byte* filteredPtr = (byte*)filtered.Data;
                            long labelStep = labels.Step() / sizeof(int);
                            long filteredStep = filtered.Step();

                            for (int y = 0; y < h; y++)
                            {
                                int* rowPtr = labelPtr + y * labelStep;
                                byte* filteredRow = filteredPtr + y * filteredStep;
                                for (int x = 0; x < w; x++)
                                {
                                    int label = rowPtr[x];
                                    if (label > 0 && label < count && valid[label])
                                    {
                                        filteredRow[x] = 255;
                                    }
                                }
                            }
                        }

                        if (nLinkDistance > 1)
                        {
                            int kernelSize = Math.Max(1, nLinkDistance * 2 + 1);
                            using (var kernel = OpenCvSharp.Cv2.GetStructuringElement(OpenCvSharp.MorphShapes.Rect, new OpenCvSharp.Size(kernelSize, kernelSize)))
                            {
                                OpenCvSharp.Cv2.MorphologyEx(filtered, filtered, OpenCvSharp.MorphTypes.Close, kernel);
                            }
                        }

                        using (var finalLabels = new OpenCvSharp.Mat())
                        using (var finalStats = new OpenCvSharp.Mat())
                        using (var finalCentroids = new OpenCvSharp.Mat())
                        {
                            int finalCount = OpenCvSharp.Cv2.ConnectedComponentsWithStats(filtered, finalLabels, finalStats, finalCentroids, OpenCvSharp.PixelConnectivity.Connectivity8, OpenCvSharp.MatType.CV_32S);
                            if (finalCount <= 1)
                            {
                                return;
                            }

                            bool[] finalValid = new bool[finalCount];
                            for (int i = 1; i < finalCount; i++)
                            {
                                int area = finalStats.Get<int>(i, (int)OpenCvSharp.ConnectedComponentsTypes.Area);
                                if (area >= nMinSize && area <= MaxArea)
                                {
                                    finalValid[i] = true;
                                }
                            }

                            var blobLists = new List<Point>[finalCount];
                            for (int i = 1; i < finalCount; i++)
                            {
                                if (finalValid[i])
                                {
                                    blobLists[i] = new List<Point>();
                                }
                            }

                            unsafe
                            {
                                int* labelPtr = (int*)finalLabels.Data;
                                long step = finalLabels.Step() / sizeof(int);

                                for (int y = 0; y < h; y++)
                                {
                                    int* rowPtr = labelPtr + y * step;
                                    for (int x = 0; x < w; x++)
                                    {
                                        int label = rowPtr[x];
                                        if (label > 0 && label < finalCount && finalValid[label])
                                        {
                                            blobLists[label].Add(new Point(x, y));
                                        }
                                    }
                                }
                            }

                            for (int i = 1; i < finalCount; i++)
                            {
                                var blob = blobLists[i];
                                if (blob != null && blob.Count >= nMinSize)
                                {
                                    listlistPoint.Add(blob);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }
        private void FindBlobBright(byte[] image, int w, int h, int stride, int x, int y, int nThreshold, ref List<Point> listPoint, ref bool[] bVisit, int nLinkDistance)
        {
            int nStackCount = 0;
            stack[0] = new Point(x, y);
            nStackCount++;
            bVisit[y * stride + x] = true;
            listPoint.Add(new Point(x, y));

            while (nStackCount > 0)
            {
                Point pt = stack[nStackCount - 1];
                nStackCount--;
                int x1 = pt.X;
                int y1 = pt.Y;

                for (int itery = y1 - nLinkDistance; itery <= y1 + nLinkDistance; itery++)
                {
                    for (int iterx = x1 - nLinkDistance; iterx <= x1 + nLinkDistance; iterx++)
                    {
                        if (iterx >= 0 && iterx < w && itery >= 0 && itery < h)
                        {
                            int nIndex = iterx + itery * stride;
                            if (bVisit[nIndex] == false)
                            {
                                bVisit[nIndex] = true; // Mark as visited to avoid re-processing
                                if (image[nIndex] >= nThreshold)
                                {
                                    stack[nStackCount].X = iterx;
                                    stack[nStackCount].Y = itery;

                                    nStackCount++;

                                    if (listPoint.Count < 100000000)
                                    {
                                        listPoint.Add(new Point(iterx, itery));
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private void FindBlobDark(byte[] image, int w, int h, int stride, int x, int y, int nThreshold, ref List<Point> listPoint, ref bool[] bVisit)
        {
            int nStackCount = 0;
            stack[nStackCount] = new Point(x, y);
            nStackCount++;
            bVisit[y * stride + x] = true;
            listPoint.Add(new Point(x, y));

            while (nStackCount > 0)
            {
                Point pt = stack[nStackCount - 1];
                nStackCount--;
                int x1 = pt.X;
                int y1 = pt.Y;

                if (x1 > 0 && bVisit[y1 * stride + x1 - 1] == false && image[y1 * stride + x1 - 1] <= nThreshold)
                {
                    stack[nStackCount] = new Point(x1 - 1, y1);
                    nStackCount++;

                    bVisit[y1 * stride + x1 - 1] = true;
                    listPoint.Add(new Point(x1 - 1, y1));
                }
                if (x1 < w - 1 && bVisit[y1 * stride + x1 + 1] == false && image[y1 * stride + x1 + 1] <= nThreshold)
                {
                    stack[nStackCount] = new Point(x1 + 1, y1);
                    nStackCount++;

                    bVisit[y1 * stride + x1 + 1] = true;
                    listPoint.Add(new Point(x1 + 1, y1));
                }
                if (y1 > 0 && bVisit[(y1 - 1) * stride + x1] == false && image[(y1 - 1) * stride + x1] <= nThreshold)
                {
                    stack[nStackCount] = new Point(x1, y1 - 1);
                    nStackCount++;

                    bVisit[(y1 - 1) * stride + x1] = true;
                    listPoint.Add(new Point(x1, y1 - 1));
                }
                if (y1 < h - 1 && bVisit[(y1 + 1) * stride + x1] == false && image[(y1 + 1) * stride + x1] <= nThreshold)
                {
                    stack[nStackCount] = new Point(x1, y1 + 1);
                    nStackCount++;

                    bVisit[(y1 + 1) * stride + x1] = true;
                    listPoint.Add(new Point(x1, y1 + 1));
                }
            }

        }
        public void FindBlobDark(byte[] image, int w, int h, int stride, int nThreshold, int nMinSize, int nThreshold1, ref List<List<Point>> listlistPoint, bool[] bVisit)
        {
            List<Point> listPoint = new List<Point>();
            listlistPoint.Clear();

            int left = 0;
            int right = w;
            int top = 0;
            int bottom = h;


            GetRoi(w, h, ref top, ref bottom, ref left, ref right);
            if (w != _width || h != _height)
            {
                _width = w;
                _height = h;
                stack = new Point[_width * _height];
            }


            for (int y = top; y < bottom; y++)
            {
                for (int x = left; x < right; x++)
                {
                    if (bVisit[y * stride + x] == true)
                        continue;
                    if (image[y * stride + x] > nThreshold)
                        continue;
                    listPoint = new List<Point>();
                    FindBlobDark(image, w, h, stride, x, y, nThreshold, ref listPoint, ref bVisit);
                    if (listPoint.Count >= nMinSize && listPoint.Count <= MaxArea)
                    {
                        listlistPoint.Add(listPoint);
                    }
                }
            }
        }
        public void FindBlobDark(byte[] image, int w, int h, int stride, int nThreshold, int nMinSize, int nThreshold1, ref List<List<Point>> listlistPoint)
        {
            bool element = false;
            int count = stride * h;
            bool[] bVisit = null;

            int left = 0;
            int right = w;
            int top = 0;
            int bottom = h;


            GetRoi(w, h, ref top, ref bottom, ref left, ref right);


            if (this.IsFindBlobInROI)
            {
                element = true;
                bVisit = new bool[count];

                for (int iter = 0; iter < count; iter++)
                {
                    bVisit[iter] = true;
                }


                for (int y = top; y < bottom; y++)
                {
                    for (int x = left; x < right; x++)
                    {
                        bVisit[y * stride + x] = false;
                    }
                }
            }
            else
            {
                bVisit = new bool[count];
                for (int iter = 0; iter < count; iter++)
                {
                    bVisit[iter] = false;
                }
            }

            FindBlobDark(image, w, h, stride, nThreshold, nMinSize, nThreshold1, ref listlistPoint, bVisit);
        }
    }
}
