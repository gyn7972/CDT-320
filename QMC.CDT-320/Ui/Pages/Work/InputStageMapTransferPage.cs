using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class InputStageMapTransferPage : PageBase
    {
        private Timer _refresh;
        private string _i18nTitle;
        private string _lastMapFrameObjId = "";
        private DieMapEntry _selectedEntry;
        private bool _pickStatusDirty;

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
                        RefreshActiveInputMapIfChanged();
                        ApplyLotProgress();
                    }
                    catch { }
                };
                _refresh.Start();
            }
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
                ToggleSelectedEntryTarget();
            };
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

                var frame = new DieTapeFrame
                {
                    ObjId = recipe.Frame.FrameSpecName,
                    GridX = Math.Max(1, recipe.Frame.GridX),
                    GridY = Math.Max(1, recipe.Frame.GridY),
                    PitchX = recipe.Frame.PitchX,
                    PitchY = recipe.Frame.PitchY,
                    OriginX = 0,
                    OriginY = 0,
                    Rotate = TapeFrameRotate.None
                };

                mapView.Map = DieMapGenerator.Generate(frame);
                _lastMapFrameObjId = mapView.Map != null ? mapView.Map.FrameObjId : "";
                _pickStatusDirty = false;
                RefreshDieGrid();
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

        private bool TryLoadActiveInputMap()
        {
            try
            {
                if (!IsInputMapPage())
                    return false;

                RestoreInputStageRuntimeFromSavedMaterial();

                var map = LotStorage.ActiveInputDieMap;
                if (map == null)
                {
                    map = MaterialStateService.BuildInputDieMapFromStageWafer();
                    if (map != null)
                    {
                        LotStorage.ActiveInputDieMap = map;
                        var host = FindForm() as Form1;
                        if (host != null && host.Controller != null)
                            host.Controller.ApplyInputDieMap(map, "InputStageMapTransferPage.RestoreSavedInputDieMap");
                    }
                }

                if (map == null)
                    return false;

                ApplyMap(map, "ACTIVE INPUT MAP");
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
                    stage.ApplyDieMappingResult(waferMap, dieMap.OriginX, dieMap.OriginY, dieMap.PitchX, dieMap.PitchY);
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

                var active = LotStorage.ActiveInputDieMap;
                if (active == null)
                    return;

                string frameId = active.FrameObjId ?? "";
                if (!ReferenceEquals(mapView.Map, active) || !string.Equals(_lastMapFrameObjId, frameId, StringComparison.Ordinal))
                    ApplyMap(active, "ACTIVE INPUT MAP");
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
                mapView.Caption = title;
                mapView.Map = map;
                _lastMapFrameObjId = map != null ? map.FrameObjId ?? "" : "";
                _pickStatusDirty = false;
                _selectedEntry = null;

                lblMapTitle.Text = title;
                lblProjectValue.Text = GetCurrentProjectName();
                lblBarcodeValue.Text = ResolveActiveWaferId();
                lblBinValue.Text = "0";
                lblPitchX.Text = map != null ? map.PitchX.ToString("F3") : "0";
                lblPitchY.Text = map != null ? map.PitchY.ToString("F3") : "0";
                lblDieNum.Text = "0 / " + (map != null ? map.TotalCells.ToString() : "0");
                ApplySpecInfoFromRecipe();
                RefreshDieGrid();
            }
            catch
            {
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
            foreach (var entry in map.Entries)
            {
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
                        "Input Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result != DialogResult.Yes)
                        return;
                }

                _pickStatusDirty = false;
                BuildOrFetchMap();
                RefreshDieGrid();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Input Map reload failed:\r\n" + ex.Message,
                    "Input Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    QMC.Common.MessageDialog.Show(this, "저장할 Input Map 데이터가 없습니다.",
                        "Input Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult result = QMC.Common.MessageDialog.Show(this,
                    "현재 Pick Status를 저장하고 Pickup Sequence를 갱신하시겠습니까?",
                    "Input Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
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
                    "Input Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Pick Status save failed:\r\n" + ex.Message,
                    "Input Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer != null)
                {
                    wafer.DieMapFrameObjId = map.FrameObjId ?? "";
                    wafer.HasInputStageAlignResult = true;
                    wafer.InputStageAlignOriginX = map.OriginX;
                    wafer.InputStageAlignOriginY = map.OriginY;
                    wafer.InputStageAlignPitchX = map.PitchX;
                    wafer.InputStageAlignPitchY = map.PitchY;
                    wafer.UpdatedAt = DateTime.Now;
                    if (wafer.DieIds == null)
                        wafer.DieIds = new System.Collections.Generic.List<string>();
                }

                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                        continue;

                    DieMaterial die = MaterialStateService.GetOrCreateDieMaterial(entry.DieUid);
                    if (wafer != null)
                    {
                        die.WaferID_Input = wafer.WaferId;
                        if (!wafer.DieIds.Contains(die.DieId))
                            wafer.DieIds.Add(die.DieId);
                    }

                    die.Wafer_IndexX = entry.GridX;
                    die.Wafer_IndexY = entry.GridY;
                    die.Input_BinCode = entry.BinCode;
                    die.Result = entry.Result;
                    if (die.WaferOffset == null)
                        die.WaferOffset = new VisionOffset();
                    die.WaferOffset.X = entry.X;
                    die.WaferOffset.Y = entry.Y;
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
                        "Input Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                    actionName + " 동작을 진행하시겠습니까?",
                    "Input Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
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
                        "Input Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                QMC.Common.MessageDialog.Show(this,
                    actionName + " 실패\r\nresult=" + result + "\r\nAlarm/Event Log를 확인하세요.",
                    "Input Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    actionName + " failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, actionName + " 실패:\r\n" + ex.Message,
                    "Input Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    "Input Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer == null)
                {
                    QMC.Common.MessageDialog.Show(this, "InputStage 위에 wafer Data가 없습니다.",
                        "Input Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                wafer.State = WaferMaterialState.WorkReady;
                wafer.UpdatedAt = DateTime.Now;
                MaterialStateService.NotifyAndSave("MapTransferManualAlignComplete");
                lblBarcodeValue.Text = wafer.WaferId;
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual align complete saved. wafer=" + wafer.WaferId + " - Ok");
                QMC.Common.MessageDialog.Show(this, "Manual Align Complete 저장 완료.",
                    "Input Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageMapTransferPage",
                    "Manual align complete failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "Manual Align Complete 실패:\r\n" + ex.Message,
                    "Input Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    "Input Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                btnManualAlignComplete.Enabled = enabled;
                btnNeedleBlockDown.Enabled = enabled;
                btnThetaMatchMove.Enabled = enabled;
                btnXyMatchMove.Enabled = enabled;
                btnPickStatusSave.Enabled = enabled;
                btnReloadActiveMap.Enabled = enabled;
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
                lblAxisX.Text = entry.X.ToString("F3");
                lblAxisY.Text = entry.Y.ToString("F3");
                lblBinRank.Text = entry.BinCode.ToString();
                lblDieNum.Text = string.Format("[{0},{1}] / {2}", entry.GridX, entry.GridY,
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

                object value = gridDieList.Rows[rowIndex].Cells[0].Value;
                int index;
                if (value == null || !int.TryParse(value.ToString(), out index))
                    return;

                DieMapEntry entry = mapView.Map.GetByIndex(index);
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
                gridDieList.Rows.Clear();
                DieMap map = mapView != null ? mapView.Map : null;
                if (map == null || map.Entries == null)
                {
                    UpdateMapCountLabels(null);
                    return;
                }

                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null)
                        continue;

                    gridDieList.Rows.Add(
                        entry.Index,
                        entry.GridX,
                        entry.GridY,
                        entry.IsTarget ? "TARGET" : "SKIP",
                        entry.Result,
                        entry.BinCode,
                        entry.X.ToString("F4"),
                        entry.Y.ToString("F4"),
                        entry.DieUid ?? "");
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

        private void SelectGridRow(DieMapEntry entry)
        {
            try
            {
                if (entry == null)
                    return;

                foreach (DataGridViewRow row in gridDieList.Rows)
                {
                    object value = row.Cells[0].Value;
                    int index;
                    if (value != null && int.TryParse(value.ToString(), out index) && index == entry.Index)
                    {
                        row.Selected = true;
                        if (row.Index >= 0)
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

