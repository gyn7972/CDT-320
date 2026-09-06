using System;
using System.Collections;
using System.Reflection;

internal static class InputStageReviewLogPolicyTests
{
    private static int _assertions;

    private static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: InputStageReviewLogPolicyTests.exe <QMC.Common.dll>");
            return 2;
        }

        try
        {
            Run(Assembly.LoadFrom(args[0]));
            Console.WriteLine("PASS: " + _assertions + " actual QMC.Common log-policy assertions; no logger writer, settings API, Handler, or hardware instantiated.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void Run(Assembly common)
    {
        Type policy = common.GetType("QMC.Common.Logging.LogPolicy", true);
        Type rowType = common.GetType("QMC.Common.Logging.EventRow", true);
        Type modeType = common.GetType("QMC.Common.Logging.LogMode", true);
        Type kindType = common.GetType("QMC.Common.Logging.EventKind", true);
        Type severityType = common.GetType("QMC.Common.Logging.LogSeverity", true);
        FieldInfo mode = RequireField(policy, "_mode");
        FieldInfo expiry = RequireField(policy, "_diagnosticExpireAt");
        FieldInfo scopeCount = RequireField(policy, "_verboseScopeCount");
        FieldInfo prefixes = RequireField(policy, "_persistCodePrefixes");
        FieldInfo blackbox = RequireField(policy, "Blackbox");
        object oldMode = mode.GetValue(null);
        object oldExpiry = expiry.GetValue(null);
        object oldScopeCount = scopeCount.GetValue(null);
        object oldPrefixes = prefixes.GetValue(null);
        int blackboxBefore = ((ICollection)blackbox.GetValue(null)).Count;
        object normal = Enum.Parse(severityType, "Normal");
        object audit = Enum.Parse(severityType, "Audit");
        object failure = Enum.Parse(severityType, "Failure");
        object eventKind = Enum.Parse(kindType, "Event");
        MethodInfo shouldPersist = policy.GetMethod("ShouldPersist", BindingFlags.Public | BindingFlags.Static);
        if (shouldPersist == null)
            throw new InvalidOperationException("The actual ShouldPersist API was not found.");

        // Change private policy fields in this isolated test process only. Public enable/disable/configure
        // APIs emit file logs and are intentionally not called. EventLogger is never instantiated/called.
        try
        {
            mode.SetValue(null, Enum.Parse(modeType, "ProductionMinimal"));
            expiry.SetValue(null, DateTime.MaxValue);
            scopeCount.SetValue(null, 0);
            prefixes.SetValue(null, new[] { "AUTO" });
            Assert(!(bool)policy.GetProperty("IsDiagnosticVerbose").GetValue(null, null), "Minimal mode is actually active");

            string[] codes =
            {
                "IN-REVIEW-OPEN", "IN-REVIEW-DETECTION", "IN-REVIEW-OFFSET-COMMITTED",
                "IN-REVIEW-COMMIT-SAVED", "PICKUP-TARGET", "INPUT-PICK-TARGET"
            };
            foreach (string code in codes)
            {
                object row = CreateRow(rowType, eventKind, code);
                Assert(Persists(shouldPersist, row, audit), code + " Audit is retained without an allowlist match");
                Assert(!Persists(shouldPersist, row, normal), code + " Normal alone is not permanently retained");
            }

            foreach (string headKind in new[] { "FrontHeadSeq", "RearHeadSeq" })
            {
                object row = CreateRow(rowType, Enum.Parse(kindType, headKind), "PICKUP-TARGET");
                Assert(Persists(shouldPersist, row, audit), headKind + " pickup audit is retained");
                Assert(!Persists(shouldPersist, row, normal), headKind + " normal pickup record alone is not retained");
            }

            object failureRow = CreateRow(rowType, eventKind, "IN-REVIEW-SAVE-FAILED");
            Assert(Persists(shouldPersist, failureRow, failure), "Explicit failure is retained");
            rowType.GetProperty("Description").SetValue(failureRow, "Message text ends with - Failed", null);
            Assert(!Persists(shouldPersist, failureRow, normal), "Message suffix does not replace explicit importance");
            Assert(Persists(shouldPersist, CreateRow(rowType, eventKind, "AUTO-VISION-CORRELATED-RESULT"), normal), "Existing AUTO allowlist remains effective");
            Assert(!Persists(shouldPersist, null, audit), "Null row is rejected even at Audit importance");

            const string description = "wafer=WF-001, die=D123X000001Y000002, detail=\"수동 확인\", raw=unavailable, offsetX=0";
            object csvRow = CreateRow(rowType, eventKind, "IN-REVIEW-COMMIT-SAVED");
            rowType.GetProperty("Description").SetValue(csvRow, description, null);
            string csv = (string)rowType.GetMethod("ToCsv").Invoke(csvRow, null);
            object parsed = rowType.GetMethod("FromCsv").Invoke(null, new object[] { csv });
            Assert(parsed != null, "The existing Event CSV contract parses the diagnostic row");
            Assert((string)rowType.GetProperty("Description").GetValue(parsed, null) == description, "CSV preserves IDs, Korean text, commas, quotes, unavailable and real zero");
            Assert((string)rowType.GetProperty("Code").GetValue(parsed, null) == "IN-REVIEW-COMMIT-SAVED", "CSV preserves the searchable event code");
            Assert(!(bool)policy.GetProperty("IsDiagnosticVerbose").GetValue(null, null), "Queries do not enable detailed logging");
            Assert(((ICollection)blackbox.GetValue(null)).Count == blackboxBefore, "Pure policy and CSV checks do not emit log records");
        }
        finally
        {
            mode.SetValue(null, oldMode);
            expiry.SetValue(null, oldExpiry);
            scopeCount.SetValue(null, oldScopeCount);
            prefixes.SetValue(null, oldPrefixes);
        }
    }

    private static FieldInfo RequireField(Type type, string name)
    {
        FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        if (field == null)
            throw new InvalidOperationException("The actual log policy field was not found: " + name);
        return field;
    }

    private static object CreateRow(Type rowType, object kind, string code)
    {
        object row = Activator.CreateInstance(rowType);
        rowType.GetProperty("When").SetValue(row, new DateTime(2026, 9, 6, 12, 34, 56, 789), null);
        rowType.GetProperty("Kind").SetValue(row, kind, null);
        rowType.GetProperty("User").SetValue(row, "OFFLINE-TEST", null);
        rowType.GetProperty("Code").SetValue(row, code, null);
        rowType.GetProperty("Source").SetValue(row, "InputStageReviewGeometry", null);
        rowType.GetProperty("Description").SetValue(row, "policy only", null);
        return row;
    }

    private static bool Persists(MethodInfo method, object row, object severity)
    {
        return (bool)method.Invoke(null, new[] { row, severity });
    }

    private static void Assert(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException("FAIL: " + label);
        _assertions++;
    }
}
