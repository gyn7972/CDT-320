using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 오토포커스 Tact Time 로그 — 시퀀서 사이클 시간 스타일의 정적 링버퍼.
    /// FOCUS_START=사이클 시작, FOCUS_VAL=스텝 tact, FOCUS_BEST=사이클 종료(총 사이클 시간).
    /// (<see cref="VisionCommLog"/> 와 동일한 정적 로그 패턴.)
    /// </summary>
    public static class AutoFocusTactLog
    {
        private const int MaxLines = 500;
        private static readonly LinkedList<string> _lines = new LinkedList<string>();
        private static readonly object _lock = new object();
        private static long _rev;
        private static DateTime _cycleStart = DateTime.MinValue;

        public static long Revision { get { lock (_lock) return _rev; } }

        public static void Add(string line)
        {
            lock (_lock)
            {
                _lines.AddLast(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + line);
                while (_lines.Count > MaxLines) _lines.RemoveFirst();
                _rev++;
            }
        }

        /// <summary>사이클(스캔) 시작 — 경과 기준점 기록.</summary>
        public static void MarkCycleStart(string label)
        {
            lock (_lock) _cycleStart = DateTime.Now;
            Add("── CYCLE START  " + label + " ──");
        }

        /// <summary>사이클 종료 — 시작부터 경과(사이클 시간) 기록.</summary>
        public static void MarkCycleEnd(string label)
        {
            double ms;
            lock (_lock) ms = _cycleStart == DateTime.MinValue ? 0 : (DateTime.Now - _cycleStart).TotalMilliseconds;
            Add("── CYCLE END    " + label + "   cycle=" + (long)ms + "ms ──");
        }

        public static string[] Snapshot()
        {
            lock (_lock) return _lines.ToArray();
        }

        public static void Clear()
        {
            lock (_lock) { _lines.Clear(); _rev++; }
        }
    }
}
