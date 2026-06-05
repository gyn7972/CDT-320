using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui;
using QMC.CDT320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.Material
{
    /// <summary>
    /// Wafer Die Map 페이지 — DieMapView + Generate / Load / Save / Stats.
    /// </summary>
    public class DieMapPage : PageBase
    {
        private DieMapView    _view;
        private NumericUpDown _nGridX, _nGridY;
        private NumericUpDown _nPitchX, _nPitchY;
        private NumericUpDown _nOriginX, _nOriginY;
        private ComboBox      _cbRotate;
        private Label         _lblStats, _lblCellInfo;
        private DieMap        _map;

        public DieMapPage()
        {
            Controls.Add(CreateSectionHeader("material.diemap"));
            BuildBody();
            if (!IsDesignerMode())
            {
                _map = DieMapGenerator.Generate(MakeFrameFromInputs());
                _view.Map = _map;
                UpdateStats();
            }
        }

        private void BuildBody()
        {
            // 좌측: DieMapView
            _view = new DieMapView
            {
                Location = new Point(10, 50), Size = new Size(900, 720)
            };
            _view.CellClicked += OnCellClick;
            Controls.Add(_view);

            // 우측: 입력 폼 + 액션
            var inputGroup = new GroupBox
            {
                Location = new Point(920, 50), Size = new Size(480, 360),
                Text = "TapeFrame parameters", Font = UiTheme.SectionFont,
                BackColor = UiTheme.OptionPanelBg
            };
            int row = 0;
            AddInputRow(inputGroup, row++, "Grid X",  out _nGridX,  1, 200, 5);
            AddInputRow(inputGroup, row++, "Grid Y",  out _nGridY,  1, 200, 5);
            AddInputRow(inputGroup, row++, "Pitch X (mm)", out _nPitchX, 0.001m, 100m, 1m, 3);
            AddInputRow(inputGroup, row++, "Pitch Y (mm)", out _nPitchY, 0.001m, 100m, 1m, 3);
            AddInputRow(inputGroup, row++, "Origin X (mm)", out _nOriginX, 0m, 1000m, 0m, 3);
            AddInputRow(inputGroup, row++, "Origin Y (mm)", out _nOriginY, 0m, 1000m, 0m, 3);

            inputGroup.Controls.Add(new Label
            {
                Location = new Point(20, 30 + row * 32), Size = new Size(140, 28),
                Text = "Rotate", Font = UiTheme.ButtonFont, TextAlign = ContentAlignment.MiddleLeft
            });
            _cbRotate = new ComboBox
            {
                Location = new Point(170, 30 + row * 32), Size = new Size(140, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ValueFont
            };
            foreach (var r in Enum.GetNames(typeof(TapeFrameRotate))) _cbRotate.Items.Add(r);
            _cbRotate.SelectedIndex = 0;
            inputGroup.Controls.Add(_cbRotate);
            Controls.Add(inputGroup);

            // 액션 버튼
            var actGroup = new GroupBox
            {
                Location = new Point(920, 420), Size = new Size(480, 200),
                Text = "Actions", Font = UiTheme.SectionFont,
                BackColor = UiTheme.OptionPanelBg
            };
            var btnGen = new Button
            {
                Location = new Point(10, 30), Size = new Size(220, 38),
                Text = "GENERATE", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = UiTheme.Accent, ForeColor = Color.White
            };
            btnGen.Click += (s, e) => DoGenerate();
            actGroup.Controls.Add(btnGen);

            var btnDemo = new Button
            {
                Location = new Point(240, 30), Size = new Size(220, 38),
                Text = "FILL DEMO RESULTS", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = Color.White
            };
            btnDemo.Click += (s, e) => DoFillDemo();
            actGroup.Controls.Add(btnDemo);

            var btnLoad = new Button
            {
                Location = new Point(10, 80), Size = new Size(220, 38),
                Text = "LOAD JSON…", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = Color.White
            };
            btnLoad.Click += (s, e) => DoLoad();
            actGroup.Controls.Add(btnLoad);

            var btnSave = new Button
            {
                Location = new Point(240, 80), Size = new Size(220, 38),
                Text = "SAVE CSV+JSON", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = Color.White
            };
            btnSave.Click += (s, e) => DoSave();
            actGroup.Controls.Add(btnSave);

            _lblStats = new Label
            {
                Location = new Point(10, 130), Size = new Size(450, 24),
                Text = "(stats)", Font = UiTheme.ValueFont,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            };
            actGroup.Controls.Add(_lblStats);

            _lblCellInfo = new Label
            {
                Location = new Point(10, 160), Size = new Size(450, 24),
                Text = "(click a cell to inspect)", Font = UiTheme.ValueFont,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            };
            actGroup.Controls.Add(_lblCellInfo);

            Controls.Add(actGroup);
        }

        private void AddInputRow(GroupBox g, int row, string label, out NumericUpDown n,
                                 decimal min, decimal max, decimal val, int decimals = 0)
        {
            int top = 30 + row * 32;
            g.Controls.Add(new Label
            {
                Location = new Point(20, top), Size = new Size(140, 28),
                Text = label, Font = UiTheme.ButtonFont, TextAlign = ContentAlignment.MiddleLeft
            });
            n = new NumericUpDown
            {
                Location = new Point(170, top), Size = new Size(140, 28),
                Minimum = min, Maximum = max, Value = val, DecimalPlaces = decimals,
                Increment = decimals > 0 ? 0.1m : 1m, Font = UiTheme.ValueFont
            };
            g.Controls.Add(n);
        }

        private DieTapeFrame MakeFrameFromInputs()
        {
            return new DieTapeFrame
            {
                ObjId   = "DEMO",
                GridX   = (int)_nGridX.Value,
                GridY   = (int)_nGridY.Value,
                PitchX  = (double)_nPitchX.Value,
                PitchY  = (double)_nPitchY.Value,
                OriginX = (double)_nOriginX.Value,
                OriginY = (double)_nOriginY.Value,
                Rotate  = (TapeFrameRotate)Enum.Parse(typeof(TapeFrameRotate), _cbRotate.SelectedItem.ToString()),
            };
        }

        private void DoGenerate()
        {
            _map = DieMapGenerator.Generate(MakeFrameFromInputs());
            _view.Map = _map;
            UpdateStats();
        }

        private void DoFillDemo()
        {
            if (_map == null) return;
            var rnd = new Random(42);
            foreach (var e in _map.Entries)
            {
                if (rnd.NextDouble() < 0.85)
                {
                    e.Result = DieResult.Good; e.BinCode = BinCodeMap.GoodBin;
                }
                else
                {
                    e.Result = DieResult.NG;
                    int[] sample = { 110, 111, 112, 120, 130, 102, 105, 255 };
                    e.BinCode = sample[rnd.Next(sample.Length)];
                }
            }
            _view.Invalidate();
            UpdateStats();
        }

        private void DoLoad()
        {
            using (var dlg = new OpenFileDialog
            {
                Title = "Load DieMap (JSON or CSV)", Filter = "DieMap files|*.json;*.csv|JSON|*.json|CSV|*.csv|All|*.*"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var loaded = DieMapGenerator.Load(dlg.FileName);  // Stage 22 — auto-detect (json or csv)
                if (loaded == null) { MessageBox.Show("Load failed."); return; }
                _map = loaded;
                _view.Map = _map;

                // 입력 폼 동기화
                _nGridX.Value   = _map.GridX;
                _nGridY.Value   = _map.GridY;
                _nPitchX.Value  = (decimal)Clamp(_map.PitchX, (double)_nPitchX.Minimum, (double)_nPitchX.Maximum);
                _nPitchY.Value  = (decimal)Clamp(_map.PitchY, (double)_nPitchY.Minimum, (double)_nPitchY.Maximum);
                _nOriginX.Value = (decimal)Clamp(_map.OriginX, (double)_nOriginX.Minimum, (double)_nOriginX.Maximum);
                _nOriginY.Value = (decimal)Clamp(_map.OriginY, (double)_nOriginY.Minimum, (double)_nOriginY.Maximum);
                UpdateStats();
            }
        }

        private static double Clamp(double v, double lo, double hi)
            => v < lo ? lo : v > hi ? hi : v;

        private void DoSave()
        {
            if (_map == null) { MessageBox.Show("No map. Click GENERATE first."); return; }
            try
            {
                string path = DieMapGenerator.SaveToOutput(_map, lotId: "manual");
                MessageBox.Show("Saved:\n" + path, "DieMap",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Save failed: " + ex.Message); }
        }

        private void UpdateStats()
        {
            if (_map == null) { _lblStats.Text = "(no map)"; return; }
            int total = _map.Entries.Count;
            int good  = _map.Entries.Count(e => e.Result == DieResult.Good);
            int ng    = _map.Entries.Count(e => e.Result == DieResult.NG);
            int unk   = total - good - ng;
            _lblStats.Text = $"total={total}  good={good}  ng={ng}  unknown={unk}";
        }

        private void OnCellClick(DieMapEntry e)
        {
            _lblCellInfo.Text = $"[{e.GridX},{e.GridY}]  pos=({e.X:F2},{e.Y:F2})mm  result={e.Result}  bin={e.BinCode}  uid={e.DieUid}";
        }
    }
}
