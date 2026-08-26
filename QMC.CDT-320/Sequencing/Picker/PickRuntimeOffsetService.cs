using System;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Pick 런타임 오프셋 실시간 보정 서비스.
    /// Pickup → Bottom 비전 검사(콜렛에 물린 Die의 틀어짐 측정) → Y 전처리 → 필터 →
    /// 다음 Pick 목표 반영으로 도는 폐루프 보정의 상태 보관소다.
    /// - 갱신식(2026-08-18 팀장님 지시): 폐루프 측정값은 보정 적용 후 잔차(m)이므로 전 채널을
    ///   잔차 적분(F += α·m — 잔차 0 수렴)으로 갱신한다. 잔차를 EMA(F += α(m−F))에 그대로
    ///   넣으면 오차 절반에서 평형이 생겨 폐루프에 부적합.
    ///   (T는 2026-08-18 적용 부호 가산 확정과 함께 적분 전환 — 전환 전 학습값이 남아 있으면
    ///    첫 잔차가 이상치 한계를 넘어 전 샘플 기각으로 고착될 수 있으니 재가동 전 T 리셋 필수.)
    /// - 필터 단위: PickerSide(Front/Rear) × PickerNo(1~4) = 8세트, 각 X/Y/T 3채널 독립.
    /// - Y 채널은 서비스가 전처리한다. 기존 조건(~2026-07-30): 편차 = 촬영시점 PickerY CommandPosition − 콜렛Cal Y,
    ///   전처리 YOffset = 촬영된 OffsetY − 편차. 현재 기준(사용자 지시 2026-07-30 최종): Bottom 촬영 Y가 P4 기준
    ///   고정이므로 전처리 YOffset = 촬영된 OffsetY − (현재 픽커 콜렛Cal Y − 4번 픽커 콜렛Cal Y).
    ///   X/T는 측정값 그대로 입력.
    /// - 필터 상태는 전처리 후 측정 부호 그대로(raw) 저장하고, 적용 부호(X/Y 가산 2026-07-30
    ///   확정, T 가산 2026-08-18 확정 — forceMove로 T가 실발행되자 감산이 발산함을 확인해 정정)는
    ///   적용 지점(DieCoordinateTransformService.CalculatePickTarget)에서 수행한다.
    /// - Enable/Disable(UsePickRuntimeOffset): Disable이면 적용과 학습을 모두 중지한다
    ///   (2026-08-19 팀장님 지시 — 적분 갱신은 미적용 상태에서 학습하면 무한 누적·클램프 알람.
    ///    EMA 시절 "학습 계속" 설계 폐기). Enable 판정은 적용 지점에서 GetOffset 사용 여부로 결정.
    /// - 발산 방지: 갱신 후 상태값을 X/Y ±0.50mm, T ±0.5°로 클램프하고 한계 도달 시 Warning을 1회 발생
    ///   (한계 미만 복귀 시 재무장하는 래치).
    /// - 검사 시퀀스 스레드(갱신)와 픽업 시퀀스 스레드(조회)가 다르므로 lock으로 보호한다.
    /// </summary>
    internal static class PickRuntimeOffsetService
    {
        // 필터 설정 입력 허용 범위(SetFilterSettings·설정 다이얼로그 공통, 2026-08-16 팀장님 승인).
        public const double MinCutoffFrequency = 0.001;
        public const double MaxCutoffFrequency = 1.0;
        public const double MinFilterLimit = 0.01;
        public const double MaxFilterLimit = 5.0;

        // 필터 한계 설정값(스토어 로드, SetFilterSettings로 변경) — 판정 알고리즘은 무변경.
        // 이상치 거부 한계: 현재 필터 출력 대비 편차가 이 값 이상이면 해당 채널 샘플 폐기.
        private static double _outlierLimitXyMm = PickRuntimeOffsetDocument.DefaultOutlierLimitXyMm;
        private static double _outlierLimitTDeg = PickRuntimeOffsetDocument.DefaultOutlierLimitTDeg;
        // 발산 방지 클램프 한계: 필터 상태값 자체를 이 범위로 제한.
        private static double _clampLimitXyMm = PickRuntimeOffsetDocument.DefaultClampLimitXyMm;
        private static double _clampLimitTDeg = PickRuntimeOffsetDocument.DefaultClampLimitTDeg;

        private static readonly object Sync = new object();
        private static bool _loaded;
        private static bool _useCorrection;
        private static double _cutoffFrequency = 0.05;
        private static FilterSet[] _filters;

        // [택타임 개선 2026-07-27] Bottom 검사마다(다이당 1회) Sync 락을 쥔 채 File.Create + JSON 쓰기를
        // 동기로 수행해 검사 시퀀스 스레드를 1~5ms 블로킹했고, Front/Rear가 같은 락을 두고 경합했다.
        // 저장 내용은 고정 8행(Front/Rear × Picker 1~4)뿐이라 병합 저장해도 잃는 정보가 없다.
        // 갱신 경로는 dirty 표시만 하고, 워커가 조용해진 뒤 락 밖에서 1회 저장한다.
        // 설정 변경/리셋 같은 사용자 조작 경로는 기존대로 즉시 저장한다.
        private const int DeferredSaveQuietMs = 1000;
        private static readonly object DeferredSaveSync = new object();
        // [종료 저장 2026-08-17] 지연 저장 워커와 동기 flush가 같은 파일에 동시에 쓰지 않도록 IO를 직렬화한다
        // (Place 서비스와 동일 규약 — 동시 쓰기는 파일 손상 = 학습값 전체 소실로 이어진다).
        private static readonly object DeferredSaveIoSync = new object();
        // [내구성 워터마크 2026-08-18] Material·Place 저장과 동일 규약. 저장 완료를 "몇 번째 저장 시도인가"가
        // 아니라 "어느 자료 세대가 디스크에 있는가"로 판정한다. 동시 저장이 겹쳐도 더 새 저장이 내 자료를
        // 포함하므로 성공이며, 기다리거나 재시도할 필요가 없다. 둘 다 0에서 시작(로드된 상태는 이미 내구성 있음).
        private static long _stateVersion;
        private static long _durableStateVersion;
        private static bool _deferredSaveRequested;
        private static bool _deferredSaveWorkerRunning;

        private sealed class FilterSet
        {
            public FilterSet(double cutoffFrequency)
            {
                X = new LowPassFilter(cutoffFrequency);
                Y = new LowPassFilter(cutoffFrequency);
                T = new LowPassFilter(cutoffFrequency);
            }

            public LowPassFilter X { get; private set; }
            public LowPassFilter Y { get; private set; }
            public LowPassFilter T { get; private set; }
            public DateTime LastUpdated { get; set; }
            // 클램프 워닝 래치 (채널별). 한계 도달 시 true, 한계 미만 복귀 시 false로 재무장.
            public bool ClampLatchedX { get; set; }
            public bool ClampLatchedY { get; set; }
            public bool ClampLatchedT { get; set; }
        }

        /// <summary>Pick 런타임 보정 적용 여부 (UsePickRuntimeOffset 설정값).</summary>
        public static bool IsEnabled
        {
            get
            {
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    return _useCorrection;
                }
            }
        }

        /// <summary>보정 적용 여부를 변경하고 저장한다 (필터 상태는 유지).</summary>
        public static void SetEnabled(bool enabled)
        {
            try
            {
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    if (_useCorrection == enabled)
                        return;

                    _useCorrection = enabled;
                    MarkStateChangedLocked();
                    SaveLocked();
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 오프셋 사용 설정을 변경했습니다. enabled=" + enabled + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 오프셋 사용 설정 변경 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>현재 필터 설정(fc + 이상치/클램프 한계 4종)을 반환한다.</summary>
        public static void GetFilterSettings(
            out double cutoffFrequency,
            out double outlierLimitXyMm,
            out double outlierLimitTDeg,
            out double clampLimitXyMm,
            out double clampLimitTDeg)
        {
            lock (Sync)
            {
                EnsureLoadedLocked();
                cutoffFrequency = _cutoffFrequency;
                outlierLimitXyMm = _outlierLimitXyMm;
                outlierLimitTDeg = _outlierLimitTDeg;
                clampLimitXyMm = _clampLimitXyMm;
                clampLimitTDeg = _clampLimitTDeg;
            }
        }

        /// <summary>
        /// 필터 설정(fc + 한계 4종)을 변경하고 즉시 저장한다. UI 저장은 반드시 이 API를 경유할 것 —
        /// json은 런 중 지연 저장으로 덮어써지므로 파일을 직접 쓰면 유실된다.
        /// fc 변경은 8세트 필터의 alpha만 재계산하고 학습 상태를 유지한다.
        /// 클램프 한계 축소로 현재 상태값이 범위를 벗어나면 즉시 재클램프한다(래치 무조작, 로그만).
        /// 범위 밖 입력은 저장하지 않고 false를 반환한다(다이얼로그 검증과 이중 방어).
        /// </summary>
        public static bool SetFilterSettings(
            double cutoffFrequency,
            double outlierLimitXyMm,
            double outlierLimitTDeg,
            double clampLimitXyMm,
            double clampLimitTDeg)
        {
            try
            {
                string rejectReason = BuildFilterSettingsRejectReason(
                    cutoffFrequency, outlierLimitXyMm, outlierLimitTDeg, clampLimitXyMm, clampLimitTDeg);
                if (rejectReason != null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                        "Pick 런타임 필터 설정 입력이 허용 범위를 벗어나 거부했습니다. " + rejectReason + " - Failed");
                    return false;
                }

                double oldFc;
                double oldOutlierXy;
                double oldOutlierT;
                double oldClampXy;
                double oldClampT;
                var reclampLines = new System.Collections.Generic.List<string>();

                lock (Sync)
                {
                    EnsureLoadedLocked();
                    oldFc = _cutoffFrequency;
                    oldOutlierXy = _outlierLimitXyMm;
                    oldOutlierT = _outlierLimitTDeg;
                    oldClampXy = _clampLimitXyMm;
                    oldClampT = _clampLimitTDeg;

                    _cutoffFrequency = cutoffFrequency;
                    _outlierLimitXyMm = outlierLimitXyMm;
                    _outlierLimitTDeg = outlierLimitTDeg;
                    _clampLimitXyMm = clampLimitXyMm;
                    _clampLimitTDeg = clampLimitTDeg;

                    for (int i = 0; i < _filters.Length; i++)
                    {
                        FilterSet set = _filters[i];
                        set.X.SetCutoffFrequency(_cutoffFrequency);
                        set.Y.SetCutoffFrequency(_cutoffFrequency);
                        set.T.SetCutoffFrequency(_cutoffFrequency);

                        PickerSequenceSide side = i < 4 ? PickerSequenceSide.Front : PickerSequenceSide.Rear;
                        int pickerNo = (i % 4) + 1;
                        ReclampChannelLocked(set.X, _clampLimitXyMm, side, pickerNo, "X", reclampLines);
                        ReclampChannelLocked(set.Y, _clampLimitXyMm, side, pickerNo, "Y", reclampLines);
                        ReclampChannelLocked(set.T, _clampLimitTDeg, side, pickerNo, "T", reclampLines);
                    }

                    MarkStateChangedLocked();
                    SaveLocked();
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 필터 설정을 변경했습니다." +
                    " fc=" + F(oldFc) + "→" + F(cutoffFrequency) +
                    "(alpha=" + F(LowPassFilter.CalculateAlpha(oldFc)) + "→" + F(LowPassFilter.CalculateAlpha(cutoffFrequency)) + ")" +
                    ", outlierXy=" + F(oldOutlierXy) + "→" + F(outlierLimitXyMm) +
                    ", outlierT=" + F(oldOutlierT) + "→" + F(outlierLimitTDeg) +
                    ", clampXy=" + F(oldClampXy) + "→" + F(clampLimitXyMm) +
                    ", clampT=" + F(oldClampT) + "→" + F(clampLimitTDeg) + " - Ok");

                foreach (string line in reclampLines)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                        "Pick 런타임 오프셋을 축소된 클램프 한계로 재클램프했습니다. " + line + " - Check");
                }

                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 필터 설정 변경 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 현재 필터 상태(클램프 반영된 raw 값)를 반환한다.
        /// Enable 여부와 무관하게 상태를 반환하며, Enable 판정은 적용 지점에서 한다.
        /// </summary>
        public static void GetOffset(PickerSequenceSide side, int pickerNo, out double x, out double y, out double t)
        {
            x = 0.0;
            y = 0.0;
            t = 0.0;

            try
            {
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    FilterSet set = ResolveSetLocked(side, pickerNo);
                    if (set == null)
                        return;

                    x = set.X.Value;
                    y = set.Y.Value;
                    t = set.T.Value;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 오프셋 조회 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>
        /// Bottom 검사 오프셋 1샘플을 반영한다 (Pass 판정 샘플만 호출할 것).
        /// Y 전처리(P4 기준 콜렛Cal 환산) → 채널별 이상치 검사 → 필터 갱신 → 클램프+워닝 → 저장 → 로그.
        /// 현재 픽커 또는 4번 픽커 콜렛Cal 레코드가 없거나 Valid가 아니면 샘플 전체를 폐기한다.
        /// </summary>
        // 촬영각 분기 밴드(deg): 티칭 Δθ가 0°±5°면 무변환, ±180°±5°면 회전중심 기준 반전 변환,
        // 그 외 각도는 검증되지 않은 프레임이라 샘플을 폐기한다.
        private const double ShootAngleBandDeg = 5.0;

        public static void OnBottomInspectionOffset(
            PickerSequenceSide side,
            int pickerNo,
            double offsetX,
            double rawOffsetY,
            double offsetT,
            double capturedPickerYCommand,
            double capturedPickerXCommand,
            double rotationCenterX,
            double rotationCenterY,
            bool rotationCenterUsable,
            double shootDeltaThetaTeachingDeg,
            double colletCalY,
            bool colletCalValid,
            double basePicker4ColletCalY,
            bool basePicker4ColletCalValid,
            string dieId)
        {
            try
            {
                if (!colletCalValid || !basePicker4ColletCalValid)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                        "콜렛 캘리브레이션이 유효하지 않아 Pick 런타임 오프셋 샘플을 폐기했습니다. side=" + side +
                        ", pickerNo=" + pickerNo +
                        ", die=" + (dieId ?? string.Empty) +
                        ", colletCalValid=" + colletCalValid +
                        ", basePicker4ColletCalValid=" + basePicker4ColletCalValid +
                        ", capturedPickerYCommand=" + F(capturedPickerYCommand) + " - Check");
                    return;
                }

                // [촬영각 프레임 변환 2026-08-25 팀장님 지시] 부호가 실장비 확정된 이 폐루프
                // (0도 픽업/0도 촬영 기준)는 그대로 두고, 티칭이 픽업각과 180° 다른 각도로 촬영하는
                // 레시피(0도 픽업/180도 촬영)에서는 비전 Offset을 회전중심 기준 −Δθ 회전시켜
                // "픽업각에서 찍었을 가상 raw"로 되돌린 뒤 같은 루프(전처리·필터 무수정)에 넣는다.
                // 분기(티칭 Δθ = Pick T − Bottom T, ±360 정규화):
                //   |Δθ| ≤ 5°         → 무변환(기존 경로와 완전 동일)
                //   ||Δθ|−180°| ≤ 5°  → m′ = 2·(촬영지령 − 회전중심C) − m  (R(±180)=−I라 회전방향 무관,
                //                        편심도 이 항이 함께 흡수되어 필터가 학습한다)
                //   그 외 각도         → 샘플 폐기(비검증 각도 프레임 학습 방지)
                // 프레임 주의(BottomVisionOffset 주석): 비전 Offset은 이미지 프레임 mm(X=기계 동일,
                // Y=기계 반대), 촬영지령·C는 기계 프레임 — 기계로 환산해 변환 후 이미지로 되돌린다.
                double deltaThetaTeaching = NormalizeDegreesPlusMinus180(shootDeltaThetaTeachingDeg);
                bool shotAtPickAngle = Math.Abs(deltaThetaTeaching) <= ShootAngleBandDeg;
                bool shotAtOppositeAngle = Math.Abs(Math.Abs(deltaThetaTeaching) - 180.0) <= ShootAngleBandDeg;
                double effectiveOffsetX = offsetX;
                double effectiveRawOffsetY = rawOffsetY;
                string shootFrame = "pickAngle";
                double axisFromCameraMachX = 0.0;
                double axisFromCameraMachY = 0.0;
                if (!shotAtPickAngle)
                {
                    if (!shotAtOppositeAngle)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                            "촬영각 티칭이 픽업각 0°/180° 밴드를 벗어나 Pick 런타임 오프셋 샘플을 폐기했습니다. side=" + side +
                            ", pickerNo=" + pickerNo +
                            ", die=" + (dieId ?? string.Empty) +
                            ", deltaThetaTeaching=" + F(deltaThetaTeaching) + " - Check");
                        return;
                    }

                    if (!rotationCenterUsable)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                            "180° 촬영인데 회전중심(COC)이 유효하지 않아 Pick 런타임 오프셋 샘플을 폐기했습니다" +
                            "(무변환 학습은 반대 프레임이라 금지). side=" + side +
                            ", pickerNo=" + pickerNo +
                            ", die=" + (dieId ?? string.Empty) +
                            ", deltaThetaTeaching=" + F(deltaThetaTeaching) + " - Check");
                        return;
                    }

                    shootFrame = "oppositeAngle";
                    axisFromCameraMachX = capturedPickerXCommand - rotationCenterX;
                    axisFromCameraMachY = capturedPickerYCommand - rotationCenterY;
                    double measuredMachX = offsetX;
                    double measuredMachY = -rawOffsetY;
                    effectiveOffsetX = 2.0 * axisFromCameraMachX - measuredMachX;
                    effectiveRawOffsetY = -(2.0 * axisFromCameraMachY - measuredMachY);
                }

                // Y 전처리 — 기존 조건(~2026-07-30): 편차 = 촬영시점 PickerY 지령 − 콜렛Cal Y, preprocessedY = raw − 편차.
                // 현재 기준(사용자 지시 2026-07-30 최종): Bottom 촬영 Y는 P4 기준 고정(통합검사 07-28 승인)이므로
                //   촬영시점 지령 대신 콜렛Cal 고정값으로 환산해 순수 "Die가 콜렛 기준으로 틀어진 양"만 학습한다:
                //   preprocessedY = rawOffsetY − (현재 픽커 콜렛Cal Y − 4번 픽커 콜렛Cal Y).
                //   (가산 시험 결과 실장비 확인으로 델타 감산 확정. capturedPickerYCommand는 진단 로그 전용.)
                double colletYDeltaFromP4 = colletCalY - basePicker4ColletCalY;
                double preprocessedY = effectiveRawOffsetY - colletYDeltaFromP4;

                double filteredX;
                double filteredY;
                double filteredT;
                bool acceptedX;
                bool acceptedY;
                bool acceptedT;

                lock (Sync)
                {
                    EnsureLoadedLocked();
                    FilterSet set = ResolveSetLocked(side, pickerNo);
                    if (set == null)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                            "Pick 런타임 오프셋 갱신 대상이 유효하지 않습니다. side=" + side +
                            ", pickerNo=" + pickerNo + ", die=" + (dieId ?? string.Empty) + " - Failed");
                        return;
                    }

                    // Disable 중 학습 중지(2026-08-19 팀장님 지시): 잔차 적분은 보정이 적용되지 않으면
                    // 잔차가 줄지 않아 무한 누적 → 클램프 알람이 뜬다. 적용 꺼짐이면 학습·저장도 멈춘다.
                    // (EMA 시절 "Disable이어도 학습 계속" 설계는 적분 전환으로 폐기.)
                    if (!_useCorrection)
                        return;

                    // 전 채널 잔차 적분(2026-08-18 팀장님 지시 — T는 적용 부호 가산 확정과 함께 적분 전환).
                    acceptedX = AcceptChannelLocked(set.X, effectiveOffsetX, _outlierLimitXyMm, "X", side, pickerNo, dieId, true);
                    acceptedY = AcceptChannelLocked(set.Y, preprocessedY, _outlierLimitXyMm, "Y", side, pickerNo, dieId, true);
                    acceptedT = AcceptChannelLocked(set.T, offsetT, _outlierLimitTDeg, "T", side, pickerNo, dieId, true);

                    if (acceptedX)
                        set.ClampLatchedX = ClampChannelLocked(set.X, _clampLimitXyMm, set.ClampLatchedX, "X", side, pickerNo);
                    if (acceptedY)
                        set.ClampLatchedY = ClampChannelLocked(set.Y, _clampLimitXyMm, set.ClampLatchedY, "Y", side, pickerNo);
                    if (acceptedT)
                        set.ClampLatchedT = ClampChannelLocked(set.T, _clampLimitTDeg, set.ClampLatchedT, "T", side, pickerNo);

                    if (acceptedX || acceptedY || acceptedT)
                    {
                        set.LastUpdated = DateTime.Now;
                        MarkStateChangedLocked();
                        // 다이당 실행되는 핫패스 — 디스크 쓰기를 락 밖 병합 저장으로 넘긴다.
                        RequestDeferredSave();
                    }

                    filteredX = set.X.Value;
                    filteredY = set.Y.Value;
                    filteredT = set.T.Value;
                }

                EventLogger.Write(
                    EventKind.Event,
                    "COORD",
                    "PICK-RUNTIME-OFFSET",
                    "Pick 런타임 오프셋 필터 갱신. side=" + side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + (dieId ?? string.Empty) +
                    ", shootFrame=" + shootFrame +
                    ", deltaThetaTeaching=" + F(deltaThetaTeaching) +
                    ", rotationCenter=(" + F(rotationCenterX) + "," + F(rotationCenterY) + ")(usable=" + rotationCenterUsable + ")" +
                    ", capturedPickerXCommand=" + F(capturedPickerXCommand) +
                    ", axisFromCameraMach=(" + F(axisFromCameraMachX) + "," + F(axisFromCameraMachY) + ")" +
                    ", measuredX=" + F(offsetX) +
                    ", effectiveX=" + F(effectiveOffsetX) + "(accepted=" + acceptedX + ")" +
                    ", rawOffsetY=" + F(rawOffsetY) +
                    ", effectiveRawY=" + F(effectiveRawOffsetY) +
                    ", capturedPickerYCommand=" + F(capturedPickerYCommand) +
                    ", colletCalY=" + F(colletCalY) +
                    ", basePicker4ColletCalY=" + F(basePicker4ColletCalY) +
                    ", colletYDeltaFromP4=" + F(colletYDeltaFromP4) +
                    ", preprocessedY=" + F(preprocessedY) + "(accepted=" + acceptedY + ")" +
                    ", measuredT=" + F(offsetT) + "(accepted=" + acceptedT + ")" +
                    ", filteredX=" + F(filteredX) +
                    ", filteredY=" + F(filteredY) +
                    ", filteredT=" + F(filteredT) +
                    ", updateMode=XYT:integral" +
                    ", enabled=" + _useCorrection);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 오프셋 갱신 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>
        /// 8세트 전체 필터 현재값 스냅샷을 반환한다(뷰어/메카 오프셋 이관용).
        /// 반환값은 복사본이며, Enable 여부와 무관하게 현재 학습 상태를 그대로 담는다.
        /// </summary>
        public static RuntimeOffsetSnapshot[] GetSnapshot()
        {
            try
            {
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    var rows = new RuntimeOffsetSnapshot[_filters.Length];
                    for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                    {
                        PickerSequenceSide side = sideIndex == 0
                            ? PickerSequenceSide.Front
                            : PickerSequenceSide.Rear;
                        for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                        {
                            int index = sideIndex * 4 + (pickerNo - 1);
                            FilterSet set = _filters[index];
                            rows[index] = new RuntimeOffsetSnapshot(
                                side, pickerNo, set.X.Value, set.Y.Value, set.T.Value, set.LastUpdated);
                        }
                    }

                    return rows;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 오프셋 스냅샷 조회 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return new RuntimeOffsetSnapshot[0];
            }
            finally
            {
            }
        }

        /// <summary>지정 (side, pickerNo)의 X/Y 채널만 0으로 초기화한다(메카 오프셋 이관 후 이중 보정 방지).</summary>
        public static void ResetXy(PickerSequenceSide side, int pickerNo)
        {
            try
            {
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    FilterSet set = ResolveSetLocked(side, pickerNo);
                    if (set == null)
                        return;

                    set.X.Reset(0.0);
                    set.Y.Reset(0.0);
                    set.ClampLatchedX = false;
                    set.ClampLatchedY = false;
                    set.LastUpdated = DateTime.Now;
                    MarkStateChangedLocked();
                    SaveLocked();
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 오프셋 X/Y를 초기화했습니다(메카 오프셋 이관). side=" + side +
                    ", pickerNo=" + pickerNo + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 오프셋 X/Y 초기화 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>지정 (side, pickerNo)의 필터·저장값을 0으로 초기화한다.</summary>
        public static void Reset(PickerSequenceSide side, int pickerNo)
        {
            try
            {
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    FilterSet set = ResolveSetLocked(side, pickerNo);
                    if (set == null)
                        return;

                    set.X.Reset(0.0);
                    set.Y.Reset(0.0);
                    set.T.Reset(0.0);
                    set.ClampLatchedX = false;
                    set.ClampLatchedY = false;
                    set.ClampLatchedT = false;
                    set.LastUpdated = DateTime.Now;
                    MarkStateChangedLocked();
                    SaveLocked();
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 오프셋을 초기화했습니다. side=" + side + ", pickerNo=" + pickerNo + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 오프셋 초기화 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>8세트 전체 필터·저장값을 0으로 초기화한다.</summary>
        public static void ResetAll()
        {
            try
            {
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    for (int i = 0; i < _filters.Length; i++)
                    {
                        _filters[i].X.Reset(0.0);
                        _filters[i].Y.Reset(0.0);
                        _filters[i].T.Reset(0.0);
                        _filters[i].ClampLatchedX = false;
                        _filters[i].ClampLatchedY = false;
                        _filters[i].ClampLatchedT = false;
                        _filters[i].LastUpdated = DateTime.Now;
                    }

                    MarkStateChangedLocked();
                    SaveLocked();
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 오프셋 전체를 초기화했습니다. - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 오프셋 전체 초기화 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>테스트/재로드용: 다음 접근 시 저장 파일에서 상태를 다시 로드한다.</summary>
        public static void Reload()
        {
            lock (Sync)
            {
                _loaded = false;
                _filters = null;
            }
        }

        // ── 내부 구현 (Sync lock 안에서만 호출) ─────────────────────

        private static void EnsureLoadedLocked()
        {
            if (_loaded)
                return;

            PickRuntimeOffsetDocument document = PickRuntimeOffsetStore.Load();
            _useCorrection = document.UsePickRuntimeOffset;
            _cutoffFrequency = document.CutoffFrequency > 0.0 ? document.CutoffFrequency : 0.1;
            _outlierLimitXyMm = document.OutlierLimitXyMm;
            _outlierLimitTDeg = document.OutlierLimitTDeg;
            _clampLimitXyMm = document.ClampLimitXyMm;
            _clampLimitTDeg = document.ClampLimitTDeg;
            _filters = new FilterSet[8];
            for (int i = 0; i < _filters.Length; i++)
                _filters[i] = new FilterSet(_cutoffFrequency);

            if (document.Filters != null)
            {
                foreach (PickRuntimeOffsetRow row in document.Filters)
                {
                    if (row == null)
                        continue;

                    PickerSequenceSide side;
                    if (!Enum.TryParse(row.Side, true, out side))
                        continue;

                    FilterSet set = ResolveSetLocked(side, row.PickerNo);
                    if (set == null)
                        continue;

                    set.X.Reset(row.FilteredX);
                    set.Y.Reset(row.FilteredY);
                    set.T.Reset(row.FilteredT);
                    DateTime updated;
                    if (DateTime.TryParse(row.LastUpdated, out updated))
                        set.LastUpdated = updated;
                }
            }

            _loaded = true;
        }

        private static FilterSet ResolveSetLocked(PickerSequenceSide side, int pickerNo)
        {
            if (pickerNo < 1 || pickerNo > 4)
                return null;

            int sideIndex = side == PickerSequenceSide.Front ? 0 : 1;
            return _filters[sideIndex * 4 + (pickerNo - 1)];
        }

        private static bool AcceptChannelLocked(
            LowPassFilter filter,
            double input,
            double outlierLimit,
            string channel,
            PickerSequenceSide side,
            int pickerNo,
            string dieId,
            bool integrateResidual)
        {
            // 잔차 적분 모드(2026-08-18): input은 잔차이므로 이상치도 잔차 크기로 판정한다.
            // 기존 |input−F|를 유지하면 적분으로 F가 커진 뒤 정상 잔차(≈0)까지 전부 기각된다.
            double deviation = integrateResidual
                ? Math.Abs(input)
                : Math.Abs(input - filter.Value);
            if (deviation >= outlierLimit)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                    "Pick 런타임 오프셋 이상치 샘플을 폐기했습니다. channel=" + channel +
                    ", side=" + side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + (dieId ?? string.Empty) +
                    ", input=" + F(input) +
                    ", filtered=" + F(filter.Value) +
                    ", limit=" + F(outlierLimit) + " - Check");
                return false;
            }

            // 적분: 전체 오차 재구성(F+m)을 EMA에 입력하면 F += α·m 이 된다(필터 클래스 무수정).
            filter.Update(integrateResidual ? filter.Value + input : input);
            return true;
        }

        /// <summary>
        /// 갱신 후 필터 상태를 ±limit로 클램프한다. 한계 도달 시 래치되지 않은 경우에만 Warning을 발생시키고,
        /// 한계 미만이면 래치를 해제한다. 반환값은 갱신된 래치 상태.
        /// </summary>
        private static bool ClampChannelLocked(
            LowPassFilter filter,
            double limit,
            bool latched,
            string channel,
            PickerSequenceSide side,
            int pickerNo)
        {
            double value = filter.Value;
            if (Math.Abs(value) < limit)
                return false;   // 한계 미만 복귀 → 래치 해제(재무장)

            double clamped = value > 0.0 ? limit : -limit;
            filter.Reset(clamped);

            if (!latched)
            {
                string message =
                    "Pick 런타임 오프셋이 발산 방지 한계에 도달해 클램프되었습니다. side=" + side +
                    ", pickerNo=" + pickerNo +
                    ", channel=" + channel +
                    ", valueBeforeClamp=" + F(value) +
                    ", limit=" + F(limit);
                //AlarmManager.Raise(AlarmSeverity.Warning, "PICK-RUNTIME-OFFSET-CLAMP", "PickRuntimeOffset", message);
                EventLogger.Write(EventKind.Warning, "COORD", "PICK-RUNTIME-OFFSET-CLAMP", message);
            }

            return true;
        }

        /// <summary>설정 입력 검증. 전 항목을 검사해 위반을 전부 담은 사유를 반환한다(정상이면 null).</summary>
        private static string BuildFilterSettingsRejectReason(
            double cutoffFrequency,
            double outlierLimitXyMm,
            double outlierLimitTDeg,
            double clampLimitXyMm,
            double clampLimitTDeg)
        {
            var reasons = new System.Text.StringBuilder();
            AppendRangeViolation(reasons, "fc", cutoffFrequency, MinCutoffFrequency, MaxCutoffFrequency);
            AppendRangeViolation(reasons, "outlierXy", outlierLimitXyMm, MinFilterLimit, MaxFilterLimit);
            AppendRangeViolation(reasons, "outlierT", outlierLimitTDeg, MinFilterLimit, MaxFilterLimit);
            AppendRangeViolation(reasons, "clampXy", clampLimitXyMm, MinFilterLimit, MaxFilterLimit);
            AppendRangeViolation(reasons, "clampT", clampLimitTDeg, MinFilterLimit, MaxFilterLimit);
            return reasons.Length > 0 ? reasons.ToString() : null;
        }

        private static void AppendRangeViolation(
            System.Text.StringBuilder reasons, string item, double value, double min, double max)
        {
            if (!double.IsNaN(value) && !double.IsInfinity(value) && value >= min && value <= max)
                return;

            if (reasons.Length > 0)
                reasons.Append(", ");
            reasons.Append(item + "=" + F(value) + "(허용 " + F(min) + "~" + F(max) + ")");
        }

        /// <summary>
        /// 한계 축소 시 범위를 벗어난 채널을 새 한계로 재클램프한다(워닝 래치는 조작하지 않는다).
        /// 잘린 채널은 before→after 로그 라인을 수집한다.
        /// </summary>
        private static void ReclampChannelLocked(
            LowPassFilter filter,
            double limit,
            PickerSequenceSide side,
            int pickerNo,
            string channel,
            System.Collections.Generic.List<string> reclampLines)
        {
            double value = filter.Value;
            if (Math.Abs(value) <= limit)
                return;

            double clamped = value > 0.0 ? limit : -limit;
            filter.Reset(clamped);
            reclampLines.Add(
                "side=" + side + ", pickerNo=" + pickerNo + ", channel=" + channel +
                ", before=" + F(value) + ", after=" + F(clamped) + ", limit=" + F(limit));
        }

        /// <summary>
        /// 핫패스용 병합 저장 요청. 호출자는 디스크를 기다리지 않는다.
        /// Sync 락을 쥔 상태에서 호출해도 안전하다(여기서는 플래그만 세운다).
        /// </summary>
        private static void RequestDeferredSave()
        {
            lock (DeferredSaveSync)
            {
                _deferredSaveRequested = true;
                if (_deferredSaveWorkerRunning)
                    return;

                _deferredSaveWorkerRunning = true;
            }

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        await System.Threading.Tasks.Task.Delay(DeferredSaveQuietMs).ConfigureAwait(false);

                        lock (DeferredSaveSync)
                        {
                            if (!_deferredSaveRequested)
                            {
                                _deferredSaveWorkerRunning = false;
                                return;
                            }

                            _deferredSaveRequested = false;
                        }

                        SaveOutsideLock();
                    }
                }
                catch (Exception ex)
                {
                    lock (DeferredSaveSync)
                    {
                        _deferredSaveWorkerRunning = false;
                    }

                    QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                        "Pick 런타임 오프셋 지연 저장 워커가 실패했습니다. error=" + ex.Message + " - Failed");
                }
            });
        }

        /// <summary>대기 중인 지연 저장을 즉시 반영한다(종료/수동 확정 시 호출).</summary>
        public static void FlushPendingSave()
        {
            TryFlushPendingSave("FlushPendingSave");
        }

        /// <summary>
        /// 앱 종료/정지 배리어용 동기 저장. 성공 여부를 반환한다.
        /// [내구성 워터마크 2026-08-18] "호출 시점의 자료가 디스크에 있는가"로 판정한다.
        /// 이미 확정돼 있으면 쓰지 않고, 동시 저장이 더 새 세대를 먼저 확정했어도 성공이다
        /// (그 문서가 내 자료를 포함하므로). 경합을 기다리거나 재시도하지 않는다.
        /// 지연 저장의 1000ms 무음 창 유실도 세대 판정으로 함께 해결된다 — 미확정이면 여기서 쓴다.
        /// </summary>
        public static bool TryFlushPendingSave(string reason)
        {
            long targetVersion;
            lock (Sync)
            {
                EnsureLoadedLocked();
                targetVersion = _stateVersion;
            }

            lock (DeferredSaveSync)
            {
                _deferredSaveRequested = false;
            }

            bool alreadyDurable = IsStateVersionDurable(targetVersion);
            if (!alreadyDurable)
                SaveOutsideLock();

            bool durable = IsStateVersionDurable(targetVersion);
            QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                "Pick 런타임 오프셋 동기 flush. reason=" + (reason ?? "-") +
                ", targetVersion=" + targetVersion +
                ", durableVersion=" + System.Threading.Interlocked.Read(ref _durableStateVersion) +
                ", wrote=" + (!alreadyDurable) +
                " - " + (durable ? "Ok" : "Failed"));
            return durable;
        }

        /// <summary>자료 변경 세대를 올린다(Sync 락 안에서만 호출).</summary>
        private static void MarkStateChangedLocked()
        {
            _stateVersion++;
        }

        /// <summary>지정 자료 세대가 디스크에 확정됐는지 판정한다(단조 워터마크 비교).</summary>
        private static bool IsStateVersionDurable(long targetVersion)
        {
            return System.Threading.Interlocked.Read(ref _durableStateVersion) >= targetVersion;
        }

        /// <summary>
        /// 디스크 확정 세대를 단조 증가로 게시한다. SaveLocked는 Sync를 쥔 채, SaveOutsideLock은
        /// IoSync를 쥔 채 호출하므로 락으로 보호하면 순서가 역전된다 — Interlocked CAS로 락 없이 처리한다.
        /// </summary>
        private static void PublishDurableStateVersion(long capturedVersion)
        {
            while (true)
            {
                long current = System.Threading.Interlocked.Read(ref _durableStateVersion);
                if (capturedVersion <= current)
                    return;
                if (System.Threading.Interlocked.CompareExchange(
                        ref _durableStateVersion, capturedVersion, current) == current)
                    return;
            }
        }

        // 문서 구성만 Sync 락 안에서 하고, 디스크 쓰기는 락 밖에서 수행한다.
        private static bool SaveOutsideLock()
        {
            lock (DeferredSaveIoSync)
            {
                PickRuntimeOffsetDocument document;
                long capturedStateVersion;
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    // 문서와 세대를 같은 락 안에서 집어야 "이 문서가 담은 세대"가 정확히 확정된다.
                    capturedStateVersion = _stateVersion;
                    document = BuildDocumentLocked();
                }

                bool saved = PickRuntimeOffsetStore.Save(document);
                if (saved)
                    PublishDurableStateVersion(capturedStateVersion);
                return saved;
            }
        }

        private static void SaveLocked()
        {
            // Sync를 쥔 채 호출된다 — 지금 메모리 세대가 곧 이 문서가 담는 세대다.
            long capturedStateVersion = _stateVersion;
            if (PickRuntimeOffsetStore.Save(BuildDocumentLocked()))
                PublishDurableStateVersion(capturedStateVersion);
        }

        private static PickRuntimeOffsetDocument BuildDocumentLocked()
        {
            var document = new PickRuntimeOffsetDocument();
            document.UsePickRuntimeOffset = _useCorrection;
            document.CutoffFrequency = _cutoffFrequency;
            document.OutlierLimitXyMm = _outlierLimitXyMm;
            document.OutlierLimitTDeg = _outlierLimitTDeg;
            document.ClampLimitXyMm = _clampLimitXyMm;
            document.ClampLimitTDeg = _clampLimitTDeg;
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                PickerSequenceSide side = sideIndex == 0 ? PickerSequenceSide.Front : PickerSequenceSide.Rear;
                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    FilterSet set = _filters[sideIndex * 4 + (pickerNo - 1)];
                    document.Filters.Add(new PickRuntimeOffsetRow
                    {
                        Side = side.ToString(),
                        PickerNo = pickerNo,
                        FilteredX = set.X.Value,
                        FilteredY = set.Y.Value,
                        FilteredT = set.T.Value,
                        LastUpdated = set.LastUpdated == DateTime.MinValue
                            ? string.Empty
                            : set.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

            return document;
        }

        private static string F(double value)
        {
            return value.ToString("F6");
        }

        // 티칭 각도차를 (−180, 180]로 정규화한다 — 0°/180° 촬영각 분기 판정용.
        private static double NormalizeDegreesPlusMinus180(double degrees)
        {
            double normalized = degrees % 360.0;
            if (normalized > 180.0)
                normalized -= 360.0;
            else if (normalized <= -180.0)
                normalized += 360.0;
            return normalized;
        }
    }
}
