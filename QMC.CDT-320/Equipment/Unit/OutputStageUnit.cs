using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using QMC.Common;
using QMC.Common.Motion;
using QMC.Common.IO;
using QMC.CDT320.Ajin;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT320
{
    // ==========================================================================

    /// <summary>출력 스테이지에 배출할 다이 등급입니다.</summary>
    public enum DieGrade
    {
        /// <summary>양품 다이입니다.</summary>
        Good,

        /// <summary>불량 다이입니다.</summary>
        Ng
    }

    /// <summary>TPU에서 출력 스테이지로 다이를 넘길 때 사용하는 요청 정보입니다.</summary>
    public class ReceiveDieRequest
    {
        /// <summary>다이 등급입니다.</summary>
        public DieGrade Grade { get; set; }

        /// <summary>TPU 기준 X 보정값입니다.</summary>
        public double TpuOffsetX { get; set; }

        /// <summary>TPU 기준 Y 보정값입니다.</summary>
        public double TpuOffsetY { get; set; }

        /// <summary>비전 기준 X 보정값입니다.</summary>
        public double VisionOffsetX { get; set; }

        /// <summary>비전 기준 Y 보정값입니다.</summary>
        public double VisionOffsetY { get; set; }
    }

    // --------------------------------------------------------------------------

    /// <summary>TPU와 출력 스테이지 사이의 수동/시퀀스 연동 인터페이스입니다.</summary>
    public interface ITpuUnit
    {
        /// <summary>출력 스테이지가 Place 받을 준비가 되었음을 TPU에 알립니다.</summary>
        void NotifyPlaceReady();

        /// <summary>TPU Place 완료를 대기합니다.</summary>
        /// <param name="timeoutMs">대기 시간입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        Task<bool> WaitPlaceDoneAsync(int timeoutMs = 3000);

        /// <summary>다음 다이를 받을 준비가 되었음을 TPU에 알립니다.</summary>
        void NotifyReadyForNextDie();

        /// <summary>콜렛 클리닝을 TPU에 요청합니다.</summary>
        /// <param name="timeoutMs">대기 시간입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        Task<bool> RequestColletCleaningAsync(int timeoutMs = 10000);
    }

    /// <summary>출력 카세트/피더로 bin 교체를 요청하는 인터페이스입니다.</summary>
    public interface IOutputUnloaderUnit
    {
        /// <summary>지정 등급 카세트로 bin 교체를 요청합니다.</summary>
        /// <param name="grade">다이 등급입니다.</param>
        /// <param name="timeoutMs">대기 시간입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        Task<bool> RequestWaferChangeAsync(DieGrade grade, int timeoutMs = 0);
    }

    // ==========================================================================
    /// <summary>개별 StageModule 설정 데이터입니다.</summary>
    public class StageModuleSetup : ISetupData
    {
        
    }

    /// <summary>개별 StageModule 구성 데이터입니다.</summary>
    public class StageModuleConfig : IConfigData
    {
      
    }

    /// <summary>개별 StageModule 위치 레시피입니다.</summary>
    public class StageModuleRecipe : IRecipeData
    {
        public double WorkPositionZ { get; set; } = 10.0;
        public double AvoidPositionZ { get; set; } = 0.0;
        public double UnloadPositionY { get; set; } = -50.0;
        public double HomePositionY { get; set; } = 0.0;
        public double CleaningPositionY { get; set; } = 80.0;
    }

    // --------------------------------------------------------------------------

    /// <summary>출력 스테이지 설정 데이터입니다.</summary>
    public class OutputStageSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }
    }

    /// <summary>출력 스테이지 구성 데이터입니다.</summary>
    public class OutputStageConfig : IConfigData
    {
        [DataMember] public bool bDryRun { get; set; }

        public bool IsSimulationMode
        {
            get { return bDryRun; }
            set { bDryRun = value; }
        }
        /// <summary>콜렛 클리닝 대기 시간입니다.</summary>
        [DataMember] public int ColletCleaningTimeoutMs { get; set; } = 10000;
    }

    /// <summary>출력 스테이지 축별 위치 레시피입니다.</summary>
    public class OutputStageRecipe : IRecipeData
    {
        [DataMember] public StageAxisPositions GoodStageY { get; set; } = new StageAxisPositions();
        [DataMember] public StageAxisPositions GoodStageZ { get; set; } = new StageAxisPositions();
        [DataMember] public StageAxisPositions NGStageY { get; set; } = new StageAxisPositions();
        [DataMember] public StageAxisPositions VisionX { get; set; } = new StageAxisPositions();

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsurePositionObjects();
        }

        public void EnsurePositionObjects()
        {
            if (GoodStageY == null) GoodStageY = new StageAxisPositions();
            if (GoodStageZ == null) GoodStageZ = new StageAxisPositions();
            if (NGStageY == null) NGStageY = new StageAxisPositions();
            if (VisionX == null) VisionX = new StageAxisPositions();
        }
    }

    // ==========================================================================
    // ==========================================================================
    /// <summary>GOOD/NG 개별 스테이지 모듈입니다.</summary>
    public class StageModule : BaseUnit<StageModuleSetup, StageModuleConfig, StageModuleRecipe>
    {
        // ----------------------------------------------------------------------
        // ----------------------------------------------------------------------
        public BaseAxis StageY { get; private set; }
        public BaseAxis StageZ { get; private set; }
        public bool HasStageZ { get; private set; }

        // ----------------------------------------------------------------------
        /// <summary>StageZ가 있는 스테이지 모듈을 생성합니다.</summary>
        /// <param name="moduleName">모듈 이름입니다.</param>
        public StageModule(string moduleName) : this(moduleName, true)
        {
        }

        public StageModule(string moduleName, bool hasStageZ) : base(moduleName)
        {
            HasStageZ = hasStageZ;
            StageY = AjinFactory.CreateAxis(moduleName + "_StageY");
            Components.Add(StageY);

            if (HasStageZ)
            {
                StageZ = AjinFactory.CreateAxis(moduleName + "_StageZ");
                Components.Add(StageZ);
            }
        }

        // ----------------------------------------------------------------------

        /// <summary>StageZ가 Avoid 위치에 있는지 확인합니다.</summary>
        /// <returns>처리 결과입니다.</returns>
        public bool IsAtAvoidPosition()
        {
            if (!HasStageZ || StageZ == null)
                return true;

            double tolerance = StageZ.Config != null ? StageZ.Config.InPositionTolerance : 0.01;
            double diff = Math.Abs(StageZ.ActualPosition - Recipe.AvoidPositionZ);
            return diff <= tolerance;
        }

        /// <summary>StageZ를 Avoid 위치로 이동합니다.</summary>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> MoveToAvoidPositionAsync()
        {
            if (!HasStageZ || StageZ == null)
                return true;

            if (IsAtAvoidPosition())
                return true;

            await StageZ.MoveAbsoluteAsync(Recipe.AvoidPositionZ, ResolveAxisVelocity(StageZ));

            if (StageZ.IsAlarm)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> MoveToAvoidPosition: StageZ 하강 실패.");
                return false;
            }

            AxisMoveWaitResult waitResult = await WaitAxisMoveDoneInPositionAsync(StageZ, Recipe.AvoidPositionZ).ConfigureAwait(false);
            if (!waitResult.Success)
            {
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    AxisMoveWaiter.ResolveAlarmCode("OS-AVOIDZ", waitResult),
                    source: Name + ".MoveToAvoidPositionAsync",
                    message: "StageZ avoid position wait failed. " + AxisMoveWaiter.FormatResult(waitResult, StageZ.Name));
                return false;
            }

            return true;
        }

        /// <summary>StageZ를 Work 위치로 이동합니다.</summary>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> MoveToWorkPositionAsync()
        {
            if (!HasStageZ || StageZ == null)
                return true;

            await StageZ.MoveAbsoluteAsync(Recipe.WorkPositionZ, ResolveAxisVelocity(StageZ));

            if (StageZ.IsAlarm)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> MoveToWorkPosition: StageZ 상승 실패.");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "OS-WORKZ",
                    source: Name + ".MoveToWorkPositionAsync",
                    message: "StageZ 작업 위치 이동 실패 (axis code=" + StageZ.AlarmCode + ")");
                return false;
            }

            AxisMoveWaitResult waitResult = await WaitAxisMoveDoneInPositionAsync(StageZ, Recipe.WorkPositionZ).ConfigureAwait(false);
            if (!waitResult.Success)
            {
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    AxisMoveWaiter.ResolveAlarmCode("OS-WORKZ", waitResult),
                    source: Name + ".MoveToWorkPositionAsync",
                    message: "StageZ work position wait failed. " + AxisMoveWaiter.FormatResult(waitResult, StageZ.Name));
                return false;
            }

            return true;
        }

        /// <summary>StageY를 지정 위치로 이동합니다.</summary>
        /// <param name="targetY">목표 Y 위치입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> MoveYAsync(double targetY)
        {
            await StageY.MoveAbsoluteAsync(targetY, ResolveAxisVelocity(StageY));

            if (StageY.IsAlarm)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> MoveY: StageY 이동 실패. " +
                    "Target=" + targetY + "mm");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "OS-MOVEY",
                    source: Name + ".MoveYAsync",
                    message: "StageY 이동 실패 (Target=" + targetY.ToString("F3") +
                             "mm, axis code=" + StageY.AlarmCode + ")");
                return false;
            }

            AxisMoveWaitResult waitResult = await WaitAxisMoveDoneInPositionAsync(StageY, targetY).ConfigureAwait(false);
            if (!waitResult.Success)
            {
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    AxisMoveWaiter.ResolveAlarmCode("OS-MOVEY", waitResult),
                    source: Name + ".MoveYAsync",
                    message: "StageY move wait failed. target=" + targetY.ToString("F3") + ". " +
                             AxisMoveWaiter.FormatResult(waitResult, StageY.Name));
                return false;
            }

            return true;
        }

        /// <summary>StageY를 Home 위치로 이동합니다.</summary>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> MoveToHomeAsync()
        {
            return await MoveYAsync(Recipe.HomePositionY);
        }

        private double ResolveAxisVelocity(BaseAxis axis)
        {
            if (axis != null && axis.Config != null && axis.Config.DefaultVelocity > 0.0)
                return axis.Config.DefaultVelocity;

            return 100.0;
        }

        private static int ResolveAxisMoveTimeout(BaseAxis axis)
        {
            return axis != null && axis.Setup != null && axis.Setup.MoveTimeoutMs > 0
                ? axis.Setup.MoveTimeoutMs
                : 10000;
        }

        private static double ResolveAxisInPositionTolerance(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;
        }

        private static Task<AxisMoveWaitResult> WaitAxisMoveDoneInPositionAsync(BaseAxis axis, double target)
        {
            return AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                axis,
                target,
                ResolveAxisInPositionTolerance(axis),
                ResolveAxisMoveTimeout(axis),
                0);
        }
    }

    // ==========================================================================
    // ==========================================================================

    /// <summary>GOOD/NG 출력 스테이지와 BinCamera 축을 관리하는 유닛입니다.</summary>
    public class OutputStageUnit : BaseUnit<OutputStageSetup, OutputStageConfig, OutputStageRecipe>, IUnitJogController
    {
        // ----------------------------------------------------------------------
        // ----------------------------------------------------------------------

        public StageModule GoodStage { get; private set; }

        public StageModule NgStage { get; private set; }

        public BaseAxis OutputCameraX { get; private set; }

        // ----------------------------------------------------------------------
        // Bin Guide/Clamp 센서 (DI) — Good/Ng

        public BaseDigitalInput NgBinGuideUpSensor { get; private set; }
        public BaseDigitalInput NgBinGuideDownSensor { get; private set; }
        public BaseDigitalInput NgBinClampUpSensor { get; private set; }
        public BaseDigitalInput NgBinUnclampSensor { get; private set; }
        public BaseDigitalInput NgBinRingSensor { get; private set; }
        public BaseDigitalInput GoodBinGuideUpSensor { get; private set; }
        public BaseDigitalInput GoodBinGuideDownSensor { get; private set; }
        public BaseDigitalInput GoodBinClampUpSensor { get; private set; }
        public BaseDigitalInput GoodBinUnclampSensor { get; private set; }
        public BaseDigitalInput GoodBinRingSensor { get; private set; }

        // Bin Guide/Clamp 출력 (DO) — Good/Ng + Bottom Vision Blow

        public BaseDigitalOutput NgBinGuideUpOut { get; private set; }
        public BaseDigitalOutput NgBinGuideDownOut { get; private set; }
        public BaseDigitalOutput NgBinClampUpOut { get; private set; }
        public BaseDigitalOutput NgBinClampDownOut { get; private set; }
        public BaseDigitalOutput NgBinClampOut { get; private set; }
        public BaseDigitalOutput NgBinUnclampOut { get; private set; }
        public BaseDigitalOutput GoodBinGuideUpOut { get; private set; }
        public BaseDigitalOutput GoodBinGuideDownOut { get; private set; }
        public BaseDigitalOutput GoodBinClampUpOut { get; private set; }
        public BaseDigitalOutput GoodBinClampDownOut { get; private set; }
        public BaseDigitalOutput GoodBinClampOut { get; private set; }
        public BaseDigitalOutput GoodBinUnclampOut { get; private set; }
        public BaseDigitalOutput BottomVisionBlowOnOut { get; private set; }
        public BaseDigitalOutput BottomVisionBlowOffOut { get; private set; }

        // ----------------------------------------------------------------------

        /// <summary>TPU 연동 인터페이스입니다.</summary>
        public ITpuUnit Tpu { get; private set; }

        /// <summary>출력 Unloader 연동 인터페이스입니다.</summary>
        public IOutputUnloaderUnit Unloader { get; private set; }

        // ----------------------------------------------------------------------

        /// <summary>출력 스테이지 유닛을 생성하고 GOOD/NG 스테이지와 I/O를 등록합니다.</summary>
        /// <param name="tpu">TPU 연동 인터페이스입니다.</param>
        /// <param name="unloader">출력 Unloader 연동 인터페이스입니다.</param>
        public OutputStageUnit(ITpuUnit tpu, IOutputUnloaderUnit unloader)
            : base("OutputStageUnit")
        {
            if (tpu      == null) throw new ArgumentNullException("tpu");
            if (unloader == null) throw new ArgumentNullException("unloader");

            Tpu      = tpu;
            Unloader = unloader;

            GoodStage  = new StageModule("GoodStage", true);
            NgStage    = new StageModule("NgStage", false);
            OutputCameraX = AjinFactory.CreateAxis("OutputVisionX");

            // Bin Guide/Clamp 센서 (DI)
            NgBinGuideUpSensor     = RegisterCylinderInput("NGBinGuideLift", true, "NgBinGuideUp");
            NgBinGuideDownSensor   = RegisterCylinderInput("NGBinGuideLift", false, "NgBinGuideDown");
            NgBinClampUpSensor     = RegisterCylinderInput("NGBinGuideClampLift", true, "NgBinClampUp");
            NgBinUnclampSensor     = RegisterCylinderInput("NGBinGuideClamp", false, "NgBinUnclamp");
            NgBinRingSensor        = RegisterInput("NgBinRing");
            GoodBinGuideUpSensor   = RegisterCylinderInput("GoodBinGuideLift", true, "GoodBinGuideUp");
            GoodBinGuideDownSensor = RegisterCylinderInput("GoodBinGuideLift", false, "GoodBinGuideDown");
            GoodBinClampUpSensor   = RegisterCylinderInput("GoodBinGuideClampLift", true, "GoodBinClampUp");
            GoodBinUnclampSensor   = RegisterCylinderInput("GoodBinGuideClamp", false, "GoodBinUnclamp");
            GoodBinRingSensor      = RegisterInput("GoodBinRing");

            // Bin Guide/Clamp 출력 (DO)
            NgBinGuideUpOut     = RegisterCylinderOutput("NGBinGuideLift", true, "NgBinGuideUp");
            NgBinGuideDownOut   = RegisterCylinderOutput("NGBinGuideLift", false, "NgBinGuideDown");
            NgBinClampUpOut     = RegisterCylinderOutput("NGBinGuideClampLift", true, "NgBinClampUp");
            NgBinClampDownOut   = RegisterCylinderOutput("NGBinGuideClampLift", false, "NgBinClampDown");
            NgBinClampOut       = RegisterCylinderOutput("NGBinGuideClamp", true, "NgBinClamp");
            NgBinUnclampOut     = RegisterCylinderOutput("NGBinGuideClamp", false, "NgBinUnclamp");
            GoodBinGuideUpOut   = RegisterCylinderOutput("GoodBinGuideLift", true, "GoodBinGuideUp");
            GoodBinGuideDownOut = RegisterCylinderOutput("GoodBinGuideLift", false, "GoodBinGuideDown");
            GoodBinClampUpOut   = RegisterCylinderOutput("GoodBinGuideClampLift", true, "GoodBinClampUp");
            GoodBinClampDownOut = RegisterCylinderOutput("GoodBinGuideClampLift", false, "GoodBinClampDown");
            GoodBinClampOut     = RegisterCylinderOutput("GoodBinGuideClamp", true, "GoodBinClamp");
            GoodBinUnclampOut   = RegisterCylinderOutput("GoodBinGuideClamp", false, "GoodBinUnclamp");
            BottomVisionBlowOnOut  = RegisterOutput("BottomVisionBlow");
            BottomVisionBlowOffOut = RegisterOutput("BottomVisionBlowOff");

            Components.Add(GoodStage);
            Components.Add(NgStage);
            Components.Add(OutputCameraX);
        }

        private BaseDigitalInput RegisterInput(string catalogName)
        {
            DioDefault catalog = AjinIoCatalog.FindInput(catalogName);
            BaseDigitalInput item = catalog != null
                ? AjinFactory.CreateDigitalInput(catalog)
                : CreateMissingInput(catalogName, catalogName);

            Components.Add(item);
            return item;
        }

        private BaseDigitalOutput RegisterOutput(string catalogName)
        {
            DioDefault catalog = AjinIoCatalog.FindOutput(catalogName);
            BaseDigitalOutput item = catalog != null
                ? AjinFactory.CreateDigitalOutput(catalog)
                : CreateMissingOutput(catalogName, catalogName);

            Components.Add(item);
            return item;
        }

        public void ApplyCylinderIoMappings()
        {
            try
            {
                NgBinGuideUpSensor = RebindInput(NgBinGuideUpSensor, RegisterCylinderInput("NGBinGuideLift", true, "NgBinGuideUp"));
                NgBinGuideDownSensor = RebindInput(NgBinGuideDownSensor, RegisterCylinderInput("NGBinGuideLift", false, "NgBinGuideDown"));
                NgBinClampUpSensor = RebindInput(NgBinClampUpSensor, RegisterCylinderInput("NGBinGuideClampLift", true, "NgBinClampUp"));
                NgBinUnclampSensor = RebindInput(NgBinUnclampSensor, RegisterCylinderInput("NGBinGuideClamp", false, "NgBinUnclamp"));
                GoodBinGuideUpSensor = RebindInput(GoodBinGuideUpSensor, RegisterCylinderInput("GoodBinGuideLift", true, "GoodBinGuideUp"));
                GoodBinGuideDownSensor = RebindInput(GoodBinGuideDownSensor, RegisterCylinderInput("GoodBinGuideLift", false, "GoodBinGuideDown"));
                GoodBinClampUpSensor = RebindInput(GoodBinClampUpSensor, RegisterCylinderInput("GoodBinGuideClampLift", true, "GoodBinClampUp"));
                GoodBinUnclampSensor = RebindInput(GoodBinUnclampSensor, RegisterCylinderInput("GoodBinGuideClamp", false, "GoodBinUnclamp"));

                NgBinGuideUpOut = RebindOutput(NgBinGuideUpOut, RegisterCylinderOutput("NGBinGuideLift", true, "NgBinGuideUp"));
                NgBinGuideDownOut = RebindOutput(NgBinGuideDownOut, RegisterCylinderOutput("NGBinGuideLift", false, "NgBinGuideDown"));
                NgBinClampUpOut = RebindOutput(NgBinClampUpOut, RegisterCylinderOutput("NGBinGuideClampLift", true, "NgBinClampUp"));
                NgBinClampDownOut = RebindOutput(NgBinClampDownOut, RegisterCylinderOutput("NGBinGuideClampLift", false, "NgBinClampDown"));
                NgBinClampOut = RebindOutput(NgBinClampOut, RegisterCylinderOutput("NGBinGuideClamp", true, "NgBinClamp"));
                NgBinUnclampOut = RebindOutput(NgBinUnclampOut, RegisterCylinderOutput("NGBinGuideClamp", false, "NgBinUnclamp"));
                GoodBinGuideUpOut = RebindOutput(GoodBinGuideUpOut, RegisterCylinderOutput("GoodBinGuideLift", true, "GoodBinGuideUp"));
                GoodBinGuideDownOut = RebindOutput(GoodBinGuideDownOut, RegisterCylinderOutput("GoodBinGuideLift", false, "GoodBinGuideDown"));
                GoodBinClampUpOut = RebindOutput(GoodBinClampUpOut, RegisterCylinderOutput("GoodBinGuideClampLift", true, "GoodBinClampUp"));
                GoodBinClampDownOut = RebindOutput(GoodBinClampDownOut, RegisterCylinderOutput("GoodBinGuideClampLift", false, "GoodBinClampDown"));
                GoodBinClampOut = RebindOutput(GoodBinClampOut, RegisterCylinderOutput("GoodBinGuideClamp", true, "GoodBinClamp"));
                GoodBinUnclampOut = RebindOutput(GoodBinUnclampOut, RegisterCylinderOutput("GoodBinGuideClamp", false, "GoodBinUnclamp"));
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "OS-CYL-IO", Name,
                    "OutputStage cylinder I/O mapping apply failed: " + ex.Message);
                throw;
            }
        }

        private BaseDigitalInput RebindInput(BaseDigitalInput target, BaseDigitalInput replacement)
        {
            if (replacement == null)
                return target;

            if (target != null)
                Components.Remove(target);

            if (!Components.Contains(replacement))
                Components.Add(replacement);

            return replacement;
        }

        private BaseDigitalOutput RebindOutput(BaseDigitalOutput target, BaseDigitalOutput replacement)
        {
            if (replacement == null)
                return target;

            if (target != null)
                Components.Remove(target);

            if (!Components.Contains(replacement))
                Components.Add(replacement);

            return replacement;
        }

        private BaseDigitalInput RegisterCylinderInput(string cylinderName, bool fwd, string fallbackCatalogName)
        {
            BaseDigitalInput item = ResolveCylinderRuntimeInput(cylinderName, fwd);
            DioMap map = ResolveCylinderInputMap(cylinderName, fwd);

            if (item == null)
            {
                if (map != null)
                {
                    item = AjinFactory.CreateSharedDigitalInput(
                        cylinderName + (fwd ? "_InFwd" : "_InBwd"),
                        map,
                        !AjinFactory.IsRealBoardReady);
                }
                else
                {
                    DioDefault catalog = AjinIoCatalog.FindInput(fallbackCatalogName);
                    item = catalog != null
                        ? AjinFactory.CreateDigitalInput(catalog)
                        : CreateMissingInput(cylinderName + (fwd ? "_InFwd" : "_InBwd"), fallbackCatalogName);
                }
            }

            if (item == null)
                item = CreateMissingInput(cylinderName + (fwd ? "_InFwd" : "_InBwd"), fallbackCatalogName);

            if (item != null)
                Components.Add(item);

            LogCylinderIoBinding(cylinderName, fwd ? "InFwd" : "InBwd", map, false, fallbackCatalogName);
            return item;
        }

        private BaseDigitalOutput RegisterCylinderOutput(string cylinderName, bool fwd, string fallbackCatalogName)
        {
            DioMap map = ResolveCylinderOutputMap(cylinderName, fwd);
            BaseDigitalOutput item;
            if (map != null)
            {
                item = AjinFactory.CreateSharedDigitalOutput(
                    cylinderName + (fwd ? "_OutFwd" : "_OutBwd"),
                    map,
                    !AjinFactory.IsRealBoardReady);
            }
            else
            {
                DioDefault catalog = AjinIoCatalog.FindOutput(fallbackCatalogName);
                item = catalog != null
                    ? AjinFactory.CreateDigitalOutput(catalog)
                    : CreateMissingOutput(cylinderName + (fwd ? "_OutFwd" : "_OutBwd"), fallbackCatalogName);
            }

            if (item == null)
                item = CreateMissingOutput(cylinderName + (fwd ? "_OutFwd" : "_OutBwd"), fallbackCatalogName);

            Components.Add(item);
            LogCylinderIoBinding(cylinderName, fwd ? "OutFwd" : "OutBwd", map, true, fallbackCatalogName);
            return item;
        }

        private static BaseDigitalInput CreateMissingInput(string signalName, string fallbackCatalogName)
        {
            string name = string.IsNullOrWhiteSpace(fallbackCatalogName)
                ? signalName
                : fallbackCatalogName;
            BaseDigitalInput item = new SimDigitalInput(name);
            item.Setup.ModuleNo = -1;
            item.Setup.BitNo = -1;
            item.Config.IsSimulationMode = true;

            EventLogger.Write(EventKind.Warning, "QMC", "OS-CYL-IO",
                "OutputStage input signal is not mapped. Safe simulation input created. signal=" + signalName + ", fallback=" + fallbackCatalogName);
            return item;
        }

        private static BaseDigitalOutput CreateMissingOutput(string signalName, string fallbackCatalogName)
        {
            string name = string.IsNullOrWhiteSpace(fallbackCatalogName)
                ? signalName
                : fallbackCatalogName;
            BaseDigitalOutput item = new SimDigitalOutput(name);
            item.Setup.ModuleNo = -1;
            item.Setup.BitNo = -1;
            item.Config.IsSimulationMode = true;

            EventLogger.Write(EventKind.Warning, "QMC", "OS-CYL-IO",
                "OutputStage output signal is not mapped. Safe simulation output created. signal=" + signalName + ", fallback=" + fallbackCatalogName);
            return item;
        }

        private static BaseDigitalInput ResolveCylinderRuntimeInput(string cylinderName, bool fwd)
        {
            try
            {
                BaseCylinder cylinder = CylinderManager.Get(cylinderName);
                if (cylinder == null)
                    return null;

                BaseDigitalInput input = fwd ? cylinder.InFwd : cylinder.InBwd;
                if (input == null || input.Setup == null)
                    return null;

                if (input.Setup.ModuleNo < 0 || input.Setup.BitNo < 0)
                    return null;

                return input;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private static DioMap ResolveCylinderInputMap(string cylinderName, bool fwd)
        {
            CylMap map;
            if (AjinConfigStore.Current.Cylinders.TryGetValue(cylinderName, out map) && map != null)
                return fwd
                    ? map.UseFwdInput ? map.InFwd : null
                    : map.UseBwdInput ? map.InBwd : null;

            CylinderDefault fallback = AjinIoCatalog.FindCylinder(cylinderName);
            DioDefault item = fallback == null ? null : fwd ? fallback.InFwd : fallback.InBwd;
            return ToDioMap(item);
        }

        private static DioMap ResolveCylinderOutputMap(string cylinderName, bool fwd)
        {
            CylMap map;
            if (AjinConfigStore.Current.Cylinders.TryGetValue(cylinderName, out map) && map != null)
                return fwd ? map.OutFwd : map.OutBwd;

            CylinderDefault fallback = AjinIoCatalog.FindCylinder(cylinderName);
            DioDefault item = fallback == null ? null : fwd ? fallback.OutFwd : fallback.OutBwd;
            return ToDioMap(item);
        }

        private static DioMap ToDioMap(DioDefault item)
        {
            if (item == null)
                return null;

            return new DioMap
            {
                No = item.No,
                Address = item.Address,
                Module = item.Module,
                Bit = item.Bit,
                Nc = item.Nc
            };
        }

        private static void LogCylinderIoBinding(
            string cylinderName,
            string direction,
            DioMap map,
            bool output,
            string fallbackCatalogName)
        {
            string address = map == null
                ? "fallback:" + fallbackCatalogName
                : output
                    ? AjinIoCatalog.OutputAddress(map.Module, map.Bit)
                    : AjinIoCatalog.InputAddress(map.Module, map.Bit);
            EventLogger.Write(EventKind.Event, "QMC", "OS-CYL-IO",
                cylinderName + "." + direction + " bound to " + address);
        }

        public bool CanHandleJogAxis(BaseAxis axis)
        {
            BinStageAxis stageAxis;
            return TryResolveStageAxis(axis, out stageAxis);
        }

        public async Task<int> JogStepAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed,
            double axisStepDistance)
        {
            if (!CanHandleJogAxis(axis))
                return -1;

            double signedDistance = (direction < 0 ? -1.0 : 1.0) * Math.Abs(axisStepDistance);
            double target = axis.ActualPosition + signedDistance;

            BinStageAxis stageAxis;
            if (TryResolveStageAxis(axis, out stageAxis))
                return await MoveStageAxis(stageAxis, target, speedType == JogSpeedType.Fine);

            return -1;
        }

        public Task<int> JogContinuousAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed)
        {
            if (!CanHandleJogAxis(axis))
                return Task.FromResult(-1);

            double speed = UnitJogVelocityResolver.Resolve(axis, speedType, customSpeed);
            SharedRailXMotionRuntime.MoveJogContinuous(axis, direction, speed);
            return Task.FromResult(0);
        }

        public Task<int> StopJogAsync(BaseAxis axis)
        {
            if (!CanHandleJogAxis(axis))
                return Task.FromResult(-1);

            axis.StopJog();
            return Task.FromResult(0);
        }

        public async Task<int> MoveStageAxis(BinStageAxis axis, double targetPos, bool bFine = false)
        {
            try
            {
                if (!HasStageAxis(axis))
                    return 0;

                BaseAxis item = ResolveStageAxis(axis);
                double velocity = ResolveStageAxisVelocity(item, bFine);
                EventLogger.Write(EventKind.Event, "QMC", "OS-MOVE", axis + " target=" + targetPos);
                int result = await SharedRailXMotionRuntime.MoveAxisAsync(item, targetPos, velocity);
                if (result != 0 || item.IsAlarm)
                    return RaiseOutputStageAlarm(
                        "OS-MOVE",
                        axis + " 이동 실패. result=" + result +
                        ", alarm=" + item.IsAlarm +
                        FormatStageAxisLastMotionFailure(item));

                AxisMoveWaitResult waitResult = await WaitStageAxisMoveDoneInPosition(
                    axis,
                    targetPos,
                    item.Setup != null && item.Setup.MoveTimeoutMs > 0 ? item.Setup.MoveTimeoutMs : 10000).ConfigureAwait(false);
                if (!waitResult.Success)
                    return RaiseOutputStageAlarm(
                        AxisMoveWaiter.ResolveAlarmCode("OS-MOVE", waitResult),
                        axis + " move/in-position wait failed. target=" + targetPos + ". " +
                        AxisMoveWaiter.FormatResult(waitResult, axis.ToString()));

                return 0;
            }
            catch (Exception ex)
            {
                return RaiseOutputStageAlarm("OS-MOVE-EX", axis + " 이동 중 예외가 발생했습니다. " + ex.Message);
            }
        }

        private static string FormatStageAxisLastMotionFailure(BaseAxis axis)
        {
            if (axis == null ||
                axis.LastMotionFailureCode == 0 ||
                string.IsNullOrWhiteSpace(axis.LastMotionFailureMessage))
                return string.Empty;

            return ", 마지막 이동 실패 원인=" + axis.LastMotionFailureMessage;
        }

        public Task<int> MoveStageAxisToTeachingPosition(BinStageAxis axis, string positionName, bool bFine = false)
        {
            return MoveStageAxis(axis, GetStageTeachingPosition(axis, positionName), bFine);
        }

        public async Task<int> MoveToStageAvoidPosition(bool bFine = false)
        {
            Recipe.EnsurePositionObjects();
            int result = await MoveStageAxis(BinStageAxis.GoodBinY, Recipe.GoodStageY.AvoidPosition, bFine);
            if (result != 0) return result;
            result = await MoveStageAxis(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.AvoidPosition, bFine);
            if (result != 0) return result;
            result = await MoveStageAxis(BinStageAxis.NgBinY, Recipe.NGStageY.AvoidPosition, bFine);
            if (result != 0) return result;
            return await MoveStageAxis(BinStageAxis.VisionX, Recipe.VisionX.AvoidPosition, bFine);
        }

        public async Task<int> MoveToStageLoadPosition(BinSide side, bool bFine = false)
        {
            Recipe.EnsurePositionObjects();
            if (side == BinSide.Ng)
            {
                return await MoveStageAxis(BinStageAxis.NgBinY, Recipe.NGStageY.LoadPosition, bFine);
            }

            int move = await MoveStageAxis(BinStageAxis.GoodBinY, Recipe.GoodStageY.LoadPosition, bFine);
            if (move != 0) return move;
            return await MoveStageAxis(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.LoadPosition, bFine);
        }

        public async Task<int> MoveToStageUnloadPosition(BinSide side, bool bFine = false)
        {
            Recipe.EnsurePositionObjects();
            if (side == BinSide.Ng)
                return await MoveStageAxis(BinStageAxis.NgBinY, Recipe.NGStageY.UnloadPosition, bFine);

            int move = await MoveStageAxis(BinStageAxis.GoodBinY, Recipe.GoodStageY.UnloadPosition, bFine);
            if (move != 0) return move;
            return await MoveStageAxis(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.UnloadPosition, bFine);
        }

        public async Task<int> MoveToStageProcessPosition(BinSide side, bool bFine = false)
        {
            Recipe.EnsurePositionObjects();
            int result;
            if (side == BinSide.Ng)
                result = await MoveStageAxis(BinStageAxis.NgBinY, Recipe.NGStageY.ProcessPosition, bFine);
            else
            {
                result = await MoveStageAxis(BinStageAxis.GoodBinY, Recipe.GoodStageY.ProcessPosition, bFine);
                if (result == 0)
                {
                    result = await MoveStageAxis(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.ProcessPosition, bFine);
                }
            }

            if (result != 0) return result;
            return await MoveStageAxis(BinStageAxis.VisionX, Recipe.VisionX.ProcessPosition, bFine);
        }

        public bool IsStageAxisInPosition(BinStageAxis axis, double targetPos, double tolerance)
        {
            if (!HasStageAxis(axis))
                return true;

            return Math.Abs(ResolveStageAxis(axis).ActualPosition - targetPos) <= tolerance;
        }

        /// <summary>
        /// OutputVisionX(OutputCameraX)가 Avoid 위치에 있는지 확인합니다.<br/>
        /// OutputFeederY 등 Shared Rail 관련 HOME 전 간섭 확인에 사용합니다.
        /// </summary>
        public bool IsVisionXInAvoidPosition()
        {
            if (OutputCameraX == null || Recipe == null)
                return true;

            Recipe.EnsurePositionObjects();
            double tolerance = OutputCameraX.Config != null ? OutputCameraX.Config.InPositionTolerance : 0.05;
            return IsStageAxisInPosition(BinStageAxis.VisionX, Recipe.VisionX.AvoidPosition, tolerance);
        }

        public async Task<bool> WaitStageAxisMoveDone(BinStageAxis axis, int timeoutMs)
        {
            AxisMoveWaitResult waitResult = await WaitStageAxisMoveDoneInPosition(axis, timeoutMs).ConfigureAwait(false);
            return waitResult.Success;
        }

        public async Task<AxisMoveWaitResult> WaitStageAxisMoveDoneInPosition(BinStageAxis axis, int timeoutMs)
        {
            if (!HasStageAxis(axis))
                return new AxisMoveWaitResult(AxisMoveWaitFailure.None, axis + " axis does not exist.", string.Empty);

            BaseAxis item = ResolveStageAxis(axis);
            return await WaitStageAxisMoveDoneInPosition(axis, item.CommandPosition, timeoutMs).ConfigureAwait(false);
        }

        public async Task<AxisMoveWaitResult> WaitStageAxisMoveDoneInPosition(BinStageAxis axis, double targetPos, int timeoutMs)
        {
            if (!HasStageAxis(axis))
                return new AxisMoveWaitResult(AxisMoveWaitFailure.None, axis + " axis does not exist.", string.Empty);

            BaseAxis item = ResolveStageAxis(axis);
            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;
            return await AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                item,
                targetPos,
                tolerance,
                timeoutMs,
                0).ConfigureAwait(false);
        }

        public async Task<int> MoveToStageLoadPositionAndVerifyAsync(BinSide side, int timeoutMs, bool bFine = false)
        {
            try
            {
                if (side == BinSide.Good &&
                    HasStageAxis(BinStageAxis.GoodBinZ) &&
                    !IsGoodStageZInAvoidOrProcessPosition())
                {
                    int zSafeResult = await MoveStageAxisAndVerifyAsync(
                        BinStageAxis.GoodBinZ,
                        Recipe.GoodStageZ.ProcessPosition,
                        timeoutMs,
                        bFine).ConfigureAwait(false);
                    if (zSafeResult != 0)
                        return zSafeResult;
                }

                BinStageAxis yAxis = side == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
                double yTarget = side == BinSide.Ng ? Recipe.NGStageY.LoadPosition : Recipe.GoodStageY.LoadPosition;

                int result = await MoveStageAxisAndVerifyAsync(yAxis, yTarget, timeoutMs, bFine);
                if (result != 0)
                    return result;

                if (side == BinSide.Ng)
                    return 0;

                return await MoveStageAxisAndVerifyAsync(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.LoadPosition, timeoutMs, bFine);
            }
            catch (Exception ex)
            {
                return RaiseOutputStageAlarm("OS-STAGE-LOAD-EX", "Output stage Load 위치 이동 중 예외가 발생했습니다. side=" + side + ", " + ex.Message);
            }
            finally
            {
            }
        }

        public string DescribeStageLoadMoveState(BinSide side)
        {
            try
            {
                Recipe.EnsurePositionObjects();
                double yTarget = side == BinSide.Ng ? Recipe.NGStageY.LoadPosition : Recipe.GoodStageY.LoadPosition;
                string text = DescribeOutputStageInterlockState(side) +
                              ", yTarget=" + yTarget;

                if (side == BinSide.Good)
                    text += ", zLoadTarget=" + Recipe.GoodStageZ.LoadPosition +
                            ", zProcessTarget=" + Recipe.GoodStageZ.ProcessPosition +
                            FormatStageAxisLastMotionFailure(GoodStage != null ? GoodStage.StageY : null) +
                            FormatStageAxisLastMotionFailure(GoodStage != null ? GoodStage.StageZ : null);
                else
                    text += FormatStageAxisLastMotionFailure(NgStage != null ? NgStage.StageY : null);

                return text;
            }
            catch (Exception ex)
            {
                return "OutputStage Load 이동 상태 설명 실패: " + ex.Message;
            }
        }

        public bool IsStageInLoadPosition(BinSide side)
        {
            try
            {
                BinStageAxis yAxis = side == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
                double yTarget = side == BinSide.Ng ? Recipe.NGStageY.LoadPosition : Recipe.GoodStageY.LoadPosition;

                if (!CheckStageAxisInPosition(yAxis, yTarget))
                    return false;

                if (side == BinSide.Ng)
                    return true;

                return CheckStageAxisInPosition(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.LoadPosition);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public async Task<int> MoveToStageUnloadPositionAndVerifyAsync(BinSide side, int timeoutMs, bool bFine = false)
        {
            try
            {
                Recipe.EnsurePositionObjects();
                if (side == BinSide.Ng)
                    return await MoveStageAxisAndVerifyAsync(BinStageAxis.NgBinY, Recipe.NGStageY.UnloadPosition, timeoutMs, bFine);

                int result = await MoveStageAxisAndVerifyAsync(BinStageAxis.GoodBinY, Recipe.GoodStageY.UnloadPosition, timeoutMs, bFine);
                if (result != 0)
                    return result;

                return await MoveStageAxisAndVerifyAsync(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.UnloadPosition, timeoutMs, bFine);
            }
            catch (Exception ex)
            {
                return RaiseOutputStageAlarm("OS-STAGE-UNLOAD-EX", "Output stage unload position exception. side=" + side + ", " + ex.Message);
            }
            finally
            {
            }
        }

        public bool IsStageInUnloadPosition(BinSide side)
        {
            try
            {
                Recipe.EnsurePositionObjects();
                if (side == BinSide.Ng)
                    return CheckStageAxisInPosition(BinStageAxis.NgBinY, Recipe.NGStageY.UnloadPosition);

                return CheckStageAxisInPosition(BinStageAxis.GoodBinY, Recipe.GoodStageY.UnloadPosition) &&
                       CheckStageAxisInPosition(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.UnloadPosition);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public async Task<int> MoveVisionXToAvoidAndVerifyAsync(int timeoutMs, bool bFine = false)
        {
            try
            {
                Recipe.EnsurePositionObjects();
                return await MoveStageAxisAndVerifyAsync(BinStageAxis.VisionX, Recipe.VisionX.AvoidPosition, timeoutMs, bFine);
            }
            catch (Exception ex)
            {
                return RaiseOutputStageAlarm("OS-VISION-AVOID-EX", "OutputVisionX avoid exception: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> EnsureStageMutualInterlockForLoadAsync(BinSide side, int timeoutMs, bool bFine = false)
        {
            try
            {
                int result = await EnsureBinGuideClampLiftUpAsync(BinSide.Ng, timeoutMs);
                if (result != 0)
                    return result;

                if (!IsBinGuideClampLiftUp(BinSide.Ng))
                    return RaiseOutputStageAlarm("OS-NG-CLAMP-UP", "NG Bin Clamp Lift must be up before output stage load movement.");

                result = await EnsureBinGuideDownAsync(BinSide.Good, timeoutMs);
                if (result != 0)
                    return result;

                if (!IsBinGuideDown(BinSide.Good))
                    return RaiseOutputStageAlarm("OS-GOOD-GUIDE-DOWN", "Good Bin Guide must be down before output stage movement.");

                // OK Stage 로딩 전에는 NG Stage를 Avoid로 보낸 뒤 필요 시 가이드를 내린다.
                //result = await EnsureBinGuideDownAsync(BinSide.Ng, timeoutMs);
                //if (result != 0)
                //    return result;

                //if (!IsBinGuideDown(BinSide.Ng))
                //    return RaiseOutputStageAlarm("OS-NG-GUIDE-DOWN", "NG Bin Guide must be down before output stage movement.");

                if (!IsGoodStageZInAvoidPosition())
                {
                    result = await MoveGoodStageZToAvoidAndVerifyAsync(timeoutMs, bFine);
                    if (result != 0)
                        return result;
                }

                if (side != BinSide.Ng && !IsNgStageInAvoidPosition())
                {
                    result = await MoveNgStageToAvoidAndVerifyAsync(timeoutMs, bFine);
                    if (result != 0)
                        return result;
                }

                return 0;
            }
            catch (Exception ex)
            {
                return RaiseOutputStageAlarm("OS-STAGE-LOAD-INTERLOCK-EX", "Output stage load interlock exception. side=" + side + ", " + ex.Message);
            }
            finally
            {
            }
        }

        public bool IsGoodStageZInAvoidOrProcessPosition()
        {
            try
            {
                Recipe.EnsurePositionObjects();
                return CheckStageAxisInPosition(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.AvoidPosition) ||
                       CheckStageAxisInPosition(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.ProcessPosition);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public bool IsGoodStageZInAvoidPosition()
        {
            try
            {
                Recipe.EnsurePositionObjects();
                return CheckStageAxisInPosition(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.AvoidPosition);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public bool IsNgStageInAvoidPosition()
        {
            try
            {
                Recipe.EnsurePositionObjects();
                return CheckStageAxisInPosition(BinStageAxis.NgBinY, Recipe.NGStageY.AvoidPosition) &&
                       !HasStageAxis(BinStageAxis.NgBinZ);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// GOOD 스테이지 Z가 Avoid 위치인지 — 그리드 교시값(Recipe.GoodStageZ.AvoidPosition) 기준.
        /// 시퀀스/이동이 Z를 보내는 값과 동일 소스를 써서, 인터락 판정과 실제 이동값이 항상 일치하도록 한다.
        /// (StageModule.AvoidPositionZ와의 이중 저장 불일치를 방지)
        /// </summary>
        public bool IsGoodStageZAtAvoid()
        {
            try
            {
                if (GoodStage == null || GoodStage.StageZ == null)
                    return true;
                Recipe.EnsurePositionObjects();
                return CheckStageAxisInPosition(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.AvoidPosition);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public async Task<int> MoveGoodStageZToAvoidAndVerifyAsync(int timeoutMs, bool bFine = false)
        {
            try
            {
                Recipe.EnsurePositionObjects();
                return await MoveStageAxisAndVerifyAsync(BinStageAxis.GoodBinZ, Recipe.GoodStageZ.AvoidPosition, timeoutMs, bFine);
            }
            catch (Exception ex)
            {
                return RaiseOutputStageAlarm("OS-GOOD-Z-AVOID-EX", "Good Stage Z avoid exception: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> MoveNgStageToAvoidAndVerifyAsync(int timeoutMs, bool bFine = false)
        {
            try
            {
                Recipe.EnsurePositionObjects();
                return await MoveStageAxisAndVerifyAsync(BinStageAxis.NgBinY, Recipe.NGStageY.AvoidPosition, timeoutMs, bFine);
            }
            catch (Exception ex)
            {
                return RaiseOutputStageAlarm("OS-NG-AVOID-EX", "NG Stage avoid exception: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> EnsureBinGuideUpAsync(BinSide side, int timeoutMs)
        {
            return await EnsureCylinderStateAsync(ResolveBinGuideLiftCylinder(side), true, timeoutMs, ResolveSideName(side) + " Bin Guide Up");
        }

        public async Task<int> EnsureBinGuideDownAsync(BinSide side, int timeoutMs)
        {
            return await EnsureCylinderStateAsync(ResolveBinGuideLiftCylinder(side), false, timeoutMs, ResolveSideName(side) + " Bin Guide Down");
        }

        public async Task<int> EnsureBinGuideClampLiftDownAsync(BinSide side, int timeoutMs)
        {
            return await EnsureCylinderStateAsync(ResolveBinGuideClampLiftCylinder(side), false, timeoutMs, ResolveSideName(side) + " Bin Clamp Lift Down");
        }

        public async Task<int> EnsureBinGuideClampLiftUpAsync(BinSide side, int timeoutMs)
        {
            return await EnsureCylinderStateAsync(ResolveBinGuideClampLiftCylinder(side), true, timeoutMs, ResolveSideName(side) + " Bin Clamp Lift Up");
        }

        public async Task<int> EnsureBinGuideUnclampedAsync(BinSide side, int timeoutMs)
        {
            return await EnsureCylinderStateAsync(ResolveBinGuideClampCylinder(side), false, timeoutMs, ResolveSideName(side) + " Bin Guide Unclamp");
        }

        public async Task<int> EnsureBinGuideClampedAsync(BinSide side, int timeoutMs)
        {
            return await EnsureCylinderStateAsync(ResolveBinGuideClampCylinder(side), true, timeoutMs, ResolveSideName(side) + " Bin Guide Clamp");
        }

        public bool IsBinGuideUp(BinSide side)
        {
            BaseCylinder cylinder = ResolveBinGuideLiftCylinder(side);
            return ResolveCylinderState(cylinder, side == BinSide.Ng ? NgBinGuideUpSensor : GoodBinGuideUpSensor, true, IsStageMaterialPresent(side));
        }

        public bool IsBinGuideDown(BinSide side)
        {
            BaseCylinder cylinder = ResolveBinGuideLiftCylinder(side);
            return ResolveCylinderState(cylinder, side == BinSide.Ng ? NgBinGuideDownSensor : GoodBinGuideDownSensor, false, !IsStageMaterialPresent(side));
        }

        public bool IsBinGuideClampLiftDown(BinSide side)
        {
            BaseCylinder cylinder = ResolveBinGuideClampLiftCylinder(side);
            return ResolveCylinderState(cylinder, null, false, !IsStageMaterialPresent(side));
        }

        public bool IsBinGuideClampLiftUp(BinSide side)
        {
            BaseCylinder cylinder = ResolveBinGuideClampLiftCylinder(side);
            return ResolveCylinderState(cylinder, side == BinSide.Ng ? NgBinClampUpSensor : GoodBinClampUpSensor, true, IsStageMaterialPresent(side));
        }

        public bool IsBinGuideUnclamped(BinSide side)
        {
            BaseCylinder cylinder = ResolveBinGuideClampCylinder(side);
            return ResolveCylinderState(cylinder, side == BinSide.Ng ? NgBinUnclampSensor : GoodBinUnclampSensor, false, !IsStageMaterialPresent(side));
        }

        public bool IsBinGuideClamped(BinSide side)
        {
            BaseCylinder cylinder = ResolveBinGuideClampCylinder(side);
            return ResolveCylinderState(cylinder, null, true, IsStageMaterialPresent(side));
        }

        public bool IsOutputStageSimulationOrDryRun()
        {
            AppSettings settings = AppSettingsStore.Current;
            bool simulation = Setup != null && Setup.IsSimulationMode;
            bool dryRun = Config != null && Config.bDryRun;
            bool globalDryRun = settings != null && (settings.BypassHardware || settings.DryRunMode);
            return globalDryRun || simulation || dryRun;
        }

        private bool ResolveCylinderState(BaseCylinder cylinder, BaseDigitalInput input, bool fwd, bool dryRunDefaultWhenUnknown)
        {
            RefreshCylinderInputs(cylinder);

            if (input != null && input.IsOn)
                return true;

            if (cylinder != null && (fwd ? cylinder.IsFwd : cylinder.IsBwd))
                return true;

            if (ShouldUseVirtualCylinderDefault(cylinder) && IsCylinderStateUnknown(cylinder))
                return dryRunDefaultWhenUnknown;

            return false;
        }

        private bool ShouldUseVirtualCylinderDefault(BaseCylinder cylinder)
        {
            AppSettings settings = AppSettingsStore.Current;
            bool appVirtual = settings != null && (settings.BypassHardware || settings.SimulationMode);
            bool setupSimulation = Setup != null && Setup.IsSimulationMode;
            bool cylinderSimulation = cylinder != null && cylinder.Config != null && cylinder.Config.IsSimulationMode;
            return appVirtual || setupSimulation || cylinderSimulation || !AjinFactory.IsRealBoardReady;
        }

        private static bool IsCylinderStateUnknown(BaseCylinder cylinder)
        {
            return cylinder == null || (!cylinder.IsFwd && !cylinder.IsBwd);
        }

        private static bool IsStageMaterialPresent(BinSide side)
        {
            MaterialLocationKind location = side == BinSide.Ng
                ? MaterialLocationKind.OutputStageNg
                : MaterialLocationKind.OutputStageGood;
            return MaterialStateService.GetWaferAtLocation(location) != null;
        }

        private bool RefreshCylinderInputs(BaseCylinder cylinder)
        {
            if (cylinder == null || cylinder.Setup == null ||
                cylinder.Config == null || cylinder.Config.IsSimulationMode ||
                cylinder.Config.IgnoreInputWaits ||
                !AjinFactory.IsRealBoardReady)
                return true;

            bool ok = true;
            int errorCode;

            if (cylinder.Setup.UseFwdSensor && cylinder.InFwd != null)
                ok = AjinIoScanService.TryReadHardwareInput(cylinder.InFwd, out errorCode) && ok;

            if (cylinder.Setup.UseBwdSensor && cylinder.InBwd != null)
                ok = AjinIoScanService.TryReadHardwareInput(cylinder.InBwd, out errorCode) && ok;

            return ok;
        }

        public string DescribeOutputStageInterlockState(BinSide side)
        {
            return "side=" + side +
                   ", guideUp=" + IsBinGuideUp(side) +
                   ", guideDown=" + IsBinGuideDown(side) +
                   ", clampLiftUp=" + IsBinGuideClampLiftUp(side) +
                   ", clampLiftDown=" + IsBinGuideClampLiftDown(side) +
                   ", clamp=" + IsBinGuideClamped(side) +
                   ", unclamp=" + IsBinGuideUnclamped(side) +
                   ", ngClampLiftUp=" + IsBinGuideClampLiftUp(BinSide.Ng) +
                   ", goodZSafe=" + IsGoodStageZInAvoidOrProcessPosition() +
                   ", ngStageAvoid=" + IsNgStageInAvoidPosition() +
                   ", goodY=" + FormatAxisState(GoodStage != null ? GoodStage.StageY : null) +
                   ", goodZ=" + FormatAxisState(GoodStage != null ? GoodStage.StageZ : null) +
                   ", ngY=" + FormatAxisState(NgStage != null ? NgStage.StageY : null);
        }

        private static string FormatAxisState(BaseAxis axis)
        {
            if (axis == null)
                return "null";

            return axis.Name +
                   "(servo=" + axis.IsServoOn +
                   ", alarm=" + axis.IsAlarm +
                   ", moving=" + axis.IsMoving +
                   ", actual=" + axis.ActualPosition + ")";
        }

        public void TeachStageAxisPosition(BinStageAxis axis, string positionName)
        {
            SetStageTeachingPosition(axis, positionName, ResolveStageAxis(axis).ActualPosition);
            EventLogger.Write(EventKind.Event, "QMC", "OS-TEACH", axis + "." + positionName + "=" + ResolveStageAxis(axis).ActualPosition);
        }

        public double GetStageTeachingPosition(BinStageAxis axis, string positionName)
        {
            Recipe.EnsurePositionObjects();
            StageAxisPositions positions = ResolveRecipePositions(axis);
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) return positions.AvoidPosition;
            if (string.Equals(positionName, "Load", StringComparison.OrdinalIgnoreCase)) return positions.LoadPosition;
            if (string.Equals(positionName, "Process", StringComparison.OrdinalIgnoreCase)) return positions.ProcessPosition;
            if (string.Equals(positionName, "Unload", StringComparison.OrdinalIgnoreCase)) return positions.UnloadPosition;
            if (string.Equals(positionName, "Reticle", StringComparison.OrdinalIgnoreCase)) return positions.ReticlePosition;
            throw new ArgumentException("Unknown stage teaching position: " + positionName, "positionName");
        }

        public bool ValidateStageTeachingComplete(BinSide side)
        {
            Recipe.EnsurePositionObjects();
            StageAxisPositions y = side == BinSide.Ng ? Recipe.NGStageY : Recipe.GoodStageY;
            return y.LoadPosition != y.AvoidPosition &&
                   y.ProcessPosition != y.AvoidPosition &&
                   y.UnloadPosition != y.AvoidPosition;
        }

        private async Task<int> MoveStageAxisAndVerifyAsync(BinStageAxis axis, double targetPos, int timeoutMs, bool bFine)
        {
            if (!HasStageAxis(axis))
                return 0;

            int result = await MoveStageAxis(axis, targetPos, bFine);
            if (result != 0)
                return result;

            AxisMoveWaitResult waitResult = await WaitStageAxisMoveDoneInPosition(axis, targetPos, timeoutMs).ConfigureAwait(false);
            if (!waitResult.Success)
                return RaiseOutputStageAlarm(
                    AxisMoveWaiter.ResolveAlarmCode("OS-MOVE", waitResult),
                    axis + " 이동 완료/위치 확인 실패. target=" + targetPos + ". " +
                    AxisMoveWaiter.FormatResult(waitResult, axis.ToString()));

            return 0;
        }

        private bool CheckStageAxisInPosition(BinStageAxis axis, double targetPos)
        {
            if (!HasStageAxis(axis))
                return true;

            BaseAxis item = ResolveStageAxis(axis);
            double tolerance = item.Config != null ? item.Config.InPositionTolerance : 0.05;
            return Math.Abs(item.ActualPosition - targetPos) <= tolerance;
        }

        private async Task<int> EnsureCylinderStateAsync(BaseCylinder cylinder, bool fwd, int timeoutMs, string description)
        {
            try
            {
                if (cylinder == null)
                    return RaiseOutputStageAlarm("OS-CYL-MISSING", description + " 실린더가 등록되어 있지 않습니다.");

                if (!RefreshCylinderInputs(cylinder))
                    return RaiseOutputStageAlarm("OS-CYL-INPUT", description + " 실린더 입력 갱신 실패.");

                bool already = fwd ? cylinder.IsFwd : cylinder.IsBwd;
                if (already)
                    return 0;

                bool ok = fwd ? await cylinder.MoveFwdAsync() : await cylinder.MoveBwdAsync();
                if (!ok)
                    return RaiseOutputStageAlarm("OS-CYL-MOVE", description + " 실린더 구동 실패.");

                bool refreshOk = true;
                bool arrived = await WaitUntilAsync(() =>
                {
                    if (!RefreshCylinderInputs(cylinder))
                    {
                        refreshOk = false;
                        return false;
                    }

                    return fwd ? cylinder.IsFwd : cylinder.IsBwd;
                }, timeoutMs);
                if (!refreshOk)
                    return RaiseOutputStageAlarm("OS-CYL-INPUT", description + " 실린더 입력 갱신 실패.");

                if (!arrived)
                    return RaiseOutputStageAlarm("OS-CYL-TIMEOUT", description + " 실린더 센서 대기 시간 초과.");

                return 0;
            }
            catch (Exception ex)
            {
                return RaiseOutputStageAlarm("OS-CYL-EX", description + " 실린더 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private static BaseCylinder ResolveBinGuideLiftCylinder(BinSide side)
        {
            return CylinderManager.Get(side == BinSide.Ng ? "NGBinGuideLift" : "GoodBinGuideLift");
        }

        private static BaseCylinder ResolveBinGuideClampLiftCylinder(BinSide side)
        {
            return CylinderManager.Get(side == BinSide.Ng ? "NGBinGuideClampLift" : "GoodBinGuideClampLift");
        }

        private static BaseCylinder ResolveBinGuideClampCylinder(BinSide side)
        {
            return CylinderManager.Get(side == BinSide.Ng ? "NGBinGuideClamp" : "GoodBinGuideClamp");
        }

        private static string ResolveSideName(BinSide side)
        {
            return side == BinSide.Ng ? "NG" : "GOOD";
        }

        public bool HasStageAxis(BinStageAxis axis)
        {
            switch (axis)
            {
                // NG 스테이지 Y축 장착 여부 확인
                case BinStageAxis.NgBinY:
                    return NgStage != null && NgStage.StageY != null;
                // NG 스테이지 Z축 장착 여부 확인
                case BinStageAxis.NgBinZ:
                    return NgStage != null && NgStage.HasStageZ && NgStage.StageZ != null;
                // GOOD 스테이지 Y축 장착 여부 확인
                case BinStageAxis.GoodBinY:
                    return GoodStage != null && GoodStage.StageY != null;
                // GOOD 스테이지 Z축 장착 여부 확인
                case BinStageAxis.GoodBinZ:
                    return GoodStage != null && GoodStage.HasStageZ && GoodStage.StageZ != null;
                // 아웃풋 비전 X축 장착 여부 확인
                case BinStageAxis.VisionX:
                    return OutputCameraX != null;
                default:
                    return false;
            }
        }

        public string BuildStageAxisState(BinStageAxis axis, double target)
        {
            try
            {
                if (!HasStageAxis(axis))
                    return axis + "[state=not-mounted-or-null, target=" + target + "]";

                BaseAxis item = ResolveStageAxis(axis);
                if (item == null)
                    return axis + "[state=null, target=" + target + "]";

                double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                    ? item.Config.InPositionTolerance
                    : 0.001;

                return axis +
                       "[name=" + item.Name +
                       ", servo=" + (item.IsServoOn ? "ON" : "OFF") +
                       ", alarm=" + (item.IsAlarm ? "ON" : "OFF") +
                       ", moving=" + (item.IsMoving ? "Y" : "N") +
                       ", actual=" + item.ActualPosition +
                       ", target=" + target +
                       ", tolerance=" + tolerance +
                       "]";
            }
            catch (Exception ex)
            {
                return axis + "[state=exception, target=" + target + ", error=" + ex.Message + "]";
            }
            finally
            {
            }
        }

        private BaseAxis ResolveStageAxis(BinStageAxis axis)
        {
            switch (axis)
            {
                // NG 스테이지 Y축 반환
                case BinStageAxis.NgBinY: return NgStage.StageY;
                // NG 스테이지 Z축은 별도 StageModuleRecipe 영역
                case BinStageAxis.NgBinZ:
                    throw new InvalidOperationException("NG Stage does not have Z axis.");
                // GOOD 스테이지 Y축 반환
                case BinStageAxis.GoodBinY: return GoodStage.StageY;
                // GOOD 스테이지 Z축 반환
                case BinStageAxis.GoodBinZ: return GoodStage.StageZ;
                // 아웃풋 비전 X축 반환
                case BinStageAxis.VisionX: return OutputCameraX;
                default: throw new ArgumentOutOfRangeException("axis");
            }
        }

        /// <summary>해당 스테이지 축의 원점복귀(IsHomeDone) 완료 여부. (수동버튼 홈게이트용)</summary>
        public bool IsStageAxisHomeDone(BinStageAxis axis)
        {
            BaseAxis item = ResolveStageAxis(axis);
            return item != null && item.IsHomeDone;
        }

        /// <summary>해당 스테이지 축이 목표 위치에 있는지(축 InPositionTolerance 기준) 확인. (수동버튼 Z안전 확인용)</summary>
        public bool IsStageAxisAtPosition(BinStageAxis axis, double targetPos)
        {
            BaseAxis item = ResolveStageAxis(axis);
            if (item == null)
                return false;
            double tol = (item.Config != null && item.Config.InPositionTolerance >= 0.0) ? item.Config.InPositionTolerance : 0.05;
            return Math.Abs(item.ActualPosition - targetPos) <= tol && !item.IsAlarm;
        }

        private bool TryResolveStageAxis(BaseAxis axis, out BinStageAxis stageAxis)
        {
            stageAxis = BinStageAxis.NgBinY;
            if (axis == null)
                return false;

            if (ReferenceEquals(axis, NgStage.StageY))
            {
                stageAxis = BinStageAxis.NgBinY;
                return true;
            }

            if (NgStage.HasStageZ && ReferenceEquals(axis, NgStage.StageZ))
            {
                stageAxis = BinStageAxis.NgBinZ;
                return true;
            }

            if (ReferenceEquals(axis, GoodStage.StageY))
            {
                stageAxis = BinStageAxis.GoodBinY;
                return true;
            }

            if (ReferenceEquals(axis, GoodStage.StageZ))
            {
                stageAxis = BinStageAxis.GoodBinZ;
                return true;
            }

            if (ReferenceEquals(axis, OutputCameraX))
            {
                stageAxis = BinStageAxis.VisionX;
                return true;
            }

            return false;
        }

        private double ResolveStageAxisVelocity(BaseAxis axis, bool bFine)
        {
            if (bFine && axis.Config != null && axis.Config.JogFineVelocity > 0.0)
                return axis.Config.JogFineVelocity;

            if (axis.Config != null && axis.Config.DefaultVelocity > 0.0)
                return axis.Config.DefaultVelocity;

            return 100.0;
        }

        private StageAxisPositions ResolveRecipePositions(BinStageAxis axis)
        {
            Recipe.EnsurePositionObjects();
            switch (axis)
            {
                // NG 스테이지 Y축 레시피 포지션 반환
                case BinStageAxis.NgBinY:
                    return Recipe.NGStageY;
                // NG 스테이지 Z축 레시피 포지션은 StageModuleRecipe 사용
                case BinStageAxis.NgBinZ:
                    throw new InvalidOperationException("NgStage Z teaching positions are defined in StageModuleRecipe, not OutputStageRecipe.");
                // GOOD 스테이지 Y축 레시피 포지션 반환
                case BinStageAxis.GoodBinY:
                    return Recipe.GoodStageY;
                // GOOD 스테이지 Z축 레시피 포지션 반환
                case BinStageAxis.GoodBinZ:
                    return Recipe.GoodStageZ;
                // 아웃풋 비전 X축 레시피 포지션 반환
                case BinStageAxis.VisionX:
                    return Recipe.VisionX;
                default:
                    throw new ArgumentOutOfRangeException("axis");
            }
        }

        private void SetStageTeachingPosition(BinStageAxis axis, string positionName, double position)
        {
            StageAxisPositions positions = ResolveRecipePositions(axis);
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) positions.AvoidPosition = position;
            else if (string.Equals(positionName, "Load", StringComparison.OrdinalIgnoreCase)) positions.LoadPosition = position;
            else if (string.Equals(positionName, "Process", StringComparison.OrdinalIgnoreCase)) positions.ProcessPosition = position;
            else if (string.Equals(positionName, "Unload", StringComparison.OrdinalIgnoreCase)) positions.UnloadPosition = position;
            else if (string.Equals(positionName, "Reticle", StringComparison.OrdinalIgnoreCase)) positions.ReticlePosition = position;
            else throw new ArgumentException("Unknown stage teaching position: " + positionName, "positionName");
        }

        private int RaiseOutputStageAlarm(string code, string message)
        {
            EventLogger.Write(EventKind.Alarm, "QMC", code, Name, message);
            AlarmManager.Raise(AlarmSeverity.Error, code, Name, message);
            return -1;
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs)
        {
            int elapsed = 0;
            while (timeoutMs <= 0 || elapsed < timeoutMs)
            {
                if (condition())
                    return true;

                await Task.Delay(10).ContinueWith(_ => { });
                elapsed += 10;
            }
            return condition();
        }

        // ======================================================================

        /// <summary>대상 스테이지의 반대쪽 스테이지를 반환합니다.</summary>
        /// <param name="target">기준 스테이지입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        private StageModule GetOppositeStage(StageModule target)
        {
            return ReferenceEquals(target, GoodStage) ? NgStage : GoodStage;
        }

        /// <summary>대상 StageY 이동 전 반대쪽 스테이지 Z가 Avoid 위치인지 보장합니다.</summary>
        /// <param name="targetStage">이동 대상 스테이지입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        private async Task<bool> EnsureOppositeStageAvoidedAsync(StageModule targetStage)
        {
            StageModule opposite = GetOppositeStage(targetStage);

            if (opposite == null)
                return true;

            if (!opposite.HasStageZ || opposite.StageZ == null)
            {
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Interlock: '" + opposite.Name +
                    "' has no StageZ. Z avoid interlock skipped.");
                return true;
            }

            if (opposite.IsAtAvoidPosition())
            {
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Interlock: '" + opposite.Name +
                    "' 이미 회피 위치입니다. 추가 동작 불필요.");
                return true;
            }

            Console.WriteLine(
                "[WARN]  '" + Name + "' -> Interlock: '" + opposite.Name +
                "' Z=" + AxisUnitConverter.FormatDisplay(opposite.StageZ.ActualPosition, opposite.StageZ, "0.###", true) +
                ", 회피 위치 아님. 강제 하강 시작.");

            bool avoidOk = await opposite.MoveToAvoidPositionAsync();

            if (!avoidOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> Interlock: '" + opposite.Name +
                    "' 회피 하강 실패. '" + targetStage.Name + "' StageY 이동 중단.");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "OS-AVOID",
                    source: Name + ".EnsureOppositeStageAvoidedAsync",
                    message: "반대쪽 스테이지(" + opposite.Name + ") 회피 하강 실패. '" +
                             targetStage.Name + "' StageY 이동 중단");
                return false;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> Interlock: '" + opposite.Name +
                "' 회피 완료. '" + targetStage.Name + "' StageY 이동 가능.");
            return true;
        }

        // ======================================================================
        // ======================================================================

        /// <summary>TPU에서 받은 다이를 등급에 맞는 GOOD/NG 스테이지로 수신합니다.</summary>
        /// <param name="request">다이 수신 요청입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> ReceiveDieAsync(ReceiveDieRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            StageModule target = request.Grade == DieGrade.Good ? GoodStage : NgStage;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> ReceiveDie: Grade=" + request.Grade +
                ", TpuOffsetY=" + request.TpuOffsetY.ToString("F3") +
                ", VisionOffsetY=" + request.VisionOffsetY.ToString("F3"));

            bool interlockOk = await EnsureOppositeStageAvoidedAsync(target);
            if (!interlockOk)
                return false;

            if (target.HasStageZ && target.StageZ != null)
            {
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> '" + target.Name +
                    "' StageZ 작업 위치로 상승 중...");

                bool workZOk = await target.MoveToWorkPositionAsync();
                if (!workZOk)
                    return false;
            }
            else
            {
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> '" + target.Name +
                    "' has no StageZ. StageZ work move skipped.");
            }

            // Final Y = base position + TPU offset + bottom vision offset.
            double baseY = target.Recipe.HomePositionY;
            double finalY = baseY
                            + request.TpuOffsetY
                            + request.VisionOffsetY;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' StageY 이동. FinalY=" + finalY.ToString("F3") + "mm " +
                "(Base=" + baseY.ToString("F3") +
                " + TpuY=" + request.TpuOffsetY.ToString("F3") +
                " + VisionY=" + request.VisionOffsetY.ToString("F3") + ")");

            bool moveYOk = await target.MoveYAsync(finalY);
            if (!moveYOk)
                return false;

            Tpu.NotifyPlaceReady();
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> TPU에 Place 준비 완료 신호 전송 완료.");

            return true;
        }

        // ======================================================================
        // ======================================================================

        /// <summary>TPU Place 완료 후 BinCamera로 안착 상태를 검사합니다.</summary>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> InspectBinPositionAsync()
        {
            Console.WriteLine("[INFO]  '" + Name + "' -> Bin 검사 시작. TPU Place 완료 대기 중...");

            bool placeDone = await Tpu.WaitPlaceDoneAsync();
            if (!placeDone)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> InspectBin: TPU Place 완료 대기 타임아웃");
                AlarmManager.Raise(
                    AlarmSeverity.Warning,
                    "OS-PLACEDONE",
                    source: Name + ".InspectBinPositionAsync",
                    message: "TPU Place 완료 대기 타임아웃");
                return false;
            }

            Console.WriteLine("[INFO]  '" + Name + "' -> TPU 후퇴 확인. BinCamera 진입 중...");

            int moveResult = await MoveStageAxisAndVerifyAsync(
                BinStageAxis.VisionX,
                Recipe.VisionX.ProcessPosition,
                OutputCameraX != null && OutputCameraX.Setup != null ? OutputCameraX.Setup.MoveTimeoutMs : 10000,
                false).ConfigureAwait(false);

            if (moveResult != 0)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> InspectBin: BinCameraX 진입 실패.");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "OS-BINCAM",
                    source: Name + ".InspectBinPositionAsync",
                    message: "BinCameraX 검사 위치 진입 실패 (axis code=" +
                             OutputCameraX.AlarmCode + ")");
                return false;
            }

            Console.WriteLine("[INFO]  '" + Name + "' -> BinCamera 안착 검사 수행 중...");
            SimulatorBridge.Instance?.CameraExposeFlash("BIN");
            await Task.Delay(20).ContinueWith(_ => { }); // 촬상 소요 시간 시뮬레이션
            Console.WriteLine("[INFO]  '" + Name + "' -> BinCamera 검사 완료. 즉시 후퇴.");

            moveResult = await MoveStageAxisAndVerifyAsync(
                BinStageAxis.VisionX,
                Recipe.VisionX.AvoidPosition,
                OutputCameraX != null && OutputCameraX.Setup != null ? OutputCameraX.Setup.MoveTimeoutMs : 10000,
                false).ConfigureAwait(false);

            if (moveResult != 0)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> InspectBin: BinCameraX 후퇴 실패.");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "OS-BINCAM",
                    source: Name + ".InspectBinPositionAsync",
                    message: "BinCameraX 대기 위치 후퇴 실패 (axis code=" +
                             OutputCameraX.AlarmCode + ")");
                return false;
            }

            Tpu.NotifyReadyForNextDie();
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> TPU에 다음 다이 수신 가능 통보 완료.");

            return true;
        }

        // ======================================================================
        // ======================================================================

        /// <summary>지정 등급의 출력 bin이 가득 찼을 때 교체 위치로 이동하고 Unloader에 교체를 요청합니다.</summary>
        /// <param name="grade">교체 대상 다이 등급입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> RequestWaferChangeAsync(DieGrade grade)
        {
            StageModule target = grade == DieGrade.Good ? GoodStage : NgStage;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> WaferChange: Grade=" + grade +
                " Bin Full. 교체 위치 이동 시작.");

            bool interlockOk = await EnsureOppositeStageAvoidedAsync(target);
            if (!interlockOk)
                return false;

            if (target.HasStageZ && target.StageZ != null)
            {
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> '" + target.Name +
                    "' StageZ 회피 위치 하강 중...");

                bool avoidOk = await target.MoveToAvoidPositionAsync();
                if (!avoidOk)
                    return false;
            }
            else
            {
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> '" + target.Name +
                    "' has no StageZ. StageZ avoid move skipped.");
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' 교체 위치(Y=" + target.Recipe.UnloadPositionY.ToString("F1") +
                "mm)로 이동 중...");

            bool moveOk = await target.MoveYAsync(target.Recipe.UnloadPositionY);
            if (!moveOk)
                return false;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' 교체 위치 도달. Unloader에 체인지 요청 전송 (Suspend 시작).");

            bool changeOk = await Unloader.RequestWaferChangeAsync(grade);

            if (!changeOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> WaferChange: Unloader 교체 타임아웃 Grade=" + grade);
                return false;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> WaferChange: 교체 완료. Grade=" + grade);
            return true;
        }

        // ======================================================================
        // ======================================================================

        /// <summary>NG 스테이지를 클리닝 위치로 이동시킨 뒤 TPU 콜렛 클리닝을 요청합니다.</summary>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> PerformColletCleaningAsync()
        {
            Console.WriteLine("[INFO]  '" + Name + "' -> 콜렛 클리닝 시작. NgStage 더미 영역으로 이동.");

            bool interlockOk = await EnsureOppositeStageAvoidedAsync(NgStage);
            if (!interlockOk)
                return false;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage StageY ??CleaningPositionY=" +
                NgStage.Recipe.CleaningPositionY.ToString("F1") + "mm 이동 중...");

            bool cleaningMoveOk = await NgStage.MoveYAsync(NgStage.Recipe.CleaningPositionY);
            if (!cleaningMoveOk)
                return false;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage has no StageZ. StageZ work move skipped.");

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> 클리닝 준비 완료. TPU에 콜렛 클리닝 요청 전송.");

            bool cleaningOk = await Tpu.RequestColletCleaningAsync(Config.ColletCleaningTimeoutMs);
            if (!cleaningOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> ColletCleaning: TPU 클리닝 타임아웃");
            }
            else
            {
                Console.WriteLine("[INFO]  '" + Name + "' -> TPU 콜렛 클리닝 완료.");
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage has no StageZ. StageZ avoid move skipped.");

            // Step 4-6. NgStage StageY를 HomePositionY로 복귀
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage StageY 원위치 복귀 중...");

            bool homeOk = await NgStage.MoveToHomeAsync();
            if (!homeOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> ColletCleaning: NgStage 복귀 실패.");
                return false;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> 콜렛 클리닝 시퀀스 완료. NgStage 원위치.");

            return cleaningOk;
        }
    }
}

