using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Recipes
{
    public sealed class RecipeMapBuildResult
    {
        public bool Success { get; internal set; }
        public string Message { get; internal set; } = "";
        public string BaseMapPath { get; internal set; } = "";
        public string InputBaseMapPath { get; internal set; } = "";
        public string OutputBaseMapPath { get; internal set; } = "";
        public string InputMapPath { get; internal set; } = "";
        public string GoodMapPath { get; internal set; } = "";
        public string NgMapPath { get; internal set; } = "";
        public DieMap BaseMap { get; internal set; }
        public DieMap RoleMap { get; internal set; }
        public int AddressCount { get; internal set; }
        public int TargetCount { get; internal set; }
    }

    /// <summary>
    /// Input/Output Base WaferMap을 독립적으로 관리하고 각 역할의 원본 X/Y 주소 도메인으로 파생 맵을 만든다.
    /// 역할별 Recipe Pitch는 Die 사이 Gap이고, DieMap Pitch는 장비 중심 간격(Die Size + Gap)이다.
    /// 각 역할은 독립 Base 주소 도메인, Gap, Target mask를 가지며 Die 크기는 RecipeProject.Die를 공통 사용한다.
    /// </summary>
    public static class RecipeMapBuildService
    {
        private sealed class PendingMap
        {
            public PendingMap(DieMap map, string jsonPath)
            {
                Map = map;
                JsonPath = jsonPath;
            }

            public DieMap Map { get; }
            public string JsonPath { get; }
        }

        private sealed class PendingFile
        {
            public string FinalPath { get; set; } = "";
            public string TempPath { get; set; } = "";
            public string BackupPath { get; set; } = "";
            public bool HadOriginal { get; set; }
            public bool Committed { get; set; }
        }

        private sealed class ProjectMapState
        {
            public TapeFrameSubset Frame { get; set; }
            public TapeFrameSubset InputFrame { get; set; }
            public TapeFrameSubset OutputFrame { get; set; }
            public string LegacyBasePath { get; set; } = "";
            public string InputBasePath { get; set; } = "";
            public string OutputBasePath { get; set; } = "";
            public string InputPath { get; set; } = "";
            public string OutputPath { get; set; } = "";
            public string GoodPath { get; set; } = "";
            public string NgPath { get; set; } = "";
            public int ApprovalVersion { get; set; }
            public string InputApprovalHash { get; set; } = "";
            public string GoodApprovalHash { get; set; } = "";
            public string NgApprovalHash { get; set; } = "";
            public double ChipThickness { get; set; }
        }

        public static RecipeMapBuildResult ImportBaseAndBuildRole(
            RecipeProject project,
            string sourcePath,
            bool outputRole,
            Func<RecipeProject, bool> persistProject = null)
        {
            var result = new RecipeMapBuildResult();
            ProjectMapState originalState = null;
            try
            {
                ValidateProject(project, true);
                originalState = CaptureProjectMapState(project);
                if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                    throw new FileNotFoundException("Base WaferMap 파일을 찾을 수 없습니다.", sourcePath);

                DieMap parsed = string.Equals(Path.GetExtension(sourcePath), ".txt", StringComparison.OrdinalIgnoreCase)
                    ? DieMapGenerator.LoadWaferMapTextOrThrow(sourcePath)
                    : DieMapGenerator.Load(sourcePath);
                if (!IsUsableMap(parsed))
                    throw new InvalidDataException("Base WaferMap 파싱 결과가 비어 있습니다: " + sourcePath);
                if (string.IsNullOrWhiteSpace(parsed.SourceFileName))
                    parsed.SourceFileName = Path.GetFileName(sourcePath);
                if (string.IsNullOrWhiteSpace(parsed.SourceFormat))
                    parsed.SourceFormat = ResolveSourceFormat(sourcePath);

                RecipeProjectConsistencyService.EnsureStructure(project);
                RecipeProjectConsistencyService.SynchronizeDieSpecification(project);

                RecipeMapKind baseKind = outputRole ? RecipeMapKind.GoodBin : RecipeMapKind.Input;
                DieMap baseMap = PrepareBaseMap(project, parsed);
                ApplyBaseDomainToFrame(project, baseMap, outputRole, false);

                string basePath = BuildMapPath(project, RecipeMapPaths.BaseFileSuffix(baseKind));
                var pendingMaps = new List<PendingMap> { new PendingMap(baseMap, basePath) };
                RecipeMapPaths.SetConfiguredBaseFileName(
                    project,
                    baseKind,
                    RecipeMapPaths.MakeConfigRelativePath(basePath));

                if (outputRole)
                {
                    DieMap goodMap = BuildDerivedMap(project, baseMap, RecipeMapKind.GoodBin, null);
                    DieMap ngMap = BuildDerivedMap(project, baseMap, RecipeMapKind.NgBin, null);
                    EnsureSameAddressDomain(baseMap, goodMap, "Good");
                    EnsureSameAddressDomain(baseMap, ngMap, "NG");

                    string goodPath = BuildMapPath(project, RecipeMapPaths.FileSuffix(RecipeMapKind.GoodBin));
                    string ngPath = BuildMapPath(project, RecipeMapPaths.FileSuffix(RecipeMapKind.NgBin));
                    RecipeMapPaths.SetConfiguredFileName(project, RecipeMapKind.GoodBin, RecipeMapPaths.MakeConfigRelativePath(goodPath));
                    RecipeMapPaths.SetConfiguredFileName(project, RecipeMapKind.NgBin, RecipeMapPaths.MakeConfigRelativePath(ngPath));
                    RecipeMapPaths.InvalidateApproval(project, false, true);
                    pendingMaps.Add(new PendingMap(goodMap, goodPath));
                    pendingMaps.Add(new PendingMap(ngMap, ngPath));
                    result.OutputBaseMapPath = basePath;
                    result.GoodMapPath = goodPath;
                    result.NgMapPath = ngPath;
                }
                else
                {
                    DieMap inputMap = BuildDerivedMap(project, baseMap, RecipeMapKind.Input, null);
                    EnsureSameAddressDomain(baseMap, inputMap, "Input");

                    string inputPath = BuildMapPath(project, RecipeMapPaths.FileSuffix(RecipeMapKind.Input));
                    RecipeMapPaths.SetConfiguredFileName(project, RecipeMapKind.Input, RecipeMapPaths.MakeConfigRelativePath(inputPath));
                    RecipeMapPaths.InvalidateApproval(project, true, false);
                    pendingMaps.Add(new PendingMap(inputMap, inputPath));
                    result.InputBaseMapPath = basePath;
                    result.InputMapPath = inputPath;
                }

                SaveMapFamiliesAtomically(pendingMaps, BuildProjectCommit(project, persistProject));
                TryCopyOriginalSourceSidecar(sourcePath, basePath);

                result.BaseMapPath = basePath;
                result.BaseMap = baseMap;
                result.AddressCount = baseMap.Entries.Count;
                result.TargetCount = baseMap.Entries.Count(e => e != null && e.IsTarget);
                result.Success = true;
                result.Message = outputRole
                    ? "Output Base와 Good/NG 맵 생성 완료. Bin Map Create에서 FINAL APPLY가 필요합니다."
                    : "Input Base와 Input 맵 생성 완료. Input Map Create에서 FINAL APPLY가 필요합니다.";
                return result;
            }
            catch (Exception ex)
            {
                RestoreProjectMapState(project, originalState);
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }
            finally
            {
            }
        }

        public static RecipeMapBuildResult RebuildDerivedMaps(
            RecipeProject project,
            bool rebuildInput,
            bool rebuildOutput,
            bool preserveOutputMasks,
            Func<RecipeProject, bool> persistProject = null)
        {
            var result = new RecipeMapBuildResult();
            ProjectMapState originalState = null;
            try
            {
                ValidateProject(project, true);
                originalState = CaptureProjectMapState(project);
                RecipeProjectConsistencyService.EnsureStructure(project);
                RecipeProjectConsistencyService.SynchronizeDieSpecification(project);
                if (!rebuildInput && !rebuildOutput)
                    throw new InvalidOperationException("재생성할 Input/Output 맵 역할이 선택되지 않았습니다.");

                var pendingMaps = new List<PendingMap>();
                int addressCount = 0;
                int targetCount = 0;

                if (rebuildInput)
                {
                    string inputBaseSourcePath = RecipeMapPaths.ResolveBaseConfigured(project, RecipeMapKind.Input);
                    DieMap inputBaseMap = LoadConfiguredMapWithSidecar(inputBaseSourcePath, "Input Base WaferMap");
                    inputBaseMap = PrepareBaseMap(project, inputBaseMap);
                    ApplyBaseDomainToFrame(project, inputBaseMap, false, true);

                    string inputBasePath = BuildMapPath(project, RecipeMapPaths.BaseFileSuffix(RecipeMapKind.Input));
                    DieMap existingInput = LoadDirectConfiguredMap(project, RecipeMapKind.Input);
                    DieMap inputMap = BuildDerivedMap(project, inputBaseMap, RecipeMapKind.Input, existingInput);
                    EnsureSameAddressDomain(inputBaseMap, inputMap, "Input");
                    result.InputBaseMapPath = inputBasePath;
                    result.InputMapPath = BuildMapPath(project, RecipeMapPaths.FileSuffix(RecipeMapKind.Input));
                    pendingMaps.Add(new PendingMap(inputBaseMap, inputBasePath));
                    pendingMaps.Add(new PendingMap(inputMap, result.InputMapPath));
                    RecipeMapPaths.SetConfiguredBaseFileName(
                        project,
                        RecipeMapKind.Input,
                        RecipeMapPaths.MakeConfigRelativePath(inputBasePath));
                    RecipeMapPaths.SetConfiguredFileName(
                        project,
                        RecipeMapKind.Input,
                        RecipeMapPaths.MakeConfigRelativePath(result.InputMapPath));
                    result.BaseMapPath = inputBasePath;
                    result.BaseMap = inputBaseMap;
                    addressCount += inputBaseMap.Entries.Count;
                    targetCount += inputBaseMap.Entries.Count(entry => entry != null && entry.IsTarget);
                }

                if (rebuildOutput)
                {
                    string outputBaseSourcePath = RecipeMapPaths.ResolveBaseConfigured(project, RecipeMapKind.GoodBin);
                    DieMap outputBaseMap = LoadConfiguredMapWithSidecar(outputBaseSourcePath, "Output Base WaferMap");
                    outputBaseMap = PrepareBaseMap(project, outputBaseMap);
                    ApplyBaseDomainToFrame(project, outputBaseMap, true, true);

                    string outputBasePath = BuildMapPath(project, RecipeMapPaths.BaseFileSuffix(RecipeMapKind.GoodBin));
                    DieMap existingGood = preserveOutputMasks ? LoadDirectConfiguredMap(project, RecipeMapKind.GoodBin) : null;
                    DieMap existingNg = preserveOutputMasks ? LoadDirectConfiguredMap(project, RecipeMapKind.NgBin) : null;
                    DieMap goodMap = BuildDerivedMap(project, outputBaseMap, RecipeMapKind.GoodBin, existingGood);
                    DieMap ngMap = BuildDerivedMap(project, outputBaseMap, RecipeMapKind.NgBin, existingNg);
                    EnsureSameAddressDomain(outputBaseMap, goodMap, "Good");
                    EnsureSameAddressDomain(outputBaseMap, ngMap, "NG");
                    result.OutputBaseMapPath = outputBasePath;
                    result.GoodMapPath = BuildMapPath(project, RecipeMapPaths.FileSuffix(RecipeMapKind.GoodBin));
                    result.NgMapPath = BuildMapPath(project, RecipeMapPaths.FileSuffix(RecipeMapKind.NgBin));
                    pendingMaps.Add(new PendingMap(outputBaseMap, outputBasePath));
                    pendingMaps.Add(new PendingMap(goodMap, result.GoodMapPath));
                    pendingMaps.Add(new PendingMap(ngMap, result.NgMapPath));
                    RecipeMapPaths.SetConfiguredBaseFileName(
                        project,
                        RecipeMapKind.GoodBin,
                        RecipeMapPaths.MakeConfigRelativePath(outputBasePath));
                    RecipeMapPaths.SetConfiguredFileName(
                        project,
                        RecipeMapKind.GoodBin,
                        RecipeMapPaths.MakeConfigRelativePath(result.GoodMapPath));
                    RecipeMapPaths.SetConfiguredFileName(
                        project,
                        RecipeMapKind.NgBin,
                        RecipeMapPaths.MakeConfigRelativePath(result.NgMapPath));
                    result.BaseMapPath = outputBasePath;
                    result.BaseMap = outputBaseMap;
                    addressCount += outputBaseMap.Entries.Count;
                    targetCount += outputBaseMap.Entries.Count(entry => entry != null && entry.IsTarget);
                }

                RecipeMapPaths.InvalidateApproval(project, rebuildInput, rebuildOutput);
                SaveMapFamiliesAtomically(pendingMaps, BuildProjectCommit(project, persistProject));

                result.AddressCount = addressCount;
                result.TargetCount = targetCount;
                result.Success = true;
                result.Message = "역할별 Base 기준 Recipe 파생 맵 재생성 완료. 변경된 역할은 Map Create FINAL APPLY가 필요합니다.";
                return result;
            }
            catch (Exception ex)
            {
                RestoreProjectMapState(project, originalState);
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Saves only the Target/Skip mask of an already configured role map.
        /// 선택 역할의 Base 주소 도메인만 검증하며 다른 역할의 Base/파생 맵은 변경하지 않는다.
        /// </summary>
        public static RecipeMapBuildResult SaveRoleTargetMask(
            RecipeProject project,
            RecipeMapKind kind,
            DieMap editedRoleMap,
            Func<RecipeProject, bool> persistProject = null)
        {
            var result = new RecipeMapBuildResult();
            ProjectMapState originalState = null;
            try
            {
                ValidateProject(project);
                originalState = CaptureProjectMapState(project);
                if (!IsUsableMap(editedRoleMap))
                    throw new InvalidDataException("저장할 Target/Skip Mask가 없습니다.");
                if (kind != RecipeMapKind.Input && kind != RecipeMapKind.GoodBin && kind != RecipeMapKind.NgBin)
                    throw new ArgumentOutOfRangeException(nameof(kind));

                string basePath = RecipeMapPaths.ResolveBaseConfigured(project, kind);
                string baseRoleName = kind == RecipeMapKind.Input ? "Input" : "Output";
                DieMap baseMap = LoadConfiguredMapWithSidecar(basePath, baseRoleName + " Base WaferMap");
                List<DieMapEntry> baseEntries = BuildUniqueAddressEntries(baseMap);
                if (baseEntries.Count == 0)
                    throw new InvalidDataException(baseRoleName + " Base WaferMap의 OriginalMap 주소가 비어 있습니다.");

                string configured = RecipeMapPaths.ExactConfiguredFileName(project, kind);
                string rolePath = RecipeMapPaths.ResolveConfiguredPath(configured);
                if (string.IsNullOrWhiteSpace(rolePath))
                    throw new InvalidDataException(kind + " DieMap 경로가 설정되지 않았습니다. Recipe → 웨이퍼 사양에서 해당 역할 Base WaferMap을 다시 연결하세요.");
                if (!string.Equals(Path.GetExtension(rolePath), ".json", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(kind + " DieMap이 Project 소유 JSON 형식이 아닙니다. Recipe → 웨이퍼 사양에서 해당 역할 Base WaferMap을 다시 연결하세요.");

                DieMap roleMap = LoadConfiguredMapWithSidecar(rolePath, kind + " DieMap");
                EnsureSameAddressDomain(baseMap, roleMap, kind.ToString());
                string mismatch;
                TapeFrameSubset roleFrame = RecipeDieMapResolver.ResolveFrame(project, kind);
                if (!RecipeDieMapResolver.IsCompatibleWithFrame(roleMap, roleFrame, out mismatch))
                    throw new InvalidDataException(kind + " DieMap과 Project Frame 설정이 일치하지 않습니다: " + mismatch);

                EnsureSameAddressDomain(baseMap, editedRoleMap, "편집 Mask");
                Dictionary<string, DieMapEntry> editedByAddress = BuildCompatibleMask(baseEntries, editedRoleMap);
                DieMap savedRoleMap = CloneMap(roleMap);
                foreach (DieMapEntry entry in savedRoleMap.Entries.Where(item => item != null))
                {
                    string address = BuildAddressKey(ResolveOriginalX(entry), ResolveOriginalY(entry));
                    DieMapEntry edited = editedByAddress[address];
                    entry.IsTarget = edited.IsTarget;
                    entry.Result = edited.IsTarget ? DieResult.Unknown : DieResult.NG;
                    entry.BinCode = edited.IsTarget ? (entry.BinCode > 0 ? entry.BinCode : 1) : 0;
                }

                PickupSubset pickup = kind == RecipeMapKind.Input
                    ? (project.InputPickup ?? project.Pickup ?? new PickupSubset())
                    : (project.OutputPickup ?? project.Pickup ?? new PickupSubset());
                PickupSequenceGenerator.ApplySequenceNumbers(savedRoleMap, pickup);
                RecipeMapPaths.ApproveMap(project, kind, savedRoleMap);
                SaveMapFamiliesAtomically(
                    new[] { new PendingMap(savedRoleMap, rolePath) },
                    BuildProjectCommit(project, persistProject));

                result.BaseMapPath = basePath;
                if (kind == RecipeMapKind.Input)
                    result.InputBaseMapPath = basePath;
                else
                    result.OutputBaseMapPath = basePath;
                result.InputMapPath = RecipeMapPaths.ResolveConfiguredPath(
                    RecipeMapPaths.ExactConfiguredFileName(project, RecipeMapKind.Input));
                result.GoodMapPath = RecipeMapPaths.ResolveConfiguredPath(
                    RecipeMapPaths.ExactConfiguredFileName(project, RecipeMapKind.GoodBin));
                result.NgMapPath = RecipeMapPaths.ResolveConfiguredPath(
                    RecipeMapPaths.ExactConfiguredFileName(project, RecipeMapKind.NgBin));
                result.BaseMap = baseMap;
                result.RoleMap = savedRoleMap;
                result.AddressCount = savedRoleMap.Entries.Count;
                result.TargetCount = savedRoleMap.Entries.Count(entry => entry != null && entry.IsTarget);
                result.Success = true;
                result.Message = kind + " Target/Skip Mask 및 FINAL APPLY 승인 저장 완료.";
                return result;
            }
            catch (Exception ex)
            {
                RestoreProjectMapState(project, originalState);
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }
        }

        public static DieMap BuildDerivedMap(
            RecipeProject project,
            DieMap baseMap,
            RecipeMapKind kind,
            DieMap existingRoleMask)
        {
            try
            {
                ValidateProject(project, true);
                if (!IsUsableMap(baseMap))
                    throw new InvalidDataException("Base WaferMap이 비어 있습니다.");

                TapeFrameSubset frame = RecipeDieMapResolver.ResolveFrame(project, kind);
                if (frame == null)
                    throw new InvalidDataException("Recipe Frame 설정이 없습니다. kind=" + kind);

                double dieSizeX = ResolvePositive(project.Die.WidthMm, baseMap.DieSizeX, 1.0);
                double dieSizeY = ResolvePositive(project.Die.HeightMm, baseMap.DieSizeY, 1.0);
                bool basePitchIsAuthoritative = baseMap.SourcePitchFromFile ||
                    (!string.IsNullOrWhiteSpace(baseMap.SourceFormat) &&
                     baseMap.SourceFormat.IndexOf("RAD", StringComparison.OrdinalIgnoreCase) >= 0);
                DieMap legacyPitchEvidence = existingRoleMask ?? TryLoadLegacyPitchEvidence(project, kind);
                double pitchGapX = ResolveRolePitchGap(
                    frame.PitchX,
                    baseMap.PitchX,
                    dieSizeX,
                    legacyPitchEvidence != null ? legacyPitchEvidence.PitchX : double.NaN,
                    basePitchIsAuthoritative,
                    kind,
                    "X");
                double pitchGapY = ResolveRolePitchGap(
                    frame.PitchY,
                    baseMap.PitchY,
                    dieSizeY,
                    legacyPitchEvidence != null ? legacyPitchEvidence.PitchY : double.NaN,
                    basePitchIsAuthoritative,
                    kind,
                    "Y");
                ApplyResolvedRolePitchGap(project, kind, frame, pitchGapX, pitchGapY);
                double pitchX = dieSizeX + pitchGapX;
                double pitchY = dieSizeY + pitchGapY;

                List<DieMapEntry> baseEntries = BuildUniqueAddressEntries(baseMap);
                if (baseEntries.Count == 0)
                    throw new InvalidDataException("Base WaferMap에 유효한 원본 주소가 없습니다.");

                int minX = baseEntries.Min(ResolveOriginalX);
                int maxX = baseEntries.Max(ResolveOriginalX);
                int minY = baseEntries.Min(ResolveOriginalY);
                int maxY = baseEntries.Max(ResolveOriginalY);
                if (minX < 0 || minY < 0)
                    throw new InvalidDataException("음수 WaferMap 원본 주소는 현재 공정 주소 도메인에서 지원하지 않습니다.");

                int sourceGridX = Math.Max(1, maxX - minX + 1);
                int sourceGridY = Math.Max(1, maxY - minY + 1);
                int gridX = Math.Max(sourceGridX, frame.DieMapX);
                int gridY = Math.Max(sourceGridY, frame.DieMapY);
                int localOffsetX = Math.Max(0, (gridX - sourceGridX) / 2);
                int localOffsetY = Math.Max(0, (gridY - sourceGridY) / 2);
                double centerGridX = Math.Max(0, gridX - 1) / 2.0;
                Dictionary<string, DieMapEntry> maskByAddress = BuildCompatibleMask(baseEntries, existingRoleMask);

                var map = new DieMap
                {
                    FrameObjId = RecipeMapPaths.BuildMapId(project, kind),
                    DieMapX = gridX,
                    DieMapY = gridY,
                    PitchX = pitchX,
                    PitchY = pitchY,
                    DieSizeX = dieSizeX,
                    DieSizeY = dieSizeY,
                    OuterDiameterMm = ResolvePositive(frame.OuterDiameterMm, baseMap.OuterDiameterMm, 1.0),
                    EdgeSkipMode = "ExternalMap",
                    SideEdgeSkip = baseMap.SideEdgeSkip,
                    TopBottomEdgeSkip = baseMap.TopBottomEdgeSkip,
                    OriginX = -centerGridX * pitchX,
                    OriginY = DieMapGenerator.CalculateCenteredOriginY(gridY, pitchY),
                    SourceFileName = baseMap.SourceFileName,
                    SourceFormat = baseMap.SourceFormat,
                    SourcePitchFromFile = baseMap.SourcePitchFromFile,
                    SourceDeclaredCount = baseMap.SourceDeclaredCount,
                    SourceFirstX = baseMap.SourceFirstX,
                    SourceFirstY = baseMap.SourceFirstY,
                    SourceFirstPosX = baseMap.SourceFirstPosX,
                    SourceFirstPosY = baseMap.SourceFirstPosY,
                    CreatedAt = DateTime.Now
                };

                int index = 0;
                foreach (DieMapEntry source in baseEntries.OrderByDescending(ResolveOriginalY).ThenBy(ResolveOriginalX))
                {
                    int originalX = ResolveOriginalX(source);
                    int originalY = ResolveOriginalY(source);
                    DieMapEntry mask;
                    bool hasMask = maskByAddress.TryGetValue(BuildAddressKey(originalX, originalY), out mask);
                    bool isTarget = hasMask ? mask.IsTarget : source.IsTarget;
                    int binCode = hasMask ? mask.BinCode : source.BinCode;
                    int localX = localOffsetX + originalX - minX;
                    int localY = localOffsetY + maxY - originalY;
                    double equipmentGridX = localX - centerGridX;
                    double equipmentGridY = DieMapGenerator.CalculateEquipmentGridY(localY, gridY);

                    map.Entries.Add(new DieMapEntry
                    {
                        Index = index++,
                        SequenceNo = hasMask && isTarget ? mask.SequenceNo : 0,
                        DieMapX = localX,
                        DieMapY = localY,
                        OriginalMapX = originalX,
                        OriginalMapY = originalY,
                        IsTarget = isTarget,
                        Result = isTarget ? DieResult.Unknown : DieResult.NG,
                        BinCode = isTarget ? (binCode > 0 ? binCode : 1) : 0,
                        EquipmentGridX = equipmentGridX,
                        EquipmentGridY = equipmentGridY,
                        PosX = equipmentGridX * pitchX,
                        PosY = equipmentGridY * pitchY,
                        DieUid = BuildRoleDieUid(project, kind, originalX, originalY)
                    });
                }

                PickupSubset pickup = kind == RecipeMapKind.Input
                    ? (project.InputPickup ?? project.Pickup ?? new PickupSubset())
                    : (project.OutputPickup ?? project.Pickup ?? new PickupSubset());
                PickupSequenceGenerator.ApplySequenceNumbers(map, pickup);
                return DieMapGenerator.Normalize(map);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static DieMap PrepareBaseMap(RecipeProject project, DieMap source)
        {
            List<DieMapEntry> entries = BuildUniqueAddressEntries(source);
            if (entries.Count == 0)
                throw new InvalidDataException("Base WaferMap 원본 주소가 없습니다.");

            int minX = entries.Min(ResolveOriginalX);
            int maxX = entries.Max(ResolveOriginalX);
            int minY = entries.Min(ResolveOriginalY);
            int maxY = entries.Max(ResolveOriginalY);
            if (minX < 0 || minY < 0)
                throw new InvalidDataException("음수 WaferMap 원본 주소는 현재 공정 주소 도메인에서 지원하지 않습니다.");

            double pitchX = ResolvePositive(source.PitchX, 1.0, 1.0);
            double pitchY = ResolvePositive(source.PitchY, 1.0, 1.0);
            int gridX = Math.Max(1, maxX - minX + 1);
            int gridY = Math.Max(1, maxY - minY + 1);
            double centerGridX = Math.Max(0, gridX - 1) / 2.0;
            var map = new DieMap
            {
                FrameObjId = RecipeMapPaths.SanitizeFileName(project.FileName) + "-BASE-WAFER-MAP",
                DieMapX = gridX,
                DieMapY = gridY,
                PitchX = pitchX,
                PitchY = pitchY,
                DieSizeX = ResolvePositive(project.Die.WidthMm, source.DieSizeX, pitchX),
                DieSizeY = ResolvePositive(project.Die.HeightMm, source.DieSizeY, pitchY),
                OuterDiameterMm = ResolvePositive(source.OuterDiameterMm, 1.0, 1.0),
                EdgeSkipMode = "ExternalMap",
                SideEdgeSkip = source.SideEdgeSkip,
                TopBottomEdgeSkip = source.TopBottomEdgeSkip,
                OriginX = -centerGridX * pitchX,
                OriginY = DieMapGenerator.CalculateCenteredOriginY(gridY, pitchY),
                SourceFileName = source.SourceFileName,
                SourceFormat = source.SourceFormat,
                SourcePitchFromFile = source.SourcePitchFromFile,
                SourceDeclaredCount = source.SourceDeclaredCount,
                SourceFirstX = source.SourceFirstX,
                SourceFirstY = source.SourceFirstY,
                SourceFirstPosX = source.SourceFirstPosX,
                SourceFirstPosY = source.SourceFirstPosY,
                CreatedAt = DateTime.Now
            };

            int index = 0;
            foreach (DieMapEntry entry in entries.OrderByDescending(ResolveOriginalY).ThenBy(ResolveOriginalX))
            {
                int originalX = ResolveOriginalX(entry);
                int originalY = ResolveOriginalY(entry);
                int localX = originalX - minX;
                int localY = maxY - originalY;
                double equipmentGridX = localX - centerGridX;
                double equipmentGridY = DieMapGenerator.CalculateEquipmentGridY(localY, gridY);
                map.Entries.Add(new DieMapEntry
                {
                    Index = index++,
                    SequenceNo = entry.IsTarget ? entry.SequenceNo : 0,
                    DieMapX = localX,
                    DieMapY = localY,
                    OriginalMapX = originalX,
                    OriginalMapY = originalY,
                    IsTarget = entry.IsTarget,
                    Result = entry.IsTarget ? DieResult.Unknown : DieResult.NG,
                    BinCode = entry.IsTarget ? (entry.BinCode > 0 ? entry.BinCode : 1) : 0,
                    EquipmentGridX = equipmentGridX,
                    EquipmentGridY = equipmentGridY,
                    PosX = equipmentGridX * pitchX,
                    PosY = equipmentGridY * pitchY,
                    DieUid = BuildBaseDieUid(project, originalX, originalY)
                });
            }

            return DieMapGenerator.Normalize(map);
        }

        private static void ApplyBaseDomainToFrame(
            RecipeProject project,
            DieMap baseMap,
            bool outputRole,
            bool preserveConfiguredGrid)
        {
            RecipeProjectConsistencyService.EnsureStructure(project);
            TapeFrameSubset frame = outputRole ? project.OutputFrame : project.InputFrame;
            if (frame == null)
                throw new InvalidDataException((outputRole ? "Output" : "Input") + " Frame 설정이 없습니다.");

            frame.DieMapX = preserveConfiguredGrid
                ? Math.Max(Math.Max(1, frame.DieMapX), Math.Max(1, baseMap.DieMapX))
                : Math.Max(1, baseMap.DieMapX);
            frame.DieMapY = preserveConfiguredGrid
                ? Math.Max(Math.Max(1, frame.DieMapY), Math.Max(1, baseMap.DieMapY))
                : Math.Max(1, baseMap.DieMapY);
            frame.DieSizeX = project.Die.WidthMm;
            frame.DieSizeY = project.Die.HeightMm;
            frame.EdgeSkipMode = "ExternalMap";
            frame.SideEdgeSkip = 0;
            frame.TopBottomEdgeSkip = 0;
            frame.SideEdgeSkipMm = 0.0;
            frame.TopBottomEdgeSkipMm = 0.0;
            if (frame.OuterDiameterMm <= 0.0)
                frame.OuterDiameterMm = baseMap.OuterDiameterMm;

            // Frame은 구형 Input Frame 호환 필드다. Output Base 적용 시에는 변경하지 않는다.
            if (!outputRole)
                project.Frame = RecipeProjectConsistencyService.CloneFrame(frame);
        }

        private static Action BuildProjectCommit(
            RecipeProject project,
            Func<RecipeProject, bool> persistProject)
        {
            if (persistProject == null)
                return null;

            return () =>
            {
                if (!persistProject(project))
                    throw new IOException("DieMap과 함께 Project 파일을 저장하지 못했습니다.");
            };
        }

        private static void SaveMapFamiliesAtomically(IEnumerable<PendingMap> maps, Action commitProject)
        {
            var pendingMaps = (maps ?? Enumerable.Empty<PendingMap>())
                .Where(item => item != null && item.Map != null && !string.IsNullOrWhiteSpace(item.JsonPath))
                .ToList();
            if (pendingMaps.Count == 0)
                throw new InvalidDataException("저장할 Recipe DieMap이 없습니다.");

            string transactionId = Guid.NewGuid().ToString("N");
            var pendingFiles = new List<PendingFile>();
            try
            {
                foreach (PendingMap pending in pendingMaps)
                {
                    string directory = Path.GetDirectoryName(pending.JsonPath);
                    if (string.IsNullOrWhiteSpace(directory))
                        throw new InvalidDataException("DieMap 저장 폴더를 확인할 수 없습니다: " + pending.JsonPath);

                    Directory.CreateDirectory(directory);
                    string csvPath = Path.ChangeExtension(pending.JsonPath, ".csv");
                    string tempJsonPath = pending.JsonPath + "." + transactionId + ".tmp.json";
                    string tempCsvPath = csvPath + "." + transactionId + ".tmp.csv";
                    pendingFiles.Add(new PendingFile { FinalPath = pending.JsonPath, TempPath = tempJsonPath });
                    pendingFiles.Add(new PendingFile { FinalPath = csvPath, TempPath = tempCsvPath });

                    DieMapGenerator.SaveJson(pending.Map, tempJsonPath);
                    VerifyPersistedMap(pending.Map, DieMapGenerator.LoadJson(tempJsonPath), tempJsonPath);
                    DieMapGenerator.SaveCsv(pending.Map, tempCsvPath);
                    VerifyPersistedMap(pending.Map, DieMapGenerator.LoadCsv(tempCsvPath), tempCsvPath);
                }

                foreach (PendingFile file in pendingFiles)
                {
                    file.HadOriginal = File.Exists(file.FinalPath);
                    file.BackupPath = file.FinalPath + "." + transactionId + ".bak";
                    if (file.HadOriginal)
                        File.Replace(file.TempPath, file.FinalPath, file.BackupPath, true);
                    else
                        File.Move(file.TempPath, file.FinalPath);
                    file.Committed = true;
                }

                foreach (PendingMap pending in pendingMaps)
                {
                    VerifyPersistedMap(pending.Map, DieMapGenerator.LoadJson(pending.JsonPath), pending.JsonPath);
                    string csvPath = Path.ChangeExtension(pending.JsonPath, ".csv");
                    VerifyPersistedMap(pending.Map, DieMapGenerator.LoadCsv(csvPath), csvPath);
                }

                if (commitProject != null)
                    commitProject();

                foreach (PendingFile file in pendingFiles)
                    DeleteIfExists(file.BackupPath);
            }
            catch
            {
                RollBackCommittedFiles(pendingFiles);
                throw;
            }
            finally
            {
                foreach (PendingFile file in pendingFiles)
                    DeleteIfExists(file.TempPath);
            }
        }

        private static void VerifyPersistedMap(DieMap expected, DieMap actual, string path)
        {
            if (!IsUsableMap(actual))
                throw new IOException("저장한 DieMap을 다시 읽을 수 없습니다: " + path);

            if (expected.Entries == null || actual.Entries == null || expected.Entries.Count != actual.Entries.Count)
                throw new InvalidDataException("저장 후 DieMap 엔트리 수가 달라졌습니다: " + path);

            if (expected.DieMapX != actual.DieMapX || expected.DieMapY != actual.DieMapY ||
                !NearlyEqual(expected.PitchX, actual.PitchX) || !NearlyEqual(expected.PitchY, actual.PitchY) ||
                !NearlyEqual(expected.DieSizeX, actual.DieSizeX) || !NearlyEqual(expected.DieSizeY, actual.DieSizeY) ||
                !NearlyEqual(expected.OuterDiameterMm, actual.OuterDiameterMm) ||
                !NearlyEqual(expected.OriginX, actual.OriginX) || !NearlyEqual(expected.OriginY, actual.OriginY) ||
                !NearlyEqual(expected.SideEdgeSkip, actual.SideEdgeSkip) ||
                !NearlyEqual(expected.TopBottomEdgeSkip, actual.TopBottomEdgeSkip) ||
                !string.Equals(expected.FrameObjId ?? "", actual.FrameObjId ?? "", StringComparison.Ordinal) ||
                !string.Equals(expected.EdgeSkipMode ?? "", actual.EdgeSkipMode ?? "", StringComparison.Ordinal) ||
                !string.Equals(expected.SourceFileName ?? "", actual.SourceFileName ?? "", StringComparison.Ordinal) ||
                !string.Equals(expected.SourceFormat ?? "", actual.SourceFormat ?? "", StringComparison.Ordinal) ||
                expected.SourcePitchFromFile != actual.SourcePitchFromFile ||
                expected.SourceDeclaredCount != actual.SourceDeclaredCount ||
                expected.SourceFirstX != actual.SourceFirstX || expected.SourceFirstY != actual.SourceFirstY ||
                !NearlyEqual(expected.SourceFirstPosX, actual.SourceFirstPosX) ||
                !NearlyEqual(expected.SourceFirstPosY, actual.SourceFirstPosY))
            {
                throw new InvalidDataException("저장 후 DieMap 메타데이터가 달라졌습니다: " + path);
            }

            Dictionary<string, DieMapEntry> expectedByAddress = BuildUniqueAddressEntries(expected).ToDictionary(
                entry => BuildAddressKey(ResolveOriginalX(entry), ResolveOriginalY(entry)),
                entry => entry,
                StringComparer.OrdinalIgnoreCase);
            Dictionary<string, DieMapEntry> actualByAddress = BuildUniqueAddressEntries(actual).ToDictionary(
                entry => BuildAddressKey(ResolveOriginalX(entry), ResolveOriginalY(entry)),
                entry => entry,
                StringComparer.OrdinalIgnoreCase);
            if (!new HashSet<string>(expectedByAddress.Keys, StringComparer.OrdinalIgnoreCase)
                .SetEquals(actualByAddress.Keys))
            {
                throw new InvalidDataException("저장 후 OriginalMap X/Y 주소 집합이 달라졌습니다: " + path);
            }

            foreach (KeyValuePair<string, DieMapEntry> pair in expectedByAddress)
            {
                DieMapEntry actualEntry = actualByAddress[pair.Key];
                if (pair.Value.IsTarget != actualEntry.IsTarget || pair.Value.BinCode != actualEntry.BinCode ||
                    pair.Value.SequenceNo != actualEntry.SequenceNo || pair.Value.Result != actualEntry.Result ||
                    pair.Value.DieMapX != actualEntry.DieMapX || pair.Value.DieMapY != actualEntry.DieMapY ||
                    !NearlyEqual(pair.Value.PosX, actualEntry.PosX) || !NearlyEqual(pair.Value.PosY, actualEntry.PosY) ||
                    !NearlyEqual(pair.Value.EquipmentGridX, actualEntry.EquipmentGridX) ||
                    !NearlyEqual(pair.Value.EquipmentGridY, actualEntry.EquipmentGridY) ||
                    !string.Equals(pair.Value.DieUid ?? "", actualEntry.DieUid ?? "", StringComparison.Ordinal))
                {
                    throw new InvalidDataException("저장 후 DieMap Mask/UID가 달라졌습니다. address=" + pair.Key + ", path=" + path);
                }
            }
        }

        private static void EnsureSameAddressDomain(DieMap baseMap, DieMap roleMap, string roleName)
        {
            HashSet<string> baseAddresses = BuildAddressSet(baseMap);
            HashSet<string> roleAddresses = BuildAddressSet(roleMap);
            if (!baseAddresses.SetEquals(roleAddresses))
            {
                throw new InvalidDataException(
                    roleName + " DieMap의 OriginalMap X/Y 주소 집합이 Base WaferMap과 다릅니다.");
            }
        }

        private static HashSet<string> BuildAddressSet(DieMap map)
        {
            return new HashSet<string>(
                BuildUniqueAddressEntries(map).Select(entry =>
                    BuildAddressKey(ResolveOriginalX(entry), ResolveOriginalY(entry))),
                StringComparer.OrdinalIgnoreCase);
        }

        private static void RollBackCommittedFiles(List<PendingFile> files)
        {
            foreach (PendingFile file in (files ?? new List<PendingFile>()).Where(item => item.Committed).Reverse())
            {
                try
                {
                    DeleteIfExists(file.FinalPath);
                    if (file.HadOriginal && File.Exists(file.BackupPath))
                        File.Move(file.BackupPath, file.FinalPath);
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", "RECIPE", "RecipeMapBuildService",
                        "DieMap transaction rollback failed: " + ex.Message + ", path=" + file.FinalPath + " - Failed");
                }
            }
        }

        private static void DeleteIfExists(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static bool NearlyEqual(double left, double right)
        {
            if (double.IsNaN(left) && double.IsNaN(right))
                return true;
            return Math.Abs(left - right) <= 0.000001;
        }

        private static ProjectMapState CaptureProjectMapState(RecipeProject project)
        {
            if (project == null)
                return null;

            return new ProjectMapState
            {
                Frame = RecipeProjectConsistencyService.CloneFrame(project.Frame),
                InputFrame = RecipeProjectConsistencyService.CloneFrame(project.InputFrame),
                OutputFrame = RecipeProjectConsistencyService.CloneFrame(project.OutputFrame),
                LegacyBasePath = project.BaseWaferMapFileName ?? "",
                InputBasePath = project.InputBaseWaferMapFileName ?? "",
                OutputBasePath = project.OutputBaseWaferMapFileName ?? "",
                InputPath = project.InputDieMapFileName ?? "",
                OutputPath = project.OutputDieMapFileName ?? "",
                GoodPath = project.GoodBinDieMapFileName ?? "",
                NgPath = project.NgBinDieMapFileName ?? "",
                ApprovalVersion = project.MapApprovalVersion,
                InputApprovalHash = project.InputMapApprovalHash ?? "",
                GoodApprovalHash = project.GoodBinMapApprovalHash ?? "",
                NgApprovalHash = project.NgBinMapApprovalHash ?? "",
                ChipThickness = project.ChipThickness
            };
        }

        private static void RestoreProjectMapState(RecipeProject project, ProjectMapState state)
        {
            if (project == null || state == null)
                return;

            project.Frame = RecipeProjectConsistencyService.CloneFrame(state.Frame);
            project.InputFrame = RecipeProjectConsistencyService.CloneFrame(state.InputFrame);
            project.OutputFrame = RecipeProjectConsistencyService.CloneFrame(state.OutputFrame);
            project.BaseWaferMapFileName = state.LegacyBasePath;
            project.InputBaseWaferMapFileName = state.InputBasePath;
            project.OutputBaseWaferMapFileName = state.OutputBasePath;
            project.InputDieMapFileName = state.InputPath;
            project.OutputDieMapFileName = state.OutputPath;
            project.GoodBinDieMapFileName = state.GoodPath;
            project.NgBinDieMapFileName = state.NgPath;
            project.MapApprovalVersion = state.ApprovalVersion;
            project.InputMapApprovalHash = state.InputApprovalHash;
            project.GoodBinMapApprovalHash = state.GoodApprovalHash;
            project.NgBinMapApprovalHash = state.NgApprovalHash;
            project.ChipThickness = state.ChipThickness;
        }

        private static void TryCopyOriginalSourceSidecar(string sourcePath, string baseJsonPath)
        {
            try
            {
                string extension = Path.GetExtension(sourcePath);
                if (string.IsNullOrWhiteSpace(extension))
                    return;

                string directory = Path.GetDirectoryName(baseJsonPath);
                string fileName = Path.GetFileNameWithoutExtension(baseJsonPath) + ".source" + extension.ToLowerInvariant();
                string targetPath = Path.Combine(directory ?? "", fileName);
                if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
                    return;

                File.Copy(sourcePath, targetPath, true);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "RECIPE", "RecipeMapBuildService",
                    "Base WaferMap source sidecar copy failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static DieMap LoadConfiguredMapWithSidecar(string configuredPath, string mapName)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                throw new InvalidDataException(mapName + " 경로가 설정되지 않았습니다. Recipe → 웨이퍼 사양에서 Base WaferMap을 연결하세요.");

            string loadPath = File.Exists(configuredPath) ? configuredPath : "";
            if (string.IsNullOrWhiteSpace(loadPath))
            {
                string csvSidecar = Path.ChangeExtension(configuredPath, ".csv");
                if (!string.IsNullOrWhiteSpace(csvSidecar) && File.Exists(csvSidecar))
                    loadPath = csvSidecar;
            }

            if (string.IsNullOrWhiteSpace(loadPath))
                throw new FileNotFoundException(mapName + " 파일을 찾을 수 없습니다.", configuredPath);

            DieMap map = DieMapGenerator.Load(loadPath);
            if (!IsUsableMap(map))
                throw new InvalidDataException(mapName + " 파일이 비어 있거나 읽을 수 없습니다: " + loadPath);
            return map;
        }

        private static DieMap CloneMap(DieMap source)
        {
            if (source == null)
                return null;

            return new DieMap
            {
                FrameObjId = source.FrameObjId ?? "",
                DieMapX = source.DieMapX,
                DieMapY = source.DieMapY,
                PitchX = source.PitchX,
                PitchY = source.PitchY,
                DieSizeX = source.DieSizeX,
                DieSizeY = source.DieSizeY,
                OuterDiameterMm = source.OuterDiameterMm,
                EdgeSkipMode = source.EdgeSkipMode ?? "",
                SideEdgeSkip = source.SideEdgeSkip,
                TopBottomEdgeSkip = source.TopBottomEdgeSkip,
                OriginX = source.OriginX,
                OriginY = source.OriginY,
                SourceFileName = source.SourceFileName ?? "",
                SourceFormat = source.SourceFormat ?? "",
                SourcePitchFromFile = source.SourcePitchFromFile,
                SourceDeclaredCount = source.SourceDeclaredCount,
                SourceFirstX = source.SourceFirstX,
                SourceFirstY = source.SourceFirstY,
                SourceFirstPosX = source.SourceFirstPosX,
                SourceFirstPosY = source.SourceFirstPosY,
                CreatedAt = source.CreatedAt,
                Entries = (source.Entries ?? new List<DieMapEntry>())
                    .Where(entry => entry != null)
                    .Select(entry => new DieMapEntry
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
                        DieUid = entry.DieUid ?? ""
                    })
                    .ToList()
            };
        }

        private static DieMap LoadDirectConfiguredMap(RecipeProject project, RecipeMapKind kind)
        {
            string path = RecipeMapPaths.ResolveConfiguredPath(
                RecipeMapPaths.ExactConfiguredFileName(project, kind));
            if (string.IsNullOrWhiteSpace(path))
                return null;

            string loadPath = File.Exists(path) ? path : "";
            if (string.IsNullOrWhiteSpace(loadPath))
            {
                string csvSidecar = Path.ChangeExtension(path, ".csv");
                if (!string.IsNullOrWhiteSpace(csvSidecar) && File.Exists(csvSidecar))
                    loadPath = csvSidecar;
            }

            if (string.IsNullOrWhiteSpace(loadPath))
                throw new FileNotFoundException("기존 역할 DieMap Mask 파일을 찾을 수 없습니다.", path);

            DieMap map = DieMapGenerator.Load(loadPath);
            if (!IsUsableMap(map))
                throw new InvalidDataException("기존 역할 DieMap을 읽을 수 없어 Mask 보존을 중단했습니다: " + loadPath);

            return map;
        }

        private static DieMap TryLoadLegacyPitchEvidence(RecipeProject project, RecipeMapKind kind)
        {
            try
            {
                return LoadDirectConfiguredMap(project, kind);
            }
            catch
            {
                return null;
            }
        }

        private static Dictionary<string, DieMapEntry> BuildCompatibleMask(
            List<DieMapEntry> baseEntries,
            DieMap existingRoleMask)
        {
            var empty = new Dictionary<string, DieMapEntry>(StringComparer.OrdinalIgnoreCase);
            if (!IsUsableMap(existingRoleMask))
                return empty;

            List<DieMapEntry> maskEntries = BuildUniqueAddressEntries(existingRoleMask);
            var baseKeys = new HashSet<string>(baseEntries.Select(e => BuildAddressKey(ResolveOriginalX(e), ResolveOriginalY(e))), StringComparer.OrdinalIgnoreCase);
            var maskKeys = new HashSet<string>(maskEntries.Select(e => BuildAddressKey(ResolveOriginalX(e), ResolveOriginalY(e))), StringComparer.OrdinalIgnoreCase);
            if (!baseKeys.SetEquals(maskKeys))
            {
                throw new InvalidDataException(
                    "기존 역할 DieMap Mask의 OriginalMap X/Y 주소 집합이 Base와 달라 보존할 수 없습니다. " +
                    "Base Wafer Map을 다시 Import해 명시적으로 초기화하세요.");
            }

            return maskEntries.ToDictionary(
                e => BuildAddressKey(ResolveOriginalX(e), ResolveOriginalY(e)),
                e => e,
                StringComparer.OrdinalIgnoreCase);
        }

        private static List<DieMapEntry> BuildUniqueAddressEntries(DieMap map)
        {
            if (map == null || map.Entries == null)
                return new List<DieMapEntry>();

            var groups = map.Entries
                .Where(e => e != null)
                .GroupBy(e => BuildAddressKey(ResolveOriginalX(e), ResolveOriginalY(e)), StringComparer.OrdinalIgnoreCase)
                .ToList();
            IGrouping<string, DieMapEntry> duplicate = groups.FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new InvalidDataException("WaferMap에 중복 OriginalMap X/Y 주소가 있습니다: " + duplicate.Key);

            return groups.Select(group => group.First()).ToList();
        }

        private static string BuildMapPath(RecipeProject project, string suffix)
        {
            string recipeName = RecipeMapPaths.SanitizeFileName(project.FileName);
            string fileName = recipeName + "_" + suffix + ".json";
            return RecipeMapPaths.ResolveConfiguredPath(RecipeMapPaths.BuildProjectMapRelativePath(recipeName, fileName));
        }

        private static int ResolveOriginalX(DieMapEntry entry)
        {
            return entry != null && entry.OriginalMapX >= 0 ? entry.OriginalMapX : (entry != null ? entry.DieMapX : -1);
        }

        private static int ResolveOriginalY(DieMapEntry entry)
        {
            return entry != null && entry.OriginalMapY >= 0 ? entry.OriginalMapY : (entry != null ? entry.DieMapY : -1);
        }

        private static string BuildAddressKey(int x, int y)
        {
            return x.ToString(CultureInfo.InvariantCulture) + "," + y.ToString(CultureInfo.InvariantCulture);
        }

        private static string BuildBaseDieUid(RecipeProject project, int x, int y)
        {
            return RecipeMapPaths.SanitizeFileName(project.FileName) + "-BASE-X" +
                   x.ToString("0000", CultureInfo.InvariantCulture) + "-Y" +
                   y.ToString("0000", CultureInfo.InvariantCulture);
        }

        private static string BuildRoleDieUid(RecipeProject project, RecipeMapKind kind, int x, int y)
        {
            return RecipeMapPaths.BuildMapId(project, kind) + "-X" +
                   x.ToString("0000", CultureInfo.InvariantCulture) + "-Y" +
                   y.ToString("0000", CultureInfo.InvariantCulture);
        }

        private static string ResolveSourceFormat(string path)
        {
            string extension = Path.GetExtension(path ?? "").ToLowerInvariant();
            if (extension == ".txt") return "RAD TXT";
            if (extension == ".csv") return "CSV";
            if (extension == ".json") return "JSON";
            return string.IsNullOrWhiteSpace(extension) ? "UNKNOWN" : extension.TrimStart('.').ToUpperInvariant();
        }

        private static bool IsUsableMap(DieMap map)
        {
            return map != null && map.Entries != null && map.Entries.Count > 0;
        }

        private static void ValidateProject(RecipeProject project, bool allowBasePitchRecovery = false)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(project.FileName))
                throw new InvalidDataException("Recipe 이름이 없습니다.");

            RecipeProjectConsistencyService.EnsureStructure(project);
            if (project.Die == null ||
                !IsFinitePositive(project.Die.WidthMm) ||
                !IsFinitePositive(project.Die.HeightMm) ||
                !IsFinitePositive(project.Die.ThicknessMm))
            {
                throw new InvalidDataException("Recipe → 다이 사양의 Width/Height/Thickness를 유효한 양수로 설정하세요.");
            }

            if (project.InputFrame == null || project.OutputFrame == null)
            {
                throw new InvalidDataException("Recipe → 웨이퍼 사양의 Input/Output Frame 설정이 없습니다.");
            }

            if (!allowBasePitchRecovery)
            {
                ValidateRolePitchForSave(project.InputFrame, "INPUT");
                ValidateRolePitchForSave(project.OutputFrame, "OUTPUT");
            }

            if (!IsFinitePositive(project.InputFrame.OuterDiameterMm) ||
                !IsFinitePositive(project.OutputFrame.OuterDiameterMm))
            {
                throw new InvalidDataException("Recipe → 웨이퍼 사양의 Input/Output Wafer Diameter를 유효한 양수로 설정하세요.");
            }
        }

        private static bool IsFinitePositive(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
        }

        private static bool IsPitchGapValid(double pitchGap)
        {
            return !double.IsNaN(pitchGap) && !double.IsInfinity(pitchGap) && pitchGap >= 0.0;
        }

        private static bool IsSourceCenterStepValid(double centerStep, double dieSize)
        {
            return IsFinitePositive(centerStep) && IsFinitePositive(dieSize) &&
                   centerStep + 0.000000001 >= dieSize;
        }

        private static void ValidateRolePitchForSave(
            TapeFrameSubset frame,
            string role)
        {
            if (frame != null &&
                IsPitchGapValid(frame.PitchX) &&
                IsPitchGapValid(frame.PitchY))
            {
                return;
            }

            throw new InvalidDataException(
                "Recipe → 웨이퍼 사양의 " + role +
                " Pitch Gap X/Y는 유한한 0 이상의 값이어야 합니다. " +
                "Gap=(" + FormatDouble(frame != null ? frame.PitchX : double.NaN) + "," +
                FormatDouble(frame != null ? frame.PitchY : double.NaN) + ") mm.");
        }

        private static double ResolveRolePitchGap(
            double configuredGap,
            double baseCenterStep,
            double dieSize,
            double existingRoleMapStep,
            bool basePitchIsAuthoritative,
            RecipeMapKind kind,
            string axis)
        {
            bool baseStepValid = basePitchIsAuthoritative &&
                                 IsSourceCenterStepValid(baseCenterStep, dieSize);
            double baseGap = baseStepValid ? Math.Max(0.0, baseCenterStep - dieSize) : 0.0;
            bool legacyMinimumClamp = baseStepValid &&
                                      NearlyEqual(configuredGap, 0.001) &&
                                      NearlyEqual(baseGap, 0.0) &&
                                      NearlyEqual(existingRoleMapStep, configuredGap) &&
                                      existingRoleMapStep + 0.000000001 < dieSize;
            if (IsPitchGapValid(configuredGap) && !legacyMinimumClamp)
                return configuredGap;
            if (baseStepValid)
                return baseGap;

            throw new InvalidDataException(
                kind + " Pitch Gap " + axis + "가 유효하지 않습니다. " +
                "설정 Gap=" + FormatDouble(configuredGap) + " mm, Die " + axis + "=" +
                FormatDouble(dieSize) + " mm, Base RAD 중심 간격=" + FormatDouble(baseCenterStep) +
                " mm. Recipe → 웨이퍼 사양에서 Gap을 0 이상으로 설정하세요.");
        }

        private static void ApplyResolvedRolePitchGap(
            RecipeProject project,
            RecipeMapKind kind,
            TapeFrameSubset frame,
            double pitchGapX,
            double pitchGapY)
        {
            frame.PitchX = pitchGapX;
            frame.PitchY = pitchGapY;
            if (kind == RecipeMapKind.Input && project != null && project.Frame != null)
            {
                project.Frame.PitchX = pitchGapX;
                project.Frame.PitchY = pitchGapY;
            }
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static double ResolvePositive(double first, double second, double fallback)
        {
            if (!double.IsNaN(first) && !double.IsInfinity(first) && first > 0.0)
                return first;
            if (!double.IsNaN(second) && !double.IsInfinity(second) && second > 0.0)
                return second;
            return fallback;
        }
    }
}
