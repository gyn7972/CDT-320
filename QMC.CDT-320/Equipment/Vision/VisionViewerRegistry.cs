using System.Collections.Generic;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// 현재 활성 뷰어 스트림(포트별)을 추적하는 전역 레지스트리.
    /// <see cref="VisionViewerSource"/>가 Live 시작/정지 시 등록·해제한다. 추가 네트워크 트래픽 없음.
    /// "VISION 연결" 페이지가 포트별 '스트리밍 중' 여부를 표시하는 데 사용한다.
    /// </summary>
    public static class VisionViewerRegistry
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<int, int> _active = new Dictionary<int, int>(); // port -> 활성 스트림 수

        public static void StreamStarted(int port)
        {
            if (port <= 0) return;
            lock (_lock)
            {
                int n;
                _active[port] = (_active.TryGetValue(port, out n) ? n : 0) + 1;
            }
        }

        public static void StreamStopped(int port)
        {
            if (port <= 0) return;
            lock (_lock)
            {
                int n;
                if (_active.TryGetValue(port, out n))
                {
                    if (n <= 1) _active.Remove(port);
                    else _active[port] = n - 1;
                }
            }
        }

        public static bool IsStreaming(int port)
        {
            if (port <= 0) return false;
            lock (_lock)
            {
                int n;
                return _active.TryGetValue(port, out n) && n > 0;
            }
        }
    }
}
