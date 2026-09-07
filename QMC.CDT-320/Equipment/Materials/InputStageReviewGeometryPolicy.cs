using System;
using System.Collections.Generic;
using System.Globalization;

namespace QMC.CDT320.Materials
{
    public sealed class InputStageReviewOffsetLimits
    {
        public double SingleX { get; set; }
        public double SingleY { get; set; }
        public double CumulativeX { get; set; }
        public double CumulativeY { get; set; }
    }

    public sealed class InputStageReviewGeometryContext
    {
        public string WaferId { get; set; }
        public string MappingRevision { get; set; }
        public string ConditionSignature { get; set; }
        public string CandidateSignature { get; set; }
        public long SessionGeneration { get; set; }
        public long RequestGeneration { get; set; }
        public double StageTheta { get; set; }
        public double PitchX { get; set; }
        public double PitchY { get; set; }
        public double OriginX { get; set; }
        public double OriginY { get; set; }
        public double BaselineOriginX { get; set; }
        public double BaselineOriginY { get; set; }
        public bool IsSimulation { get; set; }

        public InputStageReviewGeometryContext Clone()
        {
            return (InputStageReviewGeometryContext)MemberwiseClone();
        }
    }

    public sealed class InputStageReviewOffsetCandidate
    {
        public double OriginX { get; private set; }
        public double OriginY { get; private set; }
        public double OffsetX { get; private set; }
        public double OffsetY { get; private set; }
        public double CumulativeX { get; private set; }
        public double CumulativeY { get; private set; }
        public double HalfPitchRatioX { get; private set; }
        public double HalfPitchRatioY { get; private set; }

        internal InputStageReviewOffsetCandidate(
            InputStageReviewGeometryContext context, double offsetX, double offsetY)
        {
            OriginX = context.OriginX + offsetX;
            OriginY = context.OriginY + offsetY;
            OffsetX = offsetX;
            OffsetY = offsetY;
            CumulativeX = OriginX - context.BaselineOriginX;
            CumulativeY = OriginY - context.BaselineOriginY;
            HalfPitchRatioX = Math.Abs(offsetX) / context.PitchX * 2.0;
            HalfPitchRatioY = Math.Abs(offsetY) / context.PitchY * 2.0;
        }
    }

    public sealed class InputStageReviewMeasurement
    {
        public string DieUid { get; set; }
        public int GridX { get; set; }
        public int GridY { get; set; }
        public double ExpectedX { get; set; }
        public double ExpectedY { get; set; }
        public double CaptureX { get; set; }
        public double CaptureY { get; set; }
        public double CaptureT { get; set; }
        public double RawVisionDeltaX { get; set; }
        public double RawVisionDeltaY { get; set; }
        public bool CorrespondenceConfirmed { get; set; }
        public string CorrespondenceEvidence { get; set; }

        public InputStageReviewMeasurement Clone()
        {
            return (InputStageReviewMeasurement)MemberwiseClone();
        }
    }

    public sealed class InputStageReviewGeometryTolerance
    {
        public double ResidualX { get; set; }
        public double ResidualY { get; set; }
        public double Pitch { get; set; }
        public double Theta { get; set; }
        public double StageTheta { get; set; }

        public InputStageReviewGeometryTolerance Clone()
        {
            return (InputStageReviewGeometryTolerance)MemberwiseClone();
        }
    }

    public sealed class InputStageReviewGeometryEvidence
    {
        private readonly InputStageReviewGeometryContext _context;
        private readonly InputStageReviewGeometryTolerance _tolerance;
        private readonly List<InputStageReviewMeasurement> _measurements;

        public InputStageReviewGeometryContext Context { get { return _context.Clone(); } }
        public InputStageReviewGeometryTolerance Tolerance { get { return _tolerance.Clone(); } }
        public IList<InputStageReviewMeasurement> Measurements
        {
            get { return _measurements.ConvertAll(sample => sample.Clone()).AsReadOnly(); }
        }
        public double MaximumResidualX { get; private set; }
        public double MaximumResidualY { get; private set; }
        public double MeasuredPitchX { get; private set; }
        public double MeasuredPitchY { get; private set; }
        public double GridAngleErrorX { get; private set; }
        public double GridAngleErrorY { get; private set; }

