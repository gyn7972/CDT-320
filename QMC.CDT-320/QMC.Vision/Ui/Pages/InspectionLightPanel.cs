using System;
using System.Collections.Generic;
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
    /// 결선(Controller/풀)은 Setup 의 AlgorithmLightWiring 에서 추론(읽기 전용 헤더).
    /// <para>Stage 72 — 채널 선택 콤보 제거. 결선 채널 풀에 따라 행을 <b>자동 생성</b>하고
    /// 사용자는 <b>Level / Page</b> 만 수정. Channel/이름(Setup 라벨)은 읽기 전용.
    /// On 은 Level&gt;0 으로 파생, Strobe/안정화 값은 기존 저장값을 보존(이 패널에선 미편집).</para>
    /// Save = algorithm_camera.json 통합, Apply = LightHub 라이브.
    /// </summary>
    public class InspectionLightPanel : UserControl
    {
        private Label    _lblHeader;
        private Label    _lblWiring;
        private DataGridView _grid;
        private Button   _btnSave, _btnApply, _btnReset, _btnCancel;
        private Label    _lblStatus;

        private string _algorithm, _inspectionId;
        private int    _maxPower = 240;
        // Stage 72 — 채널별 보존값(Strobe/안정화/On) — 이 패널에서 편집하지 않지만 유실 방지.
        private readonly Dictionary<int, InspectionLightSetting> _carry = new Dictionary<int, InspectionLightSetting>();

        public InspectionLightPanel()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildLayout();
        }

        /// <summary>Stage 70 E — FinderPage/InspectorPage 가 생성 즉시 검사 컨텍스트 주입.</summary>
        public InspectionLightPanel(string algorithm, string inspectionId)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildLayout();
            SelectInspection(algorithm, inspectionId);
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
                Dock = DockStyle.Top, Height = 44, Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray,
                BackColor = Color.WhiteSmoke, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(_lblWiring);

            var bar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = UiTheme.MainBg };
            _btnSave   = MakeBtn("저장", 8,   UiTheme.Accent, Color.White); _btnSave.Click   += (s, e) => Save();
            _btnApply  = MakeBtn("실행 적용", 104, Color.White, Color.Black); _btnApply.Click += (s, e) => Apply();
            _btnReset  = MakeBtn("초기화", 200, Color.White, Color.Black); _btnReset.Click  += (s, e) => ResetLevels();
            _btnCancel = MakeBtn("취소", 296, Color.White, Color.Black); _btnCancel.Click += (s, e) => BindFields();
            bar.Controls.AddRange(new Control[] { _btnSave, _btnApply, _btnReset, _btnCancel });
            Controls.Add(bar);

            _lblStatus = new Label { Dock = DockStyle.Bottom, Height = 24, Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray, Padding = new Padding(8, 2, 0, 0) };
            Controls.Add(_lblStatus);

            // Stage 72 — 채널은 결선 풀에서 자동 생성 → 사용자 행 추가 금지. Level/Page 만 편집.
            _grid = new DataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false, Font = UiTheme.ValueFont, BackgroundColor = Color.White, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false };
            var colCh   = new DataGridViewTextBoxColumn  { Name = "Channel", HeaderText = "Ch",          ReadOnly = true, FillWeight = 14 };
            var colName = new DataGridViewTextBoxColumn  { Name = "Name",    HeaderText = "이름",        ReadOnly = true, FillWeight = 40 };
            var colLvl  = new DataGridViewTextBoxColumn  { Name = "Level",   HeaderText = "Level(0~Max)", FillWeight = 26 };
            var colPg   = new DataGridViewComboBoxColumn { Name = "Page",    HeaderText = "Page",         FillWeight = 20 };
            _grid.Columns.Add(colCh);
            _grid.Columns.Add(colName);
            _grid.Columns.Add(colLvl);
            _grid.Columns.Add(colPg);
            // 읽기 전용 셀 시각 구분
            colCh.DefaultCellStyle.BackColor   = Color.FromArgb(0xF2, 0xF2, 0xF2);
            colName.DefaultCellStyle.BackColor = Color.FromArgb(0xF2, 0xF2, 0xF2);
            _grid.DataError += (s, e) => { e.ThrowException = false; };
            Controls.Add(_grid);
            Controls.SetChildIndex(_grid, 0);
        }

        // ── Setup 결선 조회 ──
        private AlgorithmLightWiring Wiring()
            => LightSystemSetupStore.Current?.GetWiring(_algorithm);

        private void BindFields()
        {
            _carry.Clear();
            var w = Wiring();
            var pool = w?.Channels ?? new List<int>();
            bool wired = w != null && !string.IsNullOrEmpty(w.ControllerPort) && pool.Count > 0;

            // 컨트롤러 능력(PageCount/MaxPower) + 채널 라벨 조회
            LightControllerEntry ctrl = (w != null && !string.IsNullOrEmpty(w.ControllerPort))
                ? LightSystemSetupStore.Current?.GetController(w.ControllerPort) : null;
            int pageCount = (ctrl != null && ctrl.PageCount > 0) ? ctrl.PageCount : 1;
            _maxPower = (ctrl != null && ctrl.MaxPower > 0) ? ctrl.MaxPower : 240;

            // 결선 헤더
            if (w == null || string.IsNullOrEmpty(w.ControllerPort))
                _lblWiring.Text = "결선 없음 — [설정 > 조명 시스템]에서 이 알고리즘에 컨트롤러/채널을 배정하세요.";
            else if (pool.Count == 0)
                _lblWiring.Text = $"결선: {w.ControllerPort} / 사용 채널 풀 비어 있음 (조명 미사용)";
            else
                _lblWiring.Text = $"결선: {w.ControllerPort} / 풀: [{string.Join(",", pool)}]   (Level 0 = OFF, MaxPwr {_maxPower}, Page 0~{pageCount - 1})";

            // Page 콤보 = 0 ~ PageCount-1 (컨트롤러 능력)
            var pgCol = (DataGridViewComboBoxColumn)_grid.Columns["Page"];
            pgCol.Items.Clear();
            for (int p = 0; p < pageCount; p++) pgCol.Items.Add(p);
            pgCol.ReadOnly = pageCount <= 1;

            // 기존 저장 설정(채널→설정) 로드 — Level/Page 표시 + Strobe/Stab/On 보존
            var saved = new Dictionary<int, InspectionLightSetting>();
            var ov = AlgorithmCameraMapStore.Current?.Get(_algorithm)?.GetLightOverride(_inspectionId);
            if (ov?.Settings != null)
                foreach (var s in ov.Settings)
                    if (!saved.ContainsKey(s.Channel)) saved[s.Channel] = s;

            // 채널별 라벨 이름
            var labelByCh = new Dictionary<int, string>();
            if (ctrl?.ChannelLabels != null)
                foreach (var l in ctrl.ChannelLabels)
                    if (!labelByCh.ContainsKey(l.Channel)) labelByCh[l.Channel] = l.Name ?? "";

            // 풀 채널 순서대로 1행씩 자동 생성
            _grid.Rows.Clear();
            foreach (var ch in pool)
            {
                saved.TryGetValue(ch, out var s);
                int level = s?.Level ?? 0;
                int page  = s != null ? s.Page : 0;
                if (page < 0) page = 0; if (page > pageCount - 1) page = pageCount - 1;
                labelByCh.TryGetValue(ch, out var nm);
                int idx = _grid.Rows.Add(ch, nm ?? "", level, page);
                _grid.Rows[idx].Cells["Page"].Value = page;   // 콤보 초기값
                // 보존값 기억 (없으면 기본값)
                _carry[ch] = new InspectionLightSetting
                {
                    Channel = ch,
                    StrobeTimeUs     = s?.StrobeTimeUs ?? 0,
                    StabilizeDelayMs = s?.StabilizeDelayMs ?? 0
                };
            }

            _grid.Enabled = wired;
            _btnApply.Enabled = _btnSave.Enabled = _btnReset.Enabled = wired;
            SetStatus(wired ? "" : "결선 미설정 — 조명 미사용", !wired);
        }

        /// <summary>UI → InspectionLightOverride 수집. 행 = 결선 풀 채널. On=Level&gt;0 파생, Strobe/Stab 보존.</summary>
        private InspectionLightOverride Collect()
        {
            var ov = new InspectionLightOverride { InspectionId = _inspectionId };
            foreach (DataGridViewRow r in _grid.Rows)
            {
                if (r.IsNewRow) continue;
                if (!int.TryParse(r.Cells["Channel"].Value?.ToString(), out int ch)) continue;
                int level = IntOf(r, "Level", 0);
                if (level < 0) level = 0; if (level > _maxPower) level = _maxPower;
                int page = IntOf(r, "Page", 0);
                _carry.TryGetValue(ch, out var keep);
                ov.Settings.Add(new InspectionLightSetting
                {
                    Channel = ch,
                    Level = level,
                    On = level > 0,                                   // Stage 72 — Level 0 = OFF
                    StrobeTimeUs = keep?.StrobeTimeUs ?? 0,           // 보존
                    StabilizeDelayMs = keep?.StabilizeDelayMs ?? 0,   // 보존
                    Page = page
                });
            }
            return ov;
        }

        private void Save()
        {
            var bm = AlgorithmCameraMapStore.Current?.Get(_algorithm);
            if (bm == null) { SetStatus("알고리즘 매핑 없음", true); return; }
            var ov = Collect();
            // OFF(Level 0) 이면서 Strobe/Stab 도 기본값인 채널은 직렬화에서 제외 (JSON 정리)
            ov.Settings.RemoveAll(s => s.Level <= 0 && s.StrobeTimeUs <= 0 && s.StabilizeDelayMs <= 0);
            var existing = bm.GetOrCreateLightOverride(_inspectionId);
            existing.Settings = ov.Settings;
            if (existing.IsEmpty()) bm.InspectionLights?.RemoveAll(o => string.Equals(o.InspectionId, _inspectionId, StringComparison.OrdinalIgnoreCase));
            AlgorithmCameraMapStore.Save();
            int on = ov.Settings.Count(s => s.Level > 0);
            SetStatus($"저장 완료 — 점등 {on}채널", false);
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
            var ov = Collect();   // 풀 전체 (Level 0 포함) → 미사용 채널도 명시적 OFF
            try
            {
                int applied = 0;
                // Page 별로 그룹핑하여 페이지 전환 회수 최소화.
                foreach (var grp in ov.Settings.GroupBy(s => s.Page).OrderBy(g => g.Key))
                {
                    ctrl.SwitchPageAsync(grp.Key);
                    foreach (var s in grp)
                    {
                        if (!w.Channels.Contains(s.Channel))
                        {
                            AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-CHANNEL-OUT-OF-POOL", "Light/" + _algorithm, $"ch{s.Channel} not in pool");
                            continue;
                        }
                        ctrl.SetPowerAsync(s.Channel, s.Level);
                        ctrl.SetOnOffAsync(s.Channel, s.On);
                        if (s.StrobeTimeUs > 0) ctrl.SetStrobeTimeAsync(s.Channel, s.StrobeTimeUs);
                        if (s.Level > 0) applied++;
                    }
                }
                SetStatus($"적용 완료 — 점등 {applied}채널 ({w.ControllerPort})", false);
            }
            catch (Exception ex) { SetStatus("적용 예외: " + ex.Message, true); }
        }

        /// <summary>Stage 72 — 모든 행 Level 0 / Page 0 으로 초기화 (행은 유지).</summary>
        private void ResetLevels()
        {
            foreach (DataGridViewRow r in _grid.Rows)
            {
                if (r.IsNewRow) continue;
                r.Cells["Level"].Value = 0;
                if (!_grid.Columns["Page"].ReadOnly) r.Cells["Page"].Value = 0;
            }
            SetStatus("초기화 — 모든 채널 Level 0 (저장 시 조명 미사용)", false);
        }

        private static Button MakeBtn(string t, int x, Color bg, Color fg)
            => new Button { Location = new Point(x, 4), Size = new Size(90, 32), Text = t, FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = bg, ForeColor = fg };
        private static int IntOf(DataGridViewRow r, string col, int def) => int.TryParse(r.Cells[col].Value?.ToString(), out var v) ? v : def;
        private void SetStatus(string msg, bool err) { _lblStatus.ForeColor = err ? Color.Firebrick : Color.DarkSlateGray; _lblStatus.Text = msg; }
    }
}
