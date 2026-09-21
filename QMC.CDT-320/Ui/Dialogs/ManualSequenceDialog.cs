using QMC.CDT_320.Ui.Localization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class ManualSequenceDialog : Form
    {
        private readonly MachineController _controller;
        private readonly Dictionary<BinSide, LoadTargetItem> _outputLoadSelections = new Dictionary<BinSide, LoadTargetItem>();
        private bool _busy;
        private BinSide? _listedOutputSide;
        private int _manualRunId;

        /// <summary>LOAD 대상 콤보 항목. Slot이 음수이면 자동 순번을 의미한다.</summary>
        private sealed class LoadTargetItem
        {
            public LoadTargetItem(string text, CassetteMaterialRole role, int slotIndex)
            {
                Text = text;
                Role = role;
                SlotIndex = slotIndex;
            }

            public string Text { get; private set; }
            public CassetteMaterialRole Role { get; private set; }
            public int SlotIndex { get; private set; }

            public override string ToString()
            {
                return Text;
            }
        }

        public ManualSequenceDialog(MachineController controller)
        {
            InitializeComponent();
            InitializeLanguageBindings();
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            cmbPickerNo.SelectedIndex = 0;
            InitializeSpeedPercent();
            WireEvents();
            RefreshLoadTargets();
        }

        // LOAD 대상 목록을 현재 Material 상태로 다시 구성한다.
        // UNLOAD는 원본 슬롯으로만 복귀하므로 선택 대상이 없다(항상 자재의 SourceSlot 사용).
        private void RefreshLoadTargets(bool showFailureStatus = true)
        {
            try
            {
                LoadComboItems(cmbInputLoadTarget, BuildInputLoadTargets());
                RefreshOutputLoadTargets();
            }
            catch (Exception ex)
            {
                if (showFailureStatus)
                    Lang.BindFormat(statusLabel, "extraDialog.manualSequence.targetsFailed", ex.Message);
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualSequenceLoadTarget",
                    "로딩 대상 목록 구성 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void RefreshOutputLoadTargets()
        {
            if (_listedOutputSide.HasValue && cmbOutputLoadTarget.SelectedItem is LoadTargetItem selected)
                _outputLoadSelections[_listedOutputSide.Value] = selected;

            if (rdoOutputAll.Checked)
            {
                _listedOutputSide = null;
                LoadComboItems(cmbOutputLoadTarget, new List<LoadTargetItem>
                {
                    new LoadTargetItem("자동 — NG/GOOD 각각 다음 순번", CassetteMaterialRole.Good1, -1)
                });
            }
            else
            {
                BinSide side = SelectedOutputSide();
                LoadComboItems(cmbOutputLoadTarget, BuildOutputLoadTargets(side));
                LoadTargetItem previous;
                if (_outputLoadSelections.TryGetValue(side, out previous))
                {
                    foreach (LoadTargetItem item in cmbOutputLoadTarget.Items)
                    {
                        if (item.Role == previous.Role && item.SlotIndex == previous.SlotIndex)
                        {
                            cmbOutputLoadTarget.SelectedItem = item;
                            break;
                        }
                    }
                }
                _listedOutputSide = side;
            }

            cmbOutputLoadTarget.Enabled = !_busy && !rdoOutputAll.Checked;
        }

        private static void LoadComboItems(ComboBox combo, List<LoadTargetItem> items)
        {
            if (combo == null)
                return;

            combo.BeginUpdate();
            try
            {
                combo.Items.Clear();
                foreach (LoadTargetItem item in items)
                    combo.Items.Add(item);
                if (combo.Items.Count > 0)
                    combo.SelectedIndex = 0;
            }
            finally
            {
                combo.EndUpdate();
            }
        }

        // Input 카세트에서 로딩 가능한(Ready/WorkReady) wafer 목록을 레벨 순서대로 만든다.
        private static List<LoadTargetItem> BuildInputLoadTargets()
        {
            var items = new List<LoadTargetItem>();
            items.Add(new LoadTargetItem("자동 (다음 순번)", CassetteMaterialRole.Input1, -1));

            AppendCassetteReadyTargets(items, CassetteMaterialRole.Input1, "INPUT1", true);
            AppendCassetteReadyTargets(items, CassetteMaterialRole.Input2, "INPUT2", true);
            return items;
        }

        // 선택한 side의 Output 카세트에서 공급 가능한(Ready) bin 목록을 만든다.
        private static List<LoadTargetItem> BuildOutputLoadTargets(BinSide side)
        {
            var items = new List<LoadTargetItem>();
            items.Add(new LoadTargetItem("자동 (다음 순번)", CassetteMaterialRole.Good1, -1));

            if (side == BinSide.Ng)
            {
                AppendCassetteReadyTargets(items, CassetteMaterialRole.Ng1, "NG", false);
                return items;
            }

            AppendCassetteReadyTargets(items, CassetteMaterialRole.Good1, "GOOD1", false);
            AppendCassetteReadyTargets(items, CassetteMaterialRole.Good2, "GOOD2", false);
            return items;
        }

        private static void AppendCassetteReadyTargets(
            List<LoadTargetItem> items,
            CassetteMaterialRole role,
            string label,
            bool allowWorkReady)
        {
            MaterialSnapshot state = MaterialStateService.State;
            if (state == null || state.Cassettes == null)
                return;

            CassetteMaterial cassette = null;
            foreach (CassetteMaterial candidate in state.Cassettes)
            {
                if (candidate != null && candidate.Role == role)
                {
                    cassette = candidate;
                    break;
                }
            }

            if (cassette == null || !cassette.IsEnabled || !cassette.IsPresent || !cassette.IsMapped)
                return;

            cassette.EnsureSlots();
            for (int i = 0; i < cassette.Slots.Count; i++)
            {
                CassetteSlotMaterial slot = cassette.Slots[i];
                if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                    continue;

                WaferMaterial wafer = MaterialStateService.GetWaferInCassette(role, i);
                if (wafer == null)
                    continue;

                WaferMaterialState waferState = WaferMaterialStateText.Normalize(wafer.State);
                bool loadable = waferState == WaferMaterialState.Ready ||
                                (allowWorkReady && waferState == WaferMaterialState.WorkReady);
                if (!loadable)
                    continue;

                items.Add(new LoadTargetItem(
                    label + " " + (i + 1).ToString("00") + " - " + (wafer.WaferId ?? ""),
                    role,
                    i));
            }
        }

        private static LoadTargetItem ResolveSelectedTarget(ComboBox combo, CassetteMaterialRole fallbackRole)
        {
            LoadTargetItem item = combo != null ? combo.SelectedItem as LoadTargetItem : null;
            return item ?? new LoadTargetItem("자동 (다음 순번)", fallbackRole, -1);
        }

        // Manual/Ready 시퀀스 속도 %(디폴트 속도 대비)를 현재 설정값으로 표시한다.
        // 숫자 편집 중에는 운전값을 바꾸지 않고, SAVE 버튼을 눌렀을 때 두 값을 함께 저장·적용한다.
        // MotionSpeedScale.EffectiveScaleFactor가 이동 속도와 가감속을 항상 같은 배율로 스케일한다.
        // Manual 값은 이 화면뿐 아니라 작업 정보 Action 버튼 등 ManualSequence 스코프 전체에 적용된다.
        private void InitializeSpeedPercent()
        {
            numSpeedPercent.Value = ClampToControlRange(
                numSpeedPercent, QMC.Common.Motion.MotionSpeedScale.ManualSequencePercent);
            numReadySpeedPercent.Value = ClampToControlRange(
                numReadySpeedPercent, QMC.Common.Motion.MotionSpeedScale.ReadySequencePercent);

            numSpeedPercent.ValueChanged += SequenceSpeedPercent_ValueChanged;
            numReadySpeedPercent.ValueChanged += SequenceSpeedPercent_ValueChanged;
        }

        private static decimal ClampToControlRange(NumericUpDown control, double value)
        {
            decimal current = (decimal)value;
            if (current < control.Minimum) return control.Minimum;
            if (current > control.Maximum) return control.Maximum;
            return current;
        }

        // 편집값이 아직 저장되지 않았음을 SAVE 버튼 색상과 상태 문구로 알린다.
        private void SequenceSpeedPercent_ValueChanged(object sender, EventArgs e)
        {
            btnSaveSpeedPercent.BackColor = System.Drawing.Color.FromArgb(224, 115, 0);
            Lang.BindKey(statusLabel, "extraDialog.manualSequence.speedChanged");
        }

        // SAVE 버튼을 눌렀을 때만 설정 파일에 저장한 뒤 Runtime 속도에 적용한다.
        private void SaveSequenceSpeedSettings()
        {
            double requestedManual = (double)numSpeedPercent.Value;
            double requestedReady = (double)numReadySpeedPercent.Value;

            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null)
                    throw new InvalidOperationException("AppSettings를 찾을 수 없습니다.");

                settings.ManualSequenceScalePercent = requestedManual;
                settings.ReadySequenceScalePercent = requestedReady;
                AppSettingsStore.Save();

                QMC.Common.Motion.MotionSpeedScale.ManualSequencePercent = requestedManual;
                QMC.Common.Motion.MotionSpeedScale.ReadySequencePercent = requestedReady;
                btnSaveSpeedPercent.BackColor = System.Drawing.Color.FromArgb(0, 145, 80);
                Lang.BindFormat(statusLabel, "extraDialog.manualSequence.speedSaved", requestedManual.ToString("0.###"), requestedReady.ToString("0.###"));

                QMC.Common.Log.Write("Main", "SYSTEM", "SequenceSpeedScale",
                    "Manual/Ready 시퀀스 속도 퍼센트를 SAVE 버튼으로 저장하고 적용했습니다. manual=" +
                    requestedManual.ToString("0.###") +
                    ", ready=" + requestedReady.ToString("0.###") +
                    " (이동 속도/가감속 동일 배율 적용) - Set");
            }
            catch (Exception ex)
            {
                btnSaveSpeedPercent.BackColor = System.Drawing.Color.FromArgb(192, 48, 48);

                QMC.Common.Log.Write("Main", "SYSTEM", "SequenceSpeedScale",
                    "Manual/Ready 시퀀스 속도 퍼센트 저장에 실패했습니다. error=" +
                    ex.Message + " - Failed");
                Lang.BindFormat(statusLabel, "extraDialog.manualSequence.saveFailed", ex.Message);
            }
            finally
            {
            }
        }

        // 개별 Output LOAD/UNLOAD의 물리적 Side. ALL은 별도 Controller 진입점을 사용한다.
        private BinSide SelectedOutputSide()
        {
            return rbOutputNg.Checked ? BinSide.Ng : BinSide.Good;
        }

        private void WireEvents()
        {
            btnInputLoad.Click += async delegate
            {
                LoadTargetItem target = ResolveSelectedTarget(cmbInputLoadTarget, CassetteMaterialRole.Input1);
                string label = target.SlotIndex >= 0
                    ? "INPUT LOAD(" + target.Text + ")"
                    : "INPUT LOAD";

                if (!ConfirmManualProcessStart(
                    "INPUT LOAD",
                    "대상: " + target.Text))
                {
                    return;
                }

                await RunManualProcessAsync(label,
                    () => _controller.RunManualInputLoadAsync(target.Role, target.SlotIndex)).ConfigureAwait(true);
            };
            btnInputUnload.Click += async delegate
            {
                if (!ConfirmManualProcessStart(
                    "INPUT UNLOAD",
                    "Input Stage 자재를 원본 슬롯으로 배출합니다."))
                {
                    return;
                }

                await RunManualProcessAsync("INPUT UNLOAD", _controller.RunManualInputUnloadAsync).ConfigureAwait(true);
            };
            btnRefreshLoadTargets.Click += delegate { RefreshLoadTargets(); };
            btnSaveSpeedPercent.Click += delegate { SaveSequenceSpeedSettings(); };

            btnPickUp.Click += async delegate { await RunPickerProcessAsync("PickUp", "PICK UP").ConfigureAwait(true); };
            btnBottom.Click += async delegate { await RunPickerProcessAsync("Bottom", "BOTTOM").ConfigureAwait(true); };
            btnSide.Click += async delegate { await RunPickerProcessAsync("Side", "SIDE").ConfigureAwait(true); };
            btnPlace.Click += async delegate { await RunPickerProcessAsync("Place", "PLACE").ConfigureAwait(true); };
            btnPickUpZTest.Click += async delegate { await RunPickerPickUpZMotionTestAsync().ConfigureAwait(true); };
            btnAllStep.Click += async delegate { await RunUnitStepAsync(SequenceUnitKind.All, "ALL STEP").ConfigureAwait(true); };
            btnClose.Click += delegate { Close(); };
        }

        private async void btnOutputLoad_Click(object sender, EventArgs e)
        {
            if (_busy)
                return;

            if (rdoOutputAll.Checked)
            {
                if (!ConfirmManualProcessStart("OUTPUT LOAD(ALL)",
                    "출력 구분: ALL / 기본 순서: NG → GOOD" + Environment.NewLine +
                    "빈 Stage에 각 구분의 다음 순번 Bin을 로딩합니다. NG 미사용 시 신규 NG 공급은 제외합니다." + Environment.NewLine +
                    "Feeder 잔류 자재는 기존 재개 조건을 먼저 확인하며, 실패·정지 시 다음 대상을 시작하지 않습니다."))
                    return;

                await RunManualProcessAsync("OUTPUT LOAD(ALL)",
                    _controller.RunManualOutputLoadAllAsync, true).ConfigureAwait(true);
                return;
            }

            BinSide side = SelectedOutputSide();
            LoadTargetItem target = ResolveSelectedTarget(cmbOutputLoadTarget, CassetteMaterialRole.Good1);
            string label = target.SlotIndex >= 0
                ? "OUTPUT LOAD(" + side + " " + target.Text + ")"
                : "OUTPUT LOAD(" + side + ")";
            if (!ConfirmManualProcessStart("OUTPUT LOAD",
                "출력 구분: " + side.ToString().ToUpperInvariant() + Environment.NewLine +
                "대상: " + target.Text))
                return;

            await RunManualProcessAsync(label,
                () => _controller.RunManualOutputLoadAsync(side, target.Role, target.SlotIndex)).ConfigureAwait(true);
        }

        private async void btnOutputUnload_Click(object sender, EventArgs e)
        {
            if (_busy)
                return;

            if (rdoOutputAll.Checked)
            {
                if (!ConfirmManualProcessStart("OUTPUT UNLOAD(ALL)",
                    "출력 구분: ALL / 기본 순서: NG → GOOD" + Environment.NewLine +
                    "자재가 있는 Stage를 원본 카세트 슬롯으로 배출하고 종료합니다." + Environment.NewLine +
                    "Feeder 잔류 자재는 기존 배출 조건을 먼저 확인하며, 실패·정지 시 다음 대상을 시작하지 않습니다."))
                    return;

                await RunManualProcessAsync("OUTPUT UNLOAD(ALL)",
                    _controller.RunManualOutputUnloadAllAsync, true).ConfigureAwait(true);
                return;
            }

            BinSide side = SelectedOutputSide();
            if (!ConfirmManualProcessStart("OUTPUT UNLOAD",
                "출력 구분: " + side.ToString().ToUpperInvariant() + Environment.NewLine +
                "선택한 Stage/Feeder 자재를 원본 카세트 슬롯으로 배출합니다."))
                return;

            await RunManualProcessAsync("OUTPUT UNLOAD(" + side + ")",
                () => _controller.RunManualOutputUnloadAsync(side)).ConfigureAwait(true);
        }

        private void rbOutputGood_CheckedChanged(object sender, EventArgs e)
        {
            if (rbOutputGood.Checked)
                RefreshLoadTargets();
        }

        private void rbOutputNg_CheckedChanged(object sender, EventArgs e)
        {
            if (rbOutputNg.Checked)
                RefreshLoadTargets();
        }

        private void rdoOutputAll_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoOutputAll.Checked)
                RefreshLoadTargets();
        }

        // LOAD/UNLOAD는 실제 Auto 시퀀스와 동일한 모션·인터락을 사용하므로 실행 직전에 작업자 확인을 받는다.
        private bool ConfirmManualProcessStart(string processName, string detail)
        {
            string message = Lang.Format("extraDialog.manualSequence.confirmStart", Lang.Display(processName));
            if (!string.IsNullOrWhiteSpace(detail))
                message += Environment.NewLine + Environment.NewLine + detail;

            return QMC.Common.MessageDialog.Show(
                this,
                message,
                Lang.T("extraDialog.manualSequence.confirmTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private Task RunManualProcessAsync(string label, Func<Task<int>> action)
        {
            return RunManualProcessAsync(label, progress => action(), false);
        }

        private async Task RunManualProcessAsync(string label, Func<IProgress<string>, Task<int>> action, bool isOutputBatch)
        {
            if (_busy)
                return;

            int runId = ++_manualRunId;
            bool acceptProgress = true;
            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                Lang.BindFormat(statusLabel, "extraDialog.manualSequence.running", Lang.Display(label));

                var progress = new Progress<string>(message =>
                {
                    if (acceptProgress && _busy && runId == _manualRunId && !IsDisposed && !Disposing)
                        Lang.Bind(statusLabel, message);
                });
                int result = await action(progress).ConfigureAwait(true);
                // Progress는 UI 큐에 게시되므로 최종 결과나 다음 실행의 상태를 덮어쓰지 못하게 막는다.
                acceptProgress = false;
                string batchMessage = isOutputBatch ? _controller.LastManualOutputBatchMessage : null;
                if (result == 0)
                {
                    Lang.Bind(statusLabel, string.IsNullOrWhiteSpace(batchMessage) ? label + " 완료." : batchMessage);
                    return;
                }

                ShowFailure(!string.IsNullOrWhiteSpace(batchMessage) ? batchMessage :
                    string.IsNullOrWhiteSpace(_controller.LastActionFailureMessage)
                    ? label + " 실행 실패"
                    : _controller.LastActionFailureMessage);
            }
            catch (Exception ex)
            {
                acceptProgress = false;
                string batchMessage = isOutputBatch ? _controller.LastManualOutputBatchMessage : null;
                ShowError((string.IsNullOrWhiteSpace(batchMessage) ? label : batchMessage) +
                    " 실행 중 예외가 발생했습니다. " + ex.Message);
            }
            finally
            {
                acceptProgress = false;
                _busy = false;
                SetButtonsEnabled(true);
                // 이송 결과로 슬롯 상태가 바뀌므로 선택 목록을 최신 Material로 갱신한다.
                RefreshLoadTargets(!isOutputBatch);
            }
        }

        private async Task RunUnitStepAsync(SequenceUnitKind unit, string label)
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                Lang.BindFormat(statusLabel, "extraDialog.manualSequence.running", Lang.Display(label));

                int result = unit == SequenceUnitKind.All
                    ? await _controller.RunProcessSequenceStepAsync().ConfigureAwait(true)
                    : await _controller.RunManualSequenceUnitStepAsync(unit).ConfigureAwait(true);

                if (result == 0)
                {
                    Lang.BindFormat(statusLabel, "extraDialog.manualSequence.requested", Lang.Display(label));
                    return;
                }

                ShowFailure(string.IsNullOrWhiteSpace(_controller.LastActionFailureMessage)
                    ? "Manual 시퀀스 실행 실패"
                    : _controller.LastActionFailureMessage);
            }
            catch (Exception ex)
            {
                ShowError("Manual 시퀀스 실행 중 예외가 발생했습니다. " + ex.Message);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private async Task RunPickerProcessAsync(string processName, string label)
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                PickerSequenceSide side = rbRearPicker.Checked ? PickerSequenceSide.Rear : PickerSequenceSide.Front;
                Lang.BindFormat(statusLabel, "extraDialog.manualSequence.sideRunning", Lang.Display(side.ToString()), Lang.Display(label));

                int result = await _controller.RunManualPickerProcessAsync(side, processName).ConfigureAwait(true);
                if (result == 0)
                {
                    Lang.BindFormat(statusLabel, "extraDialog.manualSequence.sideComplete", Lang.Display(side.ToString()), Lang.Display(label));
                    return;
                }

                ShowFailure(string.IsNullOrWhiteSpace(_controller.LastActionFailureMessage)
                    ? "Picker Manual 공정 실행 실패"
                    : _controller.LastActionFailureMessage);
            }
            catch (Exception ex)
            {
                ShowError("Picker Manual 공정 실행 중 예외가 발생했습니다. " + ex.Message);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private async Task RunPickerPickUpZMotionTestAsync()
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                PickerSequenceSide side = rbRearPicker.Checked ? PickerSequenceSide.Rear : PickerSequenceSide.Front;
                int pickerNo = ResolveSelectedPickerNo();
                string label = side + " PICK Z TEST #" + pickerNo;
                Lang.BindFormat(statusLabel, "extraDialog.manualSequence.running", Lang.Display(label));

                int result = await _controller.RunManualPickerPickUpZMotionTestAsync(side, pickerNo).ConfigureAwait(true);
                if (result == 0)
                {
                    Lang.BindFormat(statusLabel, "extraDialog.manualSequence.complete", Lang.Display(label));
                    return;
                }

                ShowFailure(string.IsNullOrWhiteSpace(_controller.LastActionFailureMessage)
                    ? "PickUp Z 단독 테스트 실패"
                    : _controller.LastActionFailureMessage);
            }
            catch (Exception ex)
            {
                ShowError("PickUp Z 단독 테스트 중 예외가 발생했습니다. " + ex.Message);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private int ResolveSelectedPickerNo()
        {
            int pickerNo;
            if (cmbPickerNo.SelectedItem != null &&
                int.TryParse(cmbPickerNo.SelectedItem.ToString(), out pickerNo))
                return pickerNo;

            return 1;
        }

        private void ShowFailure(string message)
        {
            Lang.Bind(statusLabel, message);
            QMC.Common.MessageDialog.Show(this, message, Lang.T("extraDialog.manualSequenceDialog.titleLabel.caption"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ShowError(string message)
        {
            Lang.Bind(statusLabel, message);
            QMC.Common.MessageDialog.Show(this, message, Lang.T("extraDialog.manualSequenceDialog.titleLabel.caption"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            foreach (Control control in Controls)
                SetButtonsEnabledRecursive(control, enabled);

            rbOutputGood.Enabled = enabled;
            rbOutputNg.Enabled = enabled;
            rdoOutputAll.Enabled = enabled;
            cmbInputLoadTarget.Enabled = enabled;
            cmbOutputLoadTarget.Enabled = enabled && !rdoOutputAll.Checked;
            numSpeedPercent.Enabled = enabled;
            numReadySpeedPercent.Enabled = enabled;
        }

        private static void SetButtonsEnabledRecursive(Control control, bool enabled)
        {
            // 레시피 MANUAL ACTION과 같은 ActionButton도 수동 시퀀스 실행 중에는 중복 조작을 차단한다.
            if (control is Button || control is QMC.CDT_320.Ui.Controls.ActionButton)
                control.Enabled = enabled;

            foreach (Control child in control.Controls)
                SetButtonsEnabledRecursive(child, enabled);
        }
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(titleLabel, "extraDialog.manualSequenceDialog.titleLabel.caption");
            Lang.BindKey(btnInputLoad, "extraDialog.manualSequenceDialog.btnInputLoad.caption");
            Lang.BindKey(btnInputUnload, "extraDialog.manualSequenceDialog.btnInputUnload.caption");
            Lang.BindKey(btnOutputLoad, "extraDialog.manualSequenceDialog.btnOutputLoad.caption");
            Lang.BindKey(btnOutputUnload, "extraDialog.manualSequenceDialog.btnOutputUnload.caption");
            Lang.BindKey(rdoOutputAll, "extraDialog.manualSequenceDialog.rdoOutputAll.caption");
            Lang.BindKey(rbOutputNg, "extraDialog.manualSequenceDialog.rbOutputNg.caption");
            Lang.BindKey(rbOutputGood, "extraDialog.manualSequenceDialog.rbOutputGood.caption");
            Lang.BindKey(lblOutputSide, "extraDialog.manualSequenceDialog.lblOutputSide.caption");
            Lang.BindKey(lblPickerNo, "extraDialog.manualSequenceDialog.lblPickerNo.caption");
            Lang.BindKey(rbRearPicker, "extraDialog.manualSequenceDialog.rbRearPicker.caption");
            Lang.BindKey(rbFrontPicker, "extraDialog.manualSequenceDialog.rbFrontPicker.caption");
            Lang.BindKey(btnPickUp, "extraDialog.manualSequenceDialog.btnPickUp.caption");
            Lang.BindKey(btnBottom, "extraDialog.manualSequenceDialog.btnBottom.caption");
            Lang.BindKey(btnSide, "extraDialog.manualSequenceDialog.btnSide.caption");
            Lang.BindKey(btnPlace, "extraDialog.manualSequenceDialog.btnPlace.caption");
            Lang.BindKey(btnPickUpZTest, "extraDialog.manualSequenceDialog.btnPickUpZTest.caption");
            Lang.BindKey(btnAllStep, "extraDialog.manualSequenceDialog.btnAllStep.caption");
            Lang.BindKey(btnClose, "extraDialog.manualSequenceDialog.btnClose.caption");
            Lang.BindKey(btnSaveSpeedPercent, "extraDialog.manualSequenceDialog.btnSaveSpeedPercent.caption");
            Lang.BindKey(lblReadySpeedPercent, "extraDialog.manualSequenceDialog.lblReadySpeedPercent.caption");
            Lang.BindKey(lblSpeedPercent, "extraDialog.manualSequenceDialog.lblSpeedPercent.caption");
            Lang.BindKey(btnRefreshLoadTargets, "extraDialog.manualSequenceDialog.btnRefreshLoadTargets.caption");
            Lang.BindKey(lblOutputLoadTarget, "extraDialog.manualSequenceDialog.lblOutputLoadTarget.caption");
            Lang.BindKey(lblInputLoadTarget, "extraDialog.manualSequenceDialog.lblInputLoadTarget.caption");
            Load += (sender, args) => Lang.Apply(this);
        }

    }
}
