using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>Die subset editor. Values are stored in RecipeProject.Die and mirrored to MaterialSpecs.</summary>
    public partial class DieSubsetPage : SubsetPageBase
    {
        private bool _loadingControls;

        protected override bool SaveToRecipePersistsProject => true;
        protected override bool SaveToRecipeAppliesCurrentRecipe => true;

        public DieSubsetPage() : base("recipe.dieSubset")
        {
            InitializeComponent();
        }

        protected override void BuildEditor(Panel c)
        {
            // 편집영역 배경 흰색 통일 (그룹박스 배치는 Designer에서 구성)
            if (c != null)
                c.BackColor = System.Drawing.Color.White;

            HookDirtyTracking();
        }

        protected override void LoadFromRecipe()
        {
            _loadingControls = true;
            try
            {
                var d = _project.Die ?? new DieSubset();
                RefreshSpecList(d.DieSpecName);
                _tbName.Text = d.DieSpecName ?? "";
                _nW.Value = ClampDecimal((decimal)d.WidthMm, _nW.Minimum, _nW.Maximum);
                _nH.Value = ClampDecimal((decimal)d.HeightMm, _nH.Minimum, _nH.Maximum);
                _nT.Value = ClampDecimal((decimal)d.ThicknessMm, _nT.Minimum, _nT.Maximum);
                _nWLow.Value = ClampDecimal((decimal)d.ChipLowerSpecLimitWidth, _nWLow.Minimum, _nWLow.Maximum);
                _nWUp.Value = ClampDecimal((decimal)d.ChipUpperSpecLimitWidth, _nWUp.Minimum, _nWUp.Maximum);
                _nHLow.Value = ClampDecimal((decimal)d.ChipLowerSpecLimitHeight, _nHLow.Minimum, _nHLow.Maximum);
                _nHUp.Value = ClampDecimal((decimal)d.ChipUpperSpecLimitHeight, _nHUp.Minimum, _nHUp.Maximum);
                _nChipDepth.Value = ClampDecimal((decimal)d.ChippingDepthMax, _nChipDepth.Minimum, _nChipDepth.Maximum);
                _nChipLen.Value = ClampDecimal((decimal)d.ChippingLengthMax, _nChipLen.Minimum, _nChipLen.Maximum);
                _nForeign.Value = ClampDecimal((decimal)d.ForeignSizeMax, _nForeign.Minimum, _nForeign.Maximum);
                UpdateCurrentRecipeInfo();
                SetOperationStatus("RECIPE LOAD", "현재 Recipe의 Die 설정을 화면에 불러왔습니다. 값 변경 전 상태입니다.", false);
            }
            finally
            {
                _loadingControls = false;
            }
        }

        protected override void SaveToRecipe()
        {
            SaveDieSettings(false);
        }

        private void btnLoadSpec_Click(object sender, EventArgs e)
        {
            try
            {
                string specName = _cbSpecLibrary.SelectedItem != null ? _cbSpecLibrary.SelectedItem.ToString() : "";
                if (string.IsNullOrWhiteSpace(specName))
                    throw new InvalidDataException("[LOAD SPEC] Spec library에서 불러올 사양을 먼저 선택하세요.");

                MaterialSpecs.Load();
                var spec = MaterialSpecs.FindDie(specName);
                if (spec == null)
                    throw new InvalidDataException("[LOAD SPEC] 선택한 Die Spec을 material_specs.json에서 찾을 수 없습니다. name=" + specName);

                _loadingControls = true;
                try
                {
                    ApplySpecToControls(spec);
                }
                finally
                {
                    _loadingControls = false;
                }

                SetOperationStatus(
                    "LOAD SPEC",
                    "'" + specName + "' 값을 화면에만 불러왔습니다. 아직 현재 Recipe에는 반영되지 않았습니다. 상단 SAVE 또는 SAVE SPEC을 누르세요.",
                    false);
            }
            catch (Exception ex)
            {
                SetOperationStatus("LOAD SPEC FAILED", ex.Message, true);
                MessageBox.Show("Die Spec 불러오기 실패\r\n\r\n" + ex.Message, "Die Spec", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnSaveSpec_Click(object sender, EventArgs e)
        {
            try
            {
                SaveDieSettings(true);
                MessageBox.Show(
                    "Die Spec library 저장 및 현재 Recipe 적용이 완료되었습니다.\r\n\r\nRecipe: " + _project.FileName +
                    "\r\nSpec: " + _project.Die.DieSpecName,
                    "Die Spec",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Die Spec 저장 실패\r\n\r\n" + ex.Message, "Die Spec", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void RefreshSpecList(string selectedName)
        {
            try
            {
                string selected = selectedName ?? "";
                _cbSpecLibrary.Items.Clear();
                MaterialSpecs.Load();
                if (MaterialSpecs.Data != null && MaterialSpecs.Data.Dies != null)
                {
                    foreach (var spec in MaterialSpecs.Data.Dies)
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
            catch
            {
            }
            finally
            {
            }
        }

        private void ApplySpecToControls(DieSpec spec)
        {
            if (spec == null)
                return;

            _tbName.Text = spec.Name ?? "";
            _nW.Value = ClampDecimal((decimal)spec.WidthMm, _nW.Minimum, _nW.Maximum);
            _nH.Value = ClampDecimal((decimal)spec.HeightMm, _nH.Minimum, _nH.Maximum);
            _nT.Value = ClampDecimal((decimal)spec.ThicknessMm, _nT.Minimum, _nT.Maximum);
            _nWLow.Value = ClampDecimal((decimal)spec.WidthLower, _nWLow.Minimum, _nWLow.Maximum);
            _nWUp.Value = ClampDecimal((decimal)spec.WidthUpper, _nWUp.Minimum, _nWUp.Maximum);
            _nHLow.Value = ClampDecimal((decimal)spec.HeightLower, _nHLow.Minimum, _nHLow.Maximum);
            _nHUp.Value = ClampDecimal((decimal)spec.HeightUpper, _nHUp.Minimum, _nHUp.Maximum);
            _nChipDepth.Value = ClampDecimal((decimal)spec.ChippingDepthMax, _nChipDepth.Minimum, _nChipDepth.Maximum);
            _nChipLen.Value = ClampDecimal((decimal)spec.ChippingLengthMax, _nChipLen.Minimum, _nChipLen.Maximum);
            _nForeign.Value = ClampDecimal((decimal)spec.ForeignSizeMax, _nForeign.Minimum, _nForeign.Maximum);
        }

        private void SaveDieSettings(bool saveSpecButton)
        {
            string stage = "VALIDATION";
            bool recipePersisted = false;
            bool libraryPersisted = false;
            try
            {
                if (_project == null)
                    throw new InvalidOperationException("로드된 현재 Recipe Project가 없습니다. Project 페이지에서 Recipe를 선택한 뒤 Reload 하세요.");
                if (string.IsNullOrWhiteSpace(_project.FileName))
                    throw new InvalidDataException("현재 Recipe의 FileName이 비어 있어 저장 대상 파일을 결정할 수 없습니다.");

                var die = _project.Die ?? (_project.Die = new DieSubset());
                ApplyControlsToDie(die);
                ValidateDie(die);

                stage = "RECIPE SYNCHRONIZE";
                RecipeProjectConsistencyService.SynchronizeDieSpecification(_project);

                // SAVE SPEC은 library 저장을 명시적으로 요청하는 버튼이다.
                // Recipe/Map 저장 전에 library 쓰기와 재읽기 검증을 먼저 수행한다.
                if (saveSpecButton)
                {
                    stage = "SPEC LIBRARY SAVE";
                    PersistAndVerifyMaterialSpec(die);
                    libraryPersisted = true;
                }

                string inputBaseMapPath = RecipeMapPaths.ResolveBaseConfigured(_project, RecipeMapKind.Input);
                string outputBaseMapPath = RecipeMapPaths.ResolveBaseConfigured(_project, RecipeMapKind.GoodBin);
                bool hasInputBaseMap = !string.IsNullOrWhiteSpace(
                    RecipeMapPaths.ConfiguredBaseFileName(_project, RecipeMapKind.Input));
                bool hasOutputBaseMap = !string.IsNullOrWhiteSpace(
                    RecipeMapPaths.ConfiguredBaseFileName(_project, RecipeMapKind.GoodBin));
                if (hasInputBaseMap || hasOutputBaseMap)
                {
                    stage = "BASE MAP CHECK";
                    if (hasInputBaseMap && (string.IsNullOrWhiteSpace(inputBaseMapPath) || !File.Exists(inputBaseMapPath)))
                    {
                        throw new FileNotFoundException(
                            "Recipe에 Input Base WaferMap이 설정되어 있지만 파일을 찾을 수 없습니다. 웨이퍼 사양 페이지에서 INPUT 역할을 선택한 뒤 LOAD WAFER MAP을 다시 실행하세요.",
                            inputBaseMapPath);
                    }
                    if (hasOutputBaseMap && (string.IsNullOrWhiteSpace(outputBaseMapPath) || !File.Exists(outputBaseMapPath)))
                    {
                        throw new FileNotFoundException(
                            "Recipe에 Output Base WaferMap이 설정되어 있지만 파일을 찾을 수 없습니다. 웨이퍼 사양 페이지에서 OUTPUT 역할을 선택한 뒤 LOAD WAFER MAP을 다시 실행하세요.",
                            outputBaseMapPath);
                    }

                    stage = "WAFER MAP REBUILD";
                    RecipeMapBuildResult result = RecipeMapBuildService.RebuildDerivedMaps(
                        _project,
                        hasInputBaseMap,
                        hasOutputBaseMap,
                        true,
                        RecipeStore.Save);
                    if (!result.Success)
                    {
                        throw new InvalidOperationException(
                            "Die 규격 반영 후 역할별 Base/Input/Good/NG WaferMap 재생성에 실패했습니다. 원인: " + result.Message);
                    }
                    recipePersisted = true;
                }
                else
                {
                    stage = "PROJECT FILE SAVE";
                    if (!RecipeStore.Save(_project))
                    {
                        string recipePath = Path.Combine(RecipeStore.Dir, _project.FileName + ".Project");
                        throw new IOException(
                            "Project Recipe 파일을 기록하거나 저장 후 검증하지 못했습니다. 경로/쓰기 권한/디스크 상태를 확인하세요. path=" + recipePath);
                    }
                    recipePersisted = true;
                }

                // 상단 SAVE도 실행 중 MaterialSpec 참조가 이전 값으로 남지 않도록
                // 동일 이름의 runtime spec을 동기화하고 실제 파일 재읽기로 검증한다.
                if (!saveSpecButton)
                {
                    stage = "RUNTIME SPEC SYNCHRONIZE";
                    PersistAndVerifyMaterialSpec(die);
                    libraryPersisted = true;
                }

                stage = "CURRENT RECIPE APPLY";
                ApplySavedRecipeToHost();
                RefreshSpecList(die.DieSpecName);
                UpdateCurrentRecipeInfo();

                string mapResult = hasInputBaseMap || hasOutputBaseMap
                    ? "연결된 역할별 Base와 파생 맵도 새 Die 크기로 재생성했습니다. 공정 사용 전 Map Create에서 해당 역할을 확인하고 FINAL APPLY 하세요."
                    : "Input/Output Base WaferMap이 연결되지 않아 Recipe 값만 저장했습니다.";
                SetOperationStatus(
                    saveSpecButton ? "SAVE SPEC COMPLETE" : "SAVE COMPLETE",
                    "현재 Recipe '" + _project.FileName + "'에 " + die.WidthMm.ToString("0.####") + " x " +
                    die.HeightMm.ToString("0.####") + " x " + die.ThicknessMm.ToString("0.####") +
                    " mm를 적용했습니다. " + mapResult,
                    false); 
            }
            catch (Exception ex)
            {
                string partial = recipePersisted
                    ? " Recipe 파일/맵 저장은 완료됐지만 이후 적용 단계에서 실패했습니다."
                    : (libraryPersisted
                        ? " Spec library 저장은 완료됐지만 Recipe 파일/맵 저장은 완료되지 않았습니다."
                        : " Recipe 파일에는 완료 상태로 저장되지 않았습니다.");
                string detail = "[" + stage + "] " + ex.Message + partial;
                SetOperationStatus("SAVE FAILED", detail, true);
                throw new InvalidOperationException(detail, ex);
            }
            finally
            {
            }
        }

        private void ApplyControlsToDie(DieSubset d)
        {
            if (d == null)
                return;

            string specName = (_tbName.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(specName))
                throw new InvalidDataException("Die Spec name을 입력하세요.");

            d.DieSpecName = specName;
            d.WidthMm = (double)_nW.Value;
            d.HeightMm = (double)_nH.Value;
            d.ThicknessMm = (double)_nT.Value;
            d.ChipLowerSpecLimitWidth = (double)_nWLow.Value;
            d.ChipUpperSpecLimitWidth = (double)_nWUp.Value;
            d.ChipLowerSpecLimitHeight = (double)_nHLow.Value;
            d.ChipUpperSpecLimitHeight = (double)_nHUp.Value;
            d.ChippingDepthMax = (double)_nChipDepth.Value;
            d.ChippingLengthMax = (double)_nChipLen.Value;
            d.ForeignSizeMax = (double)_nForeign.Value;
        }

        private static void ValidateDie(DieSubset d)
        {
            if (d == null)
                throw new InvalidDataException("Die 설정 객체가 없습니다.");
            if (d.WidthMm <= 0.0 || d.HeightMm <= 0.0 || d.ThicknessMm <= 0.0)
                throw new InvalidDataException("Width, Height, Thickness는 모두 0보다 커야 합니다.");
            if (d.ChipLowerSpecLimitWidth > d.ChipUpperSpecLimitWidth)
                throw new InvalidDataException("Width lower는 Width upper보다 클 수 없습니다.");
            if (d.ChipLowerSpecLimitHeight > d.ChipUpperSpecLimitHeight)
                throw new InvalidDataException("Height lower는 Height upper보다 클 수 없습니다.");
        }

        private void PersistAndVerifyMaterialSpec(DieSubset die)
        {
            string syncedName = MaterialStateService.SyncRecipeDieSpec(_project);
            if (string.IsNullOrWhiteSpace(syncedName))
                throw new IOException("Material Die Spec 동기화가 실패했습니다. material_specs.json 쓰기 상태와 로그를 확인하세요.");

            MaterialSpecs.Load();
            DieSpec saved = FindMaterialSpecIgnoreCase(die.DieSpecName);
            if (saved == null)
                throw new IOException("material_specs.json 저장 후 동일 이름의 Die Spec을 다시 찾지 못했습니다. name=" + die.DieSpecName);

            if (!NearlyEqual(saved.WidthMm, die.WidthMm) ||
                !NearlyEqual(saved.HeightMm, die.HeightMm) ||
                !NearlyEqual(saved.ThicknessMm, die.ThicknessMm) ||
                !NearlyEqual(saved.WidthLower, die.ChipLowerSpecLimitWidth) ||
                !NearlyEqual(saved.WidthUpper, die.ChipUpperSpecLimitWidth) ||
                !NearlyEqual(saved.HeightLower, die.ChipLowerSpecLimitHeight) ||
                !NearlyEqual(saved.HeightUpper, die.ChipUpperSpecLimitHeight) ||
                !NearlyEqual(saved.ChippingDepthMax, die.ChippingDepthMax) ||
                !NearlyEqual(saved.ChippingLengthMax, die.ChippingLengthMax) ||
                !NearlyEqual(saved.ForeignSizeMax, die.ForeignSizeMax))
            {
                throw new IOException("material_specs.json 저장 후 재읽은 값이 화면 입력값과 다릅니다. 다른 저장 작업 또는 파일 쓰기 실패 여부를 확인하세요.");
            }
        }

        private static DieSpec FindMaterialSpecIgnoreCase(string name)
        {
            if (MaterialSpecs.Data == null || MaterialSpecs.Data.Dies == null)
                return null;

            foreach (DieSpec spec in MaterialSpecs.Data.Dies)
            {
                if (spec != null && string.Equals(spec.Name, name, StringComparison.OrdinalIgnoreCase))
                    return spec;
            }
            return null;
        }

        private void ApplySavedRecipeToHost()
        {
            var host = FindForm() as Form1;
            if (host == null)
            {
                RecipeStore.SaveLastProjectName(_project.FileName);
                return;
            }

            if (host.Machine != null)
            {
                if (!host.SaveMachineRecipe(_project.FileName))
                {
                    throw new IOException(
                        "장비 Unit Recipe 저장이 false를 반환했습니다. Project Die 값은 저장됐지만 장비 Recipe 적용을 완료하지 못했습니다. recipe=" + _project.FileName);
                }
                host.LoadMachineRecipe(_project.FileName);
            }

            host.RefreshProjectName(_project.FileName);
            RecipeStore.SaveLastProjectName(_project.FileName);
        }

        private void HookDirtyTracking()
        {
            _tbName.TextChanged += OnEditorValueChanged;
            _nW.ValueChanged += OnEditorValueChanged;
            _nH.ValueChanged += OnEditorValueChanged;
            _nT.ValueChanged += OnEditorValueChanged;
            _nWLow.ValueChanged += OnEditorValueChanged;
            _nWUp.ValueChanged += OnEditorValueChanged;
            _nHLow.ValueChanged += OnEditorValueChanged;
            _nHUp.ValueChanged += OnEditorValueChanged;
            _nChipDepth.ValueChanged += OnEditorValueChanged;
            _nChipLen.ValueChanged += OnEditorValueChanged;
            _nForeign.ValueChanged += OnEditorValueChanged;
        }

        private void OnEditorValueChanged(object sender, EventArgs e)
        {
            if (_loadingControls)
                return;
            SetOperationStatus(
                "UNSAVED CHANGES",
                "화면 값이 변경되었습니다. 현재 Recipe 파일과 실행 데이터에는 아직 반영되지 않았습니다. 상단 SAVE 또는 SAVE SPEC을 누르세요.",
                false);
        }

        private void UpdateCurrentRecipeInfo()
        {
            if (_lblCurrentRecipeInfo == null)
                return;

            string recipeName = _project != null && !string.IsNullOrWhiteSpace(_project.FileName)
                ? _project.FileName
                : "(없음)";
            string inputBaseMap = _project != null
                ? RecipeMapPaths.ResolveBaseConfigured(_project, RecipeMapKind.Input)
                : "";
            string outputBaseMap = _project != null
                ? RecipeMapPaths.ResolveBaseConfigured(_project, RecipeMapKind.GoodBin)
                : "";
            string inputMapState = string.IsNullOrWhiteSpace(inputBaseMap)
                ? "연결 안 됨"
                : (File.Exists(inputBaseMap) ? Path.GetFileName(inputBaseMap) : "파일 없음: " + inputBaseMap);
            string outputMapState = string.IsNullOrWhiteSpace(outputBaseMap)
                ? "연결 안 됨"
                : (File.Exists(outputBaseMap) ? Path.GetFileName(outputBaseMap) : "파일 없음: " + outputBaseMap);

            _lblCurrentRecipeInfo.Text = "현재 Recipe : " + recipeName +
                "\r\nBase WaferMap : Input=" + inputMapState + " / Output=" + outputMapState;
        }

        private void SetOperationStatus(string operation, string detail, bool isError)
        {
            if (_txtOperationStatus == null)
                return;

            _txtOperationStatus.ForeColor = isError
                ? Color.FromArgb(190, 45, 35)
                : Color.FromArgb(45, 65, 80);
            _txtOperationStatus.Text = DateTime.Now.ToString("HH:mm:ss") + "  [" + operation + "]\r\n" + (detail ?? "");
        }

        private static bool NearlyEqual(double left, double right)
        {
            return Math.Abs(left - right) <= 0.0000001;
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
