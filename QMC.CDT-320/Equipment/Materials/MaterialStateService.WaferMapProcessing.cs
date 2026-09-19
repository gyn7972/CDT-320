using System;
using System.IO;
using System.Linq;
using QMC.CDT320.DieMaps;

namespace QMC.CDT320.Materials
{
    public static partial class MaterialStateService
    {
        private static void ClearPreparedWaferMapsForNewInstanceNoLock(WaferMaterial wafer)
        {
            wafer.InputPreparedMap = null;
            wafer.InputPreparedMapInstanceId = null;
            wafer.InputPreparedMapBarcode = null;
            wafer.InputPreparedMapUsesNetwork = false;
            wafer.InputMapUseRemoteSnapshot = null;
            wafer.OutputReceivePreparedMap = null;
            wafer.OutputReceivePreparedMapInstanceId = null;
        }

        private static void ValidatePreparedOutputReceiveOrder(WaferMaterial wafer,
            System.Collections.Generic.List<DieMapEntry> ordered)
        {
            var slots = wafer.OutputReceiveSlots;
            if (ordered == null || ordered.Count == 0 || slots == null || slots.Count != ordered.Count ||
                wafer.OutputReceiveTotalCount <= 0 || wafer.OutputReceiveTotalCount > ordered.Count ||
                wafer.OutputReceiveNextIndex < 0 || wafer.OutputReceiveNextIndex > ordered.Count)
                throw new InvalidDataException("저장된 Output 맵과 수납 슬롯 수가 일치하지 않습니다.");
            var byOrder = slots.Where(slot => slot != null).OrderBy(slot => slot.OrderIndex).ToList();
            if (byOrder.Count != ordered.Count)
                throw new InvalidDataException("저장된 Output 수납 슬롯에 누락이 있습니다.");
            for (int i = 0; i < ordered.Count; i++)
            {
                DieMapEntry entry = ordered[i];
                OutputReceiveSlotMaterial slot = byOrder[i];
                if (entry.SequenceNo != i + 1 || slot.OrderIndex != i ||
                    entry.DieMapX != slot.DieMapX || entry.DieMapY != slot.DieMapY ||
                    entry.OriginalMapX != slot.OriginalMapX || entry.OriginalMapY != slot.OriginalMapY ||
                    entry.LogicalGridX != slot.LogicalGridX || entry.LogicalGridY != slot.LogicalGridY ||
                    double.IsNaN(slot.PosX) || double.IsNaN(slot.PosY) ||
                    double.IsInfinity(slot.PosX) || double.IsInfinity(slot.PosY) ||
                    double.IsNaN(entry.PosX) || double.IsNaN(entry.PosY) ||
                    double.IsInfinity(entry.PosX) || double.IsInfinity(entry.PosY) ||
                    Math.Abs(entry.PosX - slot.PosX) > 0.000001 || Math.Abs(entry.PosY - slot.PosY) > 0.000001)
                    throw new InvalidDataException("저장된 Output 맵과 수납 슬롯의 순서·다이 주소·위치가 다릅니다. index=" + i);
            }
        }

        public static bool HasStartedInputMapWork()
        {
            return ReadState(state => state != null && state.Wafers != null && state.Wafers.Any(wafer =>
                wafer != null && wafer.CurrentLocation != null &&
                (wafer.CurrentLocation.Kind == MaterialLocationKind.InputStage || wafer.CurrentLocation.Kind == MaterialLocationKind.InputFeeder) &&
                (wafer.InputMapUseRemoteSnapshot.HasValue || wafer.InputPreparedMap != null ||
                 wafer.HasInputStageDieMappingResult || wafer.BarcodeSequencePerformed)));
        }

        // RecipeInputMapSource의 모드 저장/투입 잠금 안에서 호출한다. 모션이나 맵 생성은 하지 않는다.
        internal static bool? GetInputMapModeSnapshot(WaferMaterial expected)
        {
            lock (_stateSync)
            {
                WaferMaterial current = ResolveInputMapModeWaferNoLock(expected);
                if (current.InputMapUseRemoteSnapshot.HasValue) return current.InputMapUseRemoteSnapshot;
                if (current.InputPreparedMap != null &&
                    string.Equals(current.InputPreparedMapInstanceId, current.WaferInstanceId, StringComparison.OrdinalIgnoreCase))
                    return current.InputPreparedMapUsesNetwork;
                return null;
            }
        }

        internal static void PinInputMapMode(WaferMaterial expected, bool network)
        {
            lock (_stateSync)
            {
                WaferMaterial current = ResolveInputMapModeWaferNoLock(expected);
                if (current.InputMapUseRemoteSnapshot.HasValue && current.InputMapUseRemoteSnapshot.Value != network)
                    throw new InvalidDataException("현재 웨이퍼에 고정된 입력 맵 사용 모드를 변경할 수 없습니다.");
                current.InputMapUseRemoteSnapshot = network;
            }
            if (!TryNotifyAndSave("InputWaferMapModePinned") || !TryFlushPendingSave("InputWaferMapModePinned"))
                throw new IOException("현재 웨이퍼의 입력 맵 사용 모드를 저장하지 못했습니다. 투입을 중단합니다.");
        }

