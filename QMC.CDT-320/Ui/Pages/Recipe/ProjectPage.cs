using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QMC.CDT320.Logging;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>레시피 - PROJECT. RecipeStore 를 통해 파일 목록/로드/저장.</summary>
    public class ProjectPage : PageBase
    {
        private ListBox      _listProjects;
        private RecipeProject _current;

        // 값 필드들
        private TextBox _tbFile, _tbMachine, _tbFlow;
        private TextBox _tbMap, _tbDir, _tbChip, _tbMaster, _tbTape, _tbBin;
        private TextBox _tbLot, _tbPart, _tbInCst, _tbOutCst, _tbColletM, _tbColletL, _tbXml;

        public ProjectPage()
        {
            Controls.Add(CreateSectionHeader("recipe.project"));
            BuildLeft();
            BuildCenter();
            BuildRight();
            BuildBottom();

            // 기본으로 첫 프로젝트 로드
            var names = RecipeStore.List();
            if (names.Count > 0) LoadProject(names[0]);
        }

        // ──────────────────────────────────────────
        //  좌측 — 프로젝트 목록
        // ──────────────────────────────────────────
        private void BuildLeft()
        {
            var leftPanel = new Panel { Location = new Point(8, 40), Size = new Size(360, 840), BackColor = UiTheme.OptionPanelBg };
            leftPanel.Controls.Add(new Label
            {
                Dock = DockStyle.Top, Height = 26, Text = "프로젝트 목록",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });

            _listProjects = new ListBox
            {
                Location = new Point(4, 30), Size = new Size(352, 760),
                Font = new Font("Consolas", 9F)
            };
            _listProjects.DoubleClick += (s, e) =>
            {
                if (_listProjects.SelectedItem is string f) LoadProject(f);
            };
            ReloadList();
            leftPanel.Controls.Add(_listProjects);

            var btnDel  = new Button { Location = new Point(4,   796), Size = new Size(84, 28), Text = Lang.T("common.delete"), Tag = "i18n:common.delete", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            var btnSave = new Button { Location = new Point(92,  796), Size = new Size(84, 28), Text = "SAVE AS",   FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            var btnOpen = new Button { Location = new Point(180, 796), Size = new Size(84, 28), Text = Lang.T("common.open"), Tag = "i18n:common.open", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            var btnNewF = new Button { Location = new Point(268, 796), Size = new Size(84, 28), Text = "새 폴더",    FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            btnDel .Click += (s, e) => OnDelete();
            btnSave.Click += (s, e) => OnSaveAs();
            btnOpen.Click += (s, e) => OnOpen();
            btnNewF.Click += (s, e) => OnOpenFolder();
            leftPanel.Controls.Add(btnDel); leftPanel.Controls.Add(btnSave); leftPanel.Controls.Add(btnOpen); leftPanel.Controls.Add(btnNewF);

            Controls.Add(leftPanel);
        }

        // ──────────────────────────────────────────
        //  중앙 — GLOBAL OPTION
        // ──────────────────────────────────────────
        private void BuildCenter()
        {
            Controls.Add(new Label
            {
                Location = new Point(380, 40), Size = new Size(340, 26),
                Text = "GLOBAL OPTION",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
            int y = 70;
            _tbMachine = AddTextRow(380, y, "장비 번호", "DS530"); y += 32;
            _tbFlow    = AddTextRow(380, y, "카세트 흐름 방식", "Mapping"); y += 32;
            AddReadOnlyRow(380, y, "DRY RUN",  "DISABLE"); y += 32;
            AddReadOnlyRow(380, y, "STEP RUN", "DISABLE"); y += 32;
            AddReadOnlyRow(380, y, "XML SAVE", "ENABLE");  y += 32;
            AddReadOnlyRow(380, y, "Re-DT",    "DISABLE"); y += 32;
            AddReadOnlyRow(380, y, "EBR MODE", "ENABLE");  y += 32;
            AddReadOnlyRow(380, y, "얼라인 확인 기능", "ENABLE"); y += 32;
            AddReadOnlyRow(380, y, "니들 위치 확인 모드", "DISABLE"); y += 32;
            AddReadOnlyRow(380, y, "AUTO POSITION DEVIATION LIMIT", "50");
        }

        // ──────────────────────────────────────────
        //  우측 — 프로젝트 옵션 + XML TRACE
        // ──────────────────────────────────────────
        private void BuildRight()
        {
            Controls.Add(new Label
            {
                Location = new Point(740, 40), Size = new Size(320, 26),
                Text = "프로젝트 옵션",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
            int y = 70;
            _tbFile   = AddTextRow(740, y, "파일 이름",       "");        y += 32;
            _tbMap    = AddTextRow(740, y, "맵 파일 형식",    "SEC");     y += 32;
            _tbDir    = AddTextRow(740, y, "맵 파일 방향",    "표준(0도)"); y += 32;
            _tbChip   = AddTextRow(740, y, "칩 두께",         "150"); y += 32;
            _tbMaster = AddTextRow(740, y, "마스터 칩 두께",   "150"); y += 32;
            _tbTape   = AddTextRow(740, y, "테이프 두께",     "100"); y += 32;
            _tbBin    = AddTextRow(740, y, "BIN SORT NUMBER", "1");   y += 32;

            // XML TRACE
            Controls.Add(new Label
            {
                Location = new Point(740, 400), Size = new Size(560, 26),
                Text = "XML TRACE FILE RECORDE ITEM",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
            y = 430;
            _tbLot     = AddTextRow(740, y, "LOT ID",            ""); y += 32;
            _tbPart    = AddTextRow(740, y, "PART ID",           ""); y += 32;
            _tbInCst   = AddTextRow(740, y, "INPUT CASSETTE ID", ""); y += 32;
            _tbOutCst  = AddTextRow(740, y, "OUTPUT CASSETTE ID",""); y += 32;
            _tbColletM = AddTextRow(740, y, "COLLET MODEL NUM",  ""); y += 32;
            _tbColletL = AddTextRow(740, y, "COLLET LOT NUM",    ""); y += 32;
            _tbXml     = AddTextRow(740, y, "XML PATH",          "");
        }

        private void BuildBottom()
        {
            var btnSaveRecipe = new Button
            {
                Location = new Point(1120, 350), Size = new Size(160, 44),
                Text = "RECIPE SAVE", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = Color.FromArgb(0xD9, 0x77, 0x06), ForeColor = Color.White
            };
            btnSaveRecipe.Click += (s, e) => OnSaveCurrent();
            Controls.Add(btnSaveRecipe);
        }

        // ──────────────────────────────────────────
        //  동작
        // ──────────────────────────────────────────

        private void ReloadList()
        {
            _listProjects.Items.Clear();
            foreach (var f in RecipeStore.List())
                _listProjects.Items.Add(f);
        }

        private void LoadProject(string fileName)
        {
            var p = RecipeStore.Load(fileName);
            if (p == null) return;
            _current = p;
            _tbFile.Text    = p.FileName;
            _tbMachine.Text = p.MachineNumber;
            _tbFlow.Text    = p.CassetteFlow;
            _tbMap.Text     = p.MapFormat;
            _tbDir.Text     = p.MapDirection;
            _tbChip.Text    = p.ChipThickness.ToString("0");
            _tbMaster.Text  = p.MasterChipThickness.ToString("0");
            _tbTape.Text    = p.TapeThickness.ToString("0");
            _tbBin.Text     = p.BinSortNumber.ToString();
            _tbLot.Text     = p.LotId ?? "";
            _tbPart.Text    = p.PartId ?? "";
            _tbInCst.Text   = p.InputCassetteId ?? "";
            _tbOutCst.Text  = p.OutputCassetteId ?? "";
            _tbColletM.Text = p.ColletModelNum ?? "";
            _tbColletL.Text = p.ColletLotNum ?? "";
            _tbXml.Text     = p.XmlPath ?? "";
            EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-LOAD", "Project loaded: " + fileName);
            // 마지막 로드된 프로젝트 기록 (재시작 시 자동 로드)
            RecipeStore.SaveLastProjectName(fileName);
            // Stage 61 — 상태바 Project Name 갱신
            try
            {
                var host = FindForm() as Form1;
                host?.RefreshProjectName(p.FileName);
                // Controller 에도 새 Pickup 옵션 등 적용
                host?.Controller?.ApplyRecipeMode(p);
            }
            catch { }
        }

        private void OnOpen()
        {
            if (_listProjects.SelectedItem is string f) LoadProject(f);
            else MessageBox.Show("프로젝트를 선택하세요.");
        }

        private void OnDelete()
        {
            if (!(_listProjects.SelectedItem is string f)) return;
            if (MessageBox.Show("삭제 하시겠습니까?\n" + f, "DELETE", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            RecipeStore.Delete(f);
            EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-DEL", "Project deleted: " + f);
            ReloadList();
        }

        private void OnSaveAs()
        {
            var p = CollectFromUi();
            string name = Prompt.Show("저장할 파일 이름을 입력하세요", p.FileName);
            if (string.IsNullOrWhiteSpace(name)) return;
            p.FileName = name.Trim();
            RecipeStore.Save(p);
            EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-SAVEAS", "Project saved: " + p.FileName);
            ReloadList();
            _listProjects.SelectedItem = p.FileName + ".Project";
        }

        private void OnSaveCurrent()
        {
            var p = CollectFromUi();
            if (string.IsNullOrWhiteSpace(p.FileName)) { MessageBox.Show("파일 이름이 비어 있습니다."); return; }
            RecipeStore.Save(p);
            EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-SAVE", "Project saved: " + p.FileName);
            ReloadList();
            MessageBox.Show("저장되었습니다: " + p.FileName);
        }

        private void OnOpenFolder()
        {
            try { System.Diagnostics.Process.Start(RecipeStore.Dir); } catch { }
        }

        private RecipeProject CollectFromUi()
        {
            var p = _current ?? new RecipeProject();
            p.FileName          = _tbFile.Text.Trim();
            p.MachineNumber     = _tbMachine.Text.Trim();
            p.CassetteFlow      = _tbFlow.Text.Trim();
            p.MapFormat         = _tbMap.Text.Trim();
            p.MapDirection      = _tbDir.Text.Trim();
            double.TryParse(_tbChip.Text,   out var c1); p.ChipThickness       = c1;
            double.TryParse(_tbMaster.Text, out var c2); p.MasterChipThickness = c2;
            double.TryParse(_tbTape.Text,   out var c3); p.TapeThickness       = c3;
            int.TryParse(_tbBin.Text,       out var b);  p.BinSortNumber       = b;
            p.LotId            = _tbLot.Text;
            p.PartId           = _tbPart.Text;
            p.InputCassetteId  = _tbInCst.Text;
            p.OutputCassetteId = _tbOutCst.Text;
            p.ColletModelNum   = _tbColletM.Text;
            p.ColletLotNum     = _tbColletL.Text;
            p.XmlPath          = _tbXml.Text;
            return p;
        }

        // ──────────────────────────────────────────
        //  입력 행 헬퍼
        // ──────────────────────────────────────────
        private TextBox AddTextRow(int x, int y, string label, string value)
        {
            var lbl = new Label
            {
                Location = new Point(x, y), Size = new Size(200, 28),
                Text = label, BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0),
                Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0), BorderStyle = BorderStyle.FixedSingle
            };
            var tb = new TextBox
            {
                Location = new Point(x + 204, y + 2), Size = new Size(140, 24),
                Text = value, Font = new Font("Consolas", 10F),
                TextAlign = HorizontalAlignment.Right
            };
            Controls.Add(lbl); Controls.Add(tb);
            return tb;
        }

        private void AddReadOnlyRow(int x, int y, string label, string value)
        {
            Controls.Add(new Label
            {
                Location = new Point(x, y), Size = new Size(200, 28),
                Text = label, BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0),
                Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0), BorderStyle = BorderStyle.FixedSingle
            });
            Controls.Add(new Label
            {
                Location = new Point(x + 204, y), Size = new Size(140, 28),
                Text = value, BackColor = Color.White,
                Font = new Font("Consolas", 10F), TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 6, 0), BorderStyle = BorderStyle.FixedSingle
            });
        }
    }

    /// <summary>간단한 프롬프트 다이얼로그.</summary>
    internal static class Prompt
    {
        public static string Show(string question, string defaultValue = "")
        {
            using (var f = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition   = FormStartPosition.CenterParent,
                MinimizeBox     = false, MaximizeBox = false,
                ShowIcon        = false,
                ClientSize      = new Size(420, 130),
                Text            = question
            })
            {
                var lbl = new Label   { Location = new Point(12, 12),  AutoSize = true, Text = question };
                var tb  = new TextBox { Location = new Point(12, 40),  Size = new Size(396, 24), Text = defaultValue };
                var ok  = new Button  { Location = new Point(240, 80), Size = new Size(80, 28), Text = "OK",     DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
                var cc  = new Button  { Location = new Point(328, 80), Size = new Size(80, 28), Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
                f.Controls.Add(lbl); f.Controls.Add(tb); f.Controls.Add(ok); f.Controls.Add(cc);
                f.AcceptButton = ok; f.CancelButton = cc;
                return f.ShowDialog() == DialogResult.OK ? tb.Text : null;
            }
        }
    }
}
