using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.CDT320.Materials;

internal static class InputStageReviewGeometryPolicyTests
{
    private static int _passed;

    private static int Main()
    {
        try
        {
            Run("incident coordinate difference is representable, not a measured pickup error", IncidentCoordinates);
            Run("large review offset is allowed without half-pitch clamp", LargeOffset);
            Run("independent single and cumulative axis limits", IndependentLimits);
            Run("signed exact boundaries and visible excess", Boundaries);
            Run("100 small applications preserve original baseline", RepeatedOffsets);
            Run("serialized baseline is preserved when review reopens", BaselinePersistence);
            Run("invalid finite geometry and settings fail without candidate mutation", InvalidInputs);
            Run("arithmetic overflow fails without candidate mutation", Overflow);
            Run("three corresponding points verify correctly", ThreePoints);
            Run("large valid candidate still needs physical correspondence", LargeOffsetCorrespondence);
            Run("center fits but far points drift", FarPointDrift);
            Run("individual recentering cannot hide original residual", RecenteredPoints);
            Run("measured pitch is independently checked", PitchMismatch);
            Run("absent pitch and invalid tolerance are not success", MissingPitch);
            Run("single row cannot establish Y pitch", Collinear);
            Run("nearly collinear in-map points request reselection before claiming angle error", NearCollinearLayout);
            Run("layout selection is invariant under rotation, scale, mirror and point order", LayoutTransformInvariance);
            Run("layout criterion is distinct from offset and precision limits", LayoutIndependentLimits);
            Run("duplicate UID and duplicate grid coordinate are rejected", DuplicateSamples);
            Run("index spread with collinear physical points is rejected", CollinearPhysicalPoints);
            Run("grid angle mismatch is independently checked", GridAngleMismatch);
            Run("expected rotated grid is valid without forcing raw angle to zero", RotatedGrid);
            Run("final stage angle permits wrap, rejects changed T", StageTheta);
            Run("fourth unverified point cannot hide behind valid triangle", FourthPoint);
            Run("interior expected point cannot silently change candidate geometry", InconsistentExpectedPoint);
            Run("input camera Y sign is applied once", VisionYSign);
            Run("changed session, request, conditions and candidate invalidate evidence", StaleContexts);
            Run("four completed picks then authorized pending offset rebases candidate only", AuthorizedPendingOffsetRebase);
            Run("authorized pending offset rebase rejects every unrelated context change", AuthorizedPendingOffsetRejectsOtherChanges);
            Run("evidence snapshots do not alias caller state", EvidenceImmutable);
            Run("persisted JSON context, tolerance and raw samples reverify", EvidenceJsonRoundTrip);
            Run("simulation evidence cannot approve production", Simulation);
            Console.WriteLine("PASS: " + _passed + " independent policy scenarios. No equipment assemblies or hardware were loaded.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL after " + _passed + " scenarios: " + ex);
            return 1;
        }
    }

    private static void Run(string name, Action test)
    {
        test();
        _passed++;
        Console.WriteLine("PASS " + _passed + ": " + name);
    }

    private static InputStageReviewGeometryContext Context()
    {
        return new InputStageReviewGeometryContext
        {
            WaferId = "test-wafer", MappingRevision = "map-1", ConditionSignature = "recipe-align-calibration-1",
            CandidateSignature = "candidate-1", SessionGeneration = 1, RequestGeneration = 1,
            OriginX = 100.0, OriginY = 200.0, BaselineOriginX = 100.0, BaselineOriginY = 200.0,
            PitchX = 8.37, PitchY = 6.37, StageTheta = 179.81909
        };
    }

    private static InputStageReviewOffsetLimits Limits()
    {
        return new InputStageReviewOffsetLimits { SingleX = 20, SingleY = 20, CumulativeX = 20, CumulativeY = 20 };
    }

    private static InputStageReviewGeometryTolerance Tolerance()
    {
        // Test-only inputs; these values are not written to an equipment configuration.
        return new InputStageReviewGeometryTolerance { ResidualX = .1, ResidualY = .1, Pitch = .1, Theta = .05, StageTheta = .01 };
    }

    private static List<InputStageReviewMeasurement> Samples()
    {
        return new List<InputStageReviewMeasurement>
        {
            Sample("a", 0, 0, 100, 200), Sample("b", 10, 0, 183.7, 200), Sample("c", 0, 10, 100, 263.7)
        };
    }

