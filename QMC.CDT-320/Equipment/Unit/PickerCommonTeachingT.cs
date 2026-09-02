using System;
using System.IO;
using System.Runtime.Serialization;
using QMC.Common;
using QMC.Common.Data.Store;
using QMC.Common.Logging;

namespace QMC.CDT320
{
    /// <summary>픽커 T 각도의 레시피 공통 저장값. Front/Rear 4개 픽커가 같은 값을 사용한다.</summary>
    [DataContract]
    public class PickerCommonTeachingTRecipe
    {
        [DataMember] public double AvoidT { get; set; }
        [DataMember] public double PickT { get; set; }
        [DataMember] public double BottomT { get; set; }
        [DataMember] public double SideT { get; set; }
        [DataMember] public double PlaceT { get; set; }
    }

    /// <summary>
    /// 픽커 T 티칭값의 단일 읽기/쓰기 깔때기. 축(T0~T3)과 헤드(Front/Rear)에 무관하게
    /// 레시피 공통값 하나를 반환하고 갱신한다. 레시피에 값이 없으면 장비 Config 현재값을 이관한다.
    /// </summary>
    public static class PickerCommonTeachingT
    {
        private const string MachineRecipeStorageKey = "CDT-320";

        private static Func<CDT320MachineRecipe> _recipeGetter;
        private static Func<PickerAxisPositionSet> _configSourceGetter;
        private static bool _seedFailureLogged;

        public static void Bind(Func<CDT320MachineRecipe> recipeGetter, Func<PickerAxisPositionSet> configSourceGetter)
        {
            _recipeGetter = recipeGetter;
            _configSourceGetter = configSourceGetter;
        }

        public static bool IsPickerTAxis(PickerAxis axis)
        {
            return axis == PickerAxis.PickerT0 || axis == PickerAxis.PickerT1 ||
                   axis == PickerAxis.PickerT2 || axis == PickerAxis.PickerT3;
        }

        public static bool TryGet(PickerAxis axis, string positionName, out double value)
        {
            value = 0.0;
            string field;
            if (!IsPickerTAxis(axis) || !TryResolveField(positionName, out field))
                return false;

            PickerCommonTeachingTRecipe common = Resolve();
            if (common == null)
                return false;

            value = Read(common, field);
            return true;
        }

        public static bool TrySet(PickerAxis axis, string positionName, double value)
        {
            string field;
            if (!IsPickerTAxis(axis) || !TryResolveField(positionName, out field))
                return false;

            PickerCommonTeachingTRecipe common = Resolve();
            if (common == null)
                return false;

            double before = Read(common, field);
            Write(common, field, value);
            Log.Write("Main", "SYSTEM", "PickerCommonTeachingT",
                "픽커 T 공통값 변경. position=" + field +
                ", before=" + before.ToString("F4") +
                ", after=" + value.ToString("F4") +
                ", axis=" + axis + " - Ok");
            return true;
        }

        /// <summary>저장된 모든 레시피에 현재 Config 값을 1회 기록한다(값이 이미 있으면 건너뛴다).</summary>
        public static void MigrateAllRecipes(PickerAxisPositionSet configSource)
        {
            try
            {
                // 이관은 1회성·비가역이다. 소스를 믿을 수 없으면 아무 레시피도 건드리지 않고
                // 기존 Config 경로를 그대로 쓰게 둔다(비정지 통보만).
                PickerCommonTeachingTRecipe snapshot = BuildFromConfig(configSource);
                if (snapshot == null)
                {
                    EventLogger.Write(EventKind.Alarm, "SYSTEM", "PICKER-T-MIGRATE-SKIP", "PickerCommonTeachingT",
                        "픽커 T 이관 소스가 유효하지 않아(미로딩 추정) 레시피 기록을 건너뜁니다. Config 로드 상태를 확인하십시오.");
                    return;
                }

                foreach (string dir in Directory.GetDirectories(QMC.CDT320.Recipes.RecipeStore.Dir))
                {
                    string recipeName = Path.GetFileName(dir);
                    CDT320MachineRecipe recipe;
                    string reason;
                    if (!UnitDataStore.TryLoadRecipeRequired(recipeName, MachineRecipeStorageKey, out recipe, out reason))
                        continue;
                    if (recipe.PickerT != null)
                        continue;

                    recipe.PickerT = Clone(snapshot);
                    bool saved = UnitDataStore.SaveRecipe(recipe, recipeName, MachineRecipeStorageKey);
                    Log.Write("Main", "SYSTEM", "PickerCommonTeachingT",
                        "레시피에 픽커 T 공통값 현재값을 기록했습니다. recipe=" + recipeName +
                        ", avoid=" + recipe.PickerT.AvoidT.ToString("F4") +
                        ", pick=" + recipe.PickerT.PickT.ToString("F4") +
                        ", bottom=" + recipe.PickerT.BottomT.ToString("F4") +
                        ", side=" + recipe.PickerT.SideT.ToString("F4") +
                        ", place=" + recipe.PickerT.PlaceT.ToString("F4") +
                        ", saved=" + saved + " - " + (saved ? "Ok" : "Failed"));
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "PickerCommonTeachingT",
                    "픽커 T 공통값 레시피 일괄 기록 실패: " + ex.Message + " - Failed");
            }
        }

