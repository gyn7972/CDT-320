using QMC.Common.Data.Store;
using QMC.Common.IO;
using QMC.Common.Motion.Ajin;

namespace QMC.CDT320.Ajin
{
    /// <summary>
    /// AXL AxdiReadInportBit 기반 실 DI.
    /// Setup.ModuleNo / BitNo / IsNormallyClosed 를 사용.
    /// </summary>
    public class AjinDigitalInput : BaseDigitalInput
    {
        protected override bool UseInternalStatusUpdate
        {
            get { return false; }
        }

        public AjinDigitalInput(string name, int moduleNo, int bitNo, bool normallyClosed = false, string settingsStorageKey = null)
            : base(name)
        {
            SettingsStorageKey = string.IsNullOrWhiteSpace(settingsStorageKey) ? name : settingsStorageKey;
            Setup.ModuleNo         = moduleNo;
            Setup.BitNo            = bitNo;
            Setup.IsNormallyClosed = normallyClosed;
            Config.IsSimulationMode = false;
        }

        // Setup 저장 생략을 앱 실행당 1회만 로그로 남기기 위한 플래그(포트마다 찍으면 92줄이 된다).
        private static bool _setupSaveSkipLogged;

        // To do: [IO Setup 보호] 실장비에서는 종료 저장이 배선값(Setup)을 덮어쓰지 않게 한다.
        // 기존 조건: BaseComponent.SaveSettings()가 Setup + Config를 모두 저장했다.
        //            → 앱 종료 시 Form1.SaveMachineSettings()가 메모리 값으로 EquipmentData Setup 파일을
        //              되쓰면서, 외부에서 고친 주소/극성이 지워지고 런타임 오염이 영구화됐다.
        //              (2026-07-28 아웃풋 링 센서 건 - 파일을 고쳐도 반영되지 않는 원인이었다)
        // 현재 기준: AjinDigitalInput은 실보드에서만 생성되므로 여기서 Setup 저장을 건너뛴다.
        //            Config(IsSimulationMode/IgnoreWaits)는 운전 중 바꾸는 값이므로 그대로 저장한다.
        //            배선값은 IO 설정 화면의 명시적 저장 경로에서만 변경된다.
        //
        // ★ 2026-07-29 추가 — 위 "Config는 그대로 저장한다"가 남긴 구멍을 LoadSettings()에서 막는다.
        //    아래 LoadSettings() 주석 참조. Save는 그대로 두고 Load에서만 강제한다.
        //    (Load에서 false로 되돌리므로, 다음 저장 때 파일에도 자연히 false가 기록된다.)
        public override bool SaveSettings()
        {
            try
            {
                if (!_setupSaveSkipLogged)
                {
                    _setupSaveSkipLogged = true;
                    QMC.Common.Log.Write("Main", "SYSTEM", "IO-SETUP-SAVE-SKIP",
                        "실장비 DI Setup(주소/극성) 자동 저장을 생략합니다. " +
                        "배선값은 IO 설정 화면의 명시적 저장으로만 변경됩니다. - Check");
                }

                return UnitDataStore.SaveConfig(Config, SettingsStorageKey);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        // ============================================================================
        // [실장비 시뮬 강제 해제 2026-07-29]  ★실장비 미검증 — 실장비에서 테스트 필요★
        //
        // 증상: 실장비인데 특정 입력이 실센서를 전혀 따라오지 않는다(값이 얼어붙거나 반대로 보인다).
        //
        // 원인 경로:
        //   1) AjinDigitalInput 생성자        → Config.IsSimulationMode = false  (실보드)
        //   2) io_settings.json               → 전부 실보드로 맞춰놔도 여기까지는 false
        //   3) Machine.LoadSettings()         → BaseComponent.LoadSettings() 에서
        //                                       Config = UnitDataStore.LoadConfig(SettingsStorageKey, Config)
        //                                       ★EquipmentData\Config\<이름>.json 의 값이 통째로 이긴다★
        //      → 파일에 "IsSimulationMode": true 가 남아 있으면 실보드 포인트가 시뮬로 바뀐다.
        //
        //   그 상태에서 UpdateStatus() 는 아래 시뮬 분기로 빠져 AXD.Read 를 아예 타지 않는다.
        //   게다가 주입된 시뮬값이 없으면 TryGetSimulatedState 가 false 를 반환해
        //   ApplyScannedState 조차 호출되지 않고 → 그 신호는 마지막 값에 그대로 얼어붙는다.
        //
        //   07-28 에 Setup(주소/극성) 저장은 막았지만 Config 는 "운전 중 바꾸는 값"이라 열어뒀고,
        //   그 구멍으로 IsSimulationMode 가 파일에 영구 저장돼 매 기동마다 되살아났다.
        //   (2026-07-28 실장비 OutputStage GoodBin/Ng 링 센서 건이 이 경로였다)
        //
        // 조치: AjinDigitalInput 은 실보드에서만 생성되는 클래스다. 따라서 파일이 넣은
        //       IsSimulationMode 는 근거가 없는 값으로 보고 기동 시 false 로 되돌린다.
        //
        //       ★기동 시 1회만 강제한다★ — 운전 중 IO 화면에서 특정 포인트를 임시로 시뮬 전환하는
        //       기능은 그대로 살아 있다. 재시작하면 실신호로 복귀할 뿐이다.
        //       IgnoreWaits 는 건드리지 않는다.
        //
        // 실장비 확인 방법: 기동 로그에서 "IO-SIM-FORCE-REAL" 을 검색한다.
        //       포인트별 1줄 + 마지막에 합계 1줄이 남는다. 합계가 0 이면 원래 깨끗한 상태다.
        //       0 이 아니면 그동안 그만큼의 실센서가 죽어 있었다는 뜻이므로,
        //       살아난 신호들 때문에 가려져 있던 알람이 몰려 나올 수 있다 → 첫 기동은 무인으로 두지 말 것.
        // ============================================================================
        private static int _simForcedRealCount;

        public override void LoadSettings()
        {
            base.LoadSettings();

            try
            {
                if (Config == null || !Config.IsSimulationMode)
                    return;

                Config.IsSimulationMode = false;
                _simForcedRealCount++;

                QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "IO-SIM-FORCE-REAL",
                    "실보드 DI 인데 Config 파일이 시뮬 모드로 지정했습니다. 실신호를 쓰도록 되돌립니다. name=" +
                    Name + ", M" + Setup.ModuleNo + "/B" + Setup.BitNo +
                    ", 파일=EquipmentData\\Config\\" + SettingsStorageKey + ".json - Check");
            }
            catch
            {
            }
            finally
            {
            }
        }

        /// <summary>
        /// [실장비 시뮬 강제 해제 2026-07-29] 강제 복귀 합계를 로그로 남긴다.
        /// 모든 유닛의 LoadSettings 가 끝난 뒤(Form1 의 LoadMachineSettings 직후) 1회 호출한다.
        /// </summary>
        public static int LogSimForcedRealSummary()
        {
            int count = _simForcedRealCount;
            QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "IO-SIM-FORCE-REAL",
                "실보드 DI 시뮬 강제 해제 합계=" + count + "건 - " + (count == 0 ? "Ok" : "Check"));
            return count;
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

            if (!AjinSystem.IsOpen)      return;
            AjinIoScanService service = AjinIoScanService.Current;
            if (service != null && service.TryApplyLatest(this)) return;

            bool raw = false;
            int ret;
            lock (AjinIoScanService.AxdSyncRoot)
                ret = AXD.Read(Setup.ModuleNo, Setup.BitNo, ref raw);
            if (ret != 0) return;

            bool signal = raw;
            bool logical = Setup.IsNormallyClosed ? !signal : signal;
            ApplyScannedState(logical);
        }

        // SimulateInput 은 실보드 모드에서 무시되므로 base 그대로.
    }
}
