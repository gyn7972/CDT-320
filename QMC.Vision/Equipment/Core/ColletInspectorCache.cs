using System;
using System.Collections.Generic;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 콜렛별 검사기 런타임 인스턴스 캐시 — (모듈, 검사기, 전역 픽커 1~8[, 채널]) 키로 인스턴스를 영속 보관한다.
    /// <para>확정 정책(2026-07-04): 파라미터/티칭은 레시피 1벌 공유, 처리 속도를 위해 '실행 인스턴스'만
    /// 콜렛(8개: Front 1~4 + Back 5~8) 기준으로 분리한다. 매 배치마다 새로 만들던 인스턴스를 재사용해
    /// 생성 비용을 제거하고, 사용 직전 <see cref="VisionCommandCore.CopyInspectorConfig"/> 로 레시피
    /// 파라미터를 반사 복제해 최신 설정을 반영한다(레시피 변경 즉시 추종).</para>
    /// <para>Side 는 같은 콜렛의 0°/90° 채널이 병렬 처리되므로 채널까지 키에 포함한다(동시 사용 충돌 방지).
    /// CDT-310 알고리즘 코어는 건드리지 않는다 — 인스턴스 수명 관리만 담당.</para>
    /// </summary>
    public static class ColletInspectorCache
    {
        private static readonly Dictionary<string, IInspector> _cache =
            new Dictionary<string, IInspector>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new object();

        private static string Key(string module, string insp, int picker, int channel)
            => (module ?? "") + "/" + (insp ?? "") + "#" + picker + "#" + channel;

        /// <summary>
        /// 콜렛 전용 인스턴스를 가져온다(없으면 <see cref="DomainInspectorFactory"/> 로 1회 생성 후 보관).
        /// picker=전역 픽커(1~8), channel=Side 0/1(그 외 -1). 생성 실패 시 false.
        /// </summary>
        public static bool TryGet(string module, string insp, int picker, int channel, out IInspector inspector)
        {
            inspector = null;
            try
            {
                if (string.IsNullOrEmpty(module) || string.IsNullOrEmpty(insp)) return false;
                string key = Key(module, insp, picker, channel);
                lock (_lock)
                {
                    if (_cache.TryGetValue(key, out inspector) && inspector != null)
                        return true;
                    // 모듈 한정 id(모듈명/도구)로 도메인 검사기 선택 — 짧은 id 만으론 Side 의
                    // SurfaceInspector 가 Bottom 으로 오인될 수 있다(기존 배치 경로와 동일 규칙).
                    if (!DomainInspectorFactory.TryCreate(module + "/" + insp, out inspector) || inspector == null)
                    {
                        inspector = null;
                        return false;
                    }
                    _cache[key] = inspector;
                    return true;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "ColletInspectorCache",
                    module + "/" + insp + " 콜렛 인스턴스 생성 실패(picker=" + picker + ", ch=" + channel + "): " + ex.Message);
                inspector = null;
                return false;
            }
            finally
            {
            }
        }

        /// <summary>캐시 전체 폐기 — 레시피 구조 교체 등 인스턴스 재생성이 필요한 경우 호출.</summary>
        public static void Clear()
        {
            lock (_lock)
            {
                foreach (var kv in _cache)
                {
                    var d = kv.Value as IDisposable;
                    if (d != null) { try { d.Dispose(); } catch { } }
                }
                _cache.Clear();
            }
        }
    }
}
