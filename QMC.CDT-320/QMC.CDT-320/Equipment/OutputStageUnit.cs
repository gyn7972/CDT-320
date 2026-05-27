using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;
using QMC.Common.Alarms;

namespace QMC.CDT320
{
    // ==========================================================================
    //  OutputStageUnit 전용 외부 연동 인터페이스
    // ==========================================================================

    /// <summary>
    /// 다이 분류 결과를 식별하는 열거형.
    /// </summary>
    public enum DieGrade
    {
        /// <summary>양품 다이. GoodStage로 이송된다.</summary>
        Good,

        /// <summary>불량 다이. NgStage로 이송된다.</summary>
        Ng
    }

    /// <summary>
    /// OutputStageUnit으로 전달되는 다이 1개의 수신 요청 파라미터.<br/>
    /// TPU 피커 기구 오프셋과 Bottom Vision 보정값을 합산하여 최종 StageY 좌표를 계산한다.
    /// </summary>
    public class ReceiveDieRequest
    {
        /// <summary>이 다이를 배출할 타겟 스테이지 등급.</summary>
        public DieGrade Grade { get; set; }

        /// <summary>
        /// TPU 피커 기구 오프셋 X [mm].<br/>
        /// 픽커 노즐의 기계적 편심량으로, 캘리브레이션으로 측정된 고정값.
        /// </summary>
        public double TpuOffsetX { get; set; }

        /// <summary>
        /// TPU 피커 기구 오프셋 Y [mm].<br/>
        /// StageY 최종 이동 위치에 가산된다.
        /// </summary>
        public double TpuOffsetY { get; set; }

        /// <summary>
        /// Bottom Vision 보정 오프셋 X [mm].<br/>
        /// 비전 촬상으로 계산된 다이 중심의 X 편차.
        /// </summary>
        public double VisionOffsetX { get; set; }

        /// <summary>
        /// Bottom Vision 보정 오프셋 Y [mm].<br/>
        /// StageY 최종 이동 위치에 가산된다.
        /// </summary>
        public double VisionOffsetY { get; set; }
    }

    // --------------------------------------------------------------------------

    /// <summary>
    /// OutputStageUnit과 연동하는 TPU(Transfer Picker Unit) 인터페이스.<br/>
    /// OutputStageUnit은 이 계약을 통해 TPU에 "Place 준비 완료" 및
    /// "다음 다이 수신 가능" 상태를 알린다.
    /// </summary>
    public interface ITpuUnit
    {
        /// <summary>
        /// OutputStage가 TPU Place 위치로 이동 완료했음을 알린다.<br/>
        /// TPU는 이 신호를 받은 뒤 PickerZ를 하강시켜 다이를 Place한다.
        /// </summary>
        void NotifyPlaceReady();

        /// <summary>
        /// TPU가 Place를 완료하고 픽커를 상승·후퇴할 때까지 비동기 대기한다.<br/>
        /// 이 메서드가 반환된 뒤 BinCamera 검사를 수행해도 안전하다.
        /// </summary>
        /// <param name="timeoutMs">대기 타임아웃 [ms]</param>
        /// <returns>TPU 후퇴 완료 시 true, 타임아웃 시 false.</returns>
        Task<bool> WaitPlaceDoneAsync(int timeoutMs = 3000);

        /// <summary>
        /// OutputStageUnit이 다음 다이를 수신할 준비가 됐음을 TPU에 알린다.<br/>
        /// TPU는 이 신호를 받은 뒤 다음 피커의 Place 시퀀스를 시작한다.
        /// </summary>
        void NotifyReadyForNextDie();

        /// <summary>
        /// TPU에 콜렛 크리닝(Collet Cleaning) 동작을 요청하고 완료될 때까지 대기한다.<br/>
        /// NgStage의 더미 영역에서 실행되며, 크리닝 완료 시 반환된다.
        /// </summary>
        /// <param name="timeoutMs">크리닝 완료 대기 타임아웃 [ms]</param>
        /// <returns>크리닝 완료 시 true, 타임아웃 시 false.</returns>
        Task<bool> RequestColletCleaningAsync(int timeoutMs = 10000);
    }

