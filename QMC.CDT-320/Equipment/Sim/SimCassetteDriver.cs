using System;
using QMC.CDT320.Sequencing;
using QMC.Common.IO;

namespace QMC.CDT320.Sim
{
    /// <summary>
    /// 시뮬레이션 모드에서 카세트 관련 센서(DI)들의 동작을 모방하는 드라이버.<br/>
    /// 실보드 모드에서는 본 드라이버를 사용하지 않는다.
    /// <para>
    /// 책임:<br/>
    /// 1. <see cref="InputLoaderUnit.CassetteExistSensor"/> 기본 상태(true) 주입<br/>
    /// 2. <see cref="InputLoaderUnit.ProtrusionSensor"/> 기본 OFF<br/>
    /// 3. ElevatorZ 이동 완료 시 현재 위치를 슬롯으로 환산하여
    ///    <see cref="InputLoaderUnit.WaferDetectSensor"/> 에 슬롯별 웨이퍼 유무를 반영<br/>
    /// 4. <see cref="InputLoaderUnit.FeederClampCyl"/> 전진/후진에 따라
    ///    <see cref="InputLoaderUnit.WaferClampedSensor"/> 토글<br/>
    /// 5. OutputUnloader 측 동등 처리 (간이)
    /// </para>
    /// </summary>
    public class SimCassetteDriver
    {
        //private readonly InputLoaderUnit _input; //-> 대체
        private readonly InputCassetteUnit _inputCassette;
        private readonly InputFeederUnit _inputFeeder;
        private readonly OutputCassetteUnit _outputCassette;
        private readonly OutputFeederUnit _outputFeeder;

        // ─── Input 측 시뮬 상태 ─────────────────────────────────────────
        /// <summary>Input 카세트 슬롯별 가상 웨이퍼 유무 (인덱스 0 = 최하단).</summary>
        public bool[] InputSlotsHasWafer { get; private set; }
        /// <summary>Input 카세트 안착 여부 (시뮬).</summary>
        public bool   InputCassettePresent { get; private set; } = true;
        /// <summary>Input 카세트 슬롯 피치 [mm] — 사양서 기준 6.0.</summary>
        public double InputSlotPitchMm   { get; set; } = 6.0;

        // ─── Output 측 시뮬 상태 ────────────────────────────────────────
        public bool[] OutputNgSlots    { get; private set; }
        public bool[] OutputGood1Slots { get; private set; }
        public bool[] OutputGood2Slots { get; private set; }
        public bool   OutputCassettePresent { get; private set; } = true;

        /// <summary>슬롯/카세트 상태가 바뀔 때 발행 (UI 갱신용).</summary>
        public event Action StateChanged;

        public SimCassetteDriver(
            InputCassetteUnit inputCassette,
            InputFeederUnit inputFeeder,
            OutputCassetteUnit outputCassette,
            OutputFeederUnit outputFeeder)
        {
            _inputCassette = inputCassette ?? throw new ArgumentNullException(nameof(inputCassette));
            _inputFeeder = inputFeeder ?? throw new ArgumentNullException(nameof(inputFeeder));
            _outputCassette = outputCassette ?? throw new ArgumentNullException(nameof(outputCassette));
            _outputFeeder = outputFeeder ?? throw new ArgumentNullException(nameof(outputFeeder));

            int inputSlotCount = _inputCassette.Config != null && _inputCassette.Config.SlotCount > 0
                ? _inputCassette.Config.SlotCount
                : 16;
            int outputSlotCount = 25;

            // 기본 카세트 가득 채우기
            InputSlotsHasWafer = new bool[inputSlotCount];
            for (int i = 0; i < InputSlotsHasWafer.Length; i++) InputSlotsHasWafer[i] = true;

            // Output 카세트 - 비어있는 상태로 시작
            OutputNgSlots    = new bool[outputSlotCount];
            OutputGood1Slots = new bool[outputSlotCount];
            OutputGood2Slots = new bool[outputSlotCount];

            HookInput();
            HookOutput();

            // 초기 센서 상태 주입
            _inputCassette.CassetteExistSensor.SimulateInput(InputCassettePresent);
            _inputCassette.ProtrusionSensor.SimulateInput(false);
            _inputCassette.WaferDetectSensor.SimulateInput(false);
            _inputFeeder.WaferClampedSensor.SimulateInput(false);
            UpdateInputDetectFromPosition();

            // Stage 27 — Output 측 초기 센서 주입
            _outputCassette.NgBin8CassetteCheck0.SimulateInput(true);
            _outputCassette.GoodBin8CassetteCheck0.SimulateInput(true);
            _outputCassette.GoodBin8CassetteCheck1.SimulateInput(true);
            _outputCassette.ProtrusionSensor.SimulateInput(false);
            _outputCassette.WaferDetectSensor.SimulateInput(false);
            _outputFeeder.WaferClampedSensor.SimulateInput(false);
            UpdateOutputDetectFromPosition();
        }

