using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;

internal static class InputStageReviewConfirmationTests
{
    private static readonly MethodInfo Check = typeof(MaterialStateService).GetMethod(
        "CheckInputStageReviewConfirmationContext", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo AppendJson = typeof(MaterialStateService).GetMethod(
        "AppendInputStageReviewDiagnosticValue", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo ProvenanceCheck = typeof(MaterialStateService).GetMethod(
        "CheckInputStageReviewMappingProvenance", BindingFlags.Static | BindingFlags.NonPublic);
    private static int _checks;

    private sealed class Fixture
    {
        internal string Token = "CONFIRM-CONTEXT-PRODUCTION-OFFLINE";
        internal InputStageReviewGeometryContext Saved = new InputStageReviewGeometryContext
        {
            WaferId = "OFFLINE-WAFER", MappingRevision = "MAP-A", ConditionSignature = "CONDITION-A",
            CandidateSignature = "COORDINATES-A", SessionGeneration = 1, RequestGeneration = 2,
            StageTheta = -90, PitchX = 8.12, PitchY = 6.12, OriginX = 10, OriginY = 20,
            BaselineOriginX = 10, BaselineOriginY = 20, IsSimulation = false
        };
        internal InputStageReviewGeometryContext Current;
        internal IList<InputStageReviewMeasurement> Measurements = new List<InputStageReviewMeasurement>();
        internal Fixture() { Current = Saved.Clone(); }
    }

    private static void Expect(string label, bool expected, Action<Fixture> arrange = null)
    {
        var data = new Fixture();
        if (arrange != null) arrange(data);
        object[] arguments = { data.Token, data.Saved, data.Current, data.Measurements, null };
        bool actual = (bool)Check.Invoke(null, arguments);
        if (actual != expected)
            throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ", reason=" + arguments[4]);
        _checks++;
    }

    private sealed class ProvenanceFixture
    {
        internal WaferMaterial Wafer = new WaferMaterial { InputMapApprovalHashAtMapping = "APPROVED-A" };
        internal RecipeProject Recipe = new RecipeProject
        { FileName = "JMB_Rework", MapApprovalVersion = 1, InputMapApprovalHash = "APPROVED-A" };
        internal string MaterialRecipe = "JMB_Rework";
        internal bool NonProduction;
    }

    private static void ExpectProvenance(string label, bool expected, Action<ProvenanceFixture> arrange = null)
    {
        var data = new ProvenanceFixture();
        if (arrange != null) arrange(data);
        object[] arguments = { data.Wafer, data.Recipe, data.MaterialRecipe, data.NonProduction, null };
        bool actual = (bool)ProvenanceCheck.Invoke(null, arguments);
        if (actual != expected)
            throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ", reason=" + arguments[4]);
        _checks++;
    }

