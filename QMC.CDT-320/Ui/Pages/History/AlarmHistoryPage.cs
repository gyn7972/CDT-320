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
        private const int MaxRows = 500;
        private const int LiveFlushIntervalMs = 250;
        private const int MaxLiveFlushRows = 50;
        private const int MaxPendingLiveRows = 500;

        private readonly object _pendingAlarmRowsLock = new object();
        private readonly Queue<AlarmRecord> _pendingAlarmRows = new Queue<AlarmRecord>();
        private readonly Timer _liveFlushTimer = new Timer();
        private bool _alarmEventSubscribed;

        public AlarmHistoryPage()
        {
            InitializeComponent();
            WireEvents();

            if (!IsDesignerMode())
            {
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
            _liveFlushTimer.Interval = LiveFlushIntervalMs;
            _liveFlushTimer.Tick += (s, e) => FlushPendingAlarmRows();
        }

        // 전체 재생성 — 초기 로드 / 필터 변경 / Clear 같은 사용자 액션에서만 호출한다.
        private void LoadGrid()
        {
            try
            {
                var rows = new List<DataGridViewRow>();
                foreach (var a in AlarmManager.History.Reverse().Take(MaxRows)) // 최신순
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
            // 로딩 부하를 줄이기 위해 현재시간 기준 최근 1시간 이내만 표시.
            if (a.Raised < DateTime.Now.AddHours(-1)) return false;
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
            string message = ResolveAlarmMessage(a, def, lang);
            string cause = ResolveAlarmCause(a, def, lang, message);
            string action = ResolveAlarmAction(a, def, lang, message);

            var row = new DataGridViewRow();
            row.CreateCells(_grid,
                a.Raised.ToString("HH:mm:ss.fff"),
                a.Severity,
                a.Code,
                a.Source ?? "",
                message,
                cause,
                action);
            row.Tag = a.Id; // 행 ↔ 알람 식별 (후속 상태 표시용)
            return row;
        }

        private static string ResolveAlarmMessage(AlarmRecord alarm, AlarmDefinition definition, string lang)
        {
            try
            {
                string rawMessage = alarm != null ? alarm.Message ?? string.Empty : string.Empty;
                if (!string.IsNullOrWhiteSpace(rawMessage))
                    return rawMessage;

                string resolved = MessageCatalog.Resolve(EventKind.Alarm, alarm != null ? alarm.Code : string.Empty, lang, rawMessage);
                if (!string.IsNullOrWhiteSpace(resolved))
                    return resolved;

                return definition != null ? definition.GetTitle(lang) ?? string.Empty : string.Empty;
            }
            catch
            {
                return alarm != null ? alarm.Message ?? string.Empty : string.Empty;
            }
            finally
            {
            }
        }

        private static string ResolveAlarmCause(AlarmRecord alarm, AlarmDefinition definition, string lang, string message)
        {
            try
            {
                string code = alarm != null ? alarm.Code ?? string.Empty : string.Empty;
                string source = alarm != null ? alarm.Source ?? string.Empty : string.Empty;
                message = message ?? string.Empty;

                if (IsInterlockAlarm(code))
                {
                    if (HasDetailedMessage(message))
                        return "인터락 차단 사유: " + message;

                    return "인터락 조건이 만족되지 않아 " + source + " 동작이 차단되었습니다.";
                }

                if (IsReticleCylinderAlarm(code))
                {
                    if (HasDetailedMessage(message))
                        return "Reticle 실린더 동작 실패 사유: " + message;

                    return "Reticle 실린더가 목표 센서 상태에 도달하지 못했습니다.";
                }

                return definition != null ? definition.GetCause(lang) ?? string.Empty : string.Empty;
            }
            catch
            {
                return definition != null ? definition.GetCause(lang) ?? string.Empty : string.Empty;
            }
            finally
            {
            }
        }

        private static string ResolveAlarmAction(AlarmRecord alarm, AlarmDefinition definition, string lang, string message)
        {
            try
            {
                string code = alarm != null ? alarm.Code ?? string.Empty : string.Empty;
                string source = alarm != null ? alarm.Source ?? string.Empty : string.Empty;

                if (IsInterlockAlarm(code))
                    return "Cause의 차단 사유에 나온 축/실린더/센서 상태를 안전 위치로 복구한 뒤 알람 리셋 후 재시도하세요. 대상=" + source;

                if (IsReticleCylinderAlarm(code))
                    return "Reticle Lift/Slide 센서와 Picker/Camera 회피 위치를 확인하고, Cause의 인터락 사유를 먼저 해소한 뒤 다시 실행하세요.";

                return definition != null ? definition.GetAction(lang) ?? string.Empty : string.Empty;
            }
            catch
            {
                return definition != null ? definition.GetAction(lang) ?? string.Empty : string.Empty;
            }
            finally
            {
            }
        }

        private static bool IsInterlockAlarm(string code)
        {
            return string.Equals(code ?? string.Empty, "INTERLOCK", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(code ?? string.Empty, "INTERLOCK-GUARD", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReticleCylinderAlarm(string code)
        {
            return (code ?? string.Empty).StartsWith("VS-RETICLE-CYL", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasDetailedMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            string normalized = message.Trim();
            return !string.Equals(normalized, "인터록 차단", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(normalized, "Interlock blocked", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateCount()
        {
            if (_lblCount != null) _lblCount.Text = "(" + _grid.Rows.Count + ")";
        }

        // 새 알람은 전체 재생성 없이 맨 위에 1행만 끼워넣는다 → 사용자의 선택이 유지된다.
        private void OnRaise(AlarmRecord r)
        {
            if (r == null)
                return;

            lock (_pendingAlarmRowsLock)
            {
                _pendingAlarmRows.Enqueue(r);
                while (_pendingAlarmRows.Count > MaxPendingLiveRows)
                    _pendingAlarmRows.Dequeue();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateAlarmEventSubscription();
        }

        private void UpdateAlarmEventSubscription()
        {
            if (ShouldRefreshVisible(this))
            {
                SubscribeAlarmEvents();
                if (!_liveFlushTimer.Enabled)
                    _liveFlushTimer.Start();
            }
            else
            {
                UnsubscribeAlarmEvents();
                _liveFlushTimer.Stop();
                ClearPendingAlarmRows();
            }
        }

        private void SubscribeAlarmEvents()
        {
            if (_alarmEventSubscribed)
                return;

            AlarmManager.AlarmRaised += OnRaise;
            _alarmEventSubscribed = true;
        }

        private void UnsubscribeAlarmEvents()
        {
            if (!_alarmEventSubscribed)
                return;

            AlarmManager.AlarmRaised -= OnRaise;
            _alarmEventSubscribed = false;
        }

        private void FlushPendingAlarmRows()
        {
            if (!ShouldRefreshVisible(this))
                return;

            List<AlarmRecord> rows = DequeuePendingAlarmRows();
            if (rows.Count == 0)
                return;

            var prevAutoSize = _grid.AutoSizeColumnsMode;
            _grid.SuspendLayout();
            try
            {
                _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                foreach (AlarmRecord row in rows)
                {
                    if (!PassesFilter(row))
                        continue;

                    _grid.Rows.Insert(0, BuildRow(row));           // 최신이 맨 위
                    while (_grid.Rows.Count > MaxRows)             // 상한 유지
                        _grid.Rows.RemoveAt(_grid.Rows.Count - 1);
                }

                UpdateCount();
            }
            catch
            {
            }
            finally
            {
                _grid.AutoSizeColumnsMode = prevAutoSize;
                _grid.ResumeLayout();
            }
        }

        private List<AlarmRecord> DequeuePendingAlarmRows()
        {
            var rows = new List<AlarmRecord>();
            lock (_pendingAlarmRowsLock)
            {
                while (_pendingAlarmRows.Count > 0 && rows.Count < MaxLiveFlushRows)
                    rows.Add(_pendingAlarmRows.Dequeue());
            }

            return rows;
        }

        private void ClearPendingAlarmRows()
        {
            lock (_pendingAlarmRowsLock)
            {
                _pendingAlarmRows.Clear();
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                UnsubscribeAlarmEvents();
                _liveFlushTimer.Stop();
                _liveFlushTimer.Dispose();
            }
            catch { }
            base.OnHandleDestroyed(e);
        }
    }
}

