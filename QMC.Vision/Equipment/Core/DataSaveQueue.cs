using System;
using System.Collections.Concurrent;
using System.Threading;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 데이터 파일 저장 전용 백그라운드 큐(단일 소비 스레드) — <see cref="ImageLogSaver"/>의 이미지 큐와 동일 패턴.
    /// 검사/시퀀스 스레드는 저장 작업을 큐에 넣기만 하고 즉시 복귀한다(디스크 지연이 사이클 타임에 미포함).
    /// CSV 등 텍스트 위주라 포화될 일은 사실상 없지만, 포화 시 드롭하고 로그를 남긴다(검사 지연보다 손실 우선).
    /// </summary>
    public static class DataSaveQueue
    {
        private const int MaxQueue = 256;
        private static readonly BlockingCollection<Tuple<string, Action>> _q
            = new BlockingCollection<Tuple<string, Action>>(MaxQueue);
        private static int _workerStarted;

        /// <summary>저장 작업 등록(논블로킹). 큐 포화 시 false 반환 + 디버그 로그.</summary>
        public static bool Enqueue(string tag, Action job)
        {
            if (job == null) return false;
            try
            {
                if (Interlocked.CompareExchange(ref _workerStarted, 1, 0) == 0)
                {
                    var th = new Thread(SaveLoop) { IsBackground = true, Name = "DataSaveQueue", Priority = ThreadPriority.BelowNormal };
                    th.Start();
                }
                bool ok = _q.TryAdd(Tuple.Create(tag ?? "", job));
                if (!ok) System.Diagnostics.Debug.WriteLine("[DataSaveQueue] 큐 포화 — 저장 생략: " + tag);
                return ok;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[DataSaveQueue] Enqueue 실패(" + tag + "): " + ex.Message);
                return false;
            }
        }

        private static void SaveLoop()
        {
            foreach (var job in _q.GetConsumingEnumerable())
            {
                try
                {
                    job.Item2();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("[DataSaveQueue] 저장 실패(" + job.Item1 + "): " + ex.Message);
                }
            }
        }
    }
}