    private static void CheckJson()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            var json = new StringBuilder("{");
            AppendJson.Invoke(null, new object[] { json, "exact", 0.12345678901234566 });
            AppendJson.Invoke(null, new object[] { json, "unknown", null });
            AppendJson.Invoke(null, new object[] { json, "nonFinite", double.NaN });
            AppendJson.Invoke(null, new object[] { json, "text", "레시피\"\\\r\n\t\u0001" });
            AppendJson.Invoke(null, new object[] { json, "measured", false });
            json.Append('}');
            string result = json.ToString();
            if (!result.Contains("\"exact\":" + 0.12345678901234566.ToString("R", CultureInfo.InvariantCulture)) ||
                !result.Contains("\"unknown\":null") || !result.Contains("\"nonFinite\":\"NaN\"") ||
                !result.Contains("\"measured\":false") || result.Contains("\r") || result.Contains("\n"))
                throw new InvalidOperationException("Diagnostic JSON did not preserve invariant values/null/non-finite markers: " + result);
            using (XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(
                Encoding.UTF8.GetBytes(result), XmlDictionaryReaderQuotas.Max))
                while (reader.Read()) { }
            _checks++;
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    private static int Main()
    {
        if (Check == null || AppendJson == null || ProvenanceCheck == null)
            throw new MissingMethodException("Current handler confirmation/diagnostic helpers are missing.");
        Expect("Production confirmation succeeds with zero physical samples", true);
        Expect("Nonproduction confirmation succeeds with zero physical samples", true, f =>
        {
            f.Token = "CONFIRM-CONTEXT-NONPRODUCTION-OFFLINE";
            f.Saved.IsSimulation = f.Current.IsSimulation = true;
        });
        Expect("Confirmation never accepts a fabricated physical sample", false, f => f.Measurements.Add(new InputStageReviewMeasurement()));
        Expect("Null sample metadata is not accepted", false, f => f.Measurements = null);
        Expect("Legacy verification token is distinct", false, f => f.Token = "OLD-THREE-POINT-TOKEN");
        Expect("Legacy nonproduction token is distinct", false, f => f.Token = "NONPRODUCTION-MANUAL-OFFLINE");
        Expect("Simulation default token is distinct", false, f => f.Token = "SIMULATION-DEFAULT-OFFLINE");
        Expect("Missing token", false, f => f.Token = null);
        Expect("Empty token suffix", false, f => f.Token = "CONFIRM-CONTEXT-PRODUCTION-");
        Expect("Production token cannot approve simulation", false, f => f.Saved.IsSimulation = f.Current.IsSimulation = true);
        Expect("Simulation token cannot approve production", false, f => f.Token = "CONFIRM-CONTEXT-NONPRODUCTION-OFFLINE");
        Expect("Mode change", false, f => f.Current.IsSimulation = true);
        Expect("Missing saved context", false, f => f.Saved = null);
        Expect("Missing current context", false, f => f.Current = null);
        Expect("Recipe/calibration/runtime condition changed", false, f => f.Current.ConditionSignature = "CONDITION-B");
        Expect("Physical wafer changed", false, f => f.Current.WaferId = "OTHER-WAFER");
        Expect("Mapping changed", false, f => f.Current.MappingRevision = "MAP-B");
        Expect("Any die coordinate changed", false, f => f.Current.CandidateSignature = "COORDINATES-B");
        Expect("Session changed", false, f => f.Current.SessionGeneration++);
        Expect("Request changed", false, f => f.Current.RequestGeneration++);
        Expect("Origin changed", false, f => f.Current.OriginX += 0.001);
        Expect("Baseline changed", false, f => f.Current.BaselineOriginY += 0.001);
        Expect("Pitch changed", false, f => f.Current.PitchY += 0.001);
        Expect("Stored T changed", false, f => f.Current.StageTheta += 0.001);
        Expect("Unknown condition", false, f => f.Saved.ConditionSignature = f.Current.ConditionSignature = "");
        Expect("Unknown session", false, f => f.Saved.SessionGeneration = f.Current.SessionGeneration = 0);
        Expect("NaN coordinate", false, f => f.Saved.OriginX = f.Current.OriginX = double.NaN);
        Expect("Invalid pitch", false, f => f.Saved.PitchX = f.Current.PitchX = 0);
        ExpectProvenance("Current recipe and mapped approval match", true);
        ExpectProvenance("Hash comparison preserves existing case-insensitive contract", true, f => f.Wafer.InputMapApprovalHashAtMapping = "approved-a");
        ExpectProvenance("Old map under new approved recipe cannot be confirmed", false, f => f.Recipe.InputMapApprovalHash = "APPROVED-B");
        ExpectProvenance("Versioned recipe needs mapping approval metadata", false, f => f.Wafer.InputMapApprovalHashAtMapping = "");
        ExpectProvenance("Missing current approved hash", false, f => f.Recipe.InputMapApprovalHash = "");
        ExpectProvenance("Version zero legacy map contract remains usable", true, f =>
        { f.Recipe.MapApprovalVersion = 0; f.Recipe.InputMapApprovalHash = ""; f.Wafer.InputMapApprovalHashAtMapping = ""; });
        ExpectProvenance("Known different material recipe is rejected", false, f => f.MaterialRecipe = "RAD_Rework");
        ExpectProvenance("Project extension, surrounding whitespace and case normalize", true, f => f.MaterialRecipe = "  jmb_rework.PROJECT  ");
        ExpectProvenance("Blank legacy material recipe remains compatible", true, f => f.MaterialRecipe = "");
        ExpectProvenance("Unknown current recipe rejected", false, f => f.Recipe.FileName = "");
        ExpectProvenance("Missing current recipe object rejected", false, f => f.Recipe = null);
        ExpectProvenance("Missing wafer rejected", false, f => f.Wafer = null);
        ExpectProvenance("Saved nonproduction map cannot be relabeled as production", false, f =>
        { f.Wafer.InputStageReviewVerification = new InputStageReviewSavedVerification { Context = new InputStageReviewGeometryContext { IsSimulation = true } }; });
        ExpectProvenance("Nonproduction re-confirmation remains allowed", true, f =>
        { f.NonProduction = true; f.Wafer.InputStageReviewVerification = new InputStageReviewSavedVerification { Context = new InputStageReviewGeometryContext { IsSimulation = true } }; });
        ExpectProvenance("Saved production context remains production", true, f =>
        { f.Wafer.InputStageReviewVerification = new InputStageReviewSavedVerification { Context = new InputStageReviewGeometryContext { IsSimulation = false } }; });
        CheckJson();
        Console.WriteLine("PASS: {0} confirmation context and diagnostic JSON checks; no hardware/application startup.", _checks);
        return 0;
    }
}
