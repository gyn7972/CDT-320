using System;
using System.Collections.Generic;
using System.Drawing;

namespace QMC.Vision.Core
{
    /// <summary>
    /// INSPECTASYNC 로 '그랩만' 해둔 이미지를 모듈별로 보관하는 스토어.
    /// <para>방식 B: 그랩이 예상 개수(픽커 수)에 도달하는 순간 자동으로 일괄 병렬 처리를 트리거한다.
    /// 처리 시작은 <see cref="TryBeginProcessing"/> 로 멱등 보장(정확히 한 번만 true).</para>
    /// <para>안전망: 예상 개수가 채워지지 않아도(부분 배치) 이후 INSPECTRESULT 폴링이 <see cref="TryBeginProcessing"/>
    /// 를 호출해 남은 보관분을 처리하도록 한다(둘 중 먼저 호출된 쪽이 처리, 나머지는 무시).</para>
    /// <para>모든 API 는 스레드 세이프. 이미지 소유권은 <see cref="Take"/> 측으로 이전된다(처리 후 Dispose 책임).</para>
    /// </summary>
    public static class PendingGrabStore
    {
        /// <summary>보관 항목 — 픽커별 그랩 1건.</summary>
        public sealed class Item
        {
            public string Insp;
            public string ChipUid;
            public int    Picker;   // 전역 픽커(1~8: Front 콜렛=1~4, Back 콜렛=5~8). 0 이면 미지정(도착 순서로 대체).
            public int    IndexX;   // 다이 격자 좌표(Bottom 위치맵용). 미지정이면 0.
            public int    IndexY;
            public int    Channel;  // 측면 채널(신형 0=0°/1=90°, 구형 0~3). 해당 없으면 -1.
            public Bitmap Image;
            public long   Gen;      // 그랩 시점의 결과 스토어 세대 — 웨이퍼 경계(Clear) 넘긴 잔여 배치 판별
        }

        private sealed class Batch
        {
            public readonly List<Item> Items = new List<Item>();
            public bool Processing;   // 처리 시작됨(멱등 가드)
        }

        private static readonly Dictionary<string, Batch> _byModule =
            new Dictionary<string, Batch>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new object();

        /// <summary>그랩 이미지 보관 후, 현재 보관 개수를 반환. picker=0 이면 미지정(도착 순서로 대체).
        /// ix/iy=다이 격자(맵용), channel=측면 채널(해당 없으면 -1).</summary>
        public static int Add(string module, string insp, string chipUid, int picker, int ix, int iy, int channel, Bitmap image)
        {
            if (string.IsNullOrEmpty(module)) return 0;
            lock (_lock)
            {
                if (!_byModule.TryGetValue(module, out var b)) { b = new Batch(); _byModule[module] = b; }
                b.Items.Add(new Item { Insp = insp, ChipUid = chipUid, Picker = picker, IndexX = ix, IndexY = iy, Channel = channel, Image = image,
                                       Gen = InspectionResultStore.GenerationOf(module) });
                return b.Items.Count;
            }
        }

        /// <summary>현재 보관 개수.</summary>
        public static int Count(string module)
        {
            if (string.IsNullOrEmpty(module)) return 0;
            lock (_lock)
                return _byModule.TryGetValue(module, out var b) ? b.Items.Count : 0;
        }

        /// <summary>일괄 처리를 '한 번만' 시작하도록 하는 멱등 가드. 처음 호출에서만 true, 이후 false.
        /// 보관분이 없으면 false.</summary>
        public static bool TryBeginProcessing(string module)
        {
            if (string.IsNullOrEmpty(module)) return false;
            lock (_lock)
            {
                if (_byModule.TryGetValue(module, out var b) && !b.Processing && b.Items.Count > 0)
                {
                    b.Processing = true;
                    return true;
                }
                return false;
            }
        }

        /// <summary>보관분을 회수하고 스토어에서 제거(다음 사이클 대비). 회수한 이미지의 Dispose 책임은 호출자.</summary>
        public static List<Item> Take(string module)
        {
            if (string.IsNullOrEmpty(module)) return new List<Item>();
            lock (_lock)
            {
                if (_byModule.TryGetValue(module, out var b))
                {
                    var items = new List<Item>(b.Items);
                    _byModule.Remove(module);
                    return items;
                }
                return new List<Item>();
            }
        }

        // ── 진행 중 그랩(in-flight) 추적 — 안전망 조기 발화 방지 ──
        // INSPECTASYNC 수락 시점에 +1, 그랩 완료/실패 시 -1. 안전망(INSPECTRESULT 대기 루프)은
        // in-flight 가 0일 때만 부분 배치를 처리한다. 그랩이 아직 쌓이는 중에 발화하면
        // 배치가 1+3 등으로 쪼개져 진짜 병렬이 깨진다(2s 시간 유예만으로는 그랩 3s+ 케이스에서 부족).
        private static readonly Dictionary<string, int> _inflight =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>그랩 시작 예고(+1). INSPECTASYNC 수락 직후 호출.</summary>
        public static void NoteGrabStarted(string module)
        {
            if (string.IsNullOrEmpty(module)) return;
            lock (_lock) { _inflight.TryGetValue(module, out int n); _inflight[module] = n + 1; }
        }

        /// <summary>그랩 종료(-1). 성공(Add 후)/실패/예외 모든 경로에서 호출(finally).</summary>
        public static void NoteGrabEnded(string module)
        {
            if (string.IsNullOrEmpty(module)) return;
            lock (_lock) { _inflight.TryGetValue(module, out int n); _inflight[module] = Math.Max(0, n - 1); }
        }

        /// <summary>진행 중 그랩이 있으면 true — 안전망은 false 일 때만 발화해야 한다.</summary>
        public static bool HasInFlight(string module)
        {
            if (string.IsNullOrEmpty(module)) return false;
            lock (_lock) return _inflight.TryGetValue(module, out int n) && n > 0;
        }

        /// <summary>사이클 취소/리셋 — 보관분을 폐기(이미지 Dispose)하고 상태 제거.</summary>
        public static void Clear(string module)
        {
            if (string.IsNullOrEmpty(module)) return;
            List<Item> drop = null;
            lock (_lock)
            {
                if (_byModule.TryGetValue(module, out var b)) { drop = new List<Item>(b.Items); _byModule.Remove(module); }
            }
            if (drop != null)
                foreach (var it in drop) { try { it.Image?.Dispose(); } catch { } }
        }
    }
}
