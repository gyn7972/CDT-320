using System;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Place 런타임 오프셋 실시간 보정 서비스.
    /// Place → Bin 비전 후검사 결과(X/Y/T 오프셋)를 LowPassFilter(EMA)로 누적해
    /// 다음 Place 목표 좌표 계산에 반영하는 폐루프 보정의 상태 보관소다.
    /// - 필터 단위: PickerSide(Front/Rear) × PickerNo(1~4) = 8세트, 각 X/Y/T 3채널 독립.
    /// - 필터 상태는 비전 측정 부호 그대로(raw) 저장하고, 부호 변환(X:−, Y:+, T:−)은
    ///   적용 지점(DieCoordinateTransformService.CalculatePlaceTarget)에서 수행한다.
    /// - 검사 큐 스레드에서 갱신, 시퀀스 스레드에서 조회하므로 lock으로 보호한다.
    /// </summary>
    internal static class PlaceRuntimeOffsetService
    {
        // 이상치 거부 한계: 현재 필터 출력 대비 편차가 이 값 이상이면 해당 채널 샘플 폐기.
        private const double OutlierLimitXyMm = 1.0;
        private const double OutlierLimitTDeg = 0.5;

        private static readonly object Sync = new object();
        private static bool _loaded;
        private static double _cutoffFrequency = 0.1;
        private static FilterSet[] _filters;

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
        }

        /// <summary>
        /// 현재 필터 출력(raw)을 반환한다. 미초기화/범위 밖 인자면 0을 반환한다.
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
            string dieId)
        {
            try
            {
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
                        QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffset",
                            "Place 런타임 오프셋 갱신 대상이 유효하지 않습니다. side=" + side +
                            ", pickerNo=" + pickerNo + ", die=" + (dieId ?? string.Empty) + " - Failed");
                        return;
                    }

                    acceptedX = AcceptChannelLocked(set.X, measuredX, OutlierLimitXyMm, "X", side, pickerNo, dieId);
                    acceptedY = AcceptChannelLocked(set.Y, measuredY, OutlierLimitXyMm, "Y", side, pickerNo, dieId);
                    acceptedT = AcceptChannelLocked(set.T, measuredT, OutlierLimitTDeg, "T", side, pickerNo, dieId);

                    if (acceptedX || acceptedY || acceptedT)
                    {
                        set.LastUpdated = DateTime.Now;
                        SaveLocked();
                    }

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
                    ", filteredT=" + F(filteredT));
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
                    set.LastUpdated = DateTime.Now;
                    SaveLocked();
                }

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
                        _filters[i].X.Reset(0.0);
                        _filters[i].Y.Reset(0.0);
                        _filters[i].T.Reset(0.0);
                        _filters[i].LastUpdated = DateTime.Now;
                    }

                    SaveLocked();
                }

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

            PlaceRuntimeOffsetDocument document = PlaceRuntimeOffsetStore.Load();
            _cutoffFrequency = document.CutoffFrequency > 0.0 ? document.CutoffFrequency : 0.1;
            _filters = new FilterSet[8];
            for (int i = 0; i < _filters.Length; i++)
                _filters[i] = new FilterSet(_cutoffFrequency);

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
            if (pickerNo < 1 || pickerNo > 4)
                return null;

            int sideIndex = side == PickerSequenceSide.Front ? 0 : 1;
            return _filters[sideIndex * 4 + (pickerNo - 1)];
        }

        private static bool AcceptChannelLocked(
            LowPassFilter filter,
            double measured,
            double outlierLimit,
            string channel,
            PickerSequenceSide side,
            int pickerNo,
            string dieId)
        {
            double deviation = Math.Abs(measured - filter.Value);
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

            filter.Update(measured);
            return true;
        }

        private static void SaveLocked()
        {
            var document = new PlaceRuntimeOffsetDocument();
            document.CutoffFrequency = _cutoffFrequency;
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

            PlaceRuntimeOffsetStore.Save(document);
        }

        private static string F(double value)
        {
            return value.ToString("F6");
        }
    }
}
