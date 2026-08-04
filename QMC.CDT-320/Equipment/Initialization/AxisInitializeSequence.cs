using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;
using static QMC.CDT320.Initialization.AxisInitializePlanStore;

namespace QMC.CDT320.Initialization
{
    /// <summary>
    /// 전체 축 초기화의 Step 정의와 실행 순서를 한곳에서 관장합니다.
    /// 공통 선행 Step → Input/Output 병렬 Lane → SharedRail 후행 Step 순서를 유지하며,
    /// 선택된 한 Step의 실제 HOME 실행은 AxisInitializeExecutor에 위임합니다.
    /// </summary>
    internal sealed class AxisInitializeSequence
    {
        private const int CurrentDefaultVersion = 19;

        // 전체 초기화 실행 순서의 단일 정본입니다.
        // Step을 추가하거나 순서를 바꿀 때는 이 네 구간만 먼저 검토합니다.
        private static readonly int[] CommonStepOrder =
        {
            10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 150
        };

        private static readonly int[] InputLaneStepOrder =
        {
            170, 180, 190, 200, 210, 230
        };

        private static readonly int[] OutputLaneStepOrder =
        {
            240, 250, 260, 270, 290, 300, 310, 320, 330
        };

        private static readonly int[] SharedRailStepOrder =
        {
            340, 350, 360, 370
        };

        private static readonly int[] DefaultStepOrder = CommonStepOrder
            .Concat(InputLaneStepOrder)
            .Concat(OutputLaneStepOrder)
            .Concat(SharedRailStepOrder)
            .ToArray();

        #region 기본 초기화 Step 순서 정의

        /// <summary>
        /// 전체 초기화와 Monitor가 함께 사용하는 기본 Step 목록을 생성합니다.
        /// 실제 실행 순서를 바꾸려면 아래 구간 메서드 호출 순서와 각 StepNo를 함께 검토합니다.
        /// </summary>
        internal static AxisInitializePlan CreateDefaultPlan(IEnumerable<BaseAxis> axes)
        {
            var plan = new AxisInitializePlan
            {
                Comment = "CDT-320 automatic axis initialize sequence. JSON 표준 주석은 사용할 수 없어서 Comment/Help 필드로 수정 기준을 남깁니다.",
                Version = CurrentDefaultVersion,
                SavedAt = DateTime.Now,
                Steps = new List<AxisInitializeStep>()
            };

            try
            {
                var cleanAxes = (axes ?? Enumerable.Empty<BaseAxis>())
                    .Where(x => x != null)
                    .OrderBy(x => x.Setup != null ? x.Setup.AxisNo : int.MaxValue)
                    .ThenBy(x => x.Name)
                    .ToList();

                var axisByName = cleanAxes
                    .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 전체 초기화 Step 정의의 단일 정본입니다.
                // 10~150: 공통 안전 확보 → 170~330: Input/Output Lane 병렬 → 340~370: SharedRail 직렬
                AddVerticalAxisHomeSteps(plan, axisByName, used);
                AddReticleSafetySteps(plan, axisByName, used);
                AddStageZSafetySteps(plan, axisByName, used);
                AddPickerTHomeSteps(plan, axisByName, used);
                AddPickerYPairHomeStep(plan, axisByName, used);
                AddInputFeederHomeSteps(plan, axisByName, used);
                AddInputStageAndCassetteSteps(plan, axisByName, used);
                AddOutputLaneSteps(plan, axisByName, used);
                AddSharedRailHomeSteps(plan, axisByName, used);
                AssignDefaultParallelLanes(plan);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Axis initialize default plan create failed: " + ex.Message + " - Failed");
            }

            ApplyCommonSafetyInterlocks(plan);
            return plan;
        }

        private static void AddVerticalAxisHomeSteps(
            AxisInitializePlan plan,
            IDictionary<string, BaseAxis> axisByName,
            ISet<string> used)
        {
            // 1. 수직축을 먼저 Home하여 이후 실린더/평면축 이동 공간을 확보한다.
            AddKnownStep(plan, axisByName, used, 10, "FrontPickerZ", AxisInitializeRunMode.Parallel,
                "Front Picker Z0~Z3 home together.",
                "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3");
            AddKnownStep(plan, axisByName, used, 20, "RearPickerZ", AxisInitializeRunMode.Parallel,
                "Rear Picker Z0~Z3 home together.",
                "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");
            AddKnownStep(plan, axisByName, used, 30, "NeedleZ", AxisInitializeRunMode.Parallel,
                "NeedleZ and EjectPinZ home together.", "NeedleZ", "EjectPinZ");

        }

        private static void AddReticleSafetySteps(
            AxisInitializePlan plan,
            IDictionary<string, BaseAxis> axisByName,
            ISet<string> used)
        {
            // 2. Reticle은 Rear/Front Slide Bwd 완료 후 Lift Bwd 순서로 이동한다.
            AddActionOnlyStep(plan, 40, "ReticleSideSlideRear", "ReticleSideSlideRear",
                AxisInitializeActionCommand.CylinderBwd, "Reticle rear slide moves backward.");
            AddAxisHomeDoneInterlocks(plan, 40, "ReticleSideSlideRear",
                "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");

            AddActionOnlyStep(plan, 50, "ReticleSideSlideFront", "ReticleSideSlideFront",
                AxisInitializeActionCommand.CylinderBwd, "Reticle front slide moves backward.");
            AddAxisHomeDoneInterlocks(plan, 50, "ReticleSideSlideFront",
                "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");

            AddActionOnlyStep(plan, 60, "ReticleLift", "ReticleLift",
                AxisInitializeActionCommand.CylinderBwd, "Reticle lift moves backward after both slides.");
            AddAxisHomeDoneInterlocks(plan, 60, "ReticleLift",
                "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");
            AddStepInterlock(plan, 60, "ReticleLift", AxisInitializeInterlockTarget.Cylinder,
                "ReticleSideSlideRear", AxisInitializeInterlockState.Bwd,
                "Reticle Rear Slide를 Bwd 상태로 만든 후 다시 실행하십시오.");
            AddStepInterlock(plan, 60, "ReticleLift", AxisInitializeInterlockTarget.Cylinder,
                "ReticleSideSlideFront", AxisInitializeInterlockState.Bwd,
                "Reticle Front Slide를 Bwd 상태로 만든 후 다시 실행하십시오.");


        }

        private static void AddStageZSafetySteps(
            AxisInitializePlan plan,
            IDictionary<string, BaseAxis> axisByName,
            ISet<string> used)
        {
            // 3. OutputStage 6개 실린더 중 축 Home에 필요한 상태만 만든다.
            // 제품 유무와 관계없이 NG Clamp를 Bwd한 뒤 ClampLift를 Up한다.
            AddCustomActionOnlyStep(plan, 70, "OutputStageNGClampPrepare",
                AxisInitializeActionName.PrepareOutputStageNgClamp,
                "NG Stage 제품 유무와 관계없이 Clamp를 Bwd/Unclamp로 이동합니다.");

            AddActionOnlyStep(plan, 80, "OutputStageNGClampLift", "NGBinGuideClampLift",
                AxisInitializeActionCommand.CylinderFwd,
                "NG Clamp Bwd/Unclamp 완료 후 ClampLift를 Up으로 이동합니다.");

            AddKnownSingleStep(plan, axisByName, used, 90, "OutputStageZ", AxisInitializeRunMode.Serial,
                "OutputGoodStageZ home after NG Clamp Bwd and ClampLift Up.",
                "OutputGoodStageZ", "GoodStage_StageZ");
            AddFeederSafeInterlocks(plan, 90, "OutputStageZ", false);
            AddStepInterlock(plan, 90, "OutputStageZ", AxisInitializeInterlockTarget.Cylinder,
                "NGBinGuideClamp", AxisInitializeInterlockState.SafeForStageMove,
                "NG Bin ClampLift가 Up 상태인지 확인하십시오. Stage 이동 중 Clamp/Unclamp 상태는 무관합니다.");
            AddStepInterlock(plan, 90, "OutputStageZ", AxisInitializeInterlockTarget.Cylinder,
                "NGBinGuideClampLift", AxisInitializeInterlockState.Fwd,
                "NG ClampLift가 Up 상태인지 확인하십시오.");

            AddKnownSingleStep(plan, axisByName, used, 100, "InputStageZ", AxisInitializeRunMode.Serial,
                "InputExpandingZ home only when InputFeeder is empty and already unclamped.",
                "InputExpandingZ", "ExpanderZ");
            AddFeederSafeInterlocks(plan, 100, "InputStageZ", true);
            AddAxisHomeDoneInterlocks(plan, 100, "InputStageZ", "NeedleZ", "EjectPinZ");


        }

        private static void AddPickerTHomeSteps(
            AxisInitializePlan plan,
            IDictionary<string, BaseAxis> axisByName,
            ISet<string> used)
        {
            // 4. Picker T와 기구 간섭이 없는 같은 Side Vision Y축을 함께 초기화한다.
            AddKnownStep(plan, axisByName, used, 110, "FrontPickerT", AxisInitializeRunMode.Parallel,
                "Front Picker T0~T3 and Front Side Vision Y home together.",
                "FrontPickerT0", "FrontPickerT1", "FrontPickerT2", "FrontPickerT3",
                "FrontSideVisionY0");
            AddAxisHomeDoneInterlocks(plan, 110, "FrontPickerT",
                "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3");

            AddKnownStep(plan, axisByName, used, 120, "RearPickerT", AxisInitializeRunMode.Parallel,
                "Rear Picker T0~T3 and Rear Side Vision Y home together.",
                "RearPickerT0", "RearPickerT1", "RearPickerT2", "RearPickerT3",
                "RearSideVisionY0");
            AddAxisHomeDoneInterlocks(plan, 120, "RearPickerT",
                "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");


        }

        private static void AddPickerYPairHomeStep(
            AxisInitializePlan plan,
            IDictionary<string, BaseAxis> axisByName,
            ISet<string> used)
        {
            // 5. Picker Y는 수직 Stage가 안전해진 후 양쪽 하드리밋을 탐색하고 동시에 Home한다.
            AddKnownStep(plan, axisByName, used, 150, "PickerYPair", AxisInitializeRunMode.Parallel,
                "FrontPickerY MEL and RearPickerY PEL search, then simultaneous pair home.",
                "FrontPickerY", "RearPickerY");
            AddAxisHomeDoneInterlocks(plan, 150, "PickerYPair",
                "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3",
                "InputExpandingZ", "OutputGoodStageZ");
            AddStepInterlock(plan, 150, "PickerYPair", AxisInitializeInterlockTarget.Cylinder,
                "ReticleLift", AxisInitializeInterlockState.Bwd,
                "Reticle Lift가 Bwd 상태인지 확인하십시오.");


        }

