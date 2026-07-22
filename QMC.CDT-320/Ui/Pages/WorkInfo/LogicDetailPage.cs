using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.Common.Diagnostics.TactTime;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class LogicDetailPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private const int MaxGridRows = 5000;

        private readonly List<TactTimeRecord> _chartRecords = new List<TactTimeRecord>();
        private readonly List<TactTimeRecord> _historyRecords = new List<TactTimeRecord>();
        private System.Windows.Forms.Timer _refresh;
        private CancellationTokenSource _historyLoadCts;
        private TactTimeCsvIndexResult _historyIndex;
        private string _historyFilePath = "";
        private string _lastSignature = "";
        private DateTime _viewSince = DateTime.MinValue;
        private bool _historyMode;
        private bool _historyLoading;
        private bool _suppressRunSelection;
        private bool _synchronizingSelection;

        // 실시간 CycleTime 간트 (지연 생성 — CYCLE TIME 탭을 처음 열 때만 만든다).
        private QMC.CDT320.Ui.Controls.CycleTimeGanttControl _cycleGantt;

        public LogicDetailPage()
        {
            InitializeComponent();

            foreach (DataGridViewColumn column in _grid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.Resizable = DataGridViewTriState.True;
            }

            _timeChart.RecordSelected += timeChart_RecordSelected;
            tabs.SelectedIndexChanged += tabs_SelectedIndexChangedForCycle;

            if (!IsDesignerMode())
            {
                if (cmbCategory.Items.Count > 0)
                    cmbCategory.SelectedIndex = 0;
                if (cmbItemFilter.Items.Count > 0)
                    cmbItemFilter.SelectedIndex = 0;
                if (cmbChartMode.Items.Count > 0)
                    cmbChartMode.SelectedIndex = 0;

                _refresh = new System.Windows.Forms.Timer { Interval = 1000 };
                _refresh.Tick += refresh_Tick;
                VisibleChanged += LogicDetailPage_VisibleChanged;
                ApplyDataSourceState();
                RefreshAll(true);
            }
        }

        private void refresh_Tick(object sender, EventArgs e)
        {
            if (!ShouldRefreshVisible(this) || !chkAutoRefresh.Checked || _historyMode || _historyLoading)
                return;

            RefreshAll(false);
        }

        private void LogicDetailPage_VisibleChanged(object sender, EventArgs e)
        {
            UpdateRefreshTimer();
        }

        private void cmbCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshAll(true);
        }

        private void cmbItemFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshAll(true);
        }

        private void cmbChartMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            _timeChart.ViewMode = cmbChartMode.SelectedIndex == 1
                ? TactTimeChartViewMode.Timeline
                : TactTimeChartViewMode.Trend;
        }

        private void chkAutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRefreshTimer();
            RefreshAll(true);
        }

        private void btnResetChart_Click(object sender, EventArgs e)
        {
            _timeChart.ResetView();
        }

        private void btnClearView_Click(object sender, EventArgs e)
        {
            try
            {
                _lastSignature = "";
                _chartRecords.Clear();
                _grid.Rows.Clear();

                if (_historyMode)
                {
                    _historyRecords.Clear();
                    lblSummary.Text = "과거 기록 화면을 비웠습니다. 원본 CSV는 변경되지 않았습니다.";
                    lblStatus.Text = "Run을 다시 선택하면 과거 기록을 다시 불러옵니다.";
                }
                else
                {
                    _viewSince = DateTime.Now;
                    lblSummary.Text = "화면 기록을 초기화했습니다. 이후 발생한 택타임만 표시합니다.";
                    lblStatus.Text = "실시간 화면 기록을 초기화했습니다.";
                }

                _timeChart.SetRecords(_chartRecords);
            }
            catch (Exception ex)
            {
                LogUiFailure("ClearView", ex);
                lblStatus.Text = "택타임 화면 초기화에 실패했습니다: " + ex.Message;
            }
        }

        private async void btnOpenHistory_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Title = "이전 택타임 기록 불러오기";
                    dialog.Filter = "택타임 CSV (*.csv)|*.csv|모든 파일 (*.*)|*.*";
                    dialog.Multiselect = false;
                    dialog.CheckFileExists = true;
                    dialog.InitialDirectory = ResolveTactTimeDirectory();
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;

                    await LoadHistoryIndexAndLatestRunAsync(dialog.FileName).ConfigureAwait(true);
                }
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "과거 택타임 기록 불러오기를 취소했습니다.";
            }
            catch (Exception ex)
            {
                HandleHistoryFailure("HistoryOpen", "과거 택타임 기록을 불러오지 못했습니다.", ex);
            }
        }

        private void btnLiveView_Click(object sender, EventArgs e)
        {
            CancelHistoryLoad();
            _historyMode = false;
            _historyFilePath = "";
            _historyIndex = null;
            _historyRecords.Clear();
            _suppressRunSelection = true;
            try
            {
                cmbRun.Items.Clear();
                cmbRun.SelectedIndex = -1;
            }
            finally
            {
                _suppressRunSelection = false;
            }

            _lastSignature = "";
            ApplyDataSourceState();
            RefreshAll(true);
            UpdateRefreshTimer();
        }

        private void btnCancelHistory_Click(object sender, EventArgs e)
        {
            CancelHistoryLoad();
            lblStatus.Text = "과거 택타임 기록 불러오기 취소를 요청했습니다.";
        }

        private async void cmbRun_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressRunSelection || !_historyMode || _historyLoading)
                return;

            TactTimeRunInfo run = cmbRun.SelectedItem as TactTimeRunInfo;
            if (run == null || string.IsNullOrWhiteSpace(_historyFilePath))
                return;

            try
            {
                await LoadHistoryRunAsync(run).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "선택한 Run 불러오기를 취소했습니다.";
            }
            catch (Exception ex)
            {
                HandleHistoryFailure("HistoryRunLoad", "선택한 Run을 불러오지 못했습니다.", ex);
            }
        }

        private void grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_synchronizingSelection || _grid.SelectedRows.Count <= 0)
                return;

            TactTimeRecord record = _grid.SelectedRows[0].Tag as TactTimeRecord;
            if (record == null)
                return;

            _synchronizingSelection = true;
            try
            {
                _timeChart.SetSelectedRecord(record, true);
                UpdateSelectedRecordStatus(record);
            }
            finally
            {
                _synchronizingSelection = false;
            }
        }

        /// <summary>CYCLE TIME 탭 진입 시 간트 컨트롤을 지연 생성한다. 실패해도 다른 탭 동작에 영향 없음.</summary>
        private void tabs_SelectedIndexChangedForCycle(object sender, EventArgs e)
        {
            try
            {
                if (tabs.SelectedTab != tabCycle)
                    return;

                if (_cycleGantt == null)
                {
                    _cycleGantt = new QMC.CDT320.Ui.Controls.CycleTimeGanttControl { Dock = DockStyle.Fill };
                    tabCycle.Controls.Add(_cycleGantt);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LogicDetailPage] CycleTime 간트 생성 실패: " + ex.Message);
            }
        }

        private void timeChart_RecordSelected(object sender, TactTimeRecordSelectedEventArgs e)
        {
            if (_synchronizingSelection || e == null || e.Record == null)
                return;

            _synchronizingSelection = true;
            try
            {
                bool selected = false;
                for (int i = 0; i < _grid.Rows.Count; i++)
                {
                    TactTimeRecord rowRecord = _grid.Rows[i].Tag as TactTimeRecord;
                    if (!IsSameRecord(rowRecord, e.Record))
                        continue;

                    _grid.ClearSelection();
                    _grid.Rows[i].Selected = true;
                    if (_grid.Rows[i].Cells.Count > 0)
                        _grid.CurrentCell = _grid.Rows[i].Cells[0];
                    selected = true;
                    break;
                }

                UpdateSelectedRecordStatus(e.Record);
                if (!selected && _grid.Rows.Count >= MaxGridRows)
                    lblStatus.Text += " / 해당 기록은 Grid 표시 한도 밖에 있습니다.";
            }
            finally
            {
                _synchronizingSelection = false;
            }
        }

        private async Task LoadHistoryIndexAndLatestRunAsync(string filePath)
        {
            CancellationTokenSource operation = BeginHistoryLoad();
            try
            {
                _historyFilePath = filePath;
                lblStatus.Text = "과거 택타임 파일의 Run 목록을 확인하고 있습니다.";
                IProgress<TactTimeCsvReadProgress> progress = CreateHistoryProgress();
                TactTimeCsvIndexResult index = await TactTimeCsvReader.IndexRunsAsync(
                    filePath,
                    operation.Token,
                    progress).ConfigureAwait(true);

                operation.Token.ThrowIfCancellationRequested();
                if (index.Runs == null || index.Runs.Count == 0)
                    throw new InvalidDataException("선택한 파일에 표시 가능한 Run이 없습니다.");

                _historyIndex = index;
                _historyMode = true;
                _viewSince = DateTime.MinValue;
                _suppressRunSelection = true;
                try
                {
                    cmbRun.Items.Clear();
                    for (int i = 0; i < index.Runs.Count; i++)
                        cmbRun.Items.Add(index.Runs[i]);
                    cmbRun.SelectedIndex = 0;
                }
                finally
                {
                    _suppressRunSelection = false;
                }

                ApplyDataSourceState();
                TactTimeRunInfo latestRun = cmbRun.SelectedItem as TactTimeRunInfo;
                if (latestRun != null)
                    await LoadHistoryRunCoreAsync(latestRun, operation, progress).ConfigureAwait(true);
            }
            finally
            {
                CompleteHistoryLoad(operation);
            }
        }

        private async Task LoadHistoryRunAsync(TactTimeRunInfo run)
        {
            CancellationTokenSource operation = BeginHistoryLoad();
            try
            {
                IProgress<TactTimeCsvReadProgress> progress = CreateHistoryProgress();
                await LoadHistoryRunCoreAsync(run, operation, progress).ConfigureAwait(true);
            }
            finally
            {
                CompleteHistoryLoad(operation);
            }
        }

        private async Task LoadHistoryRunCoreAsync(
            TactTimeRunInfo run,
            CancellationTokenSource operation,
            IProgress<TactTimeCsvReadProgress> progress)
        {
            if (run == null)
                throw new ArgumentNullException("run");

            lblStatus.Text = "선택한 Run의 택타임 기록을 불러오고 있습니다. run=" + run.RunId;
            TactTimeCsvLoadResult load = await TactTimeCsvReader.LoadRunAsync(
                _historyFilePath,
                run.RunId,
                operation.Token,
                progress).ConfigureAwait(true);
            operation.Token.ThrowIfCancellationRequested();

            _historyRecords.Clear();
            _historyRecords.AddRange(load.Records);
            _lastSignature = "";
            UpdateHistoryFileInfo(run, load);
            RefreshAll(true);

            string warning = load.SkippedRecordCount > 0 || load.IncompleteLastRecord || load.Warnings.Count > 0
                ? " / 제외=" + load.SkippedRecordCount + "건" +
                  (load.IncompleteLastRecord ? ", 마지막 미완성 행 제외" : "") +
                  (load.Warnings.Count > 0 ? ", 경고=" + load.Warnings.Count + "건" : "")
                : "";
            lblStatus.Text = "과거 Run을 불러왔습니다. records=" + _historyRecords.Count.ToString("N0") + warning;
        }

        private CancellationTokenSource BeginHistoryLoad()
        {
            CancelHistoryLoad();
            var operation = new CancellationTokenSource();
            _historyLoadCts = operation;
            SetHistoryLoading(true);
            return operation;
        }

        private void CompleteHistoryLoad(CancellationTokenSource operation)
        {
            bool isCurrent = object.ReferenceEquals(_historyLoadCts, operation);
            if (isCurrent)
                _historyLoadCts = null;
            operation.Dispose();
            if (isCurrent && !IsDisposed)
                SetHistoryLoading(false);
        }

        private void CancelHistoryLoad()
        {
            CancellationTokenSource current = _historyLoadCts;
            if (current == null)
                return;

            try
            {
                current.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // 이미 완료된 로딩 작업은 추가 정리가 필요하지 않다.
            }
        }

        private IProgress<TactTimeCsvReadProgress> CreateHistoryProgress()
        {
            return new Progress<TactTimeCsvReadProgress>(value =>
            {
                if (value == null || IsDisposed)
                    return;

                progressHistory.Value = Math.Max(progressHistory.Minimum, Math.Min(progressHistory.Maximum, value.Percent));
                lblFileInfo.Text = value.Phase + " " + value.Percent + "% / " + value.RecordCount.ToString("N0") + "건";
            });
        }

        private void SetHistoryLoading(bool loading)
        {
            _historyLoading = loading;
            btnOpenHistory.Enabled = !loading;
            btnLiveView.Enabled = !loading;
            btnCancelHistory.Enabled = loading;
            cmbRun.Enabled = !loading && _historyMode && cmbRun.Items.Count > 0;
            progressHistory.Value = 0;
            UseWaitCursor = loading;
            UpdateRefreshTimer();
        }

        private void ApplyDataSourceState()
        {
            if (_historyMode)
            {
                lblDataSource.Text = "HISTORY";
                lblDataSource.BackColor = Color.FromArgb(0x75, 0x57, 0xA8);
                chkAutoRefresh.Enabled = false;
                cmbRun.Enabled = !_historyLoading && cmbRun.Items.Count > 0;
                if (!string.IsNullOrWhiteSpace(_historyFilePath) && _historyIndex != null)
                {
                    var file = new FileInfo(_historyFilePath);
                    lblFileInfo.Text = file.Name + " / " + FormatFileSize(file.Length) +
                                       " / Run " + _historyIndex.Runs.Count.ToString("N0") + "개";
                }
            }
            else
            {
                lblDataSource.Text = "LIVE";
                lblDataSource.BackColor = Color.FromArgb(0x2F, 0x80, 0xC9);
                chkAutoRefresh.Enabled = true;
                cmbRun.Enabled = false;
                lblFileInfo.Text = "실시간 메모리 기록 (최대 5,000건)";
            }
        }

        private void UpdateHistoryFileInfo(TactTimeRunInfo run, TactTimeCsvLoadResult load)
        {
            var file = new FileInfo(_historyFilePath);
            int runIndex = cmbRun.SelectedIndex >= 0 ? cmbRun.SelectedIndex + 1 : 0;
            int runCount = _historyIndex != null && _historyIndex.Runs != null ? _historyIndex.Runs.Count : 0;
            lblFileInfo.Text = file.Name + " / " + FormatFileSize(file.Length) +
                               " / Run " + runIndex + "/" + runCount +
                               " / " + load.Records.Count.ToString("N0") + "건";
        }

        private void RefreshAll(bool force)
        {
            try
            {
                List<TactTimeRecord> records = LoadFilteredRecords();
                string signature = BuildSignature(records);
                if (!force && string.Equals(signature, _lastSignature, StringComparison.Ordinal))
                    return;

                _lastSignature = signature;
                UpdateGrid(records);
                UpdateChartRecords(records, !force);
                UpdateSummary(records);
            }
            catch (Exception ex)
            {
                LogUiFailure("Refresh", ex);
                lblStatus.Text = "택타임 화면 갱신에 실패했습니다: " + ex.Message;
            }
        }

        private List<TactTimeRecord> LoadFilteredRecords()
        {
            IReadOnlyList<TactTimeRecord> snapshot = ResolveSourceSnapshot();
            string category = cmbCategory.SelectedItem != null ? cmbCategory.SelectedItem.ToString() : "ALL";
            string itemFilter = cmbItemFilter.SelectedItem != null ? cmbItemFilter.SelectedItem.ToString() : "ALL";
            if (snapshot == null)
                return new List<TactTimeRecord>();

            if (string.Equals(category, "SEQUENCE", StringComparison.OrdinalIgnoreCase))
                return ApplyItemFilter(BuildSequenceSummaryRecords(snapshot), itemFilter);

            var result = new List<TactTimeRecord>();
            for (int i = 0; i < snapshot.Count; i++)
            {
                TactTimeRecord record = snapshot[i];
                if (record == null)
                    continue;
                if (!_historyMode && _viewSince != DateTime.MinValue && record.StartedAt < _viewSince)
                    continue;
                if (!string.Equals(category, "ALL", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(record.Category.ToString(), category, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!MatchesItemFilter(record, itemFilter))
                    continue;
                result.Add(record);
            }

            result.Sort((a, b) => a.StartedAt.CompareTo(b.StartedAt));
            return result;
        }

        private IReadOnlyList<TactTimeRecord> ResolveSourceSnapshot()
        {
            if (_historyMode)
                return _historyRecords.AsReadOnly();

            Form1 host = (FindForm() ?? ParentForm) as Form1;
            MachineController controller = host != null ? host.Controller : null;
            return controller != null ? controller.GetTactTimeSnapshot() : null;
        }

        private List<TactTimeRecord> BuildSequenceSummaryRecords(IReadOnlyList<TactTimeRecord> snapshot)
        {
            var result = new List<TactTimeRecord>();
            var placeRecords = new List<TactTimeRecord>();
            var outputReceiveRecords = new List<TactTimeRecord>();
            var inspectionDetailRecords = new List<TactTimeRecord>();
            if (snapshot == null)
                return result;

            for (int i = 0; i < snapshot.Count; i++)
            {
                TactTimeRecord record = snapshot[i];
                if (record == null)
                    continue;
                if (!_historyMode && _viewSince != DateTime.MinValue && record.StartedAt < _viewSince)
                    continue;

                if (IsMajorUnitRecord(record))
                {
                    TactTimeRecord clone = record.Clone();
                    clone.Detail = "유닛 전체 동작 시간. " + Safe(record.Detail);
                    result.Add(clone);
                }

                if (IsPlaceProcessRecord(record))
                    placeRecords.Add(record);

                if (IsOutputReceiveTactRecord(record))
                {
                    TactTimeRecord clone = record.Clone();
                    clone.Detail = "OutputStage 제품 1개 수령 간격. " + Safe(record.Detail);
                    outputReceiveRecords.Add(clone);
                }

                if (IsInspectionDetailTactRecord(record))
                {
                    TactTimeRecord clone = record.Clone();
                    clone.Detail = "검사 세부 시간. " + Safe(record.Detail);
                    inspectionDetailRecords.Add(clone);
                }
            }

            outputReceiveRecords.Sort((a, b) => a.EndedAt.CompareTo(b.EndedAt));
            if (outputReceiveRecords.Count > 0)
            {
                AddOutputReceiveAverageRecords(result, outputReceiveRecords);
                result.AddRange(outputReceiveRecords);
            }

            inspectionDetailRecords.Sort((a, b) => a.StartedAt.CompareTo(b.StartedAt));
            if (inspectionDetailRecords.Count > 0)
            {
                AddInspectionDetailAverageRecords(result, inspectionDetailRecords);
                result.AddRange(inspectionDetailRecords);
            }

            if (outputReceiveRecords.Count == 0 && inspectionDetailRecords.Count == 0)
                AddPlaceToPlaceRecords(result, placeRecords);

            result.Sort((a, b) => a.StartedAt.CompareTo(b.StartedAt));
            return result;
        }

        private static void AddPlaceToPlaceRecords(List<TactTimeRecord> result, List<TactTimeRecord> placeRecords)
        {
            placeRecords.Sort((a, b) => a.EndedAt.CompareTo(b.EndedAt));
            for (int i = 1; i < placeRecords.Count; i++)
            {
                TactTimeRecord previous = placeRecords[i - 1];
                TactTimeRecord current = placeRecords[i];
                if (previous.EndedAt == DateTime.MinValue || current.EndedAt == DateTime.MinValue)
                    continue;

                long elapsed = Math.Max(0, (long)(current.EndedAt - previous.EndedAt).TotalMilliseconds);
                result.Add(new TactTimeRecord
                {
                    RunId = current.RunId,
                    ParentId = current.ParentId,
                    CorrelationId = "TOTAL-PLACE-TO-PLACE-" + current.CorrelationId,
                    EquipmentId = current.EquipmentId,
                    ProjectName = current.ProjectName,
                    LotId = current.LotId,
                    Mode = current.Mode,
                    UnitName = "Total",
                    SequenceName = "SequenceSummary",
                    ProcessName = "Total TactTime",
                    StepName = "Place 완료 간격",
                    Category = TactTimeCategory.Process,
                    StartedAt = previous.EndedAt,
                    EndedAt = current.EndedAt,
                    ElapsedMs = elapsed,
                    Result = current.Result,
                    AlarmCode = current.AlarmCode,
                    Detail = "Output 수령 이벤트가 없어 Place 완료 시각 간격으로 계산한 임시 택타임입니다. previous=" +
                             Safe(previous.UnitName) + "/" + Safe(previous.ProcessName) +
                             ", current=" + Safe(current.UnitName) + "/" + Safe(current.ProcessName)
                });
            }
        }

        private static void AddOutputReceiveAverageRecords(List<TactTimeRecord> result, List<TactTimeRecord> records)
        {
            TryAddOutputReceiveAverageRecord(result, records, "", "전체 평균");
            TryAddOutputReceiveAverageRecord(result, records, "OK", "OK 평균");
            TryAddOutputReceiveAverageRecord(result, records, "NG", "NG 평균");
        }

        private static void AddInspectionDetailAverageRecords(List<TactTimeRecord> result, List<TactTimeRecord> records)
        {
            TryAddProcessAverageRecord(result, records, "Bottom Camera Inspect", "Bottom 검사시간 평균");
            TryAddProcessAverageRecord(result, records, "Bottom Camera Inspect Interval", "Bottom 검사간격 평균");
            TryAddProcessAverageRecord(result, records, "Bottom Vision To Pitch Move", "Bottom 비전 후 피치 이동 평균");
            TryAddProcessAverageRecord(result, records, "Side 0deg Inspect", "Side 0도 검사시간 평균");
            TryAddProcessAverageRecord(result, records, "Side 0deg Inspect Interval", "Side 0도 검사간격 평균");
            TryAddProcessAverageRecord(result, records, "Side 0deg To 90deg Motion", "Side 0도→90도 모션 평균");
            TryAddProcessAverageRecord(result, records, "Side 90deg Inspect", "Side 90도 검사시간 평균");
            TryAddProcessAverageRecord(result, records, "Side 90deg Inspect Interval", "Side 90도 검사간격 평균");
        }

        private static List<TactTimeRecord> ApplyItemFilter(List<TactTimeRecord> records, string itemFilter)
        {
            if (records == null)
                return new List<TactTimeRecord>();
            return records.Where(x => MatchesItemFilter(x, itemFilter)).ToList();
        }

        private static bool MatchesItemFilter(TactTimeRecord record, string itemFilter)
        {
            if (record == null)
                return false;
            if (string.IsNullOrWhiteSpace(itemFilter) || string.Equals(itemFilter, "ALL", StringComparison.OrdinalIgnoreCase))
                return true;

            string process = record.ProcessName ?? "";
            if (string.Equals(itemFilter, "UNIT FLOW", StringComparison.OrdinalIgnoreCase))
                return record.Category == TactTimeCategory.Unit || IsMajorUnitRecord(record);
            if (string.Equals(itemFilter, "OUTPUT RECEIVE", StringComparison.OrdinalIgnoreCase))
                return EqualsAny(process, "Output Receive TactTime", "Output Receive AVG");
            if (string.Equals(itemFilter, "BOTTOM INSPECT", StringComparison.OrdinalIgnoreCase))
                return EqualsAny(process, "Bottom Camera Inspect", "Bottom Camera Inspect AVG");
            if (string.Equals(itemFilter, "BOTTOM VISION->PITCH", StringComparison.OrdinalIgnoreCase))
                return EqualsAny(process, "Bottom Vision To Pitch Move", "Bottom Vision To Pitch Move AVG");
            if (string.Equals(itemFilter, "BOTTOM INTERVAL", StringComparison.OrdinalIgnoreCase))
                return EqualsAny(process, "Bottom Camera Inspect Interval", "Bottom Camera Inspect Interval AVG");
            if (string.Equals(itemFilter, "SIDE 0 INSPECT", StringComparison.OrdinalIgnoreCase))
                return EqualsAny(process, "Side 0deg Inspect", "Side 0deg Inspect AVG");
            if (string.Equals(itemFilter, "SIDE 0 INTERVAL", StringComparison.OrdinalIgnoreCase))
                return EqualsAny(process, "Side 0deg Inspect Interval", "Side 0deg Inspect Interval AVG");
            if (string.Equals(itemFilter, "SIDE 0->90 MOTION", StringComparison.OrdinalIgnoreCase))
                return EqualsAny(process, "Side 0deg To 90deg Motion", "Side 0deg To 90deg Motion AVG");
            if (string.Equals(itemFilter, "SIDE 90 INSPECT", StringComparison.OrdinalIgnoreCase))
                return EqualsAny(process, "Side 90deg Inspect", "Side 90deg Inspect AVG");
            if (string.Equals(itemFilter, "SIDE 90 INTERVAL", StringComparison.OrdinalIgnoreCase))
                return EqualsAny(process, "Side 90deg Inspect Interval", "Side 90deg Inspect Interval AVG");
            return true;
        }

        private static void TryAddProcessAverageRecord(
            List<TactTimeRecord> result,
            List<TactTimeRecord> records,
            string processName,
            string label)
        {
            List<TactTimeRecord> targets = records
                .Where(x => x != null && string.Equals(x.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (targets.Count == 0)
                return;

            TactTimeRecord first = targets.OrderBy(x => x.StartedAt).First();
            TactTimeRecord last = targets.OrderByDescending(x => x.EndedAt).First();
            long average = (long)targets.Average(x => (double)Math.Max(0, x.ElapsedMs));
            result.Add(new TactTimeRecord
            {
                RunId = last.RunId,
                ParentId = last.ParentId,
                CorrelationId = "INSPECTION-AVG-" + processName.Replace(" ", "-"),
                EquipmentId = last.EquipmentId,
                ProjectName = last.ProjectName,
                LotId = last.LotId,
                Mode = last.Mode,
                UnitName = "Inspection",
                SequenceName = "SequenceSummary",
                ProcessName = processName + " AVG",
                StepName = label,
                Category = TactTimeCategory.Process,
                StartedAt = first.StartedAt,
                EndedAt = last.EndedAt,
                ElapsedMs = average,
                Result = TactTimeResult.Ok,
                Detail = label + "입니다. count=" + targets.Count + ", avgMs=" + average
            });
        }

        private static void TryAddOutputReceiveAverageRecord(
            List<TactTimeRecord> result,
            List<TactTimeRecord> records,
            string side,
            string label)
        {
            List<TactTimeRecord> targets = records
                .Where(x => x != null && (string.IsNullOrWhiteSpace(side) ||
                    string.Equals(x.StepName, side, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (targets.Count == 0)
                return;

            TactTimeRecord first = targets.OrderBy(x => x.StartedAt).First();
            TactTimeRecord last = targets.OrderByDescending(x => x.EndedAt).First();
            long average = (long)targets.Average(x => (double)Math.Max(0, x.ElapsedMs));
            result.Add(new TactTimeRecord
            {
                RunId = last.RunId,
                ParentId = last.ParentId,
                CorrelationId = "OUTPUT-RECEIVE-AVG-" + (string.IsNullOrWhiteSpace(side) ? "ALL" : side),
                EquipmentId = last.EquipmentId,
                ProjectName = last.ProjectName,
                LotId = last.LotId,
                Mode = last.Mode,
                UnitName = "OutputStage",
                SequenceName = "SequenceSummary",
                ProcessName = "Output Receive AVG",
                StepName = label,
                Category = TactTimeCategory.Process,
                StartedAt = first.StartedAt,
                EndedAt = last.EndedAt,
                ElapsedMs = average,
                Result = TactTimeResult.Ok,
                Detail = "OutputStage 제품 1개 수령 간격 평균입니다. 기준=" + label +
                         ", count=" + targets.Count + ", avgMs=" + average
            });
        }

        private static bool IsMajorUnitRecord(TactTimeRecord record)
        {
            if (record == null || record.Category != TactTimeCategory.Unit)
                return false;

            string unit = record.UnitName ?? "";
            string sequence = record.SequenceName ?? "";
            return ContainsAny(unit, "Input", "Output", "FrontPicker", "RearPicker") ||
                   ContainsAny(sequence, "InputSequence", "OutputSequence", "FrontPickerSequence", "RearPickerSequence");
        }

        private static bool IsPlaceProcessRecord(TactTimeRecord record)
        {
            return record != null &&
                   record.Category == TactTimeCategory.Process &&
                   string.Equals(record.ProcessName, "Place", StringComparison.OrdinalIgnoreCase) &&
                   record.EndedAt != DateTime.MinValue;
        }

        private static bool IsOutputReceiveTactRecord(TactTimeRecord record)
        {
            return record != null &&
                   record.Category == TactTimeCategory.Process &&
                   string.Equals(record.ProcessName, "Output Receive TactTime", StringComparison.OrdinalIgnoreCase) &&
                   record.EndedAt != DateTime.MinValue;
        }

        private static bool IsInspectionDetailTactRecord(TactTimeRecord record)
        {
            if (record == null)
                return false;

            return EqualsAny(record.ProcessName ?? "",
                "Bottom Camera Inspect",
                "Bottom Camera Inspect Interval",
                "Bottom Vision To Pitch Move",
                "Side 0deg Inspect",
                "Side 0deg Inspect Interval",
                "Side 0deg To 90deg Motion",
                "Side 90deg Inspect",
                "Side 90deg Inspect Interval");
        }

        private void UpdateGrid(List<TactTimeRecord> records)
        {
            _grid.SuspendLayout();
            try
            {
                _grid.Rows.Clear();
                int firstIndex = Math.Max(0, records.Count - MaxGridRows);
                int rowNo = 1;
                for (int i = records.Count - 1; i >= firstIndex; i--)
                {
                    TactTimeRecord record = records[i];
                    int index = _grid.Rows.Add(
                        rowNo.ToString(),
                        record.Category.ToString(),
                        Safe(record.UnitName),
                        Safe(record.SequenceName),
                        Safe(record.ProcessName),
                        Safe(record.StepName),
                        record.Result.ToString(),
                        record.ElapsedMs.ToString("N0"),
                        FormatTime(record.StartedAt),
                        FormatTime(record.EndedAt),
                        Safe(record.Detail));

                    DataGridViewRow row = _grid.Rows[index];
                    row.Tag = record;
                    row.DefaultCellStyle.BackColor = ResolveResultBackColor(record.Result);
                    rowNo++;
                }

                if (_grid.Rows.Count > 0 && _grid.SelectedRows.Count == 0)
                    _grid.Rows[0].Selected = true;
            }
            finally
            {
                _grid.ResumeLayout();
            }
        }

        private void UpdateChartRecords(List<TactTimeRecord> records, bool preserveView)
        {
            _chartRecords.Clear();
            for (int i = 0; i < records.Count; i++)
            {
                TactTimeRecord record = records[i];
                if (record.StartedAt == DateTime.MinValue || record.EndedAt == DateTime.MinValue)
                    continue;
                _chartRecords.Add(record);
            }

            _timeChart.EmptyMessage = _historyMode
                ? "선택한 과거 Run에 해당하는 택타임 기록이 없습니다."
                : "장비가 운전되면 실시간 택타임이 표시됩니다.";
            _timeChart.UpdateRecords(_chartRecords, preserveView);
        }

        private void UpdateSummary(List<TactTimeRecord> records)
        {
            if (records.Count == 0)
            {
                lblSummary.Text = "표시할 택타임 기록이 없습니다.";
                lblStatus.Text = _historyMode
                    ? "선택한 Run 또는 필터에 해당하는 기록이 없습니다."
                    : "택타임 기록을 기다리는 중입니다.";
                return;
            }

            DateTime start = records.Min(x => x.StartedAt);
            DateTime end = records.Max(x => x.EndedAt);
            List<TactTimeRecord> metricRecords = ResolveMetricRecords(records);
            bool containersExcluded = metricRecords.Count != records.Count;
            double spanMs = Math.Max(0.0, (end - start).TotalMilliseconds);
            double average = metricRecords.Average(x => (double)Math.Max(0, x.ElapsedMs));
            long minimum = metricRecords.Min(x => Math.Max(0, x.ElapsedMs));
            long maximum = metricRecords.Max(x => Math.Max(0, x.ElapsedMs));
            long p95 = CalculateNearestRankPercentile(metricRecords, 0.95);
            int failed = records.Count(x => x.Result == TactTimeResult.Failed);
            int stopped = records.Count(x => x.Result == TactTimeResult.Stopped || x.Result == TactTimeResult.Canceled);
            int displayed = Math.Min(records.Count, MaxGridRows);
            int statisticGroups = metricRecords.Select(ResolveStatisticItemKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            string mixedNotice = statisticGroups > 1
                ? " / 평균·P95 혼합 참고값(" + statisticGroups + "종)"
                : "";

            lblSummary.Text =
                "기록 " + records.Count.ToString("N0") + "건" +
                (records.Count > MaxGridRows ? " (Grid " + displayed.ToString("N0") + "건)" : "") +
                " / 구간 " + FormatDuration(spanMs) +
                " / 평균 " + FormatDuration(average) +
                " / 최소 " + FormatDuration(minimum) +
                " / P95 " + FormatDuration(p95) +
                " / 최대 " + FormatDuration(maximum) +
                " / 실패 " + failed + " / 정지 " + stopped +
                (containersExcluded ? " / 통계 Run·Unit 중첩 제외" : "") +
                mixedNotice;
        }

        private static List<TactTimeRecord> ResolveMetricRecords(List<TactTimeRecord> records)
        {
            List<TactTimeRecord> details = records
                .Where(x => x.Category != TactTimeCategory.Run && x.Category != TactTimeCategory.Unit)
                .ToList();
            return details.Count > 0 ? details : records;
        }

        private static string ResolveStatisticItemKey(TactTimeRecord record)
        {
            if (record == null)
                return "기타";
            if (!string.IsNullOrWhiteSpace(record.ProcessName))
                return record.ProcessName;
            if (!string.IsNullOrWhiteSpace(record.StepName))
                return record.StepName;
            if (!string.IsNullOrWhiteSpace(record.SequenceName))
                return record.SequenceName;
            return record.Category.ToString();
        }

        private void UpdateSelectedRecordStatus(TactTimeRecord record)
        {
            lblStatus.Text =
                "선택: " + record.Category +
                " / " + Safe(record.UnitName) +
                " / " + Safe(record.ProcessName) +
                " / " + Safe(record.StepName) +
                " / " + record.ElapsedMs.ToString("N0") + " ms" +
                " / " + record.Result +
                (string.IsNullOrWhiteSpace(record.AlarmCode) ? "" : " / " + record.AlarmCode) +
                (string.IsNullOrWhiteSpace(record.Detail) ? "" : " / " + record.Detail);
        }

        private void UpdateRefreshTimer()
        {
            if (_refresh == null)
                return;

            if (Visible && chkAutoRefresh.Checked && !_historyMode && !_historyLoading)
                _refresh.Start();
            else
                _refresh.Stop();
        }

        private void HandleHistoryFailure(string operation, string message, Exception ex)
        {
            LogUiFailure(operation, ex);
            lblStatus.Text = message + " " + ex.Message;
            MessageBox.Show(this, message + Environment.NewLine + Environment.NewLine + ex.Message,
                "TIMECHART", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void LogUiFailure(string operation, Exception ex)
        {
            QMC.Common.Log.Write("Main", "SYSTEM", "LogicTimeChart",
                "TIMECHART UI 작업 실패. operation=" + operation +
                ", file=" + (string.IsNullOrWhiteSpace(_historyFilePath) ? "-" : _historyFilePath) +
                ", progress=" + progressHistory.Value + "%" +
                ", error=" + (ex != null ? ex.Message : "-") + " - Failed");
        }

        private static string ResolveTactTimeDirectory()
        {
            string configured = Path.Combine(EventLogger.LogRoot, "TactTime");
            if (Directory.Exists(configured))
                return configured;

            const string deployed = @"D:\CDT-320\Log\TactTime";
            if (Directory.Exists(deployed))
                return deployed;

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static string BuildSignature(List<TactTimeRecord> records)
        {
            if (records == null || records.Count == 0)
                return "0";

            TactTimeRecord first = records[0];
            TactTimeRecord last = records[records.Count - 1];
            return records.Count + "|" + first.RunId + "|" + last.RunId + "|" +
                   last.CorrelationId + "|" + first.StartedAt.Ticks + "|" + last.EndedAt.Ticks + "|" + last.Result;
        }

        private static long CalculateNearestRankPercentile(List<TactTimeRecord> records, double percentile)
        {
            if (records == null || records.Count == 0)
                return 0;

            long[] values = records.Select(x => Math.Max(0, x.ElapsedMs)).OrderBy(x => x).ToArray();
            int rank = (int)Math.Ceiling(Math.Max(0.0, Math.Min(1.0, percentile)) * values.Length);
            return values[Math.Max(0, Math.Min(values.Length - 1, rank - 1))];
        }

        private static Color ResolveResultBackColor(TactTimeResult result)
        {
            switch (result)
            {
                case TactTimeResult.Failed:
                    return Color.FromArgb(0xFF, 0xDD, 0xDD);
                case TactTimeResult.Stopped:
                case TactTimeResult.Canceled:
                    return Color.FromArgb(0xFF, 0xF2, 0xCC);
                case TactTimeResult.Skipped:
                    return Color.FromArgb(0xEE, 0xEE, 0xEE);
                default:
                    return Color.White;
            }
        }

        private static bool IsSameRecord(TactTimeRecord left, TactTimeRecord right)
        {
            if (object.ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;
            if (!string.IsNullOrWhiteSpace(left.CorrelationId) && !string.IsNullOrWhiteSpace(right.CorrelationId))
                return string.Equals(left.CorrelationId, right.CorrelationId, StringComparison.OrdinalIgnoreCase);

            return left.StartedAt == right.StartedAt &&
                   left.EndedAt == right.EndedAt &&
                   string.Equals(left.UnitName, right.UnitName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(left.ProcessName, right.ProcessName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(left.StepName, right.StepName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool EqualsAny(string value, params string[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                if (string.Equals(value, candidates[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            value = value ?? "";
            for (int i = 0; i < tokens.Length; i++)
            {
                if (value.IndexOf(tokens[i] ?? "", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static string FormatTime(DateTime value)
        {
            return value == DateTime.MinValue ? "-" : value.ToString("HH:mm:ss.fff");
        }

        private static string FormatDuration(double milliseconds)
        {
            if (milliseconds < 1000.0)
                return Math.Max(0.0, milliseconds).ToString("0") + " ms";
            if (milliseconds < 60000.0)
                return (milliseconds / 1000.0).ToString("0.###") + " s";
            return TimeSpan.FromMilliseconds(milliseconds).ToString(@"hh\:mm\:ss\.fff");
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024L)
                return bytes + " B";
            if (bytes < 1024L * 1024L)
                return (bytes / 1024.0).ToString("0.0") + " KB";
            return (bytes / 1024.0 / 1024.0).ToString("0.0") + " MB";
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRefreshTimer();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            CancelHistoryLoad();
            if (_refresh != null)
            {
                _refresh.Stop();
                _refresh.Tick -= refresh_Tick;
                _refresh.Dispose();
                _refresh = null;
            }

            _timeChart.RecordSelected -= timeChart_RecordSelected;
            VisibleChanged -= LogicDetailPage_VisibleChanged;
            base.OnHandleDestroyed(e);
        }
    }
}
