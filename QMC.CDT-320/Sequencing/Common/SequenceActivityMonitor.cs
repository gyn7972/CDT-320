using System;
using System.Collections.Generic;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// 자동/수동 시퀀스 4개 유닛(INPUT/FRONT/REAR/OUTPUT)의 현재 동작 상태를 보관하는 공식 상태 객체.
    /// <list type="bullet">
    ///   <item><description>시퀀스 스레드는 <see cref="Update"/>/<see cref="SetState"/> 로 상태만 기록한다.</description></item>
    ///   <item><description>UI 는 <see cref="Get"/>/<see cref="GetAll"/> 로 스냅샷만 읽는다. (UI 스레드 직접 접근 없음)</description></item>
    ///   <item><description>모든 접근은 lock 으로 보호한다. 로그 문자열 파싱 없이 상태로만 관리한다.</description></item>
    /// </list>
    /// </summary>
    public sealed class SequenceActivityMonitor
    {
        private static readonly SequenceUnitKind[] DisplayOrder =
        {
            SequenceUnitKind.InputLoader,
            SequenceUnitKind.PickerFront,
            SequenceUnitKind.PickerRear,
            SequenceUnitKind.OutputUnloader
        };

        private sealed class Entry
        {
            public SequenceActivityState State;
            public string ActionName = string.Empty;
            public string StepName = string.Empty;
            public string Detail = string.Empty;
            public DateTime StartedAt;
            public DateTime UpdatedAt;
        }

        private readonly object _gate = new object();
        private readonly Dictionary<SequenceUnitKind, Entry> _entries =
            new Dictionary<SequenceUnitKind, Entry>();

        /// <summary>상태가 바뀔 때 발생(선택적). UI 는 보통 저주기 Timer 로 스냅샷을 폴링한다.</summary>
        public event Action Changed;

        public SequenceActivityMonitor()
        {
            DateTime now = DateTime.Now;
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                _entries[DisplayOrder[i]] = new Entry
                {
                    State = SequenceActivityState.Idle,
                    StartedAt = now,
                    UpdatedAt = now
                };
            }
        }

        /// <summary>UI 표시 순서대로의 유닛 목록(INPUT/FRONT/REAR/OUTPUT).</summary>
        public IReadOnlyList<SequenceUnitKind> Units
        {
            get { return DisplayOrder; }
        }

        /// <summary>모든 유닛을 Idle 로 초기화한다. (새 시퀀스 시작/Reset 시)</summary>
        public void Reset()
        {
            lock (_gate)
            {
                DateTime now = DateTime.Now;
                for (int i = 0; i < DisplayOrder.Length; i++)
                {
                    Entry e = GetOrCreate(DisplayOrder[i]);
                    e.State = SequenceActivityState.Idle;
                    e.ActionName = string.Empty;
                    e.StepName = string.Empty;
                    e.Detail = string.Empty;
                    e.StartedAt = now;
                    e.UpdatedAt = now;
                }
            }

            RaiseChanged();
        }

        /// <summary>유닛 상태와 작업/Step/상세를 갱신한다.</summary>
        public void Update(SequenceUnitKind unit, SequenceActivityState state,
            string actionName, string stepName, string detail)
        {
            lock (_gate)
            {
                Entry e = GetOrCreate(unit);
                DateTime now = DateTime.Now;

                // Running/Waiting 으로 새로 진입하면 경과시간 기준 시각을 재설정한다.
                bool wasActive = e.State == SequenceActivityState.Running || e.State == SequenceActivityState.Waiting;
                bool nowActive = state == SequenceActivityState.Running || state == SequenceActivityState.Waiting;
                if (nowActive && !wasActive)
                    e.StartedAt = now;

                e.State = state;
                if (actionName != null) e.ActionName = actionName;
                if (stepName != null) e.StepName = stepName;
                if (detail != null) e.Detail = detail;
                e.UpdatedAt = now;
            }

            RaiseChanged();
        }

        /// <summary>상태만 갱신한다. (작업/Step/상세는 유지)</summary>
        public void SetState(SequenceUnitKind unit, SequenceActivityState state, string detail = null)
        {
            Update(unit, state, null, null, detail);
        }

        /// <summary>
        /// 현재 진행/대기(Running/Waiting) 중인 유닛만 지정한 종료 상태로 정리한다.
        /// (전체 정지/취소/알람 시 남은 유닛 정리용)
        /// </summary>
        public void SweepActiveTo(SequenceActivityState state, string detail = null)
        {
            bool changed = false;
            lock (_gate)
            {
                DateTime now = DateTime.Now;
                for (int i = 0; i < DisplayOrder.Length; i++)
                {
                    Entry e = GetOrCreate(DisplayOrder[i]);
                    if (e.State != SequenceActivityState.Running && e.State != SequenceActivityState.Waiting)
                        continue;

                    e.State = state;
                    if (detail != null) e.Detail = detail;
                    e.UpdatedAt = now;
                    changed = true;
                }
            }

            if (changed)
                RaiseChanged();
        }

        /// <summary>유닛 1개의 현재 스냅샷을 만든다. (경과시간은 호출 시점 기준으로 계산)</summary>
        public SequenceActivitySnapshot Get(SequenceUnitKind unit)
        {
            lock (_gate)
            {
                Entry e = GetOrCreate(unit);
                return BuildSnapshot(unit, e);
            }
        }

        /// <summary>표시 순서대로 4개 유닛의 스냅샷을 만든다.</summary>
        public IReadOnlyList<SequenceActivitySnapshot> GetAll()
        {
            var list = new List<SequenceActivitySnapshot>(DisplayOrder.Length);
            lock (_gate)
            {
                for (int i = 0; i < DisplayOrder.Length; i++)
                {
                    SequenceUnitKind unit = DisplayOrder[i];
                    list.Add(BuildSnapshot(unit, GetOrCreate(unit)));
                }
            }
            return list;
        }

        private SequenceActivitySnapshot BuildSnapshot(SequenceUnitKind unit, Entry e)
        {
            bool active = e.State == SequenceActivityState.Running || e.State == SequenceActivityState.Waiting;
            TimeSpan elapsed = active
                ? DateTime.Now - e.StartedAt
                : e.UpdatedAt - e.StartedAt;

            return new SequenceActivitySnapshot(
                unit,
                ResolveDisplayName(unit),
                e.State,
                e.ActionName,
                e.StepName,
                e.Detail,
                e.StartedAt,
                e.UpdatedAt,
                elapsed);
        }

        private Entry GetOrCreate(SequenceUnitKind unit)
        {
            Entry e;
            if (!_entries.TryGetValue(unit, out e))
            {
                DateTime now = DateTime.Now;
                e = new Entry { State = SequenceActivityState.Idle, StartedAt = now, UpdatedAt = now };
                _entries[unit] = e;
            }
            return e;
        }

        /// <summary>유닛 종류 → 화면 표시 이름.</summary>
        public static string ResolveDisplayName(SequenceUnitKind unit)
        {
            switch (unit)
            {
                case SequenceUnitKind.InputLoader: return "INPUT";
                case SequenceUnitKind.PickerFront: return "FRONT";
                case SequenceUnitKind.PickerRear: return "REAR";
                case SequenceUnitKind.OutputUnloader: return "OUTPUT";
                default: return unit.ToString();
            }
        }

        private void RaiseChanged()
        {
            Action h = Changed;
            if (h == null)
                return;
            try { h(); } catch { }
        }
    }
}
