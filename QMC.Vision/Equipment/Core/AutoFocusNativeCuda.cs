using System.Runtime.InteropServices;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 오토포커스 <b>전용</b> CUDA 네이티브 백엔드 P/Invoke (<c>AutoFocusCuda.dll</c>).
    /// 콜렛 Finder용 <c>ColletFinderCuda.dll</c> 과는 무관한 별도 모듈이다.
    /// <para>
    /// DLL 부재(<see cref="System.DllNotFoundException"/>) / 커널 미export
    /// (<see cref="System.EntryPointNotFoundException"/>) / CUDA 디바이스 부재 시에는
    /// 상위(<see cref="AutoFocusCore"/>)가 자동으로 CPU 경로로 폴백한다.
    /// </para>
    /// </summary>
    internal static class AutoFocusNativeCuda
    {
        private const string Dll = "AutoFocusCuda.dll";

        /// <summary>사용 가능한 CUDA 디바이스 수(0 = 없음/드라이버 없음).</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int af_cuda_device_count();

        /// <summary>디바이스 이름을 buf 에 채운다. 0 = 성공.</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int af_cuda_device_name(int device, byte[] buf, int bufLen);

        /// <summary>
        /// GPU 초점 점수 측정. 입력 <paramref name="gray"/> 는 8bit grayscale(길이 w*h, 행 우선).
        /// CPU <see cref="AutoFocusCore.ScoreFocus(byte[], int, int, int, int)"/> 와 동일 알고리즘을 구현해야 한다:
        ///  - <paramref name="marginFraction"/> 만큼 가장자리 제외(전체프레임=1/3, ROI=0),
        ///  - 밝기 &gt; <paramref name="objThreshold"/> 픽셀만 8-이웃 라플라시안 응답 계산,
        ///  - 응답 상위 200픽셀 평균을 <paramref name="score"/> 에 반환.
        /// 반환값 0 = 성공(<paramref name="score"/> 유효), 음수 = 실패(상위에서 CPU 폴백).
        /// </summary>
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int af_focus_score_cuda(
            byte[] gray, int w, int h,
            int objThreshold, double marginFraction,
            out double score);
    }
}
