using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Motion;
using QMC.CDT320;
using QMC.CDT320.Bin;
using QMC.CDT320.Calibration;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Pages.WorkInfo;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class OutputStageMapTransferPage : PageBase
    {
        private enum OutputDieManualState
        {
            Wait,
            GoodComplete,
            NgComplete,
            Skip
        }

        private Timer _refresh;
        private string _i18nTitle;
        private DieMapEntry _selectedEntry;
        private BinSide _selectedSide = BinSide.Good;
        private string _lastMapSignature;
        private ContextMenuStrip _gridMenu;
        private ToolStripMenuItem _gridMoveMenuItem;
        private ToolStripMenuItem[] _gridMoveFrontPickerMenuItems;
        private ToolStripMenuItem[] _gridMoveRearPickerMenuItems;
        private ToolStripMenuItem[] _gridPlaceTestFrontPickerMenuItems;
        private ToolStripMenuItem[] _gridPlaceTestRearPickerMenuItems;
        private OutputPlaceTargetSelectDialog _placeTestDialog;
        private bool _manualMoveBusy;
        private static readonly PickerAxis[] PickerZAxes =
        {
            PickerAxis.PickerZ0,
            PickerAxis.PickerZ1,
            PickerAxis.PickerZ2,
            PickerAxis.PickerZ3
        };

        private sealed class OutputPlaceManualTargets
        {
            public double OutputStageY { get; set; }
            public double PickerX { get; set; }
            public double PickerY { get; set; }
            public double PickerYForward { get; set; }
            public double PickerT { get; set; }
            public string Formula { get; set; }
        }

        public OutputStageMapTransferPage() : this("work.page.outputMap")
        {
        }

        public OutputStageMapTransferPage(string titleI18n)
        {
            _i18nTitle = titleI18n;
            InitializeComponent();
            AssignStableControlNames();
            ConfigureOutputDesignerText();
            ApplyTitle();
            WireEvents();

            if (!IsDesignerMode())
            {
                SelectAvailableOutputSideFromMaterial();
                ReloadOutputMap();
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

                        ReloadOutputMap();
                    }
                    catch
                    {
                    }
                };
                if (ShouldRefreshVisible(this))
                    _refresh.Start();
            }
        }

        private void ConfigureOutputDesignerText()
        {
            lblHeader.Tag = "i18n:" + _i18nTitle;
            lblHeader.Text = Lang.T(_i18nTitle);
            lblMapTitle.Text = "OUTPUT GOOD RECEIVE MAP";
            grpReceiveMap.Text = "OUTPUT GOOD RECEIVE MAP";
            grpDieGrid.Text = "OUTPUT GOOD RECEIVE MAP DGV";
            grpAction.Text = "ACTION";
            mapView.Caption = "OUTPUT GOOD RECEIVE MAP";
            // 현재 기준: 출력 전환 화면도 공통 DieMapView 표시 옵션으로 맞춘다.
            mapView.BackColor = System.Drawing.Color.FromArgb(0xDD, 0xDD, 0xDD);
            mapView.ShowWaferOutline = true;
            mapView.ShowEquipmentAxes = true;
            mapView.CompactUsedBounds = true;
            mapView.EntryVisibilityPredicate = IsVisibleOutputMapEntry;
            mapView.EnableRectangleSelection = true;

            rbStandard.Text = "GOOD";
            rbStartIndex.Text = "NG";
            rbSelectPickStatus.Text = "WAIT / 대기";
            rbDragPickStatus.Text = "SKIP / 제외";
            if (rdoOutputStateGood != null)
                rdoOutputStateGood.Text = "GOOD / 완료";
            if (rdoOutputStateNg != null)
                rdoOutputStateNg.Text = "NG / 불량";
            if (btnApplyOutputDieState != null)
                btnApplyOutputDieState.Text = "APPLY SELECTED STATE";
            rbStandard.Checked = true;
            rbSelectPickStatus.Checked = true;
            rbSelectPickStatus.Enabled = true;
            rbDragPickStatus.Enabled = true;

            grpMapInfo.Text = "BIN / DIE INFO";
            grpMode.Text = "OUTPUT STAGE";
            lblBarcodeCaption.Text = "Source Wafer :";
            lblBinCaption.Text = "Side :";
            lblChipWCaption.Text = "Die Size X (mm)";
            lblChipHCaption.Text = "Die Size Y (mm)";
            lblPitchXCaption.Text = "Pitch Gap X (mm)";
            lblPitchYCaption.Text = "Pitch Gap Y (mm)";
            lblWaferDiaCaption.Text = "Wafer Diameter (mm)";
            lblAxisXCaption.Text = "Output Camera X (mm)";
            lblAxisYCaption.Text = "Good/NG Stage Y (mm)";
            lblBinRankCaption.Text = "Equipment Grid X/Y";
            lblDieNumCaption.Text = "Original DieMap X/Y";

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
            ConfigureOutputMapCoordinateGridColumns();
        }

        private void ConfigureOutputMapCoordinateGridColumns()
        {
            if (gridDieList == null)
                return;

            if (colGridX != null)
            {
                colGridX.HeaderText = "DieMapX (Raw)";
                colGridX.FillWeight = 65F;
            }
            if (colGridY != null)
            {
                colGridY.HeaderText = "DieMapY (Raw)";
                colGridY.FillWeight = 65F;
            }
            if (colAxisX != null)
            {
                colAxisX.HeaderText = "Process X(mm)";
                colAxisX.FillWeight = 90F;
            }
            if (colAxisY != null)
            {
                colAxisY.HeaderText = "Process Y(mm)";
                colAxisY.FillWeight = 90F;
            }

            if (gridDieList.Columns["colEquipmentGridX"] == null)
            {
                gridDieList.Columns.Insert(3, new DataGridViewTextBoxColumn
                {
                    Name = "colEquipmentGridX",
                    HeaderText = "Grid X",
                    FillWeight = 55F,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                });
            }
            if (gridDieList.Columns["colEquipmentGridY"] == null)
            {
                gridDieList.Columns.Insert(4, new DataGridViewTextBoxColumn
                {
                    Name = "colEquipmentGridY",
                    HeaderText = "Grid Y",
                    FillWeight = 55F,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                });
            }
        }

        private void ApplyTitle()
        {
            lblHeader.Tag = "i18n:" + _i18nTitle;
            lblHeader.Text = Lang.T(_i18nTitle);
            lblProjectValue.Text = GetCurrentProjectName();
        }

        private static void UpdateStageToggleButtonStyle(RadioButton radio)
        {
            if (radio == null)
                return;

            bool selected = radio.Checked;
            radio.BackColor = System.Drawing.Color.White;
            radio.ForeColor = selected
                ? System.Drawing.Color.FromArgb(0x00, 0x66, 0xB3)
                : System.Drawing.Color.FromArgb(0x25, 0x29, 0x2E);
            radio.FlatAppearance.BorderColor = selected
                ? System.Drawing.Color.FromArgb(0x00, 0x78, 0xD7)
                : System.Drawing.Color.FromArgb(0xC9, 0xCF, 0xD8);
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
            mapView.SelectionRectangleCompleted += entries =>
            {
                HandleOutputMapRectangleSelection(entries);
            };

            BuildGridContextMenu();
        }

        // 이하 표준 이벤트 핸들러들은 디자이너(InitializeComponent)에서 구독한다. 컨트롤명_이벤트명 규칙.
        private void gridDieList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            SelectEntryByGridRow(e.RowIndex);
        }

        private void rbStandard_CheckedChanged(object sender, EventArgs e)
        {
            UpdateStageToggleButtonStyle(rbStandard);
            if (!rbStandard.Checked)
                return;
            _selectedSide = BinSide.Good;
            _lastMapSignature = null;
            ReloadOutputMap();
        }

        private void rbStartIndex_CheckedChanged(object sender, EventArgs e)
        {
            UpdateStageToggleButtonStyle(rbStartIndex);
            if (!rbStartIndex.Checked)
                return;
            _selectedSide = BinSide.Ng;
            _lastMapSignature = null;
            ReloadOutputMap();
        }

        private void btnReloadActiveMap_Click(object sender, EventArgs e)
        {
            _lastMapSignature = null;
            ReloadOutputMap();
        }

        private async void btnPickStatusSave_Click(object sender, EventArgs e)
        {
            await MoveSelectedBinSlotAsync().ConfigureAwait(true);
        }

        private void btnManualAlignComplete_Click(object sender, EventArgs e)
        {
            InitializeReceivePlan(BinSide.Good);
        }

        private void btnNeedleBlockDown_Click(object sender, EventArgs e)
        {
            InitializeReceivePlan(BinSide.Ng);
        }

        private void btnThetaMatchMove_Click(object sender, EventArgs e)
        {
            SaveMaterialState();
        }

        private void btnXyMatchMove_Click(object sender, EventArgs e)
        {
            _lastMapSignature = null;
            ReloadOutputMap();
        }

        private void btnApplyOutputDieState_Click(object sender, EventArgs e)
        {
            ApplySelectedOutputDieState();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            try
            {
                if (IsDesignerMode())
                    return;

                if (!ShouldRefreshVisible(this))
                {
                    if (_refresh != null)
                        _refresh.Stop();
                    return;
                }

                if (_refresh != null && !_refresh.Enabled)
                    _refresh.Start();

                SelectAvailableOutputSideFromMaterial();
                _lastMapSignature = null;
                ReloadOutputMap();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output stage map visible refresh failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void SelectAvailableOutputSideFromMaterial()
        {
            try
            {
                WaferMaterial good = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood);
                WaferMaterial ng = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg);
                BinSide nextSide = good != null ? BinSide.Good : (ng != null ? BinSide.Ng : _selectedSide);

                if (nextSide == _selectedSide)
                    return;

                _selectedSide = nextSide;
                _lastMapSignature = null;
                if (nextSide == BinSide.Good)
                {
                    if (rbStandard != null && !rbStandard.Checked)
                        rbStandard.Checked = true;
                }
                else
                {
                    if (rbStartIndex != null && !rbStartIndex.Checked)
                        rbStartIndex.Checked = true;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output stage map side select failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private string GetCurrentProjectName()
        {
            try
            {
                RecipeProject project = LoadActiveOutputRecipeProject();
                if (project != null && !string.IsNullOrWhiteSpace(project.FileName))
                    return project.FileName;
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
                RecipeProject activeProject = LoadActiveOutputRecipeProject();
                DieMap recipeMap = LoadRecipeBinMap(_selectedSide);
                // 관리형 Recipe는 승인된 역할 맵을 기준으로 Material 상태만 overlay한다.
                // 승인 대기/불일치 상태에서 이전 Material 또는 원형 fallback으로 모션하지 않는다.
                DieMap baseMap = activeProject != null && activeProject.MapApprovalVersion > 0
                    ? recipeMap
                    : (materialMap ?? recipeMap);
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
                outputWafer != null ? outputWafer.WaferId ?? "" : "",
                outputWafer != null ? outputWafer.State.ToString() : "",
                outputWafer != null ? outputWafer.OutputReceiveSourceWaferId ?? "" : "",
                outputWafer != null ? outputWafer.OutputReceiveTotalCount.ToString() : "0",
                outputWafer != null ? outputWafer.UpdatedAt.ToString("O") : "",
                BuildOutputReceiveSlotHash(outputWafer).ToString(),
                map.DieMapX.ToString(),
                map.DieMapY.ToString(),
                map.PitchX.ToString("F4"),
                map.PitchY.ToString("F4"),
                map.DieSizeX.ToString("F4"),
                map.DieSizeY.ToString("F4"),
                map.OuterDiameterMm.ToString("F4"),
                map.OriginX.ToString("F4"),
                map.OriginY.ToString("F4"),
                (map.Entries != null ? map.Entries.Count : 0).ToString(),
                next.ToString());
        }

        private static int BuildOutputReceiveSlotHash(WaferMaterial outputWafer)
        {
            try
            {
                unchecked
                {
                    int hash = 17;
                    if (outputWafer == null || outputWafer.OutputReceiveSlots == null)
                        return hash;

                    foreach (OutputReceiveSlotMaterial slot in outputWafer.OutputReceiveSlots
                        .Where(s => s != null)
                        .OrderBy(s => s.OrderIndex))
                    {
                        hash = hash * 31 + slot.OrderIndex;
                        hash = hash * 31 + slot.SequenceNo;
                        hash = hash * 31 + slot.DieMapX;
                        hash = hash * 31 + slot.DieMapY;
                        hash = hash * 31 + (slot.IsTarget ? 1 : 0);
                        hash = hash * 31 + (int)slot.Result;
                        hash = hash * 31 + slot.BinCode;
                        hash = hash * 31 + slot.PosX.GetHashCode();
                        hash = hash * 31 + slot.PosY.GetHashCode();
                        hash = hash * 31 + (slot.DieUid != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(slot.DieUid) : 0);
                    }

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
                RecipeProject project = LoadActiveOutputRecipeProject();
                if (project == null)
                    return null;

                RecipeMapKind kind = side == BinSide.Ng ? RecipeMapKind.NgBin : RecipeMapKind.GoodBin;
                string path;
                string reason;
                // 현재 기준: Output 전환 화면도 Good/NG 모두 원본 wafer map index 기준으로 로드한다.
                DieMap map = RecipeDieMapResolver.LoadCompatibleMap(project, kind, out path, out reason);
                if (map == null || map.Entries == null || map.Entries.Count == 0)
                {
                    if (project.MapApprovalVersion > 0)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                            "Managed output map blocked until FINAL APPLY. side=" + side +
                            ", reason=" + (reason ?? "") + " - Check");
                        return null;
                    }
                    return CreateOutputCircleMapFromRecipe(project, side);
                }

                return DieMapGenerator.Normalize(map);
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

        private RecipeProject LoadActiveOutputRecipeProject()
        {
            try
            {
                Form1 host = FindForm() as Form1;
                string recipeName = host != null ? host.ActiveRecipeName : "";
                if (string.IsNullOrWhiteSpace(recipeName))
                {
                    var lot = LotStorage.ActiveLot;
                    recipeName = lot != null ? lot.RecipeName : "";
                }

                RecipeProject project = !string.IsNullOrWhiteSpace(recipeName)
                    ? RecipeStore.Load(System.IO.Path.GetFileNameWithoutExtension(recipeName))
                    : null;
                return project ?? RecipeStore.LoadLastOrDefault();
            }
            catch
            {
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
            display.Entries = display.Entries.Where(IsVisibleOutputMapEntry).ToList();
            DieMapGenerator.Normalize(display);

            double processX = ResolveOutputVisionProcessX();
            double processY = ResolveOutputStageProcessY(_selectedSide);
            List<DieMapEntry> ordered = BuildReceiveOrder(display);
            Dictionary<string, OutputReceiveSlotMaterial> savedSlots = BuildOutputReceiveSlotLookup(outputWafer);
            int nextIndex = outputWafer != null ? outputWafer.OutputReceiveNextIndex : 0;
            int total = ordered.Count;
            if (nextIndex < 0)
                nextIndex = 0;
            if (nextIndex > total)
                nextIndex = total;

            for (int i = 0; i < ordered.Count; i++)
            {
                DieMapEntry entry = ordered[i];
                OutputReceiveSlotMaterial savedSlot = null;
                if (savedSlots != null)
                    savedSlots.TryGetValue(BuildEntryGridKey(entry), out savedSlot);

                if (ShouldUseSavedOutputSlotState(savedSlot))
                {
                    entry.IsTarget = savedSlot.IsTarget;
                    entry.Result = savedSlot.Result;
                    entry.BinCode = savedSlot.BinCode;
                    continue;
                }

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

        private static Dictionary<string, OutputReceiveSlotMaterial> BuildOutputReceiveSlotLookup(WaferMaterial outputWafer)
        {
            var result = new Dictionary<string, OutputReceiveSlotMaterial>(StringComparer.Ordinal);
            try
            {
                if (outputWafer == null || outputWafer.OutputReceiveSlots == null)
                    return result;

                foreach (OutputReceiveSlotMaterial slot in outputWafer.OutputReceiveSlots)
                {
                    if (slot == null)
                        continue;

                    string key = BuildGridKey(slot.DieMapX, slot.DieMapY);
                    if (!result.ContainsKey(key))
                        result.Add(key, slot);
                }
            }
            catch
            {
            }
            finally
            {
            }

            return result;
        }

        private static bool ShouldUseSavedOutputSlotState(OutputReceiveSlotMaterial slot)
        {
            if (slot == null)
                return false;

            if (!slot.IsTarget)
                return true;

            if (slot.Result != DieResult.Unknown)
                return true;

            // DieUid가 있는 슬롯은 실제 배치된 Die 상태이므로 WAIT도 진행률 계산으로 덮어쓰지 않는다.
            return !string.IsNullOrWhiteSpace(slot.DieUid);
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
                DieSizeX = source.DieSizeX,
                DieSizeY = source.DieSizeY,
                OuterDiameterMm = source.OuterDiameterMm,
                EdgeSkipMode = source.EdgeSkipMode,
                SideEdgeSkip = source.SideEdgeSkip,
                TopBottomEdgeSkip = source.TopBottomEdgeSkip,
                OriginX = source.OriginX,
                OriginY = source.OriginY,
                SourceFileName = source.SourceFileName,
                SourceFormat = source.SourceFormat,
                SourcePitchFromFile = source.SourcePitchFromFile,
                SourceDeclaredCount = source.SourceDeclaredCount,
                SourceFirstX = source.SourceFirstX,
                SourceFirstY = source.SourceFirstY,
                SourceFirstPosX = source.SourceFirstPosX,
                SourceFirstPosY = source.SourceFirstPosY,
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
                    OriginalMapX = entry.OriginalMapX,
                    OriginalMapY = entry.OriginalMapY,
                    IsTarget = entry.IsTarget,
                    Result = entry.Result,
                    BinCode = entry.BinCode,
                    PosX = entry.PosX,
                    PosY = entry.PosY,
                    EquipmentGridX = entry.EquipmentGridX,
                    EquipmentGridY = entry.EquipmentGridY,
                    DieUid = entry.DieUid
                });
            }

            return DieMapGenerator.Normalize(clone);
        }

        private static bool IsVisibleOutputMapEntry(DieMapEntry entry)
        {
            // 현재 기준: SKIP은 수납 대상에서만 제외하고, 맵/리스트에는 상태로 표시한다.
            return entry != null;
        }

        private static int ResolveEntryMapX(DieMapEntry entry)
        {
            return DieMapGenerator.ResolveMapIndexX(entry);
        }

        private static int ResolveEntryMapY(DieMapEntry entry)
        {
            return DieMapGenerator.ResolveMapIndexY(entry);
        }

        private static string BuildGridKey(int x, int y)
        {
            return x.ToString() + ":" + y.ToString();
        }

        private static string BuildEntryGridKey(DieMapEntry entry)
        {
            return BuildGridKey(ResolveEntryMapX(entry), ResolveEntryMapY(entry));
        }

        private static string BuildEntryMapText(DieMapEntry entry)
        {
            return "[" + DieMapGenerator.ResolveOriginalMapIndexX(entry) + "," +
                DieMapGenerator.ResolveOriginalMapIndexY(entry) + "]";
        }

        private static string ResolveOutputDieGridStateText(DieMapEntry entry)
        {
            if (entry == null)
                return "";

            if (!entry.IsTarget)
                return "SKIP";

            if (entry.Result == DieResult.Good)
                return "GOOD";

            if (entry.Result == DieResult.NG)
                return "NG";

            return "WAIT";
        }

        private List<DieMapEntry> BuildReceiveOrder(DieMap map)
        {
            try
            {
                var project = LoadActiveOutputRecipeProject();
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
                ? map.Entries.Where(e => e != null && e.IsTarget).OrderBy(e => ResolveEntryMapY(e)).ThenBy(e => ResolveEntryMapX(e)).ToList()
                : new List<DieMapEntry>();
        }

        private void ApplyMap(DieMap map, WaferMaterial outputWafer, WaferMaterial sourceWafer)
        {
            try
            {
                string sideText = _selectedSide == BinSide.Ng ? "NG" : "GOOD";
                DieMapGenerator.Normalize(map);
                lblAxisYCaption.Text = sideText + " Stage Y (mm)";
                mapView.Caption = "OUTPUT " + sideText + " RECEIVE MAP";
                mapView.Map = map;
                _selectedEntry = null;

                lblMapTitle.Text = "OUTPUT " + sideText + " RECEIVE MAP";
                lblProjectValue.Text = GetCurrentProjectName();
                lblBarcodeValue.Text = sourceWafer != null ? sourceWafer.WaferId : "-";
                lblBinValue.Text = sideText;
                grpMapInfo.Text = "BIN / DIE INFO   Grid " + map.DieMapX + "x" + map.DieMapY +
                    "   Progress " + BuildProgressText(outputWafer, map);
                lblChipW.Text = map != null ? map.DieSizeX.ToString("F4") : "0";
                lblChipH.Text = map != null ? map.DieSizeY.ToString("F4") : "0";
                lblPitchX.Text = map != null
                    ? DieMapGenerator.CalculatePitchGap(map.PitchX, map.DieSizeX).ToString("F4")
                    : "0";
                lblPitchY.Text = map != null
                    ? DieMapGenerator.CalculatePitchGap(map.PitchY, map.DieSizeY).ToString("F4")
                    : "0";
                lblWaferDia.Text = map != null ? map.OuterDiameterMm.ToString("F3") : "0";
                lblAxisX.Text = ResolveOutputVisionProcessX().ToString("F3");
                lblAxisY.Text = ResolveOutputStageProcessY(_selectedSide).ToString("F3");
                lblBinRank.Text = "0 / 0";
                lblDieNum.Text = "- / -";

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
            mapView.Caption = "OUTPUT GOOD RECEIVE MAP";
            lblAxisYCaption.Text = (_selectedSide == BinSide.Ng ? "NG" : "GOOD") + " Stage Y (mm)";
            lblMapTitle.Text = "OUTPUT GOOD RECEIVE MAP";
            lblProjectValue.Text = GetCurrentProjectName();
            lblBarcodeValue.Text = sourceWafer != null ? sourceWafer.WaferId : "-";
            lblBinValue.Text = _selectedSide == BinSide.Ng ? "NG" : "GOOD";
            grpMapInfo.Text = "BIN / DIE INFO   NO MAP";
            lblChipW.Text = "0";
            lblChipH.Text = "0";
            lblPitchX.Text = "0";
            lblPitchY.Text = "0";
            lblWaferDia.Text = "0";
            lblAxisX.Text = ResolveOutputVisionProcessX().ToString("F3");
            lblAxisY.Text = ResolveOutputStageProcessY(_selectedSide).ToString("F3");
            lblBinRank.Text = "0 / 0";
            lblDieNum.Text = "NO MAP";
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

        private string BuildNextTargetText(WaferMaterial outputWafer, DieMap map)
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
            return string.Format("[{0},{1}] / {2}", ResolveEntryMapX(next), ResolveEntryMapY(next), ordered.Count);
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

                    string status = ResolveOutputDieGridStateText(entry);
                    int rowIndex = gridDieList.Rows.Add(
                        i,
                        DieMapGenerator.ResolveOriginalMapIndexX(entry),
                        DieMapGenerator.ResolveOriginalMapIndexY(entry),
                        FormatEquipmentGrid(entry.EquipmentGridX),
                        FormatEquipmentGrid(entry.EquipmentGridY),
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
                ApplySelectedOutputCoordinateInfo(entry);
                SetOutputStateRadioFromEntry(entry);
                SelectGridRow(entry);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ApplySelectedOutputCoordinateInfo(DieMapEntry entry)
        {
            if (entry == null)
                return;

            lblAxisX.Text = entry.PosX.ToString("F3");
            lblAxisY.Text = entry.PosY.ToString("F3");
            lblBinRank.Text = FormatEquipmentGrid(entry.EquipmentGridX) + " / " +
                FormatEquipmentGrid(entry.EquipmentGridY);
            lblDieNum.Text = DieMapGenerator.ResolveOriginalMapIndexX(entry).ToString() + " / " +
                DieMapGenerator.ResolveOriginalMapIndexY(entry).ToString();
        }

        private static string FormatEquipmentGrid(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "-";
            return value.ToString("0.###", CultureInfo.InvariantCulture);
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

        private void HandleOutputMapRectangleSelection(IReadOnlyList<DieMapEntry> entries)
        {
            try
            {
                if (entries == null || entries.Count <= 0)
                    return;

                DieMapEntry first = entries[0];
                _selectedEntry = first;
                SelectGridRow(first);
                SetOutputStateRadioFromEntry(first);
                ApplySelectedOutputCoordinateInfo(first);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output Die Map 사각 선택 처리 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ApplySelectedOutputDieState()
        {
            try
            {
                DieMap map = mapView != null ? mapView.Map : null;
                if (map == null || map.Entries == null)
                {
                    QMC.Common.MessageDialog.Show(this, "상태를 변경할 Output Die Map 데이터가 없습니다.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string reason;
                if (!CanEditOutputDieState(out reason))
                {
                    QMC.Common.MessageDialog.Show(this, reason,
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                List<DieMapEntry> entries = ResolveSelectedOutputDieEntries(map);
                if (entries.Count <= 0)
                {
                    QMC.Common.MessageDialog.Show(this, "상태를 변경할 Die를 먼저 선택하세요.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                OutputDieManualState state = ResolveSelectedOutputDieManualState();
                string stateText = ResolveOutputManualStateDisplayName(state);
                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    "선택 Output Die " + entries.Count + "개 상태를 [" + stateText + "]로 변경하시겠습니까?\r\n" +
                    "첫 Die=" + BuildEntryMapText(entries[0]) + "\r\n" +
                    "UID=" + (entries[0].DieUid ?? ""),
                    "Output Stage Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                for (int i = 0; i < entries.Count; i++)
                    ApplyManualStateToOutputEntry(entries[i], state);

                for (int i = 0; i < entries.Count; i++)
                    SyncManualOutputReceiveSlotState(entries[i], state);

                for (int i = 0; i < entries.Count; i++)
                    SyncManualOutputDieState(entries[i], "OutputMapManualDieState");
                MaterialStateService.NotifyAndSave("OutputMapManualDieState");

                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output Die 상태 일괄 변경 완료. side=" + _selectedSide +
                    ", count=" + entries.Count +
                    ", firstGrid=(" + ResolveEntryMapX(entries[0]) + "," + ResolveEntryMapY(entries[0]) + ")" +
                    ", state=" + stateText + " - Ok");

                _lastMapSignature = null;
                ReloadOutputMap();
                QMC.Common.MessageDialog.Show(this, "선택 Output Die " + entries.Count + "개 상태 변경 완료.",
                    "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output Die 상태 변경 실패: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "Output Die 상태 변경 실패:\r\n" + ex.Message,
                    "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private bool CanEditOutputDieState(out string reason)
        {
            reason = string.Empty;
            try
            {
                if (_manualMoveBusy)
                {
                    reason = "좌표 이동 동작 중에는 Output Die 상태를 변경할 수 없습니다.";
                    return false;
                }

                var host = FindForm() as Form1;
                var controller = host != null ? host.Controller : null;
                if (controller != null &&
                    (controller.Status == EquipmentStatus.AutoRunning ||
                     controller.Status == EquipmentStatus.Initializing ||
                     controller.IsSequenceRunning ||
                     controller.IsManualBusy))
                {
                    reason = "장비 동작 중에는 Output Die 상태를 변경할 수 없습니다.\r\n" +
                             "Auto/Manual 동작을 정지한 뒤 다시 시도하세요.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "Output Die 상태 변경 가능 여부 확인 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    reason + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private List<DieMapEntry> ResolveSelectedOutputDieEntries(DieMap map)
        {
            var result = new List<DieMapEntry>();
            try
            {
                if (map == null || map.Entries == null)
                    return result;

                IReadOnlyList<DieMapEntry> selected = mapView != null ? mapView.SelectedEntries : null;
                if (selected != null && selected.Count > 1)
                {
                    for (int i = 0; i < selected.Count; i++)
                    {
                        DieMapEntry entry = FindEquivalentEntry(map, selected[i]);
                        if (entry != null && !ContainsEntry(result, entry))
                            result.Add(entry);
                    }
                }

                if (result.Count <= 0)
                {
                    DieMapEntry entry = FindEquivalentEntry(map, _selectedEntry);
                    if (entry != null)
                        result.Add(entry);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output Die Map 선택 목록 확인 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return result;
        }

        private static DieMapEntry FindEquivalentEntry(DieMap map, DieMapEntry source)
        {
            if (map == null || map.Entries == null || source == null)
                return null;

            int sourceX = ResolveEntryMapX(source);
            int sourceY = ResolveEntryMapY(source);
            string sourceUid = source.DieUid ?? "";
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null)
                    continue;
                if (ResolveEntryMapX(entry) == sourceX &&
                    ResolveEntryMapY(entry) == sourceY &&
                    string.Equals(entry.DieUid ?? "", sourceUid, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            return null;
        }

        private static bool ContainsEntry(List<DieMapEntry> entries, DieMapEntry target)
        {
            if (entries == null || target == null)
                return false;

            int targetX = ResolveEntryMapX(target);
            int targetY = ResolveEntryMapY(target);
            string targetUid = target.DieUid ?? "";
            for (int i = 0; i < entries.Count; i++)
            {
                DieMapEntry entry = entries[i];
                if (entry == null)
                    continue;
                if (ResolveEntryMapX(entry) == targetX &&
                    ResolveEntryMapY(entry) == targetY &&
                    string.Equals(entry.DieUid ?? "", targetUid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private OutputDieManualState ResolveSelectedOutputDieManualState()
        {
            if (rdoOutputStateGood != null && rdoOutputStateGood.Checked)
                return OutputDieManualState.GoodComplete;
            if (rdoOutputStateNg != null && rdoOutputStateNg.Checked)
                return OutputDieManualState.NgComplete;
            if (rbDragPickStatus != null && rbDragPickStatus.Checked)
                return OutputDieManualState.Skip;

            return OutputDieManualState.Wait;
        }

        private static void ApplyManualStateToOutputEntry(DieMapEntry entry, OutputDieManualState state)
        {
            if (entry == null)
                return;

            switch (state)
            {
                case OutputDieManualState.GoodComplete:
                    entry.IsTarget = true;
                    entry.Result = DieResult.Good;
                    entry.BinCode = BinCodeMap.GoodBin;
                    return;
                case OutputDieManualState.NgComplete:
                    entry.IsTarget = true;
                    entry.Result = DieResult.NG;
                    entry.BinCode = BinCodeMap.MaxBin;
                    return;
                case OutputDieManualState.Skip:
                    entry.IsTarget = false;
                    entry.Result = DieResult.Unknown;
                    entry.BinCode = 0;
                    entry.SequenceNo = 0;
                    return;
                case OutputDieManualState.Wait:
                default:
                    entry.IsTarget = true;
                    entry.Result = DieResult.Unknown;
                    entry.BinCode = 0;
                    return;
            }
        }

        private void SyncManualOutputReceiveSlotState(DieMapEntry entry, OutputDieManualState state)
        {
            try
            {
                if (entry == null)
                    return;

                WaferMaterial wafer = GetSelectedOutputWafer();
                if (wafer == null || wafer.OutputReceiveSlots == null)
                    return;

                OutputReceiveSlotMaterial slot = FindOutputReceiveSlotByEntry(wafer, entry);
                if (slot == null)
                    return;

                // 현재 기준: 수동 GOOD/NG 완료 슬롯은 DieUid가 없어도 다음 place 대상에서 제외한다.
                slot.IsTarget = entry.IsTarget;
                slot.Result = entry.IsTarget ? entry.Result : DieResult.Unknown;
                slot.BinCode = entry.IsTarget ? entry.BinCode : 0;
                slot.IsOutputInspectionDone =
                    state == OutputDieManualState.GoodComplete ||
                    state == OutputDieManualState.NgComplete;
                slot.IsOutputInspectionOk = state == OutputDieManualState.GoodComplete;
                if (state == OutputDieManualState.Wait)
                {
                    slot.IsOutputInspectionDone = false;
                    slot.IsOutputInspectionOk = false;
                }
                if (state == OutputDieManualState.Skip)
                {
                    slot.IsOutputInspectionDone = false;
                    slot.IsOutputInspectionOk = false;
                    slot.DieUid = "";
                }

                wafer.OutputReceiveNextIndex = CalculateNextOutputReceiveIndex(wafer.OutputReceiveSlots);
                wafer.State = IsOutputReceiveCompleteForDisplay(wafer)
                    ? WaferMaterialState.Finish
                    : WaferMaterialState.Working;
                wafer.UpdatedAt = DateTime.Now;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output Receive Slot 수동 상태 동기화 실패. die=" + (entry != null ? entry.DieUid ?? "" : "") +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private OutputReceiveSlotMaterial FindOutputReceiveSlotByEntry(WaferMaterial wafer, DieMapEntry entry)
        {
            if (wafer == null || wafer.OutputReceiveSlots == null || entry == null)
                return null;

            int mapX = ResolveEntryMapX(entry);
            int mapY = ResolveEntryMapY(entry);
            OutputReceiveSlotMaterial slot = wafer.OutputReceiveSlots.FirstOrDefault(s =>
                s != null &&
                s.DieMapX == mapX &&
                s.DieMapY == mapY);

            if (slot != null)
                return slot;

            return wafer.OutputReceiveSlots.FirstOrDefault(s =>
                s != null &&
                s.OriginalMapX == DieMapGenerator.ResolveOriginalMapIndexX(entry) &&
                s.OriginalMapY == DieMapGenerator.ResolveOriginalMapIndexY(entry));
        }

        private static bool IsOutputReceiveCompleteForDisplay(WaferMaterial wafer)
        {
            if (wafer == null || wafer.OutputReceiveSlots == null)
                return false;

            List<OutputReceiveSlotMaterial> targetSlots = wafer.OutputReceiveSlots
                .Where(s => s != null && s.IsTarget)
                .ToList();
            return targetSlots.Count > 0 && targetSlots.All(s => !IsOutputReceiveSlotPending(s));
        }

        private void SyncManualOutputDieState(DieMapEntry entry, string reason)
        {
            if (entry == null)
                return;

            WaferMaterial wafer = GetSelectedOutputWafer();
            OutputReceiveSlotMaterial slot = FindOutputReceiveSlotByEntry(wafer, entry);
            string dieUid = slot != null ? slot.DieUid : "";
            if (string.IsNullOrWhiteSpace(dieUid))
                return;

            string message;
            bool ok = MaterialStateService.ApplyManualDieState(
                dieUid,
                entry.IsTarget,
                entry.IsTarget ? entry.Result : DieResult.Unknown,
                entry.IsTarget ? entry.BinCode : 0,
                "",
                reason,
                out message);
            if (!ok)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output Die 상태 공통 동기화 실패. die=" + (dieUid ?? "") +
                    ", message=" + message + " - Failed");
            }
        }

        private void SetOutputStateRadioFromEntry(DieMapEntry entry)
        {
            try
            {
                OutputDieManualState state = ResolveEntryOutputManualState(entry);
                switch (state)
                {
                    case OutputDieManualState.GoodComplete:
                        if (rdoOutputStateGood != null)
                            rdoOutputStateGood.Checked = true;
                        break;
                    case OutputDieManualState.NgComplete:
                        if (rdoOutputStateNg != null)
                            rdoOutputStateNg.Checked = true;
                        break;
                    case OutputDieManualState.Skip:
                        if (rbDragPickStatus != null)
                            rbDragPickStatus.Checked = true;
                        break;
                    case OutputDieManualState.Wait:
                    default:
                        if (rbSelectPickStatus != null)
                            rbSelectPickStatus.Checked = true;
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

        private static OutputDieManualState ResolveEntryOutputManualState(DieMapEntry entry)
        {
            if (entry == null)
                return OutputDieManualState.Wait;
            if (!entry.IsTarget)
                return OutputDieManualState.Skip;
            if (entry.Result == DieResult.Good)
                return OutputDieManualState.GoodComplete;
            if (entry.Result == DieResult.NG)
                return OutputDieManualState.NgComplete;

            return OutputDieManualState.Wait;
        }

        private static string ResolveOutputManualStateDisplayName(OutputDieManualState state)
        {
            switch (state)
            {
                case OutputDieManualState.GoodComplete:
                    return "GOOD / 완료";
                case OutputDieManualState.NgComplete:
                    return "NG / 불량";
                case OutputDieManualState.Skip:
                    return "SKIP / 제외";
                case OutputDieManualState.Wait:
                default:
                    return "WAIT / 대기";
            }
        }

        private DieMap CreateOutputCircleMapFromRecipe(RecipeProject recipe, BinSide side)
        {
            if (recipe == null)
                return null;

            TapeFrameSubset frame = recipe.OutputFrame ?? recipe.Frame;
            if (frame == null)
                return null;
            double dieSizeX = recipe.Die != null && recipe.Die.WidthMm > 0.0
                ? recipe.Die.WidthMm
                : (frame.DieSizeX > 0.0 ? frame.DieSizeX : 1.0);
            double dieSizeY = recipe.Die != null && recipe.Die.HeightMm > 0.0
                ? recipe.Die.HeightMm
                : (frame.DieSizeY > 0.0 ? frame.DieSizeY : 1.0);
            double centerStepX = DieMapGenerator.CalculateCenterStep(dieSizeX, frame.PitchX);
            double centerStepY = DieMapGenerator.CalculateCenterStep(dieSizeY, frame.PitchY);
            double diameterMm = frame.OuterDiameterMm > 0.0 ? frame.OuterDiameterMm : 0.0;
            bool millimeterSkip = !string.IsNullOrWhiteSpace(frame.EdgeSkipMode) &&
                frame.EdgeSkipMode.IndexOf("Millimeter", StringComparison.OrdinalIgnoreCase) >= 0;
            WaferEdgeSkipMode skipMode = millimeterSkip ? WaferEdgeSkipMode.Millimeter : WaferEdgeSkipMode.Grid;
            double sideSkip = millimeterSkip ? frame.SideEdgeSkipMm : frame.SideEdgeSkip;
            double topBottomSkip = millimeterSkip ? frame.TopBottomEdgeSkipMm : frame.TopBottomEdgeSkip;
            DieMap map = DieMapGenerator.GenerateCircularWafer(
                diameterMm,
                centerStepX,
                centerStepY,
                dieSizeX,
                dieSizeY,
                skipMode,
                sideSkip,
                topBottomSkip,
                (side == BinSide.Ng ? "NG" : "GOOD") + "_OUTPUT_CIRCLE");

            int binCode = side == BinSide.Ng ? 255 : 1;
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null)
                    continue;
                entry.BinCode = entry.IsTarget ? binCode : 0;
                entry.DieUid = (side == BinSide.Ng ? "NG" : "GOOD") + "-D" +
                               entry.DieMapY.ToString("000") + "-" + entry.DieMapX.ToString("000");
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
                _gridMenu.Items.Add(new ToolStripSeparator());
                _gridMenu.Items.Add(BuildPickerPlaceTestMenu("PLACE TEST FRONT PICKER", PickerSequenceSide.Front, out _gridPlaceTestFrontPickerMenuItems));
                _gridMenu.Items.Add(BuildPickerPlaceTestMenu("PLACE TEST REAR PICKER", PickerSequenceSide.Rear, out _gridPlaceTestRearPickerMenuItems));
                _gridMenu.Opening += (s, e) =>
                {
                    bool enabled = _selectedEntry != null && !_manualMoveBusy;
                    if (_gridMoveMenuItem != null)
                        _gridMoveMenuItem.Enabled = enabled;

                    SetPickerMoveMenuEnabled(_gridMoveFrontPickerMenuItems, enabled);
                    SetPickerMoveMenuEnabled(_gridMoveRearPickerMenuItems, enabled);
                    SetPickerMoveMenuEnabled(_gridPlaceTestFrontPickerMenuItems, enabled);
                    SetPickerMoveMenuEnabled(_gridPlaceTestRearPickerMenuItems, enabled);
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

        private ToolStripMenuItem BuildPickerPlaceTestMenu(string title, PickerSequenceSide side, out ToolStripMenuItem[] items)
        {
            ToolStripMenuItem root = new ToolStripMenuItem(title);
            items = new ToolStripMenuItem[4];

            for (int i = 0; i < items.Length; i++)
            {
                int pickerNo = i + 1;
                ToolStripMenuItem item = new ToolStripMenuItem("PICKER #" + pickerNo);
                item.Click += (s, e) => ShowPlaceTestDialogForSelectedOutputSlot(side, pickerNo);
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

        private void ShowPlaceTestDialogForSelectedOutputSlot(PickerSequenceSide side, int pickerNo)
        {
            try
            {
                DieMapEntry entry = _selectedEntry;
                if (entry == null)
                {
                    QMC.Common.MessageDialog.Show(this, "Place Test 대상 Output Slot이 선택되지 않았습니다.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Form1 host = FindForm() as Form1;
                if (host == null || host.Controller == null)
                {
                    QMC.Common.MessageDialog.Show(this, "Place Test를 실행할 Controller 정보를 찾을 수 없습니다.",
                        "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                CloseOutputMapPlaceTestDialog();

                _placeTestDialog = new OutputPlaceTargetSelectDialog(
                    host.Controller,
                    side,
                    pickerNo,
                    _selectedSide,
                    entry,
                    ResolveSelectedEntryOrderIndex(entry));
                _placeTestDialog.FormClosed += (s, e) => _placeTestDialog = null;

                IWin32Window ownerWindow = FindForm();
                if (ownerWindow != null)
                    _placeTestDialog.Show(ownerWindow);
                else
                    _placeTestDialog.Show();

                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) +
                    " Place Test dialog opened. slot=" + BuildEntryMapText(entry) +
                    ", outputSide=" + _selectedSide + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Place Test dialog open failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "Place Test 다이얼로그 실행 실패:\r\n" + ex.Message,
                    "Output Stage Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private int ResolveSelectedEntryOrderIndex(DieMapEntry entry)
        {
            try
            {
                DieMap map = mapView != null ? mapView.Map : null;
                if (entry == null || map == null)
                    return 0;

                List<DieMapEntry> ordered = BuildReceiveOrder(map);
                for (int i = 0; i < ordered.Count; i++)
                {
                    DieMapEntry item = ordered[i];
                    if (item == null)
                        continue;

                    if (ResolveEntryMapX(item) == ResolveEntryMapX(entry) &&
                        ResolveEntryMapY(item) == ResolveEntryMapY(entry) &&
                        string.Equals(item.DieUid ?? "", entry.DieUid ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            catch
            {
            }
            finally
            {
            }

            return 0;
        }

        private void CloseOutputMapPlaceTestDialog()
        {
            try
            {
                if (_placeTestDialog == null || _placeTestDialog.IsDisposed)
                    return;

                _placeTestDialog.Close();
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
        /// 인터락: VisionX(공유레일) 이동 전 Front/Rear Picker를 Z->Y->X 순서로 Avoid 선행 이동.<br/>
        /// 축 구조 D3: 행(Y)=스테이지 {side}BinY, 열(X)=VisionX(카메라). 각 단계 타임아웃 가드 + 정지.
        /// </summary>
        private async Task MoveSelectedBinSlotAsync()
        {
            Form1 host = FindForm() as Form1;
            IDisposable actionScope = null;
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

                JogSpeedType speedType;
                if (!ConfirmManualMapMoveSpeed(
                    this,
                    "Output Stage Map",
                    "빈 슬롯 " + BuildEntryMapText(entry) + "의 좌표로 이동하시겠습니까?\r\n" +
                    "X(VisionX)=" + absX.ToString("F3") + " mm, Y(StageY)=" + absY.ToString("F3") + " mm",
                    out speedType))
                {
                    return;
                }

                int timeoutMs = ResolveManualMoveTimeoutMs(host);
                SetActionButtonsEnabled(false);
                _manualMoveBusy = true;
                actionScope = host.Controller.BeginManualActionScope(
                    ManualMotionScopeKind.SpeedOnly,
                    "OutputStageMapTransferPage:MoveSelectedBinSlot");

                // 1) VisionX(공유레일) 이동 전 Front/Rear Picker를 Z 상승 -> Y 후진 -> X Avoid 순서로 선행 이동(간섭 차단).
                int prepareResult = await AwaitManualMoveStepAsync(
                    MovePickersToAvoidForOutputMoveAsync(host, speedType),
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

                // 2) 행(Y): 스테이지 Y축
                BinStageAxis yAxis = _selectedSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
                int rowResult = await AwaitManualMoveStepAsync(
                    MoveOutputStageYToSlotWithInterlockAsync(unit, _selectedSide, absY, timeoutMs, "OutputVisionMove", speedType),
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
                    unit.MoveStageAxis(BinStageAxis.VisionX, absX, speedType, 0.0),
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
                try
                {
                    if (actionScope != null)
                        actionScope.Dispose();
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                        "수동 이동 스코프 정리 중 오류: " + ex.Message + " - Failed");
                }
                finally
                {
                    _manualMoveBusy = false;
                    SetActionButtonsEnabled(true);
                }
            }
        }

        private async Task MoveSelectedSlotByPickerAsync(PickerSequenceSide side, int pickerNo)
        {
            Form1 host = FindForm() as Form1;
            IDisposable actionScope = null;
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

                JogSpeedType speedType;
                if (!ConfirmManualMapMoveSpeed(
                    this,
                    "Output Stage Map",
                    ResolvePickerMoveTitle(side, pickerNo) + "를 선택 빈 슬롯 Place 위치로 이동하시겠습니까?\r\n" +
                    "Slot=" + BuildEntryMapText(entry) + "\r\n" +
                    "StageY=" + targets.OutputStageY.ToString("F3") + " mm\r\n" +
                    "PickerX=" + targets.PickerX.ToString("F3") + " mm\r\n" +
                    "PickerY=" + targets.PickerY.ToString("F3") + " mm\r\n" +
                    "PickerY Forward=" + targets.PickerYForward.ToString("F3") + " mm\r\n" +
                    "PickerT=" + targets.PickerT.ToString("F3") + " deg\r\n" +
                    "Formula=" + (targets.Formula ?? string.Empty) + "\r\n" +
                    "PickerZ는 이동하지 않습니다.",
                    out speedType))
                {
                    return;
                }

                int timeoutMs = ResolveManualMoveTimeoutMs(host);
                SetActionButtonsEnabled(false);
                _manualMoveBusy = true;
                actionScope = host.Controller.BeginManualActionScope(
                    ManualMotionScopeKind.SpeedOnly,
                    "OutputStageMapTransferPage:" + ResolvePickerMoveTitle(side, pickerNo));

                int result = await AwaitManualMoveStepAsync(
                    MoveSelectedSlotByPickerCoreAsync(host, _selectedSide, side, pickerNo, entry, targets, timeoutMs, speedType),
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
                try
                {
                    if (actionScope != null)
                        actionScope.Dispose();
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                        "Picker 수동 이동 스코프 정리 중 오류: " + ex.Message + " - Failed");
                }
                finally
                {
                    _manualMoveBusy = false;
                    SetActionButtonsEnabled(true);
                }
            }
        }

        private async Task<int> MoveSelectedSlotByPickerCoreAsync(
            Form1 host,
            BinSide outputSide,
            PickerSequenceSide side,
            int pickerNo,
            DieMapEntry entry,
            OutputPlaceManualTargets targets,
            int timeoutMs,
            JogSpeedType speedType)
        {
            try
            {
                OutputStageUnit unit = host.Machine.OutputStageUnit;

                // 수동 Picker Place 이동 전 OutputVisionX를 먼저 Avoid로 빼서 공유레일 간섭을 줄인다.
                int visionAvoidResult = await MoveOutputVisionXToAvoidForPickerMoveAsync(host, unit, timeoutMs, speedType).ConfigureAwait(true);
                if (visionAvoidResult != 0)
                    return visionAvoidResult;

                // Picker X 이동 전 대상 Picker는 Z 상승 후 Y 후진을 먼저 완료한다.
                int targetPickerSafeResult = await MoveTargetPickerZAndYToAvoidForPickerMoveAsync(host, side, timeoutMs, speedType).ConfigureAwait(true);
                if (targetPickerSafeResult != 0)
                    return targetPickerSafeResult;

                // 반대편 Picker는 Output 영역 진입 전 Z->Y->X 순서로 완전 Avoid 위치에 둔다.
                int otherPickerAvoidResult = await MoveOtherPickerOutOfOutputZoneForPickerMoveAsync(host, side, timeoutMs, speedType).ConfigureAwait(true);
                if (otherPickerAvoidResult != 0)
                    return otherPickerAvoidResult;

                BinStageAxis yAxis = outputSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
                int stageResult = await MoveOutputStageYToSlotWithInterlockAsync(
                    unit,
                    outputSide,
                    targets.OutputStageY,
                    timeoutMs,
                    "ManualPickerMove",
                    speedType).ConfigureAwait(true);
                if (stageResult != 0)
                    return stageResult;

                int pickerIndex = pickerNo - 1;
                PickerAxis tAxis = GetPickerTAxis(pickerIndex);
                string targetName = "DiePlacePosition[P" + pickerNo + "];ManualOutputDieMapMove";

                // PickerY는 후진된 상태에서 X/T를 먼저 맞춘 뒤, 마지막에 Place Y로 전진시킨다.
                Task<int> movePickerX = MovePickerAxisAsync(host, side, PickerAxis.PickerX, targets.PickerX, targetName, speedType);
                Task<int> movePickerT = MovePickerAxisAsync(host, side, tAxis, targets.PickerT, targetName, speedType);
                int[] moveResults = await Task.WhenAll(movePickerX, movePickerT).ConfigureAwait(true);
                for (int i = 0; i < moveResults.Length; i++)
                {
                    if (moveResults[i] != 0)
                        return moveResults[i];
                }

                Task<int> waitPickerX = WaitPickerAxisInPositionAsync(host, side, PickerAxis.PickerX, targets.PickerX, timeoutMs);
                Task<int> waitPickerT = WaitPickerAxisInPositionAsync(host, side, tAxis, targets.PickerT, timeoutMs);
                int[] waitResults = await Task.WhenAll(waitPickerX, waitPickerT).ConfigureAwait(true);
                for (int i = 0; i < waitResults.Length; i++)
                {
                    if (waitResults[i] != 0)
                        return waitResults[i];
                }

                int pickerYMoveResult = await MovePickerAxisAsync(
                    host,
                    side,
                    PickerAxis.PickerY,
                    targets.PickerY,
                    targetName + ";PickerPhase=ForwardY",
                    speedType).ConfigureAwait(true);
                if (pickerYMoveResult != 0)
                    return pickerYMoveResult;

                int pickerYWaitResult = await WaitPickerAxisInPositionAsync(
                    host,
                    side,
                    PickerAxis.PickerY,
                    targets.PickerY,
                    timeoutMs).ConfigureAwait(true);
                if (pickerYWaitResult != 0)
                    return pickerYWaitResult;

                if (!IsPickerAxisInPosition(host, side, PickerAxis.PickerX, targets.PickerX) ||
                    !IsPickerAxisInPosition(host, side, PickerAxis.PickerY, targets.PickerY) ||
                    !IsPickerAxisInPosition(host, side, tAxis, targets.PickerT))
                    return -1;

                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    ResolvePickerMoveTitle(side, pickerNo) +
                    " output place view move complete. slot=" + BuildEntryMapText(entry) +
                    ", formula=" + (targets.Formula ?? string.Empty) +
                    ", outputStageYState=" + unit.BuildStageAxisState(yAxis, targets.OutputStageY) +
                    ", pickerXState=" + BuildPickerAxisState(host, side, PickerAxis.PickerX, targets.PickerX) +
                    ", pickerYState=" + BuildPickerAxisState(host, side, PickerAxis.PickerY, targets.PickerY) +
                    ", pickerTState=" + BuildPickerAxisState(host, side, tAxis, targets.PickerT) +
                    " - Ok");
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

        /// <summary>VisionX(공유레일) 이동 전 Front/Rear Picker를 모두 Z->Y->X 순서로 Avoid 위치로 이동합니다.</summary>
        private async Task<int> MovePickersToAvoidForOutputMoveAsync(Form1 host, JogSpeedType speedType)
        {
            try
            {
                if (host == null || host.Machine == null)
                    return -1;

                int timeoutMs = ResolveManualMoveTimeoutMs(host);

                int frontResult = await MovePickerToAvoidOrderedAsync(
                    host,
                    PickerSequenceSide.Front,
                    timeoutMs,
                    "OutputVisionMove",
                    speedType).ConfigureAwait(true);
                if (frontResult != 0)
                    return frontResult;

                int rearResult = await MovePickerToAvoidOrderedAsync(
                    host,
                    PickerSequenceSide.Rear,
                    timeoutMs,
                    "OutputVisionMove",
                    speedType).ConfigureAwait(true);
                if (rearResult != 0)
                    return rearResult;

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

        private async Task<int> MoveOutputVisionXToAvoidForPickerMoveAsync(Form1 host, OutputStageUnit unit, int timeoutMs, JogSpeedType speedType)
        {
            try
            {
                if (unit == null || unit.Recipe == null || unit.Recipe.VisionX == null)
                    return -1;

                if (unit.IsVisionXInAvoidPosition())
                    return 0;

                int result = await unit.MoveStageAxis(BinStageAxis.VisionX, unit.Recipe.VisionX.AvoidPosition, speedType, 0.0).ConfigureAwait(true);
                if (result != 0)
                    return result;

                AxisMoveWaitResult wait = await unit.WaitStageAxisMoveDoneInPosition(
                    BinStageAxis.VisionX,
                    unit.Recipe.VisionX.AvoidPosition,
                    timeoutMs).ConfigureAwait(true);
                if (wait == null || !wait.Success)
                    return -1;

                return unit.IsVisionXInAvoidPosition() ? 0 : -1;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Picker 이동 전 OutputVisionX Avoid 이동 실패: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private static bool IsFrontPickerInOutputZone(PickerFrontUnit picker)
        {
            if (picker == null)
                return false;

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (picker.IsFrontPickerInDiePlacePosition(pickerNo))
                    return true;
            }

            return false;
        }

        private static bool IsRearPickerInOutputZone(PickerRearUnit picker)
        {
            if (picker == null)
                return false;

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (picker.IsRearPickerInDiePlacePosition(pickerNo))
                    return true;
            }

            return false;
        }

        /// <summary>선택 Picker가 Output Zone에 들어가기 전, 상대 Picker를 Z->Y->X 순서로 Avoid 이동합니다.</summary>
        private async Task<int> MoveOtherPickerOutOfOutputZoneForPickerMoveAsync(Form1 host, PickerSequenceSide movingSide, int timeoutMs, JogSpeedType speedType)
        {
            try
            {
                if (host == null || host.Machine == null)
                    return -1;

                PickerSequenceSide oppositeSide = movingSide == PickerSequenceSide.Front
                    ? PickerSequenceSide.Rear
                    : PickerSequenceSide.Front;

                return await MovePickerToAvoidOrderedAsync(
                    host,
                    oppositeSide,
                    timeoutMs,
                    "ManualPickerMoveOther",
                    speedType).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Picker 진입 전 상대 Picker Output Zone 회피 이동 실패: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> PrepareOutputStageYMoveAsync(OutputStageUnit unit, BinSide side, int timeoutMs, JogSpeedType speedType)
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

                    int zResult = await unit.MoveStageAxis(BinStageAxis.GoodBinZ, targetZ, speedType, 0.0).ConfigureAwait(true);
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

        private async Task<int> MoveOutputStageYToSlotWithInterlockAsync(
            OutputStageUnit unit,
            BinSide side,
            double targetY,
            int timeoutMs,
            string reasonTag,
            JogSpeedType speedType)
        {
            try
            {
                if (unit == null)
                    return -1;

                int prepareResult = await PrepareOutputStageYMoveAsync(unit, side, timeoutMs, speedType).ConfigureAwait(true);
                if (prepareResult != 0)
                    return prepareResult;

                BinStageAxis yAxis = side == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output " + side + " StageY slot move start. axis=" + yAxis +
                    ", targetY=" + targetY.ToString("F6") +
                    ", reason=" + (reasonTag ?? string.Empty) +
                    ". MoveStageAxis interlock path is used. - Start");

                // 해당 GOOD/NG StageY 이동은 기존 MoveStageAxis 경로로 인터락을 확인한 뒤 진행한다.
                int stageResult = await unit.MoveStageAxis(yAxis, targetY, speedType, 0.0).ConfigureAwait(true);
                if (stageResult != 0)
                    return stageResult;

                AxisMoveWaitResult stageWait = await unit.WaitStageAxisMoveDoneInPosition(yAxis, targetY, timeoutMs).ConfigureAwait(true);
                if (stageWait == null || !stageWait.Success)
                    return -1;

                if (!unit.IsStageAxisInPosition(yAxis, targetY, ResolveOutputStageAxisTolerance(unit, yAxis)))
                    return -1;

                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output " + side + " StageY slot move complete. " +
                    unit.BuildStageAxisState(yAxis, targetY) + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    "Output StageY slot move failed: side=" + side +
                    ", targetY=" + targetY.ToString("F6") +
                    ", error=" + ex.Message + " - Failed");
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
                    outputSide,
                    out offsetX,
                    out offsetY,
                    out offsetReason))
                {
                    reason = offsetReason;
                    return false;
                }

                PlaceCoordinateResult calibratedTarget =
                    PickerMotionTargetResolver.CalculateOutputPlaceTarget(
                        host.Machine,
                        side,
                        pickerIndex,
                        "OutputStageMapTransferPage.ManualOutputMap",
                        entry.DieUid,
                        outputSide,
                        0.0,
                        entry.PosX,
                        entry.PosY,
                        0.0,
                        offsetX,
                        offsetY);

                targets = new OutputPlaceManualTargets
                {
                    OutputStageY = calibratedTarget.OutputStageY,
                    PickerX = calibratedTarget.PickerX,
                    PickerY = calibratedTarget.PickerY,
                    PickerYForward = Math.Abs(calibratedTarget.PickerY),
                    PickerT = calibratedTarget.PickerT,
                    Formula = calibratedTarget.Formula
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

        private static VisionFocusPickerSide ToVisionFocusPickerSide(PickerSequenceSide side)
        {
            return side == PickerSequenceSide.Front
                ? VisionFocusPickerSide.Front
                : VisionFocusPickerSide.Rear;
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

        private static Task<int> MovePickerAxisAsync(
            Form1 host,
            PickerSequenceSide side,
            PickerAxis axis,
            double target,
            string targetName,
            JogSpeedType speedType)
        {
            if (host == null || host.Machine == null)
                return Task.FromResult(-1);

            if (side == PickerSequenceSide.Front)
            {
                PickerFrontUnit front = host.Machine.PickerFrontUnit;
                return front != null ? front.MoveFrontPickerAxis(axis, target, speedType, 0.0, targetName) : Task.FromResult(-1);
            }

            PickerRearUnit rear = host.Machine.PickerRearUnit;
            return rear != null ? rear.MoveRearPickerAxis(axis, target, speedType, 0.0, targetName) : Task.FromResult(-1);
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

        private static string BuildPickerAxisState(Form1 host, PickerSequenceSide side, PickerAxis axis, double target)
        {
            BaseAxis item = ResolvePickerAxis(host, side, axis);
            if (item == null)
                return "axis=" + axis + ", target=" + target.ToString("F6") + ", state=axis-not-found";

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;

            return "axis=" + axis +
                   ", name=" + item.Name +
                   ", servo=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (item.IsMoving ? "Y" : "N") +
                   ", actual=" + item.ActualPosition.ToString("F6") +
                   ", target=" + target.ToString("F6") +
                   ", tolerance=" + tolerance.ToString("F6");
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

        private static string ResolvePickerSideName(PickerSequenceSide side)
        {
            return side == PickerSequenceSide.Front ? "FRONT" : "REAR";
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
                    QMC.Common.Alarms.AlarmSeverity.Error,
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
                    string.IsNullOrWhiteSpace(title) ? "Output Stage Map" : title,
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
                if (btnApplyOutputDieState != null)
                    btnApplyOutputDieState.Enabled = enabled;
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
                wafer.OutputReceiveDieSizeX = map.DieSizeX;
                wafer.OutputReceiveDieSizeY = map.DieSizeY;
                wafer.OutputReceiveOuterDiameterMm = map.OuterDiameterMm;
                wafer.OutputReceiveOriginX = map.OriginX;
                wafer.OutputReceiveOriginY = map.OriginY;
                wafer.OutputReceiveTotalCount = map.Entries.Count(e => e != null && e.IsTarget);
                wafer.UpdatedAt = DateTime.Now;

                List<DieMapEntry> ordered = BuildReceiveOrder(map);
                wafer.OutputReceiveNextIndex = CalculateNextOutputReceiveIndex(ordered);
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
                        // 현재 기준: OutputReceiveSlot도 웨이퍼맵 원본 X/Y 인덱스를 저장한다.
                        DieMapX = ResolveEntryMapX(entry),
                        DieMapY = ResolveEntryMapY(entry),
                        OriginalMapX = DieMapGenerator.ResolveOriginalMapIndexX(entry),
                        OriginalMapY = DieMapGenerator.ResolveOriginalMapIndexY(entry),
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

        private async Task<int> MoveTargetPickerZAndYToAvoidForPickerMoveAsync(Form1 host, PickerSequenceSide side, int timeoutMs, JogSpeedType speedType)
        {
            try
            {
                // Picker X 이동 전 대상 Picker는 Z를 올리고 Y를 후진시켜 X 이동 간섭을 줄인다.
                int zResult = await MovePickerZAxesToAvoidAsync(
                    host,
                    side,
                    timeoutMs,
                    "ManualPickerMoveTarget",
                    speedType).ConfigureAwait(true);
                if (zResult != 0)
                    return zResult;

                return await MovePickerAxisToTeachingIfNeededAsync(
                    host,
                    side,
                    PickerAxis.PickerY,
                    "AvoidPosition",
                    timeoutMs,
                    "ManualPickerMoveTarget.SafeY",
                    speedType).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    ResolvePickerSideName(side) + " picker Z/Y avoid prepare failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerToAvoidOrderedAsync(Form1 host, PickerSequenceSide side, int timeoutMs, string reasonTag, JogSpeedType speedType)
        {
            try
            {
                // 수동 Output 이동 안전 순서: Z 상승 -> Y 후진 -> X Avoid 순서로 이동한다.
                int zResult = await MovePickerZAxesToAvoidAsync(host, side, timeoutMs, reasonTag, speedType).ConfigureAwait(true);
                if (zResult != 0)
                    return zResult;

                int yResult = await MovePickerAxisToTeachingIfNeededAsync(
                    host,
                    side,
                    PickerAxis.PickerY,
                    "AvoidPosition",
                    timeoutMs,
                    reasonTag + ".SafeY",
                    speedType).ConfigureAwait(true);
                if (yResult != 0)
                    return yResult;

                int xResult = await MovePickerAxisToTeachingIfNeededAsync(
                    host,
                    side,
                    PickerAxis.PickerX,
                    "AvoidPosition",
                    timeoutMs,
                    reasonTag + ".SafeX",
                    speedType).ConfigureAwait(true);
                if (xResult != 0)
                    return xResult;

                return IsPickerSideAtOrderedAvoidPosition(host, side) ? 0 : -1;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    ResolvePickerSideName(side) + " ordered avoid move failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerZAxesToAvoidAsync(Form1 host, PickerSequenceSide side, int timeoutMs, string reasonTag, JogSpeedType speedType)
        {
            try
            {
                for (int i = 0; i < PickerZAxes.Length; i++)
                {
                    int result = await MovePickerAxisToTeachingIfNeededAsync(
                        host,
                        side,
                        PickerZAxes[i],
                        "AvoidPosition",
                        timeoutMs,
                        reasonTag + ".SafeZ",
                        speedType).ConfigureAwait(true);
                    if (result != 0)
                        return result;
                }

                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    ResolvePickerSideName(side) + " PickerZ avoid move failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerAxisToTeachingIfNeededAsync(
            Form1 host,
            PickerSequenceSide side,
            PickerAxis axis,
            string positionName,
            int timeoutMs,
            string phaseName,
            JogSpeedType speedType)
        {
            try
            {
                if (host == null || host.Machine == null)
                    return -1;

                double target = GetPickerTeachingPosition(host, side, axis, positionName);
                if (IsPickerAxisInPosition(host, side, axis, target))
                    return 0;

                string targetName = positionName + ";ManualOutputDieMapMove;PickerPhase=" + phaseName;
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    ResolvePickerSideName(side) + " " + axis +
                    " move to " + positionName + ". target=" + target.ToString("F6") +
                    ", phase=" + phaseName + " - Start");

                int result = await MovePickerAxisAsync(host, side, axis, target, targetName, speedType).ConfigureAwait(true);
                if (result != 0)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                        ResolvePickerSideName(side) + " " + axis +
                        " move to " + positionName + " failed. result=" + result + " - Failed");
                    return result;
                }

                result = await WaitPickerAxisInPositionAsync(host, side, axis, target, timeoutMs).ConfigureAwait(true);
                if (result != 0)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                        ResolvePickerSideName(side) + " " + axis +
                        " wait " + positionName + " failed. target=" + target.ToString("F6") +
                        ", state=" + BuildPickerAxisState(host, side, axis, target) + " - Failed");
                    return result;
                }

                if (!IsPickerAxisInPosition(host, side, axis, target))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                        ResolvePickerSideName(side) + " " + axis +
                        " final " + positionName + " check failed. " +
                        BuildPickerAxisState(host, side, axis, target) + " - Failed");
                    return -1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageMapTransferPage",
                    ResolvePickerSideName(side) + " " + axis +
                    " teaching move failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private static bool IsPickerSideAtOrderedAvoidPosition(Form1 host, PickerSequenceSide side)
        {
            if (host == null || host.Machine == null)
                return false;

            if (!IsPickerAxisInPosition(host, side, PickerAxis.PickerX, GetPickerTeachingPosition(host, side, PickerAxis.PickerX, "AvoidPosition")))
                return false;

            if (!IsPickerAxisInPosition(host, side, PickerAxis.PickerY, GetPickerTeachingPosition(host, side, PickerAxis.PickerY, "AvoidPosition")))
                return false;

            for (int i = 0; i < PickerZAxes.Length; i++)
            {
                PickerAxis axis = PickerZAxes[i];
                if (!IsPickerAxisInPosition(host, side, axis, GetPickerTeachingPosition(host, side, axis, "AvoidPosition")))
                    return false;
            }

            return true;
        }

        private static int CalculateNextOutputReceiveIndex(List<DieMapEntry> ordered)
        {
            if (ordered == null || ordered.Count <= 0)
                return 0;

            int targetCount = 0;
            for (int index = 0; index < ordered.Count; index++)
            {
                DieMapEntry entry = ordered[index];
                if (entry == null)
                    continue;

                if (!entry.IsTarget)
                    continue;

                targetCount++;
                // 현재 기준: 표시용 Output map의 DieUid는 맵 셀 UID일 수 있으므로 Result 기준으로 다음 place 대상을 계산한다.
                if (entry.Result == DieResult.Unknown)
                    return index;
            }

            return targetCount > 0 ? targetCount : 0;
        }

        private static int CalculateNextOutputReceiveIndex(List<OutputReceiveSlotMaterial> slots)
        {
            if (slots == null || slots.Count <= 0)
                return 0;

            OutputReceiveSlotMaterial next = slots
                .Where(s => s != null && s.IsTarget && IsOutputReceiveSlotPending(s))
                .OrderBy(s => s.OrderIndex)
                .FirstOrDefault();
            if (next != null)
                return next.OrderIndex;

            int targetCount = slots.Count(s => s != null && s.IsTarget);
            return targetCount > 0 ? targetCount : 0;
        }

        private static bool IsOutputReceiveSlotPending(OutputReceiveSlotMaterial slot)
        {
            return slot != null &&
                   slot.IsTarget &&
                   slot.Result == DieResult.Unknown &&
                   string.IsNullOrWhiteSpace(slot.DieUid);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                CloseOutputMapPlaceTestDialog();
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
