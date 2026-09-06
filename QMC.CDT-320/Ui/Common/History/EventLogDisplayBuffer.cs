using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Common.History
{
    // 화면 전용 캐시. 설정, 파일, 장비, UI 컨트롤에는 접근하지 않는다.
    internal sealed class EventLogDisplayBuffer
    {
        private readonly List<EventRow> _rows = new List<EventRow>();
        private readonly HashSet<EventRow> _knownRows =
            new HashSet<EventRow>(new EventRowReferenceComparer());
        private readonly HashSet<EventRow> _snapshotRows =
            new HashSet<EventRow>(new EventRowReferenceComparer());

        public IReadOnlyList<EventRow> Rows => _rows;

        public bool ContainsRow(EventRow row) => row != null && _knownRows.Contains(row);

        public void Clear()
        {
            _rows.Clear();
            _knownRows.Clear();
            _snapshotRows.Clear();
        }

        public void ReplaceRows(IEnumerable<EventRow> rows, int limit)
        {
            Clear();
            var snapshot = rows == null ? new List<EventRow>() : new List<EventRow>(rows);
            AppendRows(snapshot, limit);
            // 표시 상한으로 잘린 조회 행도 기억한다. 그 행의 늦은 알림이 최신 행을 밀어내면 안 된다.
            foreach (EventRow row in snapshot)
            {
                if (row != null)
                    _snapshotRows.Add(row);
            }
        }

        public List<EventRow> AppendRows(IEnumerable<EventRow> rows, int limit)
        {
            var added = new List<EventRow>();
            if (rows != null)
            {
                foreach (EventRow row in rows)
                {
                    // 메모리 조회와 라이브 알림에 함께 들어온 동일 객체만 제외한다.
                    // 시각과 문구가 같은 별도 로그는 정상 반복일 수 있으므로 유지한다.
                    if (row == null || _snapshotRows.Contains(row) || !_knownRows.Add(row))
                        continue;

                    _rows.Add(row);
                    added.Add(row);
                }
            }

            int removeCount = _rows.Count - Math.Max(1, limit);
            if (removeCount > 0)
            {
                for (int i = 0; i < removeCount; i++)
                    _knownRows.Remove(_rows[i]);
                _rows.RemoveRange(0, removeCount);
                added.RemoveAll(row => !_knownRows.Contains(row));
            }

            return added;
        }

        public bool RemoveOutsideRange(DateTime? date, bool recentHourOnly, DateTime now)
        {
            int removed = _rows.RemoveAll(row =>
            {
                if (MatchesTimeRange(row, date, recentHourOnly, now))
                    return false;

                _knownRows.Remove(row);
                return true;
            });
            return removed > 0;
        }

        public static bool MatchesTimeRange(
            EventRow row, DateTime? date, bool recentHourOnly, DateTime now)
        {
            if (row == null)
                return false;
            if (date.HasValue && row.When.Date != date.Value.Date)
                return false;
            return !recentHourOnly || row.When >= now.AddHours(-1);
        }

        private sealed class EventRowReferenceComparer : IEqualityComparer<EventRow>
        {
            public bool Equals(EventRow x, EventRow y) => ReferenceEquals(x, y);
            public int GetHashCode(EventRow row) => RuntimeHelpers.GetHashCode(row);
        }
    }
}
