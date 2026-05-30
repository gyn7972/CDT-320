using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Motion;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class BoardScanDialog : Form
    {
        private readonly List<BaseAxis> _axes;
        private CancellationTokenSource _cts;

        public BoardScanDialog(IEnumerable<BaseAxis> axes)
        {
            _axes = new List<BaseAxis>(axes ?? Array.Empty<BaseAxis>());
            InitializeComponent();
            WireEvents();
            Seed();
        }

        private void WireEvents()
        {
            _btnStart.Click += async (s, e) => await StartAsync();
            _btnCancel.Click += (s, e) => { _cts?.Cancel(); Close(); };
        }

        private void Seed()
        {
            _grid.Rows.Clear();
            for (int i = 0; i < _axes.Count; i++)
                _grid.Rows.Add(i + 1, _axes[i].Name, "-", "-", "-", "-", "PENDING");
        }

        private async Task StartAsync()
        {
            _btnStart.Enabled = false;
            _cts = new CancellationTokenSource();
            _pb.Minimum = 0;
            _pb.Maximum = _axes.Count;
            _pb.Value = 0;

            EventLogger.Write(EventKind.Event, "QMC", "BOARD-SCAN", "start (n=" + _axes.Count + ")");

            for (int i = 0; i < _axes.Count; i++)
            {
                if (_cts.IsCancellationRequested) break;
                await TestOneAsync(i, _axes[i], _cts.Token);
                _pb.Value = i + 1;
            }

            _btnStart.Enabled = true;
            EventLogger.Write(EventKind.Event, "QMC", "BOARD-SCAN", "done");
        }

        private async Task TestOneAsync(int idx, BaseAxis ax, CancellationToken ct)
        {
            var row = _grid.Rows[idx];
            try
            {
                ax.ResetAlarm();
                ax.ServoOn();
                await Task.Delay(150, ct).ContinueWith(_ => { });
                row.Cells[2].Value = ax.IsServoOn ? "ON" : "OFF";
                row.Cells[4].Value = ax.IsAlarm ? "ALARM" : "OK";

                if (ax.IsAlarm)
                {
                    PaintRow(row, Color.IndianRed);
                    row.Cells[6].Value = "ALARM";
                    return;
                }

                double start = ax.ActualPosition;
                await ax.MoveRelativeAsync(0.5, velocity: 5.0);
                await Task.Delay(200, ct).ContinueWith(_ => { });
                await ax.MoveRelativeAsync(-1.0, velocity: 5.0);
                await Task.Delay(200, ct).ContinueWith(_ => { });
                await ax.MoveAbsoluteAsync(start, velocity: 5.0);
                await Task.Delay(100, ct).ContinueWith(_ => { });

                double end = ax.ActualPosition;
                row.Cells[3].Value = "+/-0.5 mm";
                row.Cells[5].Value = end.ToString("F2");
                row.Cells[4].Value = ax.IsAlarm ? "ALARM" : "OK";

                bool ok = !ax.IsAlarm && Math.Abs(end - start) < 0.2;
                row.Cells[6].Value = ok ? "OK" : (ax.IsAlarm ? "ALARM" : "NG");
                PaintRow(row, ok ? Color.LightGreen : (ax.IsAlarm ? Color.IndianRed : Color.Khaki));
            }
            catch (Exception ex)
            {
                row.Cells[6].Value = "EXC: " + ex.Message;
                PaintRow(row, Color.IndianRed);
            }
        }

        private static void PaintRow(DataGridViewRow row, Color bg)
        {
            row.DefaultCellStyle.BackColor = bg;
        }
    }
}

