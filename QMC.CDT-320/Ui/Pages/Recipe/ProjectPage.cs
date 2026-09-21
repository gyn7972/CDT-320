using QMC.CDT_320.Ui.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Data.Store;
using QMC.Common.Logging;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class ProjectPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private const int KeyColumnIndex = 0;
        private const int NameColumnIndex = 1;
        private const int ValueColumnIndex = 2;
        private const string DieOwnerToolTip = "레시피 → 다이 사양에서 항목을 수정한 뒤 상단 저장(SAVE)으로 저장·적용합니다.\r\n사양 화면은 현재 장비에 적용 중인 레시피를 편집합니다. 다른 레시피를 선택했다면 적용 대상을 먼저 확인하세요.";
        private const string WaferOwnerToolTip = "레시피 → 웨이퍼 사양에서 입력(INPUT) 또는 출력(OUTPUT) 역할을 선택하고 항목을 수정한 뒤 상단 저장(SAVE)으로 저장·적용합니다.\r\n사양 화면은 현재 장비에 적용 중인 레시피를 편집합니다.";
        private const string ProjectEditToolTip = "이 화면에서 값을 수정한 뒤 [프로젝트 정보 저장]으로 프로젝트 파일을 저장합니다.\r\n장비에 적용하려면 [선택 레시피 장비 적용]을 사용합니다. 적용 전 레시피·자재 상태 검사를 따르세요.";
        private const string ProcessMapOwnerToolTip = "[웨이퍼·맵 설정 열기] → [공정 맵 설정 · 사용 모드 / 형식 / 회전 / 좌표 기준]에서 변경합니다.\r\n입력 원격/등록 모드와 입력·출력 회전/좌표 기준을 변경한 뒤 [설정 저장 및 현재 레시피 적용]을 누르세요. 활성 레시피에서만 저장할 수 있습니다.";

        private RecipeProject _current;
        private RecipeProject _monitorProject;
        private string _mapCheckSignature;
        private string _loadedProjectName = string.Empty;
        private bool _loading;
        private bool _parameterSaveActionPending;
        private string _lastMonitorError = string.Empty;
        private Dictionary<string, string> _savedEditableValues = new Dictionary<string, string>();
        private static readonly string[] ColletTypeOptions = { "Flat", "Rim" };

        public ProjectPage()
        {
            InitializeComponent();
            InitializeRecipeLanguageBindings();
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
            timerSettingsMonitor.Enabled = Visible;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (timerSettingsMonitor != null)
                timerSettingsMonitor.Enabled = Visible && !IsDesignerMode() && _current != null;
            if (!Visible || _loading || _parameterSaveActionPending || listProjects == null || gridProject == null ||
                LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            string name = GetCurrentProjectName();
            if (string.IsNullOrWhiteSpace(name))
                return;

            // 다른 화면을 다녀와도 저장하지 않은 편집값을 조용히 덮어쓰지 않는다.
            if (HasUnsavedProjectEdits()) return;
            RecipeProject latest = RecipeStore.Load(name);
            if (latest != null)
                LoadProject(latest);
        }

        /// <summary>모든 그리드의 헤더 클릭 정렬(오름/내림차순) 기능을 끈다.</summary>
        private void DisableColumnSorting()
        {
            var grids = new[] { gridSummary, gridGlobal, gridProject, gridXml, gridMap, gridStatus, gridInputMap, gridOutputMap, gridMapApproval };
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
            ConfigureEditableGrid(gridInputMap);
            ConfigureEditableGrid(gridOutputMap);
            ConfigureMapGrid();
            gridMapApproval.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridMapApproval.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "맵", FillWeight = 16 });
            gridMapApproval.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "확인 상태", FillWeight = 25 });
            gridMapApproval.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "내용 / 다음 작업", FillWeight = 85 });
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
            grid.ShowCellToolTips = true;

            var key = new DataGridViewTextBoxColumn { Name = "Key", HeaderText = "Key", Visible = false };
            var name = new DataGridViewTextBoxColumn { Name = "Property", HeaderText = "항목", FillWeight = 42, ReadOnly = true };
            var value = new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "설정값", FillWeight = 58 };
            grid.Columns.Add(key);
            grid.Columns.Add(name);
            grid.Columns.Add(value);
            Lang.BindKey(name, "recipeUi.projectColumn.item");
            Lang.BindKey(value, "recipeUi.projectColumn.value");
        }

        private void ConfigureMapGrid()
        {
            gridMap.AutoGenerateColumns = false;
            gridMap.Columns.Clear();
            gridMap.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridMap.MultiSelect = false;
            gridMap.EditMode = DataGridViewEditMode.EditOnEnter;
            gridMap.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            gridMap.Columns.Add(new DataGridViewTextBoxColumn { Name = "Key", HeaderText = "Key", Visible = false });
            gridMap.Columns.Add(new DataGridViewTextBoxColumn { Name = "Map", HeaderText = "맵 구분", FillWeight = 32, ReadOnly = true });
            gridMap.Columns.Add(new DataGridViewTextBoxColumn { Name = "Configured", HeaderText = "등록 맵 파일", FillWeight = 38 });
            gridMap.Columns.Add(new DataGridViewTextBoxColumn { Name = "Resolved", HeaderText = "실제 파일 경로", FillWeight = 50, ReadOnly = true });
            gridMap.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "상태", FillWeight = 18, ReadOnly = true });
            Lang.BindKey(gridMap.Columns["Map"], "recipeUi.projectColumn.map");
            Lang.BindKey(gridMap.Columns["Configured"], "recipeUi.projectColumn.configured");
            Lang.BindKey(gridMap.Columns["Resolved"], "recipeUi.projectColumn.resolved");
            Lang.BindKey(gridMap.Columns["Status"], "recipeUi.projectColumn.status");
        }

        private void ConfigureStatusGrid()
        {
            gridStatus.AutoGenerateColumns = false;
            gridStatus.Columns.Clear();
            gridStatus.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridStatus.MultiSelect = false;
            gridStatus.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            gridStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "Item", HeaderText = "점검 항목", FillWeight = 34 });
            gridStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "상태", FillWeight = 18 });
            gridStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "Detail", HeaderText = "상세 내용", FillWeight = 64 });
            Lang.BindKey(gridStatus.Columns["Item"], "recipeUi.projectColumn.check");
            Lang.BindKey(gridStatus.Columns["Status"], "recipeUi.projectColumn.status");
            Lang.BindKey(gridStatus.Columns["Detail"], "recipeUi.projectColumn.detail");
        }

        private void listProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_loading && listProjects.SelectedItem is string fileName)
            {
                if (!ConfirmDiscardProjectEdits("다른 레시피 불러오기"))
                {
                    _loading = true;
                    try { listProjects.SelectedItem = _loadedProjectName; }
                    finally { _loading = false; }
                    return;
                }
                LoadProject(fileName);
            }
        }

        private async void menuNewRecipe_Click(object sender, EventArgs e) => await RunAfterParameterSaveAsync(OnNew);
        private async void btnCopy_Click(object sender, EventArgs e) => await RunAfterParameterSaveAsync(OnCopy);
        private async void menuDeleteRecipe_Click(object sender, EventArgs e) => await RunAfterParameterSaveAsync(OnDelete);
        private void menuProjectFolder_Click(object sender, EventArgs e) => OpenPath(RecipeStore.Dir);
        private void btnReload_Click(object sender, EventArgs e) => OnReload();
        private void menuRecipeFolder_Click(object sender, EventArgs e) => OnOpenRecipeFolder();
        private void btnBrowseMap_Click(object sender, EventArgs e) => OnBrowseMap();
        private void menuMapFolder_Click(object sender, EventArgs e) => OnOpenMap();
        private void menuXmlPath_Click(object sender, EventArgs e) => OnBrowseXmlPath();
        private async void btnApplyCurrent_Click(object sender, EventArgs e) => await RunAfterParameterSaveAsync(OnApplyCurrent);
        private async void btnSaveRecipe_Click(object sender, EventArgs e) => await RunAfterParameterSaveAsync(OnSaveCurrent);
        private void btnManage_Click(object sender, EventArgs e) => menuManagement.Show(btnManage, new Point(0, btnManage.Height));

        private void menuManagement_Opening(object sender, CancelEventArgs e)
        {
            if (_parameterSaveActionPending) { e.Cancel = true; return; }
            var host = FindForm() as Form1;
            bool hasProject = _current != null && !string.IsNullOrWhiteSpace(_loadedProjectName);
            menuDeleteRecipe.Enabled = hasProject && (host == null || !string.Equals(
                NormalizeProjectName(host.ActiveRecipeName), _loadedProjectName, StringComparison.OrdinalIgnoreCase));
            menuRecipeFolder.Enabled = hasProject;
            menuMapFolder.Enabled = hasProject && gridMap.CurrentRow != null;
            menuXmlPath.Enabled = hasProject;
        }

        private Dictionary<string, string> CaptureEditableValues()
        {
            var values = new Dictionary<string, string>();
            foreach (DataGridView grid in new[] { gridSummary, gridGlobal, gridXml })
                foreach (DataGridViewRow row in grid.Rows)
                    if (!row.IsNewRow && !row.Cells[ValueColumnIndex].ReadOnly)
                        values[grid.Name + "/" + CellText(row, KeyColumnIndex)] = CellText(row, ValueColumnIndex);
            return values;
        }

        private bool HasUnsavedProjectEdits()
        {
            if (_current == null || _savedEditableValues.Count == 0) return false;
            foreach (DataGridView grid in new[] { gridSummary, gridGlobal, gridXml }) grid.EndEdit();
            return HasPendingProjectEdits();
        }

        private bool HasPendingProjectEdits()
        {
            foreach (var item in CaptureEditableValues())
            {
                string saved;
                if (!_savedEditableValues.TryGetValue(item.Key, out saved) || saved != item.Value) return true;
            }
            return false;
        }

        private bool ConfirmDiscardProjectEdits(string action)
        {
            return !HasUnsavedProjectEdits() || QMC.Common.MessageDialog.Show(this,
                "이 화면에서 수정한 프로젝트 정보가 아직 저장되지 않았습니다.\r\n" +
                "수정한 내용을 버리고 계속 진행할까요?\r\n진행할 작업: " + action,
                "미저장 정보 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private async Task RunAfterParameterSaveAsync(Action action)
        {
            if (_parameterSaveActionPending || IsDisposed || Disposing) return;
            _parameterSaveActionPending = true;
            bool wasEnabled = Enabled;
            Enabled = false;
            try
            {
                // 대기 중 다른 프로젝트를 선택하거나 같은 파일 작업을 중복 요청하지 못하게 합니다.
                // 다른 메인 화면 전환은 계속 가능하며, 파일 작업 직전에만 비동기로 저장을 기다립니다.
                var host = FindForm() as Form1;
                if (host == null || !await host.FlushParameterSavesBeforeRecipeFileActionAsync()) return;
                if (IsDisposed || Disposing) return;
                action();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-FILE-ACTION",
                    "Recipe 파일 작업을 완료하지 못했습니다. " + ex);
                if (!IsDisposed && !Disposing)
                    QMC.Common.MessageDialog.Show(this, ex.Message, "Recipe 파일 작업", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _parameterSaveActionPending = false;
                if (!IsDisposed && !Disposing) Enabled = wasEnabled;
            }
        }

        private void gridProjectEdit_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!_loading && _current != null) UpdateProjectIdentity();
        }

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
                QMC.Common.MessageDialog.Show("프로젝트 파일을 불러오지 못했습니다.\r\nProject=" + name, "프로젝트",
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
                QMC.Common.MessageDialog.Show("프로젝트 로드 처리 실패:\r\n" + ex.Message, "프로젝트",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loading = false;
            }
        }

        private void PopulateProjectToUi(RecipeProject project)
        {
            _monitorProject = project;
            gridSummary.Rows.Clear();
            AddRow(gridSummary, "FileName", "레시피 이름", project.FileName);
            AddRow(gridSummary, "MachineNumber", "장비 번호", project.MachineNumber);
            AddRow(gridSummary, "CassetteFlow", "카세트 처리 방식", project.CassetteFlow == "Mapping" ? "매핑" : project.CassetteFlow);
            AddRow(gridSummary, "LotId", "로트 식별자", project.LotId);
            AddRow(gridSummary, "PartId", "제품 식별자", project.PartId);
            UpdateMapSettingsMonitor(project, AppSettingsStore.Current);

            gridGlobal.Rows.Clear();
            AddRow(gridGlobal, "DryRun", "공운전", ToEnableText(project.DryRun));
            AddRow(gridGlobal, "StepRun", "단계별 운전", ToEnableText(project.StepRun));
            AddRow(gridGlobal, "XmlSave", "XML 저장", ToEnableText(project.XmlSave));
            AddRow(gridGlobal, "ReDt", "재작업 (Re-DT)", ToEnableText(project.ReDt));
            AddRow(gridGlobal, "EbrMode", "EBR 모드", ToEnableText(project.EbrMode));
            AddRow(gridGlobal, "AlignConfirmEnable", "정렬 확인", ToEnableText(project.AlignConfirmEnable));
            AddRow(gridGlobal, "NeedleCheckMode", "니들 확인", ToEnableText(project.NeedleCheckMode));
            AddRow(gridGlobal, "AutoPositionDeviationLimit", "자동 위치 편차 한계", project.AutoPositionDeviationLimit.ToString("0.###", CultureInfo.InvariantCulture));
            AddReadOnlyRow(gridGlobal, "ChipThickness", "칩 두께 (μm, 다이에서 자동)", project.ChipThickness.ToString("0.###", CultureInfo.InvariantCulture),
                "레시피 → 다이 사양 → 두께(Thickness, mm)에서 자동 계산됩니다.");
            AddRow(gridGlobal, "MasterChipThickness", "기준 칩 두께", project.MasterChipThickness.ToString("0.###", CultureInfo.InvariantCulture));
            AddRow(gridGlobal, "TapeThickness", "테이프 두께", project.TapeThickness.ToString("0.###", CultureInfo.InvariantCulture));
            AddComboRow(gridGlobal, "ColletType", "콜렛 형태", project.ColletZ.ColletType.ToString(), ColletTypeOptions);
            AddRow(gridGlobal, "BinSortNumber", "빈 분류 번호", project.BinSortNumber.ToString(CultureInfo.InvariantCulture));
            AddRow(gridGlobal, "InputCassetteLevelCount", "입력 카세트 층 수", project.InputCassetteLevelCount.ToString(CultureInfo.InvariantCulture));
            AddRow(gridGlobal, "GoodCassetteLevelCount", "양품 카세트 층 수", project.GoodCassetteLevelCount.ToString(CultureInfo.InvariantCulture));
            AddReadOnlyRow(gridGlobal, "LegacyOutputDieMapFileName", "기존 출력 다이 맵", project.OutputDieMapFileName, WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputFrameSpecName", "입력 웨이퍼 사양 이름", project.InputFrame.FrameSpecName, WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputFrameSpecName", "출력 웨이퍼 사양 이름", project.OutputFrame.FrameSpecName, WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputOuterDiameterMm", "입력 웨이퍼 직경 (mm)", project.InputFrame.OuterDiameterMm.ToString("0.###", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputOuterDiameterMm", "출력 웨이퍼 직경 (mm)", project.OutputFrame.OuterDiameterMm.ToString("0.###", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputEdgeSkipMode", "입력 가장자리 제외 방식", FormatEdgeSkipMode(project.InputFrame.EdgeSkipMode), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputEdgeSkipMode", "출력 가장자리 제외 방식", FormatEdgeSkipMode(project.OutputFrame.EdgeSkipMode), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputSideEdgeSkip", "입력 좌우 가장자리 제외 (칸)", project.InputFrame.SideEdgeSkip.ToString(CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputTopBottomEdgeSkip", "입력 상하 가장자리 제외 (칸)", project.InputFrame.TopBottomEdgeSkip.ToString(CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputSideEdgeSkip", "출력 좌우 가장자리 제외 (칸)", project.OutputFrame.SideEdgeSkip.ToString(CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputTopBottomEdgeSkip", "출력 상하 가장자리 제외 (칸)", project.OutputFrame.TopBottomEdgeSkip.ToString(CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputSideEdgeSkipMm", "입력 좌우 가장자리 제외 (mm)", project.InputFrame.SideEdgeSkipMm.ToString("0.###", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "InputTopBottomEdgeSkipMm", "입력 상하 가장자리 제외 (mm)", project.InputFrame.TopBottomEdgeSkipMm.ToString("0.###", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputSideEdgeSkipMm", "출력 좌우 가장자리 제외 (mm)", project.OutputFrame.SideEdgeSkipMm.ToString("0.###", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            AddReadOnlyRow(gridGlobal, "OutputTopBottomEdgeSkipMm", "출력 상하 가장자리 제외 (mm)", project.OutputFrame.TopBottomEdgeSkipMm.ToString("0.###", CultureInfo.InvariantCulture), WaferOwnerToolTip);

            gridProject.Rows.Clear();
            UpdateReadOnlyReferences(project);

            gridXml.Rows.Clear();
            AddRow(gridXml, "InputCassetteId", "입력 카세트 식별자", project.InputCassetteId);
            AddRow(gridXml, "OutputCassetteId", "출력 카세트 식별자", project.OutputCassetteId);
            AddRow(gridXml, "ColletModelNum", "콜렛 모델 번호", project.ColletModelNum);
            AddRow(gridXml, "ColletLotNum", "콜렛 로트 번호", project.ColletLotNum);
            AddRow(gridXml, "XmlPath", "XML 저장 경로", project.XmlPath);

            PopulateMapFileRows(project);
            _savedEditableValues = CaptureEditableValues();
            UpdateProjectIdentity();
        }

        private void PopulateMapFileRows(RecipeProject project)
        {
            gridMap.Rows.Clear();
            AddMapRow(
                "InputBase",
                "입력 기준 웨이퍼 맵",
                RecipeMapPaths.ConfiguredBaseFileName(project, RecipeMapKind.Input),
                ResolveBaseWaferMapPath(project, RecipeMapKind.Input),
                true);
            AddMapRow(
                "OutputBase",
                "출력 기준 웨이퍼 맵",
                RecipeMapPaths.ConfiguredBaseFileName(project, RecipeMapKind.GoodBin),
                ResolveBaseWaferMapPath(project, RecipeMapKind.GoodBin),
                true);
            AddMapRow("Input", "입력 다이 맵 (자동 생성)", project.InputDieMapFileName, RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.Input), true);
            AddMapRow("GoodBin", "양품 다이 맵 (자동 생성)", project.GoodBinDieMapFileName, RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.GoodBin), true);
            AddMapRow("NgBin", "불량 다이 맵 (자동 생성)", project.NgBinDieMapFileName, RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.NgBin), true);

            UpdateMapStatus();
            UpdateRecipeStatus(project);
        }

        private void RefreshMapChecksIfChanged(RecipeProject project)
        {
            // 파일이나 설정이 같으면 큰 맵을 2초마다 다시 파싱하지 않는다.
            string signature;
            using (var stream = new MemoryStream())
            {
                new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(RecipeProject)).WriteObject(stream, project);
                signature = Convert.ToBase64String(stream.ToArray());
            }
            foreach (RecipeMapKind kind in new[] { RecipeMapKind.Input, RecipeMapKind.GoodBin, RecipeMapKind.NgBin })
            {
                string path = RecipeMapPaths.ResolveConfigured(project, kind);
                var info = string.IsNullOrWhiteSpace(path) ? null : new FileInfo(path);
                signature += "|" + path + "|" + (info != null && info.Exists ? info.Length + ":" + info.LastWriteTimeUtc.Ticks : "missing");
                string basePath = ResolveBaseWaferMapPath(project, kind);
                signature += "|" + basePath + "|" + (!string.IsNullOrWhiteSpace(basePath) && File.Exists(basePath));
            }
            signature += "|" + RecipeInputMapSource.UsesRemote(project, AppSettingsStore.Current) + "|" + AppSettingsStore.Current.NetworkWaferMapFolder;
            if (signature == _mapCheckSignature) return;
            PopulateMapFileRows(project);
            _mapCheckSignature = signature;
        }

        private void timerSettingsMonitor_Tick(object sender, EventArgs e)
        {
            if (!ShouldRefreshVisible(this) || _loading || _parameterSaveActionPending || _current == null)
                return;
            try
            {
                // 선택한 저장 레시피만 다시 읽는다. 편집 중인 값과 장비 활성 레시피는 변경하지 않는다.
                RecipeProject latest = RecipeStore.Load(_loadedProjectName);
                if (latest == null)
                    throw new IOException("선택 레시피의 저장값을 읽을 수 없습니다. recipe=" + _loadedProjectName);
                EnsureProjectObjects(latest);
                _monitorProject = latest;
                UpdateMapSettingsMonitor(latest, AppSettingsStore.Current);
                RefreshMapChecksIfChanged(latest);
                UpdateReadOnlyReferences(latest);
                UpdateProjectIdentity();
                _lastMonitorError = string.Empty;
            }
            catch (Exception ex)
            {
                foreach (DataGridView grid in new[] { gridInputMap, gridOutputMap })
                    foreach (DataGridViewRow row in grid.Rows)
                        row.Cells[ValueColumnIndex].Value = "확인 불가 · 새로고침 필요";
                gridMapApproval.Rows.Clear();
                gridMapApproval.Rows.Add("전체", "확인 실패", ex.Message);
                _mapCheckSignature = null;
                Lang.BindFormat(lblCurrentProject, "recipeUi.projectPage.lblCurrentProject.text", (object)(_loadedProjectName), (object)(ex.Message));
                lblCurrentProject.ForeColor = Color.Firebrick;
                if (!string.Equals(_lastMonitorError, ex.Message, StringComparison.Ordinal))
                    EventLogger.Write(EventKind.Warning, Security.UserSession.Name, "RECIPE-MONITOR", "설정 모니터 갱신 실패: " + ex);
                _lastMonitorError = ex.Message;
            }
        }

        private void UpdateProjectIdentity()
        {
            var host = FindForm() as Form1;
            string active = host != null ? NormalizeProjectName(host.ActiveRecipeName) : string.Empty;
            string selected = NormalizeProjectName(_loadedProjectName);
            bool same = !string.IsNullOrWhiteSpace(active) && string.Equals(active, selected, StringComparison.OrdinalIgnoreCase);
            bool edited = HasPendingProjectEdits();
            string applyStatus = host == null ? "장비 반영 상태 확인 불가" : host.GetProjectConfigurationStatus(_monitorProject ?? _current);
            Lang.BindFormat(lblCurrentProject, "recipeUi.projectPage.lblCurrentProject.state2", (object)(selected), (object)((string.IsNullOrWhiteSpace(active) ? "확인되지 않음" : active)), (object)((edited ? "● 미저장 변경 있음" : "저장된 프로젝트 정보")), (object)(applyStatus));
            lblCurrentProject.ForeColor = edited ? Color.Firebrick : same ? Color.FromArgb(35, 65, 90) : Color.DarkOrange;
        }

        private void UpdateMapSettingsMonitor(RecipeProject project, AppSettings settings)
        {
            bool network = RecipeInputMapSource.UsesRemote(project, settings);
            bool inherited = project.InputMapProcessing == null || project.InputMapProcessing.Format == WaferMapSourceFormat.Legacy;
            // 원격 수신의 ResolveConfiguredFormat과 같은 우선순위. 비활성 Recipe도 선택한 파일 기준으로 표시한다.
            WaferMapSourceFormat format = inherited
                ? settings != null && string.Equals((settings.NetworkWaferMapFormat ?? "").Trim(), "Camtek", StringComparison.OrdinalIgnoreCase)
                    ? WaferMapSourceFormat.Camtek : WaferMapSourceFormat.Samsung
                : project.InputMapProcessing.Format;
            string formatText = format == WaferMapSourceFormat.Samsung || format == WaferMapSourceFormat.Rad ? "삼성 (RAD)" :
                format == WaferMapSourceFormat.Camtek ? "CAMTEK" : format == WaferMapSourceFormat.Circle ? "서클 (등록 생성 맵)" : "미지원 형식 (" + format + ")";
            SetReadOnlyRow(gridInputMap, "MonitorInputSource", "입력 맵 사용",
                RecipeInputMapSource.DescribeMode(project, settings) + (project.InputUseRemoteWaferMap.HasValue ? "" : " · 구형 공통값"),
                ProcessMapOwnerToolTip + "\r\n입력 맵 사용에서 원격/등록 모드를 선택합니다. 공정 중 저장하면 현재 웨이퍼는 유지하고 다음 웨이퍼부터 적용합니다. 구형 레시피는 저장 전까지 기존 공통값을 따릅니다. 레시피 이름으로 모드를 자동 변경하지 않습니다.");
            SetReadOnlyRow(gridInputMap, "MonitorInputFormat", "입력 원격 맵 형식",
                settings == null && inherited ? "설정 확인 불가" : (network ? "" : "미적용 · ") + formatText + (inherited ? " (공통 설정)" : " (레시피 설정)"),
                ProcessMapOwnerToolTip + "\r\n설정 대상 입력(INPUT) → 원격 맵 구분자를 선택하세요. 원격 사용 시에만 파일 형식을 검사합니다.\r\n'기존 설정 사용'이면 설정 → 일반의 구형 형식 기본값을 따릅니다. 이 창에서 삼성(RAD) 또는 CAMTEK을 지정하면 레시피에 고정됩니다. 형식 불일치는 알람 처리됩니다.");
            AddMapTransformMonitor(project.InputMapProcessing, "Input", "INPUT");
            SetReadOnlyRow(gridOutputMap, "MonitorOutputSource", "출력 맵 사용", "등록 맵 · 양품 / 불량",
                "출력은 다운로드 없이 등록된 양품/불량 맵을 사용합니다.\r\n" + WaferOwnerToolTip +
                "\r\n출력 다이 맵 생성에서 양품/불량 각각 대상·제외을 확인하고 [맵 확인 및 적용]을 누르세요.");
            AddMapTransformMonitor(project.OutputMapProcessing, "Output", "OUTPUT");
            SetReadOnlyRow(gridInputMap, "MonitorInputBarcode", "입력 바코드",
                settings == null ? "설정 확인 불가" : settings.UseInputWaferBarcode ? "사용" : network ? "미사용 · 원격 사용 시 필수" : "미사용",
                "설정 → 바코드(BARCODE) → 입력 웨이퍼 바코드(INPUT WAFER BARCODE)에서 [USE INPUT WAFER BARCODE]를 변경하고 [SAVE BARCODE SETTINGS]로 저장합니다.\r\n원격 맵을 사용하려면 바코드 판독이 반드시 켜져 있어야 합니다.");
        }

        private void AddMapTransformMonitor(WaferMapProcessSettings settings, string key, string role)
        {
            int angle = settings != null ? settings.RotationDegrees : 0;
            WaferMapGridOrigin origin = settings != null ? settings.GridOrigin : WaferMapGridOrigin.TopLeft;
            string originText;
            switch (origin)
            {
                case WaferMapGridOrigin.TopLeft: originText = "좌상단 (X→ Y↓)"; break;
                case WaferMapGridOrigin.BottomLeft: originText = "좌하단 (X→ Y↑)"; break;
                case WaferMapGridOrigin.TopRight: originText = "우상단 (X← Y↓)"; break;
                case WaferMapGridOrigin.BottomRight: originText = "우하단 (X← Y↑)"; break;
                case WaferMapGridOrigin.Center: originText = "센터 (X→ Y↑)"; break;
                default: originText = "잘못된 설정 (" + origin + ")"; break;
            }
            string roleText = role == "OUTPUT" ? "출력" : "입력";
            string owner = ProcessMapOwnerToolTip + "\r\n설정 대상 " + roleText + "(" + role + ")을 선택하세요.";
            if (role == "OUTPUT") owner += " 양품/불량은 동일한 회전·기준점을 사용합니다.";
            SetReadOnlyRow(role == "OUTPUT" ? gridOutputMap : gridInputMap, "Monitor" + key + "Rotation", roleText + " 맵 회전",
                angle.ToString(CultureInfo.InvariantCulture) + "°" + (angle == 0 || angle == 180 ? "" : " · 공정 사용 불가"),
                owner + "\r\n'맵 회전'에서 0°/180°를 선택합니다. 기준 맵에 추가 적용하는 소프트웨어 회전이며, 생성 시 저장한 방향이나 기구 T축 각도와는 별도입니다.\r\n이 값은 레시피 설정입니다. 현재 웨이퍼에 이미 준비된 맵은 작업 화면에서 확인하세요.");
            SetReadOnlyRow(role == "OUTPUT" ? gridOutputMap : gridInputMap, "Monitor" + key + "Origin", roleText + " 좌표 기준 (1,1)", originText,
                owner + "\r\n'Grid 1,1 기준'에서 좌상단·좌하단·우상단·우하단·센터를 선택합니다.\r\n회전 후 기준점을 적용합니다. 뷰어와 결과 파일은 같은 1 기준 X/Y이며, 센터는 음수·소수 좌표가 있을 수 있습니다.");
        }

        private void UpdateReadOnlyReferences(RecipeProject project)
        {
            SetReadOnlyRow(gridProject, "DieSpecName", "다이 사양 이름", project.Die.DieSpecName, DieOwnerToolTip);
            SetReadOnlyRow(gridProject, "DieSize", "다이 크기 X / Y (mm)",
                project.Die.WidthMm.ToString("0.######", CultureInfo.InvariantCulture) + " / " + project.Die.HeightMm.ToString("0.######", CultureInfo.InvariantCulture), DieOwnerToolTip);
            SetReadOnlyRow(gridProject, "DieThicknessMm", "다이 두께 (mm)", project.Die.ThicknessMm.ToString("0.######", CultureInfo.InvariantCulture), DieOwnerToolTip);
            SetReadOnlyRow(gridProject, "InputPitch", "입력 간격 X / Y (mm)",
                project.InputFrame.PitchX.ToString("0.######", CultureInfo.InvariantCulture) + " / " + project.InputFrame.PitchY.ToString("0.######", CultureInfo.InvariantCulture), WaferOwnerToolTip);
            SetReadOnlyRow(gridProject, "OutputPitch", "출력 간격 X / Y (mm)",
                project.OutputFrame.PitchX.ToString("0.######", CultureInfo.InvariantCulture) + " / " + project.OutputFrame.PitchY.ToString("0.######", CultureInfo.InvariantCulture), WaferOwnerToolTip);
        }

        private static void SetReadOnlyRow(DataGridView grid, string key, string name, object value, string toolTip)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (!string.Equals(CellText(row, KeyColumnIndex), key, StringComparison.Ordinal)) continue;
                string text = value == null ? "" : value.ToString();
                if (CellText(row, ValueColumnIndex) != text) row.Cells[ValueColumnIndex].Value = text;
                row.Cells[NameColumnIndex].ToolTipText = toolTip;
                row.Cells[ValueColumnIndex].ToolTipText = toolTip;
                return;
            }
            AddReadOnlyRow(grid, key, name, value, toolTip);
        }

        private static void AddRow(DataGridView grid, string key, string name, object value)
        {
            int row = grid.Rows.Add(key, name, value == null ? "" : value.ToString());
            grid.Rows[row].Cells[NameColumnIndex].Style.BackColor = Color.FromArgb(224, 224, 224);
            grid.Rows[row].Cells[NameColumnIndex].Style.Font = grid.ColumnHeadersDefaultCellStyle.Font;
            grid.Rows[row].Cells[NameColumnIndex].ToolTipText = ProjectEditToolTip;
            grid.Rows[row].Cells[ValueColumnIndex].ToolTipText = ProjectEditToolTip;
        }

        private static void AddReadOnlyRow(DataGridView grid, string key, string name, object value, string toolTip)
        {
            int row = grid.Rows.Add(key, name, value == null ? "" : value.ToString());
            grid.Rows[row].Cells[NameColumnIndex].Style.BackColor = Color.FromArgb(224, 224, 224);
            grid.Rows[row].Cells[NameColumnIndex].Style.Font = grid.ColumnHeadersDefaultCellStyle.Font;
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
            grid.Rows[row].Cells[NameColumnIndex].Style.Font = grid.ColumnHeadersDefaultCellStyle.Font;

            var combo = new DataGridViewComboBoxCell();
            combo.FlatStyle = FlatStyle.Flat;
            combo.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
            var choices = new List<KeyValuePair<string, string>>();
            foreach (string option in options ?? new string[0])
                choices.Add(new KeyValuePair<string, string>(option, option == "Flat" ? "평면형" : option == "Rim" ? "테두리형" : option));
            combo.DisplayMember = "Value";
            combo.ValueMember = "Key";
            combo.DataSource = choices;
            combo.Value = selected;
            grid.Rows[row].Cells[ValueColumnIndex] = combo;
            grid.Rows[row].Cells[NameColumnIndex].ToolTipText = ProjectEditToolTip;
            combo.ToolTipText = ProjectEditToolTip;
        }

        private void AddMapRow(string key, string name, string configured, string resolved, bool configuredReadOnly)
        {
            int row = gridMap.Rows.Add(key, name, configured ?? "", resolved ?? "", "");
            gridMap.Rows[row].Cells[1].Style.BackColor = Color.FromArgb(224, 224, 224);
            gridMap.Rows[row].Cells[1].Style.Font = gridMap.ColumnHeadersDefaultCellStyle.Font;
            gridMap.Rows[row].Cells[2].ReadOnly = configuredReadOnly;
            if (configuredReadOnly)
            {
                gridMap.Rows[row].Cells[2].Style.BackColor = Color.FromArgb(238, 238, 238);
                string role = key.StartsWith("Input", StringComparison.OrdinalIgnoreCase) ? "입력" : "출력";
                string toolTip = key.EndsWith("Base", StringComparison.OrdinalIgnoreCase)
                    ? "레시피 → 웨이퍼 사양 → " + role + " 역할에서 웨이퍼 맵 불러오기 또는 맵 생성 미리보기 저장으로 설정합니다."
                    : "기준 맵과 웨이퍼 사양으로 생성된 등록 맵입니다.\r\n레시피 → " +
                        (role == "입력" ? "입력 다이 맵 생성" : "출력 다이 맵 생성 → " + (key == "NgBin" ? "불량(NG)" : "양품(GOOD)")) +
                        "에서 대상/제외를 확인·수정하고 [맵 확인 및 적용]으로 확정합니다.";
                toolTip += "\r\n회전·좌표 기준은 상단 입력/출력 맵 설정에 표시됩니다.\r\n파일 경로: " + (resolved ?? "");
                for (int column = 1; column < gridMap.Columns.Count; column++)
                    gridMap.Rows[row].Cells[column].ToolTipText = toolTip;
            }
            else
            {
                gridMap.Rows[row].Cells[2].ToolTipText = "웨이퍼·맵 설정 화면에서 원본 웨이퍼 맵을 불러옵니다.";
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
                    row.Cells[4].Value = exists ? "파일 있음" : "파일 없음";
                    row.Cells[4].Style.BackColor = exists ? Color.FromArgb(210, 245, 210) : Color.FromArgb(255, 230, 210);
                    row.Cells[4].Style.ForeColor = exists ? Color.DarkGreen : Color.DarkRed;
                    row.Cells[4].Style.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                }

                RecipeProject project = _monitorProject ?? _current;
                if (project != null)
                {
                    UpdateRecipeStatus(project);
                    UpdateMapApprovalSummary(project);
                }
            }
            catch (Exception ex)
            {
                gridMapApproval.Rows.Clear();
                gridMapApproval.Rows.Add("전체", "확인 실패", ex.Message);
                EventLogger.Write(EventKind.Warning, Security.UserSession.Name, "RECIPE-MAP-STATUS", "맵 상태 확인 실패: " + ex);
            }
        }

        private void UpdateMapApprovalSummary(RecipeProject project)
        {
            gridMapApproval.Rows.Clear();
            foreach (RecipeMapKind kind in new[] { RecipeMapKind.Input, RecipeMapKind.GoodBin, RecipeMapKind.NgBin })
            {
                string status, detail;
                bool approved = CheckRegisteredMap(project, kind, out status, out detail);
                bool remote = kind == RecipeMapKind.Input && RecipeInputMapSource.UsesRemote(project, AppSettingsStore.Current);
                if (remote)
                {
                    detail = "바코드 판독 후 수신 파일 검사 · 등록 맵: " + status + (approved ? "" : " · " + detail);
                    status = "원격 수신 시 검사";
                }
                int index = gridMapApproval.Rows.Add(kind == RecipeMapKind.Input ? "입력" : kind == RecipeMapKind.GoodBin ? "출력 양품" : "출력 불량", status, detail);
                var row = gridMapApproval.Rows[index];
                row.Cells[1].Style.BackColor = remote ? Color.FromArgb(225, 239, 250) : approved ? Color.FromArgb(220, 240, 220) : Color.FromArgb(255, 238, 211);
                foreach (DataGridViewCell cell in row.Cells) cell.ToolTipText = detail;
            }
        }

        private static bool CheckRegisteredMap(RecipeProject project, RecipeMapKind kind, out string status, out string detail)
        {
            status = "맵 준비 필요";
            detail = "웨이퍼·맵 설정에서 맵을 준비한 뒤 다이 맵 생성에서 확인·적용하세요.";
            try
            {
                string configured = project.MapApprovalVersion > 0
                    ? RecipeMapPaths.ExactConfiguredFileName(project, kind) : RecipeMapPaths.ConfiguredFileName(project, kind);
                string path = RecipeMapPaths.ResolveConfiguredPath(configured);
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
                DieMap map = DieMapGenerator.Load(path);
                string reason;
                TapeFrameSubset frame = kind == RecipeMapKind.Input ? project.InputFrame ?? project.Frame : project.OutputFrame ?? project.Frame;
                if (!RecipeDieMapResolver.IsSupportedForEquipment(map, out reason) ||
                    !RecipeDieMapResolver.IsCompatibleWithFrame(map, frame, out reason) ||
                    !RecipeMapPaths.IsMapApproved(project, kind, map, out reason))
                {
                    status = "맵 재확인 필요";
                    detail = reason;
                    return false;
                }
                status = project.MapApprovalVersion > 0 ? "맵 승인 완료" : "구형 호환 맵";
                detail = project.MapApprovalVersion > 0
                    ? "등록 파일·웨이퍼 사양·맵 승인 일치. 운전 조건은 시작 시 별도로 검사합니다."
                    : "기존 호환 규칙으로 확인했습니다. 신규 맵 승인 완료를 뜻하지 않습니다.";
                return true;
            }
            catch (Exception ex)
            {
                status = "맵 읽기 실패";
                detail = ex.Message;
                return false;
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

            int folderRow = gridStatus.Rows.Add("원격 맵 공통 폴더", "설정 위치: 설정 → 일반", AppSettingsStore.Current.NetworkWaferMapFolder ?? "미설정");
            foreach (DataGridViewCell cell in gridStatus.Rows[folderRow].Cells)
                cell.ToolTipText = "모든 레시피가 같은 폴더에서 바코드와 파일명이 일치하는 맵을 찾습니다. 경로 변경과 연결 확인은 설정 → 일반에서 진행합니다.";
            AddStatus("프로젝트 파일", File.Exists(projectPath), projectPath);
            AddStatus("장치별 레시피 폴더", Directory.Exists(recipeDir), recipeDir);
            int markerRow = gridStatus.Rows.Add("마지막 레시피 기록",
                string.Equals(name, lastName, StringComparison.OrdinalIgnoreCase) ? "선택 레시피와 같음" : "다른 레시피 · 참고",
                "마지막 레시피=" + (lastName ?? "-"));
            gridStatus.Rows[markerRow].Cells[1].Style.ForeColor = Color.DimGray;

            List<string> unitFiles = RecipeDataStore.ListUnits(name);
            AddStatus("장치별 레시피 파일 수", unitFiles.Count > 0, unitFiles.Count.ToString(CultureInfo.InvariantCulture) + "개");

            bool dieSizeConsistent = project.Die != null && project.InputFrame != null && project.OutputFrame != null &&
                                     NearlyEqual(project.Die.WidthMm, project.InputFrame.DieSizeX) &&
                                     NearlyEqual(project.Die.HeightMm, project.InputFrame.DieSizeY) &&
                                     NearlyEqual(project.Die.WidthMm, project.OutputFrame.DieSizeX) &&
                                     NearlyEqual(project.Die.HeightMm, project.OutputFrame.DieSizeY);
            string dieSizeDetail = project.Die == null
                ? "프로젝트 다이 사양 없음"
                : "프로젝트=" + project.Die.WidthMm.ToString("0.######", CultureInfo.InvariantCulture) + "x" +
                  project.Die.HeightMm.ToString("0.######", CultureInfo.InvariantCulture) +
                  ", 입력 웨이퍼=" + FormatFrameDieSize(project.InputFrame) +
                  ", 출력 웨이퍼=" + FormatFrameDieSize(project.OutputFrame);
            AddStatus("다이 크기 일치 여부", dieSizeConsistent, dieSizeDetail);

            foreach (string key in new[] { "CDT-320", "InputStageUnit", "PickerFrontUnit", "PickerRearUnit", "VisionUnit", "OutputStageUnit" })
            {
                string path = RecipeDataStore.PathOf(name, key);
                string unitName = key == "CDT-320" ? "장비 공통" : key == "InputStageUnit" ? "입력 스테이지" :
                    key == "PickerFrontUnit" ? "전면 헤드" : key == "PickerRearUnit" ? "후면 헤드" :
                    key == "VisionUnit" ? "비전" : "출력 스테이지";
                AddStatus(unitName, File.Exists(path), path);
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
            int row = gridStatus.Rows.Add(item, ok ? "정상" : "확인 필요", detail ?? "");
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
                QMC.Common.MessageDialog.Show("이미 존재하는 프로젝트입니다.\r\nProject=" + name, "프로젝트",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var project = new RecipeProject { FileName = name, InputUseRemoteWaferMap = AppSettingsStore.Current.UseLotNetworkWaferMap };
                EnsureProjectObjects(project);

                if (!RecipeStore.Save(project))
                    throw new IOException("Project 파일 생성에 실패했습니다.");

                if (!SaveMachineRecipe(name))
                {
                    RecipeStore.Delete(name);
                    RecipeDataStore.DeleteRecipe(name);

                    throw new IOException(
                        "장치별 레시피 생성에 실패하여 새 프로젝트 생성을 취소했습니다.");
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
                    "프로젝트",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            //기존 코드
            //var project = new RecipeProject { FileName = name, InputUseRemoteWaferMap = AppSettingsStore.Current.UseLotNetworkWaferMap };
            //EnsureProjectObjects(project);
            //RecipeStore.Save(project);
            //SaveMachineRecipe(name);
            //MarkCurrentProject(name);
            //ReloadList();
            //SelectAndLoadProject(name);
        }

        private void OnReload()
        {
            if (!ConfirmDiscardProjectEdits("새로고침")) return;
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
                QMC.Common.MessageDialog.Show("이미 존재하는 프로젝트입니다.\r\nProject=" + targetName, "프로젝트",
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
                        "원본 프로젝트의 장치별 레시피 파일이 없어 복사를 취소했습니다. source=" + sourceName);
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
                QMC.Common.MessageDialog.Show("프로젝트 복사 실패:\r\n" + ex.Message, "프로젝트",
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
                    "현재 장비에 적용된 레시피는 삭제할 수 없습니다.\r\n" +
                    "다른 레시피를 먼저 장비에 적용한 후 삭제하세요.\r\n\r\n" +
                    "장비 적용 레시피=" + name,
                    "레시피 삭제",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (QMC.Common.MessageDialog.Show(
                    "프로젝트와 장치별 레시피 데이터를 삭제할까요?\r\nProject=" + name,
                    "레시피 삭제",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                if (!RecipeStore.Delete(name))
                    throw new IOException("프로젝트 파일을 삭제하지 못했습니다. project=" + name);
                DataStoreResult deleted = RecipeDataStore.DeleteRecipe(name);
                if (!deleted.Success)
                    throw new IOException("프로젝트 파일은 삭제되었지만 장치별 레시피 폴더를 삭제하지 못했습니다. path=" +
                        deleted.Path + ", detail=" + deleted.Message);
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
                QMC.Common.MessageDialog.Show("프로젝트 삭제 실패:\r\n" + ex.Message, "프로젝트",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSaveCurrent()
        {
            RecipeProject project = CollectFromUi();
            if (string.IsNullOrWhiteSpace(project.FileName))
            {
                QMC.Common.MessageDialog.Show("프로젝트 이름이 비어 있습니다.", "프로젝트",
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
                //        "장치별 레시피 저장에 실패했습니다. Project 파일은 저장되었지만 장비 레시피 저장이 완료되지 않았습니다.");
                //}
                //MarkCurrentProject(project.FileName);

                _current = project;
                _loadedProjectName = project.FileName;
                ReloadList();
                SelectAndLoadProject(project.FileName);

                EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-SAVE",
                    "프로젝트를 저장했습니다. project=" + project.FileName);
                if (showMessage)
                    QMC.Common.MessageDialog.Show("저장 완료: " + project.FileName, "프로젝트",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-SAVE",
                    "프로젝트 저장 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show("프로젝트 저장 실패:\r\n" + ex.Message, "프로젝트",
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

                var host = FindForm() as Form1;
                if (host == null)
                    throw new InvalidOperationException("메인 화면을 찾을 수 없습니다.");

                bool cancelled;
                string reason;
                // 대상 검증과 작업자 확인이 끝나기 전에 기존 Material을 삭제하지 않습니다.
                if (!host.TryApplyMachineRecipe(project, true, out cancelled, out reason))
                {
                    if (cancelled) return;
                    throw new InvalidOperationException(reason);
                }

                _current = project;
                _loadedProjectName = project.FileName;
                PopulateProjectToUi(project);
                UpdateRecipeStatus(project);
                QMC.Common.MessageDialog.Show("현재 프로젝트를 장비에 적용하고 저장했습니다.\r\nProject=" + project.FileName,
                    "프로젝트", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, Security.UserSession.Name, "RECIPE-APPLY",
                    "프로젝트 적용 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show("프로젝트 적용 실패:\r\n" + ex.Message, "프로젝트",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            var host = FindForm() as Form1;
            string reason;
            if (!CanOpenWaferSettings(_loadedProjectName, host != null ? host.ActiveRecipeName : null, out reason))
            {
                QMC.Common.MessageDialog.Show(this, reason, "웨이퍼·맵 설정", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ConfirmDiscardProjectEdits("웨이퍼·맵 설정 화면으로 이동")) return;
            try
            {
                Control parent = Parent;
                while (parent != null && !(parent is QMC.CDT_320.Ui.Tabs.RecipeTab)) parent = parent.Parent;
                var recipeTab = parent as QMC.CDT_320.Ui.Tabs.RecipeTab;
                if (recipeTab == null) throw new InvalidOperationException("레시피 화면을 찾을 수 없습니다.");
                recipeTab.ShowPage("recipe.tapeFrameSubset");
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, Security.UserSession.Name, "RECIPE-MAP-OPEN", "웨이퍼 사양 화면 이동 실패: " + ex);
                QMC.Common.MessageDialog.Show(this, ex.Message, "웨이퍼·맵 설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static bool CanOpenWaferSettings(string selectedName, string activeName, out string reason)
        {
            string selected = NormalizeProjectName(selectedName);
            string active = NormalizeProjectName(activeName);
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(selected) || string.IsNullOrWhiteSpace(active) ||
                !string.Equals(selected, active, StringComparison.OrdinalIgnoreCase))
            {
                reason = "웨이퍼·맵 설정은 현재 장비에 적용된 레시피를 편집합니다.\r\n" +
                    "선택 레시피: " + (string.IsNullOrWhiteSpace(selected) ? "없음" : selected) + "\r\n" +
                    "장비 적용 레시피: " + (string.IsNullOrWhiteSpace(active) ? "확인되지 않음" : active) + "\r\n\r\n" +
                    "선택한 레시피를 편집하려면 [선택 레시피 장비 적용]을 먼저 진행하세요.";
                return false;
            }
            return true;
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
                dialog.Description = "XML 추적 정보 저장 경로를 선택하세요.";
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
            string cassetteFlow = GetValue(gridSummary, "CassetteFlow");
            project.CassetteFlow = cassetteFlow == "매핑" ? "Mapping" : cassetteFlow;
            // 예전 표시용 MapFormat/MapDirection은 파일에 보존한다.
            // 읽기 전용 맵 모니터 값을 공정 설정이나 호환 필드에 역으로 저장하지 않는다.
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
                    "장치별 레시피 저장 실패: " + ex.Message);
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
            return value ? "사용" : "미사용";
        }

        private static string FormatEdgeSkipMode(string value)
        {
            if (string.Equals(value, "Grid", StringComparison.OrdinalIgnoreCase)) return "격자 수 기준";
            if (string.Equals(value, "Millimeter", StringComparison.OrdinalIgnoreCase)) return "거리 기준 (mm)";
            if (string.Equals(value, "ExternalMap", StringComparison.OrdinalIgnoreCase)) return "외부 맵 기준";
            return value ?? "";
        }

        private static bool ParseEnable(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string text = value.Trim();
            return text.Equals("사용", StringComparison.Ordinal) ||
                   text.Equals("ENABLE", StringComparison.OrdinalIgnoreCase) ||
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
                    QMC.Common.MessageDialog.Show("경로를 찾을 수 없습니다.\r\nPath=" + path, "폴더 열기",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Process.Start(target);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("경로 열기 실패:\r\n" + ex.Message, "폴더 열기",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Keep Designer declarations intact; language bindings only affect displayed captions.
        private void InitializeRecipeLanguageBindings()
        {
            Lang.BindKey(this.toolTipSettings, this.btnCopy, "recipeUi.tooltip.21");
            Lang.BindKey(this.toolTipSettings, this.btnManage, "recipeUi.tooltip.22");
            Lang.BindKey(this.toolTipSettings, this.btnBrowseMap, "recipeUi.tooltip.23");
            Lang.BindKey(this.toolTipSettings, this.grpSummary, "recipeUi.tooltip.24");
            Lang.BindKey(this.toolTipSettings, this.lblCurrentProject, "recipeUi.tooltip.25");
            Lang.BindKey(this.toolTipSettings, this.btnReload, "recipeUi.tooltip.26");
            Lang.BindKey(this.toolTipSettings, this.btnSaveRecipe, "recipeUi.tooltip.27");
            Lang.BindKey(this.toolTipSettings, this.btnApplyCurrent, "recipeUi.tooltip.28");
            Lang.BindKey(this.toolTipSettings, this.grpMapApproval, "recipeUi.tooltip.29");
            Lang.BindKey(this.lblHeader, "recipeUi.projectPage.lblHeader.text");
            Lang.BindKey(this.tabBasic, "recipeUi.projectPage.tabBasic.text");
            Lang.BindKey(this.tabDetails, "recipeUi.projectPage.tabDetails.text");
            Lang.BindKey(this.tabAdvanced, "recipeUi.projectPage.tabAdvanced.text");
            Lang.BindKey(this.grpProjects, "recipeUi.projectPage.grpProjects.text");
            Lang.BindKey(this.grpSummary, "recipeUi.projectPage.grpSummary.text");
            Lang.BindKey(this.grpProjectOption, "recipeUi.projectPage.grpProjectOption.text");
            Lang.BindKey(this.grpInputMap, "recipeUi.projectPage.grpInputMap.text");
            Lang.BindKey(this.grpOutputMap, "recipeUi.projectPage.grpOutputMap.text");
            Lang.BindKey(this.grpMapApproval, "recipeUi.projectPage.grpMapApproval.text");
            Lang.BindKey(this.grpMap, "recipeUi.projectPage.grpMap.text");
            Lang.BindKey(this.grpStatus, "recipeUi.projectPage.grpStatus.text");
            Lang.BindKey(this.grpGlobal, "recipeUi.projectPage.grpGlobal.text");
            Lang.BindKey(this.grpXml, "recipeUi.projectPage.grpXml.text");
            Lang.BindKey(this.lblFooterHint, "recipeUi.projectPage.lblFooterHint.text");
            Lang.BindKey(this.btnCopy, "recipeUi.projectPage.btnCopy.text");
            Lang.BindKey(this.btnManage, "recipeUi.projectPage.btnManage.text");
            Lang.BindKey(this.btnReload, "recipeUi.projectPage.btnReload.text");
            Lang.BindKey(this.btnSaveRecipe, "recipeUi.projectPage.btnSaveRecipe.text");
            Lang.BindKey(this.btnApplyCurrent, "recipeUi.projectPage.btnApplyCurrent.text");
            Lang.BindKey(this.btnBrowseMap, "recipeUi.projectPage.btnBrowseMap.text");
            Lang.BindKey(this.menuNewRecipe, "recipeUi.projectPage.menuNewRecipe.text");
            Lang.BindKey(this.menuDeleteRecipe, "recipeUi.projectPage.menuDeleteRecipe.text");
            Lang.BindKey(this.menuProjectFolder, "recipeUi.projectPage.menuProjectFolder.text");
            Lang.BindKey(this.menuRecipeFolder, "recipeUi.projectPage.menuRecipeFolder.text");
            Lang.BindKey(this.menuMapFolder, "recipeUi.projectPage.menuMapFolder.text");
            Lang.BindKey(this.menuXmlPath, "recipeUi.projectPage.menuXmlPath.text");
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
                var ok = new Button { Location = new Point(240, 80), Size = new Size(80, 28), Text = "확인", DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
                var cancel = new Button { Location = new Point(328, 80), Size = new Size(80, 28), Text = "취소", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
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
