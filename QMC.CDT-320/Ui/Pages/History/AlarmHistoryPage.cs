using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Pages.History
{
    public partial class AlarmHistoryPage : PageBase
    {
        public AlarmHistoryPage()
        {
            InitializeComponent();
            WireEvents();

            if (!IsDesignerMode())
            {
                // 2초 폴링(전체 재생성) 대신 알람 발생 이벤트로 증분 갱신한다 → 선택 유지, 부하·깜빡임 제거.
                AlarmManager.AlarmRaised += OnRaise;
                LoadGrid();
            }
        }

        private void WireEvents()
        {
            _cbSeverity.Items.Add("(All)");
            foreach (var s in Enum.GetNames(typeof(AlarmSeverity))) _cbSeverity.Items.Add(s);
            _cbSeverity.SelectedIndex = 0;
            _cbSeverity.SelectedIndexChanged += (s, e) => LoadGrid();
            _tbFilter.TextChanged += (s, e) => LoadGrid();
            btnClear.Click += (s, e) => { AlarmManager.ClearAll(); LoadGrid(); };
        }

        // 전체 재생성 — 초기 로드 / 필터 변경 / Clear 같은 사용자 액션에서만 호출한다.
        private void LoadGrid()
        {
            try
            {
                var rows = new List<DataGridViewRow>();
                foreach (var a in AlarmManager.History.Reverse().Take(500)) // 최신순
                {
                    if (!PassesFilter(a)) continue;
                    rows.Add(BuildRow(a));
                }

                var prevAutoSize = _grid.AutoSizeColumnsMode;
                _grid.SuspendLayout();
                try
                {
                    _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                    _grid.Rows.Clear();
                    if (rows.Count > 0) _grid.Rows.AddRange(rows.ToArray());
                }
                finally
                {
                    _grid.AutoSizeColumnsMode = prevAutoSize;
                    _grid.ResumeLayout();
                }

                UpdateCount();
            }
            catch
            {
            }
        }

        // 현재 Severity / 검색어 필터를 통과하는지.
        private bool PassesFilter(AlarmRecord a)
        {
            if (a == null) return false;
            string sev = _cbSeverity?.SelectedItem?.ToString() ?? "(All)";
            if (sev != "(All)" && a.Severity.ToString() != sev) return false;

            string filter = (_tbFilter?.Text ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(filter)
                && (a.Code ?? "").ToLowerInvariant().IndexOf(filter) < 0
                && (a.Source ?? "").ToLowerInvariant().IndexOf(filter) < 0
                && (a.Message ?? "").ToLowerInvariant().IndexOf(filter) < 0)
            {
                return false;
            }
            return true;
        }

        // 알람 1건 → 셀 값 + 심각도 배경색을 가진 행.
        private DataGridViewRow BuildRow(AlarmRecord a)
        {
            var def = AlarmMaster.Get(a.Code);
            string lang = Localization.Lang.Current ?? "ko";
            // 메시지 카탈로그에 코드가 있으면 그 문구를 우선, 없으면 알람 메시지를 그대로.
            string message = MessageCatalog.Resolve(EventKind.Alarm, a.Code, lang, a.Message ?? "");

            var row = new DataGridViewRow();
            row.CreateCells(_grid,
                a.Raised.ToString("HH:mm:ss.fff"),
                a.Severity,
                a.Code,
                a.Source ?? "",
                message,
                def?.GetCause(lang) ?? "",
                def?.GetAction(lang) ?? "");
            row.Tag = a.Id; // 행 ↔ 알람 식별 (후속 상태 표시용)
            return row;
        }

        private void UpdateCount()
        {
            if (_lblCount != null) _lblCount.Text = "(" + _grid.Rows.Count + ")";
        }

        // 새 알람은 전체 재생성 없이 맨 위에 1행만 끼워넣는다 → 사용자의 선택이 유지된다.
        private void OnRaise(AlarmRecord r)
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<AlarmRecord>(OnRaise), r); } catch { }
                return;
            }

            try
            {
                if (!PassesFilter(r)) return;
                _grid.Rows.Insert(0, BuildRow(r));                 // 최신이 맨 위
                while (_grid.Rows.Count > 500)                     // 상한 유지
                    _grid.Rows.RemoveAt(_grid.Rows.Count - 1);
                UpdateCount();
            }
            catch
            {
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { AlarmManager.AlarmRaised -= OnRaise; } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}

