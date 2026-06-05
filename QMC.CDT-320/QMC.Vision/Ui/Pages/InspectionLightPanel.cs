using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.Common.Recipes;
using QMC.Vision.Comm;
using QMC.Vision.Config;
using QMC.Vision.Optics;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 검사별 조명 값 편집 패널 (Recipe).
    /// <para>Stage 81 — 다중 컨트롤러 결선 지원. 결선(ControllerSets/풀)은 Setup 에서 추론(읽기전용 헤더).
    /// 행 = (Controller, Channel) 자동 생성. 사용자는 Level/Page 만 편집. Apply 는 컨트롤러별 batch 를 Task.WhenAll 병렬.</para>
    /// </summary>
    public class InspectionLightPanel : UserControl
    {
        private Label    _lblHeader;
        private Label    _lblWiring;
        private DataGridView _grid;
        private Button   _btnSave, _btnApply, _btnReset, _btnCancel;
        private Label    _lblStatus;

        private string _algorithm, _inspectionId;
        // Stage 81 — 컨트롤러별 MaxPower / (port,ch) 보존값.
        private readonly Dictionary<string, int> _maxPowerByPort = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, InspectionLightSetting> _carry = new Dictionary<string, InspectionLightSetting>();

        public InspectionLightPanel()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildLayout();
        }

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
                Dock = DockStyle.Top, Height = 48, Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray,
                BackColor = Color.WhiteSmoke, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 2, 0, 0)
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

            _grid = new DataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false, Font = UiTheme.ValueFont, BackgroundColor = Color.White, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false };
            var colCtrl = new DataGridViewTextBoxColumn  { Name = "Ctrl",    HeaderText = "컨트롤러", ReadOnly = true, FillWeight = 24 };
            var colCh   = new DataGridViewTextBoxColumn  { Name = "Channel", HeaderText = "Ch",       ReadOnly = true, FillWeight = 10 };
            var colName = new DataGridViewTextBoxColumn  { Name = "Name",    HeaderText = "이름",     ReadOnly = true, FillWeight = 30 };
            var colLvl  = new DataGridViewTextBoxColumn  { Name = "Level",   HeaderText = "Level",    FillWeight = 18 };
            var colPg   = new DataGridViewComboBoxColumn { Name = "Page",    HeaderText = "Page",     FillWeight = 12 };
            _grid.Columns.Add(colCtrl);
            _grid.Columns.Add(colCh);
            _grid.Columns.Add(colName);
            _grid.Columns.Add(colLvl);
            _grid.Columns.Add(colPg);
            var ro = Color.FromArgb(0xF2, 0xF2, 0xF2);
            colCtrl.DefaultCellStyle.BackColor = ro;
            colCh.DefaultCellStyle.BackColor   = ro;
            colName.DefaultCellStyle.BackColor = ro;
            _grid.DataError += (s, e) => { e.ThrowException = false; };
            Controls.Add(_grid);
            Controls.SetChildIndex(_grid, 0);
        }

        // ── Setup 결선 조회 ──
        private AlgorithmLightWiring Wiring()
            => LightSystemSetupStore.Current?.GetWiring(_algorithm);

        private static string Key(string port, int ch) => (port ?? "") + "/" + ch;

        private void BindFields()
        {
            _carry.Clear();
            _maxPowerByPort.Clear();
            var w = Wiring();
            var sets = (w?.ControllerSets ?? new List<ControllerChannels>())
                       .Where(cs => !string.IsNullOrEmpty(cs.ControllerPort)).ToList();
            bool wired = w != null && w.IsWired;

            // 컨트롤러 능력(MaxPower/PageCount) 수집
            int maxPageCount = 1;
            foreach (var cs in sets)
            {
                var ce = LightSystemSetupStore.Current?.GetController(cs.ControllerPort);
                _maxPowerByPort[cs.ControllerPort] = (ce != null && ce.MaxPower > 0) ? ce.MaxPower : 240;
                if (ce != null && ce.PageCount > maxPageCount) maxPageCount = ce.PageCount;
            }

            // 결선 헤더 (다중)
            if (!wired)
                _lblWiring.Text = "결선 없음 — [설정 > 조명 시스템]에서 이 알고리즘에 컨트롤러/채널을 배정하세요.";
            else
                _lblWiring.Text = "결선: " + string.Join("    ", sets.Select(cs =>
                {
                    var ce = LightSystemSetupStore.Current?.GetController(cs.ControllerPort);
                    string nm = (ce != null && !string.IsNullOrEmpty(ce.Name)) ? $" ({ce.Name})" : "";
                    return $"{cs.ControllerPort}{nm} [{string.Join(",", cs.Channels)}]";
                })) + "   — 변경은 [설정 > 조명 시스템]";

            // 저장 설정 (port,ch) → 설정
            var saved = new Dictionary<string, InspectionLightSetting>(StringComparer.OrdinalIgnoreCase);
            var ov = AlgorithmCameraMapStore.Current?.Get(_algorithm)?.GetLightOverride(_inspectionId);
            if (ov?.Settings != null)
                foreach (var s in ov.Settings)
                {
                    string key = Key(s.ControllerPort, s.Channel);
                    if (!saved.ContainsKey(key)) saved[key] = s;
                }

            // Page 콤보 = 0 ~ maxPageCount-1
            var pgCol = (DataGridViewComboBoxColumn)_grid.Columns["Page"];
            pgCol.Items.Clear();
            for (int p = 0; p < maxPageCount; p++) pgCol.Items.Add(p);
            pgCol.ReadOnly = maxPageCount <= 1;

            // 행 자동 생성 — 각 ControllerSet 의 채널마다
            _grid.Rows.Clear();
            foreach (var cs in sets)
            {
                var ce = LightSystemSetupStore.Current?.GetController(cs.ControllerPort);
                var labelByCh = new Dictionary<int, string>();
                if (ce?.ChannelLabels != null)
                    foreach (var l in ce.ChannelLabels)
                        if (!labelByCh.ContainsKey(l.Channel)) labelByCh[l.Channel] = l.Name ?? "";
                string ctrlDisp = cs.ControllerPort + ((ce != null && !string.IsNullOrEmpty(ce.Name)) ? $" ({ce.Name})" : "");

                foreach (var ch in (cs.Channels ?? new List<int>()))
                {
                    string key = Key(cs.ControllerPort, ch);
                    saved.TryGetValue(key, out var s);
                    int level = s?.Level ?? 0;
                    int page  = s != null ? s.Page : 0;
                    if (page < 0) page = 0; if (page > maxPageCount - 1) page = maxPageCount - 1;
                    labelByCh.TryGetValue(ch, out var nm);
                    int idx = _grid.Rows.Add(ctrlDisp, ch, nm ?? "", level, page);
                    _grid.Rows[idx].Cells["Page"].Value = page;
                    _grid.Rows[idx].Tag = cs.ControllerPort;   // 저장용 실제 PortName
                    _carry[key] = new InspectionLightSetting
                    {
                        ControllerPort = cs.ControllerPort, Channel = ch,
                        StrobeTimeUs = s?.StrobeTimeUs ?? 0, StabilizeDelayMs = s?.StabilizeDelayMs ?? 0
                    };
                }
            }

            _grid.Enabled = wired;
            _btnApply.Enabled = _btnSave.Enabled = _btnReset.Enabled = wired;
            SetStatus(wired ? "" : "결선 미설정 — [설정 > 조명 시스템]에서 결선 후 사용", !wired);
        }

        /// <summary>UI → InspectionLightOverride. 행 = (Controller, Channel). On=Level&gt;0 파생, Strobe/Stab 보존.</summary>
        private InspectionLightOverride Collect()
        {
            var ov = new InspectionLightOverride { InspectionId = _inspectionId };
            foreach (DataGridViewRow r in _grid.Rows)
            {
                if (r.IsNewRow) continue;
                string port = r.Tag as string;
                if (!int.TryParse(r.Cells["Channel"].Value?.ToString(), out int ch)) continue;
                int maxP = (port != null && _maxPowerByPort.TryGetValue(port, out var mp)) ? mp : 240;
                int level = IntOf(r, "Level", 0);
                if (level < 0) level = 0; if (level > maxP) level = maxP;
                int page = IntOf(r, "Page", 0);
                _carry.TryGetValue(Key(port, ch), out var keep);
                ov.Settings.Add(new InspectionLightSetting
                {
                    ControllerPort = port, Channel = ch, Level = level, On = level > 0,
                    StrobeTimeUs = keep?.StrobeTimeUs ?? 0, StabilizeDelayMs = keep?.StabilizeDelayMs ?? 0, Page = page
                });
            }
            return ov;
        }

        private void Save()
        {
            var bm = AlgorithmCameraMapStore.Current?.Get(_algorithm);
            if (bm == null) { SetStatus("알고리즘 매핑 없음", true); return; }
            var ov = Collect();
            ov.Settings.RemoveAll(s => s.Level <= 0 && s.StrobeTimeUs <= 0 && s.StabilizeDelayMs <= 0);
            var existing = bm.GetOrCreateLightOverride(_inspectionId);
            existing.Settings = ov.Settings;
            if (existing.IsEmpty()) bm.InspectionLights?.RemoveAll(o => string.Equals(o.InspectionId, _inspectionId, StringComparison.OrdinalIgnoreCase));
            AlgorithmCameraMapStore.Save();
            SetStatus($"저장 완료 — 점등 {ov.Settings.Count(s => s.Level > 0)}채널", false);
        }

        private async void Apply()
        {
            var w = Wiring();
            if (w == null || !w.IsWired)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-WIRING-MISS", "Light/" + _algorithm, $"{_algorithm} 결선 누락");
                SetStatus("적용 거부 — 결선 누락", true); return;
            }
            var ov = Collect();
            try
            {
                // Stage 81 — 컨트롤러별 group → 각 컨트롤러 batch Task → Task.WhenAll 병렬(독립 시리얼 포트).
                var tasks = new List<Task<bool>>();
                var ports = new List<string>();
                foreach (var grp in ov.Settings.Where(s => !string.IsNullOrEmpty(s.ControllerPort)).GroupBy(s => s.ControllerPort))
                {
                    var ctrl = LightHub.Get(grp.Key);
                    if (ctrl == null)
                    {
                        AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-MAP-INVALID", "Light/" + _algorithm, $"{grp.Key} not in LightHub");
                        continue;
                    }
                    var cs = w.GetSet(grp.Key);
                    if (cs == null)
                    {
                        AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-WIRING-MISS", "Light/" + _algorithm, $"{grp.Key} 결선 누락");
                        continue;
                    }
                    ports.Add(grp.Key);
                    tasks.Add(ApplyControllerAsync(ctrl, cs, grp.ToList()));
                }
                if (tasks.Count == 0) { SetStatus("적용 거부 — 등록된 컨트롤러 없음 (조명 연결 필요)", true); return; }

                var results = await Task.WhenAll(tasks);
                int okCtrl = results.Count(r => r);
                SetStatus($"적용 완료 — {okCtrl}/{tasks.Count} 컨트롤러 병렬 ({string.Join(", ", ports)})", okCtrl != tasks.Count);
            }
            catch (Exception ex) { SetStatus("적용 예외: " + ex.Message, true); }
        }

        /// <summary>한 컨트롤러의 settings 를 Page 별 batch 로 적용 (Level 0 = OFF). 모두 성공 시 true.</summary>
        private async Task<bool> ApplyControllerAsync(ILightController ctrl, ControllerChannels cs, List<InspectionLightSetting> settings)
        {
            bool allOk = true;
            foreach (var pgrp in settings.GroupBy(s => s.Page).OrderBy(g => g.Key))
            {
                await ctrl.SwitchPageAsync(pgrp.Key);
                int[] times = new int[ctrl.ChannelCount];   // 0 = OFF
                foreach (var s in pgrp)
                {
                    if (cs.Channels == null || !cs.Channels.Contains(s.Channel))
                    {
                        AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-CHANNEL-OUT-OF-POOL", "Light/" + ctrl.PortName, $"ch{s.Channel} not in pool");
                        continue;
                    }
                    if (s.Channel >= 1 && s.Channel <= ctrl.ChannelCount)
                        times[s.Channel - 1] = s.On ? s.Level : 0;
                }
                allOk &= await ctrl.SetChannelBatchAsync(pgrp.Key, times);
            }
            return allOk;
        }

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
