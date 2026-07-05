using System;
using System.Collections.Generic;

namespace QMC.Vision.Optics
{
    /// <summary>
    /// 조명 컨트롤러 페이지별 마지막 송신 채널값 캐시.
    /// <para>
    /// 배치 적용(SetChannelBatchAsync) 시 이전 송신값과 같으면(캐시 히트) 시리얼 통신과
    /// 안정화 대기(SettleDelayMs)를 모두 생략해 그랩마다 조명을 적용해도 비용이 들지 않게 한다.
    /// 채널 단위 개별 명령(SetPower/SetOnOff 등)이 나가면 배치 캐시가 어긋나므로 Clear 로 무효화한다.
    /// </para>
    /// </summary>
    internal sealed class LightBatchCache
    {
        private readonly Dictionary<int, int[]> _byPage = new Dictionary<int, int[]>();
        private readonly object _lock = new object();

        /// <summary>해당 페이지의 마지막 송신값과 동일한지(캐시 히트).</summary>
        public bool IsHit(int page, int[] values)
        {
            if (values == null) return false;
            lock (_lock)
            {
                int[] prev;
                if (!_byPage.TryGetValue(page, out prev) || prev == null || prev.Length != values.Length) return false;
                for (int i = 0; i < values.Length; i++)
                    if (prev[i] != values[i]) return false;
                return true;
            }
        }

        /// <summary>송신 성공한 값을 페이지 캐시에 보관.</summary>
        public void Store(int page, int[] values)
        {
            if (values == null) return;
            lock (_lock) _byPage[page] = (int[])values.Clone();
        }

        /// <summary>캐시 전체 무효화 — 재연결, 채널 단위 개별 명령 후 호출(다음 배치는 반드시 송신).</summary>
        public void Clear()
        {
            lock (_lock) _byPage.Clear();
        }
    }
}
