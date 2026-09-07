using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using QMC.CDT320;

internal static class InputWaferBarcodePolicyTests
{
    private static int _passed;
    private static int _failed;

    private static int Main(string[] args)
    {
        Test("disabled-preserves-legacy", delegate { CheckValidation(false, 0, false, null, null, true); });
        Test("five-character-match", delegate { CheckValidation(true, 5, true, "YZAMH", "YZAMH.02", true); });
        Test("three-character-match", delegate { CheckValidation(true, 3, true, "YZAMH", "YZAXY.02", true); });
        Test("case-insensitive-match", delegate { CheckValidation(true, 5, true, "yzamh", "YZAMH.02", true); });
        Test("trimmed-values", delegate { CheckValidation(true, 5, true, " YZAMH ", " YZAMH.02 ", true); });
        Test("one-character-minimum", delegate { CheckValidation(true, 1, true, "Y", "Y.02", true); });
        Test("128-character-maximum", delegate { CheckValidation(true, 128, true, new string('A', 128), new string('a', 128), true); });
        Test("same-lot-other-wafer-is-allowed", delegate { CheckValidation(true, 5, true, "YZAMH", "YZAMH.03", true); });
        Test("barcode-off-is-rejected", delegate { CheckValidation(true, 5, false, "YZAMH", "YZAMH.02", false); });
        Test("different-prefix-is-rejected", delegate { CheckValidation(true, 5, true, "YZAMH", "YZ9XH.02", false); });
        Test("internal-lot-whitespace-is-not-removed", delegate { CheckValidation(true, 5, true, "YZ AMH", "YZAMH.02", false); });
        foreach (int length in new[] { 0, -1, 129, int.MinValue, int.MaxValue })
        {
            int invalidLength = length;
            Test("invalid-length-" + length, delegate { CheckValidation(true, invalidLength, true, "YZAMH", "YZAMH.02", false); });
        }
        foreach (string empty in new[] { null, "", " " })
        {
            string missing = empty;
            Test("missing-lot-" + (_passed + _failed), delegate { CheckValidation(true, 5, true, missing, "YZAMH.02", false); });
            Test("missing-barcode-" + (_passed + _failed), delegate { CheckValidation(true, 5, true, "YZAMH", missing, false); });
        }
        Test("short-lot", delegate { CheckValidation(true, 5, true, "YZA", "YZAMH.02", false); });
        Test("short-barcode", delegate { CheckValidation(true, 5, true, "YZAMH", "YZA", false); });
        Test("new-config-defaults", delegate { CheckConfig(new InputStageConfig(), false, 5); });
        Test("legacy-missing-keys", delegate { CheckConfig(Read("{}"), false, 5); });
        Test("missing-length-uses-five", delegate { CheckConfig(Read("{\"UseBarcodeLotPrefixCheck\":true}"), true, 5); });
        Test("missing-enabled-stays-off", delegate { CheckConfig(Read("{\"BarcodeLotPrefixLength\":3}"), false, 3); });
        Test("explicit-zero-is-preserved-and-rejected", delegate
        {
            InputStageConfig recipe = Read("{\"UseBarcodeLotPrefixCheck\":true,\"BarcodeLotPrefixLength\":0}");
            CheckConfig(recipe, true, 0);
            CheckValidation(recipe.UseBarcodeLotPrefixCheck, recipe.BarcodeLotPrefixLength, true, "YZAMH", "YZAMH.02", false);
        });
        Test("explicit-negative-is-preserved", delegate { CheckConfig(Read("{\"UseBarcodeLotPrefixCheck\":true,\"BarcodeLotPrefixLength\":-1}"), true, -1); });
        Test("disabled-invalid-length-is-preserved", delegate
        {
            InputStageConfig recipe = Read("{\"UseBarcodeLotPrefixCheck\":false,\"BarcodeLotPrefixLength\":129}");
            CheckConfig(recipe, false, 129);
            CheckValidation(recipe.UseBarcodeLotPrefixCheck, recipe.BarcodeLotPrefixLength, false, null, null, true);
        });
        Test("common-config-roundtrip", delegate
        {
            InputStageConfig rke = RoundTrip(new InputStageConfig { UseBarcodeLotPrefixCheck = true, BarcodeLotPrefixLength = 5 });
            InputStageConfig other = RoundTrip(new InputStageConfig { UseBarcodeLotPrefixCheck = false, BarcodeLotPrefixLength = 3 });
            CheckConfig(rke, true, 5);
            CheckConfig(other, false, 3);
            Check(rke.AlignVisionTimeoutMs > 0, "Existing config defaults were lost.");
        });
        Test("recipe-switch-does-not-own-common-policy", delegate
        {
            InputStageConfig config = new InputStageConfig { UseBarcodeLotPrefixCheck = true, BarcodeLotPrefixLength = 3 };
            InputStageRecipe recipe = new InputStageRecipe();
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("{}")))
                recipe = (InputStageRecipe)new DataContractJsonSerializer(typeof(InputStageRecipe)).ReadObject(stream);
            Check(recipe.WaferY != null && recipe.DieMap != null, "Existing recipe restoration changed.");
            Check(typeof(InputStageRecipe).GetProperty("UseBarcodeLotPrefixCheck") == null &&
                typeof(InputStageRecipe).GetProperty("BarcodeLotPrefixLength") == null, "Policy still belongs to Recipe.");
            CheckConfig(config, true, 3);
        });
        if (args.Length == 1)
        {
            Test("existing-common-config-readonly-defaults", delegate
            {
                byte[] before = File.ReadAllBytes(args[0]);
                byte[] beforeHash = Hash(before);
                InputStageConfig recipe;
                using (var stream = new MemoryStream(before))
                    recipe = (InputStageConfig)new DataContractJsonSerializer(typeof(InputStageConfig)).ReadObject(stream);
                CheckConfig(recipe, false, 5);
                Check(recipe.SequenceMoveTimeoutMs > 0, "Existing common config was not read.");
                Check(Convert.ToBase64String(beforeHash) == Convert.ToBase64String(Hash(File.ReadAllBytes(args[0]))), "Config original bytes changed.");
            });
        }
        Console.WriteLine("SUMMARY passed=" + _passed + ", failed=" + _failed + ", realConfig=" + (args.Length == 1 ? "1" : "SKIPPED"));
        return _failed == 0 ? 0 : 1;
    }

    private static void CheckValidation(bool enabled, int length, bool barcodeEnabled, string lot, string barcode, bool expected)
    {
        string reason;
        bool actual = InputWaferBarcodePolicy.TryValidate(enabled, length, barcodeEnabled, lot, barcode, out reason);
        Check(actual == expected, "Unexpected validation result: " + reason);
        Check(actual ? reason == string.Empty : !string.IsNullOrWhiteSpace(reason), "Unexpected reason state.");
    }

    private static void CheckConfig(InputStageConfig recipe, bool enabled, int length)
    {
        Check(recipe.UseBarcodeLotPrefixCheck == enabled, "Enabled value changed.");
        Check(recipe.BarcodeLotPrefixLength == length, "Prefix length changed.");
    }

    private static InputStageConfig Read(string json)
    {
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            return (InputStageConfig)new DataContractJsonSerializer(typeof(InputStageConfig)).ReadObject(stream);
    }

    private static InputStageConfig RoundTrip(InputStageConfig recipe)
    {
        var serializer = new DataContractJsonSerializer(typeof(InputStageConfig));
        using (var stream = new MemoryStream())
        {
            serializer.WriteObject(stream, recipe);
            stream.Position = 0;
            return (InputStageConfig)serializer.ReadObject(stream);
        }
    }

    private static byte[] Hash(byte[] bytes)
    {
        using (SHA256 hash = SHA256.Create()) return hash.ComputeHash(bytes);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Test(string name, Action action)
    {
        try { action(); _passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { _failed++; Console.Error.WriteLine("FAIL " + name + ": " + ex.Message); }
    }
}
