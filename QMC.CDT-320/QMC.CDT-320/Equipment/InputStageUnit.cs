using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;
using QMC.CDT320.Alarms;

namespace QMC.CDT320
{
    // ??????????????????????????????????????????????????????????????????????????
    //  InputStageUnit 전용 데이터 클래스
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>
    /// InputStageUnit의 기구적 설정값.<br/>
    /// 각 축의 기준 위치 및 기구 오프셋 등 하드웨어 교체 전까지 유지되는 값을 담는다.
    /// </summary>
    public class InputStageSetup : ISetupData
    {
        /// <summary>ExpanderZ 하강(웨이퍼 고정) 절대 위치 [mm].</summary>
        public double ExpanderDownPosition  { get; set; } = 50.0;

        /// <summary>ExpanderZ 상승(언로딩 가능) 절대 위치 [mm].</summary>
        public double ExpanderUpPosition    { get; set; } = 0.0;

        /// <summary>웨이퍼 언로딩 시 StageY가 이동할 절대 위치 [mm].</summary>
        public double UnloadPositionY       { get; set; } = 0.0;

        /// <summary>NeedleZ 상승(이젝트) 절대 위치 [mm].</summary>
        public double NeedleEjectPosition   { get; set; } = 5.0;

        /// <summary>NeedleZ 하강(대기) 절대 위치 [mm].</summary>
        public double NeedleDownPosition    { get; set; } = 0.0;

        /// <summary>
        /// TPU 픽커 기구의 StageY 방향 오프셋 [mm].<br/>
        /// 스캔 위치에서 픽업 위치로 이동 시 더해진다.
        /// </summary>
        public double PickerOffsetY         { get; set; } = 3.0;

        /// <summary>
        /// TPU 픽커 기구의 NeedleBlockX 방향 오프셋 [mm].<br/>
        /// 스캔 위치에서 픽업 위치로 이동 시 더해진다.
        /// </summary>
        public double PickerOffsetX         { get; set; } = 0.0;

        /// <summary>바코드 리더 읽기 타임아웃 [ms].</summary>
        public int BarcodeReadTimeoutMs     { get; set; } = 3000;

        // ─── Stage 61 — PICK 시퀀스 신규 티칭 위치 ────────────────────

        /// <summary>웨이퍼 비전 카메라 X 원점 (티칭) [mm].
        /// CameraX 절대 위치 = CameraOriginX + 와퍼맵 Die X.</summary>
        public double CameraOriginX         { get; set; } = 100.0;

        /// <summary>웨이퍼 비전 카메라 Z 포커스 위치 (티칭) [mm].</summary>
        public double CameraFocusZ          { get; set; } = -2.0;

        /// <summary>웨이퍼 비전 카메라 Z 안전(회피) 위치 [mm].
        /// 촬영 종료 후 ArmY 가 픽업 영역에 진입하기 전 CameraZ 가 이 위치까지 상승.</summary>
        public double CameraSafeZ           { get; set; } = 50.0;

        /// <summary>Wafer Stage Y 기준 위치 (티칭) [mm].
        /// StageY 절대 위치 = StageYTeachPosition + WaferAlignOffsetY + 와퍼맵 Die Y + VisionOffsetY.
        /// die.Y 범위 ±150mm (300mm wafer) 가정 시 TeachPosition 은 |die.Y_max| 보다 커야 음수 stage 위치 회피.</summary>
        public double StageYTeachPosition   { get; set; } = 200.0;

        /// <summary>니들 X 티칭 위치 [mm].
        /// NeedleX 절대 위치 = NeedleTeachX + WaferAlignOffsetX + 와퍼맵 Die X + VisionOffsetX.</summary>
        public double NeedleTeachX          { get; set; } = 100.0;

        /// <summary>비전 픽셀-mm 환산 계수 [mm/px]. (m.X-320, m.Y-240) 의 단위 변환에 사용.</summary>
        public double VisionPixelToMm       { get; set; } = 0.01;
    }

    /// <summary>
    /// InputStageUnit의 고정 사양 파라미터.
    /// </summary>
    public class InputStageConfig : IConfigData
    {
        /// <summary>시뮬레이션 모드 여부 (기본값: true).</summary>
        public bool IsSimulationMode { get; set; } = true;

        /// <summary>얼라인 반복 촬상 최대 횟수.</summary>
        public int MaxAlignIterations { get; set; } = 3;

        /// <summary>얼라인 수렴 임계값 [deg]. 이 값 이하이면 반복을 종료한다.</summary>
        public double AlignConvergenceThresholdDeg { get; set; } = 0.005;
    }

    /// <summary>
    /// InputStageUnit의 공정별 작업 파라미터.
    /// </summary>
    public class InputStageRecipe : IRecipeData
    {
        /// <summary>일반 이동 속도 [mm/s]. xlsx WAFER STAGE_Y = 500 mm/s 반영.</summary>
        public double MoveVelocity          { get; set; } = 500.0;

        /// <summary>얼라인 이동 속도 [mm/s]. 미세 이동이라 30→100 정도.</summary>
        public double AlignVelocity         { get; set; } = 100.0;

        /// <summary>NeedleZ 이젝트 이동 속도 [mm/s]. xlsx NEEDLE_Z = 100 mm/s.</summary>
        public double NeedleVelocity        { get; set; } = 100.0;

        /// <summary>NeedleVacuum 흡착 안정화 대기 [ms].</summary>
        public int NeedleVacuumSettleMs     { get; set; } = 50;

        /// <summary>
        /// 레퍼런스 마크 2점 간 X 거리가 0에 가까워 피치 계산 불가 시 사용할 기본 X 피치 [mm].
        /// </summary>
        public double DefaultPitchX         { get; set; } = 0.15;

        /// <summary>
        /// 레퍼런스 마크 2점 간 Y 거리가 0에 가까워 피치 계산 불가 시 사용할 기본 Y 피치 [mm].
        /// </summary>
        public double DefaultPitchY         { get; set; } = 0.15;

        /// <summary>비전 Expose 완료 대기 타임아웃 [ms].</summary>
        public int VisionExposeTimeoutMs    { get; set; } = 2000;

