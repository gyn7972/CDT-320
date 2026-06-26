using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using QMC.Vision.Core;       // ResourceMonitor
using QMC.Vision.Modules;
using QMC.Vision.Sequencing;
using QMC.Vision.Ui.Dialogs; // LoadChartDialog

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 시퀀서 개별 테스트 페이지 — 모듈 선택(전체/개별) + 모드(Auto/Step) + 시작/정지/한단계 + 실시간 로그.
    /// 실행은 Form1.AutoSeq(VisionAutoSequenceHost) 에 위임. 로그는 Host.Message 구독.
    /// </summary>
    public partial class SequencerPage : PageBase
    {
        // 콤보 인덱스 ↔ 모듈 종류
        private static readonly SequenceModuleKind[] _kinds =
        {
            SequenceModuleKind.All,
            SequenceModuleKind.WaferVision,
            SequenceModuleKind.BinVision,
            SequenceModuleKind.BottomInspection,
            SequenceModuleKind.TopSideVision,    // 앞쪽 측면 (각각)
            SequenceModuleKind.BottomSideVision, // 뒤쪽 측면 (각각)
            SequenceModuleKind.SideVision        // 측면 앞+뒤 동시(병렬) — 합성 비트마스크
        };

        private bool _subscribed;

        // 그랩 FPS 계산용(ViewerFrameSeq 델타)
        private readonly Dictionary<SequenceModuleKind, long>     _fpsPrev = new Dictionary<SequenceModuleKind, long>();
        private readonly Dictionary<SequenceModuleKind, DateTime> _fpsTime = new Dictionary<SequenceModuleKind, DateTime>();
        private Timer _metricTimer;

        // CPU/GPU/메모리 부하 체크(누적) — [경과초, CPU%, MEM MB, GPU%]
        private readonly ResourceMonitor _res = new ResourceMonitor();
        private bool _profiling;
        private DateTime _profStart;
        private readonly List<double[]> _profSamples = new List<double[]>();

        // 메트릭 그리드 행에 표시할 모듈(전체 제외)
        private static readonly SequenceModuleKind[] _metricKinds =
        {
            SequenceModuleKind.WaferVision, SequenceModuleKind.BinVision,
            SequenceModuleKind.BottomInspection, SequenceModuleKind.TopSideVision, SequenceModuleKind.BottomSideVision
        };
        private static readonly string[] _metricNames =
        { "웨이퍼", "빈", "바텀", "앞측면", "뒤측면" };

        public SequencerPage()
        {
            InitializeComponent();
            // __COLLAPSIBLE_WRAP__
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                QMC.Vision.Ui.Controls.CollapsibleGrids.Wrap(this._metrics, "METRICS");
            }
            if (IsDesignerMode()) return;

            _cbModule.Items.AddRange(new object[]
            { "전체", "웨이퍼 비전", "빈 비전", "바텀 검사", "앞쪽 측면", "뒤쪽 측면", "측면(앞+뒤 동시)" });
            _cbModule.SelectedIndex = 0;
            _cbModule.SelectedIndexChanged += (s, e) => { PopulateTools(); BuildMetricsGrid(); };

            _cbMode.Items.AddRange(new object[] { "Auto (연속)", "Step (수동)" });
            _cbMode.SelectedIndex = 0;

            // METRICS 그리드를 도구 다중선택으로 — 행(도구) 여러 개 선택 → 동시 실행(Ctrl/Shift). 체크박스 불필요.
            _metrics.MultiSelect = true;
            _metrics.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // 도구 드롭다운으로 특정 도구를 고르면 그리드 기본선택을 해제 → 드롭다운 선택이 시작에 반영되게(그리드 우선 충돌 방지).
            _cbTool.SelectedIndexChanged += (s, e) =>
            {
                if (SelectedToolId() != null)
                    try { _metrics.ClearSelection(); _metrics.CurrentCell = null; } catch { }
            };

            PopulateTools();
            BuildMetricsGrid();
            _btnLoadStop.Enabled = false;   // 부하 체크 시작 전엔 완료 비활성
        }

        private const string AllToolsLabel = "(전체 도구)";

        /// <summary>METRICS 그리드에서 선택된 도구들(모듈+id). 미선택이면 빈 목록.</summary>
        private System.Collections.Generic.List<ToolKey> SelectedTools()
        {
            var list = new System.Collections.Generic.List<ToolKey>();
            if (_metrics == null) return list;
            foreach (DataGridViewRow row in _metrics.SelectedRows)
                if (row?.Tag is ToolKey tk) list.Add(tk);
            list.Reverse();   // SelectedRows 는 역순 → 화면 순서대로
            return list;
        }

        /// <summary>선택 모듈의 도구 목록을 도구 콤보에 채운다. '전체' 모듈은 (전체 도구)만.</summary>
        private void PopulateTools()
        {
            _cbTool.Items.Clear();
            _cbTool.Items.Add(AllToolsLabel);
            var kind = SelectedKind();
            if (kind != SequenceModuleKind.All && SequenceToolCatalog.Has(kind))
                foreach (var t in SequenceToolCatalog.Tools(kind))
                    _cbTool.Items.Add((t.Key == "MATCH" ? "[F] " : "[I] ") + t.Value);
            _cbTool.SelectedIndex = 0;
        }

        /// <summary>선택된 도구 Id(접두사 제거). (전체 도구) 또는 미선택이면 null.</summary>
        private string SelectedToolId()
        {
            int i = _cbTool.SelectedIndex;
            if (i <= 0) return null;
            string s = _cbTool.SelectedItem as string;
            if (string.IsNullOrEmpty(s)) return null;
            if (s.StartsWith("[F] ") || s.StartsWith("[I] ")) s = s.Substring(4);
            return s;
        }

        // 메트릭 그리드 행 식별(모듈+도구).
        private sealed class ToolKey { public SequenceModuleKind Kind; public string Id; }

        // 선택 모듈에 따라 도구별 행을 구성한다('전체'면 전 모듈 도구).
        private void BuildMetricsGrid()
        {
            _metrics.Columns.Clear();
            _metrics.Rows.Clear();
            _metrics.Columns.Add("mod",   "모듈");
            _metrics.Columns.Add("tool",  "도구");
            _metrics.Columns.Add("kind",  "종류");
            _metrics.Columns.Add("res",   "판정");
            _metrics.Columns.Add("cycle", "사이클(ms)");
            _metrics.Columns.Add("count", "사이클수");

            var sel = SelectedKind();
            for (int i = 0; i < _metricKinds.Length; i++)
            {
                var kind = _metricKinds[i];
                // 합성 선택(측면 동시 등)도 비트마스크로 포함 판정 — All 이면 전부.
                if (sel != SequenceModuleKind.All && (sel & kind) == 0) continue;
                if (!SequenceToolCatalog.Has(kind)) continue;
                foreach (var t in SequenceToolCatalog.Tools(kind))
                {
                    int r = _metrics.Rows.Add(_metricNames[i], t.Value,
                                              t.Key == "MATCH" ? "Finder" : "Inspector", "-", "-", "0");
                    _metrics.Rows[r].Tag = new ToolKey { Kind = kind, Id = t.Value };
                }
            }
            // 기본 행 자동선택 해제 — 미선택 상태에선 도구 드롭다운(또는 모듈 전체)이 시작을 주도.
            // (행을 직접 클릭/Ctrl·Shift 선택해야 '선택 도구 동시 실행'이 동작.)
            try { _metrics.ClearSelection(); _metrics.CurrentCell = null; } catch { }
        }

        private QMC.Vision.Form1 Host => FindForm() as QMC.Vision.Form1;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var host = Host;
            if (host?.AutoSeq != null && !_subscribed)
            {
                host.AutoSeq.Message += OnSeqMessage;
                _subscribed = true;
            }
            if (_metricTimer == null)
            {
                _metricTimer = new Timer { Interval = 700 };
                _metricTimer.Tick += (s, ev) => OnMetricTick();
            }
            _metricTimer.Start();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _metricTimer?.Stop(); } catch { }
            var host = Host;
            if (host?.AutoSeq != null && _subscribed)
            {
                host.AutoSeq.Message -= OnSeqMessage;
                _subscribed = false;
            }
            base.OnHandleDestroyed(e);
        }

        // ── 메트릭(사이클 ms / 그랩 FPS / 사이클수) 갱신 ──
        private IVisionModule ModuleOf(SequenceModuleKind k)
        {
            var h = Host; if (h == null) return null;
            switch (k)
            {
                case SequenceModuleKind.WaferVision:      return h.WaferMod;
                case SequenceModuleKind.BinVision:        return h.BinMod;
                case SequenceModuleKind.BottomInspection: return h.BottomMod;
                case SequenceModuleKind.TopSideVision:    return h.TopSideVisionMod;
                case SequenceModuleKind.BottomSideVision: return h.BottomSideVisionMod;
                default: return null;
            }
        }

        private void RefreshMetrics()
        {
            var h = Host; if (h?.AutoSeq == null) return;
            var snap = h.AutoSeq.Metrics();
            foreach (DataGridViewRow row in _metrics.Rows)
            {
                if (!(row.Tag is ToolKey key)) continue;
                double cycleMs = 0; long cycles = 0; string status = "-";
                foreach (var m in snap)
                    if (m.Kind == key.Kind && m.ToolId == key.Id)
                    { cycleMs = m.CycleMs; cycles = m.Cycles; status = m.Status; break; }
                row.Cells[3].Value = status;
                row.Cells[4].Value = cycleMs > 0 ? cycleMs.ToString("F0") : "-";
                row.Cells[5].Value = cycles.ToString();
                // 판정 색
                row.Cells[3].Style.ForeColor =
                    status == "OK"  ? System.Drawing.Color.SeaGreen :
                    status == "NG"  ? System.Drawing.Color.Firebrick :
                                      System.Drawing.Color.DimGray;
            }
        }

        private double ComputeFps(SequenceModuleKind kind)
        {
            var mod = ModuleOf(kind);
            if (mod == null) return 0;
            long cur = mod.ViewerFrameSeq;
            var now = DateTime.UtcNow;
            double fps = 0;
            if (_fpsPrev.TryGetValue(kind, out long prev) && _fpsTime.TryGetValue(kind, out DateTime pt))
            {
                double dt = (now - pt).TotalSeconds;
                if (dt > 0.05) fps = (cur - prev) / dt;
            }
            _fpsPrev[kind] = cur; _fpsTime[kind] = now;
            return fps < 0 ? 0 : fps;
        }

        private SequenceModuleKind SelectedKind()
        {
            int i = _cbModule.SelectedIndex;
            return (i >= 0 && i < _kinds.Length) ? _kinds[i] : SequenceModuleKind.All;
        }

        private SequenceRunMode SelectedMode()
            => _cbMode.SelectedIndex == 1 ? SequenceRunMode.Manual : SequenceRunMode.Auto;

        private static int Interval()
            => QMC.Vision.Config.VisionConfigStore.Current?.SimSequenceIntervalMs ?? 500;

        private void OnStartClick(object sender, EventArgs e)
        {
            var host = Host; if (host?.AutoSeq == null) { Append("[UI] 호스트 없음"); return; }
            var mode = SelectedMode();

            // 1) METRICS 그리드에서 도구를 선택했으면 — 한 개든 여러 개든 동시 실행(병렬).
            var tools = SelectedTools();
            if (tools.Count > 0)
            {
                var pairs = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<SequenceModuleKind, string>>();
                foreach (var tk in tools) pairs.Add(new System.Collections.Generic.KeyValuePair<SequenceModuleKind, string>(tk.Kind, tk.Id));
                host.AutoSeq.StartTools(pairs, mode, Interval());
                Append("[UI] 선택 도구 동시 실행 — " + string.Join(", ", tools.ConvertAll(t => t.Id)));
                return;
            }

            // 2) 도구 미선택 — 드롭다운 기준(모듈 전체 또는 도구콤보 하나).
            var kind = SelectedKind();
            string toolId = SelectedToolId();
            if (toolId == null)
                host.AutoSeq.StartModules(kind, mode, Interval());           // 모듈 전체(모든 도구)
            else
                host.AutoSeq.StartTool(kind, toolId, mode, Interval());      // 도구 하나만
        }

        private void OnStopClick(object sender, EventArgs e)
            => Host?.AutoSeq?.Stop();

        private void OnStepClick(object sender, EventArgs e)
        {
            var host = Host; if (host?.AutoSeq == null) return;
            var kind = SelectedKind();
            string toolId = SelectedToolId();
            if (toolId == null)
            {
                // 도구 미선택 — 모듈 순서상 '다음 도구'를 한 단계.
                // 단일 모듈만 가능(전체·측면 동시 등 합성 선택은 도구 순서 정의가 없으므로 모듈 지정 필요).
                if (!SequenceToolCatalog.Has(kind))
                { Append("[UI] '한 단계'는 단일 모듈을 선택하세요(전체·측면 동시는 도구 순서 진행 불가)."); return; }
                host.AutoSeq.StepNextTool(kind, Interval());
            }
            else
            {
                host.AutoSeq.StepTool(kind, toolId, Interval());            // 선택 도구 1회
            }
        }

        private void OnClearClick(object sender, EventArgs e) => _log.Clear();

        // ── CPU/메모리 부하 체크(누적) ──
        private void OnMetricTick()
        {
            RefreshMetrics();
            _res.Sample();
            if (_profiling)
                _profSamples.Add(new[] { (DateTime.Now - _profStart).TotalSeconds, _res.CpuPercent, _res.WorkingSetMB, _res.GpuPercent });
        }

        private void OnLoadCheckStartClick(object sender, EventArgs e)
        {
            _profSamples.Clear();
            _res.ResetPeaks();
            _res.Sample();                 // 기준점
            _profStart = DateTime.Now;
            _profiling = true;
            _btnLoadStart.Enabled = false;
            _btnLoadStop.Enabled  = true;
            Append("[부하] 체크 시작 — 시퀀서를 진행하세요. 완료 시 [체크 완료]로 차트 확인.");
        }

        private void OnLoadCheckStopClick(object sender, EventArgs e)
        {
            _profiling = false;
            _btnLoadStart.Enabled = true;
            _btnLoadStop.Enabled  = false;
            Append($"[부하] 체크 완료 — 샘플 {_profSamples.Count}개.");
            if (_profSamples.Count < 2)
            {
                MessageBox.Show(this, "수집된 데이터가 부족합니다. 시작 후 시퀀서를 잠시 진행한 뒤 완료하세요.",
                    "부하 체크", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new LoadChartDialog(new List<double[]>(_profSamples)))
                dlg.ShowDialog(this);
        }

        // ── 로그 ──
        private void OnSeqMessage(string msg)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((Action<string>)Append, msg); } catch { } return; }
            Append(msg);
        }

        private void Append(string msg)
        {
            try
            {
                string line = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg + Environment.NewLine;
                if (_log.TextLength > 60000) _log.Clear();   // 과대 방지
                _log.AppendText(line);
            }
            catch { }
        }
    }
}
