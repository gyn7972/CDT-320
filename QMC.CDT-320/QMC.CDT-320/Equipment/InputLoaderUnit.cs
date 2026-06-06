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
    // ??????????????????????????????????????????????????????????????????????????
    //  InputLoaderUnit 전용 데이터 클래스
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>
    /// InputLoaderUnit의 기구적 설정값.<br/>
    /// 카세트 슬롯 피치, 매핑 시작 위치 등 하드웨어 교체 전까지 유지되는 값을 담는다.
    /// </summary>
    public class InputLoaderSetup : ISetupData
    {
        /// <summary>카세트의 첫 번째(최하단) 슬롯 절대 위치 [mm].</summary>
        public double FirstSlotPosition { get; set; } = 10.0;

        /// <summary>피더가 웨이퍼를 InputStage로 내보내는 교환 위치(Y축 절대값) [mm].</summary>
        public double ExchangePositionY { get; set; } = 150.0;
    }

    /// <summary>
    /// InputLoaderUnit의 고정 사양 파라미터.
    /// </summary>
    public class InputLoaderConfig : IConfigData
    {
        /// <summary>
        /// 시뮬레이션 모드 여부 (기본값: true).<br/>
        /// true이면 실제 하드웨어 없이 시퀀스 검증만 수행한다.
        /// </summary>
        public bool IsSimulationMode { get; set; } = true;
    }

    /// <summary>
    /// InputLoaderUnit의 공정별 작업 파라미터.
    /// </summary>
    public class InputLoaderRecipe : IRecipeData
    {
        /// <summary>카세트 매핑 시 슬롯 간 이동 속도 [mm/s].</summary>
        public double ScanVelocity { get; set; } = 20.0;

        /// <summary>카세트 슬롯 이동 후 센서 안정화 대기 시간 [ms].</summary>
        public int ScanSettleTimeMs { get; set; } = 100;

        /// <summary>엘리베이터 일반 이동 타임아웃 [ms].</summary>
        public int ElevatorMoveTimeoutMs { get; set; } = 10000;

        /// <summary>피더 이동 타임아웃 [ms].</summary>
        public int FeederMoveTimeoutMs { get; set; } = 5000;
    }

    // ??????????????????????????????????????????????????????????????????????????
    //  InputLoaderUnit
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>
    /// Input Loader 유닛.<br/>
    /// Input Cassette에서 웨이퍼를 슬롯 단위로 꺼내어 InputStage로 공급한다.
    /// <para>
    /// 공정 흐름:<br/>
    /// ScanCassetteAsync → MoveToTargetSlotAsync → MoveToExchangePositionAsync → RetractFeederAsync
    /// </para>
    /// </summary>
    public class InputLoaderUnit : BaseUnit<InputLoaderSetup, InputLoaderConfig, InputLoaderRecipe>
    {
        // ──────────────────────────────────────────────────────────────────────
        //  §1. 하드웨어 컴포넌트 선언
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 카세트 엘리베이터 Z축.<br/>
        /// 카세트를 상하로 이동시켜 목표 슬롯을 피더 높이에 정렬한다.
        /// </summary>
        public BaseAxis ElevatorZ { get; private set; }

        /// <summary>
        /// 웨이퍼 피더 Y축.<br/>
        /// 카세트에서 웨이퍼를 밀어내어 InputStage로 이송하는 전후진 축.
        /// </summary>
        public BaseAxis FeederY { get; private set; }

        /// <summary>
        /// 카세트 유무 감지 센서.<br/>
        /// 로드포트에 카세트가 올바르게 안착되어 있을 때 ON이 된다.
        /// 이 센서가 OFF이면 모든 이송 시퀀스가 시작되어서는 안 된다.
        /// </summary>
        public BaseDigitalInput CassetteExistSensor { get; private set; }

        /// <summary>
        /// 웨이퍼 돌출 감지 센서 (핵심 인터락).<br/>
        /// 카세트 밖으로 웨이퍼가 튀어나온 상태를 감지한다.
        /// ON 상태에서 엘리베이터 이동 시 웨이퍼 파손이 발생하므로 이동을 즉시 금지한다.
        /// </summary>
        public BaseDigitalInput ProtrusionSensor { get; private set; }

        /// <summary>
        /// 웨이퍼 스캔(매핑)용 센서.<br/>
        /// 매핑 시퀀스에서 엘리베이터가 각 슬롯 높이를 통과할 때 웨이퍼 유무를 판별한다.
        /// </summary>
        public BaseDigitalInput WaferDetectSensor { get; private set; }

        /// <summary>
        /// 피더 클램프 확인 센서.<br/>
        /// 피더가 웨이퍼를 확실히 물고 있을 때 ON이 된다.
        /// 이 센서가 OFF이면 이송 중 웨이퍼 낙하 위험이 있으므로 시퀀스를 중단한다.
        /// </summary>
        public BaseDigitalInput WaferClampedSensor { get; private set; }

        /// <summary>
        /// 피더 상하강 실린더.<br/>
        /// 피더 전체를 웨이퍼 높이에 맞게 상/하강시킨다.
        /// </summary>
        public BaseCylinder FeederUpDownCyl { get; private set; }

        /// <summary>
        /// 웨이퍼 클램프(그립) 실린더.<br/>
        /// 피더가 웨이퍼를 파지하거나 해제하는 실린더.
        /// </summary>
        public BaseCylinder FeederClampCyl { get; private set; }

        // ──────────────────────────────────────────────────────────────────────
        //  §2. 내부 상태
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>마지막 카세트 매핑 결과. 인덱스 0 = 최하단 슬롯. true = 웨이퍼 있음.</summary>
        public IReadOnlyList<bool> WaferMap { get; private set; }

        // ──────────────────────────────────────────────────────────────────────
        //  §3. 생성자
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// <see cref="InputLoaderUnit"/>을 초기화하고 모든 하드웨어 컴포넌트를 생성하여
        /// <see cref="BaseUnit{TSetup,TConfig,TRecipe}.Components"/> 트리에 등록한다.
        /// </summary>
        public InputLoaderUnit() : base("InputLoaderUnit")
        {
            // ── Motion Axes ────────────────────────────────────────────────
            ElevatorZ = AjinFactory.CreateAxis("ElevatorZ");
            FeederY   = AjinFactory.CreateAxis("FeederY");

            // ── Sensors (DI) ───────────────────────────────────────────────
            CassetteExistSensor = AjinFactory.CreateDigitalInput("CassetteExistSensor");
            ProtrusionSensor    = AjinFactory.CreateDigitalInput("ProtrusionSensor");
            WaferDetectSensor   = AjinFactory.CreateDigitalInput("WaferDetectSensor");
            WaferClampedSensor  = AjinFactory.CreateDigitalInput("WaferClampedSensor");

            // ── Cylinders ──────────────────────────────────────────────────
            FeederUpDownCyl = AjinFactory.CreateCylinder("FeederUpDownCyl");
            FeederClampCyl  = AjinFactory.CreateCylinder("FeederClampCyl");

            // ── Composite 트리 등록 ────────────────────────────────────────
            Components.Add(ElevatorZ);
            Components.Add(FeederY);
            Components.Add(CassetteExistSensor);
            Components.Add(ProtrusionSensor);
            Components.Add(WaferDetectSensor);
            Components.Add(WaferClampedSensor);
            Components.Add(FeederUpDownCyl);
            Components.Add(FeederClampCyl);

            WaferMap = new List<bool>().AsReadOnly();
        }

        // ──────────────────────────────────────────────────────────────────────
        //  §4. 시퀀스 로직
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 카세트 전체 슬롯을 스캔하여 웨이퍼 맵을 생성한다.<br/>
        /// 인터락: <see cref="CassetteExistSensor"/>가 OFF이면 즉시 false 반환.<br/>
        /// 동작: <see cref="ElevatorZ"/>를 첫 슬롯 위치부터 <paramref name="slotPitch"/>씩
        /// 상승시키며 <see cref="WaferDetectSensor"/> 상태를 읽어 맵 배열을 채운다.
        /// </summary>
        /// <param name="maxSlots">카세트 최대 슬롯 수</param>
        /// <param name="slotPitch">슬롯 간격 [mm]</param>
        /// <returns>스캔 성공 시 true, 인터락 위반 또는 이동 실패 시 false</returns>
        public async Task<bool> ScanCassetteAsync(int maxSlots, double slotPitch)
        {
            // ── 인터락: 카세트 유무 확인 ──────────────────────────────────
            if (!CassetteExistSensor.IsOn)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? ScanCassette: 카세트 미감지. 스캔을 중단합니다.");
                return false;
            }

            Console.WriteLine($"[INFO]  '{Name}' ? 카세트 매핑 시작 (슬롯 수: {maxSlots}, 피치: {slotPitch}mm)");

            var map = new List<bool>();

            // ── 첫 슬롯 위치로 이동 ───────────────────────────────────────
            await ElevatorZ.MoveAbsoluteAsync(Setup.FirstSlotPosition, Recipe.ScanVelocity);

            if (ElevatorZ.IsAlarm)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? ScanCassette: ElevatorZ 이동 실패 (슬롯 0).");
                return false;
            }

            // ── 슬롯별 순차 스캔 ──────────────────────────────────────────
            for (int i = 0; i < maxSlots; i++)
            {
                // 첫 슬롯은 이미 위치해 있으므로, 두 번째 슬롯부터 상대 이동
                if (i > 0)
                {
                    await ElevatorZ.MoveRelativeAsync(slotPitch, Recipe.ScanVelocity);

                    if (ElevatorZ.IsAlarm)
                    {
                        Console.WriteLine($"[ALARM] '{Name}' ? ScanCassette: ElevatorZ 이동 실패 (슬롯 {i}).");
                        return false;
                    }
                }

                // 센서 안정화 대기
                await Task.Delay(Recipe.ScanSettleTimeMs).ContinueWith(_ => { });

                bool hasWafer = WaferDetectSensor.IsOn;
                map.Add(hasWafer);

                Console.WriteLine(
                    $"[INFO]  '{Name}' ? 슬롯 [{i:D2}] {(hasWafer ? "웨이퍼 있음 ●" : "비어 있음 ○")}");
            }

            WaferMap = map.AsReadOnly();
            Console.WriteLine($"[INFO]  '{Name}' ? 카세트 매핑 완료. 웨이퍼 수: {map.FindAll(x => x).Count}/{maxSlots}");
            return true;
        }

        /// <summary>
        /// 엘리베이터를 지정한 슬롯 절대 위치로 이동한다.<br/>
        /// <para>
        /// 인터락(<see cref="ProtrusionSensor"/>):<br/>
        /// 이동 명령 전 및 이동 완료 후 돌출 센서가 ON이면 즉시
        /// <see cref="BaseAxis.EStop"/>을 호출하고 <see cref="InvalidOperationException"/>을 발생시킨다.
        /// </para>
        /// </summary>
        /// <param name="targetPosition">이동할 절대 위치 [mm]</param>
        /// <exception cref="InvalidOperationException">
        /// 이동 전 또는 이동 완료 후 돌출 센서가 감지된 경우.
        /// </exception>
        public async Task MoveToTargetSlotAsync(double targetPosition)
        {
            // ── 이동 전 돌출 인터락 확인 ──────────────────────────────────
            if (ProtrusionSensor.IsOn)
            {
                ElevatorZ.EStop();
                string msg = $"[ALARM] '{Name}' ? MoveToTargetSlot: 이동 전 돌출 센서 감지! EStop 실행.";
                Console.WriteLine(msg);
                throw new InvalidOperationException(msg);
            }

            // ── 이동 명령 + 돌출 인터락 병렬 감시 ────────────────────────
            using (var cts = new CancellationTokenSource())
            {
                // 이동 태스크
                Task moveTask = ElevatorZ.MoveAbsoluteAsync(targetPosition);

                // 돌출 센서 감시 태스크: 10ms 주기로 폴링
                Task<bool> watchTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        if (ProtrusionSensor.IsOn)
                            return true;
                        await Task.Delay(10, cts.Token).ContinueWith(_ => { });
                    }
                    return false;
                }, cts.Token);

                // 이동 완료 또는 돌출 감지 중 먼저 끝나는 쪽 대기
                Task first = await Task.WhenAny(moveTask, watchTask);

                // 이동이 먼저 끝난 경우 감시 태스크 취소
                if (first == moveTask)
                {
                    cts.Cancel();
                    await watchTask.ContinueWith(_ => { }); // 감시 태스크 정리 대기
                }
                else
                {
                    // 돌출 감지가 먼저 → 즉시 비상 정지
                    ElevatorZ.EStop();
                    cts.Cancel();
                    await moveTask.ContinueWith(_ => { }); // 이동 태스크 정리 대기

                    string msg = $"[ALARM] '{Name}' ? MoveToTargetSlot: 이동 중 돌출 센서 감지! EStop 실행.";
                    Console.WriteLine(msg);
                    throw new InvalidOperationException(msg);
                }
            }

            // ── 이동 후 축 알람 확인 ──────────────────────────────────────
            if (ElevatorZ.IsAlarm)
            {
                string msg = $"[ALARM] '{Name}' ? MoveToTargetSlot: ElevatorZ 이동 실패 (알람 발생).";
                Console.WriteLine(msg);
                throw new InvalidOperationException(msg);
            }

            Console.WriteLine(
                $"[INFO]  '{Name}' ? 슬롯 위치 이동 완료: {ElevatorZ.ActualPosition:F3}mm");
        }

        /// <summary>
        /// 피더를 클램프한 뒤 교환 위치(Y축)로 전진하여 웨이퍼를 InputStage 앞까지 이송한다.<br/>
        /// <para>
        /// 시퀀스:<br/>
        /// 1. <see cref="FeederUpDownCyl"/> 전진(하강) → 피더를 웨이퍼 높이에 정렬<br/>
        /// 2. <see cref="FeederClampCyl"/> 전진 → 웨이퍼 파지<br/>
        /// 3. <see cref="WaferClampedSensor"/> 확인 → 파지 실패 시 중단<br/>
        /// 4. <see cref="FeederY"/> 교환 위치로 전진
        /// </para>
        /// </summary>
        /// <returns>시퀀스 전체 성공 시 true, 중간 단계 실패 시 false</returns>
        public async Task<bool> MoveToExchangePositionAsync()
        {
            // ── Step 1: 피더 하강 ──────────────────────────────────────────
            Console.WriteLine($"[INFO]  '{Name}' ? 피더 하강 시작.");
            bool cylResult = await FeederUpDownCyl.MoveFwdAsync();

            if (!cylResult)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? MoveToExchangePosition: 피더 하강 타임아웃.");
                return false;
            }

            // ── Step 2: 웨이퍼 클램프 ─────────────────────────────────────
            Console.WriteLine($"[INFO]  '{Name}' ? 웨이퍼 클램프 전진.");
            cylResult = await FeederClampCyl.MoveFwdAsync();

            if (!cylResult)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? MoveToExchangePosition: 클램프 실린더 타임아웃.");
                return false;
            }

            // ── Step 3: 클램프 확인 센서 인터락 ──────────────────────────
            bool clamped = await WaferClampedSensor.WaitUntilStateAsync(true, timeoutMs: 1000);

            if (!clamped)
            {
                Console.WriteLine(
                    $"[ALARM] '{Name}' ? MoveToExchangePosition: 웨이퍼 파지 미확인. 이송을 중단합니다.");
                // 안전을 위해 클램프 해제 후 복귀
                await FeederClampCyl.MoveBwdAsync();
                await FeederUpDownCyl.MoveBwdAsync();
                return false;
            }

            // ── Step 4: 피더 Y축 교환 위치로 전진 ────────────────────────
            Console.WriteLine(
                $"[INFO]  '{Name}' ? FeederY 교환 위치({Setup.ExchangePositionY}mm)로 전진.");
            await FeederY.MoveAbsoluteAsync(Setup.ExchangePositionY);

            if (FeederY.IsAlarm)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? MoveToExchangePosition: FeederY 이동 실패.");
                return false;
            }

            Console.WriteLine($"[INFO]  '{Name}' ? 교환 위치 이동 완료.");
            return true;
        }

        /// <summary>
        /// 웨이퍼를 InputStage에 전달한 뒤 피더를 원점으로 복귀시킨다.<br/>
        /// <para>
        /// 시퀀스:<br/>
        /// 1. <see cref="FeederClampCyl"/> 후진 → 웨이퍼 해제<br/>
        /// 2. <see cref="WaferClampedSensor"/> OFF 확인<br/>
        /// 3. <see cref="FeederY"/> 원점(0mm)으로 복귀<br/>
        /// 4. <see cref="FeederUpDownCyl"/> 후진 → 피더 상승
        /// </para>
        /// </summary>
        /// <returns>시퀀스 전체 성공 시 true, 중간 단계 실패 시 false</returns>
        public async Task<bool> RetractFeederAsync()
        {
            // ── Step 1: 클램프 해제 ───────────────────────────────────────
            Console.WriteLine($"[INFO]  '{Name}' ? 클램프 해제.");
            bool cylResult = await FeederClampCyl.MoveBwdAsync();

            if (!cylResult)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? RetractFeeder: 클램프 해제 타임아웃.");
                return false;
            }

            // ── Step 2: 클램프 해제 확인 ──────────────────────────────────
            bool released = await WaferClampedSensor.WaitUntilStateAsync(false, timeoutMs: 1000);

            if (!released)
            {
                Console.WriteLine(
                    $"[ALARM] '{Name}' ? RetractFeeder: 클램프 해제 미확인 (센서 OFF 대기 타임아웃).");
                return false;
            }

            // ── Step 3: 피더 Y축 원점 복귀 ───────────────────────────────
            Console.WriteLine($"[INFO]  '{Name}' ? FeederY 원점 복귀.");
            await FeederY.MoveAbsoluteAsync(0.0);

            if (FeederY.IsAlarm)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? RetractFeeder: FeederY 원점 복귀 실패.");
                return false;
            }

            // ── Step 4: 피더 상승 ─────────────────────────────────────────
            Console.WriteLine($"[INFO]  '{Name}' ? 피더 상승.");
            cylResult = await FeederUpDownCyl.MoveBwdAsync();

            if (!cylResult)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? RetractFeeder: 피더 상승 타임아웃.");
                return false;
            }

            Console.WriteLine($"[INFO]  '{Name}' ? 피더 원점 복귀 완료.");
            return true;
        }
    }
}
