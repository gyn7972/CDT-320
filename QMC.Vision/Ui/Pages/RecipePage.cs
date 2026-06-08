using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Core;
using QMC.Vision.Modules;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// Recipe — Handler VisionRecipePage 미러(우측 사이드바). 좌측 트리 제거(R2).
    /// 사이드바(RecipeTab 경량 미러): 모듈 그룹 + 타깃 버튼 → lazy 페이지 스왑(_content).
    /// 타깃 본문은 기존 호스트 페이지 재사용(FinderPage/InspectorPage/SpcChartPage/ParameterEditorHost) — 계약 보존.
    /// 무인자 ctor / public ShowSpc·ShowParameterEditors 보존.
    /// </summary>
    public partial class RecipePage : UserControl
    {
        private readonly Dictionary<string, Func<UserControl>> _targets = new Dictionary<string, Func<UserControl>>();
        private readonly Dictionary<string, UserControl> _cache = new Dictionary<string, UserControl>();
        private readonly Dictionary<string, Button> _btns = new Dictionary<string, Button>();
        private string _curKey;

        public RecipePage()
        {
            InitializeComponent();
        }

        private void OnPageLoad(object sender, EventArgs e) => BuildSidebar();

        // ── public 진입 보존(기존 호출자 호환) ──
        private void ShowSpc() => ShowTarget("__spc");
        private void ShowParameterEditors() => ShowTarget("__params");

        // ── 사이드바 구성(런타임) ──
        private void BuildSidebar()
        {
            var host = FindForm() as Form1;
            if (host == null) return;

            _sideFlow.Controls.Clear();
            _targets.Clear();
            _cache.Clear();
            _btns.Clear();
            _curKey = null;

            AddModuleGroup("Wafer vision",             host.WaferMod);
            AddModuleGroup("Bin vision",               host.BinMod);
            AddModuleGroup("Bottom inspection vision", host.BottomMod);
            AddModuleGroup("Front side inspection",    host.FrontSideMod);
            AddModuleGroup("Rear side inspection",     host.RearSideMod);

            AddGroupHeader("일반");
            AddTargetButton("SPC X-bar Chart", "__spc", () => new SpcChartPage { Dock = DockStyle.Fill });
            AddTargetButton("검사 파라미터", "__params", () => new Editors.ParameterEditorHost { Dock = DockStyle.Fill });

            // 첫 타깃 자동 선택
            foreach (var kv in _targets) { ShowTarget(kv.Key); break; }
        }

        private void AddModuleGroup(string label, VisionModule module)
        {
            if (module == null) return;
            AddGroupHeader(label + "  [" + module.Camera.Info.Id + "]");

            foreach (var kv in module.Finders)
            {
                var mod = module; var finder = kv.Value; var fid = kv.Key;
                // R2 프로토타입 대표 타깃 = Wafer/AlignDieFinder → 신규 3열 VisionTargetPage. 나머지는 기존 FinderPage.
                bool rep = label == "Wafer vision" && fid == "AlignDieFinder";
                Func<UserControl> factory = rep
                    ? (Func<UserControl>)(() => new VisionTargetPage(mod, finder) { Dock = DockStyle.Fill })
                    : () => new FinderPage(mod, finder) { Dock = DockStyle.Fill };
                AddTargetButton(rep ? (fid + "  ★") : fid, "F:" + label + ":" + fid, factory);
            }
            foreach (var kv in module.Inspectors)
            {
                var mod = module; var insp = kv.Value;
                AddTargetButton(kv.Key, "I:" + label + ":" + kv.Key,
                    () => new InspectorPage(mod, insp) { Dock = DockStyle.Fill });
            }
        }

        private void AddGroupHeader(string text)
        {
            var lbl = new Label
            {
                Text = text,
                AutoSize = false,
                Width = 224,
                Height = 24,
                Margin = new Padding(0, 6, 0, 2),
                Font = UiTheme.ButtonFont,
                ForeColor = Color.Gainsboro,
                BackColor = Color.FromArgb(0x40, 0x40, 0x40),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _sideFlow.Controls.Add(lbl);
        }

        /// <summary>타깃 버튼(상태 점 + 텍스트) — 클릭 시 lazy 페이지 스왑.</summary>
        private void AddTargetButton(string text, string key, Func<UserControl> factory)
        {
            if (_targets.ContainsKey(key)) return;
            _targets[key] = factory;

            var btn = new Button
            {
                Text = "   " + text,
                Tag = key,
                AutoSize = false,
                Width = 224,
                Height = 30,
                Margin = new Padding(0, 0, 0, 2),
                FlatStyle = FlatStyle.Flat,
                Font = UiTheme.ButtonFont,
                BackColor = UiTheme.SidebarBtnBg,
                ForeColor = UiTheme.SidebarBtnFg,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btn.FlatAppearance.BorderSize = 0;
            // 상태 점(미설정=회색) — 좌측 색바. R3/리뷰에서 설정완료/변경됨 색 매핑.
            var dot = new Panel { Width = 8, Height = 30, Dock = DockStyle.Left, BackColor = UiTheme.SidebarBtnBg };
            dot.BackColor = Color.FromArgb(0x8C, 0x8C, 0x8C);
            btn.Controls.Add(dot);
            btn.Click += new EventHandler(OnTargetButtonClick);

            _btns[key] = btn;
            _sideFlow.Controls.Add(btn);
        }

        private void OnTargetButtonClick(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Tag is string key) ShowTarget(key);
        }

        /// <summary>lazy 페이지 스왑(RecipeTab.ShowPage 미러) — 생성·캐시·가시성 토글.</summary>
        private void ShowTarget(string key)
        {
            if (key == null || !_targets.ContainsKey(key)) return;

            // 버튼 선택 표시
            foreach (var kv in _btns)
            {
                bool sel = kv.Key == key;
                kv.Value.BackColor = sel ? UiTheme.SidebarBtnSelBg : UiTheme.SidebarBtnBg;
                kv.Value.ForeColor = sel ? UiTheme.SidebarBtnSelFg : UiTheme.SidebarBtnFg;
            }

            // 이전 페이지 숨김
            if (_curKey != null && _cache.TryGetValue(_curKey, out var prev)) prev.Visible = false;
            _curKey = key;

            // lazy 생성 + 캐시
            if (!_cache.TryGetValue(key, out var page))
            {
                page = _targets[key]();
                if (page != null)
                {
                    page.Dock = DockStyle.Fill;
                    page.Visible = false;
                    _content.Controls.Add(page);
                    _cache[key] = page;
                }
            }
            if (page != null)
            {
                page.Visible = true;
                page.BringToFront();
                if (_btns.TryGetValue(key, out var b)) _hdr.Text = "Recipe — " + b.Text.Trim();
            }
        }
    }
}
