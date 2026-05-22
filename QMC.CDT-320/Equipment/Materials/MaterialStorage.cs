using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace QMC.CDT320.Materials
{
    /// <summary>
    /// 머터리얼 스토리지 — 모든 Die / DieTapeFrame 을 ObjId 기반으로 보관.
    /// 310 의 <c>MaterialStorage</c> 와 동일 역할이지만 SoftBricks 의존성 없음.
    /// </summary>
    public static class MaterialStorage
    {
        private static readonly ConcurrentDictionary<string, Die>           _dies   = new ConcurrentDictionary<string, Die>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, DieTapeFrame>  _frames = new ConcurrentDictionary<string, DieTapeFrame>(StringComparer.Ordinal);

        public static IReadOnlyDictionary<string, Die>          Dies   => _dies;
        public static IReadOnlyDictionary<string, DieTapeFrame> Frames => _frames;

        // ── Die ──
        public static Die GetOrCreateDie(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            return _dies.GetOrAdd(uid, k => new Die { Uid = k });
        }

        public static Die GetDie(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            _dies.TryGetValue(uid, out var d);
            return d;
        }

        public static bool RemoveDie(string uid)
            => _dies.TryRemove(uid, out _);

        public static void AddDie(Die die)
        {
            if (die == null || string.IsNullOrEmpty(die.Uid)) return;
            _dies[die.Uid] = die;
        }

        // ── DieTapeFrame ──
        public static DieTapeFrame GetOrCreateFrame(string objId)
        {
            if (string.IsNullOrEmpty(objId)) return null;
            return _frames.GetOrAdd(objId, k => new DieTapeFrame { ObjId = k });
        }

        public static DieTapeFrame GetFrame(string objId)
        {
            if (string.IsNullOrEmpty(objId)) return null;
            _frames.TryGetValue(objId, out var f);
            return f;
        }

        public static void AddFrame(DieTapeFrame frame)
        {
            if (frame == null || string.IsNullOrEmpty(frame.ObjId)) return;
            _frames[frame.ObjId] = frame;
        }

        public static bool RemoveFrame(string objId)
            => _frames.TryRemove(objId, out _);

        public static object GetByObjId(string objId)
        {
            // 310 호환 — Die 또는 Frame 타입 어느 쪽이든 반환.
            if (string.IsNullOrEmpty(objId)) return null;
            if (_frames.TryGetValue(objId, out var f)) return f;
            if (_dies  .TryGetValue(objId, out var d)) return d;
            return null;
        }

        public static void Clear()
        {
            _dies.Clear();
            _frames.Clear();
        }
    }
}
