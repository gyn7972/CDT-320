using System;

namespace QMC.CDT320.Diagnostics
{
    /// <summary>
    /// 운전 사이클 계측 훅 — 시퀀스 코드에는 이 클래스 호출 1줄만 삽입한다.
    /// 파일 로그는 각 시퀀스의 기존 WriteLog가 담당하므로 여기서는 메모리 계측(CycleTimeStore)만
    /// 수행하며, 실패해도 운전에 절대 영향을 주지 않도록 이중 try/catch로 무해화한다.
    /// </summary>
    public static class HandlerTactLog
    {
        /// <summary>사이클 시작 — head="FRONT"/"REAR", headIndex=피커 번호(1~4).</summary>
        public static void CycleStart(string unit, string requestId, string head, int headIndex, int dieIndex, string motionInfo)
        {
            try { CycleTimeStore.MarkStart(unit, requestId, head, headIndex, dieIndex, motionInfo); } catch { }
        }

        public static void MotionStart(string unit, string requestId, string axis)
        {
            try { CycleTimeStore.MarkMotionStart(unit, requestId, axis); } catch { }
        }

        public static void MotionEnd(string unit, string requestId, string axis)
        {
            try { CycleTimeStore.MarkMotionEnd(unit, requestId, axis); } catch { }
        }

        public static void CycleResult(string unit, string requestId)
        {
            try { CycleTimeStore.MarkResult(unit, requestId); } catch { }
        }

        public static void CycleError(string unit, string requestId)
        {
            try { CycleTimeStore.MarkError(unit, requestId); } catch { }
        }

        /// <summary>초크 포인트 — 시퀀스 실패/취소 시 해당 프리픽스(예: "PICKUP|F")의 활성 사이클 전부 ERR.</summary>
        public static void CycleErrorAll(string keyPrefix)
        {
            try { CycleTimeStore.MarkErrorAllActive(keyPrefix); } catch { }
        }
    }
}
