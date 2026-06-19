using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class LogicDetailPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private static readonly string[] UnitNames =
        {
            "InputLoader",
            "InputStage",
            "TransferPicker(Front)",
            "TransferPicker(Rear)",
            "OutputStage",
            "OutputCassette/Feeder"
        };

        private readonly List<List<TimeBar>> _bars = new List<List<TimeBar>>();
        private readonly DateTime _chartStart = DateTime.Now;
        private Timer _refresh;
        private int _lastDoneSnapshot;

        private struct TimeBar
        {
            public int StartMs;
            public int LenMs;
            public Color Color;
        }

        public LogicDetailPage()
        {
            InitializeComponent();
            InitializeRows();
            WireEvents();

            if (!IsDesignerMode())
            {
                _refresh = new Timer { Interval = 1000 };
                _refresh.Tick += (s, e) =>
                {
                    if (!ShouldRefreshVisible(this))
                        return;

                    RefreshAll();
                };
                _refresh.Start();
                RefreshAll();
            }
        }

        private void InitializeRows()
        {
            for (int i = 0; i < UnitNames.Length; i++)
            {
                _bars.Add(new List<TimeBar>());
                _grid.Rows.Add((i + 1).ToString(), UnitNames[i], "IDLE", "-", "(no data)", "0");
            }
        }

        private void WireEvents()
        {
            _chartHost.Paint += OnPaintChart;
        }

        private void RefreshAll()
        {
            try
            {
                var host = (FindForm() ?? ParentForm) as Form1;
                var ctrl = host?.Controller;
                EquipmentStatus status = ctrl?.Status ?? EquipmentStatus.Idle;
                int done = ctrl?.CycleDone ?? 0;
                int total = ctrl?.CycleTotal ?? 0;
                bool cycling = status == EquipmentStatus.AutoRunning;

                var states = new string[UnitNames.Length];
                var steps = new string[UnitNames.Length];
                var descriptions = new string[UnitNames.Length];
                for (int i = 0; i < UnitNames.Length; i++)
                {
                    if (!cycling)
                    {
                        states[i] = status.ToString().ToUpper();
                        steps[i] = "-";
                        descriptions[i] = status == EquipmentStatus.Initializing ? "Homing..." : "Waiting";
                    }
                    else
                    {
                        int active = done % UnitNames.Length;
                        states[i] = i == active ? "ACTIVE" : "READY";
                        steps[i] = i == active ? StepName(i) : "-";
                        descriptions[i] = i == active
                            ? string.Format("die {0}/{1} processing", done + 1, Math.Max(total, 1))
                            : "Waiting for next cycle";
                    }
                }

                for (int i = 0; i < UnitNames.Length && i < _grid.Rows.Count; i++)
                {
                    long duration = states[i] == "ACTIVE"
                        ? (long)((DateTime.Now - _chartStart).TotalMilliseconds % 5000)
                        : 0;

                    var row = _grid.Rows[i];
                    row.Cells[2].Value = states[i];
                    row.Cells[3].Value = steps[i];
                    row.Cells[4].Value = descriptions[i];
                    row.Cells[5].Value = duration.ToString();
                    row.DefaultCellStyle.BackColor = states[i] == "ACTIVE"
                        ? Color.FromArgb(0xCC, 0xF2, 0xDD)
                        : states[i] == "ALARM" ? Color.FromArgb(0xFF, 0xCC, 0xCC) : Color.White;
                }

                if (done > _lastDoneSnapshot)
                {
                    int delta = done - _lastDoneSnapshot;
                    int now = (int)((DateTime.Now - _chartStart).TotalMilliseconds);
                    for (int k = 0; k < delta; k++)
                    {
                        int t = now + k * 30;
                        for (int unit = 0; unit < UnitNames.Length; unit++)
                        {
                            int width = 200 + unit * 50;
                            _bars[unit].Add(new TimeBar
                            {
                                StartMs = t + unit * width / 6,
                                LenMs = width / 2,
                                Color = ColorForUnit(unit)
                            });
                        }
                    }

                    _lastDoneSnapshot = done;
                }

                _chartHost.Invalidate();
            }
            catch
            {
            }
        }

        private static string StepName(int unitIndex)
        {
            switch (unitIndex)
            {
                // FEED 단계명
                case 0: return "FEED";
                // ALIGN 단계명
                case 1: return "ALIGN";
                // PICK 단계명
                case 2: return "PICK";
                // INSPECT 단계명
                case 3: return "INSPECT";
                // PLACE 단계명
                case 4: return "PLACE";
                // STORE 단계명
                case 5: return "STORE";
                default: return "-";
            }
        }

        private static Color ColorForUnit(int unitIndex)
        {
            Color[] colors =
            {
                Color.FromArgb(0x58, 0xD9, 0xA8),
                Color.FromArgb(0x58, 0xC0, 0xD9),
                Color.FromArgb(0xD9, 0xA8, 0x58),
                Color.FromArgb(0xD9, 0x58, 0xA8),
                Color.FromArgb(0x58, 0x88, 0xD9),
                Color.FromArgb(0x88, 0x58, 0xD9),
            };
            return colors[unitIndex % colors.Length];
        }

        private void OnPaintChart(object sender, PaintEventArgs e)
        {
            int width = _chartHost.Width;
            int height = _chartHost.Height;
            var graphics = e.Graphics;

            using (var gridPen = new Pen(Color.LightGray))
            {
                for (int y = 0; y < height; y += 40) graphics.DrawLine(gridPen, 0, y, width, y);
                for (int x = 0; x < width; x += 80) graphics.DrawLine(gridPen, x, 0, x, height);
            }

            int totalMs = (int)((DateTime.Now - _chartStart).TotalMilliseconds);
            int axisMs = Math.Max(20000, Math.Max(1000, totalMs));
            for (int unit = 0; unit < _bars.Count; unit++)
            {
                int yRow = 20 + unit * 40;
                using (var brush = new SolidBrush(ColorForUnit(unit)))
                {
                    foreach (var bar in _bars[unit])
                    {
                        int x = (int)(bar.StartMs / (double)axisMs * width);
                        int barWidth = Math.Max(2, (int)(bar.LenMs / (double)axisMs * width));
                        if (x > width) x %= Math.Max(width, 1);
                        graphics.FillRectangle(brush, x, yRow, barWidth, 20);
                    }
                }
            }

            using (var font = new Font("Segoe UI", 9F))
            {
                for (int i = 0; i < UnitNames.Length; i++)
                    graphics.DrawString(UnitNames[i], font, Brushes.Black, 4, 20 + i * 40);

                using (var brush = new SolidBrush(Color.DimGray))
                    graphics.DrawString("axis = " + (axisMs / 1000) + "s", font, brush, width - 100, height - 18);
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                _refresh?.Stop();
                _refresh?.Dispose();
            }
            catch { }

            base.OnHandleDestroyed(e);
        }
    }
}

