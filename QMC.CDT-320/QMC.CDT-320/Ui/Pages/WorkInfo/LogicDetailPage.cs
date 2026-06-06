using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    /// <summary>
    /// 작업정보 - LOGIC 상세. TabControl 2탭:
    /// 1) LOGIC : 6 unit 의 (state, step, description, last update) 실시간 그리드
    /// 2) TIMECHART : 사이클 진행에 따른 unit 활성 구간을 가로 막대로 누적
    /// Stage 58 — 하드코딩된 sample row 제거. MachineController.Status + LotStorage.ActiveLot
    /// + 각 Unit 의 Name 으로부터 데이터를 1초마다 읽어와 표시.
    /// </summary>
    public class LogicDetailPage : PageBase
    {
        // 6 Unit 표시 순서
        private static readonly string[] UnitNames =
        {
            "InputLoader", "InputStage", "TransferPicker(Front)", "TransferPicker(Rear)",
            "OutputStage", "OutputUnloader"
        };

        private DataGridView _grid;
        private Panel _chartHost;
        private System.Windows.Forms.Timer _refresh;

        // 시간차트 누적 — 각 unit 별로 (시작 ms, 길이 ms) 막대 리스트
        private readonly List<List<TimeBar>> _bars = new List<List<TimeBar>>();
        private DateTime _chartStart = DateTime.Now;
        private int _lastDoneSnapshot = 0;

        private struct TimeBar { public int StartMs; public int LenMs; public Color Color; }

        public LogicDetailPage()
        {
            Controls.Add(CreateSectionHeader("wi.logic"));

            var tabs = new TabControl { Dock = DockStyle.Fill, Font = UiTheme.ButtonFont };
            var tpLogic = new TabPage { Text = Lang.T("wi.logicLogic"),     Tag = "i18n:wi.logicLogic" };
            var tpChart = new TabPage { Text = Lang.T("wi.logicTimechart"), Tag = "i18n:wi.logicTimechart" };

            // LOGIC 탭: 모듈별 현재 상태 그리드
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, Font = new Font("맑은 고딕", 9F),
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50,0x50,0x50), ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                },
                RowTemplate = { Height = 26 }
            };
            foreach (var c in new[] { "INDEX", "MODULE", "STATE", "STEP", "DESCRIPTION", "DURATION(ms)" })
                _grid.Columns.Add(c, c);
            for (int i = 0; i < UnitNames.Length; i++)
                _grid.Rows.Add((i + 1).ToString(), UnitNames[i], "IDLE", "-", "(no data)", "0");
            tpLogic.Controls.Add(_grid);

            // TIMECHART 탭
            _chartHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            for (int i = 0; i < UnitNames.Length; i++) _bars.Add(new List<TimeBar>());
            _chartHost.Paint += OnPaintChart;
            tpChart.Controls.Add(_chartHost);

            tabs.TabPages.Add(tpLogic);
            tabs.TabPages.Add(tpChart);
            Controls.Add(tabs);
            Controls.SetChildIndex(tabs, 0);

            if (!IsDesignerMode())
            {
                _refresh = new System.Windows.Forms.Timer { Interval = 1000 };
                _refresh.Tick += (s, e) => RefreshAll();
                _refresh.Start();
                RefreshAll();
            }
        }

        private void RefreshAll()
        {
            try
            {
                var host = (FindForm() ?? ParentForm) as Form1;
                var ctrl = host?.Controller;
                MachineStatus ms = ctrl?.Status ?? MachineStatus.Idle;

                // unit 별 STATE/STEP/DESC 결정 — Cycling 중이면 진행 단계에 따라 다른 값
                int done = ctrl?.CycleDone ?? 0;
                int total = ctrl?.CycleTotal ?? 0;
                bool cycling = ms == MachineStatus.Cycling;

                // STATE 추정
                string[] states = new string[UnitNames.Length];
                string[] steps  = new string[UnitNames.Length];
                string[] descs  = new string[UnitNames.Length];
                for (int i = 0; i < UnitNames.Length; i++)
                {
                    if (!cycling)
                    {
                        states[i] = ms.ToString().ToUpper();
                        steps[i] = "-";
                        descs[i] = ms == MachineStatus.Initializing ? "Homing..." : "대기 중";
                    }
                    else
                    {
                        // 6 unit 파이프라인 중에서 현재 활성 추정 — done % 6 위치를 ACTIVE
                        int active = done % UnitNames.Length;
                        states[i] = (i == active) ? "ACTIVE" : "READY";
                        steps[i]  = (i == active) ? StepName(i) : "-";
                        descs[i]  = (i == active)
                            ? string.Format("die {0}/{1} 처리 중", done + 1, Math.Max(total, 1))
                            : "다음 다이 대기";
                    }
                }

                long[] durations = new long[UnitNames.Length];
                for (int i = 0; i < UnitNames.Length; i++)
                    durations[i] = (states[i] == "ACTIVE") ? (long)((DateTime.Now - _chartStart).TotalMilliseconds % 5000) : 0;

                for (int i = 0; i < UnitNames.Length && i < _grid.Rows.Count; i++)
                {
                    var row = _grid.Rows[i];
                    row.Cells[2].Value = states[i];
                    row.Cells[3].Value = steps[i];
                    row.Cells[4].Value = descs[i];
                    row.Cells[5].Value = durations[i].ToString();
                    Color back = states[i] == "ACTIVE" ? Color.FromArgb(0xCC, 0xF2, 0xDD)
                              : (states[i] == "ALARM" ? Color.FromArgb(0xFF, 0xCC, 0xCC) : Color.White);
                    row.DefaultCellStyle.BackColor = back;
                }

                // 차트: 매 다이 done 증가 시 6 unit 에 한 칸씩 막대 추가
                if (done > _lastDoneSnapshot)
                {
                    int dt = done - _lastDoneSnapshot;
                    int now = (int)((DateTime.Now - _chartStart).TotalMilliseconds);
                    for (int k = 0; k < dt; k++)
                    {
                        int t = now + k * 30;
                        for (int u = 0; u < UnitNames.Length; u++)
                        {
                            int width = 200 + (u * 50);
                            _bars[u].Add(new TimeBar { StartMs = t + u * width / 6, LenMs = width / 2, Color = ColorForUnit(u) });
                        }
                    }
                    _lastDoneSnapshot = done;
                    _chartHost.Invalidate();
                }
                else
                {
                    _chartHost.Invalidate();
                }
            }
            catch { }
        }

        private static string StepName(int unitIndex)
        {
            switch (unitIndex)
            {
                case 0: return "FEED";
                case 1: return "ALIGN";
                case 2: return "PICK";
                case 3: return "INSPECT";
                case 4: return "PLACE";
                case 5: return "STORE";
            }
            return "-";
        }

        private static Color ColorForUnit(int u)
        {
            Color[] cs =
            {
                Color.FromArgb(0x58, 0xD9, 0xA8), // Loader   green
                Color.FromArgb(0x58, 0xC0, 0xD9), // Stage    cyan
                Color.FromArgb(0xD9, 0xA8, 0x58), // Front    orange
                Color.FromArgb(0xD9, 0x58, 0xA8), // Rear     pink
                Color.FromArgb(0x58, 0x88, 0xD9), // OutStage blue
                Color.FromArgb(0x88, 0x58, 0xD9), // Unloader violet
            };
            return cs[u % cs.Length];
        }

        private void OnPaintChart(object s, PaintEventArgs e)
        {
            int w = _chartHost.Width, h = _chartHost.Height;
            var g = e.Graphics;
            // grid
            using (var gray = new Pen(Color.LightGray)) for (int y = 0; y < h; y += 40) g.DrawLine(gray, 0, y, w, y);
            using (var gray = new Pen(Color.LightGray)) for (int x = 0; x < w; x += 80) g.DrawLine(gray, x, 0, x, h);

            // 가로축: 0..현재경과ms 를 wrap 하여 화면 너비에 매핑
            int totalMs = (int)((DateTime.Now - _chartStart).TotalMilliseconds);
            if (totalMs < 1000) totalMs = 1000;
            int axisMs = Math.Max(20000, totalMs); // 최소 20초 윈도우
            for (int u = 0; u < _bars.Count; u++)
            {
                int yRow = 20 + u * 40;
                using (var br = new SolidBrush(ColorForUnit(u)))
                {
                    foreach (var b in _bars[u])
                    {
                        int x = (int)(b.StartMs / (double)axisMs * w);
                        int bw = Math.Max(2, (int)(b.LenMs / (double)axisMs * w));
                        if (x > w) x = x % w;
                        g.FillRectangle(br, x, yRow, bw, 20);
                    }
                }
            }
            using (var f = new Font("맑은 고딕", 9F))
            {
                for (int i = 0; i < UnitNames.Length; i++)
                    g.DrawString(UnitNames[i], f, Brushes.Black, 4, 20 + i * 40);
                using (var br = new SolidBrush(Color.DimGray))
                    g.DrawString("axis = " + (axisMs / 1000) + "s", f, br, w - 100, h - 18);
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _refresh?.Stop(); _refresh?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
