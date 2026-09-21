using QMC.CDT_320.Ui.Localization;
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
using QMC.Common.Logging;
using QMC.CDT320.Materials;
using QMC.CDT320.VisionComm;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class SystemSelfTestDialog : Form
    {
        private readonly List<(string name, Func<Task<(bool ok, string detail)>> test)> _tests
            = new List<(string, Func<Task<(bool, string)>>)>();

        private readonly Form1 _host;

        public SystemSelfTestDialog(Form1 host)
        {
            _host = host;
            InitializeComponent();
            InitializeLanguageBindings();
            ApplyDialogStyle();
            Lang.BindReadOnlyCells(_grid, TranslateTestDisplay);
            WireEvents();
            RegisterTests();
            Seed();
        }

        private void WireEvents()
        {
            _btnRun.Click += async (s, e) => await RunAll();
        }

        private void RegisterTests()
        {
            _tests.Add(("AppSettings", () => Task.FromResult(
                (File.Exists(AppSettingsStore.Path_),
                 "path=" + AppSettingsStore.Path_))));

            _tests.Add(("AjinConfig", () => Task.FromResult(
                (AjinConfigStore.Current != null,
                 $"axes={AjinConfigStore.Current.Axes.Count} dio={AjinConfigStore.Current.DigitalInputs.Count + AjinConfigStore.Current.DigitalOutputs.Count}"))));

            _tests.Add(("AXL library", () => Task.FromResult(
                AppSettingsStore.Current.UseAjin
                    ? (AjinSystem.IsOpen, AjinSystem.IsOpen ? $"axes={AjinSystem.AxisCount} dio={AjinSystem.DioModuleCount}" : AjinSystem.LastError ?? "")
                    : (true, "disabled"))));

            _tests.Add(("Machine tree", () => Task.FromResult(
                (_host?.Machine?.Units != null,
                 $"units={_host?.Machine?.Units?.Count ?? 0}"))));

            _tests.Add(("Simulator TCP", async () =>
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

            _tests.Add(("Vision/Wafer", async () => await PingVision(VisionHub.Wafer)));
            _tests.Add(("Vision/Inspection", async () => await PingVision(VisionHub.Inspection)));
            _tests.Add(("Vision/Bin", async () => await PingVision(VisionHub.Bin)));

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

            _tests.Add(("BinCodeMap", () =>
            {
                try
                {
                    var d1 = new Die();
                    var d2 = new Die();
                    d2.AddNG("ChippingTopOver");
                    int b1 = BinCodeMap.ConvertToBinCode(d1);
                    int b2 = BinCodeMap.ConvertToBinCode(d2);
                    bool ok = b1 == BinCodeMap.GoodBin && b2 != BinCodeMap.GoodBin;
                    return Task.FromResult((ok, $"good={b1} ng[ChippingTopOver]={b2} codes={(BinCodeMap.Data.Codes?.Count ?? 0)}"));
                }
                catch (Exception ex) { return Task.FromResult((false, ex.Message)); }
            }));

            _tests.Add(("DieMap generator", () =>
            {
                try
                {
                    var f = new DieTapeFrame { ObjId = "TST", DieMapX = 5, DieMapY = 5, PitchX = 1.0, PitchY = 1.0 };
                    var m = DieMapGenerator.Generate(f);
                    bool ok = m != null && m.Entries.Count == 25;
                    return Task.FromResult((ok, $"5x5 entries={m?.Entries?.Count ?? 0}"));
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
                    double[] px = { 0, 100, 0 };
                    double[] py = { 0, 0, 100 };
                    double[] mx = { 0, 1, 0 };
                    double[] my = { 0, 0, 1 };
                    var cm = AlignmentSolver.Solve3Point(px, py, mx, my, out string err);
                    bool ok = cm != null && string.IsNullOrEmpty(err);
                    double mmX = 0, mmY = 0;
                    if (cm != null) cm.ApplyToMotor(50, 50, out mmX, out mmY);
                    return Task.FromResult((ok, ok ? $"100px=1mm (50,50)=>({mmX:F3},{mmY:F3})" : err));
                }
                catch (Exception ex) { return Task.FromResult((false, ex.Message)); }
            }));
        }

        private async Task<(bool, string)> PingVision(VisionTcpClient c)
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
            foreach (var t in _tests)
            {
                int rowIndex = _grid.Rows.Add(t.name, "-", "pending");
                _grid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.White;
            }
        }

        private void ApplyDialogStyle()
        {
            Lang.BindKey(this, "extraDialog.selfTest.title");
            ClientSize = new Size(560, 500);
            BackColor = Color.White;

            rootLayout.Margin = Padding.Empty;
            rootLayout.Padding = Padding.Empty;
            rootLayout.BackColor = Color.White;
            if (rootLayout.RowStyles.Count >= 4)
            {
                rootLayout.RowStyles[0].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[0].Height = 60F;
                rootLayout.RowStyles[2].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[2].Height = 10F;
                rootLayout.RowStyles[3].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[3].Height = 52F;
            }

            Lang.BindKey(lblTitle, "extraDialog.selfTest.title");
            lblTitle.Margin = Padding.Empty;
            lblTitle.Padding = new Padding(18, 0, 18, 0);
            lblTitle.BackColor = Color.FromArgb(38, 50, 66);
            lblTitle.ForeColor = Color.White;
            lblTitle.Font = new Font("Malgun Gothic", 18F, FontStyle.Bold);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;

            _grid.Margin = new Padding(8, 8, 8, 4);
            _grid.BackgroundColor = Color.White;
            _grid.BorderStyle = BorderStyle.FixedSingle;
            _grid.GridColor = Color.FromArgb(214, 219, 226);
            _grid.Font = new Font("Malgun Gothic", 8.5F, FontStyle.Regular);
            _grid.RowTemplate.Height = 22;
            _grid.ColumnHeadersHeight = 26;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(73, 78, 83);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Malgun Gothic", 8.5F, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _grid.ColumnHeadersDefaultCellStyle.Padding = Padding.Empty;
            _grid.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(42, 123, 214);
            _grid.DefaultCellStyle.SelectionForeColor = Color.White;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            colName.FillWeight = 34F;
            colState.FillWeight = 12F;
            colDetail.FillWeight = 54F;
            colState.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _pb.Margin = new Padding(8, 0, 8, 0);

            bottomLayout.Margin = Padding.Empty;
            bottomLayout.Padding = new Padding(8, 6, 8, 10);
            bottomLayout.BackColor = Color.White;
            bottomLayout.ColumnStyles.Clear();
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));

            StyleActionButton(_btnRun, true);
            StyleActionButton(_btnClose, false);
        }

        private static void StyleActionButton(Button button, bool primary)
        {
            if (button == null)
                return;

            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(4, 0, 4, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.UseVisualStyleBackColor = false;
            button.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            button.ForeColor = Color.White;
            button.BackColor = primary ? Color.FromArgb(72, 94, 130) : Color.FromArgb(128, 128, 128);
        }

        private async Task RunAll()
        {
            _btnRun.Enabled = false;
            _pb.Minimum = 0;
            _pb.Maximum = _tests.Count;
            _pb.Value = 0;

            EventLogger.Write(EventKind.Event, "SYS", "SELFTEST", "run start");

            for (int i = 0; i < _tests.Count; i++)
            {
                var row = _grid.Rows[i];
                row.Cells[1].Value = "...";
                try
                {
                    var result = await _tests[i].test();
                    row.Cells[1].Value = result.ok ? "OK" : "NG";
                    row.Cells[2].Value = result.detail ?? "";
                    row.DefaultCellStyle.BackColor = result.ok ? Color.FromArgb(228, 245, 235) : Color.FromArgb(255, 231, 231);
                }
                catch (Exception ex)
                {
                    row.Cells[1].Value = "EX";
                    row.Cells[2].Value = ex.Message;
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 231, 231);
                }
                _pb.Value = i + 1;
            }

            int okCount = _grid.Rows.Cast<DataGridViewRow>().Count(r => (string)r.Cells[1].Value == "OK");
            EventLogger.Write(EventKind.Event, "SYS", "SELFTEST", $"done ok={okCount}/{_tests.Count}");
            _btnRun.Enabled = true;
        }
        private static string TranslateTestDisplay(string value)
        {
            string key;
            switch (value)
            {
                case "AppSettings": key = "extraDialog.selfTest.appSettings"; break;
                case "AjinConfig": key = "extraDialog.selfTest.ajinConfig"; break;
                case "AXL library": key = "extraDialog.selfTest.axl"; break;
                case "Machine tree": key = "extraDialog.selfTest.machine"; break;
                case "Simulator TCP": key = "extraDialog.selfTest.simulator"; break;
                case "Vision/Wafer": key = "extraDialog.selfTest.waferVision"; break;
                case "Vision/Inspection": key = "extraDialog.selfTest.inspectionVision"; break;
                case "Vision/Bin": key = "extraDialog.selfTest.binVision"; break;
                case "Event log writable": key = "extraDialog.selfTest.eventLog"; break;
                case "Recipe dir writable": key = "extraDialog.selfTest.recipeDir"; break;
                case "BinCodeMap": key = "extraDialog.selfTest.binMap"; break;
                case "DieMap generator": key = "extraDialog.selfTest.dieMap"; break;
                case "JobQueue": key = "extraDialog.selfTest.jobQueue"; break;
                case "InterlockRegistry": key = "extraDialog.selfTest.interlock"; break;
                case "AlignmentSolver (3pt)": key = "extraDialog.selfTest.alignment"; break;
                case "pending": key = "extraDialog.selfTest.pending"; break;
                case "running": key = "extraDialog.selfTest.running"; break;
                case "OK": key = "extraDialog.selfTest.passed"; break;
                case "FAIL": key = "extraDialog.selfTest.failed"; break;
                case "disabled": key = "extraDialog.selfTest.disabled"; break;
                case "no bridge": key = "extraDialog.selfTest.noBridge"; break;
                case "connect failed": key = "extraDialog.selfTest.connectFailed"; break;
                case "not connected": key = "extraDialog.selfTest.notConnected"; break;
                case "ping fail": key = "extraDialog.selfTest.pingFailed"; break;
                default: return value;
            }
            return Lang.T(key);
        }

        private void InitializeLanguageBindings()
        {
            Lang.BindKey(colName, "extraDialog.systemSelfTestDialog.colName.caption");
            Lang.BindKey(colState, "extraDialog.systemSelfTestDialog.colState.caption");
            Lang.BindKey(colDetail, "extraDialog.systemSelfTestDialog.colDetail.caption");
            Lang.BindKey(_btnRun, "extraDialog.systemSelfTestDialog._btnRun.caption");
            Lang.BindKey(_btnClose, "extraDialog.systemSelfTestDialog._btnClose.caption");
            Load += (sender, args) => Lang.Apply(this);
        }

    }
}

