using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    /// <summary>Bin 카세트 리프터와 매핑에 필요한 기구 설정값입니다.</summary>
    public class OutCassetteSetup : ISetupData
    {
        /// <summary>BinLifterZ 대기 위치입니다.</summary>
        public double AvoidPosition { get; set; } = 0.0;

        /// <summary>NG 카세트 첫 슬롯 위치입니다.</summary>
        public double NgFirstSlotPosition { get; set; } = 10.0;

        /// <summary>Good1 카세트 첫 슬롯 위치입니다.</summary>
        public double Good1FirstSlotPosition { get; set; } = 80.0;

        /// <summary>Good2 카세트 첫 슬롯 위치입니다.</summary>
        public double Good2FirstSlotPosition { get; set; } = 160.0;

        /// <summary>Bin 매핑 시작 위치입니다.</summary>
        public double MappingStartPosition { get; set; } = 5.0;

        /// <summary>Bin 매핑 종료 위치입니다.</summary>
        public double MappingEndPosition { get; set; } = 304.0;

        /// <summary>슬롯 간 Z 피치입니다.</summary>
        public double SlotPitch { get; set; } = 6.0;

        /// <summary>카세트당 슬롯 개수입니다.</summary>
        public int SlotCount { get; set; } = 25;

        /// <summary>위치 도달 판정 허용 오차입니다.</summary>
        public double InPositionTolerance { get; set; } = 0.05;
    }

    /// <summary>Bin 카세트 고정 사양 설정입니다.</summary>
    public class OutCassetteConfig : IConfigData
    {
        /// <summary>시뮬레이션 모드 사용 여부입니다.</summary>
        public bool IsSimulationMode { get; set; } = true;
    }

    /// <summary>Bin 카세트 동작 레시피입니다.</summary>
    public class OutCassetteRecipe : IRecipeData
    {
        /// <summary>매핑 시 BinLifterZ 이동 속도입니다.</summary>
        public double ScanVelocity { get; set; } = 20.0;

        /// <summary>일반 BinLifterZ 이동 속도입니다.</summary>
        public double ElevatorVelocity { get; set; } = 80.0;

        /// <summary>슬롯 감지 안정화 대기 시간입니다.</summary>
        public int ScanSettleTimeMs { get; set; } = 100;

        /// <summary>BinLifterZ 이동 완료 대기 시간입니다.</summary>
        public int ElevatorMoveTimeoutMs { get; set; } = 10000;
    }

    /// <summary>NG, Good1, Good2 Bin 카세트 리프터와 매핑 센서를 관리하는 유닛입니다.</summary>
    public class OutCassetteUnit : BaseUnit<OutCassetteSetup, OutCassetteConfig, OutCassetteRecipe>
    {
        private readonly Dictionary<TargetCassette, bool[]> _slotMap =
            new Dictionary<TargetCassette, bool[]>();

        /// <summary>Bin 카세트 Z축 리프터입니다.</summary>
        public BaseAxis BinLifterZ { get; private set; }

        /// <summary>Good Bin 8인치 카세트 확인 센서 0입니다.</summary>
        public BaseDigitalInput GoodBin8CassetteCheck0 { get; private set; }

        /// <summary>Good Bin 8인치 카세트 확인 센서 1입니다.</summary>
        public BaseDigitalInput GoodBin8CassetteCheck1 { get; private set; }

        /// <summary>Good Bin 12인치 카세트 확인 센서 0입니다.</summary>
        public BaseDigitalInput GoodBin12CassetteCheck0 { get; private set; }

        /// <summary>Good Bin 12인치 카세트 확인 센서 1입니다.</summary>
        public BaseDigitalInput GoodBin12CassetteCheck1 { get; private set; }

        /// <summary>NG Bin 8인치 카세트 확인 센서 0입니다.</summary>
        public BaseDigitalInput NgBin8CassetteCheck0 { get; private set; }

        /// <summary>NG Bin 8인치 카세트 확인 센서 1입니다.</summary>
        public BaseDigitalInput NgBin8CassetteCheck1 { get; private set; }

        /// <summary>NG Bin 12인치 카세트 확인 센서 0입니다.</summary>
        public BaseDigitalInput NgBin12CassetteCheck0 { get; private set; }

        /// <summary>NG Bin 12인치 카세트 확인 센서 1입니다.</summary>
        public BaseDigitalInput NgBin12CassetteCheck1 { get; private set; }

        /// <summary>NG Bin 카세트 후방 확인 센서입니다.</summary>
        public BaseDigitalInput NgBinCassetteBw { get; private set; }

        /// <summary>NG Bin 카세트 Lock 확인 센서입니다.</summary>
        public BaseDigitalInput NgBinCassetteLock { get; private set; }

        /// <summary>Bin 링 돌출 감지 센서입니다.</summary>
        public BaseDigitalInput BinRingJutCheck { get; private set; }

        /// <summary>Bin 매핑 센서입니다.</summary>
        public BaseDigitalInput BinMappingSensor { get; private set; }

        /// <summary>대표 카세트 존재 센서입니다.</summary>
        public BaseDigitalInput CassetteExistSensor { get { return GoodBin8CassetteCheck0; } }

        /// <summary>돌출 감지 센서입니다.</summary>
        public BaseDigitalInput ProtrusionSensor { get { return BinRingJutCheck; } }

        /// <summary>슬롯 웨이퍼 감지 센서입니다.</summary>
        public BaseDigitalInput WaferDetectSensor { get { return BinMappingSensor; } }

        /// <summary>최근 매핑된 카세트별 슬롯 상태입니다.</summary>
        public IReadOnlyDictionary<TargetCassette, bool[]> SlotMap { get { return _slotMap; } }

        /// <summary>BinCassetteUnit을 생성하고 축과 센서를 등록합니다.</summary>
        public OutCassetteUnit() : base("BinCassetteUnit")
        {
            BinLifterZ = AjinFactory.CreateAxis("BinLifterZ");
            BinLifterZ.Setup.SoftLimitPlus = 400.0;

            GoodBin8CassetteCheck0 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("GoodBin8CassetteCheck0"));
            GoodBin8CassetteCheck1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("GoodBin8CassetteCheck1"));
            GoodBin12CassetteCheck0 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("GoodBin12CassetteCheck0"));
            GoodBin12CassetteCheck1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("GoodBin12CassetteCheck1"));
            NgBin8CassetteCheck0 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("NgBin8CassetteCheck0"));
            NgBin8CassetteCheck1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("NgBin8CassetteCheck1"));
            NgBin12CassetteCheck0 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("NgBin12CassetteCheck0"));
            NgBin12CassetteCheck1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("NgBin12CassetteCheck1"));
            NgBinCassetteBw = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("NgBinCassetteBw"));
            NgBinCassetteLock = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("NgBinCassetteLock"));
            BinRingJutCheck = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinRingJUTCheck"));
            BinMappingSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput("BinMapping"));

            Components.Add(BinLifterZ);
            Components.Add(GoodBin8CassetteCheck0);
            Components.Add(GoodBin8CassetteCheck1);
            Components.Add(GoodBin12CassetteCheck0);
            Components.Add(GoodBin12CassetteCheck1);
            Components.Add(NgBin8CassetteCheck0);
            Components.Add(NgBin8CassetteCheck1);
            Components.Add(NgBin12CassetteCheck0);
            Components.Add(NgBin12CassetteCheck1);
            Components.Add(NgBinCassetteBw);
            Components.Add(NgBinCassetteLock);
            Components.Add(BinRingJutCheck);
            Components.Add(BinMappingSensor);

            BeginBinMapping();
        }

        /// <summary>BinLifterZ를 지정 위치로 이동합니다.</summary>
        public async Task MoveBinLifterZ(double targetPos, bool bFine = false)
        {
            double velocity = bFine ? Recipe.ScanVelocity * 0.5 : Recipe.ElevatorVelocity;
            await MoveWithProtrusionWatch(targetPos, velocity);
        }

        /// <summary>BinLifterZ를 지정 티칭 위치로 이동합니다.</summary>
        public Task MoveBinLifterZToTeachingPosition(string positionName, bool bFine = false)
        {
            return MoveBinLifterZ(GetTeachingPosition(positionName), bFine);
        }

        /// <summary>BinLifterZ를 대기 위치로 이동합니다.</summary>
        public Task MoveToBinCassetteAvoidPosition(bool bFine = false)
        {
            return MoveBinLifterZ(Setup.AvoidPosition, bFine);
        }

        /// <summary>지정 카세트의 슬롯 위치로 BinLifterZ를 이동합니다.</summary>
        public Task MoveToBinCassetteSlotPosition(TargetCassette cassette, int slotIndex, bool bFine = false)
        {
            return MoveBinLifterZ(CalculateBinCassetteSlotTargetPosition(cassette, slotIndex), bFine);
        }

        /// <summary>Bin 매핑 시작 위치로 이동합니다.</summary>
        public Task MoveToBinCassetteMappingStartPosition(bool bFine = false)
        {
            return MoveBinLifterZ(Setup.MappingStartPosition, bFine);
        }

        /// <summary>Bin 매핑 종료 위치로 이동합니다.</summary>
        public Task MoveToBinCassetteMappingEndPosition(bool bFine = false)
        {
            return MoveBinLifterZ(Setup.MappingEndPosition, bFine);
        }

        /// <summary>BinLifterZ 위치 도달 여부를 확인합니다.</summary>
        public bool IsBinLifterZInPosition(double targetPos, double tolerance)
        {
            return Math.Abs(BinLifterZ.ActualPosition - targetPos) <= tolerance;
        }

        /// <summary>BinLifterZ 이동 완료를 대기합니다.</summary>
        public async Task<bool> WaitBinLifterZMoveDone(int timeoutMs)
        {
            return await WaitUntilAsync(() => !BinLifterZ.IsMoving && BinLifterZ.IsInPosition && !BinLifterZ.IsAlarm, timeoutMs);
        }

        /// <summary>BinLifterZ 지정 티칭 위치 도달을 대기합니다.</summary>
        public async Task<bool> WaitBinLifterZInPosition(string positionName, int timeoutMs)
        {
            double target = GetTeachingPosition(positionName);
            return await WaitUntilAsync(() => IsBinLifterZInPosition(target, Setup.InPositionTolerance), timeoutMs);
        }

        /// <summary>BinLifterZ가 대기 위치에 있는지 확인합니다.</summary>
        public bool IsBinLifterZInAvoidPosition()
        {
            return IsBinLifterZInPosition(Setup.AvoidPosition, Setup.InPositionTolerance);
        }

        /// <summary>BinLifterZ가 지정 카세트 슬롯 위치에 있는지 확인합니다.</summary>
        public bool IsBinLifterZInSlotPosition(TargetCassette cassette, int slotIndex)
        {
            return IsBinLifterZInPosition(CalculateBinCassetteSlotTargetPosition(cassette, slotIndex), Setup.InPositionTolerance);
        }

        /// <summary>BinLifterZ 현재 위치를 지정 티칭 위치로 저장합니다.</summary>
        public void TeachBinLifterZPosition(string positionName)
        {
            SetTeachingPosition(positionName, BinLifterZ.ActualPosition);
        }

        /// <summary>대기 위치를 현재 BinLifterZ 위치로 저장합니다.</summary>
        public void TeachBinLifterZAvoidPosition()
        {
            Setup.AvoidPosition = BinLifterZ.ActualPosition;
        }

        /// <summary>매핑 시작 위치를 현재 BinLifterZ 위치로 저장합니다.</summary>
        public void TeachBinLifterZMappingStartPosition()
        {
            Setup.MappingStartPosition = BinLifterZ.ActualPosition;
        }

        /// <summary>매핑 종료 위치를 현재 BinLifterZ 위치로 저장합니다.</summary>
        public void TeachBinLifterZMappingEndPosition()
        {
            Setup.MappingEndPosition = BinLifterZ.ActualPosition;
        }

        /// <summary>지정 카세트 첫 슬롯 위치를 현재 BinLifterZ 위치로 저장합니다.</summary>
        public void TeachBinLifterZFirstSlotPosition(TargetCassette cassette)
        {
            SetFirstSlotPosition(cassette, BinLifterZ.ActualPosition);
        }

        /// <summary>지정 카세트 슬롯의 BinLifterZ 목표 위치를 계산합니다.</summary>
        public double CalculateBinCassetteSlotTargetPosition(TargetCassette cassette, int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return GetFirstSlotPosition(cassette) + (Setup.SlotPitch * slotIndex);
        }

        /// <summary>BinLifterZ 티칭 데이터가 유효한지 확인합니다.</summary>
        public bool ValidateBinLifterZTeachingComplete()
        {
            return Setup.SlotCount > 0 &&
                   Setup.SlotPitch > 0.0 &&
                   Setup.MappingEndPosition != Setup.MappingStartPosition;
        }

        /// <summary>지정 티칭 위치로 이동한 뒤 위치 도달을 확인합니다.</summary>
        public async Task<bool> MoveToTeachingPositionAndVerify(string positionName, bool bFine = false)
        {
            await MoveBinLifterZToTeachingPosition(positionName, bFine);
            return await WaitBinLifterZInPosition(positionName, Recipe.ElevatorMoveTimeoutMs);
        }

        /// <summary>지정 크기의 Bin 카세트 존재 여부를 확인합니다.</summary>
        public bool IsBinCassetteExist(TargetCassette cassette, int nSize)
        {
            if (cassette == TargetCassette.Ng)
            {
                if (nSize == 8) return NgBin8CassetteCheck0.IsOn || NgBin8CassetteCheck1.IsOn;
                if (nSize == 12) return NgBin12CassetteCheck0.IsOn || NgBin12CassetteCheck1.IsOn;
            }
            else
            {
                if (nSize == 8) return GoodBin8CassetteCheck0.IsOn || GoodBin8CassetteCheck1.IsOn;
                if (nSize == 12) return GoodBin12CassetteCheck0.IsOn || GoodBin12CassetteCheck1.IsOn;
            }

            return IsAnyCassetteSensorOn(cassette);
        }

        /// <summary>지정 크기의 Bin 카세트가 모든 확인 센서에 감지되는지 확인합니다.</summary>
        public bool IsBinCassettePresentAll(TargetCassette cassette, int recipeSize)
        {
            if (cassette == TargetCassette.Ng)
            {
                if (recipeSize == 8) return NgBin8CassetteCheck0.IsOn && NgBin8CassetteCheck1.IsOn;
                if (recipeSize == 12) return NgBin12CassetteCheck0.IsOn && NgBin12CassetteCheck1.IsOn;
            }
            else
            {
                if (recipeSize == 8) return GoodBin8CassetteCheck0.IsOn && GoodBin8CassetteCheck1.IsOn;
                if (recipeSize == 12) return GoodBin12CassetteCheck0.IsOn && GoodBin12CassetteCheck1.IsOn;
            }

            return IsAnyCassetteSensorOn(cassette);
        }

        /// <summary>Bin 링 돌출 감지 여부를 반환합니다.</summary>
        public bool IsBinProtrusionDetected()
        {
            return BinRingJutCheck.IsOn;
        }

        /// <summary>Bin 매핑 센서 ON 여부를 반환합니다.</summary>
        public bool IsBinMapping()
        {
            return BinMappingSensor.IsOn;
        }

        /// <summary>Bin 링 돌출 센서가 해제될 때까지 대기합니다.</summary>
        public async Task<bool> WaitBinJutClear(int timeoutMs)
        {
            return await BinRingJutCheck.WaitUntilStateAsync(false, timeoutMs);
        }

        /// <summary>Bin 매핑 센서가 지정 상태가 될 때까지 대기합니다.</summary>
        public async Task<bool> WaitBinMappingSensor(bool expected, int timeoutMs)
        {
            return await BinMappingSensor.WaitUntilStateAsync(expected, timeoutMs);
        }

        /// <summary>BinLifterZ를 조그 이동합니다.</summary>
        public void ManualMoveBinLifterZJog(int direction, double speed)
        {
            BinLifterZ.MoveJogContinuous(direction, JogSpeedType.Custom, speed);
        }

        /// <summary>BinLifterZ 조그 이동을 정지합니다.</summary>
        public void ManualStopBinLifterZ()
        {
            BinLifterZ.StopJog();
        }

        /// <summary>수동으로 BinLifterZ를 대기 위치로 이동합니다.</summary>
        public Task ManualMoveToBinCassetteAvoidPosition(bool bFine = false)
        {
            return MoveToBinCassetteAvoidPosition(bFine);
        }

        /// <summary>수동으로 BinLifterZ를 지정 슬롯 위치로 이동합니다.</summary>
        public Task ManualMoveToBinCassetteSlotPosition(TargetCassette cassette, int slotIndex, bool bFine = false)
        {
            return MoveToBinCassetteSlotPosition(cassette, slotIndex, bFine);
        }

        /// <summary>수동으로 BinLifterZ를 매핑 시작 위치로 이동합니다.</summary>
        public Task ManualMoveToBinCassetteMappingStartPosition(bool bFine = false)
        {
            return MoveToBinCassetteMappingStartPosition(bFine);
        }

        /// <summary>수동으로 BinLifterZ를 매핑 종료 위치로 이동합니다.</summary>
        public Task ManualMoveToBinCassetteMappingEndPosition(bool bFine = false)
        {
            return MoveToBinCassetteMappingEndPosition(bFine);
        }

        /// <summary>지정 카세트를 스캔하고 슬롯 맵을 갱신합니다.</summary>
        public async Task<bool> ScanCassetteAsync(TargetCassette cassette, int maxSlots, double slotPitch)
        {
            if (!IsAnyCassetteSensorOn(cassette))
            {
                Console.WriteLine("[ALARM] '" + Name + "' ScanCassette: cassette not detected. cassette=" + cassette);
                _slotMap[cassette] = new bool[0];
                return false;
            }

            var map = new bool[maxSlots];
            for (int i = 0; i < maxSlots; i++)
            {
                await MoveBinLifterZ(GetFirstSlotPosition(cassette) + (i * slotPitch));
                if (BinLifterZ.IsAlarm)
                {
                    Console.WriteLine("[ALARM] '" + Name + "' ScanCassette: BinLifterZ move failed at slot " + i + ".");
                    return false;
                }

                await Task.Delay(Recipe.ScanSettleTimeMs).ContinueWith(_ => { });
                map[i] = BinMappingSensor.IsOn;
            }

            _slotMap[cassette] = map;
            return true;
        }

        /// <summary>모든 Bin 카세트를 스캔하고 슬롯 맵을 갱신합니다.</summary>
        public async Task<bool> ScanAllCassettesAsync()
        {
            BeginBinMapping();
            bool ok = true;
            ok &= await ScanCassetteAsync(TargetCassette.Ng, Setup.SlotCount, Setup.SlotPitch);
            ok &= await ScanCassetteAsync(TargetCassette.Good1, Setup.SlotCount, Setup.SlotPitch);
            ok &= await ScanCassetteAsync(TargetCassette.Good2, Setup.SlotCount, Setup.SlotPitch);
            EndBinMapping();
            return ok;
        }

        /// <summary>지정 슬롯 위치로 이동할 준비가 되었는지 확인하고 이동합니다.</summary>
        public async Task<bool> PrepareBinCassetteForFeederLoad(TargetCassette cassette, int slotIndex, int timeoutMs, bool bFine = false)
        {
            if (!CheckBinCassetteMoveReady())
                return false;

            await MoveToBinCassetteSlotPosition(cassette, slotIndex, bFine);
            return await WaitBinLifterZMoveDone(timeoutMs);
        }

        /// <summary>Bin 카세트를 안전 상태로 복귀합니다.</summary>
        public async Task<bool> RecoverBinCassetteToSafeState(int timeoutMs, bool moveAvoid = true)
        {
            if (!await WaitBinJutClear(timeoutMs))
                return false;

            if (moveAvoid)
            {
                await MoveToBinCassetteAvoidPosition();
                return await WaitBinLifterZMoveDone(timeoutMs);
            }

            return true;
        }

        /// <summary>Bin 카세트 축 이동 가능 상태인지 확인합니다.</summary>
        public bool CheckBinCassetteMoveReady()
        {
            return !BinLifterZ.IsAlarm && !IsBinProtrusionDetected();
        }

        /// <summary>Bin 카세트 이송 준비 상태를 확인합니다.</summary>
        public bool CheckBinCassetteTransferReady(TargetCassette cassette, TransferMode mode)
        {
            if (!CheckBinCassetteMoveReady())
                return false;
            if (mode == TransferMode.Load || mode == TransferMode.Unload)
                return IsAnyCassetteSensorOn(cassette);
            return true;
        }

        /// <summary>Bin 카세트 매핑 준비 상태를 확인합니다.</summary>
        public bool CheckBinCassetteMappingReady(TargetCassette cassette)
        {
            return CheckBinCassetteMoveReady() &&
                   IsAnyCassetteSensorOn(cassette) &&
                   ValidateBinLifterZTeachingComplete();
        }

        /// <summary>지정 카세트에서 첫 번째 빈 슬롯을 찾습니다.</summary>
        public int FindFirstEmptySlot(TargetCassette cassette)
        {
            bool[] map;
            if (!_slotMap.TryGetValue(cassette, out map))
                return -1;

            for (int i = 0; i < map.Length; i++)
                if (!map[i]) return i;
            return -1;
        }

        /// <summary>지정 카세트에서 첫 번째 채워진 슬롯을 찾습니다.</summary>
        public int FindFirstFullSlot(TargetCassette cassette)
        {
            bool[] map;
            if (!_slotMap.TryGetValue(cassette, out map))
                return -1;

            for (int i = 0; i < map.Length; i++)
                if (map[i]) return i;
            return -1;
        }

        /// <summary>지정 카세트 슬롯 상태를 갱신합니다.</summary>
        public void UpdateBinCassetteSlotState(TargetCassette cassette, int slotIndex, bool hasWafer)
        {
            ValidateSlotIndex(slotIndex);
            EnsureSlotMap(cassette);
            _slotMap[cassette][slotIndex] = hasWafer;
        }

        /// <summary>Bin 매핑 상태를 초기화합니다.</summary>
        public void BeginBinMapping()
        {
            _slotMap[TargetCassette.Ng] = new bool[0];
            _slotMap[TargetCassette.Good1] = new bool[0];
            _slotMap[TargetCassette.Good2] = new bool[0];
        }

        /// <summary>Bin 매핑 완료 후 후처리를 수행합니다.</summary>
        public void EndBinMapping()
        {
        }

        /// <summary>Bin 카세트 축을 정지하고 사유를 기록합니다.</summary>
        public void StopBinCassetteMotion(string reason)
        {
            BinLifterZ.Stop();
            Console.WriteLine("[STOP] '" + Name + "' " + reason);
        }

        /// <summary>지정 위치로 이동하는 호환 메서드입니다.</summary>
        public Task MoveToTargetSlotAsync(double targetPosition)
        {
            return MoveBinLifterZ(targetPosition);
        }

        private async Task MoveWithProtrusionWatch(double targetPosition, double velocity)
        {
            if (IsBinProtrusionDetected())
            {
                BinLifterZ.EStop();
                throw new InvalidOperationException("'" + Name + "' Move: protrusion sensor is ON.");
            }

            using (var cts = new CancellationTokenSource())
            {
                Task moveTask = BinLifterZ.MoveAbsoluteAsync(targetPosition, velocity);
                Task<bool> watchTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        if (IsBinProtrusionDetected())
                            return true;
                        await Task.Delay(10, cts.Token).ContinueWith(_ => { });
                    }
                    return false;
                }, cts.Token);

                Task first = await Task.WhenAny(moveTask, watchTask);
                if (first == moveTask)
                {
                    cts.Cancel();
                    await watchTask.ContinueWith(_ => { });
                }
                else
                {
                    BinLifterZ.EStop();
                    cts.Cancel();
                    await moveTask.ContinueWith(_ => { });
                    throw new InvalidOperationException("'" + Name + "' Move: protrusion detected while moving.");
                }
            }

            if (BinLifterZ.IsAlarm)
                throw new InvalidOperationException("'" + Name + "' Move: BinLifterZ alarm.");
        }

        private double GetTeachingPosition(string positionName)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) return Setup.AvoidPosition;
            if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase)) return Setup.MappingStartPosition;
            if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase)) return Setup.MappingEndPosition;
            if (string.Equals(positionName, "NgFirstSlot", StringComparison.OrdinalIgnoreCase)) return Setup.NgFirstSlotPosition;
            if (string.Equals(positionName, "Good1FirstSlot", StringComparison.OrdinalIgnoreCase)) return Setup.Good1FirstSlotPosition;
            if (string.Equals(positionName, "Good2FirstSlot", StringComparison.OrdinalIgnoreCase)) return Setup.Good2FirstSlotPosition;
            throw new ArgumentException("Unknown BinLifterZ teaching position: " + positionName, "positionName");
        }

        private void SetTeachingPosition(string positionName, double position)
        {
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase)) Setup.AvoidPosition = position;
            else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase)) Setup.MappingStartPosition = position;
            else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase)) Setup.MappingEndPosition = position;
            else if (string.Equals(positionName, "NgFirstSlot", StringComparison.OrdinalIgnoreCase)) Setup.NgFirstSlotPosition = position;
            else if (string.Equals(positionName, "Good1FirstSlot", StringComparison.OrdinalIgnoreCase)) Setup.Good1FirstSlotPosition = position;
            else if (string.Equals(positionName, "Good2FirstSlot", StringComparison.OrdinalIgnoreCase)) Setup.Good2FirstSlotPosition = position;
            else throw new ArgumentException("Unknown BinLifterZ teaching position: " + positionName, "positionName");
        }

        private double GetFirstSlotPosition(TargetCassette cassette)
        {
            switch (cassette)
            {
                case TargetCassette.Ng: return Setup.NgFirstSlotPosition;
                case TargetCassette.Good1: return Setup.Good1FirstSlotPosition;
                case TargetCassette.Good2: return Setup.Good2FirstSlotPosition;
                default: throw new ArgumentOutOfRangeException("cassette");
            }
        }

        private void SetFirstSlotPosition(TargetCassette cassette, double position)
        {
            switch (cassette)
            {
                case TargetCassette.Ng: Setup.NgFirstSlotPosition = position; break;
                case TargetCassette.Good1: Setup.Good1FirstSlotPosition = position; break;
                case TargetCassette.Good2: Setup.Good2FirstSlotPosition = position; break;
                default: throw new ArgumentOutOfRangeException("cassette");
            }
        }

        private bool IsAnyCassetteSensorOn(TargetCassette cassette)
        {
            if (cassette == TargetCassette.Ng)
            {
                return NgBin8CassetteCheck0.IsOn || NgBin8CassetteCheck1.IsOn ||
                       NgBin12CassetteCheck0.IsOn || NgBin12CassetteCheck1.IsOn;
            }

            return GoodBin8CassetteCheck0.IsOn || GoodBin8CassetteCheck1.IsOn ||
                   GoodBin12CassetteCheck0.IsOn || GoodBin12CassetteCheck1.IsOn;
        }

        private void EnsureSlotMap(TargetCassette cassette)
        {
            bool[] map;
            if (!_slotMap.TryGetValue(cassette, out map) || map == null || map.Length != Setup.SlotCount)
                _slotMap[cassette] = new bool[Setup.SlotCount];
        }

        private void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Setup.SlotCount)
                throw new ArgumentOutOfRangeException("slotIndex", "Slot index is out of bin cassette range.");
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
