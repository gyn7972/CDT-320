using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QMC.Common.Recipes;   // VisionAlgorithm, InspectionLabel
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui.Controls;   // SidebarButton
using QMC.Vision.Ui.Localization; // Lang

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// R2c — Handler VisionRecipePage 미러. 사이드바=검사 알고리즘 5개(평면, Handler SidebarButton 1:1) +
    /// 영속 세팅선택기 바=알고리즘의 finder/inspector. 상태점(미설정 회색/설정완료 녹/변경됨 주황) 페인트.
    /// 본문 스왑 finder→VisionTargetPage·inspector→InspectorTargetPage(R2d, 3열). 상단바 SAVE=타깃 레시피저장.
    /// 무인자 ctor·ShowSpc 보존(SPC 미노출). P4 — ② 파라미터는 타깃 페이지/SettingsPage 통일 그리드로 흡수(ParameterEditorHost 제거).
    /// </summary>
    public partial class RecipePage : PageBase
    {
        private sealed class Setting
        {
            public IVisionModule Module;
            public string Id;
            public bool IsFinder;
            public IPatternFinder Finder;
            public IInspector Inspector;
        }

        private readonly Dictionary<string, IVisionModule> _algoModules = new Dictionary<string, IVisionModule>();
        private readonly Dictionary<string, SidebarButton> _algoBtns = new Dictionary<string, SidebarButton>();
        private readonly Dictionary<string, Setting> _settings = new Dictionary<string, Setting>();
        private readonly Dictionary<string, SidebarButton> _setBtns = new Dictionary<string, SidebarButton>();
        private readonly Dictionary<string, UserControl> _cache = new Dictionary<string, UserControl>();
        private string _curAlgo;
        private string _curSetKey;
        private bool _projectView;        // 현재 콘텐츠가 프로젝트(품목) 목록 뷰인지
        private bool _langHooked;         // LanguageChanged 중복 구독 방지
        private SidebarButton _projBtn;   // 모듈 레일의 '프로젝트(레시피)' 버튼

        public RecipePage()
        {
            InitializeComponent();
        }

        private void OnPageLoad(object sender, EventArgs e)
        {
            BuildSidebar();
            ReloadRecipeList();
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

        /// <summary>현재 언어로 사이드바/헤더/버튼/공통 그리드 표시 문구를 적용.</summary>
        private void ApplyLanguage()
        {
            if (_projBtn != null) _projBtn.Text = Lang.T("rec.projItem");
            if (_setHdr        != null) _setHdr.Text        = Lang.T("rec.finderInspector");
            if (_btnSaveRecipe != null) _btnSaveRecipe.Text = Lang.T("rec.saveRecipe");
            if (_sideHdr       != null) _sideHdr.Text       = Lang.T("rec.sideModule");
            if (_projHdr       != null) _projHdr.Text       = Lang.T("rec.projHdr");
            if (_btnRecApply   != null) _btnRecApply.Text   = Lang.T("common.apply");
            if (_btnRecNew     != null) _btnRecNew.Text     = Lang.T("rec.new");
            if (_btnRecCopy    != null) _btnRecCopy.Text    = Lang.T("rec.copy");
            if (_btnRecSaveAs  != null) _btnRecSaveAs.Text  = Lang.T("rec.saveAs");
            if (_btnRecDelete  != null) _btnRecDelete.Text  = Lang.T("common.delete");
            if (_commonHdr     != null) _commonHdr.Text     = Lang.T("rec.commonHdr");
            if (_btnCommonSave != null) _btnCommonSave.Text = Lang.T("rec.commonSave");

            // 사이드바 알고리즘 '탭' 버튼 + 세팅(finder/inspector) 버튼 표시명 갱신.
            foreach (var kv in _algoBtns)
                kv.Value.Text = Lang.Algo(kv.Key);
            foreach (var kv in _setBtns)
                if (_settings.TryGetValue(kv.Key, out var s))
                    kv.Value.Text = Lang.Inspection(s.Module.AlgorithmKey, s.Id);

            UpdateHeader();
            BuildCommonGrid();
        }

        /// <summary>현재 뷰(세팅/프로젝트)에 맞춰 상단 헤더를 현재 언어로 구성.</summary>
        private void UpdateHeader()
        {
            string prefix = Lang.T("rec.hdrPrefix");
            if (!_projectView && _curSetKey != null && _settings.TryGetValue(_curSetKey, out var s))
                _hdr.Text = prefix + " — " + Lang.Algo(_curAlgo) + " / " + Lang.Inspection(s.Module.AlgorithmKey, s.Id);
            else
                _hdr.Text = prefix + " — " + Lang.T("rec.hdrProject");
        }

        /// <summary>현재 활성 레시피 명칭(핸들러 수신 = Machine.CurrentRecipeName). 타깃 페이지 생성 시 주입.</summary>
        private string CurrentRecipeName()
            => (FindForm() as Form1)?.Machine?.CurrentRecipeName ?? "default";

        // ── public 진입 보존(계약 — SPC/파라미터는 미노출이나 메서드/페이지 보존) ──
        private void ShowSpc()
        {
            _content.Controls.Clear();
            _content.Controls.Add(new SpcChartPage { Dock = DockStyle.Fill });
        }
        // P4 — ShowParameterEditors(ParameterEditorHost) 제거: ② 편집은 타깃 페이지/SettingsPage 통일 그리드로 흡수.

        // ── 사이드바: 검사 알고리즘 5개 평면(Handler SidebarButton) ──
        private void BuildSidebar()
        {
            var host = FindForm() as Form1;
            if (host == null) return;

            _sideFlow.Controls.Clear();
            _algoBtns.Clear();
            _algoModules.Clear();

            // 핸들러 RecipeTab 처럼 모듈 위에 '프로젝트(레시피)' 항목 — 누르면 레시피 목록 표시
            _projBtn = new SidebarButton
            {
                Text = Lang.T("rec.projItem"),
                Width = UiTheme.SidebarWidth - 8,
                Height = 46,
                ShowStatusDot = false,
                Margin = new Padding(0, 0, 0, 8)
            };
            _projBtn.Click += new EventHandler((s, e) => ShowRecipeList());
            _sideFlow.Controls.Add(_projBtn);

            AddAlgoButton(host.WaferMod);
            AddAlgoButton(host.BinMod);
            AddAlgoButton(host.BottomMod);
            AddAlgoButton(host.TopSideVisionMod);
            AddAlgoButton(host.BottomSideVisionMod);

            foreach (var kv in _algoBtns) { SelectAlgorithm(kv.Key); break; }
        }

        private void AddAlgoButton(IVisionModule module)
        {
            if (module == null) return;
            string key = module.AlgorithmKey;
            if (string.IsNullOrEmpty(key) || _algoModules.ContainsKey(key)) return;
            _algoModules[key] = module;

            var btn = new SidebarButton
            {
                Text = Lang.Algo(key),
                Tag = key,
                Width = UiTheme.SidebarWidth - 8,   // 설정 사이드바와 동일 폭
                Height = 46,
                ShowStatusDot = false,              // 설정 사이드바처럼 상태점 제거 → 깔끔
                Margin = new Padding(0, 0, 0, 2)
            };
            btn.Click += new EventHandler(OnAlgorithmClick);
            _algoBtns[key] = btn;
            _sideFlow.Controls.Add(btn);
        }

        private void OnAlgorithmClick(object sender, EventArgs e)
        {
            if (sender is SidebarButton b && b.Tag is string key) SelectAlgorithm(key);
        }

        private void SelectAlgorithm(string algoKey)
        {
            if (!_algoModules.TryGetValue(algoKey, out var module)) return;
            _projectView = false;
            _projRail.Visible = false;   // 모듈 선택 시 레시피 목록 숨김
            SetFinderRailVisible(true);  // 모듈 = Finder/Inspector 레일 표시
            if (_projBtn != null) _projBtn.Selected = false;   // 프로젝트 버튼 해제
            _curAlgo = algoKey;
            foreach (var kv in _algoBtns) kv.Value.Selected = (kv.Key == algoKey);
            BuildSettingSelector(module);
        }

        // ── 세팅선택기: 현 알고리즘의 finder/inspector ──
        private void BuildSettingSelector(IVisionModule module)
        {
            _setFlow.Controls.Clear();
            _settings.Clear();
            _setBtns.Clear();
            _curSetKey = null;

            foreach (var kv in module.Finders)
                AddSetting(new Setting { Module = module, Id = kv.Key, IsFinder = true, Finder = kv.Value });
            foreach (var kv in module.Inspectors)
                AddSetting(new Setting { Module = module, Id = kv.Key, IsFinder = false, Inspector = kv.Value });

            foreach (var kv in _settings) { ShowSetting(kv.Key); break; }
        }

        private void AddSetting(Setting s)
        {
            string key = (s.IsFinder ? "F:" : "I:") + s.Module.AlgorithmKey + ":" + s.Id;
            if (_settings.ContainsKey(key)) return;
            _settings[key] = s;

            var btn = new SidebarButton
            {
                Text = Lang.Inspection(s.Module.AlgorithmKey, s.Id),
                Tag = key,
                Height = 36,
                Width = 188,                          // 세로 레일 전체폭
                Margin = new Padding(0, 0, 0, 3),
                Status = SettingStatus(key, s)
            };
            btn.Click += new EventHandler(OnSettingClick);
            _setBtns[key] = btn;
            _setFlow.Controls.Add(btn);
        }

        private void OnSettingClick(object sender, EventArgs e)
        {
            if (sender is SidebarButton b && b.Tag is string key) ShowSetting(key);
        }

        private void ShowSetting(string key)
        {
            if (!_settings.TryGetValue(key, out var s)) return;

            foreach (var kv in _setBtns) kv.Value.Selected = (kv.Key == key);

            if (_curSetKey != null && _cache.TryGetValue(_curSetKey, out var prev)) prev.Visible = false;
            _curSetKey = key;

            if (!_cache.TryGetValue(key, out var page))
            {
                string k = key;
                if (s.IsFinder)
                {
                    var vtp = new VisionTargetPage(s.Module, s.Finder, CurrentRecipeName()) { Dock = DockStyle.Fill, Visible = false };
                    vtp.DirtyChanged += (snd, ev) => { UpdateSettingDot(k); UpdateAlgoDot(s.Module); };
                    page = vtp;
                }
                else
                {
                    var itp = new InspectorTargetPage(s.Module, s.Inspector, CurrentRecipeName()) { Dock = DockStyle.Fill, Visible = false };
                    itp.DirtyChanged += (snd, ev) => { UpdateSettingDot(k); UpdateAlgoDot(s.Module); };
                    page = itp;
                }
                _content.Controls.Add(page);
                _cache[key] = page;
            }
            page.Visible = true;
            page.BringToFront();
            // C3b-3 — SettingsPage 에서 바뀐 조명 지정(노드 LightPages)을 레벨 그리드에 반영(캐시 페이지 재바인딩).
            (page as ITargetPage)?.RefreshLightAssignment();

            _projectView = false;
            UpdateHeader();
            UpdateSettingDot(key);
        }

        // ── 상태(미설정/설정완료/변경됨) ──
        // B — 새 BaseUnit 레시피 파일 경로: Recipes/default/<모듈StorageKey>.<id>.recipe.json (노드 TargetPath 와 일치).
        private static string RecipeFilePath(IVisionModule module, string id)
            => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", "default",
                            (module?.StorageKey ?? "Unknown") + "." + (id ?? "x") + ".recipe.json");

        private string SettingPath(Setting s) => RecipeFilePath(s.Module, s.Id);

        private SidebarStatus SettingStatus(string key, Setting s)
        {
            bool hasData = File.Exists(SettingPath(s));
            if (_cache.TryGetValue(key, out var pg) && pg is ITargetPage tp && tp.IsDirty)
                return SidebarStatus.Dirty;
            return hasData ? SidebarStatus.Done : SidebarStatus.Off;
        }

        private void UpdateSettingDot(string key)
        {
            if (_settings.TryGetValue(key, out var s) && _setBtns.TryGetValue(key, out var btn))
                btn.Status = SettingStatus(key, s);
        }

        /// <summary>알고리즘 상태점 = 세팅 집계(any dirty→변경됨 / any 저장→설정완료 / 없음→미설정).</summary>
        private SidebarStatus AlgoStatus(IVisionModule module)
        {
            bool anyData = false;
            foreach (var kv in module.Finders)
            {
                if (File.Exists(RecipeFilePath(module, kv.Key))) anyData = true;
                string k = "F:" + module.AlgorithmKey + ":" + kv.Key;
                if (_cache.TryGetValue(k, out var pg) && pg is ITargetPage tp && tp.IsDirty) return SidebarStatus.Dirty;
            }
            foreach (var kv in module.Inspectors)
            {
                if (File.Exists(RecipeFilePath(module, kv.Key))) anyData = true;
                string k = "I:" + module.AlgorithmKey + ":" + kv.Key;
                if (_cache.TryGetValue(k, out var pg) && pg is ITargetPage tp && tp.IsDirty) return SidebarStatus.Dirty;
            }
            return anyData ? SidebarStatus.Done : SidebarStatus.Off;
        }

        private void UpdateAlgoDot(IVisionModule module)
        {
            if (module != null && _algoBtns.TryGetValue(module.AlgorithmKey, out var btn))
                btn.Status = AlgoStatus(module);
        }

        // ── 상단바 SAVE = 타깃 레시피 저장 ──
        private void OnSaveRecipeClick(object sender, EventArgs e)
        {
            if (_curSetKey == null || !_settings.TryGetValue(_curSetKey, out var s)) return;
            try
            {
                if (_cache.TryGetValue(_curSetKey, out var pg) && pg is ITargetPage tp)
                    tp.SaveTarget();   // 노드 SaveSettings/SaveRecipe + dirty clear (DirtyChanged→dot 갱신)
                UpdateSettingDot(_curSetKey);
                UpdateAlgoDot(s.Module);
            }
            catch { }
        }

        // ── 불러오기 = 저장된 레시피로 되돌리기(revert) ──
        private void OnLoadRecipeClick(object sender, EventArgs e)
        {
            if (_curSetKey == null || !_settings.TryGetValue(_curSetKey, out var s)) return;
            try
            {
                if (_cache.TryGetValue(_curSetKey, out var pg) && pg is ITargetPage tp)
                    tp.LoadTarget();   // 노드 LoadSettings/LoadRecipe + dirty clear (DirtyChanged→dot 갱신)
                UpdateSettingDot(_curSetKey);
                UpdateAlgoDot(s.Module);
            }
            catch { }
        }

        // ── 좌측 레시피(품목) 리스트 — 핸들러 ProjectPage 미러(Vision Recipes\ 폴더 백업) ──
        private void ReloadRecipeList()
        {
            try
            {
                _projList.Items.Clear();
                var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase) { "default" };
                string root = QMC.Common.Data.Store.RecipeDataStore.Root;
                if (Directory.Exists(root))
                    foreach (var d in Directory.GetDirectories(root))
                        names.Add(Path.GetFileName(d));
                foreach (var n in names) _projList.Items.Add(n);

                int idx = _projList.Items.IndexOf(CurrentRecipeName());
                if (idx >= 0) _projList.SelectedIndex = idx;
            }
            catch { }
        }

        private void OnRecipeApplyClick(object sender, EventArgs e)
        {
            if (!(_projList.SelectedItem is string name)) return;
            // 적용 = 해당 품목을 활성으로 로드 + 마지막 레시피로 영속화(재시작 복원) + 상단 표시.
            var host = FindForm() as Form1;
            host?.Machine?.SetRecipe(name);
            PersistActiveRecipe(name);
            host?.SetRecipeStatus(name);
            RebuildForRecipe();
            LogRecipeEvent("레시피 적용", name);
        }

        /// <summary>마지막 적용 레시피명을 VisionSettings 에 저장 — 재시작 시 RestoreLastRecipe 가 복원.</summary>
        private static void PersistActiveRecipe(string name)
        {
            try
            {
                QMC.Vision.Config.VisionConfigStore.Current.LastRecipeName = name ?? "";
                QMC.Vision.Config.VisionConfigStore.Save();
            }
            catch { }
        }

        /// <summary>레시피 적용/생성/저장/삭제를 이력(EventLogger)에 기록 — History 탭에서 조회.</summary>
        private static void LogRecipeEvent(string action, string name)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event, "USER", "RECIPE",
                    action + " - " + (string.IsNullOrWhiteSpace(name) ? "(이름없음)" : name));
            }
            catch { }
        }

        /// <summary>모듈 레일에서 '프로젝트' 선택 → 콘텐츠에 레시피(품목) 목록 표시.</summary>
        private void ShowRecipeList()
        {
            if (_curSetKey != null && _cache.TryGetValue(_curSetKey, out var prev)) prev.Visible = false;
            foreach (var kv in _algoBtns) kv.Value.Selected = false;
            if (_projBtn != null) _projBtn.Selected = true;   // 프로젝트 버튼 활성(흰색)
            _setFlow.Controls.Clear();      // 프로젝트엔 finder 없음
            SetFinderRailVisible(false);    // 프로젝트 화면 = Finder/Inspector 레일 숨김
            ReloadRecipeList();
            BuildCommonGrid();
            _projRail.Visible = true;
            _projRail.BringToFront();
            _projectView = true;
            UpdateHeader();
        }

        /// <summary>가운데 Finder/Inspector 레일 표시/숨김(+컬럼 폭 접기).</summary>
        private void SetFinderRailVisible(bool show)
        {
            if (_finderRail != null) _finderRail.Visible = show;
            if (_body != null && _body.ColumnStyles.Count >= 2)
                _body.ColumnStyles[1].Width = show ? 200 : 0;
        }

        /// <summary>우측 공통 설정 폼(품목 공통 비전 파라미터) — Machine.Recipe 에 바인딩.</summary>
        private void BuildCommonGrid()
        {
            var r = (FindForm() as Form1)?.Machine?.Recipe;
            if (r == null || _commonGrid == null) return;
            const ParameterGridScope sc = ParameterGridScope.Recipe;
            var items = new List<ParameterGridItem>
            {
                ParameterGridItem.Text(Lang.T("rec.partId"), sc, () => r.PartId, v => r.PartId = v),
                ParameterGridItem.Text("Lot ID",  sc, () => r.LotId,  v => r.LotId = v),
                ParameterGridItem.Text("Wafer ID", sc, () => r.WaferId, v => r.WaferId = v),
                ParameterGridItem.Double("Chip Width",  "mm", sc, () => r.ChipWidthMm,  v => r.ChipWidthMm = v),
                ParameterGridItem.Double("Chip Height", "mm", sc, () => r.ChipHeightMm, v => r.ChipHeightMm = v),
                ParameterGridItem.Double("Chip W Lower", "mm", sc, () => r.ChipWidthLowerMm,  v => r.ChipWidthLowerMm = v),
                ParameterGridItem.Double("Chip W Upper", "mm", sc, () => r.ChipWidthUpperMm,  v => r.ChipWidthUpperMm = v),
                ParameterGridItem.Double("Chip H Lower", "mm", sc, () => r.ChipHeightLowerMm, v => r.ChipHeightLowerMm = v),
                ParameterGridItem.Double("Chip H Upper", "mm", sc, () => r.ChipHeightUpperMm, v => r.ChipHeightUpperMm = v),
                ParameterGridItem.Double("Chip Thickness",    "mm", sc, () => r.ChipThicknessMm,   v => r.ChipThicknessMm = v),
                ParameterGridItem.Double("Tape Thickness",    "mm", sc, () => r.TapeThicknessMm,   v => r.TapeThicknessMm = v),
                ParameterGridItem.Double("Blade Width",       "mm", sc, () => r.BladeWidthMm,      v => r.BladeWidthMm = v),
                ParameterGridItem.Double("First Blade Depth", "mm", sc, () => r.FirstBladeDepthMm, v => r.FirstBladeDepthMm = v),
                ParameterGridItem.Double("Max Chipping Depth",  "mm", sc, () => r.MaxChippingDepthMm,  v => r.MaxChippingDepthMm = v),
                ParameterGridItem.Double("Max Chipping Length", "mm", sc, () => r.MaxChippingLengthMm, v => r.MaxChippingLengthMm = v),
                ParameterGridItem.Double("Max Foreign Size",    "mm", sc, () => r.MaxForeignSizeMm,    v => r.MaxForeignSizeMm = v),
                ParameterGridItem.Bool(Lang.T("rec.saveGoodImg"), sc, () => r.SaveGoodImage,             v => r.SaveGoodImage = v),
                ParameterGridItem.Bool(Lang.T("rec.useContam"),    sc, () => r.UseContaminationInspection, v => r.UseContaminationInspection = v),
                ParameterGridItem.Text(Lang.T("rec.imageSavePath"), sc, () => r.ImageSavePath, v => r.ImageSavePath = v),
            };
            _commonGrid.SetItems(items);
        }

        private void OnCommonSaveClick(object sender, EventArgs e)
        {
            var host = FindForm() as Form1;
            var m = host?.Machine;
            if (m == null) return;
            try
            {
                string name = CurrentRecipeName();
                m.SaveRecipe(name);
                PersistActiveRecipe(name);
                host.SetRecipeStatus(name);   // 저장 시 상단 Recipe 갱신
                _hdr.Text = Lang.T("rec.hdrPrefix") + " — " + Lang.T("rec.hdrProject")
                            + " · " + Lang.T("rec.commonSaved") + " [" + name + "]";
                LogRecipeEvent("공통설정 저장", name);
            }
            catch { }
        }

        /// <summary>레시피 전환 시 에디터 캐시 폐기. 다음 모듈 선택 시 새 레시피로 재로드. 프로젝트 뷰 유지.</summary>
        private void RebuildForRecipe()
        {
            foreach (var kv in _cache) { try { _content.Controls.Remove(kv.Value); kv.Value.Dispose(); } catch { } }
            _cache.Clear();
            _curSetKey = null;
            _setFlow.Controls.Clear();
            BuildCommonGrid();          // 새 레시피의 공통값으로 폼 갱신
            _projRail.Visible = true;
            _projRail.BringToFront();
        }

        private void OnRecipeNewClick(object sender, EventArgs e)
        {
            string name = PromptText(Lang.T("rec.newName"), "");
            if (string.IsNullOrWhiteSpace(name)) return;
            var host = FindForm() as Form1;
            var m = host?.Machine;
            if (m == null) return;
            m.NewRecipe(name);     // 런타임 기본값 리셋 + 빈 레시피로 저장(이전 데이터 미상속)
            PersistActiveRecipe(name);
            host.SetRecipeStatus(name);
            ReloadRecipeList();
            _projList.SelectedItem = name;
            RebuildForRecipe();
            LogRecipeEvent("레시피 생성(New)", name);
        }

        /// <summary>Copy = 현재 적용된 레시피(디스크 저장본)를 파일 그대로 새 이름으로 복제.
        /// (Save As 는 현재 편집중인 런타임을 저장 / Copy 는 저장된 레시피 폴더를 그대로 복사)</summary>
        private void OnRecipeCopyClick(object sender, EventArgs e)
        {
            string source = CurrentRecipeName();
            if (string.IsNullOrWhiteSpace(source))
            { MessageBox.Show(Lang.T("rec.copyNoCurrent"), Lang.T("rec.copy"), MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            string name = PromptText(Lang.T("rec.copyNewName") + " (" + source + ")", source + Lang.T("rec.copySuffix"));
            if (string.IsNullOrWhiteSpace(name)) return;
            if (string.Equals(name, source, StringComparison.OrdinalIgnoreCase))
            { MessageBox.Show(Lang.T("rec.copyDiffName"), Lang.T("rec.copy"), MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            string targetDir = QMC.Common.Data.Store.RecipeDataStore.DirOf(name);
            if (System.IO.Directory.Exists(targetDir))
            { MessageBox.Show(Lang.T("rec.copyExists") + "\n" + name, Lang.T("rec.copy"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var r = QMC.Common.Data.Store.RecipeDataStore.CopyRecipe(source, name);
            if (r == null || !r.Success)
            { MessageBox.Show(Lang.T("rec.copyFail") + (r?.Message ?? Lang.T("rec.copyNoSrc")), Lang.T("rec.copy"), MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            ReloadRecipeList();
            _projList.SelectedItem = name;   // 적용은 하지 않음(상단 Recipe 유지) — 목록에서 선택만
            LogRecipeEvent("레시피 복사(Copy) [" + source + " → " + name + "]", name);
        }

        private void OnRecipeSaveAsClick(object sender, EventArgs e)
        {
            string name = PromptText(Lang.T("rec.saveAsName"), CurrentRecipeName());
            if (string.IsNullOrWhiteSpace(name)) return;
            var host = FindForm() as Form1;
            var m = host?.Machine;
            if (m == null) return;
            m.SaveRecipe(name);    // 현재 상태를 새 레시피로 저장
            m.SetRecipe(name);
            PersistActiveRecipe(name);
            host.SetRecipeStatus(name);
            ReloadRecipeList();
            _projList.SelectedItem = name;
            LogRecipeEvent("다른 이름으로 저장(Save As)", name);
        }

        private void OnRecipeDeleteClick(object sender, EventArgs e)
        {
            if (!(_projList.SelectedItem is string name)) return;
            if (string.Equals(name, "default", StringComparison.OrdinalIgnoreCase))
            { MessageBox.Show(Lang.T("rec.delDefault"), Lang.T("common.delete"), MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (MessageBox.Show(Lang.T("rec.delConfirm") + "\n" + name, Lang.T("common.delete"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            QMC.Common.Data.Store.RecipeDataStore.DeleteRecipe(name);
            ReloadRecipeList();
            LogRecipeEvent("레시피 삭제", name);
        }

        /// <summary>간단 텍스트 입력 다이얼로그.</summary>
        private string PromptText(string title, string def)
        {
            using (var f = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(330, 100),
                MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false
            })
            {
                var tb = new TextBox { Left = 12, Top = 18, Width = 306, Text = def };
                var ok = new Button { Text = Lang.T("common.ok"), DialogResult = DialogResult.OK, Left = 160, Top = 56, Width = 74 };
                var cancel = new Button { Text = Lang.T("common.cancel"), DialogResult = DialogResult.Cancel, Left = 244, Top = 56, Width = 74 };
                f.Controls.Add(tb); f.Controls.Add(ok); f.Controls.Add(cancel);
                f.AcceptButton = ok; f.CancelButton = cancel;
                return f.ShowDialog(this) == DialogResult.OK ? tb.Text.Trim() : null;
            }
        }
    }
}