    /// <summary>
    /// OutputStageUnit과 연동하는 OutputUnloaderUnit 인터페이스.<br/>
    /// 빈(Bin)이 가득 찼을 때 웨이퍼 교체(Wafer Change)를 요청한다.
    /// </summary>
    public interface IOutputUnloaderUnit
    {
        /// <summary>
        /// 지정 등급의 빈(Bin)이 가득 찼음을 Unloader에 알리고
        /// 웨이퍼 교체가 완료될 때까지 비동기 대기(Suspend)한다.<br/>
        /// Unloader가 교체를 완료하면 반환된다.
        /// </summary>
        /// <param name="grade">교체 요청 대상 등급 (Good / NG)</param>
        /// <param name="timeoutMs">교체 완료 대기 타임아웃 [ms]. 0 = 무한 대기.</param>
        /// <returns>교체 완료 시 true, 타임아웃 시 false.</returns>
        Task<bool> RequestWaferChangeAsync(DieGrade grade, int timeoutMs = 0);
    }

    // ==========================================================================
    //  OutputStageUnit 전용 데이터 클래스
    // ==========================================================================

    /// <summary>
    /// StageModule의 기구적 설정값.<br/>
    /// 작업 높이, 충돌 회피 높이, 더미(크리닝) 위치 등 하드웨어 교체 전까지 유지되는 값.
    /// </summary>
    public class StageModuleSetup : ISetupData
    {
        /// <summary>
        /// StageZ 작업 높이 [mm].<br/>
        /// TPU 픽커가 다이를 Place하는 동안 스테이지가 유지해야 하는 Z 위치.
        /// </summary>
        public double WorkPositionZ   { get; set; } = 10.0;

        /// <summary>
        /// StageZ 충돌 회피 하강 위치 [mm].<br/>
        /// 반대쪽 스테이지가 X 공유 구간을 통과할 때 반드시 이 위치 이하로 하강해야 한다.
        /// </summary>
        public double AvoidPositionZ  { get; set; } = 0.0;

        /// <summary>
        /// StageY 웨이퍼 교체(Unload) 위치 [mm].<br/>
        /// Unloader가 빈 웨이퍼를 꺼내는 위치.
        /// </summary>
        public double UnloadPositionY { get; set; } = -50.0;

        /// <summary>
        /// StageY 대기 위치 [mm].<br/>
        /// 작업이 없을 때 스테이지가 머무는 홈 위치.
        /// </summary>
        public double HomePositionY   { get; set; } = 0.0;

        /// <summary>
        /// StageY 콜렛 크리닝용 더미 영역 위치 [mm].<br/>
        /// NG 웨이퍼의 사용하지 않는 외곽 좌표로, TPU 콜렛을 닦는 용도.
        /// </summary>
        public double CleaningPositionY { get; set; } = 80.0;

        /// <summary>
        /// StageZ 위치 비교 시 허용 오차 [mm].<br/>
        /// ActualPosition이 AvoidPositionZ ± Tolerance 범위 내에 있으면 회피 완료로 판정.
        /// </summary>
        public double PositionTolerance { get; set; } = 0.05;
    }

    /// <summary>StageModule 고정 사양 파라미터.</summary>
    public class StageModuleConfig : IConfigData
    {
        /// <summary>시뮬레이션 모드 여부.</summary>
        public bool IsSimulationMode { get; set; } = true;

        /// <summary>StageZ 회피 완료 확인 폴링 간격 [ms].</summary>
        public int AvoidCheckIntervalMs { get; set; } = 10;

        /// <summary>StageZ 회피 완료 최대 대기 시간 [ms].</summary>
        public int AvoidTimeoutMs { get; set; } = 3000;
    }

    /// <summary>StageModule 공정별 작업 파라미터.</summary>
    public class StageModuleRecipe : IRecipeData
    {
        /// <summary>StageY 이동 속도 [mm/s].</summary>
        public double YVelocity { get; set; } = 100.0;

        /// <summary>StageZ 이동 속도 [mm/s].</summary>
        public double ZVelocity { get; set; } = 50.0;
    }

    // --------------------------------------------------------------------------

    /// <summary>OutputStageUnit 기구적 설정값.</summary>
    public class OutputStageSetup : ISetupData
    {
        /// <summary>
        /// BinCameraX 검사 진입 위치 [mm].<br/>
        /// 스테이지 작업 X 좌표 위에서 안착 상태를 검사하는 위치.
        /// </summary>
        public double BinCameraWorkPositionX { get; set; } = 150.0;

        /// <summary>
        /// BinCameraX 대기(회피) 위치 [mm].<br/>
        /// 스테이지 이동 및 TPU 진입 시 충돌을 피해 후퇴하는 위치.
        /// </summary>
        public double BinCameraRetractPositionX { get; set; } = 0.0;

