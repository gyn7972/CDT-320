using QMC.CDT320.Ajin;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QMC.CDT320
{
    /// <summary>수동 조그 이동 방향입니다.</summary>
    public enum Direction
    {
        /// <summary>음의 방향입니다.</summary>
        Minus = -1,
        /// <summary>양의 방향입니다.</summary>
        Plus = 1
    }

    /// <summary>WaferStage 시트에 정의된 축입니다.</summary>
    public enum WaferStageAxis { WaferY, WaferT, WaferExpandingZ, VisionX, NeedleX, NeedleZ, EjectPinZ }

    /// <summary>Picker 시트에 정의된 축입니다.</summary>
    public enum PickerAxis { PickerX, PickerY, PickerT0, PickerZ0, PickerT1, PickerZ1, PickerT2, PickerZ2, PickerT3, PickerZ3 }

    /// <summary>Vision 시트에 정의된 축입니다.</summary>
    public enum VisionAxis { FrontSideVisionY, RearSideVisionY }

    /// <summary>OutputStage 시트에 정의된 축입니다.</summary>
    public enum BinStageAxis { NgBinY, NgBinZ, GoodBinY, GoodBinZ, VisionX }

    /// <summary>Bin 계열 좌우/양불 작업 영역입니다.</summary>
    public enum BinSide { Ng, Good }

    /// <summary>Output/bin cassette target group.</summary>
    public enum TargetCassette { Ng, Good1, Good2 }

    /// <summary>카세트/피더/스테이지 간 이송 동작 모드입니다.</summary>
    public enum TransferMode { Load, Unload, Mapping, Manual }

    /// <summary>이송 전 확인 대상 위치입니다.</summary>
    public enum TransferPointType { Cassette, Stage, Picker }

    /// <summary>피더/스테이지 위치 타입입니다.</summary>
    public enum FeederPositionType { Avoid, CassetteLoad, CassetteUnload, Barcode, StageLoad, StageUnload, Exchange }

    /// <summary>스테이지 위치 타입입니다.</summary>
    public enum BinStagePositionType { Avoid, Load, Process, Unload, SafeRetreat, Map }

    /// <summary>정지/복구 시 출력 안전 정책입니다.</summary>
    public enum FeederSafePolicy { AllOff, HoldClamp, HoldCurrent }

    /// <summary>정지/복구 시 OutputStage 출력 안전 정책입니다.</summary>
    public enum StageSafePolicy { AllOff, HoldClamp, HoldCurrent }

    /// <summary>Unit 내 재료 상태입니다.</summary>
    public enum MaterialState { Empty, Occupied, Processing, Completed, Error }

    /// <summary>카세트 슬롯 내 소재 존재 상태입니다.</summary>
    public enum SlotPresence { Unknown, Empty, Exist }

    /// <summary>카세트 슬롯 단위 공정 상태입니다.</summary>
    public enum ProcessState { Unknown, Ready, Processing, Done, Ng }

    /// <summary>피더 알람 코드입니다.</summary>
    public enum FeederAlarmCode { None, AxisAlarm, MoveTimeout, TeachingMissing, Interlock, Overload, RingMissing }

    /// <summary>스테이지 알람 코드입니다.</summary>
    public enum StageAlarmCode { None, AxisAlarm, MoveTimeout, TeachingMissing, Interlock, MaterialMissing }

    /// <summary>Input wafer cassette 알람 코드입니다.</summary>
    public enum WaferCassetteAlarmCode { None, CassetteMissing, SizeMismatch, ProtrusionDetected, MappingTimeout, MoveTimeout, TeachingMissing }

    /// <summary>Output/bin cassette 알람 코드입니다.</summary>
    public enum CassetteAlarmCode { None, CassetteMissing, SizeMismatch, ProtrusionDetected, MappingTimeout, MoveTimeout, TeachingMissing, LockTimeout }

    /// <summary>Wafer/bin feeder 소재 이송 상태입니다.</summary>
    public enum WaferFeederProcessState { Empty, HasWafer, Moving, Alarm }

    /// <summary>카세트 슬롯 상태입니다.</summary>
    public sealed class WaferSlotState
    {
        public SlotPresence Presence { get; set; }
        public ProcessState Process { get; set; }
    }

    /// <summary>Input wafer cassette 센서 상태입니다.</summary>
    public sealed class WaferCassetteSensorState
    {
        public bool Wafer8CassetteCheck0 { get; set; }
        public bool Wafer8CassetteCheck1 { get; set; }
        public bool Wafer12CassetteCheck0 { get; set; }
        public bool Wafer12CassetteCheck1 { get; set; }
        public bool WaferRingJutCheck { get; set; }
        public bool WaferMapping { get; set; }
        public bool IsCassetteExist { get; set; }
        public bool IsSizeMatched { get; set; }
    }

    /// <summary>Input wafer cassette 소재 슬롯 모음입니다.</summary>
    public sealed class WaferCassetteMaterial
    {
        public WaferCassetteMaterial(int maxSlots)
        {
            MaxSlots = maxSlots;
            Slots = new List<WaferSlotState>();
        }

        public int MaxSlots { get; private set; }
        public List<WaferSlotState> Slots { get; private set; }
    }

    /// <summary>Output/bin cassette 센서 상태입니다.</summary>
    public sealed class BinCassetteSensorState
    {
        public bool GoodBin8CassetteCheck0 { get; set; }
        public bool GoodBin8CassetteCheck1 { get; set; }
        public bool GoodBin12CassetteCheck0 { get; set; }
        public bool GoodBin12CassetteCheck1 { get; set; }
        public bool NgBin8CassetteCheck0 { get; set; }
        public bool NgBin8CassetteCheck1 { get; set; }
        public bool NgBin12CassetteCheck0 { get; set; }
        public bool NgBin12CassetteCheck1 { get; set; }
        public bool NgBinCassetteBw { get; set; }
        public bool NgBinCassetteLock { get; set; }
        public bool BinRingJutCheck { get; set; }
        public bool BinMapping { get; set; }
        public bool IsGoodCassetteExist { get; set; }
        public bool IsNgCassetteExist { get; set; }
        public bool IsSizeMatched { get; set; }
    }

    /// <summary>Output/bin cassette 소재 슬롯 모음입니다.</summary>
    public sealed class BinCassetteMaterial
    {
        public BinCassetteMaterial(int maxSlots)
        {
            MaxSlots = maxSlots;
            Slots = new List<WaferSlotState>();
        }

        public int MaxSlots { get; private set; }
        public List<WaferSlotState> Slots { get; private set; }
    }

    /// <summary>엑셀 Sheet 기반 Unit에서 공통으로 사용하는 Setup 데이터입니다.</summary>
    public class UnitDefinedSetup : ISetupData
    {
        /// <summary>티칭 위치 테이블입니다.</summary>
        public Dictionary<string, double> TeachingPositions { get; set; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        /// <summary>일반 이동 속도입니다.</summary>
        public double MoveVelocity { get; set; } = 50.0;

        /// <summary>미세 이동 속도 비율입니다.</summary>
        public double FineVelocityScale { get; set; } = 0.5;

        /// <summary>위치 도착 허용 오차입니다.</summary>
        public double InPositionTolerance { get; set; } = 0.05;

        /// <summary>최대 Die 개수입니다.</summary>
        public int MaxDieCount { get; set; } = 10000;

        /// <summary>최대 Collet 개수입니다.</summary>
        public int MaxColletCount { get; set; } = 4;
    }

    /// <summary>엑셀 Sheet 기반 Unit에서 공통으로 사용하는 Config 데이터입니다.</summary>
    public class UnitDefinedConfig : IConfigData
    {
        /// <summary>시뮬레이션 모드 여부입니다.</summary>
        public bool IsSimulationMode { get; set; } = true;
    }

    /// <summary>엑셀 Sheet 기반 Unit에서 공통으로 사용하는 Recipe 데이터입니다.</summary>
    public class UnitDefinedRecipe : IRecipeData
    {
        /// <summary>축 이동 제한 시간입니다.</summary>
        public int MoveTimeoutMs { get; set; } = 5000;

        /// <summary>I/O 동작 제한 시간입니다.</summary>
        public int IoTimeoutMs { get; set; } = 1000;

        /// <summary>Blow 유지 시간입니다.</summary>
        public int BlowTimeMs { get; set; } = 100;
    }

    /// <summary>엑셀 시트 정의 기반 Unit 공통 기능을 제공합니다.</summary>
    public abstract class UnitDefined<TAxis> : BaseUnit<UnitDefinedSetup, UnitDefinedConfig, UnitDefinedRecipe>, IUnitJogController
        where TAxis : struct
    {
        private readonly Dictionary<TAxis, BaseAxis> _axes = new Dictionary<TAxis, BaseAxis>();
        private readonly Dictionary<string, BaseDigitalInput> _inputs = new Dictionary<string, BaseDigitalInput>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, BaseDigitalOutput> _outputs = new Dictionary<string, BaseDigitalOutput>(StringComparer.OrdinalIgnoreCase);

        /// <summary>시트 정의 Unit을 생성합니다.</summary>
        protected UnitDefined(string name) : base(name) { }

        /// <summary>등록된 축 목록입니다.</summary>
        public IReadOnlyDictionary<TAxis, BaseAxis> Axes { get { return _axes; } }

        /// <summary>등록된 입력 목록입니다.</summary>
        public IReadOnlyDictionary<string, BaseDigitalInput> Inputs { get { return _inputs; } }

        /// <summary>등록된 출력 목록입니다.</summary>
        public IReadOnlyDictionary<string, BaseDigitalOutput> Outputs { get { return _outputs; } }

        public bool CanHandleJogAxis(BaseAxis axis)
        {
            TAxis unitAxis;
            return TryResolveAxis(axis, out unitAxis);
        }

        public async Task<int> JogStepAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed,
            double axisStepDistance)
        {
            TAxis unitAxis;
            if (!TryResolveAxis(axis, out unitAxis))
                return -1;

            double signedDistance = (direction < 0 ? -1.0 : 1.0) * Math.Abs(axisStepDistance);
            double target = axis.ActualPosition + signedDistance;
            await MoveAxisAsync(unitAxis, target, speedType == JogSpeedType.Fine);
            return 0;
        }

        public Task<int> JogContinuousAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed)
        {
            TAxis unitAxis;
            if (!TryResolveAxis(axis, out unitAxis))
                return Task.FromResult(-1);

            double speed = UnitJogVelocityResolver.Resolve(axis, speedType, customSpeed);
            ManualMoveAxisJog(unitAxis, direction < 0 ? Direction.Minus : Direction.Plus, speed);
            return Task.FromResult(0);
        }

        public Task<int> StopJogAsync(BaseAxis axis)
        {
            TAxis unitAxis;
            if (!TryResolveAxis(axis, out unitAxis))
                return Task.FromResult(-1);

            ManualStopAxis(unitAxis);
            return Task.FromResult(0);
        }

        /// <summary>축을 등록하고 Components에 추가합니다.</summary>
        protected BaseAxis RegisterAxis(TAxis axis, string axisName)
        {
            var item = AjinFactory.CreateAxis(axisName);
            _axes[axis] = item;
            Components.Add(item);
            return item;
        }

        private bool TryResolveAxis(BaseAxis axis, out TAxis unitAxis)
        {
            foreach (KeyValuePair<TAxis, BaseAxis> pair in _axes)
            {
                if (ReferenceEquals(axis, pair.Value))
                {
                    unitAxis = pair.Key;
                    return true;
                }
            }

            unitAxis = default(TAxis);
            return false;
        }

        /// <summary>입력 포트를 등록하고 Components에 추가합니다.</summary>
        protected BaseDigitalInput RegisterInput(string key, string catalogName)
        {
            var item = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput(catalogName));
            _inputs[key] = item;
            Components.Add(item);
            return item;
        }

        /// <summary>출력 포트를 등록하고 Components에 추가합니다.</summary>
        protected BaseDigitalOutput RegisterOutput(string key, string catalogName)
        {
            var item = AjinFactory.CreateDigitalOutput(AjinIoCatalog.FindOutput(catalogName));
            _outputs[key] = item;
            Components.Add(item);
            return item;
        }

        /// <summary>축 객체를 반환합니다.</summary>
        public BaseAxis GetAxis(TAxis axis)
        {
            BaseAxis item;
            if (!_axes.TryGetValue(axis, out item))
                throw new ArgumentException("등록되지 않은 축입니다: " + axis);
            return item;
        }

        /// <summary>축을 지정 좌표로 이동합니다.</summary>
        public async Task MoveAxisAsync(TAxis axis, double targetPos, bool bFine = false)
        {
            var item = GetAxis(axis);
            await item.MoveAbsoluteAsync(targetPos, bFine ? Setup.MoveVelocity * Setup.FineVelocityScale : Setup.MoveVelocity);
            if (item.IsAlarm)
                throw new InvalidOperationException("'" + Name + "' axis alarm: " + axis);
        }

        /// <summary>복수 축을 지정 좌표로 병렬 이동합니다.</summary>
        public async Task MoveAxesAsync(IDictionary<TAxis, double> targets, bool bFine = false)
        {
            var tasks = new List<Task>();
            foreach (var pair in targets)
                tasks.Add(MoveAxisAsync(pair.Key, pair.Value, bFine));
            await Task.WhenAll(tasks);
        }

        /// <summary>축을 티칭 위치로 이동합니다.</summary>
        public Task MoveAxisToTeachingPositionAsync(TAxis axis, string positionName, bool bFine = false)
        {
            return MoveAxisAsync(axis, GetTeachingPosition(axis, positionName), bFine);
        }

        /// <summary>축이 지정 좌표에 도착했는지 확인합니다.</summary>
        public bool IsAxisInPosition(TAxis axis, double targetPos, double tolerance)
        {
            return Math.Abs(GetAxis(axis).ActualPosition - targetPos) <= tolerance && !GetAxis(axis).IsAlarm;
        }

        /// <summary>축 이동 완료를 대기합니다.</summary>
        public async Task<bool> WaitAxisMoveDone(TAxis axis, int timeoutMs)
        {
            AxisMoveWaitResult waitResult = await WaitAxisMoveDoneInPosition(axis, timeoutMs).ConfigureAwait(false);
            return waitResult.Success;
        }

        /// <summary>축 이동 완료와 목표 위치 도착을 상세 결과로 대기합니다.</summary>
        public async Task<AxisMoveWaitResult> WaitAxisMoveDoneInPosition(TAxis axis, int timeoutMs)
        {
            var item = GetAxis(axis);
            return await WaitAxisMoveDoneInPosition(axis, item.CommandPosition, timeoutMs).ConfigureAwait(false);
        }

        /// <summary>축 이동 완료와 지정 목표 위치 도착을 상세 결과로 대기합니다.</summary>
        public async Task<AxisMoveWaitResult> WaitAxisMoveDoneInPosition(TAxis axis, double targetPos, int timeoutMs)
        {
            var item = GetAxis(axis);
            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : Setup.InPositionTolerance;
            return await AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                item,
                targetPos,
                tolerance,
                timeoutMs,
                0).ConfigureAwait(false);
        }

        /// <summary>축이 티칭 위치에 도착했는지 확인합니다.</summary>
        public bool IsAxisInTeachingPosition(TAxis axis, string positionName)
        {
            return IsAxisInPosition(axis, GetTeachingPosition(axis, positionName), Setup.InPositionTolerance);
        }

        /// <summary>축이 티칭 위치에 도착할 때까지 대기합니다.</summary>
        public async Task<bool> WaitAxisInTeachingPosition(TAxis axis, string positionName, int timeoutMs)
        {
            AxisMoveWaitResult waitResult = await WaitAxisMoveDoneInPosition(
                axis,
                GetTeachingPosition(axis, positionName),
                timeoutMs).ConfigureAwait(false);
            return waitResult.Success;
        }

        /// <summary>현재 축 위치를 티칭 위치로 저장합니다.</summary>
        public void TeachAxisPosition(TAxis axis, string positionName)
        {
            Setup.TeachingPositions[BuildTeachingKey(axis, positionName)] = GetAxis(axis).ActualPosition;
        }

        /// <summary>티칭 위치 값을 반환합니다.</summary>
        public double GetTeachingPosition(TAxis axis, string positionName)
        {
            double value;
            if (!Setup.TeachingPositions.TryGetValue(BuildTeachingKey(axis, positionName), out value))
                value = 0.0;
            return value;
        }

        /// <summary>티칭 위치가 등록되어 있는지 확인합니다.</summary>
        public bool HasTeachingPosition(TAxis axis, string positionName)
        {
            return Setup.TeachingPositions.ContainsKey(BuildTeachingKey(axis, positionName));
        }

        /// <summary>티칭 위치 이동과 도착 확인을 수행합니다.</summary>
        public async Task<bool> MoveToTeachingPositionAndVerify(TAxis axis, string positionName, bool bFine = false)
        {
            await MoveAxisToTeachingPositionAsync(axis, positionName, bFine);
            return await WaitAxisInTeachingPosition(axis, positionName, Recipe.MoveTimeoutMs);
        }

        /// <summary>출력을 제어합니다.</summary>
        public void SetOutput(string key, bool on)
        {
            BaseDigitalOutput output;
            if (!_outputs.TryGetValue(key, out output))
                return;
            if (on) output.On(); else output.Off();
        }

        /// <summary>입력 상태를 확인합니다.</summary>
        public bool IsInputOn(string key)
        {
            BaseDigitalInput input;
            return _inputs.TryGetValue(key, out input) && input.IsOn;
        }

        /// <summary>입력이 기대 상태가 될 때까지 대기합니다.</summary>
        public async Task<bool> WaitInputState(string key, bool expected, int timeoutMs)
        {
            BaseDigitalInput input;
            if (_inputs.TryGetValue(key, out input) && input != null)
                return await input.WaitUntilStateAsync(expected, timeoutMs);

            return false;
        }

        /// <summary>축을 수동 조그 이동합니다.</summary>
        public void ManualMoveAxisJog(TAxis axis, Direction dir, double speed)
        {
            GetAxis(axis).MoveJogContinuous((int)dir, JogSpeedType.Custom, speed);
        }

        /// <summary>축 수동 조그를 정지합니다.</summary>
        public void ManualStopAxis(TAxis axis)
        {
            GetAxis(axis).StopJog();
        }

        /// <summary>모든 축과 출력을 안전 정지합니다.</summary>
        public void StopMotionAndOutputs(string reason)
        {
            foreach (var axis in _axes.Values)
                axis.StopJog();
            foreach (var output in _outputs.Values)
                output.Off();
        }

        /// <summary>티칭 키를 생성합니다.</summary>
        protected static string BuildTeachingKey(TAxis axis, string positionName)
        {
            return axis + "." + positionName;
        }

        /// <summary>조건이 만족될 때까지 대기합니다.</summary>
        protected static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs)
        {
            DateTime start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
            {
                if (condition())
                    return true;
                await Task.Delay(10).ContinueWith(_ => { });
            }
            return condition();
        }
    }
}
