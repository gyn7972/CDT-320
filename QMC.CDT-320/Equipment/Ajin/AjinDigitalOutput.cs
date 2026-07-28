using QMC.Common.Data.Store;
using QMC.Common.IO;
using QMC.Common.Logging;

namespace QMC.CDT320.Ajin
{
    public class AjinDigitalOutput : BaseDigitalOutput
    {
        protected override bool UseInternalStatusUpdate
        {
            get { return false; }
        }

        public AjinDigitalOutput(string name, int moduleNo, int bitNo, bool normallyClosed = false)
            : base(name)
        {
            Setup.ModuleNo = moduleNo;
            Setup.BitNo = bitNo;
            Setup.IsNormallyClosed = normallyClosed;
            Config.IsSimulationMode = false;
        }

        // Setup 저장 생략을 앱 실행당 1회만 로그로 남기기 위한 플래그(포트마다 찍으면 72줄이 된다).
        private static bool _setupSaveSkipLogged;

        // To do: [IO Setup 보호] 실장비에서는 종료 저장이 배선값(Setup)을 덮어쓰지 않게 한다.
        // 기존 조건: BaseComponent.SaveSettings()가 Setup + Config를 모두 저장했다.
        //            → 앱 종료 시 Form1.SaveMachineSettings()가 메모리 값으로 EquipmentData Setup 파일을
        //              되쓰면서, 외부에서 고친 주소/극성이 지워지고 런타임 오염이 영구화됐다.
        // 현재 기준: AjinDigitalOutput은 실보드에서만 생성되므로 여기서 Setup 저장을 건너뛴다.
        //            Config(IsSimulationMode/IgnoreWaits)는 운전 중 바꾸는 값이므로 그대로 저장한다.
        public override bool SaveSettings()
        {
            try
            {
                if (!_setupSaveSkipLogged)
                {
                    _setupSaveSkipLogged = true;
                    QMC.Common.Log.Write("Main", "SYSTEM", "IO-SETUP-SAVE-SKIP",
                        "실장비 DO Setup(주소/극성) 자동 저장을 생략합니다. " +
                        "배선값은 IO 설정 화면의 명시적 저장으로만 변경됩니다. - Check");
                }

                return UnitDataStore.SaveConfig(Config, StorageKey);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public override void Write(bool state)
        {
            base.Write(state);

            if (IsBinOutput(Name))
            {
                EventLogger.Write(EventKind.Event, "QMC", "AJIN-DO-WRITE",
                    "DO write. name=" + Name
                    + ", module=" + Setup.ModuleNo
                    + ", bit=" + Setup.BitNo
                    + ", state=" + (state ? "ON" : "OFF")
                    + ", sim=" + Config.IsSimulationMode);
            }

            if (Config.IsSimulationMode) return;
            if (!AjinSystem.IsOpen) return;
            AjinIoScanService.WriteOutput(this, state);
        }

        private static bool IsBinOutput(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                return name.IndexOf("BinGuide", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("BinClamp", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("BinUnclamp", System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public override void UpdateStatus()
        {
            if (Config.IsSimulationMode)
            {
                bool simulatedState;
                if (AjinIoScanService.TryGetSimulatedState(this, out simulatedState))
                    ApplyScannedState(simulatedState);
                return;
            }

            if (!AjinSystem.IsOpen) return;

            AjinIoScanService service = AjinIoScanService.Current;
            if (service != null && service.TryApplyLatest(this)) return;

            bool raw = false;
            int ret;
            lock (AjinIoScanService.AxdSyncRoot)
                ret = QMC.Common.Motion.Ajin.AXD.ReadOutput(Setup.ModuleNo, Setup.BitNo, ref raw);
            if (ret != 0) return;

            bool logical = Setup.IsNormallyClosed ? !raw : raw;
            ApplyScannedState(logical);
        }

    }
}
