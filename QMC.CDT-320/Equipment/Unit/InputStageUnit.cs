using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Motion.SharedRailX;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.CDT320.Materials;
using QMC.CDT320.VisionComm;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QMC.CDT320
{
    // ??????????????????????????????????????????????????????????????????????????
    //  InputStageUnit 전용 데이터 클래스
    // ??????????????????????????????????????????????????????????????????????????

    [DataContract]
    public enum InputDieVisionFailureAction
    {
        [EnumMember] SkipDie = 0,
        [EnumMember] Alarm = 1
    }

    /// <summary>
    /// InputStageUnit의 기구적 설정값.<br/>
    /// 각 축의 기준 위치 및 기구 오프셋 등 하드웨어 교체 전까지 유지되는 값을 담는다.
    /// </summary>
    public class InputStageSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; } = false;

        [DataMember] public double SafetyRadius { get; set; } = 0.0;

        [DataMember] public double WorkAreaRadius { get; set; } = 150.0;

        [DataMember] public double NeedleWorkAreaRadius { get; set; } = 125.0;

        [DataMember] public double WorkAreaCenterX { get; set; } = 0.0;

        [DataMember] public double WorkAreaCenterY { get; set; } = 0.0;

        [DataMember] public double NeedleWorkAreaCenterX { get; set; } = 0.0;

        [DataMember] public double NeedleWorkAreaCenterY { get; set; } = 0.0;

        [DataMember] public string NeedlePinCalVisionTargetId { get; set; } = VisionToolIds.Wafer.EjectPinFinder;

        [DataMember] public int NeedlePinCalVisionTimeoutMs { get; set; } = 5000;

        [DataMember] public int BarcodeReadTimeoutMs { get; set; } = 3000;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            if (WorkAreaRadius <= 0.0 && SafetyRadius > 0.0)
                WorkAreaRadius = SafetyRadius;
            if (WorkAreaRadius <= 0.0)
                WorkAreaRadius = 150.0;
            if (NeedleWorkAreaRadius <= 0.0)
                NeedleWorkAreaRadius = 125.0;
            if (string.IsNullOrWhiteSpace(NeedlePinCalVisionTargetId))
                NeedlePinCalVisionTargetId = VisionToolIds.Wafer.EjectPinFinder;
            if (NeedlePinCalVisionTimeoutMs <= 0)
                NeedlePinCalVisionTimeoutMs = 5000;
        }
    }

    /// <summary>
    /// InputStageUnit의 고정 사양 파라미터.
    /// </summary>
    public class InputStageConfig : IConfigData
    {
        [DataMember] public bool bDryRun { get; set; }

        [DataMember] public double PickUpEjectPinOffset { get; set; }

        [DataMember] public double PickUpEjectPinSpeed { get; set; } = 100.0;

        [DataMember] public double PickUpEjectPinAcc { get; set; }

        [DataMember] public double PickUpEjectPinDec { get; set; }

        [DataMember] public double PickUpNeedleSyncLiftDistance { get; set; } = 2.0;

        [DataMember] public double PickUpNeedleSyncLiftVelocity { get; set; } = 5.0;

        [DataMember] public double PickUpNeedleSyncLiftAcc { get; set; } = 100.0;

        [DataMember] public double PickUpNeedleSyncLiftDec { get; set; } = 100.0;

        [DataMember] public int PickUpNeedleSyncLiftSettleMs { get; set; }

        [DataMember] public double PickUpNeedleSeparateDistance { get; set; } = 1.0;

        [DataMember] public double PickUpNeedleSeparateSpeedPercent { get; set; } = 1.0;

        // Legacy values are kept only for reading old config files.
        [DataMember] public double PickUpNeedleSeparateVelocity { get; set; }
        [DataMember] public double PickUpNeedleSeparateAcc { get; set; } = 100.0;
        [DataMember] public double PickUpNeedleSeparateDec { get; set; } = 100.0;

        public bool IsSimulationMode
        {
            get { return bDryRun; }
            set { bDryRun = value; }
        }

        /// <summary>얼라인 반복 촬상 최대 횟수.</summary>
        [DataMember] public int MaxAlignIterations { get; set; } = 3;

        /// <summary>얼라인 수렴 임계값 [deg]. 이 값 이하이면 반복을 종료한다.</summary>
        [DataMember] public double AlignConvergenceThresholdDeg { get; set; } = 0.005;

        /// <summary>얼라인 T 보정 허용 최대값 [deg].</summary>
        [DataMember] public double AlignThetaCorrectionLimitDeg { get; set; } = 1.0;

        // To do: [얼라인 허용값 파라미터화] 시퀀스 코드 상수 3종을 Config로 이관 - UI에서 조정 가능.
        //        기본값은 현재 운용값(0.1/0.1/0.05) 기준. (기존 코드 상수는 0.05/0.05/0.01이었다)
        /// <summary>얼라인 Ref 피치 비교 허용값 [mm].</summary>
        [DataMember] public double AlignPitchCompareToleranceMm { get; set; } = 0.1;

        /// <summary>얼라인 최종 센터 오프셋 허용값 [mm]. 최종 센터 검증(IN-STAGE-ALIGN-FINAL-CENTER-TOL)에 사용한다.</summary>
        [DataMember] public double AlignCenterToleranceMm { get; set; } = 0.1;

        /// <summary>얼라인 유효 T 허용값 상한 [deg]. 요청/수렴 임계값이 커도 이 값을 넘지 않는다.</summary>
        [DataMember] public double MaxEffectiveThetaToleranceDeg { get; set; } = 0.05;

        /// <summary>수동 Die 검출로 전체 Input Die Map에 적용할 수 있는 X Offset 최대값 [mm].</summary>
        [DataMember] public double ManualDieDetectOffsetLimitX { get; set; } = 20.0;

        /// <summary>수동 Die 검출로 전체 Input Die Map에 적용할 수 있는 Y Offset 최대값 [mm].</summary>
        [DataMember] public double ManualDieDetectOffsetLimitY { get; set; } = 20.0;

        /// <summary>Align 예상 Anchor에서 Die Mapping이 허용할 X 미세 보정 최대값 [mm].</summary>
        [DataMember] public double DieMapFineOffsetLimitX { get; set; } = 2.0;

        /// <summary>Align 예상 Anchor에서 Die Mapping이 허용할 Y 미세 보정 최대값 [mm].</summary>
        [DataMember] public double DieMapFineOffsetLimitY { get; set; } = 2.0;

        /// <summary>PickUp 전 Input Die Vision 검사 재시도 횟수.</summary>
        [DataMember] public int InputDieVisionRetryCount { get; set; } = 3;

        /// <summary>PickUp 전 Input Die Vision 검사 실패 시 처리 방식.</summary>
        [DataMember] public InputDieVisionFailureAction InputDieVisionFailureAction { get; set; } = InputDieVisionFailureAction.SkipDie;

        [DataMember] public int SequenceMoveTimeoutMs { get; set; } = 10000;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsurePickUpMotionDefaults();
        }

        public void EnsurePickUpMotionDefaults()
        {
            if (AlignThetaCorrectionLimitDeg <= 0.0)
                AlignThetaCorrectionLimitDeg = 1.0;
            if (ManualDieDetectOffsetLimitX <= 0.0)
                ManualDieDetectOffsetLimitX = 20.0;
            if (ManualDieDetectOffsetLimitY <= 0.0)
                ManualDieDetectOffsetLimitY = 20.0;
            if (DieMapFineOffsetLimitX <= 0.0)
                DieMapFineOffsetLimitX = 2.0;
            if (DieMapFineOffsetLimitY <= 0.0)
                DieMapFineOffsetLimitY = 2.0;
            if (PickUpNeedleSyncLiftDistance <= 0.0)
                PickUpNeedleSyncLiftDistance = 2.0;
            if (PickUpNeedleSyncLiftVelocity <= 0.0)
                PickUpNeedleSyncLiftVelocity = 5.0;
            if (PickUpNeedleSyncLiftAcc <= 0.0)
                PickUpNeedleSyncLiftAcc = 100.0;
            if (PickUpNeedleSyncLiftDec <= 0.0)
                PickUpNeedleSyncLiftDec = 100.0;
            if (PickUpNeedleSyncLiftSettleMs < 0)
                PickUpNeedleSyncLiftSettleMs = 0;
            if (PickUpNeedleSeparateDistance <= 0.0)
                PickUpNeedleSeparateDistance = 1.0;
            if (PickUpNeedleSeparateSpeedPercent <= 0.0 && PickUpNeedleSeparateVelocity > 0.0)
                PickUpNeedleSeparateSpeedPercent = 1.0;
            if (PickUpNeedleSeparateSpeedPercent <= 0.0)
                PickUpNeedleSeparateSpeedPercent = 1.0;
            if (InputDieVisionRetryCount <= 0)
                InputDieVisionRetryCount = 3;
            // 현재 기준: 얼라인 허용값 3종은 0 이하로 저장된 경우 기본값(현재 운용값)으로 복원한다.
            if (AlignPitchCompareToleranceMm <= 0.0)
                AlignPitchCompareToleranceMm = 0.1;
            if (AlignCenterToleranceMm <= 0.0)
                AlignCenterToleranceMm = 0.1;
            if (MaxEffectiveThetaToleranceDeg <= 0.0)
                MaxEffectiveThetaToleranceDeg = 0.05;
        }
    }

    /// <summary>
    /// InputStageUnit의 공정별 작업 파라미터.
    /// </summary>
    [DataContract]
    public sealed class StageAxisPositions
    {
        [DataMember] public double AvoidPosition { get; set; }
        [DataMember] public double LoadPosition { get; set; }
        [DataMember] public double ProcessPosition { get; set; }
        [DataMember] public double UnloadPosition { get; set; }
        [DataMember] public double ReadyPosition { get; set; }
        [DataMember] public double ReticlePosition { get; set; }
        [DataMember] public double NeedlePinCalPosition { get; set; }
        [DataMember] public double[] DiePosition { get; set; } = new double[0];
    }

    [DataContract]
    public sealed class InputStageDieMapMarkPoint
    {
        [DataMember] public string Name { get; set; } = "";
        [DataMember] public bool Enabled { get; set; } = true;
        [DataMember] public double StageYPosition { get; set; }
        [DataMember] public double VisionXPosition { get; set; }
        [DataMember] public double VisionOffsetX { get; set; }
        [DataMember] public double VisionOffsetY { get; set; }
    }

    [DataContract]
    public sealed class InputStageDieMapRecipe
    {
        [DataMember] public InputStageDieMapMarkPoint Top { get; set; } = new InputStageDieMapMarkPoint { Name = "Top" };
        [DataMember] public InputStageDieMapMarkPoint Bottom { get; set; } = new InputStageDieMapMarkPoint { Name = "Bottom" };
        [DataMember] public InputStageDieMapMarkPoint Left { get; set; } = new InputStageDieMapMarkPoint { Name = "Left" };
        [DataMember] public InputStageDieMapMarkPoint Right { get; set; } = new InputStageDieMapMarkPoint { Name = "Right" };
        [DataMember] public string VisionTargetId { get; set; } = VisionAlignTargetIds.Center;
        [DataMember] public int VisionRetryCount { get; set; } = 3;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsurePoints();
        }

        public void EnsurePoints()
        {
            if (Top == null) Top = new InputStageDieMapMarkPoint();
            if (Bottom == null) Bottom = new InputStageDieMapMarkPoint();
            if (Left == null) Left = new InputStageDieMapMarkPoint();
            if (Right == null) Right = new InputStageDieMapMarkPoint();
            if (string.IsNullOrWhiteSpace(Top.Name)) Top.Name = "Top";
            if (string.IsNullOrWhiteSpace(Bottom.Name)) Bottom.Name = "Bottom";
            if (string.IsNullOrWhiteSpace(Left.Name)) Left.Name = "Left";
            if (string.IsNullOrWhiteSpace(Right.Name)) Right.Name = "Right";
            if (VisionRetryCount <= 0) VisionRetryCount = 3;
            if (string.IsNullOrWhiteSpace(VisionTargetId) ||
                string.Equals(VisionTargetId, "DieMapMark", StringComparison.OrdinalIgnoreCase))
                VisionTargetId = VisionAlignTargetIds.Center;
        }

        public InputStageDieMapMarkPoint[] Points()
        {
            EnsurePoints();
            return new[] { Top, Bottom, Left, Right };
        }
    }

    public class InputStageRecipe : IRecipeData
    {
        [DataMember] public StageAxisPositions WaferY { get; set; } = new StageAxisPositions();
        [DataMember] public StageAxisPositions WaferT { get; set; } = new StageAxisPositions();
        [DataMember] public StageAxisPositions WaferZ { get; set; } = new StageAxisPositions();
        [DataMember] public StageAxisPositions VisionX { get; set; } = new StageAxisPositions();
        [DataMember] public StageAxisPositions NeedleX { get; set; } = new StageAxisPositions();
        [DataMember] public StageAxisPositions NeedleZ { get; set; } = new StageAxisPositions();
        [DataMember] public StageAxisPositions EjectPinZ { get; set; } = new StageAxisPositions();
        [DataMember] public InputStageDieMapRecipe DieMap { get; set; } = new InputStageDieMapRecipe();

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsurePositionObjects();
        }

        public void EnsurePositionObjects()
        {
            if (WaferY == null) WaferY = new StageAxisPositions();
            if (WaferT == null) WaferT = new StageAxisPositions();
            if (WaferZ == null) WaferZ = new StageAxisPositions();
            if (VisionX == null) VisionX = new StageAxisPositions();
            if (NeedleX == null) NeedleX = new StageAxisPositions();
            if (NeedleZ == null) NeedleZ = new StageAxisPositions();
            if (EjectPinZ == null) EjectPinZ = new StageAxisPositions();
            if (DieMap == null) DieMap = new InputStageDieMapRecipe();
            DieMap.EnsurePoints();
        }
    }

    public partial class InputStageUnit
    {
        private const double DefaultEstimatedPitchX = 0.15;
        private const double DefaultEstimatedPitchY = 0.15;

        private static double ResolveAxisVelocity(BaseAxis axis)
        {
            // DefaultVelocity 기반 일반 이동 속도. 전체 퍼센트 스케일을 적용한다(GetDefaultVel).
            return axis != null && axis.Config != null && axis.Config.GetRawDefaultVelocity() > 0.0
                ? axis.Config.GetDefaultVel()
                : MotionSpeedScale.ApplyDefaultVelocityScale(100.0);
        }

        private static double ResolveAxisFineVelocity(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.JogFineVelocity > 0.0
                ? axis.Config.JogFineVelocity
                : ResolveAxisVelocity(axis);
        }

        // [정정 2026-07-26, 사용자 지시] 일반 이동 가감속은 GetDefaultAcc/Dec(스케일 1회 적용
        // 최종값)로 넘긴다 — 축 레이어 명시 프로파일 스코프는 재스케일하지 않으므로 원값을
        // 넘기면 원본이 그대로 보드로 나갔다(EjectPinZ acc 10000 실측, 2026-07-26 23:03 로그).
        private static double ResolveAxisAcceleration(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.GetRawAcceleration() > 0.0
                ? axis.Config.GetDefaultAcc()
                : MotionSpeedScale.ApplyDefaultAccelerationScale(100.0);
        }

        private static double ResolveAxisDeceleration(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.GetRawDeceleration() > 0.0
                ? axis.Config.GetDefaultDec()
                : MotionSpeedScale.ApplyDefaultAccelerationScale(100.0);
        }

        private static double ResolveAxisFineAcceleration(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.JogAcceleration > 0.0
                ? axis.Config.JogAcceleration
                : ResolveAxisAcceleration(axis);
        }

        private static double ResolveAxisFineDeceleration(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.JogDeceleration > 0.0
                ? axis.Config.JogDeceleration
                : ResolveAxisDeceleration(axis);
        }
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
    public partial class InputStageUnit : BaseUnit<InputStageSetup, InputStageConfig, InputStageRecipe>, IUnitJogController
    {
        private const string ContinuousJogTargetName = "ContinuousJog";

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

        public string LastStageMoveFailureMessage { get; private set; }

        /// <summary>
        /// 니들 진공 흡착 DO.<br/>
        /// On 상태에서 테이프를 니들 상단에 흡착·고정하여 이젝트 동작의 정밀도를 높인다.
        /// </summary>
        public BaseDigitalOutput NeedleVacuum { get; private set; }

        /// <summary>니들 블로우 DO. 흡착 해제 시 에어를 분사하여 다이/테이프 분리를 돕는다.</summary>
        public BaseDigitalOutput NeedleBlow { get; private set; }

        /// <summary>이오나이저 On DO. 정전기 제거를 위해 사용한다.</summary>
        public BaseDigitalOutput Ionizer { get; private set; }

        /// <summary>8인치 웨이퍼 링 감지 DI.</summary>
        public BaseDigitalInput WaferStage8RingCheckSensor { get; private set; }

        /// <summary>12인치 웨이퍼 링 감지 DI.</summary>
        public BaseDigitalInput WaferStage12RingCheckSensor { get; private set; }

        /// <summary>웨이퍼 스테이지 터치 센서 DI.</summary>
        public BaseDigitalInput WaferStageTouchSensor { get; private set; }


        // InputLoadSequence으로 옮겨야함.
        // ──────────────────────────────────────────────────────────────────────
        //  §2. 외부 연동 인터페이스 (생성자 주입)
        // ──────────────────────────────────────────────────────────────────────

        ///// <summary>촬상 트리거 및 결과 수신을 위한 비전 PC TCP 통신 인터페이스.</summary>
        public IVisionTcpClient Vision { get; private set; }

        ///// <summary>맵 파싱 및 UI 전송을 위한 웨이퍼 맵 핸들러 인터페이스.</summary>
        public IWaferMapHandler MapHandler { get; private set; }

        /// <summary>피더 안전 위치 확인을 위한 로더 유닛 인터페이스.</summary>
        //public IWaferLoader        Loader     { get; private set; }

        ///// <summary>웨이퍼 ID 취득을 위한 바코드 리더 인터페이스.</summary>
        //public IBarcodeReader      Barcode    { get; private set; }



        ///// <summary>픽업 신호 송수신을 위한 TPU 연동 인터페이스.</summary>
        //public ITransferPickerUnit Tpu        { get; private set; }

        // ──────────────────────────────────────────────────────────────────────
        //  §3. 내부 상태 (얼라인 결과 및 원점 좌표)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>현재 로드된 웨이퍼의 맵 데이터. 로딩 완료 후 설정된다.</summary>
        public WaferMapData CurrentWaferMap  { get; private set; }

        public WaferMaterial CurrentWaferMaterial { get; private set; }

        public string CurrentWaferId { get { return CurrentWaferMaterial != null ? CurrentWaferMaterial.WaferId : ""; } }

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
        private readonly object _confirmSync = new object();
        private TaskCompletionSource<UserConfirmResult> _confirmTcs;

        public event Action UserConfirmRequested;
        public event Action UserConfirmWaitEnded;
        public event Action<string> UserConfirmProcessingFailed;

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
        public InputStageUnit(IVisionTcpClient    vision, IWaferMapHandler mapHandler)
            : base("InputStageUnit")
        {
            // ── 외부 인터페이스 저장 ───────────────────────────────────────
            Vision = vision ?? throw new ArgumentNullException("vision");
            MapHandler = mapHandler ?? throw new ArgumentNullException("mapHandler");

            //Loader     = loader     ?? throw new ArgumentNullException("loader");
            //Barcode    = barcode    ?? throw new ArgumentNullException("barcode");
            //Tpu        = tpu        ?? throw new ArgumentNullException("tpu");

            // ── Motion Axes ────────────────────────────────────────────────
            StageY       = AjinFactory.CreateAxis("StageY");
            StageT       = AjinFactory.CreateAxis("StageT");
            ExpanderZ    = AjinFactory.CreateAxis("ExpanderZ");
            CameraX      = AjinFactory.CreateAxis("CameraX");
            NeedleBlockX = AjinFactory.CreateAxis("NeedleBlockX");
            NeedleZ      = AjinFactory.CreateAxis("NeedleZ");
            EjectPinZ    = AjinFactory.CreateAxis("EjectPinZ");
            
            // ── Digital Output ─────────────────────────────────────────────
            NeedleVacuum = AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.NeedleVacuum);
            NeedleBlow = AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.NeedleBlow);
            Ionizer = AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.IonizerOn);

            // ── Digital Input ──────────────────────────────────────────────
            WaferStage8RingCheckSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeeder8RingCheck);
            WaferStage12RingCheckSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferFeeder12RingCheck);
            WaferStageTouchSensor = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.WaferStageTouchSensor);

            // ── Composite 트리 등록 ────────────────────────────────────────
            Components.Add(StageY);
            Components.Add(StageT);
            Components.Add(ExpanderZ);
            Components.Add(CameraX);
            Components.Add(NeedleBlockX);
            Components.Add(NeedleZ);
            Components.Add(EjectPinZ);
            Components.Add(NeedleVacuum);
            Components.Add(NeedleBlow);
            Components.Add(Ionizer);
            Components.Add(WaferStage8RingCheckSensor);
            Components.Add(WaferStage12RingCheckSensor);
            Components.Add(WaferStageTouchSensor);
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

        /// <summary>웨이퍼 로딩 후 얼라인 절차에서 확정된 StageT 보정 결과.</summary>
        public bool HasWaferAlignThetaResult { get; set; } = false;

        /// <summary>StageT 티칭 기준 위치 [deg]. 기본 기준은 WaferT.ProcessPosition.</summary>
        public double WaferAlignReferenceT { get; set; } = 0.0;

        /// <summary>얼라인 완료 후 사용해야 하는 StageT 절대 위치 [deg].</summary>
        public double WaferAlignCorrectedT { get; set; } = 0.0;

        /// <summary>StageT 티칭 기준 위치 대비 웨이퍼 단위 보정 오프셋 [deg].</summary>
        public double WaferAlignOffsetT { get; set; } = 0.0;

        public double DieMappingOffsetX { get; set; } = 0.0;

        public double DieMappingOffsetY { get; set; } = 0.0;

        /// <summary>
        /// NeedleZ가 Safe(Avoid) 위치에 있는지 확인합니다.<br/>
        /// InputStageY HOME 전, NeedleZ 간섭을 피하기 위한 안전 조건 확인에 사용합니다.
        /// </summary>
        public bool IsNeedleZInSafePosition()
        {
            if (NeedleZ == null || Recipe == null)
                return true;

            Recipe.EnsurePositionObjects();
            return Math.Abs(NeedleZ.ActualPosition - Recipe.NeedleZ.AvoidPosition) <= ResolveNeedleZInPositionTolerance();
        }

        public bool IsNeedleZInHomeOrSafePosition()
        {
            if (NeedleZ == null || Recipe == null)
                return true;

            Recipe.EnsurePositionObjects();
            double tolerance = ResolveNeedleZInPositionTolerance();
            return NeedleZ.ActualPosition <= 0.0 + tolerance ||
                   Math.Abs(NeedleZ.ActualPosition - Recipe.NeedleZ.AvoidPosition) <= tolerance;
        }

        public double ResolveWorkAreaCenterX()
        {
            if (Setup != null)
                return Setup.WorkAreaCenterX;

            return 0.0;
        }

        public double ResolveWorkAreaCenterY()
        {
            if (Setup != null)
                return Setup.WorkAreaCenterY;

            return 0.0;
        }

        public double ResolveWorkAreaRadius()
        {
            if (Setup != null && Setup.WorkAreaRadius > 0.0)
                return Setup.WorkAreaRadius;

            if (Setup != null && Setup.SafetyRadius > 0.0)
                return Setup.SafetyRadius;

            return 150.0;
        }

        public double ResolveNeedleWorkAreaRadius()
        {
            if (Setup != null && Setup.NeedleWorkAreaRadius > 0.0)
                return Setup.NeedleWorkAreaRadius;

            return 125.0;
        }

        public double ResolveNeedleWorkAreaCenterX()
        {
            if (Setup != null)
                return Setup.NeedleWorkAreaCenterX;

            return ResolveWorkAreaCenterX();
        }

        public double ResolveNeedleWorkAreaCenterY()
        {
            if (Setup != null)
                return Setup.NeedleWorkAreaCenterY;

            return ResolveWorkAreaCenterY();
        }

        public double ConvertNeedleXToVisionX(double needleX)
        {
            return needleX;
        }

        public bool IsInputStageWorkPointInArea(double visionX, double stageY, out string reason)
        {
            return IsWorkPointInArea(visionX, stageY, ResolveWorkAreaRadius(), "InputStage work area", out reason);
        }

        public bool IsNeedleWorkPointInArea(double needleX, double stageY, out string reason)
        {
            return IsNeedlePointInArea(needleX, stageY, ResolveNeedleWorkAreaRadius(), "Needle work area", out reason);
        }

        public bool TryResolveNeedleWorkPointMoveOrder(
            double targetNeedleX,
            double targetStageY,
            out bool moveNeedleXFirst,
            out string reason)
        {
            moveNeedleXFirst = true;
            reason = string.Empty;

            double currentNeedleX = NeedleBlockX != null ? NeedleBlockX.ActualPosition : targetNeedleX;
            double currentStageY = StageY != null ? StageY.ActualPosition : targetStageY;
            bool needleZAvoid = IsNeedleZInSafePosition();
            bool needleZLoweredOrAvoid = IsNeedleZInHomeOrSafePosition();

            string currentReason;
            bool currentInArea = IsNeedleWorkPointInArea(currentNeedleX, currentStageY, out currentReason);
            string finalReason;
            bool finalInArea = IsNeedleWorkPointInArea(targetNeedleX, targetStageY, out finalReason);

            if (needleZAvoid)
            {
                string safeXFirstReason;
                if (IsNeedleWorkPointInArea(targetNeedleX, currentStageY, out safeXFirstReason))
                {
                    moveNeedleXFirst = true;
                    return true;
                }

                string safeYFirstReason;
                if (IsNeedleWorkPointInArea(currentNeedleX, targetStageY, out safeYFirstReason))
                {
                    moveNeedleXFirst = false;
                    return true;
                }

                // NeedleZ가 Avoid 위치면 원 밖으로 빠지는 이동은 허용한다.
                moveNeedleXFirst = true;
                return true;
            }

            if (!currentInArea)
            {
                if (needleZLoweredOrAvoid && finalInArea)
                {
                    string loweredXFirstReason;
                    if (IsNeedleWorkPointInArea(targetNeedleX, currentStageY, out loweredXFirstReason))
                    {
                        moveNeedleXFirst = true;
                        return true;
                    }

                    string loweredYFirstReason;
                    if (IsNeedleWorkPointInArea(currentNeedleX, targetStageY, out loweredYFirstReason))
                    {
                        moveNeedleXFirst = false;
                        return true;
                    }

                    moveNeedleXFirst = true;
                    return true;
                }

                reason = "NeedleZ가 작업 높이에 있을 때 현재 NeedleX/StageY 위치가 니들 작업 원 밖입니다. " +
                    "currentNeedleX=" + currentNeedleX.ToString("F6") +
                    ", currentStageY=" + currentStageY.ToString("F6") +
                    ", targetNeedleX=" + targetNeedleX.ToString("F6") +
                    ", targetStageY=" + targetStageY.ToString("F6") +
                    ", currentReason=" + currentReason +
                    ", needleZSafeRequirement=Avoid 또는 0 이하";
                return false;
            }

            if (!finalInArea)
            {
                reason = "NeedleZ가 작업 높이에 있을 때 목표 NeedleX/StageY 위치가 니들 작업 원 밖입니다. " +
                    "currentNeedleX=" + currentNeedleX.ToString("F6") +
                    ", currentStageY=" + currentStageY.ToString("F6") +
                    ", targetNeedleX=" + targetNeedleX.ToString("F6") +
                    ", targetStageY=" + targetStageY.ToString("F6") +
                    ", targetReason=" + finalReason;
                return false;
            }

            string xFirstReason;
            if (IsNeedleWorkPointInArea(targetNeedleX, currentStageY, out xFirstReason))
            {
                moveNeedleXFirst = true;
                return true;
            }

            string yFirstReason;
            if (IsNeedleWorkPointInArea(currentNeedleX, targetStageY, out yFirstReason))
            {
                moveNeedleXFirst = false;
                return true;
            }

            reason = "NeedleZ가 작업 높이에 있을 때 NeedleX/StageY 이동 가능한 안전 순서를 찾지 못했습니다. " +
                "currentNeedleX=" + currentNeedleX.ToString("F6") +
                ", currentStageY=" + currentStageY.ToString("F6") +
                ", targetNeedleX=" + targetNeedleX.ToString("F6") +
                ", targetStageY=" + targetStageY.ToString("F6") +
                ", needleXFirst=" + xFirstReason +
                ", stageYFirst=" + yFirstReason;
            return false;
        }

        public Task<int> MoveNeedleWorkPointSafelyAsync(
            double targetNeedleX,
            double targetStageY,
            bool bFine,
            string source = null)
        {
            return MoveNeedleWorkPointSafelyAsync(
                targetNeedleX,
                targetStageY,
                (axis, target) => MoveInputStageAxis(axis, target, bFine),
                source);
        }

        public Task<int> MoveNeedleWorkPointSafelyAsync(
            double targetNeedleX,
            double targetStageY,
            JogSpeedType speedType,
            double customSpeed,
            string source = null)
        {
            return MoveNeedleWorkPointSafelyAsync(
                targetNeedleX,
                targetStageY,
                (axis, target) => MoveInputStageAxis(axis, target, speedType, customSpeed),
                source);
        }

        public Task<int> MoveNeedleWorkPointSafelyAsync(
            double targetNeedleX,
            double targetStageY,
            double velocity,
            double acceleration,
            double deceleration,
            int timeoutMs,
            string source = null)
        {
            return MoveNeedleWorkPointSafelyAsync(
                targetNeedleX,
                targetStageY,
                (axis, target) => MoveInputStageAxisWithMotionAndVerifyAsync(
                    axis,
                    target,
                    velocity,
                    acceleration,
                    deceleration,
                    timeoutMs),
                source);
        }

        private async Task<int> MoveNeedleWorkPointSafelyAsync(
            double targetNeedleX,
            double targetStageY,
            Func<WaferStageAxis, double, Task<int>> moveAxisAsync,
            string source)
        {
            string moveSource = string.IsNullOrWhiteSpace(source)
                ? "InputStageUnit.MoveNeedleWorkPointSafelyAsync"
                : source;

            try
            {
                bool moveNeedleXFirst;
                string orderReason;
                if (!TryResolveNeedleWorkPointMoveOrder(targetNeedleX, targetStageY, out moveNeedleXFirst, out orderReason))
                {
                    return RaiseStageAlarm(AlarmSeverity.Error, "IN-STAGE-NEEDLE-XY-PATH", moveSource,
                        "NeedleX/StageY 이동 안전 순서를 찾지 못했습니다. " + orderReason);
                }

                if (moveNeedleXFirst)
                {
                    int result = await moveAxisAsync(WaferStageAxis.NeedleX, targetNeedleX).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return await moveAxisAsync(WaferStageAxis.WaferY, targetStageY).ConfigureAwait(false);
                }
                else
                {
                    int result = await moveAxisAsync(WaferStageAxis.WaferY, targetStageY).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return await moveAxisAsync(WaferStageAxis.NeedleX, targetNeedleX).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                return RaiseStageAlarm(AlarmSeverity.Error, "IN-STAGE-NEEDLE-XY-PATH-EX", moveSource,
                    "NeedleX/StageY 안전 순서 이동 중 예외가 발생했습니다. error=" + ex.Message +
                    ", targetNeedleX=" + targetNeedleX.ToString("F6") +
                    ", targetStageY=" + targetStageY.ToString("F6"));
            }
            finally
            {
            }
        }

        public bool IsInputStageAxisTargetAllowedInWorkArea(WaferStageAxis axis, double target, out string reason)
        {
            reason = string.Empty;
            if (Recipe == null)
                return true;

            Recipe.EnsurePositionObjects();

            // StageY가 비공정 티칭 위치에서 Process로 진입할 때는 작업영역 안 목표라도 NeedleZ 안전 위치를 먼저 확인한다.
            if (axis == WaferStageAxis.WaferY &&
                !VerifyNeedleZSafeForWaferYProcessEntry(target, out reason))
                return false;

            // NeedleZ가 작업 높이에 있으면 StageY/NeedleX 이동의 현재점과 목표점이 모두 니들 작업 영역 안이어야 한다.
            if ((axis == WaferStageAxis.WaferY || axis == WaferStageAxis.NeedleX) &&
                !VerifyNeedleZRaisedXyMoveInNeedleWorkArea(axis, target, out reason))
                return false;

            if (axis == WaferStageAxis.VisionX)
            {
                // InputVisionX는 카메라/캘리브레이션/티칭 용도로 wafer 작업 원 밖까지 이동해야 한다.
                // 상부 공유레일/피커/피더 간섭은 MotionGuard에서 별도로 확인하므로 여기서는 원형 작업영역으로 제한하지 않는다.
                return true;
            }

            if (axis == WaferStageAxis.WaferY &&
                IsWaferYNonProcessTravelTeachingTarget(target))
            {
                if (!VerifyExpanderZSafeForStageYNonProcessTarget(axis, target, out reason))
                    return false;

                return VerifyNeedleZSafeForNonProcessTarget(axis, target, out reason);
            }

            if (IsSafeTeachingTarget(axis, target))
            {
                if (!VerifyExpanderZSafeForStageYNonProcessTarget(axis, target, out reason))
                    return false;

                return VerifyNeedleZSafeForNonProcessTarget(axis, target, out reason);
            }

            if (axis == WaferStageAxis.WaferY && IsNeedleZInHomeOrSafePosition())
                return true;

            if (axis == WaferStageAxis.WaferY)
            {
                double needleX = NeedleBlockX != null ? NeedleBlockX.ActualPosition : ResolveNeedleWorkAreaCenterX();
                // 현재 기준: StageY 작업 반경은 CameraX가 아니라 현재 NeedleX/StageY 실축 좌표로 계산한다.
                return IsNeedleWorkPointInArea(needleX, target, out reason);
            }

            if (axis == WaferStageAxis.NeedleX)
            {
                if (IsNeedleZInSafePosition())
                    return true;

                double targetY = StageY != null ? StageY.ActualPosition : ResolveWorkAreaCenterY();
                return IsNeedleWorkPointInArea(target, targetY, out reason);
            }

            if (axis == WaferStageAxis.NeedleZ)
            {
                // NeedleZ는 니들 작업 영역 밖에서는 하강 안전 위치로 내려가는 이동만 허용한다.
                return VerifyNeedleZMoveAllowedByNeedleWorkArea(target, out reason);
            }

            if (axis == WaferStageAxis.EjectPinZ)
            {
                double needleX = NeedleBlockX != null ? NeedleBlockX.ActualPosition : Recipe.NeedleX.ProcessPosition;
                double stageY = StageY != null ? StageY.ActualPosition : ResolveWorkAreaCenterY();
                return IsNeedleWorkPointInArea(needleX, stageY, out reason);
            }

            if (axis == WaferStageAxis.WaferT || axis == WaferStageAxis.WaferExpandingZ)
            {
                double needleX = NeedleBlockX != null ? NeedleBlockX.ActualPosition : ResolveNeedleWorkAreaCenterX();
                double stageY = StageY != null ? StageY.ActualPosition : ResolveWorkAreaCenterY();
                // 현재 기준: Stage T/Z 작업 반경도 실제 간섭축인 NeedleX/StageY 기준으로 확인한다.
                return IsNeedleWorkPointInArea(needleX, stageY, out reason);
            }

            return true;
        }

        private bool VerifyNeedleZRaisedXyMoveInNeedleWorkArea(WaferStageAxis axis, double target, out string reason)
        {
            reason = string.Empty;
            if (axis == WaferStageAxis.WaferY && IsNeedleZInHomeOrSafePosition())
                return true;

            if (IsNeedleZInSafePosition())
                return true;

            bool needleZLoweredOrAvoid = IsNeedleZInHomeOrSafePosition();
            double currentNeedleX = NeedleBlockX != null ? NeedleBlockX.ActualPosition : Recipe.NeedleX.ProcessPosition;
            double currentStageY = StageY != null ? StageY.ActualPosition : ResolveNeedleWorkAreaCenterY();
            double targetNeedleX = axis == WaferStageAxis.NeedleX ? target : currentNeedleX;
            double targetStageY = axis == WaferStageAxis.WaferY ? target : currentStageY;

            string currentReason;
            if (!IsNeedleWorkPointInArea(currentNeedleX, currentStageY, out currentReason))
            {
                if (needleZLoweredOrAvoid)
                    return true;

                reason = "NeedleZ가 Avoid 위치가 아닐 때 현재 NeedleX/StageY 위치가 니들 작업 영역 밖입니다. " +
                    currentReason +
                    ", axis=" + axis +
                    ", target=" + target.ToString("F3") +
                    ", needleZActual=" + (NeedleZ != null ? NeedleZ.ActualPosition.ToString("F3") : "null") +
                    ", needleZHome=0.000" +
                    ", needleZAvoid=" + (Recipe != null ? Recipe.NeedleZ.AvoidPosition.ToString("F3") : "null") +
                    ", tolerance=" + ResolveNeedleZInPositionTolerance().ToString("F3") +
                    ", required=Avoid 또는 0 이하";
                return false;
            }

            string targetReason;
            if (!IsNeedleWorkPointInArea(targetNeedleX, targetStageY, out targetReason))
            {
                reason = "NeedleZ가 Avoid 위치가 아닐 때 목표 NeedleX/StageY 위치가 니들 작업 영역 밖입니다. " +
                    targetReason +
                    ", axis=" + axis +
                    ", target=" + target.ToString("F3") +
                    ", currentNeedleX=" + currentNeedleX.ToString("F3") +
                    ", currentStageY=" + currentStageY.ToString("F3") +
                    ", targetNeedleX=" + targetNeedleX.ToString("F3") +
                    ", targetStageY=" + targetStageY.ToString("F3");
                return false;
            }

            return true;
        }

        private bool VerifyNeedleZMoveAllowedByNeedleWorkArea(double target, out string reason)
        {
            reason = string.Empty;
            double needleX = NeedleBlockX != null ? NeedleBlockX.ActualPosition : Recipe.NeedleX.ProcessPosition;
            double stageY = StageY != null ? StageY.ActualPosition : ResolveNeedleWorkAreaCenterY();

            string areaReason;
            if (IsNeedleWorkPointInArea(needleX, stageY, out areaReason))
                return true;

            if (IsNeedleZTargetLoweredPosition(target))
                return true;

            reason = "니들 작업 영역 밖에서는 NeedleZ가 하강 안전 위치로 이동하는 경우만 허용됩니다. " +
                areaReason +
                ", target=" + target.ToString("F3") +
                ", needleZActual=" + (NeedleZ != null ? NeedleZ.ActualPosition.ToString("F3") : "null") +
                ", needleZHome=0.000" +
                ", needleZAvoid=" + Recipe.NeedleZ.AvoidPosition.ToString("F3") +
                ", tolerance=" + ResolveNeedleZInPositionTolerance().ToString("F3");
            return false;
        }

        private bool IsNeedleZTargetLoweredPosition(double target)
        {
            double tolerance = ResolveNeedleZInPositionTolerance();
            return target <= 0.0 + tolerance ||
                   IsNear(target, Recipe.NeedleZ.AvoidPosition, tolerance);
        }

        public bool IsWaferYNonProcessTravelTeachingTarget(double target)
        {
            if (Recipe == null)
                return false;

            Recipe.EnsurePositionObjects();
            return IsStageTravelTeachingTarget(WaferStageAxis.WaferY, target) &&
                   !IsProcessTeachingTarget(WaferStageAxis.WaferY, target);
        }

        public bool VerifyNeedleZSafeForWaferYNonProcessTravel(double target, out string reason)
        {
            if (!IsWaferYNonProcessTravelTeachingTarget(target))
            {
                reason = string.Empty;
                return true;
            }

            return VerifyNeedleZSafeForNonProcessTarget(WaferStageAxis.WaferY, target, out reason);
        }

        private bool VerifyNeedleZSafeForWaferYProcessEntry(double target, out string reason)
        {
            reason = string.Empty;
            if (!IsProcessTeachingTarget(WaferStageAxis.WaferY, target))
                return true;

            if (StageY == null)
                return true;

            double currentStageY = StageY.ActualPosition;
            if (!IsWaferYNonProcessTravelTeachingTarget(currentStageY))
                return true;

            if (IsNeedleZInHomeOrSafePosition())
                return true;

            reason = "StageY 비공정 위치에서 Process 위치 진입 전 NeedleZ가 반드시 0 이하 또는 Avoid 위치에 있어야 합니다. " +
                "currentStageY=" + currentStageY.ToString("F3") +
                ", targetStageY=" + target.ToString("F3") +
                ", processStageY=" + Recipe.WaferY.ProcessPosition.ToString("F3") +
                ", needleZActual=" + (NeedleZ != null ? NeedleZ.ActualPosition.ToString("F3") : "null") +
                ", needleZHomeOrBelow=0.000" +
                ", needleZAvoid=" + (Recipe != null ? Recipe.NeedleZ.AvoidPosition.ToString("F3") : "null") +
                ", tolerance=" + ResolveNeedleZInPositionTolerance().ToString("F3");
            return false;
        }

        public bool IsInputStageJogAllowedInWorkArea(WaferStageAxis axis, Direction direction, out string reason)
        {
            reason = string.Empty;
            BaseAxis motionAxis = ResolveInputStageAxis(axis);
            if (motionAxis == null || motionAxis.Setup == null)
                return true;

            double target = direction == Direction.Plus
                ? motionAxis.Setup.SoftLimitPlus
                : motionAxis.Setup.SoftLimitMinus;

            return IsInputStageAxisTargetAllowedInWorkArea(axis, target, out reason);
        }

        private bool TryResolveInputStageContinuousJogTarget(WaferStageAxis axis, Direction direction, out double target, out string reason)
        {
            reason = string.Empty;
            BaseAxis motionAxis = ResolveInputStageAxis(axis);
            target = motionAxis != null ? motionAxis.ActualPosition : 0.0;
            if (motionAxis == null)
            {
                reason = "InputStage jog axis is null. axis=" + axis;
                return false;
            }

            if (axis == WaferStageAxis.VisionX)
            {
                // VisionX continuous jog has no fixed ABS target. Use a short probe target
                // only to verify direction/limit; SharedRailX monitors current clearance while moving.
                double sign = direction == Direction.Plus ? 1.0 : -1.0;
                double probeDistance = Math.Max(1.0, ResolveAxisPositionTolerance(motionAxis) * 10.0);
                target = ClampToSoftLimit(motionAxis, motionAxis.ActualPosition + (sign * probeDistance));
                return VerifyJogDirectionTarget(axis, direction, target, out reason);
            }

            if (axis == WaferStageAxis.WaferY)
            {
                double stageYActual = motionAxis.ActualPosition;

                if (IsNeedleZInHomeOrSafePosition())
                {
                    target = ClampToSoftLimit(motionAxis, direction == Direction.Plus
                        ? motionAxis.Setup.SoftLimitPlus
                        : motionAxis.Setup.SoftLimitMinus);
                    return VerifyJogDirectionTarget(axis, direction, target, out reason);
                }

                double needleXActual = NeedleBlockX != null ? NeedleBlockX.ActualPosition : ResolveNeedleWorkAreaCenterX();
                string currentNeedleAreaReason;
                if (!IsNeedleWorkPointInArea(needleXActual, stageYActual, out currentNeedleAreaReason))
                {
                    reason = "InputStageY 조그 불가: NeedleZ가 Avoid 위치가 아닐 때 현재 NeedleX/StageY 위치가 니들 작업 영역 밖입니다. " +
                        "currentInArea=N" +
                        ", currentReason=" + currentNeedleAreaReason +
                        ", needleX=" + needleXActual.ToString("F3") +
                        ", stageY=" + stageYActual.ToString("F3") +
                        ", needleZActual=" + (NeedleZ != null ? NeedleZ.ActualPosition.ToString("F3") : "null") +
                        ", needleZHome=0.000" +
                        ", needleZAvoid=" + (Recipe != null ? Recipe.NeedleZ.AvoidPosition.ToString("F3") : "null") +
                        ", tolerance=" + ResolveNeedleZInPositionTolerance().ToString("F3");
                    return false;
                }

                // NeedleZ가 Avoid 위치가 아닌 StageY 조그는 NeedleX/StageY 니들 작업 원 안에서만 경계 목표를 계산한다.
                if (!TryResolveCircularJogTarget(
                    stageYActual,
                    needleXActual,
                    ResolveNeedleWorkAreaCenterY(),
                    ResolveNeedleWorkAreaCenterX(),
                    ResolveNeedleWorkAreaRadius(),
                    direction,
                    "Needle work area",
                    out target,
                    out reason))
                    return false;

                double boundaryMargin = Math.Max(ResolveAxisPositionTolerance(motionAxis), 0.01);
                double insideTarget = direction == Direction.Plus
                    ? target - boundaryMargin
                    : target + boundaryMargin;
                if ((direction == Direction.Plus && insideTarget > stageYActual) ||
                    (direction == Direction.Minus && insideTarget < stageYActual))
                    target = insideTarget;

                target = ClampToSoftLimit(motionAxis, target);
                return VerifyJogDirectionTarget(axis, direction, target, out reason);
            }

            if (axis == WaferStageAxis.NeedleX)
            {
                if (IsNeedleZInSafePosition())
                {
                    target = ClampToSoftLimit(motionAxis, direction == Direction.Plus
                        ? motionAxis.Setup.SoftLimitPlus
                        : motionAxis.Setup.SoftLimitMinus);
                    return VerifyResolvedJogTarget(axis, direction, target, out reason);
                }

                double stageY = StageY != null ? StageY.ActualPosition : ResolveWorkAreaCenterY();
                if (!TryResolveNeedleXContinuousJogTarget(
                    motionAxis.ActualPosition,
                    stageY,
                    direction,
                    out target,
                    out reason))
                    return false;

                return true;
            }

            if (axis == WaferStageAxis.NeedleZ || axis == WaferStageAxis.EjectPinZ)
            {
                double safeTarget = ResolveStagePositions(axis).AvoidPosition;
                if (IsDirectionTowardTarget(motionAxis.ActualPosition, safeTarget, direction, ResolveAxisPositionTolerance(motionAxis)))
                {
                    target = ClampToSoftLimit(motionAxis, safeTarget);
                    return VerifyResolvedJogTarget(axis, direction, target, out reason);
                }

                target = ClampToSoftLimit(motionAxis, direction == Direction.Plus
                    ? motionAxis.Setup.SoftLimitPlus
                    : motionAxis.Setup.SoftLimitMinus);
                return VerifyResolvedJogTarget(axis, direction, target, out reason);
            }

            target = ClampToSoftLimit(motionAxis, direction == Direction.Plus
                ? motionAxis.Setup.SoftLimitPlus
                : motionAxis.Setup.SoftLimitMinus);
            return VerifyResolvedJogTarget(axis, direction, target, out reason);
        }

        private bool TryResolveNeedleXContinuousJogTarget(
            double needleActual,
            double stageY,
            Direction direction,
            out double target,
            out string reason)
        {
            target = needleActual;
            reason = string.Empty;

            double radius = ResolveNeedleWorkAreaRadius();
            if (radius <= 0.0)
            {
                reason = "Needle work area radius is invalid. radius=" + radius.ToString("F3");
                return false;
            }

            double centerX = ResolveNeedleWorkAreaCenterX();
            double centerY = ResolveNeedleWorkAreaCenterY();
            double yDelta = stageY - centerY;
            double remain = (radius * radius) - (yDelta * yDelta);
            if (remain < 0.0)
            {
                reason = "Needle work area Y is outside circular band. y=" + stageY.ToString("F3") +
                    ", centerY=" + centerY.ToString("F3") +
                    ", delta=" + yDelta.ToString("F3") +
                    ", radius=" + radius.ToString("F3");
                return false;
            }

            double span = Math.Sqrt(Math.Max(0.0, remain));
            double minNeedleX = centerX - span;
            double maxNeedleX = centerX + span;
            const double boundaryTolerance = 0.0001;
            bool outsidePlus = needleActual > maxNeedleX + boundaryTolerance;
            bool outsideMinus = needleActual < minNeedleX - boundaryTolerance;

            if (outsidePlus)
            {
                reason = "NeedleX 조그 불가: NeedleZ가 Avoid 위치가 아닐 때 현재 NeedleX/StageY 위치가 니들 작업 영역 밖입니다. x=" +
                    needleActual.ToString("F3") +
                    ", max=" + maxNeedleX.ToString("F3") +
                    ", centerX=" + centerX.ToString("F3") +
                    ", radius=" + radius.ToString("F3");
                return false;
            }

            if (outsideMinus)
            {
                reason = "NeedleX 조그 불가: NeedleZ가 Avoid 위치가 아닐 때 현재 NeedleX/StageY 위치가 니들 작업 영역 밖입니다. x=" +
                    needleActual.ToString("F3") +
                    ", min=" + minNeedleX.ToString("F3") +
                    ", centerX=" + centerX.ToString("F3") +
                    ", radius=" + radius.ToString("F3");
                return false;
            }

            target = ClampToSoftLimit(NeedleBlockX, direction == Direction.Plus ? maxNeedleX : minNeedleX);
            return VerifyResolvedJogTarget(WaferStageAxis.NeedleX, direction, target, out reason);
        }

        private bool VerifyResolvedJogTarget(WaferStageAxis axis, Direction direction, double target, out string reason)
        {
            if (!VerifyJogDirectionTarget(axis, direction, target, out reason))
                return false;

            return IsInputStageAxisTargetAllowedInWorkArea(axis, target, out reason);
        }

        private bool VerifyJogDirectionTarget(WaferStageAxis axis, Direction direction, double target, out string reason)
        {
            BaseAxis motionAxis = ResolveInputStageAxis(axis);
            double actual = motionAxis != null ? motionAxis.ActualPosition : target;
            double tolerance = ResolveAxisPositionTolerance(motionAxis);
            if (direction == Direction.Plus && target <= actual + tolerance)
            {
                reason = "Jog plus direction is blocked at work area boundary. axis=" + axis +
                    ", actual=" + actual.ToString("F3") +
                    ", target=" + target.ToString("F3") +
                    ", tolerance=" + tolerance.ToString("F3");
                return false;
            }

            if (direction == Direction.Minus && target >= actual - tolerance)
            {
                reason = "Jog minus direction is blocked at work area boundary. axis=" + axis +
                    ", actual=" + actual.ToString("F3") +
                    ", target=" + target.ToString("F3") +
                    ", tolerance=" + tolerance.ToString("F3");
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool TryResolveWaferYContinuousJogTarget(
            double stageYActual,
            double needleXActual,
            double stageYCenter,
            double needleXCenter,
            double radius,
            Direction direction,
            out double target,
            out bool needleXOutsideCircularBand,
            out string reason)
        {
            needleXOutsideCircularBand = false;
            if (TryResolveCircularJogTarget(
                stageYActual,
                needleXActual,
                stageYCenter,
                needleXCenter,
                radius,
                direction,
                "Needle work area",
                out target,
                out reason))
            {
                return true;
            }

            double xDelta = needleXActual - needleXCenter;
            if (radius <= 0.0 || Math.Abs(xDelta) <= radius)
                return false;

            needleXOutsideCircularBand = true;
            double tolerance = 0.0001;
            if (Math.Abs(stageYActual - stageYCenter) <= tolerance)
            {
                reason = "InputStageY가 이미 복귀 기준 위치입니다. y=" + stageYActual.ToString("F3") +
                    ", centerY=" + stageYCenter.ToString("F3") +
                    ", needleX=" + needleXActual.ToString("F3") +
                    ", centerX=" + needleXCenter.ToString("F3") +
                    ", radius=" + radius.ToString("F3");
                return false;
            }

            bool towardCenter = direction == Direction.Plus
                ? stageYCenter > stageYActual
                : stageYCenter < stageYActual;
            if (!towardCenter)
            {
                reason = "InputStageY 조그 방향이 복귀 방향이 아닙니다. y=" + stageYActual.ToString("F3") +
                    ", centerY=" + stageYCenter.ToString("F3") +
                    ", direction=" + direction +
                    ", needleX=" + needleXActual.ToString("F3") +
                    ", centerX=" + needleXCenter.ToString("F3") +
                    ", radius=" + radius.ToString("F3");
                return false;
            }

            target = stageYCenter;
            reason = string.Empty;
            return true;
        }

        private static bool TryResolveCircularJogTarget(
            double primaryActual,
            double secondaryActual,
            double primaryCenter,
            double secondaryCenter,
            double radius,
            Direction direction,
            string label,
            out double target,
            out string reason)
        {
            target = primaryActual;
            reason = string.Empty;
            if (radius <= 0.0)
            {
                reason = label + " radius is invalid. radius=" + radius.ToString("F3");
                return false;
            }

            double secondaryDelta = secondaryActual - secondaryCenter;
            double remain = (radius * radius) - (secondaryDelta * secondaryDelta);
            if (remain < 0.0)
            {
                reason = label +
                    " secondary axis is outside circular band. secondary=" + secondaryActual.ToString("F3") +
                    ", center=" + secondaryCenter.ToString("F3") +
                    ", delta=" + secondaryDelta.ToString("F3") +
                    ", radius=" + radius.ToString("F3");
                return false;
            }

            double span = Math.Sqrt(Math.Max(0.0, remain));
            double min = primaryCenter - span;
            double max = primaryCenter + span;
            const double tolerance = 0.0001;

            if (direction == Direction.Plus)
            {
                if (primaryActual > max + tolerance)
                {
                    reason = label +
                        " jog plus moves farther outside circular boundary. actual=" + primaryActual.ToString("F3") +
                        ", max=" + max.ToString("F3");
                    return false;
                }

                target = max;
                return true;
            }

            if (primaryActual < min - tolerance)
            {
                reason = label +
                    " jog minus moves farther outside circular boundary. actual=" + primaryActual.ToString("F3") +
                    ", min=" + min.ToString("F3");
                return false;
            }

            target = min;
            return true;
        }

        private static bool IsDirectionTowardTarget(double actual, double target, Direction direction, double tolerance)
        {
            double deadband = tolerance > 0.0 ? tolerance : 0.0001;
            if (Math.Abs(actual - target) <= deadband)
                return false;

            return direction == Direction.Plus ? target > actual : target < actual;
        }

        private static double ClampToSoftLimit(BaseAxis axis, double target)
        {
            if (axis == null || axis.Setup == null || !axis.Setup.SoftLimitEnabled)
                return target;

            if (target > axis.Setup.SoftLimitPlus)
                return axis.Setup.SoftLimitPlus;
            if (target < axis.Setup.SoftLimitMinus)
                return axis.Setup.SoftLimitMinus;
            return target;
        }

        private bool IsWorkPointInArea(double visionX, double stageY, double radius, string label, out string reason)
        {
            double centerX = ResolveWorkAreaCenterX();
            double centerY = ResolveWorkAreaCenterY();
            double dx = visionX - centerX;
            double dy = stageY - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance <= radius)
            {
                reason = string.Empty;
                return true;
            }

            reason = label +
                " out of radius. x=" + visionX.ToString("F3") +
                ", y=" + stageY.ToString("F3") +
                ", centerX=" + centerX.ToString("F3") +
                ", centerY=" + centerY.ToString("F3") +
                ", distance=" + distance.ToString("F3") +
                ", radius=" + radius.ToString("F3");
            return false;
        }

        private bool IsNeedlePointInArea(double needleX, double stageY, double radius, string label, out string reason)
        {
            double centerX = ResolveNeedleWorkAreaCenterX();
            double centerY = ResolveNeedleWorkAreaCenterY();
            double dx = needleX - centerX;
            double dy = stageY - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance <= radius)
            {
                reason = string.Empty;
                return true;
            }

            reason = label +
                " out of radius. x=" + needleX.ToString("F3") +
                ", y=" + stageY.ToString("F3") +
                ", centerX=" + centerX.ToString("F3") +
                ", centerY=" + centerY.ToString("F3") +
                ", distance=" + distance.ToString("F3") +
                ", radius=" + radius.ToString("F3");
            return false;
        }

        private bool IsStageTravelTeachingTarget(WaferStageAxis axis, double target)
        {
            StageAxisPositions positions = ResolveStagePositions(axis);
            BaseAxis motionAxis = ResolveInputStageAxis(axis);
            double tolerance = ResolveAxisPositionTolerance(motionAxis);
            return IsNear(target, positions.ProcessPosition, tolerance) ||
                   IsNear(target, positions.AvoidPosition, tolerance) ||
                   IsNear(target, positions.ReadyPosition, tolerance) ||
                   IsNear(target, positions.LoadPosition, tolerance) ||
                   IsNear(target, positions.UnloadPosition, tolerance);
        }

        private bool IsProcessTeachingTarget(WaferStageAxis axis, double target)
        {
            StageAxisPositions positions = ResolveStagePositions(axis);
            BaseAxis motionAxis = ResolveInputStageAxis(axis);
            double tolerance = ResolveAxisPositionTolerance(motionAxis);
            return IsNear(target, positions.ProcessPosition, tolerance);
        }

        private bool IsSafeTeachingTarget(WaferStageAxis axis, double target)
        {
            StageAxisPositions positions = ResolveStagePositions(axis);
            BaseAxis motionAxis = ResolveInputStageAxis(axis);
            double tolerance = ResolveAxisPositionTolerance(motionAxis);
            return IsNear(target, positions.AvoidPosition, tolerance) ||
                   IsNear(target, positions.ReadyPosition, tolerance) ||
                   IsNear(target, positions.LoadPosition, tolerance) ||
                   IsNear(target, positions.UnloadPosition, tolerance);
        }

        private bool VerifyNeedleZSafeForNonProcessTarget(WaferStageAxis axis, double target, out string reason)
        {
            reason = string.Empty;
            if (axis == WaferStageAxis.NeedleZ && IsNear(target, Recipe.NeedleZ.AvoidPosition, ResolveNeedleZInPositionTolerance()))
                return true;

            if (axis == WaferStageAxis.WaferY && IsNeedleZInHomeOrSafePosition())
                return true;

            if (IsNeedleZInSafePosition())
                return true;

            string requiredText = axis == WaferStageAxis.WaferY
                ? "0 이하 또는 Avoid 위치"
                : "Avoid 위치";
            reason = "비공정 위치 이동 전 NeedleZ가 반드시 " + requiredText + "에 있어야 합니다. " +
                "axis=" + axis +
                ", target=" + target.ToString("F3") +
                ", needleZActual=" + (NeedleZ != null ? NeedleZ.ActualPosition.ToString("F3") : "null") +
                ", needleZHomeOrBelow=0.000" +
                ", needleZAvoid=" + (Recipe != null ? Recipe.NeedleZ.AvoidPosition.ToString("F3") : "null") +
                ", tolerance=" + ResolveNeedleZInPositionTolerance().ToString("F3");
            return false;
        }

        private bool VerifyExpanderZSafeForStageYNonProcessTarget(WaferStageAxis axis, double target, out string reason)
        {
            reason = string.Empty;
            if (axis != WaferStageAxis.WaferY)
                return true;

            if (IsExpanderZInAvoidOrProcessPosition())
                return true;

            Recipe.EnsurePositionObjects();
            double actual = ExpanderZ != null ? ExpanderZ.ActualPosition : 0.0;
            double tolerance = ResolveExpanderZInPositionTolerance();
            reason = "StageY 비공정 위치 이동 전 StageZ는 Avoid 또는 Process 위치여야 합니다. " +
                "axis=" + axis +
                ", target=" + target.ToString("F3") +
                ", stageZActual=" + (ExpanderZ != null ? actual.ToString("F3") : "null") +
                ", stageZAvoid=" + Recipe.WaferZ.AvoidPosition.ToString("F3") +
                ", stageZProcess=" + Recipe.WaferZ.ProcessPosition.ToString("F3") +
                ", tolerance=" + tolerance.ToString("F3");
            return false;
        }

        private StageAxisPositions ResolveStagePositions(WaferStageAxis axis)
        {
            Recipe.EnsurePositionObjects();
            switch (axis)
            {
                case WaferStageAxis.WaferY: return Recipe.WaferY;
                case WaferStageAxis.WaferT: return Recipe.WaferT;
                case WaferStageAxis.WaferExpandingZ: return Recipe.WaferZ;
                case WaferStageAxis.VisionX: return Recipe.VisionX;
                case WaferStageAxis.NeedleX: return Recipe.NeedleX;
                case WaferStageAxis.NeedleZ: return Recipe.NeedleZ;
                case WaferStageAxis.EjectPinZ: return Recipe.EjectPinZ;
                default: return new StageAxisPositions();
            }
        }

        private static double ResolveAxisPositionTolerance(BaseAxis axis)
        {
            if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                return axis.Config.InPositionTolerance;

            return 0.05;
        }

        private static bool IsNear(double value, double target, double tolerance)
        {
            return Math.Abs(value - target) <= tolerance;
        }

        private double ResolveNeedleZInPositionTolerance()
        {
            try
            {
                if (NeedleZ != null && NeedleZ.Config != null && NeedleZ.Config.InPositionTolerance >= 0.0)
                    return NeedleZ.Config.InPositionTolerance;
            }
            catch
            {
            }
            finally
            {
            }

            return 0.05;
        }

        /// <summary>
        /// VisionX(CameraX)가 Avoid 위치에 있는지 확인합니다.<br/>
        /// Shared Rail X축(FrontPickerX 등) HOME 전 간섭을 피하기 위한 조건 확인에 사용합니다.
        /// </summary>
        public bool IsVisionXInAvoidPosition()
        {
            if (CameraX == null || Recipe == null)
                return true;

            Recipe.EnsurePositionObjects();
            return Math.Abs(CameraX.ActualPosition - Recipe.VisionX.AvoidPosition) <= ResolveVisionXInPositionTolerance();
        }

        private double ResolveVisionXInPositionTolerance()
        {
            try
            {
                if (CameraX != null && CameraX.Config != null && CameraX.Config.InPositionTolerance >= 0.0)
                    return CameraX.Config.InPositionTolerance;
            }
            catch
            {
            }
            finally
            {
            }

            return 0.05;
        }

        /// <summary>
        /// ExpanderZ(InputExpandingZ)가 Avoid 위치에 있는지 확인합니다.<br/>
        /// Shared Rail X축(FrontPickerX 등) HOME 전 간섭을 피하기 위한 조건 확인에 사용합니다.
        /// </summary>
        public bool IsExpanderZInAvoidPosition()
        {
            if (ExpanderZ == null || Recipe == null)
                return true;

            Recipe.EnsurePositionObjects();
            return Math.Abs(ExpanderZ.ActualPosition - Recipe.WaferZ.AvoidPosition) <= ResolveExpanderZInPositionTolerance();
        }

        public bool IsExpanderZInAvoidOrProcessPosition()
        {
            if (ExpanderZ == null || Recipe == null)
                return true;

            Recipe.EnsurePositionObjects();
            double tolerance = ResolveExpanderZInPositionTolerance();
            return Math.Abs(ExpanderZ.ActualPosition - Recipe.WaferZ.AvoidPosition) <= tolerance ||
                   Math.Abs(ExpanderZ.ActualPosition - Recipe.WaferZ.ProcessPosition) <= tolerance;
        }

        private double ResolveExpanderZInPositionTolerance()
        {
            try
            {
                if (ExpanderZ != null && ExpanderZ.Config != null && ExpanderZ.Config.InPositionTolerance >= 0.0)
                    return ExpanderZ.Config.InPositionTolerance;
            }
            catch
            {
            }
            finally
            {
            }

            return 0.05;
        }

        public bool CanHandleJogAxis(BaseAxis axis)
        {
            if (axis == null)
                return false;

            WaferStageAxis stageAxis;
            return TryResolveInputStageAxis(axis, out stageAxis);
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

            // 이동 중 반복 Step Jog 입력은 조작 중복이므로 알람 없이 리턴한다.
            if (IsJogAxisMoving(axis))
            {
                VerifyJogSafetyWhileMoving(axis, direction);
                return 0;
            }

            double signedDistance = (direction < 0 ? -1.0 : 1.0) * Math.Abs(axisStepDistance);
            double target = axis.ActualPosition + signedDistance;

            WaferStageAxis stageAxis;
            if (TryResolveInputStageAxis(axis, out stageAxis))
            {
                int result = await MoveInputStageAxis(stageAxis, target, speedType, customSpeed).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return 0;
            }

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

            // 이동 중 반복 Continuous Jog 입력은 현재 이동을 유지하고 추가 알람을 만들지 않는다.
            if (IsJogAxisMoving(axis))
            {
                VerifyJogSafetyWhileMoving(axis, direction);
                return Task.FromResult(0);
            }

            double speed = UnitJogVelocityResolver.Resolve(axis, speedType, customSpeed);
            Direction dir = direction < 0 ? Direction.Minus : Direction.Plus;

            WaferStageAxis stageAxis;
            if (TryResolveInputStageAxis(axis, out stageAxis))
                return Task.FromResult(ManualMoveInputStageAxisJog(stageAxis, dir, speed));

            return Task.FromResult(0);
        }

        private static bool IsJogAxisMoving(BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return false;

                axis.UpdateStatus();
                return axis.IsMoving;
            }
            catch
            {
                return axis != null && axis.IsMoving;
            }
            finally
            {
            }
        }

        private static void VerifyJogSafetyWhileMoving(BaseAxis axis, int direction)
        {
            try
            {
                if (axis == null)
                    return;

                SharedRailXMotionRuntime.VerifyJogSafetyWhileMoving(axis, direction);
            }
            catch
            {
            }
            finally
            {
            }
        }

        public Task<int> StopJogAsync(BaseAxis axis)
        {
            if (!CanHandleJogAxis(axis))
                return Task.FromResult(-1);

            WaferStageAxis stageAxis;
            if (TryResolveInputStageAxis(axis, out stageAxis))
                ManualStopInputStageAxis(stageAxis);

            return Task.FromResult(0);
        }

        public async Task<int> MoveInputStageAxis(WaferStageAxis axis, double targetPos, bool bFine = false)
        {
            return await MoveInputStageAxis(axis, targetPos, bFine, false).ConfigureAwait(false);
        }

        public async Task<int> MoveInputStageAxis(WaferStageAxis axis, double targetPos, bool bFine, bool forceMove)
        {
            try
            {
                BaseAxis item = ResolveInputStageAxis(axis);
                if (item == null)
                {
                    LastStageMoveFailureMessage = axis + " move failed. axis is null. target=" + targetPos;
                    return RaiseStageAlarm(AlarmSeverity.Error, "IN-STAGE-MOVE", Name, LastStageMoveFailureMessage);
                }

                // 인터락 사전검사는 실제 이동(MoveAbsoluteAsync)의 BaseAxis.MotionGuard 훅에서
                // InputStageInterlockRules.Verify로 1번 수행한다. 여기서 중복 호출하지 않는다.
                double tolerance = ResolveAxisPositionTolerance(item);
                if (!forceMove && item.IsAtTargetPosition(targetPos, tolerance))
                {
                    LastStageMoveFailureMessage = string.Empty;
                    return 0;
                }

                double velocity = ResolveInputStageMoveVelocity(axis, bFine);
                double acceleration = ResolveInputStageMoveAcceleration(axis, bFine);
                double deceleration = ResolveInputStageMoveDeceleration(axis, bFine);
                int result = await SharedRailXMotionRuntime.MoveAxisAsync(item, targetPos, velocity, acceleration, deceleration, forceMove).ConfigureAwait(false);
                if (result != 0 || item.IsAlarm)
                {
                    string message = axis + " move failed. result=" + result +
                        ", alarm=" + item.IsAlarm +
                        ", alarmCode=" + item.AlarmCode +
                        ", servo=" + item.IsServoOn +
                        ", moving=" + item.IsMoving +
                        ", actual=" + item.ActualPosition +
                        ", target=" + targetPos +
                        FormatAxisLastMotionFailure(item);
                    LastStageMoveFailureMessage = message;
                    return ReportStageMoveFailure("IN-STAGE-MOVE", result, message);
                }

                // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
                LastStageMoveFailureMessage = string.Empty;
                return 0;
            }
            catch (Exception ex)
            {
                LastStageMoveFailureMessage = axis + " move exception. target=" + targetPos + ". " + ex.Message;
                return RaiseStageAlarm(AlarmSeverity.Error, "IN-STAGE-MOVE", Name, LastStageMoveFailureMessage);
            }
            finally
            {
            }
        }

        public async Task<int> MoveInputStageAxis(WaferStageAxis axis, double targetPos, JogSpeedType speedType, double customSpeed)
        {
            try
            {
                BaseAxis item = ResolveInputStageAxis(axis);
                if (item == null)
                {
                    LastStageMoveFailureMessage = axis + " 조그 속도 위치 이동 실패. 축 정보가 없습니다. target=" + targetPos;
                    return RaiseStageAlarm(AlarmSeverity.Error, "IN-STAGE-MOVE", Name, LastStageMoveFailureMessage);
                }

                double velocity = UnitJogVelocityResolver.Resolve(item, speedType, customSpeed);
                double acceleration = UnitJogVelocityResolver.ResolveAcceleration(item);
                double deceleration = UnitJogVelocityResolver.ResolveDeceleration(item);
                int result = await MoveInputStageAxisCommandWithMotion(axis, targetPos, velocity, acceleration, deceleration, true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
                LastStageMoveFailureMessage = string.Empty;
                return 0;
            }
            catch (Exception ex)
            {
                LastStageMoveFailureMessage = axis + " 조그 속도 위치 이동 예외. target=" + targetPos + ". " + ex.Message;
                return RaiseStageAlarm(AlarmSeverity.Error, "IN-STAGE-MOVE-EX", Name, LastStageMoveFailureMessage);
            }
            finally
            {
            }
        }

        public async Task<int> MoveInputStageAxisCommandWithVelocity(WaferStageAxis axis, double targetPos, double velocity)
        {
            return await MoveInputStageAxisCommandWithMotion(axis, targetPos, velocity, 0.0, 0.0).ConfigureAwait(false);
        }

        public async Task<int> MoveInputStageAxisCommandWithMotion(WaferStageAxis axis, double targetPos, double velocity, double acceleration, double deceleration)
        {
            return await MoveInputStageAxisCommandWithMotion(axis, targetPos, velocity, acceleration, deceleration, false).ConfigureAwait(false);
        }

        private async Task<int> MoveInputStageAxisCommandWithMotion(WaferStageAxis axis, double targetPos, double velocity, double acceleration, double deceleration, bool isJogStep)
        {
            try
            {
                BaseAxis item = ResolveInputStageAxis(axis);
                if (item == null)
                {
                    LastStageMoveFailureMessage = axis + " move command failed. axis is null. target=" + targetPos;
                    return RaiseStageAlarm(AlarmSeverity.Error, "IN-STAGE-MOVE", Name, LastStageMoveFailureMessage);
                }

                double tolerance = ResolveAxisPositionTolerance(item);
                if (!isJogStep && item.IsAtTargetPosition(targetPos, tolerance))
                {
                    LastStageMoveFailureMessage = string.Empty;
                    return 0;
                }

                string interlockReason;
                bool interlockOk = isJogStep
                    ? MotionGuardRuntime.VerifyAxisStepJog(item, targetPos, "StepJog", out interlockReason)
                    : MotionGuardRuntime.VerifyAxisMove(item, targetPos, out interlockReason);
                if (!interlockOk)
                {
                    string message = axis + " move command blocked by interlock. target=" + targetPos + ". " + interlockReason;
                    LastStageMoveFailureMessage = message;
                    return RaiseStageAlarm(
                        AlarmSeverity.Error,
                        "IN-STAGE-MOVE-INTERLOCK",
                        Name,
                        message);
                }

                int result;
                if (isJogStep)
                {
                    using (SharedRailXMotionRuntime.EnterInternalDispatch())
                    {
                        result = await SharedRailXMotionRuntime.MoveAxisAsync(
                            item,
                            targetPos,
                            velocity,
                            acceleration,
                            deceleration,
                            true).ConfigureAwait(false);
                    }
                }
                else
                {
                    result = await SharedRailXMotionRuntime.MoveAxisAsync(
                        item,
                        targetPos,
                        velocity,
                        acceleration,
                        deceleration).ConfigureAwait(false);
                }

                if (result != 0 || item.IsAlarm)
                {
                    string message = axis + " move command failed. result=" + result +
                        ", alarm=" + item.IsAlarm +
                        ", alarmCode=" + item.AlarmCode +
                        ", servo=" + item.IsServoOn +
                        ", moving=" + item.IsMoving +
                        ", actual=" + item.ActualPosition +
                        ", target=" + targetPos +
                        FormatAxisLastMotionFailure(item);
                    LastStageMoveFailureMessage = message;
                    return ReportStageMoveFailure("IN-STAGE-MOVE", result, message);
                }

                LastStageMoveFailureMessage = string.Empty;
                return 0;
            }
            catch (Exception ex)
            {
                LastStageMoveFailureMessage = axis + " move command exception. target=" + targetPos + ". " + ex.Message;
                return RaiseStageAlarm(AlarmSeverity.Error, "IN-STAGE-MOVE", Name, LastStageMoveFailureMessage);
            }
            finally
            {
            }
        }

        public async Task<int> MoveInputStageAxisWithMotionAndVerifyAsync(
            WaferStageAxis axis,
            double targetPos,
            double velocity,
            double acceleration,
            double deceleration,
            int timeoutMs)
        {
            int result = await MoveInputStageAxisCommandWithMotion(
                axis,
                targetPos,
                velocity,
                acceleration,
                deceleration).ConfigureAwait(false);
            if (result != 0)
                return result;

            // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
            LastStageMoveFailureMessage = string.Empty;
            return 0;
        }

        public async Task<int> WaitInputStageAxisInPosition(WaferStageAxis axis, double targetPos, int timeoutMs)
        {
            return await WaitInputStageAxisInPosition(axis, targetPos, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> WaitInputStageAxisInPosition(WaferStageAxis axis, double targetPos, int timeoutMs, CancellationToken ct)
        {
            try
            {
                int waitCode = await WaitInputStageAxisInPositionResult(axis, targetPos, timeoutMs, ct).ConfigureAwait(false);
                if (waitCode == 0)
                    return 0;

                BaseAxis item = ResolveInputStageAxis(axis);
                return RaiseStageAlarm(
                    AlarmSeverity.Error,
                    "IN-STAGE-MOVE",
                    Name,
                    axis + " move/in-position wait failed. waitCode=" + waitCode +
                    FormatAxisLastMotionFailure(item));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "IN-STAGE-MOVE-WAIT", Name, ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private static string FormatAxisLastMotionFailure(BaseAxis axis)
        {
            if (axis == null || string.IsNullOrWhiteSpace(axis.LastMotionFailureMessage))
                return string.Empty;

            return ", lastMotionFailure=" + axis.LastMotionFailureMessage;
        }

        // 기존 조건: AxisMoveWaitResult(실패 7종) 반환 — 현재 기준: int(0=완료, 음수=실패) 반환(R3).
        //           실패 사유는 축.LastMotionFailureMessage에 기록된다.
        public async Task<int> WaitInputStageAxisInPositionResult(WaferStageAxis axis, double targetPos, int timeoutMs)
        {
            return await WaitInputStageAxisInPositionResult(axis, targetPos, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<int> WaitInputStageAxisInPositionResult(WaferStageAxis axis, double targetPos, int timeoutMs, CancellationToken ct)
        {
            BaseAxis item = ResolveInputStageAxis(axis);
            if (item == null)
                return -2;
            return await item.WaitMoveCompleteAsync(
                targetPos,
                timeoutMs > 0 ? timeoutMs : 10000,
                ct).ConfigureAwait(false);
        }

        public int ManualMoveInputStageAxisJog(WaferStageAxis axis, Direction dir, double speed)
        {
            BaseAxis item = ResolveInputStageAxis(axis);

            double target;
            string interlockReason;
            if (!TryResolveInputStageContinuousJogTarget(axis, dir, out target, out interlockReason))
            {
                // 기존 조건: 작업영역 경계에 걸린 조그 no-move는 알람 없이 정상 처리했다.
                // 현재 기준: 모터가 안 가고 멈추는 상태도 운전자가 원인을 알아야 하므로 인터락 알람으로 처리한다.
                // if (IsJogBoundaryNoMoveReason(interlockReason))
                // {
                //     LastStageMoveFailureMessage = string.Empty;
                //     return 0;
                // }

                string message = axis + " jog blocked by work area interlock. direction=" + dir + ". " + interlockReason;
                LastStageMoveFailureMessage = message;
                AlarmManager.Raise(AlarmSeverity.Error, "IN-STAGE-JOG-INTERLOCK", Name, message);
                return -1;
            }

            if (axis == WaferStageAxis.VisionX)
            {
                // VisionX continuous jog must be judged by current clearance and moving direction.
                // Do not dispatch it as an ABS move to the soft limit; that over-blocks normal jog.
                StartContinuousJogVelocity(item, dir, speed);
                LastStageMoveFailureMessage = string.Empty;
                return 0;
            }

            StartBoundedJogMoveAsync(item, target, speed);
            LastStageMoveFailureMessage = string.Empty;
            return 0;
        }

        private void StartContinuousJogVelocity(BaseAxis axis, Direction direction, double speed)
        {
            try
            {
                if (axis == null)
                    return;

                // Continuous jog has no fixed teaching target. SharedRailX must judge it by
                // current clearance + jog direction and then monitor clearance while moving.
                axis.MoveJogContinuous((int)direction, JogSpeedType.Custom, speed);
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "IN-STAGE-JOG-EX", Name, ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsJogBoundaryNoMoveReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return false;

            return reason.IndexOf("Jog plus direction is blocked at work area boundary", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   reason.IndexOf("Jog minus direction is blocked at work area boundary", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void StartBoundedJogMoveAsync(BaseAxis axis, double target, double speed)
        {
            try
            {
                // Continuous jog is dispatched as an absolute bounded move, so tag it
                // for interlock rules that must allow manual recovery movement.
                Task<int> moveTask;
                using (MotionGuardRuntime.BeginAxisTeachingMove(axis, target, ContinuousJogTargetName))
                    moveTask = SharedRailXMotionRuntime.MoveAxisAsync(axis, target, speed, true);

                _ = ObserveBoundedJogMoveAsync(axis, target, moveTask);
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "IN-STAGE-JOG-EX", Name, ex.Message);
            }
            finally
            {
            }
        }

        private async Task ObserveBoundedJogMoveAsync(BaseAxis axis, double target, Task<int> moveTask)
        {
            try
            {
                if (moveTask == null)
                    return;

                int result = await moveTask.ConfigureAwait(false);
                if (result == -4)
                {
                    LastStageMoveFailureMessage = string.Empty;
                    EventLogger.Write(
                        EventKind.Event,
                        "QMC",
                        "IN-STAGE-JOG",
                        "InputStage 제한 조그 사용자 정지. axis=" + (axis != null ? axis.Name : "-") +
                        ", target=" + target.ToString("F3"));
                    return;
                }

                if (result != 0)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "IN-STAGE-JOG-FAIL",
                        Name,
                        "InputStage 제한 조그 이동 실패. result=" + result);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "IN-STAGE-JOG-EX",
                        Name,
                        "InputStage 제한 조그 이동 중 예외가 발생했습니다. error=" + ex.Message);
                }
                catch
                {
                }
            }
            finally
            {
            }
        }

        public void ManualStopInputStageAxis(WaferStageAxis axis)
        {
            ResolveInputStageAxis(axis).StopJog();
        }

        private BaseAxis ResolveInputStageAxis(WaferStageAxis axis)
        {
            switch (axis)
            {
                // 웨이퍼 스테이지 Y축 반환
                case WaferStageAxis.WaferY: return StageY;
                // 웨이퍼 스테이지 T축 반환
                case WaferStageAxis.WaferT: return StageT;
                // 웨이퍼 확장 Z축 반환
                case WaferStageAxis.WaferExpandingZ: return ExpanderZ;
                // 인풋 비전 X축 반환
                case WaferStageAxis.VisionX: return CameraX;
                // 니들 블록 X축 반환
                case WaferStageAxis.NeedleX: return NeedleBlockX;
                // 니들 Z축 반환
                case WaferStageAxis.NeedleZ: return NeedleZ;
                // 이젝트 핀 Z축 반환
                case WaferStageAxis.EjectPinZ: return EjectPinZ;
                default: throw new ArgumentOutOfRangeException("axis");
            }
        }

        private bool TryResolveInputStageAxis(BaseAxis axis, out WaferStageAxis stageAxis)
        {
            stageAxis = WaferStageAxis.WaferY;
            if (axis == null)
                return false;

            if (ReferenceEquals(axis, StageY))
            {
                stageAxis = WaferStageAxis.WaferY;
                return true;
            }

            if (ReferenceEquals(axis, StageT))
            {
                stageAxis = WaferStageAxis.WaferT;
                return true;
            }

            if (ReferenceEquals(axis, ExpanderZ))
            {
                stageAxis = WaferStageAxis.WaferExpandingZ;
                return true;
            }

            if (ReferenceEquals(axis, CameraX))
            {
                stageAxis = WaferStageAxis.VisionX;
                return true;
            }

            if (ReferenceEquals(axis, NeedleBlockX))
            {
                stageAxis = WaferStageAxis.NeedleX;
                return true;
            }

            if (ReferenceEquals(axis, NeedleZ))
            {
                stageAxis = WaferStageAxis.NeedleZ;
                return true;
            }

            if (ReferenceEquals(axis, EjectPinZ))
            {
                stageAxis = WaferStageAxis.EjectPinZ;
                return true;
            }

            return false;
        }

        private double ResolveInputStageMoveVelocity(WaferStageAxis axis, bool bFine)
        {
            if (axis == WaferStageAxis.NeedleZ || axis == WaferStageAxis.EjectPinZ)
                return ResolveAxisVelocity(ResolveInputStageAxis(axis));

            BaseAxis item = ResolveInputStageAxis(axis);
            return bFine ? ResolveAxisFineVelocity(item) : ResolveAxisVelocity(item);
        }

        private double ResolveInputStageMoveVelocity(bool bFine)
        {
            return bFine ? ResolveAxisFineVelocity(StageY) : ResolveAxisVelocity(StageY);
        }

        private double ResolveInputStageMoveAcceleration(WaferStageAxis axis, bool bFine)
        {
            if (axis == WaferStageAxis.NeedleZ || axis == WaferStageAxis.EjectPinZ)
                return ResolveAxisAcceleration(ResolveInputStageAxis(axis));

            BaseAxis item = ResolveInputStageAxis(axis);
            return bFine ? ResolveAxisFineAcceleration(item) : ResolveAxisAcceleration(item);
        }

        private double ResolveInputStageMoveDeceleration(WaferStageAxis axis, bool bFine)
        {
            if (axis == WaferStageAxis.NeedleZ || axis == WaferStageAxis.EjectPinZ)
                return ResolveAxisDeceleration(ResolveInputStageAxis(axis));

            BaseAxis item = ResolveInputStageAxis(axis);
            return bFine ? ResolveAxisFineDeceleration(item) : ResolveAxisDeceleration(item);
        }

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
            TaskCompletionSource<UserConfirmResult> tcs;
            lock (_confirmSync)
            {
                tcs = _confirmTcs;
            }

            if (tcs != null)
                tcs.TrySetResult(result ?? new UserConfirmResult { IsConfirmed = false });
        }

        public void FailUserConfirmFromUi(string message)
        {
            TaskCompletionSource<UserConfirmResult> tcs;
            lock (_confirmSync)
            {
                tcs = _confirmTcs;
            }

            if (tcs != null)
                tcs.TrySetException(new InvalidOperationException(
                    string.IsNullOrWhiteSpace(message)
                        ? "InputStage 사용자 확인 화면 처리에 실패했습니다."
                        : message));
        }

        public void NotifyUserConfirmProcessingFailed(string message)
        {
            Action<string> handler = UserConfirmProcessingFailed;
            if (handler == null)
                return;

            try
            {
                handler(string.IsNullOrWhiteSpace(message)
                    ? "InputStage Review 처리에 실패했습니다. 조건을 확인한 뒤 다시 시도하세요."
                    : message);
            }
            catch
            {
            }
        }

        // ??????????????????????????????????????????????????????????????????????
        //  §6. 핵심 시퀀스 로직
        // ??????????????????????????????????????????????????????????????????????

        public async Task<int> LoadAndPrepareWaferAsync(
            string waferId,
            bool requireMapData,
            bool bFine = false,
            Func<Task<int>> beforeExpanderZMoveAsync = null)
        {
            try
            {
                EnsurePositionObjectsForSequence();

                // Feeder 로딩 준비 완료 조건은 NeedleZ와 EjectPinZ가 모두 Avoid여야 한다.
                // 두 축을 먼저 안전 위치로 이동해 뒤의 Feeder 시퀀스가 StageZ를 다시 내리지 않도록 한다.
                Task<int> needleZMove = MoveNeedleZAvoidForNonProcessMoveAsync(
                    bFine,
                    "InputStageUnit.LoadAndPrepareWaferAsync");
                Task<int> ejectPinZMove = MoveLoadPreparationAxisAsync(
                    WaferStageAxis.EjectPinZ,
                    Recipe.EjectPinZ.AvoidPosition,
                    EjectPinZ,
                    "EjectPinZ avoid",
                    "IS-LOAD-EJECT-Z",
                    bFine);
                int[] safeZMoveResults = await Task.WhenAll(needleZMove, ejectPinZMove).ConfigureAwait(false);
                if (safeZMoveResults[0] != 0)
                    return safeZMoveResults[0];
                if (safeZMoveResults[1] != 0)
                    return safeZMoveResults[1];

                int result = await MoveInputStageAxis(WaferStageAxis.WaferT, Recipe.WaferT.LoadPosition, bFine).ConfigureAwait(false);
                if (result != 0 || StageT.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-LOAD-T", "InputStageUnit.LoadAndPrepareWaferAsync",
                        "StageT Load 위치 이동 실패. result=" + result + ", alarm=" + StageT.IsAlarm);

                result = await WaitInputStageAxisInPosition(WaferStageAxis.WaferT, Recipe.WaferT.LoadPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageAxis(WaferStageAxis.WaferExpandingZ, Recipe.WaferZ.AvoidPosition, bFine).ConfigureAwait(false);
                if (result != 0 || ExpanderZ.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-LOAD-Z-AVOID", "InputStageUnit.LoadAndPrepareWaferAsync",
                        "ExpanderZ Avoid 위치 이동 실패. result=" + result + ", alarm=" + ExpanderZ.IsAlarm);

                result = await WaitInputStageAxisInPosition(WaferStageAxis.WaferExpandingZ, Recipe.WaferZ.AvoidPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // ExpanderZ가 Avoid인 동안 Feeder 진입 간섭축을 모두 정리한다.
                // PrepareLoad 종료 시 후속 IsInputStageFullyPreparedForCassetteLoad 조건과 동일한 자세가 된다.
                result = await MoveLoadPreparationAxisAsync(
                    WaferStageAxis.VisionX,
                    Recipe.VisionX.AvoidPosition,
                    CameraX,
                    "VisionX avoid",
                    "IS-LOAD-VISION-X",
                    bFine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveLoadPreparationAxisAsync(
                    WaferStageAxis.NeedleX,
                    Recipe.NeedleX.AvoidPosition,
                    NeedleBlockX,
                    "NeedleX avoid",
                    "IS-LOAD-NEEDLE-X",
                    bFine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageAxis(WaferStageAxis.WaferY, Recipe.WaferY.LoadPosition, bFine).ConfigureAwait(false);
                if (result != 0 || StageY.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-LOAD-Y", "InputStageUnit.LoadAndPrepareWaferAsync",
                        "StageY Load 위치 이동 실패. result=" + result + ", alarm=" + StageY.IsAlarm);

                result = await WaitInputStageAxisInPosition(WaferStageAxis.WaferY, Recipe.WaferY.LoadPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (beforeExpanderZMoveAsync != null)
                {
                    result = await beforeExpanderZMoveAsync().ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                result = await MoveInputStageAxis(WaferStageAxis.WaferExpandingZ, Recipe.WaferZ.LoadPosition, bFine).ConfigureAwait(false);
                if (result != 0 || ExpanderZ.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-LOAD-Z", "InputStageUnit.LoadAndPrepareWaferAsync",
                        "ExpanderZ Load 위치 이동 실패. result=" + result + ", alarm=" + ExpanderZ.IsAlarm);

                result = await WaitInputStageAxisInPosition(WaferStageAxis.WaferExpandingZ, Recipe.WaferZ.LoadPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (string.IsNullOrWhiteSpace(waferId))
                    waferId = "WAFER-" + DateTime.Now.ToString("yyyyMMddHHmmss");

                WaferMapData mapData = null;
                if (MapHandler != null)
                    mapData = await MapHandler.ParseMapAsync(waferId).ConfigureAwait(false);

                if (mapData == null)
                {
                    if (requireMapData)
                    {
                        return RaiseStageAlarm(AlarmSeverity.Error, "IS-MAP", "InputStageUnit.LoadAndPrepareWaferAsync",
                            "Wafer map load failed. waferId=" + waferId);
                    }

                    mapData = CreateFallbackWaferMap(waferId);
                }

                NormalizeWaferMap(mapData, waferId);
                CurrentWaferMap = mapData;

                if (MapHandler != null)
                    MapHandler.SendMapToUi(mapData);

                EventLogger.Write(EventKind.Event, "QMC", "IS-LOAD",
                    "InputStage wafer prepared. waferId=" + waferId + ", rows=" + mapData.RowCount + ", cols=" + mapData.ColumnCount);
                return 0;
            }
            catch (Exception ex)
            {
                return RaiseStageAlarm(AlarmSeverity.Error, "IS-LOAD-EX", "InputStageUnit.LoadAndPrepareWaferAsync",
                    "LoadAndPrepareWafer exception: " + ex.Message);
            }
            finally
            {
            }
        }

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
        public async Task<int> VisionAlignAndSetupOriginAsync(bool requireVisionAlign, bool bFine = false)
        {
            try
            {
                if (CurrentWaferMap == null)
                    CurrentWaferMap = CreateFallbackWaferMap("WAFER-NO-MAP");

                NormalizeWaferMap(CurrentWaferMap, CurrentWaferMap.WaferId);
                WaferMapData map = CurrentWaferMap;

                int result = await MoveInputStageAxis(WaferStageAxis.WaferY, Recipe.WaferY.ProcessPosition, bFine).ConfigureAwait(false);
                if (result != 0) return result;

                result = await WaitInputStageAxisInPosition(WaferStageAxis.WaferY, Recipe.WaferY.ProcessPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveInputStageAxis(WaferStageAxis.VisionX, Recipe.VisionX.ProcessPosition, bFine).ConfigureAwait(false);
                if (result != 0) return result;

                result = await WaitInputStageAxisInPosition(WaferStageAxis.VisionX, Recipe.VisionX.ProcessPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0) return result;

                if (!requireVisionAlign || Vision == null)
                {
                    SetEstimatedOriginFromCurrentPosition(map);
                    return 0;
                }

                int centerRow = map.RowCount / 2;
                int centerCol = map.ColumnCount / 2;

                bool useEstimatedDiePosition = true;
                result = await MoveToDieAsync(centerRow, centerCol, useEstimatedDiePosition, bFine).ConfigureAwait(false);
                if (result != 0) return result;

                for (int iter = 0; iter < Config.MaxAlignIterations; iter++)
                {
                    VisionAlignResult alignResult = await Vision.TriggerAlignAsync(VisionAlignTargetIds.Center).ConfigureAwait(false);
                    if (alignResult == null)
                        return RaiseStageAlarm(AlarmSeverity.Error, "IS-ALIGN", "InputStageUnit.VisionAlignAndSetupOriginAsync",
                            "Center vision align failed. iteration=" + (iter + 1));

                    double targetT = StageT.ActualPosition + alignResult.DeltaTheta;
                    int thetaResult = await StageT.MoveRelativeAsync(alignResult.DeltaTheta, ResolveAxisFineVelocity(StageT)).ConfigureAwait(false);
                    if (thetaResult != 0 || StageT.IsAlarm)
                        return RaiseStageAlarm(AlarmSeverity.Error, "IS-ALIGN-T", "InputStageUnit.VisionAlignAndSetupOriginAsync",
                            "StageT correction failed. result=" + thetaResult + ", alarm=" + StageT.IsAlarm);

                    int thetaWaitResult = await WaitInputStageAxisInPosition(WaferStageAxis.WaferT, targetT, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                    if (thetaWaitResult != 0)
                        return thetaWaitResult;

                    if (Math.Abs(alignResult.DeltaTheta) < Config.AlignConvergenceThresholdDeg)
                        break;
                }

                result = await MoveToDieAsync(map.Ref1Row, map.Ref1Col, useEstimatedDiePosition, bFine).ConfigureAwait(false);
                if (result != 0) return result;

                VisionAlignResult ref1Result = await Vision.TriggerAlignAsync(VisionAlignTargetIds.Ref1).ConfigureAwait(false);
                if (ref1Result == null)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-ALIGN-REF1", "InputStageUnit.VisionAlignAndSetupOriginAsync",
                        "Ref1 vision align failed.");

                double ref1X = CameraX.ActualPosition + ref1Result.DeltaX;
                double ref1Y = StageY.ActualPosition + ref1Result.DeltaY;

                result = await MoveToDieAsync(map.Ref2Row, map.Ref2Col, useEstimatedDiePosition, bFine).ConfigureAwait(false);
                if (result != 0) return result;

                VisionAlignResult ref2Result = await Vision.TriggerAlignAsync(VisionAlignTargetIds.Ref2).ConfigureAwait(false);
                if (ref2Result == null)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-ALIGN-REF2", "InputStageUnit.VisionAlignAndSetupOriginAsync",
                        "Ref2 vision align failed.");

                double ref2X = CameraX.ActualPosition + ref2Result.DeltaX;
                double ref2Y = StageY.ActualPosition + ref2Result.DeltaY;

                int colSpan = map.Ref2Col - map.Ref1Col;
                int rowSpan = map.Ref2Row - map.Ref1Row;
                PitchX = colSpan != 0 && Math.Abs(ref2X - ref1X) > 1e-6 ? (ref2X - ref1X) / colSpan : ResolveFallbackPitchX(ref1Result, ref2Result);
                PitchY = rowSpan != 0 && Math.Abs(ref2Y - ref1Y) > 1e-6 ? (ref2Y - ref1Y) / rowSpan : ResolveFallbackPitchY(ref1Result, ref2Result);
                OriginX = ref1X - (map.Ref1Col * PitchX);
                OriginY = ref1Y - (map.Ref1Row * PitchY);

                EventLogger.Write(EventKind.Event, "QMC", "IS-ALIGN",
                    "InputStage align complete. originX=" + OriginX + ", originY=" + OriginY + ", pitchX=" + PitchX + ", pitchY=" + PitchY);
                return 0;
            }
            catch (Exception ex)
            {
                return RaiseStageAlarm(AlarmSeverity.Error, "IS-ALIGN-EX", "InputStageUnit.VisionAlignAndSetupOriginAsync",
                    "VisionAlign exception: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> PrepareUnloadWaferAsync(
            bool bFine = false,
            Func<Task<int>> beforeExpanderZMoveAsync = null)
        {
            try
            {
                EnsurePositionObjectsForSequence();

                Task<int> needleZMove = MoveNeedleZAvoidForNonProcessMoveAsync(bFine, "InputStageUnit.PrepareUnloadWaferAsync");
                Task<int> ejectPinZMove = MoveUnloadSafeAxisAsync(
                    WaferStageAxis.EjectPinZ,
                    Recipe.EjectPinZ.AvoidPosition,
                    EjectPinZ,
                    "EjectPinZ avoid",
                    "IS-UNLOAD-EJECT-Z",
                    bFine);
                int[] zMoveResults = await Task.WhenAll(needleZMove, ejectPinZMove).ConfigureAwait(false);
                if (zMoveResults[0] != 0)
                    return zMoveResults[0];
                if (zMoveResults[1] != 0)
                    return zMoveResults[1];

                int result = await MoveInputStageAxis(WaferStageAxis.WaferT, Recipe.WaferT.UnloadPosition, bFine).ConfigureAwait(false);
                if (result != 0 || StageT.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-UNLOAD-T", "InputStageUnit.PrepareUnloadWaferAsync",
                        "StageT Unload 위치 이동 실패. result=" + result + ", alarm=" + StageT.IsAlarm);

                result = await WaitInputStageAxisInPosition(WaferStageAxis.WaferT, Recipe.WaferT.UnloadPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageAxis(WaferStageAxis.WaferExpandingZ, Recipe.WaferZ.AvoidPosition, bFine).ConfigureAwait(false);
                if (result != 0 || ExpanderZ.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-UNLOAD-Z-AVOID", "InputStageUnit.PrepareUnloadWaferAsync",
                        "ExpanderZ Avoid 위치 이동 실패. result=" + result + ", alarm=" + ExpanderZ.IsAlarm);

                result = await WaitInputStageAxisInPosition(WaferStageAxis.WaferExpandingZ, Recipe.WaferZ.AvoidPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveUnloadSafeAxisAsync(
                    WaferStageAxis.VisionX,
                    Recipe.VisionX.AvoidPosition,
                    CameraX,
                    "VisionX avoid",
                    "IS-UNLOAD-VISION-X",
                    bFine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // NeedleX는 AVOID 로 갈필요없다.
                //result = await MoveUnloadSafeAxisAsync(
                //    WaferStageAxis.NeedleX,
                //    Recipe.NeedleX.AvoidPosition,
                //    NeedleBlockX,
                //    "NeedleX avoid",
                //    "IS-UNLOAD-NEEDLE-X",
                //    bFine).ConfigureAwait(false);
                //if (result != 0)
                //    return result;

                result = await MoveInputStageAxis(WaferStageAxis.WaferY, Recipe.WaferY.UnloadPosition, bFine).ConfigureAwait(false);
                if (result != 0 || StageY.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-UNLOAD-Y", "InputStageUnit.PrepareUnloadWaferAsync",
                        "StageY Unload 위치 이동 실패. result=" + result + ", alarm=" + StageY.IsAlarm);

                result = await WaitInputStageAxisInPosition(WaferStageAxis.WaferY, Recipe.WaferY.UnloadPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (beforeExpanderZMoveAsync != null)
                {
                    result = await beforeExpanderZMoveAsync().ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                result = await MoveInputStageAxis(WaferStageAxis.WaferExpandingZ, Recipe.WaferZ.UnloadPosition, bFine).ConfigureAwait(false);
                if (result != 0 || ExpanderZ.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-UNLOAD-Z", "InputStageUnit.PrepareUnloadWaferAsync",
                        "ExpanderZ Unload 위치 이동 실패. result=" + result + ", alarm=" + ExpanderZ.IsAlarm);

                result = await WaitInputStageAxisInPosition(WaferStageAxis.WaferExpandingZ, Recipe.WaferZ.UnloadPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                OnWaferChangeRequested();
                return 0;
            }
            catch (Exception ex)
            {
                return RaiseStageAlarm(AlarmSeverity.Error, "IS-UNLOAD-EX", "InputStageUnit.PrepareUnloadWaferAsync",
                    "PrepareUnloadWafer exception: " + ex.Message);
            }
            finally
            {
            }
        }

        public void ClearCurrentWaferMap()
        {
            CurrentWaferMap = null;
            OriginX = 0.0;
            OriginY = 0.0;
            PitchX = 0.0;
            PitchY = 0.0;
            WaferAlignOffsetX = 0.0;
            WaferAlignOffsetY = 0.0;
            HasWaferAlignThetaResult = false;
            WaferAlignReferenceT = 0.0;
            WaferAlignCorrectedT = 0.0;
            WaferAlignOffsetT = 0.0;
            DieMappingOffsetX = 0.0;
            DieMappingOffsetY = 0.0;
        }

        public WaferMapData EnsureWaferMapForAlign(string waferId, bool allowFallback)
        {
            try
            {
                if (CurrentWaferMap == null && allowFallback)
                    CurrentWaferMap = CreateFallbackWaferMap(waferId);

                NormalizeWaferMap(CurrentWaferMap, waferId);
                return CurrentWaferMap;
            }
            catch (Exception ex)
            {
                RaiseStageAlarm(AlarmSeverity.Error, "IS-MAP-ALIGN", "InputStageUnit.EnsureWaferMapForAlign",
                    "Align wafer map prepare failed: " + ex.Message);
                return null;
            }
            finally
            {
            }
        }

        public double ResolveAlignPitchX(VisionAlignResult ref1Result, VisionAlignResult ref2Result)
        {
            try
            {
                return ResolveFallbackPitchX(ref1Result, ref2Result);
            }
            catch
            {
                return DefaultEstimatedPitchX;
            }
            finally
            {
            }
        }

        public double ResolveAlignPitchY(VisionAlignResult ref1Result, VisionAlignResult ref2Result)
        {
            try
            {
                return ResolveFallbackPitchY(ref1Result, ref2Result);
            }
            catch
            {
                return DefaultEstimatedPitchY;
            }
            finally
            {
            }
        }

        public void ApplyWaferAlignResult(double originX, double originY, double pitchX, double pitchY, double alignOffsetX, double alignOffsetY)
        {
            try
            {
                OriginX = originX;
                OriginY = originY;
                PitchX = pitchX;
                PitchY = pitchY;
                WaferAlignOffsetX = alignOffsetX;
                WaferAlignOffsetY = alignOffsetY;
                EventLogger.Write(EventKind.Event, "QMC", "IS-ALIGN",
                    "InputStage align result applied. originX=" + OriginX.ToString("F4") +
                    ", originY=" + OriginY.ToString("F4") +
                    ", pitchX=" + PitchX.ToString("F4") +
                    ", pitchY=" + PitchY.ToString("F4") +
                    ", offsetX=" + WaferAlignOffsetX.ToString("F4") +
                    ", offsetY=" + WaferAlignOffsetY.ToString("F4"));
            }
            catch (Exception ex)
            {
                RaiseStageAlarm(AlarmSeverity.Error, "IS-ALIGN-APPLY", "InputStageUnit.ApplyWaferAlignResult",
                    "Align result apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public double ResolveWaferAlignReferenceT()
        {
            try
            {
                if (Recipe == null)
                    return 0.0;

                Recipe.EnsurePositionObjects();
                return Recipe.WaferT != null ? Recipe.WaferT.ProcessPosition : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        public double ResolveWaferAlignThetaCorrectionLimit()
        {
            try
            {
                return Config != null && Config.AlignThetaCorrectionLimitDeg > 0.0
                    ? Config.AlignThetaCorrectionLimitDeg
                    : 1.0;
            }
            catch
            {
                return 1.0;
            }
            finally
            {
            }
        }

        public bool IsWaferAlignThetaOffsetWithinLimit(double offsetT, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (double.IsNaN(offsetT) || double.IsInfinity(offsetT))
                {
                    reason = "InputStage theta align offset is invalid.";
                    return false;
                }

                double limit = ResolveWaferAlignThetaCorrectionLimit();
                if (Math.Abs(offsetT) <= limit)
                {
                    reason = "InputStage theta align offset is within limit. offsetT=" + offsetT.ToString("F6") +
                             ", limit=" + limit.ToString("F6");
                    return true;
                }

                reason = "InputStage theta align offset exceeds limit. offsetT=" + offsetT.ToString("F6") +
                         ", limit=" + limit.ToString("F6");
                return false;
            }
            catch (Exception ex)
            {
                reason = "InputStage theta align offset limit check failed: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        public double ResolveManualDieDetectOffsetLimitX()
        {
            try
            {
                return Config != null && Config.ManualDieDetectOffsetLimitX > 0.0
                    ? Config.ManualDieDetectOffsetLimitX
                    : 5.0;
            }
            catch
            {
                return 5.0;
            }
            finally
            {
            }
        }

        public double ResolveManualDieDetectOffsetLimitY()
        {
            try
            {
                return Config != null && Config.ManualDieDetectOffsetLimitY > 0.0
                    ? Config.ManualDieDetectOffsetLimitY
                    : 5.0;
            }
            catch
            {
                return 5.0;
            }
            finally
            {
            }
        }

        public bool IsManualDieDetectOffsetWithinLimit(double offsetX, double offsetY, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (double.IsNaN(offsetX) || double.IsInfinity(offsetX) ||
                    double.IsNaN(offsetY) || double.IsInfinity(offsetY))
                {
                    reason = "InputStage manual die detect offset is invalid.";
                    return false;
                }

                double limitX = ResolveManualDieDetectOffsetLimitX();
                double limitY = ResolveManualDieDetectOffsetLimitY();
                if (Math.Abs(offsetX) <= limitX && Math.Abs(offsetY) <= limitY)
                {
                    reason = "InputStage manual die detect offset is within limit. offsetX=" + offsetX.ToString("F6") +
                             ", offsetY=" + offsetY.ToString("F6") +
                             ", limitX=" + limitX.ToString("F6") +
                             ", limitY=" + limitY.ToString("F6");
                    return true;
                }

                reason = "InputStage manual die detect offset exceeds limit. offsetX=" + offsetX.ToString("F6") +
                         ", offsetY=" + offsetY.ToString("F6") +
                         ", limitX=" + limitX.ToString("F6") +
                         ", limitY=" + limitY.ToString("F6");
                return false;
            }
            catch (Exception ex)
            {
                reason = "InputStage manual die detect offset limit check failed: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        public double ResolveDieMapFineOffsetLimitX()
        {
            try
            {
                return Config != null && Config.DieMapFineOffsetLimitX > 0.0
                    ? Config.DieMapFineOffsetLimitX
                    : 2.0;
            }
            catch
            {
                return 2.0;
            }
            finally
            {
            }
        }

        public double ResolveDieMapFineOffsetLimitY()
        {
            try
            {
                return Config != null && Config.DieMapFineOffsetLimitY > 0.0
                    ? Config.DieMapFineOffsetLimitY
                    : 2.0;
            }
            catch
            {
                return 2.0;
            }
            finally
            {
            }
        }

        public void ApplyWaferAlignThetaResult(double referenceT, double correctedT, double alignOffsetT)
        {
            try
            {
                HasWaferAlignThetaResult = true;
                WaferAlignReferenceT = referenceT;
                WaferAlignCorrectedT = correctedT;
                WaferAlignOffsetT = alignOffsetT;

                EventLogger.Write(EventKind.Event, "QMC", "IS-ALIGN",
                    "InputStage theta align result applied. referenceT=" + WaferAlignReferenceT.ToString("F6") +
                    ", correctedT=" + WaferAlignCorrectedT.ToString("F6") +
                    ", offsetT=" + WaferAlignOffsetT.ToString("F6"));
            }
            catch (Exception ex)
            {
                RaiseStageAlarm(AlarmSeverity.Error, "IS-ALIGN-T-APPLY", "InputStageUnit.ApplyWaferAlignThetaResult",
                    "Theta align result apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public bool TryResolveWaferAlignThetaTarget(out double targetT)
        {
            targetT = 0.0;
            try
            {
                if (!HasWaferAlignThetaResult)
                    return false;

                if (double.IsNaN(WaferAlignCorrectedT) || double.IsInfinity(WaferAlignCorrectedT))
                    return false;

                targetT = WaferAlignCorrectedT;
                return true;
            }
            catch
            {
                targetT = 0.0;
                return false;
            }
            finally
            {
            }
        }

        public bool IsWaferAlignThetaResultReady(out string reason)
        {
            reason = string.Empty;
            try
            {
                if (!HasWaferAlignThetaResult)
                {
                    reason = "InputStage theta align result is not complete.";
                    return false;
                }

                if (double.IsNaN(WaferAlignReferenceT) ||
                    double.IsInfinity(WaferAlignReferenceT) ||
                    double.IsNaN(WaferAlignCorrectedT) ||
                    double.IsInfinity(WaferAlignCorrectedT) ||
                    double.IsNaN(WaferAlignOffsetT) ||
                    double.IsInfinity(WaferAlignOffsetT))
                {
                    reason = "InputStage theta align value is invalid.";
                    return false;
                }

                string limitReason;
                if (!IsWaferAlignThetaOffsetWithinLimit(WaferAlignOffsetT, out limitReason))
                {
                    reason = limitReason;
                    return false;
                }

                reason = "InputStage theta align result ready. referenceT=" + WaferAlignReferenceT.ToString("F6") +
                         ", correctedT=" + WaferAlignCorrectedT.ToString("F6") +
                         ", offsetT=" + WaferAlignOffsetT.ToString("F6") +
                         ", limit=" + ResolveWaferAlignThetaCorrectionLimit().ToString("F6");
                return true;
            }
            catch (Exception ex)
            {
                reason = "InputStage theta align result ready check failed: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        public void ClearWaferAlignThetaResult()
        {
            HasWaferAlignThetaResult = false;
            WaferAlignReferenceT = 0.0;
            WaferAlignCorrectedT = 0.0;
            WaferAlignOffsetT = 0.0;
        }

        public bool IsWaferAlignThetaInPosition()
        {
            try
            {
                double targetT;
                if (!TryResolveWaferAlignThetaTarget(out targetT))
                    return true;

                if (StageT == null)
                    return false;

                double tolerance = ResolveAxisPositionTolerance(StageT);
                return !StageT.IsMoving &&
                       !StageT.IsAlarm &&
                       Math.Abs(StageT.ActualPosition - targetT) <= tolerance;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public void ApplyDieMappingResult(WaferMapData map, double originX, double originY, double pitchX, double pitchY, double mappingOffsetX = 0.0, double mappingOffsetY = 0.0)
        {
            try
            {
                if (map != null)
                {
                    NormalizeWaferMap(map, map.WaferId);
                    CurrentWaferMap = map;
                }

                OriginX = originX;
                OriginY = originY;
                PitchX = pitchX;
                PitchY = pitchY;
                DieMappingOffsetX = mappingOffsetX;
                DieMappingOffsetY = mappingOffsetY;
                EventLogger.Write(EventKind.Event, "QMC", "IS-DIEMAP",
                    "InputStage die mapping result applied. wafer=" + (map != null ? map.WaferId : "") +
                    ", row=" + (map != null ? map.RowCount.ToString() : "0") +
                    ", col=" + (map != null ? map.ColumnCount.ToString() : "0") +
                    ", originX=" + OriginX.ToString("F4") +
                    ", originY=" + OriginY.ToString("F4") +
                    ", pitchX=" + PitchX.ToString("F4") +
                    ", pitchY=" + PitchY.ToString("F4") +
                    ", offsetX=" + DieMappingOffsetX.ToString("F4") +
                    ", offsetY=" + DieMappingOffsetY.ToString("F4"));
            }
            catch (Exception ex)
            {
                RaiseStageAlarm(AlarmSeverity.Error, "IS-DIEMAP-APPLY", "InputStageUnit.ApplyDieMappingResult",
                    "Die mapping result apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public void SetCurrentWaferMaterial(WaferMaterial wafer)
        {
            CurrentWaferMaterial = wafer;
        }

        public WaferMaterial GetCurrentStageWaferMaterial()
        {
            try
            {
                return CurrentWaferMaterial ?? MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            }
            catch
            {
                return CurrentWaferMaterial;
            }
            finally
            {
            }
        }

        public bool HasWaferOnStage()
        {
            try
            {
                WaferMaterial wafer = GetCurrentStageWaferMaterial();
                return wafer != null &&
                       !string.IsNullOrWhiteSpace(wafer.WaferId) &&
                       WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Empty;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public bool IsInputStageSimulationOrDryRun()
        {
            try
            {
                bool setupSimulation = Setup != null && Setup.IsSimulationMode;
                bool configDryRun = Config != null && Config.bDryRun;
                return setupSimulation || configDryRun;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public WaferMaterial TakeCurrentWaferMaterial()
        {
            WaferMaterial wafer = CurrentWaferMaterial;
            CurrentWaferMaterial = null;
            return wafer;
        }

        public void ClearCurrentWaferMaterial()
        {
            CurrentWaferMaterial = null;
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
            return await WaitForUserConfirmAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<UserConfirmResult> WaitForUserConfirmAsync(CancellationToken ct)
        {
            TaskCompletionSource<UserConfirmResult> tcs =
                new TaskCompletionSource<UserConfirmResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<UserConfirmResult> oldTcs;

            lock (_confirmSync)
            {
                oldTcs = _confirmTcs;
                _confirmTcs = tcs;
            }

            if (oldTcs != null)
                oldTcs.TrySetCanceled();

            CancellationTokenRegistration cancellationRegistration = ct.Register(() => tcs.TrySetCanceled());
            try
            {
                Action requested = UserConfirmRequested;
                if (requested == null)
                    throw new InvalidOperationException("InputStage 사용자 확인 화면 요청을 처리할 UI가 연결되어 있지 않습니다.");

                requested();
                Console.WriteLine(
                    $"[INFO]  '{Name}' - 사용자 컨펌 대기 중... " +
                    "(UI에서 얼라인 결과 확인 후 ConfirmFromUi()를 호출하세요)");

                UserConfirmResult result = await tcs.Task.ConfigureAwait(false);
                if (result == null)
                    result = new UserConfirmResult { IsConfirmed = false };

                if (result.IsConfirmed)
                {
                    // ── 사용자 수정값 적용 ─────────────────────────────────────
                    if (Math.Abs(result.AngleOffset) > 1e-6)
                    {
                        Console.WriteLine(
                            $"[INFO]  '{Name}' - 사용자 Angle 오프셋 적용: {result.AngleOffset:F4}°");
                        double targetT = StageT.ActualPosition + result.AngleOffset;
                        int moveResult = await StageT.MoveRelativeAsync(result.AngleOffset, ResolveAxisFineVelocity(StageT)).ConfigureAwait(false);
                        if (moveResult != 0 || StageT.IsAlarm)
                        {
                            RaiseStageAlarm(AlarmSeverity.Error, "IS-CONFIRM-T", "InputStageUnit.WaitForUserConfirmAsync",
                                "User confirm StageT correction failed. result=" + moveResult + ", alarm=" + StageT.IsAlarm);
                            result.IsConfirmed = false;
                            return result;
                        }

                        moveResult = await WaitInputStageAxisInPosition(WaferStageAxis.WaferT, targetT, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                        if (moveResult != 0)
                        {
                            result.IsConfirmed = false;
                            return result;
                        }
                    }

                    if (Math.Abs(result.StartOffsetX) > 1e-6 || Math.Abs(result.StartOffsetY) > 1e-6)
                    {
                        OriginX += result.StartOffsetX;
                        OriginY += result.StartOffsetY;
                        Console.WriteLine(
                            $"[INFO]  '{Name}' - 시작 위치 오프셋 적용. " +
                            $"새 Origin X={OriginX:F4}mm, Y={OriginY:F4}mm");
                    }

                    Console.WriteLine(
                        $"[INFO]  '{Name}' - 컨펌 완료. " +
                        $"시작 다이 인덱스: {result.StartDieIndex}");
                }
                else
                {
                    Console.WriteLine($"[WARN]  '{Name}' - 사용자가 T Align 재시작을 요청했습니다.");
                }

                return result;
            }
            finally
            {
                cancellationRegistration.Dispose();
                lock (_confirmSync)
                {
                    if (ReferenceEquals(_confirmTcs, tcs))
                        _confirmTcs = null;
                }

                Action ended = UserConfirmWaitEnded;
                if (ended != null)
                    ended();
            }
        }



        // Step 4 : FrontPickerSequence, RearPickerSequence로 옮겨서 구현 예정.
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
        //public async Task<int> MultiScanAndPickupAsync(int startDieIndex = 0)
        //{
        //    try
        //    {
        //        if (CurrentWaferMap == null)
        //            return RaiseStageAlarm(AlarmSeverity.Error, "IS-PICK-MAP", "InputStageUnit.MultiScanAndPickupAsync", "맵 데이터 없음.");

        //        WaferMapData map       = CurrentWaferMap;
        //        int          totalDies = map.RowCount * map.ColumnCount;
        //        int          batchSize = Tpu.PickerCount > 0 ? Tpu.PickerCount : 1;

        //        Console.WriteLine(
        //            $"[INFO]  '{Name}' ? 다이 픽업 시작. " +
        //            $"총 {totalDies}개, 배치 크기: {batchSize}, 시작 인덱스: {startDieIndex}");

        //        int dieIndex = startDieIndex;

        //        while (dieIndex < totalDies)
        //        {
        //            // ── Phase A: 배치 단위 비전 스캔 ──────────────────────────
        //            // Expose 완료만 받고 결과를 기다리지 않아 다음 이동이 즉시 시작된다.
        //            int batchStart = dieIndex;
        //            int batchEnd   = Math.Min(dieIndex + batchSize, totalDies);

        //            for (int i = batchStart; i < batchEnd; i++)
        //            {
        //                int row = i / map.ColumnCount;
        //                int col = i % map.ColumnCount;

        //                if (!map.DieMap[row, col])
        //                {
        //                    Console.WriteLine($"[INFO]  '{Name}' ? 다이 [{i}] NG ? 스캔 스킵.");
        //                    continue;
        //                }

        //                // StageY + CameraX 동시 이동 (비전 촬상 위치)
        //                double targetY = OriginY + row * PitchY;
        //                double targetX = OriginX + col * PitchX;

        //                Task<int> moveY = StageY.MoveAbsoluteAsync(targetY, ResolveAxisVelocity(StageY));
        //                Task<int> moveX = SharedRailXMotionRuntime.MoveAxisAsync(CameraX, targetX, ResolveAxisVelocity(CameraX));
        //                int[] moveResults = await Task.WhenAll(moveY, moveX);

        //                if (moveResults[0] != 0 || moveResults[1] != 0 || StageY.IsAlarm || CameraX.IsAlarm)
        //                {
        //                    return RaiseStageAlarm(
        //                        AlarmSeverity.Error,
        //                        "IS-MOVE",
        //                        "InputStageUnit.MultiScanAndPickupAsync",
        //                        $"Phase A 스캔 이동 후 축 알람 (다이 [{i}], StageY result={moveResults[0]}, CameraX result={moveResults[1]}, StageY.IsAlarm={StageY.IsAlarm}, CameraX.IsAlarm={CameraX.IsAlarm}).");
        //                }

        //                // ── 비전 트리거: Expose 완료만 대기, 결과는 나중에 수집 ──
        //                bool exposed = await Vision.TriggerExposeAsync(i);

        //                if (!exposed)
        //                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-VISION-EXPOSE", "InputStageUnit.MultiScanAndPickupAsync", $"비전 Expose 실패 (다이 [{i}]).");

        //                // Expose 완료 즉시 다음 좌표 이동 ? 비전 분석은 백그라운드에서 진행
        //                Console.WriteLine(
        //                    $"[INFO]  '{Name}' ? 다이 [{i}] Expose 완료. 즉시 다음 좌표로 이동.");
        //            }

        //            // ── Phase B: 배치 단위 픽업 동작 ──────────────────────────
        //            for (int i = batchStart; i < batchEnd; i++)
        //            {
        //                int row = i / map.ColumnCount;
        //                int col = i % map.ColumnCount;

        //                if (!map.DieMap[row, col])
        //                {
        //                    Console.WriteLine($"[INFO]  '{Name}' ? 다이 [{i}] NG ? 픽업 스킵.");
        //                    continue;
        //                }

        //                // ── 비전 결과 수집 (픽업 전 OK 여부 최종 확인) ────────
        //                bool inspOk = await Vision.GetResultAsync(i, ResolveVisionResultTimeoutMs());

        //                if (!inspOk)
        //                {
        //                    Console.WriteLine(
        //                        $"[WARN]  '{Name}' ? 다이 [{i}] 비전 NG 또는 타임아웃 ? 픽업 스킵.");
        //                    continue;
        //                }

        //                // ── TPU 픽커 준비 확인 ─────────────────────────────────
        //                if (!Tpu.IsPickerReady)
        //                {
        //                    Console.WriteLine($"[WARN]  '{Name}' ? 다이 [{i}] TPU 픽커 미준비 ? 픽업 스킵.");
        //                    continue;
        //                }

        //                // ── 픽업 위치로 이동 (스캔 오프셋 + 기구 오프셋 적용) ─
        //                double pickY = OriginY + row * PitchY + Setup.PickerOffsetY;
        //                double pickX = OriginX + col * PitchX + Setup.PickerOffsetX;

        //                Task<int> pickMoveY = StageY.MoveAbsoluteAsync(pickY, ResolveAxisVelocity(StageY));
        //                Task<int> pickMoveX = NeedleBlockX.MoveAbsoluteAsync(pickX, ResolveAxisVelocity(NeedleBlockX));
        //                int[] pickMoveResults = await Task.WhenAll(pickMoveY, pickMoveX);

        //                if (pickMoveResults[0] != 0 || pickMoveResults[1] != 0 || StageY.IsAlarm || NeedleBlockX.IsAlarm)
        //                {
        //                    return RaiseStageAlarm(
        //                        AlarmSeverity.Error,
        //                        "IS-MOVE",
        //                        "InputStageUnit.MultiScanAndPickupAsync",
        //                        $"Phase B 픽업 위치 이동 후 축 알람 (다이 [{i}], StageY result={pickMoveResults[0]}, NeedleBlockX result={pickMoveResults[1]}, StageY.IsAlarm={StageY.IsAlarm}, NeedleBlockX.IsAlarm={NeedleBlockX.IsAlarm}).");
        //                }

        //                // ── 픽업 시퀀스 실행 ───────────────────────────────────
        //                int pickOk = await ExecutePickupAsync(i);

        //                if (pickOk != 0)
        //                    return pickOk;

        //                Console.WriteLine($"[INFO]  '{Name}' ? 다이 [{i}] 픽업 완료.");
        //            }

        //            dieIndex = batchEnd;
        //        }

        //        Console.WriteLine($"[INFO]  '{Name}' ? 모든 다이 픽업 완료.");
        //        return 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        return RaiseStageAlarm(AlarmSeverity.Error, "IS-PICK-EX", "InputStageUnit.MultiScanAndPickupAsync", "MultiScanAndPickup exception: " + ex.Message);
        //    }
        //    finally
        //    {
        //    }
        //}



        // Step 5 : InputLoadSequence로 옮겨서 구현 예정.
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
        //public async Task<int> UnloadWaferAsync()
        //{
        //    try
        //    {
        //        // ── Step 1: 언로딩 위치로 이동 ───────────────────────────────
        //        Console.WriteLine(
        //            $"[INFO]  '{Name}' ? 언로딩 위치({Setup.UnloadPositionY}mm)로 StageY 이동.");
        //        int moveResult = await StageY.MoveAbsoluteAsync(Setup.UnloadPositionY, ResolveAxisVelocity(StageY));

        //        if (moveResult != 0 || StageY.IsAlarm)
        //            return RaiseStageAlarm(AlarmSeverity.Error, "IS-STAGEY", "InputStageUnit.UnloadWaferAsync", $"StageY 이동 실패 (result={moveResult}, axis code={StageY.AlarmCode}).");

        //        // ── Step 2: ExpanderZ Up 이동 (텐션 해제) ────────────────────
        //        Console.WriteLine(
        //            $"[INFO]  '{Name}' ? ExpanderZ Up 위치({Setup.ExpanderUpPosition}mm) 이동. 텐션 해제.");
        //        moveResult = await ExpanderZ.MoveAbsoluteAsync(Setup.ExpanderUpPosition, ResolveAxisVelocity(ExpanderZ));

        //        if (moveResult != 0 || ExpanderZ.IsAlarm)
        //            return RaiseStageAlarm(AlarmSeverity.Error, "IS-EXPZ", "InputStageUnit.UnloadWaferAsync", $"ExpanderZ 이동 실패 (result={moveResult}, axis code={ExpanderZ.AlarmCode}).");

        //        // ── Step 3: 로더에 웨이퍼 교체 요청 신호 전송 ───────────────
        //        // IWaferLoader 인터페이스를 통한 가상 이벤트 발생
        //        Console.WriteLine($"[INFO]  '{Name}' ? 로더 유닛에 웨이퍼 교체(Change) 요청 신호 전송.");
        //        OnWaferChangeRequested();

        //        CurrentWaferMap = null;

        //        Console.WriteLine($"[INFO]  '{Name}' ? 웨이퍼 언로딩 완료. 다음 자재 대기 중.");
        //        return 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        return RaiseStageAlarm(AlarmSeverity.Error, "IS-UNLOAD-EX", "InputStageUnit.UnloadWaferAsync", "UnloadWafer exception: " + ex.Message);
        //    }
        //    finally
        //    {
        //    }
        //}

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
        private void EnsurePositionObjectsForSequence()
        {
            if (Recipe != null)
                Recipe.EnsurePositionObjects();
        }

        private WaferMapData CreateFallbackWaferMap(string waferId)
        {
            return new WaferMapData
            {
                WaferId = string.IsNullOrWhiteSpace(waferId) ? "WAFER-FALLBACK" : waferId,
                RowCount = 1,
                ColumnCount = 1,
                DieMap = new bool[1, 1] { { true } },
                Ref1Row = 0,
                Ref1Col = 0,
                Ref2Row = 0,
                Ref2Col = 0
            };
        }

        private void NormalizeWaferMap(WaferMapData map, string waferId)
        {
            if (map == null)
                return;

            if (string.IsNullOrWhiteSpace(map.WaferId))
                map.WaferId = string.IsNullOrWhiteSpace(waferId) ? "WAFER" : waferId;

            if (map.RowCount <= 0)
                map.RowCount = map.DieMap != null ? map.DieMap.GetLength(0) : 1;
            if (map.ColumnCount <= 0)
                map.ColumnCount = map.DieMap != null ? map.DieMap.GetLength(1) : 1;
            if (map.DieMap == null || map.DieMap.GetLength(0) != map.RowCount || map.DieMap.GetLength(1) != map.ColumnCount)
            {
                map.DieMap = new bool[map.RowCount, map.ColumnCount];
                for (int row = 0; row < map.RowCount; row++)
                {
                    for (int col = 0; col < map.ColumnCount; col++)
                        map.DieMap[row, col] = true;
                }
            }

            map.Ref1Row = ClampIndex(map.Ref1Row, map.RowCount);
            map.Ref1Col = ClampIndex(map.Ref1Col, map.ColumnCount);
            map.Ref2Row = ClampIndex(map.Ref2Row, map.RowCount);
            map.Ref2Col = ClampIndex(map.Ref2Col, map.ColumnCount);
        }

        private static int ClampIndex(int value, int count)
        {
            if (count <= 0)
                return 0;
            if (value < 0)
                return 0;
            if (value >= count)
                return count - 1;
            return value;
        }

        private void SetEstimatedOriginFromCurrentPosition(WaferMapData map)
        {
            PitchX = DefaultEstimatedPitchX;
            PitchY = DefaultEstimatedPitchY;

            int refCol = map != null ? map.Ref1Col : 0;
            int refRow = map != null ? map.Ref1Row : 0;
            OriginX = CameraX.ActualPosition - (refCol * PitchX);
            OriginY = StageY.ActualPosition - (refRow * PitchY);
        }

        private double ResolveFallbackPitchX(VisionAlignResult ref1Result, VisionAlignResult ref2Result)
        {
            if (ref2Result != null && ref2Result.PitchX > 0.0)
                return ref2Result.PitchX;
            if (ref1Result != null && ref1Result.PitchX > 0.0)
                return ref1Result.PitchX;
            return DefaultEstimatedPitchX;
        }

        private double ResolveFallbackPitchY(VisionAlignResult ref1Result, VisionAlignResult ref2Result)
        {
            if (ref2Result != null && ref2Result.PitchY > 0.0)
                return ref2Result.PitchY;
            if (ref1Result != null && ref1Result.PitchY > 0.0)
                return ref1Result.PitchY;
            return DefaultEstimatedPitchY;
        }

        private static bool IsAxisInPosition(BaseAxis axis, double target)
        {
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;
            return Math.Abs(axis.ActualPosition - target) <= tolerance;
        }

        private int ResolveSequenceMoveTimeout()
        {
            return Config != null && Config.SequenceMoveTimeoutMs > 0 ? Config.SequenceMoveTimeoutMs : 10000;
        }

        public Task<int> MoveVisionPointSafelyAsync(double targetX, double targetY, bool bFine = false, string source = null)
        {
            return MoveVisionPointSafelyAsync(
                targetX,
                targetY,
                (axis, target) => MoveInputStageAxis(axis, target, bFine),
                source);
        }

        public Task<int> MoveVisionPointSafelyAsync(double targetX, double targetY, JogSpeedType speedType, double customSpeed, string source = null)
        {
            return MoveVisionPointSafelyAsync(
                targetX,
                targetY,
                (axis, target) => MoveInputStageAxis(axis, target, speedType, customSpeed),
                source);
        }

        /// <summary>
        /// Review 화면의 Map 절대좌표 이동을 READY 시퀀스 속도 퍼센트로 실행합니다.
        /// 전역 READY Scope는 Auto 축에도 영향을 줄 수 있으므로 열지 않고, 각 축의 원본
        /// DefaultVelocity/Acceleration/Deceleration에 현재 ReadySequencePercent를 직접 적용합니다.
        /// </summary>
        public Task<int> MoveVisionPointSafelyAtReadySequenceSpeedAsync(
            double targetX,
            double targetY,
            string source = null)
        {
            double readyPercent = MotionSpeedScale.ReadySequencePercent;
            return MoveVisionPointSafelyAsync(
                targetX,
                targetY,
                (axis, target) => MoveInputStageAxisAtDefaultSpeedPercentAsync(
                    axis,
                    target,
                    readyPercent),
                source);
        }

        private Task<int> MoveInputStageAxisAtDefaultSpeedPercentAsync(
            WaferStageAxis axis,
            double target,
            double speedPercent)
        {
            BaseAxis item = ResolveInputStageAxis(axis);
            if (item == null || item.Config == null)
                return MoveInputStageAxisCommandWithMotion(axis, target, 0.0, 0.0, 0.0);

            double rawVelocity = item.Config.GetRawDefaultVelocity();
            double rawAcceleration = item.Config.GetRawAcceleration();
            double rawDeceleration = item.Config.GetRawDeceleration();

            double velocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                rawVelocity > 0.0 ? rawVelocity : 100.0,
                speedPercent);
            double acceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                rawAcceleration > 0.0 ? rawAcceleration : 100.0,
                speedPercent);
            double deceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                rawDeceleration > 0.0 ? rawDeceleration : 100.0,
                speedPercent);

            return MoveInputStageAxisCommandWithMotion(
                axis,
                target,
                velocity,
                acceleration,
                deceleration);
        }

        private async Task<int> MoveVisionPointSafelyAsync(double targetX, double targetY, Func<WaferStageAxis, double, Task<int>> moveAxisAsync, string source = null)
        {
            try
            {
                string moveSource = string.IsNullOrWhiteSpace(source)
                    ? "InputStageUnit.MoveVisionPointSafelyAsync"
                    : source;

                // 현재 기준: VisionX는 작업영역 원형 경계와 무관하고, StageY는 NeedleX/StageY/NeedleZ 인터락에서 판단한다.
                int result = await moveAxisAsync(WaferStageAxis.WaferY, targetY).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await moveAxisAsync(WaferStageAxis.VisionX, targetX).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return RaiseStageAlarm(AlarmSeverity.Error, "IN-STAGE-VISION-EX",
                    string.IsNullOrWhiteSpace(source) ? "InputStageUnit.MoveVisionPointSafelyAsync" : source,
                    "Vision point safe move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>
        /// 지정한 다이 좌표로 StageY와 CameraX를 동시에 이동한다.
        /// </summary>
        /// <param name="row">대상 다이의 행 인덱스</param>
        /// <param name="col">대상 다이의 열 인덱스</param>
        /// <param name="useEstimate">
        /// true이면 원점/피치 미수립 상태에서 Recipe 기본 피치로 좌표를 추정한다.<br/>
        /// false이면 수립된 <see cref="OriginX"/>, <see cref="OriginY"/>, <see cref="PitchX"/>, <see cref="PitchY"/>를 사용한다.
        /// </param>
        private async Task<int> MoveToDieAsync(int row, int col, bool useEstimate = false, bool bFine = false)
        {
            try
            {
                double pitchX = useEstimate ? DefaultEstimatedPitchX : PitchX;
                double pitchY = useEstimate ? DefaultEstimatedPitchY : PitchY;
                double origX  = useEstimate ? 0.0               : OriginX;
                double origY  = useEstimate ? 0.0               : OriginY;

                double targetX = origX + col * pitchX;
                double targetY = origY + row * pitchY;

                // 현재 기준: 다이 Vision 좌표는 CameraX 작업 원으로 차단하지 않고 StageY 축 인터락에서 안전성을 확인한다.
                return await MoveVisionPointSafelyAsync(targetX, targetY, bFine, "InputStageUnit.MoveToDieAsync").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return RaiseStageAlarm(AlarmSeverity.Error, "IS-MOVE-DIE-EX", "InputStageUnit.MoveToDieAsync", "MoveToDie exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveNeedleZAvoidForNonProcessMoveAsync(bool bFine, string source)
        {
            try
            {
                if (NeedleZ == null || Recipe == null)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-NEEDLEZ-AVOID", source,
                        "NeedleZ avoid move requires axis/recipe information.");

                Recipe.EnsurePositionObjects();
                if (Recipe.NeedleZ == null)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-NEEDLEZ-AVOID", source,
                        "NeedleZ avoid move requires NeedleZ recipe information.");

                double target = Recipe.NeedleZ.AvoidPosition;
                if (NeedleZ.IsAtTargetPosition(target, 0.0))
                    return 0;

                int result = await MoveInputStageAxis(WaferStageAxis.NeedleZ, target, bFine).ConfigureAwait(false);
                if (result != 0 || NeedleZ.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-NEEDLEZ-AVOID", source,
                        "NeedleZ avoid move before non-process move failed. result=" + result +
                        ", alarm=" + NeedleZ.IsAlarm +
                        ", actual=" + (NeedleZ != null ? NeedleZ.ActualPosition.ToString("F3") : "null") +
                        ", target=" + target.ToString("F3"));

                result = await WaitInputStageAxisInPosition(WaferStageAxis.NeedleZ, target, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return 0;
            }
            catch (Exception ex)
            {
                return RaiseStageAlarm(AlarmSeverity.Error, "IS-NEEDLEZ-AVOID-EX", source,
                    "NeedleZ avoid move before non-process move exception: " + ex.Message);
            }
        }

        private async Task<int> MoveLoadPreparationAxisAsync(
            WaferStageAxis axis,
            double target,
            BaseAxis axisState,
            string description,
            string alarmCode,
            bool bFine)
        {
            try
            {
                int result = await MoveInputStageAxis(axis, target, bFine).ConfigureAwait(false);
                bool axisAlarm = axisState != null && axisState.IsAlarm;
                if (result != 0 || axisAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, alarmCode, "InputStageUnit.LoadAndPrepareWaferAsync",
                        description + " move before load failed. result=" + result +
                        ", alarm=" + axisAlarm +
                        ", actual=" + (axisState != null ? axisState.ActualPosition.ToString("F3") : "null") +
                        ", target=" + target.ToString("F3"));

                result = await WaitInputStageAxisInPosition(axis, target, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return 0;
            }
            catch (Exception ex)
            {
                return RaiseStageAlarm(AlarmSeverity.Error, alarmCode + "-EX", "InputStageUnit.LoadAndPrepareWaferAsync",
                    description + " move before load exception: " + ex.Message);
            }
        }

        private async Task<int> MoveUnloadSafeAxisAsync(
            WaferStageAxis axis,
            double target,
            BaseAxis axisState,
            string description,
            string alarmCode,
            bool bFine)
        {
            try
            {
                int result = await MoveInputStageAxis(axis, target, bFine).ConfigureAwait(false);
                bool axisAlarm = axisState != null && axisState.IsAlarm;
                if (result != 0 || axisAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, alarmCode, "InputStageUnit.PrepareUnloadWaferAsync",
                        description + " move before unload failed. result=" + result +
                        ", alarm=" + axisAlarm +
                        ", actual=" + (axisState != null ? axisState.ActualPosition.ToString("F3") : "null") +
                        ", target=" + target.ToString("F3"));

                result = await WaitInputStageAxisInPosition(axis, target, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return 0;
            }
            catch (Exception ex)
            {
                return RaiseStageAlarm(AlarmSeverity.Error, alarmCode + "-EX", "InputStageUnit.PrepareUnloadWaferAsync",
                    description + " move before unload exception: " + ex.Message);
            }
        }


        // ExecutePickupAsync : FrontPickerSequence, RearPickerSequence로 옮겨서 구현 예정.
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
        //private async Task<int> ExecutePickupAsync(int dieIndex)
        //{
        //    try
        //    {
        //        // ── 1. NeedleVacuum On ─────────────────────────────────────────
        //        NeedleVacuum.On();
        //        await Task.Delay(ResolveNeedleVacuumSettleMs()).ContinueWith(_ => { });

        //        // ── 2. TPU 픽업 가능 신호 전송 ────────────────────────────────
        //        Tpu.NotifyPickReady(dieIndex);

        //        // ── 3. NeedleZ 상승 (Eject) ───────────────────────────────────
        //        int moveResult = await NeedleZ.MoveAbsoluteAsync(Setup.NeedleEjectPosition, ResolveAxisVelocity(NeedleZ));

        //        if (moveResult != 0 || NeedleZ.IsAlarm)
        //            return RaiseStageAlarm(AlarmSeverity.Error, "IS-MOVE", "InputStageUnit.ExecutePickupAsync", $"NeedleZ 상승(Eject) 실패 (다이 [{dieIndex}], result={moveResult}, axis code={NeedleZ.AlarmCode}).");

        //        // ── 4. TPU 픽커 상승 완료 대기 ───────────────────────────────
        //        bool pickerUp = await Tpu.WaitPickerUpAsync(ResolvePickerUpTimeoutMs());

        //        if (!pickerUp)
        //        {
        //            Console.WriteLine(
        //                $"[ALARM] '{Name}' ? ExecutePickup: TPU 픽커 상승 타임아웃 (다이 [{dieIndex}]).");
        //            // 안전을 위해 NeedleZ 하강 + 진공 해제
        //            await NeedleZ.MoveAbsoluteAsync(Setup.NeedleDownPosition, ResolveAxisVelocity(NeedleZ));
        //            return RaiseStageAlarm(AlarmSeverity.Error, "IS-PICKER-UP", "InputStageUnit.ExecutePickupAsync", $"TPU 픽커 상승 타임아웃 (다이 [{dieIndex}]).");
        //        }

        //        // ── 5. NeedleZ 하강 + NeedleVacuum Off ───────────────────────
        //        moveResult = await NeedleZ.MoveAbsoluteAsync(Setup.NeedleDownPosition, ResolveAxisVelocity(NeedleZ));

        //        if (moveResult != 0 || NeedleZ.IsAlarm)
        //            return RaiseStageAlarm(AlarmSeverity.Error, "IS-MOVE", "InputStageUnit.ExecutePickupAsync", $"NeedleZ 하강 실패 (다이 [{dieIndex}], result={moveResult}, axis code={NeedleZ.AlarmCode}).");

        //        return 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        return RaiseStageAlarm(AlarmSeverity.Error, "IS-PICK-EX", "InputStageUnit.ExecutePickupAsync", "ExecutePickup exception: " + ex.Message);
        //    }
        //    finally
        //    {
        //        NeedleVacuum.Off();
        //    }
        //}

        // 이동 실패 보고: 인터락/공유레일 차단(result == -11)은 하위 가드가 이미 동일 사유로
        // 알람 1회 + 로그를 남겼으므로 여기서 중복 알람을 올리지 않고 이벤트 로그만 남긴다.
        // 비-인터락 실패(타임아웃·축알람 등)만 유닛 알람으로 보고한다.
        private int ReportStageMoveFailure(string code, int result, string message)
        {
            if (result == -11)
            {
                EventLogger.Write(EventKind.Event, "QMC", code + "-BLOCKED", Name, message);
                return result;
            }
            return RaiseStageAlarm(AlarmSeverity.Error, code, Name, message);
        }

        private int RaiseStageAlarm(AlarmSeverity severity, string code, string source, string message)
        {
            try
            {
                Console.WriteLine($"[ALARM] '{Name}' ? {message}");
                // AlarmManager.Raise가 이벤트 로그(EventKind.Alarm)를 기록하므로 직접 기록 생략(이벤트 로그 중복 방지)
                AlarmManager.Raise(severity, code, source: source, message: message);
            }
            catch
            {
            }
            finally
            {
            }

            return -1;
        }
    }
}

