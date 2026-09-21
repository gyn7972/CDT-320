using System;
using System.Text.RegularExpressions;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Common.WaferMaps
{
    // Called only for this UI's own text. SDK reasons, identifiers and logs remain opaque.
    internal static class WorkMapMessageText
    {
        internal static string DisplayKnown(string original)
        {
            if (original == null) return null;
            switch (original)
            {
                case "모션": return Lang.T("workMessage.fragment.motion");
                case "Needle Block Down 단위동작 함수가 아직 연결되어 있지 않습니다.": return Lang.T("workMessage.fragment.needleUnavailable");
                case "현재 선택 Die 절대좌표": return Lang.T("workMessage.fragment.absoluteSource");
                case "PROCESS 중심 기준 예상 Die 좌표": return Lang.T("workMessage.fragment.estimatedSource");
                case "마지막 MOVE VISION 전송 위치": return Lang.T("workMessage.fragment.lastVisionSource");
                case "Input PROCESS CENTER": return Lang.T("workMessage.fragment.processCenter");
                case "Die Map 적용 결과가 없습니다.": return Lang.T("workMessage.fragment.applyMissing");
                case "절대좌표 Die Map과 Mapping 완료 상태를 저장했습니다.": return Lang.T("workMessage.fragment.mappingSaved");
                case "기존 Die 상태와 검사 이력은 유지했습니다.": return Lang.T("workMessage.fragment.historyRetained");
                case "현재 Material 데이터의 저장은 완료되었지만 요청한 작업은 모두 완료되지 않았습니다.": return Lang.T("workMessage.fragment.savePartial");
                case "저장 완료는 확인되지 않았습니다.": return Lang.T("workMessage.fragment.saveUnconfirmed");
                case "\r\n\r\n[재픽업 복구]\r\n선택 Die가 실제로 Input Stage에 있는 것을 눈으로 확인한 경우에만 진행하십시오.\r\n이전 Pick/검사/Output 수신 상태를 초기화하여 다시 픽업 가능하게 만듭니다.": return Lang.T("workMessage.fragment.repickWarning");
                case "Front": return Lang.T("workMessage.fragment.front");
                case "Rear": return Lang.T("workMessage.fragment.rear");
            }
            if (original.StartsWith("FRONT PICKER #", StringComparison.Ordinal))
                return Lang.Format("workMessage.fragment.frontPicker", original.Substring(14));
            if (original.StartsWith("REAR PICKER #", StringComparison.Ordinal))
                return Lang.Format("workMessage.fragment.rearPicker", original.Substring(13));
            if (original.StartsWith("Die ", StringComparison.Ordinal))
                return Lang.Format("workMessage.fragment.die", original.Substring(4));
            return WorkMapText.Display(original);
        }

        internal static string DisplayOwnedMessage(string original)
        {
            if (original == null) return null;
            // Extract formatted text only; do not parse/recalculate coordinates or call equipment.
            string formatted;
            if (TryFormat(original, "\\A현재 StageT 위치를 InputStage 웨이퍼 T 보정값으로 저장하시겠습니까\\?\\r\\n\\r\\nReference T : (.*?)\\r\\nCurrent T   : (.*?)\\r\\nOffset T    : (.*?)\\r\\nLimit T     : (.*?)\\z", "workMessage.fragment.thetaConfirm", out formatted)) return formatted;
            if (TryFormat(original, "\\A선택 Die (.*?)개를 재픽업 대기로 복구했습니다\\.\\z", "workMessage.fragment.repickComplete", out formatted)) return formatted;
            if (TryFormat(original, "\\A선택 Die (.*?)개 상태 변경 완료\\.\\z", "workMessage.fragment.stateComplete", out formatted)) return formatted;
            if (TryFormat(original, "\\A\\r\\n실제 적용 Offset X=(.*?) mm, Y=(.*?) mm\\z", "workMessage.fragment.effectiveOffset", out formatted)) return formatted;
            if (TryFormat(original, "\\A\\r\\nNeedleX 이동 목표 X=(.*?) mm \\(Die VisionX=(.*?) - NeedleXToVisionXOffset=(.*?)\\)\\r\\nPicker 이동 목표 X=(.*?) mm, Y=(.*?) mm, StageY=(.*?) mm \\(CameraOffset X=(.*?), Y=(.*?) is applied once inside InputVisionToPicker\\)\\z", "workMessage.fragment.needleTargets", out formatted)) return formatted;
            if (TryFormat(original, "\\ADie Position\\(Offset 전\\) X=(.*?) mm, Y=(.*?) mm\\r\\n적용 Offset X=(.*?) mm, Y=(.*?) mm\\r\\n최종 Die 위치 X=(.*?) mm, Y=(.*?) mm\\z", "workMessage.fragment.diePosition", out formatted)) return formatted;
            return DisplayKnown(original);
        }

        private static bool TryFormat(string original, string pattern, string key, out string formatted)
        {
            Match match = Regex.Match(original, pattern, RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                formatted = null;
                return false;
            }
            object[] values = new object[match.Groups.Count - 1];
            for (int i = 0; i < values.Length; i++) values[i] = match.Groups[i + 1].Value;
            formatted = Lang.Format(key, values);
            return true;
        }
    }
}
