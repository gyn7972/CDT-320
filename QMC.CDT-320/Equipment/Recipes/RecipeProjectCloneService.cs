using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.CDT320.Recipes
{
    public sealed class RecipeProjectCloneResult
    {
        public bool Success { get; internal set; }
        public string Message { get; internal set; } = "";
        public RecipeProject Project { get; internal set; }
        public bool UnitRecipeCopied { get; internal set; }
        public int MapFileCount { get; internal set; }
    }

    /// <summary>
    /// Clones a Project file, unit recipe files, and all configured map assets.
    /// Existing recipes keep their legacy paths; only the clone receives project-owned map paths.
    /// </summary>
    public static class RecipeProjectCloneService
    {
        private static readonly string[] MapSidecarExtensions = { ".txt", ".csv", ".json" };

        public static RecipeProjectCloneResult Clone(RecipeProject source, string targetRecipeName)
        {
            var result = new RecipeProjectCloneResult();
            string stageDirectory = "";
            string targetDirectory = "";
            string targetProjectName = "";
            bool targetDirectoryCommitted = false;
            bool projectWriteAttempted = false;
            int mapFileCount = 0;

            try
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));

                string sourceProjectName = StorageName.Safe(source.FileName);
                targetProjectName = StorageName.Safe(targetRecipeName);
                if (string.IsNullOrWhiteSpace(sourceProjectName) || sourceProjectName == "_")
                    throw new InvalidOperationException("Source recipe name is empty.");
                if (string.IsNullOrWhiteSpace(targetProjectName) || targetProjectName == "_")
                    throw new InvalidOperationException("Target recipe name is empty.");
                if (string.Equals(sourceProjectName, targetProjectName, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Source and target recipe names are the same.");

                string targetProjectPath = Path.Combine(RecipeStore.Dir, targetProjectName + ".Project");
                targetDirectory = RecipeDataStore.DirOf(targetProjectName);
                if (File.Exists(targetProjectPath) || RecipeStore.Load(targetProjectName) != null)
                    throw new InvalidOperationException("Target Project already exists: " + targetProjectName);
                if (Directory.Exists(targetDirectory))
                    throw new InvalidOperationException("Target Unit Recipe folder already exists: " + targetDirectory);

                RecipeProject clone = DeepClone(source);
                clone.FileName = targetProjectName;
                PrepareIndependentSpecNames(clone, targetProjectName);

                string sourceDirectory = RecipeDataStore.DirOf(sourceProjectName);
                stageDirectory = BuildStageDirectory(targetDirectory);
                Directory.CreateDirectory(stageDirectory);

                if (Directory.Exists(sourceDirectory))
                {
                    CopyDirectoryContents(sourceDirectory, stageDirectory, true);
                    result.UnitRecipeCopied = Directory.GetFiles(sourceDirectory, "*.recipe.json").Length > 0;
                }

                string stageMapDirectory = Path.Combine(stageDirectory, "Maps");
                Directory.CreateDirectory(stageMapDirectory);

                string inputBaseSourcePath = ResolveBaseSourcePath(source, RecipeMapKind.Input);
                if (RecipeDieMapResolver.IsExternalFrame(RecipeDieMapResolver.ResolveFrame(source, RecipeMapKind.Input)) &&
                    string.IsNullOrWhiteSpace(inputBaseSourcePath))
                {
                    throw new FileNotFoundException("Input Base WaferMap을 찾을 수 없습니다. recipe=" + sourceProjectName);
                }

                string outputBaseSourcePath = ResolveBaseSourcePath(source, RecipeMapKind.GoodBin);
                if (RecipeDieMapResolver.IsExternalFrame(RecipeDieMapResolver.ResolveFrame(source, RecipeMapKind.GoodBin)) &&
                    string.IsNullOrWhiteSpace(outputBaseSourcePath))
                {
                    throw new FileNotFoundException("Output Base WaferMap을 찾을 수 없습니다. recipe=" + sourceProjectName);
                }

                clone.BaseWaferMapFileName = "";
                clone.InputBaseWaferMapFileName = CopyMapFamily(
                    inputBaseSourcePath,
                    targetProjectName,
                    RecipeMapPaths.BaseFileSuffix(RecipeMapKind.Input),
                    stageMapDirectory,
                    ref mapFileCount);
                clone.OutputBaseWaferMapFileName = CopyMapFamily(
                    outputBaseSourcePath,
                    targetProjectName,
                    RecipeMapPaths.BaseFileSuffix(RecipeMapKind.GoodBin),
                    stageMapDirectory,
                    ref mapFileCount);
                clone.InputDieMapFileName = CopyConfiguredMap(
                    source,
                    RecipeMapKind.Input,
                    targetProjectName,
                    stageMapDirectory,
                    ref mapFileCount);
                clone.GoodBinDieMapFileName = CopyConfiguredMap(
                    source,
                    RecipeMapKind.GoodBin,
                    targetProjectName,
                    stageMapDirectory,
                    ref mapFileCount);
                clone.NgBinDieMapFileName = CopyConfiguredMap(
                    source,
                    RecipeMapKind.NgBin,
                    targetProjectName,
                    stageMapDirectory,
                    ref mapFileCount);
                clone.OutputDieMapFileName = CopyMapFamily(
                    RecipeMapPaths.ResolveConfiguredPath(source.OutputDieMapFileName),
                    targetProjectName,
                    "OutputDieMap",
                    stageMapDirectory,
                    ref mapFileCount);

                Directory.Move(stageDirectory, targetDirectory);
                stageDirectory = "";
                targetDirectoryCommitted = true;

                projectWriteAttempted = true;
                if (!RecipeStore.Save(clone))
                    throw new IOException("Target Project file save failed: " + targetProjectName);

                result.Success = true;
                result.Project = clone;
                result.MapFileCount = mapFileCount;
                result.Message = result.UnitRecipeCopied
                    ? "Project, Unit Recipe, and map assets were cloned."
                    : "Project and map assets were cloned. Unit Recipe source folder was empty.";
                return result;
            }
            catch (Exception ex)
            {
                if (projectWriteAttempted && !string.IsNullOrWhiteSpace(targetProjectName))
                    RecipeStore.Delete(targetProjectName);
                if (targetDirectoryCommitted && !string.IsNullOrWhiteSpace(targetProjectName))
                    RecipeDataStore.DeleteRecipe(targetProjectName);

                result.Success = false;
                result.Message = ex.Message;
                result.Project = null;
                result.MapFileCount = mapFileCount;
                return result;
            }
            finally
            {
                TryDeleteStageDirectory(stageDirectory);
            }
        }

        private static RecipeProject DeepClone(RecipeProject source)
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(RecipeProject));
                using (var stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, source);
                    stream.Position = 0;
                    return (RecipeProject)serializer.ReadObject(stream);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static void PrepareIndependentSpecNames(RecipeProject clone, string targetProjectName)
        {
            try
            {
                if (clone == null)
                    return;

                if (clone.Frame == null)
                    clone.Frame = new TapeFrameSubset();
                if (clone.InputFrame == null)
                    clone.InputFrame = CloneFrame(clone.Frame);
                if (clone.OutputFrame == null)
                    clone.OutputFrame = CloneFrame(clone.Frame);

                string safeName = RecipeMapPaths.SanitizeFileName(targetProjectName);
                if (clone.Die != null)
                    clone.Die.DieSpecName = safeName + "_Die";

                clone.Frame.FrameSpecName = safeName + "_InputWafer";
                clone.InputFrame.FrameSpecName = safeName + "_InputWafer";
                clone.OutputFrame.FrameSpecName = safeName + "_OutputWafer";
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static TapeFrameSubset CloneFrame(TapeFrameSubset source)
        {
            try
            {
                if (source == null)
                    return new TapeFrameSubset();

                return new TapeFrameSubset
                {
                    FrameSpecName = source.FrameSpecName,
                    DieMapX = source.DieMapX,
                    DieMapY = source.DieMapY,
                    PitchX = source.PitchX,
                    PitchY = source.PitchY,
                    DieSizeX = source.DieSizeX,
                    DieSizeY = source.DieSizeY,
                    Rotate = source.Rotate,
                    OuterDiameterMm = source.OuterDiameterMm,
                    EdgeSkipMode = source.EdgeSkipMode,
                    SideEdgeSkip = source.SideEdgeSkip,
                    TopBottomEdgeSkip = source.TopBottomEdgeSkip,
                    SideEdgeSkipMm = source.SideEdgeSkipMm,
                    TopBottomEdgeSkipMm = source.TopBottomEdgeSkipMm
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static string ResolveBaseSourcePath(RecipeProject source, RecipeMapKind kind)
        {
            try
            {
                string configuredPath = RecipeMapPaths.ResolveBaseConfigured(source, kind);
                if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
                    return configuredPath;

                return RecipeDieMapResolver.ResolveExternalSourcePath(source, kind);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static string CopyConfiguredMap(
            RecipeProject source,
            RecipeMapKind kind,
            string targetProjectName,
            string stageMapDirectory,
            ref int mapFileCount)
        {
            try
            {
                string configured = RecipeMapPaths.ConfiguredFileName(source, kind);
                if (string.IsNullOrWhiteSpace(configured))
                    return "";

                return CopyMapFamily(
                    RecipeMapPaths.ResolveConfigured(source, kind),
                    targetProjectName,
                    RecipeMapPaths.FileSuffix(kind),
                    stageMapDirectory,
                    ref mapFileCount);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static string CopyMapFamily(
            string sourcePath,
            string targetProjectName,
            string targetSuffix,
            string stageMapDirectory,
            ref int mapFileCount)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath))
                    return "";
                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException("Configured map file was not found.", sourcePath);

                string sourceExtension = Path.GetExtension(sourcePath);
                if (string.IsNullOrWhiteSpace(sourceExtension))
                    sourceExtension = ".json";

                string targetBaseName = RecipeMapPaths.SanitizeFileName(targetProjectName + "_" + targetSuffix);
                string sourceBasePath = Path.Combine(
                    Path.GetDirectoryName(sourcePath) ?? "",
                    Path.GetFileNameWithoutExtension(sourcePath));
                var copiedSourcePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                CopyMapFile(sourcePath, Path.Combine(stageMapDirectory, targetBaseName + sourceExtension));
                copiedSourcePaths.Add(Path.GetFullPath(sourcePath));
                mapFileCount++;

                foreach (string extension in MapSidecarExtensions)
                {
                    string sidecarSourcePath = sourceBasePath + extension;
                    if (!File.Exists(sidecarSourcePath))
                        continue;

                    string fullSidecarPath = Path.GetFullPath(sidecarSourcePath);
                    if (!copiedSourcePaths.Add(fullSidecarPath))
                        continue;

                    CopyMapFile(sidecarSourcePath, Path.Combine(stageMapDirectory, targetBaseName + extension));
                    mapFileCount++;
                }

                return RecipeMapPaths.BuildProjectMapRelativePath(
                    targetProjectName,
                    targetBaseName + sourceExtension);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static void CopyMapFile(string sourcePath, string targetPath)
        {
            try
            {
                string targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);
                File.Copy(sourcePath, targetPath, true);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static void CopyDirectoryContents(string sourceDirectory, string targetDirectory, bool skipProjectMaps)
        {
            try
            {
                Directory.CreateDirectory(targetDirectory);
                foreach (string sourceFile in Directory.GetFiles(sourceDirectory))
                {
                    string targetFile = Path.Combine(targetDirectory, Path.GetFileName(sourceFile));
                    File.Copy(sourceFile, targetFile, true);
                }

                foreach (string sourceChildDirectory in Directory.GetDirectories(sourceDirectory))
                {
                    if (skipProjectMaps &&
                        string.Equals(Path.GetFileName(sourceChildDirectory), "Maps", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string targetChildDirectory = Path.Combine(targetDirectory, Path.GetFileName(sourceChildDirectory));
                    CopyDirectoryContents(sourceChildDirectory, targetChildDirectory, false);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static string BuildStageDirectory(string targetDirectory)
        {
            try
            {
                string parent = Path.GetDirectoryName(targetDirectory);
                if (string.IsNullOrWhiteSpace(parent))
                    throw new InvalidOperationException("Target Recipe folder parent is empty.");

                string name = ".clone-" + Path.GetFileName(targetDirectory) + "-" + Guid.NewGuid().ToString("N");
                return Path.Combine(parent, name);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static void TryDeleteStageDirectory(string stageDirectory)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(stageDirectory) || !Directory.Exists(stageDirectory))
                    return;

                string fullStagePath = Path.GetFullPath(stageDirectory);
                string recipeRoot = Path.GetFullPath(RecipeDataStore.Root)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!fullStagePath.StartsWith(recipeRoot, StringComparison.OrdinalIgnoreCase) ||
                    !Path.GetFileName(fullStagePath).StartsWith(".clone-", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                Directory.Delete(fullStagePath, true);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
