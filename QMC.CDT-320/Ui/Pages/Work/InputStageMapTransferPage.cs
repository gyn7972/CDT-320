using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using System.Linq;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Pages.WorkInfo;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class InputStageMapTransferPage : PageBase
    {
        private enum InputDieManualState
        {
            InspectionWait,
            InspectionGood,
            InspectionNg,
            PickSkip
        }

        private enum InputDieMapCellState
        {
            None = 0,
            InspectionWait = 1,
            InspectionDone = 2,
            PickComplete = 3
        }

        private struct InputDieMapStats
        {
            public int Target;
            public int Done;
            public int InspectionWait;
            public int InspectionDone;
            public int PickComplete;
            public int Good;
            public int Ng;
        }

        private struct DiePositionMoveDisplay
        {
            public double BaseX;
            public double BaseY;
            public double OffsetX;
            public double OffsetY;
            public double FinalX;
            public double FinalY;
        }

        private static readonly System.Drawing.Color InspectionWaitColor = System.Drawing.Color.FromArgb(0xCC, 0xDD, 0xEE);
        private static readonly System.Drawing.Color InspectionDoneColor = System.Drawing.Color.FromArgb(0xF2, 0xC1, 0x4E);
        private static readonly System.Drawing.Color PickCompleteColor = System.Drawing.Color.FromArgb(0x24, 0xB8, 0x6A);
        private static readonly System.Drawing.Color SkipColor = System.Drawing.Color.FromArgb(0x66, 0x66, 0x66);
        private static readonly object ManualDieDetectSimVisionRandomLock = new object();
        private static readonly Random ManualDieDetectSimVisionRandom = new Random();
        private const string ManualInputDieDetectFinderName = "DieFinder";
        private const int ManualInputDieDetectVisionIndex = 0;
        private const int ManualInputDieDetectVisionTimeoutMs = 5000;
        private const double ManualInputDieDetectPitchMm = 0.15;
        private static readonly PickerAxis[] PickerZAxes =
        {
            PickerAxis.PickerZ0,
            PickerAxis.PickerZ1,
            PickerAxis.PickerZ2,
            PickerAxis.PickerZ3
        };

        private Timer _refresh;
        private string _i18nTitle;
        private string _lastMapFrameObjId = "";
        private string _lastMapSignature = "";
        private DieMapEntry _selectedEntry;
        private bool _pickStatusDirty;
        private bool _suppressLotProgressOverlay;
        private Dictionary<string, InputDieMapCellState> _inputDieMapCellStates =
            new Dictionary<string, InputDieMapCellState>(StringComparer.Ordinal);
        private InputDieMapStats _inputDieMapStats;
        private ContextMenuStrip _gridMenu;
        private ToolStripMenuItem _gridMoveMenuItem;
        private ToolStripMenuItem[] _gridMoveFrontPickerMenuItems;
        private ToolStripMenuItem[] _gridMoveRearPickerMenuItems;
        private ToolStripMenuItem[] _gridPickUpTestFrontPickerMenuItems;
        private ToolStripMenuItem[] _gridPickUpTestRearPickerMenuItems;
        private ToolStripMenuItem[] _gridOffsetFrontPickerMenuItems;
        private ToolStripMenuItem[] _gridOffsetRearPickerMenuItems;
        private InputPickTargetSelectDialog _pickUpTestDialog;
        private bool _manualMoveBusy;
        private bool _manualDieDetectSentPositionValid;
        private string _manualDieDetectSentMapSignature = "";
        private string _manualDieDetectSentFrameObjId = "";
        private string _manualDieDetectSentDieUid = "";
        private int _manualDieDetectSentMapX;
        private int _manualDieDetectSentMapY;
        private double _manualDieDetectSentX;
        private double _manualDieDetectSentY;
        private bool _manualDieDetectOffsetPending;
        private string _manualDieDetectMapSignature = "";
        private string _manualDieDetectFrameObjId = "";
        private string _manualDieDetectDieUid = "";
        private int _manualDieDetectMapX;
        private int _manualDieDetectMapY;
        private double _manualDieDetectReferenceX;
        private double _manualDieDetectReferenceY;
        private double _manualDieDetectCurrentX;
        private double _manualDieDetectCurrentY;
        private double _manualDieDetectJogDeltaX;
        private double _manualDieDetectJogDeltaY;
        private double _manualDieDetectOffsetX;
        private double _manualDieDetectOffsetY;
        private double _manualDieDetectDetectedCenterX;
        private double _manualDieDetectDetectedCenterY;
        private double _manualDieDetectBottomRefVisionDeltaX;
        private double _manualDieDetectBottomRefVisionDeltaY;
        private double _manualDieDetectVisionDeltaX;
        private double _manualDieDetectVisionDeltaY;
        private double _manualDieDetectVisionDeltaT;
        private double _manualDieDetectCameraOffsetX;
        private double _manualDieDetectCameraOffsetY;
        private double _manualDieDetectBaseOriginX;
        private double _manualDieDetectBaseOriginY;

        public InputStageMapTransferPage() : this("work.page.inputMap")
        {
        }

        public InputStageMapTransferPage(string titleI18n)
        {
            _i18nTitle = titleI18n;
            InitializeComponent();
            ApplyTitle();
            InitializeMapDisplayStyle();
            WireEvents();

            if (!IsDesignerMode())
            {
                BuildOrFetchMap();
                _refresh = new Timer { Interval = 1500 };
                _refresh.Tick += (s, e) =>
                {
                    try
                    {
                        if (!ShouldRefreshVisible(this))
                        {
                            _refresh.Stop();
                            return;
                        }

                        RefreshActiveInputMapIfChanged();
                        ApplyLotProgress();
                    }
                    catch { }
                };
                if (ShouldRefreshVisible(this))
                    _refresh.Start();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            try
            {
                if (_refresh == null)
                    return;

                if (ShouldRefreshVisible(this))
                    _refresh.Start();
                else
                    _refresh.Stop();
            }
            catch { }
        }

        private void ApplyTitle()
        {
            lblHeader.Tag = "i18n:" + _i18nTitle;
            lblHeader.Text = Lang.T(_i18nTitle);
            mapView.Caption = Lang.T(_i18nTitle);
            lblProjectValue.Text = GetCurrentProjectName();
        }

        private void InitializeMapDisplayStyle()
        {
            try
            {
                mapView.BackColor = System.Drawing.Color.FromArgb(0xDD, 0xDD, 0xDD);
                mapView.ShowWaferOutline = true;
                mapView.CompactUsedBounds = true;
                mapView.EntryVisibilityPredicate = IsVisibleInputDieMapEntry;
                mapView.CellColorResolver = ResolveInputDieMapCellColor;
                mapView.CellStatusResolver = ResolveInputDieMapCellStatusText;
                mapView.LegendItemsResolver = BuildInputDieMapLegendItems;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void WireEvents()
        {
            BuildGridContextMenu();

            mapView.CellClicked += entry =>
            {
                if (entry == null) return;

                SelectEntry(entry);
                if (rbSelectPickStatus.Checked)
                    ToggleSelectedEntryTarget();
            };
            gridDieList.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0)
                    return;
                SelectEntryByGridRow(e.RowIndex);
            };
            gridDieList.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0)
                    return;
                SelectEntryByGridRow(e.RowIndex);
                if (rbSelectPickStatus.Checked)
                    ToggleSelectedEntryTarget();
            };
            gridDieList.CellMouseDown += OnGridDieListCellMouseDown;
            btnReloadActiveMap.Click += (s, e) => ReloadMapFromActiveOrRecipe();
            btnPickStatusSave.Click += (s, e) => SavePickStatus();
            btnApplyDieState.Click += (s, e) => ApplySelectedDieState();
            btnManualAlignComplete.Click += (s, e) => MarkManualAlignComplete();
            btnNeedleBlockDown.Click += (s, e) => ShowNotReadyAction("NEEDLE BLOCK DOWN", "Needle Block Down 단위동작 함수가 아직 연결되어 있지 않습니다.");
            btnThetaMatchMove.Click += (s, e) => ApplyManualInputStageThetaCorrection();
            btnXyMatchMove.Click += async (s, e) => await RunManualInputDieDetectAsync().ConfigureAwait(true);
            btnManualDieMapOffsetApply.Click += (s, e) => ApplyPendingManualInputDieMapOffset();
        }

        private string GetCurrentProjectName()
        {
            try
            {
                var lot = LotStorage.ActiveLot;
                if (lot != null && !string.IsNullOrEmpty(lot.RecipeName)) return lot.RecipeName;

                var list = RecipeStore.List();
                if (list != null && list.Count > 0)
                    return System.IO.Path.GetFileNameWithoutExtension(list[0]);
            }
            catch { }

            return "--";
        }

        private void BuildGridContextMenu()
        {
            try
            {
                _gridMoveMenuItem = new ToolStripMenuItem("MOVE VISION");
                _gridMoveMenuItem.Click += async (s, e) => await MoveSelectedDieAsync().ConfigureAwait(true);

                _gridMenu = new ContextMenuStrip();
                _gridMenu.Items.Add(_gridMoveMenuItem);
                _gridMenu.Items.Add(new ToolStripSeparator());
                _gridMenu.Items.Add(BuildPickerMoveMenu("MOVE FRONT PICKER", PickerSequenceSide.Front, out _gridMoveFrontPickerMenuItems));
                _gridMenu.Items.Add(BuildPickerMoveMenu("MOVE REAR PICKER", PickerSequenceSide.Rear, out _gridMoveRearPickerMenuItems));
                _gridMenu.Items.Add(new ToolStripSeparator());
                _gridMenu.Items.Add(BuildPickerPickUpTestMenu("PICKUP TEST FRONT PICKER", PickerSequenceSide.Front, out _gridPickUpTestFrontPickerMenuItems));
                _gridMenu.Items.Add(BuildPickerPickUpTestMenu("PICKUP TEST REAR PICKER", PickerSequenceSide.Rear, out _gridPickUpTestRearPickerMenuItems));
                _gridMenu.Items.Add(new ToolStripSeparator());
                _gridMenu.Items.Add(BuildPickerOffsetMenu("SET FRONT PICKER OFFSET", PickerSequenceSide.Front, out _gridOffsetFrontPickerMenuItems));
                _gridMenu.Items.Add(BuildPickerOffsetMenu("SET REAR PICKER OFFSET", PickerSequenceSide.Rear, out _gridOffsetRearPickerMenuItems));
                _gridMenu.Opening += (s, e) =>
                {
                    bool enabled = _selectedEntry != null && !_manualMoveBusy;
                    if (_gridMoveMenuItem != null)
                        _gridMoveMenuItem.Enabled = enabled;

                    SetPickerMoveMenuEnabled(_gridMoveFrontPickerMenuItems, enabled);
                    SetPickerMoveMenuEnabled(_gridMoveRearPickerMenuItems, enabled);
                    SetPickerMoveMenuEnabled(_gridPickUpTestFrontPickerMenuItems, enabled);
                    SetPickerMoveMenuEnabled(_gridPickUpTestRearPickerMenuItems, enabled);
                    SetPickerMoveMenuEnabled(_gridOffsetFrontPickerMenuItems, enabled);
                    SetPickerMoveMenuEnabled(_gridOffsetRearPickerMenuItems, enabled);
                };

                gridDieList.ContextMenuStrip = _gridMenu;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private ToolStripMenuItem BuildPickerMoveMenu(string title, PickerSequenceSide side, out ToolStripMenuItem[] items)
        {
            ToolStripMenuItem root = new ToolStripMenuItem(title);
            items = new ToolStripMenuItem[4];

            for (int i = 0; i < items.Length; i++)
            {
                int pickerNo = i + 1;
                ToolStripMenuItem item = new ToolStripMenuItem("PICKER #" + pickerNo);
                item.Click += async (s, e) => await MoveSelectedDieByPickerAsync(side, pickerNo).ConfigureAwait(true);
                items[i] = item;
                root.DropDownItems.Add(item);
            }

            return root;
        }

        private ToolStripMenuItem BuildPickerPickUpTestMenu(string title, PickerSequenceSide side, out ToolStripMenuItem[] items)
        {
            ToolStripMenuItem root = new ToolStripMenuItem(title);
            items = new ToolStripMenuItem[4];

            for (int i = 0; i < items.Length; i++)
            {
                int pickerNo = i + 1;
                ToolStripMenuItem item = new ToolStripMenuItem("PICKER #" + pickerNo);
                item.Click += (s, e) => ShowPickUpTestDialogForSelectedInputDie(side, pickerNo);
                items[i] = item;
                root.DropDownItems.Add(item);
            }

            return root;
        }

        private ToolStripMenuItem BuildPickerOffsetMenu(string title, PickerSequenceSide side, out ToolStripMenuItem[] items)
        {
            ToolStripMenuItem root = new ToolStripMenuItem(title);
            items = new ToolStripMenuItem[4];

            for (int i = 0; i < items.Length; i++)
            {
                int pickerNo = i + 1;
                ToolStripMenuItem item = new ToolStripMenuItem("PICKER #" + pickerNo);
                item.Click += (s, e) => ShowInputPickerOffsetSetupDialog(side, pickerNo);
                items[i] = item;
                root.DropDownItems.Add(item);
            }

            return root;
        }

        private static void SetPickerMoveMenuEnabled(ToolStripMenuItem[] items, bool enabled)
        {
            if (items == null)
                return;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                    items[i].Enabled = enabled;
            }
        }

        private void OnGridDieListCellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.Button != MouseButtons.Right)
                    return;

                gridDieList.ClearSelection();
                DataGridViewRow row = gridDieList.Rows[e.RowIndex];
                row.Selected = true;
                if (row.Cells.Count > 0)
                    gridDieList.CurrentCell = row.Cells[0];

                SelectEntryByGridRow(e.RowIndex);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ShowInputPickerOffsetSetupDialog(PickerSequenceSide side, int pickerNo)
        {
            try
            {
                DieMapEntry entry = _selectedEntry;
                if (entry == null)
                {
                    QMC.Common.MessageDialog.Show(this, "Offset을 설정할 다이가 선택되지 않았습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Form1 host = FindForm() as Form1;
                if (host == null || host.Machine == null)
                {
                    QMC.Common.MessageDialog.Show(this, "장비 정보를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var dialog = new InputPickerOffsetSetupDialog(
                    host.Machine,
                    side,
                    pickerNo,
                    entry.PosX,
                    entry.PosY))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;

                    host.SaveMachineSettings();

                    double effectiveX;
                    double effectiveY;
                    string effectiveText = "";
                    string needleTargetText = "";
                    if (TryResolvePickerInputOffsets(host, side, pickerNo, out effectiveX, out effectiveY))
                    {
                        effectiveText =
                            "\r\n실제 적용 Offset X=" + effectiveX.ToString("F3") +
                            " mm, Y=" + effectiveY.ToString("F3") + " mm";
                        PickCoordinateResult target = InputPickerPickTargetResolver.CalculateManualInputMapTarget(
                            host.Machine,
                            side,
                            pickerNo - 1,
                            entry.DieUid ?? "",
                            entry.PosX,
                            entry.PosY,
                            effectiveX,
                            effectiveY,
                            false);
                        double cameraOffsetX;
                        double cameraOffsetY;
                        InputPickerPickTargetResolver.TryResolveInputCameraToBottomOffsets(host.Machine, out cameraOffsetX, out cameraOffsetY);
                        needleTargetText =
                            "\r\nNeedleX 이동 목표 X=" + target.NeedleX.ToString("F3") +
                            " mm (Die VisionX=" + entry.PosX.ToString("F3") +
                            " - NeedleXToVisionXOffset=" +
                            InputPickerPickTargetResolver.ResolveNeedleCalibrationOffsetX(host.Machine).ToString("F3") + ")";
                        needleTargetText +=
                            "\r\nPicker 이동 목표 X=" + target.PickerX.ToString("F3") +
                            " mm, Y=" + target.PickerY.ToString("F3") +
                            " mm, StageY=" + target.StageY.ToString("F3") +
                            " mm (CameraOffset X=" + cameraOffsetX.ToString("F3") +
                            ", Y=" + cameraOffsetY.ToString("F3") +
                            " is included in InputVisionToPicker)";
                    }

                    QMC.Common.MessageDialog.Show(this,
                        ResolvePickerMoveTitle(side, pickerNo) + " InputVision -> Picker Offset 저장 완료\r\n" +
                        "InputVisionToPicker X=" + dialog.SavedOffsetX.ToString("F3") +
                        " mm, Y=" + dialog.SavedOffsetY.ToString("F3") + " mm" +
                        effectiveText +
                        needleTargetText +
                        "\r\n자동 PickUp과 수동 우클릭 이동은 같은 Setup 값을 사용합니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Input picker offset setup failed. side=" + side +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "Input Picker Offset 설정 실패:\r\n" + ex.Message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BuildOrFetchMap()
        {
            try
            {
                if (TryLoadActiveInputMap())
                    return;

                var list = RecipeStore.List();
                if (list == null || list.Count == 0) return;

                var recipe = RecipeStore.Load(list[0]);
                if (recipe?.Frame == null) return;

                DieMap recipeMap = LoadRecipeInputMapFromFile(recipe);
                if (recipeMap != null)
                {
                    ApplyMap(recipeMap, "RECIPE INPUT DIE MAP");
                    _pickStatusDirty = false;
                    lblChipW.Text = (recipe.Die != null ? recipe.Die.WidthMm : 1.0).ToString("F3");
                    lblChipH.Text = (recipe.Die != null ? recipe.Die.HeightMm : 1.0).ToString("F3");
                    lblPitchX.Text = recipeMap.PitchX.ToString("F3");
                    lblPitchY.Text = recipeMap.PitchY.ToString("F3");
                    lblWaferDia.Text = recipeMap.OuterDiameterMm > 0.0
                        ? recipeMap.OuterDiameterMm.ToString("F0")
                        : recipe.Frame.OuterDiameterMm.ToString("F0");
                    lblBarcodeValue.Text = "--";
                    lblProjectValue.Text = GetCurrentProjectName();
                    return;
                }

                DieMap preview = CreateInputCircleMapFromRecipe(recipe);
                ApplyMap(preview, "RECIPE INPUT CIRCLE DIE MAP");
                _pickStatusDirty = false;
                lblChipW.Text = (recipe.Die != null ? recipe.Die.WidthMm : 1.0).ToString("F3");
                lblChipH.Text = (recipe.Die != null ? recipe.Die.HeightMm : 1.0).ToString("F3");
                lblPitchX.Text = recipe.Frame.PitchX.ToString("F3");
                lblPitchY.Text = recipe.Frame.PitchY.ToString("F3");
                lblWaferDia.Text = recipe.Frame.OuterDiameterMm.ToString("F0");
                lblBarcodeValue.Text = "--";
                lblBinValue.Text = "0";
                lblDieNum.Text = "0 / " + (mapView.Map != null ? mapView.Map.TotalCells.ToString() : "0");
                lblProjectValue.Text = GetCurrentProjectName();
            }
            catch { }
        }

        private DieMap LoadRecipeInputMapFromFile(RecipeProject recipe)
        {
            try
            {
                if (recipe == null)
                    return null;

                string path;
                string reason;
                // 현재 기준: 전환 화면 preview도 Process Test/Die Mapping과 같은 외부맵 로더를 사용한다.
                DieMap map = RecipeDieMapResolver.LoadCompatibleMap(recipe, RecipeMapKind.Input, out path, out reason);
                if (map == null || map.Entries == null || map.Entries.Count == 0)
                    return null;

                // 현재 기준: 전환 화면 preview도 레시피 웨이퍼맵 X/Y 원본 인덱스를 그대로 사용한다.
                PickupSequenceGenerator.ApplySequenceNumbers(map, recipe.InputPickup ?? recipe.Pickup ?? new PickupSubset());
                return DieMapGenerator.Normalize(map);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Recipe input die map preview load failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private DieMap CreateInputCircleMapFromRecipe(RecipeProject recipe)
        {
            if (recipe == null || recipe.Frame == null)
                return null;

            int gridX = Math.Max(1, recipe.Frame.DieMapX);
            int gridY = Math.Max(1, recipe.Frame.DieMapY);
            double pitchX = recipe.Frame.PitchX > 0.0 ? recipe.Frame.PitchX : 1.0;
            double pitchY = recipe.Frame.PitchY > 0.0 ? recipe.Frame.PitchY : 1.0;
            double originX = -((gridX - 1) * pitchX) / 2.0;
            double originY = -((gridY - 1) * pitchY) / 2.0;
            int sideEdgeSkip = Math.Max(0, recipe.Frame.SideEdgeSkip);
            int topBottomEdgeSkip = Math.Max(0, recipe.Frame.TopBottomEdgeSkip);
            double diameterMm = recipe.Frame.OuterDiameterMm > 0.0 ? recipe.Frame.OuterDiameterMm : 0.0;

            var map = new DieMap
            {
                FrameObjId = string.IsNullOrWhiteSpace(recipe.Frame.FrameSpecName) ? "INPUT_CIRCLE" : recipe.Frame.FrameSpecName,
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
                    bool target = IsInsideInputCircle(col, row, gridX, gridY, sideEdgeSkip, topBottomEdgeSkip, x, y, pitchX, pitchY, diameterMm);
                    if (!target)
                        continue;

                    map.Entries.Add(new DieMapEntry
                    {
                        Index = index++,
                        DieMapX = col,
                        DieMapY = row,
                        IsTarget = true,
                        Result = DieResult.Unknown,
                        BinCode = 0,
                        PosX = x,
                        PosY = y
                    });
                }
            }

            PickupSequenceGenerator.ApplySequenceNumbers(map, recipe.InputPickup ?? recipe.Pickup ?? new PickupSubset());
            return DieMapGenerator.Normalize(map);
        }

        private static bool IsInsideInputCircle(
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
            if ((nx * nx) + (ny * ny) > 1.0)
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

        private bool TryLoadActiveInputMap()
        {
            try
            {
                if (!IsInputMapPage())
                    return false;

                RestoreInputStageRuntimeFromSavedMaterial();

                DieMap mappedWaferMap = null;
                WaferMaterial stageWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (stageWafer != null && stageWafer.HasInputStageDieMappingResult)
                    mappedWaferMap = MaterialStateService.BuildDieMapFromWafer(stageWafer);

                var map = mappedWaferMap ?? LotStorage.ActiveInputDieMap;
                if (map == null)
                {
                    map = MaterialStateService.BuildInputDieMapFromStageWafer();
                    if (map != null)
                    {
                        LotStorage.ActiveInputDieMap = map;
                        var host = FindForm() as Form1;
                        if (host != null && host.Controller != null)
                        {
                            host.Controller.PickupOptions = ResolveInputPickupSubsetFromRecipe();
                            host.Controller.ApplyInputDieMap(map, "InputStageMapTransferPage.RestoreSavedInputDieMap");
                        }
                    }
                }

                if (map == null)
                    return false;

                if (mappedWaferMap != null)
                {
                    LotStorage.ActiveInputDieMap = mappedWaferMap;
                    var host = FindForm() as Form1;
                    if (host != null && host.Controller != null)
                    {
                        host.Controller.PickupOptions = ResolveInputPickupSubsetFromRecipe();
                        host.Controller.ApplyInputDieMap(mappedWaferMap, "InputStageMapTransferPage.RestoreMappedWaferDieMap");
                    }
                }

                WriteMapLoadLog("InitialLoad", map, mappedWaferMap != null ? "MappedWafer" : "ActiveOrSaved");
                ApplyMap(map, "ACTIVE INPUT DIE MAP");
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

        private static PickupSubset ResolveInputPickupSubsetFromRecipe()
        {
            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project == null)
                    return new PickupSubset();

                return project.InputPickup ?? project.Pickup ?? new PickupSubset();
            }
            catch
            {
                return new PickupSubset();
            }
            finally
            {
            }
        }

        private void RestoreInputStageRuntimeFromSavedMaterial()
        {
            try
            {
                var host = FindForm() as Form1;
                var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                if (stage == null)
                    return;

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer == null)
                    return;

                stage.SetCurrentWaferMaterial(wafer);

                if (wafer.HasInputStageAlignResult)
                {
                    stage.ApplyWaferAlignResult(
                        wafer.InputStageAlignOriginX,
                        wafer.InputStageAlignOriginY,
                        wafer.InputStageAlignPitchX,
                        wafer.InputStageAlignPitchY,
                        wafer.InputStageAlignOffsetX,
                        wafer.InputStageAlignOffsetY);
                }

                if (wafer.HasInputStageThetaAlignResult)
                {
                    stage.ApplyWaferAlignThetaResult(
                        wafer.InputStageAlignReferenceT,
                        wafer.InputStageAlignCorrectedT,
                        wafer.InputStageAlignOffsetT);
                }

                WaferMapData waferMap = MaterialStateService.BuildWaferMapDataFromWafer(wafer);
                DieMap dieMap = MaterialStateService.BuildDieMapFromWafer(wafer);
                if (waferMap != null && dieMap != null)
                    stage.ApplyDieMappingResult(
                        waferMap,
                        dieMap.OriginX,
                        dieMap.OriginY,
                        dieMap.PitchX,
                        dieMap.PitchY,
                        wafer.InputStageDieMappingOffsetX,
                        wafer.InputStageDieMappingOffsetY);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Input stage saved material restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void RefreshActiveInputMapIfChanged()
        {
            try
            {
                if (_pickStatusDirty)
                    return;
                if (!IsInputMapPage())
                    return;

                DieMap mappedWaferMap = null;
                WaferMaterial stageWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (stageWafer != null && stageWafer.HasInputStageDieMappingResult)
                    mappedWaferMap = MaterialStateService.BuildDieMapFromWafer(stageWafer);

                var active = mappedWaferMap ?? LotStorage.ActiveInputDieMap;
                if (active == null)
                    return;

                string signature = BuildMapSignature(active);
                if (mappedWaferMap != null &&
                    !string.Equals(_lastMapSignature, signature, StringComparison.Ordinal))
                {
                    LotStorage.ActiveInputDieMap = mappedWaferMap;
                }

                string frameId = active.FrameObjId ?? "";
                if (!string.Equals(_lastMapSignature, signature, StringComparison.Ordinal) ||
                    !string.Equals(_lastMapFrameObjId, frameId, StringComparison.Ordinal))
                {
                    WriteMapLoadLog("Refresh", active, mappedWaferMap != null ? "MappedWafer" : "Active");
                    ApplyMap(active, "ACTIVE INPUT DIE MAP");
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void WriteMapLoadLog(string phase, DieMap map, string source)
        {
            try
            {
                int entries = map != null && map.Entries != null ? map.Entries.Count : 0;
                int targets = 0;
                if (map != null && map.Entries != null)
                {
                    foreach (DieMapEntry entry in map.Entries)
                    {
                        if (entry != null && entry.IsTarget)
                            targets++;
                    }
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Input die map " + phase +
                    ". source=" + (source ?? "") +
                    ", frame=" + (map != null ? map.FrameObjId ?? "" : "") +
                    ", grid=" + (map != null ? map.DieMapX.ToString() : "0") + "x" + (map != null ? map.DieMapY.ToString() : "0") +
                    ", totalCells=" + (map != null ? map.TotalCells.ToString() : "0") +
                    ", entries=" + entries.ToString() +
                    ", targets=" + targets.ToString() +
                    " - Ok");
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ApplyMap(DieMap map, string title)
        {
            try
            {
                DieMapGenerator.Normalize(map);
                DieMapEntry previousSelection = _selectedEntry;
                string signature = BuildMapSignature(map);

                mapView.Caption = title;
                mapView.Map = map;
                _lastMapFrameObjId = map != null ? map.FrameObjId ?? "" : "";
                _lastMapSignature = signature;
                _pickStatusDirty = false;
                _suppressLotProgressOverlay = false;
                _selectedEntry = FindEquivalentEntry(map, previousSelection);
                ClearPendingManualInputDieDetectOffset();

                lblMapTitle.Text = title;
                lblProjectValue.Text = GetCurrentProjectName();
                lblBarcodeValue.Text = ResolveActiveWaferId();
                lblBinValue.Text = "0";
                lblPitchX.Text = map != null ? map.PitchX.ToString("F3") : "0";
                lblPitchY.Text = map != null ? map.PitchY.ToString("F3") : "0";
                lblDieNum.Text = "0 / " + CountVisibleInputDieMapEntries(map).ToString();
                ApplySpecInfoFromRecipe();
                RefreshDieGrid();
                if (_selectedEntry != null)
                {
                    SelectEntry(_selectedEntry);
                    mapView.Invalidate();
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private string BuildMapSignature(DieMap map)
        {
            try
            {
                if (map == null || map.Entries == null)
                    return "";

                long targetCount = 0;
                long sequenceSum = 0;
                long statusHash = 17;
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null)
                        continue;

                    if (entry.IsTarget)
                        targetCount++;

                    sequenceSum += entry.SequenceNo;
                    int mapX = ResolveEntryMapX(entry);
                    int mapY = ResolveEntryMapY(entry);
                    statusHash = statusHash * 31 +
                        entry.Index * 3 +
                        mapX * 5 +
                        mapY * 7 +
                        (entry.IsTarget ? 11 : 13) +
                        ((int)entry.Result * 17) +
                        entry.BinCode * 19 +
                        entry.SequenceNo * 23 +
                        StableHash(MaterialStateService.ResolveInputDieDisplayState(entry));
                }

                return (map.FrameObjId ?? "") + "|" +
                    map.DieMapX.ToString() + "|" +
                    map.DieMapY.ToString() + "|" +
                    map.PitchX.ToString("F6") + "|" +
                    map.PitchY.ToString("F6") + "|" +
                    map.OriginX.ToString("F6") + "|" +
                    map.OriginY.ToString("F6") + "|" +
                    map.Entries.Count.ToString() + "|" +
                    targetCount.ToString() + "|" +
                    sequenceSum.ToString() + "|" +
                    statusHash.ToString();
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
            finally
            {
            }
        }

        private static int StableHash(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                    return 0;

                unchecked
                {
                    int hash = 23;
                    for (int i = 0; i < value.Length; i++)
                        hash = hash * 31 + value[i];
                    return hash;
                }
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
        }

        private DieMapEntry FindEquivalentEntry(DieMap map, DieMapEntry entry)
        {
            try
            {
                if (map == null || map.Entries == null || entry == null)
                    return null;

                if (!string.IsNullOrWhiteSpace(entry.DieUid))
                {
                    foreach (DieMapEntry candidate in map.Entries)
                    {
                        if (candidate != null &&
                            string.Equals(candidate.DieUid, entry.DieUid, StringComparison.OrdinalIgnoreCase))
                            return candidate;
                    }
                }

                return map.GetCell(ResolveEntryMapX(entry), ResolveEntryMapY(entry));
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private void ApplySpecInfoFromRecipe()
        {
            try
            {
                var list = RecipeStore.List();
                if (list == null || list.Count == 0)
                    return;

                var recipe = RecipeStore.Load(list[0]);
                if (recipe == null)
                    return;

                lblChipW.Text = (recipe.Die != null ? recipe.Die.WidthMm : 1.0).ToString("F3");
                lblChipH.Text = (recipe.Die != null ? recipe.Die.HeightMm : 1.0).ToString("F3");
                lblWaferDia.Text = (recipe.Frame != null ? recipe.Frame.OuterDiameterMm : 0.0).ToString("F0");
            }
            catch
            {
            }
            finally
            {
            }
        }

        private string ResolveActiveWaferId()
        {
            try
            {
                var host = FindForm() as Form1;
                var wafer = host != null && host.Machine != null
                    ? MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage)
                    : null;
                if (wafer != null && !string.IsNullOrWhiteSpace(wafer.WaferId))
                    return wafer.WaferId;

                var map = LotStorage.ActiveInputDieMap;
                return map != null && !string.IsNullOrWhiteSpace(map.FrameObjId) ? map.FrameObjId : "--";
            }
            catch
            {
                return "--";
            }
            finally
            {
            }
        }

        private bool IsInputMapPage()
        {
            return string.Equals(_i18nTitle, "work.page.inputMap", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyLotProgress()
        {
            if (_pickStatusDirty || _suppressLotProgressOverlay)
                return;
            var map = mapView?.Map;
            if (map == null) return;

            var lot = LotStorage.ActiveLot;
            if (lot == null)
            {
                mapView.Invalidate();
                return;
            }

            int processed = lot.ProcessedDies;
            int good = lot.GoodCount;
            int filled = 0;
            int goodFilled = 0;
            foreach (var entry in BuildDisplayEntries(map))
            {
                if (entry == null || !entry.IsTarget)
                    continue;

                if (filled < processed)
                {
                    if (goodFilled < good)
                    {
                        entry.Result = DieResult.Good;
                        entry.BinCode = QMC.CDT320.Bin.BinCodeMap.GoodBin;
                        goodFilled++;
                    }
                    else
                    {
                        entry.Result = DieResult.NG;
                        entry.BinCode = 110;
                    }

                    filled++;
                }
                else
                {
                    entry.Result = DieResult.Unknown;
                    entry.BinCode = 0;
                }
            }

            lblDieNum.Text = processed + " / " + CountVisibleInputDieMapEntries(map);
            RefreshDieGrid();
            mapView.Invalidate();
        }

        private void ReloadMapFromActiveOrRecipe()
        {
            try
            {
                if (_pickStatusDirty)
                {
                    DialogResult result = QMC.Common.MessageDialog.Show(this,
                        "저장하지 않은 Pick Status 변경이 있습니다.\r\n다시 불러오시겠습니까?",
                        "Input Die Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result != DialogResult.Yes)
                        return;
                }

                _pickStatusDirty = false;
                BuildOrFetchMap();
                RefreshDieGrid();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Input Die Map reload failed:\r\n" + ex.Message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void SavePickStatus()
        {
            try
            {
                DieMap map = mapView != null ? mapView.Map : null;
                if (map == null)
                {
                    QMC.Common.MessageDialog.Show(this, "저장할 Input Die Map 데이터가 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult result = QMC.Common.MessageDialog.Show(this,
                    "현재 Pick Status를 저장하고 Pickup Sequence를 갱신하시겠습니까?",
                    "Input Die Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    return;

                LotStorage.ActiveInputDieMap = map;
                PersistPickStatusToMaterialState(map);
                var host = FindForm() as Form1;
                if (host != null && host.Controller != null)
                    host.Controller.ApplyInputDieMap(map, "InputStageMapTransferPage.SavePickStatus");

                _pickStatusDirty = false;
                RefreshDieGrid();
                mapView.Invalidate();
                QMC.Common.MessageDialog.Show(this, "Pick Status 저장 완료.",
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Pick Status save failed:\r\n" + ex.Message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void PersistPickStatusToMaterialState(
            DieMap map,
            double? mappingOffsetX = null,
            double? mappingOffsetY = null,
            string saveReason = "MapTransferPickStatusSave")
        {
            try
            {
                if (map == null || map.Entries == null)
                    return;

                DieMapGenerator.Normalize(map);
                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer != null)
                {
                    wafer.DieMapFrameObjId = map.FrameObjId ?? "";
                    wafer.HasInputStageAlignResult = true;
                    wafer.InputStageAlignOriginX = map.OriginX;
                    wafer.InputStageAlignOriginY = map.OriginY;
                    wafer.InputStageAlignPitchX = map.PitchX;
                    wafer.InputStageAlignPitchY = map.PitchY;
                    wafer.HasInputStageDieMappingResult = true;
                    if (mappingOffsetX.HasValue)
                        wafer.InputStageDieMappingOffsetX = mappingOffsetX.Value;
                    if (mappingOffsetY.HasValue)
                        wafer.InputStageDieMappingOffsetY = mappingOffsetY.Value;
                    wafer.UpdatedAt = DateTime.Now;
                    if (wafer.DieIds == null)
                        wafer.DieIds = new System.Collections.Generic.List<string>();
                    else
                        wafer.DieIds.Clear();
                }

                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null)
                        continue;

                    int mapX = ResolveEntryMapX(entry);
                    int mapY = ResolveEntryMapY(entry);
                    if (string.IsNullOrWhiteSpace(entry.DieUid))
                        entry.DieUid = "INPUT-D" + mapY.ToString("000") + "-" + mapX.ToString("000");

                    DieMaterial die = MaterialStateService.GetOrCreateDieMaterial(entry.DieUid);
                    if (wafer != null)
                    {
                        die.WaferID_Input = wafer.WaferId;
                        if (!wafer.DieIds.Contains(die.DieId))
                            wafer.DieIds.Add(die.DieId);
                    }

                    // 현재 기준: DieMaterial에는 웨이퍼맵 원본 X/Y 인덱스를 저장한다.
                    die.Wafer_IndexX = mapX;
                    die.Wafer_IndexY = mapY;
                    die.InputSequenceNo = entry.SequenceNo;
                    die.Input_BinCode = entry.BinCode;
                    die.IsInputTarget = entry.IsTarget;
                    if (!entry.IsTarget)
                        die.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.Unknown };
                    else if (die.CurrentLocation == null || die.CurrentLocation.Kind == MaterialLocationKind.Unknown)
                        die.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage };
                    die.Result = entry.IsTarget ? entry.Result : DieResult.NG;
                    if (die.WaferOffset == null)
                        die.WaferOffset = new VisionOffset();
                    die.WaferOffset.X = entry.PosX;
                    die.WaferOffset.Y = entry.PosY;
                    die.WaferOffset.R = 0.0;
                    die.WaferOffset.IsValid = true;
                    die.UpdatedAt = DateTime.Now;
                }

                MaterialStateService.NotifyAndSave(string.IsNullOrWhiteSpace(saveReason)
                    ? "MapTransferPickStatusSave"
                    : saveReason);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Pick status material save failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private async Task RunInputStageSequenceActionAsync(string actionName, Func<Form1, Task<int>> action)
        {
            IDisposable actionScope = null;
            try
            {
                var host = FindForm() as Form1;
                if (host == null || host.Controller == null || host.Machine == null || host.Machine.InputStageUnit == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage 장비 정보를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    actionName + " 동작을 진행하시겠습니까?",
                    "Input Die Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                SetActionButtonsEnabled(false);
                actionScope = host.Controller.BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "InputStageMapTransferPage:" + actionName);
                int result = await action(host).ConfigureAwait(true);
                RefreshActiveInputMapIfChanged();
                RefreshDieGrid();
                mapView.Invalidate();

                if (result == 0)
                {
                    QMC.Common.MessageDialog.Show(this, actionName + " 완료.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                QMC.Common.MessageDialog.Show(this,
                    actionName + " 실패\r\nresult=" + result + "\r\nAlarm/Event Log를 확인하세요.",
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    actionName + " failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, actionName + " 실패:\r\n" + ex.Message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (actionScope != null)
                    actionScope.Dispose();
                SetActionButtonsEnabled(true);
            }
        }

        private async Task RunManualInputDieDetectAsync()
        {
            IDisposable actionScope = null;
            try
            {
                DieMap map = mapView != null ? mapView.Map : null;
                if (map == null || map.Entries == null || map.Entries.Count == 0)
                {
                    QMC.Common.MessageDialog.Show(this, "수동 다이 검출을 적용할 Active Input Die Map 데이터가 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DieMapEntry entry = FindEquivalentEntry(map, _selectedEntry);
                if (entry == null)
                {
                    QMC.Common.MessageDialog.Show(this, "수동 다이 검출 기준 Die를 먼저 선택하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Form1 host = FindForm() as Form1;
                if (host == null || host.Controller == null || host.Machine == null || host.Machine.InputStageUnit == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage 장비 정보를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                InputStageUnit stage = host.Machine.InputStageUnit;
                if (stage.Vision == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage Vision 정보를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (stage.CameraX == null || stage.StageY == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage Vision X 또는 Stage Y 축 정보를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                double sentDieX;
                double sentDieY;
                string sentPositionSource;
                TryResolveManualInputDieDetectSentPosition(map, entry, out sentDieX, out sentDieY, out sentPositionSource);

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                string materialReason;
                if (!MaterialStateService.IsInputStageThetaAlignComplete(wafer, out materialReason))
                {
                    QMC.Common.MessageDialog.Show(this,
                        "T 보정 완료 상태가 아니라 다이 검출을 진행할 수 없습니다.\r\n" + materialReason,
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    "현재 Vision 화면에서 선택 Die 중심을 검출하시겠습니까?\r\n" +
                    "Die=" + BuildSelectedDieText(entry) + "\r\n" +
                    "보낸 Die 위치(" + sentPositionSource + ") X=" + sentDieX.ToString("F3") +
                    " mm, Y=" + sentDieY.ToString("F3") + " mm\r\n" +
                    "현재 Vision 위치 X=" + stage.CameraX.ActualPosition.ToString("F3") +
                    " mm, StageY=" + stage.StageY.ActualPosition.ToString("F3") + " mm",
                    "Input Die Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                RestoreInputStageRuntimeFromSavedMaterial();

                SetActionButtonsEnabled(false);
                _manualMoveBusy = true;
                actionScope = host.Controller.BeginManualActionScope(
                    ManualMotionScopeKind.ProcessSequence,
                    "InputStageMapTransferPage:ManualInputDieDetect");

                int prepareResult = await AwaitManualMoveStepAsync(
                    MovePickersToAvoidForVisionMoveAsync(host, JogSpeedType.Fine),
                    ResolveManualMoveTimeoutMs(host),
                    "다이 검출 전 Picker Avoid 준비",
                    () => StopManualMapMove(host, "Manual die detect picker avoid timeout")).ConfigureAwait(true);
                if (prepareResult != 0)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "다이 검출 전 Picker Avoid 준비 실패\r\nresult=" + prepareResult +
                        "\r\nAlarm/Event Log를 확인하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int thetaPrepareResult = await EnsureEjectPinZAvoidForThetaMoveAsync(stage).ConfigureAwait(true);
                if (thetaPrepareResult != 0)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "다이 검출 전 EjectPinZ Avoid 준비 실패\r\nresult=" + thetaPrepareResult +
                        "\r\nAlarm/Event Log를 확인하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int thetaResult = await EnsureManualInputStageThetaPositionAsync(stage).ConfigureAwait(true);
                if (thetaResult != 0)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "다이 검출 전 StageT 보정 위치 확인/이동 실패\r\nresult=" + thetaResult +
                        "\r\nAlarm/Event Log를 확인하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                double baseOriginX = stage.OriginX;
                double baseOriginY = stage.OriginY;
                double currentVisionX = stage.CameraX.ActualPosition;
                double currentStageY = stage.StageY.ActualPosition;
                double jogDeltaX = currentVisionX - sentDieX;
                double jogDeltaY = currentStageY - sentDieY;
                bool simulationOrDryRun = IsManualInputDieDetectSimulationOrDryRun(stage);

                VisionAlignResult vision = await RequestManualInputDieDetectVisionAsync(
                    stage,
                    entry,
                    currentVisionX,
                    currentStageY).ConfigureAwait(true);
                if (!IsValidVisionAlignResult(vision))
                {
                    QMC.Common.MessageDialog.Show(this,
                        "InputPickDie 비전 검출 결과가 유효하지 않습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                double bottomRefVisionDeltaX = vision.DeltaX;
                double bottomRefVisionDeltaY = vision.DeltaY;
                double cameraOffsetX = 0.0;
                double cameraOffsetY = 0.0;
                bool cameraOffsetXExcluded = !simulationOrDryRun &&
                    InputPickerPickTargetResolver.TryResolveInputCameraToBottomOffsets(
                        host.Machine,
                        out cameraOffsetX,
                        out cameraOffsetY);
                double centerMoveDeltaX = bottomRefVisionDeltaX;
                double centerMoveDeltaY = bottomRefVisionDeltaY;
                if (cameraOffsetXExcluded)
                {
                    centerMoveDeltaX -= cameraOffsetX;
                }

                VisionAlignResult centerMoveVision = new VisionAlignResult
                {
                    DeltaX = centerMoveDeltaX,
                    DeltaY = centerMoveDeltaY,
                    DeltaTheta = vision.DeltaTheta,
                    PitchX = vision.PitchX,
                    PitchY = vision.PitchY
                };

                double detectedCenterX = currentVisionX + centerMoveDeltaX;
                double detectedCenterY = currentStageY + centerMoveDeltaY;
                double offsetX = detectedCenterX - sentDieX;
                double offsetY = detectedCenterY - sentDieY;

                string limitReason;
                if (!stage.IsManualDieDetectOffsetWithinLimit(offsetX, offsetY, out limitReason))
                {
                    QMC.Common.MessageDialog.Show(this,
                        "다이 검출 Offset이 허용 Limit을 초과했습니다.\r\n" + limitReason,
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int centerMoveResult = await AwaitManualMoveStepAsync(
                    stage.MoveVisionPointSafelyAsync(
                        detectedCenterX,
                        detectedCenterY,
                        JogSpeedType.Fine,
                        0.0,
                        "InputStageMapTransferPage.ManualInputDieDetectCenterMove"),
                    ResolveManualMoveTimeoutMs(host),
                    "검출 다이 중심 Vision 좌표 이동",
                    () => StopManualMapMove(host, "Manual die detect center move timeout")).ConfigureAwait(true);
                if (centerMoveResult != 0)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "검출 다이 중심 좌표 이동 실패\r\nresult=" + centerMoveResult +
                        "\r\nAlarm/Event Log를 확인하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                StorePendingManualInputDieDetectOffset(
                    map,
                    entry,
                    offsetX,
                    offsetY,
                    sentDieX,
                    sentDieY,
                    currentVisionX,
                    currentStageY,
                    jogDeltaX,
                    jogDeltaY,
                    detectedCenterX,
                    detectedCenterY,
                    centerMoveVision,
                    bottomRefVisionDeltaX,
                    bottomRefVisionDeltaY,
                    cameraOffsetX,
                    cameraOffsetY,
                    baseOriginX,
                    baseOriginY);

                _selectedEntry = entry;
                RefreshDieGrid();
                SelectEntry(entry);
                mapView.Invalidate();

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual input die detect completed. die=" + (entry.DieUid ?? "") +
                    ", sentX=" + sentDieX.ToString("F6") +
                    ", sentY=" + sentDieY.ToString("F6") +
                    ", currentX=" + currentVisionX.ToString("F6") +
                    ", currentY=" + currentStageY.ToString("F6") +
                    ", jogDeltaX=" + jogDeltaX.ToString("F6") +
                    ", jogDeltaY=" + jogDeltaY.ToString("F6") +
                    ", bottomRefVisionDeltaX=" + bottomRefVisionDeltaX.ToString("F6") +
                    ", inputVisionDeltaY=" + bottomRefVisionDeltaY.ToString("F6") +
                    ", cameraOffsetXExcluded=" + cameraOffsetXExcluded +
                    ", cameraOffsetX=" + cameraOffsetX.ToString("F6") +
                    ", cameraOffsetY=" + cameraOffsetY.ToString("F6") + "(notUsedForDieY)" +
                    ", centerMoveDeltaX=" + centerMoveDeltaX.ToString("F6") +
                    ", centerMoveDeltaY=" + centerMoveDeltaY.ToString("F6") +
                    ", offsetX=" + offsetX.ToString("F6") +
                    ", offsetY=" + offsetY.ToString("F6") + " - Ok");

                QMC.Common.MessageDialog.Show(this,
                    "다이 검출 완료.\r\n" +
                    "보낸 Die 위치 X=" + sentDieX.ToString("F3") + " mm, Y=" + sentDieY.ToString("F3") + " mm\r\n" +
                    "현재 Jog 위치 X=" + currentVisionX.ToString("F3") + " mm, Y=" + currentStageY.ToString("F3") + " mm\r\n" +
                    "Jog 이동량 X=" + jogDeltaX.ToString("F6") + " mm, Y=" + jogDeltaY.ToString("F6") + " mm\r\n" +
                    "Vision Delta X(보정 포함)=" + bottomRefVisionDeltaX.ToString("F6") + " mm, Y(Input only)=" + bottomRefVisionDeltaY.ToString("F6") + " mm\r\n" +
                    "Camera Offset X=" + cameraOffsetX.ToString("F6") + (cameraOffsetXExcluded ? " mm (X 센터 이동에서 제외), " : " mm (X 미적용), ") +
                    "Y=" + cameraOffsetY.ToString("F6") + " mm (Die Y 계산 미사용)\r\n" +
                    "Center Move Delta X=" + centerMoveDeltaX.ToString("F6") + " mm, Y=" + centerMoveDeltaY.ToString("F6") + " mm\r\n" +
                    "Vision Delta T=" + vision.DeltaTheta.ToString("F6") + " deg (T 보정 미적용)\r\n" +
                    "Detected Center X=" + detectedCenterX.ToString("F3") + " mm, Y=" + detectedCenterY.ToString("F3") + " mm\r\n" +
                    "Map Offset X=" + offsetX.ToString("F6") + " mm, Y=" + offsetY.ToString("F6") + " mm\r\n" +
                    "(Map Offset = Jog 이동량 + Center Move Delta)\r\n\r\n" +
                    "[Offset 적용] 버튼을 누르면 전체 Die Map에 적용됩니다.",
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual input die detect failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "다이 검출 실패:\r\n" + ex.Message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (actionScope != null)
                        actionScope.Dispose();
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "다이 검출 스코프 정리 중 오류: " + ex.Message + " - Failed");
                }
                finally
                {
                    _manualMoveBusy = false;
                    SetActionButtonsEnabled(true);
                }
            }
        }

        private void StoreManualInputDieDetectSentPosition(DieMap map, DieMapEntry entry, double sentX, double sentY)
        {
            try
            {
                _manualDieDetectSentPositionValid = entry != null && IsFinite(sentX) && IsFinite(sentY);
                _manualDieDetectSentMapSignature = BuildMapSignature(map);
                _manualDieDetectSentFrameObjId = map != null ? map.FrameObjId ?? "" : "";
                _manualDieDetectSentDieUid = entry != null ? entry.DieUid ?? "" : "";
                _manualDieDetectSentMapX = ResolveEntryMapX(entry);
                _manualDieDetectSentMapY = ResolveEntryMapY(entry);
                _manualDieDetectSentX = sentX;
                _manualDieDetectSentY = sentY;
            }
            catch
            {
                _manualDieDetectSentPositionValid = false;
                _manualDieDetectSentMapSignature = "";
                _manualDieDetectSentFrameObjId = "";
                _manualDieDetectSentDieUid = "";
                _manualDieDetectSentMapX = 0;
                _manualDieDetectSentMapY = 0;
                _manualDieDetectSentX = 0.0;
                _manualDieDetectSentY = 0.0;
            }
            finally
            {
            }
        }

        private bool TryResolveManualInputDieDetectSentPosition(
            DieMap map,
            DieMapEntry entry,
            out double sentX,
            out double sentY,
            out string source)
        {
            sentX = entry != null ? entry.PosX : 0.0;
            sentY = entry != null ? entry.PosY : 0.0;
            source = "현재 선택 Die 위치";

            try
            {
                if (!_manualDieDetectSentPositionValid || map == null || entry == null)
                    return false;

                if (!string.Equals(BuildMapSignature(map), _manualDieDetectSentMapSignature, StringComparison.Ordinal))
                    return false;

                if (!string.Equals(map.FrameObjId ?? "", _manualDieDetectSentFrameObjId, StringComparison.Ordinal))
                    return false;

                if (!string.IsNullOrWhiteSpace(_manualDieDetectSentDieUid) &&
                    !string.Equals(entry.DieUid ?? "", _manualDieDetectSentDieUid, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (ResolveEntryMapX(entry) != _manualDieDetectSentMapX ||
                    ResolveEntryMapY(entry) != _manualDieDetectSentMapY)
                    return false;

                sentX = _manualDieDetectSentX;
                sentY = _manualDieDetectSentY;
                source = "마지막 MOVE VISION 전송 위치";
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

        private async Task<int> EnsureEjectPinZAvoidForThetaMoveAsync(InputStageUnit stage)
        {
            try
            {
                if (stage == null || stage.EjectPinZ == null)
                    return 0;

                stage.Recipe.EnsurePositionObjects();
                double target = stage.Recipe.EjectPinZ.AvoidPosition;
                if (IsAxisInPosition(stage.EjectPinZ, target))
                    return 0;

                int result = await stage.MoveInputStageAxis(
                    WaferStageAxis.EjectPinZ,
                    target,
                    JogSpeedType.Fine,
                    0.0).ConfigureAwait(true);
                if (result != 0)
                    return result;

                return await stage.WaitInputStageAxisInPosition(
                    WaferStageAxis.EjectPinZ,
                    target,
                    ResolveStageMoveTimeoutMs(stage)).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "EjectPinZ avoid prepare for manual die detect failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> EnsureManualInputStageThetaPositionAsync(InputStageUnit stage)
        {
            try
            {
                if (stage == null)
                    return -1;

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                string materialReason;
                if (!MaterialStateService.IsInputStageThetaAlignComplete(wafer, out materialReason))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "Manual die detect theta align blocked. " + materialReason + " - Check");
                    return -1;
                }

                stage.ApplyWaferAlignThetaResult(
                    wafer.InputStageAlignReferenceT,
                    wafer.InputStageAlignCorrectedT,
                    wafer.InputStageAlignOffsetT);

                string readyReason;
                if (!stage.IsWaferAlignThetaResultReady(out readyReason))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "Manual die detect theta align not ready. " + readyReason + " - Check");
                    return -1;
                }

                double targetT;
                if (!stage.TryResolveWaferAlignThetaTarget(out targetT))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "Manual die detect theta target resolve failed. - Check");
                    return -1;
                }

                if (stage.IsWaferAlignThetaInPosition())
                    return 0;

                int result = await stage.MoveInputStageAxis(
                    WaferStageAxis.WaferT,
                    targetT,
                    JogSpeedType.Fine,
                    0.0).ConfigureAwait(true);
                if (result != 0)
                    return result;

                return await stage.WaitInputStageAxisInPosition(
                    WaferStageAxis.WaferT,
                    targetT,
                    ResolveStageMoveTimeoutMs(stage)).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual die detect theta position check failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private void StorePendingManualInputDieDetectOffset(
            DieMap map,
            DieMapEntry entry,
            double offsetX,
            double offsetY,
            double referenceX,
            double referenceY,
            double currentX,
            double currentY,
            double jogDeltaX,
            double jogDeltaY,
            double detectedCenterX,
            double detectedCenterY,
            VisionAlignResult vision,
            double bottomRefVisionDeltaX,
            double bottomRefVisionDeltaY,
            double cameraOffsetX,
            double cameraOffsetY,
            double baseOriginX,
            double baseOriginY)
        {
            _manualDieDetectOffsetPending = true;
            _manualDieDetectMapSignature = BuildMapSignature(map);
            _manualDieDetectFrameObjId = map != null ? map.FrameObjId ?? "" : "";
            _manualDieDetectDieUid = entry != null ? entry.DieUid ?? "" : "";
            _manualDieDetectMapX = ResolveEntryMapX(entry);
            _manualDieDetectMapY = ResolveEntryMapY(entry);
            _manualDieDetectReferenceX = referenceX;
            _manualDieDetectReferenceY = referenceY;
            _manualDieDetectCurrentX = currentX;
            _manualDieDetectCurrentY = currentY;
            _manualDieDetectJogDeltaX = jogDeltaX;
            _manualDieDetectJogDeltaY = jogDeltaY;
            _manualDieDetectOffsetX = offsetX;
            _manualDieDetectOffsetY = offsetY;
            _manualDieDetectDetectedCenterX = detectedCenterX;
            _manualDieDetectDetectedCenterY = detectedCenterY;
            _manualDieDetectBottomRefVisionDeltaX = bottomRefVisionDeltaX;
            _manualDieDetectBottomRefVisionDeltaY = bottomRefVisionDeltaY;
            _manualDieDetectVisionDeltaX = vision != null ? vision.DeltaX : 0.0;
            _manualDieDetectVisionDeltaY = vision != null ? vision.DeltaY : 0.0;
            _manualDieDetectVisionDeltaT = vision != null ? vision.DeltaTheta : 0.0;
            _manualDieDetectCameraOffsetX = cameraOffsetX;
            _manualDieDetectCameraOffsetY = cameraOffsetY;
            _manualDieDetectBaseOriginX = baseOriginX;
            _manualDieDetectBaseOriginY = baseOriginY;
        }

        private void ClearPendingManualInputDieDetectOffset()
        {
            _manualDieDetectOffsetPending = false;
            _manualDieDetectMapSignature = "";
            _manualDieDetectFrameObjId = "";
            _manualDieDetectDieUid = "";
            _manualDieDetectMapX = 0;
            _manualDieDetectMapY = 0;
            _manualDieDetectReferenceX = 0.0;
            _manualDieDetectReferenceY = 0.0;
            _manualDieDetectCurrentX = 0.0;
            _manualDieDetectCurrentY = 0.0;
            _manualDieDetectJogDeltaX = 0.0;
            _manualDieDetectJogDeltaY = 0.0;
            _manualDieDetectOffsetX = 0.0;
            _manualDieDetectOffsetY = 0.0;
            _manualDieDetectDetectedCenterX = 0.0;
            _manualDieDetectDetectedCenterY = 0.0;
            _manualDieDetectBottomRefVisionDeltaX = 0.0;
            _manualDieDetectBottomRefVisionDeltaY = 0.0;
            _manualDieDetectVisionDeltaX = 0.0;
            _manualDieDetectVisionDeltaY = 0.0;
            _manualDieDetectVisionDeltaT = 0.0;
            _manualDieDetectCameraOffsetX = 0.0;
            _manualDieDetectCameraOffsetY = 0.0;
            _manualDieDetectBaseOriginX = 0.0;
            _manualDieDetectBaseOriginY = 0.0;
        }

        private void ApplyPendingManualInputDieMapOffset()
        {
            DieMap map = null;
            bool offsetApplied = false;
            bool offsetCommitted = false;
            try
            {
                if (!_manualDieDetectOffsetPending)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "적용할 다이 검출 Offset이 없습니다.\r\n먼저 [다이 검출]을 진행하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                map = mapView != null ? mapView.Map : null;
                if (map == null || map.Entries == null || map.Entries.Count == 0)
                {
                    QMC.Common.MessageDialog.Show(this, "Offset을 적용할 Active Input Die Map 데이터가 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string currentSignature = BuildMapSignature(map);
                if (!string.Equals(currentSignature, _manualDieDetectMapSignature, StringComparison.Ordinal))
                {
                    QMC.Common.MessageDialog.Show(this,
                        "다이 검출 이후 Die Map이 변경되어 Offset을 적용할 수 없습니다.\r\n다시 [다이 검출]을 진행하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ClearPendingManualInputDieDetectOffset();
                    return;
                }

                DieMapEntry entry = FindPendingManualInputDieDetectEntry(map);
                if (entry == null)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "검출 기준 Die를 현재 Map에서 찾을 수 없습니다.\r\n다시 [다이 검출]을 진행하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ClearPendingManualInputDieDetectOffset();
                    return;
                }

                Form1 host = FindForm() as Form1;
                InputStageUnit stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                if (host == null || host.Controller == null || stage == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage 장비 정보를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (host.Controller.Status == EquipmentStatus.AutoRunning ||
                    host.Controller.Status == EquipmentStatus.Initializing ||
                    host.Controller.IsSequenceRunning ||
                    host.Controller.IsManualBusy)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "장비 동작 중에는 Offset을 적용할 수 없습니다.\r\nAuto/Manual 동작을 정지한 뒤 다시 시도하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    "검출된 Offset을 전체 Input Die Map에 적용하시겠습니까?\r\n" +
                    "Die=" + BuildSelectedDieText(entry) + "\r\n" +
                    "보낸 Die 위치 X=" + _manualDieDetectReferenceX.ToString("F3") + " mm, Y=" + _manualDieDetectReferenceY.ToString("F3") + " mm\r\n" +
                    "현재 Jog 위치 X=" + _manualDieDetectCurrentX.ToString("F3") + " mm, Y=" + _manualDieDetectCurrentY.ToString("F3") + " mm\r\n" +
                    "Jog 이동량 X=" + _manualDieDetectJogDeltaX.ToString("F6") + " mm, Y=" + _manualDieDetectJogDeltaY.ToString("F6") + " mm\r\n" +
                    "Vision Delta X(보정 포함)=" + _manualDieDetectBottomRefVisionDeltaX.ToString("F6") + " mm, Y(Input only)=" + _manualDieDetectBottomRefVisionDeltaY.ToString("F6") + " mm\r\n" +
                    "Camera Offset X=" + _manualDieDetectCameraOffsetX.ToString("F6") + " mm (X 센터 이동에서 제외), Y=" + _manualDieDetectCameraOffsetY.ToString("F6") + " mm (Die Y 계산 미사용)\r\n" +
                    "Center Move Delta X=" + _manualDieDetectVisionDeltaX.ToString("F6") + " mm, Y=" + _manualDieDetectVisionDeltaY.ToString("F6") + " mm\r\n" +
                    "Vision Delta T=" + _manualDieDetectVisionDeltaT.ToString("F6") + " deg (T 보정 미적용)\r\n" +
                    "Detected Center X=" + _manualDieDetectDetectedCenterX.ToString("F3") + " mm, Y=" + _manualDieDetectDetectedCenterY.ToString("F3") + " mm\r\n" +
                    "Map Offset X=" + _manualDieDetectOffsetX.ToString("F6") + " mm, Y=" + _manualDieDetectOffsetY.ToString("F6") + " mm\r\n" +
                    "(Map Offset = Jog 이동량 + Center Move Delta)",
                    "Input Die Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                SetActionButtonsEnabled(false);

                ApplyManualInputDieMapOffset(map, _manualDieDetectOffsetX, _manualDieDetectOffsetY);
                offsetApplied = true;
                InputStageDieMapApplyResult applyResult = InputStageDieMapApplyService.Apply(
                    new InputStageDieMapApplyRequest
                    {
                        Stage = stage,
                        Controller = host.Controller,
                        Bus = null,
                        DieMap = map,
                        WaferMap = null,
                        ExpectedWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage),
                        PickupOptions = ResolveInputPickupSubsetFromRecipe(),
                        Source = "InputStageMapTransferPage.ManualInputDieMapOffsetApply",
                        SaveReason = "InputStageManualDieDetectOffsetApply",
                        PublishReadySignals = true
                    });
                if (applyResult == null || !applyResult.Success)
                {
                    ApplyManualInputDieMapOffset(map, -_manualDieDetectOffsetX, -_manualDieDetectOffsetY);
                    offsetApplied = false;
                    QMC.Common.MessageDialog.Show(this,
                        "Offset 적용 실패:\r\n" + (applyResult != null ? applyResult.ErrorMessage : ""),
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                offsetCommitted = true;

                _selectedEntry = entry;
                _pickStatusDirty = false;
                _suppressLotProgressOverlay = true;
                _lastMapSignature = BuildMapSignature(map);
                _lastMapFrameObjId = map.FrameObjId ?? "";
                RefreshDieGrid();
                SelectEntry(entry);
                mapView.Invalidate();

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual input die map offset applied. die=" + (entry.DieUid ?? "") +
                    ", offsetX=" + _manualDieDetectOffsetX.ToString("F6") +
                    ", offsetY=" + _manualDieDetectOffsetY.ToString("F6") +
                    ", mappingOffsetX=" + applyResult.MappingOffsetX.ToString("F6") +
                    ", mappingOffsetY=" + applyResult.MappingOffsetY.ToString("F6") + " - Ok");

                ClearPendingManualInputDieDetectOffset();

                QMC.Common.MessageDialog.Show(this,
                    "Offset 적용 완료.\r\n" +
                    "Offset X=" + applyResult.MappingOffsetX.ToString("F6") + " mm, Y=" + applyResult.MappingOffsetY.ToString("F6") + " mm",
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (offsetApplied && !offsetCommitted && map != null)
                {
                    try
                    {
                        ApplyManualInputDieMapOffset(map, -_manualDieDetectOffsetX, -_manualDieDetectOffsetY);
                    }
                    catch (Exception rollbackEx)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                            "Manual input die map offset rollback failed: " + rollbackEx.Message + " - Failed");
                    }
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual input die map offset apply failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "Offset 적용 실패:\r\n" + ex.Message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetActionButtonsEnabled(true);
            }
        }

        private DieMapEntry FindPendingManualInputDieDetectEntry(DieMap map)
        {
            try
            {
                if (map == null || map.Entries == null)
                    return null;

                if (!string.IsNullOrWhiteSpace(_manualDieDetectDieUid))
                {
                    foreach (DieMapEntry candidate in map.Entries)
                    {
                        if (candidate != null &&
                            string.Equals(candidate.DieUid, _manualDieDetectDieUid, StringComparison.OrdinalIgnoreCase))
                            return candidate;
                    }
                }

                return map.GetCell(_manualDieDetectMapX, _manualDieDetectMapY);
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private async Task<VisionAlignResult> RequestManualInputDieDetectVisionAsync(
            InputStageUnit stage,
            DieMapEntry entry,
            double currentVisionX,
            double currentStageY)
        {
            try
            {
                if (stage == null)
                    return null;

                if (IsManualInputDieDetectSimulationOrDryRun(stage))
                    return CreateManualInputDieDetectSimVisionOffset(entry, currentVisionX, currentStageY);

                if (stage.Vision == null)
                    return null;

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual input die detect Vision request. channel=Wafer" +
                    ", finder=" + ManualInputDieDetectFinderName +
                    ", index=" + ManualInputDieDetectVisionIndex +
                    ", timeoutMs=" + ManualInputDieDetectVisionTimeoutMs + " - Start");

                bool grabbed = await QMC.CDT320.VisionComm.AutoVisionRequestService.GrabAsync(
                    QMC.CDT320.VisionComm.AutoVisionChannel.Wafer,
                    ManualInputDieDetectVisionIndex,
                    ManualInputDieDetectVisionTimeoutMs,
                    System.Threading.CancellationToken.None).ConfigureAwait(true);
                if (!grabbed)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "Manual input die detect Vision GRAB failed. channel=Wafer" +
                        ", finder=" + ManualInputDieDetectFinderName +
                        ", index=" + ManualInputDieDetectVisionIndex + " - Failed");
                    return null;
                }

                QMC.CDT320.VisionComm.MatchResultDto match = await QMC.CDT320.VisionComm.AutoVisionRequestService.MatchAsync(
                    QMC.CDT320.VisionComm.AutoVisionChannel.Wafer,
                    ManualInputDieDetectFinderName,
                    ManualInputDieDetectVisionIndex,
                    ManualInputDieDetectVisionTimeoutMs,
                    System.Threading.CancellationToken.None).ConfigureAwait(true);
                if (match == null || !match.Success)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "Manual input die detect Vision MATCH failed. channel=Wafer" +
                        ", finder=" + ManualInputDieDetectFinderName +
                        ", index=" + ManualInputDieDetectVisionIndex +
                        ", raw=" + (match != null ? match.RawError ?? "" : "null") + " - Failed");
                    return null;
                }

                VisionAlignResult bottomRefAlign = QMC.CDT320.Calibration.VisionCameraCalibrationTransform.ToAlignResult(
                    QMC.CDT320.VisionComm.AutoVisionChannel.Wafer,
                    match,
                    ManualInputDieDetectPitchMm);
                if (bottomRefAlign == null)
                    return null;

                QMC.CDT320.Calibration.VisionCameraPixelCalibration inputCamera =
                    QMC.CDT320.Calibration.VisionCameraCalibrationTransform.ResolveCamera(
                        null,
                        QMC.CDT320.VisionComm.AutoVisionChannel.Wafer);
                if (inputCamera == null)
                    inputCamera = new QMC.CDT320.Calibration.VisionCameraPixelCalibration();

                inputCamera.EnsureDefaults(320.0, 240.0, 0.001, 0.001);
                if (match.HasImageSize)
                    inputCamera.ApplyImageSize(match.ImageWidthPixel, match.ImageHeightPixel);

                double inputOnlyDeltaY = inputCamera.PixelToMmOffsetY(match.Y);
                VisionAlignResult result = new VisionAlignResult
                {
                    DeltaX = bottomRefAlign.DeltaX,
                    DeltaY = inputOnlyDeltaY,
                    DeltaTheta = bottomRefAlign.DeltaTheta,
                    PitchX = bottomRefAlign.PitchX,
                    PitchY = bottomRefAlign.PitchY
                };

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual input die detect Vision result. channel=Wafer" +
                    ", finder=" + ManualInputDieDetectFinderName +
                    ", index=" + ManualInputDieDetectVisionIndex +
                    ", pixelX=" + match.X.ToString("F6") +
                    ", pixelY=" + match.Y.ToString("F6") +
                    ", inputCenterY=" + inputCamera.ImageCenterPixelY.ToString("F6") +
                    ", inputScaleY=" + inputCamera.PixelToMmY.ToString("F9") +
                    ", formulaY=(centerY-pixelY)*scaleY=" + inputOnlyDeltaY.ToString("F6") +
                    ", dx=" + (result != null ? result.DeltaX.ToString("F6") : "null") +
                    ", dy=" + (result != null ? result.DeltaY.ToString("F6") : "null") +
                    ", dt=" + (result != null ? result.DeltaTheta.ToString("F6") : "null") +
                    ", inputToBottomOffsetY=notUsedForDieY" +
                    (result != null ? " - Ok" : " - Failed"));
                return result;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual input die detect vision request failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static bool IsManualInputDieDetectSimulationOrDryRun(InputStageUnit stage)
        {
            try
            {
                if (stage != null && stage.IsInputStageSimulationOrDryRun())
                    return true;

                AppSettings settings = AppSettingsStore.Current;
                if (settings != null && settings.DryRunMode)
                    return true;

                return settings != null && !settings.UseVision;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static VisionAlignResult CreateManualInputDieDetectSimVisionOffset(
            DieMapEntry entry,
            double currentVisionX,
            double currentStageY)
        {
            lock (ManualDieDetectSimVisionRandomLock)
            {
                double randomOffsetX = (ManualDieDetectSimVisionRandom.NextDouble() - 0.5) * 0.002;
                double randomOffsetY = (ManualDieDetectSimVisionRandom.NextDouble() - 0.5) * 0.002;

                return new VisionAlignResult
                {
                    DeltaX = randomOffsetX,
                    DeltaY = randomOffsetY,
                    DeltaTheta = (ManualDieDetectSimVisionRandom.NextDouble() - 0.5) * 0.02
                };
            }
        }

        private static bool IsValidVisionAlignResult(VisionAlignResult result)
        {
            return result != null &&
                   IsFinite(result.DeltaX) &&
                   IsFinite(result.DeltaY) &&
                   IsFinite(result.DeltaTheta);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private void ApplyManualInputDieMapOffset(DieMap map, double offsetX, double offsetY)
        {
            if (map == null || map.Entries == null)
                return;

            map.OriginX += offsetX;
            map.OriginY += offsetY;
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null)
                    continue;

                entry.PosX += offsetX;
                entry.PosY += offsetY;
            }

            PickupSequenceGenerator.ApplySequenceNumbers(map, ResolveInputPickupSubsetFromRecipe());
            DieMapGenerator.Normalize(map);
        }

        private static WaferMapData BuildWaferMapDataFromDieMap(DieMap map, WaferMaterial wafer)
        {
            if (map == null || map.DieMapX <= 0 || map.DieMapY <= 0)
                return null;

            var waferMap = new WaferMapData
            {
                WaferId = wafer != null ? wafer.WaferId : (map.FrameObjId ?? ""),
                ColumnCount = map.DieMapX,
                RowCount = map.DieMapY,
                DieMap = new bool[map.DieMapY, map.DieMapX],
                Ref1Row = map.DieMapY / 2,
                Ref1Col = Math.Max(0, map.DieMapX / 4),
                Ref2Row = map.DieMapY / 2,
                Ref2Col = map.DieMapX > 1 ? Math.Min(map.DieMapX - 1, (map.DieMapX * 3) / 4) : 0
            };

            if (map.Entries != null)
            {
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null)
                        continue;

                    int mapX = ResolveEntryMapX(entry);
                    int mapY = ResolveEntryMapY(entry);
                    if (mapX < 0 || mapY < 0 || mapX >= waferMap.ColumnCount || mapY >= waferMap.RowCount)
                        continue;

                    waferMap.DieMap[mapY, mapX] = entry.IsTarget;
                }
            }

            return waferMap;
        }

        private void ApplyManualInputStageThetaCorrection()
        {
            IDisposable actionScope = null;
            try
            {
                var host = FindForm() as Form1;
                if (host == null || host.Controller == null || host.Machine == null || host.Machine.InputStageUnit == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage 장비 정보를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var stage = host.Machine.InputStageUnit;
                if (stage.StageT == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage StageT 축 정보를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (stage.StageT.IsAlarm)
                {
                    QMC.Common.MessageDialog.Show(this, "StageT Alarm 상태에서는 T 보정을 저장할 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage 위에 wafer Data가 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                double referenceT = stage.ResolveWaferAlignReferenceT();
                double correctedT = stage.StageT.ActualPosition;
                double offsetT = correctedT - referenceT;
                double thetaLimit = stage.ResolveWaferAlignThetaCorrectionLimit();
                if (Math.Abs(offsetT) <= 0.000001)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "T 보정 Offset이 0입니다.\r\n현재 StageT 위치가 티칭 기준 T와 같아 보정 완료값으로 저장할 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string limitReason;
                if (!stage.IsWaferAlignThetaOffsetWithinLimit(offsetT, out limitReason))
                {
                    QMC.Common.MessageDialog.Show(this,
                        "T 보정 Offset이 허용 Limit을 초과했습니다.\r\n" + limitReason,
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string message =
                    "현재 StageT 위치를 InputStage 웨이퍼 T 보정값으로 저장하시겠습니까?\r\n\r\n" +
                    "Reference T : " + referenceT.ToString("F6") + "\r\n" +
                    "Current T   : " + correctedT.ToString("F6") + "\r\n" +
                    "Offset T    : " + offsetT.ToString("F6") + "\r\n" +
                    "Limit T     : " + thetaLimit.ToString("F6");
                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    message,
                    "Input Die Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                SetActionButtonsEnabled(false);
                actionScope = host.Controller.BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "InputStageMapTransferPage:T_CORRECTION");

                wafer.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage };
                stage.SetCurrentWaferMaterial(wafer);
                stage.ApplyWaferAlignThetaResult(referenceT, correctedT, offsetT);
                MaterialStateService.SaveInputStageThetaAlignResult(wafer, referenceT, correctedT, offsetT);

                lblBarcodeValue.Text = wafer.WaferId;
                RefreshActiveInputMapIfChanged();
                RefreshDieGrid();
                mapView.Invalidate();

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual input stage theta correction saved. wafer=" + wafer.WaferId +
                    ", referenceT=" + referenceT.ToString("F6") +
                    ", correctedT=" + correctedT.ToString("F6") +
                    ", offsetT=" + offsetT.ToString("F6") + " - Ok");
                QMC.Common.MessageDialog.Show(this,
                    "T 보정 저장 완료.\r\nOffset T = " + offsetT.ToString("F6"),
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual input stage theta correction failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "T 보정 저장 실패:\r\n" + ex.Message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (actionScope != null)
                    actionScope.Dispose();
                SetActionButtonsEnabled(true);
            }
        }

        private void MarkManualAlignComplete()
        {
            try
            {
                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    "현재 InputStage wafer를 Manual Align Complete 상태로 저장하시겠습니까?",
                    "Input Die Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage 위에 wafer Data가 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                wafer.State = WaferMaterialState.WorkReady;
                wafer.UpdatedAt = DateTime.Now;
                MaterialStateService.NotifyAndSave("MapTransferManualAlignComplete");
                lblBarcodeValue.Text = wafer.WaferId;
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual align complete saved. wafer=" + wafer.WaferId + " - Ok");
                QMC.Common.MessageDialog.Show(this, "Manual Align Complete 저장 완료.",
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual align complete failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "Manual Align Complete 실패:\r\n" + ex.Message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ShowNotReadyAction(string actionName, string message)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    actionName + " blocked: " + message + " - Check");
                QMC.Common.MessageDialog.Show(this, message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private async Task MoveSelectedDieAsync()
        {
            IDisposable actionScope = null;
            try
            {
                DieMapEntry entry = _selectedEntry;
                if (entry == null)
                {
                    QMC.Common.MessageDialog.Show(this, "이동할 다이가 선택되지 않았습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Form1 host = FindForm() as Form1;
                if (host == null || host.Machine == null || host.Machine.InputStageUnit == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage 장비 정보를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                InputStageUnit stage = host.Machine.InputStageUnit;
                string indexText = entry.SequenceNo > 0
                    ? entry.SequenceNo.ToString()
                    : BuildEntryMapText(entry);
                DiePositionMoveDisplay diePosition = BuildDiePositionMoveDisplay(entry, host);
                JogSpeedType speedType;
                if (!ConfirmManualMapMoveSpeed(
                    this,
                    "Input Die Map",
                    "Die " + indexText + "의 X,Y 좌표로 이동하시겠습니까?\r\n" +
                    BuildDiePositionMoveText(diePosition, "최종 이동 위치"),
                    out speedType))
                {
                    return;
                }

                SetActionButtonsEnabled(false);
                _manualMoveBusy = true;
                actionScope = host.Controller.BeginManualActionScope(
                    ManualMotionScopeKind.SpeedOnly,
                    "InputStageMapTransferPage:MoveSelectedDie");

                int prepareResult = await AwaitManualMoveStepAsync(
                    MovePickersToAvoidForVisionMoveAsync(host, speedType),
                    ResolveManualMoveTimeoutMs(host),
                    "Vision 이동 전 Picker Avoid 준비",
                    () => StopManualMapMove(host, "Vision move prepare timeout")).ConfigureAwait(true);
                if (prepareResult != 0)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "Vision 이동 전 Picker Avoid 준비 실패\r\nresult=" + prepareResult +
                        "\r\nAlarm/Event Log를 확인하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int result = await AwaitManualMoveStepAsync(
                    stage.MoveVisionPointSafelyAsync(
                        entry.PosX,
                        entry.PosY,
                        speedType,
                        0.0,
                        "InputStageMapTransferPage.MoveSelectedDieAsync"),
                    ResolveManualMoveTimeoutMs(host),
                    "선택 다이 Vision 좌표 이동",
                    () => StopManualMapMove(host, "Vision die move timeout")).ConfigureAwait(true);
                if (result != 0)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "선택 다이 좌표 이동 실패\r\nresult=" + result +
                        "\r\nAlarm/Event Log를 확인하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                StoreManualInputDieDetectSentPosition(mapView != null ? mapView.Map : null, entry, entry.PosX, entry.PosY);
                lblAxisX.Text = entry.PosX.ToString("F3");
                lblAxisY.Text = entry.PosY.ToString("F3");
                QMC.Common.MessageDialog.Show(this, "선택 다이 좌표 이동 완료.",
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "선택 다이 좌표 이동 실패:\r\n" + ex.Message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (actionScope != null)
                        actionScope.Dispose();
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "수동 이동 스코프 정리 중 오류: " + ex.Message + " - Failed");
                }
                finally
                {
                    _manualMoveBusy = false;
                    SetActionButtonsEnabled(true);
                }
            }
        }

        private async Task MoveSelectedDieByPickerAsync(PickerSequenceSide side, int pickerNo)
        {
            IDisposable actionScope = null;
            try
            {
                DieMapEntry entry = _selectedEntry;
                if (entry == null)
                {
                    QMC.Common.MessageDialog.Show(this, "이동할 다이가 선택되지 않았습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Form1 host = FindForm() as Form1;
                if (host == null || host.Machine == null || host.Machine.InputStageUnit == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage 장비 정보를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                double offsetX;
                double offsetY;
                if (!TryResolvePickerInputOffsets(host, side, pickerNo, out offsetX, out offsetY))
                {
                    QMC.Common.MessageDialog.Show(this,
                        ResolvePickerMoveTitle(side, pickerNo) + "의 InputVision 기준 Offset을 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                PickCoordinateResult target = InputPickerPickTargetResolver.CalculateManualInputMapTarget(
                    host.Machine,
                    side,
                    pickerNo - 1,
                    entry.DieUid ?? "",
                    entry.PosX,
                    entry.PosY,
                    offsetX,
                    offsetY,
                    false);
                double targetPickerX = target.PickerX;
                double targetPickerY = target.PickerY;
                double targetStageY = target.StageY;
                double targetNeedleX = target.NeedleX;
                double cameraOffsetX;
                double cameraOffsetY;
                InputPickerPickTargetResolver.TryResolveInputCameraToBottomOffsets(host.Machine, out cameraOffsetX, out cameraOffsetY);
                DiePositionMoveDisplay diePosition = BuildDiePositionMoveDisplay(entry, host);
                JogSpeedType speedType;
                if (!ConfirmManualMapMoveSpeed(
                    this,
                    "Input Die Map",
                    ResolvePickerMoveTitle(side, pickerNo) + "를 선택 다이 위치로 이동하시겠습니까?\r\n" +
                    "Die=" + BuildSelectedDieText(entry) + "\r\n" +
                    BuildDiePositionMoveText(diePosition, "최종 Die 위치") + "\r\n" +
                    "PickerX=" + targetPickerX.ToString("F3") + " mm\r\n" +
                    "PickerY=" + targetPickerY.ToString("F3") + " mm\r\n" +
                    "NeedleX=" + targetNeedleX.ToString("F3") + " mm\r\n" +
                    "StageY=" + targetStageY.ToString("F3") + " mm\r\n" +
                    "(InputVision Offset X=" + offsetX.ToString("F3") + " mm, Y=" + offsetY.ToString("F3") + " mm\r\n" +
                    " Camera Bottom-Input Offset X=" + cameraOffsetX.ToString("F3") +
                    " mm, Y=" + cameraOffsetY.ToString("F3") +
                    " mm (InputVision Offset 저장값에 포함됨, 이동 공식에서 중복 적용하지 않음)\r\n" +
                    " Auto formula 기준, Runtime AlignOffset X/Y=0 (DieMap 좌표에 이미 적용됨)\r\n" +
                    " " + target.Formula + ")",
                    out speedType))
                {
                    return;
                }

                SetActionButtonsEnabled(false);
                _manualMoveBusy = true;
                actionScope = host.Controller.BeginManualActionScope(
                    ManualMotionScopeKind.SpeedOnly,
                    "InputStageMapTransferPage:" + ResolvePickerMoveTitle(side, pickerNo));

                int result = await AwaitManualMoveStepAsync(
                    MoveSelectedDieByPickerCoreAsync(host, side, pickerNo, entry, targetPickerX, targetPickerY, targetStageY, targetNeedleX, speedType),
                    ResolvePickerManualMoveTimeoutMs(),
                    ResolvePickerMoveTitle(side, pickerNo) + " 선택 다이 좌표 이동",
                    () => StopManualMapMove(host, ResolvePickerMoveTitle(side, pickerNo) + " die move timeout")).ConfigureAwait(true);
                if (result != 0)
                {
                    QMC.Common.MessageDialog.Show(this,
                        ResolvePickerMoveTitle(side, pickerNo) + " 좌표 이동 실패\r\nresult=" + result +
                        "\r\nAlarm/Event Log를 확인하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                lblAxisX.Text = entry.PosX.ToString("F3");
                lblAxisY.Text = entry.PosY.ToString("F3");
                QMC.Common.MessageDialog.Show(this,
                    ResolvePickerMoveTitle(side, pickerNo) + " 좌표 이동 완료.",
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Picker die move failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "Picker 좌표 이동 실패:\r\n" + ex.Message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (actionScope != null)
                        actionScope.Dispose();
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "Picker 수동 이동 스코프 정리 중 오류: " + ex.Message + " - Failed");
                }
                finally
                {
                    _manualMoveBusy = false;
                    SetActionButtonsEnabled(true);
                }
            }
        }

        private void ShowPickUpTestDialogForSelectedInputDie(PickerSequenceSide side, int pickerNo)
        {
            try
            {
                DieMapEntry entry = _selectedEntry;
                if (entry == null)
                {
                    QMC.Common.MessageDialog.Show(this, "PickUp Test 대상 Die가 선택되지 않았습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Form1 host = FindForm() as Form1;
                if (host == null || host.Controller == null)
                {
                    QMC.Common.MessageDialog.Show(this, "PickUp Test를 실행할 Controller 정보를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                {
                    QMC.Common.MessageDialog.Show(this, "PickUp Test 대상 Die UID를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                CloseInputMapPickUpTestDialog();

                _pickUpTestDialog = new InputPickTargetSelectDialog(
                    host.Controller,
                    side,
                    pickerNo,
                    entry.DieUid,
                    ResolveEntryMapX(entry),
                    ResolveEntryMapY(entry),
                    entry.PosX,
                    entry.PosY);
                _pickUpTestDialog.FormClosed += (s, e) => _pickUpTestDialog = null;

                IWin32Window ownerWindow = FindForm();
                if (ownerWindow != null)
                    _pickUpTestDialog.Show(ownerWindow);
                else
                    _pickUpTestDialog.Show();

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) +
                    " PickUp Test dialog opened. die=" + entry.DieUid + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "PickUp Test dialog open failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "PickUp Test 다이얼로그 실행 실패:\r\n" + ex.Message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void CloseInputMapPickUpTestDialog()
        {
            try
            {
                if (_pickUpTestDialog == null || _pickUpTestDialog.IsDisposed)
                    return;

                _pickUpTestDialog.Close();
            }
            catch
            {
            }
            finally
            {
                _pickUpTestDialog = null;
            }
        }

        private DiePositionMoveDisplay BuildDiePositionMoveDisplay(DieMapEntry entry, Form1 host)
        {
            var display = new DiePositionMoveDisplay();
            try
            {
                if (entry == null)
                    return display;

                display.FinalX = entry.PosX;
                display.FinalY = entry.PosY;

                bool resolved = false;
                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer != null && wafer.HasInputStageDieMappingResult)
                {
                    display.OffsetX = wafer.InputStageDieMappingOffsetX;
                    display.OffsetY = wafer.InputStageDieMappingOffsetY;
                    resolved = true;
                }

                if (!resolved)
                {
                    InputStageUnit stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                    if (stage != null)
                    {
                        display.OffsetX = stage.DieMappingOffsetX;
                        display.OffsetY = stage.DieMappingOffsetY;
                    }
                }

                display.BaseX = display.FinalX - display.OffsetX;
                display.BaseY = display.FinalY - display.OffsetY;
                return display;
            }
            catch
            {
                display.FinalX = entry != null ? entry.PosX : 0.0;
                display.FinalY = entry != null ? entry.PosY : 0.0;
                display.BaseX = display.FinalX;
                display.BaseY = display.FinalY;
                display.OffsetX = 0.0;
                display.OffsetY = 0.0;
                return display;
            }
            finally
            {
            }
        }

        private static string BuildDiePositionMoveText(DiePositionMoveDisplay display, string finalLabel)
        {
            if (string.IsNullOrWhiteSpace(finalLabel))
                finalLabel = "최종 이동 위치";

            return "Die Position(Offset 전) X=" + display.BaseX.ToString("F3") +
                   " mm, Y=" + display.BaseY.ToString("F3") + " mm\r\n" +
                   "적용 Offset X=" + display.OffsetX.ToString("F3") +
                   " mm, Y=" + display.OffsetY.ToString("F3") + " mm\r\n" +
                   finalLabel + " X=" + display.FinalX.ToString("F3") +
                   " mm, Y=" + display.FinalY.ToString("F3") + " mm";
        }

        private static bool ConfirmManualMapMoveSpeed(
            IWin32Window owner,
            string title,
            string message,
            out JogSpeedType speedType)
        {
            speedType = JogSpeedType.Fine;

            using (var dialog = new QMC.Common.MessageBoxYesNo())
            {
                dialog.ButtonGroupLabel = "MOVE";
                DialogResult result = dialog.ShowDialog(
                    string.IsNullOrWhiteSpace(title) ? "Input Die Map" : title,
                    message,
                    owner,
                    new[] { "Coarse 이동", "Fine 이동", "No" });

                if (result == DialogResult.Yes)
                {
                    speedType = JogSpeedType.Coarse;
                    return true;
                }

                if (result == DialogResult.No)
                {
                    speedType = JogSpeedType.Fine;
                    return true;
                }

                return false;
            }
        }

        private async Task<int> MoveSelectedDieByPickerCoreAsync(
            Form1 host,
            PickerSequenceSide side,
            int pickerNo,
            DieMapEntry entry,
            double targetPickerX,
            double targetPickerY,
            double targetStageY,
            double targetNeedleX,
            JogSpeedType speedType)
        {
            try
            {
                InputStageUnit stage = host.Machine.InputStageUnit;
                string areaReason;
                // 기존 조건: VisionX/StageY 원형 작업영역은 실제 간섭축 기준이 아니라서 수동 픽커 이동 차단 조건으로 쓰지 않는다.
                // if (!stage.IsInputStageWorkPointInArea(entry.PosX, targetStageY, out areaReason)) ...
                // 현재 기준: 선택 Die의 VisionX를 NeedleX 좌표로 변환한 뒤 NeedleX/StageY 작업영역을 확인한다.
                if (!stage.IsNeedleWorkPointInArea(targetNeedleX, targetStageY, out areaReason))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerMoveTitle(side, pickerNo) +
                        " blocked: target is outside needle work area. dieX=" + entry.PosX.ToString("F3") +
                        ", needleX=" + targetNeedleX.ToString("F3") +
                        ", stageY=" + targetStageY.ToString("F3") +
                        ", reason=" + areaReason + " - Check");
                    return -1;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) +
                    " calculated manual picker target. die=" + BuildSelectedDieText(entry) +
                    ", dieVisionX=" + entry.PosX.ToString("F6") +
                    ", dieStageY=" + entry.PosY.ToString("F6") +
                    ", pickerX=" + targetPickerX.ToString("F6") +
                    ", pickerY=" + targetPickerY.ToString("F6") +
                    ", stageY=" + targetStageY.ToString("F6") +
                    ", needleX=" + targetNeedleX.ToString("F6") +
                    ", formulaNeedleX=dieVisionX(" + entry.PosX.ToString("F6") +
                    ")-NeedleXToVisionXOffset(" +
                    InputPickerPickTargetResolver.ResolveNeedleCalibrationOffsetX(host.Machine).ToString("F6") +
                    ")=" + targetNeedleX.ToString("F6") + " - Check");

                int result = await EnsureManualPickerMoveZAxesAtAvoidAsync(host, side, speedType).ConfigureAwait(true);
                if (result != 0)
                    return result;

                result = await MoveInputVisionToAvoidForPickerMoveAsync(stage, speedType).ConfigureAwait(true);
                if (result != 0)
                    return result;

                result = await MoveOppositePickerToAvoidForManualPickerMoveAsync(host, side, speedType).ConfigureAwait(true);
                if (result != 0)
                    return result;

                result = await MoveTargetPickerYToAvoidForManualPickerMoveAsync(host, side, pickerNo, speedType).ConfigureAwait(true);
                if (result != 0)
                    return result;

                Task<int> moveNeedleStage = MoveNeedleXAndStageYForManualPickerMoveAsync(
                    stage,
                    targetNeedleX,
                    targetStageY,
                    entry.PosX,
                    speedType,
                    ResolvePickerMoveTitle(side, pickerNo));
                string pickerTargetName = "DiePickPosition[" + (pickerNo - 1) + "];ManualInputDieMapMove";
                Task<int> movePickerX = side == PickerSequenceSide.Front
                    ? host.Machine.PickerFrontUnit.MoveFrontPickerAxis(PickerAxis.PickerX, targetPickerX, speedType, 0.0, pickerTargetName)
                    : host.Machine.PickerRearUnit.MoveRearPickerAxis(PickerAxis.PickerX, targetPickerX, speedType, 0.0, pickerTargetName);
                int[] moveResults = await Task.WhenAll(moveNeedleStage, movePickerX).ConfigureAwait(true);

                if (moveResults[0] != 0)
                    return moveResults[0];
                if (moveResults[1] != 0)
                    return moveResults[1];

                Task<int> waitPickerX = WaitPickerXMoveDoneAsync(host, side, targetPickerX);
                int waitPickerResult = await waitPickerX.ConfigureAwait(true);
                if (waitPickerResult != 0)
                    return waitPickerResult;

                result = await MoveTargetPickerYForManualPickerMoveAsync(
                    host,
                    side,
                    pickerNo,
                    targetPickerY,
                    speedType,
                    pickerTargetName + ";PickerZone=Input",
                    true).ConfigureAwait(true);
                if (result != 0)
                    return result;

                if (!IsStageYInPosition(stage, targetStageY))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerMoveTitle(side, pickerNo) +
                        " final check failed: StageY target=" + targetStageY.ToString("F3") +
                        ", actual=" + stage.StageY.ActualPosition.ToString("F3") + " - Failed");
                    return -1;
                }

                if (!IsAxisInPosition(stage.NeedleBlockX, targetNeedleX))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerMoveTitle(side, pickerNo) +
                        " final check failed: NeedleX target=" + targetNeedleX.ToString("F3") +
                        ", actual=" + (stage.NeedleBlockX != null ? stage.NeedleBlockX.ActualPosition.ToString("F3") : "-") +
                        " - Failed");
                    return -1;
                }

                if (!IsPickerXInPosition(host, side, targetPickerX))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerMoveTitle(side, pickerNo) +
                        " final check failed: PickerX target=" + targetPickerX.ToString("F3") + " - Failed");
                    return -1;
                }

                if (!IsPickerYInPosition(host, side, targetPickerY))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerMoveTitle(side, pickerNo) +
                        " final check failed: PickerY target=" + targetPickerY.ToString("F3") + " - Failed");
                    return -1;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) +
                    " move complete. die=" + BuildSelectedDieText(entry) +
                    ", pickerX=" + targetPickerX.ToString("F3") +
                    ", pickerY=" + targetPickerY.ToString("F3") +
                    ", needleX=" + targetNeedleX.ToString("F3") +
                    ", stageY=" + targetStageY.ToString("F3") + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) + " move exception: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveNeedleXAndStageYForManualPickerMoveAsync(
            InputStageUnit stage,
            double targetNeedleX,
            double targetStageY,
            double workAreaVisionX,
            JogSpeedType speedType,
            string title)
        {
            try
            {
                if (stage == null)
                    return -1;

                bool needleInPosition = IsAxisInPosition(stage.NeedleBlockX, targetNeedleX);
                bool stageYInPosition = IsStageYInPosition(stage, targetStageY);
                if (needleInPosition && stageYInPosition)
                    return 0;

                bool moveNeedleXFirst;
                string reason;
                if (!stage.TryResolveNeedleWorkPointMoveOrder(targetNeedleX, targetStageY, out moveNeedleXFirst, out reason))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        title + " safe move order resolve failed. needleX=" + targetNeedleX.ToString("F3") +
                        ", stageY=" + targetStageY.ToString("F3") +
                        ", reason=" + reason + " - Failed");
                    RaiseManualMoveAlarm("IN-STAGE-MAP-NEEDLE-STAGE-PATH",
                        title + " NeedleX/StageY 이동 가능한 안전 순서를 찾지 못했습니다. " + reason);
                    return -1;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    title + " safe move order. first=" + (moveNeedleXFirst ? "NeedleX" : "StageY") +
                    ", needleX=" + targetNeedleX.ToString("F3") +
                    ", stageY=" + targetStageY.ToString("F3") + " - Check");

                if (moveNeedleXFirst)
                {
                    if (!needleInPosition)
                    {
                        int result = await MoveNeedleXForManualPickerMoveAsync(
                            stage,
                            targetNeedleX,
                            speedType,
                            title).ConfigureAwait(true);
                        if (result != 0)
                            return result;
                    }

                    if (!stageYInPosition)
                    {
                        int result = await MoveStageYForManualPickerMoveAsync(
                            stage,
                            workAreaVisionX,
                            targetStageY,
                            targetNeedleX,
                            speedType,
                            title).ConfigureAwait(true);
                        if (result != 0)
                            return result;
                    }
                }
                else
                {
                    if (!stageYInPosition)
                    {
                        int result = await MoveStageYForManualPickerMoveAsync(
                            stage,
                            workAreaVisionX,
                            targetStageY,
                            targetNeedleX,
                            speedType,
                            title).ConfigureAwait(true);
                        if (result != 0)
                            return result;
                    }

                    if (!needleInPosition)
                    {
                        int result = await MoveNeedleXForManualPickerMoveAsync(
                            stage,
                            targetNeedleX,
                            speedType,
                            title).ConfigureAwait(true);
                        if (result != 0)
                            return result;
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    title + " NeedleX/StageY safe move exception: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveNeedleXForManualPickerMoveAsync(
            InputStageUnit stage,
            double targetNeedleX,
            JogSpeedType speedType,
            string title)
        {
            try
            {
                if (stage == null || stage.NeedleBlockX == null)
                    return -1;

                if (IsAxisInPosition(stage.NeedleBlockX, targetNeedleX))
                    return 0;

                int result = await stage.MoveInputStageAxis(
                    WaferStageAxis.NeedleX,
                    targetNeedleX,
                    speedType,
                    0.0).ConfigureAwait(true);
                if (result != 0)
                    return result;

                result = await stage.WaitInputStageAxisInPosition(
                    WaferStageAxis.NeedleX,
                    targetNeedleX,
                    ResolveStageMoveTimeoutMs(stage)).ConfigureAwait(true);
                if (result != 0)
                    return result;

                return IsAxisInPosition(stage.NeedleBlockX, targetNeedleX) ? 0 : -1;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    title + " NeedleX move exception: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveStageYForManualPickerMoveAsync(
            InputStageUnit stage,
            double workAreaVisionX,
            double targetStageY,
            double targetNeedleX,
            JogSpeedType speedType,
            string title)
        {
            try
            {
                if (stage == null || stage.StageY == null)
                    return -1;

                if (IsStageYInPosition(stage, targetStageY))
                    return 0;

                int result = await PickerInputStageMoveHelper.MoveStageYForPickerWorkPointCommandAsync(
                    stage,
                    workAreaVisionX,
                    targetStageY,
                    speedType,
                    0.0,
                    "InputStageMapTransferPickerMove",
                    targetNeedleX).ConfigureAwait(true);
                if (result != 0)
                    return result;

                result = await stage.WaitInputStageAxisInPosition(
                    WaferStageAxis.WaferY,
                    targetStageY,
                    ResolveStageMoveTimeoutMs(stage)).ConfigureAwait(true);
                if (result != 0)
                    return result;

                return IsStageYInPosition(stage, targetStageY) ? 0 : -1;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    title + " StageY move exception: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> EnsureManualPickerMoveZAxesAtAvoidAsync(
            Form1 host,
            PickerSequenceSide side,
            JogSpeedType speedType)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerSideName(side) +
                    " picker manual move Z safety prepare. PickerZ/NeedleZ/EjectPinZ move to Avoid first. - Start");

                int result = await MoveTargetPickerZAxesToAvoidForManualPickerMoveAsync(
                    host,
                    side,
                    speedType).ConfigureAwait(true);
                if (result != 0)
                {
                    string message = ResolvePickerSideName(side) +
                        " picker manual move blocked. PickerZ Avoid prepare failed. result=" + result;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage", message + " - Failed");
                    RaiseManualMoveAlarm("IN-STAGE-MAP-PICKER-Z-AVOID", message);
                    return result;
                }

                result = await MoveInputStageZAxesToAvoidForManualPickerMoveAsync(
                    host != null && host.Machine != null ? host.Machine.InputStageUnit : null,
                    speedType).ConfigureAwait(true);
                if (result != 0)
                {
                    string message = ResolvePickerSideName(side) +
                        " picker manual move blocked. InputStage NeedleZ/EjectPinZ Avoid prepare failed. result=" + result;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage", message + " - Failed");
                    RaiseManualMoveAlarm("IN-STAGE-MAP-INPUT-Z-AVOID", message);
                    return result;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerSideName(side) +
                    " picker manual move Z safety prepare complete. - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                string message = ResolvePickerSideName(side) +
                    " picker manual move Z safety prepare exception: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage", message + " - Failed");
                RaiseManualMoveAlarm("IN-STAGE-MAP-Z-AVOID-EXCEPTION", message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetPickerZAxesToAvoidForManualPickerMoveAsync(
            Form1 host,
            PickerSequenceSide side,
            JogSpeedType speedType)
        {
            try
            {
                if (host == null || host.Machine == null)
                    return -1;

                for (int i = 0; i < PickerZAxes.Length; i++)
                {
                    PickerAxis axis = PickerZAxes[i];
                    double target = InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                        host.Machine,
                        side,
                        axis,
                        "AvoidPosition");
                    if (IsPickerAxisInPosition(host, side, axis, target))
                        continue;

                    string targetName = "AvoidPosition;ManualInputDieMapMove;PickerPhase=SafeZ";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerSideName(side) +
                        " " + axis + " avoid move before manual picker move. target=" +
                        target.ToString("F6") + " - Start");

                    int result;
                    if (side == PickerSequenceSide.Front)
                    {
                        PickerFrontUnit front = host.Machine.PickerFrontUnit;
                        if (front == null)
                            return -1;

                        result = await front.MoveFrontPickerAxis(
                            axis,
                            target,
                            speedType,
                            0.0,
                            targetName).ConfigureAwait(true);
                    }
                    else
                    {
                        PickerRearUnit rear = host.Machine.PickerRearUnit;
                        if (rear == null)
                            return -1;

                        result = await rear.MoveRearPickerAxis(
                            axis,
                            target,
                            speedType,
                            0.0,
                            targetName).ConfigureAwait(true);
                    }

                    if (result != 0)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                            ResolvePickerSideName(side) +
                            " " + axis + " avoid move before manual picker move failed. result=" +
                            result + " - Failed");
                        return result;
                    }

                    if (!IsPickerAxisInPosition(host, side, axis, target))
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                            ResolvePickerSideName(side) +
                            " " + axis + " avoid final check failed. target=" +
                            target.ToString("F6") + " - Failed");
                        return -1;
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerSideName(side) +
                    " PickerZ avoid prepare failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageZAxesToAvoidForManualPickerMoveAsync(
            InputStageUnit stage,
            JogSpeedType speedType)
        {
            try
            {
                if (stage == null || stage.Recipe == null)
                    return -1;

                stage.Recipe.EnsurePositionObjects();

                int result = await MoveInputStageZAxisToAvoidForManualPickerMoveAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    stage.Recipe.EjectPinZ.AvoidPosition,
                    speedType,
                    "EjectPinZ").ConfigureAwait(true);
                if (result != 0)
                    return result;

                return await MoveInputStageZAxisToAvoidForManualPickerMoveAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    stage.Recipe.NeedleZ.AvoidPosition,
                    speedType,
                    "NeedleZ").ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "InputStage NeedleZ/EjectPinZ avoid prepare failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageZAxisToAvoidForManualPickerMoveAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            JogSpeedType speedType,
            string axisName)
        {
            if (stage == null)
                return -1;

            BaseAxis item = ResolveInputStageAxis(stage, axis);
            if (IsAxisInPosition(item, target))
                return 0;

            QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                axisName + " avoid move before manual picker move. target=" +
                target.ToString("F6") + " - Start");

            int result = await stage.MoveInputStageAxis(
                axis,
                target,
                speedType,
                0.0).ConfigureAwait(true);
            if (result != 0)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    axisName + " avoid move before manual picker move failed. result=" +
                    result + " - Failed");
                return result;
            }

            result = await stage.WaitInputStageAxisInPosition(
                axis,
                target,
                ResolveStageMoveTimeoutMs(stage)).ConfigureAwait(true);
            if (result != 0)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    axisName + " avoid wait before manual picker move failed. result=" +
                    result + " - Failed");
                return result;
            }

            return IsAxisInPosition(item, target) ? 0 : -1;
        }

        private async Task<int> MoveOppositePickerToAvoidForManualPickerMoveAsync(
            Form1 host,
            PickerSequenceSide movingSide,
            JogSpeedType speedType)
        {
            try
            {
                PickerSequenceSide oppositeSide = movingSide == PickerSequenceSide.Front
                    ? PickerSequenceSide.Rear
                    : PickerSequenceSide.Front;

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerSideName(movingSide) + " picker manual move prepare. " +
                    ResolvePickerSideName(oppositeSide) + " picker moves to Avoid first. - Start");

                int result = await MoveTargetPickerToAvoidAsync(host, oppositeSide, speedType).ConfigureAwait(true);
                if (result != 0)
                {
                    string message = ResolvePickerSideName(movingSide) +
                        " picker manual move blocked. " +
                        ResolvePickerSideName(oppositeSide) +
                        " picker Avoid move failed. result=" + result;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage", message + " - Failed");
                    RaiseManualMoveAlarm("IN-STAGE-MAP-OPPOSITE-PICKER-AVOID", message);
                    return result;
                }

                if (!IsTargetPickerInAvoidPosition(host, oppositeSide))
                {
                    string message = ResolvePickerSideName(movingSide) +
                        " picker manual move blocked. " +
                        ResolvePickerSideName(oppositeSide) +
                        " picker final Avoid position check failed.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage", message + " - Failed");
                    RaiseManualMoveAlarm("IN-STAGE-MAP-OPPOSITE-PICKER-AVOID-CHECK", message);
                    return -1;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerSideName(oppositeSide) + " picker Avoid prepare complete for " +
                    ResolvePickerSideName(movingSide) + " picker manual move. - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                string message = ResolvePickerSideName(movingSide) +
                    " picker manual move prepare failed: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage", message + " - Failed");
                RaiseManualMoveAlarm("IN-STAGE-MAP-OPPOSITE-PICKER-AVOID-EXCEPTION", message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MovePickersToAvoidForVisionMoveAsync(Form1 host, JogSpeedType speedType)
        {
            try
            {
                int frontResult = await MoveTargetPickerToAvoidAsync(host, PickerSequenceSide.Front, speedType).ConfigureAwait(true);
                if (frontResult != 0)
                    return frontResult;

                int rearResult = await MoveTargetPickerToAvoidAsync(host, PickerSequenceSide.Rear, speedType).ConfigureAwait(true);
                if (rearResult != 0)
                    return rearResult;

                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Picker avoid prepare for vision move failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputVisionToAvoidForPickerMoveAsync(InputStageUnit stage, JogSpeedType speedType)
        {
            try
            {
                if (stage == null)
                    return -1;

                if (stage.IsVisionXInAvoidPosition())
                    return 0;

                stage.Recipe.EnsurePositionObjects();
                double avoidTarget = stage.Recipe.VisionX.AvoidPosition;
                int result = await stage.MoveInputStageAxis(WaferStageAxis.VisionX, avoidTarget, speedType, 0.0).ConfigureAwait(true);
                if (result != 0)
                    return result;

                result = await stage.WaitInputStageAxisInPosition(
                    WaferStageAxis.VisionX,
                    avoidTarget,
                    ResolveStageMoveTimeoutMs(stage)).ConfigureAwait(true);
                if (result != 0)
                    return result;

                if (!stage.IsVisionXInAvoidPosition())
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "InputVisionX avoid final check failed. target=" + avoidTarget.ToString("F3") +
                        ", actual=" + (stage.CameraX != null ? stage.CameraX.ActualPosition.ToString("F3") : "-") + " - Failed");
                    return -1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "InputVisionX avoid move failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetPickerToAvoidAsync(Form1 host, PickerSequenceSide side, JogSpeedType speedType)
        {
            try
            {
                if (host == null || host.Machine == null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerSideName(side) + " avoid move failed: host or machine is null. - Failed");
                    return -1;
                }

                if (side == PickerSequenceSide.Front)
                {
                    PickerFrontUnit front = host.Machine.PickerFrontUnit;
                    if (front == null)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                            "FRONT avoid move failed: PickerFrontUnit is null. - Failed");
                        return -1;
                    }

                    if (front.IsFrontPickerInAvoidPosition())
                        return 0;

                    int result = await front.MoveToFrontPickerAvoidPosition(speedType, 0.0).ConfigureAwait(true);
                    if (result != 0)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                            "FRONT avoid move failed. result=" + result + " - Failed");
                        return result;
                    }

                    if (!front.IsFrontPickerInAvoidPosition())
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                            "FRONT avoid final check failed. - Failed");
                        return -1;
                    }

                    return 0;
                }

                PickerRearUnit rear = host.Machine.PickerRearUnit;
                if (rear == null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "REAR avoid move failed: PickerRearUnit is null. - Failed");
                    return -1;
                }

                if (rear.IsRearPickerInAvoidPosition())
                    return 0;

                int rearResult = await rear.MoveToRearPickerAvoidPosition(speedType, 0.0).ConfigureAwait(true);
                if (rearResult != 0)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "REAR avoid move failed. result=" + rearResult + " - Failed");
                    return rearResult;
                }

                if (!rear.IsRearPickerInAvoidPosition())
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "REAR avoid final check failed. - Failed");
                    return -1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerSideName(side) + " avoid move failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private static bool IsTargetPickerInAvoidPosition(Form1 host, PickerSequenceSide side)
        {
            try
            {
                if (host == null || host.Machine == null)
                    return false;

                if (side == PickerSequenceSide.Front)
                {
                    PickerFrontUnit front = host.Machine.PickerFrontUnit;
                    return front != null && front.IsFrontPickerInAvoidPosition();
                }

                PickerRearUnit rear = host.Machine.PickerRearUnit;
                return rear != null && rear.IsRearPickerInAvoidPosition();
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> WaitPickerXMoveDoneAsync(Form1 host, PickerSequenceSide side, double targetPickerX)
        {
            try
            {
                if (host == null || host.Machine == null)
                    return -1;

                if (side == PickerSequenceSide.Front)
                {
                    PickerFrontUnit front = host.Machine.PickerFrontUnit;
                    if (front == null)
                        return -1;

                    bool ok = await front.WaitFrontPickerAxisMoveDone(
                        PickerAxis.PickerX,
                        front.ResolvePickerAxisMoveTimeoutMs(PickerAxis.PickerX)).ConfigureAwait(true);
                    if (!ok)
                        return -1;

                    return front.IsFrontPickerAxisInPosition(
                        PickerAxis.PickerX,
                        targetPickerX,
                        ResolvePickerTolerance(front.PickerX)) ? 0 : -1;
                }

                PickerRearUnit rear = host.Machine.PickerRearUnit;
                if (rear == null)
                    return -1;

                bool rearOk = await rear.WaitRearPickerAxisMoveDone(
                    PickerAxis.PickerX,
                    rear.ResolvePickerAxisMoveTimeoutMs(PickerAxis.PickerX)).ConfigureAwait(true);
                if (!rearOk)
                    return -1;

                return rear.IsRearPickerAxisInPosition(
                    PickerAxis.PickerX,
                    targetPickerX,
                    ResolvePickerTolerance(rear.PickerX)) ? 0 : -1;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerSideName(side) + " picker X wait failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetPickerYToAvoidForManualPickerMoveAsync(
            Form1 host,
            PickerSequenceSide side,
            int pickerNo,
            JogSpeedType speedType)
        {
            try
            {
                double avoidTarget = InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                    host != null ? host.Machine : null,
                    side,
                    PickerAxis.PickerY,
                    "AvoidPosition");
                string targetName = "AvoidPosition;ManualInputDieMapMove;PickerPhase=SafeY";
                return await MoveTargetPickerYForManualPickerMoveAsync(
                    host,
                    side,
                    pickerNo,
                    avoidTarget,
                    speedType,
                    targetName,
                    false).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) +
                    " PickerY avoid move exception: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetPickerYForManualPickerMoveAsync(
            Form1 host,
            PickerSequenceSide side,
            int pickerNo,
            double targetPickerY,
            JogSpeedType speedType,
            string targetName,
            bool requireForwardTarget)
        {
            try
            {
                if (host == null || host.Machine == null)
                    return -1;

                BaseAxis pickerY = ResolvePickerYAxis(host, side);
                double actualBefore = pickerY != null ? pickerY.ActualPosition : 0.0;
                double tolerance = ResolvePickerTolerance(pickerY);

                if (requireForwardTarget && IsPickerYForwardTargetInvalid(host, side, targetPickerY, tolerance))
                {
                    string message = ResolvePickerMoveTitle(side, pickerNo) +
                        " PickerY 전진 목표값이 Avoid/Home과 같아 전진할 수 없습니다. " +
                        "calculatedTargetY=" + targetPickerY.ToString("F6") +
                        ", actualY=" + actualBefore.ToString("F6") +
                        ", avoidY=" + InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                            host.Machine,
                            side,
                            PickerAxis.PickerY,
                            "AvoidPosition").ToString("F6") +
                        ", tolerance=" + tolerance.ToString("F6") +
                        ", targetName=" + (targetName ?? "-") +
                        ". InputVisionToPickerY 설정과 PickerY 축 방향을 확인하세요.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage", message + " - Failed");
                    RaiseManualMoveAlarm("IN-STAGE-MAP-PICKER-Y-TARGET", message);
                    return -1;
                }

                if (IsPickerYInPosition(host, side, targetPickerY))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerMoveTitle(side, pickerNo) +
                        " PickerY move skipped because axis is already at calculated target. " +
                        "targetY=" + targetPickerY.ToString("F6") +
                        ", actualY=" + actualBefore.ToString("F6") +
                        ", tolerance=" + tolerance.ToString("F6") +
                        ", targetName=" + (targetName ?? "-") + " - Ok");
                    return 0;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) +
                    " PickerY move start. targetY=" + targetPickerY.ToString("F6") +
                    ", actualY=" + actualBefore.ToString("F6") +
                    ", tolerance=" + tolerance.ToString("F6") +
                    ", targetName=" + (targetName ?? "-") + " - Start");

                int result;
                if (side == PickerSequenceSide.Front)
                {
                    PickerFrontUnit front = host.Machine.PickerFrontUnit;
                    if (front == null)
                        return -1;

                    result = await front.MoveFrontPickerAxis(
                        PickerAxis.PickerY,
                        targetPickerY,
                        speedType,
                        0.0,
                        targetName).ConfigureAwait(true);
                    if (result != 0)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                            ResolvePickerMoveTitle(side, pickerNo) +
                            " PickerY move command failed. result=" + result +
                            ", targetY=" + targetPickerY.ToString("F6") +
                            ", actualY=" + (front.PickerY != null ? front.PickerY.ActualPosition.ToString("F6") : "-") +
                            ", targetName=" + (targetName ?? "-") + " - Failed");
                        return result;
                    }

                    bool ok = await front.WaitFrontPickerAxisMoveDone(
                        PickerAxis.PickerY,
                        front.ResolvePickerAxisMoveTimeoutMs(PickerAxis.PickerY)).ConfigureAwait(true);
                    if (!ok)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                            ResolvePickerMoveTitle(side, pickerNo) +
                            " PickerY move wait failed. targetY=" + targetPickerY.ToString("F6") +
                            ", actualY=" + (front.PickerY != null ? front.PickerY.ActualPosition.ToString("F6") : "-") +
                            ", targetName=" + (targetName ?? "-") + " - Failed");
                        return -1;
                    }

                    bool inPosition = front.IsFrontPickerAxisInPosition(
                        PickerAxis.PickerY,
                        targetPickerY,
                        ResolvePickerTolerance(front.PickerY));
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerMoveTitle(side, pickerNo) +
                        " PickerY move complete check. targetY=" + targetPickerY.ToString("F6") +
                        ", actualY=" + (front.PickerY != null ? front.PickerY.ActualPosition.ToString("F6") : "-") +
                        ", inPosition=" + (inPosition ? "Y" : "N") +
                        ", targetName=" + (targetName ?? "-") +
                        (inPosition ? " - Ok" : " - Failed"));
                    return inPosition ? 0 : -1;
                }

                PickerRearUnit rear = host.Machine.PickerRearUnit;
                if (rear == null)
                    return -1;

                result = await rear.MoveRearPickerAxis(
                    PickerAxis.PickerY,
                    targetPickerY,
                    speedType,
                    0.0,
                    targetName).ConfigureAwait(true);
                if (result != 0)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerMoveTitle(side, pickerNo) +
                        " PickerY move command failed. result=" + result +
                        ", targetY=" + targetPickerY.ToString("F6") +
                        ", actualY=" + (rear.PickerY != null ? rear.PickerY.ActualPosition.ToString("F6") : "-") +
                        ", targetName=" + (targetName ?? "-") + " - Failed");
                    return result;
                }

                bool rearOk = await rear.WaitRearPickerAxisMoveDone(
                    PickerAxis.PickerY,
                    rear.ResolvePickerAxisMoveTimeoutMs(PickerAxis.PickerY)).ConfigureAwait(true);
                if (!rearOk)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerMoveTitle(side, pickerNo) +
                        " PickerY move wait failed. targetY=" + targetPickerY.ToString("F6") +
                        ", actualY=" + (rear.PickerY != null ? rear.PickerY.ActualPosition.ToString("F6") : "-") +
                        ", targetName=" + (targetName ?? "-") + " - Failed");
                    return -1;
                }

                bool rearInPosition = rear.IsRearPickerAxisInPosition(
                    PickerAxis.PickerY,
                    targetPickerY,
                    ResolvePickerTolerance(rear.PickerY));
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) +
                    " PickerY move complete check. targetY=" + targetPickerY.ToString("F6") +
                    ", actualY=" + (rear.PickerY != null ? rear.PickerY.ActualPosition.ToString("F6") : "-") +
                    ", inPosition=" + (rearInPosition ? "Y" : "N") +
                    ", targetName=" + (targetName ?? "-") +
                    (rearInPosition ? " - Ok" : " - Failed"));
                return rearInPosition ? 0 : -1;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) +
                    " PickerY move exception: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> AwaitManualMoveStepAsync(
            Task<int> operation,
            int timeoutMs,
            string description,
            Action onTimeoutStop)
        {
            try
            {
                if (operation == null)
                    return -1;

                int effectiveTimeoutMs = timeoutMs > 0 ? timeoutMs : 30000;
                Task timeoutTask = Task.Delay(effectiveTimeoutMs);
                Task completed = await Task.WhenAny(operation, timeoutTask).ConfigureAwait(true);
                if (completed == operation)
                    return await operation.ConfigureAwait(true);

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    description + " timeout. timeoutMs=" + effectiveTimeoutMs + " - Failed");
                RaiseManualMoveAlarm("IN-STAGE-MAP-MANUAL-TIMEOUT", description + " timeout. Manual move stopped.");

                try
                {
                    if (onTimeoutStop != null)
                        onTimeoutStop();
                }
                catch (Exception stopEx)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        description + " timeout stop failed: " + stopEx.Message + " - Failed");
                }
                finally
                {
                }

                ObserveManualMoveTask(operation, description);
                return -1;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    description + " failed: " + ex.Message + " - Failed");
                RaiseManualMoveAlarm("IN-STAGE-MAP-MANUAL-FAIL", description + " failed. " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private static void ObserveManualMoveTask(Task<int> operation, string description)
        {
            try
            {
                if (operation == null)
                    return;

                operation.ContinueWith(t =>
                {
                    try
                    {
                        if (t.IsFaulted)
                        {
                            Exception ex = t.Exception != null ? t.Exception.GetBaseException() : null;
                            QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                                description + " background task faulted after timeout: " +
                                (ex != null ? ex.Message : "-") + " - Failed");
                            return;
                        }

                        if (t.IsCanceled)
                        {
                            QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                                description + " background task canceled after timeout. - Check");
                            return;
                        }

                        QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                            description + " background task completed after timeout. result=" + t.Result + " - Check");
                    }
                    catch
                    {
                    }
                    finally
                    {
                    }
                });
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void StopManualMapMove(Form1 host, string reason)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual map move stop requested. reason=" + reason + " - Check");

                if (host != null && host.Controller != null)
                    host.Controller.CancelManualOperation();

                if (host == null || host.Machine == null)
                    return;

                InputStageUnit stage = host.Machine.InputStageUnit;
                if (stage != null)
                {
                    stage.ManualStopInputStageAxis(WaferStageAxis.WaferY);
                    stage.ManualStopInputStageAxis(WaferStageAxis.VisionX);
                    stage.ManualStopInputStageAxis(WaferStageAxis.NeedleX);
                    stage.ManualStopInputStageAxis(WaferStageAxis.NeedleZ);
                    stage.ManualStopInputStageAxis(WaferStageAxis.EjectPinZ);
                }

                PickerFrontUnit front = host.Machine.PickerFrontUnit;
                if (front != null)
                    front.StopPickerMotionAndOutputs(reason);

                PickerRearUnit rear = host.Machine.PickerRearUnit;
                if (rear != null)
                    rear.StopPickerMotionAndOutputs(reason);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual map move stop failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static void RaiseManualMoveAlarm(string code, string message)
        {
            try
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Error,
                    code,
                    "InputStageMapTransferPage",
                    message);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static bool TryResolvePickerInputOffsets(Form1 host, PickerSequenceSide side, int pickerNo, out double offsetX, out double offsetY)
        {
            offsetX = 0.0;
            offsetY = 0.0;

            try
            {
                int index = pickerNo - 1;
                string reason;
                bool resolved = PickerCoordinateTransformHelper.TryResolveInputVisionToPickerOffsets(
                    host != null ? host.Machine : null,
                    side,
                    index,
                    out offsetX,
                    out offsetY,
                    out reason);

                if (!resolved)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        "Picker input offset resolve failed. side=" + side +
                        ", pickerNo=" + pickerNo +
                        ", pickerIndex=" + index +
                        ", reason=" + reason + " - Failed");
                }

                return resolved;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Picker input offset resolve exception. side=" + side +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static string ResolvePickerMoveTitle(PickerSequenceSide side, int pickerNo)
        {
            return ResolvePickerSideName(side) + " PICKER #" + pickerNo;
        }

        private static string ResolvePickerSideName(PickerSequenceSide side)
        {
            return side == PickerSequenceSide.Front ? "FRONT" : "REAR";
        }

        private static string BuildSelectedDieText(DieMapEntry entry)
        {
            if (entry == null)
                return "-";

            return entry.SequenceNo > 0
                ? entry.SequenceNo.ToString()
                : BuildEntryMapText(entry);
        }

        private static int ResolveStageMoveTimeoutMs(InputStageUnit stage)
        {
            if (stage != null && stage.Config != null && stage.Config.SequenceMoveTimeoutMs > 0)
                return stage.Config.SequenceMoveTimeoutMs;

            return 10000;
        }

        private static int ResolveManualMoveTimeoutMs(Form1 host)
        {
            try
            {
                int timeoutMs = 30000;
                if (host != null && host.Machine != null && host.Machine.InputStageUnit != null)
                {
                    int stageTimeoutMs = ResolveStageMoveTimeoutMs(host.Machine.InputStageUnit);
                    if (stageTimeoutMs > timeoutMs)
                        timeoutMs = stageTimeoutMs;
                }

                if (host != null && host.Machine != null && host.Machine.PickerFrontUnit != null)
                {
                    int frontTimeoutMs = host.Machine.PickerFrontUnit.ResolvePickerAxisMoveTimeoutMs(PickerAxis.PickerX);
                    if (frontTimeoutMs > timeoutMs)
                        timeoutMs = frontTimeoutMs;
                }

                if (host != null && host.Machine != null && host.Machine.PickerRearUnit != null)
                {
                    int rearTimeoutMs = host.Machine.PickerRearUnit.ResolvePickerAxisMoveTimeoutMs(PickerAxis.PickerX);
                    if (rearTimeoutMs > timeoutMs)
                        timeoutMs = rearTimeoutMs;
                }

                return Math.Max(timeoutMs + 5000, 30000);
            }
            catch
            {
                return 30000;
            }
            finally
            {
            }
        }

        private static int ResolvePickerManualMoveTimeoutMs()
        {
            return 5 * 60 * 1000;
        }

        private static double ResolvePickerTolerance(BaseAxis axis)
        {
            if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                return axis.Config.InPositionTolerance;

            return 0.05;
        }

        private static bool IsStageYInPosition(InputStageUnit stage, double targetStageY)
        {
            if (stage == null || stage.StageY == null)
                return false;

            double tolerance = stage.StageY.Config != null && stage.StageY.Config.InPositionTolerance > 0.0
                ? stage.StageY.Config.InPositionTolerance
                : 0.05;
            return Math.Abs(stage.StageY.ActualPosition - targetStageY) <= tolerance && !stage.StageY.IsAlarm;
        }

        private static bool IsAxisInPosition(BaseAxis axis, double target)
        {
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;
            return Math.Abs(axis.ActualPosition - target) <= tolerance && !axis.IsAlarm && !axis.IsMoving;
        }

        private static bool IsPickerAxisInPosition(Form1 host, PickerSequenceSide side, PickerAxis axis, double target)
        {
            if (host == null || host.Machine == null)
                return false;

            BaseAxis item = ResolvePickerAxis(host, side, axis);
            double tolerance = ResolvePickerTolerance(item);
            if (side == PickerSequenceSide.Front)
            {
                PickerFrontUnit front = host.Machine.PickerFrontUnit;
                return front != null && front.IsFrontPickerAxisInPosition(axis, target, tolerance);
            }

            PickerRearUnit rear = host.Machine.PickerRearUnit;
            return rear != null && rear.IsRearPickerAxisInPosition(axis, target, tolerance);
        }

        private static BaseAxis ResolvePickerAxis(Form1 host, PickerSequenceSide side, PickerAxis axis)
        {
            if (host == null || host.Machine == null)
                return null;

            BaseAxis item;
            if (side == PickerSequenceSide.Front)
            {
                PickerFrontUnit front = host.Machine.PickerFrontUnit;
                if (front != null && front.Axes != null && front.Axes.TryGetValue(axis, out item))
                    return item;
                return null;
            }

            PickerRearUnit rear = host.Machine.PickerRearUnit;
            if (rear != null && rear.Axes != null && rear.Axes.TryGetValue(axis, out item))
                return item;

            return null;
        }

        private static BaseAxis ResolveInputStageAxis(InputStageUnit stage, WaferStageAxis axis)
        {
            if (stage == null)
                return null;

            switch (axis)
            {
                case WaferStageAxis.WaferY:
                    return stage.StageY;
                case WaferStageAxis.VisionX:
                    return stage.CameraX;
                case WaferStageAxis.NeedleX:
                    return stage.NeedleBlockX;
                case WaferStageAxis.NeedleZ:
                    return stage.NeedleZ;
                case WaferStageAxis.EjectPinZ:
                    return stage.EjectPinZ;
                case WaferStageAxis.WaferExpandingZ:
                    return stage.ExpanderZ;
                case WaferStageAxis.WaferT:
                    return stage.StageT;
                default:
                    return null;
            }
        }

        private static bool IsPickerXInPosition(Form1 host, PickerSequenceSide side, double targetPickerX)
        {
            if (host == null || host.Machine == null)
                return false;

            if (side == PickerSequenceSide.Front)
            {
                PickerFrontUnit front = host.Machine.PickerFrontUnit;
                return front != null &&
                    front.PickerX != null &&
                    front.IsFrontPickerAxisInPosition(
                        PickerAxis.PickerX,
                        targetPickerX,
                        ResolvePickerTolerance(front.PickerX));
            }

            PickerRearUnit rear = host.Machine.PickerRearUnit;
            return rear != null &&
                rear.PickerX != null &&
                rear.IsRearPickerAxisInPosition(
                    PickerAxis.PickerX,
                    targetPickerX,
                    ResolvePickerTolerance(rear.PickerX));
        }

        private static bool IsPickerYInPosition(Form1 host, PickerSequenceSide side, double targetPickerY)
        {
            if (host == null || host.Machine == null)
                return false;

            if (side == PickerSequenceSide.Front)
            {
                PickerFrontUnit front = host.Machine.PickerFrontUnit;
                return front != null &&
                    front.PickerY != null &&
                    front.IsFrontPickerAxisInPosition(
                        PickerAxis.PickerY,
                        targetPickerY,
                        ResolvePickerTolerance(front.PickerY));
            }

            PickerRearUnit rear = host.Machine.PickerRearUnit;
            return rear != null &&
                rear.PickerY != null &&
                rear.IsRearPickerAxisInPosition(
                    PickerAxis.PickerY,
                    targetPickerY,
                    ResolvePickerTolerance(rear.PickerY));
        }

        private static BaseAxis ResolvePickerYAxis(Form1 host, PickerSequenceSide side)
        {
            if (host == null || host.Machine == null)
                return null;

            if (side == PickerSequenceSide.Front)
                return host.Machine.PickerFrontUnit != null ? host.Machine.PickerFrontUnit.PickerY : null;

            return host.Machine.PickerRearUnit != null ? host.Machine.PickerRearUnit.PickerY : null;
        }

        private static bool IsPickerYForwardTargetInvalid(
            Form1 host,
            PickerSequenceSide side,
            double targetPickerY,
            double tolerance)
        {
            try
            {
                if (host == null || host.Machine == null)
                    return true;

                double avoid = InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                    host.Machine,
                    side,
                    PickerAxis.PickerY,
                    "AvoidPosition");
                double tol = tolerance > 0.0 ? tolerance : 0.05;
                return Math.Abs(targetPickerY) <= tol || Math.Abs(targetPickerY - avoid) <= tol;
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => SetActionButtonsEnabled(enabled)));
                    return;
                }

                btnManualAlignComplete.Enabled = enabled;
                btnNeedleBlockDown.Enabled = enabled;
                btnThetaMatchMove.Enabled = enabled;
                btnXyMatchMove.Enabled = enabled;
                btnManualDieMapOffsetApply.Enabled = enabled;
                btnPickStatusSave.Enabled = enabled;
                btnReloadActiveMap.Enabled = enabled;
                btnApplyDieState.Enabled = enabled;
                rdoDieStateWait.Enabled = enabled;
                rdoDieStateGood.Enabled = enabled;
                rdoDieStateNg.Enabled = enabled;
                rdoDieStateSkip.Enabled = enabled;
                gridDieList.Enabled = enabled;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static InputStageSequence CreateInputStageSequence(Form1 host)
        {
            var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            return new InputStageSequence(ctx);
        }

        private static InputStageSequenceOptions BuildInputStageOptions(Form1 host)
        {
            var options = InputStageSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = SequenceStartMode.Resume;
            options.FineMove = false;
            options.RequireVisionAlign = false;
            options.RequireMapData = false;
            options.WaferId = ResolveInputStageWaferId();
            ApplyInputStageUnitParameters(host, options);
            return options;
        }

        private static void ApplyInputStageUnitParameters(Form1 host, InputStageSequenceOptions options)
        {
            try
            {
                var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                if (stage == null || stage.Config == null || options == null)
                    return;

                if (stage.Config.SequenceMoveTimeoutMs > 0)
                    options.MoveTimeoutMs = stage.Config.SequenceMoveTimeoutMs;
                if (stage.Config.AlignConvergenceThresholdDeg > 0.0)
                    options.AlignThetaToleranceDeg = stage.Config.AlignConvergenceThresholdDeg;
                if (stage.Config.AlignThetaCorrectionLimitDeg > 0.0)
                    options.AlignThetaCorrectionLimitDeg = stage.Config.AlignThetaCorrectionLimitDeg;
                if (stage.Config.MaxAlignIterations > 0)
                    options.AlignRetryCount = stage.Config.MaxAlignIterations;
                if (stage.Recipe != null && stage.Recipe.DieMap != null)
                {
                    if (!string.IsNullOrWhiteSpace(stage.Recipe.DieMap.VisionTargetId))
                        options.DieMapVisionTargetId = stage.Recipe.DieMap.VisionTargetId;
                    if (stage.Recipe.DieMap.VisionRetryCount > 0)
                        options.DieMapVisionRetryCount = stage.Recipe.DieMap.VisionRetryCount;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "InputStage sequence option parameter apply failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static string ResolveInputStageWaferId()
        {
            try
            {
                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                return wafer != null ? wafer.WaferId ?? "" : "";
            }
            catch
            {
                return "";
            }
            finally
            {
            }
        }

        private void SelectEntry(DieMapEntry entry)
        {
            try
            {
                if (entry == null)
                    return;

                _selectedEntry = entry;
                lblAxisX.Text = entry.PosX.ToString("F3");
                lblAxisY.Text = entry.PosY.ToString("F3");
                lblBinRank.Text = entry.BinCode.ToString();
                lblDieNum.Text = string.Format("[{0},{1}] / {2}", ResolveEntryMapX(entry), ResolveEntryMapY(entry),
                    CountVisibleInputDieMapEntries(mapView.Map));
                SetDieStateRadioFromEntry(entry);
                SelectGridRow(entry);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void SelectEntryByGridRow(int rowIndex)
        {
            try
            {
                if (mapView == null || mapView.Map == null || rowIndex < 0 || rowIndex >= gridDieList.Rows.Count)
                    return;

                DieMapEntry entry = gridDieList.Rows[rowIndex].Tag as DieMapEntry;
                if (entry != null)
                    SelectEntry(entry);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ToggleSelectedEntryTarget()
        {
            try
            {
                if (_selectedEntry == null)
                    return;

                _selectedEntry.IsTarget = !_selectedEntry.IsTarget;
                if (!_selectedEntry.IsTarget)
                {
                    _selectedEntry.Result = DieResult.NG;
                    _selectedEntry.BinCode = 255;
                }
                else
                {
                    _selectedEntry.Result = DieResult.Unknown;
                    _selectedEntry.BinCode = 0;
                }

                if (mapView != null && mapView.Map != null)
                    PickupSequenceGenerator.ApplySequenceNumbers(mapView.Map, ResolveInputPickupSubsetFromRecipe());

                _pickStatusDirty = true;
                RefreshDieGrid();
                SetDieStateRadioFromEntry(_selectedEntry);
                SelectGridRow(_selectedEntry);
                mapView.Invalidate();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ApplySelectedDieState()
        {
            try
            {
                DieMap map = mapView != null ? mapView.Map : null;
                if (map == null || map.Entries == null)
                {
                    QMC.Common.MessageDialog.Show(this, "상태를 변경할 Active Input Die Map 데이터가 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DieMapEntry entry = FindEquivalentEntry(map, _selectedEntry);
                if (entry == null)
                {
                    QMC.Common.MessageDialog.Show(this, "상태를 변경할 Die를 먼저 선택하세요.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string reason;
                if (!CanEditSelectedDieState(entry, out reason))
                {
                    QMC.Common.MessageDialog.Show(this, reason,
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                InputDieManualState state = ResolveSelectedDieManualState();
                string stateText = ResolveManualStateDisplayName(state);
                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    "선택 Die 상태를 [" + stateText + "]로 변경하시겠습니까?\r\n" +
                    "Die=" + BuildSelectedDieText(entry) + "\r\n" +
                    "UID=" + (entry.DieUid ?? ""),
                    "Input Die Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                ApplyManualStateToEntry(entry, state);
                PickupSequenceGenerator.ApplySequenceNumbers(map, ResolveInputPickupSubsetFromRecipe());

                LotStorage.ActiveInputDieMap = map;
                PersistPickStatusToMaterialState(map);
                SyncManualInputPickVisionInspection(entry, state);

                var host = FindForm() as Form1;
                if (host != null && host.Controller != null)
                    host.Controller.ApplyInputDieMap(map, "InputStageMapTransferPage.ApplySelectedDieState");

                MaterialStateService.TryFlushPendingSave("InputMapManualDieState");

                _selectedEntry = entry;
                _pickStatusDirty = false;
                _suppressLotProgressOverlay = true;
                _lastMapSignature = BuildMapSignature(map);
                _lastMapFrameObjId = map.FrameObjId ?? "";
                RefreshDieGrid();
                SelectEntry(entry);
                mapView.Invalidate();

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Input Die 상태 변경 완료. die=" + (entry.DieUid ?? "") +
                    ", grid=(" + ResolveEntryMapX(entry) + "," + ResolveEntryMapY(entry) + ")" +
                    ", state=" + stateText + " - Ok");
                QMC.Common.MessageDialog.Show(this, "선택 Die 상태 변경 완료.",
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Input Die 상태 변경 실패: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "Input Die 상태 변경 실패:\r\n" + ex.Message,
                    "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private bool CanEditSelectedDieState(DieMapEntry entry, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (_manualMoveBusy)
                {
                    reason = "좌표 이동 동작 중에는 Die 상태를 변경할 수 없습니다.";
                    return false;
                }

                var host = FindForm() as Form1;
                var controller = host != null ? host.Controller : null;
                if (controller != null)
                {
                    if (controller.Status == EquipmentStatus.AutoRunning ||
                        controller.Status == EquipmentStatus.Initializing ||
                        controller.IsSequenceRunning ||
                        controller.IsManualBusy)
                    {
                        reason = "장비 동작 중에는 Active Die 상태를 변경할 수 없습니다.\r\n" +
                                 "Auto/Manual 동작을 정지한 뒤 다시 시도하세요.";
                        return false;
                    }
                }

                DieMaterial die = MaterialStateService.GetDieMaterial(entry != null ? entry.DieUid : "");
                if (die == null)
                    return true;

                if ((die.ReservedPickerLocation == MaterialLocationKind.PickerFront ||
                     die.ReservedPickerLocation == MaterialLocationKind.PickerRear) &&
                    die.ReservedPickerNo > 0)
                {
                    reason = "선택 Die는 Picker 예약 상태라 변경할 수 없습니다.\r\n" +
                             "예약 해제 또는 시퀀스 정지 상태를 확인하세요.";
                    return false;
                }

                MaterialLocation location = die.CurrentLocation;
                MaterialLocationKind kind = location != null ? location.Kind : MaterialLocationKind.Unknown;
                if (kind != MaterialLocationKind.InputStage && kind != MaterialLocationKind.Unknown)
                {
                    reason = "선택 Die는 이미 InputStage를 벗어나 상태 변경이 차단되었습니다.\r\n" +
                             "현재 위치=" + kind;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "Die 상태 변경 가능 여부 확인 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    reason + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private InputDieManualState ResolveSelectedDieManualState()
        {
            try
            {
                if (rdoDieStateGood.Checked)
                    return InputDieManualState.InspectionGood;
                if (rdoDieStateNg.Checked)
                    return InputDieManualState.InspectionNg;
                if (rdoDieStateSkip.Checked)
                    return InputDieManualState.PickSkip;

                return InputDieManualState.InspectionWait;
            }
            catch
            {
                return InputDieManualState.InspectionWait;
            }
            finally
            {
            }
        }

        private static void ApplyManualStateToEntry(DieMapEntry entry, InputDieManualState state)
        {
            if (entry == null)
                return;

            switch (state)
            {
                case InputDieManualState.InspectionGood:
                    entry.IsTarget = true;
                    entry.Result = DieResult.Good;
                    entry.BinCode = BinCodeMap.GoodBin;
                    return;
                case InputDieManualState.InspectionNg:
                    entry.IsTarget = true;
                    entry.Result = DieResult.NG;
                    entry.BinCode = BinCodeMap.MaxBin;
                    return;
                case InputDieManualState.PickSkip:
                    entry.IsTarget = false;
                    entry.Result = DieResult.NG;
                    entry.BinCode = BinCodeMap.MaxBin;
                    entry.SequenceNo = 0;
                    return;
                case InputDieManualState.InspectionWait:
                default:
                    entry.IsTarget = true;
                    entry.Result = DieResult.Unknown;
                    entry.BinCode = 0;
                    return;
            }
        }

        private void SetDieStateRadioFromEntry(DieMapEntry entry)
        {
            try
            {
                switch (ResolveEntryManualState(entry))
                {
                    case InputDieManualState.InspectionGood:
                        rdoDieStateGood.Checked = true;
                        break;
                    case InputDieManualState.InspectionNg:
                        rdoDieStateNg.Checked = true;
                        break;
                    case InputDieManualState.PickSkip:
                        rdoDieStateSkip.Checked = true;
                        break;
                    case InputDieManualState.InspectionWait:
                    default:
                        rdoDieStateWait.Checked = true;
                        break;
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static InputDieManualState ResolveEntryManualState(DieMapEntry entry)
        {
            if (entry == null)
                return InputDieManualState.InspectionWait;

            if (!entry.IsTarget)
                return InputDieManualState.PickSkip;
            if (entry.Result == DieResult.Good)
                return InputDieManualState.InspectionGood;
            if (entry.Result == DieResult.NG)
                return InputDieManualState.InspectionNg;

            return InputDieManualState.InspectionWait;
        }

        private static string ResolveManualStateDisplayName(InputDieManualState state)
        {
            switch (state)
            {
                case InputDieManualState.InspectionGood:
                    return "검사 완료(Good)";
                case InputDieManualState.InspectionNg:
                    return "검사 NG";
                case InputDieManualState.PickSkip:
                    return "픽업 제외";
                case InputDieManualState.InspectionWait:
                default:
                    return "검사 대기";
            }
        }

        private void SyncManualInputPickVisionInspection(DieMapEntry entry, InputDieManualState state)
        {
            try
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                    return;

                if (state == InputDieManualState.InspectionWait ||
                    state == InputDieManualState.PickSkip)
                {
                    MaterialStateService.RemoveInspection(entry.DieUid, "InputPickVision");
                    return;
                }

                var record = new DieInspectionRecord
                {
                    InspectionType = "InputPickVision",
                    Result = state == InputDieManualState.InspectionNg
                        ? MaterialInspectionResult.Ng
                        : MaterialInspectionResult.Ok,
                    Offset = new VisionOffset
                    {
                        X = entry.PosX,
                        Y = entry.PosY,
                        R = 0.0,
                        IsValid = true
                    },
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                if (state == InputDieManualState.InspectionNg)
                    record.NgCodes.Add("ManualInputMapEdit");

                MaterialStateService.UpsertInspection(entry.DieUid, record);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "수동 InputPickVision 검사 상태 동기화 실패: die=" +
                    (entry != null ? entry.DieUid ?? "" : "") +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private System.Drawing.Color ResolveInputDieMapCellColor(DieMapEntry entry)
        {
            try
            {
                if (entry == null || !entry.IsTarget)
                    return SkipColor;

                InputDieMapCellState state = ResolveInputDieMapCellState(entry);
                if (state == InputDieMapCellState.PickComplete)
                    return PickCompleteColor;

                if (entry.Result == DieResult.NG)
                {
                    int binCode = entry.BinCode > 0 ? entry.BinCode : BinCodeMap.MaxBin;
                    System.Drawing.Color color = BinCodeMap.ConvertToBinCodeColor(binCode);
                    return color.ToArgb() == System.Drawing.Color.Black.ToArgb()
                        ? System.Drawing.Color.IndianRed
                        : color;
                }

                if (state == InputDieMapCellState.InspectionDone || entry.Result == DieResult.Good)
                    return InspectionDoneColor;

                if (state == InputDieMapCellState.InspectionWait)
                    return InspectionWaitColor;

                if (entry.BinCode > 0)
                    return BinCodeMap.ConvertToBinCodeColor(entry.BinCode);

                return InspectionWaitColor;
            }
            catch
            {
                return InspectionWaitColor;
            }
            finally
            {
            }
        }

        private string ResolveInputDieMapCellStatusText(DieMapEntry entry)
        {
            try
            {
                if (entry == null)
                    return "";
                if (!entry.IsTarget)
                    return "픽업 제외";

                InputDieMapCellState state = ResolveInputDieMapCellState(entry);
                if (state == InputDieMapCellState.PickComplete)
                    return "픽업완료";
                if (entry.Result == DieResult.NG)
                    return "검사NG";
                if (state == InputDieMapCellState.InspectionDone || entry.Result == DieResult.Good)
                    return "검사완료";

                return "검사대기";
            }
            catch
            {
                return "";
            }
            finally
            {
            }
        }

        private InputDieMapCellState ResolveInputDieMapCellState(DieMapEntry entry)
        {
            if (entry == null)
                return InputDieMapCellState.None;

            InputDieMapCellState state;
            if (_inputDieMapCellStates != null &&
                _inputDieMapCellStates.TryGetValue(BuildEntryGridKey(entry), out state))
                return state;

            return InputDieMapCellState.InspectionWait;
        }

        private Tuple<string, System.Drawing.Color>[] BuildInputDieMapLegendItems()
        {
            return new[]
            {
                Tuple.Create("검사대기", InspectionWaitColor),
                Tuple.Create("검사완료", InspectionDoneColor),
                Tuple.Create("픽업완료", PickCompleteColor),
                Tuple.Create("NG", System.Drawing.Color.IndianRed),
                Tuple.Create("제외", SkipColor),
            };
        }

        private void RefreshInputDieMapDisplayState(DieMap map)
        {
            try
            {
                Dictionary<string, InputDieMapCellState> states;
                _inputDieMapStats = BuildInputDieMapStats(map, out states);
                _inputDieMapCellStates = states;
                mapView.Caption = BuildInputDieMapCaption(map, _inputDieMapStats);
            }
            catch (Exception ex)
            {
                _inputDieMapCellStates = new Dictionary<string, InputDieMapCellState>(StringComparer.Ordinal);
                _inputDieMapStats = new InputDieMapStats();
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Input Die Map 표시 상태 갱신 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private InputDieMapStats BuildInputDieMapStats(
            DieMap map,
            out Dictionary<string, InputDieMapCellState> states)
        {
            states = new Dictionary<string, InputDieMapCellState>(StringComparer.Ordinal);
            var stats = new InputDieMapStats();
            try
            {
                if (map == null || map.Entries == null)
                    return stats;

                Dictionary<string, DieMaterial> dieById;
                Dictionary<string, DieMaterial> dieByGrid;
                BuildInputDieMaterialLookup(out dieById, out dieByGrid);

                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null)
                        continue;
                    if (!IsVisibleInputDieMapEntry(entry))
                        continue;

                    DieMaterial die = ResolveEntryDieMaterial(entry, dieById, dieByGrid);
                    InputDieMapCellState state = ResolveInputDieMapCellState(die, entry);
                    states[BuildEntryGridKey(entry)] = state;

                    if (!entry.IsTarget)
                        continue;

                    stats.Target++;
                    if (state == InputDieMapCellState.PickComplete)
                        stats.PickComplete++;
                    else if (state == InputDieMapCellState.InspectionDone || entry.Result == DieResult.Good)
                        stats.InspectionDone++;
                    else
                        stats.InspectionWait++;

                    if (entry.Result == DieResult.Good)
                    {
                        stats.Good++;
                        stats.Done++;
                    }
                    else if (entry.Result == DieResult.NG)
                    {
                        stats.Ng++;
                        stats.Done++;
                    }
                }

                return stats;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Input Die Map 표시 통계 계산 실패: " + ex.Message + " - Failed");
                return stats;
            }
            finally
            {
            }
        }

        private static void BuildInputDieMaterialLookup(
            out Dictionary<string, DieMaterial> dieById,
            out Dictionary<string, DieMaterial> dieByGrid)
        {
            dieById = new Dictionary<string, DieMaterial>(StringComparer.OrdinalIgnoreCase);
            dieByGrid = new Dictionary<string, DieMaterial>(StringComparer.Ordinal);

            MaterialSnapshot state = MaterialStorage.State;
            if (state == null || state.Dies == null)
                return;

            foreach (DieMaterial die in state.Dies)
            {
                if (die == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(die.DieId) && !dieById.ContainsKey(die.DieId))
                    dieById.Add(die.DieId, die);

                if (die.Wafer_IndexX >= 0 && die.Wafer_IndexY >= 0)
                {
                    string key = BuildGridKey(die.Wafer_IndexX, die.Wafer_IndexY);
                    if (!dieByGrid.ContainsKey(key))
                        dieByGrid.Add(key, die);
                }
            }
        }

        private static DieMaterial ResolveEntryDieMaterial(
            DieMapEntry entry,
            Dictionary<string, DieMaterial> dieById,
            Dictionary<string, DieMaterial> dieByGrid)
        {
            if (entry == null)
                return null;

            DieMaterial die = null;
            if (!string.IsNullOrWhiteSpace(entry.DieUid) && dieById != null)
                dieById.TryGetValue(entry.DieUid, out die);
            if (die == null && dieByGrid != null)
                dieByGrid.TryGetValue(BuildEntryGridKey(entry), out die);

            return die;
        }

        private static InputDieMapCellState ResolveInputDieMapCellState(DieMaterial die, DieMapEntry entry)
        {
            if (die == null)
                return entry != null && entry.Result == DieResult.Good
                    ? InputDieMapCellState.InspectionDone
                    : InputDieMapCellState.InspectionWait;

            if (IsInputDiePicked(die))
                return InputDieMapCellState.PickComplete;

            if (HasInputPickVisionInspection(die) ||
                (entry != null && entry.Result == DieResult.Good))
                return InputDieMapCellState.InspectionDone;

            return InputDieMapCellState.InspectionWait;
        }

        private static bool IsInputDiePicked(DieMaterial die)
        {
            if (die == null)
                return false;

            if (HasValidPickedAt(die.PickedAt) ||
                die.PickedPickerLocation == MaterialLocationKind.PickerFront ||
                die.PickedPickerLocation == MaterialLocationKind.PickerRear ||
                die.PickedPickerNo >= 0)
                return true;

            return die.CurrentLocation != null &&
                   (die.CurrentLocation.Kind == MaterialLocationKind.PickerFront ||
                    die.CurrentLocation.Kind == MaterialLocationKind.PickerRear ||
                    die.CurrentLocation.Kind == MaterialLocationKind.OutputStageGood ||
                    die.CurrentLocation.Kind == MaterialLocationKind.OutputStageNg ||
                    die.CurrentLocation.Kind == MaterialLocationKind.OutputFeeder ||
                    die.CurrentLocation.Kind == MaterialLocationKind.OutputCassette);
        }

        private static bool HasValidPickedAt(DateTime pickedAt)
        {
            return pickedAt > new DateTime(2000, 1, 1);
        }

        private static bool HasInputPickVisionInspection(DieMaterial die)
        {
            if (die == null || die.Inspections == null)
                return false;

            foreach (DieInspectionRecord record in die.Inspections)
            {
                if (record == null)
                    continue;

                if (string.Equals(record.InspectionType, "InputPickVision", StringComparison.OrdinalIgnoreCase) &&
                    record.Result != MaterialInspectionResult.Unknown)
                    return true;
            }

            return false;
        }

        private static string BuildGridKey(int x, int y)
        {
            return x.ToString() + ":" + y.ToString();
        }

        private static int ResolveEntryMapX(DieMapEntry entry)
        {
            return DieMapGenerator.ResolveMapIndexX(entry);
        }

        private static int ResolveEntryMapY(DieMapEntry entry)
        {
            return DieMapGenerator.ResolveMapIndexY(entry);
        }

        private static string BuildEntryGridKey(DieMapEntry entry)
        {
            return BuildGridKey(ResolveEntryMapX(entry), ResolveEntryMapY(entry));
        }

        private static string BuildEntryMapText(DieMapEntry entry)
        {
            return "[" + ResolveEntryMapX(entry) + "," + ResolveEntryMapY(entry) + "]";
        }

        private string BuildInputDieMapCaption(DieMap map, InputDieMapStats stats)
        {
            try
            {
                Lot lot = LotStorage.ActiveLot;
                string lotText = lot != null ? lot.LotID : "(no active lot)";
                if (map == null)
                    return "INPUT WAFER MAP   LOT " + lotText + "  (no input die map)";

                return string.Format(
                    "INPUT WAFER MAP   LOT {0}  target={1}  wait={2}  vision={3}  pick={4}  good={5}  ng={6}",
                    lotText,
                    stats.Target,
                    stats.InspectionWait,
                    stats.InspectionDone,
                    stats.PickComplete,
                    stats.Good,
                    stats.Ng);
            }
            catch
            {
                return "INPUT WAFER MAP";
            }
            finally
            {
            }
        }

        private void RefreshDieGrid()
        {
            try
            {
                DieMap map = mapView != null ? mapView.Map : null;
                RefreshInputDieMapDisplayState(map);
                if (map == null || map.Entries == null)
                {
                    gridDieList.Rows.Clear();
                    UpdateMapCountLabels(null);
                    return;
                }

                gridDieList.SuspendLayout();
                try
                {
                    gridDieList.Rows.Clear();
                    foreach (DieMapEntry entry in BuildDisplayEntries(map))
                    {
                        if (entry == null)
                            continue;

                        int rowIndex = gridDieList.Rows.Add(
                            entry.IsTarget && entry.SequenceNo > 0 ? (object)entry.SequenceNo : "",
                            ResolveEntryMapX(entry),
                            ResolveEntryMapY(entry),
                            MaterialStateService.ResolveInputDieDisplayState(entry),
                            entry.Result,
                            entry.BinCode,
                            entry.PosX.ToString("F4"),
                            entry.PosY.ToString("F4"),
                            entry.DieUid ?? "");
                        gridDieList.Rows[rowIndex].Tag = entry;
                    }
                }
                finally
                {
                    gridDieList.ResumeLayout();
                }

                UpdateMapCountLabels(map);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void UpdateMapCountLabels(DieMap map)
        {
            try
            {
                if (map == null || map.Entries == null)
                {
                    lblBinValue.Text = "0";
                    lblDieNum.Text = "0 / 0";
                    return;
                }

                int target = 0;
                int good = 0;
                int ng = 0;
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null)
                        continue;
                    if (!IsVisibleInputDieMapEntry(entry))
                        continue;
                    if (entry.IsTarget)
                        target++;
                    if (entry.Result == DieResult.Good)
                        good++;
                    else if (entry.Result == DieResult.NG)
                        ng++;
                }

                int displayCount = CountVisibleInputDieMapEntries(map);
                lblBinValue.Text = target.ToString();
                lblDieNum.Text = "TARGET " + target + " / MAP " + displayCount + " / G " + good + " / NG " + ng;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private System.Collections.Generic.List<DieMapEntry> BuildDisplayEntries(DieMap map)
        {
            try
            {
                if (map == null || map.Entries == null)
                    return new System.Collections.Generic.List<DieMapEntry>();

                return map.Entries
                    .Where(IsVisibleInputDieMapEntry)
                    .OrderBy(entry => entry.IsTarget ? 0 : 1)
                    .ThenBy(entry => entry.SequenceNo <= 0 ? int.MaxValue : entry.SequenceNo)
                    .ThenBy(entry => ResolveEntryMapY(entry))
                    .ThenBy(entry => ResolveEntryMapX(entry))
                    .ToList();
            }
            catch
            {
                return map != null && map.Entries != null
                    ? map.Entries.Where(IsVisibleInputDieMapEntry).ToList()
                    : new System.Collections.Generic.List<DieMapEntry>();
            }
            finally
            {
            }
        }

        private static bool IsVisibleInputDieMapEntry(DieMapEntry entry)
        {
            // 현재 기준: 전환 화면은 실제 처리 대상 die만 표시하고, 빈 grid SKIP 셀은 숨긴다.
            return entry != null && entry.IsTarget;
        }

        private static int CountVisibleInputDieMapEntries(DieMap map)
        {
            try
            {
                if (map == null || map.Entries == null)
                    return 0;

                int count = 0;
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (IsVisibleInputDieMapEntry(entry))
                        count++;
                }

                return count;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
        }

        private void SelectGridRow(DieMapEntry entry)
        {
            try
            {
                if (entry == null)
                    return;

                foreach (DataGridViewRow row in gridDieList.Rows)
                {
                    DieMapEntry rowEntry = row.Tag as DieMapEntry;
                    if (rowEntry != null &&
                        ResolveEntryMapX(rowEntry) == ResolveEntryMapX(entry) &&
                        ResolveEntryMapY(rowEntry) == ResolveEntryMapY(entry) &&
                        string.Equals(rowEntry.DieUid ?? "", entry.DieUid ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        gridDieList.ClearSelection();
                        row.Selected = true;
                        if (row.Index >= 0 && row.Cells.Count > 0)
                            gridDieList.CurrentCell = row.Cells[0];

                        if (row.Index >= 0 && !row.Displayed)
                            gridDieList.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
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

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                _refresh?.Stop();
                _refresh?.Dispose();
            }
            catch { }

            base.OnHandleDestroyed(e);
        }
    }
}

