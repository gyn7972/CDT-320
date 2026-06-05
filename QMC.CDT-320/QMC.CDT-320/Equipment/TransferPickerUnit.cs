using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;

using Alarms = QMC.Common.Alarms;
namespace QMC.CDT320
{
    // ==========================================================================
    //  TPU 전용 비전 인터페이스 및 결과 DTO
    // ==========================================================================

    /// <summary>
    /// 개별 피커 1회 촬상에 대한 Bottom 비전 보정 결과.<br/>
    /// 비전 PC가 Bottom 카메라로 분석한 다이의 중심 오프셋을 담는다.
    /// </summary>
    public class BottomVisionOffset
    {
        /// <summary>피커 번호 (1~4).</summary>
        public int PickerNo { get; set; }

        /// <summary>X축 보정 오프셋 [mm]. 양수 = 오른쪽.</summary>
        public double OffsetX { get; set; }

        /// <summary>Y축 보정 오프셋 [mm]. 양수 = 앞쪽.</summary>
        public double OffsetY { get; set; }

        /// <summary>회전 보정 오프셋 [deg]. PlaceTheta 보정에 사용.</summary>
        public double OffsetT { get; set; }

        /// <summary>비전 판정 결과. true = OK, false = NG.</summary>
        public bool IsOk { get; set; }
    }

    /// <summary>
    /// Side 비전 4면 검사 결과.<br/>
    /// 회전 전 2면(Side 1·2)과 90도 회전 후 2면(Side 3·4)을 담는다.
    /// </summary>
    public class SideVisionResult
    {
        /// <summary>피커 번호 (1~4).</summary>
        public int PickerNo { get; set; }

        /// <summary>Side 1 검사 OK 여부.</summary>
        public bool Side1Ok { get; set; }

        /// <summary>Side 2 검사 OK 여부.</summary>
        public bool Side2Ok { get; set; }

        /// <summary>Side 3 검사 OK 여부 (90도 회전 후).</summary>
        public bool Side3Ok { get; set; }

        /// <summary>Side 4 검사 OK 여부 (90도 회전 후).</summary>
        public bool Side4Ok { get; set; }

        /// <summary>4개 Side 모두 OK인지 여부.</summary>
        public bool IsAllOk
        {
            get { return Side1Ok && Side2Ok && Side3Ok && Side4Ok; }
        }
    }

    /// <summary>
    /// TPU Bottom·Side 비전 전용 TCP/IP 통신 계약.<br/>
    /// <para>
    /// 핵심 설계 원칙:<br/>
    /// Trigger 계열 메서드는 <b>Expose End(노출 완료)</b> 응답만 반환한다.
    /// 이미지 연산 결과를 기다리지 않으므로, 반환 즉시 다음 모션으로 진입 가능하다.
    /// 검사 결과는 별도 <c>GetBottomResultsAsync</c> / <c>GetSideResultAsync</c>로 수집한다.
    /// </para>
    /// </summary>
    public interface IVisionTpuClient
    {
        // ── Bottom Vision ──────────────────────────────────────────────────────

        /// <summary>
        /// 지정 피커에 대한 Bottom 촬상을 트리거하고 <b>Expose End만 대기</b>한다.<br/>
        /// 이미지 분석은 비전 PC에서 백그라운드로 수행되며, 결과는
        /// <see cref="GetBottomResultsAsync"/>로 별도 수집한다.
        /// </summary>
        /// <param name="pickerNo">촬상 대상 피커 번호 (1~4)</param>
        /// <param name="timeoutMs">Expose End 대기 타임아웃 [ms]</param>
        /// <returns>Expose End 수신 시 true, 타임아웃/오류 시 false.</returns>
        Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs = 1000);

