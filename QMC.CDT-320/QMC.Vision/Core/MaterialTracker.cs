using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 다이 단위 검사 결과 누적. 310 의 <c>MaterialDataManager.InspectionDatas</c> 와 동일 역할.
    /// 모든 필드는 string 기반 (CSV 직렬화/공란 허용).
    /// </summary>
    [Serializable]
    public class DieRecord
    {
        public string ChipUid                    { get; set; } = "";
        public DateTime UpdatedAt                { get; set; } = DateTime.Now;

        // 로딩/언로딩 기판
        public string LoadingSubstrateId         { get; set; } = "";
        public string LoadingSubstrateX          { get; set; } = "";
        public string LoadingSubstrateY          { get; set; } = "";
        public string UnloadingSubstrateId       { get; set; } = "";
        public string UnloadingSubstrateX        { get; set; } = "";
        public string UnloadingSubstrateY        { get; set; } = "";

        // 다이 크기
        public string DieWidth                   { get; set; } = "";
        public string DieHeight                  { get; set; } = "";
        public string ChipLowerSpecLimitWidth    { get; set; } = "";
        public string ChipUpperSpecLimitWidth    { get; set; } = "";
        public string ChipLowerSpecLimitHeight   { get; set; } = "";
        public string ChipUpperSpecLimitHeight   { get; set; } = "";

        // Bottom (후면) chipping
        public string BackChippingTopSize        { get; set; } = "";
        public string BackChippingRightSize      { get; set; } = "";
        public string BackChippingBottomSize     { get; set; } = "";
        public string BackChippingLeftSize       { get; set; } = "";
        public string BackChippingLength         { get; set; } = "";

        // Side chipping (4 surface)
        public string SideChippingTopSize        { get; set; } = "";
        public string SideChippingRightSize      { get; set; } = "";
        public string SideChippingBottomSize     { get; set; } = "";
        public string SideChippingLeftSize       { get; set; } = "";
        public string SideChippingLength         { get; set; } = "";

        // 이물
        public string BackForeignSize            { get; set; } = "";
        public string ForeignObjectSize          { get; set; } = "";

        // PLACE 후 갭
        public string PlaceTopGapAverage         { get; set; } = "";
        public string PlaceBottomGapAverage      { get; set; } = "";
        public string PlaceLeftGapAverage        { get; set; } = "";
        public string PlaceRightGapAverage       { get; set; } = "";
        public string DieGapUpperLimit           { get; set; } = "";
        public string DieGapLowerLimit           { get; set; } = "";
    }

    /// <summary>
    /// chipUid 키 기반 다이 레코드 추적기. ConcurrentDictionary.
    /// </summary>
    public static class MaterialTracker
    {
        private static readonly ConcurrentDictionary<string, DieRecord> _records
            = new ConcurrentDictionary<string, DieRecord>(StringComparer.Ordinal);

        public static IReadOnlyDictionary<string, DieRecord> Records => _records;

        public static int Count => _records.Count;

        /// <summary>해당 uid 의 record 를 가져오거나 새로 생성하여 mutate.</summary>
        public static DieRecord Update(string chipUid, Action<DieRecord> mutate)
        {
            if (string.IsNullOrEmpty(chipUid)) return null;
            var rec = _records.GetOrAdd(chipUid, k => new DieRecord { ChipUid = k });
            try { mutate?.Invoke(rec); } catch { }
            rec.UpdatedAt = DateTime.Now;
            return rec;
        }

        public static DieRecord Get(string chipUid)
        {
            if (string.IsNullOrEmpty(chipUid)) return null;
            _records.TryGetValue(chipUid, out var r);
            return r;
        }

        public static bool Remove(string chipUid)
            => _records.TryRemove(chipUid, out _);

        public static void Clear() => _records.Clear();

        /// <summary>InspectionResult.Items 리스트를 받아 BottomInspection 분류로 누적.</summary>
        public static void ApplyBottom(string chipUid, InspectionResult r)
        {
            if (r == null || string.IsNullOrEmpty(chipUid)) return;
            Update(chipUid, rec =>
            {
                foreach (var it in r.Items)
                {
                    switch (it.Name)
                    {
                        case "Width":                rec.DieWidth = it.Value; break;
                        case "Height":               rec.DieHeight = it.Value; break;
                        case "Chipping Top Size":    rec.BackChippingTopSize    = it.Value; break;
                        case "Chipping Right Size":  rec.BackChippingRightSize  = it.Value; break;
                        case "Chipping Bottom Size": rec.BackChippingBottomSize = it.Value; break;
                        case "Chipping Left Size":   rec.BackChippingLeftSize   = it.Value; break;
                        case "Chipping Length":      rec.BackChippingLength     = it.Value; break;
                        case "Foreign Size":         rec.BackForeignSize        = it.Value; break;
                        case "Foreign Object Size":  rec.ForeignObjectSize      = it.Value; break;
                    }
                }
            });
        }

        /// <summary>SideInspection 결과를 surface 별로 매핑하여 누적.</summary>
        public static void ApplySide(string chipUid, InspectionResult r, string surface)
        {
            if (r == null || string.IsNullOrEmpty(chipUid)) return;
            Update(chipUid, rec =>
            {
                foreach (var it in r.Items)
                {
                    if (it.Name == "Max Chipping Depth")
                    {
                        switch (surface)
                        {
                            case "FrontWidth":  rec.SideChippingBottomSize = it.Value; break;
                            case "BackWidth":   rec.SideChippingTopSize    = it.Value; break;
                            case "FrontHeight": rec.SideChippingLeftSize   = it.Value; break;
                            case "BackHeight":  rec.SideChippingRightSize  = it.Value; break;
                        }
                    }
                    else if (it.Name == "Chipping Length")
                    {
                        rec.SideChippingLength = it.Value;
                    }
                }
            });
        }

        /// <summary>DieGap Inspection 결과 누적.</summary>
        public static void ApplyDieGap(string chipUid, InspectionResult r,
                                       string upperLimit = "", string lowerLimit = "")
        {
            if (r == null || string.IsNullOrEmpty(chipUid)) return;
            Update(chipUid, rec =>
            {
                foreach (var it in r.Items)
                {
                    switch (it.Name)
                    {
                        case "Top Gap Avg":    rec.PlaceTopGapAverage    = it.Value; break;
                        case "Bottom Gap Avg": rec.PlaceBottomGapAverage = it.Value; break;
                        case "Left Gap Avg":   rec.PlaceLeftGapAverage   = it.Value; break;
                        case "Right Gap Avg":  rec.PlaceRightGapAverage  = it.Value; break;
                    }
                }
                if (!string.IsNullOrEmpty(upperLimit)) rec.DieGapUpperLimit = upperLimit;
                if (!string.IsNullOrEmpty(lowerLimit)) rec.DieGapLowerLimit = lowerLimit;
            });
        }
    }
}
