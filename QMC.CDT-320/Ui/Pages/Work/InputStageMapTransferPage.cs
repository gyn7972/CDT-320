using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using System.Linq;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Localization;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class InputStageMapTransferPage : PageBase
    {
        private Timer _refresh;
        private string _i18nTitle;
        private string _lastMapFrameObjId = "";
        private string _lastMapSignature = "";
        private DieMapEntry _selectedEntry;
        private bool _pickStatusDirty;
        private ContextMenuStrip _gridMenu;
        private ToolStripMenuItem _gridMoveMenuItem;
        private ToolStripMenuItem[] _gridMoveFrontPickerMenuItems;
        private ToolStripMenuItem[] _gridMoveRearPickerMenuItems;
        private bool _manualMoveBusy;

        public InputStageMapTransferPage() : this("work.page.inputMap")
        {
        }

        public InputStageMapTransferPage(string titleI18n)
        {
            _i18nTitle = titleI18n;
            InitializeComponent();
            ApplyTitle();
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
            btnManualAlignComplete.Click += (s, e) => MarkManualAlignComplete();
            btnNeedleBlockDown.Click += (s, e) => ShowNotReadyAction("NEEDLE BLOCK DOWN", "Needle Block Down 단위동작 함수가 아직 연결되어 있지 않습니다.");
            btnThetaMatchMove.Click += async (s, e) => await RunInputStageSequenceActionAsync(
                "THETA MATCH MOVE",
                host => CreateInputStageSequence(host).RunAlignAsync(host.Controller.ManualOperationToken, BuildInputStageOptions(host)));
            btnXyMatchMove.Click += async (s, e) => await RunInputStageSequenceActionAsync(
                "X/Y MATCH MOVE",
                host => CreateInputStageSequence(host).RunDieMappingAsync(host.Controller.ManualOperationToken, BuildInputStageOptions(host)));
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
                _gridMenu.Opening += (s, e) =>
                {
                    bool enabled = _selectedEntry != null && !_manualMoveBusy;
                    if (_gridMoveMenuItem != null)
                        _gridMoveMenuItem.Enabled = enabled;

                    SetPickerMoveMenuEnabled(_gridMoveFrontPickerMenuItems, enabled);
                    SetPickerMoveMenuEnabled(_gridMoveRearPickerMenuItems, enabled);
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
                    map.Entries.Add(new DieMapEntry
                    {
                        Index = index++,
                        DieMapX = col,
                        DieMapY = row,
                        IsTarget = target,
                        Result = target ? DieResult.Unknown : DieResult.NG,
                        BinCode = target ? 0 : 255,
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
                _selectedEntry = FindEquivalentEntry(map, previousSelection);

                lblMapTitle.Text = title;
                lblProjectValue.Text = GetCurrentProjectName();
                lblBarcodeValue.Text = ResolveActiveWaferId();
                lblBinValue.Text = "0";
                lblPitchX.Text = map != null ? map.PitchX.ToString("F3") : "0";
                lblPitchY.Text = map != null ? map.PitchY.ToString("F3") : "0";
                lblDieNum.Text = "0 / " + (map != null ? map.TotalCells.ToString() : "0");
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
                    statusHash = statusHash * 31 +
                        entry.Index * 3 +
                        entry.DieMapX * 5 +
                        entry.DieMapY * 7 +
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

                return map.GetCell(entry.DieMapX, entry.DieMapY);
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
            if (_pickStatusDirty)
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

            lblDieNum.Text = processed + " / " + map.TotalCells;
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

        private void PersistPickStatusToMaterialState(DieMap map)
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

                    if (string.IsNullOrWhiteSpace(entry.DieUid))
                        entry.DieUid = "INPUT-D" + entry.DieMapY.ToString("000") + "-" + entry.DieMapX.ToString("000");

                    DieMaterial die = MaterialStateService.GetOrCreateDieMaterial(entry.DieUid);
                    if (wafer != null)
                    {
                        die.WaferID_Input = wafer.WaferId;
                        if (!wafer.DieIds.Contains(die.DieId))
                            wafer.DieIds.Add(die.DieId);
                    }

                    die.Wafer_IndexX = entry.DieMapX;
                    die.Wafer_IndexY = entry.DieMapY;
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

                MaterialStateService.NotifyAndSave("MapTransferPickStatusSave");
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
            try
            {
                DieMapEntry entry = _selectedEntry;
                if (entry == null)
                {
                    QMC.Common.MessageDialog.Show(this, "이동할 다이가 선택되지 않았습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string indexText = entry.SequenceNo > 0
                    ? entry.SequenceNo.ToString()
                    : "[" + entry.DieMapX + "," + entry.DieMapY + "]";
                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    "Die " + indexText + "의 X,Y 좌표로 이동하시겠습니까?\r\n" +
                    "X=" + entry.PosX.ToString("F3") + " mm, Y=" + entry.PosY.ToString("F3") + " mm",
                    "Input Die Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                Form1 host = FindForm() as Form1;
                if (host == null || host.Machine == null || host.Machine.InputStageUnit == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage 장비 정보를 찾을 수 없습니다.",
                        "Input Die Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                InputStageUnit stage = host.Machine.InputStageUnit;
                SetActionButtonsEnabled(false);
                _manualMoveBusy = true;

                int prepareResult = await AwaitManualMoveStepAsync(
                    MovePickersToAvoidForVisionMoveAsync(host),
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
                        true,
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
                _manualMoveBusy = false;
                SetActionButtonsEnabled(true);
            }
        }

        private async Task MoveSelectedDieByPickerAsync(PickerSequenceSide side, int pickerNo)
        {
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

                double targetPickerX = entry.PosX + offsetX;
                double targetStageY = entry.PosY + offsetY;
                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    ResolvePickerMoveTitle(side, pickerNo) + "를 선택 다이 위치로 이동하시겠습니까?\r\n" +
                    "Die=" + BuildSelectedDieText(entry) + "\r\n" +
                    "PickerX=" + targetPickerX.ToString("F3") + " mm\r\n" +
                    "StageY=" + targetStageY.ToString("F3") + " mm\r\n" +
                    "(InputVision Offset X=" + offsetX.ToString("F3") + " mm, Y=" + offsetY.ToString("F3") + " mm)",
                    "Input Die Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                SetActionButtonsEnabled(false);
                _manualMoveBusy = true;

                int result = await AwaitManualMoveStepAsync(
                    MoveSelectedDieByPickerCoreAsync(host, side, pickerNo, entry, targetPickerX, targetStageY),
                    ResolveManualMoveTimeoutMs(host),
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
                _manualMoveBusy = false;
                SetActionButtonsEnabled(true);
            }
        }

        private async Task<int> MoveSelectedDieByPickerCoreAsync(
            Form1 host,
            PickerSequenceSide side,
            int pickerNo,
            DieMapEntry entry,
            double targetPickerX,
            double targetStageY)
        {
            try
            {
                InputStageUnit stage = host.Machine.InputStageUnit;
                string areaReason;
                if (!stage.IsInputStageWorkPointInArea(entry.PosX, targetStageY, out areaReason))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerMoveTitle(side, pickerNo) +
                        " blocked: target is outside work area. dieX=" + entry.PosX.ToString("F3") +
                        ", stageY=" + targetStageY.ToString("F3") +
                        ", reason=" + areaReason + " - Check");
                    return -1;
                }

                int result = await MoveInputVisionToAvoidForPickerMoveAsync(stage).ConfigureAwait(true);
                if (result != 0)
                    return result;

                result = await MoveOppositePickerToAvoidForManualPickerMoveAsync(host, side).ConfigureAwait(true);
                if (result != 0)
                    return result;

                Task<int> moveStageY = PickerInputStageMoveHelper.MoveStageYForPickerWorkPointCommandAsync(
                    stage,
                    entry.PosX,
                    targetStageY,
                    true,
                    "InputStageMapTransferPickerMove");
                string pickerTargetName = "DiePickPosition[" + (pickerNo - 1) + "];ManualInputDieMapMove";
                Task<int> movePickerX = side == PickerSequenceSide.Front
                    ? host.Machine.PickerFrontUnit.MoveFrontPickerAxis(PickerAxis.PickerX, targetPickerX, true, pickerTargetName)
                    : host.Machine.PickerRearUnit.MoveRearPickerAxis(PickerAxis.PickerX, targetPickerX, true, pickerTargetName);
                int[] moveResults = await Task.WhenAll(moveStageY, movePickerX).ConfigureAwait(true);

                if (moveResults[0] != 0)
                    return moveResults[0];
                if (moveResults[1] != 0)
                    return moveResults[1];

                Task<int> waitStageY = stage.WaitInputStageAxisInPosition(
                    WaferStageAxis.WaferY,
                    targetStageY,
                    ResolveStageMoveTimeoutMs(stage));
                Task<int> waitPickerX = WaitPickerXMoveDoneAsync(host, side, targetPickerX);
                int[] waitResults = await Task.WhenAll(waitStageY, waitPickerX).ConfigureAwait(true);

                if (waitResults[0] != 0)
                    return waitResults[0];
                if (waitResults[1] != 0)
                    return waitResults[1];

                if (!IsStageYInPosition(stage, targetStageY))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerMoveTitle(side, pickerNo) +
                        " final check failed: StageY target=" + targetStageY.ToString("F3") +
                        ", actual=" + stage.StageY.ActualPosition.ToString("F3") + " - Failed");
                    return -1;
                }

                if (!IsPickerXInPosition(host, side, targetPickerX))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                        ResolvePickerMoveTitle(side, pickerNo) +
                        " final check failed: PickerX target=" + targetPickerX.ToString("F3") + " - Failed");
                    return -1;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) +
                    " move complete. die=" + BuildSelectedDieText(entry) +
                    ", pickerX=" + targetPickerX.ToString("F3") +
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

        private async Task<int> MoveOppositePickerToAvoidForManualPickerMoveAsync(Form1 host, PickerSequenceSide movingSide)
        {
            try
            {
                PickerSequenceSide oppositeSide = movingSide == PickerSequenceSide.Front
                    ? PickerSequenceSide.Rear
                    : PickerSequenceSide.Front;

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    ResolvePickerSideName(movingSide) + " picker manual move prepare. " +
                    ResolvePickerSideName(oppositeSide) + " picker moves to Avoid first. - Start");

                int result = await MoveTargetPickerToAvoidAsync(host, oppositeSide).ConfigureAwait(true);
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

        private async Task<int> MovePickersToAvoidForVisionMoveAsync(Form1 host)
        {
            try
            {
                int frontResult = await MoveTargetPickerToAvoidAsync(host, PickerSequenceSide.Front).ConfigureAwait(true);
                if (frontResult != 0)
                    return frontResult;

                int rearResult = await MoveTargetPickerToAvoidAsync(host, PickerSequenceSide.Rear).ConfigureAwait(true);
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

        private async Task<int> MoveInputVisionToAvoidForPickerMoveAsync(InputStageUnit stage)
        {
            try
            {
                if (stage == null)
                    return -1;

                if (stage.IsVisionXInAvoidPosition())
                    return 0;

                stage.Recipe.EnsurePositionObjects();
                double avoidTarget = stage.Recipe.VisionX.AvoidPosition;
                int result = await stage.MoveInputStageAxis(WaferStageAxis.VisionX, avoidTarget, true).ConfigureAwait(true);
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

        private async Task<int> MoveTargetPickerToAvoidAsync(Form1 host, PickerSequenceSide side)
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

                    int result = await front.MoveToFrontPickerAvoidPosition(true).ConfigureAwait(true);
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

                int rearResult = await rear.MoveToRearPickerAvoidPosition(true).ConfigureAwait(true);
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
                : "[" + entry.DieMapX + "," + entry.DieMapY + "]";
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
                btnPickStatusSave.Enabled = enabled;
                btnReloadActiveMap.Enabled = enabled;
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
                lblDieNum.Text = string.Format("[{0},{1}] / {2}", entry.DieMapX, entry.DieMapY,
                    mapView.Map != null ? mapView.Map.TotalCells : 0);
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

        private void RefreshDieGrid()
        {
            try
            {
                DieMap map = mapView != null ? mapView.Map : null;
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
                            entry.DieMapX,
                            entry.DieMapY,
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
                    if (entry.IsTarget)
                        target++;
                    if (entry.Result == DieResult.Good)
                        good++;
                    else if (entry.Result == DieResult.NG)
                        ng++;
                }

                lblBinValue.Text = target.ToString();
                lblDieNum.Text = "TARGET " + target + " / TOTAL " + map.Entries.Count + " / G " + good + " / NG " + ng;
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
                    .Where(entry => entry != null && entry.IsTarget)
                    .OrderBy(entry => entry.SequenceNo <= 0 ? int.MaxValue : entry.SequenceNo)
                    .ThenBy(entry => entry.DieMapY)
                    .ThenBy(entry => entry.DieMapX)
                    .ToList();
            }
            catch
            {
                return map != null && map.Entries != null
                    ? map.Entries.Where(entry => entry != null && entry.IsTarget).ToList()
                    : new System.Collections.Generic.List<DieMapEntry>();
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
                        rowEntry.DieMapX == entry.DieMapX &&
                        rowEntry.DieMapY == entry.DieMapY &&
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