        /// <summary>
        /// 4개 피커의 Bottom 검사 결과 배열을 수신한다.<br/>
        /// 모든 피커의 촬상 트리거가 완료된 이후 호출한다.
        /// </summary>
        /// <param name="timeoutMs">결과 수신 대기 타임아웃 [ms]</param>
        /// <returns>피커별 <see cref="BottomVisionOffset"/> 배열 (인덱스 0 = 피커 1번). 실패 시 null.</returns>
        Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs = 5000);

        // ── Side Vision ────────────────────────────────────────────────────────

        /// <summary>
        /// 지정 피커의 Side N번 면 촬상을 트리거하고 <b>Expose End만 대기</b>한다.<br/>
        /// 회전 전 2면(Side 1, 2)과 90도 회전 후 2면(Side 3, 4)에 각각 호출된다.
        /// </summary>
        /// <param name="pickerNo">촬상 대상 피커 번호 (1~4)</param>
        /// <param name="sideNo">Side 번호 (1~4)</param>
        /// <param name="timeoutMs">Expose End 대기 타임아웃 [ms]</param>
        /// <returns>Expose End 수신 시 true, 타임아웃/오류 시 false.</returns>
        Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs = 1000);

        /// <summary>
        /// 지정 피커의 Side 검사 결과를 수신한다.<br/>
        /// 해당 피커의 4면 촬상이 완료된 이후 호출한다.
        /// </summary>
        /// <param name="pickerNo">결과를 수집할 피커 번호 (1~4)</param>
        /// <param name="timeoutMs">결과 수신 대기 타임아웃 [ms]</param>
        /// <returns>Side 검사 결과. 실패 시 null.</returns>
        Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs = 5000);
    }

    // ==========================================================================
    //  PickerComponent 전용 데이터 클래스
    // ==========================================================================

    /// <summary>
    /// 개별 픽커의 기구적 설정값.<br/>
    /// 진공 안정화 시간, Blow 펄스 시간 등 하드웨어 교체 전까지 유지되는 값.
    /// </summary>
    public class PickerSetup : ISetupData
    {
        /// <summary>PickerZ 픽업 하강 절대 위치 [mm]. Wafer 접촉 위치.</summary>
        public double PickupPosition { get; set; } = -5.0;

        /// <summary>PickerZ 포커스(촬상) 절대 위치 [mm].</summary>
        public double FocusPosition  { get; set; } = -2.0;

        /// <summary>PickerZ 대기(상승) 절대 위치 [mm].</summary>
        public double WaitPosition   { get; set; } = 0.0;

        /// <summary>Place 시 PickerZ 하강 절대 위치 [mm].</summary>
        public double PlacePosition  { get; set; } = -4.0;

        /// <summary>Collet 교체 모드에서 측정한 picker 의 ArmX 보정 오프셋 [mm]. 각 picker 별로 존재.</summary>
        public double ColletOffsetX  { get; set; } = 0.0;

        /// <summary>Collet 교체 모드에서 측정한 picker 의 ArmY 보정 오프셋 [mm]. 각 picker 별로 존재.</summary>
        public double ColletOffsetY  { get; set; } = 0.0;
    }

    /// <summary>픽커 고정 사양 파라미터.</summary>
    public class PickerConfig : IConfigData
    {
        /// <summary>시뮬레이션 모드 여부.</summary>
        public bool IsSimulationMode { get; set; } = true;
    }

    /// <summary>픽커 공정별 작업 파라미터.</summary>
    public class PickerRecipe : IRecipeData
    {
        /// <summary>PickerZ 이동 속도 [mm/s]. xlsx FRONT PICKER_Z = 1000 mm/s 반영.</summary>
        public double ZVelocity     { get; set; } = 1000.0;

        /// <summary>PickerT 회전 속도 [deg/s]. xlsx 36000 은 비현실 — 실용값 1000 적용.</summary>
        public double ThetaVelocity { get; set; } = 1000.0;

        /// <summary>진공 흡착 안정화 대기 시간 [ms].</summary>
        public int VacuumSettleMs   { get; set; } = 50;

        /// <summary>Blow(파기) 펄스 지속 시간 [ms].</summary>
        public int BlowPulseMs      { get; set; } = 100;

        // ─── PICK 시퀀스 ③④ 단계 레시피 항목 ───

        /// <summary>PICK ③단계 — Lift 상승 오프셋 [mm].
        /// Needle Up 위치 = NeedleDownPosition + PickLiftPosition (대기 위치에서 위로 +).
        /// Picker Up 위치 = PickupPosition + PickLiftPosition (Pickup 위치에서 위로 +).
        /// 제품(다이 두께/접착력)별로 조정.</summary>
        public double PickLiftPosition { get; set; } = 2.0;

        /// <summary>PICK ④단계 — Needle Up + Picker Up 완료 후 진공 안정화/다이 분리 안정화 대기 [ms].</summary>
        public int PickLiftWaitMs      { get; set; } = 50;

        /// <summary>PLACE — PickerZ 하강 + Blow ON 후 다이가 안정적으로 떨어질 때까지 대기 [ms].</summary>
        public int PlaceDelayMs        { get; set; } = 50;
    }

    // ==========================================================================
    //  TpuArmUnit 전용 데이터 클래스
    // ==========================================================================

    /// <summary>
    /// TpuArm의 기구적 설정값.<br/>
    /// Bottom/Side Vision 고정 위치, 피커 간격 등을 담는다.
    /// </summary>
    public class TpuArmSetup : ISetupData
    {
        /// <summary>Bottom Vision 카메라 X축 기준 위치 [mm]. 피커 1번 기준.</summary>
        public double BottomVisionX  { get; set; } = 100.0;

        /// <summary>Bottom Vision 카메라 Y축 고정 위치 [mm].</summary>
        public double BottomVisionY  { get; set; } = 0.0;

        /// <summary>Side Vision 1번 카메라 X축 기준 위치 [mm]. 피커 1번 기준.</summary>
        public double SideVision1X   { get; set; } = 850.0;

        /// <summary>Side Vision 1번 카메라 Y축 기준 위치 [mm].</summary>
        public double SideVision1Y   { get; set; } = 0.0;

        /// <summary>Side Vision 2번 카메라 X축 기준 위치 [mm]. 피커 1번 기준. (Side1 과 동일 X 위치)</summary>
        public double SideVision2X   { get; set; } = 850.0;

        /// <summary>Side Vision 2번 카메라 Y축 기준 위치 [mm].</summary>
        public double SideVision2Y   { get; set; } = 0.0;

        /// <summary>
        /// 인접 피커 간 ArmX 방향 간격 [mm].<br/>
        /// 피커 N번의 포커스 X = 기준 X + (N-1) * PickerPitchX
        /// </summary>
        public double PickerPitchX   { get; set; } = 50.0;

        /// <summary>Place 이동 목표 X 위치 [mm].</summary>
        public double PlacePositionX { get; set; } = 300.0;

        /// <summary>Place 이동 목표 Y 위치 [mm].</summary>
        public double PlacePositionY { get; set; } = 50.0;

        // Stage 60 R-teach — PositionTeachingPage 보강용 신규 위치 필드
        /// <summary>InputStage 픽업 진입 시 ArmX 위치 [mm].</summary>
        public double ArmInputPositionX     { get; set; } = 300.0;
        /// <summary>Bottom/Side Vision 검사 진입 시 ArmX 위치 [mm].</summary>
        public double ArmInspectionPositionX{ get; set; } = 750.0;
        /// <summary>OutputStage Place 시 ArmX 위치 [mm].</summary>
        public double ArmOutputPositionX    { get; set; } = 1200.0;
        /// <summary>SideVision 갠트리 Y 베이스 위치 [mm].</summary>
        public double SideVisionY0          { get; set; } = 0.0;

        // Stage 61 — ArmY 픽업/회피 위치
        /// <summary>다이 픽업 시 ArmY 절대 위치 [mm].</summary>
        public double ArmYPickupPosition    { get; set; } = 100.0;
        /// <summary>이동 중 회피 (간섭 회피) ArmY 위치 [mm].</summary>
        public double ArmYAvoidPosition     { get; set; } = 50.0;
    }

    /// <summary>TpuArm 고정 사양 파라미터.</summary>
    public class TpuArmConfig : IConfigData
    {
        /// <summary>시뮬레이션 모드 여부.</summary>
        public bool IsSimulationMode    { get; set; } = true;

        /// <summary>비전 Expose End 대기 타임아웃 [ms].</summary>
        public int VisionExposeTimeoutMs { get; set; } = 1000;

        /// <summary>비전 검사 결과 수신 타임아웃 [ms].</summary>
        public int VisionResultTimeoutMs { get; set; } = 5000;
    }

    /// <summary>TpuArm 공정별 작업 파라미터.</summary>
    public class TpuArmRecipe : IRecipeData
    {
        /// <summary>ArmX 이동 속도 [mm/s]. xlsx FRONT PICKER_X = 2000 mm/s 반영.</summary>
        public double ArmXVelocity { get; set; } = 2000.0;

        /// <summary>ArmY 이동 속도 [mm/s]. xlsx FRONT PICKER_Y = 500 mm/s 반영.</summary>
        public double ArmYVelocity { get; set; } = 500.0;
    }

    // ==========================================================================
    //  TransferPickerUnit 전용 데이터 클래스
    // ==========================================================================

    /// <summary>TransferPickerUnit 기구적 설정값.</summary>
    public class TpuSetup : ISetupData { }

    /// <summary>TransferPickerUnit 고정 사양 파라미터.</summary>
    public class TpuConfig : IConfigData { }

    /// <summary>TransferPickerUnit 공정별 작업 파라미터.</summary>
    public class TpuRecipe : IRecipeData { }

    // ==========================================================================
    //  A. PickerComponent - 개별 픽업 툴 (좌·우 Arm 각 4개, 총 8개)
    // ==========================================================================

    /// <summary>
    /// 개별 픽업 툴 컴포넌트.<br/>
    /// Z축(픽업/포커스/대기)과 T축(Theta 보정)을 보유하며,
    /// 진공·Blow DO를 통해 다이를 흡착·파기한다.
    /// <para>
    /// 계층 위치: <c>TransferPickerUnit → TpuArmUnit → PickerComponent</c>
    /// </para>
    /// </summary>
    public class PickerComponent : BaseComponent<PickerSetup, PickerConfig, PickerRecipe>
    {
        // ----------------------------------------------------------------------
        //  A-1. 하드웨어 멤버
        // ----------------------------------------------------------------------

        /// <summary>
        /// 픽커 Z축.<br/>
        /// WaitPosition(대기) ↔ FocusPosition(포커스) ↔ PickupPosition(픽업) 간 이동.
        /// </summary>
        public BaseAxis PickerZ { get; private set; }

        /// <summary>
        /// 픽커 Theta(회전) 축.<br/>
        /// 다이 각도 보정 및 Side 촬상을 위한 90도 회전에 사용된다.
        /// </summary>
        public BaseAxis PickerT { get; private set; }

        /// <summary>
        /// 진공 흡착 DO.<br/>
        /// On 상태에서 다이를 픽커 노즐에 흡착·유지한다.
        /// </summary>
        public BaseDigitalOutput VacuumOut { get; private set; }

        /// <summary>
        /// Blow(에어 파기) DO.<br/>
        /// On 상태에서 순간 에어를 분사하여 다이를 Place 위치에 안착시킨다.
        /// </summary>
        public BaseDigitalOutput BlowOut { get; private set; }

        // ----------------------------------------------------------------------
        //  A-2. 생성자
        // ----------------------------------------------------------------------

        /// <summary>
        /// <see cref="PickerComponent"/>를 초기화한다.
        /// </summary>
        /// <param name="pickerNo">피커 번호 (1~4). 이름 접두사에 사용된다.</param>
        public PickerComponent(int pickerNo) : base("Picker" + pickerNo)
        {
            string prefix = "Picker" + pickerNo + "_";

            PickerZ   = AjinFactory.CreateAxis(prefix + "Z");
            PickerT   = AjinFactory.CreateAxis(prefix + "T");
            VacuumOut = AjinFactory.CreateDigitalOutput(prefix + "Vacuum");
            BlowOut   = AjinFactory.CreateDigitalOutput(prefix + "Blow");
        }

        // ----------------------------------------------------------------------
        //  A-3. 기본 제어 메서드
        // ----------------------------------------------------------------------

        /// <summary>진공을 ON하여 다이를 흡착한다.</summary>
        public void VacuumOn()  { VacuumOut.On(); }

        /// <summary>진공을 OFF하여 흡착을 해제한다.</summary>
        public void VacuumOff() { VacuumOut.Off(); }

        /// <summary>Blow를 ON하여 에어를 분사한다.</summary>
        public void BlowOn()    { BlowOut.On(); }

        /// <summary>Blow를 OFF하여 에어 분사를 중단한다.</summary>
        public void BlowOff()   { BlowOut.Off(); }

        /// <summary>
        /// PickerZ를 픽업 위치로 하강시키고 진공을 ON하여 다이를 픽업한다.
        /// </summary>
        /// <returns>성공 시 true, PickerZ 알람 시 false.</returns>
        public async Task<bool> PickupAsync()
        {
            await PickerZ.MoveAbsoluteAsync(Setup.PickupPosition, Recipe.ZVelocity);

            if (PickerZ.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> Pickup: PickerZ 하강 실패.");
                return false;
            }

            VacuumOn();

            // 진공 흡착 안정화 대기
            await Task.Delay(Recipe.VacuumSettleMs).ContinueWith(_ => { });
            return true;
        }

        /// <summary>
        /// PickerZ를 포커스 위치로 이동한다.<br/>
        /// Bottom/Side Vision 촬상 전 호출한다.
        /// </summary>
        /// <returns>성공 시 true, PickerZ 알람 시 false.</returns>
        public async Task<bool> MoveToFocusAsync()
        {
            await PickerZ.MoveAbsoluteAsync(Setup.FocusPosition, Recipe.ZVelocity);

            if (PickerZ.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> MoveToFocus: PickerZ 이동 실패.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// PickerZ를 대기(상승) 위치로 복귀시킨다.
        /// </summary>
        /// <returns>성공 시 true, PickerZ 알람 시 false.</returns>
        public async Task<bool> MoveToWaitAsync()
        {
            await PickerZ.MoveAbsoluteAsync(Setup.WaitPosition, Recipe.ZVelocity);

            if (PickerZ.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> MoveToWait: PickerZ 이동 실패.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// PickerZ를 Place 위치로 하강시키고, Blow 펄스를 발생시켜 다이를 배출한 뒤
        /// PickerZ를 대기 위치로 복귀시킨다.
        /// </summary>
        /// <returns>성공 시 true, PickerZ 알람 시 false.</returns>
        public async Task<bool> PlaceAsync()
        {
            // 하강
            await PickerZ.MoveAbsoluteAsync(Setup.PlacePosition, Recipe.ZVelocity);
            if (PickerZ.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> Place: PickerZ 하강 실패.");
                return false;
            }

            // 진공 OFF + Blow 파기 펄스
            VacuumOff();
            BlowOn();
            await Task.Delay(Recipe.BlowPulseMs).ContinueWith(_ => { });
            BlowOff();

            // 상승 복귀
            await PickerZ.MoveAbsoluteAsync(Setup.WaitPosition, Recipe.ZVelocity);
            if (PickerZ.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> Place: PickerZ 상승 복귀 실패.");
                return false;
            }

            return true;
        }
    }

    // ==========================================================================
    //  B. TpuArmUnit - X·Y축 + 4-Picker 암 유닛 (좌·우 2개 생성됨)
    // ==========================================================================

    /// <summary>
    /// Transfer Picker 암(Arm) 유닛.<br/>
    /// ArmX·ArmY 갠트리 축과 4개의 <see cref="PickerComponent"/>를 묶는 중간 계층 유닛이다.
    /// <para>
    /// 이 유닛 내부에서 Bottom Vision 촬상(<see cref="InspectBottomVisionAsync"/>),
    /// Side Vision 촬상(<see cref="InspectSideVisionAsync"/>),
    /// Place 배출(<see cref="PlaceDiesAsync"/>) 시퀀스가 실행된다.
    /// </para>
    /// <para>
    /// 계층 위치: <c>TransferPickerUnit → TpuArmUnit</c>
    /// </para>
    /// </summary>
    public class TpuArmUnit : BaseUnit<TpuArmSetup, TpuArmConfig, TpuArmRecipe>
    {
        // ----------------------------------------------------------------------
        //  B-1. 하드웨어 컴포넌트
        // ----------------------------------------------------------------------

        /// <summary>
        /// 암 X축.<br/>
        /// InputStage / Vision / OutputStage 위치 간 전체 이동 및
        /// Bottom·Side Vision 포커스 위치 정렬에 사용된다.
        /// </summary>
        public BaseAxis ArmX { get; private set; }

        /// <summary>
        /// 암 Y축.<br/>
        /// 4개의 픽커가 매달려 있으며, Side Vision 정밀 포커스 Y 위치 정렬에 사용된다.
        /// </summary>
        public BaseAxis ArmY { get; private set; }

        /// <summary>
        /// Stage 44 — Side Vision Y 축 (Simulator axis 19/20 호환).<br/>
        /// 4면 검사 시 Side 카메라를 다이 정면으로 이동.
        /// </summary>
        public BaseAxis SideVisionY { get; private set; }

        /// <summary>
        /// 4개의 개별 픽업 툴 컴포넌트 배열.<br/>
        /// 인덱스 0 = Picker1, 인덱스 3 = Picker4.
        /// </summary>
        public PickerComponent[] Pickers { get; private set; }

        // ----------------------------------------------------------------------
        //  B-2. 외부 연동 인터페이스
        // ----------------------------------------------------------------------

        /// <summary>Bottom·Side Vision TCP/IP 통신 클라이언트.</summary>
        public IVisionTpuClient Vision { get; private set; }

        // ----------------------------------------------------------------------
        //  B-3. 생성자
        // ----------------------------------------------------------------------

        /// <summary>
        /// <see cref="TpuArmUnit"/>을 초기화하고 ArmX·ArmY 및 4개의
        /// <see cref="PickerComponent"/>를 생성하여 Composite 트리에 등록한다.
        /// </summary>
        /// <param name="armName">암 이름 (예: "LeftArm", "RightArm")</param>
        /// <param name="vision">비전 TCP/IP 클라이언트</param>
        public TpuArmUnit(string armName, IVisionTpuClient vision) : base(armName)
        {
            if (vision == null) throw new ArgumentNullException("vision");
            Vision = vision;

            // 모션 축 생성
            ArmX = AjinFactory.CreateAxis(armName + "_ArmX");
            ArmY = AjinFactory.CreateAxis(armName + "_ArmY");
            // Stage 44 — Side Vision Y 축 (Front=axis 19, Rear=axis 20 매뉴얼 호환)
            SideVisionY = AjinFactory.CreateAxis(armName + "_SideVisionY");

            // Stage 29 — TPU Arm SoftLimit 확장
            //   ArmX: InputStage(300) → BottomVision(700) → SideVision(900) → OutputStage(1200~1500) 까지 이동
            //   ArmY: 0~300mm
            ArmX.Setup.SoftLimitPlus = 1600.0;
            ArmY.Setup.SoftLimitPlus = 350.0;
            SideVisionY.Setup.SoftLimitPlus = 250.0;

            // 4개 PickerComponent 인스턴스화 (번호 1~4)
            Pickers = new PickerComponent[4];
            for (int i = 0; i < 4; i++)
            {
                Pickers[i] = new PickerComponent(i + 1);
                // Stage 29 — Picker Z/T SoftLimit 확장
                Pickers[i].PickerZ.Setup.SoftLimitPlus  = 100.0;
                Pickers[i].PickerT.Setup.SoftLimitPlus  = 360.0;
                Pickers[i].PickerT.Setup.SoftLimitMinus = -360.0;
            }

            // Composite 트리 등록 - Save() 등 공통 동작이 전체 하위 트리에 전파됨
            Components.Add(ArmX);
            Components.Add(ArmY);
            Components.Add(SideVisionY);
            foreach (PickerComponent picker in Pickers)
                Components.Add(picker);
        }

        // ======================================================================
        //  B-4. Step 1: Bottom Vision 촬상
        // ======================================================================

        /// <summary>
        /// 4개 피커를 순서대로 Bottom Vision 고정 위치로 이동하여 촬상하고,
        /// 전체 촬상 완료 후 비전 PC로부터 4개 피커의 (X, Y) 보정 오프셋 배열을 수신한다.
        /// <para>
        /// 비전 파이프라인 설계:<br/>
        /// <see cref="IVisionTpuClient.TriggerBottomExposeAsync"/>는
        /// <b>Expose End(노출 완료)만 반환</b>한다. 반환 즉시 다음 피커 위치로 이동하므로
        /// 4개 피커의 이미지 분석은 비전 PC에서 모션과 병렬로 누적 처리된다.<br/>
        /// 모든 촬상이 끝난 뒤 <see cref="IVisionTpuClient.GetBottomResultsAsync"/>로
        /// 결과를 일괄 수신한다.
        /// </para>
        /// </summary>
        /// <returns>
        /// 피커 1~4번 순서의 <see cref="BottomVisionOffset"/> 배열.
        /// 비전 통신 오류 또는 모션 알람 시 null.
        /// </returns>
        public async Task<BottomVisionOffset[]> InspectBottomVisionAsync()
        {
            Console.WriteLine("[INFO]  '" + Name + "' -> Bottom Vision 촬상 시작.");

            for (int i = 0; i < Pickers.Length; i++)
            {
                PickerComponent picker   = Pickers[i];
                int             pickerNo = i + 1; // 1-based 번호

                // Bottom Vision 카메라는 고정 (모션 없음).
                // Picker 를 ArmInspectionPositionX 기준으로 이동:
                //   ArmX = ArmInspectionPositionX + i * PickerPitchX + Picker[i].Setup.ColletOffsetX
                //   ArmY = Picker[i].Setup.ColletOffsetY
                // ColletOffset 은 Collet 교체 모드에서 측정한 picker 별 보정값.
                double focusX = Setup.ArmInspectionPositionX
                                + i * Setup.PickerPitchX
                                + picker.Setup.ColletOffsetX;
                double focusY = picker.Setup.ColletOffsetY;

                // ArmX, ArmY 동시 이동
                await Task.WhenAll(
                    ArmX.MoveAbsoluteAsync(focusX, Recipe.ArmXVelocity),
                    ArmY.MoveAbsoluteAsync(focusY, Recipe.ArmYVelocity));

                if (ArmX.IsAlarm || ArmY.IsAlarm)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> InspectBottom: Arm 이동 실패 (Picker" +
                        pickerNo + ").");
                    return null;
                }

                // PickerZ 는 이미 외부에서 FocusPosition 으로 이동됨 (Bottom Vision 검사 진입 단계 14번).
                // 별도 PickerZ 이동 없이 즉시 Expose.

                // ── Bottom Trigger: Expose End만 대기 ────────────────────────
                // TriggerBottomExposeAsync()는 카메라 노출 완료(Expose End) 신호만
                // 반환한다. 이미지 분석은 비전 PC 백그라운드에서 진행되므로
                // 반환 즉시 다음 피커 이동으로 넘어가 전체 사이클 타임을 단축한다.
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Picker" + pickerNo +
                    " Bottom Trigger 전송. Expose End 대기 중...");

                bool exposed = await Vision.TriggerBottomExposeAsync(
                    pickerNo, Config.VisionExposeTimeoutMs);

                if (!exposed)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> InspectBottom: Expose End 수신 실패 " +
                        "(Picker" + pickerNo + ").");
                    // OS-12 (Stage 60 cycle 4) — EXPOSE-TIMEOUT Raise
                    QMC.Common.Alarms.AlarmManager.Raise(
                        QMC.Common.Alarms.AlarmSeverity.Error,
                        "EXPOSE-TIMEOUT", Name,
                        "Bottom Vision Expose End 수신 실패 (Picker" + pickerNo +
                        ", timeout=" + Config.VisionExposeTimeoutMs + "ms)");
                    return null;
                }

                // Expose End 수신 → 이미지 분석 대기 없이 즉시 다음 피커로 이동
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Picker" + pickerNo +
                    " Expose End 수신. 즉시 다음 피커 이동.");
            }

            // 4개 피커 촬상 완료 후 결과 일괄 수신
            // 비전 PC는 4번의 촬상 동안 분석을 완료했으므로 대기 시간이 최소화된다.
            Console.WriteLine("[INFO]  '" + Name + "' -> 4개 피커 촬상 완료. Bottom 결과 수신 중...");

            BottomVisionOffset[] results =
                await Vision.GetBottomResultsAsync(Config.VisionResultTimeoutMs);

            if (results == null)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> InspectBottom: 결과 수신 실패.");
                return null;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> Bottom Vision 완료. 결과 수신 건수: " + results.Length);
            return results;
        }

        // ======================================================================
        //  B-5. Step 2: Side Vision 촬상 (4면 × 4피커)
        // ======================================================================

        /// <summary>
        /// Bottom Vision 오프셋을 적용하여 각 피커를 Side Vision 포커스 위치로 정렬하고
        /// 4면(Side 1~4) 검사를 수행한다.
        /// <para>
        /// 비전 파이프라인 + 동시 구동 설계:<br/>
        /// 1. Side 1 Trigger → <b>Expose End 대기</b> → 즉시 Side 2 위치로 전환.<br/>
        /// 2. Side 2 Trigger → <b>Expose End 대기</b>.<br/>
        /// 3. 나머지 2면 준비: <b><c>Task.WhenAll</c></b>로 PickerT 90도 회전과
        ///    Side 3 X/Y 포커스 이동을 <b>동시 실행</b>하여 이동 시간을 단축.<br/>
        /// 4. Side 3 Trigger → <b>Expose End 대기</b> → Side 4 위치 이동.<br/>
        /// 5. Side 4 Trigger → <b>Expose End 대기</b> → 결과 수신 → 다음 피커 반복.
        /// </para>
        /// </summary>
        /// <param name="bottomOffsets">
        /// <see cref="InspectBottomVisionAsync"/>에서 수신한 피커별 Bottom 오프셋 배열.
        /// null이면 오프셋 미적용.
        /// </param>
        /// <returns>
        /// 피커 1~4번 순서의 <see cref="SideVisionResult"/> 배열.
        /// 비전 통신 오류 또는 모션 알람 시 null.
        /// </returns>
        public async Task<SideVisionResult[]> InspectSideVisionAsync(
            BottomVisionOffset[] bottomOffsets, double dieSizeXMm = 0.0, double dieSizeYMm = 0.0)
        {
            Console.WriteLine("[INFO]  '" + Name + "' -> Side Vision 촬상 시작.");

            SideVisionResult[] results = new SideVisionResult[Pickers.Length];

            for (int i = 0; i < Pickers.Length; i++)
            {
                PickerComponent picker   = Pickers[i];
                int             pickerNo = i + 1;

                // Bottom Offset 적용 (제공되지 않으면 0으로 처리)
                double offsetX = 0.0;
                double offsetY = 0.0;
                if (bottomOffsets != null && i < bottomOffsets.Length &&
                    bottomOffsets[i] != null)
                {
                    offsetX = bottomOffsets[i].OffsetX;
                    offsetY = bottomOffsets[i].OffsetY;
                }

                // ──────────────────────────────────────────────────────────────
                //  [회전 전] Side 1, 2 촬상 (2면)
                // ──────────────────────────────────────────────────────────────

                // Side Vision 1번 카메라 X/Y 포커스 위치 계산 (Bottom Offset 적용)
                double side1X = Setup.SideVision1X + i * Setup.PickerPitchX + offsetX;
                double side1Y = Setup.SideVision1Y + offsetY;

                // ArmX, ArmY 동시 이동
                await Task.WhenAll(
                    ArmX.MoveAbsoluteAsync(side1X, Recipe.ArmXVelocity),
                    ArmY.MoveAbsoluteAsync(side1Y, Recipe.ArmYVelocity));

                if (ArmX.IsAlarm || ArmY.IsAlarm)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> InspectSide: Side1 위치 이동 실패 " +
                        "(Picker" + pickerNo + ").");
                    return null;
                }

                // PickerZ 포커스 위치 하강
                bool focusOk = await picker.MoveToFocusAsync();
                if (!focusOk) return null;

                // ── Side 1 Trigger: Expose End만 대기 ────────────────────────
                // 노출 완료 신호를 받는 즉시 Side 2 위치로 이동하여 사이클 타임 절약
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Picker" + pickerNo +
                    " Side 1 Trigger. Expose End 대기 중...");

                bool side1Exposed = await Vision.TriggerSideExposeAsync(
                    pickerNo, 1, Config.VisionExposeTimeoutMs);

                if (!side1Exposed)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> InspectSide: Side1 Expose End 실패 " +
                        "(Picker" + pickerNo + ").");
                    return null;
                }

                // Expose End 수신 → 즉시 Side 2 위치로 전환
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Picker" + pickerNo +
                    " Side1 Expose End. Side 2로 즉시 이동.");

                // Side Vision 2번 카메라 X/Y 포커스 위치 계산
                double side2X = Setup.SideVision2X + i * Setup.PickerPitchX + offsetX;
                double side2Y = Setup.SideVision2Y + offsetY;

                await Task.WhenAll(
                    ArmX.MoveAbsoluteAsync(side2X, Recipe.ArmXVelocity),
                    ArmY.MoveAbsoluteAsync(side2Y, Recipe.ArmYVelocity));

                if (ArmX.IsAlarm || ArmY.IsAlarm)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> InspectSide: Side2 위치 이동 실패 " +
                        "(Picker" + pickerNo + ").");
                    return null;
                }

                // ── Side 2 Trigger: Expose End만 대기 ────────────────────────
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Picker" + pickerNo +
                    " Side 2 Trigger. Expose End 대기 중...");

                bool side2Exposed = await Vision.TriggerSideExposeAsync(
                    pickerNo, 2, Config.VisionExposeTimeoutMs);

                if (!side2Exposed)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> InspectSide: Side2 Expose End 실패 " +
                        "(Picker" + pickerNo + ").");
                    return null;
                }

                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Picker" + pickerNo +
                    " Side2 Expose End. 90도 회전 + Side3 포커스 이동 동시 시작.");

                // ──────────────────────────────────────────────────────────────
                //  [핵심] Task.WhenAll: PickerT 90도 회전 + ArmX/ArmY 재정렬 동시 구동
                //
                //  PickerT 회전(기계적으로 가장 느린 동작)과 다음 촬상 위치로의
                //  ArmX/ArmY 이동을 동시에 실행한다.
                //  두 동작 중 더 오래 걸리는 쪽에 맞춰 대기하므로 사이클 타임 최소화.
                // ──────────────────────────────────────────────────────────────

                // Side 3은 Side 1번 카메라를 재활용 (90도 회전 후 다른 2면 촬상)
                // 90도 회전 시 다이의 X,Y 가 swap 되므로 ArmY 가 (DieX - DieY) 만큼 변함
                double rotateYDelta = dieSizeXMm - dieSizeYMm;
                double side3X = Setup.SideVision1X + i * Setup.PickerPitchX + offsetX;
                double side3Y = Setup.SideVision1Y + offsetY + rotateYDelta;

                Task rotateTask = picker.PickerT.MoveRelativeAsync(
                    90.0, picker.Recipe.ThetaVelocity);

                Task focusMoveTask = Task.WhenAll(
                    ArmX.MoveAbsoluteAsync(side3X, Recipe.ArmXVelocity),
                    ArmY.MoveAbsoluteAsync(side3Y, Recipe.ArmYVelocity));

                // 회전과 X/Y 포커스 이동을 동시 대기
                await Task.WhenAll(rotateTask, focusMoveTask);

                if (picker.PickerT.IsAlarm || ArmX.IsAlarm || ArmY.IsAlarm)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> InspectSide: 90도 회전 또는 Side3 위치 이동 실패 " +
                        "(Picker" + pickerNo + ").");
                    return null;
                }

                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Picker" + pickerNo +
                    " 90도 회전 + Side3 포커스 이동 완료.");

                // ──────────────────────────────────────────────────────────────
                //  [회전 후] Side 3, 4 촬상 (2면)
                // ──────────────────────────────────────────────────────────────

                // ── Side 3 Trigger: Expose End만 대기 ────────────────────────
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Picker" + pickerNo +
                    " Side 3 Trigger. Expose End 대기 중...");

                bool side3Exposed = await Vision.TriggerSideExposeAsync(
                    pickerNo, 3, Config.VisionExposeTimeoutMs);

                if (!side3Exposed)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> InspectSide: Side3 Expose End 실패 " +
                        "(Picker" + pickerNo + ").");
                    return null;
                }

                // Expose End 수신 → 즉시 Side 4 위치로 전환 (Side 2번 카메라 재활용)
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Picker" + pickerNo +
                    " Side3 Expose End. Side 4로 즉시 이동.");

                double side4X = Setup.SideVision2X + i * Setup.PickerPitchX + offsetX;
                double side4Y = Setup.SideVision2Y + offsetY + rotateYDelta;

                await Task.WhenAll(
                    ArmX.MoveAbsoluteAsync(side4X, Recipe.ArmXVelocity),
                    ArmY.MoveAbsoluteAsync(side4Y, Recipe.ArmYVelocity));

                if (ArmX.IsAlarm || ArmY.IsAlarm)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> InspectSide: Side4 위치 이동 실패 " +
                        "(Picker" + pickerNo + ").");
                    return null;
                }

                // ── Side 4 Trigger: Expose End만 대기 ────────────────────────
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Picker" + pickerNo +
                    " Side 4 Trigger. Expose End 대기 중...");

                bool side4Exposed = await Vision.TriggerSideExposeAsync(
                    pickerNo, 4, Config.VisionExposeTimeoutMs);

                if (!side4Exposed)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> InspectSide: Side4 Expose End 실패 " +
                        "(Picker" + pickerNo + ").");
                    return null;
                }

                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Picker" + pickerNo +
                    " Side4 Expose End. 결과 수신 중...");

                // 해당 피커의 4면 촬상이 완료되었으므로 비전 PC에서 분석이 완료됨
                SideVisionResult sideResult =
                    await Vision.GetSideResultAsync(pickerNo, Config.VisionResultTimeoutMs);

                if (sideResult == null)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> InspectSide: Side 결과 수신 실패 " +
                        "(Picker" + pickerNo + ").");
                    return null;
                }

                results[i] = sideResult;

                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Picker" + pickerNo +
                    " Side 검사 결과: " + (sideResult.IsAllOk ? "ALL OK" : "NG"));
            }

            Console.WriteLine("[INFO]  '" + Name + "' -> Side Vision 완료.");
            return results;
        }

        // ======================================================================
        //  B-5b. Bottom + Side 병렬 파이프라인 (Step 15 신규)
        //
        //  좌표 모델: picker N 절대 X = ArmX - N × PickerPitchX
        //  (N=0 가 가장 오른쪽, N 증가 시 좌측으로 PickerPitchX 만큼 떨어져 위치)
        //
        //  파이프라인 (N=4 기준 totalSteps = 6):
        //    Step 0: ArmX=ArmInspectionPositionX           → Bottom(0)
        //    Step 1: ArmX=ArmInspectionPositionX +   pitch → Bottom(1)
        //    Step 2: ArmX=ArmInspectionPositionX + 2*pitch → Bottom(2) ∥ Side(0)
        //    Step 3: ArmX=ArmInspectionPositionX + 3*pitch → Bottom(3) ∥ Side(1)
        //    Step 4: ArmX=SideVision1X + 2*pitch           → Side(2)
        //    Step 5: ArmX=SideVision1X + 3*pitch           → Side(3)
        //
        //  Step 0~1 Z 다운(FocusPosition) — 해당 picker 만, 나머지는 WaitPosition 유지.
        //  Step 2~3 에서 bottomIdx 이미 측정 끝났지만 결과 수신은 Step 3 후 일괄.
        //  Side(0)/Side(1) 는 bottom 결과 수신 전이라 bottom offset 미적용,
        //  Side(2)/Side(3) 만 bottom offset 적용.
        //  Z 업(WaitPosition) 은 각 picker 자신의 Side sub-sequence 마지막에 발생.
        //
        //  Side sub-sequence:
        //    1) Side1 Expose
        //    2) Side2 Expose
        //    3) PickerT +90° ∥ SideVisionY → (base + offY + rotateYDelta)
        //    4) Side1 Expose (rotated)
        //    5) Side2 Expose (rotated)
        //    6) PickerT -90° ∥ PickerZ → WaitPosition
        //    7) GetSideResultAsync
        // ======================================================================

        /// <summary>
        /// Bottom Vision 4 picker 와 Side Vision 4 picker 4면 검사를 단일 ArmX
        /// 파이프라인에서 병렬 실행한다.<br/>
        /// 동일 ArmX 위치에서 한 picker 는 Bottom 카메라에, 다른 picker (idx-2) 는
        /// Side 카메라에 정렬되어 동시 촬영이 가능하다.
        /// </summary>
        /// <param name="dieSizeXMm">Die X 치수 [mm] — 90° 회전 후 Side Y 보정.</param>
        /// <param name="dieSizeYMm">Die Y 치수 [mm].</param>
        /// <returns>
        /// (BottomVisionOffset[], SideVisionResult[]) tuple. 실패 시 null.
        /// </returns>
        public async Task<Tuple<BottomVisionOffset[], SideVisionResult[]>>
            InspectBottomAndSideAsync(double dieSizeXMm = 0.0, double dieSizeYMm = 0.0)
        {
            int N = Pickers.Length;
            BottomVisionOffset[] bottomResults = null;
            SideVisionResult[]   sideResults   = new SideVisionResult[N];
            double rotateYDelta = dieSizeXMm - dieSizeYMm;
            int totalSteps = N + 2; // 6 for N=4

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> Bottom+Side 병렬 파이프라인 시작 (steps=" +
                totalSteps + ").");

            // 모든 picker Z 를 우선 WaitPosition 으로 확보 (안전 시작점).
            // 이후 각 picker 의 Bottom 시작 직전에만 Z 다운.
            for (int i = 0; i < N; i++)
            {
                PickerComponent p = Pickers[i];
                await p.PickerZ.MoveAbsoluteAsync(p.Setup.WaitPosition, p.Recipe.ZVelocity);
            }

            for (int step = 0; step < totalSteps; step++)
            {
                int bottomIdx = (step < N) ? step : -1;
                int sideIdx   = (step >= 2 && step < N + 2) ? step - 2 : -1;

                // ── 타겟 ArmX 계산 ───────────────────────────────────────────
                // bottomIdx valid: picker[bottomIdx] @ ArmInspectionPositionX
                //   ArmX = ArmInspectionPositionX + bottomIdx * PickerPitchX
                // else (sideIdx only): picker[sideIdx] @ SideVision1X
                //   ArmX = SideVision1X + sideIdx * PickerPitchX
                double targetArmX;
                if (bottomIdx >= 0)
                    targetArmX = Setup.ArmInspectionPositionX + bottomIdx * Setup.PickerPitchX;
                else
                    targetArmX = Setup.SideVision1X + sideIdx * Setup.PickerPitchX;

                // ArmY: Bottom picker 가 있으면 그 picker 의 ColletOffsetY, 없으면 0.
                double targetArmY = (bottomIdx >= 0)
                    ? Pickers[bottomIdx].Setup.ColletOffsetY
                    : 0.0;

                // ── 이동 단계: ArmX/ArmY + (Bottom picker Z↓) 동시 ───────────
                var moveTasks = new List<Task>();
                moveTasks.Add(ArmX.MoveAbsoluteAsync(targetArmX, Recipe.ArmXVelocity));
                moveTasks.Add(ArmY.MoveAbsoluteAsync(targetArmY, Recipe.ArmYVelocity));
                if (bottomIdx >= 0)
                {
                    PickerComponent bp = Pickers[bottomIdx];
                    moveTasks.Add(bp.PickerZ.MoveAbsoluteAsync(
                        bp.Setup.FocusPosition, bp.Recipe.ZVelocity));
                }
                await Task.WhenAll(moveTasks);

                if (ArmX.IsAlarm || ArmY.IsAlarm ||
                    (bottomIdx >= 0 && Pickers[bottomIdx].PickerZ.IsAlarm))
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> Step " + step + " 이동 실패.");
                    return null;
                }

                // ── 동시 실행: Bottom Expose ∥ Side sub-sequence ──────────────
                Task<bool> bottomTask = null;
                Task       sideTask   = null;

                if (bottomIdx >= 0)
                {
                    int pNo = bottomIdx + 1;
                    Console.WriteLine(
                        "[INFO]  '" + Name + "' -> Step " + step +
                        " Bottom Trigger Picker" + pNo + ".");
                    SimulatorBridge.Instance?.CameraExposeFlash("BOTTOM");
                    bottomTask = Vision.TriggerBottomExposeAsync(
                        pNo, Config.VisionExposeTimeoutMs);
                }

                if (sideIdx >= 0)
                {
                    Console.WriteLine(
                        "[INFO]  '" + Name + "' -> Step " + step +
                        " Side sub-sequence Picker" + (sideIdx + 1) + " 시작.");
                    sideTask = RunSideSubSequenceAsync(
                        sideIdx, bottomResults, sideResults, rotateYDelta);
                }

                // 둘 다 끝날 때까지 대기
                if (bottomTask != null && sideTask != null)
                    await Task.WhenAll(bottomTask, sideTask);
                else if (bottomTask != null)
                    await bottomTask;
                else if (sideTask != null)
                    await sideTask;

                // Bottom Expose 결과 검증
                if (bottomTask != null && !bottomTask.Result)
                {
                    int pNo = bottomIdx + 1;
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> Bottom Expose End 실패 (Picker" +
                        pNo + ").");
                    QMC.Common.Alarms.AlarmManager.Raise(
                        QMC.Common.Alarms.AlarmSeverity.Error,
                        "EXPOSE-TIMEOUT", Name,
                        "Bottom Vision Expose End 수신 실패 (Picker" + pNo + ")");
                    return null;
                }

                // ── 마지막 Bottom Expose 직후: 전체 Bottom 결과 일괄 수신 ──
                if (bottomIdx == N - 1)
                {
                    Console.WriteLine(
                        "[INFO]  '" + Name +
                        "' -> 4개 picker Bottom Expose 완료 — GetBottomResultsAsync 호출.");
                    bottomResults = await Vision.GetBottomResultsAsync(
                        Config.VisionResultTimeoutMs);
                    if (bottomResults == null)
                    {
                        Console.WriteLine(
                            "[ALARM] '" + Name + "' -> Bottom 결과 수신 실패.");
                        return null;
                    }
                    Console.WriteLine(
                        "[INFO]  '" + Name + "' -> Bottom 결과 수신 (n=" +
                        bottomResults.Length + ").");
                }
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> Bottom+Side 병렬 파이프라인 완료.");
            return Tuple.Create(bottomResults, sideResults);
        }

        /// <summary>
        /// 단일 picker 의 Side 4면 sub-sequence 를 실행한다.<br/>
        /// 시퀀스: Side1 → Side2 → +90°+Y move → Side1 → Side2 → -90°+Z up → Get result.
        /// </summary>
        private async Task RunSideSubSequenceAsync(
            int idx,
            BottomVisionOffset[] bottomOffsets,
            SideVisionResult[]   outResults,
            double rotateYDelta)
        {
            PickerComponent picker = Pickers[idx];
            int pNo = idx + 1;

            // Bottom Offset Y 적용 (bottom 결과가 아직 없으면 0).
            double offY = 0.0;
            if (bottomOffsets != null && idx < bottomOffsets.Length &&
                bottomOffsets[idx] != null)
            {
                offY = bottomOffsets[idx].OffsetY;
            }

            // SideVisionY 베이스 위치 (회전 전).
            if (SideVisionY != null)
            {
                await SideVisionY.MoveAbsoluteAsync(
                    Setup.SideVisionY0 + offY, 80.0);
            }

            // 1) Side1 Expose
            SimulatorBridge.Instance?.CameraExposeFlash("SIDE1");
            bool ok = await Vision.TriggerSideExposeAsync(
                pNo, 1, Config.VisionExposeTimeoutMs);
            if (!ok)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> Side1 Expose 실패 (Picker" + pNo + ").");
                return;
            }

            // 2) Side2 Expose
            SimulatorBridge.Instance?.CameraExposeFlash("SIDE2");
            ok = await Vision.TriggerSideExposeAsync(
                pNo, 2, Config.VisionExposeTimeoutMs);
            if (!ok)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> Side2 Expose 실패 (Picker" + pNo + ").");
                return;
            }

            // 3) PickerT +90° ∥ SideVisionY → (base + offY + rotateYDelta)
            Task rotTask = picker.PickerT.MoveRelativeAsync(
                90.0, picker.Recipe.ThetaVelocity);
            Task yTask = (SideVisionY != null)
                ? SideVisionY.MoveAbsoluteAsync(
                    Setup.SideVisionY0 + offY + rotateYDelta, 80.0)
                : Task.CompletedTask;
            await Task.WhenAll(rotTask, yTask);

            // 4) Side1 Expose (회전 후 = Side3)
            SimulatorBridge.Instance?.CameraExposeFlash("SIDE1");
            ok = await Vision.TriggerSideExposeAsync(
                pNo, 3, Config.VisionExposeTimeoutMs);
            if (!ok)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> Side3 Expose 실패 (Picker" + pNo + ").");
                return;
            }

            // 5) Side2 Expose (회전 후 = Side4)
            SimulatorBridge.Instance?.CameraExposeFlash("SIDE2");
            ok = await Vision.TriggerSideExposeAsync(
                pNo, 4, Config.VisionExposeTimeoutMs);
            if (!ok)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> Side4 Expose 실패 (Picker" + pNo + ").");
                return;
            }

            // 6) PickerT -90° ∥ PickerZ → WaitPosition
            Task rotBack = picker.PickerT.MoveRelativeAsync(
                -90.0, picker.Recipe.ThetaVelocity);
            Task zUp = picker.PickerZ.MoveAbsoluteAsync(
                picker.Setup.WaitPosition, picker.Recipe.ZVelocity);
            await Task.WhenAll(rotBack, zUp);

            // 7) Side 검사 결과 수신
            SideVisionResult sr = await Vision.GetSideResultAsync(
                pNo, Config.VisionResultTimeoutMs);
            if (sr == null)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> Side 결과 수신 실패 (Picker" + pNo + ").");
                return;
            }
            outResults[idx] = sr;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> Picker" + pNo +
                " Side sub-sequence 완료. 결과=" + (sr.IsAllOk ? "ALL OK" : "NG"));
        }

        // ======================================================================
        //  B-6. Step 3: Place 이동 및 다이 배출
        // ======================================================================

        /// <summary>
        /// OutputStage의 Place 위치로 ArmX·ArmY를 이동한 뒤,
        /// 4개 피커를 순서대로 하강·Blow 파기·상승하여 다이를 배출한다.
        /// <para>
        /// 피커 1개 기준 시퀀스:<br/>
        /// 1. ArmX·ArmY → Place 위치 동시 이동<br/>
        /// 2. PickerZ 하강 (PlacePosition)<br/>
        /// 3. VacuumOut Off → BlowOut On (BlowPulseMs) → BlowOut Off<br/>
        /// 4. PickerZ 상승 (WaitPosition)
        /// </para>
        /// </summary>
        /// <returns>모든 피커 배출 성공 시 true, 중간 실패 시 false.</returns>
        public async Task<bool> PlaceDiesAsync()
        {
            Console.WriteLine("[INFO]  '" + Name + "' -> Place 위치로 이동 시작.");

            // ArmX·ArmY를 Place 위치로 동시 이동
            await Task.WhenAll(
                ArmX.MoveAbsoluteAsync(Setup.PlacePositionX, Recipe.ArmXVelocity),
                ArmY.MoveAbsoluteAsync(Setup.PlacePositionY, Recipe.ArmYVelocity));

            if (ArmX.IsAlarm || ArmY.IsAlarm)
            {
                Console.WriteLine("[ALARM] '" + Name + "' -> PlaceDies: Place 위치 이동 실패.");
                return false;
            }

            Console.WriteLine("[INFO]  '" + Name + "' -> Place 위치 도달. 피커 배출 시작.");

            // 피커 1~4번 순서대로 배출
            for (int i = 0; i < Pickers.Length; i++)
            {
                PickerComponent picker   = Pickers[i];
                int             pickerNo = i + 1;

                Console.WriteLine("[INFO]  '" + Name + "' -> Picker" + pickerNo + " 배출 시작.");

                bool placeOk = await picker.PlaceAsync();
                if (!placeOk)
                {
                    Console.WriteLine(
                        "[ALARM] '" + Name + "' -> PlaceDies: 배출 실패 (Picker" + pickerNo + ").");
                    return false;
                }

                Console.WriteLine("[INFO]  '" + Name + "' -> Picker" + pickerNo + " 배출 완료.");
            }

            Console.WriteLine("[INFO]  '" + Name + "' -> 4개 피커 배출 완료.");
            return true;
        }
    }

    // ==========================================================================
    //  C. TransferPickerUnit - 최상위 메인 유닛
    // ==========================================================================

    /// <summary>
    /// Transfer Picker 최상위 유닛.<br/>
    /// 좌·우 미러 형태의 <see cref="TpuArmUnit"/> 2개를 소유하며,
    /// 각 Arm이 독립적으로 Bottom Vision → Side Vision → Place 시퀀스를 실행한다.
    /// <para>
    /// 계층 구조:<br/>
    /// <c>TransferPickerUnit</c><br/>
    /// ├─ <c>LeftArm  (TpuArmUnit)</c> → <c>PickerComponent × 4</c><br/>
    /// └─ <c>RightArm (TpuArmUnit)</c> → <c>PickerComponent × 4</c>
    /// </para>
    /// </summary>
    public class TransferPickerUnit : BaseUnit<TpuSetup, TpuConfig, TpuRecipe>
    {
        // ----------------------------------------------------------------------
        //  C-1. 하위 유닛 선언
        // ----------------------------------------------------------------------

        /// <summary>
        /// 좌측 암 유닛.<br/>
        /// InputStage → Vision → OutputStage 방향의 주 이송 암.
        /// </summary>
        public TpuArmUnit LeftArm { get; private set; }

        /// <summary>
        /// 우측 암 유닛.<br/>
        /// LeftArm과 미러 대칭 구조로 배치되어 교번 이송으로 스루풋을 2배로 높인다.
        /// </summary>
        public TpuArmUnit RightArm { get; private set; }

        // ----------------------------------------------------------------------
        //  C-2. 생성자
        // ----------------------------------------------------------------------

        /// <summary>
        /// <see cref="TransferPickerUnit"/>을 초기화하고 좌·우
        /// <see cref="TpuArmUnit"/>을 생성하여 Composite 트리에 등록한다.
        /// </summary>
        /// <param name="vision">좌·우 Arm이 공유하는 비전 TCP/IP 클라이언트</param>
        public TransferPickerUnit(IVisionTpuClient vision) : base("TransferPickerUnit")
        {
            if (vision == null) throw new ArgumentNullException("vision");

            LeftArm  = new TpuArmUnit("LeftArm",  vision);
            RightArm = new TpuArmUnit("RightArm", vision);

            // BaseUnit.Components에 TpuArmUnit을 등록하면 Save() 등 공통 동작이
            // 전체 하위 트리(PickerComponent, Axis, DO 포함)에 연쇄 전파된다.
            Components.Add(LeftArm);
            Components.Add(RightArm);
        }
    }
}
