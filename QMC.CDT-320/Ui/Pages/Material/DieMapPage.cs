using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
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
                if (!LoadActiveStageMap(false))
                {
                    _map = DieMapGenerator.Generate(MakeFrameFromInputs());
                    ApplyMapToView(_map, "Generated Demo Die Map");
                }
            }
        }

        private void WireEvents()
        {
            foreach (var r in Enum.GetNames(typeof(TapeFrameRotate))) _cbRotate.Items.Add(r);
            _cbRotate.SelectedIndex = 0;
            _view.CellClicked += OnCellClick;
            btnLoadActive.Click += (s, e) => LoadActiveStageMap(true);
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
                DieMapX = (int)_nGridX.Value,
                DieMapY = (int)_nGridY.Value,
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
            ApplyMapToView(_map, "Generated Demo Die Map");
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
            RefreshEntryGrid();
            UpdateStats();
        }

        private bool LoadActiveStageMap(bool showMessage)
        {
            try
            {
                DieMap map = LotStorage.ActiveInputDieMap;
                string caption = "Active Input Die Map";
                if (map == null)
                {
                    var host = FindForm() as Form1;
                    var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                    if (stage != null && stage.CurrentWaferMap != null)
                    {
                        map = ConvertWaferMap(stage.CurrentWaferMap, stage.OriginX, stage.OriginY, stage.PitchX, stage.PitchY);
                        caption = "InputStage Current Wafer Map";
                    }
                }

                if (map == null)
                {
                    if (showMessage)
                        QMC.Common.MessageDialog.Show(this, "현재 표시할 DieMap/WaferMap 데이터가 없습니다.", "DieMap", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                ApplyMapToView(map, caption);
                return true;
            }
            catch (Exception ex)
            {
                if (showMessage)
                    QMC.Common.MessageDialog.Show(this, "Active DieMap load failed:\n" + ex.Message, "DieMap", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
            }
        }

        private static DieMap ConvertWaferMap(WaferMapData waferMap, double originX, double originY, double pitchX, double pitchY)
        {
            if (waferMap == null)
                return null;

            int rows = waferMap.RowCount > 0 ? waferMap.RowCount : (waferMap.DieMap != null ? waferMap.DieMap.GetLength(0) : 0);
            int cols = waferMap.ColumnCount > 0 ? waferMap.ColumnCount : (waferMap.DieMap != null ? waferMap.DieMap.GetLength(1) : 0);
            if (rows <= 0 || cols <= 0)
                return null;

            var map = new DieMap
            {
                FrameObjId = waferMap.WaferId ?? "",
                DieMapX = cols,
                DieMapY = rows,
                PitchX = pitchX,
                PitchY = pitchY,
                OriginX = originX,
                OriginY = originY,
                CreatedAt = DateTime.Now
            };

            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    bool target = waferMap.DieMap == null || waferMap.DieMap[row, col];
                    map.Entries.Add(new DieMapEntry
                    {
                        Index = index,
                        DieMapX = col,
                        DieMapY = row,
                        IsTarget = target,
                        Result = target ? DieResult.Unknown : DieResult.NG,
                        BinCode = target ? 0 : 255,
                        PosX = originX + col * pitchX,
                        PosY = originY + row * pitchY,
                        DieUid = BuildDisplayDieId(waferMap.WaferId, row, col)
                    });
                    index++;
                }
            }

            return map;
        }

        private void ApplyMapToView(DieMap map, string caption)
        {
            _map = map;
            _view.Caption = string.IsNullOrWhiteSpace(caption) ? "Die Map" : caption;
            _view.Map = _map;
            ApplyMapToInputs(_map);
            RefreshEntryGrid();
            UpdateStats();
        }

        private void ApplyMapToInputs(DieMap map)
        {
            if (map == null)
                return;

            _nGridX.Value = (decimal)Clamp(map.DieMapX, (double)_nGridX.Minimum, (double)_nGridX.Maximum);
            _nGridY.Value = (decimal)Clamp(map.DieMapY, (double)_nGridY.Minimum, (double)_nGridY.Maximum);
            _nPitchX.Value = (decimal)Clamp(map.PitchX, (double)_nPitchX.Minimum, (double)_nPitchX.Maximum);
            _nPitchY.Value = (decimal)Clamp(map.PitchY, (double)_nPitchY.Minimum, (double)_nPitchY.Maximum);
            _nOriginX.Value = (decimal)Clamp(map.OriginX, (double)_nOriginX.Minimum, (double)_nOriginX.Maximum);
            _nOriginY.Value = (decimal)Clamp(map.OriginY, (double)_nOriginY.Minimum, (double)_nOriginY.Maximum);
        }

        private void RefreshEntryGrid()
        {
            try
            {
                _gridEntries.Rows.Clear();
                if (_map == null || _map.Entries == null)
                    return;

                foreach (DieMapEntry entry in _map.Entries)
                {
                    if (entry == null)
                        continue;

                    _gridEntries.Rows.Add(
                        entry.Index,
                        entry.DieMapX,
                        entry.DieMapY,
                        entry.IsTarget ? "Y" : "N",
                        entry.Result,
                        entry.BinCode,
                        entry.PosX.ToString("F4"),
                        entry.PosY.ToString("F4"),
                        entry.DieUid ?? "");
                }
            }
            catch
            {
            }
            finally
            {
            }
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
                    QMC.Common.MessageDialog.Show("Load failed.");
                    return;
                }

                _map = loaded;
                ApplyMapToView(_map, "Loaded Die Map");
            }
        }

        private static double Clamp(double v, double lo, double hi)
            => v < lo ? lo : v > hi ? hi : v;

        private void DoSave()
        {
            if (_map == null)
            {
                QMC.Common.MessageDialog.Show("No map. Click GENERATE first.");
                return;
            }

            try
            {
                string path = DieMapGenerator.SaveToOutput(_map, lotId: "manual");
                QMC.Common.MessageDialog.Show("Saved:\n" + path, "DieMap", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("Save failed: " + ex.Message);
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
            _lblCellInfo.Text = $"[{e.DieMapX},{e.DieMapY}]  pos=({e.PosX:F2},{e.PosY:F2})mm  result={e.Result}  bin={e.BinCode}  uid={e.DieUid}";
            SelectEntryRow(e);
        }

        private void SelectEntryRow(DieMapEntry entry)
        {
            try
            {
                if (entry == null)
                    return;

                foreach (DataGridViewRow row in _gridEntries.Rows)
                {
                    if (row.Cells[0].Value != null && Convert.ToInt32(row.Cells[0].Value) == entry.Index)
                    {
                        row.Selected = true;
                        _gridEntries.FirstDisplayedScrollingRowIndex = Math.Max(0, row.Index);
                        break;
                    }
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static string BuildDisplayDieId(string waferId, int row, int col)
        {
            string prefix = string.IsNullOrWhiteSpace(waferId) ? "WAFER" : waferId;
            return prefix + "-D" + row.ToString("000") + "-" + col.ToString("000");
        }
    }
}

