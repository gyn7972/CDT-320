using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class MapCreatePage : PageBase
    {
        private readonly string _titleI18n;
        private DieMap _map;
        private RecipeProject _project;

        /// <summary>맵 에디터 모드. 입력 웨이퍼 맵 1종 + 출력(빈) 맵 GOOD/NG 2종.</summary>
        private enum MapEditorMode { Input, OutputGood, OutputNg }
        private MapEditorMode _mode = MapEditorMode.Input;

        /// <summary>입력 맵이 아니면(=빈 맵이면) true. 기존 호출부 호환용 계산 속성.</summary>
        private bool _isOutputMap { get { return _mode != MapEditorMode.Input; } }

        /// <summary>현재 모드에 대응하는 레시피 맵 종류(경로/파일명 해석에 사용).</summary>
        private RecipeMapKind CurrentMapKind
        {
            get
            {
                switch (_mode)
                {
                    case MapEditorMode.OutputGood: return RecipeMapKind.GoodBin;
                    case MapEditorMode.OutputNg: return RecipeMapKind.NgBin;
                    default: return RecipeMapKind.Input;
                }
            }
        }
        private ContextMenuStrip _mapMenu;
        private string _currentMapPath;
        private string _currentFrameSpecName;
        private readonly Dictionary<string, string> _mapLibraryPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DieMap> _mapLibraryMemoryMaps = new Dictionary<string, DieMap>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TapeFrameSpec> _mapLibraryFrameSpecs = new Dictionary<string, TapeFrameSpec>(StringComparer.OrdinalIgnoreCase);

        public MapCreatePage() : this("recipe.inputMapCreate")
        {
        }

        public MapCreatePage(string titleI18n)
        {
            _titleI18n = titleI18n;
            InitializeComponent();
            ApplyTitle();
            InitializeMapEditor();
        }

        private void ApplyTitle()
        {
            lblHeader.Tag = "i18n:" + _titleI18n;
            lblHeader.Text = Lang.T(_titleI18n);
            // 빈 맵 진입(recipe.binMapCreate) 또는 레거시 출력 맵 진입(recipe.outputMapCreate) → 빈 GOOD 시작.
            if (string.Equals(_titleI18n, "recipe.binMapCreate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_titleI18n, "recipe.outputMapCreate", StringComparison.OrdinalIgnoreCase))
                _mode = MapEditorMode.OutputGood;
            else
                _mode = MapEditorMode.Input;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (IsDesignerMode())
                return;

            LoadRecipeMapOrCreatePreview();
        }

        private void InitializeMapEditor()
        {
            _mapView.Caption = "Recipe Die Map";
            _mapView.CellClicked += OnMapCellClicked;
            _mapMenu = BuildMapContextMenu();
            _mapView.ContextMenuStrip = _mapMenu;

            btnCreate.Text = "CREATE DIE MAP";
            btnSave.Text = "SAVE DIE MAP";
            btnFirstDieMoveComplete.Text = "LOAD RECIPE MAP";
            btnAutoMatch.Text = "IMPORT DIE MAP";
            btnThetaMatchMove.Text = "EXPORT DIE MAP";
            btnXyMatchMove.Text = "INVERT TARGET";
            _btnMapNew.Text = "SAVE AS";
            rbStandard.Text = "CLICK TOGGLE";
            rbManualSelectPick.Text = "CLICK TARGET";
            rbAlignCheckIndex.Text = "CLICK SKIP";
            rbDragSelectPick.Text = "RIGHT CLICK MENU";
            // 입력/빈 모두 원형 형상 고정(직사각 미사용). 토글 비활성·체크 유지.
            chkCircularMap.Text = _isOutputMap ? "BIN CIRCLE DIE MAP" : "INPUT CIRCLE DIE MAP";
            chkCircularMap.Checked = true;
            chkCircularMap.Enabled = false;
            rbStartIndex.Enabled = false;
            rbReference1.Enabled = false;
            rbReference2.Enabled = false;
            HookClickModeEvents();
            ClearMapClickModes();
            UpdateMapInteractionMode();
            ConfigureBinSideToggle();

            _btnMapLoad.Click += (s, e) => LoadSelectedLibraryMap();
            _btnMapNew.Click += (s, e) => SaveMapAsLibraryMap();
            _btnMapRename.Click += (s, e) => RenameSelectedLibraryMap();
            _btnMapDelete.Click += (s, e) => DeleteSelectedLibraryMap();
            btnCreate.Click += (s, e) => CreateMapFromRecipeSpec(true);
            btnSave.Click += (s, e) => SaveMapToRecipe();
            btnFirstDieMoveComplete.Click += (s, e) => LoadSavedRecipeMap(true);
            btnAutoMatch.Click += (s, e) => ImportMapFile();
            btnThetaMatchMove.Click += (s, e) => ExportMapCsv();
            btnXyMatchMove.Click += (s, e) => InvertMapTargets();
        }

        /// <summary>빈 맵 모드에서만 GOOD/NG 토글을 노출하고 형상 파라미터 편집을 허용한다.</summary>
        private void ConfigureBinSideToggle()
        {
            bool bin = _isOutputMap;
            binSidePanel.Visible = bin;
            rbBinGood.Visible = bin;
            rbBinNg.Visible = bin;
            if (!bin)
                return;

            rbBinGood.Text = "GOOD BIN MAP";
            rbBinNg.Text = "NG BIN MAP";
            rbBinGood.Checked = _mode != MapEditorMode.OutputNg;
            rbBinNg.Checked = _mode == MapEditorMode.OutputNg;
            rbBinGood.CheckedChanged += OnBinSideChanged;
            rbBinNg.CheckedChanged += OnBinSideChanged;

            // 빈 맵은 별도 SPEC 페이지가 없으므로 형상 파라미터를 이 화면에서 편집한다.
            EnableBinAuthoringControls();
        }

        private void OnBinSideChanged(object sender, EventArgs e)
        {
            try
            {
                RadioButton rb = sender as RadioButton;
                if (rb == null || !rb.Checked)
                    return;

                MapEditorMode next = rbBinNg.Checked ? MapEditorMode.OutputNg : MapEditorMode.OutputGood;
                if (next == _mode)
                    return;

                _mode = next;
                chkCircularMap.Text = "BIN CIRCLE DIE MAP";
                _currentMapPath = "";
                _currentFrameSpecName = "";
                if (!LoadSavedRecipeMap(false))
                    CreateMapFromRecipeSpec(false);
                RefreshMapLibraryList();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "RECIPE", "MapCreatePage",
                    "Bin side toggle failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void EnableBinAuthoringControls()
        {
            SetNumericEditable(_nGridX);
            SetNumericEditable(_nGridY);
            SetNumericEditable(_nPitchX);
            SetNumericEditable(_nPitchY);
            SetNumericEditable(_nDiameter);
            SetNumericEditable(_nSideEdgeSkip);
            SetNumericEditable(_nTopBottomEdgeSkip);
        }

        private static void SetNumericEditable(NumericUpDown control)
        {
            if (control == null)
                return;
            control.Enabled = true;
            control.ReadOnly = false;
            control.TabStop = true;
        }

        private void LoadEdgeSkipFromRecipe()
        {
            try
            {
                TapeFrameSubset frame = _project != null ? _project.Frame : null;
                if (frame == null)
                    return;

                _tbFrameSpecName.Text = frame.FrameSpecName ?? "";
                _currentFrameSpecName = MaterialSpecs.FindFrame(frame.FrameSpecName) != null ? frame.FrameSpecName : "";
                _nGridX.Value = ClampDecimal(frame.DieMapX, _nGridX.Minimum, _nGridX.Maximum);
                _nGridY.Value = ClampDecimal(frame.DieMapY, _nGridY.Minimum, _nGridY.Maximum);
                _nPitchX.Value = ClampDecimal(frame.PitchX, _nPitchX.Minimum, _nPitchX.Maximum);
                _nPitchY.Value = ClampDecimal(frame.PitchY, _nPitchY.Minimum, _nPitchY.Maximum);
                _nDiameter.Value = ClampDecimal(frame.OuterDiameterMm, _nDiameter.Minimum, _nDiameter.Maximum);
                _nSideEdgeSkip.Value = ClampDecimal(frame.SideEdgeSkip, _nSideEdgeSkip.Minimum, _nSideEdgeSkip.Maximum);
                _nTopBottomEdgeSkip.Value = ClampDecimal(frame.TopBottomEdgeSkip, _nTopBottomEdgeSkip.Minimum, _nTopBottomEdgeSkip.Maximum);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private ContextMenuStrip BuildMapContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Opening += OnMapContextMenuOpening;
            menu.Items.Add("TARGET ALL", null, (s, e) => SetAllTargets(true));
            menu.Items.Add("SKIP ALL", null, (s, e) => SetAllTargets(false));
            menu.Items.Add("INVERT TARGET", null, (s, e) => InvertMapTargets());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("CREATE DIE MAP", null, (s, e) => CreateMapFromRecipeSpec(true));
            menu.Items.Add("LOAD RECIPE MAP", null, (s, e) => LoadSavedRecipeMap(true));
            menu.Items.Add("IMPORT DIE MAP", null, (s, e) => ImportMapFile());
            menu.Items.Add("SAVE DIE MAP", null, (s, e) => SaveMapToRecipe());
            return menu;
        }

        private void HookClickModeEvents()
        {
            try
            {
                rbStandard.CheckedChanged += OnMapClickModeChanged;
                rbManualSelectPick.CheckedChanged += OnMapClickModeChanged;
                rbAlignCheckIndex.CheckedChanged += OnMapClickModeChanged;
                rbDragSelectPick.CheckedChanged += OnMapClickModeChanged;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ClearMapClickModes()
        {
            try
            {
                rbStandard.Checked = false;
                rbManualSelectPick.Checked = false;
                rbAlignCheckIndex.Checked = false;
                rbDragSelectPick.Checked = false;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void OnMapClickModeChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateMapInteractionMode();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void UpdateMapInteractionMode()
        {
            try
            {
                if (_mapView == null)
                    return;

                _mapView.ContextMenuStrip = rbDragSelectPick != null && rbDragSelectPick.Checked
                    ? _mapMenu
                    : null;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void OnMapContextMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                e.Cancel = rbDragSelectPick == null || !rbDragSelectPick.Checked;
            }
            catch
            {
                e.Cancel = true;
            }
            finally
            {
            }
        }

        private void LoadRecipeMapOrCreatePreview()
        {
            try
            {
                _project = RecipeStore.LoadLastOrDefault();
                if (_project == null)
                {
                    QMC.Common.MessageDialog.Show(this, "로드된 Recipe가 없습니다.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                LoadEdgeSkipFromRecipe();
                RefreshMapLibraryList();

                string mapPath = ResolveRecipeMapPath(_project, _isOutputMap);
                if (LoadSavedRecipeMap(false))
                    return;

                CreateMapFromRecipeSpec(false);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Recipe die map load failed:\r\n" + ex.Message, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void CreateMapFromRecipeSpec(bool confirm)
        {
            try
            {
                _project = _project ?? RecipeStore.LoadLastOrDefault();
                if (_project == null)
                    return;

                ApplySelectedFrameSpecToControlsIfNeeded();

                if (confirm && _map != null)
                {
                    DialogResult result = QMC.Common.MessageDialog.Show(this,
                        "현재 Die Map을 Frame Spec 기준으로 다시 생성하시겠습니까?",
                        "Die Map Create", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result != DialogResult.Yes)
                        return;
                }

                DieMap map = _isOutputMap
                    ? CreateOutputMapFromRecipe(_project)
                    : CreateCircleDieMapFromRecipe(_project, false);
                ApplyMap(map, BuildGeneratedCaption());
                RefreshMapLibraryList();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Die map create failed:\r\n" + ex.Message, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private string BuildGeneratedCaption()
        {
            string specName = !string.IsNullOrWhiteSpace(_tbFrameSpecName.Text)
                ? _tbFrameSpecName.Text.Trim()
                : "";
            if (!_isOutputMap)
                return "Generated Input Circle Die Map: " + specName;

            string sideTag = _mode == MapEditorMode.OutputNg ? "NG" : "GOOD";
            return "Generated " + sideTag + " Bin Circle Die Map: " + specName;
        }

        private DieMap CreateOutputMapFromRecipe(RecipeProject project)
        {
            // 빈 맵은 원형 형상 고정(직사각 미사용).
            return CreateCircleDieMapFromRecipe(project, true);
        }

        private DieMap CreateCircleDieMapFromRecipe(RecipeProject project, bool outputMap)
        {
            TapeFrameSubset frame = BuildFrameFromControls();

            int gridX = Math.Max(1, frame.DieMapX);
            int gridY = Math.Max(1, frame.DieMapY);
            double pitchX = frame.PitchX > 0.0 ? frame.PitchX : 1.0;
            double pitchY = frame.PitchY > 0.0 ? frame.PitchY : 1.0;
            double originX = -((gridX - 1) * pitchX) / 2.0;
            double originY = -((gridY - 1) * pitchY) / 2.0;
            int sideEdgeSkip = ResolveEdgeSkip(_nSideEdgeSkip, gridX);
            int topBottomEdgeSkip = ResolveEdgeSkip(_nTopBottomEdgeSkip, gridY);
            double diameterMm = frame.OuterDiameterMm > 0.0 ? frame.OuterDiameterMm : 0.0;

            var map = new DieMap
            {
                FrameObjId = BuildRecipeMapId(project, outputMap),
                DieMapX = gridX,
                DieMapY = gridY,
                PitchX = pitchX,
                PitchY = pitchY,
                OriginX = originX,
                OriginY = originY,
                CreatedAt = DateTime.Now
            };

            int index = 0;
            for (int row = 0; row < gridY; row++)
            {
                for (int col = 0; col < gridX; col++)
                {
                    double x = originX + col * pitchX;
                    double y = originY + row * pitchY;
                    bool target = IsInsideWaferCircle(
                        col,
                        row,
                        gridX,
                        gridY,
                        sideEdgeSkip,
                        topBottomEdgeSkip,
                        x,
                        y,
                        pitchX,
                        pitchY,
                        diameterMm);
                    map.Entries.Add(new DieMapEntry
                    {
                        Index = index++,
                        DieMapX = col,
                        DieMapY = row,
                        IsTarget = target,
                        Result = target ? DieResult.Unknown : DieResult.NG,
                        BinCode = target ? 0 : 255,
                        PosX = x,
                        PosY = y,
                        DieUid = BuildDieId(project, row, col)
                    });
                }
            }

            return ApplyPickupSequence(map, outputMap);
        }

        private static int ResolveEdgeSkip(NumericUpDown control, int gridCount)
        {
            int value = control != null ? (int)control.Value : 0;
            int max = Math.Max(0, (gridCount - 1) / 2);
            if (value < 0) return 0;
            if (value > max) return max;
            return value;
        }

        private static bool IsInsideWaferCircle(
            int col,
            int row,
            int gridX,
            int gridY,
            int sideEdgeSkip,
            int topBottomEdgeSkip,
            double x,
            double y,
            double pitchX,
            double pitchY,
            double diameterMm)
        {
            if (gridX <= 0 || gridY <= 0)
                return false;

            if (col < sideEdgeSkip || col >= gridX - sideEdgeSkip)
                return false;
            if (row < topBottomEdgeSkip || row >= gridY - topBottomEdgeSkip)
                return false;

            double centerX = (gridX - 1) / 2.0;
            double centerY = (gridY - 1) / 2.0;
            double radiusX = Math.Max(0.5, (gridX - 1 - (sideEdgeSkip * 2)) / 2.0);
            double radiusY = Math.Max(0.5, (gridY - 1 - (topBottomEdgeSkip * 2)) / 2.0);
            double nx = (col - centerX) / radiusX;
            double ny = (row - centerY) / radiusY;
            bool insideGridCircle = (nx * nx) + (ny * ny) <= 1.0;
            if (!insideGridCircle)
                return false;

            if (diameterMm <= 0.0)
                return true;

            double activeSpanX = Math.Max(pitchX, (gridX - 1 - (sideEdgeSkip * 2)) * pitchX);
            double activeSpanY = Math.Max(pitchY, (gridY - 1 - (topBottomEdgeSkip * 2)) * pitchY);
            double activeDiameter = Math.Min(activeSpanX, activeSpanY);
            if (diameterMm >= activeDiameter)
                return true;

            double radiusMm = diameterMm / 2.0;
            return (x * x) + (y * y) <= radiusMm * radiusMm;
        }

        private DieMap ApplyPickupSequence(DieMap map, bool outputMap)
        {
            PickupSubset pickup = ResolvePickupSubsetForMap(outputMap);
            LogPickupSequenceSource(outputMap, pickup);
            return PickupSequenceGenerator.ApplySequenceNumbers(map, pickup);
        }

        private PickupSubset ResolvePickupSubsetForMap(bool outputMap)
        {
            RecipeProject project = LoadLatestRecipeForPickup();
            if (project == null)
                return new PickupSubset();

            if (outputMap)
                return project.OutputPickup ?? project.Pickup ?? new PickupSubset();

            return project.InputPickup ?? project.Pickup ?? new PickupSubset();
        }

        private RecipeProject LoadLatestRecipeForPickup()
        {
            try
            {
                RecipeProject latest = null;
                if (_project != null && !string.IsNullOrWhiteSpace(_project.FileName))
                    latest = RecipeStore.Load(_project.FileName);

                if (latest == null)
                    latest = RecipeStore.LoadLastOrDefault();

                if (latest != null)
                    _project = latest;

                return latest;
            }
            catch
            {
                return _project ?? RecipeStore.LoadLastOrDefault();
            }
            finally
            {
            }
        }

        private void LogPickupSequenceSource(bool outputMap, PickupSubset pickup)
        {
            try
            {
                string mapKind = outputMap ? "Output" : "Input";
                string projectName = _project != null ? _project.FileName : "-";
                string pickupText = pickup != null
                    ? pickup.StartCorner + "/" + pickup.Direction + "/" + pickup.Pattern
                    : "-";

                QMC.Common.Log.Write("Main", "RECIPE", "DieMapPickup",
                    "Apply " + mapKind + " die map pickup sequence. project=" +
                    projectName + ", pickup=" + pickupText + " - Ok");
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ApplyMap(DieMap map, string caption)
        {
            _map = DieMapGenerator.Normalize(map);
            ApplyPickupSequence(_map, _isOutputMap);
            ApplyMapToControls(_map);
            _mapView.Caption = caption ?? "Recipe Die Map";
            _mapView.Map = _map;
            RefreshSettingLabels();
        }

        private TapeFrameSubset BuildFrameFromControls()
        {
            return new TapeFrameSubset
            {
                FrameSpecName = string.IsNullOrWhiteSpace(_tbFrameSpecName.Text) ? "RecipeFrame" : _tbFrameSpecName.Text.Trim(),
                DieMapX = Math.Max(1, (int)_nGridX.Value),
                DieMapY = Math.Max(1, (int)_nGridY.Value),
                PitchX = (double)_nPitchX.Value > 0.0 ? (double)_nPitchX.Value : 1.0,
                PitchY = (double)_nPitchY.Value > 0.0 ? (double)_nPitchY.Value : 1.0,
                OuterDiameterMm = (double)_nDiameter.Value > 0.0 ? (double)_nDiameter.Value : 0.0,
                SideEdgeSkip = (int)_nSideEdgeSkip.Value,
                TopBottomEdgeSkip = (int)_nTopBottomEdgeSkip.Value
            };
        }

        private void ApplyMapToControls(DieMap map)
        {
            if (map == null)
                return;

            _nGridX.Value = ClampDecimal(map.DieMapX, _nGridX.Minimum, _nGridX.Maximum);
            _nGridY.Value = ClampDecimal(map.DieMapY, _nGridY.Minimum, _nGridY.Maximum);
            _nPitchX.Value = ClampDecimal(map.PitchX, _nPitchX.Minimum, _nPitchX.Maximum);
            _nPitchY.Value = ClampDecimal(map.PitchY, _nPitchY.Minimum, _nPitchY.Maximum);
        }

        private void RefreshSettingLabels()
        {
            if (_map == null)
            {
                lblMapTitle.Text = "DIE MAP";
                return;
            }

            int targetCount = _map.Entries != null ? _map.Entries.Count(e => e != null && e.IsTarget) : 0;
            lblMapTitle.Text = "DIE MAP  TARGET " + targetCount + " / " + (_map.Entries != null ? _map.Entries.Count : 0);
            _mapView.Invalidate();
        }

        private void OnMapCellClicked(DieMapEntry entry)
        {
            if (entry == null)
                return;

            if (rbManualSelectPick.Checked)
                ApplyEntryTarget(entry, true);
            else if (rbAlignCheckIndex.Checked)
                ApplyEntryTarget(entry, false);
            else if (rbStandard.Checked)
                ApplyEntryTarget(entry, !entry.IsTarget);
            else
                return;

            RefreshSettingLabels();
        }

        private void ApplyEntryTarget(DieMapEntry entry, bool target)
        {
            if (entry == null)
                return;

            entry.IsTarget = target;
            entry.Result = target ? DieResult.Unknown : DieResult.NG;
            entry.BinCode = target ? 0 : 255;
        }

        private void SetAllTargets(bool target)
        {
            try
            {
                if (_map == null || _map.Entries == null)
                    return;

                DialogResult result = QMC.Common.MessageDialog.Show(this,
                    target ? "전체 다이를 TARGET으로 설정하시겠습니까?" : "전체 다이를 SKIP으로 설정하시겠습니까?",
                    "Die Map Create", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    return;

                foreach (DieMapEntry entry in _map.Entries)
                    ApplyEntryTarget(entry, target);

                RefreshSettingLabels();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void InvertMapTargets()
        {
            try
            {
                if (_map == null || _map.Entries == null)
                    return;

                foreach (DieMapEntry entry in _map.Entries)
                {
                    if (entry != null)
                        ApplyEntryTarget(entry, !entry.IsTarget);
                }

                RefreshSettingLabels();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private bool LoadSavedRecipeMap(bool showMessage)
        {
            try
            {
                _project = _project ?? RecipeStore.LoadLastOrDefault();
                if (_project == null)
                    return false;

                string path = ResolveRecipeMapPath(_project, _isOutputMap);
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    if (showMessage)
                        QMC.Common.MessageDialog.Show(this, "Recipe에 연결된 Die Map 파일이 없습니다.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                DieMap loaded = DieMapGenerator.Load(path);
                if (loaded == null)
                {
                    if (showMessage)
                        QMC.Common.MessageDialog.Show(this, "Die Map 파일을 읽을 수 없습니다.\r\n" + path, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                ApplyMap(loaded, "Recipe Die Map: " + Path.GetFileName(path));
                _currentMapPath = path;
                _currentFrameSpecName = "";
                SelectLibraryPath(path);
                return true;
            }
            catch (Exception ex)
            {
                if (showMessage)
                    QMC.Common.MessageDialog.Show(this, "Saved die map load failed:\r\n" + ex.Message, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
            }
        }

        private void RefreshMapLibraryList()
        {
            try
            {
                string selectedPath = _currentMapPath;
                if (string.IsNullOrWhiteSpace(selectedPath) && _project != null)
                    selectedPath = ResolveRecipeMapPath(_project, _isOutputMap);

                _mapLibraryPaths.Clear();
                _mapLibraryMemoryMaps.Clear();
                _mapLibraryFrameSpecs.Clear();
                _cbMapLibrary.Items.Clear();

                MaterialSpecs.Load();
                if (MaterialSpecs.Data != null && MaterialSpecs.Data.Frames != null)
                {
                    foreach (TapeFrameSpec spec in MaterialSpecs.Data.Frames)
                        AddFrameSpecToLibrary(spec);
                }

                if (_map != null)
                    AddMemoryMapToLibrary("[CURRENT] Editing Die Map", _map);

                if (!_isOutputMap && LotStorage.ActiveInputDieMap != null)
                    AddMemoryMapToLibrary("[CURRENT] Active Input Die Map", LotStorage.ActiveInputDieMap);

                string dir = GetDieMapDirectory();
                Directory.CreateDirectory(dir);
                foreach (string path in EnumerateMapFiles(dir))
                    AddMapFileToLibrary(path);

                SelectLibraryPath(selectedPath);
                if (_cbMapLibrary.SelectedIndex < 0 && _cbMapLibrary.Items.Count > 0)
                    _cbMapLibrary.SelectedIndex = 0;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void AddMemoryMapToLibrary(string name, DieMap map)
        {
            if (string.IsNullOrWhiteSpace(name) || map == null)
                return;

            string key = MakeUniqueLibraryKey(name);
            _mapLibraryMemoryMaps[key] = map;
            _cbMapLibrary.Items.Add(key);
        }

        private void AddFrameSpecToLibrary(TapeFrameSpec spec)
        {
            if (spec == null || string.IsNullOrWhiteSpace(spec.Name))
                return;

            string key = MakeUniqueLibraryKey("[SPEC] " + spec.Name.Trim());
            _mapLibraryFrameSpecs[key] = spec;
            _cbMapLibrary.Items.Add(key);
        }

        private void AddMapFileToLibrary(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return;
                if (!ShouldShowMapFile(path))
                    return;

                string key = MakeUniqueLibraryKey(Path.GetFileNameWithoutExtension(path));
                _mapLibraryPaths[key] = path;
                _cbMapLibrary.Items.Add(key);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private bool ShouldShowMapFile(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path) ?? "";
            bool isInputName = name.IndexOf("InputDieMap", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               name.IndexOf("InputMap", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isLegacyOutputName = name.IndexOf("OutputDieMap", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      name.IndexOf("OutputMap", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isGoodBinName = name.IndexOf("GoodBinDieMap", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isNgBinName = name.IndexOf("NgBinDieMap", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!_isOutputMap)
            {
                // 입력 모드: 빈 맵(레거시 출력/GOOD/NG) 숨김.
                return !isLegacyOutputName && !isGoodBinName && !isNgBinName;
            }

            // 빈 모드: 입력 맵 숨김 + 반대 side 숨김. 해당 side 또는 레거시 출력 맵만 노출.
            if (isInputName)
                return false;
            if (CurrentMapKind == RecipeMapKind.NgBin)
                return isNgBinName || isLegacyOutputName;
            return isGoodBinName || isLegacyOutputName;
        }

        private IEnumerable<string> EnumerateMapFiles(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return Enumerable.Empty<string>();

            string[] jsonFiles = Directory.GetFiles(dir, "*.json");
            var jsonBaseNames = new HashSet<string>(
                jsonFiles.Select(path => Path.GetFileNameWithoutExtension(path) ?? ""),
                StringComparer.OrdinalIgnoreCase);

            IEnumerable<string> standaloneCsvFiles = Directory.GetFiles(dir, "*.csv")
                .Where(path => !jsonBaseNames.Contains(Path.GetFileNameWithoutExtension(path) ?? ""));

            return jsonFiles
                .Concat(standaloneCsvFiles)
                .OrderBy(Path.GetFileName);
        }

        private string MakeUniqueLibraryKey(string baseName)
        {
            string key = string.IsNullOrWhiteSpace(baseName) ? "Map" : baseName.Trim();
            string candidate = key;
            int index = 2;
            while (_mapLibraryPaths.ContainsKey(candidate) || _mapLibraryMemoryMaps.ContainsKey(candidate) || _mapLibraryFrameSpecs.ContainsKey(candidate) || _cbMapLibrary.Items.Contains(candidate))
                candidate = key + " (" + index++ + ")";
            return candidate;
        }

        private void SelectLibraryPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return;

                string fullPath = Path.GetFullPath(path);
                foreach (var pair in _mapLibraryPaths)
                {
                    if (string.Equals(Path.GetFullPath(pair.Value), fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        _cbMapLibrary.SelectedItem = pair.Key;
                        return;
                    }
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private string GetSelectedLibraryPath()
        {
            string key = _cbMapLibrary.SelectedItem != null ? _cbMapLibrary.SelectedItem.ToString() : "";
            string path;
            return !string.IsNullOrWhiteSpace(key) && _mapLibraryPaths.TryGetValue(key, out path) ? path : "";
        }

        private void LoadSelectedLibraryMap()
        {
            try
            {
                string key = _cbMapLibrary.SelectedItem != null ? _cbMapLibrary.SelectedItem.ToString() : "";
                TapeFrameSpec frameSpec;
                if (!string.IsNullOrWhiteSpace(key) && _mapLibraryFrameSpecs.TryGetValue(key, out frameSpec))
                {
                    ApplyFrameSpecToControls(frameSpec);
                    _currentMapPath = ResolveCurrentRecipeMapPathOrEmpty();
                    _currentFrameSpecName = frameSpec.Name ?? "";
                    CreateMapFromRecipeSpec(false);
                    SelectFrameSpecName(frameSpec.Name);
                    return;
                }

                DieMap memoryMap;
                if (!string.IsNullOrWhiteSpace(key) && _mapLibraryMemoryMaps.TryGetValue(key, out memoryMap))
                {
                    ApplyMap(memoryMap, "Current Die Map: " + key);
                    _currentMapPath = "";
                    _currentFrameSpecName = "";
                    return;
                }

                string path = GetSelectedLibraryPath();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    QMC.Common.MessageDialog.Show(this, "선택된 Die Map 파일이 없습니다.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DieMap loaded = DieMapGenerator.Load(path);
                if (loaded == null)
                {
                    QMC.Common.MessageDialog.Show(this, "Die Map 파일을 읽을 수 없습니다.\r\n" + path, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ApplyMap(loaded, "Library Die Map: " + Path.GetFileName(path));
                _currentMapPath = path;
                _currentFrameSpecName = "";
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Die map load failed:\r\n" + ex.Message, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ApplyFrameSpecToControls(TapeFrameSpec spec)
        {
            if (spec == null)
                return;

            _tbFrameSpecName.Text = spec.Name ?? "";
            _currentFrameSpecName = spec.Name ?? "";
            _nGridX.Value = ClampDecimal(spec.DieMapX, _nGridX.Minimum, _nGridX.Maximum);
            _nGridY.Value = ClampDecimal(spec.DieMapY, _nGridY.Minimum, _nGridY.Maximum);
            _nPitchX.Value = ClampDecimal(spec.PitchX, _nPitchX.Minimum, _nPitchX.Maximum);
            _nPitchY.Value = ClampDecimal(spec.PitchY, _nPitchY.Minimum, _nPitchY.Maximum);
            _nDiameter.Value = ClampDecimal(spec.OuterDiameterMm, _nDiameter.Minimum, _nDiameter.Maximum);
        }

        private void SelectFrameSpecName(string specName)
        {
            if (string.IsNullOrWhiteSpace(specName))
                return;

            string expected = "[SPEC] " + specName.Trim();
            foreach (object item in _cbMapLibrary.Items)
            {
                if (item != null && string.Equals(item.ToString(), expected, StringComparison.OrdinalIgnoreCase))
                {
                    _cbMapLibrary.SelectedItem = item;
                    return;
                }
            }
        }

        private void SaveMapAsLibraryMap()
        {
            try
            {
                _project = _project ?? RecipeStore.LoadLastOrDefault();
                if (_project == null)
                    return;

                if (_map == null)
                    CreateMapFromRecipeSpec(false);
                if (_map == null)
                    return;

                string defaultName = BuildDefaultMapName();
                string name = PromptText("새 맵 이름", defaultName);
                if (string.IsNullOrWhiteSpace(name))
                    return;

                _currentMapPath = BuildMapPathByName(name);
                _currentFrameSpecName = "";
                if (File.Exists(_currentMapPath))
                {
                    QMC.Common.MessageDialog.Show(this, "이미 같은 이름의 Die Map이 있습니다.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ApplyPickupSequence(_map, _isOutputMap);
                _map.FrameObjId = Path.GetFileNameWithoutExtension(_currentMapPath);
                SaveMapToRecipe(_currentMapPath);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Save as die map failed:\r\n" + ex.Message, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void RenameSelectedLibraryMap()
        {
            try
            {
                string key = _cbMapLibrary.SelectedItem != null ? _cbMapLibrary.SelectedItem.ToString() : "";
                TapeFrameSpec selectedSpec;
                if (!string.IsNullOrWhiteSpace(key) && _mapLibraryFrameSpecs.TryGetValue(key, out selectedSpec))
                {
                    ShowSpecEditPageMessage();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(key) && _mapLibraryMemoryMaps.ContainsKey(key))
                {
                    QMC.Common.MessageDialog.Show(this,
                        "현재 메모리 Die Map은 이름 변경 대상이 아닙니다.\r\nFrame Spec 항목 또는 저장된 Die Map 파일을 선택하세요.",
                        "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string oldPath = GetSelectedLibraryPath();
                if (string.IsNullOrWhiteSpace(oldPath) || !File.Exists(oldPath))
                {
                    QMC.Common.MessageDialog.Show(this, "이름을 바꿀 Frame Spec 또는 저장된 Die Map 파일을 선택하세요.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string oldName = Path.GetFileNameWithoutExtension(oldPath);
                string newName = PromptText("Die Map 이름 변경", oldName);
                if (string.IsNullOrWhiteSpace(newName))
                    return;

                string newPath = BuildMapPathByName(newName);
                if (File.Exists(newPath))
                {
                    QMC.Common.MessageDialog.Show(this, "이미 같은 이름의 Die Map이 있습니다.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                File.Move(oldPath, newPath);
                MoveSidecarCsv(oldPath, newPath);
                if (_map != null)
                    _map.FrameObjId = Path.GetFileNameWithoutExtension(newPath);

                UpdateRecipeMapFileName(newPath);
                RecipeStore.Save(_project);
                _currentMapPath = newPath;
                RefreshMapLibraryList();
                SelectLibraryPath(newPath);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Die map rename failed:\r\n" + ex.Message, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void DeleteSelectedLibraryMap()
        {
            try
            {
                string key = _cbMapLibrary.SelectedItem != null ? _cbMapLibrary.SelectedItem.ToString() : "";
                TapeFrameSpec selectedSpec;
                if (!string.IsNullOrWhiteSpace(key) && _mapLibraryFrameSpecs.TryGetValue(key, out selectedSpec))
                {
                    ShowSpecEditPageMessage();
                    return;
                }

                string path = GetSelectedLibraryPath();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    QMC.Common.MessageDialog.Show(this, "삭제할 Die Map을 선택하세요.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DialogResult result = QMC.Common.MessageDialog.Show(this,
                    "선택한 Die Map을 삭제하시겠습니까?\r\n" + Path.GetFileName(path),
                    "Die Map Create", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                    return;

                File.Delete(path);
                string csv = Path.ChangeExtension(path, ".csv");
                if (File.Exists(csv))
                    File.Delete(csv);

                string recipePath = _project != null ? ResolveRecipeMapPath(_project, _isOutputMap) : "";
                if (!string.IsNullOrWhiteSpace(recipePath) &&
                    string.Equals(Path.GetFullPath(recipePath), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
                {
                    UpdateRecipeMapFileName("");
                    RecipeStore.Save(_project);
                }

                if (!string.IsNullOrWhiteSpace(_currentMapPath) &&
                    string.Equals(Path.GetFullPath(_currentMapPath), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
                {
                    _currentMapPath = "";
                }

                RefreshMapLibraryList();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Die map delete failed:\r\n" + ex.Message, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ImportMapFile()
        {
            try
            {
                using (var dlg = new OpenFileDialog
                {
                    Title = "Import DieMap",
                    Filter = "DieMap files|*.json;*.csv|JSON|*.json|CSV|*.csv|All files|*.*"
                })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return;

                    DieMap loaded = DieMapGenerator.Load(dlg.FileName);
                    if (loaded == null)
                    {
                        QMC.Common.MessageDialog.Show(this, "Die Map 파일을 읽을 수 없습니다.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    ApplyMap(loaded, "Imported Die Map: " + Path.GetFileName(dlg.FileName));
                    _currentMapPath = "";
                    _currentFrameSpecName = "";
                    _cbMapLibrary.SelectedIndex = -1;
                    RefreshMapLibraryList();
                }
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Die map import failed:\r\n" + ex.Message, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ExportMapCsv()
        {
            try
            {
                if (_map == null)
                {
                    QMC.Common.MessageDialog.Show(this, "내보낼 Die Map이 없습니다.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var dlg = new SaveFileDialog
                {
                    Title = "Export DieMap CSV",
                    Filter = "CSV|*.csv|JSON|*.json",
                    FileName = (_map.FrameObjId ?? "DieMap") + ".csv"
                })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return;

                    if (string.Equals(Path.GetExtension(dlg.FileName), ".json", StringComparison.OrdinalIgnoreCase))
                        DieMapGenerator.SaveJson(_map, dlg.FileName);
                    else
                        DieMapGenerator.SaveCsv(_map, dlg.FileName);

                    QMC.Common.MessageDialog.Show(this, "Die Map export 완료.\r\n" + dlg.FileName, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Die map export failed:\r\n" + ex.Message, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void SaveMapToRecipe(string explicitPath = "")
        {
            try
            {
                _project = _project ?? RecipeStore.LoadLastOrDefault();
                if (_project == null)
                    return;

                if (_map == null)
                {
                    QMC.Common.MessageDialog.Show(this, "저장할 Die Map이 없습니다.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ApplyPickupSequence(_map, _isOutputMap);
                string path = !string.IsNullOrWhiteSpace(explicitPath)
                    ? explicitPath
                    : ResolveSaveMapPath();
                _map.FrameObjId = Path.GetFileNameWithoutExtension(path);
                DieMapGenerator.SaveJson(_map, path);
                DieMapGenerator.SaveCsv(_map, Path.ChangeExtension(path, ".csv"));

                UpdateRecipeMapFileName(path);
                RecipeStore.Save(_project);
                RecipeStore.SaveLastProjectName(_project.FileName);
                _currentMapPath = path;
                RefreshMapLibraryList();
                SelectLibraryPath(path);

                if (!_isOutputMap)
                {
                    LotStorage.ActiveInputDieMap = _map;
                    var host = FindForm() as Form1;
                    if (host != null && host.Controller != null)
                        host.Controller.ApplyInputDieMap(_map, "MapCreatePage.Save");
                }

                QMC.Common.MessageDialog.Show(this,
                    "Die Map 저장 및 Recipe 연결 완료.\r\n" + path,
                    "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Die map save failed:\r\n" + ex.Message, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ApplySelectedFrameSpecToControlsIfNeeded()
        {
            try
            {
                string key = _cbMapLibrary.SelectedItem != null ? _cbMapLibrary.SelectedItem.ToString() : "";
                TapeFrameSpec frameSpec;
                if (!string.IsNullOrWhiteSpace(key) && _mapLibraryFrameSpecs.TryGetValue(key, out frameSpec))
                {
                    if (!string.Equals(_currentFrameSpecName, frameSpec.Name ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        ApplyFrameSpecToControls(frameSpec);
                        _currentFrameSpecName = frameSpec.Name ?? "";
                    }
                    if (string.IsNullOrWhiteSpace(_currentMapPath))
                        _currentMapPath = ResolveCurrentRecipeMapPathOrEmpty();
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private string ResolveCurrentRecipeMapPathOrEmpty()
        {
            try
            {
                RecipeProject project = _project ?? RecipeStore.LoadLastOrDefault();
                if (project == null)
                    return "";

                string path = ResolveRecipeMapPath(project, _isOutputMap);
                return string.IsNullOrWhiteSpace(path) ? "" : path;
            }
            catch
            {
                return "";
            }
            finally
            {
            }
        }

        private void ShowSpecEditPageMessage()
        {
            QMC.Common.MessageDialog.Show(this,
                "Wafer SPEC은 이 화면에서 수정하지 않습니다.\r\nSPEC 수정은 Wafer Spec 페이지에서 진행하세요.",
                "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // 경로/파일명 규칙은 RecipeMapPaths(공용)로 위임 — 입력/GOOD/NG 일관 처리, 중복 제거.
        private string ResolveRecipeMapPath(RecipeProject project, bool output)
        {
            return RecipeMapPaths.ResolveConfigured(project, CurrentMapKind);
        }

        private string BuildRecipeMapPath(RecipeProject project, bool output)
        {
            return RecipeMapPaths.BuildDefaultPath(project, CurrentMapKind);
        }

        private string ResolveSaveMapPath()
        {
            string recipePath = _project != null ? ResolveRecipeMapPath(_project, _isOutputMap) : "";
            if (!string.IsNullOrWhiteSpace(recipePath))
                return recipePath;

            return BuildRecipeMapPath(_project, _isOutputMap);
        }

        private static string GetDieMapDirectory()
        {
            return RecipeMapPaths.GetDieMapDirectory();
        }

        private string BuildMapPathByName(string name)
        {
            return RecipeMapPaths.BuildPathByName(name, CurrentMapKind);
        }

        private string BuildDefaultMapName()
        {
            return RecipeMapPaths.BuildDefaultName(_project, CurrentMapKind);
        }

        private void UpdateRecipeMapFileName(string path)
        {
            if (_project == null)
                return;

            string relativePath = RecipeMapPaths.MakeConfigRelativePath(path);
            RecipeMapPaths.SetConfiguredFileName(_project, CurrentMapKind, relativePath);
        }

        private static void MoveSidecarCsv(string oldJsonPath, string newJsonPath)
        {
            string oldCsv = Path.ChangeExtension(oldJsonPath, ".csv");
            string newCsv = Path.ChangeExtension(newJsonPath, ".csv");
            if (File.Exists(oldCsv) && !File.Exists(newCsv))
                File.Move(oldCsv, newCsv);
        }

        private static string PromptText(string title, string defaultValue)
        {
            using (var form = new Form())
            using (var textBox = new TextBox())
            using (var ok = new Button())
            using (var cancel = new Button())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ClientSize = new Size(420, 95);

                textBox.Text = defaultValue ?? "";
                textBox.Font = new Font("Consolas", 10F);
                textBox.Location = new Point(12, 12);
                textBox.Size = new Size(396, 24);

                ok.Text = "OK";
                ok.DialogResult = DialogResult.OK;
                ok.Location = new Point(246, 52);
                ok.Size = new Size(78, 28);

                cancel.Text = "CANCEL";
                cancel.DialogResult = DialogResult.Cancel;
                cancel.Location = new Point(330, 52);
                cancel.Size = new Size(78, 28);

                form.Controls.Add(textBox);
                form.Controls.Add(ok);
                form.Controls.Add(cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;

                return form.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : "";
            }
        }

        private string BuildRecipeMapId(RecipeProject project, bool output)
        {
            return RecipeMapPaths.BuildMapId(project, CurrentMapKind);
        }

        private static string BuildDieId(RecipeProject project, int row, int col)
        {
            string recipeName = RecipeMapPaths.SanitizeFileName(project != null ? project.FileName : "Recipe");
            return recipeName + "-D" + row.ToString("000") + "-" + col.ToString("000");
        }

        private static decimal ClampDecimal(int value, decimal min, decimal max)
        {
            decimal decimalValue = value;
            if (decimalValue < min) return min;
            if (decimalValue > max) return max;
            return decimalValue;
        }

        private static decimal ClampDecimal(double value, decimal min, decimal max)
        {
            decimal decimalValue = (decimal)value;
            if (decimalValue < min) return min;
            if (decimalValue > max) return max;
            return decimalValue;
        }
    }
}
