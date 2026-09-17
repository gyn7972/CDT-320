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
