using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>
    /// Subset 편집 페이지 공통 베이스.
    /// 현재 프로젝트(AppSettings.LastProject) 를 자동 로드/저장.
    /// 서브클래스는 BuildEditor() 에서 입력 컨트롤 생성 + LoadFromRecipe() / SaveToRecipe() 구현.
    /// </summary>
    public abstract class SubsetPageBase : PageBase
    {
        protected RecipeProject _project;
        protected Panel         _editorPanel;
        protected Label         _lblProject;

        protected SubsetPageBase(string i18nKey)
        {
            // Stage 61 — 도킹 충돌 회피: 단일 Top container 안에 SectionHeader + TopBar 를 명시적 Y 로 배치
            //   기존 두 개의 Dock=Top 컨트롤 분리 시 z-order 따라 editor 가 헤더에 가려지는 문제 해결.
            BuildEditorContainer();      // ① Dock=Fill (먼저 추가 — 빈 영역 채우기 후 헤더 가 위에서 잘라냄)
            BuildHeaderContainer(i18nKey); // ② Dock=Top H=66 (SectionHeader 30 + TopBar 36)
            if (!IsDesignerMode())
            {
                LoadCurrentProject();
                if (_project != null) SafeLoadFromRecipe();
            }
        }

        private void BuildHeaderContainer(string i18nKey)
        {
            // 단일 Top container — TableLayoutPanel 로 2 row 명확히 분리 (Dock z-order 회피)
            //   Row 0 (H=30): Section header
            //   Row 1 (H=36): TopBar (Project + Reload + SAVE)
            var headerHost = new TableLayoutPanel
            {
                Dock = DockStyle.Top, Height = 66,
                ColumnCount = 1, RowCount = 2,
                Padding = Padding.Empty, Margin = Padding.Empty
            };
            headerHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            headerHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

            // Row 0 — Section header
            var sectionHeader = CreateSectionHeader(i18nKey);
            sectionHeader.Dock = DockStyle.Fill;   // 셀 전체 채움
            sectionHeader.Margin = Padding.Empty;
            headerHost.Controls.Add(sectionHeader, 0, 0);

            // Row 1 — TopBar
            var topBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.OptionHeaderBg,
                Margin = Padding.Empty
            };

            var btnSave = new Button
            {
                Dock = DockStyle.Right, Width = 180, Text = "SAVE",
                FlatStyle = FlatStyle.Flat, BackColor = UiTheme.Accent, ForeColor = Color.White,
                Font = UiTheme.ButtonFont
            };
            btnSave.Click += (s, e) => DoSave();

            var btnLoad = new Button
            {
                Dock = DockStyle.Right, Width = 120, Text = "Reload",
                FlatStyle = FlatStyle.Flat, BackColor = Color.White, Font = UiTheme.ButtonFont
            };
            btnLoad.Click += (s, e) =>
            {
                LoadCurrentProject();
                if (_project != null) SafeLoadFromRecipe();
            };

            _lblProject = new Label
            {
                Dock = DockStyle.Fill, ForeColor = Color.White, Font = UiTheme.SectionFont,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0),
                Text = "(no project)"
            };

            // Dock=Right 두 버튼 먼저 추가 → Fill 라벨
            topBar.Controls.Add(_lblProject);
            topBar.Controls.Add(btnLoad);
            topBar.Controls.Add(btnSave);

            headerHost.Controls.Add(topBar, 0, 1);
            Controls.Add(headerHost);
        }

        private void BuildEditorContainer()
        {
            _editorPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.MainBg,
                Padding = new Padding(8, 12, 8, 8),
                AutoScroll = true
            };
            Controls.Add(_editorPanel);
            BuildEditor(_editorPanel);
        }

        private void LoadCurrentProject()
        {
            // Stage 61 — RecipeStore 의 마지막 프로젝트 마커 우선 → AppSettings → 첫 파일 순
            string name = RecipeStore.GetLastProjectName();
            if (string.IsNullOrEmpty(name)) name = AppSettingsStore.Current.LastProject;
            if (string.IsNullOrEmpty(name))
            {
                var list = RecipeStore.List();
                if (list.Count > 0) name = list[0];
            }
            if (string.IsNullOrEmpty(name)) { _project = null; _lblProject.Text = "(no project)"; return; }
            _project = RecipeStore.Load(name);
            _lblProject.Text = _project != null ? "Project: " + _project.FileName : "(load failed: " + name + ")";

            // 누락된 subset 자동 보충
            if (_project != null)
            {
                if (_project.Die         == null) _project.Die         = new DieSubset();
                if (_project.Frame       == null) _project.Frame       = new TapeFrameSubset();
                if (_project.LoadFrame   == null) _project.LoadFrame   = new LoadTapeFrameSubset();
                if (_project.UnloadFrame == null) _project.UnloadFrame = new UnloadTapeFrameSubset();
                if (_project.Module      == null) _project.Module      = new ModuleSubset();
                if (_project.Pickup      == null) _project.Pickup      = new PickupSubset();
            }
        }

        private void DoSave()
        {
            if (_project == null) { MessageBox.Show("No project loaded."); return; }
            try
            {
                SafeSaveToRecipe();
                RecipeStore.Save(_project);
                // Stage 61 — 마지막 프로젝트 마커 + 상태바 갱신
                RecipeStore.SaveLastProjectName(_project.FileName);
                try
                {
                    var host = FindForm() as Form1;
                    host?.RefreshProjectName(_project.FileName);
                }
                catch { }
                MessageBox.Show($"Saved to {_project.FileName}.Project", "Recipe",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Save failed: " + ex.Message); }
        }

        private void SafeLoadFromRecipe() { try { LoadFromRecipe(); } catch { } }
        private void SafeSaveToRecipe()   {       SaveToRecipe();              }

        // ── 서브클래스 구현 ──
        protected abstract void BuildEditor(Panel container);
        protected abstract void LoadFromRecipe();
        protected abstract void SaveToRecipe();

        // ── 편의 ──
        protected Label MakeLabel(string text, int x, int y, int w = 200, int h = 26)
            => new Label
            {
                Location = new Point(x, y), Size = new Size(w, h),
                Text = text, Font = UiTheme.ButtonFont,
                TextAlign = ContentAlignment.MiddleLeft
            };

        protected NumericUpDown MakeNum(int x, int y, decimal min, decimal max, int decimals = 0,
                                        int w = 160, int h = 28)
        {
            return new NumericUpDown
            {
                Location = new Point(x, y), Size = new Size(w, h),
                Minimum = min, Maximum = max, DecimalPlaces = decimals,
                Increment = decimals > 0 ? (decimal)Math.Pow(0.1, decimals) : 1m,
                Font = UiTheme.ValueFont
            };
        }

        protected TextBox MakeText(int x, int y, int w = 200)
            => new TextBox
            {
                Location = new Point(x, y), Size = new Size(w, 28),
                Font = UiTheme.ValueFont
            };

        protected ComboBox MakeCombo(int x, int y, string[] items, int w = 200)
        {
            var cb = new ComboBox
            {
                Location = new Point(x, y), Size = new Size(w, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ValueFont
            };
            cb.Items.AddRange(items);
            return cb;
        }

        protected CheckBox MakeCheck(int x, int y, string text, int w = 200)
            => new CheckBox
            {
                Location = new Point(x, y), Size = new Size(w, 28),
                Text = text, Font = UiTheme.ButtonFont
            };
    }
}
