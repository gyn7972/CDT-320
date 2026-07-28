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
        //
        // ★ 2026-07-29 추가 — 위 "Config는 그대로 저장한다"가 남긴 구멍을 LoadSettings()에서 막는다.
        //    아래 LoadSettings() 주석 참조. Save는 그대로 두고 Load에서만 강제한다.
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

        // ============================================================================
        // [실장비 시뮬 강제 해제 2026-07-29]  ★실장비 미검증 — 실장비에서 테스트 필요★
        //
        // 입력측(AjinDigitalInput)과 같은 원인이며, 출력측 결과가 더 위험하다.
        //
        // 원인 경로:
        //   1) AjinDigitalOutput 생성자  → Config.IsSimulationMode = false  (실보드)
        //   2) io_settings.json          → 전부 실보드로 맞춰도 여기까지는 false
        //   3) Machine.LoadSettings()    → BaseComponent.LoadSettings() 가
        //                                  Config = UnitDataStore.LoadConfig(StorageKey, Config) 로
        //                                  ★EquipmentData\Config\<이름>.json 값을 통째로 덮어쓴다★
        //
        // 이 상태가 되면:
        //   - Write()      : 아래 "if (Config.IsSimulationMode) return;" 에서 빠져나가
        //                    ★솔레노이드에 실제로 아무 신호도 안 나간다★.
        //                    소프트웨어는 출동시켰다고 알고 있는데 실린더는 그대로 있는다.
        //   - UpdateStatus : AXD.ReadOutput 을 안 타고 시뮬값(없으면 마지막 값)에 얼어붙는다.
        //
        // 조치: AjinDigitalOutput 은 실보드에서만 생성된다. 파일이 넣은 IsSimulationMode 는
        //       근거 없는 값으로 보고 기동 시 false 로 되돌린다. 기동 시 1회만 강제하므로
        //       운전 중 IO 화면에서의 임시 시뮬 전환은 그대로 쓸 수 있다(재시작 시 실신호 복귀).
        //       IgnoreWaits 는 건드리지 않는다.
        //
        // 실장비 확인: 기동 로그에서 "IO-SIM-FORCE-REAL" 검색. 합계 0 이면 원래 깨끗한 상태다.
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
                    "실보드 DO 인데 Config 파일이 시뮬 모드로 지정했습니다. 실제 출력을 내도록 되돌립니다. name=" +
                    Name + ", M" + Setup.ModuleNo + "/B" + Setup.BitNo +
                    ", 파일=EquipmentData\\Config\\" + StorageKey + ".json - Check");
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
                "실보드 DO 시뮬 강제 해제 합계=" + count + "건 - " + (count == 0 ? "Ok" : "Check"));
            return count;
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