        /// <summary>비전 검사 결과 수신 타임아웃 [ms].</summary>
        public int VisionResultTimeoutMs    { get; set; } = 5000;

        /// <summary>TPU 픽커 상승 완료 대기 타임아웃 [ms].</summary>
        public int PickerUpTimeoutMs        { get; set; } = 3000;

        /// <summary>축 이동 완료 대기 타임아웃 [ms].</summary>
        public int MoveTimeoutMs            { get; set; } = 10000;
    }

    // ??????????????????????????????????????????????????????????????????????????
    //  InputStageUnit
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>
    /// Input Stage 유닛.<br/>
    /// InputLoaderUnit에서 전달받은 웨이퍼를 고정하고, 비전 얼라인으로 원점을 수립한 뒤,
    /// 다이를 TransferPickerUnit이 픽업할 수 있도록 순차적으로 위치를 제공하는 핵심 유닛.
    /// <para>
    /// 전체 공정 흐름:<br/>
    /// <see cref="LoadAndPrepareWaferAsync"/> →
    /// <see cref="VisionAlignAndSetupOriginAsync"/> →
    /// <see cref="WaitForUserConfirmAsync"/> →
    /// <see cref="MultiScanAndPickupAsync"/> →
    /// <see cref="UnloadWaferAsync"/>
    /// </para>
    /// </summary>
    public class InputStageUnit : BaseUnit<InputStageSetup, InputStageConfig, InputStageRecipe>
    {
        // ──────────────────────────────────────────────────────────────────────
        //  §1. 하드웨어 컴포넌트 선언
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 스테이지 Y축.<br/>
        /// 웨이퍼를 전후로 이동시켜 목표 다이 행(Row)을 카메라 및 니들 위치에 정렬한다.
        /// </summary>
        public BaseAxis StageY      { get; private set; }

        /// <summary>
        /// 스테이지 Theta(회전) 축.<br/>
        /// 웨이퍼의 각도 오차를 보정하는 얼라인 축이다.
        /// </summary>
        public BaseAxis StageT      { get; private set; }

        /// <summary>
        /// 웨이퍼 테이프 텐션 제어 Z축.<br/>
        /// Down 위치: 웨이퍼 테이프를 팽팽하게 고정하여 픽업 정밀도를 확보한다.<br/>
        /// Up 위치: 텐션 해제 상태로 웨이퍼 로딩/언로딩이 가능하다.
        /// </summary>
        public BaseAxis ExpanderZ   { get; private set; }

        /// <summary>
        /// 스캔 카메라 X축.<br/>
        /// 다이 열(Column)을 따라 카메라를 좌우로 이동시켜 촬상 위치를 결정한다.
        /// </summary>
        public BaseAxis CameraX     { get; private set; }

        /// <summary>
        /// 니들 블럭 X축.<br/>
        /// 이젝터 니들을 픽업 대상 다이의 X 좌표에 정렬한다.
        /// </summary>
        public BaseAxis NeedleBlockX { get; private set; }

        /// <summary>
        /// 이젝터 니들 Z축.<br/>
        /// 픽업 시 다이를 테이프 아래에서 위로 밀어 올려(Eject) TPU 픽커의 흡착을 돕는다.
        /// </summary>
        public BaseAxis NeedleZ     { get; private set; }

        /// <summary>
        /// Stage 44 — Eject Pin Z축 (Simulator axis 8 호환).<br/>
        /// 다이 픽업 시 테이프 아래에서 핀을 밀어올려 다이를 분리.
        /// </summary>
        public BaseAxis EjectPinZ   { get; private set; }

        /// <summary>Stage 61 — 웨이퍼 비전 카메라 Z 축 (Simulator axis 37 호환).
        /// 다이 촬영 전 포커스 위치로 이동.</summary>
        public BaseAxis CameraZ     { get; private set; }

        /// <summary>
        /// 니들 진공 흡착 DO.<br/>
        /// On 상태에서 테이프를 니들 상단에 흡착·고정하여 이젝트 동작의 정밀도를 높인다.
        /// </summary>
        public BaseDigitalOutput NeedleVacuum { get; private set; }

        // ──────────────────────────────────────────────────────────────────────
        //  §2. 외부 연동 인터페이스 (생성자 주입)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>피더 안전 위치 확인을 위한 로더 유닛 인터페이스.</summary>
        public IWaferLoader        Loader     { get; private set; }

        /// <summary>웨이퍼 ID 취득을 위한 바코드 리더 인터페이스.</summary>
        public IBarcodeReader      Barcode    { get; private set; }

        /// <summary>촬상 트리거 및 결과 수신을 위한 비전 PC TCP 통신 인터페이스.</summary>
        public IVisionTcpClient    Vision     { get; private set; }

        /// <summary>맵 파싱 및 UI 전송을 위한 웨이퍼 맵 핸들러 인터페이스.</summary>
        public IWaferMapHandler    MapHandler { get; private set; }

        /// <summary>픽업 신호 송수신을 위한 TPU 연동 인터페이스.</summary>
        public ITransferPickerUnit Tpu        { get; private set; }

        // ──────────────────────────────────────────────────────────────────────
        //  §3. 내부 상태 (얼라인 결과 및 원점 좌표)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>현재 로드된 웨이퍼의 맵 데이터. 로딩 완료 후 설정된다.</summary>
        public WaferMapData CurrentWaferMap  { get; private set; }

        /// <summary>얼라인 완료 후 확정된 첫 번째 다이(Index 1)의 StageY 절대 좌표 [mm].</summary>
        public double OriginY                { get; private set; }

        /// <summary>얼라인 완료 후 확정된 첫 번째 다이(Index 1)의 CameraX 절대 좌표 [mm].</summary>
        public double OriginX                { get; private set; }

        /// <summary>얼라인으로 수립된 다이 간 X축 피치 [mm].</summary>
        public double PitchX                 { get; private set; }

        /// <summary>얼라인으로 수립된 다이 간 Y축 피치 [mm].</summary>
        public double PitchY                 { get; private set; }