        internal InputStageReviewGeometryEvidence(
            InputStageReviewGeometryContext context,
            InputStageReviewGeometryTolerance tolerance,
            IList<InputStageReviewMeasurement> measurements,
            double residualX, double residualY,
            double pitchX, double pitchY, double angleX, double angleY)
        {
            _context = context.Clone();
            _tolerance = tolerance.Clone();
            _measurements = new List<InputStageReviewMeasurement>();
            foreach (InputStageReviewMeasurement measurement in measurements)
                _measurements.Add(measurement.Clone());
            MaximumResidualX = residualX;
            MaximumResidualY = residualY;
            MeasuredPitchX = pitchX;
            MeasuredPitchY = pitchY;
            GridAngleErrorX = angleX;
            GridAngleErrorY = angleY;
        }
    }

    /// <summary>
    /// Review 후보만 계산하고 검사한다. Material, Draft, 모션 또는 운영 설정을 변경하지 않는다.
    /// 물리 다이 대응 근거는 호출자가 확인해야 하며 반복 패턴 검출 성공만으로 만들 수 없다.
    /// </summary>
    public static class InputStageReviewGeometryPolicy
    {
        // 부동소수점 덧셈의 표현 오차만 흡수한다. 장비의 품질 허용치는 별도 설정값이다.
        private const double NumericTolerance = 1e-9;
        private const double GeometryNumericTolerance = 1e-6;
        // 검증점 선택의 형상 기준이다. OFFSET 또는 장비의 XY/T 품질 허용치가 아니다.
        // 최장변 대비 최소 높이 10%를 확보하여 거의 일직선인 점의 오차 확대를 피한다.
        private const double MinimumVerificationTriangleHeightRatio = 0.1;

        public static bool TryCreateOffsetCandidate(
            InputStageReviewGeometryContext context,
            double offsetX, double offsetY,
            InputStageReviewOffsetLimits limits,
            out InputStageReviewOffsetCandidate candidate,
            out string reason)
        {
            candidate = null;
            context = context != null ? context.Clone() : null;
            if (!CheckContext(context, out reason))
                return false;
            if (limits == null || !IsPositiveFinite(limits.SingleX) || !IsPositiveFinite(limits.SingleY) ||
                !IsPositiveFinite(limits.CumulativeX) || !IsPositiveFinite(limits.CumulativeY))
                return Fail("단발/누적 OFFSET 한계가 유효한 양수가 아닙니다.", out reason);
            if (!IsFinite(offsetX) || !IsFinite(offsetY))
                return Fail("OFFSET 값이 유효하지 않습니다.", out reason);

            var proposed = new InputStageReviewOffsetCandidate(context, offsetX, offsetY);
            if (!IsFinite(proposed.OriginX) || !IsFinite(proposed.OriginY) ||
                !IsFinite(proposed.CumulativeX) || !IsFinite(proposed.CumulativeY) ||
                !IsFinite(proposed.HalfPitchRatioX) || !IsFinite(proposed.HalfPitchRatioY))
                return Fail("OFFSET 후보 원점 또는 누적량 계산이 유효하지 않습니다.", out reason);
            if (Exceeds(Math.Abs(offsetX), limits.SingleX) || Exceeds(Math.Abs(offsetY), limits.SingleY))
                return Fail("단발 OFFSET 한계를 초과했습니다. X=" + Format(offsetX) +
                    "/" + Format(limits.SingleX) + ", Y=" + Format(offsetY) + "/" + Format(limits.SingleY), out reason);
            if (Exceeds(Math.Abs(proposed.CumulativeX), limits.CumulativeX) ||
                Exceeds(Math.Abs(proposed.CumulativeY), limits.CumulativeY))
                return Fail("누적 OFFSET 한계를 초과했습니다. X=" + Format(proposed.CumulativeX) +
                    "/" + Format(limits.CumulativeX) + ", Y=" + Format(proposed.CumulativeY) +
                    "/" + Format(limits.CumulativeY), out reason);

            // 반 피치 비율은 진단용이다. 수동 Review의 허용 범위를 반 피치로 축소하지 않는다.
            candidate = proposed;
            reason = string.Empty;
            return true;
        }

