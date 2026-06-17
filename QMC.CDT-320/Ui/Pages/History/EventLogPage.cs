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
        private const string AllKinds = "(All)";

        // 알람 행 강조용 폰트. 행마다 new Font 를 만들지 않도록 1회만 생성해 재사용한다.
        private static readonly Font AlarmFont = new Font("Consolas", 10F, FontStyle.Bold);

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
            _cbKind.Items.Add(AllKinds);
            foreach (var k in Enum.GetNames(typeof(EventKind))) _cbKind.Items.Add(k);
            _cbKind.SelectedItem = _presetKind?.ToString() ?? AllKinds;
            if (_cbKind.SelectedIndex < 0) _cbKind.SelectedIndex = 0;
            _cbKind.SelectedIndexChanged += (s, e) => ReloadCurrent();

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

        // 현재 Kind 콤보 선택값에 따른 표시 여부.
        private bool PassesKindFilter(EventKind kind)
        {
            string sel = _cbKind?.SelectedItem?.ToString() ?? AllKinds;
            return sel == AllKinds || kind.ToString() == sel;
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
            // 1) 먼저 표시할 행들을 메모리에서 모두 만들어 둔다(파싱·필터·스타일).
            var rows = new List<DataGridViewRow>();
            foreach (var r in source)
            {
                if (!PassesKindFilter(r.Kind)) continue;
                rows.Add(BuildRow(r));
            }

            // CSV 는 과거→최신 순으로 쌓이므로 뒤집어 최신순(내림차순)으로 표시한다.
            rows.Reverse();

            // 2) 그리드 갱신은 레이아웃/오토사이즈를 멈춘 상태에서 AddRange 로 한 번에 처리한다.
            //    (행마다 Rows.Add + Fill 재계산하던 것을 일괄 처리로 바꿔 로딩 시간을 줄임)
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
                _grid.Rows.Insert(0, BuildRow(r));
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

            switch (r.Kind)
            {
                // 알람 로그 강조 표시 (폰트 크기는 그리드 기본과 동일, 굵게만 적용)
                case EventKind.Alarm:
                    row.DefaultCellStyle.ForeColor = Color.IndianRed;
                    row.DefaultCellStyle.Font = AlarmFont;
                    break;
                // 경고 로그 표시
                case EventKind.Warning:
                    row.DefaultCellStyle.ForeColor = Color.DarkOrange;
                    break;
                // 데이터 로그 표시
                case EventKind.Data:
                    row.DefaultCellStyle.ForeColor = Color.SteelBlue;
                    break;
                // 작업 로그 표시
                case EventKind.Work:
                    row.DefaultCellStyle.ForeColor = Color.Teal;
                    break;
                // 시퀀스 로그 표시 (입력/출력/프론트헤드/리어헤드)
                case EventKind.InputSeq:
                    row.DefaultCellStyle.ForeColor = Color.MediumSeaGreen;
                    break;
                case EventKind.OutputSeq:
                    row.DefaultCellStyle.ForeColor = Color.MediumVioletRed;
                    break;
                case EventKind.FrontHeadSeq:
                    row.DefaultCellStyle.ForeColor = Color.RoyalBlue;
                    break;
                case EventKind.RearHeadSeq:
                    row.DefaultCellStyle.ForeColor = Color.DarkSlateBlue;
                    break;
            }

            return row;
        }
    }
}