        /// <summary>
        /// 사용자 컨펌 대기에 사용되는 TaskCompletionSource.<br/>
        /// UI 스레드에서 <see cref="ConfirmFromUi"/>를 호출하면 완료된다.
        /// </summary>
        private TaskCompletionSource<UserConfirmResult> _confirmTcs;

        // ──────────────────────────────────────────────────────────────────────
        //  §4. 생성자
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// <see cref="InputStageUnit"/>을 초기화하고 모든 하드웨어 컴포넌트를 생성하여
        /// 외부 연동 인터페이스를 주입한다.
        /// </summary>
        /// <param name="loader">피더 안전 위치 확인용 로더 유닛 인터페이스</param>
        /// <param name="barcode">바코드 리더 인터페이스</param>
        /// <param name="vision">비전 PC TCP 통신 인터페이스</param>
        /// <param name="mapHandler">웨이퍼 맵 핸들러 인터페이스</param>
        /// <param name="tpu">TPU 연동 인터페이스</param>
        public InputStageUnit(
            IWaferLoader        loader,
            IBarcodeReader      barcode,
            IVisionTcpClient    vision,
            IWaferMapHandler    mapHandler,
            ITransferPickerUnit tpu)
            : base("InputStageUnit")
        {
            // ── 외부 인터페이스 저장 ───────────────────────────────────────
            Loader     = loader     ?? throw new ArgumentNullException("loader");
            Barcode    = barcode    ?? throw new ArgumentNullException("barcode");
            Vision     = vision     ?? throw new ArgumentNullException("vision");
            MapHandler = mapHandler ?? throw new ArgumentNullException("mapHandler");
            Tpu        = tpu        ?? throw new ArgumentNullException("tpu");

            // ── Motion Axes ────────────────────────────────────────────────
            StageY       = AjinFactory.CreateAxis("StageY");
            StageT       = AjinFactory.CreateAxis("StageT");
            ExpanderZ    = AjinFactory.CreateAxis("ExpanderZ");
            CameraX      = AjinFactory.CreateAxis("CameraX");
            NeedleBlockX = AjinFactory.CreateAxis("NeedleBlockX");
            NeedleZ      = AjinFactory.CreateAxis("NeedleZ");
            // Stage 44 — Eject Pin Z 축 (매뉴얼 사양 호환)
            EjectPinZ    = AjinFactory.CreateAxis("EjectPinZ");
            // Stage 61 — 웨이퍼 비전 카메라 Z (포커스 이동) 축
            CameraZ      = AjinFactory.CreateAxis("CameraZ");
            CameraZ.Setup.SoftLimitPlus  = 100.0;
            CameraZ.Setup.SoftLimitMinus = -50.0;

            // Stage 28 — InputStage 축 SoftLimit 확장 (default 200 → 적정값)
            //   StageY: 다이 행(Row)에 따라 ~300mm 까지 이동 (Wafer Stage 사양)
            //   CameraX: 다이 열에 따라 ~300mm 이동
            //   StageT: ±360° 회전 (단위는 deg)
            //   ExpanderZ / NeedleZ / NeedleBlockX: 100mm 이내
            StageY.Setup.SoftLimitPlus       = 350.0;
            CameraX.Setup.SoftLimitPlus      = 350.0;
            StageT.Setup.SoftLimitPlus       = 360.0;
            StageT.Setup.SoftLimitMinus      = -360.0;
            NeedleBlockX.Setup.SoftLimitPlus = 250.0;

            // ── Digital Output ─────────────────────────────────────────────
            NeedleVacuum = AjinFactory.CreateDigitalOutput("NeedleVacuum");

            // ── Composite 트리 등록 ────────────────────────────────────────
            Components.Add(StageY);
            Components.Add(StageT);
            Components.Add(ExpanderZ);
            Components.Add(CameraX);
            Components.Add(NeedleBlockX);
            Components.Add(NeedleZ);
            Components.Add(EjectPinZ);
            Components.Add(CameraZ);
            Components.Add(NeedleVacuum);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Stage 61 — Runtime 얼라인 결과 (Setup 아님 — 웨이퍼 로딩 후 얼라인 시 set)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>웨이퍼 로딩 후 얼라인 절차에서 산출되는 X 보정 오프셋 [mm].
        /// PICK 시 StageY/NeedleX/ArmX 절대 위치 계산에 사용.</summary>
        public double WaferAlignOffsetX { get; set; } = 0.0;

        /// <summary>웨이퍼 로딩 후 얼라인 절차에서 산출되는 Y 보정 오프셋 [mm].
        /// PICK 시 StageY 절대 위치 계산에 사용.</summary>
        public double WaferAlignOffsetY { get; set; } = 0.0;

        // ──────────────────────────────────────────────────────────────────────
        //  §5. UI 연동 ? 컨펌 신호 수신 메서드
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// UI 스레드에서 호출하여 <see cref="WaitForUserConfirmAsync"/> 대기를 해제한다.<br/>
        /// 사용자가 얼라인 결과를 확인하고 진행/취소를 선택했을 때 호출한다.
        /// </summary>
        /// <param name="result">사용자가 입력한 컨펌 결과 데이터.</param>
        public void ConfirmFromUi(UserConfirmResult result)
        {
            TaskCompletionSource<UserConfirmResult> tcs = _confirmTcs;
            if (tcs != null)
                tcs.TrySetResult(result);
        }

        // ??????????????????????????????????????????????????????????????????????
        //  §6. 핵심 시퀀스 로직
        // ??????????????????????????????????????????????????????????????????????

        // ──────────────────────────────────────────────────────────────────────
        //  Step 1: 자재 로딩 및 준비
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 웨이퍼를 스테이지에 고정하고 맵 데이터를 로드한다.<br/>
        /// <para>
        /// 시퀀스:<br/>
        /// 1. 로더 피더 안전 위치 인터락 확인<br/>
        /// 2. ExpanderZ → Down 위치 이동 (테이프 텐션 확보)<br/>
        /// 3. 바코드 리딩으로 Wafer ID 취득<br/>
        /// 4. 맵 데이터 파싱 및 UI 업데이트
        /// </para>
        /// </summary>
        /// <returns>시퀀스 전체 성공 시 true, 인터락 위반 또는 중간 실패 시 false</returns>
        public async Task<bool> LoadAndPrepareWaferAsync()
        {
            // ── Step 1: 로더 피더 안전 위치 인터락 ───────────────────────
            if (!Loader.IsFeederAtSafePosition)
            {
                Console.WriteLine(
                    $"[ALARM] '{Name}' ? LoadAndPrepare: 로더 피더가 안전 위치에 없습니다. " +
                    "ExpanderZ 하강을 금지합니다.");
                AlarmManager.Raise(
                    AlarmSeverity.Warning,
                    "IS-FEEDER",
                    source: "InputStageUnit.LoadAndPrepareWaferAsync",
                    message: "로더 피더가 안전 위치에 없습니다. ExpanderZ 하강 금지.");
                return false;
            }

            // ── Step 2: ExpanderZ Down 이동 (테이프 텐션 확보) ───────────
            Console.WriteLine($"[INFO]  '{Name}' ? ExpanderZ Down 위치({Setup.ExpanderDownPosition}mm) 이동.");
            await ExpanderZ.MoveAbsoluteAsync(Setup.ExpanderDownPosition, Recipe.MoveVelocity);

            if (ExpanderZ.IsAlarm)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? LoadAndPrepare: ExpanderZ 이동 실패.");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "IS-EXPZ",
                    source: "InputStageUnit.LoadAndPrepareWaferAsync",
                    message: $"ExpanderZ 알람 발생 (axis code={ExpanderZ.AlarmCode}).");
                return false;
            }