        /// <summary>
        /// StageY 기준 위치 [mm].<br/>
        /// TPU가 Place할 때 스테이지의 기본 Y 위치. Offset이 이 값에 가산된다.
        /// </summary>
        public double StageBasePositionY { get; set; } = 50.0;
    }

    /// <summary>OutputStageUnit 고정 사양 파라미터.</summary>
    public class OutputStageConfig : IConfigData
    {
        /// <summary>시뮬레이션 모드 여부.</summary>
        public bool IsSimulationMode { get; set; } = true;

        /// <summary>TPU Place 완료 대기 타임아웃 [ms].</summary>
        public int TpuPlaceDoneTimeoutMs { get; set; } = 3000;

        /// <summary>웨이퍼 교체 완료 대기 타임아웃 [ms]. 0 = 무한 대기.</summary>
        public int WaferChangeTimeoutMs { get; set; } = 0;

        /// <summary>콜렛 크리닝 완료 대기 타임아웃 [ms].</summary>
        public int ColletCleaningTimeoutMs { get; set; } = 10000;
    }

    /// <summary>OutputStageUnit 공정별 작업 파라미터.</summary>
    public class OutputStageRecipe : IRecipeData
    {
        /// <summary>BinCameraX 이동 속도 [mm/s].</summary>
        public double BinCameraVelocity { get; set; } = 200.0;
    }

    // ==========================================================================
    //  A. StageModule - 개별 스테이지 제어 서브 모듈 (Good / NG 각 1개)
    // ==========================================================================

    /// <summary>
    /// 개별 출력 스테이지(Good 또는 NG) 제어 서브 모듈.<br/>
    /// StageY(전후 이동)와 StageZ(상하 이동)를 보유하며, Z축으로
    /// 충돌 회피 및 작업 높이 조절을 담당한다.
    /// <para>
    /// <b>충돌 회피 원칙:</b><br/>
    /// Good Stage와 NG Stage는 동일한 X 작업 좌표를 공유한다.
    /// 따라서 한쪽 스테이지가 Y 방향으로 이동하기 전에 반드시 반대쪽 스테이지의
    /// StageZ가 <see cref="StageModuleSetup.AvoidPositionZ"/> 이하에 있어야 한다.
    /// </para>
    /// <para>
    /// 계층 위치: <c>OutputStageUnit → StageModule</c>
    /// </para>
    /// </summary>
    public class StageModule : BaseUnit<StageModuleSetup, StageModuleConfig, StageModuleRecipe>
    {
        // ----------------------------------------------------------------------
        //  A-1. 하드웨어 멤버
        // ----------------------------------------------------------------------

        /// <summary>
        /// 스테이지 전후 이동 축 (Y축).<br/>
        /// 다이 수신 위치, Unload 위치, 크리닝 위치 간 이동에 사용된다.
        /// </summary>
        public BaseAxis StageY { get; private set; }

        /// <summary>
        /// 스테이지 상하 이동 축 (Z축).<br/>
        /// <see cref="StageModuleSetup.WorkPositionZ"/> : TPU Place 작업 높이.<br/>
        /// <see cref="StageModuleSetup.AvoidPositionZ"/> : 충돌 회피 하강 위치.
        /// </summary>
        public BaseAxis StageZ { get; private set; }

        // ----------------------------------------------------------------------
        //  A-2. 생성자
        // ----------------------------------------------------------------------

        /// <summary>
        /// <see cref="StageModule"/>을 초기화한다.
        /// </summary>
        /// <param name="moduleName">모듈 이름 (예: "GoodStage", "NgStage")</param>
        public StageModule(string moduleName) : base(moduleName)
        {
            StageY = AjinFactory.CreateAxis(moduleName + "_StageY");
            StageZ = AjinFactory.CreateAxis(moduleName + "_StageZ");

            // Stage 30 — StageModule SoftLimit 확장 (default 200 too strict)
            StageY.Setup.SoftLimitPlus = 350.0;
            StageZ.Setup.SoftLimitPlus = 250.0;

            // Composite 트리 등록
            Components.Add(StageY);
            Components.Add(StageZ);
        }

        // ----------------------------------------------------------------------
        //  A-3. Z축 충돌 회피 헬퍼 메서드
        // ----------------------------------------------------------------------