        private static void AddInputFeederHomeSteps(
            AxisInitializePlan plan,
            IDictionary<string, BaseAxis> axisByName,
            ISet<string> used)
        {
            // 6. InputFeeder는 자동 Unclamp하지 않고 Empty/Unclamp 확인 후 Down/Home한다.
            AddActionOnlyStep(plan, 170, "InputFeederLift", "InputFeederLift",
                AxisInitializeActionCommand.CylinderBwd,
                "InputFeeder Lift moves Down only when empty and already unclamped.");
            AddFeederSafeInterlocks(plan, 170, "InputFeederLift", true);
            AddAxisHomeDoneInterlocks(plan, 170, "InputFeederLift", "FrontPickerY", "RearPickerY");

            AddKnownSingleStep(plan, axisByName, used, 180, "InputFeeder", AxisInitializeRunMode.Serial,
                "InputVisionX MEL safety retreat and InputFeederY home after Lift Down.",
                "InputFeederY", "FeederY");
            AddFeederSafeInterlocks(plan, 180, "InputFeeder", true);
            AddAxisHomeDoneInterlocks(plan, 180, "InputFeeder",
                "FrontPickerY", "RearPickerY",
                "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");
            AddStepInterlock(plan, 180, "InputFeeder", AxisInitializeInterlockTarget.Cylinder,
                "InputFeederLift", AxisInitializeInterlockState.Bwd,
                "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");


        }

