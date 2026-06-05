using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Motion;
using QMC.CDT320.Logging;
using QMC.CDT_320.Ui;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// 보드/축 커미셔닝 체크 다이얼로그.
    /// 각 축에 대해: Servo ON → 소량 이동(+1.0 / -1.0 단위) → 원위치 → 알람 확인 → Servo OFF.
    /// 결과를 리스트로 표시 (OK/NG/ALARM).
    /// </summary>
    public class BoardScanDialog : Form
    {
        private readonly List<BaseAxis> _axes;
        private DataGridView _grid;
        private Button _btnStart, _btnCancel;
        private ProgressBar _pb;
        private CancellationTokenSource _cts;

        public BoardScanDialog(IEnumerable<BaseAxis> axes)
        {
            _axes = new List<BaseAxis>(axes ?? Array.Empty<BaseAxis>());

            Text            = "BOARD SCAN TEST";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MinimizeBox     = MaximizeBox = false;
            ClientSize      = new Size(760, 620);
            BackColor       = UiTheme.MainBg;
            ShowIcon        = false;

            Build();
            Seed();
        }

        private void Build()
        {
            var title = new Label
            {
                Dock = DockStyle.Top, Height = 40,
                Text = "BOARD SCAN TEST",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(title);

            var desc = new Label
            {
                Dock = DockStyle.Top, Height = 40, Padding = new Padding(10),
                Text = "각 축에 서보ON → ±0.5mm 소량 이동 → 원위치 복귀 → 알람 확인 순으로 점검합니다.\n전체 완료까지 수십 초 소요 가능. 장비에 간섭 요인이 없을 때만 수행하세요.",
                Font = new Font("맑은 고딕", 9F), ForeColor = Color.DimGray
            };
            Controls.Add(desc);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, Font = new Font("맑은 고딕", 9F),
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50, 0x50, 0x50), ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                },
                RowTemplate = { Height = 24 }
            };
            _grid.Columns.Add("IDX",    "#");
            _grid.Columns.Add("NAME",   "AXIS");
            _grid.Columns.Add("SERVO",  "SERVO");
            _grid.Columns.Add("MOVE",   "MOVE");
            _grid.Columns.Add("ALARM",  "ALARM");
            _grid.Columns.Add("POS",    "POS");
            _grid.Columns.Add("RESULT", "RESULT");
            Controls.Add(_grid);
            Controls.SetChildIndex(_grid, 0);

            _pb = new ProgressBar { Dock = DockStyle.Bottom, Height = 16 };
            Controls.Add(_pb);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = UiTheme.MainBg };
            _btnStart  = new Button { Location = new Point(540, 10), Size = new Size(100, 32), Text = "START",  FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            _btnCancel = new Button { Location = new Point(648, 10), Size = new Size(100, 32), Text = "CLOSE",  FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            _btnStart .Click += async (s, e) => await StartAsync();
            _btnCancel.Click += (s, e) => { _cts?.Cancel(); Close(); };
            bottom.Controls.Add(_btnStart);
            bottom.Controls.Add(_btnCancel);
            Controls.Add(bottom);
        }

        private void Seed()
        {
            for (int i = 0; i < _axes.Count; i++)
                _grid.Rows.Add(i + 1, _axes[i].Name, "-", "-", "-", "-", "PENDING");
        }

        private async Task StartAsync()
        {
            _btnStart.Enabled = false;
            _cts = new CancellationTokenSource();
            _pb.Minimum = 0; _pb.Maximum = _axes.Count; _pb.Value = 0;

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
                row.Cells[4].Value = ax.IsAlarm    ? "ALARM" : "OK";
                if (ax.IsAlarm)
                {
                PaintRow(row, Color.IndianRed); row.Cells[6].Value = "ALARM";
                    return;
                }

                double start = ax.ActualPosition;
                // +0.5 → -0.5 → 원위치
                await ax.MoveRelativeAsync(0.5, velocity: 5.0);
                await Task.Delay(200, ct).ContinueWith(_ => { });
                await ax.MoveRelativeAsync(-1.0, velocity: 5.0);
                await Task.Delay(200, ct).ContinueWith(_ => { });
                await ax.MoveAbsoluteAsync(start, velocity: 5.0);
                await Task.Delay(100, ct).ContinueWith(_ => { });

                double end = ax.ActualPosition;
                row.Cells[3].Value = "±0.5 mm";
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
            finally
            {
                // ⚠ 진단 후 자동 ServoOff 제거 — 정책: 사용자 manual OFF / 비상정지 / 프로그램 종료 시에만 OFF
            }
        }

        private static void PaintRow(DataGridViewRow row, Color bg)
        {
            row.DefaultCellStyle.BackColor = bg;
        }
    }
}
