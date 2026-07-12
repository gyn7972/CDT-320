using QMC.Common;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
//using OpenCvSharp;

namespace QMC.Vision.Inspector
{
    public class CDTInspector
    {
        /// <summary>
        /// EventSearchDieEnd — Bottom 외곽(패턴) 탐색이 끝나는 즉시 X/Y/T + W/H 를 통지한다
        /// (칩핑/이물 CUDA 검사 '이전' 발화 — Side 공정이 결과 완료를 기다리지 않고 좌표를 쓰게 함).
        /// 인자: (x, y, angleDeg, indexX, indexY, wMm, hMm). x/y 는 최종 result.Offset 과 동일 규약
        /// (ChipRoi 오프셋 + 0.5 스케일 + X/Y 스왑, 픽셀). w/h 는 최종 result.Width/Height 와 동일 값
        /// (mm 변환 ÷2 + W↔H 스왑 적용, 2026-07-12). angle 이 NaN 이면 외곽 미검출.
        /// 구독자는 즉시 반환할 것(무거운 작업은 구독자 측에서 비동기 처리).
        /// 코어 알고리즘 로직은 변경하지 않는다 — 좌표/크기 '사본' 으로 계산해 원본 결과 흐름에 영향 없음.
        /// </summary>
        public static event Action<float, float, double, int, int, double, double> SearchDieEnd;

