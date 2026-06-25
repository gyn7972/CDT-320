using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.Common.Diagnostics.TactTime;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class LogicDetailPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private readonly List<TactTimeRecord> _chartRecords = new List<TactTimeRecord>();
        private readonly List<string> _chartLanes = new List<string>();
        private Timer _refresh;
        private string _lastSignature = string.Empty;
        private DateTime _viewSince = DateTime.MinValue;

        public LogicDetailPage()
        {
            InitializeComponent();

            if (!IsDesignerMode())
            {
                if (cmbCategory.Items.Count > 0)
                    cmbCategory.SelectedIndex = 0;
                if (cmbItemFilter.Items.Count > 0)
                    cmbItemFilter.SelectedIndex = 0;

                _refresh = new Timer { Interval = 1000 };
                _refresh.Tick += (s, e) =>
                {
                    if (!ShouldRefreshVisible(this) || !chkAutoRefresh.Checked)
                        return;

                    RefreshAll(false);
                };
                VisibleChanged += (s, e) => UpdateRefreshTimer();
                RefreshAll(true);
            }
        }

        private void cmbCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshAll(true);
        }

        private void cmbItemFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshAll(true);
        }

        private void chkAutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRefreshTimer();
            RefreshAll(true);
        }

        private void btnClearView_Click(object sender, EventArgs e)
        {
            try
            {
                _viewSince = DateTime.Now;
                _lastSignature = string.Empty;
                _chartRecords.Clear();
                _chartLanes.Clear();
                _grid.Rows.Clear();
                lblSummary.Text = "화면 기록을 초기화했습니다. 이후 발생한 택타임만 표시합니다.";
                lblStatus.Text = "화면 기록을 초기화했습니다.";
                _chartHost.Invalidate();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void grid_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (_grid.SelectedRows.Count <= 0)
                    return;

                TactTimeRecord record = _grid.SelectedRows[0].Tag as TactTimeRecord;
                if (record == null)
                    return;

                lblStatus.Text =
                    "선택: " + record.Category +
                    " / " + Safe(record.UnitName) +
                    " / " + Safe(record.ProcessName) +
                    " / " + Safe(record.StepName) +
                    " / " + record.ElapsedMs + " ms" +
                    " / " + record.Result +
                    (string.IsNullOrWhiteSpace(record.Detail) ? "" : " / " + record.Detail);
            }
            catch
            {
            }
            finally
            {
            }
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
                UpdateChartRecords(records);
                UpdateSummary(records);
                _chartHost.Invalidate();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private List<TactTimeRecord> LoadFilteredRecords()
        {
            var result = new List<TactTimeRecord>();
            try
            {
                var host = (FindForm() ?? ParentForm) as Form1;
                MachineController ctrl = host != null ? host.Controller : null;
                IReadOnlyList<TactTimeRecord> snapshot = ctrl != null ? ctrl.GetTactTimeSnapshot() : null;
                string category = cmbCategory.SelectedItem != null ? cmbCategory.SelectedItem.ToString() : "ALL";
                string itemFilter = cmbItemFilter.SelectedItem != null ? cmbItemFilter.SelectedItem.ToString() : "ALL";

                if (snapshot == null)
                    return result;

                if (string.Equals(category, "SEQUENCE", StringComparison.OrdinalIgnoreCase))
                    return ApplyItemFilter(BuildSequenceSummaryRecords(snapshot), itemFilter);

                for (int i = 0; i < snapshot.Count; i++)
                {
                    TactTimeRecord record = snapshot[i];
                    if (record == null)
                        continue;

                    if (_viewSince != DateTime.MinValue && record.StartedAt < _viewSince)
                        continue;

                    if (!string.Equals(category, "ALL", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(record.Category.ToString(), category, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!MatchesItemFilter(record, itemFilter))
                        continue;

                    result.Add(record);
                }

                result.Sort((a, b) => a.StartedAt.CompareTo(b.StartedAt));
            }
            catch
            {
            }
            finally
            {
            }

            return result;
        }

        private List<TactTimeRecord> BuildSequenceSummaryRecords(IReadOnlyList<TactTimeRecord> snapshot)
        {
            var result = new List<TactTimeRecord>();
            var placeRecords = new List<TactTimeRecord>();
            var outputReceiveRecords = new List<TactTimeRecord>();
            var inspectionDetailRecords = new List<TactTimeRecord>();
            try
            {
                if (snapshot == null)
                    return result;

                for (int i = 0; i < snapshot.Count; i++)
                {
                    TactTimeRecord record = snapshot[i];
                    if (record == null)
                        continue;

                    if (_viewSince != DateTime.MinValue && record.StartedAt < _viewSince)
                        continue;

                    if (IsMajorUnitRecord(record))
                    {
                        TactTimeRecord clone = record.Clone();
                        clone.Detail = "유닛 전체 동작 택타임. " + Safe(record.Detail);
                        result.Add(clone);
                    }

                    if (IsPlaceProcessRecord(record))
                        placeRecords.Add(record);

                    if (IsOutputReceiveTactRecord(record))
                    {
                        TactTimeRecord clone = record.Clone();
                        clone.Detail = "OutputStage 제품 1개 수령 기준 택타임. " + Safe(record.Detail);
                        outputReceiveRecords.Add(clone);
                    }

                    if (IsInspectionDetailTactRecord(record))
                    {
                        TactTimeRecord clone = record.Clone();
                        clone.Detail = "검사 세부 택타임. " + Safe(record.Detail);
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

                if (outputReceiveRecords.Count > 0 || inspectionDetailRecords.Count > 0)
                {
                    result.Sort((a, b) => a.StartedAt.CompareTo(b.StartedAt));
                    return result;
                }

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
                        Detail = "Output 수령 이벤트가 없어 Place 완료 기준으로 임시 계산한 시간입니다. previous=" +
                                 Safe(previous.UnitName) + "/" + Safe(previous.ProcessName) +
                                 ", current=" + Safe(current.UnitName) + "/" + Safe(current.ProcessName)
                    });
                }

                result.Sort((a, b) => a.StartedAt.CompareTo(b.StartedAt));
            }
            catch
            {
            }
            finally
            {
            }

            return result;
        }

        private static void AddOutputReceiveAverageRecords(List<TactTimeRecord> result, List<TactTimeRecord> records)
        {
            TryAddOutputReceiveAverageRecord(result, records, "", "전체 평균");
            TryAddOutputReceiveAverageRecord(result, records, "OK", "OK 평균");
            TryAddOutputReceiveAverageRecord(result, records, "NG", "NG 평균");
        }

        private static void AddInspectionDetailAverageRecords(List<TactTimeRecord> result, List<TactTimeRecord> records)
        {
            TryAddProcessAverageRecord(result, records, "Bottom Camera Inspect", "Bottom 검사 시간 평균");
            TryAddProcessAverageRecord(result, records, "Bottom Camera Inspect Interval", "Bottom 검사 간격 평균");
            TryAddProcessAverageRecord(result, records, "Bottom Vision To Pitch Move", "Bottom 비전 후 피치 이동 평균");
            TryAddProcessAverageRecord(result, records, "Side 0deg Inspect", "Side 0도 검사 시간 평균");
            TryAddProcessAverageRecord(result, records, "Side 0deg Inspect Interval", "Side 0도 검사 간격 평균");
            TryAddProcessAverageRecord(result, records, "Side 0deg To 90deg Motion", "Side 0도→90도 모션 평균");
            TryAddProcessAverageRecord(result, records, "Side 90deg Inspect", "Side 90도 검사 시간 평균");
            TryAddProcessAverageRecord(result, records, "Side 90deg Inspect Interval", "Side 90도 검사 간격 평균");
        }

        private static List<TactTimeRecord> ApplyItemFilter(List<TactTimeRecord> records, string itemFilter)
        {
            var filtered = new List<TactTimeRecord>();
            try
            {
                if (records == null)
                    return filtered;

                for (int i = 0; i < records.Count; i++)
                {
                    TactTimeRecord record = records[i];
                    if (MatchesItemFilter(record, itemFilter))
                        filtered.Add(record);
                }
            }
            catch
            {
            }
            finally
            {
            }

            return filtered;
        }

        private static bool MatchesItemFilter(TactTimeRecord record, string itemFilter)
        {
            if (record == null)
                return false;

            if (string.IsNullOrWhiteSpace(itemFilter) ||
                string.Equals(itemFilter, "ALL", StringComparison.OrdinalIgnoreCase))
                return true;

            string process = record.ProcessName ?? "";

            if (string.Equals(itemFilter, "UNIT FLOW", StringComparison.OrdinalIgnoreCase))
                return record.Category == TactTimeCategory.Unit || IsMajorUnitRecord(record);

            if (string.Equals(itemFilter, "OUTPUT RECEIVE", StringComparison.OrdinalIgnoreCase))
                return string.Equals(process, "Output Receive TactTime", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(process, "Output Receive AVG", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(itemFilter, "BOTTOM INSPECT", StringComparison.OrdinalIgnoreCase))
                return string.Equals(process, "Bottom Camera Inspect", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(process, "Bottom Camera Inspect AVG", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(itemFilter, "BOTTOM VISION->PITCH", StringComparison.OrdinalIgnoreCase))
                return string.Equals(process, "Bottom Vision To Pitch Move", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(process, "Bottom Vision To Pitch Move AVG", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(itemFilter, "BOTTOM INTERVAL", StringComparison.OrdinalIgnoreCase))
                return string.Equals(process, "Bottom Camera Inspect Interval", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(process, "Bottom Camera Inspect Interval AVG", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(itemFilter, "SIDE 0 INSPECT", StringComparison.OrdinalIgnoreCase))
                return string.Equals(process, "Side 0deg Inspect", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(process, "Side 0deg Inspect AVG", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(itemFilter, "SIDE 0 INTERVAL", StringComparison.OrdinalIgnoreCase))
                return string.Equals(process, "Side 0deg Inspect Interval", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(process, "Side 0deg Inspect Interval AVG", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(itemFilter, "SIDE 0->90 MOTION", StringComparison.OrdinalIgnoreCase))
                return string.Equals(process, "Side 0deg To 90deg Motion", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(process, "Side 0deg To 90deg Motion AVG", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(itemFilter, "SIDE 90 INSPECT", StringComparison.OrdinalIgnoreCase))
                return string.Equals(process, "Side 90deg Inspect", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(process, "Side 90deg Inspect AVG", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(itemFilter, "SIDE 90 INTERVAL", StringComparison.OrdinalIgnoreCase))
                return string.Equals(process, "Side 90deg Inspect Interval", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(process, "Side 90deg Inspect Interval AVG", StringComparison.OrdinalIgnoreCase);

            return true;
        }

        private static void TryAddProcessAverageRecord(
            List<TactTimeRecord> result,
            List<TactTimeRecord> records,
            string processName,
            string label)
        {
            try
            {
                if (result == null || records == null || string.IsNullOrWhiteSpace(processName))
                    return;

                long sum = 0;
                int count = 0;
                TactTimeRecord first = null;
                TactTimeRecord last = null;
                for (int i = 0; i < records.Count; i++)
                {
                    TactTimeRecord record = records[i];
                    if (record == null ||
                        !string.Equals(record.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (first == null || record.StartedAt < first.StartedAt)
                        first = record;
                    if (last == null || record.EndedAt > last.EndedAt)
                        last = record;

                    sum += Math.Max(0, record.ElapsedMs);
                    count++;
                }

                if (count <= 0 || first == null || last == null)
                    return;

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
                    ElapsedMs = sum / count,
                    Result = TactTimeResult.Ok,
                    Detail = label + "입니다. count=" + count + ", avgMs=" + (sum / count)
                });
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void TryAddOutputReceiveAverageRecord(
            List<TactTimeRecord> result,
            List<TactTimeRecord> records,
            string side,
            string label)
        {
            try
            {
                if (result == null || records == null || records.Count == 0)
                    return;

                long sum = 0;
                int count = 0;
                TactTimeRecord first = null;
                TactTimeRecord last = null;
                for (int i = 0; i < records.Count; i++)
                {
                    TactTimeRecord record = records[i];
                    if (record == null)
                        continue;

                    if (!string.IsNullOrWhiteSpace(side) &&
                        !string.Equals(record.StepName, side, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (first == null || record.StartedAt < first.StartedAt)
                        first = record;
                    if (last == null || record.EndedAt > last.EndedAt)
                        last = record;

                    sum += Math.Max(0, record.ElapsedMs);
                    count++;
                }

                if (count <= 0 || first == null || last == null)
                    return;

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
                    ElapsedMs = sum / count,
                    Result = TactTimeResult.Ok,
                    Detail = "OutputStage가 제품 1개를 받은 간격의 평균입니다. 기준=" +
                             label + ", count=" + count +
                             ", avgMs=" + (sum / count)
                });
            }
            catch
            {
            }
            finally
            {
            }
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
            if (record == null || record.Category != TactTimeCategory.Process)
                return false;

            return string.Equals(record.ProcessName, "Place", StringComparison.OrdinalIgnoreCase) &&
                   record.EndedAt != DateTime.MinValue;
        }

        private static bool IsOutputReceiveTactRecord(TactTimeRecord record)
        {
            if (record == null || record.Category != TactTimeCategory.Process)
                return false;

            return string.Equals(record.ProcessName, "Output Receive TactTime", StringComparison.OrdinalIgnoreCase) &&
                   record.EndedAt != DateTime.MinValue;
        }

        private static bool IsInspectionDetailTactRecord(TactTimeRecord record)
        {
            if (record == null)
                return false;

            string process = record.ProcessName ?? "";
            return string.Equals(process, "Bottom Camera Inspect", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(process, "Bottom Camera Inspect Interval", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(process, "Bottom Vision To Pitch Move", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(process, "Side 0deg Inspect", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(process, "Side 0deg Inspect Interval", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(process, "Side 0deg To 90deg Motion", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(process, "Side 90deg Inspect", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(process, "Side 90deg Inspect Interval", StringComparison.OrdinalIgnoreCase);
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

        private void UpdateGrid(List<TactTimeRecord> records)
        {
            _grid.SuspendLayout();
            try
            {
                _grid.Rows.Clear();
                int rowNo = 1;
                for (int i = records.Count - 1; i >= 0; i--)
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
                        record.ElapsedMs.ToString(),
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

        private void UpdateChartRecords(List<TactTimeRecord> records)
        {
            _chartRecords.Clear();
            _chartLanes.Clear();

            for (int i = 0; i < records.Count; i++)
            {
                TactTimeRecord record = records[i];
                if (record.EndedAt == DateTime.MinValue || record.StartedAt == DateTime.MinValue)
                    continue;

                _chartRecords.Add(record);

                string lane = ResolveLaneName(record);
                if (!_chartLanes.Contains(lane))
                    _chartLanes.Add(lane);
            }
        }

        private void UpdateSummary(List<TactTimeRecord> records)
        {
            long totalMs = 0;
            int failed = 0;
            int stopped = 0;
            for (int i = 0; i < records.Count; i++)
            {
                totalMs += Math.Max(0, records[i].ElapsedMs);
                if (records[i].Result == TactTimeResult.Failed)
                    failed++;
                else if (records[i].Result == TactTimeResult.Stopped || records[i].Result == TactTimeResult.Canceled)
                    stopped++;
            }

            lblSummary.Text =
                "records=" + records.Count +
                " / total=" + totalMs + " ms" +
                " / failed=" + failed +
                " / stopped=" + stopped +
                " / filter=" + (cmbCategory.SelectedItem != null ? cmbCategory.SelectedItem.ToString() : "ALL") +
                " / item=" + (cmbItemFilter.SelectedItem != null ? cmbItemFilter.SelectedItem.ToString() : "ALL");

            if (records.Count == 0)
                lblStatus.Text = "택타임 기록이 없습니다. 시퀀스를 실행하면 이 화면에 기록됩니다.";
        }

        private void chartHost_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                DrawChart(e.Graphics, _chartHost.ClientRectangle);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void DrawChart(Graphics graphics, Rectangle bounds)
        {
            graphics.Clear(Color.White);

            if (_chartRecords.Count == 0 || bounds.Width <= 20 || bounds.Height <= 20)
            {
                using (var font = new Font("맑은 고딕", 10F, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.DimGray))
                    graphics.DrawString("택타임 기록이 없습니다.", font, brush, 16, 16);
                return;
            }

            DateTime start = _chartRecords[0].StartedAt;
            DateTime end = _chartRecords[0].EndedAt;
            for (int i = 0; i < _chartRecords.Count; i++)
            {
                if (_chartRecords[i].StartedAt < start)
                    start = _chartRecords[i].StartedAt;
                if (_chartRecords[i].EndedAt > end)
                    end = _chartRecords[i].EndedAt;
            }

            double totalMs = Math.Max(1000.0, (end - start).TotalMilliseconds);
            int left = 180;
            int top = 16;
            int bottom = 28;
            int rowHeight = 30;
            int plotWidth = Math.Max(1, bounds.Width - left - 20);

            using (var gridPen = new Pen(Color.FromArgb(0xDD, 0xDD, 0xDD)))
            using (var textBrush = new SolidBrush(Color.Black))
            using (var axisBrush = new SolidBrush(Color.DimGray))
            using (var font = new Font("맑은 고딕", 8.5F))
            {
                for (int i = 0; i < _chartLanes.Count; i++)
                {
                    int y = top + i * rowHeight;
                    graphics.DrawLine(gridPen, 0, y + rowHeight, bounds.Width, y + rowHeight);
                    graphics.DrawString(_chartLanes[i], font, textBrush, 8, y + 7);
                }

                int axisY = Math.Min(bounds.Height - bottom, top + _chartLanes.Count * rowHeight + 8);
                for (int i = 0; i <= 10; i++)
                {
                    int x = left + (int)(plotWidth * (i / 10.0));
                    graphics.DrawLine(gridPen, x, top, x, axisY);
                    double sec = totalMs * i / 10.0 / 1000.0;
                    graphics.DrawString(sec.ToString("0.0") + "s", font, axisBrush, x - 10, axisY + 4);
                }

                for (int i = 0; i < _chartRecords.Count; i++)
                {
                    TactTimeRecord record = _chartRecords[i];
                    int lane = _chartLanes.IndexOf(ResolveLaneName(record));
                    if (lane < 0)
                        continue;

                    int y = top + lane * rowHeight + 7;
                    int x = left + (int)(((record.StartedAt - start).TotalMilliseconds / totalMs) * plotWidth);
                    int w = Math.Max(2, (int)((Math.Max(1, record.ElapsedMs) / totalMs) * plotWidth));
                    using (var brush = new SolidBrush(ResolveCategoryColor(record)))
                    {
                        graphics.FillRectangle(brush, x, y, w, 16);
                    }
                    graphics.DrawRectangle(Pens.DimGray, x, y, w, 16);
                }

                graphics.DrawString("axis = " + (totalMs / 1000.0).ToString("0.0") + "s", font, axisBrush, bounds.Width - 90, bounds.Height - 18);
            }
        }

        private void UpdateRefreshTimer()
        {
            try
            {
                if (_refresh == null)
                    return;

                if (Visible && chkAutoRefresh.Checked)
                    _refresh.Start();
                else
                    _refresh.Stop();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static string BuildSignature(List<TactTimeRecord> records)
        {
            if (records == null || records.Count == 0)
                return "0";

            TactTimeRecord last = records[records.Count - 1];
            return records.Count + "|" + last.RunId + "|" + last.CorrelationId + "|" +
                   last.StartedAt.Ticks + "|" + last.EndedAt.Ticks + "|" + last.Result;
        }

        private static string ResolveLaneName(TactTimeRecord record)
        {
            if (record == null)
                return "-";

            if (record.Category == TactTimeCategory.Run)
                return "Run";

            if (!string.IsNullOrWhiteSpace(record.UnitName))
                return record.UnitName;

            if (!string.IsNullOrWhiteSpace(record.SequenceName))
                return record.SequenceName;

            return record.Category.ToString();
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

        private static Color ResolveCategoryColor(TactTimeRecord record)
        {
            if (record == null)
                return Color.Gray;

            if (record.Result == TactTimeResult.Failed)
                return Color.FromArgb(0xD9, 0x44, 0x44);
            if (record.Result == TactTimeResult.Stopped || record.Result == TactTimeResult.Canceled)
                return Color.FromArgb(0xD9, 0xA8, 0x58);

            switch (record.Category)
            {
                case TactTimeCategory.Run:
                    return Color.FromArgb(0x5B, 0x8D, 0xD6);
                case TactTimeCategory.Unit:
                    return Color.FromArgb(0x58, 0xC0, 0xD9);
                case TactTimeCategory.Process:
                    return Color.FromArgb(0x58, 0xD9, 0xA8);
                case TactTimeCategory.Step:
                    return Color.FromArgb(0x88, 0x88, 0x88);
                case TactTimeCategory.Vision:
                    return Color.FromArgb(0x9A, 0x75, 0xD9);
                case TactTimeCategory.Motion:
                    return Color.FromArgb(0xD9, 0xA8, 0x58);
                default:
                    return Color.FromArgb(0x88, 0xB0, 0xC8);
            }
        }

        private static string FormatTime(DateTime value)
        {
            if (value == DateTime.MinValue)
                return "-";
            return value.ToString("HH:mm:ss.fff");
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
            try
            {
                _refresh?.Stop();
                _refresh?.Dispose();
            }
            catch
            {
            }
            finally
            {
            }

            base.OnHandleDestroyed(e);
        }
    }
}
