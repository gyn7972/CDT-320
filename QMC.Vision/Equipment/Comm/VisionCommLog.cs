using System;
using System.Collections.Generic;

namespace QMC.Vision.Comm
{
    /// <summary>
    /// 핸들러↔Vision TCP 통신 로그(TX/RX/EPD/ARM 등)를 모으는 프로세스 전역 링버퍼.
    /// 서버(<see cref="VisionTcpServer"/>/<see cref="MainCommServer"/>)의 Log 이벤트를 구독해 적재하고,
    /// 통신 페이지(CommLinkPage)가 주기적으로 <see cref="Snapshot"/> 으로 읽어 표시한다.
    /// 멀티스레드 안전(서버 수신 스레드에서 Add, UI 스레드에서 Snapshot).
    /// </summary>
    public static class VisionCommLog
    {
        private const int MaxLines = 1000;
        private static readonly object _lock = new object();
        private static readonly LinkedList<string> _lines = new LinkedList<string>();

        // 직전 줄과 본문이 같으면 새 줄을 쌓지 않고 마지막 줄을 "(×N)" 카운터로 합친다.
        // LIVE 중 EPD(프레임 완료) 푸시가 초당 수 회 반복돼 로그가 동일 줄로 폭주하는 것을 방지.
        private static string _lastBody;
        private static int    _lastRepeat;

        /// <summary>적재된 라인 수가 바뀔 때마다 증가(페이지가 변경 감지에 사용).</summary>
        public static long Revision { get; private set; }

        public static void Add(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            string now = DateTime.Now.ToString("HH:mm:ss.fff");
            lock (_lock)
            {
                if (line == _lastBody && _lines.Count > 0)
                {
                    // 동일 본문 연속 → 마지막 줄을 시각 갱신 + 반복횟수 표기로 교체.
                    _lastRepeat++;
                    _lines.RemoveLast();
                    _lines.AddLast($"{now}  {line}  (×{_lastRepeat})");
                }
                else
                {
                    _lastBody   = line;
                    _lastRepeat = 1;
                    _lines.AddLast($"{now}  {line}");
                    while (_lines.Count > MaxLines) _lines.RemoveFirst();
                }
                Revision++;
            }
        }

        public static string[] Snapshot()
        {
            lock (_lock)
            {
                var arr = new string[_lines.Count];
                _lines.CopyTo(arr, 0);
                return arr;
            }
        }

        public static void Clear()
        {
            lock (_lock) { _lines.Clear(); _lastBody = null; _lastRepeat = 0; Revision++; }
        }
    }
}
