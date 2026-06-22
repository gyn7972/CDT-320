using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 로그가 있는 날짜만 선택할 수 있는 날짜 선택기.
    /// 읽기전용 텍스트 + 드롭다운 버튼을 누르면 <see cref="MonthCalendar"/> 팝업이 열린다.
    /// 로그가 있는 날짜는 굵게(BoldedDates) 표시하고, 로그가 없는 날짜를 클릭하면
    /// 선택을 무시(이전 값으로 복원)한다 — DateTimePicker 와 달리 개별 날짜 구분이 가능하다.
    /// 정적 UI 골격은 .Designer.cs, 동작 로직은 본 파일(AGENTS 디자이너 규칙).
    /// </summary>
    public partial class LogDatePicker : UserControl
    {
        // ── Fields ──
        private readonly HashSet<DateTime> _available = new HashSet<DateTime>();
        private DateTime _value = DateTime.Today;
        private bool _suppressCalendarEvent;   // 프로그램적 SelectionStart 변경 시 재진입 방지

        // ── Events ──
        /// <summary>선택 날짜가 실제로 바뀌면 발생(설정값이 동일하면 미발생).</summary>
        public event EventHandler ValueChanged;

        // ── Properties ──
        /// <summary>현재 선택 날짜(자정 기준). 값이 바뀌면 <see cref="ValueChanged"/> 발생.</summary>
        public DateTime Value
        {
            get { return _value; }
            set
            {
                DateTime d = value.Date;
                if (_value == d) return;
                _value = d;
                UpdateText();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // ── Constructor ──
        public LogDatePicker()
        {
            InitializeComponent();
            UpdateText();
        }

        // ── Public Methods ──
        /// <summary>로그가 있는 날짜 집합을 설정한다. 달력에 굵게 표시하고 이동 범위를 갱신한다.</summary>
        public void SetAvailableDates(IEnumerable<DateTime> dates)
        {
            try
            {
                _available.Clear();
                if (dates != null)
                    foreach (var d in dates) _available.Add(d.Date);
                ApplyCalendarRange();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[LogDatePicker] SetAvailableDates 실패: " + ex.Message);
            }
        }

        /// <summary>ValueChanged 를 발생시키지 않고 값만 설정(초기 기본값 지정용).</summary>
        public void SetValueSilent(DateTime value)
        {
            try
            {
                _value = value.Date;
                UpdateText();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[LogDatePicker] SetValueSilent 실패: " + ex.Message);
            }
        }

        /// <summary>해당 날짜에 로그가 있는지 확인한다.</summary>
        public bool IsAvailable(DateTime date) => _available.Contains(date.Date);

        /// <summary>로그가 있는 가장 최근 날짜(없으면 null).</summary>
        public DateTime? GetLatestAvailable()
            => _available.Count > 0 ? _available.Max() : (DateTime?)null;

        // ── Event Methods ──
        private void _btnDrop_Click(object sender, EventArgs e) => ShowCalendar();

        private void _txt_Click(object sender, EventArgs e) => ShowCalendar();

        private void _calendar_DateSelected(object sender, DateRangeEventArgs e)
        {
            if (_suppressCalendarEvent) return;
            try
            {
                DateTime d = e.Start.Date;
                if (IsAvailable(d))
                {
                    _dropDown.Close();
                    Value = d;   // ValueChanged 발생
                }
                else
                {
                    // 로그 없는 날짜 → 선택 무시(이전 값 복원, 팝업 유지)
                    RestoreSelection();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[LogDatePicker] DateSelected 실패: " + ex.Message);
            }
        }

        // ── Private Methods ──
        /// <summary>달력 팝업을 현재 값 위치로 맞춰 컨트롤 하단에 띄운다.</summary>
        private void ShowCalendar()
        {
            try
            {
                _suppressCalendarEvent = true;
                try
                {
                    DateTime sel = _value;
                    if (sel < _calendar.MinDate) sel = _calendar.MinDate;
                    else if (sel > _calendar.MaxDate) sel = _calendar.MaxDate;
                    _calendar.SelectionStart = sel;
                    _calendar.SetDate(sel);   // 해당 월로 이동
                }
                finally { _suppressCalendarEvent = false; }

                _dropDown.Show(this, new Point(0, Height));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[LogDatePicker] ShowCalendar 실패: " + ex.Message);
            }
        }

        /// <summary>달력 선택을 현재 값으로 되돌린다(범위 밖이면 최소일).</summary>
        private void RestoreSelection()
        {
            try
            {
                _suppressCalendarEvent = true;
                try
                {
                    DateTime sel = (_value >= _calendar.MinDate && _value <= _calendar.MaxDate)
                        ? _value : _calendar.MinDate;
                    _calendar.SelectionStart = sel;
                }
                finally { _suppressCalendarEvent = false; }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[LogDatePicker] RestoreSelection 실패: " + ex.Message);
            }
        }

        /// <summary>로그 날짜 집합으로 달력의 이동 범위와 굵은 표시를 갱신한다.</summary>
        private void ApplyCalendarRange()
        {
            try
            {
                DateTime today = DateTime.Today;
                DateTime min, max;
                if (_available.Count > 0)
                {
                    min = _available.Min();
                    max = _available.Max();
                    if (today > max) max = today;   // 오늘 월까지 탐색 허용
                }
                else
                {
                    min = max = today;
                }
                if (min > max) min = max;

                _calendar.MinDate = min;
                _calendar.MaxDate = max;

                _calendar.RemoveAllBoldedDates();
                _calendar.BoldedDates = _available.ToArray();
                _calendar.UpdateBoldedDates();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[LogDatePicker] ApplyCalendarRange 실패: " + ex.Message);
            }
        }

        // ── UI Update Methods ──
        private void UpdateText()
        {
            if (_txt != null) _txt.Text = _value.ToString("yyyy-MM-dd");
        }
    }
}
