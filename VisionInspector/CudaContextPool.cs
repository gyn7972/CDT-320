using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// CUDA 컨텍스트 풀 (2026-07-11) — MakePixelShiftImage.dll 의 QmcCtx* API 로 디바이스 버퍼 컨텍스트를
    /// 기본 8개 만들어 두고, 검사 1건당 하나를 빌려 쓰고(Rent) 끝나면 반납(Lease.Dispose)한다.
    /// <para>버퍼 크기 정책은 네이티브 쪽 규칙: 이전 호출과 같은 이미지 크기면 그대로 재사용(할당 0회),
    /// ROI 변경 등으로 크기가 달라지면 그 슬롯만 재할당 후 계속 사용 — 재할당된 컨텍스트가 그대로 풀로 반납된다.</para>
    /// <para>폴백: CUDA 불가 PC / 구버전 DLL(QmcCtx* 미탑재)에서는 풀이 비활성화되고 Rent 가 무효 Lease 를
    /// 반환한다 — 호출측은 Handle=IntPtr.Zero 를 보고 기존(호출마다 할당) 경로를 그대로 쓴다.</para>
    /// </summary>
    public static class CudaContextPool
    {
        [DllImport("MakePixelShiftImage.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int QmcCtxCreate(out IntPtr ctx);

        [DllImport("MakePixelShiftImage.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void QmcCtxDestroy(IntPtr ctx);

        /// <summary>풀 크기(기본 8). 첫 Rent 이전에만 변경 유효.</summary>
        public static int PoolSize { get; set; } = 8;

        /// <summary>풀 고갈 시 대기 한도(ms). 초과하면 무효 Lease 반환 → 호출측이 레거시 경로 사용.</summary>
        public static int RentTimeoutMs { get; set; } = 5000;

        private static readonly object _initLock = new object();
        private static BlockingCollection<IntPtr> _pool;
        private static int _state = -1;   // -1=미초기화, 0=불가(폴백 고정), 1=가능

        /// <summary>컨텍스트 대여권 — using 으로 감싸면 예외 경로에서도 풀 반납이 보장된다.</summary>
        public struct Lease : IDisposable
        {
            internal IntPtr _handle;

            /// <summary>네이티브 컨텍스트 핸들. Zero = 풀 비활성/고갈 — 레거시(호출마다 할당) 경로 사용.</summary>
            public IntPtr Handle => _handle;
            public bool IsValid => _handle != IntPtr.Zero;

            public void Dispose()
            {
                if (_handle == IntPtr.Zero) return;
                IntPtr h = _handle;
                _handle = IntPtr.Zero;
                try { _pool?.Add(h); } catch { /* 종료 중 CompleteAdding 등 — 컨텍스트는 프로세스 종료와 함께 회수 */ }
            }
        }

        /// <summary>진단용 강제 비활성(2026-07-12) — 환경변수 QMC_CUDA_CTX_DISABLE=1 이면 풀을 쓰지 않고
        /// 항상 무효 Lease 를 반환한다(레거시 호출마다-할당 경로로 고정). ctx 경로/레거시 경로 결과 대조용.</summary>
        private static readonly bool _forceDisabled =
            Environment.GetEnvironmentVariable("QMC_CUDA_CTX_DISABLE") == "1";

        /// <summary>컨텍스트 대여. 풀 비활성(무-CUDA/구 DLL)이거나 대기 한도 초과면 무효 Lease.</summary>
        public static Lease Rent()
        {
            if (_forceDisabled) return new Lease();
            if (_state == 0) return new Lease();
            if (_state == -1) Initialize();
            if (_state != 1) return new Lease();

            IntPtr h;
            if (_pool.TryTake(out h, RentTimeoutMs)) return new Lease { _handle = h };
            Console.WriteLine("[CudaContextPool] 풀 고갈 " + RentTimeoutMs + "ms 초과 — 레거시 경로 폴백");
            return new Lease();
        }

        private static void Initialize()
        {
            lock (_initLock)
            {
                if (_state != -1) return;
                int size = PoolSize < 1 ? 1 : PoolSize;
                var created = new BlockingCollection<IntPtr>();
                try
                {
                    for (int i = 0; i < size; i++)
                    {
                        IntPtr h;
                        int st = QmcCtxCreate(out h);
                        if (st != 0 || h == IntPtr.Zero)
                        {
                            Console.WriteLine("[CudaContextPool] QmcCtxCreate 실패(status=" + st + ") — 풀 비활성(레거시 경로)");
                            foreach (var c in created) { try { QmcCtxDestroy(c); } catch { } }
                            _state = 0;
                            return;
                        }
                        created.Add(h);
                    }
                }
                catch (Exception ex)
                {
                    // DllNotFound / EntryPointNotFound(구버전 DLL) — 풀 없이 레거시 경로로 계속.
                    Console.WriteLine("[CudaContextPool] 초기화 불가 — 레거시 경로 폴백: " + ex.GetType().Name + ": " + ex.Message);
                    foreach (var c in created) { try { QmcCtxDestroy(c); } catch { } }
                    _state = 0;
                    return;
                }

                _pool = created;
                _state = 1;
                Console.WriteLine("[CudaContextPool] 컨텍스트 " + size + "개 생성 완료");
                AppDomain.CurrentDomain.ProcessExit += (s, e) => DestroyAll();
            }
        }

        private static void DestroyAll()
        {
            try
            {
                var pool = _pool;
                if (pool == null) return;
                IntPtr h;
                while (pool.TryTake(out h, 0)) { try { QmcCtxDestroy(h); } catch { } }
            }
            catch { }
        }
    }
}
