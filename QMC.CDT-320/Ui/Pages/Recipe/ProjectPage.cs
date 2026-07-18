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
        private const string DieOwnerToolTip = "Recipe → 다이 사양 페이지에서 설정합니다.";
        private const string WaferOwnerToolTip = "Recipe → 웨이퍼 사양 페이지에서 설정합니다.";

        private RecipeProject _current;
        private string _loadedProjectName = string.Empty;
        private bool _loading;
        private static readonly string[] ColletTypeOptions = { "Flat", "Rim" };

        public ProjectPage()
        {
            InitializeComponent();
            ConfigureRuntimeUi();
            DisableColumnSorting();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            ReloadList();
            string last = RecipeStore.GetLastProjectName();
            if (!string.IsNullOrWhiteSpace(last))
                SelectAndLoadProject(last);
            else if (listProjects.Items.Count > 0)
                LoadProject(listProjects.Items[0] as string);
            else
                LoadProject(new RecipeProject { FileName = "NEW" });
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible || _loading || listProjects == null || gridProject == null ||
                LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            string name = GetCurrentProjectName();
            if (string.IsNullOrWhiteSpace(name))
                return;

            RecipeProject latest = RecipeStore.Load(name);
            if (latest != null)
                LoadProject(latest);
        }

        /// <summary>모든 그리드의 헤더 클릭 정렬(오름/내림차순) 기능을 끈다.</summary>
        private void DisableColumnSorting()
        {
            var grids = new[] { gridSummary, gridGlobal, gridProject, gridXml, gridMap, gridStatus };
            foreach (var grid in grids)
                foreach (DataGridViewColumn col in grid.Columns)
                    col.SortMode = DataGridViewColumnSortMode.NotSortable;
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
            gridMap.Columns.Add(new DataGridViewTextBoxColumn { Name = "Configured", HeaderText = "Project Map File", FillWeight = 44 });
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

        private void listProjects_DoubleClick(object sender, EventArgs e) => OnOpen();

        private void listProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_loading && listProjects.SelectedItem is string fileName)
                LoadProject(fileName);
        }

        private void btnNew_Click(object sender, EventArgs e) => OnNew();
        private void btnOpen_Click(object sender, EventArgs e) => OnOpen();
        private void btnCopy_Click(object sender, EventArgs e) => OnCopy();
        private void btnDelete_Click(object sender, EventArgs e) => OnDelete();
        private void btnOpenFolder_Click(object sender, EventArgs e) => OpenPath(RecipeStore.Dir);
        private void btnReload_Click(object sender, EventArgs e) => OnReload();
        private void btnOpenRecipeFolder_Click(object sender, EventArgs e) => OnOpenRecipeFolder();
        private void btnBrowseMap_Click(object sender, EventArgs e) => OnBrowseMap();
        private void btnOpenMap_Click(object sender, EventArgs e) => OnOpenMap();
        private void btnBrowseXml_Click(object sender, EventArgs e) => OnBrowseXmlPath();
        private void btnApplyCurrent_Click(object sender, EventArgs e) => OnApplyCurrent();
        private void btnSaveRecipe_Click(object sender, EventArgs e) => OnSaveCurrent();
        private void btnSaveAs_Click(object sender, EventArgs e) => OnSaveAs();

        private void gridMap_CellEndEdit(object sender, DataGridViewCellEventArgs e) => UpdateMapStatus();

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

            LoadProject(project);
        }

        private void LoadProject(RecipeProject project)
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
                //if (applyToMachine)
                //    ApplyProjectToMachine(project);

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
            lblCurrentProject.Text = "Selected Project: " + (project.FileName ?? "-");

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
            AddReadOnlyRow(gridGlobal, "ChipThickness", "CHIP THICKNESS (um, DIE에서 자동)", project.ChipThickness.ToString("0.###", CultureInfo.InvariantCulture),
                "Recipe → 다이 사양 → Thickness (mm)에서 자동 계산됩니다.");
            AddRow(gridGlobal, "MasterChipThickness", "MASTER CHIP THICKNESS", project.MasterChipThickness.ToString("0.###", CultureInfo.InvariantCulture));
            AddRow(gridGlobal, "TapeThickness", "TAPE THICKNESS", project.TapeThickness.ToString("0.###", CultureInfo.InvariantCulture));
            AddComboRow(gridGlobal, "ColletType", "COLLET TYPE", project.ColletZ.ColletType.ToString(), ColletTypeOptions);
            AddRow(gridGlobal, "BinSortNumber", "BIN SORT NUMBER", project.BinSortNumber.ToString(CultureInfo.InvariantCulture));
            AddRow(gridGlobal, "InputCassetteLevelCount", "INPUT CASSETTE LEVEL COUNT", project.InputCassetteLevelCount.ToString(CultureInfo.InvariantCulture));
            AddRow(gridGlobal, "GoodCassetteLevelCount", "GOOD CASSETTE LEVEL COUNT", project.GoodCassetteLevelCount.ToString(CultureInfo.InvariantCulture));
            AddReadOnlyRow(gridGlobal, "LegacyOutputDieMapFileName", "LEGACY OUTPUT DIE MAP", project.OutputDieMapFileName, WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputFrameSpecName", "INPUT FRAME SPEC NAME", project.InputFrame.FrameSpecName, WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputFrameSpecName", "OUTPUT FRAME SPEC NAME", project.OutputFrame.FrameSpecName, WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputOuterDiameterMm", "INPUT WAFER DIAMETER (mm)", project.InputFrame.OuterDiameterMm.ToString("0.###", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputOuterDiameterMm", "OUTPUT WAFER DIAMETER (mm)", project.OutputFrame.OuterDiameterMm.ToString("0.###", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputRotate", "INPUT FRAME ROTATE (RESERVED)", project.InputFrame.Rotate,
                WaferOwnerToolTip + " 현재 공정 좌표에는 아직 적용되지 않습니다.");
            AddReadOnlyRow(gridGlobal, "OutputRotate", "OUTPUT FRAME ROTATE (RESERVED)", project.OutputFrame.Rotate,
                WaferOwnerToolTip + " 현재 공정 좌표에는 아직 적용되지 않습니다.");
            AddReadOnlyRow(gridGlobal, "InputEdgeSkipMode", "INPUT EDGE SKIP MODE", project.InputFrame.EdgeSkipMode, WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputEdgeSkipMode", "OUTPUT EDGE SKIP MODE", project.OutputFrame.EdgeSkipMode, WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputSideEdgeSkip", "INPUT EDGE SKIP L/R (grid)", project.InputFrame.SideEdgeSkip.ToString(CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputTopBottomEdgeSkip", "INPUT EDGE SKIP T/B (grid)", project.InputFrame.TopBottomEdgeSkip.ToString(CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputSideEdgeSkip", "OUTPUT EDGE SKIP L/R (grid)", project.OutputFrame.SideEdgeSkip.ToString(CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputTopBottomEdgeSkip", "OUTPUT EDGE SKIP T/B (grid)", project.OutputFrame.TopBottomEdgeSkip.ToString(CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputSideEdgeSkipMm", "INPUT EDGE SKIP L/R (mm)", project.InputFrame.SideEdgeSkipMm.ToString("0.###", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputTopBottomEdgeSkipMm", "INPUT EDGE SKIP T/B (mm)", project.InputFrame.TopBottomEdgeSkipMm.ToString("0.###", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputSideEdgeSkipMm", "OUTPUT EDGE SKIP L/R (mm)", project.OutputFrame.SideEdgeSkipMm.ToString("0.###", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputTopBottomEdgeSkipMm", "OUTPUT EDGE SKIP T/B (mm)", project.OutputFrame.TopBottomEdgeSkipMm.ToString("0.###", CultureInfo.InvariantCulture), WaferOwnerToolTip);

            gridProject.Rows.Clear();
            AddReadOnlyRow(gridProject, "DieSpecName", "DIE SPEC NAME", project.Die.DieSpecName, DieOwnerToolTip);
            AddReadOnlyRow(gridProject, "DieWidthMm", "DIE WIDTH (mm)", project.Die.WidthMm.ToString("0.######", CultureInfo.InvariantCulture), DieOwnerToolTip);
            AddReadOnlyRow(gridProject, "DieHeightMm", "DIE HEIGHT (mm)", project.Die.HeightMm.ToString("0.######", CultureInfo.InvariantCulture), DieOwnerToolTip);
            AddReadOnlyRow(gridProject, "DieThicknessMm", "DIE THICKNESS (mm)", project.Die.ThicknessMm.ToString("0.######", CultureInfo.InvariantCulture), DieOwnerToolTip);
            AddReadOnlyRow(gridProject, "InputPitchX", "INPUT PITCH GAP X (mm)", project.InputFrame.PitchX.ToString("0.######", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridProject, "InputPitchY", "INPUT PITCH GAP Y (mm)", project.InputFrame.PitchY.ToString("0.######", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridProject, "OutputPitchX", "OUTPUT PITCH GAP X (mm)", project.OutputFrame.PitchX.ToString("0.######", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridProject, "OutputPitchY", "OUTPUT PITCH GAP Y (mm)", project.OutputFrame.PitchY.ToString("0.######", CultureInfo.InvariantCulture), WaferOwnerToolTip);

            gridXml.Rows.Clear();
            AddRow(gridXml, "InputCassetteId", "INPUT CASSETTE ID", project.InputCassetteId);
            AddRow(gridXml, "OutputCassetteId", "OUTPUT CASSETTE ID", project.OutputCassetteId);
            AddRow(gridXml, "ColletModelNum", "COLLET MODEL NUM", project.ColletModelNum);
            AddRow(gridXml, "ColletLotNum", "COLLET LOT NUM", project.ColletLotNum);
            AddRow(gridXml, "XmlPath", "XML PATH", project.XmlPath);

            gridMap.Rows.Clear();
            AddMapRow(
                "InputBase",
                "INPUT BASE WAFER MAP",
                RecipeMapPaths.ConfiguredBaseFileName(project, RecipeMapKind.Input),
                ResolveBaseWaferMapPath(project, RecipeMapKind.Input),
                true);
            AddMapRow(
                "OutputBase",
                "OUTPUT BASE WAFER MAP",
                RecipeMapPaths.ConfiguredBaseFileName(project, RecipeMapKind.GoodBin),
                ResolveBaseWaferMapPath(project, RecipeMapKind.GoodBin),
                true);
            AddMapRow("Input", "INPUT DIE MAP (AUTO)", project.InputDieMapFileName, RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.Input), true);
            AddMapRow("GoodBin", "GOOD BIN DIE MAP (AUTO)", project.GoodBinDieMapFileName, RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.GoodBin), true);
            AddMapRow("NgBin", "NG BIN DIE MAP (AUTO)", project.NgBinDieMapFileName, RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.NgBin), true);

            UpdateMapStatus();
            UpdateRecipeStatus(project);
        }

        private static void AddRow(DataGridView grid, string key, string name, object value)
        {
            int row = grid.Rows.Add(key, name, value == null ? "" : value.ToString());
            grid.Rows[row].Cells[NameColumnIndex].Style.BackColor = Color.FromArgb(224, 224, 224);
            grid.Rows[row].Cells[NameColumnIndex].Style.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
        }

        private static void AddReadOnlyRow(DataGridView grid, string key, string name, object value, string toolTip)
        {
            int row = grid.Rows.Add(key, name, value == null ? "" : value.ToString());
            grid.Rows[row].Cells[NameColumnIndex].Style.BackColor = Color.FromArgb(224, 224, 224);
            grid.Rows[row].Cells[NameColumnIndex].Style.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            grid.Rows[row].Cells[ValueColumnIndex].ReadOnly = true;
            grid.Rows[row].Cells[ValueColumnIndex].Style.BackColor = Color.FromArgb(238, 238, 238);
            grid.Rows[row].Cells[NameColumnIndex].ToolTipText = toolTip ?? "";
            grid.Rows[row].Cells[ValueColumnIndex].ToolTipText = toolTip ?? "";
        }

        private static void AddComboRow(DataGridView grid, string key, string name, string value, string[] options)
        {
            string selected = string.IsNullOrWhiteSpace(value) ? "Flat" : value;
            if (Array.IndexOf(options ?? new string[0], selected) < 0)
                selected = options != null && options.Length > 0 ? options[0] : "";

            int row = grid.Rows.Add(key, name, selected);
            grid.Rows[row].Cells[NameColumnIndex].Style.BackColor = Color.FromArgb(224, 224, 224);
            grid.Rows[row].Cells[NameColumnIndex].Style.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);

            var combo = new DataGridViewComboBoxCell();
            combo.FlatStyle = FlatStyle.Flat;
            combo.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
            combo.Items.AddRange(options ?? new string[0]);
            combo.Value = selected;
            grid.Rows[row].Cells[ValueColumnIndex] = combo;
        }

        private void AddMapRow(string key, string name, string configured, string resolved, bool configuredReadOnly)
        {
            int row = gridMap.Rows.Add(key, name, configured ?? "", resolved ?? "", "");
            gridMap.Rows[row].Cells[1].Style.BackColor = Color.FromArgb(224, 224, 224);
            gridMap.Rows[row].Cells[1].Style.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            gridMap.Rows[row].Cells[2].ReadOnly = configuredReadOnly;
            if (configuredReadOnly)
            {
                gridMap.Rows[row].Cells[2].Style.BackColor = Color.FromArgb(238, 238, 238);
                string toolTip = key.EndsWith("Base", StringComparison.OrdinalIgnoreCase)
                    ? "Recipe → 웨이퍼 사양 → LOAD WAFER MAP에서 설정합니다."
                    : "Base WaferMap과 Recipe → 웨이퍼 사양의 Role별 Pitch로 자동 생성됩니다.";
                gridMap.Rows[row].Cells[1].ToolTipText = toolTip;
                gridMap.Rows[row].Cells[2].ToolTipText = toolTip;
            }
            else
            {
                gridMap.Rows[row].Cells[2].ToolTipText = "MAP BROWSE 버튼으로 원본 WaferMap을 불러옵니다.";
            }
        }

        private void UpdateMapStatus()
        {
            try
            {
                for (int i = 0; i < gridMap.Rows.Count; i++)
                {
                    DataGridViewRow row = gridMap.Rows[i];
                    string key = CellText(row, 0);
                    string configured = CellText(row, 2);
                    string resolved;
                    if (string.Equals(key, "InputBase", StringComparison.OrdinalIgnoreCase))
                        resolved = ResolveBaseWaferMapPath(_current, RecipeMapKind.Input, configured);
                    else if (string.Equals(key, "OutputBase", StringComparison.OrdinalIgnoreCase))
                        resolved = ResolveBaseWaferMapPath(_current, RecipeMapKind.GoodBin, configured);
                    else
                        resolved = ResolveConfiguredRaw(configured);
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

            bool dieSizeConsistent = project.Die != null && project.InputFrame != null && project.OutputFrame != null &&
                                     NearlyEqual(project.Die.WidthMm, project.InputFrame.DieSizeX) &&
                                     NearlyEqual(project.Die.HeightMm, project.InputFrame.DieSizeY) &&
                                     NearlyEqual(project.Die.WidthMm, project.OutputFrame.DieSizeX) &&
                                     NearlyEqual(project.Die.HeightMm, project.OutputFrame.DieSizeY);
            string dieSizeDetail = project.Die == null
                ? "Project Die 없음"
                : "Project=" + project.Die.WidthMm.ToString("0.######", CultureInfo.InvariantCulture) + "x" +
                  project.Die.HeightMm.ToString("0.######", CultureInfo.InvariantCulture) +
                  ", InputFrame=" + FormatFrameDieSize(project.InputFrame) +
                  ", OutputFrame=" + FormatFrameDieSize(project.OutputFrame);
            AddStatus("Die Size Consistency", dieSizeConsistent, dieSizeDetail);

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

            try
            {
                var project = new RecipeProject { FileName = name };
                EnsureProjectObjects(project);

                if (!RecipeStore.Save(project))
                    throw new IOException("Project 파일 생성에 실패했습니다.");

                if (!SaveMachineRecipe(name))
                {
                    RecipeStore.Delete(name);
                    RecipeDataStore.DeleteRecipe(name);

                    throw new IOException(
                        "Unit Recipe 생성에 실패하여 새 프로젝트 생성을 취소했습니다.");
                }

                //MarkCurrentProject(name);
                ReloadList();
                SelectAndLoadProject(name);
            }
            catch (Exception ex)
            {
                EventLogger.Write(
                    EventKind.Alarm,
                    Security.UserSession.Name,
                    "RECIPE-NEW",
                    "새 프로젝트 생성 실패: " + ex.Message);

                QMC.Common.MessageDialog.Show(
                    "새 프로젝트 생성 실패:\r\n" + ex.Message,
                    "Project",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            //기존 코드
            //var project = new RecipeProject { FileName = name };
            //EnsureProjectObjects(project);
            //RecipeStore.Save(project);
            //SaveMachineRecipe(name);
            //MarkCurrentProject(name);
            //ReloadList();
            //SelectAndLoadProject(name);
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
                RecipeProject source = CollectFromUi();
                source.FileName = sourceName;
                RecipeProjectCloneResult cloneResult = RecipeProjectCloneService.Clone(source, targetName);
                if (!cloneResult.Success)
                    throw new InvalidOperationException(cloneResult.Message);

                if (!cloneResult.UnitRecipeCopied)
                {
                    RollbackClonedProject(targetName);

                    throw new IOException(
                        "원본 프로젝트의 Unit Recipe 파일이 없어 복사를 취소했습니다. source=" + sourceName);
                }

                //MarkCurrentProject(targetName);
                ReloadList();
                SelectAndLoadProject(targetName);
                EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-COPY",
                    "프로젝트를 독립 복사했습니다. source=" + sourceName +
                    ", target=" + targetName +
                    ", unitRecipeCopied=" + cloneResult.UnitRecipeCopied +
                    ", mapFiles=" + cloneResult.MapFileCount);
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

            var host = FindForm() as Form1;
            if (host != null &&
                string.Equals(
                    NormalizeProjectName(host.ActiveRecipeName),
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                QMC.Common.MessageDialog.Show(
                    "현재 장비에 적용된 Recipe는 삭제할 수 없습니다.\r\n" +
                    "다른 Recipe를 먼저 Apply한 후 삭제하세요.\r\n\r\n" +
                    "Active Recipe=" + name,
                    "Project Delete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (QMC.Common.MessageDialog.Show(
                    "프로젝트와 Unit Recipe 데이터를 삭제할까요?\r\nProject=" + name,
                    "Project Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

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
                    LoadProject(new RecipeProject { FileName = "NEW" });
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
            OnCopy();
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

                if (!RecipeStore.Save(project))
                    throw new IOException("Project 파일 저장에 실패했습니다.");

                //if (!SaveMachineRecipe(project.FileName))
                //{
                //    throw new IOException(
                //        "Unit Recipe 저장에 실패했습니다. Project 파일은 저장되었지만 장비 레시피 저장이 완료되지 않았습니다.");
                //}
                //MarkCurrentProject(project.FileName);

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
            try
            {
                RecipeProject project = CollectFromUi();
                if (project == null || string.IsNullOrWhiteSpace(project.FileName))
                    return;

                if (!RecipeStore.Save(project))
                    throw new IOException("Project 파일 저장에 실패했습니다.");
                _current = project;
                PopulateProjectToUi(project);
                ApplyProjectToMachine(project);
                UpdateRecipeStatus(project);
                QMC.Common.MessageDialog.Show("현재 프로젝트를 장비에 적용했습니다.\r\nProject=" + project.FileName,
                    "Project", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-APPLY",
                    "프로젝트 적용 준비 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show("프로젝트 적용 실패:\r\n" + ex.Message, "Project",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private static bool NearlyEqual(double left, double right)
        {
            return Math.Abs(left - right) <= 0.000001;
        }

        private static string FormatFrameDieSize(TapeFrameSubset frame)
        {
            return frame == null
                ? "-"
                : frame.DieSizeX.ToString("0.######", CultureInfo.InvariantCulture) + "x" +
                  frame.DieSizeY.ToString("0.######", CultureInfo.InvariantCulture);
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
            QMC.Common.MessageDialog.Show(
                "Base WaferMap은 Recipe → 웨이퍼 사양 → LOAD WAFER MAP에서 불러옵니다.",
                "Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private RecipeProject LoadLatestProjectForEditing()
        {
            string sourceName = NormalizeProjectName(_loadedProjectName);
            if (!string.IsNullOrWhiteSpace(sourceName))
            {
                RecipeProject latest = RecipeStore.Load(sourceName);
                if (latest != null)
                    return latest;
            }

            return _current ?? new RecipeProject();
        }

        private RecipeProject CollectFromUi()
        {
            RecipeProject project = LoadLatestProjectForEditing();
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

            project.MasterChipThickness = ParseDouble(GetValue(gridGlobal, "MasterChipThickness"), project.MasterChipThickness);
            project.TapeThickness = ParseDouble(GetValue(gridGlobal, "TapeThickness"), project.TapeThickness);
            project.ColletZ.ColletType = ParseColletType(GetValue(gridGlobal, "ColletType"), project.ColletZ.ColletType);
            project.BinSortNumber = ParseInt(GetValue(gridGlobal, "BinSortNumber"), project.BinSortNumber);
            project.InputCassetteLevelCount = Clamp(ParseInt(GetValue(gridGlobal, "InputCassetteLevelCount"), project.InputCassetteLevelCount), 1, 2);
            project.GoodCassetteLevelCount = Clamp(ParseInt(GetValue(gridGlobal, "GoodCassetteLevelCount"), project.GoodCassetteLevelCount), 1, 2);
            project.InputCassetteId = GetValue(gridXml, "InputCassetteId");
            project.OutputCassetteId = GetValue(gridXml, "OutputCassetteId");
            project.ColletModelNum = GetValue(gridXml, "ColletModelNum");
            project.ColletLotNum = GetValue(gridXml, "ColletLotNum");
            project.XmlPath = GetValue(gridXml, "XmlPath");

            return project;
        }

        private void ApplyProjectToMachine(RecipeProject project)
        {
            if (project == null)
                return;

            try
            {
                var host = FindForm() as Form1;
                if (host == null)
                    throw new InvalidOperationException("메인 화면을 찾을 수 없습니다.");

                if (!host.ApplyMachineRecipe(project))
                {
                    throw new InvalidOperationException(
                        "장비 Recipe 적용이 완료되지 않았습니다. Project=" + project.FileName);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(
                    EventKind.Alarm,
                    Security.UserSession.Name,
                    "RECIPE-APPLY",
                    "프로젝트 장비 적용 실패: " + ex.Message);

                throw;
            }
            finally
            {
            }
        }

        private bool SaveMachineRecipe(string recipeName)
        {
            try
            {
                var host = FindForm() as Form1;
                return host != null && host.SaveMachineRecipe(NormalizeProjectName(recipeName));
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-DATA-SAVE",
                    "Unit Recipe 저장 실패: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private void RollbackClonedProject(string recipeName)
        {
            try
            {
                string normalized = NormalizeProjectName(recipeName);
                RecipeStore.Delete(normalized);
                RecipeDataStore.DeleteRecipe(normalized);
                EventLogger.Write(EventKind.Warning, Security.UserSession.Name, "RECIPE-COPY-ROLLBACK",
                    "프로젝트 복사 롤백을 완료했습니다. project=" + normalized);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-COPY-ROLLBACK",
                    "프로젝트 복사 롤백 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private static void EnsureProjectObjects(RecipeProject project)
        {
            if (project == null)
                return;
            RecipeProjectConsistencyService.EnsureStructure(project);
            if (project.ColletZ == null) project.ColletZ = new ColletZConfigSubset();
            project.ColletZ.Ensure();
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
            bool parsed = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
                          double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
            return parsed && !double.IsNaN(result) && !double.IsInfinity(result) ? result : fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            int result;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ||
                   int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out result)
                ? result
                : fallback;
        }

        private static ColletShapeType ParseColletType(string value, ColletShapeType fallback)
        {
            ColletShapeType parsed;
            return Enum.TryParse(value, true, out parsed) ? parsed : fallback;
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

        private static string ResolveBaseWaferMapPath(
            RecipeProject project,
            RecipeMapKind kind,
            string configured = null)
        {
            try
            {
                string configuredPath = ResolveConfiguredRaw(
                    configured ?? RecipeMapPaths.ConfiguredBaseFileName(project, kind));
                if (!string.IsNullOrWhiteSpace(configuredPath))
                    return configuredPath;

                return project != null
                    ? RecipeDieMapResolver.ResolveExternalSourcePath(project, kind)
                    : "";
            }
            catch
            {
                return "";
            }
            finally
            {
            }
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
