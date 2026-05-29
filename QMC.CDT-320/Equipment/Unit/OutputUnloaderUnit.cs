using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;

namespace QMC.CDT320
{
    // ==========================================================================
    //  카세트 대상 식별 열거형
    // ==========================================================================

    /// <summary>
    /// OutputUnloader가 접근하는 카세트 슬롯 종류.<br/>
    /// <para>
    /// 레이아웃:<br/>
    /// ┌─────────────┐ ← 상단 블록<br/>
    /// │  Good2       │<br/>
    /// │  Good1       │<br/>
    /// ├─────────────┤<br/>
    /// │  Ng          │ ← 하단<br/>
    /// └─────────────┘
    /// </para>
    /// </summary>
    public enum TargetCassette
    {
        /// <summary>하단 NG 카세트.</summary>
        Ng,

        /// <summary>상단 Good 카세트 1번 (하위).</summary>
        Good1,

        /// <summary>상단 Good 카세트 2번 (상위).</summary>
        Good2
    }

    // ==========================================================================
    //  OutputUnloaderUnit 전용 데이터 클래스
    // ==========================================================================

    /// <summary>
    /// OutputUnloaderUnit 기구적 설정값.<br/>
    /// ElevatorZ 각 카세트 기준 위치, 피더 교환 위치 등
    /// 하드웨어 교체 전까지 유지되는 고정 기구값을 담는다.
    /// </summary>
    public class OutputLoaderSetup : ISetupData
    {
        // ── ElevatorZ 카세트별 기준 Z 위치 ──────────────────────────────────

        /// <summary>NG 카세트의 첫 번째(최하단) 슬롯 ElevatorZ 위치 [mm].</summary>
        public double NgFirstSlotPositionZ    { get; set; } = 10.0;

        /// <summary>Good1 카세트의 첫 번째 슬롯 ElevatorZ 위치 [mm].</summary>
        public double Good1FirstSlotPositionZ { get; set; } = 80.0;

        /// <summary>Good2 카세트의 첫 번째 슬롯 ElevatorZ 위치 [mm].</summary>
        public double Good2FirstSlotPositionZ { get; set; } = 160.0;

        /// <summary>
        /// 슬롯 간 Z 피치 [mm].<br/>
        /// 모든 카세트의 슬롯 간격이 동일하다고 가정한다.
        /// </summary>
        public double SlotPitchZ              { get; set; } = 6.0;

        // ── FeederY 위치 ─────────────────────────────────────────────────────

        /// <summary>
        /// 피더 후퇴(홈) Y 위치 [mm].<br/>
        /// 작업이 없을 때 피더가 머무는 기본 위치.
        /// </summary>
        public double FeederHomePositionY     { get; set; } = 0.0;

        /// <summary>
        /// Good OutputStage 교환 FeederY 위치 [mm].<br/>
        /// Good 웨이퍼를 픽업하거나 내려놓기 위해 GoodStage 앞까지 전진하는 위치.
        /// </summary>
        public double GoodStageExchangePositionY { get; set; } = 150.0;

        /// <summary>
        /// NG OutputStage 교환 FeederY 위치 [mm].<br/>
        /// NG 웨이퍼를 픽업하거나 내려놓기 위해 NgStage 앞까지 전진하는 위치.
        /// </summary>
        public double NgStageExchangePositionY   { get; set; } = 200.0;

        /// <summary>
        /// 카세트 삽입/추출 FeederY 위치 [mm].<br/>
        /// 피더가 카세트 슬롯까지 전진하는 위치.
        /// </summary>
        public double CassetteInsertPositionY    { get; set; } = 250.0;

        // ── 카세트 슬롯 수 ───────────────────────────────────────────────────

        /// <summary>카세트당 최대 슬롯 수.</summary>
        public int MaxSlotsPerCassette        { get; set; } = 25;
    }

    /// <summary>OutputUnloaderUnit 고정 사양 파라미터.</summary>
    public class OutputLoaderConfig : IConfigData
    {
        /// <summary>시뮬레이션 모드 여부 (기본값: true).</summary>
        public bool IsSimulationMode { get; set; } = true;

        /// <summary>돌출 감지 센서 폴링 간격 [ms].</summary>
        public int ProtrusionPollIntervalMs   { get; set; } = 10;

        /// <summary>카세트 스캔 시 센서 안정화 대기 시간 [ms].</summary>
        public int ScanSettleTimeMs           { get; set; } = 100;
    }

    /// <summary>OutputUnloaderUnit 공정별 작업 파라미터.</summary>
    public class OutputLoaderRecipe : IRecipeData
    {
        /// <summary>카세트 스캔 시 ElevatorZ 이동 속도 [mm/s].</summary>
        public double ScanVelocity            { get; set; } = 20.0;

        /// <summary>일반 ElevatorZ 이동 속도 [mm/s].</summary>
        public double ElevatorVelocity        { get; set; } = 80.0;

        /// <summary>FeederY 이동 속도 [mm/s].</summary>
        public double FeederVelocity          { get; set; } = 100.0;

        /// <summary>FeederY 이동 타임아웃 [ms].</summary>
        public int FeederMoveTimeoutMs        { get; set; } = 5000;

        /// <summary>ElevatorZ 이동 타임아웃 [ms].</summary>
        public int ElevatorMoveTimeoutMs      { get; set; } = 10000;