        private static PickerCommonTeachingTRecipe Resolve()
        {
            CDT320MachineRecipe recipe = _recipeGetter != null ? _recipeGetter() : null;
            if (recipe == null)
                return null;

            if (recipe.PickerT == null)
            {
                PickerCommonTeachingTRecipe seed = BuildFromConfig(_configSourceGetter != null ? _configSourceGetter() : null);
                if (seed == null)
                {
                    // 이관 소스를 믿을 수 없으면 레시피에 심지 않는다. 호출자는 false를 받아
                    // 기존 Config 축별 값을 그대로 읽는다(값 유실 방지).
                    if (!_seedFailureLogged)
                    {
                        _seedFailureLogged = true;
                        Log.Write("Main", "SYSTEM", "PickerCommonTeachingT",
                            "픽커 T 이관 소스가 유효하지 않아 레시피 공통값을 생성하지 않습니다. Config 축별 값을 사용합니다. - Check");
                    }
                    return null;
                }

                recipe.PickerT = seed;
                Log.Write("Main", "SYSTEM", "PickerCommonTeachingT",
                    "레시피에 픽커 T 공통값이 없어 장비 Config 현재값을 이관했습니다. avoid=" +
                    seed.AvoidT.ToString("F4") +
                    ", pick=" + seed.PickT.ToString("F4") +
                    ", bottom=" + seed.BottomT.ToString("F4") +
                    ", side=" + seed.SideT.ToString("F4") +
                    ", place=" + seed.PlaceT.ToString("F4") + " - Check");
            }

            return recipe.PickerT;
        }

        /// <summary>
        /// 이관 소스가 신뢰 가능한 경우에만 공통값을 만든다. 5개 값이 모두 정확히 0이면 Config가
        /// 로드되지 않은 기본 객체(미로딩)로 보고 거부한다 — 1회성 이관이 0을 영구 기록하는 것을 막는다.
        /// </summary>
        private static PickerCommonTeachingTRecipe BuildFromConfig(PickerAxisPositionSet source)
        {
            if (source == null)
                return null;

            if (source.AvoidPosition == 0.0 && source.PickPosition == 0.0 && source.BottomPosition == 0.0 &&
                source.SidePosition == 0.0 && source.PlacePosition == 0.0)
                return null;

            return new PickerCommonTeachingTRecipe
            {
                AvoidT = source.AvoidPosition,
                PickT = source.PickPosition,
                BottomT = source.BottomPosition,
                SideT = source.SidePosition,
                PlaceT = source.PlacePosition
            };
        }

        private static PickerCommonTeachingTRecipe Clone(PickerCommonTeachingTRecipe source)
        {
            return new PickerCommonTeachingTRecipe
            {
                AvoidT = source.AvoidT,
                PickT = source.PickT,
                BottomT = source.BottomT,
                SideT = source.SideT,
                PlaceT = source.PlaceT
            };
        }

        /// <summary>
        /// 공통값으로 다루는 위치 이름을 판정한다. 픽커별 Die*Position 배열도 같은 존 값으로 흡수해
        /// 공통값 밖으로 새는 경로를 남기지 않는다. Input/OutputAvoidPosition은 T축 대상이 아니다.
        /// </summary>
        private static bool TryResolveField(string positionName, out string field)
        {
            field = string.Empty;
            if (string.IsNullOrWhiteSpace(positionName))
                return false;

            if (positionName == "AvoidPosition") field = "AVOID";
            else if (positionName == "PickPosition") field = "PICK";
            else if (positionName == "BottomPosition") field = "BOTTOM";
            else if (positionName == "SidePosition") field = "SIDE";
            else if (positionName == "PlacePosition") field = "PLACE";
            else if (positionName.StartsWith("DiePickPosition", StringComparison.OrdinalIgnoreCase)) field = "PICK";
            else if (positionName.StartsWith("DieBottomPosition", StringComparison.OrdinalIgnoreCase)) field = "BOTTOM";
            else if (positionName.StartsWith("DieSidePosition", StringComparison.OrdinalIgnoreCase)) field = "SIDE";
            else if (positionName.StartsWith("DiePlacePosition", StringComparison.OrdinalIgnoreCase)) field = "PLACE";
            else return false;

            return true;
        }

        private static double Read(PickerCommonTeachingTRecipe common, string field)
        {
            if (field == "AVOID") return common.AvoidT;
            if (field == "PICK") return common.PickT;
            if (field == "BOTTOM") return common.BottomT;
            if (field == "SIDE") return common.SideT;
            return common.PlaceT;
        }

        private static void Write(PickerCommonTeachingTRecipe common, string field, double value)
        {
            if (field == "AVOID") common.AvoidT = value;
            else if (field == "PICK") common.PickT = value;
            else if (field == "BOTTOM") common.BottomT = value;
            else if (field == "SIDE") common.SideT = value;
            else common.PlaceT = value;
        }
    }
}
