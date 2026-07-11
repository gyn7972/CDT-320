using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System;
using QMC.Vision.Inspector;

// 1. C++의 LineParams 구조체와 일치하는 C# 구조체 정의
[StructLayout(LayoutKind.Sequential)]
public struct LineParams
{
    public float slope;
    public float intercept;
}

// CUDA로부터 받을 블롭 정보 구조체
[StructLayout(LayoutKind.Sequential)]
public struct BlobInfo
{
    public int MinX;
    public int MaxX;
    public int MinY;
    public int MaxY;
    public int PointCount;
    public int PointsStartIndex;
}

public class CudaWrapper
{
    // 2. kernel.cu에 추가한 FindChipping 함수에 대한 P/Invoke 선언
    [DllImport("MakePixelShiftImage.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FindChipping(
        IntPtr h_inputImage,
        IntPtr h_outputMask,
        IntPtr h_outputMask2,
        int width,
        int height,
        LineParams lineTop,
        LineParams lineBottom,
        LineParams lineLeft,
        LineParams lineRight,
        byte threshold,
        int margin,
        int topHatRadius,
        byte topHatThreshold);

    // 수정된 FindBlobsWithCuda DllImport
    [DllImport("MakePixelShiftImage.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FindBlobsWithCuda(
        IntPtr h_inputImage, // CPU 메모리의 이미지 포인터
        int width,
        int height,
        byte threshold,
        int minDefectSize,
        out IntPtr h_blobInfos, // 블롭 정보 배열 포인터
        out int blobCount);     // 찾은 블롭의 수

    [DllImport("MakePixelShiftImage.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FreeCudaHostMemory(IntPtr ptr);

    // 컨텍스트 버전(2026-07-11) — 디바이스 버퍼를 컨텍스트(CudaContextPool)가 보유, 호출마다 할당 없음.
    [DllImport("MakePixelShiftImage.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FindChippingCtx(
        IntPtr ctx,
        IntPtr h_inputImage,
        IntPtr h_outputMask,
        IntPtr h_outputMask2,
        int width,
        int height,
        LineParams lineTop,
        LineParams lineBottom,
        LineParams lineLeft,
        LineParams lineRight,
        byte threshold,
        int margin,
        int topHatRadius,
        byte topHatThreshold);

    [DllImport("MakePixelShiftImage.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int ApplySobelFilter(
        IntPtr h_inputImage,
        IntPtr h_outputImage,
        int width,
        int height);

    // ── CUDA 가용성 캐시 ─────────────────────────────────
    // 무-CUDA(개발) PC 에서 매 검사마다 MakePixelShiftImage.dll(CUDA) 를 호출해 error 35(드라이버 없음)를
    // 반복 출력/예외 발생시키던 문제를 막는다. 첫 호출에서 실패(예외 또는 status!=0)하면 이후 호출은 건너뛰고
    // 빈 결과를 반환한다(실패 시 기존 동작과 동일한 결과 — 콘솔 스팸/예외 반복만 제거).
    // 실제 CUDA 장비 PC 는 첫 호출이 성공(0)하므로 계속 GPU 경로를 사용한다.
    // -1=미확인, 0=불가(이후 스킵), 1=가능.
    private static int _cudaState = -1;

    /// <summary>CUDA 가 사용 불가로 확정되었는가(첫 호출 실패 후 true). 진단용.</summary>
    public static bool CudaUnavailable => _cudaState == 0;

    /// <summary>이번 호출에서 CUDA 시도를 건너뛸지 여부(불가로 확정된 경우).</summary>
    private static bool SkipCuda => _cudaState == 0;

    /// <summary>CUDA 호출 결과 기록 — 성공(0)이면 가능, 실패/예외면 불가로 고정.</summary>
    private static void MarkCuda(bool ok)
    {
        _cudaState = ok ? 1 : 0;
    }

    //public byte[,] DetectChippingWithCuda(byte[,] imageArray, PointF topLeft, PointF topRight, PointF bottomRight, PointF bottomLeft, byte threshold, int margin)
    //{
    //    // 4개의 점으로부터 4개의 Line 객체 생성
    //    var csharpLineTop = new Line(new List<PointF> { topLeft, topRight });
    //    var csharpLineBottom = new Line(new List<PointF> { bottomLeft, bottomRight });
    //    var csharpLineLeft = new Line(new List<PointF> { topLeft, bottomLeft });
    //    var csharpLineRight = new Line(new List<PointF> { topRight, bottomRight });

    //    // 기존의 DetectChippingWithCuda 메서드 호출
    //    return DetectChippingWithCuda(imageArray, csharpLineTop, csharpLineBottom, csharpLineLeft, csharpLineRight, threshold, margin);
    //}

    // 3. C#에서 CUDA 함수를 사용하는 예시 메서드
    public (byte[,] mask1, byte[,] mask2) DetectChippingWithCuda(byte[,] imageArray, Line csharpLineTop, Line csharpLineBottom, Line csharpLineLeft, Line csharpLineRight, byte threshold, int margin, int topHatRadius, byte topHatThreshold)
        => DetectChippingWithCuda(IntPtr.Zero, imageArray, csharpLineTop, csharpLineBottom, csharpLineLeft, csharpLineRight, threshold, margin, topHatRadius, topHatThreshold);

    /// <summary>컨텍스트 버전(2026-07-11) — cudaCtx(CudaContextPool 대여 핸들)가 유효하면 디바이스 버퍼를
    /// 재사용하는 FindChippingCtx 로 호출(호출마다 cudaMalloc 없음). Zero/구 DLL 이면 기존 경로.</summary>
    public (byte[,] mask1, byte[,] mask2) DetectChippingWithCuda(IntPtr cudaCtx, byte[,] imageArray, Line csharpLineTop, Line csharpLineBottom, Line csharpLineLeft, Line csharpLineRight, byte threshold, int margin, int topHatRadius, byte topHatThreshold)
    {
        int height = imageArray.GetLength(0);
        int width = imageArray.GetLength(1);

        // CUDA 불가로 확정된 PC(무-CUDA)에서는 DLL 호출 없이 빈 마스크 반환(반복 호출/스팸 방지).
        if (SkipCuda)
            return (new byte[height, width], new byte[height, width]);

        // C#의 Line 객체를 CUDA가 사용할 LineParams 구조체로 변환
        LineParams cudaLineTop = new LineParams { slope = (float)csharpLineTop.mA, intercept = (float)csharpLineTop.mB };
        LineParams cudaLineBottom = new LineParams { slope = (float)csharpLineBottom.mA, intercept = (float)csharpLineBottom.mB };
        LineParams cudaLineLeft = new LineParams { slope = (float)csharpLineLeft.mA, intercept = (float)csharpLineLeft.mB };
        LineParams cudaLineRight = new LineParams { slope = (float)csharpLineRight.mA, intercept = (float)csharpLineRight.mB };

        // [최적화] 불필요한 1차원 배열 할당 및 복사 제거
        // 원본 2차원 배열과 결과 2차원 배열 메모리를 직접 Pinned 상태로 CUDA에 넘깁니다.
        // C#의 2차원 배열은 메모리상에 행 우선(Row-major) 순서로 연속 할당됩니다.
        byte[,] outputMask2D1 = new byte[height, width];
        byte[,] outputMask2D2 = new byte[height, width];

        GCHandle hInput = GCHandle.Alloc(imageArray, GCHandleType.Pinned);
        GCHandle hOutput1 = GCHandle.Alloc(outputMask2D1, GCHandleType.Pinned);
        GCHandle hOutput2 = GCHandle.Alloc(outputMask2D2, GCHandleType.Pinned);

        try
        {
            IntPtr ptrInput = hInput.AddrOfPinnedObject();
            IntPtr ptrOutput1 = hOutput1.AddrOfPinnedObject();
            IntPtr ptrOutput2 = hOutput2.AddrOfPinnedObject();

            // 첫 번째 FindChipping 호출 (margin 적용) — 컨텍스트 핸들이 있으면 버퍼 재사용 경로.
            int cudaStatus;
            try
            {
                if (cudaCtx != IntPtr.Zero)
                {
                    try
                    {
                        cudaStatus = FindChippingCtx(
                            cudaCtx,
                            ptrInput, ptrOutput1, ptrOutput2,
                            width, height,
                            cudaLineTop, cudaLineBottom, cudaLineLeft, cudaLineRight,
                            threshold, margin, topHatRadius, topHatThreshold);
                    }
                    catch (EntryPointNotFoundException)
                    {
                        // 구버전 DLL(QmcCtx 미탑재) — 기존(호출마다 할당) 경로 폴백.
                        cudaStatus = FindChipping(
                            ptrInput, ptrOutput1, ptrOutput2,
                            width, height,
                            cudaLineTop, cudaLineBottom, cudaLineLeft, cudaLineRight,
                            threshold, margin, topHatRadius, topHatThreshold);
                    }
                }
                else
                {
                    cudaStatus = FindChipping(
                        ptrInput,
                        ptrOutput1,
                        ptrOutput2,
                        width, height,
                        cudaLineTop, cudaLineBottom, cudaLineLeft, cudaLineRight,
                        threshold, margin,
                        topHatRadius,
                        topHatThreshold
                    );
                }
            }
            catch (Exception ex)
            {
                // DLL/익스포트 없음(DllNotFound/EntryPointNotFound) 또는 런타임 실패 → 이후 CUDA 스킵(고정).
                MarkCuda(false);
                Console.WriteLine($"CUDA unavailable -> CPU/skip fixed: {ex.GetType().Name}: {ex.Message}");
                return (outputMask2D1, outputMask2D2);
            }

            if (cudaStatus != 0) // cudaSuccess는 0
            {
                // 첫 실패면 불가로 확정(다음 호출부터 스킵) — 스팸 방지.
                MarkCuda(false);
                Console.WriteLine($"CUDA error 1: {cudaStatus} -> skip subsequent CUDA calls (no-CUDA PC)");
            }
            else
            {
                MarkCuda(true);
            }
        }
        finally
        {
            if (hInput.IsAllocated) hInput.Free();
            if (hOutput1.IsAllocated) hOutput1.Free();
            if (hOutput2.IsAllocated) hOutput2.Free();
        }

        return (outputMask2D1, outputMask2D2);
    }

    public byte[,] ApplySobelFilter(byte[,] imageArray)
    {
        if (SkipCuda)
            return null;

        int height = imageArray.GetLength(0);
        int width = imageArray.GetLength(1);

        // 2차원 배열을 1차원 배열로 변환
        byte[] input1D = new byte[width * height];
        Buffer.BlockCopy(imageArray, 0, input1D, 0, input1D.Length);
        byte[] output1D = new byte[width * height];

        // 메모리 고정
        GCHandle hInput = GCHandle.Alloc(input1D, GCHandleType.Pinned);
        GCHandle hOutput = GCHandle.Alloc(output1D, GCHandleType.Pinned);

        byte[,] outputImage2D = null;

        try
        {
            // CUDA 함수 호출
            int cudaStatus;
            try
            {
                cudaStatus = ApplySobelFilter(
                    hInput.AddrOfPinnedObject(),
                    hOutput.AddrOfPinnedObject(),
                    width, height
                );
            }
            catch (Exception ex)
            {
                MarkCuda(false);
                Console.WriteLine($"CUDA unavailable(Sobel) -> skip fixed: {ex.GetType().Name}: {ex.Message}");
                return null;
            }

            if (cudaStatus != 0) // cudaSuccess는 0
            {
                MarkCuda(false);
                Console.WriteLine($"CUDA error in ApplySobelFilter: {cudaStatus} -> skip subsequent CUDA");
            }
            else
            {
                MarkCuda(true);
                // 결과를 다시 2차원 배열로 변환
                outputImage2D = new byte[height, width];
                Buffer.BlockCopy(output1D, 0, outputImage2D, 0, output1D.Length);
            }
        }
        finally
        {
            hInput.Free();
            hOutput.Free();
        }

        return outputImage2D;
    }
    // CUDA를 사용하여 블blob을 찾는 C# 래퍼 메서드 (수정됨)
    public List<ChippingInfo> DetectChippingBlobsWithCuda(byte[,] imageArray, byte threshold, int minDefectSize)
    {
        var chippingInfos = new List<ChippingInfo>();
        if (SkipCuda)
            return chippingInfos;

        int height = imageArray.GetLength(0);
        int width = imageArray.GetLength(1);
        int imageSize = width * height;

        byte[] image1D = new byte[imageSize];
        Buffer.BlockCopy(imageArray, 0, image1D, 0, imageSize);

        GCHandle h_input = GCHandle.Alloc(image1D, GCHandleType.Pinned);
        IntPtr h_blobInfos = IntPtr.Zero;

        try
        {
            int status;
            int blobCount = 0;
            try
            {
                status = FindBlobsWithCuda(
                    h_input.AddrOfPinnedObject(),
                    width, height, threshold, minDefectSize,
                    out h_blobInfos, out blobCount
                );
            }
            catch (Exception ex)
            {
                MarkCuda(false);
                Console.WriteLine($"CUDA unavailable(FindBlobs) -> skip fixed: {ex.GetType().Name}: {ex.Message}");
                return chippingInfos;
            }

            if (status != 0) MarkCuda(false); else MarkCuda(true);

            if (status == 0 && blobCount > 0)
            {
                int blobInfoSize = Marshal.SizeOf(typeof(BlobInfo));
                for (int i = 0; i < blobCount; i++)
                {
                    IntPtr currentPtr = IntPtr.Add(h_blobInfos, i * blobInfoSize);
                    BlobInfo blobInfo = Marshal.PtrToStructure<BlobInfo>(currentPtr);

                    // CUDA 코드에서 경계 상자 계산을 생략했으므로,
                    // 여기서는 PointCount만 사용하여 ChippingInfo를 생성합니다.
                    // 필요시 CUDA에서 경계상자까지 계산하여 넘겨주도록 확장할 수 있습니다.
                    var chippingInfo = new ChippingInfo
                    {
                        // Contour 정보는 이 구현에서 반환하지 않으므로 비워둡니다.
                        Contour = new List<PointF>(), 
                        // Length/Depth 대신 PointCount를 사용하거나, 0으로 둡니다.
                        Length = blobInfo.PointCount,
                        Depth = 1 
                    };
                    chippingInfos.Add(chippingInfo);
                }
            }
        }
        finally
        {
            h_input.Free();
            if (h_blobInfos != IntPtr.Zero)
            {
                // C++ DLL에서 할당한 메모리를 해제합니다.
                FreeCudaHostMemory(h_blobInfos);
            }
        }

        return chippingInfos;
    }
}