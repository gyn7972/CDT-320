using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    /// <summary>Bin Feeder Y축과 실린더 동작에 필요한 기구 설정값입니다.</summary>
    public class BinFeederSetup : ISetupData
    {
        /// <summary>Bin Feeder 대기 위치입니다.</summary>
        public double AvoidPosition { get; set; } = 0.0;

        /// <summary>OutputStage Good 교환 위치입니다.</summary>
        public double GoodStageExchangePositionY { get; set; } = 150.0;

        /// <summary>OutputStage NG 교환 위치입니다.</summary>
        public double NgStageExchangePositionY { get; set; } = 200.0;

        /// <summary>Bin 카세트 삽입 위치입니다.</summary>
        public double CassetteInsertPositionY { get; set; } = 250.0;

        /// <summary>카세트 로딩 기준 위치입니다.</summary>
        public double CassetteLoadBasePosition { get; set; } = 30.0;

        /// <summary>카세트 로딩 위치 피치입니다.</summary>
        public double CassetteLoadPitch { get; set; } = 6.0;

        /// <summary>위치 도달 판정 허용 오차입니다.</summary>
        public double InPositionTolerance { get; set; } = 0.05;
    }

    /// <summary>Bin Feeder 고정 사양 설정입니다.</summary>
    public class BinFeederConfig : IConfigData
    {
        /// <summary>시뮬레이션 모드 사용 여부입니다.</summary>
        public bool IsSimulationMode { get; set; } = true;
    }

    /// <summary>Bin Feeder 동작 레시피입니다.</summary>
    public class BinFeederRecipe : IRecipeData
    {
        /// <summary>Bin Feeder Y축 이동 속도입니다.</summary>
        public double MoveVelocity { get; set; } = 100.0;

        /// <summary>Bin Feeder Y축 이동 완료 대기 시간입니다.</summary>
        public int FeederMoveTimeoutMs { get; set; } = 5000;

        /// <summary>Bin Feeder 실린더 동작 완료 대기 시간입니다.</summary>
        public int CylinderTimeoutMs { get; set; } = 1000;
    }

    /// <summary>Bin Feeder의 Y축, 업다운 실린더, 클램프 실린더를 관리하는 유닛입니다.</summary>
    public class BinFeederUnit : BaseUnit<BinFeederSetup, BinFeederConfig, BinFeederRecipe>
    {
        private readonly Dictionary<string, double> _positionSnapshots = new Dictionary<string, double>();

        /// <summary>Bin Feeder Y축입니다.</summary>
        public BaseAxis FeederY { get; private set; }

        /// <summary>Bin Feeder 상승 확인 센서입니다.</summary>
        public BaseDigitalInput BinFeederUpSensor { get; private set; }

        /// <summary>Bin Feeder 하강 확인 센서입니다.</summary>
        public BaseDigitalInput BinFeederDownSensor { get; private set; }

        /// <summary>Bin Feeder 언클램프 확인 센서입니다.</summary>
        public BaseDigitalInput BinFeederUnclampSensor { get; private set; }

        /// <summary>Bin Feeder 링 또는 웨이퍼 확인 센서입니다.</summary>
        public BaseDigitalInput BinFeederRingCheckSensor { get; private set; }

        /// <summary>Bin Feeder 과부하 확인 센서입니다.</summary>
        public BaseDigitalInput BinFeederOverloadSensor { get; private set; }

        /// <summary>Bin Feeder가 웨이퍼를 보유했는지 판단하는 대표 센서입니다.</summary>
        public BaseDigitalInput WaferClampedSensor { get { return BinFeederRingCheckSensor; } }

        /// <summary>Bin Feeder 업다운 실린더입니다.</summary>
        public BaseCylinder FeederUpDownCyl { get; private set; }

        /// <summary>Bin Feeder 클램프 실린더입니다.</summary>
        public BaseCylinder FeederClampCyl { get; private set; }

        /// <summary>Bin Feeder 상승 출력입니다.</summary>
        public BaseDigitalOutput BinFeederUpOut { get { return FeederUpDownCyl.OutFwd; } }

        /// <summary>Bin Feeder 하강 출력입니다.</summary>
        public BaseDigitalOutput BinFeederDownOut { get { return FeederUpDownCyl.OutBwd; } }

        /// <summary>Bin Feeder 클램프 출력입니다.</summary>
        public BaseDigitalOutput BinFeederClampOut { get { return FeederClampCyl.OutFwd; } }

        /// <summary>Bin Feeder 언클램프 출력입니다.</summary>
        public BaseDigitalOutput BinFeederUnclampOut { get { return FeederClampCyl.OutBwd; } }

        /// <summary>BinFeederUnit을 생성하고 축, 센서, 실린더를 등록합니다.</summary>
        public BinFeederUnit() : base("BinFeederUnit")
        {
            FeederY = AjinFactory.CreateAxis("BinFeederY");
            FeederY.Setup.SoftLimitPlus = 350.0;

            BinFeederUpSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinNFeederUp"));
            BinFeederDownSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinFeederDown"));
            BinFeederUnclampSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinFeederUnclamp"));
            BinFeederRingCheckSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinFeederRing"));
            BinFeederOverloadSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinFeederOverload"));
            FeederUpDownCyl = AjinFactory.CreateCylinder(AjinIoCatalog.CylinderRefs.BinFeederUpDownCyl);
            FeederClampCyl = AjinFactory.CreateCylinder(AjinIoCatalog.CylinderRefs.BinFeederClampCyl);

            Components.Add(FeederY);
            Components.Add(BinFeederUpSensor);
            Components.Add(BinFeederDownSensor);
            Components.Add(BinFeederUnclampSensor);
            Components.Add(BinFeederRingCheckSensor);
            Components.Add(BinFeederOverloadSensor);
            Components.Add(FeederUpDownCyl);
            Components.Add(FeederClampCyl);
        }

        /// <summary>Bin Feeder Y축을 지정 위치로 이동합니다.</summary>
        public async Task MoveBinFeederY(double targetPos, bool bFine = false)
        {
            await FeederY.MoveAbsoluteAsync(targetPos, bFine ? Recipe.MoveVelocity * 0.5 : Recipe.MoveVelocity);
            if (FeederY.IsAlarm)
                throw new InvalidOperationException("'" + Name + "' MoveBinFeederY: FeederY alarm.");
        }

        /// <summary>Bin Feeder Y축을 지정 티칭 위치로 이동합니다.</summary>
        public Task MoveBinFeederYToTeachingPosition(string positionName, bool bFine = false)
        {
            return MoveBinFeederY(GetTeachingPosition(positionName), bFine);
        }

        /// <summary>Bin Feeder를 대기 위치로 이동합니다.</summary>
        public Task MoveToBinFeederAvoidPosition(bool bFine = false)
        {
            return MoveBinFeederY(Setup.AvoidPosition, bFine);
        }

        /// <summary>Bin Feeder를 지정 카세트 로딩 슬롯 위치로 이동합니다.</summary>
        public Task MoveToBinFeederCassetteLoadPosition(int slotIndex, bool bFine = false)
        {
            return MoveBinFeederY(CalculateBinFeederCassetteLoadPosition(slotIndex), bFine);
        }

        /// <summary>Bin Feeder를 지정 카세트 삽입 위치로 이동합니다.</summary>
        public Task MoveToBinFeederCassetteInsertPosition(bool bFine = false)
        {
            return MoveBinFeederY(Setup.CassetteInsertPositionY, bFine);
        }

        /// <summary>Bin Feeder를 Good OutputStage 교환 위치로 이동합니다.</summary>
        public Task MoveToBinFeederGoodStageExchangePosition(bool bFine = false)
        {
            return MoveBinFeederY(Setup.GoodStageExchangePositionY, bFine);
        }

        /// <summary>Bin Feeder를 NG OutputStage 교환 위치로 이동합니다.</summary>
        public Task MoveToBinFeederNgStageExchangePosition(bool bFine = false)
        {
            return MoveBinFeederY(Setup.NgStageExchangePositionY, bFine);
        }

        /// <summary>지정 카세트에 대응하는 OutputStage 교환 위치로 이동합니다.</summary>
        public Task MoveToBinFeederStageExchangePosition(TargetCassette cassette, bool bFine = false)
        {
            return cassette == TargetCassette.Ng
                ? MoveToBinFeederNgStageExchangePosition(bFine)
                : MoveToBinFeederGoodStageExchangePosition(bFine);
        }

        /// <summary>Bin Feeder Y축 이동 완료를 대기합니다.</summary>
        public async Task<bool> WaitBinFeederYMoveDone(int timeoutMs)
        {
            return await WaitUntilAsync(() => !FeederY.IsMoving && FeederY.IsInPosition && !FeederY.IsAlarm, timeoutMs);
        }

        /// <summary>Bin Feeder Y축 지정 티칭 위치 도달을 대기합니다.</summary>
        public async Task<bool> WaitBinFeederYInPosition(string positionName, int timeoutMs)
        {
            double target = GetTeachingPosition(positionName);
            return await WaitUntilAsync(() => IsBinFeederYInPosition(target, Setup.InPositionTolerance), timeoutMs);
        }

        /// <summary>Bin Feeder Y축 위치 도달 여부를 확인합니다.</summary>
        public bool IsBinFeederYInPosition(double targetPos, double tolerance)
        {
            return Math.Abs(FeederY.ActualPosition - targetPos) <= tolerance;
        }

        /// <summary>Bin Feeder가 대기 위치에 있는지 확인합니다.</summary>
        public bool IsBinFeederInAvoidPosition()
        {
            return IsBinFeederYInPosition(Setup.AvoidPosition, Setup.InPositionTolerance);
        }

        /// <summary>Bin Feeder가 지정 카세트 로딩 위치에 있는지 확인합니다.</summary>
        public bool IsBinFeederInCassetteLoadPosition(int slotIndex)
        {
            return IsBinFeederYInPosition(CalculateBinFeederCassetteLoadPosition(slotIndex), Setup.InPositionTolerance);
        }

        /// <summary>Bin Feeder가 상승 상태인지 확인합니다.</summary>
        public bool IsBinFeederUp()
        {
            return BinFeederUpSensor.IsOn;
        }

        /// <summary>Bin Feeder가 하강 상태인지 확인합니다.</summary>
        public bool IsBinFeederDown()
        {
            return BinFeederDownSensor.IsOn;
        }

        /// <summary>Bin Feeder가 클램프 상태인지 확인합니다.</summary>
        public bool IsBinFeederClamp()
        {
            return !BinFeederUnclampSensor.IsOn || BinFeederRingCheckSensor.IsOn;
        }

        /// <summary>Bin Feeder가 언클램프 상태인지 확인합니다.</summary>
        public bool IsBinFeederUnclamp()
        {
            return BinFeederUnclampSensor.IsOn;
        }

        /// <summary>Bin Feeder 링 확인 센서 상태를 반환합니다.</summary>
        public bool IsBinFeederRingCheck()
        {
            return BinFeederRingCheckSensor.IsOn;
        }

        /// <summary>Bin Feeder Y축 현재 위치를 지정 티칭 위치로 저장합니다.</summary>
        public void TeachBinFeederYPosition(string positionName)
        {
            SetTeachingPosition(positionName, FeederY.ActualPosition);
        }

        /// <summary>Bin Feeder 대기 위치를 현재 위치로 저장합니다.</summary>
        public void TeachBinFeederAvoidPosition()
        {
            Setup.AvoidPosition = FeederY.ActualPosition;
        }

        /// <summary>Good Stage 교환 위치를 현재 위치로 저장합니다.</summary>
        public void TeachBinFeederGoodStageExchangePosition()
        {
            Setup.GoodStageExchangePositionY = FeederY.ActualPosition;
        }

        /// <summary>NG Stage 교환 위치를 현재 위치로 저장합니다.</summary>
        public void TeachBinFeederNgStageExchangePosition()
        {
            Setup.NgStageExchangePositionY = FeederY.ActualPosition;
        }

        /// <summary>카세트 삽입 위치를 현재 위치로 저장합니다.</summary>
        public void TeachBinFeederCassetteInsertPosition()
        {
            Setup.CassetteInsertPositionY = FeederY.ActualPosition;
        }

        /// <summary>카세트 로딩 기준 위치를 현재 위치로 저장합니다.</summary>
        public void TeachBinFeederCassetteLoadBasePosition()
        {
            Setup.CassetteLoadBasePosition = FeederY.ActualPosition;
        }

        /// <summary>지정 슬롯의 Bin Feeder 카세트 로딩 위치를 계산합니다.</summary>
        public double CalculateBinFeederCassetteLoadPosition(int slotIndex)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException("slotIndex");
            return Setup.CassetteLoadBasePosition + (Setup.CassetteLoadPitch * slotIndex);
        }

        /// <summary>Bin Feeder 티칭 데이터가 유효한지 확인합니다.</summary>
        public bool ValidateBinFeederTeachingComplete()
        {
            return Setup.CassetteLoadPitch > 0.0 &&
                   Setup.CassetteInsertPositionY != Setup.AvoidPosition &&
                   Setup.GoodStageExchangePositionY != Setup.NgStageExchangePositionY;
        }

        /// <summary>지정 티칭 위치로 이동한 뒤 위치 도달을 확인합니다.</summary>
        public async Task<bool> MoveToTeachingPositionAndVerify(string positionName, bool bFine = false)
        {
            await MoveBinFeederYToTeachingPosition(positionName, bFine);
            return await WaitBinFeederYInPosition(positionName, Recipe.FeederMoveTimeoutMs);
        }

        /// <summary>Bin Feeder 업다운 실린더를 동작합니다.</summary>
        public async Task<bool> SetBinFeederUpDown(bool up)
        {
            if (up)
                return await FeederUpDownCyl.MoveFwdAsync();
            return await FeederUpDownCyl.MoveBwdAsync();
        }

        /// <summary>Bin Feeder 클램프 실린더를 동작합니다.</summary>
        public async Task<bool> SetBinFeederClamp(bool clamp)
        {
            if (clamp)
                return await FeederClampCyl.MoveFwdAsync();
            return await FeederClampCyl.MoveBwdAsync();
        }

        /// <summary>Bin Feeder 상승 확인을 대기합니다.</summary>
        public async Task<bool> WaitBinFeederUp(int timeoutMs)
        {
            return await BinFeederUpSensor.WaitUntilStateAsync(true, timeoutMs);
        }

        /// <summary>Bin Feeder 하강 확인을 대기합니다.</summary>
        public async Task<bool> WaitBinFeederDown(int timeoutMs)
        {
            return await BinFeederDownSensor.WaitUntilStateAsync(true, timeoutMs);
        }

        /// <summary>Bin Feeder 클램프 확인을 대기합니다.</summary>
        public async Task<bool> WaitBinFeederClamp(int timeoutMs)
        {
            return await BinFeederRingCheckSensor.WaitUntilStateAsync(true, timeoutMs);
        }

        /// <summary>Bin Feeder 언클램프 확인을 대기합니다.</summary>
        public async Task<bool> WaitBinFeederUnclamp(int timeoutMs)
        {
            return await BinFeederUnclampSensor.WaitUntilStateAsync(true, timeoutMs);
        }

        /// <summary>Bin Feeder 링 센서 해제를 대기합니다.</summary>
        public async Task<bool> WaitBinFeederRingClear(int timeoutMs)
        {
            return await BinFeederRingCheckSensor.WaitUntilStateAsync(false, timeoutMs);
        }

        /// <summary>Bin Feeder Y축을 조그 이동합니다.</summary>
        public void ManualMoveBinFeederYJog(int direction, double speed)
        {
            FeederY.MoveJogContinuous(direction, JogSpeedType.Custom, speed);
        }

        /// <summary>Bin Feeder Y축 조그 이동을 정지합니다.</summary>
        public void ManualStopBinFeederY()
        {
            FeederY.StopJog();
        }

        /// <summary>수동으로 Bin Feeder를 대기 위치로 이동합니다.</summary>
        public Task ManualMoveToBinFeederAvoidPosition(bool bFine = false)
        {
            return MoveToBinFeederAvoidPosition(bFine);
        }

        /// <summary>수동으로 Bin Feeder를 카세트 로딩 위치로 이동합니다.</summary>
        public Task ManualMoveToBinFeederCassetteLoadPosition(int slotIndex, bool bFine = false)
        {
            return MoveToBinFeederCassetteLoadPosition(slotIndex, bFine);
        }

        /// <summary>수동으로 Bin Feeder를 카세트 삽입 위치로 이동합니다.</summary>
        public Task ManualMoveToBinFeederCassetteInsertPosition(bool bFine = false)
        {
            return MoveToBinFeederCassetteInsertPosition(bFine);
        }

        /// <summary>Bin 카세트에서 Feeder로 웨이퍼를 로드합니다.</summary>
        public async Task<bool> LoadWaferFromCassetteToFeeder(int slotIndex, int timeoutMs, bool bFine = false)
        {
            if (!CheckBinFeederTransferReady(TransferMode.Load))
                return false;

            await MoveToBinFeederCassetteLoadPosition(slotIndex, bFine);
            if (!await WaitBinFeederYMoveDone(timeoutMs))
                return false;

            if (!await SetBinFeederUpDown(true)) return false;
            if (!await SetBinFeederClamp(true)) return false;
            return await WaitBinFeederClamp(timeoutMs);
        }

        /// <summary>Bin Feeder를 OutputStage 교환 위치로 이동합니다.</summary>
        public async Task<bool> MoveBinFeederToStageExchangePosition(TargetCassette cassette, int timeoutMs, bool bFine = false)
        {
            if (!HasWaferOnFeeder())
                return false;

            await MoveToBinFeederStageExchangePosition(cassette, bFine);
            return await WaitBinFeederYMoveDone(timeoutMs);
        }

        /// <summary>Bin Feeder 웨이퍼를 카세트로 되돌립니다.</summary>
        public async Task<bool> ReturnWaferFromFeederToCassette(int slotIndex, int timeoutMs, bool bFine = false)
        {
            await MoveToBinFeederCassetteLoadPosition(slotIndex, bFine);
            if (!await WaitBinFeederYMoveDone(timeoutMs))
                return false;

            if (!await SetBinFeederClamp(false)) return false;
            if (!await SetBinFeederUpDown(false)) return false;
            return await WaitBinFeederUnclamp(timeoutMs);
        }

        /// <summary>Bin Feeder를 안전 상태로 복귀합니다.</summary>
        public async Task<bool> RecoverBinFeederToSafeState(int timeoutMs, bool unclamp = true)
        {
            if (unclamp && !await SetBinFeederClamp(false))
                return false;

            if (!await SetBinFeederUpDown(false))
                return false;

            await MoveToBinFeederAvoidPosition();
            return await WaitBinFeederYMoveDone(timeoutMs);
        }

        /// <summary>Bin Feeder 축 이동 가능 상태인지 확인합니다.</summary>
        public bool CheckBinFeederMoveReady()
        {
            return !FeederY.IsAlarm && !BinFeederOverloadSensor.IsOn;
        }

        /// <summary>Bin Feeder 이송 준비 상태를 확인합니다.</summary>
        public bool CheckBinFeederTransferReady(TransferMode mode)
        {
            if (!CheckBinFeederMoveReady())
                return false;
            if (mode == TransferMode.Load)
                return IsBinFeederDown() && IsBinFeederUnclamp();
            if (mode == TransferMode.Unload)
                return HasWaferOnFeeder();
            return true;
        }

        /// <summary>Bin 카세트 로딩 준비 상태를 확인합니다.</summary>
        public bool CheckBinCassetteLoadReady(int slotIndex, TransferMode mode)
        {
            if (slotIndex < 0)
                return false;
            return CheckBinFeederTransferReady(mode) && ValidateBinFeederTeachingComplete();
        }

        /// <summary>Bin Feeder 공정 상태를 반환합니다.</summary>
        public WaferFeederProcessState GetBinFeederProcessState()
        {
            if (FeederY.IsAlarm || BinFeederOverloadSensor.IsOn)
                return WaferFeederProcessState.Alarm;
            if (FeederY.IsMoving)
                return WaferFeederProcessState.Moving;
            if (HasWaferOnFeeder())
                return WaferFeederProcessState.HasWafer;
            return WaferFeederProcessState.Empty;
        }

        /// <summary>Bin Feeder에 웨이퍼가 있는지 확인합니다.</summary>
        public bool HasWaferOnFeeder()
        {
            return BinFeederRingCheckSensor.IsOn || !BinFeederUnclampSensor.IsOn;
        }

        /// <summary>Bin Feeder가 안전 상태인지 확인합니다.</summary>
        public bool IsBinFeederSafe()
        {
            return !BinFeederOverloadSensor.IsOn && !FeederY.IsAlarm;
        }

        /// <summary>조그 이동 전 인터락을 확인합니다.</summary>
        public bool InterlockBeforeJog()
        {
            return IsBinFeederSafe();
        }

        /// <summary>픽업 전 Bin Feeder 상태를 검증합니다.</summary>
        public bool ValidateBinFeederBeforePickup(TransferPointType type)
        {
            if (!IsBinFeederSafe())
                return false;
            if (type == TransferPointType.Cassette)
                return IsBinFeederDown() || IsBinFeederInCassetteLoadPosition(0);
            return true;
        }

        /// <summary>Bin Feeder 현재 위치 스냅샷을 저장합니다.</summary>
        public void RecordBinFeederPositionSnapshot(string key)
        {
            _positionSnapshots[key] = FeederY.ActualPosition;
        }

        /// <summary>Bin Feeder를 카세트 삽입 교환 위치로 이동합니다.</summary>
        public async Task<bool> MoveToExchangePositionAsync()
        {
            bool cylResult = await SetBinFeederUpDown(true);
            if (!cylResult)
            {
                Console.WriteLine("[ALARM] '" + Name + "' MoveToExchangePosition: feeder up/down timeout.");
                return false;
            }

            cylResult = await SetBinFeederClamp(true);
            if (!cylResult)
            {
                Console.WriteLine("[ALARM] '" + Name + "' MoveToExchangePosition: feeder clamp timeout.");
                return false;
            }

            await MoveBinFeederY(Setup.CassetteInsertPositionY);
            return !FeederY.IsAlarm;
        }

        /// <summary>Bin Feeder를 언클램프 후 대기 위치로 복귀합니다.</summary>
        public async Task<bool> RetractFeederAsync()
        {
            if (!await SetBinFeederClamp(false))
            {
                Console.WriteLine("[ALARM] '" + Name + "' RetractFeeder: feeder unclamp timeout.");
                return false;
            }

            bool released = await WaitBinFeederUnclamp(Recipe.CylinderTimeoutMs);
            if (!released)
            {
                Console.WriteLine("[ALARM] '" + Name + "' RetractFeeder: feeder unclamp sensor timeout.");
                return false;
            }

            await MoveToBinFeederAvoidPosition();
            if (FeederY.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' RetractFeeder: FeederY origin move failed.");
                return false;
            }

            if (!await SetBinFeederUpDown(false))
            {
                Console.WriteLine("[ALARM] '" + Name + "' RetractFeeder: feeder down timeout.");
                return false;
            }

            return true;
        }

        private double GetTeachingPosition(string positionName)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) return Setup.AvoidPosition;
            if (string.Equals(positionName, "GoodStageExchange", StringComparison.OrdinalIgnoreCase)) return Setup.GoodStageExchangePositionY;
            if (string.Equals(positionName, "NgStageExchange", StringComparison.OrdinalIgnoreCase)) return Setup.NgStageExchangePositionY;
            if (string.Equals(positionName, "CassetteInsert", StringComparison.OrdinalIgnoreCase)) return Setup.CassetteInsertPositionY;
            if (string.Equals(positionName, "CassetteLoadBase", StringComparison.OrdinalIgnoreCase)) return Setup.CassetteLoadBasePosition;
            throw new ArgumentException("Unknown BinFeederY teaching position: " + positionName, "positionName");
        }

        private void SetTeachingPosition(string positionName, double position)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) Setup.AvoidPosition = position;
            else if (string.Equals(positionName, "GoodStageExchange", StringComparison.OrdinalIgnoreCase)) Setup.GoodStageExchangePositionY = position;
            else if (string.Equals(positionName, "NgStageExchange", StringComparison.OrdinalIgnoreCase)) Setup.NgStageExchangePositionY = position;
            else if (string.Equals(positionName, "CassetteInsert", StringComparison.OrdinalIgnoreCase)) Setup.CassetteInsertPositionY = position;
            else if (string.Equals(positionName, "CassetteLoadBase", StringComparison.OrdinalIgnoreCase)) Setup.CassetteLoadBasePosition = position;
            else throw new ArgumentException("Unknown BinFeederY teaching position: " + positionName, "positionName");
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
    }
}