        private static void AddInputStageAndCassetteSteps(
            AxisInitializePlan plan,
            IDictionary<string, BaseAxis> axisByName,
            ISet<string> used)
        {
            // 7. InputStage 평면축과 Input Cassette를 초기화한다.
            AddKnownStep(plan, axisByName, used, 190, "InputStageY", AxisInitializeRunMode.Parallel,
                "InputStageY and NeedleX home together when NeedleZ is at Home or Avoid.",
                "InputStageY", "NeedleX");
            AddFeederSafeInterlocks(plan, 190, "InputStageY", true);
            AddAxisHomeDoneInterlocks(plan, 190, "InputStageY",
                "InputFeederY", "NeedleZ", "EjectPinZ", "InputExpandingZ",
                "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");
            AddStepInterlock(plan, 190, "InputStageY", AxisInitializeInterlockTarget.Axis,
                "NeedleZ", AxisInitializeInterlockState.HomeOrAvoid,
                "NeedleZ를 Home(0) 또는 Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
            AddStepInterlock(plan, 190, "InputStageY", AxisInitializeInterlockTarget.Cylinder,
                "InputFeederLift", AxisInitializeInterlockState.Bwd,
                "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");

            AddAxisTeachingActionOnlyStep(plan, 200, "InputStageYAvoid", "InputStageY", "AvoidPosition",
                "InputStageY moves to Avoid after home.");
            AddFeederSafeInterlocks(plan, 200, "InputStageYAvoid", true);
            AddAxisHomeDoneInterlocks(plan, 200, "InputStageYAvoid",
                "InputStageY", "NeedleX", "InputFeederY", "InputExpandingZ", "EjectPinZ");
            AddStepInterlock(plan, 200, "InputStageYAvoid", AxisInitializeInterlockTarget.Axis,
                "InputFeederY", AxisInitializeInterlockState.AtPosition,
                "InputFeederY를 Home/Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
            AddStepInterlock(plan, 200, "InputStageYAvoid", AxisInitializeInterlockTarget.DigitalInput,
                AxisInitializeSafetyInput.WaferFeederAvoidPositionCheck, AxisInitializeInterlockState.On,
                "InputFeederY Home 후 Wafer Feeder Avoid 센서를 확인하십시오.");
            AddStepInterlock(plan, 200, "InputStageYAvoid", AxisInitializeInterlockTarget.Cylinder,
                "InputFeederLift", AxisInitializeInterlockState.Bwd,
                "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");

            AddKnownSingleStep(plan, axisByName, used, 210, "InputStageT", AxisInitializeRunMode.Serial,
                "InputStageT home after InputStageY Avoid.", "InputStageT", "StageT");
            AddFeederSafeInterlocks(plan, 210, "InputStageT", true);
            AddAxisHomeDoneInterlocks(plan, 210, "InputStageT",
                "InputStageY", "InputFeederY", "EjectPinZ", "InputExpandingZ",
                "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");
            AddStepInterlock(plan, 210, "InputStageT", AxisInitializeInterlockTarget.Cylinder,
                "InputFeederLift", AxisInitializeInterlockState.Bwd,
                "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");
            AddStepInterlock(plan, 210, "InputStageT", AxisInitializeInterlockTarget.Axis,
                "InputStageY", AxisInitializeInterlockState.AtPosition,
                "InputStageY를 Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");

            AddKnownStep(plan, axisByName, used, 230, "InputCassette", AxisInitializeRunMode.Serial,
                "InputLifterZ home after InputFeederY is safe.", "InputLifterZ");
            AddFeederSafeInterlocks(plan, 230, "InputCassette", true);
            AddAxisHomeDoneInterlocks(plan, 230, "InputCassette", "InputFeederY");
            AddStepInterlock(plan, 230, "InputCassette", AxisInitializeInterlockTarget.Cylinder,
                "InputFeederLift", AxisInitializeInterlockState.Bwd,
                "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");


        }

        private static void AddOutputLaneSteps(
            AxisInitializePlan plan,
            IDictionary<string, BaseAxis> axisByName,
            ISet<string> used)
        {
            // 8. OutputFeeder와 Output Stage Y축을 초기화한다.
            AddActionOnlyStep(plan, 240, "OutputGoodBinGuideDown", "GoodBinGuideLift",
                AxisInitializeActionCommand.CylinderBwd,
                "Good Bin Guide moves Down before OutputFeederY and NGStageY home.");
            AddAxisHomeDoneInterlocks(plan, 240, "OutputGoodBinGuideDown", "OutputGoodStageZ");

            // 사용자 승인 변경: OutputFeeder HOME 전에 Lift Up을 수행하던 기존 Step 250은 제거한다.
            // HOME 절대조건인 Lift Down을 먼저 만족시킨 뒤 OutputFeederY HOME을 실행한다.
            AddActionOnlyStep(plan, 250, "OutputFeederLiftDown", "OutputFeederLift",
                AxisInitializeActionCommand.CylinderBwd,
                "OutputFeeder Lift moves Down before OutputFeederY home.");
            AddFeederSafeInterlocks(plan, 250, "OutputFeederLiftDown", false);
            AddAxisHomeDoneInterlocks(plan, 250, "OutputFeederLiftDown",
                "FrontPickerY", "RearPickerY", "OutputGoodStageZ");
            AddStepInterlock(plan, 250, "OutputFeederLiftDown", AxisInitializeInterlockTarget.Cylinder,
                "GoodBinGuideLift", AxisInitializeInterlockState.Bwd,
                "Good Bin Guide를 Down 상태로 만든 후 다시 실행하십시오.");

            AddKnownStep(plan, axisByName, used, 260, "OutputFeeder", AxisInitializeRunMode.Serial,
                "OutputVisionX PEL safety retreat and OutputFeederY home after Lift Down and Good Guide Down.",
                "OutputFeederY");
            AddFeederSafeInterlocks(plan, 260, "OutputFeeder", false);
            AddAxisHomeDoneInterlocks(plan, 260, "OutputFeeder",
                "FrontPickerY", "RearPickerY", "OutputGoodStageZ",
                "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");
            AddStepInterlock(plan, 260, "OutputFeeder", AxisInitializeInterlockTarget.Cylinder,
                "OutputFeederLift", AxisInitializeInterlockState.Bwd,
                "OutputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");
            AddStepInterlock(plan, 260, "OutputFeeder", AxisInitializeInterlockTarget.Cylinder,
                "GoodBinGuideLift", AxisInitializeInterlockState.Bwd,
                "Good Bin Guide를 Down 상태로 만든 후 다시 실행하십시오.");

            AddAxisTeachingActionOnlyStep(plan, 270, "OutputStageZAvoid", "OutputGoodStageZ", "AvoidPosition",
                "OutputGoodStageZ moves to Avoid after OutputFeederY home.");
            AddAxisHomeDoneInterlocks(plan, 270, "OutputStageZAvoid", "OutputGoodStageZ", "OutputFeederY");
            AddFeederSafeInterlocks(plan, 270, "OutputStageZAvoid", false);
            AddStepInterlock(plan, 270, "OutputStageZAvoid", AxisInitializeInterlockTarget.Axis,
                "OutputFeederY", AxisInitializeInterlockState.AtPosition,
                "OutputFeederY를 Home/Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
            AddStepInterlock(plan, 270, "OutputStageZAvoid", AxisInitializeInterlockTarget.DigitalInput,
                AxisInitializeSafetyInput.BinFeederAvoidPositionCheck, AxisInitializeInterlockState.On,
                "OutputFeederY Home 후 Bin Feeder Avoid 센서를 확인하십시오.");
            AddStepInterlock(plan, 270, "OutputStageZAvoid", AxisInitializeInterlockTarget.Cylinder,
                "NGBinGuideClampLift", AxisInitializeInterlockState.Fwd,
                "NG ClampLift가 Up 상태인지 확인하십시오.");
            AddStepInterlock(plan, 270, "OutputStageZAvoid", AxisInitializeInterlockTarget.Cylinder,
                "NGBinGuideClamp", AxisInitializeInterlockState.SafeForStageMove,
                "NG Bin ClampLift가 Up 상태인지 확인하십시오. Stage 이동 중 Clamp/Unclamp 상태는 무관합니다.");

            AddKnownSingleStep(plan, axisByName, used, 290, "OutputNGStageY", AxisInitializeRunMode.Serial,
                "OutputNGStageY home after GoodStageZ Avoid and Good Guide Down.",
                "OutputNGStageY", "NgStage_StageY");
            AddAxisHomeDoneInterlocks(plan, 290, "OutputNGStageY", "OutputGoodStageZ", "OutputFeederY");
            AddStepInterlock(plan, 290, "OutputNGStageY", AxisInitializeInterlockTarget.Axis,
                "OutputGoodStageZ", AxisInitializeInterlockState.AtPosition,
                "OutputGoodStageZ를 Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
            AddStepInterlock(plan, 290, "OutputNGStageY", AxisInitializeInterlockTarget.Cylinder,
                "GoodBinGuideLift", AxisInitializeInterlockState.Bwd,
                "Good Bin Guide를 Down 상태로 만든 후 다시 실행하십시오.");
            AddStepInterlock(plan, 290, "OutputNGStageY", AxisInitializeInterlockTarget.Cylinder,
                "NGBinGuideClamp", AxisInitializeInterlockState.SafeForStageMove,
                "NG Bin ClampLift가 Up 상태인지 확인하십시오. Stage 이동 중 Clamp/Unclamp 상태는 무관합니다.");
            AddStepInterlock(plan, 290, "OutputNGStageY", AxisInitializeInterlockTarget.Cylinder,
                "NGBinGuideClampLift", AxisInitializeInterlockState.Fwd,
                "NG ClampLift가 Up 상태인지 확인하십시오.");

            AddAxisTeachingActionOnlyStep(plan, 300, "OutputNGStageYAvoid", "OutputNGStageY", "AvoidPosition",
                "OutputNGStageY moves to Avoid after home.");
            AddAxisHomeDoneInterlocks(plan, 300, "OutputNGStageYAvoid", "OutputNGStageY", "OutputGoodStageZ", "OutputFeederY");
            AddStepInterlock(plan, 300, "OutputNGStageYAvoid", AxisInitializeInterlockTarget.Cylinder,
                "GoodBinGuideLift", AxisInitializeInterlockState.Bwd,
                "Good Bin Guide를 Down 상태로 만든 후 다시 실행하십시오.");
            AddStepInterlock(plan, 300, "OutputNGStageYAvoid", AxisInitializeInterlockTarget.Cylinder,
                "NGBinGuideClampLift", AxisInitializeInterlockState.Fwd,
                "NG ClampLift가 Up 상태인지 확인하십시오.");
            AddStepInterlock(plan, 300, "OutputNGStageYAvoid", AxisInitializeInterlockTarget.Cylinder,
                "NGBinGuideClamp", AxisInitializeInterlockState.SafeForStageMove,
                "NG Bin ClampLift가 Up 상태인지 확인하십시오. Stage 이동 중 Clamp/Unclamp 상태는 무관합니다.");

            AddKnownSingleStep(plan, axisByName, used, 310, "OutputGoodStageY", AxisInitializeRunMode.Serial,
                "OutputGoodStageY home after OutputNGStageY Avoid.",
                "OutputGoodStageY", "GoodStage_StageY");
            AddAxisHomeDoneInterlocks(plan, 310, "OutputGoodStageY", "OutputGoodStageZ", "OutputNGStageY", "OutputFeederY");
            AddStepInterlock(plan, 310, "OutputGoodStageY", AxisInitializeInterlockTarget.Axis,
                "OutputFeederY", AxisInitializeInterlockState.AtPosition,
                "OutputFeederY를 Home/Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
            AddStepInterlock(plan, 310, "OutputGoodStageY", AxisInitializeInterlockTarget.DigitalInput,
                AxisInitializeSafetyInput.BinFeederAvoidPositionCheck, AxisInitializeInterlockState.On,
                "OutputFeederY Home 후 Bin Feeder Avoid 센서를 확인하십시오.");
            AddStepInterlock(plan, 310, "OutputGoodStageY", AxisInitializeInterlockTarget.Axis,
                "OutputGoodStageZ", AxisInitializeInterlockState.AtPosition,
                "OutputGoodStageZ를 Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
            AddStepInterlock(plan, 310, "OutputGoodStageY", AxisInitializeInterlockTarget.Axis,
                "OutputNGStageY", AxisInitializeInterlockState.AtPosition,
                "OutputNGStageY를 Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
            AddStepInterlock(plan, 310, "OutputGoodStageY", AxisInitializeInterlockTarget.Cylinder,
                "NGBinGuideClampLift", AxisInitializeInterlockState.Fwd,
                "NG ClampLift가 Up 상태인지 확인하십시오.");
            AddStepInterlock(plan, 310, "OutputGoodStageY", AxisInitializeInterlockTarget.Cylinder,
                "NGBinGuideClamp", AxisInitializeInterlockState.SafeForStageMove,
                "NG Bin ClampLift가 Up 상태인지 확인하십시오. Stage 이동 중 Clamp/Unclamp 상태는 무관합니다.");

            AddAxisTeachingActionOnlyStep(plan, 320, "OutputGoodStageYAvoid", "OutputGoodStageY", "AvoidPosition",
                "OutputGoodStageY moves to Avoid after home.");
            AddAxisHomeDoneInterlocks(plan, 320, "OutputGoodStageYAvoid",
                "OutputGoodStageY", "OutputGoodStageZ", "OutputNGStageY", "OutputFeederY");
            AddStepInterlock(plan, 320, "OutputGoodStageYAvoid", AxisInitializeInterlockTarget.Axis,
                "OutputNGStageY", AxisInitializeInterlockState.AtPosition,
                "OutputNGStageY를 Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
            AddStepInterlock(plan, 320, "OutputGoodStageYAvoid", AxisInitializeInterlockTarget.Cylinder,
                "NGBinGuideClampLift", AxisInitializeInterlockState.Fwd,
                "NG ClampLift가 Up 상태인지 확인하십시오.");
            AddStepInterlock(plan, 320, "OutputGoodStageYAvoid", AxisInitializeInterlockTarget.Cylinder,
                "NGBinGuideClamp", AxisInitializeInterlockState.SafeForStageMove,
                "NG Bin ClampLift가 Up 상태인지 확인하십시오. Stage 이동 중 Clamp/Unclamp 상태는 무관합니다.");

            AddKnownStep(plan, axisByName, used, 330, "OutputCassette", AxisInitializeRunMode.Serial,
                "OutputLifterZ home after OutputFeederY is safe.", "OutputLifterZ");
            AddFeederSafeInterlocks(plan, 330, "OutputCassette", false);
            AddAxisHomeDoneInterlocks(plan, 330, "OutputCassette", "OutputFeederY");
            AddStepInterlock(plan, 330, "OutputCassette", AxisInitializeInterlockTarget.Axis,
                "OutputFeederY", AxisInitializeInterlockState.AtPosition,
                "OutputFeederY를 Home/Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
            AddStepInterlock(plan, 330, "OutputCassette", AxisInitializeInterlockTarget.DigitalInput,
                AxisInitializeSafetyInput.BinFeederAvoidPositionCheck, AxisInitializeInterlockState.On,
                "OutputFeederY Home 후 Bin Feeder Avoid 센서를 확인하십시오.");
            AddStepInterlock(plan, 330, "OutputCassette", AxisInitializeInterlockTarget.Cylinder,
                "OutputFeederLift", AxisInitializeInterlockState.Bwd,
                "OutputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");


        }

        private static void AddSharedRailHomeSteps(
            AxisInitializePlan plan,
            IDictionary<string, BaseAxis> axisByName,
            ISet<string> used)
        {
            // 9. SharedRail X 4축은 마지막에 직렬 Home하며 현재→Home 전체 경로를 Pair Clearance로 검사한다.
            AddKnownSingleStep(plan, axisByName, used, 340, "InputVisionX", AxisInitializeRunMode.Serial,
                "InputVisionX home first on the shared rail.", "InputVisionX", "CameraX");
            AddAxisHomeDoneInterlocks(plan, 340, "InputVisionX", "InputFeederY", "FrontPickerY", "RearPickerY");
            AddStepInterlock(plan, 340, "InputVisionX", AxisInitializeInterlockTarget.Cylinder,
                "InputFeederLift", AxisInitializeInterlockState.Bwd,
                "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");
            AddStepInterlock(plan, 340, "InputVisionX", AxisInitializeInterlockTarget.DigitalInput,
                AxisInitializeSafetyInput.WaferFeederAvoidPositionCheck, AxisInitializeInterlockState.On,
                "InputVisionX HOME 전 InputFeeder Avoid Dog(X090)를 확인하십시오.");
            AddStepInterlock(plan, 340, "InputVisionX", AxisInitializeInterlockTarget.Axis,
                "InputVisionX", AxisInitializeInterlockState.SharedRailHomeClear,
                "SharedRail X축 현재 위치를 확인하고 간섭물을 제거한 후 다시 실행하십시오.");

            AddKnownStep(plan, axisByName, used, 350, "FrontPickerX", AxisInitializeRunMode.Serial,
                "FrontPickerX home after InputVisionX.", "FrontPickerX");
            AddAxisHomeDoneInterlocks(plan, 350, "FrontPickerX",
                "InputVisionX", "FrontPickerY", "RearPickerY",
                "InputFeederY", "OutputFeederY", "InputExpandingZ", "OutputGoodStageZ",
                "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3");
            AddPickerXGlobalSafetyInterlocks(plan, 350, "FrontPickerX");
            AddStepInterlock(plan, 350, "FrontPickerX", AxisInitializeInterlockTarget.Axis,
                "FrontPickerX", AxisInitializeInterlockState.SharedRailHomeClear,
                "SharedRail X축 현재 위치를 확인하고 간섭물을 제거한 후 다시 실행하십시오.");

            AddKnownStep(plan, axisByName, used, 360, "RearPickerX", AxisInitializeRunMode.Serial,
                "RearPickerX home after FrontPickerX.", "RearPickerX");
            AddAxisHomeDoneInterlocks(plan, 360, "RearPickerX",
                "InputVisionX", "FrontPickerY", "RearPickerY",
                "InputFeederY", "OutputFeederY", "InputExpandingZ", "OutputGoodStageZ",
                "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");
            AddPickerXGlobalSafetyInterlocks(plan, 360, "RearPickerX");
            AddStepInterlock(plan, 360, "RearPickerX", AxisInitializeInterlockTarget.Axis,
                "RearPickerX", AxisInitializeInterlockState.SharedRailHomeClear,
                "SharedRail X축 현재 위치를 확인하고 간섭물을 제거한 후 다시 실행하십시오.");

            AddKnownStep(plan, axisByName, used, 370, "SharedRailXOutput", AxisInitializeRunMode.Serial,
                "OutputVisionX home after both Picker X axes.", "OutputVisionX");
            AddAxisHomeDoneInterlocks(plan, 370, "SharedRailXOutput",
                "FrontPickerX", "RearPickerX", "OutputFeederY");
            AddStepInterlock(plan, 370, "SharedRailXOutput", AxisInitializeInterlockTarget.Cylinder,
                "OutputFeederLift", AxisInitializeInterlockState.Bwd,
                "OutputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");
            AddStepInterlock(plan, 370, "SharedRailXOutput", AxisInitializeInterlockTarget.DigitalInput,
                AxisInitializeSafetyInput.BinFeederAvoidPositionCheck, AxisInitializeInterlockState.On,
                "OutputVisionX HOME 전 OutputFeeder Avoid Dog(X091)를 확인하십시오.");
            AddStepInterlock(plan, 370, "SharedRailXOutput", AxisInitializeInterlockTarget.Axis,
                "OutputVisionX", AxisInitializeInterlockState.SharedRailHomeClear,
                "SharedRail X축 현재 위치를 확인하고 간섭물을 제거한 후 다시 실행하십시오.");
        }

        private static void AssignDefaultParallelLanes(
            AxisInitializePlan plan)
        {
            if (plan == null || plan.Steps == null)
                return;

            foreach (AxisInitializeStep step in plan.Steps.Where(x => x != null))
            {
                if (InputLaneStepOrder.Contains(step.StepNo))
                    step.ParallelLane = AxisInitializeParallelLane.Input;
                else if (OutputLaneStepOrder.Contains(step.StepNo))
                    step.ParallelLane = AxisInitializeParallelLane.Output;
                else
                    step.ParallelLane = AxisInitializeParallelLane.None;
            }
        }

        internal static string ResolveDefaultRoutePhase(int stepNo)
        {
            if (CommonStepOrder.Contains(stepNo))
                return AxisInitializeRoutePhase.CommonPreLane;
            if (InputLaneStepOrder.Contains(stepNo))
                return AxisInitializeRoutePhase.InputLane;
            if (OutputLaneStepOrder.Contains(stepNo))
                return AxisInitializeRoutePhase.OutputLane;
            if (SharedRailStepOrder.Contains(stepNo))
                return AxisInitializeRoutePhase.SharedRailPostLane;

            return AxisInitializeRoutePhase.SerialFallback;
        }

        #endregion

        private readonly AxisInitializeExecutor _executor;
        private readonly AxisInitializeRuntime _runtime;
        private readonly AxisInitializeRoutePlanner _routePlanner;
        private readonly CDT320_Machine _machine;

        public AxisInitializeSequence(
            AxisInitializeExecutor executor,
            AxisInitializeRuntime runtime,
            CDT320_Machine machine)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _routePlanner = new AxisInitializeRoutePlanner(_runtime);
        }

        /// <summary>
        /// Step 번호를 기준으로 실제 Unit의 축·실린더·DI를 연결합니다.
        /// 문자열 이름은 Monitor와 로그에만 사용하고 실제 장치 선택은 이 switch가 담당합니다.
        /// </summary>
        private AxisInitializeResult BindRuntimeTargets(IList<AxisInitializeStep> steps)
        {
            try
            {
                List<BaseAxis> knownAxes = GetKnownAxes();
                List<BaseCylinder> knownCylinders = GetKnownCylinders();

                foreach (AxisInitializeStep step in steps ?? new AxisInitializeStep[0])
                {
                    if (step == null || !step.Enabled)
                        continue;

                    step.RuntimeAxes = new List<BaseAxis>();
                    step.RuntimeInterlockAxes = new List<BaseAxis>();
                    step.RuntimeParallelHome = false;
                    step.RuntimePickerYPairHome = false;

                    string bindReason;
                    switch (step.StepNo)
                    {
                        // 10~30: 수직축을 먼저 연결하여 Picker와 Needle의 상하 간섭 공간을 확보합니다.
                        case 10:
                            BindAxes(step, true,
                                _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerZ0 : null,
                                _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerZ1 : null,
                                _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerZ2 : null,
                                _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerZ3 : null);
                            break;

                        case 20:
                            BindAxes(step, true,
                                _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerZ0 : null,
                                _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerZ1 : null,
                                _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerZ2 : null,
                                _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerZ3 : null);
                            break;

                        case 30:
                            BindAxes(step, true,
                                _machine.InputStageUnit != null ? _machine.InputStageUnit.NeedleZ : null,
                                _machine.InputStageUnit != null ? _machine.InputStageUnit.EjectPinZ : null);
                            break;

                        // 40~80: Reticle과 Output NG Clamp의 선행 실린더 동작입니다.
                        case 40:
                            BindCylinderAction(step,
                                _machine.VisionUnit != null ? _machine.VisionUnit.ReticleRearSideSlide : null);
                            break;

                        case 50:
                            BindCylinderAction(step,
                                _machine.VisionUnit != null ? _machine.VisionUnit.ReticleFrontSideSlide : null);
                            break;

                        case 60:
                            BindCylinderAction(step,
                                _machine.VisionUnit != null ? _machine.VisionUnit.ReticleLift : null);
                            break;

                        case 70:
                            BindCylinderAction(step,
                                _machine.OutputStageUnit != null
                                    ? _machine.OutputStageUnit.NgBinGuideClampCylinder
                                    : null);
                            break;

                        case 80:
                            BindCylinderAction(step,
                                _machine.OutputStageUnit != null
                                    ? _machine.OutputStageUnit.NgBinGuideClampLiftCylinder
                                    : null);
                            break;

                        // 90~120: Stage Z와 Picker T축을 실제 Unit 축에 연결합니다.
                        case 90:
                            BindAxes(step, false,
                                _machine.OutputStageUnit != null && _machine.OutputStageUnit.GoodStage != null
                                    ? _machine.OutputStageUnit.GoodStage.StageZ
                                    : null);
                            break;

                        case 100:
                            BindAxes(step, false,
                                _machine.InputStageUnit != null ? _machine.InputStageUnit.ExpanderZ : null);
                            break;

                        case 110:
                            BindAxes(step, true,
                                _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerT0 : null,
                                _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerT1 : null,
                                _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerT2 : null,
                                _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerT3 : null,
                                _machine.VisionUnit != null ? _machine.VisionUnit.FrontSideVisionY : null);
                            break;

                        case 120:
                            BindAxes(step, true,
                                _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerT0 : null,
                                _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerT1 : null,
                                _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerT2 : null,
                                _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerT3 : null,
                                _machine.VisionUnit != null ? _machine.VisionUnit.RearSideVisionY : null);
                            break;

                        // 150: Front/Rear PickerY는 일반 병렬 HOME이 아니라 전용 Pair 절차로 실행합니다.
                        case 150:
                            BindAxes(step, false,
                                _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerY : null,
                                _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerY : null);
                            step.RuntimePickerYPairHome = true;
                            break;

                        // 170~230: Input Lane의 Lift → Feeder → Stage → Cassette 순서입니다.
                        case 170:
                            BindCylinderAction(step,
                                _machine.InputFeederUnit != null
                                    ? _machine.InputFeederUnit.InputFeederLift
                                    : null);
                            break;

                        case 180:
                            BindAxes(step, false,
                                _machine.InputFeederUnit != null ? _machine.InputFeederUnit.FeederY : null);
                            break;

                        case 190:
                            BindAxes(step, true,
                                _machine.InputStageUnit != null ? _machine.InputStageUnit.StageY : null,
                                _machine.InputStageUnit != null ? _machine.InputStageUnit.NeedleBlockX : null);
                            break;

                        case 200:
                            BindInputStageYAvoidAction(step);
                            break;

                        case 210:
                            BindAxes(step, false,
                                _machine.InputStageUnit != null ? _machine.InputStageUnit.StageT : null);
                            break;

                        case 230:
                            BindAxes(step, false,
                                _machine.InputCassetteUnit != null ? _machine.InputCassetteUnit.InputLifterZ : null);
                            break;

                        // 240~330: Output Lane의 Guide/Lift → Feeder → Stage → Cassette 순서입니다.
                        case 240:
                            BindCylinderAction(step,
                                _machine.OutputStageUnit != null
                                    ? _machine.OutputStageUnit.GoodBinGuideLiftCylinder
                                    : null);
                            break;

                        case 250:
                            BindCylinderAction(step,
                                _machine.OutputFeederUnit != null
                                    ? _machine.OutputFeederUnit.FeederUpDownCyl
                                    : null);
                            break;

                        case 260:
                            BindAxes(step, false,
                                _machine.OutputFeederUnit != null ? _machine.OutputFeederUnit.FeederY : null);
                            break;

                        case 270:
                            BindOutputGoodStageZAvoidAction(step);
                            break;

                        case 290:
                            BindAxes(step, false,
                                _machine.OutputStageUnit != null && _machine.OutputStageUnit.NgStage != null
                                    ? _machine.OutputStageUnit.NgStage.StageY
                                    : null);
                            break;

                        case 300:
                            BindOutputNgStageYAvoidAction(step);
                            break;

                        case 310:
                            BindAxes(step, false,
                                _machine.OutputStageUnit != null && _machine.OutputStageUnit.GoodStage != null
                                    ? _machine.OutputStageUnit.GoodStage.StageY
                                    : null);
                            break;

                        case 320:
                            BindOutputGoodStageYAvoidAction(step);
                            break;

                        case 330:
                            BindAxes(step, false,
                                _machine.OutputCassetteUnit != null ? _machine.OutputCassetteUnit.OutputLifterZ : null);
                            break;

                        // 340~370: 양쪽 Lane 완료 후 SharedRail X축을 정해진 순서로 HOME 합니다.
                        case 340:
                            BindAxes(step, false,
                                _machine.InputStageUnit != null ? _machine.InputStageUnit.CameraX : null);
                            break;

                        case 350:
                            BindAxes(step, false,
                                _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerX : null);
                            break;

                        case 360:
                            BindAxes(step, false,
                                _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerX : null);
                            break;

                        case 370:
                            BindAxes(step, false,
                                _machine.OutputStageUnit != null ? _machine.OutputStageUnit.OutputCameraX : null);
                            break;

                        default:
                            bindReason = "지원하지 않는 전체 초기화 Step입니다. step=" + step.StepNo +
                                ", group=" + step.GroupName;
                            return AxisInitializeResult.Failure(-1, step, string.Empty, bindReason);
                    }

                    // Unit 연결 개수와 Action 목표가 완전한지 먼저 확인한 뒤 인터락 대상을 연결합니다.
                    if (!ValidateBoundStep(step, out bindReason))
                        return AxisInitializeResult.Failure(-1, step, string.Empty, bindReason);

                    if (!BindInterlockTargets(step, knownAxes, knownCylinders, out bindReason))
                        return AxisInitializeResult.Failure(-1, step, string.Empty, bindReason);

                    List<BaseAxis> interlockFallbackAxes = GetRuntimeStepAxes(step);
                    List<BaseAxis> resolvedInterlockAxes;
                    if (!_runtime.TryResolveInterlockAxes(
                        step,
                        interlockFallbackAxes,
                        out resolvedInterlockAxes,
                        out bindReason))
                    {
                        return AxisInitializeResult.Failure(
                            -1,
                            step,
                            string.Empty,
                            bindReason);
                    }
                    step.RuntimeInterlockAxes = resolvedInterlockAxes;
                }

                return AxisInitializeResult.Success();
            }
            catch (Exception ex)
            {
                string message = "전체 초기화 실제 장치 연결 중 예외가 발생했습니다. error=" + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "AxisInitializeBinding",
                    message + " - Failed");
                return AxisInitializeResult.Failure(-1, null, string.Empty, message);
            }
        }

        /// <summary>
        /// 한 Step에서 실제 HOME할 축과 Step 내부 동시 HOME 여부를 기록합니다.
        /// </summary>
        private static void BindAxes(
            AxisInitializeStep step,
            bool parallelHome,
            params BaseAxis[] axes)
        {
            step.RuntimeAxes = (axes ?? new BaseAxis[0]).Where(x => x != null).ToList();
            step.RuntimeParallelHome = parallelHome;
        }

        /// <summary>
        /// HOME 축과 Pre/Post Action 이동축을 합쳐 간섭 그룹 정지와 Lane 중복 검사에 사용합니다.
        /// </summary>
        private static List<BaseAxis> GetRuntimeStepAxes(AxisInitializeStep step)
        {
            var axes = new List<BaseAxis>();
            if (step == null)
                return axes;

            foreach (BaseAxis axis in step.RuntimeAxes ?? new List<BaseAxis>())
            {
                if (axis != null && !axes.Contains(axis))
                    axes.Add(axis);
            }

            IEnumerable<AxisInitializeAction> actions =
                (step.PreActions ?? new List<AxisInitializeAction>())
                .Concat(step.PostActions ?? new List<AxisInitializeAction>());
            foreach (AxisInitializeAction action in actions)
            {
                if (action != null && action.Enabled &&
                    action.RuntimeAxis != null && !axes.Contains(action.RuntimeAxis))
                {
                    axes.Add(action.RuntimeAxis);
                }
            }

            return axes;
        }

        private static AxisInitializeAction GetSingleEnabledPreAction(AxisInitializeStep step)
        {
            return step != null && step.PreActions != null
                ? step.PreActions.FirstOrDefault(x => x != null && x.Enabled)
                : null;
        }

        private static void BindCylinderAction(AxisInitializeStep step, BaseCylinder cylinder)
        {
            AxisInitializeAction action = GetSingleEnabledPreAction(step);
            if (action != null)
                action.RuntimeCylinder = cylinder;
        }

        private static void BindAxisAction(
            AxisInitializeStep step,
            BaseAxis axis,
            double targetPosition,
            string targetName)
        {
            AxisInitializeAction action = GetSingleEnabledPreAction(step);
            if (action == null)
                return;

            action.RuntimeAxis = axis;
            action.RuntimeTargetPosition = targetPosition;
            action.HasRuntimeTargetPosition = true;
            action.RuntimeTargetName = targetName ?? string.Empty;
        }

        private void BindInputStageYAvoidAction(AxisInitializeStep step)
        {
            BaseAxis axis = _machine.InputStageUnit != null ? _machine.InputStageUnit.StageY : null;
            double target = _machine.InputStageUnit != null &&
                            _machine.InputStageUnit.Recipe != null &&
                            _machine.InputStageUnit.Recipe.WaferY != null
                ? _machine.InputStageUnit.Recipe.WaferY.AvoidPosition
                : double.NaN;
            BindAxisAction(step, axis, target, "InputStageY.Avoid");
        }

        private void BindOutputGoodStageZAvoidAction(AxisInitializeStep step)
        {
            BaseAxis axis = _machine.OutputStageUnit != null && _machine.OutputStageUnit.GoodStage != null
                ? _machine.OutputStageUnit.GoodStage.StageZ
                : null;
            double target = _machine.OutputStageUnit != null &&
                            _machine.OutputStageUnit.Recipe != null &&
                            _machine.OutputStageUnit.Recipe.GoodStageZ != null
                ? _machine.OutputStageUnit.Recipe.GoodStageZ.AvoidPosition
                : double.NaN;
            BindAxisAction(step, axis, target, "OutputGoodStageZ.Avoid");
        }

        private void BindOutputNgStageYAvoidAction(AxisInitializeStep step)
        {
            BaseAxis axis = _machine.OutputStageUnit != null && _machine.OutputStageUnit.NgStage != null
                ? _machine.OutputStageUnit.NgStage.StageY
                : null;
            double target = _machine.OutputStageUnit != null &&
                            _machine.OutputStageUnit.Recipe != null &&
                            _machine.OutputStageUnit.Recipe.NGStageY != null
                ? _machine.OutputStageUnit.Recipe.NGStageY.AvoidPosition
                : double.NaN;
            BindAxisAction(step, axis, target, "OutputNGStageY.Avoid");
        }

        private void BindOutputGoodStageYAvoidAction(AxisInitializeStep step)
        {
            BaseAxis axis = _machine.OutputStageUnit != null && _machine.OutputStageUnit.GoodStage != null
                ? _machine.OutputStageUnit.GoodStage.StageY
                : null;
            double target = _machine.OutputStageUnit != null &&
                            _machine.OutputStageUnit.Recipe != null &&
                            _machine.OutputStageUnit.Recipe.GoodStageY != null
                ? _machine.OutputStageUnit.Recipe.GoodStageY.AvoidPosition
                : double.NaN;
            BindAxisAction(step, axis, target, "OutputGoodStageY.Avoid");
        }

        /// <summary>
        /// Step별 필수 축 수와 Action의 실제 장치 연결 상태를 모션 시작 전에 확인합니다.
        /// </summary>
        private static bool ValidateBoundStep(AxisInitializeStep step, out string reason)
        {
            reason = string.Empty;
            if (step == null)
            {
                reason = "초기화 Step 정보가 없습니다.";
                return false;
            }

            int expectedHomeAxisCount;
            switch (step.StepNo)
            {
                case 10:
                case 20:
                    expectedHomeAxisCount = 4;
                    break;

                case 30:
                case 150:
                case 190:
                    expectedHomeAxisCount = 2;
                    break;

                case 110:
                case 120:
                    expectedHomeAxisCount = 5;
                    break;

                case 90:
                case 100:
                case 180:
                case 210:
                case 230:
                case 260:
                case 290:
                case 310:
                case 330:
                case 340:
                case 350:
                case 360:
                case 370:
                    expectedHomeAxisCount = 1;
                    break;

                default:
                    expectedHomeAxisCount = 0;
                    break;
            }

            int boundHomeAxisCount = step.RuntimeAxes != null
                ? step.RuntimeAxes.Where(x => x != null).Distinct().Count()
                : 0;
            if (expectedHomeAxisCount > 0 && boundHomeAxisCount != expectedHomeAxisCount)
            {
                reason = "초기화 Step의 실제 Unit 축 구성이 완전하지 않습니다. step=" +
                    step.StepNo + ", group=" + step.GroupName +
                    ", expected=" + expectedHomeAxisCount +
                    ", boundDistinct=" + boundHomeAxisCount;
                return false;
            }

            IEnumerable<AxisInitializeAction> actions =
                (step.PreActions ?? new List<AxisInitializeAction>())
                .Concat(step.PostActions ?? new List<AxisInitializeAction>())
                .Where(x => x != null && x.Enabled);
            foreach (AxisInitializeAction action in actions)
            {
                if (string.Equals(action.Command, AxisInitializeActionCommand.AxisTeachingMove,
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (action.RuntimeAxis == null || !action.HasRuntimeTargetPosition ||
                        double.IsNaN(action.RuntimeTargetPosition) ||
                        double.IsInfinity(action.RuntimeTargetPosition))
                    {
                        reason = "초기화 티칭 이동의 실제 축/목표값을 찾을 수 없습니다. step=" +
                            step.StepNo + ", group=" + step.GroupName;
                        return false;
                    }
                }
                else if (string.Equals(action.Command, AxisInitializeActionCommand.CylinderFwd,
                             StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(action.Command, AxisInitializeActionCommand.CylinderBwd,
                             StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(action.Command, AxisInitializeActionCommand.CustomHook,
                             StringComparison.OrdinalIgnoreCase))
                {
                    if (action.RuntimeCylinder == null)
                    {
                        reason = "초기화 Action의 실제 Unit 실린더를 찾을 수 없습니다. step=" +
                            step.StepNo + ", group=" + step.GroupName;
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Plan의 표시 이름을 실제 Unit 축·실린더·DI 객체에 연결합니다.
        /// 실장비에서 찾지 못한 장치는 실행 전에 실패 처리합니다.
        /// </summary>
        private bool BindInterlockTargets(
            AxisInitializeStep step,
            IList<BaseAxis> knownAxes,
            IList<BaseCylinder> knownCylinders,
            out string reason)
        {
            reason = string.Empty;
            foreach (AxisInitializeInterlockRule rule in
                step != null && step.Interlocks != null
                    ? step.Interlocks
                    : new List<AxisInitializeInterlockRule>())
            {
                if (rule == null || !rule.Enabled)
                    continue;

                rule.RuntimeTargetBindingApplied = true;

                if (string.Equals(rule.TargetType, AxisInitializeInterlockTarget.Axis,
                    StringComparison.OrdinalIgnoreCase))
                {
                    rule.RuntimeAxis = FindKnownAxis(rule.Name, knownAxes);
                    if (rule.RuntimeAxis == null)
                    {
                        reason = BuildBindingFailure(step, rule);
                        return false;
                    }
                }
                else if (string.Equals(rule.TargetType, AxisInitializeInterlockTarget.Cylinder,
                    StringComparison.OrdinalIgnoreCase))
                {
                    rule.RuntimeCylinder = FindKnownCylinder(rule.Name, knownCylinders);
                    if (rule.RuntimeCylinder == null)
                    {
                        reason = BuildBindingFailure(step, rule);
                        return false;
                    }
                }
                else if (string.Equals(rule.TargetType, AxisInitializeInterlockTarget.DigitalInput,
                    StringComparison.OrdinalIgnoreCase))
                {
                    rule.RuntimeDigitalInput = ResolveSafetyInput(rule.Name);
                    // 순수 Simulation/BypassHardware에서는 물리 DI가 없어도 기존 정책상 허용됩니다.
                    // 실장비 모드는 InterlockService가 null을 실패 처리하며 이름 기반 신규 DI는 만들지 않습니다.
                }
            }

            return true;
        }

        private static string BuildBindingFailure(
            AxisInitializeStep step,
            AxisInitializeInterlockRule rule)
        {
            return "초기화 인터락의 실제 Unit 장치를 찾을 수 없습니다. step=" +
                (step != null ? step.StepNo : 0) + ", group=" +
                (step != null ? step.GroupName : "-") + ", target=" +
                (rule != null ? rule.TargetType + ":" + rule.Name : "-");
        }

        private static BaseAxis FindKnownAxis(string requestedName, IEnumerable<BaseAxis> axes)
        {
            if (string.IsNullOrWhiteSpace(requestedName))
                return null;

            string canonicalName = AjinAxisDefaults.ResolveName(requestedName.Trim());
            return (axes ?? Enumerable.Empty<BaseAxis>()).FirstOrDefault(axis =>
                axis != null &&
                (string.Equals(axis.Name, requestedName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(AjinAxisDefaults.ResolveName(axis.Name), canonicalName,
                     StringComparison.OrdinalIgnoreCase)));
        }

        private static BaseCylinder FindKnownCylinder(
            string requestedName,
            IEnumerable<BaseCylinder> cylinders)
        {
            if (string.IsNullOrWhiteSpace(requestedName))
                return null;

            return (cylinders ?? Enumerable.Empty<BaseCylinder>()).FirstOrDefault(cylinder =>
                cylinder != null &&
                string.Equals(cylinder.Name, requestedName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private BaseDigitalInput ResolveSafetyInput(string inputName)
        {
            if (_machine.OpPanelUnit != null)
            {
                if (string.Equals(inputName, AxisInitializeSafetyInput.ElecEmgOn,
                    StringComparison.OrdinalIgnoreCase))
                    return _machine.OpPanelUnit.EmgFront;
                if (string.Equals(inputName, AxisInitializeSafetyInput.OpEmgOn,
                    StringComparison.OrdinalIgnoreCase))
                    return _machine.OpPanelUnit.OpEmgOn;
                if (string.Equals(inputName, AxisInitializeSafetyInput.RearEmgOn,
                    StringComparison.OrdinalIgnoreCase))
                    return _machine.OpPanelUnit.EmgRear;
                if (string.Equals(inputName, AxisInitializeSafetyInput.RightEmgOn,
                    StringComparison.OrdinalIgnoreCase))
                    return _machine.OpPanelUnit.EmgLeft;
            }

            if (string.Equals(inputName, AxisInitializeSafetyInput.WaferFeederAvoidPositionCheck,
                StringComparison.OrdinalIgnoreCase))
            {
                return _machine.InputFeederUnit != null
                    ? _machine.InputFeederUnit.WaferFeederAvoidPositionCheckSensor
                    : null;
            }

            if (string.Equals(inputName, AxisInitializeSafetyInput.BinFeederAvoidPositionCheck,
                StringComparison.OrdinalIgnoreCase))
            {
                return _machine.OutputFeederUnit != null
                    ? _machine.OutputFeederUnit.BinFeederAvoidPositionCheckSensor
                    : null;
            }

            return null;
        }

        /// <summary>
        /// 초기화 Plan에서 참조할 수 있는 실제 장비 축 목록입니다.
        /// </summary>
        private List<BaseAxis> GetKnownAxes()
        {
            var axes = new List<BaseAxis>();
            AddAxes(axes,
                _machine.InputCassetteUnit != null ? _machine.InputCassetteUnit.InputLifterZ : null,
                _machine.InputFeederUnit != null ? _machine.InputFeederUnit.FeederY : null,
                _machine.InputStageUnit != null ? _machine.InputStageUnit.StageY : null,
                _machine.InputStageUnit != null ? _machine.InputStageUnit.StageT : null,
                _machine.InputStageUnit != null ? _machine.InputStageUnit.ExpanderZ : null,
                _machine.InputStageUnit != null ? _machine.InputStageUnit.CameraX : null,
                _machine.InputStageUnit != null ? _machine.InputStageUnit.NeedleBlockX : null,
                _machine.InputStageUnit != null ? _machine.InputStageUnit.NeedleZ : null,
                _machine.InputStageUnit != null ? _machine.InputStageUnit.EjectPinZ : null);

            if (_machine.PickerFrontUnit != null)
            {
                AddAxes(axes,
                    _machine.PickerFrontUnit.PickerX, _machine.PickerFrontUnit.PickerY,
                    _machine.PickerFrontUnit.PickerT0, _machine.PickerFrontUnit.PickerZ0,
                    _machine.PickerFrontUnit.PickerT1, _machine.PickerFrontUnit.PickerZ1,
                    _machine.PickerFrontUnit.PickerT2, _machine.PickerFrontUnit.PickerZ2,
                    _machine.PickerFrontUnit.PickerT3, _machine.PickerFrontUnit.PickerZ3);
            }

            if (_machine.PickerRearUnit != null)
            {
                AddAxes(axes,
                    _machine.PickerRearUnit.PickerX, _machine.PickerRearUnit.PickerY,
                    _machine.PickerRearUnit.PickerT0, _machine.PickerRearUnit.PickerZ0,
                    _machine.PickerRearUnit.PickerT1, _machine.PickerRearUnit.PickerZ1,
                    _machine.PickerRearUnit.PickerT2, _machine.PickerRearUnit.PickerZ2,
                    _machine.PickerRearUnit.PickerT3, _machine.PickerRearUnit.PickerZ3);
            }

            AddAxes(axes,
                _machine.VisionUnit != null ? _machine.VisionUnit.FrontSideVisionY : null,
                _machine.VisionUnit != null ? _machine.VisionUnit.RearSideVisionY : null,
                _machine.OutputStageUnit != null && _machine.OutputStageUnit.GoodStage != null
                    ? _machine.OutputStageUnit.GoodStage.StageY : null,
                _machine.OutputStageUnit != null && _machine.OutputStageUnit.GoodStage != null
                    ? _machine.OutputStageUnit.GoodStage.StageZ : null,
                _machine.OutputStageUnit != null && _machine.OutputStageUnit.NgStage != null
                    ? _machine.OutputStageUnit.NgStage.StageY : null,
                _machine.OutputStageUnit != null ? _machine.OutputStageUnit.OutputCameraX : null,
                _machine.OutputFeederUnit != null ? _machine.OutputFeederUnit.FeederY : null,
                _machine.OutputCassetteUnit != null ? _machine.OutputCassetteUnit.OutputLifterZ : null);

            return axes.Where(x => x != null).Distinct().ToList();
        }

        /// <summary>
        /// 초기화 Action과 인터락에서 참조할 수 있는 실제 실린더 목록입니다.
        /// </summary>
        private List<BaseCylinder> GetKnownCylinders()
        {
            var cylinders = new List<BaseCylinder>();
            AddCylinders(cylinders,
                _machine.VisionUnit != null ? _machine.VisionUnit.ReticleRearSideSlide : null,
                _machine.VisionUnit != null ? _machine.VisionUnit.ReticleFrontSideSlide : null,
                _machine.VisionUnit != null ? _machine.VisionUnit.ReticleLift : null,
                _machine.InputFeederUnit != null ? _machine.InputFeederUnit.InputFeederLift : null,
                _machine.InputFeederUnit != null ? _machine.InputFeederUnit.InputFeederClamp : null,
                _machine.OutputFeederUnit != null ? _machine.OutputFeederUnit.FeederUpDownCyl : null,
                _machine.OutputFeederUnit != null ? _machine.OutputFeederUnit.FeederClampCyl : null,
                _machine.OutputStageUnit != null ? _machine.OutputStageUnit.NgBinGuideClampCylinder : null,
                _machine.OutputStageUnit != null ? _machine.OutputStageUnit.NgBinGuideClampLiftCylinder : null,
                _machine.OutputStageUnit != null ? _machine.OutputStageUnit.GoodBinGuideLiftCylinder : null);
            return cylinders.Where(x => x != null).Distinct().ToList();
        }

        private static void AddAxes(ICollection<BaseAxis> target, params BaseAxis[] axes)
        {
            foreach (BaseAxis axis in axes ?? new BaseAxis[0])
            {
                if (axis != null)
                    target.Add(axis);
            }
        }

        private static void AddCylinders(
            ICollection<BaseCylinder> target,
            params BaseCylinder[] cylinders)
        {
            foreach (BaseCylinder cylinder in cylinders ?? new BaseCylinder[0])
            {
                if (cylinder != null)
                    target.Add(cylinder);
            }
        }

        /// <summary>
        /// 현재 축·인터락 상태를 읽어 경로를 미리 표시합니다.
        /// 실제 순서를 변경하거나 HOME을 허가하는 기능은 없습니다.
        /// </summary>
        internal AxisInitializeRouteResult PreviewRoute(IList<AxisInitializeStep> steps)
        {
            // Preview도 실제 실행과 같은 Unit 객체 바인딩을 사용합니다.
            AxisInitializeResult bindingResult = BindRuntimeTargets(steps);
            if (bindingResult != null && !bindingResult.Succeeded)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "AxisInitializeBinding",
                    "Axis initialize preview binding failed. " +
                    bindingResult.ErrorMessage + " - Failed");
            }

            Dictionary<int, AxisInitializeStep> stepByNumber;
            string layoutReason;
            bool useDefaultSequenceFlow =
                TryCreateStepIndex(steps, out stepByNumber, out layoutReason) &&
                TryValidateDefaultSequence(stepByNumber, out layoutReason);
            AxisInitializeRouteResult route = _routePlanner.Build(
                steps,
                useDefaultSequenceFlow);
            if (route == null)
                return route;

            bool bindingFailed = bindingResult != null && !bindingResult.Succeeded;
            bool layoutFailed = !useDefaultSequenceFlow;
            if (!bindingFailed && !layoutFailed)
                return route;

            // 전체 Plan의 장치 연결 또는 구성이 잘못되면 Monitor도 실행 가능으로 표시하지 않습니다.
            foreach (AxisInitializeRouteStep routeStep in route.Steps)
            {
                if (routeStep == null || string.Equals(
                    routeStep.State,
                    AxisInitializeRouteState.Disabled,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool failedStep = bindingFailed &&
                    (bindingResult.FailedStepNo == 0 ||
                    (routeStep.StepNo == bindingResult.FailedStepNo &&
                     string.Equals(
                          routeStep.GroupName,
                          bindingResult.FailedGroup,
                          StringComparison.OrdinalIgnoreCase)));
                routeStep.State = AxisInitializeRouteState.RequiresRecheck;
                routeStep.Reason = bindingFailed
                    ? (failedStep
                        ? bindingResult.ErrorMessage
                        : "초기화 장치 연결 실패를 먼저 해결해야 합니다.")
                    : layoutReason;
            }

            return route;
        }

        /// <summary>
        /// 전체 초기화 Plan을 실행합니다.
        /// 이 메서드가 전체 초기화 순서의 단일 진입점이며 Executor에는 전체 Plan 순서를 두지 않습니다.
        /// </summary>
        public async Task<AxisInitializeResult> ExecuteAsync(
            IList<AxisInitializeStep> steps,
            bool requireCompleteDefaultSequence)
        {
            bool runStarted = false;
            try
            {
                if (steps == null || steps.Count == 0)
                {
                    const string message = "초기화 Step 정보가 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                        "Axis initialize failed: step list is empty. - Failed");
                    return AxisInitializeResult.Failure(-1, null, string.Empty, message);
                }

                var enabledSteps = steps
                    .Where(x => x != null && x.Enabled)
                    .OrderBy(x => x.StepNo)
                    .ToList();

                AxisInitializeResult bindingResult = BindRuntimeTargets(enabledSteps);
                if (!bindingResult.Succeeded)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "AxisInitializeBinding",
                        bindingResult.ErrorMessage + " - Failed");
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "INIT-STEP-BINDING",
                        "MachineController",
                        bindingResult.ErrorMessage);
                    return bindingResult;
                }

                // 실제 Step 실행 전 현재 상태를 기록합니다.
                // 미리보기와 무관하게 각 Step 직전 실행용 인터락을 다시 검사합니다.
                TryLogRoutePreview(enabledSteps);

                _executor.BeginRun(enabledSteps);
                runStarted = true;

                return await ExecuteSequenceFlowAsync(
                    enabledSteps,
                    requireCompleteDefaultSequence).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string message = "초기화 Step 실행 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                    "Axis initialize step execution failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-STEP-EX",
                    "MachineController",
                    message);
                return AxisInitializeResult.Failure(-1, null, string.Empty, message);
            }
            finally
            {
                if (runStarted)
                    _executor.EndRun();
            }
        }

        private void TryLogRoutePreview(IList<AxisInitializeStep> steps)
        {
            try
            {
                Dictionary<int, AxisInitializeStep> stepByNumber;
                string layoutReason;
                bool useDefaultSequenceFlow =
                    TryCreateStepIndex(steps, out stepByNumber, out layoutReason) &&
                    TryValidateDefaultSequence(stepByNumber, out layoutReason);
                AxisInitializeRouteResult preview = _routePlanner.Build(
                    steps,
                    useDefaultSequenceFlow);
                if (preview == null)
                    return;

                QMC.Common.Log.Write("Main", "SYSTEM", "AxisInitializeRoutePreview",
                    "Axis initialize route preview. " + preview.BuildSummary() + " - Ok");

                AxisInitializeSafetySnapshot snapshot = preview.Snapshot;
                foreach (AxisInitializeAxisState axisState in
                    snapshot != null
                        ? snapshot.AxisStates
                        : new AxisInitializeAxisState[0])
                {
                    if (axisState == null)
                        continue;

                    QMC.Common.Log.Write("Main", "SYSTEM", "AxisInitializeSafetySnapshot",
                        axisState.BuildLogText() + " - Ok");
                }

                foreach (AxisInitializeRouteStep routeStep in preview.Steps)
                {
                    if (routeStep == null)
                        continue;

                    QMC.Common.Log.Write("Main", "SYSTEM", "AxisInitializeRoutePreview",
                        routeStep.BuildLogText() + " - Ok");
                }
            }
            catch (Exception ex)
            {
                // 미리보기는 진단 기능이므로 실패해도 기존 전체 초기화 실행을 막지 않습니다.
                QMC.Common.Log.Write("Main", "SYSTEM", "AxisInitializeRoutePreview",
                    "Axis initialize route preview failed. error=" + ex.Message + " - Continue");
            }
        }

        /// <summary>
        /// 현재 승인된 전체 초기화 흐름입니다.
        /// 1) 공통 선행 구간, 2) Input/Output 병렬 Lane, 3) SharedRail 후행 구간 순서를 유지합니다.
        /// </summary>
        private async Task<AxisInitializeResult> ExecuteSequenceFlowAsync(
            IList<AxisInitializeStep> enabledSteps,
            bool requireCompleteDefaultSequence)
        {
            try
            {
                Dictionary<int, AxisInitializeStep> stepByNumber;
                string indexReason;
                if (!TryCreateStepIndex(enabledSteps, out stepByNumber, out indexReason))
                    return FailPreparation(null, indexReason);

                // 전체 초기화는 필수 32개 Step 중 하나라도 없으면 일부 HOME만 실행하지 않고 중단합니다.
                // 그룹/선택 초기화만 전달된 Step은 StepNo 순서로 직렬 실행합니다.
                string layoutReason;
                if (!TryValidateDefaultSequence(stepByNumber, out layoutReason))
                {
                    if (requireCompleteDefaultSequence)
                        return FailPreparation(null, layoutReason);

                    return await ExecuteSelectedStepsSerialAsync(enabledSteps).ConfigureAwait(false);
                }

                // 공통 안전 확보 구간: 순서를 코드에서 직접 확인할 수 있도록 명시합니다.
                AxisInitializeResult preResult = await ExecuteSerialStepsAsync(
                    CommonStepOrder,
                    stepByNumber)
                    .ConfigureAwait(false);
                if (!preResult.Succeeded)
                    return preResult;

                AxisInitializeResult parallelResult = await ExecuteParallelLanesAsync(
                    stepByNumber).ConfigureAwait(false);
                if (!parallelResult.Succeeded)
                    return parallelResult;

                // SharedRail은 양쪽 Lane이 모두 끝난 뒤 340 → 350 → 360 → 370 순서로 실행합니다.
                return await ExecuteSerialStepsAsync(
                    SharedRailStepOrder,
                    stepByNumber).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string message = "병렬 초기화 플랜 실행 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                    message + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-PARALLEL-PLAN-EX",
                    "MachineController",
                    message);
                return AxisInitializeResult.Failure(-1, null, string.Empty, message);
            }
        }

        /// <summary>
        /// 활성 Step을 StepNo로 찾기 쉽게 만들고 중복 StepNo를 첫 모션 전에 차단합니다.
        /// </summary>
        private static bool TryCreateStepIndex(
            IEnumerable<AxisInitializeStep> steps,
            out Dictionary<int, AxisInitializeStep> stepByNumber,
            out string reason)
        {
            stepByNumber = new Dictionary<int, AxisInitializeStep>();
            reason = string.Empty;
            foreach (AxisInitializeStep step in steps ?? Enumerable.Empty<AxisInitializeStep>())
            {
                if (step == null || !step.Enabled)
                    continue;

                if (stepByNumber.ContainsKey(step.StepNo))
                {
                    reason = "초기화 StepNo가 중복되었습니다. step=" + step.StepNo;
                    return false;
                }

                stepByNumber.Add(step.StepNo, step);
            }

            return true;
        }

        /// <summary>
        /// 전체 초기화는 정본 32 Step과 Lane 구성이 정확히 일치할 때만 실행합니다.
        /// 일부 Step만 전달되는 단일/그룹 초기화는 이 검증 실패 후 직렬 경로로 분기합니다.
        /// </summary>
        private static bool TryValidateDefaultSequence(
            IDictionary<int, AxisInitializeStep> stepByNumber,
            out string reason)
        {
            reason = string.Empty;
            if (stepByNumber == null)
            {
                reason = "전체 초기화 Step 구성이 없습니다.";
                return false;
            }

            int[] missingSteps = DefaultStepOrder
                .Where(x => !stepByNumber.ContainsKey(x))
                .ToArray();
            int[] extraSteps = stepByNumber.Keys
                .Where(x => !DefaultStepOrder.Contains(x))
                .OrderBy(x => x)
                .ToArray();
            if (stepByNumber.Count != DefaultStepOrder.Length ||
                missingSteps.Length > 0 || extraSteps.Length > 0)
            {
                reason = "전체 초기화 필수 Step 구성이 완전하지 않습니다. missing=" +
                    (missingSteps.Length > 0
                        ? string.Join(",", missingSteps.Select(x => x.ToString()).ToArray())
                        : "-") +
                    ", extra=" +
                    (extraSteps.Length > 0
                        ? string.Join(",", extraSteps.Select(x => x.ToString()).ToArray())
                        : "-");
                return false;
            }

            foreach (int stepNo in InputLaneStepOrder)
            {
                if (AxisInitializeParallelLane.Is(
                    stepByNumber[stepNo].ParallelLane,
                    AxisInitializeParallelLane.Input))
                {
                    continue;
                }

                reason = "Input 초기화 Step의 Lane 지정이 올바르지 않습니다. step=" + stepNo;
                return false;
            }

            foreach (int stepNo in OutputLaneStepOrder)
            {
                if (AxisInitializeParallelLane.Is(
                    stepByNumber[stepNo].ParallelLane,
                    AxisInitializeParallelLane.Output))
                {
                    continue;
                }

                reason = "Output 초기화 Step의 Lane 지정이 올바르지 않습니다. step=" + stepNo;
                return false;
            }

            foreach (int stepNo in CommonStepOrder.Concat(SharedRailStepOrder))
            {
                if (string.IsNullOrWhiteSpace(stepByNumber[stepNo].ParallelLane))
                    continue;

                reason = "직렬 초기화 Step에 병렬 Lane이 지정되었습니다. step=" + stepNo +
                    ", lane=" + stepByNumber[stepNo].ParallelLane;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 전달받은 Step 순서를 그대로 하나씩 완료 확인하며 실행합니다.
        /// Common과 SharedRail이 이 공통 직렬 실행기를 사용합니다.
        /// </summary>
        private async Task<AxisInitializeResult> ExecuteSerialStepsAsync(
            IEnumerable<int> stepOrder,
            IDictionary<int, AxisInitializeStep> stepByNumber)
        {
            foreach (int stepNo in stepOrder ?? new int[0])
            {
                AxisInitializeResult result = await ExecuteStepByNumberAsync(
                    stepNo,
                    stepByNumber,
                    null,
                    string.Empty).ConfigureAwait(false);
                if (!result.Succeeded)
                    return result;
            }

            return AxisInitializeResult.Success();
        }

        /// <summary>
        /// 단일축·그룹·Monitor Step 실행은 병렬 Lane을 만들지 않고 StepNo 순으로 처리합니다.
        /// </summary>
        private async Task<AxisInitializeResult> ExecuteSelectedStepsSerialAsync(
            IEnumerable<AxisInitializeStep> steps)
        {
            Dictionary<int, AxisInitializeStep> stepByNumber;
            string reason;
            if (!TryCreateStepIndex(steps, out stepByNumber, out reason))
                return FailPreparation(null, reason);

            foreach (AxisInitializeStep step in stepByNumber.Values.OrderBy(x => x.StepNo))
            {
                AxisInitializeResult result = await ExecuteStepByNumberAsync(
                    step.StepNo,
                    stepByNumber,
                    null,
                    string.Empty).ConfigureAwait(false);
                if (!result.Succeeded)
                    return result;
            }

            return AxisInitializeResult.Success();
        }

        /// <summary>
        /// 실제 Step 실행 진입점입니다. 지원 Step 여부와 실제 Unit 연결은 BindRuntimeTargets에서 확인합니다.
        /// </summary>
        private Task<AxisInitializeResult> ExecuteStepByNumberAsync(
            int stepNo,
            IDictionary<int, AxisInitializeStep> stepByNumber,
            ISet<BaseAxis> allowedConcurrentAxes,
            string laneName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            AxisInitializeStep step;
            if (stepByNumber == null || !stepByNumber.TryGetValue(stepNo, out step))
            {
                return Task.FromResult(FailPreparation(
                    null,
                    "초기화 Step을 찾을 수 없습니다. step=" + stepNo));
            }

            return _executor.ExecuteStepAsync(
                step,
                step.RuntimeAxes,
                allowedConcurrentAxes,
                laneName,
                cancellationToken);
        }

        /// <summary>
        /// Input과 Output Lane만 동시에 실행합니다.
        /// 각 Lane 내부는 직렬이며 한쪽 실패 시 공유 취소 후 전체 축 정지를 요청합니다.
        /// </summary>
        private async Task<AxisInitializeResult> ExecuteParallelLanesAsync(
            IDictionary<int, AxisInitializeStep> stepByNumber)
        {
            ParallelLaneExecutionState executionState = null;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                List<AxisInitializeStep> inputLaneSteps = InputLaneStepOrder
                    .Select(x => stepByNumber[x])
                    .ToList();
                List<AxisInitializeStep> outputLaneSteps = OutputLaneStepOrder
                    .Select(x => stepByNumber[x])
                    .ToList();

                HashSet<BaseAxis> inputLaneAxes = CollectRuntimeAxes(inputLaneSteps);
                HashSet<BaseAxis> outputLaneAxes = CollectRuntimeAxes(outputLaneSteps);

                // Step 180/260 안에서 Vision X를 잠시 퇴피하므로 병렬 Lane의 허용 축에도 포함합니다.
                // HOME 대상 목록에는 넣지 않아 정식 SharedRail HOME(340~370) 순서는 그대로 유지합니다.
                if (_machine.InputStageUnit != null && _machine.InputStageUnit.CameraX != null)
                    inputLaneAxes.Add(_machine.InputStageUnit.CameraX);
                if (_machine.OutputStageUnit != null && _machine.OutputStageUnit.OutputCameraX != null)
                    outputLaneAxes.Add(_machine.OutputStageUnit.OutputCameraX);

                var overlappingAxes = inputLaneAxes
                    .Intersect(outputLaneAxes)
                    .ToList();
                if (overlappingAxes.Count > 0)
                {
                    string message = "Input/Output 병렬 초기화 Lane에 중복 축이 있습니다. axes=" +
                        string.Join(",", overlappingAxes.Select(x => x.Name).ToArray());
                    return FailPreparation(null, message);
                }

                AxisInitializeStep firstInputStep = stepByNumber[InputLaneStepOrder[0]];
                AxisInitializeStep firstOutputStep = stepByNumber[OutputLaneStepOrder[0]];
                string preflightReason;
                if (!_runtime.VerifyStep(firstInputStep, out preflightReason))
                {
                    _executor.RaiseStepProgress(
                        firstInputStep,
                        AxisInitializeStepStatus.Failed,
                        preflightReason);
                    return AxisInitializeResult.Failure(
                        -1,
                        firstInputStep,
                        AxisInitializeParallelLane.Input,
                        preflightReason);
                }

                if (!_runtime.VerifyStep(firstOutputStep, out preflightReason))
                {
                    _executor.RaiseStepProgress(
                        firstOutputStep,
                        AxisInitializeStepStatus.Failed,
                        preflightReason);
                    return AxisInitializeResult.Failure(
                        -1,
                        firstOutputStep,
                        AxisInitializeParallelLane.Output,
                        preflightReason);
                }

                executionState = new ParallelLaneExecutionState();
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                    "Input/Output initialize lanes start concurrently. inputSteps=" +
                    string.Join(",", inputLaneSteps.Select(x => x.StepNo + ":" + x.GroupName).ToArray()) +
                    ", outputSteps=" +
                    string.Join(",", outputLaneSteps.Select(x => x.StepNo + ":" + x.GroupName).ToArray()) +
                    ", inputAxes=" + string.Join(",", inputLaneAxes.Select(x => x.Name).ToArray()) +
                    ", outputAxes=" + string.Join(",", outputLaneAxes.Select(x => x.Name).ToArray()) + " - Start");

                Task<AxisInitializeResult> inputTask = Task.Run(() => ExecuteLaneAsync(
                    AxisInitializeParallelLane.Input,
                    InputLaneStepOrder,
                    stepByNumber,
                    outputLaneAxes,
                    executionState));
                Task<AxisInitializeResult> outputTask = Task.Run(() => ExecuteLaneAsync(
                    AxisInitializeParallelLane.Output,
                    OutputLaneStepOrder,
                    stepByNumber,
                    inputLaneAxes,
                    executionState));
                AxisInitializeResult[] results = await Task.WhenAll(inputTask, outputTask).ConfigureAwait(false);

                AxisInitializeResult failure = executionState.GetFailure();
                if (failure != null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                        "Input/Output initialize lanes failed. firstFailureLane=" + failure.FailedLane +
                        ", step=" + failure.FailedStepNo +
                        ", group=" + failure.FailedGroup +
                        ", elapsedMs=" + stopwatch.ElapsedMilliseconds +
                        ", message=" + failure.ErrorMessage + " - Failed");
                    return failure;
                }

                AxisInitializeResult failedResult = results.FirstOrDefault(x => x != null && !x.Succeeded);
                if (failedResult != null)
                {
                    const string message = "병렬 초기화 Lane이 실패했지만 상세 실패 정보가 없습니다.";
                    return AxisInitializeResult.Failure(
                        failedResult.ResultCode,
                        null,
                        string.Empty,
                        message);
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                    "Input/Output initialize lanes completed concurrently. elapsedMs=" +
                    stopwatch.ElapsedMilliseconds + " - Ok");
                return AxisInitializeResult.Success();
            }
            catch (Exception ex)
            {
                if (executionState != null)
                {
                    executionState.Cancel();
                    if (executionState.TryRequestAxisStop())
                        await _runtime.StopAllAxesAsync().ConfigureAwait(false);
                }

                string message = "병렬 초기화 Lane 실행 예외: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                    message + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-PARALLEL-LANE-EX",
                    "MachineController",
                    message);
                return AxisInitializeResult.Failure(-1, null, string.Empty, message);
            }
            finally
            {
                stopwatch.Stop();
                if (executionState != null)
                    executionState.Dispose();
            }
        }

        /// <summary>
        /// Lane이 HOME하거나 Action으로 움직일 모든 실제 축을 모아 양쪽 Lane 중복을 검사합니다.
        /// </summary>
        private static HashSet<BaseAxis> CollectRuntimeAxes(
            IEnumerable<AxisInitializeStep> steps)
        {
            var axes = new HashSet<BaseAxis>();
            foreach (AxisInitializeStep step in steps ?? Enumerable.Empty<AxisInitializeStep>())
            {
                foreach (BaseAxis axis in step != null && step.RuntimeAxes != null
                    ? step.RuntimeAxes
                    : new List<BaseAxis>())
                {
                    if (axis != null)
                        axes.Add(axis);
                }

                IEnumerable<AxisInitializeAction> actions = step != null
                    ? (step.PreActions ?? new List<AxisInitializeAction>())
                        .Concat(step.PostActions ?? new List<AxisInitializeAction>())
                    : new List<AxisInitializeAction>();
                foreach (AxisInitializeAction action in actions)
                {
                    if (action != null && action.Enabled && action.RuntimeAxis != null)
                        axes.Add(action.RuntimeAxis);
                }
            }

            return axes;
        }

        /// <summary>
        /// 한 Lane의 Step을 순서대로 실행하며 반대 Lane 취소 신호를 Step 전후에 확인합니다.
        /// </summary>
        private async Task<AxisInitializeResult> ExecuteLaneAsync(
            string laneName,
            IList<int> stepNumbers,
            IDictionary<int, AxisInitializeStep> stepByNumber,
            ISet<BaseAxis> allowedConcurrentAxes,
            ParallelLaneExecutionState executionState)
        {
            AxisInitializeStep currentStep = null;
            try
            {
                var orderedSteps = (stepNumbers ?? new int[0])
                    .Select(x => stepByNumber[x])
                    .ToList();
                for (int i = 0; i < orderedSteps.Count; i++)
                {
                    currentStep = orderedSteps[i];
                    if (executionState != null && executionState.Token.IsCancellationRequested)
                    {
                        string message = "반대 Lane 실패로 병렬 초기화가 중단되었습니다. lane=" + laneName;
                        MarkLaneStepsCancelled(orderedSteps, i, message);
                        return AxisInitializeResult.Failure(-1, currentStep, laneName, message);
                    }

                    AxisInitializeResult stepResult = await ExecuteStepByNumberAsync(
                        currentStep.StepNo,
                        stepByNumber,
                        allowedConcurrentAxes,
                        laneName,
                        executionState != null
                            ? executionState.Token
                            : CancellationToken.None).ConfigureAwait(false);
                    if (stepResult.Succeeded &&
                        executionState != null &&
                        executionState.Token.IsCancellationRequested)
                    {
                        string reinitializeMessage =
                            "반대 Lane 실패 중 동작이 정지되었으므로 재초기화가 필요합니다. lane=" + laneName;
                        _executor.RaiseStepProgress(
                            currentStep,
                            AxisInitializeStepStatus.ReinitializeRequired,
                            reinitializeMessage);
                        string cancelledMessage =
                            "반대 Lane 실패로 병렬 초기화가 중단되었습니다. lane=" + laneName;
                        MarkLaneStepsCancelled(orderedSteps, i + 1, cancelledMessage);
                        return AxisInitializeResult.Failure(
                            -1,
                            currentStep,
                            laneName,
                            reinitializeMessage);
                    }

                    if (stepResult.Succeeded)
                        continue;

                    string failureMessage = ResolveParallelLaneFailureMessage(
                        stepResult,
                        currentStep,
                        laneName);
                    AxisInitializeResult failure = AxisInitializeResult.Failure(
                        stepResult.ResultCode,
                        currentStep,
                        laneName,
                        failureMessage);
                    await ReportParallelLaneFailureAsync(
                        executionState,
                        failure).ConfigureAwait(false);
                    return failure;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeLane",
                    "Initialize parallel lane completed. lane=" + laneName + " - Ok");
                return AxisInitializeResult.Success();
            }
            catch (Exception ex)
            {
                string failureMessage = "병렬 초기화 Lane 예외. lane=" + laneName +
                    ", step=" + (currentStep != null ? currentStep.StepNo : 0) +
                    ", group=" + (currentStep != null ? currentStep.GroupName : "-") +
                    ", error=" + ex.Message;
                AxisInitializeResult failure = AxisInitializeResult.Failure(
                    -1,
                    currentStep,
                    laneName,
                    failureMessage);
                await ReportParallelLaneFailureAsync(
                    executionState,
                    failure).ConfigureAwait(false);
                return failure;
            }
        }

        /// <summary>
        /// 병렬 Lane의 첫 실패만 보존하고 취소·전체 축 정지·알람을 한 번만 처리합니다.
        /// </summary>
        private async Task ReportParallelLaneFailureAsync(
            ParallelLaneExecutionState executionState,
            AxisInitializeResult failure)
        {
            try
            {
                if (executionState == null || failure == null)
                    return;

                bool firstFailure = executionState.TrySetFailure(failure);
                executionState.Cancel();

                if (executionState.TryRequestAxisStop())
                {
                    int stopResult = await _runtime.StopAllAxesAsync().ConfigureAwait(false);
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                        "Parallel lane peer-stop requested. lane=" + failure.FailedLane +
                        ", step=" + failure.FailedStepNo +
                        ", stopResult=" + stopResult +
                        (stopResult == 0 ? " - Ok" : " - Failed"));
                }

                if (firstFailure)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                        failure.ErrorMessage + " - Failed");
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "INIT-PARALLEL-" +
                            (string.IsNullOrWhiteSpace(failure.FailedLane)
                                ? "LANE"
                                : failure.FailedLane.ToUpperInvariant()),
                        "MachineController",
                        failure.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                    "Parallel lane failure handling failed. error=" + ex.Message + " - Failed");
            }
        }

        private string ResolveParallelLaneFailureMessage(
            AxisInitializeResult stepResult,
            AxisInitializeStep step,
            string laneName)
        {
            try
            {
                string stepMessage = stepResult != null ? stepResult.ErrorMessage : "";
                if (string.IsNullOrWhiteSpace(stepMessage))
                    stepMessage = _runtime.GetLastFailureMessage();
                if (string.IsNullOrWhiteSpace(stepMessage))
                    stepMessage = "상세 실패 원인이 없습니다.";

                return "병렬 초기화 Lane 실패. lane=" + laneName +
                    ", step=" + (step != null ? step.StepNo : 0) +
                    ", group=" + (step != null ? step.GroupName : "-") +
                    ", result=" + (stepResult != null ? stepResult.ResultCode : -1) +
                    ", reason=" + stepMessage;
            }
            catch (Exception ex)
            {
                return "병렬 초기화 Lane 실패 원인 확인 중 예외가 발생했습니다. lane=" + laneName +
                    ", error=" + ex.Message;
            }
        }

        private AxisInitializeResult FailPreparation(
            AxisInitializeStep step,
            string message)
        {
            QMC.Common.Log.Write("Main", "SYSTEM", "InitializePreparation", message + " - Failed");
            AlarmManager.Raise(
                AlarmSeverity.Error,
                "INIT-PREP",
                "MachineController",
                message);
            return AxisInitializeResult.Failure(-1, step, string.Empty, message);
        }

        private void MarkLaneStepsCancelled(
            IList<AxisInitializeStep> orderedSteps,
            int startIndex,
            string message)
        {
            try
            {
                if (orderedSteps == null)
                    return;

                for (int i = Math.Max(0, startIndex); i < orderedSteps.Count; i++)
                {
                    _executor.RaiseStepProgress(
                        orderedSteps[i],
                        AxisInitializeStepStatus.ReinitializeRequired,
                        message);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                    "Cancelled lane progress update failed. error=" + ex.Message + " - Failed");
            }
        }

        /// <summary>
        /// 두 Lane이 공유하는 취소 토큰, 최초 실패 정보, 전체 축 정지 요청 상태입니다.
        /// </summary>
        private sealed class ParallelLaneExecutionState : IDisposable
        {
            private readonly object _failureLock = new object();
            private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
            private AxisInitializeResult _failure;
            private int _axisStopRequested;

            public CancellationToken Token
            {
                get { return _cancellation.Token; }
            }

            public bool TrySetFailure(AxisInitializeResult failure)
            {
                try
                {
                    if (failure == null)
                        return false;

                    lock (_failureLock)
                    {
                        if (_failure != null)
                            return false;

                        _failure = failure;
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            public AxisInitializeResult GetFailure()
            {
                try
                {
                    lock (_failureLock)
                    {
                        return _failure;
                    }
                }
                catch
                {
                    return null;
                }
            }

            public void Cancel()
            {
                try
                {
                    if (!_cancellation.IsCancellationRequested)
                        _cancellation.Cancel();
                }
                catch
                {
                }
            }

            public bool TryRequestAxisStop()
            {
                return Interlocked.CompareExchange(ref _axisStopRequested, 1, 0) == 0;
            }

            public void Dispose()
            {
                try
                {
                    _cancellation.Dispose();
                }
                catch
                {
                }
            }
        }
    }
}
