using System;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Picker Z 런타임 오프셋 실시간 보정 서비스 (Z 단채널).
    /// Side 검사 FrontSide 비전 0도(ch0) 촬영의 center_offset_mm를 LowPassFilter(EMA)로 누적해
    /// Pick Z·Place Z·Bottom 검사 Z·Side 검사 Z 4곳의 이동 목표에서 감산하는 폐루프 보정의 상태 보관소다.
    /// - 소스: FRONTSIDE raw의 ch0_side_item_center_offset_mm만 사용. RearSide·ch1(90도)은
    ///   사용하지 않는다(팀장님 확정 2026-08-14).
    /// - 필터 단위: PickerSide(Front/Rear) × PickerNo(1~4) = 8세트, Z 1채널.
    /// - 필터 상태는 비전 raw 부호 그대로 저장하고, 감산(−)은 적용 지점 4곳에서 수행한다
    ///   (사이클별 이동 목표에만 1회 — 레시피 공정값·AF 확정식에 스며들면 이중 적용이라 금지).
    /// - Enable/Disable(UsePickerZRuntimeOffset): Disable이어도 필터 갱신(학습)·저장은 계속하며
    ///   적용만 중지한다. 기본 OFF — 실장비 방향 검증(§5-3) 후 팀장님이 ON.
    /// - 발산 방지: 갱신 후 상태값을 ±0.3mm(기본)로 클램프하고 한계 도달 시 Warning 1회(래치).
    ///   Z는 충돌 리스크가 있어 XY(±0.5)보다 타이트하다.
    /// - 검사 시퀀스 스레드(갱신)와 픽업/플레이스 시퀀스 스레드(조회)가 다르므로 lock으로 보호한다.
    /// </summary>
    internal static class PickerZRuntimeOffsetService
    {
        // 필터 설정 입력 허용 범위(SetFilterSettings·설정 다이얼로그 공통 — XY 서비스와 동일 범위).
        public const double MinCutoffFrequency = 0.001;
        public const double MaxCutoffFrequency = 1.0;
        public const double MinFilterLimit = 0.01;
        public const double MaxFilterLimit = 5.0;

        // 필터 한계 설정값(스토어 로드, SetFilterSettings로 변경) — 판정 알고리즘은 XY 서비스와 동일.
        private static double _outlierLimitMm = PickerZRuntimeOffsetDocument.DefaultOutlierLimitMm;
        private static double _clampLimitMm = PickerZRuntimeOffsetDocument.DefaultClampLimitMm;

        private static readonly object Sync = new object();
        private static bool _loaded;
        private static bool _useCorrection;
        private static double _cutoffFrequency = 0.1;
        private static FilterSet[] _filters;

        // 다이당 실행되는 핫패스 — 디스크 쓰기는 락 밖 병합 저장(Pick/Place 서비스와 동일 방식).
        private const int DeferredSaveQuietMs = 1000;
        private static readonly object DeferredSaveSync = new object();
        // [종료 저장 2026-08-17] 지연 저장 워커와 동기 flush가 같은 파일에 동시에 쓰지 않도록 IO를 직렬화한다
        // (Place 서비스와 동일 규약 — 동시 쓰기는 파일 손상 = 학습값 전체 소실로 이어진다).
        private static readonly object DeferredSaveIoSync = new object();
        private static bool _deferredSaveRequested;
        private static bool _deferredSaveWorkerRunning;

        private sealed class FilterSet
        {
            public FilterSet(double cutoffFrequency)
            {
                Z = new LowPassFilter(cutoffFrequency);
            }

            public LowPassFilter Z { get; private set; }
            public DateTime LastUpdated { get; set; }
            // 클램프 워닝 래치. 한계 도달 시 true, 한계 미만 복귀 시 false로 재무장.
            public bool ClampLatchedZ { get; set; }
        }

        /// <summary>PickerZ 런타임 보정 적용 여부 (UsePickerZRuntimeOffset 설정값).</summary>
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

                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                    "PickerZ 런타임 오프셋 사용 설정을 변경했습니다. enabled=" + enabled + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                    "PickerZ 런타임 오프셋 사용 설정 변경 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>현재 필터 설정(fc + 이상치/클램프 한계)을 반환한다.</summary>
        public static void GetFilterSettings(
            out double cutoffFrequency,
            out double outlierLimitMm,
            out double clampLimitMm)
        {
            lock (Sync)
            {
                EnsureLoadedLocked();
                cutoffFrequency = _cutoffFrequency;
                outlierLimitMm = _outlierLimitMm;
                clampLimitMm = _clampLimitMm;
            }
        }

        /// <summary>
        /// 필터 설정(fc + 한계 2종)을 변경하고 즉시 저장한다. UI 저장은 반드시 이 API를 경유할 것 —
        /// json은 런 중 지연 저장으로 덮어써지므로 파일을 직접 쓰면 유실된다.
        /// fc 변경은 8세트 필터의 alpha만 재계산하고 학습 상태를 유지한다.
        /// 클램프 한계 축소로 현재 상태값이 범위를 벗어나면 즉시 재클램프한다(래치 무조작, 로그만).
        /// 범위 밖 입력은 저장하지 않고 false를 반환한다(다이얼로그 검증과 이중 방어).
        /// </summary>
        public static bool SetFilterSettings(
            double cutoffFrequency,
            double outlierLimitMm,
            double clampLimitMm)
        {
            try
            {
                string rejectReason = BuildFilterSettingsRejectReason(cutoffFrequency, outlierLimitMm, clampLimitMm);
                if (rejectReason != null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                        "PickerZ 런타임 필터 설정 입력이 허용 범위를 벗어나 거부했습니다. " + rejectReason + " - Failed");
                    return false;
                }

                double oldFc;
                double oldOutlier;
                double oldClamp;
                var reclampLines = new System.Collections.Generic.List<string>();

                lock (Sync)
                {
                    EnsureLoadedLocked();
                    oldFc = _cutoffFrequency;
                    oldOutlier = _outlierLimitMm;
                    oldClamp = _clampLimitMm;

                    _cutoffFrequency = cutoffFrequency;
                    _outlierLimitMm = outlierLimitMm;
                    _clampLimitMm = clampLimitMm;

                    for (int i = 0; i < _filters.Length; i++)
                    {
                        FilterSet set = _filters[i];
                        set.Z.SetCutoffFrequency(_cutoffFrequency);

                        PickerSequenceSide side = i < 4 ? PickerSequenceSide.Front : PickerSequenceSide.Rear;
                        int pickerNo = (i % 4) + 1;
                        ReclampChannelLocked(set.Z, _clampLimitMm, side, pickerNo, reclampLines);
                    }

                    SaveLocked();
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                    "PickerZ 런타임 필터 설정을 변경했습니다." +
                    " fc=" + F(oldFc) + "→" + F(cutoffFrequency) +
                    "(alpha=" + F(LowPassFilter.CalculateAlpha(oldFc)) + "→" + F(LowPassFilter.CalculateAlpha(cutoffFrequency)) + ")" +
                    ", outlierZ=" + F(oldOutlier) + "→" + F(outlierLimitMm) +
                    ", clampZ=" + F(oldClamp) + "→" + F(clampLimitMm) + " - Ok");

                foreach (string line in reclampLines)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                        "PickerZ 런타임 오프셋을 축소된 클램프 한계로 재클램프했습니다. " + line + " - Check");
                }

                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                    "PickerZ 런타임 필터 설정 변경 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 현재 필터 상태(클램프 반영된 raw 값)를 반환한다. 미초기화/범위 밖 인자면 0을 반환한다.
        /// Enable 여부와 무관하게 상태를 반환하며, Enable 판정은 적용 지점에서 한다.
        /// </summary>
        public static void GetOffset(PickerSequenceSide side, int pickerNo, out double z)
        {
            z = 0.0;

            try
            {
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    FilterSet set = ResolveSetLocked(side, pickerNo);
                    if (set == null)
                        return;

                    z = set.Z.Value;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                    "PickerZ 런타임 오프셋 조회 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>
        /// Side 검사 FrontSide ch0 center_offset 1샘플을 반영한다 (자격 게이트 통과 샘플만 호출할 것).
        /// 이상치 검사 → 필터 갱신 → 클램프+워닝 → 지연 저장 → 로그.
        /// </summary>
        public static void OnSideInspectionOffset(
            PickerSequenceSide side,
            int pickerNo,
            double measuredZ,
            string dieId)
        {
            try
            {
                double filteredZ;
                bool acceptedZ;

                lock (Sync)
                {
                    EnsureLoadedLocked();
                    FilterSet set = ResolveSetLocked(side, pickerNo);
                    if (set == null)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                            "PickerZ 런타임 오프셋 갱신 대상이 유효하지 않습니다. side=" + side +
                            ", pickerNo=" + pickerNo + ", die=" + (dieId ?? string.Empty) + " - Failed");
                        return;
                    }

                    acceptedZ = AcceptChannelLocked(set.Z, measuredZ, _outlierLimitMm, side, pickerNo, dieId);
                    if (acceptedZ)
                    {
                        set.ClampLatchedZ = ClampChannelLocked(set.Z, _clampLimitMm, set.ClampLatchedZ, side, pickerNo);
                        set.LastUpdated = DateTime.Now;
                        RequestDeferredSave();
                    }

                    filteredZ = set.Z.Value;
                }

                EventLogger.Write(
                    EventKind.Event,
                    "COORD",
                    "PICKERZ-RUNTIME-OFFSET",
                    "PickerZ 런타임 오프셋 필터 갱신. side=" + side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + (dieId ?? string.Empty) +
                    ", measuredZ=" + F(measuredZ) + "(accepted=" + acceptedZ + ")" +
                    ", filteredZ=" + F(filteredZ) +
                    ", enabled=" + _useCorrection);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                    "PickerZ 런타임 오프셋 갱신 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>8세트 전체 Z 필터 현재값 스냅샷을 반환한다(모니터 표시 전용 — 기구 이관 대상 아님).</summary>
        public static PickerZRuntimeOffsetSnapshot[] GetSnapshot()
        {
            try
            {
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    var rows = new PickerZRuntimeOffsetSnapshot[_filters.Length];
                    for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                    {
                        PickerSequenceSide side = sideIndex == 0
                            ? PickerSequenceSide.Front
                            : PickerSequenceSide.Rear;
                        for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                        {
                            int index = sideIndex * 4 + (pickerNo - 1);
                            FilterSet set = _filters[index];
                            rows[index] = new PickerZRuntimeOffsetSnapshot(
                                side, pickerNo, set.Z.Value, set.LastUpdated);
                        }
                    }

                    return rows;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                    "PickerZ 런타임 오프셋 스냅샷 조회 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return new PickerZRuntimeOffsetSnapshot[0];
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

                    set.Z.Reset(0.0);
                    set.ClampLatchedZ = false;
                    set.LastUpdated = DateTime.Now;
                    SaveLocked();
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                    "PickerZ 런타임 오프셋을 초기화했습니다. side=" + side + ", pickerNo=" + pickerNo + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                    "PickerZ 런타임 오프셋 초기화 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
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
                        _filters[i].Z.Reset(0.0);
                        _filters[i].ClampLatchedZ = false;
                        _filters[i].LastUpdated = DateTime.Now;
                    }

                    SaveLocked();
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                    "PickerZ 런타임 오프셋 전체를 초기화했습니다. - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                    "PickerZ 런타임 오프셋 전체 초기화 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
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

        /// <summary>대기 중인 지연 저장을 즉시 반영한다(종료/수동 확정 시 호출).</summary>
        public static void FlushPendingSave()
        {
            TryFlushPendingSave("FlushPendingSave");
        }

        /// <summary>
        /// 앱 종료/정지 배리어용 동기 저장. 지연 저장 워커와 파일 IO를 직렬화하고 성공 여부를 반환한다.
        /// [종료 저장 2026-08-17] 대기 중인 저장이 없어도 무조건 1회 저장한다 — 지연 저장은 1000ms 무음
        /// 후에만 기록하므로, "pending일 때만 저장"으로 두면 종료가 그 창에 걸릴 때 최신 학습분이 유실된다.
        /// </summary>
        public static bool TryFlushPendingSave(string reason)
        {
            lock (DeferredSaveSync)
            {
                _deferredSaveRequested = false;
            }

            bool saved = SaveOutsideLock();
            QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                "PickerZ 런타임 오프셋 동기 flush. reason=" + (reason ?? "-") +
                " - " + (saved ? "Ok" : "Failed"));
            return saved;
        }

        // ── 내부 구현 (Sync lock 안에서만 호출) ─────────────────────

        private static void EnsureLoadedLocked()
        {
            if (_loaded)
                return;

            PickerZRuntimeOffsetDocument document = PickerZRuntimeOffsetStore.Load();
            _useCorrection = document.UsePickerZRuntimeOffset;
            _cutoffFrequency = document.CutoffFrequency > 0.0 ? document.CutoffFrequency : 0.1;
            _outlierLimitMm = document.OutlierLimitMm;
            _clampLimitMm = document.ClampLimitMm;
            _filters = new FilterSet[8];
            for (int i = 0; i < _filters.Length; i++)
                _filters[i] = new FilterSet(_cutoffFrequency);

            if (document.Filters != null)
            {
                foreach (PickerZRuntimeOffsetRow row in document.Filters)
                {
                    if (row == null)
                        continue;

                    PickerSequenceSide side;
                    if (!Enum.TryParse(row.Side, true, out side))
                        continue;

                    FilterSet set = ResolveSetLocked(side, row.PickerNo);
                    if (set == null)
                        continue;

                    set.Z.Reset(row.FilteredZ);
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
            PickerSequenceSide side,
            int pickerNo,
            string dieId)
        {
            double deviation = Math.Abs(input - filter.Value);
            if (deviation >= outlierLimit)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                    "PickerZ 런타임 오프셋 이상치 샘플을 폐기했습니다. side=" + side +
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
        /// 한계 미만이면 래치를 해제한다. 반환값은 갱신된 래치 상태. (Place 서비스와 동일 정책)
        /// </summary>
        private static bool ClampChannelLocked(
            LowPassFilter filter,
            double limit,
            bool latched,
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
                    "PickerZ 런타임 오프셋이 발산 방지 한계에 도달해 클램프되었습니다. side=" + side +
                    ", pickerNo=" + pickerNo +
                    ", valueBeforeClamp=" + F(value) +
                    ", limit=" + F(limit);
                AlarmManager.Raise(AlarmSeverity.Warning, "PICKERZ-RUNTIME-OFFSET-CLAMP", "PickerZRuntimeOffset", message);
                EventLogger.Write(EventKind.Warning, "COORD", "PICKERZ-RUNTIME-OFFSET-CLAMP", message);
            }

            return true;
        }

        /// <summary>설정 입력 검증. 위반을 전부 담은 사유를 반환한다(정상이면 null).</summary>
        private static string BuildFilterSettingsRejectReason(
            double cutoffFrequency,
            double outlierLimitMm,
            double clampLimitMm)
        {
            var reasons = new System.Text.StringBuilder();
            AppendRangeViolation(reasons, "fc", cutoffFrequency, MinCutoffFrequency, MaxCutoffFrequency);
            AppendRangeViolation(reasons, "outlierZ", outlierLimitMm, MinFilterLimit, MaxFilterLimit);
            AppendRangeViolation(reasons, "clampZ", clampLimitMm, MinFilterLimit, MaxFilterLimit);
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
            System.Collections.Generic.List<string> reclampLines)
        {
            double value = filter.Value;
            if (Math.Abs(value) <= limit)
                return;

            double clamped = value > 0.0 ? limit : -limit;
            filter.Reset(clamped);
            reclampLines.Add(
                "side=" + side + ", pickerNo=" + pickerNo +
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

                    QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffset",
                        "PickerZ 런타임 오프셋 지연 저장 워커가 실패했습니다. error=" + ex.Message + " - Failed");
                }
            });
        }

        // 문서 구성만 Sync 락 안에서 하고, 디스크 쓰기는 락 밖에서 수행한다.
        private static bool SaveOutsideLock()
        {
            lock (DeferredSaveIoSync)
            {
                PickerZRuntimeOffsetDocument document;
                lock (Sync)
                {
                    EnsureLoadedLocked();
                    document = BuildDocumentLocked();
                }

                return PickerZRuntimeOffsetStore.Save(document);
            }
        }

        private static void SaveLocked()
        {
            PickerZRuntimeOffsetStore.Save(BuildDocumentLocked());
        }

        private static PickerZRuntimeOffsetDocument BuildDocumentLocked()
        {
            var document = new PickerZRuntimeOffsetDocument();
            document.UsePickerZRuntimeOffset = _useCorrection;
            document.CutoffFrequency = _cutoffFrequency;
            document.OutlierLimitMm = _outlierLimitMm;
            document.ClampLimitMm = _clampLimitMm;
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                PickerSequenceSide side = sideIndex == 0 ? PickerSequenceSide.Front : PickerSequenceSide.Rear;
                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    FilterSet set = _filters[sideIndex * 4 + (pickerNo - 1)];
                    document.Filters.Add(new PickerZRuntimeOffsetRow
                    {
                        Side = side.ToString(),
                        PickerNo = pickerNo,
                        FilteredZ = set.Z.Value,
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
