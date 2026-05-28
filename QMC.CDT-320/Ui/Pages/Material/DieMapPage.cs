using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;

namespace QMC.CDT_320.Ui.Pages.Material
{
    public partial class DieMapPage : PageBase
    {
        private DieMap _map;

        public DieMapPage()
        {
            InitializeComponent();
            WireEvents();

            if (!IsDesignerMode())
            {
                _map = DieMapGenerator.Generate(MakeFrameFromInputs());
                _view.Map = _map;
                UpdateStats();
            }
        }

        private void WireEvents()
        {
            foreach (var r in Enum.GetNames(typeof(TapeFrameRotate))) _cbRotate.Items.Add(r);
            _cbRotate.SelectedIndex = 0;
            _view.CellClicked += OnCellClick;
            btnGenerate.Click += (s, e) => DoGenerate();
            btnDemo.Click += (s, e) => DoFillDemo();
            btnLoad.Click += (s, e) => DoLoad();
            btnSave.Click += (s, e) => DoSave();
        }

        private DieTapeFrame MakeFrameFromInputs()
        {
            return new DieTapeFrame
            {
                ObjId = "DEMO",
                GridX = (int)_nGridX.Value,
                GridY = (int)_nGridY.Value,
                PitchX = (double)_nPitchX.Value,
                PitchY = (double)_nPitchY.Value,
                OriginX = (double)_nOriginX.Value,
                OriginY = (double)_nOriginY.Value,
                Rotate = (TapeFrameRotate)Enum.Parse(typeof(TapeFrameRotate), _cbRotate.SelectedItem.ToString()),
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
                    e.Result = DieResult.Good;
                    e.BinCode = BinCodeMap.GoodBin;
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
                Title = "Load DieMap (JSON or CSV)",
                Filter = "DieMap files|*.json;*.csv|JSON|*.json|CSV|*.csv|All|*.*"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                var loaded = DieMapGenerator.Load(dlg.FileName);
                if (loaded == null)
                {
                    MessageBox.Show("Load failed.");
                    return;
                }

                _map = loaded;
                _view.Map = _map;
                _nGridX.Value = _map.GridX;
                _nGridY.Value = _map.GridY;
                _nPitchX.Value = (decimal)Clamp(_map.PitchX, (double)_nPitchX.Minimum, (double)_nPitchX.Maximum);
                _nPitchY.Value = (decimal)Clamp(_map.PitchY, (double)_nPitchY.Minimum, (double)_nPitchY.Maximum);
                _nOriginX.Value = (decimal)Clamp(_map.OriginX, (double)_nOriginX.Minimum, (double)_nOriginX.Maximum);
                _nOriginY.Value = (decimal)Clamp(_map.OriginY, (double)_nOriginY.Minimum, (double)_nOriginY.Maximum);
                UpdateStats();
            }
        }

        private static double Clamp(double v, double lo, double hi)
            => v < lo ? lo : v > hi ? hi : v;

        private void DoSave()
        {
            if (_map == null)
            {
                MessageBox.Show("No map. Click GENERATE first.");
                return;
            }

            try
            {
                string path = DieMapGenerator.SaveToOutput(_map, lotId: "manual");
                MessageBox.Show("Saved:\n" + path, "DieMap", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message);
            }
        }

        private void UpdateStats()
        {
            if (_map == null)
            {
                _lblStats.Text = "(no map)";
                return;
            }

            int total = _map.Entries.Count;
            int good = _map.Entries.Count(e => e.Result == DieResult.Good);
            int ng = _map.Entries.Count(e => e.Result == DieResult.NG);
            int unk = total - good - ng;
            _lblStats.Text = $"total={total}  good={good}  ng={ng}  unknown={unk}";
        }

        private void OnCellClick(DieMapEntry e)
        {
            _lblCellInfo.Text = $"[{e.GridX},{e.GridY}]  pos=({e.X:F2},{e.Y:F2})mm  result={e.Result}  bin={e.BinCode}  uid={e.DieUid}";
        }
    }
}
