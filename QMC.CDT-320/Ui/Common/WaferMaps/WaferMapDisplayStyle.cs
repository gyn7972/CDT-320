using System;
using System.Drawing;
using System.Collections.Generic;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Common.WaferMaps
{
    /// <summary>화면 표시 전용 상태입니다. Material의 Result/BIN이나 시퀀스 상태에 저장하지 않습니다.</summary>
    public enum WaferMapCellState
    {
        Unknown,
        Wait,
        VisionDone,
        PickerHeld,
        Placed,
        Good,
        Ng,
        Skip
    }

    /// <summary>
    /// Input/Output 전환 화면과 WorkMain이 함께 사용하는 상태 문구·색·범례입니다.
    /// 전달받은 값만 계산하며 장비, 파일, Material 전역 상태에 접근하지 않습니다.
    /// 색상 값은 WaferMapPalette 한 곳에서만 수정합니다.
    /// </summary>
    public static class WaferMapDisplayStyle
    {
        /// <summary>
        /// 표시 셀에 연결할 Input Die의 소유권만 확인합니다. Material은 변경하지 않습니다.
        /// ownedDieIds가 null이면 구형 데이터의 미지정 목록, 빈 집합이면 소유 Die 없음입니다.
        /// </summary>
        public static bool CanMatchInputDie(DieMaterial die, WaferMaterial wafer, ISet<string> ownedDieIds)
        {
            if (die == null || wafer == null || string.IsNullOrWhiteSpace(die.DieId) ||
                string.IsNullOrWhiteSpace(wafer.WaferId))
                return false;

            if (!string.Equals(die.WaferID_Input, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return false;
            if (ownedDieIds != null && !ownedDieIds.Contains(die.DieId))
                return false;

            if (!string.IsNullOrWhiteSpace(wafer.WaferInstanceId) &&
                !string.IsNullOrWhiteSpace(die.InputWaferInstanceId))
            {
                return string.Equals(wafer.WaferInstanceId.Trim(), die.InputWaferInstanceId.Trim(),
                    StringComparison.OrdinalIgnoreCase);
            }
            return true;
        }

        public static WaferMapCellState ResolveInputState(
            bool isTarget, DieResult result, bool isOnPicker, bool inputVisionDone)
        {
            if (!isTarget) return WaferMapCellState.Skip;
            if (result == DieResult.NG) return WaferMapCellState.Ng;
            if (isOnPicker) return WaferMapCellState.PickerHeld;
            if (result == DieResult.Good) return WaferMapCellState.Good;
            if (inputVisionDone) return WaferMapCellState.VisionDone;
            return WaferMapCellState.Wait;
        }

        public static WaferMapCellState ResolveOutputState(
            bool isTarget, DieResult result, bool hasDie, bool inspectionDone, bool inspectionOk)
        {
            if (!isTarget) return WaferMapCellState.Skip;
            if (inspectionDone)
            {
                // GOOD BIN에 배치된 Die라도 검사 NG이면 반드시 NG색입니다.
                // 반대로 Output 검사가 OK여도 기존 물류 NG 결과를 GOOD로 바꾸지 않습니다.
                return !inspectionOk || result == DieResult.NG
                    ? WaferMapCellState.Ng
                    : WaferMapCellState.VisionDone;
            }
            if (hasDie || result == DieResult.Good || result == DieResult.NG)
                return WaferMapCellState.Placed;
            // 배치 전 BIN=1/255는 수납 계획이지 검사 완료를 뜻하지 않습니다.
            return WaferMapCellState.Wait;
        }

        public static Color GetColor(WaferMapCellState state)
        {
            switch (state)
            {
                case WaferMapCellState.Wait: return WaferMapPalette.Wait;
                case WaferMapCellState.VisionDone: return WaferMapPalette.Vision;
                case WaferMapCellState.PickerHeld: return WaferMapPalette.PickerHeld;
                case WaferMapCellState.Placed: return WaferMapPalette.Placed;
                case WaferMapCellState.Good: return WaferMapPalette.Good;
                case WaferMapCellState.Ng: return WaferMapPalette.Ng;
                case WaferMapCellState.Skip: return WaferMapPalette.Skip;
                default: return WaferMapPalette.Unknown;
            }
        }

        public static string GetText(WaferMapCellState state)
        {
            switch (state)
            {
                case WaferMapCellState.Wait: return "대기";
                case WaferMapCellState.VisionDone: return "검사 완료";
                case WaferMapCellState.PickerHeld: return "픽커 보유";
                case WaferMapCellState.Placed: return "안착 완료 / 검사 전";
                case WaferMapCellState.Good: return "GOOD";
                case WaferMapCellState.Ng: return "NG";
                case WaferMapCellState.Skip: return "SKIP";
                default: return "상태 미확인";
            }
        }

        /// <summary>예약/보유는 현재 실행 중이라는 뜻이 아닙니다. 실제 위치 정보만 상세 문구로 표시합니다.</summary>
        public static string GetTrackingText(MaterialLocationKind location, int pickerNo,
            MaterialLocationKind reservedLocation, int reservedPickerNo)
        {
            if (location == MaterialLocationKind.PickerFront)
                return "보유 F" + pickerNo;
            if (location == MaterialLocationKind.PickerRear)
                return "보유 R" + pickerNo;
            if (reservedPickerNo > 0 && reservedLocation == MaterialLocationKind.PickerFront)
                return "예약 F" + reservedPickerNo;
            if (reservedPickerNo > 0 && reservedLocation == MaterialLocationKind.PickerRear)
                return "예약 R" + reservedPickerNo;
            switch (location)
            {
                case MaterialLocationKind.OutputStageGood: return "GOOD STAGE";
                case MaterialLocationKind.OutputStageNg: return "NG STAGE";
                case MaterialLocationKind.OutputFeeder: return "OUT FEEDER";
                case MaterialLocationKind.OutputCassette: return "FINISH";
                default: return string.Empty;
            }
        }

        public static Tuple<string, Color>[] BuildLegend()
        {
            return new[]
            {
                Tuple.Create(GetText(WaferMapCellState.Wait), GetColor(WaferMapCellState.Wait)),
                Tuple.Create(GetText(WaferMapCellState.VisionDone), GetColor(WaferMapCellState.VisionDone)),
                Tuple.Create(GetText(WaferMapCellState.PickerHeld), GetColor(WaferMapCellState.PickerHeld)),
                Tuple.Create(GetText(WaferMapCellState.Placed), GetColor(WaferMapCellState.Placed)),
                Tuple.Create(GetText(WaferMapCellState.Good), GetColor(WaferMapCellState.Good)),
                Tuple.Create(GetText(WaferMapCellState.Ng), GetColor(WaferMapCellState.Ng)),
                Tuple.Create(GetText(WaferMapCellState.Skip), GetColor(WaferMapCellState.Skip)),
                Tuple.Create(GetText(WaferMapCellState.Unknown), GetColor(WaferMapCellState.Unknown))
            };
        }
    }
}
