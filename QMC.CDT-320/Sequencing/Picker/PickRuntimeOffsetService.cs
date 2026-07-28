using System;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Pick 런타임 오프셋 실시간 보정 서비스.
    /// Pickup → Bottom 비전 검사(콜렛에 물린 Die의 틀어짐 측정) → Y 전처리 → LowPassFilter →
    /// 다음 Pick 목표 반영으로 도는 폐루프 보정의 상태 보관소다.
    /// - 필터 단위: PickerSide(Front/Rear) × PickerNo(1~4) = 8세트, 각 X/Y/T 3채널 독립.
    /// - Y 채널은 서비스가 전처리한다: 피커Y편차 = 촬영시점 PickerY CommandPosition − 콜렛Cal Y(FinalPickerY),
    ///   전처리 YOffset = 촬영된 OffsetY − 피커Y편차. X/T는 측정값 그대로 입력.
    /// - 필터 상태는 전처리 후 측정 부호 그대로(raw) 저장하고, 부호 반전(전 채널 −)은
    ///   적용 지점(DieCoordinateTransformService.CalculatePickTarget)에서 수행한다.
    /// - Enable/Disable(UsePickRuntimeOffset): Disable이어도 필터 갱신(학습)·저장은 계속하며
    ///   적용만 중지한다 — Enable 판정은 적용 지점(PickerPickUpSequence)에서 GetOffset 사용 여부로 결정.
    /// - 발산 방지: 갱신 후 상태값을 X/Y ±0.50mm, T ±0.5°로 클램프하고 한계 도달 시 Warning을 1회 발생
    ///   (한계 미만 복귀 시 재무장하는 래치).
    /// - 검사 시퀀스 스레드(갱신)와 픽업 시퀀스 스레드(조회)가 다르므로 lock으로 보호한다.
    /// </summary>
    internal static class PickRuntimeOffsetService
    {
        // 이상치 거부 한계: 현재 필터 출력 대비 편차가 이 값 이상이면 해당 채널 샘플 폐기.
        private const double OutlierLimitXyMm = 3.0;
        private const double OutlierLimitTDeg = 0.5;
        // 발산 방지 클램프 한계 (필터 상태값 자체를 이 범위로 제한).
        private const double ClampLimitXyMm = 2.0;
        private const double ClampLimitTDeg = 0.5;

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
        /// Y 전처리(콜렛Cal 기준) → 채널별 이상치 검사 → 필터 갱신 → 클램프+워닝 → 저장 → 로그.
        /// 콜렛Cal 레코드가 없거나 Valid가 아니면 샘플 전체를 폐기한다.
        /// </summary>
        public static void OnBottomInspectionOffset(
            PickerSequenceSide side,
            int pickerNo,
            double offsetX,
            double rawOffsetY,
            double offsetT,
            double capturedPickerYCommand,
            double colletCalY,
            bool colletCalValid,
            string dieId)
        {
            try
            {
                if (!colletCalValid)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "PickRuntimeOffset",
                        "콜렛 캘리브레이션이 유효하지 않아 Pick 런타임 오프셋 샘플을 폐기했습니다. side=" + side +
                        ", pickerNo=" + pickerNo +
                        ", die=" + (dieId ?? string.Empty) +
                        ", capturedPickerYCommand=" + F(capturedPickerYCommand) + " - Check");
                    return;
                }

                // Y 전처리: 촬영 시점 PickerY 지령 위치와 콜렛Cal Y(FinalPickerY)의 편차를 제거해
                // "Die가 콜렛 기준으로 틀어진 양"만 학습한다.
                double pickerYDeviation = capturedPickerYCommand - colletCalY;
                double preprocessedY = rawOffsetY - pickerYDeviation;

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

                    acceptedX = AcceptChannelLocked(set.X, offsetX, OutlierLimitXyMm, "X", side, pickerNo, dieId);
                    acceptedY = AcceptChannelLocked(set.Y, preprocessedY, OutlierLimitXyMm, "Y", side, pickerNo, dieId);
                    acceptedT = AcceptChannelLocked(set.T, offsetT, OutlierLimitTDeg, "T", side, pickerNo, dieId);

                    if (acceptedX)
                        set.ClampLatchedX = ClampChannelLocked(set.X, ClampLimitXyMm, set.ClampLatchedX, "X", side, pickerNo);
                    if (acceptedY)
                        set.ClampLatchedY = ClampChannelLocked(set.Y, ClampLimitXyMm, set.ClampLatchedY, "Y", side, pickerNo);
                    if (acceptedT)
                        set.ClampLatchedT = ClampChannelLocked(set.T, ClampLimitTDeg, set.ClampLatchedT, "T", side, pickerNo);

                    if (acceptedX || acceptedY || acceptedT)
                    {
                        set.LastUpdated = DateTime.Now;
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
                    ", measuredX=" + F(offsetX) + "(accepted=" + acceptedX + ")" +
                    ", rawOffsetY=" + F(rawOffsetY) +
                    ", capturedPickerYCommand=" + F(capturedPickerYCommand) +
                    ", colletCalY=" + F(colletCalY) +
                    ", pickerYDeviation=" + F(pickerYDeviation) +
                    ", preprocessedY=" + F(preprocessedY) + "(accepted=" + acceptedY + ")" +
                    ", measuredT=" + F(offsetT) + "(accepted=" + acceptedT + ")" +
                    ", filteredX=" + F(filteredX) +
                    ", filteredY=" + F(filteredY) +
                    ", filteredT=" + F(filteredT) +
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
            string dieId)
        {
            double deviation = Math.Abs(input - filter.Value);
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

            filter.Update(input);
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
            bool pending;
            lock (DeferredSaveSync)
            {
                pending = _deferredSaveRequested;
                _deferredSaveRequested = false;
            }

            if (pending)
                SaveOutsideLock();
        }

        // 문서 구성만 Sync 락 안에서 하고, 디스크 쓰기는 락 밖에서 수행한다.
        private static void SaveOutsideLock()
        {
            PickRuntimeOffsetDocument document;
            lock (Sync)
            {
                document = BuildDocumentLocked();
            }

            PickRuntimeOffsetStore.Save(document);
        }

        private static void SaveLocked()
        {
            PickRuntimeOffsetStore.Save(BuildDocumentLocked());
        }

        private static PickRuntimeOffsetDocument BuildDocumentLocked()
        {
            var document = new PickRuntimeOffsetDocument();
            document.UsePickRuntimeOffset = _useCorrection;
            document.CutoffFrequency = _cutoffFrequency;
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
    }
}
