using System;
using System.IO;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;
using QMC.Common.Data.Store;

internal static class ApprovalPersistenceHarness
{
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("ASSERT FAILED: " + message);
    }

    private static int Main()
    {
        try
        {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ap-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(root);
            DataPaths.Root = root;

            var project = new RecipeProject { FileName = "AP-" + Guid.NewGuid().ToString("N").Substring(0, 8) };
            RecipeProjectConsistencyService.EnsureStructure(project);
            project.Die.WidthMm = 8.0;
            project.Die.HeightMm = 6.0;
            project.Die.ThicknessMm = 0.25;
            project.InputFrame.PitchX = 8.12;
            project.InputFrame.PitchY = 6.12;
            project.InputFrame.OuterDiameterMm = 300.0;
            project.OutputFrame.PitchX = 8.12;
            project.OutputFrame.PitchY = 6.12;
            project.OutputFrame.OuterDiameterMm = 300.0;

            RecipeMapBuildResult built = RecipeMapBuildService.ImportBaseAndBuildAll(
                project,
                @"D:\CDT-320\Config\WaferMap\RAD1.txt",
                RecipeStore.Save);
            Assert(built.Success, "import + Project commit: " + built.Message);

            foreach (RecipeMapKind kind in new[] { RecipeMapKind.Input, RecipeMapKind.GoodBin, RecipeMapKind.NgBin })
            {
                string configured = RecipeMapPaths.ResolveConfigured(project, kind);
                DieMap roleMap = DieMapGenerator.LoadJson(configured);
                RecipeMapBuildResult applied = RecipeMapBuildService.SaveRoleTargetMask(project, kind, roleMap, RecipeStore.Save);
                Assert(applied.Success, kind + " FINAL APPLY + Project commit: " + applied.Message);
            }

            RecipeProject loaded = RecipeStore.Load(project.FileName);
            Assert(loaded != null, "Project reload");
            Assert(loaded.MapApprovalVersion == 1, "approval version round-trip");
            Assert(loaded.InputMapApprovalHash == project.InputMapApprovalHash, "Input hash round-trip");
            Assert(loaded.GoodBinMapApprovalHash == project.GoodBinMapApprovalHash, "Good hash round-trip");
            Assert(loaded.NgBinMapApprovalHash == project.NgBinMapApprovalHash, "NG hash round-trip");

            foreach (RecipeMapKind kind in new[] { RecipeMapKind.Input, RecipeMapKind.GoodBin, RecipeMapKind.NgBin })
            {
                string sourcePath;
                string reason;
                DieMap map = RecipeDieMapResolver.LoadCompatibleMap(loaded, kind, out sourcePath, out reason);
                Assert(map != null, kind + " resolver after Project reload: " + reason);
            }

            Console.WriteLine("PASS root=" + root);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
