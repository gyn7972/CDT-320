using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class TapeFrameSubsetPage : SubsetPageBase
    {
        private const string InputWaferText = "INPUT WAFER";
        private const string OutputWaferText = "OUTPUT WAFER";
        private const string EdgeSkipGridText = "GRID COUNT";
        private const string EdgeSkipMmText = "MM";
        private const string EdgeSkipExternalMapText = "EXTERNAL MAP";

        private bool _loadingRole;
        private bool _currentRoleIsOutput;

        public TapeFrameSubsetPage() : base("recipe.tapeFrameSubset")
        {
            InitializeComponent();
            _nGridX.Maximum = 9999;
            _nGridY.Maximum = 9999;
            UpdateEdgeSkipModeUi();
        }

        private void _cbEdgeSkipMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEdgeSkipModeUi();
        }

        protected override void BuildEditor(Panel c)
        {
            // 편집영역 배경 흰색 통일 (그룹박스 배치는 Designer)
            if (c != null)
                c.BackColor = System.Drawing.Color.White;
        }

        protected override void LoadFromRecipe()
        {
            EnsureRecipeFrames();
            _loadingRole = true;
            try
            {
                if (_cbWaferRole.SelectedIndex < 0)
                    _cbWaferRole.SelectedIndex = 0;
                _currentRoleIsOutput = IsOutputRoleSelected();
                TapeFrameSubset frame = ResolveRoleFrame(_currentRoleIsOutput) ?? new TapeFrameSubset();
                RefreshSpecList(frame.FrameSpecName);
                ApplyFrameToControls(frame);
                UpdateMapFileLabel();
            }
            finally
            {
                _loadingRole = false;
            }
        }

        protected override void SaveToRecipe()
        {
            SaveControlsToSelectedRole();
            MaterialStateService.SyncRecipeTapeFrameSpec(_project);
            RefreshSpecList(ResolveRoleFrame(_currentRoleIsOutput)?.FrameSpecName);
            UpdateMapFileLabel();
        }

        private void OnWaferRoleChanged(object sender, EventArgs e)
        {
            if (_loadingRole)
                return;

            try
            {
                if (_project != null)
                    SaveControlsToRole(_currentRoleIsOutput);

                _currentRoleIsOutput = IsOutputRoleSelected();
                TapeFrameSubset frame = ResolveRoleFrame(_currentRoleIsOutput) ?? new TapeFrameSubset();
                RefreshSpecList(frame.FrameSpecName);
                ApplyFrameToControls(frame);
                UpdateMapFileLabel();
            }
            catch
            {
            }
        }

        private void btnLoadSpec_Click(object sender, EventArgs e)
        {
            try
            {
                string specName = _cbSpecLibrary.SelectedItem != null ? _cbSpecLibrary.SelectedItem.ToString() : "";
                var spec = MaterialSpecs.FindFrame(specName);
                if (spec == null)
                {
                    MessageBox.Show("선택된 TapeFrame Spec을 찾을 수 없습니다.", "TapeFrame Spec", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ApplySpecToControls(spec);
            }
            catch (Exception ex)
            {
                MessageBox.Show("TapeFrame Spec 불러오기 실패: " + ex.Message, "TapeFrame Spec", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveSpec_Click(object sender, EventArgs e)
        {
            try
            {
                SaveControlsToSelectedRole();
                MaterialStateService.SyncRecipeTapeFrameSpec(_project);
                RecipeStore.Save(_project);
                RecipeStore.SaveLastProjectName(_project.FileName);
                var host = FindForm() as Form1;
                if (host != null)
                    host.SaveMachineRecipe(_project.FileName);

                TapeFrameSubset frame = ResolveRoleFrame(_currentRoleIsOutput);
                RefreshSpecList(frame != null ? frame.FrameSpecName : "");
                UpdateMapFileLabel();
                MessageBox.Show("TapeFrame Spec 저장 및 Recipe 연동 완료.", "TapeFrame Spec", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("TapeFrame Spec 저장 실패: " + ex.Message, "TapeFrame Spec", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnImportWaferMap_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new OpenFileDialog
                {
                    Title = "Load Wafer Map",
                    Filter = "Wafer map files|*.txt;*.json;*.csv|WaferMap TXT|*.txt|JSON|*.json|CSV|*.csv|All files|*.*",
                    InitialDirectory = ResolveWaferMapInitialDirectory()
                })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return;

                    DieMap map = DieMapGenerator.Load(dlg.FileName);
                    if (map == null)
                    {
                        MessageBox.Show("Wafer Map 파일을 읽을 수 없습니다.", "Wafer Map", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    ApplyImportedMapToControls(map, dlg.FileName);
                    SaveImportedMapToRecipe(map);
                    MessageBox.Show("Wafer Map 불러오기 및 Recipe 연결 완료.\r\n" + BuildImportedMapMessage(), "Wafer Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wafer Map 불러오기 실패: " + ex.Message, "Wafer Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string ResolveWaferMapInitialDirectory()
        {
            string appWaferMapDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "WaferMap");
            if (Directory.Exists(appWaferMapDir))
                return appWaferMapDir;

            string machineWaferMapDir = @"D:\CDT-320\Config\WaferMap";
            if (Directory.Exists(machineWaferMapDir))
                return machineWaferMapDir;

            return RecipeMapPaths.GetDieMapDirectory();
        }

        private void ApplyImportedMapToControls(DieMap map, string sourcePath)
        {
            if (map == null)
                return;

            string baseName = Path.GetFileNameWithoutExtension(sourcePath);
            string roleName = _currentRoleIsOutput ? "OutputWafer" : "InputWafer";
            _tbName.Text = RecipeMapPaths.SanitizeFileName(baseName + "_" + roleName);
            _nGridX.Value = ClampDecimal(map.DieMapX, _nGridX.Minimum, _nGridX.Maximum);
            _nGridY.Value = ClampDecimal(map.DieMapY, _nGridY.Minimum, _nGridY.Maximum);
            _nPitchX.Value = ClampDecimal((decimal)Math.Max(0.001, map.PitchX), _nPitchX.Minimum, _nPitchX.Maximum);
            _nPitchY.Value = ClampDecimal((decimal)Math.Max(0.001, map.PitchY), _nPitchY.Minimum, _nPitchY.Maximum);
            _nDieSizeX.Value = ClampDecimal((decimal)Math.Max(0.001, map.DieSizeX > 0.0 ? map.DieSizeX : map.PitchX), _nDieSizeX.Minimum, _nDieSizeX.Maximum);
            _nDieSizeY.Value = ClampDecimal((decimal)Math.Max(0.001, map.DieSizeY > 0.0 ? map.DieSizeY : map.PitchY), _nDieSizeY.Minimum, _nDieSizeY.Maximum);
            _nDiameter.Value = ClampDecimal((decimal)ResolveOuterDiameter(map), _nDiameter.Minimum, _nDiameter.Maximum);
            SetSelectedEdgeSkipMode(map.EdgeSkipMode);
            _nSideEdgeSkip.Value = ClampDecimal((decimal)Math.Max(0.0, map.SideEdgeSkip), _nSideEdgeSkip.Minimum, _nSideEdgeSkip.Maximum);
            _nTopBottomEdgeSkip.Value = ClampDecimal((decimal)Math.Max(0.0, map.TopBottomEdgeSkip), _nTopBottomEdgeSkip.Minimum, _nTopBottomEdgeSkip.Maximum);
        }

        private void SaveImportedMapToRecipe(DieMap map)
        {
            if (_project == null || map == null)
                return;

            SaveControlsToSelectedRole();

            if (_currentRoleIsOutput)
            {
                SaveImportedMapForKind(map, RecipeMapKind.GoodBin);
                SaveImportedMapForKind(map, RecipeMapKind.NgBin);
            }
            else
            {
                SaveImportedMapForKind(map, RecipeMapKind.Input);
            }

            // 현재 기준: wafer spec import 시 map 파일과 frame spec을 동시에 recipe에 고정한다.
            MaterialStateService.SyncRecipeTapeFrameSpec(_project);
            RecipeStore.Save(_project);
            RecipeStore.SaveLastProjectName(_project.FileName);
            var host = FindForm() as Form1;
            if (host != null)
                host.SaveMachineRecipe(_project.FileName);
            UpdateMapFileLabel();
        }

        private void SaveImportedMapForKind(DieMap map, RecipeMapKind kind)
        {
            string path = RecipeMapPaths.BuildDefaultPath(_project, kind);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            DieMap normalized = DieMapGenerator.Normalize(map);
            normalized.FrameObjId = Path.GetFileNameWithoutExtension(path);
            DieMapGenerator.Save(normalized, path);
            if (!string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
                DieMapGenerator.SaveCsv(normalized, Path.ChangeExtension(path, ".csv"));

            string relativePath = RecipeMapPaths.MakeConfigRelativePath(path);
            RecipeMapPaths.SetConfiguredFileName(_project, kind, relativePath);
        }

        private string BuildImportedMapMessage()
        {
            if (!_currentRoleIsOutput)
                return "Input map: " + (_project != null ? _project.InputDieMapFileName : "-");

            return "Good map: " + (_project != null ? _project.GoodBinDieMapFileName : "-") +
                   "\r\nNG map: " + (_project != null ? _project.NgBinDieMapFileName : "-");
        }

        private void RefreshSpecList(string selectedName)
        {
            try
            {
                string selected = selectedName ?? "";
                _cbSpecLibrary.Items.Clear();
                MaterialSpecs.Load();
                if (MaterialSpecs.Data != null && MaterialSpecs.Data.Frames != null)
                {
                    foreach (var spec in MaterialSpecs.Data.Frames)
                    {
                        if (spec != null && !string.IsNullOrWhiteSpace(spec.Name))
                            _cbSpecLibrary.Items.Add(spec.Name);
                    }
                }

                if (!string.IsNullOrWhiteSpace(selected) && !_cbSpecLibrary.Items.Contains(selected))
                    _cbSpecLibrary.Items.Add(selected);

                if (!string.IsNullOrWhiteSpace(selected))
                    _cbSpecLibrary.SelectedItem = selected;
                if (_cbSpecLibrary.SelectedIndex < 0 && _cbSpecLibrary.Items.Count > 0)
                    _cbSpecLibrary.SelectedIndex = 0;
            }
            catch
            {
            }
        }

        private void ApplySpecToControls(TapeFrameSpec spec)
        {
            if (spec == null)
                return;

            _tbName.Text = spec.Name ?? "";
            _nGridX.Value = ClampDecimal(spec.DieMapX, _nGridX.Minimum, _nGridX.Maximum);
            _nGridY.Value = ClampDecimal(spec.DieMapY, _nGridY.Minimum, _nGridY.Maximum);
            _nPitchX.Value = ClampDecimal((decimal)spec.PitchX, _nPitchX.Minimum, _nPitchX.Maximum);
            _nPitchY.Value = ClampDecimal((decimal)spec.PitchY, _nPitchY.Minimum, _nPitchY.Maximum);
            _nDieSizeX.Value = ClampDecimal((decimal)(spec.DieSizeX > 0.0 ? spec.DieSizeX : spec.PitchX), _nDieSizeX.Minimum, _nDieSizeX.Maximum);
            _nDieSizeY.Value = ClampDecimal((decimal)(spec.DieSizeY > 0.0 ? spec.DieSizeY : spec.PitchY), _nDieSizeY.Minimum, _nDieSizeY.Maximum);
            _nDiameter.Value = ClampDecimal((decimal)spec.OuterDiameterMm, _nDiameter.Minimum, _nDiameter.Maximum);
            SetSelectedEdgeSkipMode(spec.EdgeSkipMode);
            _nSideEdgeSkip.Value = ClampDecimal(GetEdgeSkipControlValue(spec), _nSideEdgeSkip.Minimum, _nSideEdgeSkip.Maximum);
            _nTopBottomEdgeSkip.Value = ClampDecimal(GetTopBottomEdgeSkipControlValue(spec), _nTopBottomEdgeSkip.Minimum, _nTopBottomEdgeSkip.Maximum);
        }

        private void ApplyFrameToControls(TapeFrameSubset frame)
        {
            if (frame == null)
                return;

            _tbName.Text = frame.FrameSpecName ?? "";
            _nGridX.Value = ClampDecimal(frame.DieMapX, _nGridX.Minimum, _nGridX.Maximum);
            _nGridY.Value = ClampDecimal(frame.DieMapY, _nGridY.Minimum, _nGridY.Maximum);
            _nPitchX.Value = ClampDecimal((decimal)frame.PitchX, _nPitchX.Minimum, _nPitchX.Maximum);
            _nPitchY.Value = ClampDecimal((decimal)frame.PitchY, _nPitchY.Minimum, _nPitchY.Maximum);
            _nDieSizeX.Value = ClampDecimal((decimal)(frame.DieSizeX > 0.0 ? frame.DieSizeX : frame.PitchX), _nDieSizeX.Minimum, _nDieSizeX.Maximum);
            _nDieSizeY.Value = ClampDecimal((decimal)(frame.DieSizeY > 0.0 ? frame.DieSizeY : frame.PitchY), _nDieSizeY.Minimum, _nDieSizeY.Maximum);
            _nDiameter.Value = ClampDecimal((decimal)frame.OuterDiameterMm, _nDiameter.Minimum, _nDiameter.Maximum);
            SetSelectedEdgeSkipMode(frame.EdgeSkipMode);
            _nSideEdgeSkip.Value = ClampDecimal(GetEdgeSkipControlValue(frame), _nSideEdgeSkip.Minimum, _nSideEdgeSkip.Maximum);
            _nTopBottomEdgeSkip.Value = ClampDecimal(GetTopBottomEdgeSkipControlValue(frame), _nTopBottomEdgeSkip.Minimum, _nTopBottomEdgeSkip.Maximum);
            _cbRotate.SelectedItem = frame.Rotate ?? "None";
            if (_cbRotate.SelectedIndex < 0) _cbRotate.SelectedIndex = 0;
        }

        private void ApplyControlsToFrame(TapeFrameSubset frame)
        {
            if (frame == null)
                return;

            string edgeMode = GetSelectedEdgeSkipModeName();
            bool mmMode = string.Equals(edgeMode, "Millimeter", StringComparison.OrdinalIgnoreCase);

            frame.FrameSpecName = string.IsNullOrWhiteSpace(_tbName.Text) ? "8inch_5x5" : _tbName.Text.Trim();
            frame.PitchX = (double)_nPitchX.Value;
            frame.PitchY = (double)_nPitchY.Value;
            frame.DieSizeX = (double)_nDieSizeX.Value;
            frame.DieSizeY = (double)_nDieSizeY.Value;
            frame.OuterDiameterMm = (double)_nDiameter.Value;
            frame.DieMapX = ResolvePitchBasedGridCount(frame.OuterDiameterMm, frame.PitchX, (int)_nGridX.Value);
            frame.DieMapY = ResolvePitchBasedGridCount(frame.OuterDiameterMm, frame.PitchY, (int)_nGridY.Value);
            frame.EdgeSkipMode = edgeMode;
            frame.SideEdgeSkip = !mmMode ? Math.Max(0, (int)Math.Floor(_nSideEdgeSkip.Value)) : 0;
            frame.TopBottomEdgeSkip = !mmMode ? Math.Max(0, (int)Math.Floor(_nTopBottomEdgeSkip.Value)) : 0;
            frame.SideEdgeSkipMm = mmMode ? Math.Max(0.0, (double)_nSideEdgeSkip.Value) : 0.0;
            frame.TopBottomEdgeSkipMm = mmMode ? Math.Max(0.0, (double)_nTopBottomEdgeSkip.Value) : 0.0;
            frame.Rotate = _cbRotate.SelectedItem != null ? _cbRotate.SelectedItem.ToString() : "None";
            _nGridX.Value = ClampDecimal(frame.DieMapX, _nGridX.Minimum, _nGridX.Maximum);
            _nGridY.Value = ClampDecimal(frame.DieMapY, _nGridY.Minimum, _nGridY.Maximum);
        }

        private static int ResolvePitchBasedGridCount(double outerDiameterMm, double pitchMm, int fallback)
        {
            if (outerDiameterMm <= 0.0 || pitchMm <= 0.0)
                return Math.Max(1, fallback);

            return DieMapGenerator.CalculateWaferGridCount(outerDiameterMm, pitchMm, pitchMm);
        }

        private void SaveControlsToSelectedRole()
        {
            SaveControlsToRole(_currentRoleIsOutput);
        }

        private void SaveControlsToRole(bool output)
        {
            if (_project == null)
                return;

            TapeFrameSubset frame = EnsureRoleFrame(output);
            ApplyControlsToFrame(frame);
            if (!output)
                _project.Frame = CloneFrame(frame);
        }

        private void EnsureRecipeFrames()
        {
            if (_project == null)
                return;

            if (_project.Frame == null)
                _project.Frame = new TapeFrameSubset();
            if (_project.InputFrame == null)
                _project.InputFrame = CloneFrame(_project.Frame);
            if (_project.OutputFrame == null)
                _project.OutputFrame = CloneFrame(_project.Frame);
        }

        private TapeFrameSubset ResolveRoleFrame(bool output)
        {
            if (_project == null)
                return null;

            return output ? (_project.OutputFrame ?? _project.Frame) : (_project.InputFrame ?? _project.Frame);
        }

        private TapeFrameSubset EnsureRoleFrame(bool output)
        {
            EnsureRecipeFrames();
            if (output)
                return _project.OutputFrame ?? (_project.OutputFrame = CloneFrame(_project.Frame));
            return _project.InputFrame ?? (_project.InputFrame = CloneFrame(_project.Frame));
        }

        private bool IsOutputRoleSelected()
        {
            return _cbWaferRole != null &&
                   _cbWaferRole.SelectedItem != null &&
                   _cbWaferRole.SelectedItem.ToString().IndexOf("OUTPUT", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void UpdateMapFileLabel()
        {
            if (_lblMapFileValue == null || _project == null)
                return;

            _lblMapFileValue.Text = _currentRoleIsOutput
                ? "GOOD=" + EmptyDash(_project.GoodBinDieMapFileName) + " / NG=" + EmptyDash(_project.NgBinDieMapFileName)
                : EmptyDash(_project.InputDieMapFileName);
        }

        private static string EmptyDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        private void UpdateEdgeSkipModeUi()
        {
            if (_cbEdgeSkipMode == null)
                return;

            bool mmMode = GetSelectedEdgeSkipModeName().IndexOf("Millimeter", StringComparison.OrdinalIgnoreCase) >= 0;
            _nSideEdgeSkip.DecimalPlaces = mmMode ? 3 : 0;
            _nTopBottomEdgeSkip.DecimalPlaces = mmMode ? 3 : 0;
            _nSideEdgeSkip.Increment = mmMode ? 0.1M : 1M;
            _nTopBottomEdgeSkip.Increment = mmMode ? 0.1M : 1M;
            decimal max = mmMode ? Math.Max(0.001M, _nDiameter.Value / 2M) : 500M;
            _nSideEdgeSkip.Maximum = max;
            _nTopBottomEdgeSkip.Maximum = max;
        }

        private void SetSelectedEdgeSkipMode(string mode)
        {
            if (_cbEdgeSkipMode == null)
                return;

            string text = EdgeSkipGridText;
            if (!string.IsNullOrWhiteSpace(mode))
            {
                if (mode.IndexOf("EXTERNAL", StringComparison.OrdinalIgnoreCase) >= 0)
                    text = EdgeSkipExternalMapText;
                else if (mode.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         mode.IndexOf("MILLI", StringComparison.OrdinalIgnoreCase) >= 0)
                    text = EdgeSkipMmText;
            }

            _cbEdgeSkipMode.SelectedItem = text;
            if (_cbEdgeSkipMode.SelectedIndex < 0)
                _cbEdgeSkipMode.SelectedIndex = 0;
            UpdateEdgeSkipModeUi();
        }

        private string GetSelectedEdgeSkipModeName()
        {
            string text = _cbEdgeSkipMode != null && _cbEdgeSkipMode.SelectedItem != null
                ? _cbEdgeSkipMode.SelectedItem.ToString()
                : "";
            if (text.IndexOf("EXTERNAL", StringComparison.OrdinalIgnoreCase) >= 0)
                return "ExternalMap";
            if (text.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Millimeter";
            return "Grid";
        }

        private static decimal GetEdgeSkipControlValue(TapeFrameSpec spec)
        {
            bool mmMode = spec != null && !string.IsNullOrWhiteSpace(spec.EdgeSkipMode) &&
                          spec.EdgeSkipMode.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0;
            return (decimal)(mmMode ? spec.SideEdgeSkipMm : spec.SideEdgeSkip);
        }

        private static decimal GetTopBottomEdgeSkipControlValue(TapeFrameSpec spec)
        {
            bool mmMode = spec != null && !string.IsNullOrWhiteSpace(spec.EdgeSkipMode) &&
                          spec.EdgeSkipMode.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0;
            return (decimal)(mmMode ? spec.TopBottomEdgeSkipMm : spec.TopBottomEdgeSkip);
        }

        private static decimal GetEdgeSkipControlValue(TapeFrameSubset frame)
        {
            bool mmMode = frame != null && !string.IsNullOrWhiteSpace(frame.EdgeSkipMode) &&
                          frame.EdgeSkipMode.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0;
            return (decimal)(mmMode ? frame.SideEdgeSkipMm : frame.SideEdgeSkip);
        }

        private static decimal GetTopBottomEdgeSkipControlValue(TapeFrameSubset frame)
        {
            bool mmMode = frame != null && !string.IsNullOrWhiteSpace(frame.EdgeSkipMode) &&
                          frame.EdgeSkipMode.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0;
            return (decimal)(mmMode ? frame.TopBottomEdgeSkipMm : frame.TopBottomEdgeSkip);
        }

        private static TapeFrameSubset CloneFrame(TapeFrameSubset source)
        {
            if (source == null)
                return new TapeFrameSubset();

            return new TapeFrameSubset
            {
                FrameSpecName = source.FrameSpecName,
                DieMapX = source.DieMapX,
                DieMapY = source.DieMapY,
                PitchX = source.PitchX,
                PitchY = source.PitchY,
                DieSizeX = source.DieSizeX,
                DieSizeY = source.DieSizeY,
                Rotate = source.Rotate,
                OuterDiameterMm = source.OuterDiameterMm,
                EdgeSkipMode = source.EdgeSkipMode,
                SideEdgeSkip = source.SideEdgeSkip,
                TopBottomEdgeSkip = source.TopBottomEdgeSkip,
                SideEdgeSkipMm = source.SideEdgeSkipMm,
                TopBottomEdgeSkipMm = source.TopBottomEdgeSkipMm
            };
        }

        private static double ResolveOuterDiameter(DieMap map)
        {
            if (map == null)
                return 200.0;
            if (map.OuterDiameterMm > 0.0)
                return map.OuterDiameterMm;
            if (map.Entries == null || map.Entries.Count == 0)
                return 200.0;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null)
                    continue;
                int x = entry.OriginalMapX >= 0 ? entry.OriginalMapX : entry.DieMapX;
                int y = entry.OriginalMapY >= 0 ? entry.OriginalMapY : entry.DieMapY;
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }

            if (minX == int.MaxValue || minY == int.MaxValue)
                return 200.0;
            return Math.Max((maxX - minX + 1) * Math.Max(0.001, map.PitchX), (maxY - minY + 1) * Math.Max(0.001, map.PitchY));
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