    private static InputStageReviewMeasurement Sample(string id, int x, int y, double expectedX, double expectedY)
    {
        return new InputStageReviewMeasurement
        {
            DieUid = id, GridX = x, GridY = y, ExpectedX = expectedX, ExpectedY = expectedY,
            CaptureX = expectedX, CaptureY = expectedY, CaptureT = 179.81909,
            CorrespondenceConfirmed = true, CorrespondenceEvidence = "offline explicitly identified physical die " + id
        };
    }

    private static void Assert(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException(reason);
    }

    private static void Near(double actual, double expected)
    {
        Assert(Math.Abs(actual - expected) < .00000001, "Expected " + expected + ", actual " + actual);
    }

    private static InputStageReviewOffsetCandidate Allow(InputStageReviewGeometryContext context, double x, double y, InputStageReviewOffsetLimits limits)
    {
        InputStageReviewOffsetCandidate candidate;
        string reason;
        Assert(InputStageReviewGeometryPolicy.TryCreateOffsetCandidate(context, x, y, limits, out candidate, out reason), reason);
        Assert(candidate != null, "allowed candidate missing");
        return candidate;
    }

    private static void Deny(InputStageReviewGeometryContext context, double x, double y, InputStageReviewOffsetLimits limits)
    {
        InputStageReviewOffsetCandidate candidate;
        string reason;
        Assert(!InputStageReviewGeometryPolicy.TryCreateOffsetCandidate(context, x, y, limits, out candidate, out reason), "unexpected offset acceptance");
        Assert(candidate == null && !string.IsNullOrWhiteSpace(reason), "failure mutated/published candidate or omitted reason");
    }

    private static InputStageReviewGeometryEvidence Verify(InputStageReviewGeometryContext context, IList<InputStageReviewMeasurement> samples, InputStageReviewGeometryTolerance tolerance)
    {
        InputStageReviewGeometryEvidence evidence;
        string reason;
        Assert(InputStageReviewGeometryPolicy.TryVerify(context, samples, tolerance, out evidence, out reason), reason);
        return evidence;
    }

    private static void FailVerify(InputStageReviewGeometryContext context, IList<InputStageReviewMeasurement> samples, InputStageReviewGeometryTolerance tolerance)
    {
        InputStageReviewGeometryEvidence evidence;
        string reason;
        Assert(!InputStageReviewGeometryPolicy.TryVerify(context, samples, tolerance, out evidence, out reason), "unexpected geometry acceptance");
        Assert(evidence == null && !string.IsNullOrWhiteSpace(reason), "failure published evidence or omitted reason");
    }

    private static void IncidentCoordinates()
    {
        var context = Context();
        context.OriginX = context.BaselineOriginX = 379.991945;
        context.OriginY = context.BaselineOriginY = 9.735110;
        Near(379.991945 + 33 * 8.37, 656.201945);
        Near(9.735110 + 19 * 6.37, 130.765110);
        var candidate = Allow(context, 664.781275 - 656.201945, 131.101475 - 130.765110, Limits());
        Near(candidate.CumulativeX, 8.579330);
        Near(candidate.CumulativeY, .336365);
    }

    private static void LargeOffset()
    {
        var candidate = Allow(Context(), 8.218, .3, Limits());
        Near(candidate.OriginX, 108.218);
        Assert(candidate.HalfPitchRatioX > 1, "scenario must exceed half pitch");
        var limits = Limits(); limits.SingleX = 40; limits.CumulativeX = 40;
        Allow(Context(), 35, 0, limits);
    }

    private static void IndependentLimits()
    {
        var limits = Limits(); limits.SingleX = 1; limits.SingleY = 8; limits.CumulativeX = 10; limits.CumulativeY = 3;
        Allow(Context(), 1, 3, limits);
        Deny(Context(), 1.1, 0, limits);
        Deny(Context(), 0, 3.1, limits);
        var context = Context(); context.OriginX = 109;
        Allow(context, 1, 0, limits);
        context.OriginX = 109.1;
        Deny(context, 1, 0, limits);
    }

