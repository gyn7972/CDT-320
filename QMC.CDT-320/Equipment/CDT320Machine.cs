using QMC.Common;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320
{
    // ??????????????????????????????????????????????????????????????????????????
    //  CDT-320 ���� ���� ������ Ŭ����
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>CDT-320 ���� ������ �ⱸ�� ������.</summary>
    public class CDT320MachineSetup : ISetupData
    {
        /// <summary>���� �ø��� ��Ī.</summary>
        public string MachineSeries { get; set; } = "CDT-320";
    }

    /// <summary>CDT-320 ���� ������ ���� ��� �Ķ����.</summary>
    public class CDT320MachineConfig : IConfigData
    {
        /// <summary>����Ʈ���� �� ����.</summary>
        public string ModelVersion { get; set; } = "v1.0";
    }

    /// <summary>CDT-320 ���� ������ ������ �۾� �Ķ����.</summary>
    public class CDT320MachineRecipe : IRecipeData
    {
        /// <summary>���� �ε�� ��ǰ(����) ID.</summary>
        public string ProductId { get; set; } = "PRODUCT-A";
    }

    // ??????????????????????????????????????????????????????????????????????????
    //  Null Object ����ü ? ��� �� �ܵ� �׽�Ʈ��
    //  ���� �ϵ����/���� ���� �� �������� �����Ű�� ���� �ּ� �����̴�.
    //  �� ����ý��� ���� �Ϸ� �� �ش� ��ü Ŭ������ ��ü�Ѵ�.
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>IWaferLoader ���� Null Object.</summary>
    internal class NullWaferLoader : IWaferLoader
    {
        public bool IsFeederAtSafePosition => true;
    }

    /// <summary>IBarcodeReader ���� Null Object.</summary>
    internal class NullBarcodeReader : IBarcodeReader
    {
        public Task<string> ReadAsync(int timeoutMs = 3000)
            => Task.FromResult("WAFER-NULL-ID");
    }

    /// <summary>IVisionTcpClient ���� Null Object (InputStageUnit��).</summary>
    internal class NullVisionTcpClient : IVisionTcpClient
    {
        public Task<bool> TriggerExposeAsync(int dieIndex)
            => Task.FromResult(true);

        public Task<bool> GetResultAsync(int dieIndex, int timeoutMs = 5000)
            => Task.FromResult(true);

        public Task<VisionAlignResult> TriggerAlignAsync(string alignTargetId)
            => Task.FromResult(new VisionAlignResult());
    }

    /// <summary>IWaferMapHandler ���� Null Object.</summary>
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

    /// <summary>ITransferPickerUnit ���� Null Object.</summary>
    internal class NullTransferPickerUnit : ITransferPickerUnit
    {
        public int  PickerCount    => 1;
        public bool IsPickerReady  => true;

        public void NotifyPickReady(int dieIndex) { }

        public Task<bool> WaitPickerUpAsync(int timeoutMs = 3000)
            => Task.FromResult(true);
    }

    /// <summary>IVisionTpuClient ���� Null Object (TransferPickerUnit��).</summary>
    internal class NullVisionTpuClient : IVisionTpuClient
    {
        public Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs = 1000)
            => Task.FromResult(true);

        public Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs, CancellationToken ct)
            => Task.FromResult(true);

        public Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs = 5000)
            => Task.FromResult(new BottomVisionOffset[]
            {
                new BottomVisionOffset { PickerNo = 1, OffsetX = 0, OffsetY = 0, IsOk = true },
                new BottomVisionOffset { PickerNo = 2, OffsetX = 0, OffsetY = 0, IsOk = true },
                new BottomVisionOffset { PickerNo = 3, OffsetX = 0, OffsetY = 0, IsOk = true },
                new BottomVisionOffset { PickerNo = 4, OffsetX = 0, OffsetY = 0, IsOk = true },
            });

        public Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs, CancellationToken ct)
            => GetBottomResultsAsync(timeoutMs);

        public Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs = 1000)
            => Task.FromResult(true);

        public Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs, CancellationToken ct)
            => Task.FromResult(true);

        public Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs = 5000)
            => Task.FromResult(new SideVisionResult
            {
                PickerNo = pickerNo,
                Side1Ok  = true,
                Side2Ok  = true,
                Side3Ok  = true,
                Side4Ok  = true,
            });

        public Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
            => GetSideResultAsync(pickerNo, timeoutMs);
    }

    /// <summary>ITpuUnit �� Null Object (OutputStageUnit��).</summary>
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

    /// <summary>IOutputUnloaderUnit �� Null Object (OutputStageUnit��).</summary>
    internal class NullOutputUnloaderUnit : IOutputUnloaderUnit
    {
        public Task<bool> RequestWaferChangeAsync(DieGrade grade, int timeoutMs = 0)
            => Task.FromResult(true);
    }

    // ??????????????????????????????????????????????????????????????????????????
    //  ��4. VisionInspectionUnit
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>
    /// Vision Inspection ����.<br/>
    /// �ִ� 5��(Bottom 1�� + Side 4��)�� �Ի��Ͽ� ����ũ�ν�ũ��ġ�� �� Ĩ�� �����ϴ� ����.
    /// </summary>
    public class VisionInspectionUnit : BaseUnit<UnitSetup, UnitConfig, UnitRecipe>
    {
        /// <summary>Vision Inspection ������ �ʱ�ȭ�Ѵ�.</summary>
        public VisionInspectionUnit() : base("VisionInspectionUnit") { }
    }

    // ??????????????????????????????????????????????????????????????????????????
    //  CDT-320 �ӽ� ��Ʈ Ŭ����
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>
    /// CDT-320 �ӽ� 5�� �˻� �� �з� �ڵ鷯 ����� �ֻ��� ��Ʈ Ŭ����.<br/>
    /// 6���� Main Unit�� �����ϸ�, Composite Pattern�� ���� Save() �� ���� ������
    /// ��ü Ʈ���� ��������� ���ĵȴ�.
    /// <para>
    /// ��� ���� �帧:<br/>
    /// [InputLoader] �� [InputStage] �� [Picker] �� [VisionInspection]
    ///                                      �� [OutputStage] �� [OutputCassette/OutputFeeder]
    /// </para>
    /// </summary>
    public class CDT320_Machine
        : Machine<CDT320MachineSetup, CDT320MachineConfig, CDT320MachineRecipe>
    {
        /// <summary>Input Cassette���� �����۸� �����ϴ� �δ� ����.</summary>
        public InputCassetteUnit    InputCassetteUnit { get; }
        public InputFeederUnit      InputFeederUnit { get; }
        /// <summary>�����۸� �����ϰ� ���� ��ġ�� �����ϴ� Input Stage ����.</summary>
        public InputStageUnit       InputStageUnit       { get; }

        /// <summary>���� PickerFront Sheet ���� ��/I/O/ƼĪ Unit�Դϴ�.</summary>
        public PickerFrontUnit      PickerFrontUnit      { get; }
        /// <summary>���� PickerRear Sheet ���� ��/I/O/ƼĪ Unit�Դϴ�.</summary>
        public PickerRearUnit       PickerRearUnit       { get; }

        // <summary>���� Vision Sheet ���� ��/I/O/ƼĪ Unit�Դϴ�.</summary>
        public VisionUnit VisionUnit { get; }

        // �̰� ����?
        /// <summary>5�� �Ի� �� ��� ���� ����.</summary>
        public VisionInspectionUnit VisionInspection { get; }
        
        /// <summary>��� �з� ���� Output Stage ����.</summary>
        public OutputStageUnit      OutputStageUnit      { get; }
        /// <summary>Output Bin Feeder Y��� Ŭ���� �Ǹ����� ����ϴ� �����Դϴ�.</summary>
        public OutputFeederUnit OutputFeederUnit { get; }
        /// <summary>Output Bin ī��Ʈ �����Ϳ� ���� ������ ����ϴ� �����Դϴ�.</summary>
        public OutputCassetteUnit      OutputCassetteUnit      { get; }

        /// <summary>Stage 45 ? ���� �г� (��ư + ���� + ��ȣž + ����).</summary>
        public OperationPanelUnit   OpPanelUnit          { get; }

        /// <summary>Stage 46 ? Resource Sensors (CDA + Vacuum ���� �з� ����).</summary>
        public ResourceSensorsUnit  ResourcesUnit        { get; }

        /// <summary>Stage 47 ? Ionizer (������ ���ű�).</summary>
        public IonizerUnit          IonizerUnit          { get; }

        /// <summary>Stage 50 ? Bin Barcode Reader (Output ī��Ʈ ID �б�).</summary>
        public IBarcodeReader       BinBarcodeReader { get; }

        /// <summary>
        /// <see cref="CDT320_Machine"/>�� �ʱ�ȭ�ϰ� 6�� Unit Ʈ���� �����Ѵ�.<br/>
        /// ��� �ܺ� ���� �������̽��� Null Object�� �ʱ�ȭ�Ǹ�,
        /// ���� �ϵ���� �ý��� ���� �Ϸ� �� ������ ����(DI)���� ��ü�Ѵ�.
        /// </summary>
        public CDT320_Machine() : base("CDT-320")
        {
            InputCassetteUnit = new InputCassetteUnit();
            InputFeederUnit = new InputFeederUnit();
            InputCassetteUnit.BindMachine(this);

            // InputStageUnit - Wafer Vision �� �� TCP Adapter ��� (QMC.Vision �� ���).
            // VisionHub �� ���� �� �� ��� Adapter �� ���� fallback(Expose/Match = false).
            // Stage 28 ? NullWaferLoader �� WaferLoaderAdapter(InputLoader) �� ��ü:
            //   InputStage �� ���� ���Ͷ��� �� InputLoader.FeederY ��ġ + Cyl ���¸� üũ�ϵ��� ��.
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

            OutputFeederUnit = new OutputFeederUnit();
            OutputCassetteUnit = new OutputCassetteUnit();
            OutputStageUnit = new OutputStageUnit(
                tpu: new NullTpuUnit(),
                unloader: new QMC.CDT320.Sim.OutputUnloaderAdapter(OutputCassetteUnit, OutputFeederUnit));
            

            // Stage 45 ? Operation Panel + Tower Lamp + Buzzer �ű�
            OpPanelUnit = new OperationPanelUnit();

            // Stage 46 ? Resource Sensors (CDA + Vacuum ����)
            ResourcesUnit = new ResourceSensorsUnit();

            // Stage 47 ? Ionizer (������ ���ű�)
            IonizerUnit = new IonizerUnit();

            // Stage 50 ? Bin Barcode Reader (���� IBarcodeReader �ν��Ͻ�)
            //   �Ǻ��� � �� BarcodeSerialAdapter �� ��ü ����
            BinBarcodeReader = new NullBarcodeReader();


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
        }
    }
}