        /// <summary>
        /// StageZ가 현재 충돌 회피 위치(AvoidPositionZ)에 있는지 확인한다.<br/>
        /// 실제 위치(ActualPosition)가 AvoidPositionZ ± PositionTolerance 범위 내에
        /// 있으면 회피 완료로 판정한다.
        /// </summary>
        /// <returns>회피 위치에 있으면 true.</returns>
        public bool IsAtAvoidPosition()
        {
            double diff = Math.Abs(StageZ.ActualPosition - Setup.AvoidPositionZ);
            return diff <= Setup.PositionTolerance;
        }

        /// <summary>
        /// StageZ를 충돌 회피 위치(<see cref="StageModuleSetup.AvoidPositionZ"/>)로 하강시킨다.<br/>
        /// 이미 회피 위치에 있으면 이동 없이 즉시 반환한다.
        /// </summary>
        /// <returns>하강 완료 시 true, 축 알람 발생 시 false.</returns>
        public async Task<bool> MoveToAvoidPositionAsync()
        {
            // 이미 회피 위치에 있으면 불필요한 이동 생략
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
        /// StageZ를 작업 높이(<see cref="StageModuleSetup.WorkPositionZ"/>)로 상승시킨다.<br/>
        /// TPU Place 직전에 호출한다.
        /// </summary>
        /// <returns>상승 완료 시 true, 축 알람 발생 시 false.</returns>
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
        /// StageY를 지정한 절대 위치로 이동한다.
        /// </summary>
        /// <param name="targetY">목표 Y 위치 [mm]</param>
        /// <returns>이동 완료 시 true, 축 알람 발생 시 false.</returns>
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
        /// StageY를 대기(홈) 위치로 복귀시킨다.<br/>
        /// 크리닝 완료 후, 또는 Unload 완료 후 원위치 복귀에 사용한다.
        /// </summary>
        /// <returns>복귀 완료 시 true, 축 알람 발생 시 false.</returns>
        public async Task<bool> MoveToHomeAsync()
        {
            return await MoveYAsync(Setup.HomePositionY);
        }
    }

    // ==========================================================================
    //  B. OutputStageUnit - 최상위 Output Stage 메인 유닛
    // ==========================================================================

    /// <summary>
    /// 검사 완료된 다이를 양품(Good) / 불량(NG)으로 분류하여 적재하는 Output Stage 유닛.<br/>
    /// GoodStage와 NgStage 두 개의 <see cref="StageModule"/>을 보유하며,
    /// BinCameraX로 안착 상태를 검사한다.
    /// <para>
    /// <b>핵심 인터락 설계 - 충돌 회피(Collision Avoidance):</b><br/>
    /// GoodStage와 NgStage는 동일한 X 작업 좌표를 공유한다. 따라서 타겟 스테이지의
    /// StageY를 움직이기 전, 반드시 반대쪽 스테이지의 StageZ를
    /// <see cref="StageModuleSetup.AvoidPositionZ"/>로 하강시킨 뒤 이동해야 한다.
    /// </para>
    /// <para>
    /// 계층 구조:<br/>
    /// <c>OutputStageUnit</c><br/>
    /// ├─ <c>GoodStage (StageModule)</c><br/>
    /// │   ├─ <c>GoodStage_StageY (SimAxis)</c><br/>
    /// │   └─ <c>GoodStage_StageZ (SimAxis)</c><br/>
    /// ├─ <c>NgStage   (StageModule)</c><br/>
    /// │   ├─ <c>NgStage_StageY   (SimAxis)</c><br/>
    /// │   └─ <c>NgStage_StageZ   (SimAxis)</c><br/>
    /// └─ <c>BinCameraX            (SimAxis)</c>
    /// </para>
    /// </summary>
    public class OutputStageUnit : BaseUnit<OutputStageSetup, OutputStageConfig, OutputStageRecipe>
    {
        // ----------------------------------------------------------------------
        //  B-1. 하드웨어 컴포넌트
        // ----------------------------------------------------------------------

        /// <summary>
        /// 양품(Good) 다이를 적재하는 스테이지 모듈.
        /// </summary>
        public StageModule GoodStage { get; private set; }

        /// <summary>
        /// 불량(NG) 다이를 적재하고 콜렛 크리닝 더미 영역을 제공하는 스테이지 모듈.
        /// </summary>
        public StageModule NgStage { get; private set; }

