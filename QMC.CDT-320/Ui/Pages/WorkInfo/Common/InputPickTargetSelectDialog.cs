using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Security;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    internal partial class InputPickTargetSelectDialog : Form
    {
        private readonly MachineController _controller;
        private readonly PickerSequenceSide _side;
        private readonly List<InputStagePickTargetCandidate> _targets = new List<InputStagePickTargetCandidate>();
        private readonly Dictionary<string, InputStagePickTargetCandidate> _targetByDieId =
            new Dictionary<string, InputStagePickTargetCandidate>(StringComparer.OrdinalIgnoreCase);
        private bool _busy;
        private bool _syncingSelection;
        private InputStagePickTargetCandidate _selectedTarget;
        private string _preparedDieId = "";
        private int _preparedPickerNo;

        private sealed class PickZStepItem
        {
            public PickerPickUpZManualStep Step { get; set; }
            public string Text { get; set; }

            public override string ToString()
            {
                return Text ?? Step.ToString();
            }
        }

        public InputPickTargetSelectDialog(
            MachineController controller,
            PickerSequenceSide side,
            int defaultPickerNo)
        {
            _controller = controller;
            _side = side;
            _preparedPickerNo = Math.Max(1, Math.Min(4, defaultPickerNo));

            InitializeComponent();
            Text = SideName + " PickUp Test";
            lblHeader.Text = SideName.ToUpperInvariant() + " INPUT DIE PICKUP TEST";
            mapView.CellClicked += OnMapCellClicked;
            mapView.CellColorResolver = ResolveMapCellColor;
            mapView.CellTextResolver = ResolveMapCellText;
            mapView.CellStatusResolver = ResolveMapCellStatus;
            gridTargets.SelectionChanged += gridTargets_SelectionChanged;
            SetDefaultPickerNo(_preparedPickerNo);
            InitializePickZSteps();
            BindTargets();
            UpdatePreparedState();
        }

        private string SideName
        {
            get { return _side == PickerSequenceSide.Front ? "Front Picker" : "Rear Picker"; }
        }

        private void SetDefaultPickerNo(int pickerNo)
        {
            try
            {
                int normalized = Math.Max(1, Math.Min(4, pickerNo));
                cmbPickerNo.SelectedItem = normalized.ToString(CultureInfo.InvariantCulture);
                if (cmbPickerNo.SelectedIndex < 0)
                    cmbPickerNo.SelectedIndex = 0;
            }
            catch
            {
                cmbPickerNo.SelectedIndex = 0;
            }
            finally
            {
            }
        }

        private void BindTargets()
        {
            try
            {
                string selectedDieId = GetSelectedDieId();
                _targets.Clear();
                _targetByDieId.Clear();
                IList<InputStagePickTargetCandidate> targets = MaterialStateService.GetReadyInputStagePickTargetCandidates();
                if (targets != null)
                    _targets.AddRange(targets);
                for (int i = 0; i < _targets.Count; i++)
                {
                    InputStagePickTargetCandidate target = _targets[i];
                    if (target != null && !string.IsNullOrWhiteSpace(target.DieId))
                        _targetByDieId[target.DieId] = target;
                }

                BindMapView();
                gridTargets.Rows.Clear();

                for (int i = 0; i < _targets.Count; i++)
                {
                    InputStagePickTargetCandidate target = _targets[i];
                    if (target == null)
                        continue;

                    int rowIndex = gridTargets.Rows.Add(
                        target.OrderIndex + 1,
                        target.DieId,
                        target.DieMapX + " / " + target.DieMapY,
                        target.TargetX.ToString("0.###", CultureInfo.InvariantCulture) +
                        " / " +
                        target.TargetY.ToString("0.###", CultureInfo.InvariantCulture));
                    gridTargets.Rows[rowIndex].Tag = target;

                    if (!string.IsNullOrWhiteSpace(selectedDieId) &&
                        string.Equals(selectedDieId, target.DieId, StringComparison.OrdinalIgnoreCase))
                    {
                        _syncingSelection = true;
                        gridTargets.Rows[rowIndex].Selected = true;
                        gridTargets.CurrentCell = gridTargets.Rows[rowIndex].Cells[0];
                        _syncingSelection = false;
                        _selectedTarget = target;
                    }
                }

                if (gridTargets.Rows.Count > 0 && gridTargets.CurrentRow == null)
                {
                    _syncingSelection = true;
                    gridTargets.Rows[0].Selected = true;
                    gridTargets.CurrentCell = gridTargets.Rows[0].Cells[0];
                    _syncingSelection = false;
                    _selectedTarget = gridTargets.Rows[0].Tag as InputStagePickTargetCandidate;
                }

                SelectMapEntryByDieId(_selectedTarget != null ? _selectedTarget.DieId : "");
                lblStatus.Text = _targets.Count > 0
                    ? "Select a die on Wafer View, then run INSPECT / MOVE. Wheel=zoom, drag=pan."
                    : "No InputStage die is available for PickUp test.";
            }
            catch (Exception ex)
            {
                ShowMessage("Failed to display PickUp target die list.\r\n" + ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                UpdatePreparedState();
            }
        }

        private async void btnPrepare_Click(object sender, EventArgs e)
        {
            await RunPrepareAsync().ConfigureAwait(true);
        }

        private async void btnPickZTest_Click(object sender, EventArgs e)
        {
            await RunPickZTestAsync().ConfigureAwait(true);
        }

        private async void btnRunStep_Click(object sender, EventArgs e)
        {
            await RunPickZStepAsync(false).ConfigureAwait(true);
        }

        private async void btnNextStep_Click(object sender, EventArgs e)
        {
            await RunPickZStepAsync(true).ConfigureAwait(true);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            BindTargets();
        }

        private void gridTargets_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (_syncingSelection)
                    return;

                _selectedTarget = gridTargets.CurrentRow != null
                    ? gridTargets.CurrentRow.Tag as InputStagePickTargetCandidate
                    : null;
                SelectMapEntryByDieId(_selectedTarget != null ? _selectedTarget.DieId : "");
                UpdateSelectedStatus();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async Task RunPrepareAsync()
        {
            try
            {
                if (_busy)
                    return;

                if (_controller == null)
                {
                    ShowMessage("MachineController is not available. PickUp test cannot run.", MessageBoxIcon.Warning);
                    return;
                }

                InputStagePickTargetCandidate target = GetSelectedTarget();
                if (target == null || string.IsNullOrWhiteSpace(target.DieId))
                {
                    ShowMessage("Select a die to inspect on InputStage.", MessageBoxIcon.Warning);
                    return;
                }

                int pickerNo = ResolvePickerNo();
                DialogResult answer = QMC.Common.MessageDialog.Show(
                    this,
                    "Inspect the selected die with Input Vision and move Picker to corrected position?\r\n\r\n" +
                    "Die=" + target.DieId + "\r\n" +
                    "PickerNo=" + pickerNo,
                    SideName + " PickUp Test",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                    return;

                SetBusy(true);
                SequenceFailureStore.Clear();
                lblStatus.Text = "Input Vision inspection and Picker move running...";
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " selected die prepare start. die=" + target.DieId +
                    ", pickerNo=" + pickerNo + " - Start");

                int result = await _controller
                    .RunManualPickerSelectedDiePrepareAsync(_side, pickerNo, target.DieId)
                    .ConfigureAwait(true);
                if (result != 0)
                {
                    string reason = string.IsNullOrWhiteSpace(_controller.LastActionFailureMessage)
                        ? "No detail reason"
                        : _controller.LastActionFailureMessage;
                    lblStatus.Text = "Inspect / Move failed: " + reason;
                    ShowMessage("Inspect / Move failed: " + reason, MessageBoxIcon.Error);
                    return;
                }

                _preparedDieId = target.DieId;
                _preparedPickerNo = pickerNo;
                lblStatus.Text = "Inspect / Move complete. Pick Z Test or Step Test is available.";
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " selected die prepare complete. die=" + _preparedDieId +
                    ", pickerNo=" + _preparedPickerNo + " - Ok");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Inspect / Move exception: " + ex.Message;
                ShowMessage("Error during Inspect / Move.\r\n" + ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
                UpdatePreparedState();
            }
        }

        private async Task RunPickZTestAsync()
        {
            try
            {
                if (_busy)
                    return;

                if (_controller == null)
                {
                    ShowMessage("MachineController is not available. Pick Z Test cannot run.", MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_preparedDieId))
                {
                    ShowMessage("Run INSPECT / MOVE before Pick Z Test.", MessageBoxIcon.Warning);
                    return;
                }

                int pickerNo = ResolvePickerNo();
                if (pickerNo != _preparedPickerNo)
                {
                    ShowMessage(
                        "Prepared Picker No and current Picker No are different.\r\n" +
                        "Prepared PickerNo=" + _preparedPickerNo + ", current PickerNo=" + pickerNo,
                        MessageBoxIcon.Warning);
                    return;
                }

                DialogResult answer = QMC.Common.MessageDialog.Show(
                    this,
                    "Run Pick Z Test with the prepared die?\r\n\r\n" +
                    "Die=" + _preparedDieId + "\r\n" +
                    "PickerNo=" + _preparedPickerNo + "\r\n\r\n" +
                    "On success, Material state changes to Picker-held state.",
                    SideName + " PickUp Test",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                    return;

                SetBusy(true);
                SequenceFailureStore.Clear();
                lblStatus.Text = "Pick Z Test running...";
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " Pick Z Test start. die=" + _preparedDieId +
                    ", pickerNo=" + _preparedPickerNo + " - Start");

                int result = await _controller
                    .RunManualPickerPreparedDiePickZAsync(_side, _preparedPickerNo, _preparedDieId)
                    .ConfigureAwait(true);
                if (result != 0)
                {
                    string reason = string.IsNullOrWhiteSpace(_controller.LastActionFailureMessage)
                        ? "No detail reason"
                        : _controller.LastActionFailureMessage;
                    lblStatus.Text = "Pick Z Test failed: " + reason;
                    ShowMessage("Pick Z Test failed: " + reason, MessageBoxIcon.Error);
                    return;
                }

                lblStatus.Text = "Pick Z Test complete.";
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " Pick Z Test complete. die=" + _preparedDieId +
                    ", pickerNo=" + _preparedPickerNo + " - Ok");
                _preparedDieId = "";
                BindTargets();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Pick Z Test exception: " + ex.Message;
                ShowMessage("Error during Pick Z Test.\r\n" + ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
                UpdatePreparedState();
            }
        }

        private async Task RunPickZStepAsync(bool moveNextWhenSuccess)
        {
            try
            {
                if (_busy)
                    return;

                if (_controller == null)
                {
                    ShowMessage("MachineController is not available. Pick Z Step test cannot run.", MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_preparedDieId))
                {
                    ShowMessage("Run INSPECT / MOVE before Pick Z Step test.", MessageBoxIcon.Warning);
                    return;
                }

                int pickerNo = ResolvePickerNo();
                if (pickerNo != _preparedPickerNo)
                {
                    ShowMessage(
                        "Prepared Picker No and current Picker No are different.\r\n" +
                        "Prepared PickerNo=" + _preparedPickerNo + ", current PickerNo=" + pickerNo,
                        MessageBoxIcon.Warning);
                    return;
                }

                PickZStepItem stepItem = ResolveSelectedPickZStep();
                if (stepItem == null)
                {
                    ShowMessage("Select a Pick Z Step to run.", MessageBoxIcon.Warning);
                    return;
                }

                SetBusy(true);
                SequenceFailureStore.Clear();
                lblStatus.Text = "Pick Z Step running: " + stepItem.Text;
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " Pick Z Step start. die=" + _preparedDieId +
                    ", pickerNo=" + _preparedPickerNo +
                    ", step=" + stepItem.Step + " - Start");

                int result = await _controller
                    .RunManualPickerPreparedDiePickZStepAsync(_side, _preparedPickerNo, _preparedDieId, stepItem.Step)
                    .ConfigureAwait(true);
                if (result != 0)
                {
                    string reason = string.IsNullOrWhiteSpace(_controller.LastActionFailureMessage)
                        ? "No detail reason"
                        : _controller.LastActionFailureMessage;
                    lblStatus.Text = "Pick Z Step failed: " + stepItem.Text + " / " + reason;
                    ShowMessage("Pick Z Step failed: " + stepItem.Text + "\r\n" + reason, MessageBoxIcon.Error);
                    return;
                }

                lblStatus.Text = "Pick Z Step complete: " + stepItem.Text;
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " Pick Z Step complete. die=" + _preparedDieId +
                    ", pickerNo=" + _preparedPickerNo +
                    ", step=" + stepItem.Step + " - Ok");

                if (stepItem.Step == PickerPickUpZManualStep.UpdateMaterialToPicker)
                {
                    _preparedDieId = "";
                    BindTargets();
                    return;
                }

                if (moveNextWhenSuccess)
                    MoveToNextPickZStep();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Pick Z Step exception: " + ex.Message;
                ShowMessage("Error during Pick Z Step.\r\n" + ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
                UpdatePreparedState();
            }
        }

        private void InitializePickZSteps()
        {
            cmbPickZStep.Items.Clear();
            cmbPickZStep.Items.Add(new PickZStepItem { Step = PickerPickUpZManualStep.PrepareNeedlePinZ, Text = "01. Prepare Needle/EjectPin Z" });
            cmbPickZStep.Items.Add(new PickZStepItem { Step = PickerPickUpZManualStep.VacuumOnBeforePick, Text = "02. Vacuum ON / Settle" });
            cmbPickZStep.Items.Add(new PickZStepItem { Step = PickerPickUpZManualStep.MovePickerZPrePick, Text = "03. Move PickerZ PrePick" });
            cmbPickZStep.Items.Add(new PickZStepItem { Step = PickerPickUpZManualStep.MovePickerZSlowToContact, Text = "04. Slow PickerZ To Contact" });
            cmbPickZStep.Items.Add(new PickZStepItem { Step = PickerPickUpZManualStep.MoveNeedlePickerZSyncLift, Text = "05. Sync Lift Needle/PickerZ" });
            cmbPickZStep.Items.Add(new PickZStepItem { Step = PickerPickUpZManualStep.SeparateNeedlePickerZ, Text = "06. Separate Needle/PickerZ" });
            cmbPickZStep.Items.Add(new PickZStepItem { Step = PickerPickUpZManualStep.VerifyDiePicked, Text = "07. Verify Die Picked" });
            cmbPickZStep.Items.Add(new PickZStepItem { Step = PickerPickUpZManualStep.MoveZToSafeAfterPick, Text = "08. Move Z To Safe" });
            cmbPickZStep.Items.Add(new PickZStepItem { Step = PickerPickUpZManualStep.UpdateMaterialToPicker, Text = "09. Update Material Picked" });

            if (cmbPickZStep.Items.Count > 0)
                cmbPickZStep.SelectedIndex = 0;
        }

        private PickZStepItem ResolveSelectedPickZStep()
        {
            return cmbPickZStep.SelectedItem as PickZStepItem;
        }

        private void MoveToNextPickZStep()
        {
            if (cmbPickZStep.SelectedIndex < 0)
            {
                cmbPickZStep.SelectedIndex = 0;
                return;
            }

            if (cmbPickZStep.SelectedIndex < cmbPickZStep.Items.Count - 1)
                cmbPickZStep.SelectedIndex++;
        }

        private InputStagePickTargetCandidate GetSelectedTarget()
        {
            try
            {
                if (_selectedTarget != null)
                    return _selectedTarget;

                return gridTargets.CurrentRow != null
                    ? gridTargets.CurrentRow.Tag as InputStagePickTargetCandidate
                    : null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private string GetSelectedDieId()
        {
            InputStagePickTargetCandidate target = GetSelectedTarget();
            return target != null ? target.DieId : "";
        }

        private void BindMapView()
        {
            try
            {
                DieMap map = MaterialStateService.BuildInputDieMapFromStageWafer();
                if (map != null)
                {
                    int done = 0;
                    int target = 0;
                    foreach (DieMapEntry entry in map.Entries)
                    {
                        if (entry == null || !entry.IsTarget)
                            continue;

                        target++;
                        string state = MaterialStateService.ResolveInputDieDisplayState(entry);
                        if (IsDoneState(state))
                            done++;
                    }

                    mapView.Caption = "InputStage Wafer  target=" + target +
                                      " done=" + done +
                                      " ready=" + _targets.Count;
                }
                else
                {
                    mapView.Caption = "InputStage Wafer";
                }

                mapView.Map = map;
                mapView.ShowWaferOutline = true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Wafer View display failed: " + ex.Message;
            }
            finally
            {
            }
        }

        private void OnMapCellClicked(DieMapEntry entry)
        {
            try
            {
                if (entry == null)
                    return;

                InputStagePickTargetCandidate target;
                if (!string.IsNullOrWhiteSpace(entry.DieUid) &&
                    _targetByDieId.TryGetValue(entry.DieUid, out target))
                {
                    SelectTarget(target);
                    return;
                }

                _selectedTarget = null;
                SelectGridRowByDieId("");
                mapView.SelectedEntry = entry;
                lblStatus.Text = "Selected die is not available for PickUp test. die=" +
                                 (entry.DieUid ?? "-") +
                                 ", map=" + entry.DieMapX + "/" + entry.DieMapY +
                                 ", state=" + ResolveMapCellStatus(entry);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Wafer View selection failed: " + ex.Message;
            }
            finally
            {
                UpdatePreparedState();
            }
        }

        private void SelectTarget(InputStagePickTargetCandidate target)
        {
            try
            {
                _selectedTarget = target;
                SelectGridRowByDieId(target != null ? target.DieId : "");
                SelectMapEntryByDieId(target != null ? target.DieId : "");
                UpdateSelectedStatus();
            }
            finally
            {
            }
        }

        private void SelectGridRowByDieId(string dieId)
        {
            try
            {
                _syncingSelection = true;
                gridTargets.ClearSelection();
                if (string.IsNullOrWhiteSpace(dieId))
                    return;

                foreach (DataGridViewRow row in gridTargets.Rows)
                {
                    InputStagePickTargetCandidate target = row.Tag as InputStagePickTargetCandidate;
                    if (target == null || !string.Equals(target.DieId, dieId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    row.Selected = true;
                    gridTargets.CurrentCell = row.Cells[0];
                    break;
                }
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        private void SelectMapEntryByDieId(string dieId)
        {
            try
            {
                DieMap map = mapView.Map;
                if (map == null || map.Entries == null || string.IsNullOrWhiteSpace(dieId))
                {
                    mapView.SelectedEntry = null;
                    return;
                }

                mapView.SelectedEntry = map.Entries.FirstOrDefault(e =>
                    e != null &&
                    string.Equals(e.DieUid, dieId, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                mapView.SelectedEntry = null;
            }
            finally
            {
            }
        }

        private void UpdateSelectedStatus()
        {
            if (_selectedTarget == null)
                return;

            lblStatus.Text = "Selected Die: " + _selectedTarget.DieId +
                             ", map=" + _selectedTarget.DieMapX + "/" + _selectedTarget.DieMapY +
                             ", target=" +
                             _selectedTarget.TargetX.ToString("0.###", CultureInfo.InvariantCulture) +
                             "/" +
                             _selectedTarget.TargetY.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private Color ResolveMapCellColor(DieMapEntry entry)
        {
            if (entry == null)
                return Color.FromArgb(45, 45, 45);

            string state = ResolveMapCellStatus(entry);
            if (!entry.IsTarget || string.Equals(state, "SKIP", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(70, 70, 70);
            if (string.Equals(state, "REJECT", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(180, 70, 70);
            if (IsDoneState(state))
                return Color.FromArgb(60, 150, 90);
            if (state.StartsWith("PICK", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(230, 150, 50);
            if (state.StartsWith("RESERVE", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(230, 210, 80);
            if (!string.IsNullOrWhiteSpace(entry.DieUid) && _targetByDieId.ContainsKey(entry.DieUid))
                return Color.FromArgb(190, 215, 235);

            return Color.FromArgb(105, 115, 125);
        }

        private string ResolveMapCellText(DieMapEntry entry)
        {
            if (entry == null)
                return "";

            string state = ResolveMapCellStatus(entry);
            if (IsDoneState(state))
                return "D";
            if (state.StartsWith("PICK", StringComparison.OrdinalIgnoreCase))
                return "P";
            if (state.StartsWith("RESERVE", StringComparison.OrdinalIgnoreCase))
                return "R";
            return "";
        }

        private string ResolveMapCellStatus(DieMapEntry entry)
        {
            return MaterialStateService.ResolveInputDieDisplayState(entry);
        }

        private static bool IsDoneState(string state)
        {
            return string.Equals(state, "FINISH", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(state, "GOOD STAGE", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(state, "NG STAGE", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(state, "OUT FEEDER", StringComparison.OrdinalIgnoreCase);
        }

        private int ResolvePickerNo()
        {
            try
            {
                int pickerNo;
                if (!int.TryParse(cmbPickerNo.Text, out pickerNo))
                    pickerNo = 1;

                return Math.Max(1, Math.Min(4, pickerNo));
            }
            catch
            {
                return 1;
            }
            finally
            {
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            gridTargets.Enabled = !busy;
            cmbPickerNo.Enabled = !busy;
            btnRefresh.Enabled = !busy;
            btnPrepare.Enabled = !busy;
            btnPickZTest.Enabled = !busy && !string.IsNullOrWhiteSpace(_preparedDieId);
            btnRunStep.Enabled = !busy && !string.IsNullOrWhiteSpace(_preparedDieId);
            btnNextStep.Enabled = !busy && !string.IsNullOrWhiteSpace(_preparedDieId);
            cmbPickZStep.Enabled = !busy;
            btnClose.Enabled = !busy;
            UseWaitCursor = busy;
        }

        private void UpdatePreparedState()
        {
            if (_busy)
                return;

            btnPickZTest.Enabled = !string.IsNullOrWhiteSpace(_preparedDieId);
            btnRunStep.Enabled = !string.IsNullOrWhiteSpace(_preparedDieId);
            btnNextStep.Enabled = !string.IsNullOrWhiteSpace(_preparedDieId);
            lblPrepared.Text = string.IsNullOrWhiteSpace(_preparedDieId)
                ? "Prepared: -"
                : "Prepared: Picker #" + _preparedPickerNo + " / " + _preparedDieId;
        }

        private void ShowMessage(string message, MessageBoxIcon icon)
        {
            QMC.Common.MessageDialog.Show(
                this,
                message,
                SideName + " PickUp Test",
                MessageBoxButtons.OK,
                icon);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (_busy)
                {
                    e.Cancel = true;
                    ShowMessage("The dialog cannot be closed while PickUp test is running.", MessageBoxIcon.Warning);
                    return;
                }

                ReleasePreparedReservation();
            }
            finally
            {
                base.OnFormClosing(e);
            }
        }

        private void ReleasePreparedReservation()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_preparedDieId))
                    return;

                MaterialStateService.ReleaseInputStagePickReservation(
                    _preparedDieId,
                    ResolvePickerLocation(),
                    _preparedPickerNo);
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " PickUp Test close: prepared die reservation released. die=" + _preparedDieId +
                    ", pickerNo=" + _preparedPickerNo + " - Ok");
                _preparedDieId = "";
            }
            catch (Exception ex)
            {
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " PickUp Test close reservation release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private MaterialLocationKind ResolvePickerLocation()
        {
            return _side == PickerSequenceSide.Front
                ? MaterialLocationKind.PickerFront
                : MaterialLocationKind.PickerRear;
        }
    }
}
