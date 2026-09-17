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
    public class SubsetPageBase : PageBase
    {
        protected RecipeProject _project;
        protected Panel         _editorPanel;
        protected Label         _lblProject;
        private bool            _editorBuilt;
        private Button          _btnSave;

        public SubsetPageBase()
            : this("recipe.subset")
        {
        }

        protected SubsetPageBase(string i18nKey)
        {
            // Stage 61 — 도킹 충돌 회피: 단일 Top container 안에 SectionHeader + TopBar 를 명시적 Y 로 배치
            //   기존 두 개의 Dock=Top 컨트롤 분리 시 z-order 따라 editor 가 헤더에 가려지는 문제 해결.
            BuildEditorContainer();      // ① Dock=Fill (먼저 추가 — 빈 영역 채우기 후 헤더 가 위에서 잘라냄)
            BuildHeaderContainer(i18nKey); // ② Dock=Top H=66 (SectionHeader 30 + TopBar 36)
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            EnsureEditorBuilt();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible || !_editorBuilt || IsDesignerMode())
                return;

            // Recipe 탭은 페이지를 캐시한다. 다른 Recipe 페이지에서 저장한 최신값을
            // 재진입 시 다시 읽어 stale Project 전체가 덮어써지는 것을 막는다.
            LoadCurrentProject();
            if (_project != null)
                SafeLoadFromRecipe();
        }

        private void EnsureEditorBuilt()
        {
            if (_editorBuilt) return;
            _editorBuilt = true;

            BuildEditor(_editorPanel);

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
            // 헤더 배경색을 유닛 페이지(카세트 등)와 동일한 진회색으로 통일 (project 바(topBar)는 유지)
            sectionHeader.BackColor = Color.FromArgb(64, 64, 64);
            // 헤더 텍스트 시작점을 유닛 페이지(카세트=18)와 동일하게 통일
            sectionHeader.Padding = new Padding(18, 0, 0, 0);
            headerHost.Controls.Add(sectionHeader, 0, 0);

            // Row 1 — TopBar
            var topBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.OptionHeaderBg,
                Margin = Padding.Empty
            };

            _btnSave = new Button
            {
                Dock = DockStyle.Right, Width = 150, Text = "저장",
                FlatStyle = FlatStyle.Flat, BackColor = UiTheme.Accent, ForeColor = Color.White,
                Font = UiTheme.ButtonFont
            };
            _btnSave.Click += (s, e) => DoSave();

            var btnLoad = new Button
            {
                Dock = DockStyle.Right, Width = 150, Text = "새로고침",
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
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 0, 0, 0),
                Text = "(no project)"
            };

            // Dock=Right 두 버튼 먼저 추가 → Fill 라벨
            topBar.Controls.Add(_lblProject);
            topBar.Controls.Add(btnLoad);
            topBar.Controls.Add(_btnSave);

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
        }

        private void LoadCurrentProject()
        {
            // 화면 진입만으로 다른 Recipe를 활성화하지 않는다.
            // Main 화면의 현재 Recipe가 있으면 그것을 편집 대상으로 삼고,
            // 시작 직후처럼 활성 Recipe가 없을 때만 마지막 프로젝트로 fallback 한다.
            var host = FindForm() as Form1;
            string name = host != null ? host.ActiveRecipeName : null;
            if (string.IsNullOrEmpty(name)) name = RecipeStore.GetLastProjectName();
            if (string.IsNullOrEmpty(name)) name = AppSettingsStore.Current.LastProject;
            if (string.IsNullOrEmpty(name))
            {
                var list = RecipeStore.List();
                if (list.Count > 0) name = list[0];
            }
            if (string.IsNullOrEmpty(name)) { _project = null; _lblProject.Text = "(no project)"; return; }
            _project = RecipeStore.Load(name);
            _lblProject.Text = _project != null ? "레시피: " + _project.FileName : "(load failed: " + name + ")";

            // 누락된 subset 자동 보충
            if (_project != null)
            {
                if (_project.Die         == null) _project.Die         = new DieSubset();
                if (_project.Frame       == null) _project.Frame       = new TapeFrameSubset();
                if (_project.LoadFrame   == null) _project.LoadFrame   = new LoadTapeFrameSubset();
                if (_project.UnloadFrame == null) _project.UnloadFrame = new UnloadTapeFrameSubset();
                if (_project.Module      == null) _project.Module      = new ModuleSubset();
                if (_project.Pickup      == null) _project.Pickup      = new PickupSubset();
                if (_project.InputPickup == null) _project.InputPickup = ClonePickupSubset(_project.Pickup);
                if (_project.OutputPickup == null) _project.OutputPickup = ClonePickupSubset(_project.Pickup);
            }
        }

        private static PickupSubset ClonePickupSubset(PickupSubset source)
        {
            if (source == null)
                return new PickupSubset();

            return new PickupSubset
            {
                StartCorner = source.StartCorner,
                Direction = source.Direction,
                Pattern = source.Pattern
            };
        }

        private void DoSave()
        {
            if (_project == null) { QMC.Common.MessageDialog.Show("No project loaded."); return; }
            try
            {
                SafeSaveToRecipe();
                if (!SaveToRecipePersistsProject &&
                    !RecipeStore.Save(_project))
                {
                    throw new InvalidOperationException("Project Recipe 파일 저장에 실패했습니다.");
                }

                var host = FindForm() as Form1;

                if (!SaveToRecipeAppliesCurrentRecipe)
                {
                    if (host == null)
                        throw new InvalidOperationException("메인 화면을 찾을 수 없습니다.");

                    if (!host.SaveAndApplyActiveRecipe(_project))
                    {
                        throw new InvalidOperationException(
                            "활성 Recipe 저장 및 적용에 실패했습니다. recipe=" +
                            _project.FileName);
                    }
                }

                QMC.Common.MessageDialog.Show(
                    $"Saved to {_project.FileName}.Project",
                    "Recipe",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("Save failed: " + ex.Message);
            }
        }

        private void SafeLoadFromRecipe() { try { LoadFromRecipe(); } catch { } }
        private void SafeSaveToRecipe()   {       SaveToRecipe();              }

        // ── 서브클래스 구현 ──
        protected virtual void BuildEditor(Panel container) { }
        protected virtual void LoadFromRecipe() { }
        protected virtual void SaveToRecipe() { }
        protected virtual bool SaveToRecipePersistsProject => false;
        protected virtual bool SaveToRecipeAppliesCurrentRecipe => false;

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
