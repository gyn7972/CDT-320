using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using QMC.Common.Data.Store;
using QMC.Common.Logging;
using QMC.CDT320;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class ProjectPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private const int KeyColumnIndex = 0;
        private const int NameColumnIndex = 1;
        private const int ValueColumnIndex = 2;

        private RecipeProject _current;
        private string _loadedProjectName = string.Empty;
        private bool _loading;

        public ProjectPage()
        {
            InitializeComponent();
            ConfigureRuntimeUi();
            WireEvents();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            ReloadList();
            string last = RecipeStore.GetLastProjectName();
            if (!string.IsNullOrWhiteSpace(last))
                SelectAndLoadProject(last);
            else if (listProjects.Items.Count > 0)
                LoadProject(listProjects.Items[0] as string);
            else
                LoadProject(new RecipeProject { FileName = "NEW" }, false);
        }

        private void ConfigureRuntimeUi()
        {
            ConfigureEditableGrid(gridSummary);
            ConfigureEditableGrid(gridGlobal);
            ConfigureEditableGrid(gridProject);
            ConfigureEditableGrid(gridXml);
            ConfigureMapGrid();
            ConfigureStatusGrid();
        }

        private void ConfigureEditableGrid(DataGridView grid)
        {
            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.EditMode = DataGridViewEditMode.EditOnEnter;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            grid.DefaultCellStyle.Font = new Font("맑은 고딕", 9F);

            var key = new DataGridViewTextBoxColumn { Name = "Key", HeaderText = "Key", Visible = false };
            var name = new DataGridViewTextBoxColumn { Name = "Property", HeaderText = "Property", FillWeight = 42, ReadOnly = true };
            var value = new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Value", FillWeight = 58 };
            grid.Columns.Add(key);
            grid.Columns.Add(name);
            grid.Columns.Add(value);
        }

        private void ConfigureMapGrid()
        {
            gridMap.AutoGenerateColumns = false;
            gridMap.Columns.Clear();
            gridMap.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridMap.MultiSelect = false;
            gridMap.EditMode = DataGridViewEditMode.EditOnEnter;
            gridMap.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridMap.ColumnHeadersDefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            gridMap.DefaultCellStyle.Font = new Font("맑은 고딕", 9F);

            gridMap.Columns.Add(new DataGridViewTextBoxColumn { Name = "Key", HeaderText = "Key", Visible = false });
            gridMap.Columns.Add(new DataGridViewTextBoxColumn { Name = "Map", HeaderText = "Map", FillWeight = 22, ReadOnly = true });
            gridMap.Columns.Add(new DataGridViewTextBoxColumn { Name = "Configured", HeaderText = "Configured File", FillWeight = 44 });
            gridMap.Columns.Add(new DataGridViewTextBoxColumn { Name = "Resolved", HeaderText = "Resolved Path", FillWeight = 54, ReadOnly = true });
            gridMap.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 18, ReadOnly = true });
        }

        private void ConfigureStatusGrid()
        {
            gridStatus.AutoGenerateColumns = false;
            gridStatus.Columns.Clear();
            gridStatus.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridStatus.MultiSelect = false;
            gridStatus.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridStatus.ColumnHeadersDefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            gridStatus.DefaultCellStyle.Font = new Font("맑은 고딕", 9F);

            gridStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "Item", HeaderText = "Item", FillWeight = 34 });
            gridStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 18 });
            gridStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "Detail", HeaderText = "Detail", FillWeight = 64 });
        }

        private void WireEvents()
        {
            listProjects.DoubleClick += (s, e) => OnOpen();
            listProjects.SelectedIndexChanged += (s, e) =>
            {
                if (!_loading && listProjects.SelectedItem is string fileName)
                    LoadProject(fileName);
            };

            btnNew.Click += (s, e) => OnNew();
            btnOpen.Click += (s, e) => OnOpen();
            btnCopy.Click += (s, e) => OnCopy();
            btnDelete.Click += (s, e) => OnDelete();
            btnOpenFolder.Click += (s, e) => OpenPath(RecipeStore.Dir);
            btnReload.Click += (s, e) => OnReload();
            btnOpenRecipeFolder.Click += (s, e) => OnOpenRecipeFolder();
            btnBrowseMap.Click += (s, e) => OnBrowseMap();
            btnOpenMap.Click += (s, e) => OnOpenMap();
            btnBrowseXml.Click += (s, e) => OnBrowseXmlPath();
            btnApplyCurrent.Click += (s, e) => OnApplyCurrent();
            btnSaveRecipe.Click += (s, e) => OnSaveCurrent();
            btnSaveAs.Click += (s, e) => OnSaveAs();

            gridMap.CellEndEdit += (s, e) => UpdateMapStatus();
        }

        private void ReloadList()
        {
            try
            {
                _loading = true;
                string selected = GetCurrentProjectName();
                listProjects.Items.Clear();
                foreach (string fileName in RecipeStore.List())
                    listProjects.Items.Add(NormalizeProjectName(fileName));

                if (!string.IsNullOrWhiteSpace(selected))
                    listProjects.SelectedItem = selected;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-LIST",
                    "프로젝트 목록 갱신 실패: " + ex.Message);
            }
            finally
            {
                _loading = false;
            }
        }

        private void SelectAndLoadProject(string projectName)
        {
            string normalized = NormalizeProjectName(projectName);
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            _loading = true;
            try
            {
                listProjects.SelectedItem = normalized;
            }
            finally
            {
                _loading = false;
            }

            LoadProject(normalized);
        }

        private void LoadProject(string fileName)
        {
            string name = NormalizeProjectName(fileName);
            RecipeProject project = RecipeStore.Load(name);
            if (project == null)
            {
                QMC.Common.MessageDialog.Show("프로젝트 파일을 불러오지 못했습니다.\r\nProject=" + name, "Project",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LoadProject(project, true);
        }

        private void LoadProject(RecipeProject project, bool applyToMachine)
        {
            if (project == null)
                return;

            try
            {
                _loading = true;
                EnsureProjectObjects(project);
                project.FileName = NormalizeProjectName(project.FileName);
                _current = project;
                _loadedProjectName = project.FileName;

                PopulateProjectToUi(project);
                if (applyToMachine)
                    ApplyProjectToMachine(project);

                EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-LOAD",
                    "프로젝트를 불러왔습니다. project=" + project.FileName);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-LOAD",
                    "프로젝트 로드 처리 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show("프로젝트 로드 처리 실패:\r\n" + ex.Message, "Project",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loading = false;
            }
        }

        private void PopulateProjectToUi(RecipeProject project)
        {
            lblCurrentProject.Text = "Current Project: " + (project.FileName ?? "-");

            gridSummary.Rows.Clear();
            AddRow(gridSummary, "FileName", "PROJECT NAME", project.FileName);
            AddRow(gridSummary, "MachineNumber", "MACHINE NO", project.MachineNumber);
            AddRow(gridSummary, "CassetteFlow", "CASSETTE FLOW", project.CassetteFlow);
            AddRow(gridSummary, "MapFormat", "MAP FORMAT", project.MapFormat);
            AddRow(gridSummary, "MapDirection", "MAP DIRECTION", project.MapDirection);
            AddRow(gridSummary, "LotId", "LOT ID", project.LotId);
            AddRow(gridSummary, "PartId", "PART ID", project.PartId);

            gridGlobal.Rows.Clear();
            AddRow(gridGlobal, "DryRun", "DRY RUN", ToEnableText(project.DryRun));
            AddRow(gridGlobal, "StepRun", "STEP RUN", ToEnableText(project.StepRun));
            AddRow(gridGlobal, "XmlSave", "XML SAVE", ToEnableText(project.XmlSave));
            AddRow(gridGlobal, "ReDt", "Re-DT", ToEnableText(project.ReDt));
            AddRow(gridGlobal, "EbrMode", "EBR MODE", ToEnableText(project.EbrMode));
            AddRow(gridGlobal, "AlignConfirmEnable", "ALIGN CONFIRM", ToEnableText(project.AlignConfirmEnable));
            AddRow(gridGlobal, "NeedleCheckMode", "NEEDLE CHECK", ToEnableText(project.NeedleCheckMode));
            AddRow(gridGlobal, "AutoPositionDeviationLimit", "AUTO POSITION DEVIATION LIMIT", project.AutoPositionDeviationLimit.ToString("0.###", CultureInfo.InvariantCulture));

            gridProject.Rows.Clear();
            AddRow(gridProject, "ChipThickness", "CHIP THICKNESS", project.ChipThickness.ToString("0.###", CultureInfo.InvariantCulture));
            AddRow(gridProject, "MasterChipThickness", "MASTER CHIP THICKNESS", project.MasterChipThickness.ToString("0.###", CultureInfo.InvariantCulture));
            AddRow(gridProject, "TapeThickness", "TAPE THICKNESS", project.TapeThickness.ToString("0.###", CultureInfo.InvariantCulture));
            AddRow(gridProject, "BinSortNumber", "BIN SORT NUMBER", project.BinSortNumber.ToString(CultureInfo.InvariantCulture));
            AddRow(gridProject, "InputCassetteLevelCount", "INPUT CASSETTE LEVEL COUNT", project.InputCassetteLevelCount.ToString(CultureInfo.InvariantCulture));
            AddRow(gridProject, "GoodCassetteLevelCount", "GOOD CASSETTE LEVEL COUNT", project.GoodCassetteLevelCount.ToString(CultureInfo.InvariantCulture));

            gridXml.Rows.Clear();
            AddRow(gridXml, "InputCassetteId", "INPUT CASSETTE ID", project.InputCassetteId);
            AddRow(gridXml, "OutputCassetteId", "OUTPUT CASSETTE ID", project.OutputCassetteId);
            AddRow(gridXml, "ColletModelNum", "COLLET MODEL NUM", project.ColletModelNum);
            AddRow(gridXml, "ColletLotNum", "COLLET LOT NUM", project.ColletLotNum);
            AddRow(gridXml, "XmlPath", "XML PATH", project.XmlPath);

            gridMap.Rows.Clear();
            AddMapRow("Input", "INPUT DIE MAP", project.InputDieMapFileName, RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.Input));
            AddMapRow("GoodBin", "GOOD BIN DIE MAP", project.GoodBinDieMapFileName, RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.GoodBin));
            AddMapRow("NgBin", "NG BIN DIE MAP", project.NgBinDieMapFileName, RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.NgBin));
            AddMapRow("LegacyOutput", "LEGACY OUTPUT DIE MAP", project.OutputDieMapFileName, ResolveConfiguredRaw(project.OutputDieMapFileName));

            UpdateMapStatus();
            UpdateRecipeStatus(project);
        }

        private static void AddRow(DataGridView grid, string key, string name, object value)
        {
            int row = grid.Rows.Add(key, name, value == null ? "" : value.ToString());
            grid.Rows[row].Cells[NameColumnIndex].Style.BackColor = Color.FromArgb(224, 224, 224);
            grid.Rows[row].Cells[NameColumnIndex].Style.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
        }

        private void AddMapRow(string key, string name, string configured, string resolved)
        {
            int row = gridMap.Rows.Add(key, name, configured ?? "", resolved ?? "", "");
            gridMap.Rows[row].Cells[1].Style.BackColor = Color.FromArgb(224, 224, 224);
            gridMap.Rows[row].Cells[1].Style.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
        }

        private void UpdateMapStatus()
        {
            try
            {
                for (int i = 0; i < gridMap.Rows.Count; i++)
                {
                    DataGridViewRow row = gridMap.Rows[i];
                    string configured = CellText(row, 2);
                    string resolved = ResolveConfiguredRaw(configured);
                    row.Cells[3].Value = resolved;

                    bool exists = !string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved);
                    row.Cells[4].Value = exists ? "OK" : "MISSING";
                    row.Cells[4].Style.BackColor = exists ? Color.FromArgb(210, 245, 210) : Color.FromArgb(255, 230, 210);
                    row.Cells[4].Style.ForeColor = exists ? Color.DarkGreen : Color.DarkRed;
                    row.Cells[4].Style.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                }

                if (_current != null)
                    UpdateRecipeStatus(_current);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void UpdateRecipeStatus(RecipeProject project)
        {
            if (project == null)
                return;

            gridStatus.Rows.Clear();
            string name = NormalizeProjectName(project.FileName);
            string projectPath = Path.Combine(RecipeStore.Dir, name + ".Project");
            string recipeDir = RecipeDataStore.DirOf(name);
            string lastName = RecipeStore.GetLastProjectName();

            AddStatus(".Project file", File.Exists(projectPath), projectPath);
            AddStatus("Unit Recipe Folder", Directory.Exists(recipeDir), recipeDir);
            AddStatus("Last Project Marker", string.Equals(name, lastName, StringComparison.OrdinalIgnoreCase),
                "last=" + (lastName ?? "-"));

            List<string> unitFiles = RecipeDataStore.ListUnits(name);
            AddStatus("Unit Recipe Count", unitFiles.Count > 0, unitFiles.Count.ToString(CultureInfo.InvariantCulture) + " files");

            foreach (string key in new[] { "CDT-320", "InputStageUnit", "PickerFrontUnit", "PickerRearUnit", "VisionUnit", "OutputStageUnit" })
            {
                string path = RecipeDataStore.PathOf(name, key);
                AddStatus(key, File.Exists(path), path);
            }

            for (int i = 0; i < gridMap.Rows.Count; i++)
            {
                DataGridViewRow row = gridMap.Rows[i];
                string mapName = CellText(row, 1);
                string resolved = CellText(row, 3);
                AddStatus(mapName, !string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved), resolved);
            }
        }

        private void AddStatus(string item, bool ok, string detail)
        {
            int row = gridStatus.Rows.Add(item, ok ? "OK" : "MISSING", detail ?? "");
            gridStatus.Rows[row].Cells[1].Style.BackColor = ok ? Color.FromArgb(210, 245, 210) : Color.FromArgb(255, 230, 210);
            gridStatus.Rows[row].Cells[1].Style.ForeColor = ok ? Color.DarkGreen : Color.DarkRed;
            gridStatus.Rows[row].Cells[1].Style.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
        }

        private void OnNew()
        {
            string name = Prompt.Show("새 프로젝트 이름을 입력하세요.", "NEW_PROJECT");
            name = NormalizeProjectName(name);
            if (string.IsNullOrWhiteSpace(name))
                return;

            if (RecipeStore.Load(name) != null)
            {
                QMC.Common.MessageDialog.Show("이미 존재하는 프로젝트입니다.\r\nProject=" + name, "Project",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var project = new RecipeProject { FileName = name };
            EnsureProjectObjects(project);
            RecipeStore.Save(project);
            SaveMachineRecipe(name);
            MarkCurrentProject(name);
            ReloadList();
            SelectAndLoadProject(name);
        }

        private void OnOpen()
        {
            if (listProjects.SelectedItem is string fileName)
                LoadProject(fileName);
            else
                QMC.Common.MessageDialog.Show("프로젝트를 선택하세요.", "Project",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnReload()
        {
            string name = GetCurrentProjectName();
            ReloadList();
            if (!string.IsNullOrWhiteSpace(name))
                SelectAndLoadProject(name);
        }

        private void OnCopy()
        {
            if (_current == null)
                return;

            string sourceName = NormalizeProjectName(_current.FileName);
            string targetName = Prompt.Show("복사할 프로젝트 이름을 입력하세요.", sourceName + "_COPY");
            targetName = NormalizeProjectName(targetName);
            if (string.IsNullOrWhiteSpace(targetName))
                return;

            if (RecipeStore.Load(targetName) != null)
            {
                QMC.Common.MessageDialog.Show("이미 존재하는 프로젝트입니다.\r\nProject=" + targetName, "Project",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                RecipeProject copy = CollectFromUi();
                copy.FileName = targetName;
                RecipeStore.Save(copy);

                string sourceDir = RecipeDataStore.DirOf(sourceName);
                if (Directory.Exists(sourceDir))
                    RecipeDataStore.CopyRecipe(sourceName, targetName);
                else
                    SaveMachineRecipe(targetName);

                MarkCurrentProject(targetName);
                ReloadList();
                SelectAndLoadProject(targetName);
                EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-COPY",
                    "프로젝트를 복사했습니다. source=" + sourceName + ", target=" + targetName);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-COPY",
                    "프로젝트 복사 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show("프로젝트 복사 실패:\r\n" + ex.Message, "Project",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDelete()
        {
            string fileName = listProjects.SelectedItem as string;
            string name = NormalizeProjectName(fileName);
            if (string.IsNullOrWhiteSpace(name))
                return;

            if (QMC.Common.MessageDialog.Show("프로젝트와 Unit Recipe 데이터를 삭제할까요?\r\nProject=" + name,
                    "Project Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                RecipeStore.Delete(name);
                RecipeDataStore.DeleteRecipe(name);
                EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-DEL",
                    "프로젝트를 삭제했습니다. project=" + name);

                if (string.Equals(_loadedProjectName, name, StringComparison.OrdinalIgnoreCase))
                {
                    _current = null;
                    _loadedProjectName = string.Empty;
                }

                ReloadList();
                if (listProjects.Items.Count > 0)
                    LoadProject(listProjects.Items[0] as string);
                else
                    LoadProject(new RecipeProject { FileName = "NEW" }, false);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-DEL",
                    "프로젝트 삭제 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show("프로젝트 삭제 실패:\r\n" + ex.Message, "Project",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSaveAs()
        {
            RecipeProject project = CollectFromUi();
            string name = Prompt.Show("저장할 프로젝트 이름을 입력하세요.", project.FileName);
            name = NormalizeProjectName(name);
            if (string.IsNullOrWhiteSpace(name))
                return;

            project.FileName = name;
            SaveProject(project, true);
        }

        private void OnSaveCurrent()
        {
            RecipeProject project = CollectFromUi();
            if (string.IsNullOrWhiteSpace(project.FileName))
            {
                QMC.Common.MessageDialog.Show("프로젝트 이름이 비어 있습니다.", "Project",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveProject(project, true);
        }

        private void SaveProject(RecipeProject project, bool showMessage)
        {
            try
            {
                EnsureProjectObjects(project);
                project.FileName = NormalizeProjectName(project.FileName);
                RecipeStore.Save(project);
                SaveMachineRecipe(project.FileName);
                MarkCurrentProject(project.FileName);

                _current = project;
                _loadedProjectName = project.FileName;
                ReloadList();
                SelectAndLoadProject(project.FileName);

                EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-SAVE",
                    "프로젝트를 저장했습니다. project=" + project.FileName);
                if (showMessage)
                    QMC.Common.MessageDialog.Show("저장 완료: " + project.FileName, "Project",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-SAVE",
                    "프로젝트 저장 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show("프로젝트 저장 실패:\r\n" + ex.Message, "Project",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnApplyCurrent()
        {
            RecipeProject project = CollectFromUi();
            if (project == null || string.IsNullOrWhiteSpace(project.FileName))
                return;

            ApplyProjectToMachine(project);
            UpdateRecipeStatus(project);
            QMC.Common.MessageDialog.Show("현재 프로젝트를 장비에 적용했습니다.\r\nProject=" + project.FileName,
                "Project", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnOpenRecipeFolder()
        {
            string name = GetCurrentProjectName();
            if (string.IsNullOrWhiteSpace(name))
                return;

            OpenPath(RecipeDataStore.DirOf(name));
        }

        private void OnBrowseMap()
        {
            if (gridMap.CurrentRow == null)
            {
                QMC.Common.MessageDialog.Show("맵 행을 선택하세요.", "Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select Die Map";
                dialog.Filter = "DieMap files|*.json;*.csv|JSON|*.json|CSV|*.csv|All files|*.*";
                dialog.InitialDirectory = RecipeMapPaths.GetDieMapDirectory();
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                gridMap.CurrentRow.Cells[2].Value = RecipeMapPaths.MakeConfigRelativePath(dialog.FileName);
                UpdateMapStatus();
            }
        }

        private void OnOpenMap()
        {
            if (gridMap.CurrentRow == null)
                return;

            string path = CellText(gridMap.CurrentRow, 3);
            if (string.IsNullOrWhiteSpace(path))
                path = CellText(gridMap.CurrentRow, 2);

            OpenPath(path);
        }

        private void OnBrowseXmlPath()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "XML Trace 저장 경로를 선택하세요.";
                string current = GetValue(gridXml, "XmlPath");
                if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
                    dialog.SelectedPath = current;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                SetValue(gridXml, "XmlPath", dialog.SelectedPath);
            }
        }

        private RecipeProject CollectFromUi()
        {
            RecipeProject project = _current ?? new RecipeProject();
            EnsureProjectObjects(project);

            project.FileName = NormalizeProjectName(GetValue(gridSummary, "FileName"));
            project.MachineNumber = GetValue(gridSummary, "MachineNumber");
            project.CassetteFlow = GetValue(gridSummary, "CassetteFlow");
            project.MapFormat = GetValue(gridSummary, "MapFormat");
            project.MapDirection = GetValue(gridSummary, "MapDirection");
            project.LotId = GetValue(gridSummary, "LotId");
            project.PartId = GetValue(gridSummary, "PartId");

            project.DryRun = ParseEnable(GetValue(gridGlobal, "DryRun"));
            project.StepRun = ParseEnable(GetValue(gridGlobal, "StepRun"));
            project.XmlSave = ParseEnable(GetValue(gridGlobal, "XmlSave"));
            project.ReDt = ParseEnable(GetValue(gridGlobal, "ReDt"));
            project.EbrMode = ParseEnable(GetValue(gridGlobal, "EbrMode"));
            project.AlignConfirmEnable = ParseEnable(GetValue(gridGlobal, "AlignConfirmEnable"));
            project.NeedleCheckMode = ParseEnable(GetValue(gridGlobal, "NeedleCheckMode"));
            project.AutoPositionDeviationLimit = ParseDouble(GetValue(gridGlobal, "AutoPositionDeviationLimit"), project.AutoPositionDeviationLimit);

            project.ChipThickness = ParseDouble(GetValue(gridProject, "ChipThickness"), project.ChipThickness);
            project.MasterChipThickness = ParseDouble(GetValue(gridProject, "MasterChipThickness"), project.MasterChipThickness);
            project.TapeThickness = ParseDouble(GetValue(gridProject, "TapeThickness"), project.TapeThickness);
            project.BinSortNumber = ParseInt(GetValue(gridProject, "BinSortNumber"), project.BinSortNumber);
            project.InputCassetteLevelCount = Clamp(ParseInt(GetValue(gridProject, "InputCassetteLevelCount"), project.InputCassetteLevelCount), 1, 2);
            project.GoodCassetteLevelCount = Clamp(ParseInt(GetValue(gridProject, "GoodCassetteLevelCount"), project.GoodCassetteLevelCount), 1, 2);

            project.InputCassetteId = GetValue(gridXml, "InputCassetteId");
            project.OutputCassetteId = GetValue(gridXml, "OutputCassetteId");
            project.ColletModelNum = GetValue(gridXml, "ColletModelNum");
            project.ColletLotNum = GetValue(gridXml, "ColletLotNum");
            project.XmlPath = GetValue(gridXml, "XmlPath");

            project.InputDieMapFileName = GetMapConfigured("Input");
            project.GoodBinDieMapFileName = GetMapConfigured("GoodBin");
            project.NgBinDieMapFileName = GetMapConfigured("NgBin");
            project.OutputDieMapFileName = GetMapConfigured("LegacyOutput");

            return project;
        }

        private void ApplyProjectToMachine(RecipeProject project)
        {
            if (project == null)
                return;

            try
            {
                string name = NormalizeProjectName(project.FileName);
                var host = FindForm() as Form1;
                RecipeStore.SaveLastProjectName(name);
                AppSettingsStore.Current.LastProject = name;
                AppSettingsStore.Save();
                host?.LoadMachineRecipe(name);
                host?.RefreshProjectName(name);
                host?.Controller?.ApplyRecipeMode(project);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-APPLY",
                    "프로젝트 장비 적용 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private void SaveMachineRecipe(string recipeName)
        {
            try
            {
                var host = FindForm() as Form1;
                host?.SaveMachineRecipe(NormalizeProjectName(recipeName));
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-DATA-SAVE",
                    "Unit Recipe 저장 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void MarkCurrentProject(string name)
        {
            string normalized = NormalizeProjectName(name);
            RecipeStore.SaveLastProjectName(normalized);
            AppSettingsStore.Current.LastProject = normalized;
            AppSettingsStore.Save();
            lblCurrentProject.Text = "Current Project: " + normalized;
        }

        private static void EnsureProjectObjects(RecipeProject project)
        {
            if (project == null)
                return;
            if (project.Die == null) project.Die = new DieSubset();
            if (project.Frame == null) project.Frame = new TapeFrameSubset();
            if (project.LoadFrame == null) project.LoadFrame = new LoadTapeFrameSubset();
            if (project.UnloadFrame == null) project.UnloadFrame = new UnloadTapeFrameSubset();
            if (project.Module == null) project.Module = new ModuleSubset();
            if (project.BottomInsp == null) project.BottomInsp = new InspectionSubset();
            if (project.FrontSideInsp == null) project.FrontSideInsp = new InspectionSubset();
            if (project.RearSideInsp == null) project.RearSideInsp = new InspectionSubset();
            if (project.Output == null) project.Output = new OutputSubset();
            if (project.Pickup == null) project.Pickup = new PickupSubset();
            if (project.InputPickup == null) project.InputPickup = ClonePickup(project.Pickup);
            if (project.OutputPickup == null) project.OutputPickup = ClonePickup(project.Pickup);
        }

        private static PickupSubset ClonePickup(PickupSubset source)
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

        private string GetCurrentProjectName()
        {
            if (_current != null && !string.IsNullOrWhiteSpace(_current.FileName))
                return NormalizeProjectName(_current.FileName);
            if (!string.IsNullOrWhiteSpace(_loadedProjectName))
                return NormalizeProjectName(_loadedProjectName);
            if (listProjects.SelectedItem is string selected)
                return NormalizeProjectName(selected);
            return string.Empty;
        }

        private static string NormalizeProjectName(string recipeName)
        {
            if (string.IsNullOrWhiteSpace(recipeName))
                return string.Empty;

            string name = recipeName.Trim();
            if (name.EndsWith(".Project", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - ".Project".Length);

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }

        private static string ToEnableText(bool value)
        {
            return value ? "ENABLE" : "DISABLE";
        }

        private static bool ParseEnable(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string text = value.Trim();
            return text.Equals("ENABLE", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        private static double ParseDouble(string value, double fallback)
        {
            double result;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
                   double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result)
                ? result
                : fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            int result;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ||
                   int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out result)
                ? result
                : fallback;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static string GetValue(DataGridView grid, string key)
        {
            DataGridViewRow row = FindRow(grid, key);
            return row == null ? "" : CellText(row, ValueColumnIndex);
        }

        private static void SetValue(DataGridView grid, string key, string value)
        {
            DataGridViewRow row = FindRow(grid, key);
            if (row != null)
                row.Cells[ValueColumnIndex].Value = value ?? "";
        }

        private static DataGridViewRow FindRow(DataGridView grid, string key)
        {
            if (grid == null || string.IsNullOrWhiteSpace(key))
                return null;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                if (string.Equals(CellText(row, KeyColumnIndex), key, StringComparison.OrdinalIgnoreCase))
                    return row;
            }

            return null;
        }

        private string GetMapConfigured(string key)
        {
            DataGridViewRow row = FindMapRow(key);
            return row == null ? "" : CellText(row, 2);
        }

        private DataGridViewRow FindMapRow(string key)
        {
            foreach (DataGridViewRow row in gridMap.Rows)
            {
                if (row.IsNewRow) continue;
                if (string.Equals(CellText(row, 0), key, StringComparison.OrdinalIgnoreCase))
                    return row;
            }

            return null;
        }

        private static string CellText(DataGridViewRow row, int index)
        {
            if (row == null || index < 0 || index >= row.Cells.Count)
                return string.Empty;
            object value = row.Cells[index].Value;
            return value == null ? string.Empty : value.ToString();
        }

        private static string ResolveConfiguredRaw(string configured)
        {
            if (string.IsNullOrWhiteSpace(configured))
                return string.Empty;
            if (Path.IsPathRooted(configured))
                return configured;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configured);
        }

        private static void OpenPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return;

                string target = path;
                if (File.Exists(target))
                    target = Path.GetDirectoryName(target);

                if (!Directory.Exists(target))
                {
                    QMC.Common.MessageDialog.Show("경로를 찾을 수 없습니다.\r\nPath=" + path, "Open Path",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Process.Start(target);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("경로 열기 실패:\r\n" + ex.Message, "Open Path",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal static class Prompt
    {
        public static string Show(string question, string defaultValue = "")
        {
            using (var form = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowIcon = false,
                ClientSize = new Size(420, 130),
                Text = question
            })
            {
                var label = new Label { Location = new Point(12, 12), AutoSize = true, Text = question };
                var textBox = new TextBox { Location = new Point(12, 40), Size = new Size(396, 24), Text = defaultValue };
                var ok = new Button { Location = new Point(240, 80), Size = new Size(80, 28), Text = "OK", DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
                var cancel = new Button { Location = new Point(328, 80), Size = new Size(80, 28), Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
                form.Controls.Add(label);
                form.Controls.Add(textBox);
                form.Controls.Add(ok);
                form.Controls.Add(cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;
                return form.ShowDialog() == DialogResult.OK ? textBox.Text : null;
            }
        }
    }
}
