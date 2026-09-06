using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;

internal static class InputStageReviewProgressPreservationTests
{
    private const string Revision = "OFFLINE-PARTIAL-MAP";
    private static readonly MethodInfo Check = typeof(MaterialStateService).GetMethod(
        "CheckInputStageReviewProgressPreservation", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo PointCheck = typeof(MaterialStateService).GetMethod(
        "CheckInputStageReviewVerificationDieState", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo ModeCheck = typeof(MaterialStateService).GetMethod(
        "CheckInputStageReviewNonProductionContext", BindingFlags.Static | BindingFlags.NonPublic);
    private static int _checks;
    private static int _pointChecks;
    private static int _modeChecks;

    private sealed class ModeFixture
    {
        internal string Token = "NONPRODUCTION-MANUAL-OFFLINE";
        internal bool ExplicitMode = true;
        internal InputStageReviewGeometryContext Saved = new InputStageReviewGeometryContext
        {
            WaferId = "OFFLINE-WAFER", MappingRevision = Revision, ConditionSignature = "RUNTIME-FLAGS-A",
            CandidateSignature = "OFFLINE-COORDINATES", SessionGeneration = 1, RequestGeneration = 2,
            StageTheta = -90, PitchX = 8.12, PitchY = 6.12, OriginX = 10, OriginY = 20,
            BaselineOriginX = 10, BaselineOriginY = 20, IsSimulation = true
        };
        internal InputStageReviewGeometryContext Current;
        internal ModeFixture() { Current = Saved.Clone(); }
    }

    private sealed class Fixture
    {
        internal WaferMaterial Wafer;
        internal DieMap Map;
        internal UserConfirmResult Review;
        internal string CurrentRevision = Revision;
    }

    private static Fixture Create()
    {
        var map = new DieMap { FrameObjId = Revision, OriginX = 10, OriginY = 20 };
        for (int i = 0; i < 5; i++)
            map.Entries.Add(new DieMapEntry
            {
                DieUid = ((char)('A' + i)).ToString(), Index = i, DieMapX = i % 2, DieMapY = i / 2,
                PosX = 10 + i % 2 * 8.12, PosY = 20 + i / 2 * 6.12, IsTarget = i < 4,
                Result = i == 0 ? DieResult.Good : i == 4 ? DieResult.NG : DieResult.Unknown,
                BinCode = i == 4 ? 255 : 1, SequenceNo = i < 4 ? i + 1 : 0
            });
        return new Fixture
        {
            Wafer = new WaferMaterial
            {
                WaferId = "OFFLINE-WAFER", InputStageRunReviewMappingRevision = Revision,
                InputStageRunReviewOrderedDieIds = new List<string> { "A", "B", "C", "D" },
                InputStageRunReviewStartDieUid = "A", InputStageRunReviewStartDieIndex = 4
            },
            Map = map,
            Review = new UserConfirmResult
            {
                HasMapOrigin = true, MapOriginX = map.OriginX, MapOriginY = map.OriginY,
                StartDieUid = "B", StartDieIndex = 1, OrderedDieIds = new List<string> { "B", "C", "D" },
                DieStates = map.Entries.Select(entry => new InputStageRunReviewDieState
                {
                    DieId = entry.DieUid, IsTarget = entry.IsTarget, Result = entry.Result,
                    BinCode = entry.BinCode, HasPosition = true, PositionX = entry.PosX, PositionY = entry.PosY
                }).ToList()
            }
        };
    }

    private static string Values(params object[] values)
    {
        return string.Join("|", values.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture)));
    }

    private static string Snapshot(Fixture data)
    {
        return Values(data.Wafer.HasInputStageRunReviewApproval, data.Wafer.InputStageRunReviewMappingRevision,
            data.Wafer.InputStageRunReviewStartDieUid, data.Wafer.InputStageRunReviewStartDieIndex,
            string.Join(",", data.Wafer.InputStageRunReviewOrderedDieIds ?? new List<string>()),
            data.Map.OriginX, data.Map.OriginY,
            string.Join(";", data.Map.Entries.Select(entry => Values(entry.DieUid, entry.IsTarget, entry.Result,
                entry.BinCode, entry.PosX, entry.PosY, entry.SequenceNo))),
            data.Review.StartDieUid, data.Review.StartDieIndex,
            string.Join(",", data.Review.OrderedDieIds ?? new List<string>()),
            string.Join(";", data.Review.DieStates.Select(entry => Values(entry.DieId, entry.IsTarget, entry.Result,
                entry.BinCode, entry.PositionX, entry.PositionY, entry.HasPosition))));
    }

    private static void Expect(string label, bool expected, Action<Fixture> arrange = null)
    {
        Fixture data = Create();
        if (arrange != null) arrange(data);
        string before = Snapshot(data);
        List<string> originalOrder = data.Wafer.InputStageRunReviewOrderedDieIds;
        var arguments = new object[] { data.Wafer, data.Map, data.Review, data.CurrentRevision, null };
        bool accepted = (bool)Check.Invoke(null, arguments);
        if (accepted != expected)
            throw new InvalidOperationException(label + " expected=" + expected + ", reason=" + arguments[4]);
        if (Snapshot(data) != before || !ReferenceEquals(originalOrder, data.Wafer.InputStageRunReviewOrderedDieIds))
            throw new InvalidOperationException(label + ": validation mutated saved material/order or submitted map");
        if (!accepted && string.IsNullOrWhiteSpace(Convert.ToString(arguments[4])))
            throw new InvalidOperationException(label + ": rejection reason missing");
        _checks++;
        Console.WriteLine("PASS " + label);
    }

