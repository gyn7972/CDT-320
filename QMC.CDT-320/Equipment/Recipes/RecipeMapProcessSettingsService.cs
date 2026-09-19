using System;
using System.IO;
using System.Runtime.Serialization.Json;
using QMC.CDT320.DieMaps;

namespace QMC.CDT320.Recipes
{
    public static class RecipeMapProcessSettingsService
    {
        /// <summary>기준 맵/마스크를 재생성하지 않고 공정 설정을 갱신한다. 실제 저장/활성 적용은 호출자가 수행한다.</summary>
        public static RecipeProject CreateUpdatedProject(RecipeProject current, WaferMapProcessSettings input,
            WaferMapProcessSettings output, bool inputUsesNetwork)
        {
            if (current == null) throw new InvalidDataException("현재 레시피가 없습니다.");
            WaferMapProcessService.ValidateSettings(input);
            WaferMapProcessService.ValidateSettings(output);
            RecipeProject updated;
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(RecipeProject));
                serializer.WriteObject(stream, current);
                stream.Position = 0;
                updated = (RecipeProject)serializer.ReadObject(stream);
            }
            updated.InputUseRemoteWaferMap = inputUsesNetwork;
            updated.NextInputUseRemoteWaferMap = null;
            updated.InputMapProcessing = WaferMapProcessService.CloneSettings(input);
            updated.OutputMapProcessing = WaferMapProcessService.CloneSettings(output);
            foreach (RecipeMapKind kind in new[] { RecipeMapKind.Input, RecipeMapKind.GoodBin, RecipeMapKind.NgBin })
            {
                WaferMapProcessSettings before = kind == RecipeMapKind.Input ? current.InputMapProcessing : current.OutputMapProcessing;
                WaferMapProcessSettings after = kind == RecipeMapKind.Input ? input : output;
                if (WaferMapProcessService.GetSettingsKey(before) == WaferMapProcessService.GetSettingsKey(after)) continue;
                string path;
                string reason;
                DieMap approved = RecipeDieMapResolver.LoadCompatibleMap(current, kind, out path, out reason);
                if (approved == null) continue; // 기존에 미승인인 마스크를 설정 저장만으로 승인하지 않는다.
                // 원격 구분자 변경 때문에 등록 Input/Output 맵 승인을 해제하지 않는다.
                // 기존 승인 대상과 회전·원점의 유효성을 확인한 뒤 승인 해시를 갱신한다.
                WaferMapProcessService.Prepare(approved, after, kind.ToString());
                if (current.MapApprovalVersion > 0)
                    RecipeMapPaths.ApproveMap(updated, kind, approved);
            }
            return updated;
        }
    }
}
