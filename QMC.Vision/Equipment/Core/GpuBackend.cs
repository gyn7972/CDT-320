using System.Collections.Concurrent;

namespace QMC.Vision.Core
{
    /// <summary>알고리즘이 실제로 사용한 연산 백엔드.</summary>
    public enum ComputeBackend { Cpu, Cuda }

    /// <summary>
    /// CUDA/CPU 백엔드 선택·상태 보고의 중앙 지점 — Collet Finder 의 <c>StdDevFilter.CudaAvailable /
    /// CudaDeviceName / ComputeBackend</c> 패턴을 320 전반에 일반화한 것.
    /// 원칙: "CUDA 가 사용 가능하면 CUDA, 아니면 CPU 폴백". 장비 PC(CUDA DLL 보유)에서는 자동으로 GPU 경로가 켜지고,
    /// 개발 PC(미보유)에서는 모든 호출이 CPU 로 우회된다(<see cref="CudaInterop"/> 가 예외 없이 false 반환).
    /// 각 알고리즘은 한 사이클의 마지막 실행 백엔드를 여기에 기록(<see cref="Note"/>)하여 UI/로그에 노출한다.
    /// </summary>
    public static class GpuBackend
    {
        /// <summary>CUDA DLL+GPU 사용 가능 여부(최초 1회 프로브).</summary>
        public static bool CudaAvailable => CudaInterop.Available;

        /// <summary>CUDA 디바이스 이름(미지원/미보유면 null).</summary>
        public static string DeviceName => CudaInterop.DeviceName;

        private static readonly ConcurrentDictionary<string, ComputeBackend> _last
            = new ConcurrentDictionary<string, ComputeBackend>();

        /// <summary>알고리즘별 직전 실행 백엔드 기록(키=알고리즘 명, 예 "Contamination").</summary>
        public static void Note(string algorithm, ComputeBackend backend)
            => _last[algorithm ?? ""] = backend;

        /// <summary>알고리즘의 직전 실행 백엔드(기록 없으면 Cpu).</summary>
        public static ComputeBackend Last(string algorithm)
            => _last.TryGetValue(algorithm ?? "", out var b) ? b : ComputeBackend.Cpu;

        /// <summary>UI/로그 1줄 상태("CUDA: &lt;device&gt;" 또는 "CPU").</summary>
        public static string StatusLine
            => CudaAvailable ? ("CUDA: " + (DeviceName ?? "GPU")) : "CPU (CUDA 미사용)";
    }
}
