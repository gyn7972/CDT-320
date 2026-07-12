using System;
using System.Collections.Concurrent;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// Bottom 검사 host 스크래치 컨텍스트 풀(2026-07-12, 모델 B) — <see cref="CudaContextPool"/> 와 동형.
    /// 기본 8개(콜렛 1~8 동시)를 만들어 두고 검사 1건이 하나를 빌려(Rent) 끝나면 반납(Lease.Dispose)한다.
    /// <para>풀 고갈(동시 검사 &gt; PoolSize) 시에도 안전: 풀 밖 임시 컨텍스트를 반환하며(이때만 할당),
    /// 그 컨텍스트는 반납하지 않고 GC 에 맡긴다. 정상(동시 ≤ 8)에서는 임시 생성이 없다.</para>
    /// </summary>
    public static class BottomScratchPool
    {
        /// <summary>풀 크기(기본 8). 첫 Rent 이전에만 변경 유효.</summary>
        public static int PoolSize { get; set; } = 8;

        private static readonly object _initLock = new object();
        private static ConcurrentBag<BottomInspectContext> _pool;
        private static bool _init;

        /// <summary>스크래치 대여권 — using 으로 감싸면 예외 경로에서도 반납이 보장된다.</summary>
        public struct Lease : IDisposable
        {
            internal BottomInspectContext _ctx;
            internal bool _fromPool;

            /// <summary>빌린 컨텍스트. 항상 유효(고갈 시 임시 컨텍스트).</summary>
            public BottomInspectContext Ctx => _ctx;

            public void Dispose()
            {
                if (_ctx == null) return;
                var c = _ctx;
                _ctx = null;
                if (_fromPool)
                {
                    try { _pool?.Add(c); } catch { /* 종료 중 — GC 회수 */ }
                }
                // 풀 밖 임시 컨텍스트는 반납하지 않음 → 핀 해제 후 GC.
                else
                {
                    try { c.FreePins(); } catch { }
                }
            }
        }

        /// <summary>스크래치 컨텍스트 대여. 고갈 시 풀 밖 임시 컨텍스트(이때만 할당).</summary>
        public static Lease Rent()
        {
            if (!_init) Initialize();
            BottomInspectContext c;
            if (_pool.TryTake(out c)) return new Lease { _ctx = c, _fromPool = true };
            // 고갈(동시 검사 > PoolSize) — 임시 컨텍스트로 안전 폴백(반납 안 함).
            return new Lease { _ctx = new BottomInspectContext(), _fromPool = false };
        }

        private static void Initialize()
        {
            lock (_initLock)
            {
                if (_init) return;
                int size = PoolSize < 1 ? 1 : PoolSize;
                var created = new ConcurrentBag<BottomInspectContext>();
                for (int i = 0; i < size; i++) created.Add(new BottomInspectContext());
                _pool = created;
                _init = true;
                AppDomain.CurrentDomain.ProcessExit += (s, e) => DestroyAll();
            }
        }

        private static void DestroyAll()
        {
            try
            {
                var pool = _pool;
                if (pool == null) return;
                BottomInspectContext c;
                while (pool.TryTake(out c)) { try { c.FreePins(); } catch { } }
            }
            catch { }
        }
    }
}
