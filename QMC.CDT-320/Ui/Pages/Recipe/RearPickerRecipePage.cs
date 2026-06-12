using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;
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
            refreshTimer.Tick += delegate { RefreshView(); };
            optionParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
            waitParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
            optionParameterGrid.ParameterRowDoubleClicked += OptionParameterGrid_RowDoubleClicked;
            BindParameterGridMenus();
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
            refreshTimer.Start();
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
            manualPanel.Enabled = enabled;
        }

        private void EnsureData()
        {
            if (unit == null)
                return;

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
            string[] tzUnits = { "deg", "um", "deg", "um", "deg", "um", "deg", "um" };

            PickerAxis[] dieKeys = { PickerAxis.PickerX, PickerAxis.PickerY };
            string[] dieNames = { "PICKER X", "PICKER Y" };

            // AVOID — 전체 축의 avoid 위치
            optionItems.Add(ParameterGridItem.Header("AVOID POSITION", "K_AVOID"));
            AddPositionItem(optionItems, PickerAxis.PickerX, "PICKER X", "INPUT AVOID POSITION", "InputAvoidPosition", "um", "PICKER X INPUT", "K_AVOID");
            AddPositionItem(optionItems, PickerAxis.PickerX, "PICKER X", "OUTPUT AVOID POSITION", "OutputAvoidPosition", "um", "PICKER X OUTPUT", "K_AVOID");
            AddPositionItem(optionItems, PickerAxis.PickerX, "PICKER X", "AVOID POSITION", "AvoidPosition", "um", "PICKER X", "K_AVOID");
            AddPositionItem(optionItems, PickerAxis.PickerY, "PICKER Y", "AVOID POSITION", "AvoidPosition", "um", "PICKER Y", "K_AVOID");
            for (int i = 0; i < tzKeys.Length; i++)
                AddPositionItem(optionItems, tzKeys[i], tzNames[i], "AVOID POSITION", "AvoidPosition", tzUnits[i], tzNames[i], "K_AVOID");

            // T/Z 모션 종류 — PICK / BOTTOM / SIDE / PLACE
            string[] tzKinds = { "PICK", "BOTTOM", "SIDE", "PLACE" };
            string[] tzKindPos = { "PickPosition", "BottomPosition", "SidePosition", "PlacePosition" };
            for (int k = 0; k < tzKinds.Length; k++)
            {
                string gk = "K_" + tzKinds[k];
                optionItems.Add(ParameterGridItem.Header(tzKinds[k] + " POSITION", gk));
                for (int i = 0; i < tzKeys.Length; i++)
                    AddPositionItem(optionItems, tzKeys[i], tzNames[i], tzKinds[k] + " POSITION", tzKindPos[k], tzUnits[i], tzNames[i], gk);
            }

            // X/Y 다이 어레이 종류 — DIE PICK / DIE BOTTOM / DIE SIDE / DIE PLACE (P1~4)
            string[] dieKinds = { "DIE PICK", "DIE BOTTOM", "DIE SIDE", "DIE PLACE" };
            string[] dieKindPos = { "DiePickPosition", "DieBottomPosition", "DieSidePosition", "DiePlacePosition" };
            for (int k = 0; k < dieKinds.Length; k++)
            {
                string gk = "K_" + dieKindPos[k];
                optionItems.Add(ParameterGridItem.Header(dieKinds[k] + " POSITION", gk));
                for (int a = 0; a < dieKeys.Length; a++)
                    for (int i = 0; i < 4; i++)
                        AddPositionItem(optionItems, dieKeys[a], dieNames[a], "P" + (i + 1) + " " + dieKinds[k] + " POSITION", dieKindPos[k] + "[" + i + "]", "um", dieNames[a] + " P" + (i + 1), gk);
            }

            // ===== CONFIG (그룹 없이 펼친 상태로 표시) =====
            optionItems.Add(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v));
            AddPickerConfigItems(optionItems);

            // ===== SETUP (그룹 없이 펼친 상태로 표시) =====
            optionItems.Add(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v));
            optionItems.Add(ParameterGridItem.Double("INPUT SAFETY OFFSET", "um", ParameterGridScope.Setup, () => unit.Setup.InputSafetyOffset * 1000.0, v => unit.Setup.InputSafetyOffset = v / 1000.0));
            optionItems.Add(ParameterGridItem.Double("OUTPUT SAFETY OFFSET", "um", ParameterGridScope.Setup, () => unit.Setup.OutputSafetyOffset * 1000.0, v => unit.Setup.OutputSafetyOffset = v / 1000.0));

            optionParameterGrid.SetItems(optionItems);

            waitParameterGrid.SetItems(new[]
            {
                ParameterGridItem.Int("MOVE TIMEOUT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.MoveTimeoutMs, v => unit.Recipe.MoveTimeoutMs = Math.Max(0, v)),
                ParameterGridItem.Int("I/O TIMEOUT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.IoTimeoutMs, v => unit.Recipe.IoTimeoutMs = Math.Max(0, v)),
                ParameterGridItem.Int("BLOW TIME", "ms", ParameterGridScope.Recipe, () => unit.Recipe.BlowTimeMs, v => unit.Recipe.BlowTimeMs = Math.Max(0, v)),
                ParameterGridItem.Int("VACUUM SETTLE", "ms", ParameterGridScope.Recipe, () => unit.Recipe.VacuumSettleMs, v => unit.Recipe.VacuumSettleMs = Math.Max(0, v)),
                ParameterGridItem.Int("PICK LIFT WAIT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.PickLiftWaitMs, v => unit.Recipe.PickLiftWaitMs = Math.Max(0, v)),
                ParameterGridItem.Int("PLACE DELAY", "ms", ParameterGridScope.Recipe, () => unit.Recipe.PlaceDelayMs, v => unit.Recipe.PlaceDelayMs = Math.Max(0, v)),
                ParameterGridItem.Double("ARM X VELOCITY", "um/s", ParameterGridScope.Recipe, () => unit.Recipe.ArmXVelocity * 1000.0, v => unit.Recipe.ArmXVelocity = v / 1000.0),
                ParameterGridItem.Double("ARM Y VELOCITY", "um/s", ParameterGridScope.Recipe, () => unit.Recipe.ArmYVelocity * 1000.0, v => unit.Recipe.ArmYVelocity = v / 1000.0),
                ParameterGridItem.Double("PICKER Z VELOCITY", "um/s", ParameterGridScope.Recipe, () => unit.Recipe.PickerZVelocity * 1000.0, v => unit.Recipe.PickerZVelocity = v / 1000.0),
                ParameterGridItem.Double("PICKER T VELOCITY", "deg/s", ParameterGridScope.Recipe, () => unit.Recipe.PickerTVelocity, v => unit.Recipe.PickerTVelocity = v),
                ParameterGridItem.Double("PICK LIFT POSITION", "um", ParameterGridScope.Recipe, () => unit.Recipe.PickLiftPosition * 1000.0, v => unit.Recipe.PickLiftPosition = v / 1000.0)
            });
        }

        private void AddPickerConfigItems(List<ParameterGridItem> items)
        {
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                string name = "PICKER " + (index + 1);
                items.Add(ParameterGridItem.Bool(name + " USE", ParameterGridScope.Config, () => unit.Config.UsePicker[index], v => unit.Config.UsePicker[index] = v));
                items.Add(ParameterGridItem.Double(name + " OFFSET X", "um", ParameterGridScope.Config, () => unit.Config.Picker[index].AlignOffsetX * 1000.0, v => unit.Config.Picker[index].AlignOffsetX = v / 1000.0));
                items.Add(ParameterGridItem.Double(name + " OFFSET Y", "um", ParameterGridScope.Config, () => unit.Config.Picker[index].AlignOffsetY * 1000.0, v => unit.Config.Picker[index].AlignOffsetY = v / 1000.0));
                items.Add(ParameterGridItem.Double(name + " OFFSET T", "deg", ParameterGridScope.Config, () => unit.Config.Picker[index].AlignOffsetT, v => unit.Config.Picker[index].AlignOffsetT = v));
            }
        }

        private void AddPositionItem(List<ParameterGridItem> items, PickerAxis axis, string axisName, string displaySuffix, string positionName, string displayUnit, string memberDisplay, string groupKey)
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

            ParameterGridItem item;
            if (displayUnit == "deg")
                item = ParameterGridItem.Double(memberDisplay, "deg", ParameterGridScope.Recipe, () => unit.GetPickerTeachingPosition(axis, positionName), v => SetPosition(axis, positionName, v));
            else
                item = ParameterGridItem.Double(memberDisplay, "um", ParameterGridScope.Recipe, () => unit.GetPickerTeachingPosition(axis, positionName) * 1000.0, v => SetPosition(axis, positionName, v / 1000.0));

            item.Key = display;                     // 이동/티칭 조회는 전체 이름(positionItems 키)으로 매칭
            item.GroupKey = groupKey;
            items.Add(item);
        }

        private void BindIoPanel()
        {
            if (unit == null)
                return;

            List<IoCylinderItem> items = new List<IoCylinderItem>();

            // 공용 압력 체크
            items.Add(IoCylinderItem.Input("REAR PICKER CDA TANK PRESSURE CHECK", () => unit.IsPickerCdaPressureOk()));
            items.Add(IoCylinderItem.Input("REAR PICKER VACUUM TANK PRESSURE CHECK", () => unit.IsPickerVacuumPressureOk()));

            // 피커 1~4: FLOW / VACUUM / BLOW 를 한 세트로 묶어 정렬 (5~8 미사용)
            for (int i = 1; i <= 4; i++)
            {
                int pickerNo = i;
                items.Add(IoCylinderItem.Input("REAR PICKER " + pickerNo + " FLOW CHECK", () => unit.IsPickerFlowDetected(pickerNo)));
                items.Add(IoCylinderItem.Output("REAR PICKER " + pickerNo + " VACUUM", () => OutputOn(unit.Vacuums, pickerNo), on => { unit.SetPickerVacuum(pickerNo, on); return Task.FromResult(0); }, "ON", "OFF"));
                items.Add(IoCylinderItem.Output("REAR PICKER " + pickerNo + " BLOW", () => OutputOn(unit.Blows, pickerNo), on => { unit.SetPickerBlow(pickerNo, on); return Task.FromResult(0); }, "ON", "OFF"));
            }

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
            jogPositionListControl.SetItems(items);
        }

        private void AddJogItem(List<JogAxisItem> items, string name, PickerAxis axisKey, string plus, string minus, JogAxisControlKind kind)
        {
            BaseAxis axis = GetAxis(axisKey);
            if (axis == null)
                return;

            bool theta = IsTheta(axisKey);
            JogAxisItem item = JogAxisItem.Single(name, axis, theta ? "deg" : "um", theta ? 1.0 : 1000.0, plus, minus).WithControlKind(kind);
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
            optionParameterGrid.ContextMenuStrip = menu;
        }

        private async Task MoveSelectedPositionAsync(string key)
        {
            if (unit == null || string.IsNullOrWhiteSpace(key) || !positionItems.ContainsKey(key))
                return;

            PositionItem item = positionItems[key];
            await ConfirmMoveAsync(item.DisplayName, delegate { return unit.MovePickerAxisToTeachingPosition(item.Axis, item.PositionName, IsFineMove()); });
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
        private async Task<int> MoveMembersAsync(List<PositionItem> moves, bool fine)
        {
            foreach (PositionItem m in moves)
            {
                int r = await unit.MovePickerAxisToTeachingPosition(m.Axis, m.PositionName, fine);
                if (r != 0)
                    return r;
            }
            return 0;
        }

        private int AbortSeq(string title, string message)
        {
            // 상세 사유는 로그(EventLogger Alarm)에 기록하고, 팝업은 래퍼의 실패 팝업 하나로 합쳐 표시한다.
            EventLogger.Write(EventKind.Alarm, "UI", "REAR-PICKER", title + " 시퀀스 중단: " + message);
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

        // I4: 헤드(Z/T) 하강 전, 갠트리 X/Y가 짝 DIE 위치(P1~4 중 동일 인덱스)에 정렬됐는지
        private bool CheckGantryAlignedForKind(string kind, out string reason)
        {
            reason = string.Empty;
            string baseName;
            switch (kind)
            {
                case "PICK": baseName = "DiePickPosition"; break;
                case "BOTTOM": baseName = "DieBottomPosition"; break;
                case "SIDE": baseName = "DieSidePosition"; break;
                case "PLACE": baseName = "DiePlacePosition"; break;
                default: reason = "알 수 없는 종류(" + kind + ")"; return false;
            }
            for (int i = 0; i < 4; i++)
            {
                string pos = baseName + "[" + i + "]";
                if (unit.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, pos) &&
                    unit.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, pos))
                    return true;
            }
            reason = "갠트리(X/Y)가 DIE " + kind + " 위치에 정렬되지 않음";
            return false;
        }

        // ① AVOID — T복귀 → Z상승 → Y → X(일반 Avoid)
        private async Task<int> MoveAvoidSequenceAsync()
        {
            const string title = "AVOID";
            if (unit == null)
                return -1;

            bool fine = IsFineMove();
            string reason;

            if (RequireHomingForAvoid && !CheckPickerHomedForAvoid(out reason))
                return AbortSeq(title, reason);

            int r = await MoveMembersAsync(GroupMembersByAxes("K_AVOID", PickerTAxes), fine);
            if (r != 0) return AbortSeq(title, "T 복귀 실패 (CDA/알람 확인)");

            r = await MoveMembersAsync(GroupMembersByAxes("K_AVOID", PickerZAxes), fine);
            if (r != 0) return AbortSeq(title, "Z 상승 실패 (CDA/알람 확인)");

            string zblock = unit.GetPickerZClearBlockReason();
            if (zblock != null) return AbortSeq(title, "Y/X 전 Z 상승 미완료: " + zblock);

            r = await MoveMembersAsync(GroupMembersByAxes("K_AVOID", PickerAxis.PickerY), fine);
            if (r != 0) return AbortSeq(title, "Y 회피 실패");

            // AVOID의 X는 일반 AvoidPosition만 (Input/Output Avoid 제외)
            List<PositionItem> xMoves = GroupMembersByAxes("K_AVOID", PickerAxis.PickerX)
                .FindAll(m => string.Equals(m.PositionName, "AvoidPosition", StringComparison.OrdinalIgnoreCase));
            r = await MoveMembersAsync(xMoves, fine);
            if (r != 0) return AbortSeq(title, "X 회피 실패 (공유레일 확인)");

            return 0;
        }

        // ② DIE PICK/BOTTOM/SIDE/PLACE — Z 전부 상승 확인 후 Y → X
        private async Task<int> MoveDieKindSequenceAsync(string title, string kindPos)
        {
            if (unit == null)
                return -1;

            bool fine = IsFineMove();
            string groupKey = "K_" + kindPos;

            string zblock = unit.GetPickerZClearBlockReason();
            if (zblock != null) return AbortSeq(title, "X/Y 이동 전 Z 상승 미완료: " + zblock);

            int r = await MoveMembersAsync(GroupMembersByAxes(groupKey, PickerAxis.PickerY), fine);
            if (r != 0) return AbortSeq(title, "Y 이동 실패 (CDA/공유레일/알람 확인)");

            r = await MoveMembersAsync(GroupMembersByAxes(groupKey, PickerAxis.PickerX), fine);
            if (r != 0) return AbortSeq(title, "X 이동 실패 (CDA/공유레일/알람 확인)");

            return 0;
        }

        // ③ PICK/BOTTOM/SIDE/PLACE — 갠트리 정렬 확인 후 Z 하강 → T 회전
        private async Task<int> MoveHeadKindSequenceAsync(string kind)
        {
            if (unit == null)
                return -1;

            bool fine = IsFineMove();
            string groupKey = "K_" + kind;
            string reason;

            if (!CheckGantryAlignedForKind(kind, out reason))
                return AbortSeq(kind, "Z 하강 전 " + reason);

            int r = await MoveMembersAsync(GroupMembersByAxes(groupKey, PickerZAxes), fine);
            if (r != 0) return AbortSeq(kind, "Z 하강 실패 (CDA/알람 확인)");

            r = await MoveMembersAsync(GroupMembersByAxes(groupKey, PickerTAxes), fine);
            if (r != 0) return AbortSeq(kind, "T 회전 실패 (CDA/알람 확인)");

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

        private async Task ConfirmMoveAsync(string actionName, Func<Task<int>> move)
        {
            if (unit == null || move == null)
                return;

            if (ManualMoveGuard.BlockIfNotReady(this, "Rear Picker"))
                return;

            DialogResult result = QMC.Common.MessageDialog.Show(this, actionName + " move?", "Rear Picker", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;

            await RunSafeAsync(move, actionName);
        }

        private async Task RunSafeAsync(Func<Task<int>> action, string actionName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                lastAbortReason = null;
                int result = await action();
                EventLogger.Write(EventKind.Event, "UI", "REAR-PICKER", actionName + " result=" + result);
                if (result != 0)
                {
                    string detail = string.IsNullOrEmpty(lastAbortReason)
                        ? " failed. result=" + result
                        : " 실패" + Environment.NewLine + "사유: " + lastAbortReason;
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
            return theta ? axis.ActualPosition.ToString("0.###") + " deg" : (axis.ActualPosition * 1000.0).ToString("0.###") + " um";
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

        private bool IsFineMove()
        {
            return true;
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
