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
    /// </summary>
    public class LightSystemSetupPage : UserControl
    {
        private DataGridView _gridCtrl;     // 컨트롤러 인벤토리
        private DataGridView _gridLabel;    // 선택 컨트롤러의 채널 라벨
        // Stage 81 — 결선: 평면 grid → TreeView + 선택 알고리즘의 ControllerSets 디테일 그리드 (다중 컨트롤러).
        private TreeView _treeWiring;
        private DataGridView _gridSets;     // 선택 알고리즘의 ControllerSets (ControllerPort 콤보 + ChannelsCsv)
        private string _selAlg;             // 현재 선택 알고리즘
        private bool   _suspendSets;        // _gridSets 프로그램적 바인딩 중 flush 억제
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<ControllerChannels>> _wiringModel
            = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<ControllerChannels>>(StringComparer.OrdinalIgnoreCase);
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
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildLayout();
            LoadFromStore();
        }

        private void BuildLayout()
        {
            BackColor = UiTheme.MainBg;

            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 30,
                Text = "조명 시스템 설정 — 기구적 결선 (한 번 설정 후 거의 변경 없음)   ※ 검사별 밝기/On-Off 값은 [레시피] 에서 설정",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            var bar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.WhiteSmoke };
            var btnSave    = MakeBtn("저장", 8, UiTheme.Accent, Color.White);   btnSave.Click    += (s, e) => Save();
            var btnReload  = MakeBtn("취소", 120, Color.White, Color.Black);    btnReload.Click  += (s, e) => LoadFromStore();
            var btnAddCtrl = MakeBtn("컨트롤러 추가", 210, Color.White, Color.Black); btnAddCtrl.Width = 120; btnAddCtrl.Click += (s, e) => AddController();
            var btnDelCtrl = MakeBtn("컨트롤러 삭제", 340, Color.White, Color.Black); btnDelCtrl.Width = 120; btnDelCtrl.Click += (s, e) => DeleteController();
            var btnMigrate = MakeBtn("io_set 가져오기", 470, Color.White, Color.Black); btnMigrate.Width = 130; btnMigrate.Click += (s, e) => Migrate();
            var btnRename  = MakeBtn("포트 일괄 변경", 610, Color.White, Color.Black); btnRename.Width = 130; btnRename.Click += (s, e) => RenamePort();
            // Stage 73 — 실장비 조명 연결/해제 (비전 Sim 이어도 실제 시리얼 점등 테스트용)
            var btnConnect = MakeBtn("조명 연결", 750, Color.FromArgb(0x2E, 0x7D, 0x32), Color.White); btnConnect.Width = 110; btnConnect.Click += (s, e) => ConnectLights();
            var btnDisc    = MakeBtn("조명 해제", 870, Color.White, Color.Black); btnDisc.Width = 110; btnDisc.Click += (s, e) => DisconnectLights();
            bar.Controls.AddRange(new Control[] { btnSave, btnReload, btnAddCtrl, btnDelCtrl, btnMigrate, btnRename, btnConnect, btnDisc });
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
            // Stage 77 — Vendor 콤보 (LFine/Leesos). 컬렉션 끝(index 6)에 추가하되 DisplayIndex 로 PortName 옆 표시
            //   → 기존 인덱스 기반 셀 접근(Cells[0], IntOf(r,2..5)) 보존.
            var colVendor = new DataGridViewComboBoxColumn { Name = "Vendor", HeaderText = "Vendor", FlatStyle = FlatStyle.Flat };
            colVendor.Items.Add("LFine"); colVendor.Items.Add("Leesos");
            _gridCtrl.Columns.Add(colVendor);
            colVendor.DisplayIndex = 1;
            // Stage 79 — Mode 콤보 (캐시 정책). 컬렉션 끝(index 7) + DisplayIndex 2 로 Vendor 옆 표시.
            var colMode = new DataGridViewComboBoxColumn { Name = "Mode", HeaderText = "Mode", FlatStyle = FlatStyle.Flat };
            colMode.Items.Add("Continuous"); colMode.Items.Add("StrobeExternal"); colMode.Items.Add("StrobeOnCommand");
            _gridCtrl.Columns.Add(colMode);
            colMode.DisplayIndex = 2;
            _gridCtrl.DataError += (s, e) => { e.ThrowException = false; };   // 콤보 외 값 입력 예외 방지
            _gridCtrl.CellEndEdit += GridCtrl_VendorCellEndEdit;              // Vendor/Mode 변경 시 보정
            _gridCtrl.SelectionChanged += (s, e) => BindLabelsForSelectedController();
            // Stage 70 A — Delete 키로 컨트롤러 삭제
            _gridCtrl.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) { DeleteController(); e.Handled = true; } };
            // Stage 70 C — PortName 편집 시 결선 콤보 items 동기
            // Stage 70 B 방향2 — ChannelCount 셀 편집 시 라벨 행 증감
            _gridCtrl.CellEndEdit += GridCtrl_CellEndEdit;
            // Stage 71 — PortName 편집 시작값 캡처 (라벨 캐시 키 이전용)
            _gridCtrl.CellBeginEdit += (s, e) =>
            {
                if (e.RowIndex >= 0 && _gridCtrl.Columns[e.ColumnIndex].Name == "PortName")
                    _editingOldPort = _gridCtrl.Rows[e.RowIndex].Cells[0].Value?.ToString();
            };
            left.Controls.Add(_gridCtrl, 0, 1);
            left.Controls.Add(SectionLabel("선택 컨트롤러의 채널 라벨 — Ch 번호 직접 지정 (1~Ch수, 비연속/부분 사용 가능)"), 0, 2);
            _gridLabel = MakeGrid();
            // Stage 82 — Channel 편집 가능 (4채널 중 1,3번만 사용 등 비연속 채널 배정 지원)
            _gridLabel.Columns.Add(Col("Channel", "Ch"));
            _gridLabel.Columns.Add(Col("Name", "Name"));
            _gridLabel.Columns.Add(Col("Color", "Color"));
            // Stage 82 — 라벨 행 추가/삭제는 ChannelCount 와 분리 — 캐시에만 반영 (Channel 값은 사용자 지정)
            _gridLabel.UserAddedRow   += (s, e) => { if (!_suspendSync) FlushLabelsToCache(); };
            _gridLabel.RowsRemoved    += (s, e) => { if (!_suspendSync) FlushLabelsToCache(); };
            // Stage 71 — 라벨 Name/Color 편집을 즉시 캐시에 write-through (선택 변경에도 보존)
            _gridLabel.CellEndEdit    += (s, e) => { if (!_suspendSync) FlushLabelsToCache(); };
            left.Controls.Add(_gridLabel, 0, 3);
            split.Controls.Add(left, 0, 0);

            // 우측: 알고리즘 결선 (Stage 81 — TreeView + 선택 알고리즘 ControllerSets 디테일, 다중 컨트롤러)
            var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = UiTheme.MainBg };
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            right.Controls.Add(SectionLabel("알고리즘 결선 — 알고리즘 선택 후 컨트롤러/채널 배정 (한 알고리즘 = 여러 컨트롤러 가능)"), 0, 0);

            _treeWiring = new TreeView { Dock = DockStyle.Fill, Font = UiTheme.ValueFont, HideSelection = false, BackColor = Color.White };
            _treeWiring.AfterSelect += (s, e) => OnAlgNodeSelected(e.Node);
            right.Controls.Add(_treeWiring, 0, 1);

            var setsBar = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.MainBg };
            var btnAddSet = new Button { Location = new Point(4, 2), Size = new Size(110, 28), Text = "컨트롤러 추가", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White };
            btnAddSet.Click += (s, e) => AddControllerSet();
            var btnDelSet = new Button { Location = new Point(120, 2), Size = new Size(70, 28), Text = "삭제", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White };
            btnDelSet.Click += (s, e) => DeleteControllerSet();
            setsBar.Controls.AddRange(new Control[] { btnAddSet, btnDelSet });
            right.Controls.Add(setsBar, 0, 2);

            _gridSets = MakeGrid();
            _gridSets.AllowUserToAddRows = false;
            var colSetCtrl = new DataGridViewComboBoxColumn { Name = "ControllerPort", HeaderText = "컨트롤러(Port)" };
            _gridSets.Columns.Add(colSetCtrl);
            _gridSets.Columns.Add(Col("ChannelsCsv", "채널 (예: 3,4,5)"));
            _gridSets.DataError += (s, e) => { e.ThrowException = false; };
            _gridSets.CellEndEdit  += (s, e) => { if (!_suspendSets) FlushSetsToModel(); };
            _gridSets.RowsRemoved  += (s, e) => { if (!_suspendSets) FlushSetsToModel(); };
            right.Controls.Add(_gridSets, 0, 3);

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

            RefreshControllerCombo();   // Stage 70 C — 콤보 items = 현재 PortName 들

            // Stage 81 — 결선 작업 모델 재시드 + 트리 빌드
            _wiringModel.Clear();
            foreach (var alg in VisionAlgorithm.All)
            {
                var w = setup.GetWiring(alg);
                var list = new System.Collections.Generic.List<ControllerChannels>();
                if (w?.ControllerSets != null) foreach (var cs in w.ControllerSets) list.Add(cs.Clone());
                _wiringModel[alg] = list;
            }
            _selAlg = null;
            RebuildWiringTree();
            BindLabelsForSelectedController();
        }

        /// <summary>Stage 81 — ControllerSets 디테일 그리드의 Controller 콤보 items 를 현재 인벤토리 PortName 으로 재구성.</summary>
        private void RefreshControllerCombo()
        {
            var col = _gridSets?.Columns["ControllerPort"] as DataGridViewComboBoxColumn;
            if (col == null) return;
            col.Items.Clear();
            col.Items.Add("");   // 미배정
            foreach (DataGridViewRow r in _gridCtrl.Rows)
            {
                if (r.IsNewRow) continue;
                string p = r.Cells[0].Value?.ToString();
                if (!string.IsNullOrEmpty(p) && !col.Items.Contains(p)) col.Items.Add(p);
            }
        }

        // ── Stage 81 — 결선 TreeView + ControllerSets 디테일 ──
        private void RebuildWiringTree()
        {
            _treeWiring.BeginUpdate();
            _treeWiring.Nodes.Clear();
            foreach (var alg in VisionAlgorithm.All)
            {
                var node = new TreeNode(VisionAlgorithm.Label(alg) + " (" + alg + ")") { Tag = alg };
                if (_wiringModel.TryGetValue(alg, out var sets))
                    foreach (var cs in sets)
                        node.Nodes.Add(new TreeNode($"{(string.IsNullOrEmpty(cs.ControllerPort) ? "(미배정)" : cs.ControllerPort)}  [{string.Join(",", cs.Channels ?? new System.Collections.Generic.List<int>())}]"));
                node.Expand();
                _treeWiring.Nodes.Add(node);
            }
            _treeWiring.EndUpdate();
            // 선택 유지
            if (_selAlg != null)
                foreach (TreeNode n in _treeWiring.Nodes)
                    if (string.Equals(n.Tag as string, _selAlg, StringComparison.OrdinalIgnoreCase)) { _treeWiring.SelectedNode = n; break; }
        }

        private void OnAlgNodeSelected(TreeNode node)
        {
            if (node == null) return;
            // 자식(ControllerSet) 노드 클릭 시 부모(알고리즘)로
            string alg = (node.Tag as string) ?? (node.Parent?.Tag as string);
            if (string.IsNullOrEmpty(alg)) return;
            _selAlg = alg;
            BindSetsGrid();
        }

        private void BindSetsGrid()
        {
            _suspendSets = true;




                
            try
            {
                _gridSets.Rows.Clear();
                if (_selAlg == null || !_wiringModel.TryGetValue(_selAlg, out var sets)) return;
                foreach (var cs in sets)
                    _gridSets.Rows.Add(cs.ControllerPort ?? "", string.Join(",", cs.Channels ?? new System.Collections.Generic.List<int>()));
            }
            finally { _suspendSets = false; }
        }

        /// <summary>_gridSets → _wiringModel[_selAlg] 반영 + 트리 노드 갱신.</summary>
        private void FlushSetsToModel()
        {
            if (_selAlg == null) return;
            var list = new System.Collections.Generic.List<ControllerChannels>();
            foreach (DataGridViewRow r in _gridSets.Rows)
            {
                if (r.IsNewRow) continue;
                string port = r.Cells["ControllerPort"].Value?.ToString()?.Trim();
                var chans = ParseCsv(r.Cells["ChannelsCsv"].Value?.ToString());
                list.Add(new ControllerChannels { ControllerPort = port, Channels = chans });
            }
            // (Controller,Channel) 중복 경고 (차단 X)
            WarnDuplicateChannels(list);
            _wiringModel[_selAlg] = list;
            RebuildWiringTree();
        }

        private void WarnDuplicateChannels(System.Collections.Generic.List<ControllerChannels> list)
        {
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cs in list)
                if (!string.IsNullOrEmpty(cs.ControllerPort) && cs.Channels != null)
                    foreach (var ch in cs.Channels)
                        if (!seen.Add(cs.ControllerPort + "/" + ch))
                            { SetStatus($"경고 — 동일 (컨트롤러,채널) 중복: {cs.ControllerPort}/{ch}", true); return; }
        }

        private void AddControllerSet()
        {
            if (_selAlg == null) { SetStatus("알고리즘을 먼저 선택하세요", true); return; }
            if (!_wiringModel.TryGetValue(_selAlg, out var sets)) { sets = new System.Collections.Generic.List<ControllerChannels>(); _wiringModel[_selAlg] = sets; }
            sets.Add(new ControllerChannels { ControllerPort = "", Channels = new System.Collections.Generic.List<int>() });
            BindSetsGrid();
            RebuildWiringTree();
            SetStatus($"{VisionAlgorithm.Label(_selAlg)} — 컨트롤러 결선 행 추가 (Port/채널 입력)", false);
        }

        private void DeleteControllerSet()
        {
            if (_selAlg == null || _gridSets.CurrentRow == null || _gridSets.CurrentRow.IsNewRow) { SetStatus("삭제할 결선 행을 선택하세요", true); return; }
            int idx = _gridSets.CurrentRow.Index;
            if (_wiringModel.TryGetValue(_selAlg, out var sets) && idx >= 0 && idx < sets.Count)
            {
                sets.RemoveAt(idx);
                BindSetsGrid();
                RebuildWiringTree();
                SetStatus("결선 행 삭제됨", false);
            }
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

            // Stage 81 — 참조 검사: 이 포트를 쓰는 ControllerSets (모든 알고리즘)
            int refCount = _wiringModel.Sum(kv => kv.Value.Count(cs => string.Equals(cs.ControllerPort, port, StringComparison.OrdinalIgnoreCase)));
            if (refCount > 0)
            {
                var dlg = MessageBox.Show($"이 컨트롤러를 사용 중인 결선 {refCount}건도 함께 비웁니다. (Recipe 의 해당 컨트롤러 값은 다음 로드 시 풀 밖으로 표시) 진행할까요?",
                    "컨트롤러 삭제", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dlg != DialogResult.Yes) return;
                foreach (var kv in _wiringModel)
                    kv.Value.RemoveAll(cs => string.Equals(cs.ControllerPort, port, StringComparison.OrdinalIgnoreCase));
                BindSetsGrid();
                RebuildWiringTree();
            }
            _gridCtrl.Rows.Remove(_gridCtrl.CurrentRow);
            // Stage 71 — 캐시 엔트리 제거 (다음 BindLabels 가 stale flush 하지 않도록 _boundPort 도 해제)
            if (!string.IsNullOrEmpty(port)) _labelCache.Remove(port);
            if (string.Equals(_boundPort, port, StringComparison.OrdinalIgnoreCase)) _boundPort = null;
            RefreshControllerCombo();
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
                RefreshControllerCombo();
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
                TrimWiringPool(port, k);
            }
        }

        /// <summary>Stage 81 — 해당 포트를 쓰는 모든 ControllerSets 의 Channels 에서 > maxCh 채널 제거 + 보고.</summary>
        private void TrimWiringPool(string port, int maxCh)
        {
            if (string.IsNullOrEmpty(port)) return;
            int trimmed = 0;
            foreach (var kv in _wiringModel)
                foreach (var cs in kv.Value)
                {
                    if (!string.Equals(cs.ControllerPort, port, StringComparison.OrdinalIgnoreCase) || cs.Channels == null) continue;
                    int before = cs.Channels.Count;
                    cs.Channels = cs.Channels.Where(c => c <= maxCh).ToList();
                    trimmed += before - cs.Channels.Count;
                }
            if (trimmed > 0)
            {
                BindSetsGrid();
                RebuildWiringTree();
                SetStatus($"결선 채널 {trimmed}건 잘림 (ChannelCount={maxCh} 초과)", false);
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
            // Stage 81 — 현재 디테일 그리드 편집을 모델에 flush 후, _wiringModel → ControllerSets 수집
            FlushSetsToModel();
            setup.EnsureWirings();
            foreach (var alg in VisionAlgorithm.All)
            {
                var w = setup.GetWiring(alg);
                w.ControllerSets = new System.Collections.Generic.List<ControllerChannels>();
                if (_wiringModel.TryGetValue(alg, out var list))
                    foreach (var cs in list)
                        if (!string.IsNullOrEmpty(cs.ControllerPort))
                            w.ControllerSets.Add(new ControllerChannels
                            {
                                ControllerPort = cs.ControllerPort,
                                Channels = new System.Collections.Generic.List<int>(cs.Channels ?? new System.Collections.Generic.List<int>())
                            });
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
