using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.CDT320.Materials;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Sequencing;

// Compile the exact production async barcode/map coordinator. Device, preflight parser and
// persistence boundaries are substituted. Actual parser/transform behavior is tested separately.
internal static class VerifyBarcodeMapGate
{
    private static int checks;
    internal static bool MapValid, ContextChanges;
    internal static int Prompts, Preflights, Commits, Pins, ContextReads;
    private static void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
    private static int Main()
    {
        try
        {
            Run("registered ignores remote parser", false, false, false, true, true, null, 0, 1, 0);
            Run("remote valid newly read", true, true, false, true, true, null, 0, 1, 1);
            Run("remote valid confirmed", true, true, true, true, false, null, 0, 0, 1);
            Run("remote mismatch triggers caller alarm", true, false, false, true, true, typeof(InvalidDataException), 0, 0, 0);
            Run("remote mismatch confirmed triggers caller alarm", true, false, true, true, true, typeof(InvalidDataException), 0, 0, 0);
            Run("manual align remote mismatch", true, false, true, true, false, typeof(InvalidDataException), 0, 0, 0);
            Run("barcode failure keeps existing recovery", true, false, false, false, true, typeof(InvalidOperationException), 1, 0, 0);
            Run("manual barcode failure has no recovery", true, false, false, false, false, typeof(InvalidOperationException), 0, 0, 0);
            Run("context changed after preflight", true, true, false, true, true, typeof(InvalidOperationException), 0, 0, 0, true);
            Console.WriteLine("PASS: " + checks + " barcode/map failure gate assertions; actual async method, no equipment runtime.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Run(string name, bool network, bool mapValid, bool confirmed, bool barcodeValid, bool recovery,
        Type errorType, int prompts, int commits, int pins, bool contextChanges = false)
    {
        MapValid = mapValid; ContextChanges = contextChanges;
        Prompts = Preflights = Commits = Pins = ContextReads = 0;
        AppSettingsStore.Current = new AppSettings { UseLotNetworkWaferMap = network };
        var wafer = new WaferMaterial { WaferInstanceId = "INSTANCE", WaferId = confirmed ? "1234.02" : "SLOT-1",
            BarcodeId = confirmed ? "1234.02" : "", BarcodeConfirmed = confirmed };
        MaterialStateService.Current = wafer;
        var stage = new InputStageUnit();
        Exception failure = null;
        try { InputFeederLoadToStageSequence.ValidateAndApplyInputBarcodeAsync(stage, wafer, barcodeValid ? "1234.02" : "BAD", "TEST", 1, CancellationToken.None, recovery).GetAwaiter().GetResult(); }
        catch (Exception ex) { failure = ex; }
        Check(errorType == null ? failure == null : failure != null && failure.GetType() == errorType, name + " exception type: " + failure);
        Check(Prompts == prompts, name + " operator prompt count");
        Check(Commits == commits && Pins == pins, name + " no unexpected barcode commit or prepared map mutation");
        Check(Preflights == (barcodeValid ? 1 : 0), name + " barcode policy precedes map preflight");
        if (errorType == typeof(InvalidDataException))
            Check(failure.Message.Contains("원격 Input") && failure.Message.Contains("형식 불일치"), name + " reason reaches existing sequence Fail/Alarm handler");
        if (failure == null) Check(wafer.BarcodeConfirmed && wafer.BarcodeId == "1234.02", name + " accepted barcode committed");
    }
}

namespace QMC.CDT320.DieMaps { public sealed class DieMap { } }
namespace QMC.CDT320
{
    public sealed class AppSettings { public bool UseLotNetworkWaferMap; public int InputBarcodeRetryCount; public double InputBarcodeRetryStepMm; }
    public static class AppSettingsStore { public static AppSettings Current; }
    public sealed class InputStageRecipe { }
    public sealed class InputStageConfig { public bool UseBarcodeLotPrefixCheck; public int BarcodeLotPrefixLength; }
    public sealed class InputStageUnit
    {
        public InputStageRecipe Recipe = new InputStageRecipe(); public InputStageConfig Config = new InputStageConfig();
        public void SetCurrentWaferMaterial(WaferMaterial wafer) { }
    }
    public enum BarcodeReaderChannel { InputWafer }
    public enum BarcodeRecoveryDecision { Cancelled, ManualApply, Retry }
    public sealed class BarcodeRecoveryRequest
    {
        public BarcodeReaderChannel Channel; public string MaterialId, MaterialInstanceId, FailureMessage, CurrentBarcode, LotId;
        public bool ValidationRecovery; public int PrefixLength, RetryCount; public double RetryStepMm;
    }
    public sealed class BarcodeRecoveryResponse { public BarcodeRecoveryDecision Decision; public string ManualBarcode; }
    public static class BarcodeOperatorPromptService
    {
        public static Task<BarcodeRecoveryResponse> RequestAsync(BarcodeRecoveryRequest request, CancellationToken token)
        { VerifyBarcodeMapGate.Prompts++; return Task.FromResult(new BarcodeRecoveryResponse { Decision = BarcodeRecoveryDecision.Cancelled }); }
    }
}
namespace QMC.CDT320.Materials
{
    public enum MaterialLocationKind { InputStage }
    public sealed class WaferMaterial { public string WaferId, WaferInstanceId, BarcodeId; public bool BarcodeConfirmed; }
    public static class MaterialStateService
    {
        public static WaferMaterial Current;
        public static string GetProductionLotId() { return "LOT"; }
        public static WaferMaterial GetWaferAtLocation(MaterialLocationKind kind) { return Current; }
        public static void PinPreparedInputMap(WaferMaterial wafer, string barcode, bool network, DieMap map) { VerifyBarcodeMapGate.Pins++; }
        public static bool TryApplyWaferBarcode(string instance, MaterialLocationKind location, string barcode, string source, int attempts, out string previous, out string reason)
        { VerifyBarcodeMapGate.Commits++; previous = Current.WaferId; reason = ""; Current.WaferId = Current.BarcodeId = barcode; Current.BarcodeConfirmed = true; return true; }
    }
}
namespace QMC.CDT320.Lots
{
    public static class InputWaferMapPreflightService
    {
        public static bool TryValidate(string barcode, InputStageUnit stage, out DieMap prepared, out string reason)
        {
            VerifyBarcodeMapGate.Preflights++; prepared = null; reason = "";
            if (!AppSettingsStore.Current.UseLotNetworkWaferMap) return true;
            if (!VerifyBarcodeMapGate.MapValid) { reason = "형식 불일치: Samsung / CAMTEK"; return false; }
            prepared = new DieMap(); return true;
        }
    }
}
namespace QMC.CDT320.Sequencing
{
    internal static partial class InputFeederLoadToStageSequence
    {
        private static string NormalizeBarcode(string value) { return value.Trim(); }
        private static string BuildBarcodeValidationContext(InputStageUnit stage, WaferMaterial wafer) { return "CONTEXT"; }
        private static WaferMaterial RequireUnchangedBarcodeValidationContext(InputStageUnit stage, InputStageRecipe recipe, InputStageConfig config, AppSettings settings,
            string context, string instance, string waferId, string barcode, bool confirmed)
        {
            VerifyBarcodeMapGate.ContextReads++;
            if (VerifyBarcodeMapGate.ContextChanges && VerifyBarcodeMapGate.ContextReads > 1) throw new InvalidOperationException("Context changed");
            return MaterialStateService.Current;
        }
        private static bool TryValidateInputBarcodePolicy(InputStageUnit stage, string barcode, out string reason) { reason = barcode == "BAD" ? "Invalid barcode" : ""; return reason.Length == 0; }
        private static bool HasInputBarcodeProcessingStarted(WaferMaterial wafer) { return false; }
    }
}
namespace QMC.Common
{
    public enum LogLevel { AboveNormal }
    public static class Log { public static void Write(LogLevel level, params string[] values) { } }
}
namespace QMC.Common.Logging
{
    public enum EventKind { Warning }
    public static class EventLogger { public static void Write(EventKind kind, params string[] values) { } }
}

// Mode persistence is exercised with the actual store in the integration harness.
namespace QMC.CDT320.Recipes
{
    public static class RecipeInputMapSource
    { public static bool UsesRemoteForActiveRecipe(QMC.CDT320.AppSettings settings) { return settings.UseLotNetworkWaferMap; } }
}
