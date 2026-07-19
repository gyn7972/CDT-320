using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Dialogs
{
    public enum InputStageRunReviewMode
    {
        AlignRecovery,
        MappingReview
    }

    public enum InputStageReviewDieState
    {
        Wait,
        Good,
        Ng,
        Skip
    }

    public enum InputStageReviewJogAxis
    {
        VisionX,
        WaferY,
        WaferT
    }

    public sealed class InputStageReviewJogEventArgs : EventArgs
    {
        public InputStageReviewJogEventArgs(InputStageReviewJogAxis axis, int direction, string speed)
        {
            Axis = axis;
            Direction = direction < 0 ? -1 : 1;
            Speed = speed ?? "Fine";
        }

        public InputStageReviewJogAxis Axis { get; private set; }
        public int Direction { get; private set; }
        public string Speed { get; private set; }
    }

    public sealed class InputStageReviewDieStateEventArgs : EventArgs
    {
        public InputStageReviewDieStateEventArgs(IReadOnlyList<DieMapEntry> entries, InputStageReviewDieState state)
        {
            Entries = entries ?? new List<DieMapEntry>().AsReadOnly();
            State = state;
        }

        public IReadOnlyList<DieMapEntry> Entries { get; private set; }
        public InputStageReviewDieState State { get; private set; }
    }

    public sealed class InputStageReviewPickupOrderEventArgs : EventArgs
    {
        public InputStageReviewPickupOrderEventArgs(
            PickupSubset options,
            DieMapEntry startDie,
            IReadOnlyList<DieMapEntry> orderedEntries)
        {
            Options = options ?? new PickupSubset();
            StartDie = startDie;
            OrderedEntries = orderedEntries ?? new List<DieMapEntry>().AsReadOnly();
        }

        public PickupSubset Options { get; private set; }
        public DieMapEntry StartDie { get; private set; }
        public IReadOnlyList<DieMapEntry> OrderedEntries { get; private set; }
    }

    /// <summary>
    /// Wafer Align/Die Mapping 완료 후 작업자가 맵과 픽업 시작 조건을 검토하는 화면입니다.
    /// 이 Form은 장비를 직접 구동하지 않고, 화면에서 발생한 요청을 이벤트로 전달합니다.
    /// </summary>
    public sealed partial class InputStageRunReviewDialog : Form
    {
        private readonly Dictionary<DieMapEntry, int> _previewSequence =
            new Dictionary<DieMapEntry, int>();
        private readonly List<DieMapEntry> _previewOrder = new List<DieMapEntry>();
        private DieMap _dieMap;
        private DieMapEntry _selectedDie;
        private DieMapEntry _startDie;
        private InputStageRunReviewMode _mode;
        private bool _busy;
        private bool _synchronizingSelection;
        private bool _pickupOrderApplied;
        private bool _alignComplete;
        private bool _mappingComplete;
        private bool _reviewValid;
        private bool _readOnlyPreview;

        public InputStageRunReviewDialog()
        {
            InitializeComponent();
            ConfigureMapView();
            SetMode(InputStageRunReviewMode.MappingReview);
            SetWorkflowState("-", "-", false, false, "-", false, "REVIEW REQUIRED");
            SetAxisPositions(0.0, 0.0, 0.0);
            SetStatus("Wafer Align / Die Mapping 결과를 불러오는 중입니다.");
        }

        public event EventHandler AlignRetryRequested;
        public event EventHandler MappingRetryRequested;
        public event EventHandler MappingSetupRequested;
        public event EventHandler VisionTestRequested;
        public event EventHandler ThetaCorrectionRequested;
        public event EventHandler DieDetectionRequested;
        public event EventHandler OffsetApplyRequested;
        public event EventHandler SelectedDieMoveRequested;
        public event EventHandler StartRunRequested;
        public event EventHandler AbortAutoRequested;
        public event EventHandler JogStopRequested;
        public event EventHandler<InputStageReviewJogEventArgs> JogRequested;
        public event EventHandler<InputStageReviewDieStateEventArgs> DieStateApplyRequested;
        public event EventHandler<InputStageReviewPickupOrderEventArgs> PickupOrderApplyRequested;

        public DieMapEntry SelectedDie { get { return _selectedDie; } }
        public DieMapEntry StartDie { get { return _startDie; } }
        public IReadOnlyList<DieMapEntry> PreviewOrder { get { return _previewOrder.AsReadOnly(); } }

        public void SetMode(InputStageRunReviewMode mode)
        {
            _mode = mode;
            lblDialogMode.Text = mode == InputStageRunReviewMode.AlignRecovery
                ? "ALIGN RECOVERY"
                : "MAPPING REVIEW";
            lblDialogMode.BackColor = mode == InputStageRunReviewMode.AlignRecovery
                ? Color.FromArgb(192, 80, 64)
                : Color.FromArgb(38, 113, 82);
            UpdateActionAvailability();
        }

        public void SetDieMap(DieMap map)
        {
            _dieMap = map;
            _selectedDie = null;
            _startDie = null;
            _pickupOrderApplied = false;
            mapView.SetMap(map, true);
            RefreshPickupPreview();
            RefreshMapInformation();
            SetStatus(map == null
                ? "표시할 Input Die Map이 없습니다."
                : "Input Die Map을 불러왔습니다. 시작 Die와 픽업 경로를 확인하세요.");
        }

        public void SetWorkflowState(
            string waferId,
            string recipeName,
            bool visionConnected,
            bool alignComplete,
            string mappingRevision,
            bool mappingComplete,
            string reviewState)
        {
            lblWaferValue.Text = string.IsNullOrWhiteSpace(waferId) ? "-" : waferId;
            lblRecipeValue.Text = string.IsNullOrWhiteSpace(recipeName) ? "-" : recipeName;
            lblVisionValue.Text = visionConnected ? "CONNECTED" : "DISCONNECTED";
            lblVisionValue.ForeColor = visionConnected ? Color.LightGreen : Color.LightSalmon;
            _alignComplete = alignComplete;
            _mappingComplete = mappingComplete;
            _reviewValid = false;
            lblAlignValue.Text = alignComplete ? "COMPLETE" : "REQUIRED";
            lblAlignValue.ForeColor = alignComplete ? Color.LightGreen : Color.Khaki;
            lblMappingValue.Text = mappingComplete ? "COMPLETE" : "REQUIRED";
            lblMappingValue.ForeColor = mappingComplete ? Color.LightGreen : Color.Khaki;
            lblMappingRevisionValue.Text = string.IsNullOrWhiteSpace(mappingRevision) ? "-" : mappingRevision;
            lblReviewValue.Text = string.IsNullOrWhiteSpace(reviewState) ? "REVIEW REQUIRED" : reviewState;
            UpdateActionAvailability();
        }

        public void SetReviewValid(bool valid, string reviewState)
        {
            _reviewValid = valid;
            lblReviewValue.Text = string.IsNullOrWhiteSpace(reviewState)
                ? (valid ? "READY TO START" : "REVIEW REQUIRED")
                : reviewState;
            lblReviewValue.ForeColor = valid ? Color.LightGreen : Color.Khaki;
            UpdateActionAvailability();
        }

        public void SetPickupOptions(PickupSubset options)
        {
            PickupSubset source = options ?? new PickupSubset();
            rbCornerTopLeft.Checked = source.StartCorner == PickupStartCorner.TopLeft;
            rbCornerBottomLeft.Checked = source.StartCorner == PickupStartCorner.BottomLeft;
            rbCornerBottomRight.Checked = source.StartCorner == PickupStartCorner.BottomRight;
            rbCornerTopRight.Checked = source.StartCorner == PickupStartCorner.TopRight;
            rbDirectionHorizontal.Checked = source.Direction == PickupDirection.Horizontal;
            rbDirectionVertical.Checked = source.Direction == PickupDirection.Vertical;
            rbPatternStraight.Checked = source.Pattern == PickupPattern.Straight;
            rbPatternZigZag.Checked = source.Pattern == PickupPattern.ZigZag;
            RefreshPickupPreview();
        }

        public void SetReadOnlyPreview(bool readOnly)
        {
            _readOnlyPreview = readOnly;
            UpdateActionAvailability();
            if (readOnly)
                SetStatus("현재 Stage Wafer/DieMap의 읽기 전용 화면입니다. 모션 및 데이터 변경 기능은 연결되지 않았습니다.");
        }

        public void SetFailureDetail(string alarmCode, string detail)
        {
            txtFailureDetail.Text = string.IsNullOrWhiteSpace(alarmCode)
                ? (detail ?? string.Empty)
                : alarmCode + Environment.NewLine + (detail ?? string.Empty);
        }

        public void SetAxisPositions(double visionX, double waferY, double waferT)
        {
            lblVisionXValue.Text = visionX.ToString("F3");
            lblWaferYValue.Text = waferY.ToString("F3");
            lblWaferTValue.Text = waferT.ToString("F4");
            lblInputCameraXValue.Text = visionX.ToString("F3");
            lblInputStageYValue.Text = waferY.ToString("F3");
        }

        public void SetBusy(bool busy, string status)
        {
            _busy = busy;
            if (!string.IsNullOrWhiteSpace(status))
                SetStatus(status);
            UpdateActionAvailability();
        }

        private void ConfigureMapView()
        {
            mapView.Caption = "INPUT WAFER MAP";
            mapView.ShowWaferOutline = true;
            mapView.ShowEquipmentAxes = true;
            mapView.CompactUsedBounds = true;
            mapView.EnableRectangleSelection = true;
            mapView.CellColorResolver = ResolveMapCellColor;
            mapView.CellTextResolver = ResolveMapCellText;
            mapView.CellStatusResolver = BuildMapCellStatus;
            mapView.LegendItemsResolver = BuildLegendItems;
        }

        private Color ResolveMapCellColor(DieMapEntry entry)
        {
            if (entry == null)
                return Color.DimGray;
            if (ReferenceEquals(entry, _startDie))
                return Color.FromArgb(245, 190, 52);
            if (!entry.IsTarget)
                return Color.FromArgb(90, 90, 90);
            if (entry.Result == DieResult.Good)
                return Color.FromArgb(55, 176, 116);
            if (entry.Result == DieResult.NG)
                return Color.FromArgb(214, 91, 91);
            return Color.FromArgb(188, 216, 239);
        }

        private string ResolveMapCellText(DieMapEntry entry)
        {
            if (entry == null)
                return string.Empty;
            if (ReferenceEquals(entry, _startDie))
                return "S";

            int sequence;
            return _previewSequence.TryGetValue(entry, out sequence)
                ? sequence.ToString()
                : string.Empty;
        }

        private string BuildMapCellStatus(DieMapEntry entry)
        {
            if (entry == null)
                return string.Empty;

            int sequence;
            _previewSequence.TryGetValue(entry, out sequence);
            return "Sequence=" + (sequence > 0 ? sequence.ToString() : "-") +
                   ", Map=(" + entry.DieMapX + "," + entry.DieMapY + ")" +
                   ", EquipmentGrid=(" + FormatGrid(entry.EquipmentGridX) + "," + FormatGrid(entry.EquipmentGridY) + ")" +
                   ", Position=(" + entry.PosX.ToString("F3") + "," + entry.PosY.ToString("F3") + ")" +
                   ", UID=" + (entry.DieUid ?? "");
        }

        private Tuple<string, Color>[] BuildLegendItems()
        {
            return new[]
            {
                Tuple.Create("WAIT", Color.FromArgb(188, 216, 239)),
                Tuple.Create("START", Color.FromArgb(245, 190, 52)),
                Tuple.Create("GOOD", Color.FromArgb(55, 176, 116)),
                Tuple.Create("NG", Color.FromArgb(214, 91, 91)),
                Tuple.Create("SKIP", Color.FromArgb(90, 90, 90))
            };
        }

        private void RefreshPickupPreview()
        {
            _previewOrder.Clear();
            _previewSequence.Clear();

            if (_dieMap != null)
            {
                List<DieMapEntry> ordered = PickupSequenceGenerator.Build(_dieMap, BuildPickupOptions());
                if (chkUseSelectedStart.Checked && _startDie != null)
                    ordered = RotateOrderAtStartDie(ordered, _startDie);

                for (int i = 0; i < ordered.Count; i++)
                {
                    DieMapEntry entry = ordered[i];
                    if (entry == null)
                        continue;
                    _previewOrder.Add(entry);
                    _previewSequence[entry] = _previewOrder.Count;
                }
            }

            RefreshDieGrid();
            RefreshProgress();
            mapView.Invalidate();
            _pickupOrderApplied = false;
            UpdateActionAvailability();
        }

        private PickupSubset BuildPickupOptions()
        {
            var options = new PickupSubset();
            if (rbCornerTopLeft.Checked)
                options.StartCorner = PickupStartCorner.TopLeft;
            else if (rbCornerBottomLeft.Checked)
                options.StartCorner = PickupStartCorner.BottomLeft;
            else if (rbCornerBottomRight.Checked)
                options.StartCorner = PickupStartCorner.BottomRight;
            else
                options.StartCorner = PickupStartCorner.TopRight;

            options.Direction = rbDirectionVertical.Checked
                ? PickupDirection.Vertical
                : PickupDirection.Horizontal;
            options.Pattern = rbPatternStraight.Checked
                ? PickupPattern.Straight
                : PickupPattern.ZigZag;
            return options;
        }

        private static List<DieMapEntry> RotateOrderAtStartDie(List<DieMapEntry> source, DieMapEntry startDie)
        {
            if (source == null || source.Count == 0 || startDie == null)
                return source ?? new List<DieMapEntry>();

            int startIndex = source.FindIndex(entry => IsSameEntry(entry, startDie));
            if (startIndex <= 0)
                return source;

            var rotated = new List<DieMapEntry>(source.Count);
            for (int i = startIndex; i < source.Count; i++)
                rotated.Add(source[i]);
            for (int i = 0; i < startIndex; i++)
                rotated.Add(source[i]);
            return rotated;
        }

        private void RefreshDieGrid()
        {
            _synchronizingSelection = true;
            try
            {
                dieGrid.Rows.Clear();
                IEnumerable<DieMapEntry> entries = _dieMap != null && _dieMap.Entries != null
                    ? _dieMap.Entries
                    : Enumerable.Empty<DieMapEntry>();

                foreach (DieMapEntry entry in entries
                    .Where(item => item != null)
                    .OrderBy(item => ResolvePreviewSequence(item) <= 0 ? int.MaxValue : ResolvePreviewSequence(item))
                    .ThenBy(item => item.DieMapY)
                    .ThenBy(item => item.DieMapX))
                {
                    int rowIndex = dieGrid.Rows.Add(
                        ResolvePreviewSequence(entry) > 0 ? ResolvePreviewSequence(entry).ToString() : "-",
                        entry.DieMapX,
                        entry.DieMapY,
                        FormatGrid(entry.EquipmentGridX),
                        FormatGrid(entry.EquipmentGridY),
                        entry.OriginalMapX >= 0 ? entry.OriginalMapX.ToString() : "-",
                        entry.OriginalMapY >= 0 ? entry.OriginalMapY.ToString() : "-",
                        ResolveDieStateText(entry),
                        entry.Result,
                        entry.BinCode,
                        entry.PosX.ToString("F4"),
                        entry.PosY.ToString("F4"),
                        entry.DieUid ?? string.Empty);
                    dieGrid.Rows[rowIndex].Tag = entry;
                    if (IsSameEntry(entry, _selectedDie))
                        dieGrid.Rows[rowIndex].Selected = true;
                }
            }
            finally
            {
                _synchronizingSelection = false;
            }
        }

        private void RefreshProgress()
        {
            int total = _dieMap != null && _dieMap.Entries != null
                ? _dieMap.Entries.Count(entry => entry != null && entry.IsTarget)
                : 0;
            int complete = _dieMap != null && _dieMap.Entries != null
                ? _dieMap.Entries.Count(entry => entry != null && entry.IsTarget &&
                    (entry.Result == DieResult.Good || entry.Result == DieResult.NG))
                : 0;
            lblMapGridValue.Text = _dieMap == null ? "-" : _dieMap.DieMapX + " x " + _dieMap.DieMapY;
            lblMapProgressValue.Text = complete + " / " + total;
            lblTargetCountValue.Text = total.ToString();
        }

        private void RefreshMapInformation()
        {
            lblDieSizeXValue.Text = _dieMap != null ? _dieMap.DieSizeX.ToString("F4") : "-";
            lblDieSizeYValue.Text = _dieMap != null ? _dieMap.DieSizeY.ToString("F4") : "-";
            lblPitchGapXValue.Text = _dieMap != null ? (_dieMap.PitchX - _dieMap.DieSizeX).ToString("F4") : "-";
            lblPitchGapYValue.Text = _dieMap != null ? (_dieMap.PitchY - _dieMap.DieSizeY).ToString("F4") : "-";
            lblWaferDiameterValue.Text = _dieMap != null ? _dieMap.OuterDiameterMm.ToString("F3") : "-";
            lblMappingOriginValue.Text = _dieMap != null
                ? _dieMap.OriginX.ToString("F3") + " / " + _dieMap.OriginY.ToString("F3")
                : "-";
            RefreshSelectedDieInformation();
        }

        private void RefreshSelectedDieInformation()
        {
            DieMapEntry entry = _selectedDie;
            lblSelectedDieValue.Text = entry != null
                ? (string.IsNullOrWhiteSpace(entry.DieUid) ? "-" : entry.DieUid)
                : "-";
            lblEquipmentGridValue.Text = entry != null
                ? FormatGrid(entry.EquipmentGridX) + " / " + FormatGrid(entry.EquipmentGridY)
                : "-";
            lblOriginalMapValue.Text = entry != null
                ? (entry.OriginalMapX >= 0 ? entry.OriginalMapX.ToString() : "-") + " / " +
                  (entry.OriginalMapY >= 0 ? entry.OriginalMapY.ToString() : "-")
                : "-";
            lblSelectedPositionValue.Text = entry != null
                ? entry.PosX.ToString("F3") + " / " + entry.PosY.ToString("F3")
                : "-";
            lblSelectedSequenceValue.Text = entry != null && ResolvePreviewSequence(entry) > 0
                ? ResolvePreviewSequence(entry).ToString()
                : "-";
            lblStartDieValue.Text = _startDie != null
                ? (string.IsNullOrWhiteSpace(_startDie.DieUid)
                    ? "Map " + _startDie.DieMapX + "," + _startDie.DieMapY
                    : _startDie.DieUid)
                : "NOT SET";
        }

        private int ResolvePreviewSequence(DieMapEntry entry)
        {
            int sequence;
            return entry != null && _previewSequence.TryGetValue(entry, out sequence) ? sequence : 0;
        }

        private static string ResolveDieStateText(DieMapEntry entry)
        {
            if (entry == null || !entry.IsTarget)
                return "SKIP";
            if (entry.Result == DieResult.Good)
                return "GOOD";
            if (entry.Result == DieResult.NG)
                return "NG";
            return "WAIT";
        }

        private static bool IsSameEntry(DieMapEntry left, DieMapEntry right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;
            if (!string.IsNullOrWhiteSpace(left.DieUid) && !string.IsNullOrWhiteSpace(right.DieUid))
                return string.Equals(left.DieUid, right.DieUid, StringComparison.OrdinalIgnoreCase);
            return left.DieMapX == right.DieMapX && left.DieMapY == right.DieMapY;
        }

        private static string FormatGrid(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? "-" : value.ToString("0.###");
        }

        private void SelectDie(DieMapEntry entry, bool selectGrid)
        {
            _selectedDie = entry;
            mapView.SelectedEntry = entry;
            RefreshSelectedDieInformation();

            if (!selectGrid || entry == null)
                return;

            _synchronizingSelection = true;
            try
            {
                foreach (DataGridViewRow row in dieGrid.Rows)
                {
                    bool selected = IsSameEntry(row.Tag as DieMapEntry, entry);
                    row.Selected = selected;
                    if (selected)
                        dieGrid.FirstDisplayedScrollingRowIndex = Math.Max(0, row.Index);
                }
            }
            finally
            {
                _synchronizingSelection = false;
            }
        }

        private void MapView_CellClicked(DieMapEntry entry)
        {
            SelectDie(entry, true);
        }

        private void MapView_CellDoubleClicked(DieMapEntry entry)
        {
            SelectDie(entry, true);
            if (!_readOnlyPreview)
                RaiseSimpleEvent(SelectedDieMoveRequested);
        }

        private void MapView_SelectionRectangleCompleted(IReadOnlyList<DieMapEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return;
            SelectDie(entries[0], true);
            SetStatus(entries.Count + "개 Die가 선택되었습니다. 상태 변경 시 전체 선택 대상에 적용됩니다.");
        }

        private void DieGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (_synchronizingSelection || dieGrid.SelectedRows.Count == 0)
                return;
            SelectDie(dieGrid.SelectedRows[0].Tag as DieMapEntry, false);
        }

        private void PickupOption_CheckedChanged(object sender, EventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio != null && !radio.Checked)
                return;
            RefreshPickupPreview();
            SetStatus("픽업 경로 설정이 변경되었습니다. PREVIEW를 확인한 뒤 APPLY PICKUP ORDER를 실행하세요.");
        }

        private void ChkUseSelectedStart_CheckedChanged(object sender, EventArgs e)
        {
            RefreshPickupPreview();
        }

        private void BtnSetStartDie_Click(object sender, EventArgs e)
        {
            if (_selectedDie == null)
            {
                SetStatus("시작할 Die를 Wafer Map 또는 목록에서 먼저 선택하세요.");
                return;
            }
            if (!_selectedDie.IsTarget)
            {
                SetStatus("SKIP/비대상 Die는 시작 Die로 설정할 수 없습니다.");
                return;
            }

            _startDie = _selectedDie;
            chkUseSelectedStart.Checked = true;
            RefreshPickupPreview();
            RefreshSelectedDieInformation();
            SetStatus("선택 Die를 시작점으로 설정했습니다. UID=" + (_startDie.DieUid ?? "") +
                      ", Map=(" + _startDie.DieMapX + "," + _startDie.DieMapY + ")");
        }

        private void BtnSetStartIndex_Click(object sender, EventArgs e)
        {
            int requested = (int)numStartIndex.Value;
            DieMapEntry entry = _previewOrder.FirstOrDefault(item => ResolvePreviewSequence(item) == requested);
            if (entry == null)
            {
                SetStatus("입력한 순번에 해당하는 Target Die가 없습니다. sequence=" + requested);
                return;
            }

            SelectDie(entry, true);
            BtnSetStartDie_Click(sender, e);
        }

        private void BtnPreviewPath_Click(object sender, EventArgs e)
        {
            RefreshPickupPreview();
            SetStatus("픽업 경로 미리보기를 갱신했습니다. Target=" + _previewOrder.Count +
                      ", Start=" + (_startDie != null ? lblStartDieValue.Text : "Recipe Corner"));
        }

        private void BtnApplyPickupOrder_Click(object sender, EventArgs e)
        {
            if (_previewOrder.Count == 0)
            {
                SetStatus("적용할 Target Die 순서가 없습니다.");
                return;
            }
            if (chkUseSelectedStart.Checked && _startDie == null)
            {
                SetStatus("선택 시작 Die 사용이 켜져 있지만 시작 Die가 지정되지 않았습니다.");
                return;
            }

            var handler = PickupOrderApplyRequested;
            if (handler != null)
                handler(this, new InputStageReviewPickupOrderEventArgs(
                    BuildPickupOptions(),
                    _startDie,
                    new List<DieMapEntry>(_previewOrder).AsReadOnly()));
            _pickupOrderApplied = true;
            SetStatus("픽업 경로 적용 요청을 완료했습니다. Target=" + _previewOrder.Count);
            UpdateActionAvailability();
        }

        private void BtnApplyDieState_Click(object sender, EventArgs e)
        {
            List<DieMapEntry> entries = mapView.SelectedEntries != null
                ? mapView.SelectedEntries.Where(item => item != null).ToList()
                : new List<DieMapEntry>();
            if (entries.Count == 0 && _selectedDie != null)
                entries.Add(_selectedDie);
            if (entries.Count == 0)
            {
                SetStatus("상태를 변경할 Die를 선택하세요.");
                return;
            }

            InputStageReviewDieState state = rbDieStateGood.Checked
                ? InputStageReviewDieState.Good
                : rbDieStateNg.Checked
                    ? InputStageReviewDieState.Ng
                    : rbDieStateSkip.Checked
                        ? InputStageReviewDieState.Skip
                        : InputStageReviewDieState.Wait;
            var handler = DieStateApplyRequested;
            if (handler != null)
                handler(this, new InputStageReviewDieStateEventArgs(entries.AsReadOnly(), state));
            SetStatus(entries.Count + "개 Die 상태 변경 요청: " + state);
        }

        private void JogButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (_busy || e.Button != MouseButtons.Left)
                return;

            Button button = sender as Button;
            if (button == null || !(button.Tag is InputStageReviewJogAxis))
                return;

            int direction = button.Name.EndsWith("Minus", StringComparison.Ordinal) ? -1 : 1;
            var handler = JogRequested;
            if (handler != null)
                handler(this, new InputStageReviewJogEventArgs(
                    (InputStageReviewJogAxis)button.Tag,
                    direction,
                    cmbJogSpeed.Text));
            SetStatus(button.Text + " Jog 요청 중입니다. 버튼을 놓으면 정지 요청합니다.");
        }

        private void JogButton_MouseUp(object sender, MouseEventArgs e)
        {
            RaiseSimpleEvent(JogStopRequested);
            SetStatus("Jog 정지를 요청했습니다.");
        }

        private void BtnJogStop_Click(object sender, EventArgs e)
        {
            RaiseSimpleEvent(JogStopRequested);
            SetStatus("전체 Jog 정지를 요청했습니다.");
        }

        private void BtnMoveSelectedDie_Click(object sender, EventArgs e)
        {
            if (_selectedDie == null)
            {
                SetStatus("이동할 Die를 먼저 선택하세요.");
                return;
            }
            RaiseSimpleEvent(SelectedDieMoveRequested);
        }

        private void BtnRetryAlign_Click(object sender, EventArgs e) { RaiseSimpleEvent(AlignRetryRequested); }
        private void BtnRetryMapping_Click(object sender, EventArgs e) { RaiseSimpleEvent(MappingRetryRequested); }
        private void BtnMappingSetup_Click(object sender, EventArgs e) { RaiseSimpleEvent(MappingSetupRequested); }
        private void BtnVisionTest_Click(object sender, EventArgs e) { RaiseSimpleEvent(VisionTestRequested); }
        private void BtnThetaCorrection_Click(object sender, EventArgs e) { RaiseSimpleEvent(ThetaCorrectionRequested); }
        private void BtnDieDetection_Click(object sender, EventArgs e) { RaiseSimpleEvent(DieDetectionRequested); }
        private void BtnOffsetApply_Click(object sender, EventArgs e) { RaiseSimpleEvent(OffsetApplyRequested); }
        private void BtnStartRun_Click(object sender, EventArgs e) { RaiseSimpleEvent(StartRunRequested); }
        private void BtnAbortAuto_Click(object sender, EventArgs e) { RaiseSimpleEvent(AbortAutoRequested); }

        private void RaiseSimpleEvent(EventHandler handler)
        {
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void UpdateActionAvailability()
        {
            bool enabled = !_busy;
            bool actionEnabled = enabled && !_readOnlyPreview;
            grpDieState.Enabled = actionEnabled && _mode == InputStageRunReviewMode.MappingReview;
            grpStartDie.Enabled = enabled && _mappingComplete;
            grpJog.Enabled = actionEnabled;
            grpActions.Enabled = actionEnabled;
            grpPickupRoute.Enabled = enabled && _mappingComplete;
            btnPreviewPath.Enabled = enabled && _mappingComplete;
            btnApplyPickupOrder.Enabled = actionEnabled && _mappingComplete;
            btnRetryAlign.Enabled = actionEnabled;
            btnRetryMapping.Enabled = actionEnabled && _alignComplete;
            btnMappingSetup.Enabled = actionEnabled;
            btnStartRun.Enabled = actionEnabled &&
                                  _mode == InputStageRunReviewMode.MappingReview &&
                                  _alignComplete &&
                                  _mappingComplete &&
                                  _reviewValid &&
                                  _pickupOrderApplied &&
                                  (!chkUseSelectedStart.Checked || _startDie != null);
            btnAbortAuto.Enabled = actionEnabled;
            btnClose.Enabled = enabled;
            btnJogStop.Enabled = actionEnabled;
        }

        private void SetStatus(string message)
        {
            lblStatus.Text = string.IsNullOrWhiteSpace(message) ? "-" : message;
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void InputStageRunReviewDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_busy && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                SetStatus("동작 진행 중에는 화면을 닫을 수 없습니다. 먼저 STOP 또는 작업 완료를 확인하세요.");
                return;
            }

            if (!_readOnlyPreview && e.CloseReason == CloseReason.UserClosing && DialogResult != DialogResult.OK)
                RaiseSimpleEvent(AbortAutoRequested);
        }
    }
}
