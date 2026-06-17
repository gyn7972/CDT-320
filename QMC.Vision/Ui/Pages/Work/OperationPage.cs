using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Core;          // GrabResult
using QMC.Vision.Modules;       // IVisionModule
using QMC.Vision.Ui.Controls;   // CameraView

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

        private static readonly Color CardBg   = Color.FromArgb(0x10, 0x14, 0x18);
        private static readonly Color CardName = Color.FromArgb(0x7d, 0xa0, 0xc4);
        private static readonly Color LiveOn   = Color.FromArgb(0x7e, 0xe0, 0xb8);
        private static readonly Color LiveOff  = Color.FromArgb(0x6b, 0x77, 0x86);

        // 핸들러 정렬 순서(Wafer·Bin·Bottom·TopSide·BottomSide)로 고정.
        private IVisionModule[] _mods;
        private CameraView[]    _viewByMod;   // _mods[i] ↔ 표시 패널
        private Label[]         _cardStats;
        private long[]          _lastSeq;
        private int[]           _lastActiveTick;

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
            _initialized = true;
            StartTap();
        }

        /// <summary>상단 5개 상태카드(비클릭). 카드[i] ↔ _mods[i].</summary>
        private void BuildCards()
        {
            _cardsHost.Controls.Clear();
            _cardStats = new Label[_mods.Length];

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
                    Text      = (m != null ? m.Name : "—") + (isMain ? "  · MAIN" : ""),
                    ForeColor = isMain ? UiTheme.Accent : CardName,
                    Font      = new Font("Segoe UI", 8.5F, isMain ? FontStyle.Bold : FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding   = new Padding(8, 2, 0, 0)
                };
                var stat = new Label
                {
                    Dock      = DockStyle.Fill,
                    Text      = "READY",
                    ForeColor = LiveOff,
                    Font      = new Font("Segoe UI", 15F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding   = new Padding(8, 0, 0, 6)
                };

                card.Controls.Add(stat);
                card.Controls.Add(name);
                _cardsHost.Controls.Add(card, i, 0);
                _cardStats[i] = stat;
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
        private void StopLive()  { _tapTimer?.Stop(); }   // Designer.Dispose 에서 호출

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

                UpdateCardState(i, now);
            }
        }

        /// <summary>최근 Grab 활동(시퀀스 변화)이 있으면 LIVE, 없으면 READY.
        /// 추후 시퀀서/알고리즘 실행 상태 API 가 생기면 이 판정을 그것으로 대체.</summary>
        private void UpdateCardState(int i, int now)
        {
            if (_cardStats == null || i >= _cardStats.Length) return;
            bool live = (now - _lastActiveTick[i]) <= LiveTimeoutMs;
            string text = live ? "LIVE" : "READY";
            if (_cardStats[i].Text != text)
            {
                _cardStats[i].Text      = text;
                _cardStats[i].ForeColor = live ? LiveOn : LiveOff;
            }
        }
    }
}
