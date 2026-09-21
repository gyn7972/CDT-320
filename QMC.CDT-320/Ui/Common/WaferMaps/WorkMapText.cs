using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Common.WaferMaps
{
    internal static class WorkMapText
    {
        private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "INPUT DIE MAP CREATE", "workMap.inputCreate" },
            { "Input Die Map", "workMap.inputMap" },
            { "DIE MAP INFO", "workMap.inputInfo" },
            { "Chip Width", "workMap.chipWidth" },
            { "Chip Height", "workMap.chipHeight" },
            { "Center Step X", "workMap.centerX" },
            { "Center Step Y", "workMap.centerY" },
            { "Wafer Dia", "workMap.diameter" },
            { "Axis X", "workMap.axisX" },
            { "Axis Y", "workMap.axisY" },
            { "BIN RANK", "workMap.binRank" },
            { "Die Number", "workMap.dieNumber" },
            { "DIE STATE EDIT", "workMap.dieState" },
            { "WAIT / 검사 대기", "workMap.waitInspect" },
            { "GOOD / 검사 완료", "workMap.goodInspect" },
            { "NG / 검사 불량", "workMap.ngInspect" },
            { "SKIP / 제외", "workMap.skip" },
            { "FLYING DIE / 유실", "workMap.flying" },
            { "APPLY SELECTED DIE", "workMap.applyDie" },
            { "APPLY SELECTED STATE", "workMap.applyState" },
            { "WAFER ALIGN / DIE MAP REVIEW", "workMap.review" },
            { "MANUAL ALIGN COMPLETE", "workMap.manualAlign" },
            { "RELOAD ACTIVE MAP", "workMap.reloadInput" },
            { "ACTION", "workMap.action" },
            { "T 보정", "workMap.theta" },
            { "다이 검출", "workMap.detect" },
            { "Offset 적용", "workMap.offset" },
            { "STANDARD", "workMap.standard" },
            { "START INDEX", "workMap.startIndex" },
            { "SELECT PICK STATUS", "workMap.selectStatus" },
            { "DRAG PICK STATUS", "workMap.dragStatus" },
            { "SELECT PICK STATUS SAVE", "workMap.saveStatus" },
            { "NEEDLE BLOCK DOWN", "workMap.needleDown" },
            { "INPUT STAGE DIE MAP", "workMap.inputStageMap" },
            { "Project Name :", "workMap.projectColon" },
            { "Barcode Name :", "workMap.barcode" },
            { "1Bin :", "workMap.firstBin" },
            { "CLOSE", "workMap.close" },
            { "Index", "workMap.index" },
            { "맵 X", "workMap.mapX" },
            { "맵 Y", "workMap.mapY" },
            { "Grid X", "workMap.gridX" },
            { "Grid Y", "workMap.gridY" },
            { "State", "workMap.state" },
            { "Result", "workMap.result" },
            { "Bin", "workMap.bin" },
            { "BIN", "workMap.bin" },
            { "Process X(mm)", "workMap.processX" },
            { "Process Y(mm)", "workMap.processY" },
            { "Die UID", "workMap.dieUid" },
            { "OUTPUT GOOD RECEIVE MAP", "workMap.outputGood" },
            { "OUTPUT NG RECEIVE MAP", "workMap.outputNg" },
            { "OUTPUT GOOD RECEIVE MAP DGV", "workMap.outputGrid" },
            { "BIN / DIE INFO", "workMap.outputInfo" },
            { "Project Name", "workMap.project" },
            { "Source Wafer :", "workMap.sourceWafer" },
            { "Side :", "workMap.side" },
            { "Progress", "workMap.progress" },
            { "Bin / State", "workMap.binState" },
            { "Next Target", "workMap.nextTarget" },
            { "OUTPUT STAGE", "workMap.outputStage" },
            { "GOOD", "workMap.good" },
            { "NG", "workMap.ng" },
            { "WAIT / 대기", "workMap.wait" },
            { "GOOD / 완료", "workMap.goodDone" },
            { "NG / 불량", "workMap.ngDone" },
            { "RELOAD OUTPUT DIE MAP", "workMap.reloadOutput" },
            { "MOVE SELECTED SLOT", "workMap.moveSlot" },
            { "GOOD PLAN INIT", "workMap.goodPlan" },
            { "NG PLAN INIT", "workMap.ngPlan" },
            { "SAVE MATERIAL STATE", "workMap.saveMaterial" },
            { "REFRESH DISPLAY", "workMap.refresh" },
            { "Die Size X (mm)", "workMap.sizeX" },
            { "Die Size Y (mm)", "workMap.sizeY" },
            { "Pitch Gap X (mm)", "workMap.gapX" },
            { "Pitch Gap Y (mm)", "workMap.gapY" },
            { "Wafer Diameter (mm)", "workMap.waferDiameter" },
            { "공정 순서", "workMap.processOrder" },
            { "BIN / DIE INFO   NO MAP", "workMap.noOutput" },
            { "DIE MAP INFO   NO MAP", "workMap.noInput" },
            { "NO MAP", "workMap.noMap" },
            { "INPUT", "workMap.input" },
            { "MOVE VISION", "workMap.visionMove" },
            { "MOVE VISION/STAGE", "workMap.stageMove" },
            { "MOVE FRONT PICKER", "workMap.frontMove" },
            { "MOVE REAR PICKER", "workMap.rearMove" },
            { "PLACE TEST FRONT PICKER", "workMap.frontPlace" },
            { "PLACE TEST REAR PICKER", "workMap.rearPlace" },
            { "PICKUP TEST FRONT PICKER", "workMap.frontPick" },
            { "PICKUP TEST REAR PICKER", "workMap.rearPick" },
            { "SET FRONT PICKER OFFSET", "workMap.frontOffset" },
            { "SET REAR PICKER OFFSET", "workMap.rearOffset" },
            { "MOVE DIE DATA TO PICKER", "workMap.dataMove" },
            { "FRONT PICKER", "workMap.front" },
            { "REAR PICKER", "workMap.rear" },
            { "INPUT MAP - NO ACTIVE RECIPE", "workMap.noRecipe" },
            { "INPUT MAP - WAFER SPEC NOT CONFIGURED", "workMap.noSpec" },
            { "RECIPE INPUT DIE MAP (CENTER-RELATIVE PREVIEW)", "workMap.recipePreview" },
            { "BASE WAFER MAP PREVIEW (WAFER SPEC SAVE + FINAL APPLY REQUIRED)", "workMap.basePreview" },
            { "INPUT MAP FINAL APPLY REQUIRED", "workMap.needApply" },
            { "RECIPE INPUT CIRCLE DIE MAP (CENTER-RELATIVE PREVIEW)", "workMap.circlePreview" },
            { "INPUT MAP LOAD FAILED - SEE LOG", "workMap.loadFailed" },
            { "ACTIVE INPUT DIE MAP (MACHINE ABSOLUTE)", "workMap.activeMap" },
            { "INPUT MAP ALIGN / MAPPING REQUIRED", "workMap.needAlign" },
            { "INPUT MAP REALIGN / REMAP REQUIRED", "workMap.needRealign" },
            { "INPUT MAP REMAP / FINAL APPLY REQUIRED", "workMap.needRemap" },
            { "INPUT WAFER MAP", "workMap.waferMap" },
            { "(no lot)", "workMap.noLot" },
        };

        private static readonly Dictionary<string, string> StateKeys = ReadDisplayAliases(
            "DiagramStrings", new[] { "diagram.liveMap.state.wait", "diagram.liveMap.state.visionDone",
                "diagram.liveMap.state.pickerHeld", "diagram.liveMap.state.placed", "diagram.liveMap.state.good",
                "diagram.liveMap.state.ng", "diagram.liveMap.state.skip", "diagram.liveMap.state.unknown" });
        private static readonly Dictionary<string, string> TrackingKeys = ReadDisplayAliases(
            "ControlStrings", new[] { "controls.map.goodStage", "controls.map.ngStage", "controls.map.outputFeeder",
                "controls.map.finish", "controls.map.heldFront", "controls.map.heldRear",
                "controls.map.reservedFront", "controls.map.reservedRear" });

        private static Dictionary<string, string> ReadDisplayAliases(string resourceName, string[] keys)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var resources = new ResourceManager("QMC.CDT_320.Ui.Localization.Resources." + resourceName, typeof(WorkMapText).Assembly);
            foreach (string language in Lang.Supported)
                foreach (string key in keys)
                {
                    string value = resources.GetString(key, CultureInfo.GetCultureInfo(language));
                    if (!string.IsNullOrEmpty(value)) result[value] = key;
                }
            return result;
        }

        /// <summary>기존 셀에 저장된 표시 언어만 다시 그립니다. 실제 Cell.Value와 추적 번호는 유지합니다.</summary>
        public static string DisplayCellState(string original)
        {
            if (original == null) return null;
            string key;
            if (StateKeys.TryGetValue(original, out key)) return Lang.T(key);
            string state = null;
            foreach (var entry in StateKeys)
                if ((state == null || entry.Key.Length > state.Length) &&
                    original.StartsWith(entry.Key + " / ", StringComparison.Ordinal))
                {
                    state = entry.Key;
                    key = entry.Value;
                }
            return state == null ? Display(original) :
                Lang.T(key) + " / " + DisplayTracking(original.Substring(state.Length + 3));
        }

        private static string DisplayTracking(string original)
        {
            string key;
            if (TrackingKeys.TryGetValue(original, out key)) return Lang.T(key);
            foreach (var entry in TrackingKeys)
            {
                int slot = entry.Key.IndexOf("{0}", StringComparison.Ordinal);
                if (slot < 0) continue;
                string prefix = entry.Key.Substring(0, slot);
                string suffix = entry.Key.Substring(slot + 3);
                if (!original.StartsWith(prefix, StringComparison.Ordinal) ||
                    !original.EndsWith(suffix, StringComparison.Ordinal) || original.Length < prefix.Length + suffix.Length) continue;
                string number = original.Substring(prefix.Length, original.Length - prefix.Length - suffix.Length);
                int pickerNumber;
                if (int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out pickerNumber))
                    return Lang.Format(entry.Value, number);
            }
            return WaferMapDisplayStyle.DisplayTrackingText(original);
        }

        public static string Display(string original)
        {
            if (original == null) return null;
            string key;
            return Keys.TryGetValue(original, out key) ? Lang.T(key) : Lang.Display(original);
        }

        public static void Bind(Control control, string original)
        {
            Lang.BindDisplay(control, original, Display);
        }

        public static void Bind(DataGridViewColumn column, string original)
        {
            string key;
            if (Keys.TryGetValue(original, out key)) Lang.BindKey(column, key);
        }

        public static void Bind(ToolStripItem item, string original)
        {
            string key;
            if (Keys.TryGetValue(original, out key)) Lang.BindKey(item, key);
        }
    }
}
