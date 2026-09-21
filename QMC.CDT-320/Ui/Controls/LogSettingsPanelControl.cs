using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using QMC.Common.Logging;
using QMC.CDT320;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>로그 전용 공용 설정 판넬.
    /// - Log history view(ENABLE/DISABLE) → 이력 탭 로그 로드 여부(알람 페이지 무관).
    /// - 로그 압축 / 압축된 로그파일 삭제: 사용 여부 + 각 기간(일, 1~365, 마지막 값 기억).
    /// - 로그 종류별 파일 경로 그리드 / Vision OK·NG 이미지 경로 그리드(모두 "..."로 폴더 선택).
    /// - 이미지 파일 형식(JPG/BMP) 콤보.
    /// 값은 <see cref="Save"/> 호출(다이얼로그 SAVE) 시 AppSettings 에 반영된다.</summary>
    public partial class LogSettingsPanelControl : UserControl, ILocalizedView
    {
        private static readonly string[] LogTypes =
        {
            "ALL LOG", "EVENT", "WARNING", "ALARM", "DATA",
            "WORK", "INPUT SEQ", "FRONTHEAD SEQ", "REARHEAD SEQ", "OUTPUT SEQ"
        };
        private static readonly string[] VisionImages = { "OK IMAGE", "NG IMAGE" };

        // ENABLE로 설정해둔 기간을 기억했다가 다시 ENABLE 될 때 복원한다.
        private int _lastCompressDays = 14;
        private int _lastDeleteDays = 90;

        // 진단 상세 로그(런타임 모드) UI — SAVE와 무관하게 즉시 적용된다.
        private Label _lblDiagStatus;
        private NumericUpDown _nDiagMinutes;
        private Button _btnDiagEnable;
        private Button _btnDiagDisable;
        private Timer _diagStatusTimer;

        public LogSettingsPanelControl()
        {
            InitializeComponent();
            InitializeLocalization();
            BuildDiagnosticVerboseUi();
            ConfigureGrids();
            BuildLogPathRows();
            BuildVisionRows();
            WireEvents();
            LoadSettings();
        }

        private void InitializeLocalization()
        {
            Lang.BindKey(grpSnapshot, "controls.log.snapshot");
            Lang.BindKey(lblSnapshotHint, "controls.log.snapshotHint");
            Lang.BindKey(grpMaint, "controls.log.maintenance");
            Lang.BindKey(lblLogHistory, "controls.log.history");
            Lang.BindKey(lblLogCompress, "controls.log.compress");
            Lang.BindKey(lblCompressDays, "controls.log.compressDays");
            Lang.BindKey(lblLogDelete, "controls.log.delete");
            Lang.BindKey(lblDeleteDays, "controls.log.deleteDays");
            Lang.BindKey(grpPaths, "controls.log.paths");
            Lang.BindKey(lblPathMode, "controls.log.pathMode");
            Lang.BindKey(grpVision, "controls.log.visionImages");
            Lang.BindKey(lblImageFormat, "controls.log.imageFormat");
            Lang.BindKey(colLogType, "controls.log.type");
            Lang.BindKey(colLogPath, "controls.log.filePath");
            Lang.BindKey(colVisType, "controls.log.image");
            Lang.BindKey(colVisPath, "controls.log.imagePath");
            Lang.BindReadOnlyCells(gridLogPaths, DisplayChoice, cell => cell.ColumnIndex == 0);
            Lang.BindReadOnlyCells(gridVisionImages, DisplayChoice, cell => cell.ColumnIndex == 0);
            foreach (ComboBox combo in new[] { _cbLogHistory, _cbLogCompress, _cbLogDelete, _cbPathMode })
            {
                Lang.BindChoices(combo, DisplayChoice);
            }
        }

        public void ApplyLanguage()
        {
            // 표시만 다시 그립니다. 저장 모드, 원본 행과 선택값을 다시 설정하지 않습니다.
            gridLogPaths.Invalidate();
            gridVisionImages.Invalidate();
            foreach (ComboBox combo in new[] { _cbLogHistory, _cbLogCompress, _cbLogDelete, _cbPathMode })
                combo.Invalidate();
        }

        private static string DisplayChoice(string value)
        {
            switch (value)
            {
                case "ENABLE": return Lang.T("controls.log.enable");
                case "DISABLE": return Lang.T("controls.log.disable");
                case "ALL (single folder)": return Lang.T("controls.log.modeAll");
                case "KIND (per type)": return Lang.T("controls.log.modeKind");
                case "ALL LOG": return Lang.T("controls.log.all");
                case "EVENT": return Lang.T("controls.log.event");
                case "WARNING": return Lang.T("controls.log.warning");
                case "ALARM": return Lang.T("controls.log.alarm");
                case "DATA": return Lang.T("controls.log.data");
                case "WORK": return Lang.T("controls.log.work");
                case "INPUT SEQ": return Lang.T("controls.log.inputSequence");
                case "FRONTHEAD SEQ": return Lang.T("controls.log.frontSequence");
                case "REARHEAD SEQ": return Lang.T("controls.log.rearSequence");
                case "OUTPUT SEQ": return Lang.T("controls.log.outputSequence");
                case "OK IMAGE": return Lang.T("controls.log.goodImage");
                case "NG IMAGE": return Lang.T("controls.log.ngImage");
                default: return value;
            }
        }

        /// <summary>
        /// 진단 상세 로그(DiagnosticVerbose) 토글 UI를 상단에 코드로 구성한다.
        /// 기존 조건: LogPolicy.EnableDiagnosticVerbose를 호출하는 UI가 없어 Motion/Main 등
        ///           정상 상세 로그가 디스크에 저장되지 않았고(최소 모드), 2026-07-25 피커 폭주
        ///           사고에서 모션 로그가 하나도 남지 않아 원인 확정이 불가능했다.
        /// 현재 기준: LOG SETTINGS에서 지속시간(분)을 지정해 즉시 켜고 끌 수 있다. 제한 시간이
        ///           지나면 LogPolicy가 스스로 최소 모드로 복귀한다(과다 로그 방지 설계 유지).
        ///           런타임 모드이므로 AppSettings 저장(SAVE) 대상이 아니다.
        /// </summary>
        private void BuildDiagnosticVerboseUi()
        {
            var grpDiag = new GroupBox
            {
                Text = "DIAGNOSTIC LOG (진단 상세 로그 — 즉시 적용)",
                Dock = DockStyle.Top,
                Height = 86,
                Padding = new Padding(8, 4, 8, 4)
            };

            Lang.BindKey(grpDiag, "controls.log.diagnostic");

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));                // 시간 라벨
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));           // 분 입력
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));          // ENABLE
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));          // DISABLE
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));           // 여백
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            _lblDiagStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold)
            };
            layout.SetColumnSpan(_lblDiagStatus, 5);
            layout.Controls.Add(_lblDiagStatus, 0, 0);

            var lblMinutes = new Label
            {
                Text = "지속 시간(분)",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            Lang.BindKey(lblMinutes, "controls.log.minutes");
            layout.Controls.Add(lblMinutes, 0, 1);

            _nDiagMinutes = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 1440,
                Value = 240,
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 4, 8, 4)
            };
            layout.Controls.Add(_nDiagMinutes, 1, 1);

            _btnDiagEnable = new Button
            {
                Text = "ENABLE",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold),
                UseVisualStyleBackColor = true,
                Cursor = Cursors.Hand
            };
            Lang.BindKey(_btnDiagEnable, "controls.log.enable");
            _btnDiagEnable.Click += (s, e) =>
            {
                LogPolicy.EnableDiagnosticVerbose(
                    (int)_nDiagMinutes.Value,
                    QMC.CDT_320.Ui.Security.UserSession.Name);
                UpdateDiagnosticVerboseStatus();
            };
            layout.Controls.Add(_btnDiagEnable, 2, 1);

            _btnDiagDisable = new Button
            {
                Text = "DISABLE",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold),
                UseVisualStyleBackColor = true,
                Cursor = Cursors.Hand
            };
            Lang.BindKey(_btnDiagDisable, "controls.log.disable");
            _btnDiagDisable.Click += (s, e) =>
            {
                LogPolicy.DisableDiagnosticVerbose(QMC.CDT_320.Ui.Security.UserSession.Name);
                UpdateDiagnosticVerboseStatus();
            };
            layout.Controls.Add(_btnDiagDisable, 3, 1);

            grpDiag.Controls.Add(layout);
            Controls.Add(grpDiag);
            // Fill(rootLayout)이 Top 그룹을 제외한 나머지 영역을 차지하도록 z-order 보정.
            rootLayout.BringToFront();

            // 남은 시간 갱신(1초). 만료 자동 복귀도 이 갱신으로 UI에 반영된다.
            _diagStatusTimer = new Timer { Interval = 1000 };
            _diagStatusTimer.Tick += (s, e) => UpdateDiagnosticVerboseStatus();
            _diagStatusTimer.Start();
            Disposed += (s, e) =>
            {
                _diagStatusTimer.Stop();
                _diagStatusTimer.Dispose();
            };

            UpdateDiagnosticVerboseStatus();
        }

        private void UpdateDiagnosticVerboseStatus()
        {
            // 시작 자동 활성은 만료가 없으므로 남은 시간 대신 무기한으로 표시한다. DISABLE로 해제할 수 있다.
            if (LogPolicy.IsDiagnosticVerboseUnlimited)
            {
                Lang.BindKey(_lblDiagStatus, "controls.log.diagnosticUnlimited");
                _lblDiagStatus.ForeColor = System.Drawing.Color.FromArgb(230, 88, 31);
                _btnDiagEnable.Enabled = false;
                _btnDiagDisable.Enabled = true;
                _nDiagMinutes.Enabled = false;
                return;
            }

            bool on = LogPolicy.Mode == LogMode.DiagnosticVerbose;
            if (on)
            {
                TimeSpan remain = LogPolicy.DiagnosticVerboseRemaining;
                Lang.BindFormat(_lblDiagStatus, "controls.log.diagnosticRemaining", (int)remain.TotalMinutes, remain.Seconds);
                _lblDiagStatus.ForeColor = System.Drawing.Color.FromArgb(230, 88, 31);
            }
            else
            {
                Lang.BindKey(_lblDiagStatus, "controls.log.diagnosticMinimal");
                _lblDiagStatus.ForeColor = System.Drawing.Color.FromArgb(35, 45, 57);
            }

            _btnDiagEnable.Enabled = !on;
            _btnDiagDisable.Enabled = on;
            _nDiagMinutes.Enabled = !on;
        }

        /// <summary>두 그리드의 헤더 클릭 정렬(오름/내림차순) 제거.</summary>
        private void ConfigureGrids()
        {
            foreach (DataGridViewColumn col in gridLogPaths.Columns)
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            foreach (DataGridViewColumn col in gridVisionImages.Columns)
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        /// <summary>경로 행을 채운다. "ALL LOG"=전체모드 폴더, 나머지=종류별 폴더.
        /// 저장값(AppSettings) 우선, 없으면 기본 경로. (모드와 무관하게 두 세트를 다 표시)</summary>
        private void BuildLogPathRows()
        {
            var cfg = AppSettingsStore.Current;
            gridLogPaths.Rows.Clear();
            foreach (string type in LogTypes)
            {
                EventKind? kind = MapLogTypeToKind(type);
                string dir;
                if (kind == null)
                {
                    dir = !string.IsNullOrWhiteSpace(cfg.LogAllDir) ? cfg.LogAllDir : DefaultAllDir();
                }
                else
                {
                    string ov = null;
                    if (cfg.LogKindPaths != null)
                        cfg.LogKindPaths.TryGetValue(kind.Value.ToString(), out ov);
                    dir = !string.IsNullOrWhiteSpace(ov) ? ov : DefaultKindDir(kind.Value);
                }
                gridLogPaths.Rows.Add(type, dir, "...");
            }
        }

        /// <summary>설정창 표기명 → EventKind. "ALL LOG"(루트)는 null 을 돌려준다.</summary>
        private static EventKind? MapLogTypeToKind(string type)
        {
            switch (type)
            {
                case "EVENT":         return EventKind.Event;
                case "WARNING":       return EventKind.Warning;
                case "ALARM":         return EventKind.Alarm;
                case "DATA":          return EventKind.Data;
                case "WORK":          return EventKind.Work;
                case "INPUT SEQ":     return EventKind.InputSeq;
                case "FRONTHEAD SEQ": return EventKind.FrontHeadSeq;
                case "REARHEAD SEQ":  return EventKind.RearHeadSeq;
                case "OUTPUT SEQ":    return EventKind.OutputSeq;
                default:              return null;   // "ALL LOG"
            }
        }

        private static string DefaultKindDir(EventKind kind)
        {
            try { return Path.Combine(EventLogger.LogRoot, EventLogger.KindFolderName(kind)); }
            catch { return string.Empty; }
        }

        private void BuildVisionRows()
        {
            gridVisionImages.Rows.Clear();
            foreach (string type in VisionImages)
                gridVisionImages.Rows.Add(type, "", "...");
        }

        private static string DefaultAllDir()
        {
            try { return Path.Combine(EventLogger.LogRoot, "Event"); }   // 전체 모드 기본 폴더(기존 위치)
            catch { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "Event"); }
        }

        private void WireEvents()
        {
            // 압축(사용/일수)이 바뀌면 삭제 사용가능 여부 + 최소값(=압축 일수) 제약을 다시 적용한다.
            _cbLogCompress.SelectedIndexChanged += (s, e) => { ApplyCompressState(); ApplyDeleteEnableGate(); ApplyDeleteState(); };
            _nCompressDays.ValueChanged += (s, e) => ApplyDeleteState();
            _cbLogDelete.SelectedIndexChanged += (s, e) => ApplyDeleteState();
            _cbPathMode.SelectedIndexChanged += (s, e) => ApplyPathModeUi();
            // 잠긴(비활성) 행의 "..." 는 무시한다.
            gridLogPaths.CellContentClick += (s, e) =>
            {
                if (IsLogRowEditable(e.RowIndex))
                    GridBrowse(gridLogPaths, e, colLogBrowse.Index, colLogPath.Index);
            };
            gridVisionImages.CellContentClick += (s, e) => GridBrowse(gridVisionImages, e, colVisBrowse.Index, colVisPath.Index);
            _chkInspectionDetail.CheckedChanged += (s, e) => ApplyInspectionDetailUi();
        }

        /// <summary>측정값 상세 저장 버튼(체크박스 버튼형)의 표시 상태를 갱신한다.</summary>
        private void ApplyInspectionDetailUi()
        {
            bool on = _chkInspectionDetail.Checked;
            Lang.BindKey(_chkInspectionDetail, on ? "controls.log.detailOn" : "controls.log.detailOff");
            _chkInspectionDetail.BackColor = on
                ? System.Drawing.Color.FromArgb(230, 88, 31)
                : System.Drawing.Color.FromArgb(238, 238, 238);
            _chkInspectionDetail.ForeColor = on
                ? System.Drawing.Color.White
                : System.Drawing.Color.FromArgb(60, 60, 60);
        }

        /// <summary>현재 설정값을 UI에 로드한다.</summary>
        private void LoadSettings()
        {
            var cfg = AppSettingsStore.Current;

            _cbLogHistory.SelectedIndex = cfg.FileLogHistoryEnabled ? 0 : 1;

            _lastCompressDays = Clamp(cfg.LogCompressDays, 1, 365);
            _cbLogCompress.SelectedIndex = cfg.LogCompressEnabled ? 0 : 1;

            if (cfg.ArchiveKeepDays > 0)
                _lastDeleteDays = Clamp(cfg.ArchiveKeepDays, 1, 365);
            _cbLogDelete.SelectedIndex = cfg.ArchiveKeepDays > 0 ? 0 : 1;

            _cbImageFormat.SelectedIndex = 0;

            // Log save mode — 0=ALL (single folder), 1=KIND (per type)
            _cbPathMode.Items.Clear();
            _cbPathMode.Items.Add("ALL (single folder)");
            _cbPathMode.Items.Add("KIND (per type)");
            _cbPathMode.SelectedIndex = cfg.LogSplitByKind ? 1 : 0;

            _chkInspectionDetail.Checked = cfg.SaveMaterialInspectionDetail;
            ApplyInspectionDetailUi();

            ApplyCompressState();
            ApplyDeleteEnableGate();
            ApplyDeleteState();
            ApplyPathModeUi();
        }

        /// <summary>압축이 DISABLE이면 삭제도 강제 DISABLE + 잠금(압축 없이 삭제만 쓰는 조합 금지).
        /// 원본을 지우는 유일한 경로가 '압축'이라, 압축이 꺼지면 삭제만으론 원본 용량 관리가 안 되기 때문.</summary>
        private void ApplyDeleteEnableGate()
        {
            bool compressOn = _cbLogCompress.SelectedIndex == 0;
            if (!compressOn && _cbLogDelete.SelectedIndex != 1)
                _cbLogDelete.SelectedIndex = 1;   // DISABLE 로 강제
            _cbLogDelete.Enabled = compressOn;    // 압축 OFF면 삭제 토글 잠금
        }

        /// <summary>선택된 저장 방식에 따라 편집 가능한 경로 행을 정한다.
        /// 전체(ALL) 모드: "ALL LOG" 행만, 종류별(KIND) 모드: 종류별 9행만 편집 가능(나머지는 회색 잠금).</summary>
        private void ApplyPathModeUi()
        {
            bool splitByKind = _cbPathMode.SelectedIndex == 1;
            foreach (DataGridViewRow row in gridLogPaths.Rows)
            {
                if (row.IsNewRow)
                    continue;
                bool editable = IsRowEditable(row.Cells[colLogType.Index].Value as string, splitByKind);
                DataGridViewCell pathCell = row.Cells[colLogPath.Index];
                pathCell.ReadOnly = !editable;
                pathCell.Style.BackColor = editable ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(236, 236, 236);
                pathCell.Style.ForeColor = editable ? System.Drawing.Color.Black : System.Drawing.Color.Gray;
            }
        }

        private static bool IsRowEditable(string type, bool splitByKind)
        {
            // "ALL LOG"(kind==null)는 전체 모드에서, 종류 행은 종류별 모드에서 편집 가능.
            return MapLogTypeToKind(type) == null ? !splitByKind : splitByKind;
        }

        private bool IsLogRowEditable(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= gridLogPaths.Rows.Count)
                return false;
            return IsRowEditable(gridLogPaths.Rows[rowIndex].Cells[colLogType.Index].Value as string,
                                 _cbPathMode.SelectedIndex == 1);
        }

        /// <summary>다이얼로그 SAVE 시 호출 — 설정값을 AppSettings 에 반영·저장한다.</summary>
        public void Save()
        {
            var cfg = AppSettingsStore.Current;

            cfg.FileLogHistoryEnabled = _cbLogHistory.SelectedIndex == 0;

            // material_state.json 에 검사 측정값 상세를 포함할지 여부(기본 OFF).
            cfg.SaveMaterialInspectionDetail = _chkInspectionDetail.Checked;

            cfg.LogCompressEnabled = _cbLogCompress.SelectedIndex == 0;
            cfg.LogCompressDays = _cbLogCompress.SelectedIndex == 0 ? (int)_nCompressDays.Value : _lastCompressDays;

            // 압축된 로그파일 삭제 → ArchiveKeepDays (0 = 삭제 안 함/무기한 보관).
            // 압축 OFF면 삭제 조합은 금지 → 무조건 0.
            cfg.ArchiveKeepDays = (_cbLogCompress.SelectedIndex == 0 && _cbLogDelete.SelectedIndex == 0)
                ? (int)_nDeleteDays.Value : 0;

            // 로그 저장 방식 + 경로 저장 + 즉시 적용.
            bool splitByKind = _cbPathMode.SelectedIndex == 1;
            string allDir = null;
            var kindPaths = new Dictionary<string, string>();
            foreach (DataGridViewRow row in gridLogPaths.Rows)
            {
                if (row.IsNewRow)
                    continue;
                string path = row.Cells[colLogPath.Index].Value as string;
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                EventKind? kind = MapLogTypeToKind(row.Cells[colLogType.Index].Value as string);
                if (kind == null)
                    allDir = path.Trim();                       // "ALL LOG" 행 = 전체 모드 폴더
                else
                    kindPaths[kind.Value.ToString()] = path.Trim();
            }
            cfg.LogSplitByKind = splitByKind;
            cfg.LogAllDir = allDir;
            cfg.LogKindPaths = kindPaths;
            EventLogger.ConfigureLogPathsByName(splitByKind, allDir, kindPaths);   // 다음 로그부터 새 경로로 저장

            AppSettingsStore.Save();
        }

        private void ApplyCompressState()
        {
            ApplyDaysState(_cbLogCompress, _nCompressDays, ref _lastCompressDays);
        }

        /// <summary>압축본 삭제 일수 상태. 삭제 기간은 압축 유예 기간보다 짧을 수 없다
        /// (압축되자마자 삭제되는 것 방지) → 삭제칸 최소값을 현재 압축 일수로 동적 제한한다.</summary>
        private void ApplyDeleteState()
        {
            if (_cbLogDelete.SelectedIndex == 0) // ENABLE
            {
                int min = _cbLogCompress.SelectedIndex == 0 ? Math.Max(1, (int)_nCompressDays.Value) : 1;
                _nDeleteDays.Enabled = true;
                _nDeleteDays.Maximum = 365;
                _nDeleteDays.Minimum = min;                       // 압축 일수보다 낮게 입력 불가
                _nDeleteDays.Value = Clamp(Math.Max(_lastDeleteDays, min), min, 365);
            }
            else // DISABLE
            {
                if (_nDeleteDays.Value >= 1)
                    _lastDeleteDays = (int)_nDeleteDays.Value;   // 마지막 설정값 기억
                _nDeleteDays.Enabled = false;
                _nDeleteDays.Minimum = 0;
                _nDeleteDays.Value = 0;
            }
        }

        /// <summary>토글이 DISABLE이면 일수칸을 0으로 고정·비활성(직전 ENABLE 값 기억),
        /// ENABLE이면 1~365 범위로 활성하며 기억해둔 값을 복원한다.</summary>
        private void ApplyDaysState(ComboBox toggle, NumericUpDown days, ref int lastValue)
        {
            if (toggle.SelectedIndex == 0) // ENABLE
            {
                days.Enabled = true;
                days.Minimum = 1;
                days.Maximum = 365;
                days.Value = Clamp(lastValue, 1, 365);
            }
            else // DISABLE
            {
                if (days.Value >= 1)
                    lastValue = (int)days.Value;   // 마지막 설정값 기억
                days.Enabled = false;
                days.Minimum = 0;
                days.Value = 0;
            }
        }

        private void GridBrowse(DataGridView grid, DataGridViewCellEventArgs e, int browseColIndex, int pathColIndex)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != browseColIndex)
                return;

            var cell = grid.Rows[e.RowIndex].Cells[pathColIndex];
            string picked = BrowseFolder(cell.Value as string);
            if (picked != null)
                cell.Value = picked;
        }

        /// <summary>폴더 선택 창을 열고 선택 경로를 반환(취소 시 null).</summary>
        private string BrowseFolder(string current)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = Lang.T("controls.log.selectPath");
                if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
                    dlg.SelectedPath = current;
                return dlg.ShowDialog(this) == DialogResult.OK ? dlg.SelectedPath : null;
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
