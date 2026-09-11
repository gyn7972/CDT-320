using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>독립 웨이퍼 생성 미리보기. Recipe와 장비 상태를 저장하거나 변경하지 않는다.</summary>
    public partial class WaferMapCreateDialog : Form
    {
        private GeneratedWaferMap _baseMap;
        private GeneratedWaferMap _generatedMap;
        private bool _busy;
        private bool _synchronizing;
        private bool _edgePending;
        private bool _closing;
        private int _operationVersion;

        public GeneratedWaferMap GeneratedMap
        {
            get { return _busy || _edgePending ? null : _generatedMap; }
        }

        public bool IsBusy { get { return _busy; } }

        public WaferMapCreateDialog()
            : this(new WaferMapGenerationSettings(200m, 5m, 5m, 0m, 0m))
        {
        }

        public WaferMapCreateDialog(WaferMapGenerationSettings settings)
        {
            InitializeComponent();
            var notices = new List<string>();
            _synchronizing = true;
            try
            {
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
        }

        private void settings_ValueChanged(object sender, EventArgs e)
        {
            if (_synchronizing || _busy) return;
            UpdateCenterSteps();
            ClearResult("사양이 변경되었습니다. AUTO WAFER CREATE로 다시 생성하세요.");
        }

        private void edgeCount_ValueChanged(object sender, EventArgs e)
        {
            if (_synchronizing || _busy || _baseMap == null) return;
            _edgePending = true;
            _generatedMap = null;
            lblStatus.Text = "끝줄 개수 편집 중 · 아직 미적용입니다. 미리보기에는 직전 결과가 표시됩니다. 외곽 보정 적용을 누르세요.";
            UpdateButtons();
        }

        private async void btnGenerate_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            var settings = new WaferMapGenerationSettings(numDiameter.Value, numDieX.Value,
                numDieY.Value, numGapX.Value, numGapY.Value);
            await RunGenerationAsync(() => WaferMapGeneration.Generate(settings), true);
        }

        private async void btnApplyEdges_Click(object sender, EventArgs e)
        {
            if (_busy || _baseMap == null || _baseMap.Count == 0) return;
            GeneratedWaferMap original = _baseMap;
            var requested = new WaferMapEdgeCounts((int)numEdgeTop.Value, (int)numEdgeBottom.Value,
                (int)numEdgeLeft.Value, (int)numEdgeRight.Value);
            await RunGenerationAsync(() => WaferMapGeneration.ApplyEdgeCounts(original, requested), false);
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (_busy || _baseMap == null) return;
            SetEdgeControls(_baseMap);
            PublishMap(_baseMap);
            lblStatus.Text = "최초 생성 원본으로 복원했습니다.";
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
        }

        private async Task RunGenerationAsync(Func<GeneratedWaferMap> operation, bool generateBase)
        {
            int version = ++_operationVersion;
            _generatedMap = null;
            SetBusy(true, generateBase ? "웨이퍼 맵을 생성하는 중입니다..." : "원본 맵에서 외곽 보정을 계산하는 중입니다...");
            try
            {
                GeneratedWaferMap result = await Task.Run(operation);
                if (!CanPublish(version)) return;
                if (generateBase)
                {
                    _baseMap = result;
                    SetEdgeControls(result);
                }
                PublishMap(result);
                lblStatus.Text = result.Count == 0
                    ? "현재 조건에서 생성된 다이가 없습니다. 직경·다이·GAP을 확인하세요."
                    : (result.IsAdjusted ? "외곽 보정을 적용했습니다. " : "원본 웨이퍼 맵을 생성했습니다. ") +
                      "생성 결과 " + result.Count.ToString("N0") + "개 · Recipe 저장 및 장비 적용은 하지 않습니다.";
            }
            catch (Exception ex)
            {
                if (!CanPublish(version)) return;
                if (generateBase) ClearResult("생성 실패: " + ex.Message);
                else
                {
                    _generatedMap = null;
                    _edgePending = true;
                    lblStatus.Text = "외곽 보정 미적용: " + ex.Message;
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

        private void PublishMap(GeneratedWaferMap map)
        {
            _generatedMap = map;
            _edgePending = false;
            mapView.Map = map;
            lblCounts.Text = "생성 다이: " + map.Count.ToString("N0") + "개" +
                "\r\n사용 영역: " + map.UsedColumns + " columns × " + map.UsedRows + " rows" +
                "\r\n후보 격자: " + map.CandidateColumns + " columns × " + map.CandidateRows + " rows" +
                "\r\n끝줄 실제: 상 " + map.EdgeCounts.Top + " / 하 " + map.EdgeCounts.Bottom +
                " / 좌 " + map.EdgeCounts.Left + " / 우 " + map.EdgeCounts.Right;
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
            }
            finally { _synchronizing = false; }
        }

        private void ClearResult(string notice)
        {
            _baseMap = null;
            _generatedMap = null;
            _edgePending = false;
            mapView.Map = null;
            _synchronizing = true;
            try
            {
                SetEdgeControl(numEdgeTop, 0);
                SetEdgeControl(numEdgeBottom, 0);
                SetEdgeControl(numEdgeLeft, 0);
                SetEdgeControl(numEdgeRight, 0);
            }
            finally { _synchronizing = false; }
            lblCounts.Text = "생성 다이: —\r\n사용 영역: —\r\n후보 격자: —\r\n끝줄 실제: —";
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
            groupEdges.Enabled = !_busy && _baseMap != null && _baseMap.Count > 0;
            btnRestore.Enabled = !_busy && _baseMap != null && (_edgePending || (_generatedMap != null && _generatedMap.IsAdjusted));
            btnFit.Enabled = mapView.Map != null;
        }

        private void UpdateCenterSteps()
        {
            txtStepX.Text = (numDieX.Value + numGapX.Value).ToString("0.000", CultureInfo.CurrentCulture);
            txtStepY.Text = (numDieY.Value + numGapY.Value).ToString("0.000", CultureInfo.CurrentCulture);
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
            control.Minimum = 0m;
            control.Maximum = count;
            control.Minimum = count > 0 ? 1m : 0m;
            control.Value = count;
        }

        private static string FormatPosition(decimal value)
        {
            return value.ToString("0.###", CultureInfo.CurrentCulture);
        }
    }
}