    private static void Boundaries()
    {
        foreach (double sign in new[] { -1.0, 1.0 })
        {
            Allow(Context(), sign * 19.999999, sign * 19.999999, Limits());
            Allow(Context(), sign * 20, sign * 20, Limits());
            Deny(Context(), sign * 20.000001, 0, Limits());
            Deny(Context(), 0, sign * 20.000001, Limits());
            Deny(Context(), sign * 20.000001, sign * 20.000001, Limits());
        }
    }

    private static void RepeatedOffsets()
    {
        var context = Context();
        for (int i = 0; i < 100; i++) context.OriginX = Allow(context, .2, 0, Limits()).OriginX;
        Near(context.OriginX, 120);
        Near(context.BaselineOriginX, 100);
        Deny(context, .2, 0, Limits());
    }

    private static void BaselinePersistence()
    {
        var context = Context(); context.OriginX = 119.8;
        var serializer = new DataContractSerializer(typeof(InputStageReviewGeometryContext));
        using (var stream = new MemoryStream())
        {
            serializer.WriteObject(stream, context);
            stream.Position = 0;
            var reloaded = (InputStageReviewGeometryContext)serializer.ReadObject(stream);
            reloaded.SessionGeneration = 5;
            reloaded.RequestGeneration = 9;
            Near(Allow(reloaded, .2, 0, Limits()).CumulativeX, 20);
            Deny(reloaded, .3, 0, Limits());
        }
    }

