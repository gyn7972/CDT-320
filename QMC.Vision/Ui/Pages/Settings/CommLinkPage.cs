using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Comm;
using QMC.Vision.Config;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 설정 - 통신(TCP) 페이지. 핸들러 SettingsTab 의 VisionLinkPage 와 대칭.
    /// <para>토폴로지: Vision = TCP 서버(listen), 핸들러 = TCP 클라이언트(접속).</para>
    /// 6 채널(Wafer/Inspection/Bin/Main/TopSide/BottomSide) 서버 포트 편집·저장 +
    /// 1초 주기 상태 램프(접속) + 최근 수신 경과(워치독) + 통신 로그(TX/RX/EPD/ARM) 표시.
    /// TCP 포트/상태는 DataGridView 로 표시(포트=편집 셀, 상태/RX=색상 셀).
    /// </summary>
    public partial class CommLinkPage : PageBase
    {
        /// <summary>접속돼 있는데 이 시간(초) 이상 무통신이면 RX 경과를 경고색으로 표시.</summary>
        private const double StaleSeconds = 30.0;

        private const int RowCount = 6;
        private const int MainRow = 3;   // Main Comm — 뷰어(영상) 없음

        // 그리드 행 순서(상태 st 인덱스와 동일).
        private static readonly string[] ChannelNames =
            { "Wafer Vision", "Bottom Inspection", "Bin Vision", "Main Comm", "Top Side Vision", "Bottom Side Vision" };

        // 뷰어 상태(vt) 인덱스 → 그리드 행 매핑(Wafer/Insp/Bin/Top/Bot — Main 없음).
        private static readonly int[] ViewerRowMap = { 0, 1, 2, 4, 5 };

        private System.Windows.Forms.Timer _timer;
        private long _lastLogRev = -1;

        public CommLinkPage()
        {
            InitializeComponent();
            if (IsDesignerMode()) return;

            // 다른 페이지와 동일한 그리드 룩 + 접기 헤더(주황 라인) 적용.
            GridTheme.Apply(_gridPorts);
            // 인라인 텍스트 편집 대신 셀 클릭 시 숫자 키패드로 입력받는다.
            _gridPorts.ReadOnly = true;
            colCmdPort.DefaultCellStyle.BackColor = Color.White;
            colViewPort.DefaultCellStyle.BackColor = Color.White;
            CollapsibleGrids.Wrap(this._gridPorts, "TCP 포트 / 상태 (Vision = 서버 · 포트 변경은 재시작 후 반영)");

            LoadPorts();
            WireEvents();

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) => { RefreshStatus(); RefreshLog(); };
            _timer.Start();
            RefreshStatus();
            RefreshLog();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }

        private void WireEvents()
        {
            _btnSave.Click += OnSaveClick;
            _btnLoad.Click += OnLoadClick;
            _btnClearLog.Click += (s, e) => { VisionCommLog.Clear(); _lastLogRev = -1; RefreshLog(); };
            _gridPorts.CellClick += OnGridCellClick;
        }

        // 포트 셀(명령/뷰어) 클릭 시 숫자 키패드로 입력. Main 행 뷰어는 제외.
        private void OnGridCellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (e.ColumnIndex != colCmdPort.Index && e.ColumnIndex != colViewPort.Index) return;
                if (e.ColumnIndex == colViewPort.Index && e.RowIndex == MainRow) return;

                var cell = _gridPorts.Rows[e.RowIndex].Cells[e.ColumnIndex];
                string channel = _gridPorts.Rows[e.RowIndex].Cells[colChannel.Index].Value?.ToString() ?? string.Empty;
                string title = (channel + " " + _gridPorts.Columns[e.ColumnIndex].HeaderText).Trim();
                string current = cell.Value?.ToString() ?? string.Empty;

                using (var dlg = new NumericKeypadDialog(title, current, ""))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    string text = dlg.ValueText;
                    if (double.TryParse(text, out var d))
                    {
                        int p = (int)d;
                        if (p > 0 && p < 65536) cell.Value = p.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[COMMLINK] keypad failed: " + ex.Message);
            }
        }

        // 그리드 6행을 1회 생성(채널명 + Main 뷰어 비활성).
        private void EnsureRows()
        {
            try
            {
                if (_gridPorts.Rows.Count >= RowCount) return;
                _gridPorts.Rows.Clear();
                _gridPorts.Rows.Add(RowCount);
                for (int i = 0; i < RowCount; i++)
                    _gridPorts.Rows[i].Cells[colChannel.Index].Value = ChannelNames[i];

                // Main 행은 뷰어 포트/상태 없음 — 편집 불가 + 회색 처리.
                var mv = _gridPorts.Rows[MainRow].Cells[colViewPort.Index];
                mv.ReadOnly = true;
                mv.Value = string.Empty;
                mv.Style.BackColor = Color.FromArgb(238, 238, 238);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[COMMLINK] EnsureRows failed: " + ex.Message);
            }
        }

        private void LoadPorts()
        {
            EnsureRows();
            var cfg = VisionConfigStore.Current;
            int[] cmd =
            {
                cfg.WaferVisionPort, cfg.InspectionVisionPort, cfg.BinVisionPort,
                cfg.MainCommPort, cfg.TopSideVisionPort, cfg.BottomSideVisionPort
            };
            int[] view =
            {
                cfg.WaferViewerPort, cfg.InspectionViewerPort, cfg.BinViewerPort,
                0, cfg.FrontSideViewerPort, cfg.RearSideViewerPort
            };
            for (int i = 0; i < RowCount; i++)
            {
                _gridPorts.Rows[i].Cells[colCmdPort.Index].Value = cmd[i].ToString();
                if (i != MainRow)
                    _gridPorts.Rows[i].Cells[colViewPort.Index].Value = view[i].ToString();
            }
        }

        // ── 저장 — 6 포트를 vision.json 에 기록(재시작 후 반영) ──
        private void OnSaveClick(object sender, EventArgs e)
        {
            try
            {
                EnsureRows();
                var cfg = VisionConfigStore.Current;
                cfg.WaferVisionPort      = ParseCell(0, colCmdPort, cfg.WaferVisionPort);
                cfg.InspectionVisionPort = ParseCell(1, colCmdPort, cfg.InspectionVisionPort);
                cfg.BinVisionPort        = ParseCell(2, colCmdPort, cfg.BinVisionPort);
                cfg.MainCommPort         = ParseCell(3, colCmdPort, cfg.MainCommPort);
                cfg.TopSideVisionPort    = ParseCell(4, colCmdPort, cfg.TopSideVisionPort);
                cfg.BottomSideVisionPort = ParseCell(5, colCmdPort, cfg.BottomSideVisionPort);
                // 뷰어 포트(5200대) — Main 제외
                cfg.WaferViewerPort      = ParseCell(0, colViewPort, cfg.WaferViewerPort);
                cfg.InspectionViewerPort = ParseCell(1, colViewPort, cfg.InspectionViewerPort);
                cfg.BinViewerPort        = ParseCell(2, colViewPort, cfg.BinViewerPort);
                cfg.FrontSideViewerPort  = ParseCell(4, colViewPort, cfg.FrontSideViewerPort);
                cfg.RearSideViewerPort   = ParseCell(5, colViewPort, cfg.RearSideViewerPort);
                VisionConfigStore.Save();
                LoadPorts();
                MessageBox.Show("저장되었습니다.\n(포트 변경은 재시작 후 반영)",
                    "통신 설정", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("저장 실패: " + ex.Message, "통신 설정",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnLoadClick(object sender, EventArgs e)
        {
            try
            {
                VisionConfigStore.Load();
                LoadPorts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("불러오기 실패: " + ex.Message, "통신 설정",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private int ParseCell(int row, DataGridViewColumn col, int fallback)
        {
            try
            {
                var v = _gridPorts.Rows[row].Cells[col.Index].Value?.ToString();
                return int.TryParse(v, out var p) && p > 0 && p < 65536 ? p : fallback;
            }
            catch { return fallback; }
        }

        // ── 상태 셀 + 최근 수신(워치독) 갱신(1초 주기) ──
        private void RefreshStatus()
        {
            if (IsDisposed) return;
            EnsureRows();

            List<Form1.CommChannelStatus> st = null;
            try { st = (FindForm() as Form1)?.GetVisionCommStatus(); } catch { }

            if (st == null)
            {
                for (int i = 0; i < RowCount; i++) { SetCmdStatus(i, false, false, 0); SetRx(i, default(DateTime), false); }
                for (int i = 0; i < RowCount; i++) SetViewerStatus(i, false, 0, 0);
                return;
            }

            int n = Math.Min(RowCount, st.Count);
            for (int i = 0; i < n; i++)
            {
                SetCmdStatus(i, st[i].Listening, st[i].Connected, st[i].Port);
                SetRx(i, st[i].LastRxUtc, st[i].Connected);
            }

            // 뷰어(영상 스트림) 상태 — Wafer/Insp/Bin/Top/Bot (Main 없음).
            List<Form1.ViewerChannelStatus> vt = null;
            try { vt = (FindForm() as Form1)?.GetVisionViewerStatus(); } catch { }
            for (int k = 0; k < ViewerRowMap.Length; k++)
            {
                int row = ViewerRowMap[k];
                if (vt != null && k < vt.Count) SetViewerStatus(row, vt[k].Listening, vt[k].Clients, vt[k].Port);
                else                            SetViewerStatus(row, false, 0, 0);
            }
        }

        /// <summary>명령 상태 셀 — 접속됨(초록) / 대기·listen(주황) / 중지(회색).</summary>
        private void SetCmdStatus(int row, bool listening, bool connected, int port)
        {
            var c = _gridPorts.Rows[row].Cells[colCmdStat.Index];
            string suffix = port > 0 ? $" :{port}" : "";
            if (connected)      { c.Style.ForeColor = Color.LimeGreen; c.Value = "● 접속됨" + suffix; }
            else if (listening) { c.Style.ForeColor = Color.Goldenrod; c.Value = "● 대기"   + suffix; }
            else                { c.Style.ForeColor = Color.Gray;      c.Value = "● 중지"   + suffix; }
        }

        /// <summary>뷰어 상태 셀 — 클라이언트 접속 시 초록(:포트 ×N), listen 만이면 주황(대기), 아니면 회색(중지). Main 은 공백.</summary>
        private void SetViewerStatus(int row, bool listening, int clients, int port)
        {
            var c = _gridPorts.Rows[row].Cells[colViewStat.Index];
            if (row == MainRow) { c.Value = string.Empty; return; }
            string suffix = port > 0 ? $" :{port}" : "";
            if (clients > 0)    { c.Style.ForeColor = Color.LimeGreen; c.Value = "● 접속됨" + suffix + " ×" + clients; }
            else if (listening) { c.Style.ForeColor = Color.Goldenrod; c.Value = "● 대기"   + suffix; }
            else                { c.Style.ForeColor = Color.Gray;      c.Value = "● 중지"   + suffix; }
        }

        /// <summary>마지막 수신 경과 셀. 접속 중 무통신이 길면(StaleSeconds↑) 경고색.</summary>
        private void SetRx(int row, DateTime lastUtc, bool connected)
        {
            var c = _gridPorts.Rows[row].Cells[colRx.Index];
            if (lastUtc == default(DateTime))
            {
                c.Style.ForeColor = Color.DimGray;
                c.Value = "RX -";
                return;
            }
            var d = DateTime.UtcNow - lastUtc;
            if (d.Ticks < 0) d = TimeSpan.Zero;

            string ago;
            if (d.TotalSeconds < 60)      ago = $"{(int)d.TotalSeconds}s 전";
            else if (d.TotalMinutes < 60) ago = $"{(int)d.TotalMinutes}m 전";
            else                          ago = $"{(int)d.TotalHours}h 전";

            bool stale = connected && d.TotalSeconds > StaleSeconds;
            c.Style.ForeColor = stale ? Color.Goldenrod : Color.DimGray;
            c.Value = "RX " + ago;
        }

        // ── 통신 로그 갱신(변경 시에만) ──
        private void RefreshLog()
        {
            if (IsDisposed || _txtLog == null) return;
            long rev = VisionCommLog.Revision;
            if (rev == _lastLogRev) return;
            _lastLogRev = rev;
            _txtLog.Lines = VisionCommLog.Snapshot();
            _txtLog.SelectionStart = _txtLog.TextLength;
            _txtLog.ScrollToCaret();
        }
    }
}
