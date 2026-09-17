using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>역할별 웨이퍼 맵 생성·회전·끝줄 보정. 저장은 호스트의 Recipe 저장 경로로 전달한다.</summary>
    public partial class WaferMapCreateDialog : Form
    {
        private const string QuarterTurnNotice = "90°·270°는 현재 장비에서 사용할 수 없습니다. 미리보기/설정 저장만 가능하며 FINAL APPLY는 차단됩니다.";
        private GeneratedWaferMap _baseMap;
        private GeneratedWaferMap _generatedMap;
        private bool _busy;
        private bool _synchronizing;
        private bool _edgePending;
        private bool _settingsPending;
        private bool _legacyPreview;
        private bool _closing;
        private int _operationVersion;
        private readonly Func<GeneratedWaferMap, string> _saveMap;
        private readonly bool _outputRole;

        public GeneratedWaferMap GeneratedMap
        {
            get { return _busy || _edgePending || _settingsPending ? null : _generatedMap; }
        }

        public bool IsBusy { get { return _busy; } }
        public bool HasSaved { get; private set; }

        public WaferMapCreateDialog()
            : this(new WaferMapGenerationSettings(200m, 5m, 5m, 0m, 0m))
        {
        }

        public WaferMapCreateDialog(WaferMapGenerationSettings settings)
            : this(settings, false, null, null)
        {
        }

        public WaferMapCreateDialog(WaferMapGenerationSettings settings, bool outputRole,
            GeneratedWaferMap savedMap, Func<GeneratedWaferMap, string> saveMap)
        {
            InitializeComponent();
            _outputRole = outputRole;
            _saveMap = saveMap;
            lblTitle.Text = (outputRole ? "OUTPUT" : "INPUT") + " WAFER MAP CREATE";
            Text = (outputRole ? "OUTPUT" : "INPUT") + " · 맵 생성 미리보기 / 저장";
            var notices = new List<string>();
            _synchronizing = true;
            try
            {
                cmbRotation.SelectedIndex = savedMap != null ? savedMap.RotationDegrees / 90 : 0;
                SetInitialValue(numEdgeMargin, settings != null && settings.GenerationVersion >= 2
                    ? settings.EdgeMarginMm : .200m, "내부 여백", notices);
                if (settings != null)
                {
                    SetInitialValue(numDiameter, settings.OuterDiameterMm, "웨이퍼 직경", notices);
                    SetInitialValue(numDieX, settings.DieSizeXMm, "다이 X", notices);
                    SetInitialValue(numDieY, settings.DieSizeYMm, "다이 Y", notices);
                    SetInitialValue(numGapX, settings.GapXMm, "GAP X", notices);
                    SetInitialValue(numGapY, settings.GapYMm, "GAP Y", notices);
                }
            }
            finally { _synchronizing = false; }
            UpdateCenterSteps();
            ClearResult(notices.Count > 0 ? string.Join("\r\n", notices) : "사양을 확인한 뒤 AUTO WAFER CREATE를 누르세요.");
            if (savedMap != null && notices.Count == 0 && MatchesDisplayedSpecification(savedMap))
            {
                _baseMap = savedMap.BaseMap;
                SetEdgeControls(_baseMap);
                SetRequestedEdgeCounts(savedMap.RequestedEdgeCounts ?? savedMap.EdgeCounts);
                numTotalCount.Value = savedMap.RequestedTotalCount ?? savedMap.Count;
                PublishMap(savedMap);
                lblStatus.Text = WithMapNotice("저장된 맵의 회전과 개수 조건을 복원했습니다.", savedMap);
            }
            else if (savedMap != null && notices.Count == 0)
            {
                ClearResult("입력 사양과 저장된 맵 조건이 다릅니다. AUTO WAFER CREATE로 다시 생성하세요.");
            }
        }

        private void settings_ValueChanged(object sender, EventArgs e)
        {
            if (_synchronizing || _busy) return;
            SetMarginMode(false);
            UpdateCenterSteps();
            MarkSettingsPending();
        }

        private void edgeCount_ValueChanged(object sender, EventArgs e)
        {
            if (_synchronizing || _busy || _baseMap == null) return;
            _edgePending = true;
            _generatedMap = null;
            lblStatus.Text = WithMapNotice("입력 조건 미적용 · 저장할 수 없습니다. 직전 결과를 표시합니다. 개수 적용 / 재생성으로 상단 사양과 개수를 함께 적용하세요.", mapView.Map, false);
            UpdateButtons();
        }

        private async void btnGenerate_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            WaferMapGenerationSettings settings = ReadSettings();
            SetMarginMode(false);
            int angle = cmbRotation.SelectedIndex * 90;
            await RunGenerationAsync(() => WaferMapGeneration.Rotate(WaferMapGeneration.Generate(settings), angle), true);
        }

        private async void cmbRotation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_synchronizing || _busy) return;
            UpdateCenterSteps();
            if (_baseMap == null)
            {
                lblStatus.Text = cmbRotation.SelectedIndex == 1 || cmbRotation.SelectedIndex == 3
                    ? QuarterTurnNotice : "사양을 확인한 뒤 AUTO WAFER CREATE를 누르세요.";
                return;
            }
            if (_settingsPending)
            {
                MarkSettingsPending();
                return;
            }
            GeneratedWaferMap source = _baseMap;
            int angle = cmbRotation.SelectedIndex * 90;
            await RunGenerationAsync(() => WaferMapGeneration.Rotate(source, angle), true);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            GeneratedWaferMap map = GeneratedMap;
            if (_saveMap == null || map == null || map.Count == 0 || _busy) return;
            SetBusy(true, "생성한 맵을 저장하는 중입니다...");
            try
            {
                string error = _saveMap(map);
                if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
                HasSaved = true;
                bool configurationOnly = IsQuarterTurn(map);
                lblStatus.Text = WithMapNotice(
                    (_outputRole ? "OUTPUT Base / GOOD / NG" : "INPUT Base / INPUT") +
                    " 맵 " + map.Count.ToString("N0") + (configurationOnly
                        ? "개의 설정을 저장했습니다. PENDING · FINAL APPLY / 장비 사용 차단"
                        : "개를 저장했습니다. 현재 승인은 PENDING입니다.\r\n" +
                            (_outputRole ? "BIN DIE MAP CREATE에서 GOOD·NG 각각" : "INPUT DIE MAP CREATE에서") +
                            " 확인 후 FINAL APPLY 하세요."), map);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "맵 저장 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "UI", "WaferMapCreateDialog.Save", "생성 맵 저장 실패: " + ex);
                QMC.Common.MessageDialog.Show(this, "생성한 맵을 저장할 수 없습니다.\r\n" + ex.Message,
                    "맵 저장", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { SetBusy(false, null); }
        }

        private async void btnApplyEdges_Click(object sender, EventArgs e)
        {
            if (_busy || _baseMap == null || _legacyPreview) return;
            WaferMapGenerationSettings settings = ReadSettings();
            int angle = cmbRotation.SelectedIndex * 90;
            var requested = new WaferMapEdgeCounts((int)numEdgeTop.Value, (int)numEdgeBottom.Value,
                (int)numEdgeLeft.Value, (int)numEdgeRight.Value);
            int total = (int)numTotalCount.Value;
            // 이전 미리보기의 사양 대신 현재 입력 사양으로 기본 맵을 만든 뒤 개수 조건을 적용한다.
            await RunGenerationAsync(() =>
            {
                GeneratedWaferMap original = WaferMapGeneration.Rotate(WaferMapGeneration.Generate(settings), angle);
                return WaferMapGeneration.ApplyCounts(original, requested, total);
            }, false);
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (_busy || _baseMap == null) return;
            RestoreBaseSettings();
            SetEdgeControls(_baseMap);
            PublishMap(_baseMap);
            lblStatus.Text = WithMapNotice("마지막으로 생성한 사양과 회전의 AUTO 결과로 복원했습니다.", _baseMap);
        }

        private void btnFit_Click(object sender, EventArgs e)
        {
            mapView.FitToView();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void WaferMapCreateDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            _closing = true;
            _operationVersion++;
        }

        private void mapView_DieSelected(GeneratedWaferDie die)
        {
            lblSelection.Text = die == null
                ? "다이를 클릭하면 원본 주소와 웨이퍼 중심 기준 위치를 표시합니다.\r\n마우스 휠: 확대/축소 · +Y: 위쪽"
                : "원본 주소: Column " + die.RawColumn + " / Row " + die.RawRow +
                  "\r\n중심 위치: X " + FormatPosition(die.CenterXMm) + " mm / Y " + FormatPosition(die.CenterYMm) + " mm";
            if (die != null && mapView.Map != null && !mapView.Map.IsWithinBoundary(die))
                lblSelection.Text += " · 허용 원 영역 초과";
        }

        private async Task RunGenerationAsync(Func<GeneratedWaferMap> operation, bool generateBase)
        {
            int version = ++_operationVersion;
            _generatedMap = null;
            SetBusy(true, generateBase ? "웨이퍼 맵을 생성하는 중입니다..." : "현재 입력 사양과 끝줄·전체 개수 조건으로 다시 계산하는 중입니다...");
            try
            {
                GeneratedWaferMap result = await Task.Run(operation);
                if (!CanPublish(version)) return;
                _baseMap = result.BaseMap;
                if (generateBase)
                {
                    SetEdgeControls(result);
                }
                PublishMap(result);
                lblStatus.Text = WithMapNotice(result.Count == 0
                    ? "현재 조건에서 AUTO 생성된 다이가 없습니다. 개수 조건을 입력하여 미리볼 수 있습니다."
                    : (result.IsAdjusted ? "개수 조건을 적용했습니다. " : "웨이퍼 맵을 생성했습니다. ") +
                      "생성 결과 " + result.Count.ToString("N0") + "개 · 시계 방향 " + result.RotationDegrees + "°.", result);
            }
            catch (Exception ex)
            {
                if (!CanPublish(version)) return;
                if (generateBase) ClearResult("생성 실패: " + ex.Message);
                else
                {
                    _generatedMap = null;
                    _edgePending = true;
                    lblStatus.Text = WithMapNotice("입력 조건 미적용: " + ex.Message + " 직전 미리보기를 유지하며 저장할 수 없습니다.", mapView.Map, false);
                }
                if (!(ex is ArgumentException) && !(ex is InvalidOperationException) && !(ex is OverflowException))
                {
                    QMC.Common.Log.Write("Main", "UI", "WaferMapCreateDialog", "웨이퍼 맵 미리보기 처리 실패: " + ex);
                    QMC.Common.MessageDialog.Show(this, "웨이퍼 맵 미리보기 처리에 실패했습니다.\r\n" + ex.Message,
                        "Wafer Map Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (CanPublish(version)) SetBusy(false, null);
            }
        }

        private bool CanPublish(int version)
        {
            return !_closing && !IsDisposed && !Disposing && version == _operationVersion;
        }

        private WaferMapGenerationSettings ReadSettings()
        {
            return new WaferMapGenerationSettings(numDiameter.Value, numDieX.Value,
                numDieY.Value, numGapX.Value, numGapY.Value, numEdgeMargin.Value, 4);
        }

        private void MarkSettingsPending()
        {
            _settingsPending = true;
            _generatedMap = null;
            lblStatus.Text = _baseMap == null
                ? "사양을 확인한 뒤 AUTO WAFER CREATE를 누르세요."
                : WithMapNotice("사양 편집 중 · 아직 미적용이므로 저장할 수 없습니다. 직전 결과와 요청 개수를 유지합니다. 개수 적용 / 재생성으로 함께 적용하세요.", mapView.Map, false);
            UpdateButtons();
        }

        private void RestoreBaseSettings()
        {
            WaferMapGenerationSettings settings = _baseMap.Settings;
            _synchronizing = true;
            try
            {
                numDiameter.Value = settings.OuterDiameterMm;
                numDieX.Value = settings.DieSizeXMm;
                numDieY.Value = settings.DieSizeYMm;
                numGapX.Value = settings.GapXMm;
                numGapY.Value = settings.GapYMm;
                numEdgeMargin.Value = settings.GenerationVersion == 1 ? .200m : settings.EdgeMarginMm;
                cmbRotation.SelectedIndex = _baseMap.RotationDegrees / 90;
            }
            finally { _synchronizing = false; }
            UpdateCenterSteps();
        }

        private static bool IsQuarterTurn(GeneratedWaferMap map)
        {
            return map != null && (map.RotationDegrees == 90 || map.RotationDegrees == 270);
        }

        private static string WithMapNotice(string message, GeneratedWaferMap map, bool storageReady = true)
        {
            if (map == null) return message;
            if (map.OutOfBoundsCount > 0)
                message += "\r\n허용 원 영역 초과 " + map.OutOfBoundsCount.ToString("N0") +
                    "개: " + (storageReady ? "설정 저장 가능 · " : "") +
                    "생성된 배치를 유지합니다. 현재 배치 필요 직경 " + map.RequiredOuterDiameterMm.ToString("0.000") + " mm 이상.";
            if (map.Settings.GenerationVersion == 1)
                message += "\r\n이전식 외곽 여유 +0.200 mm입니다. 새 방식은 AUTO 또는 사양 편집 후 개수 적용 / 재생성으로 만드세요.";
            if (IsQuarterTurn(map)) message += "\r\n" + QuarterTurnNotice;
            return message;
        }

        private void SetMarginMode(bool legacy)
        {
            _legacyPreview = legacy;
            lblEdgeMargin.Text = legacy ? "이전식 외곽 여유 (+mm)" : "내부 여백 (mm)";
            toolTip.SetToolTip(numEdgeMargin, legacy
                ? "현재 맵은 이전 수식의 반경 바깥쪽 여유 +0.200 mm를 사용합니다. 값을 편집하거나 AUTO 생성하면 새 내부 여백 방식으로 전환됩니다."
                : "웨이퍼 원에서 안쪽으로 비울 반경 여백입니다. 허용 반경 = 직경/2 - 여백. DIE GAP과 다른 값입니다.");
        }

        private bool MatchesDisplayedSpecification(GeneratedWaferMap map)
        {
            WaferMapGenerationSettings settings = map.Settings;
            return numDiameter.Value == settings.OuterDiameterMm && numDieX.Value == settings.DieSizeXMm &&
                numDieY.Value == settings.DieSizeYMm && numGapX.Value == settings.GapXMm && numGapY.Value == settings.GapYMm &&
                numEdgeMargin.Value == (settings.GenerationVersion == 1 ? .200m : settings.EdgeMarginMm);
        }

        private void PublishMap(GeneratedWaferMap map)
        {
            _generatedMap = map;
            _edgePending = false;
            _settingsPending = false;
            SetMarginMode(map.Settings.GenerationVersion == 1);
            mapView.Map = map;
            lblCounts.Text = "생성 다이: " + map.Count.ToString("N0") + "개" +
                " · 회전 " + map.RotationDegrees + "°" +
                "\r\n현재 맵 격자: " + map.UsedColumns + " columns × " + map.UsedRows + " rows" +
                "\r\n끝줄 실제: 상 " + map.EdgeCounts.Top + " / 하 " + map.EdgeCounts.Bottom +
                " / 좌 " + map.EdgeCounts.Left + " / 우 " + map.EdgeCounts.Right +
                "\r\n" + (map.Settings.GenerationVersion == 1 ? "이전식 외곽 여유: +0.200 mm" :
                    "내부 여백: " + map.Settings.EdgeMarginMm.ToString("0.000") + " mm") +
                "\r\n원 영역 초과: " + map.OutOfBoundsCount.ToString("N0") + "개";
            UpdateButtons();
        }

        private void SetEdgeControls(GeneratedWaferMap map)
        {
            _synchronizing = true;
            try
            {
                SetEdgeControl(numEdgeTop, map.EdgeCounts.Top);
                SetEdgeControl(numEdgeBottom, map.EdgeCounts.Bottom);
                SetEdgeControl(numEdgeLeft, map.EdgeCounts.Left);
                SetEdgeControl(numEdgeRight, map.EdgeCounts.Right);
                numTotalCount.Value = Math.Max(1, map.Count);
            }
            finally { _synchronizing = false; }
        }

        private void SetRequestedEdgeCounts(WaferMapEdgeCounts counts)
        {
            _synchronizing = true;
            try
            {
                numEdgeTop.Value = counts.Top;
                numEdgeBottom.Value = counts.Bottom;
                numEdgeLeft.Value = counts.Left;
                numEdgeRight.Value = counts.Right;
            }
            finally { _synchronizing = false; }
        }

        private void ClearResult(string notice)
        {
            _baseMap = null;
            _generatedMap = null;
            SetMarginMode(false);
            _edgePending = false;
            _settingsPending = false;
            mapView.Map = null;
            _synchronizing = true;
            try
            {
                SetEdgeControl(numEdgeTop, 0);
                SetEdgeControl(numEdgeBottom, 0);
                SetEdgeControl(numEdgeLeft, 0);
                SetEdgeControl(numEdgeRight, 0);
                numTotalCount.Value = 1;
            }
            finally { _synchronizing = false; }
            lblCounts.Text = "생성 다이: —\r\n현재 맵 격자: —\r\n끝줄 실제: —\r\n원 영역 초과: —";
            lblBoundaryStatus.Text = "내부 여백을 뺀 원 안에 다이 전체가 들어오는지 확인합니다. 여백은 DIE GAP과 별개입니다.";
            lblBoundaryStatus.ForeColor = System.Drawing.Color.FromArgb(48, 66, 79);
            lblStatus.Text = notice;
            UpdateButtons();
        }

        private void SetBusy(bool busy, string notice)
        {
            _busy = busy;
            if (notice != null) lblStatus.Text = notice;
            UseWaitCursor = busy;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            groupSettings.Enabled = !_busy;
            btnGenerate.Enabled = !_busy;
            groupEdges.Enabled = !_busy && _baseMap != null && !_legacyPreview;
            btnRestore.Enabled = !_busy && _baseMap != null && (_edgePending || _settingsPending || (_generatedMap != null && _generatedMap.IsAdjusted));
            btnFit.Enabled = mapView.Map != null;
            btnSave.Enabled = !_busy && !_edgePending && !_settingsPending && _generatedMap != null && _generatedMap.Count > 0 &&
                _saveMap != null;
            btnSave.BackColor = btnSave.Enabled ? System.Drawing.Color.FromArgb(230, 88, 31) : System.Drawing.Color.FromArgb(205, 209, 213);
            btnClose.Enabled = !_busy;
            UpdateBoundaryStatus();
        }

        private void UpdateBoundaryStatus()
        {
            GeneratedWaferMap map = mapView.Map;
            if (map == null) return;
            lblBoundaryStatus.Text = map.OutOfBoundsCount > 0
                ? "빨간 다이 " + map.OutOfBoundsCount.ToString("N0") + "개가 허용 원 영역 밖에 있습니다. " +
                    (_settingsPending || _edgePending ? "입력 미적용 · 저장 차단" : "설정 저장 가능") +
                    " · 생성된 배치 유지\r\n현재 배치 필요 직경: " + map.RequiredOuterDiameterMm.ToString("0.000") + " mm 이상"
                : map.Settings.GenerationVersion == 1
                    ? "이전 방식: 외곽 여유 +0.200 mm · 새로 생성하면 내부 여백 방식으로 전환됩니다."
                    : "실선: 웨이퍼 외곽 / 점선: 내부 여백을 뺀 허용 원 · 초록: 내부 / 빨강: 초과";
            lblBoundaryStatus.ForeColor = map.OutOfBoundsCount > 0 ? System.Drawing.Color.Firebrick : System.Drawing.Color.FromArgb(48, 66, 79);
        }

        private void UpdateCenterSteps()
        {
            bool swap = cmbRotation.SelectedIndex == 1 || cmbRotation.SelectedIndex == 3;
            decimal stepX = numDieX.Value + numGapX.Value;
            decimal stepY = numDieY.Value + numGapY.Value;
            txtStepX.Text = (swap ? stepY : stepX).ToString("0.000", CultureInfo.CurrentCulture);
            txtStepY.Text = (swap ? stepX : stepY).ToString("0.000", CultureInfo.CurrentCulture);
        }

        private static void SetInitialValue(NumericUpDown control, decimal value, string label, ICollection<string> notices)
        {
            if (value < control.Minimum || value > control.Maximum || decimal.Round(value, 3) != value)
            {
                control.Value = 0m;
                notices.Add(label + " 초기값 " + value.ToString(CultureInfo.CurrentCulture) +
                    " mm를 그대로 표시할 수 없어 입력하지 않았습니다. 0.001 mm 단위의 유효한 값을 입력하세요.");
                return;
            }
            control.Value = value;
        }

        private static void SetEdgeControl(NumericUpDown control, int count)
        {
            control.Value = Math.Max(1, count);
        }

        private static string FormatPosition(decimal value)
        {
            return value.ToString("0.####", CultureInfo.CurrentCulture);
        }
    }
}
