using System;
using System.Linq;
using System.Threading;
using QMC.CDT320;
using QMC.CDT320.Barcode;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;

internal static class BarcodeFlowTests
{
    private static InputStageUnit _stage;
    private static WaferMaterial _wafer;
    private static int _passed, _failed;

    public static int Main()
    {
        Test("reader-valid-commits-once", () => { Run("YZAMH.02"); CheckCommit("YZAMH.02"); });
        Test("scanner-normalization-before-validation", () => { Run("\u0002 YZ AMH.02\r\n\u0003"); CheckCommit("YZAMH.02"); });
        Test("working-first-input-can-be-corrected", () =>
        {
            Check(_wafer.State == WaferMaterialState.Working, "Expected actual post-transfer state.");
            BarcodeOperatorPromptService.Respond = (request, ct) =>
            {
                CheckUncommitted();
                Check(request.ValidationRecovery && request.CurrentBarcode == "BADXX.02" &&
                    request.LotId == "YZAMH" && request.PrefixLength == 5, "Recovery context missing.");
                return Manual("YZAMH.02");
            };
            Run("BADXX.02");
            CheckCommit("YZAMH.02");
            Check(InputWaferMapPreflightService.Candidates.SequenceEqual(new[] { "YZAMH.02" }), "Rejected prefix reached network.");
        });
        Test("bad-map-manual-correction-uses-new-key", () =>
        {
            InputWaferMapPreflightService.Validate = value => value == "YZAMH.03";
            BarcodeOperatorPromptService.Respond = (request, ct) => { CheckUncommitted(); return Manual("YZAMH.03"); };
            Run("YZAMH.02");
            CheckCommit("YZAMH.03");
            Check(InputWaferMapPreflightService.Candidates.SequenceEqual(new[] { "YZAMH.02", "YZAMH.03" }), "Map key was not changed.");
        });
        Test("repeated-invalid-manual-candidates-never-commit", () =>
        {
            BarcodeOperatorPromptService.Respond = (request, ct) =>
            {
                CheckUncommitted();
                return Manual(BarcodeOperatorPromptService.Requests.Count == 1 ? "WRONG.02" : "YZAMH.02");
            };
            Run("BADXX.02"); CheckCommit("YZAMH.02");
            Check(BarcodeOperatorPromptService.Requests.Count == 2, "Invalid manual candidate did not return to recovery.");
            Check(InputWaferMapPreflightService.Candidates.SequenceEqual(new[] { "YZAMH.02" }), "Invalid candidate reached map lookup.");
        });
        Test("retry-revalidates-same-candidate", () =>
        {
            InputWaferMapPreflightService.Validate = value => InputWaferMapPreflightService.Candidates.Count == 2;
            BarcodeOperatorPromptService.Respond = (request, ct) => { CheckUncommitted(); return new BarcodeRecoveryResponse { Decision = BarcodeRecoveryDecision.Retry }; };
            Run("YZAMH.02");
            CheckCommit("YZAMH.02");
            Check(InputWaferMapPreflightService.Candidates.SequenceEqual(new[] { "YZAMH.02", "YZAMH.02" }), "Retry changed candidate.");
        });
        Test("cancelled-prompt-never-commits", () => { Fails(() => Run("BADXX.02")); CheckUncommitted(); });
        Test("closed-null-prompt-never-commits", () =>
        {
            BarcodeOperatorPromptService.Respond = (request, ct) => null;
            Fails(() => Run("BADXX.02")); CheckUncommitted();
        });
        Test("cancel-before-entry-never-commits", () =>
        {
            using (var cancel = new CancellationTokenSource()) { cancel.Cancel(); Cancels(() => Run("YZAMH.02", true, cancel.Token)); }
            CheckUncommitted(); CheckNoPrompt();
        });
        Test("cancel-during-map-never-commits", () =>
        {
            using (var cancel = new CancellationTokenSource())
            {
                InputWaferMapPreflightService.Validate = value => { cancel.Cancel(); return true; };
                Cancels(() => Run("YZAMH.02", true, cancel.Token));
            }
            CheckUncommitted(); CheckNoPrompt();
        });
        Test("cancel-during-prompt-never-commits", () =>
        {
            using (var cancel = new CancellationTokenSource())
            {
                BarcodeOperatorPromptService.Respond = (request, ct) => { cancel.Cancel(); return Manual("YZAMH.02"); };
                Cancels(() => Run("BADXX.02", true, cancel.Token));
            }
            CheckUncommitted();
        });
        Test("confirmed-same-value-needs-no-commit", () =>
        {
            Confirm("YZAMH.02"); Run("YZAMH.02"); CheckNoCommit(); CheckNoPrompt();
            Check(InputWaferMapPreflightService.Candidates.Count == 1, "Resume skipped map validation.");
        });
        Test("confirmed-wrong-prefix-can-be-corrected-before-align", () =>
        {
            Confirm("BADXX.02");
            BarcodeOperatorPromptService.Respond = (request, ct) => { CheckNoCommit(); Check(_wafer.WaferId == "BADXX.02", "Premature correction."); return Manual("YZAMH.02"); };
            Run("BADXX.02"); CheckCommit("YZAMH.02");
        });
        Test("manual-validated-confirmed-value-is-read-only", () => { Confirm("YZAMH.02"); Run("YZAMH.02", false); CheckNoCommit(); CheckNoPrompt(); });
        Test("manual-unconfirmed-value-blocked-without-dialog", () => { Fails(() => Run("YZAMH.02", false)); CheckUncommitted(); CheckNoPrompt(); });
        Test("manual-bad-prefix-blocked-without-dialog", () => { Confirm("BADXX.02"); Fails(() => Run("BADXX.02", false)); CheckNoCommit(); CheckNoPrompt(); });
        Test("manual-bad-map-blocked-without-dialog", () =>
        {
            Confirm("YZAMH.02"); InputWaferMapPreflightService.Validate = value => false;
            Fails(() => Run("YZAMH.02", false)); CheckNoCommit(); CheckNoPrompt();
        });
        Test("aligned-correction-blocked", () => { _wafer.HasInputStageAlignResult = true; Fails(() => Run("BADXX.02")); CheckUncommitted(); CheckNoPrompt(); });
        Test("theta-correction-blocked", () => { _wafer.HasInputStageThetaAlignResult = true; Fails(() => Run("YZAMH.02")); CheckUncommitted(); CheckNoPrompt(); });
        Test("mapped-correction-blocked", () => { _wafer.HasInputStageDieMappingResult = true; Fails(() => Run("YZAMH.02")); CheckUncommitted(); CheckNoPrompt(); });
        Test("review-approved-correction-blocked", () => { _wafer.HasInputStageRunReviewApproval = true; Fails(() => Run("YZAMH.02")); CheckUncommitted(); CheckNoPrompt(); });
        Test("finished-correction-blocked", () => { _wafer.State = WaferMaterialState.Finish; Fails(() => Run("YZAMH.02")); CheckUncommitted(); CheckNoPrompt(); });
        Test("reserved-die-correction-blocked", () =>
        {
            MaterialStateService.State.Dies.Add(new DieMaterial { ReservedPickerLocation = MaterialLocationKind.InputPicker });
            Fails(() => Run("YZAMH.02")); CheckUncommitted(); CheckNoPrompt();
        });
        Test("picked-die-correction-blocked", () =>
        {
            MaterialStateService.State.Dies.Add(new DieMaterial { PickedAt = DateTime.UtcNow });
            Fails(() => Run("YZAMH.02")); CheckUncommitted(); CheckNoPrompt();
        });
        Test("other-wafer-picked-die-does-not-block", () =>
        {
            MaterialStateService.State.Dies.Add(new DieMaterial { InputWaferInstanceId = "other", PickedAt = DateTime.UtcNow });
            Run("YZAMH.02"); CheckCommit("YZAMH.02");
        });
        Test("saved-align-motion-blocks-correction-without-result-flags", () =>
        {
            SequenceResumeStore.AlignStep = "MoveVisionProcessPosition";
            Fails(() => Run("BADXX.02")); CheckUncommitted(); CheckNoPrompt();
        });
        Test("saved-checkunit-allows-first-input", () => { SequenceResumeStore.AlignStep = "CheckUnit"; Run("YZAMH.02"); CheckCommit("YZAMH.02"); });
        Test("saved-idle-allows-first-input", () => { SequenceResumeStore.AlignStep = "Idle"; Run("YZAMH.02"); CheckCommit("YZAMH.02"); });
        Test("after-align-confirmed-same-value-revalidation-is-read-only", () =>
        {
            Confirm("YZAMH.02"); _wafer.HasInputStageAlignResult = true;
            Run("YZAMH.02"); CheckNoCommit(); CheckNoPrompt();
        });
        Test("lot-change-during-map-blocked", () => MapMutation(() => MaterialStateService.LotId = "OTHER"));
        Test("config-value-change-during-map-blocked", () => MapMutation(() => _stage.Config.BarcodeLotPrefixLength = 3));
        Test("config-reference-change-during-map-blocked", () => MapMutation(() => _stage.Config = new InputStageConfig()));
        Test("recipe-change-during-map-blocked", () => MapMutation(() => _stage.Recipe = new InputStageRecipe()));
        Test("network-setting-change-during-map-blocked", () => MapMutation(() => AppSettingsStore.Current.NetworkWaferMapFolder = "changed"));
        Test("app-settings-reference-change-during-map-blocked", () => MapMutation(() => AppSettingsStore.Current = new AppSettings()));
        Test("lot-bin-change-during-map-blocked", () => MapMutation(() => MaterialStateService.BinSelection = "2"));
        Test("recipe-bin-change-during-map-blocked", () => MapMutation(() => _stage.Recipe.DieMap.PickupBinFilterCsv = "2"));
        Test("instance-change-during-map-blocked", () => MapMutation(() => MaterialStateService.Wafer = new WaferMaterial { WaferInstanceId = "physical-2" }));
        Test("lot-change-during-prompt-blocked", () => PromptMutation(() => MaterialStateService.LotId = "OTHER"));
        Test("config-change-during-prompt-blocked", () => PromptMutation(() => _stage.Config.UseBarcodeLotPrefixCheck = false));
        Test("instance-change-during-prompt-blocked", () => PromptMutation(() => MaterialStateService.Wafer = new WaferMaterial { WaferInstanceId = "physical-2" }));
        Test("barcode-off-prefix-on-blocked", () => { AppSettingsStore.Current.UseInputWaferBarcode = false; Fails(() => Run("YZAMH.02", false)); CheckUncommitted(); CheckNoPrompt(); });
        Test("barcode-off-network-on-prefix-off-blocked", () =>
        {
            AppSettingsStore.Current.UseInputWaferBarcode = false; _stage.Config.UseBarcodeLotPrefixCheck = false;
            Fails(() => Run("YZAMH.02", false)); CheckUncommitted(); CheckNoPrompt();
        });
        Test("network-off-prefix-on-still-validates", () =>
        {
            AppSettingsStore.Current.UseLotNetworkWaferMap = false;
            InputWaferMapPreflightService.Validate = value => { throw new Exception("Network was accessed."); };
            BarcodeOperatorPromptService.Respond = (request, ct) => { CheckUncommitted(); return Manual("YZAMH.02"); };
            Run("OTHER.02"); CheckCommit("YZAMH.02");
        });
        Test("network-on-prefix-off-still-checks-map", () =>
        {
            _stage.Config.UseBarcodeLotPrefixCheck = false;
            InputWaferMapPreflightService.Validate = value => false;
            Fails(() => Run("OTHER.02")); CheckUncommitted();
            Check(InputWaferMapPreflightService.Candidates.Count == 1, "Map validation skipped.");
        });
        Test("network-and-prefix-off-preserve-barcode-only-mode", () =>
        {
            AppSettingsStore.Current.UseLotNetworkWaferMap = false; _stage.Config.UseBarcodeLotPrefixCheck = false;
            MaterialStateService.LotId = ""; Run("OTHER.02"); CheckCommit("OTHER.02");
        });
        Test("same-lot-other-wafer-is-not-new-policy-rejection", () => { Run("YZAMH.03"); CheckCommit("YZAMH.03"); });
        Test("storage-rejection-propagates", () => { MaterialStateService.RejectApply = true; Fails(() => Run("YZAMH.02")); CheckUncommitted(); CheckNoPrompt(); });
        Console.WriteLine("BARCODE FLOW: PASS " + _passed + "/" + (_passed + _failed) + ", failed=" + _failed);
        return _failed == 0 ? 0 : 1;
    }