        private static WaferMaterial ResolveInputMapModeWaferNoLock(WaferMaterial expected)
        {
            if (expected == null || string.IsNullOrWhiteSpace(expected.WaferInstanceId))
                throw new InvalidDataException("입력 맵 모드를 고정할 물리 웨이퍼 식별자가 없습니다.");
            WaferMaterial current = State.Wafers.FirstOrDefault(wafer => wafer != null &&
                string.Equals(wafer.WaferInstanceId, expected.WaferInstanceId, StringComparison.OrdinalIgnoreCase) &&
                wafer.CurrentLocation != null && (wafer.CurrentLocation.Kind == MaterialLocationKind.InputStage ||
                wafer.CurrentLocation.Kind == MaterialLocationKind.InputFeeder));
            if (current == null) throw new InvalidDataException("입력 맵 모드를 준비하는 동안 웨이퍼가 변경되었습니다.");
            if (State.Wafers.Any(wafer => wafer != null && wafer != current && wafer.CurrentLocation != null &&
                (wafer.CurrentLocation.Kind == MaterialLocationKind.InputStage || wafer.CurrentLocation.Kind == MaterialLocationKind.InputFeeder)))
                throw new InvalidDataException("입력 스테이지/이동부의 이전 웨이퍼 처리를 먼저 완료해야 합니다.");
            return current;
        }

        public static DieMap GetPreparedInputMap(WaferMaterial wafer, string barcode, bool network)
        {
            if (wafer == null) return null;
            lock (_stateSync)
            {
                if (wafer.InputPreparedMap == null ||
                    !string.Equals(wafer.InputPreparedMapInstanceId, wafer.WaferInstanceId, StringComparison.OrdinalIgnoreCase)) return null;
                if (wafer.InputPreparedMapUsesNetwork != network ||
                    !string.Equals(wafer.InputPreparedMapBarcode ?? "", barcode ?? "", StringComparison.Ordinal))
                    throw new InvalidDataException("준비한 웨이퍼맵의 바코드 또는 원격/등록 모드가 변경되었습니다. 해당 웨이퍼의 맵 준비 상태를 확인하세요.");
                return WaferMapProcessService.CloneMap(wafer.InputPreparedMap);
            }
        }

        public static void PinPreparedInputMap(WaferMaterial expectedWafer, string barcode, bool network, DieMap map)
        {
            if (expectedWafer == null || map == null || map.ProcessTransform == null)
                throw new InvalidDataException("웨이퍼에 고정할 준비 맵과 변환 이력이 없습니다.");
            DieMap snapshot = WaferMapProcessService.CloneMap(map);
            lock (_stateSync)
            {
                WaferMaterial current = State.Wafers.FirstOrDefault(item => item != null &&
                    string.Equals(item.WaferInstanceId, expectedWafer.WaferInstanceId, StringComparison.OrdinalIgnoreCase) &&
                    item.CurrentLocation != null && item.CurrentLocation.Kind == MaterialLocationKind.InputStage);
                if (current == null || string.IsNullOrWhiteSpace(current.WaferInstanceId))
                    throw new InvalidDataException("맵 준비 중 InputStage 물리 웨이퍼가 변경되었습니다.");
                if (current.InputPreparedMap != null &&
                    string.Equals(current.InputPreparedMapInstanceId, current.WaferInstanceId, StringComparison.OrdinalIgnoreCase))
                {
                    DieMap existing = GetPreparedInputMap(current, barcode, network);
                    if (existing.ProcessTransform == null || !string.Equals(existing.ProcessTransform.SettingsKey,
                        snapshot.ProcessTransform.SettingsKey, StringComparison.Ordinal) ||
                        !string.Equals(existing.ProcessTransform.SourceMapHash, snapshot.ProcessTransform.SourceMapHash, StringComparison.Ordinal))
                        throw new InvalidDataException("이미 준비한 웨이퍼맵의 원본 또는 회전/원점 설정을 변경할 수 없습니다.");
                    return;
                }
                if (current.HasInputStageDieMappingResult)
                    throw new InvalidDataException("매핑이 완료된 웨이퍼에 기준 맵을 새로 적용할 수 없습니다. 저장된 공정 상태로 재개하세요.");
                current.InputPreparedMap = snapshot;
                current.InputPreparedMapInstanceId = current.WaferInstanceId;
                current.InputPreparedMapBarcode = barcode ?? "";
                current.InputPreparedMapUsesNetwork = network;
            }
            NotifyAndSave("InputWaferMapPrepared");
        }
    }
}
