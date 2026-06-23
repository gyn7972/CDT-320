using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Core;          // GrabResult
using QMC.Vision.Modules;       // IVisionModule
using QMC.Vision.Ui.Controls;   // CameraView
using QMC.Vision.Ui.Localization; // Lang

namespace QMC.Vision.Ui.Pages
{
    /// <summary>Operation / Monitoring — 5개 비전 모듈 상태카드 + 모듈별 Grab 정지영상.
    ///
    /// • 상단 카드: 비클릭 상태표시. 해당 모듈에 최근 Grab 활동이 있으면 LIVE, 없으면 READY.
    ///   (시퀀서 상태 API 연동 전 임시 — 프레임 시퀀스 활동으로 LIVE 판정. 추후 알고리즘/시퀀서 연동 시 교체)
    /// • 하단: 메인(Bottom Inspection)을 크게, 나머지 4개(Wafer/Bin/TopSide/BottomSide)를 2×2.
    ///   각 패널은 그 모듈의 "마지막 Grab 정지영상"을 보여준다(연속 라이브 캡처 아님).
    /// • 프레임 갱신: 능동 Grab() 안 함. 기존 원격뷰어(Form1.MakeViewer/GrabStreamServer)와 동일하게
    ///   ViewerFrameSeq 변화(=테스트 그랩/시퀀서 그랩 시점)를 감지해 AcquireViewerFrame 으로만 가져옴.
    ///   → 핸들러/TCP 사이클에 끼어들지 않음.
    /// 정적 UI 골격은 .Designer.cs, 동작 로직은 본 파일(AGENTS 디자이너 규칙).</summary>
    public partial class OperationPage : PageBase
    {
        private const int LiveTimeoutMs = 1500;   // 최근 이 시간 내 새 프레임 → LIVE

        // 카메라 패널 식별 타이틀(장비 모듈 식별자 — 영문 고정). 안내문은 Lang.T("work.grabWait") 와 조합.
        private const string TitleBig    = "BOTTOM INSPECTION (MAIN)";
        private const string TitleWafer  = "WAFER VISION";
        private const string TitleBin    = "BIN VISION";
        private const string TitleTop    = "TOP SIDE";
        private const string TitleBottom = "BOTTOM SIDE";

        private static readonly Color CardBg   = Color.FromArgb(0x10, 0x14, 0x18);
        private static readonly Color CardName = Color.FromArgb(0x7d, 0xa0, 0xc4);
        private static readonly Color LiveOn   = Color.FromArgb(0x7e, 0xe0, 0xb8);
        private static readonly Color LiveOff  = Color.FromArgb(0x6b, 0x77, 0x86);

        // 핸들러 정렬 순서(Wafer·Bin·Bottom·TopSide·BottomSide)로 고정.
        private IVisionModule[] _mods;
        private CameraView[]    _viewByMod;   // _mods[i] ↔ 표시 패널
        private Label[]         _cardStats;
        private Label[]         _cardNames;   // 카드 이름 라벨(언어 변경 시 MAIN 접미사 갱신)
        private bool[]          _cardLive;    // 카드별 최근 LIVE 여부(언어 변경 시 상태 텍스트 재적용)
        private long[]          _lastSeq;
        private int[]           _lastActiveTick;
        private bool            _langHooked;  // LanguageChanged 중복 구독 방지

        private bool _initialized;
        private System.Windows.Forms.Timer _tapTimer;

