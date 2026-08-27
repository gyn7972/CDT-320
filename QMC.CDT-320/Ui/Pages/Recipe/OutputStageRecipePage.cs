using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Equipment.Vision;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>Output Stage 레시피에서 OutputStageUnit(Good/Ng 스테이지)을 조작하는 화면입니다.</summary>
    public partial class OutputStageRecipePage : PageBase
    {
        private const int ManualCylinderTimeoutMs = 5000;
        private readonly string _titleI18n;
        private readonly Timer _refreshTimer = new Timer();
        private IDisposable _visionPreview;
        private OutputStageUnit _outputStageUnit;

        public OutputStageRecipePage() : this("recipe.outputStage")
        {
        }

        public OutputStageRecipePage(string titleI18n)
        {
            try
            {
                _titleI18n = titleI18n;
                InitializeComponent();
                if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                    return;

                InstallVisionPreview();
                ApplyTitle();
                ApplyRuntimeLayout();
                ConfigureRuntimeBehavior();
                ConfigureManualActions();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(ex.Message, "Output Stage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

                ResolveUnit();
                BindParameterGrids();
                BindIoPanel();
                BindJogPanel();
                RefreshView();
                if (ShouldRefreshVisible(this))
                    _refreshTimer.Start();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            try { if (ShouldRefreshVisible(this)) _refreshTimer.Start(); else _refreshTimer.Stop(); } catch { }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                _refreshTimer.Stop();
                DisposeVisionPreview();
                if (jogAxisMoveControl != null)
                    jogAxisMoveControl.StopAllAsync(true).GetAwaiter().GetResult();
            }
            catch
            {
            }
            finally
            {
                base.OnHandleDestroyed(e);
            }
        }

        private void InstallVisionPreview()
        {
            _visionPreview = RecipeVisionPreview.ShowSingle(visionPanel, "BIN VISION", VisionViewerPorts.Bin);
        }

        private void DisposeVisionPreview()
        {
            try { if (_visionPreview != null) _visionPreview.Dispose(); } catch { }
            _visionPreview = null;
        }

        private void ApplyTitle()
        {
            try
            {
                lblHeader.Tag = "i18n:" + _titleI18n;
                lblHeader.Text = Lang.T(_titleI18n);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void ApplyRuntimeLayout()
        {
            try
            {
                // 신형 컨트롤 z-order(레거시 위로)만 런타임 유지한다.
                optionParameterGrid.BringToFront();
                waitParameterGrid.BringToFront();
                ioCylinderPanel.BringToFront();
                jogCommonLayout.BringToFront();
                jogSpeedControl.BringToFront();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ConfigureRuntimeBehavior()
        {
            try
            {
                _refreshTimer.Interval = 250;
                _refreshTimer.Tick += RefreshTimer_Tick;
                optionParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
                waitParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
                BindParameterGridMenus();

                jogAxisMoveControl.SpeedControl = jogSpeedControl;
                jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.OutputStagePad;
                jogAxisMoveControl.AxisColumnsPerRow = 2;
                jogAxisMoveControl.ShowCurrentSpeedMode = true;
                jogAxisMoveControl.ButtonAreaMinHeight = 420;
                jogAxisMoveControl.ButtonAreaMaxHeight = 460;
                jogAxisMoveControl.ButtonAreaMinWidth = 170;
                jogAxisMoveControl.ButtonAreaMaxWidth = 320;
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!ShouldRefreshVisible(this))
                {
                    _refreshTimer.Stop();
                    return;
                }

                RefreshView();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ResolveUnit()
        {
            try
            {
                var machine = FindMachine();
                _outputStageUnit = machine != null ? machine.OutputStageUnit : null;
                if (_outputStageUnit != null)
                    _outputStageUnit.Recipe.EnsurePositionObjects();
                SetEnabledState(_outputStageUnit != null);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private CDT320_Machine FindMachine()
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    var host = form as Form1;
                    if (host != null)
                        return host.Machine;
                }

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private Form1 FindHostForm()
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    var host = form as Form1;
                    if (host != null)
                        return host;
                }

                return FindForm() as Form1;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private void SetOutputStageResultRoutingMode(
            OutputStageUnit unit,
            OutputStageResultRoutingMode mode)
        {
            if (unit == null || unit.Config == null)
                return;

            Form1 host = FindHostForm();
            if (host != null && host.Controller != null)
            {
                EquipmentStatus status = host.Controller.Status;
                bool busy =
                    status == EquipmentStatus.AutoRunning ||
                    status == EquipmentStatus.ManualRunning ||
                    status == EquipmentStatus.Initializing ||
                    host.Controller.IsSequenceRunning ||
                    host.Controller.IsManualBusy;
                if (busy)
                {
                    const string message = "자동/수동 시퀀스 또는 초기화 중에는 Die 결과 배출 모드를 변경할 수 없습니다.";
                    EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", message);
                    QMC.Common.MessageDialog.Show(
                        this,
                        message,
                        "Output Stage Config",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            unit.Config.ResultRoutingMode = mode;
        }

        private void SetEnabledState(bool enabled)
        {
            try
            {
                foreach (Control control in new Control[] { grpOptions, grpWait, grpManual, grpIo, grpVision, grpJog, grpSpeed })
                    control.Enabled = enabled;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void BindParameterGrids()
        {
            try
            {
                if (_outputStageUnit == null)
                    return;

                _outputStageUnit.Recipe.EnsurePositionObjects();
                var unit = _outputStageUnit;
                var items = new List<ParameterGridItem>();

                // Recipe 위치 — 위치 종류별 접이식 그룹 (멤버 = GOOD Y/Z, NG Y, VISION X)
                AddKindGroup(items, "AVOID POSITION", "Avoid", true, true, true, true);
                AddKindGroup(items, "LOAD POSITION", "Load", true, true, true, false);
                AddKindGroup(items, "PROCESS POSITION", "Process", true, true, true, true);
                AddKindGroup(items, "UNLOAD POSITION", "Unload", true, true, true, false);
                AddKindGroup(items, "RETICLE POSITION", "Reticle", false, true, false, true);
                AddKindGroup(items, "BARCODE POSITION", "Barcode", true, false, true, true);

                // 빈맵(원형) 형상은 BIN DIE MAP CREATE 페이지에서 레시피 맵으로 저장/관리한다.

                items.Add(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v));
                items.Add(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v));
                items.Add(ParameterGridItem.Selection<OutputStageResultRoutingMode>(
                    "DIE RESULT ROUTING MODE",
                    "mode",
                    ParameterGridScope.Config,
                    () => unit.Config.ResultRoutingMode,
                    v => SetOutputStageResultRoutingMode(unit, v)));

                // 바코드 설정은 설정(Settings) → 바코드 화면 한 곳에서만 관리한다(레시피 쪽에 두지 않는다).

                optionParameterGrid.SetItems(items);

                waitParameterGrid.SetItems(new ParameterGridItem[0]);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "BindParameterGrids failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        // 한 위치 종류(kind)를 헤더로 묶고, 그 종류를 가진 축들을 멤버로 추가
        private void AddKindGroup(List<ParameterGridItem> items, string kindLabel, string kind, bool goodY, bool goodZ, bool ng, bool vision)
        {
            string groupKey = "K_" + kind.ToUpperInvariant();
            ParameterGridItem header = ParameterGridItem.Header(kindLabel, groupKey);
            if (string.Equals(kind, "Reticle", StringComparison.OrdinalIgnoreCase))
                header.Description = "Vision Cal Position";
            items.Add(header);

            // [바코드 위치 Config 이관 2026-08-26 팀장님 지시] Barcode 판독 위치는 기구 좌표라
            // Config 스코프로 분리한다 — getter는 유닛 깔때기(Config 우선, 0이면 구 Recipe 폴백)를
            // 타서 표시값=시퀀스 실사용값이 일치하고, setter/TEACH는 Config에 기록된다.
            if (string.Equals(kind, "Barcode", StringComparison.OrdinalIgnoreCase))
            {
                if (goodY) items.Add(BarcodeMember("GOOD Y", groupKey, kindLabel, _outputStageUnit.GoodStage.StageY, BinStageAxis.GoodBinY,
                    v => _outputStageUnit.Config.GoodStageYBarcodePosition = v));
                if (ng) items.Add(BarcodeMember("NG Y", groupKey, kindLabel, _outputStageUnit.NgStage.StageY, BinStageAxis.NgBinY,
                    v => _outputStageUnit.Config.NGStageYBarcodePosition = v));
                if (vision) items.Add(BarcodeMember("VISION X", groupKey, kindLabel, _outputStageUnit.OutputCameraX, BinStageAxis.VisionX,
                    v => _outputStageUnit.Config.VisionXBarcodePosition = v));
                AddBarcodeCameraSettingItems(items, groupKey);
                return;
            }

            // LoadRecipe/LoadSettings가 Recipe 객체를 교체하므로 지역 캡처 대신 매 호출 시 라이브 Recipe를 따라간다.
            if (goodY) items.Add(StageMember("GOOD Y", groupKey, kindLabel, kind, _outputStageUnit.GoodStage.StageY, () => _outputStageUnit.Recipe.GoodStageY));
            if (goodZ) items.Add(StageMember("GOOD Z", groupKey, kindLabel, kind, _outputStageUnit.GoodStage.StageZ, () => _outputStageUnit.Recipe.GoodStageZ));
            if (ng) items.Add(StageMember("NG Y", groupKey, kindLabel, kind, _outputStageUnit.NgStage.StageY, () => _outputStageUnit.Recipe.NGStageY));
            if (vision) items.Add(StageMember("VISION X", groupKey, kindLabel, kind, _outputStageUnit.OutputCameraX, () => _outputStageUnit.Recipe.VisionX));
        }

        // [바코드 위치 Config 이관 2026-08-26] Barcode 위치 그리드 행 — Config 스코프, MOVE/TEACH 유지.
        private ParameterGridItem BarcodeMember(string axisLabel, string groupKey, string kindLabel, BaseAxis axis, BinStageAxis binAxis, Action<double> configSetter)
        {
            var item = AxisDouble(axisLabel, ParameterGridScope.Config, axis,
                () => _outputStageUnit.GetStageTeachingPosition(binAxis, "Barcode"),
                configSetter);
            item.Key = axisLabel + " " + kindLabel;   // 이동/티칭 조회는 전체 이름(Key)으로 파싱
            item.GroupKey = groupKey;
            item.SupportsTeaching = true;             // 행에 MOVE/TEACH 버튼 표시(티칭 포지션)
            return item;
        }

        // [카메라 바코드 2026-08-26 팀장님 지시] 판독 소스·카메라 파라미터를 BARCODE POSITION 그룹에 노출.
        // 저장소는 AppSettings(Config\settings.json) — 유닛 Config/Recipe가 아니므로 setter에서 즉시
        // AppSettingsStore.Save()로 영속한다(그리드 공통 저장 SaveCurrentSettingsData는 유닛 설정만 저장).
        private void AddBarcodeCameraSettingItems(List<ParameterGridItem> items, string groupKey)
        {
            ParameterGridItem useCamera = ParameterGridItem.Bool("BARCODE USE CAMERA", ParameterGridScope.Config,
                () => AppSettingsStore.Current != null && AppSettingsStore.Current.OutputBarcodeUseCamera,
                v => SaveBarcodeCameraAppSetting(s => s.OutputBarcodeUseCamera = v));
            useCamera.GroupKey = groupKey;
            useCamera.Description = "Output Bin 바코드 판독 소스입니다.\r\nOFF = 시리얼 리더(기존 경로 그대로), ON = BIN 카메라 2샷(바코드 티칭 위치 ± 샷 오프셋 촬영 후 비전 PC BinBarcodeReader 디코드).\r\n바코드 사용 자체의 ON/OFF는 Settings > Device의 Output 바코드 사용 체크와 별개입니다.";
            items.Add(useCamera);

            ParameterGridItem shotOffset = ParameterGridItem.Double("BARCODE CAMERA SHOT OFFSET", "mm (0.000)", ParameterGridScope.Config,
                () => AppSettingsStore.Current != null ? AppSettingsStore.Current.OutputBarcodeCameraShotOffsetMm : 5.0,
                v => SaveBarcodeCameraAppSetting(s => s.OutputBarcodeCameraShotOffsetMm =
                    double.IsNaN(v) || double.IsInfinity(v) || v <= 0.0 ? 5.000 : Math.Min(50.000, v)));
            shotOffset.GroupKey = groupKey;
            shotOffset.Description = "카메라 2샷 판독 시 바코드 티칭 위치 기준 StageY ± 샷 오프셋(mm)입니다. 기본 5.000, 허용 범위 0 초과 ~ 50(범위 밖 입력은 클램프).";
            items.Add(shotOffset);

            ParameterGridItem resultTimeout = ParameterGridItem.Int("BARCODE CAMERA RESULT TIMEOUT", "ms", ParameterGridScope.Config,
                () => AppSettingsStore.Current != null ? AppSettingsStore.Current.OutputBarcodeCameraResultTimeoutMs : 5000,
                v => SaveBarcodeCameraAppSetting(s => s.OutputBarcodeCameraResultTimeoutMs = Math.Max(500, Math.Min(60000, v))));
            resultTimeout.GroupKey = groupKey;
            resultTimeout.Description = "카메라 2샷 EPD 완료 후 집계 RESULT 대기 상한(ms)입니다. 기본 5000, 허용 범위 500 ~ 60000(범위 밖 입력은 클램프).";
            items.Add(resultTimeout);
        }

        private static void SaveBarcodeCameraAppSetting(Action<AppSettings> apply)
        {
            AppSettings settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
            apply(settings);
            AppSettingsStore.Save();
        }

        private ParameterGridItem StageMember(string axisLabel, string groupKey, string kindLabel, string kind, BaseAxis axis, Func<StageAxisPositions> set)
        {
            Func<double> getter;
            Action<double> setter;
            switch (kind)
            {
                // Avoid 위치 레시피 연결
                case "Avoid": getter = () => set().AvoidPosition; setter = v => set().AvoidPosition = v; break;
                // Load 위치 레시피 연결
                case "Load": getter = () => set().LoadPosition; setter = v => set().LoadPosition = v; break;
                // Process 위치 레시피 연결
                case "Process": getter = () => set().ProcessPosition; setter = v => set().ProcessPosition = v; break;
                // Unload 위치 레시피 연결
                case "Unload": getter = () => set().UnloadPosition; setter = v => set().UnloadPosition = v; break;
                // Reticle 위치 레시피 연결
                case "Reticle": getter = () => set().ReticlePosition; setter = v => set().ReticlePosition = v; break;
                // Barcode 판독 위치는 Config 이관(2026-08-26)으로 BarcodeMember 경로만 사용한다.
                default: getter = () => 0.0; setter = v => { }; break;
            }

            var item = AxisDouble(axisLabel, ParameterGridScope.Recipe, axis, getter, setter);
            item.Key = axisLabel + " " + kindLabel;   // 이동/티칭 조회는 전체 이름(Key)으로 파싱
            item.GroupKey = groupKey;
            item.SupportsTeaching = true;             // 행에 MOVE/TEACH 버튼 표시(티칭 포지션)
            return item;
        }

        // 매뉴얼 액션 버튼(Designer 배치)의 Click 핸들러 — 좌측 GOOD / 우측 NG, 인터락 시퀀스로 이동
        private void ConfigureManualActions()
        {
            try
            {
                // 공용 MANUAL ACTION 판넬에 GOOD/NG/VISION 이동 버튼 등록 (2열, 행 수 자동)
                manualActionPanel.ColumnCount = 2;
                manualActionPanel.SetItems(new[]
                {
                    ManualActionItem.Create("GOOD AVOID POSITION", () => ConfirmAndRunAsync("GOOD AVOID POSITION", () => MoveBinSequenceAsync(BinSide.Good, "Avoid"))),
                    ManualActionItem.Create("NG AVOID POSITION", () => ConfirmAndRunAsync("NG AVOID POSITION", () => MoveBinSequenceAsync(BinSide.Ng, "Avoid"))),
                    ManualActionItem.Create("GOOD LOAD POSITION", () => ConfirmAndRunAsync("GOOD LOAD POSITION", () => MoveBinSequenceAsync(BinSide.Good, "Load"))),
                    ManualActionItem.Create("NG LOAD POSITION", () => ConfirmAndRunAsync("NG LOAD POSITION", () => MoveBinSequenceAsync(BinSide.Ng, "Load"))),
                    ManualActionItem.Create("GOOD PROCESS POSITION", () => ConfirmAndRunAsync("GOOD PROCESS POSITION", () => MoveBinSequenceAsync(BinSide.Good, "Process"))),
                    ManualActionItem.Create("NG PROCESS POSITION", () => ConfirmAndRunAsync("NG PROCESS POSITION", () => MoveBinSequenceAsync(BinSide.Ng, "Process"))),
                    ManualActionItem.Create("GOOD UNLOAD POSITION", () => ConfirmAndRunAsync("GOOD UNLOAD POSITION", () => MoveBinSequenceAsync(BinSide.Good, "Unload"))),
                    ManualActionItem.Create("NG UNLOAD POSITION", () => ConfirmAndRunAsync("NG UNLOAD POSITION", () => MoveBinSequenceAsync(BinSide.Ng, "Unload"))),
                    ManualActionItem.Create("VISION AVOID POSITION", () => ConfirmAndRunAsync("VISION AVOID POSITION", () => MoveVisionSequenceAsync("Avoid"))),
                    ManualActionItem.Create("VISION PROCESS POSITION", () => ConfirmAndRunAsync("VISION PROCESS POSITION", () => MoveVisionSequenceAsync("Process"))),
                    ManualActionItem.Create("VISION RETICLE POSITION", () => ConfirmAndRunAsync("VISION RETICLE POSITION", () => MoveVisionSequenceAsync("Reticle"))),
                    // [카메라 바코드 테스트 2026-08-27 팀장님 지시] 바코드 판독 단독 테스트 —
                    // BARCODE USE CAMERA 옵션에 따라 카메라 2샷 또는 시리얼 리더로 읽는다.
                    ManualActionItem.Create("GOOD BARCODE READ", () => ConfirmAndRunAsync("GOOD BARCODE READ", () => ReadBinBarcodeManualAsync(BinSide.Good))),
                    ManualActionItem.Create("NG BARCODE READ", () => ConfirmAndRunAsync("NG BARCODE READ", () => ReadBinBarcodeManualAsync(BinSide.Ng)))
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "ConfigureManualActions failed: " + ex.Message);
            }
            finally
            {
            }
        }
        private async void btnGoodAvoidPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("GOOD AVOID POSITION", () => MoveBinSequenceAsync(BinSide.Good, "Avoid"));
        }

        private async void btnNgAvoidPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("NG AVOID POSITION", () => MoveBinSequenceAsync(BinSide.Ng, "Avoid"));
        }

        private async void btnGoodLoadPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("GOOD LOAD POSITION", () => MoveBinSequenceAsync(BinSide.Good, "Load"));
        }

        private async void btnNgLoadPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("NG LOAD POSITION", () => MoveBinSequenceAsync(BinSide.Ng, "Load"));
        }

        private async void btnGoodProcessPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("GOOD PROCESS POSITION", () => MoveBinSequenceAsync(BinSide.Good, "Process"));
        }

        private async void btnNgProcessPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("NG PROCESS POSITION", () => MoveBinSequenceAsync(BinSide.Ng, "Process"));
        }

        private async void btnGoodUnloadPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("GOOD UNLOAD POSITION", () => MoveBinSequenceAsync(BinSide.Good, "Unload"));
        }

        private async void btnNgUnloadPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("NG UNLOAD POSITION", () => MoveBinSequenceAsync(BinSide.Ng, "Unload"));
        }

        private async void btnVisionAvoidPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("VISION AVOID POSITION", () => MoveVisionSequenceAsync("Avoid"));
        }

        private async void btnVisionProcessPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("VISION PROCESS POSITION", () => MoveVisionSequenceAsync("Process"));
        }

        private async void btnVisionReticlePosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("VISION RETICLE POSITION", () => MoveVisionSequenceAsync("Reticle"));
        }

        // ===================== 인터락 시퀀스 (OutputStage) =====================
        // 규칙: Y 이동 전 해당 빈 Z = Avoid · C(해당 빈 클램프리프트 Up) · P(Front/Rear 픽커 Z 전부 상승)
        // 홈게이트 = AVOID/LOAD/UNLOAD (PROCESS 제외) · 공유레일/알람은 이동 메서드 내부 자동검사
        // PROCESS는 VisionX 동반 이동 유지. NG의 Z 교시는 NgStage.Recipe(Work/AvoidPositionZ) 사용.

        // 마지막 시퀀스 중단 사유 — 실행 래퍼(ConfirmAndRunAsync)의 실패 팝업에 합쳐서 표시
        private string _lastAbortReason;

        // ── [카메라 바코드 테스트 2026-08-27 팀장님 지시] MANUAL ACTION 바코드 판독 ─────────────
        // 자동 시퀀스(OutputFeederLoadToStageSequence.RunBarcodeSequence)와 동일 순서를 수동으로 1회:
        // VisionX → Barcode 티칭(리더/카메라 공통, 자동 경로 미러) → StageY → Barcode 티칭 →
        //   [리더]  현 위치에서 BinBarcodeReader.ReadAsync 1회
        //   [카메라] StageY 티칭+샷오프셋 → ch0 샷/EPD → 티칭−샷오프셋 → ch1 샷/EPD →
        //            집계 RESULT(BIN_BARCODE) 회수 — 프로토콜 공용부(BinBarcodeCameraReader) 사용.
        // 이동은 전부 기존 수동 이동 시퀀스/헬퍼(MoveBinSequenceAsync·MoveStageAxisWithSelectedSpeedAsync,
        // 가드·속도 선택 포함) 재사용 — 신규 이동 코드 없음. 판독 결과는 다이얼로그로 표시하고 0을
        // 반환한다(이동/전제 실패만 AbortSeq로 공통 실패 처리).
        private async Task<int> ReadBinBarcodeManualAsync(BinSide side)
        {
            string title = (side == BinSide.Ng ? "NG" : "GOOD") + " BARCODE READ";
            if (_outputStageUnit == null)
                return -1;

            AppSettings settings = AppSettingsStore.Current ?? new AppSettings();
            bool useCamera = settings.OutputBarcodeUseCamera;

            // 1) VisionX → Barcode 티칭 위치 (자동 시퀀스 공통부 미러 — 공유레일 클리어 가드 포함)
            // [바코드 테스트 속도 2026-08-27 팀장님 지시] VisionX는 조그 속도(FINE 10mm/s) 대신
            // 수동 시퀀스 속도(%)로 이동 — 원거리(수백 mm) 이동이 너무 느린 문제 해소.
            int r = await MoveVisionBarcodeWithManualSequenceSpeedAsync().ConfigureAwait(true);
            if (r != 0)
                return r;

            // 2) StageY → Barcode 티칭 위치 (홈/클램프/피더/반대편 Z 가드 포함)
            r = await MoveBinSequenceAsync(side, "Barcode").ConfigureAwait(true);
            if (r != 0)
                return r;

            if (!useCamera)
                return await ReadBinBarcodeByReaderManualAsync(side, title, settings).ConfigureAwait(true);

            return await ReadBinBarcodeByCameraManualAsync(side, title, settings).ConfigureAwait(true);
        }

        private async Task<int> ReadBinBarcodeByReaderManualAsync(BinSide side, string title, AppSettings settings)
        {
            var host = FindHostForm();
            IBarcodeReader reader = host != null && host.Machine != null
                ? host.Machine.BinBarcodeReader
                : null;
            if (reader == null)
                return AbortSeq(title, "Output Bin barcode reader가 구성되지 않았습니다.");

            try
            {
                if (!reader.IsConnected && (!reader.TryOpen() || !reader.IsConnected))
                    return AbortSeq(title, "barcode reader 연결 실패. reader=" + reader.ReaderName);

                int timeoutMs = settings.OutputBarcodeReadTimeoutMs > 0
                    ? settings.OutputBarcodeReadTimeoutMs
                    : 3000;
                string value = await reader.ReadAsync(timeoutMs).ConfigureAwait(true);
                bool ok = !string.IsNullOrWhiteSpace(value);
                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageRecipePage",
                    title + " 리더 판독 결과. reader=" + reader.ReaderName +
                    ", barcode=" + (ok ? value : "-") + (ok ? " - Ok" : " - Check"));
                QMC.Common.MessageDialog.Show(this,
                    ok
                        ? title + " 성공 (리더)\r\nbarcode = " + value
                        : title + " 판독 실패/타임아웃 (리더)\r\nreader = " + reader.ReaderName +
                          ", timeoutMs = " + timeoutMs,
                    title, MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                return 0;
            }
            catch (Exception ex)
            {
                return AbortSeq(title, "리더 판독 예외: " + ex.Message);
            }
        }

        private async Task<int> ReadBinBarcodeByCameraManualAsync(BinSide side, string title, AppSettings settings)
        {
            string visionReason;
            if (!QMC.CDT320.VisionComm.BinBarcodeCameraReader.IsVisionReady(out visionReason))
                return AbortSeq(title, visionReason);

            BinStageAxis yAxis = BinYAxis(side);
            double baseY = _outputStageUnit.GetStageTeachingPosition(yAxis, "Barcode");
            double shotOffsetMm = Math.Abs(settings.OutputBarcodeCameraShotOffsetMm);
            int epdTimeoutMs = settings.OutputBarcodeReadTimeoutMs > 0
                ? settings.OutputBarcodeReadTimeoutMs
                : 3000;
            int resultTimeoutMs = settings.OutputBarcodeCameraResultTimeoutMs > 0
                ? settings.OutputBarcodeCameraResultTimeoutMs
                : 5000;

            string groupId = QMC.CDT320.VisionComm.BinBarcodeCameraReader.NewGroupId();
            string headSide = side == BinSide.Ng ? "NG" : "GOOD";
            QMC.CDT320.VisionComm.VisionRequestHandle lastHandle = null;
            for (int shot = 0; shot <= 1; shot++)
            {
                double shotTarget = shot == 0 ? baseY + shotOffsetMm : baseY - shotOffsetMm;
                int moveResult = await MoveStageAxisWithSelectedSpeedAsync(yAxis, shotTarget).ConfigureAwait(true);
                if (moveResult != 0)
                    return AbortSeq(title, "StageY 샷 위치 이동 실패. shot=" + shot +
                        ", target=" + shotTarget.ToString("F3"));

                QMC.CDT320.VisionComm.VisionRequestHandle handle =
                    await QMC.CDT320.VisionComm.BinBarcodeCameraReader.SendShotAsync(
                        headSide, shot, groupId, string.Empty, epdTimeoutMs,
                        System.Threading.CancellationToken.None).ConfigureAwait(true);
                if (handle == null)
                    return AbortSeq(title, "샷 요청/EPD 실패. shot=" + shot + ", groupId=" + groupId);

                lastHandle = handle;
            }

            QMC.CDT320.VisionComm.BinBarcodeCameraResult result =
                await QMC.CDT320.VisionComm.BinBarcodeCameraReader.WaitAggregateResultAsync(
                    lastHandle, resultTimeoutMs,
                    System.Threading.CancellationToken.None).ConfigureAwait(true);

            QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageRecipePage",
                title + " 카메라 2샷 판독 결과. status=" + (string.IsNullOrEmpty(result.Status) ? "-" : result.Status) +
                ", barcode=" + (string.IsNullOrEmpty(result.Barcode) ? "-" : result.Barcode) +
                ", decodedShot=" + (string.IsNullOrEmpty(result.DecodedShot) ? "-" : result.DecodedShot) +
                ", failReason=" + (string.IsNullOrWhiteSpace(result.FailReason) ? "-" : result.FailReason) +
                ", groupId=" + result.GroupId + (result.Pass ? " - Ok" : " - Check"));

            QMC.Common.MessageDialog.Show(this,
                result.Pass
                    ? title + " 성공 (카메라 2샷)\r\nbarcode = " + result.Barcode +
                      "\r\ndecodedShot = " + result.DecodedShot
                    : title + " 판독 실패 (카메라 2샷)\r\nstatus = " +
                      (string.IsNullOrEmpty(result.Status) ? "-" : result.Status) +
                      "\r\nfailReason = " + (string.IsNullOrWhiteSpace(result.FailReason) ? "-" : result.FailReason),
                title, MessageBoxButtons.OK, result.Pass ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            return 0;
        }

        private int AbortSeq(string title, string message)
        {
            QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Error, "OUTPUT-STAGE", "UI", title + " 시퀀스 중단: " + message);
            _lastAbortReason = message;
            return -1;
        }

        private static BinStageAxis BinYAxis(BinSide side) { return side == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY; }
        private static BinStageAxis BinZAxis(BinSide side) { return side == BinSide.Ng ? BinStageAxis.NgBinZ : BinStageAxis.GoodBinZ; }

        private double GetBinZAvoidTarget(BinSide side)
        {
            return side == BinSide.Ng
                ? _outputStageUnit.NgStage.Recipe.AvoidPositionZ
                : _outputStageUnit.GetStageTeachingPosition(BinStageAxis.GoodBinZ, "Avoid");
        }

        // C: 해당 빈 클램프리프트 Up
        private bool CheckClampUp(BinSide side, out string reason)
        {
            reason = string.Empty;
            if (_outputStageUnit.IsBinGuideClampLiftUp(side))
                return true;
            reason = (side == BinSide.Ng ? "NG" : "GOOD") + " 클램프리프트 Up 미완료";
            return false;
        }

        // P: Front/Rear 픽커 Z 전부 상승(빈 위 간섭 차단)
        private bool CheckPickerZClear(out string reason)
        {
            reason = string.Empty;
            var machine = FindMachine();
            if (machine == null)
                return true;

            string block = machine.PickerFrontUnit != null ? machine.PickerFrontUnit.GetPickerZClearBlockReason() : null;
            if (block != null) { reason = "Front 픽커 " + block; return false; }

            block = machine.PickerRearUnit != null ? machine.PickerRearUnit.GetPickerZClearBlockReason() : null;
            if (block != null) { reason = "Rear 픽커 " + block; return false; }
            return true;
        }

        // VISION X(공유레일) 이동 전: Front/Rear 픽커가 Avoid 위치(Clear)인지 확인.
        // (InputStage의 CheckVisionXClear와 동일 기준 — 공유레일 충돌 방지)
        private bool CheckVisionXClear(out string reason)
        {
            reason = string.Empty;
            var machine = FindMachine();
            if (machine == null)
                return true;

            if (machine.PickerFrontUnit != null && !machine.PickerFrontUnit.IsPickerInAvoidPosition())
            { reason = "Front 픽커 Avoid 위치 아님"; return false; }
            if (machine.PickerRearUnit != null && !machine.PickerRearUnit.IsPickerInAvoidPosition())
            { reason = "Rear 픽커 Avoid 위치 아님"; return false; }
            return true;
        }

        // 홈게이트: 해당 빈 Y/Z 원점복귀 완료 (Z축이 없는 스테이지는 Z 생략 — 예: NG는 Y 전용)
        private bool CheckBinAxesHomed(BinSide side, out string reason)
        {
            reason = string.Empty;
            if (!_outputStageUnit.IsStageAxisHomeDone(BinYAxis(side))) { reason = BinYAxis(side) + " 원점복귀 필요"; return false; }
            if (_outputStageUnit.HasStageAxis(BinZAxis(side)) && !_outputStageUnit.IsStageAxisHomeDone(BinZAxis(side)))
            { reason = BinZAxis(side) + " 원점복귀 필요"; return false; }
            return true;
        }

        private async Task<int> EnsureOutputFeederSafeBeforeStageMoveAsync(string title)
        {
            var machine = FindMachine();
            var feeder = machine != null ? machine.OutputFeederUnit : null;
            if (feeder == null)
                return AbortSeq(title, "OutputFeederUnit 없음");

            if (!feeder.IsFeederUnclamped())
            {
                int clampResult = await feeder.SetFeederClampAsync(false, ManualCylinderTimeoutMs).ConfigureAwait(true);
                if (clampResult != 0)
                    return AbortSeq(title, "OutputFeeder Unclamp 실패. result=" + clampResult + ", " + feeder.DescribeFeederCylinderState());
            }

            if (!feeder.IsFeederUnclamped())
                return AbortSeq(title, "OutputFeeder Unclamp 최종 확인 실패. " + feeder.DescribeFeederCylinderState());

            if (!feeder.FeederY.IsAtTargetPosition(feeder.Recipe.AvoidPosition, 0.0))
            {
                int moveResult = await feeder.MoveToFeederAvoidPosition(
                    jogAxisMoveControl.SelectedSpeedType,
                    jogAxisMoveControl.GetSelectedSpeed(feeder.FeederY)).ConfigureAwait(true);
                if (moveResult != 0)
                    return AbortSeq(title, "OutputFeederY Avoid 이동 실패. result=" + moveResult + ", " + feeder.DescribeBinFeederYMoveDoneState() + feeder.DescribeBinFeederYLastMotionFailure());

                // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
            }

            if (!feeder.IsBinFeederYInAvoidPosition())
                return AbortSeq(title, "OutputFeederY Avoid 최종 확인 실패. " + feeder.DescribeBinFeederYMoveDoneState());

            return 0;
        }

        private async Task<int> EnsureBinStageZSafeBeforeYAsync(BinSide side, string title, string context)
        {
            if (!_outputStageUnit.HasStageAxis(BinZAxis(side)))
                return 0;

            double zAvoid = GetBinZAvoidTarget(side);
            int result = await MoveStageAxisWithSelectedSpeedAsync(BinZAxis(side), zAvoid).ConfigureAwait(true);
            if (result != 0)
                return AbortSeq(title, context + " Z Avoid 이동 실패");

            if (!_outputStageUnit.IsStageAxisAtPosition(BinZAxis(side), zAvoid))
                return AbortSeq(title, context + " Y 이동 전 Z Avoid 미확인");

            return 0;
        }

        private async Task<int> EnsureOppositeStageZSafeBeforeYAsync(BinSide side, string title)
        {
            BinSide opposite = side == BinSide.Ng ? BinSide.Good : BinSide.Ng;
            return await EnsureBinStageZSafeBeforeYAsync(opposite, title, "반대쪽 " + (opposite == BinSide.Ng ? "NG" : "GOOD")).ConfigureAwait(true);
        }

        private async Task<int> EnsureGoodGuideDownBeforeNgYMoveAsync(string title)
        {
            int result = await _outputStageUnit.EnsureBinGuideDownAsync(BinSide.Good, ManualCylinderTimeoutMs).ConfigureAwait(true);
            if (result != 0)
                return AbortSeq(title, "NG Y 이동 전 Good Bin Guide Down 실패. result=" + result + ", " + _outputStageUnit.DescribeOutputStageInterlockState(BinSide.Ng));

            if (!_outputStageUnit.IsBinGuideDown(BinSide.Good))
                return AbortSeq(title, "NG Y 이동 전 Good Bin Guide Down 최종 확인 실패. " + _outputStageUnit.DescribeOutputStageInterlockState(BinSide.Ng));

            return 0;
        }

        private async Task<int> EnsureGoodProcessOppositeStageClearAsync(string title)
        {
            int result = await MoveStageTeachingPositionWithSelectedSpeedAsync(BinStageAxis.NgBinY, "Avoid").ConfigureAwait(true);
            if (result != 0)
                return AbortSeq(title, "GOOD Process 전 NG Y Avoid 이동 실패");

            if (!_outputStageUnit.IsStageAxisAtPosition(BinStageAxis.NgBinY, _outputStageUnit.Recipe.NGStageY.AvoidPosition))
                return AbortSeq(title, "GOOD Process 전 NG Y Avoid 최종 확인 실패");

            result = await _outputStageUnit.EnsureBinGuideClampLiftUpAsync(BinSide.Ng, ManualCylinderTimeoutMs).ConfigureAwait(true);
            if (result != 0)
                return AbortSeq(title, "GOOD Process 전 NG Clamp Lift Up 실패. result=" + result + ", " + _outputStageUnit.DescribeOutputStageInterlockState(BinSide.Good));

            if (!_outputStageUnit.IsBinGuideClampLiftUp(BinSide.Ng))
                return AbortSeq(title, "GOOD Process 전 NG Clamp Lift Up 최종 확인 실패. " + _outputStageUnit.DescribeOutputStageInterlockState(BinSide.Good));

            return 0;
        }

        // GOOD/NG 빈 공통 시퀀스: 게이트 → Feeder 안전 → 반대 Z Avoid → 대상 Z Avoid → Y → 종류 → (종류별 Z) → (Process면 VisionX)
        private async Task<int> MoveBinSequenceAsync(BinSide side, string kind)
        {
            if (_outputStageUnit == null)
                return -1;

            string title = (side == BinSide.Ng ? "NG " : "GOOD ") + kind.ToUpperInvariant();
            string reason;
            int r;
            bool isAvoidMove = string.Equals(kind, "Avoid", StringComparison.OrdinalIgnoreCase);

            // 이동 대상 축(해당 빈 Y/Z)의 HOME END(IsHomeDone) 미완료면 차단.
            if (!CheckBinAxesHomed(side, out reason))
                return AbortSeq(title, reason);
            if (!isAvoidMove && !CheckClampUp(side, out reason))
                return AbortSeq(title, reason);
            if (!CheckPickerZClear(out reason))
                return AbortSeq(title, reason);

            r = await EnsureOutputFeederSafeBeforeStageMoveAsync(title).ConfigureAwait(true);
            if (r != 0) return r;

            r = await EnsureOppositeStageZSafeBeforeYAsync(side, title).ConfigureAwait(true);
            if (r != 0) return r;

            if (side == BinSide.Ng)
            {
                r = await EnsureGoodGuideDownBeforeNgYMoveAsync(title).ConfigureAwait(true);
                if (r != 0) return r;
            }

            bool barcodeNgAlreadyAvoid = false;
            if (side == BinSide.Good &&
                (string.Equals(kind, "Process", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(kind, "Barcode", StringComparison.OrdinalIgnoreCase)))
            {
                // [바코드 테스트 2026-08-27 팀장님 지시] GOOD 바코드 촬영도 Process와 동일하게,
                // NG빈이 촬영 자리를 가리고 있으면 먼저 NG Y를 Avoid로 치운다(이미 Avoid면 무동작).
                // NgY 이동 절대가드는 GoodZ가 정확한 Avoid/0 이하일 것을 요구하므로, NG 회피가 실제로
                // 필요할 때만 GoodZ Avoid를 선행한다. NgY가 이미 Avoid면(팀장님 지시 08-27) GoodZ
                // Avoid 동작 자체를 생략한다 — GoodY 이동은 GoodZ 높이와 무관(자동 Conti가 Z Process
                // 상태로 Y를 움직이는 기존 설계와 동일)하므로 Z 0↔Process 왕복이 사라진다.
                if (string.Equals(kind, "Barcode", StringComparison.OrdinalIgnoreCase))
                {
                    barcodeNgAlreadyAvoid = _outputStageUnit.IsStageAxisAtPosition(
                        BinStageAxis.NgBinY, _outputStageUnit.Recipe.NGStageY.AvoidPosition);
                    if (!barcodeNgAlreadyAvoid)
                    {
                        r = await EnsureBinStageZSafeBeforeYAsync(side, title, "대상 GOOD(NG 회피 선행)").ConfigureAwait(true);
                        if (r != 0) return r;
                    }
                }

                // NgY가 이미 Avoid면 헬퍼 내 NgY 이동은 자연 스킵되고, GoodY 이동 인터락이 요구하는
                // NG Clamp Lift Up 확보만 수행된다.
                r = await EnsureGoodProcessOppositeStageClearAsync(title).ConfigureAwait(true);
                if (r != 0) return r;
            }

            // [바코드 테스트 2026-08-27] GOOD Barcode는 대상 Z Avoid를 생략한다 — Y 이동 전에 Z를
            // Process로 정렬하므로(아래) Avoid 하강이 불필요하고, NgY 회피가 필요했던 케이스는 위
            // "NG 회피 선행" 블록이 이미 GoodZ Avoid를 수행했다. 그 외 kind/side는 기존 그대로.
            bool skipOwnZAvoidForBarcode = side == BinSide.Good &&
                string.Equals(kind, "Barcode", StringComparison.OrdinalIgnoreCase);
            if (!skipOwnZAvoidForBarcode)
            {
                r = await EnsureBinStageZSafeBeforeYAsync(side, title, "대상 " + (side == BinSide.Ng ? "NG" : "GOOD")).ConfigureAwait(true);
                if (r != 0) return r;
            }

            // [바코드 촬영 높이 2026-08-27 팀장님 지시] GOOD 바코드는 Y 이동 전에 GoodZ를 Process로
            // 정렬한다 — GoodY Barcode 이동 인터락이 GoodZ Avoid∥Process를 요구하므로 어중간한
            // 높이(빈 수령 직후 Load 등)면 Y가 차단되고(17:18 실측 -11), 판독(촬영) 높이도 어긋난다.
            // 위에서 NgY Avoid가 확보된 뒤라 GoodZ 상승 절대가드(NG 정확 Avoid 요구)도 통과한다.
            // 이미 Process면 무동작. 이 선행 정렬로 기존 "Y 도착 후 Z Process 복귀"는 제거.
            if (side == BinSide.Good && string.Equals(kind, "Barcode", StringComparison.OrdinalIgnoreCase))
            {
                r = await MoveStageTeachingPositionWithSelectedSpeedAsync(BinStageAxis.GoodBinZ, "Process");
                if (r != 0) return AbortSeq(title, "Z Process 정렬(바코드 촬영 높이) 실패");
            }

            // 3) Y → 종류 위치
            r = await MoveStageTeachingPositionWithSelectedSpeedAsync(BinYAxis(side), kind);
            if (r != 0) return AbortSeq(title, "Y 이동 실패");

            // 4) 종류별 Z 마무리 (Z축 있을 때만)
            if (_outputStageUnit.HasStageAxis(BinZAxis(side)) && string.Equals(kind, "Load", StringComparison.OrdinalIgnoreCase))
            {
                double zTarget = side == BinSide.Ng
                    ? _outputStageUnit.NgStage.Recipe.WorkPositionZ
                    : _outputStageUnit.GetStageTeachingPosition(BinStageAxis.GoodBinZ, "Load");
                r = await MoveStageAxisWithSelectedSpeedAsync(BinZAxis(side), zTarget);
                if (r != 0) return AbortSeq(title, "Z Load/Work 이동 실패");
            }
            else if (side == BinSide.Good &&
                     (string.Equals(kind, "Process", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(kind, "Unload", StringComparison.OrdinalIgnoreCase)))
            {
                r = await MoveStageTeachingPositionWithSelectedSpeedAsync(BinStageAxis.GoodBinZ, kind);
                if (r != 0) return AbortSeq(title, "Z " + kind + " 이동 실패");
            }
            // (GOOD Barcode의 Z Process 정렬은 Y 이동 전 선행으로 이동 — 2026-08-27 팀장님 지시)

            // 5) PROCESS: VisionX 동반 이동 (공유레일 → Front/Rear 픽커 Avoid 선행 확인)
            if (string.Equals(kind, "Process", StringComparison.OrdinalIgnoreCase))
            {
                if (!_outputStageUnit.IsStageAxisHomeDone(BinStageAxis.VisionX))
                    return AbortSeq(title, BinStageAxis.VisionX + " 원점복귀 필요");
                if (!CheckVisionXClear(out reason))
                    return AbortSeq(title, "VISION X 전 " + reason);
                r = await MoveStageTeachingPositionWithSelectedSpeedAsync(BinStageAxis.VisionX, "Process");
                if (r != 0) return AbortSeq(title, "VISION X 이동 실패 (공유레일 확인)");
            }

            return 0;
        }

        // VISION 단독: AVOID/PROCESS는 바로, RETICLE은 레티클 실린더 Clear 선행
        private async Task<int> MoveVisionSequenceAsync(string kind)
        {
            if (_outputStageUnit == null)
                return -1;

            string title = "VISION " + kind.ToUpperInvariant();
            string reason;

            // 이동 대상 축(VISION X)의 HOME END(IsHomeDone) 미완료면 차단.
            if (!_outputStageUnit.IsStageAxisHomeDone(BinStageAxis.VisionX))
                return AbortSeq(title, BinStageAxis.VisionX + " 원점복귀 필요");

            if (string.Equals(kind, "Reticle", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsReticleClear(out reason))
                    return AbortSeq(title, "VISION X 전 레티클 " + reason);

                int zResult = await MoveStageTeachingPositionWithSelectedSpeedAsync(BinStageAxis.GoodBinZ, "Reticle");
                if (zResult != 0)
                    return AbortSeq(title, "GOOD Z Reticle 이동 실패");
            }

            // 공유레일 → VISION X 이동 전 Front/Rear 픽커 Avoid 확인
            if (!CheckVisionXClear(out reason))
                return AbortSeq(title, "VISION X 전 " + reason);

            int r = await MoveStageTeachingPositionWithSelectedSpeedAsync(BinStageAxis.VisionX, kind);
            if (r != 0) return AbortSeq(title, "VISION X 이동 실패 (공유레일 확인)");
            return 0;
        }

        private Task<int> MoveStageTeachingPositionWithSelectedSpeedAsync(BinStageAxis axis, string positionName)
        {
            return _outputStageUnit.MoveStageAxisToTeachingPositionForManualAction(
                axis,
                positionName,
                jogAxisMoveControl.SelectedSpeedType,
                jogSpeedControl.SpeedPercent);
        }

        // [바코드 테스트 속도 2026-08-27 팀장님 지시] 바코드 테스트 전용 VisionX 이동 — 가드는
        // MoveVisionSequenceAsync와 동일(HOME END + 공유레일 클리어)하되, 속도만 조그 대신
        // 수동 시퀀스 속도(%)(DefaultVelocity × ManualSequencePercent, Manual Sequence 화면에서 조정)로
        // 이동한다. 이동 자체는 자동 바코드 경로와 같은 MoveVisionXToTargetAndVerifyAsync 재사용.
        // 다른 VISION AVOID/PROCESS/RETICLE 버튼은 기존 조그 속도 유지.
        private async Task<int> MoveVisionBarcodeWithManualSequenceSpeedAsync()
        {
            const string title = "VISION BARCODE";
            if (_outputStageUnit == null)
                return -1;

            string reason;
            if (!_outputStageUnit.IsStageAxisHomeDone(BinStageAxis.VisionX))
                return AbortSeq(title, BinStageAxis.VisionX + " 원점복귀 필요");
            if (!CheckVisionXClear(out reason))
                return AbortSeq(title, "VISION X 전 " + reason);

            double target = _outputStageUnit.GetStageTeachingPosition(BinStageAxis.VisionX, "Barcode");
            QMC.Common.Log.Write("Main", "SYSTEM", "OutputStageRecipePage",
                title + " VisionX 수동 시퀀스 속도 이동. target=" + target.ToString("F3") +
                ", manualSequencePercent=" + MotionSpeedScale.ManualSequencePercent.ToString("0.###") + " - Start");
            int r;
            using (MotionSpeedScale.BeginManualSequenceScale())
            {
                r = await _outputStageUnit.MoveVisionXToTargetAndVerifyAsync(
                    target, 60000, false, System.Threading.CancellationToken.None).ConfigureAwait(true);
            }
            if (r != 0)
                return AbortSeq(title, "VISION X 이동 실패 (공유레일 확인). result=" + r);
            return 0;
        }

        private Task<int> MoveStageAxisWithSelectedSpeedAsync(BinStageAxis axis, double target)
        {
            return _outputStageUnit.MoveStageAxisForManualAction(
                axis,
                target,
                jogAxisMoveControl.SelectedSpeedType,
                jogSpeedControl.SpeedPercent);
        }

        private BaseAxis ResolveOutputStageBaseAxis(BinStageAxis axis)
        {
            if (_outputStageUnit == null)
                return null;

            switch (axis)
            {
                case BinStageAxis.GoodBinY: return _outputStageUnit.GoodStage != null ? _outputStageUnit.GoodStage.StageY : null;
                case BinStageAxis.GoodBinZ: return _outputStageUnit.GoodStage != null ? _outputStageUnit.GoodStage.StageZ : null;
                case BinStageAxis.NgBinY: return _outputStageUnit.NgStage != null ? _outputStageUnit.NgStage.StageY : null;
                case BinStageAxis.VisionX: return _outputStageUnit.OutputCameraX;
                default: return null;
            }
        }

        // 레티클 실린더(승강/사이드슬라이드 전·후)가 모두 후퇴(Clear)인지 — 인풋스테이지와 동일 기준(공유 스테이션)
        private bool IsReticleClear(out string reason)
        {
            reason = string.Empty;
            var machine = FindMachine();
            if (machine == null || machine.VisionUnit == null)
                return true;

            var v = machine.VisionUnit;
            if (v.ReticleLift != null && v.ReticleLift.IsFwd) { reason = "ReticleLift 전개됨"; return false; }
            if (v.ReticleFrontSideSlide != null && v.ReticleFrontSideSlide.IsFwd) { reason = "ReticleSideSlideFront 전개됨"; return false; }
            if (v.ReticleRearSideSlide != null && v.ReticleRearSideSlide.IsFwd) { reason = "ReticleSideSlideRear 전개됨"; return false; }
            return true;
        }

        private void BindParameterGridMenus()
        {
            try
            {
                // 우클릭 메뉴 대신, 티칭 포지션 행의 MOVE/TEACH 버튼으로 이동/티칭 수행
                optionParameterGrid.ParameterMoveRequested += OptionParameterGrid_MoveRequested;
                optionParameterGrid.ParameterTeachRequested += OptionParameterGrid_TeachRequested;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "BindParameterGridMenus failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void OptionParameterGrid_MoveRequested(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e == null || e.Item == null)
                    return;

                BinStageAxis axis;
                string positionName;
                if (!TryGetSelectedTeachingPosition(out axis, out positionName))
                    return;

                // 이동 대상 축의 HOME END(IsHomeDone) 미완료면 차단.
                if (!_outputStageUnit.IsStageAxisHomeDone(axis))
                {
                    string homeMsg = e.Item.Key + " 불가: " + axis + " 축 HOME END(원점복귀)가 완료되지 않았습니다.";
                    QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Warning, "OUTPUT-STAGE", "UI", homeMsg);
                    QMC.Common.MessageDialog.Show(this, homeMsg, "Output Stage Move", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                await ConfirmAndRunMoveToPositionAsync(e.Item.Key, () => MoveStageTeachingPositionWithSelectedSpeedAsync(axis, positionName));
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "Move button failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void OptionParameterGrid_TeachRequested(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e == null || e.Item == null)
                    return;

                BinStageAxis axis;
                string positionName;
                if (!TryGetSelectedTeachingPosition(out axis, out positionName))
                    return;

                if (!ConfirmTeachPosition("Output Stage Teach", e.Item.Key))
                    return;

                _outputStageUnit.TeachStageAxisPosition(axis, positionName);
                // [바코드 위치 Config 이관 2026-08-26] Config 스코프 티칭(Barcode)은 Recipe가 아니라
                // 장비 설정으로 영속해야 한다 — 항목 스코프 기준으로 저장 경로를 분기한다.
                if (e.Item != null && e.Item.Scope != ParameterGridScope.Recipe)
                    SaveCurrentSettingsData();
                else
                    SaveCurrentRecipeData();
                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "Teach button failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private bool ConfirmTeachPosition(string title, string actionName)
        {
            string name = string.IsNullOrWhiteSpace(actionName) ? "Teach Position" : actionName;
            using (var dialog = new QMC.Common.MessageBoxYesNo())
            {
                dialog.ButtonGroupLabel = "TEACH";
                DialogResult result = dialog.ShowDialog(
                    title,
                    name + " 현재 위치로 티칭하시겠습니까?",
                    this,
                    new[] { "Yes", "No" });

                if (result == DialogResult.Yes)
                    return true;

                EventLogger.Write(EventKind.Event, "UI", "OUTPUT-STAGE", name + " teach canceled.");
                return false;
            }
        }

        private bool TryGetSelectedTeachingPosition(out BinStageAxis axis, out string positionName)
        {
            axis = BinStageAxis.GoodBinY;
            positionName = string.Empty;

            var item = optionParameterGrid.SelectedItem;
            string key = item != null ? item.Key : string.Empty;
            if (string.IsNullOrWhiteSpace(key) || !key.EndsWith(" POSITION", StringComparison.OrdinalIgnoreCase))
                return false;

            string name = key.Substring(0, key.Length - " POSITION".Length);
            if (name.StartsWith("GOOD Y ", StringComparison.OrdinalIgnoreCase))
            {
                axis = BinStageAxis.GoodBinY;
                positionName = name.Substring("GOOD Y ".Length);
                return true;
            }
            if (name.StartsWith("GOOD Z ", StringComparison.OrdinalIgnoreCase))
            {
                axis = BinStageAxis.GoodBinZ;
                positionName = name.Substring("GOOD Z ".Length);
                return true;
            }
            if (name.StartsWith("NG Y ", StringComparison.OrdinalIgnoreCase))
            {
                axis = BinStageAxis.NgBinY;
                positionName = name.Substring("NG Y ".Length);
                return true;
            }
            if (name.StartsWith("VISION X ", StringComparison.OrdinalIgnoreCase))
            {
                axis = BinStageAxis.VisionX;
                positionName = name.Substring("VISION X ".Length);
                return true;
            }

            return false;
        }

        private void BindIoPanel()
        {
            try
            {
                if (_outputStageUnit == null)
                    return;

                var unit = _outputStageUnit;
                ioCylinderPanel.ColumnCount = 2;   // 2열 배치 (Front Head 기준)
                ioCylinderPanel.AutoFitParentGroupHeight = true;   // 그룹 높이 실측 자동맞춤 (스크롤 없이 전 항목 표시)
            ioCylinderPanel.SetItems(new[]
                {
                    // ===== 단독(묶이지 않은) 체크 센서 — 최상단 =====
                    IoCylinderItem.Input("GOOD BIN RING CHECK", () => IsOn(unit.GoodBinRingSensor)),
                    IoCylinderItem.Input("NG BIN RING CHECK", () => IsOn(unit.NgBinRingSensor)),

                    // ===== GOOD BIN : SET GUIDE (Up/Down 체크 센서 + Up/Down 출력 통합) =====
                    IoCylinderItem.Input("GOOD BIN GUIDE UP", () => IsOn(unit.GoodBinGuideUpSensor)),
                    IoCylinderItem.Input("GOOD BIN GUIDE DOWN", () => IsOn(unit.GoodBinGuideDownSensor)),
                    IoCylinderItem.Output("GOOD BIN GUIDE", () => unit.IsBinGuideUp(BinSide.Good),
                        on => SetBinGuideAsync(BinSide.Good, on), "UP", "DOWN"),

                    // ===== GOOD BIN : SET CLAMP LIFT (Up 체크 센서 + Up/Down 출력 통합) =====
                    IoCylinderItem.Input("GOOD CLAMP LIFT UP", () => IsOn(unit.GoodBinClampUpSensor)),
                    IoCylinderItem.Output("GOOD CLAMP LIFT", () => unit.IsBinGuideClampLiftUp(BinSide.Good),
                        on => SetBinClampLiftAsync(BinSide.Good, on), "UP", "DOWN"),

                    // ===== GOOD BIN : SET CLAMP (Unclamp 체크 센서 + Clamp/Unclamp 출력 통합) =====
                    IoCylinderItem.Input("GOOD BIN UNCLAMP", () => unit.IsBinGuideUnclamped(BinSide.Good)),
                    IoCylinderItem.Output("GOOD BIN CLAMP", () => unit.IsBinGuideClampOutputActive(BinSide.Good),
                        on => SetBinClampAsync(BinSide.Good, on), "CLAMP", "UNCLAMP"),

                    // ===== NG BIN : SET GUIDE (Up/Down 체크 센서 + Up/Down 출력 통합) =====
                    IoCylinderItem.Input("NG BIN GUIDE UP", () => IsOn(unit.NgBinGuideUpSensor)),
                    IoCylinderItem.Input("NG BIN GUIDE DOWN", () => IsOn(unit.NgBinGuideDownSensor)),
                    IoCylinderItem.Output("NG BIN GUIDE", () => unit.IsBinGuideUp(BinSide.Ng),
                        on => SetBinGuideAsync(BinSide.Ng, on), "UP", "DOWN"),

                    // ===== NG BIN : SET CLAMP LIFT (Up 체크 센서 + Up/Down 출력 통합) =====
                    IoCylinderItem.Input("NG CLAMP LIFT UP", () => IsOn(unit.NgBinClampUpSensor)),
                    IoCylinderItem.Output("NG CLAMP LIFT", () => unit.IsBinGuideClampLiftUp(BinSide.Ng),
                        on => SetBinClampLiftAsync(BinSide.Ng, on), "UP", "DOWN"),

                    // ===== NG BIN : SET CLAMP (Unclamp 체크 센서 + Clamp/Unclamp 출력 통합) =====
                    IoCylinderItem.Input("NG BIN UNCLAMP", () => unit.IsBinGuideUnclamped(BinSide.Ng)),
                    IoCylinderItem.Output("NG BIN CLAMP", () => unit.IsBinGuideClampOutputActive(BinSide.Ng),
                        on => SetBinClampAsync(BinSide.Ng, on), "CLAMP", "UNCLAMP")
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "BindIoPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private static bool IsOn(QMC.Common.IO.BaseDigitalInput input)
        {
            try { input?.UpdateStatus(); } catch { }
            return input != null && input.IsOn;
        }

        private Task<int> SetBinGuideAsync(BinSide side, bool on)
        {
            if (_outputStageUnit == null)
                return Task.FromResult(-1);

            return on
                ? _outputStageUnit.EnsureBinGuideUpAsync(side, ManualCylinderTimeoutMs)
                : _outputStageUnit.EnsureBinGuideDownAsync(side, ManualCylinderTimeoutMs);
        }

        private Task<int> SetBinClampLiftAsync(BinSide side, bool on)
        {
            if (_outputStageUnit == null)
                return Task.FromResult(-1);

            return on
                ? _outputStageUnit.EnsureBinGuideClampLiftUpAsync(side, ManualCylinderTimeoutMs)
                : _outputStageUnit.EnsureBinGuideClampLiftDownAsync(side, ManualCylinderTimeoutMs);
        }

        private Task<int> SetBinClampAsync(BinSide side, bool on)
        {
            if (_outputStageUnit == null)
                return Task.FromResult(-1);

            return on
                ? _outputStageUnit.EnsureBinGuideClampedAsync(side, ManualCylinderTimeoutMs)
                : _outputStageUnit.EnsureBinGuideUnclampedAsync(side, ManualCylinderTimeoutMs);
        }

        private void BindJogPanel()
        {
            try
            {
                if (_outputStageUnit == null)
                    return;

                var unit = _outputStageUnit;
                // 축 순서: GoodY/Z, NgY, VisionX
                var items = new List<JogAxisItem>
                {
                    BuildJogAxis("GOOD Y", unit.GoodStage.StageY, "Y+", "Y-", JogAxisControlKind.Vertical),
                    BuildJogAxis("GOOD Z", unit.GoodStage.StageZ, "Z+", "Z-", JogAxisControlKind.Vertical),
                    BuildJogAxis("NG Y", unit.NgStage.StageY, "Y+", "Y-", JogAxisControlKind.Vertical),
                    BuildJogAxis("VISION X", unit.OutputCameraX, "X+", "X-", JogAxisControlKind.Vertical)
                };

                jogPositionListControl.SetItems(items);
                jogAxisMoveControl.SetItems(items);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "BindJogPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private JogAxisItem BuildJogAxis(string name, BaseAxis axis, string plus, string minus, JogAxisControlKind kind)
        {
            JogAxisItem item = JogAxisItem.Single(name, axis, AxisUnitConverter.DisplayUnitFor(axis), 1.0, plus, minus).WithControlKind(kind);
            item.StepMoveAsync = (it, direction, speedType, customSpeed, axisStepDistance) =>
                _outputStageUnit.JogStepAsync(axis, direction, speedType, customSpeed, axisStepDistance);
            item.ContinuousMoveAsync = (it, direction, speedType, customSpeed) =>
                _outputStageUnit.JogContinuousAsync(axis, direction, speedType, customSpeed);
            item.StopAsync = it => _outputStageUnit.StopJogAsync(axis);
            return item;
        }

        private async Task ConfirmAndRunAsync(string actionName, Func<Task<int>> action)
        {
            await ConfirmAndRunInternalAsync(actionName, action, false);
        }

        private async Task ConfirmAndRunMoveToPositionAsync(string actionName, Func<Task<int>> action)
        {
            await ConfirmAndRunInternalAsync(actionName, action, true);
        }

        private async Task ConfirmAndRunInternalAsync(string actionName, Func<Task<int>> action, bool selectMoveSpeed)
        {
            try
            {
                if (_outputStageUnit == null || action == null)
                    return;

                if (ManualMoveGuard.BlockIfNotReady(this, "Output Stage"))
                    return;

                if (selectMoveSpeed)
                {
                    JogSpeedType speedType;
                    if (!ManualMoveGuard.ConfirmMoveSpeed(this, "Output Stage", actionName, out speedType))
                        return;

                    jogAxisMoveControl.SetSelectedSpeedType(speedType);
                }
                else
                {
                    DialogResult confirm = QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Output Stage", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (confirm != DialogResult.Yes)
                        return;
                }

                Cursor = Cursors.WaitCursor;
                _lastAbortReason = null;
                int result;
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("OutputStageRecipePage." + actionName))
                {
                    result = await action();
                }
                EventLogger.Write(EventKind.Event, "UI", "OUTPUT-STAGE", actionName + " result=" + result);
                if (result != 0)
                {
                    string detail = string.IsNullOrEmpty(_lastAbortReason) ? "" : Environment.NewLine + "사유 : " + _lastAbortReason;
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패" + detail, "Output Stage", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, actionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                RefreshView();
            }
        }

        private void ParameterGrid_ParameterValueChanged(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e != null && e.Item != null && e.Item.Scope == ParameterGridScope.Recipe)
                    SaveCurrentRecipeData();
                else
                    SaveCurrentSettingsData();

                RefreshView();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void SaveCurrentRecipeData()
        {
            try
            {
                var host = FindHostForm();
                if (host == null || string.IsNullOrWhiteSpace(host.ActiveRecipeName))
                    throw new InvalidOperationException("활성 Recipe가 없어 Output Stage Recipe 값을 저장할 수 없습니다.");

                if (!host.SaveMachineRecipe(host.ActiveRecipeName))
                    throw new InvalidOperationException(
                        "Output Stage Recipe 저장에 실패했습니다. 현재 적용값과 저장 파일의 값이 다를 수 있으며, " +
                        "재시작하면 이전값으로 복원될 수 있습니다. Alarm/Event Log를 확인하십시오. recipe=" +
                        host.ActiveRecipeName);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void SaveCurrentSettingsData()
        {
            try
            {
                var host = FindHostForm();
                if (host == null)
                    throw new InvalidOperationException("Main 화면을 찾을 수 없어 Output Stage Config/Setup 값을 저장할 수 없습니다.");

                if (!host.SaveMachineSettings())
                    throw new InvalidOperationException(
                        "Output Stage Config/Setup 저장에 실패했습니다. 현재 적용값과 저장 파일의 값이 다를 수 있으며, " +
                        "재시작하면 이전값으로 복원될 수 있습니다. Alarm/Event Log를 확인하십시오.");
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void RefreshView()
        {
            try
            {
                if (_outputStageUnit == null)
                    return;

                var unit = _outputStageUnit;
                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();

                lblVisionInfo.Text =
                    "OUTPUT STAGE" + Environment.NewLine +
                    "GOOD Y : " + FormatAxis(unit.GoodStage.StageY.ActualPosition, unit.GoodStage.StageY) + Environment.NewLine +
                    "GOOD Z : " + FormatAxis(unit.GoodStage.StageZ.ActualPosition, unit.GoodStage.StageZ) + Environment.NewLine +
                    "NG Y   : " + FormatAxis(unit.NgStage.StageY.ActualPosition, unit.NgStage.StageY) + Environment.NewLine +
                    "BIN X  : " + FormatAxis(unit.OutputCameraX.ActualPosition, unit.OutputCameraX) + Environment.NewLine +
                    "G-AVOID: " + OnOff(unit.GoodStage.IsAtAvoidPosition()) + Environment.NewLine +
                    "N-AVOID: " + OnOff(unit.NgStage.IsAtAvoidPosition());
            }
            catch
            {
            }
            finally
            {
            }
        }

        private ParameterGridItem AxisDouble(string displayName, ParameterGridScope scope, BaseAxis axis, Func<double> getter, Action<double> setter)
        {
            ParameterGridItem item = ParameterGridItem.Double(
                displayName,
                AxisUnitConverter.DisplayUnitFor(axis),
                scope,
                () => AxisUnitConverter.ToDisplay(getter(), axis),
                v => setter(AxisUnitConverter.FromDisplay(v, axis)));
            item.UnitGetter = () => AxisUnitConverter.DisplayUnitFor(axis);
            return item;
        }

        private static string FormatAxis(double value, BaseAxis axis)
        {
            try
            {
                return AxisUnitConverter.FormatDisplay(value, axis, "0.###", true);
            }
            catch
            {
                return "0 " + AxisUnitConverter.DisplayUnitFor(axis);
            }
            finally
            {
            }
        }

        private static string OnOff(bool value)
        {
            try
            {
                return value ? "ON" : "OFF";
            }
            catch
            {
                return "OFF";
            }
            finally
            {
            }
        }
    }
}
