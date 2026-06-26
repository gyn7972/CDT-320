using System;
using System.Collections.Generic;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Handler/Vision 통신 로그(TX/RX/FPD/ARM)를 보관하는 인메모리 링버퍼.
    /// 파일 로그가 아니므로 고빈도 통신에서도 UI 표시용으로만 가볍게 사용한다.
    /// </summary>
    public static class VisionCommLog
    {
        private const int MaxLines = 500;
        private static readonly object _lock = new object();
        private static readonly List<string> _lines = new List<string>(MaxLines + 8);
        private static long _revision;
        private static string _lastMessage;
        private static int _duplicateCount;

        /// <summary>로그가 바뀔 때마다 증가한다. UI는 값이 바뀐 경우에만 Snapshot을 읽으면 된다.</summary>
        public static long Revision
        {
            get { lock (_lock) { return _revision; } }
        }

        /// <summary>
        /// 로그 1줄을 추가한다. 직전 라인과 같으면 새 줄을 만들지 않고 마지막 줄의 반복 횟수만 갱신한다.
        /// </summary>
        public static void Add(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            lock (_lock)
            {
                if (_lines.Count > 0 && line == _lastMessage)
                {
                    _duplicateCount++;
                    _lines[_lines.Count - 1] = timestamp + "  " + line + "  (x" + (_duplicateCount + 1) + ")";
                }
                else
                {
                    _lastMessage = line;
                    _duplicateCount = 0;
                    _lines.Add(timestamp + "  " + line);
                    while (_lines.Count > MaxLines)
                        _lines.RemoveAt(0);
                }

                _revision++;
            }
        }

        /// <summary>현재 로그 스냅샷을 오래된 순서부터 최신 순서까지 반환한다.</summary>
        public static string[] Snapshot()
        {
            lock (_lock)
            {
                return _lines.ToArray();
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _lines.Clear();
                _lastMessage = null;
                _duplicateCount = 0;
                _revision++;
            }
        }
    }
}
