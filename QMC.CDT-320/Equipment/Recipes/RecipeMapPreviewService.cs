using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using QMC.CDT320.DieMaps;
using QMC.Common.Data.Store;

namespace QMC.CDT320.Recipes
{
    /// <summary>등록 맵의 편집 대상과 공정 기준으로 표시한 다이를 연결한다.</summary>
    public sealed class RecipeMapPreview
    {
        private readonly Dictionary<DieMapEntry, DieMapEntry> _sourceEntries;
        private readonly HashSet<DieMapEntry> _filteredEntries;

        public DieMap Map { get; private set; }

        internal RecipeMapPreview(DieMap map, Dictionary<DieMapEntry, DieMapEntry> sourceEntries,
            HashSet<DieMapEntry> filteredEntries)
        {
            Map = map;
            _sourceEntries = sourceEntries;
            _filteredEntries = filteredEntries;
        }

        public DieMapEntry GetSource(DieMapEntry displayedEntry)
        {
            DieMapEntry source;
            if (displayedEntry == null || !_sourceEntries.TryGetValue(displayedEntry, out source))
                throw new InvalidDataException("선택한 다이가 현재 등록 맵 미리보기와 일치하지 않습니다. 맵을 다시 확인하세요.");
            return source;
        }

        public bool IsFilteredOut(DieMapEntry displayedEntry)
        {
            return displayedEntry != null && _filteredEntries.Contains(displayedEntry);
        }
    }

    /// <summary>등록 맵 전용 미리보기. 원본 형상/데이터와 런타임 Material은 변경하지 않는다.</summary>
    public static class RecipeMapPreviewService
    {
        public static RecipeMapPreview Create(DieMap source, WaferMapProcessSettings settings,
            PickupSubset pickup, RecipeMapKind kind, HashSet<int> inputBins)
        {
            if (source != null && source.ProcessTransform != null)
                throw new InvalidDataException("공정 변환 전의 등록 기준 맵이 필요합니다. 적용된 맵을 다시 회전하여 저장할 수 없습니다.");

            DieMap map = WaferMapProcessService.Prepare(source, settings, kind.ToString());
            var byAddress = new Dictionary<string, DieMapEntry>(StringComparer.Ordinal);
            foreach (DieMapEntry entry in source.Entries)
            {
                string key = GetOriginalAddress(entry);
                if (byAddress.ContainsKey(key))
                    throw new InvalidDataException("등록 맵에 중복된 원본 다이 주소가 있습니다: " + key);
                byAddress.Add(key, entry);
            }

            var sourceEntries = new Dictionary<DieMapEntry, DieMapEntry>();
            var filteredEntries = new HashSet<DieMapEntry>();
            foreach (DieMapEntry entry in map.Entries)
            {
                DieMapEntry original;
                if (!byAddress.TryGetValue(GetOriginalAddress(entry), out original) ||
                    !string.Equals(entry.DieUid, original.DieUid, StringComparison.Ordinal))
                    throw new InvalidDataException("등록 맵과 표시 다이의 원본 연결이 일치하지 않습니다.");
                sourceEntries.Add(entry, original);

                // FINAL APPLY의 역할 맵 저장 규칙과 동일하다. 원본 BIN/토큰은 Source 필드에 보존된다.
                if (entry.IsTarget && entry.BinCode <= 0) entry.BinCode = 1;
                if (kind == RecipeMapKind.Input && entry.IsTarget && inputBins != null &&
                    inputBins.Count > 0 && !inputBins.Contains(entry.BinCode))
                {
                    entry.IsTarget = false;
                    filteredEntries.Add(entry);
                }
            }
            PickupSequenceGenerator.ApplySequenceNumbers(map, pickup ?? new PickupSubset());
            return new RecipeMapPreview(map, sourceEntries, filteredEntries);
        }

        public static HashSet<int> LoadSavedInputBins(string recipeName)
        {
            InputStageRecipe recipe;
            string reason;
            if (!UnitDataStore.TryLoadRecipeRequired(recipeName, "InputStageUnit", out recipe, out reason))
                throw new InvalidDataException("등록 맵의 PICKUP BIN FILTER를 확인할 수 없습니다. " + reason);
            HashSet<int> bins;
            return recipe.DieMap != null && recipe.DieMap.TryGetPickupBinFilter(out bins) ? bins : null;
        }

        private static string GetOriginalAddress(DieMapEntry entry)
        {
            int x = entry.OriginalMapX >= 0 ? entry.OriginalMapX : entry.DieMapX;
            int y = entry.OriginalMapY >= 0 ? entry.OriginalMapY : entry.DieMapY;
            return x.ToString(CultureInfo.InvariantCulture) + "," + y.ToString(CultureInfo.InvariantCulture);
        }
    }
}
