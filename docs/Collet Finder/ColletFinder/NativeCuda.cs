using System.Runtime.InteropServices;

namespace ColletFinder
{
    /// <summary>
    /// ColletFinderCuda.dll (CUDA 네이티브 백엔드) P/Invoke 선언.
    /// DLL 이 없거나 CUDA 디바이스가 없으면 호출 시 예외/0 이 반환되며,
    /// 상위 로직(StdDevFilter)에서 CPU 경로로 폴백한다.
    /// </summary>
    internal static class NativeCuda
    {
        private const string Dll = "ColletFinderCuda.dll";

        /// <summary>사용 가능한 CUDA 디바이스 수(0 = 없음/드라이버 없음).</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int cf_cuda_device_count();

        /// <summary>디바이스 이름을 buf 에 채운다. 0 = 성공.</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int cf_cuda_device_name(int device, byte[] buf, int bufLen);

        /// <summary>
        /// 표준편차 텍스처 필터(GPU). gray/outGray 는 w*h 바이트(행 우선).
        /// 0 = 성공, 음수 = 실패(이 경우 CPU 로 폴백).
        /// </summary>
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int cf_stddev_filter_cuda(
            byte[] gray, int w, int h,
            int roiX, int roiY, int roiW, int roiH,
            int blockSize, double threshold,
            byte[] outGray);
    }
}
