using System;
using System.Collections.Generic;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// 핸들러↔Vision 통신 로그(TX/RX/EPD/ARM)의 인메모리 링버퍼. 파일이 아니므로 고빈도에도 저렴.
    /// "VISION 연결" 설정 페이지의 통신 로그 패널이 <see cref="Revision"/> 변경 시에만 <see cref="Snapshot"/>로 갱신한다.
    /// </summary>
    public static class VisionCommLog
    {
        private const int MaxLines = 500;
        private static readonly object _lock = new object();
        private static readonly List<string> _lines = new List<string>(MaxLines + 8);
        private static long _rev;
        private static string _lastMsg;   // 직전 원본 메시지(타임스탬프 제외) — 연속 중복 합치기용
        private static int _dupCount;     // 직전 메시지의 연속 반복 횟수(2회차부터 증가)

        /// <summary>로그가 바뀔 때마다 증가. UI는 값이 바뀐 경우에만 Snapshot을 읽으면 된다.</summary>
        public static long Revision
        {
            get { lock (_lock) { return _rev; } }
        }

        /// <summary>로그 1줄 추가(타임스탬프 자동 부착).
        /// <para>직전 라인과 동일하면 새 줄을 만들지 않고 마지막 줄을 최신 시각 + 반복 횟수(×N)로 갱신한다
        /// — Live 중 EPD 푸시처럼 같은 메시지가 고빈도로 반복돼 로그가 도배되는 것을 방지.</para></summary>
        public static void Add(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            string ts = DateTime.Now.ToString("HH:mm:ss.fff");
            lock (_lock)
            {
                if (_lines.Count > 0 && line == _lastMsg)
                {
                    // 직전과 동일 — 마지막 줄을 갱신(최신 시각 + 반복 횟수). 줄 수는 늘리지 않는다.
                    _dupCount++;
                    _lines[_lines.Count - 1] = ts + "  " + line + "  (×" + (_dupCount + 1) + ")";
                }
                else
                {
                    _lastMsg = line;
                    _dupCount = 0;
                    _lines.Add(ts + "  " + line);
                    while (_lines.Count > MaxLines) _lines.RemoveAt(0);
                }
                _rev++;
            }
        }

        /// <summary>현재 로그 라인 배열(오래된→최신).</summary>
        public static string[] Snapshot()
        {
            lock (_lock) { return _lines.ToArray(); }
        }

        public static void Clear()
        {
            lock (_lock) { _lines.Clear(); _lastMsg = null; _dupCount = 0; _rev++; }
        }
    }
}
