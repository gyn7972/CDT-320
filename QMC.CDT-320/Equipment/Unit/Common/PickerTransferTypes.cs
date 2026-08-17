using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    public enum PickerPickUpSeparateMode
    {
        Simultaneous = 0,
        NeedleFirst = 1,
        PickerFirst = 2
    }

    public enum PickerPickUpZMotionMode
    {
        Detailed = 0,
        SimpleZDownVacuumUp = 1
    }

    public enum PickerPickUpTransferMotionMode
    {
        Default = 0,
        ContiSegmentedPickUp = 2
    }

    public enum PickerBottomFlyingZDownMode
    {
        Off = 0,
        DownDistance = 1,
        ToBottomPosition = 2
    }

    public enum PickerPlaceMotionMode
    {
        Default = 0,
        ContiSegmentedPlace = 2
    }

    [DataContract]
    public sealed class PickerPickUpMotionConfig
    {
        public const double MinimumPickerSafeForWaferStageDistance = 2.0;
        public const int MechanicalOffsetPickerCount = 4;
        public const double DefaultMechanicalOffsetLimitMm = 1.0;
        public const double MaximumMechanicalOffsetLimitMm = 2.0;
        // T 기구 보정 한계(deg) — 2026-08-16 팀장님 지시로 T 기구 보정 신설. X/Y 한계와 대칭(기본 1.0, 상한 2.0).
        public const double DefaultMechanicalOffsetLimitTDeg = 1.0;
        public const double MaximumMechanicalOffsetLimitTDeg = 2.0;
        public const double LegacyPickUpMechanicalOffsetXmm = 0.020;

        [DataMember] public PickerPickUpZMotionMode MotionMode { get; set; } = PickerPickUpZMotionMode.Detailed;
        [DataMember] public PickerPickUpTransferMotionMode TransferMotionMode { get; set; } = PickerPickUpTransferMotionMode.Default;
        [DataMember] public double MechanicalOffsetLimitMm { get; set; } = DefaultMechanicalOffsetLimitMm;
        [DataMember] public double MechanicalOffsetTLimitDeg { get; set; } = DefaultMechanicalOffsetLimitTDeg;
        [DataMember] public double[] MechanicalOffsetX { get; set; } =
            new double[] { LegacyPickUpMechanicalOffsetXmm, LegacyPickUpMechanicalOffsetXmm, LegacyPickUpMechanicalOffsetXmm, LegacyPickUpMechanicalOffsetXmm };
        [DataMember] public double[] MechanicalOffsetY { get; set; } = new double[MechanicalOffsetPickerCount];
        // PickerT 축 기구 보정(deg) — 이동식에서 PickerT에 가산(+). 런타임 T(감산)와 같은 축이라
        // 이관식은 기구T′ = 기구T − 필터T 로 코드 확정(2026-08-16).
        [DataMember] public double[] MechanicalOffsetT { get; set; } = new double[MechanicalOffsetPickerCount];
        [DataMember] public int TransferContiCoordinate { get; set; } = 2;
        [DataMember] public int TransferContiTimeoutMs { get; set; } = 5000;
        [DataMember] public double TransferContiMaxTravelDistance { get; set; } = 45.0;
        [DataMember] public double TransferContiPickerYMaxCorrectionDistance { get; set; } = 1.5;
        [DataMember] public double TransferContiXYMidRatio { get; set; } = 0.5;
        [DataMember] public double TransferContiSplineCurvePercent { get; set; } = 100.0;
        // 삭제(사용자 확정 속도 모델 2026-07-26): TransferContiMaxVelocity/Acc/Dec,
        // TransferContiNode0~3SpeedPercent — 이송 속도는 각 축 DefaultVelocity × 전역 스케일로
        // 일원화되어 폐지. 구버전 설정 파일의 해당 키는 역직렬화 시 무시된다.
        [DataMember] public bool TransferContiUseGlobalSpeedScale { get; set; } = true;
        [DataMember] public double PickerZPrePickDistance { get; set; } = 1.0;
        [DataMember] public double PickerZSlowApproachSpeedPercent { get; set; } = 1.0;
        [DataMember] public double PickerZSyncLiftDistance { get; set; } = 2.0;
        [DataMember] public double PickerZSyncLiftVelocity { get; set; } = 5.0;
        [DataMember] public double PickerZSyncLiftAcceleration { get; set; } = 100.0;
        [DataMember] public double PickerZSyncLiftDeceleration { get; set; } = 100.0;
        [DataMember] public double PickerZSeparateDistance { get; set; } = 1.0;
        [DataMember] public double PickerZSeparateSpeedPercent { get; set; } = 1.0;
        [DataMember] public double PickerZAvoidReturnSpeedPercent { get; set; } = 10.0;
        [DataMember] public double PickerSafeForWaferStageDistance { get; set; } = MinimumPickerSafeForWaferStageDistance;
        [DataMember] public PickerPickUpSeparateMode SeparateMode { get; set; } = PickerPickUpSeparateMode.Simultaneous;
        [DataMember] public int VacuumOnBeforePickDelayMs { get; set; } = 0;
        [DataMember] public int NeedleVacuumOffSettleBeforeXYMs { get; set; } = 100;
        [DataMember] public int SyncLiftSettleMs { get; set; } = 0;
        [DataMember] public int PickSettleMs { get; set; } = 0;

        // PickUp Z 선행(사용자 승인 2026-07-26): 1-A(Y 전진 ∥ Z PrePick 선행) 스위치. 기본 Off.
        [DataMember] public bool PickUpEntryZPreDownMode { get; set; } = false;
        // 반경 게이트: die 목표 NeedleX/StageY와 Needle 작업영역 중심의 거리 ≤ 이 값일 때만 선행.
        // 사용 시 런타임 Needle 작업영역 반경(ResolveNeedleWorkAreaRadius)으로 상한 클램프한다.
        [DataMember] public double PreDownNeedleWorkRadiusMm { get; set; } = 130.0;

        // [동적 선행 대기점, 지시서 2026-07-27] 촬영(선행검사) 진행 중 대기 픽커 X를
        // "배치 maxVisionX + 팔로잉 클리어런스(+여유)"까지 선행 접근시키는 스위치.
        // Auto + ContiSegmentedPickUp에서만 동작. 기본 Off.
        [DataMember] public bool PickUpDynamicWaitMode { get; set; } = false;
        // 동적 대기점 여유 가산(mm, 현장 튜닝용) — 팔로잉 클리어런스에 더해진다. 기본 0.
        [DataMember] public double DynamicWaitExtraMarginMm { get; set; } = 0.0;

        // Legacy values are kept only for reading old config files.
        [DataMember] public double PickerZSlowApproachVelocity { get; set; } = 0.0;
        [DataMember] public double PickerZSlowApproachAcceleration { get; set; } = 0.0;
        [DataMember] public double PickerZSlowApproachDeceleration { get; set; } = 0.0;
        [DataMember] public double SyncLiftDistance { get; set; } = 0.0;
        [DataMember] public double SyncLiftSpeedPercent { get; set; } = 0.0;
        [DataMember] public double NeedleSeparateDistance { get; set; } = 0.0;
        [DataMember] public double PickerSeparateDistance { get; set; } = 0.0;
        [DataMember] public double SeparateSpeedPercent { get; set; } = 0.0;
        [DataMember] public double PickerZSeparateVelocity { get; set; } = 0.0;
        [DataMember] public double PickerZSeparateAcceleration { get; set; } = 0.0;
        [DataMember] public double PickerZSeparateDeceleration { get; set; } = 0.0;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            TransferContiSplineCurvePercent = 100.0;
            TransferContiUseGlobalSpeedScale = true;
            NeedleVacuumOffSettleBeforeXYMs = 100;
            // 구버전에는 기구 보정 필드가 없고 PickUp X에 +0.020 mm가 코드로 고정되어 있었다.
            MechanicalOffsetLimitMm = DefaultMechanicalOffsetLimitMm;
            MechanicalOffsetTLimitDeg = DefaultMechanicalOffsetLimitTDeg;
            MechanicalOffsetX =
                new double[] { LegacyPickUpMechanicalOffsetXmm, LegacyPickUpMechanicalOffsetXmm, LegacyPickUpMechanicalOffsetXmm, LegacyPickUpMechanicalOffsetXmm };
            MechanicalOffsetY = new double[MechanicalOffsetPickerCount];
            MechanicalOffsetT = new double[MechanicalOffsetPickerCount];
            // 구버전 설정 파일 하위호환: 멤버 부재 시 기본값 보장(스위치는 안전측 Off).
            PickUpEntryZPreDownMode = false;
            PreDownNeedleWorkRadiusMm = 130.0;
            PickUpDynamicWaitMode = false;
            DynamicWaitExtraMarginMm = 0.0;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            Ensure();
        }

        public void Ensure()
        {
            MechanicalOffsetLimitMm = NormalizeMechanicalOffsetLimit(MechanicalOffsetLimitMm);
            MechanicalOffsetTLimitDeg = NormalizeMechanicalOffsetLimit(MechanicalOffsetTLimitDeg);
            MechanicalOffsetX = EnsureMechanicalOffsetArray(
                MechanicalOffsetX,
                LegacyPickUpMechanicalOffsetXmm,
                MechanicalOffsetLimitMm);
            MechanicalOffsetY = EnsureMechanicalOffsetArray(MechanicalOffsetY, 0.0, MechanicalOffsetLimitMm);
            MechanicalOffsetT = EnsureMechanicalOffsetArray(MechanicalOffsetT, 0.0, MechanicalOffsetTLimitDeg);

            if (PickerZSyncLiftDistance <= 0.0 && SyncLiftDistance > 0.0)
                PickerZSyncLiftDistance = SyncLiftDistance;
            if (PickerZSeparateDistance <= 0.0 && PickerSeparateDistance > 0.0)
                PickerZSeparateDistance = PickerSeparateDistance;
            if (PickerZSlowApproachSpeedPercent <= 0.0 && PickerZSlowApproachVelocity > 0.0)
                PickerZSlowApproachSpeedPercent = 1.0;
            if (PickerZSyncLiftVelocity <= 0.0 && SyncLiftSpeedPercent > 0.0)
                PickerZSyncLiftVelocity = 5.0;
            if (PickerZSeparateSpeedPercent <= 0.0 && SeparateSpeedPercent > 0.0)
                PickerZSeparateSpeedPercent = SeparateSpeedPercent;
            if (PickerZSeparateSpeedPercent <= 0.0 && PickerZSeparateVelocity > 0.0)
                PickerZSeparateSpeedPercent = 1.0;

            // 기존 값 3(FastContiSegmentedPickUp, 삭제됨)은 Default로 정규화한다.
            if (TransferMotionMode != PickerPickUpTransferMotionMode.Default &&
                TransferMotionMode != PickerPickUpTransferMotionMode.ContiSegmentedPickUp)
            {
                TransferMotionMode = PickerPickUpTransferMotionMode.Default;
            }

            if (TransferContiCoordinate <= 0)
                TransferContiCoordinate = 2;
            if (TransferContiTimeoutMs <= 0)
                TransferContiTimeoutMs = 5000;
            TransferContiMaxTravelDistance = NormalizePositive(TransferContiMaxTravelDistance, 45.0);
            TransferContiPickerYMaxCorrectionDistance = NormalizePositive(TransferContiPickerYMaxCorrectionDistance, 1.5);
            TransferContiXYMidRatio = NormalizeRatio(TransferContiXYMidRatio, 0.5);
            TransferContiSplineCurvePercent = NormalizeSplineCurvePercent(TransferContiSplineCurvePercent, 100.0);
            // TransferContiMaxVelocity/Acc/Dec, Node0~3SpeedPercent 정규화 삭제(필드 폐지).

            PickerZPrePickDistance = NormalizeDistance(PickerZPrePickDistance);
            PreDownNeedleWorkRadiusMm = NormalizeDistance(PreDownNeedleWorkRadiusMm);
            if (DynamicWaitExtraMarginMm < 0.0)
                DynamicWaitExtraMarginMm = 0.0;
            PickerZSlowApproachSpeedPercent = NormalizePercent(PickerZSlowApproachSpeedPercent, 1.0);
            PickerZSyncLiftDistance = NormalizeDistance(PickerZSyncLiftDistance);
            PickerZSyncLiftVelocity = NormalizePositive(PickerZSyncLiftVelocity, 5.0);
            PickerZSyncLiftAcceleration = NormalizePositive(PickerZSyncLiftAcceleration, 100.0);
            PickerZSyncLiftDeceleration = NormalizePositive(PickerZSyncLiftDeceleration, 100.0);
            PickerZSeparateDistance = NormalizeDistance(PickerZSeparateDistance);
            PickerZSeparateSpeedPercent = NormalizePercent(PickerZSeparateSpeedPercent, 1.0);
            PickerZAvoidReturnSpeedPercent = NormalizePercent(PickerZAvoidReturnSpeedPercent, 10.0);
            PickerSafeForWaferStageDistance = NormalizePickerSafeForWaferStageDistance(PickerSafeForWaferStageDistance);

            if (VacuumOnBeforePickDelayMs < 0)
                VacuumOnBeforePickDelayMs = 0;
            if (NeedleVacuumOffSettleBeforeXYMs < 0)
                NeedleVacuumOffSettleBeforeXYMs = 0;
            if (NeedleVacuumOffSettleBeforeXYMs > 60000)
                NeedleVacuumOffSettleBeforeXYMs = 60000;
            if (SyncLiftSettleMs < 0)
                SyncLiftSettleMs = 0;
            if (PickSettleMs < 0)
                PickSettleMs = 0;
        }

        // GetTransferContiNodeVelocity/Acceleration/Deceleration 삭제(사용자 확정 속도 모델
        // 2026-07-26) — 이송 속도는 각 축 DefaultVelocity × 전역 스케일로 일원화.

        public static double NormalizePercent(double percent, double fallback)
        {
            if (double.IsNaN(percent) || double.IsInfinity(percent) || percent <= 0.0)
                percent = fallback;
            if (percent < 1.0)
                return 1.0;
            if (percent > 100.0)
                return 100.0;
            return percent;
        }

        public static double NormalizePositive(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
                return fallback;
            return value;
        }

        public static double NormalizeMechanicalOffsetLimit(double limit)
        {
            if (double.IsNaN(limit) || double.IsInfinity(limit))
                limit = DefaultMechanicalOffsetLimitMm;

            limit = Math.Round(limit, 3, MidpointRounding.AwayFromZero);
            if (limit < 0.0)
                return 0.0;
            if (limit > MaximumMechanicalOffsetLimitMm)
                return MaximumMechanicalOffsetLimitMm;
            return limit;
        }

        public static double NormalizeMechanicalOffset(double value, double limit)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                value = 0.0;

            limit = NormalizeMechanicalOffsetLimit(limit);
            value = Math.Round(value, 3, MidpointRounding.AwayFromZero);
            if (value < -limit)
                return -limit;
            if (value > limit)
                return limit;
            return value;
        }

        public static double[] EnsureMechanicalOffsetArray(double[] source, double fallback, double limit)
        {
            double[] result = source;
            if (result == null || result.Length != MechanicalOffsetPickerCount)
            {
                result = new double[MechanicalOffsetPickerCount];
                for (int i = 0; i < result.Length; i++)
                    result[i] = fallback;

                if (source != null)
                {
                    for (int i = 0; i < Math.Min(source.Length, result.Length); i++)
                        result[i] = source[i];
                }
            }

            for (int i = 0; i < result.Length; i++)
                result[i] = NormalizeMechanicalOffset(result[i], limit);
            return result;
        }

        public double GetMechanicalOffsetX(int pickerIndex)
        {
            Ensure();
            return pickerIndex >= 0 && pickerIndex < MechanicalOffsetX.Length
                ? MechanicalOffsetX[pickerIndex]
                : 0.0;
        }

        public double GetMechanicalOffsetY(int pickerIndex)
        {
            Ensure();
            return pickerIndex >= 0 && pickerIndex < MechanicalOffsetY.Length
                ? MechanicalOffsetY[pickerIndex]
                : 0.0;
        }

        public void SetMechanicalOffsetX(int pickerIndex, double value)
        {
            Ensure();
            if (pickerIndex >= 0 && pickerIndex < MechanicalOffsetX.Length)
                MechanicalOffsetX[pickerIndex] = NormalizeMechanicalOffset(value, MechanicalOffsetLimitMm);
        }

        public void SetMechanicalOffsetY(int pickerIndex, double value)
        {
            Ensure();
            if (pickerIndex >= 0 && pickerIndex < MechanicalOffsetY.Length)
                MechanicalOffsetY[pickerIndex] = NormalizeMechanicalOffset(value, MechanicalOffsetLimitMm);
        }

        public double GetMechanicalOffsetT(int pickerIndex)
        {
            Ensure();
            return pickerIndex >= 0 && pickerIndex < MechanicalOffsetT.Length
                ? MechanicalOffsetT[pickerIndex]
                : 0.0;
        }

        public void SetMechanicalOffsetT(int pickerIndex, double value)
        {
            Ensure();
            if (pickerIndex >= 0 && pickerIndex < MechanicalOffsetT.Length)
                MechanicalOffsetT[pickerIndex] = NormalizeMechanicalOffset(value, MechanicalOffsetTLimitDeg);
        }

        public static double NormalizePickerSafeForWaferStageDistance(double distance)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance))
                return MinimumPickerSafeForWaferStageDistance;
            return Math.Max(MinimumPickerSafeForWaferStageDistance, distance);
        }

        public static double NormalizeSplineCurvePercent(double percent, double fallback)
        {
            if (double.IsNaN(percent) || double.IsInfinity(percent))
                percent = fallback;
            if (percent < 0.0)
                return 0.0;
            if (percent > 200.0)
                return 200.0;
            return percent;
        }

        private static double NormalizeDistance(double distance)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0.0)
                return 0.0;
            return distance;
        }

        // GetTransferContiNodeRatio 삭제(Node0~3SpeedPercent 필드 폐지).

        private static double NormalizeRatio(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                value = fallback;
            if (value < 0.0)
                return 0.0;
            if (value > 1.0)
                return 1.0;
            return value;
        }
    }

    [DataContract]
    public sealed class PickerBottomInspectionMotionConfig
    {
        [DataMember] public PickerBottomFlyingZDownMode FlyingZDownMode { get; set; } = PickerBottomFlyingZDownMode.Off;
        [DataMember] public double FlyingZDownDistance { get; set; } = 2.0;

        // 접근 구간 Z+T 선행(사용자 승인 2026-07-26): 첫 피커 Bottom X 이동 중 잔여 거리가
        // 이 값 이하가 되면 해당 피커 Z 하강+T 회전을 X와 동시에 시작한다.
        // 0 이하 = 기능 Off. 설비(유닛) Config — 레시피 아님.
        [DataMember] public double ApproachPreMotionDistanceMm { get; set; } = 50.0;

        // 마지막 Bottom(P1) 촬영과 첫 Side(P4) 촬영 요청을 병렬 송신하는 특수 오버랩.
        // 기존 조건: 시퀀스에서 하드코딩 false로 잠겨 있어 어떤 설정으로도 켤 수 없었다.
        // 현재 기준(사용자 승인 2026-07-28): 유닛 Config로 노출. 기본 Off — 레시피 아님.
        [DataMember] public bool ParallelFirstSideOverlap { get; set; } = false;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            // DataContract 역직렬화는 필드 초기화식을 건너뛴다 — 구버전 설정 파일에
            // 멤버가 없으면 여기서 기본값을 보장한다(파일에 값이 있으면 이후 덮어씀).
            ApproachPreMotionDistanceMm = 50.0;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            Ensure();
        }

        public void Ensure()
        {
            FlyingZDownDistance = NormalizeDistance(FlyingZDownDistance);
            ApproachPreMotionDistanceMm = NormalizeDistance(ApproachPreMotionDistanceMm);
        }

        public double ResolveFlyingZDownTarget(double avoid, double bottom)
        {
            Ensure();

            switch (FlyingZDownMode)
            {
                case PickerBottomFlyingZDownMode.ToBottomPosition:
                    return bottom;

                case PickerBottomFlyingZDownMode.DownDistance:
                    return ResolveFlyingZDownDistanceTarget(avoid, bottom);

                case PickerBottomFlyingZDownMode.Off:
                default:
                    return avoid;
            }
        }

        private double ResolveFlyingZDownDistanceTarget(double avoid, double bottom)
        {
            double distance = NormalizeDistance(FlyingZDownDistance);
            if (distance <= 0.0)
                return avoid;

            double delta = bottom - avoid;
            double total = Math.Abs(delta);
            if (total <= 0.0001)
                return bottom;

            if (distance >= total)
                return bottom;

            return avoid + Math.Sign(delta) * distance;
        }

        public static double NormalizeDistance(double distance)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0.0)
                return 0.0;
            return distance;
        }
    }

    [DataContract]
    public sealed class PickerPlaceMotionConfig
    {
        [DataMember] public double MechanicalOffsetLimitMm { get; set; } = PickerPickUpMotionConfig.DefaultMechanicalOffsetLimitMm;
        [DataMember] public double MechanicalOffsetTLimitDeg { get; set; } = PickerPickUpMotionConfig.DefaultMechanicalOffsetLimitTDeg;
        [DataMember] public double BottomPlaceCorrectionLimitMm { get; set; } = PickerPickUpMotionConfig.DefaultMechanicalOffsetLimitMm;
        [DataMember] public double[] MechanicalOffsetX { get; set; } = new double[PickerPickUpMotionConfig.MechanicalOffsetPickerCount];
        [DataMember] public double[] MechanicalOffsetY { get; set; } = new double[PickerPickUpMotionConfig.MechanicalOffsetPickerCount];
        // PickerT 축 기구 보정(deg) — 이동식에서 PickerT에 가산(+). 런타임 T(감산)와 같은 축이라
        // 이관식은 기구T′ = 기구T − 필터T 로 코드 확정(2026-08-16).
        [DataMember] public double[] MechanicalOffsetT { get; set; } = new double[PickerPickUpMotionConfig.MechanicalOffsetPickerCount];
        [DataMember] public PickerPlaceMotionMode MotionMode { get; set; } = PickerPlaceMotionMode.ContiSegmentedPlace;
        [DataMember] public int ContiCoordinate { get; set; } = 1;
        [DataMember] public int ContiTimeoutMs { get; set; } = 5000;
        [DataMember] public double ContiMaxTravelDistance { get; set; } = 45.0;
        [DataMember] public double ContiZ1Step1Clearance { get; set; } = 2.0;
        [DataMember] public double ContiZ1Step2Clearance { get; set; } = 2.0;
        [DataMember] public double ContiNearAvoidDistance { get; set; } = 1.0;
        [DataMember] public double ContiXYMidRatio { get; set; } = 0.5;
        [DataMember] public double ContiSplineCurvePercent { get; set; } = 100.0;
        [DataMember] public double ContiOverDrive { get; set; } = 0.03;
        [DataMember] public double PlaceZOverDrive { get; set; } = 0.0;
        [DataMember] public int PlaceReleaseDwellMs { get; set; } = 0;
        [DataMember] public int PlaceBlowDelayMs { get; set; } = 100;
        [DataMember] public double ContiTapeThicknessFallback { get; set; } = 0.0;
        [DataMember] public double ContiDieThicknessFallback { get; set; } = 0.0;
        [DataMember] public double ContiMaxVelocity { get; set; } = 500.0;
        [DataMember] public double ContiMaxAcceleration { get; set; } = 5000.0;
        [DataMember] public double ContiMaxDeceleration { get; set; } = 5000.0;
        [DataMember] public bool ContiUseGlobalSpeedScale { get; set; } = true;
        [DataMember] public double ContiNode0SpeedPercent { get; set; } = 1.0;
        [DataMember] public double ContiNode1SpeedPercent { get; set; } = 20.0;
        [DataMember] public double ContiNode2SpeedPercent { get; set; } = 100.0;
        [DataMember] public double ContiNode3SpeedPercent { get; set; } = 100.0;
        [DataMember] public double ContiNode4SpeedPercent { get; set; } = 1.0;

        // Place Z 선행/조기완료(사용자 승인 2026-07-26): 1-A(Y 전진 ∥ Z PrePlace 선행)와
        // 1-B(상승 PrePlace 조기 완료 판정)를 묶는 공용 스위치. 기본 Off — 켜야만 동작.
        [DataMember] public bool PlaceEntryZPreDownMode { get; set; } = false;
        // Rear 전용 발동 제약(방향 정정 — 사용자 실측 확인 2026-07-26): 대상 Bin StageY의
        // 실측과 수령 이동 목표가 모두 이 값 "이상(≥)"일 때만 Rear에서 Z 선행 발동.
        // 리어 물리 간섭 구조물은 StageY가 작은 구간에 있다. 0.0 = Rear 발동 안 함(안전측
        // 기본 — 현장 실측으로 설정해야 켜짐). Front 미적용.
        [DataMember] public double RearEntryPreDownStageYLimitMm { get; set; } = 0.0;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            ContiSplineCurvePercent = 100.0;
            ContiUseGlobalSpeedScale = true;
            PlaceBlowDelayMs = 100;
            // 구버전 설정 파일 하위호환: 멤버 부재 시 안전측 기본값(Off/0.0) 보장.
            PlaceEntryZPreDownMode = false;
            RearEntryPreDownStageYLimitMm = 0.0;
            MechanicalOffsetLimitMm = PickerPickUpMotionConfig.DefaultMechanicalOffsetLimitMm;
            MechanicalOffsetTLimitDeg = PickerPickUpMotionConfig.DefaultMechanicalOffsetLimitTDeg;
            BottomPlaceCorrectionLimitMm = PickerPickUpMotionConfig.DefaultMechanicalOffsetLimitMm;
            MechanicalOffsetX = new double[PickerPickUpMotionConfig.MechanicalOffsetPickerCount];
            MechanicalOffsetY = new double[PickerPickUpMotionConfig.MechanicalOffsetPickerCount];
            MechanicalOffsetT = new double[PickerPickUpMotionConfig.MechanicalOffsetPickerCount];
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            Ensure();
        }

        public void Ensure()
        {
            MechanicalOffsetLimitMm = PickerPickUpMotionConfig.NormalizeMechanicalOffsetLimit(MechanicalOffsetLimitMm);
            MechanicalOffsetTLimitDeg = PickerPickUpMotionConfig.NormalizeMechanicalOffsetLimit(MechanicalOffsetTLimitDeg);
            BottomPlaceCorrectionLimitMm =
                PickerPickUpMotionConfig.NormalizeMechanicalOffsetLimit(BottomPlaceCorrectionLimitMm);
            MechanicalOffsetX = PickerPickUpMotionConfig.EnsureMechanicalOffsetArray(
                MechanicalOffsetX,
                0.0,
                MechanicalOffsetLimitMm);
            MechanicalOffsetY = PickerPickUpMotionConfig.EnsureMechanicalOffsetArray(
                MechanicalOffsetY,
                0.0,
                MechanicalOffsetLimitMm);
            MechanicalOffsetT = PickerPickUpMotionConfig.EnsureMechanicalOffsetArray(
                MechanicalOffsetT,
                0.0,
                MechanicalOffsetTLimitDeg);

            if (MotionMode != PickerPlaceMotionMode.Default &&
                MotionMode != PickerPlaceMotionMode.ContiSegmentedPlace)
            {
                MotionMode = PickerPlaceMotionMode.ContiSegmentedPlace;
            }

            if (ContiCoordinate <= 0)
                ContiCoordinate = 1;
            if (ContiTimeoutMs <= 0)
                ContiTimeoutMs = 5000;
            ContiMaxTravelDistance = PickerPickUpMotionConfig.NormalizePositive(ContiMaxTravelDistance, 45.0);
            ContiZ1Step1Clearance = NormalizeNonNegative(ContiZ1Step1Clearance);
            ContiZ1Step2Clearance = NormalizeNonNegative(ContiZ1Step2Clearance);
            ContiNearAvoidDistance = NormalizeNonNegative(ContiNearAvoidDistance);
            ContiXYMidRatio = NormalizeRatio(ContiXYMidRatio, 0.5);
            ContiSplineCurvePercent = PickerPickUpMotionConfig.NormalizeSplineCurvePercent(ContiSplineCurvePercent, 100.0);
            ContiOverDrive = NormalizeNonNegative(ContiOverDrive);
            PlaceZOverDrive = NormalizeFinite(PlaceZOverDrive);
            if (PlaceReleaseDwellMs < 0)
                PlaceReleaseDwellMs = 0;
            if (PlaceBlowDelayMs < 0)
                PlaceBlowDelayMs = 0;
            ContiTapeThicknessFallback = NormalizeNonNegative(ContiTapeThicknessFallback);
            ContiDieThicknessFallback = NormalizeNonNegative(ContiDieThicknessFallback);
            RearEntryPreDownStageYLimitMm = NormalizeFinite(RearEntryPreDownStageYLimitMm);
            ContiMaxVelocity = PickerPickUpMotionConfig.NormalizePositive(ContiMaxVelocity, 500.0);
            ContiMaxAcceleration = PickerPickUpMotionConfig.NormalizePositive(ContiMaxAcceleration, 5000.0);
            ContiMaxDeceleration = PickerPickUpMotionConfig.NormalizePositive(ContiMaxDeceleration, 5000.0);
            ContiNode0SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(ContiNode0SpeedPercent, 1.0);
            ContiNode1SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(ContiNode1SpeedPercent, 20.0);
            ContiNode2SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(ContiNode2SpeedPercent, 100.0);
            ContiNode3SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(ContiNode3SpeedPercent, 100.0);
            ContiNode4SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(ContiNode4SpeedPercent, 1.0);
        }

        public double GetMechanicalOffsetX(int pickerIndex)
        {
            Ensure();
            return pickerIndex >= 0 && pickerIndex < MechanicalOffsetX.Length
                ? MechanicalOffsetX[pickerIndex]
                : 0.0;
        }

        public double GetMechanicalOffsetY(int pickerIndex)
        {
            Ensure();
            return pickerIndex >= 0 && pickerIndex < MechanicalOffsetY.Length
                ? MechanicalOffsetY[pickerIndex]
                : 0.0;
        }

        public void SetMechanicalOffsetX(int pickerIndex, double value)
        {
            Ensure();
            if (pickerIndex >= 0 && pickerIndex < MechanicalOffsetX.Length)
                MechanicalOffsetX[pickerIndex] = PickerPickUpMotionConfig.NormalizeMechanicalOffset(value, MechanicalOffsetLimitMm);
        }

        public void SetMechanicalOffsetY(int pickerIndex, double value)
        {
            Ensure();
            if (pickerIndex >= 0 && pickerIndex < MechanicalOffsetY.Length)
                MechanicalOffsetY[pickerIndex] = PickerPickUpMotionConfig.NormalizeMechanicalOffset(value, MechanicalOffsetLimitMm);
        }

        public double GetMechanicalOffsetT(int pickerIndex)
        {
            Ensure();
            return pickerIndex >= 0 && pickerIndex < MechanicalOffsetT.Length
                ? MechanicalOffsetT[pickerIndex]
                : 0.0;
        }

        public void SetMechanicalOffsetT(int pickerIndex, double value)
        {
            Ensure();
            if (pickerIndex >= 0 && pickerIndex < MechanicalOffsetT.Length)
                MechanicalOffsetT[pickerIndex] = PickerPickUpMotionConfig.NormalizeMechanicalOffset(value, MechanicalOffsetTLimitDeg);
        }

        public double GetContiNodeVelocity(int nodeIndex)
        {
            double velocity = ContiMaxVelocity * GetContiNodeRatio(nodeIndex);
            return ContiUseGlobalSpeedScale ? MotionSpeedScale.ApplyDefaultVelocityScale(velocity) : velocity;
        }

        public double GetContiNodeAcceleration(int nodeIndex)
        {
            double acceleration = ContiMaxAcceleration * GetContiNodeRatio(nodeIndex);
            return ContiUseGlobalSpeedScale ? MotionSpeedScale.ApplyDefaultAccelerationScale(acceleration) : acceleration;
        }

        public double GetContiNodeDeceleration(int nodeIndex)
        {
            double deceleration = ContiMaxDeceleration * GetContiNodeRatio(nodeIndex);
            return ContiUseGlobalSpeedScale ? MotionSpeedScale.ApplyDefaultAccelerationScale(deceleration) : deceleration;
        }

        private double GetContiNodeRatio(int nodeIndex)
        {
            double percent;
            switch (nodeIndex)
            {
                case 0: percent = ContiNode0SpeedPercent; break;
                case 1: percent = ContiNode1SpeedPercent; break;
                case 2: percent = ContiNode2SpeedPercent; break;
                case 3: percent = ContiNode3SpeedPercent; break;
                case 4: percent = ContiNode4SpeedPercent; break;
                default: percent = ContiNode4SpeedPercent; break;
            }

            return PickerPickUpMotionConfig.NormalizePercent(percent, 1.0) / 100.0;
        }

        private static double NormalizeNonNegative(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
                return 0.0;
            return value;
        }

        private static double NormalizeFinite(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0.0;
            return value;
        }

        private static double NormalizeRatio(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                value = fallback;
            if (value < 0.0)
                return 0.0;
            if (value > 1.0)
                return 1.0;
            return value;
        }
    }

    public class BottomVisionOffset
    {
        public int PickerNo { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public double OffsetT { get; set; }
        // FINAL RESULT의 Place 보정 전용 값입니다.
        // 기존 OffsetX/OffsetY는 MRESULT 기반 Side 보정 의미를 유지하므로 혼용하지 않습니다.
        public double BottomItemOffsetX { get; set; }
        public double BottomItemOffsetY { get; set; }
        public bool HasBottomItemOffsetX { get; set; }
        public bool HasBottomItemOffsetY { get; set; }
        public bool BottomItemOffsetXPass { get; set; }
        public bool BottomItemOffsetYPass { get; set; }
        public bool HasBottomItemOffsetXPass { get; set; }
        public bool HasBottomItemOffsetYPass { get; set; }
        public bool MeasureValid { get; set; }
        public bool HasMeasureValid { get; set; }
        public string RequestId { get; set; }
        public string GroupId { get; set; }
        public string DieId { get; set; }
        public int DieIndex { get; set; } = -1;
        // Bottom 완료 결과의 중심 Offset(mm). Side 카메라 각도별 보정 전용이며 Place OffsetX/Y와 분리한다.
        public double BottomCenterOffsetX { get; set; }
        public double BottomCenterOffsetY { get; set; }
        public bool HasBottomCenterOffset { get; set; }
        public double SideVisionYOffset { get; set; }
        public double PickerZOffset { get; set; }
        public bool HasSideInspectionCorrection { get; set; }
        public bool IsOk { get; set; }
        public string Raw { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public class SideVisionResult
    {
        public int PickerNo { get; set; }
        public bool Side1Ok { get; set; }
        public bool Side2Ok { get; set; }
        public bool Side3Ok { get; set; }
        public bool Side4Ok { get; set; }
        public string Raw { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool IsAllOk
        {
            get { return Side1Ok && Side2Ok && Side3Ok && Side4Ok; }
        }
    }

    public interface IVisionTpuClient
    {
        Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs = 1000);
        Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<bool> StartBottomInspectAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<BottomVisionOffset> WaitBottomMResultAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<BottomVisionOffset> WaitBottomFinalResultAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<BottomVisionOffset> WaitBottomResultAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<BottomVisionOffset> GetBottomResultAsync(int pickerNo, int timeoutMs = 5000);
        Task<BottomVisionOffset> GetBottomResultAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs = 5000);
        Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs, CancellationToken ct);
        Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs = 1000);
        Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs, CancellationToken ct);
        Task<bool> StartSideInspectAsync(int pickerNo, int angleDeg, int timeoutMs, CancellationToken ct);
        Task<SideVisionResult> WaitSideResultAsync(int pickerNo, int timeoutMs, CancellationToken ct);
        void AbandonPendingInspection(int pickerNo, string reason);
        Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs = 5000);
        Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs, CancellationToken ct);
    }

    public sealed class PickerRuntimeTool
    {
        private readonly Func<PickerRuntimeToolSetup> _setupFactory;
        private readonly Func<PickerRuntimeToolRecipe> _recipeFactory;
        private readonly Action _vacuumOn;
        private readonly Action _vacuumOff;
        private readonly Action _blowOn;
        private readonly Action _blowOff;

        public PickerRuntimeTool(
            BaseAxis pickerZ,
            BaseAxis pickerT,
            Func<PickerRuntimeToolSetup> setupFactory,
            Func<PickerRuntimeToolRecipe> recipeFactory,
            Action vacuumOn,
            Action vacuumOff,
            Action blowOn,
            Action blowOff)
        {
            PickerZ = pickerZ;
            PickerT = pickerT;
            _setupFactory = setupFactory;
            _recipeFactory = recipeFactory;
            _vacuumOn = vacuumOn;
            _vacuumOff = vacuumOff;
            _blowOn = blowOn;
            _blowOff = blowOff;
        }

        public BaseAxis PickerZ { get; private set; }
        public BaseAxis PickerT { get; private set; }
        public PickerRuntimeToolSetup Setup { get { return _setupFactory(); } }
        public PickerRuntimeToolRecipe Recipe { get { return _recipeFactory(); } }

        public void VacuumOn() { _vacuumOn(); }
        public void VacuumOff() { _vacuumOff(); }
        public void BlowOn() { _blowOn(); }
        public void BlowOff() { _blowOff(); }
    }

    public sealed class PickerRuntimeToolSetup
    {
        public double ColletOffsetX { get; set; }
        public double ColletOffsetY { get; set; }
        public double PickupPosition { get; set; }
        public double WaitPosition { get; set; }
        public double PlacePosition { get; set; }
    }

    public sealed class PickerRuntimeToolRecipe
    {
        public double ZVelocity { get; set; }
        public double ThetaVelocity { get; set; }
        public int VacuumSettleMs { get; set; }
        public double PickLiftPosition { get; set; }
        public int PickLiftWaitMs { get; set; }
        public int PlaceDelayMs { get; set; }
    }
}
