using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.Bin;
using QMC.CDT320.Materials;

namespace QMC.CDT_320.Ui.Pages.Material
{
    public partial class MaterialBinPage : PageBase
    {
        public MaterialBinPage()
        {
            InitializeComponent();
            WireEvents();
            if (!IsDesignerMode()) LoadGrids();
        }

        private void WireEvents()
        {
            _gridColors.CellFormatting += OnColorRowFormat;
            _gridColors.CellDoubleClick += OnColorPick;
            _btnTest.Click += (s, e) => DoTest();
            _btnReset.Click += (s, e) => ResetDefaults();
            _btnSave.Click += (s, e) => DoSave();
        }

        private void ResetDefaults()
        {
            if (QMC.Common.MessageDialog.Show("Restore default bin mapping?", "Reset", MessageBoxButtons.OKCancel) != DialogResult.OK)
            {
                return;
            }

            BinCodeMap.Data.Codes = BinCodeMapData.CreateDefault().Codes;
            BinCodeMap.Data.Colors = BinCodeMapData.CreateDefault().Colors;
            LoadGrids();
        }

        private void LoadGrids()
        {
            _gridCodes.Rows.Clear();
            if (BinCodeMap.Data.Codes != null)
            {
                foreach (var kv in BinCodeMap.Data.Codes)
                {
                    _gridCodes.Rows.Add(kv.Key, kv.Value.ToString());
                }
            }

            _gridColors.Rows.Clear();
            if (BinCodeMap.Data.Colors != null)
            {
                foreach (var kv in BinCodeMap.Data.Colors.OrderBy(x => x.Key))
                {
                    _gridColors.Rows.Add(kv.Key.ToString(), kv.Value);
                }
            }
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
                e.CellStyle.ForeColor = (c.R + c.G + c.B) > 384
                    ? System.Drawing.Color.Black
                    : System.Drawing.Color.White;
            }
            catch
            {
            }
        }

        private void OnColorPick(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;

            using (var dlg = new ColorDialog())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                string hex = "#" + dlg.Color.R.ToString("X2") + dlg.Color.G.ToString("X2") + dlg.Color.B.ToString("X2");
                _gridColors.Rows[e.RowIndex].Cells[1].Value = hex;
            }
        }

        private void DoTest()
        {
            CommitGridsToData();
            var die = new Die();
            foreach (var c in (_tbTestNg.Text ?? "").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0))
            {
                die.NGCodes.Add(c);
            }

            int bin = BinCodeMap.ConvertToBinCode(die);
            var color = BinCodeMap.ConvertToBinCodeColor(bin);
            _lblTestResult.Text = $"NGCodes={string.Join(",", die.NGCodes)} -> bin={bin}";
            _lblTestResult.BackColor = color;
            _lblTestResult.ForeColor = (color.R + color.G + color.B) > 384
                ? System.Drawing.Color.Black
                : System.Drawing.Color.White;
        }

        private void CommitGridsToData()
        {
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
                QMC.Common.MessageDialog.Show("Saved: Config/bin_codes.json", "BinCode", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("Save failed: " + ex.Message);
            }
        }
    }
}

