using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class OutputStageMapTransferPage : PageBase
    {
        private Timer _refresh;
        private string _i18nTitle;
        private DieMapEntry _selectedEntry;
        private BinSide _selectedSide = BinSide.Good;

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
            btnPickStatusSave.Text = "SELECTED PLAN INIT";
            btnManualAlignComplete.Text = "GOOD PLAN INIT";
            btnNeedleBlockDown.Text = "NG PLAN INIT";
            btnThetaMatchMove.Text = "SAVE MATERIAL STATE";
            btnXyMatchMove.Text = "REFRESH DISPLAY";
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
            };

            gridDieList.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0)
                    return;
                SelectEntryByGridRow(e.RowIndex);
            };

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
            btnPickStatusSave.Click += (s, e) => InitializeSelectedReceivePlan();
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
                DieMap sourceMap = MaterialStateService.BuildDieMapFromWafer(sourceWafer);
                if (sourceMap == null)
                {
                    ApplyEmptyOutputMap(outputWafer, sourceWafer);
                    return;
                }

                DieMap displayMap = BuildDisplayMap(sourceMap, outputWafer);
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
                    entry.IsTarget = false;
                }
            }

            return display;
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
                        : (entry.IsTarget ? "NEXT" : "WAIT");
                    gridDieList.Rows.Add(
                        i,
                        entry.DieMapX,
                        entry.DieMapY,
                        status,
                        entry.Result,
                        entry.BinCode,
                        entry.PosX.ToString("F4"),
                        entry.PosY.ToString("F4"),
                        entry.DieUid ?? "");
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

                List<DieMapEntry> ordered = BuildReceiveOrder(map);
                if (rowIndex >= ordered.Count)
                    return;

                SelectEntry(ordered[rowIndex]);
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
