using System;
using System.Collections.Generic;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 비동기 MATCH(MATCHASYNC)/INSPECT(INSPECTASYNC) 작업의 상태/결과를 모듈+finder+chip_uid(번호) 단위로 보관.
    /// <para>핸들러 플로우: MATCHASYNC(그랩 전 1차 ACK=STARTED) → 백그라운드 그랩+알고리즘 → 완료 시 여기 Complete →
    /// 핸들러가 MATCHRESULT|finder|chip_uid 로 폴링하여 0(진행)/1;data(완료)/ERR 를 받는다.</para>
    /// 같은 (모듈,finder,chip_uid) 재시작 시 최신으로 덮어쓴다(이전 결과 무효화).
    /// 번호(chip_uid)를 키에 포함하므로 각 이미지(번호)별 첫 결과가 개별 보관되어, 해당 번호 요청에 그 번호 데이터를 반환한다.
    /// </summary>
    public static class AsyncMatchStore
    {
        public enum State { None, Running, Done, Error }

        private class Job { public State State; public string Payload; public DateTime Time; }

        private static readonly object _lock = new object();
        private static readonly Dictionary<string, Job> _map =
            new Dictionary<string, Job>(StringComparer.OrdinalIgnoreCase);

        private static string Key(string module, string finder, string chipUid)
            => (module ?? "") + "|" + (finder ?? "") + "|" + (chipUid ?? "");

        /// <summary>알고리즘 시작 표시(Running). 해당 (모듈,finder,chip_uid)의 기존 결과를 무효화한다.</summary>
        public static void Start(string module, string finder, string chipUid)
        {
            lock (_lock) _map[Key(module, finder, chipUid)] = new Job { State = State.Running, Payload = "", Time = DateTime.Now };
        }

        /// <summary>완료 — payload 예: "x=..;y=..;r=..;score=.." (MATCH) / "PASS;.." | "FAIL;.." (INSPECT).</summary>
        public static void Complete(string module, string finder, string chipUid, string payload)
        {
            lock (_lock) _map[Key(module, finder, chipUid)] = new Job { State = State.Done, Payload = payload ?? "", Time = DateTime.Now };
        }

        /// <summary>실패 — reason 보관(RESULT 가 ERR;reason 로 응답).</summary>
        public static void Fail(string module, string finder, string chipUid, string reason)
        {
            lock (_lock) _map[Key(module, finder, chipUid)] = new Job { State = State.Error, Payload = reason ?? "", Time = DateTime.Now };
        }

        /// <summary>현재 상태/결과 조회. 미시작이면 State.None.</summary>
        public static State TryGet(string module, string finder, string chipUid, out string payload)
        {
            payload = "";
            lock (_lock)
            {
                if (_map.TryGetValue(Key(module, finder, chipUid), out var j)) { payload = j.Payload; return j.State; }
                return State.None;
            }
        }

        public static void Clear(string module, string finder, string chipUid)
        {
            lock (_lock) _map.Remove(Key(module, finder, chipUid));
        }
    }
}
