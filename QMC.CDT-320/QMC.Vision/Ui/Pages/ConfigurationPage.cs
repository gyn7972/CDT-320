using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QMC.Vision.Config;
using QMC.Vision.Core;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>Configuration — 백엔드 Provider 선택 + Image log saver 설정.</summary>
    public class ConfigurationPage : UserControl
    {
        private ComboBox _cbProvider;
        private Label    _lblBackendVer;
        private TextBox  _tbImagePath;
        private CheckBox _cbImageEnable;

        public ConfigurationPage()
        {

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildLayout();
        }

        private void BuildLayout()
        {
            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 30, Text = "Configuration — Module",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            var provGrp = new GroupBox
            {
                Location = new Point(20, 50), Size = new Size(640, 110),
                Text = "Vision Backend (재시작 후 반영)",
                Font = UiTheme.SectionFont
            };
            provGrp.Controls.Add(new Label { Location = new Point(16, 30), AutoSize = true, Text = "Provider", Font = UiTheme.ButtonFont });
            _cbProvider = new ComboBox
            {
                Location = new Point(100, 26), Size = new Size(200, 26),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ButtonFont
            };
            foreach (var p in Enum.GetValues(typeof(VisionProvider))) _cbProvider.Items.Add(p);
            _cbProvider.SelectedItem = VisionConfigStore.Current.Provider;
            _cbProvider.SelectedIndexChanged += (s, e) =>
            {
                var sel = (VisionProvider)_cbProvider.SelectedItem;
                VisionFactory.Switch(sel);
                _lblBackendVer.Text = VisionFactory.Global.VersionInfo;
            };
            provGrp.Controls.Add(_cbProvider);

            _lblBackendVer = new Label
            {
                Location = new Point(16, 66), Size = new Size(600, 30),
                Text = VisionFactory.Global.VersionInfo,
                Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray
            };
            provGrp.Controls.Add(_lblBackendVer);

            Controls.Add(provGrp);

            // Cognex 라이선스 / 어셈블리 진단 패널
            var cgxGrp = new GroupBox
            {
                Location = new Point(680, 50), Size = new Size(560, 110),
                Text = "Cognex VisionPro diagnostics",
                Font = UiTheme.SectionFont
            };
            var cgxLabel = new Label
            {
                Location = new Point(16, 30), Size = new Size(530, 70),
                Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray, Text = ProbeCognex()
            };
            var btnCgxRefresh = new Button
            {
                Location = new Point(330, 78), Size = new Size(100, 24),
                Text = "Refresh", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White
            };
            btnCgxRefresh.Click += (s, e) => cgxLabel.Text = ProbeCognex();
            cgxGrp.Controls.Add(cgxLabel);
            cgxGrp.Controls.Add(btnCgxRefresh);

            var btnCgxTest = new Button
            {
                Location = new Point(440, 78), Size = new Size(100, 24),
                Text = "Run test", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = UiTheme.Accent, ForeColor = Color.White
            };
            btnCgxTest.Click += (s, e) => cgxLabel.Text = RunCognexTest();
            cgxGrp.Controls.Add(btnCgxTest);
            Controls.Add(cgxGrp);

            var imgGrp = new GroupBox
            {
                Location = new Point(20, 180), Size = new Size(640, 100),
                Text = "Image log saver", Font = UiTheme.SectionFont
            };
            imgGrp.Controls.Add(new Label { Location = new Point(16, 30), AutoSize = true, Text = "Path", Font = UiTheme.ButtonFont });
            _tbImagePath = new TextBox
            {
                Location = new Point(56, 26), Size = new Size(440, 26),
                Text = VisionConfigStore.Current.ImageLogPath, Font = UiTheme.ValueFont
            };
            _tbImagePath.TextChanged += (s, e) =>
            {
                VisionConfigStore.Current.ImageLogPath = _tbImagePath.Text;
                VisionConfigStore.Save();
            };
            imgGrp.Controls.Add(_tbImagePath);

            var btnBrowse = new Button
            {
                Location = new Point(504, 24), Size = new Size(100, 30),
                Text = "Browse...", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont
            };
            btnBrowse.Click += (s, e) =>
            {
                using (var d = new FolderBrowserDialog { SelectedPath = Directory.Exists(_tbImagePath.Text) ? _tbImagePath.Text : "" })
                    if (d.ShowDialog() == DialogResult.OK) _tbImagePath.Text = d.SelectedPath;
            };
            imgGrp.Controls.Add(btnBrowse);

            _cbImageEnable = new CheckBox
            {
                Location = new Point(16, 64), AutoSize = true,
                Text = "Enable", Font = UiTheme.ButtonFont,
                Checked = VisionConfigStore.Current.ImageLogEnable
            };
            _cbImageEnable.CheckedChanged += (s, e) =>
            {
                VisionConfigStore.Current.ImageLogEnable = _cbImageEnable.Checked;
                VisionConfigStore.Save();
            };
            imgGrp.Controls.Add(_cbImageEnable);

            Controls.Add(imgGrp);
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
