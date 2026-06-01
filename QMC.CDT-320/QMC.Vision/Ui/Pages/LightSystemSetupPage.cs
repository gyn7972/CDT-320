using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.Common.Recipes;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// Stage 69 — 조명 시스템 Setup 페이지.
    /// 섹션 1: 컨트롤러 인벤토리 (Port/Baud/ChannelCount/PageCount/MaxPower) + 채널 라벨.
    /// 섹션 2: 알고리즘 결선 표 (5 알고리즘 × 컨트롤러 콤보 + 채널 다중체크 + 페이지).
    /// 포트 일괄 변경 버튼 포함. 저장: Config\light_system.json.
    /// </summary>
    public class LightSystemSetupPage : UserControl
    {
        private DataGridView _gridCtrl;     // 컨트롤러 인벤토리
        private DataGridView _gridLabel;    // 선택 컨트롤러의 채널 라벨
        private DataGridView _gridWiring;   // 알고리즘 결선
        private Label _lblStatus;

        public LightSystemSetupPage()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildLayout();
            LoadFromStore();
        }

        private void BuildLayout()
        {
            BackColor = UiTheme.MainBg;

            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 30, Text = "조명 시스템 설정 — 컨트롤러 인벤토리 + 알고리즘 결선",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            var bar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.WhiteSmoke };
            var btnSave    = MakeBtn("저장", 8, UiTheme.Accent, Color.White);   btnSave.Click    += (s, e) => Save();
            var btnReload  = MakeBtn("취소", 120, Color.White, Color.Black);    btnReload.Click  += (s, e) => LoadFromStore();
            var btnAddCtrl = MakeBtn("컨트롤러 추가", 210, Color.White, Color.Black); btnAddCtrl.Width = 120; btnAddCtrl.Click += (s, e) => AddController();
            var btnMigrate = MakeBtn("io_set 가져오기", 340, Color.White, Color.Black); btnMigrate.Width = 130; btnMigrate.Click += (s, e) => Migrate();
            var btnRename  = MakeBtn("포트 일괄 변경", 480, Color.White, Color.Black); btnRename.Width = 130; btnRename.Click += (s, e) => RenamePort();
            bar.Controls.AddRange(new Control[] { btnSave, btnReload, btnAddCtrl, btnMigrate, btnRename });
            Controls.Add(bar);

            _lblStatus = new Label { Dock = DockStyle.Bottom, Height = 24, Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray, Padding = new Padding(8, 2, 0, 0) };
            Controls.Add(_lblStatus);

            // 본문: 좌(컨트롤러+라벨) / 우(결선)
            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiTheme.MainBg };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            Controls.Add(split);
            Controls.SetChildIndex(split, 0);

            // 좌측: 컨트롤러 인벤토리 + 채널 라벨 (세로 2분할)
            var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = UiTheme.MainBg };
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            left.Controls.Add(SectionLabel("컨트롤러 인벤토리 (PortName 고유)"), 0, 0);
            _gridCtrl = MakeGrid();
            _gridCtrl.Columns.Add(Col("PortName", "PortName"));
            _gridCtrl.Columns.Add(Col("Name", "Name"));
            _gridCtrl.Columns.Add(Col("BaudRate", "Baud"));
            _gridCtrl.Columns.Add(Col("ChannelCount", "Ch수"));
            _gridCtrl.Columns.Add(Col("PageCount", "Page수"));
            _gridCtrl.Columns.Add(Col("MaxPower", "MaxPwr"));
            _gridCtrl.SelectionChanged += (s, e) => BindLabelsForSelectedController();
            left.Controls.Add(_gridCtrl, 0, 1);
            left.Controls.Add(SectionLabel("선택 컨트롤러의 채널 라벨"), 0, 2);
            _gridLabel = MakeGrid();
            _gridLabel.Columns.Add(Col("Channel", "Ch"));
            _gridLabel.Columns.Add(Col("Name", "Name"));
            _gridLabel.Columns.Add(Col("Color", "Color"));
            left.Controls.Add(_gridLabel, 0, 3);
            split.Controls.Add(left, 0, 0);

            // 우측: 알고리즘 결선
            var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = UiTheme.MainBg };
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            right.Controls.Add(SectionLabel("알고리즘 결선 (컨트롤러 + 사용 채널 + 페이지)"), 0, 0);
            _gridWiring = MakeGrid();
            _gridWiring.AllowUserToAddRows = false;   // 5 알고리즘 고정
            var colAlg = Col("Algorithm", "알고리즘"); colAlg.ReadOnly = true; _gridWiring.Columns.Add(colAlg);
            _gridWiring.Columns.Add(Col("ControllerPort", "컨트롤러(Port)"));
            _gridWiring.Columns.Add(Col("ChannelsCsv", "채널 (예: 3,4,5)"));
            _gridWiring.Columns.Add(Col("Page", "Page"));
            right.Controls.Add(_gridWiring, 0, 1);
            split.Controls.Add(right, 0, 0);
            split.Controls.Add(right, 1, 0);
        }

        // ── 로드/저장 ──
        private void LoadFromStore()
        {
            var setup = LightSystemSetupStore.Load();
            BindAll(setup);
            SetStatus("로드 완료 — 컨트롤러 " + (setup.Controllers?.Count ?? 0) + "개", false);
        }

        private void BindAll(LightSystemSetup setup)
        {
            _gridCtrl.Rows.Clear();
            foreach (var c in setup.Controllers)
                _gridCtrl.Rows.Add(c.PortName, c.Name, c.BaudRate, c.ChannelCount, c.PageCount, c.MaxPower);

            _gridWiring.Rows.Clear();
            foreach (var alg in VisionAlgorithm.All)
            {
                var w = setup.GetWiring(alg);
                string csv = (w?.Channels != null) ? string.Join(",", w.Channels) : "";
                _gridWiring.Rows.Add(VisionAlgorithm.Label(alg) + " (" + alg + ")", w?.ControllerPort ?? "", csv, w?.Page ?? 0);
            }
            BindLabelsForSelectedController();
        }

        private void BindLabelsForSelectedController()
        {
            _gridLabel.Rows.Clear();
            var port = SelectedControllerPort();
            if (port == null) return;
            var c = LightSystemSetupStore.Current.GetController(port);
            if (c?.ChannelLabels == null) return;
            foreach (var l in c.ChannelLabels) _gridLabel.Rows.Add(l.Channel, l.Name, l.Color);
        }

        private string SelectedControllerPort()
        {
            if (_gridCtrl.CurrentRow == null) return null;
            return _gridCtrl.CurrentRow.Cells[0].Value?.ToString();
        }

        /// <summary>UI → LightSystemSetup 객체 수집.</summary>
        private LightSystemSetup CollectFromUi()
        {
            var setup = new LightSystemSetup();
            foreach (DataGridViewRow r in _gridCtrl.Rows)
            {
                if (r.IsNewRow) continue;
                string port = Str(r, 0);
                if (string.IsNullOrEmpty(port)) continue;
                // 기존 채널 라벨 보존 (현재 store 에서)
                var existing = LightSystemSetupStore.Current.GetController(port);
                setup.Controllers.Add(new LightControllerEntry
                {
                    PortName = port, Name = Str(r, 1),
                    BaudRate = IntOf(r, 2, 9600), ChannelCount = IntOf(r, 3, 8),
                    PageCount = IntOf(r, 4, 1), MaxPower = IntOf(r, 5, 240),
                    ChannelLabels = existing?.ChannelLabels ?? new System.Collections.Generic.List<LightChannelLabel>()
                });
            }
            setup.EnsureWirings();
            int wi = 0;
            foreach (var alg in VisionAlgorithm.All)
            {
                if (wi >= _gridWiring.Rows.Count) break;
                var r = _gridWiring.Rows[wi++];
                var w = setup.GetWiring(alg);
                w.ControllerPort = Str(r, 1);
                w.Channels = ParseCsv(Str(r, 2));
                w.Page = IntOf(r, 3, 0);
            }
            return setup;
        }

        private void Save()
        {
            var setup = CollectFromUi();
            var errs = setup.Validate();
            if (errs.Count > 0) { SetStatus("저장 거부 — " + string.Join(" / ", errs.Take(3)), true); return; }
            LightSystemSetupStore.SetCurrent(setup);
            LightSystemSetupStore.Save();
            SetStatus("저장 완료 — " + LightSystemSetupStore.Path_, false);
        }

        private void AddController()
        {
            _gridCtrl.Rows.Add("COM?", "Illuminator", 9600, 8, 1, 240);
            SetStatus("컨트롤러 행 추가 — PortName 수정 후 저장", false);
        }

        private void Migrate()
        {
            // io_set.lightSource.json 은 Handler/Vision 의 Config 폴더에 위치 — 현 exe Config 우선.
            string ioSet = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "io_set.lightSource.json");
            var setup = LightSystemMigrator.MigrateFromLegacy(ioSet);
            if (setup == null) { SetStatus("io_set.lightSource.json 없음/파싱 실패: " + ioSet, true); return; }
            LightSystemMigrator.BackupLegacy(ioSet, DateTime.Now.ToString("yyyyMMdd"));
            setup.EnsureWirings();
            LightSystemSetupStore.SetCurrent(setup);
            BindAll(setup);
            SetStatus($"가져오기 완료 — 컨트롤러 {setup.Controllers.Count}개 (저장 필요, 모호 채널은 결선 표에서 직접 배정)", false);
        }

        private void RenamePort()
        {
            string oldPort = SelectedControllerPort();
            if (string.IsNullOrEmpty(oldPort)) { SetStatus("컨트롤러를 먼저 선택하세요", true); return; }
            string newPort = Prompt("새 PortName 입력 (예: COM5)", oldPort);
            if (string.IsNullOrEmpty(newPort) || newPort == oldPort) return;
            // 현재 store 에 반영 후 재바인딩 (인벤토리 + 모든 결선 동시 갱신)
            var setup = CollectFromUi();
            // CollectFromUi 는 라벨을 store 기준으로 보존하므로 store 를 임시 갱신
            LightSystemSetupStore.SetCurrent(setup);
            if (!setup.RenamePort(oldPort, newPort)) { SetStatus("포트 변경 실패 (대상 포트 중복?)", true); return; }
            BindAll(setup);
            SetStatus($"포트 변경: {oldPort} → {newPort} (결선 동시 갱신, 저장 필요)", false);
        }

        // ── helpers ──
        private static Button MakeBtn(string t, int x, Color bg, Color fg)
            => new Button { Location = new Point(x, 4), Size = new Size(100, 32), Text = t, FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = bg, ForeColor = fg };
        private static Label SectionLabel(string t)
            => new Label { Dock = DockStyle.Fill, Text = "  " + t, Font = UiTheme.ButtonFont, ForeColor = Color.Black, BackColor = UiTheme.SidebarHeaderBg, TextAlign = ContentAlignment.MiddleLeft };
        private static DataGridView MakeGrid()
            => new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = true, AllowUserToDeleteRows = true, RowHeadersVisible = false, Font = UiTheme.ValueFont, BackgroundColor = Color.White, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private static DataGridViewTextBoxColumn Col(string name, string header)
            => new DataGridViewTextBoxColumn { Name = name, HeaderText = header };
        private static string Str(DataGridViewRow r, int i) => r.Cells[i].Value?.ToString()?.Trim() ?? "";
        private static int IntOf(DataGridViewRow r, int i, int def) => int.TryParse(Str(r, i), out var v) ? v : def;
        private static System.Collections.Generic.List<int> ParseCsv(string csv)
            => string.IsNullOrEmpty(csv) ? new System.Collections.Generic.List<int>()
               : csv.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)
                    .Select(s => int.TryParse(s, out var v) ? v : -1).Where(v => v > 0).Distinct().ToList();
        private void SetStatus(string msg, bool err) { _lblStatus.ForeColor = err ? Color.Firebrick : Color.DarkSlateGray; _lblStatus.Text = msg; }

        private static string Prompt(string text, string def)
        {
            using (var f = new Form { Width = 360, Height = 150, Text = "입력", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog })
            {
                var lbl = new Label { Left = 12, Top = 12, Width = 320, Text = text };
                var tb  = new TextBox { Left = 12, Top = 38, Width = 320, Text = def };
                var ok  = new Button { Text = "OK", Left = 176, Top = 70, Width = 70, DialogResult = DialogResult.OK };
                var ca  = new Button { Text = "취소", Left = 256, Top = 70, Width = 70, DialogResult = DialogResult.Cancel };
                f.Controls.AddRange(new Control[] { lbl, tb, ok, ca });
                f.AcceptButton = ok; f.CancelButton = ca;
                return f.ShowDialog() == DialogResult.OK ? tb.Text.Trim() : null;
            }
        }
    }
}
