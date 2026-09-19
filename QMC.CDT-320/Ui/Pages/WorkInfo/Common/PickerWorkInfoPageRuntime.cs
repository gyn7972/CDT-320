using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    internal enum PickerManualSequenceKind
    {
        Process,
        PickUp,
        Inspect,
        Bottom,
        Side,
        Place,
        Recover
    }

    internal sealed class PickerWorkInfoPageRuntime
    {
        private readonly PageBase _owner;
        private readonly PickerSequenceSide _side;
        private readonly Func<Form1> _getHost;
        private readonly Label _lblHeader;
        private readonly Label[] _headValueLabels;
        private readonly Label _lblColletChangeValue;
        private readonly Label _lblAutoPosValue;
        private readonly Label _lblColletCleaningValue;
        private readonly Label _lblColletCleanHistoryValue;
        private readonly Label _lblColletCheckValue;
        private readonly Label _lblPickFailValue;
        private readonly Label _lblPlaceFailValue;
        private readonly Label _lblHeadZoneValue;
        private readonly Label _lblProcessDetailValue;
        private readonly Label[] _colletUseTitleLabels;
        private readonly Label[] _colletUseValueLabels;
        private readonly IndicatorDot[] _vacuumDots;
        private readonly IndicatorDot[] _blowDots;
        private readonly Label[] _vacuumLabels;
        private readonly Label[] _blowLabels;
        private readonly Label[] _axisValueLabels;
        private readonly MaterialDetailView _headDieDetailView;
        private readonly RadioButton[] _headSelectButtons;
        private int _selectedHeadNo = 1;
        private readonly Button _btnCountClear;
        private readonly ActionButton _btnInput;
        private readonly ActionButton _btnInspect;
        private readonly ActionButton _btnBottom;
        private readonly ActionButton _btnSide;
        private readonly ActionButton _btnOutput;
        private readonly ActionButton _btnPickUpTest;
        private readonly ComboBox _cmbPickZTestPickerNo;
        private readonly ActionButton _btnPickZTest;
        private readonly ActionButton _btnStop;
        private readonly Control.ControlCollection _actionControls;
        private readonly System.Windows.Forms.Timer _timer;
        private PickerProcessSequence _stepSequence;
        private PickerPickUpSequence _pickUpStepSequence;
        private PickerBottomInspectionSequence _bottomInspectStepSequence;
        private PickerSideInspectionSequence _sideInspectStepSequence;
        private PickerPlaceSequence _placeStepSequence;
        private InputPickTargetSelectDialog _pickUpTestDialog;
        private bool _manualSequenceRunning;
        private string _lastStableProcess = "AVOID";

        private DateTime _colletCleaningHistoryLoadedAt = DateTime.MinValue;
        private string _colletCleaningHistoryText = "-";

        /// <summary>
        /// 콜렛 클리닝 이력을 요약해서 표시한다(가장 최근에 클리닝한 콜렛 기준).
        /// 파일 I/O이므로 5초 간격으로만 다시 읽는다.
        /// </summary>
        private void UpdateColletCleaningHistoryDisplay()
        {
            if (_lblColletCleanHistoryValue == null)
                return;

            try
            {
                if ((DateTime.Now - _colletCleaningHistoryLoadedAt).TotalSeconds >= 5.0)
                {
                    _colletCleaningHistoryLoadedAt = DateTime.Now;
                    _colletCleaningHistoryText = BuildColletCleaningHistoryText();
                }

                _lblColletCleanHistoryValue.Text = _colletCleaningHistoryText;
            }
            catch (Exception ex)
            {
                _lblColletCleanHistoryValue.Text = "-";
                System.Diagnostics.Debug.WriteLine("Collet cleaning history display failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private string BuildColletCleaningHistoryText()
        {
            CalibrationData data = CalibrationDataStore.LoadOrCreate();
            if (data == null)
                return "-";

            data.EnsureObjects();
            VisionFocusPickerSide side = _side == PickerSequenceSide.Front
                ? VisionFocusPickerSide.Front
                : VisionFocusPickerSide.Rear;

            ColletCleaningHistoryRecord latest = null;
            int latestColletNo = 0;
            for (int colletNo = 1; colletNo <= 4; colletNo++)
            {
                ColletCleaningHistoryRecord record = data.ColletCleaningHistory.Get(side, colletNo);
                if (record == null || !record.HasHistory)
                    continue;

                if (latest == null || record.LastCleanedAt > latest.LastCleanedAt)
                {
                    latest = record;
                    latestColletNo = colletNo;
                }
            }

            if (latest == null)
                return "-";

            return "#" + latestColletNo + " " + latest.LastCleanedAt.ToString("MM-dd HH:mm") +
                   " " + latest.LastResult;
        }

        public PickerWorkInfoPageRuntime(
            PageBase owner,
            PickerSequenceSide side,
            Func<Form1> getHost,
            Label lblHeader,
            Label[] headValueLabels,
            Label lblColletChangeValue,
            Label lblAutoPosValue,
            Label lblColletCleaningValue,
            Label lblColletCheckValue,
            Label lblPickFailValue,
            Label lblPlaceFailValue,
            Label lblHeadZoneValue,
            Label lblProcessDetailValue,
            Label[] colletUseTitleLabels,
            Label[] colletUseValueLabels,
            IndicatorDot[] vacuumDots,
            IndicatorDot[] blowDots,
            Label[] vacuumLabels,
            Label[] blowLabels,
            Label[] axisValueLabels,
            MaterialDetailView headDieDetailView,
            RadioButton[] headSelectButtons,
            Button btnCountClear,
            ActionButton btnInput,
            ActionButton btnInspect,
            ActionButton btnBottom,
            ActionButton btnSide,
            ActionButton btnOutput,
            ActionButton btnPickUpTest,
            ComboBox cmbPickZTestPickerNo,
            ActionButton btnPickZTest,
            ActionButton btnStop,
            Control.ControlCollection actionControls,
            // 콜렛 클리닝 이력 표시(선택). 전달하지 않으면 표시를 생략한다.
            Label lblColletCleanHistoryValue = null)
        {
            _lblColletCleanHistoryValue = lblColletCleanHistoryValue;
            _owner = owner;
            _side = side;
            _getHost = getHost;
            _lblHeader = lblHeader;
            _headValueLabels = headValueLabels ?? new Label[0];
            _lblColletChangeValue = lblColletChangeValue;
            _lblAutoPosValue = lblAutoPosValue;
            _lblColletCleaningValue = lblColletCleaningValue;
            _lblColletCheckValue = lblColletCheckValue;
            _lblPickFailValue = lblPickFailValue;
            _lblPlaceFailValue = lblPlaceFailValue;
            _lblHeadZoneValue = lblHeadZoneValue;
            _lblProcessDetailValue = lblProcessDetailValue;
            _colletUseTitleLabels = colletUseTitleLabels ?? new Label[0];
            _colletUseValueLabels = colletUseValueLabels ?? new Label[0];
            _vacuumDots = vacuumDots ?? new IndicatorDot[0];
            _blowDots = blowDots ?? new IndicatorDot[0];
            _vacuumLabels = vacuumLabels ?? new Label[0];
            _blowLabels = blowLabels ?? new Label[0];
            _axisValueLabels = axisValueLabels ?? new Label[0];
            _headDieDetailView = headDieDetailView;
            _headSelectButtons = headSelectButtons ?? new RadioButton[0];
            _btnCountClear = btnCountClear;
            _btnInput = btnInput;
            _btnInspect = btnInspect;
            _btnBottom = btnBottom;
            _btnSide = btnSide;
            _btnOutput = btnOutput;
            _btnPickUpTest = btnPickUpTest;
            _cmbPickZTestPickerNo = cmbPickZTestPickerNo;
            _btnPickZTest = btnPickZTest;
            _btnStop = btnStop;
            _actionControls = actionControls;

            BindLocalizedCaptions();
            WireEvents();
            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) =>
            {
                if (!PageBase.ShouldRefreshVisible(_owner))
                    return;

                Refresh();
            };
            _owner.VisibleChanged += (s, e) => { if (_owner.Visible) _timer.Start(); else _timer.Stop(); };
            _owner.HandleDestroyed += (s, e) =>
            {
                _timer.Stop();
                ResetStepSequence();
                ClosePickUpTestDialog();
            };
            Refresh();
        }

        private string SideName
        {
            get { return _side == PickerSequenceSide.Front ? "Front Picker" : "Rear Picker"; }
        }

        private string LogCode
        {
            get { return _side == PickerSequenceSide.Front ? "FRONT-PICKER-PAGE" : "REAR-PICKER-PAGE"; }
        }

        private void WireEvents()
        {
            _btnInput.Click += async (s, e) => await RunSequenceAction(SideName + " PICK UP", SequenceRunMode.Manual, PickerManualSequenceKind.PickUp);
            _btnInspect.Click += async (s, e) => await RunSequenceAction(SideName + " INSPECT", SequenceRunMode.Manual, PickerManualSequenceKind.Inspect);
            _btnBottom.Click += async (s, e) => await RunSequenceAction(SideName + " BOTTOM", SequenceRunMode.Manual, PickerManualSequenceKind.Bottom);
            _btnSide.Click += async (s, e) => await RunSequenceAction(SideName + " SIDE", SequenceRunMode.Manual, PickerManualSequenceKind.Side);
            _btnOutput.Click += async (s, e) => await RunSequenceAction(SideName + " PLACE", SequenceRunMode.Manual, PickerManualSequenceKind.Place);
            if (_btnPickUpTest != null)
                _btnPickUpTest.Click += (s, e) => ShowPickUpTestDialog();
            if (_btnPickZTest != null)
            {
                _btnPickZTest.Visible = false;
                _btnPickZTest.Enabled = false;
            }
            _btnStop.Click += async (s, e) => await StopManualActionAsync();
            if (_btnCountClear != null)
                _btnCountClear.Click += (s, e) => ClearCounters();

            if (_cmbPickZTestPickerNo != null && _cmbPickZTestPickerNo.SelectedIndex < 0)
                _cmbPickZTestPickerNo.SelectedIndex = 0;
            if (_cmbPickZTestPickerNo != null)
                _cmbPickZTestPickerNo.Visible = false;

            for (int i = 0; i < _headSelectButtons.Length; i++)
            {
                RadioButton button = _headSelectButtons[i];
                if (button == null)
                    continue;

                int headNo = i + 1;
                button.Click += (s, e) => SelectHead(headNo);
            }

            // [검사 재실행 2026-08-27 팀장님 지시] BOTTOM/SIDE CLEAR — 픽커 헤드에 물려 있는
            // (아직 Place 전) 다이들의 Bottom/Side 검사 데이터를 삭제해 재검사 가능 상태로 되돌린다.
            if (_headDieDetailView != null)
                _headDieDetailView.ClearInspectionDataRequested += (s, e) => ClearHeadInspectionData();
        }

        // Bottom/Side 검사 데이터 삭제 → 이후 BOTTOM/SIDE 버튼으로 재검사(대상=물린 다이 전부,
        // 결과는 Upsert 교체). Result=Unknown 리셋으로 NG 래치(기존 NG면 재검사 OK여도 NG 유지)가
        // 풀리고, Place는 검사 흐름 미완료 차단이 있어 재검사 전 진행이 안전하게 막힌다.
        private void ClearHeadInspectionData()
        {
            try
            {
                Form1 host = _getHost();
                var controller = host != null ? host.Controller : null;
                if (controller != null &&
                    (controller.Status == EquipmentStatus.AutoRunning ||
                     controller.Status == EquipmentStatus.Initializing ||
                     controller.IsSequenceRunning ||
                     controller.IsManualBusy))
                {
                    QMC.Common.MessageDialog.Show(_owner,
                        "장비 동작 중에는 검사 데이터를 삭제할 수 없습니다.\r\nAuto/Manual 동작을 정지한 뒤 다시 시도하세요.",
                        SideName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MaterialLocationKind location = _side == PickerSequenceSide.Front
                    ? MaterialLocationKind.PickerFront
                    : MaterialLocationKind.PickerRear;

                var targetLines = new System.Text.StringBuilder();
                int targetCount = 0;
                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    DieMaterial die = MaterialStateService.GetDieAtPicker(location, pickerNo);
                    if (die == null)
                        continue;

                    targetCount++;
                    targetLines.AppendLine("HEAD #" + pickerNo + " : " + die.DieId + " (Result=" + die.Result + ")");
                }

                if (targetCount == 0)
                {
                    QMC.Common.MessageDialog.Show(_owner,
                        "픽커에 물려 있는 Die가 없습니다.",
                        SideName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult confirm = QMC.Common.MessageDialog.Show(_owner,
                    SideName + " 헤드 Die " + targetCount + "개의 BOTTOM/SIDE 검사 데이터를 삭제하시겠습니까?\r\n" +
                    "삭제 후 BOTTOM/SIDE 버튼으로 재검사할 수 있으며, 재검사 완료 전에는 PLACE가 차단됩니다.\r\n\r\n" +
                    targetLines,
                    SideName + " 검사 데이터 삭제", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                string message;
                System.Collections.Generic.List<string> clearedDies;
                bool cleared = MaterialStateService.ClearPickerHeadInspectionData(
                    location,
                    LogCode + ":ManualInspectionClear",
                    out message,
                    out clearedDies);

                MaterialStateService.TryFlushPendingSave(SideName + "InspectionClear");
                RefreshHeadDieDetail();

                QMC.Common.MessageDialog.Show(_owner,
                    message + (cleared ? "\r\nBOTTOM → SIDE 버튼으로 재검사를 진행하세요." : string.Empty),
                    SideName + " 검사 데이터 삭제", MessageBoxButtons.OK,
                    cleared ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", LogCode,
                    "헤드 Die 검사 데이터 삭제 실패: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(_owner,
                    "검사 데이터 삭제 실패:\r\n" + ex.Message,
                    SideName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectHead(int headNo)
        {
            _selectedHeadNo = Math.Max(1, Math.Min(4, headNo));
            UpdateHeadSelectButtons();
            RefreshHeadDieDetail();
        }

        private void UpdateHeadSelectButtons()
        {
            for (int i = 0; i < _headSelectButtons.Length; i++)
            {
                RadioButton button = _headSelectButtons[i];
                if (button == null)
                    continue;

                bool selected = (i + 1) == _selectedHeadNo;
                button.Checked = selected;
            }
        }

        public void Refresh()
        {
            try
            {
                Form1 host = _getHost();
                CDT320_Machine machine = host != null ? host.Machine : null;
                if (machine == null)
                {
                    SetEmptyMonitor();
                    return;
                }

                RefreshSummary(machine);
                RefreshAxisLabels(machine);
                RefreshHeadDieDetail();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void BindLocalizedCaptions()
        {
            Lang.Bind(_lblHeader, _side == PickerSequenceSide.Front ? "FRONT PICKER" : "REAR PICKER");
            BindCaption("grpState", "WORK INFO");
            BindCaption("lblHeadZoneTitle", "HEAD ZONE");
            BindCaption("lblHead1Title", "HEAD #1");
            BindCaption("lblHead2Title", "HEAD #2");
            BindCaption("lblHead3Title", "HEAD #3");
            BindCaption("lblHead4Title", "HEAD #4");
            BindCaption("lblColletChangeTitle", "COLLET CHANGE");
            BindCaption("lblAutoPosTitle", "AUTO POSITION");
            BindCaption("lblColletCleaningTitle", "COLLET CLEANING");
            BindCaption("lblColletCheckTitle", "COLLET CHECK");
            BindCaption("grpCounters", "COUNTER");
            BindCaption("lblColletCleanTitle", "LAST CLEAN");
            BindCaption("lblPickFailTitle", "PICK FAIL");
            BindCaption("lblPlaceFailTitle", "PLACE FAIL");
            BindCaption("btnCountClear", "COUNT CLEAR");
            BindCaption("grpInfo", "INFO");
            BindCaption("lblProcessDetailTitle", "PROCESS");
            BindCaption("lblAxis1Title", "PICKER X");
            BindCaption("lblAxis2Title", "PICKER Y");
            BindCaption("lblAxis3Title", "PICKER T#1");
            BindCaption("lblAxis4Title", "PICKER Z#1");
            BindCaption("lblAxis5Title", "PICKER T#2");
            BindCaption("lblAxis6Title", "PICKER Z#2");
            BindCaption("lblAxis7Title", "PICKER T#3");
            BindCaption("lblAxis8Title", "PICKER Z#3");
            BindCaption("lblAxis9Title", "PICKER T#4");
            BindCaption("lblAxis10Title", "PICKER Z#4");
            BindCaption("grpSensor", "SENSOR STATE");
            BindCaption("lblHeadDieTitle", "HEAD DIE");
            BindCaption("btnHead1Select", "HEAD 1");
            BindCaption("btnHead2Select", "HEAD 2");
            BindCaption("btnHead3Select", "HEAD 3");
            BindCaption("btnHead4Select", "HEAD 4");
            BindCaption("grpAction", "ACTION");
            BindCaption("btnInput", "PICK UP");
            BindCaption("btnInspect", "INSPECT");
            BindCaption("btnBottom", "BOTTOM");
            BindCaption("btnSide", "SIDE");
            BindCaption("btnOutput", "PLACE");
            BindCaption("btnPickUpTest", "PICKUP TEST");
            BindCaption("btnAjinLineMapTest", "LINE MAP TEST");
            BindCaption("btnAjinLineMoveTest", "LINE MOVE TEST");
            BindCaption("btnVisionBottomInspect", "VISION: BOTTOM INSP");
            BindCaption("btnVisionFrontSide", "VISION: FRONT SIDE");
            BindCaption("btnVisionRearSide", "VISION: REAR SIDE");
            BindCaption("btnStop", "STOP");
        }

        private void BindCaption(string controlName, string original)
        {
            foreach (Control control in _owner.Controls.Find(controlName, true))
                Lang.Bind(control, original);
        }

        private void SetEmptyMonitor()
        {
            for (int i = 0; i < _headValueLabels.Length; i++)
            {
                if (_headValueLabels[i] != null)
                    _headValueLabels[i].Text = "-";
            }
            _lblColletChangeValue.Text = "-";
            _lblAutoPosValue.Text = "-";
            _lblColletCleaningValue.Text = "-";
            _lblColletCheckValue.Text = "-";
            _lblPickFailValue.Text = "0 ea";
            _lblPlaceFailValue.Text = "0 ea";
            SetHeadZone("-");
            SetProcessDetail("-");
            for (int i = 0; i < _colletUseTitleLabels.Length; i++)
            {
                if (_colletUseTitleLabels[i] != null)
                {
                    _colletUseTitleLabels[i].Text = "#" + (i + 1) + " " + Lang.Display("COLLET USE");
                    _colletUseTitleLabels[i].BackColor = Color.FromArgb(0xC8, 0xC8, 0xC8);
                    _colletUseTitleLabels[i].ForeColor = Color.Black;
                }
            }
            for (int i = 0; i < _colletUseValueLabels.Length; i++)
            {
                if (_colletUseValueLabels[i] != null)
                    _colletUseValueLabels[i].Text = "0 ea";
            }
            for (int i = 0; i < _vacuumDots.Length; i++)
                SetDot(_vacuumDots[i], false);
            for (int i = 0; i < _blowDots.Length; i++)
                SetDot(_blowDots[i], false);
        }

        private void RefreshSummary(CDT320_Machine machine)
        {
            bool cdaOk = ReadInput(GetCdaPressure(machine));
            bool vacuumOk = ReadInput(GetVacuumPressure(machine));
            bool simulationOrDryRun = IsSimulationOrDryRun(machine);

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                int index = pickerNo - 1;
                bool flow = ReadInput(GetFlow(machine, pickerNo));
                bool vacuum = ReadOutput(GetVacuum(machine, pickerNo));
                bool blow = ReadOutput(GetBlow(machine, pickerNo));
                DieMaterial pickedDie = GetPickedDieMaterial(pickerNo);
                bool hasPickedMaterial = pickedDie != null;
                bool vacuumDisplay = vacuum || flow || (simulationOrDryRun && hasPickedMaterial);

                if (index < _headValueLabels.Length && _headValueLabels[index] != null)
                {
                    string headState = ResolveHeadState(machine, pickerNo, flow, pickedDie);
                    _headValueLabels[index].Text = headState.StartsWith("PICK / ", StringComparison.Ordinal)
                        ? Lang.Display("PICK") + headState.Substring(4)
                        : Lang.Display(headState);
                    _headValueLabels[index].ForeColor = IsPickedHeadState(headState) ? Color.Lime : Color.Black;
                }

                bool usePicker = UsePicker(machine, pickerNo);
                if (index < _colletUseTitleLabels.Length && _colletUseTitleLabels[index] != null)
                {
                    _colletUseTitleLabels[index].Text = "#" + pickerNo + " " + Lang.Display(usePicker ? "COLLET USE" : "COLLET UNUSED");
                    _colletUseTitleLabels[index].BackColor = usePicker ? Color.FromArgb(0x00, 0xB0, 0x50) : Color.FromArgb(0x96, 0x96, 0x96);
                    _colletUseTitleLabels[index].ForeColor = usePicker ? Color.White : Color.Gainsboro;
                }
                if (index < _colletUseValueLabels.Length && _colletUseValueLabels[index] != null)
                {
                    _colletUseValueLabels[index].Text = GetColletUseCount(machine, pickerNo) + " ea";
                    _colletUseValueLabels[index].BackColor = Color.White;
                    _colletUseValueLabels[index].ForeColor = Color.Black;
                }

                if (index < _vacuumLabels.Length && _vacuumLabels[index] != null)
                    _vacuumLabels[index].Text = Lang.Display("VACUUM") + " #" + pickerNo + "\r\n: " + Lang.Display(vacuumDisplay ? "ON" : "OFF");
                if (index < _blowLabels.Length && _blowLabels[index] != null)
                    _blowLabels[index].Text = Lang.Display("BLOW") + " #" + pickerNo + "\r\n: " + Lang.Display(blow ? "ON" : "OFF");

                if (index < _vacuumDots.Length)
                    SetDot(_vacuumDots[index], vacuumDisplay);
                if (index < _blowDots.Length)
                    SetDot(_blowDots[index], blow);
            }

            _lblColletChangeValue.Text = Lang.Display(cdaOk ? "READY" : "CHECK");
            _lblAutoPosValue.Text = Lang.Display(IsGroupInPosition(machine, "AvoidPosition") ? "AVOID" : "MOVING");
            _lblColletCleaningValue.Text = Lang.Display(vacuumOk ? "READY" : "CHECK");
            _lblColletCheckValue.Text = Lang.Display(cdaOk && vacuumOk ? "READY" : "CHECK");
            UpdateColletCleaningHistoryDisplay();
            _lblPickFailValue.Text = GetPickFailCount(machine) + " ea";
            _lblPlaceFailValue.Text = GetPlaceFailCount(machine) + " ea";
            string headZone = ResolveHeadZone(machine);
            string headProcess = ResolveHeadProcess(machine, headZone);
            bool pickerMoving = IsPickerMoving(machine);
            SetHeadZone(headZone);
            SetProcessDetail(ResolveProcessDetail(machine, headProcess, pickerMoving));

            if (!pickerMoving && IsStableProcess(headProcess))
                _lastStableProcess = NormalizeFlowProcess(headProcess);
        }

        private void ClearCounters()
        {
            try
            {
                Form1 host = _getHost();
                if (host == null || host.Machine == null)
                    return;

                DialogResult answer = QMC.Common.MessageDialog.Show(
                    _owner,
                    SideName + " count clear 진행하시겠습니까?",
                    SideName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                    return;

                if (_side == PickerSequenceSide.Front && host.Machine.PickerFrontUnit != null)
                    host.Machine.PickerFrontUnit.ResetWorkCounters();
                if (_side == PickerSequenceSide.Rear && host.Machine.PickerRearUnit != null)
                    host.Machine.PickerRearUnit.ResetWorkCounters();

                if (host.Controller != null)
                    host.Controller.SaveMachineRuntimeState(SideName + "WorkCounterClear");

                WriteEvent(SideName + " count clear.");
                Refresh();
            }
            catch (Exception ex)
            {
                WriteAlarm("Count clear failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(_owner, ex.Message, SideName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private static readonly PickerAxis[] AxisLabelOrder = new PickerAxis[]
        {
            PickerAxis.PickerX,
            PickerAxis.PickerY,
            PickerAxis.PickerT0,
            PickerAxis.PickerZ0,
            PickerAxis.PickerT1,
            PickerAxis.PickerZ1,
            PickerAxis.PickerT2,
            PickerAxis.PickerZ2,
            PickerAxis.PickerT3,
            PickerAxis.PickerZ3
        };

        private void RefreshAxisLabels(CDT320_Machine machine)
        {
            for (int i = 0; i < _axisValueLabels.Length && i < AxisLabelOrder.Length; i++)
            {
                Label label = _axisValueLabels[i];
                if (label == null)
                    continue;

                BaseAxis axis = GetAxis(machine, AxisLabelOrder[i]);
                label.Text = axis != null ? FormatAxisDisplay(axis.ActualPosition, axis) : "-";
            }
        }

        private void RefreshHeadDieDetail()
        {
            if (_headDieDetailView == null)
                return;

            UpdateHeadSelectButtons();

            string sidePrefix = _side == PickerSequenceSide.Front ? "FRONT" : "REAR";
            string title = sidePrefix + " PICKER HEAD #" + _selectedHeadNo + " DIE";
            DieMaterial die = GetPickedDieMaterial(_selectedHeadNo);
            _headDieDetailView.SetRows(title, BuildHeadDieRows(die));
        }

        private IEnumerable<MaterialDetailRow> BuildHeadDieRows(DieMaterial die)
        {
            return new[]
            {
                DetailRow("Die ID", die != null ? die.DieId : ""),
                DetailRow("Wafer", die != null ? die.WaferID_Input : ""),
                DetailRow("Sequence", die != null ? die.InputSequenceNo.ToString() : ""),
                DetailRow("Map X/Y", die != null ? die.Wafer_IndexX + " / " + die.Wafer_IndexY : ""),
                DetailRow("Location", die != null && die.CurrentLocation != null ? die.CurrentLocation.ToString() : ""),
                DetailRow("Result", die != null ? die.Result.ToString() : "-"),
                DetailRow("Input Target", die != null ? (die.IsInputTarget ? "YES" : "NO") : ""),
                DetailRow("NG Code", die != null && die.NgCodes != null && die.NgCodes.Count > 0 ? string.Join(",", die.NgCodes.ToArray()) : ""),
                DetailRow("Reason", "-")
            };
        }

        private static MaterialDetailRow DetailRow(string name, string value)
        {
            return new MaterialDetailRow
            {
                Name = name,
                Value = string.IsNullOrWhiteSpace(value) ? "-" : value,
                Editable = false
            };
        }

        private string FormatAxisDisplay(double nativeValue, BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return "-";

                return AxisUnitConverter.FormatDisplay(nativeValue, axis, "0.###", true);
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker 축 위치 단위 표시 실패: " + ex.Message);
                return nativeValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            }
            finally
            {
            }
        }

        private async Task RunSequenceAction(string actionName, SequenceRunMode mode)
        {
            await RunSequenceAction(actionName, mode, PickerManualSequenceKind.Process).ConfigureAwait(true);
        }

        private async Task RunSequenceAction(string actionName, SequenceRunMode mode, PickerManualSequenceKind kind)
        {
            IDisposable actionScope = null;
            bool showFailure = false;
            string exceptionMessage = null;
            try
            {
                Form1 host = _getHost();
                if (!ValidateManualSequenceHost(host, actionName))
                    return;

                if (_manualSequenceRunning)
                    return;
                SequenceStartMode startMode;
                if (!SelectManualSequenceStartMode(actionName, out startMode))
                    return;

                _manualSequenceRunning = true;
                SetButtonsEnabled(false);
                actionScope = host.Controller.BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, SideName + "WorkInfo:" + actionName);
                CancellationToken manualToken = host.Controller.ManualOperationToken;
                SequenceFailureStore.Clear();
                if (startMode == SequenceStartMode.Restart)
                    ResetStepSequence();

                WriteEvent(actionName + " 시작. 시작모드=" + FormatStartMode(startMode));

                Task<bool> actionTask = RunPickerSequenceTask(CreateContext(host), mode, kind, startMode, manualToken);
                Task cancelTask = WaitForCancellationAsync(manualToken);
                Task completed = await Task.WhenAny(actionTask, cancelTask).ConfigureAwait(true);
                if (completed == cancelTask)
                {
                    ObserveManualActionTask(actionTask, actionName);
                    WriteEvent(actionName + " 정지 요청으로 취소.");
                    return;
                }

                bool ok = await actionTask.ConfigureAwait(true);
                WriteEvent(actionName + " 결과=" + ok);
                if (!ok)
                    showFailure = true;
            }
            catch (OperationCanceledException)
            {
                WriteEvent(actionName + " 취소.");
            }
            catch (QMC.CDT320.ManualActionBlockedException ex)
            {
                // 수동 시작 거부는 장비 이상이 아니므로 알람을 올리지 않는다(알람은 전체 축 EStop 유발).
                EventLogger.Write(EventKind.Warning, "QMC", LogCode, actionName + " blocked: " + ex.Message);
                QMC.Common.MessageDialog.Show(
                    _owner,
                    "지금은 수동 동작을 시작할 수 없습니다.\r\n\r\n" + ex.Message,
                    SideName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                WriteAlarm(actionName + " 실패: " + ex.Message);
                exceptionMessage = ex.Message;
            }
            finally
            {
                try
                {
                    if (actionScope != null)
                        actionScope.Dispose();

                    if (showFailure || !string.IsNullOrWhiteSpace(exceptionMessage))
                        ResetStepSequence();

                    TrySaveMaterialStateAfterManualSequence(actionName);
                }
                catch (Exception ex)
                {
                    WriteAlarm(actionName + " 종료 처리 중 오류: " + ex.Message);
                }
                finally
                {
                    _manualSequenceRunning = false;
                    SetButtonsEnabledSafe(true);
                    RefreshSafe();
                }
            }

            if (showFailure)
                ShowFailure(actionName);

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(_owner, exceptionMessage, SideName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async Task RunPickUpZMotionTestActionAsync()
        {
            string actionName = SideName + " PICK Z TEST #" + ResolvePickUpZTestPickerNo();
            bool showFailure = false;
            string exceptionMessage = null;

            try
            {
                Form1 host = _getHost();
                if (!ValidateManualSequenceHost(host, actionName))
                    return;

                if (_manualSequenceRunning)
                    return;

                DialogResult answer = QMC.Common.MessageDialog.Show(
                    _owner,
                    actionName + " 실행하시겠습니까?\r\nMaterial/DieMap 상태는 변경하지 않고 PickUp Z 세부 모션만 테스트합니다.",
                    SideName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                    return;

                _manualSequenceRunning = true;
                SetButtonsEnabled(false);
                SequenceFailureStore.Clear();

                WriteEvent(actionName + " 시작.");
                int result = await host.Controller
                    .RunManualPickerPickUpZMotionTestAsync(_side, ResolvePickUpZTestPickerNo())
                    .ConfigureAwait(true);

                if (result == 0)
                {
                    WriteEvent(actionName + " 완료.");
                    return;
                }

                WriteAlarm(actionName + " 실패. " + host.Controller.LastActionFailureMessage);
                showFailure = true;
            }
            catch (OperationCanceledException)
            {
                WriteEvent(actionName + " 취소.");
            }
            catch (Exception ex)
            {
                WriteAlarm(actionName + " 예외: " + ex.Message);
                exceptionMessage = ex.Message;
            }
            finally
            {
                _manualSequenceRunning = false;
                SetButtonsEnabledSafe(true);
                RefreshSafe();
            }

            if (showFailure)
                ShowFailure(actionName);

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(_owner, exceptionMessage, SideName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowPickUpTestDialog()
        {
            string actionName = SideName + " SELECTED DIE PICKUP TEST";

            try
            {
                Form1 host = _getHost();
                if (!ValidateManualSequenceHost(host, actionName))
                    return;

                if (_pickUpTestDialog != null && !_pickUpTestDialog.IsDisposed)
                {
                    _pickUpTestDialog.Show();
                    _pickUpTestDialog.Activate();
                    return;
                }

                _pickUpTestDialog = new InputPickTargetSelectDialog(
                    host.Controller,
                    _side,
                    ResolvePickUpZTestPickerNo());
                _pickUpTestDialog.FormClosed += (s, e) => _pickUpTestDialog = null;

                IWin32Window ownerWindow = _owner.FindForm();
                if (ownerWindow != null)
                    _pickUpTestDialog.Show(ownerWindow);
                else
                    _pickUpTestDialog.Show();

                WriteEvent(actionName + " 다이얼로그 열림.");
            }
            catch (Exception ex)
            {
                WriteAlarm(actionName + " 다이얼로그 실행 예외: " + ex.Message);
                QMC.Common.MessageDialog.Show(_owner, ex.Message, SideName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ClosePickUpTestDialog()
        {
            try
            {
                if (_pickUpTestDialog == null || _pickUpTestDialog.IsDisposed)
                    return;

                _pickUpTestDialog.Close();
                _pickUpTestDialog = null;
            }
            catch
            {
                _pickUpTestDialog = null;
            }
            finally
            {
            }
        }

        private int ResolvePickUpZTestPickerNo()
        {
            try
            {
                int pickerNo;
                if (_cmbPickZTestPickerNo != null &&
                    _cmbPickZTestPickerNo.SelectedItem != null &&
                    int.TryParse(_cmbPickZTestPickerNo.SelectedItem.ToString(), out pickerNo))
                    return Math.Max(1, Math.Min(4, pickerNo));

                if (_cmbPickZTestPickerNo != null &&
                    int.TryParse(_cmbPickZTestPickerNo.Text, out pickerNo))
                    return Math.Max(1, Math.Min(4, pickerNo));

                return 1;
            }
            catch
            {
                return 1;
            }
            finally
            {
            }
        }

        private bool ValidateManualSequenceHost(Form1 host, string actionName)
        {
            if (host == null)
                return ShowManualSequenceHostError(actionName, "Form host is null.");

            if (host.Controller == null)
                return ShowManualSequenceHostError(actionName, "MachineController is null.");

            if (host.Machine == null)
                return ShowManualSequenceHostError(actionName, "Machine is null.");

            return true;
        }

        private bool ShowManualSequenceHostError(string actionName, string reason)
        {
            string message = actionName + " 실행 불가: " + reason;
            WriteAlarm(message);
            QMC.Common.MessageDialog.Show(_owner, message, SideName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private async Task<bool> RunPickerSequenceTask(
            MachineSequenceContext context,
            SequenceRunMode mode,
            PickerManualSequenceKind kind,
            SequenceStartMode startMode,
            CancellationToken ct)
        {
            try
            {
                PickerSequenceOptions options = BuildManualSequenceOptions(context, mode, startMode);

                int result = await RunManualPickerSequenceAsync(context, kind, options, ct).ConfigureAwait(false);
                return result == 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker sequence failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        public void ShowHeadDieDialog(int pickerNo)
        {
            try
            {
                using (PickerHeadDieDialog dialog = new PickerHeadDieDialog(_side, pickerNo))
                {
                    DialogResult result = dialog.ShowDialog(_owner);
                    if (result == DialogResult.OK)
                    {
                        MaterialStateService.TryFlushPendingSave(SideName + "HeadDieDialogUpdate");
                        Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker Head Die 정보 창 표시 실패: pickerNo=" + pickerNo + ", error=" + ex.Message);
                QMC.Common.MessageDialog.Show(
                    _owner,
                    "Picker Head Die 정보 창 표시 실패:\r\n" + ex.Message,
                    SideName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private PickerSequenceOptions BuildManualSequenceOptions(MachineSequenceContext context, SequenceRunMode mode, SequenceStartMode startMode)
        {
            PickerSequenceOptions options = PickerSequenceOptions.Default();
            options.RunMode = mode;
            options.StartMode = startMode;
            options.SimulateVisionResult = ShouldSimulateVisionResult(context);
            options.ApplyInputStageVisionPolicy(context != null ? context.Machine : null);
            return options;
        }

        private bool ShouldSimulateVisionResult(MachineSequenceContext context)
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings != null &&
                    (settings.SimulationMode || settings.BypassHardware || !settings.UseAjin))
                    return true;

                CDT320_Machine machine = context != null ? context.Machine : null;
                if (machine == null)
                    return false;

                if (_side == PickerSequenceSide.Front)
                {
                    return machine.PickerFrontUnit != null &&
                           ((machine.PickerFrontUnit.Setup != null && machine.PickerFrontUnit.Setup.IsSimulationMode) ||
                            (machine.PickerFrontUnit.Config != null && machine.PickerFrontUnit.Config.IsSimulationMode));
                }

                return machine.PickerRearUnit != null &&
                       ((machine.PickerRearUnit.Setup != null && machine.PickerRearUnit.Setup.IsSimulationMode) ||
                        (machine.PickerRearUnit.Config != null && machine.PickerRearUnit.Config.IsSimulationMode));
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private bool IsSimulationOrDryRun(MachineSequenceContext context)
        {
            try
            {
                if (AppSettingsStore.Current != null &&
                    (AppSettingsStore.Current.SimulationMode || AppSettingsStore.Current.DryRunMode))
                    return true;

                if (context != null && context.Controller != null && context.Controller.GlobalDryRun)
                    return true;

                CDT320_Machine machine = context != null ? context.Machine : null;
                return IsSimulationOrDryRun(machine);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsSimulationOrDryRun(CDT320_Machine machine)
        {
            try
            {
                if (AppSettingsStore.Current != null &&
                    (AppSettingsStore.Current.SimulationMode || AppSettingsStore.Current.DryRunMode))
                    return true;

                Form1 host = _getHost != null ? _getHost() : null;
                if (host != null && host.Controller != null && host.Controller.GlobalDryRun)
                    return true;

                if (machine == null)
                    return false;

                if (machine.InputStageUnit != null && machine.InputStageUnit.IsInputStageSimulationOrDryRun())
                    return true;

                if (_side == PickerSequenceSide.Front &&
                    machine.PickerFrontUnit != null &&
                    machine.PickerFrontUnit.Config != null &&
                    machine.PickerFrontUnit.Config.bDryRun)
                    return true;

                if (_side == PickerSequenceSide.Rear &&
                    machine.PickerRearUnit != null &&
                    machine.PickerRearUnit.Config != null &&
                    machine.PickerRearUnit.Config.bDryRun)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> RunManualPickerSequenceAsync(
            MachineSequenceContext context,
            PickerManualSequenceKind kind,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            if (options != null && options.RunMode == SequenceRunMode.Step)
                return await RunManualPickerStepSequenceAsync(context, kind, options, ct).ConfigureAwait(false);

            ResetStepSequence();

            switch (kind)
            {
                // 수동 픽업 시퀀스 실행
                case PickerManualSequenceKind.PickUp:
                    return await new PickerPickUpSequence(context, _side)
                        .RunAsync(ct, options)
                        .ConfigureAwait(false);

                // 수동 검사 시퀀스 실행
                case PickerManualSequenceKind.Inspect:
                    return await RunInspectionSequenceAsync(context, options, ct).ConfigureAwait(false);

                // 수동 Bottom 검사 시퀀스 실행
                case PickerManualSequenceKind.Bottom:
                    return await RunBottomInspectionSequenceAsync(context, options, ct).ConfigureAwait(false);

                // 수동 Side 검사 시퀀스 실행
                case PickerManualSequenceKind.Side:
                    return await RunSideInspectionSequenceAsync(context, options, ct).ConfigureAwait(false);

                // 수동 플레이스 시퀀스 실행
                case PickerManualSequenceKind.Place:
                    return await RunPlaceOrProcessFromLoadedPickerAsync(context, options, ct).ConfigureAwait(false);

                // 수동 복구 시퀀스 실행
                case PickerManualSequenceKind.Recover:
                    return await RunRecoverSequenceAsync(context, options, ct).ConfigureAwait(false);

                default:
                    return await new PickerProcessSequence(context, _side)
                        .RunAsync(ct, options)
                        .ConfigureAwait(false);
            }
        }

        private async Task<int> RunManualPickerStepSequenceAsync(
            MachineSequenceContext context,
            PickerManualSequenceKind kind,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            switch (kind)
            {
                // 픽업 스텝 시퀀스 이어서 실행
                case PickerManualSequenceKind.PickUp:
                    if (_pickUpStepSequence == null || _pickUpStepSequence.IsComplete)
                        _pickUpStepSequence = new PickerPickUpSequence(context, _side);
                    return await RunPickUpStepAsync(options, ct).ConfigureAwait(false);

                // 검사 스텝 시퀀스 이어서 실행
                case PickerManualSequenceKind.Inspect:
                    return await RunInspectionStepAsync(context, options, ct).ConfigureAwait(false);

                // Bottom 검사 스텝 시퀀스 이어서 실행
                case PickerManualSequenceKind.Bottom:
                    if (_bottomInspectStepSequence == null || _bottomInspectStepSequence.IsComplete)
                        _bottomInspectStepSequence = new PickerBottomInspectionSequence(context, _side);
                    return await RunBottomInspectionStepAsync(options, ct).ConfigureAwait(false);

                // Side 검사 스텝 시퀀스 이어서 실행
                case PickerManualSequenceKind.Side:
                    if (_sideInspectStepSequence == null || _sideInspectStepSequence.IsComplete)
                        _sideInspectStepSequence = new PickerSideInspectionSequence(context, _side);
                    return await RunSideInspectionStepAsync(options, ct).ConfigureAwait(false);

                // 플레이스 스텝 시퀀스 이어서 실행
                case PickerManualSequenceKind.Place:
                    if (_placeStepSequence == null || _placeStepSequence.IsComplete)
                        _placeStepSequence = new PickerPlaceSequence(context, _side);
                    return await RunPlaceStepAsync(options, ct).ConfigureAwait(false);

                default:
                    if (_stepSequence == null || _stepSequence.IsComplete)
                        _stepSequence = new PickerProcessSequence(context, _side);
                    return await RunProcessStepAsync(options, ct).ConfigureAwait(false);
            }
        }

        private async Task<int> RunProcessStepAsync(PickerSequenceOptions options, CancellationToken ct)
        {
            int result = await _stepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (_stepSequence.IsComplete)
                _stepSequence = null;
            return result;
        }

        private async Task<int> RunPickUpStepAsync(PickerSequenceOptions options, CancellationToken ct)
        {
            int result = await _pickUpStepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (_pickUpStepSequence.IsComplete)
                _pickUpStepSequence = null;
            return result;
        }

        private async Task<int> RunInspectionStepAsync(
            MachineSequenceContext context,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            if (_bottomInspectStepSequence == null && _sideInspectStepSequence == null)
                _bottomInspectStepSequence = new PickerBottomInspectionSequence(context, _side);

            if (_bottomInspectStepSequence != null)
            {
                int bottomResult = await _bottomInspectStepSequence.RunAsync(ct, options).ConfigureAwait(false);
                if (bottomResult != 0)
                    return bottomResult;

                if (!_bottomInspectStepSequence.IsComplete)
                    return 0;

                _bottomInspectStepSequence = null;
                _sideInspectStepSequence = new PickerSideInspectionSequence(context, _side);
            }

            int sideResult = await _sideInspectStepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (_sideInspectStepSequence.IsComplete)
                _sideInspectStepSequence = null;
            return sideResult;
        }

        private async Task<int> RunBottomInspectionStepAsync(PickerSequenceOptions options, CancellationToken ct)
        {
            int result = await _bottomInspectStepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (_bottomInspectStepSequence.IsComplete)
                _bottomInspectStepSequence = null;
            return result;
        }

        private async Task<int> RunSideInspectionStepAsync(PickerSequenceOptions options, CancellationToken ct)
        {
            int result = await _sideInspectStepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (_sideInspectStepSequence.IsComplete)
                _sideInspectStepSequence = null;
            return result;
        }

        private async Task<int> RunPlaceStepAsync(PickerSequenceOptions options, CancellationToken ct)
        {
            int result = await _placeStepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (_placeStepSequence.IsComplete)
                _placeStepSequence = null;
            return result;
        }

        private async Task<int> RunPlaceOrProcessFromLoadedPickerAsync(
            MachineSequenceContext context,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            try
            {
                if (HasLoadedPickerDie())
                {
                    return await new PickerProcessSequence(context, _side)
                        .RunAsync(ct, options)
                        .ConfigureAwait(false);
                }

                return await new PickerPlaceSequence(context, _side)
                    .RunAsync(ct, options)
                    .ConfigureAwait(false);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private bool HasLoadedPickerDie()
        {
            try
            {
                MaterialLocationKind location = _side == PickerSequenceSide.Front
                    ? MaterialLocationKind.PickerFront
                    : MaterialLocationKind.PickerRear;

                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    if (MaterialStateService.GetDieAtPicker(location, pickerNo) != null)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> RunInspectionSequenceAsync(
            MachineSequenceContext context,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            int bottomResult = await new PickerBottomInspectionSequence(context, _side)
                .RunAsync(ct, options)
                .ConfigureAwait(false);
            if (bottomResult != 0)
                return bottomResult;

            return await new PickerSideInspectionSequence(context, _side)
                .RunAsync(ct, options)
                .ConfigureAwait(false);
        }

        private async Task<int> RunBottomInspectionSequenceAsync(
            MachineSequenceContext context,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            return await new PickerBottomInspectionSequence(context, _side)
                .RunAsync(ct, options)
                .ConfigureAwait(false);
        }

        private async Task<int> RunSideInspectionSequenceAsync(
            MachineSequenceContext context,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            return await new PickerSideInspectionSequence(context, _side)
                .RunAsync(ct, options)
                .ConfigureAwait(false);
        }

        private async Task<int> RunRecoverSequenceAsync(
            MachineSequenceContext context,
            PickerSequenceOptions options,
            CancellationToken ct)
        {
            int placeResult = await new PickerPlaceSequence(context, _side)
                .RunAsync(ct, BuildRecoverOptions(options))
                .ConfigureAwait(false);
            if (placeResult != 0)
                return placeResult;

            return await MoveGroupToPositionAsync(
                context.Machine,
                "AvoidPosition",
                ct).ConfigureAwait(false);
        }

        private PickerSequenceOptions BuildRecoverOptions(PickerSequenceOptions source)
        {
            PickerSequenceOptions options = PickerSequenceOptions.Default();
            options.RunMode = source != null ? source.RunMode : SequenceRunMode.Auto;
            options.StartMode = source != null ? source.StartMode : SequenceStartMode.Resume;
            options.FineMove = source != null && source.FineMove;
            options.MoveTimeoutMs = source != null ? source.MoveTimeoutMs : 30000;
            options.ResourceTimeoutMs = source != null ? source.ResourceTimeoutMs : 30000;
            options.PickerNo = source != null ? source.PickerNo : 0;
            options.VisionRetryCount = source != null ? source.VisionRetryCount : 3;
            options.InputDieVisionFailureAction = source != null
                ? source.InputDieVisionFailureAction
                : InputDieVisionFailureAction.SkipDie;
            options.SimulateVisionResult = source != null && source.SimulateVisionResult;
            return options;
        }

        private async Task<int> MoveGroupToPositionAsync(CDT320_Machine machine, string positionName, CancellationToken ct)
        {
            Dictionary<PickerAxis, double> targets = new Dictionary<PickerAxis, double>();
            PickerAxis[] axes = new PickerAxis[]
            {
                PickerAxis.PickerZ0,
                PickerAxis.PickerZ1,
                PickerAxis.PickerZ2,
                PickerAxis.PickerZ3,
                PickerAxis.PickerX,
                PickerAxis.PickerY,
                PickerAxis.PickerT0,
                PickerAxis.PickerT1,
                PickerAxis.PickerT2,
                PickerAxis.PickerT3
            };

            foreach (PickerAxis axis in axes)
                targets[axis] = GetTeachingPosition(machine, axis, positionName);

            ct.ThrowIfCancellationRequested();
            // 기존 조건: Fine(미세 조그 속도) — 현재 기준: 작업 정보 수동 위치 이동은 Coarse(일반 조그 속도)로 구동한다.
            int result = _side == PickerSequenceSide.Front
                ? await machine.PickerFrontUnit.MovePickerAxes(targets, JogSpeedType.Coarse, 0.0, positionName).ConfigureAwait(false)
                : await machine.PickerRearUnit.MovePickerAxes(targets, JogSpeedType.Coarse, 0.0, positionName).ConfigureAwait(false);
            if (result != 0)
                return result;

            int timeout = 5000;
            bool done = _side == PickerSequenceSide.Front
                ? await machine.PickerFrontUnit.WaitPickerAxesMoveDone(targets.Keys, timeout).ConfigureAwait(false)
                : await machine.PickerRearUnit.WaitPickerAxesMoveDone(targets.Keys, timeout).ConfigureAwait(false);
            if (!done)
                return -11;

            foreach (KeyValuePair<PickerAxis, double> pair in targets)
            {
                bool inPosition = _side == PickerSequenceSide.Front
                    ? machine.PickerFrontUnit.IsPickerAxisInPosition(pair.Key, pair.Value, 10.0)
                    : machine.PickerRearUnit.IsPickerAxisInPosition(pair.Key, pair.Value, 10.0);
                if (!inPosition)
                    return -12;
            }

            return 0;
        }

        private MachineSequenceContext CreateContext(Form1 host)
        {
            return new MachineSequenceContext(host.Controller, new SequenceSignalBus());
        }

        private async Task StopManualActionAsync()
        {
            Form1 host = _getHost();
            if (host == null || host.Controller == null)
                return;

            try
            {
                host.Controller.CancelManualOperation();
                ResetStepSequence();
                CDT320_Machine machine = host.Machine;
                if (machine != null)
                {
                    if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null)
                        machine.PickerFrontUnit.StopPickerMotionAndOutputs("Manual stop");
                    if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null)
                        machine.PickerRearUnit.StopPickerMotionAndOutputs("Manual stop");
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("Manual stop failed: " + ex.Message);
            }
            finally
            {
                await Task.Delay(50).ConfigureAwait(true);
                _manualSequenceRunning = false;
                SetButtonsEnabledSafe(true);
                RefreshSafe();
            }
        }

        private void ResetStepSequence()
        {
            try
            {
                if (_stepSequence != null)
                    _stepSequence.Abort();
                if (_pickUpStepSequence != null)
                    _pickUpStepSequence.Abort();
                if (_bottomInspectStepSequence != null)
                    _bottomInspectStepSequence.Abort();
                if (_sideInspectStepSequence != null)
                    _sideInspectStepSequence.Abort();
                if (_placeStepSequence != null)
                    _placeStepSequence.Abort();
            }
            catch (Exception ex)
            {
                WriteAlarm("Step sequence reset failed: " + ex.Message);
            }
            finally
            {
                _stepSequence = null;
                _pickUpStepSequence = null;
                _bottomInspectStepSequence = null;
                _sideInspectStepSequence = null;
                _placeStepSequence = null;
            }
        }

        private static async Task WaitForCancellationAsync(CancellationToken ct)
        {
            if (!ct.CanBeCanceled)
            {
                await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
                return;
            }

            if (ct.IsCancellationRequested)
                return;

            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ct.Register(() => tcs.TrySetResult(0)))
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }

        private void ObserveManualActionTask(Task<bool> task, string actionName)
        {
            if (task == null)
                return;

            _ = ObserveManualActionTaskAsync(task, actionName);
        }

        private async Task ObserveManualActionTaskAsync(Task<bool> task, string actionName)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteEvent(actionName + " 정지 후 취소 완료.");
            }
            catch (Exception ex)
            {
                WriteAlarm(actionName + " 정지 후 종료 처리 중 오류: " + ex.Message);
            }
            finally
            {
            }
        }

        private void TrySaveMaterialStateAfterManualSequence(string actionName)
        {
            try
            {
                bool saved = MaterialStateService.TryNotifyAndSave("PickerManualSequenceFinally:" + actionName);
                if (!saved)
                    WriteAlarm(actionName + " 종료 시 Material 상태 저장 실패. 저장 파일과 Alarm/Event Log를 확인하세요.");
            }
            catch (Exception ex)
            {
                WriteAlarm(actionName + " 종료 시 Material 상태 저장 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ShowFailure(string actionName)
        {
            string message = SequenceFailureStore.BuildManualFailureMessage(
                actionName,
                actionName + " 실패\r\nAlarm/Event Log를 확인하세요.");
            QMC.Common.MessageDialog.Show(_owner, message, SideName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private bool SelectManualSequenceStartMode(string actionName, out SequenceStartMode startMode)
        {
            startMode = SequenceStartMode.Resume;

            string message =
                actionName + " 시작 방식을 선택하세요.\r\n\r\n" +
                "[예] 처음부터 시작\r\n" +
                "[아니오] 현재 스텝에서 진행";

            DialogResult result = QMC.Common.MessageDialog.Show(
                _owner,
                message,
                SideName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                startMode = SequenceStartMode.Restart;
                return true;
            }

            if (result == DialogResult.No)
            {
                startMode = SequenceStartMode.Resume;
                return true;
            }

            return false;
        }

        private string FormatStartMode(SequenceStartMode startMode)
        {
            return startMode == SequenceStartMode.Restart ? "처음부터" : "현재스텝";
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (_btnCountClear != null)
                _btnCountClear.Enabled = enabled;

            foreach (Control control in _actionControls)
            {
                if (ReferenceEquals(control, _btnStop))
                    control.Enabled = !enabled || _manualSequenceRunning;
                else
                    control.Enabled = enabled;
            }

            _btnStop.Enabled = !enabled || _manualSequenceRunning;
        }

        private void SetButtonsEnabledSafe(bool enabled)
        {
            try
            {
                if (_owner == null || _owner.IsDisposed)
                    return;

                if (_owner.InvokeRequired)
                {
                    _owner.Invoke(new Action(() => SetButtonsEnabled(enabled)));
                    return;
                }

                SetButtonsEnabled(enabled);
            }
            catch (Exception ex)
            {
                WriteAlarm("메뉴얼 시퀀스 버튼 상태 복구 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void RefreshSafe()
        {
            try
            {
                if (_owner == null || _owner.IsDisposed)
                    return;

                if (_owner.InvokeRequired)
                {
                    _owner.BeginInvoke(new Action(Refresh));
                    return;
                }

                Refresh();
            }
            catch (Exception ex)
            {
                WriteAlarm("메뉴얼 시퀀스 화면 갱신 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private string ResolveHeadState(CDT320_Machine machine, int pickerNo, bool flow)
        {
            return ResolveHeadState(machine, pickerNo, flow, null);
        }

        private string ResolveHeadState(CDT320_Machine machine, int pickerNo, bool flow, DieMaterial pickedDie)
        {
            if (!UsePicker(machine, pickerNo))
                return "DISABLE";

            DieMaterial die = pickedDie ?? GetPickedDieMaterial(pickerNo);
            if (die != null)
                return "PICK / " + FormatDieShortId(die.DieId);

            return flow ? "PICK(SENSOR)" : "EMPTY";
        }

        private static bool IsPickedHeadState(string headState)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(headState) &&
                       headState.StartsWith("PICK", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private DieMaterial GetPickedDieMaterial(int pickerNo)
        {
            try
            {
                MaterialLocationKind location = _side == PickerSequenceSide.Front
                    ? MaterialLocationKind.PickerFront
                    : MaterialLocationKind.PickerRear;

                return MaterialStateService.GetDieAtPicker(location, pickerNo);
            }
            catch (Exception ex)
            {
                WriteAlarm("픽커 Material 상태 조회 실패: pickerNo=" + pickerNo + ", error=" + ex.Message);
                return null;
            }
            finally
            {
            }
        }

        private static string FormatDieShortId(string dieId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dieId))
                    return "-";

                string value = dieId.Trim();
                int dieMarker = value.LastIndexOf("-D", StringComparison.OrdinalIgnoreCase);
                if (dieMarker >= 0 && dieMarker + 1 < value.Length)
                    return value.Substring(dieMarker + 1);

                return value.Length > 12 ? value.Substring(value.Length - 12) : value;
            }
            catch
            {
                return dieId ?? "-";
            }
            finally
            {
            }
        }

        private string ResolveHeadZone(CDT320_Machine machine)
        {
            try
            {
                if (machine == null)
                    return "-";

                string physicalZone = ResolveEncoderHeadZone(machine);
                return string.IsNullOrWhiteSpace(physicalZone) ? "UNKNOWN" : physicalZone;
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker Head Zone 판정 실패: " + ex.Message);
                return "UNKNOWN";
            }
            finally
            {
            }
        }

        private string ResolveEncoderHeadZone(CDT320_Machine machine)
        {
            try
            {
                if (machine == null)
                    return "UNKNOWN";

                string physicalZone = PickerZoneInterlockRules.ResolvePickerPhysicalZoneName(
                    machine,
                    _side == PickerSequenceSide.Front);

                return string.IsNullOrWhiteSpace(physicalZone) ? "UNKNOWN" : physicalZone;
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker Head Zone 엔코더 보조 판정 실패: " + ex.Message);
                return "UNKNOWN";
            }
            finally
            {
            }
        }

        private string ResolveActivePickerWorkZone()
        {
            try
            {
                PickerWorkZone zone;
                string owner;
                bool active = PickerZoneInterlockRules.TryGetPickerWorkArea(
                    _side == PickerSequenceSide.Front,
                    out zone,
                    out owner);

                if (!active)
                    return string.Empty;

                switch (zone)
                {
                    case PickerWorkZone.Input:
                        return "PICK";
                    case PickerWorkZone.Bottom:
                        return "INSPECT_B";
                    case PickerWorkZone.Side:
                        return "INSPECT_S";
                    case PickerWorkZone.Output:
                        return "PLACE";
                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker Head Zone 작업영역 판정 실패: " + ex.Message);
                return string.Empty;
            }
            finally
            {
            }
        }

        private bool IsPickerXInZone(CDT320_Machine machine, string positionName)
        {
            try
            {
                BaseAxis pickerX = GetAxis(machine, PickerAxis.PickerX);
                if (pickerX == null)
                    return false;

                double target = GetTeachingPosition(machine, PickerAxis.PickerX, positionName);
                double tolerance = pickerX.Config != null ? pickerX.Config.InPositionTolerance : 0.05;
                tolerance = Math.Max(tolerance, 0.05);

                return Math.Abs(pickerX.ActualPosition - target) <= tolerance;
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker Head Zone X축 위치 판정 실패: position=" + positionName + ", error=" + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private bool IsAnyPickerInDieBottomZone(CDT320_Machine machine)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.IsPickerInDieBottomZone(pickerNo))
                    return true;
                if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.IsPickerInDieBottomZone(pickerNo))
                    return true;
            }

            return false;
        }

        private bool IsAnyPickerInDieSideZone(CDT320_Machine machine)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.IsPickerInDieSideZone(pickerNo))
                    return true;
                if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.IsPickerInDieSideZone(pickerNo))
                    return true;
            }

            return false;
        }

        private bool IsAnyPickerInDiePickPosition(CDT320_Machine machine)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.IsPickerInDiePickPosition(pickerNo))
                    return true;
                if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.IsPickerInDiePickPosition(pickerNo))
                    return true;
            }

            return false;
        }

        private bool IsAnyPickerInDiePlacePosition(CDT320_Machine machine)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.IsPickerInDiePlacePosition(pickerNo))
                    return true;
                if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.IsPickerInDiePlacePosition(pickerNo))
                    return true;
            }

            return false;
        }

        private void SetHeadZone(string zone)
        {
            if (_lblHeadZoneValue == null)
                return;

            _lblHeadZoneValue.Text = string.IsNullOrEmpty(zone) ? "-" : zone;
            _lblHeadZoneValue.ForeColor = Color.White;

            switch (_lblHeadZoneValue.Text)
            {
                // 인풋 영역 표시
                case "INPUT":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(0, 128, 192);
                    break;
                // 작업 영역 표시
                case "PICK":
                case "INSPECT_B":
                case "INSPECT_S":
                case "PLACE":
                case "OUTPUT":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(217, 119, 6);
                    break;
                // Avoid 영역 표시
                case "AVOID":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(0, 176, 80);
                    break;
                // 이동 중 상태 표시
                case "MOVING":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(255, 192, 0);
                    _lblHeadZoneValue.ForeColor = Color.Black;
                    break;
                // 위치 미확인 상태 표시
                case "UNKNOWN":
                    _lblHeadZoneValue.BackColor = Color.FromArgb(160, 160, 160);
                    break;
                default:
                    _lblHeadZoneValue.BackColor = Color.White;
                    _lblHeadZoneValue.ForeColor = Color.Black;
                    break;
            }

            // 상태 판정과 색상은 원래 토큰을 사용하고 화면 문구만 번역한다.
            _lblHeadZoneValue.Text = Lang.Display(_lblHeadZoneValue.Text);
        }

        private string ResolveHeadProcess(CDT320_Machine machine, string encoderZone)
        {
            try
            {
                string activeWorkZone = ResolveActivePickerWorkZone();
                if (!string.IsNullOrWhiteSpace(activeWorkZone))
                    return ResolveHeadProcessFromZone(activeWorkZone);

                if (IsPickerMoving(machine))
                    return "MOVING";

                return ResolveHeadProcessFromZone(encoderZone);
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker 공정 상태 판정 실패: " + ex.Message);
                return "UNKNOWN";
            }
            finally
            {
            }
        }

        private string ResolveHeadProcessFromZone(string zone)
        {
            switch (zone)
            {
                case "AVOID":
                    return "AVOID";
                case "INPUT":
                case "PICK":
                    return "PICKUP";
                case "INSPECT_B":
                    return "INSPECT_B";
                case "INSPECT_S":
                    return "INSPECT_S";
                case "PLACE":
                case "OUTPUT":
                    return "PLACE";
                case "MOVING":
                    return "MOVING";
                case "UNKNOWN":
                    return "UNKNOWN";
                default:
                    return "-";
            }
        }

        private bool IsPickerMoving(CDT320_Machine machine)
        {
            try
            {
                PickerAxis[] axes = new PickerAxis[]
                {
                    PickerAxis.PickerX,
                    PickerAxis.PickerY,
                    PickerAxis.PickerT0,
                    PickerAxis.PickerT1,
                    PickerAxis.PickerT2,
                    PickerAxis.PickerT3,
                    PickerAxis.PickerZ0,
                    PickerAxis.PickerZ1,
                    PickerAxis.PickerZ2,
                    PickerAxis.PickerZ3
                };

                foreach (PickerAxis axis in axes)
                {
                    BaseAxis item = GetAxis(machine, axis);
                    if (item != null && item.IsMoving)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                WriteAlarm("Picker 공정 이동 상태 판정 실패: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private string ResolveProcessDetail(CDT320_Machine machine, string process, bool moving)
        {
            string flowProcess = NormalizeFlowProcess(process);
            string encoderZone = ResolveEncoderHeadZone(machine);
            string encoderText = string.IsNullOrWhiteSpace(encoderZone)
                ? string.Empty
                : " / " + Lang.Display("ENC") + "=" + Lang.Display(encoderZone);

            if (string.IsNullOrWhiteSpace(flowProcess) || flowProcess == "-")
                return string.IsNullOrWhiteSpace(encoderText) ? "-" : encoderText.TrimStart(' ', '/');

            if (flowProcess == "UNKNOWN")
                return "현재 위치 확인 필요" + encoderText;

            if (moving)
            {
                if (IsStableProcess(flowProcess) &&
                    IsStableProcess(_lastStableProcess) &&
                    !string.Equals(_lastStableProcess, flowProcess, StringComparison.OrdinalIgnoreCase))
                    return Lang.Display(_lastStableProcess) + " -> " + Lang.Display(flowProcess) + " 이동 중" + encoderText;

                return Lang.Display(flowProcess) + " 위치 이동 중" + encoderText;
            }

            switch (flowProcess)
            {
                case "AVOID":
                    return Lang.Display("AVOID") + " 대기" + encoderText;
                case "PICKUP":
                    return Lang.Display("PICKUP") + " 공정 진행 중" + encoderText;
                case "BOTTOM":
                    return Lang.Display("BOTTOM") + " 검사 진행 중" + encoderText;
                case "SIDE":
                    return Lang.Display("SIDE") + " 검사 진행 중" + encoderText;
                case "PLACE":
                    return Lang.Display("PLACE") + " 공정 진행 중" + encoderText;
                default:
                    return Lang.Display(flowProcess) + encoderText;
            }
        }

        private void SetProcessDetail(string detail)
        {
            if (_lblProcessDetailValue == null)
                return;

            _lblProcessDetailValue.Text = string.IsNullOrWhiteSpace(detail) ? "-" : detail;
            _lblProcessDetailValue.ForeColor = Color.Black;
            _lblProcessDetailValue.BackColor = detail != null && detail.Contains("이동 중")
                ? Color.FromArgb(255, 242, 204)
                : Color.FromArgb(240, 240, 240);
        }

        private string NormalizeFlowProcess(string process)
        {
            switch (process)
            {
                case "INPUT":
                case "PICK":
                case "PICKUP":
                    return "PICKUP";
                case "INSPECT":
                case "INSPECT_B":
                case "BOTTOM":
                    return "BOTTOM";
                case "INSPECT_S":
                case "SIDE":
                    return "SIDE";
                case "OUTPUT":
                case "PLACE":
                    return "PLACE";
                case "AVOID":
                case "UNKNOWN":
                case "MOVING":
                case "-":
                    return process;
                default:
                    return string.IsNullOrWhiteSpace(process) ? "-" : process;
            }
        }

        private bool IsStableProcess(string process)
        {
            string value = NormalizeFlowProcess(process);
            return value == "AVOID" ||
                   value == "PICKUP" ||
                   value == "BOTTOM" ||
                   value == "SIDE" ||
                   value == "PLACE";
        }

        private bool IsGroupInPosition(CDT320_Machine machine, string positionName)
        {
            PickerAxis[] axes = new PickerAxis[] { PickerAxis.PickerX, PickerAxis.PickerY, PickerAxis.PickerT0, PickerAxis.PickerT1, PickerAxis.PickerT2, PickerAxis.PickerT3, PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            foreach (PickerAxis axis in axes)
            {
                double target = GetTeachingPosition(machine, axis, positionName);
                BaseAxis item = GetAxis(machine, axis);
                if (item == null || Math.Abs(item.ActualPosition - target) > 10.0)
                    return false;
            }
            return true;
        }

        private BaseAxis GetAxis(CDT320_Machine machine, PickerAxis axis)
        {
            if (machine == null)
                return null;
            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.Axes.ContainsKey(axis))
                return machine.PickerFrontUnit.Axes[axis];
            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.Axes.ContainsKey(axis))
                return machine.PickerRearUnit.Axes[axis];
            return null;
        }

        private double GetTeachingPosition(CDT320_Machine machine, PickerAxis axis, string positionName)
        {
            if (_side == PickerSequenceSide.Front)
                return machine.PickerFrontUnit.GetPickerTeachingPosition(axis, positionName);
            return machine.PickerRearUnit.GetPickerTeachingPosition(axis, positionName);
        }

        private bool UsePicker(CDT320_Machine machine, int pickerNo)
        {
            int index = Math.Max(0, pickerNo - 1);
            if (_side == PickerSequenceSide.Front)
                return machine.PickerFrontUnit != null && machine.PickerFrontUnit.Config != null && machine.PickerFrontUnit.Config.UsePicker != null && index < machine.PickerFrontUnit.Config.UsePicker.Length && machine.PickerFrontUnit.Config.UsePicker[index];
            return machine.PickerRearUnit != null && machine.PickerRearUnit.Config != null && machine.PickerRearUnit.Config.UsePicker != null && index < machine.PickerRearUnit.Config.UsePicker.Length && machine.PickerRearUnit.Config.UsePicker[index];
        }

        private int GetColletUseCount(CDT320_Machine machine, int pickerNo)
        {
            int index = Math.Max(0, pickerNo - 1);
            if (_side == PickerSequenceSide.Front &&
                machine.PickerFrontUnit != null &&
                machine.PickerFrontUnit.ColletUseCounts != null &&
                index < machine.PickerFrontUnit.ColletUseCounts.Length)
            {
                return machine.PickerFrontUnit.ColletUseCounts[index];
            }

            if (_side == PickerSequenceSide.Rear &&
                machine.PickerRearUnit != null &&
                machine.PickerRearUnit.ColletUseCounts != null &&
                index < machine.PickerRearUnit.ColletUseCounts.Length)
            {
                return machine.PickerRearUnit.ColletUseCounts[index];
            }

            return 0;
        }

        private int GetPickFailCount(CDT320_Machine machine)
        {
            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null)
                return machine.PickerFrontUnit.PickFailCount;
            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null)
                return machine.PickerRearUnit.PickFailCount;
            return 0;
        }

        private int GetPlaceFailCount(CDT320_Machine machine)
        {
            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null)
                return machine.PickerFrontUnit.PlaceFailCount;
            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null)
                return machine.PickerRearUnit.PlaceFailCount;
            return 0;
        }

        private BaseDigitalInput GetFlow(CDT320_Machine machine, int pickerNo)
        {
            int index = Math.Max(0, pickerNo - 1);
            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.FlowChecks != null && index < machine.PickerFrontUnit.FlowChecks.Length)
                return machine.PickerFrontUnit.FlowChecks[index];
            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.FlowChecks != null && index < machine.PickerRearUnit.FlowChecks.Length)
                return machine.PickerRearUnit.FlowChecks[index];
            return null;
        }

        private BaseDigitalOutput GetVacuum(CDT320_Machine machine, int pickerNo)
        {
            int index = Math.Max(0, pickerNo - 1);
            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.Vacuums != null && index < machine.PickerFrontUnit.Vacuums.Length)
                return machine.PickerFrontUnit.Vacuums[index];
            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.Vacuums != null && index < machine.PickerRearUnit.Vacuums.Length)
                return machine.PickerRearUnit.Vacuums[index];
            return null;
        }

        private BaseDigitalOutput GetBlow(CDT320_Machine machine, int pickerNo)
        {
            int index = Math.Max(0, pickerNo - 1);
            if (_side == PickerSequenceSide.Front && machine.PickerFrontUnit != null && machine.PickerFrontUnit.Blows != null && index < machine.PickerFrontUnit.Blows.Length)
                return machine.PickerFrontUnit.Blows[index];
            if (_side == PickerSequenceSide.Rear && machine.PickerRearUnit != null && machine.PickerRearUnit.Blows != null && index < machine.PickerRearUnit.Blows.Length)
                return machine.PickerRearUnit.Blows[index];
            return null;
        }

        private BaseDigitalInput GetCdaPressure(CDT320_Machine machine)
        {
            if (_side == PickerSequenceSide.Front)
                return machine.PickerFrontUnit != null ? machine.PickerFrontUnit.CdaTankPressureCheck : null;
            return machine.PickerRearUnit != null ? machine.PickerRearUnit.CdaTankPressureCheck : null;
        }

        private BaseDigitalInput GetVacuumPressure(CDT320_Machine machine)
        {
            if (_side == PickerSequenceSide.Front)
                return machine.PickerFrontUnit != null ? machine.PickerFrontUnit.VacuumTankPressureCheck : null;
            return machine.PickerRearUnit != null ? machine.PickerRearUnit.VacuumTankPressureCheck : null;
        }

        private static bool ReadInput(BaseDigitalInput input)
        {
            return input != null && input.IsOn;
        }

        private static bool ReadOutput(BaseDigitalOutput output)
        {
            return output != null && output.IsOn;
        }

        private static string FormatAxis(BaseAxis axis, string format, string unit)
        {
            if (axis == null)
                return "-";
            return axis.ActualPosition.ToString(format) + " " + unit;
        }

        private static void SetDot(IndicatorDot dot, bool on)
        {
            if (dot == null)
                return;
            dot.IsOn = on;
        }

        private void WriteEvent(string message)
        {
            EventLogger.Write(EventKind.Event, "QMC", LogCode, message);
        }

        private void WriteAlarm(string message)
        {
            EventLogger.Write(EventKind.Alarm, "QMC", LogCode, message);
        }
    }
}
