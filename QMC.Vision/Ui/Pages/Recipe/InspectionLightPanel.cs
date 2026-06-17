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
using QMC.Vision.Modules;
using QMC.Vision.Optics;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 검사별 조명 값 편집 패널 (Recipe).
    /// <para>Stage 81 — 다중 컨트롤러 결선 지원. 행 = (Controller, Channel) 자동 생성. 사용자는 Level/Page 만 편집.</para>
    /// Stage 94 — Designer/Code 분리. 정적 shell(헤더/결선/버튼/그리드+컬럼)은 .Designer.cs, 그리드 행 바인딩(BindFields)은 Code.
    /// </summary>
    public partial class InspectionLightPanel : UserControl
    {
        private string _algorithm, _inspectionId;
        // C2/C3b-3 — 조명 SSOT = 알고리즘 노드 BaseUnit(Setup.LightPages 지정 / Recipe.LightSettings 레벨).
        // null 이면 호스트가 노드 미해결(조명 설정 비활성).
        private IAlgorithmNode _node;
        // Stage 81 — 컨트롤러별 MaxPower / (port,ch) 보존값.
        private readonly Dictionary<string, int> _maxPowerByPort = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, InspectionLightSetting> _carry = new Dictionary<string, InspectionLightSetting>();

        // R2e — 행 바인딩 중 변경 이벤트 억제 가드.
        private bool _suppressChange;

        /// <summary>R2e — 조명 값(Level/Page) 변경 알림. 타깃 페이지가 구독해 dirty 전파(상태점 점등).</summary>
        public event EventHandler LightChanged;

        /// <summary>
        /// R2e — 타깃 페이지 편입 모드. true 면 자체 저장/취소 버튼 숨김(상단바 통합 저장이 PersistLight 호출).
        /// 기본 false = 독립 에디터(SettingsPage/옛 FinderPage·InspectorPage) 동작 보존.
        /// </summary>
        public bool EmbeddedMode
        {
            get { return _embedded; }
            set
            {
                _embedded = value;
                if (_btnSave != null) _btnSave.Visible = !value;
                if (_btnCancel != null) _btnCancel.Visible = !value;
            }
        }
        private bool _embedded;

        public InspectionLightPanel()
        {
            InitializeComponent();
            WireGrid();
        }

        public InspectionLightPanel(string algorithm, string inspectionId)
        {
            InitializeComponent();
            WireGrid();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            SelectInspection(algorithm, inspectionId);
        }

        private void WireGrid()
        {
            if (_grid == null) return;
            QMC.Vision.Ui.Controls.GridTheme.Apply(_grid);   // R2e — PARAMETERS 그리드와 시각 통일
            _grid.CurrentCellDirtyStateChanged += (s, e) =>
            { if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            _grid.CellValueChanged += (s, e) =>
            { if (!_suppressChange) LightChanged?.Invoke(this, EventArgs.Empty); };
        }

        /// <summary>R2e — 통합 저장 진입점(타깃 상단바 저장이 호출). 독립 Save 와 동일.</summary>
        public void PersistLight() => Save();

        public void SelectInspection(string algorithm, string inspectionId)
        {
            _algorithm = algorithm;
            _inspectionId = inspectionId;
            _lblHeader.Text = "검사 조명 — " + InspectionLabel.Get(algorithm, inspectionId)
                            + "  (" + VisionAlgorithm.Label(algorithm) + " / " + inspectionId + ")";
            BindFields();
        }

        /// <summary>C2 — 노드 컨텍스트 주입 버전. 호스트가 finder/inspector 참조(또는 GetAlgorithm)로 해석한 노드를 전달.</summary>
        public void SelectInspection(IAlgorithmNode node, string algorithm, string inspectionId)
        {
            _node = node;
            SelectInspection(algorithm, inspectionId);
        }

        /// <summary>저장된 레벨 출처 — 노드 Recipe.LightSettings(없으면 빈 목록).</summary>
        private List<InspectionLightSetting> SavedSettings()
        {
            // C3a — 조명 SSOT = 노드 Recipe.LightSettings (구 algorithm_camera.json fallback 폐지).
            var r = _node?.Recipe as AlgoRecipeBase;
            return r?.LightSettings ?? new List<InspectionLightSetting>();
        }

        // ── 이벤트 핸들러 (Designer 에서 named 연결) ──
        private void OnSaveClick(object sender, EventArgs e) => Save();
        private void OnApplyClick(object sender, EventArgs e) => Apply();
        private void OnResetClick(object sender, EventArgs e) => ResetLevels();
        private void OnCancelClick(object sender, EventArgs e) => BindFields();
        private void OnGridDataError(object sender, DataGridViewDataErrorEventArgs e) => e.ThrowException = false;

        // ── 검사가 구동하는 컨트롤러/페이지 지정 = 소속 모듈 Setup.LightPages(카메라=조명 1:1 하드웨어) ──
        private List<LightPageRef> ActivePages()
        {
            var mod = (FindForm() as Form1)?.ResolveModule(_algorithm);
            var pages = (mod?.Setup as VisionModuleSetupBase)?.LightPages;
            if (pages != null && pages.Count > 0) return pages;
            // 마이그 전 폴백(표시용) — 검사 Recipe 레벨에서 (Port,Page) 도출.
            return SavedSettings()
                .Where(s => !string.IsNullOrEmpty(s.ControllerPort))
                .GroupBy(s => s.ControllerPort.ToUpperInvariant() + "/" + s.Page)
                .Select(g => new LightPageRef { ControllerPort = g.First().ControllerPort, Page = g.First().Page })
                .ToList();
        }

        private static string Key(string port, int page, int ch) => (port ?? "") + "/" + page + "/" + ch;

        private void BindFields()
        {
            _suppressChange = true;
            try { BindFieldsCore(); }
            finally { _suppressChange = false; }
        }

        private void BindFieldsCore()
        {
            _carry.Clear();
            _maxPowerByPort.Clear();

            // C3a — 검사 노드 미해결: 명시 메시지 + 입력 비활성(조용한 구경로 금지).
            if (_node == null)
            {
                _grid.Rows.Clear();
                _grid.Enabled = false;
                _btnApply.Enabled = _btnSave.Enabled = _btnReset.Enabled = false;
                _lblWiring.Text = "";
                SetStatus("설정 불러올 수 없음 — 검사 노드 미해결", true);
                System.Diagnostics.Debug.WriteLine("[InspectionLightPanel] 노드 미해결: " + _algorithm + "/" + _inspectionId);
                return;
            }

            // C3b-3 — 결선 풀 대신 노드 Setup.LightPages(검사가 구동하는 컨트롤러/페이지) 기반.
            var pages = ActivePages();
            bool assigned = pages.Count > 0;

            // 컨트롤러 능력(MaxPower) 수집
            foreach (var pr in pages)
            {
                var ce = LightSystemSetupStore.Current?.GetController(pr.ControllerPort);
                _maxPowerByPort[pr.ControllerPort] = (ce != null && ce.MaxPower > 0) ? ce.MaxPower : 240;
            }

            // 지정 헤더
            if (!assigned)
                _lblWiring.Text = "컨트롤러/페이지 미지정 — [설정 > 검사]에서 이 검사에 컨트롤러/페이지를 지정하세요.";
            else
                _lblWiring.Text = "지정: " + string.Join("    ", pages.Select(pr =>
                {
                    var ce = LightSystemSetupStore.Current?.GetController(pr.ControllerPort);
                    string nm = (ce != null && !string.IsNullOrEmpty(ce.Name)) ? $" ({ce.Name})" : "";
                    return $"{pr.ControllerPort}{nm} p{pr.Page}";
                })) + "   — 지정 변경은 [설정 > 검사]";

            // 저장 레벨 (port,page,ch) → 설정 (노드 Recipe.LightSettings)
            var saved = new Dictionary<string, InspectionLightSetting>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in SavedSettings())
            {
                string key = Key(s.ControllerPort, s.Page, s.Channel);
                if (!saved.ContainsKey(key)) saved[key] = s;
            }

            // Page 컬럼 = 모듈 Setup.LightPages 에서 고정 → 읽기전용 텍스트 표시(콤보 Items 불필요).
            _grid.Columns["Page"].ReadOnly = true;

            // 행 생성 — 각 지정(컨트롤러/페이지)의 채널 1..ChannelCount (레벨 0=미사용)
            _grid.Rows.Clear();
            foreach (var pr in pages)
            {
                var ce = LightSystemSetupStore.Current?.GetController(pr.ControllerPort);
                int channelCount = (ce != null && ce.ChannelCount > 0) ? ce.ChannelCount : 8;
                var labelByCh = new Dictionary<int, string>();
                if (ce?.ChannelLabels != null)
                    foreach (var l in ce.ChannelLabels)
                        if (!labelByCh.ContainsKey(l.Channel)) labelByCh[l.Channel] = l.Name ?? "";
                string ctrlDisp = pr.ControllerPort + ((ce != null && !string.IsNullOrEmpty(ce.Name)) ? $" ({ce.Name})" : "");

                for (int ch = 1; ch <= channelCount; ch++)
                {
                    string key = Key(pr.ControllerPort, pr.Page, ch);
                    saved.TryGetValue(key, out var s);
                    int level = s?.Level ?? 0;
                    labelByCh.TryGetValue(ch, out var nm);
                    int idx = _grid.Rows.Add(ctrlDisp, ch, nm ?? "", level, pr.Page);
                    _grid.Rows[idx].Cells["Page"].Value = pr.Page;
                    _grid.Rows[idx].Tag = pr.ControllerPort;   // 저장용 실제 PortName
                    _carry[key] = new InspectionLightSetting
                    {
                        ControllerPort = pr.ControllerPort, Channel = ch, Page = pr.Page,
                        StrobeTimeUs = s?.StrobeTimeUs ?? 0, StabilizeDelayMs = s?.StabilizeDelayMs ?? 0
                    };
                }
            }

            _grid.Enabled = assigned;
            _btnApply.Enabled = _btnSave.Enabled = _btnReset.Enabled = assigned;
            SetStatus(assigned ? "" : "컨트롤러/페이지 미지정 — [설정 > 검사]에서 지정 후 사용", !assigned);
        }

        /// <summary>UI → 노드 Recipe 조명 리스트(List&lt;InspectionLightSetting&gt;). 행 = (Controller, Channel). On=Level&gt;0 파생, Strobe/Stab 보존.</summary>
        private List<InspectionLightSetting> Collect()
        {
            var list = new List<InspectionLightSetting>();
            foreach (DataGridViewRow r in _grid.Rows)
            {
                if (r.IsNewRow) continue;
                string port = r.Tag as string;
                if (!int.TryParse(r.Cells["Channel"].Value?.ToString(), out int ch)) continue;
                int maxP = (port != null && _maxPowerByPort.TryGetValue(port, out var mp)) ? mp : 240;
                int level = IntOf(r, "Level", 0);
                if (level < 0) level = 0; if (level > maxP) level = maxP;
                int page = IntOf(r, "Page", 0);
                _carry.TryGetValue(Key(port, page, ch), out var keep);
                list.Add(new InspectionLightSetting
                {
                    ControllerPort = port, Channel = ch, Level = level, On = level > 0,
                    StrobeTimeUs = keep?.StrobeTimeUs ?? 0, StabilizeDelayMs = keep?.StabilizeDelayMs ?? 0, Page = page
                });
            }
            return list;
        }

        private void Save()
        {
            // C3b-3 — 레벨만 저장(노드 Recipe). 컨트롤러/페이지 지정(Setup.LightPages)은 [설정>검사]에서 별도 편집.
            var recipe = _node?.Recipe as AlgoRecipeBase;
            if (recipe == null) { SetStatus("저장 불가 — 검사 노드 미해결", true); return; }

            var settings = Collect();
            settings.RemoveAll(s => s.Level <= 0 && s.StrobeTimeUs <= 0 && s.StabilizeDelayMs <= 0);   // 미사용(0) 정리
            recipe.LightSettings = settings;
            try { _node.SaveRecipe("default"); }
            catch (System.Exception ex) { SetStatus("저장 예외: " + ex.Message, true); return; }
            SetStatus($"저장 완료 — 노드 [{_node.StorageKey}] 점등 {settings.Count(s => s.Level > 0)}채널", false);
        }

        private async void Apply()
        {
            // C3b-3 — 결선 풀 검증 폐기. 지정(LightPages) 없으면 거부, 있으면 페이지 전 채널 배열(미사용=0) 송신.
            if (ActivePages().Count == 0)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-PAGE-MISS", "Light/" + _algorithm, $"{_algorithm}/{_inspectionId} 컨트롤러/페이지 미지정");
                SetStatus("적용 거부 — 컨트롤러/페이지 미지정", true); return;
            }
            var settings = Collect();
            try
            {
                // Stage 81 — 컨트롤러별 group → 각 컨트롤러 batch Task → Task.WhenAll 병렬(독립 시리얼 포트).
                var tasks = new List<Task<bool>>();
                var ports = new List<string>();
                foreach (var grp in settings.Where(s => !string.IsNullOrEmpty(s.ControllerPort)).GroupBy(s => s.ControllerPort))
                {
                    var ctrl = LightHub.Get(grp.Key);
                    if (ctrl == null)
                    {
                        AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-MAP-INVALID", "Light/" + _algorithm, $"{grp.Key} not in LightHub");
                        continue;
                    }
                    ports.Add(grp.Key);
                    tasks.Add(ApplyControllerAsync(ctrl, grp.ToList()));
                }
                if (tasks.Count == 0) { SetStatus("적용 거부 — 등록된 컨트롤러 없음 (조명 연결 필요)", true); return; }

                var results = await Task.WhenAll(tasks);
                int okCtrl = results.Count(r => r);
                SetStatus($"적용 완료 — {okCtrl}/{tasks.Count} 컨트롤러 병렬 ({string.Join(", ", ports)})", okCtrl != tasks.Count);
            }
            catch (Exception ex) { SetStatus("적용 예외: " + ex.Message, true); }
        }

        /// <summary>한 컨트롤러의 settings 를 Page 별 batch 로 적용 (Level 0 = OFF=미사용). 모두 성공 시 true.</summary>
        private async Task<bool> ApplyControllerAsync(ILightController ctrl, List<InspectionLightSetting> settings)
        {
            bool allOk = true;
            foreach (var pgrp in settings.GroupBy(s => s.Page).OrderBy(g => g.Key))
            {
                await ctrl.SwitchPageAsync(pgrp.Key);
                int[] times = new int[ctrl.ChannelCount];   // 0 = OFF (미사용)
                foreach (var s in pgrp)
                {
                    // 결선 풀 검증 폐기(C3b-3) — 컨트롤러의 전 채널이 유효, 레벨 0 = OFF(미사용).
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

        private static int IntOf(DataGridViewRow r, string col, int def) => int.TryParse(r.Cells[col].Value?.ToString(), out var v) ? v : def;
        private void SetStatus(string msg, bool err) { _lblStatus.ForeColor = err ? Color.Firebrick : Color.DarkSlateGray; _lblStatus.Text = msg; }
    }
}
