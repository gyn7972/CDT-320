using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using QMC.Common;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;
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
    ///
    /// </summary>
    public class StageModuleSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }

        /// <summary>
        ///
        ///
        /// </summary>
        public double WorkPositionZ   { get; set; } = 10.0;

        /// <summary>
        ///
        ///
        /// </summary>
        public double AvoidPositionZ  { get; set; } = 0.0;

        /// <summary>
        ///
        ///
        /// </summary>
        public double UnloadPositionY { get; set; } = -50.0;

        /// <summary>
        ///
        ///
        /// </summary>
        public double HomePositionY   { get; set; } = 0.0;

        /// <summary>
        ///
        ///
        /// </summary>
        public double CleaningPositionY { get; set; } = 80.0;

        /// <summary>
        ///
        ///
        /// </summary>
        public double PositionTolerance { get; set; } = 0.05;
    }

    /// <summary>구현 설명 주석입니다.</summary>
    public class StageModuleConfig : IConfigData
    {
        [DataMember] public bool bDryRun { get; set; }

        public bool IsSimulationMode
        {
            get { return bDryRun; }
            set { bDryRun = value; }
        }

        /// <summary>구현 설명 주석입니다.</summary>
        public int AvoidCheckIntervalMs { get; set; } = 10;

        /// <summary>구현 설명 주석입니다.</summary>
        public int AvoidTimeoutMs { get; set; } = 3000;
    }

    /// <summary>구현 설명 주석입니다.</summary>
    public class StageModuleRecipe : IRecipeData
    {
        /// <summary>구현 설명 주석입니다.</summary>
        public double YVelocity { get; set; } = 100.0;

        /// <summary>구현 설명 주석입니다.</summary>
        public double ZVelocity { get; set; } = 50.0;
    }

    // --------------------------------------------------------------------------

    /// <summary>구현 설명 주석입니다.</summary>
    public class OutputStageSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }

        /// <summary>
        ///
        ///
        /// </summary>
        public double BinCameraWorkPositionX { get; set; } = 150.0;

        /// <summary>
        ///
        ///
        /// </summary>
        public double BinCameraRetractPositionX { get; set; } = 0.0;

        /// <summary>
        ///
        ///
        /// </summary>
        public double StageBasePositionY { get; set; } = 50.0;
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
        public int TpuPlaceDoneTimeoutMs { get; set; } = 3000;

        /// <summary>구현 설명 주석입니다.</summary>
        public int WaferChangeTimeoutMs { get; set; } = 0;

        /// <summary>구현 설명 주석입니다.</summary>
        public int ColletCleaningTimeoutMs { get; set; } = 10000;
    }

    /// <summary>구현 설명 주석입니다.</summary>
    public class OutputStageRecipe : IRecipeData
    {
        [DataMember] public StageAxisPositions GoodBinY { get; set; } = new StageAxisPositions();
        [DataMember] public StageAxisPositions GoodBinZ { get; set; } = new StageAxisPositions();
        [DataMember] public StageAxisPositions NGBinY { get; set; } = new StageAxisPositions();
        [DataMember] public StageAxisPositions VisionX { get; set; } = new StageAxisPositions();

        /// <summary>구현 설명 주석입니다.</summary>
        public double BinCameraVelocity { get; set; } = 200.0;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsurePositionObjects();
        }

        public void EnsurePositionObjects()
        {
            if (GoodBinY == null) GoodBinY = new StageAxisPositions();
            if (GoodBinZ == null) GoodBinZ = new StageAxisPositions();
            if (NGBinY == null) NGBinY = new StageAxisPositions();
            if (VisionX == null) VisionX = new StageAxisPositions();
        }
    }

    // ==========================================================================
    // 구현 보조 주석입니다.
    // ==========================================================================

    /// <summary>
    ///
    ///
    /// <para>
    ///
    ///
    ///
    /// </para>
    /// <para>
    ///
    /// </para>
    /// </summary>
    public class StageModule : BaseUnit<StageModuleSetup, StageModuleConfig, StageModuleRecipe>
    {
        // ----------------------------------------------------------------------
        // 구현 보조 주석입니다.
        // ----------------------------------------------------------------------

        /// <summary>
        ///
        ///
        /// </summary>
        public BaseAxis StageY { get; private set; }

        /// <summary>
        ///
        ///
        ///
        /// </summary>
        public BaseAxis StageZ { get; private set; }

        // ----------------------------------------------------------------------
        // 구현 보조 주석입니다.

        /// <summary>
        ///
        /// </summary>
        /// <param name="value">입력 값입니다.</param>
        public StageModule(string moduleName) : base(moduleName)
        {
            StageY = AjinFactory.CreateAxis(moduleName + "_StageY");
            StageZ = AjinFactory.CreateAxis(moduleName + "_StageZ");

            // 구현 보조 주석입니다.
            StageY.Setup.SoftLimitPlus = 350.0;
            StageZ.Setup.SoftLimitPlus = 250.0;

            // 구현 보조 주석입니다.
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
            double diff = Math.Abs(StageZ.ActualPosition - Setup.AvoidPositionZ);
            return diff <= Setup.PositionTolerance;
        }

        /// <summary>
        ///
        ///
        /// </summary>
        /// <returns>처리 결과입니다.</returns>
        public async Task<bool> MoveToAvoidPositionAsync()
        {
            // 구현 보조 주석입니다.
            if (IsAtAvoidPosition())
                return true;

            await StageZ.MoveAbsoluteAsync(Setup.AvoidPositionZ, Recipe.ZVelocity);

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
            await StageZ.MoveAbsoluteAsync(Setup.WorkPositionZ, Recipe.ZVelocity);

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
            await StageY.MoveAbsoluteAsync(targetY, Recipe.YVelocity);

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
            return await MoveYAsync(Setup.HomePositionY);
        }
    }

    // ==========================================================================
    // 구현 보조 주석입니다.
    // ==========================================================================

    /// <summary>
    ///
    ///
    ///
    /// <para>
    ///
    ///
    /// </para>
    /// <para>
    ///
    /// <c>OutputStageUnit</c><br/>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// </para>
    /// </summary>
    public class OutputStageUnit : BaseUnit<OutputStageSetup, OutputStageConfig, OutputStageRecipe>
    {
        // ----------------------------------------------------------------------
        // 구현 보조 주석입니다.
        // ----------------------------------------------------------------------

        /// <summary>
        ///
        /// </summary>
        public StageModule GoodStage { get; private set; }

        /// <summary>
        ///
        /// </summary>
        public StageModule NgStage { get; private set; }

        /// <summary>
        ///
        ///
        /// </summary>
        public BaseAxis BinCameraX { get; private set; }

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
            BinCameraX = AjinFactory.CreateAxis("OutputStage_BinCameraX");
            // 구현 보조 주석입니다.
            BinCameraX.Setup.SoftLimitPlus = 350.0;

            // 구현 보조 주석입니다.
            Components.Add(GoodStage);
            Components.Add(NgStage);
            Components.Add(BinCameraX);
        }

        public async Task<int> MoveStageAxis(BinStageAxis axis, double targetPos, bool bFine = false)
        {
            try
            {
                BaseAxis item = ResolveStageAxis(axis);
                double velocity = ResolveStageAxisVelocity(item, bFine);
                EventLogger.Write(EventKind.Event, "QMC", "OS-MOVE", axis + " target=" + targetPos);
                int result = await item.MoveAbsoluteAsync(targetPos, velocity);
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
            int result = await MoveStageAxis(BinStageAxis.GoodBinY, Recipe.GoodBinY.AvoidPosition, bFine);
            if (result != 0) return result;
            result = await GoodStage.StageZ.MoveAbsoluteAsync(Recipe.GoodBinZ.AvoidPosition, GoodStage.Recipe.ZVelocity);
            if (result != 0 || GoodStage.StageZ.IsAlarm) return RaiseOutputStageAlarm("OS-GOOD-Z", "GoodStage Z avoid move failed.");
            result = await MoveStageAxis(BinStageAxis.NgBinY, Recipe.NGBinY.AvoidPosition, bFine);
            if (result != 0) return result;
            result = await NgStage.StageZ.MoveAbsoluteAsync(NgStage.Setup.AvoidPositionZ, NgStage.Recipe.ZVelocity);
            if (result != 0 || NgStage.StageZ.IsAlarm) return RaiseOutputStageAlarm("OS-NG-Z", "NgStage Z avoid move failed.");
            return await MoveStageAxis(BinStageAxis.VisionX, Recipe.VisionX.AvoidPosition, bFine);
        }

        public async Task<int> MoveToStageLoadPosition(BinSide side, bool bFine = false)
        {
            Recipe.EnsurePositionObjects();
            if (side == BinSide.Ng)
            {
                int result = await MoveStageAxis(BinStageAxis.NgBinY, Recipe.NGBinY.LoadPosition, bFine);
                if (result != 0) return result;
                result = await NgStage.StageZ.MoveAbsoluteAsync(NgStage.Setup.WorkPositionZ, NgStage.Recipe.ZVelocity);
                return result == 0 && !NgStage.StageZ.IsAlarm ? 0 : RaiseOutputStageAlarm("OS-NG-Z", "NgStage Z load move failed.");
            }

            int move = await MoveStageAxis(BinStageAxis.GoodBinY, Recipe.GoodBinY.LoadPosition, bFine);
            if (move != 0) return move;
            move = await GoodStage.StageZ.MoveAbsoluteAsync(Recipe.GoodBinZ.LoadPosition, GoodStage.Recipe.ZVelocity);
            return move == 0 && !GoodStage.StageZ.IsAlarm ? 0 : RaiseOutputStageAlarm("OS-GOOD-Z", "GoodStage Z load move failed.");
        }

        public async Task<int> MoveToStageUnloadPosition(BinSide side, bool bFine = false)
        {
            Recipe.EnsurePositionObjects();
            if (side == BinSide.Ng)
                return await MoveStageAxis(BinStageAxis.NgBinY, Recipe.NGBinY.UnloadPosition, bFine);

            int move = await MoveStageAxis(BinStageAxis.GoodBinY, Recipe.GoodBinY.UnloadPosition, bFine);
            if (move != 0) return move;
            move = await GoodStage.StageZ.MoveAbsoluteAsync(Recipe.GoodBinZ.UnloadPosition, GoodStage.Recipe.ZVelocity);
            return move == 0 && !GoodStage.StageZ.IsAlarm ? 0 : RaiseOutputStageAlarm("OS-GOOD-Z", "GoodStage Z unload move failed.");
        }

        public async Task<int> MoveToStageProcessPosition(BinSide side, bool bFine = false)
        {
            Recipe.EnsurePositionObjects();
            int result;
            if (side == BinSide.Ng)
                result = await MoveStageAxis(BinStageAxis.NgBinY, Recipe.NGBinY.ProcessPosition, bFine);
            else
            {
                result = await MoveStageAxis(BinStageAxis.GoodBinY, Recipe.GoodBinY.ProcessPosition, bFine);
                if (result == 0)
                {
                    int z = await GoodStage.StageZ.MoveAbsoluteAsync(Recipe.GoodBinZ.ProcessPosition, GoodStage.Recipe.ZVelocity);
                    if (z != 0 || GoodStage.StageZ.IsAlarm) result = RaiseOutputStageAlarm("OS-GOOD-Z", "GoodStage Z process move failed.");
                }
            }

            if (result != 0) return result;
            return await MoveStageAxis(BinStageAxis.VisionX, Recipe.VisionX.ProcessPosition, bFine);
        }

        public bool IsStageAxisInPosition(BinStageAxis axis, double targetPos, double tolerance)
        {
            return Math.Abs(ResolveStageAxis(axis).ActualPosition - targetPos) <= tolerance;
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
            StageAxisPositions y = side == BinSide.Ng ? Recipe.NGBinY : Recipe.GoodBinY;
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
                case BinStageAxis.VisionX: return BinCameraX;
                default: throw new ArgumentOutOfRangeException("axis");
            }
        }

        private double ResolveStageAxisVelocity(BaseAxis axis, bool bFine)
        {
            if (axis == BinCameraX)
                return Recipe.BinCameraVelocity;

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
                case BinStageAxis.NgBinZ:
                    return Recipe.NGBinY;
                case BinStageAxis.GoodBinY:
                    return Recipe.GoodBinY;
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

            // 구현 보조 주석입니다.
            if (opposite.IsAtAvoidPosition())
            {
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Interlock: '" + opposite.Name +
                    "' 이미 회피 위치입니다. 추가 동작 불필요.");
                return true;
            }

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
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
            double finalY = Setup.StageBasePositionY
                            + request.TpuOffsetY
                            + request.VisionOffsetY;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' StageY 이동. FinalY=" + finalY.ToString("F3") + "mm " +
                "(Base=" + Setup.StageBasePositionY.ToString("F3") +
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
            bool placeDone = await Tpu.WaitPlaceDoneAsync(Config.TpuPlaceDoneTimeoutMs);
            if (!placeDone)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> InspectBin: TPU Place 완료 대기 타임아웃");
                AlarmManager.Raise(
                    AlarmSeverity.Warning,
                    "OS-PLACEDONE",
                    source: Name + ".InspectBinPositionAsync",
                    message: "TPU Place 완료 대기 타임아웃(timeout=" +
                             Config.TpuPlaceDoneTimeoutMs + "ms)");
                return false;
            }

            Console.WriteLine("[INFO]  '" + Name + "' -> TPU 후퇴 확인. BinCamera 진입 중...");

            // 구현 보조 주석입니다.
            await BinCameraX.MoveAbsoluteAsync(
                Setup.BinCameraWorkPositionX, Recipe.BinCameraVelocity);

            if (BinCameraX.IsAlarm)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> InspectBin: BinCameraX 진입 실패.");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "OS-BINCAM",
                    source: Name + ".InspectBinPositionAsync",
                    message: "BinCameraX 검사 위치 진입 실패 (axis code=" +
                             BinCameraX.AlarmCode + ")");
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
            await BinCameraX.MoveAbsoluteAsync(
                Setup.BinCameraRetractPositionX, Recipe.BinCameraVelocity);

            if (BinCameraX.IsAlarm)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> InspectBin: BinCameraX 후퇴 실패.");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "OS-BINCAM",
                    source: Name + ".InspectBinPositionAsync",
                    message: "BinCameraX 대기 위치 후퇴 실패 (axis code=" +
                             BinCameraX.AlarmCode + ")");
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
                "' 교체 위치(Y=" + target.Setup.UnloadPositionY.ToString("F1") +
                "mm)로 이동 중...");

            bool moveOk = await target.MoveYAsync(target.Setup.UnloadPositionY);
            if (!moveOk)
                return false;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' 교체 위치 도달. Unloader에 체인지 요청 전송 (Suspend 시작).");

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            bool changeOk = await Unloader.RequestWaferChangeAsync(grade, Config.WaferChangeTimeoutMs);

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

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            bool interlockOk = await EnsureOppositeStageAvoidedAsync(NgStage);
            if (!interlockOk)
                return false;

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage StageY ??CleaningPositionY=" +
                NgStage.Setup.CleaningPositionY.ToString("F1") + "mm 이동 중...");

            bool cleaningMoveOk = await NgStage.MoveYAsync(NgStage.Setup.CleaningPositionY);
            if (!cleaningMoveOk)
                return false;

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage StageZ WorkPositionZ 상승 중...");

            bool workZOk = await NgStage.MoveToWorkPositionAsync();
            if (!workZOk)
                return false;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> 클리닝 준비 완료. TPU에 콜렛 클리닝 요청 전송.");

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
            bool cleaningOk = await Tpu.RequestColletCleaningAsync(Config.ColletCleaningTimeoutMs);
            if (!cleaningOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> ColletCleaning: TPU 클리닝 타임아웃");
                // 구현 보조 주석입니다.
            }
            else
            {
                Console.WriteLine("[INFO]  '" + Name + "' -> TPU 콜렛 클리닝 완료.");
            }

            // 구현 보조 주석입니다.
            // 구현 보조 주석입니다.
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