    private static void InvalidInputs()
    {
        foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, 0, -1 })
        {
            var context = Context(); context.PitchX = value; Deny(context, 0, 0, Limits());
            context = Context(); context.PitchY = value; Deny(context, 0, 0, Limits());
            var limits = Limits(); limits.SingleX = value; Deny(Context(), 0, 0, limits);
            limits = Limits(); limits.SingleY = value; Deny(Context(), 0, 0, limits);
            limits = Limits(); limits.CumulativeX = value; Deny(Context(), 0, 0, limits);
            limits = Limits(); limits.CumulativeY = value; Deny(Context(), 0, 0, limits);
        }
        foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Deny(Context(), value, 0, Limits()); Deny(Context(), 0, value, Limits());
            var context = Context(); context.BaselineOriginX = value; Deny(context, 0, 0, Limits());
            context = Context(); context.OriginY = value; Deny(context, 0, 0, Limits());
        }
        var valid = Context();
        Deny(valid, 21, 0, Limits()); Near(valid.OriginX, 100);
    }

    private static void Overflow()
    {
        var context = Context(); context.OriginX = context.BaselineOriginX = double.MaxValue;
        var limits = Limits(); limits.SingleX = limits.CumulativeX = double.MaxValue;
        Deny(context, double.MaxValue, 0, limits);
        context = Context(); context.BaselineOriginX = -double.MaxValue; context.OriginX = double.MaxValue;
        Deny(context, 0, 0, limits);
    }

    private static void ThreePoints()
    {
        var context = Context();
        var evidence = Verify(context, Samples(), Tolerance());
        Near(evidence.MeasuredPitchX, 8.37); Near(evidence.MeasuredPitchY, 6.37);
        string reason;
        Assert(InputStageReviewGeometryPolicy.IsEvidenceUsable(evidence, context, true, out reason), reason);
    }

    private static void LargeOffsetCorrespondence()
    {
        var context = Context(); context.OriginX = Allow(context, 8.218, 0, Limits()).OriginX;
        var samples = Samples();
        foreach (var sample in samples) { sample.ExpectedX += 8.218; sample.CaptureX += 8.218; }
        Verify(context, samples, Tolerance());
        samples[0].CorrespondenceConfirmed = false;
        FailVerify(context, samples, Tolerance());
        samples[0].CorrespondenceConfirmed = true; samples[0].CorrespondenceEvidence = "";
        FailVerify(context, samples, Tolerance());
    }

    private static void FarPointDrift()
    {
        var samples = Samples(); samples[1].RawVisionDeltaX = 1.4027;
        FailVerify(Context(), samples, Tolerance());
    }

    private static void RecenteredPoints()
    {
        var samples = Samples(); samples[1].CaptureX += 1.4027;
        Near(samples[1].RawVisionDeltaX, 0);
        FailVerify(Context(), samples, Tolerance());
    }

    private static void PitchMismatch()
    {
        var samples = Samples(); samples[1].CaptureX = 182.2973;
        var tolerance = Tolerance(); tolerance.ResidualX = 2;
        FailVerify(Context(), samples, tolerance);
        string reason;
        Assert(!InputStageReviewGeometryPolicy.TryValidateMeasuredPitch(8.229730, 8.37, .1, out reason), "measured pitch silently replaced");
        Assert(InputStageReviewGeometryPolicy.TryValidateMeasuredPitch(8.27, 8.37, .1, out reason), reason);
    }

    private static void MissingPitch()
    {
        string reason;
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, 0, -1 })
        {
            Assert(!InputStageReviewGeometryPolicy.TryValidateMeasuredPitch(invalid, 8.37, .1, out reason), "absent pitch accepted");
            var tolerance = Tolerance(); tolerance.Pitch = invalid;
            FailVerify(Context(), Samples(), tolerance);
        }
    }

    private static void Collinear()
    {
        var samples = Samples(); samples[2] = Sample("c", 20, 0, 267.4, 200);
        FailVerify(Context(), samples, Tolerance());
        samples.RemoveAt(2); FailVerify(Context(), samples, Tolerance());
    }

    private static void DuplicateSamples()
    {
        var samples = Samples(); samples[2].DieUid = "A";
        FailVerify(Context(), samples, Tolerance());
        samples = Samples(); samples[2].GridX = 10; samples[2].GridY = 0;
        FailVerify(Context(), samples, Tolerance());
    }

    private static void CollinearPhysicalPoints()
    {
        var samples = Samples(); samples[2] = Sample("c", 0, 10, 163.7, 200);
        FailVerify(Context(), samples, Tolerance());
    }

    private static void GridAngleMismatch()
    {
        var samples = Samples(); samples[1].CaptureY = 200.15;
        var tolerance = Tolerance(); tolerance.ResidualY = 1;
        FailVerify(Context(), samples, tolerance);
    }

    private static void RotatedGrid()
    {
        var samples = new List<InputStageReviewMeasurement>
        {
            Sample("a", 0, 0, 100, 200), Sample("b", 10, 0, 100, 283.7), Sample("c", 0, 10, 36.3, 200)
        };
        var evidence = Verify(Context(), samples, Tolerance());
        Near(evidence.GridAngleErrorX, 0); Near(evidence.GridAngleErrorY, 0);
        samples = new List<InputStageReviewMeasurement>
        {
            Sample("a", 0, 0, 100, 200), Sample("b", 10, 0, 16.3, 200), Sample("c", 0, 10, 100, 136.3)
        };
        Verify(Context(), samples, Tolerance());
    }

    private static void StageTheta()
    {
        var samples = Samples(); samples[0].CaptureT -= 360;
        Verify(Context(), samples, Tolerance());
        samples[0].CaptureT += .02;
        FailVerify(Context(), samples, Tolerance());
    }

    private static void NearCollinearLayout()
    {
        var context = Context();
        var samples = new List<InputStageReviewMeasurement>
        {
            Sample("a", 0, 0, 100, 200),
            Sample("b", 31, 30, 100 + 31 * 8.37, 200 + 30 * 6.37),
            Sample("c", 32, 31, 100 + 32 * 8.37, 200 + 31 * 6.37)
        };
        string reason;
        Assert(!InputStageReviewGeometryPolicy.CheckVerificationPointLayout(samples, out reason), "thin triangle accepted for movement");
        Assert(reason.Contains("배치") && reason.Contains("다시 선택"), "layout failure did not explain point reselection");
        // Synthetic perturbation to reproduce conditioning sensitivity; this does not assert equipment noise is 1 um.
        samples[1].RawVisionDeltaX = .001;
        InputStageReviewGeometryEvidence evidence;
        Assert(!InputStageReviewGeometryPolicy.TryVerify(context, samples, Tolerance(), out evidence, out reason), "thin triangle yielded approval");
        Assert(reason.Contains("배치") && !reason.Contains("실측 격자 각도"), "conditioning failure was misreported as measured grid angle failure");
        Assert(evidence == null, "layout rejection issued evidence");
    }

    private static void LayoutTransformInvariance()
    {
        int[][] orders = { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 }, new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
        foreach (double scale in new[] { .01, 1.0, 1000.0 })
            foreach (double angle in new[] { 0.0, 37.0, 90.0, 180.0 })
                foreach (double mirror in new[] { -1.0, 1.0 })
                    foreach (int[] order in orders)
                    {
                        var original = Samples();
                        var samples = new List<InputStageReviewMeasurement>();
                        double radians = angle * Math.PI / 180.0;
                        foreach (int index in order)
                        {
                            var point = original[index].Clone();
                            double x = point.ExpectedX * scale * mirror;
                            double y = point.ExpectedY * scale;
                            point.ExpectedX = point.CaptureX = Math.Cos(radians) * x - Math.Sin(radians) * y;
                            point.ExpectedY = point.CaptureY = Math.Sin(radians) * x + Math.Cos(radians) * y;
                            samples.Add(point);
                        }
                        string reason;
                        Assert(InputStageReviewGeometryPolicy.CheckVerificationPointLayout(samples, out reason), reason);
                        var context = Context();
                        context.PitchX *= scale; context.PitchY *= scale;
                        Verify(context, samples, Tolerance());
                    }
    }

    private static void LayoutIndependentLimits()
    {
        var points = Samples();
        string reason;
        Assert(InputStageReviewGeometryPolicy.CheckVerificationPointLayout(points, out reason), reason);
        var context = Context();
        // Layout does not become invalid when an independently permitted whole-map translation exceeds half pitch.
        var candidate = Allow(context, 8.218, .2, Limits());
        foreach (var point in points)
        {
            point.ExpectedX += candidate.OffsetX; point.CaptureX += candidate.OffsetX;
            point.ExpectedY += candidate.OffsetY; point.CaptureY += candidate.OffsetY;
        }
        context.OriginX = candidate.OriginX; context.OriginY = candidate.OriginY;
        Assert(InputStageReviewGeometryPolicy.CheckVerificationPointLayout(points, out reason), reason);
        Verify(context, points, Tolerance());
        points[0].RawVisionDeltaX = .2;
        FailVerify(context, points, Tolerance());
    }

    private static void FourthPoint()
    {
        var samples = Samples(); var fourth = Sample("d", 10, 10, 183.7, 263.7); fourth.RawVisionDeltaY = 1;
        samples.Add(fourth); FailVerify(Context(), samples, Tolerance());
    }

    private static void VisionYSign()
    {
        var samples = Samples();
        foreach (var sample in samples) { sample.CaptureY += .2; sample.RawVisionDeltaY = .2; }
        Verify(Context(), samples, Tolerance());
        foreach (var sample in samples) sample.RawVisionDeltaY = -.2;
        FailVerify(Context(), samples, Tolerance());
    }

    private static void InconsistentExpectedPoint()
    {
        var samples = Samples(); samples.Add(Sample("d", 1, 1, 108.4, 206.37));
        FailVerify(Context(), samples, Tolerance());
    }

    private static void StaleContexts()
    {
        var original = Context(); var evidence = Verify(original, Samples(), Tolerance());
        Action<InputStageReviewGeometryContext>[] changes =
        {
            context => context.SessionGeneration++, context => context.RequestGeneration++, context => context.WaferId = "other",
            context => context.MappingRevision = "map-2", context => context.ConditionSignature = "recipe-align-calibration-2",
            context => context.CandidateSignature = "candidate-2", context => context.StageTheta += .0001,
            context => context.PitchX += .001, context => context.PitchY += .001,
            context => context.OriginX += .001, context => context.OriginY += .001,
            context => context.BaselineOriginX += .001, context => context.BaselineOriginY += .001,
            context => context.IsSimulation = true
        };
        foreach (var change in changes)
        {
            var current = original.Clone(); change(current); string reason;
            Assert(!InputStageReviewGeometryPolicy.IsEvidenceUsable(evidence, current, true, out reason), "stale evidence accepted");
        }
    }

    private static void AuthorizedPendingOffsetRebase()
    {
        var approved = Context();
        var beforeUpdate = approved.Clone();
        // Four completed picks change progress/result fields, which are deliberately outside this geometry context.
        var afterUpdate = beforeUpdate.Clone();
        afterUpdate.CandidateSignature = "candidate-after-pending-offset";

        InputStageReviewGeometryContext rebased;
        string reason;
        Assert(InputStageReviewGeometryPolicy.TryRebaseCandidateSignatureAfterAuthorizedUpdate(
            approved, beforeUpdate, afterUpdate, out rebased, out reason), reason);
        Assert(rebased != null && rebased.CandidateSignature == afterUpdate.CandidateSignature,
            "authorized candidate signature was not rebased");
        Assert(!object.ReferenceEquals(rebased, approved) && !object.ReferenceEquals(rebased, afterUpdate),
            "rebased context aliases caller state");
        Assert(InputStageReviewGeometryPolicy.IsSameContext(rebased, afterUpdate, out reason), reason);
        Assert(!InputStageReviewGeometryPolicy.IsSameContext(approved, afterUpdate, out reason),
            "strict ordinary context comparison accepted a coordinate change");
    }

    private static void AuthorizedPendingOffsetRejectsOtherChanges()
    {
        var approved = Context();
        var beforeUpdate = approved.Clone();
        Action<InputStageReviewGeometryContext>[] changes =
        {
            context => context.WaferId = "other", context => context.MappingRevision = "map-2",
            context => context.ConditionSignature = "condition-2", context => context.SessionGeneration++,
            context => context.RequestGeneration++, context => context.StageTheta += .001,
            context => context.PitchX += .001, context => context.PitchY += .001,
            context => context.OriginX += .001, context => context.OriginY += .001,
            context => context.BaselineOriginX += .001, context => context.BaselineOriginY += .001,
            context => context.IsSimulation = true
        };
        foreach (var change in changes)
        {
            var afterUpdate = beforeUpdate.Clone();
            afterUpdate.CandidateSignature = "candidate-after-pending-offset";
            change(afterUpdate);
            InputStageReviewGeometryContext rebased;
            string reason;
            Assert(!InputStageReviewGeometryPolicy.TryRebaseCandidateSignatureAfterAuthorizedUpdate(
                approved, beforeUpdate, afterUpdate, out rebased, out reason),
                "unrelated context change was accepted");
            Assert(rebased == null, "rejected context produced a rebased approval");
        }

        var staleBefore = beforeUpdate.Clone();
        staleBefore.CandidateSignature = "already-stale-before-update";
        InputStageReviewGeometryContext staleRebased;
        string staleReason;
        Assert(!InputStageReviewGeometryPolicy.TryRebaseCandidateSignatureAfterAuthorizedUpdate(
            approved, staleBefore, staleBefore.Clone(), out staleRebased, out staleReason),
            "pre-existing approval mismatch was accepted");
    }

    private static void EvidenceImmutable()
    {
        var context = Context(); var samples = Samples(); var tolerance = Tolerance();
        var evidence = Verify(context, samples, tolerance);
        context.OriginX = -100; samples[0].CaptureX = -100; tolerance.ResidualX = 100;
        evidence.Context.OriginX = -200; evidence.Measurements[0].CaptureX = -200; evidence.Tolerance.ResidualX = 200;
        Near(evidence.Context.OriginX, 100); Near(evidence.Measurements[0].CaptureX, 100); Near(evidence.Tolerance.ResidualX, .1);
        string reason;
        Assert(InputStageReviewGeometryPolicy.IsEvidenceUsable(evidence, Context(), true, out reason), reason);
    }

    private static void Simulation()
    {
        var context = Context(); context.IsSimulation = true;
        var evidence = Verify(context, Samples(), Tolerance()); string reason;
        Assert(!InputStageReviewGeometryPolicy.IsEvidenceUsable(evidence, context, true, out reason), "simulation accepted in production");
        Assert(InputStageReviewGeometryPolicy.IsEvidenceUsable(evidence, context, false, out reason), reason);
    }

    private static T JsonRoundTrip<T>(T value)
    {
        var serializer = new DataContractJsonSerializer(typeof(T));
        using (var stream = new MemoryStream())
        {
            serializer.WriteObject(stream, value);
            stream.Position = 0;
            return (T)serializer.ReadObject(stream);
        }
    }

    private static void EvidenceJsonRoundTrip()
    {
        var context = JsonRoundTrip(Context());
        var samples = JsonRoundTrip(Samples());
        var tolerance = JsonRoundTrip(Tolerance());
        var evidence = Verify(context, samples, tolerance);
        string reason;
        Assert(InputStageReviewGeometryPolicy.IsEvidenceUsable(evidence, Context(), true, out reason), reason);
        samples[1].CaptureX += .2;
        FailVerify(context, samples, tolerance);
        samples = JsonRoundTrip(Samples());
        samples[0].CorrespondenceEvidence = null;
        FailVerify(context, samples, tolerance);
    }
}
