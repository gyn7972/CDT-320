using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.Common.Recipes;
using QMC.Vision.Comm;
using QMC.Vision.Config;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// Stage 69 — 검사별 조명 값 편집 패널 (Recipe).
    /// 결선(Controller/Page/풀)은 Setup 의 AlgorithmLightWiring 에서 추론(읽기 전용 헤더).
    /// 값 편집 표는 풀 내 채널만. Save = algorithm_camera.json 통합, Apply = LightHub 라이브.
    /// </summary>
    public class InspectionLightPanel : UserControl
    {
        private Label    _lblHeader;
        private Label    _lblWiring;
        private DataGridView _grid;
        private Button   _btnSave, _btnApply, _btnReset, _btnCancel, _btnAllOn, _btnAllOff;
        private Label    _lblStatus;

        private string _algorithm, _inspectionId;

        public InspectionLightPanel()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildLayout();
        }

        public void SelectInspection(string algorithm, string inspectionId)
        {
            _algorithm = algorithm;
            _inspectionId = inspectionId;
            _lblHeader.Text = "검사 조명 — " + InspectionLabel.Get(algorithm, inspectionId)
                            + "  (" + VisionAlgorithm.Label(algorithm) + " / " + inspectionId + ")";
            BindFields();
        }

        private void BuildLayout()
        {
            BackColor = UiTheme.MainBg;

            _lblHeader = new Label
            {
                Dock = DockStyle.Top, Height = 30, Text = "검사 조명",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(_lblHeader);

            _lblWiring = new Label
            {
                Dock = DockStyle.Top, Height = 48, Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray,
                BackColor = Color.WhiteSmoke, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(_lblWiring);

            var bar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = UiTheme.MainBg };
            _btnSave   = MakeBtn("저장", 8,   UiTheme.Accent, Color.White); _btnSave.Click   += (s, e) => Save();
            _btnApply  = MakeBtn("실행 적용", 118, Color.White, Color.Black); _btnApply.Width = 100; _btnApply.Click += (s, e) => Apply();
            _btnReset  = MakeBtn("초기화", 228, Color.White, Color.Black); _btnReset.Click  += (s, e) => ResetRows();
            _btnCancel = MakeBtn("취소", 338, Color.White, Color.Black); _btnCancel.Click += (s, e) => BindFields();
            _btnAllOn  = MakeBtn("All On", 448, Color.White, Color.Black); _btnAllOn.Click += (s, e) => SetAll(true);
            _btnAllOff = MakeBtn("All Off", 558, Color.White, Color.Black); _btnAllOff.Click += (s, e) => SetAll(false);
            bar.Controls.AddRange(new Control[] { _btnSave, _btnApply, _btnReset, _btnCancel, _btnAllOn, _btnAllOff });
            Controls.Add(bar);

            _lblStatus = new Label { Dock = DockStyle.Bottom, Height = 24, Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray, Padding = new Padding(8, 2, 0, 0) };
            Controls.Add(_lblStatus);

            _grid = new DataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false, Font = UiTheme.ValueFont, BackgroundColor = Color.White, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = true };
            _grid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Channel", HeaderText = "Channel(풀)" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Level",   HeaderText = "Level(0~Max)" });
            _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "On",      HeaderText = "On" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Strobe",  HeaderText = "Strobe(us)" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Stab",    HeaderText = "안정화(ms)" });
            Controls.Add(_grid);
            Controls.SetChildIndex(_grid, 0);
        }

        // ── Setup 결선 조회 ──
        private AlgorithmLightWiring Wiring()
            => LightSystemSetupStore.Current?.GetWiring(_algorithm);

        private void BindFields()
        {
            var w = Wiring();
            var pool = w?.Channels ?? new System.Collections.Generic.List<int>();
            bool wired = w != null && !string.IsNullOrEmpty(w.ControllerPort) && pool.Count > 0;

            // 결선 헤더
            if (w == null || string.IsNullOrEmpty(w.ControllerPort))
                _lblWiring.Text = "결선 없음 — [설정 > 조명 시스템]에서 이 알고리즘에 컨트롤러/채널을 배정하세요.";
            else if (pool.Count == 0)
                _lblWiring.Text = $"결선: {w.ControllerPort} / Page {w.Page} / 사용 채널 풀 비어 있음 (조명 미사용)";
            else
                _lblWiring.Text = $"소속 알고리즘: {VisionAlgorithm.Label(_algorithm)}   결선: {w.ControllerPort} / Page {w.Page} / 풀: [{string.Join(",", pool)}]";

            // 채널 콤보 = 풀만
            var chCol = (DataGridViewComboBoxColumn)_grid.Columns["Channel"];
            chCol.Items.Clear();
            foreach (var ch in pool) chCol.Items.Add(ch);

            _grid.Rows.Clear();
            var ov = AlgorithmCameraMapStore.Current?.Get(_algorithm)?.GetLightOverride(_inspectionId);
            if (ov?.Settings != null)
                foreach (var s in ov.Settings)
                    if (pool.Contains(s.Channel))   // 풀 밖 설정은 표시 안 함
                        _grid.Rows.Add(s.Channel, s.Level, s.On, s.StrobeTimeUs, s.StabilizeDelayMs);

            _grid.Enabled = wired;
            _btnApply.Enabled = _btnSave.Enabled = wired;
            SetStatus(wired ? "" : "결선 미설정 — 조명 미사용", !wired);
        }

        /// <summary>UI → InspectionLightOverride 수집 (풀 검증 포함).</summary>
        private InspectionLightOverride Collect(out int outOfPool)
        {
            outOfPool = 0;
            var pool = Wiring()?.Channels ?? new System.Collections.Generic.List<int>();
            var ov = new InspectionLightOverride { InspectionId = _inspectionId };
            foreach (DataGridViewRow r in _grid.Rows)
            {
                if (r.IsNewRow) continue;
                if (!int.TryParse(r.Cells["Channel"].Value?.ToString(), out int ch)) continue;
                if (!pool.Contains(ch)) { outOfPool++; continue; }
                ov.Settings.Add(new InspectionLightSetting
                {
                    Channel = ch,
                    Level = IntOf(r, "Level", 0),
                    On = BoolOf(r, "On"),
                    StrobeTimeUs = IntOf(r, "Strobe", 0),
                    StabilizeDelayMs = IntOf(r, "Stab", 0)
                });
            }
            return ov;
        }

        private void Save()
        {
            var bm = AlgorithmCameraMapStore.Current?.Get(_algorithm);
            if (bm == null) { SetStatus("알고리즘 매핑 없음", true); return; }
            var ov = Collect(out int oop);
            // upsert
            var existing = bm.GetOrCreateLightOverride(_inspectionId);
            existing.Settings = ov.Settings;
            if (existing.IsEmpty()) bm.InspectionLights?.RemoveAll(o => string.Equals(o.InspectionId, _inspectionId, StringComparison.OrdinalIgnoreCase));
            AlgorithmCameraMapStore.Save();
            SetStatus($"저장 완료{(oop > 0 ? $" (풀 밖 {oop}건 무시)" : "")}", false);
        }

        private void Apply()
        {
            var w = Wiring();
            if (w == null || string.IsNullOrEmpty(w.ControllerPort))
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-WIRING-MISS", "Light/" + _algorithm, $"{_algorithm} 결선 누락");
                SetStatus("적용 거부 — 결선 누락", true); return;
            }
            var ctrl = LightHub.Get(w.ControllerPort);
            if (ctrl == null)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-MAP-INVALID", "Light/" + _algorithm, $"{w.ControllerPort} not in LightHub");
                SetStatus("적용 거부 — 컨트롤러 미등록: " + w.ControllerPort, true); return;
            }
            var ov = Collect(out _);
            try
            {
                if (ctrl.ChannelCount >= 0) ctrl.SwitchPageAsync(w.Page);
                int applied = 0;
                foreach (var s in ov.Settings)
                {
                    if (!w.Channels.Contains(s.Channel))
                    {
                        AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-CHANNEL-OUT-OF-POOL", "Light/" + _algorithm, $"ch{s.Channel} not in pool");
                        continue;
                    }
                    ctrl.SetPowerAsync(s.Channel, s.Level);
                    ctrl.SetOnOffAsync(s.Channel, s.On);
                    if (s.StrobeTimeUs > 0) ctrl.SetStrobeTimeAsync(s.Channel, s.StrobeTimeUs);
                    applied++;
                }
                SetStatus($"적용 완료 — {applied}채널 ({w.ControllerPort})", false);
            }
            catch (Exception ex) { SetStatus("적용 예외: " + ex.Message, true); }
        }

        private void ResetRows() { _grid.Rows.Clear(); SetStatus("행 초기화 — 저장 시 조명 미사용", false); }

        private void SetAll(bool on)
        {
            foreach (DataGridViewRow r in _grid.Rows) if (!r.IsNewRow) r.Cells["On"].Value = on;
        }

        private static Button MakeBtn(string t, int x, Color bg, Color fg)
            => new Button { Location = new Point(x, 4), Size = new Size(90, 32), Text = t, FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = bg, ForeColor = fg };
        private static int IntOf(DataGridViewRow r, string col, int def) => int.TryParse(r.Cells[col].Value?.ToString(), out var v) ? v : def;
        private static bool BoolOf(DataGridViewRow r, string col) { var v = r.Cells[col].Value; return v is bool b ? b : (v?.ToString() == "True"); }
        private void SetStatus(string msg, bool err) { _lblStatus.ForeColor = err ? Color.Firebrick : Color.DarkSlateGray; _lblStatus.Text = msg; }
    }
}