    private static void Test(string name, Action action)
    {
        Reset();
        try { action(); _passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { _failed++; Console.WriteLine("FAIL " + name + ": " + ex); }
    }
    private static void Reset()
    {
        _wafer = new WaferMaterial(); _stage = new InputStageUnit { CurrentWaferMaterial = _wafer };
        AppSettingsStore.Current = new AppSettings(); MaterialStateService.Wafer = _wafer;
        MaterialStateService.State = new MaterialState(); MaterialStateService.LotId = "YZAMH";
        MaterialStateService.BinSelection = "1"; MaterialStateService.Commits.Clear();
        MaterialStateService.RejectApply = false; MaterialStateService.BeforeCommit = null;
        InputWaferMapPreflightService.Candidates.Clear(); InputWaferMapPreflightService.Validate = null;
        BarcodeOperatorPromptService.Requests.Clear(); BarcodeOperatorPromptService.Respond = null;
        SequenceResumeStore.AlignStep = null;
    }
    private static void Run(string candidate, bool recovery = true, CancellationToken ct = default(CancellationToken))
    {
        InputFeederLoadToStageSequence.ValidateAndApplyInputBarcodeAsync(_stage, _wafer, candidate, "test-reader", 1, ct, recovery).GetAwaiter().GetResult();
    }
    private static void Confirm(string value) { _wafer.WaferId = value; _wafer.BarcodeId = value; _wafer.BarcodeConfirmed = true; }
    private static BarcodeRecoveryResponse Manual(string value) { return new BarcodeRecoveryResponse { Decision = BarcodeRecoveryDecision.ManualApply, ManualBarcode = value }; }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void CheckNoCommit() { Check(MaterialStateService.Commits.Count == 0, "A rejected candidate was committed."); }
    private static void CheckNoPrompt() { Check(BarcodeOperatorPromptService.Requests.Count == 0, "Unexpected recovery dialog."); }
    private static void CheckUncommitted() { CheckNoCommit(); Check(_wafer.WaferId == "temporary-id" && !_wafer.BarcodeConfirmed && _wafer.BarcodeId == "", "Material was mutated before validation."); }
    private static void CheckCommit(string value) { Check(MaterialStateService.Commits.SequenceEqual(new[] { value }), "Expected exactly one final candidate commit."); Check(_wafer.BarcodeConfirmed && _wafer.WaferId == value && _wafer.BarcodeId == value, "Final identity incorrect."); }
    private static void Fails(Action run) { try { run(); } catch (InvalidOperationException) { return; } throw new Exception("Expected validation failure."); }
    private static void Cancels(Action run) { try { run(); } catch (OperationCanceledException) { return; } throw new Exception("Expected cancellation."); }
    private static void MapMutation(Action mutate)
    {
        InputWaferMapPreflightService.Validate = value => { mutate(); return true; };
        Fails(() => Run("YZAMH.02")); CheckUncommitted(); CheckNoPrompt();
    }
    private static void PromptMutation(Action mutate)
    {
        BarcodeOperatorPromptService.Respond = (request, ct) => { mutate(); return Manual("YZAMH.02"); };
        Fails(() => Run("BADXX.02")); CheckUncommitted();
    }
}
