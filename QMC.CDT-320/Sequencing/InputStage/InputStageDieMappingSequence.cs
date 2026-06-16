using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.Recipes;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal enum InputStageDieMappingStep
    {
        Idle,
        CheckUnit,
        MoveTopPoint,
        FindTopPoint,
        MoveBottomPoint,
        FindBottomPoint,
        MoveLeftPoint,
        FindLeftPoint,
        MoveRightPoint,
        FindRightPoint,
        CalculateDieMap,
        ApplyDieMap,
        Complete,
        Error
    }

    internal sealed class InputStageDieMappingSequence : InputStageSequenceBase<InputStageDieMappingStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();

        private readonly Dictionary<string, MappedMarkPoint> _mappedPoints = new Dictionary<string, MappedMarkPoint>(StringComparer.OrdinalIgnoreCase);
        private WaferMaterial _wafer;
        private TapeFrameSpec _frameSpec;
        private DieMap _dieMap;
        private WaferMapData _waferMap;
        private int _createdDieCount;

        public InputStageDieMappingSequence(MachineSequenceContext context)
            : base(context, InputStageSequenceKind.DieMapping, "InputStageDieMappingSequence")
        {
        }

        protected override InputStageDieMappingStep IdleStep { get { return InputStageDieMappingStep.Idle; } }
        protected override InputStageDieMappingStep InitialStep { get { return InputStageDieMappingStep.CheckUnit; } }
        protected override InputStageDieMappingStep CompleteStep { get { return InputStageDieMappingStep.Complete; } }
        protected override InputStageDieMappingStep ErrorStep { get { return InputStageDieMappingStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case InputStageDieMappingStep.CheckUnit:
                        return Task.FromResult(CheckDieMappingUnit());
                    // 상단 포인트 이동
                    case InputStageDieMappingStep.MoveTopPoint:
                        return MoveMarkPointAsync(Stage.Recipe.DieMap.Top, InputStageDieMappingStep.FindTopPoint, ct);
                    // 상단 포인트 찾기
                    case InputStageDieMappingStep.FindTopPoint:
                        return FindMarkPointAsync(Stage.Recipe.DieMap.Top, InputStageDieMappingStep.MoveBottomPoint, ct);
                    // 하단 포인트 이동
                    case InputStageDieMappingStep.MoveBottomPoint:
                        return MoveMarkPointAsync(Stage.Recipe.DieMap.Bottom, InputStageDieMappingStep.FindBottomPoint, ct);
                    // 하단 포인트 찾기
                    case InputStageDieMappingStep.FindBottomPoint:
                        return FindMarkPointAsync(Stage.Recipe.DieMap.Bottom, InputStageDieMappingStep.MoveLeftPoint, ct);
                    // 좌측 포인트 이동
                    case InputStageDieMappingStep.MoveLeftPoint:
                        return MoveMarkPointAsync(Stage.Recipe.DieMap.Left, InputStageDieMappingStep.FindLeftPoint, ct);
                    // 좌측 포인트 찾기
                    case InputStageDieMappingStep.FindLeftPoint:
                        return FindMarkPointAsync(Stage.Recipe.DieMap.Left, InputStageDieMappingStep.MoveRightPoint, ct);
                    // 우측 포인트 이동
                    case InputStageDieMappingStep.MoveRightPoint:
                        return MoveMarkPointAsync(Stage.Recipe.DieMap.Right, InputStageDieMappingStep.FindRightPoint, ct);
                    // 우측 포인트 찾기
                    case InputStageDieMappingStep.FindRightPoint:
                        return FindMarkPointAsync(Stage.Recipe.DieMap.Right, InputStageDieMappingStep.CalculateDieMap, ct);
                    // 다이 맵 계산
                    case InputStageDieMappingStep.CalculateDieMap:
                        return Task.FromResult(CalculateDieMap());
                    // 다이 맵 적용
                    case InputStageDieMappingStep.ApplyDieMap:
                        return Task.FromResult(ApplyDieMap());
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-STAGE-DIEMAP-STEP-EX", "InputStageDieMappingSequence", "Die mapping step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckDieMappingUnit()
        {
            try
            {
                int result = CheckUnit(InputStageDieMappingStep.MoveTopPoint);
                if (result != 0)
                    return result;

                if (Stage.Recipe == null)
                    return Fail("IN-STAGE-DIEMAP-RECIPE", Stage.Name, "Input stage recipe is not available.");

                string servoReason = BuildDieMappingServoReason();
                if (!string.IsNullOrEmpty(servoReason))
                    return Fail("IN-STAGE-DIEMAP-SERVO", Stage.Name, "Input stage servo is not on. " + servoReason);

                Stage.Recipe.EnsurePositionObjects();
                if (Stage.Recipe.DieMap == null)
                    return Fail("IN-STAGE-DIEMAP-RECIPE", Stage.Name, "Input stage die map recipe is not available.");

                Stage.Recipe.DieMap.EnsurePoints();
                result = CheckMarkPoint(Stage.Recipe.DieMap.Top);
                if (result != 0) return result;
                result = CheckMarkPoint(Stage.Recipe.DieMap.Bottom);
                if (result != 0) return result;
                result = CheckMarkPoint(Stage.Recipe.DieMap.Left);
                if (result != 0) return result;
                result = CheckMarkPoint(Stage.Recipe.DieMap.Right);
                if (result != 0) return result;

                if (!Stage.HasWaferOnStage())
                    return Fail("IN-STAGE-DIEMAP-WAFER", "Material",
                        "InputStage wafer data was not found. HasWaferOnStage=false, CurrentWaferMaterial=" +
                        (Stage.CurrentWaferMaterial != null ? Stage.CurrentWaferMaterial.WaferId : "null"));

                _wafer = Stage.GetCurrentStageWaferMaterial();
                if (_wafer == null)
                    _wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (_wafer == null)
                    return Fail("IN-STAGE-DIEMAP-WAFER", "Material",
                        "InputStage wafer material is not available. CurrentWaferMaterial=null, MaterialLocation=InputStage empty.");

                if (Stage.PitchX == 0.0 || Stage.PitchY == 0.0)
                    return Fail("IN-STAGE-DIEMAP-ALIGN", Stage.Name,
                        "InputStage align result is not available. Run align before die mapping. PitchX=" +
                        Stage.PitchX + ", PitchY=" + Stage.PitchY);

                _frameSpec = ResolveFrameSpecForWafer(_wafer);
                if (_frameSpec == null)
                    return Fail("IN-STAGE-DIEMAP-SPEC", "Material",
                        "TapeFrame spec was not found. waferId=" + _wafer.WaferId +
                        ", specName=" + _wafer.TapeFrameSpecName);
                if (_frameSpec.GridX <= 0 || _frameSpec.GridY <= 0)
                    return Fail("IN-STAGE-DIEMAP-SPEC", "Material",
                        "TapeFrame grid is invalid. specName=" + _frameSpec.Name +
                        ", gridX=" + _frameSpec.GridX + ", gridY=" + _frameSpec.GridY);

                if (Options.RequireVisionAlign && Stage.Vision == null && !IsSimulationOrDryRun())
                    return Fail("IN-STAGE-DIEMAP-VISION", Stage.Name, "Vision client is required but not available.");

                _mappedPoints.Clear();
                CurrentStep = InputStageDieMappingStep.MoveTopPoint;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-CHECK-EX", "InputStageDieMappingSequence", "Die mapping unit check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckMarkPoint(InputStageDieMapMarkPoint point)
        {
            try
            {
                if (point == null || !point.Enabled)
                    return Fail("IN-STAGE-DIEMAP-POINT", Stage.Name, "Die map mark point is disabled or missing.");
                if (string.IsNullOrWhiteSpace(point.Name))
                    return Fail("IN-STAGE-DIEMAP-POINT", Stage.Name, "Die map mark point name is empty.");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-POINT-EX", Stage.Name, "Die map mark point check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveMarkPointAsync(InputStageDieMapMarkPoint point, InputStageDieMappingStep nextStep, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (point == null)
                    return Fail("IN-STAGE-DIEMAP-POINT", Stage.Name, "Die map mark point is null.");

                if (Options.EnableMotion)
                {
                    string areaReason;
                    if (!Stage.IsInputStageWorkPointInArea(point.VisionXPosition, point.StageYPosition, out areaReason))
                        return Fail("IN-STAGE-DIEMAP-WORK-AREA", Stage.Name,
                            point.Name + " mark target is outside input stage work area. " + areaReason);

                    Task<int> moveY = MoveAxisCommandAsync(WaferStageAxis.WaferY, point.StageYPosition, point.Name + " StageY", ct);
                    Task<int> moveX = MoveAxisCommandAsync(WaferStageAxis.VisionX, point.VisionXPosition, point.Name + " VisionX", ct);
                    int[] moveResults = await Task.WhenAll(moveY, moveX).ConfigureAwait(false);
                    if (moveResults[0] != 0) return moveResults[0];
                    if (moveResults[1] != 0) return moveResults[1];

                    Task<int> waitY = WaitAxisInPositionResultAsync(WaferStageAxis.WaferY, point.StageYPosition, point.Name + " StageY", ct);
                    Task<int> waitX = WaitAxisInPositionResultAsync(WaferStageAxis.VisionX, point.VisionXPosition, point.Name + " VisionX", ct);
                    int[] waitResults = await Task.WhenAll(waitY, waitX).ConfigureAwait(false);
                    if (waitResults[0] != 0) return waitResults[0];
                    if (waitResults[1] != 0) return waitResults[1];

                }

                CurrentStep = nextStep;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-MOVE-EX", Stage.Name, point != null ? point.Name + " move failed: " + ex.Message : "Mark point move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> FindMarkPointAsync(InputStageDieMapMarkPoint point, InputStageDieMappingStep nextStep, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (point == null)
                    return Fail("IN-STAGE-DIEMAP-POINT", Stage.Name, "Die map mark point is null.");

                VisionAlignResult vision = await RequestVisionPcOffsetWithRetryAsync(ResolveTargetId(), point.Name, ct).ConfigureAwait(false);
                if (vision == null)
                    return Fail("IN-STAGE-DIEMAP-VISION", "Vision", point.Name + " vision offset receive failed.");

                point.VisionOffsetX = vision.DeltaX;
                point.VisionOffsetY = vision.DeltaY;
                _mappedPoints[point.Name] = new MappedMarkPoint
                {
                    Name = point.Name,
                    X = Stage.CameraX.ActualPosition + vision.DeltaX,
                    Y = Stage.StageY.ActualPosition + vision.DeltaY,
                    OffsetX = vision.DeltaX,
                    OffsetY = vision.DeltaY
                };

                WriteLog("InputStageDieMappingSequence",
                    "Die map mark found. point=" + point.Name +
                    ", x=" + _mappedPoints[point.Name].X.ToString("F6") +
                    ", y=" + _mappedPoints[point.Name].Y.ToString("F6") +
                    ", dx=" + vision.DeltaX.ToString("F6") +
                    ", dy=" + vision.DeltaY.ToString("F6") + " - Ok");

                CurrentStep = nextStep;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-VISION-EX", "Vision", point != null ? point.Name + " vision failed: " + ex.Message : "Vision mark find failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CalculateDieMap()
        {
            try
            {
                MappedMarkPoint top;
                MappedMarkPoint bottom;
                MappedMarkPoint left;
                MappedMarkPoint right;
                if (!TryGetMappedPoint("Top", out top) ||
                    !TryGetMappedPoint("Bottom", out bottom) ||
                    !TryGetMappedPoint("Left", out left) ||
                    !TryGetMappedPoint("Right", out right))
                {
                    return Fail("IN-STAGE-DIEMAP-POINTS", "Vision", "Die mapping requires Top/Bottom/Left/Right mark results.");
                }

                DieMap sourceMap = ResolveSourceInputDieMap(_wafer);
                if (!IsUsableSourceMap(sourceMap))
                {
                    return Fail("IN-STAGE-DIEMAP-SOURCE-MAP", "InputStageDieMappingSequence",
                        "Input die map is not available. Create and save an input wafer map from Recipe > INPUT MAP CREATE first.");
                }

                int gridX = sourceMap != null && sourceMap.GridX > 0 ? sourceMap.GridX : Math.Max(1, _frameSpec.GridX);
                int gridY = sourceMap != null && sourceMap.GridY > 0 ? sourceMap.GridY : Math.Max(1, _frameSpec.GridY);
                double pitchX = gridX > 1 ? Math.Abs(right.X - left.X) / (gridX - 1) : ResolvePitchX();
                double pitchY = gridY > 1 ? Math.Abs(bottom.Y - top.Y) / (gridY - 1) : ResolvePitchY();
                if (pitchX <= 0.0)
                    pitchX = sourceMap != null && sourceMap.PitchX > 0.0 ? sourceMap.PitchX : ResolvePitchX();
                if (pitchY <= 0.0)
                    pitchY = sourceMap != null && sourceMap.PitchY > 0.0 ? sourceMap.PitchY : ResolvePitchY();
                if (pitchX <= 0.0 || pitchY <= 0.0)
                    return Fail("IN-STAGE-DIEMAP-PITCH", "InputStageDieMappingSequence", "Die map pitch is invalid.");

                double signX = right.X >= left.X ? 1.0 : -1.0;
                double signY = bottom.Y >= top.Y ? 1.0 : -1.0;
                double originX = left.X;
                double originY = top.Y;
                double centerX = (left.X + right.X) / 2.0;
                double centerY = (top.Y + bottom.Y) / 2.0;
                double waferRadius = ResolveWaferRadiusFromSpecOrMarks(_frameSpec, left, right, top, bottom);

                _dieMap = new DieMap
                {
                    FrameObjId = BuildFrameObjId(_wafer),
                    GridX = gridX,
                    GridY = gridY,
                    PitchX = pitchX,
                    PitchY = pitchY,
                    OriginX = originX,
                    OriginY = originY,
                    CreatedAt = DateTime.Now
                };

                _waferMap = new WaferMapData
                {
                    WaferId = _wafer != null ? _wafer.WaferId : "",
                    ColumnCount = gridX,
                    RowCount = gridY,
                    DieMap = new bool[gridY, gridX],
                    Ref1Row = gridY / 2,
                    Ref1Col = Math.Max(0, gridX / 4),
                    Ref2Row = gridY / 2,
                    Ref2Col = gridX > 1 ? Math.Min(gridX - 1, (gridX * 3) / 4) : 0
                };

                int index = 0;
                int targetCount = 0;
                for (int row = 0; row < gridY; row++)
                {
                    for (int col = 0; col < gridX; col++)
                    {
                        double x = originX + signX * pitchX * col;
                        double y = originY + signY * pitchY * row;
                        DieMapEntry sourceEntry = sourceMap != null ? sourceMap.GetCell(col, row) : null;
                        bool target = sourceEntry != null
                            ? sourceEntry.IsTarget
                            : IsInsideWaferCircle(x, y, centerX, centerY, waferRadius);
                        if (target)
                            targetCount++;

                        _waferMap.DieMap[row, col] = target;
                        _dieMap.Entries.Add(new DieMapEntry
                        {
                            Index = index++,
                            SequenceNo = sourceEntry != null ? sourceEntry.SequenceNo : 0,
                            GridX = col,
                            GridY = row,
                            IsTarget = target,
                            Result = sourceEntry != null ? sourceEntry.Result : DieResult.Unknown,
                            BinCode = sourceEntry != null ? sourceEntry.BinCode : 0,
                            X = x,
                            Y = y,
                            DieUid = sourceEntry != null && !string.IsNullOrWhiteSpace(sourceEntry.DieUid)
                                ? sourceEntry.DieUid
                                : BuildDieId(_wafer, row, col)
                        });
                    }
                }

                ApplyInputPickupSequence(_dieMap);
                int orderedCount = CountSequencedTargets(_dieMap);

                WriteLog("InputStageDieMappingSequence",
                    "Die map coordinate mapping calculated. grid=" + gridX + "x" + gridY +
                    ", sourceMap=" + (sourceMap != null ? sourceMap.FrameObjId : "none") +
                    ", pitchX=" + pitchX.ToString("F6") +
                    ", pitchY=" + pitchY.ToString("F6") +
                    ", centerX=" + centerX.ToString("F6") +
                    ", centerY=" + centerY.ToString("F6") +
                    ", radius=" + waferRadius.ToString("F6") +
                    ", outerDiameter=" + (_frameSpec != null ? _frameSpec.OuterDiameterMm.ToString("F6") : "0") +
                    ", targetCount=" + targetCount +
                    ", orderedCount=" + orderedCount + " - Ok");

                CurrentStep = InputStageDieMappingStep.ApplyDieMap;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-CALC-EX", "InputStageDieMappingSequence", "Die map calculation failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int ApplyDieMap()
        {
            try
            {
                if (_dieMap == null || _waferMap == null)
                    return Fail("IN-STAGE-DIEMAP-APPLY", "InputStageDieMappingSequence", "Die map result is not available.");

                ApplyInputPickupSequence(_dieMap);
                Stage.ApplyDieMappingResult(_waferMap, _dieMap.OriginX, _dieMap.OriginY, _dieMap.PitchX, _dieMap.PitchY);
                LotStorage.ActiveInputDieMap = _dieMap;
                if (Context != null && Context.Controller != null)
                {
                    Context.Controller.PickupOptions = ResolveInputPickupSubset();
                    Context.Controller.ApplyInputDieMap(_dieMap, "InputStageDieMappingSequence.ApplyDieMap");
                }

                _createdDieCount = ApplyDieMaterials(_dieMap, _wafer);
                if (_wafer != null)
                {
                    _wafer.DieMapFrameObjId = _dieMap.FrameObjId;
                    _wafer.HasInputStageAlignResult = true;
                    _wafer.InputStageAlignOriginX = _dieMap.OriginX;
                    _wafer.InputStageAlignOriginY = _dieMap.OriginY;
                    _wafer.InputStageAlignPitchX = _dieMap.PitchX;
                    _wafer.InputStageAlignPitchY = _dieMap.PitchY;
                    _wafer.InputStageAlignOffsetX = Stage.WaferAlignOffsetX;
                    _wafer.InputStageAlignOffsetY = Stage.WaferAlignOffsetY;
                    _wafer.State = WaferMaterialState.Working;
                    _wafer.UpdatedAt = DateTime.Now;
                }

                MaterialStateService.NotifyAndSave("InputStageDieMapping");
                Context.Bus.Set("InputStageDieMapped");
                Context.Bus.Set("InputStageReady");
                WriteLog("InputStageDieMappingSequence",
                    "Input stage die mapping applied. wafer=" + (_wafer != null ? _wafer.WaferId : "") +
                    ", gridX=" + _dieMap.GridX +
                    ", gridY=" + _dieMap.GridY +
                    ", dieCount=" + _createdDieCount + " - Ok");

                CurrentStep = InputStageDieMappingStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-APPLY-EX", "InputStageDieMappingSequence", "Die map apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static DieMap ResolveSourceInputDieMap(WaferMaterial wafer)
        {
            try
            {
                DieMap activeMap = LotStorage.ActiveInputDieMap;
                if (IsUsableSourceMap(activeMap))
                    return ApplyInputPickupSequence(activeMap);

                DieMap materialMap = MaterialStateService.BuildDieMapFromWafer(wafer);
                if (IsUsableSourceMap(materialMap))
                    return ApplyInputPickupSequence(materialMap);

                DieMap recipeMap = LoadRecipeInputDieMap();
                if (IsUsableSourceMap(recipeMap))
                    return ApplyInputPickupSequence(recipeMap);

                return null;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Source input die map resolve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static DieMap LoadRecipeInputDieMap()
        {
            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project == null || string.IsNullOrWhiteSpace(project.InputDieMapFileName))
                    return null;

                string path = project.InputDieMapFileName;
                if (!System.IO.Path.IsPathRooted(path))
                    path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

                DieMap map = DieMapGenerator.Load(path);
                if (map != null)
                {
                    ApplyInputPickupSequence(map);
                    WriteLog("InputStageDieMappingSequence", "Recipe input die map loaded. path=" + path + " - Ok");
                }
                return map;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Recipe input die map load failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static bool IsUsableSourceMap(DieMap map)
        {
            try
            {
                return map != null &&
                       map.GridX > 0 &&
                       map.GridY > 0 &&
                       map.Entries != null &&
                       map.Entries.Count > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static DieMap ApplyInputPickupSequence(DieMap map)
        {
            try
            {
                if (map == null)
                    return null;

                PickupSequenceGenerator.ApplySequenceNumbers(map, ResolveInputPickupSubset());
                return DieMapGenerator.Normalize(map);
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Input pickup sequence apply failed: " + ex.Message + " - Failed");
                return map;
            }
            finally
            {
            }
        }

        private static PickupSubset ResolveInputPickupSubset()
        {
            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project == null)
                    return new PickupSubset();

                if (project.InputPickup != null)
                    return project.InputPickup;
                if (project.Pickup != null)
                    return project.Pickup;
                return new PickupSubset();
            }
            catch
            {
                return new PickupSubset();
            }
            finally
            {
            }
        }

        private static int CountSequencedTargets(DieMap map)
        {
            try
            {
                if (map == null || map.Entries == null)
                    return 0;

                int count = 0;
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry != null && entry.IsTarget && entry.SequenceNo > 0)
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

        private int ApplyDieMaterials(DieMap map, WaferMaterial wafer)
        {
            try
            {
                if (map == null || wafer == null)
                    return 0;

                if (wafer.DieIds == null)
                    wafer.DieIds = new List<string>();
                wafer.DieIds.Clear();

                int count = 0;
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null || !entry.IsTarget)
                        continue;

                    string dieId = string.IsNullOrWhiteSpace(entry.DieUid) ? BuildDieId(wafer, entry.GridY, entry.GridX) : entry.DieUid;
                    entry.DieUid = dieId;
                    DieMaterial die = MaterialStateService.GetOrCreateDieMaterial(dieId);
                    die.WaferID_Input = wafer.WaferId;
                    die.WaferID_Output = "";
                    die.Wafer_IndexX = entry.GridX;
                    die.Wafer_IndexY = entry.GridY;
                    die.Input_BinCode = entry.BinCode;
                    die.Output_BinCode = 0;
                    die.Bin_IndexX = -1;
                    die.Bin_IndexY = -1;
                    die.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage };
                    die.ReservedPickerLocation = MaterialLocationKind.Unknown;
                    die.ReservedPickerNo = -1;
                    die.Result = DieResult.Unknown;
                    if (die.NgCodes == null)
                        die.NgCodes = new List<string>();
                    else
                        die.NgCodes.Clear();
                    if (die.WaferOffset == null)
                        die.WaferOffset = new VisionOffset();
                    die.WaferOffset.X = entry.X;
                    die.WaferOffset.Y = entry.Y;
                    die.WaferOffset.R = 0.0;
                    die.WaferOffset.IsValid = true;
                    die.UpdatedAt = DateTime.Now;
                    wafer.DieIds.Add(dieId);
                    count++;
                }

                return count;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Die material apply failed: " + ex.Message + " - Failed");
                return 0;
            }
            finally
            {
            }
        }

        private async Task<VisionAlignResult> RequestVisionPcOffsetWithRetryAsync(string targetId, string stepName, CancellationToken ct)
        {
            try
            {
                int retryCount = Math.Max(3, ResolveDieMapRetryCount());
                int maxAttempts = retryCount + 1;
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    ct.ThrowIfCancellationRequested();
                    VisionAlignResult result = await RequestVisionPcOffsetOnceAsync(targetId, stepName, ct).ConfigureAwait(false);
                    if (result != null)
                        return result;

                    WriteLog("InputStageDieMappingSequence",
                        "Vision PC offset receive failed. step=" + stepName +
                        ", target=" + targetId +
                        ", attempt=" + attempt + "/" + maxAttempts + " - Retry");
                }

                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Vision offset retry exception. step=" + stepName + ": " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private async Task<VisionAlignResult> RequestVisionPcOffsetOnceAsync(string targetId, string stepName, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (IsSimulationOrDryRun())
                    return await RequestSimVisionOffsetAsync(targetId, stepName, ct).ConfigureAwait(false);

                if (Stage.Vision == null)
                    return null;

                Task<VisionAlignResult> alignTask = Stage.Vision.TriggerAlignAsync(targetId);
                if (alignTask == null)
                    return null;

                if (alignTask.IsCompleted)
                    return await alignTask.ConfigureAwait(false);

                Task cancelTask = Task.Delay(Timeout.Infinite, ct);
                Task completed = await Task.WhenAny(alignTask, cancelTask).ConfigureAwait(false);
                if (!ReferenceEquals(completed, alignTask))
                    ct.ThrowIfCancellationRequested();

                return await alignTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Vision offset request exception. step=" + stepName + ": " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private async Task<VisionAlignResult> RequestSimVisionOffsetAsync(string targetId, string stepName, CancellationToken ct)
        {
            try
            {
                await Task.Delay(120, ct).ConfigureAwait(false);
                bool ok;
                double dx;
                double dy;
                lock (SimVisionRandomLock)
                {
                    ok = SimVisionRandom.Next(0, 100) >= 20;
                    dx = (SimVisionRandom.NextDouble() - 0.5) * 0.02;
                    dy = (SimVisionRandom.NextDouble() - 0.5) * 0.02;
                }

                if (!ok)
                    return null;

                return new VisionAlignResult
                {
                    DeltaX = dx,
                    DeltaY = dy,
                    DeltaTheta = 0.0,
                    PitchX = ResolvePitchX(),
                    PitchY = ResolvePitchY()
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Simulation vision offset failed. target=" + targetId + ", step=" + stepName + ": " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private async Task<int> MoveAxisCommandAsync(WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                string guardReason;
                if (!VerifySharedRailAxisMove(axis, target, out guardReason))
                    return Fail("IN-STAGE-DIEMAP-SHARED-RAIL", Stage.Name,
                        description + " shared rail check failed. axis=" + axis + ", target=" + target + ". " + guardReason);

                int result = await AwaitStepWithCancellationAsync(Stage.MoveInputStageAxis(axis, target, Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-STAGE-DIEMAP-MOVE", Stage.Name,
                        description + " move command failed. axis=" + axis + ", target=" + target +
                        ", result=" + result + ". " + BuildAxisState(axis, target));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-MOVE-EX", Stage.Name, description + " move command exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitAxisInPositionResultAsync(WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                AxisMoveWaitResult waitResult = await AwaitStepWithCancellationAsync(
                    Stage.WaitInputStageAxisInPositionResult(axis, target, ResolveTimeout()),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                    return Fail(ResolveAxisMoveWaitAlarmCode("IN-STAGE-DIEMAP-MOVE", waitResult), Stage.Name,
                        description + " move/in-position wait failed. axis=" + axis + ", target=" + target +
                        ". " + FormatAxisMoveWaitResult(waitResult, BuildAxisState(axis, target)));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-WAIT-EX", Stage.Name, description + " move wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckStageAxisInPosition(WaferStageAxis axis, double target, string description)
        {
            try
            {
                QMC.Common.Motion.BaseAxis item = ResolveStageAxis(axis);
                if (item == null)
                    return Fail("IN-STAGE-DIEMAP-AXIS", Stage.Name,
                        description + " axis is not available. " + BuildAxisState(axis, target));

                if (item.IsMoving || item.IsAlarm || !IsAxisInPosition(item, target))
                    return Fail("IN-STAGE-DIEMAP-POSITION", Stage.Name,
                        description + " final position check failed. axis=" + axis + ", target=" + target +
                        ". " + BuildAxisState(axis, target));

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-POSITION-EX", Stage.Name, description + " final position check exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private bool VerifySharedRailAxisMove(WaferStageAxis axis, double target, out string reason)
        {
            reason = string.Empty;
            try
            {
                QMC.Common.Motion.BaseAxis item = ResolveStageAxis(axis);
                if (item == null)
                {
                    reason = "Axis is not available.";
                    return false;
                }

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(Context.Machine);
                if (service == null || !service.IsSharedRailAxis(item))
                    return true;

                return service.VerifySingleAxisMove(item, target, out reason);
            }
            catch (Exception ex)
            {
                reason = "SharedRailX check exception: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private QMC.Common.Motion.BaseAxis ResolveStageAxis(WaferStageAxis axis)
        {
            try
            {
                switch (axis)
                {
                    // 웨이퍼 Y축 반환
                    case WaferStageAxis.WaferY: return Stage.StageY;
                    // 웨이퍼 T축 반환
                    case WaferStageAxis.WaferT: return Stage.StageT;
                    // 웨이퍼 확장 Z축 반환
                    case WaferStageAxis.WaferExpandingZ: return Stage.ExpanderZ;
                    // 비전 X축 반환
                    case WaferStageAxis.VisionX: return Stage.CameraX;
                    // 니들 X축 반환
                    case WaferStageAxis.NeedleX: return Stage.NeedleBlockX;
                    // 니들 Z축 반환
                    case WaferStageAxis.NeedleZ: return Stage.NeedleZ;
                    // 이젝트 핀 Z축 반환
                    case WaferStageAxis.EjectPinZ: return Stage.EjectPinZ;
                    default: return null;
                }
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private string BuildDieMappingServoReason()
        {
            string reason = string.Empty;
            AppendServoOff(ref reason, WaferStageAxis.WaferY, Stage.StageY);
            AppendServoOff(ref reason, WaferStageAxis.VisionX, Stage.CameraX);
            return reason;
        }

        private void AppendServoOff(ref string reason, WaferStageAxis axis, QMC.Common.Motion.BaseAxis item)
        {
            if (item == null || item.IsServoOn)
                return;

            if (reason.Length > 0)
                reason += " ";
            reason += BuildAxisState(axis, item.ActualPosition) + ";";
        }

        private static bool IsAxisInPosition(QMC.Common.Motion.BaseAxis axis, double target)
        {
            try
            {
                if (axis == null)
                    return false;

                double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                    ? axis.Config.InPositionTolerance
                    : 0.05;
                return Math.Abs(axis.ActualPosition - target) <= tolerance;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private TapeFrameSpec ResolveFrameSpecForWafer(WaferMaterial wafer)
        {
            try
            {
                string specName = wafer != null ? wafer.TapeFrameSpecName : "";
                if (string.IsNullOrWhiteSpace(specName))
                {
                    specName = MaterialStateService.ResolveRecipeTapeFrameSpecName(0);
                    if (wafer != null && !string.IsNullOrWhiteSpace(specName))
                        wafer.TapeFrameSpecName = specName;
                }

                TapeFrameSpec spec = MaterialSpecs.FindFrame(specName);
                if (spec != null)
                    return spec;

                specName = MaterialStateService.ResolveRecipeTapeFrameSpecName(0);
                return MaterialSpecs.FindFrame(specName);
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Frame spec resolve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private bool TryGetMappedPoint(string name, out MappedMarkPoint point)
        {
            return _mappedPoints.TryGetValue(name, out point);
        }

        private string ResolveTargetId()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Options.DieMapVisionTargetId))
                    return Options.DieMapVisionTargetId.Trim();
                if (Stage != null && Stage.Recipe != null && Stage.Recipe.DieMap != null && !string.IsNullOrWhiteSpace(Stage.Recipe.DieMap.VisionTargetId))
                    return Stage.Recipe.DieMap.VisionTargetId.Trim();
            }
            catch
            {
            }
            finally
            {
            }

            return "DieMapMark";
        }

        private int ResolveDieMapRetryCount()
        {
            try
            {
                if (Options.DieMapVisionRetryCount > 0)
                    return Options.DieMapVisionRetryCount;
                if (Stage != null && Stage.Recipe != null && Stage.Recipe.DieMap != null && Stage.Recipe.DieMap.VisionRetryCount > 0)
                    return Stage.Recipe.DieMap.VisionRetryCount;
            }
            catch
            {
            }
            finally
            {
            }

            return 3;
        }

        private int ResolveTimeout()
        {
            return Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 10000;
        }

        private double ResolvePitchX()
        {
            try
            {
                if (_frameSpec != null && _frameSpec.PitchX > 0.0)
                    return _frameSpec.PitchX;
                if (Stage != null && Stage.PitchX > 0.0)
                    return Stage.PitchX;
            }
            catch
            {
            }
            finally
            {
            }

            return 1.0;
        }

        private double ResolvePitchY()
        {
            try
            {
                if (_frameSpec != null && _frameSpec.PitchY > 0.0)
                    return _frameSpec.PitchY;
                if (Stage != null && Stage.PitchY > 0.0)
                    return Stage.PitchY;
            }
            catch
            {
            }
            finally
            {
            }

            return 1.0;
        }

        private bool IsSimulationOrDryRun()
        {
            try
            {
                return Stage != null && Stage.IsInputStageSimulationOrDryRun();
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static double ResolveWaferRadiusFromSpecOrMarks(TapeFrameSpec spec, MappedMarkPoint left, MappedMarkPoint right, MappedMarkPoint top, MappedMarkPoint bottom)
        {
            try
            {
                if (spec != null && spec.OuterDiameterMm > 0.0)
                    return spec.OuterDiameterMm / 2.0;

                double radiusX = Math.Abs(right.X - left.X) / 2.0;
                double radiusY = Math.Abs(bottom.Y - top.Y) / 2.0;
                double radius = (radiusX + radiusY) / 2.0;
                return radius > 0.0 ? radius : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private static bool IsInsideWaferCircle(double x, double y, double centerX, double centerY, double radius)
        {
            try
            {
                if (radius <= 0.0)
                    return true;

                double dx = x - centerX;
                double dy = y - centerY;
                double distanceSquared = (dx * dx) + (dy * dy);
                double radiusWithTolerance = radius + 0.0001;
                return distanceSquared <= radiusWithTolerance * radiusWithTolerance;
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private static string BuildFrameObjId(WaferMaterial wafer)
        {
            string waferId = wafer != null && !string.IsNullOrWhiteSpace(wafer.WaferId) ? wafer.WaferId : "InputStage";
            return waferId + "-DIEMAP-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        private static string BuildDieId(WaferMaterial wafer, int row, int col)
        {
            string waferId = wafer != null && !string.IsNullOrWhiteSpace(wafer.WaferId) ? wafer.WaferId : "WAFER";
            return waferId + "-D" + row.ToString("000") + "-" + col.ToString("000");
        }

        private static async Task<int> AwaitStepWithCancellationAsync(Task<int> stepTask, CancellationToken ct)
        {
            try
            {
                if (stepTask == null)
                    return -1;

                if (stepTask.IsCompleted)
                    return await stepTask.ConfigureAwait(false);

                Task cancelTask = Task.Delay(Timeout.Infinite, ct);
                Task completed = await Task.WhenAny(stepTask, cancelTask).ConfigureAwait(false);
                if (!ReferenceEquals(completed, stepTask))
                    ct.ThrowIfCancellationRequested();

                return await stepTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return -1;
            }
            finally
            {
            }
        }

        private static async Task<AxisMoveWaitResult> AwaitStepWithCancellationAsync(Task<AxisMoveWaitResult> stepTask, CancellationToken ct)
        {
            try
            {
                if (stepTask == null)
                    return null;

                if (stepTask.IsCompleted)
                    return await stepTask.ConfigureAwait(false);

                Task cancelTask = Task.Delay(Timeout.Infinite, ct);
                Task completed = await Task.WhenAny(stepTask, cancelTask).ConfigureAwait(false);
                if (!ReferenceEquals(completed, stepTask))
                    ct.ThrowIfCancellationRequested();

                return await stepTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private sealed class MappedMarkPoint
        {
            public string Name { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double OffsetX { get; set; }
            public double OffsetY { get; set; }
        }
    }
}

