using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Recipes;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// Stage 69 — 조명 시스템 Setup 페이지.
    /// 섹션 1: 컨트롤러 인벤토리 (Port/Baud/ChannelCount/PageCount/MaxPower) + 채널 라벨.
    /// 섹션 2: 알고리즘 결선 표 (5 알고리즘 × 컨트롤러 콤보 + 채널 다중체크 + 페이지).
    /// 포트 일괄 변경 버튼 포함. 저장: Config\light_system.json.
    /// Stage 97 — Designer/Code 분리. 정적 shell 은 .Designer.cs, 결선그리드·TreeView·콤보 채움·바인딩 로직은 Code.
    /// </summary>
    public partial class LightSystemSetupPage : UserControl
    {
        private DataGridView _gridCtrl;     // 컨트롤러 인벤토리
        private DataGridView _gridLabel;    // 선택 컨트롤러의 채널 라벨
        // (C3b-3) 알고리즘 결선(TreeView/_gridSets/_wiringModel) 제거 — 결선 개념 폐기, 검사별 컨트롤러/페이지는 노드 Setup 지정.
        private Label _lblStatus;
        private bool  _suspendSync;         // 프로그램적 바인딩 중 동기 이벤트 억제 (Stage 70 B)

        // Stage 71 — 채널 라벨 단일 진실원. 포트별 라벨을 캐시로 보관하고 grid 편집을 즉시 write-through.
        // (기존 버그: CollectFromUi 가 grid 가 아닌 store 라벨을 복사 → Name 편집/Ch수 변경이 사라짐)
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<LightChannelLabel>> _labelCache
            = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<LightChannelLabel>>(StringComparer.OrdinalIgnoreCase);
        private string _boundPort;          // 현재 _gridLabel 에 바인딩된 포트 (flush 대상)
        private string _editingOldPort;     // PortName 편집 시작 시점의 이전 값 (캐시 키 이전용)

        public LightSystemSetupPage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            LoadFromStore();
        }

        // ── 이벤트 핸들러 (Designer 에서 named 연결, 원본 람다와 동일 동작) ──
        private void OnSaveClick(object sender, EventArgs e) => Save();
        private void OnReloadClick(object sender, EventArgs e) => LoadFromStore();
        private void OnAddCtrlClick(object sender, EventArgs e) => AddController();
        private void OnDelCtrlClick(object sender, EventArgs e) => DeleteController();
        private void OnMigrateClick(object sender, EventArgs e) => Migrate();
        private void OnRenameClick(object sender, EventArgs e) => RenamePort();
        private void OnConnectLightsClick(object sender, EventArgs e) => ConnectLights();
        private void OnDisconnectLightsClick(object sender, EventArgs e) => DisconnectLights();
        private void OnGridDataError(object sender, DataGridViewDataErrorEventArgs e) => e.ThrowException = false;
        private void OnGridCtrlSelectionChanged(object sender, EventArgs e) => BindLabelsForSelectedController();
        private void OnGridCtrlKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete) { DeleteController(); e.Handled = true; }
        }
        private void OnGridCtrlCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex >= 0 && _gridCtrl.Columns[e.ColumnIndex].Name == "PortName")
                _editingOldPort = _gridCtrl.Rows[e.RowIndex].Cells[0].Value?.ToString();
        }
        private void OnGridLabelUserAddedRow(object sender, DataGridViewRowEventArgs e) { if (!_suspendSync) FlushLabelsToCache(); }
        private void OnGridLabelRowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e) { if (!_suspendSync) FlushLabelsToCache(); }
        private void OnGridLabelCellEndEdit(object sender, DataGridViewCellEventArgs e) { if (!_suspendSync) FlushLabelsToCache(); }

        // ── 로드/저장 ──
        private void LoadFromStore()
        {
            var setup = LightSystemSetupStore.Load();
            BindAll(setup);
            SetStatus("로드 완료 — 컨트롤러 " + (setup.Controllers?.Count ?? 0) + "개", false);
        }

        private void BindAll(LightSystemSetup setup)
        {
            _suspendSync = true;
            try { BindAllCore(setup); } finally { _suspendSync = false; }
        }

        private void BindAllCore(LightSystemSetup setup)
        {
            // Stage 71 — 라벨 캐시를 로드된 setup 으로 재시드 (포트별 라벨 복제본 보관)
            _labelCache.Clear();
            _boundPort = null;

            _gridCtrl.Rows.Clear();
            foreach (var c in setup.Controllers)
            {
                _gridCtrl.Rows.Add(c.PortName, c.Name, c.BaudRate, c.ChannelCount, c.PageCount, c.MaxPower,
                                   string.IsNullOrEmpty(c.Vendor) ? "LFine" : c.Vendor, c.Mode.ToString());
                if (!string.IsNullOrEmpty(c.PortName))
                    _labelCache[c.PortName] = CloneLabels(c.ChannelLabels);
            }

            BindLabelsForSelectedController();
        }

        private void BindLabelsForSelectedController()
        {
            bool prev = _suspendSync;
            // 다른 포트로 넘어가기 전에 현재 grid 편집을 캐시로 flush (프로그램적 바인딩 중이 아닐 때만)
            if (!prev) FlushLabelsToCache();
            _suspendSync = true;
            try
            {
                _gridLabel.Rows.Clear();
                var port = SelectedControllerPort();
                _boundPort = port;
                if (string.IsNullOrEmpty(port)) return;
                // 캐시(단일 진실원)에서 읽고 ChannelCount 에 맞춰 행 수 reconcile
                var labels = GetOrSeedLabels(port);
                foreach (var l in labels) _gridLabel.Rows.Add(l.Channel, l.Name, l.Color);
            }
            finally { _suspendSync = prev; }
        }

        /// <summary>Stage 71 — _gridLabel 의 현재 행을 _boundPort 의 캐시 라벨로 저장 (Channel 1..N 재부여).</summary>
        private void FlushLabelsToCache()
        {
            if (string.IsNullOrEmpty(_boundPort)) return;
            var list = new System.Collections.Generic.List<LightChannelLabel>();
            int auto = 1;
            foreach (DataGridViewRow r in _gridLabel.Rows)
            {
                if (r.IsNewRow) continue;
                int ch = IntOf(r, 0, 0);              // Stage 82 — 편집된 Channel 셀 (강제 재번호 안 함)
                if (ch <= 0) ch = auto;               // 비었으면 순번 fallback
                auto = ch + 1;
                string color = Str(r, 2);
                list.Add(new LightChannelLabel { Channel = ch, Name = Str(r, 1), Color = string.IsNullOrEmpty(color) ? "White" : color });
            }
            _labelCache[_boundPort] = list;
        }

        /// <summary>Stage 82 — 캐시 라벨을 그대로 반환(채널 번호 보존). 라벨이 없으면 ChannelCount 기준 1..N 기본 시드.</summary>
        private System.Collections.Generic.List<LightChannelLabel> GetOrSeedLabels(string port)
        {
            _labelCache.TryGetValue(port, out var list);
            if (list == null || list.Count == 0) list = SeedLabels(ChannelCountForPort(port));
            else list = CloneLabels(list);   // 기존 라벨 유지 — 재번호/트림 안 함
            _labelCache[port] = list;
            return list;
        }

        /// <summary>1..n 기본 라벨 시드 (첫 선택 시 편의 — 이후 사용자가 Channel 편집/행 추가삭제 가능).</summary>
        private static System.Collections.Generic.List<LightChannelLabel> SeedLabels(int n)
        {
            var list = new System.Collections.Generic.List<LightChannelLabel>();
            if (n < 0) n = 0;
            for (int i = 1; i <= n; i++) list.Add(new LightChannelLabel { Channel = i, Name = "", Color = "White" });
            return list;
        }

        /// <summary>Stage 82 — 채널 번호 보존하되 하드웨어 범위[1,maxCh] 밖/중복은 제거 (재번호 안 함). 저장 정합용.</summary>
        private static System.Collections.Generic.List<LightChannelLabel> SanitizeLabels(System.Collections.Generic.List<LightChannelLabel> src, int maxCh)
        {
            var list = new System.Collections.Generic.List<LightChannelLabel>();
            var seen = new System.Collections.Generic.HashSet<int>();
            if (src != null)
                foreach (var l in src)
                {
                    if (l.Channel < 1 || l.Channel > maxCh) continue;   // 범위 밖
                    if (!seen.Add(l.Channel)) continue;                 // 중복(첫 항목 우선)
                    list.Add(new LightChannelLabel { Channel = l.Channel, Name = l.Name, Color = string.IsNullOrEmpty(l.Color) ? "White" : l.Color });
                }
            return list;
        }

        private static System.Collections.Generic.List<LightChannelLabel> CloneLabels(System.Collections.Generic.List<LightChannelLabel> src)
        {
            var list = new System.Collections.Generic.List<LightChannelLabel>();
            if (src != null)
                foreach (var l in src) list.Add(new LightChannelLabel { Channel = l.Channel, Name = l.Name, Color = string.IsNullOrEmpty(l.Color) ? "White" : l.Color });
            return list;
        }

        /// <summary>_gridCtrl 에서 해당 포트 행의 ChannelCount 값 (없으면 8).</summary>
        private int ChannelCountForPort(string port)
        {
            foreach (DataGridViewRow r in _gridCtrl.Rows)
            {
                if (r.IsNewRow) continue;
                if (string.Equals(r.Cells[0].Value?.ToString(), port, StringComparison.OrdinalIgnoreCase))
                    return IntOf(r, 3, 8);
            }
            return 8;
        }

        // ── Stage 70 A — 컨트롤러 삭제 ──
        private void DeleteController()
        {
            if (_gridCtrl.CurrentRow == null || _gridCtrl.CurrentRow.IsNewRow) { SetStatus("삭제할 컨트롤러를 선택하세요", true); return; }
            string port = _gridCtrl.CurrentRow.Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(port)) { _gridCtrl.Rows.Remove(_gridCtrl.CurrentRow); return; }

            _gridCtrl.Rows.Remove(_gridCtrl.CurrentRow);
            // Stage 71 — 캐시 엔트리 제거 (다음 BindLabels 가 stale flush 하지 않도록 _boundPort 도 해제)
            if (!string.IsNullOrEmpty(port)) _labelCache.Remove(port);
            if (string.Equals(_boundPort, port, StringComparison.OrdinalIgnoreCase)) _boundPort = null;
            BindLabelsForSelectedController();
            int left = _gridCtrl.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);
            SetStatus($"{port} 삭제됨" + (left == 0 ? " — 조명 미사용 상태가 됩니다." : ""), false);
        }

        // ── Stage 70 B 방향2 + C — 컨트롤러 셀 편집 (ChannelCount → 라벨, PortName → 콤보) ──
        private void GridCtrl_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _gridCtrl.Rows[e.RowIndex];
            string colName = _gridCtrl.Columns[e.ColumnIndex].Name;

            if (colName == "PortName")
            {
                // Stage 71 — 라벨 캐시 키를 이전 포트명 → 새 포트명으로 이전 (라벨 보존)
                string newPort = row.Cells[0].Value?.ToString();
                if (!string.IsNullOrEmpty(_editingOldPort) && !string.Equals(_editingOldPort, newPort, StringComparison.OrdinalIgnoreCase)
                    && _labelCache.TryGetValue(_editingOldPort, out var lbls))
                {
                    _labelCache.Remove(_editingOldPort);
                    if (!string.IsNullOrEmpty(newPort)) _labelCache[newPort] = lbls;
                }
                if (string.Equals(_boundPort, _editingOldPort, StringComparison.OrdinalIgnoreCase)) _boundPort = newPort;
                _editingOldPort = null;
                return;
            }
            if (colName == "ChannelCount")
            {
                if (!int.TryParse(row.Cells["ChannelCount"].Value?.ToString(), out int k) || k < 0) return;
                // 선택 컨트롤러가 이 행이어야 라벨 그리드가 일치 — 현재 행 선택 강제
                if (_gridCtrl.CurrentRow != row) { _gridCtrl.CurrentCell = row.Cells[0]; BindLabelsForSelectedController(); }
                string port = row.Cells[0].Value?.ToString();
                _boundPort = port;
                FlushLabelsToCache();   // 현재 grid 편집 보존
                // Stage 82 — ChannelCount 는 하드웨어 채널 수(유효 범위). 라벨 행 수를 강제하지 않고
                //            범위[1,k] 밖 채널 라벨만 제거. 라벨이 비어 있으면 1..k 기본 시드.
                _labelCache.TryGetValue(port, out var labels);
                labels = (labels == null || labels.Count == 0) ? SeedLabels(k) : SanitizeLabels(labels, k);
                _labelCache[port] = labels;
                _suspendSync = true;
                try
                {
                    _gridLabel.Rows.Clear();
                    foreach (var l in labels) _gridLabel.Rows.Add(l.Channel, l.Name, l.Color);
                }
                finally { _suspendSync = false; }
            }
        }

        private string SelectedControllerPort()
        {
            if (_gridCtrl.CurrentRow == null) return null;
            return _gridCtrl.CurrentRow.Cells[0].Value?.ToString();
        }

        /// <summary>UI → LightSystemSetup 객체 수집.</summary>
        private LightSystemSetup CollectFromUi()
        {
            // Stage 71 — 현재 grid 의 라벨 편집을 캐시에 flush 한 뒤 캐시를 라벨 진실원으로 사용
            FlushLabelsToCache();
            var setup = new LightSystemSetup();
            foreach (DataGridViewRow r in _gridCtrl.Rows)
            {
                if (r.IsNewRow) continue;
                string port = Str(r, 0);
                if (string.IsNullOrEmpty(port)) continue;
                int chCount = IntOf(r, 3, 8);
                // 채널 라벨: 캐시(단일 진실원) → ChannelCount 로 reconcile (캐시 미스 시 store fallback)
                _labelCache.TryGetValue(port, out var cached);
                if (cached == null) cached = LightSystemSetupStore.Current.GetController(port)?.ChannelLabels;
                string vendor = r.Cells["Vendor"].Value?.ToString()?.Trim();
                if (string.IsNullOrEmpty(vendor)) vendor = "LFine";
                if (!Enum.TryParse(r.Cells["Mode"].Value?.ToString(), out LightControllerMode mode))
                    mode = LightControllerMode.StrobeOnCommand;
                setup.Controllers.Add(new LightControllerEntry
                {
                    PortName = port, Vendor = vendor, Mode = mode, Name = Str(r, 1),
                    BaudRate = IntOf(r, 2, 9600), ChannelCount = chCount,
                    PageCount = IntOf(r, 4, 1), MaxPower = IntOf(r, 5, 240),
                    ChannelLabels = SanitizeLabels(cached, chCount)
                });
            }
            // (C3b-3) 결선(AlgorithmWirings) 수집 제거 — 검사별 컨트롤러/페이지는 노드 Setup 지정.
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

        /// <summary>Stage 77/79 — Vendor 변경 시 벤더 특성에 맞춰 Mode/PageCount/MaxPower 보정.</summary>
        private void GridCtrl_VendorCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string col = _gridCtrl.Columns[e.ColumnIndex].Name;
            if (col == "Mode") { ApplyModeLabel(_gridCtrl.Rows[e.RowIndex]); return; }
            if (col != "Vendor") return;
            var row = _gridCtrl.Rows[e.RowIndex];
            string vendor = row.Cells["Vendor"].Value?.ToString();
            if (string.Equals(vendor, "Leesos", StringComparison.OrdinalIgnoreCase))
            {
                // Leesos: Page 미지원(1), Mode=Continuous 고정(readonly), Volume 12-bit(0~4095)
                row.Cells["PageCount"].Value = 1;
                row.Cells["Mode"].Value = "Continuous";
                row.Cells["Mode"].ReadOnly = true;
                if (!int.TryParse(row.Cells["MaxPower"].Value?.ToString(), out int mp) || mp <= 0 || mp > 4095 || mp == 240)
                    row.Cells["MaxPower"].Value = 4095;
                SetStatus("Vendor=Leesos — Page 미지원(1) / Mode=Continuous 고정 / MaxPwr 0~4095 (12-bit, Strobe 미사용)", false);
            }
            else if (string.Equals(vendor, "LFine", StringComparison.OrdinalIgnoreCase))
            {
                row.Cells["Mode"].ReadOnly = false;
                if (!int.TryParse(row.Cells["MaxPower"].Value?.ToString(), out int mp) || mp <= 0)
                    row.Cells["MaxPower"].Value = 240;
            }
        }

        private void ApplyModeLabel(DataGridViewRow row)
        {
            if (string.Equals(row.Cells["Mode"].Value?.ToString(), "StrobeOnCommand", StringComparison.OrdinalIgnoreCase))
                SetStatus("Mode=StrobeOnCommand — 캐시 skip 안 함(매 호출이 발사 트리거). 같은 검사 반복 시 매번 송신.", false);
        }

        private void AddController()
        {
            _gridCtrl.Rows.Add("COM?", "Illuminator", 9600, 8, 1, 240, "LFine", "StrobeOnCommand");   // Stage 77/79 — 기본 Vendor/Mode
            SetStatus("컨트롤러 행 추가 — PortName 수정 후 저장", false);
        }

        private void Migrate()
        {
            // io_set.lightSource.json 은 Handler/Vision 의 Config 폴더에 위치 — 현 exe Config 우선.
            string ioSet = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "io_set.lightSource.json");
            var setup = LightSystemMigrator.MigrateFromLegacy(ioSet);
            if (setup == null) { SetStatus("io_set.lightSource.json 없음/파싱 실패: " + ioSet, true); return; }
            LightSystemMigrator.BackupLegacy(ioSet, DateTime.Now.ToString("yyyyMMdd"));
            LightSystemSetupStore.SetCurrent(setup);
            BindAll(setup);
            SetStatus($"가져오기 완료 — 컨트롤러 {setup.Controllers.Count}개 (저장 필요). 검사별 컨트롤러/페이지는 [설정>검사]에서 지정.", false);
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

        // ── Stage 73 — 실장비 조명 연결/해제 ──
        /// <summary>현재 인벤토리(grid)로 실장비 컨트롤러를 재초기화하고 시리얼 포트를 Open.
        /// 비전 백엔드가 Sim 이어도 실제 LFine 조명에 시리얼이 나간다. 이후 검사 노드의 '실행 적용'으로 점등 확인.</summary>
        private async void ConnectLights()
        {
            try
            {
                var setup = CollectFromUi();   // 미저장 인벤토리 편집도 반영 (포트/Baud 등)
                if (setup.Controllers == null || setup.Controllers.Count == 0)
                { SetStatus("컨트롤러가 없습니다 — 인벤토리에 추가 후 다시 시도", true); return; }

                // 실장비(useSim=false) 로 LightHub 재구성 후 포트 Open
                QMC.Vision.Comm.LightHub.Initialize(setup, false);
                SetStatus("조명 연결 중… (시리얼 Open)", false);
                var res = await QMC.Vision.Comm.LightHub.ConnectAllAsync();

                int ok = res.Count(kv => kv.Value);
                var fails = res.Where(kv => !kv.Value).Select(kv => kv.Key).ToList();
                if (fails.Count == 0)
                    SetStatus($"조명 연결 완료 — {ok}포트 Open. 검사 노드의 '실행 적용'으로 실제 점등 테스트하세요.", false);
                else
                    SetStatus($"조명 연결 — {ok}포트 OK / 실패 [{string.Join(",", fails)}] (포트/케이블 확인, LIGHT-OPEN-FAIL 알람 참조)", true);
            }
            catch (Exception ex) { SetStatus("조명 연결 예외: " + ex.Message, true); }
        }

        /// <summary>시리얼 Close 후 Sim 컨트롤러로 복귀(안전 상태). 실제 점등 중단.</summary>
        private async void DisconnectLights()
        {
            try
            {
                await QMC.Vision.Comm.LightHub.DisconnectAllAsync();
                QMC.Vision.Comm.LightHub.Initialize(LightSystemSetupStore.Current, true);   // Sim 복귀
                SetStatus("조명 해제 — 시리얼 Close, Sim 컨트롤러로 복귀", false);
            }
            catch (Exception ex) { SetStatus("조명 해제 예외: " + ex.Message, true); }
        }

        // ── helpers (로직) ──
        private static string Str(DataGridViewRow r, int i) => r.Cells[i].Value?.ToString()?.Trim() ?? "";
        private static int IntOf(DataGridViewRow r, int i, int def) => int.TryParse(Str(r, i), out var v) ? v : def;
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
