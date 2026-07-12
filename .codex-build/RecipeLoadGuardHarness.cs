using System;
using System.IO;
using QMC.CDT320.Recipes;

internal static class RecipeLoadGuardHarness
{
    private static int Main()
    {
        try
        {
            string recipeDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes");
            Directory.CreateDirectory(recipeDirectory);
            string projectPath = Path.Combine(recipeDirectory, "LegacyLoadGuard.Project");
            const string json =
                "{\"FileName\":\"LegacyLoadGuard\",\"ChipThickness\":150," +
                "\"Die\":{\"DieSpecName\":\"LEGACY-PLACEHOLDER\",\"WidthMm\":1,\"HeightMm\":1,\"ThicknessMm\":0.15}," +
                "\"Frame\":{\"DieSizeX\":8.12,\"DieSizeY\":3.12,\"PitchX\":8.12,\"PitchY\":6.12}," +
                "\"InputFrame\":{\"DieSizeX\":8.12,\"DieSizeY\":3.12,\"PitchX\":8.12,\"PitchY\":6.12}," +
                "\"OutputFrame\":{\"DieSizeX\":8.12,\"DieSizeY\":6.12,\"PitchX\":8.12,\"PitchY\":6.12}}";
            File.WriteAllText(projectPath, json);

            RecipeProject loaded = RecipeStore.Load("LegacyLoadGuard");
            Assert(loaded != null, "Legacy Project load");
            AssertNear(loaded.Die.WidthMm, 1.0, "Project.Die remains unchanged on load");
            AssertNear(loaded.InputFrame.DieSizeX, 8.12, "Input Frame remains unchanged on load");
            AssertNear(loaded.OutputFrame.DieSizeY, 6.12, "Output Frame remains unchanged on load");

            string beforeSave = Convert.ToBase64String(File.ReadAllBytes(projectPath));
            bool saveResult;
            using (FileStream locked = File.Open(projectPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                loaded.PartId = "MUST-NOT-COMMIT";
                saveResult = RecipeStore.Save(loaded);
            }
            Assert(!saveResult, "Locked Project save must fail");
            Assert(beforeSave == Convert.ToBase64String(File.ReadAllBytes(projectPath)),
                "Failed Project save preserves the previous file byte-for-byte");

            Console.WriteLine("PASS legacy Recipe load guard");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("ASSERT FAILED: " + message);
    }

    private static void AssertNear(double actual, double expected, string message)
    {
        Assert(Math.Abs(actual - expected) <= 0.000001,
            message + ", actual=" + actual + ", expected=" + expected);
    }
}
