using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320
{
    // ----------------------------------------------------------------------
    //  CDT-320 설비 수준 데이터 클래스
    // ----------------------------------------------------------------------

    /// <summary>CDT-320 설비 수준의 기구적 설정값.</summary>
    public class CDT320MachineSetup : ISetupData
    {
        /// <summary>설비 시리즈 명칭.</summary>
        public string MachineSeries { get; set; } = "CDT-320";
    }

    /// <summary>CDT-320 설비 수준의 고정 사양 파라미터.</summary>
    public class CDT320MachineConfig : IConfigData
    {
        /// <summary>소프트웨어 모델 버전.</summary>
        public string ModelVersion { get; set; } = "v1.0";
    }

    /// <summary>CDT-320 설비 수준의 공정별 작업 파라미터.</summary>
    public class CDT320MachineRecipe : IRecipeData
    {
        /// <summary>현재 로드된 제품(공정) ID.</summary>
        public string ProductId { get; set; } = "PRODUCT-A";
    }

    // ----------------------------------------------------------------------
    //  Null Object 구현체 - 빌드 및 단독 테스트용
    //  실제 하드웨어/서버 연동 전 컴파일을 통과시키기 위한 최소 구현이다.
    //  각 서브시스템 구현 완료 후 해당 구체 클래스로 교체한다.
    // ----------------------------------------------------------------------

    /// <summary>IWaferLoader 빌드용 Null Object.</summary>
    internal class NullWaferLoader : IWaferLoader
    {
        public bool IsFeederAtSafePosition => true;
    }

    /// <summary>IBarcodeReader 빌드용 Null Object.</summary>
    internal class NullBarcodeReader : IBarcodeReader
    {
        public NullBarcodeReader(string readerName = "BARCODE")
        {
            ReaderName = string.IsNullOrWhiteSpace(readerName) ? "BARCODE" : readerName;
        }

        public string ReaderName { get; private set; }
        public bool IsConnected => false;
        public bool TryOpen() => false;
        public void Close() { }

        public Task<string> ReadAsync(int timeoutMs = 3000)
            => Task.FromResult("");
    }

    /// <summary>IVisionTcpClient 빌드용 Null Object (InputStageUnit용).</summary>
    internal class NullVisionTcpClient : IVisionTcpClient
    {
        public Task<bool> TriggerExposeAsync(int dieIndex)
            => Task.FromResult(true);

        public Task<bool> GetResultAsync(int dieIndex, int timeoutMs = 5000)
            => Task.FromResult(true);

        public Task<VisionAlignResult> TriggerAlignAsync(string alignTargetId)
            => Task.FromResult(new VisionAlignResult());
    }

    /// <summary>IWaferMapHandler 빌드용 Null Object.</summary>
    internal class NullWaferMapHandler : IWaferMapHandler
    {
        public Task<WaferMapData> ParseMapAsync(string waferId)
            => Task.FromResult(new WaferMapData
            {
                WaferId     = waferId,
                RowCount    = 1,
                ColumnCount = 1,
                DieMap      = new bool[1, 1] { { true } }
            });

        public void SendMapToUi(WaferMapData mapData) { }
    }

    /// <summary>ITransferPickerUnit 빌드용 Null Object.</summary>
    internal class NullTransferPickerUnit : ITransferPickerUnit
    {
        public int  PickerCount    => 1;
        public bool IsPickerReady  => true;

        public void NotifyPickReady(int dieIndex) { }

        public Task<bool> WaitPickerUpAsync(int timeoutMs = 3000)
            => Task.FromResult(true);
    }

    /// <summary>IVisionTpuClient 빌드용 Null Object (TransferPickerUnit용).</summary>
    internal class NullVisionTpuClient : IVisionTpuClient
    {
        public Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs = 1000)
            => Task.FromResult(true);

        public Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs, CancellationToken ct)
            => Task.FromResult(true);

        public Task<bool> StartBottomInspectAsync(int pickerNo, int timeoutMs, CancellationToken ct)
            => Task.FromResult(true);

        public Task<BottomVisionOffset> WaitBottomMResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
            => Task.FromResult(BuildSimulatedBottom(pickerNo));

        public Task<BottomVisionOffset> WaitBottomFinalResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
            => Task.FromResult(BuildSimulatedBottom(pickerNo));

        public Task<BottomVisionOffset> WaitBottomResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
            => Task.FromResult(BuildSimulatedBottom(pickerNo));

        public Task<BottomVisionOffset> GetBottomResultAsync(int pickerNo, int timeoutMs = 5000)
            => Task.FromResult(BuildSimulatedBottom(pickerNo));

        public Task<BottomVisionOffset> GetBottomResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
            => GetBottomResultAsync(pickerNo, timeoutMs);

        public Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs = 5000)
            => Task.FromResult(new BottomVisionOffset[]
            {
                BuildSimulatedBottom(1),
                BuildSimulatedBottom(2),
                BuildSimulatedBottom(3),
                BuildSimulatedBottom(4),
            });

        public Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs, CancellationToken ct)
            => GetBottomResultsAsync(timeoutMs);

        public Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs = 1000)
            => Task.FromResult(true);

        public Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs, CancellationToken ct)
            => Task.FromResult(true);

        public Task<bool> StartSideInspectAsync(int pickerNo, int angleDeg, int timeoutMs, CancellationToken ct)
            => Task.FromResult(true);

        public Task<SideVisionResult> WaitSideResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
            => GetSideResultAsync(pickerNo, timeoutMs);

        public void AbandonPendingInspection(int pickerNo, string reason) { }

        public Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs = 5000)
            => Task.FromResult(BuildSimulatedSide(pickerNo));

        public Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
            => GetSideResultAsync(pickerNo, timeoutMs);

        private static BottomVisionOffset BuildSimulatedBottom(int pickerNo)
        {
            QMC.CDT320.VisionComm.InspectionResultDto inspection =
                QMC.CDT320.VisionComm.AutoVisionRequestService.BuildSimulationInspectionResult(
                    QMC.CDT320.VisionComm.AutoVisionChannel.BottomInspection,
                    QMC.CDT320.VisionComm.VisionToolIds.BottomInspection.SurfaceInspector,
                    pickerNo);
            return QMC.CDT320.Calibration.VisionCameraCalibrationTransform.ToBottomVisionOffset(pickerNo, inspection);
        }

        private static SideVisionResult BuildSimulatedSide(int pickerNo)
        {
            QMC.CDT320.VisionComm.InspectionResultDto inspection =
                QMC.CDT320.VisionComm.AutoVisionRequestService.BuildSimulationInspectionResult(
                    QMC.CDT320.VisionComm.AutoVisionChannel.FrontSide,
                    QMC.CDT320.VisionComm.VisionToolIds.FrontSide.SurfaceInspector,
                    pickerNo);
            bool pass = inspection != null && inspection.IsPass;
            return new SideVisionResult
            {
                PickerNo = pickerNo,
                Side1Ok = pass,
                Side2Ok = pass,
                Side3Ok = true,
                Side4Ok = true,
                Raw = inspection != null ? inspection.Raw : "",
                Values = inspection != null && inspection.Values != null
                    ? new System.Collections.Generic.Dictionary<string, string>(inspection.Values, System.StringComparer.OrdinalIgnoreCase)
                    : new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            };
        }
    }

    /// <summary>ITpuUnit 용 Null Object (OutputStageUnit용).</summary>
    internal class NullTpuUnit : ITpuUnit
    {
        public void NotifyPlaceReady() { }

        public Task<bool> WaitPlaceDoneAsync(int timeoutMs = 3000)
            => Task.FromResult(true);

        public Task<bool> WaitPlaceDoneAsync(int timeoutMs, CancellationToken ct)
            => Task.FromResult(true);

        public void NotifyReadyForNextDie() { }

        public Task<bool> RequestColletCleaningAsync(int timeoutMs = 10000)
            => Task.FromResult(true);
    }

    /// <summary>IOutputUnloaderUnit 용 Null Object (OutputStageUnit용).</summary>
    internal class NullOutputUnloaderUnit : IOutputUnloaderUnit
    {
        public Task<bool> RequestWaferChangeAsync(DieGrade grade, int timeoutMs = 0)
            => Task.FromResult(true);
    }

    // ----------------------------------------------------------------------
    //  §4. VisionInspectionUnit
    // ----------------------------------------------------------------------

    /// <summary>
    /// Vision Inspection 유닛.<br/>
    /// 최대 5면(Bottom 1면 + Side 4면)을 촬상하여 마이크로스크래치를 및 칩을 검출하는 유닛.
    /// </summary>
    public class VisionInspectionUnit : BaseUnit<UnitSetup, UnitConfig, UnitRecipe>
    {
        /// <summary>Vision Inspection 유닛을 초기화한다.</summary>
        public VisionInspectionUnit() : base("VisionInspectionUnit") { }
    }

    // ----------------------------------------------------------------------
    //  CDT-320 머신 루트 클래스
    // ----------------------------------------------------------------------

    /// <summary>
    /// CDT-320 머신 5개 검사 및 분류 핸들러 장비의 최상위 루트 클래스.<br/>
    /// 6개의 Main Unit을 소유하며, Composite Pattern에 의해 Save() 등 공통 동작이
    /// 전체 트리에 재귀적으로 전파된다.
    /// <para>
    /// 장비 공정 흐름:<br/>
    /// [InputLoader] → [InputStage] → [Picker] → [VisionInspection]
    ///                                      → [OutputStage] → [OutputCassette/OutputFeeder]
    /// </para>
    /// </summary>
    public class CDT320_Machine
        : Machine<CDT320MachineSetup, CDT320MachineConfig, CDT320MachineRecipe>
    {
        /// <summary>Input Cassette에서 웨이퍼를 공급하는 로더 유닛.</summary>
        public InputCassetteUnit    InputCassetteUnit { get; }
        public InputFeederUnit      InputFeederUnit { get; }
        /// <summary>웨이퍼를 고정하고 다이 위치를 관리하는 Input Stage 유닛.</summary>
        public InputStageUnit       InputStageUnit       { get; }

        /// <summary>엑셀 PickerFront Sheet 기준 축/I/O/티칭 Unit입니다.</summary>
        public PickerFrontUnit      PickerFrontUnit      { get; }
        /// <summary>엑셀 PickerRear Sheet 기준 축/I/O/티칭 Unit입니다.</summary>
        public PickerRearUnit       PickerRearUnit       { get; }

        // <summary>엑셀 Vision Sheet 기준 축/I/O/티칭 Unit입니다.</summary>
        public VisionUnit VisionUnit { get; }

        // 이거 쓰나?
        /// <summary>5면 촬상 후 결과 판정 유닛.</summary>
        public VisionInspectionUnit VisionInspection { get; }

        /// <summary>양불 분류 적재 Output Stage 유닛.</summary>
        public OutputStageUnit      OutputStageUnit      { get; }
        /// <summary>Output Bin Feeder Y축과 클램프 실린더를 담당하는 유닛입니다.</summary>
        public OutputFeederUnit OutputFeederUnit { get; }
        /// <summary>Output Bin 카세트 리프터와 매핑 센서를 담당하는 유닛입니다.</summary>
        public OutputCassetteUnit      OutputCassetteUnit      { get; }

        /// <summary>Stage 45 - 운전 패널 (버튼 + 램프 + 신호탑 + 부저).</summary>
        public OperationPanelUnit   OpPanelUnit          { get; }

        /// <summary>Stage 46 - Resource Sensors (CDA + Vacuum 라인 압력 감지).</summary>
        public ResourceSensorsUnit  ResourcesUnit        { get; }

        /// <summary>Stage 47 - Ionizer (정전기 제거기).</summary>
        public IonizerUnit          IonizerUnit          { get; }

        /// <summary>Input Camera X에 설치된 Wafer Barcode Reader.</summary>
        public IBarcodeReader       WaferBarcodeReader { get; private set; }

        /// <summary>Output Camera X에 설치된 Bin Barcode Reader.</summary>
        public IBarcodeReader       BinBarcodeReader { get; private set; }

        /// <summary>
        /// <see cref="CDT320_Machine"/>을 초기화하고 6개 Unit 트리를 구성한다.<br/>
        /// 모든 외부 연동 인터페이스는 Null Object로 초기화되며,
        /// 실제 하드웨어 시스템 구성 완료 후 의존성 주입(DI)으로 교체한다.
        /// </summary>
        public CDT320_Machine() : base("CDT-320")
        {
            InputCassetteUnit = new InputCassetteUnit();
            InputFeederUnit = new InputFeederUnit();
            InputCassetteUnit.BindMachine(this);

            // InputStageUnit - Wafer Vision 은 실 TCP Adapter 사용 (QMC.Vision 과 통신).
            // VisionHub 가 연결 안 된 경우 Adapter 는 안전 fallback(Expose/Match = false).
            // Stage 28 - NullWaferLoader 를 WaferLoaderAdapter(InputLoader) 로 교체:
            //   InputStage 의 안전 인터락이 실 InputLoader.FeederY 위치 + Cyl 상태를 체크하도록 함.
            InputStageUnit = new InputStageUnit(
                vision: new VisionComm.WaferVisionAdapter(),
                mapHandler: new NullWaferMapHandler());
                //loader:     new QMC.CDT320.Sim.WaferLoaderAdapter(InputFeeder),
                //barcode:    new NullBarcodeReader(),
                //tpu:        new NullTransferPickerUnit());

            VisionInspection = new VisionInspectionUnit();
            PickerFrontUnit = new PickerFrontUnit();
            PickerRearUnit = new PickerRearUnit();
            VisionUnit = new VisionUnit();
            Calibration.CalibrationCoordinateService.MachineProvider = () => this;
            Calibration.VisionCameraCalibrationTransform.CalibrationProvider =
                () => Calibration.CalibrationCoordinateService.ResolveCamera(this);

            OutputFeederUnit = new OutputFeederUnit();
            OutputCassetteUnit = new OutputCassetteUnit();
            OutputCassetteUnit.BindMachine(this);
            OutputStageUnit = new OutputStageUnit(
                tpu: new NullTpuUnit(),
                unloader: new QMC.CDT320.Sim.OutputUnloaderAdapter(OutputCassetteUnit, OutputFeederUnit));


            // Stage 45 - Operation Panel + Tower Lamp + Buzzer 신규
            OpPanelUnit = new OperationPanelUnit();

            // Stage 46 - Resource Sensors (CDA + Vacuum 라인)
            ResourcesUnit = new ResourceSensorsUnit();

            // Stage 47 - Ionizer (정전기 제거기)
            IonizerUnit = new IonizerUnit();

            // Input/Output 카메라에 설치된 두 Barcode Reader는 AppSettings 채널별 설정으로 구성합니다.
            ReloadBarcodeReaders();


            Units.Add(InputCassetteUnit);
            Units.Add(InputFeederUnit);
            Units.Add(InputStageUnit);
            Units.Add(PickerFrontUnit);
            Units.Add(PickerRearUnit);
            Units.Add(VisionInspection);
            Units.Add(VisionUnit);
            Units.Add(OutputCassetteUnit);
            Units.Add(OutputFeederUnit);
            Units.Add(OutputStageUnit);
            Units.Add(OpPanelUnit);

            BindPickerFlowTransitionDiagnostics();
        }

        /// <summary>
        /// 저장된 채널별 설정으로 Input Wafer/Output Bin 리더를 다시 구성합니다.
        /// 설정 화면은 장비와 시퀀스가 정지된 상태에서만 이 메서드를 호출해야 합니다.
        /// </summary>
        public void ReloadBarcodeReaders()
        {
            AppSettings settings = AppSettingsStore.Current ?? new AppSettings();
            IBarcodeReader oldWaferReader = WaferBarcodeReader;
            IBarcodeReader oldBinReader = BinBarcodeReader;

            WaferBarcodeReader = CreateBarcodeReader(
                "INPUT WAFER",
                settings.WaferBarcodeSerialPort,
                settings.WaferBarcodeSerialBaud,
                settings.InputBarcodeTriggerCommand);
            BinBarcodeReader = CreateBarcodeReader(
                "OUTPUT BIN",
                settings.BinBarcodeSerialPort,
                settings.BinBarcodeSerialBaud,
                settings.OutputBarcodeTriggerCommand);

            CloseBarcodeReader(oldWaferReader);
            if (!ReferenceEquals(oldBinReader, oldWaferReader))
                CloseBarcodeReader(oldBinReader);
        }

        private static IBarcodeReader CreateBarcodeReader(
            string readerName,
            int portNumber,
            int baudRate,
            string triggerCommand)
        {
            int safePortNumber = Math.Max(1, portNumber);
            int safeBaudRate = baudRate > 0 ? baudRate : 9600;
            return new VisionComm.BarcodeSerialAdapter(
                readerName,
                "COM" + safePortNumber,
                safeBaudRate,
                triggerCommand);
        }

        private static void CloseBarcodeReader(IBarcodeReader reader)
        {
            if (reader == null)
                return;
            try { reader.Close(); } catch { }
            IDisposable disposable = reader as IDisposable;
            if (disposable != null)
            {
                try { disposable.Dispose(); } catch { }
            }
        }

        private void BindPickerFlowTransitionDiagnostics()
        {
            BindPickerFlowTransitionDiagnostics(
                "Front",
                PickerFrontUnit.FlowChecks,
                new[] { PickerFrontUnit.PickerZ0, PickerFrontUnit.PickerZ1, PickerFrontUnit.PickerZ2, PickerFrontUnit.PickerZ3 });
            BindPickerFlowTransitionDiagnostics(
                "Rear",
                PickerRearUnit.FlowChecks,
                new[] { PickerRearUnit.PickerZ0, PickerRearUnit.PickerZ1, PickerRearUnit.PickerZ2, PickerRearUnit.PickerZ3 });
        }

        private void BindPickerFlowTransitionDiagnostics(
            string side,
            BaseDigitalInput[] flowChecks,
            BaseAxis[] pickerZAxes)
        {
            if (flowChecks == null || pickerZAxes == null)
                return;

            int count = Math.Min(flowChecks.Length, pickerZAxes.Length);
            for (int i = 0; i < count; i++)
            {
                BaseDigitalInput flowCheck = flowChecks[i];
                BaseAxis pickerZ = pickerZAxes[i];
                int pickerNo = i + 1;
                if (flowCheck == null)
                    continue;

                flowCheck.StateChanged += (input, flowOn) =>
                    LogPickerFlowTransition(side, pickerNo, flowOn, pickerZ);
            }
        }

        private void LogPickerFlowTransition(string side, int pickerNo, bool flowOn, BaseAxis pickerZ)
        {
            DateTime transitionAt = DateTime.Now;
            Log.Write(
                LogLevel.Normal,
                "Main",
                "PickerFlowTransition",
                "Picker Flow 전이 감지. transitionAt=" + transitionAt.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                ", side=" + side +
                ", pickerNo=" + pickerNo +
                ", flow=" + (flowOn ? "ON" : "OFF") +
                ", pickerZ=" + BuildFlowTransitionAxisSnapshot(pickerZ) +
                ", ejectPinZ=" + BuildFlowTransitionAxisSnapshot(InputStageUnit != null ? InputStageUnit.EjectPinZ : null) +
                " - Check");
        }

        private static string BuildFlowTransitionAxisSnapshot(BaseAxis axis)
        {
            if (axis == null)
                return "null";

            try
            {
                return "name=" + axis.Name +
                       ",actual=" + axis.ActualPosition.ToString("F6") +
                       ",command=" + axis.CommandPosition.ToString("F6") +
                       ",moving=" + axis.IsMoving;
            }
            catch (Exception ex)
            {
                return "name=" + axis.Name + ",readError=" + ex.Message;
            }
        }
    }
}