            // ── Step 3: 바코드 리딩 ───────────────────────────────────────
            Console.WriteLine($"[INFO]  '{Name}' ? 바코드 리딩 시작.");
            string waferId = await Barcode.ReadAsync(Setup.BarcodeReadTimeoutMs);

            if (string.IsNullOrEmpty(waferId))
            {
                Console.WriteLine($"[ALARM] '{Name}' ? LoadAndPrepare: 바코드 읽기 실패. 시퀀스를 중단합니다.");
                AlarmManager.Raise(
                    AlarmSeverity.Warning,
                    "IS-BARCODE",
                    source: "InputStageUnit.LoadAndPrepareWaferAsync",
                    message: "바코드 읽기 실패 (Wafer ID 비어있음).");
                return false;
            }

            Console.WriteLine($"[INFO]  '{Name}' ? Wafer ID 취득 완료: [{waferId}]");

            // ── Step 4: 맵 파싱 및 UI 업데이트 ───────────────────────────
            WaferMapData mapData = await MapHandler.ParseMapAsync(waferId);

            if (mapData == null)
            {
                Console.WriteLine(
                    $"[ALARM] '{Name}' ? LoadAndPrepare: Wafer ID [{waferId}]에 대한 맵 파싱 실패.");
                AlarmManager.Raise(
                    AlarmSeverity.Warning,
                    "IS-MAP",
                    source: "InputStageUnit.LoadAndPrepareWaferAsync",
                    message: $"Wafer ID [{waferId}] 맵 파싱 실패 (ParseMapAsync null).");
                return false;
            }

            CurrentWaferMap = mapData;
            MapHandler.SendMapToUi(mapData);

            Console.WriteLine(
                $"[INFO]  '{Name}' ? 맵 로드 완료. " +
                $"다이 배열: {mapData.RowCount}행 × {mapData.ColumnCount}열");
            return true;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Step 2: 비전 얼라인 및 원점 셋업
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 비전 촬상과 축 이동을 반복하여 웨이퍼 각도를 보정하고 다이 원점 좌표를 수립한다.<br/>
        /// <para>
        /// 시퀀스:<br/>
        /// 1. 맵 중앙 다이로 이동 후 촬상 → Theta 보정 (수렴까지 반복)<br/>
        /// 2. 레퍼런스 마크 1번으로 이동 후 촬상 → Ref1 좌표 기록<br/>
        /// 3. 레퍼런스 마크 2번으로 이동 후 촬상 → Ref2 좌표 기록<br/>
        /// 4. 두 마크 간 거리로 X/Y 피치 계산 (좌표 동일 시 Recipe 기본값 사용)<br/>
        /// 5. 첫 번째 다이(Index 1) 절대 좌표를 Origin으로 확정
        /// </para>
        /// </summary>
        /// <returns>얼라인 성공 시 true, 통신 오류 또는 축 알람 시 false</returns>
        public async Task<bool> VisionAlignAndSetupOriginAsync()
        {
            if (CurrentWaferMap == null)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? VisionAlign: 맵 데이터 없음. LoadAndPrepare를 먼저 실행하세요.");
                return false;
            }

            WaferMapData map = CurrentWaferMap;

            // ── Step 1: 맵 중앙 다이로 이동 후 Theta 반복 보정 ──────────
            int centerRow = map.RowCount / 2;
            int centerCol = map.ColumnCount / 2;

            Console.WriteLine(
                $"[INFO]  '{Name}' ? 얼라인 시작. 중앙 다이 [{centerRow},{centerCol}]로 이동.");

            // 초기 중앙 좌표로 이동 (원점 미수립 상태이므로 0 + 피치 추정으로 이동)
            await MoveToDieAsync(centerRow, centerCol, useEstimate: true);
            if (StageY.IsAlarm || CameraX.IsAlarm)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? VisionAlign: 중앙 이동 실패.");
                return false;
            }

            double totalDeltaTheta = 0.0;

