using System;
using System.IO;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;

public static class BaseMapSplitScenario
{
    private static DieMap CreateMap(int width, int height, string source)
    {
        var map = new DieMap
        {
            FrameObjId = source,
            DieMapX = width,
            DieMapY = height,
            PitchX = 1.0,
            PitchY = 1.0,
            DieSizeX = 1.0,
            DieSizeY = 1.0,
            OuterDiameterMm = 100.0,
            SourceFileName = source + ".json",
            SourceFormat = "JSON",
            SourcePitchFromFile = true
        };
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                map.Entries.Add(new DieMapEntry
                {
                    Index = index++,
                    DieMapX = x,
                    DieMapY = y,
                    OriginalMapX = x,
                    OriginalMapY = y,
                    IsTarget = true,
                    BinCode = 1
                });
            }
        }
        return map;
    }

    private static TapeFrameSubset CreateFrame(string name)
    {
        return new TapeFrameSubset
        {
            FrameSpecName = name,
            DieMapX = 1,
            DieMapY = 1,
            PitchX = 0.0,
            PitchY = 0.0,
            DieSizeX = 1.0,
            DieSizeY = 1.0,
            OuterDiameterMm = 100.0,
            EdgeSkipMode = "ExternalMap"
        };
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    public static int Main()
    {
        try
        {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_base_split_scenario");
            Directory.CreateDirectory(root);
            string inputSource = Path.Combine(root, "input_source.json");
            string outputSource = Path.Combine(root, "output_source.json");
            DieMapGenerator.SaveJson(CreateMap(2, 2, "INPUT-SOURCE"), inputSource);
            DieMapGenerator.SaveJson(CreateMap(3, 1, "OUTPUT-SOURCE"), outputSource);

            var project = new RecipeProject
            {
                FileName = "_BASE_SPLIT_VERIFY",
                Die = new DieSubset { WidthMm = 1.0, HeightMm = 1.0, ThicknessMm = 0.1 },
                Frame = CreateFrame("INPUT"),
                InputFrame = CreateFrame("INPUT"),
                OutputFrame = CreateFrame("OUTPUT")
            };

            RecipeMapBuildResult inputResult =
                RecipeMapBuildService.ImportBaseAndBuildRole(project, inputSource, false, null);
            Require(inputResult.Success, "Input import failed: " + inputResult.Message);
            string inputBaseConfigured = project.InputBaseWaferMapFileName;
            Require(!string.IsNullOrWhiteSpace(inputBaseConfigured), "Input Base path was not saved.");
            Require(string.IsNullOrWhiteSpace(project.OutputBaseWaferMapFileName),
                "Input import changed Output Base path.");

            RecipeMapBuildResult outputResult =
                RecipeMapBuildService.ImportBaseAndBuildRole(project, outputSource, true, null);
            Require(outputResult.Success, "Output import failed: " + outputResult.Message);
            Require(string.Equals(inputBaseConfigured, project.InputBaseWaferMapFileName,
                StringComparison.OrdinalIgnoreCase), "Output import changed Input Base path.");
            Require(!string.IsNullOrWhiteSpace(project.OutputBaseWaferMapFileName),
                "Output Base path was not saved.");

            string inputBasePath = RecipeMapPaths.ResolveBaseConfigured(project, RecipeMapKind.Input);
            string outputBasePath = RecipeMapPaths.ResolveBaseConfigured(project, RecipeMapKind.GoodBin);
            Require(!string.Equals(inputBasePath, outputBasePath, StringComparison.OrdinalIgnoreCase),
                "Input/Output Base paths are still shared.");
            Require(File.Exists(inputBasePath) && File.Exists(outputBasePath),
                "Role Base files were not written.");

            DieMap inputBase = DieMapGenerator.Load(inputBasePath);
            DieMap outputBase = DieMapGenerator.Load(outputBasePath);
            DieMap inputMap = DieMapGenerator.Load(RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.Input));
            DieMap goodMap = DieMapGenerator.Load(RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.GoodBin));
            DieMap ngMap = DieMapGenerator.Load(RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.NgBin));
            Require(inputBase.Entries.Count == 4 && inputMap.Entries.Count == 4,
                "Input domain did not stay on Input Base.");
            Require(outputBase.Entries.Count == 3 && goodMap.Entries.Count == 3 && ngMap.Entries.Count == 3,
                "Output domain did not stay on Output Base.");

            string outputBaseConfigured = project.OutputBaseWaferMapFileName;
            string outputBaseContent = File.ReadAllText(outputBasePath);
            string goodMapContent = File.ReadAllText(RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.GoodBin));
            string ngMapContent = File.ReadAllText(RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.NgBin));
            project.InputFrame.PitchX = 0.2;
            project.InputFrame.PitchY = 0.3;
            RecipeMapBuildResult inputRebuild =
                RecipeMapBuildService.RebuildDerivedMaps(project, true, false, true, null);
            Require(inputRebuild.Success, "Input-only rebuild failed: " + inputRebuild.Message);
            Require(project.OutputBaseWaferMapFileName == outputBaseConfigured,
                "Input-only rebuild changed Output Base configured path.");
            Require(File.ReadAllText(outputBasePath) == outputBaseContent,
                "Input-only rebuild rewrote Output Base.");
            Require(File.ReadAllText(RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.GoodBin)) == goodMapContent,
                "Input-only rebuild rewrote Good map.");
            Require(File.ReadAllText(RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.NgBin)) == ngMapContent,
                "Input-only rebuild rewrote NG map.");

            inputMap = DieMapGenerator.Load(RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.Input));
            Require(Math.Abs(inputMap.PitchX - 1.2) < 0.000001 &&
                    Math.Abs(inputMap.PitchY - 1.3) < 0.000001,
                "Input-only rebuild did not apply Input pitch.");
            RecipeMapBuildResult inputApply =
                RecipeMapBuildService.SaveRoleTargetMask(project, RecipeMapKind.Input, inputMap, null);
            Require(inputApply.Success, "Input FINAL APPLY failed with split domains: " + inputApply.Message);
            RecipeMapBuildResult goodApply =
                RecipeMapBuildService.SaveRoleTargetMask(project, RecipeMapKind.GoodBin, goodMap, null);
            Require(goodApply.Success, "Good FINAL APPLY failed with split domains: " + goodApply.Message);

            var legacy = new RecipeProject
            {
                BaseWaferMapFileName = "legacy_base.json",
                InputBaseWaferMapFileName = "",
                OutputBaseWaferMapFileName = ""
            };
            Require(RecipeMapPaths.ConfiguredBaseFileName(legacy, RecipeMapKind.Input) == "legacy_base.json",
                "Legacy Input fallback failed.");
            Require(RecipeMapPaths.ConfiguredBaseFileName(legacy, RecipeMapKind.GoodBin) == "legacy_base.json",
                "Legacy Output fallback failed.");
            RecipeMapPaths.SetConfiguredBaseFileName(legacy, RecipeMapKind.Input, "input_only.json");
            Require(RecipeMapPaths.ConfiguredBaseFileName(legacy, RecipeMapKind.Input) == "input_only.json",
                "Input role override failed.");
            Require(RecipeMapPaths.ConfiguredBaseFileName(legacy, RecipeMapKind.GoodBin) == "legacy_base.json",
                "Input role override changed Output fallback.");

            string cloneName = "_BASE_SPLIT_CLONE_" + DateTime.UtcNow.Ticks;
            RecipeProjectCloneResult cloneResult = RecipeProjectCloneService.Clone(project, cloneName);
            Require(cloneResult.Success, "Split Base clone failed: " + cloneResult.Message);
            Require(!string.IsNullOrWhiteSpace(cloneResult.Project.InputBaseWaferMapFileName) &&
                    !string.IsNullOrWhiteSpace(cloneResult.Project.OutputBaseWaferMapFileName),
                "Clone did not preserve both role Base paths.");
            Require(!string.Equals(
                    cloneResult.Project.InputBaseWaferMapFileName,
                    cloneResult.Project.OutputBaseWaferMapFileName,
                    StringComparison.OrdinalIgnoreCase),
                "Clone collapsed role Base paths.");
            Require(File.Exists(RecipeMapPaths.ResolveBaseConfigured(cloneResult.Project, RecipeMapKind.Input)) &&
                    File.Exists(RecipeMapPaths.ResolveBaseConfigured(cloneResult.Project, RecipeMapKind.GoodBin)),
                "Clone role Base files were not copied.");
            RecipeProject persistedClone = RecipeStore.Load(cloneName);
            Require(persistedClone != null &&
                    persistedClone.InputBaseWaferMapFileName == cloneResult.Project.InputBaseWaferMapFileName &&
                    persistedClone.OutputBaseWaferMapFileName == cloneResult.Project.OutputBaseWaferMapFileName,
                "Role Base paths were not preserved by Project serialization.");

            Console.WriteLine("PASS");
            Console.WriteLine("InputBase=" + inputBasePath + " entries=" + inputBase.Entries.Count);
            Console.WriteLine("OutputBase=" + outputBasePath + " entries=" + outputBase.Entries.Count);
            Console.WriteLine("InputMap=" + inputMap.Entries.Count +
                " GoodMap=" + goodMap.Entries.Count + " NgMap=" + ngMap.Entries.Count);
            Console.WriteLine("InputOnlyRebuild=PASS OutputUnchanged=PASS FinalApply=PASS");
            Console.WriteLine("LegacyFallback=PASS");
            Console.WriteLine("CloneRoleBases=PASS Serialization=PASS files=" + cloneResult.MapFileCount);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
    }
}