        // CUDA DLL 함수 선언
        [System.Runtime.InteropServices.DllImport("MakePixelShiftImage.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern int Upscale2xBilinear(
            byte[] input, int inWidth, int inHeight,
            byte[] output);

        [DllImport("MakePixelShiftImage.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int UpscaleROI2xBilinear(
      byte[] input, int inWidth, int inHeight,
      int roiX, int roiY, int roiWidth, int roiHeight,
      IntPtr output2D);
        [DllImport("MakePixelShiftImage.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int UpscaleROI2xBilinearAndSobel(
      byte[] input, int inWidth, int inHeight,
      int roiX, int roiY, int roiWidth, int roiHeight,
      IntPtr output2D, IntPtr outputSobel);

        // 컨텍스트 버전(2026-07-11) — 디바이스 버퍼를 컨텍스트(CudaContextPool)가 보유, 호출마다 할당 없음.
        [DllImport("MakePixelShiftImage.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int UpscaleROI2xBilinearAndSobelCtx(
      IntPtr ctx,
      byte[] input, int inWidth, int inHeight,
      int roiX, int roiY, int roiWidth, int roiHeight,
      IntPtr output2D, IntPtr outputSobel);

        [DllImport("MakePixelShiftImage.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int UpscaleROI2xBilinear_Init(int inWidth, int inHeight, int roiWidth, int roiHeight);

        [DllImport("MakePixelShiftImage.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UpscaleROI2xBilinear_Release();

        private VisionConfig _visionConfig = new VisionConfig();
        private QMC_BlobTool _blobTool = new QMC_BlobTool();
        public QMC_FindChippingNForeign FindChippingNForeign { get; set; } = new QMC_FindChippingNForeign();
        public int BlockSize { get; set; } = 32; // 캐시 효율을 위한 블록 크기(조정 가능)
        public double dXdef { get; private set; }
        public double dYdef { get; private set; }

        double widthMM = 0.0;
        double heightMM = 0.0;
        int chipIndex = 0;
        static object CodaLock;

        public bool bSimulate = false;
        static int nInstanceCount = 0;

        public double PeekValueThreshold = 20;
        public double dFirtPeekValueThreshold = 230;
        public double dStdEv { get; set; } =  0.01;
        public int nChipOffset = 0;

        int indexX = 0;
        int indexY = 0;
        double dMaxDefactSize = 0;

        public CDTInspector()
        {
            if(CodaLock  == null)
            {
                CodaLock = new object();
                UpscaleROI2xBilinear_Init(12000, 12000, 12000, 12000);
            }
            
        }

        public void SetVisionConfig(VisionConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            Log.Write("SideVisionConfig" ,"Front", config.SideVisionFront.ToString());
            Log.Write("SideVisionConfig", "Back", config.SideVisionBack.ToString());
            _visionConfig = config;
        }

        private List<List<Point>> GroupAdjacentPoints(List<Point> points)
        {
            var groups = new List<List<Point>>();
            if (points == null || !points.Any())
            {
                return groups;
            }

            var unvisited = new HashSet<Point>(points);

            while (unvisited.Any())
            {
                var currentGroup = new List<Point>();
                var queue = new Queue<Point>();

                var startPoint = unvisited.First();
                queue.Enqueue(startPoint);
                unvisited.Remove(startPoint);
                currentGroup.Add(startPoint);

                while (queue.Any())
                {
                    var currentPoint = queue.Dequeue();

                    // 8방향 인접 픽셀 확인
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            var neighbor = new Point(currentPoint.X + dx, currentPoint.Y + dy);

                            if (unvisited.Contains(neighbor))
                            {
                                unvisited.Remove(neighbor);
                                queue.Enqueue(neighbor);
                                currentGroup.Add(neighbor);
                            }
                        }
                    }
                }
                groups.Add(currentGroup);
            }

            return groups;
        }
        
        public SideResult SideInspect(SideInspectionParameter bip)
        {
            SideResult sideResult = new SideResult();
            
            try
            {
                if(bip.Images.Count > 0)
                {
                    int nThreshold = bip.Threshold;
                    if (nThreshold <= 0) nThreshold = 110;
                    
                    // 이미지 준비
                    var img = MakeRoiImage(new Rectangle(0, 0, bip.ImageWidth, bip.ImageHeight), 
                                           bip.Images[0], bip.ImageWidth);
                    
                    // 사이드 치핑 검사기 생성 및 실행
                    var sideChippingInspector = new SideChippingInspector(_visionConfig);
                    var chippingResult = sideChippingInspector.InspectChipping(
                        ref img, bip.ImageWidth, bip.ImageHeight, bip);

                    Log.Write("SideResult_" + bip.WaferID, "X , " + bip.IndexX.ToString()
                        + ",Y , " + bip.IndexY.ToString()
                        + "," + chippingResult.MaxChippingSize.ToString());

                    if (chippingResult.IsSuccess)
                    {
                        // 결과 설정
                        if (chippingResult.IsDefect)
                        {
                            sideResult.DefectCode = 5; // 치핑 불량
                            
                           
                            
                            Log.Write("SideInspect", 
                                $"치핑 불량 - Top: {chippingResult.TopChippingSize:F4}mm, " +
                                $"Bottom: {chippingResult.BottomChippingSize:F4}mm");
                        }
                        else
                        {
                            sideResult.DefectCode = 0; // 양품

                        }
                    }
                    else
                    {
                        sideResult.DefectCode = 10; // 검사 실패
                    }
                    // 치핑 정보 추가
                    if (chippingResult.TopChippingSize > 0)
                    {
                        sideResult.ChippingInfos.Add(new ChippingInfo
                        {
                            Depth = chippingResult.TopChippingSize,
                            Length = chippingResult.TopChippingSize
                        });
                    }

                    if (chippingResult.BottomChippingSize > 0)
                    {
                        sideResult.ChippingInfos.Add(new ChippingInfo
                        {
                            Depth = chippingResult.BottomChippingSize,
                            Length = chippingResult.BottomChippingSize
                        });
                    }
                    // 기존 이미지 저장 로직
                    //if(bip.IsSaveGoodImage)
                    {
                        SaveImageSide(bip, img, sideResult.DefectCode ==0);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("SideInspect", $"검사 중 오류: {ex.Message}");
                Log.Write(ex);
                sideResult.DefectCode = 10;
            }
            
            return sideResult;
        }

        private void SaveImageSide(SideInspectionParameter bip, byte[,] ShiftImage,bool bOK = true)
        {
            try
            {
                if(bip.Images.Count > 0)
                {
                    foreach(var image in bip.Images)
                    {

                        string strFileName = "d:\\Log\\SideImage";

                        IfNotExistMakeFolder(strFileName);

                        strFileName += "\\" + bip.WaferID;
                        IfNotExistMakeFolder(strFileName);

                        if(bOK)
                        {

                            strFileName += "\\" +"OK";
                            IfNotExistMakeFolder(strFileName);

                        }
                        else
                        {
                            strFileName += "\\" + "NG";
                            IfNotExistMakeFolder(strFileName);

                        }

                        lock (CodaLock)
                            {
                                SaveIndex++;
                            }
                        strFileName += "\\_X-" + bip.IndexX.ToString() + "_Y-" + bip.IndexY.ToString() + "_" + bip.ColletID.ToString() + "_" + SaveIndex.ToString();
                        

                        SaveImage(ShiftImage, bip.ImageWidth, bip.ImageHeight, strFileName);
                    }
                }
                

            }
            catch (Exception e)
            {
            }
        }
        Point FindChipCenter(BottomInspectionParameter bip)
        {
            
            Point ptCenter =  new Point(bip.ImageWidth/2, bip.ImageHeight/2);
            byte[] img = bip.Images[0];
            int y = bip.ImageHeight / 2;
            int xFindLeft = 0;
            int xFindRight = bip.ImageWidth;
            int refY = y * bip.ImageWidth;
            int yTop = 0;
            int yBottom = bip.ImageHeight;
            bool bfindDark = false;
            int nDarkCount = 0;
            int nWhiteCount = 0;
            int nThreshold = 160;
            for (int iter = bip.ImageWidth / 2; iter>0; iter--)
            {
                int xLeft = iter;
                byte value = img[refY + xLeft];
                if (value < 80)
                {
                    nDarkCount++;
                }

                if(nDarkCount > 20)
                {

                    xFindLeft = xLeft;
                    break;
                    bfindDark = true;
                }
                if(bfindDark)
                {
                    if (value > nThreshold)
                    {
                        nWhiteCount++;
                    }
                    if (nWhiteCount > 20)
                    {
                        xFindLeft = xLeft;
                        break;
                    }
                }
                
            }
            bfindDark = false;
            nDarkCount = 0;
            nWhiteCount = 0;
            for (int iter = bip.ImageWidth / 2; iter >0 ; iter--)
            {
                int xLeft = (bip.ImageWidth - iter-1);
                byte value = img[refY + xLeft];
                if (value < 80)
                {
                    nDarkCount++;
                }

                if (nDarkCount > 20)
                {

                    xFindRight = xLeft;
                    break;
                    bfindDark = true;
                }
                if (bfindDark)
                {
                    if (value > nThreshold)
                    {
                        nWhiteCount++;
                    }
                    if (nWhiteCount > 20)
                    {
                        xFindRight = xLeft;
                        break;
                    }
                }
            }
           
            int nWidth = bip.ImageWidth;
            bfindDark = false;
            nDarkCount = 0;
            nWhiteCount = 0;
            int xRef = nWidth / 2;
            for (int iter = bip.ImageHeight / 2; iter >0; iter--)
            {
                int yTemp = iter;
                byte value = img[yTemp * nWidth + xRef];
                if (value < 80)
                {
                    nDarkCount++;
                }

                if (nDarkCount > 20)
                {
                    yTop = yTemp;
                    break;
                    bfindDark = true;
                }
                if (bfindDark)
                {
                    if (value > nThreshold)
                    {
                        nWhiteCount++;
                    }
                    if (nWhiteCount > 20)
                    {
                        yTop = yTemp;
                        break;
                    }
                }
            }

            bfindDark = false;
            nDarkCount = 0;
            nWhiteCount = 0;
            for (int iter = bip.ImageHeight / 2; iter > 0; iter--)
            {
                int yTemp = (bip.ImageHeight - iter -1);
                byte value = img[yTemp * nWidth + xRef];
                if (value < 80)
                {
                    nDarkCount++;
                }

                if (nDarkCount > 20)
                {

                    yBottom = yTemp;
                    break;
                    bfindDark = true;
                }
                if (bfindDark)
                {
                    if (value > nThreshold)
                    {
                        nWhiteCount++;
                    }
                    if (nWhiteCount > 20)
                    {
                        yBottom = yTemp;
                        break;
                    }
                }
            }
            return ptCenter = new Point((xFindLeft + xFindRight) / 2, (yTop + yBottom) / 2); 
        }
        public BottomResult BottomInspectForFourCorner(BottomInspectionParameter bip)
        {
            BottomResult result = new BottomResult();
            bip.ChippingDepth = 0;
            try
            {
                result = BottomInspect(bip);
                if (result == null || result.DefectCode == 10)
                {
                    SizeF dTemp = bip.ChipLowerSpecLimit;
                    bip.ChipLowerSpecLimit = new SizeF(dTemp.Height, dTemp.Width);
                    dTemp = bip.ChipUpperSpecLimit;
                    bip.ChipUpperSpecLimit = new SizeF(dTemp.Height, dTemp.Width);

                    bip.ChipRoi = new Rectangle(bip.ChipRoi.X, bip.ChipRoi.Y, bip.ChipRoi.Height, bip.ChipRoi.Width);
                    result = BottomInspect(bip);
                }
                return result;
            }
            catch (Exception ex)
            {
                Log.Write(ex);
               
               
            }
            return null;



        }

        /// <summary>SearchDieEnd 발화 — 최종 result.Offset/Angle/Width/Height 와 동일 규약의 X/Y/T/W/H 를
        /// 사본으로 선계산해 통지. 실패해도 검사 흐름에 영향 주지 않는다(로그만).</summary>
        private void RaiseSearchDieEnd(BottomInspectionParameter bip, QMC_ResultChppingNForeign best, List<QMC_ResultChppingNForeign> vList)
        {
            try
            {
                var handler = SearchDieEnd;
                if (handler == null || best == null || bip == null)
                    return;

                // 아래 본류(코너 보정부)와 동일한 보정을 '사본'에 적용 — 원본 포인트는 본류가 나중에 직접 보정한다.
                PointF lt = best.LeftTop, rt = best.RightTop, rb = best.RightBottom, lb = best.LeftBottom;
                OffsetPointFAndReSize(ref lt, bip.ChipRoi, 0.5);
                OffsetPointFAndReSize(ref rt, bip.ChipRoi, 0.5);
                OffsetPointFAndReSize(ref rb, bip.ChipRoi, 0.5);
                OffsetPointFAndReSize(ref lb, bip.ChipRoi, 0.5);
                float cx = (lt.X + rt.X + rb.X + lb.X) / 4f;
                float cy = (lt.Y + rt.Y + rb.Y + lb.Y) / 4f;
                double angle = best.GetAngle();   // NaN = 외곽 미검출(구독자 판단)

                // W/H 사본 선계산 — 본류와 동일 수식: 픽셀 평균(vList 상위 2) → mm 변환(×PixelSize÷2).
                // (2026-07-12 deece5d5 'W/H 바꾸는 코드 삭제' 반영 — 스왑 없이 최종 result.Width/Height 와 동일 값.)
                double wPx = vList != null && vList.Count > 0 ? vList.Average(t => t.w) : 0;
                double hPx = vList != null && vList.Count > 0 ? vList.Average(t => t.h) : 0;
                double wMm = wPx * _visionConfig.BottomVision.PixelSizeWidthMm / 2;
                double hMm = hPx * _visionConfig.BottomVision.PixelSizeHeightMm / 2;

                // 최종 result.Offset 규약(deece5d5 이후 스왑 없음)과 동일하게 전달.
                handler(cx, cy, angle, bip.IndexX, bip.IndexY, wMm, hMm);
            }
            catch (Exception ex)
            {
                Log.Write("VisionInspector", "SearchDieEnd 통지 실패(검사 흐름에는 영향 없음): " + ex.Message);
            }
            finally
            {
            }
        }

        // 동시 진행 중인 BottomInspect 수(2026-07-12) — 내부 Parallel.For 병렬도 배분용(프로세스 전역).
        private static int _concurrentInspects = 0;

        // 페이즈 프로파일링(2026-07-12, 진단용) — QMC_PROFILE=1 일 때만 단계별 소요(ms)를 콘솔로 출력.
        private static readonly bool bProfilePhases =
            Environment.GetEnvironmentVariable("QMC_PROFILE") == "1";

        public BottomResult BottomInspect(BottomInspectionParameter bip)
        {
            var swTotalProf = bProfilePhases ? System.Diagnostics.Stopwatch.StartNew() : null;
            // 동시 검사 수에 맞춰 내부 병렬도 배분(2026-07-12) — 검사 8건 동시 진행 시 각 검사의
            // Parallel.For 가 코어 전체를 두고 경합해 tact 가 8배 이상 부풀던 문제 완화. 계산식/결과 불변.
            int nConcurrent = System.Threading.Interlocked.Increment(ref _concurrentInspects);
            QMC_FindChippingNForeign.MaxParallelism =
                Math.Max(2, Environment.ProcessorCount / Math.Max(1, nConcurrent));

            lock (CodaLock)
            {
                Defact = 0;
            }
            lock(this)
            {
                
                dMaxDefactSize = 0;
                LastBitMap = null;
            }
            bip.WaferID  = bip.WaferID.StartsWith("I_") ? bip.WaferID.Substring(2) : bip.WaferID;
            chipIndex++;
            // 검사 파라미터를 사용하여 검사 수행
            // 예: bip.Images, bip.SelectedChipType, bip.ChipDepth, bip.Threshold
            // 검사 결과를 BottomResult 객체에 저장
            BottomResult result = new BottomResult();
            QMC_ResultChppingNForeign resultChppingNForeign = new QMC_ResultChppingNForeign();
            int w = 1, h = 1;
            byte[,] ShiftImage = new byte[1, 1];

            byte[,] ShiftImageSobel = new byte[1, 1];
            // CUDA 컨텍스트 대여(2026-07-11) — 검사 1건당 1개. 이 검사의 모든 CUDA 호출(업스케일/칩핑)이
            // 같은 컨텍스트의 디바이스 버퍼를 재사용한다(같은 이미지 크기 = 할당 0회, ROI 변경 시 그 슬롯만 재할당).
            // 풀 비활성(무-CUDA/구 DLL)이면 Handle=Zero → 기존(호출마다 할당) 경로 그대로.
            CudaContextPool.Lease cudaLease = CudaContextPool.Rent();
            try
            {
                //int test = 3;
                Log.Write("VisionInspector", "BottomInspect Start");

                //MakePixelShiftImage(bip, result, out w, out h, out ShiftImage);
               
                List<QMC_ResultChppingNForeign> results = new List<QMC_ResultChppingNForeign>();
                double dChppingMargin = Math.Min(bip.ChippingDepth, bip.ChippingLength);
                dChppingMargin /= (_visionConfig.BottomVision.PixelSizeWidthMm / 2);
                List<Task<QMC_ResultChppingNForeign>> tasks = new List<Task<QMC_ResultChppingNForeign>>();
                int ImageCount = bip.Images.Count;
                ImageCount = 1;
                // 설비 확정 계약(2026-07-12, 사용자 지정): 설비에서는 항상 실제 검사 모드로 동작한다 —
                // 입력은 '1배 원본 전체' 이미지여야 하며, lib 가 FindChipCenter 로 ChipRoi 를 다이 중심에
                // 재배치해 크롭 → GPU 2배 확장 → 검사한다. (미리 2배 확장한 이미지를 넣으면 이중 확장
                // (527MP)으로 수십 초 지연 + W/H·좌표 2배 왜곡 — 공급측 BottomInspector 가 1배 원본을 넣는다.)
                bSimulate = false;
                if (bSimulate)
                {
                    ImageCount = 1;
                    Log.Write("VisionInspector", "시뮬레이션 모드로 실행");
                }
                else
                {
                    Log.Write("VisionInspector", "실제 검사 모드로 실행");
                }
                var cudaWrapper = new CudaWrapper();
                double[] profMs = new double[6];   // 0:roi 1:outline 2:cuda 3:chip 4:(예비) 5:(예비) — QMC_PROFILE=1 진단용
                for (int i = 0; i < ImageCount; i++)
                {
                    var t = Task.Factory.StartNew((obj) =>
                    {
                        int imageindex = (int)obj;
                        var vv = new QMC_ResultChppingNForeign();
                        var swProf = bProfilePhases ? System.Diagnostics.Stopwatch.StartNew() : null;
                        if (bSimulate)
                        {
                            ShiftImage = MakeRoiImage(new Rectangle(new Point(0, 0), new Size(bip.ImageWidth, bip.ImageHeight)), bip.Images[0], bip.ImageWidth); // 시뮬레이션 모드에서는 첫 번째 이미지를 사용
                            w = bip.ImageWidth; // 이미지 너비를 2배로 확장
                            h = bip.ImageHeight; // 이미지 높이를 2배로 확장

                            // Sobel 이미지 생성
                            lock (CodaLock)
                            {
                                //ShiftImageSobel = cudaWrapper.ApplySobelFilter(ShiftImage);
                            }

                            //SaveImage(ShiftImage, bip.ImageWidth, bip.ImageHeight, "d:\\temp\\C.bmp");

                            vv.shiftimage = ShiftImage;
                            vv.shiftSobelimage = ShiftImageSobel;
                        }
                        else
                        {
                            {
                                MakeSoftWareExpendImage(bip, out ShiftImage, out ShiftImageSobel, out w, out h, imageindex, cudaLease.Handle);
                                vv.shiftimage = ShiftImage;
                                vv.shiftSobelimage = ShiftImageSobel;
                            }
                        }
                        if (swProf != null) { profMs[0] = swProf.Elapsed.TotalMilliseconds; swProf.Restart(); }
                        //ShiftImage = MakeRoiImage(bip.ChipRoi, bip.Images[0], bip.ImageWidth);
                        //w = bip.ChipRoi.Width ;
                        //h = bip.ChipRoi.Height;

                        Log.Write("VisionInspector", "Image Save");

                        // SaveImage(w, h, ShiftImage);


                        FindChippingNForeign.SetChippingThreshold(bip.Threshold);

                        
                        FindChippingNForeign.dFisrtPeekValueThreshold = bip.FirstPeekValueThreshold;
                        FindChippingNForeign.nChipOffset = nChipOffset;
                        FindChippingNForeign.PeekValueThreshold = bip.PeekValueThreshold;
                        FindChippingNForeign.dStdEv = bip.Stdev;
                        
                        FindChippingNForeign.FindChipOutline(vv, w, h, vv.shiftimage, bip.SelectedChipType == InspectionParameterBase.ChipType.White);
                        if (swProf != null) profMs[1] = swProf.Elapsed.TotalMilliseconds;
                        return vv;
                    }, i);
                    tasks.Add(t);
                }
                foreach (var v in tasks)
                {
                    v.Wait();
                    var res = v.Result;
                    if (res == null) continue;
                    double dAngle = res.GetAngle();
                    if (!double.IsNaN(dAngle))
                    {
                        results.Add(res);
                    }
                }
                // Fallback: if all results were filtered out (NaN angle), use first available task result
                QMC_ResultChppingNForeign bestResult = null;
                if (results.Count >0)
                {
                    bestResult = results.OrderBy(t => t.w + t.h).FirstOrDefault();
                }
                else
                {
                    var any = tasks.Select(t => t.Result).FirstOrDefault(r => r != null);
                    bestResult = any;
                }
                if (bestResult == null)
                {
                    Log.Write("VisionInspector", "No valid chip outline result. Aborting.");
                    result.DefectCode =10;
                    return null;
                }

                // 유효(각도 non-NaN) 결과가 없으면 폴백 목록(비-null 태스크 결과)에서 취한다.
                // 기존 버그: results.Count==0 이면 nTake=0 → Take(0) → 아래 Average 가 빈 시퀀스 예외
                // → BottomInspect null → 레거시 폴백 재검사(픽커당 검사 2회)로 전체 사이클이 느려졌다.
                var vSrc = (results.Count > 0 ? results : tasks.Select(t => t.Result).Where(r => r != null)).ToList();
                int nTake = Math.Min(vSrc.Count, 2);
                var vList = vSrc.OrderBy(t => t.w + t.h).Take(nTake).ToList();

                //
                // EventSearchDieEnd — 외곽 확정 즉시 X/Y/T + W/H 통지(아래 CUDA 칩핑/이물 단계 이전).
                // W/H 픽셀 평균(vList) 확정 직후로 이동(2026-07-12) — 여전히 칩핑/이물 검사 전이다.
                RaiseSearchDieEnd(bip, bestResult, vList);
                resultChppingNForeign = bestResult;
                ShiftImage = resultChppingNForeign.shiftimage;
                ShiftImageSobel = resultChppingNForeign.shiftSobelimage;
                Log.Write("VisionInspector", "검사 완료");

                Random rand = new Random();
                result.Offset = new System.Drawing.PointF((float)rand.NextDouble(), (float)rand.NextDouble());
                result.Angle = resultChppingNForeign.GetAngle(); // 0 to 360 degrees
                result.Width = vList.Count > 0 ? vList.Average(t => t.w) : 0;
                result.Height = vList.Count > 0 ? vList.Average(t => t.h) : 0;
                bool bUssCorrectMotionBlur = false;
                if(bUssCorrectMotionBlur)
                {
                    CorrectMotionblur(result, resultChppingNForeign, w, h, ShiftImage);
                }

                // 3. CUDA를 사용하여 치핑 검사 및 마스크 생성
                // Chipping 검사 시 외곽선에서 안쪽으로 5픽셀 마진을 줍니다.
                int margin = (int)(dChppingMargin + 1);

                // 칩핑 마스크는 1차원 풀 버퍼로 수령(2026-07-12) — mask2 는 소비처가 없어 D2H/할당을 생략하고,
                // 종전의 [2D 수령 → InspectChipping 에서 1D 재복사] 였던 131MB 중간 복사도 제거(내용/결과 동일).
                byte[] chippingMask = null;

                {
                    Log.Write("Cuda", "DetectChippingWithCuda Start");
                    var swCuda = bProfilePhases ? System.Diagnostics.Stopwatch.StartNew() : null;

                    int topHatRadius = Math.Max(1, margin / 2);
                    chippingMask = cudaWrapper.DetectChippingMask1WithCuda(
                     cudaLease.Handle,   // 컨텍스트 대여 핸들 — 디바이스 버퍼 재사용(2026-07-11)
                     ShiftImage,
                     resultChppingNForeign.m_lineTop,
                     resultChppingNForeign.m_lineBottom,
                     resultChppingNForeign.m_lineLeft,
                     resultChppingNForeign.m_lineRight,
                     //(byte)bip.Threshold,
                     160,
                     margin / 2,
                     bip.TopHatRadius,
                     (byte)bip.TopHatThreshold
                     );
                    Log.Write("Cuda", "DetectChippingWithCuda END");
                    if (swCuda != null) profMs[2] = swCuda.Elapsed.TotalMilliseconds;
                }

                DateTime startTime = DateTime.Now;


                bool bChipping = false;
                bool bForeign = false;
                if (bip.ChippingDepth != 0)
                {
                    var swChip = bProfilePhases ? System.Diagnostics.Stopwatch.StartNew() : null;
                    // ctx 경로는 마스크의 다이 외곽이 0 으로 보장(cudaMemset + isInside 한정 기록) → 다이 bbox 만 블랍 탐색 가능.
                    bool maskZeroOutside = cudaLease.Handle != IntPtr.Zero;
                    var v =InspectChipping(bip, result, w, h, ShiftImage, dChppingMargin, startTime, chippingMask, resultChppingNForeign, margin / 2 + 3, maskZeroOutside);
                    bChipping = v.bChipping;
                    bForeign = v.bForeign;
                    if (swChip != null)
                    {
                        profMs[3] = swChip.Elapsed.TotalMilliseconds;
                        Console.WriteLine(string.Format("[PROF] roi={0:F1} outline={1:F1} cuda={2:F1} chip={3:F1}",
                            profMs[0], profMs[1], profMs[2], profMs[3]));
                    }
                }
                BufferPool.Return(chippingMask); chippingMask = null;   // 풀 반납 — 이후 참조 없음


                //Log.Write("4Corner", "X , Y :  " + bip.IndexX.ToString() + "_" + bip.IndexY.ToString());
                //Log.Write("4Corner", "LT : " + resultChppingNForeign.LeftTop.ToString());
                //Log.Write("4Corner", "RT : " + resultChppingNForeign.RightTop.ToString());
                //Log.Write("4Corner", "LB : " + resultChppingNForeign.LeftBottom.ToString());
                //Log.Write("4Corner", "RB : " + resultChppingNForeign.RightBottom.ToString());
                SaveOutLine(bip, ShiftImage, resultChppingNForeign);


                //bool bChipping = false;
                OffsetPointFAndReSize(ref resultChppingNForeign.LeftTop, bip.ChipRoi, 0.5);// Top-left corner
                OffsetPointFAndReSize(ref resultChppingNForeign.RightTop, bip.ChipRoi, 0.5);// Top-right corner
                OffsetPointFAndReSize(ref resultChppingNForeign.RightBottom, bip.ChipRoi, 0.5);// Bottom-right corner
                OffsetPointFAndReSize(ref resultChppingNForeign.LeftBottom, bip.ChipRoi, 0.5);// Bottom-left corner




                result.Corners[0] = resultChppingNForeign.LeftTop; // Top-left corner
                result.Corners[1] = resultChppingNForeign.RightTop; // Top-right corner
                result.Corners[2] = resultChppingNForeign.RightBottom; // Bottom-right corner
                result.Corners[3] = resultChppingNForeign.LeftBottom; // Bottom-left corner

                // 칩핑 컨투어/이물 위치도 코너와 동일 규약(×0.5 + ChipRoi 좌상단)으로 원본 입력 좌표 환산.
                // (검사는 2배 확장 이미지에서 수행 — 종전에는 컨투어가 검사 좌표 그대로 반환되어
                //  소비측(BottomInspector)에서 디펙 마크가 엉뚱한 위치에 표시됐다. 2026-07-11)
                foreach (var ci in result.ChippingInfos)
                {
                    if (ci == null || ci.Contour == null) continue;
                    for (int i = 0; i < ci.Contour.Count; i++)
                    {
                        PointF p = ci.Contour[i];
                        OffsetPointFAndReSize(ref p, bip.ChipRoi, 0.5);
                        ci.Contour[i] = p;
                    }
                }
                foreach (var fi in result.ForeignInfos)
                {
                    if (fi == null) continue;
                    fi.Rect = new RectangleF(
                        bip.ChipRoi.Left + fi.Rect.X * 0.5f,
                        bip.ChipRoi.Top + fi.Rect.Y * 0.5f,
                        fi.Rect.Width * 0.5f,
                        fi.Rect.Height * 0.5f);
                }

                double dOffsetX = 0;
                double dOffsetY = 0;
                foreach (var v in result.Corners)
                {
                    dOffsetX += v.X;
                    dOffsetY += v.Y;
                }

                dOffsetX /= 4.0;
                dOffsetY /= 4.0;
                result.Offset = new PointF((float)dOffsetX, (float)dOffsetY); // 평균 오프셋 계산

                // result.Width 와 Height가 스팩 범위 2% 초과 또는 미달시 칩 못찾음으로 처리
                double dSpcec = 0.1; // 10% 범위


                result.Width *= _visionConfig.BottomVision.PixelSizeWidthMm / 2; // mm 단위로 변환
                result.Height *= _visionConfig.BottomVision.PixelSizeHeightMm / 2; // mm 단위로 변환

               
                Log.Write("Data_" +bip.WaferID , bip.IndexX.ToString() 
                    + "," + bip.IndexY.ToString() 
                    + "," + result.Width 
                    + ", " + result.Height 
                    + "," + result.Angle);

                if (double.IsNaN(result.Angle))
                {
                    result.DefectCode = 10;
                }
                else if (result.Width > bip.ChipUpperSpecLimit.Width * (1 + dSpcec) || result.Width < bip.ChipLowerSpecLimit.Width * (1 - dSpcec))
                {
                    result.DefectCode = 10; // 예: 5는 Width가 스펙을 초과 또는 미달하는 경우
                }
                else if (result.Height > bip.ChipUpperSpecLimit.Height * (1 + dSpcec) || result.Height < bip.ChipLowerSpecLimit.Height * (1 - dSpcec))
                {
                    result.DefectCode = 11; // 예: 6은 Height가 스펙을 초과 또는 미달하는 경우
                }
                else
                {
                    result.DefectCode = 0; // 예: 0은 스펙 내에 있는 경우
                }

                if (result.DefectCode == 0)
                {
                    if (result.Width > bip.ChipUpperSpecLimit.Width)
                    {
                        result.DefectCode = 1; // 예: 1은 Width가 스펙을 초과하는 경우
                    }
                    else if (result.Width < bip.ChipLowerSpecLimit.Width)
                    {
                        result.DefectCode = 2; // 예: 2는 Width가 스펙을 미달하는 경우
                    }
                    else if (result.Height > bip.ChipUpperSpecLimit.Height)
                    {
                        result.DefectCode = 3; // 예: 3은 Height가 스펙을 초과하는 경우
                    }
                    else if (result.Height < bip.ChipLowerSpecLimit.Height)
                    {
                        result.DefectCode = 4; // 예: 4는 Height가 스펙을 미달하는 경우
                    }
                    else
                    {
                        if (double.IsNaN(result.Width) || double.IsNaN(result.Height) || double.IsNaN(result.Angle))
                        {
                            result.Width = 0; // 예: NaN인 경우 0으로 설정
                            result.Height = 0;
                            result.Angle = 0; // 예: NaN인 경우 0으로 설정
                            result.Offset = new PointF(0, 0); // 예: NaN인 경우 (0,0)으로 설정
                            result.DefectCode = 0;
                            result.DefectCode = 9;
                            return null; // 예외 발생 시 null 반환
                        }
                        else if (bChipping)
                        {
                            result.DefectCode = 5;
                        }
                        else if(bForeign)
                        {
                            result.DefectCode = 11; // 예: 0은 스펙 내에 있는 경우
                        }
                        else 
                        {
                            result.DefectCode = 0; // 예: 0은 스펙 내에 있는 경우
                        }
                    }
                }
                else
                {
                    result.DefectCode = 10;
                    return null; // 예외 발생 시 null 반환
                }



            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write(ex);
                result.DefectCode = 10;
                return null; // 예외 발생 시 null 반환
            }
            finally
            {
                if (swTotalProf != null) Console.WriteLine(string.Format("[PROF4] total={0:F1}", swTotalProf.Elapsed.TotalMilliseconds));
                System.Threading.Interlocked.Decrement(ref _concurrentInspects);
                cudaLease.Dispose();   // CUDA 컨텍스트 풀 반납 — 재할당된 버퍼도 그대로 반납되어 다음 검사에 재사용(2026-07-11)
                int nStartX = Math.Min((int)result.Corners[0].X, (int)result.Corners[3].X)*2;
                int nStartY = Math.Min((int)result.Corners[0].Y, (int)result.Corners[1].Y)*2;
                int nEndX = Math.Max((int)result.Corners[1].X, (int)result.Corners[2].X)*2;
                int nEndY = Math.Max((int)result.Corners[2].Y, (int)result.Corners[3].Y) * 2;

                if(nStartX < 0) nStartX = 0;
                if(nStartY < 0) nStartY = 0;
                if(nEndX >= w) nEndX = w - 1;
                if(nEndY >= h) nEndY = h - 1;

                
                //if (bSimulate == false)
                // IsSaveGoodImage 존중(2026-07-12) — 양품(DefectCode==0)은 플래그가 켜진 경우에만 원본 저장.
                // 종전에는 플래그를 무시하고 양품도 131MP PNG 를 매 검사 저장해 인코드 CPU/디스크가 tact 를 지배했다.
                // NG 저장/파일명/경로 규칙과 검사 결과 계산은 그대로다.
                if (result.DefectCode != 0 || bip.IsSaveGoodImage)
                {
                    SaveImage(w, h, ShiftImage, result.DefectCode == 0, bip.IndexX, bip.IndexY, bip.WaferID,bip.ColletID);
                }
                // 저장 완료 동기 대기 제거(2026-07-12) — 디펙 크롭 저장(SaveCount)은 ImageSaveQueue 가
                // 백그라운드에서 수행하며, 저장 결과는 검사 결과 계산과 무관(DisplayImage 소비처 없음 확인).
                // 종전에는 PNG 인코드(장당 CPU 1~2초)가 검사 tact 에 포함되어 병렬 검사를 크게 지연시켰다.
                //SaveImage(w, h, ShiftImageSobel, result.DefectCode == 0, bip.IndexX, bip.IndexY, bip.WaferID, bip.ColletID+1000);
                if (result.DefectCode == 0)
                {
                    result.DisplayImage = null;
                }
                else
                {
                    
                }
               
                if(result.DefectCode > 0 && result.DefectCode < 5)
                {
                    if(this.indexX == bip.IndexX
                        && this.indexY == bip.IndexY)
                    {

                    }
                    else
                    {
                       // result.DefectCode = 100;
                    }
                }
            }
            
            return result;
        }

        private void SaveOutLine(BottomInspectionParameter bip, byte[,] shiftImage, QMC_ResultChppingNForeign resultChppingNForeign)
        {
            bool bDebug = false;
            if (bDebug)
            {
                int w = shiftImage.GetLength(1); // shiftImage의 실제 너비 사용
                int h = shiftImage.GetLength(0); // shiftImage의 실제 높이 사용

                try
                {
                    // 1. shiftImage로 8비트 비트맵 생성
                    using (Bitmap bmp8bit = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format8bppIndexed))
                    {
                        // 회색조 팔레트 설정
                        var palette = bmp8bit.Palette;
                        for (int i = 0; i < 256; i++)
                        {
                            palette.Entries[i] = Color.FromArgb(i, i, i);
                        }
                        bmp8bit.Palette = palette;

                        // shiftImage 데이터를 8비트 비트맵에 복사
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
                                    for (int y = 0; y < h; y++)
                                    {
                                        Buffer.MemoryCopy(
                                            pSrc + y * w,      // 소스: shiftImage[y, 0]
                                            ptr + y * stride,  // 타겟: Bitmap의 y번째 라인
                                            stride,            // 타겟 버퍼 크기
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

                        // 2. 24비트 컬러 비트맵 생성 및 8비트 이미지 그리기
                        using (Bitmap bmp24bit = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                        {
                            using (Graphics g = Graphics.FromImage(bmp24bit))
                            {
                                // 8비트 비트맵을 24비트 비트맵에 그리기
                                g.DrawImage(bmp8bit, 0, 0);

                                // 3. 4개의 라인 그리기
                                using (Pen pen = new Pen(Color.Red, 2))
                                {
                                    // Top Line
                                    g.DrawLine(pen,
                                        resultChppingNForeign.LeftTop.X, resultChppingNForeign.LeftTop.Y,
                                        resultChppingNForeign.RightTop.X, resultChppingNForeign.RightTop.Y);

                                    // Right Line
                                    g.DrawLine(pen,
                                        resultChppingNForeign.RightTop.X, resultChppingNForeign.RightTop.Y,
                                        resultChppingNForeign.RightBottom.X, resultChppingNForeign.RightBottom.Y);

                                    // Bottom Line
                                    g.DrawLine(pen,
                                        resultChppingNForeign.RightBottom.X, resultChppingNForeign.RightBottom.Y,
                                        resultChppingNForeign.LeftBottom.X, resultChppingNForeign.LeftBottom.Y);

                                    // Left Line
                                    g.DrawLine(pen,
                                        resultChppingNForeign.LeftBottom.X, resultChppingNForeign.LeftBottom.Y,
                                        resultChppingNForeign.LeftTop.X, resultChppingNForeign.LeftTop.Y);
                                }
                            }

                            // 4. 파일 저장
                            lock (this)
                            {
                                Defact++;
                            }

                            string fileName = $"{bip.IndexX}_{bip.IndexY}_{DateTime.Now.Ticks.ToString()}";
                            string path = $"{fileName}_.png";
                            bmp24bit.Save(path, System.Drawing.Imaging.ImageFormat.Png);

                            Log.Write("SaveOutLine", $"이미지 저장 완료: {path}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write("SaveOutLine", $"이미지 저장 중 오류: {ex.Message}");
                    Log.Write(ex);
                }
            }
        }

        private void CorrectMotionblur(BottomResult result, QMC_ResultChppingNForeign resultChppingNForeign, int w, int h, byte[,] ShiftImage)
        {
            double[] cornerAngles, cornerLengths;
            double avgBlurLength = EstimateBlurLengthFromAllChipCorners(
                resultChppingNForeign, ShiftImage, w, h,
                out cornerAngles, out cornerLengths);

            // 최적의 블러 파라미터 선택
            if (SelectBestBlurParameters(cornerAngles, cornerLengths, out double bestAngle, out double bestLength))
            {
                // 블러 보정 적용
                // if (bestLength > 0 && bestAngle > 110 && bestAngle < 130)
                {
                    // angle 값을 이용하여 블러된 x방향 길이 y방향 길이를 계산합니다.
                    double BlurLengthX = bestLength * Math.Cos(bestAngle * Math.PI / 180.0);
                    double BlurLengthY = bestLength * Math.Sin(bestAngle * Math.PI / 180.0);
                    result.Width -= Math.Abs(BlurLengthX * 2);
                    result.Height -= Math.Abs(BlurLengthY * 2);

                    Log.Write("MotionDeblur", $"블러 보정 적용 - X: {BlurLengthX:F2}, Y: {BlurLengthY:F2}");
                }
            }
            else
            {
                Log.Write("MotionDeblur", "블러 보정을 적용하지 않음");
            }
        }

        private (bool bChipping,bool bForeign) InspectChipping(BottomInspectionParameter bip, BottomResult result, int w, int h, byte[,] ShiftImage,  double dChppingMargin, DateTime startTime, byte[] chippingMask, QMC_ResultChppingNForeign resultChppingNForeign, int margin, bool maskZeroOutside = false)
        {
            bool bChipping = false;
            bool bForeign = false;
            if (chippingMask != null)
            {
                // SaveImage(w, h, chippingMask, false, bip.IndexX, bip.IndexY, "Mask");

                var blobTool = new QMC_BlobTool();
                var chippingRegions = new List<List<Point>>();

                // 다이 bbox 한정 탐색(2026-07-12): 마스크 백색 픽셀은 외곽 4라인 안쪽(isInside)에만 존재하고
                // ctx 경로는 그 밖이 0 으로 보장되므로, 4코너 bbox(+여유)만 라벨링해도 성분/순서/좌표가 동일하다.
                Rectangle blobRoi = new Rectangle(0, 0, 0, 0);   // 무효 = 전체 프레임
                if (maskZeroOutside)
                {
                    float cminX = Math.Min(Math.Min(resultChppingNForeign.LeftTop.X, resultChppingNForeign.LeftBottom.X), Math.Min(resultChppingNForeign.RightTop.X, resultChppingNForeign.RightBottom.X));
                    float cmaxX = Math.Max(Math.Max(resultChppingNForeign.LeftTop.X, resultChppingNForeign.LeftBottom.X), Math.Max(resultChppingNForeign.RightTop.X, resultChppingNForeign.RightBottom.X));
                    float cminY = Math.Min(Math.Min(resultChppingNForeign.LeftTop.Y, resultChppingNForeign.RightTop.Y), Math.Min(resultChppingNForeign.LeftBottom.Y, resultChppingNForeign.RightBottom.Y));
                    float cmaxY = Math.Max(Math.Max(resultChppingNForeign.LeftTop.Y, resultChppingNForeign.RightTop.Y), Math.Max(resultChppingNForeign.LeftBottom.Y, resultChppingNForeign.RightBottom.Y));
                    int bx0 = Math.Max(0, (int)Math.Floor(cminX) - 2);
                    int by0 = Math.Max(0, (int)Math.Floor(cminY) - 2);
                    int bx1 = Math.Min(w - 1, (int)Math.Ceiling(cmaxX) + 2);
                    int by1 = Math.Min(h - 1, (int)Math.Ceiling(cmaxY) + 2);
                    if (bx1 > bx0 && by1 > by0)
                        blobRoi = new Rectangle(bx0, by0, bx1 - bx0 + 1, by1 - by0 + 1);
                }

                // 마스크는 이미 1차원(w*h) — 종전의 2D→1D 131MB 재복사 제거(2026-07-12, 내용 동일).
                // 링크거리 1 → 모폴로지 없음 → 성분 정의가 순수 8-연결이라 희소 라벨링으로 대체(성분/점 동일).
                var swBlob = bProfilePhases ? System.Diagnostics.Stopwatch.StartNew() : null;
                blobTool.FindBlobBrightSparse(chippingMask, w, h, w, 120, 1,
                    ref chippingRegions, blobRoi.X, blobRoi.Y, blobRoi.Width, blobRoi.Height);
                double msBlob = swBlob != null ? swBlob.Elapsed.TotalMilliseconds : 0;
                if (swBlob != null) swBlob.Restart();

                var chippingResult = InspectChippingRegions(bip, result, w, h, ShiftImage, chippingRegions, resultChppingNForeign, margin);
                if (swBlob != null) Console.WriteLine(string.Format("[PROF3] blob={0:F1} regions={1:F1} regionCount={2}", msBlob, swBlob.Elapsed.TotalMilliseconds, chippingRegions.Count));
                bChipping = chippingResult.bChipping;
                
                if (chippingResult.innerRegions.Count > 0)
                {
                    int minForeignSize = (int)(bip.PortentiolDefactMinSize);
                    
                    var foreignCandidates = chippingResult.innerRegions
                        .Where(region => region != null && region.Count >= minForeignSize)
                        .ToList();

                    if (foreignCandidates.Count > 0)
                    {
                        var foreignResult = InspectForeignRegions(bip, result, w, h, ShiftImage, foreignCandidates, w, h);
                        bForeign = foreignResult.bForeign;
                        result.ForeingSize = foreignResult.maxForeignSize;
                    }
                }
            }
            return (bChipping, bForeign);
        }

        private (bool bChipping, List<List<Point>> innerRegions) InspectChippingRegions(
            BottomInspectionParameter bip,
            BottomResult result,
            int w,
            int h,
            byte[,] shiftImage,
            List<List<Point>> chippingRegions,
            QMC_ResultChppingNForeign resultChppingNForeign,
            int margin)
        {
            bool bChipping = false;
            var innerRegions = new List<List<Point>>();
            double dMaxChippingSizeLeft = 0;
            double dMaxChippingSizeRight = 0;
            double dMaxChippingSizeTop = 0;
            double dMaxChippingSizeBottom = 0;

            foreach (var regionPoints in chippingRegions)
            {
                try
                {
                    // 빈 영역: 종전엔 Min() 예외 → catch 로 건너뜀 — 동일하게 건너뜀(블랍은 원래 비지 않음)
                    if (regionPoints == null || regionPoints.Count == 0) continue;
                    // LINQ Min/Max 4회 순회 → 1회 순회(같은 값 — 결과 동일, 2026-07-12)
                    int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
                    for (int pi = 0; pi < regionPoints.Count; pi++)
                    {
                        Point p = regionPoints[pi];
                        if (p.X < minX) minX = p.X;
                        if (p.X > maxX) maxX = p.X;
                        if (p.Y < minY) minY = p.Y;
                        if (p.Y > maxY) maxY = p.Y;
                    }

                    Point topLeft = new Point(minX - margin, minY - margin);
                    Point topRight = new Point(maxX + margin, minY - margin);
                    Point bottomLeft = new Point(minX - margin, maxY + margin);
                    Point bottomRight = new Point(maxX + margin, maxY + margin);

                    Line lineTop = new Line(topLeft, topRight);
                    Line lineBottom = new Line(bottomLeft, bottomRight);
                    Line lineLeft = new Line(topLeft, bottomLeft);
                    Line lineRight = new Line(topRight, bottomRight);

                    string intersectedLine = null;
                    double dSpec = Math.Min(bip.ChippingLength, bip.ChippingDepth);
                    bool bTopIntersects = false;
                    bool bBottomIntersects = false;
                    bool bLeftIntersects = false;
                    bool bRightIntersects = false;

                    bTopIntersects = CheckLineIntersection(resultChppingNForeign, lineTop, resultChppingNForeign.m_lineTop, topLeft, bottomRight, ref intersectedLine)
                        || CheckLineIntersection(resultChppingNForeign, lineLeft, resultChppingNForeign.m_lineTop, topLeft, bottomRight, ref intersectedLine)
                        || CheckLineIntersection(resultChppingNForeign, lineRight, resultChppingNForeign.m_lineTop, topLeft, bottomRight, ref intersectedLine)
                        || CheckLineIntersection(resultChppingNForeign, lineBottom, resultChppingNForeign.m_lineTop, topLeft, bottomRight, ref intersectedLine);
                    bBottomIntersects = CheckLineIntersection(resultChppingNForeign, lineTop, resultChppingNForeign.m_lineBottom, topLeft, bottomRight, ref intersectedLine)
                        || CheckLineIntersection(resultChppingNForeign, lineLeft, resultChppingNForeign.m_lineBottom, topLeft, bottomRight, ref intersectedLine)
                        || CheckLineIntersection(resultChppingNForeign, lineRight, resultChppingNForeign.m_lineBottom, topLeft, bottomRight, ref intersectedLine)
                        || CheckLineIntersection(resultChppingNForeign, lineBottom, resultChppingNForeign.m_lineBottom, topLeft, bottomRight, ref intersectedLine);
                    bLeftIntersects = CheckLineIntersection(resultChppingNForeign, lineTop, resultChppingNForeign.m_lineLeft, topLeft, bottomRight, ref intersectedLine)
                        || CheckLineIntersection(resultChppingNForeign, lineLeft, resultChppingNForeign.m_lineLeft, topLeft, bottomRight, ref intersectedLine)
                        || CheckLineIntersection(resultChppingNForeign, lineRight, resultChppingNForeign.m_lineLeft, topLeft, bottomRight, ref intersectedLine)
                        || CheckLineIntersection(resultChppingNForeign, lineBottom, resultChppingNForeign.m_lineLeft, topLeft, bottomRight, ref intersectedLine);
                    bRightIntersects = CheckLineIntersection(resultChppingNForeign, lineTop, resultChppingNForeign.m_lineRight, topLeft, bottomRight, ref intersectedLine)
                        || CheckLineIntersection(resultChppingNForeign, lineLeft, resultChppingNForeign.m_lineRight, topLeft, bottomRight, ref intersectedLine)
                        || CheckLineIntersection(resultChppingNForeign, lineRight, resultChppingNForeign.m_lineRight, topLeft, bottomRight, ref intersectedLine)
                        || CheckLineIntersection(resultChppingNForeign, lineBottom, resultChppingNForeign.m_lineRight, topLeft, bottomRight, ref intersectedLine);

                    if (!(bTopIntersects || bBottomIntersects || bLeftIntersects || bRightIntersects))
                    {
                        innerRegions.Add(regionPoints);
                        continue;
                    }

                    int nMaginSpec = margin / 2;
                    int interCount = 0;

                    Rectangle saveRect = new Rectangle(minX, minY, maxX - minX, maxY - minY);
                    double ChippingSize = Math.Max(ConvertPixelToMM(saveRect.Width, _visionConfig.BottomVision.PixelSizeWidthMm / 2), ConvertPixelToMM(saveRect.Height, _visionConfig.BottomVision.PixelSizeWidthMm / 2));
                    Line line = null;
                    int nOffset = 0;
                    if (bTopIntersects)
                    {
                        interCount++;
                        line = resultChppingNForeign.m_lineTop;

                        nOffset = (int)(saveRect.Y - Math.Min(resultChppingNForeign.m_lineTop.GetY(minX), resultChppingNForeign.m_lineTop.GetY(maxX)));
                        saveRect.Y -= (int)nOffset;
                        saveRect.Height += nOffset;
                        ChippingSize = ConvertPixelToMM(saveRect.Height, _visionConfig.BottomVision.PixelSizeWidthMm / 2);
                        nOffset = (int)(saveRect.X - Math.Min(resultChppingNForeign.m_lineLeft.GetX(saveRect.Y), resultChppingNForeign.m_lineLeft.GetX(saveRect.Y + saveRect.Height)));

                        if (nOffset <= nMaginSpec)
                        {
                            saveRect.X -= (int)nOffset;
                            saveRect.Width += nOffset;
                            ChippingSize = Math.Min(ConvertPixelToMM(saveRect.Width, _visionConfig.BottomVision.PixelSizeHeightMm / 2), ChippingSize);
                        }
                        nOffset = (int)(Math.Max(resultChppingNForeign.m_lineRight.GetX(saveRect.Y), resultChppingNForeign.m_lineRight.GetX(saveRect.Y + saveRect.Height)) - (saveRect.X + saveRect.Width));

                        if (nOffset <= nMaginSpec)
                        {
                            ChippingSize = Math.Min(ConvertPixelToMM(saveRect.Width, _visionConfig.BottomVision.PixelSizeHeightMm / 2), ChippingSize);
                            saveRect.Width += nOffset;
                        }

                    }
                    if (bBottomIntersects)
                    {

                        interCount++;
                        line = resultChppingNForeign.m_lineBottom;
                        nOffset = (int)(Math.Max(resultChppingNForeign.m_lineBottom.GetY(minX), resultChppingNForeign.m_lineBottom.GetY(maxX)) - (saveRect.Y + saveRect.Height));
                        saveRect.Height += nOffset;
                        ChippingSize = ConvertPixelToMM(saveRect.Height, _visionConfig.BottomVision.PixelSizeWidthMm / 2);
                        nOffset = (int)(saveRect.X - Math.Min(resultChppingNForeign.m_lineLeft.GetX(saveRect.Y), resultChppingNForeign.m_lineLeft.GetX(saveRect.Y + saveRect.Height)));
                        if (nOffset <= nMaginSpec)
                        {
                            saveRect.X -= nOffset;
                            saveRect.Width += nOffset;
                            ChippingSize = Math.Min(ConvertPixelToMM(saveRect.Width, _visionConfig.BottomVision.PixelSizeHeightMm / 2), ChippingSize);
                        }
                        nOffset = (int)(Math.Max(resultChppingNForeign.m_lineRight.GetX(saveRect.Y), resultChppingNForeign.m_lineRight.GetY(maxY)) - (saveRect.X + saveRect.Width));
                        if (nOffset <= nMaginSpec)
                        {
                            saveRect.Width += nOffset;
                            ChippingSize = Math.Min(ConvertPixelToMM(saveRect.Width, _visionConfig.BottomVision.PixelSizeHeightMm / 2), ChippingSize);
                        }
                    }

                    if (bLeftIntersects)
                    {

                        interCount++;
                        line = resultChppingNForeign.m_lineLeft;
                        nOffset = (int)(saveRect.X - Math.Min(resultChppingNForeign.m_lineLeft.GetX(minY), resultChppingNForeign.m_lineLeft.GetX(maxY)));
                        saveRect.X -= nOffset;
                        saveRect.Width += nOffset;
                        ChippingSize = ConvertPixelToMM(saveRect.Width, _visionConfig.BottomVision.PixelSizeHeightMm / 2);
                        nOffset = (int)(saveRect.Y - Math.Min(resultChppingNForeign.m_lineTop.GetY(minX), resultChppingNForeign.m_lineTop.GetY(maxX)));
                        if (nOffset <= nMaginSpec)
                        {
                            saveRect.Y -= nOffset;
                            saveRect.Height += nOffset;
                            ChippingSize = Math.Min(ConvertPixelToMM(saveRect.Height, _visionConfig.BottomVision.PixelSizeWidthMm / 2), ChippingSize);
                        }
                        nOffset = (int)(Math.Max(resultChppingNForeign.m_lineBottom.GetY(minX), resultChppingNForeign.m_lineBottom.GetY(maxX)) - (saveRect.Y + saveRect.Height));
                        if (nOffset <= nMaginSpec)
                        {
                            saveRect.Height += nOffset;
                            ChippingSize = Math.Min(ConvertPixelToMM(saveRect.Height, _visionConfig.BottomVision.PixelSizeHeightMm / 2), ChippingSize);
                        }
                    }
                    if (bRightIntersects)
                    {

                        interCount++;
                        line = resultChppingNForeign.m_lineRight;
                        nOffset = (int)(Math.Max(resultChppingNForeign.m_lineRight.GetX(minY), resultChppingNForeign.m_lineRight.GetX(maxY)) - (saveRect.X + saveRect.Width));
                        saveRect.Width += nOffset;
                        ChippingSize = ConvertPixelToMM(saveRect.Width, _visionConfig.BottomVision.PixelSizeHeightMm / 2);
                        nOffset = (int)(saveRect.Y - Math.Min(resultChppingNForeign.m_lineTop.GetY(minX), resultChppingNForeign.m_lineTop.GetY(maxX)));
                        if (nOffset <= nMaginSpec)
                        {
                            saveRect.Y -= nOffset;
                            saveRect.Height += nOffset;
                            ChippingSize = Math.Min(ConvertPixelToMM(saveRect.Height, _visionConfig.BottomVision.PixelSizeWidthMm / 2), ChippingSize);
                        }
                        nOffset = (int)(Math.Max(resultChppingNForeign.m_lineBottom.GetY(minX), resultChppingNForeign.m_lineBottom.GetY(maxX)) - (saveRect.Y + saveRect.Height));

                        if (nOffset <= nMaginSpec)
                        {
                            saveRect.Height += nOffset;
                            ChippingSize = Math.Min(ConvertPixelToMM(saveRect.Height, _visionConfig.BottomVision.PixelSizeHeightMm / 2), ChippingSize);
                        }
                    }

                    if (interCount > 1)
                    {
                        double ChippingSizeH = Math.Max(ConvertPixelToMM(saveRect.Height, _visionConfig.BottomVision.PixelSizeHeightMm / 2), ChippingSize);

                        double ChippingSizeW = Math.Max(ConvertPixelToMM(saveRect.Width, _visionConfig.BottomVision.PixelSizeHeightMm / 2), ChippingSize);
                        ChippingSize = Math.Min(ChippingSizeH, ChippingSizeW);
                    }

                    if (bTopIntersects)
                    {
                        if (dMaxChippingSizeTop < ChippingSize)
                        {
                            dMaxChippingSizeTop = ChippingSize;
                        }
                    }
                    else if (bBottomIntersects)
                    {
                        if (dMaxChippingSizeBottom < ChippingSize)
                        {
                            dMaxChippingSizeBottom = ChippingSize;
                        }
                    }
                    else if (bLeftIntersects)
                    {

                        if (dMaxChippingSizeLeft < ChippingSize)
                        {
                            dMaxChippingSizeLeft = ChippingSize;
                        }
                    }
                    else if (bRightIntersects)
                    {

                        if (dMaxChippingSizeRight < ChippingSize)
                        {
                            dMaxChippingSizeRight = ChippingSize;
                        }
                    }


                    if (bSimulate)
                    {
                        dSpec = 0.05;
                    }
                    if (ChippingSize < dSpec)
                    {
                        if (ChippingSize > dSpec / 2)
                        {

                            SaveDefactImage(w, h, shiftImage, saveRect, bip, line, false, result);
                        }

                        continue; // 치핑 크기가 최소 크기 보다 작으면 무시합니다.
                    }

                    if (ChippingSize > dSpec)
                    {
                        
                        bChipping = true;
                    }


                    SaveDefactImage(w, h, shiftImage, saveRect, bip, line, true, result);

                    var chippingInfo = new ChippingInfo
                    {
                        Length = maxX - minX + 1,
                        Depth = maxY - minY + 1
                    };
                    chippingInfo.Contour.Add(new PointF(minX, minY));
                    chippingInfo.Contour.Add(new PointF(minX, maxY));
                    chippingInfo.Contour.Add(new PointF(maxX, minY));
                    chippingInfo.Contour.Add(new PointF(maxX, maxY));

                    result.ChippingInfos.Add(chippingInfo);
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }

            }

            // 각 면에서 치핑이 검출되지 않은 경우 (dMaxChippingSize == 0), 
            // FindRespectChippingH/W를 사용하여 외곽선 기반으로 가장 큰 치핑 사이즈를 보완합니다.
            double dSpec2 = Math.Min(bip.ChippingLength, bip.ChippingDepth);
            if (bSimulate) dSpec2 = 0.05;

            if (dMaxChippingSizeTop == 0 || dMaxChippingSizeBottom == 0)
            {
                double dSizeTop = 0;
                double dSizeBottom = 0;
                FindRespectChippingH(bip, shiftImage, resultChppingNForeign, out dSizeTop, out dSizeBottom);

                if (dMaxChippingSizeTop == 0 && dSizeTop > 0)
                {
                    dMaxChippingSizeTop = dSizeTop;
                    if (dSizeTop >= dSpec2)
                    {
                        bChipping = true;
                        Log.Write("InspectChippingRegions", $"FindRespectChippingH - Top 치핑 보완: {dSizeTop:F4}mm");
                    }
                }
                if (dMaxChippingSizeBottom == 0 && dSizeBottom > 0)
                {
                    dMaxChippingSizeBottom = dSizeBottom;
                    if (dSizeBottom >= dSpec2)
                    {
                        bChipping = true;
                        Log.Write("InspectChippingRegions", $"FindRespectChippingH - Bottom 치핑 보완: {dSizeBottom:F4}mm");
                    }
                }
            }

            if (dMaxChippingSizeLeft == 0 || dMaxChippingSizeRight == 0)
            {
                double dSizeLeft = 0;
                double dSizeRight = 0;
                FindRespectChippingW(bip, shiftImage, resultChppingNForeign, out dSizeLeft, out dSizeRight);

                if (dMaxChippingSizeLeft == 0 && dSizeLeft > 0)
                {
                    dMaxChippingSizeLeft = dSizeLeft;
                    if (dSizeLeft >= dSpec2)
                    {
                        bChipping = true;
                        Log.Write("InspectChippingRegions", $"FindRespectChippingW - Left 치핑 보완: {dSizeLeft:F4}mm");
                    }
                }
                if (dMaxChippingSizeRight == 0 && dSizeRight > 0)
                {
                    dMaxChippingSizeRight = dSizeRight;
                    if (dSizeRight >= dSpec2)
                    {
                        bChipping = true;
                        Log.Write("InspectChippingRegions", $"FindRespectChippingW - Right 치핑 보완: {dSizeRight:F4}mm");
                    }
                }
            }

            result.Channel1ChippingSize = Math.Max(dMaxChippingSizeLeft, dMaxChippingSizeRight);
            result.Channel2ChippingSize = Math.Max(dMaxChippingSizeTop, dMaxChippingSizeBottom);

            result.ChppingBottomSize = dMaxChippingSizeBottom;
            result.ChppingLeftSize = dMaxChippingSizeLeft;
            result.ChppingRightSize = dMaxChippingSizeRight;
            result.ChppingTopSize = dMaxChippingSizeTop;


            Log.Write(bip.WaferID + "Chipping", "BottomInspect", "X:" + bip.IndexX.ToString() + ", Y :  " + bip.IndexY.ToString() +
                ",Top:" + dMaxChippingSizeLeft.ToString("F4") + ", Right : " + dMaxChippingSizeTop.ToString("F4")
                + ", Bottom : " + dMaxChippingSizeRight.ToString("F4")
                + ", Left : " + dMaxChippingSizeBottom.ToString("F4"));
            
            DateTime endTime = DateTime.Now;
            

            return (bChipping, innerRegions);
        }

        private (bool bForeign, double maxForeignSize) InspectForeignRegions(BottomInspectionParameter bip, BottomResult result, int w, int h, byte[,] shiftImage, List<List<Point>> regions, int maskWidth, int maskHeight)
        {
            if (regions == null || regions.Count == 0)
            {
                return (false, 0);
            }

            var linkedRegions = LinkForeignRegions(regions, maskWidth, maskHeight, (int)bip.PortentiolDefactMinSize, bip.LinkDistance);
            if (linkedRegions.Count == 0)
            {
                return (false, 0);
            }

            bool bForeign = false;
            double dMaxForeignSize = 0;
            foreach (var regionPoints in linkedRegions)
            {
                if (regionPoints == null || regionPoints.Count == 0  )
                {
                    continue;
                }
                
                int area = regionPoints.Count;
                // LINQ Min/Max 4회 순회 → 1회 순회(같은 값 — 결과 동일, 2026-07-12)
                int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
                for (int pi = 0; pi < regionPoints.Count; pi++)
                {
                    Point p = regionPoints[pi];
                    if (p.X < minX) minX = p.X;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.Y > maxY) maxY = p.Y;
                }

                Rectangle saveRect = new Rectangle(minX, minY, maxX - minX, maxY - minY);
                double foreignSize = Math.Max(ConvertPixelToMM(saveRect.Width, _visionConfig.BottomVision.PixelSizeWidthMm / 2), ConvertPixelToMM(saveRect.Height, _visionConfig.BottomVision.PixelSizeWidthMm / 2));
                if (foreignSize > dMaxForeignSize)
                {
                    dMaxForeignSize = foreignSize;
                }

                bool isNg = foreignSize >= bip.ForeignObjectSize;
                if (isNg && regionPoints.Count > bip.MinForeignAreaFilterSize)
                {
                    bForeign = true;
                    // 이물 위치를 결과에 담는다(검사 좌표 — 반환 직전 BottomInspect 가 원본 좌표로 환산).
                    result.ForeignInfos.Add(new ForeignInfo { Rect = saveRect, SizeMm = foreignSize, Area = area, IsNg = true });
                    SaveDefactImage(w, h, shiftImage, saveRect, bip, null, true, result , area);
                }
                else if (foreignSize > bip.ForeignObjectSize / 5)
                {
                    result.ForeignInfos.Add(new ForeignInfo { Rect = saveRect, SizeMm = foreignSize, Area = area, IsNg = false });
                    SaveDefactImage(w, h, shiftImage, saveRect, bip, null, false, result, area);
                }
            }

            return (bForeign, dMaxForeignSize);
        }

        private List<List<Point>> LinkForeignRegions(List<List<Point>> regions, int width, int height, int minSize, int linkDistance)
        { 
            if (regions == null || regions.Count == 0)
            {
                return new List<List<Point>>();
            }

            var swLink = System.Diagnostics.Stopwatch.StartNew();   // 이물 링크 소요 진단(2026-07-12) — 실장비 90초 정체 재발 감시용
            // 클러스터 분해 처리(2026-07-12): 종전에는 후보 점 몇 천 개를 위해 전체 프레임(131MP) 마스크를
            // 새로 할당하고 모폴로지 Close + ConnectedComponents×2 를 돌렸다. Close(사각 커널 반지름 =
            // linkDistance)는 서로 체비셰프 거리 2×linkDistance 초과로 떨어진 픽셀 집합을 절대 연결하지
            // 못하므로(각각의 dilate 가 접촉 불가), 후보 영역들을 '바운딩박스 간격 ≤ 2×linkDistance' 기준으로
            // 뭉친 클러스터별 소창(패딩 = linkDistance+2)에서 독립 처리해도 출력 백색 픽셀 집합·성분 구성이
            // 전체 처리와 동일하다(과잉 병합은 무해 — 같은 창 안에서도 Close 가 못 잇는 건 CCL 이 갈라놓음).
            // 최종 목록은 성분 첫-등장 픽셀(y,x) 래스터 순으로 정렬해 전체-프레임 라벨 순서와 일치시킨다.
            int n = regions.Count;
            var bboxes = new Rectangle[n];
            var alive = new bool[n];
            for (int i = 0; i < n; i++)
            {
                var region = regions[i];
                if (region == null || region.Count == 0) continue;
                int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
                bool has = false;
                foreach (var pt in region)
                {
                    if (pt.X < 0 || pt.X >= width || pt.Y < 0 || pt.Y >= height) continue;
                    if (pt.X < minX) minX = pt.X;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.Y > maxY) maxY = pt.Y;
                    has = true;
                }
                if (!has) continue;
                alive[i] = true;
                bboxes[i] = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
            }

            // union-find 로 클러스터링(박스 간격 ≤ 2*linkDistance → 같은 클러스터; 보수적 과잉 병합은 결과 불변)
            // 근접쌍 탐색은 그리드 버킷(2026-07-12): 종전의 전수 i×j 비교는 후보 수만 개(먼지 많은 실물 다이)에서
            // n² 폭발로 검사 1건에 90초 이상 걸렸다. 셀 크기 = linkGap 격자에 박스를 등록하고, 각 박스는
            // linkGap 만큼 확장한 범위의 셀에 든 박스와만 정확 판정(gap ≤ linkGap)한다 — 병합 결과(분할)는
            // 전수 비교와 동일(같은 판정식, union 순서는 파티션에 무영향).
            int linkGap = 2 * Math.Max(1, linkDistance);
            var parent = new int[n];
            for (int i = 0; i < n; i++) parent[i] = i;
            bool useLegacyPairLoop = Environment.GetEnvironmentVariable("QMC_LINK_LEGACY") == "1";   // 동일성 검증용 임시 스위치
            Func<int, int> find = (i) =>
            {
                int root = i;
                while (parent[root] != root) root = parent[root];
                while (parent[i] != root) { int next = parent[i]; parent[i] = root; i = next; }   // 경로 압축(반복형 — 대량 후보에서 재귀 스택 방지)
                return root;
            };
            if (useLegacyPairLoop)
            {
                for (int i = 0; i < n; i++)
                {
                    if (!alive[i]) continue;
                    for (int j = i + 1; j < n; j++)
                    {
                        if (!alive[j]) continue;
                        int gapX = Math.Max(bboxes[i].Left, bboxes[j].Left) - Math.Min(bboxes[i].Right - 1, bboxes[j].Right - 1) - 1;
                        int gapY = Math.Max(bboxes[i].Top, bboxes[j].Top) - Math.Min(bboxes[i].Bottom - 1, bboxes[j].Bottom - 1) - 1;
                        if (Math.Max(gapX, gapY) <= linkGap)
                        {
                            parent[find(i)] = find(j);
                        }
                    }
                }
            }
            else
            {
            int cell = Math.Max(1, linkGap);
            var buckets = new Dictionary<long, List<int>>();
            for (int i = 0; i < n; i++)
            {
                if (!alive[i]) continue;
                int cx0 = bboxes[i].Left / cell;
                int cx1 = (bboxes[i].Right - 1) / cell;
                int cy0 = bboxes[i].Top / cell;
                int cy1 = (bboxes[i].Bottom - 1) / cell;
                for (int cy = cy0; cy <= cy1; cy++)
                    for (int cx = cx0; cx <= cx1; cx++)
                    {
                        long bkey = ((long)cy << 32) | (uint)cx;
                        if (!buckets.TryGetValue(bkey, out var list)) { list = new List<int>(); buckets[bkey] = list; }
                        list.Add(i);
                    }
            }
            for (int i = 0; i < n; i++)
            {
                if (!alive[i]) continue;
                // linkGap 확장 박스가 닿는 셀만 조회 — gap ≤ linkGap 인 상대는 반드시 이 셀들 중에 있다.
                int qx0 = Math.Max(0, bboxes[i].Left - linkGap) / cell;
                int qx1 = (bboxes[i].Right - 1 + linkGap) / cell;
                int qy0 = Math.Max(0, bboxes[i].Top - linkGap) / cell;
                int qy1 = (bboxes[i].Bottom - 1 + linkGap) / cell;
                for (int cy = qy0; cy <= qy1; cy++)
                    for (int cx = qx0; cx <= qx1; cx++)
                    {
                        long bkey = ((long)cy << 32) | (uint)cx;
                        if (!buckets.TryGetValue(bkey, out var list)) continue;
                        for (int k = 0; k < list.Count; k++)
                        {
                            int j = list[k];
                            if (j <= i) continue;   // 쌍 1회 판정(전수 비교의 i<j 와 동일)
                            int ri = find(i), rj = find(j);
                            if (ri == rj) continue;
                            int gapX = Math.Max(bboxes[i].Left, bboxes[j].Left) - Math.Min(bboxes[i].Right - 1, bboxes[j].Right - 1) - 1;
                            int gapY = Math.Max(bboxes[i].Top, bboxes[j].Top) - Math.Min(bboxes[i].Bottom - 1, bboxes[j].Bottom - 1) - 1;
                            if (Math.Max(gapX, gapY) <= linkGap)
                            {
                                parent[ri] = rj;
                            }
                        }
                    }
            }
            }

            var clusters = new Dictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                if (!alive[i]) continue;
                int root = find(i);
                List<int> members;
                if (!clusters.TryGetValue(root, out members)) { members = new List<int>(); clusters[root] = members; }
                members.Add(i);
            }

            if (clusters.Count == 0)
            {
                return new List<List<Point>>();
            }

            int pad = Math.Max(1, linkDistance) + 2;
            var blobTool = new QMC_BlobTool();
            var all = new List<List<Point>>();
            foreach (var kv in clusters)
            {
                // 클러스터 창 = 멤버 bbox 합집합 + 패딩
                int minCX = int.MaxValue, maxCX = int.MinValue, minCY = int.MaxValue, maxCY = int.MinValue;
                foreach (int idx in kv.Value)
                {
                    var b = bboxes[idx];
                    if (b.Left < minCX) minCX = b.Left;
                    if (b.Right - 1 > maxCX) maxCX = b.Right - 1;
                    if (b.Top < minCY) minCY = b.Top;
                    if (b.Bottom - 1 > maxCY) maxCY = b.Bottom - 1;
                }
                int x0 = Math.Max(0, minCX - pad);
                int y0 = Math.Max(0, minCY - pad);
                int x1 = Math.Min(width - 1, maxCX + pad);
                int y1 = Math.Min(height - 1, maxCY + pad);
                int subW = x1 - x0 + 1;
                int subH = y1 - y0 + 1;

                byte[] mask = BufferPool.Rent(subW * subH);
                Array.Clear(mask, 0, mask.Length);
                foreach (int idx in kv.Value)
                {
                    foreach (var pt in regions[idx])
                    {
                        if (pt.X < 0 || pt.X >= width || pt.Y < 0 || pt.Y >= height) continue;
                        mask[(pt.Y - y0) * subW + (pt.X - x0)] = 255;
                    }
                }

                var found = new List<List<Point>>();
                blobTool.FindBlobBright(mask, subW, subH, subW, 120, minSize, (byte)120, ref found, null, linkDistance);
                BufferPool.Return(mask);

                foreach (var blob in found)
                {
                    if (x0 != 0 || y0 != 0)
                    {
                        for (int i = 0; i < blob.Count; i++)
                        {
                            blob[i] = new Point(blob[i].X + x0, blob[i].Y + y0);
                        }
                    }
                    all.Add(blob);
                }
            }

            // 전체-프레임 라벨링과 동일한 순서(첫-등장 픽셀 래스터 순) — 블랍 내 점들은 이미 래스터 순.
            all.Sort((a, b) =>
            {
                Point pa = a[0], pb = b[0];
                int c = pa.Y.CompareTo(pb.Y);
                return c != 0 ? c : pa.X.CompareTo(pb.X);
            });
            Log.Write("VisionInspector", "Foreign link: 후보 " + n + "개 → 클러스터 " + clusters.Count
                + "개 → 연결 " + all.Count + "개, " + swLink.ElapsedMilliseconds + "ms");
            return all;
        }

        private static List<List<Point>> FindBrightBlobsOpenCv(byte[] image, int width, int height, int stride, byte threshold, int minDefectSize, int linkDistance)
        {
            var blobTool = new QMC_BlobTool();
            var results = new List<List<Point>>();
            blobTool.FindBlobBright(image, width, height, stride, threshold, minDefectSize, threshold, ref results, null, linkDistance);
            return results;
         }
        private void FindRespectChippingH(BottomInspectionParameter bip, byte[,] shiftImage, QMC_ResultChppingNForeign resultChppingNForeign, out double dSizeTop, out double dSizeBottom)
        {
            dSizeTop = 0;
            dSizeBottom = 0;
            if (resultChppingNForeign.m_lineTop != null && resultChppingNForeign.m_lineBottom != null)
            {
                //위쪽 라인과 Left라인의 크로스 지점 부터 Right라인 까지의 사이의 치핑을 검사 한다.
                int nMargin = (int)(bip.ChippingDepth/(_visionConfig.BottomVision.PixelSizeWidthMm/2)/2);

                int StartX = (int)resultChppingNForeign.LeftTop.X + nMargin * 2;
                int EndX = (int)resultChppingNForeign.RightTop.X - nMargin * 2;
                if (StartX < 0) StartX = 0;
                if (EndX >= shiftImage.GetLength(1)) EndX = shiftImage.GetLength(1) - 1;
                int ChippingThresholdDark = -1;
                int ChippingThresholdBright = bip.Threshold;

                // 위쪽 라인 검사 (TopLine)
                for (int x = StartX; x <= EndX; x++)
                {
                    int yTop = (int)resultChppingNForeign.m_lineTop.GetY(x);
                    if (yTop < 0 || yTop >= shiftImage.GetLength(0)) continue; // 경계 검사
                    bool bFind = false;
                    for(int iter = 0; iter < nMargin; iter ++ )
                    {
                        if (yTop + iter >= shiftImage.GetLength(0)) break; // 경계 검사
                        if (shiftImage[yTop + iter, x] < ChippingThresholdDark 
                            || shiftImage[yTop + iter, x] >= ChippingThresholdBright)
                        {
                            // 치핑이 발견된 경우
                            int yChippingSize = iter;
                            if (yChippingSize <= 0) continue; // 치핑 크기가 0 이하인 경우 무시

                            double dChippingSizeH = ConvertPixelToMM(yChippingSize, _visionConfig.BottomVision.PixelSizeHeightMm / 2);
                            if (dSizeTop < dChippingSizeH)
                                dSizeTop = dChippingSizeH;
                            bFind = true;
                        }
                        else
                        {
                            if(bFind)
                            {
                                break;
                            }
                        }
                    }
                }

                // 아래쪽 라인 검사 (BottomLine) - FindRespectChippingW의 RightLine 검사 부분과 동일한 패턴
                StartX = (int)resultChppingNForeign.LeftBottom.X + nMargin;
                EndX = (int)resultChppingNForeign.RightBottom.X - nMargin;
                if (StartX < 0) StartX = 0;
                if (EndX >= shiftImage.GetLength(1)) EndX = shiftImage.GetLength(1) - 1;

                for (int x = StartX; x <= EndX; x++)
                {
                    int yBottom = (int)resultChppingNForeign.m_lineBottom.GetY(x);
                    if (yBottom < 0 || yBottom >= shiftImage.GetLength(0)) continue; // 경계 검사
                    bool bFind = false;
                    for (int iter = 0; iter < nMargin; iter++)
                    {
                        if (yBottom - iter < 0) break; // 경계 검사
                        if (shiftImage[yBottom - iter, x] < ChippingThresholdDark
                            || shiftImage[yBottom - iter, x] >= ChippingThresholdBright)
                        {
                            // 치핑이 발견된 경우
                            int yChippingSize = iter;
                            if (yChippingSize <= 0) continue; // 치핑 크기가 0 이하인 경우 무시

                            double dChippingSizeH = ConvertPixelToMM(yChippingSize, _visionConfig.BottomVision.PixelSizeHeightMm / 2);
                            if (dSizeBottom < dChippingSizeH)
                                dSizeBottom = dChippingSizeH;
                            bFind = true;
                        }
                        else
                        {
                            if (bFind)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
        private void FindRespectChippingW(BottomInspectionParameter bip, byte[,] shiftImage, QMC_ResultChppingNForeign resultChppingNForeign, out double dMaxChippingSizeLeft, out double dMaxChippingSizeRight)
            

        {
            dMaxChippingSizeLeft = 0;
            dMaxChippingSizeRight = 0;
            if (resultChppingNForeign.m_lineLeft != null && resultChppingNForeign.m_lineRight != null)
            {
                //왼쪽 라인과 Top라인의 크로스 지점 부터 Bottom라인 까지의 사이의 치핑을 검사 한다.
                int nMargin = (int)(bip.ChippingLength/(_visionConfig.BottomVision.PixelSizeHeightMm/2)/2);

                int StartY = (int)resultChppingNForeign.LeftTop.Y + nMargin*2;
                int EndY = (int)resultChppingNForeign.LeftBottom.Y - nMargin*2;
                if (StartY < 0) StartY = 0;
                if (EndY >= shiftImage.GetLength(0)) EndY = shiftImage.GetLength(0) - 1;
                int ChippingThresholdDark = -1;
                int ChippingThresholdBright = bip.Threshold;

                for (int y = StartY; y <= EndY; y++)
                {
                    int xLeft = (int)resultChppingNForeign.m_lineLeft.GetX(y);
                    int xRight = (int)resultChppingNForeign.m_lineRight.GetX(y);
                    if (xLeft < 0 || xLeft >= shiftImage.GetLength(1)) continue; // 경계 검사
                    if (xRight < 0 || xRight >= shiftImage.GetLength(1)) continue; // 경계 검사
                    bool bFind = false; 
                    for (int iter = 0; iter < nMargin; iter ++ )
                    {
                        if (xLeft + iter >= shiftImage.GetLength(1)) break; // 경계 검사
                        if (xRight - iter < 0) break; // 경계 검사
                        if (shiftImage[y, xLeft + iter] < ChippingThresholdDark 
                            || shiftImage[y, xLeft + iter] >= ChippingThresholdBright)
                        {
                            // 치핑이 발견된 경우
                            int xChippingSize = iter;
                            if (xChippingSize <= 0) continue; // 치핑 크기가 0 이하인 경우 무시

                            double dChippingSizeW = ConvertPixelToMM(xChippingSize, _visionConfig.BottomVision.PixelSizeWidthMm / 2);
                            if (dMaxChippingSizeLeft < dChippingSizeW)
                                dMaxChippingSizeLeft = dChippingSizeW;
                            bFind = true;
                        }
                        else
                        {
                            if (bFind)
                            {
                                break;
                            }
                        }
                    }
                }

                StartY = (int)resultChppingNForeign.RightTop.Y + nMargin;
                EndY = (int)resultChppingNForeign.RightBottom.Y - nMargin;
                for (int y = StartY; y <= EndY; y++)
                {
                    int xRight = (int)resultChppingNForeign.m_lineRight.GetX(y);
                    if (xRight < 0 || xRight >= shiftImage.GetLength(1)) continue; // 경계 검사
                    bool bFind = false;
                    for (int iter = 0; iter < nMargin; iter++)
                    {
                        if (xRight - iter < 0) break; // 경계 검사
                        if (shiftImage[y, xRight - iter] < ChippingThresholdDark
                            || shiftImage[y, xRight - iter] >= ChippingThresholdBright)
                        {
                            // 치핑이 발견된 경우
                            int xChippingSize = iter;
                            if (xChippingSize <= 0) continue; // 치핑 크기가 0 이하인 경우 무시

                            double dChippingSizeW = ConvertPixelToMM(xChippingSize, _visionConfig.BottomVision.PixelSizeWidthMm / 2);
                            if (dMaxChippingSizeRight < dChippingSizeW)
                                dMaxChippingSizeRight = dChippingSizeW;
                            bFind = true;
                        }
                        else
                        {
                            if (bFind)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            
        }

        private double ConvertPixelToMM(int width, double v)
        {
            if (width <= 0 || v <= 0)
                return 0;
            return width * v; // 픽셀 너비를 mm로 변환
        }

        // 두 라인이 교차하는지 확인하는 헬퍼 메서드
        private bool CheckLineIntersection(QMC_ResultChppingNForeign resultChppingNForeign , Line line1, Line line2, Point rectTopLeft, Point rectBottomRight, ref string intersectedLine)
        {
            
            //intersectedLine = null;

            if (line1 == null || line2 == null) return false;

            // 두 라인의 교점을 계산
            if (line1.GetCrossPoint(line2, out Point crossPoint))
            {
                // 교점이 사각형 내부에 있는지 확인
                if (crossPoint.X >= rectTopLeft.X && crossPoint.X <= rectBottomRight.X &&
                    crossPoint.Y >= rectTopLeft.Y && crossPoint.Y <= rectBottomRight.Y)
                {
                    // 교차한 라인의 이름 반환
                    if (line2 == resultChppingNForeign.m_lineTop) intersectedLine = "m_lineTop";
                    else if (line2 == resultChppingNForeign.m_lineBottom) intersectedLine = "m_lineBottom";
                    else if (line2 == resultChppingNForeign.m_lineLeft) intersectedLine = "m_lineLeft";
                    else if (line2 == resultChppingNForeign.m_lineRight) intersectedLine = "m_lineRight";

                    return true;
                }
            }

            return false;
        }
        private void MakeSoftWareExpendImage(BottomInspectionParameter bip, out byte[,] shiftImage, out byte[,] shiftImageSobel, out int w, out int h, int i = 0, IntPtr cudaCtx = default(IntPtr))
        {
            if (bip.Images == null || bip.Images.Count == 0 || bip.Images[0] == null)
                throw new ArgumentException("입력 이미지가 없습니다.");

            Point ptCenter = FindChipCenter(bip);
            
            var srcImage = bip.Images[i];
            bip.ChipRoi = new Rectangle(ptCenter.X - bip.ChipRoi.Width / 2, ptCenter.Y - bip.ChipRoi.Height / 2, bip.ChipRoi.Width, bip.ChipRoi.Height);
            var roi = bip.ChipRoi;
            int srcW = roi.Width;
            int srcH = roi.Height;
            int srcStride = bip.ImageWidth;


            w = srcW * 2;
            h = srcH * 2;

            byte[,] output2D = new byte[h, w]; // [y, x]


            byte[,] outputSobel = new byte[h, w]; // [y, x]


            // output2D를 포인터로 고정하여 전달
            var handle = GCHandle.Alloc(output2D, GCHandleType.Pinned);
            var handleSobel = GCHandle.Alloc(outputSobel, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();

                IntPtr ptrSobel = handleSobel.AddrOfPinnedObject();
                int result;
                if (cudaCtx != IntPtr.Zero)
                {
                    // 컨텍스트 경로(2026-07-11) — 디바이스 버퍼 재사용(같은 크기면 할당 0회, 크기 변경 시만 재할당).
                    try
                    {
                        result = UpscaleROI2xBilinearAndSobelCtx(
                            cudaCtx,
                            bip.Images[0], bip.ImageWidth, bip.ImageHeight,
                            roi.Left, roi.Top, roi.Width, roi.Height,
                            ptr, ptrSobel);
                    }
                    catch (EntryPointNotFoundException)
                    {
                        // 구버전 DLL(QmcCtx 미탑재) — 기존(호출마다 할당) 경로 폴백.
                        result = UpscaleROI2xBilinearAndSobel(
                            bip.Images[0], bip.ImageWidth, bip.ImageHeight,
                            roi.Left, roi.Top, roi.Width, roi.Height,
                            ptr, ptrSobel);
                    }
                }
                else
                {
                    result = UpscaleROI2xBilinearAndSobel(
                        bip.Images[0], bip.ImageWidth, bip.ImageHeight,
                        roi.Left, roi.Top, roi.Width, roi.Height,
                        ptr, ptrSobel);
                }
                // result 체크
            }
            finally
            {
                handle.Free();
                handleSobel.Free();
            }

            shiftImage = output2D;
            shiftImageSobel = outputSobel;
        }

        private void MakePixelShiftImage(BottomInspectionParameter bip, BottomResult result, out int w, out int h, out byte[,] ShiftImage)
        {
            w = bip.ChipRoi.Width * 2;
            h = bip.ChipRoi.Height * 2;

            List<byte[,]> images = new List<byte[,]>();
            List<Task<QMC_ResultChppingNForeign>> tasks = new List<Task<QMC_ResultChppingNForeign>>();

            foreach (var img in bip.Images)
            {
                if (img == null || img.Length < bip.ChipRoi.Width * bip.ChipRoi.Height)
                    throw new ArgumentException("입력 이미지 크기가 올바르지 않습니다.");
                byte[,] RoiImage = MakeRoiImage(bip.ChipRoi, img, bip.ImageWidth);
                images.Add(RoiImage);
                nInstanceCount++;
                int myW = w / 2;
                int myH = h / 2;
                var t = Task.Factory.StartNew(() => {


                    if (System.Threading.Thread.CurrentThread.Name == null)
                        System.Threading.Thread.CurrentThread.Name = "Vision_Inspect" + nInstanceCount.ToString();

                    QMC_ResultChppingNForeign resultchip = new QMC_ResultChppingNForeign();
                    QMC_FindChippingNForeign fcf = new QMC_FindChippingNForeign();
                    fcf.SetChippingThreshold(bip.Threshold);
                    fcf.FindChipOutline(resultchip, myW, myH, RoiImage);
                    return resultchip;
                });
                tasks.Add(t);
            }
            List<PointF> offsets = new List<PointF>();
            foreach (var t in tasks)
            {
                t.Wait(); // 모든 작업이 완료될 때까지 대기
                List<PointF> bipoffsets = new List<PointF>();

                bipoffsets.Add(t.Result.LeftTop); // Top-left corner
                bipoffsets.Add(t.Result.RightTop); // Top-right corner
                bipoffsets.Add(t.Result.RightBottom); // Bottom-right corner
                bipoffsets.Add(t.Result.LeftBottom); // Bottom-left corner

                double dOX = 0;
                double dOY = 0;
                foreach (var v in result.Corners)
                {
                    dOX += v.X;
                    dOY += v.Y;
                }
                dOX /= 4.0;
                dOY /= 4.0;
                offsets.Add(new PointF((float)dOX, (float)dOX)); // 평균 오프셋 계산
            }
            //w = test;
            ShiftImage = MakeImagePixelShift(bip.Images, bip.ChipRoi, bip.Threshold, bip.ImageWidth, offsets);
        }
        private unsafe void SaveDefactImage(int w, int h, byte[,] ShiftImage, Rectangle rt, BottomInspectionParameter bip, Line line, bool bIsNG,BottomResult result,int area = 0)
        {
            const int margin = 100;
            //if (bIsNG == false)
            //{
            //    return;
            //}

            // 1. 확장된 ROI(관심 영역) 계산 및 경계 검사
            int cropX = Math.Max(0, rt.X - margin);
            int cropY = Math.Max(0, rt.Y - margin);
            int cropWidth = Math.Min(rt.Width + 2 * margin, w - cropX);
            int cropHeight = Math.Min(rt.Height + 2 * margin, h - cropY);

            // 잘라낼 영역이 유효하지 않으면 함수 종료
            if (cropWidth <= 0 || cropHeight <= 0)
            {
                return;
            }
            cropWidth = ((cropWidth + 3) / 4) * 4; // 4의 배수로 맞춤

            // 2. 잘라낸 이미지를 담을 새로운 2차원 배열 생성
            byte[,] croppedImage = new byte[cropHeight, cropWidth];

            // 3. *** 수정: unsafe 블록으로 직접 메모리 복사 ***
            unsafe
            {
                fixed (byte* pSrc = ShiftImage)
                fixed (byte* pDst = croppedImage)
                {
                    for (int y = 0; y < cropHeight; y++)
                    {
                        int sourceY = cropY + y;
                        int resultStartIndex = y * cropWidth;

                        // 경계 검사
                        if (sourceY >= 0 && sourceY < h)
                        {
                            // 소스 포인터: ShiftImage[srcY, cropX]
                            byte* srcRow = pSrc + (sourceY * w + cropX);
                            // 대상 포인터: croppedImage[y, 0]
                            byte* dstRow = pDst + (y * cropWidth);

                            // 한 행씩 복사
                            int copyWidth = Math.Min(cropWidth, w - cropX);
                            Buffer.MemoryCopy(srcRow, dstRow, copyWidth, copyWidth);
                        }
                    }
                }
            }

            try
            {
                // 저장할 디렉터리 생성
                string dirPath = "c:\\Log\\Image";
                if(bSimulate)
                {
                    dirPath = "c:\\Log\\SimulateImage";
                }
                IfNotExistMakeFolder(dirPath);

                dirPath += "\\" + bip.WaferID;
                IfNotExistMakeFolder(dirPath);

                if (bIsNG)
                {
                    dirPath += "\\" + "Defact";
                }
                else
                {
                    if (bSimulate)
                    {
                        return;
                    }
                    dirPath += "\\" + "Potential Defact";
                }

                IfNotExistMakeFolder(dirPath);

                dirPath += "\\" + $"X-{bip.IndexX}_Y-{bip.IndexY}";
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                int x = (int)(rt.Width * _visionConfig.BottomVision.PixelSizeWidthMm * 1000 / 2);
                int y = (int)(rt.Height * _visionConfig.BottomVision.PixelSizeHeightMm * 1000 / 2);

                // 고유한 파일명 생성
                string fileName = "";
                if (line == null)
                {
                    fileName = Path.Combine(dirPath, $"Defact_X-{bip.IndexX}_Y-{bip.IndexY}_W_{x}um_H_{y}um");
                }
                else
                {
                    fileName = Path.Combine(dirPath, $"Chipping_X-{bip.IndexX}_Y-{bip.IndexY}_W_{x}um_H_{y}um");
                }
                 
                // 기존 SaveImage 헬퍼 메서드를 사용하여 파일로 저장
                SaveImageWidthSize(croppedImage, cropWidth, cropHeight, fileName, 100, line, result, bIsNG, area);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
        long SaveIndex = DateTime.Now.Ticks;
        private void SaveImage(int w, int h, byte[,] ShiftImage, bool bIsGood = true, int indexX = 0, int indexY = 0,string waferID ="Empty", int colletID = 0)
        {
            string strFileName = "d:\\Log\\Image";
            try
            {
                
               
                IfNotExistMakeFolder(strFileName);
                if (waferID == string.Empty || waferID.Length == 0)
                {
                    waferID = "Empty";
                }
                strFileName += "\\" + waferID;
                IfNotExistMakeFolder(strFileName);

                if (bIsGood)
                {
                    strFileName += "\\OK";
                }
                else
                {
                    strFileName += "\\NG";
                }
                IfNotExistMakeFolder(strFileName);
                lock (CodaLock)
                {
                    SaveIndex++;
                }
                strFileName += "\\_X-" + indexX.ToString() + "_Y-" + indexY.ToString() + "_" + colletID.ToString() + "_" + SaveIndex.ToString();

                
                SaveImage(ShiftImage, w, h, strFileName);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                Log.Write("SaveImage", strFileName);
            }
            
        }

        public static void IfNotExistMakeFolder(string strFileName)
        {
            Log.Write("IfNotExistMakeFolder", strFileName);
            if (!Directory.Exists(strFileName))
            {
                Directory.CreateDirectory(strFileName);
            }
        }

        private void OffsetPointFAndReSize(ref PointF leftTop, Rectangle chipRoi, double dScale)
        {
            leftTop.X *= (float)dScale;
            leftTop.Y *= (float)dScale;
            leftTop += new SizeF((float)chipRoi.Left, (float)chipRoi.Top);
        }
        
        uint Defact = 0;
        private void SaveImage(byte[,] image, int width, int height, string strFileName)
        {
            //SaveImageHelper helper = new SaveImageHelper
            //{
            //    ShiftImage = new byte[height, width],
            //    //    Width = width,
    //    Height = height,
    //    FileName = strFileName
    //};
    //// 이미지 복사
    //for (int (y = 0; y < height; y++)
    //{
    //    for ( int x = 0; x < width; x++)
    //    {
    //        helper.ShiftImage[y, x] = image[y, x];
    //    }
    //}

    SaveImageHelper helper = new SaveImageHelper
    {
        ShiftImage = image,
        Width = width,
        Height = height,
        FileName = strFileName
    };
    // 저장 전용 큐(2026-07-12) — PNG 인코드를 검사 스레드풀에서 분리(내용/경로 동일, 타이밍만 분리).
    ImageSaveQueue.Enqueue(() =>
    {
        SaveImageHelper saveHelper = helper;

        string strOrginalFileName = saveHelper.FileName;
        byte[,] shiftImage = saveHelper.ShiftImage;
        int w = saveHelper.Width;
        int h = saveHelper.Height;
        string fileName = saveHelper.FileName;
        //byte[,] img = new byte[ h/4, w / 4];
        //if(w>5000)
        //{
        //    for (int x = 0; x < w / 4; x++)
        //    {
        //        for (int y = 0; y < h / 4; y++)
        //        {
        //            int nSum = 0;
        //            int nCount = 0;
        //            if (x > 0 && x < w/4-1)
        //            {
        //                nSum = shiftImage[y*4, x * 4 - 1] 
        //                 +  shiftImage[ y * 4, x * 4]
        //                 +  shiftImage[y * 4, x * 4 + 1]
        //                 +  shiftImage[y * 4, x * 4 + 2];
        //                nCount = 4;
        //            }
        //            else
        //            {
        //                nSum = shiftImage[y * 4, x * 4];
        //                nCount = 1;
        //            }
        //            if (y > 0 && y < h/4-1)
        //            {
        //                nSum += shiftImage[ y * 4 - 1, x * 4]
        //                + shiftImage[ y * 4 + 1,x * 4]
        //                + shiftImage[y * 4 + 2, x * 4];
        //                nCount += 3;
        //            }
        //            img[y, x] = (byte)(nSum / nCount);
        //        }
        //    }
        //    shiftImage = img;
        //    w = w / 4;
        //    h = h/4;
        //}
        
        try
        {
            if (shiftImage == null)
                throw new ArgumentNullException(nameof(shiftImage));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("파일 이름이 올바르지 않습니다.", nameof(fileName));

            using (var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format8bppIndexed))
            {
                var palette = bmp.Palette;
                for (int i = 0; i < 256; i++)
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                bmp.Palette = palette;

                // 픽셀 데이터 복사
                var rect = new Rectangle(0, 0, w, h);
                var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
                try
                {
                    int stride = bmpData.Stride;
                    unsafe
                    {
                        fixed (byte* pSrc = &shiftImage[0, 0])
                        {
                            byte* ptr = (byte*)bmpData.Scan0;
                            int rowBytes = w; // 실제 복사할 바이트 수는 이미지 너비
                            for (int y = 0; y < h; y++)
                            {
                                Buffer.MemoryCopy(
                                    pSrc + y * w,      // 소스: shiftImage[y, 0]
                                    ptr + y * stride,  // 타겟: Bitmap의 y번째 라인
                                    rowBytes,          // 복사할 바이트 수
                                    rowBytes           // 복사할 바이트 수
                                );
                            }
                        }
                    }
                }
                finally
                {
                    bmp.UnlockBits(bmpData);
                }
                lock (this)
                {
                    Defact++;

                }
                string path = fileName + "_" + Defact.ToString() + ".png";


                try
                {

                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                }catch(Exception ex)
                {
                    try
                    {
                        path = path.ToUpper();
                        path = path.Replace("d:\\", "D:\\");
                        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    catch (Exception ex2)
                    {
                    }

                }
                


            }
        }
        catch (Exception ex)
        {
            Log.Write(ex);
            //SaveImage(shiftImage, w, h, strOrginalFileName + "Retry_");
        }

    });




        }
        Bitmap LastBitMap = null;
        
        private unsafe void SaveImageWidthSize(byte[,] image, int width, int height, string strFileName, int nMargin, Line line,BottomResult result,bool bIsNG,int area = 0)
        {
            SaveImageHelper helper = new SaveImageHelper
            {
                ShiftImage = image,
                Width = width,
                Height = height,
                FileName = strFileName,
                Result = result,
                Area = area
                
            };
            lock (result)
            {

                result.SaveCount++;

                // MaxDefactSize 동기 갱신(2026-07-12): 종전에는 이 값이 비동기 저장 작업 안에서 설정됐고
                // BottomInspect 가 SaveCount 대기 루프로 완료를 보장했다. 저장을 ImageSaveQueue 로 분리하면서
                // 대기 없이 반환하므로, 같은 식(dsize = min(크롭폭, 크롭높이), NG 시 최대 갱신)을 저장 등록
                // 시점에 즉시 계산한다 — 최댓값 갱신이라 순서 무관, 종전과 같은 최종값이 반환 전에 확정된다.
                double dsizeSync = Math.Min(width, height);
                if (dsizeSync > result.MaxDefactSize && bIsNG)
                {
                    result.MaxDefactSize = dsizeSync;
                }
            }
            // 저장 전용 큐(2026-07-12) — 디펙 크롭 PNG 인코드를 검사 스레드풀에서 분리(내용/경로 동일).
            ImageSaveQueue.Enqueue(() =>
            {

                SaveImageHelper saveHelper = helper;

                string strOrginalFileName = saveHelper.FileName;
                byte[,] shiftImage = saveHelper.ShiftImage;
                int w = saveHelper.Width;
                int h = saveHelper.Height;
                string fileName = saveHelper.FileName;
                saveHelper.Result = result;
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
                                    int rowBytes = w; // 실제 복사할 바이트 수는 이미지 너비
                                    for (int y = 0; y < h; y++)
                                    {
                                        Buffer.MemoryCopy(
                                            pSrc + y * w,      // 소스: shiftImage[y, 0]
                                            ptr + y * stride,  // 타겟: Bitmap의 y번째 라인
                                            rowBytes,          // 복사할 바이트 수
                                            rowBytes           // 복사할 바이트 수
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
                                // 8비트 비트맵을 24비트 비트맵에 그리기
                                g.DrawImage(bmp8bit, 0, 0);

                                //사각형 그리기
                                using (Pen pen = new Pen(Color.Red, 2))
                                {
                                    g.DrawRectangle(pen, nMargin, nMargin, w - 2 * nMargin, h - 2 * nMargin);
                                }

                                // Width와 Height 텍스트 추가
                                using (Font font = new Font("Arial", 12))
                                using (Brush brush = new SolidBrush(Color.Red))
                                {
                                    double dWidth = w - nMargin * 2;
                                    double dHeight = h - nMargin * 2;
                                    dWidth *= _visionConfig.BottomVision.PixelSizeWidthMm / 2 * 1000; // mm 단위로 변환
                                    dHeight *= _visionConfig.BottomVision.PixelSizeHeightMm / 2 * 1000; // mm 단위로 변환
                                    // 텍스트 위치 조정
                                    string text = $"W: {dWidth:F1} um, H: {dHeight:F1} um ";
                                    int nStartPos = 50;
                                    if(helper.Area> 0)
                                    {
                                        nMargin -= 30;
                                        text += $"\n      A:{helper.Area} Pixel";
                                    }
                                    g.DrawString(text, font, brush, new PointF(nStartPos, nMargin - 30));
                                }
                            }

                            lock (this)
                            {
                                Defact++;
                            }

                            string path = fileName + "_" + Defact.ToString() + ".png";
                            try
                            {

                                bmp24bit.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                            }catch(Exception ex)
                            {
                                try
                                {

                                    path = path.ToUpper();
                                    path = path.Replace("d:\\", "D:\\");
                                    bmp24bit.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                                }catch(Exception ex2)
                                {

                                }
                            }
                            // MaxDefactSize 는 저장 등록 시점(동기)에 갱신하도록 이동(2026-07-12) — 위 주석 참조.
                            // DisplayImage 는 소비처가 없어(선언/초기화 외 참조 없음 확인) 설정을 중단한다.
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                   // SaveImage(shiftImage, w, h, strOrginalFileName + "Retry_");
                }finally
                {
                    lock (saveHelper.Result)
                    {
                        saveHelper.Result.SaveCount--;
                    }
                }
            });
        }
        private byte[,] MakeImagePixelShift(List<byte[]> images, Rectangle chipRoi, int threshold, int nSourceWidth)
        {
            int srcW = chipRoi.Width;
            int srcH = chipRoi.Height;
            int dstW = srcW * 2;
            int dstH = srcH * 2;

            if (images == null || images.Count != 4)
                throw new ArgumentException("4장의 이미지가 필요합니다.");
            foreach (var img in images)
            {
                if (img == null || img.Length < srcW * srcH)
                    throw new ArgumentException("입력 이미지 크기가 올바르지 않습니다.");
            }

            byte[,] result = new byte[dstH, dstW]; // [y, x]
            try
            {


                Parallel.For(0, (srcH + BlockSize - 1) / BlockSize, blockIdx =>
                {
                    int yStart = blockIdx * BlockSize;
                    int yEnd = Math.Min(yStart + BlockSize, srcH);
                    for (int y = yStart; y < yEnd; y++)
                    {
                        for (int x = 0; x < srcW; x++)
                        {
                            // 각 이미지에서 픽셀 쉬프트 적용
                            byte[] srcPixels = new byte[4];
                            for (int i = 0; i < 4; i++)
                            {
                                int srcIndex = (chipRoi.Y + y) * nSourceWidth + x + chipRoi.X;
                                if (srcIndex < images[i].Length)
                                {
                                    srcPixels[i] = images[i][srcIndex];
                                }
                                else
                                {
                                    srcPixels[i] = 0; // 범위를 벗어난 경우 기본값 0
                                }
                            }
                            // 픽셀 쉬프트 적용
                            result[y * 2, x * 2] = srcPixels[0]; // [y, x]
                            result[y * 2, x * 2 + 1] = srcPixels[1]; // [y, x]
                            result[y * 2 + 1, x * 2] = srcPixels[2]; // [y, x]
                            result[y * 2 + 1, x * 2 + 1] = srcPixels[3]; // [y, x]

                        }
                    }
                });
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write(ex);
            }

            return result;
        }

        private byte[,] MakeImagePixelShift(List<byte[]> images, Rectangle chipRoi, int threshold, int nSourceWidth, List<PointF> offset)
        {
            int srcW = chipRoi.Width;
            int srcH = chipRoi.Height;
            int dstW = srcW * 2;
            int dstH = srcH * 2;

            if (images == null || images.Count != 4)
                throw new ArgumentException("4장의 이미지가 필요합니다.");
            if (offset == null || offset.Count != 4)
                throw new ArgumentException("4개 이상의 Offset이 필요합니다.");
            for (int i = 0; i < 4; i++)
                if (images[i] == null || images[i].Length < srcW * srcH)
                    throw new ArgumentException("입력 이미지 크기가 올바르지 않습니다.");

            // 첫 번째 이미지를 기준으로 상대 오프셋(정수) 계산
            int[] offsetX = new int[4];
            int[] offsetY = new int[4];
            float baseX = offset[0].X;
            float baseY = offset[0].Y;
            for (int i = 0; i < 4; i++)
            {
                offsetX[i] = (int)Math.Round(offset[i].X - baseX);
                offsetY[i] = (int)Math.Round(offset[i].Y - baseY);
            }

            byte[,] result = new byte[dstH, dstW]; // [y, x]
            try
            {

                Parallel.For(0, (srcH + BlockSize - 1) / BlockSize, blockIdx =>
                {
                    int yStart = blockIdx * BlockSize;
                    int yEnd = Math.Min(yStart + BlockSize, srcH);
                    for (int y = yStart; y < yEnd; y++)
                    {
                        for (int x = 0, x2 = 0; x < srcW; x++, x2 += 2)
                        {
                            byte[] srcPixels = new byte[4];
                            for (int i = 0; i < 4; i++)
                            {
                                int srcX = chipRoi.X + x + offsetX[i];
                                int srcY = chipRoi.Y + y + offsetY[i];
                                int srcIndex = srcY * nSourceWidth + srcX;

                                if (srcX >= chipRoi.X && srcX < chipRoi.X + srcW &&
                                    srcY >= chipRoi.Y && srcY < chipRoi.Y + srcH &&
                                    srcIndex >= 0 && srcIndex < images[i].Length)
                                {
                                    srcPixels[i] = images[i][srcIndex];
                                }
                                else
                                {
                                    srcPixels[i] = 0;
                                }
                            }
                            result[y * 2, x2] = srcPixels[0]; // [y, x]
                            result[y * 2 + 1, x2] = srcPixels[2]; // [y, x]
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write(ex);
            }

            return result;
        }

        private byte[] MakeImagePixelShift(List<byte[]> images, double chippingDepth, double chippingLength, Rectangle chipRoi, int threshold)
        {
            throw new NotImplementedException();
        }

        public DieGapResult DieGapInspect(DieGapInspectionParameter param)
        {
            // 검사 파라미터를 사용하여 검사 수행
            // 예: param.Roi, param.Threshold, param.Width, param.Height, param.Image
            // 검사 결과를 DieGapResult 객체에 저장
            DieGapResult result = new DieGapResult();
            try
            {
                Random rand = new Random();
                byte[,] image = MakeRoiImage(param.Roi, param.Image, param.ImageWidth);
                double dAngle = 0;
                List<PointF> points = new List<PointF>();
                result.Gaps = FindDieGaps(image, param.Threshold, out dAngle, out points);
                result.Gaps.SetOffset(+0.03);
                if (points.Count == 4)
                {
                    result.Corners = points.ToArray(); // 4개의 코너 포인트 저장
                }

                OffsetPointFAndReSize(ref result.Corners[0], param.Roi, 1); // Top-left corner
                OffsetPointFAndReSize(ref result.Corners[1], param.Roi, 1); // Top-right corner
                OffsetPointFAndReSize(ref result.Corners[2], param.Roi, 1); // Bottom-right corner
                OffsetPointFAndReSize(ref result.Corners[3], param.Roi, 1); // Bottom-left corner

                bool bResult = JudgmentDieGapOK(result.Gaps.Left, param.LowerLimit, param.UpperLimit);
                bResult &= JudgmentDieGapOK(result.Gaps.Right, param.LowerLimit, param.UpperLimit);
                bResult &= JudgmentDieGapOK(result.Gaps.Top, param.LowerLimit, param.UpperLimit);
                bResult &= JudgmentDieGapOK(result.Gaps.Bottom, param.LowerLimit, param.UpperLimit);
                if(bResult == false)
                {
                    result.DefectCode = 20;
                }
                else
                {
                    result.DefectCode = 0;
                }
                
                // 각도와 오프셋도 랜덤 값 할당
                result.Angle = dAngle; // 0~360도
                result.Offset = result.Corners.Aggregate(new PointF(0, 0), (acc, p) => new PointF(acc.X + p.X, acc.Y + p.Y));
                result.Offset = new PointF(result.Offset.X / 4f, result.Offset.Y / 4f);
                Log.Write("DieGap", "Offset = " + result.Offset.ToString());
                if (result.Offset.X < 0)
                {
                    result.DefectCode = 11;
                    result.Offset = new PointF(0, 0);
                    Log.Write("DieGap", "return Offset = " + result.Offset.ToString());
                }

                Task.Run(() =>
                {

                    try
                    {
                        string strFolder = "d:\\Log\\Binimage\\" + param.WaferID;
                        IfNotExistMakeFolder(strFolder);
                        string strFileName = strFolder +"\\"+ param.IndexX.ToString("000") +"_"+param.IndexY.ToString("000")+"_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + DateTime.Now.Ticks.ToString() + ".png";


                        SaveImage(image, param.Roi.Width, param.Roi.Height, strFileName);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                    }
                });
                if (result.Angle == 0)
                {
                    result.DefectCode = 10;
                    return null; // 예외 발생 시 null 반환 
                }
                // 불량 코드도 랜덤 할당
                // result.DefectCode = rand.Next(0, 10);
                //result.Gaps.Left = result.Gaps.Right;
            }
            catch (Exception ex)
            {

                Log.Write(ex);
                return null; // 예외 발생 시 null 반환
            }finally
            {
                Log.Write("DieGap", "DefectCode = " + result.DefectCode.ToString());
            }
            // 결과 반환
            return result;
        }

        private bool JudgmentDieGapOK(Gap gap, double lowerLimit, double upperLimit)
        {
            if(gap == null)
                return false;       
            if(gap.Min ==0 && gap.Max ==0)
            {
                return true;
            }
            if(gap.Avg < lowerLimit || upperLimit  < gap.Avg)
            {
                return false;
            }
            return true;
            
        }

        private byte[,] MakeRoiImage(Rectangle roi, byte[] image, int imageWidth)
        {
            if (image == null || image.Length < roi.Width * roi.Height)
                throw new ArgumentException("입력 이미지 크기가 올바르지 않습니다.");
            byte[,] roiImage = new byte[roi.Height, roi.Width]; // [y, x]


            int height = roi.Height;
            int width = roi.Width;

            Parallel.For(0, (height + BlockSize - 1) / BlockSize, blockIdx =>
            {
                int yStart = blockIdx * BlockSize;
                int yEnd = Math.Min(yStart + BlockSize, height);
                unsafe
                {
                    fixed (byte* pSrc = image)
                    fixed (byte* pDst = &roiImage[0, 0])
                    {
                        for (int y = yStart; y < yEnd; y++)
                        {
                            int srcRow = (roi.Y + y) * imageWidth + roi.X;
                            byte* srcPtr = pSrc + srcRow;
                            byte* dstPtr = pDst + (y * width);
                            Buffer.MemoryCopy(srcPtr, dstPtr, width, width);
                        }
                    }
                }
            });
            return roiImage;
        }

        /// <summary>이웃 다이 진위 판정(2026-07-12) — 전이 지점에서 바깥 방향(dy,dx)으로 span 픽셀을 볼 때
        /// 90% 이상이 임계 미만(어두움)이어야 '실제 옆다이'로 인정한다. 웨이퍼 위 먼지/섬유는 수 픽셀
        /// 두께라 지속되지 않으므로 배제되고, 실제 다이(수백 px)는 통과한다. 경계까지 span 의 절반도
        /// 확보되지 않으면 판정 불가로 다이가 아니라고 본다(경계 노이즈 배제).</summary>
        private static bool IsNeighborDieDark(byte[,] image, int y, int x, int dy, int dx, int threshold, int span)
        {
            int height = image.GetLength(0), width = image.GetLength(1);
            int dark = 0, total = 0;
            for (int k = 0; k < span; k++)
            {
                int yy = y + dy * k, xx = x + dx * k;
                if (yy < 0 || yy >= height || xx < 0 || xx >= width) break;
                total++;
                if (image[yy, xx] < threshold) dark++;
            }
            if (total < span / 2) return false;
            return dark * 10 >= total * 9;
        }

        /// <summary>이웃 다이 지속성 검사 길이(px) — 먼지/섬유(수~수십 px)와 실제 다이(수백 px)를 가른다.</summary>
        private const int NeighborDieSpanPx = 40;

        private Gap FindDieGapLeft(byte[,] image, int threshold, out double angle, out Line line)
        {
            int width = image.GetLength(1); // [y, x]
            int height = image.GetLength(0); // [y, x]
            int centerX = width / 2;
            line = new Line();
            // 1. 중앙 칩의 위/아래 엣지 찾기 (Y범위 결정)
            int chipTop = -1, chipBottom = -1;
            for (int y = height / 2; y > 0; y--)
            {
                if (image[y, centerX] < threshold && image[y - 1, centerX] >= threshold) // [y, x]
                {
                    chipTop = y;
                    break;
                }
            }

            for (int y = height / 2; y < height - 1; y++)
            {
                if (image[y, centerX] < threshold && image[y + 1, centerX] >= threshold) // [y, x]
                {
                    chipBottom = y;
                    break;
                }
            }
            if (chipTop < 0 || chipBottom < 0 || chipBottom <= chipTop)
            {
                angle = 0;
                return new Gap { Min = 0, Max = 0 };
            }
            // 2. 위~아래 90%만 사용
            int yStart = chipTop + (int)((chipBottom - chipTop) * 0.10);
            int yEnd = chipBottom - (int)((chipBottom - chipTop) * 0.10);
            List<int> gaps = new List<int>();
            List<PointF> edgePoints = new List<PointF>();

            for (int y = yStart; y < yEnd; y++)
            {
                // 중앙칩의 왼쪽 엣지 찾기 (중앙에서 왼쪽으로)
                int chipEdge = -1;
                for (int x = centerX; x > 4; x--)
                {

                    if (image[y, x] < threshold && image[y, x - 3] >= threshold) // [y, x]
                    {
                        chipEdge = x;
                        edgePoints.Add(new PointF(x, y));
                        break;
                    }
                }
                if (chipEdge > 0 && chipEdge < width - 1)
                {
                    // 옆칩의 오른쪽 엣지 찾기 — 먼지(수 px)는 어두움이 지속되지 않으므로 통과시키고 실제 다이만 인정(2026-07-12)
                    for (int x = chipEdge - 3; x > 4; x--)
                    {

                        if (image[y, x] >= threshold && image[y, x - 3] < threshold // [y, x]
                            && IsNeighborDieDark(image, y, x - 3, 0, -1, threshold, NeighborDieSpanPx))
                        {
                            int gap = chipEdge - x;
                            gaps.Add(gap);
                            break;
                        }
                    }
                }
            }
            if (edgePoints.Count > 200)
            {
                line = new Line(edgePoints);
                angle = line.GetAngle();
            }
            else
            {
                angle = 0;
            }

            gaps = RemoveGap(gaps);

            return new Gap
            {
                Min = gaps.Count > 200 ? gaps.Min() : 0,
                Max = gaps.Count > 200 ? gaps.Max() : 0
            };

        }


        private Gap FindDieGapRight(byte[,] image, int threshold, out double angle, out Line line)
        {
            int width = image.GetLength(1); // [y, x]
            int height = image.GetLength(0); // [y, x]
            int centerX = width / 2;
            line = new Line();
            // 1. 중앙 칩의 위/아래 엣지 찾기 (Y범위 결정)
            int chipTop = -1, chipBottom = -1;
            for (int y = height / 2; y > 0; y--)
            {
                if (image[y, centerX] < threshold && image[y - 1, centerX] >= threshold) // [y, x]
                {
                    chipTop = y;
                    break;
                }
            }
            for (int y = height / 2; y < height - 1; y++)
            {
                if (image[y, centerX] < threshold && image[y + 1, centerX] >= threshold) // [y, x]
                {
                    chipBottom = y;
                    break;
                }
            }
            if (chipTop < 0 || chipBottom < 0 || chipBottom <= chipTop)
            {
                angle = 0;
                return new Gap { Min = 0, Max = 0 };
            }
            // 2. 위~아래 90%만 사용
            int yStart = chipTop + (int)((chipBottom - chipTop) * 0.1);
            int yEnd = chipBottom - (int)((chipBottom - chipTop) * 0.1);
            List<int> gaps = new List<int>();
            List<PointF> edgePoints = new List<PointF>();
            for (int y = yStart; y < yEnd; y++)
            {
                // 중앙칩의 오른쪽 엣지 찾기 (중앙에서 오른쪽으로)
                int chipEdge = -1;
                for (int x = centerX; x < width - 4; x++)
                {

                    if (image[y, x] < threshold && image[y, x + 3] >= threshold) // [y, x]
                    {
                        chipEdge = x;
                        edgePoints.Add(new PointF(x, y));
                        break;
                    }
                }
                if (chipEdge > 0 && chipEdge < width - 4)
                {
                    // 옆칩의 왼쪽 엣지 — 먼지(수 px)는 어두움이 지속되지 않으므로 통과시키고 실제 다이만 인정(2026-07-12)
                    for (int x = chipEdge + 1; x < width - 4; x++)
                    {
                        if (image[y, x] >= threshold && image[y, x + 3] < threshold // [y, x]
                            && IsNeighborDieDark(image, y, x + 3, 0, 1, threshold, NeighborDieSpanPx))
                        {
                            int gap = x - chipEdge;
                            gaps.Add(gap);
                            break;
                        }
                    }
                }
            }
            if (edgePoints.Count > 2)
            {
                line = new Line(edgePoints);
                angle = line.GetAngle();
            }
            else
            {
                angle = 0;
            }

            gaps = RemoveGap(gaps);
            return new Gap
            {
                Min = gaps.Count > 0 ? gaps.Min() : 0,
                Max = gaps.Count > 0 ? gaps.Max() : 0
            };
        }
        private Gap FindDieGapTop(byte[,] image, int threshold, out double angle, out Line line)
        {

            int width = image.GetLength(1); // [y, x]
            int height = image.GetLength(0); // [y, x]
            int centerX = width / 2;
            int centerY = height / 2;
            line = new Line();
            // 1. 중앙 칩의 좌/우 엣지 찾기 (X범위 결정)
            int chipLeft = -1, chipRight = -1;
            for (int x = width / 2; x > 0; x--)
            {
                if (image[centerY, x] < threshold && image[centerY, x - 1] >= threshold) // [y, x]
                {
                    chipLeft = x;
                    break;
                }
            }
            for (int x = width / 2; x < width - 1; x++)
            {
                if (image[centerY, x] < threshold && image[centerY, x + 1] >= threshold) // [y, x]
                {
                    chipRight = x;
                    break;
                }
            }
            if (chipLeft < 0 || chipRight < 0 || chipRight <= chipLeft)
            {
                angle = 0;
                return new Gap { Min = 0, Max = 0 };
            }
            // 2. 좌~우 90%만 사용
            int xStart = chipLeft + (int)((chipRight - chipLeft) * 0.1);
            int xEnd = chipRight - (int)((chipRight - chipLeft) * 0.1);
            List<int> gaps = new List<int>();
            List<PointF> edgePoints = new List<PointF>();

            for (int x = xStart; x < xEnd; x++)
            {
                // 중앙칩의 위쪽 엣지 찾기 (중앙에서 위로)
                int chipEdge = -1;
                for (int y = centerY; y > 4; y--)
                {

                    if (image[y, x] < threshold && image[y - 3, x] >= threshold) // [y, x]
                    {
                        chipEdge = y;
                        edgePoints.Add(new PointF(x, y));
                        break;
                    }
                }
                if (chipEdge > 0 && chipEdge < height - 1)
                {
                    // 옆칩의 아래쪽 엣지 찾기 — 먼지(수 px)는 어두움이 지속되지 않으므로 통과시키고 실제 다이만 인정(2026-07-12)
                    for (int y = chipEdge - 3; y > 4; y--)
                    {

                        if (image[y, x] >= threshold && image[y - 3, x] < threshold // [y, x]
                            && IsNeighborDieDark(image, y - 3, x, -1, 0, threshold, NeighborDieSpanPx))
                        {
                            int gap = chipEdge - y;
                            gaps.Add(gap);
                            break;
                        }
                    }
                }
            }
            if (edgePoints.Count > 2)
            {
                line = new Line(edgePoints);
                angle = line.GetAngle();
            }
            else
            {
                angle = 0;
            }

            gaps = RemoveGap(gaps);
            return new Gap
            {
                Min = gaps.Count > 0 ? gaps.Min() : 0,
                Max = gaps.Count > 0 ? gaps.Max() : 0
            };
        }


        private Gap FindDieGapBottom(byte[,] image, int threshold, out double angle, out Line line)
        {


            int width = image.GetLength(1); // [y, x]
            int height = image.GetLength(0); // [y, x]
            int centerX = width / 2;
            int centerY = height / 2;
            line = new Line();
            // 1. 중앙 칩의 좌/우 엣지 찾기 (X범위 결정)
            int chipLeft = -1, chipRight = -1;

            for (int x = width / 2; x > 0; x--)
            {
                if (image[centerY, x] < threshold && image[centerY, x - 1] >= threshold) // [y, x]
                {
                    chipLeft = x;
                    break;
                }
            }
            for (int x = width / 2; x < width - 1; x++)
            {
                if (image[centerY, x] < threshold && image[centerY, x + 1] >= threshold) // [y, x]
                {
                    chipRight = x;
                    break;
                }
            }



            if (chipLeft < 0 || chipRight < 0 || chipRight <= chipLeft)
            {
                angle = 0;
                return new Gap { Min = 0, Max = 0 };
            }
            // 2. 좌우 90%만 사용
            int xStart = chipLeft + (int)((chipRight - chipLeft) * 0.1);
            int xEnd = chipRight - (int)((chipRight - chipLeft) * 0.1);
            List<int> gaps = new List<int>();
            List<PointF> edgePoints = new List<PointF>();
            for (int x = xStart; x < xEnd; x++)
            {
                // 중앙칩의 아래쪽 엣지 찾기 (중앙에서 아래로)

                int chipEdge = -1;
                for (int y = centerY; y < height - 4; y++)
                {
                    if (image[y, x] < threshold && image[y + 3, x] >= threshold) // [y, x]
                    {
                        chipEdge = y;
                        edgePoints.Add(new PointF(x, y));
                        break;
                    }
                }
                if (chipEdge > 0 && chipEdge < height - 1)
                {
                    // 옆칩의 위쪽 엣지 찾기 — 먼지(수 px)는 어두움이 지속되지 않으므로 통과시키고 실제 다이만 인정(2026-07-12)
                    for (int y = chipEdge + 3; y < height - 4; y++)
                    {

                        if (image[y, x] >= threshold && image[y + 3, x] < threshold // [y, x]
                            && IsNeighborDieDark(image, y + 3, x, 1, 0, threshold, NeighborDieSpanPx))
                        {
                            int gap = y - chipEdge;
                            gaps.Add(gap);
                            break;
                        }
                    }
                }

            }
            if (edgePoints.Count > 2)
            {
                line = new Line(edgePoints);
                angle = line.GetAngle();
            }
            else
            {
                angle = 0;
            }

            gaps = RemoveGap(gaps);
            return new Gap
            {
                Min = gaps.Count > 0 ? gaps.Min() : 0,
                Max = gaps.Count > 0 ? gaps.Max() : 0
            };
        }


        // 메인 함수: 4개 방향 Gap/Angle 계산 및 평균 각도 반환
        private GapSet FindDieGaps(byte[,] image, int threshold, out double dAngle, out List<PointF> corners)
        {
            double angleLeft, angleRight, angleTop, angleBottom;
            Line lineLeft, lineRight, lineTop, lineBottom;
            // 각 방향의 Gap과 Angle 찾기
            // FindDieGapLeft, FindDieGapRight, FindDieGapTop, FindDieGapBottom 메서드를 사용
            // 각 방향의 Gap과 Angle을 찾고, 이상적인 각도(왼쪽: 90도, 수평: 0도)와 비교하여 평균 각도를 계산


            Gap left = FindDieGapLeft(image, threshold, out angleLeft, out lineLeft);
            Gap right = FindDieGapRight(image, threshold, out angleRight, out lineRight);
            Gap top = FindDieGapTop(image, threshold, out angleTop, out lineTop);
            Gap bottom = FindDieGapBottom(image, threshold, out angleBottom, out lineBottom);

            left.Max *= _visionConfig.TargetVision.PixelSizeWidthMm; // mm 단위로 변환
            right.Max *= _visionConfig.TargetVision.PixelSizeWidthMm; // mm 단위로 변환
            top.Max *= _visionConfig.TargetVision.PixelSizeHeightMm; // mm 단위로 변환
            bottom.Max *= _visionConfig.TargetVision.PixelSizeHeightMm; // mm 단위로 변환



            left.Min *= _visionConfig.TargetVision.PixelSizeWidthMm; // mm 단위로 변환
            right.Min *= _visionConfig.TargetVision.PixelSizeWidthMm; // mm 단위로 변환
            top.Min *= _visionConfig.TargetVision.PixelSizeHeightMm; // mm 단위로 변환
            bottom.Min *= _visionConfig.TargetVision.PixelSizeHeightMm; // mm 단위로 변환



            // 각 변의 이상적인 각도와의 차이(절대값)
            double diffLeft = NormalizeAngle(angleLeft + 90);
            double diffRight = NormalizeAngle(angleRight + 90);
            double diffTop = NormalizeAngle(angleTop - 0.0);
            double diffBottom = NormalizeAngle(angleBottom - 0.0);
            int count = 0;
            double dAngleSum = 0.0;
            //if (diffLeft != 0)
            //{
            //    count++;
            //    dAngleSum+= diffLeft;
            //}

            //if (diffRight != 0)
            //{
            //    count++;
            //    dAngleSum+= diffRight;
            //}
            if (diffTop != 0)
            {
                count++;
                dAngleSum += diffTop;
            }
            if (diffBottom != 0)
            {
                count++;
                dAngleSum += diffBottom;
            }
            // 평균 각도 계산 (0으로 나누기 방지)
            if (count > 0)
            {
                dAngle = dAngleSum / count;
            }
            else
            {
                dAngle = 0.0; // 모든 각도가 0인 경우
            }
            // 4개의 교차점 생성후 Result에 저장
            PointF leftTop = new PointF();
            PointF rightTop = new PointF();
            PointF rightBottom = new PointF();
            PointF leftBottom = new PointF();
            lineLeft.GetCrossPoint(lineTop, out leftTop);
            lineRight.GetCrossPoint(lineTop, out rightTop);
            lineRight.GetCrossPoint(lineBottom, out rightBottom);
            lineLeft.GetCrossPoint(lineBottom, out leftBottom);

            corners = new List<PointF>
            {
                leftTop,
                rightTop,
                rightBottom,
                leftBottom
            };



            return new GapSet
            {
                Left = left,
                Right = right,
                Top = top,
                Bottom = bottom
            };
        }

        // 직선의 각도(도 단위) 반환 (수직: 90, 수평: 0)
        private double GetLineAngle(List<PointF> points, bool horizontal = false)
        {
            int n = points.Count;
            if (n < 2) return horizontal ? 0.0 : 90.0;

            double sumX = 0, sumY = 0, sumXX = 0, sumXY = 0;
            foreach (var pt in points)
            {
                sumX += pt.X;
                sumY += pt.Y;
                sumXX += pt.X * pt.X;
                sumXY += pt.X * pt.Y;
            }
            double avgX = sumX / n;
            double avgY = sumY / n;

            double denominator = sumXX - sumX * avgX;
            if (Math.Abs(denominator) < 1e-8)
                return 90.0;

            double a = (sumXY - sumX * avgY) / denominator;
            double angleRad = Math.Atan(a);
            double angleDeg = angleRad * 180.0 / Math.PI;
            return horizontal ? angleDeg : 90.0 - angleDeg;
        }

        // -180~180 범위로 각도 정규화
        private double NormalizeAngle(double angle)
        {
            while (angle > 180.0) angle -= 360.0;
            while (angle < -180.0) angle += 360.0;
            return angle;
        }

        public bool CorrectDistortion(DistortionCorrectParameter param)
        {
            // 왜곡 보정 수행
            // param.TargetSearch 값에 따라 크로스 라인 또는 원 검색
            // 실제 보정 알고리즘은 구현 필요

            // 예시: 파라미터에 따라 분기
            switch (param.TargetSearch)
            {
                case DistortionCorrectParameter.SearchType.CrossLine:
                    // 크로스 라인 검색 및 보정 로직
                    // ...
                    break;
                case DistortionCorrectParameter.SearchType.Circle:
                    // 원 검색 및 보정 로직
                    // ...
                    break;
            }

            // 실제 구현에서는 보정 성공 여부 반환
            // 여기서는 예시로 true 반환
            return true;
        }

        public PointD CalcPixelPitch(ScaleCalcParameter param)
        {
            if (param == null)
                throw new System.ArgumentNullException(nameof(param));
            if (param.ImageWidth <= 0 || param.ImageHeight <= 0)
            {
                param.ImageWidth = 12000;
                param.ImageHeight = 12000;
            }
            BottomInspectionParameter bip = new BottomInspectionParameter();
            double dScaleX = 1;
            double dScaleY = 1;
            double pixelPitchX = _visionConfig.BottomVision.PixelSizeWidthMm * dScaleX;
            double pixelPitchY = _visionConfig.BottomVision.PixelSizeHeightMm * dScaleY; ;

            try
            {
                bip.ChipRoi = param.ChipRoi;
                bip.Threshold = param.Threshold;
                bip.ImageWidth = param.ImageWidth;
                bip.ImageHeight = param.ImageHeight;
                bip.ChipUpperSpecLimit = new SizeF((float)param.ChipWidthMm + 1, (float)param.ChipHeightMm + 1);
                bip.ChipLowerSpecLimit = new SizeF((float)param.ChipWidthMm - 1, (float)param.ChipHeightMm - 1);

                bip.Images = param.Images;
                var result = BottomInspect(bip);
                dScaleX = param.ChipWidthMm / result.Width;
                dScaleY = param.ChipHeightMm / result.Height;
                pixelPitchX = _visionConfig.BottomVision.PixelSizeWidthMm * dScaleX * 1000;
                pixelPitchY = _visionConfig.BottomVision.PixelSizeHeightMm * dScaleY * 1000;
                
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            return new PointD(pixelPitchX, pixelPitchY);
        }

        /// <summary>
        /// 칩의 4개 코너에서 각각 다른 방식으로 영역을 잘라서 모션 블러 길이와 각도를 추정합니다.
        /// </summary>
        /// <param name="resultChppingNForeign">칩핑 검사 결과</param>
        /// <param name="shiftImage">원본 이미지</param>
        /// <param name="imageWidth">이미지 너비</param>
        /// <param name="imageHeight">이미지 높이</param>
        /// <param name="cornerAngles">4개 코너의 각도 배열 (LeftTop, RightTop, RightBottom, LeftBottom)</param>
        /// <param name="cornerLengths">4개 코너의 길이 배열 (LeftTop, RightTop, RightBottom, LeftBottom)</param>
        /// <returns>평균 블러 길이</returns>
        private double EstimateBlurLengthFromAllChipCorners(QMC_ResultChppingNForeign resultChppingNForeign, 
            byte[,] shiftImage, int imageWidth, int imageHeight, 
            out double[] cornerAngles, out double[] cornerLengths)
        {
            cornerAngles = new double[4];
            cornerLengths = new double[4];
            
            const int cropWidth = 350;
            const int cropHeight = 300;
            const int margin = 50;
            
            try
            {
                // 4개 코너의 좌표
                PointF[] corners = {
                    resultChppingNForeign.LeftTop,      // 0: Left-Top
                    resultChppingNForeign.RightTop,     // 1: Right-Top  
                    resultChppingNForeign.RightBottom,  // 2: Right-Bottom
                    resultChppingNForeign.LeftBottom    // 3: Left-Bottom
                };

                string[] cornerNames = { "LeftTop", "RightTop", "RightBottom", "LeftBottom" };
                
                Log.Write("MotionDeblur", "=== 4개 코너 블러 분석 시작 (모든 코너 50픽셀 여백) ===");

                for (int i = 0; i < 4; i++)
                {
                    double angle, length;
                    int offsetX, offsetY;
                    
                    // 각 코너별로 50픽셀 여백을 위한 오프셋 적용
                    switch (i)
                    {
                        case 0: // LeftTop: x-50, y-50 (기존 방식)
                            offsetX = -margin;
                            offsetY = -margin;
                            break;
                            
                        case 1: // RightTop: x-(300-50), y-50 (왼쪽으로 크롭하여 50픽셀 여백)
                            offsetX = -(cropWidth - margin);  // -(300-50) = -250
                            offsetY = -margin;                // -50
                            break;
                            
                        case 2: // RightBottom: x-(300-50), y-(250-50) (왼쪽 위로 크롭하여 50픽셀 여백)
                            offsetX = -(cropWidth - margin);  // -(300-50) = -250
                            offsetY = -(cropHeight - margin); // -(250-50) = -200
                            break;
                            
                        case 3: // LeftBottom: x-50, y-(250-50) (위로 크롭하여 50픽셀 여백)
                            offsetX = -margin;                // -50
                            offsetY = -(cropHeight - margin); // -(250-50) = -200
                            break;
                            
                        default:
                            offsetX = -margin;
                            offsetY = -margin;
                            break;
                    }
                    
                    EstimateBlurLengthFromSingleCornerWithOffset(
                        corners[i], cornerNames[i], shiftImage, imageWidth, imageHeight,
                        offsetX, offsetY, cropWidth, cropHeight, out angle, out length);
                    
                    cornerAngles[i] = angle;
                    cornerLengths[i] = length;
                    
                    Log.Write("MotionDeblur", $"{cornerNames[i]} - 오프셋({offsetX}, {offsetY}) 각도: {angle:F2}°, 길이: {length:F2}px");
                }

                // 유효한 값들만으로 평균 계산
                var validLengths = cornerLengths.Where(l => l > 0).ToArray();
                double avgLength = validLengths.Length > 0 ? validLengths.Average() : 0;
                
                Log.Write("MotionDeblur", $"=== 평균 블러 길이: {avgLength:F2}px ===");
                
                return avgLength;
            }
            catch (Exception ex)
            {
                Log.Write("MotionDeblur", $"4개 코너 블러 분석 중 오류: {ex.Message}");
                Log.Write(ex);
                
                // 실패 시 기본값 설정
                for (int i = 0; i < 4; i++)
                {
                    cornerAngles[i] = 0;
                    cornerLengths[i] = 0;
                }
                return 0.0;
            }
        }

        /// <summary>
        /// 단일 코너에서 지정된 오프셋과 크기로 블러 길이와 각도를 추정합니다.
        /// </summary>
        /// <param name="cornerPoint">코너 좌표</param>
        /// <param name="cornerName">코너 이름 (디버깅용)</param>
        /// <param name="shiftImage">원본 이미지</param>
        /// <param name="imageWidth">이미지 너비</param>
        /// <param name="imageHeight">이미지 높이</param>
        /// <param name="offsetX">X 오프셋 (음수: 왼쪽, 양수: 오른쪽)</param>
        /// <param name="offsetY">Y 오프셋 (음수: 위쪽, 양수: 아래쪽)</param>
        /// <param name="cropWidth">크롭 너비</param>
        /// <param name="cropHeight">크롭 높이</param>
        /// <param name="estimatedAngle">추정된 각도</param>
        /// <param name="estimatedLength">추정된 길이</param>
        /// <returns>추정된 블러 길이</returns>
        private double EstimateBlurLengthFromSingleCornerWithOffset(PointF cornerPoint, string cornerName,
            byte[,] shiftImage, int imageWidth, int imageHeight, int offsetX, int offsetY,
            int cropWidth, int cropHeight, out double estimatedAngle, out double estimatedLength)
        {
            estimatedAngle = 0;
            estimatedLength = 0;
            
            try
            {
                // 1. 크롭 영역 계산 (코너에서 지정된 오프셋 적용)
                int cropX = (int)(cornerPoint.X + offsetX);
                int cropY = (int)(cornerPoint.Y + offsetY);

                // 2. 경계 검사 및 조정
                cropX = Math.Max(0, cropX);
                cropY = Math.Max(0, cropY);
                
                // 이미지 경계를 벗어나지 않도록 조정
                if (cropX + cropWidth > imageWidth)
                    cropWidth = imageWidth - cropX;
                if (cropY + cropHeight > imageHeight)
                    cropHeight = imageHeight - cropY;

                // 크롭 영역이 너무 작으면 처리하지 않음
                if (cropWidth < 100 || cropHeight < 100)
                {
                    Log.Write("MotionDeblur", $"{cornerName} - 크롭 영역이 너무 작습니다: {cropWidth}x{cropHeight}");
                    return 0.0;
                }

                Log.Write("MotionDeblur", $"{cornerName} - 오프셋({offsetX}, {offsetY}) 크롭 영역: ({cropX}, {cropY}) {cropWidth}x{cropHeight}");

                // 3. 영역 추출
                byte[] croppedImageData = ExtractImageRegion(shiftImage, imageWidth, imageHeight, 
                    cropX, cropY, cropWidth, cropHeight);

                // 4. EstimateMotionParametersAdvanced 호출
                MotionDeblur.EstimateMotionParametersAdvanced(croppedImageData, cropWidth, cropHeight, 
                    out estimatedAngle, out estimatedLength);

                // 5. 선택사항: 크롭된 이미지 저장 (디버깅용)
                //if (Defact % 20 == 0) // 20번에 한 번만 저장 (4개 코너 * 5회)
                {
                    SaveCroppedImageForDebugging(croppedImageData, cropWidth, cropHeight, 
                        estimatedAngle, estimatedLength, $"{cornerName}_Offset({offsetX},{offsetY})");
                }

                return estimatedLength;
            }
            catch (Exception ex)
            {
                Log.Write("MotionDeblur", $"{cornerName} - 블러 길이 추정 중 오류: {ex.Message}");
                return 0.0;
            }
        }

        /// <summary>
        /// 2D 이미지 배열에서 지정된 영역을 1D 배열로 추출합니다.
        /// </summary>
        /// <param name="sourceImage">원본 2D 이미지 배열</param>
        /// <param name="sourceWidth">원본 이미지 너비</param>
        /// <param name="sourceHeight">원본 이미지 높이</param>
        /// <param name="cropX">크롭 시작 X 좌표</param>
        /// <param name="cropY">크롭 시작 Y 좌표</param>
        /// <param name="cropWidth">크롭 너비</param>
        /// <param name="cropHeight">크롭 높이</param>
        /// <returns>크롭된 이미지 데이터 (1D 배열)</returns>
        private byte[] ExtractImageRegion(byte[,] sourceImage, int sourceWidth, int sourceHeight,
            int cropX, int cropY, int cropWidth, int cropHeight)
        {
            // 입력 검증
            if (sourceImage == null)
                throw new ArgumentNullException(nameof(sourceImage));

            if (cropWidth <= 0 || cropHeight <= 0)
                throw new ArgumentException("크롭 크기가 유효하지 않습니다.");

            // 결과 배열 생성
            byte[] result = new byte[cropWidth * cropHeight];

            try
            {
                // 효율적인 메모리 복사를 위한 방법
                for (int y = 0; y < cropHeight; y++)
                {
                    int sourceY = cropY + y;
                    int resultStartIndex = y * cropWidth;

                    // 경계 검사
                    if (sourceY >= 0 && sourceY < sourceHeight)
                    {
                        for (int x = 0; x < cropWidth; x++)
                        {
                            int sourceX = cropX + x;

                            // 경계 검사
                            if (sourceX >= 0 && sourceX < sourceWidth)
                            {
                                result[resultStartIndex + x] = sourceImage[sourceY, sourceX];
                            }
                            else
                            {
                                result[resultStartIndex + x] = 0; // 경계 밖은 0으로 설정
                            }
                        }
                    }
                    else
                    {
                        // 경계 밖인 줄은 모두 0으로 설정
                        Array.Clear(result, resultStartIndex, cropWidth);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("ExtractImageRegion", $"이미지 영역 추출 중 오류: {ex.Message}");
                throw;
            }

            return result;
        }
        /// <summary>
        /// 디버깅용으로 크롭된 이미지를 저장합니다 (코너 이름 포함).
        /// </summary>
        /// <param name="croppedData">크롭된 이미지 데이터</param>
        /// <param name="width">이미지 너비</param>
        /// <param name="height">이미지 높이</param>
        /// <param name="angle">추정된 각도</param>
        /// <param name="length">추정된 길이</param>
        /// <param name="cornerName">코너 이름</param>
        private void SaveCroppedImageForDebugging(byte[] croppedData, int width, int height, 
            double angle, double length, string cornerName)
        {
            try
            {
                string debugDir = "d:\\Log\\Image\\BlurAnalysis";
                IfNotExistMakeFolder(debugDir);

                string fileName = Path.Combine(debugDir, 
                    $"BlurCrop_A{angle:F1}_L{length:F1}_{cornerName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                // 1D 배열을 2D 배열로 변환
                byte[,] image2D = new byte[height, width];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        image2D[y, x] = croppedData[y * width + x];
                    }
                }

                // 기존 SaveImage 메서드 활용
                SaveImage(image2D, width, height, fileName);

                Log.Write("MotionDeblur", $"{cornerName} - 블러 분석용 이미지 저장: {fileName}");
            }
            catch (Exception ex)
            {
                Log.Write("MotionDeblur", $"{cornerName} - 디버깅 이미지 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 4개 코너의 블러 분석 결과를 기반으로 최적의 각도와 길이를 선택합니다.
        /// </summary>
        /// <param name="cornerAngles">4개 코너의 각도</param>
        /// <param name="cornerLengths">4개 코너의 길이</param>
        /// <param name="bestAngle">최적 각도</param>
        /// <param name="bestLength">최적 길이</param>
        /// <returns>유효한 결과가 있는지 여부</returns>
        private bool SelectBestBlurParameters(double[] cornerAngles, double[] cornerLengths, 
            out double bestAngle, out double bestLength)
        {
            bestAngle = 0;
            bestLength = 0;

            try
            {
                // 유효한 데이터만 필터링 (길이 > 0이고 각도가 110~130도 범위)
                var validData = new List<(double angle, double length, int index)>();
                
                for (int i = 0; i < 4; i++)
                {
                    if ( (120<cornerAngles[i]   && cornerAngles[i] < 122)  || (59 < cornerAngles[i] && cornerAngles[i] < 61))
                    {
                        validData.Add((cornerAngles[i], cornerLengths[i], i));
                    }
                }

                if (validData.Count <2)
                {
                    Log.Write("MotionDeblur", "유효한 블러 데이터가 없습니다");
                    return false;
                }

                // 가장 긴 블러 길이를 가진 데이터 선택 (블러가 가장 심한 부분)
                var bestData = validData.OrderByDescending(x => x.length).First();
                bestAngle = bestData.angle;
                bestLength = bestData.length;

                string[] cornerNames = { "LeftTop", "RightTop", "RightBottom", "LeftBottom" };
                Log.Write("MotionDeblur", $"최적 블러 파라미터: {cornerNames[bestData.index]} - 각도: {bestAngle:F2}°, 길이: {bestLength:F2}px");

                return true;
            }
            catch (Exception ex)
            {
                Log.Write("MotionDeblur", $"최적 블러 파라미터 선택 중 오류: {ex.Message}");
                return false;
            }
        }
        // ... CDTInspector 클래스 내부에 추가 ...
        /// <summary>
        /// Gap 리스트에서 이상치(노이즈)를 제거합니다.
        /// 간단히: 평균과 표준편차를 이용해 극단값을 제거하거나, 0 이하 값 제거 등.
        /// </summary>
        private List<int> RemoveGap(List<int> gaps)
        {
            if (gaps == null || gaps.Count == 0)
                return new List<int>();

            // 0 이하 값 제거
            var filtered = gaps.Where(g => g > 0).ToList();

            if (filtered.Count < 3)
            {
                return filtered;
            }

            // 평균, 표준편차 계산
            double avg = filtered.Average();
            double std = Math.Sqrt(filtered.Average(v => Math.Pow(v - avg, 2)));

            // 평균 ± 2*표준편차 범위 내 값만 남김
            return filtered.Where(g => Math.Abs(g - avg) <= 2 * std).ToList();
        }
    }
    class SaveImageHelper : Object
    {
        public byte[,] ShiftImage { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string FileName { get; set; }
        
        public int Area { get;set; }
        public BottomResult Result{ get; set; } = null;
    }

}
