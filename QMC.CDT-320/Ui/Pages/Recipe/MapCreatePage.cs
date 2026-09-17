using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
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
        private DateTime _currentMapWriteUtc;
        private string _currentFrameSpecName;
        private string _currentLibraryKey;
        private const string EdgeSkipGridText = "격자 수 (GRID)";
        private const string EdgeSkipMmText = "거리 (MM)";
        private const string EdgeSkipExternalMapText = "외부 맵 (EXTERNAL MAP)";
        private readonly Dictionary<string, string> _mapLibraryPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DieMap> _mapLibraryMemoryMaps = new Dictionary<string, DieMap>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TapeFrameSpec> _mapLibraryFrameSpecs = new Dictionary<string, TapeFrameSpec>(StringComparer.OrdinalIgnoreCase);
        private bool _suppressMapSpecEvents;
        private bool _mapSpecDirty;
        private bool _maskDirty;
        private bool _changingBinSide;

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

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible || IsDesignerMode())
                return;

            ReloadRecipeMapIfChanged();
        }

        private void InitializeMapEditor()
        {
            _mapView.Caption = "등록 다이 맵 · 적용 기준 미리보기";
            ConfigureRegisteredMapPreview();
            _mapMenu = BuildMapContextMenu();
            _mapView.ContextMenuStrip = _mapMenu;
            _mapView.CellClicked += OnMapCellClicked;
            HookMapSpecControlEvents();
            EnableBinAuthoringControls();
            UpdateEdgeSkipModeUi();

            lblHeader.Text = _isOutputMap ? "출력 다이 맵 생성" : "입력 다이 맵 생성";
            lblSettingTitle.Text = "맵 사양 · 확인용";
            lblActionTitle.Text = "확인 및 적용";
            _btnMapLoad.Text = "사양 불러오기";
            btnSave.Text = "맵 확인 및 적용";
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
            rbStandard.Checked = true;
            ConfigureRecipeMapGeneratorUi();
            ConfigureBinSideToggle();
        }

        private void _btnMapLoad_Click(object sender, EventArgs e) => LoadSelectedLibraryMap();

        private void btnSave_Click(object sender, EventArgs e) => SaveMapToRecipe();

        private void ConfigureRecipeMapGeneratorUi()
        {
            try
            {
                _btnMapNew.Visible = false;
                _btnMapRename.Visible = false;
                _btnMapDelete.Visible = false;
                _cbMapLibrary.Enabled = false;
                _btnMapLoad.Enabled = false;
                _btnMapLoad.Text = "웨이퍼 사양에서 설정";
                _recipeLocationToolTip.SetToolTip(_cbMapLibrary, "레시피 → 웨이퍼 사양 → 웨이퍼 맵 불러오기에서 설정합니다.");
                _recipeLocationToolTip.SetToolTip(_btnMapLoad, "레시피 → 웨이퍼 사양 → 웨이퍼 맵 불러오기에서 설정합니다.");

                chkCircularMap.Visible = false;
                rbStandard.Visible = false;
                rbStartIndex.Visible = false;
                rbReference1.Visible = false;
                rbReference2.Visible = false;
                rbManualSelectPick.Visible = false;
                rbAlignCheckIndex.Visible = false;
                rbDragSelectPick.Visible = false;

                for (int i = 1; i <= 8 && i < modeSection.RowStyles.Count; i++)
                {
                    modeSection.RowStyles[i].SizeType = SizeType.Absolute;
                    modeSection.RowStyles[i].Height = 0F;
                }
                if (modeSection.RowStyles.Count > 9)
                {
                    modeSection.RowStyles[9].SizeType = SizeType.Percent;
                    modeSection.RowStyles[9].Height = 100F;
                }

                btnCreate.Visible = false;
                btnFirstDieMoveComplete.Visible = false;
                btnAutoMatch.Visible = false;
                btnThetaMatchMove.Visible = false;
                btnXyMatchMove.Visible = false;

                actionSection.SetCellPosition(btnSave, new TableLayoutPanelCellPosition(0, 2));
                actionSection.SetColumnSpan(btnSave, 2);
                btnSave.Dock = DockStyle.Fill;
                actionSection.RowStyles[1].SizeType = SizeType.Absolute;
                actionSection.RowStyles[1].Height = 150F;
                actionSection.RowStyles[2].SizeType = SizeType.Percent;
                actionSection.RowStyles[2].Height = 100F;
                actionSection.RowStyles[3].SizeType = SizeType.Absolute;
                actionSection.RowStyles[3].Height = 0F;
                if (actionSection.RowStyles.Count > 4)
                {
                    actionSection.RowStyles[4].SizeType = SizeType.Absolute;
                    actionSection.RowStyles[4].Height = 0F;
                }

                rightLayout.RowStyles[1].SizeType = SizeType.Absolute;
                rightLayout.RowStyles[1].Height = _isOutputMap ? 70F : 0F;
                rightLayout.RowStyles[0].SizeType = SizeType.Absolute;
                rightLayout.RowStyles[0].Height = 340F;
                rightLayout.RowStyles[2].SizeType = SizeType.Percent;
                rightLayout.RowStyles[2].Height = 100F;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "RECIPE", "MapCreatePage",
                    "Recipe map generator UI 구성 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void HookMapSpecControlEvents()
        {
            _nPitchX.ValueChanged += OnMapSpecControlChanged;
            _nPitchY.ValueChanged += OnMapSpecControlChanged;
            _nDieSizeX.ValueChanged += OnMapSpecControlChanged;
            _nDieSizeY.ValueChanged += OnMapSpecControlChanged;
            _nDiameter.ValueChanged += OnMapSpecControlChanged;
            _nSideEdgeSkip.ValueChanged += OnMapSpecControlChanged;
            _nTopBottomEdgeSkip.ValueChanged += OnMapSpecControlChanged;
            _cbEdgeSkipMode.SelectedIndexChanged += OnMapSpecControlChanged;
        }

        private void OnMapSpecControlChanged(object sender, EventArgs e)
        {
            if (_suppressMapSpecEvents)
                return;

            UpdateEdgeSkipModeUi();
            RecalculateGridPreviewFromControls();
            _mapSpecDirty = true;
        }

        private void RecalculateGridPreviewFromControls()
        {
            if (_nDieSizeX == null)
                return;

            try
            {
                bool externalMap = string.Equals(GetSelectedEdgeSkipModeName(), "ExternalMap", StringComparison.OrdinalIgnoreCase);
                TapeFrameSubset frame = ResolveCurrentFrameFromRecipe();
                int gridX = externalMap && frame != null
                    ? Math.Max(1, frame.DieMapX)
                    : ResolvePitchBasedGridCount(
                        (double)_nDiameter.Value,
                        (double)_nDieSizeX.Value + Math.Max(0.0, (double)_nPitchX.Value),
                        (double)_nDieSizeX.Value);
                int gridY = externalMap && frame != null
                    ? Math.Max(1, frame.DieMapY)
                    : ResolvePitchBasedGridCount(
                        (double)_nDiameter.Value,
                        (double)_nDieSizeY.Value + Math.Max(0.0, (double)_nPitchY.Value),
                        (double)_nDieSizeY.Value);
                _suppressMapSpecEvents = true;
                _nGridX.Value = ClampDecimal(gridX, _nGridX.Minimum, _nGridX.Maximum);
                _nGridY.Value = ClampDecimal(gridY, _nGridY.Minimum, _nGridY.Maximum);
            }
            catch
            {
            }
            finally
            {
                _suppressMapSpecEvents = false;
            }
        }

        private void UpdateEdgeSkipModeUi()
        {
            if (_cbEdgeSkipMode == null || _nSideEdgeSkip == null || _nTopBottomEdgeSkip == null)
                return;

            string modeName = GetSelectedEdgeSkipModeName();
            bool mmMode = string.Equals(modeName, WaferEdgeSkipMode.Millimeter.ToString(), StringComparison.OrdinalIgnoreCase);
            bool externalMap = string.Equals(modeName, "ExternalMap", StringComparison.OrdinalIgnoreCase);
            decimal max = mmMode ? Math.Max(0.001M, _nDiameter.Value / 2M) : 500M;
            _nSideEdgeSkip.Maximum = max;
            _nTopBottomEdgeSkip.Maximum = max;
            _nSideEdgeSkip.DecimalPlaces = mmMode ? 3 : 0;
            _nTopBottomEdgeSkip.DecimalPlaces = mmMode ? 3 : 0;
            _nSideEdgeSkip.Increment = mmMode ? 0.1M : 1M;
            _nTopBottomEdgeSkip.Increment = mmMode ? 0.1M : 1M;
            lblAxisYKey.Text = externalMap
                ? "가장자리 제외 (외부 맵)"
                : (mmMode ? "가장자리 제외 좌우/상하 (mm)" : "가장자리 제외 좌우/상하 (칸)");
        }

        private WaferEdgeSkipMode GetSelectedEdgeSkipMode()
        {
            string text = _cbEdgeSkipMode != null && _cbEdgeSkipMode.SelectedItem != null
                ? _cbEdgeSkipMode.SelectedItem.ToString()
                : "";
            return text.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0
                ? WaferEdgeSkipMode.Millimeter
                : WaferEdgeSkipMode.Grid;
        }

        private void SetSelectedEdgeSkipMode(string mode)
        {
            if (_cbEdgeSkipMode == null)
                return;

            bool externalMap = !string.IsNullOrWhiteSpace(mode) &&
                               mode.IndexOf("EXTERNAL", StringComparison.OrdinalIgnoreCase) >= 0;
            bool mm = IsMillimeterEdgeSkipMode(mode);
            _cbEdgeSkipMode.SelectedItem = externalMap ? EdgeSkipExternalMapText : (mm ? EdgeSkipMmText : EdgeSkipGridText);
            if (_cbEdgeSkipMode.SelectedIndex < 0)
                _cbEdgeSkipMode.SelectedIndex = externalMap ? 2 : (mm ? 1 : 0);
        }

        private string GetSelectedEdgeSkipModeName()
        {
            string text = _cbEdgeSkipMode != null && _cbEdgeSkipMode.SelectedItem != null
                ? _cbEdgeSkipMode.SelectedItem.ToString()
                : "";
            if (text.IndexOf("EXTERNAL", StringComparison.OrdinalIgnoreCase) >= 0)
                return "ExternalMap";
            return GetSelectedEdgeSkipMode().ToString();
        }

        /// <summary>GOOD/NG Mask 편집은 유지하되 공용 Recipe 형상값은 Project 페이지 소유로 고정한다.</summary>
        private void ConfigureBinSideToggle()
        {
            bool bin = _isOutputMap;
            binSidePanel.Visible = bin;
            rbBinGood.Visible = bin;
            rbBinNg.Visible = bin;
            if (!bin)
                return;

            rbBinGood.Text = "양품 맵";
            rbBinNg.Text = "불량 맵";
            rbBinGood.Checked = _mode != MapEditorMode.OutputNg;
            rbBinNg.Checked = _mode == MapEditorMode.OutputNg;
            ConfigureBinSideButton(rbBinGood);
            ConfigureBinSideButton(rbBinNg);
            UpdateBinSideButtonStyle();
            rbBinGood.CheckedChanged += OnBinSideChanged;
            rbBinNg.CheckedChanged += OnBinSideChanged;
        }

        private static void ConfigureBinSideButton(RadioButton button)
        {
            if (button == null)
                return;

            button.Appearance = Appearance.Button;
            button.FlatStyle = FlatStyle.Flat;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Cursor = Cursors.Hand;
            button.Margin = new Padding(4, 2, 4, 2);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xF0, 0xD0, 0xA0);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(0xD9, 0x77, 0x06);
        }

        private void UpdateBinSideButtonStyle()
        {
            try
            {
                ApplyBinSideButtonStyle(rbBinGood, rbBinGood != null && rbBinGood.Checked);
                ApplyBinSideButtonStyle(rbBinNg, rbBinNg != null && rbBinNg.Checked);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void ApplyBinSideButtonStyle(RadioButton button, bool selected)
        {
            if (button == null)
                return;

            button.BackColor = selected
                ? Color.FromArgb(0xD9, 0x77, 0x06)
                : Color.FromArgb(0xE6, 0xE6, 0xE6);
            button.ForeColor = selected ? Color.White : Color.Black;
            button.FlatAppearance.BorderColor = selected
                ? Color.FromArgb(0x88, 0x45, 0x00)
                : Color.FromArgb(0x88, 0x88, 0x88);
        }

        private void OnBinSideChanged(object sender, EventArgs e)
        {
            if (_changingBinSide)
                return;

            try
            {
                RadioButton rb = sender as RadioButton;
                if (rb == null || !rb.Checked)
                {
                    UpdateBinSideButtonStyle();
                    return;
                }

                MapEditorMode next = rbBinNg.Checked ? MapEditorMode.OutputNg : MapEditorMode.OutputGood;
                if (next == _mode)
                {
                    UpdateBinSideButtonStyle();
                    return;
                }

                if (!ConfirmPendingMaskBeforeRoleChange())
                {
                    _changingBinSide = true;
                    try
                    {
                        rbBinGood.Checked = _mode == MapEditorMode.OutputGood;
                        rbBinNg.Checked = _mode == MapEditorMode.OutputNg;
                    }
                    finally
                    {
                        _changingBinSide = false;
                    }
                    UpdateBinSideButtonStyle();
                    return;
                }

                _mode = next;
                chkCircularMap.Text = "BIN CIRCLE DIE MAP";
                _currentMapPath = "";
                _currentFrameSpecName = "";
                if (!LoadSavedRecipeMap(false))
                {
                    _map = null;
                    _mapView.Map = null;
                    _mapView.Caption = "Recipe → 웨이퍼 사양에서 Base WaferMap을 연결하세요.";
                    RefreshSettingLabels();
                }
                RefreshMapLibraryList();
                UpdateBinSideButtonStyle();
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
            SetNumericReadOnly(_nGridX);
            SetNumericReadOnly(_nGridY);
            SetNumericReadOnly(_nPitchX);
            SetNumericReadOnly(_nPitchY);
            SetNumericReadOnly(_nDieSizeX);
            SetNumericReadOnly(_nDieSizeY);
            SetNumericReadOnly(_nDiameter);
            SetNumericReadOnly(_nSideEdgeSkip);
            SetNumericReadOnly(_nTopBottomEdgeSkip);
            if (_cbEdgeSkipMode != null)
                _cbEdgeSkipMode.Enabled = false;
        }

        private static void SetNumericReadOnly(NumericUpDown control)
        {
            if (control == null)
                return;
            control.Enabled = false;
            control.ReadOnly = true;
            control.TabStop = false;
        }

        private void LoadEdgeSkipFromRecipe()
        {
            try
            {
                TapeFrameSubset frame = ResolveCurrentFrameFromRecipe();
                if (frame == null)
                    return;

                _suppressMapSpecEvents = true;
                _tbFrameSpecName.Text = frame.FrameSpecName ?? "";
                _currentFrameSpecName = MaterialSpecs.FindFrame(frame.FrameSpecName) != null ? frame.FrameSpecName : "";
                _nPitchX.Value = ClampDecimal(frame.PitchX, _nPitchX.Minimum, _nPitchX.Maximum);
                _nPitchY.Value = ClampDecimal(frame.PitchY, _nPitchY.Minimum, _nPitchY.Maximum);
                _nDieSizeX.Value = ClampDecimal(ResolveRecipeDieSizeX(), _nDieSizeX.Minimum, _nDieSizeX.Maximum);
                _nDieSizeY.Value = ClampDecimal(ResolveRecipeDieSizeY(), _nDieSizeY.Minimum, _nDieSizeY.Maximum);
                _nDiameter.Value = ClampDecimal(frame.OuterDiameterMm, _nDiameter.Minimum, _nDiameter.Maximum);
                SetSelectedEdgeSkipMode(frame.EdgeSkipMode);
                UpdateEdgeSkipModeUi();
                if (GetSelectedEdgeSkipMode() == WaferEdgeSkipMode.Millimeter)
                {
                    _nSideEdgeSkip.Value = ClampDecimal(frame.SideEdgeSkipMm, _nSideEdgeSkip.Minimum, _nSideEdgeSkip.Maximum);
                    _nTopBottomEdgeSkip.Value = ClampDecimal(frame.TopBottomEdgeSkipMm, _nTopBottomEdgeSkip.Minimum, _nTopBottomEdgeSkip.Maximum);
                }
                else
                {
                    _nSideEdgeSkip.Value = ClampDecimal(frame.SideEdgeSkip, _nSideEdgeSkip.Minimum, _nSideEdgeSkip.Maximum);
                    _nTopBottomEdgeSkip.Value = ClampDecimal(frame.TopBottomEdgeSkip, _nTopBottomEdgeSkip.Minimum, _nTopBottomEdgeSkip.Maximum);
                }
                RecalculateGridPreviewFromControls();
                _mapSpecDirty = false;
            }
            catch
            {
            }
            finally
            {
                _suppressMapSpecEvents = false;
            }
        }

        private TapeFrameSubset ResolveCurrentFrameFromRecipe()
        {
            if (_project == null)
                return null;

            TapeFrameSubset roleFrame = _isOutputMap ? _project.OutputFrame : _project.InputFrame;
            return roleFrame ?? _project.Frame;
        }

        private double ResolveRecipeDieSizeX()
        {
            if (_project != null && _project.Die != null && _project.Die.WidthMm > 0.0)
                return _project.Die.WidthMm;
            return 1.0;
        }

        private double ResolveRecipeDieSizeY()
        {
            if (_project != null && _project.Die != null && _project.Die.HeightMm > 0.0)
                return _project.Die.HeightMm;
            return 1.0;
        }

        private static bool IsMillimeterEdgeSkipMode(string mode)
        {
            return !string.IsNullOrWhiteSpace(mode) &&
                   (mode.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    mode.IndexOf("MILLI", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private ContextMenuStrip BuildMapContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Opening += OnMapContextMenuOpening;
            menu.Items.Add("TARGET ALL", null, (s, e) => SetAllTargets(true));
            menu.Items.Add("SKIP ALL", null, (s, e) => SetAllTargets(false));
            menu.Items.Add("INVERT TARGET", null, (s, e) => InvertMapTargets());
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

                _mapView.ContextMenuStrip = _mapMenu;
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
                e.Cancel = _map == null;
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
                _project = LoadActiveRecipeProject();
                if (_project == null)
                {
                    QMC.Common.MessageDialog.Show(this, "로드된 Recipe가 없습니다.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                LoadEdgeSkipFromRecipe();
                RefreshMapLibraryList();
                SelectFrameSpecName(_currentFrameSpecName);

                if (LoadSavedRecipeMap(false))
                    return;

                _map = null;
                _mapView.Map = null;
                _mapView.Caption = "Base Wafer Map을 Recipe → 웨이퍼 사양에서 설정하세요.";
                RefreshSettingLabels();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Recipe die map load failed:\r\n" + ex.Message, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ReloadRecipeMapIfChanged()
        {
            try
            {
                RecipeProject latest = LoadActiveRecipeProject();
                if (latest == null)
                    return;

                _project = latest;
                string path;
                string reason;
                DieMap latestMap = LoadExactConfiguredRoleMap(out path, out reason);
                if (latestMap == null)
                {
                    _map = null;
                    _currentMapPath = "";
                    _currentMapWriteUtc = DateTime.MinValue;
                    _mapView.Map = null;
                    _mapView.Caption = "Recipe Die Map not configured";
                    RefreshSettingLabels();
                    return;
                }

                DateTime writeUtc = !string.IsNullOrWhiteSpace(path) && File.Exists(path)
                    ? File.GetLastWriteTimeUtc(path)
                    : DateTime.MinValue;
                bool pathChanged = string.IsNullOrWhiteSpace(_currentMapPath) ||
                                   !string.Equals(Path.GetFullPath(_currentMapPath), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase);
                if (!pathChanged && writeUtc == _currentMapWriteUtc)
                {
                    // 역할 파일을 다시 저장하지 않는 회전/원점/순서 변경도 반영한다.
                    // 현재 편집 중인 Target/Skip은 유지하고 표시용 사본만 다시 준비한다.
                    RefreshSettingLabels();
                    return;
                }

                if (_maskDirty)
                {
                    DialogResult answer = QMC.Common.MessageDialog.Show(this,
                        "다른 Recipe 페이지에서 역할 맵이 변경됐지만 현재 Target/Skip 편집값이 아직 적용되지 않았습니다.\r\n\r\n" +
                        "Yes: 현재 편집값 맵 확인 및 적용 후 재로드 보류\r\nNo: 편집 폐기 후 최신 맵 로드\r\nCancel: 현재 화면 유지",
                        "Die Map 외부 변경 확인",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning);
                    if (answer == DialogResult.Cancel)
                        return;
                    if (answer == DialogResult.Yes)
                    {
                        SaveMapToRecipe();
                        return;
                    }
                    _maskDirty = false;
                }

                // Project 페이지에서 연결한 최신 Recipe map은 Map Create 재진입 시 다시 읽는다.
                LoadEdgeSkipFromRecipe();
                RefreshMapLibraryList();
                ApplyMap(latestMap, "Recipe Die Map: " + Path.GetFileName(path));
                _currentMapPath = path;
                _currentMapWriteUtc = writeUtc;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void CreateMapFromRecipeSpec(bool confirm)
        {
            try
            {
                _project = LoadActiveRecipeProject();
                if (_project == null)
                    return;

                ApplySelectedFrameSpecToControlsIfNeeded();

                TapeFrameSubset activeFrame = ResolveCurrentFrameFromRecipe();
                if (RecipeDieMapResolver.IsExternalFrame(activeFrame))
                {
                    if (confirm)
                    {
                        QMC.Common.MessageDialog.Show(this,
                            "External Map은 이 화면에서 새로 생성하지 않습니다.\r\n레시피 → 웨이퍼 사양 → 웨이퍼 맵 불러오기에서 불러오세요.",
                            "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }

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
            WaferEdgeSkipMode edgeSkipMode = IsMillimeterEdgeSkipMode(frame.EdgeSkipMode)
                ? WaferEdgeSkipMode.Millimeter
                : WaferEdgeSkipMode.Grid;
            double sideEdgeSkip = edgeSkipMode == WaferEdgeSkipMode.Millimeter ? frame.SideEdgeSkipMm : frame.SideEdgeSkip;
            double topBottomEdgeSkip = edgeSkipMode == WaferEdgeSkipMode.Millimeter ? frame.TopBottomEdgeSkipMm : frame.TopBottomEdgeSkip;

            // Recipe Pitch는 gap이므로 DieMap 장비 중심 간격은 Die Size + Gap으로 만든다.
            double centerStepX = frame.DieSizeX + Math.Max(0.0, frame.PitchX);
            double centerStepY = frame.DieSizeY + Math.Max(0.0, frame.PitchY);
            DieMap map = DieMapGenerator.GenerateCircularWafer(
                frame.OuterDiameterMm,
                centerStepX,
                centerStepY,
                frame.DieSizeX,
                frame.DieSizeY,
                edgeSkipMode,
                sideEdgeSkip,
                topBottomEdgeSkip,
                BuildRecipeMapId(project, outputMap));

            if (map.Entries != null)
            {
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry != null)
                        entry.DieUid = BuildDieId(project, entry.DieMapY, entry.DieMapX);
                }
            }

            return ApplyPickupSequence(map, outputMap);
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
                RecipeProject latest = LoadActiveRecipeProject();

                if (latest != null)
                    _project = latest;

                return latest;
            }
            catch
            {
                return _project;
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
            _maskDirty = false;
            ApplyMapToControls(_map);
            _mapView.Caption = caption ?? "Recipe Die Map";
            _mapView.SetMap(null, true);
            RefreshSettingLabels();
        }

        private TapeFrameSubset BuildFrameFromControls()
        {
            double diameter = (double)_nDiameter.Value > 0.0 ? (double)_nDiameter.Value : 0.0;
            double pitchGapX = _nPitchX != null ? Math.Max(0.0, (double)_nPitchX.Value) : 0.0;
            double pitchGapY = _nPitchY != null ? Math.Max(0.0, (double)_nPitchY.Value) : 0.0;
            double recipeDieSizeX = ResolveRecipeDieSizeX();
            double recipeDieSizeY = ResolveRecipeDieSizeY();
            double dieSizeX = _nDieSizeX != null && (double)_nDieSizeX.Value > 0.0
                ? (double)_nDieSizeX.Value
                : (recipeDieSizeX > 0.0 ? recipeDieSizeX : 1.0);
            double dieSizeY = _nDieSizeY != null && (double)_nDieSizeY.Value > 0.0
                ? (double)_nDieSizeY.Value
                : (recipeDieSizeY > 0.0 ? recipeDieSizeY : 1.0);
            double centerStepX = dieSizeX + pitchGapX;
            double centerStepY = dieSizeY + pitchGapY;
            WaferEdgeSkipMode edgeSkipMode = GetSelectedEdgeSkipMode();
            string edgeSkipModeName = GetSelectedEdgeSkipModeName();
            bool externalMapMode = string.Equals(edgeSkipModeName, "ExternalMap", StringComparison.OrdinalIgnoreCase);
            double edgeSideValue = _nSideEdgeSkip != null ? (double)_nSideEdgeSkip.Value : 0.0;
            double edgeTopBottomValue = _nTopBottomEdgeSkip != null ? (double)_nTopBottomEdgeSkip.Value : 0.0;
            int gridX = externalMapMode
                ? Math.Max(1, (int)_nGridX.Value)
                : ResolvePitchBasedGridCount(diameter, centerStepX, dieSizeX);
            int gridY = externalMapMode
                ? Math.Max(1, (int)_nGridY.Value)
                : ResolvePitchBasedGridCount(diameter, centerStepY, dieSizeY);

            return new TapeFrameSubset
            {
                FrameSpecName = string.IsNullOrWhiteSpace(_tbFrameSpecName.Text) ? "RecipeFrame" : _tbFrameSpecName.Text.Trim(),
                DieMapX = gridX,
                DieMapY = gridY,
                PitchX = pitchGapX,
                PitchY = pitchGapY,
                DieSizeX = dieSizeX,
                DieSizeY = dieSizeY,
                OuterDiameterMm = diameter,
                EdgeSkipMode = edgeSkipModeName,
                SideEdgeSkip = edgeSkipMode == WaferEdgeSkipMode.Grid && !externalMapMode ? Math.Max(0, (int)Math.Floor(edgeSideValue)) : 0,
                TopBottomEdgeSkip = edgeSkipMode == WaferEdgeSkipMode.Grid && !externalMapMode ? Math.Max(0, (int)Math.Floor(edgeTopBottomValue)) : 0,
                SideEdgeSkipMm = edgeSkipMode == WaferEdgeSkipMode.Millimeter ? Math.Max(0.0, edgeSideValue) : 0.0,
                TopBottomEdgeSkipMm = edgeSkipMode == WaferEdgeSkipMode.Millimeter ? Math.Max(0.0, edgeTopBottomValue) : 0.0
            };
        }

        private static int ResolvePitchBasedGridCount(double outerDiameterMm, double pitchMm, double dieSizeMm)
        {
            if (outerDiameterMm <= 0.0 || pitchMm <= 0.0 || dieSizeMm <= 0.0)
                return 1;

            return DieMapGenerator.CalculateWaferGridCount(outerDiameterMm, pitchMm, dieSizeMm);
        }

        private void ApplyMapToControls(DieMap map)
        {
            if (map == null)
                return;

            try
            {
                _suppressMapSpecEvents = true;
                TapeFrameSubset frame = ResolveCurrentFrameFromRecipe();
                _nGridX.Value = ClampDecimal(map.DieMapX, _nGridX.Minimum, _nGridX.Maximum);
                _nGridY.Value = ClampDecimal(map.DieMapY, _nGridY.Minimum, _nGridY.Maximum);
                // Recipe Frame의 Pitch는 Die 사이 gap(0 허용), DieMap.Pitch는 장비 중심 간격이다.
                // 오른쪽 설정란에는 사용자가 저장한 gap을 표시하고 아래 정보란에 두 값을 모두 표시한다.
                double pitchGapX = frame != null ? frame.PitchX : Math.Max(0.0, map.PitchX - map.DieSizeX);
                double pitchGapY = frame != null ? frame.PitchY : Math.Max(0.0, map.PitchY - map.DieSizeY);
                if (frame != null && map.Generation != null &&
                    (map.Generation.RotationDegrees == 90 || map.Generation.RotationDegrees == 270))
                {
                    pitchGapX = frame.PitchY;
                    pitchGapY = frame.PitchX;
                }
                _nPitchX.Value = ClampDecimal(pitchGapX, _nPitchX.Minimum, _nPitchX.Maximum);
                _nPitchY.Value = ClampDecimal(pitchGapY, _nPitchY.Minimum, _nPitchY.Maximum);
                _nDieSizeX.Value = ClampDecimal(map.DieSizeX > 0.0 ? map.DieSizeX : ResolveRecipeDieSizeX(), _nDieSizeX.Minimum, _nDieSizeX.Maximum);
                _nDieSizeY.Value = ClampDecimal(map.DieSizeY > 0.0 ? map.DieSizeY : ResolveRecipeDieSizeY(), _nDieSizeY.Minimum, _nDieSizeY.Maximum);
                double diameter = map.OuterDiameterMm > 0.0
                    ? map.OuterDiameterMm
                    : (frame != null ? frame.OuterDiameterMm : 0.0);
                if (diameter > 0.0)
                    _nDiameter.Value = ClampDecimal(diameter, _nDiameter.Minimum, _nDiameter.Maximum);
                string edgeSkipMode = frame != null && !string.IsNullOrWhiteSpace(frame.EdgeSkipMode)
                    ? frame.EdgeSkipMode
                    : map.EdgeSkipMode;
                if (!string.IsNullOrWhiteSpace(edgeSkipMode))
                    SetSelectedEdgeSkipMode(edgeSkipMode);
                UpdateEdgeSkipModeUi();
                bool mmMode = GetSelectedEdgeSkipMode() == WaferEdgeSkipMode.Millimeter;
                double sideSkip = frame != null
                    ? (mmMode ? frame.SideEdgeSkipMm : frame.SideEdgeSkip)
                    : map.SideEdgeSkip;
                double topBottomSkip = frame != null
                    ? (mmMode ? frame.TopBottomEdgeSkipMm : frame.TopBottomEdgeSkip)
                    : map.TopBottomEdgeSkip;
                _nSideEdgeSkip.Value = ClampDecimal(sideSkip, _nSideEdgeSkip.Minimum, _nSideEdgeSkip.Maximum);
                _nTopBottomEdgeSkip.Value = ClampDecimal(topBottomSkip, _nTopBottomEdgeSkip.Minimum, _nTopBottomEdgeSkip.Maximum);
                _mapSpecDirty = false;
            }
            finally
            {
                _suppressMapSpecEvents = false;
            }
        }

        private void RefreshSettingLabels()
        {
            RefreshRegisteredMapPreview();
            if (_map == null)
            {
                lblMapTitle.Text = "다이 맵";
                UpdateMapApplyInfo();
                return;
            }

            int targetCount = _preview != null ? _preview.Map.Entries.Count(e => e.IsTarget) : 0;
            lblMapTitle.Text = "다이 맵 · 작업 대상 " + targetCount + " / " +
                               (_map.Entries != null ? _map.Entries.Count : 0) +
                               "   X/Y: 셀 가리키기   숫자: 공정 순서   클릭: 대상/제외";
            _mapView.Invalidate();
            UpdateMapApplyInfo();
        }

        private void UpdateMapApplyInfo()
        {
            if (_tbMapApplyInfo == null || btnSave == null)
                return;

            try
            {
                string reason;
                bool valid = ValidateCurrentMapForApply(out reason);
                if (_preview == null || !_preview.Map.Entries.Any(entry => entry.IsTarget))
                {
                    valid = false;
                    reason = _previewError ?? "적용 설정과 BIN 필터를 확인하세요. 공정 대상이 없습니다.";
                }
                btnSave.Enabled = valid;
                btnSave.BackColor = valid ? Color.FromArgb(230, 88, 31) : Color.FromArgb(128, 128, 128);

                string role = CurrentMapKind == RecipeMapKind.Input
                    ? "입력"
                    : (CurrentMapKind == RecipeMapKind.GoodBin ? "출력 양품" : "출력 불량");
                var lines = new List<string>
                {
                    "레시피: " + (_project != null ? _project.FileName ?? "-" : "-"),
                    "대상: " + role,
                    GetPreviewSummary()
                };

                if (_map != null && _map.Entries != null && _map.Entries.Count > 0)
                {
                    List<DieMapEntry> entries = _map.Entries.Where(entry => entry != null).ToList();
                    lines.Add("원본: " + (string.IsNullOrWhiteSpace(_map.SourceFileName) ? "-" : _map.SourceFileName) +
                              " (" + (string.IsNullOrWhiteSpace(_map.SourceFormat) ? "map" : _map.SourceFormat) + ")");
                    if (_preview != null)
                    {
                        var displayed = _preview.Map.Entries;
                        lines.Add("맵 X: " + WaferMapProcessService.FormatMapCoordinate(displayed.Min(entry => entry.LogicalGridX)) +
                            " .. " + WaferMapProcessService.FormatMapCoordinate(displayed.Max(entry => entry.LogicalGridX)) +
                            " / Y: " + WaferMapProcessService.FormatMapCoordinate(displayed.Min(entry => entry.LogicalGridY)) +
                            " .. " + WaferMapProcessService.FormatMapCoordinate(displayed.Max(entry => entry.LogicalGridY)));
                        lines.Add("공정 대상: " + displayed.Count(entry => entry.IsTarget) + " / " + displayed.Count);
                    }
                    if (_map.Generation != null)
                        lines.Add("Rotate : " + _map.Generation.RotationDegrees + "° CW / 생성 다이 " + entries.Count + "개");
                    TapeFrameSubset roleFrame = RecipeDieMapResolver.ResolveFrame(_project, CurrentMapKind);
                    lines.Add((_map.Generation != null ? "Gap(0°): " : "다이 간격: ") + (roleFrame != null
                        ? roleFrame.PitchX.ToString("0.###") + " x " + roleFrame.PitchY.ToString("0.###") + " mm (Recipe)"
                        : "not set"));
                    lines.Add("중심 간격: " + _map.PitchX.ToString("0.###") + " x " + _map.PitchY.ToString("0.###") + " mm (Die + Gap)");
                    lines.Add("다이 크기: " + _map.DieSizeX.ToString("0.###") + " x " + _map.DieSizeY.ToString("0.###") +
                        (_map.Generation != null ? " mm (회전 적용)" : " mm (Die Spec)"));
                    lines.Add("웨이퍼 직경: " + (_map.OuterDiameterMm > 0.0
                        ? _map.OuterDiameterMm.ToString("0.###") + " mm"
                        : "not set"));
                    string basePath = RecipeMapPaths.ResolveBaseConfigured(_project, CurrentMapKind);
                    if (!string.IsNullOrWhiteSpace(basePath) && File.Exists(basePath))
                    {
                        DieMap baseMap = DieMapGenerator.Load(basePath);
                        if (baseMap != null)
                            lines.Add("Base   : Center step " + baseMap.PitchX.ToString("0.###") + " x " +
                                      baseMap.PitchY.ToString("0.###") + " mm (source file)");
                    }
                    lines.Add("대상/전체: " + entries.Count(entry => entry.IsTarget) + "/" + entries.Count +
                              (_maskDirty ? " (수정됨 · 미적용)" : " (불러온 값)"));
                    string approvalReason;
                    bool approved = RecipeMapPaths.IsMapApproved(_project, CurrentMapKind, _map, out approvalReason);
                    lines.Add("맵 승인: " + (approved ? "확인 완료" : "확인 필요 · " + approvalReason));
                }

                lines.Add(valid ? "현재 편집값: 확인 및 적용 가능" : "현재 편집값: 적용 불가 · " + reason);
                lines.Add("[맵 확인 및 적용]은 선택한 맵의 작업 대상/제외와 승인을 저장합니다.");
                _tbMapApplyInfo.Text = string.Join(Environment.NewLine, lines);
            }
            catch (Exception ex)
            {
                btnSave.Enabled = false;
                btnSave.BackColor = Color.FromArgb(128, 128, 128);
                _tbMapApplyInfo.Text = "맵 확인 실패" + Environment.NewLine + ex.Message;
            }
        }

        private bool ConfirmPendingMaskBeforeRoleChange()
        {
            if (!_maskDirty)
                return true;

            DialogResult answer = QMC.Common.MessageDialog.Show(this,
                "현재 " + (CurrentMapKind == RecipeMapKind.GoodBin ? "GOOD" : "NG") +
                " Target/Skip 편집값이 아직 적용되지 않았습니다.\r\n\r\nYes: 현재 맵 맵 확인 및 적용\r\nNo: 변경 폐기\r\nCancel: 역할 전환 취소",
                "Bin Map 변경 확인",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);
            if (answer == DialogResult.Cancel)
                return false;
            if (answer == DialogResult.No)
            {
                _maskDirty = false;
                return true;
            }

            SaveMapToRecipe();
            return !_maskDirty;
        }

        private bool ValidateCurrentMapForApply(out string reason)
        {
            reason = "";
            if (_project == null)
            {
                reason = "active Recipe가 없습니다.";
                return false;
            }
            if (_map == null || _map.Entries == null || _map.Entries.Count == 0)
            {
                reason = "역할 맵이 로드되지 않았습니다.";
                return false;
            }
            if (!RecipeDieMapResolver.IsSupportedForEquipment(_map, out reason))
                return false;
            if (_map.PitchX <= 0.0 || _map.PitchY <= 0.0 ||
                double.IsNaN(_map.PitchX) || double.IsNaN(_map.PitchY) ||
                double.IsInfinity(_map.PitchX) || double.IsInfinity(_map.PitchY))
            {
                reason = "Pitch X/Y는 유한한 양수여야 합니다.";
                return false;
            }
            if (_map.DieSizeX <= 0.0 || _map.DieSizeY <= 0.0 ||
                double.IsNaN(_map.DieSizeX) || double.IsNaN(_map.DieSizeY) ||
                double.IsInfinity(_map.DieSizeX) || double.IsInfinity(_map.DieSizeY))
            {
                reason = "Die Size X/Y가 다이 사양의 양수 값으로 연결되지 않았습니다.";
                return false;
            }
            TapeFrameSubset frame = RecipeDieMapResolver.ResolveFrame(_project, CurrentMapKind);
            if (frame == null || double.IsNaN(frame.PitchX) || double.IsInfinity(frame.PitchX) ||
                double.IsNaN(frame.PitchY) || double.IsInfinity(frame.PitchY) ||
                frame.PitchX < 0.0 || frame.PitchY < 0.0)
            {
                reason = "Pitch Gap X/Y는 0 이상의 유한한 값이어야 합니다.";
                return false;
            }
            const double geometryTolerance = 0.000001;
            bool generated = _map.Generation != null || string.Equals(_map.SourceFormat,
                GeneratedWaferMapCodec.SourceFormat, StringComparison.Ordinal);
            if (generated && !GeneratedWaferMapCodec.Validate(_map, out reason))
                return false;
            bool swapped = generated && (_map.Generation.RotationDegrees == 90 || _map.Generation.RotationDegrees == 270);
            double expectedStepX = _map.DieSizeX + (swapped ? frame.PitchY : frame.PitchX);
            double expectedStepY = _map.DieSizeY + (swapped ? frame.PitchX : frame.PitchY);
            if (Math.Abs(_map.PitchX - expectedStepX) > geometryTolerance ||
                Math.Abs(_map.PitchY - expectedStepY) > geometryTolerance)
            {
                reason = "역할 맵 중심 간격이 Die Size + Pitch Gap과 다릅니다. Step=" +
                         _map.PitchX.ToString("0.######") + " x " + _map.PitchY.ToString("0.######") +
                         " mm, expected=" + expectedStepX.ToString("0.######") + " x " +
                         expectedStepY.ToString("0.######") + (generated
                             ? " mm. 맵 생성 미리보기에서 다시 생성하고 저장하십시오."
                             : " mm. 웨이퍼 사양에서 SAVE하여 맵을 다시 생성하세요.");
                return false;
            }

            string frameMismatch;
            if (!RecipeDieMapResolver.IsCompatibleWithFrame(_map, frame, out frameMismatch))
            {
                reason = "Wafer Spec 불일치: " + frameMismatch;
                return false;
            }

            var localKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var originalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DieMapEntry entry in _map.Entries.Where(item => item != null))
            {
                if (entry.DieMapX < 0 || entry.DieMapX >= _map.DieMapX ||
                    entry.DieMapY < 0 || entry.DieMapY >= _map.DieMapY)
                {
                    reason = "Local index가 Grid 범위를 벗어났습니다: " + entry.DieMapX + "," + entry.DieMapY;
                    return false;
                }
                if (!localKeys.Add(entry.DieMapX + "," + entry.DieMapY))
                {
                    reason = "중복 Local index가 있습니다: " + entry.DieMapX + "," + entry.DieMapY;
                    return false;
                }
                string originalKey = ResolveOriginalX(entry) + "," + ResolveOriginalY(entry);
                if (!originalKeys.Add(originalKey))
                {
                    reason = "중복 Original index가 있습니다: " + originalKey;
                    return false;
                }

                double expectedGridX = generated
                    ? _map.OriginX / _map.PitchX + entry.DieMapX
                    : entry.DieMapX - Math.Max(0, _map.DieMapX - 1) / 2.0;
                double expectedGridY = generated
                    ? _map.OriginY / _map.PitchY + entry.DieMapY
                    : DieMapGenerator.CalculateEquipmentGridY(entry.DieMapY, _map.DieMapY);
                if (Math.Abs(entry.EquipmentGridX - expectedGridX) > 0.000001 ||
                    Math.Abs(entry.EquipmentGridY - expectedGridY) > 0.000001 ||
                    Math.Abs(entry.PosX - expectedGridX * _map.PitchX) > 0.000001 ||
                    Math.Abs(entry.PosY - expectedGridY * _map.PitchY) > 0.000001)
                {
                    reason = "장비 Grid/Position이 중심 (0,0) 또는 역할 Pitch와 맞지 않습니다. local=" +
                             entry.DieMapX + "," + entry.DieMapY;
                    return false;
                }
            }

            string basePath = RecipeMapPaths.ResolveBaseConfigured(_project, CurrentMapKind);
            if (string.IsNullOrWhiteSpace(basePath) || !File.Exists(basePath))
            {
                reason = "Base WaferMap 파일을 찾을 수 없습니다.";
                return false;
            }

            DieMap baseMap = DieMapGenerator.Load(basePath);
            if (baseMap == null || baseMap.Entries == null || baseMap.Entries.Count == 0)
            {
                reason = "Base WaferMap을 읽을 수 없습니다.";
                return false;
            }

            if (generated && !RecipeDieMapResolver.IsCompatibleWithFrame(baseMap, frame, out reason))
            {
                reason = "생성 Base 맵 불일치: " + reason;
                return false;
            }

            var baseDomain = new HashSet<string>(baseMap.Entries.Where(entry => entry != null)
                .Select(entry => ResolveOriginalX(entry) + "," + ResolveOriginalY(entry)), StringComparer.OrdinalIgnoreCase);
            var roleDomain = new HashSet<string>(_map.Entries.Where(entry => entry != null)
                .Select(entry => ResolveOriginalX(entry) + "," + ResolveOriginalY(entry)), StringComparer.OrdinalIgnoreCase);
            if (baseMap.Entries.Count(entry => entry != null) != _map.Entries.Count(entry => entry != null) ||
                !baseDomain.SetEquals(roleDomain))
            {
                reason = "Base와 역할 맵의 Original index 집합이 다릅니다.";
                return false;
            }

            return true;
        }

        private static int ResolveOriginalX(DieMapEntry entry)
        {
            return entry != null && entry.OriginalMapX >= 0 ? entry.OriginalMapX : (entry != null ? entry.DieMapX : 0);
        }

        private static int ResolveOriginalY(DieMapEntry entry)
        {
            return entry != null && entry.OriginalMapY >= 0 ? entry.OriginalMapY : (entry != null ? entry.DieMapY : 0);
        }

        private void OnMapCellClicked(DieMapEntry entry)
        {
            if (entry == null || _preview == null)
                return;

            DieMapEntry source = _preview.GetSource(entry);
            if (rbManualSelectPick.Checked)
                ApplyEntryTarget(source, true);
            else if (rbAlignCheckIndex.Checked)
                ApplyEntryTarget(source, false);
            else if (rbStandard.Checked)
                ApplyEntryTarget(source, !source.IsTarget);
            else
                return;

            RefreshSettingLabels();
        }

        private void ApplyEntryTarget(DieMapEntry entry, bool target)
        {
            if (entry == null)
                return;

            entry.IsTarget = target;
            entry.Result = DieResult.Unknown;
            // 표시 회전이나 BIN 필터가 원본 다이 데이터에 저장되지 않도록 Mask만 편집한다.
            _maskDirty = true;
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
                _project = LoadActiveRecipeProject();
                if (_project == null)
                    return false;

                string path;
                string reason;
                DieMap loaded = LoadExactConfiguredRoleMap(out path, out reason);
                if (loaded == null)
                {
                    if (showMessage)
                    {
                        string detail = string.IsNullOrWhiteSpace(reason) ? "Recipe에 연결된 Die Map 파일이 없습니다." : reason;
                        QMC.Common.MessageDialog.Show(this, detail, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    return false;
                }

                ApplyMap(loaded, "Recipe Die Map: " + Path.GetFileName(path));
                _currentMapPath = path;
                _currentMapWriteUtc = !string.IsNullOrWhiteSpace(path) && File.Exists(path)
                    ? File.GetLastWriteTimeUtc(path)
                    : DateTime.MinValue;
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

        private DieMap LoadExactConfiguredRoleMap(out string sourcePath, out string reason)
        {
            sourcePath = "";
            reason = "";
            try
            {
                if (_project == null)
                {
                    reason = "Recipe가 로드되지 않았습니다.";
                    return null;
                }

                string configured = RecipeMapPaths.ExactConfiguredFileName(_project, CurrentMapKind);
                string configuredPath = RecipeMapPaths.ResolveConfiguredPath(configured);
                if (string.IsNullOrWhiteSpace(configuredPath))
                {
                    reason = CurrentMapKind + " DieMap이 설정되지 않았습니다. Recipe → 웨이퍼 사양에서 Base WaferMap을 연결하세요.";
                    return null;
                }

                string loadPath = File.Exists(configuredPath) ? configuredPath : "";
                if (string.IsNullOrWhiteSpace(loadPath))
                {
                    string csvSidecar = Path.ChangeExtension(configuredPath, ".csv");
                    if (!string.IsNullOrWhiteSpace(csvSidecar) && File.Exists(csvSidecar))
                        loadPath = csvSidecar;
                }

                if (string.IsNullOrWhiteSpace(loadPath))
                {
                    reason = "설정된 DieMap 파일을 찾을 수 없습니다: " + configuredPath;
                    return null;
                }

                DieMap loaded = DieMapGenerator.Load(loadPath);
                if (loaded == null || loaded.Entries == null || loaded.Entries.Count == 0)
                {
                    reason = "설정된 DieMap 파일이 비어 있습니다: " + loadPath;
                    return null;
                }

                string mismatch;
                TapeFrameSubset frame = RecipeDieMapResolver.ResolveFrame(_project, CurrentMapKind);
                if (!RecipeDieMapResolver.IsCompatibleWithFrame(loaded, frame, out mismatch))
                {
                    reason = "설정된 DieMap과 Project Frame 값이 일치하지 않습니다: " + mismatch;
                    return null;
                }

                sourcePath = loadPath;
                return DieMapGenerator.Normalize(loaded);
            }
            catch (Exception ex)
            {
                reason = "Recipe DieMap을 읽지 못했습니다: " + ex.Message;
                sourcePath = "";
                return null;
            }
        }

        private RecipeProject LoadActiveRecipeProject()
        {
            Form1 host = FindForm() as Form1;
            string activeName = host != null ? host.ActiveRecipeName : "";
            if (!string.IsNullOrWhiteSpace(activeName))
            {
                RecipeProject active = RecipeStore.Load(activeName);
                if (active == null)
                    throw new InvalidDataException("현재 장비 Recipe Project를 읽을 수 없습니다: " + activeName);
                return active;
            }

            if (_project != null && !string.IsNullOrWhiteSpace(_project.FileName))
            {
                RecipeProject current = RecipeStore.Load(_project.FileName);
                if (current != null)
                    return current;
            }

            return RecipeStore.LoadLastOrDefault();
        }

        private void RefreshMapLibraryList()
        {
            try
            {
                string selectedKey = !string.IsNullOrWhiteSpace(_currentLibraryKey)
                    ? _currentLibraryKey
                    : (_cbMapLibrary.SelectedItem != null ? _cbMapLibrary.SelectedItem.ToString() : "");
                if (string.IsNullOrWhiteSpace(selectedKey) && !string.IsNullOrWhiteSpace(_currentFrameSpecName))
                    selectedKey = "[SPEC] " + _currentFrameSpecName.Trim();
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

                SelectLibraryKey(selectedKey);

                if (_cbMapLibrary.SelectedIndex < 0 && _cbMapLibrary.Items.Count > 0)
                    _cbMapLibrary.SelectedIndex = 0;

                _currentLibraryKey = _cbMapLibrary.SelectedItem != null ? _cbMapLibrary.SelectedItem.ToString() : "";
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

            string[] txtFiles = Directory.GetFiles(dir, "*.txt");
            string[] jsonFiles = Directory.GetFiles(dir, "*.json");
            var txtBaseNames = new HashSet<string>(
                txtFiles.Select(path => Path.GetFileNameWithoutExtension(path) ?? ""),
                StringComparer.OrdinalIgnoreCase);
            var jsonBaseNames = new HashSet<string>(
                jsonFiles.Select(path => Path.GetFileNameWithoutExtension(path) ?? ""),
                StringComparer.OrdinalIgnoreCase);

            IEnumerable<string> standaloneJsonFiles = jsonFiles
                .Where(path => !txtBaseNames.Contains(Path.GetFileNameWithoutExtension(path) ?? ""));

            IEnumerable<string> standaloneCsvFiles = Directory.GetFiles(dir, "*.csv")
                .Where(path =>
                {
                    string baseName = Path.GetFileNameWithoutExtension(path) ?? "";
                    return !txtBaseNames.Contains(baseName) && !jsonBaseNames.Contains(baseName);
                });

            return txtFiles
                .Concat(standaloneJsonFiles)
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

        private bool SelectLibraryKey(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key) || !_cbMapLibrary.Items.Contains(key))
                    return false;

                _cbMapLibrary.SelectedItem = key;
                _currentLibraryKey = key;
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool SelectLibraryPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return false;

                string fullPath = Path.GetFullPath(path);
                foreach (var pair in _mapLibraryPaths)
                {
                    if (string.Equals(Path.GetFullPath(pair.Value), fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        _cbMapLibrary.SelectedItem = pair.Key;
                        _currentLibraryKey = pair.Key;
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
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
                    _currentLibraryKey = key;
                    ApplyFrameSpecToControls(frameSpec);
                    _currentMapPath = "";
                    _currentFrameSpecName = frameSpec.Name ?? "";
                    CreateMapFromRecipeSpec(false);
                    SelectFrameSpecName(frameSpec.Name);
                    return;
                }

                QMC.Common.MessageDialog.Show(this, "맵 생성을 위한 Frame Spec을 선택하세요.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Frame Spec load failed:\r\n" + ex.Message, "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ApplyFrameSpecToControls(TapeFrameSpec spec)
        {
            if (spec == null)
                return;

            try
            {
                _suppressMapSpecEvents = true;
                _tbFrameSpecName.Text = spec.Name ?? "";
                _currentFrameSpecName = spec.Name ?? "";
                _nPitchX.Value = ClampDecimal(spec.PitchX, _nPitchX.Minimum, _nPitchX.Maximum);
                _nPitchY.Value = ClampDecimal(spec.PitchY, _nPitchY.Minimum, _nPitchY.Maximum);
                if (spec.DieSizeX > 0.0 && spec.DieSizeY > 0.0)
                {
                    _nDieSizeX.Value = ClampDecimal(spec.DieSizeX, _nDieSizeX.Minimum, _nDieSizeX.Maximum);
                    _nDieSizeY.Value = ClampDecimal(spec.DieSizeY, _nDieSizeY.Minimum, _nDieSizeY.Maximum);
                }
                else
                {
                    ApplyDieSpecToControls(spec.DieSpecName);
                }
                _nDiameter.Value = ClampDecimal(spec.OuterDiameterMm, _nDiameter.Minimum, _nDiameter.Maximum);
                SetSelectedEdgeSkipMode(spec.EdgeSkipMode);
                UpdateEdgeSkipModeUi();
                if (GetSelectedEdgeSkipMode() == WaferEdgeSkipMode.Millimeter)
                {
                    _nSideEdgeSkip.Value = ClampDecimal(spec.SideEdgeSkipMm, _nSideEdgeSkip.Minimum, _nSideEdgeSkip.Maximum);
                    _nTopBottomEdgeSkip.Value = ClampDecimal(spec.TopBottomEdgeSkipMm, _nTopBottomEdgeSkip.Minimum, _nTopBottomEdgeSkip.Maximum);
                }
                else
                {
                    _nSideEdgeSkip.Value = ClampDecimal(spec.SideEdgeSkip, _nSideEdgeSkip.Minimum, _nSideEdgeSkip.Maximum);
                    _nTopBottomEdgeSkip.Value = ClampDecimal(spec.TopBottomEdgeSkip, _nTopBottomEdgeSkip.Minimum, _nTopBottomEdgeSkip.Maximum);
                }
                RecalculateGridPreviewFromControls();
                _mapSpecDirty = true;
            }
            finally
            {
                _suppressMapSpecEvents = false;
            }
        }

        private void ApplyDieSpecToControls(string dieSpecName)
        {
            DieSpec dieSpec = !string.IsNullOrWhiteSpace(dieSpecName) ? MaterialSpecs.FindDie(dieSpecName) : null;
            double dieSizeX = dieSpec != null && dieSpec.WidthMm > 0.0 ? dieSpec.WidthMm : ResolveRecipeDieSizeX();
            double dieSizeY = dieSpec != null && dieSpec.HeightMm > 0.0 ? dieSpec.HeightMm : ResolveRecipeDieSizeY();
            _nDieSizeX.Value = ClampDecimal(dieSizeX, _nDieSizeX.Minimum, _nDieSizeX.Maximum);
            _nDieSizeY.Value = ClampDecimal(dieSizeY, _nDieSizeY.Minimum, _nDieSizeY.Maximum);
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
                    _currentLibraryKey = item.ToString();
                    return;
                }
            }
        }

        private void SaveMapAsLibraryMap()
        {
            try
            {
                _project = LoadActiveRecipeProject();
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
                    _currentLibraryKey = "";
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
                    Filter = "DieMap files|*.json;*.csv;*.txt|JSON|*.json|CSV|*.csv|WaferMap TXT|*.txt|All files|*.*",
                    InitialDirectory = ResolveMapImportInitialDirectory()
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
                    _currentLibraryKey = "";
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

        private static string ResolveMapImportInitialDirectory()
        {
            string waferMapDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "WaferMap");
            return Directory.Exists(waferMapDir) ? waferMapDir : RecipeMapPaths.GetDieMapDirectory();
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
                    Title = "Export DieMap",
                    Filter = "WaferMap TXT|*.txt|CSV|*.csv|JSON|*.json",
                    FileName = (_map.FrameObjId ?? "DieMap") + ".txt"
                })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return;

                    DieMapGenerator.Save(_map, dlg.FileName);

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
                if (!string.IsNullOrWhiteSpace(explicitPath))
                {
                    ShowSpecEditPageMessage();
                    return;
                }

                _project = LoadActiveRecipeProject();
                if (_project == null)
                    return;

                if (_map == null)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "저장할 Target/Skip Mask가 없습니다.\r\nRecipe → 웨이퍼 사양에서 Base WaferMap을 먼저 연결하세요.",
                        "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_mapSpecDirty)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "맵 형상은 이 화면에서 저장할 수 없습니다.\r\nRecipe → 웨이퍼 사양에서 설정한 맵을 다시 불러온 뒤 Target/Skip Mask만 편집하세요.",
                        "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                RefreshSettingLabels();
                if (!btnSave.Enabled)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "현재 설정으로 맵을 적용할 수 없습니다.\r\n" + _tbMapApplyInfo.Text,
                        "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                RecipeMapBuildResult result = RecipeMapBuildService.SaveRoleTargetMask(
                    _project,
                    CurrentMapKind,
                    _map,
                    RecipeStore.Save);
                if (result == null || !result.Success)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "Target/Skip Mask 저장 실패:\r\n" + (result != null ? result.Message : "결과 없음"),
                        "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string recipeMapPath = ResolveSavedRolePath(result);
                ApplyMap(result.RoleMap, "Recipe Mask: " + Path.GetFileName(recipeMapPath));
                _currentMapPath = recipeMapPath;
                _currentMapWriteUtc = File.Exists(recipeMapPath) ? File.GetLastWriteTimeUtc(recipeMapPath) : DateTime.MinValue;
                // Recipe map의 PosX/PosY는 중심 (0,0) 상대 오프셋이다.
                // 이를 Active map에 즉시 넣으면 Input Camera X / Stage Y 절대좌표로 오해될 수 있으므로
                // 다음 wafer의 Align + Die Mapping에서 실제 축 중심을 더한 뒤 적용한다.
                bool appliedImmediately = false;
                RefreshMapLibraryList();
                SelectLibraryPath(recipeMapPath);

                QMC.Common.Log.Write("Main", "RECIPE", "MapCreatePage",
                    "Die Map Target/Skip Mask 저장 완료. path=" + recipeMapPath +
                    ", immediateApply=" + appliedImmediately + " - Ok");

                string applyMessage =
                    "현재 진행 중인 웨이퍼/축에는 즉시 적용하지 않습니다.\r\n" +
                    "다음 웨이퍼 Align + Die Mapping에서 Input은 Input Camera X/Input Stage Y, " +
                    "Output은 Output Camera X/Output Stage Y 기준좌표를 사용합니다(배치 시 Picker X로 변환).";

                QMC.Common.MessageDialog.Show(this,
                    "맵 확인 및 적용 완료. 역할 맵 승인 hash와 Target/Skip Mask를 저장했습니다.\r\n" +
                    applyMessage + "\r\n" + recipeMapPath,
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

        private string ResolveSavedRolePath(RecipeMapBuildResult result)
        {
            if (result == null)
                return "";
            if (CurrentMapKind == RecipeMapKind.GoodBin)
                return result.GoodMapPath ?? "";
            if (CurrentMapKind == RecipeMapKind.NgBin)
                return result.NgMapPath ?? "";
            return result.InputMapPath ?? "";
        }

        private string SaveRecipeCompatibleMapFiles(string primaryPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(primaryPath))
                    return primaryPath ?? "";

                string csvPath = Path.ChangeExtension(primaryPath, ".csv");
                if (!string.IsNullOrWhiteSpace(csvPath))
                    DieMapGenerator.SaveCsv(_map, csvPath);

                if (string.Equals(Path.GetExtension(primaryPath), ".csv", StringComparison.OrdinalIgnoreCase))
                {
                    string txtPath = Path.ChangeExtension(primaryPath, ".txt");
                    if (!string.IsNullOrWhiteSpace(txtPath))
                        DieMapGenerator.Save(_map, txtPath);
                }

                // JSON을 canonical 경로로 유지하고 CSV는 사람이 확인할 수 있는 sidecar로만 저장한다.
                return primaryPath;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "RECIPE", "MapCreatePage",
                    "Recipe compatible map sidecar save failed: " + ex.Message + ". primaryPath=" + primaryPath + " - Failed");
                return primaryPath ?? "";
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
                "공용 형상값은 이 화면에서 수정하지 않습니다.\r\nDie는 Recipe → 다이 사양, Wafer/Pitch는 Recipe → 웨이퍼 사양에서 설정하세요.",
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
