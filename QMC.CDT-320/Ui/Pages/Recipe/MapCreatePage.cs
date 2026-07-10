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
        private const string EdgeSkipGridText = "GRID COUNT";
        private const string EdgeSkipMmText = "MM";
        private const string EdgeSkipExternalMapText = "EXTERNAL MAP";
        private readonly Dictionary<string, string> _mapLibraryPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DieMap> _mapLibraryMemoryMaps = new Dictionary<string, DieMap>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TapeFrameSpec> _mapLibraryFrameSpecs = new Dictionary<string, TapeFrameSpec>(StringComparer.OrdinalIgnoreCase);
        private Label _lblDieSizeXKey;
        private Label _lblDieSizeYKey;
        private Label _lblEdgeSkipModeKey;
        private NumericUpDown _nDieSizeX;
        private NumericUpDown _nDieSizeY;
        private ComboBox _cbEdgeSkipMode;
        private bool _suppressMapSpecEvents;
        private bool _mapSpecDirty;

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
            _mapView.Caption = "Recipe Die Map";
            _mapMenu = null;
            _mapView.ContextMenuStrip = null;
            EnsurePhysicalMapSpecControls();

            lblHeader.Text = _isOutputMap ? "BIN MAP GENERATOR" : "INPUT MAP GENERATOR";
            lblSettingTitle.Text = "DIE MAP SETTING";
            lblActionTitle.Text = "APPLY";
            _btnMapLoad.Text = "LOAD SPEC";
            btnSave.Text = "APPLY TO RECIPE";
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
            ClearMapClickModes();
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

                actionSection.SetCellPosition(btnSave, new TableLayoutPanelCellPosition(0, 1));
                actionSection.SetColumnSpan(btnSave, 2);
                btnSave.Dock = DockStyle.Fill;
                actionSection.RowStyles[1].SizeType = SizeType.Percent;
                actionSection.RowStyles[1].Height = 100F;
                actionSection.RowStyles[2].SizeType = SizeType.Absolute;
                actionSection.RowStyles[2].Height = 0F;
                actionSection.RowStyles[3].SizeType = SizeType.Absolute;
                actionSection.RowStyles[3].Height = 0F;

                rightLayout.RowStyles[1].SizeType = SizeType.Absolute;
                rightLayout.RowStyles[1].Height = _isOutputMap ? 70F : 0F;
                rightLayout.RowStyles[0].SizeType = SizeType.Absolute;
                rightLayout.RowStyles[0].Height = 340F;
                rightLayout.RowStyles[2].SizeType = SizeType.Absolute;
                rightLayout.RowStyles[2].Height = 90F;
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

        private void EnsurePhysicalMapSpecControls()
        {
            if (_nDieSizeX != null)
                return;

            _lblDieSizeXKey = CreateSettingLabel("lblDieSizeXKey", "DIE SIZE X (mm)");
            _lblDieSizeYKey = CreateSettingLabel("lblDieSizeYKey", "DIE SIZE Y (mm)");
            _lblEdgeSkipModeKey = CreateSettingLabel("lblEdgeSkipModeKey", "EDGE SKIP MODE");
            _nDieSizeX = CreateSpecNumeric("_nDieSizeX", 0.001M, 1000M, 1M, 3);
            _nDieSizeY = CreateSpecNumeric("_nDieSizeY", 0.001M, 1000M, 1M, 3);
            _cbEdgeSkipMode = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Consolas", 10F),
                Margin = new Padding(1)
            };
            _cbEdgeSkipMode.Items.Add(EdgeSkipGridText);
            _cbEdgeSkipMode.Items.Add(EdgeSkipMmText);
            _cbEdgeSkipMode.Items.Add(EdgeSkipExternalMapText);
            _cbEdgeSkipMode.SelectedIndex = 0;

            RebuildSettingSectionLayout();
            HookMapSpecControlEvents();
            EnableBinAuthoringControls();
            UpdateEdgeSkipModeUi();
        }

        private static Label CreateSettingLabel(string name, string text)
        {
            return new Label
            {
                BackColor = Color.Gainsboro,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 9F),
                Margin = new Padding(1),
                Name = name,
                Padding = new Padding(6, 0, 6, 0),
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static NumericUpDown CreateSpecNumeric(string name, decimal minimum, decimal maximum, decimal value, int decimals)
        {
            var control = new NumericUpDown
            {
                DecimalPlaces = decimals,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10F),
                InterceptArrowKeys = false,
                Margin = new Padding(1),
                Maximum = maximum,
                Minimum = minimum,
                Name = name,
                TextAlign = HorizontalAlignment.Right,
                Value = value
            };
            return control;
        }

        private void RebuildSettingSectionLayout()
        {
            if (settingSection == null)
                return;

            settingSection.SuspendLayout();
            try
            {
                settingSection.Controls.Clear();
                settingSection.RowStyles.Clear();
                settingSection.RowCount = 11;
                settingSection.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
                for (int i = 1; i < settingSection.RowCount; i++)
                    settingSection.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

                lblSettingTitle.Text = "DIE MAP SETTING";
                settingSection.Controls.Add(lblSettingTitle, 0, 0);
                settingSection.SetColumnSpan(lblSettingTitle, 2);

                AddSettingRow(1, lblChipCountXKey, _tbFrameSpecName, "FRAME SPEC NAME");
                AddSettingRow(2, lblChipCountYKey, _nGridX, "GRID X (AUTO)");
                AddSettingRow(3, lblChipPitchXKey, _nGridY, "GRID Y (AUTO)");
                AddSettingRow(4, lblChipPitchYKey, _nPitchX, "PITCH X (mm)");
                AddSettingRow(5, lblWaferDiameterKey, _nPitchY, "PITCH Y (mm)");
                AddSettingRow(6, _lblDieSizeXKey, _nDieSizeX, "DIE SIZE X (mm)");
                AddSettingRow(7, _lblDieSizeYKey, _nDieSizeY, "DIE SIZE Y (mm)");
                AddSettingRow(8, lblAxisXKey, _nDiameter, "OUTER DIAMETER (mm)");
                AddSettingRow(9, _lblEdgeSkipModeKey, _cbEdgeSkipMode, "EDGE SKIP MODE");
                AddSettingRow(10, lblAxisYKey, edgeSkipPanel, "EDGE SKIP L/R, T/B");
            }
            finally
            {
                settingSection.ResumeLayout(false);
                settingSection.PerformLayout();
            }
        }

        private void AddSettingRow(int row, Label label, Control valueControl, string text)
        {
            if (label == null || valueControl == null)
                return;

            label.Text = text;
            settingSection.Controls.Add(label, 0, row);
            settingSection.Controls.Add(valueControl, 1, row);
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
                int gridX = ResolvePitchBasedGridCount((double)_nDiameter.Value, (double)_nPitchX.Value);
                int gridY = ResolvePitchBasedGridCount((double)_nDiameter.Value, (double)_nPitchY.Value);
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
                ? "EDGE SKIP L/R, T/B (external)"
                : (mmMode ? "EDGE SKIP L/R, T/B (mm)" : "EDGE SKIP L/R, T/B (grid)");
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

                _mode = next;
                chkCircularMap.Text = "BIN CIRCLE DIE MAP";
                _currentMapPath = "";
                _currentFrameSpecName = "";
                if (!LoadSavedRecipeMap(false))
                    CreateMapFromRecipeSpec(false);
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
            SetNumericEditable(_nPitchX);
            SetNumericEditable(_nPitchY);
            SetNumericEditable(_nDieSizeX);
            SetNumericEditable(_nDieSizeY);
            SetNumericEditable(_nDiameter);
            SetNumericEditable(_nSideEdgeSkip);
            SetNumericEditable(_nTopBottomEdgeSkip);
            if (_cbEdgeSkipMode != null)
                _cbEdgeSkipMode.Enabled = true;
        }

        private static void SetNumericEditable(NumericUpDown control)
        {
            if (control == null)
                return;
            control.Enabled = true;
            control.ReadOnly = false;
            control.TabStop = true;
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
                _nDieSizeX.Value = ClampDecimal(ResolveFrameDieSizeX(frame), _nDieSizeX.Minimum, _nDieSizeX.Maximum);
                _nDieSizeY.Value = ClampDecimal(ResolveFrameDieSizeY(frame), _nDieSizeY.Minimum, _nDieSizeY.Maximum);
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

        private double ResolveFrameDieSizeX(TapeFrameSubset frame)
        {
            if (frame != null && frame.DieSizeX > 0.0)
                return frame.DieSizeX;
            return ResolveRecipeDieSizeX();
        }

        private double ResolveFrameDieSizeY(TapeFrameSubset frame)
        {
            if (frame != null && frame.DieSizeY > 0.0)
                return frame.DieSizeY;
            return ResolveRecipeDieSizeY();
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
                SelectFrameSpecName(_currentFrameSpecName);

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

        private void ReloadRecipeMapIfChanged()
        {
            try
            {
                RecipeProject latest = RecipeStore.LoadLastOrDefault();
                if (latest == null)
                    return;

                _project = latest;
                string path = ResolveRecipeMapPath(_project, _isOutputMap);
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return;

                DateTime writeUtc = File.GetLastWriteTimeUtc(path);
                bool pathChanged = string.IsNullOrWhiteSpace(_currentMapPath) ||
                                   !string.Equals(Path.GetFullPath(_currentMapPath), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase);
                if (!pathChanged && writeUtc == _currentMapWriteUtc)
                    return;

                // 현재 기준: Wafer Spec 화면에서 연결한 최신 recipe map은 Map Create 재진입 시 다시 읽는다.
                LoadEdgeSkipFromRecipe();
                RefreshMapLibraryList();
                LoadSavedRecipeMap(false);
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
            WaferEdgeSkipMode edgeSkipMode = IsMillimeterEdgeSkipMode(frame.EdgeSkipMode)
                ? WaferEdgeSkipMode.Millimeter
                : WaferEdgeSkipMode.Grid;
            double sideEdgeSkip = edgeSkipMode == WaferEdgeSkipMode.Millimeter ? frame.SideEdgeSkipMm : frame.SideEdgeSkip;
            double topBottomEdgeSkip = edgeSkipMode == WaferEdgeSkipMode.Millimeter ? frame.TopBottomEdgeSkipMm : frame.TopBottomEdgeSkip;

            // 현재 기준: Input/Bin 모두 외경, 피치, 다이 크기, Edge Skip 모드만으로 원형 맵을 생성한다.
            DieMap map = DieMapGenerator.GenerateCircularWafer(
                frame.OuterDiameterMm,
                frame.PitchX,
                frame.PitchY,
                frame.PitchX,
                frame.PitchY,
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
            double diameter = (double)_nDiameter.Value > 0.0 ? (double)_nDiameter.Value : 0.0;
            double pitchX = (double)_nPitchX.Value > 0.0 ? (double)_nPitchX.Value : 1.0;
            double pitchY = (double)_nPitchY.Value > 0.0 ? (double)_nPitchY.Value : 1.0;
            double dieSizeX = _nDieSizeX != null && (double)_nDieSizeX.Value > 0.0 ? (double)_nDieSizeX.Value : pitchX;
            double dieSizeY = _nDieSizeY != null && (double)_nDieSizeY.Value > 0.0 ? (double)_nDieSizeY.Value : pitchY;
            WaferEdgeSkipMode edgeSkipMode = GetSelectedEdgeSkipMode();
            string edgeSkipModeName = GetSelectedEdgeSkipModeName();
            bool externalMapMode = string.Equals(edgeSkipModeName, "ExternalMap", StringComparison.OrdinalIgnoreCase);
            double edgeSideValue = _nSideEdgeSkip != null ? (double)_nSideEdgeSkip.Value : 0.0;
            double edgeTopBottomValue = _nTopBottomEdgeSkip != null ? (double)_nTopBottomEdgeSkip.Value : 0.0;

            return new TapeFrameSubset
            {
                FrameSpecName = string.IsNullOrWhiteSpace(_tbFrameSpecName.Text) ? "RecipeFrame" : _tbFrameSpecName.Text.Trim(),
                DieMapX = ResolvePitchBasedGridCount(diameter, pitchX),
                DieMapY = ResolvePitchBasedGridCount(diameter, pitchY),
                PitchX = pitchX,
                PitchY = pitchY,
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

        private static int ResolvePitchBasedGridCount(double outerDiameterMm, double pitchMm)
        {
            if (outerDiameterMm <= 0.0 || pitchMm <= 0.0)
                return 1;

            return DieMapGenerator.CalculateWaferGridCount(outerDiameterMm, pitchMm, pitchMm);
        }

        private void ApplyMapToControls(DieMap map)
        {
            if (map == null)
                return;

            try
            {
                _suppressMapSpecEvents = true;
                _nGridX.Value = ClampDecimal(map.DieMapX, _nGridX.Minimum, _nGridX.Maximum);
                _nGridY.Value = ClampDecimal(map.DieMapY, _nGridY.Minimum, _nGridY.Maximum);
                _nPitchX.Value = ClampDecimal(map.PitchX, _nPitchX.Minimum, _nPitchX.Maximum);
                _nPitchY.Value = ClampDecimal(map.PitchY, _nPitchY.Minimum, _nPitchY.Maximum);
                if (map.DieSizeX > 0.0)
                    _nDieSizeX.Value = ClampDecimal(map.DieSizeX, _nDieSizeX.Minimum, _nDieSizeX.Maximum);
                if (map.DieSizeY > 0.0)
                    _nDieSizeY.Value = ClampDecimal(map.DieSizeY, _nDieSizeY.Minimum, _nDieSizeY.Maximum);
                if (map.OuterDiameterMm > 0.0)
                    _nDiameter.Value = ClampDecimal(map.OuterDiameterMm, _nDiameter.Minimum, _nDiameter.Maximum);
                if (!string.IsNullOrWhiteSpace(map.EdgeSkipMode))
                    SetSelectedEdgeSkipMode(map.EdgeSkipMode);
                UpdateEdgeSkipModeUi();
                _nSideEdgeSkip.Value = ClampDecimal(map.SideEdgeSkip, _nSideEdgeSkip.Minimum, _nSideEdgeSkip.Maximum);
                _nTopBottomEdgeSkip.Value = ClampDecimal(map.TopBottomEdgeSkip, _nTopBottomEdgeSkip.Minimum, _nTopBottomEdgeSkip.Maximum);
                _mapSpecDirty = false;
            }
            finally
            {
                _suppressMapSpecEvents = false;
            }
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
                _currentMapWriteUtc = File.GetLastWriteTimeUtc(path);
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
                _project = _project ?? RecipeStore.LoadLastOrDefault();
                if (_project == null)
                    return;

                if (_map == null)
                {
                    CreateMapFromRecipeSpec(false);
                    if (_map == null)
                    {
                        QMC.Common.MessageDialog.Show(this, "저장할 Die Map이 없습니다.", "Die Map Create", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                else if (_mapSpecDirty)
                {
                    CreateMapFromRecipeSpec(false);
                    if (_map == null)
                        return;
                }

                SaveCurrentFrameSpecToRecipe();
                ApplyPickupSequence(_map, _isOutputMap);
                string path = !string.IsNullOrWhiteSpace(explicitPath)
                    ? explicitPath
                    : ResolveSaveMapPath();
                _map.FrameObjId = Path.GetFileNameWithoutExtension(path);
                DieMapGenerator.Save(_map, path);
                string recipeMapPath = SaveRecipeCompatibleMapFiles(path);

                UpdateRecipeMapFileName(recipeMapPath);
                RecipeStore.Save(_project);
                RecipeStore.SaveLastProjectName(_project.FileName);
                _currentMapPath = recipeMapPath;
                _currentMapWriteUtc = File.Exists(recipeMapPath) ? File.GetLastWriteTimeUtc(recipeMapPath) : DateTime.MinValue;
                bool appliedImmediately = TryApplyInputRecipeMapImmediatelyIfIdle(recipeMapPath);
                RefreshMapLibraryList();
                SelectLibraryPath(recipeMapPath);

                QMC.Common.Log.Write("Main", "RECIPE", "MapCreatePage",
                    "Die Map Recipe 연결 저장 완료. path=" + recipeMapPath +
                    ", originalPath=" + path +
                    ", immediateApply=" + appliedImmediately + " - Ok");

                string applyMessage = appliedImmediately
                    ? "현재 공정 중인 Input 웨이퍼가 없어 Active Input Die Map에도 즉시 적용했습니다."
                    : "현재 진행 중인 웨이퍼에는 적용하지 않습니다.\r\n다음 웨이퍼부터 적용됩니다.";

                QMC.Common.MessageDialog.Show(this,
                    "Die Map 저장 및 Recipe 연결 완료.\r\n" + applyMessage + "\r\n" + recipeMapPath,
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

                return !string.IsNullOrWhiteSpace(csvPath) && File.Exists(csvPath)
                    ? csvPath
                    : primaryPath;
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

        private bool TryApplyInputRecipeMapImmediatelyIfIdle(string recipeMapPath)
        {
            try
            {
                if (_mode != MapEditorMode.Input)
                    return false;

                if (HasCurrentInputProcessWafer())
                {
                    QMC.Common.Log.Write("Main", "RECIPE", "MapCreatePage",
                        "Input Die Map active apply deferred because current input wafer exists. path=" + recipeMapPath + " - Deferred");
                    return false;
                }

                DieMap applyMap = !string.IsNullOrWhiteSpace(recipeMapPath) && File.Exists(recipeMapPath)
                    ? DieMapGenerator.Load(recipeMapPath)
                    : null;
                if (applyMap == null)
                    applyMap = _map;
                if (applyMap == null)
                    return false;

                Form1 host = FindForm() as Form1;
                if (host != null && host.Controller != null)
                {
                    host.Controller.ApplyInputDieMap(applyMap, "MapCreatePage.SaveMapToRecipe.IdleInput");
                }
                else
                {
                    QMC.CDT320.Lots.LotStorage.ActiveInputDieMap = DieMapGenerator.Normalize(applyMap);
                }

                QMC.Common.Log.Write("Main", "RECIPE", "MapCreatePage",
                    "Input Die Map active apply completed because current input wafer is empty. path=" + recipeMapPath + " - Ok");
                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "RECIPE", "MapCreatePage",
                    "Input Die Map active apply failed: " + ex.Message + ". path=" + recipeMapPath + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private bool HasCurrentInputProcessWafer()
        {
            try
            {
                Form1 host = FindForm() as Form1;
                if (host != null && host.Machine != null)
                {
                    if (host.Machine.InputStageUnit != null && host.Machine.InputStageUnit.CurrentWaferMaterial != null)
                        return true;
                    if (host.Machine.InputFeederUnit != null && host.Machine.InputFeederUnit.CurrentWaferMaterial != null)
                        return true;
                }

                return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage) != null ||
                       MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder) != null;
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private void SaveCurrentFrameSpecToRecipe()
        {
            if (_project == null)
                return;

            TapeFrameSubset frame = BuildFrameFromControls();
            if (_isOutputMap)
                _project.OutputFrame = frame;
            else
                _project.InputFrame = frame;
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
