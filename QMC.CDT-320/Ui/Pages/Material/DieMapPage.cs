using System;
using System.Drawing;
using System.IO;
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

        private static readonly Color DieGroupTitleColor = Color.FromArgb(38, 50, 66);

        public DieMapPage()
        {
            InitializeComponent();
            BuildLayout();
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

        /// <summary>비전 얼라인과 동일 구조: 좌 7(상 7 맵 / 하 3 그리드) : 우 3(PARAMETERS/ACTION/RESULT 그룹).
        /// Designer 컨트롤을 재부모화(reparent)해 기능·바인딩 유지. 여백 최소화.</summary>
        private void BuildLayout()
        {
            SuspendLayout();
            try
            {
                this.BackColor = Color.White;
                rootLayout.Padding = new Padding(0);
                lblHeader.Margin = new Padding(0);

                // contentLayout: 좌 70 / 우 30, 좌측은 상 70(맵) / 하 30(그리드)
                contentLayout.BackColor = Color.White;
                contentLayout.Padding = new Padding(0);
                contentLayout.ColumnStyles.Clear();
                contentLayout.ColumnCount = 2;
                contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
                contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
                contentLayout.RowStyles.Clear();
                contentLayout.RowCount = 2;
                contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
                contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
                _view.Margin = new Padding(0, 0, 0, 2);
                _gridEntries.Margin = new Padding(0);

                // 그룹 스타일 통일(흰 배경 + 슬레이트 제목)
                StyleGroup(grpParams, "PARAMETERS");
                StyleGroup(grpActions, "ACTION");
                GroupBox grpResult = new GroupBox();
                StyleGroup(grpResult, "RESULT");

                // ACTION 그룹의 값 라벨(stats/cell info) 분리 → RESULT 그룹
                _lblStats.Parent?.Controls.Remove(_lblStats);
                _lblCellInfo.Parent?.Controls.Remove(_lblCellInfo);

                // actionLayout: 버튼만 남기고 1열 × 5행(46px)으로 쭉 배치
                actionLayout.ColumnStyles.Clear();
                actionLayout.ColumnCount = 1;
                actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                actionLayout.RowStyles.Clear();
                actionLayout.RowCount = 5;
                for (int i = 0; i < 5; i++)
                    actionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
                actionLayout.Padding = new Padding(8, 6, 8, 6);
                actionLayout.SetColumnSpan(btnLoadActive, 1);
                Control[] actBtns = { btnLoadActive, btnGenerate, btnDemo, btnLoad, btnSave };
                for (int i = 0; i < actBtns.Length; i++)
                {
                    actionLayout.SetCellPosition(actBtns[i], new TableLayoutPanelCellPosition(0, i));
                    actBtns[i].Margin = new Padding(3);
                }

                // RESULT 그룹 본문
                var resultLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Margin = new Padding(0),
                    Padding = new Padding(8, 6, 8, 6),
                    ColumnCount = 1,
                    RowCount = 2
                };
                resultLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                resultLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
                resultLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                _lblStats.Dock = DockStyle.Fill;
                _lblStats.Margin = new Padding(0, 0, 0, 4);
                _lblCellInfo.Dock = DockStyle.Fill;
                _lblCellInfo.Margin = new Padding(0);
                resultLayout.Controls.Add(_lblStats, 0, 0);
                resultLayout.Controls.Add(_lblCellInfo, 0, 1);
                grpResult.Controls.Add(resultLayout);

                // rightLayout: PARAMETERS / ACTION / RESULT / 채움
                rightLayout.Controls.Clear();
                rightLayout.RowStyles.Clear();
                rightLayout.ColumnStyles.Clear();
                rightLayout.BackColor = Color.White;
                rightLayout.Margin = new Padding(2, 0, 0, 0);
                rightLayout.ColumnCount = 1;
                rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                rightLayout.RowCount = 4;
                rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 310F));  // PARAMETERS
                rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 264F));  // ACTION (1열 5버튼)
                rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // 채움
                rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));  // RESULT (하단 고정)
                grpParams.Margin = new Padding(0, 0, 0, 4);
                grpActions.Margin = new Padding(0, 0, 0, 4);
                grpResult.Margin = new Padding(0);
                rightLayout.Controls.Add(grpParams, 0, 0);
                rightLayout.Controls.Add(grpActions, 0, 1);
                rightLayout.Controls.Add(grpResult, 0, 3);   // 채움 아래 = 하단
            }
            catch { }
            finally
            {
                ResumeLayout(true);
            }
        }

        private static void StyleGroup(GroupBox g, string text)
        {
            g.Text = text;
            g.Dock = DockStyle.Fill;
            g.BackColor = Color.White;
            g.ForeColor = DieGroupTitleColor;
            g.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
            g.Padding = new Padding(4);
            g.TabStop = false;
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
                        ResolveEntryMapX(entry),
                        ResolveEntryMapY(entry),
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
                Title = "Load DieMap (JSON, CSV or TXT)",
                Filter = "DieMap files|*.json;*.csv;*.txt|JSON|*.json|CSV|*.csv|WaferMap TXT|*.txt|All|*.*",
                InitialDirectory = ResolveMapLoadInitialDirectory()
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

        private static string ResolveMapLoadInitialDirectory()
        {
            string waferMapDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "WaferMap");
            return Directory.Exists(waferMapDir) ? waferMapDir : AppDomain.CurrentDomain.BaseDirectory;
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
            _lblCellInfo.Text = $"[{ResolveEntryMapX(e)},{ResolveEntryMapY(e)}]  pos=({e.PosX:F2},{e.PosY:F2})mm  result={e.Result}  bin={e.BinCode}  uid={e.DieUid}";
            SelectEntryRow(e);
        }

        private static int ResolveEntryMapX(DieMapEntry entry)
        {
            return DieMapGenerator.ResolveMapIndexX(entry);
        }

        private static int ResolveEntryMapY(DieMapEntry entry)
        {
            return DieMapGenerator.ResolveMapIndexY(entry);
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