    private static void ExpectPoint(string label, bool expected, Action<DieMaterial> arrange = null)
    {
        var die = new DieMaterial { DieId = "OFFLINE-POINT", CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage } };
        if (arrange != null) arrange(die);
        string before = Values(die.DieId, die.IsInputTarget, die.Result, die.ReservedPickerLocation,
            die.ReservedPickerNo, die.PickedPickerLocation, die.PickedPickerNo, die.PickedAt,
            die.CurrentLocation != null ? die.CurrentLocation.Kind.ToString() : "NULL");
        var arguments = new object[] { die, null };
        bool accepted = (bool)PointCheck.Invoke(null, arguments);
        if (accepted != expected)
            throw new InvalidOperationException(label + " expected=" + expected + ", reason=" + arguments[1]);
        string after = Values(die.DieId, die.IsInputTarget, die.Result, die.ReservedPickerLocation,
            die.ReservedPickerNo, die.PickedPickerLocation, die.PickedPickerNo, die.PickedAt,
            die.CurrentLocation != null ? die.CurrentLocation.Kind.ToString() : "NULL");
        if (before != after) throw new InvalidOperationException(label + ": validation mutated physical die state");
        _pointChecks++;
        Console.WriteLine("PASS point " + label);
    }

    private static void ExpectMode(string label, bool expected, Action<ModeFixture> arrange = null)
    {
        var data = new ModeFixture();
        if (arrange != null) arrange(data);
        var arguments = new object[] { data.Token, data.Saved, data.Current, data.ExplicitMode, null };
        bool accepted = (bool)ModeCheck.Invoke(null, arguments);
        if (accepted != expected)
            throw new InvalidOperationException(label + " expected=" + expected + ", reason=" + arguments[4]);
        _modeChecks++;
        Console.WriteLine("PASS mode " + label);
    }

    private static int Main()
    {
        try
        {
            if (Check == null) throw new InvalidOperationException("Latest compiled review preservation helper is missing");
            Expect("partial wafer with completed A and normalized SKIP keeps original full order/start", true);
            Expect("remaining UI start index differs without changing first UID or stored order", true,
                data => data.Review.StartDieIndex = 2);
            Expect("no selected UI start preserves saved original start", true,
                data => { data.Review.StartDieUid = ""; data.Review.StartDieIndex = 0; });
            Expect("X origin edit", false, data => data.Review.MapOriginX += 0.001);
            Expect("Y origin edit", false, data => data.Review.MapOriginY += 0.001);
            Expect("missing origin", false, data => data.Review.HasMapOrigin = false);
            Expect("tiny X coordinate edit is not hidden by tolerance", false, data => data.Review.DieStates[1].PositionX += 0.00000001);
            Expect("Y coordinate edit", false, data => data.Review.DieStates[1].PositionY += 0.001);
            Expect("missing position", false, data => data.Review.DieStates[1].HasPosition = false);
            Expect("NaN position", false, data => data.Review.DieStates[1].PositionX = double.NaN);
            Expect("WAIT target disabled", false, data => data.Review.DieStates[1].IsTarget = false);
            Expect("completed die restored to WAIT", false, data => data.Review.DieStates[0].Result = DieResult.Unknown);
            Expect("completed die Bin changed", false, data => data.Review.DieStates[0].BinCode = 255);
            Expect("normalized SKIP altered", false, data => data.Review.DieStates[4].Result = DieResult.Unknown);
            Expect("UID changed", false, data => data.Review.DieStates[1].DieId = "OTHER");
            Expect("duplicate submitted UID", false, data => data.Review.DieStates[1].DieId = "A");
            Expect("missing submitted die", false, data => data.Review.DieStates.RemoveAt(0));
            Expect("remaining order reversed", false, data => data.Review.OrderedDieIds.Reverse());
            Expect("selected later start skips waiting die", false, data =>
            {
                data.Review.OrderedDieIds.RemoveAt(0); data.Review.StartDieUid = "C";
            });
            Expect("rotated remaining order", false, data =>
            {
                data.Review.OrderedDieIds = new List<string> { "C", "D", "B" };
                data.Review.StartDieUid = "C"; data.Review.StartDieIndex = 2;
            });
            Expect("start UID differs from first remaining die", false, data => data.Review.StartDieUid = "C");
            Expect("start index exceeds remaining range", false, data => data.Review.StartDieIndex = 4);
            Expect("missing saved mapping revision", false, data => data.Wafer.InputStageRunReviewMappingRevision = "");
            Expect("changed current mapping revision", false, data => data.CurrentRevision = "NEW-MAP");
            Expect("missing original approved order", false, data => data.Wafer.InputStageRunReviewOrderedDieIds = null);
            Expect("unknown original approved UID", false, data => data.Wafer.InputStageRunReviewOrderedDieIds.Add("OTHER"));
            Expect("duplicate original approved UID", false, data => data.Wafer.InputStageRunReviewOrderedDieIds.Add("A"));
            Expect("WAIT die absent from original approved order", false, data => data.Wafer.InputStageRunReviewOrderedDieIds.Remove("C"));
            Expect("invalid saved original start metadata", false, data => data.Wafer.InputStageRunReviewStartDieUid = "C");
            Expect("all completed retains original order without restoring targets", true, data =>
            {
                for (int i = 1; i < 4; i++)
                {
                    data.Map.Entries[i].Result = DieResult.Good;
                    data.Review.DieStates[i].Result = DieResult.Good;
                }
                data.Review.OrderedDieIds.Clear(); data.Review.StartDieUid = ""; data.Review.StartDieIndex = 0;
            });
            Console.WriteLine("PASS {0} progress preservation cases; every case also checks no state/order mutation.", _checks);
            if (PointCheck == null) throw new InvalidOperationException("Latest compiled verification point guard is missing");
            ExpectPoint("unreserved InputStage WAIT", true);
            ExpectPoint("unknown physical location", false, die => die.CurrentLocation.Kind = MaterialLocationKind.Unknown);
            ExpectPoint("missing physical location", false, die => die.CurrentLocation = null);
            ExpectPoint("already on front picker", false, die => die.CurrentLocation.Kind = MaterialLocationKind.PickerFront);
            ExpectPoint("already on output stage", false, die => die.CurrentLocation.Kind = MaterialLocationKind.OutputStageGood);
            ExpectPoint("non-target", false, die => die.IsInputTarget = false);
            ExpectPoint("completed GOOD", false, die => die.Result = DieResult.Good);
            ExpectPoint("completed NG", false, die => die.Result = DieResult.NG);
            ExpectPoint("front picker reserved", false, die =>
            {
                die.ReservedPickerLocation = MaterialLocationKind.PickerFront; die.ReservedPickerNo = 1;
            });
            ExpectPoint("rear picker reserved", false, die =>
            {
                die.ReservedPickerLocation = MaterialLocationKind.PickerRear; die.ReservedPickerNo = 2;
            });
            ExpectPoint("picked time remains", false, die => die.PickedAt = new DateTime(2026, 9, 4));
            ExpectPoint("picked picker number remains", false, die => die.PickedPickerNo = 1);
            ExpectPoint("picked picker location remains", false, die => die.PickedPickerLocation = MaterialLocationKind.PickerRear);
            ExpectPoint("PickUp inspection OK remains", false, die => die.Inspections.Add(new DieInspectionRecord
            {
                InspectionType = "PickUp", Result = MaterialInspectionResult.Ok
            }));
            ExpectPoint("PickUp inspection NG remains", false, die => die.Inspections.Add(new DieInspectionRecord
            {
                InspectionType = "PickUp", Result = MaterialInspectionResult.Ng
            }));
            ExpectPoint("unrelated inspection does not mean picked", true, die => die.Inspections.Add(new DieInspectionRecord
            {
                InspectionType = "InputVision", Result = MaterialInspectionResult.Ok
            }));
            Console.WriteLine("PASS {0} verification point state cases; no equipment construction or commands.", _pointChecks);
            if (ModeCheck == null) throw new InvalidOperationException("Latest compiled nonproduction approval guard is missing");
            ExpectMode("matching explicit nonproduction manual approval", true);
            ExpectMode("runtime becomes production", false, data => data.Current.IsSimulation = false);
            ExpectMode("production context cannot be relabeled as nonproduction", false, data => data.Saved.IsSimulation = false);
            ExpectMode("unknown or non-explicit runtime mode", false, data => data.ExplicitMode = false);
            ExpectMode("ordinary verification token cannot use manual branch", false, data => data.Token = "ORDINARY-VERIFICATION");
            ExpectMode("simulation default token is a distinct approval kind", false, data => data.Token = "SIMULATION-DEFAULT-OFFLINE");
            ExpectMode("missing token", false, data => data.Token = null);
            ExpectMode("wafer changes", false, data => data.Current.WaferId = "NEXT-WAFER");
            ExpectMode("mapping revision changes", false, data => data.Current.MappingRevision = "NEXT-MAP");
            ExpectMode("request changes", false, data => data.Current.RequestGeneration++);
            ExpectMode("session changes", false, data => data.Current.SessionGeneration++);
            ExpectMode("individual runtime flag signature changes", false, data => data.Current.ConditionSignature = "RUNTIME-FLAGS-B");
            ExpectMode("candidate coordinate signature changes", false, data => data.Current.CandidateSignature = "OTHER-COORDINATES");
            ExpectMode("stored theta changes", false, data => data.Current.StageTheta += 0.001);
            ExpectMode("pitch changes", false, data => data.Current.PitchX += 0.001);
            Console.WriteLine("PASS {0} nonproduction marker/context cases; no synthetic measurements.", _modeChecks);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
