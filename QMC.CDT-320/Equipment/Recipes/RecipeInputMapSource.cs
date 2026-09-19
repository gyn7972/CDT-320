using System;
using System.IO;
using QMC.CDT320.Materials;
using QMC.CDT320.DieMaps;

namespace QMC.CDT320.Recipes
{
    /// <summary>입력 맵의 사용 모드와 원격 형식을 한 곳에서 결정한다. 누락된 구형 필드만 공통 설정을 따른다.</summary>
    public static class RecipeInputMapSource
    {
        internal static readonly object ModeSync = new object();
        public static event Action<RecipeProject> ModeActivated;

        public static bool RequestedUsesRemote(RecipeProject project, AppSettings settings)
        {
            return project != null && project.NextInputUseRemoteWaferMap.HasValue
                ? project.NextInputUseRemoteWaferMap.Value : UsesRemote(project, settings);
        }

        public static string DescribeMode(RecipeProject project, AppSettings settings)
        {
            bool active = UsesRemote(project, settings);
            string text = active ? "원격 다운로드" : "등록 맵 사용";
            if (project != null && project.NextInputUseRemoteWaferMap.HasValue)
                text = "현재 " + text + " → 다음 웨이퍼 " + (project.NextInputUseRemoteWaferMap.Value ? "원격 다운로드" : "등록 맵 사용");
            return text;
        }

        internal static RecipeProject CreateModeRequest(RecipeProject current, AppSettings settings, bool requested, bool defer)
        {
            bool active = UsesRemote(current, settings);
            RecipeProject updated = RecipeMapProcessSettingsService.CreateUpdatedProject(current,
                current.InputMapProcessing, current.OutputMapProcessing, defer ? active : requested);
            updated.NextInputUseRemoteWaferMap = defer && active != requested ? (bool?)requested : null;
            return updated;
        }

        // 모드 예약은 투입 시작에서만 활성화한다. 현재 웨이퍼의 재시도/재개는 스냅샷을 유지한다.
        public static void BeginWafer(WaferMaterial wafer)
        {
            RecipeProject activated = null;
            lock (ModeSync)
            {
                RecipeProject current = LoadActiveProject();
                bool active = UsesRemote(current, AppSettingsStore.Current);
                bool? pinned = MaterialStateService.GetInputMapModeSnapshot(wafer);
                if (pinned.HasValue)
                {
                    if (pinned.Value != active)
                        throw new InvalidDataException("현재 웨이퍼의 입력 맵 사용 모드와 적용 레시피가 다릅니다. 저장된 작업 상태를 확인하세요.");
                    MaterialStateService.PinInputMapMode(wafer, pinned.Value);
                    return;
                }
                // 구형 진행 자재도 현재 모드로 고정하고 예약은 다음 물리 웨이퍼까지 보류한다.
                bool alreadyStarted = wafer.HasInputStageDieMappingResult || wafer.BarcodeSequencePerformed;
                if (!alreadyStarted && current.NextInputUseRemoteWaferMap.HasValue)
                {
                    bool next = current.NextInputUseRemoteWaferMap.Value;
                    // 원격 OFF에서는 LOT BIN 초기화의 내구성을 먼저 확인한다.
                    // 이 경계에는 이전 입력 웨이퍼가 없으며, 실패하면 예약값을 유지한 채 투입을 중단한다.
                    if (active && !next)
                    {
                        MaterialStateService.ResetPickupBinSelectionToAll("NextInputWaferRegisteredMode:" + current.FileName);
                        if (!MaterialStateService.TryFlushPendingSave("NextInputWaferBinSelection"))
                            throw new IOException("다음 웨이퍼의 BIN 선택 초기화를 저장하지 못했습니다.");
                    }
                    RecipeProject updated = CreateModeRequest(current, AppSettingsStore.Current, next, false);
                    if (!RecipeStore.Save(updated)) throw new IOException("다음 웨이퍼의 입력 맵 사용 모드를 저장하지 못했습니다.");
                    RecipeProject verified = LoadActiveProject();
                    if (verified.InputUseRemoteWaferMap != next || verified.NextInputUseRemoteWaferMap.HasValue)
                        throw new InvalidDataException("다음 웨이퍼에 적용한 입력 맵 사용 모드의 저장 검증에 실패했습니다.");
                    active = next;
                    activated = verified;
                }
                MaterialStateService.PinInputMapMode(wafer, active);
                QMC.Common.Log.Write("Main", "SYSTEM", "InputMapMode",
                    "입력 웨이퍼 맵 사용 모드 고정. wafer=" + wafer.WaferInstanceId + ", remote=" + active + " - Ok");
            }
            if (activated != null)
            {
                try { ModeActivated?.Invoke(activated); }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InputMapMode", "입력 모드 적용 화면 알림 실패: " + ex + " - Failed");
                }
            }
        }

        private static RecipeProject LoadActiveProject()
        {
            string active = RecipeStore.GetLastProjectName();
            RecipeProject project = RecipeStore.LoadLastOrDefaultCached();
            if (string.IsNullOrWhiteSpace(active) || project == null ||
                !string.Equals(active, project.FileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("입력 맵 사용 모드를 확인할 활성 레시피가 없습니다. 레시피를 정상 적용하세요.");
            return project;
        }

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
