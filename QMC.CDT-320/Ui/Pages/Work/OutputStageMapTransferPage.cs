using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private string _lastMapSignature;
        private ContextMenuStrip _gridMenu;
        private ToolStripMenuItem _gridMoveMenuItem;
        private bool _manualMoveBusy;

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
                    return null;

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
                        : (entry.BinCode != 0 && entry.IsTarget ? "NEXT" : "WAIT");
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

        private void BuildGridContextMenu()
        {
            try
            {
                _gridMoveMenuItem = new ToolStripMenuItem("MOVE");
                _gridMoveMenuItem.Click += async (s, e) => await MoveSelectedBinSlotAsync().ConfigureAwait(true);

                _gridMenu = new ContextMenuStrip();
                _gridMenu.Items.Add(_gridMoveMenuItem);
                _gridMenu.Opening += (s, e) =>
                {
                    if (_gridMoveMenuItem != null)
                        _gridMoveMenuItem.Enabled = _selectedEntry != null && !_manualMoveBusy;
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

                // 원형 빈맵의 PosX/PosY는 ProcessPosition(센터) 기준 상대좌표 → 센터를 더해 절대 축 위치로 변환.
                double baseX = unit.Recipe.VisionX.ProcessPosition;
                double baseY = (_selectedSide == BinSide.Ng
                    ? unit.Recipe.NGStageY
                    : unit.Recipe.GoodStageY).ProcessPosition;
                double absX = baseX + entry.PosX;
                double absY = baseY + entry.PosY;

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

                    wafer.OutputReceiveSlots.Add(new OutputReceiveSlotMaterial
                    {
                        OrderIndex = i,
                        SequenceNo = entry.SequenceNo,
                        DieMapX = entry.DieMapX,
                        DieMapY = entry.DieMapY,
                        IsTarget = entry.IsTarget,
                        Result = entry.Result,
                        BinCode = entry.BinCode,
                        PosX = entry.PosX,
                        PosY = entry.PosY,
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