            for (int iter = 0; iter < Config.MaxAlignIterations; iter++)
            {
                VisionAlignResult alignResult = await Vision.TriggerAlignAsync("Center");

                if (alignResult == null)
                {
                    Console.WriteLine(
                        $"[ALARM] '{Name}' ? VisionAlign: 비전 얼라인 통신 실패 (반복 {iter + 1}).");
                    AlarmManager.Raise(
                        AlarmSeverity.Warning,
                        "IS-ALIGN",
                        source: "InputStageUnit.VisionAlignAndSetupOriginAsync",
                        message: $"중앙 다이 비전 얼라인 매칭 실패 (반복 {iter + 1}).");
                    return false;
                }

                double dTheta = alignResult.DeltaTheta;
                totalDeltaTheta += dTheta;

                Console.WriteLine(
                    $"[INFO]  '{Name}' ? 얼라인 반복 [{iter + 1}/{Config.MaxAlignIterations}] " +
                    $"dTheta={dTheta:F4}°, dX={alignResult.DeltaX:F4}mm, dY={alignResult.DeltaY:F4}mm");

                // Theta 보정 이동
                await StageT.MoveRelativeAsync(dTheta, Recipe.AlignVelocity);
                if (StageT.IsAlarm)
                {
                    Console.WriteLine($"[ALARM] '{Name}' ? VisionAlign: StageT 이동 실패.");
                    AlarmManager.Raise(
                        AlarmSeverity.Warning,
                        "IS-ALIGN",
                        source: "InputStageUnit.VisionAlignAndSetupOriginAsync",
                        message: $"StageT Theta 보정 이동 실패 (axis code={StageT.AlarmCode}).");
                    return false;
                }

                // 수렴 판정
                if (Math.Abs(dTheta) < Config.AlignConvergenceThresholdDeg)
                {
                    Console.WriteLine(
                        $"[INFO]  '{Name}' ? Theta 수렴 확인 (누적 보정: {totalDeltaTheta:F4}°). 얼라인 완료.");
                    break;
                }
            }

            // ── Step 2: 레퍼런스 마크 1 촬상 ─────────────────────────────
            Console.WriteLine($"[INFO]  '{Name}' ? 레퍼런스 마크 1 [{map.Ref1Row},{map.Ref1Col}] 촬상.");
            await MoveToDieAsync(map.Ref1Row, map.Ref1Col, useEstimate: true);

