using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class TapeFrameSubsetPage : SubsetPageBase
    {
        protected override bool SaveToRecipePersistsProject => true;

        private const string EdgeSkipGridText = "GRID COUNT";
        private const string EdgeSkipMmText = "MM";
        private const string EdgeSkipExternalMapText = "EXTERNAL MAP";

        private bool _loadingRole;
        private bool _applyingPitchValues;
        private bool _currentRoleIsOutput;
        private bool _inputGridDefinitionDirty;
        private bool _outputGridDefinitionDirty;
        private string _pitchLoadNotice = "";
        private string _lastGridCountPreview = "";
        private string _lastWaferStatus = "대기: Base Wafer Map을 불러오거나 현재 Recipe를 확인하세요.";

        public TapeFrameSubsetPage() : base("recipe.tapeFrameSubset")
        {
            InitializeComponent();
            _nGridX.Maximum = 9999;
            _nGridY.Maximum = 9999;
            // Recipe Pitch는 center-to-center가 아니라 Die 사이 Gap이다. 접촉 배치는 0 mm가 정상값이다.
            _nPitchX.Minimum = 0M;
            _nPitchY.Minimum = 0M;
            _nPitchX.Value = 0M;
            _nPitchY.Value = 0M;
            lblPitchX.Text = "Pitch Gap X (mm)";
            lblPitchY.Text = "Pitch Gap Y (mm)";
            toolTipRecipeLocation.SetToolTip(_nPitchX, "다이 X 중심 간격 = Recipe Die Width + Pitch Gap X");
            toolTipRecipeLocation.SetToolTip(_nPitchY, "다이 Y 중심 간격 = Recipe Die Height + Pitch Gap Y");
            _nPitchX.ValueChanged += OnPitchValueChanged;
            _nPitchY.ValueChanged += OnPitchValueChanged;
            _nGridX.ValueChanged += OnGridValueChanged;
            _nGridY.ValueChanged += OnGridValueChanged;
            _btnImportWaferMap.Text = "LOAD WAFER MAP";
            toolTipRecipeLocation.SetToolTip(_btnImportWaferMap, "외부 RAD TXT/CSV/JSON Wafer Map을 선택 역할의 Base Map으로 연결합니다.");
            toolTipRecipeLocation.SetToolTip(btnGridMapCreate, "현재 Grid X/Y, Die Size, Pitch Gap, Wafer Diameter로 선택 역할의 Grid Base Map을 생성합니다.");
            UpdateEdgeSkipModeUi();
        }

        private void _cbEdgeSkipMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEdgeSkipModeUi();
        }

        protected override void BuildEditor(Panel c)
        {
            if (c != null)
                c.BackColor = Color.White;
        }

        protected override void LoadFromRecipe()
        {
            RecipeProjectConsistencyService.EnsureStructure(_project);
            _loadingRole = true;
            try
            {
                if (_cbWaferRole.SelectedIndex < 0)
                    _cbWaferRole.SelectedIndex = 0;

                _currentRoleIsOutput = IsOutputRoleSelected();
                TapeFrameSubset frame = ResolveRoleFrame(_currentRoleIsOutput) ?? new TapeFrameSubset();
                RefreshSpecList(frame.FrameSpecName);
                ApplyFrameToControls(frame);
                UpdateMapFileLabel();
                UpdateDerivedControlAccess();
                UpdateMapSourceInfo();
            }
            finally
            {
                _loadingRole = false;
            }
        }

        protected override void SaveToRecipe()
        {
            try
            {
                bool createGridMaps = IsCurrentGridDefinitionDirty();
                GridCountPreview gridPreview = null;
                RecipeMapBuildResult gridResult = null;
                if (createGridMaps)
                {
                    gridPreview = BuildGridCountPreview();
                    if (gridPreview.GeneratedTargetCount <= 0)
                    {
                        throw new InvalidOperationException(
                            "현재 Grid/Die/Pitch/Wafer Diameter/Edge Skip 조건에서 공정 대상 Die가 없습니다.");
                    }
                }

                SaveControlsToSelectedRole();
                if (createGridMaps)
                {
                    gridResult = RecipeMapBuildService.CreateGridBaseAndBuildRole(
                        _project,
                        _currentRoleIsOutput,
                        RecipeStore.Save);
                    if (gridResult == null || !gridResult.Success)
                    {
                        throw new InvalidOperationException(
                            gridResult != null ? gridResult.Message : "Grid Map 생성 결과가 없습니다.");
                    }
                    SetCurrentGridDefinitionDirty(false);
                }
                else if (!RebuildConfiguredMaps() && !RecipeStore.Save(_project))
                {
                    throw new IOException("[PROJECT FILE SAVE] Recipe Project 파일을 저장할 수 없습니다.");
                }

                _loadingRole = true;
                try
                {
                    TapeFrameSubset frame = ResolveRoleFrame(_currentRoleIsOutput);
                    RefreshSpecList(frame != null ? frame.FrameSpecName : "");
                    ApplyFrameToControls(frame);
                    UpdateMapFileLabel();
                    UpdateDerivedControlAccess();
                    _lastWaferStatus = createGridMaps
                        ? "[SAVE + GRID MAP CREATE OK] " + (_currentRoleIsOutput ? "OUTPUT GOOD/NG" : "INPUT") +
                          " 역할 맵을 Grid " + gridPreview.GridX + "x" + gridPreview.GridY +
                          ", 전체 셀 " + gridResult.AddressCount + ", Target " + gridResult.TargetCount +
                          "개로 생성했습니다. Map Create에서 FINAL APPLY 하세요."
                        : "[SAVE OK] " + (_currentRoleIsOutput ? "OUTPUT" : "INPUT") +
                          " Pitch Gap을 Recipe에 저장하고 Die Size + Gap 중심 간격으로 역할 맵 좌표를 다시 계산했습니다.";
                    if (!createGridMaps && IsBaseMapConnected())
                        _lastWaferStatus += " 공정 사용 전 Map Create에서 역할별 FINAL APPLY가 필요합니다.";
                    UpdateMapSourceInfo();
                }
                finally
                {
                    _loadingRole = false;
                }
            }
            catch (Exception ex)
            {
                _lastWaferStatus = "[SAVE FAILED] " + ex.Message;
                UpdateMapSourceInfo();
                throw;
            }
        }

        private void OnWaferRoleChanged(object sender, EventArgs e)
        {
            if (_loadingRole)
                return;

            try
            {
                // ComboBox는 이미 새 역할을 표시하지만 이 필드는 직전 역할을 유지한다.
                // 따라서 전환 전에 직전 역할의 미저장 편집값을 Recipe 메모리에 보존한다.
                SaveControlsToRole(_currentRoleIsOutput);

                _currentRoleIsOutput = IsOutputRoleSelected();
                _loadingRole = true;
                try
                {
                    TapeFrameSubset frame = ResolveRoleFrame(_currentRoleIsOutput) ?? new TapeFrameSubset();
                    RefreshSpecList(frame.FrameSpecName);
                    ApplyFrameToControls(frame);
                    UpdateMapFileLabel();
                    UpdateDerivedControlAccess();
                }
                finally
                {
                    _loadingRole = false;
                }
                _lastWaferStatus = "역할 전환: " + (_currentRoleIsOutput ? "OUTPUT" : "INPUT") +
                    " 설정과 생성 맵 정보를 표시합니다. 미저장 Pitch Gap은 화면 값으로만 유지됩니다." +
                    (string.IsNullOrWhiteSpace(_pitchLoadNotice) ? "" : " " + _pitchLoadNotice);
                UpdateMapSourceInfo();
            }
            catch (Exception ex)
            {
                _loadingRole = true;
                try
                {
                    _cbWaferRole.SelectedIndex = _currentRoleIsOutput ? 1 : 0;
                }
                finally
                {
                    _loadingRole = false;
                }

                MessageBox.Show("Wafer 역할 전환 실패: " + ex.Message, "Wafer Spec", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoadSpec_Click(object sender, EventArgs e)
        {
            try
            {
                string specName = _cbSpecLibrary.SelectedItem != null ? _cbSpecLibrary.SelectedItem.ToString() : "";
                TapeFrameSpec spec = MaterialSpecs.FindFrame(specName);
                if (spec == null)
                {
                    MessageBox.Show("선택된 Wafer/Frame Spec을 찾을 수 없습니다.", "Wafer Spec", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ApplySpecToControls(spec);
                _lastWaferStatus = "[LOAD SPEC] 선택 Spec을 화면에만 불러왔습니다. 상단 SAVE 또는 SAVE SPEC 전에는 Recipe에 적용되지 않습니다." +
                    (string.IsNullOrWhiteSpace(_pitchLoadNotice) ? "" : " " + _pitchLoadNotice);
                UpdateMapSourceInfo();
            }
            catch (Exception ex)
            {
                _lastWaferStatus = "[LOAD SPEC FAILED] " + ex.Message;
                UpdateMapSourceInfo();
                MessageBox.Show("Wafer/Frame Spec 불러오기 실패: " + ex.Message, "Wafer Spec", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveSpec_Click(object sender, EventArgs e)
        {
            try
            {
                SaveToRecipe();
                SaveFrameSpec(ResolveRoleFrame(_currentRoleIsOutput), ResolveRoleMapFileName(_currentRoleIsOutput));

                var host = FindForm() as Form1;
                if (host == null)
                    throw new InvalidOperationException("메인 화면을 찾을 수 없습니다.");

                if (!host.SaveAndApplyActiveRecipe(_project))
                {
                    throw new IOException(
                        "활성 Recipe의 Wafer/Frame 설정 저장 및 적용에 실패했습니다. recipe=" +
                        _project.FileName);
                }

                TapeFrameSubset frame = ResolveRoleFrame(_currentRoleIsOutput);
                RefreshSpecList(frame != null ? frame.FrameSpecName : "");
                UpdateMapFileLabel();
                _lastWaferStatus = "[SAVE SPEC OK] 현재 역할 Spec library와 Recipe를 함께 저장했습니다.";
                UpdateMapSourceInfo();
                MessageBox.Show("Wafer/Frame Spec 저장 및 Recipe 연동 완료.", "Wafer Spec", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _lastWaferStatus = "[SAVE SPEC FAILED] " + ex.Message;
                UpdateMapSourceInfo();
                MessageBox.Show("Wafer/Frame Spec 저장 실패: " + ex.Message, "Wafer Spec", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnImportWaferMap_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new OpenFileDialog
                {
                    Title = "Load Base Wafer Map",
                    Filter = "Wafer map files|*.txt;*.json;*.csv|WaferMap TXT|*.txt|JSON|*.json|CSV|*.csv|All files|*.*",
                    InitialDirectory = ResolveWaferMapInitialDirectory()
                })
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;

                    // Base 파싱 전에 현재 역할의 Pitch/Frame 편집값을 먼저 반영한다.
                    SaveControlsToSelectedRole();
                    RecipeMapBuildResult result = RecipeMapBuildService.ImportBaseAndBuildRole(
                        _project,
                        dialog.FileName,
                        _currentRoleIsOutput,
                        RecipeStore.Save);
                    if (!result.Success)
                        throw new InvalidOperationException(result.Message);

                    SetCurrentGridDefinitionDirty(false);

                    SaveAllFrameSpecs();
                    var host = FindForm() as Form1;
                    if (host == null)
                        throw new InvalidOperationException("메인 화면을 찾을 수 없습니다.");

                    if (!host.SaveAndApplyActiveRecipe(_project))
                    {
                        throw new IOException(
                            "활성 Recipe의 WaferMap 저장 및 적용에 실패했습니다. recipe=" +
                            _project.FileName);
                    }

                    _loadingRole = true;
                    try
                    {
                        ApplyFrameToControls(ResolveRoleFrame(_currentRoleIsOutput));
                        UpdateMapFileLabel();
                        UpdateDerivedControlAccess();
                        _lastWaferStatus = "[LOAD WAFER MAP OK] " + Path.GetFileName(dialog.FileName) +
                            "을 " + (_currentRoleIsOutput ? "Output Base" : "Input Base") + "로 저장하고 " +
                            (_currentRoleIsOutput ? "Good/NG" : "Input") + " 역할 맵을 생성했습니다. Map Create에서 확인 후 APPLY 하세요.";
                        UpdateMapSourceInfo();
                    }
                    finally
                    {
                        _loadingRole = false;
                    }

                    MessageBox.Show(
                        string.Format(
                            "{0} Base Wafer Map 생성 완료.\r\n주소 수: {1}\r\n\r\n" +
                            "공정 사용 전 {2}에서 해당 역할을 확인하고 FINAL APPLY 하세요.",
                            _currentRoleIsOutput ? "Output" : "Input",
                            result.AddressCount,
                            _currentRoleIsOutput ? "BIN DIE MAP CREATE" : "INPUT DIE MAP CREATE"),
                        "Wafer Map",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _lastWaferStatus = "[LOAD WAFER MAP FAILED] " + ex.Message;
                UpdateMapSourceInfo();
                MessageBox.Show("Wafer Map 불러오기 실패: " + ex.Message, "Wafer Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnGridMapCreate_Click(object sender, EventArgs e)
        {
            try
            {
                GridCountPreview preview = BuildGridCountPreview();
                if (preview.GeneratedTargetCount <= 0)
                {
                    throw new InvalidOperationException(
                        "현재 Grid/Die/Pitch/Wafer Diameter 조건에서 웨이퍼 내부에 완전히 들어오는 Die가 없습니다.");
                }

                string configuredBase = _project != null
                    ? RecipeMapPaths.ConfiguredBaseFileName(_project, ResolveBaseMapKind(_currentRoleIsOutput))
                    : "";
                if (!string.IsNullOrWhiteSpace(configuredBase))
                {
                    string affectedRole = _currentRoleIsOutput ? "OUTPUT GOOD/NG" : "INPUT";
                    DialogResult confirm = MessageBox.Show(
                        "현재 역할에 Base Map이 연결되어 있습니다.\r\n\r\n" +
                        "역할: " + affectedRole + "\r\n" +
                        "기존 Base: " + configuredBase + "\r\n" +
                        "새 Grid: " + preview.GridX + " x " + preview.GridY + "\r\n" +
                        "전체 셀: " + preview.RectCellCount + "\r\n" +
                        "예상 Target(Full Die + Edge Skip): " + preview.GeneratedTargetCount + "\r\n\r\n" +
                        "기존 Target/Skip Mask와 FINAL APPLY 승인은 초기화됩니다.\r\n" +
                        "Grid Base Map으로 교체하시겠습니까?",
                        "Grid Map Create",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes)
                        return;
                }

                SaveControlsToSelectedRole();
                RecipeMapBuildResult result = RecipeMapBuildService.CreateGridBaseAndBuildRole(
                    _project,
                    _currentRoleIsOutput,
                    RecipeStore.Save);
                if (result == null || !result.Success)
                    throw new InvalidOperationException(result != null ? result.Message : "Grid Map 생성 결과가 없습니다.");

                SetCurrentGridDefinitionDirty(false);
                SaveAllFrameSpecs();
                var host = FindForm() as Form1;
                if (host == null)
                    throw new InvalidOperationException("메인 화면을 찾을 수 없습니다.");
                if (!host.SaveAndApplyActiveRecipe(_project))
                {
                    throw new IOException(
                        "활성 Recipe의 Grid Map 저장 및 적용에 실패했습니다. recipe=" +
                        _project.FileName);
                }

                _loadingRole = true;
                try
                {
                    ApplyFrameToControls(ResolveRoleFrame(_currentRoleIsOutput));
                    UpdateMapFileLabel();
                    UpdateDerivedControlAccess();
                    _lastGridCountPreview =
                        "Grid          : " + preview.GridX + " x " + preview.GridY + Environment.NewLine +
                        "Created cells : " + result.AddressCount + Environment.NewLine +
                        "Target dies   : " + result.TargetCount;
                    _lastWaferStatus = "[GRID MAP CREATE OK] " +
                        (_currentRoleIsOutput ? "OUTPUT GOOD/NG" : "INPUT") +
                        " Grid Base와 역할 맵을 " + preview.GridX + "x" + preview.GridY +
                        "로 생성했습니다. Map Create에서 FINAL APPLY 하세요.";
                    UpdateMapSourceInfo();
                }
                finally
                {
                    _loadingRole = false;
                }

                MessageBox.Show(
                    "Grid Map 생성 완료.\r\n" +
                    "Grid: " + preview.GridX + " x " + preview.GridY + "\r\n" +
                    "전체 셀: " + result.AddressCount + "\r\n" +
                    "Target Die: " + result.TargetCount + "\r\n\r\n" +
                    "공정 사용 전 Map Create에서 FINAL APPLY 하세요.",
                    "Grid Map Create",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _lastWaferStatus = "[GRID MAP CREATE FAILED] " + ex.Message;
                UpdateMapSourceInfo();
                MessageBox.Show("Grid Map 생성 실패: " + ex.Message, "Grid Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string ResolveWaferMapInitialDirectory()
        {
            string machineWaferMapDir = @"D:\CDT-320\Config\WaferMap";
            if (Directory.Exists(machineWaferMapDir))
                return machineWaferMapDir;

            string appWaferMapDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "WaferMap");
            if (Directory.Exists(appWaferMapDir))
                return appWaferMapDir;

            return RecipeMapPaths.GetDieMapDirectory();
        }

        private void RefreshSpecList(string selectedName)
        {
            string selected = selectedName ?? "";
            _cbSpecLibrary.Items.Clear();
            MaterialSpecs.Load();
            if (MaterialSpecs.Data != null && MaterialSpecs.Data.Frames != null)
            {
                foreach (TapeFrameSpec spec in MaterialSpecs.Data.Frames)
                {
                    if (spec != null && !string.IsNullOrWhiteSpace(spec.Name))
                        _cbSpecLibrary.Items.Add(spec.Name);
                }
            }

            if (!string.IsNullOrWhiteSpace(selected) && !_cbSpecLibrary.Items.Contains(selected))
                _cbSpecLibrary.Items.Add(selected);

            if (!string.IsNullOrWhiteSpace(selected))
                _cbSpecLibrary.SelectedItem = selected;
            if (_cbSpecLibrary.SelectedIndex < 0 && _cbSpecLibrary.Items.Count > 0)
                _cbSpecLibrary.SelectedIndex = 0;
        }

        private void ApplySpecToControls(TapeFrameSpec spec)
        {
            if (spec == null)
                return;

            _tbName.Text = spec.Name ?? "";
            ApplyPitchValuesToControls(spec.PitchX, spec.PitchY, "선택 Spec");
            _nDiameter.Value = ClampDecimal((decimal)spec.OuterDiameterMm, _nDiameter.Minimum, _nDiameter.Maximum);

            if (!IsBaseMapConnected())
            {
                _nGridX.Value = ClampDecimal(spec.DieMapX, _nGridX.Minimum, _nGridX.Maximum);
                _nGridY.Value = ClampDecimal(spec.DieMapY, _nGridY.Minimum, _nGridY.Maximum);
                SetSelectedEdgeSkipMode(spec.EdgeSkipMode);
                _nSideEdgeSkip.Value = ClampDecimal(GetEdgeSkipControlValue(spec), _nSideEdgeSkip.Minimum, _nSideEdgeSkip.Maximum);
                _nTopBottomEdgeSkip.Value = ClampDecimal(GetTopBottomEdgeSkipControlValue(spec), _nTopBottomEdgeSkip.Minimum, _nTopBottomEdgeSkip.Maximum);
            }

            ApplyRecipeDieSizeToControls();
            UpdateDerivedControlAccess();
        }

        private void ApplyFrameToControls(TapeFrameSubset frame)
        {
            if (frame == null)
                return;

            _tbName.Text = frame.FrameSpecName ?? "";
            _nGridX.Value = ClampDecimal(frame.DieMapX, _nGridX.Minimum, _nGridX.Maximum);
            _nGridY.Value = ClampDecimal(frame.DieMapY, _nGridY.Minimum, _nGridY.Maximum);
            ApplyPitchValuesToControls(frame.PitchX, frame.PitchY, "저장된 " + (_currentRoleIsOutput ? "OUTPUT" : "INPUT") + " 역할");
            ApplyRecipeDieSizeToControls();
            _nDiameter.Value = ClampDecimal((decimal)frame.OuterDiameterMm, _nDiameter.Minimum, _nDiameter.Maximum);
            SetSelectedEdgeSkipMode(frame.EdgeSkipMode);
            _nSideEdgeSkip.Value = ClampDecimal(GetEdgeSkipControlValue(frame), _nSideEdgeSkip.Minimum, _nSideEdgeSkip.Maximum);
            _nTopBottomEdgeSkip.Value = ClampDecimal(GetTopBottomEdgeSkipControlValue(frame), _nTopBottomEdgeSkip.Minimum, _nTopBottomEdgeSkip.Maximum);
            _cbRotate.SelectedItem = frame.Rotate ?? "None";
            if (_cbRotate.SelectedIndex < 0)
                _cbRotate.SelectedIndex = 0;
        }

        private void ApplyControlsToFrame(TapeFrameSubset frame)
        {
            if (frame == null)
                return;

            double pitchX = (double)_nPitchX.Value;
            double pitchY = (double)_nPitchY.Value;
            ValidatePitchForSave(pitchX, pitchY);

            frame.FrameSpecName = string.IsNullOrWhiteSpace(_tbName.Text) ? "8inch_5x5" : _tbName.Text.Trim();
            frame.PitchX = pitchX;
            frame.PitchY = pitchY;
            frame.DieSizeX = ResolveRecipeDieSizeX();
            frame.DieSizeY = ResolveRecipeDieSizeY();
            frame.OuterDiameterMm = (double)_nDiameter.Value;
            frame.Rotate = _cbRotate.SelectedItem != null ? _cbRotate.SelectedItem.ToString() : "None";
            frame.DieMapX = Math.Max(1, (int)_nGridX.Value);
            frame.DieMapY = Math.Max(1, (int)_nGridY.Value);

            if (IsBaseMapConnected())
            {
                frame.EdgeSkipMode = "ExternalMap";
                frame.SideEdgeSkip = 0;
                frame.TopBottomEdgeSkip = 0;
                frame.SideEdgeSkipMm = 0.0;
                frame.TopBottomEdgeSkipMm = 0.0;
                return;
            }

            string edgeMode = GetSelectedEdgeSkipModeName();
            bool mmMode = string.Equals(edgeMode, "Millimeter", StringComparison.OrdinalIgnoreCase);
            bool externalMode = string.Equals(edgeMode, "ExternalMap", StringComparison.OrdinalIgnoreCase);
            frame.EdgeSkipMode = edgeMode;
            frame.SideEdgeSkip = !mmMode && !externalMode ? Math.Max(0, (int)Math.Floor(_nSideEdgeSkip.Value)) : 0;
            frame.TopBottomEdgeSkip = !mmMode && !externalMode ? Math.Max(0, (int)Math.Floor(_nTopBottomEdgeSkip.Value)) : 0;
            frame.SideEdgeSkipMm = mmMode ? Math.Max(0.0, (double)_nSideEdgeSkip.Value) : 0.0;
            frame.TopBottomEdgeSkipMm = mmMode ? Math.Max(0.0, (double)_nTopBottomEdgeSkip.Value) : 0.0;
        }

        private void SaveControlsToSelectedRole()
        {
            SaveControlsToRole(_currentRoleIsOutput);
        }

        private void SaveControlsToRole(bool output)
        {
            if (_project == null)
                return;

            RecipeProjectConsistencyService.EnsureStructure(_project);
            TapeFrameSubset frame = output ? _project.OutputFrame : _project.InputFrame;
            ApplyControlsToFrame(frame);
            if (!output)
                _project.Frame = RecipeProjectConsistencyService.CloneFrame(frame);
        }

        private bool RebuildConfiguredMaps()
        {
            if (!IsBaseMapConnected())
                return false;

            bool rebuildInput = !_currentRoleIsOutput;
            bool rebuildOutput = _currentRoleIsOutput;
            RecipeMapBuildResult result = RecipeMapBuildService.RebuildDerivedMaps(
                _project,
                rebuildInput,
                rebuildOutput,
                true,
                RecipeStore.Save);
            if (!result.Success)
                throw new InvalidOperationException(result.Message);
            return true;
        }

        private void SaveAllFrameSpecs()
        {
            SaveFrameSpec(_project != null ? _project.InputFrame : null, _project != null ? _project.InputDieMapFileName : "");
            SaveFrameSpec(_project != null ? _project.OutputFrame : null, _project != null ? _project.GoodBinDieMapFileName : "");
        }

        private void SaveFrameSpec(TapeFrameSubset frame, string mapFileName)
        {
            if (frame == null)
                return;

            MaterialSpecs.UpsertFrame(
                frame.FrameSpecName,
                frame.DieMapX,
                frame.DieMapY,
                frame.PitchX,
                frame.PitchY,
                ResolveRecipeDieSizeX(),
                ResolveRecipeDieSizeY(),
                frame.OuterDiameterMm,
                frame.EdgeSkipMode,
                frame.SideEdgeSkip,
                frame.TopBottomEdgeSkip,
                frame.SideEdgeSkipMm,
                frame.TopBottomEdgeSkipMm,
                mapFileName ?? "",
                _project != null && _project.Die != null ? _project.Die.DieSpecName ?? "" : "");
        }

        private string ResolveRoleMapFileName(bool output)
        {
            if (_project == null)
                return "";
            return output ? _project.GoodBinDieMapFileName : _project.InputDieMapFileName;
        }

        private static RecipeMapKind ResolveBaseMapKind(bool output)
        {
            return output ? RecipeMapKind.GoodBin : RecipeMapKind.Input;
        }

        private TapeFrameSubset ResolveRoleFrame(bool output)
        {
            if (_project == null)
                return null;

            return output ? (_project.OutputFrame ?? _project.Frame) : (_project.InputFrame ?? _project.Frame);
        }

        private bool IsOutputRoleSelected()
        {
            return _cbWaferRole != null &&
                   _cbWaferRole.SelectedItem != null &&
                   _cbWaferRole.SelectedItem.ToString().IndexOf("OUTPUT", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsBaseMapConnected()
        {
            return _project != null && !string.IsNullOrWhiteSpace(
                RecipeMapPaths.ConfiguredBaseFileName(_project, ResolveBaseMapKind(_currentRoleIsOutput)));
        }

        private void UpdateMapFileLabel()
        {
            if (_lblMapFileValue == null || _project == null)
                return;

            _lblMapFileValue.Text = _currentRoleIsOutput
                ? "OUTPUT BASE=" + EmptyDash(RecipeMapPaths.ConfiguredBaseFileName(_project, RecipeMapKind.GoodBin)) + " / GOOD=" + EmptyDash(_project.GoodBinDieMapFileName) + " / NG=" + EmptyDash(_project.NgBinDieMapFileName)
                : "INPUT BASE=" + EmptyDash(RecipeMapPaths.ConfiguredBaseFileName(_project, RecipeMapKind.Input)) + " / INPUT=" + EmptyDash(_project.InputDieMapFileName);
        }

        private void OnPitchValueChanged(object sender, EventArgs e)
        {
            if (_loadingRole || _applyingPitchValues)
                return;

            _pitchLoadNotice = "";
            _lastWaferStatus = "[EDITING] 현재 화면의 Pitch Gap이 변경되었습니다. 실제 중심 간격은 Die Size + Gap이며 상단 SAVE 전에는 역할 맵 좌표에 적용되지 않습니다.";
            UpdateMapSourceInfo();
        }

        private void OnGridValueChanged(object sender, EventArgs e)
        {
            if (_loadingRole)
                return;

            SetCurrentGridDefinitionDirty(true);
            _lastGridCountPreview = "";
            _lastWaferStatus = "[EDITING] Grid X/Y changed. COUNT CHECK 후 SAVE/SAVE SPEC을 누르면 선택 역할의 Grid Map을 생성합니다.";
            if (lblGridCountResult != null)
                lblGridCountResult.Text = "Count: -";
            UpdateMapSourceInfo();
        }

        private bool IsCurrentGridDefinitionDirty()
        {
            return _currentRoleIsOutput ? _outputGridDefinitionDirty : _inputGridDefinitionDirty;
        }

        private void SetCurrentGridDefinitionDirty(bool dirty)
        {
            if (_currentRoleIsOutput)
                _outputGridDefinitionDirty = dirty;
            else
                _inputGridDefinitionDirty = dirty;
        }

        private void btnGridCountPreview_Click(object sender, EventArgs e)
        {
            try
            {
                GridCountPreview preview = BuildGridCountPreview();
                _lastGridCountPreview =
                    "Grid          : " + preview.GridX + " x " + preview.GridY + Environment.NewLine +
                    "Rect cells    : " + preview.RectCellCount + Environment.NewLine +
                    "Center in dia : " + preview.CenterInsideWaferCount + " / wafer " + FormatNumber(preview.WaferDiameter) + " mm" + Environment.NewLine +
                    "Full die in   : " + preview.FullDieInsideWaferCount + " / die " + FormatNumber(preview.DieSizeX) + " x " + FormatNumber(preview.DieSizeY) + " mm" + Environment.NewLine +
                    "Target + skip : " + preview.GeneratedTargetCount + Environment.NewLine +
                    "Center step   : " + FormatNumber(preview.CenterStepX) + " x " + FormatNumber(preview.CenterStepY) + " mm";
                if (preview.LoadedTargetCount >= 0)
                    _lastGridCountPreview += Environment.NewLine + "Loaded target : " + preview.LoadedTargetCount + " / raw " + preview.LoadedRawGridX + " x " + preview.LoadedRawGridY;

                if (lblGridCountResult != null)
                    lblGridCountResult.Text = "Target: " + preview.GeneratedTargetCount + " / Cells: " + preview.RectCellCount;

                _lastWaferStatus = "[GRID COUNT CHECK] " + preview.GridX + "x" + preview.GridY +
                    " center-in-dia=" + preview.CenterInsideWaferCount +
                    ", full-die-in-dia=" + preview.FullDieInsideWaferCount +
                    ", target-after-skip=" + preview.GeneratedTargetCount +
                    ", rect=" + preview.RectCellCount + ".";
                UpdateMapSourceInfo();
            }
            catch (Exception ex)
            {
                _lastWaferStatus = "[GRID COUNT CHECK FAILED] " + ex.Message;
                UpdateMapSourceInfo();
                MessageBox.Show("Grid count check failed: " + ex.Message, "Wafer Spec", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyPitchValuesToControls(double configuredPitchX, double configuredPitchY, string sourceName)
        {
            double dieSizeX = ResolveRecipeDieSizeX();
            double dieSizeY = ResolveRecipeDieSizeY();
            double baseCenterStepX;
            double baseCenterStepY;
            bool hasBaseSourcePitch = TryResolveBaseSourcePitch(out baseCenterStepX, out baseCenterStepY);
            bool baseXValid = hasBaseSourcePitch && IsSourceCenterStepValid(baseCenterStepX, dieSizeX);
            bool baseYValid = hasBaseSourcePitch && IsSourceCenterStepValid(baseCenterStepY, dieSizeY);
            double baseGapX = baseXValid ? Math.Max(0.0, baseCenterStepX - dieSizeX) : 0.0;
            double baseGapY = baseYValid ? Math.Max(0.0, baseCenterStepY - dieSizeY) : 0.0;
            bool configuredXValid = IsPitchGapValid(configuredPitchX);
            bool configuredYValid = IsPitchGapValid(configuredPitchY);

            // 기존 NumericUpDown 최소값(0.001)이 정상 Gap 0을 0.001로 올려 저장한 Recipe를 복구한다.
            bool legacyMinimumClampX = baseXValid && NearlyEqual(configuredPitchX, 0.001) &&
                                       NearlyEqual(baseGapX, 0.0) &&
                                       IsLegacyRoleMapPitch(configuredPitchX, dieSizeX, true, sourceName);
            bool legacyMinimumClampY = baseYValid && NearlyEqual(configuredPitchY, 0.001) &&
                                       NearlyEqual(baseGapY, 0.0) &&
                                       IsLegacyRoleMapPitch(configuredPitchY, dieSizeY, false, sourceName);
            bool recoverX = !configuredXValid || legacyMinimumClampX;
            bool recoverY = !configuredYValid || legacyMinimumClampY;
            double displayPitchX = recoverX ? (baseXValid ? baseGapX : 0.0) : configuredPitchX;
            double displayPitchY = recoverY ? (baseYValid ? baseGapY : 0.0) : configuredPitchY;

            _applyingPitchValues = true;
            try
            {
                _nPitchX.Value = ClampDecimal(ToSafeDecimal(displayPitchX, _nPitchX.Minimum), _nPitchX.Minimum, _nPitchX.Maximum);
                _nPitchY.Value = ClampDecimal(ToSafeDecimal(displayPitchY, _nPitchY.Minimum), _nPitchY.Minimum, _nPitchY.Maximum);
            }
            finally
            {
                _applyingPitchValues = false;
            }

            if (!recoverX && !recoverY)
            {
                _pitchLoadNotice = "";
                return;
            }

            string configuredText = "(" + FormatNumber(configuredPitchX) + ", " + FormatNumber(configuredPitchY) + ") mm";
            if ((!recoverX || baseXValid) && (!recoverY || baseYValid))
            {
                _pitchLoadNotice = "[PITCH GAP RECOVERY] " + sourceName + " Gap " + configuredText +
                    "을 Base RAD 중심 간격 (" + FormatNumber(baseCenterStepX) + ", " +
                    FormatNumber(baseCenterStepY) + ") - Die Size (" + FormatNumber(dieSizeX) + ", " +
                    FormatNumber(dieSizeY) + ")로 계산한 Gap (" + FormatNumber(displayPitchX) + ", " +
                    FormatNumber(displayPitchY) + ") mm로 표시합니다. 상단 SAVE 시 역할 맵을 다시 계산합니다.";
                return;
            }

            _pitchLoadNotice = "[PITCH GAP INVALID] " + sourceName + " Gap " + configuredText +
                "이 유효하지 않고 사용할 수 있는 Base RAD 중심 간격도 없어 0 mm로 표시합니다. " +
                "Gap은 0 이상의 값으로 저장됩니다.";
        }

        private bool IsLegacyRoleMapPitch(double configuredGap, double dieSize, bool xAxis, string sourceName)
        {
            if (string.IsNullOrWhiteSpace(sourceName) ||
                !sourceName.StartsWith("저장된 ", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                string configuredPath = ResolveRoleMapFileName(_currentRoleIsOutput);
                string absolutePath = RecipeMapPaths.ResolveConfiguredPath(configuredPath);
                DieMap roleMap = !string.IsNullOrWhiteSpace(absolutePath) && File.Exists(absolutePath)
                    ? DieMapGenerator.Load(absolutePath)
                    : null;
                if (roleMap == null)
                    return false;

                double storedMapStep = xAxis ? roleMap.PitchX : roleMap.PitchY;
                return NearlyEqual(storedMapStep, configuredGap) &&
                       storedMapStep + 0.000000001 < dieSize;
            }
            catch
            {
                return false;
            }
        }

        private bool TryResolveBaseSourcePitch(out double pitchX, out double pitchY)
        {
            pitchX = 0.0;
            pitchY = 0.0;
            try
            {
                if (_project == null)
                    return false;

                string basePath = RecipeMapPaths.ResolveBaseConfigured(
                    _project,
                    ResolveBaseMapKind(_currentRoleIsOutput));
                if (string.IsNullOrWhiteSpace(basePath) || !File.Exists(basePath))
                    return false;

                DieMap baseMap = DieMapGenerator.Load(basePath);
                if (baseMap == null || !baseMap.SourcePitchFromFile)
                    return false;

                pitchX = baseMap.PitchX;
                pitchY = baseMap.PitchY;
                return true;
            }
            catch
            {
                pitchX = 0.0;
                pitchY = 0.0;
                return false;
            }
        }

        private void ValidatePitchForSave(double pitchX, double pitchY)
        {
            if (!IsPitchGapValid(pitchX) || !IsPitchGapValid(pitchY))
            {
                throw new InvalidOperationException(
                    "[PITCH GAP VALIDATION] Pitch Gap은 유한한 0 이상의 값이어야 합니다. " +
                    "Gap=(" + FormatNumber(pitchX) + ", " + FormatNumber(pitchY) + ") mm.");
            }
        }

        private static bool IsPitchGapValid(double pitchGap)
        {
            return !double.IsNaN(pitchGap) && !double.IsInfinity(pitchGap) && pitchGap >= 0.0;
        }

        private static bool IsSourceCenterStepValid(double centerStep, double dieSize)
        {
            return !double.IsNaN(centerStep) && !double.IsInfinity(centerStep) && centerStep > 0.0 &&
                   !double.IsNaN(dieSize) && !double.IsInfinity(dieSize) && dieSize > 0.0 &&
                   centerStep + 0.000000001 >= dieSize;
        }

        private static bool NearlyEqual(double left, double right)
        {
            return Math.Abs(left - right) <= 0.000001;
        }

        private static decimal ToSafeDecimal(double value, decimal fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < (double)decimal.MinValue || value > (double)decimal.MaxValue)
                return fallback;
            return (decimal)value;
        }

        private void UpdateMapSourceInfo()
        {
            if (_tbMapSourceInfo == null)
                return;

            try
            {
                string role = _currentRoleIsOutput ? "OUTPUT" : "INPUT";
                string axisX = _currentRoleIsOutput ? "Output Camera X" : "Input Camera X";
                string axisY = _currentRoleIsOutput ? "Output Stage Y" : "Input Stage Y";
                string projectName = _project != null ? _project.FileName ?? "-" : "-";
                string basePath = _project != null
                    ? RecipeMapPaths.ResolveBaseConfigured(_project, ResolveBaseMapKind(_currentRoleIsOutput))
                    : "";
                DieMap map = !string.IsNullOrWhiteSpace(basePath) && File.Exists(basePath)
                    ? DieMapGenerator.Load(basePath)
                    : null;

                var lines = new System.Collections.Generic.List<string>
                {
                    "[CURRENT RECIPE]",
                    "Project       : " + projectName,
                    "Wafer role    : " + role,
                    "Axis mapping  : X = " + axisX + " / Y = " + axisY,
                    "Status        : " + (_lastWaferStatus ?? "-"),
                    ""
                };

                if (map == null || map.Entries == null || map.Entries.Count == 0)
                {
                    lines.Add("[ORIGINAL WAFER MAP]");
                    lines.Add(string.IsNullOrWhiteSpace(basePath)
                        ? "Base map      : NOT CONNECTED"
                        : "Base map      : READ FAILED - " + basePath);
                    lines.Add("Source folder : D:\\CDT-320\\Config\\WaferMap");
                    lines.Add("");
                    AppendWaferApplyFlow(lines);
                    _tbMapSourceInfo.Text = string.Join(Environment.NewLine, lines);
                    return;
                }

                DieMapGenerator.Normalize(map);
                var entries = map.Entries.Where(entry => entry != null).ToList();
                int minRawX = entries.Min(entry => entry.OriginalMapX >= 0 ? entry.OriginalMapX : entry.DieMapX);
                int maxRawX = entries.Max(entry => entry.OriginalMapX >= 0 ? entry.OriginalMapX : entry.DieMapX);
                int minRawY = entries.Min(entry => entry.OriginalMapY >= 0 ? entry.OriginalMapY : entry.DieMapY);
                int maxRawY = entries.Max(entry => entry.OriginalMapY >= 0 ? entry.OriginalMapY : entry.DieMapY);
                double centerRawX = (minRawX + maxRawX) / 2.0;
                double centerRawY = (minRawY + maxRawY) / 2.0;
                int targetCount = entries.Count(entry => entry.IsTarget);
                double roleGapX = _nPitchX != null ? (double)_nPitchX.Value : 0.0;
                double roleGapY = _nPitchY != null ? (double)_nPitchY.Value : 0.0;
                double roleCenterStepX = ResolveRecipeDieSizeX() + roleGapX;
                double roleCenterStepY = ResolveRecipeDieSizeY() + roleGapY;
                RecipeMapKind roleKind = _currentRoleIsOutput ? RecipeMapKind.GoodBin : RecipeMapKind.Input;
                string roleConfiguredPath = ResolveRoleMapFileName(_currentRoleIsOutput);
                string roleAbsolutePath = RecipeMapPaths.ResolveConfiguredPath(roleConfiguredPath);
                DieMap savedRoleMap = !string.IsNullOrWhiteSpace(roleAbsolutePath) && File.Exists(roleAbsolutePath)
                    ? DieMapGenerator.Load(roleAbsolutePath)
                    : null;
                bool screenPitchUnsaved = savedRoleMap == null ||
                    Math.Abs(savedRoleMap.PitchX - roleCenterStepX) > 0.000001 ||
                    Math.Abs(savedRoleMap.PitchY - roleCenterStepY) > 0.000001;
                double firstCalculatedX = map.SourceFirstX >= 0 ? (map.SourceFirstX - centerRawX) * map.PitchX : double.NaN;
                double firstCalculatedY = map.SourceFirstY >= 0 ? (centerRawY - map.SourceFirstY) * map.PitchY : double.NaN;
                bool localDomainMatches = map.DieMapX == maxRawX - minRawX + 1 &&
                                          map.DieMapY == maxRawY - minRawY + 1;

                lines.Add("[ORIGINAL WAFER MAP]");
                lines.Add("Source file   : " + EmptyDash(map.SourceFileName));
                lines.Add("Format        : " + EmptyDash(map.SourceFormat));
                lines.Add("Stored Base   : " + basePath);
                lines.Add("Records       : " + entries.Count + " / Target " + targetCount +
                          (map.SourceDeclaredCount > 0 ? " / Header " + map.SourceDeclaredCount : ""));
                lines.Add("Raw index X   : " + minRawX + " .. " + maxRawX);
                lines.Add("Raw index Y   : " + minRawY + " .. " + maxRawY);
                lines.Add("Raw center    : (" + FormatNumber(centerRawX) + ", " + FormatNumber(centerRawY) + ") -> Equipment Grid (0,0)");
                lines.Add("Local array   : " + map.DieMapX + " x " + map.DieMapY +
                          (localDomainMatches ? " (normalized)" : " (LEGACY RAW/LOCAL MIXED - SAVE REQUIRED)"));
                lines.Add("Header step   : " + (map.SourcePitchFromFile
                    ? FormatNumber(map.PitchX) + " x " + FormatNumber(map.PitchY) + " mm (source center-step)"
                    : "not provided; Die Size + role Gap is used"));
                lines.Add("Die size      : " + FormatNumber(ResolveRecipeDieSizeX()) + " x " +
                          FormatNumber(ResolveRecipeDieSizeY()) + " mm (Recipe -> Die Spec)");
                if (map.SourceFirstX >= 0 && map.SourceFirstY >= 0)
                {
                    lines.Add("Header FIRST  : raw(" + map.SourceFirstX + "," + map.SourceFirstY + ")");
                    if (!double.IsNaN(map.SourceFirstPosX) && !double.IsNaN(map.SourceFirstPosY))
                    {
                        lines.Add("Header FX/FY  : (" + FormatNumber(map.SourceFirstPosX) + ", " +
                                  FormatNumber(map.SourceFirstPosY) + ") mm");
                        lines.Add("Grid calc     : (" + FormatNumber(firstCalculatedX) + ", " +
                                  FormatNumber(firstCalculatedY) + ") mm");
                    }
                }

                lines.Add("");
                lines.Add("[" + role + " ROLE MAP]");
                lines.Add("Pitch gap     : " + FormatNumber(roleGapX) + " x " + FormatNumber(roleGapY) + " mm");
                lines.Add("Center step   : " + FormatNumber(roleCenterStepX) + " x " + FormatNumber(roleCenterStepY) + " mm" +
                          (screenPitchUnsaved ? " (UNSAVED)" : " (same as saved map)"));
                lines.Add("Saved map step: " + (savedRoleMap != null
                    ? FormatNumber(savedRoleMap.PitchX) + " x " + FormatNumber(savedRoleMap.PitchY) + " mm (actual role file)"
                    : "role map not found"));
                lines.Add("Grid control  : center (0,0), Left-/Right+, Down+/Up-");
                lines.Add("Axis reference: X offset -> " + axisX + ", Y offset -> " + axisY);
                if (_currentRoleIsOutput)
                    lines.Add("Output place  : Camera X 기준좌표를 Picker X 배치좌표로 변환");
                lines.Add("Map path      : " + roleAbsolutePath);
                string approvalReason = "";
                bool approved = savedRoleMap != null && RecipeMapPaths.IsMapApproved(_project, roleKind, savedRoleMap, out approvalReason);
                lines.Add("Final apply   : " + (approved ? "APPROVED" : "PENDING - " + (savedRoleMap == null ? "role map not found" : approvalReason)));
                if (_currentRoleIsOutput && _project != null)
                {
                    string ngPath = RecipeMapPaths.ResolveConfiguredPath(_project.NgBinDieMapFileName);
                    DieMap ngMap = !string.IsNullOrWhiteSpace(ngPath) && File.Exists(ngPath) ? DieMapGenerator.Load(ngPath) : null;
                    string ngApprovalReason = "";
                    bool ngApproved = ngMap != null && RecipeMapPaths.IsMapApproved(_project, RecipeMapKind.NgBin, ngMap, out ngApprovalReason);
                    lines.Add("NG map path   : " + ngPath);
                    lines.Add("NG final apply: " + (ngApproved ? "APPROVED" : "PENDING - " + (ngMap == null ? "role map not found" : ngApprovalReason)));
                }
                lines.Add("");
                if (!string.IsNullOrWhiteSpace(_lastGridCountPreview))
                {
                    lines.Add("[GRID COUNT CHECK]");
                    foreach (string previewLine in _lastGridCountPreview.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                        lines.Add(previewLine);
                    lines.Add("");
                }
                AppendWaferApplyFlow(lines);
                _tbMapSourceInfo.Text = string.Join(Environment.NewLine, lines);
            }
            catch (Exception ex)
            {
                _tbMapSourceInfo.Text = "[WAFER MAP INFO FAILED]" + Environment.NewLine + ex.Message +
                    Environment.NewLine + Environment.NewLine + (_lastWaferStatus ?? "");
            }
        }

        private static void AppendWaferApplyFlow(System.Collections.Generic.ICollection<string> lines)
        {
            lines.Add("[SAVE / LOAD / FINAL APPLY]");
            lines.Add("1. 역할 선택 -> 외부 맵이면 LOAD WAFER MAP, 직접 Grid면 Grid X/Y 입력 -> COUNT CHECK -> SAVE/SAVE SPEC");
            lines.Add("2. Pitch Gap만 변경할 때는 top SAVE로 기존 Base 주소의 역할 좌표를 다시 계산");
            lines.Add("3. INPUT DIE MAP CREATE: Input Base 기준 맵/좌표 확인 -> FINAL APPLY");
            lines.Add("4. BIN DIE MAP CREATE: Output Base 기준 GOOD/NG 각각 확인 -> FINAL APPLY");
            lines.Add("※ LOAD SPEC은 화면만 변경하며 SAVE 전에는 Recipe에 적용되지 않습니다.");
        }

        private sealed class GridCountPreview
        {
            public int GridX;
            public int GridY;
            public int RectCellCount;
            public int CenterInsideWaferCount;
            public int FullDieInsideWaferCount;
            public int GeneratedTargetCount;
            public int LoadedTargetCount = -1;
            public int LoadedRawGridX;
            public int LoadedRawGridY;
            public double WaferDiameter;
            public double DieSizeX;
            public double DieSizeY;
            public double CenterStepX;
            public double CenterStepY;
        }

        private GridCountPreview BuildGridCountPreview()
        {
            int gridX = Math.Max(1, (int)_nGridX.Value);
            int gridY = Math.Max(1, (int)_nGridY.Value);
            long rectCount = (long)gridX * gridY;
            if (rectCount > 1000000L)
                throw new InvalidOperationException("Grid is too large to preview safely: " + gridX + " x " + gridY);

            double dieSizeX = ResolveRecipeDieSizeX();
            double dieSizeY = ResolveRecipeDieSizeY();
            double centerStepX = dieSizeX + (double)_nPitchX.Value;
            double centerStepY = dieSizeY + (double)_nPitchY.Value;
            double diameter = (double)_nDiameter.Value;
            double radius = Math.Max(0.0, diameter / 2.0);
            double centerGridX = Math.Max(0, gridX - 1) / 2.0;
            string edgeSkipMode = GetSelectedEdgeSkipModeName();
            bool millimeterMode = edgeSkipMode.IndexOf("Millimeter", StringComparison.OrdinalIgnoreCase) >= 0;
            int sideGridSkip = millimeterMode
                ? 0
                : Math.Min(Math.Max(0, (int)Math.Floor(_nSideEdgeSkip.Value)), Math.Max(0, (gridX - 1) / 2));
            int topBottomGridSkip = millimeterMode
                ? 0
                : Math.Min(Math.Max(0, (int)Math.Floor(_nTopBottomEdgeSkip.Value)), Math.Max(0, (gridY - 1) / 2));
            double sideMmSkip = millimeterMode ? Math.Max(0.0, (double)_nSideEdgeSkip.Value) : 0.0;
            double topBottomMmSkip = millimeterMode ? Math.Max(0.0, (double)_nTopBottomEdgeSkip.Value) : 0.0;
            int centerInside = 0;
            int fullDieInside = 0;
            int generatedTarget = 0;

            for (int y = 0; y < gridY; y++)
            {
                double equipmentGridY = DieMapGenerator.CalculateEquipmentGridY(y, gridY);
                double posY = equipmentGridY * centerStepY;
                for (int x = 0; x < gridX; x++)
                {
                    double equipmentGridX = x - centerGridX;
                    double posX = equipmentGridX * centerStepX;
                    if (Math.Sqrt(posX * posX + posY * posY) <= radius + 0.000001)
                        centerInside++;

                    bool fullInside = true;
                    for (int sx = -1; sx <= 1 && fullInside; sx += 2)
                    {
                        for (int sy = -1; sy <= 1; sy += 2)
                        {
                            double cornerX = posX + sx * dieSizeX / 2.0;
                            double cornerY = posY + sy * dieSizeY / 2.0;
                            if (Math.Sqrt(cornerX * cornerX + cornerY * cornerY) > radius + 0.000001)
                            {
                                fullInside = false;
                                break;
                            }
                        }
                    }

                    if (fullInside)
                        fullDieInside++;

                    bool target = fullInside;
                    if (!millimeterMode)
                    {
                        if (x < sideGridSkip || x >= gridX - sideGridSkip ||
                            y < topBottomGridSkip || y >= gridY - topBottomGridSkip)
                        {
                            target = false;
                        }
                    }
                    else
                    {
                        double halfDieX = dieSizeX / 2.0;
                        double halfDieY = dieSizeY / 2.0;
                        double usableHalfX = Math.Max(0.0, radius - sideMmSkip);
                        double usableHalfY = Math.Max(0.0, radius - topBottomMmSkip);
                        double usableRadius = Math.Max(0.0, radius - Math.Max(sideMmSkip, topBottomMmSkip));
                        target = usableRadius > 0.0 &&
                                 Math.Abs(posX) + halfDieX <= usableHalfX + 0.000001 &&
                                 Math.Abs(posY) + halfDieY <= usableHalfY + 0.000001;
                        for (int sx = -1; sx <= 1 && target; sx += 2)
                        {
                            for (int sy = -1; sy <= 1; sy += 2)
                            {
                                double cornerX = posX + sx * halfDieX;
                                double cornerY = posY + sy * halfDieY;
                                if (Math.Sqrt(cornerX * cornerX + cornerY * cornerY) > usableRadius + 0.000001)
                                {
                                    target = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (target)
                        generatedTarget++;
                }
            }

            GridCountPreview preview = new GridCountPreview
            {
                GridX = gridX,
                GridY = gridY,
                RectCellCount = (int)rectCount,
                CenterInsideWaferCount = centerInside,
                FullDieInsideWaferCount = fullDieInside,
                GeneratedTargetCount = generatedTarget,
                WaferDiameter = diameter,
                DieSizeX = dieSizeX,
                DieSizeY = dieSizeY,
                CenterStepX = centerStepX,
                CenterStepY = centerStepY
            };

            try
            {
                string basePath = _project != null
                    ? RecipeMapPaths.ResolveBaseConfigured(_project, ResolveBaseMapKind(_currentRoleIsOutput))
                    : "";
                DieMap baseMap = !string.IsNullOrWhiteSpace(basePath) && File.Exists(basePath)
                    ? DieMapGenerator.Load(basePath)
                    : null;
                if (baseMap != null && baseMap.Entries != null && baseMap.Entries.Count > 0)
                {
                    var entries = baseMap.Entries.Where(entry => entry != null).ToList();
                    int minRawX = entries.Min(entry => entry.OriginalMapX >= 0 ? entry.OriginalMapX : entry.DieMapX);
                    int maxRawX = entries.Max(entry => entry.OriginalMapX >= 0 ? entry.OriginalMapX : entry.DieMapX);
                    int minRawY = entries.Min(entry => entry.OriginalMapY >= 0 ? entry.OriginalMapY : entry.DieMapY);
                    int maxRawY = entries.Max(entry => entry.OriginalMapY >= 0 ? entry.OriginalMapY : entry.DieMapY);
                    preview.LoadedRawGridX = Math.Max(1, maxRawX - minRawX + 1);
                    preview.LoadedRawGridY = Math.Max(1, maxRawY - minRawY + 1);
                    preview.LoadedTargetCount = entries.Count(entry => entry.IsTarget);
                }
            }
            catch
            {
                preview.LoadedTargetCount = -1;
            }

            return preview;
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string EmptyDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        private void ApplyRecipeDieSizeToControls()
        {
            if (_nDieSizeX == null || _nDieSizeY == null)
                return;

            _nDieSizeX.Value = ClampDecimal((decimal)ResolveRecipeDieSizeX(), _nDieSizeX.Minimum, _nDieSizeX.Maximum);
            _nDieSizeY.Value = ClampDecimal((decimal)ResolveRecipeDieSizeY(), _nDieSizeY.Minimum, _nDieSizeY.Maximum);
        }

        private double ResolveRecipeDieSizeX()
        {
            return _project != null && _project.Die != null && _project.Die.WidthMm > 0.0
                ? _project.Die.WidthMm
                : 1.0;
        }

        private double ResolveRecipeDieSizeY()
        {
            return _project != null && _project.Die != null && _project.Die.HeightMm > 0.0
                ? _project.Die.HeightMm
                : 1.0;
        }

        private void UpdateDerivedControlAccess()
        {
            bool baseConnected = IsBaseMapConnected();
            _nGridX.Enabled = true;
            _nGridX.ReadOnly = false;
            _nGridX.TabStop = true;
            _nGridY.Enabled = true;
            _nGridY.ReadOnly = false;
            _nGridY.TabStop = true;
            _cbEdgeSkipMode.Enabled = !baseConnected;

            bool externalMode = GetSelectedEdgeSkipModeName().IndexOf("External", StringComparison.OrdinalIgnoreCase) >= 0;
            _nSideEdgeSkip.Enabled = !baseConnected && !externalMode;
            _nSideEdgeSkip.TabStop = _nSideEdgeSkip.Enabled;
            _nTopBottomEdgeSkip.Enabled = !baseConnected && !externalMode;
            _nTopBottomEdgeSkip.TabStop = _nTopBottomEdgeSkip.Enabled;

            string derivedTip = baseConnected
                ? "LOAD WAFER MAP으로 연결한 Base Map의 주소 영역에서 자동 결정됩니다."
                : "Base Map이 연결되지 않은 Recipe에서 직접 설정합니다.";
            toolTipRecipeLocation.SetToolTip(lblGridX, derivedTip);
            toolTipRecipeLocation.SetToolTip(_nGridX, derivedTip);
            toolTipRecipeLocation.SetToolTip(lblGridY, derivedTip);
            toolTipRecipeLocation.SetToolTip(_nGridY, derivedTip);
        }

        private void UpdateEdgeSkipModeUi()
        {
            if (_cbEdgeSkipMode == null)
                return;

            bool mmMode = GetSelectedEdgeSkipModeName().IndexOf("Millimeter", StringComparison.OrdinalIgnoreCase) >= 0;
            _nSideEdgeSkip.DecimalPlaces = mmMode ? 3 : 0;
            _nTopBottomEdgeSkip.DecimalPlaces = mmMode ? 3 : 0;
            _nSideEdgeSkip.Increment = mmMode ? 0.1M : 1M;
            _nTopBottomEdgeSkip.Increment = mmMode ? 0.1M : 1M;
            decimal max = mmMode ? Math.Max(0.001M, _nDiameter.Value / 2M) : 500M;
            _nSideEdgeSkip.Maximum = max;
            _nTopBottomEdgeSkip.Maximum = max;
            UpdateDerivedControlAccess();
        }

        private void SetSelectedEdgeSkipMode(string mode)
        {
            if (_cbEdgeSkipMode == null)
                return;

            string text = EdgeSkipGridText;
            if (!string.IsNullOrWhiteSpace(mode))
            {
                if (mode.IndexOf("EXTERNAL", StringComparison.OrdinalIgnoreCase) >= 0)
                    text = EdgeSkipExternalMapText;
                else if (mode.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         mode.IndexOf("MILLI", StringComparison.OrdinalIgnoreCase) >= 0)
                    text = EdgeSkipMmText;
            }

            _cbEdgeSkipMode.SelectedItem = text;
            if (_cbEdgeSkipMode.SelectedIndex < 0)
                _cbEdgeSkipMode.SelectedIndex = 0;
            UpdateEdgeSkipModeUi();
        }

        private string GetSelectedEdgeSkipModeName()
        {
            string text = _cbEdgeSkipMode != null && _cbEdgeSkipMode.SelectedItem != null
                ? _cbEdgeSkipMode.SelectedItem.ToString()
                : "";
            if (text.IndexOf("EXTERNAL", StringComparison.OrdinalIgnoreCase) >= 0)
                return "ExternalMap";
            if (text.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Millimeter";
            return "Grid";
        }

        private static decimal GetEdgeSkipControlValue(TapeFrameSpec spec)
        {
            bool mmMode = spec != null && !string.IsNullOrWhiteSpace(spec.EdgeSkipMode) &&
                          spec.EdgeSkipMode.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0;
            return (decimal)(mmMode ? spec.SideEdgeSkipMm : spec.SideEdgeSkip);
        }

        private static decimal GetTopBottomEdgeSkipControlValue(TapeFrameSpec spec)
        {
            bool mmMode = spec != null && !string.IsNullOrWhiteSpace(spec.EdgeSkipMode) &&
                          spec.EdgeSkipMode.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0;
            return (decimal)(mmMode ? spec.TopBottomEdgeSkipMm : spec.TopBottomEdgeSkip);
        }

        private static decimal GetEdgeSkipControlValue(TapeFrameSubset frame)
        {
            bool mmMode = frame != null && !string.IsNullOrWhiteSpace(frame.EdgeSkipMode) &&
                          frame.EdgeSkipMode.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0;
            return (decimal)(mmMode ? frame.SideEdgeSkipMm : frame.SideEdgeSkip);
        }

        private static decimal GetTopBottomEdgeSkipControlValue(TapeFrameSubset frame)
        {
            bool mmMode = frame != null && !string.IsNullOrWhiteSpace(frame.EdgeSkipMode) &&
                          frame.EdgeSkipMode.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0;
            return (decimal)(mmMode ? frame.TopBottomEdgeSkipMm : frame.TopBottomEdgeSkip);
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
