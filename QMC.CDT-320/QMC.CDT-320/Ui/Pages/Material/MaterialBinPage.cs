using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.Bin;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui;

namespace QMC.CDT_320.Ui.Pages.Material
{
    /// <summary>
    /// BinCode 매핑 편집 페이지 — NG 코드 → bin 번호, bin 번호 → 색상.
    /// 310 BinCodeManagerConfigurationEditor 의 GUI 기능을 우리 스타일로 작성.
    /// </summary>
    public class MaterialBinPage : PageBase
    {
        private DataGridView _gridCodes;
        private DataGridView _gridColors;
        private Button _btnReset, _btnSave, _btnTest;
        private TextBox _tbTestNg;
        private Label _lblTestResult;

        public MaterialBinPage()
        {
            Controls.Add(CreateSectionHeader("material.bin"));
            BuildBody();
            if (!IsDesignerMode()) LoadGrids();
        }

        private void BuildBody()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = UiTheme.OptionHeaderBg };
            top.Controls.Add(new Label
            {
                Text = "BinCode Mapping (NG codes → bin number, bin number → color)",
                Dock = DockStyle.Fill, ForeColor = Color.White, Font = UiTheme.SectionFont,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
            });
            Controls.Add(top);

            // 좌측: NG 코드 → bin 번호
            var codeGroup = new GroupBox
            {
                Location = new Point(10, 50), Size = new Size(820, 540),
                Text = "NG Code → Bin Number  (priority by row order)",
                Font = UiTheme.SectionFont, BackColor = UiTheme.OptionPanelBg
            };
            _gridCodes = new DataGridView
            {
                Location = new Point(10, 28), Size = new Size(800, 500),
                AllowUserToAddRows = true, RowHeadersVisible = false,
                BackgroundColor = Color.White, Font = UiTheme.ValueFont,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50,0x50,0x50), ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                }
            };
            _gridCodes.Columns.Add("NgCode", "NG Code");
            var binCol = new DataGridViewTextBoxColumn { Name = "Bin", HeaderText = "Bin Number" };
            _gridCodes.Columns.Add(binCol);
            codeGroup.Controls.Add(_gridCodes);
            Controls.Add(codeGroup);

            // 우측: bin 번호 → 색상
            var colorGroup = new GroupBox
            {
                Location = new Point(840, 50), Size = new Size(560, 380),
                Text = "Bin Number Range → Color",
                Font = UiTheme.SectionFont, BackColor = UiTheme.OptionPanelBg
            };
            _gridColors = new DataGridView
            {
                Location = new Point(10, 28), Size = new Size(540, 340),
                AllowUserToAddRows = true, RowHeadersVisible = false,
                BackgroundColor = Color.White, Font = UiTheme.ValueFont,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50,0x50,0x50), ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                }
            };
            _gridColors.Columns.Add("BinUpper", "Bin ≤");
            _gridColors.Columns.Add("Color", "Color (#RRGGBB)");
            _gridColors.CellFormatting += OnColorRowFormat;
            _gridColors.CellDoubleClick += OnColorPick;
            colorGroup.Controls.Add(_gridColors);
            Controls.Add(colorGroup);

            // 우측 하단: 빈 매핑 테스트 + 액션 버튼
            var testGroup = new GroupBox
            {
                Location = new Point(840, 440), Size = new Size(560, 150),
                Text = "Test mapping",
                Font = UiTheme.SectionFont, BackColor = UiTheme.OptionPanelBg
            };
            testGroup.Controls.Add(new Label
            {
                Location = new Point(10, 30), Size = new Size(120, 24),
                Text = "NG Codes (CSV)", Font = UiTheme.ButtonFont, TextAlign = ContentAlignment.MiddleLeft
            });
            _tbTestNg = new TextBox
            {
                Location = new Point(140, 28), Size = new Size(280, 26),
                Font = UiTheme.ValueFont, Text = "ChippingTopOver,ForeignOver"
            };
            testGroup.Controls.Add(_tbTestNg);
            _btnTest = new Button
            {
                Location = new Point(430, 26), Size = new Size(110, 30),
                Text = "Test", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = Color.White
            };
            _btnTest.Click += (s, e) => DoTest();
            testGroup.Controls.Add(_btnTest);
            _lblTestResult = new Label
            {
                Location = new Point(10, 64), Size = new Size(530, 28),
                Text = "(test result)",
                Font = new Font("Consolas", 11F, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            };
            testGroup.Controls.Add(_lblTestResult);

            _btnReset = new Button
            {
                Location = new Point(10, 105), Size = new Size(160, 32),
                Text = "Restore defaults", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = Color.LightYellow
            };
            _btnReset.Click += (s, e) =>
            {
                if (MessageBox.Show("Restore default bin mapping?", "Reset",
                                    MessageBoxButtons.OKCancel) != DialogResult.OK) return;
                BinCodeMap.Data.Codes = BinCodeMapData.CreateDefault().Codes;
                BinCodeMap.Data.Colors = BinCodeMapData.CreateDefault().Colors;
                LoadGrids();
            };
            testGroup.Controls.Add(_btnReset);

            _btnSave = new Button
            {
                Location = new Point(180, 105), Size = new Size(360, 32),
                Text = "SAVE  (Config/bin_codes.json)", FlatStyle = FlatStyle.Flat,
                Font = UiTheme.ButtonFont, BackColor = UiTheme.Accent, ForeColor = Color.White
            };
            _btnSave.Click += (s, e) => DoSave();
            testGroup.Controls.Add(_btnSave);

            Controls.Add(testGroup);
        }

        private void LoadGrids()
        {
            _gridCodes.Rows.Clear();
            if (BinCodeMap.Data.Codes != null)
                foreach (var kv in BinCodeMap.Data.Codes)
                    _gridCodes.Rows.Add(kv.Key, kv.Value.ToString());

            _gridColors.Rows.Clear();
            if (BinCodeMap.Data.Colors != null)
                foreach (var kv in BinCodeMap.Data.Colors.OrderBy(x => x.Key))
                    _gridColors.Rows.Add(kv.Key.ToString(), kv.Value);
        }

        private void OnColorRowFormat(object s, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;
            string val = e.Value as string;
            if (string.IsNullOrEmpty(val)) return;
            try
            {
                var c = ColorTranslator.FromHtml(val);
                e.CellStyle.BackColor = c;
                e.CellStyle.ForeColor = (c.R + c.G + c.B) > 384 ? Color.Black : Color.White;
            }
            catch { }
        }

        private void OnColorPick(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;
            using (var dlg = new ColorDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string hex = "#" + dlg.Color.R.ToString("X2") + dlg.Color.G.ToString("X2") + dlg.Color.B.ToString("X2");
                    _gridColors.Rows[e.RowIndex].Cells[1].Value = hex;
                }
            }
        }

        private void DoTest()
        {
            CommitGridsToData();
            var die = new Die();
            foreach (var c in (_tbTestNg.Text ?? "").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0))
                die.NGCodes.Add(c);
            int bin = BinCodeMap.ConvertToBinCode(die);
            var color = BinCodeMap.ConvertToBinCodeColor(bin);
            _lblTestResult.Text = $"NGCodes={string.Join(",", die.NGCodes)} → bin={bin}";
            _lblTestResult.BackColor = color;
            _lblTestResult.ForeColor = (color.R + color.G + color.B) > 384 ? Color.Black : Color.White;
        }

        private void CommitGridsToData()
        {
            // codes
            var codes = new List<KeyValuePair<string, int>>();
            foreach (DataGridViewRow row in _gridCodes.Rows)
            {
                if (row.IsNewRow) continue;
                string k = row.Cells[0].Value as string;
                if (string.IsNullOrWhiteSpace(k)) continue;
                if (!int.TryParse(row.Cells[1].Value as string, out var bin)) continue;
                codes.Add(new KeyValuePair<string, int>(k.Trim(), bin));
            }
            BinCodeMap.Data.Codes = codes;

            // colors
            var colors = new List<KeyValuePair<int, string>>();
            foreach (DataGridViewRow row in _gridColors.Rows)
            {
                if (row.IsNewRow) continue;
                if (!int.TryParse(row.Cells[0].Value as string, out var bin)) continue;
                string hex = (row.Cells[1].Value as string ?? "").Trim();
                if (string.IsNullOrEmpty(hex)) continue;
                colors.Add(new KeyValuePair<int, string>(bin, hex));
            }
            BinCodeMap.Data.Colors = colors;
        }

        private void DoSave()
        {
            try
            {
                CommitGridsToData();
                BinCodeMap.Save();
                MessageBox.Show("Saved: Config/bin_codes.json", "BinCode",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Save failed: " + ex.Message); }
        }
    }
}