        public static bool IsSameContext(
            InputStageReviewGeometryContext expected,
            InputStageReviewGeometryContext current,
            out string reason)
        {
            if (!CheckContext(expected, out reason) || !CheckContext(current, out reason))
                return false;
            if (!string.Equals(expected.WaferId, current.WaferId, StringComparison.Ordinal) ||
                !string.Equals(expected.MappingRevision, current.MappingRevision, StringComparison.Ordinal) ||
                !string.Equals(expected.ConditionSignature, current.ConditionSignature, StringComparison.Ordinal) ||
                !string.Equals(expected.CandidateSignature, current.CandidateSignature, StringComparison.Ordinal) ||
                expected.SessionGeneration != current.SessionGeneration ||
                expected.RequestGeneration != current.RequestGeneration ||
                expected.StageTheta != current.StageTheta || expected.PitchX != current.PitchX || expected.PitchY != current.PitchY ||
                expected.OriginX != current.OriginX || expected.OriginY != current.OriginY ||
                expected.BaselineOriginX != current.BaselineOriginX || expected.BaselineOriginY != current.BaselineOriginY ||
                expected.IsSimulation != current.IsSimulation)
                return Fail("Review 검출/검증 이후 세션, 후보 좌표 또는 장비 조건이 변경되었습니다. 다시 검증하십시오.", out reason);
            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// 저장 승인이 변경 직전 context와 완전히 같고, 명시적으로 허용된 변경 뒤에는
        /// 후보 좌표 서명만 달라졌을 때 승인 context를 새 후보 좌표로 재기준화한다.
        /// 호출자는 임의 편집이 아닌 단일 허용 좌표 갱신 경로에서만 사용해야 한다.
        /// </summary>
        public static bool TryRebaseCandidateSignatureAfterAuthorizedUpdate(
            InputStageReviewGeometryContext approved,
            InputStageReviewGeometryContext beforeUpdate,
            InputStageReviewGeometryContext afterUpdate,
            out InputStageReviewGeometryContext rebased,
            out string reason)
        {
            rebased = null;
            if (!IsSameContext(approved, beforeUpdate, out reason))
                return Fail("허용 좌표 갱신 직전 context가 저장 승인과 다릅니다. " + reason, out reason);
            if (!CheckContext(afterUpdate, out reason))
                return false;
            if (!string.Equals(beforeUpdate.WaferId, afterUpdate.WaferId, StringComparison.Ordinal) ||
                !string.Equals(beforeUpdate.MappingRevision, afterUpdate.MappingRevision, StringComparison.Ordinal) ||
                !string.Equals(beforeUpdate.ConditionSignature, afterUpdate.ConditionSignature, StringComparison.Ordinal) ||
                beforeUpdate.SessionGeneration != afterUpdate.SessionGeneration ||
                beforeUpdate.RequestGeneration != afterUpdate.RequestGeneration ||
                beforeUpdate.StageTheta != afterUpdate.StageTheta ||
                beforeUpdate.PitchX != afterUpdate.PitchX || beforeUpdate.PitchY != afterUpdate.PitchY ||
                beforeUpdate.OriginX != afterUpdate.OriginX || beforeUpdate.OriginY != afterUpdate.OriginY ||
                beforeUpdate.BaselineOriginX != afterUpdate.BaselineOriginX ||
                beforeUpdate.BaselineOriginY != afterUpdate.BaselineOriginY ||
                beforeUpdate.IsSimulation != afterUpdate.IsSimulation)
                return Fail("허용된 후보 좌표 서명 이외의 Review context가 함께 변경되었습니다.", out reason);

            rebased = afterUpdate.Clone();
            reason = string.Empty;
            return true;
        }

        public static bool TryVerify(
            InputStageReviewGeometryContext context,
            IList<InputStageReviewMeasurement> samples,
            InputStageReviewGeometryTolerance tolerance,
            out InputStageReviewGeometryEvidence evidence,
            out string reason)
        {
            evidence = null;
            // 호출자의 DTO를 보관하지 않고 판정값과 발행하는 검증 증거의 값을 일치시킨다.
            context = context != null ? context.Clone() : null;
            tolerance = tolerance != null ? tolerance.Clone() : null;
            if (samples != null)
            {
                var snapshot = new List<InputStageReviewMeasurement>();
                foreach (InputStageReviewMeasurement sample in samples)
                    snapshot.Add(sample != null ? sample.Clone() : null);
                samples = snapshot;
            }
            if (!CheckContext(context, out reason))
                return false;
            if (tolerance == null || !IsPositiveFinite(tolerance.ResidualX) || !IsPositiveFinite(tolerance.ResidualY) ||
                !IsPositiveFinite(tolerance.Pitch) || !IsPositiveFinite(tolerance.Theta) || !IsPositiveFinite(tolerance.StageTheta))
                return Fail("Review 좌표/T/pitch 품질 허용치가 설정되지 않았거나 유효하지 않습니다.", out reason);
            if (samples == null || samples.Count < 3)
                return Fail("대응이 확인된 비공선 검증점이 3개 이상 필요합니다.", out reason);

            var dieIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var indices = new HashSet<string>(StringComparer.Ordinal);
            double maxResidualX = 0.0;
            double maxResidualY = 0.0;
            foreach (InputStageReviewMeasurement sample in samples)
            {
                if (sample == null || string.IsNullOrWhiteSpace(sample.DieUid) || !dieIds.Add(sample.DieUid) ||
                    !indices.Add(sample.GridX.ToString(CultureInfo.InvariantCulture) + ":" + sample.GridY.ToString(CultureInfo.InvariantCulture)))
                    return Fail("검증점의 다이 UID 또는 격자 index가 비어 있거나 중복되었습니다.", out reason);
                if (!sample.CorrespondenceConfirmed || string.IsNullOrWhiteSpace(sample.CorrespondenceEvidence))
                    return Fail("검증점의 실제 다이/index 대응 근거가 없습니다. die=" + sample.DieUid, out reason);
                if (!IsFinite(sample.ExpectedX) || !IsFinite(sample.ExpectedY) || !IsFinite(sample.CaptureX) ||
                    !IsFinite(sample.CaptureY) || !IsFinite(sample.CaptureT) || !IsFinite(sample.RawVisionDeltaX) ||
                    !IsFinite(sample.RawVisionDeltaY))
                    return Fail("검증점의 촬영 좌표 또는 Vision 측정값이 유효하지 않습니다. die=" + sample.DieUid, out reason);
                if (Exceeds(Math.Abs(GetAngleDifference(sample.CaptureT, context.StageTheta)), tolerance.StageTheta))
                    return Fail("검증점 촬영 시 Stage T가 최종 검증 기준과 다릅니다. die=" + sample.DieUid, out reason);

                // 기존 InputStage 부호를 유지한다. 각 점을 중심 이동했더라도 원래 예상 위치와 비교한다.
                double detectedX = sample.CaptureX + sample.RawVisionDeltaX;
                double detectedY = sample.CaptureY - sample.RawVisionDeltaY;
                double residualX = detectedX - sample.ExpectedX;
                double residualY = detectedY - sample.ExpectedY;
                if (!IsFinite(detectedX) || !IsFinite(detectedY) || !IsFinite(residualX) || !IsFinite(residualY))
                    return Fail("검증점 좌표/잔차 계산이 유효하지 않습니다. die=" + sample.DieUid, out reason);
                maxResidualX = Math.Max(maxResidualX, Math.Abs(residualX));
                maxResidualY = Math.Max(maxResidualY, Math.Abs(residualY));
                if (Exceeds(Math.Abs(residualX), tolerance.ResidualX) || Exceeds(Math.Abs(residualY), tolerance.ResidualY))
                    return Fail("후보 예상 좌표의 검증 잔차가 허용치를 초과했습니다. die=" + sample.DieUid +
                        ", X=" + Format(residualX) + ", Y=" + Format(residualY), out reason);
            }

            int first;
            int second;
            int third;
            if (!CheckVerificationPointLayout(samples, out reason))
                return false;
            if (!TrySelectNonCollinearTriangle(samples, out first, out second, out third))
                return Fail("검증점이 일직선이어서 X/Y pitch와 격자 각도를 모두 확인할 수 없습니다.", out reason);

            double expectedColumnX, expectedColumnY, expectedRowX, expectedRowY;
            double measuredColumnX, measuredColumnY, measuredRowX, measuredRowY;
            ResolveBasis(samples[first], samples[second], samples[third], false,
                out expectedColumnX, out expectedColumnY, out expectedRowX, out expectedRowY);
            ResolveBasis(samples[first], samples[second], samples[third], true,
                out measuredColumnX, out measuredColumnY, out measuredRowX, out measuredRowY);
            double expectedPitchX = GetLength(expectedColumnX, expectedColumnY);
            double expectedPitchY = GetLength(expectedRowX, expectedRowY);
            double measuredPitchX = GetLength(measuredColumnX, measuredColumnY);
            double measuredPitchY = GetLength(measuredRowX, measuredRowY);
            if (!IsPositiveFinite(expectedPitchX) || !IsPositiveFinite(expectedPitchY) ||
                !IsPositiveFinite(measuredPitchX) || !IsPositiveFinite(measuredPitchY))
                return Fail("X/Y pitch를 계산할 수 있는 유효한 검증점 간격이 없습니다.", out reason);
            double expectedCross = expectedColumnX / expectedPitchX * (expectedRowY / expectedPitchY) -
                expectedColumnY / expectedPitchX * (expectedRowX / expectedPitchY);
            if (!IsFinite(expectedCross) || Math.Abs(expectedCross) <= GeometryNumericTolerance)
                return Fail("예상 좌표가 비공선 X/Y 격자를 구성하지 않습니다.", out reason);
            if (Math.Abs(expectedPitchX - context.PitchX) > GeometryNumericTolerance ||
                Math.Abs(expectedPitchY - context.PitchY) > GeometryNumericTolerance)
                return Fail("검증점 예상 좌표와 후보 맵 pitch가 일치하지 않습니다.", out reason);
            foreach (InputStageReviewMeasurement sample in samples)
            {
                double indexX = (double)sample.GridX - samples[first].GridX;
                double indexY = (double)sample.GridY - samples[first].GridY;
                double expectedX = samples[first].ExpectedX + indexX * expectedColumnX + indexY * expectedRowX;
                double expectedY = samples[first].ExpectedY + indexX * expectedColumnY + indexY * expectedRowY;
                if (!IsFinite(expectedX) || !IsFinite(expectedY) ||
                    Math.Abs(expectedX - sample.ExpectedX) > GeometryNumericTolerance ||
                    Math.Abs(expectedY - sample.ExpectedY) > GeometryNumericTolerance)
                    return Fail("검증점 예상 좌표가 동일한 후보 격자를 구성하지 않습니다. die=" + sample.DieUid, out reason);
            }

            double angleX = GetAngleDifference(Math.Atan2(measuredColumnY, measuredColumnX) * 180.0 / Math.PI,
                Math.Atan2(expectedColumnY, expectedColumnX) * 180.0 / Math.PI);
            double angleY = GetAngleDifference(Math.Atan2(measuredRowY, measuredRowX) * 180.0 / Math.PI,
                Math.Atan2(expectedRowY, expectedRowX) * 180.0 / Math.PI);
            if (Exceeds(Math.Abs(measuredPitchX - context.PitchX), tolerance.Pitch) ||
                Exceeds(Math.Abs(measuredPitchY - context.PitchY), tolerance.Pitch))
                return Fail("실측 pitch가 후보 맵 pitch 허용 차이를 초과했습니다. X=" + Format(measuredPitchX) +
                    "/" + Format(context.PitchX) + ", Y=" + Format(measuredPitchY) + "/" + Format(context.PitchY), out reason);
            if (!IsFinite(angleX) || !IsFinite(angleY) || Exceeds(Math.Abs(angleX), tolerance.Theta) || Exceeds(Math.Abs(angleY), tolerance.Theta))
                return Fail("실측 격자 각도가 후보 맵 각도 허용 차이를 초과했습니다. X=" + Format(angleX) + ", Y=" + Format(angleY), out reason);

            evidence = new InputStageReviewGeometryEvidence(context, tolerance, samples, maxResidualX, maxResidualY,
                measuredPitchX, measuredPitchY, angleX, angleY);
            reason = string.Empty;
            return true;
        }

        public static bool TryValidateMeasuredPitch(
            double measured, double configured, double tolerance, out string reason)
        {
            if (!IsPositiveFinite(measured) || !IsPositiveFinite(configured) || !IsPositiveFinite(tolerance))
                return Fail("실측/설정 pitch 또는 비교 허용치가 유효한 양수가 아닙니다.", out reason);
            if (Exceeds(Math.Abs(measured - configured), tolerance))
                return Fail("실측 pitch가 설정값의 허용 차이를 초과했습니다. measured=" + Format(measured) +
                    ", configured=" + Format(configured) + ", tolerance=" + Format(tolerance), out reason);
            reason = string.Empty;
            return true;
        }

        public static bool CheckVerificationPointLayout(IList<InputStageReviewMeasurement> samples, out string reason)
        {
            if (samples == null || samples.Count < 3)
                return Fail("검증점이 3개 이상 필요합니다.", out reason);
            foreach (InputStageReviewMeasurement sample in samples)
                if (sample == null || !IsFinite(sample.ExpectedX) || !IsFinite(sample.ExpectedY))
                    return Fail("검증점 예상 좌표가 유효하지 않습니다.", out reason);
            int first;
            int second;
            int third;
            if (!TrySelectNonCollinearTriangle(samples, out first, out second, out third))
                return Fail("검증점이 거의 한 직선이거나 너무 가늘게 배치되었습니다. 서로 떨어진 삼각형으로 다시 선택하세요. " +
                    "최소 높이가 최장변의 10% 이상이어야 합니다. 이는 검증점 배치 기준이며 OFFSET/정밀도 한계가 아닙니다.", out reason);
            reason = string.Empty;
            return true;
        }

        public static bool IsEvidenceUsable(
            InputStageReviewGeometryEvidence evidence,
            InputStageReviewGeometryContext current,
            bool production,
            out string reason)
        {
            if (evidence == null)
                return Fail("현재 후보 맵에 대한 다점 좌표 검증 증거가 없습니다.", out reason);
            if (!IsSameContext(evidence.Context, current, out reason))
                return false;
            if (production && evidence.Context.IsSimulation)
                return Fail("Simulation 검증 증거는 실제 생산 승인에 사용할 수 없습니다.", out reason);
            reason = string.Empty;
            return true;
        }

        private static bool CheckContext(InputStageReviewGeometryContext context, out string reason)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.WaferId) ||
                string.IsNullOrWhiteSpace(context.MappingRevision) || string.IsNullOrWhiteSpace(context.ConditionSignature) ||
                string.IsNullOrWhiteSpace(context.CandidateSignature) || context.SessionGeneration <= 0 || context.RequestGeneration <= 0)
                return Fail("Review wafer/맵/조건/후보 또는 세션 기준이 없습니다.", out reason);
            if (!IsPositiveFinite(context.PitchX) || !IsPositiveFinite(context.PitchY) || !IsFinite(context.StageTheta) ||
                !IsFinite(context.OriginX) || !IsFinite(context.OriginY) ||
                !IsFinite(context.BaselineOriginX) || !IsFinite(context.BaselineOriginY))
                return Fail("Review 기준 원점, T 또는 pitch가 유효하지 않습니다.", out reason);
            reason = string.Empty;
            return true;
        }

        private static bool TrySelectNonCollinearTriangle(
            IList<InputStageReviewMeasurement> samples, out int first, out int second, out int third)
        {
            first = second = third = 0;
            double maximumArea = 0.0;
            for (int a = 0; a < samples.Count - 2; a++)
                for (int b = a + 1; b < samples.Count - 1; b++)
                    for (int c = b + 1; c < samples.Count; c++)
                    {
                        double area = Math.Abs(((double)samples[b].GridX - samples[a].GridX) * ((double)samples[c].GridY - samples[a].GridY) -
                            ((double)samples[c].GridX - samples[a].GridX) * ((double)samples[b].GridY - samples[a].GridY));
                        double heightRatio = GetVerificationTriangleHeightRatio(samples[a], samples[b], samples[c]);
                        if (area > maximumArea && IsFinite(heightRatio) &&
                            !Exceeds(MinimumVerificationTriangleHeightRatio, heightRatio))
                        {
                            maximumArea = area;
                            first = a;
                            second = b;
                            third = c;
                        }
                    }
            return maximumArea > 0.0;
        }

        private static double GetVerificationTriangleHeightRatio(
            InputStageReviewMeasurement a, InputStageReviewMeasurement b, InputStageReviewMeasurement c)
        {
            double bx = b.ExpectedX - a.ExpectedX;
            double by = b.ExpectedY - a.ExpectedY;
            double cx = c.ExpectedX - a.ExpectedX;
            double cy = c.ExpectedY - a.ExpectedY;
            double scale = Math.Max(Math.Max(Math.Abs(bx), Math.Abs(by)), Math.Max(Math.Abs(cx), Math.Abs(cy)));
            if (!IsPositiveFinite(scale))
                return 0.0;
            bx /= scale;
            by /= scale;
            cx /= scale;
            cy /= scale;
            double longestEdgeSquared = Math.Max(bx * bx + by * by,
                Math.Max(cx * cx + cy * cy, (cx - bx) * (cx - bx) + (cy - by) * (cy - by)));
            return Math.Abs(bx * cy - by * cx) / longestEdgeSquared;
        }

        private static void ResolveBasis(
            InputStageReviewMeasurement a, InputStageReviewMeasurement b, InputStageReviewMeasurement c, bool measured,
            out double columnX, out double columnY, out double rowX, out double rowY)
        {
            double indexBX = (double)b.GridX - a.GridX;
            double indexBY = (double)b.GridY - a.GridY;
            double indexCX = (double)c.GridX - a.GridX;
            double indexCY = (double)c.GridY - a.GridY;
            double determinant = indexBX * indexCY - indexCX * indexBY;
            double aX = measured ? a.CaptureX + a.RawVisionDeltaX : a.ExpectedX;
            double aY = measured ? a.CaptureY - a.RawVisionDeltaY : a.ExpectedY;
            double bx = (measured ? b.CaptureX + b.RawVisionDeltaX : b.ExpectedX) - aX;
            double by = (measured ? b.CaptureY - b.RawVisionDeltaY : b.ExpectedY) - aY;
            double cx = (measured ? c.CaptureX + c.RawVisionDeltaX : c.ExpectedX) - aX;
            double cy = (measured ? c.CaptureY - c.RawVisionDeltaY : c.ExpectedY) - aY;
            columnX = (bx * indexCY - cx * indexBY) / determinant;
            columnY = (by * indexCY - cy * indexBY) / determinant;
            rowX = (cx * indexBX - bx * indexCX) / determinant;
            rowY = (cy * indexBX - by * indexCX) / determinant;
        }

        private static double GetLength(double x, double y)
        {
            double maximum = Math.Max(Math.Abs(x), Math.Abs(y));
            if (maximum == 0.0)
                return 0.0;
            return maximum * Math.Sqrt(x / maximum * (x / maximum) + y / maximum * (y / maximum));
        }

        private static double GetAngleDifference(double actual, double expected)
        {
            double delta = (actual % 360.0 - expected % 360.0) % 360.0;
            if (delta > 180.0)
                delta -= 360.0;
            else if (delta < -180.0)
                delta += 360.0;
            return delta;
        }

        private static bool Exceeds(double value, double limit)
        {
            return value > limit && value - limit > NumericTolerance;
        }

        private static bool IsPositiveFinite(double value) { return IsFinite(value) && value > 0.0; }
        private static bool IsFinite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static bool Fail(string message, out string reason) { reason = message; return false; }
        private static string Format(double value) { return value.ToString("F6", CultureInfo.InvariantCulture); }
    }
}
