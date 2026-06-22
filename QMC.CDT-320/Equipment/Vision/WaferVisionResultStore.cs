using System;
using System.Collections.Generic;

namespace QMC.CDT_320.Equipment.Vision
{
    /// <summary>
    /// Wafer 비전 정렬/다이검사 결과의 휘발성 최종값 저장소(SSOT).
    /// <para>
    /// <see cref="QMC.CDT320.VisionComm.WaferVisionAdapter"/>가 TriggerAlignAsync /
    /// GetResultAsync 호출 시 결과를 기록한다. 따라서 작업정보의 수동 테스트와
    /// InputStageAlignSequence(시퀀서)가 <b>동일한 어댑터 경로</b>를 거쳐 같은 데이터를 채운다.
    /// </para>
    /// </summary>
    public static class WaferVisionResultStore
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, WaferAlignSample> _aligns =
            new Dictionary<string, WaferAlignSample>(StringComparer.OrdinalIgnoreCase);
        private static WaferVisionInspectionResult _lastInspection = new WaferVisionInspectionResult();
        private static bool _lastDieCheckOk;
        private static DateTime _lastDieCheckTime;

        /// <summary>저장 값이 갱신될 때 발생(UI 갱신용).</summary>
        public static event Action Changed;

        /// <summary>정렬 타깃(Center/Ref1/Ref2 등)별 비전 정렬 결과를 기록한다.</summary>
        public static void RecordAlign(string targetId, QMC.CDT320.VisionAlignResult result)
        {
            try
            {
                if (result == null)
                    return;

                string key = string.IsNullOrWhiteSpace(targetId) ? "Center" : targetId.Trim();
                lock (_lock)
                {
                    _aligns[key] = new WaferAlignSample
                    {
                        TargetId   = key,
                        DeltaX     = result.DeltaX,
                        DeltaY     = result.DeltaY,
                        DeltaTheta = result.DeltaTheta,
                        PitchX     = result.PitchX,
                        PitchY     = result.PitchY,
                        SampledAt  = DateTime.Now
                    };
                    // 마지막 정렬 결과를 깊은 검사 모델로도 보관(시퀀서/리포트 소비용).
                    _lastInspection = new WaferVisionInspectionResult
                    {
                        DieOffsetX     = result.DeltaX,
                        DieOffsetY     = result.DeltaY,
                        DieRotation    = result.DeltaTheta,
                        RecipeName     = _lastInspection != null ? _lastInspection.RecipeName : "",
                        InspectionTime = DateTime.Now
                    };
                }
                RaiseChanged();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "WaferVisionResultStore",
                    "RecordAlign failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>다이 OK/NG 검사 결과를 기록한다.</summary>
        public static void RecordDieCheck(bool ok)
        {
            try
            {
                lock (_lock)
                {
                    _lastDieCheckOk = ok;
                    _lastDieCheckTime = DateTime.Now;
                }
                RaiseChanged();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "WaferVisionResultStore",
                    "RecordDieCheck failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>타깃별 마지막 정렬 결과. 없으면 null.</summary>
        public static WaferAlignSample GetAlign(string targetId)
        {
            string key = string.IsNullOrWhiteSpace(targetId) ? "Center" : targetId.Trim();
            lock (_lock)
            {
                WaferAlignSample sample;
                return _aligns.TryGetValue(key, out sample) ? sample : null;
            }
        }

        /// <summary>마지막 정렬을 반영한 깊은 검사 모델(복사본 아님).</summary>
        public static WaferVisionInspectionResult LastInspection
        {
            get { lock (_lock) { return _lastInspection; } }
        }

        public static bool LastDieCheckOk
        {
            get { lock (_lock) { return _lastDieCheckOk; } }
        }

        public static DateTime LastDieCheckTime
        {
            get { lock (_lock) { return _lastDieCheckTime; } }
        }

        /// <summary>저장 값을 모두 초기화한다.</summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _aligns.Clear();
                _lastInspection = new WaferVisionInspectionResult();
                _lastDieCheckOk = false;
                _lastDieCheckTime = DateTime.MinValue;
            }
            RaiseChanged();
        }

        private static void RaiseChanged()
        {
            try { Changed?.Invoke(); } catch { }
        }
    }

    /// <summary>정렬 타깃 1개에 대한 비전 정렬 결과 1샘플.</summary>
    public class WaferAlignSample
    {
        public string TargetId { get; set; } = "";
        public double DeltaX { get; set; }
        public double DeltaY { get; set; }
        public double DeltaTheta { get; set; }
        public double PitchX { get; set; }
        public double PitchY { get; set; }
        public DateTime SampledAt { get; set; }
    }
}
