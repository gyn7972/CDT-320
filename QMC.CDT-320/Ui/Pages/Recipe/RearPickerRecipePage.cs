using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public sealed partial class RearPickerRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private sealed class PositionItem
        {
            public string DisplayName;
            public PickerAxis Axis;
            public string PositionName;
        }

        private readonly Timer refreshTimer = new Timer();
        private readonly Dictionary<string, PositionItem> positionItems = new Dictionary<string, PositionItem>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PositionItem>> groupMoves = new Dictionary<string, List<PositionItem>>(StringComparer.OrdinalIgnoreCase);
        private PickerRearUnit unit;

        public RearPickerRecipePage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            BackColor = Color.FromArgb(207, 210, 214);
            ForeColor = Color.Black;
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
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            lblHeader.Tag = "i18n:recipe.rearHead";
            lblHeader.Text = Lang.T("recipe.rearHead");
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

        private void ResolveUnit()
        {
            CDT320_Machine machine = FindMachine();
            unit = machine != null ? machine.PickerRearUnit : null;
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
        private void ConfigureManualActions()
        {
            try
            {
                // 공용 MANUAL ACTION 판넬에 위치 이동 버튼 등록 (2열, 행 수 자동)
                manualActionPanel.ColumnCount = 2;
                manualActionPanel.SetItems(new[]
                {
                    ManualActionItem.Create("AVOID POSITION", () => ConfirmMoveAsync("AVOID POSITION", MoveAvoidSequenceAsync)),
                    ManualActionItem.Create("PICK POSITION", () => ConfirmMoveAsync("PICK POSITION", () => MoveHeadKindSequenceAsync("PICK"))),
                    ManualActionItem.Create("BOTTOM POSITION", () => ConfirmMoveAsync("BOTTOM POSITION", () => MoveHeadKindSequenceAsync("BOTTOM"))),
                    ManualActionItem.Create("SIDE POSITION", () => ConfirmMoveAsync("SIDE POSITION", () => MoveHeadKindSequenceAsync("SIDE"))),
                    ManualActionItem.Create("PLACE POSITION", () => ConfirmMoveAsync("PLACE POSITION", () => MoveHeadKindSequenceAsync("PLACE"))),
                    ManualActionItem.Create("DIE PICK POSITION", () => ConfirmMoveAsync("DIE PICK POSITION", () => MoveDieKindSequenceAsync("DIE PICK", "DiePickPosition"))),
                    ManualActionItem.Create("DIE BOTTOM POSITION", () => ConfirmMoveAsync("DIE BOTTOM POSITION", () => MoveDieKindSequenceAsync("DIE BOTTOM", "DieBottomPosition"))),
                    ManualActionItem.Create("DIE SIDE POSITION", () => ConfirmMoveAsync("DIE SIDE POSITION", () => MoveDieKindSequenceAsync("DIE SIDE", "DieSidePosition"))),
                    ManualActionItem.Create("DIE PLACE POSITION", () => ConfirmMoveAsync("DIE PLACE POSITION", () => MoveDieKindSequenceAsync("DIE PLACE", "DiePlacePosition")))
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "REAR-PICKER", "ConfigureManualActions failed: " + ex.Message);
            }
            finally
            {
            }
        }
        private async void btnAvoidPosition_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("AVOID POSITION", MoveAvoidSequenceAsync);
        }

        private async void btnPickPosition_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("PICK POSITION", () => MoveHeadKindSequenceAsync("PICK"));
        }

        private async void btnBottomPosition_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("BOTTOM POSITION", () => MoveHeadKindSequenceAsync("BOTTOM"));
        }

        private async void btnSidePosition_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("SIDE POSITION", () => MoveHeadKindSequenceAsync("SIDE"));
        }

        private async void btnPlacePosition_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("PLACE POSITION", () => MoveHeadKindSequenceAsync("PLACE"));
        }

        private async void btnDiePickPosition_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("DIE PICK POSITION", () => MoveDieKindSequenceAsync("DIE PICK", "DiePickPosition"));
        }

        private async void btnDieBottomPosition_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("DIE BOTTOM POSITION", () => MoveDieKindSequenceAsync("DIE BOTTOM", "DieBottomPosition"));
        }

        private async void btnDieSidePosition_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("DIE SIDE POSITION", () => MoveDieKindSequenceAsync("DIE SIDE", "DieSidePosition"));
        }

        private async void btnDiePlacePosition_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("DIE PLACE POSITION", () => MoveDieKindSequenceAsync("DIE PLACE", "DiePlacePosition"));
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
                "InputAvoidPosition은 Input 카메라 위치가 아니라 Rear Picker X가 Input-side 방향으로 빠져 간섭을 피하기 위한 회피 위치입니다.");
            AddPositionItem(optionItems, PickerAxis.PickerX, "PICKER X", "OUTPUT-SIDE AVOID POSITION", "OutputAvoidPosition", AxisUnitConverter.Millimeter, "PICKER X OUTPUT-SIDE AVOID", "K_AVOID",
                "OutputAvoidPosition은 Output 카메라 위치가 아니라 Rear Picker X가 Output-side 방향으로 빠져 간섭을 피하기 위한 회피 위치입니다.");
            AddPositionItem(optionItems, PickerAxis.PickerX, "PICKER X", "AVOID POSITION", "AvoidPosition", AxisUnitConverter.Millimeter, "PICKER X", "K_AVOID");
            AddPositionItem(optionItems, PickerAxis.PickerY, "PICKER Y", "AVOID POSITION", "AvoidPosition", AxisUnitConverter.Millimeter, "PICKER Y", "K_AVOID");
            for (int i = 0; i < tzKeys.Length; i++)
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
                for (int i = 0; i < tzKeys.Length; i++)
                    AddPositionItem(optionItems, tzKeys[i], tzNames[i], zoneKinds[k] + " POSITION", zoneKindPos[k], tzUnits[i], tzNames[i], gk);
            }

            const string pickerSettingGroup = "K_PICKER_SETTING";
            optionItems.Add(ParameterGridItem.Header("PICKER SETTING", pickerSettingGroup));
            optionItems.Add(InGroup(ParameterGridItem.Bool("REAR PICKER USE", ParameterGridScope.Config, () => unit.Config.UseUnit, v => unit.Config.UseUnit = v), pickerSettingGroup));
            optionItems.Add(InGroup(ParameterGridItem.Selection("RUN ORDER MODE", "mode", ParameterGridScope.Config, () => unit.Config.RunOrderMode, v => unit.Config.RunOrderMode = v), pickerSettingGroup));
            optionItems.Add(InGroup(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v), pickerSettingGroup));
            AddPickerConfigItems(optionItems, pickerSettingGroup);

            const string pickUpSettingGroup = "K_PICKUP_SETTING";
            optionItems.Add(ParameterGridItem.Header("PICKUP SETTING", pickUpSettingGroup));
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
            AddVisionPickerOffsetItems(optionItems, "INPUT VISION", unit.Setup.InputVisionToPicker, PickerAxis.PickerX, PickerAxis.PickerY, visionOffsetGroup);
            AddVisionPickerOffsetItems(optionItems, "OUTPUT VISION", unit.Setup.OutputVisionToPicker, PickerAxis.PickerX, PickerAxis.PickerY, visionOffsetGroup);

            optionParameterGrid.SetItems(optionItems);

            waitParameterGrid.AutoFitParentGroupHeight = true;   // WAIT 그룹 높이를 내용에 맞춰 자동 조정 (스크롤 없이 전 항목 표시)
                waitParameterGrid.SetItems(new[]
            {
                ParameterGridItem.Int("PICK LIFT WAIT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.PickLiftWaitMs, v => unit.Recipe.PickLiftWaitMs = Math.Max(0, v)),
                ParameterGridItem.Int("PLACE DELAY", "ms", ParameterGridScope.Recipe, () => unit.Recipe.PlaceDelayMs, v => unit.Recipe.PlaceDelayMs = Math.Max(0, v)),
                AxisDouble("PICK LIFT POSITION", PickerAxis.PickerZ0, AxisUnitConverter.Millimeter, ParameterGridScope.Recipe, () => unit.Recipe.PickLiftPosition, v => unit.Recipe.PickLiftPosition = v)
            });
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

        private void AddPickUpSettingItems(List<ParameterGridItem> items, string groupKey)
        {
            PickerPickUpMotionConfig pickUp = unit.Config.PickUp;
            if (pickUp == null)
                unit.Config.PickUp = pickUp = new PickerPickUpMotionConfig();

            pickUp.Ensure();
            items.Add(InGroup(ParameterGridItem.Selection<PickerPickUpZMotionMode>("PICKUP Z MOTION MODE", "mode", ParameterGridScope.Config, () => pickUp.MotionMode, v => pickUp.MotionMode = v), groupKey));
            items.Add(InGroup(ParameterGridItem.Double("PICKER Z PRE PICK DISTANCE", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => pickUp.PickerZPrePickDistance, v => pickUp.PickerZPrePickDistance = Math.Max(0.0, v)), groupKey));
            items.Add(InGroup(ParameterGridItem.Double("PICKER Z APPROACH SPEED", "%", ParameterGridScope.Config, () => pickUp.PickerZSlowApproachSpeedPercent, v => pickUp.PickerZSlowApproachSpeedPercent = PickerPickUpMotionConfig.NormalizePercent(v, 1.0)), groupKey));
            items.Add(InGroup(ParameterGridItem.Double("PICKER Z SYNC LIFT DISTANCE", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => pickUp.PickerZSyncLiftDistance, v => pickUp.PickerZSyncLiftDistance = Math.Max(0.0, v)), groupKey));
            items.Add(InGroup(ParameterGridItem.Double("PICKER Z SYNC LIFT VELOCITY", AxisUnitConverter.Millimeter + "/s", ParameterGridScope.Config, () => pickUp.PickerZSyncLiftVelocity, v => pickUp.PickerZSyncLiftVelocity = PickerPickUpMotionConfig.NormalizePositive(v, 5.0)), groupKey));
            items.Add(InGroup(ParameterGridItem.Double("PICKER Z SYNC LIFT ACC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => pickUp.PickerZSyncLiftAcceleration, v => pickUp.PickerZSyncLiftAcceleration = PickerPickUpMotionConfig.NormalizePositive(v, 100.0)), groupKey));
            items.Add(InGroup(ParameterGridItem.Double("PICKER Z SYNC LIFT DEC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => pickUp.PickerZSyncLiftDeceleration, v => pickUp.PickerZSyncLiftDeceleration = PickerPickUpMotionConfig.NormalizePositive(v, 100.0)), groupKey));
            items.Add(InGroup(ParameterGridItem.Double("PICKER Z SEPARATE DISTANCE", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => pickUp.PickerZSeparateDistance, v => pickUp.PickerZSeparateDistance = Math.Max(0.0, v)), groupKey));
            items.Add(InGroup(ParameterGridItem.Double("PICKER Z SEPARATE SPEED", "%", ParameterGridScope.Config, () => pickUp.PickerZSeparateSpeedPercent, v => pickUp.PickerZSeparateSpeedPercent = PickerPickUpMotionConfig.NormalizePercent(v, 1.0)), groupKey));
            items.Add(InGroup(ParameterGridItem.Selection<PickerPickUpSeparateMode>("SEPARATE MODE", "mode", ParameterGridScope.Config, () => pickUp.SeparateMode, v => pickUp.SeparateMode = v), groupKey));
            items.Add(InGroup(ParameterGridItem.Int("VACUUM BEFORE PICK DELAY", "ms", ParameterGridScope.Config, () => pickUp.VacuumOnBeforePickDelayMs, v => pickUp.VacuumOnBeforePickDelayMs = Math.Max(0, v)), groupKey));
            items.Add(InGroup(ParameterGridItem.Int("PICK SETTLE", "ms", ParameterGridScope.Config, () => pickUp.PickSettleMs, v => pickUp.PickSettleMs = Math.Max(0, v)), groupKey));
        }

        private void AddBottomMotionSettingItems(List<ParameterGridItem> items, string groupKey)
        {
            PickerBottomInspectionMotionConfig bottom = unit.Config.BottomInspection;
            if (bottom == null)
                unit.Config.BottomInspection = bottom = new PickerBottomInspectionMotionConfig();

            bottom.Ensure();
            items.Add(InGroup(Describe(ParameterGridItem.Selection<PickerBottomFlyingZDownMode>("BOTTOM FLYING Z DOWN MODE", "mode", ParameterGridScope.Config, () => bottom.FlyingZDownMode, v => bottom.FlyingZDownMode = v),
                "Bottom 검사 위치로 X/Y/T 이동하는 동안 Picker Z를 미리 내릴지 정합니다.\r\nOff: 미리 내리지 않음\r\nDownDistance: Avoid 위치에서 지정 거리만큼 먼저 하강\r\nToBottomPosition: Bottom 검사 Z 위치까지 바로 하강"), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("BOTTOM FLYING Z DOWN DISTANCE", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => bottom.FlyingZDownDistance, v => bottom.FlyingZDownDistance = PickerBottomInspectionMotionConfig.NormalizeDistance(v)),
                "DOWN MODE가 DownDistance일 때 사용할 선행 하강 거리입니다.\r\n예: 2 mm면 Avoid 위치에서 2 mm만 먼저 내려가고, 이후 정식 Bottom Z 위치로 이동합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Selection<PickerBottomFlyingZStartMode>("BOTTOM FLYING Z START MODE", "mode", ParameterGridScope.Config, () => bottom.FlyingZStartMode, v => bottom.FlyingZStartMode = v),
                "Z 선행 하강을 언제 시작할지 정합니다.\r\nImmediate: X/Y/T 이동 시작과 거의 동시에 시작\r\nDelayMs: 지정 시간 대기 후 시작\r\nXRemainingDistance: X축 목표까지 남은 거리가 설정값 이하일 때 시작"), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Int("BOTTOM FLYING Z START DELAY", "ms", ParameterGridScope.Config, () => bottom.FlyingZStartDelayMs, v => bottom.FlyingZStartDelayMs = Math.Max(0, v)),
                "START MODE가 DelayMs일 때 대기할 시간입니다.\r\n0 ms면 지연 없이 바로 시작합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("BOTTOM FLYING Z START X REMAINING", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => bottom.FlyingZStartXRemainingDistance, v => bottom.FlyingZStartXRemainingDistance = PickerBottomInspectionMotionConfig.NormalizeDistance(v)),
                "START MODE가 XRemainingDistance일 때 사용하는 X축 잔여 거리 기준입니다.\r\n예: 5 mm면 Picker X가 목표 위치 5 mm 이내로 들어온 뒤 Z 선행 하강을 시작합니다."), groupKey));
        }

        private void AddPlaceMotionSettingItems(List<ParameterGridItem> items, string groupKey)
        {
            PickerPlaceMotionConfig place = unit.Config.Place;
            if (place == null)
                unit.Config.Place = place = new PickerPlaceMotionConfig();

            place.Ensure();
            items.Add(InGroup(Describe(ParameterGridItem.Selection<PickerPlaceMotionMode>("PLACE MOTION MODE", "mode", ParameterGridScope.Config, () => place.MotionMode, v => place.MotionMode = v),
                "Default는 기존 Place 이동 순서를 사용합니다.\r\nSynchronizedArrival은 OutputStageY/PickerX/PickerZ 동시 도착 보간을 사용합니다.\r\nContiSegmentedPlace는 이전 Z1 상승과 현재 Z2 접근을 5개 ContiNode로 나누어 연속 구동합니다."), groupKey));
            items.Add(InGroup(ParameterGridItem.Int("PLACE SYNC COORDINATE", "coord", ParameterGridScope.Config, () => place.InterpolationCoordinate, v => place.InterpolationCoordinate = Math.Max(0, v)), groupKey));
            items.Add(InGroup(ParameterGridItem.Double("PLACE SYNC VELOCITY", AxisUnitConverter.Millimeter + "/s", ParameterGridScope.Config, () => place.SynchronizedVelocity, v => place.SynchronizedVelocity = PickerPickUpMotionConfig.NormalizePositive(v, 1.0)), groupKey));
            items.Add(InGroup(ParameterGridItem.Double("PLACE SYNC ACC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => place.SynchronizedAcceleration, v => place.SynchronizedAcceleration = PickerPickUpMotionConfig.NormalizePositive(v, 10.0)), groupKey));
            items.Add(InGroup(ParameterGridItem.Double("PLACE SYNC DEC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => place.SynchronizedDeceleration, v => place.SynchronizedDeceleration = PickerPickUpMotionConfig.NormalizePositive(v, 10.0)), groupKey));
            items.Add(InGroup(ParameterGridItem.Int("PLACE SYNC TIMEOUT", "ms", ParameterGridScope.Config, () => place.SynchronizedTimeoutMs, v => place.SynchronizedTimeoutMs = Math.Max(1, v)), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE SYNC MAX TRAVEL", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => place.MaxSynchronizedTravelDistance, v => place.MaxSynchronizedTravelDistance = PickerPickUpMotionConfig.NormalizePositive(v, 37.0)),
                "현재 위치에서 Place 목표 위치까지 한 축이라도 이 거리보다 많이 움직이면 보간을 사용하지 않고 기존 이동 방식으로 접근합니다.\r\n알람/정지 후 Avoid 위치에서 재시작할 때 긴 거리를 보간으로 이동하지 않게 막는 값입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI Z1 STEP1 CLEAR", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => place.ContiZ1Step1Clearance, v => place.ContiZ1Step1Clearance = Math.Max(0.0, v)),
                "ContiSegmentedPlace node0에서 이전 PickerZ(Z1)를 티칭 Place 기준 + Tape + Die 위치보다 위로 올리는 1단 회피량입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI Z1 STEP2 CLEAR", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => place.ContiZ1Step2Clearance, v => place.ContiZ1Step2Clearance = Math.Max(0.0, v)),
                "ContiSegmentedPlace node1에서 이전 PickerZ(Z1)를 추가로 올리는 2단 회피량입니다. node0보다 빠른 속도 설정을 사용할 수 있습니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NEAR AVOID", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => place.ContiNearAvoidDistance, v => place.ContiNearAvoidDistance = Math.Max(0.0, v)),
                "node2/node3에서 Z1과 Z2가 Avoid 바로 전까지 접근할 거리입니다. 1 mm이면 Avoid 위치에서 Place 방향으로 1 mm 내려온 위치를 사용합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI XY MID RATIO", "ratio", ParameterGridScope.Config, () => place.ContiXYMidRatio, v => place.ContiXYMidRatio = Math.Max(0.0, Math.Min(1.0, v))),
                "node2의 X/Y 중간 위치 비율입니다. 0.5면 현재 위치와 Target Pos의 중간까지 이동합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI OVERDRIVE", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => place.ContiOverDrive, v => place.ContiOverDrive = Math.Max(0.0, v)),
                "node4에서 현재 PickerZ(Z2)가 최종 Place 위치에 더 들어가는 OverDrive 값입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI TAPE FALLBACK", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => place.ContiTapeThicknessFallback, v => place.ContiTapeThicknessFallback = Math.Max(0.0, v)),
                "프로젝트/웨이퍼 정보에서 Tape 두께를 읽지 못했을 때 사용할 예비 Tape 두께입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI DIE FALLBACK", AxisUnitConverter.Millimeter, ParameterGridScope.Config, () => place.ContiDieThicknessFallback, v => place.ContiDieThicknessFallback = Math.Max(0.0, v)),
                "Die 정보/프로젝트 Die 두께를 읽지 못했을 때 사용할 예비 Die 두께입니다."), groupKey));
            AddPlaceContiNodeMotionItems(items, groupKey, place);
        }

        private void AddPlaceContiNodeMotionItems(List<ParameterGridItem> items, string groupKey, PickerPlaceMotionConfig place)
        {
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE0 VEL", AxisUnitConverter.Millimeter + "/s", ParameterGridScope.Config, () => place.ContiNode0Velocity, v => place.ContiNode0Velocity = PickerPickUpMotionConfig.NormalizePositive(v, 2.0)),
                "node0 속도입니다. X/Y는 유지하고 이전 PickerZ(Z1)를 1단 회피 위치까지 천천히 상승시킵니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE0 ACC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => place.ContiNode0Acceleration, v => place.ContiNode0Acceleration = PickerPickUpMotionConfig.NormalizePositive(v, 20.0)),
                "node0 가속도입니다. Z1 초기 상승 충격을 줄이기 위해 보수적으로 설정합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE0 DEC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => place.ContiNode0Deceleration, v => place.ContiNode0Deceleration = PickerPickUpMotionConfig.NormalizePositive(v, 20.0)),
                "node0 감속도입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE1 VEL", AxisUnitConverter.Millimeter + "/s", ParameterGridScope.Config, () => place.ContiNode1Velocity, v => place.ContiNode1Velocity = PickerPickUpMotionConfig.NormalizePositive(v, 5.0)),
                "node1 속도입니다. Z1을 2단 회피 위치까지 올리는 구간으로 node0보다 빠르게 설정할 수 있습니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE1 ACC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => place.ContiNode1Acceleration, v => place.ContiNode1Acceleration = PickerPickUpMotionConfig.NormalizePositive(v, 50.0)),
                "node1 가속도입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE1 DEC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => place.ContiNode1Deceleration, v => place.ContiNode1Deceleration = PickerPickUpMotionConfig.NormalizePositive(v, 50.0)),
                "node1 감속도입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE2 VEL", AxisUnitConverter.Millimeter + "/s", ParameterGridScope.Config, () => place.ContiNode2Velocity, v => place.ContiNode2Velocity = PickerPickUpMotionConfig.NormalizePositive(v, 5.0)),
                "node2 속도입니다. X/Y를 중간 위치로 보내면서 Z1/Z2를 Avoid 근처 위치로 이동합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE2 ACC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => place.ContiNode2Acceleration, v => place.ContiNode2Acceleration = PickerPickUpMotionConfig.NormalizePositive(v, 50.0)),
                "node2 가속도입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE2 DEC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => place.ContiNode2Deceleration, v => place.ContiNode2Deceleration = PickerPickUpMotionConfig.NormalizePositive(v, 50.0)),
                "node2 감속도입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE3 VEL", AxisUnitConverter.Millimeter + "/s", ParameterGridScope.Config, () => place.ContiNode3Velocity, v => place.ContiNode3Velocity = PickerPickUpMotionConfig.NormalizePositive(v, 2.0)),
                "node3 속도입니다. X/Y를 Target Pos로 맞추고 Z2를 Place 직전 위치까지 접근시킵니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE3 ACC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => place.ContiNode3Acceleration, v => place.ContiNode3Acceleration = PickerPickUpMotionConfig.NormalizePositive(v, 20.0)),
                "node3 가속도입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE3 DEC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => place.ContiNode3Deceleration, v => place.ContiNode3Deceleration = PickerPickUpMotionConfig.NormalizePositive(v, 20.0)),
                "node3 감속도입니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE4 VEL", AxisUnitConverter.Millimeter + "/s", ParameterGridScope.Config, () => place.ContiNode4Velocity, v => place.ContiNode4Velocity = PickerPickUpMotionConfig.NormalizePositive(v, 0.5)),
                "node4 속도입니다. X/Y Target 상태에서 Z1은 Avoid로, Z2는 최종 Place/OverDrive 위치로 이동합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE4 ACC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => place.ContiNode4Acceleration, v => place.ContiNode4Acceleration = PickerPickUpMotionConfig.NormalizePositive(v, 10.0)),
                "node4 가속도입니다. 제품 접촉 구간이므로 낮게 시작하는 것을 권장합니다."), groupKey));
            items.Add(InGroup(Describe(ParameterGridItem.Double("PLACE CONTI NODE4 DEC", AxisUnitConverter.Millimeter + "/s2", ParameterGridScope.Config, () => place.ContiNode4Deceleration, v => place.ContiNode4Deceleration = PickerPickUpMotionConfig.NormalizePositive(v, 10.0)),
                "node4 감속도입니다."), groupKey));
        }

        private void AddVisionPickerOffsetItems(
            List<ParameterGridItem> items,
            string prefix,
            PickerVisionCoordinateOffsets offsets,
            PickerAxis xAxis,
            PickerAxis yAxis,
            string groupKey)
        {
            if (offsets == null)
                return;

            offsets.EnsureArrays();
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                string pickerName = "PICKER " + (index + 1);
                items.Add(InGroup(AxisDouble(prefix + " -> " + pickerName + " X OFFSET",
                    xAxis,
                    AxisUnitConverter.Millimeter,
                    ParameterGridScope.Setup,
                    () => offsets.OffsetX[index],
                    v => offsets.OffsetX[index] = v), groupKey));
                items.Add(InGroup(AxisDouble(prefix + " -> " + pickerName + " Y OFFSET",
                    yAxis,
                    AxisUnitConverter.Millimeter,
                    ParameterGridScope.Setup,
                    () => offsets.OffsetY[index],
                    v => offsets.OffsetY[index] = v), groupKey));
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
            PositionItem posItem = new PositionItem { DisplayName = display, Axis = axis, PositionName = positionName };
            positionItems[display] = posItem;

            List<PositionItem> groupList;
            if (!groupMoves.TryGetValue(groupKey, out groupList))
            {
                groupList = new List<PositionItem>();
                groupMoves[groupKey] = groupList;
            }
            groupList.Add(posItem);

            string unitName = DisplayUnitFor(axis, displayUnit);
            ParameterGridItem item = ParameterGridItem.Double(memberDisplay, unitName, ParameterGridScope.Recipe,
                () => ToAxisDisplay(unit.GetPickerTeachingPosition(axis, positionName), axis),
                v => SetPosition(axis, positionName, FromAxisDisplay(v, axis)));
            item.UnitGetter = () => DisplayUnitFor(axis, displayUnit);

            item.Key = display;                     // 이동/티칭 조회는 전체 이름(positionItems 키)으로 매칭
            item.GroupKey = groupKey;
            item.Description = description ?? string.Empty;
            items.Add(item);
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

            // 2열 열우선 배치 ("REAR PICKER" 접두사 생략): [1열] CDA TANK + P1·P2, [2열] VACUUM TANK + P3·P4
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
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Move To Position", null, async delegate
            {
                ParameterGridItem selected = optionParameterGrid.SelectedItem;
                await MoveSelectedPositionAsync(selected != null ? selected.Key : string.Empty);
            });
            menu.Items.Add("Teach Current Position", null, delegate
            {
                ParameterGridItem selected = optionParameterGrid.SelectedItem;
                TeachSelectedPosition(selected != null ? selected.Key : string.Empty);
            });
            ManualMoveGuard.ConfigureTeachingPositionContextMenu(menu);
            menu.Opening += delegate(object sender, System.ComponentModel.CancelEventArgs e)
            {
                ParameterGridItem selected = optionParameterGrid.SelectedItem;
                e.Cancel = selected == null || !positionItems.ContainsKey(selected.Key);
            };
            optionParameterGrid.ContextMenuStrip = menu;
        }

        private async Task MoveSelectedPositionAsync(string key)
        {
            if (unit == null || string.IsNullOrWhiteSpace(key) || !positionItems.ContainsKey(key))
                return;

            PositionItem item = positionItems[key];
            await ConfirmMoveToPositionAsync(item.DisplayName, delegate { return MovePickerTeachingPositionAsync(item.Axis, item.PositionName); }, item.Axis);
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

        // ===================== 인터락 시퀀스 (RearPicker — FrontPicker와 동일 규칙) =====================
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
            QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Error, "REAR-PICKER", "UI", title + " 시퀀스 중단: " + message);
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

        private void TeachSelectedPosition(string key)
        {
            if (unit == null || string.IsNullOrWhiteSpace(key) || !positionItems.ContainsKey(key))
                return;

            PositionItem item = positionItems[key];
            unit.TeachPickerAxisPosition(item.Axis, item.PositionName);
            SaveCurrentRecipeData();
            RefreshView();
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

            if (ManualMoveGuard.BlockIfNotReady(this, "Rear Picker"))
                return;

            // 이동 대상 축의 HOME END(IsHomeDone) 미완료면 차단 — 우클릭 move / 액션 버튼 공통.
            if (!EnsureTargetAxesHomeDone(actionName, targetAxes))
                return;

            if (selectMoveSpeed)
            {
                JogSpeedType speedType;
                if (!ManualMoveGuard.ConfirmMoveSpeed(this, "Rear Picker", actionName, out speedType))
                    return;

                jogAxisMoveControl.SetSelectedSpeedType(speedType);
            }
            else
            {
                DialogResult result = QMC.Common.MessageDialog.Show(this, actionName + " move?", "Rear Picker", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    return;
            }

            await RunSafeAsync(move, actionName);
        }

        // 이동 대상 축의 HOME END(IsHomeDone) 확인. targetAxes 미지정(액션 버튼)이면 RearPicker 전 축을 본다.
        private bool EnsureTargetAxesHomeDone(string actionName, PickerAxis[] targetAxes)
        {
            PickerAxis[] axes = (targetAxes != null && targetAxes.Length > 0) ? targetAxes : AllPickerAxesForHomeCheck;
            foreach (PickerAxis ax in axes)
            {
                if (!unit.IsPickerAxisHomeDone(ax))
                {
                    string axisName = "Rear" + ax.ToString().Replace("Picker", "");
                    string msg = actionName + " 불가: " + axisName + " 축 HOME END(원점복귀)가 완료되지 않았습니다.";
                    EventLogger.Write(EventKind.Alarm, "UI", "REAR-PICKER", msg);
                    QMC.Common.MessageDialog.Show(this, msg, "Rear Picker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("RearPickerRecipePage." + actionName))
                {
                    result = await action();
                }
                EventLogger.Write(EventKind.Event, "UI", "REAR-PICKER", actionName + " result=" + result);
                if (result != 0)
                {
                    string detail = string.IsNullOrEmpty(lastAbortReason)
                        ? " failed. result=" + result
                        : " 실패" + Environment.NewLine + "사유 : " + lastAbortReason;
                    QMC.Common.MessageDialog.Show(this, actionName + detail, "Rear Picker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                EventLogger.Write(EventKind.Alarm, "UI", "REAR-PICKER", "Parameter save failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Rear Picker Parameter Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveCurrentRecipeData()
        {
            Form1 host = FindHostForm();
            if (host == null || string.IsNullOrWhiteSpace(host.CurrentRecipeName))
                return;

            host.SaveMachineRecipe(host.CurrentRecipeName);
        }

        private void SaveCurrentSettingsData()
        {
            Form1 host = FindHostForm();
            if (host != null)
                host.SaveMachineSettings();
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
                "REAR PICKER" + Environment.NewLine +
                "X : " + AxisText(GetAxis(PickerAxis.PickerX), false) + Environment.NewLine +
                "Y : " + AxisText(GetAxis(PickerAxis.PickerY), false) + Environment.NewLine +
                "T0: " + AxisText(GetAxis(PickerAxis.PickerT0), true) + Environment.NewLine +
                "Z0: " + AxisText(GetAxis(PickerAxis.PickerZ0), false) + Environment.NewLine +
                "CDA: " + OnOff(unit.IsPickerCdaPressureOk()) + Environment.NewLine +
                "VAC: " + OnOff(unit.IsPickerVacuumPressureOk());

            lblVisionInfo.Text = "BOTTOM VISION" + Environment.NewLine + body;
            lblVisionInfo2.Text = "SIDE VISION 1" + Environment.NewLine + body;
            lblVisionInfo3.Text = "SIDE VISION 2" + Environment.NewLine + body;
        }

        private void SetPosition(PickerAxis axis, string positionName, double value)
        {
            PickerAxisPositionSet set = GetPositionSet(axis);
            if (set == null || string.IsNullOrWhiteSpace(positionName))
                return;

            if (positionName == "InputAvoidPosition") set.InputAvoidPosition = value;
            else if (positionName == "OutputAvoidPosition") set.OutputAvoidPosition = value;
            else if (positionName == "AvoidPosition") set.AvoidPosition = value;
            else if (positionName == "PickPosition") set.PickPosition = value;
            else if (positionName == "BottomPosition") set.BottomPosition = value;
            else if (positionName == "SidePosition") set.SidePosition = value;
            else if (positionName == "PlacePosition") set.PlacePosition = value;
            else if (positionName.StartsWith("DiePickPosition", StringComparison.OrdinalIgnoreCase)) set.DiePickPosition = SetIndexed(set.DiePickPosition, ExtractIndex(positionName), value);
            else if (positionName.StartsWith("DieBottomPosition", StringComparison.OrdinalIgnoreCase)) set.DieBottomPosition = SetIndexed(set.DieBottomPosition, ExtractIndex(positionName), value);
            else if (positionName.StartsWith("DieSidePosition", StringComparison.OrdinalIgnoreCase)) set.DieSidePosition = SetIndexed(set.DieSidePosition, ExtractIndex(positionName), value);
            else if (positionName.StartsWith("DiePlacePosition", StringComparison.OrdinalIgnoreCase)) set.DiePlacePosition = SetIndexed(set.DiePlacePosition, ExtractIndex(positionName), value);
        }

        private PickerAxisPositionSet GetPositionSet(PickerAxis axis)
        {
            if (axis == PickerAxis.PickerX) return unit.Recipe.PickerX;
            if (axis == PickerAxis.PickerY) return unit.Recipe.PickerY;
            if (axis == PickerAxis.PickerT0) return unit.Recipe.PickerT0;
            if (axis == PickerAxis.PickerT1) return unit.Recipe.PickerT1;
            if (axis == PickerAxis.PickerT2) return unit.Recipe.PickerT2;
            if (axis == PickerAxis.PickerT3) return unit.Recipe.PickerT3;
            if (axis == PickerAxis.PickerZ1) return unit.Recipe.PickerZ1;
            if (axis == PickerAxis.PickerZ2) return unit.Recipe.PickerZ2;
            if (axis == PickerAxis.PickerZ3) return unit.Recipe.PickerZ3;
            return unit.Recipe.PickerZ0;
        }

        private BaseAxis GetAxis(PickerAxis axis)
        {
            BaseAxis item;
            return unit != null && unit.Axes.TryGetValue(axis, out item) ? item : null;
        }

        private static double[] SetIndexed(double[] values, int index, double value)
        {
            if (index < 0)
                return values;
            if (values == null)
                values = new double[index + 1];
            if (values.Length <= index)
            {
                double[] next = new double[index + 1];
                Array.Copy(values, next, values.Length);
                values = next;
            }
            values[index] = value;
            return values;
        }

        private static int ExtractIndex(string name)
        {
            int start = name.IndexOf('[');
            int end = name.IndexOf(']');
            if (start < 0 || end <= start)
                return -1;
            int index;
            return int.TryParse(name.Substring(start + 1, end - start - 1), out index) ? index : -1;
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
