using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Recipes
{
    /// <summary>레시피 DieMap 로드 기준을 한 곳으로 통일한다.</summary>
    public static class RecipeDieMapResolver
    {
        public static DieMap LoadCompatibleMap(RecipeProject project, RecipeMapKind kind, out string sourcePath, out string reason)
        {
            sourcePath = "";
            reason = "";
            try
            {
                TapeFrameSubset frame = ResolveFrame(project, kind);

                // 관리형 Recipe는 역할별 파일이 비어 있을 때 다른 역할 맵으로 fallback하면 안 된다.
                // Input/Good/NG가 같은 원본 주소 영역을 사용해도 Pitch/Mask/승인은 각각 독립이다.
                string configuredFileName = project != null && project.MapApprovalVersion > 0
                    ? RecipeMapPaths.ExactConfiguredFileName(project, kind)
                    : RecipeMapPaths.ConfiguredFileName(project, kind);
                string configuredPath = RecipeMapPaths.ResolveConfiguredPath(configuredFileName);
                if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
                {
                    DieMap configuredMap = DieMapGenerator.Load(configuredPath);
                    if (IsUsableMap(configuredMap))
                    {
                        if (!IsSupportedForEquipment(configuredMap, out reason))
                            return null;
                        string mismatch;
                        if (IsCompatibleWithFrame(configuredMap, frame, out mismatch))
                        {
                            configuredMap = DieMapGenerator.Normalize(configuredMap);
                            string approvalReason;
                            if (RecipeMapPaths.IsMapApproved(project, kind, configuredMap, out approvalReason))
                            {
                                sourcePath = configuredPath;
                                return configuredMap;
                            }

                            reason = "configured map approval blocked. path=" + configuredPath + ", " + approvalReason;
                        }
                        else
                        {
                            reason = "configured map mismatch. path=" + configuredPath + ", " + mismatch;
                        }
                    }
                    else
                    {
                        reason = "configured map is empty. path=" + configuredPath;
                    }

                    string sidecarCsvPath;
                    string sidecarCsvReason;
                    DieMap sidecarCsvMap = LoadCompatibleSidecarCsv(configuredPath, frame, out sidecarCsvPath, out sidecarCsvReason);
                    if (IsUsableMap(sidecarCsvMap))
                    {
                        if (!IsSupportedForEquipment(sidecarCsvMap, out reason))
                            return null;
                        sidecarCsvMap = DieMapGenerator.Normalize(sidecarCsvMap);
                        string approvalReason;
                        if (RecipeMapPaths.IsMapApproved(project, kind, sidecarCsvMap, out approvalReason))
                        {
                            sourcePath = sidecarCsvPath;
                            return sidecarCsvMap;
                        }

                        reason = AppendReason(reason, "configured sidecar approval blocked. path=" + sidecarCsvPath + ", " + approvalReason);
                    }

                    if (!string.IsNullOrWhiteSpace(sidecarCsvReason))
                        reason = AppendReason(reason, sidecarCsvReason);
                }
                else if (!string.IsNullOrWhiteSpace(configuredPath))
                {
                    reason = "configured map file not found. path=" + configuredPath;
                }

                // 새 관리형 Recipe는 Base/원본 map을 공정 역할 map으로 fallback하지 않는다.
                // Wafer/Die 변경 뒤 Map Create FINAL APPLY 전에는 명시적으로 차단해야 한다.
                if (project != null && project.MapApprovalVersion > 0)
                {
                    if (string.IsNullOrWhiteSpace(reason))
                        reason = kind + " configured role map is not FINAL APPLY approved.";
                    return null;
                }

                if (IsExternalFrame(frame))
                {
                    DieMap externalMap = LoadExternalSourceMap(project, frame, kind, out sourcePath);
                    if (IsUsableMap(externalMap))
                    {
                        if (!IsSupportedForEquipment(externalMap, out reason))
                            return null;
                        string mismatch;
                        if (IsCompatibleWithFrame(externalMap, frame, out mismatch))
                        {
                            if (!RecipeMapPaths.IsMapApproved(project, kind, externalMap, out reason))
                                return null;
                            return DieMapGenerator.Normalize(externalMap);
                        }

                        reason = AppendReason(reason, "external source map mismatch. path=" + sourcePath + ", " + mismatch);
                    }
                    else
                    {
                        reason = AppendReason(reason, "external source map not found. frame=" + (frame != null ? frame.FrameSpecName : ""));
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                reason = "recipe die map resolve failed: " + ex.Message;
                sourcePath = "";
                return null;
            }
        }

        private static DieMap LoadCompatibleSidecarCsv(string configuredPath, TapeFrameSubset frame, out string sourcePath, out string reason)
        {
            sourcePath = "";
            reason = "";

            try
            {
                if (string.IsNullOrWhiteSpace(configuredPath))
                    return null;

                if (string.Equals(Path.GetExtension(configuredPath), ".csv", StringComparison.OrdinalIgnoreCase))
                    return null;

                string csvPath = Path.ChangeExtension(configuredPath, ".csv");
                if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
                    return null;

                DieMap csvMap = DieMapGenerator.Load(csvPath);
                if (!IsUsableMap(csvMap))
                {
                    reason = "configured sidecar csv is empty. path=" + csvPath;
                    return null;
                }

                string mismatch;
                if (IsCompatibleWithFrame(csvMap, frame, out mismatch))
                {
                    sourcePath = csvPath;
                    return DieMapGenerator.Normalize(csvMap);
                }

                reason = "configured sidecar csv mismatch. path=" + csvPath + ", " + mismatch;
                return null;
            }
            catch (Exception ex)
            {
                reason = "configured sidecar csv load failed. path=" + configuredPath + ", error=" + ex.Message;
                return null;
            }
        }

        public static TapeFrameSubset ResolveFrame(RecipeProject project, RecipeMapKind kind)
        {
            if (project == null)
                return null;

            if (kind == RecipeMapKind.Input)
            {
                TapeFrameSubset inputFrame = project.InputFrame ?? project.Frame;
                if (inputFrame != null)
                    inputFrame.FrameSpecName = MaterialStateService.NormalizeInputTapeFrameSpecName(inputFrame.FrameSpecName);
                return inputFrame;
            }

            return project.OutputFrame ?? project.Frame;
        }

        public static bool IsExternalFrame(TapeFrameSubset frame)
        {
            return frame != null &&
                   !string.IsNullOrWhiteSpace(frame.EdgeSkipMode) &&
                   frame.EdgeSkipMode.IndexOf("External", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsExternalMap(DieMap map)
        {
            return map != null &&
                   !string.IsNullOrWhiteSpace(map.EdgeSkipMode) &&
                   map.EdgeSkipMode.IndexOf("External", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>생성 설정의 저장 가능 여부와 현재 실장비의 사용 가능 여부를 분리한다.</summary>
        public static bool IsSupportedForEquipment(DieMap map, out string reason)
        {
            reason = "";
            if (map == null || (map.Generation == null &&
                !string.Equals(map.SourceFormat, GeneratedWaferMapCodec.SourceFormat, StringComparison.Ordinal)))
                return true;

            // 생성 정의의 누락/변조 및 V2 외곽 초과는 승인 hash가 있더라도 장비에 사용할 수 없다.
            if (!GeneratedWaferMapCodec.Validate(map, out reason))
                return false;
            int angle = map.Generation.RotationDegrees;
            if (angle != 90 && angle != 270)
                return true;

            reason = "현재 장비에서는 " + angle + "° 회전 맵을 사용할 수 없습니다. " +
                "미리보기와 설정 저장만 가능하며 FINAL APPLY와 장비 사용은 차단됩니다. " +
                "맵 생성 미리보기에서 0° 또는 180°로 다시 생성하여 저장하세요.";
            return false;
        }

        public static bool IsCompatibleWithFrame(DieMap map, TapeFrameSubset frame, out string reason)
        {
            reason = "";
            try
            {
                if (map != null && (map.Generation != null ||
                    string.Equals(map.SourceFormat, GeneratedWaferMapCodec.SourceFormat, StringComparison.Ordinal)))
                    return IsGeneratedMapCompatibleWithFrame(map, frame, out reason);

                if (!IsUsableMap(map) || frame == null)
                    return true;

                int frameX = Math.Max(1, frame.DieMapX);
                int frameY = Math.Max(1, frame.DieMapY);
                int mapX = ResolveMapSizeX(map);
                int mapY = ResolveMapSizeY(map);
                double dieSizeX = frame.DieSizeX > 0.0
                    ? frame.DieSizeX
                    : (map.DieSizeX > 0.0 ? map.DieSizeX : 1.0);
                double dieSizeY = frame.DieSizeY > 0.0
                    ? frame.DieSizeY
                    : (map.DieSizeY > 0.0 ? map.DieSizeY : 1.0);
                double expectedStepX = DieMapGenerator.CalculateCenterStep(dieSizeX, frame.PitchX);
                double expectedStepY = DieMapGenerator.CalculateCenterStep(dieSizeY, frame.PitchY);

                bool sizeMismatch = mapX != frameX || mapY != frameY;
                bool pitchMismatch =
                    map.PitchX <= 0.0 || map.PitchY <= 0.0 ||
                    Math.Abs(map.PitchX - expectedStepX) > 1e-6 ||
                    Math.Abs(map.PitchY - expectedStepY) > 1e-6;

                if (sizeMismatch || pitchMismatch)
                {
                    // 현재 기준: ExternalMap은 원본 wafer map index 범위가 frame grid와 같아야 한다.
                    reason =
                        "frame=" + (frame.FrameSpecName ?? "") +
                        ", frameDie=" + frameX + "x" + frameY +
                        ", mapDie=" + mapX + "x" + mapY +
                        ", frameGap=(" + frame.PitchX.ToString("F6") + "," + frame.PitchY.ToString("F6") + ")" +
                        ", dieSize=(" + dieSizeX.ToString("F6") + "," + dieSizeY.ToString("F6") + ")" +
                        ", expectedStep=(" + expectedStepX.ToString("F6") + "," + expectedStepY.ToString("F6") + ")" +
                        ", mapStep=(" + map.PitchX.ToString("F6") + "," + map.PitchY.ToString("F6") + ")";
                    return false;
                }

                foreach (DieMapEntry entry in map.Entries.Where(item => item != null))
                {
                    double expectedGridY = DieMapGenerator.CalculateEquipmentGridY(entry.DieMapY, mapY);
                    double expectedPosY = expectedGridY * map.PitchY;
                    if (Math.Abs(entry.EquipmentGridY - expectedGridY) <= 0.000001 &&
                        Math.Abs(entry.PosY - expectedPosY) <= 0.000001)
                    {
                        continue;
                    }

                    reason = "맵 Y 장비좌표가 현재 엔코더 방향(위-/아래+)과 일치하지 않습니다. " +
                             "localY=" + entry.DieMapY +
                             ", gridY=" + entry.EquipmentGridY.ToString("F6") +
                             ", expectedGridY=" + expectedGridY.ToString("F6") +
                             ". Wafer Spec에서 맵을 다시 SAVE/APPLY 하세요.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "compatibility check failed: " + ex.Message;
                return false;
            }
        }

        public static string GetGeneratedRotationToken(int degrees)
        {
            switch (degrees)
            {
                case 0: return "None";
                case 90: return "CW90";
                case 180: return "Rotate180";
                case 270: return "CCW90";
                default: throw new ArgumentOutOfRangeException(nameof(degrees), "맵 회전 각도는 0°, 90°, 180°, 270°만 지원합니다.");
            }
        }

        private static bool IsGeneratedMapCompatibleWithFrame(DieMap map, TapeFrameSubset frame, out string reason)
        {
            // 설정 초안의 물리 사양 일치 여부다. 장비 사용은 IsSupportedForEquipment에서 별도로 차단한다.
            if (!GeneratedWaferMapCodec.ValidateForStorage(map, out reason))
                return false;
            if (frame == null)
            {
                reason = "생성 맵의 역할 웨이퍼 사양이 없습니다.";
                return false;
            }

            GeneratedWaferMapDefinition definition = map.Generation;
            bool matches = frame.DieMapX == map.DieMapX && frame.DieMapY == map.DieMapY &&
                Math.Abs(frame.OuterDiameterMm - (double)definition.OuterDiameterMm) <= 0.000001 &&
                Math.Abs(frame.DieSizeX - (double)definition.DieSizeXMm) <= 0.000001 &&
                Math.Abs(frame.DieSizeY - (double)definition.DieSizeYMm) <= 0.000001 &&
                Math.Abs(frame.PitchX - (double)definition.GapXMm) <= 0.000001 &&
                Math.Abs(frame.PitchY - (double)definition.GapYMm) <= 0.000001 &&
                string.Equals(string.IsNullOrWhiteSpace(frame.Rotate) ? "None" : frame.Rotate,
                    GetGeneratedRotationToken(definition.RotationDegrees), StringComparison.OrdinalIgnoreCase);
            if (!matches)
            {
                reason = "생성 맵의 Grid/직경/다이 크기/Gap/회전과 역할 웨이퍼 사양이 다릅니다. " +
                    "맵 생성 미리보기에서 다시 생성하고 저장하세요.";
                return false;
            }
            reason = "";
            return true;
        }

        /// <summary>
        /// Center-relative 승인 역할 맵과 Mapping 완료 절대좌표 맵이 같은 Grid/Pitch/원본 주소인지 확인한다.
        /// Runtime의 Result/Target 상태는 공정 중 바뀔 수 있으므로 좌표 domain만 비교한다.
        /// </summary>
        public static bool IsMappedInputCompatibleWithRecipe(DieMap mapped, DieMap approved, out string reason,
            WaferMapProcessSettings processSettings = null)
        {
            reason = "";
            try
            {
                if (!IsUsableMap(mapped) || !IsUsableMap(approved))
                {
                    reason = "mapped 또는 approved Input 맵이 비어 있습니다.";
                    return false;
                }

                if (!IsSupportedForEquipment(approved, out reason))
                    return false;

                // 기준 맵 승인/90·270 차단을 먼저 확인하고 공정에 적용한 방향으로 주소를 비교한다.
                if (processSettings != null)
                    approved = WaferMapProcessService.Prepare(approved, processSettings, "Input");

                DieMapGenerator.Normalize(mapped);
                DieMapGenerator.Normalize(approved);
                if (mapped.DieMapX != approved.DieMapX || mapped.DieMapY != approved.DieMapY ||
                    Math.Abs(mapped.PitchX - approved.PitchX) > 0.000001 ||
                    Math.Abs(mapped.PitchY - approved.PitchY) > 0.000001 ||
                    mapped.Entries.Count != approved.Entries.Count)
                {
                    reason = "Grid/Pitch/record 수가 승인 Input 맵과 다릅니다.";
                    return false;
                }

                var mappedByRaw = mapped.Entries
                    .Where(entry => entry != null)
                    .GroupBy(entry => DieMapGenerator.ResolveOriginalMapIndexX(entry) + "," +
                                      DieMapGenerator.ResolveOriginalMapIndexY(entry))
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
                if (mappedByRaw.Count != mapped.Entries.Count(entry => entry != null))
                {
                    reason = "Mapping 완료 맵에 중복 Original index가 있습니다.";
                    return false;
                }

                foreach (DieMapEntry approvedEntry in approved.Entries.Where(entry => entry != null))
                {
                    string key = DieMapGenerator.ResolveOriginalMapIndexX(approvedEntry) + "," +
                                 DieMapGenerator.ResolveOriginalMapIndexY(approvedEntry);
                    DieMapEntry mappedEntry;
                    if (!mappedByRaw.TryGetValue(key, out mappedEntry) ||
                        mappedEntry.DieMapX != approvedEntry.DieMapX ||
                        mappedEntry.DieMapY != approvedEntry.DieMapY ||
                        Math.Abs(mappedEntry.EquipmentGridX - approvedEntry.EquipmentGridX) > 0.000001 ||
                        Math.Abs(mappedEntry.EquipmentGridY - approvedEntry.EquipmentGridY) > 0.000001)
                    {
                        reason = "Original/Local/Equipment Grid domain이 승인 Input 맵과 다릅니다. raw=" + key;
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "mapped Input map compatibility check failed: " + ex.Message;
                return false;
            }
        }

        public static string ResolveExternalSourcePath(RecipeProject project, RecipeMapKind kind)
        {
            try
            {
                string sourcePath;
                DieMap map = LoadExternalSourceMap(project, ResolveFrame(project, kind), kind, out sourcePath);
                return IsUsableMap(map) ? sourcePath : "";
            }
            catch
            {
                return "";
            }
            finally
            {
            }
        }

        private static DieMap LoadExternalSourceMap(RecipeProject project, TapeFrameSubset frame, RecipeMapKind kind, out string sourcePath)
        {
            sourcePath = "";
            foreach (string path in BuildExternalSourceCandidates(project, frame, kind))
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                        continue;

                    DieMap map = DieMapGenerator.Load(path);
                    if (!IsUsableMap(map))
                        continue;

                    sourcePath = path;
                    return DieMapGenerator.Normalize(map);
                }
                catch
                {
                }
            }

            return null;
        }

        private static IEnumerable<string> BuildExternalSourceCandidates(RecipeProject project, TapeFrameSubset frame, RecipeMapKind kind)
        {
            var paths = new List<string>();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            AddCandidate(paths, RecipeMapPaths.ResolveBaseConfigured(project, kind));
            foreach (string name in BuildExternalSourceNames(frame, kind))
            {
                AddCandidate(paths, Path.Combine(baseDir, "Config", "WaferMap", name + ".txt"));
                AddCandidate(paths, Path.Combine(baseDir, "Config", "DieMaps", name + ".txt"));

                string driveRoot = Path.GetPathRoot(baseDir);
                if (!string.IsNullOrWhiteSpace(driveRoot))
                {
                    AddCandidate(paths, Path.Combine(driveRoot, "CDT-320", "Config", "WaferMap", name + ".txt"));
                    AddCandidate(paths, Path.Combine(driveRoot, "CDT-320", "Config", "DieMaps", name + ".txt"));
                }
            }

            return paths;
        }

        private static IEnumerable<string> BuildExternalSourceNames(TapeFrameSubset frame, RecipeMapKind kind)
        {
            var names = new List<string>();
            string specName = frame != null ? frame.FrameSpecName ?? "" : "";
            AddName(names, specName);

            string stripped = StripKnownRoleSuffix(specName);
            AddName(names, stripped);

            if (!string.IsNullOrWhiteSpace(stripped) && stripped.IndexOf('_') > 0)
                AddName(names, stripped.Substring(0, stripped.IndexOf('_')));

            return names;
        }

        private static string StripKnownRoleSuffix(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
            string[] suffixes =
            {
                "_InputWafer", "_OutputWafer", "-InputWafer", "-OutputWafer",
                " InputWafer", " OutputWafer", "_Input", "_Output",
                "-Input", "-Output", "_GoodBin", "_NgBin", "_Good", "_Ng"
            };

            foreach (string suffix in suffixes)
            {
                if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return text.Substring(0, text.Length - suffix.Length);
            }

            return text;
        }

        private static bool IsUsableMap(DieMap map)
        {
            return map != null &&
                   map.DieMapX > 0 &&
                   map.DieMapY > 0 &&
                   map.Entries != null &&
                   map.Entries.Count > 0;
        }

        private static int ResolveMapSizeX(DieMap map)
        {
            int maxX = map != null && map.Entries != null
                ? map.Entries.Where(e => e != null).Select(e => DieMapGenerator.ResolveMapIndexX(e)).DefaultIfEmpty(-1).Max() + 1
                : 0;
            return Math.Max(map != null ? map.DieMapX : 0, maxX);
        }

        private static int ResolveMapSizeY(DieMap map)
        {
            int maxY = map != null && map.Entries != null
                ? map.Entries.Where(e => e != null).Select(e => DieMapGenerator.ResolveMapIndexY(e)).DefaultIfEmpty(-1).Max() + 1
                : 0;
            return Math.Max(map != null ? map.DieMapY : 0, maxY);
        }

        private static void AddCandidate(List<string> paths, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!paths.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)))
                paths.Add(path);
        }

        private static void AddName(List<string> names, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            string safeName = RecipeMapPaths.SanitizeFileName(Path.GetFileNameWithoutExtension(name.Trim()));
            if (!names.Any(n => string.Equals(n, safeName, StringComparison.OrdinalIgnoreCase)))
                names.Add(safeName);
        }

        private static string AppendReason(string current, string next)
        {
            if (string.IsNullOrWhiteSpace(current))
                return next ?? "";
            if (string.IsNullOrWhiteSpace(next))
                return current;
            return current + " / " + next;
        }
    }
}