        /// <summary>
        /// Bin(안착) 위치를 검사하는 카메라의 X 이동 축.<br/>
        /// Place 완료 후 작업 위치로 진입하여 안착 여부를 촬상하고, 즉시 후퇴한다.
        /// </summary>
        public BaseAxis BinCameraX { get; private set; }

        // ----------------------------------------------------------------------
        //  B-2. 외부 연동 인터페이스
        // ----------------------------------------------------------------------

        /// <summary>
        /// TPU 연동 인터페이스.<br/>
        /// "Place 준비 완료" 신호 전송 및 "Place 완료 / 다음 수신 가능" 상태 통보에 사용.
        /// </summary>
        public ITpuUnit Tpu { get; private set; }

        /// <summary>
        /// OutputUnloader 연동 인터페이스.<br/>
        /// 빈(Bin)이 가득 찼을 때 웨이퍼 교체 요청에 사용.
        /// </summary>
        public IOutputUnloaderUnit Unloader { get; private set; }

        // ----------------------------------------------------------------------
        //  B-3. 생성자
        // ----------------------------------------------------------------------

        /// <summary>
        /// <see cref="OutputStageUnit"/>을 초기화하고 하위 모듈을 Composite 트리에 등록한다.
        /// </summary>
        /// <param name="tpu">TPU 연동 인터페이스</param>
        /// <param name="unloader">OutputUnloader 연동 인터페이스</param>
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
            // Stage 30 — BinCameraX SoftLimit 확장
            BinCameraX.Setup.SoftLimitPlus = 350.0;

            // Composite 트리 등록 - Save() 등 공통 동작이 하위 트리 전체에 전파됨
            Components.Add(GoodStage);
            Components.Add(NgStage);
            Components.Add(BinCameraX);
        }

        // ======================================================================
        //  B-4. 내부 인터락 헬퍼 메서드
        // ======================================================================

        /// <summary>
        /// 지정한 타겟 스테이지의 반대쪽 스테이지를 반환한다.
        /// </summary>
        /// <param name="target">타겟 스테이지 모듈</param>
        /// <returns>반대쪽 스테이지 모듈.</returns>
        private StageModule GetOppositeStage(StageModule target)
        {
            return ReferenceEquals(target, GoodStage) ? NgStage : GoodStage;
        }

        /// <summary>
        /// <b>[충돌 회피 인터락 핵심 메서드]</b><br/>
        /// 타겟 스테이지의 StageY를 이동하기 전에 반대쪽 스테이지의 StageZ가
        /// <see cref="StageModuleSetup.AvoidPositionZ"/>에 있는지 확인하고,
        /// 아니라면 먼저 하강시킨다.
        /// <para>
        /// 인터락 시나리오:<br/>
        /// 예) Good Stage를 작업 위치로 보낼 때, NG Stage가 아직 WorkPositionZ에 있다면
        /// NG Stage를 AvoidPositionZ로 먼저 내린 후 Good Stage Y를 이동한다.
        /// </para>
        /// </summary>
        /// <param name="targetStage">Y축을 이동시킬 타겟 스테이지</param>
        /// <returns>인터락 처리 완료(회피 확인 및 필요 시 하강) 시 true, 알람 시 false.</returns>
        private async Task<bool> EnsureOppositeStageAvoidedAsync(StageModule targetStage)
        {
            StageModule opposite = GetOppositeStage(targetStage);

            // 반대쪽 스테이지가 이미 회피 위치에 있으면 즉시 반환
            if (opposite.IsAtAvoidPosition())
            {
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Interlock: '" + opposite.Name +
                    "' 이미 회피 위치. 추가 동작 불필요.");
                return true;
            }

