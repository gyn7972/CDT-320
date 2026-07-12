using System;
using System.Collections.Concurrent;
using System.Threading;
using QMC.Common;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// 검사 결과 이미지 저장 전용 큐(2026-07-12) — PNG 인코드/디스크 쓰기를 검사 스레드에서 분리한다.
    /// <para>종전에는 검사마다 Task.Factory.StartNew 로 인코드가 검사 워커와 같은 스레드풀에서 병렬 실행되어
    /// 병렬 검사 tact 를 크게 부풀렸다(131MP PNG 인코드 ≈ CPU 1~2초/장). 저장 내용/경로/파일명 규칙은
    /// 그대로이고 실행 시점만 전용 저장 스레드(2개, BelowNormal)로 옮긴다 — 검사 결과 계산에 영향 없음.</para>
    /// </summary>
    public static class ImageSaveQueue
    {
        private static readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private const int WorkerCount = 2;

        static ImageSaveQueue()
        {
            for (int i = 0; i < WorkerCount; i++)
            {
                var t = new Thread(Worker)
                {
                    IsBackground = true,
                    Name = "ImageSave-" + (i + 1),
                    Priority = ThreadPriority.BelowNormal   // 검사 워커에 CPU 양보
                };
                t.Start();
            }
        }

        private static void Worker()
        {
            foreach (Action job in _queue.GetConsumingEnumerable())
            {
                try { job(); }
                catch (Exception ex) { try { Log.Write(ex); } catch { } }
            }
        }

        /// <summary>저장 작업 등록 — 즉시 반환(비차단). 작업 예외는 워커에서 로그로 흡수.</summary>
        public static void Enqueue(Action job)
        {
            if (job == null) return;
            try { _queue.Add(job); }
            catch (Exception ex) { try { Log.Write(ex); } catch { } }
        }

        /// <summary>대기 중 저장 작업 수(진단용).</summary>
        public static int Pending => _queue.Count;
    }
}
