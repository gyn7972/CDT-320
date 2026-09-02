using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Equipment.Vision;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT320.Calibration;
using QMC.CDT320.Sequencing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public sealed partial class FrontPickerRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private sealed class PositionItem
        {
            public string DisplayName;
            public PickerAxis Axis;
            public string PositionName;
            public ParameterGridScope Scope;
            public bool IsCommonT;
        }

        private readonly Timer refreshTimer = new Timer();
        private IDisposable bottomVisionPreview;
        private IDisposable sideVisionPreview;
        private readonly Dictionary<string, PositionItem> positionItems = new Dictionary<string, PositionItem>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PositionItem>> groupMoves = new Dictionary<string, List<PositionItem>>(StringComparer.OrdinalIgnoreCase);
        private const int ManualActionFrameHeight = 29;
        private PickerFrontUnit unit;
        private int selectedManualPickerNo = 4;
        private const string CommonTDisplayName = "PICKER T (공통)";

        public FrontPickerRecipePage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            InstallVisionPreview();
            refreshTimer.Interval = 250;
            refreshTimer.Tick += delegate
            {
                if (!ShouldRefreshVisible(this))
                {
                    refreshTimer.Stop();
                    return;
                }

                RefreshView();
            };
            optionParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
            waitParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
            optionParameterGrid.ParameterRowDoubleClicked += OptionParameterGrid_RowDoubleClicked;
            BindParameterGridMenus();
            ConfigureManualActions();
            // 매뉴얼 액션/티칭 이동 기본 속도를 Coarse로 사용한다(필요 시 화면 Speed Mode에서 Fine 선택 가능).
            jogAxisMoveControl.SetSelectedSpeedType(JogSpeedType.Coarse);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            lblHeader.Tag = "i18n:recipe.frontHead";
            lblHeader.Text = Lang.T("recipe.frontHead");
            ResolveUnit();
            BindParameterGrids();
            BindIoPanel();
            BindJogPanel();
            RefreshView();
            if (ShouldRefreshVisible(this))
                refreshTimer.Start();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            try { if (ShouldRefreshVisible(this)) refreshTimer.Start(); else refreshTimer.Stop(); } catch { }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                refreshTimer.Stop();
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
            tabBottom.Text = "BOTTOM";
            tabSide.Text = "SIDE";
            bottomVisionPreview = RecipeVisionPreview.ShowSingle(tabBottom, "BOTTOM VISION", VisionViewerPorts.BottomInspection);
            sideVisionPreview = RecipeVisionPreview.ShowVertical(
                tabSide,
                new RecipeVisionPreviewTile("FRONT SIDE VISION", VisionViewerPorts.FrontSideVision),
                new RecipeVisionPreviewTile("REAR SIDE VISION", VisionViewerPorts.RearSideVision));
        }

        private void DisposeVisionPreview()
        {
            try { if (bottomVisionPreview != null) bottomVisionPreview.Dispose(); } catch { }
            try { if (sideVisionPreview != null) sideVisionPreview.Dispose(); } catch { }
            bottomVisionPreview = null;
            sideVisionPreview = null;
        }

        private void ResolveUnit()
        {
            CDT320_Machine machine = FindMachine();
            unit = machine != null ? machine.PickerFrontUnit : null;
            EnsureData();

            bool enabled = unit != null;
            optionParameterGrid.Enabled = enabled;
            waitParameterGrid.Enabled = enabled;
            ioCylinderPanel.Enabled = enabled;
            jogPositionListControl.Enabled = enabled;
            jogAxisMoveControl.Enabled = enabled;
            jogSpeedControl.Enabled = enabled;
            manualActionPanel.SetButtonsEnabled(enabled);
        }

        private void EnsureData()
        {
            if (unit == null)
                return;

            unit.Setup.EnsureGeometryData();
            unit.Config.EnsureArrays();
            unit.Recipe.EnsurePositionObjects();
        }

        // 매뉴얼 액션 버튼(Designer 배치)의 Click 핸들러 — 버튼은 옵션 그룹과 1:1, 인터락 시퀀스로 이동
        private async void btnAvoidPosition_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("AVOID POSITION", MoveAvoidSequenceAsync);
        }

        private async void btnPickPosition_Click(object sender, EventArgs e)
        {
            await ConfirmSelectedAppliedZoneMoveAsync("PICK POSITION", "DiePickPosition", "PICK");
        }

        private async void btnBottomPosition_Click(object sender, EventArgs e)
        {
            await ConfirmSelectedAppliedZoneMoveAsync("BOTTOM POSITION", "DieBottomPosition", "BOTTOM");
        }

        private async void btnSidePosition_Click(object sender, EventArgs e)
        {
            await ConfirmSelectedAppliedZoneMoveAsync("SIDE POSITION", "DieSidePosition", "SIDE");
        }

        private async void btnPlacePosition_Click(object sender, EventArgs e)
        {
            await ConfirmSelectedAppliedZoneMoveAsync("PLACE POSITION", "DiePlacePosition", "PLACE");
        }

        private async void btnDiePickPosition_Click(object sender, EventArgs e)
        {
            await ConfirmSelectedAppliedZoneMoveAsync("DIE PICK POSITION", "DiePickPosition", "PICK");
        }

        private async void btnDieBottomPosition_Click(object sender, EventArgs e)
        {
            await ConfirmSelectedAppliedZoneMoveAsync("DIE BOTTOM POSITION", "DieBottomPosition", "BOTTOM");
        }

        private async void btnDieSidePosition_Click(object sender, EventArgs e)
        {
            await ConfirmSelectedAppliedZoneMoveAsync("DIE SIDE POSITION", "DieSidePosition", "SIDE");
        }

        private async void btnDiePlacePosition_Click(object sender, EventArgs e)
        {
            await ConfirmSelectedAppliedZoneMoveAsync("DIE PLACE POSITION", "DiePlacePosition", "PLACE");
        }

        private void ConfigureManualActions()
        {
            try
            {
                // 픽커 선택은 4열 한 줄, 이동 동작은 2칸씩 사용해서 기존 2열 감각을 유지한다.
                manualActionPanel.RowHeight = 45;
                manualActionPanel.ColumnCount = 4;
                manualActionPanel.SetItems(new[]
                {
                    CreatePickerSelectItem(1),
                    CreatePickerSelectItem(2),
                    CreatePickerSelectItem(3),
                    CreatePickerSelectItem(4),
                    CreateManualMoveItem("AVOID POSITION", () => ConfirmMoveAsync("AVOID POSITION", MoveAvoidSequenceAsync)),
                    CreateManualMoveItem("PICK POSITION", () => ConfirmSelectedAppliedZoneMoveAsync("PICK POSITION", "DiePickPosition", "PICK")),
                    CreateManualMoveItem("BOTTOM POSITION", () => ConfirmSelectedAppliedZoneMoveAsync("BOTTOM POSITION", "DieBottomPosition", "BOTTOM")),
                    CreateManualMoveItem("SIDE POSITION", () => ConfirmSelectedAppliedZoneMoveAsync("SIDE POSITION", "DieSidePosition", "SIDE")),
                    CreateManualMoveItem("PLACE POSITION", () => ConfirmSelectedAppliedZoneMoveAsync("PLACE POSITION", "DiePlacePosition", "PLACE")),
                    CreateManualMoveItem("DIE PICK POSITION", () => ConfirmSelectedAppliedZoneMoveAsync("DIE PICK POSITION", "DiePickPosition", "PICK")),
                    CreateManualMoveItem("DIE BOTTOM POSITION", () => ConfirmSelectedAppliedZoneMoveAsync("DIE BOTTOM POSITION", "DieBottomPosition", "BOTTOM")),
                    CreateManualMoveItem("DIE SIDE POSITION", () => ConfirmSelectedAppliedZoneMoveAsync("DIE SIDE POSITION", "DieSidePosition", "SIDE")),
                    CreateManualMoveItem("DIE PLACE POSITION", () => ConfirmSelectedAppliedZoneMoveAsync("DIE PLACE POSITION", "DiePlacePosition", "PLACE")),
                    CreateManualMoveItem("APPLIED ZONE MOVE", ShowAppliedZoneMoveDialogAsync),

                    CreateManualMoveItem("Z1 0-2mm x50 TEST", () => ConfirmMoveAsync("FRONT PICKER Z1 0-2mm x50 TEST", RunFrontPickerZ1CycleTestAsync))
                });
                FitManualActionGroupHeight();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "FRONT-PICKER", "ConfigureManualActions failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void FitManualActionGroupHeight()
        {
            grpManual.Height = manualActionPanel.PreferredContentHeight + ManualActionFrameHeight;
        }

        private void BindParameterGrids()
        {
            if (unit == null)
                return;

            EnsureData();
            positionItems.Clear();
            groupMoves.Clear();

            List<ParameterGridItem> optionItems = new List<ParameterGridItem>();

            // ===== RECIPE (teaching positions) — 위치 종류별 접이식 그룹 =====
            PickerAxis[] tzKeys = { PickerAxis.PickerT0, PickerAxis.PickerZ0, PickerAxis.PickerT1, PickerAxis.PickerZ1, PickerAxis.PickerT2, PickerAxis.PickerZ2, PickerAxis.PickerT3, PickerAxis.PickerZ3 };
            string[] tzNames = { "PICKER T1", "PICKER Z1", "PICKER T2", "PICKER Z2", "PICKER T3", "PICKER Z3", "PICKER T4", "PICKER Z4" };
            string[] tzUnits = { AxisUnitConverter.Degree, AxisUnitConverter.Millimeter, AxisUnitConverter.Degree, AxisUnitConverter.Millimeter, AxisUnitConverter.Degree, AxisUnitConverter.Millimeter, AxisUnitConverter.Degree, AxisUnitConverter.Millimeter };

            PickerAxis[] xyKeys = { PickerAxis.PickerX, PickerAxis.PickerY };
            string[] xyNames = { "PICKER X", "PICKER Y" };

            // AVOID — 전체 축의 avoid 위치
            optionItems.Add(ParameterGridItem.Header("AVOID POSITION", "K_AVOID"));
            AddPositionItem(optionItems, PickerAxis.PickerX, "PICKER X", "INPUT-SIDE AVOID POSITION", "InputAvoidPosition", AxisUnitConverter.Millimeter, "PICKER X INPUT-SIDE AVOID", "K_AVOID",
                "InputAvoidPosition은 Input 카메라 위치가 아니라 Front Picker X가 Input-side 방향으로 빠져 간섭을 피하기 위한 회피 위치입니다.");
            AddPositionItem(optionItems, PickerAxis.PickerX, "PICKER X", "OUTPUT-SIDE AVOID POSITION", "OutputAvoidPosition", AxisUnitConverter.Millimeter, "PICKER X OUTPUT-SIDE AVOID", "K_AVOID",
                "OutputAvoidPosition은 Output 카메라 위치가 아니라 Front Picker X가 Output-side 방향으로 빠져 간섭을 피하기 위한 회피 위치입니다.");
            AddPositionItem(optionItems, PickerAxis.PickerX, "PICKER X", "AVOID POSITION", "AvoidPosition", AxisUnitConverter.Millimeter, "PICKER X", "K_AVOID");
            AddPositionItem(optionItems, PickerAxis.PickerY, "PICKER Y", "AVOID POSITION", "AvoidPosition", AxisUnitConverter.Millimeter, "PICKER Y", "K_AVOID");
            AddCommonTPositionItem(optionItems, "AVOID POSITION", "AvoidPosition", "K_AVOID");
            for (int i = 0; i < tzKeys.Length; i++)
                if (!IsTheta(tzKeys[i]))
                    AddPositionItem(optionItems, tzKeys[i], tzNames[i], "AVOID POSITION", "AvoidPosition", tzUnits[i], tzNames[i], "K_AVOID");

            // Zone position: X/Y is taught by picker #4 base. Picker #1~#4 use Config offset X/Y inside each zone.
            string[] zoneKinds = { "PICK", "BOTTOM", "SIDE", "PLACE" };
            string[] zoneKindPos = { "PickPosition", "BottomPosition", "SidePosition", "PlacePosition" };
            for (int k = 0; k < zoneKinds.Length; k++)
            {
                string gk = "K_" + zoneKinds[k];
                optionItems.Add(ParameterGridItem.Header(zoneKinds[k] + " POSITION", gk));
                for (int i = 0; i < xyKeys.Length; i++)
                    AddPositionItem(optionItems, xyKeys[i], xyNames[i], zoneKinds[k] + " POSITION", zoneKindPos[k], AxisUnitConverter.Millimeter, xyNames[i], gk);
                AddCommonTPositionItem(optionItems, zoneKinds[k] + " POSITION", zoneKindPos[k], gk);
                for (int i = 0; i < tzKeys.Length; i++)
                    if (!IsTheta(tzKeys[i]))
                        AddPositionItem(optionItems, tzKeys[i], tzNames[i], zoneKinds[k] + " POSITION", zoneKindPos[k], tzUnits[i], tzNames[i], gk);
            }

            const string rotationCenterGroup = "K_COLLET_ROTATION_CENTER";
            optionItems.Add(Describe(ParameterGridItem.Header("COLLET ROTATION CENTER", rotationCenterGroup),
                "COC CENTER / RE-CAL에서 계산한 Collet별 회전 중심의 실제 Picker X/Y 좌표입니다."));
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                string name = "PICKER " + (index + 1) + " COC";
                optionItems.Add(InGroup(Describe(AxisDouble(name + " X", PickerAxis.PickerX, AxisUnitConverter.Millimeter, ParameterGridScope.Config,
                    () => unit.Config.ColletRotationCenterX[index], v => unit.Config.ColletRotationCenterX[index] = v),
                    "선택 Collet의 회전 중심 PickerX 기계 좌표입니다."), rotationCenterGroup));
                optionItems.Add(InGroup(Describe(AxisDouble(name + " Y", PickerAxis.PickerY, AxisUnitConverter.Millimeter, ParameterGridScope.Config,
                    () => unit.Config.ColletRotationCenterY[index], v => unit.Config.ColletRotationCenterY[index] = v),
                    "선택 Collet의 회전 중심 PickerY 기계 좌표입니다."), rotationCenterGroup));
                optionItems.Add(InGroup(ParameterGridItem.Bool(name + " VALID", ParameterGridScope.Config,
                    () => unit.Config.ColletRotationCenterValid[index], v => unit.Config.ColletRotationCenterValid[index] = v), rotationCenterGroup));
            }

            const string pickerSettingGroup = "K_PICKER_SETTING";
            optionItems.Add(ParameterGridItem.Header("PICKER SETTING", pickerSettingGroup));
            optionItems.Add(InGroup(ParameterGridItem.Bool("FRONT PICKER USE", ParameterGridScope.Config, () => unit.Config.UseUnit, v => unit.Config.UseUnit = v), pickerSettingGroup));
            optionItems.Add(InGroup(ParameterGridItem.Selection("RUN ORDER MODE", "mode", ParameterGridScope.Config, () => unit.Config.RunOrderMode, v => unit.Config.RunOrderMode = v), pickerSettingGroup));
            optionItems.Add(InGroup(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v), pickerSettingGroup));
            AddPickerConfigItems(optionItems, pickerSettingGroup);

            const string pickUpSettingGroup = "K_PICKUP_SETTING";
            optionItems.Add(Describe(ParameterGridItem.Header("PICKUP SETTING", pickUpSettingGroup),
                "Picker가 Input Die를 집을 때 사용하는 PickUp Z 상세 동작 파라미터입니다.\r\nPickUp Test Step 02~10과 자동 PickUp 시퀀스에서 같이 사용됩니다.\r\nSync Lift 거리/속도/가감속은 InputStage의 NEEDLE PICKUP SETTING 공통값을 사용합니다."));
            AddPickUpSettingItems(optionItems, pickUpSettingGroup);

            const string bottomMotionSettingGroup = "K_BOTTOM_MOTION_SETTING";
            optionItems.Add(ParameterGridItem.Header("BOTTOM MOTION SETTING", bottomMotionSettingGroup));
            AddBottomMotionSettingItems(optionItems, bottomMotionSettingGroup);

            const string placeMotionSettingGroup = "K_PLACE_MOTION_SETTING";
            optionItems.Add(ParameterGridItem.Header("PLACE MOTION SETTING", placeMotionSettingGroup));
            AddPlaceMotionSettingItems(optionItems, placeMotionSettingGroup);

            const string safetySettingGroup = "K_SAFETY_SETTING";
            optionItems.Add(ParameterGridItem.Header("SAFETY SETTING", safetySettingGroup));
            optionItems.Add(InGroup(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v), safetySettingGroup));
            optionItems.Add(InGroup(AxisDouble("INPUT SAFETY OFFSET", PickerAxis.PickerX, AxisUnitConverter.Millimeter, ParameterGridScope.Setup, () => unit.Setup.InputSafetyOffset, v => unit.Setup.InputSafetyOffset = v), safetySettingGroup));
            optionItems.Add(InGroup(AxisDouble("OUTPUT SAFETY OFFSET", PickerAxis.PickerX, AxisUnitConverter.Millimeter, ParameterGridScope.Setup, () => unit.Setup.OutputSafetyOffset, v => unit.Setup.OutputSafetyOffset = v), safetySettingGroup));
            optionItems.Add(InGroup(AxisDouble("PICKER Y FACING X CLEARANCE", PickerAxis.PickerX, AxisUnitConverter.Millimeter, ParameterGridScope.Setup, () => unit.Setup.PickerYFacingXClearance, v => unit.Setup.PickerYFacingXClearance = Math.Max(0.0, v)), safetySettingGroup));
            optionItems.Add(InGroup(AxisDouble("PICKER Y OUT DISTANCE", PickerAxis.PickerY, AxisUnitConverter.Millimeter, ParameterGridScope.Setup, () => unit.Setup.PickerYOutDistance, v => unit.Setup.PickerYOutDistance = Math.Max(0.0, v)), safetySettingGroup));
            optionItems.Add(InGroup(AxisDouble("PICKER PITCH X", PickerAxis.PickerX, AxisUnitConverter.Millimeter, ParameterGridScope.Setup, () => unit.Setup.PickerPitchX, v => unit.Setup.PickerPitchX = v), safetySettingGroup));
            optionItems.Add(InGroup(AxisDouble("PICKER PITCH Y", PickerAxis.PickerY, AxisUnitConverter.Millimeter, ParameterGridScope.Setup, () => unit.Setup.PickerPitchY, v => unit.Setup.PickerPitchY = v), safetySettingGroup));

            const string visionOffsetGroup = "K_VISION_OFFSET_SETTING";
            optionItems.Add(ParameterGridItem.Header("VISION OFFSET SETTING", visionOffsetGroup));
            AddVisionPickerOffsetItems(optionItems, "INPUT VISION", () => ResolveLiveVisionOffsets(true), PickerAxis.PickerX, PickerAxis.PickerY, visionOffsetGroup);
            AddVisionPickerOffsetItems(optionItems, "OUTPUT VISION", () => ResolveLiveVisionOffsets(false), PickerAxis.PickerX, PickerAxis.PickerY, visionOffsetGroup);

            AddAfProcessZItems(optionItems);
            AddColletExchangeItems(optionItems);

            optionParameterGrid.SetItems(optionItems);

            waitParameterGrid.AutoFitParentGroupHeight = true;   // WAIT 그룹 높이를 내용에 맞춰 자동 조정 (스크롤 없이 전 항목 표시)
                waitParameterGrid.SetItems(new[]
            {
                Describe(ParameterGridItem.Int("PICK LIFT WAIT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.PickLiftWaitMs, v => unit.Recipe.PickLiftWaitMs = Math.Max(0, v)),
                    "구 PickUp 경로에서 Needle/Picker가 PickLiftPosition만큼 들어 올린 뒤 진동 안정화를 위해 대기하는 시간입니다."),
                Describe(ParameterGridItem.Int("PLACE DELAY", "ms", ParameterGridScope.Recipe, () => unit.Recipe.PlaceDelayMs, v => unit.Recipe.PlaceDelayMs = Math.Max(0, v)),
                    "Place 동작 완료 후 다음 동작으로 넘어가기 전에 기다리는 시간입니다."),
                Describe(AxisDouble("PICK LIFT POSITION", PickerAxis.PickerZ0, AxisUnitConverter.Millimeter, ParameterGridScope.Recipe, () => unit.Recipe.PickLiftPosition, v => unit.Recipe.PickLiftPosition = v),
                    "구 PickUp 경로에서 Die를 집은 뒤 Needle과 Picker를 동시에 위로 들어 올릴 상대 거리입니다.")
            });
        }

        // AF 기반 Pick/Place 공정 Z 파라미터: Bottom to Pick/Place 변환값, 갱신 안전 한계, 헤드·콜렛별 Pick Overdrive.
        // PickPosition은 콜렛 AF(콜렛 캘)와 생산 런타임 다이 AF가, PlacePosition은 다이 AF가 산식으로 자동 갱신한다(승인 2026-07-29).
        // 콜렛 교체 위치(PickerX)는 Config 스코프다 — 레시피를 바꿔도 같은 위치를 쓴다.
        // 교체 시 Y/T/Z는 항상 Avoid로 후퇴하므로 X만 티칭한다.
        private void AddColletExchangeItems(List<ParameterGridItem> items)
        {
            const string groupKey = "M_COLLET_EXCHANGE";
            items.Add(ParameterGridItem.Header("COLLET EXCHANGE POSITION", groupKey));
            items.Add(InGroup(Describe(AxisDouble("EXCHANGE INPUT SIDE X", PickerAxis.PickerX, AxisUnitConverter.Millimeter, ParameterGridScope.Config,
                () => unit.Config.ColletExchangeInputX,
                v => unit.Config.ColletExchangeInputX = v),
                "콜렛 교체 시 공용 X레일에서 Input 방향 끝으로 빼는 PickerX 위치입니다. Y/T/Z는 Avoid로 후퇴합니다."), groupKey));
            items.Add(InGroup(Describe(AxisDouble("EXCHANGE OUTPUT SIDE X", PickerAxis.PickerX, AxisUnitConverter.Millimeter, ParameterGridScope.Config,
                () => unit.Config.ColletExchangeOutputX,
                v => unit.Config.ColletExchangeOutputX = v),
                "콜렛 교체 시 공용 X레일에서 Output 방향 끝으로 빼는 PickerX 위치입니다. Y/T/Z는 Avoid로 후퇴합니다."), groupKey));
        }

        private void AddAfProcessZItems(List<ParameterGridItem> items)
        {
            const string groupKey = "L_AF_PROCESS_Z";
            unit.Recipe.EnsurePositionObjects();
            items.Add(ParameterGridItem.Header("AF PROCESS Z (PICK/PLACE)", groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("BOTTOM TO PICK", "mm (0.000)", ParameterGridScope.Recipe,
                () => unit.Recipe.BottomToPickMm,
                v => unit.Recipe.BottomToPickMm = v),
                "Bottom 카메라 포커스 평면 Z를 Pick 공정 Z로 변환하는 고정 가산값(mm)입니다. " +
                "콜렛 AF: PickZ = AF BestZ + Collet Offset + Die Thickness + Bottom to Pick / 다이 AF: PickZ = AF BestZ + Bottom to Pick."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("BOTTOM TO PLACE", "mm (0.000)", ParameterGridScope.Recipe,
                () => unit.Recipe.BottomToPlaceMm,
                v => unit.Recipe.BottomToPlaceMm = v),
                "Bottom 카메라 포커스 평면 Z를 Place 공정 Z로 변환하는 고정 가산값(mm)입니다. " +
                "다이 AF: PlaceZ = AF BestZ + Bottom to Place (콜렛 AF는 Place를 갱신하지 않습니다)."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("AF Z UPDATE LIMIT", "mm (0.000)", ParameterGridScope.Recipe,
                () => unit.Recipe.AfZUpdateLimitMm,
                v => unit.Recipe.AfZUpdateLimitMm = Math.Max(0.001, Math.Abs(v))),
                "AF 기반 PickPosition/PlacePosition 갱신 안전 한계(|신규-기존|, mm)입니다. 초과 시 알람으로 차단하고 티칭을 갱신하지 않습니다. 기본 0.3mm."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("HEAD PICK OVERDRIVE", "mm (0.000)", ParameterGridScope.Recipe,
                () => unit.Recipe.HeadPickOverdriveMm,
                v => unit.Recipe.HeadPickOverdriveMm = v),
                "헤드(사이드) 공통 Pick Overdrive(mm)입니다. 콜렛별 값과 합산해 공정 Pick Z에 가산됩니다. +면 덜 내려오고 -면 더 내려옵니다."), groupKey));
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                items.Add(InGroup(Describe(ParameterGridItem.Double("COLLET " + (index + 1) + " PICK OVERDRIVE", "mm (0.000)", ParameterGridScope.Recipe,
                    () => unit.Recipe.ColletPickOverdriveMm != null && unit.Recipe.ColletPickOverdriveMm.Length > index
                        ? unit.Recipe.ColletPickOverdriveMm[index]
                        : 0.0,
                    v =>
                    {
                        unit.Recipe.EnsurePositionObjects();
                        unit.Recipe.ColletPickOverdriveMm[index] = v;
                    }),
                    "콜렛별 Pick Overdrive(mm)입니다. HEAD PICK OVERDRIVE와 합산해 공정 Pick Z에 가산됩니다. +면 덜 내려오고 -면 더 내려옵니다. Place에는 적용되지 않습니다."), groupKey));
            }
        }

        private void AddPickerConfigItems(List<ParameterGridItem> items, string groupKey)
        {
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                string name = "PICKER " + (index + 1);
                items.Add(InGroup(ParameterGridItem.Bool(name + " USE", ParameterGridScope.Config, () => unit.Config.UsePicker[index], v => unit.Config.UsePicker[index] = v), groupKey));
            }
        }

        // LoadSettings()가 Config/Setup 객체를 새 인스턴스로 교체하므로(BaseUnit.LoadSettings),
        // 그리드 클로저는 객체를 지역 캡처하지 말고 아래 리졸버로 매 호출 시 라이브 객체를 따라간다.
        // (지역 캡처 클로저는 교체 전 old 객체에 읽고 써서 UI만 갱신되고 런타임은 old 값을 쓰는 버그가 됨)
        private PickerPickUpMotionConfig ResolveLivePickUpConfig()
        {
            PickerPickUpMotionConfig pickUp = unit.Config.PickUp;
            if (pickUp == null)
                unit.Config.PickUp = pickUp = new PickerPickUpMotionConfig();
            pickUp.Ensure();
            return pickUp;
        }

        private PickerPlaceMotionConfig ResolveLivePlaceConfig()
        {
            PickerPlaceMotionConfig place = unit.Config.Place;
            if (place == null)
                unit.Config.Place = place = new PickerPlaceMotionConfig();
            place.Ensure();
            return place;
        }

        private PickerBottomInspectionMotionConfig ResolveLiveBottomInspectionConfig()
        {
            PickerBottomInspectionMotionConfig bottom = unit.Config.BottomInspection;
            if (bottom == null)
                unit.Config.BottomInspection = bottom = new PickerBottomInspectionMotionConfig();
            bottom.Ensure();
            return bottom;
        }

        private PickerVisionCoordinateOffsets ResolveLiveVisionOffsets(bool inputVision)
        {
            unit.Setup.EnsureGeometryData();
            PickerVisionCoordinateOffsets offsets = inputVision
                ? unit.Setup.InputVisionToPicker
                : unit.Setup.OutputVisionToPicker;
            if (offsets != null)
                offsets.EnsureArrays();
            return offsets;
        }

        private void AddPickUpSettingItems(List<ParameterGridItem> items, string groupKey)
        {
            items.Add(InGroup(Describe(ParameterGridItem.Double("PICKUP MECHANICAL OFFSET LIMIT", "mm (0.000)", ParameterGridScope.Config,
                () => ResolveLivePickUpConfig().MechanicalOffsetLimitMm,
                v => SetPickUpMechanicalOffsetLimit(ResolveLivePickUpConfig(), v)),
                "Picker별 PickUp 기구 보정의 X/Y 절대값 한계입니다. 기본값은 1.000 mm이고 안전 상한은 2.000 mm입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PICKUP MECHANICAL OFFSET T LIMIT", "deg (0.000)", ParameterGridScope.Config,
                () => ResolveLivePickUpConfig().MechanicalOffsetTLimitDeg,
                v => SetPickUpMechanicalOffsetTLimit(ResolveLivePickUpConfig(), v)),
                "Picker별 PickUp T 기구 보정의 절대값 한계입니다. 기본값은 1.000 deg이고 안전 상한은 2.000 deg입니다."), groupKey));
            for (int i = 0; i < PickerPickUpMotionConfig.MechanicalOffsetPickerCount; i++)
            {
                int index = i;
                string pickerName = "PICKER " + (index + 1);
                items.Add(InGroup(Describe(ParameterGridItem.Double(pickerName + " PICKUP MECHANICAL X", "mm (0.000)", ParameterGridScope.Config,
                    () => ResolveLivePickUpConfig().GetMechanicalOffsetX(index),
                    v => SetPickUpMechanicalOffset(ResolveLivePickUpConfig(), index, true, v)),
                    "PickUp 목표의 PickerX와 NeedleX에 동일하게 더하는 Picker별 기구 보정입니다."), groupKey));
                items.Add(InGroup(Describe(ParameterGridItem.Double(pickerName + " PICKUP MECHANICAL Y", "mm (0.000)", ParameterGridScope.Config,
                    () => ResolveLivePickUpConfig().GetMechanicalOffsetY(index),
                    v => SetPickUpMechanicalOffset(ResolveLivePickUpConfig(), index, false, v)),
                    "PickUp 목표의 PickerY에만 더합니다. NeedleX와 1:1로 움직이는 WaferStageY 목표에는 적용하지 않습니다."), groupKey));
                items.Add(InGroup(Describe(ParameterGridItem.Double(pickerName + " PICKUP MECHANICAL T", "deg (0.000)", ParameterGridScope.Config,
                    () => ResolveLivePickUpConfig().GetMechanicalOffsetT(index),
                    v => SetPickUpMechanicalOffsetT(ResolveLivePickUpConfig(), index, v)),
                    "PickUp 목표의 PickerT에 더하는 Picker별 T 기구 보정입니다(2026-08-16 신설). 런타임 T 필터 이관은 기구T = 기구T + 필터T 입니다(2026-08-18 가산 정정)."), groupKey));
            }
            items.Add(InGroup(Describe(ParameterGridItem.Selection<PickerPickUpZMotionMode>("PICKUP Z MOTION MODE", "mode", ParameterGridScope.Config, () => ResolveLivePickUpConfig().MotionMode, v => ResolveLivePickUpConfig().MotionMode = v),
                "PickUp Z 동작 방식을 선택합니다.\r\nDetailed: Needle/Eject 준비, 진공, PrePick, 저속 접촉, 동기 상승, 안전 복귀 순서로 동작합니다.\r\nSimpleZDownVacuumUp: PickerZ 하강, 진공 ON, PickerZ 상승만 수행하는 단순 모드입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Selection<PickerPickUpTransferMotionMode>("PICKUP TRANSFER MODE", "mode", ParameterGridScope.Config, () => ResolveLivePickUpConfig().TransferMotionMode, v => ResolveLivePickUpConfig().TransferMotionMode = v),
                "Default는 기존 PickUp 위치 이동 순서를 사용합니다.\r\nContiSegmentedPickUp은 연속 PickUp 중 PickerY/T 선보정과 EjectPinZ Avoid를 같이 준비한 뒤 PickerX/NeedleX/StageY/PickerZ를 4축 ContiNode로 이동합니다."), groupKey));
            items.Add(InGroup(ParameterGridItem.Int("PICKUP CONTI COORDINATE", "coord", ParameterGridScope.Config, () => ResolveLivePickUpConfig().TransferContiCoordinate, v => ResolveLivePickUpConfig().TransferContiCoordinate = Math.Max(1, v)), groupKey));
            items.Add(InGroup(ParameterGridItem.Int("PICKUP CONTI TIMEOUT", "ms", ParameterGridScope.Config, () => ResolveLivePickUpConfig().TransferContiTimeoutMs, v => ResolveLivePickUpConfig().TransferContiTimeoutMs = Math.Max(1, v)), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PICKUP CONTI MAX TRAVEL", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePickUpConfig().TransferContiMaxTravelDistance, v => ResolveLivePickUpConfig().TransferContiMaxTravelDistance = PickerPickUpMotionConfig.NormalizePositive(v, 45.0)),
                "현재 위치에서 다음 PickUp 목표까지 한 축이라도 이 거리보다 많이 움직이면 ContiNode를 쓰지 않고 기존 이동 방식으로 접근합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PICKUP CONTI Y MAX CORR", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePickUpConfig().TransferContiPickerYMaxCorrectionDistance, v => ResolveLivePickUpConfig().TransferContiPickerYMaxCorrectionDistance = PickerPickUpMotionConfig.NormalizePositive(v, 1.5)),
                "PickerY를 Avoid로 빼지 않고 다음 PickUp Y 위치로 선보정할 때 허용하는 최대 보정 거리입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PICKUP CONTI XY MID RATIO", "ratio", ParameterGridScope.Config, () => ResolveLivePickUpConfig().TransferContiXYMidRatio, v => ResolveLivePickUpConfig().TransferContiXYMidRatio = Math.Max(0.0, Math.Min(1.0, v))),
                "ContiNode 중간 위치 비율입니다. 0.5면 현재 위치와 다음 PickUp 목표의 중간점을 사용합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PICKUP CONTI SPLINE CURVE %", "%", ParameterGridScope.Config, () => ResolveLivePickUpConfig().TransferContiSplineCurvePercent, v => ResolveLivePickUpConfig().TransferContiSplineCurvePercent = PickerPickUpMotionConfig.NormalizeSplineCurvePercent(v, 100.0)),
                "PickUp ContiNode 스플라인 곡선 강도입니다.\r\n0%는 직선에 가깝게, 100%는 현재 기준, 200%는 더 둥근 X-Z 궤적으로 이동합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Bool("PICKUP CONTI USE GLOBAL SPEED SCALE", ParameterGridScope.Config, () => ResolveLivePickUpConfig().TransferContiUseGlobalSpeedScale, v => ResolveLivePickUpConfig().TransferContiUseGlobalSpeedScale = v),
                "PickUp ContiNode 속도에 MOTION 화면의 DEFAULT SPEED SCALE %를 적용할지 선택합니다.\r\nTrue: 전역 스케일을 적용합니다.\r\nFalse: PICKUP CONTI MAX VEL/ACC/DEC와 NODE SPEED % 값만 사용합니다."), groupKey));
            // PICKUP CONTI 속도 항목 삭제(사용자 확정 속도 모델 2026-07-26) — 축 기본속도×전역 스케일 일원화.
            items.Add(InGroup(Describe(ParameterGridItem.Double("PICKER Z PRE PICK DISTANCE", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePickUpConfig().PickerZPrePickDistance, v => ResolveLivePickUpConfig().PickerZPrePickDistance = Math.Max(0.0, v)),
                "PickerZ가 PickPosition으로 바로 내려가기 전에 멈추는 거리입니다.\r\nPickPosition에서 Avoid 방향으로 이 거리만큼 떨어진 위치까지 먼저 이동한 뒤 저속 접근합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PICKER Z APPROACH SPEED", "%", ParameterGridScope.Config, () => ResolveLivePickUpConfig().PickerZSlowApproachSpeedPercent, v => ResolveLivePickUpConfig().PickerZSlowApproachSpeedPercent = PickerPickUpMotionConfig.NormalizePercent(v, 1.0)),
                "PrePick 위치에서 실제 PickPosition까지 천천히 내려갈 때 사용하는 PickerZ 속도 비율입니다.\r\n축 기본 속도 대비 퍼센트로 적용됩니다."), groupKey));
            // [사용자 지시 2026-07-27] Separate 스텝 폐지 — SEPARATE DISTANCE/SPEED,
            // AVOID SPEED(미사용), SEPARATE MODE(구 옵션) UI 항목 제거. Config 프로퍼티는
            // 직렬화 하위호환용으로만 유지.
            items.Add(InGroup(Describe(ParameterGridItem.Double("PICKER SAFE FOR WAFERSTAGE", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePickUpConfig().PickerSafeForWaferStageDistance, v => ResolveLivePickUpConfig().PickerSafeForWaferStageDistance = PickerPickUpMotionConfig.NormalizePickerSafeForWaferStageDistance(v)),
                "PickUp 후 PickerZ Avoid 복귀를 끝까지 기다리지 않고 다음 동작을 허용할 최소 상승 거리입니다.\r\nDie Touch 높이에서 Avoid 방향으로 이 거리 이상 올라오면 다음 시퀀스를 진행합니다. 최소값은 2.0 mm입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Int("VACUUM BEFORE PICK DELAY", "ms", ParameterGridScope.Config, () => ResolveLivePickUpConfig().VacuumOnBeforePickDelayMs, v => ResolveLivePickUpConfig().VacuumOnBeforePickDelayMs = Math.Max(0, v)),
                "PickerZ가 Die Touch 위치에 도착하고 위치 확인이 끝난 직후 기다리는 시간입니다.\r\n이 시간이 지난 뒤 Sync Lift 또는 PickerZ 상승을 시작합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Int("SYNC LIFT SETTLE", "ms", ParameterGridScope.Config, () => ResolveLivePickUpConfig().SyncLiftSettleMs, v => ResolveLivePickUpConfig().SyncLiftSettleMs = Math.Max(0, v)),
                "Sync Lift 완료 직후 PickerZ Separate 전에 기다리던 기존 Picker별 값입니다.\r\n현재 자동 PickUp은 InputStage NEEDLE PICKUP SETTING의 PICKUP SYNC LIFT SETTLE 공통값을 우선 사용합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Int("PICK SETTLE", "ms", ParameterGridScope.Config, () => ResolveLivePickUpConfig().PickSettleMs, v => ResolveLivePickUpConfig().PickSettleMs = Math.Max(0, v)),
                "PickUp Z 동작 후 흡착 확인/Material 갱신 전에 기다리는 안정화 시간입니다.\r\nDie가 흔들리거나 진공 응답이 늦을 때 늘립니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Int("NEEDLE VACUUM OFF SETTLE", "ms", ParameterGridScope.Config, () => ResolveLivePickUpConfig().NeedleVacuumOffSettleBeforeXYMs, v => ResolveLivePickUpConfig().NeedleVacuumOffSettleBeforeXYMs = Math.Max(0, Math.Min(60000, v))),
                "Needle Vacuum OFF 후 StageY/NeedleX 이동 허용까지의 안정화 시간입니다.\r\nOFF 확인 시점부터 경과 시간을 차감하므로 보통 추가 대기 없이 통과합니다. 기본 100 ms."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Bool("PICKUP ENTRY Z PREDOWN", ParameterGridScope.Config, () => ResolveLivePickUpConfig().PickUpEntryZPreDownMode, v => ResolveLivePickUpConfig().PickUpEntryZPreDownMode = v),
                "PickUp 진입 Y 전진과 동시에 Z를 PrePick까지 선행 하강합니다(1-A, 반경 게이트 내 한정).\r\n픽업 후 Z는 항상 Avoid까지 상승하며, PICKER SAFE FOR WAFERSTAGE 통과 시 시퀀스가 조기 진행됩니다.\r\nAuto + ContiSegmentedPickUp에서만 동작. 기본 Off."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PREDOWN NEEDLE WORK RADIUS", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePickUpConfig().PreDownNeedleWorkRadiusMm, v => ResolveLivePickUpConfig().PreDownNeedleWorkRadiusMm = Math.Max(0.0, v)),
                "진입 Z 선행 하강(1-A) 발동 반경입니다. die 목표 NeedleX/StageY와 Needle 작업영역 중심의 거리가 이 값 이하일 때만 발동합니다.\r\n웨이퍼 가장자리 링/클램프 간섭 방지용 — 런타임 Needle 작업영역 반경을 넘으면 자동으로 그 값까지 줄입니다. 기본 130 mm, 0이면 기능 Off."), groupKey));
            // [동적 선행 대기점, 지시서 2026-07-27]
            items.Add(InGroup(Describe(ParameterGridItem.Bool("PICKUP DYNAMIC WAIT", ParameterGridScope.Config, () => ResolveLivePickUpConfig().PickUpDynamicWaitMode, v => ResolveLivePickUpConfig().PickUpDynamicWaitMode = v),
                "촬영(선행검사) 진행 중 대기 픽커 X를 배치 최근접 비전 좌표 + 팔로잉 클리어런스 위치까지 미리 접근시킵니다.\r\n허가 후 팔로잉 진입 거리가 줄어 픽업 진입 시간이 단축됩니다. 전진만 하며 후퇴는 하지 않습니다.\r\nAuto + ContiSegmentedPickUp에서만 동작. 기본 Off."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("DYNAMIC WAIT EXTRA MARGIN", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePickUpConfig().DynamicWaitExtraMarginMm, v => ResolveLivePickUpConfig().DynamicWaitExtraMarginMm = Math.Max(0.0, v)),
                "동적 선행 대기점에 추가하는 여유 간격입니다(현장 튜닝용).\r\n대기점 = 배치 maxVisionX + 팔로잉 클리어런스(SafetyDistance+Extra) + 이 값. 기본 0 mm."), groupKey));
        }

        // AddPickUpContiNodeSpeedRatioItems 삭제(사용자 확정 속도 모델 2026-07-26):
        // PICKUP CONTI MAX VEL/ACC/DEC, NODE0~3 SPEED % 설정 폐지 — 이송 속도는
        // 각 축 DefaultVelocity × 전역 DEFAULT SPEED SCALE로 일원화.

        private void AddBottomMotionSettingItems(List<ParameterGridItem> items, string groupKey)
        {
            ResolveLiveBottomInspectionConfig();
            items.Add(InGroup(Describe(ParameterGridItem.Selection<PickerBottomFlyingZDownMode>("BOTTOM FLYING Z DOWN MODE", "mode", ParameterGridScope.Config, () => ResolveLiveBottomInspectionConfig().FlyingZDownMode, v => ResolveLiveBottomInspectionConfig().FlyingZDownMode = v),
                "Bottom 검사 위치로 X/Y/T 이동하는 동안 Picker Z를 미리 내릴지 정합니다.\r\nOff: 미리 내리지 않음\r\nDownDistance: Avoid 위치에서 지정 거리만큼 먼저 하강\r\nToBottomPosition: Bottom 검사 Z 위치까지 바로 하강"), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("BOTTOM FLYING Z DOWN DISTANCE", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLiveBottomInspectionConfig().FlyingZDownDistance, v => ResolveLiveBottomInspectionConfig().FlyingZDownDistance = PickerBottomInspectionMotionConfig.NormalizeDistance(v)),
                "DOWN MODE가 DownDistance일 때 사용할 선행 하강 거리입니다.\r\n예: 2 mm면 Avoid 위치에서 2 mm만 먼저 내려가고, 이후 정식 Bottom Z 위치로 이동합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("BOTTOM APPROACH PREMOTION DIST", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLiveBottomInspectionConfig().ApproachPreMotionDistanceMm, v => ResolveLiveBottomInspectionConfig().ApproachPreMotionDistanceMm = PickerBottomInspectionMotionConfig.NormalizeDistance(v)),
                "Bottom 접근 X 이동 중 잔여 거리가 이 값 이하가 되면 해당 피커의 Z 하강+T 회전을 X와 동시에 시작합니다.\r\n발동 조건: Auto + PickerY가 이미 촬영 위치(fixed-Y 확립). 0이면 기능을 끕니다. 기본 50 mm."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Bool("BOTTOM P1 + SIDE P4 OVERLAP", ParameterGridScope.Config, () => ResolveLiveBottomInspectionConfig().ParallelFirstSideOverlap, v => ResolveLiveBottomInspectionConfig().ParallelFirstSideOverlap = v),
                "마지막 Bottom(P1) 촬영과 첫 Side(P4) 촬영 요청을 병렬로 송신하는 특수 오버랩입니다.\r\n발동 조건: Auto + Bottom 마지막 대상이 P1, Side 첫 대상이 P4, 두 픽커 제품 문맥 일치.\r\n기본 Off."), groupKey));
        }

        private void AddPlaceMotionSettingItems(List<ParameterGridItem> items, string groupKey)
        {
            PickerPlaceMotionConfig place = ResolveLivePlaceConfig();
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE MECHANICAL OFFSET LIMIT", "mm (0.000)", ParameterGridScope.Config,
                () => ResolveLivePlaceConfig().MechanicalOffsetLimitMm,
                v => SetPlaceMechanicalOffsetLimit(ResolveLivePlaceConfig(), v)),
                "Picker별 Place 기구 보정의 X/Y 절대값 한계입니다. 기본값은 1.000 mm이고 안전 상한은 2.000 mm입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE MECHANICAL OFFSET T LIMIT", "deg (0.000)", ParameterGridScope.Config,
                () => ResolveLivePlaceConfig().MechanicalOffsetTLimitDeg,
                v => SetPlaceMechanicalOffsetTLimit(ResolveLivePlaceConfig(), v)),
                "Picker별 Place T 기구 보정의 절대값 한계입니다. 기본값은 1.000 deg이고 안전 상한은 2.000 deg입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("BOTTOM PLACE CORRECTION LIMIT", "mm (0.000)", ParameterGridScope.Config,
                () => ResolveLivePlaceConfig().BottomPlaceCorrectionLimitMm,
                v => SetBottomPlaceCorrectionLimit(ResolveLivePlaceConfig(), v)),
                "Bottom FINAL item X/Y 및 기구 보정을 합산한 Place 보정의 절대값 한계입니다. 기본값은 1.000 mm이고 안전 상한은 2.000 mm입니다."), groupKey));
            for (int i = 0; i < PickerPickUpMotionConfig.MechanicalOffsetPickerCount; i++)
            {
                int index = i;
                string pickerName = "PICKER " + (index + 1);
                items.Add(InGroup(Describe(ParameterGridItem.Double(pickerName + " PLACE MECHANICAL X", "mm (0.000)", ParameterGridScope.Config,
                    () => ResolveLivePlaceConfig().GetMechanicalOffsetX(index),
                    v => SetPlaceMechanicalOffset(ResolveLivePlaceConfig(), index, true, v)),
                    "Place 목표의 PickerX에 더하는 Picker별 기구 보정입니다."), groupKey));
                items.Add(InGroup(Describe(ParameterGridItem.Double(pickerName + " PLACE MECHANICAL Y", "mm (0.000)", ParameterGridScope.Config,
                    () => ResolveLivePlaceConfig().GetMechanicalOffsetY(index),
                    v => SetPlaceMechanicalOffset(ResolveLivePlaceConfig(), index, false, v)),
                    "Place 대상 GOOD/NG OutputStageY 목표에 더합니다. PickerY Place Teaching은 변경하지 않습니다."), groupKey));
                items.Add(InGroup(Describe(ParameterGridItem.Double(pickerName + " PLACE MECHANICAL T", "deg (0.000)", ParameterGridScope.Config,
                    () => ResolveLivePlaceConfig().GetMechanicalOffsetT(index),
                    v => SetPlaceMechanicalOffsetT(ResolveLivePlaceConfig(), index, v)),
                    "Place 목표의 PickerT에 더하는 Picker별 T 기구 보정입니다(2026-08-16 신설). 런타임 T 필터 이관은 기구T = 기구T − 필터T 입니다."), groupKey));
            }
            items.Add(InGroup(Describe(ParameterGridItem.Selection<PickerPlaceMotionMode>("PLACE MOTION MODE", "mode", ParameterGridScope.Config, () => ResolveLivePlaceConfig().MotionMode, v => ResolveLivePlaceConfig().MotionMode = v),
                "Default는 기존 Place 이동 순서를 사용합니다.\r\nContiSegmentedPlace는 이전 Z1 상승과 현재 Z2 접근을 5개 ContiNode로 나누어 연속 구동합니다."), groupKey));
            items.Add(InGroup(ParameterGridItem.Int("PLACE CONTI COORDINATE", "coord", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiCoordinate, v => ResolveLivePlaceConfig().ContiCoordinate = Math.Max(1, v)), groupKey));
            items.Add(InGroup(ParameterGridItem.Int("PLACE CONTI TIMEOUT", "ms", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiTimeoutMs, v => ResolveLivePlaceConfig().ContiTimeoutMs = Math.Max(1, v)), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Bool("PLACE ENTRY Z PREDOWN", ParameterGridScope.Config, () => ResolveLivePlaceConfig().PlaceEntryZPreDownMode, v => ResolveLivePlaceConfig().PlaceEntryZPreDownMode = v),
                "Place 진입 Y 전진과 동시에 첫 피커 Z를 PrePlace까지 선행 하강하고(1-A),\r\nPlace 후 Z 상승을 PrePlace에서 끊어 완료를 조기 판정합니다(1-B). Auto + ContiSegmentedPlace에서만 동작. 기본 Off."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("REAR ENTRY PREDOWN STAGEY LIMIT", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePlaceConfig().RearEntryPreDownStageYLimitMm, v => ResolveLivePlaceConfig().RearEntryPreDownStageYLimitMm = v),
                "Rear 전용 발동 제약: 대상 Bin StageY의 실측과 수령 이동 목표가 모두 이 값 이상(≥)일 때만 Rear에서 Z 선행 하강을 발동합니다.\r\n리어 물리 충돌 구조물은 StageY가 작은 구간에 있어, 간섭 없는 구간의 최솟값을 실측으로 설정해야 켜집니다.\r\n0이면 Rear는 발동하지 않습니다(안전측). Front는 이 값을 사용하지 않습니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI MAX TRAVEL", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiMaxTravelDistance, v => ResolveLivePlaceConfig().ContiMaxTravelDistance = PickerPickUpMotionConfig.NormalizePositive(v, 45.0)),
                "현재 위치에서 Place 목표 위치까지 한 축이라도 이 거리보다 많이 움직이면 ContiNode를 사용하지 않고 기존 이동 방식으로 접근합니다.\r\n알람/정지 후 Avoid 위치에서 재시작할 때 긴 거리를 ContiNode로 이동하지 않게 막는 값입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE Z OVERDRIVE", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePlaceConfig().PlaceZOverDrive, v => ResolveLivePlaceConfig().PlaceZOverDrive = v),
                "Place Z 티칭 위치에 더해서 내려놓는 보정량입니다.\r\n최종 Place Z = 티칭 Place Z + 이 값입니다. 장비 Z 좌표 방향에 맞춰 부호를 설정하세요."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Int("PLACE RELEASE DWELL", "ms", ParameterGridScope.Config, () => ResolveLivePlaceConfig().PlaceReleaseDwellMs, v => ResolveLivePlaceConfig().PlaceReleaseDwellMs = Math.Max(0, v)),
                "Place 위치에서 제품을 내려놓기 위해 유지하는 총 대기 시간입니다.\r\nBlow Delay보다 길면 Blow OFF 후 남은 시간만 더 대기합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Int("PLACE BLOW DELAY", "ms", ParameterGridScope.Config, () => ResolveLivePlaceConfig().PlaceBlowDelayMs, v => ResolveLivePlaceConfig().PlaceBlowDelayMs = Math.Max(0, v)),
                "Place 위치에서 Vacuum OFF 후 Blow를 켜고 유지하는 시간입니다.\r\n이 시간이 지나면 PickerZ를 올리기 전에 Blow를 먼저 OFF합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI Z1 STEP1 CLEAR", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiZ1Step1Clearance, v => ResolveLivePlaceConfig().ContiZ1Step1Clearance = Math.Max(0.0, v)),
                "ContiSegmentedPlace node0에서 이전 PickerZ(Z1)를 티칭 Place 기준 + Tape + Die 위치보다 위로 올리는 1단 회피량입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI Z1 STEP2 CLEAR", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiZ1Step2Clearance, v => ResolveLivePlaceConfig().ContiZ1Step2Clearance = Math.Max(0.0, v)),
                "ContiSegmentedPlace node1에서 이전 PickerZ(Z1)를 추가로 올리는 2단 회피량입니다. node0보다 빠른 속도 설정을 사용할 수 있습니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NEAR AVOID", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiNearAvoidDistance, v => ResolveLivePlaceConfig().ContiNearAvoidDistance = Math.Max(0.0, v)),
                "node2/node3에서 Z1과 Z2가 Avoid 바로 전까지 접근할 거리입니다. 1 mm이면 Avoid 위치에서 Place 방향으로 1 mm 내려온 위치를 사용합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI XY MID RATIO", "ratio", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiXYMidRatio, v => ResolveLivePlaceConfig().ContiXYMidRatio = Math.Max(0.0, Math.Min(1.0, v))),
                "node2의 X/Y 중간 위치 비율입니다. 0.5면 현재 위치와 Target Pos의 중간까지 이동합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI SPLINE CURVE %", "%", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiSplineCurvePercent, v => ResolveLivePlaceConfig().ContiSplineCurvePercent = PickerPickUpMotionConfig.NormalizeSplineCurvePercent(v, 100.0)),
                "Place ContiNode 스플라인 곡선 강도입니다.\r\n0%는 직선에 가깝게, 100%는 현재 기준, 200%는 더 둥근 X-Z 궤적으로 이동합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI OVERDRIVE", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiOverDrive, v => ResolveLivePlaceConfig().ContiOverDrive = Math.Max(0.0, v)),
                "node4에서 현재 PickerZ(Z2)가 최종 Place 위치에 더 들어가는 OverDrive 값입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI TAPE FALLBACK", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiTapeThicknessFallback, v => ResolveLivePlaceConfig().ContiTapeThicknessFallback = Math.Max(0.0, v)),
                "프로젝트/웨이퍼 정보에서 Tape 두께를 읽지 못했을 때 사용할 예비 Tape 두께입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI DIE FALLBACK", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiDieThicknessFallback, v => ResolveLivePlaceConfig().ContiDieThicknessFallback = Math.Max(0.0, v)),
                "Die 정보/프로젝트 Die 두께를 읽지 못했을 때 사용할 예비 Die 두께입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Bool("PLACE CONTI USE GLOBAL SPEED SCALE", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiUseGlobalSpeedScale, v => ResolveLivePlaceConfig().ContiUseGlobalSpeedScale = v),
                "Place ContiNode 속도에 MOTION 화면의 DEFAULT SPEED SCALE %를 적용할지 선택합니다.\r\nTrue: 전역 스케일을 적용합니다.\r\nFalse: PLACE CONTI MAX VEL/ACC/DEC와 NODE SPEED % 값만 사용합니다."), groupKey));
            AddPlaceContiNodeSpeedRatioItems(items, groupKey);
        }

        private void AddPlaceContiNodeSpeedRatioItems(List<ParameterGridItem> items, string groupKey)
        {
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI MAX VEL", AxisUnitConverter.Millimeter + "/s", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiMaxVelocity, v => ResolveLivePlaceConfig().ContiMaxVelocity = PickerPickUpMotionConfig.NormalizePositive(v, 500.0)),
                "ContiNode에서 사용할 최고 속도입니다. 각 node 속도는 이 값에 node별 SPEED %를 곱해 계산합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI MAX ACC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiMaxAcceleration, v => ResolveLivePlaceConfig().ContiMaxAcceleration = PickerPickUpMotionConfig.NormalizePositive(v, 5000.0)),
                "ContiNode에서 사용할 최고 가속도입니다. 각 node 가속도는 이 값에 node별 SPEED %를 곱해 계산합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI MAX DEC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiMaxDeceleration, v => ResolveLivePlaceConfig().ContiMaxDeceleration = PickerPickUpMotionConfig.NormalizePositive(v, 5000.0)),
                "ContiNode에서 사용할 최고 감속도입니다. 각 node 감속도는 이 값에 node별 SPEED %를 곱해 계산합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE0 SPEED %", "%", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiNode0SpeedPercent, v => ResolveLivePlaceConfig().ContiNode0SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(v, 1.0)),
                "node0 비율입니다. X/Y는 유지하고 이전 PickerZ(Z1)를 1단 회피 위치까지 천천히 상승시킵니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE1 SPEED %", "%", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiNode1SpeedPercent, v => ResolveLivePlaceConfig().ContiNode1SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(v, 20.0)),
                "node1 비율입니다. Z1을 2단 회피 위치까지 올리는 구간입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE2 SPEED %", "%", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiNode2SpeedPercent, v => ResolveLivePlaceConfig().ContiNode2SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(v, 100.0)),
                "node2 비율입니다. X/Y를 중간 위치로 보내면서 Z1/Z2를 Avoid 근처 위치로 이동합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE3 SPEED %", "%", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiNode3SpeedPercent, v => ResolveLivePlaceConfig().ContiNode3SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(v, 100.0)),
                "node3 비율입니다. X/Y를 Target Pos로 맞추고 Z2를 Place 직전 위치까지 접근시킵니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE4 SPEED %", "%", ParameterGridScope.Config, () => ResolveLivePlaceConfig().ContiNode4SpeedPercent, v => ResolveLivePlaceConfig().ContiNode4SpeedPercent = PickerPickUpMotionConfig.NormalizePercent(v, 1.0)),
                "node4 비율입니다. 제품 접촉/OverDrive 구간이므로 낮게 시작합니다."), groupKey));
        }

        // LoadSettings()가 Setup 객체를 교체하므로 offsets 객체를 캡처하지 않고 리졸버(Func)로 매번 라이브 객체를 따라간다.
        private void AddVisionPickerOffsetItems(
            List<ParameterGridItem> items,
            string prefix,
            Func<PickerVisionCoordinateOffsets> resolveOffsets,
            PickerAxis xAxis,
            PickerAxis yAxis,
            string groupKey)
        {
            if (resolveOffsets == null || resolveOffsets() == null)
                return;

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                string pickerName = "PICKER " + (index + 1);
                items.Add(InGroup(AxisDouble(prefix + " -> " + pickerName + " X OFFSET",
                    xAxis,
                    AxisUnitConverter.Millimeter,
                    ParameterGridScope.Setup,
                    () => resolveOffsets().OffsetX[index],
                    v => resolveOffsets().OffsetX[index] = v), groupKey));
                items.Add(InGroup(AxisDouble(prefix + " -> " + pickerName + " Y OFFSET",
                    yAxis,
                    AxisUnitConverter.Millimeter,
                    ParameterGridScope.Setup,
                    () => resolveOffsets().OffsetY[index],
                    v => resolveOffsets().OffsetY[index] = v), groupKey));
            }
        }

        private static ParameterGridItem InGroup(ParameterGridItem item, string groupKey)
        {
            item.GroupKey = groupKey;
            return item;
        }

        private static ParameterGridItem Describe(ParameterGridItem item, string description)
        {
            if (item != null)
                item.Description = description ?? string.Empty;
            return item;
        }

        private void AddPositionItem(List<ParameterGridItem> items, PickerAxis axis, string axisName, string displaySuffix, string positionName, string displayUnit, string memberDisplay, string groupKey, string description = "")
        {
            string display = axisName + " " + displaySuffix;
            // 티칭 값이 저장되는 스코프 — 그리드 항목과 PositionItem이 어긋나지 않도록 한 곳에서 정한다.
            const ParameterGridScope scope = ParameterGridScope.Config;
            PositionItem posItem = new PositionItem { DisplayName = display, Axis = axis, PositionName = positionName, Scope = scope };
            positionItems[display] = posItem;

            List<PositionItem> groupList;
            if (!groupMoves.TryGetValue(groupKey, out groupList))
            {
                groupList = new List<PositionItem>();
                groupMoves[groupKey] = groupList;
            }
            groupList.Add(posItem);

            // [티칭 위치 Config 이관 2026-08-18] 저장소가 Config로 옮겨졌으므로 스코프 표시도 Config다.
            string unitName = DisplayUnitFor(axis, displayUnit);
            ParameterGridItem item = ParameterGridItem.Double(memberDisplay, unitName, scope,
                () => ToAxisDisplay(unit.GetPickerTeachingPosition(axis, positionName), axis),
                v => unit.SetPickerAxisTeachingPosition(axis, positionName, FromAxisDisplay(v, axis)));
            item.UnitGetter = () => DisplayUnitFor(axis, displayUnit);

            item.Key = display;                     // 이동/티칭 조회는 전체 이름(positionItems 키)으로 매칭
            item.GroupKey = groupKey;
            item.Description = description ?? string.Empty;
            item.SupportsTeaching = true;           // 행에 MOVE/TEACH 버튼 표시(티칭 포지션)
            items.Add(item);
        }

        // 픽커 T는 4개 픽커와 Front/Rear가 같은 각도를 쓰므로 화면 행을 1개만 만든다(값은 레시피 공통 저장).
        // groupMoves에는 T0~T3 4축을 그대로 등록해야 그룹 AVOID 복귀가 4축을 모두 회전시킨다.
        private void AddCommonTPositionItem(List<ParameterGridItem> items, string displaySuffix, string positionName, string groupKey)
        {
            string display = CommonTDisplayName + " " + displaySuffix;
            positionItems[display] = new PositionItem
            {
                DisplayName = display,
                Axis = PickerAxis.PickerT0,
                PositionName = positionName,
                Scope = ParameterGridScope.Recipe,
                IsCommonT = true
            };

            List<PositionItem> groupList;
            if (!groupMoves.TryGetValue(groupKey, out groupList))
            {
                groupList = new List<PositionItem>();
                groupMoves[groupKey] = groupList;
            }
            foreach (PickerAxis tAxis in PickerTAxes)
                groupList.Add(new PositionItem { DisplayName = display, Axis = tAxis, PositionName = positionName, Scope = ParameterGridScope.Recipe });

            ParameterGridItem item = ParameterGridItem.Double(
                CommonTDisplayName,
                DisplayUnitFor(PickerAxis.PickerT0, AxisUnitConverter.Degree),
                ParameterGridScope.Recipe,
                () => ToAxisDisplay(unit.GetPickerTeachingPosition(PickerAxis.PickerT0, positionName), PickerAxis.PickerT0),
                v => unit.SetPickerAxisTeachingPosition(PickerAxis.PickerT0, positionName, FromAxisDisplay(v, PickerAxis.PickerT0)));
            item.UnitGetter = () => DisplayUnitFor(PickerAxis.PickerT0, AxisUnitConverter.Degree) + " (P#" + selectedManualPickerNo + ")";
            item.Key = display;
            item.GroupKey = groupKey;
            item.Description = "Front/Rear 4개 픽커가 함께 쓰는 T 각도입니다(레시피 저장). MOVE/TEACH는 MANUAL ACTION에서 선택한 픽커 축에만 적용됩니다.";
            item.SupportsTeaching = true;
            items.Add(item);
        }

        // 공통 T 행의 MOVE/TEACH는 선택된 픽커 축 하나에만 적용한다.
        // 이 경로에는 강제되는 T 인터락이 없어 4축 동시 회전은 위험 노출을 4배로 키운다.
        private PickerAxis ResolveActionAxis(PositionItem item)
        {
            return item != null && item.IsCommonT ? ResolvePickerTAxis(selectedManualPickerNo - 1) : item.Axis;
        }

        private string ResolveActionDisplayName(PositionItem item)
        {
            if (item == null)
                return string.Empty;

            return item.IsCommonT
                ? item.DisplayName + " PICKER #" + selectedManualPickerNo
                : item.DisplayName;
        }

        // 공통 T 행 TEACH는 Front/Rear 8개 T축의 타깃을 한 번에 바꾸므로 적용 범위와
        // 캡처 축, before-after 값을 확인창 본문에 명시한다.
        private string BuildCommonTeachDetail(PositionItem item)
        {
            if (item == null || !item.IsCommonT || unit == null)
                return null;

            PickerAxis captureAxis = ResolveActionAxis(item);
            BaseAxis axis = GetAxis(captureAxis);
            string unitLabel = DisplayUnitFor(PickerAxis.PickerT0, AxisUnitConverter.Degree);
            double before = ToAxisDisplay(unit.GetPickerTeachingPosition(PickerAxis.PickerT0, item.PositionName), PickerAxis.PickerT0);
            double after = axis != null ? ToAxisDisplay(axis.ActualPosition, PickerAxis.PickerT0) : before;

            return "이 값은 FRONT/REAR 픽커 8개 T축이 함께 쓰는 공통값입니다." + Environment.NewLine +
                   "캡처 축: PICKER #" + selectedManualPickerNo + " (" + captureAxis + ")" + Environment.NewLine +
                   "현재값 " + before.ToString("F3") + unitLabel + " -> " + after.ToString("F3") + unitLabel;
        }

        private ParameterGridItem AxisDouble(string displayName, PickerAxis axis, string fallbackUnit, ParameterGridScope scope, Func<double> getter, Action<double> setter, string unitSuffix = "")
        {
            ParameterGridItem item = ParameterGridItem.Double(
                displayName,
                DisplayUnitFor(axis, fallbackUnit) + unitSuffix,
                scope,
                () => ToAxisDisplay(getter(), axis),
                v => setter(FromAxisDisplay(v, axis)));
            item.UnitGetter = () => DisplayUnitFor(axis, fallbackUnit) + unitSuffix;
            return item;
        }

        private void BindIoPanel()
        {
            if (unit == null)
                return;

            List<IoCylinderItem> items = new List<IoCylinderItem>();

            // 2열 열우선 배치 ("FRONT PICKER" 접두사 생략): [1열] CDA TANK + P1·P2, [2열] VACUUM TANK + P3·P4
            items.Add(IoCylinderItem.Input("CDA TANK", () => unit.IsPickerCdaPressureOk()));
            for (int i = 1; i <= 4; i++)
            {
                if (i == 3)  // 2열(P3부터) 시작 전에 VACUUM TANK 삽입
                    items.Add(IoCylinderItem.Input("VACUUM TANK", () => unit.IsPickerVacuumPressureOk()));

                int pickerNo = i;
                items.Add(IoCylinderItem.Input("P" + pickerNo + " FLOW", () => unit.IsPickerFlowDetected(pickerNo)));
                items.Add(IoCylinderItem.Output("P" + pickerNo + " VACUUM", () => OutputOn(unit.Vacuums, pickerNo), on => { unit.SetPickerVacuum(pickerNo, on); return Task.FromResult(0); }, "ON", "OFF"));
                items.Add(IoCylinderItem.Output("P" + pickerNo + " BLOW", () => OutputOn(unit.Blows, pickerNo), on => { unit.SetPickerBlow(pickerNo, on); return Task.FromResult(0); }, "ON", "OFF"));
            }

            // 14개 항목을 2열(열당 7개)로 → 스크롤 없이 한눈에
            ioCylinderPanel.ColumnCount = 2;
            ioCylinderPanel.SetItems(items);
        }

        private void BindJogPanel()
        {
            if (unit == null)
                return;

            List<JogAxisItem> items = new List<JogAxisItem>();
            AddJogItem(items, "PICKER X", PickerAxis.PickerX, "X+", "X-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER Y", PickerAxis.PickerY, "Y+", "Y-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER T1", PickerAxis.PickerT0, "T+", "T-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER Z1", PickerAxis.PickerZ0, "Z+", "Z-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER T2", PickerAxis.PickerT1, "T+", "T-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER Z2", PickerAxis.PickerZ1, "Z+", "Z-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER T3", PickerAxis.PickerT2, "T+", "T-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER Z3", PickerAxis.PickerZ2, "Z+", "Z-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER T4", PickerAxis.PickerT3, "T+", "T-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER Z4", PickerAxis.PickerZ3, "Z+", "Z-", JogAxisControlKind.Vertical);
            jogAxisMoveControl.SpeedControl = jogSpeedControl;
            jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.PickerTabbed;
            jogAxisMoveControl.ShowCurrentSpeedMode = true;
            jogAxisMoveControl.ButtonAreaMinHeight = 360;
            jogAxisMoveControl.ButtonAreaMaxHeight = 700;
            jogAxisMoveControl.SetItems(items);

            // 위치 리스트만 열 우선 순서로 재배열: [1열] X,T1,Z1,T2,Z2  [2열] Y,T3,Z3,T4,Z4
            // (조그 패드는 원래 순서 유지 → PickerTabbed 그룹핑에 영향 없음)
            List<JogAxisItem> listItems = items;
            if (items.Count == 10)
            {
                listItems = new List<JogAxisItem>
                {
                    items[0], items[2], items[3], items[4], items[5],
                    items[1], items[6], items[7], items[8], items[9]
                };
            }
            jogPositionListControl.SetItems(listItems);
        }

        private void AddJogItem(List<JogAxisItem> items, string name, PickerAxis axisKey, string plus, string minus, JogAxisControlKind kind)
        {
            BaseAxis axis = GetAxis(axisKey);
            if (axis == null)
                return;

            bool theta = IsTheta(axisKey);
            JogAxisItem item = JogAxisItem.Single(name, axis, theta ? AxisUnitConverter.Degree : AxisUnitConverter.Millimeter, 1.0, plus, minus).WithControlKind(kind);
            item.StepMoveAsync = (jogItem, direction, speedType, customSpeed, axisStepDistance) => unit.JogStepAsync(axis, direction, speedType, customSpeed, axisStepDistance);
            item.ContinuousMoveAsync = (jogItem, direction, speedType, customSpeed) => unit.JogContinuousAsync(axis, direction, speedType, customSpeed);
            item.StopAsync = jogItem => unit.StopJogAsync(axis);
            items.Add(item);
        }

        private async void OptionParameterGrid_RowDoubleClicked(object sender, ParameterGridChangedEventArgs e)
        {
            string key = e != null && e.Item != null ? e.Item.Key : string.Empty;
            await MoveSelectedPositionAsync(key);
        }

        private void BindParameterGridMenus()
        {
            // 우클릭 메뉴 대신, 티칭 포지션 행의 MOVE/TEACH 버튼으로 이동/티칭 수행
            optionParameterGrid.ParameterMoveRequested += OptionParameterGrid_MoveRequested;
            optionParameterGrid.ParameterTeachRequested += OptionParameterGrid_TeachRequested;
        }

        private async void OptionParameterGrid_MoveRequested(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e == null || e.Item == null)
                    return;

                await MoveSelectedPositionAsync(e.Item.Key);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "FRONT-PICKER", "Move button failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Front Picker Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                PositionItem item;
                if (!positionItems.TryGetValue(e.Item.Key, out item))
                    return;

                if (!ConfirmTeachPosition("Front Picker Teach", item.DisplayName, BuildCommonTeachDetail(item)))
                    return;

                TeachSelectedPosition(e.Item.Key);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "FRONT-PICKER", "Teach button failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Front Picker Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task MoveSelectedPositionAsync(string key)
        {
            if (unit == null || string.IsNullOrWhiteSpace(key) || !positionItems.ContainsKey(key))
                return;

            PositionItem item = positionItems[key];
            PickerAxis axis = ResolveActionAxis(item);
            await ConfirmMoveToPositionAsync(ResolveActionDisplayName(item), delegate { return MovePickerTeachingPositionAsync(axis, item.PositionName); }, axis);
        }

        private Task<int> MovePickerTeachingPositionAsync(PickerAxis axis, string positionName)
        {
            return unit.MovePickerAxisToTeachingPosition(
                axis,
                positionName,
                jogAxisMoveControl.SelectedSpeedType,
                jogAxisMoveControl.GetSelectedSpeed(ResolvePickerBaseAxis(axis)));
        }

        private BaseAxis ResolvePickerBaseAxis(PickerAxis axis)
        {
            if (unit == null)
                return null;

            switch (axis)
            {
                case PickerAxis.PickerX: return unit.PickerX;
                case PickerAxis.PickerY: return unit.PickerY;
                case PickerAxis.PickerT0: return unit.PickerT0;
                case PickerAxis.PickerT1: return unit.PickerT1;
                case PickerAxis.PickerT2: return unit.PickerT2;
                case PickerAxis.PickerT3: return unit.PickerT3;
                case PickerAxis.PickerZ0: return unit.PickerZ0;
                case PickerAxis.PickerZ1: return unit.PickerZ1;
                case PickerAxis.PickerZ2: return unit.PickerZ2;
                case PickerAxis.PickerZ3: return unit.PickerZ3;
                default: return null;
            }
        }

        // ===================== 인터락 시퀀스 (FrontPicker) =====================
        // 순서 규칙: 하강 = Z→T / 상승 = T→Z(Z 마지막) / 수평 = Y→X
        // I1 X/Y 이동 전 Z 전부 상승  · I3 CDA/알람(이동 메서드 내부 검사)
        // I4 Z 하강 전 갠트리(X/Y) 짝 DIE 위치 정렬 · 홈게이트는 AVOID 전용
        private static readonly PickerAxis[] PickerZAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
        private static readonly PickerAxis[] PickerTAxes = { PickerAxis.PickerT0, PickerAxis.PickerT1, PickerAxis.PickerT2, PickerAxis.PickerT3 };

        // 인터락 진단/테스트 토글
        private static readonly bool RequireHomingForAvoid = true;     // AVOID 홈복귀 게이트

        // 마지막 시퀀스 중단 사유 — 실행 래퍼(RunSafeAsync)의 실패 팝업에 합쳐서 표시
        private string lastAbortReason;

        private List<PositionItem> GroupMembersByAxes(string groupKey, params PickerAxis[] axisFilter)
        {
            List<PositionItem> result = new List<PositionItem>();
            if (string.IsNullOrEmpty(groupKey) || !groupMoves.ContainsKey(groupKey))
                return result;

            HashSet<PickerAxis> set = new HashSet<PickerAxis>(axisFilter);
            foreach (PositionItem m in groupMoves[groupKey])
                if (set.Contains(m.Axis))
                    result.Add(m);
            return result;
        }

        // 목록을 순차 이동, 첫 실패에서 즉시 중단(코드 반환). CDA/공유레일/알람은 이동 메서드 내부에서 검사됨.
        private async Task<int> MoveMembersAsync(List<PositionItem> moves)
        {
            foreach (PositionItem m in moves)
            {
                int r = await MovePickerTeachingPositionAsync(m.Axis, m.PositionName);
                if (r != 0)
                    return r;
            }
            return 0;
        }

        private int AbortSeq(string title, string message)
        {
            QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Error, "FRONT-PICKER", "UI", title + " 시퀀스 중단: " + message);
            lastAbortReason = message;
            return -1;
        }

        // AVOID 홈게이트: 픽커 전 축 원점복귀(IsHomeDone) 완료 확인
        private bool CheckPickerHomedForAvoid(out string reason)
        {
            reason = string.Empty;
            PickerAxis[] all =
            {
                PickerAxis.PickerX, PickerAxis.PickerY,
                PickerAxis.PickerT0, PickerAxis.PickerT1, PickerAxis.PickerT2, PickerAxis.PickerT3,
                PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3
            };
            foreach (PickerAxis a in all)
            {
                if (!unit.IsPickerAxisHomeDone(a))
                {
                    reason = a.ToString().Replace("Picker", "") + " 원점복귀 필요";
                    return false;
                }
            }
            return true;
        }

        // I4: 헤드(Z/T) 하강 전, 갠트리 X/Y가 zone base 위치에 정렬됐는지 확인한다.
        private bool CheckGantryAlignedForKind(string kind, out string reason)
        {
            reason = string.Empty;
            string baseName;
            switch (kind)
            {
                // Pick 위치 기준 확인
                case "PICK": baseName = "PickPosition"; break;
                // Bottom 검사 위치 기준 확인
                case "BOTTOM": baseName = "BottomPosition"; break;
                // Side 검사 위치 기준 확인
                case "SIDE": baseName = "SidePosition"; break;
                // Place 위치 기준 확인
                case "PLACE": baseName = "PlacePosition"; break;
                default: reason = "알 수 없는 종류(" + kind + ")"; return false;
            }

            if (unit.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, baseName) &&
                unit.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, baseName))
                return true;

            reason = "갠트리(X/Y)가 " + kind + " zone 위치에 정렬되지 않음";
            return false;
        }

        // ① AVOID — T복귀 → Z상승 → Y → X(일반 Avoid)
        private async Task<int> MoveAvoidSequenceAsync()
        {
            const string title = "AVOID";
            if (unit == null)
                return -1;

            string reason;

            if (RequireHomingForAvoid && !CheckPickerHomedForAvoid(out reason))
                return AbortSeq(title, reason);

            // T 회전 인터락이 "짝 Z가 Avoid(상승)일 것"을 요구하므로, Z를 먼저 상승시킨 뒤 T를 복귀시킨다.
            int r = await MoveMembersAsync(GroupMembersByAxes("K_AVOID", PickerZAxes));
            if (r != 0) return AbortSeq(title, "Z 상승 실패 (CDA/알람 확인)");

            r = await MoveMembersAsync(GroupMembersByAxes("K_AVOID", PickerTAxes));
            if (r != 0) return AbortSeq(title, "T 복귀 실패 (CDA/알람 확인)");

            string zblock = unit.GetPickerZClearBlockReason();
            if (zblock != null) return AbortSeq(title, "Y/X 전 Z 상승 미완료: " + zblock);

            r = await MoveMembersAsync(GroupMembersByAxes("K_AVOID", PickerAxis.PickerY));
            if (r != 0) return AbortSeq(title, "Y 회피 실패");

            // AVOID의 X는 일반 AvoidPosition만 (Input/Output Avoid 제외)
            List<PositionItem> xMoves = GroupMembersByAxes("K_AVOID", PickerAxis.PickerX)
                .FindAll(m => string.Equals(m.PositionName, "AvoidPosition", StringComparison.OrdinalIgnoreCase));
            r = await MoveMembersAsync(xMoves);

            if (r != 0) return AbortSeq(title, "X 회피 실패 (공유레일 확인)");

            return 0;
        }

        // ② DIE PICK/BOTTOM/SIDE/PLACE — Z 전부 상승 확인 후 Y → X
        private async Task<int> MoveDieKindSequenceAsync(string title, string kindPos)
        {
            if (unit == null)
                return -1;

            string groupKey = "K_" + ResolveZoneGroupKind(kindPos);

            string zblock = unit.GetPickerZClearBlockReason();
            if (zblock != null) return AbortSeq(title, "X/Y 이동 전 Z 상승 미완료: " + zblock);

            // 공유레일 X 이동 인터락이 "PickerY가 Avoid일 것"을 요구하므로, Y가 Avoid인 상태에서 X를 먼저 옮긴다.
            int r = await MoveMembersAsync(GroupMembersByAxes(groupKey, PickerAxis.PickerX));
            if (r != 0) return AbortSeq(title, "X 이동 실패 (CDA/공유레일/알람 확인)");

            r = await MoveMembersAsync(GroupMembersByAxes(groupKey, PickerAxis.PickerY));
            if (r != 0) return AbortSeq(title, "Y 이동 실패 (CDA/공유레일/알람 확인)");

            return 0;
        }

        private static string ResolveZoneGroupKind(string kindPos)
        {
            if (string.Equals(kindPos, "DieBottomPosition", StringComparison.OrdinalIgnoreCase))
                return "BOTTOM";
            if (string.Equals(kindPos, "DieSidePosition", StringComparison.OrdinalIgnoreCase))
                return "SIDE";
            if (string.Equals(kindPos, "DiePlacePosition", StringComparison.OrdinalIgnoreCase))
                return "PLACE";
            return "PICK";
        }

        // ③ PICK/BOTTOM/SIDE/PLACE — 갠트리 정렬 확인 후 Z 하강 → T 회전
        private async Task<int> MoveHeadKindSequenceAsync(string kind)
        {
            if (unit == null)
                return -1;

            string groupKey = "K_" + kind;
            string reason;

            if (!CheckGantryAlignedForKind(kind, out reason))
                return AbortSeq(kind, "Z 하강 전 " + reason);

            // T 회전 인터락이 "같은 헤드의 Z가 Avoid(상승)일 것"을 요구하므로, Z가 상승해 있는 상태에서 T를 먼저 돌린 뒤 Z를 내린다.
            int r = await MoveMembersAsync(GroupMembersByAxes(groupKey, PickerTAxes));
            if (r != 0) return AbortSeq(kind, "T 회전 실패 (CDA/알람 확인)");

            r = await MoveMembersAsync(GroupMembersByAxes(groupKey, PickerZAxes));
            if (r != 0) return AbortSeq(kind, "Z 하강 실패 (CDA/알람 확인)");

            return 0;
        }

        private async Task ShowAppliedZoneMoveDialogAsync()
        {
            using (PickerAppliedZoneMoveDialog dialog = new PickerAppliedZoneMoveDialog("Front Applied Zone Move"))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                int pickerNo = dialog.PickerNo;
                string zoneText = dialog.ZoneText;
                string positionArrayName = dialog.PositionArrayName;
                string actionName = "APPLIED " + zoneText + " PICKER #" + pickerNo;
                await ConfirmAppliedZoneMoveAsync(actionName, pickerNo, positionArrayName, zoneText).ConfigureAwait(true);
            }
        }

        private ManualActionItem CreatePickerSelectItem(int pickerNo)
        {
            return ManualActionItem
                .Create(BuildPickerSelectText(pickerNo), () => SelectManualPickerAsync(pickerNo))
                .WithVisualKind(pickerNo == selectedManualPickerNo ? ManualActionVisualKind.PickerSelected : ManualActionVisualKind.PickerSelect);
        }

        private static ManualActionItem CreateManualMoveItem(string text, Func<Task> clickAsync)
        {
            return ManualActionItem.Create(text, clickAsync).WithColumnSpan(2);
        }

        private string BuildPickerSelectText(int pickerNo)
        {
            return pickerNo == selectedManualPickerNo
                ? "#" + pickerNo + " SELECTED"
                : "#" + pickerNo;
        }

        private Task SelectManualPickerAsync(int pickerNo)
        {
            selectedManualPickerNo = Math.Max(1, Math.Min(4, pickerNo));
            EventLogger.Write(EventKind.Event, "UI", "FRONT-PICKER",
                "Manual action picker selected. pickerNo=" + selectedManualPickerNo);
            ConfigureManualActions();
            return Task.FromResult(0);
        }

        private Task ConfirmSelectedAppliedZoneMoveAsync(string actionName, string positionArrayName, string zoneText)
        {
            return ConfirmAppliedZoneMoveAsync(
                actionName + " PICKER #" + selectedManualPickerNo,
                selectedManualPickerNo,
                positionArrayName,
                zoneText);
        }

        private async Task ConfirmAppliedZoneMoveAsync(string actionName, int pickerNo, string positionArrayName, string zoneText)
        {
            if (unit == null)
                return;

            if (ManualMoveGuard.BlockIfNotReady(this, "Front Picker"))
                return;

            PickerAxis zAxis = ResolvePickerZAxis(pickerNo - 1);
            PickerAxis tAxis = ResolvePickerTAxis(pickerNo - 1);
            if (!EnsureTargetAxesHomeDone(actionName, new[] { PickerAxis.PickerX, PickerAxis.PickerY, tAxis, zAxis }))
                return;

            RecipePickerMoveTarget target;
            string targetReason;
            if (!RecipePickerMoveTargetResolver.TryResolve(
                FindMachine(),
                PickerSequenceSide.Front,
                pickerNo,
                positionArrayName,
                zoneText,
                out target,
                out targetReason))
            {
                QMC.Common.MessageDialog.Show(this,
                    actionName + " 보정 적용 위치 계산 실패\r\n" + targetReason,
                    "Front Picker",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = QMC.Common.MessageDialog.Show(this,
                actionName + " 보정 적용 위치로 이동하시겠습니까?\r\n" +
                "Picker #" + pickerNo + " / Zone=" + zoneText + "\r\n" +
                "Source=" + target.SourceMode + "\r\n" +
                "Loaded Die=" + target.LoadedDieText + "\r\n" +
                "Final X=" + target.X.ToString("F3") + " mm\r\n" +
                "Final Y=" + target.Y.ToString("F3") + " mm\r\n" +
                "Final T=" + target.T.ToString("F3") + " deg\r\n" +
                "Final Z=" + target.Z.ToString("F3") + " mm\r\n" +
                "순서: Z 상승 -> Y 후진 -> X 이동 -> Y 전진 -> T 이동 -> Z 하강\r\n" +
                "\r\n축별 계산\r\n" +
                target.AxisFormula,
                "Front Picker", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            await RunSafeAsync(() => MoveAppliedZonePositionAsync(target, actionName, zoneText), actionName).ConfigureAwait(true);
        }

        private async Task<int> MoveAppliedZonePositionAsync(RecipePickerMoveTarget target, string actionName, string zoneText)
        {
            if (unit == null || target == null)
                return -1;

            EventLogger.Write(EventKind.Event, "UI", "FRONT-PICKER-APPLIED-ZONE",
                actionName + " target calculated. pickerNo=" + (target.PickerIndex + 1) +
                ", zone=" + zoneText +
                ", source=" + target.SourceMode +
                ", loadedDie=" + target.LoadedDieText +
                ", axisFormula=" + target.AxisFormula.Replace(Environment.NewLine, " | "));

            int r = await MoveMembersAsync(GroupMembersByAxes("K_AVOID", PickerZAxes)).ConfigureAwait(true);
            if (r != 0) return AbortSeq(actionName, "Z 상승 실패");

            r = await MovePickerAxisTargetAsync(
                PickerAxis.PickerY,
                unit.GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                BuildAppliedZoneTargetName(target, "SafeY")).ConfigureAwait(true);
            if (r != 0) return AbortSeq(actionName, "Y 후진 실패");

            r = await MovePickerAxisTargetAsync(
                PickerAxis.PickerX,
                target.X,
                BuildAppliedZoneTargetName(target, "X")).ConfigureAwait(true);
            if (r != 0) return AbortSeq(actionName, "X 이동 실패");

            r = await MovePickerAxisTargetAsync(
                PickerAxis.PickerY,
                target.Y,
                BuildAppliedZoneTargetName(target, "YForward")).ConfigureAwait(true);
            if (r != 0) return AbortSeq(actionName, "Y 전진 실패");

            r = await MovePickerAxisTargetAsync(
                target.PickerTAxis,
                target.T,
                BuildAppliedZoneTargetName(target, "T")).ConfigureAwait(true);
            if (r != 0) return AbortSeq(actionName, "T 이동 실패");

            r = await MovePickerAxisTargetAsync(
                target.PickerZAxis,
                target.Z,
                BuildAppliedZoneTargetName(target, "ZDown")).ConfigureAwait(true);
            if (r != 0) return AbortSeq(actionName, "Z 하강 실패");

            if (!IsPickerAxisInPosition(PickerAxis.PickerX, target.X) ||
                !IsPickerAxisInPosition(PickerAxis.PickerY, target.Y) ||
                !IsPickerAxisInPosition(target.PickerTAxis, target.T) ||
                !IsPickerAxisInPosition(target.PickerZAxis, target.Z))
            {
                return AbortSeq(actionName,
                    "최종 위치 확인 실패. " +
                    "X축[" + BuildAxisState(PickerAxis.PickerX, target.X) + "] / " +
                    "Y축[" + BuildAxisState(PickerAxis.PickerY, target.Y) + "] / " +
                    "T축[" + BuildAxisState(target.PickerTAxis, target.T) + "] / " +
                    "Z축[" + BuildAxisState(target.PickerZAxis, target.Z) + "]");
            }

            EventLogger.Write(EventKind.Event, "UI", "FRONT-PICKER-APPLIED-ZONE",
                actionName + " move complete. source=" + target.SourceMode +
                ", loadedDie=" + target.LoadedDieText +
                ", axisFormula=" + target.AxisFormula.Replace(Environment.NewLine, " | ") +
                ", axisState=X축[" + BuildAxisState(PickerAxis.PickerX, target.X) + "]" +
                ", Y축[" + BuildAxisState(PickerAxis.PickerY, target.Y) + "]" +
                ", T축[" + BuildAxisState(target.PickerTAxis, target.T) + "]" +
                ", Z축[" + BuildAxisState(target.PickerZAxis, target.Z) + "]" +
                " - Ok");
            return 0;
        }

        private Task<int> MovePickerAxisTargetAsync(PickerAxis axis, double target, string targetName)
        {
            return unit.MovePickerAxis(
                axis,
                target,
                jogAxisMoveControl.SelectedSpeedType,
                jogAxisMoveControl.GetSelectedSpeed(ResolvePickerBaseAxis(axis)),
                targetName);
        }

        private bool IsPickerAxisInPosition(PickerAxis axis, double target)
        {
            BaseAxis item = ResolvePickerBaseAxis(axis);
            if (item == null)
                return false;

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;
            return unit.IsFrontPickerAxisInPosition(axis, target, tolerance);
        }

        private string BuildAxisState(PickerAxis axis, double target)
        {
            BaseAxis item = ResolvePickerBaseAxis(axis);
            double tolerance = item != null && item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;
            if (item == null)
                return "axisState=[axis=null, target=" + target.ToString("F6") + "]";
            return "axisState=[actual=" + item.ActualPosition.ToString("F6") +
                   ", command=" + item.CommandPosition.ToString("F6") +
                   ", target=" + target.ToString("F6") +
                   ", tolerance=" + tolerance.ToString("F6") +
                   ", moving=" + item.IsMoving +
                   ", servo=" + item.IsServoOn +
                   ", alarm=" + item.IsAlarm + "]";
        }

        private static string BuildAppliedZoneTargetName(RecipePickerMoveTarget target, string phase)
        {
            return target.PositionArrayName + "[P" + target.PickerNo + "];RecipeAppliedZoneMove;PickerZone=" +
                   target.ZonePositionName + ";PickerPhase=" + phase + ";Source=" + target.SourceMode;
        }

        private static PickerAxis ResolvePickerTAxis(int pickerIndex)
        {
            if (pickerIndex <= 0) return PickerAxis.PickerT0;
            if (pickerIndex == 1) return PickerAxis.PickerT1;
            if (pickerIndex == 2) return PickerAxis.PickerT2;
            return PickerAxis.PickerT3;
        }

        private static PickerAxis ResolvePickerZAxis(int pickerIndex)
        {
            if (pickerIndex <= 0) return PickerAxis.PickerZ0;
            if (pickerIndex == 1) return PickerAxis.PickerZ1;
            if (pickerIndex == 2) return PickerAxis.PickerZ2;
            return PickerAxis.PickerZ3;
        }

        private async Task<int> RunFrontPickerZ1CycleTestAsync()
        {
            const int repeatCount = 50;
            const double lowPosition = 0.0;
            const double highPosition = -4.0;
            const PickerAxis axis = PickerAxis.PickerZ0; // 화면 표기 Front Picker #1 Z축

            if (unit == null)
                return -1;

            manualActionPanel.SetButtonsEnabled(false);   // 테스트 중 수동 이동 잠금

            System.Diagnostics.Stopwatch totalWatch = System.Diagnostics.Stopwatch.StartNew();
            EventLogger.Write(
                EventKind.Event,
                "UI",
                "FRONT-PICKER-Z1-CYCLE-TEST",
                "Front Picker #1 Z축 0<->2mm 50회 왕복 테스트 시작. axis=" + axis +
                ", low=" + lowPosition.ToString("0.###") +
                ", high=" + highPosition.ToString("0.###") +
                ", repeat=" + repeatCount);

            try
            {
                for (int i = 1; i <= repeatCount; i++)
                {
                    int result = await MoveFrontPickerZ1CycleTestAxisAsync(axis, highPosition, i, "UP").ConfigureAwait(true);
                    if (result != 0)
                        return result;

                    result = await MoveFrontPickerZ1CycleTestAxisAsync(axis, lowPosition, i, "DOWN").ConfigureAwait(true);
                    if (result != 0)
                        return result;
                }

                totalWatch.Stop();
                EventLogger.Write(
                    EventKind.Event,
                    "UI",
                    "FRONT-PICKER-Z1-CYCLE-TEST",
                    "Front Picker #1 Z축 0<->2mm 50회 왕복 테스트 완료. elapsedMs=" +
                    totalWatch.ElapsedMilliseconds);
                return 0;
            }
            finally
            {
                if (!manualActionPanel.IsDisposed)
                    manualActionPanel.SetButtonsEnabled(true);
            }
        }

        private async Task<int> MoveFrontPickerZ1CycleTestAxisAsync(PickerAxis axis, double target, int cycleNo, string direction)
        {
            BaseAxis baseAxis = GetAxis(axis);
            double before = baseAxis != null ? baseAxis.ActualPosition : 0.0;
            System.Diagnostics.Stopwatch moveWatch = System.Diagnostics.Stopwatch.StartNew();

            EventLogger.Write(
                EventKind.Event,
                "UI",
                "FRONT-PICKER-Z1-CYCLE-TEST",
                "Front Picker #1 Z축 테스트 이동 시작. cycle=" + cycleNo +
                ", direction=" + direction +
                ", target=" + target.ToString("0.###") +
                ", before=" + before.ToString("0.###"));

            int result = await unit.MovePickerAxis(
                axis,
                target,
                false,
                "FrontPickerZ1CycleTest").ConfigureAwait(true);

            moveWatch.Stop();
            double after = baseAxis != null ? baseAxis.ActualPosition : 0.0;
            EventLogger.Write(
                result == 0 ? EventKind.Event : EventKind.Alarm,
                "UI",
                "FRONT-PICKER-Z1-CYCLE-TEST",
                "Front Picker #1 Z축 테스트 이동 " + (result == 0 ? "완료" : "실패") +
                ". cycle=" + cycleNo +
                ", direction=" + direction +
                ", target=" + target.ToString("0.###") +
                ", before=" + before.ToString("0.###") +
                ", after=" + after.ToString("0.###") +
                ", elapsedMs=" + moveWatch.ElapsedMilliseconds +
                ", result=" + result);

            if (result != 0)
                lastAbortReason = "Front Picker #1 Z축 테스트 이동 실패. cycle=" + cycleNo +
                    ", direction=" + direction +
                    ", target=" + target.ToString("0.###") +
                    ", result=" + result;

            return result;
        }

        private void TeachSelectedPosition(string key)
        {
            if (unit == null || string.IsNullOrWhiteSpace(key) || !positionItems.ContainsKey(key))
                return;

            PositionItem item = positionItems[key];
            unit.TeachPickerAxisPosition(ResolveActionAxis(item), item.PositionName);
            // 값이 실제로 기록되는 스코프로 저장한다. Config 티칭을 Recipe 저장 경로로 보내면
            // 파일에 내려가지 않아 비정상 종료 시 티칭 결과가 사라진다.
            if (item.Scope == ParameterGridScope.Recipe)
                SaveCurrentRecipeData();
            else
                SaveCurrentSettingsData();
            RefreshView();
        }

        private bool ConfirmTeachPosition(string title, string actionName, string extraLines = null)
        {
            string name = string.IsNullOrWhiteSpace(actionName) ? "Teach Position" : actionName;
            using (var dialog = new QMC.Common.MessageBoxYesNo())
            {
                dialog.ButtonGroupLabel = "TEACH";
                DialogResult result = dialog.ShowDialog(
                    title,
                    name + " 현재 위치로 티칭하시겠습니까?" +
                        (string.IsNullOrEmpty(extraLines) ? string.Empty : Environment.NewLine + Environment.NewLine + extraLines),
                    this,
                    new[] { "Yes", "No" });

                if (result == DialogResult.Yes)
                    return true;

                EventLogger.Write(EventKind.Event, "UI", "FRONT-PICKER", name + " teach canceled.");
                return false;
            }
        }

        private static readonly PickerAxis[] AllPickerAxesForHomeCheck =
        {
            PickerAxis.PickerX, PickerAxis.PickerY,
            PickerAxis.PickerT0, PickerAxis.PickerT1, PickerAxis.PickerT2, PickerAxis.PickerT3,
            PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3
        };

        private async Task ConfirmMoveAsync(string actionName, Func<Task<int>> move, params PickerAxis[] targetAxes)
        {
            await ConfirmMoveInternalAsync(actionName, move, false, targetAxes);
        }

        private async Task ConfirmMoveToPositionAsync(string actionName, Func<Task<int>> move, params PickerAxis[] targetAxes)
        {
            await ConfirmMoveInternalAsync(actionName, move, true, targetAxes);
        }

        private async Task ConfirmMoveInternalAsync(string actionName, Func<Task<int>> move, bool selectMoveSpeed, params PickerAxis[] targetAxes)
        {
            if (unit == null || move == null)
                return;

            if (ManualMoveGuard.BlockIfNotReady(this, "Front Picker"))
                return;

            // 이동 대상 축의 HOME END(IsHomeDone) 미완료면 차단 — 우클릭 move / 액션 버튼 공통.
            if (!EnsureTargetAxesHomeDone(actionName, targetAxes))
                return;

            if (selectMoveSpeed)
            {
                JogSpeedType speedType;
                if (!ManualMoveGuard.ConfirmMoveSpeed(this, "Front Picker", actionName, out speedType))
                    return;

                jogAxisMoveControl.SetSelectedSpeedType(speedType);
            }
            else
            {
                DialogResult result = QMC.Common.MessageDialog.Show(this, actionName + " move?", "Front Picker", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    return;
            }

            await RunSafeAsync(move, actionName);
        }

        // 이동 대상 축의 HOME END(IsHomeDone) 확인. targetAxes 미지정(액션 버튼)이면 FrontPicker 전 축을 본다.
        private bool EnsureTargetAxesHomeDone(string actionName, PickerAxis[] targetAxes)
        {
            PickerAxis[] axes = (targetAxes != null && targetAxes.Length > 0) ? targetAxes : AllPickerAxesForHomeCheck;
            foreach (PickerAxis ax in axes)
            {
                if (!unit.IsPickerAxisHomeDone(ax))
                {
                    string axisName = "Front" + ax.ToString().Replace("Picker", "");
                    string msg = actionName + " 불가: " + axisName + " 축 HOME END(원점복귀)가 완료되지 않았습니다.";
                    EventLogger.Write(EventKind.Alarm, "UI", "FRONT-PICKER", msg);
                    QMC.Common.MessageDialog.Show(this, msg, "Front Picker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            return true;
        }

        private async Task RunSafeAsync(Func<Task<int>> action, string actionName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                lastAbortReason = null;
                int result;
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("FrontPickerRecipePage." + actionName))
                {
                    result = await action();
                }
                EventLogger.Write(EventKind.Event, "UI", "FRONT-PICKER", actionName + " result=" + result);
                if (result != 0)
                {
                    string detail = string.IsNullOrEmpty(lastAbortReason)
                        ? " failed. result=" + result
                        : " 실패" + Environment.NewLine + "사유 : " + lastAbortReason;
                    QMC.Common.MessageDialog.Show(this, actionName + detail, "Front Picker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                EventLogger.Write(EventKind.Alarm, "UI", "FRONT-PICKER", "Parameter save failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Front Picker Parameter Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetPickUpMechanicalOffsetLimit(PickerPickUpMotionConfig config, double value)
        {
            if (config == null || !CanEditMechanicalOffsetConfig())
                return;

            config.MechanicalOffsetLimitMm = PickerPickUpMotionConfig.NormalizeMechanicalOffsetLimit(value);
            config.Ensure();
        }

        private void SetPickUpMechanicalOffset(PickerPickUpMotionConfig config, int pickerIndex, bool isX, double value)
        {
            if (config == null || !CanEditMechanicalOffsetConfig())
                return;

            if (isX)
                config.SetMechanicalOffsetX(pickerIndex, value);
            else
                config.SetMechanicalOffsetY(pickerIndex, value);
        }

        private void SetPickUpMechanicalOffsetTLimit(PickerPickUpMotionConfig config, double value)
        {
            if (config == null || !CanEditMechanicalOffsetConfig())
                return;

            config.MechanicalOffsetTLimitDeg = PickerPickUpMotionConfig.NormalizeMechanicalOffsetLimit(value);
            config.Ensure();
        }

        private void SetPickUpMechanicalOffsetT(PickerPickUpMotionConfig config, int pickerIndex, double value)
        {
            if (config == null || !CanEditMechanicalOffsetConfig())
                return;

            config.SetMechanicalOffsetT(pickerIndex, value);
        }

        private void SetPlaceMechanicalOffsetLimit(PickerPlaceMotionConfig config, double value)
        {
            if (config == null || !CanEditMechanicalOffsetConfig())
                return;

            config.MechanicalOffsetLimitMm = PickerPickUpMotionConfig.NormalizeMechanicalOffsetLimit(value);
            config.Ensure();
        }

        private void SetBottomPlaceCorrectionLimit(PickerPlaceMotionConfig config, double value)
        {
            if (config == null || !CanEditMechanicalOffsetConfig())
                return;

            config.BottomPlaceCorrectionLimitMm =
                PickerPickUpMotionConfig.NormalizeMechanicalOffsetLimit(value);
            config.Ensure();
        }

        private void SetPlaceMechanicalOffset(PickerPlaceMotionConfig config, int pickerIndex, bool isX, double value)
        {
            if (config == null || !CanEditMechanicalOffsetConfig())
                return;

            if (isX)
                config.SetMechanicalOffsetX(pickerIndex, value);
            else
                config.SetMechanicalOffsetY(pickerIndex, value);
        }

        private void SetPlaceMechanicalOffsetTLimit(PickerPlaceMotionConfig config, double value)
        {
            if (config == null || !CanEditMechanicalOffsetConfig())
                return;

            config.MechanicalOffsetTLimitDeg = PickerPickUpMotionConfig.NormalizeMechanicalOffsetLimit(value);
            config.Ensure();
        }

        private void SetPlaceMechanicalOffsetT(PickerPlaceMotionConfig config, int pickerIndex, double value)
        {
            if (config == null || !CanEditMechanicalOffsetConfig())
                return;

            config.SetMechanicalOffsetT(pickerIndex, value);
        }

        private bool CanEditMechanicalOffsetConfig()
        {
            Form1 host = FindHostForm();
            if (host == null || host.Controller == null)
                return true;

            EquipmentStatus status = host.Controller.Status;
            bool busy =
                status == EquipmentStatus.AutoRunning ||
                status == EquipmentStatus.ManualRunning ||
                status == EquipmentStatus.Initializing ||
                host.Controller.IsSequenceRunning ||
                host.Controller.IsManualBusy;
            if (!busy)
                return true;

            const string message = "자동/수동 시퀀스 또는 초기화 중에는 Picker 기구 보정 Config를 변경할 수 없습니다.";
            EventLogger.Write(EventKind.Alarm, "UI", "FRONT-PICKER", message);
            QMC.Common.MessageDialog.Show(this, message, "Front Picker Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private void SaveCurrentRecipeData()
        {
            Form1 host = FindHostForm();
            if (host == null || string.IsNullOrWhiteSpace(host.ActiveRecipeName))
                throw new InvalidOperationException("활성 Recipe가 없어 Front Picker Recipe 값을 저장할 수 없습니다.");

            if (!host.SaveMachineRecipe(host.ActiveRecipeName))
                throw new InvalidOperationException(
                    "Front Picker Recipe 저장에 실패했습니다. 현재 적용값과 저장 파일의 값이 다를 수 있으며, " +
                    "재시작하면 이전값으로 복원될 수 있습니다. Alarm/Event Log를 확인하십시오. recipe=" +
                    host.ActiveRecipeName);
        }

        private void SaveCurrentSettingsData()
        {
            Form1 host = FindHostForm();
            if (host == null)
                throw new InvalidOperationException("Main 화면을 찾을 수 없어 Front Picker Config/Setup 값을 저장할 수 없습니다.");

            if (!host.SaveMachineSettings())
                throw new InvalidOperationException(
                    "Front Picker Config/Setup 저장에 실패했습니다. 현재 적용값과 저장 파일의 값이 다를 수 있으며, " +
                    "재시작하면 이전값으로 복원될 수 있습니다. Alarm/Event Log를 확인하십시오.");
        }

        private void RefreshView()
        {
            try
            {
                if (unit == null)
                    return;

                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();
                RefreshVisionView();
            }
            catch
            {
            }
        }

        private void RefreshVisionView()
        {
            string body =
                "FRONT PICKER" + Environment.NewLine +
                "X : " + AxisText(GetAxis(PickerAxis.PickerX), false) + Environment.NewLine +
                "Y : " + AxisText(GetAxis(PickerAxis.PickerY), false) + Environment.NewLine +
                "T1: " + AxisText(GetAxis(PickerAxis.PickerT0), true) + Environment.NewLine +
                "Z1: " + AxisText(GetAxis(PickerAxis.PickerZ0), false) + Environment.NewLine +
                "CDA: " + OnOff(unit.IsPickerCdaPressureOk()) + Environment.NewLine +
                "VAC: " + OnOff(unit.IsPickerVacuumPressureOk());

            lblVisionInfo.Text = "BOTTOM VISION" + Environment.NewLine + body;
            lblVisionInfo2.Text = "SIDE VISION 1" + Environment.NewLine + body;
            lblVisionInfo3.Text = "SIDE VISION 2" + Environment.NewLine + body;
        }

        private BaseAxis GetAxis(PickerAxis axis)
        {
            BaseAxis item;
            return unit != null && unit.Axes.TryGetValue(axis, out item) ? item : null;
        }

        private static bool IsTheta(PickerAxis axis)
        {
            return axis == PickerAxis.PickerT0 || axis == PickerAxis.PickerT1 || axis == PickerAxis.PickerT2 || axis == PickerAxis.PickerT3;
        }

        private static string AxisText(BaseAxis axis, bool theta)
        {
            if (axis == null)
                return "-";
            return AxisUnitConverter.FormatDisplay(axis.ActualPosition, axis, "0.###", true);
        }

        private string DisplayUnitFor(PickerAxis axis, string fallbackUnit)
        {
            BaseAxis item = GetAxis(axis);
            return item != null ? AxisUnitConverter.DisplayUnitFor(item) : AxisUnitConverter.Normalize(fallbackUnit);
        }

        private double ToAxisDisplay(double nativeValue, PickerAxis axis)
        {
            BaseAxis item = GetAxis(axis);
            return item != null ? AxisUnitConverter.ToDisplay(nativeValue, item) : AxisUnitConverter.ToDisplay(nativeValue, DisplayUnitFor(axis, AxisUnitConverter.Millimeter));
        }

        private double FromAxisDisplay(double displayValue, PickerAxis axis)
        {
            BaseAxis item = GetAxis(axis);
            return item != null ? AxisUnitConverter.FromDisplay(displayValue, item) : AxisUnitConverter.FromDisplay(displayValue, DisplayUnitFor(axis, AxisUnitConverter.Millimeter));
        }

        private static string OnOff(bool value)
        {
            return value ? "ON" : "OFF";
        }

        private static bool OutputOn(IList<BaseDigitalOutput> outputs, int pickerNo)
        {
            int index = pickerNo - 1;
            return outputs != null && index >= 0 && index < outputs.Count && outputs[index] != null && outputs[index].IsOn;
        }

        private CDT320_Machine FindMachine()
        {
            Form1 host = FindHostForm();
            return host != null ? host.Machine : null;
        }

        private Form1 FindHostForm()
        {
            foreach (Form form in Application.OpenForms)
            {
                Form1 host = form as Form1;
                if (host != null)
                    return host;
            }
            return FindForm() as Form1;
        }
    }
}