        public OperationPage()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            TryInit();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) { TryInit(); ResumeTap(); }
            else         { PauseTap(); }
        }

        /// <summary>호스트(Form1)에서 5개 모듈을 받아 카드/패널 구성. 핸들이 늦게 잡힐 수 있어 1회만 수행.</summary>
        private void TryInit()
        {
            if (_initialized || IsDesignerMode()) return;

            var host = FindForm() as QMC.Vision.Form1;
            if (host == null) return;   // 아직 폼 부착 전 — OnVisibleChanged 에서 재시도

            // 카드/시퀀스 추적: 핸들러 정렬 순서
            _mods = new IVisionModule[]
            {
                host.WaferMod, host.BinMod, host.BottomMod,
                host.TopSideVisionMod, host.BottomSideVisionMod
            };

            // 모듈 ↔ 표시 패널 매핑(중요도: Bottom=메인 크게, 나머지 2×2)
            _viewByMod = new CameraView[]
            {
                _camS1,   // Wafer
                _camS2,   // Bin
                _camBig,  // Bottom (MAIN)
                _camS3,   // TopSide
                _camS4    // BottomSide
            };

            _lastSeq        = new long[_mods.Length];
            _lastActiveTick = new int[_mods.Length];
            for (int i = 0; i < _mods.Length; i++) { _lastSeq[i] = -1; _lastActiveTick[i] = int.MinValue / 2; }

            BuildCards();

            // 언어 동기화 — 헤더/카메라 안내문/카드 텍스트를 현재 언어로 적용 + 변경 구독(1회).
            if (!_langHooked) { Lang.LanguageChanged += OnLanguageChanged; _langHooked = true; }
            ApplyLanguage();

            _initialized = true;
            StartTap();
            UpdateRunButton();
            UpdateReadyButton();
        }

        /// <summary>언어 변경 이벤트 — UI 스레드로 마샬링 후 표시 문구 재적용.</summary>
        private void OnLanguageChanged()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((Action)ApplyLanguage); } catch { } return; }
            ApplyLanguage();
        }

        /// <summary>현재 언어로 헤더 + 5개 카메라 안내문 + 카드(MAIN/상태) 표시 문구를 적용.</summary>
        private void ApplyLanguage()
        {
            _hdr.Text = Lang.T("work.monitorHeader");

            string wait = Lang.T("work.grabWait");
            if (_camBig != null) _camBig.InfoText = TitleBig    + "\r\n" + wait;
            if (_camS1  != null) _camS1.InfoText  = TitleWafer  + "\r\n" + wait;
            if (_camS2  != null) _camS2.InfoText  = TitleBin    + "\r\n" + wait;
            if (_camS3  != null) _camS3.InfoText  = TitleTop    + "\r\n" + wait;
            if (_camS4  != null) _camS4.InfoText  = TitleBottom + "\r\n" + wait;

            RefreshCardTexts();
            Invalidate(true);
        }

        /// <summary>카드 이름(모듈명 + MAIN)과 상태(READY/LIVE)를 현재 언어로 재적용.</summary>
        private void RefreshCardTexts()
        {
            if (_cardNames == null || _mods == null) return;
            for (int i = 0; i < _cardNames.Length; i++)
            {
                bool isMain = (i == 2);   // Bottom
                string nm = (_mods[i] != null ? _mods[i].Name : "—")
                            + (isMain ? "  · " + Lang.T("work.main") : "");
                if (_cardNames[i] != null) _cardNames[i].Text = nm;

                if (_cardStats != null && _cardStats[i] != null)
                {
                    bool live = _cardLive != null && _cardLive[i];
                    _cardStats[i].Text = live ? Lang.T("common.live") : Lang.T("common.ready");
                }
            }
        }

        /// <summary>상단 5개 상태카드(비클릭). 카드[i] ↔ _mods[i].</summary>
        private void BuildCards()
        {
            _cardsHost.Controls.Clear();
            _cardStats = new Label[_mods.Length];
            _cardNames = new Label[_mods.Length];
            _cardLive  = new bool[_mods.Length];

            for (int i = 0; i < _mods.Length; i++)
            {
                IVisionModule m = _mods[i];
                bool isMain = (i == 2);   // Bottom

                var card = new Panel
                {
                    Dock      = DockStyle.Fill,
                    Margin    = new Padding(6),
                    BackColor = CardBg
                    // 비클릭: Click 핸들러/Cursor 변경 없음
                };
                var name = new Label
                {
                    Dock      = DockStyle.Top,
                    Height    = 22,
                    Text      = (m != null ? m.Name : "—") + (isMain ? "  · " + Lang.T("work.main") : ""),
                    ForeColor = isMain ? UiTheme.Accent : CardName,
                    Font      = new Font("Segoe UI", 8.5F, isMain ? FontStyle.Bold : FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding   = new Padding(8, 2, 0, 0)
                };
                var stat = new Label
                {
                    Dock      = DockStyle.Fill,
                    Text      = Lang.T("common.ready"),
                    ForeColor = LiveOff,
                    Font      = new Font("Segoe UI", 15F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding   = new Padding(8, 0, 0, 6)
                };

                card.Controls.Add(stat);
                card.Controls.Add(name);
                _cardsHost.Controls.Add(card, i, 0);
                _cardStats[i] = stat;
                _cardNames[i] = name;
            }
        }

        // ── 뷰어 탭 (비침습): 새 프레임이 있을 때만 가져와 표시 + LIVE 판정 ──
        private void StartTap()
        {
            if (_tapTimer == null)
            {
                _tapTimer = new System.Windows.Forms.Timer { Interval = 150 };
                _tapTimer.Tick += _tapTimer_Tick;
            }
            if (Visible) _tapTimer.Start();
        }

        private void ResumeTap() { if (_initialized) StartTap(); }
        private void PauseTap()  { _tapTimer?.Stop(); }
        private void StopLive()                            // Designer.Dispose 에서 호출
        {
            _tapTimer?.Stop();
            if (_langHooked) { Lang.LanguageChanged -= OnLanguageChanged; _langHooked = false; }
        }

        private void _tapTimer_Tick(object sender, EventArgs e)
        {
            if (_mods == null || !Visible) return;
            int now = Environment.TickCount;

            for (int i = 0; i < _mods.Length; i++)
            {
                IVisionModule m = _mods[i];
                if (m == null) continue;

                try
                {
                    long seq = m.ViewerFrameSeq;
                    if (seq != _lastSeq[i])
                    {
                        _lastSeq[i] = seq;
                        _lastActiveTick[i] = now;
                        // AcquireViewerFrame 은 clone 반환 → SetFrame 이 다시 clone → 내 사본 해제.
                        using (Bitmap bmp = m.AcquireViewerFrame())
                            if (bmp != null) _viewByMod[i].SetFrame(GrabResult.Success(bmp));
                    }
                }
                catch (Exception ex)
                {
                    // 모니터링 틱은 best-effort — 1프레임 실패가 루프를 멈추지 않도록 로그만 남긴다.
                    System.Diagnostics.Debug.WriteLine("[OperationPage] viewer tap 실패: " + ex.Message);
                }

                // 검사 결과 오버레이(판정 OK/NG + 결과 라인) — 시퀀서/핸들러 INSPECT 결과 반영.
                try
                {
                    if (_viewByMod != null && i < _viewByMod.Length && _viewByMod[i] != null &&
                        QMC.Vision.Core.ModuleResultStore.TryGet(m.Name, out bool pass, out string[] lines))
                    {
                        _viewByMod[i].SetVerdict(pass ? "OK" : "NG", pass);
                        _viewByMod[i].SetResultLines(lines);
                    }
                }
                catch { }

                // MATCH 결과 오버레이(찾은 위치/각/박스 + 검색 ROI) — 시퀀서/핸들러 MATCH 결과 반영.
                try
                {
                    if (_viewByMod != null && i < _viewByMod.Length && _viewByMod[i] != null &&
                        QMC.Vision.Core.MatchOverlayStore.TryGet(m.Name, out var ov))
                    {
                        var roi = (ov.RoiW > 0 && ov.RoiH > 0)
                            ? new RectangleF((float)ov.RoiX, (float)ov.RoiY, (float)ov.RoiW, (float)ov.RoiH)
                            : RectangleF.Empty;
                        System.Collections.Generic.List<QMC.Common.Ui.Controls.OverlayMark> marks = null;
                        if (ov.Marks != null && ov.Marks.Length > 0)
                        {
                            marks = new System.Collections.Generic.List<QMC.Common.Ui.Controls.OverlayMark>(ov.Marks.Length);
                            foreach (var k in ov.Marks)
                                marks.Add(new QMC.Common.Ui.Controls.OverlayMark(k.X, k.Y, k.Score, k.Angle, k.BoxW, k.BoxH));
                        }
                        _viewByMod[i].SetOverlay(roi, marks);
                    }
                }
                catch { }

                UpdateCardState(i, now);
            }

            UpdateRunButton();
            UpdateReadyButton();
        }

        /// <summary>최근 Grab 활동(시퀀스 변화)이 있으면 LIVE, 없으면 READY.
        /// 추후 시퀀서/알고리즘 실행 상태 API 가 생기면 이 판정을 그것으로 대체.</summary>
        private void UpdateCardState(int i, int now)
        {
            if (_cardStats == null || i >= _cardStats.Length) return;
            bool live = (now - _lastActiveTick[i]) <= LiveTimeoutMs;
            if (_cardLive != null && i < _cardLive.Length) _cardLive[i] = live;
            string text = live ? Lang.T("common.live") : Lang.T("common.ready");
            if (_cardStats[i].Text != text)
            {
                _cardStats[i].Text      = text;
                _cardStats[i].ForeColor = live ? LiveOn : LiveOff;
            }
        }

        // ── RUN/STOP 토글 — Sim 은 자체 시퀀서, 실제 모드는 핸들러 접속 시 RUN(CDI-300 방식). ──
        private void OnRunToggleClick(object sender, EventArgs e)
        {
            try
            {
                var host = FindForm() as QMC.Vision.Form1;
                if (host == null) return;
                if (host.IsRunActive) host.SetRun(false);     // STOP (READY 도 자동 해제)
                else if (host.CanRun) host.SetRun(true);       // RUN (Sim 항상 / 실제는 접속 시)
                UpdateRunButton();
                UpdateReadyButton();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[OperationPage] RUN 토글 실패: " + ex.Message); }
        }

        /// <summary>READY/해제 토글 — RUN 활성 상태에서만 켤 수 있다. 켜지면 핸들러 VISION 사용 허용.</summary>
        private void OnReadyToggleClick(object sender, EventArgs e)
        {
            try
            {
                var host = FindForm() as QMC.Vision.Form1;
                if (host == null) return;
                if (host.IsReady) host.SetReady(false);        // READY 해제
                else if (host.CanReady) host.SetReady(true);   // READY (RUN 활성 시에만)
                UpdateReadyButton();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[OperationPage] READY 토글 실패: " + ex.Message); }
        }

        /// <summary>RUN 버튼 텍스트/색/활성 상태를 현재 모드·연결·가동 상태로 갱신(틱마다 호출).</summary>
        private void UpdateRunButton()
        {
            if (_btnRun == null) return;
            var host = FindForm() as QMC.Vision.Form1;
            if (host == null) { _btnRun.Enabled = false; return; }

            bool running = host.IsRunActive;
            bool canRun  = host.CanRun;

            _btnRun.Enabled   = running || canRun;   // 가동 중이면 STOP, 아니면 RUN 가능할 때만
            _btnRun.Text      = running ? "STOP" : "RUN";
            _btnRun.BackColor = running
                ? Color.FromArgb(0x5a, 0x22, 0x22)
                : (canRun ? Color.FromArgb(0x1f, 0x9d, 0x4d) : Color.FromArgb(0x55, 0x55, 0x55));
        }

        /// <summary>READY 버튼 텍스트/색/활성 갱신(틱마다 호출). RUN 활성 시에만 누를 수 있고,
        /// READY 면 초록(핸들러 VISION 사용 가능), 아니면 회색.</summary>
        private void UpdateReadyButton()
        {
            if (_btnReady == null) return;
            var host = FindForm() as QMC.Vision.Form1;
            if (host == null) { _btnReady.Enabled = false; return; }

            bool ready   = host.IsReady;
            bool canRdy  = host.CanReady;   // RUN 활성 상태에서만 READY 가능

            _btnReady.Enabled   = ready || canRdy;
            _btnReady.Text      = ready ? "READY ●" : "READY";
            _btnReady.BackColor = ready
                ? Color.FromArgb(0x1f, 0x9d, 0x4d)
                : (canRdy ? Color.FromArgb(0x35, 0x6b, 0x46) : Color.FromArgb(0x55, 0x55, 0x55));
        }
    }
}
