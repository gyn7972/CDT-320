using System;
using System.Reflection;
using System.Runtime.Serialization;
using QMC.CDT320;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;

internal static class ExternalMapAlignGuardHarness
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("ASSERT FAILED: " + message);
    }

    public static int Main()
    {
        Assembly assembly = typeof(WaferMapData).Assembly;
        Type sequenceType = assembly.GetType("QMC.CDT320.Sequencing.InputStageAlignSequence", true);
        object sequence = FormatterServices.GetUninitializedObject(sequenceType);
        MethodInfo apply = sequenceType.GetMethod(
            "ApplyFrameSpecToMap",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert(apply != null, "ApplyFrameSpecToMap reflection");

        var externalMap = new WaferMapData
        {
            RowCount = 3,
            ColumnCount = 4,
            DieMap = new bool[,]
            {
                { true, false, true, false },
                { false, true, false, true },
                { true, true, false, false }
            },
            Ref1Row = 0,
            Ref1Col = 0,
            Ref2Row = 0,
            Ref2Col = 0
        };
        bool[,] before = (bool[,])externalMap.DieMap.Clone();
        var externalSpec = new TapeFrameSpec
        {
            Name = "EXTERNAL",
            EdgeSkipMode = "ExternalMap",
            DieMapX = 99,
            DieMapY = 88,
            OuterDiameterMm = 300.0,
            PitchX = 8.12,
            PitchY = 6.12
        };

        apply.Invoke(sequence, new object[] { externalMap, externalSpec });
        Assert(externalMap.RowCount == 3 && externalMap.ColumnCount == 4,
            "External map shape must remain unchanged");
        for (int row = 0; row < 3; row++)
        for (int col = 0; col < 4; col++)
            Assert(externalMap.DieMap[row, col] == before[row, col],
                "External Target/Skip mask must remain unchanged at " + row + "," + col);

        FieldInfo sourceExternal = sequenceType.GetField(
            "_resolvedAlignMapIsExternal",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert(sourceExternal != null, "resolved external source guard field");
        sourceExternal.SetValue(sequence, true);
        var staleSpecMap = new WaferMapData
        {
            RowCount = 2,
            ColumnCount = 3,
            DieMap = new bool[,]
            {
                { true, false, true },
                { false, true, false }
            }
        };
        bool[,] staleBefore = (bool[,])staleSpecMap.DieMap.Clone();
        var staleGridSpec = new TapeFrameSpec
        {
            Name = "STALE-GRID-SPEC",
            EdgeSkipMode = "Grid",
            DieMapX = 50,
            DieMapY = 60,
            OuterDiameterMm = 300.0,
            PitchX = 8.12,
            PitchY = 6.12
        };
        apply.Invoke(sequence, new object[] { staleSpecMap, staleGridSpec });
        Assert(staleSpecMap.RowCount == 2 && staleSpecMap.ColumnCount == 3,
            "Actual external source must override stale Grid spec");
        for (int row = 0; row < 2; row++)
        for (int col = 0; col < 3; col++)
            Assert(staleSpecMap.DieMap[row, col] == staleBefore[row, col],
                "Stale spec must not change external source mask at " + row + "," + col);

        sourceExternal.SetValue(sequence, false);
        var generatedMap = new WaferMapData
        {
            RowCount = 1,
            ColumnCount = 1,
            DieMap = new bool[1, 1]
        };
        var generatedSpec = new TapeFrameSpec
        {
            Name = "GRID",
            EdgeSkipMode = "Grid",
            DieMapX = 5,
            DieMapY = 6,
            OuterDiameterMm = 0.0,
            PitchX = 1.0,
            PitchY = 1.0
        };
        apply.Invoke(sequence, new object[] { generatedMap, generatedSpec });
        Assert(generatedMap.RowCount == 6 && generatedMap.ColumnCount == 5,
            "Non-external fallback behavior must remain intact");

        MaterialSpecs.Load();
        var project = new RecipeProject { FileName = "ExternalGridGuard" };
        RecipeProjectConsistencyService.EnsureStructure(project);
        project.Die.DieSpecName = "ExternalGridGuard_Die";
        project.InputFrame.FrameSpecName = "ExternalGridGuard_Input";
        project.InputFrame.EdgeSkipMode = "ExternalMap";
        project.InputFrame.DieMapX = 201;
        project.InputFrame.DieMapY = 228;
        project.InputFrame.PitchX = 8.12;
        project.InputFrame.PitchY = 6.12;
        project.InputFrame.OuterDiameterMm = 287.6;
        MaterialStateService.SyncRecipeTapeFrameSpec(project);
        TapeFrameSpec savedExternalSpec = MaterialSpecs.FindFrame("ExternalGridGuard_Input");
        Assert(savedExternalSpec != null, "External frame spec persisted");
        Assert(savedExternalSpec.DieMapX == 201 && savedExternalSpec.DieMapY == 228,
            "External MaterialSpec grid must preserve Base address domain");

        Console.WriteLine("PASS external align map domain/mask guard");
        return 0;
    }
}
