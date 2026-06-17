using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QMC.Common.Recipes;   // VisionAlgorithm, InspectionLabel
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui.Controls;   // SidebarButton

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

        public RecipePage()
        {
            InitializeComponent();
        }

        private void OnPageLoad(object sender, EventArgs e) => BuildSidebar();

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
                Text = VisionAlgorithm.Label(key),
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
                Text = InspectionLabel.Get(s.Module.AlgorithmKey, s.Id),
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

            _hdr.Text = "Recipe — " + VisionAlgorithm.Label(_curAlgo) + " / " + InspectionLabel.Get(s.Module.AlgorithmKey, s.Id);
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
    }
}
