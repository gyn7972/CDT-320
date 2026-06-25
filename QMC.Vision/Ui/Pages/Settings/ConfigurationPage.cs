using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Ui.Controls;
using QMC.Vision.Ui.Localization;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>Configuration — GENERAL. 섹션마다 주황 밑줄 타이틀 + 그 섹션 전용 ParameterGrid 로 편집.
    /// 값은 자동저장하지 않고 하단 [불러오기]/[저장]으로 일괄 처리. 모든 표시 문구는 언어 설정(Lang) 연동.</summary>
    public partial class ConfigurationPage : PageBase
    {
        private string _cgxText = string.Empty;   // Cognex 진단 표시(읽기전용 Info)
        private string _ocvText = string.Empty;   // OpenCV(EmguCV) 진단 표시(읽기전용 Info)
        private bool   _langHooked;

        public ConfigurationPage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            _cgxText = ProbeCognex();
            _ocvText = ProbeOpenCv();
            BuildGrids();
            Lang.Apply(this);
            if (!_langHooked) { Lang.LanguageChanged += OnLangChanged; _langHooked = true; }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { if (_langHooked) { Lang.LanguageChanged -= OnLangChanged; _langHooked = false; } } catch { }
            base.OnHandleDestroyed(e);
        }

        // ── 섹션별 그리드 구성(DisplayName 은 Lang.T 로 언어 연동) ──
        private void BuildGrids()
        {
            const ParameterGridScope sc = ParameterGridScope.Config;

            // 언어 / 실행 모드
            _g1.SetItems(new List<ParameterGridItem>
            {
                ParameterGridItem.Selection(Lang.T("set.gen.language"), "", sc,
                    () => VisionConfigStore.Current.Language,
                    v => OnLanguagePicked(Convert.ToString(v)),
                    Lang.Supported.Select(c => new ParameterGridOption(c, c))),
                ParameterGridItem.Bool(Lang.T("set.gen.simAuto"), sc,
                    () => VisionConfigStore.Current.SimAutoSequence,
                    v => VisionConfigStore.Current.SimAutoSequence = v),
            });

            // Vision Backend
            _g2.SetItems(new List<ParameterGridItem>
            {
                ParameterGridItem.Selection<VisionProvider>(Lang.T("set.gen.provider"), "", sc,
                    () => VisionConfigStore.Current.Provider,
                    v => VisionConfigStore.Current.Provider = v),
                ParameterGridItem.Info(Lang.T("set.gen.backendVer"), sc,
                    () => VisionFactory.Global.VersionInfo),
            });

            // Cognex 진단
            _g3.SetItems(new List<ParameterGridItem>
            {
                ParameterGridItem.Info(Lang.T("set.gen.cgxOut"), sc, () => _cgxText),
                ParameterGridItem.Action(Lang.T("common.refresh"), Lang.T("common.refresh"), sc,
                    () => { _cgxText = ProbeCognex(); _g3.RefreshValues(); }),
                ParameterGridItem.Action(Lang.T("common.test"), Lang.T("common.test"), sc,
                    () => { _cgxText = RunCognexTest(); _g3.RefreshValues(); }),
            });

            // OpenCV 진단
            _g6.SetItems(new List<ParameterGridItem>
            {
                ParameterGridItem.Info("진단 결과", sc, () => _ocvText),
                ParameterGridItem.Action(Lang.T("common.refresh"), Lang.T("common.refresh"), sc,
                    () => { _ocvText = ProbeOpenCv(); _g6.RefreshValues(); }),
                ParameterGridItem.Action(Lang.T("common.test"), Lang.T("common.test"), sc,
                    () => { _ocvText = RunOpenCvTest(); _g6.RefreshValues(); }),
            });

            // 이미지 로그
            _g4.SetItems(new List<ParameterGridItem>
            {
                ParameterGridItem.FolderPath(Lang.T("set.gen.imgPath"), sc,
                    () => VisionConfigStore.Current.ImageLogPath,
                    v => VisionConfigStore.Current.ImageLogPath = v),
                ParameterGridItem.Bool(Lang.T("set.gen.imgEnable"), sc,
                    () => VisionConfigStore.Current.ImageLogEnable,
                    v => VisionConfigStore.Current.ImageLogEnable = v),
                ParameterGridItem.Bool("리소스 로그(CPU/MEM)", sc,
                    () => VisionConfigStore.Current.ResourceLogEnable,
                    v => VisionConfigStore.Current.ResourceLogEnable = v),
            });

            // 데이터 저장 경로
            _g5.SetItems(new List<ParameterGridItem>
            {
                ParameterGridItem.FolderPath(Lang.T("set.gen.dataRoot"), sc,
                    () => VisionConfigStore.Current.DataRootPath,
                    v => VisionConfigStore.Current.DataRootPath = v),
            });

            SizeGrids();
            ApplyGeneralLayout();
        }

        /// <summary>각 섹션 그리드 행 높이를 내용에 맞춰 고정(행 잘림 방지).</summary>
        private void SizeGrids()
        {
            try
            {
                bodyLayout.RowStyles[1].Height = _g1.PreferredGridHeight;
                bodyLayout.RowStyles[3].Height = _g2.PreferredGridHeight;
                bodyLayout.RowStyles[5].Height = _g3.PreferredGridHeight;
                bodyLayout.RowStyles[7].Height = _g6.PreferredGridHeight;
                bodyLayout.RowStyles[9].Height = _g4.PreferredGridHeight;
                bodyLayout.RowStyles[11].Height = _g5.PreferredGridHeight;
            }
            catch { }
        }

        /// <summary>섹션 제목을 각 그리드의 접기 헤더로 이동하고, 기존 회색 타이틀 행을 제거한다.
        /// Cognex / OpenCV 진단 섹션은 기본 접힘으로 시작한다.</summary>
        private void ApplyGeneralLayout()
        {
            try
            {
                // 섹션 제목을 그리드 접기 헤더(제목바)로 이동
                _g1.Title = Lang.T("set.gen.secLang");
                _g2.Title = Lang.T("set.gen.backend");
                _g3.Title = Lang.T("set.gen.cgxResult");
                _g6.Title = Lang.T("set.gen.ocvResult");
                _g4.Title = Lang.T("set.gen.secImg");
                _g5.Title = Lang.T("set.gen.secData");

                // 기존 회색 섹션 타이틀 라벨 숨김 + 해당 행 높이 제거
                foreach (var lbl in new[] { _t1, _t2, _t3, _t6, _t4, _t5 })
                    lbl.Visible = false;
                foreach (int r in new[] { 0, 2, 4, 6, 8, 10 })
                    bodyLayout.RowStyles[r].Height = 0;

                // Cognex / OpenCV 진단 섹션은 기본 접힘
                _g3.SetCollapsed(true);
                _g6.SetCollapsed(true);
            }
            catch { }
        }

        private void RefreshAllGrids()
        {
            _g1.RefreshValues(); _g2.RefreshValues(); _g3.RefreshValues(); _g6.RefreshValues(); _g4.RefreshValues(); _g5.RefreshValues();
        }

        // ── 언어 선택(그리드) — 라이브 미리보기. 영구 저장은 [저장]. ──
        private void OnLanguagePicked(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || !Lang.HasLanguage(code)) return;
            VisionConfigStore.Current.Language = code;
            Lang.SetLanguage(code);   // → LanguageChanged → OnLangChanged(재구성+재번역)
        }

        private void OnLangChanged()
        {
            if (IsDisposed) return;
            // 항상 BeginInvoke 로 지연 — 그리드 콤보 변경 이벤트 안에서 동기 재빌드 시 행 삭제로 예외 발생(재진입) 방지.
            try { BeginInvoke((Action)RebuildForLanguage); }
            catch { try { RebuildForLanguage(); } catch { } }
        }

        private void RebuildForLanguage()
        {
            BuildGrids();
            var root = (Control)FindForm() ?? this;
            Lang.Apply(root);
        }

        // ── 하단 [불러오기] ──
        private void OnLoadAllClick(object sender, EventArgs e)
        {
            try
            {
                VisionConfigStore.Load();
                string lang = VisionConfigStore.Current.Language;
                if (Lang.HasLanguage(lang)) Lang.SetLanguage(lang);
                BuildGrids();
                RefreshAllGrids();
                var root = (Control)FindForm() ?? this;
                Lang.Apply(root);
            }
            catch (Exception ex)
            {
                MessageBox.Show("불러오기 실패: " + ex.Message, "설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ── 하단 [저장] — 모든 값을 한 번에 저장 + 적용 ──
        private void OnSaveAllClick(object sender, EventArgs e)
        {
            try
            {
                VisionConfigStore.Save();
                VisionFactory.Switch(VisionConfigStore.Current.Provider);
                (FindForm() as Form1)?.ApplySimAutoSequence();
                RefreshAllGrids();
                MessageBox.Show(
                    "저장되었습니다.\n(Provider / 데이터 저장 경로는 재시작 후 반영)",
                    "설정 저장", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("저장 실패: " + ex.Message, "설정 저장",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Cognex 라이선스/실호출 테스트 — Sim 카메라 grab → CogPMAlignTool Train+Match → 결과.</summary>
        private static string RunCognexTest()
        {
            var sb = new System.Text.StringBuilder();
            try
            {
                var be = new QMC.Vision.Backends.Cognex.CognexBackend();
                be.Initialize();
                if (!be.CognexLoaded) { sb.AppendLine("Cognex not loaded — fallback only."); return sb.ToString(); }
                sb.AppendLine("Loaded: " + be.VersionInfo);

                var cam = new QMC.Vision.Cameras.Sim.SimCamera("Sim/Test");
                cam.Open();
                using (var g = cam.Grab(2000))
                {
                    if (!g.IsSuccess) { sb.AppendLine("Grab fail: " + g.ErrorMessage); cam.Dispose(); return sb.ToString(); }
                    sb.AppendLine($"Grab OK  {g.Width}x{g.Height}");

                    var finder = be.CreatePatternFinder("Probe/Reticle");
                    finder.SearchRoi = new QMC.Vision.Core.Roi { Name = "Probe.Search", CenterX = 320, CenterY = 240, Width = 400, Height = 300 };
                    finder.TrainRoi  = new QMC.Vision.Core.Roi { Name = "Probe.Train",  CenterX = 320, CenterY = 240, Width = 100, Height = 100 };
                    try
                    {
                        finder.Train(g.Image);
                        var r = finder.Match(g.Image);
                        if (r.Success && r.Best != null)
                            sb.AppendLine($"Match OK  x={r.Best.CenterX:F2} y={r.Best.CenterY:F2} score={r.Best.Score:F3}");
                        else
                            sb.AppendLine("Match: fallback or no result.  err=" + (r.ErrorMessage ?? "-"));
                    }
                    catch (Exception ex) { sb.AppendLine("Cognex run threw: " + ex.Message); }
                }
                cam.Dispose();
            }
            catch (Exception ex) { sb.AppendLine("Probe error: " + ex.Message); }
            return sb.ToString();
        }

        /// <summary>OpenCV(EmguCV) 실호출 테스트 — Sim grab → 템플릿 Train+Match → 결과.</summary>
        private static string RunOpenCvTest()
        {
            var sb = new System.Text.StringBuilder();
            try
            {
                var be = new QMC.Vision.Backends.OpenCv.OpenCvBackend();
                be.Initialize();
                sb.AppendLine("Loaded: " + be.VersionInfo);
                sb.AppendLine("EmguCV: " + (be.EmguLoaded ? "real" : "BasicFallback"));

                var cam = new QMC.Vision.Cameras.Sim.SimCamera("Sim/Test");
                cam.Open();
                using (var g = cam.Grab(2000))
                {
                    if (!g.IsSuccess) { sb.AppendLine("Grab fail: " + g.ErrorMessage); cam.Dispose(); return sb.ToString(); }
                    sb.AppendLine($"Grab OK  {g.Width}x{g.Height}");

                    var finder = be.CreatePatternFinder("Probe/Reticle");
                    finder.SearchRoi = new QMC.Vision.Core.Roi { Name = "Probe.Search", CenterX = 320, CenterY = 240, Width = 400, Height = 300 };
                    finder.TrainRoi  = new QMC.Vision.Core.Roi { Name = "Probe.Train",  CenterX = 320, CenterY = 240, Width = 100, Height = 100 };
                    try
                    {
                        finder.Train(g.Image);
                        var r = finder.Match(g.Image);
                        if (r.Success && r.Best != null)
                            sb.AppendLine($"Match OK  x={r.Best.CenterX:F2} y={r.Best.CenterY:F2} score={r.Best.Score:F3}");
                        else
                            sb.AppendLine("Match: no result.  err=" + (r.ErrorMessage ?? "-"));

                        var insp = be.CreateInspector("Probe/Surface");
                        insp.InspectionRoi = new QMC.Vision.Core.Roi { Name = "Probe.Insp", CenterX = 320, CenterY = 240, Width = 200, Height = 200 };
                        var ir = insp.Inspect(g.Image);
                        sb.AppendLine("Inspect: " + string.Join(", ", ir.Items.Select(i => i.Name + "=" + i.Value)));
                    }
                    catch (Exception ex) { sb.AppendLine("OpenCV run threw: " + ex.Message); }
                }
                cam.Dispose();
            }
            catch (Exception ex) { sb.AppendLine("Probe error: " + ex.Message); }
            return sb.ToString();
        }

        /// <summary>OpenCV(EmguCV) 어셈블리/네이티브 로드 상태 진단.</summary>
        private static string ProbeOpenCv()
        {
            var sb = new System.Text.StringBuilder();
            try
            {
                var be = new QMC.Vision.Backends.OpenCv.OpenCvBackend();
                be.Initialize();
                sb.AppendLine("EmguCV loaded: " + be.EmguLoaded);
                sb.AppendLine(be.VersionInfo);
            }
            catch (Exception ex)
            {
                sb.AppendLine("Probe error: " + ex.Message);
            }
            return sb.ToString();
        }

        /// <summary>Cognex 어셈블리 로드 상태 + 라이선스 동작 가능 여부 진단.</summary>
        private static string ProbeCognex()
        {
            var sb = new System.Text.StringBuilder();
            try
            {
                var be = QMC.Vision.Core.VisionFactory.Global as QMC.Vision.Backends.Cognex.CognexBackend;
                if (be == null)
                {
                    var newBe = new QMC.Vision.Backends.Cognex.CognexBackend();
                    newBe.Initialize();
                    sb.AppendLine("Backend: " + (QMC.Vision.Core.VisionFactory.Global?.Name ?? "null"));
                    sb.AppendLine("Cognex loaded: " + newBe.CognexLoaded);
                    sb.AppendLine(newBe.VersionInfo);
                }
                else
                {
                    sb.AppendLine("Cognex loaded: " + be.CognexLoaded);
                    sb.AppendLine(be.VersionInfo);
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Probe error: " + ex.Message);
            }
            return sb.ToString();
        }
    }
}