            // ── 충돌 경고 및 강제 회피 하강 ─────────────────────────────────
            // 반대쪽 스테이지가 WorkPositionZ 부근에 있는 상태에서 타겟 스테이지의
            // StageY를 이동하면 X 공유 구간에서 두 스테이지가 충돌한다.
            // 반드시 반대쪽을 AvoidPositionZ로 하강시킨 후 이동해야 한다.
            Console.WriteLine(
                "[WARN]  '" + Name + "' -> Interlock: '" + opposite.Name +
                "' Z=" + opposite.StageZ.ActualPosition.ToString("F3") +
                "mm, 회피 위치 아님. 강제 하강 시작.");

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
                "' 회피 완료. '" + targetStage.Name + "' StageY 이동 허가.");
            return true;
        }

        // ======================================================================
        //  B-5. Step 1: 다이 수신 및 충돌 회피 이동
        // ======================================================================

        /// <summary>
        /// TPU로부터 다이를 수신하기 위해 타겟 스테이지를 Place 위치로 이동한다.
        /// <para>
        /// <b>실행 순서:</b><br/>
        /// 1. [충돌 회피 인터락] 반대쪽 스테이지 StageZ → AvoidPositionZ 하강 확인.<br/>
        /// 2. 타겟 스테이지 StageZ → WorkPositionZ 상승.<br/>
        /// 3. TpuOffset + VisionOffset을 합산한 최종 Y 위치로 StageY 이동.<br/>
        /// 4. 이동 완료 후 TPU에 "Place 준비 완료" 신호 전송.
        /// </para>
        /// </summary>
        /// <param name="request">다이 수신 요청 파라미터 (등급, 오프셋 등)</param>
        /// <returns>이동 및 신호 전송 완료 시 true, 인터락/알람 발생 시 false.</returns>
        public async Task<bool> ReceiveDieAsync(ReceiveDieRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            StageModule target = request.Grade == DieGrade.Good ? GoodStage : NgStage;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> ReceiveDie: Grade=" + request.Grade +
                ", TpuOffsetY=" + request.TpuOffsetY.ToString("F3") +
                ", VisionOffsetY=" + request.VisionOffsetY.ToString("F3"));

            // ── Step 1-1. [충돌 회피 인터락] ─────────────────────────────────
            // 타겟 스테이지 StageY를 이동하기 전, 반대쪽 스테이지 StageZ가
            // AvoidPositionZ에 있는지 확인하고 필요하면 강제 하강한다.
            bool interlockOk = await EnsureOppositeStageAvoidedAsync(target);
            if (!interlockOk)
                return false;

            // ── Step 1-2. 타겟 StageZ → WorkPositionZ 상승 ──────────────────
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' StageZ 작업 위치로 상승 중...");

            bool workZOk = await target.MoveToWorkPositionAsync();
            if (!workZOk)
                return false;

            // ── Step 1-3. 오프셋 합산 후 StageY 이동 ────────────────────────
            // 최종 Y = 기준 위치 + TPU 피커 기구 오프셋 + Bottom Vision 보정 오프셋
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

            // ── Step 1-4. TPU에 Place 준비 완료 신호 전송 ────────────────────
            // TPU는 이 신호를 받은 뒤 PickerZ를 하강시켜 다이를 Place한다.
            Tpu.NotifyPlaceReady();
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> TPU에 Place 준비 완료 신호 전송 완료.");

            return true;
        }

        // ======================================================================
        //  B-6. Step 2: Bin 비전 검사 (Place 완료 후 안착 상태 확인)
        // ======================================================================

        /// <summary>
        /// TPU가 다이를 내려놓고 후퇴한 뒤 BinCamera로 안착 상태를 검사한다.
        /// <para>
        /// <b>실행 순서:</b><br/>
        /// 1. TPU Place 완료 + 픽커 후퇴까지 대기(<see cref="ITpuUnit.WaitPlaceDoneAsync"/>).<br/>
        /// 2. BinCameraX → 검사 위치(WorkPositionX)로 진입.<br/>
        /// 3. 비전 촬상 및 안착 검사 수행(시뮬레이션: 항상 OK).<br/>
        /// 4. BinCameraX → 대기(Retract) 위치로 즉시 후진.<br/>
        /// 5. TPU에 "다음 다이 수신 가능" 상태 통보.
        /// </para>
        /// </summary>
        /// <returns>검사 완료 시 true, TPU 대기 타임아웃 또는 알람 시 false.</returns>
        public async Task<bool> InspectBinPositionAsync()
        {
            Console.WriteLine("[INFO]  '" + Name + "' -> Bin 검사 시작. TPU Place 완료 대기 중...");

            // ── Step 2-1. TPU Place 완료 + 픽커 후퇴 대기 ─────────────────────
            // TPU 픽커가 완전히 후퇴한 뒤에야 BinCamera를 진입시켜 충돌을 방지한다.
            bool placeDone = await Tpu.WaitPlaceDoneAsync(Config.TpuPlaceDoneTimeoutMs);
            if (!placeDone)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> InspectBin: TPU Place 완료 대기 타임아웃.");
                AlarmManager.Raise(
                    AlarmSeverity.Warning,
                    "OS-PLACEDONE",
                    source: Name + ".InspectBinPositionAsync",
                    message: "TPU Place 완료 대기 타임아웃 (timeout=" +
                             Config.TpuPlaceDoneTimeoutMs + "ms)");
                return false;
            }

            Console.WriteLine("[INFO]  '" + Name + "' -> TPU 후퇴 확인. BinCamera 진입 중...");

            // ── Step 2-2. BinCameraX → 검사 위치 진입 ────────────────────────
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

            // ── Step 2-3. 비전 촬상 및 안착 검사 ─────────────────────────────
            // [실제 구현 시] 여기서 비전 TCP 클라이언트로 Trigger를 보내고 결과를 수신한다.
            // 시뮬레이션에서는 즉시 OK로 처리한다.
            Console.WriteLine("[INFO]  '" + Name + "' -> BinCamera 안착 검사 수행 중...");
            SimulatorBridge.Instance?.CameraExposeFlash("BIN");
            await Task.Delay(20).ContinueWith(_ => { }); // 촬상 소요 시간 시뮬레이션

            Console.WriteLine("[INFO]  '" + Name + "' -> BinCamera 검사 완료. 즉시 후퇴.");

            // ── Step 2-4. BinCameraX → 대기(Retract) 위치 즉시 후진 ──────────
            // 다음 TPU 진입 및 스테이지 이동과의 간섭을 최소화하기 위해 즉시 후퇴한다.
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

            // ── Step 2-5. TPU에 다음 다이 수신 가능 통보 ─────────────────────
            // 이 신호 이후 TPU는 다음 피커의 Place 시퀀스를 시작할 수 있다.
            Tpu.NotifyReadyForNextDie();
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> TPU에 '다음 다이 수신 가능' 통보 완료.");

            return true;
        }

        // ======================================================================
        //  B-7. Step 3: 웨이퍼 교체 대기 (Bin Full)
        // ======================================================================

        /// <summary>
        /// 지정 등급의 빈(Bin)이 가득 찼을 때 스테이지를 교체 위치로 이동하고
        /// Unloader에 웨이퍼 교체를 요청한 뒤 교체 완료까지 Suspend한다.
        /// <para>
        /// <b>실행 순서:</b><br/>
        /// 1. [충돌 회피 인터락] 반대쪽 스테이지 StageZ 회피 확인.<br/>
        /// 2. 타겟 스테이지 StageZ → AvoidPositionZ 하강(이동 중 안전 높이 확보).<br/>
        /// 3. 타겟 스테이지 StageY → UnloadPositionY 이동.<br/>
        /// 4. Unloader에 체인지 요청 전송 → 교체 완료까지 비동기 대기(Suspend).
        /// </para>
        /// </summary>
        /// <param name="grade">가득 찬 빈의 등급 (Good / NG)</param>
        /// <returns>교체 완료 시 true, 인터락/알람/타임아웃 시 false.</returns>
        public async Task<bool> RequestWaferChangeAsync(DieGrade grade)
        {
            StageModule target = grade == DieGrade.Good ? GoodStage : NgStage;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> WaferChange: Grade=" + grade +
                " Bin Full. 교체 위치 이동 시작.");

            // ── Step 3-1. [충돌 회피 인터락] ─────────────────────────────────
            bool interlockOk = await EnsureOppositeStageAvoidedAsync(target);
            if (!interlockOk)
                return false;

            // ── Step 3-2. 타겟 StageZ → AvoidPositionZ 하강 (이동 중 안전) ───
            // 교체 위치로 이동 시에도 StageZ를 회피 높이로 유지해야
            // 경로상 간섭 구조물과의 충돌을 방지한다.
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' StageZ 회피 위치 하강 중...");

            bool avoidOk = await target.MoveToAvoidPositionAsync();
            if (!avoidOk)
                return false;

            // ── Step 3-3. 타겟 StageY → UnloadPositionY 이동 ────────────────
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

            // ── Step 3-4. Unloader에 체인지 요청 → 교체 완료까지 Suspend ──────
            // WaferChangeTimeoutMs == 0이면 무한 대기(Unloader 작업 완료까지).
            bool changeOk = await Unloader.RequestWaferChangeAsync(grade, Config.WaferChangeTimeoutMs);

            if (!changeOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> WaferChange: Unloader 교체 타임아웃. Grade=" + grade);
                return false;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> WaferChange: 교체 완료. Grade=" + grade);
            return true;
        }

        // ======================================================================
        //  B-8. Step 4: 콜렛 크리닝 (새 Good 웨이퍼 로딩 완료 직후 1회 실행)
        // ======================================================================

        /// <summary>
        /// TPU 픽커의 콜렛(노즐)에 묻은 이물질을 제거하기 위해 크리닝을 수행한다.
        /// <para>
        /// <b>트리거 조건:</b> 새로운 Good 웨이퍼가 로딩 완료된 직후 1회 실행한다.
        /// </para>
        /// <para>
        /// <b>실행 순서:</b><br/>
        /// 1. [충돌 회피 인터락] GoodStage StageZ 회피 확인
        ///    (NgStage를 이동시키므로 반대쪽은 GoodStage).<br/>
        /// 2. NgStage StageY → CleaningPositionY(NG 웨이퍼 더미 외곽) 이동.<br/>
        /// 3. NgStage StageZ → WorkPositionZ 상승(TPU 콜렛이 더미 영역에 닿을 높이).<br/>
        /// 4. TPU에 콜렛 크리닝 요청 전송 → 완료 대기.<br/>
        /// 5. NgStage StageZ → AvoidPositionZ 하강.<br/>
        /// 6. NgStage StageY → HomePositionY 복귀(원위치).
        /// </para>
        /// </summary>
        /// <returns>크리닝 완료 시 true, 인터락/알람/타임아웃 시 false.</returns>
        public async Task<bool> PerformColletCleaningAsync()
        {
            Console.WriteLine("[INFO]  '" + Name + "' -> 콜렛 크리닝 시작. NgStage 더미 영역으로 이동.");

            // ── Step 4-1. [충돌 회피 인터락] ─────────────────────────────────
            // NgStage를 이동시키므로 반대쪽인 GoodStage의 StageZ 회피를 먼저 확인한다.
            bool interlockOk = await EnsureOppositeStageAvoidedAsync(NgStage);
            if (!interlockOk)
                return false;

            // ── Step 4-2. NgStage StageY → CleaningPositionY 이동 ────────────
            // NG 웨이퍼의 사용하지 않는 외곽(더미) 영역으로 이동한다.
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage StageY → CleaningPositionY=" +
                NgStage.Setup.CleaningPositionY.ToString("F1") + "mm 이동 중...");

            bool cleaningMoveOk = await NgStage.MoveYAsync(NgStage.Setup.CleaningPositionY);
            if (!cleaningMoveOk)
                return false;

            // ── Step 4-3. NgStage StageZ → WorkPositionZ 상승 ────────────────
            // TPU 콜렛이 더미 영역 표면에 닿을 수 있도록 작업 높이로 상승한다.
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage StageZ → WorkPositionZ 상승 중...");

            bool workZOk = await NgStage.MoveToWorkPositionAsync();
            if (!workZOk)
                return false;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> 크리닝 준비 완료. TPU에 콜렛 크리닝 요청 전송.");

            // ── Step 4-4. TPU에 콜렛 크리닝 요청 → 완료 대기 ────────────────
            // TPU는 이 요청을 받으면 픽커를 더미 영역 위에서 닦는 동작을 수행한다.
            // 크리닝이 완료될 때까지 비동기 대기한다.
            bool cleaningOk = await Tpu.RequestColletCleaningAsync(Config.ColletCleaningTimeoutMs);
            if (!cleaningOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> ColletCleaning: TPU 크리닝 타임아웃.");
                // 타임아웃이더라도 NgStage는 안전 위치로 복귀시킨다.
            }
            else
            {
                Console.WriteLine("[INFO]  '" + Name + "' -> TPU 콜렛 크리닝 완료.");
            }

            // ── Step 4-5. NgStage StageZ → AvoidPositionZ 하강 ──────────────
            // 이동 전 안전 높이 확보. 실패해도 계속 진행하여 복귀를 시도한다.
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage StageZ 회피 위치 하강 중...");

            bool avoidOk = await NgStage.MoveToAvoidPositionAsync();
            if (!avoidOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> ColletCleaning: NgStage StageZ 하강 실패.");
                return false;
            }

            // ── Step 4-6. NgStage StageY → HomePositionY 복귀 ────────────────
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage StageY 홈 위치 복귀 중...");

            bool homeOk = await NgStage.MoveToHomeAsync();
            if (!homeOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> ColletCleaning: NgStage 홈 복귀 실패.");
                return false;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> 콜렛 크리닝 시퀀스 완료. NgStage 원위치.");

            // 타임아웃이 있었다면 최종 false 반환
            return cleaningOk;
        }
    }
}
