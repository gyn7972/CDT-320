using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Jobs;
using QMC.CDT320.Logging;
using QMC.CDT320.Materials;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// 시스템 전반 자가 진단 — 실행 시작 전 모든 서브시스템의 상태를 한 화면에 체크.
    ///   · AppSettings 로드
    ///   · AJINEXTEK AXL DLL 로드
    ///   · 축/DIO 매핑 컨피그
    ///   · 시뮬레이터 TCP (선택)
    ///   · QMC.Vision TCP 3개 모듈
    ///   · 이벤트 로그 쓰기
    ///   · 레시피 디렉토리 쓰기
    /// </summary>
    public class SystemSelfTestDialog : Form
    {
        private readonly List<(string name, Func<Task<(bool ok, string detail)>> test)> _tests
            = new List<(string, Func<Task<(bool, string)>>)>();

        private readonly Form1 _host;
        private DataGridView _grid;
        private Button _btnRun, _btnClose;
        private ProgressBar _pb;

        public SystemSelfTestDialog(Form1 host)
        {
            _host = host;
            Text            = "System Self-Test";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MinimizeBox     = MaximizeBox = false;
            ClientSize      = new Size(820, 560);
            BackColor       = UiTheme.MainBg;
            ShowIcon        = false;

            Build();
            RegisterTests();
            Seed();
        }

        private void Build()
        {
            var title = new Label
            {
                Dock = DockStyle.Top, Height = 40, Text = "SYSTEM SELF-TEST",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(title);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false, BackgroundColor = Color.White, Font = new Font("맑은 고딕", 9F),
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50, 0x50, 0x50), ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                },
                RowTemplate = { Height = 26 }
            };
            _grid.Columns.Add("NAME",   "Check");
            _grid.Columns.Add("STATE",  "State");
            _grid.Columns.Add("DETAIL", "Detail");
            Controls.Add(_grid);
            Controls.SetChildIndex(_grid, 0);

            _pb = new ProgressBar { Dock = DockStyle.Bottom, Height = 12 };
            Controls.Add(_pb);

            var bar = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = UiTheme.MainBg };
            _btnRun   = new Button { Location = new Point(600, 10), Size = new Size(100, 32), Text = "RUN",   FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            _btnClose = new Button { Location = new Point(708, 10), Size = new Size(100, 32), Text = "CLOSE", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, DialogResult = DialogResult.OK };
            _btnRun.Click += async (s, e) => await RunAll();
            bar.Controls.Add(_btnRun); bar.Controls.Add(_btnClose);
            Controls.Add(bar);
        }

        private void RegisterTests()
        {
            _tests.Add(("AppSettings",     () => Task.FromResult(
                (File.Exists(AppSettingsStore.Path_),
                 "path=" + AppSettingsStore.Path_))));

            _tests.Add(("AjinConfig",      () => Task.FromResult(
                (AjinConfigStore.Current != null,
                 $"axes={AjinConfigStore.Current.Axes.Count} dio={AjinConfigStore.Current.DigitalInputs.Count + AjinConfigStore.Current.DigitalOutputs.Count}"))));

            _tests.Add(("AXL library",     () => Task.FromResult(
                AppSettingsStore.Current.UseAjin
                    ? (AjinSystem.IsOpen, AjinSystem.IsOpen ? $"axes={AjinSystem.AxisCount} dio={AjinSystem.DioModuleCount}" : AjinSystem.LastError ?? "")
                    : (true, "disabled"))));

            _tests.Add(("Machine tree",    () => Task.FromResult(
                (_host?.Machine?.Units != null,
                 $"units={_host?.Machine?.Units?.Count ?? 0}"))));

            _tests.Add(("Simulator TCP",   async () =>
            {
                var b = _host?.Bridge;
                if (b == null) return (false, "no bridge");
                if (b.IsConnected) return (true, $"{b.Host}:{b.Port}");
                try
                {
                    var cfg = AppSettingsStore.Current;
                    await b.ConnectAsync(cfg.SimulatorHost, cfg.SimulatorPort);
                    return (b.IsConnected, b.IsConnected ? $"{b.Host}:{b.Port}" : "connect failed");
                }
                catch (Exception ex) { return (false, ex.Message); }
            }));

            _tests.Add(("Vision/Wafer",      async () => await PingVision(VisionHub.Wafer,      "WaferVision")));
            _tests.Add(("Vision/Inspection", async () => await PingVision(VisionHub.Inspection, "BottomInspection")));
            _tests.Add(("Vision/Bin",        async () => await PingVision(VisionHub.Bin,        "BinVision")));

            _tests.Add(("Event log writable", () =>
            {
                try
                {
                    EventLogger.Write(EventKind.Event, "SYS", "SELFTEST", "write ok");
                    return Task.FromResult((true, EventLogger.LogDir));
                }
                catch (Exception ex) { return Task.FromResult((false, ex.Message)); }
            }));

            _tests.Add(("Recipe dir writable", () =>
            {
                try
                {
                    var p = Path.Combine(QMC.CDT320.Recipes.RecipeStore.Dir, "_probe.tmp");
                    File.WriteAllText(p, "probe");
                    File.Delete(p);
                    return Task.FromResult((true, QMC.CDT320.Recipes.RecipeStore.Dir));
                }
                catch (Exception ex) { return Task.FromResult((false, ex.Message)); }
            }));

            // ── 310 이식 5 항목 ──
            _tests.Add(("BinCodeMap", () =>
            {
                try
                {
                    var d1 = new Die();                                          // good
                    var d2 = new Die();
                    d2.AddNG("ChippingTopOver");                                 // 110
                    int b1 = BinCodeMap.ConvertToBinCode(d1);
                    int b2 = BinCodeMap.ConvertToBinCode(d2);
                    bool ok = b1 == BinCodeMap.GoodBin && b2 != BinCodeMap.GoodBin;
                    return Task.FromResult((ok, $"good={b1} ng[ChippingTopOver]={b2}  codes={(BinCodeMap.Data.Codes?.Count ?? 0)}"));
                }
                catch (Exception ex) { return Task.FromResult((false, ex.Message)); }
            }));

            _tests.Add(("DieMap generator", () =>
            {
                try
                {
                    var f  = new DieTapeFrame { ObjId = "TST", GridX = 5, GridY = 5, PitchX = 1.0, PitchY = 1.0 };
                    var m  = DieMapGenerator.Generate(f);
                    bool ok = m != null && m.Entries.Count == 25;
                    return Task.FromResult((ok, $"5x5 → entries={m?.Entries?.Count ?? 0}"));
                }
                catch (Exception ex) { return Task.FromResult((false, ex.Message)); }
            }));

            _tests.Add(("JobQueue", () =>
            {
                try
                {
                    int beforeHist = JobQueue.HistoryCount;
                    var j = new JobOrder { Type = JobType.Pick, DieUid = "TST" };
                    JobQueue.Enqueue(j);
                    JobQueue.MarkRunning(j);
                    JobQueue.MarkDone(j, "self-test");
                    bool ok = JobQueue.HistoryCount > beforeHist;
                    return Task.FromResult((ok, $"history={JobQueue.HistoryCount} pending={JobQueue.PendingCount}"));
                }
                catch (Exception ex) { return Task.FromResult((false, ex.Message)); }
            }));

            _tests.Add(("InterlockRegistry", () =>
            {
                try
                {
                    bool ok = InterlockRegistry.VerifyMove("X_NONE", 0.0, out string reason);
                    return Task.FromResult((true, $"registered={InterlockRegistry.All.Count} verifyMove(X_NONE,0)={ok} reason={(reason ?? "-")}"));
                }
                catch (Exception ex) { return Task.FromResult((false, ex.Message)); }
            }));

            _tests.Add(("AlignmentSolver (3pt)", () =>
            {
                try
                {
                    // 3 점 비전→모터 매칭 — 단위 변환 사례
                    double[] px = { 0,   100, 0   };
                    double[] py = { 0,   0,   100 };
                    double[] mx = { 0,   1,   0   };
                    double[] my = { 0,   0,   1   };
                    var cm = AlignmentSolver.Solve3Point(px, py, mx, my, out string err);
                    bool ok = cm != null && string.IsNullOrEmpty(err);
                    double mmX = 0, mmY = 0;
                    if (cm != null) cm.ApplyToMotor(50, 50, out mmX, out mmY);
                    return Task.FromResult((ok, ok ? $"100px=1mm → (50,50)→({mmX:F3},{mmY:F3})" : err));
                }
                catch (Exception ex) { return Task.FromResult((false, ex.Message)); }
            }));
        }

        private async Task<(bool, string)> PingVision(VisionTcpClient c, string module)
        {
            if (c == null || !c.IsConnected) return (false, "not connected");
            try
            {
                bool ok = await c.PingAsync();
                return (ok, ok ? $"{c.Host}:{c.Port}" : "ping fail");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        private void Seed()
        {
            _grid.Rows.Clear();
            foreach (var t in _tests) _grid.Rows.Add(t.name, "-", "pending");
        }

        private async Task RunAll()
        {
            _btnRun.Enabled = false;
            _pb.Minimum = 0; _pb.Maximum = _tests.Count; _pb.Value = 0;

            EventLogger.Write(EventKind.Event, "SYS", "SELFTEST", "run start");

            for (int i = 0; i < _tests.Count; i++)
            {
                var row = _grid.Rows[i];
                row.Cells[1].Value = "...";
                try
                {
                    var (ok, detail) = await _tests[i].test();
                    row.Cells[1].Value = ok ? "OK" : "NG";
                    row.Cells[2].Value = detail ?? "";
                    row.DefaultCellStyle.BackColor = ok ? Color.FromArgb(0xD9, 0xFB, 0xD9) : Color.FromArgb(0xFB, 0xD9, 0xD9);
                }
                catch (Exception ex)
                {
                    row.Cells[1].Value = "EX";
                    row.Cells[2].Value = ex.Message;
                    row.DefaultCellStyle.BackColor = Color.FromArgb(0xFB, 0xD9, 0xD9);
                }
                _pb.Value = i + 1;
            }

            int ok2 = _grid.Rows.Cast<DataGridViewRow>().Count(r => (string)r.Cells[1].Value == "OK");
            EventLogger.Write(EventKind.Event, "SYS", "SELFTEST", $"done ok={ok2}/{_tests.Count}");
            _btnRun.Enabled = true;
        }
    }
}
