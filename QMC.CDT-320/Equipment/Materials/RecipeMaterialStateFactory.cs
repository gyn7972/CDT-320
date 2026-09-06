using System;
using QMC.CDT320.Recipes;

namespace QMC.CDT320.Materials
{
    /// <summary>
    /// 레시피에 맞는 빈 Material 후보만 생성합니다. 현재 상태 교체와 저장은 호출자가 수행합니다.
    /// </summary>
    internal static class RecipeMaterialStateFactory
    {
        public static bool TryCreate(
            RecipeProject recipe,
            string activeLotId,
            out MaterialSnapshot snapshot,
            out string reason)
        {
            snapshot = null;
            reason = "";
            if (recipe == null)
            {
                reason = "빈 Material을 생성할 레시피가 없습니다.";
                return false;
            }

            string recipeName = NormalizeRecipeName(recipe.FileName);
            if (string.IsNullOrWhiteSpace(recipeName))
            {
                reason = "빈 Material을 생성할 레시피 이름이 없습니다.";
                return false;
            }

            int inputLevels = recipe.InputCassetteLevelCount;
            int goodLevels = recipe.GoodCassetteLevelCount;
            // 시작 시 Material 사용 '아니오'와 동일한 레벨 수 보정 정책을 사용합니다.
            if (inputLevels < 1 || inputLevels > 2) inputLevels = 1;
            if (goodLevels < 1 || goodLevels > 2) goodLevels = 1;

            MaterialSnapshot candidate = MaterialStorage.CreateDefaultState(inputLevels, goodLevels, 25, 25);
            candidate.RecipeName = recipeName;
            // 레시피에 저장된 과거 LOT을 복원하지 않고, 호출자가 확인한 활성 LOT만 이어받습니다.
            candidate.LotId = string.IsNullOrWhiteSpace(activeLotId) ? "" : activeLotId.Trim();
            snapshot = candidate;
            return true;
        }

        private static string NormalizeRecipeName(string recipeName)
        {
            if (string.IsNullOrWhiteSpace(recipeName))
                return "";

            recipeName = recipeName.Trim();
            if (recipeName.EndsWith(".Project", StringComparison.OrdinalIgnoreCase))
                recipeName = recipeName.Substring(0, recipeName.Length - ".Project".Length);

            return recipeName;
        }
    }
}
