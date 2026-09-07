using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Hardware, persistence, UI, and network boundaries are in-memory substitutes.
// The validation/recovery methods themselves are extracted from production source.
namespace QMC.CDT320
{
    public sealed class AppSettings
    {
        public bool UseInputWaferBarcode = true;
        public bool UseLotNetworkWaferMap = true;
        public string NetworkWaferMapFolder = "memory-map";
        public string NetworkWaferMapFormat = "RAD";
        public int InputBarcodeRetryCount = 3;
        public double InputBarcodeRetryStepMm = 1;
    }
    public static class AppSettingsStore { public static AppSettings Current; }
    public sealed class InputStageConfig
    {
        public bool UseBarcodeLotPrefixCheck = true;
        public int BarcodeLotPrefixLength = 5;
    }
    public sealed class InputStageRecipe { public DieRecipe DieMap = new DieRecipe(); }
    public sealed class DieRecipe { public string PickupBinFilterCsv = "1"; }
    public sealed class InputStageUnit
    {
        public InputStageConfig Config = new InputStageConfig();
        public InputStageRecipe Recipe = new InputStageRecipe();
        public Materials.WaferMaterial CurrentWaferMaterial;
        public void SetCurrentWaferMaterial(Materials.WaferMaterial wafer) { CurrentWaferMaterial = wafer; }
    }
}

namespace QMC.CDT320.Materials
{
    public enum MaterialLocationKind { Unknown, InputStage, InputPicker }
    public enum WaferMaterialState { Ready, Working, Finish }
    public sealed class WaferMaterial
    {
        public string WaferInstanceId = "physical-1";
        public string WaferId = "temporary-id";
        public string BarcodeId = "";
        public bool BarcodeConfirmed;
        public string TapeFrameSpecName = "RKE";
        public WaferMaterialState State = WaferMaterialState.Working;
        public bool HasInputStageAlignResult, HasInputStageThetaAlignResult;
        public bool HasInputStageDieMappingResult, HasInputStageRunReviewApproval;
    }
    public sealed class DieMaterial
    {
        public string InputWaferInstanceId = "physical-1";
        public MaterialLocationKind ReservedPickerLocation, PickedPickerLocation;
        public DateTime PickedAt;
    }
    public sealed class MaterialState { public List<DieMaterial> Dies = new List<DieMaterial>(); }
    public static class MaterialStateService
    {
        public static WaferMaterial Wafer;
        public static MaterialState State;
        public static string LotId, BinSelection;
        public static List<string> Commits = new List<string>();
        public static Action BeforeCommit;
        public static bool RejectApply;
        public static WaferMaterial GetWaferAtLocation(MaterialLocationKind location) { return Wafer; }
        public static string GetProductionLotId() { return LotId; }
        public static string DescribePickupBinSelection() { return BinSelection; }
        public static bool IsSameWaferInstance(WaferMaterial left, WaferMaterial right)
        {
            return left != null && right != null &&
                string.Equals(left.WaferInstanceId, right.WaferInstanceId, StringComparison.OrdinalIgnoreCase);
        }
        public static T ReadState<T>(Func<MaterialState, T> read) { return read(State); }
        public static bool TryApplyWaferBarcode(string instance, MaterialLocationKind location, string barcode,
            string source, int attempts, out string previousId, out string reason)
        {
            previousId = Wafer.WaferId;
            reason = RejectApply ? "simulated storage rejection" : "";
            if (RejectApply) return false;
            if (BeforeCommit != null) BeforeCommit();
            if (location != MaterialLocationKind.InputStage || Wafer.WaferInstanceId != instance)
                throw new InvalidOperationException("Invalid commit destination.");
            Commits.Add(barcode);
            Wafer.WaferId = barcode;
            Wafer.BarcodeId = barcode;
            Wafer.BarcodeConfirmed = true;
            return true;
        }
    }
}

namespace QMC.CDT320.Lots
{
    public static class InputWaferMapPreflightService
    {
        public static List<string> Candidates = new List<string>();
        public static Func<string, bool> Validate;
        public static bool TryValidate(string candidate, InputStageUnit stage, out string reason)
        {
            Candidates.Add(candidate);
            bool valid = !AppSettingsStore.Current.UseLotNetworkWaferMap || Validate == null || Validate(candidate);
            reason = valid ? "" : "simulated map failure";
            return valid;
        }
    }
}

namespace QMC.CDT320.Barcode
{
    public enum BarcodeReaderChannel { InputWafer, OutputBin }
    public enum BarcodeRecoveryDecision { Retry, ManualApply, Cancelled }
    public sealed class BarcodeRecoveryRequest
    {
        public BarcodeReaderChannel Channel;
        public string MaterialId, MaterialInstanceId, FailureMessage, CurrentBarcode, LotId;
        public bool ValidationRecovery;
        public int PrefixLength, RetryCount;
        public double RetryStepMm;
    }
    public sealed class BarcodeRecoveryResponse
    {
        public BarcodeRecoveryDecision Decision;
        public string ManualBarcode;
    }
    public static class BarcodeOperatorPromptService
    {
        public static List<BarcodeRecoveryRequest> Requests = new List<BarcodeRecoveryRequest>();
        public static Func<BarcodeRecoveryRequest, CancellationToken, BarcodeRecoveryResponse> Respond;
        public static Task<BarcodeRecoveryResponse> RequestAsync(BarcodeRecoveryRequest request, CancellationToken ct)
        {
            Requests.Add(request);
            if (Requests.Count > 5) throw new InvalidOperationException("Unexpected automatic recovery loop.");
            return Task.FromResult(Respond == null ?
                new BarcodeRecoveryResponse { Decision = BarcodeRecoveryDecision.Cancelled } : Respond(request, ct));
        }
    }
}

namespace QMC.CDT320.Sequencing
{
    public static class SequenceResumeStore
    {
        public static string AlignStep;
        public static string ResolveStartStep(string key, string fallback) { return AlignStep ?? fallback; }
    }
}
namespace QMC.Common
{
    public enum LogLevel { AboveNormal }
    public static class Log { public static void Write(LogLevel level, string channel, string source, string text) { } }
}
namespace QMC.Common.Logging
{
    public enum EventKind { Warning }
    public static class EventLogger { public static void Write(EventKind kind, string user, string source, string text) { } }
}
