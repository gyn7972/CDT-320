using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Ui.Localization;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>Configuration — GENERAL: 언어 설정 + 백엔드 Provider + Cognex 진단 + Image log saver.
    /// 핸들러 GeneralPage 정렬(헤더 + body TableLayoutPanel + GroupBox 별 TableLayoutPanel).
    /// 정적 shell 은 .Designer.cs, 런타임 데이터/진단/이벤트는 Code.</summary>
    public partial class ConfigurationPage : PageBase
    {
        private bool _initializing;   // 초기 데이터 세팅 중 핸들러 부작용(Switch/Save) 억제

        public ConfigurationPage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            ApplyRuntimeUi();
            LoadConfig();
        }

        /// <summary>i18n 태그 부여 + 현재 언어로 표시 문구 1회 반영. (언어 변경 시 Lang.Apply 가 재적용)</summary>
        private void ApplyRuntimeUi()
        {
            _hdr.Tag           = "i18n:set.general";       _hdr.Text          = Lang.T("set.general");
            _lblLanguage.Tag   = "i18n:set.gen.language";  _lblLanguage.Text  = Lang.T("set.gen.language");
            _provGrp.Tag       = "i18n:set.gen.backend";   _provGrp.Text      = Lang.T("set.gen.backend");
            _lblProvider.Tag   = "i18n:set.gen.provider";  _lblProvider.Text  = Lang.T("set.gen.provider");
            _cgxGrp.Tag        = "i18n:set.gen.cgxDiag";   _cgxGrp.Text       = Lang.T("set.gen.cgxDiag");
            _imgGrp.Tag        = "i18n:set.gen.imageLog";  _imgGrp.Text       = Lang.T("set.gen.imageLog");
            _lblImgPath.Tag    = "i18n:set.gen.path";      _lblImgPath.Text   = Lang.T("set.gen.path");
            _btnCgxRefresh.Tag = "i18n:common.refresh";    _btnCgxRefresh.Text= Lang.T("common.refresh");
            _btnCgxTest.Tag    = "i18n:common.runTest";    _btnCgxTest.Text   = Lang.T("common.runTest");
            _btnBrowse.Tag     = "i18n:common.browse";     _btnBrowse.Text    = Lang.T("common.browse");
        }

        private void LoadConfig()
        {
            _initializing = true;
            try
            {
                // 언어 콤보 — 지원 언어 코드(ko/en/zh-CN/ja). 핸들러 GeneralPage 정렬.
                _cbLang.Items.Clear();
                foreach (var code in Lang.Supported) _cbLang.Items.Add(code);
                string saved = VisionConfigStore.Current.Language;
                if (string.IsNullOrEmpty(saved) || !Lang.HasLanguage(saved)) saved = Lang.Current;
                Lang.SetLanguage(saved);   // Lang.Current 를 저장값과 일치(Form1 훅 부재 시에도 일관).
                _cbLang.SelectedItem = saved;

                foreach (var p in Enum.GetValues(typeof(VisionProvider))) _cbProvider.Items.Add(p);
                _cbProvider.SelectedItem = VisionConfigStore.Current.Provider;
                _lblBackendVer.Text = VisionFactory.Global.VersionInfo;
                _cgxLabel.Text = ProbeCognex();
                _tbImagePath.Text = VisionConfigStore.Current.ImageLogPath;
                _cbImageEnable.Checked = VisionConfigStore.Current.ImageLogEnable;
            }
            finally { _initializing = false; }
        }

        // ── 이벤트 핸들러 (Designer 에서 named 연결) ──

        /// <summary>언어 선택 — Lang 전환 + config 저장 + 최상위 폼 전체 표시 문구 재적용(라이브).</summary>
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            var code = _cbLang.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(code)) return;
            try
            {
                Lang.SetLanguage(code);
                VisionConfigStore.Current.Language = code;
                VisionConfigStore.Save();

                // 최상위 폼이 Lang.LanguageChanged 를 구독하지 않더라도 즉시 반영되도록 직접 Apply.
                var root = (Control)FindForm() ?? this;
                Lang.Apply(root);
            }
            catch (Exception ex)
            {
                MessageBox.Show("언어 변경 실패: " + ex.Message, "Language",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnProviderChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            var sel = (VisionProvider)_cbProvider.SelectedItem;
            VisionFactory.Switch(sel);
            _lblBackendVer.Text = VisionFactory.Global.VersionInfo;
        }

        private void OnCgxRefreshClick(object sender, EventArgs e) => _cgxLabel.Text = ProbeCognex();
        private void OnCgxTestClick(object sender, EventArgs e) => _cgxLabel.Text = RunCognexTest();

        private void OnImagePathChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            VisionConfigStore.Current.ImageLogPath = _tbImagePath.Text;
            VisionConfigStore.Save();
        }

        private void OnBrowseClick(object sender, EventArgs e)
        {
            using (var d = new FolderBrowserDialog { SelectedPath = Directory.Exists(_tbImagePath.Text) ? _tbImagePath.Text : "" })
                if (d.ShowDialog() == DialogResult.OK) _tbImagePath.Text = d.SelectedPath;
        }

        private void OnImageEnableChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            VisionConfigStore.Current.ImageLogEnable = _cbImageEnable.Checked;
            VisionConfigStore.Save();
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

                // Sim 카메라로 grab
                var cam = new QMC.Vision.Cameras.Sim.SimCamera("Sim/Test");
                cam.Open();
                using (var g = cam.Grab(2000))
                {
                    if (!g.IsSuccess) { sb.AppendLine("Grab fail: " + g.ErrorMessage); cam.Dispose(); return sb.ToString(); }
                    sb.AppendLine($"Grab OK  {g.Width}x{g.Height}");

                    // Finder 생성 + Train + Match
                    var finder = be.CreatePatternFinder("Probe/Reticle");
                    finder.SearchRoi = new QMC.Vision.Core.Roi { Name = "Probe.Search", CenterX = 320, CenterY = 240, Width = 400, Height = 300 };
                    finder.TrainRoi  = new QMC.Vision.Core.Roi { Name = "Probe.Train",  CenterX = 320, CenterY = 240, Width = 100, Height = 100 };
                    try
                    {
                        finder.Train(g.Image);
                        var r = finder.Match(g.Image);
                        if (r.Success && r.Best != null)
                        {
                            sb.AppendLine($"Match OK  x={r.Best.CenterX:F2} y={r.Best.CenterY:F2} score={r.Best.Score:F3}");
                        }
                        else
                        {
                            sb.AppendLine("Match: fallback or no result.  err=" + (r.ErrorMessage ?? "-"));
                        }
                    }
                    catch (Exception ex) { sb.AppendLine("Cognex run threw: " + ex.Message); }
                }
                cam.Dispose();
                sb.AppendLine("(license dongle 가 없어도 fallback 으로 ACK 가능 — 실 Cognex 동작은 Match 좌표가 random Sim 값과 다른지로 확인)");
            }
            catch (Exception ex) { sb.AppendLine("Probe error: " + ex.Message); }
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
                    sb.AppendLine("Backend instance: created (current is " + (QMC.Vision.Core.VisionFactory.Global?.Name ?? "null") + ")");
                    sb.AppendLine("Cognex loaded: " + newBe.CognexLoaded);
                    sb.AppendLine(newBe.VersionInfo);
                }
                else
                {
                    sb.AppendLine("Cognex loaded: " + be.CognexLoaded);
                    sb.AppendLine(be.VersionInfo);
                    if (be.CognexLoaded)
                    {
                        sb.Append("Assemblies:");
                        if (be.VisionProAssembly       != null) sb.Append(" Core");
                        if (be.PMAlignAssembly         != null) sb.Append(" PMAlign");
                        if (be.BlobAssembly            != null) sb.Append(" Blob");
                        if (be.CaliperAssembly         != null) sb.Append(" Caliper");
                        if (be.ImageProcessingAssembly != null) sb.Append(" ImageProcessing");
                    }
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
