using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Vision.Comm;
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// '회전 중심'(COC) 타깃 페이지 — 바텀 검사 콜렛 회전 중심 측정 전용 화면.
    /// <para>
    /// [누적 시작]으로 카메라를 누적 라이브로 켜고(핸들러가 콜렛 회전), [종료·중심 계산]으로 라이브를
    /// 끄고 누적 평균 영상의 가로/세로 대칭 중심 = 회전 중심 (x,y) 를 계산해 크로스 마크로 표시한다.
    /// 통신(COC START/END)으로 실행된 결과도 동일하게 자동 표시된다(<see cref="ColletRotationCenterCore.TryGetLast"/> 폴링).
    /// </para>
    /// 실행 로직은 <see cref="ColletRotationCenterCore"/> 와 공유 — 수동/통신이 완전히 같은 경로다.
    /// </summary>
    public sealed partial class ColletRotCenterPage : PageBase, ITargetPage
    {
        // ── Fields ─────────────────────────────────────
        private readonly IVisionModule _module;
        private IAlgorithmNode _node;                    // ColletRotCenterFinder 노드 — 노출/조명(Recipe) SSOT
        private InspectionLightPanel _lightPanel;        // 검사 조명 편집/점등 패널(편입 모드)
        private bool _dirty;                             // 파라미터/조명 변경 미저장 여부
        private string _recipeName = "default";
        private long _lastLogRev = -1;                   // COC 통신 로그 갱신 리비전
        private long _lastViewerSeq = -1;                // 모듈 뷰어 프레임 시퀀스 — 통신 COC 평균 영상 자동 표시용
        private long _lastResultSeq = -1;                // COC 결과 시퀀스 — 통신/수동 결과 변경 감지
        private PointF? _center;                         // 마지막 회전 중심(이미지 좌표) — 크로스 마크
        private int _sourceWidth;
        private int _sourceHeight;
        private int _displayWidth;
        private int _displayHeight;

        // ── Properties (ITargetPage) ───────────────────
        public bool IsDirty => _dirty;

        /// <summary>노출 또는 조명 레벨이 저장돼 있으면 저장 데이터 있음.</summary>
        public bool HasSavedData
        {
            get
            {
                var r = _node?.Recipe as AlgoRecipeBase;
                return r != null && (r.ExposureUs > 0 || (r.LightSettings != null && r.LightSettings.Any(l => l.Level > 0)));
            }
        }

        public event EventHandler DirtyChanged;

        // ── Constructor ────────────────────────────────
        public ColletRotCenterPage()
        {
            InitializeComponent();
        }

        public ColletRotCenterPage(IVisionModule module, string recipeName = "default")
        {
            _module = module;
            InitializeComponent();
            if (IsDesignerMode()) return;

            _recipeName = string.IsNullOrWhiteSpace(recipeName) ? "default" : recipeName;
            _node = _module?.GetAlgorithm(ColletRotationCenterCore.ToolId);

            // 검사 조명 — 편입 모드(저장은 상단바 SAVE=SaveTarget 에서 노드와 함께).
            _lightPanel = new InspectionLightPanel
            {
                Dock = DockStyle.Fill,
                EmbeddedMode = true,
                RecipeName = _recipeName
            };
            _lightPanel.SelectInspection(_node, _module?.AlgorithmKey ?? "", ColletRotationCenterCore.ToolId);
            _lightPanel.LightChanged += (s, e) => MarkDirty();
            pnlLightHost.Controls.Add(_lightPanel);
            _lightPanel.BringToFront();

            GridTheme.Apply(grid);
            grid.Columns.Add("item", "항목");
            grid.Columns.Add("val", "값");
            grid.Columns["val"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Rows.Add("중심 X (px)", "-");
            grid.Rows.Add("중심 Y (px)", "-");
            grid.Rows.Add("누적 프레임", "-");
            grid.Rows.Add("측정 시각", "-");

            camView.AttachModule(_module);
            camView.StageName = _module?.Name;
            camView.SetActiveTool(ColletRotationCenterCore.ToolId);   // 툴바 Grab/Live 도 이 도구의 노출/조명으로 촬상
            camView.CustomOverlayPaint = DrawCenterCross;

            btnStart.Click += btnStart_Click;
            btnEnd.Click += btnEnd_Click;
            timerRefresh.Tick += (s, e) => RefreshView();

            BuildParams();
            _params.Title = "PARAMETERS";
            lblHdrParam.Visible = false;
            _right.RowStyles[4].Height = 0;

            SectionHeaderStyle.Apply(lblHdrImg, lblHdrAction, lblHdrResult, lblHdrLight);
        }

        // ── Event Methods ──────────────────────────────
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (IsDesignerMode() || _module == null) return;
            if (Visible) { RefreshView(); timerRefresh.Start(); }
            else
            {
                timerRefresh.Stop();
                // 다른 도구/페이지로 이동 시 툴바 Live 정지(다른 레시피 페이지와 동일 정책).
                // 단, COC 누적 세션(통신/수동) 진행 중이면 세션 라이브는 코어가 소유 — 여기서 끄지 않는다.
                try
                {
                    if (!ColletRotationCenterCore.IsRunning(_module.Name) && camView != null && camView.IsLive)
                        camView.StopLive();
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[ColletRotCenterPage] 숨김 시 Live 정지 실패: " + ex.Message); }
            }
        }

        /// <summary>[누적 시작] — COC START 와 동일 경로. 노출/조명 적용 후 누적 라이브 시작(핸들러가 회전).</summary>
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (_module == null) return;
            btnStart.Enabled = false;
            Task.Run(() =>
            {
                string r = null;
                try { r = ColletRotationCenterCore.Start(_module); }
                catch (Exception ex) { r = "fail:" + ex.Message; }
                try
                {
                    BeginInvoke((Action)(() =>
                    {
                        btnStart.Enabled = true;
                        if (r == null || !r.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                            VisionCommLog.Add("[ColletRotCenterPage] COC 시작 실패: " + r);
                        RefreshView();
                    }));
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[ColletRotCenterPage] 시작 UI 갱신 실패: " + ex.Message); }
            });
        }

        /// <summary>[종료·중심 계산] — COC END 와 동일 경로. 라이브 정지 + 누적 평균의 대칭 중심 계산.
        /// 결과 표시는 타이머의 결과 시퀀스 폴링이 담당(통신 실행과 동일 흐름).</summary>
        private void btnEnd_Click(object sender, EventArgs e)
        {
            if (_module == null) return;
            btnEnd.Enabled = false;
            Task.Run(() =>
            {
                string r = null;
                try { r = ColletRotationCenterCore.End(_module); }
                catch (Exception ex) { r = "fail:" + ex.Message; }
                try
                {
                    BeginInvoke((Action)(() =>
                    {
                        btnEnd.Enabled = true;
                        if (r == null || !r.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                            VisionCommLog.Add("[ColletRotCenterPage] COC 종료 실패: " + r);
                        RefreshView();
                    }));
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[ColletRotCenterPage] 종료 UI 갱신 실패: " + ex.Message); }
            });
        }

        // ── Public Methods (ITargetPage) ───────────────
        /// <summary>노출 파라미터 + 조명을 노드 레시피로 저장.</summary>
        public void SaveTarget()
        {
            try
            {
                _lightPanel?.PersistLight();
                _node?.SaveRecipe(_recipeName);
                _dirty = false;
                try { DirtyChanged?.Invoke(this, EventArgs.Empty); } catch { }
            }
            catch (Exception ex) { VisionCommLog.Add("[ColletRotCenterPage] 저장 실패: " + ex.Message); }
        }

        /// <summary>저장된 레시피(노출/조명)로 되돌린다(revert).</summary>
        public void LoadTarget()
        {
            try { _node?.LoadRecipe(_recipeName); }
            catch (Exception ex) { VisionCommLog.Add("[ColletRotCenterPage] 로드 실패: " + ex.Message); }
            try { _params?.RefreshValues(); } catch { }
            try { _lightPanel?.SelectInspection(_node, _module?.AlgorithmKey ?? "", ColletRotationCenterCore.ToolId); } catch { }
            _dirty = false;
            try { DirtyChanged?.Invoke(this, EventArgs.Empty); } catch { }
        }

        /// <summary>조명 지정(SettingsPage) 변경을 조명 그리드에 반영 — RecipePage 가 타깃 표시 시 호출.</summary>
        public void RefreshLightAssignment()
            => _lightPanel?.SelectInspection(_node, _module?.AlgorithmKey ?? "", ColletRotationCenterCore.ToolId);

        // ── Private Methods ────────────────────────────
        /// <summary>우측 파라미터 — 도구 전용 카메라 노출(µs) + 시뮬 저장 이미지(카메라 시뮬레이션 전용).</summary>
        private void BuildParams()
        {
            var items = new System.Collections.Generic.List<ParameterGridItem>();

            // 도구 전용 노출(µs) — 0=미지정(모듈 레시피 노출 사용). COC START/툴바 그랩이 촬상 직전 적용.
            items.Add(ParameterGridItem.Double("카메라 노출", "µs", ParameterGridScope.Recipe,
                () => (_node?.Recipe as AlgoRecipeBase)?.ExposureUs ?? 0,
                v =>
                {
                    if (_node?.Recipe is AlgoRecipeBase r)
                    {
                        r.ExposureUs = v > 0 ? v : 0;
                        MarkDirty();
                        // 즉시 카메라 반영(라이브 확인용) — 저장은 SAVE 에서.
                        try { if (v > 0 && _module?.Camera != null) _module.Camera.ExposureUs = v; } catch { }
                    }
                }));

            if (_node?.Setup is AlgoSetupBase)
            {
                items.Add(ParameterGridItem.Bool("시뮬 저장 이미지 사용 (카메라 시뮬레이션 전용)", ParameterGridScope.Setup,
                    () => (_node.Setup as AlgoSetupBase)?.SimUseSavedImage ?? false,
                    v => { if (_node.Setup is AlgoSetupBase su) { su.SimUseSavedImage = v; MarkDirty(); } }));
                items.Add(ParameterGridItem.FilePath("시뮬 저장 이미지 경로", ParameterGridScope.Setup,
                    () => (_node.Setup as AlgoSetupBase)?.SimSavedImagePath ?? "",
                    v => { if (_node.Setup is AlgoSetupBase su) { su.SimSavedImagePath = v?.Trim() ?? ""; MarkDirty(); } },
                    "이미지 파일 (*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff)|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff|모든 파일 (*.*)|*.*"));
            }

            _params.SetItems(items);
            _params.ParameterValueChanged += (s, e) => MarkDirty();
        }

        private void MarkDirty()
        {
            if (_dirty) return;
            _dirty = true;
            try { DirtyChanged?.Invoke(this, EventArgs.Empty); } catch { }
        }

        // ── UI Update Methods ──────────────────────────
        /// <summary>타이머 갱신 — 진행 상태 / 통신·수동 결과 / 평균 영상 / COC 로그.</summary>
        private void RefreshView()
        {
            if (IsDesignerMode() || IsDisposed || _module == null) return;
            RefreshRunState();
            RefreshResult();
            RefreshViewerFrame();
            RefreshLog();
        }

        /// <summary>진행 상태 라벨 — 통신 COC START 로 시작된 세션도 반영.</summary>
        private void RefreshRunState()
        {
            try
            {
                bool run = ColletRotationCenterCore.IsRunning(_module.Name);
                lblState.Text = run ? "누적 중... (라이브 ON — 콜렛 회전 후 종료를 누르세요)" : "대기";
                lblState.ForeColor = run ? Color.FromArgb(232, 93, 26) : Color.FromArgb(0x22, 0x22, 0x22);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[ColletRotCenterPage] 상태 갱신 실패: " + ex.Message); }
        }

        /// <summary>마지막 COC 결과(통신/수동 공통) — 시퀀스 변경 시 결과 그리드 + 크로스 마크 갱신.</summary>
        private void RefreshResult()
        {
            try
            {
                double x, y; int frames; DateTime time; long seq;
                if (!ColletRotationCenterCore.TryGetLast(_module.Name, out x, out y, out frames, out time, out seq)) return;
                // 툴바 그랩이 CustomOverlayPaint 를 지웠으면 재연결(마지막 검출 그림 유지 정책).
                if (camView.CustomOverlayPaint == null) camView.CustomOverlayPaint = DrawCenterCross;
                if (seq == _lastResultSeq) return;
                _lastResultSeq = seq;

                _center = new PointF((float)x, (float)y);
                ColletRotationCenterCore.TryGetLastDisplayInfo(
                    _module.Name,
                    out _sourceWidth,
                    out _sourceHeight,
                    out _displayWidth,
                    out _displayHeight);
                grid.Rows[0].Cells[1].Value = x.ToString("F2");
                grid.Rows[1].Cells[1].Value = y.ToString("F2");
                grid.Rows[2].Cells[1].Value = frames.ToString();
                grid.Rows[3].Cells[1].Value = time.ToString("HH:mm:ss");
                camView.Invalidate();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[ColletRotCenterPage] 결과 갱신 실패: " + ex.Message); }
        }

        /// <summary>회전 중심 크로스 마크 — 이미지 좌표 (x,y) 를 화면 좌표로 변환해 십자+원+좌표 라벨.</summary>
        private void DrawCenterCross(Graphics g, Func<PointF, PointF> toScreen)
        {
            if (_center == null) return;
            PointF displayCenter = _center.Value;
            if (_sourceWidth > 0 && _sourceHeight > 0 && _displayWidth > 0 && _displayHeight > 0)
            {
                displayCenter = new PointF(
                    _center.Value.X * _displayWidth / _sourceWidth,
                    _center.Value.Y * _displayHeight / _sourceHeight);
            }
            PointF c = toScreen(displayCenter);
            using (var pen = new Pen(Color.Lime, 2f))
            {
                g.DrawLine(pen, c.X - 28, c.Y, c.X + 28, c.Y);
                g.DrawLine(pen, c.X, c.Y - 28, c.X, c.Y + 28);
                g.DrawEllipse(pen, c.X - 9, c.Y - 9, 18, 18);
            }
            string txt = "COC (" + _center.Value.X.ToString("F1") + ", " + _center.Value.Y.ToString("F1") + ")";
            using (var f = new Font("맑은 고딕", 9F, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(txt, f);
                using (var bg = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                    g.FillRectangle(bg, c.X + 14, c.Y + 14, sz.Width, sz.Height);
                using (var fg = new SolidBrush(Color.Lime))
                    g.DrawString(txt, f, fg, c.X + 14, c.Y + 14);
            }
        }

        /// <summary>통신 COC END 가 발행한 누적 평균 영상 등 모듈 뷰어 프레임 자동 표시(라이브 중엔 라이브 우선).</summary>
        private void RefreshViewerFrame()
        {
            try
            {
                if (camView.IsLive) return;
                long seq = _module.ViewerFrameSeq;
                if (seq == _lastViewerSeq) return;
                _lastViewerSeq = seq;
                if (seq <= 0) return;
                Bitmap f = _module.AcquireViewerFrame();
                if (f == null) return;
                try { camView.SetImage(f); }   // 내부 복제 — 원본은 여기서 해제
                finally { f.Dispose(); }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[ColletRotCenterPage] 프레임 갱신 실패: " + ex.Message); }
        }

        /// <summary>COC 관련 통신/동작 로그를 하단 스트립에 표시(변경 시에만).</summary>
        private void RefreshLog()
        {
            try
            {
                long rev = VisionCommLog.Revision;
                if (rev == _lastLogRev) return;
                _lastLogRev = rev;

                string[] lines = VisionCommLog.Snapshot()
                    .Where(l => l != null && l.IndexOf("COC", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
                if (lines.Length > 200) lines = lines.Skip(lines.Length - 200).ToArray();

                txtLog.Lines = lines.Length > 0
                    ? lines
                    : new[] { "COC 통신 대기 중... (핸들러 COC START/END 수신 시 표시)" };
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[ColletRotCenterPage] 로그 갱신 실패: " + ex.Message); }
        }
    }
}
