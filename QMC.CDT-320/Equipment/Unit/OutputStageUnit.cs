using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using QMC.Common;
using QMC.Common.Motion;
using QMC.Common.IO;
using QMC.CDT320.Ajin;
using QMC.CDT320.Motion.SharedRailX;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT320
{
    // ==========================================================================
    // 구현 보조 주석입니다.

    /// <summary>
    ///
    /// </summary>
    public enum DieGrade
    {
        /// <summary>구현 설명 주석입니다.</summary>
        Good,

        /// <summary>구현 설명 주석입니다.</summary>
        Ng
    }

    /// <summary>
    ///
    ///
    /// </summary>
    public class ReceiveDieRequest
    {
        /// <summary>구현 설명 주석입니다.</summary>
        public DieGrade Grade { get; set; }

        /// <summary>
        ///
        ///
        /// </summary>
        public double TpuOffsetX { get; set; }

        /// <summary>
        ///
        ///
        /// </summary>
        public double TpuOffsetY { get; set; }

        /// <summary>
        ///
        ///
        /// </summary>
        public double VisionOffsetX { get; set; }

        /// <summary>
        ///
        ///
        /// </summary>
        public double VisionOffsetY { get; set; }
    }

    // --------------------------------------------------------------------------

    /// <summary>
    ///
    ///
    /// </summary>
    public interface ITpuUnit
    {
        /// <summary>
        ///
        ///
        /// </summary>
        void NotifyPlaceReady();

        /// <summary>
        ///
        ///
        /// </summary>
        /// <param name="value">입력 값입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        Task<bool> WaitPlaceDoneAsync(int timeoutMs = 3000);

        /// <summary>
        ///
        ///
        /// </summary>
        void NotifyReadyForNextDie();

        /// <summary>
        ///
        ///
        /// </summary>
        /// <param name="value">입력 값입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        Task<bool> RequestColletCleaningAsync(int timeoutMs = 10000);
    }

    /// <summary>
    ///
    ///
    /// </summary>
    public interface IOutputUnloaderUnit
    {
        /// <summary>
        ///
        ///
        /// </summary>
        /// <param name="value">입력 값입니다.</param>
        /// <param name="value">입력 값입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        Task<bool> RequestWaferChangeAsync(DieGrade grade, int timeoutMs = 0);
    }

    // ==========================================================================
    // 구현 보조 주석입니다.
    /// <summary>
    ///
    /// </summary>
    public class StageModuleSetup : ISetupData
    {
        
    }

    /// <summary>구현 설명 주석입니다.</summary>
    public class StageModuleConfig : IConfigData
    {
      
    }

    /// <summary>구현 설명 주석입니다.</summary>
    public class StageModuleRecipe : IRecipeData
    {
        public double WorkPositionZ { get; set; } = 10.0;
        public double AvoidPositionZ { get; set; } = 0.0;
        public double UnloadPositionY { get; set; } = -50.0;
        public double HomePositionY { get; set; } = 0.0;
        public double CleaningPositionY { get; set; } = 80.0;
    }

    // --------------------------------------------------------------------------

    /// <summary>구현 설명 주석입니다.</summary>
    public class OutputStageSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }
    }

    /// <summary>구현 설명 주석입니다.</summary>
    public class OutputStageConfig : IConfigData
    {
        [DataMember] public bool bDryRun { get; set; }

        public bool IsSimulationMode
        {
            get { return bDryRun; }
            set { bDryRun = value; }
        }
        /// <summary>구현 설명 주석입니다.</summary>
        [DataMember] public int ColletCleaningTimeoutMs { get; set; } = 10000;
    }

    /// <summary>구현 설명 주석입니다.</summary>
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
    // 구현 보조 주석입니다.
    // ==========================================================================
    /// <summary>
    ///
    /// </summary>
    public class StageModule : BaseUnit<StageModuleSetup, StageModuleConfig, StageModuleRecipe>
    {
        // ----------------------------------------------------------------------
        // 구현 보조 주석입니다.
        // ----------------------------------------------------------------------
        public BaseAxis StageY { get; private set; }
        public BaseAxis StageZ { get; private set; }

        // ----------------------------------------------------------------------
        // 구현 보조 주석입니다.
        /// <param name="value">입력 값입니다.</param>
        public StageModule(string moduleName) : base(moduleName)
        {
            StageY = AjinFactory.CreateAxis(moduleName + "_StageY");
            StageZ = AjinFactory.CreateAxis(moduleName + "_StageZ");
            Components.Add(StageY);
            Components.Add(StageZ);
        }

        // ----------------------------------------------------------------------
        // 구현 보조 주석입니다.

        /// <summary>
        ///
        ///
        ///
        /// </summary>
        /// <returns>처리 결과입니다.</returns>
        public bool IsAtAvoidPosition()
        {
            double tolerance = StageZ.Config != null ? StageZ.Config.InPositionTolerance : 0.01;
            double diff = Math.Abs(StageZ.ActualPosition - Recipe.AvoidPositionZ);
            return diff <= tolerance;
        }

        /// <summary>
        ///
        ///
        /// </summary>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> MoveToAvoidPositionAsync()
        {
            if (IsAtAvoidPosition())
                return true;

            await StageZ.MoveAbsoluteAsync(Recipe.AvoidPositionZ, ResolveAxisVelocity(StageZ));

            if (StageZ.IsAlarm)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> MoveToAvoidPosition: StageZ 하강 실패.");
                return false;
            }

            return true;
        }

        /// <summary>
        ///
        ///
        /// </summary>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> MoveToWorkPositionAsync()
        {
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

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value">입력 값입니다.</param>
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

            return true;
        }

        /// <summary>
        ///
        ///
        /// </summary>
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
    }

    // ==========================================================================
    // 구현 보조 주석입니다.
    // ==========================================================================

    /// <summary>
    //
    /// </summary>
    public class OutputStageUnit : BaseUnit<OutputStageSetup, OutputStageConfig, OutputStageRecipe>, IUnitJogController
    {
        // ----------------------------------------------------------------------
        // 구현 보조 주석입니다.
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
        // 구현 보조 주석입니다.

        /// <summary>
        ///
        ///
        /// </summary>
        public ITpuUnit Tpu { get; private set; }

        /// <summary>
        ///
        ///
        /// </summary>
        public IOutputUnloaderUnit Unloader { get; private set; }

        // ----------------------------------------------------------------------
        // 구현 보조 주석입니다.

        /// <summary>
        ///
        /// </summary>
        /// <param name="value">입력 값입니다.</param>
        /// <param name="value">입력 값입니다.</param>
        public OutputStageUnit(ITpuUnit tpu, IOutputUnloaderUnit unloader)
            : base("OutputStageUnit")
        {
            if (tpu      == null) throw new ArgumentNullException("tpu");
            if (unloader == null) throw new ArgumentNullException("unloader");

            Tpu      = tpu;
            Unloader = unloader;

            GoodStage  = new StageModule("GoodStage");
            NgStage    = new StageModule("NgStage");
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
            GoodBinUnclampSensor   = RegisterCylinderInput("GoodBinGuideClamp", false, "GoodBinClamp");
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

            // 구현 보조 주석입니다.
            Components.Add(GoodStage);
            Components.Add(NgStage);
            Components.Add(OutputCameraX);
        }

        private BaseDigitalInput RegisterInput(string catalogName)
        {
            BaseDigitalInput item = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput(catalogName));
            Components.Add(item);
            return item;
        }

        private BaseDigitalOutput RegisterOutput(string catalogName)
        {
            BaseDigitalOutput item = AjinFactory.CreateDigitalOutput(AjinIoCatalog.FindOutput(catalogName));
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
                GoodBinUnclampSensor = RebindInput(GoodBinUnclampSensor, RegisterCylinderInput("GoodBinGuideClamp", false, "GoodBinClamp"));

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
                EventLogger.Write(EventKind.Alarm, "QMC", "OS-CYL-IO",
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
            DioMap map = ResolveCylinderInputMap(cylinderName, fwd);
            BaseDigitalInput item = map != null
                ? AjinFactory.CreateSharedDigitalInput(cylinderName + (fwd ? "_InFwd" : "_InBwd"), map, !AjinFactory.IsRealBoardReady)
                : AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput(fallbackCatalogName));
            Components.Add(item);
            LogCylinderIoBinding(cylinderName, fwd ? "InFwd" : "InBwd", map, false, fallbackCatalogName);
            return item;
        }

        private BaseDigitalOutput RegisterCylinderOutput(string cylinderName, bool fwd, string fallbackCatalogName)
        {
            DioMap map = ResolveCylinderOutputMap(cylinderName, fwd);
            BaseDigitalOutput item = map != null
                ? AjinFactory.CreateSharedDigitalOutput(cylinderName + (fwd ? "_OutFwd" : "_OutBwd"), map, !AjinFactory.IsRealBoardReady)
                : AjinFactory.CreateDigitalOutput(AjinIoCatalog.FindOutput(fallbackCatalogName));
            Components.Add(item);
            LogCylinderIoBinding(cylinderName, fwd ? "OutFwd" : "OutBwd", map, true, fallbackCatalogName);
            return item;
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
                BaseAxis item = ResolveStageAxis(axis);
                double velocity = ResolveStageAxisVelocity(item, bFine);
                EventLogger.Write(EventKind.Event, "QMC", "OS-MOVE", axis + " target=" + targetPos);
                int result = await SharedRailXMotionRuntime.MoveAxisAsync(item, targetPos, velocity);
                if (result != 0 || item.IsAlarm)
                    return RaiseOutputStageAlarm("OS-MOVE", axis + " move failed. result=" + result + ", alarm=" + item.IsAlarm);
                return 0;
            }
            catch (Exception ex)
            {
                return RaiseOutputStageAlarm("OS-MOVE-EX", axis + " move exception: " + ex.Message);
            }
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
            result = await MoveStageAxis(BinStageAxis.NgBinZ, NgStage.Recipe.AvoidPositionZ, bFine);
            if (result != 0) return result;
            return await MoveStageAxis(BinStageAxis.VisionX, Recipe.VisionX.AvoidPosition, bFine);
        }

        public async Task<int> MoveToStageLoadPosition(BinSide side, bool bFine = false)
        {
            Recipe.EnsurePositionObjects();
            if (side == BinSide.Ng)
            {
                int result = await MoveStageAxis(BinStageAxis.NgBinY, Recipe.NGStageY.LoadPosition, bFine);
                if (result != 0) return result;
                return await MoveStageAxis(BinStageAxis.NgBinZ, NgStage.Recipe.WorkPositionZ, bFine);
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
            BaseAxis item = ResolveStageAxis(axis);
            return await WaitUntilAsync(() => !item.IsMoving && item.IsInPosition && !item.IsAlarm, timeoutMs);
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

        private BaseAxis ResolveStageAxis(BinStageAxis axis)
        {
            switch (axis)
            {
                case BinStageAxis.NgBinY: return NgStage.StageY;
                case BinStageAxis.NgBinZ: return NgStage.StageZ;
                case BinStageAxis.GoodBinY: return GoodStage.StageY;
                case BinStageAxis.GoodBinZ: return GoodStage.StageZ;
                case BinStageAxis.VisionX: return OutputCameraX;
                default: throw new ArgumentOutOfRangeException("axis");
            }
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

            if (ReferenceEquals(axis, NgStage.StageZ))
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
                case BinStageAxis.NgBinY:
                    return Recipe.NGStageY;
                case BinStageAxis.NgBinZ:
                    throw new InvalidOperationException("NgStage Z teaching positions are defined in StageModuleRecipe, not OutputStageRecipe.");
                case BinStageAxis.GoodBinY:
                    return Recipe.GoodStageY;
                case BinStageAxis.GoodBinZ:
                    return Recipe.GoodStageZ;
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
            EventLogger.Write(EventKind.Alarm, "QMC", code, message);
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
        // 구현 보조 주석입니다.

        /// <summary>
        ///
        /// </summary>
        /// <param name="value">입력 값입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        private StageModule GetOppositeStage(StageModule target)
        {
            return ReferenceEquals(target, GoodStage) ? NgStage : GoodStage;
        }

        /// <summary>
        ///
        ///
        ///
        ///
        /// <para>
        ///
        ///
        /// </para>
        /// </summary>
        /// <param name="value">입력 값입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        private async Task<bool> EnsureOppositeStageAvoidedAsync(StageModule targetStage)
        {
            StageModule opposite = GetOppositeStage(targetStage);

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
        // 구현 보조 주석입니다.
        // ======================================================================

        /// <summary>
        ///
        /// <para>
        ///
        ///
        ///
        ///
        ///
        /// </para>
        /// </summary>
        /// <param name="value">입력 값입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> ReceiveDieAsync(ReceiveDieRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            StageModule target = request.Grade == DieGrade.Good ? GoodStage : NgStage;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> ReceiveDie: Grade=" + request.Grade +
                ", TpuOffsetY=" + request.TpuOffsetY.ToString("F3") +
                ", VisionOffsetY=" + request.VisionOffsetY.ToString("F3"));

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            bool interlockOk = await EnsureOppositeStageAvoidedAsync(target);
            if (!interlockOk)
                return false;

            // 구현 보조 주석입니다.
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' StageZ 작업 위치로 상승 중...");

            bool workZOk = await target.MoveToWorkPositionAsync();
            if (!workZOk)
                return false;

            // 구현 보조 주석입니다.
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

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            Tpu.NotifyPlaceReady();
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> TPU에 Place 준비 완료 신호 전송 완료.");

            return true;
        }

        // ======================================================================
        // 구현 보조 주석입니다.
        // ======================================================================

        /// <summary>
        ///
        /// <para>
        ///
        ///
        ///
        ///
        ///
        ///
        /// </para>
        /// </summary>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> InspectBinPositionAsync()
        {
            Console.WriteLine("[INFO]  '" + Name + "' -> Bin 검사 시작. TPU Place 완료 대기 중...");

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
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

            // 구현 보조 주석입니다.
            await SharedRailXMotionRuntime.MoveAxisAsync(OutputCameraX,
                Recipe.VisionX.ProcessPosition, ResolveStageAxisVelocity(OutputCameraX, false));

            if (OutputCameraX.IsAlarm)
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

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            Console.WriteLine("[INFO]  '" + Name + "' -> BinCamera 안착 검사 수행 중...");
            SimulatorBridge.Instance?.CameraExposeFlash("BIN");
            await Task.Delay(20).ContinueWith(_ => { }); // 촬상 소요 시간 시뮬레이션
            Console.WriteLine("[INFO]  '" + Name + "' -> BinCamera 검사 완료. 즉시 후퇴.");

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            await SharedRailXMotionRuntime.MoveAxisAsync(OutputCameraX,
                Recipe.VisionX.AvoidPosition, ResolveStageAxisVelocity(OutputCameraX, false));

            if (OutputCameraX.IsAlarm)
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

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            Tpu.NotifyReadyForNextDie();
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> TPU에 다음 다이 수신 가능 통보 완료.");

            return true;
        }

        // ======================================================================
        // 구현 보조 주석입니다.
        // ======================================================================

        /// <summary>
        ///
        ///
        /// <para>
        ///
        ///
        ///
        ///
        ///
        /// </para>
        /// </summary>
        /// <param name="value">입력 값입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> RequestWaferChangeAsync(DieGrade grade)
        {
            StageModule target = grade == DieGrade.Good ? GoodStage : NgStage;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> WaferChange: Grade=" + grade +
                " Bin Full. 교체 위치 이동 시작.");

            // 구현 보조 주석입니다.
            bool interlockOk = await EnsureOppositeStageAvoidedAsync(target);
            if (!interlockOk)
                return false;

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' StageZ 회피 위치 하강 중...");

            bool avoidOk = await target.MoveToAvoidPositionAsync();
            if (!avoidOk)
                return false;

            // 구현 보조 주석입니다.
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

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
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
        // 구현 보조 주석입니다.
        // ======================================================================

        /// <summary>
        ///
        /// <para>
        ///
        /// </para>
        /// <para>
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        /// </para>
        /// </summary>
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
                "[INFO]  '" + Name + "' -> NgStage StageZ WorkPositionZ 상승 중...");

            bool workZOk = await NgStage.MoveToWorkPositionAsync();
            if (!workZOk)
                return false;

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
                "[INFO]  '" + Name + "' -> NgStage StageZ 회피 위치 하강 중...");

            bool avoidOk = await NgStage.MoveToAvoidPositionAsync();
            if (!avoidOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> ColletCleaning: NgStage StageZ 하강 실패.");
                return false;
            }

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

            // 구현 보조 주석입니다.
            return cleaningOk;
        }
    }
}

