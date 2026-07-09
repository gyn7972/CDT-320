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
        private const int RunAllStepDelayMs = 300;
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
        private bool _releasePreparedReservationOnClose;
        private bool _runAllInProgress;

        private sealed class PickUpStepItem
        {
            public int No { get; set; }
            public bool IsPrepareTarget { get; set; }
            public PickerPickUpZManualStep Step { get; set; }
            public string Text { get; set; }

            public override string ToString()
            {
                return No.ToString("00", CultureInfo.InvariantCulture) + ". " +
                       (Text ?? (IsPrepareTarget ? "Prepare Target" : Step.ToString()));
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
            InitializePickUpSteps();
            lblStatus.Text = "PickUp Step Test ready.";
            UpdatePreparedState();
        }

        public InputPickTargetSelectDialog(
            MachineController controller,
            PickerSequenceSide side,
            int defaultPickerNo,
            string preparedDieId,
            bool releasePreparedReservationOnClose)
            : this(controller, side, defaultPickerNo)
        {
            SetPreparedTarget(preparedDieId, defaultPickerNo, releasePreparedReservationOnClose);
        }

        public InputPickTargetSelectDialog(
            MachineController controller,
            PickerSequenceSide side,
            int defaultPickerNo,
            string selectedDieId,
            int dieMapX,
            int dieMapY,
            double targetX,
            double targetY)
            : this(controller, side, defaultPickerNo)
        {
            SetSelectedInputDieTarget(selectedDieId, dieMapX, dieMapY, targetX, targetY);
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
                    ? "Select a die, then run 01 Prepare Target from the PickUp Step list. Wheel=zoom, drag=pan."
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

        private async void btnPickZTest_Click(object sender, EventArgs e)
        {
            await RunPickZTestAsync().ConfigureAwait(true);
        }

        private async void btnRunStep_Click(object sender, EventArgs e)
        {
            await RunSelectedPickUpStepAsync(false).ConfigureAwait(true);
        }

        private async void btnNextStep_Click(object sender, EventArgs e)
        {
            await RunSelectedPickUpStepAsync(true).ConfigureAwait(true);
        }

        private async void btnRunAllSteps_Click(object sender, EventArgs e)
        {
            await RunAllPickUpStepsAsync().ConfigureAwait(true);
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

        private async Task<bool> RunPrepareAsync(bool moveNextWhenSuccess)
        {
            return await RunPrepareAsync(moveNextWhenSuccess, true).ConfigureAwait(true);
        }

        private async Task<bool> RunPrepareAsync(bool moveNextWhenSuccess, bool showConfirm)
        {
            if (_busy)
                return false;

            try
            {
                if (_controller == null)
                {
                    ShowMessage("MachineController is not available. PickUp test cannot run.", MessageBoxIcon.Warning);
                    return false;
                }

                InputStagePickTargetCandidate target = GetSelectedTarget();
                if (target == null || string.IsNullOrWhiteSpace(target.DieId))
                {
                    ShowMessage("Select a die to prepare on InputStage.", MessageBoxIcon.Warning);
                    return false;
                }

                int pickerNo = ResolvePickerNo();
                if (showConfirm)
                {
                    DialogResult answer = QMC.Common.MessageDialog.Show(
                        this,
                        "Prepare the selected die for PickUp Step test?\r\n\r\n" +
                        "Input Vision inspection and picker approach sequence will run.\r\n\r\n" +
                        "Die=" + target.DieId + "\r\n" +
                        "PickerNo=" + pickerNo,
                        SideName + " PickUp Test",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (answer != DialogResult.Yes)
                        return false;
                }

                SetBusy(true);
                SequenceFailureStore.Clear();
                lblStatus.Text = "Prepare Target running...";
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
                    lblStatus.Text = "Prepare Target failed: " + reason;
                    ShowMessage("Prepare Target failed: " + reason, MessageBoxIcon.Error);
                    return false;
                }

                _preparedDieId = target.DieId;
                _preparedPickerNo = pickerNo;
                _releasePreparedReservationOnClose = true;
                lblStatus.Text = "Prepare Target complete. Pick Z Test or Step Test is available.";
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " selected die prepare complete. die=" + _preparedDieId +
                    ", pickerNo=" + _preparedPickerNo + " - Ok");
                if (moveNextWhenSuccess)
                    MoveToNextPickUpStep();

                return true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Prepare Target exception: " + ex.Message;
                ShowMessage("Error during Prepare Target.\r\n" + ex.Message, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                SetBusy(false);
                UpdatePreparedState();
            }
        }

        private async Task RunPickZTestAsync()
        {
            if (_busy)
                return;

            try
            {
                if (_controller == null)
                {
                    ShowMessage("MachineController is not available. Pick Z Test cannot run.", MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_preparedDieId))
                {
                    ShowMessage("PickUp target is not prepared. Prepare the picker target before Pick Z Test.", MessageBoxIcon.Warning);
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
                _releasePreparedReservationOnClose = false;
                lblStatus.Text = "Pick Z Test complete.";
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

        private async Task RunSelectedPickUpStepAsync(bool moveNextWhenSuccess)
        {
            if (_busy)
                return;

            PickUpStepItem stepItem = ResolveSelectedPickUpStep();
            if (stepItem == null)
            {
                ShowMessage("Select a PickUp Step to run.", MessageBoxIcon.Warning);
                return;
            }

            if (stepItem.IsPrepareTarget)
            {
                await RunPrepareAsync(moveNextWhenSuccess).ConfigureAwait(true);
                return;
            }

                await RunPickZStepAsync(stepItem, moveNextWhenSuccess).ConfigureAwait(true);
        }

        private async Task<bool> RunPickZStepAsync(PickUpStepItem stepItem, bool moveNextWhenSuccess)
        {
            if (_busy)
                return false;

            try
            {
                if (_controller == null)
                {
                    ShowMessage("MachineController is not available. Pick Z Step test cannot run.", MessageBoxIcon.Warning);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(_preparedDieId))
                {
                    ShowMessage("PickUp target is not prepared. Prepare the picker target before Pick Z Step test.", MessageBoxIcon.Warning);
                    return false;
                }

                int pickerNo = ResolvePickerNo();
                if (pickerNo != _preparedPickerNo)
                {
                    ShowMessage(
                        "Prepared Picker No and current Picker No are different.\r\n" +
                        "Prepared PickerNo=" + _preparedPickerNo + ", current PickerNo=" + pickerNo,
                        MessageBoxIcon.Warning);
                    return false;
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
                    return false;
                }

                lblStatus.Text = "Pick Z Step complete: " + stepItem.Text;
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " Pick Z Step complete. die=" + _preparedDieId +
                    ", pickerNo=" + _preparedPickerNo +
                    ", step=" + stepItem.Step + " - Ok");

                if (stepItem.Step == PickerPickUpZManualStep.UpdateMaterialToPicker)
                {
                    _preparedDieId = "";
                    _releasePreparedReservationOnClose = false;
                    lblStatus.Text = "Pick Z Step complete: " + stepItem.Text;
                    return true;
                }

                if (moveNextWhenSuccess)
                    MoveToNextPickUpStep();

                return true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Pick Z Step exception: " + ex.Message;
                ShowMessage("Error during Pick Z Step.\r\n" + ex.Message, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                SetBusy(false);
                UpdatePreparedState();
            }
        }

        private async Task RunAllPickUpStepsAsync()
        {
            if (_busy || _runAllInProgress)
                return;

            try
            {
                if (_controller == null)
                {
                    ShowMessage("MachineController is not available. PickUp Step All cannot run.", MessageBoxIcon.Warning);
                    return;
                }

                int pickerNo = ResolvePickerNo();
                bool prepared = !string.IsNullOrWhiteSpace(_preparedDieId);
                if (prepared && pickerNo != _preparedPickerNo)
                {
                    ShowMessage(
                        "Prepared Picker No and current Picker No are different.\r\n" +
                        "Prepared PickerNo=" + _preparedPickerNo + ", current PickerNo=" + pickerNo,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (!prepared)
                {
                    InputStagePickTargetCandidate target = GetSelectedTarget();
                    if (target == null || string.IsNullOrWhiteSpace(target.DieId))
                    {
                        ShowMessage("Select a die to run PickUp Step All.", MessageBoxIcon.Warning);
                        return;
                    }
                }

                DialogResult answer = QMC.Common.MessageDialog.Show(
                    this,
                    "Run all PickUp steps with a short delay between steps?\r\n\r\n" +
                    "Delay=" + RunAllStepDelayMs + " ms\r\n" +
                    "Prepared=" + (prepared ? ("Picker #" + _preparedPickerNo + " / " + _preparedDieId) : "No. Step 01 will prepare selected die."),
                    SideName + " PickUp Test",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                    return;

                _runAllInProgress = true;
                SetBusy(false);
                SequenceFailureStore.Clear();
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " PickUp Step All start. prepared=" + prepared +
                    ", pickerNo=" + pickerNo + " - Start");

                if (!prepared)
                {
                    SelectPickUpStepRow(1);
                    bool prepareOk = await RunPrepareAsync(true, false).ConfigureAwait(true);
                    if (!prepareOk)
                        return;

                    await DelayBetweenRunAllStepsAsync().ConfigureAwait(true);
                }
                else
                {
                    SelectPickUpStepRow(2);
                }

                foreach (PickUpStepItem step in GetPickUpStepItems())
                {
                    if (step == null || step.IsPrepareTarget)
                        continue;

                    SelectPickUpStepRow(step.No);
                    lblStatus.Text = "PickUp Step All running: " + step.Text;
                    bool ok = await RunPickZStepAsync(step, true).ConfigureAwait(true);
                    if (!ok)
                        return;

                    if (step.Step != PickerPickUpZManualStep.UpdateMaterialToPicker)
                        await DelayBetweenRunAllStepsAsync().ConfigureAwait(true);
                }

                lblStatus.Text = "PickUp Step All complete.";
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " PickUp Step All complete. pickerNo=" + pickerNo + " - Ok");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "PickUp Step All exception: " + ex.Message;
                ShowMessage("Error during PickUp Step All.\r\n" + ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                _runAllInProgress = false;
                SetBusy(false);
                UpdatePreparedState();
            }
        }

        private async Task DelayBetweenRunAllStepsAsync()
        {
            lblStatus.Text = lblStatus.Text + " / delay " + RunAllStepDelayMs + " ms";
            await Task.Delay(RunAllStepDelayMs).ConfigureAwait(true);
        }

        private void InitializePickUpSteps()
        {
            gridPickUpSteps.Rows.Clear();
            AddPickUpStepRow(new PickUpStepItem { No = 1, IsPrepareTarget = true, Text = "Move Picker To Selected Die" });
            AddPickUpStepRow(new PickUpStepItem { No = 2, Step = PickerPickUpZManualStep.PrepareNeedlePinZ, Text = "Prepare Needle/EjectPin Z" });
            AddPickUpStepRow(new PickUpStepItem { No = 3, Step = PickerPickUpZManualStep.VacuumOnBeforePick, Text = "Vacuum ON / Settle" });
            AddPickUpStepRow(new PickUpStepItem { No = 4, Step = PickerPickUpZManualStep.MovePickerZPrePick, Text = "Move PickerZ PrePick" });
            AddPickUpStepRow(new PickUpStepItem { No = 5, Step = PickerPickUpZManualStep.MovePickerZSlowToContact, Text = "Slow PickerZ To Contact" });
            AddPickUpStepRow(new PickUpStepItem { No = 6, Step = PickerPickUpZManualStep.MoveEjectPinPickerZSyncLift, Text = "Sync Lift EjectPinZ/PickerZ" });
            AddPickUpStepRow(new PickUpStepItem { No = 7, Step = PickerPickUpZManualStep.SeparateNeedlePickerZ, Text = "PickerZ Avoid / NeedlePinZ Avoid" });
            AddPickUpStepRow(new PickUpStepItem { No = 8, Step = PickerPickUpZManualStep.VerifyDiePicked, Text = "Verify Die Picked" });
            AddPickUpStepRow(new PickUpStepItem { No = 9, Step = PickerPickUpZManualStep.MoveZToSafeAfterPick, Text = "Move Z To Safe" });
            AddPickUpStepRow(new PickUpStepItem { No = 10, Step = PickerPickUpZManualStep.UpdateMaterialToPicker, Text = "Update Material Picked" });

            if (gridPickUpSteps.Rows.Count > 0)
            {
                gridPickUpSteps.Rows[0].Selected = true;
                gridPickUpSteps.CurrentCell = gridPickUpSteps.Rows[0].Cells[0];
            }
        }

        private void AddPickUpStepRow(PickUpStepItem item)
        {
            int rowIndex = gridPickUpSteps.Rows.Add(
                item.No.ToString("00", CultureInfo.InvariantCulture),
                item.Text);
            gridPickUpSteps.Rows[rowIndex].Tag = item;
        }

        private PickUpStepItem ResolveSelectedPickUpStep()
        {
            if (gridPickUpSteps.CurrentRow != null)
                return gridPickUpSteps.CurrentRow.Tag as PickUpStepItem;

            if (gridPickUpSteps.Rows.Count <= 0)
                return null;

            gridPickUpSteps.Rows[0].Selected = true;
            gridPickUpSteps.CurrentCell = gridPickUpSteps.Rows[0].Cells[0];
            return gridPickUpSteps.Rows[0].Tag as PickUpStepItem;
        }

        private IEnumerable<PickUpStepItem> GetPickUpStepItems()
        {
            foreach (DataGridViewRow row in gridPickUpSteps.Rows)
            {
                PickUpStepItem item = row.Tag as PickUpStepItem;
                if (item != null)
                    yield return item;
            }
        }

        private void SelectPickUpStepRow(int stepNo)
        {
            foreach (DataGridViewRow row in gridPickUpSteps.Rows)
            {
                PickUpStepItem item = row.Tag as PickUpStepItem;
                if (item == null || item.No != stepNo)
                    continue;

                gridPickUpSteps.ClearSelection();
                row.Selected = true;
                gridPickUpSteps.CurrentCell = row.Cells[0];
                return;
            }
        }

        private void MoveToNextPickUpStep()
        {
            if (gridPickUpSteps.Rows.Count <= 0)
                return;

            int rowIndex = gridPickUpSteps.CurrentRow != null
                ? gridPickUpSteps.CurrentRow.Index
                : -1;
            if (rowIndex < 0)
            {
                gridPickUpSteps.Rows[0].Selected = true;
                gridPickUpSteps.CurrentCell = gridPickUpSteps.Rows[0].Cells[0];
                return;
            }

            if (rowIndex < gridPickUpSteps.Rows.Count - 1)
            {
                gridPickUpSteps.Rows[rowIndex + 1].Selected = true;
                gridPickUpSteps.CurrentCell = gridPickUpSteps.Rows[rowIndex + 1].Cells[0];
            }
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
                                 ", map=" + ResolveEntryMapX(entry) + "/" + ResolveEntryMapY(entry) +
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

        private static int ResolveEntryMapX(DieMapEntry entry)
        {
            return DieMapGenerator.ResolveMapIndexX(entry);
        }

        private static int ResolveEntryMapY(DieMapEntry entry)
        {
            return DieMapGenerator.ResolveMapIndexY(entry);
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
            bool locked = busy || _runAllInProgress;
            gridTargets.Enabled = !locked;
            cmbPickerNo.Enabled = !locked;
            btnRefresh.Enabled = !locked;
            btnPickZTest.Enabled = !locked && !string.IsNullOrWhiteSpace(_preparedDieId);
            btnRunStep.Enabled = !locked;
            btnNextStep.Enabled = !locked;
            btnRunAllSteps.Enabled = !locked;
            gridPickUpSteps.Enabled = !locked;
            btnClose.Enabled = !locked;
            UseWaitCursor = locked;
        }

        private void UpdatePreparedState()
        {
            if (_busy || _runAllInProgress)
                return;

            btnPickZTest.Enabled = !string.IsNullOrWhiteSpace(_preparedDieId);
            btnRunStep.Enabled = true;
            btnNextStep.Enabled = true;
            btnRunAllSteps.Enabled = true;
            lblPrepared.Text = string.IsNullOrWhiteSpace(_preparedDieId)
                ? "Prepared: -"
                : "Prepared: Picker #" + _preparedPickerNo + " / " + _preparedDieId;
        }

        public void SetPreparedTarget(
            string preparedDieId,
            int pickerNo,
            bool releasePreparedReservationOnClose)
        {
            try
            {
                string normalizedDieId = preparedDieId ?? "";
                int normalizedPickerNo = Math.Max(1, Math.Min(4, pickerNo));
                bool sameTarget =
                    string.Equals(_preparedDieId, normalizedDieId, StringComparison.OrdinalIgnoreCase) &&
                    _preparedPickerNo == normalizedPickerNo;

                if (!sameTarget)
                    ReleasePreparedReservation();

                _preparedDieId = normalizedDieId;
                _preparedPickerNo = normalizedPickerNo;
                _releasePreparedReservationOnClose =
                    releasePreparedReservationOnClose && !string.IsNullOrWhiteSpace(_preparedDieId);
                SetDefaultPickerNo(_preparedPickerNo);
                lblStatus.Text = string.IsNullOrWhiteSpace(_preparedDieId)
                    ? "PickUp Step Test ready."
                    : "PickUp Step Test ready. Prepared die=" + _preparedDieId;
            }
            finally
            {
                UpdatePreparedState();
            }
        }

        public void SetSelectedInputDieTarget(
            string selectedDieId,
            int dieMapX,
            int dieMapY,
            double targetX,
            double targetY)
        {
            try
            {
                ReleasePreparedReservation();

                _targets.Clear();
                _targetByDieId.Clear();
                _preparedDieId = "";
                _releasePreparedReservationOnClose = false;

                var target = new InputStagePickTargetCandidate
                {
                    DieId = selectedDieId ?? "",
                    DieMapX = dieMapX,
                    DieMapY = dieMapY,
                    TargetX = targetX,
                    TargetY = targetY
                };

                _selectedTarget = target;
                if (!string.IsNullOrWhiteSpace(target.DieId))
                {
                    _targets.Add(target);
                    _targetByDieId[target.DieId] = target;
                }

                lblStatus.Text = string.IsNullOrWhiteSpace(target.DieId)
                    ? "PickUp Step Test ready."
                    : "Selected Die: " + target.DieId +
                      ", map=" + target.DieMapX + "/" + target.DieMapY +
                      ", target=" + target.TargetX.ToString("0.###", CultureInfo.InvariantCulture) +
                      "/" + target.TargetY.ToString("0.###", CultureInfo.InvariantCulture) +
                      " - Run Step 01 first.";
            }
            finally
            {
                UpdatePreparedState();
            }
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
                if (!_releasePreparedReservationOnClose || string.IsNullOrWhiteSpace(_preparedDieId))
                    return;

                MaterialStateService.ReleaseInputStagePickReservation(
                    _preparedDieId,
                    ResolvePickerLocation(),
                    _preparedPickerNo);
                Log.Write("Main", UserSession.Name, "PickUpTestDialog",
                    SideName + " PickUp Test close: prepared die reservation released. die=" + _preparedDieId +
                    ", pickerNo=" + _preparedPickerNo + " - Ok");
                _preparedDieId = "";
                _releasePreparedReservationOnClose = false;
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
