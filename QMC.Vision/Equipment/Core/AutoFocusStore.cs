using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 프로세스 전역 오토포커스 세션 보관소. (카메라 × 타깃) 별 <see cref="AutoFocusSession"/> 을 보관한다.
    /// <para>
    /// 핸들러가 TCP 로 보낸 <c>FOCUS_START</c>/<c>FOCUS_VAL</c> 명령을 라우터가 여기로 누적하고,
    /// 같은 프로세스의 Vision UI 가 BEST 표·그래프를 이 스토어에서 읽는다.
    /// (<see cref="ModuleResultStore"/>/<see cref="InspectionResultStore"/> 와 동일한 정적 스토어 패턴.)
    /// </para>
    /// </summary>
    public static class AutoFocusStore
    {
        private static readonly Dictionary<string, AutoFocusSession> _sessions =
            new Dictionary<string, AutoFocusSession>();
        private static readonly object _lock = new object();

        private static string Key(FocusCamera camera, FocusTarget target)
        {
            return camera + ":" + target;
        }

        /// <summary>세션 시작(=리셋). 없으면 생성, 있으면 샘플 초기화.</summary>
        public static AutoFocusSession Start(FocusCamera camera, FocusTarget target)
        {
            lock (_lock)
            {
                string k = Key(camera, target);
                AutoFocusSession s;
                if (!_sessions.TryGetValue(k, out s))
                {
                    s = new AutoFocusSession(camera, target);
                    _sessions[k] = s;
                }
                else
                {
                    s.Reset();
                }
                return s;
            }
        }

        /// <summary>세션 조회. 없으면 생성.</summary>
        public static AutoFocusSession GetOrCreate(FocusCamera camera, FocusTarget target)
        {
            lock (_lock)
            {
                string k = Key(camera, target);
                AutoFocusSession s;
                if (!_sessions.TryGetValue(k, out s))
                {
                    s = new AutoFocusSession(camera, target);
                    _sessions[k] = s;
                }
                return s;
            }
        }

        /// <summary>세션 조회. 없으면 null.</summary>
        public static AutoFocusSession Get(FocusCamera camera, FocusTarget target)
        {
            lock (_lock)
            {
                AutoFocusSession s;
                return _sessions.TryGetValue(Key(camera, target), out s) ? s : null;
            }
        }

        /// <summary>(motorZ, score) 1점 누적. <paramref name="isInitial"/> 면 최초값(점) 표시.</summary>
        public static void AddSample(FocusCamera camera, FocusTarget target, int pickupNo, double motorZ, double score, bool isInitial = false)
        {
            GetOrCreate(camera, target).AddSample(pickupNo, motorZ, score, isInitial);
        }

        /// <summary>현재 보관 중인 모든 세션.</summary>
        public static List<AutoFocusSession> All()
        {
            lock (_lock)
            {
                return _sessions.Values.ToList();
            }
        }

        // ── 명령 인자 파서 ─────────────────────────────────────────

        /// <summary>"BOTTOM"/"FRONT"/"BACK" → <see cref="FocusCamera"/>.</summary>
        public static bool TryParseCamera(string s, out FocusCamera camera)
        {
            camera = FocusCamera.Bottom;
            if (string.IsNullOrWhiteSpace(s)) return false;
            switch (s.Trim().ToUpperInvariant())
            {
                case "BOTTOM": camera = FocusCamera.Bottom; return true;
                case "FRONT":  camera = FocusCamera.Front;  return true;
                case "BACK":   camera = FocusCamera.Back;   return true;
                default:       return false;
            }
        }

        /// <summary>"COLLET"/"DIE"/"SIDE" → <see cref="FocusTarget"/>.</summary>
        public static bool TryParseTarget(string s, out FocusTarget target)
        {
            target = FocusTarget.Collet;
            if (string.IsNullOrWhiteSpace(s)) return false;
            switch (s.Trim().ToUpperInvariant())
            {
                case "COLLET": target = FocusTarget.Collet; return true;
                case "DIE":    target = FocusTarget.Die;    return true;
                case "SIDE":   target = FocusTarget.Side;   return true;
                default:       return false;
            }
        }
    }
}