            VisionAlignResult ref1Result = await Vision.TriggerAlignAsync("Ref1");
            if (ref1Result == null)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? VisionAlign: 레퍼런스 마크 1 촬상 실패.");
                AlarmManager.Raise(
                    AlarmSeverity.Warning,
                    "IS-ALIGN",
                    source: "InputStageUnit.VisionAlignAndSetupOriginAsync",
                    message: $"레퍼런스 마크 1 [{map.Ref1Row},{map.Ref1Col}] 비전 매칭 실패.");
                return false;
            }

            double ref1X = CameraX.ActualPosition + ref1Result.DeltaX;
            double ref1Y = StageY.ActualPosition  + ref1Result.DeltaY;

            // ── Step 3: 레퍼런스 마크 2 촬상 ─────────────────────────────
            Console.WriteLine($"[INFO]  '{Name}' ? 레퍼런스 마크 2 [{map.Ref2Row},{map.Ref2Col}] 촬상.");
            await MoveToDieAsync(map.Ref2Row, map.Ref2Col, useEstimate: true);

            VisionAlignResult ref2Result = await Vision.TriggerAlignAsync("Ref2");
            if (ref2Result == null)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? VisionAlign: 레퍼런스 마크 2 촬상 실패.");
                AlarmManager.Raise(
                    AlarmSeverity.Warning,
                    "IS-ALIGN",
                    source: "InputStageUnit.VisionAlignAndSetupOriginAsync",
                    message: $"레퍼런스 마크 2 [{map.Ref2Row},{map.Ref2Col}] 비전 매칭 실패.");
                return false;
            }

            double ref2X = CameraX.ActualPosition + ref2Result.DeltaX;
            double ref2Y = StageY.ActualPosition  + ref2Result.DeltaY;

            // ── Step 4: X/Y 피치 계산 ────────────────────────────────────
            int colSpan = map.Ref2Col - map.Ref1Col;
            int rowSpan = map.Ref2Row - map.Ref1Row;

            if (colSpan != 0 && Math.Abs(ref2X - ref1X) > 1e-6)
            {
                PitchX = (ref2X - ref1X) / colSpan;
                Console.WriteLine($"[INFO]  '{Name}' ? X 피치 계산 완료: {PitchX:F6}mm/col");
            }
            else
            {
                PitchX = Recipe.DefaultPitchX;
                Console.WriteLine(
                    $"[WARN]  '{Name}' ? X 피치 계산 불가 (Ref X좌표 동일). " +
                    $"기본값 사용: {PitchX}mm");
            }

            if (rowSpan != 0 && Math.Abs(ref2Y - ref1Y) > 1e-6)
            {
                PitchY = (ref2Y - ref1Y) / rowSpan;
                Console.WriteLine($"[INFO]  '{Name}' ? Y 피치 계산 완료: {PitchY:F6}mm/row");
            }
            else
            {
                PitchY = Recipe.DefaultPitchY;
                Console.WriteLine(
                    $"[WARN]  '{Name}' ? Y 피치 계산 불가 (Ref Y좌표 동일). " +
                    $"기본값 사용: {PitchY}mm");
            }

            // ── Step 5: 첫 번째 다이 원점(Origin) 확정 ───────────────────
            // Ref1 좌표를 기준으로 Index[0,0]의 절대 좌표를 역산
            OriginX = ref1X - (map.Ref1Col * PitchX);
            OriginY = ref1Y - (map.Ref1Row * PitchY);

            Console.WriteLine(
                $"[INFO]  '{Name}' ? 원점 확정 완료. " +
                $"Origin X={OriginX:F4}mm, Y={OriginY:F4}mm");
            return true;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Step 3: 사용자 컨펌 대기
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 시퀀스를 일시 정지하고 UI로부터 사용자 컨펌(진행/취소)을 대기한다.<br/>
        /// <para>
        /// 메커니즘:<br/>
        /// 내부적으로 <see cref="TaskCompletionSource{T}"/>를 생성하여 대기 상태에 진입한다.<br/>
        /// UI 스레드에서 <see cref="ConfirmFromUi"/>를 호출하면 대기가 해제되고
        /// 사용자가 입력한 <see cref="UserConfirmResult"/>가 반환된다.
        /// </para>
        /// </summary>
        /// <returns>
        /// 사용자가 진행을 확인하면 해당 <see cref="UserConfirmResult"/>,
        /// 취소하거나 타임아웃이면 <see cref="UserConfirmResult.IsConfirmed"/> = false인 객체.
        /// </returns>
        public async Task<UserConfirmResult> WaitForUserConfirmAsync()
        {
            // 이전 TCS가 남아 있다면 취소 처리
            TaskCompletionSource<UserConfirmResult> oldTcs = _confirmTcs;
            if (oldTcs != null)
                oldTcs.TrySetCanceled();

            _confirmTcs = new TaskCompletionSource<UserConfirmResult>();

            Console.WriteLine(
                $"[INFO]  '{Name}' ? 사용자 컨펌 대기 중... " +
                "(UI에서 얼라인 결과 확인 후 ConfirmFromUi()를 호출하세요)");

            UserConfirmResult result = await _confirmTcs.Task;
            _confirmTcs = null;

            if (result.IsConfirmed)
            {
                // ── 사용자 수정값 적용 ─────────────────────────────────────
                if (Math.Abs(result.AngleOffset) > 1e-6)
                {
                    Console.WriteLine(
                        $"[INFO]  '{Name}' ? 사용자 Angle 오프셋 적용: {result.AngleOffset:F4}°");
                    await StageT.MoveRelativeAsync(result.AngleOffset, Recipe.AlignVelocity);
                }

                if (Math.Abs(result.StartOffsetX) > 1e-6 || Math.Abs(result.StartOffsetY) > 1e-6)
                {
                    OriginX += result.StartOffsetX;
                    OriginY += result.StartOffsetY;
                    Console.WriteLine(
                        $"[INFO]  '{Name}' ? 시작 위치 오프셋 적용. " +
                        $"새 Origin X={OriginX:F4}mm, Y={OriginY:F4}mm");
                }

                Console.WriteLine(
                    $"[INFO]  '{Name}' ? 컨펌 완료. " +
                    $"시작 다이 인덱스: {result.StartDieIndex}");
            }
            else
            {
                Console.WriteLine($"[WARN]  '{Name}' ? 사용자가 시퀀스를 취소했습니다.");
            }

            return result;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Step 4: 다중 스캔 및 픽업 동기화
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 웨이퍼 맵을 순회하며 비전 스캔과 다이 픽업을 파이프라인 방식으로 처리한다.<br/>
        /// <para>
        /// 핵심 설계 (파이프라인 병렬화):<br/>
        /// 비전에 Trigger를 날리고 Expose 완료 응답만 받으면 즉시 다음 좌표로 이동하므로
        /// 비전 이미지 분석 시간이 모션 이동 시간에 감춰져 스루풋을 최대화한다.
        /// </para>
        /// <para>
        /// 처리 단위(배치):<br/>
        /// TPU 픽커 개수(<see cref="ITransferPickerUnit.PickerCount"/>) 만큼 묶어서
        /// 스캔한 뒤, 동일 배치에 대해 픽업 동작을 수행한다.
        /// </para>
        /// <para>
        /// 픽업 시퀀스 (다이 1개 기준):<br/>
        /// 1. NeedleVacuum On → NeedleBlockX 이동<br/>
        /// 2. TPU에 픽업 가능(PickReady) 신호 전송<br/>
        /// 3. NeedleZ Eject(상승) ? TPU 픽커 하강과 동기화<br/>
        /// 4. TPU 픽커 상승 완료 대기<br/>
        /// 5. NeedleZ 하강 + NeedleVacuum Off
        /// </para>
        /// </summary>
        /// <param name="startDieIndex">픽업을 시작할 글로벌 다이 인덱스 (0-based 선형 인덱스)</param>
        /// <returns>맵 완료(또는 남은 다이 소진) 시 true, 중간 오류 시 false</returns>
        public async Task<bool> MultiScanAndPickupAsync(int startDieIndex = 0)
        {
            if (CurrentWaferMap == null)
            {
                Console.WriteLine(
                    $"[ALARM] '{Name}' ? MultiScanAndPickup: 맵 데이터 없음.");
                return false;
            }

            WaferMapData map       = CurrentWaferMap;
            int          totalDies = map.RowCount * map.ColumnCount;
            int          batchSize = Tpu.PickerCount > 0 ? Tpu.PickerCount : 1;

            Console.WriteLine(
                $"[INFO]  '{Name}' ? 다이 픽업 시작. " +
                $"총 {totalDies}개, 배치 크기: {batchSize}, 시작 인덱스: {startDieIndex}");

            int dieIndex = startDieIndex;

            while (dieIndex < totalDies)
            {
                // ── Phase A: 배치 단위 비전 스캔 ──────────────────────────
                // Expose 완료만 받고 결과를 기다리지 않아 다음 이동이 즉시 시작된다.
                int batchStart = dieIndex;
                int batchEnd   = Math.Min(dieIndex + batchSize, totalDies);

                for (int i = batchStart; i < batchEnd; i++)
                {
                    int row = i / map.ColumnCount;
                    int col = i % map.ColumnCount;

                    if (!map.DieMap[row, col])
                    {
                        Console.WriteLine($"[INFO]  '{Name}' ? 다이 [{i}] NG ? 스캔 스킵.");
                        continue;
                    }

                    // StageY + CameraX 동시 이동 (비전 촬상 위치)
                    double targetY = OriginY + row * PitchY;
                    double targetX = OriginX + col * PitchX;

                    Task moveY = StageY.MoveAbsoluteAsync(targetY, Recipe.MoveVelocity);
                    Task moveX = CameraX.MoveAbsoluteAsync(targetX, Recipe.MoveVelocity);
                    await Task.WhenAll(moveY, moveX);

                    if (StageY.IsAlarm || CameraX.IsAlarm)
                    {
                        Console.WriteLine(
                            $"[ALARM] '{Name}' ? MultiScanAndPickup: 스캔 이동 실패 (다이 [{i}]).");
                        AlarmManager.Raise(
                            AlarmSeverity.Error,
                            "IS-MOVE",
                            source: "InputStageUnit.MultiScanAndPickupAsync",
                            message: $"Phase A 스캔 이동 후 축 알람 (다이 [{i}], StageY.IsAlarm={StageY.IsAlarm}, CameraX.IsAlarm={CameraX.IsAlarm}).");
                        return false;
                    }

                    // ── 비전 트리거: Expose 완료만 대기, 결과는 나중에 수집 ──
                    bool exposed = await Vision.TriggerExposeAsync(i);

                    if (!exposed)
                    {
                        Console.WriteLine(
                            $"[ALARM] '{Name}' ? MultiScanAndPickup: 비전 Expose 실패 (다이 [{i}]).");
                        return false;
                    }

                    // Expose 완료 즉시 다음 좌표 이동 ? 비전 분석은 백그라운드에서 진행
                    Console.WriteLine(
                        $"[INFO]  '{Name}' ? 다이 [{i}] Expose 완료. 즉시 다음 좌표로 이동.");
                }

                // ── Phase B: 배치 단위 픽업 동작 ──────────────────────────
                for (int i = batchStart; i < batchEnd; i++)
                {
                    int row = i / map.ColumnCount;
                    int col = i % map.ColumnCount;

                    if (!map.DieMap[row, col])
                    {
                        Console.WriteLine($"[INFO]  '{Name}' ? 다이 [{i}] NG ? 픽업 스킵.");
                        continue;
                    }

                    // ── 비전 결과 수집 (픽업 전 OK 여부 최종 확인) ────────
                    bool inspOk = await Vision.GetResultAsync(i, Recipe.VisionResultTimeoutMs);

                    if (!inspOk)
                    {
                        Console.WriteLine(
                            $"[WARN]  '{Name}' ? 다이 [{i}] 비전 NG 또는 타임아웃 ? 픽업 스킵.");
                        continue;
                    }

                    // ── TPU 픽커 준비 확인 ─────────────────────────────────
                    if (!Tpu.IsPickerReady)
                    {
                        Console.WriteLine($"[WARN]  '{Name}' ? 다이 [{i}] TPU 픽커 미준비 ? 픽업 스킵.");
                        continue;
                    }

                    // ── 픽업 위치로 이동 (스캔 오프셋 + 기구 오프셋 적용) ─
                    double pickY = OriginY + row * PitchY + Setup.PickerOffsetY;
                    double pickX = OriginX + col * PitchX + Setup.PickerOffsetX;

                    Task pickMoveY = StageY.MoveAbsoluteAsync(pickY, Recipe.MoveVelocity);
                    Task pickMoveX = NeedleBlockX.MoveAbsoluteAsync(pickX, Recipe.MoveVelocity);
                    await Task.WhenAll(pickMoveY, pickMoveX);

                    if (StageY.IsAlarm || NeedleBlockX.IsAlarm)
                    {
                        Console.WriteLine(
                            $"[ALARM] '{Name}' ? MultiScanAndPickup: 픽업 위치 이동 실패 (다이 [{i}]).");
                        AlarmManager.Raise(
                            AlarmSeverity.Error,
                            "IS-MOVE",
                            source: "InputStageUnit.MultiScanAndPickupAsync",
                            message: $"Phase B 픽업 위치 이동 후 축 알람 (다이 [{i}], StageY.IsAlarm={StageY.IsAlarm}, NeedleBlockX.IsAlarm={NeedleBlockX.IsAlarm}).");
                        return false;
                    }

                    // ── 픽업 시퀀스 실행 ───────────────────────────────────
                    bool pickOk = await ExecutePickupAsync(i);

                    if (!pickOk)
                    {
                        Console.WriteLine(
                            $"[ALARM] '{Name}' ? MultiScanAndPickup: 픽업 실패 (다이 [{i}]).");
                        return false;
                    }

                    Console.WriteLine($"[INFO]  '{Name}' ? 다이 [{i}] 픽업 완료.");
                }

                dieIndex = batchEnd;
            }

            Console.WriteLine($"[INFO]  '{Name}' ? 모든 다이 픽업 완료.");
            return true;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Step 5: 자재 언로딩
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 웨이퍼 맵 처리 완료 후 스테이지를 언로딩 위치로 이동하고 로더에 교체를 요청한다.<br/>
        /// <para>
        /// 시퀀스:<br/>
        /// 1. StageY → 언로딩 위치로 이동<br/>
        /// 2. ExpanderZ → Up 위치로 이동 (테이프 텐션 해제)<br/>
        /// 3. 로더 유닛에 웨이퍼 교체(Change) 요청 신호 전송
        /// </para>
        /// </summary>
        /// <returns>시퀀스 전체 성공 시 true, 축 알람 발생 시 false</returns>
        public async Task<bool> UnloadWaferAsync()
        {
            // ── Step 1: 언로딩 위치로 이동 ───────────────────────────────
            Console.WriteLine(
                $"[INFO]  '{Name}' ? 언로딩 위치({Setup.UnloadPositionY}mm)로 StageY 이동.");
            await StageY.MoveAbsoluteAsync(Setup.UnloadPositionY, Recipe.MoveVelocity);

            if (StageY.IsAlarm)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? UnloadWafer: StageY 이동 실패.");
                return false;
            }

            // ── Step 2: ExpanderZ Up 이동 (텐션 해제) ────────────────────
            Console.WriteLine(
                $"[INFO]  '{Name}' ? ExpanderZ Up 위치({Setup.ExpanderUpPosition}mm) 이동. 텐션 해제.");
            await ExpanderZ.MoveAbsoluteAsync(Setup.ExpanderUpPosition, Recipe.MoveVelocity);

            if (ExpanderZ.IsAlarm)
            {
                Console.WriteLine($"[ALARM] '{Name}' ? UnloadWafer: ExpanderZ 이동 실패.");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "IS-EXPZ",
                    source: "InputStageUnit.UnloadWaferAsync",
                    message: $"ExpanderZ 알람 발생 (axis code={ExpanderZ.AlarmCode}).");
                return false;
            }

            // ── Step 3: 로더에 웨이퍼 교체 요청 신호 전송 ───────────────
            // IWaferLoader 인터페이스를 통한 가상 이벤트 발생
            Console.WriteLine($"[INFO]  '{Name}' ? 로더 유닛에 웨이퍼 교체(Change) 요청 신호 전송.");
            OnWaferChangeRequested();

            CurrentWaferMap = null;

            Console.WriteLine($"[INFO]  '{Name}' ? 웨이퍼 언로딩 완료. 다음 자재 대기 중.");
            return true;
        }

        // ??????????????????????????????????????????????????????????????????????
        //  §7. 이벤트
        // ??????????????????????????????????????????????????????????????????????

        /// <summary>
        /// 웨이퍼 교체 요청이 발생했을 때 발생하는 이벤트.<br/>
        /// 로더 유닛 또는 상위 Machine 클래스가 이 이벤트를 수신하여 교체 시퀀스를 시작한다.
        /// </summary>
        public event EventHandler WaferChangeRequested;

        /// <summary><see cref="WaferChangeRequested"/> 이벤트를 발생시킨다.</summary>
        protected virtual void OnWaferChangeRequested()
        {
            EventHandler handler = WaferChangeRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        // ??????????????????????????????????????????????????????????????????????
        //  §8. 내부 유틸리티 메서드
        // ??????????????????????????????????????????????????????????????????????

        /// <summary>
        /// 지정한 다이 좌표로 StageY와 CameraX를 동시에 이동한다.
        /// </summary>
        /// <param name="row">대상 다이의 행 인덱스</param>
        /// <param name="col">대상 다이의 열 인덱스</param>
        /// <param name="useEstimate">
        /// true이면 원점/피치 미수립 상태에서 Recipe 기본 피치로 좌표를 추정한다.<br/>
        /// false이면 수립된 <see cref="OriginX"/>, <see cref="OriginY"/>, <see cref="PitchX"/>, <see cref="PitchY"/>를 사용한다.
        /// </param>
        private async Task MoveToDieAsync(int row, int col, bool useEstimate = false)
        {
            double pitchX = useEstimate ? Recipe.DefaultPitchX : PitchX;
            double pitchY = useEstimate ? Recipe.DefaultPitchY : PitchY;
            double origX  = useEstimate ? 0.0               : OriginX;
            double origY  = useEstimate ? 0.0               : OriginY;

            double targetX = origX + col * pitchX;
            double targetY = origY + row * pitchY;

            Task moveY = StageY.MoveAbsoluteAsync(targetY, Recipe.AlignVelocity);
            Task moveX = CameraX.MoveAbsoluteAsync(targetX, Recipe.AlignVelocity);
            await Task.WhenAll(moveY, moveX);
        }

        /// <summary>
        /// NeedleZ 이젝트 동작을 포함한 단일 다이 픽업 시퀀스를 실행한다.<br/>
        /// <para>
        /// 내부 시퀀스:<br/>
        /// 1. NeedleVacuum On + 안정화 대기<br/>
        /// 2. TPU에 PickReady 신호 전송<br/>
        /// 3. NeedleZ Eject 위치로 상승 (TPU 픽커 하강과 동기)<br/>
        /// 4. TPU 픽커 상승 완료 대기<br/>
        /// 5. NeedleZ 하강 + NeedleVacuum Off
        /// </para>
        /// </summary>
        /// <param name="dieIndex">픽업 대상 글로벌 다이 인덱스</param>
        /// <returns>성공 시 true, 타임아웃 또는 축 알람 시 false</returns>
        private async Task<bool> ExecutePickupAsync(int dieIndex)
        {
            // ── 1. NeedleVacuum On ─────────────────────────────────────────
            NeedleVacuum.On();
            await Task.Delay(Recipe.NeedleVacuumSettleMs).ContinueWith(_ => { });

            // ── 2. TPU 픽업 가능 신호 전송 ────────────────────────────────
            Tpu.NotifyPickReady(dieIndex);

            // ── 3. NeedleZ 상승 (Eject) ───────────────────────────────────
            await NeedleZ.MoveAbsoluteAsync(Setup.NeedleEjectPosition, Recipe.NeedleVelocity);

            if (NeedleZ.IsAlarm)
            {
                NeedleVacuum.Off();
                Console.WriteLine($"[ALARM] '{Name}' ? ExecutePickup: NeedleZ 상승 실패 (다이 [{dieIndex}]).");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "IS-MOVE",
                    source: "InputStageUnit.ExecutePickupAsync",
                    message: $"NeedleZ 상승(Eject) 실패 (다이 [{dieIndex}], axis code={NeedleZ.AlarmCode}).");
                return false;
            }

            // ── 4. TPU 픽커 상승 완료 대기 ───────────────────────────────
            bool pickerUp = await Tpu.WaitPickerUpAsync(Recipe.PickerUpTimeoutMs);

            if (!pickerUp)
            {
                Console.WriteLine(
                    $"[ALARM] '{Name}' ? ExecutePickup: TPU 픽커 상승 타임아웃 (다이 [{dieIndex}]).");
                // 안전을 위해 NeedleZ 하강 + 진공 해제
                await NeedleZ.MoveAbsoluteAsync(Setup.NeedleDownPosition, Recipe.NeedleVelocity);
                NeedleVacuum.Off();
                return false;
            }

            // ── 5. NeedleZ 하강 + NeedleVacuum Off ───────────────────────
            await NeedleZ.MoveAbsoluteAsync(Setup.NeedleDownPosition, Recipe.NeedleVelocity);

            if (NeedleZ.IsAlarm)
            {
                NeedleVacuum.Off();
                Console.WriteLine($"[ALARM] '{Name}' ? ExecutePickup: NeedleZ 하강 실패 (다이 [{dieIndex}]).");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "IS-MOVE",
                    source: "InputStageUnit.ExecutePickupAsync",
                    message: $"NeedleZ 하강 실패 (다이 [{dieIndex}], axis code={NeedleZ.AlarmCode}).");
                return false;
            }

            NeedleVacuum.Off();
            return true;
        }
    }
}
