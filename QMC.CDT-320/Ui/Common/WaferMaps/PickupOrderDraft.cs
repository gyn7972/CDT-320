using System;
using System.Collections.Generic;
using System.Linq;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Common.WaferMaps
{
    /// <summary>장비/저장소를 참조하지 않는 픽업 순서 편집 사본. Apply 전에는 원본을 변경하지 않는다.</summary>
    public sealed class PickupOrderDraft
    {
        private readonly List<DieMapEntry> _order = new List<DieMapEntry>();
        private PickupSubset _options;
        private string _startDieUid;

        public DieMap Map { get; private set; }
        public IReadOnlyList<DieMapEntry> Order { get { return _order.AsReadOnly(); } }
        public PickupSubset Options { get { return CopyOptions(_options); } }
        public string StartDieUid { get { return _startDieUid ?? string.Empty; } }
        public bool IsReadOnly { get; private set; }
        public int WrapAfterIndex { get; private set; } = -1;

        public PickupOrderDraft(DieMap map, PickupSubset options, string startDieUid,
            IEnumerable<string> orderedIds, bool readOnly)
        {
            Map = CopyMap(map);
            _options = CopyOptions(options);
            _startDieUid = startDieUid ?? string.Empty;
            IsReadOnly = readOnly;
            List<DieMapEntry> order;
            string reason;
            if (!TryResolveOrder(Map, orderedIds, out order, out reason))
                throw new ArgumentException(reason, "orderedIds");
            _order.AddRange(order);
            if (!string.IsNullOrWhiteSpace(_startDieUid) &&
                (_order.Count == 0 || !SameUid(_order[0].DieUid, _startDieUid)))
                throw new ArgumentException("시작 Die가 픽업 순서의 첫 Die와 다릅니다.");
            UpdateWrapIndex();
        }

        public void SetOptions(PickupSubset options)
        {
            EnsureEditable();
            _options = CopyOptions(options);
            _startDieUid = string.Empty;
            Rebuild();
        }

        public void SetStartDie(string dieUid)
        {
            EnsureEditable();
            if (!string.IsNullOrEmpty(dieUid) && !_order.Any(e => SameUid(e.DieUid, dieUid)))
                throw new ArgumentException("현재 픽업 대상이 아닌 Die는 시작점으로 지정할 수 없습니다.");
            _startDieUid = dieUid ?? string.Empty;
            Rebuild();
        }

        public static PickupSubset CopyOptions(PickupSubset source)
        {
            source = source ?? new PickupSubset();
            return new PickupSubset { StartCorner = source.StartCorner, Direction = source.Direction, Pattern = source.Pattern };
        }

        public static bool IsPickable(DieMapEntry entry)
        {
            return entry != null && entry.IsTarget && entry.Result != DieResult.Good && entry.Result != DieResult.NG;
        }

        public static List<DieMapEntry> BuildBaseOrder(DieMap map, PickupSubset options)
        {
            return PickupSequenceGenerator.Build(map, options).Where(IsPickable).ToList();
        }

        public static List<DieMapEntry> RotateAtStart(List<DieMapEntry> source, string startDieUid)
        {
            int index = !string.IsNullOrWhiteSpace(startDieUid)
                ? source.FindIndex(e => SameUid(e.DieUid, startDieUid)) : -1;
            return index > 0 ? source.Skip(index).Concat(source.Take(index)).ToList() : new List<DieMapEntry>(source);
        }

        public static bool TryResolveOrder(DieMap map, IEnumerable<string> orderedIds,
            out List<DieMapEntry> order, out string reason)
        {
            order = new List<DieMapEntry>();
            reason = string.Empty;
            if (map == null || map.Entries == null || orderedIds == null)
            {
                reason = "픽업 순서 또는 Die Map이 없습니다.";
                return false;
            }
            var byId = new Dictionary<string, DieMapEntry>(StringComparer.OrdinalIgnoreCase);
            var cells = new HashSet<string>(StringComparer.Ordinal);
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid) || byId.ContainsKey(entry.DieUid) ||
                    entry.DieMapX < 0 || entry.DieMapX >= map.DieMapX || entry.DieMapY < 0 || entry.DieMapY >= map.DieMapY ||
                    !cells.Add(entry.DieMapX + "," + entry.DieMapY))
                {
                    reason = "Die Map에 비어 있거나 중복된 UID/격자 또는 범위를 벗어난 격자가 있습니다.";
                    return false;
                }
                byId.Add(entry.DieUid, entry);
            }
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string uid in orderedIds)
            {
                DieMapEntry entry;
                if (string.IsNullOrWhiteSpace(uid) || !used.Add(uid) || !byId.TryGetValue(uid, out entry) || !IsPickable(entry))
                {
                    reason = "픽업 순서에 중복/누락 UID 또는 WAIT 대상이 아닌 Die가 있습니다.";
                    return false;
                }
                order.Add(entry);
            }
            if (order.Count != map.Entries.Count(IsPickable))
            {
                reason = "픽업 순서와 전체 WAIT 대상 수가 일치하지 않습니다.";
                return false;
            }
            return true;
        }

        private void EnsureEditable()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("읽기 전용 화면에서는 픽업 순서를 변경할 수 없습니다.");
        }

        private void Rebuild()
        {
            List<DieMapEntry> generated = RotateAtStart(BuildBaseOrder(Map, _options), _startDieUid);
            _order.Clear();
            _order.AddRange(generated);
            UpdateWrapIndex();
        }

        private void UpdateWrapIndex()
        {
            WrapAfterIndex = -1;
            var baseOrder = BuildBaseOrder(Map, _options);
            if (_order.Count == 0 || baseOrder.Count != _order.Count)
                return;
            var indexes = baseOrder.Select((e, i) => new { e.DieUid, Index = i })
                .ToDictionary(e => e.DieUid, e => e.Index, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i + 1 < _order.Count; i++)
            {
                if (indexes[_order[i].DieUid] == baseOrder.Count - 1 && indexes[_order[i + 1].DieUid] == 0)
                    WrapAfterIndex = i;
            }
        }

        private static bool SameUid(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static DieMap CopyMap(DieMap source)
        {
            if (source == null || source.Entries == null)
                throw new ArgumentException("표시할 Die Map이 없습니다.", "source");
            return new DieMap
            {
                FrameObjId = source.FrameObjId, DieMapX = source.DieMapX, DieMapY = source.DieMapY,
                PitchX = source.PitchX, PitchY = source.PitchY, DieSizeX = source.DieSizeX, DieSizeY = source.DieSizeY,
                OuterDiameterMm = source.OuterDiameterMm, OriginX = source.OriginX, OriginY = source.OriginY,
                EdgeSkipMode = source.EdgeSkipMode, SideEdgeSkip = source.SideEdgeSkip,
                TopBottomEdgeSkip = source.TopBottomEdgeSkip, SourceFileName = source.SourceFileName,
                SourceFormat = source.SourceFormat, SourcePitchFromFile = source.SourcePitchFromFile,
                Generation = GeneratedWaferMapCodec.CloneDefinition(source.Generation),
                SourceContentHash = source.SourceContentHash,
                ProcessTransform = WaferMapProcessService.CloneTransform(source.ProcessTransform),
                SourceDeclaredCount = source.SourceDeclaredCount, SourceFirstX = source.SourceFirstX,
                SourceFirstY = source.SourceFirstY, SourceFirstPosX = source.SourceFirstPosX,
                SourceFirstPosY = source.SourceFirstPosY, CreatedAt = source.CreatedAt,
                Entries = source.Entries.Select(e => e == null ? null : new DieMapEntry
                {
                    Index = e.Index, SequenceNo = e.SequenceNo, DieMapX = e.DieMapX, DieMapY = e.DieMapY,
                    OriginalMapX = e.OriginalMapX, OriginalMapY = e.OriginalMapY,
                    SourceBinCode = e.SourceBinCode, SourceToken = e.SourceToken,
                    LogicalGridX = e.LogicalGridX, LogicalGridY = e.LogicalGridY,
                    IsTarget = e.IsTarget, Result = e.Result, BinCode = e.BinCode,
                    PosX = e.PosX, PosY = e.PosY, EquipmentGridX = e.EquipmentGridX,
                    EquipmentGridY = e.EquipmentGridY, DieUid = e.DieUid
                }).ToList()
            };
        }
    }
}
