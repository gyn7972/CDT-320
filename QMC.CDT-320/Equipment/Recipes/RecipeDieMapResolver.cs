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

                string configuredPath = RecipeMapPaths.ResolveConfigured(project, kind);
                if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
                {
                    DieMap configuredMap = DieMapGenerator.Load(configuredPath);
                    if (IsUsableMap(configuredMap))
                    {
                        string mismatch;
                        if (IsCompatibleWithFrame(configuredMap, frame, out mismatch))
                        {
                            sourcePath = configuredPath;
                            return DieMapGenerator.Normalize(configuredMap);
                        }

                        reason = "configured map mismatch. path=" + configuredPath + ", " + mismatch;
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
                        sourcePath = sidecarCsvPath;
                        return DieMapGenerator.Normalize(sidecarCsvMap);
                    }

                    if (!string.IsNullOrWhiteSpace(sidecarCsvReason))
                        reason = AppendReason(reason, sidecarCsvReason);
                }
                else if (!string.IsNullOrWhiteSpace(configuredPath))
                {
                    reason = "configured map file not found. path=" + configuredPath;
                }

                if (IsExternalFrame(frame))
                {
                    DieMap externalMap = LoadExternalSourceMap(project, frame, kind, out sourcePath);
                    if (IsUsableMap(externalMap))
                    {
                        string mismatch;
                        if (IsCompatibleWithFrame(externalMap, frame, out mismatch))
                        {
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

        public static bool IsCompatibleWithFrame(DieMap map, TapeFrameSubset frame, out string reason)
        {
            reason = "";
            try
            {
                if (!IsUsableMap(map) || frame == null)
                    return true;

                int frameX = Math.Max(1, frame.DieMapX);
                int frameY = Math.Max(1, frame.DieMapY);
                int mapX = ResolveMapSizeX(map);
                int mapY = ResolveMapSizeY(map);

                bool sizeMismatch = mapX != frameX || mapY != frameY;
                bool pitchMismatch =
                    (frame.PitchX > 0.0 && map.PitchX > 0.0 && Math.Abs(map.PitchX - frame.PitchX) > 1e-6) ||
                    (frame.PitchY > 0.0 && map.PitchY > 0.0 && Math.Abs(map.PitchY - frame.PitchY) > 1e-6);

                if (!sizeMismatch && !pitchMismatch)
                    return true;

                // 현재 기준: ExternalMap은 원본 wafer map index 범위가 frame grid와 같아야 한다.
                reason =
                    "frame=" + (frame.FrameSpecName ?? "") +
                    ", frameDie=" + frameX + "x" + frameY +
                    ", mapDie=" + mapX + "x" + mapY +
                    ", framePitch=(" + frame.PitchX.ToString("F6") + "," + frame.PitchY.ToString("F6") + ")" +
                    ", mapPitch=(" + map.PitchX.ToString("F6") + "," + map.PitchY.ToString("F6") + ")";
                return false;
            }
            catch (Exception ex)
            {
                reason = "compatibility check failed: " + ex.Message;
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
            AddCandidate(paths, RecipeMapPaths.ResolveBaseConfigured(project));
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
