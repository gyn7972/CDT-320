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
using QMC.Vision.Ui.Localization; // Lang

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
        private bool _langHooked;   // LanguageChanged 중복 구독 방지

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

            if (!_langHooked) { Lang.LanguageChanged += OnLanguageChanged; _langHooked = true; }
            ApplyLanguage();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_langHooked) { Lang.LanguageChanged -= OnLanguageChanged; _langHooked = false; }
            base.OnHandleDestroyed(e);
        }

        /// <summary>언어 변경 — UI 스레드로 마샬링 후 표시 문구 재적용.</summary>
        private void OnLanguageChanged()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((Action)ApplyLanguage); } catch { } return; }
            ApplyLanguage();
        }

        /// <summary>현재 언어로 헤더/버튼/컬럼(한글 항목)을 적용. (Ch/Level/Page 영문 식별 컬럼은 유지)</summary>
        private void ApplyLanguage()
        {
            if (_btnSave   != null) _btnSave.Text   = Lang.T("common.save");
            if (_btnApply  != null) _btnApply.Text  = Lang.T("rec.applyRun");
            if (_btnReset  != null) _btnReset.Text  = Lang.T("rec.reset");
            if (_btnCancel != null) _btnCancel.Text = Lang.T("common.cancel");
            if (Ctrl    != null) Ctrl.HeaderText    = Lang.T("rec.ctrl");
            if (ColName != null) ColName.HeaderText = Lang.T("common.name");
            UpdateHeaderText();
        }

        /// <summary>검사 선택 상태에 맞춰 헤더 문구를 현재 언어로 구성.</summary>
        private void UpdateHeaderText()
        {
            if (_lblHeader == null) return;
            if (!string.IsNullOrEmpty(_algorithm))
                _lblHeader.Text = Lang.T("rec.inspLight") + " — " + Lang.Inspection(_algorithm, _inspectionId)
                                + "  (" + Lang.Algo(_algorithm) + " / " + _inspectionId + ")";
            else
                _lblHeader.Text = Lang.T("rec.inspLight");
        }

        /// <summary>독립(비편입) Save 버튼이 저장할 대상 레시피명. 호스트 타깃 페이지가 주입(미주입 시 default).
        /// 편입 모드(EmbeddedMode)에서는 호스트 SaveTarget 이 활성 레시피명으로 일괄 저장하므로 사용하지 않는다.</summary>
        public string RecipeName { get; set; } = "default";

        /// <summary>R2e — 통합 저장 진입점(타깃 상단바 저장이 호출).
        /// 편입 모드: 조명 레벨을 노드 Recipe.LightSettings POCO 에 반영만 하고, 실제 파일 저장은 호스트 SaveTarget 의
        /// SaveRecipe(활성 레시피명)가 일괄 수행한다(조명이 활성 레시피가 아닌 "default"로 새던 문제 수정).
        /// 비편입(독립) 모드: 자체 RecipeName 으로 즉시 저장.</summary>
        public void PersistLight()
        {
            if (!CollectIntoRecipe()) return;
            if (_embedded)
            {
                var recipe = _node?.Recipe as AlgoRecipeBase;
                SetStatus(string.Format(Lang.T("rec.lightSavedFmt"), _node?.StorageKey ?? "?", recipe?.LightSettings.Count(s => s.Level > 0) ?? 0), false);
                return;   // 저장은 호스트가 SaveRecipe(활성 레시피)로 수행
            }
            SaveToRecipeFile(string.IsNullOrWhiteSpace(RecipeName) ? "default" : RecipeName);
        }

        public void SelectInspection(string algorithm, string inspectionId)
        {
            _algorithm = algorithm;
            _inspectionId = inspectionId;
            UpdateHeaderText();
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
                SetStatus(Lang.T("rec.lightLoadFail"), true);
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
                _lblWiring.Text = Lang.T("rec.lightUnassigned");
            else
                _lblWiring.Text = Lang.T("rec.lightAssignPrefix") + string.Join("    ", pages.Select(pr =>
                {
                    var ce = LightSystemSetupStore.Current?.GetController(pr.ControllerPort);
                    string nm = (ce != null && !string.IsNullOrEmpty(ce.Name)) ? $" ({ce.Name})" : "";
                    return $"{pr.ControllerPort}{nm} p{pr.Page}";
                })) + Lang.T("rec.lightAssignSuffix");

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
            SetStatus(assigned ? "" : Lang.T("rec.lightUnassignShort"), !assigned);
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

        /// <summary>비편입(독립) 모드 자체 저장 버튼 — 주입된 RecipeName(미주입 시 default)으로 즉시 저장.</summary>
        private void Save()
        {
            if (!CollectIntoRecipe()) return;
            SaveToRecipeFile(string.IsNullOrWhiteSpace(RecipeName) ? "default" : RecipeName);
        }

        /// <summary>UI 그리드 → 노드 Recipe.LightSettings POCO 반영(파일 저장 없음). 노드 미해결 시 false.
        /// C3b-3 — 레벨만 반영. 컨트롤러/페이지 지정(Setup.LightPages)은 [설정>검사]에서 별도 편집.</summary>
        private bool CollectIntoRecipe()
        {
            var recipe = _node?.Recipe as AlgoRecipeBase;
            if (recipe == null) { SetStatus(Lang.T("rec.lightSaveNoNode"), true); return false; }

            var settings = Collect();
            settings.RemoveAll(s => s.Level <= 0 && s.StrobeTimeUs <= 0 && s.StabilizeDelayMs <= 0);   // 미사용(0) 정리
            recipe.LightSettings = settings;
            return true;
        }

        /// <summary>현재 recipe POCO 를 지정 레시피명 파일로 저장.</summary>
        private void SaveToRecipeFile(string recipeName)
        {
            try { _node.SaveRecipe(recipeName); }
            catch (System.Exception ex) { SetStatus(Lang.T("rec.lightSaveExc") + ex.Message, true); return; }
            var recipe = _node.Recipe as AlgoRecipeBase;
            SetStatus(string.Format(Lang.T("rec.lightSavedFmt"), _node.StorageKey, recipe?.LightSettings.Count(s => s.Level > 0) ?? 0), false);
        }

        private async void Apply()
        {
            // C3b-3 — 결선 풀 검증 폐기. 지정(LightPages) 없으면 거부, 있으면 페이지 전 채널 배열(미사용=0) 송신.
            if (ActivePages().Count == 0)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-PAGE-MISS", "Light/" + _algorithm, $"{_algorithm}/{_inspectionId} 컨트롤러/페이지 미지정");
                SetStatus(Lang.T("rec.lightApplyReject"), true); return;
            }
            var settings = Collect();
            try
            {
                LogLight("Apply 시작 — 등록 LightHub 포트=[" + string.Join(",", LightHub.Ports) + "], 적용 채널수=" + settings.Count);
                // Stage 81 — 컨트롤러별 group → 각 컨트롤러 batch Task → Task.WhenAll 병렬(독립 시리얼 포트).
                var tasks = new List<Task<bool>>();
                var ports = new List<string>();
                foreach (var grp in settings.Where(s => !string.IsNullOrEmpty(s.ControllerPort)).GroupBy(s => s.ControllerPort))
                {
                    var ctrl = LightHub.Get(grp.Key);
                    if (ctrl == null)
                    {
                        LogLight("포트 '" + grp.Key + "' LightHub 미등록 → 건너뜀(실제 조명 미적용). [설정>조명]에서 컨트롤러/포트 확인.");
                        AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-MAP-INVALID", "Light/" + _algorithm, $"{grp.Key} not in LightHub");
                        continue;
                    }
                    LogLight("포트 '" + grp.Key + "' 컨트롤러=" + ctrl.GetType().Name + " (Sim 이면 실제 조명 변화 없음), 채널 " + grp.Count() + "개 적용");
                    ports.Add(grp.Key);
                    tasks.Add(ApplyControllerAsync(ctrl, grp.ToList()));
                }
                if (tasks.Count == 0) { LogLight("적용할 컨트롤러 없음(전 포트 미등록)."); SetStatus(Lang.T("rec.lightApplyNoCtrl"), true); return; }

                var results = await Task.WhenAll(tasks);
                int okCtrl = results.Count(r => r);
                LogLight("Apply 완료 — 성공 " + okCtrl + "/" + tasks.Count + " 포트=[" + string.Join(",", ports) + "]" + (okCtrl != tasks.Count ? " (일부 배치 실패 — 시리얼 미연결 가능)" : ""));
                SetStatus(string.Format(Lang.T("rec.lightAppliedFmt"), okCtrl, tasks.Count, string.Join(", ", ports)), okCtrl != tasks.Count);
            }
            catch (Exception ex) { LogLight("Apply 예외: " + ex.Message); SetStatus(Lang.T("rec.lightApplyExc") + ex.Message, true); }
        }

        /// <summary>조명 적용 진단 로그 — Vision DataLog(EventLogger User=VISION, Code=LightApply).</summary>
        private void LogLight(string message)
        {
            try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "LightApply",
                (_algorithm ?? "?") + "/" + (_inspectionId ?? "?") + ": " + message); }
            catch { }
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
                bool ok = await ctrl.SetChannelBatchAsync(pgrp.Key, times);
                LogLight("배치 송신 page=" + pgrp.Key + " 값=[" + string.Join(",", times) + "] 결과=" + ok);
                allOk &= ok;
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
            SetStatus(Lang.T("rec.lightReset"), false);
        }

        private static int IntOf(DataGridViewRow r, string col, int def) => int.TryParse(r.Cells[col].Value?.ToString(), out var v) ? v : def;
        private void SetStatus(string msg, bool err) { _lblStatus.ForeColor = err ? Color.Firebrick : Color.DarkSlateGray; _lblStatus.Text = msg; }
    }
}
