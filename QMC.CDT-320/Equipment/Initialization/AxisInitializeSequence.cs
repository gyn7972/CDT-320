using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Common.Motion;
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
        private const int CurrentDefaultVersion = 17;

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
            finally
            {
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
                "InputFeederY home after Lift Down.", "InputFeederY", "FeederY");
            AddFeederSafeInterlocks(plan, 180, "InputFeeder", true);
            AddAxisHomeDoneInterlocks(plan, 180, "InputFeeder", "FrontPickerY", "RearPickerY");
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
                "OutputFeederY home after Lift Down and Good Guide Down.", "OutputFeederY");
            AddFeederSafeInterlocks(plan, 260, "OutputFeeder", false);
            AddAxisHomeDoneInterlocks(plan, 260, "OutputFeeder",
                "FrontPickerY", "RearPickerY", "OutputGoodStageZ");
            // 기존 조건(사용자 승인으로 비활성): OutputFeeder HOME 전에 Lift Up(Fwd)을 요구했다.
            // AddStepInterlock(plan, 260, "OutputFeeder", AxisInitializeInterlockTarget.Cylinder,
            //     "OutputFeederLift", AxisInitializeInterlockState.Fwd,
            //     "OutputFeeder Lift를 Up 상태로 만든 후 다시 실행하십시오.");
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
            AssignParallelLane(plan, AxisInitializeParallelLane.Input,
                "InputFeederLift",
                "InputFeeder",
                "InputStageY",
                "InputStageYAvoid",
                "InputStageT",
                "InputCassette");

            AssignParallelLane(plan, AxisInitializeParallelLane.Output,
                "OutputGoodBinGuideDown",
                "OutputFeeder",
                "OutputStageZAvoid",
                "OutputFeederLiftDown",
                "OutputNGStageY",
                "OutputNGStageYAvoid",
                "OutputGoodStageY",
                "OutputGoodStageYAvoid",
                "OutputCassette");
        }

        #endregion

        private readonly AxisInitializeExecutor _executor;
        private readonly AxisInitializeRuntime _runtime;
        private readonly AxisInitializeRoutePlanner _routePlanner;

        public AxisInitializeSequence(
            AxisInitializeExecutor executor,
            AxisInitializeRuntime runtime)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _routePlanner = new AxisInitializeRoutePlanner(_runtime);
        }

        /// <summary>
        /// 현재 축·인터락 상태를 읽어 경로를 미리 표시합니다.
        /// 실제 순서를 변경하거나 HOME을 허가하는 기능은 없습니다.
        /// </summary>
        internal AxisInitializeRouteResult PreviewRoute(IList<AxisInitializeStep> steps)
        {
            return _routePlanner.Build(steps);
        }

        /// <summary>
        /// 전체 초기화 Plan을 실행합니다.
        /// 이 메서드가 전체 초기화 순서의 단일 진입점이며 Executor에는 전체 Plan 순서를 두지 않습니다.
        /// </summary>
        public async Task<AxisInitializeResult> ExecuteAsync(IList<AxisInitializeStep> steps)
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

                AxisInitializeResult axisRegistrationResult =
                    _executor.VerifyDeclaredStepAxes(enabledSteps);
                if (!axisRegistrationResult.Succeeded)
                    return axisRegistrationResult;

                // 실제 Step 실행 전 현재 상태를 기록합니다.
                // 미리보기와 무관하게 각 Step 직전 실행용 인터락을 다시 검사합니다.
                TryLogRoutePreview(enabledSteps);

                _executor.BeginRun(enabledSteps);
                runStarted = true;

                return await ExecuteSequenceFlowAsync(enabledSteps).ConfigureAwait(false);
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
                AxisInitializeRouteResult preview = PreviewRoute(steps);
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
            finally
            {
            }
        }

        /// <summary>
        /// 현재 승인된 전체 초기화 흐름입니다.
        /// 1) 공통 선행 구간, 2) Input/Output 병렬 Lane, 3) SharedRail 후행 구간 순서를 유지합니다.
        /// </summary>
        private async Task<AxisInitializeResult> ExecuteSequenceFlowAsync(
            IList<AxisInitializeStep> enabledSteps)
        {
            try
            {
                var inputLaneSteps = enabledSteps
                    .Where(x => x != null && AxisInitializeParallelLane.Is(
                        x.ParallelLane,
                        AxisInitializeParallelLane.Input))
                    .OrderBy(x => x.StepNo)
                    .ThenBy(x => x.GroupName)
                    .ToList();
                var outputLaneSteps = enabledSteps
                    .Where(x => x != null && AxisInitializeParallelLane.Is(
                        x.ParallelLane,
                        AxisInitializeParallelLane.Output))
                    .OrderBy(x => x.StepNo)
                    .ThenBy(x => x.GroupName)
                    .ToList();

                // Lane 메타데이터가 완전하지 않으면 기존 정책대로 전체 Step을 직렬 실행합니다.
                if (inputLaneSteps.Count == 0 || outputLaneSteps.Count == 0)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                        "Initialize parallel lane metadata is incomplete. Serial fallback selected. inputSteps=" +
                        inputLaneSteps.Count + ", outputSteps=" + outputLaneSteps.Count + " - Ok");
                    return await ExecuteStepBatchesSerialAsync(
                        enabledSteps,
                        "SerialFallback").ConfigureAwait(false);
                }

                int firstLaneStepNo = Math.Min(
                    inputLaneSteps.Min(x => x.StepNo),
                    outputLaneSteps.Min(x => x.StepNo));
                int lastLaneStepNo = Math.Max(
                    inputLaneSteps.Max(x => x.StepNo),
                    outputLaneSteps.Max(x => x.StepNo));
                var unlabeledStepsInsideLaneBarrier = enabledSteps
                    .Where(x => x != null &&
                        x.StepNo >= firstLaneStepNo &&
                        x.StepNo <= lastLaneStepNo &&
                        !AxisInitializeParallelLane.Is(x.ParallelLane, AxisInitializeParallelLane.Input) &&
                        !AxisInitializeParallelLane.Is(x.ParallelLane, AxisInitializeParallelLane.Output))
                    .ToList();
                if (unlabeledStepsInsideLaneBarrier.Count > 0)
                {
                    string message = "병렬 초기화 Lane 구간 안에 Lane이 지정되지 않은 Step이 있습니다. steps=" +
                        string.Join(",", unlabeledStepsInsideLaneBarrier.Select(x =>
                            x.StepNo + ":" + x.GroupName).ToArray());
                    return FailPreparation(unlabeledStepsInsideLaneBarrier[0], message);
                }

                var preLaneSteps = enabledSteps
                    .Where(x => x != null && x.StepNo < firstLaneStepNo)
                    .ToList();
                var postLaneSteps = enabledSteps
                    .Where(x => x != null && x.StepNo > lastLaneStepNo)
                    .ToList();

                // 공통 선행 Step이 모두 완료된 뒤에만 두 Lane을 시작합니다.
                AxisInitializeResult preResult = await ExecuteStepBatchesSerialAsync(
                    preLaneSteps,
                    "CommonPreLane").ConfigureAwait(false);
                if (!preResult.Succeeded)
                    return preResult;

                AxisInitializeResult parallelResult = await ExecuteParallelLanesAsync(
                    inputLaneSteps,
                    outputLaneSteps).ConfigureAwait(false);
                if (!parallelResult.Succeeded)
                    return parallelResult;

                // SharedRail Step은 두 Lane이 모두 끝난 다음 기존 StepNo 순서로 직렬 실행합니다.
                return await ExecuteStepBatchesSerialAsync(
                    postLaneSteps,
                    "SharedRailPostLane").ConfigureAwait(false);
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

        private async Task<AxisInitializeResult> ExecuteStepBatchesSerialAsync(
            IList<AxisInitializeStep> steps,
            string phase)
        {
            try
            {
                if (steps == null || steps.Count == 0)
                    return AxisInitializeResult.Success();

                foreach (var batch in steps
                    .Where(x => x != null && x.Enabled)
                    .OrderBy(x => x.StepNo)
                    .GroupBy(x => x.StepNo))
                {
                    var batchSteps = batch.OrderBy(x => x.GroupName).ToList();
                    if (batchSteps.Count == 1)
                    {
                        AxisInitializeResult singleResult = await _executor.ExecuteStepAsync(
                            batchSteps[0],
                            null,
                            string.Empty).ConfigureAwait(false);
                        if (!singleResult.Succeeded)
                            return singleResult;
                        continue;
                    }

                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                        "Axis initialize same-step serial batch start. phase=" + phase +
                        ", step=" + batch.Key +
                        ", groups=" + string.Join(",", batchSteps.Select(x => x.GroupName).ToArray()) + " - Start");
                    foreach (AxisInitializeStep batchStep in batchSteps)
                    {
                        AxisInitializeResult serialResult = await _executor.ExecuteStepAsync(
                            batchStep,
                            null,
                            string.Empty).ConfigureAwait(false);
                        if (!serialResult.Succeeded)
                            return serialResult;
                    }

                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                        "Axis initialize same-step serial batch completed. phase=" + phase +
                        ", step=" + batch.Key + " - Ok");
                }

                return AxisInitializeResult.Success();
            }
            catch (Exception ex)
            {
                string message = "직렬 초기화 구간 실행 실패: phase=" + phase +
                    ", error=" + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                    message + " - Failed");
                return AxisInitializeResult.Failure(-1, null, string.Empty, message);
            }
        }

        private async Task<AxisInitializeResult> ExecuteParallelLanesAsync(
            IList<AxisInitializeStep> inputLaneSteps,
            IList<AxisInitializeStep> outputLaneSteps)
        {
            ParallelLaneExecutionState executionState = null;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                HashSet<string> inputLaneAxisNames = _runtime.ResolveLaneAxisNames(inputLaneSteps);
                HashSet<string> outputLaneAxisNames = _runtime.ResolveLaneAxisNames(outputLaneSteps);
                var overlappingAxes = inputLaneAxisNames
                    .Intersect(outputLaneAxisNames, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (overlappingAxes.Count > 0)
                {
                    string message = "Input/Output 병렬 초기화 Lane에 중복 축이 있습니다. axes=" +
                        string.Join(",", overlappingAxes.ToArray());
                    return FailPreparation(null, message);
                }

                AxisInitializeStep firstInputStep = inputLaneSteps
                    .OrderBy(x => x.StepNo)
                    .ThenBy(x => x.GroupName)
                    .FirstOrDefault();
                AxisInitializeStep firstOutputStep = outputLaneSteps
                    .OrderBy(x => x.StepNo)
                    .ThenBy(x => x.GroupName)
                    .FirstOrDefault();
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
                    ", inputAxes=" + string.Join(",", inputLaneAxisNames.ToArray()) +
                    ", outputAxes=" + string.Join(",", outputLaneAxisNames.ToArray()) + " - Start");

                Task<AxisInitializeResult> inputTask = Task.Run(() => ExecuteLaneAsync(
                    AxisInitializeParallelLane.Input,
                    inputLaneSteps,
                    outputLaneAxisNames,
                    executionState));
                Task<AxisInitializeResult> outputTask = Task.Run(() => ExecuteLaneAsync(
                    AxisInitializeParallelLane.Output,
                    outputLaneSteps,
                    inputLaneAxisNames,
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

        private async Task<AxisInitializeResult> ExecuteLaneAsync(
            string laneName,
            IList<AxisInitializeStep> laneSteps,
            ISet<string> allowedConcurrentAxisNames,
            ParallelLaneExecutionState executionState)
        {
            AxisInitializeStep currentStep = null;
            try
            {
                var orderedSteps = (laneSteps ?? new AxisInitializeStep[0])
                    .Where(x => x != null && x.Enabled)
                    .OrderBy(x => x.StepNo)
                    .ThenBy(x => x.GroupName)
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

                    AxisInitializeResult stepResult = await _executor.ExecuteStepAsync(
                        currentStep,
                        allowedConcurrentAxisNames,
                        laneName).ConfigureAwait(false);
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
