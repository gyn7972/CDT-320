using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT_320.Ui.Localization;
using QMC.Common.Data.Store;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>
    /// Stage 59 — 시퀀스 위치 티칭 페이지.
    /// 각 Unit 의 핵심 시퀀스 위치(절대 좌표)를 그리드에서 편집·티칭·저장.
    /// 메뉴얼(CDT-310/CDT-300) + 코드의 Setup 클래스 기준으로 항목을 시드한다.
    /// </summary>
    public partial class PositionTeachingPage : PageBase
    {
        // ── 위치 항목 메타 ─────────────────────────────────────────────
        public class TeachItem
        {
            public string Group { get; set; }   // 모듈 명
            public string Key   { get; set; }   // 위치 ID
            public string Name  { get; set; }   // 표시 명
            public string Axis  { get; set; }   // 어느 축인지 (#No NAME)
            public double Value { get; set; }   // 현재 값 [mm 또는 deg]
            public string Unit  { get; set; }   // mm, deg
            public string Desc  { get; set; }   // 한 줄 설명
        }

        private List<TeachItem> _items;

        private System.Windows.Forms.Timer _jogPosTimer;
        private QMC.Common.Motion.BaseAxis _jogCurrentAxis;

        // 영속화 파일
        private static readonly string SavePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "teach_positions.json");

        public PositionTeachingPage()
        {
            InitializeComponent();
            WireRuntimeEvents();

            _items = LoadOrSeed();
            FillGrid();
        }

        private void WireRuntimeEvents()
        {
            _grid.CellEndEdit += OnCellEdit;
            _grid.SelectionChanged += (s, e) => OnGridSelectionChanged();

            btnStepMul10.Click += (s, e) => MultiplyStep(10.0);
            btnStepDiv10.Click += (s, e) => MultiplyStep(0.1);
            btnStep5.Click += (s, e) => { _jogStepBox.Text = GetJogStepPreset(0); };
            btnStep1.Click += (s, e) => { _jogStepBox.Text = GetJogStepPreset(1); };
            btnStep01.Click += (s, e) => { _jogStepBox.Text = GetJogStepPreset(2); };
            btnStep001.Click += (s, e) => { _jogStepBox.Text = GetJogStepPreset(3); };
            btnStep0001.Click += (s, e) => { _jogStepBox.Text = GetJogStepPreset(4); };

            btnTeach.Click += (s, e) => TeachFromCurrentPos();
            btnGoto.Click += (s, e) => MoveToTaught();
            btnApply.Click += (s, e) => ApplyToSetup();
            btnSave.Click += (s, e) => DoSave();
            btnReload.Click += (s, e) => { _items = LoadOrSeed(); FillGrid(); };
            btnReset.Click += (s, e) =>
            {
                if (QMC.Common.MessageDialog.Show("기본값으로 초기화하시겠습니까?", "Reset",
                                     MessageBoxButtons.OKCancel) != DialogResult.OK) return;
                _items = SeedDefault();
                FillGrid();
            };

            _jogPosTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _jogPosTimer.Tick += (s, e) =>
            {
                if (!ShouldRefreshVisible(this))
                {
                    _jogPosTimer.Stop();
                    return;
                }

                RefreshJogPos();
            };
            if (ShouldRefreshVisible(this))
                _jogPosTimer.Start();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            try
            {
                if (_jogPosTimer == null)
                    return;

                if (ShouldRefreshVisible(this))
                    _jogPosTimer.Start();
                else
                    _jogPosTimer.Stop();
            }
            catch { }
        }

        private void MultiplyStep(double factor)
        {
            if (!TryParseDisplayDouble(_jogStepBox.Text, out double v) || v <= 0) v = 1.0;
            v *= factor;
            // 자릿수 자동 (소수점 너무 길지 않게)
            _jogStepBox.Text = v.ToString("0.######");
        }

        private void OnGridSelectionChanged()
        {
            var it = CurrentItem();
            if (it == null) { _jogCurrentAxis = null; _jogAxisLabel.Text = "Axis: (없음)"; ClearJogButtons(); UpdateJogUnitUi(); return; }
            var host = FindForm() as Form1;
            if (host?.Machine == null) return;
            _jogCurrentAxis = ResolveAxis(host.Machine, it.Axis);
            _jogAxisLabel.Text = "Axis: " + it.Axis;
            UpdateJogUnitUi();
            RebuildJogButtons(DetectAxisDir(it.Axis));
        }

        private void UpdateJogUnitUi()
        {
            try
            {
                string unit = _jogCurrentAxis == null ? AxisUnitConverter.Millimeter : AxisUnitConverter.DisplayUnitFor(_jogCurrentAxis);
                lblSpeedUnit.Text = unit + "/s";
                lblStep.Text = "Step (" + unit + ")";
                btnStep5.Text = GetJogStepPreset(0);
                btnStep1.Text = GetJogStepPreset(1);
                btnStep01.Text = GetJogStepPreset(2);
                btnStep001.Text = GetJogStepPreset(3);
                btnStep0001.Text = GetJogStepPreset(4);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private string GetJogStepPreset(int index)
        {
            try
            {
                string unit = _jogCurrentAxis == null ? AxisUnitConverter.Millimeter : AxisUnitConverter.DisplayUnitFor(_jogCurrentAxis);
                double[] presets = AxisUnitConverter.Normalize(unit) == AxisUnitConverter.Micrometer
                    ? new[] { 5000.0, 1000.0, 100.0, 10.0, 1.0 }
                    : new[] { 5.0, 1.0, 0.1, 0.01, 0.001 };
                double value = presets[Math.Max(0, Math.Min(index, presets.Length - 1))];
                return value.ToString("0.###");
            }
            catch
            {
                return "1";
            }
            finally
            {
            }
        }

        private void ClearJogButtons()
        {
            if (_jogButtonArea == null) return;
            _jogButtonArea.Controls.Clear();
        }

        /// <summary>axis 라벨 (예: "#12 FrontPickerZ0", "#03 WaferStageT") 에서 마지막 방향 문자 추출.</summary>
        private static char DetectAxisDir(string axisLabel)
        {
            if (string.IsNullOrEmpty(axisLabel)) return '?';
            // "/" 가 있으면 첫 번째만 사용
            int slash = axisLabel.IndexOf('/');
            string s = (slash > 0 ? axisLabel.Substring(0, slash) : axisLabel).Trim().ToUpperInvariant();
            int us = s.LastIndexOf('_');
            if (us < 0 || us + 1 >= s.Length) return '?';
            return s[us + 1];   // 'X', 'Y', 'Z', 'T'
        }

        private void RebuildJogButtons(char dir)
        {
            ClearJogButtons();
            var area = _jogButtonArea;
            var font = new Font("맑은 고딕", 14F, FontStyle.Bold);

            Button MkBtn(string text, Point loc, Size sz, int sign)
            {
                var b = new Button
                {
                    Text = text, Location = loc, Size = sz, Font = font,
                    BackColor = Color.FromArgb(0xE2, 0xEB, 0xF8),
                    FlatStyle = FlatStyle.Flat
                };
                b.Click += async (s, e) => await DoJogAsync(sign);
                return b;
            }

            // _jogButtonArea = 274×260 (inner)
            switch (dir)
            {
                // X축 조그 버튼 배치
                case 'X':
                    // 좌우 배치: [− ◀]   [▶ +]
                    area.Controls.Add(MkBtn("◀  −X",  new Point(12,  90), new Size(120, 80), -1));
                    area.Controls.Add(MkBtn("+X  ▶",  new Point(140, 90), new Size(120, 80), +1));
                    break;
                // Y축 조그 버튼 배치
                case 'Y':
                    area.Controls.Add(MkBtn("▲  +Y",  new Point(76, 20),  new Size(120, 80), +1));
                    area.Controls.Add(MkBtn("−Y  ▼",  new Point(76, 160), new Size(120, 80), -1));
                    break;
                // Z축 조그 버튼 배치
                case 'Z':
                    area.Controls.Add(MkBtn("▲  +Z",  new Point(76, 20),  new Size(120, 80), +1));
                    area.Controls.Add(MkBtn("−Z  ▼",  new Point(76, 160), new Size(120, 80), -1));
                    break;
                // T축 조그 버튼 배치
                case 'T':
                    area.Controls.Add(MkBtn("↺ CCW",  new Point(12,  90), new Size(120, 80), -1));
                    area.Controls.Add(MkBtn("CW ↻",   new Point(140, 90), new Size(120, 80), +1));
                    break;
                default:
                    area.Controls.Add(new Label
                    {
                        Location = new Point(8, 100), Size = new Size(258, 40),
                        Text = "축 방향 식별 실패",
                        TextAlign = ContentAlignment.MiddleCenter,
                        ForeColor = Color.IndianRed,
                        Font = new Font("맑은 고딕", 9.5F)
                    });
                    break;
            }
        }

        private async System.Threading.Tasks.Task DoJogAsync(int sign)
        {
            if (_jogCurrentAxis == null) return;
            if (!TryParseDisplayDouble(_jogSpeedBox.Text, out double speed) || speed <= 0) speed = 100;
            if (!TryParseDisplayDouble(_jogStepBox.Text, out double step) || step <= 0) step = 1.0;
            try
            {
                if (!_jogCurrentAxis.IsServoOn) _jogCurrentAxis.ServoOn();
                double nativeStep = AxisUnitConverter.FromDisplay(step, _jogCurrentAxis);
                double nativeSpeed = AxisUnitConverter.FromDisplay(speed, _jogCurrentAxis);
                await _jogCurrentAxis.MoveRelativeAsync(sign * nativeStep, nativeSpeed);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("Jog 실패: " + ex.Message, "Jog",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RefreshJogPos()
        {
            if (_jogPosLabel == null) return;
            UpdateJogUnitUi();
            _jogPosLabel.Text = _jogCurrentAxis == null
                ? "Actual Pos: -"
                : "Actual Pos: " + AxisUnitConverter.FormatDisplay(_jogCurrentAxis.ActualPosition, _jogCurrentAxis, "0.###", true);
        }

        // ──────────────────────────────────────
        //  데이터 시드 (메뉴얼/코드 Setup 기반)
        // ──────────────────────────────────────
        public static List<TeachItem> SeedDefault()
        {
            var L = new List<TeachItem>();

            // ── InputLoader ────────────────────────────────────────────
            L.Add(new TeachItem { Group="InputLoader", Key="FirstSlotPosition",   Name="첫 슬롯 Z 위치",        Axis="#00 InputLifterZ", Value=10.0,  Unit="mm",  Desc= "카세트 슬롯 0번 InputLifterZ 절대 위치" });
            L.Add(new TeachItem { Group="InputLoader", Key="ExchangePositionY",   Name="교환 Y 위치",            Axis="#01 InputFeederY", Value=150.0, Unit="mm",  Desc="피더가 InputStage 입구로 전진하는 Y" });

            // ── InputStage ─────────────────────────────────────────────
            L.Add(new TeachItem { Group="InputStage",  Key="ExpanderDownPosition",Name="ExpanderZ Down",        Axis="#04 WaferExpandingZ", Value=50.0, Unit="mm", Desc="테이프 텐션 확보 위치" });
            L.Add(new TeachItem { Group="InputStage",  Key="ExpanderUpPosition",  Name="ExpanderZ Up",          Axis="#04 WaferExpandingZ", Value=0.0,  Unit="mm", Desc="언로드 위치 (텐션 해제)" });
            L.Add(new TeachItem { Group="InputStage",  Key="UnloadPositionY",     Name="Unload StageY",         Axis="#02 WaferStageY",     Value=0.0,  Unit="mm", Desc="언로드 시 StageY 이동 위치" });
            L.Add(new TeachItem { Group="InputStage",  Key="NeedleEjectPosition", Name="Needle Eject Z",        Axis="#07 NeedleZ",          Value=5.0,  Unit="mm", Desc="다이 분리 시 니들 상승 위치" });
            L.Add(new TeachItem { Group="InputStage",  Key="NeedleDownPosition",  Name="Needle Down Z",         Axis="#07 NeedleZ",          Value=0.0,  Unit="mm", Desc="니들 대기 위치" });
            L.Add(new TeachItem { Group="InputStage",  Key="PickerOffsetX",       Name="Picker Offset X",       Axis="#06 NeedleX",          Value=0.0,  Unit="mm", Desc="스캔→픽업 X 보정" });
            L.Add(new TeachItem { Group="InputStage",  Key="PickerOffsetY",       Name="Picker Offset Y",       Axis="#02 WaferStageY",     Value=3.0,  Unit="mm", Desc="스캔→픽업 Y 보정" });

            // ── TransferPicker — Picker Z 위치 (Front 4 + Rear 4 = 8 picker, 각 4 위치 = 32 entries) ──
            // 각 picker (PickerComponent) 마다 Pick/Place/Focus/Wait Z 위치를 별도 티칭.
            //   Front Picker 0~3 → axis #12, #14, #16, #18
            //   Rear  Picker 0~3 → axis #24, #26, #28, #30
            string[] frontZAxes = new[] { "#12 FrontPickerZ0", "#14 FrontPickerZ1",
                                          "#16 FrontPickerZ2", "#18 FrontPickerZ3" };
            string[] rearZAxes  = new[] { "#24 RearPickerZ0",  "#26 RearPickerZ1",
                                          "#28 RearPickerZ2",  "#30 RearPickerZ3" };
            string[] zKinds     = new[] { "PickPosition", "PlacePosition", "FocusPosition", "WaitPosition" };
            string[] zKindNames = new[] { "Pick Z",       "Place Z",       "Focus Z",       "Wait Z" };
            double[] zDefaults  = new[] {  42.0,            42.0,            20.0,           0.0 };
            string[] zDescs     = new[] { "다이 픽업 시 하강 위치", "OutputStage Place 위치",
                                          "비전 촬상 시 위치",       "대기(상승) 위치" };

            for (int i = 0; i < 4; i++)
            {
                for (int k = 0; k < 4; k++)
                {
                    L.Add(new TeachItem {
                        Group = "TPU.FrontP" + i, Key = zKinds[k],
                        Name = "Front P" + i + " " + zKindNames[k],
                        Axis = frontZAxes[i], Value = zDefaults[k], Unit = "mm",
                        Desc = "Front Picker " + i + " — " + zDescs[k]
                    });
                }
            }
            for (int i = 0; i < 4; i++)
            {
                for (int k = 0; k < 4; k++)
                {
                    L.Add(new TeachItem {
                        Group = "TPU.RearP" + i, Key = zKinds[k],
                        Name = "Rear P" + i + " " + zKindNames[k],
                        Axis = rearZAxes[i], Value = zDefaults[k], Unit = "mm",
                        Desc = "Rear Picker " + i + " — " + zDescs[k]
                    });
                }
            }

            L.Add(new TeachItem { Group="TPU.Front",   Key="PickX",              Name="Front Pick X",          Axis="#09 FrontPickerX",    Value=300.0,  Unit="mm", Desc="Recipe.PickerX.PickPosition" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="PickY",              Name="Front Pick Y",          Axis="#10 FrontPickerY",    Value=100.0,  Unit="mm", Desc="Recipe.PickerY.PickPosition" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="BottomX",            Name="Front Bottom X",        Axis="#09 FrontPickerX",    Value=750.0,  Unit="mm", Desc="Recipe.PickerX.BottomPosition" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="BottomY",            Name="Front Bottom Y",        Axis="#10 FrontPickerY",    Value=100.0,  Unit="mm", Desc="Recipe.PickerY.BottomPosition" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="SideX",              Name="Front Side X",          Axis="#09 FrontPickerX",    Value=720.0,  Unit="mm", Desc="Recipe.PickerX.SidePosition" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="SideY",              Name="Front Side Y",          Axis="#10 FrontPickerY",    Value=200.0,  Unit="mm", Desc="Recipe.PickerY.SidePosition" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="PlaceX",             Name="Front Place X",         Axis="#09 FrontPickerX",    Value=1200.0, Unit="mm", Desc="Recipe.PickerX.PlacePosition" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="PlaceY",             Name="Front Place Y",         Axis="#10 FrontPickerY",    Value=100.0,  Unit="mm", Desc="Recipe.PickerY.PlacePosition" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="AvoidY",             Name="Front Avoid Y",         Axis="#10 FrontPickerY",    Value=50.0,   Unit="mm", Desc="Recipe.PickerY.AvoidPosition" });

            L.Add(new TeachItem { Group="TPU.Rear",    Key="PickX",              Name="Rear Pick X",           Axis="#21 RearPickerX",     Value=300.0,  Unit="mm", Desc="Recipe.PickerX.PickPosition" });
            L.Add(new TeachItem { Group="TPU.Rear",    Key="PickY",              Name="Rear Pick Y",           Axis="#22 RearPickerY",     Value=100.0,  Unit="mm", Desc="Recipe.PickerY.PickPosition" });
            L.Add(new TeachItem { Group="TPU.Rear",    Key="BottomX",            Name="Rear Bottom X",         Axis="#21 RearPickerX",     Value=750.0,  Unit="mm", Desc="Recipe.PickerX.BottomPosition" });
            L.Add(new TeachItem { Group="TPU.Rear",    Key="BottomY",            Name="Rear Bottom Y",         Axis="#22 RearPickerY",     Value=100.0,  Unit="mm", Desc="Recipe.PickerY.BottomPosition" });
            L.Add(new TeachItem { Group="TPU.Rear",    Key="SideX",              Name="Rear Side X",           Axis="#21 RearPickerX",     Value=720.0,  Unit="mm", Desc="Recipe.PickerX.SidePosition" });
            L.Add(new TeachItem { Group="TPU.Rear",    Key="SideY",              Name="Rear Side Y",           Axis="#22 RearPickerY",     Value=200.0,  Unit="mm", Desc="Recipe.PickerY.SidePosition" });
            L.Add(new TeachItem { Group="TPU.Rear",    Key="PlaceX",             Name="Rear Place X",          Axis="#21 RearPickerX",     Value=1200.0, Unit="mm", Desc="Recipe.PickerX.PlacePosition" });
            L.Add(new TeachItem { Group="TPU.Rear",    Key="PlaceY",             Name="Rear Place Y",          Axis="#22 RearPickerY",     Value=100.0,  Unit="mm", Desc="Recipe.PickerY.PlacePosition" });
            L.Add(new TeachItem { Group="TPU.Rear",    Key="AvoidY",             Name="Rear Avoid Y",          Axis="#22 RearPickerY",     Value=50.0,   Unit="mm", Desc="Recipe.PickerY.AvoidPosition" });

            // ── OutputStage ────────────────────────────────────────────
            L.Add(new TeachItem { Group="OutputStage", Key="StageBasePositionY",     Name="StageY 기준 Y",       Axis="#31 OutputGoodStageY / #33 OutputNGStageY", Value=200.0, Unit="mm", Desc="Place 시 StageY 기준" });
            L.Add(new TeachItem { Group="OutputStage", Key="WorkPositionZ",          Name="Work Z (상승)",       Axis="#32 OutputGoodStageZ",          Value=80.0,  Unit="mm", Desc="다이 받을 때 StageZ 상승" });
            L.Add(new TeachItem { Group="OutputStage", Key="AvoidPositionZ",         Name="Avoid Z (하강)",      Axis="#32 OutputGoodStageZ",          Value=0.0,   Unit="mm", Desc="반대편 작업 시 회피 위치" });
            L.Add(new TeachItem { Group="OutputStage", Key="BinCameraWorkPositionX", Name="BinCamera Work X",    Axis="#34 OutputVisionX", Value=200.0, Unit="mm", Desc="Bin 검사 위치" });
            L.Add(new TeachItem { Group="OutputStage", Key="BinCameraRetractX",      Name="BinCamera Retract X", Axis="#34 OutputVisionX", Value=0.0,   Unit="mm", Desc="후퇴(대기) 위치" });

            // ── OutputCassette / OutputFeeder ─────────────────────────
            L.Add(new TeachItem { Group="OutputCassette", Key="NgFirstSlotPositionZ",    Name="NG Slot 0 Z",           Axis="#36 OutputLifterZ",  Value=20.0,   Unit="mm", Desc="NG 카세트 첫 슬롯" });
            L.Add(new TeachItem { Group="OutputCassette", Key="Good1FirstSlotPositionZ", Name="Good1 Slot 0 Z",        Axis="#36 OutputLifterZ",  Value=80.0,   Unit="mm", Desc="Good1 카세트 첫 슬롯" });
            L.Add(new TeachItem { Group="OutputCassette", Key="Good2FirstSlotPositionZ", Name="Good2 Slot 0 Z",        Axis="#36 OutputLifterZ",  Value=160.0,  Unit="mm", Desc="Good2 카세트 첫 슬롯" });
            L.Add(new TeachItem { Group="OutputCassette", Key="SlotPitchZ",              Name="슬롯 피치 Z",            Axis="#36 OutputLifterZ",  Value=6.0,    Unit="mm", Desc="슬롯 간 간격" });
            L.Add(new TeachItem { Group="OutputFeeder", Key="NgStageExchangePositionY",  Name="NG 교환 Y",             Axis="#35 OutputFeederY",  Value=180.0,  Unit="mm", Desc="NG 카세트 인계 Y" });
            L.Add(new TeachItem { Group="OutputFeeder", Key="GoodStageExchangePositionY",Name="Good 교환 Y",           Axis="#35 OutputFeederY",  Value=180.0,  Unit="mm", Desc="Good 카세트 인계 Y" });
            L.Add(new TeachItem { Group="OutputFeeder", Key="CassetteInsertPositionY",   Name="카세트 삽입 Y",         Axis="#35 OutputFeederY",  Value=250.0,  Unit="mm", Desc="카세트 안쪽 진입 위치" });

            return L;
        }

        // ──────────────────────────────────────
        //  Persistence (JSON)
        // ──────────────────────────────────────
        [DataContract]
        public class TeachStore
        {
            [DataMember] public List<TeachItem> Items { get; set; } = new List<TeachItem>();
        }

        private static List<TeachItem> LoadOrSeed()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    using (var fs = File.OpenRead(SavePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(TeachStore));
                        var s = (TeachStore)ser.ReadObject(fs);
                        if (s?.Items != null && s.Items.Count > 0)
                            return s.Items;
                    }
                }
            }
            catch { }
            return SeedDefault();
        }

        private void DoSave()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                using (var fs = File.Create(SavePath))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(TeachStore), new TeachStore { Items = _items });
                }
                QMC.Common.MessageDialog.Show("티칭 데이터 저장 완료.\n" + SavePath, "Position Teaching",
                                 MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("저장 실패: " + ex.Message);
            }
        }

        // ──────────────────────────────────────
        //  Grid 채우기 + 편집
        // ──────────────────────────────────────
        private void FillGrid()
        {
            _grid.Rows.Clear();
            string lastGroup = null;
            foreach (var it in _items)
            {
                int idx = _grid.Rows.Add(it.Group, it.Key, it.Name, it.Axis,
                                         FormatTeachValue(it), GetTeachDisplayUnit(it), it.Desc);
                if (it.Group != lastGroup)
                {
                    _grid.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(0xEC, 0xF0, 0xF6);
                    _grid.Rows[idx].DefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                    lastGroup = it.Group;
                }
            }
        }

        private void OnCellEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            if (_grid.Columns[e.ColumnIndex].Name != "VALUE") return;
            string txt = (_grid.Rows[e.RowIndex].Cells["VALUE"].Value as string) ?? "0";
            if (TryParseDisplayDouble(txt, out double v))
            {
                TeachItem item = _items[e.RowIndex];
                BaseAxis axis = ResolveTeachAxis(item);
                item.Value = axis == null ? v : AxisUnitConverter.FromDisplay(v, axis);
                _grid.Rows[e.RowIndex].Cells["VALUE"].Value = FormatTeachValue(item);
                _grid.Rows[e.RowIndex].Cells["UNIT"].Value = GetTeachDisplayUnit(item);
            }
            else
            {
                QMC.Common.MessageDialog.Show("숫자만 입력 가능합니다.");
                _grid.Rows[e.RowIndex].Cells["VALUE"].Value = FormatTeachValue(_items[e.RowIndex]);
            }
        }

        private BaseAxis ResolveTeachAxis(TeachItem item)
        {
            try
            {
                var host = FindForm() as Form1;
                if (item == null || host?.Machine == null) return null;
                return ResolveAxis(host.Machine, item.Axis);
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private string GetTeachDisplayUnit(TeachItem item)
        {
            try
            {
                BaseAxis axis = ResolveTeachAxis(item);
                return axis == null ? item.Unit : AxisUnitConverter.DisplayUnitFor(axis);
            }
            catch
            {
                return item != null ? item.Unit : string.Empty;
            }
            finally
            {
            }
        }

        private string FormatTeachValue(TeachItem item)
        {
            try
            {
                BaseAxis axis = ResolveTeachAxis(item);
                double value = axis == null ? item.Value : AxisUnitConverter.ToDisplay(item.Value, axis);
                return value.ToString("F3");
            }
            catch
            {
                return item != null ? item.Value.ToString("F3") : "0.000";
            }
            finally
            {
            }
        }

        private static bool TryParseDisplayDouble(string text, out double value)
        {
            try
            {
                text = (text ?? string.Empty)
                    .Replace("mm/s2", string.Empty)
                    .Replace("um/s2", string.Empty)
                    .Replace("deg/s2", string.Empty)
                    .Replace("mm/s", string.Empty)
                    .Replace("um/s", string.Empty)
                    .Replace("deg/s", string.Empty)
                    .Replace("um", string.Empty)
                    .Replace("mm", string.Empty)
                    .Replace("deg", string.Empty)
                    .Replace("ms", string.Empty)
                    .Trim();

                return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                       double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
            }
            catch
            {
                value = 0.0;
                return false;
            }
            finally
            {
            }
        }

        // ──────────────────────────────────────
        //  Teach / MoveTo / Apply
        // ──────────────────────────────────────
        private TeachItem CurrentItem()
        {
            int i = _grid.CurrentRow?.Index ?? -1;
            if (i < 0 || i >= _items.Count) return null;
            return _items[i];
        }

        /// <summary>현재 선택된 행의 축의 Actual Position 을 Value 로 적용 (티칭).</summary>
        private void TeachFromCurrentPos()
        {
            var it = CurrentItem();
            if (it == null) return;
            var host = FindForm() as Form1;
            if (host?.Machine == null) { QMC.Common.MessageDialog.Show("Machine 미초기화"); return; }
            double pos = ResolveAxisActualPos(host.Machine, it.Axis);
            if (double.IsNaN(pos)) { QMC.Common.MessageDialog.Show("축을 식별하지 못했습니다: " + it.Axis); return; }
            it.Value = pos;
            _grid.Rows[_grid.CurrentRow.Index].Cells["VALUE"].Value = FormatTeachValue(it);
            _grid.Rows[_grid.CurrentRow.Index].Cells["UNIT"].Value = GetTeachDisplayUnit(it);
        }

        /// <summary>현재 행의 위치값으로 해당 축을 이동.</summary>
        private async void MoveToTaught()
        {
            var it = CurrentItem();
            if (it == null) return;
            var host = FindForm() as Form1;
            if (host?.Machine == null) { QMC.Common.MessageDialog.Show("Machine 미초기화"); return; }
            var ax = ResolveAxis(host.Machine, it.Axis);
            if (ax == null) { QMC.Common.MessageDialog.Show("축을 찾지 못했습니다: " + it.Axis); return; }
            try
            {
                if (!ax.IsServoOn) ax.ServoOn();
                await ax.MoveAbsoluteAsync(it.Value, 50.0);
            }
            catch (Exception ex) { QMC.Common.MessageDialog.Show("이동 실패: " + ex.Message); }
        }

        /// <summary>티칭 데이터를 각 Unit 의 Recipe/Config/Setup 객체에 반영하고 저장한다.</summary>
        private void ApplyToSetup()
        {
            var host = FindForm() as Form1;
            if (host?.Machine == null) { QMC.Common.MessageDialog.Show("Machine 미초기화"); return; }
            var m = host.Machine;
            int applied = 0;

            foreach (var it in _items)
            {
                try
                {
                    // Per-picker Z teaching (TPU.FrontP0~P3, TPU.RearP0~P3) — switch 외 처리
                    if (it.Group.StartsWith("TPU.FrontP") || it.Group.StartsWith("TPU.RearP"))
                    {
                        applied += ApplyPerPickerZ(m, it);
                        continue;
                    }

                    switch (it.Group + "." + it.Key)
                    {
                        //case "InputLoader.FirstSlotPosition":   m.InputLoader.Setup.FirstSlotPosition  = it.Value; applied++; break;
                        //case "InputLoader.ExchangePositionY":   m.InputLoader.Setup.ExchangePositionY  = it.Value; applied++; break;

                        // 인풋 스테이지 Expander Down 티칭 적용
                        case "InputStage.ExpanderDownPosition": m.InputStageUnit.Recipe.WaferZ.LoadPosition    = it.Value; applied++; break;
                        // 인풋 스테이지 Expander Up 티칭 적용
                        case "InputStage.ExpanderUpPosition":   m.InputStageUnit.Recipe.WaferZ.ReadyPosition   = it.Value; applied++; break;
                        // 인풋 스테이지 언로드 Y 티칭 적용
                        case "InputStage.UnloadPositionY":      m.InputStageUnit.Recipe.WaferY.UnloadPosition  = it.Value; applied++; break;
                        // 인풋 스테이지 니들 이젝트 티칭 적용
                        case "InputStage.NeedleEjectPosition":  m.InputStageUnit.Recipe.NeedleZ.ProcessPosition = it.Value; applied++; break;
                        // 인풋 스테이지 니들 다운 티칭 적용
                        case "InputStage.NeedleDownPosition":   m.InputStageUnit.Recipe.NeedleZ.LoadPosition   = it.Value; applied++; break;
                        // PickerOffsetX/Y are not declared in InputStageSetup/Config/Recipe.

                        // OutputStage (Stage 59 round 11)
                        // 아웃풋 GOOD/NG 스테이지 기준 Y 티칭 적용
                        case "OutputStage.StageBasePositionY":
                            m.OutputStageUnit.GoodStage.Recipe.HomePositionY = it.Value;
                            m.OutputStageUnit.NgStage.Recipe.HomePositionY = it.Value;
                            applied += 2; break;
                        // 아웃풋 비전 작업 X 티칭 적용
                        case "OutputStage.BinCameraWorkPositionX":  m.OutputStageUnit.Recipe.VisionX.ProcessPosition = it.Value; applied++; break;
                        // 아웃풋 비전 후퇴 X 티칭 적용
                        case "OutputStage.BinCameraRetractX":       m.OutputStageUnit.Recipe.VisionX.AvoidPosition = it.Value; applied++; break;

                        // OutputCassette / OutputFeeder
                        // 아웃풋 NG 카세트 첫 슬롯 Z 티칭 적용
                        case "OutputCassette.NgFirstSlotPositionZ":    m.OutputCassetteUnit.Recipe.NGFirstSlotPosition = it.Value; applied++; break;
                        // 아웃풋 GOOD 1단 첫 슬롯 Z 티칭 적용
                        case "OutputCassette.Good1FirstSlotPositionZ": m.OutputCassetteUnit.Recipe.GoodFirstSlotPosition = it.Value; applied++; break;
                        // 아웃풋 GOOD 2단 첫 슬롯 Z 티칭 적용
                        case "OutputCassette.Good2FirstSlotPositionZ":
                            m.OutputCassetteUnit.Config.Level2PositionOffset = Math.Max(
                                0.0,
                                m.OutputCassetteUnit.Recipe.GoodFirstSlotPosition -
                                (m.OutputCassetteUnit.Config.SlotPitch * Math.Max(0, m.OutputCassetteUnit.Config.SlotCount - 1)) -
                                it.Value);
                            applied++;
                            break;
                        // 아웃풋 카세트 슬롯 피치 Z 티칭 적용
                        case "OutputCassette.SlotPitchZ":              m.OutputCassetteUnit.Config.SlotPitch = it.Value; applied++; break;
                        // 아웃풋 피더 NG 스테이지 교환 Y 티칭 적용
                        case "OutputFeeder.NgStageExchangePositionY":  m.OutputFeederUnit.Recipe.NGWaferLoadPosition = it.Value; m.OutputFeederUnit.Recipe.NGWaferUnloadPosition = it.Value; applied++; break;
                        // 아웃풋 피더 GOOD 스테이지 교환 Y 티칭 적용
                        case "OutputFeeder.GoodStageExchangePositionY":m.OutputFeederUnit.Recipe.GoodWaferLoadPosition = it.Value; m.OutputFeederUnit.Recipe.GoodWaferUnloadPosition = it.Value; applied++; break;
                        // 아웃풋 피더 카세트 삽입 Y 티칭 적용
                        case "OutputFeeder.CassetteInsertPositionY":   m.OutputFeederUnit.Recipe.GoodCassetteExchangePosition = it.Value; m.OutputFeederUnit.Recipe.NGCassetteExchangePosition = it.Value; applied++; break;

                        // Stage 60 R-teach — 추가 매핑 (이전 16개 미적용 항목 보강)
                        // OutputStage Z exists only on GoodStage.
                        // 아웃풋 GOOD 스테이지 작업 Z 티칭 적용
                        case "OutputStage.WorkPositionZ":
                            m.OutputStageUnit.GoodStage.Recipe.WorkPositionZ  = it.Value;
                            applied++; break;
                        // 아웃풋 GOOD 스테이지 Avoid Z 티칭 적용
                        case "OutputStage.AvoidPositionZ":
                            m.OutputStageUnit.GoodStage.Recipe.AvoidPositionZ = it.Value;
                            applied++; break;

                        // (Per-picker Z teaching 은 switch 진입 전에 ApplyPerPickerZ 에서 처리)

                        // Picker position teaching is stored in Recipe. Old TPU.* Arm/Side keys are mapped here
                        // for compatibility with existing position_teaching JSON files.
                        // 프론트 피커 Pick X 티칭 적용
                        case "TPU.Front.ArmInputX":
                        case "TPU.Front.PickX":
                            m.PickerFrontUnit.Recipe.PickerX.PickPosition = it.Value; applied++; break;
                        // 프론트 피커 Pick Y 티칭 적용
                        case "TPU.Front.ArmYPickup":
                        case "TPU.Front.PickY":
                            m.PickerFrontUnit.Recipe.PickerY.PickPosition = it.Value; applied++; break;
                        // 프론트 피커 Bottom X 티칭 적용
                        case "TPU.Front.ArmInspectX":
                        case "TPU.Front.BottomX":
                            m.PickerFrontUnit.Recipe.PickerX.BottomPosition = it.Value; applied++; break;
                        // 프론트 피커 Bottom Y 티칭 적용
                        case "TPU.Front.BottomY":
                            m.PickerFrontUnit.Recipe.PickerY.BottomPosition = it.Value; applied++; break;
                        // 프론트 피커 Place X 티칭 적용
                        case "TPU.Front.ArmOutputX":
                        case "TPU.Front.PlaceX":
                            m.PickerFrontUnit.Recipe.PickerX.PlacePosition = it.Value; applied++; break;
                        // 프론트 피커 Place Y 티칭 적용
                        case "TPU.Front.PlaceY":
                            m.PickerFrontUnit.Recipe.PickerY.PlacePosition = it.Value; applied++; break;
                        // 프론트 피커 Side X 티칭 적용
                        case "TPU.Front.SideVision1X":
                        case "TPU.Front.SideX":
                            m.PickerFrontUnit.Recipe.PickerX.SidePosition = it.Value; applied++; break;
                        // 프론트 피커 Side Y 티칭 적용
                        case "TPU.Front.SideVision1Y":
                        case "TPU.Front.SideY":
                            m.PickerFrontUnit.Recipe.PickerY.SidePosition = it.Value; applied++; break;
                        // 프론트 피커 Pitch/SideY0 호환 항목 카운트
                        case "TPU.Front.PickerPitchX":
                        case "TPU.Front.SideY0":
                            applied++; break;
                        // 프론트 피커 Avoid Y 티칭 적용
                        case "TPU.Front.ArmYAvoid":
                        case "TPU.Front.AvoidY":
                            m.PickerFrontUnit.Recipe.PickerY.AvoidPosition = it.Value; applied++; break;

                        // 리어 피커 Pick X 티칭 적용
                        case "TPU.Rear.ArmInputX":
                        case "TPU.Rear.PickX":
                            m.PickerRearUnit.Recipe.PickerX.PickPosition = it.Value; applied++; break;
                        // 리어 피커 Pick Y 티칭 적용
                        case "TPU.Rear.ArmYPickup":
                        case "TPU.Rear.PickY":
                            m.PickerRearUnit.Recipe.PickerY.PickPosition = it.Value; applied++; break;
                        // 리어 피커 Bottom X 티칭 적용
                        case "TPU.Rear.ArmInspectX":
                        case "TPU.Rear.BottomX":
                            m.PickerRearUnit.Recipe.PickerX.BottomPosition = it.Value; applied++; break;
                        // 리어 피커 Bottom Y 티칭 적용
                        case "TPU.Rear.BottomY":
                            m.PickerRearUnit.Recipe.PickerY.BottomPosition = it.Value; applied++; break;
                        // 리어 피커 Place X 티칭 적용
                        case "TPU.Rear.ArmOutputX":
                        case "TPU.Rear.PlaceX":
                            m.PickerRearUnit.Recipe.PickerX.PlacePosition = it.Value; applied++; break;
                        // 리어 피커 Place Y 티칭 적용
                        case "TPU.Rear.PlaceY":
                            m.PickerRearUnit.Recipe.PickerY.PlacePosition = it.Value; applied++; break;
                        // 리어 피커 Side X 티칭 적용
                        case "TPU.Rear.SideVision1X":
                        case "TPU.Rear.SideX":
                            m.PickerRearUnit.Recipe.PickerX.SidePosition = it.Value; applied++; break;
                        // 리어 피커 Side Y 티칭 적용
                        case "TPU.Rear.SideVision1Y":
                        case "TPU.Rear.SideY":
                            m.PickerRearUnit.Recipe.PickerY.SidePosition = it.Value; applied++; break;
                        // 리어 피커 Pitch/SideY0 호환 항목 카운트
                        case "TPU.Rear.PickerPitchX":
                        case "TPU.Rear.SideY0":
                            applied++; break;
                        // 리어 피커 Avoid Y 티칭 적용
                        case "TPU.Rear.ArmYAvoid":
                        case "TPU.Rear.AvoidY":
                            m.PickerRearUnit.Recipe.PickerY.AvoidPosition = it.Value; applied++; break;

                        default:
                            // 매핑 미지원 항목 — JSON 저장은 되지만 Setup 미반영. 디버그용 로그.
                            QMC.Common.Logging.EventLogger.Write(
                                QMC.Common.Logging.EventKind.Event,
                                QMC.CDT_320.Ui.Security.UserSession.Name,
                                "TEACH-NOAPPLY",
                                $"{it.Group}.{it.Key} = {it.Value} (Setup property 미정의 — JSON 만 저장)");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        QMC.Common.Logging.EventLogger.Write(
                            QMC.Common.Logging.EventKind.Event,
                            QMC.CDT_320.Ui.Security.UserSession.Name,
                            "TEACH-EX",
                            $"{it.Group}.{it.Key}: {ex.GetType().Name}: {ex.Message}");
                    }
                    catch { }
                }
            }
            try
            {
                if (!string.IsNullOrWhiteSpace(host.CurrentRecipeName))
                    host.SaveMachineRecipe(host.CurrentRecipeName);

                host.SaveMachineSettings();
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    QMC.CDT_320.Ui.Security.UserSession.Name,
                    "TEACH-SAVE-FAIL",
                    "Position teaching apply save failed: " + ex.Message);
            }
            finally
            {
            }

            QMC.Common.MessageDialog.Show($"티칭 반영 완료: {applied} 항목\n\n" +
                            "Picker 위치 티칭은 Recipe에 저장됩니다.\n" +
                            "(미반영 항목은 JSON 에만 저장됨 — EventLog TEACH-NOAPPLY 참조)",
                            "Apply", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Per-picker Z teaching (TPU.FrontP{0..3} / TPU.RearP{0..3}) item 을 해당 picker 의
        /// PickerSetup 에 적용. Key = "PickPosition"|"PlacePosition"|"FocusPosition"|"WaitPosition".
        /// </summary>
        /// <returns>적용된 항목 수 (0 또는 1).</returns>
        private static int ApplyPerPickerZ(CDT320_Machine m, TeachItem it)
        {
            try
            {
                bool isFront;
                int  idx;
                if (it.Group.StartsWith("TPU.FrontP") && int.TryParse(it.Group.Substring(10), out idx))
                    isFront = true;
                else if (it.Group.StartsWith("TPU.RearP") && int.TryParse(it.Group.Substring(9), out idx))
                    isFront = false;
                else
                    return 0;
                if (idx < 0 || idx > 3) return 0;

                if (isFront)
                    m.PickerFrontUnit.SetRuntimePickerZPosition(idx, it.Key, it.Value);
                else
                    m.PickerRearUnit.SetRuntimePickerZPosition(idx, it.Key, it.Value);

                switch (it.Key)
                {
                    // Picker Z Pick 위치 저장 인정
                    case "PickPosition":
                    // Picker Z Place 위치 저장 인정
                    case "PlacePosition":
                    // Picker Z Focus 위치 저장 인정
                    case "FocusPosition":
                    // Picker Z Wait 위치 저장 인정
                    case "WaitPosition":
                        return 1;
                    default:
                        return 0;
                }
            }
            catch { return 0; }
        }

        // 축 이름 ("#09 FrontPickerX" 또는 "#31 OutputGoodStageY / #33 OutputNGStageY") 으로부터
        // 첫 번째 매칭 축의 Actual Position 반환 — 단순 매칭 (이름 substring).
        private static double ResolveAxisActualPos(CDT320_Machine m, string axisLabel)
        {
            var ax = ResolveAxis(m, axisLabel);
            return ax != null ? ax.ActualPosition : double.NaN;
        }

        private static QMC.Common.Motion.BaseAxis ResolveAxis(CDT320_Machine m, string axisLabel)
        {
            if (string.IsNullOrEmpty(axisLabel)) return null;
            // "/" 로 구분된 첫 토큰만 사용
            string label = axisLabel.Split('/')[0].Trim();

            // 1) "#NN ..." 에서 축 번호 추출 → SimulatorBridge 매핑 reverse-lookup (가장 정확)
            if (label.StartsWith("#") && label.Length > 1)
            {
                int sp = label.IndexOf(' ');
                string numStr = sp > 0 ? label.Substring(1, sp - 1) : label.Substring(1);
                if (int.TryParse(numStr, out int axisNo))
                {
                    var bridge = QMC.CDT320.SimulatorBridge.Instance;
                    var ax = bridge?.LookupAxisByNumber(axisNo);
                    if (ax != null) return ax;
                }
            }

            // 2) Fallback — 이름 substring 매칭 (#NN 없거나 lookup 실패 시)
            {
                int sp = label.IndexOf(' ');
                string namePart = sp > 0 ? label.Substring(sp + 1).Trim() : label;
                foreach (var ax in EnumerateAxes(m))
                {
                    if (string.IsNullOrEmpty(ax.Name)) continue;
                    if (ax.Name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0
                        || namePart.IndexOf(ax.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                        return ax;
                }
            }
            return null;
        }

        private static IEnumerable<QMC.Common.Motion.BaseAxis> EnumerateAxes(CDT320_Machine m)
        {
            foreach (var u in m.Units) foreach (var a in Rec(u)) yield return a;
        }
        private static IEnumerable<QMC.Common.Motion.BaseAxis> Rec(QMC.Common.BaseEquipmentNode node)
        {
            if (node is QMC.Common.Motion.BaseAxis ax) { yield return ax; yield break; }
            var prop = node.GetType().GetProperty("Components");
            if (prop != null && prop.GetValue(node) is System.Collections.IEnumerable comps)
                foreach (QMC.Common.BaseEquipmentNode c in comps)
                    foreach (var a in Rec(c)) yield return a;
        }
    }
}


