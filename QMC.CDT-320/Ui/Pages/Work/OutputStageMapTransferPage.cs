using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Motion;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class OutputStageMapTransferPage : PageBase
    {
        private Timer _refresh;
        private string _i18nTitle;
        private DieMapEntry _selectedEntry;
        private BinSide _selectedSide = BinSide.Good;
        private string _lastMapSignature;
        private ContextMenuStrip _gridMenu;
        private ToolStripMenuItem _gridMoveMenuItem;
        private ToolStripMenuItem[] _gridMoveFrontPickerMenuItems;
        private ToolStripMenuItem[] _gridMoveRearPickerMenuItems;
        private bool _manualMoveBusy;

        private sealed class OutputPlaceManualTargets
        {
            public double OutputStageY { get; set; }
            public double PickerX { get; set; }
            public double PickerY { get; set; }
            public double PickerT { get; set; }
        }

        public OutputStageMapTransferPage() : this("work.page.outputMap")
        {
        }

        public OutputStageMapTransferPage(string titleI18n)
        {
            _i18nTitle = titleI18n;
            InitializeComponent();
            ConfigureOutputDesignerText();
            ApplyTitle();
            WireEvents();

            if (!IsDesignerMode())
            {
                ReloadOutputMap();
                _refresh = new Timer { Interval = 1500 };
                _refresh.Tick += (s, e) =>
                {
                    try
                    {
                        ReloadOutputMap();
                    }
                    catch
                    {
                    }
                };
                _refresh.Start();
            }
        }

        private void ConfigureOutputDesignerText()
        {
            lblHeader.Tag = "i18n:" + _i18nTitle;
            lblHeader.Text = Lang.T(_i18nTitle);
            lblMapTitle.Text = "OUTPUT STAGE DIE MAP";
            mapView.Caption = "OUTPUT STAGE DIE MAP";

            rbStandard.Text = "GOOD STAGE";
            rbStartIndex.Text = "NG STAGE";
            rbSelectPickStatus.Text = "SOURCE ORDER";
            rbDragPickStatus.Text = "RECEIVED STATUS";
            rbStandard.Checked = true;
            rbSelectPickStatus.Enabled = false;
            rbDragPickStatus.Enabled = false;

            grpMapInfo.Text = "BIN / DIE INFO";
            grpMode.Text = "OUTPUT STAGE";
            lblBarcodeCaption.Text = "Source Wafer :";
            lblBinCaption.Text = "Side :";
            lblChipWCaption.Text = "Grid X";
            lblChipHCaption.Text = "Grid Y";
            lblWaferDiaCaption.Text = "Progress";

            btnReloadActiveMap.Text = "RELOAD OUTPUT DIE MAP";
            btnPickStatusSave.Text = "MOVE SELECTED SLOT";
            btnManualAlignComplete.Text = "GOOD PLAN INIT";
            btnNeedleBlockDown.Text = "NG PLAN INIT";
            btnThetaMatchMove.Text = "SAVE MATERIAL STATE";
            btnXyMatchMove.Text = "REFRESH DISPLAY";

            if (gridDieList != null)
                gridDieList.ColumnHeadersHeight = Math.Max(gridDieList.ColumnHeadersHeight, 32);
            if (colIndex != null)
                colIndex.HeaderText = "Index";
            if (colTarget != null)
                colTarget.HeaderText = "State";
        }

        private void ApplyTitle()
        {
            lblHeader.Tag = "i18n:" + _i18nTitle;
            lblHeader.Text = Lang.T(_i18nTitle);
            lblProjectValue.Text = GetCurrentProjectName();
        }

        private void WireEvents()
        {
            mapView.CellClicked += entry =>
            {
                if (entry == null)
                    return;
                SelectEntry(entry);
                SelectGridRow(entry);
            };

            gridDieList.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0)
                    return;
                SelectEntryByGridRow(e.RowIndex);
            };

            gridDieList.CellMouseDown += OnGridDieListCellMouseDown;
            BuildGridContextMenu();

            rbStandard.CheckedChanged += (s, e) =>
            {
                if (!rbStandard.Checked)
                    return;
                _selectedSide = BinSide.Good;
                ReloadOutputMap();
            };

            rbStartIndex.CheckedChanged += (s, e) =>
            {
                if (!rbStartIndex.Checked)
                    return;
                _selectedSide = BinSide.Ng;
                ReloadOutputMap();
            };

            btnReloadActiveMap.Click += (s, e) => ReloadOutputMap();
            btnPickStatusSave.Click += async (s, e) => await MoveSelectedBinSlotAsync().ConfigureAwait(true);
            btnManualAlignComplete.Click += (s, e) => InitializeReceivePlan(BinSide.Good);
            btnNeedleBlockDown.Click += (s, e) => InitializeReceivePlan(BinSide.Ng);
            btnThetaMatchMove.Click += (s, e) => SaveMaterialState();
            btnXyMatchMove.Click += (s, e) => ReloadOutputMap();
        }

        private string GetCurrentProjectName()
        {
            try
            {
                var lot = LotStorage.ActiveLot;
                if (lot != null && !string.IsNullOrEmpty(lot.RecipeName))
                    return lot.RecipeName;

                var list = RecipeStore.List();
                if (list != null && list.Count > 0)
                    return System.IO.Path.GetFileNameWithoutExtension(list[0]);
            }
            catch
            {
            }

            return "--";
        }

        private void ReloadOutputMap()
        {
            try
            {
                WaferMaterial outputWafer = GetSelectedOutputWafer();
                WaferMaterial sourceWafer = ResolveSourceWafer(outputWafer);

                DieMap materialMap = MaterialStateService.BuildOutputReceiveDieMapFromWafer(outputWafer);
                // Material에 저장된 Output receive map을 우선 사용하고, 없으면 레시피 원형 빈맵으로 표시.
                DieMap baseMap = materialMap ?? LoadRecipeBinMap(_selectedSide);
                if (baseMap == null)
                {
                    ApplyEmptyOutputMap(outputWafer, sourceWafer);
                    return;
                }

                DieMap displayMap = BuildDisplayMap(baseMap, outputWafer);

                // 변경 없으면 재적용 생략(타이머가 선택/스크롤을 매번 리셋하지 않도록).
                string signature = BuildMapSignature(displayMap, outputWafer);
                if (string.Equals(signature, _lastMapSignature, StringComparison.Ordinal))
                    return;
                _lastMapSignature = signature;

                ApplyMap(displayMap, outputWafer, sourceWafer);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output stage map reload failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private string BuildMapSignature(DieMap map, WaferMaterial outputWafer)
        {
            if (map == null)
                return _selectedSide + "|null";

            int next = outputWafer != null ? outputWafer.OutputReceiveNextIndex : 0;
            return string.Join("|",
                _selectedSide.ToString(),
                map.DieMapX.ToString(),
                map.DieMapY.ToString(),
                map.PitchX.ToString("F4"),
                map.PitchY.ToString("F4"),
                map.OriginX.ToString("F4"),
                map.OriginY.ToString("F4"),
                (map.Entries != null ? map.Entries.Count : 0).ToString(),
                next.ToString());
        }

        private OutputStageUnit GetOutputStageUnit()
        {
            try
            {
                Form1 host = FindForm() as Form1;
                return host != null && host.Machine != null ? host.Machine.OutputStageUnit : null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 레시피에 저장된 원형 빈맵(GOOD/NG)을 로드합니다. 슬롯 PosX/PosY는 빈 ProcessPosition
        /// 센터 기준 상대 좌표이며, 수동 이동/배치 타겟으로 사용됩니다.
        /// </summary>
        private DieMap LoadRecipeBinMap(BinSide side)
        {
            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project == null)
                    return null;

                RecipeMapKind kind = side == BinSide.Ng ? RecipeMapKind.NgBin : RecipeMapKind.GoodBin;
                string path = RecipeMapPaths.ResolveConfigured(project, kind);
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return CreateOutputCircleMapFromRecipe(project, side);

                return DieMapGenerator.Load(path);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Recipe bin map load failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private WaferMaterial GetSelectedOutputWafer()
        {
            return MaterialStateService.GetWaferAtLocation(
                _selectedSide == BinSide.Ng ? MaterialLocationKind.OutputStageNg : MaterialLocationKind.OutputStageGood);
        }

        private WaferMaterial ResolveSourceWafer(WaferMaterial outputWafer)
        {
            try
            {
                WaferMaterial inputStageWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (inputStageWafer != null)
                    return inputStageWafer;

                if (outputWafer == null || string.IsNullOrWhiteSpace(outputWafer.OutputReceiveSourceWaferId))
                    return null;

                return MaterialStateService.State.Wafers.FirstOrDefault(w =>
                    w != null &&
                    string.Equals(w.WaferId, outputWafer.OutputReceiveSourceWaferId, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private DieMap BuildDisplayMap(DieMap sourceMap, WaferMaterial outputWafer)
        {
            DieMap display = CloneMap(sourceMap);
            double processX = ResolveOutputVisionProcessX();
            double processY = ResolveOutputStageProcessY(_selectedSide);
            List<DieMapEntry> ordered = BuildReceiveOrder(display);
            int nextIndex = outputWafer != null ? outputWafer.OutputReceiveNextIndex : 0;
            int total = ordered.Count;
            if (nextIndex < 0)
                nextIndex = 0;
            if (nextIndex > total)
                nextIndex = total;

            for (int i = 0; i < ordered.Count; i++)
            {
                DieMapEntry entry = ordered[i];
                if (i < nextIndex)
                {
                    entry.Result = _selectedSide == BinSide.Ng ? DieResult.NG : DieResult.Good;
                    entry.BinCode = _selectedSide == BinSide.Ng ? 255 : 1;
                    entry.IsTarget = true;
                }
                else if (i == nextIndex)
                {
                    entry.Result = DieResult.Unknown;
                    entry.BinCode = _selectedSide == BinSide.Ng ? 255 : 1;
                    entry.IsTarget = true;
                }
                else
                {
                    entry.Result = DieResult.Unknown;
                    entry.BinCode = 0;
                    entry.IsTarget = true;
                }
            }

            foreach (DieMapEntry entry in display.Entries)
            {
                if (entry == null)
                    continue;

                entry.PosX = processX + entry.PosX;
                entry.PosY = processY + entry.PosY;
            }

            return display;
        }

        private double ResolveOutputVisionProcessX()
        {
            OutputStageUnit unit = GetOutputStageUnit();
            return unit != null && unit.Recipe != null && unit.Recipe.VisionX != null
                ? unit.Recipe.VisionX.ProcessPosition
                : 0.0;
        }

        private double ResolveOutputStageProcessY(BinSide side)
        {
            OutputStageUnit unit = GetOutputStageUnit();
            if (unit == null || unit.Recipe == null)
                return 0.0;

            return side == BinSide.Ng
                ? unit.Recipe.NGStageY.ProcessPosition
                : unit.Recipe.GoodStageY.ProcessPosition;
        }

        private double ToRelativeOutputX(double displayX)
        {
            return displayX - ResolveOutputVisionProcessX();
        }

        private double ToRelativeOutputY(double displayY)
        {
            return displayY - ResolveOutputStageProcessY(_selectedSide);
        }

        private static DieMap CloneMap(DieMap source)
        {
            DieMapGenerator.Normalize(source);
            var clone = new DieMap
            {
                FrameObjId = source.FrameObjId,
                DieMapX = source.DieMapX,
                DieMapY = source.DieMapY,
                PitchX = source.PitchX,
                PitchY = source.PitchY,
                OriginX = source.OriginX,
                OriginY = source.OriginY,
                CreatedAt = source.CreatedAt
            };

            foreach (DieMapEntry entry in source.Entries)
            {
                if (entry == null)
                    continue;
                clone.Entries.Add(new DieMapEntry
                {
                    Index = entry.Index,
                    SequenceNo = entry.SequenceNo,
                    DieMapX = entry.DieMapX,
                    DieMapY = entry.DieMapY,
                    IsTarget = entry.IsTarget,
                    Result = entry.Result,
                    BinCode = entry.BinCode,
                    PosX = entry.PosX,
                    PosY = entry.PosY,
                    DieUid = entry.DieUid
                });
            }

            return DieMapGenerator.Normalize(clone);
        }

        private static List<DieMapEntry> BuildReceiveOrder(DieMap map)
        {
            try
            {
                var project = RecipeStore.LoadLastOrDefault();
                PickupSubset pickup = project != null && project.OutputPickup != null
                    ? project.OutputPickup
                    : (project != null && project.Pickup != null ? project.Pickup : new PickupSubset());
                List<DieMapEntry> ordered = PickupSequenceGenerator.Build(map, pickup);
                if (ordered != null && ordered.Count > 0)
                    return ordered;
            }
            catch
            {
            }

            return map != null && map.Entries != null
                ? map.Entries.Where(e => e != null && e.IsTarget).OrderBy(e => e.DieMapY).ThenBy(e => e.DieMapX).ToList()
                : new List<DieMapEntry>();
        }

        private void ApplyMap(DieMap map, WaferMaterial outputWafer, WaferMaterial sourceWafer)
        {
            try
            {
                string sideText = _selectedSide == BinSide.Ng ? "NG" : "GOOD";
                DieMapGenerator.Normalize(map);
                mapView.Caption = "OUTPUT " + sideText + " RECEIVE MAP";
                mapView.Map = map;
                _selectedEntry = null;

                lblMapTitle.Text = "OUTPUT " + sideText + " RECEIVE MAP";
                lblProjectValue.Text = GetCurrentProjectName();
                lblBarcodeValue.Text = sourceWafer != null ? sourceWafer.WaferId : "-";
                lblBinValue.Text = sideText;
                lblChipW.Text = map != null ? map.DieMapX.ToString() : "0";
                lblChipH.Text = map != null ? map.DieMapY.ToString() : "0";
                lblPitchX.Text = outputWafer != null ? outputWafer.OutputReceivePitchX.ToString("F3") : "0";
                lblPitchY.Text = outputWafer != null ? outputWafer.OutputReceivePitchY.ToString("F3") : "0";
                lblWaferDia.Text = BuildProgressText(outputWafer, map);
                lblAxisX.Text = "0";
                lblAxisY.Text = "0";
                lblBinRank.Text = outputWafer != null ? WaferMaterialStateText.ToDisplayName(outputWafer.State) : "EMPTY";
                lblDieNum.Text = BuildNextTargetText(outputWafer, map);

                RefreshDieGrid();
                SelectNextReceiveRow(outputWafer);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ApplyEmptyOutputMap(WaferMaterial outputWafer, WaferMaterial sourceWafer)
        {
            mapView.Map = null;
            mapView.Caption = "OUTPUT STAGE DIE MAP";
            lblMapTitle.Text = "OUTPUT STAGE DIE MAP";
            lblProjectValue.Text = GetCurrentProjectName();
            lblBarcodeValue.Text = sourceWafer != null ? sourceWafer.WaferId : "-";
            lblBinValue.Text = _selectedSide == BinSide.Ng ? "NG" : "GOOD";
            lblChipW.Text = "0";
            lblChipH.Text = "0";
            lblPitchX.Text = outputWafer != null ? outputWafer.OutputReceivePitchX.ToString("F3") : "0";
            lblPitchY.Text = outputWafer != null ? outputWafer.OutputReceivePitchY.ToString("F3") : "0";
            lblWaferDia.Text = BuildProgressText(outputWafer, null);
            lblAxisX.Text = "0";
            lblAxisY.Text = "0";
            lblBinRank.Text = outputWafer != null ? WaferMaterialStateText.ToDisplayName(outputWafer.State) : "EMPTY";
            lblDieNum.Text = "NO SOURCE MAP";
            RefreshDieGrid();
        }

        private static string BuildProgressText(WaferMaterial outputWafer, DieMap map)
        {
            if (outputWafer == null)
                return "0 / 0";

            int total = outputWafer.OutputReceiveTotalCount > 0
                ? outputWafer.OutputReceiveTotalCount
                : (map != null ? map.Entries.Count : 0);
            int next = outputWafer.OutputReceiveNextIndex;
            if (next < 0)
                next = 0;
            if (next > total)
                next = total;

            return next + " / " + total;
        }

        private static string BuildNextTargetText(WaferMaterial outputWafer, DieMap map)
        {
            if (outputWafer == null)
                return "NO BIN";
            if (map == null)
                return "NO MAP";

            List<DieMapEntry> ordered = BuildReceiveOrder(map);
            int index = outputWafer.OutputReceiveNextIndex;
            if (index < 0 || index >= ordered.Count)
                return "COMPLETE";

            DieMapEntry next = ordered[index];
            return string.Format("[{0},{1}] / {2}", next.DieMapX, next.DieMapY, ordered.Count);
        }

        private void RefreshDieGrid()
        {
            try
            {
                gridDieList.Rows.Clear();
                DieMap map = mapView != null ? mapView.Map : null;
                if (map == null || map.Entries == null)
                    return;

                List<DieMapEntry> ordered = BuildReceiveOrder(map);
                for (int i = 0; i < ordered.Count; i++)
                {
                    DieMapEntry entry = ordered[i];
                    if (entry == null)
                        continue;

                    string status = entry.Result == DieResult.Good || entry.Result == DieResult.NG
                        ? "RECEIVED"
                        : (entry.BinCode != 0 && entry.IsTarget ? "NEXT" : "WAIT");
                    int rowIndex = gridDieList.Rows.Add(
                        i,
                        entry.DieMapX,
                        entry.DieMapY,
                        status,
                        entry.Result,
                        entry.BinCode,
                        entry.PosX.ToString("F4"),
                        entry.PosY.ToString("F4"),
                        entry.DieUid ?? "");
                    gridDieList.Rows[rowIndex].Tag = entry;
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void SelectNextReceiveRow(WaferMaterial outputWafer)
        {
            try
            {
                if (outputWafer == null || gridDieList.Rows.Count == 0)
                    return;

                int rowIndex = outputWafer.OutputReceiveNextIndex;
                if (rowIndex < 0)
                    rowIndex = 0;
                if (rowIndex >= gridDieList.Rows.Count)
                    rowIndex = gridDieList.Rows.Count - 1;

                gridDieList.ClearSelection();
                gridDieList.Rows[rowIndex].Selected = true;
                gridDieList.FirstDisplayedScrollingRowIndex = rowIndex;
            }
            catch
            {
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
                DieMap map = mapView != null ? mapView.Map : null;
                if (map == null || rowIndex < 0)
                    return;

                if (rowIndex >= 0 && rowIndex < gridDieList.Rows.Count)
                {
                    DieMapEntry rowEntry = gridDieList.Rows[rowIndex].Tag as DieMapEntry;
                    if (rowEntry != null)
                    {
                        SelectEntry(rowEntry);
                        return;
                    }
                }

                List<DieMapEntry> ordered = BuildReceiveOrder(map);
                if (rowIndex < ordered.Count)
                    SelectEntry(ordered[rowIndex]);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void SelectGridRow(DieMapEntry entry)
        {
            try
            {
                if (entry == null || gridDieList == null)
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

        private DieMap CreateOutputCircleMapFromRecipe(RecipeProject recipe, BinSide side)
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
                FrameObjId = (side == BinSide.Ng ? "NG" : "GOOD") + "_OUTPUT_CIRCLE",
                DieMapX = gridX,
                DieMapY = gridY,
                PitchX = pitchX,
                PitchY = pitchY,
                OriginX = originX,
                OriginY = originY,
                CreatedAt = DateTime.Now
            };

            int index = 0;
            int binCode = side == BinSide.Ng ? 255 : 1;
            for (int row = 0; row < gridY; row++)
            {
                for (int col = 0; col < gridX; col++)
                {
                    double x = originX + col * pitchX;
                    double y = originY + row * pitchY;
                    bool target = IsInsideOutputCircle(col, row, gridX, gridY, sideEdgeSkip, topBottomEdgeSkip, x, y, pitchX, pitchY, diameterMm);
                    map.Entries.Add(new DieMapEntry
                    {
                        Index = index++,
                        DieMapX = col,
                        DieMapY = row,
                        IsTarget = target,
                        Result = target ? DieResult.Unknown : DieResult.NG,
                        BinCode = target ? binCode : 255,
                        PosX = x,
                        PosY = y,
                        DieUid = (side == BinSide.Ng ? "NG" : "GOOD") + "-D" + row.ToString("000") + "-" + col.ToString("000")
                    });
                }
            }

            PickupSubset pickup = recipe.OutputPickup ?? recipe.Pickup ?? new PickupSubset();
            return PickupSequenceGenerator.ApplySequenceNumbers(map, pickup);
        }

        private static bool IsInsideOutputCircle(
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

            double radiusMm = diameterMm / 2.0;
            return (x * x) + (y * y) <= radiusMm * radiusMm;
        }

        private void BuildGridContextMenu()
        {
            try
            {
                _gridMoveMenuItem = new ToolStripMenuItem("MOVE VISION/STAGE");
                _gridMoveMenuItem.Click += async (s, e) => await MoveSelectedBinSlotAsync().ConfigureAwait(true);

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
                item.Click += async (s, e) => await MoveSelectedSlotByPickerAsync(side, pickerNo).ConfigureAwait(true);
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

        /// <summary>
        /// 선택 빈 슬롯 좌표로 출력 스테이지를 이동합니다.<br/>
        /// 인터락: VisionX(공유레일) 이동 전 Front/Rear PickerX를 Avoid로 선행 이동.<br/>
        /// 축 구조 D3: 행(Y)=스테이지 {side}BinY, 열(X)=VisionX(카메라). 각 단계 타임아웃 가드 + 정지.
        /// </summary>
        private async Task MoveSelectedBinSlotAsync()
        {
            Form1 host = FindForm() as Form1;
            try
            {
                DieMapEntry entry = _selectedEntry;
                if (entry == null)
                {
                    QMC.Common.MessageDialog.Show(this, "이동할 빈 슬롯이 선택되지 않았습니다.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (host == null || host.Machine == null || host.Machine.OutputStageUnit == null)
                {
                    QMC.Common.MessageDialog.Show(this, "OutputStage 장비 정보를 찾을 수 없습니다.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                OutputStageUnit unit = host.Machine.OutputStageUnit;
                if (unit.Recipe == null)
                {
                    QMC.Common.MessageDialog.Show(this, "OutputStage 레시피 정보를 찾을 수 없습니다.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                unit.Recipe.EnsurePositionObjects();

                double absX = entry.PosX;
                double absY = entry.PosY;

                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    "빈 슬롯 [" + entry.DieMapX + "," + entry.DieMapY + "]의 좌표로 이동하시겠습니까?\r\n" +
                    "X(VisionX)=" + absX.ToString("F3") + " mm, Y(StageY)=" + absY.ToString("F3") + " mm",
                    "Output Stage Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                int timeoutMs = ResolveManualMoveTimeoutMs(host);
                SetActionButtonsEnabled(false);
                _manualMoveBusy = true;

                // 1) VisionX(공유레일) 이동 전 Front/Rear PickerX를 Avoid로 선행 이동(간섭 차단).
                int prepareResult = await AwaitManualMoveStepAsync(
                    MovePickersToAvoidForOutputMoveAsync(host),
                    timeoutMs,
                    "출력 이동 전 Picker Avoid 준비",
                    () => StopManualMapMove(host, "Output move prepare timeout")).ConfigureAwait(true);
                if (prepareResult != 0)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "출력 이동 전 Picker Avoid 준비 실패\r\nresult=" + prepareResult +
                        "\r\nAlarm/Event Log를 확인하세요.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int stagePrepareResult = await AwaitManualMoveStepAsync(
                    PrepareOutputStageYMoveAsync(unit, _selectedSide, timeoutMs),
                    timeoutMs,
                    "OutputStage Y 이동 준비",
                    () => StopManualMapMove(host, "OutputStage Y 이동 준비 타임아웃")).ConfigureAwait(true);
                if (stagePrepareResult != 0)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "OutputStage Y 이동 준비 실패\r\nresult=" + stagePrepareResult +
                        "\r\nAlarm/Event Log를 확인하세요.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 2) 행(Y): 스테이지 Y축
                BinStageAxis yAxis = _selectedSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
                int rowResult = await AwaitManualMoveStepAsync(
                    unit.MoveStageAxis(yAxis, absY, true),
                    timeoutMs,
                    "빈 슬롯 행(Y) 이동",
                    () => StopManualMapMove(host, "Output Y move timeout")).ConfigureAwait(true);
                if (rowResult != 0)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "빈 슬롯 행(Y) 이동 실패\r\nresult=" + rowResult + "\r\nAlarm/Event Log를 확인하세요.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 3) 열(X): VisionX(카메라)
                int colResult = await AwaitManualMoveStepAsync(
                    unit.MoveStageAxis(BinStageAxis.VisionX, absX, true),
                    timeoutMs,
                    "빈 슬롯 열(VisionX) 이동",
                    () => StopManualMapMove(host, "Output VisionX move timeout")).ConfigureAwait(true);
                if (colResult != 0)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "빈 슬롯 열(VisionX) 이동 실패\r\nresult=" + colResult + "\r\nAlarm/Event Log를 확인하세요.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                lblAxisX.Text = absX.ToString("F3");
                lblAxisY.Text = absY.ToString("F3");
                QMC.Common.MessageDialog.Show(this, "선택 빈 슬롯 좌표 이동 완료.",
                    "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output bin slot move failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "선택 빈 슬롯 좌표 이동 실패:\r\n" + ex.Message,
                    "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _manualMoveBusy = false;
                SetActionButtonsEnabled(true);
            }
        }

        private async Task MoveSelectedSlotByPickerAsync(PickerSequenceSide side, int pickerNo)
        {
            Form1 host = FindForm() as Form1;
            try
            {
                DieMapEntry entry = _selectedEntry;
                if (entry == null)
                {
                    QMC.Common.MessageDialog.Show(this, "이동할 빈 슬롯이 선택되지 않았습니다.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (host == null || host.Machine == null || host.Machine.OutputStageUnit == null)
                {
                    QMC.Common.MessageDialog.Show(this, "OutputStage 장비 정보를 찾을 수 없습니다.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                OutputPlaceManualTargets targets;
                string reason;
                if (!TryResolveOutputPlaceManualTargets(host, _selectedSide, side, pickerNo, entry, out targets, out reason))
                {
                    QMC.Common.MessageDialog.Show(this, "Picker Place 좌표를 계산할 수 없습니다.\r\n" + reason,
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    ResolvePickerMoveTitle(side, pickerNo) + "를 선택 빈 슬롯 Place 위치로 이동하시겠습니까?\r\n" +
                    "Slot=[" + entry.DieMapX + "," + entry.DieMapY + "]\r\n" +
                    "StageY=" + targets.OutputStageY.ToString("F3") + " mm\r\n" +
                    "PickerX=" + targets.PickerX.ToString("F3") + " mm\r\n" +
                    "PickerY=" + targets.PickerY.ToString("F3") + " mm\r\n" +
                    "PickerT=" + targets.PickerT.ToString("F3") + " deg\r\n" +
                    "PickerZ는 이동하지 않습니다.",
                    "Output Stage Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                int timeoutMs = ResolveManualMoveTimeoutMs(host);
                SetActionButtonsEnabled(false);
                _manualMoveBusy = true;

                int result = await AwaitManualMoveStepAsync(
                    MoveSelectedSlotByPickerCoreAsync(host, _selectedSide, side, pickerNo, entry, targets, timeoutMs),
                    timeoutMs,
                    ResolvePickerMoveTitle(side, pickerNo) + " Place 보기 위치 이동",
                    () => StopManualMapMove(host, ResolvePickerMoveTitle(side, pickerNo) + " output place view timeout")).ConfigureAwait(true);
                if (result != 0)
                {
                    QMC.Common.MessageDialog.Show(this,
                        ResolvePickerMoveTitle(side, pickerNo) + " Place 보기 위치 이동 실패\r\nresult=" + result +
                        "\r\nAlarm/Event Log를 확인하세요.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                lblAxisX.Text = targets.PickerX.ToString("F3");
                lblAxisY.Text = targets.OutputStageY.ToString("F3");
                QMC.Common.MessageDialog.Show(this,
                    ResolvePickerMoveTitle(side, pickerNo) + " Place 보기 위치 이동 완료.",
                    "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output picker place view move failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "Picker Place 보기 위치 이동 실패:\r\n" + ex.Message,
                    "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _manualMoveBusy = false;
                SetActionButtonsEnabled(true);
            }
        }

        private async Task<int> MoveSelectedSlotByPickerCoreAsync(
            Form1 host,
            BinSide outputSide,
            PickerSequenceSide side,
            int pickerNo,
            DieMapEntry entry,
            OutputPlaceManualTargets targets,
            int timeoutMs)
        {
            try
            {
                OutputStageUnit unit = host.Machine.OutputStageUnit;

                int prepareResult = await MovePickersToAvoidForOutputMoveAsync(host).ConfigureAwait(true);
                if (prepareResult != 0)
                    return prepareResult;

                prepareResult = await PrepareOutputStageYMoveAsync(unit, _selectedSide, timeoutMs).ConfigureAwait(true);
                if (prepareResult != 0)
                    return prepareResult;

                int loadResult = await unit.MoveToStageLoadPositionAndVerifyAsync(_selectedSide, timeoutMs, true).ConfigureAwait(true);
                if (loadResult != 0)
                    return loadResult;

                BinStageAxis yAxis = _selectedSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
                int stageResult = await unit.MoveStageAxis(yAxis, targets.OutputStageY, true).ConfigureAwait(true);
                if (stageResult != 0)
                    return stageResult;

                AxisMoveWaitResult stageWait = await unit.WaitStageAxisMoveDoneInPosition(yAxis, targets.OutputStageY, timeoutMs).ConfigureAwait(true);
                if (stageWait == null || !stageWait.Success)
                    return -1;

                int pickerIndex = pickerNo - 1;
                PickerAxis tAxis = GetPickerTAxis(pickerIndex);
                string targetName = "DiePlacePosition[" + pickerIndex + "];ManualOutputDieMapMove";

                Task<int> movePickerX = MovePickerAxisAsync(host, side, PickerAxis.PickerX, targets.PickerX, targetName);
                Task<int> movePickerY = MovePickerAxisAsync(host, side, PickerAxis.PickerY, targets.PickerY, targetName);
                Task<int> movePickerT = MovePickerAxisAsync(host, side, tAxis, targets.PickerT, targetName);
                int[] moveResults = await Task.WhenAll(movePickerX, movePickerY, movePickerT).ConfigureAwait(true);
                for (int i = 0; i < moveResults.Length; i++)
                {
                    if (moveResults[i] != 0)
                        return moveResults[i];
                }

                Task<int> waitPickerX = WaitPickerAxisInPositionAsync(host, side, PickerAxis.PickerX, targets.PickerX, timeoutMs);
                Task<int> waitPickerY = WaitPickerAxisInPositionAsync(host, side, PickerAxis.PickerY, targets.PickerY, timeoutMs);
                Task<int> waitPickerT = WaitPickerAxisInPositionAsync(host, side, tAxis, targets.PickerT, timeoutMs);
                int[] waitResults = await Task.WhenAll(waitPickerX, waitPickerY, waitPickerT).ConfigureAwait(true);
                for (int i = 0; i < waitResults.Length; i++)
                {
                    if (waitResults[i] != 0)
                        return waitResults[i];
                }

                if (!IsPickerAxisInPosition(host, side, PickerAxis.PickerX, targets.PickerX) ||
                    !IsPickerAxisInPosition(host, side, PickerAxis.PickerY, targets.PickerY) ||
                    !IsPickerAxisInPosition(host, side, tAxis, targets.PickerT))
                    return -1;

                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) +
                    " output place view move complete. slot=[" + entry.DieMapX + "," + entry.DieMapY + "]" +
                    ", stageY=" + targets.OutputStageY.ToString("F3") +
                    ", pickerX=" + targets.PickerX.ToString("F3") +
                    ", pickerY=" + targets.PickerY.ToString("F3") +
                    ", pickerT=" + targets.PickerT.ToString("F3") + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) + " output place view move exception: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>VisionX(공유레일) 이동 전 Front/Rear PickerX를 Avoid 위치로 선행 이동합니다.</summary>
        private async Task<int> MovePickersToAvoidForOutputMoveAsync(Form1 host)
        {
            try
            {
                if (host == null || host.Machine == null)
                    return -1;

                PickerFrontUnit front = host.Machine.PickerFrontUnit;
                if (front != null && !front.IsFrontPickerInAvoidPosition())
                {
                    int frontResult = await front.MoveToFrontPickerAvoidPosition(true).ConfigureAwait(true);
                    if (frontResult != 0)
                        return frontResult;
                    if (!front.IsFrontPickerInAvoidPosition())
                        return -1;
                }

                PickerRearUnit rear = host.Machine.PickerRearUnit;
                if (rear != null && !rear.IsRearPickerInAvoidPosition())
                {
                    int rearResult = await rear.MoveToRearPickerAvoidPosition(true).ConfigureAwait(true);
                    if (rearResult != 0)
                        return rearResult;
                    if (!rear.IsRearPickerInAvoidPosition())
                        return -1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Picker avoid prepare for output move failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> PrepareOutputStageYMoveAsync(OutputStageUnit unit, BinSide side, int timeoutMs)
        {
            try
            {
                if (unit == null || unit.Recipe == null)
                    return -1;

                unit.Recipe.EnsurePositionObjects();

                if (side == BinSide.Good)
                {
                    int ngClampLiftResult = await unit.EnsureBinGuideClampLiftUpAsync(BinSide.Ng, timeoutMs).ConfigureAwait(true);
                    if (ngClampLiftResult != 0)
                        return ngClampLiftResult;

                    if (!unit.IsBinGuideClampLiftUp(BinSide.Ng))
                        return -1;
                }

                if (unit.HasStageAxis(BinStageAxis.GoodBinZ))
                {
                    double targetZ = side == BinSide.Ng
                        ? unit.Recipe.GoodStageZ.AvoidPosition
                        : unit.Recipe.GoodStageZ.ProcessPosition;

                    bool alreadyReady = side == BinSide.Ng
                        ? unit.IsGoodStageZAtAvoid()
                        : unit.IsGoodStageZInAvoidOrProcessPosition();
                    if (side == BinSide.Good)
                        alreadyReady = unit.IsStageAxisInPosition(BinStageAxis.GoodBinZ, targetZ, ResolveOutputStageAxisTolerance(unit, BinStageAxis.GoodBinZ));

                    if (alreadyReady)
                        return 0;

                    int zResult = await unit.MoveStageAxis(BinStageAxis.GoodBinZ, targetZ, true).ConfigureAwait(true);
                    if (zResult != 0)
                        return zResult;

                    AxisMoveWaitResult zWait = await unit.WaitStageAxisMoveDoneInPosition(
                        BinStageAxis.GoodBinZ,
                        targetZ,
                        timeoutMs).ConfigureAwait(true);
                    if (zWait == null || !zWait.Success ||
                        !unit.IsStageAxisInPosition(BinStageAxis.GoodBinZ, targetZ, ResolveOutputStageAxisTolerance(unit, BinStageAxis.GoodBinZ)))
                        return -1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "OutputStage Y 이동 준비 실패: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private static double ResolveOutputStageAxisTolerance(OutputStageUnit unit, BinStageAxis axis)
        {
            return 0.05;
        }

        private static bool TryResolveOutputPlaceManualTargets(
            Form1 host,
            BinSide outputSide,
            PickerSequenceSide side,
            int pickerNo,
            DieMapEntry entry,
            out OutputPlaceManualTargets targets,
            out string reason)
        {
            targets = null;
            reason = string.Empty;

            try
            {
                if (host == null || host.Machine == null || host.Machine.OutputStageUnit == null)
                {
                    reason = "장비 정보를 찾을 수 없습니다.";
                    return false;
                }

                if (entry == null)
                {
                    reason = "선택된 빈 슬롯이 없습니다.";
                    return false;
                }

                int pickerIndex = pickerNo - 1;
                if (pickerIndex < 0 || pickerIndex >= 4)
                {
                    reason = "Picker 번호가 범위를 벗어났습니다. pickerNo=" + pickerNo;
                    return false;
                }

                double offsetX;
                double offsetY;
                string offsetReason;
                if (!PickerCoordinateTransformHelper.TryResolveOutputVisionToPickerOffsets(
                    host.Machine,
                    side,
                    pickerIndex,
                    out offsetX,
                    out offsetY,
                    out offsetReason))
                {
                    reason = offsetReason;
                    return false;
                }

                PickerAlignOffset alignOffset = ResolveRuntimePickerOffset(host, side, pickerIndex);
                double alignX = alignOffset != null ? alignOffset.AlignOffsetX : 0.0;
                double alignY = alignOffset != null ? alignOffset.AlignOffsetY : 0.0;
                double alignT = alignOffset != null ? alignOffset.AlignOffsetT : 0.0;

                double pickerY = GetPickerTeachingPosition(host, side, PickerAxis.PickerY, "PlacePosition") + alignY;
                PickerAxis tAxis = GetPickerTAxis(pickerIndex);
                double pickerT = GetPickerTeachingPosition(host, side, tAxis, "PlacePosition") + alignT;

                targets = new OutputPlaceManualTargets
                {
                    OutputStageY = entry.PosY + offsetY,
                    PickerX = entry.PosX + offsetX + alignX,
                    PickerY = pickerY,
                    PickerT = pickerT
                };
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private static PickerAlignOffset ResolveRuntimePickerOffset(Form1 host, PickerSequenceSide side, int pickerIndex)
        {
            if (host == null || host.Machine == null)
                return null;

            if (side == PickerSequenceSide.Front)
                return host.Machine.PickerFrontUnit != null ? host.Machine.PickerFrontUnit.GetRuntimePickerOffset(pickerIndex) : null;

            return host.Machine.PickerRearUnit != null ? host.Machine.PickerRearUnit.GetRuntimePickerOffset(pickerIndex) : null;
        }

        private static double GetPickerTeachingPosition(Form1 host, PickerSequenceSide side, PickerAxis axis, string positionName)
        {
            if (host == null || host.Machine == null)
                return 0.0;

            if (side == PickerSequenceSide.Front)
                return host.Machine.PickerFrontUnit != null ? host.Machine.PickerFrontUnit.GetPickerTeachingPosition(axis, positionName) : 0.0;

            return host.Machine.PickerRearUnit != null ? host.Machine.PickerRearUnit.GetPickerTeachingPosition(axis, positionName) : 0.0;
        }

        private static PickerAxis GetPickerTAxis(int index)
        {
            if (index <= 0) return PickerAxis.PickerT0;
            if (index == 1) return PickerAxis.PickerT1;
            if (index == 2) return PickerAxis.PickerT2;
            return PickerAxis.PickerT3;
        }

        private static Task<int> MovePickerAxisAsync(Form1 host, PickerSequenceSide side, PickerAxis axis, double target, string targetName)
        {
            if (host == null || host.Machine == null)
                return Task.FromResult(-1);

            if (side == PickerSequenceSide.Front)
            {
                PickerFrontUnit front = host.Machine.PickerFrontUnit;
                return front != null ? front.MoveFrontPickerAxis(axis, target, true, targetName) : Task.FromResult(-1);
            }

            PickerRearUnit rear = host.Machine.PickerRearUnit;
            return rear != null ? rear.MoveRearPickerAxis(axis, target, true, targetName) : Task.FromResult(-1);
        }

        private static async Task<int> WaitPickerAxisInPositionAsync(Form1 host, PickerSequenceSide side, PickerAxis axis, double target, int timeoutMs)
        {
            if (host == null || host.Machine == null)
                return -1;

            AxisMoveWaitResult waitResult;
            if (side == PickerSequenceSide.Front)
            {
                PickerFrontUnit front = host.Machine.PickerFrontUnit;
                if (front == null)
                    return -1;
                waitResult = await front.WaitPickerAxisMoveDoneInPosition(axis, target, timeoutMs).ConfigureAwait(true);
            }
            else
            {
                PickerRearUnit rear = host.Machine.PickerRearUnit;
                if (rear == null)
                    return -1;
                waitResult = await rear.WaitPickerAxisMoveDoneInPosition(axis, target, timeoutMs).ConfigureAwait(true);
            }

            return waitResult != null && waitResult.Success ? 0 : -1;
        }

        private static bool IsPickerAxisInPosition(Form1 host, PickerSequenceSide side, PickerAxis axis, double target)
        {
            if (host == null || host.Machine == null)
                return false;

            if (side == PickerSequenceSide.Front)
            {
                PickerFrontUnit front = host.Machine.PickerFrontUnit;
                return front != null && front.IsFrontPickerAxisInPosition(axis, target, ResolvePickerAxisTolerance(front, axis));
            }

            PickerRearUnit rear = host.Machine.PickerRearUnit;
            return rear != null && rear.IsRearPickerAxisInPosition(axis, target, ResolvePickerAxisTolerance(rear, axis));
        }

        private static double ResolvePickerAxisTolerance(PickerFrontUnit picker, PickerAxis axis)
        {
            BaseAxis item = picker != null && picker.Axes != null && picker.Axes.ContainsKey(axis) ? picker.Axes[axis] : null;
            return item != null && item.Config != null && item.Config.InPositionTolerance > 0.0 ? item.Config.InPositionTolerance : 0.05;
        }

        private static double ResolvePickerAxisTolerance(PickerRearUnit picker, PickerAxis axis)
        {
            BaseAxis item = picker != null && picker.Axes != null && picker.Axes.ContainsKey(axis) ? picker.Axes[axis] : null;
            return item != null && item.Config != null && item.Config.InPositionTolerance > 0.0 ? item.Config.InPositionTolerance : 0.05;
        }

        private static string ResolvePickerMoveTitle(PickerSequenceSide side, int pickerNo)
        {
            return (side == PickerSequenceSide.Front ? "FRONT" : "REAR") + " PICKER #" + pickerNo;
        }

        private async Task<int> AwaitManualMoveStepAsync(Task<int> operation, int timeoutMs, string description, Action onTimeoutStop)
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

                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    description + " timeout. timeoutMs=" + effectiveTimeoutMs + " - Failed");
                RaiseManualMoveAlarm("OUT-STAGE-MAP-MANUAL-TIMEOUT", description + " timeout. Manual move stopped.");

                try
                {
                    if (onTimeoutStop != null)
                        onTimeoutStop();
                }
                catch (Exception stopEx)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
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
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    description + " failed: " + ex.Message + " - Failed");
                RaiseManualMoveAlarm("OUT-STAGE-MAP-MANUAL-FAIL", description + " failed. " + ex.Message);
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
                            QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                                description + " background task faulted: " +
                                (t.Exception != null ? t.Exception.GetBaseException().Message : "unknown") + " - Failed");
                        else
                            QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                                description + " background task completed after timeout. result=" + t.Result + " - Check");
                    }
                    catch
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

        private static int ResolveManualMoveTimeoutMs(Form1 host)
        {
            int timeoutMs = 30000;
            try
            {
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
            }
            catch
            {
            }
            finally
            {
            }

            return timeoutMs;
        }

        private void StopManualMapMove(Form1 host, string reason)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Manual map move stop requested. reason=" + reason + " - Check");

                if (host != null && host.Controller != null)
                    host.Controller.CancelManualOperation();

                if (host == null || host.Machine == null)
                    return;

                OutputStageUnit unit = host.Machine.OutputStageUnit;
                if (unit != null)
                {
                    if (unit.GoodStage != null && unit.GoodStage.StageY != null)
                        _ = unit.StopJogAsync(unit.GoodStage.StageY);
                    if (unit.NgStage != null && unit.NgStage.StageY != null)
                        _ = unit.StopJogAsync(unit.NgStage.StageY);
                    if (unit.OutputCameraX != null)
                        _ = unit.StopJogAsync(unit.OutputCameraX);
                }

                if (host.Machine.PickerFrontUnit != null)
                    host.Machine.PickerFrontUnit.StopPickerMotionAndOutputs(reason);

                if (host.Machine.PickerRearUnit != null)
                    host.Machine.PickerRearUnit.StopPickerMotionAndOutputs(reason);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
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
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    code,
                    "OutputStageMapTransferPage",
                    message);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            try
            {
                btnReloadActiveMap.Enabled = enabled;
                btnPickStatusSave.Enabled = enabled;
                btnManualAlignComplete.Enabled = enabled;
                btnNeedleBlockDown.Enabled = enabled;
                btnThetaMatchMove.Enabled = enabled;
                btnXyMatchMove.Enabled = enabled;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void InitializeSelectedReceivePlan()
        {
            InitializeReceivePlan(_selectedSide);
        }

        private void InitializeReceivePlan(BinSide side)
        {
            try
            {
                string sideText = side == BinSide.Ng ? "NG" : "GOOD";
                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    "Output " + sideText + " Stage receive plan을 InputStage Die Map 기준으로 초기화하시겠습니까?",
                    "Output Stage Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                bool ok = MaterialStateService.InitializeOutputStageReceivePlan(side);
                ReloadOutputMap();
                if (!ok)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "Receive plan 초기화 실패.\r\nOutputStage Bin Data와 InputStage Die Map을 확인하세요.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                QMC.Common.MessageDialog.Show(this, "Receive plan 초기화 완료.",
                    "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output receive plan initialize failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "Receive plan 초기화 실패:\r\n" + ex.Message,
                    "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void SaveMaterialState()
        {
            try
            {
                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    "현재 Material 상태를 저장하시겠습니까?",
                    "Output Stage Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                PersistOutputMapToMaterialState(mapView != null ? mapView.Map : null);
                MaterialStateService.NotifyAndSave("OutputStageMapTransferSave");
                QMC.Common.MessageDialog.Show(this, "Material 상태 저장 완료.",
                    "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output material state save failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "Material 상태 저장 실패:\r\n" + ex.Message,
                    "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void PersistOutputMapToMaterialState(DieMap map)
        {
            try
            {
                if (map == null || map.Entries == null)
                    return;

                WaferMaterial wafer = GetSelectedOutputWafer();
                if (wafer == null)
                    return;

                DieMapGenerator.Normalize(map);
                wafer.DieMapFrameObjId = map.FrameObjId ?? "";
                wafer.OutputReceiveDieMapX = map.DieMapX;
                wafer.OutputReceiveDieMapY = map.DieMapY;
                wafer.OutputReceivePitchX = map.PitchX;
                wafer.OutputReceivePitchY = map.PitchY;
                wafer.OutputReceiveOriginX = map.OriginX;
                wafer.OutputReceiveOriginY = map.OriginY;
                wafer.OutputReceiveTotalCount = map.Entries.Count(e => e != null && e.IsTarget);
                wafer.UpdatedAt = DateTime.Now;

                List<DieMapEntry> ordered = BuildReceiveOrder(map);
                if (wafer.OutputReceiveSlots == null)
                    wafer.OutputReceiveSlots = new List<OutputReceiveSlotMaterial>();
                else
                    wafer.OutputReceiveSlots.Clear();

                for (int i = 0; i < ordered.Count; i++)
                {
                    DieMapEntry entry = ordered[i];
                    if (entry == null)
                        continue;

                    double relativeX = ToRelativeOutputX(entry.PosX);
                    double relativeY = ToRelativeOutputY(entry.PosY);
                    wafer.OutputReceiveSlots.Add(new OutputReceiveSlotMaterial
                    {
                        OrderIndex = i,
                        SequenceNo = entry.SequenceNo,
                        DieMapX = entry.DieMapX,
                        DieMapY = entry.DieMapY,
                        IsTarget = entry.IsTarget,
                        Result = entry.Result,
                        BinCode = entry.BinCode,
                        PosX = relativeX,
                        PosY = relativeY,
                        DieUid = entry.DieUid ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output map material persist failed: " + ex.Message + " - Failed");
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
            catch
            {
            }

            base.OnHandleDestroyed(e);
        }
    }
}