        // ─── Input 훅 ───────────────────────────────────────────────────

        private void HookInput()
        {
            // ElevatorZ 이동 완료 → WaferDetectSensor 갱신
            _inputCassette.WaferLifterZ.MoveCompleted += _ => UpdateInputDetectFromPosition();

            // FeederClampCyl InFwd ON ↔ WaferClampedSensor 동기화
            _inputFeeder.FeederClampCyl.InFwd.StateChanged += (sensor, on) =>
            {
                _inputFeeder.WaferClampedSensor.SimulateInput(on);
            };
        }

        private void UpdateInputDetectFromPosition()
        {
            double pos   = _inputCassette.WaferLifterZ.ActualPosition;
            //double first = _input.Setup.FirstSlotPosition;
            double first = _inputCassette.Recipe.FirstSlotPosition;
            int slot = (int)Math.Round((pos - first) / InputSlotPitchMm);
            bool has = (slot >= 0 && slot < InputSlotsHasWafer.Length) && InputSlotsHasWafer[slot];
            _inputCassette.WaferDetectSensor.SimulateInput(has);
        }

        // ─── Output 훅 (Stage 27) ───────────────────────────────────────

        private void HookOutput()
        {
            // ElevatorZ 이동 완료 → 카세트별 슬롯 위치 환산 후 WaferDetectSensor 갱신
            _outputCassette.BinLifterZ.MoveCompleted += _ => UpdateOutputDetectFromPosition();

            // FeederClampCyl InFwd ↔ WaferClampedSensor
            _outputFeeder.FeederClampCyl.InFwd.StateChanged += (s, on) =>
            {
                _outputFeeder.WaferClampedSensor.SimulateInput(on);
            };
        }

        private void UpdateOutputDetectFromPosition()
        {
            double pos = _outputCassette.BinLifterZ.ActualPosition;
            double pitch = _outputCassette.Config.SlotPitch;

            // NG 카세트
            int slot = (int)Math.Round((pos - _outputCassette.Recipe.NGFirstSlotPosition) / pitch);
            if (slot >= 0 && slot < OutputNgSlots.Length)
            {
                _outputCassette.WaferDetectSensor.SimulateInput(OutputNgSlots[slot]);
                return;
            }
            // Good1 카세트
            slot = (int)Math.Round((pos - _outputCassette.Recipe.GoodFirstSlotPosition) / pitch);
            if (slot >= 0 && slot < OutputGood1Slots.Length)
            {
                _outputCassette.WaferDetectSensor.SimulateInput(OutputGood1Slots[slot]);
                return;
            }
            // Good2 카세트
            slot = (int)Math.Round((pos - (_outputCassette.Recipe.GoodFirstSlotPosition + _outputCassette.Config.GOODNGPositionOffset)) / pitch);
            if (slot >= 0 && slot < OutputGood2Slots.Length)
            {
                _outputCassette.WaferDetectSensor.SimulateInput(OutputGood2Slots[slot]);
                return;
            }
            _outputCassette.WaferDetectSensor.SimulateInput(false);
        }

        // ─── 외부 제어 API ─────────────────────────────────────────────

        /// <summary>Input 카세트 안착 여부 토글 (시뮬 UI 용).</summary>
        public void SetInputCassettePresent(bool present)
        {
            InputCassettePresent = present;
            _inputCassette.CassetteExistSensor.SimulateInput(present);
            StateChanged?.Invoke();
        }

        /// <summary>Input 슬롯 K 의 가상 웨이퍼 유무 설정 (시뮬 UI 용).</summary>
        public void SetInputSlotWafer(int slot, bool hasWafer)
        {
            if (slot < 0 || slot >= InputSlotsHasWafer.Length) return;
            InputSlotsHasWafer[slot] = hasWafer;
            UpdateInputDetectFromPosition();
            StateChanged?.Invoke();
        }

        /// <summary>모든 Input 슬롯에 가상 웨이퍼 채움.</summary>
        public void RefillInputCassette()
        {
            for (int i = 0; i < InputSlotsHasWafer.Length; i++) InputSlotsHasWafer[i] = true;
            UpdateInputDetectFromPosition();
            StateChanged?.Invoke();
        }

        /// <summary>Stage 37 — Output 카세트 슬롯 적재 시뮬 (StoreFullWafer 성공 시 true).</summary>
        public void SetOutputSlotFilled(QMC.CDT320.TargetCassette target, int slot, bool filled)
        {
            bool[] arr = target == QMC.CDT320.TargetCassette.Ng    ? OutputNgSlots
                       : target == QMC.CDT320.TargetCassette.Good1 ? OutputGood1Slots
                                                                   : OutputGood2Slots;
            if (slot < 0 || slot >= arr.Length) return;
            arr[slot] = filled;
            UpdateOutputDetectFromPosition();
            StateChanged?.Invoke();
        }
    }
}
