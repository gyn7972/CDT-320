using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using QMC.CDT320;
using QMC.CDT320.Materials;

internal static class VisionInspectionResultFileWriterTests
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

    public static int Main()
    {
        try
        {
            VerifyOutputHeader();
            VerifyOutputInspectionColumns();
            VerifyFlyingDiePolicy();
            VerifyLegacyOutputRowsAreUpgradedOnce();
            Console.WriteLine("PASS: VisionInspectionResultFileWriter OUTPUT 27-column tests");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
    }

    private static void VerifyFlyingDiePolicy()
    {
        DieInspectionRecord bottom = Record(
            "Bottom",
            Measurement("BottomVision_bottom_item_offset_x", 0.001),
            Measurement("BottomVision_bottom_item_offset_y", -0.002),
            Measurement("BottomVision_bottom_item_chipping_top", 0.0021));
        DieMaterial die = new DieMaterial
        {
            DieId = "FLYING-1",
            WaferID_Input = "INPUT-1",
            InputSequenceNo = 9,
            Wafer_IndexX = 11,
            Wafer_IndexY = 12,
            Wafer_OriginalIndexX = 11,
            Wafer_OriginalIndexY = 12,
            Inspections = new List<DieInspectionRecord> { bottom }
        };
        WaferMaterial wafer = new WaferMaterial { WaferId = "NG-1" };

        object payload = InvokePrivate(
            "BuildPlacePayload",
            "Recipe",
            "Lot",
            0,
            BinSide.Ng,
            wafer,
            die,
            null,
            bottom,
            null,
            new DateTime(2026, 9, 11, 10, 0, 0),
            new DateTime(2026, 9, 11, 10, 1, 0),
            true);
        string detailLine = (string)payload.GetType()
            .GetProperty("DetailLine", BindingFlags.Public | BindingFlags.Instance)
            .GetValue(payload, null);
        List<string> fields = ParseCsvLine(detailLine);
        int[] keyColumns = (int[])payload.GetType()
            .GetProperty("DetailKeyColumns", BindingFlags.Public | BindingFlags.Instance)
            .GetValue(payload, null);

        AssertEqual(27, fields.Count, "Flying Die detail column count");
        AssertEqual("-1", fields[6], "Flying Die PlaceRow");
        AssertEqual("-1", fields[7], "Flying Die PlaceCol");
        AssertEqual("255", fields[15], "Flying Die TargetBin");
        AssertEqual(3, keyColumns.Length, "Flying Die key column count");
        AssertEqual(5, keyColumns[0], "Flying Die key output wafer column");
        AssertEqual(3, keyColumns[1], "Flying Die key input row column");
        AssertEqual(4, keyColumns[2], "Flying Die key input col column");
        AssertEqual("2.1", fields[18], "Flying Die Bottom Top value");
        for (int i = 22; i <= 26; i++)
            AssertEqual("", fields[i], "Flying Die unavailable inspection field " + i);
    }

    private static void VerifyOutputHeader()
    {
        Type writerType = GetWriterType();
        FieldInfo headerField = writerType.GetField("OutputDieHeader", PrivateStatic);
        if (headerField == null)
            throw new MissingFieldException(writerType.FullName, "OutputDieHeader");

        string[] headers = ((string)headerField.GetRawConstantValue()).Split(',');
        string[] expected =
        {
            "CHIP_SEQ", "HEAD", "PICK_WAFER_ID", "PICK_WAFER_ROW", "PICK_WAFER_COL",
            "PLACE_WAFER_ID", "PLACE_WAFER_ROW", "PLACE_WAFER_COL", "ChipSizeX", "ChipSizeY",
            "DieGapLeft", "DieGapRight", "DieGapTop", "DieGapBottom", "ANGLE", "TargetBin",
            "placement_offset_x_mm", "placement_offset_y_mm", "Back_Chipping_Top_Size",
            "Back_Chipping_Right_Size", "Back_Chipping_Bottom_Size", "Back_Chipping_Left_Size",
            "Back_Foreign_Size", "Side_Chipping_Bottom", "Side_Chipping_Left", "Side_Chipping_Top",
            "Side_Chipping_Right"
        };

        AssertEqual(expected.Length, headers.Length, "OUTPUT header column count");
        for (int i = 0; i < expected.Length; i++)
            AssertEqual(expected[i], headers[i], "OUTPUT header field " + i);
    }

    private static void VerifyOutputInspectionColumns()
    {
        DieInspectionRecord bottom = Record(
            "Bottom",
            Measurement("BottomVision_bottom_item_offset_x", 0.001),
            Measurement("BottomVision_bottom_item_offset_y", -0.002),
            Measurement("BottomVision_bottom_item_width", 8.0718),
            Measurement("BottomVision_bottom_item_height", 6.0714),
            Measurement("BottomVision_bottom_item_chipping_top", 0.0021),
            Measurement("BottomVision_bottom_item_chipping_right", 0.0032),
            Measurement("BottomVision_bottom_item_chipping_bottom", 0.0),
            Measurement("BottomVision_bottom_item_chipping_left", 0.0043));

        DieInspectionRecord side0 = Record(
            "Side0",
            Measurement("Side0Vision_FrontSide_measure_valid", 1),
            Measurement("Side0Vision_FrontSide_ch0_valid", 1),
            Measurement("Side0Vision_FrontSide_ch0_side_item_max_chipping_depth", 0.0024),
            Measurement("Side0Vision_RearSide_measure_valid", 1),
            Measurement("Side0Vision_RearSide_ch0_valid", 1),
            Measurement("Side0Vision_RearSide_ch0_side_item_max_chipping_depth", 0.0039));

        DieInspectionRecord side90 = Record(
            "Side90",
            Measurement("Side90Vision_RearSide_measure_valid", 1),
            Measurement("Side90Vision_RearSide_ch1_valid", 1),
            Measurement("Side90Vision_RearSide_ch1_side_item_max_chipping_depth", 0.0017),
            Measurement("Side90Vision_FrontSide_measure_valid", 1),
            Measurement("Side90Vision_FrontSide_ch1_valid", 1),
            Measurement("Side90Vision_FrontSide_ch1_side_item_max_chipping_depth", 0.0012));

        DieMaterial die = new DieMaterial
        {
            DieId = "DIE-1",
            WaferID_Input = "INPUT-1",
            WaferID_Output = "OUTPUT-1",
            InputSequenceNo = 7,
            PickedPickerNo = 2,
            Wafer_IndexX = 33,
            Wafer_IndexY = 25,
            Wafer_OriginalIndexX = 33,
            Wafer_OriginalIndexY = 25,
            Bin_IndexX = 9,
            Bin_IndexY = 4,
            Output_BinCode = 1,
            Inspections = new List<DieInspectionRecord> { bottom, side0, side90 }
        };

        OutputReceiveSlotMaterial slot = new OutputReceiveSlotMaterial
        {
            DieUid = die.DieId,
            DieMapX = 9,
            DieMapY = 4,
            BinCode = 1
        };
        WaferMaterial wafer = new WaferMaterial
        {
            WaferId = "OUTPUT-1",
            OutputCassetteId = "GOOD-1",
            OutputReceiveSlots = new List<OutputReceiveSlotMaterial> { slot }
        };

        object payload = InvokePrivate(
            "BuildPlacePayload",
            "Recipe",
            "Lot",
            0,
            BinSide.Good,
            wafer,
            die,
            slot,
            bottom,
            null,
            new DateTime(2026, 9, 11, 10, 0, 0),
            new DateTime(2026, 9, 11, 10, 1, 0),
            false);

        string detailLine = (string)payload.GetType()
            .GetProperty("DetailLine", BindingFlags.Public | BindingFlags.Instance)
            .GetValue(payload, null);
        List<string> fields = ParseCsvLine(detailLine);

        AssertEqual(27, fields.Count, "OUTPUT detail column count");
        string[] expectedInspectionFields =
        {
            "2.1", "3.2", "0", "4.3", "", "2.4", "1.7", "3.9", "1.2"
        };
        for (int i = 0; i < expectedInspectionFields.Length; i++)
            AssertEqual(expectedInspectionFields[i], fields[18 + i], "inspection field " + (18 + i));

        string inputLine = (string)InvokePrivate(
            "BuildInputResultLine",
            "Recipe",
            "Lot",
            die,
            slot,
            bottom,
            null);
        List<string> inputFields = ParseCsvLine(inputLine);
        AssertEqual(55, inputFields.Count, "INPUT detail column count");
        for (int i = 0; i < expectedInspectionFields.Length; i++)
        {
            AssertEqual(
                inputFields[29 + i],
                fields[18 + i],
                "INPUT/OUTPUT inspection field parity " + i);
        }

        SetMeasurement(side90, "Side90Vision_FrontSide_ch1_valid", 0);
        payload = InvokePrivate(
            "BuildPlacePayload",
            "Recipe",
            "Lot",
            0,
            BinSide.Good,
            wafer,
            die,
            slot,
            bottom,
            null,
            new DateTime(2026, 9, 11, 10, 0, 0),
            new DateTime(2026, 9, 11, 10, 1, 0),
            false);
        detailLine = (string)payload.GetType()
            .GetProperty("DetailLine", BindingFlags.Public | BindingFlags.Instance)
            .GetValue(payload, null);
        fields = ParseCsvLine(detailLine);
        AssertEqual("", fields[26], "invalid Side Right must stay blank");
    }

    private static void VerifyLegacyOutputRowsAreUpgradedOnce()
    {
        string legacy = "1,2,\"INPUT,WITH-COMMA\",4,5,6,7,8,9,10,11,12,13,14,15,16,17,18";
        string current = legacy + ",,,,,,,,,";
        var lines = new List<string> { "summary header", "summary", "legacy header", legacy, current };

        bool changed = (bool)InvokePrivate("UpgradeLegacyOutputDetailLines", lines);
        AssertEqual(true, changed, "legacy upgrade changed");
        AssertEqual(27, ParseCsvLine(lines[3]).Count, "legacy row upgraded column count");
        AssertEqual(legacy + ",,,,,,,,,", lines[3], "legacy row value preservation");
        AssertEqual(current, lines[4], "current row remains unchanged");

        string onceUpgraded = lines[3];
        changed = (bool)InvokePrivate("UpgradeLegacyOutputDetailLines", lines);
        AssertEqual(false, changed, "second legacy upgrade must be idempotent");
        AssertEqual(onceUpgraded, lines[3], "second legacy upgrade row preservation");
    }

    private static DieInspectionRecord Record(string inspectionType, params InspectionMeasurement[] measurements)
    {
        return new DieInspectionRecord
        {
            InspectionType = inspectionType,
            Measurements = new List<InspectionMeasurement>(measurements)
        };
    }

    private static InspectionMeasurement Measurement(string name, double value)
    {
        return new InspectionMeasurement
        {
            Name = name,
            Value = value,
            RawValue = value.ToString("0.####", CultureInfo.InvariantCulture)
        };
    }

    private static void SetMeasurement(DieInspectionRecord record, string name, double value)
    {
        foreach (InspectionMeasurement measurement in record.Measurements)
        {
            if (!string.Equals(measurement.Name, name, StringComparison.Ordinal))
                continue;

            measurement.Value = value;
            measurement.RawValue = value.ToString("0.####", CultureInfo.InvariantCulture);
            return;
        }

        throw new InvalidOperationException("Measurement not found: " + name);
    }

    private static object InvokePrivate(string methodName, params object[] arguments)
    {
        Type writerType = GetWriterType();
        MethodInfo method = writerType.GetMethod(methodName, PrivateStatic);
        if (method == null)
            throw new MissingMethodException(writerType.FullName, methodName);

        try
        {
            return method.Invoke(null, arguments);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    private static Type GetWriterType()
    {
        return typeof(DieMaterial).Assembly.GetType(
            "QMC.CDT320.Materials.VisionInspectionResultFileWriter",
            true);
    }

    private static List<string> ParseCsvLine(string line)
    {
        return (List<string>)InvokePrivate("ParseCsvLine", line);
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                name + ": expected=" + expected + ", actual=" + actual);
        }
    }
}
