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
using System.Collections.Generic;
using System.Runtime.Serialization;

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
        [DataMember] public bool IsSimulationMode { get; set; } = false;

        [DataMember] public double SafetyRadius { get; set; } = 0.0;

        [DataMember] public double WorkAreaRadius { get; set; } = 150.0;

        [DataMember] public double NeedleWorkAreaRadius { get; set; } = 125.0;

        [DataMember] public double WorkAreaCenterX { get; set; } = 0.0;

        [DataMember] public double WorkAreaCenterY { get; set; } = 0.0;

        [DataMember] public double NeedleWorkAreaCenterX { get; set; } = 0.0;

        [DataMember] public double NeedleWorkAreaCenterY { get; set; } = 0.0;

        [DataMember] public double NeedleXToVisionXOffset { get; set; } = 0.0;

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

        public bool IsSimulationMode
        {
            get { return bDryRun; }
            set { bDryRun = value; }
        }

        /// <summary>얼라인 반복 촬상 최대 횟수.</summary>
        [DataMember] public int MaxAlignIterations { get; set; } = 3;

        /// <summary>얼라인 수렴 임계값 [deg]. 이 값 이하이면 반복을 종료한다.</summary>
        [DataMember] public double AlignConvergenceThresholdDeg { get; set; } = 0.005;

        [DataMember] public int SequenceMoveTimeoutMs { get; set; } = 10000;
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
        [DataMember] public string VisionTargetId { get; set; } = "DieMapMark";
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
            if (string.IsNullOrWhiteSpace(VisionTargetId)) VisionTargetId = "DieMapMark";
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
            return axis != null && axis.Config != null && axis.Config.DefaultVelocity > 0.0
                ? axis.Config.DefaultVelocity
                : 100.0;
        }

        private static double ResolveAxisFineVelocity(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.JogFineVelocity > 0.0
                ? axis.Config.JogFineVelocity
                : ResolveAxisVelocity(axis);
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
            return Math.Abs(NeedleZ.ActualPosition) <= tolerance ||
                   Math.Abs(NeedleZ.ActualPosition - Recipe.NeedleZ.AvoidPosition) <= tolerance;
        }

        public double ResolveWorkAreaCenterX()
        {
            if (Setup != null && Math.Abs(Setup.WorkAreaCenterX) > 1e-9)
                return Setup.WorkAreaCenterX;

            if (Recipe != null)
            {
                Recipe.EnsurePositionObjects();
                return Recipe.VisionX.ProcessPosition;
            }

            return 0.0;
        }

        public double ResolveWorkAreaCenterY()
        {
            if (Setup != null && Math.Abs(Setup.WorkAreaCenterY) > 1e-9)
                return Setup.WorkAreaCenterY;

            if (Recipe != null)
            {
                Recipe.EnsurePositionObjects();
                return Recipe.WaferY.ProcessPosition;
            }

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
            if (Setup != null && Math.Abs(Setup.NeedleWorkAreaCenterX) > 1e-9)
                return Setup.NeedleWorkAreaCenterX;

            return ResolveWorkAreaCenterX() - (Setup != null ? Setup.NeedleXToVisionXOffset : 0.0);
        }

        public double ResolveNeedleWorkAreaCenterY()
        {
            if (Setup != null && Math.Abs(Setup.NeedleWorkAreaCenterY) > 1e-9)
                return Setup.NeedleWorkAreaCenterY;

            return ResolveWorkAreaCenterY();
        }

        public double ConvertNeedleXToVisionX(double needleX)
        {
            return needleX + (Setup != null ? Setup.NeedleXToVisionXOffset : 0.0);
        }

        public bool IsInputStageWorkPointInArea(double visionX, double stageY, out string reason)
        {
            return IsWorkPointInArea(visionX, stageY, ResolveWorkAreaRadius(), "InputStage work area", out reason);
        }

        public bool IsNeedleWorkPointInArea(double needleX, double stageY, out string reason)
        {
            return IsNeedlePointInArea(needleX, stageY, ResolveNeedleWorkAreaRadius(), "Needle work area", out reason);
        }

        public bool IsInputStageAxisTargetAllowedInWorkArea(WaferStageAxis axis, double target, out string reason)
        {
            reason = string.Empty;
            if (Recipe == null)
                return true;

            Recipe.EnsurePositionObjects();

            if ((axis == WaferStageAxis.WaferY || axis == WaferStageAxis.VisionX) &&
                IsStageTravelTeachingTarget(axis, target))
            {
                if (IsProcessTeachingTarget(axis, target))
                    return true;

                return VerifyNeedleZSafeForNonProcessTarget(axis, target, out reason);
            }

            if (IsSafeTeachingTarget(axis, target))
                return VerifyNeedleZSafeForNonProcessTarget(axis, target, out reason);

            if (axis == WaferStageAxis.WaferY)
            {
                double targetX = CameraX != null ? CameraX.ActualPosition : ResolveWorkAreaCenterX();
                if (!IsInputStageWorkPointInArea(targetX, target, out reason))
                    return false;

                if (!IsNeedleZInSafePosition())
                {
                    double needleX = NeedleBlockX != null ? NeedleBlockX.ActualPosition : Recipe.NeedleX.ProcessPosition;
                    return IsNeedleWorkPointInArea(needleX, target, out reason);
                }

                return true;
            }

            if (axis == WaferStageAxis.VisionX)
            {
                double targetY = StageY != null ? StageY.ActualPosition : ResolveWorkAreaCenterY();
                return IsInputStageWorkPointInArea(target, targetY, out reason);
            }

            if (axis == WaferStageAxis.NeedleX)
            {
                if (IsNeedleZInHomeOrSafePosition())
                    return true;

                double targetY = StageY != null ? StageY.ActualPosition : ResolveWorkAreaCenterY();
                return IsNeedleWorkPointInArea(target, targetY, out reason);
            }

            if (axis == WaferStageAxis.NeedleZ || axis == WaferStageAxis.EjectPinZ)
            {
                double needleX = NeedleBlockX != null ? NeedleBlockX.ActualPosition : Recipe.NeedleX.ProcessPosition;
                double stageY = StageY != null ? StageY.ActualPosition : ResolveWorkAreaCenterY();
                return IsNeedleWorkPointInArea(needleX, stageY, out reason);
            }

            if (axis == WaferStageAxis.WaferT || axis == WaferStageAxis.WaferExpandingZ)
            {
                double visionX = CameraX != null ? CameraX.ActualPosition : ResolveWorkAreaCenterX();
                double stageY = StageY != null ? StageY.ActualPosition : ResolveWorkAreaCenterY();
                return IsInputStageWorkPointInArea(visionX, stageY, out reason);
            }

            return true;
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
                double stageY = StageY != null ? StageY.ActualPosition : ResolveWorkAreaCenterY();
                double visionTarget;
                if (!TryResolveCircularJogTarget(
                    motionAxis.ActualPosition,
                    stageY,
                    ResolveWorkAreaCenterX(),
                    ResolveWorkAreaCenterY(),
                    ResolveWorkAreaRadius(),
                    direction,
                    "InputStage work area",
                    out visionTarget,
                    out reason))
                    return false;

                target = ClampToSoftLimit(motionAxis, visionTarget);
                return VerifyResolvedJogTarget(axis, direction, target, out reason);
            }

            if (axis == WaferStageAxis.WaferY)
            {
                double visionX = CameraX != null ? CameraX.ActualPosition : ResolveWorkAreaCenterX();
                double stageYTarget;
                if (!TryResolveCircularJogTarget(
                    motionAxis.ActualPosition,
                    visionX,
                    ResolveWorkAreaCenterY(),
                    ResolveWorkAreaCenterX(),
                    ResolveWorkAreaRadius(),
                    direction,
                    "InputStage work area",
                    out stageYTarget,
                    out reason))
                    return false;

                target = ClampToSoftLimit(motionAxis, stageYTarget);
                return VerifyResolvedJogTarget(axis, direction, target, out reason);
            }

            if (axis == WaferStageAxis.NeedleX)
            {
                if (IsNeedleZInHomeOrSafePosition())
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
                    return VerifyJogDirectionTarget(axis, direction, target, out reason);
                }

                target = ClampToSoftLimit(motionAxis, direction == Direction.Plus
                    ? motionAxis.Setup.SoftLimitPlus
                    : motionAxis.Setup.SoftLimitMinus);
                return VerifyJogDirectionTarget(axis, direction, target, out reason);
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
                if (direction != Direction.Minus)
                {
                    reason = "Needle work area jog plus moves farther outside circular boundary. x=" +
                        needleActual.ToString("F3") +
                        ", max=" + maxNeedleX.ToString("F3") +
                        ", centerX=" + centerX.ToString("F3") +
                        ", radius=" + radius.ToString("F3");
                    return false;
                }

                target = ClampToSoftLimit(NeedleBlockX, maxNeedleX);
                return VerifyJogDirectionTarget(WaferStageAxis.NeedleX, direction, target, out reason);
            }

            if (outsideMinus)
            {
                if (direction != Direction.Plus)
                {
                    reason = "Needle work area jog minus moves farther outside circular boundary. x=" +
                        needleActual.ToString("F3") +
                        ", min=" + minNeedleX.ToString("F3") +
                        ", centerX=" + centerX.ToString("F3") +
                        ", radius=" + radius.ToString("F3");
                    return false;
                }

                target = ClampToSoftLimit(NeedleBlockX, minNeedleX);
                return VerifyJogDirectionTarget(WaferStageAxis.NeedleX, direction, target, out reason);
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

            if (IsNeedleZInSafePosition())
                return true;

            reason = "NeedleZ must be at Avoid position before moving to non-process position. " +
                "axis=" + axis +
                ", target=" + target.ToString("F3") +
                ", needleZActual=" + (NeedleZ != null ? NeedleZ.ActualPosition.ToString("F3") : "null") +
                ", needleZAvoid=" + (Recipe != null ? Recipe.NeedleZ.AvoidPosition.ToString("F3") : "null") +
                ", tolerance=" + ResolveNeedleZInPositionTolerance().ToString("F3");
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

            double signedDistance = (direction < 0 ? -1.0 : 1.0) * Math.Abs(axisStepDistance);
            double target = axis.ActualPosition + signedDistance;

            WaferStageAxis stageAxis;
            if (TryResolveInputStageAxis(axis, out stageAxis))
            {
                int result = await MoveInputStageAxis(stageAxis, target, speedType == JogSpeedType.Fine).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await WaitInputStageAxisInPosition(stageAxis, target, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
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

            double speed = UnitJogVelocityResolver.Resolve(axis, speedType, customSpeed);
            Direction dir = direction < 0 ? Direction.Minus : Direction.Plus;

            WaferStageAxis stageAxis;
            if (TryResolveInputStageAxis(axis, out stageAxis))
                return Task.FromResult(ManualMoveInputStageAxisJog(stageAxis, dir, speed));

            return Task.FromResult(0);
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
            try
            {
                BaseAxis item = ResolveInputStageAxis(axis);
                if (item == null)
                {
                    LastStageMoveFailureMessage = axis + " move failed. axis is null. target=" + targetPos;
                    return RaiseStageAlarm(AlarmSeverity.Error, "IN-STAGE-MOVE", Name, LastStageMoveFailureMessage);
                }

                double tolerance = ResolveAxisPositionTolerance(item);
                if (!item.IsMoving && Math.Abs(item.ActualPosition - targetPos) <= tolerance)
                {
                    LastStageMoveFailureMessage = string.Empty;
                    return 0;
                }

                string interlockReason;
                if (!MotionGuardRuntime.VerifyAxisMove(item, targetPos, out interlockReason))
                {
                    string message = axis + " move blocked by interlock. target=" + targetPos + ". " + interlockReason;
                    LastStageMoveFailureMessage = message;
                    return RaiseStageAlarm(
                        AlarmSeverity.Error,
                        "IN-STAGE-MOVE-INTERLOCK",
                        Name,
                        message);
                }

                double velocity = ResolveInputStageMoveVelocity(axis, bFine);
                int result = await SharedRailXMotionRuntime.MoveAxisAsync(item, targetPos, velocity).ConfigureAwait(false);
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
                    return RaiseStageAlarm(AlarmSeverity.Error, "IN-STAGE-MOVE", Name, message);
                }

                AxisMoveWaitResult waitResult = await WaitInputStageAxisInPositionResult(
                    axis,
                    targetPos,
                    ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (!waitResult.Success)
                {
                    string message = axis + " move/in-position wait failed. target=" + targetPos + ". " +
                        AxisMoveWaiter.FormatResult(waitResult, axis.ToString());
                    LastStageMoveFailureMessage = message;
                    return RaiseStageAlarm(
                        AlarmSeverity.Error,
                        AxisMoveWaiter.ResolveAlarmCode("IN-STAGE-MOVE", waitResult),
                        Name,
                        message);
                }

                LastStageMoveFailureMessage = string.Empty;
                return 0;
            }
            catch (Exception ex)
            {
                LastStageMoveFailureMessage = axis + " move exception. target=" + targetPos + ". " + ex.Message;
                return RaiseStageAlarm(AlarmSeverity.Warning, "IN-STAGE-MOVE", Name, LastStageMoveFailureMessage);
            }
            finally
            {
            }
        }

        public async Task<int> WaitInputStageAxisInPosition(WaferStageAxis axis, double targetPos, int timeoutMs)
        {
            try
            {
                AxisMoveWaitResult waitResult = await WaitInputStageAxisInPositionResult(axis, targetPos, timeoutMs).ConfigureAwait(false);
                if (waitResult.Success)
                    return 0;

                return RaiseStageAlarm(
                    AlarmSeverity.Error,
                    AxisMoveWaiter.ResolveAlarmCode("IN-STAGE-MOVE", waitResult),
                    Name,
                    axis + " move/in-position wait failed. " +
                    AxisMoveWaiter.FormatResult(waitResult, axis.ToString()));
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "IN-STAGE-MOVE-WAIT", Name, ex.Message);
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

        public async Task<AxisMoveWaitResult> WaitInputStageAxisInPositionResult(WaferStageAxis axis, double targetPos, int timeoutMs)
        {
            BaseAxis item = ResolveInputStageAxis(axis);
            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;
            return await AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                item,
                targetPos,
                tolerance,
                timeoutMs > 0 ? timeoutMs : 10000,
                0).ConfigureAwait(false);
        }

        public int ManualMoveInputStageAxisJog(WaferStageAxis axis, Direction dir, double speed)
        {
            BaseAxis item = ResolveInputStageAxis(axis);
            if (axis == WaferStageAxis.NeedleZ || axis == WaferStageAxis.EjectPinZ)
            {
                StartContinuousJogVelocity(item, dir, speed);
                LastStageMoveFailureMessage = string.Empty;
                return 0;
            }

            double target;
            string interlockReason;
            if (!TryResolveInputStageContinuousJogTarget(axis, dir, out target, out interlockReason))
            {
                string message = axis + " jog blocked by work area interlock. direction=" + dir + ". " + interlockReason;
                LastStageMoveFailureMessage = message;
                AlarmManager.Raise(AlarmSeverity.Warning, "IN-STAGE-JOG-INTERLOCK", Name, message);
                return -1;
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

                double guardTarget = axis.Setup != null
                    ? (direction == Direction.Plus ? axis.Setup.SoftLimitPlus : axis.Setup.SoftLimitMinus)
                    : axis.ActualPosition;

                // Needle/Eject Z continuous jog is a recovery/manual operation. Let the
                // axis controller enforce the jog limit while motion guard skips work-area checks.
                using (MotionGuardRuntime.BeginAxisTeachingMove(axis, guardTarget, ContinuousJogTargetName))
                    axis.MoveJogContinuous((int)direction, JogSpeedType.Custom, speed);
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "IN-STAGE-JOG-EX", Name, ex.Message);
            }
            finally
            {
            }
        }

        private void StartBoundedJogMoveAsync(BaseAxis axis, double target, double speed)
        {
            try
            {
                // Continuous jog is dispatched as an absolute bounded move, so tag it
                // for interlock rules that must allow manual recovery movement.
                Task<int> moveTask;
                using (MotionGuardRuntime.BeginAxisTeachingMove(axis, target, ContinuousJogTargetName))
                    moveTask = SharedRailXMotionRuntime.MoveAxisAsync(axis, target, speed);

                moveTask.ContinueWith(t =>
                {
                    try
                    {
                        if (t.IsFaulted && t.Exception != null)
                        {
                            AlarmManager.Raise(
                                AlarmSeverity.Warning,
                                "IN-STAGE-JOG-EX",
                                Name,
                                "InputStage bounded jog exception: " + t.Exception.GetBaseException().Message);
                        }
                    }
                    catch
                    {
                    }
                });
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "IN-STAGE-JOG-EX", Name, ex.Message);
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

        public async Task<int> LoadAndPrepareWaferAsync(string waferId, bool requireMapData, bool bFine = false)
        {
            try
            {
                EnsurePositionObjectsForSequence();

                int result = await MoveNeedleZAvoidForNonProcessMoveAsync(bFine, "InputStageUnit.LoadAndPrepareWaferAsync").ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageAxis(WaferStageAxis.WaferY, Recipe.WaferY.LoadPosition, bFine).ConfigureAwait(false);
                if (result != 0 || StageY.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-LOAD-Y", "InputStageUnit.LoadAndPrepareWaferAsync",
                        "StageY load position move failed. result=" + result + ", alarm=" + StageY.IsAlarm);

                result = await WaitInputStageAxisInPosition(WaferStageAxis.WaferY, Recipe.WaferY.LoadPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageAxis(WaferStageAxis.WaferExpandingZ, Recipe.WaferZ.LoadPosition, bFine).ConfigureAwait(false);
                if (result != 0 || ExpanderZ.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-LOAD-Z", "InputStageUnit.LoadAndPrepareWaferAsync",
                        "ExpanderZ load position move failed. result=" + result + ", alarm=" + ExpanderZ.IsAlarm);

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
                        return RaiseStageAlarm(AlarmSeverity.Warning, "IS-MAP", "InputStageUnit.LoadAndPrepareWaferAsync",
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

                result = await MoveToDieAsync(centerRow, centerCol, true).ConfigureAwait(false);
                if (result != 0) return result;

                for (int iter = 0; iter < Config.MaxAlignIterations; iter++)
                {
                    VisionAlignResult alignResult = await Vision.TriggerAlignAsync("Center").ConfigureAwait(false);
                    if (alignResult == null)
                        return RaiseStageAlarm(AlarmSeverity.Warning, "IS-ALIGN", "InputStageUnit.VisionAlignAndSetupOriginAsync",
                            "Center vision align failed. iteration=" + (iter + 1));

                    double targetT = StageT.ActualPosition + alignResult.DeltaTheta;
                    int thetaResult = await StageT.MoveRelativeAsync(alignResult.DeltaTheta, ResolveAxisFineVelocity(StageT)).ConfigureAwait(false);
                    if (thetaResult != 0 || StageT.IsAlarm)
                        return RaiseStageAlarm(AlarmSeverity.Warning, "IS-ALIGN-T", "InputStageUnit.VisionAlignAndSetupOriginAsync",
                            "StageT correction failed. result=" + thetaResult + ", alarm=" + StageT.IsAlarm);

                    int thetaWaitResult = await WaitInputStageAxisInPosition(WaferStageAxis.WaferT, targetT, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                    if (thetaWaitResult != 0)
                        return thetaWaitResult;

                    if (Math.Abs(alignResult.DeltaTheta) < Config.AlignConvergenceThresholdDeg)
                        break;
                }

                result = await MoveToDieAsync(map.Ref1Row, map.Ref1Col, true).ConfigureAwait(false);
                if (result != 0) return result;

                VisionAlignResult ref1Result = await Vision.TriggerAlignAsync("Ref1").ConfigureAwait(false);
                if (ref1Result == null)
                    return RaiseStageAlarm(AlarmSeverity.Warning, "IS-ALIGN-REF1", "InputStageUnit.VisionAlignAndSetupOriginAsync",
                        "Ref1 vision align failed.");

                double ref1X = CameraX.ActualPosition + ref1Result.DeltaX;
                double ref1Y = StageY.ActualPosition + ref1Result.DeltaY;

                result = await MoveToDieAsync(map.Ref2Row, map.Ref2Col, true).ConfigureAwait(false);
                if (result != 0) return result;

                VisionAlignResult ref2Result = await Vision.TriggerAlignAsync("Ref2").ConfigureAwait(false);
                if (ref2Result == null)
                    return RaiseStageAlarm(AlarmSeverity.Warning, "IS-ALIGN-REF2", "InputStageUnit.VisionAlignAndSetupOriginAsync",
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

        public async Task<int> PrepareUnloadWaferAsync(bool bFine = false)
        {
            try
            {
                EnsurePositionObjectsForSequence();

                int result = await MoveNeedleZAvoidForNonProcessMoveAsync(bFine, "InputStageUnit.PrepareUnloadWaferAsync").ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageAxis(WaferStageAxis.WaferY, Recipe.WaferY.UnloadPosition, bFine).ConfigureAwait(false);
                if (result != 0 || StageY.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-UNLOAD-Y", "InputStageUnit.PrepareUnloadWaferAsync",
                        "StageY unload position move failed. result=" + result + ", alarm=" + StageY.IsAlarm);

                result = await WaitInputStageAxisInPosition(WaferStageAxis.WaferY, Recipe.WaferY.UnloadPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageAxis(WaferStageAxis.WaferExpandingZ, Recipe.WaferZ.UnloadPosition, bFine).ConfigureAwait(false);
                if (result != 0 || ExpanderZ.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-UNLOAD-Z", "InputStageUnit.PrepareUnloadWaferAsync",
                        "ExpanderZ unload position move failed. result=" + result + ", alarm=" + ExpanderZ.IsAlarm);

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
                RaiseStageAlarm(AlarmSeverity.Warning, "IS-MAP-ALIGN", "InputStageUnit.EnsureWaferMapForAlign",
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
                RaiseStageAlarm(AlarmSeverity.Warning, "IS-ALIGN-APPLY", "InputStageUnit.ApplyWaferAlignResult",
                    "Align result apply failed: " + ex.Message);
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
                RaiseStageAlarm(AlarmSeverity.Warning, "IS-DIEMAP-APPLY", "InputStageUnit.ApplyDieMappingResult",
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
                    double targetT = StageT.ActualPosition + result.AngleOffset;
                    int moveResult = await StageT.MoveRelativeAsync(result.AngleOffset, ResolveAxisFineVelocity(StageT)).ConfigureAwait(false);
                    if (moveResult != 0 || StageT.IsAlarm)
                    {
                        RaiseStageAlarm(AlarmSeverity.Warning, "IS-CONFIRM-T", "InputStageUnit.WaitForUserConfirmAsync",
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
        //            return RaiseStageAlarm(AlarmSeverity.Warning, "IS-PICK-MAP", "InputStageUnit.MultiScanAndPickupAsync", "맵 데이터 없음.");

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

        /// <summary>
        /// 지정한 다이 좌표로 StageY와 CameraX를 동시에 이동한다.
        /// </summary>
        /// <param name="row">대상 다이의 행 인덱스</param>
        /// <param name="col">대상 다이의 열 인덱스</param>
        /// <param name="useEstimate">
        /// true이면 원점/피치 미수립 상태에서 Recipe 기본 피치로 좌표를 추정한다.<br/>
        /// false이면 수립된 <see cref="OriginX"/>, <see cref="OriginY"/>, <see cref="PitchX"/>, <see cref="PitchY"/>를 사용한다.
        /// </param>
        private async Task<int> MoveToDieAsync(int row, int col, bool useEstimate = false)
        {
            try
            {
                double pitchX = useEstimate ? DefaultEstimatedPitchX : PitchX;
                double pitchY = useEstimate ? DefaultEstimatedPitchY : PitchY;
                double origX  = useEstimate ? 0.0               : OriginX;
                double origY  = useEstimate ? 0.0               : OriginY;

                double targetX = origX + col * pitchX;
                double targetY = origY + row * pitchY;

                string areaReason;
                if (!IsInputStageWorkPointInArea(targetX, targetY, out areaReason))
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-MOVE-DIE-AREA", "InputStageUnit.MoveToDieAsync",
                        "Die target is outside input stage work area. row=" + row + ", col=" + col + ". " + areaReason);

                Task<int> moveY = MoveInputStageAxis(WaferStageAxis.WaferY, targetY, true);
                Task<int> moveX = MoveInputStageAxis(WaferStageAxis.VisionX, targetX, true);
                int[] results = await Task.WhenAll(moveY, moveX);

                if (results[0] != 0 || results[1] != 0 || StageY.IsAlarm || CameraX.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-MOVE-DIE", "InputStageUnit.MoveToDieAsync", $"Die 이동 실패 (row={row}, col={col}, StageY result={results[0]}, CameraX result={results[1]}, StageY.IsAlarm={StageY.IsAlarm}, CameraX.IsAlarm={CameraX.IsAlarm}).");

                Task<int> waitY = WaitInputStageAxisInPosition(WaferStageAxis.WaferY, targetY, ResolveSequenceMoveTimeout());
                Task<int> waitX = WaitInputStageAxisInPosition(WaferStageAxis.VisionX, targetX, ResolveSequenceMoveTimeout());
                int[] waitResults = await Task.WhenAll(waitY, waitX);
                if (waitResults[0] != 0 || waitResults[1] != 0)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-MOVE-DIE-WAIT", "InputStageUnit.MoveToDieAsync", $"Die 이동 완료 확인 실패 (row={row}, col={col}, StageY wait={waitResults[0]}, CameraX wait={waitResults[1]}).");

                return 0;
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
                if (IsNeedleZInSafePosition())
                    return 0;

                int result = await MoveInputStageAxis(WaferStageAxis.NeedleZ, Recipe.NeedleZ.AvoidPosition, bFine).ConfigureAwait(false);
                if (result != 0 || NeedleZ.IsAlarm)
                    return RaiseStageAlarm(AlarmSeverity.Error, "IS-NEEDLEZ-AVOID", source,
                        "NeedleZ avoid move before non-process move failed. result=" + result +
                        ", alarm=" + NeedleZ.IsAlarm +
                        ", actual=" + (NeedleZ != null ? NeedleZ.ActualPosition.ToString("F3") : "null") +
                        ", target=" + Recipe.NeedleZ.AvoidPosition.ToString("F3"));

                result = await WaitInputStageAxisInPosition(WaferStageAxis.NeedleZ, Recipe.NeedleZ.AvoidPosition, ResolveSequenceMoveTimeout()).ConfigureAwait(false);
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

        private int RaiseStageAlarm(AlarmSeverity severity, string code, string source, string message)
        {
            try
            {
                Console.WriteLine($"[ALARM] '{Name}' ? {message}");
                EventLogger.Write(EventKind.Alarm, "QMC", code, message);
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

