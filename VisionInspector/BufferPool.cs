using System;
using System.Collections.Concurrent;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// 대형 byte[] 버퍼 풀(2026-07-12) — 검사 1건마다 수십~수백 MB LOH 할당(마스크/전치 밴드)이
    /// 반복되며 생기는 GC 압박을 없앤다. '정확히 같은 길이'만 재사용하며, 소비자가 전 구간을
    /// 덮어쓰는 용도(D2H 복사, 전치 결과)로만 사용하므로 계산 결과에는 영향이 없다.
    /// 반납 누락은 누수가 아니라 단순 미재사용(GC 회수) — 실패 모드도 안전하다.
    /// </summary>
    public static class BufferPool
    {
        private static readonly ConcurrentDictionary<int, ConcurrentBag<byte[]>> _pool
            = new ConcurrentDictionary<int, ConcurrentBag<byte[]>>();
        private const int MaxPooledPerSize = 8;

        public static byte[] Rent(int length)
        {
            ConcurrentBag<byte[]> bag;
            byte[] buf;
            if (_pool.TryGetValue(length, out bag) && bag.TryTake(out buf)) return buf;
            return new byte[length];
        }

        public static void Return(byte[] buf)
        {
            if (buf == null) return;
            var bag = _pool.GetOrAdd(buf.Length, _ => new ConcurrentBag<byte[]>());
            if (bag.Count < MaxPooledPerSize) bag.Add(buf);   // 초과분은 GC에 맡김
        }
    }
}
