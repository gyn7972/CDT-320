using System;
using System.Text;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Place 런타임 오프셋 실시간 보정 서비스.
    /// Place → Bin 비전 후검사 결과(X/Y/T 오프셋)를 누적해
    /// 다음 Place 목표 좌표 계산에 반영하는 폐루프 보정의 상태 보관소다.
    /// - 갱신식(2026-08-18 팀장님 지시): 폐루프 측정값은 보정 적용 후 잔차(m)이므로 전 채널을
    ///   잔차 적분(F += α·m — 잔차 0 수렴)으로 갱신한다. 잔차를 EMA(F += α(m−F))에 그대로
    ///   넣으면 오차 절반에서 평형이 생겨 폐루프에 부적합.
    ///   (T는 2026-08-19 적용 부호 가산 확정(로그 판정)과 함께 적분 전환 — 전환 전 학습값이
    ///    남아 있으면 첫 잔차가 이상치 한계를 넘어 전 샘플 기각으로 고착될 수 있으니
    ///    재가동 전 T 리셋 필수.)
    /// - 필터 단위: PickerSide(Front/Rear) × PickerNo(1~4) = 8세트, 각 X/Y/T 3채널 독립.
    /// - 필터 상태는 비전 측정 부호 그대로(raw) 저장하고, 부호 변환(전 채널 감산 — X/Y 2026-07-29
    ///   확정, T는 2026-08-19 01:26 실장비 재확정: 가산 적용 시 bin각·필터 동반 램프로 반증됨)은
    ///   적용 지점(DieCoordinateTransformService.CalculatePlaceTarget)에서 수행한다.
    /// - Enable/Disable(UsePlaceRuntimeOffset): Disable이면 적용과 학습을 모두 중지한다
    ///   (2026-08-19 팀장님 지시 — 적분 갱신은 미적용 상태에서 학습하면 무한 누적·클램프 알람.
    ///    EMA 시절 "학습 계속" 설계 폐기). Enable 판정은 적용 지점에서 GetOffset 사용 여부로 결정.
    /// - 발산 방지: 갱신 후 상태값을 설정 한계(기본 X/Y ±0.50mm, T ±1.0°)로 클램프하고 한계 도달 시
    ///   Warning을 1회 발생(한계 미만 복귀 시 재무장하는 래치) — Pick 보정과 동일 정책.
    /// - 검사 큐 스레드에서 갱신, 시퀀스 스레드에서 조회하므로 lock으로 보호한다.
    /// </summary>
    internal static class PlaceRuntimeOffsetService
    {
        // 필터 설정 입력 허용 범위(SetFilterSettings·설정 다이얼로그 공통, 2026-08-16 팀장님 승인).
        public const double MinCutoffFrequency = 0.001;
        public const double MaxCutoffFrequency = 1.0;
        public const double MinFilterLimit = 0.01;
        public const double MaxFilterLimit = 5.0;

        // 필터 한계 설정값(스토어 로드, SetFilterSettings로 변경) — 판정 알고리즘은 무변경.
        // 이상치 거부 한계: 현재 필터 출력 대비 편차가 이 값 이상이면 해당 채널 샘플 폐기.
        private static double _outlierLimitXyMm = PlaceRuntimeOffsetDocument.DefaultOutlierLimitXyMm;
        private static double _outlierLimitTDeg = PlaceRuntimeOffsetDocument.DefaultOutlierLimitTDeg;
        // 발산 방지 클램프 한계: 필터 상태값 자체를 이 범위로 제한.
        private static double _clampLimitXyMm = PlaceRuntimeOffsetDocument.DefaultClampLimitXyMm;
        private static double _clampLimitTDeg = PlaceRuntimeOffsetDocument.DefaultClampLimitTDeg;

        private static readonly object Sync = new object();
        private static bool _loaded;
        private static bool _useCorrection;
        private static double _cutoffFrequency = 0.05;
        private static FilterSet[] _filters;

        // 웨이퍼 1장용 2차 루프: 웨이퍼 시작 후 측정값 12개까지는 저장 필터(1번)로 적용·학습하고,
        // 13번째부터는 1번을 복사한 웨이퍼 필터(2번)로 전환해 완료까지 적용·학습한다.
        // 2번 필터와 카운터는 메모리 전용 — 웨이퍼가 바뀌거나 앱이 재시작되면 1번 체제로 복귀.
        private const int WaferLoopSeedSampleCount = 12;
        private static FilterSet[] _waferFilters;
        private static string _waferKey;
        private static int _waferSampleCount;
        private static bool _waferLoopActive;

        // [택타임 개선 2026-07-27] Output 후검사마다(다이당 1회) Sync 락을 쥔 채 File.Create + JSON 쓰기를
        // 동기로 수행했다. 저장 내용은 고정 8행뿐이라 병합 저장해도 잃는 정보가 없다.
        // Pick 측(PickRuntimeOffsetService)과 동일한 방식으로 처리한다.
        private const int DeferredSaveQuietMs = 1000;
        private static readonly object DeferredSaveSync = new object();
        private static readonly object DeferredSaveIoSync = new object();
        // [내구성 워터마크 2026-08-18] Material 저장과 동일 규약. 저장 완료를 "몇 번째 저장 시도인가"가
        // 아니라 "어느 자료 세대가 디스크에 있는가"로 판정한다. 동시 저장(지연 워커 + 종료 flush)이
        // 겹쳐도 더 새 저장이 내 자료를 포함하므로 성공이며, 기다리거나 재시도할 필요가 없다.
        //   _stateVersion       : 필터/설정이 바뀔 때마다 증가 (Sync 보호)
        //   _durableStateVersion: 디스크에 확정된 최대 세대, 단조 증가 (DeferredSaveIoSync 보호)
        // 둘 다 0에서 시작 — 기동 직후 로드된 상태는 이미 파일에 있으므로 내구성 있음(0>=0).
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

        /// <summary>Place 런타임 보정 적용 여부 (UsePlaceRuntimeOffset 설정값).</summary>
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
                }

                SaveOutsideLock();

                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 오프셋 사용 설정을 변경했습니다. enabled=" + enabled + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 오프셋 사용 설정 변경 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
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
                    QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                        "Place 런타임 필터 설정 입력이 허용 범위를 벗어나 거부했습니다. " + rejectReason + " - Failed");
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

                    for (int i = 0; i < _waferFilters.Length; i++)
                    {
                        FilterSet set = _waferFilters[i];
                        set.X.SetCutoffFrequency(_cutoffFrequency);
                        set.Y.SetCutoffFrequency(_cutoffFrequency);
                        set.T.SetCutoffFrequency(_cutoffFrequency);

                        PickerSequenceSide side = i < 4 ? PickerSequenceSide.Front : PickerSequenceSide.Rear;
                        int pickerNo = (i % 4) + 1;
                        ReclampChannelLocked(set.X, _clampLimitXyMm, side, pickerNo, "X/wafer", reclampLines);
                        ReclampChannelLocked(set.Y, _clampLimitXyMm, side, pickerNo, "Y/wafer", reclampLines);
                        ReclampChannelLocked(set.T, _clampLimitTDeg, side, pickerNo, "T/wafer", reclampLines);
                    }

                    MarkStateChangedLocked();
                }

                SaveOutsideLock();

                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 필터 설정을 변경했습니다." +
                    " fc=" + F(oldFc) + "→" + F(cutoffFrequency) +
                    "(alpha=" + F(LowPassFilter.CalculateAlpha(oldFc)) + "→" + F(LowPassFilter.CalculateAlpha(cutoffFrequency)) + ")" +
                    ", outlierXy=" + F(oldOutlierXy) + "→" + F(outlierLimitXyMm) +
                    ", outlierT=" + F(oldOutlierT) + "→" + F(outlierLimitTDeg) +
                    ", clampXy=" + F(oldClampXy) + "→" + F(clampLimitXyMm) +
                    ", clampT=" + F(oldClampT) + "→" + F(clampLimitTDeg) + " - Ok");

                foreach (string line in reclampLines)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                        "Place 런타임 오프셋을 축소된 클램프 한계로 재클램프했습니다. " + line + " - Check");
                }

                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 필터 설정 변경 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 현재 필터 출력(raw)을 반환한다. 미초기화/범위 밖 인자면 0을 반환한다.
        /// waferKey(다이 소스 웨이퍼)로 루프를 선택한다 — 전환 전 1번(저장), 전환 후 2번(웨이퍼).
        /// Enable 여부와 무관하게 상태를 반환하며, Enable 판정은 적용 지점에서 한다.
        /// </summary>
        public static void GetOffset(PickerSequenceSide side, int pickerNo, string waferKey, out double x, out double y, out double t)
        {
            x = 0.0;
            y = 0.0;
            t = 0.0;

            try
            {
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    EnsureWaferContextLocked(waferKey);
                    FilterSet set = ResolveSetLocked(_waferLoopActive ? _waferFilters : _filters, side, pickerNo);
                    if (set == null)
                        return;

                    x = set.X.Value;
                    y = set.Y.Value;
                    t = set.T.Value;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 오프셋 조회 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>
        /// Bin 비전 후검사 오프셋 1샘플을 반영한다.
        /// 채널별 이상치 검사 → 통과 채널만 필터 갱신 → 저장 → 로그.
        /// </summary>
        public static void OnInspectionOffset(
            PickerSequenceSide side,
            int pickerNo,
            double measuredX,
            double measuredY,
            double measuredT,
            string dieId,
            string waferKey)
        {
            try
            {
                double filteredX;
                double filteredY;
                double filteredT;
                bool acceptedX;
                bool acceptedY;
                bool acceptedT;
                bool waferLoop;
                int sampleNo;

                lock (Sync)
                {
                    EnsureLoadedLocked();

                    // Disable 중 학습 중지(2026-08-19 팀장님 지시): 잔차 적분은 보정이 적용되지 않으면
                    // 잔차가 줄지 않아 무한 누적 → 클램프 알람이 뜬다. 적용 꺼짐이면 학습·저장도 멈춘다.
                    if (!_useCorrection)
                        return;

                    EnsureWaferContextLocked(waferKey);
                    waferLoop = _waferLoopActive;
                    FilterSet set = ResolveSetLocked(waferLoop ? _waferFilters : _filters, side, pickerNo);
                    if (set == null)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                            "Place 런타임 오프셋 갱신 대상이 유효하지 않습니다. side=" + side +
                            ", pickerNo=" + pickerNo + ", die=" + (dieId ?? string.Empty) + " - Failed");
                        return;
                    }

                    string chX = waferLoop ? "X/wafer" : "X";
                    string chY = waferLoop ? "Y/wafer" : "Y";
                    string chT = waferLoop ? "T/wafer" : "T";
                    acceptedX = AcceptChannelLocked(set.X, measuredX, _outlierLimitXyMm, chX, side, pickerNo, dieId, true);
                    acceptedY = AcceptChannelLocked(set.Y, measuredY, _outlierLimitXyMm, chY, side, pickerNo, dieId, true);
                    acceptedT = AcceptChannelLocked(set.T, measuredT, _outlierLimitTDeg, chT, side, pickerNo, dieId, true);

                    if (acceptedX)
                        set.ClampLatchedX = ClampChannelLocked(set.X, _clampLimitXyMm, set.ClampLatchedX, chX, side, pickerNo);
                    if (acceptedY)
                        set.ClampLatchedY = ClampChannelLocked(set.Y, _clampLimitXyMm, set.ClampLatchedY, chY, side, pickerNo);
                    if (acceptedT)
                        set.ClampLatchedT = ClampChannelLocked(set.T, _clampLimitTDeg, set.ClampLatchedT, chT, side, pickerNo);

                    if (acceptedX || acceptedY || acceptedT)
                    {
                        set.LastUpdated = DateTime.Now;
                        // 2번(웨이퍼) 루프는 메모리 전용 — 1번(저장) 갱신만 디스크 저장 대상.
                        if (!waferLoop)
                        {
                            MarkStateChangedLocked();
                            RequestDeferredSave();
                        }
                    }

                    _waferSampleCount++;
                    sampleNo = _waferSampleCount;
                    if (!_waferLoopActive && _waferSampleCount >= WaferLoopSeedSampleCount)
                        SeedWaferLoopLocked();

                    filteredX = set.X.Value;
                    filteredY = set.Y.Value;
                    filteredT = set.T.Value;
                }

                EventLogger.Write(
                    EventKind.Event,
                    "COORD",
                    "PLACE-RUNTIME-OFFSET",
                    "Place 런타임 오프셋 필터 갱신. side=" + side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + (dieId ?? string.Empty) +
                    ", measuredX=" + F(measuredX) + "(accepted=" + acceptedX + ")" +
                    ", measuredY=" + F(measuredY) + "(accepted=" + acceptedY + ")" +
                    ", measuredT=" + F(measuredT) + "(accepted=" + acceptedT + ")" +
                    ", filteredX=" + F(filteredX) +
                    ", filteredY=" + F(filteredY) +
                    ", filteredT=" + F(filteredT) +
                    ", loop=" + (waferLoop ? "Wafer" : "Saved") +
                    ", waferSample=" + sampleNo +
                    ", updateMode=XYT:integral");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 오프셋 갱신 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
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
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 오프셋 스냅샷 조회 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
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

                    // 이관 후 이중 보정 방지 — 활성 중인 웨이퍼 루프의 X/Y도 함께 0.
                    FilterSet waferSet = ResolveSetLocked(_waferFilters, side, pickerNo);
                    if (waferSet != null)
                    {
                        waferSet.X.Reset(0.0);
                        waferSet.Y.Reset(0.0);
                        waferSet.ClampLatchedX = false;
                        waferSet.ClampLatchedY = false;
                    }

                    MarkStateChangedLocked();
                }

                SaveOutsideLock();

                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 오프셋 X/Y를 초기화했습니다(메카 오프셋 이관). side=" + side +
                    ", pickerNo=" + pickerNo + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 오프셋 X/Y 초기화 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
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

                    ResetSetLocked(set);
                    set.LastUpdated = DateTime.Now;

                    FilterSet waferSet = ResolveSetLocked(_waferFilters, side, pickerNo);
                    if (waferSet != null)
                        ResetSetLocked(waferSet);

                    MarkStateChangedLocked();
                }

                SaveOutsideLock();

                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 오프셋을 초기화했습니다. side=" + side + ", pickerNo=" + pickerNo + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 오프셋 초기화 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
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
                        ResetSetLocked(_filters[i]);
                        _filters[i].LastUpdated = DateTime.Now;
                    }

                    for (int i = 0; i < _waferFilters.Length; i++)
                        ResetSetLocked(_waferFilters[i]);
                    _waferSampleCount = 0;
                    _waferLoopActive = false;

                    MarkStateChangedLocked();
                }

                SaveOutsideLock();

                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 오프셋 전체를 초기화했습니다. - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 오프셋 전체 초기화 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>웨이퍼 옵셋(2번) 현재값을 저장 옵셋(1번)에 덮어써 저장한다(수동 확정). 2번 미활성이면 false.</summary>
        public static bool CopyWaferLoopToSaved(out string failReason)
        {
            failReason = "";
            try
            {
                var lines = new StringBuilder();
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    if (!_waferLoopActive)
                    {
                        failReason = "웨이퍼 옵셋(2번)이 아직 활성화되지 않았습니다(현재 웨이퍼 측정값 " +
                            _waferSampleCount + "/" + WaferLoopSeedSampleCount + "개).";
                        return false;
                    }

                    for (int i = 0; i < _filters.Length; i++)
                    {
                        FilterSet from = _waferFilters[i];
                        FilterSet to = _filters[i];
                        to.X.Reset(from.X.Value);
                        to.Y.Reset(from.Y.Value);
                        to.T.Reset(from.T.Value);
                        to.ClampLatchedX = false;
                        to.ClampLatchedY = false;
                        to.ClampLatchedT = false;
                        to.LastUpdated = DateTime.Now;
                        lines.Append(i < 4 ? "F" : "R").Append((i % 4) + 1)
                            .Append(" X=").Append(F(from.X.Value))
                            .Append(" Y=").Append(F(from.Y.Value))
                            .Append(" T=").Append(F(from.T.Value)).Append("; ");
                    }

                    MarkStateChangedLocked();
                }

                SaveOutsideLock();
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "웨이퍼 옵셋(2번)을 저장 옵셋(1번)으로 수동 복사했습니다. " + lines + "- Ok");
                return true;
            }
            catch (Exception ex)
            {
                failReason = ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "웨이퍼 옵셋(2번)→저장 옵셋(1번) 복사 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return false;
            }
        }

        /// <summary>테스트/재로드용: 다음 접근 시 저장 파일에서 상태를 다시 로드한다.</summary>
        public static void Reload()
        {
            lock (Sync)
            {
                _loaded = false;
                _filters = null;
                _waferFilters = null;
            }
        }

        // ── 내부 구현 (Sync lock 안에서만 호출) ─────────────────────

        private static void EnsureLoadedLocked()
        {
            if (_loaded)
                return;

            PlaceRuntimeOffsetDocument document = PlaceRuntimeOffsetStore.Load();
            _useCorrection = document.UsePlaceRuntimeOffset;
            _cutoffFrequency = document.CutoffFrequency > 0.0 ? document.CutoffFrequency : 0.1;
            _outlierLimitXyMm = document.OutlierLimitXyMm;
            _outlierLimitTDeg = document.OutlierLimitTDeg;
            _clampLimitXyMm = document.ClampLimitXyMm;
            _clampLimitTDeg = document.ClampLimitTDeg;
            _filters = new FilterSet[8];
            for (int i = 0; i < _filters.Length; i++)
                _filters[i] = new FilterSet(_cutoffFrequency);
            _waferFilters = new FilterSet[8];
            for (int i = 0; i < _waferFilters.Length; i++)
                _waferFilters[i] = new FilterSet(_cutoffFrequency);
            _waferKey = null;
            _waferSampleCount = 0;
            _waferLoopActive = false;

            if (document.Filters != null)
            {
                foreach (PlaceRuntimeOffsetRow row in document.Filters)
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
            return ResolveSetLocked(_filters, side, pickerNo);
        }

        private static FilterSet ResolveSetLocked(FilterSet[] filters, PickerSequenceSide side, int pickerNo)
        {
            if (pickerNo < 1 || pickerNo > 4)
                return null;

            int sideIndex = side == PickerSequenceSide.Front ? 0 : 1;
            return filters[sideIndex * 4 + (pickerNo - 1)];
        }

        private static void ResetSetLocked(FilterSet set)
        {
            set.X.Reset(0.0);
            set.Y.Reset(0.0);
            set.T.Reset(0.0);
            set.ClampLatchedX = false;
            set.ClampLatchedY = false;
            set.ClampLatchedT = false;
        }

        private static void EnsureWaferContextLocked(string waferKey)
        {
            string key = waferKey ?? "";
            if (string.Equals(_waferKey, key, StringComparison.OrdinalIgnoreCase))
                return;

            bool firstAssign = _waferKey == null;
            _waferKey = key;
            _waferSampleCount = 0;
            _waferLoopActive = false;
            for (int i = 0; i < _waferFilters.Length; i++)
                ResetSetLocked(_waferFilters[i]);

            if (!firstAssign)
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "웨이퍼 변경 — 웨이퍼 옵셋(2번)을 폐기하고 저장 옵셋(1번)으로 복귀합니다. wafer=" + key + " - Ok");
        }

        private static void SeedWaferLoopLocked()
        {
            for (int i = 0; i < _filters.Length; i++)
            {
                _waferFilters[i].X.Reset(_filters[i].X.Value);
                _waferFilters[i].Y.Reset(_filters[i].Y.Value);
                _waferFilters[i].T.Reset(_filters[i].T.Value);
                _waferFilters[i].ClampLatchedX = false;
                _waferFilters[i].ClampLatchedY = false;
                _waferFilters[i].ClampLatchedT = false;
                _waferFilters[i].LastUpdated = DateTime.Now;
            }

            _waferLoopActive = true;
            QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                "측정값 " + WaferLoopSeedSampleCount + "개 도달 — 저장 옵셋(1번)을 웨이퍼 옵셋(2번)으로 복사하고 전환합니다. wafer=" + _waferKey + " - Ok");
        }

        private static bool AcceptChannelLocked(
            LowPassFilter filter,
            double measured,
            double outlierLimit,
            string channel,
            PickerSequenceSide side,
            int pickerNo,
            string dieId,
            bool integrateResidual)
        {
            // 잔차 적분 모드(2026-08-18): measured는 잔차이므로 이상치도 잔차 크기로 판정한다.
            // 기존 |measured−F|를 유지하면 적분으로 F가 커진 뒤 정상 잔차(≈0)까지 전부 기각된다.
            double deviation = integrateResidual
                ? Math.Abs(measured)
                : Math.Abs(measured - filter.Value);
            if (deviation >= outlierLimit)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                    "Place 런타임 오프셋 이상치 샘플을 폐기했습니다. channel=" + channel +
                    ", side=" + side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + (dieId ?? string.Empty) +
                    ", measured=" + F(measured) +
                    ", filtered=" + F(filter.Value) +
                    ", limit=" + F(outlierLimit) + " - Check");
                return false;
            }

            // 적분: 전체 오차 재구성(F+m)을 EMA에 입력하면 F += α·m 이 된다(필터 클래스 무수정).
            filter.Update(integrateResidual ? filter.Value + measured : measured);
            return true;
        }

        /// <summary>
        /// 갱신 후 필터 상태를 ±limit로 클램프한다. 한계 도달 시 래치되지 않은 경우에만 Warning을 발생시키고,
        /// 한계 미만이면 래치를 해제한다. 반환값은 갱신된 래치 상태. (Pick 보정과 동일 정책)
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
                    "Place 런타임 오프셋이 발산 방지 한계에 도달해 클램프되었습니다. side=" + side +
                    ", pickerNo=" + pickerNo +
                    ", channel=" + channel +
                    ", valueBeforeClamp=" + F(value) +
                    ", limit=" + F(limit);
                AlarmManager.Raise(AlarmSeverity.Warning, "PLACE-RUNTIME-OFFSET-CLAMP", "PlaceRuntimeOffset", message);
                EventLogger.Write(EventKind.Warning, "COORD", "PLACE-RUNTIME-OFFSET-CLAMP", message);
            }

            return true;
        }

        /// <summary>설정 입력 검증. 전 항목을 검사해 첫 위반부터 전부 담은 사유를 반환한다(정상이면 null).</summary>
        private static string BuildFilterSettingsRejectReason(
            double cutoffFrequency,
            double outlierLimitXyMm,
            double outlierLimitTDeg,
            double clampLimitXyMm,
            double clampLimitTDeg)
        {
            var reasons = new StringBuilder();
            AppendRangeViolation(reasons, "fc", cutoffFrequency, MinCutoffFrequency, MaxCutoffFrequency);
            AppendRangeViolation(reasons, "outlierXy", outlierLimitXyMm, MinFilterLimit, MaxFilterLimit);
            AppendRangeViolation(reasons, "outlierT", outlierLimitTDeg, MinFilterLimit, MaxFilterLimit);
            AppendRangeViolation(reasons, "clampXy", clampLimitXyMm, MinFilterLimit, MaxFilterLimit);
            AppendRangeViolation(reasons, "clampT", clampLimitTDeg, MinFilterLimit, MaxFilterLimit);
            return reasons.Length > 0 ? reasons.ToString() : null;
        }

        private static void AppendRangeViolation(
            StringBuilder reasons, string item, double value, double min, double max)
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

                        if (!SaveOutsideLock())
                        {
                            QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                                "Place 런타임 오프셋 지연 저장이 실패했습니다. 다음 갱신 또는 Cycle Stop flush에서 재시도합니다. - Failed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (DeferredSaveSync)
                    {
                        _deferredSaveWorkerRunning = false;
                    }

                    QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                        "Place 런타임 오프셋 지연 저장 워커가 실패했습니다. error=" + ex.Message + " - Failed");
                }
            });
        }

        /// <summary>대기 중인 지연 저장을 즉시 반영한다(종료/수동 확정 시 호출).</summary>
        public static void FlushPendingSave()
        {
            TryFlushPendingSave("FlushPendingSave");
        }

        /// <summary>
        /// 정상 Cycle Stop / 앱 종료 배리어용 동기 저장. 성공 여부를 반환한다.
        /// [내구성 워터마크 2026-08-18] "호출 시점의 자료가 디스크에 있는가"로 판정한다.
        /// 이미 확정돼 있으면 쓰지 않고, 동시 저장이 더 새 세대를 먼저 확정했어도 성공이다
        /// (그 스냅샷이 내 자료를 포함하므로). 경합을 기다리거나 재시도하지 않는다.
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
            QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                "Place 런타임 오프셋 동기 flush. reason=" + (reason ?? "-") +
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
        /// 디스크 확정 세대를 단조 증가로 게시한다. 어떤 락 문맥에서 불려도 안전하도록 Interlocked CAS를 쓴다
        /// (락을 쓰면 Sync ↔ IoSync 순서가 호출 경로마다 달라져 역전 위험이 생긴다).
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
                PlaceRuntimeOffsetDocument document;
                long capturedStateVersion;
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    // 문서와 세대를 같은 락 안에서 집어야 "이 문서가 담은 세대"가 정확히 확정된다.
                    capturedStateVersion = _stateVersion;
                    document = BuildDocumentLocked();
                }

                bool saved = PlaceRuntimeOffsetStore.Save(document);
                if (saved)
                    PublishDurableStateVersion(capturedStateVersion);
                return saved;
            }
        }

        private static PlaceRuntimeOffsetDocument BuildDocumentLocked()
        {
            var document = new PlaceRuntimeOffsetDocument();
            document.UsePlaceRuntimeOffset = _useCorrection;
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
                    document.Filters.Add(new PlaceRuntimeOffsetRow
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
    }
}
