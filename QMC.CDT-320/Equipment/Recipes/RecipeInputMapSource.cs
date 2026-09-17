using System;
using System.IO;
using QMC.CDT320.DieMaps;

namespace QMC.CDT320.Recipes
{
    /// <summary>입력 맵의 사용 모드와 원격 형식을 한 곳에서 결정한다. 누락된 구형 필드만 공통 설정을 따른다.</summary>
    public static class RecipeInputMapSource
    {
        public static bool UsesRemote(RecipeProject project, AppSettings settings)
        {
            if (project != null && project.InputUseRemoteWaferMap.HasValue)
                return project.InputUseRemoteWaferMap.Value;
            if (settings == null) throw new InvalidDataException("입력 맵 사용 설정을 읽을 수 없습니다.");
            return settings.UseLotNetworkWaferMap;
        }

        public static bool UsesRemoteForActiveRecipe(AppSettings settings)
        {
            string active = RecipeStore.GetLastProjectName();
            RecipeProject project = RecipeStore.LoadLastOrDefaultCached();
            if (string.IsNullOrWhiteSpace(active) || project == null ||
                !string.Equals(active, project.FileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("입력 맵 사용 모드를 확인할 활성 레시피가 없습니다. 레시피를 정상 적용하세요.");
            return UsesRemote(project, settings);
        }

        public static WaferMapSourceFormat ResolveFormat(RecipeProject project, AppSettings settings)
        {
            var profile = project != null ? project.InputMapProcessing : null;
            if (profile != null && profile.Format != WaferMapSourceFormat.Legacy) return profile.Format;
            return settings != null && string.Equals((settings.NetworkWaferMapFormat ?? "").Trim(), "Camtek", StringComparison.OrdinalIgnoreCase)
                ? WaferMapSourceFormat.Camtek : WaferMapSourceFormat.Samsung;
        }
    }
}