        /// <summary>클램프/언클램프 후 센서 확인 타임아웃 [ms].</summary>
        public int ClampSensorTimeoutMs       { get; set; } = 1000;
    }

    // ==========================================================================
    //  OutputUnloaderUnit - 완성 웨이퍼 회수 및 빈 웨이퍼 공급 유닛
    // ==========================================================================

    /// <summary>
    /// 작업 완료된 웨이퍼를 카세트로 회수하고, 빈 웨이퍼를 OutputStage에 공급하는 유닛.<br/>
    /// 기구적 구성은 <see cref="InputLoaderUnit"/>과 동일하며,
    /// 하단에 NG 카세트 1개, 상단 블록에 Good 카세트 2개(총 3개)가 적재된다.
    /// <para>
    /// <b>핵심 인터락 - 돌출 감지(Protrusion Interlock):</b><br/>
    /// <see cref="ProtrusionSensor"/>가 ON이면 카세트 슬롯 밖으로 웨이퍼가 튀어나온 상태이므로
    /// ElevatorZ 이동 전·중에 항상 이 센서를 감시하고, 감지 즉시 <see cref="BaseAxis.EStop"/>을
    /// 호출하여 충돌을 방지한다.
    /// </para>
    /// <para>
    /// 계층 구조:<br/>
    /// <c>OutputUnloaderUnit</c><br/>
    /// ├─ ElevatorZ, FeederY   (SimAxis)<br/>
    /// ├─ ExistSensor_NG/Good1/Good2, ProtrusionSensor,<br/>
    /// │  WaferDetectSensor, WaferClampedSensor  (SimDigitalInput)<br/>
    /// └─ FeederUpDownCyl, FeederClampCyl        (SimCylinder)
    /// </para>
    /// </summary>
    public class OutputUnloaderUnit
        : BaseUnit<OutputLoaderSetup, OutputLoaderConfig, OutputLoaderRecipe>
    {
        // ======================================================================
        //  §1. 하드웨어 컴포넌트 선언
        // ======================================================================

        // ── Motion Axes ───────────────────────────────────────────────────────

        /// <summary>
        /// 카세트 엘리베이터 Z축.<br/>
        /// NG / Good1 / Good2 3개 카세트 전체 높이를 커버한다.
        /// Z 이동 중에는 항상 <see cref="ProtrusionSensor"/>를 동시 감시해야 한다.
        /// </summary>
        public BaseAxis BinElevatorZ { get; private set; }

        /// <summary>
        /// 피더 Y축.<br/>
        /// OutputStage ↔ 카세트 슬롯 사이를 전진/후진하며 웨이퍼를 이송한다.
        /// </summary>
        public BaseAxis FeederY { get; private set; }

        // ── Digital Input Sensors ─────────────────────────────────────────────

        /// <summary>
        /// NG 카세트 안착 감지 센서.<br/>
        /// NG 카세트가 정위치에 안착되어 있으면 ON.
        /// OFF 상태에서는 해당 카세트 관련 동작을 수행해서는 안 된다.
        /// </summary>
        public BaseDigitalInput ExistSensor_NG    { get; private set; }

        /// <summary>
        /// Good1 카세트 안착 감지 센서.<br/>
        /// Good1 카세트가 정위치에 안착되어 있으면 ON.
        /// </summary>
        public BaseDigitalInput ExistSensor_Good1 { get; private set; }

        /// <summary>
        /// Good2 카세트 안착 감지 센서.<br/>
        /// Good2 카세트가 정위치에 안착되어 있으면 ON.
        /// </summary>
        public BaseDigitalInput ExistSensor_Good2 { get; private set; }

        /// <summary>
        /// 자재 돌출 감지 센서 (ElevatorZ 이동 핵심 인터락).<br/>
        /// 카세트 슬롯 밖으로 웨이퍼가 튀어나온 상태에서 ON이 된다.
        /// <b>이 센서가 ON이면 ElevatorZ 이동을 즉시 E-Stop해야 한다.</b>
        /// </summary>
        public BaseDigitalInput ProtrusionSensor  { get; private set; }

        /// <summary>
        /// 웨이퍼 감지 센서 (스캔용).<br/>
        /// 피더가 슬롯 앞에 위치했을 때 해당 슬롯에 웨이퍼 유무를 판정한다.
        /// </summary>
        public BaseDigitalInput WaferDetectSensor { get; private set; }

        /// <summary>
        /// 피더 클램프 확인 센서.<br/>
        /// 피더가 웨이퍼를 확실히 그립(Grip)했을 때 ON.
        /// OFF이면 웨이퍼가 파지되지 않은 것이므로 이송을 중단해야 한다.
        /// </summary>
        public BaseDigitalInput WaferClampedSensor { get; private set; }

        // ── Cylinders ─────────────────────────────────────────────────────────

        /// <summary>
        /// 피더 상/하강 실린더.<br/>
        /// Forward = 하강 (웨이퍼 높이에 맞춤), Backward = 상승 (이동 높이).
        /// </summary>
        public BaseCylinder FeederUpDownCyl { get; private set; }

        /// <summary>
        /// 피더 클램프(그립) 실린더.<br/>
        /// Forward = 클램프(그립), Backward = 언클램프(해제).
        /// </summary>
        public BaseCylinder FeederClampCyl  { get; private set; }

        // ======================================================================
        //  §2. 내부 상태
        // ======================================================================

        /// <summary>
        /// 카세트별 슬롯 점유 맵.<br/>
        /// [TargetCassette][slotIndex] = true이면 해당 슬롯에 웨이퍼가 있음.
        /// </summary>
        private readonly Dictionary<TargetCassette, bool[]> _slotMap;

        // ======================================================================
        //  §3. 생성자
        // ======================================================================

        /// <summary>
        /// <see cref="OutputUnloaderUnit"/>을 초기화하고 모든 하드웨어 컴포넌트를
        /// 생성하여 Composite 트리에 등록한다.
        /// </summary>
        public OutputUnloaderUnit() : base("OutputUnloaderUnit")
        {
            // ── Motion Axes ─────────────────────────────────────────────────
            BinElevatorZ = AjinFactory.CreateAxis("BinLifterZ");
            FeederY   = AjinFactory.CreateAxis("OutputUnloader_FeederY");

            // Stage 27 fix — Default SoftLimitPlus = 200mm 이지만
            //   FeederY 는 CassetteInsertPositionY = 250mm 까지 이동
            //   ElevatorZ 는 Good2 카세트 마지막 슬롯 = 160 + 24*6 = 304mm 까지 이동
            //   따라서 안전 마진 포함 확장 (실보드 운영시 컨피그로 재정의 가능).
            FeederY  .Setup.SoftLimitPlus = 350.0;
            BinElevatorZ.Setup.SoftLimitPlus = 400.0;

            // ── Sensors (DI) ─────────────────────────────────────────────────
            ExistSensor_NG    = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.NgBin8CassetteCheck0);
            ExistSensor_Good1 = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.GoodBin8CassetteCheck0);
            ExistSensor_Good2 = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.GoodBin8CassetteCheck1);
            ProtrusionSensor  = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.BinRingJUTCheck);
            WaferDetectSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.BinMapping);
            WaferClampedSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.BinFeederUnclamp);

            // ── Cylinders ────────────────────────────────────────────────────
            FeederUpDownCyl = AjinFactory.CreateCylinder(AjinIoCatalog.CylinderRefs.BinFeederUpDownCyl);
            FeederClampCyl  = AjinFactory.CreateCylinder(AjinIoCatalog.CylinderRefs.BinFeederClampCyl);

            // ── Composite 트리 등록 ──────────────────────────────────────────
            Components.Add(BinElevatorZ);
            Components.Add(FeederY);
            Components.Add(ExistSensor_NG);
            Components.Add(ExistSensor_Good1);
            Components.Add(ExistSensor_Good2);
            Components.Add(ProtrusionSensor);
            Components.Add(WaferDetectSensor);
            Components.Add(WaferClampedSensor);
            Components.Add(FeederUpDownCyl);
            Components.Add(FeederClampCyl);

            // ── 슬롯 맵 초기화 ───────────────────────────────────────────────
            _slotMap = new Dictionary<TargetCassette, bool[]>
            {
                { TargetCassette.Ng,    new bool[0] },
                { TargetCassette.Good1, new bool[0] },
                { TargetCassette.Good2, new bool[0] },
            };
        }

        // ======================================================================
        //  §4-A. 내부 헬퍼: 카세트 안착 확인
        // ======================================================================

        /// <summary>
        /// 지정 카세트가 정위치에 안착되어 있는지 확인한다.
        /// </summary>
        /// <param name="cassette">확인할 카세트</param>
        /// <returns>안착 확인 시 true.</returns>
        private bool IsCassettePresent(TargetCassette cassette)
        {
            switch (cassette)
            {
                case TargetCassette.Ng:    return ExistSensor_NG.IsOn;
                case TargetCassette.Good1: return ExistSensor_Good1.IsOn;
                case TargetCassette.Good2: return ExistSensor_Good2.IsOn;
                default:                   return false;
            }
        }

        // ======================================================================
        //  §4-B. 내부 헬퍼: ElevatorZ 슬롯 절대 위치 계산
        // ======================================================================

        /// <summary>
        /// 지정 카세트의 지정 슬롯에 해당하는 ElevatorZ 절대 위치를 계산한다.
        /// </summary>
        /// <param name="cassette">타겟 카세트</param>
        /// <param name="slotIndex">슬롯 인덱스 (0-based, 0 = 최하단)</param>
        /// <returns>ElevatorZ 이동 목표 위치 [mm].</returns>
        private double GetSlotPositionZ(TargetCassette cassette, int slotIndex)
        {
            double baseZ;
            switch (cassette)
            {
                case TargetCassette.Ng:    baseZ = Setup.NgFirstSlotPositionZ;    break;
                case TargetCassette.Good1: baseZ = Setup.Good1FirstSlotPositionZ; break;
                case TargetCassette.Good2: baseZ = Setup.Good2FirstSlotPositionZ; break;
                default:
                    throw new ArgumentOutOfRangeException("cassette",
                        "알 수 없는 TargetCassette 값: " + cassette);
            }

            return baseZ + slotIndex * Setup.SlotPitchZ;
        }

        // ======================================================================
        //  §4-C. 내부 헬퍼: FeederY 교환 위치 반환 (카세트 등급별)
        // ======================================================================

        /// <summary>
        /// 지정 카세트 등급에 해당하는 OutputStage 교환 FeederY 위치를 반환한다.
        /// </summary>
        /// <param name="cassette">타겟 카세트</param>
        /// <returns>FeederY 목표 위치 [mm].</returns>
        private double GetStageExchangePositionY(TargetCassette cassette)
        {
            // NG 등급 다이 → NgStage, Good 등급 다이 → GoodStage
            return cassette == TargetCassette.Ng
                ? Setup.NgStageExchangePositionY
                : Setup.GoodStageExchangePositionY;
        }

        // ======================================================================
        //  §4-D. 내부 헬퍼: 돌출 감시 ElevatorZ 이동 (핵심 인터락)
        // ======================================================================

        /// <summary>
        /// <b>[핵심 인터락]</b> ElevatorZ를 목표 위치로 이동하는 동안
        /// <see cref="ProtrusionSensor"/>를 병렬 감시한다.<br/>
        /// 이동 도중 돌출이 감지되면 즉시 <see cref="BaseAxis.EStop"/>을 호출하고
        /// <see cref="InvalidOperationException"/>을 발생시켜 시퀀스를 중단한다.
        /// <para>
        /// 구현 방식:<br/>
        /// <c>Task.WhenAny</c>로 '이동 완료 Task'와 '돌출 감시 Task'를 경쟁시킨다.
        /// 돌출 감시 Task가 먼저 완료되면 EStop을 수행하고 예외를 던진다.
        /// </para>
        /// </summary>
        /// <param name="targetPositionZ">ElevatorZ 목표 위치 [mm]</param>
        /// <param name="velocity">이동 속도 [mm/s]</param>
        /// <exception cref="InvalidOperationException">
        /// 이동 전 또는 이동 중 돌출 감지 시 발생.
        /// </exception>
        private async Task MoveElevatorWithProtrusionGuardAsync(
            double targetPositionZ, double velocity)
        {
            // ── 이동 전 선제 돌출 확인 ────────────────────────────────────────
            if (ProtrusionSensor.IsOn)
            {
                BinElevatorZ.EStop();
                string preMsg =
                    "[ALARM] '" + Name + "' -> ElevatorMove: 이동 전 돌출 감지! E-Stop 실행.";
                Console.WriteLine(preMsg);
                throw new InvalidOperationException(preMsg);
            }

            using (var cts = new CancellationTokenSource())
            {
                // 이동 Task
                Task moveTask = BinElevatorZ.MoveAbsoluteAsync(targetPositionZ, velocity);

                // 돌출 감시 Task: 10ms 주기로 ProtrusionSensor를 폴링하다가
                // ON이 감지되면 즉시 true를 반환한다.
                Task<bool> watchTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        if (ProtrusionSensor.IsOn)
                            return true; // 돌출 감지

                        await Task.Delay(
                            Config.ProtrusionPollIntervalMs,
                            cts.Token).ContinueWith(_ => { });
                    }
                    return false; // 정상 종료 (이동 완료 후 취소됨)
                }, cts.Token);

                // 두 Task 중 먼저 완료되는 쪽을 대기
                Task first = await Task.WhenAny(moveTask, watchTask);

                if (first == moveTask)
                {
                    // ── 정상: 이동 완료, 감시 Task 취소 ─────────────────────
                    cts.Cancel();
                    await watchTask.ContinueWith(_ => { }); // 정리 대기
                }
                else
                {
                    // ── 이상: 돌출 감지가 먼저 → 즉시 E-Stop ─────────────────
                    BinElevatorZ.EStop();
                    cts.Cancel();
                    await moveTask.ContinueWith(_ => { }); // 이동 Task 정리 대기

                    string msg =
                        "[ALARM] '" + Name + "' -> ElevatorMove: 이동 중 돌출 감지! E-Stop 실행.";
                    Console.WriteLine(msg);
                    throw new InvalidOperationException(msg);
                }
            }

            // ── 이동 완료 후 축 알람 확인 ────────────────────────────────────
            if (BinElevatorZ.IsAlarm)
            {
                string alarmMsg =
                    "[ALARM] '" + Name + "' -> ElevatorMove: BinLifterZ 이동 실패 (축 알람).";
                Console.WriteLine(alarmMsg);
                throw new InvalidOperationException(alarmMsg);
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> BinLifterZ 이동 완료: " +
                BinElevatorZ.ActualPosition.ToString("F3") + "mm");
        }

        // ======================================================================
        //  §4-E. 내부 헬퍼: 피더 픽업 (Stage 또는 Cassette에서 웨이퍼 파지)
        // ======================================================================

        /// <summary>
        /// 지정 FeederY 위치로 전진하여 웨이퍼를 파지(클램프)한다.<br/>
        /// <para>
        /// 동작 순서:<br/>
        /// 1. FeederY → targetPositionY (전진)<br/>
        /// 2. FeederUpDownCyl Forward (하강)<br/>
        /// 3. FeederClampCyl Forward (클램프)<br/>
        /// 4. WaferClampedSensor ON 확인<br/>
        /// 5. FeederUpDownCyl Backward (상승)
        /// </para>
        /// </summary>
        /// <param name="targetPositionY">피더 전진 목표 Y 위치 [mm]</param>
        /// <returns>파지 성공 시 true, 중간 실패 시 false.</returns>
        private async Task<bool> PickupWaferAtPositionAsync(double targetPositionY)
        {
            // Step 1: FeederY 전진
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> Pickup: FeederY 전진 → " +
                targetPositionY.ToString("F1") + "mm");

            await FeederY.MoveAbsoluteAsync(targetPositionY, Recipe.FeederVelocity);
            if (FeederY.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> Pickup: FeederY 전진 실패.");
                return false;
            }

            // Step 2: 피더 하강
            Console.WriteLine("[INFO]  '" + Name + "' -> Pickup: 피더 하강.");
            bool downOk = await FeederUpDownCyl.MoveFwdAsync();
            if (!downOk)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> Pickup: 피더 하강 실패.");
                return false;
            }

            // Step 3: 클램프
            Console.WriteLine("[INFO]  '" + Name + "' -> Pickup: 클램프 체결.");
            bool clampOk = await FeederClampCyl.MoveFwdAsync();
            if (!clampOk)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> Pickup: 클램프 실린더 실패.");
                await FeederUpDownCyl.MoveBwdAsync(); // 안전 복귀 시도
                return false;
            }

            // Step 4: WaferClampedSensor ON 확인
            bool clamped = await WaferClampedSensor.WaitUntilStateAsync(
                true, Recipe.ClampSensorTimeoutMs);

            if (!clamped)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> Pickup: 클램프 확인 실패. 웨이퍼 미파지.");
                await FeederClampCyl.MoveBwdAsync();
                await FeederUpDownCyl.MoveBwdAsync();
                return false;
            }

            // Step 5: 피더 상승
            Console.WriteLine("[INFO]  '" + Name + "' -> Pickup: 피더 상승.");
            bool upOk = await FeederUpDownCyl.MoveBwdAsync();
            if (!upOk)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> Pickup: 피더 상승 실패.");
                return false;
            }

            Console.WriteLine("[INFO]  '" + Name + "' -> Pickup: 웨이퍼 파지 완료.");
            return true;
        }

        // ======================================================================
        //  §4-F. 내부 헬퍼: 피더 배출 (Stage 또는 Cassette에 웨이퍼 내려놓기)
        // ======================================================================

        /// <summary>
        /// 지정 FeederY 위치로 전진하여 웨이퍼를 내려놓는다(언클램프).<br/>
        /// <para>
        /// 동작 순서:<br/>
        /// 1. FeederY → targetPositionY (전진)<br/>
        /// 2. FeederUpDownCyl Forward (하강)<br/>
        /// 3. FeederClampCyl Backward (언클램프)<br/>
        /// 4. WaferClampedSensor OFF 확인<br/>
        /// 5. FeederUpDownCyl Backward (상승)<br/>
        /// 6. FeederY → HomePositionY (후진)
        /// </para>
        /// </summary>
        /// <param name="targetPositionY">피더 전진 목표 Y 위치 [mm]</param>
        /// <returns>배출 성공 시 true, 중간 실패 시 false.</returns>
        private async Task<bool> PlaceWaferAtPositionAsync(double targetPositionY)
        {
            // Step 1: FeederY 전진
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> Place: FeederY 전진 → " +
                targetPositionY.ToString("F1") + "mm");

            await FeederY.MoveAbsoluteAsync(targetPositionY, Recipe.FeederVelocity);
            if (FeederY.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> Place: FeederY 전진 실패.");
                return false;
            }

            // Step 2: 피더 하강
            Console.WriteLine("[INFO]  '" + Name + "' -> Place: 피더 하강.");
            bool downOk = await FeederUpDownCyl.MoveFwdAsync();
            if (!downOk)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> Place: 피더 하강 실패.");
                return false;
            }

            // Step 3: 언클램프
            Console.WriteLine("[INFO]  '" + Name + "' -> Place: 클램프 해제.");
            bool unclampOk = await FeederClampCyl.MoveBwdAsync();
            if (!unclampOk)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> Place: 언클램프 실린더 실패.");
                await FeederUpDownCyl.MoveBwdAsync(); // 안전 복귀 시도
                return false;
            }

            // Step 4: WaferClampedSensor OFF 확인
            bool released = await WaferClampedSensor.WaitUntilStateAsync(
                false, Recipe.ClampSensorTimeoutMs);

            if (!released)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> Place: 언클램프 확인 실패. 웨이퍼 미해제.");
                await FeederUpDownCyl.MoveBwdAsync();
                return false;
            }

            // Step 5: 피더 상승
            Console.WriteLine("[INFO]  '" + Name + "' -> Place: 피더 상승.");
            bool upOk = await FeederUpDownCyl.MoveBwdAsync();
            if (!upOk)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> Place: 피더 상승 실패.");
                return false;
            }

            // Step 6: FeederY 홈 후진
            Console.WriteLine("[INFO]  '" + Name + "' -> Place: FeederY 후진(홈).");
            await FeederY.MoveAbsoluteAsync(Setup.FeederHomePositionY, Recipe.FeederVelocity);
            if (FeederY.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> Place: FeederY 후진 실패.");
                return false;
            }

            Console.WriteLine("[INFO]  '" + Name + "' -> Place: 웨이퍼 배출 완료.");
            return true;
        }

        // ======================================================================
        //  §5. Step 1: 전체 카세트 스캔 (슬롯 빈/채움 상태 맵 생성)
        // ======================================================================

        /// <summary>
        /// 3개 카세트(NG / Good1 / Good2)를 순차적으로 스캔하여
        /// 각 슬롯의 빈/채움 상태 맵(<see cref="_slotMap"/>)을 갱신한다.
        /// <para>
        /// <b>스캔 절차 (카세트 1개 기준):</b><br/>
        /// 1. 카세트 안착 센서 확인 → 미안착 시 해당 카세트 스킵.<br/>
        /// 2. ElevatorZ를 첫 번째 슬롯 위치로 이동 (<see cref="MoveElevatorWithProtrusionGuardAsync"/> 사용).<br/>
        /// 3. 각 슬롯마다 ElevatorZ를 SlotPitch만큼 이동 후 <see cref="WaferDetectSensor"/>를 읽어 맵 기록.<br/>
        /// 4. 스캔 완료 후 결과 로그 출력.
        /// </para>
        /// </summary>
        /// <returns>정상 완료 시 true, 돌출 감지 또는 축 알람 시 false.</returns>
        public async Task<bool> ScanAllCassettesAsync()
        {
            Console.WriteLine("[INFO]  '" + Name + "' -> 전체 카세트 스캔 시작.");

            TargetCassette[] cassettes =
            {
                TargetCassette.Ng,
                TargetCassette.Good1,
                TargetCassette.Good2
            };

            foreach (TargetCassette cassette in cassettes)
            {
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> [" + cassette + "] 카세트 스캔 시작.");

                // ── 카세트 안착 확인 ─────────────────────────────────────────
                if (!IsCassettePresent(cassette))
                {
                    Console.WriteLine(
                        "[WARN]  '" + Name + "' -> [" + cassette +
                        "] 카세트 미안착. 스캔 스킵.");
                    _slotMap[cassette] = new bool[0];
                    continue;
                }

                int maxSlots = Setup.MaxSlotsPerCassette;
                bool[] map   = new bool[maxSlots];

                // ── 첫 번째 슬롯(인덱스 0) 위치로 이동 ──────────────────────
                double firstZ = GetSlotPositionZ(cassette, 0);
                try
                {
                    await MoveElevatorWithProtrusionGuardAsync(firstZ, Recipe.ScanVelocity);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> ScanAllCassettes: " + ex.Message);
                    return false;
                }

                // ── 슬롯별 순차 스캔 ─────────────────────────────────────────
                for (int i = 0; i < maxSlots; i++)
                {
                    // 첫 슬롯은 이미 이동 완료, 이후 슬롯은 피치 단위 상대 이동
                    if (i > 0)
                    {
                        try
                        {
                            double nextZ = GetSlotPositionZ(cassette, i);
                            await MoveElevatorWithProtrusionGuardAsync(
                                nextZ, Recipe.ScanVelocity);
                        }
                        catch (InvalidOperationException ex)
                        {
                            Console.WriteLine(
                                "[ALARM] '" + Name + "' -> ScanAllCassettes: " + ex.Message);
                            return false;
                        }
                    }

                    // 센서 안정화 대기
                    await Task.Delay(Config.ScanSettleTimeMs).ContinueWith(_ => { });

                    bool hasWafer = WaferDetectSensor.IsOn;
                    map[i] = hasWafer;

                    Console.WriteLine(
                        "[INFO]  '" + Name + "' -> [" + cassette + "] Slot[" +
                        i.ToString("D2") + "] " +
                        (hasWafer ? "웨이퍼 있음" : "빈 슬롯"));
                }

                _slotMap[cassette] = map;

                int filledCount = 0;
                foreach (bool s in map) { if (s) filledCount++; }

                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> [" + cassette + "] 스캔 완료. " +
                    filledCount + "/" + maxSlots + " 슬롯 점유.");
            }

            Console.WriteLine("[INFO]  '" + Name + "' -> 전체 카세트 스캔 완료.");
            return true;
        }

        // ======================================================================
        //  §6. Step 2: 꽉 찬 웨이퍼 배출 (Stage → Cassette)
        // ======================================================================

        /// <summary>
        /// OutputStage에서 작업 완료된(꽉 찬) 웨이퍼를 회수하여 지정 카세트 슬롯에 삽입한다.
        /// <para>
        /// <b>실행 순서:</b><br/>
        /// 1. 대상 카세트 안착 확인.<br/>
        /// 2. 피더가 빈 상태로 해당 OutputStage 앞까지 전진.<br/>
        ///    (<see cref="PickupWaferAtPositionAsync"/>: 전진 → 하강 → 클램프 → 상승).<br/>
        /// 3. 돌출 감시 포함 ElevatorZ → 지정 카세트 slotIndex 높이로 이동.<br/>
        /// 4. 피더가 카세트 삽입 위치까지 전진하여 웨이퍼를 내려놓음.<br/>
        ///    (<see cref="PlaceWaferAtPositionAsync"/>: 전진 → 하강 → 언클램프 → 상승 → 후진).
        /// </para>
        /// </summary>
        /// <param name="target">웨이퍼를 보관할 카세트</param>
        /// <param name="slotIndex">삽입할 슬롯 인덱스 (0-based)</param>
        /// <returns>성공 시 true, 실패(인터락/알람) 시 false.</returns>
        public async Task<bool> StoreFullWaferAsync(TargetCassette target, int slotIndex)
        {
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> StoreFullWafer: " + target +
                " Slot[" + slotIndex + "] 회수 시작.");

            // ── 카세트 안착 확인 ─────────────────────────────────────────────
            if (!IsCassettePresent(target))
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> StoreFullWafer: [" + target +
                    "] 카세트 미안착. 시퀀스 중단.");
                return false;
            }

            // ── Step 2-1. OutputStage에서 꽉 찬 웨이퍼 픽업 ──────────────────
            // 피더가 해당 OutputStage(Good/NG) 앞까지 전진하여 웨이퍼를 파지한다.
            double stageY = GetStageExchangePositionY(target);

            bool pickOk = await PickupWaferAtPositionAsync(stageY);
            if (!pickOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> StoreFullWafer: OutputStage 픽업 실패.");
                return false;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> StoreFullWafer: 웨이퍼 픽업 완료. " +
                "BinLifterZ → [" + target + "] Slot[" + slotIndex + "] 이동 중...");

            // ── Step 2-2. ElevatorZ → 타겟 슬롯 위치 (돌출 감시 포함) ─────────
            double slotZ = GetSlotPositionZ(target, slotIndex);
            try
            {
                await MoveElevatorWithProtrusionGuardAsync(slotZ, Recipe.ElevatorVelocity);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> StoreFullWafer: " + ex.Message);
                return false;
            }

            // ── Step 2-3. 카세트 삽입 위치로 전진하여 웨이퍼 배출 ────────────
            bool placeOk = await PlaceWaferAtPositionAsync(Setup.CassetteInsertPositionY);
            if (!placeOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> StoreFullWafer: 카세트 삽입 실패.");
                return false;
            }

            // 슬롯 맵 업데이트: 해당 슬롯이 채워짐
            if (_slotMap.ContainsKey(target) && slotIndex < _slotMap[target].Length)
                _slotMap[target][slotIndex] = true;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> StoreFullWafer: [" + target +
                "] Slot[" + slotIndex + "] 웨이퍼 보관 완료.");
            return true;
        }

        // ======================================================================
        //  §7. Step 3: 빈 웨이퍼 공급 (Cassette → Stage)
        // ======================================================================

        /// <summary>
        /// 지정 카세트 슬롯에서 빈 웨이퍼를 꺼내 해당 OutputStage에 공급한다.
        /// <para>
        /// <b>실행 순서:</b><br/>
        /// 1. 대상 카세트 안착 확인.<br/>
        /// 2. 돌출 감시 포함 ElevatorZ → 빈 웨이퍼가 있는 카세트 slotIndex 위치로 이동.<br/>
        /// 3. 피더가 카세트 삽입 위치까지 전진하여 빈 웨이퍼를 파지.<br/>
        ///    (<see cref="PickupWaferAtPositionAsync"/>).<br/>
        /// 4. 피더가 해당 OutputStage 앞까지 이동하여 빈 웨이퍼를 내려놓음.<br/>
        ///    (<see cref="PlaceWaferAtPositionAsync"/>).
        /// </para>
        /// </summary>
        /// <param name="source">빈 웨이퍼를 가져올 카세트</param>
        /// <param name="slotIndex">빈 웨이퍼가 위치한 슬롯 인덱스 (0-based)</param>
        /// <returns>성공 시 true, 실패(인터락/알람) 시 false.</returns>
        public async Task<bool> SupplyEmptyWaferAsync(TargetCassette source, int slotIndex)
        {
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> SupplyEmptyWafer: " + source +
                " Slot[" + slotIndex + "] 공급 시작.");

            // ── 카세트 안착 확인 ─────────────────────────────────────────────
            if (!IsCassettePresent(source))
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> SupplyEmptyWafer: [" + source +
                    "] 카세트 미안착. 시퀀스 중단.");
                return false;
            }

            // ── Step 3-1. ElevatorZ → 소스 슬롯 위치 (돌출 감시 포함) ─────────
            double slotZ = GetSlotPositionZ(source, slotIndex);

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> SupplyEmptyWafer: BinLifterZ → [" +
                source + "] Slot[" + slotIndex + "] 이동 중...");

            try
            {
                await MoveElevatorWithProtrusionGuardAsync(slotZ, Recipe.ElevatorVelocity);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> SupplyEmptyWafer: " + ex.Message);
                return false;
            }

            // ── Step 3-2. 카세트 삽입 위치로 전진하여 빈 웨이퍼 파지 ─────────
            bool pickOk = await PickupWaferAtPositionAsync(Setup.CassetteInsertPositionY);
            if (!pickOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> SupplyEmptyWafer: 카세트 픽업 실패.");
                return false;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> SupplyEmptyWafer: 빈 웨이퍼 파지 완료. " +
                "OutputStage로 이동 중...");

            // ── Step 3-3. OutputStage 앞까지 이동하여 빈 웨이퍼 배출 ─────────
            double stageY = GetStageExchangePositionY(source);

            bool placeOk = await PlaceWaferAtPositionAsync(stageY);
            if (!placeOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> SupplyEmptyWafer: OutputStage 배출 실패.");
                return false;
            }

            // 슬롯 맵 업데이트: 해당 슬롯이 비워짐
            if (_slotMap.ContainsKey(source) && slotIndex < _slotMap[source].Length)
                _slotMap[source][slotIndex] = false;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> SupplyEmptyWafer: [" + source +
                "] Slot[" + slotIndex + "] OutputStage 공급 완료.");
            return true;
        }

        // ======================================================================
        //  §8. Step 4: 통합 웨이퍼 교체 시퀀스
        // ======================================================================

        /// <summary>
        /// OutputStage에서 "Full Bin(웨이퍼 가득 참)" 신호가 왔을 때 실행되는 통합 교체 시퀀스.<br/>
        /// <b>꽉 찬 웨이퍼 회수 → 빈 웨이퍼 공급</b>을 순서대로 수행한다.
        /// <para>
        /// <b>실행 순서:</b><br/>
        /// 1. <see cref="StoreFullWaferAsync"/>: OutputStage의 꽉 찬 웨이퍼를
        ///    <paramref name="storeTarget"/> 카세트의 <paramref name="storeSlotIndex"/> 슬롯에 보관.<br/>
        /// 2. <see cref="SupplyEmptyWaferAsync"/>: <paramref name="supplySource"/> 카세트의
        ///    <paramref name="supplySlotIndex"/> 슬롯에서 빈 웨이퍼를 꺼내 OutputStage에 공급.<br/>
        /// 3. 어느 단계에서든 실패 시 즉시 알람 상태로 전환하고 시퀀스를 중지한다.
        /// </para>
        /// </summary>
        /// <param name="storeTarget">꽉 찬 웨이퍼를 보관할 카세트</param>
        /// <param name="storeSlotIndex">보관 슬롯 인덱스 (0-based)</param>
        /// <param name="supplySource">빈 웨이퍼를 공급할 카세트</param>
        /// <param name="supplySlotIndex">공급 슬롯 인덱스 (0-based)</param>
        /// <returns>교체 완전 성공 시 true, 어느 단계든 실패 시 false.</returns>
        public async Task<bool> ExchangeWaferSequenceAsync(
            TargetCassette storeTarget,  int storeSlotIndex,
            TargetCassette supplySource, int supplySlotIndex)
        {
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> ExchangeWafer: 교체 시퀀스 시작. " +
                "Store=[" + storeTarget + "/Slot" + storeSlotIndex + "], " +
                "Supply=[" + supplySource + "/Slot" + supplySlotIndex + "]");

            // ── Step 4-1. 꽉 찬 웨이퍼 회수 (Stage → Cassette) ──────────────
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> ExchangeWafer: [Step 1] 꽉 찬 웨이퍼 회수 시작.");

            bool storeOk = await StoreFullWaferAsync(storeTarget, storeSlotIndex);
            if (!storeOk)
            {
                // 회수 실패 → 알람 발생 및 시퀀스 즉시 중지
                // 공급 단계로 절대 넘어가지 않는다.
                // (중간 상태에서 빈 웨이퍼를 잘못 공급하면 다이 손실 발생)
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> ExchangeWafer: [Step 1] 회수 실패. " +
                    "교체 시퀀스 중단. 수동 조치 필요.");
                return false;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> ExchangeWafer: [Step 1] 회수 완료.");

            // ── Step 4-2. 빈 웨이퍼 공급 (Cassette → Stage) ──────────────────
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> ExchangeWafer: [Step 2] 빈 웨이퍼 공급 시작.");

            bool supplyOk = await SupplyEmptyWaferAsync(supplySource, supplySlotIndex);
            if (!supplyOk)
            {
                // 공급 실패 → 알람 발생 및 시퀀스 즉시 중지
                // Stage가 빈 상태이므로 TPU 동작을 일시 정지시켜야 한다.
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> ExchangeWafer: [Step 2] 공급 실패. " +
                    "교체 시퀀스 중단. 수동 조치 필요.");
                return false;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> ExchangeWafer: [Step 2] 공급 완료.");

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> ExchangeWafer: 교체 시퀀스 성공적으로 완료.");
            return true;
        }

        // ======================================================================
        //  §9. IOutputUnloaderUnit 공개 슬롯 조회 헬퍼
        // ======================================================================

        /// <summary>
        /// 지정 카세트의 슬롯 맵에서 첫 번째 빈(Empty) 슬롯 인덱스를 반환한다.
        /// </summary>
        /// <param name="cassette">검색 대상 카세트</param>
        /// <returns>빈 슬롯 인덱스. 없으면 -1.</returns>
        public int FindFirstEmptySlot(TargetCassette cassette)
        {
            bool[] map;
            if (!_slotMap.TryGetValue(cassette, out map))
                return -1;

            for (int i = 0; i < map.Length; i++)
            {
                if (!map[i])
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 지정 카세트의 슬롯 맵에서 첫 번째 채워진(Full) 슬롯 인덱스를 반환한다.
        /// </summary>
        /// <param name="cassette">검색 대상 카세트</param>
        /// <returns>채워진 슬롯 인덱스. 없으면 -1.</returns>
        public int FindFirstFullSlot(TargetCassette cassette)
        {
            bool[] map;
            if (!_slotMap.TryGetValue(cassette, out map))
                return -1;

            for (int i = 0; i < map.Length; i++)
            {
                if (map[i])
                    return i;
            }

            return -1;
        }
    }
}
