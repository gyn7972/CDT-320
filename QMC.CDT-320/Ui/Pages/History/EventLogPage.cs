using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Logging;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.History
{
    public partial class EventLogPage : PageBase
    {
        // 알람 행 강조용 폰트. 행마다 new Font 를 만들지 않도록 1회만 생성해 재사용한다.
        private static readonly Font AlarmFont = new Font("Consolas", 10F, FontStyle.Bold);

        // 표시 상한(최신 N개). 최근 1시간 필터와 함께 로딩 부하를 제한한다.
        private const int MaxRows = 500;

        // 사이드바 버튼(경고/데이터/작업)이 지정하는 초기 Kind 프리셋. null 이면 전체(이벤트).
        private readonly EventKind? _presetKind;

        // 사용자가 직접 연 로그 파일 경로. null 이면 DATE 피커 날짜 기준으로 읽는다.
        private string _overridePath;

        public EventLogPage()
            : this(null)
        {
        }

        public EventLogPage(EventKind? presetKind)
        {
            _presetKind = presetKind;
            InitializeComponent();
            ApplyKindHeader();
            WireEvents();
        }

        // 프리셋 Kind 에 맞춰 헤더 라벨 i18n 키를 교체한다.
        private void ApplyKindHeader()
        {
            string key = KindToI18n(_presetKind);
            lblHeader.Tag = "i18n:" + key;
            lblHeader.Text = Lang.T(key);
        }

        private static string KindToI18n(EventKind? kind)
        {
            switch (kind)
            {
                case EventKind.Warning:      return "hist.warning";
                case EventKind.Data:         return "hist.data";
                case EventKind.Work:         return "hist.work";
                case EventKind.Alarm:        return "hist.alarm";
                case EventKind.InputSeq:     return "hist.inputSeq";
                case EventKind.OutputSeq:    return "hist.outputSeq";
                case EventKind.FrontHeadSeq: return "hist.frontHeadSeq";
                case EventKind.RearHeadSeq:  return "hist.rearHeadSeq";
                default:                     return "hist.event";
            }
        }

        private void WireEvents()
        {
            // 초기 날짜를 오늘로 지정한다. ValueChanged 구독 전에 설정해 중복 로드를 막는다.
            _dp.Value = DateTime.Today;
            // 날짜를 바꾸면 파일 열기 모드를 해제하고 날짜 기준으로 돌아간다.
            _dp.ValueChanged += (s, e) => { _overridePath = null; ReloadCurrent(); };
            btnRefresh.Click += (s, e) => ReloadCurrent();
            btnOpenFile.Click += (s, e) => OpenFile();
            EventLogger.EventLogged += OnLiveEvent;
            Disposed += (s, e) => EventLogger.EventLogged -= OnLiveEvent;
            Load += (s, e) => ReloadCurrent();
        }

        // 페이지에 지정된 고정 Kind 만 표시한다(프리셋이 없으면 전체 표시).
        // DATE 변경·OPEN FILE 모두 이 필터를 거치므로, 해당 kind 의 로그만 로드된다.
        private bool PassesKindFilter(EventKind kind)
        {
            return _presetKind == null || kind == _presetKind.Value;
        }

        // 현재 소스(직접 연 파일 또는 DATE 날짜)를 다시 읽어 그리드에 채운다.
        private void ReloadCurrent()
        {
            var source = _overridePath != null
                ? EventLogger.ReadFile(_overridePath)
                : EventLogger.Read(_dp.Value.Date);
            LoadRows(source);
        }

        private void LoadRows(List<EventRow> source)
        {
            // 로딩 부하를 줄이기 위해 (1) 최근 1시간 이내 + (2) 최신 MaxRows(500)개로 제한한다.
            DateTime cutoff = DateTime.Now.AddHours(-1);

            // CSV 는 과거→최신 순이므로 뒤(최신)부터 훑어 최신순으로 최대 500개만 만든다.
            // (필요한 만큼만 BuildRow 하므로 거대 파일에서도 행 생성 비용이 500개로 제한됨)
            var rows = new List<DataGridViewRow>();
            for (int i = source.Count - 1; i >= 0 && rows.Count < MaxRows; i--)
            {
                var r = source[i];
                if (r.When < cutoff) continue;          // 최근 1시간만
                if (!PassesKindFilter(r.Kind)) continue;
                rows.Add(BuildRow(r));                  // 뒤에서부터 → 이미 최신순
            }

            // 그리드 갱신은 레이아웃/오토사이즈를 멈춘 상태에서 AddRange 로 한 번에 처리한다.
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
        }

        // 로그 폴더에서 CSV 파일을 골라 그 내용을 그리드에 로드한다.
        private void OpenFile()
        {
            try
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Title = Lang.T("hist.event");
                    dlg.InitialDirectory = EventLogger.LogDir;
                    dlg.Filter = "Event Log (*.csv)|*.csv|All Files (*.*)|*.*";
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;

                    _overridePath = dlg.FileName;
                    ReloadCurrent();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "OPEN FILE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnLiveEvent(EventRow r)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<EventRow>(OnLiveEvent), r);
                return;
            }

            // 직접 연 파일을 보는 중이면 실시간 이벤트로 덮지 않는다.
            // 최신순 표시이므로 새 이벤트는 맨 위에 삽입한다.
            if (_overridePath == null && _dp != null && _dp.Value.Date == DateTime.Today && PassesKindFilter(r.Kind))
            {
                _grid.Rows.Insert(0, BuildRow(r));               // 최신이 맨 위
                while (_grid.Rows.Count > MaxRows)               // 상한 유지
                    _grid.Rows.RemoveAt(_grid.Rows.Count - 1);
            }
        }

        // EventRow 하나를 그리드에 넣을 DataGridViewRow 로 변환한다(셀 값 + Kind별 강조 스타일).
        private DataGridViewRow BuildRow(EventRow r)
        {
            // 메시지 카탈로그에 코드가 등록돼 있으면 그 문구(현재 언어)를 우선 표시하고, 없으면 기록된 설명을 그대로 쓴다.
            string lang = Lang.Current ?? "ko";
            string desc = MessageCatalog.Resolve(r.Kind, r.Code, lang, r.Description ?? "");

            var row = new DataGridViewRow();
            row.CreateCells(_grid,
                r.When.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                r.Kind.ToString(),
                r.User ?? "",
                r.Code ?? "",
                r.Source ?? "",
                desc);

            // Kind 별 글씨 색상 — 페이지마다 한 종류만 표시되므로 서로 뚜렷이 구분되는 색을 쓴다(흰 배경에서 가독성 확보).
            switch (r.Kind)
            {
                case EventKind.Event:        // 슬레이트 그레이
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(52, 73, 94);
                    break;
                case EventKind.Warning:      // 오렌지
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(211, 84, 0);
                    break;
                case EventKind.Alarm:        // 레드 + 굵게 강조
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(192, 57, 43);
                    row.DefaultCellStyle.Font = AlarmFont;
                    break;
                case EventKind.Data:         // 블루
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(41, 128, 185);
                    break;
                case EventKind.Work:         // 브라운
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(121, 85, 72);
                    break;
                case EventKind.InputSeq:     // 그린
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(39, 174, 96);
                    break;
                case EventKind.FrontHeadSeq: // 퍼플
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(142, 68, 173);
                    break;
                case EventKind.RearHeadSeq:  // 시안/틸
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 131, 143);
                    break;
                case EventKind.OutputSeq:    // 마젠타
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(194, 24, 91);
                    break;
            }

            return row;
        }
    }
}

