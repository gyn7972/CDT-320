using System;
using System.Reflection;
using System.Windows.Forms;
using QMC.CDT320.Barcode;
using QMC.CDT_320.Ui.Dialogs;

internal static class DialogRegressionTests
{
    private static int _passed;
    private static int _failed;

    [STAThread]
    private static int Main()
    {
        Test("validation-context-and-actions", delegate
        {
            using (var dialog = NewDialog(true))
            {
                string text = Field<TextBox>(dialog, "txtFailure").Text;
                Check(text.Contains("YZAMH") && text.Contains("YZAXH.02") && text.Contains("5글자"), "Missing validation context.");
                Check(Field<TextBox>(dialog, "txtManualBarcode").Text == "YZAXH.02", "Candidate was not prefilled.");
                Check(Field<Button>(dialog, "btnCloseRetry").Text == "작업 중단", "Validation close is not a stop.");
                Check(Field<Button>(dialog, "btnRetry").Text == "다시 확인", "Validation retry suggests scanning.");
                Check(Field<TableLayoutPanel>(dialog, "retryLayout").GetColumnSpan(Field<Button>(dialog, "btnRetry")) == 5,
                    "Validation retry layout still exposes motion retry controls.");
                Check(Field<TextBox>(dialog, "txtFailure").ScrollBars == ScrollBars.Vertical, "Long failure detail cannot scroll.");
            }
        });
        Test("validation-stop-button-cancels", delegate
        {
            using (var dialog = NewDialog(true))
            {
                int cancelled = 0, retries = 0;
                dialog.CancelRequested += () => cancelled++;
                dialog.RetryRequested += (count, step) => retries++;
                Invoke(dialog, "btnCloseRetry_Click", dialog, EventArgs.Empty);
                Check(cancelled == 1 && retries == 0, "Stop requested a retry.");
            }
        });
        Test("validation-window-close-cancels", delegate
        {
            using (var dialog = NewDialog(true))
            {
                int cancelled = 0, retries = 0;
                dialog.CancelRequested += () => cancelled++;
                dialog.RetryRequested += (count, step) => retries++;
                Invoke(dialog, "BarcodeRecoveryDialog_FormClosing", dialog, new FormClosingEventArgs(CloseReason.UserClosing, false));
                Check(cancelled == 1 && retries == 0, "Window close requested a retry.");
            }
        });
        Test("validation-explicit-recheck", delegate
        {
            using (var dialog = NewDialog(true))
            {
                int retries = 0, cancelled = 0;
                dialog.RetryRequested += (count, step) => retries++;
                dialog.CancelRequested += () => cancelled++;
                Invoke(dialog, "btnRetry_Click", dialog, EventArgs.Empty);
                Check(retries == 1 && cancelled == 0, "Explicit recheck did not return Retry.");
            }
        });
        Test("manual-candidate-submitted-once", delegate
        {
            using (var dialog = NewDialog(true))
            {
                string candidate = null;
                int submissions = 0, otherDecisions = 0;
                dialog.ManualApplyRequested += (value, count, step) => { candidate = value; submissions++; };
                dialog.RetryRequested += (count, step) => otherDecisions++;
                dialog.CancelRequested += () => otherDecisions++;
                Field<TextBox>(dialog, "txtManualBarcode").Text = "YZAMH.02";
                Invoke(dialog, "btnManualApply_Click", dialog, EventArgs.Empty);
                Invoke(dialog, "btnRetry_Click", dialog, EventArgs.Empty);
                Invoke(dialog, "btnCloseRetry_Click", dialog, EventArgs.Empty);
                Check(candidate == "YZAMH.02" && submissions == 1 && otherDecisions == 0, "Duplicate or stale candidate decision.");
            }
        });
        Test("sequence-cancellation-does-not-retry", delegate
        {
            using (var dialog = NewDialog(true))
            {
                int decisions = 0;
                dialog.RetryRequested += (count, step) => decisions++;
                dialog.CancelRequested += () => decisions++;
                dialog.CloseForCancellation();
                Invoke(dialog, "BarcodeRecoveryDialog_FormClosing", dialog, new FormClosingEventArgs(CloseReason.UserClosing, false));
                Check(decisions == 0, "Sequence cancellation created a new operator decision.");
            }
        });
        Test("legacy-input-close-still-retries", delegate
        {
            using (var dialog = NewDialog(false))
            {
                int retries = 0, cancelled = 0;
                dialog.RetryRequested += (count, step) => retries++;
                dialog.CancelRequested += () => cancelled++;
                Invoke(dialog, "btnCloseRetry_Click", dialog, EventArgs.Empty);
                Check(retries == 1 && cancelled == 0, "Legacy reader close behavior changed.");
            }
        });
        Test("legacy-output-window-close-still-retries", delegate
        {
            using (var dialog = new BarcodeRecoveryDialog(new BarcodeRecoveryRequest { Channel = BarcodeReaderChannel.OutputBin }))
            {
                int retries = 0, cancelled = 0;
                dialog.RetryRequested += (count, step) => retries++;
                dialog.CancelRequested += () => cancelled++;
                Check(Field<Label>(dialog, "lblChannelValue").Text == "OUTPUT BIN", "Output reader label changed.");
                Check(Field<Button>(dialog, "btnRetry").Text == "RETRY SCAN", "Output reader action changed.");
                Invoke(dialog, "BarcodeRecoveryDialog_FormClosing", dialog, new FormClosingEventArgs(CloseReason.UserClosing, false));
                Check(retries == 1 && cancelled == 0, "Legacy output close behavior changed.");
            }
        });
        Console.WriteLine("SUMMARY dialogPassed=" + _passed + ", failed=" + _failed);
        return _failed == 0 ? 0 : 1;
    }

    private static BarcodeRecoveryDialog NewDialog(bool validation)
    {
        return new BarcodeRecoveryDialog(new BarcodeRecoveryRequest
        {
            ValidationRecovery = validation,
            Channel = BarcodeReaderChannel.InputWafer,
            CurrentBarcode = "YZAXH.02",
            LotId = "YZAMH",
            PrefixLength = 5,
            FailureMessage = "앞부분이 일치하지 않습니다.",
            MaterialId = "TEST-WAFER",
            MaterialInstanceId = "test-instance"
        });
    }

    private static T Field<T>(object target, string name)
    {
        return (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    private static void Invoke(object target, string name, params object[] args)
    {
        target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Test(string name, Action action)
    {
        try { action(); _passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { _failed++; Console.Error.WriteLine("FAIL " + name + ": " + error); }
    }
}
