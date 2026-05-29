using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT_320.Ui.Localization;

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
            btnStep5.Click += (s, e) => { _jogStepBox.Text = "5"; };
            btnStep1.Click += (s, e) => { _jogStepBox.Text = "1"; };
            btnStep01.Click += (s, e) => { _jogStepBox.Text = "0.1"; };
            btnStep001.Click += (s, e) => { _jogStepBox.Text = "0.01"; };
            btnStep0001.Click += (s, e) => { _jogStepBox.Text = "0.001"; };

            btnTeach.Click += (s, e) => TeachFromCurrentPos();
            btnGoto.Click += (s, e) => MoveToTaught();
            btnApply.Click += (s, e) => ApplyToSetup();
            btnSave.Click += (s, e) => DoSave();
            btnReload.Click += (s, e) => { _items = LoadOrSeed(); FillGrid(); };
            btnReset.Click += (s, e) =>
            {
                if (MessageBox.Show("기본값으로 초기화하시겠습니까?", "Reset",
                                     MessageBoxButtons.OKCancel) != DialogResult.OK) return;
                _items = SeedDefault();
                FillGrid();
            };

            _jogPosTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _jogPosTimer.Tick += (s, e) => RefreshJogPos();
            _jogPosTimer.Start();
        }

        private void MultiplyStep(double factor)
        {
            if (!double.TryParse(_jogStepBox.Text, out double v) || v <= 0) v = 1.0;
            v *= factor;
            // 자릿수 자동 (소수점 너무 길지 않게)
            _jogStepBox.Text = v.ToString("0.######");
        }

        private void OnGridSelectionChanged()
        {
            var it = CurrentItem();
            if (it == null) { _jogCurrentAxis = null; _jogAxisLabel.Text = "Axis: (없음)"; ClearJogButtons(); return; }
            var host = FindForm() as Form1;
            if (host?.Machine == null) return;
            _jogCurrentAxis = ResolveAxis(host.Machine, it.Axis);
            _jogAxisLabel.Text = "Axis: " + it.Axis;
            RebuildJogButtons(DetectAxisDir(it.Axis));
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
                case 'X':
                    // 좌우 배치: [− ◀]   [▶ +]
                    area.Controls.Add(MkBtn("◀  −X",  new Point(12,  90), new Size(120, 80), -1));
                    area.Controls.Add(MkBtn("+X  ▶",  new Point(140, 90), new Size(120, 80), +1));
                    break;
                case 'Y':
                    area.Controls.Add(MkBtn("▲  +Y",  new Point(76, 20),  new Size(120, 80), +1));
                    area.Controls.Add(MkBtn("−Y  ▼",  new Point(76, 160), new Size(120, 80), -1));
                    break;
                case 'Z':
                    area.Controls.Add(MkBtn("▲  +Z",  new Point(76, 20),  new Size(120, 80), +1));
                    area.Controls.Add(MkBtn("−Z  ▼",  new Point(76, 160), new Size(120, 80), -1));
                    break;
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
            if (!double.TryParse(_jogSpeedBox.Text, out double speed) || speed <= 0) speed = 100;
            if (!double.TryParse(_jogStepBox.Text,  out double step)  || step  <= 0) step  = 1.0;
            try
            {
                if (!_jogCurrentAxis.IsServoOn) _jogCurrentAxis.ServoOn();
                await _jogCurrentAxis.MoveRelativeAsync(sign * step, speed);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Jog 실패: " + ex.Message, "Jog",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RefreshJogPos()
        {
            if (_jogPosLabel == null) return;
            _jogPosLabel.Text = _jogCurrentAxis == null
                ? "Actual Pos: -"
                : "Actual Pos: " + _jogCurrentAxis.ActualPosition.ToString("F3");
        }

        // ──────────────────────────────────────
        //  데이터 시드 (메뉴얼/코드 Setup 기반)
        // ──────────────────────────────────────
        public static List<TeachItem> SeedDefault()
        {
            var L = new List<TeachItem>();

            // ── InputLoader ────────────────────────────────────────────
            L.Add(new TeachItem { Group="InputLoader", Key="FirstSlotPosition",   Name="첫 슬롯 Z 위치",        Axis="#00 WaferLifterZ", Value=10.0,  Unit="mm",  Desc= "카세트 슬롯 0번 WaferLifterZ 절대 위치" });
            L.Add(new TeachItem { Group="InputLoader", Key="ExchangePositionY",   Name="교환 Y 위치",            Axis="#01 WaferFeederY", Value=150.0, Unit="mm",  Desc="피더가 InputStage 입구로 전진하는 Y" });

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

            L.Add(new TeachItem { Group="TPU.Front",   Key="ArmInputX",           Name="Arm X — Input",         Axis="#09 FrontPickerX",    Value=300.0,  Unit="mm", Desc="Pickup 위치" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="ArmInspectX",         Name="Arm X — Inspection",    Axis="#09 FrontPickerX",    Value=750.0,  Unit="mm", Desc="Bottom/Side Vision 위치" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="ArmOutputX",          Name="Arm X — Output",        Axis="#09 FrontPickerX",    Value=1200.0, Unit="mm", Desc="Place 위치" });
            // Stage 61 — Front Arm Y Pickup / Avoid
            L.Add(new TeachItem { Group="TPU.Front",   Key="ArmYPickup",          Name="Arm Y — Pickup",        Axis="#10 FrontPickerY",    Value=100.0,  Unit="mm", Desc="다이 픽업 시 ArmY 위치" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="ArmYAvoid",           Name="Arm Y — Avoid",         Axis="#10 FrontPickerY",    Value=50.0,   Unit="mm", Desc="이동 중 간섭 회피 ArmY" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="SideVision1X",        Name="Side1 X (회전 전)",     Axis="#09 FrontPickerX",    Value=720.0,  Unit="mm", Desc="Side 1번 면 촬상 X" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="SideVision1Y",        Name="Side1 Y",               Axis="#10 FrontPickerY",    Value=200.0,  Unit="mm", Desc="Side 1번 면 촬상 Y" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="PickerPitchX",        Name="Picker 간 피치 X",      Axis="#09 FrontPickerX",    Value=8.0,    Unit="mm", Desc="4 picker 사이 X 거리" });
            L.Add(new TeachItem { Group="TPU.Front",   Key="SideY0",              Name="Side Vision Y0",        Axis="#19 FrontSideVisionY0", Value=0.0, Unit="mm", Desc="Side 카메라 베이스 위치" });

            L.Add(new TeachItem { Group="TPU.Rear",    Key="ArmInputX",           Name="Rear Arm X — Input",    Axis="#21 RearPickerX",     Value=300.0,  Unit="mm", Desc="Pickup" });
            L.Add(new TeachItem { Group="TPU.Rear",    Key="ArmInspectX",         Name="Rear Arm X — Inspect",  Axis="#21 RearPickerX",     Value=750.0,  Unit="mm", Desc="Inspection" });
            L.Add(new TeachItem { Group="TPU.Rear",    Key="ArmOutputX",          Name="Rear Arm X — Output",   Axis="#21 RearPickerX",     Value=1200.0, Unit="mm", Desc="Place" });
            // Stage 61 — Rear Arm Y Pickup / Avoid
            L.Add(new TeachItem { Group="TPU.Rear",    Key="ArmYPickup",          Name="Rear Arm Y — Pickup",   Axis="#22 RearPickerY",     Value=100.0,  Unit="mm", Desc="다이 픽업 시 ArmY 위치" });
            L.Add(new TeachItem { Group="TPU.Rear",    Key="ArmYAvoid",           Name="Rear Arm Y — Avoid",    Axis="#22 RearPickerY",     Value=50.0,   Unit="mm", Desc="이동 중 간섭 회피 ArmY" });
            L.Add(new TeachItem { Group="TPU.Rear",    Key="SideY0",              Name="Rear Side Vision Y0",   Axis="#20 RearSideVisionY0",  Value=0.0, Unit="mm", Desc="Rear Side 카메라 베이스" });

            // ── OutputStage ────────────────────────────────────────────
            L.Add(new TeachItem { Group="OutputStage", Key="StageBasePositionY",     Name="StageY 기준 Y",       Axis="#31 BinGoodY / #33 BinNgY", Value=200.0, Unit="mm", Desc="Place 시 StageY 기준" });
            L.Add(new TeachItem { Group="OutputStage", Key="WorkPositionZ",          Name="Work Z (상승)",       Axis="#32 BinGoodZ",          Value=80.0,  Unit="mm", Desc="다이 받을 때 StageZ 상승" });
            L.Add(new TeachItem { Group="OutputStage", Key="AvoidPositionZ",         Name="Avoid Z (하강)",      Axis="#32 BinGoodZ",          Value=0.0,   Unit="mm", Desc="반대편 작업 시 회피 위치" });
            L.Add(new TeachItem { Group="OutputStage", Key="BinCameraWorkPositionX", Name="BinCamera Work X",    Axis="#34 BinVisionX", Value=200.0, Unit="mm", Desc="Bin 검사 위치" });
            L.Add(new TeachItem { Group="OutputStage", Key="BinCameraRetractX",      Name="BinCamera Retract X", Axis="#34 BinVisionX", Value=0.0,   Unit="mm", Desc="후퇴(대기) 위치" });

            // ── OutputUnloader ─────────────────────────────────────────
            L.Add(new TeachItem { Group="OutputUnloader", Key="NgFirstSlotPositionZ",    Name="NG Slot 0 Z",           Axis="#36 BinLifterZ",  Value=20.0,   Unit="mm", Desc="NG 카세트 첫 슬롯" });
            L.Add(new TeachItem { Group="OutputUnloader", Key="Good1FirstSlotPositionZ", Name="Good1 Slot 0 Z",        Axis="#36 BinLifterZ",  Value=80.0,   Unit="mm", Desc="Good1 카세트 첫 슬롯" });
            L.Add(new TeachItem { Group="OutputUnloader", Key="Good2FirstSlotPositionZ", Name="Good2 Slot 0 Z",        Axis="#36 BinLifterZ",  Value=160.0,  Unit="mm", Desc="Good2 카세트 첫 슬롯" });
            L.Add(new TeachItem { Group="OutputUnloader", Key="SlotPitchZ",              Name="슬롯 피치 Z",            Axis="#36 BinLifterZ",  Value=6.0,    Unit="mm", Desc="슬롯 간 간격" });
            L.Add(new TeachItem { Group="OutputUnloader", Key="NgStageExchangePositionY",  Name="NG 교환 Y",            Axis="#35 BinFeederY",  Value=180.0,  Unit="mm", Desc="NG 카세트 인계 Y" });
            L.Add(new TeachItem { Group="OutputUnloader", Key="GoodStageExchangePositionY",Name="Good 교환 Y",          Axis="#35 BinFeederY",  Value=180.0,  Unit="mm", Desc="Good 카세트 인계 Y" });
            L.Add(new TeachItem { Group="OutputUnloader", Key="CassetteInsertPositionY",   Name="카세트 삽입 Y",        Axis="#35 BinFeederY",  Value=250.0,  Unit="mm", Desc="카세트 안쪽 진입 위치" });

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
                    var ser = new DataContractJsonSerializer(typeof(TeachStore));
                    ser.WriteObject(fs, new TeachStore { Items = _items });
                }
                MessageBox.Show("티칭 데이터 저장 완료.\n" + SavePath, "Position Teaching",
                                 MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("저장 실패: " + ex.Message);
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
                                         it.Value.ToString("F3"), it.Unit, it.Desc);
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
            if (double.TryParse(txt, out double v))
            {
                _items[e.RowIndex].Value = v;
                _grid.Rows[e.RowIndex].Cells["VALUE"].Value = v.ToString("F3");
            }
            else
            {
                MessageBox.Show("숫자만 입력 가능합니다.");
                _grid.Rows[e.RowIndex].Cells["VALUE"].Value = _items[e.RowIndex].Value.ToString("F3");
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
            if (host?.Machine == null) { MessageBox.Show("Machine 미초기화"); return; }
            double pos = ResolveAxisActualPos(host.Machine, it.Axis);
            if (double.IsNaN(pos)) { MessageBox.Show("축을 식별하지 못했습니다: " + it.Axis); return; }
            it.Value = pos;
            _grid.Rows[_grid.CurrentRow.Index].Cells["VALUE"].Value = pos.ToString("F3");
        }

        /// <summary>현재 행의 위치값으로 해당 축을 이동.</summary>
        private async void MoveToTaught()
        {
            var it = CurrentItem();
            if (it == null) return;
            var host = FindForm() as Form1;
            if (host?.Machine == null) { MessageBox.Show("Machine 미초기화"); return; }
            var ax = ResolveAxis(host.Machine, it.Axis);
            if (ax == null) { MessageBox.Show("축을 찾지 못했습니다: " + it.Axis); return; }
            try
            {
                if (!ax.IsServoOn) ax.ServoOn();
                await ax.MoveAbsoluteAsync(it.Value, 50.0);
            }
            catch (Exception ex) { MessageBox.Show("이동 실패: " + ex.Message); }
        }

        /// <summary>티칭 데이터를 각 Unit 의 Setup 객체에 반영 (런타임 적용).</summary>
        private void ApplyToSetup()
        {
            var host = FindForm() as Form1;
            if (host?.Machine == null) { MessageBox.Show("Machine 미초기화"); return; }
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
                        case "InputLoader.FirstSlotPosition":   m.InputLoader.Setup.FirstSlotPosition  = it.Value; applied++; break;
                        case "InputLoader.ExchangePositionY":   m.InputLoader.Setup.ExchangePositionY  = it.Value; applied++; break;

                        case "InputStage.ExpanderDownPosition": m.InputStage.Setup.ExpanderDownPosition = it.Value; applied++; break;
                        case "InputStage.ExpanderUpPosition":   m.InputStage.Setup.ExpanderUpPosition   = it.Value; applied++; break;
                        case "InputStage.UnloadPositionY":      m.InputStage.Setup.UnloadPositionY      = it.Value; applied++; break;
                        case "InputStage.NeedleEjectPosition":  m.InputStage.Setup.NeedleEjectPosition  = it.Value; applied++; break;
                        case "InputStage.NeedleDownPosition":   m.InputStage.Setup.NeedleDownPosition   = it.Value; applied++; break;
                        case "InputStage.PickerOffsetX":        m.InputStage.Setup.PickerOffsetX        = it.Value; applied++; break;
                        case "InputStage.PickerOffsetY":        m.InputStage.Setup.PickerOffsetY        = it.Value; applied++; break;

                        // OutputStage (Stage 59 round 11)
                        case "OutputStage.StageBasePositionY":      m.OutputStage.Setup.StageBasePositionY        = it.Value; applied++; break;
                        case "OutputStage.BinCameraWorkPositionX":  m.OutputStage.Setup.BinCameraWorkPositionX    = it.Value; applied++; break;
                        case "OutputStage.BinCameraRetractX":       m.OutputStage.Setup.BinCameraRetractPositionX = it.Value; applied++; break;

                        // OutputUnloader (Stage 59 round 11)
                        case "OutputUnloader.NgFirstSlotPositionZ":    m.OutputUnloader.Setup.NgFirstSlotPositionZ    = it.Value; applied++; break;
                        case "OutputUnloader.Good1FirstSlotPositionZ": m.OutputUnloader.Setup.Good1FirstSlotPositionZ = it.Value; applied++; break;
                        case "OutputUnloader.Good2FirstSlotPositionZ": m.OutputUnloader.Setup.Good2FirstSlotPositionZ = it.Value; applied++; break;
                        case "OutputUnloader.SlotPitchZ":              m.OutputUnloader.Setup.SlotPitchZ              = it.Value; applied++; break;
                        case "OutputUnloader.NgStageExchangePositionY":  m.OutputUnloader.Setup.NgStageExchangePositionY   = it.Value; applied++; break;
                        case "OutputUnloader.GoodStageExchangePositionY":m.OutputUnloader.Setup.GoodStageExchangePositionY = it.Value; applied++; break;
                        case "OutputUnloader.CassetteInsertPositionY":   m.OutputUnloader.Setup.CassetteInsertPositionY    = it.Value; applied++; break;

                        // Stage 60 R-teach — 추가 매핑 (이전 16개 미적용 항목 보강)
                        // OutputStage 의 두 StageModule (Good + Ng) 모두 동일 값 적용
                        case "OutputStage.WorkPositionZ":
                            m.OutputStage.GoodStage.Setup.WorkPositionZ  = it.Value;
                            m.OutputStage.NgStage  .Setup.WorkPositionZ  = it.Value;
                            applied += 2; break;
                        case "OutputStage.AvoidPositionZ":
                            m.OutputStage.GoodStage.Setup.AvoidPositionZ = it.Value;
                            m.OutputStage.NgStage  .Setup.AvoidPositionZ = it.Value;
                            applied += 2; break;

                        // (Per-picker Z teaching 은 switch 진입 전에 ApplyPerPickerZ 에서 처리)

                        // TpuArmSetup — Front
                        case "TPU.Front.ArmInputX":
                            m.TransferPicker.LeftArm.Setup.ArmInputPositionX      = it.Value; applied++; break;
                        case "TPU.Front.ArmInspectX":
                            m.TransferPicker.LeftArm.Setup.ArmInspectionPositionX = it.Value; applied++; break;
                        case "TPU.Front.ArmOutputX":
                            m.TransferPicker.LeftArm.Setup.ArmOutputPositionX     = it.Value; applied++; break;
                        case "TPU.Front.SideVision1X":
                            m.TransferPicker.LeftArm.Setup.SideVision1X = it.Value; applied++; break;
                        case "TPU.Front.SideVision1Y":
                            m.TransferPicker.LeftArm.Setup.SideVision1Y = it.Value; applied++; break;
                        case "TPU.Front.PickerPitchX":
                            m.TransferPicker.LeftArm.Setup.PickerPitchX = it.Value; applied++; break;
                        case "TPU.Front.SideY0":
                            m.TransferPicker.LeftArm.Setup.SideVisionY0 = it.Value; applied++; break;
                        case "TPU.Front.ArmYPickup":
                            m.TransferPicker.LeftArm.Setup.ArmYPickupPosition = it.Value; applied++; break;
                        case "TPU.Front.ArmYAvoid":
                            m.TransferPicker.LeftArm.Setup.ArmYAvoidPosition  = it.Value; applied++; break;

                        // TpuArmSetup — Rear (대칭)
                        case "TPU.Rear.ArmInputX":
                            m.TransferPicker.RightArm.Setup.ArmInputPositionX      = it.Value; applied++; break;
                        case "TPU.Rear.ArmInspectX":
                            m.TransferPicker.RightArm.Setup.ArmInspectionPositionX = it.Value; applied++; break;
                        case "TPU.Rear.ArmOutputX":
                            m.TransferPicker.RightArm.Setup.ArmOutputPositionX     = it.Value; applied++; break;
                        case "TPU.Rear.SideVision1X":
                            m.TransferPicker.RightArm.Setup.SideVision1X = it.Value; applied++; break;
                        case "TPU.Rear.SideVision1Y":
                            m.TransferPicker.RightArm.Setup.SideVision1Y = it.Value; applied++; break;
                        case "TPU.Rear.PickerPitchX":
                            m.TransferPicker.RightArm.Setup.PickerPitchX = it.Value; applied++; break;
                        case "TPU.Rear.SideY0":
                            m.TransferPicker.RightArm.Setup.SideVisionY0 = it.Value; applied++; break;
                        case "TPU.Rear.ArmYPickup":
                            m.TransferPicker.RightArm.Setup.ArmYPickupPosition = it.Value; applied++; break;
                        case "TPU.Rear.ArmYAvoid":
                            m.TransferPicker.RightArm.Setup.ArmYAvoidPosition  = it.Value; applied++; break;

                        default:
                            // 매핑 미지원 항목 — JSON 저장은 되지만 Setup 미반영. 디버그용 로그.
                            QMC.CDT320.Logging.EventLogger.Write(
                                QMC.CDT320.Logging.EventKind.Event,
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
                        QMC.CDT320.Logging.EventLogger.Write(
                            QMC.CDT320.Logging.EventKind.Event,
                            QMC.CDT_320.Ui.Security.UserSession.Name,
                            "TEACH-EX",
                            $"{it.Group}.{it.Key}: {ex.GetType().Name}: {ex.Message}");
                    }
                    catch { }
                }
            }
            MessageBox.Show($"Setup 반영 완료: {applied} 항목\n\n" +
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

                var arm = isFront ? m.TransferPicker.LeftArm : m.TransferPicker.RightArm;
                var p   = arm.Pickers[idx];
                switch (it.Key)
                {
                    case "PickPosition":  p.Setup.PickupPosition = it.Value; return 1;
                    case "PlacePosition": p.Setup.PlacePosition  = it.Value; return 1;
                    case "FocusPosition": p.Setup.FocusPosition  = it.Value; return 1;
                    case "WaitPosition":  p.Setup.WaitPosition   = it.Value; return 1;
                    default: return 0;
                }
            }
            catch { return 0; }
        }

        // 축 이름 ("#09 FrontPickerX" 또는 "#31 BinGoodY / #33 BinNgY") 으로부터
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
