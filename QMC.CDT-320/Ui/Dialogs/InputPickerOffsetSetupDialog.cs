using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Sequencing;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed class InputPickerOffsetSetupDialog : Form
    {
        private readonly CDT320_Machine _machine;
        private readonly PickerSequenceSide _side;
        private readonly int _pickerNo;
        private readonly int _pickerIndex;
        private readonly double _dieX;
        private readonly double _dieY;

        private NumericUpDown _offsetX;
        private NumericUpDown _offsetY;
        private Label _rawResolvedValue;
        private Label _effectiveValue;
        private Label _offsetStackValue;
        private Label _targetValue;
        private Label _needleTargetValue;
        private Label _currentValue;
        private TextBox _formulaValue;
        private Label _fallbackValue;
        private Label _statusValue;
        private Button _saveButton;
        private Button _calculateButton;

        public double SavedOffsetX { get; private set; }
        public double SavedOffsetY { get; private set; }

        public InputPickerOffsetSetupDialog(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerNo,
            double dieX,
            double dieY)
        {
            if (pickerNo < 1 || pickerNo > 4)
                throw new ArgumentOutOfRangeException("pickerNo");

            _machine = machine ?? throw new ArgumentNullException("machine");
            _side = side;
            _pickerNo = pickerNo;
            _pickerIndex = pickerNo - 1;
            _dieX = dieX;
            _dieY = dieY;

            InitializeComponent();
            LoadFromSetup();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = ResolvePickerTitle() + " INPUT OFFSET";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(900, 640);
            Font = SystemFonts.MessageBoxFont;

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(12)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            Label header = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Text = ResolvePickerTitle() + " 기준 InputVision -> Picker Offset 설정"
            };
            root.Controls.Add(header, 0, 0);

            TableLayoutPanel summary = CreateValueTable(4);
            summary.Margin = new Padding(0, 8, 0, 8);
            AddStaticValueRow(summary, "선택 Die", "X=" + F(_dieX) + " mm, Y=" + F(_dieY) + " mm");
            AddStaticValueRow(summary, "저장 변수", ResolveSetupPathText());
            root.Controls.Add(summary, 0, 1);

            GroupBox editorGroup = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "OFFSET"
            };
            TableLayoutPanel editor = CreateValueTable(11);
            editor.Dock = DockStyle.Fill;
            editor.Padding = new Padding(8);
            editorGroup.Controls.Add(editor);

            _offsetX = CreateNumberBox();
            _offsetY = CreateNumberBox();
            _offsetX.ValueChanged += (s, e) => RefreshPreview();
            _offsetY.ValueChanged += (s, e) => RefreshPreview();

            AddControlRow(editor, "InputVisionToPicker X", _offsetX, "mm");
            AddControlRow(editor, "InputVisionToPicker Y", _offsetY, "mm");

            _rawResolvedValue = AddDynamicValueRow(editor, "Saved Offset");
            _offsetStackValue = AddDynamicValueRow(editor, "AUTO Stack");
            _effectiveValue = AddDynamicValueRow(editor, "Formula Input");
            _targetValue = AddDynamicValueRow(editor, "최종 이동 목표");
            _needleTargetValue = AddDynamicValueRow(editor, "Needle X 이동");
            _currentValue = AddDynamicValueRow(editor, "현재 축 위치");
            _fallbackValue = AddDynamicValueRow(editor, "Fallback");
            _formulaValue = AddDynamicTextBoxRow(editor, "Formula", 92);

            root.Controls.Add(editorGroup, 0, 2);

            _statusValue = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Height = 34,
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(_statusValue, 0, 3);

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false
            };

            _saveButton = new Button { Text = "저장", Width = 90, Height = 30 };
            Button cancelButton = new Button { Text = "취소", Width = 90, Height = 30, DialogResult = DialogResult.Cancel };
            _calculateButton = new Button { Text = "현재 위치로 계산", Width = 140, Height = 30 };

            _saveButton.Click += (s, e) => SaveAndClose();
            _calculateButton.Click += (s, e) => ApplyCurrentPositionCalculation();

            buttons.Controls.Add(_saveButton);
            buttons.Controls.Add(cancelButton);
            buttons.Controls.Add(_calculateButton);
            root.Controls.Add(buttons, 0, 4);

            AcceptButton = _saveButton;
            CancelButton = cancelButton;

            ResumeLayout(false);
        }

        private void LoadFromSetup()
        {
            try
            {
                PickerVisionCoordinateOffsets offsets;
                string reason;
                if (!TryResolveOffsets(out offsets, out reason))
                {
                    SetStatus(reason);
                    SetEditorEnabled(false);
                    return;
                }

                offsets.EnsureArrays();
                double pitchX = ResolvePickerPitchX();
                double pitchY = ResolvePickerPitchY();
                double rawX = offsets.GetOffsetX(_pickerIndex, pitchX);
                double rawY = offsets.GetOffsetY(_pickerIndex, pitchY);
                SavedOffsetX = rawX;
                SavedOffsetY = rawY;
                _offsetX.Value = ClampDecimal(SavedOffsetX);
                _offsetY.Value = ClampDecimal(SavedOffsetY);
                SetEditorEnabled(true);
                RefreshPreview();
            }
            catch (Exception ex)
            {
                SetStatus("Load failed: " + ex.Message);
                SetEditorEnabled(false);
            }
            finally
            {
            }
        }

        private void SaveAndClose()
        {
            try
            {
                PickerVisionCoordinateOffsets offsets;
                string reason;
                if (!TryResolveOffsets(out offsets, out reason))
                {
                    SetStatus(reason);
                    return;
                }

                offsets.EnsureArrays();
                SavedOffsetX = (double)_offsetX.Value;
                SavedOffsetY = (double)_offsetY.Value;
                offsets.OffsetX[_pickerIndex] = SavedOffsetX;
                offsets.OffsetY[_pickerIndex] = SavedOffsetY;
                RefreshPreview();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                SetStatus("Save failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ApplyCurrentPositionCalculation()
        {
            try
            {
                PickerVisionCoordinateOffsets offsets;
                string reason;
                if (!TryResolveOffsets(out offsets, out reason))
                {
                    SetStatus(reason);
                    return;
                }

                BaseAxis pickerX = ResolvePickerXAxis();
                BaseAxis pickerY = ResolvePickerYAxis();
                InputStageUnit stage = _machine.InputStageUnit;
                if (pickerX == null || pickerY == null || stage == null || stage.StageY == null)
                {
                    SetStatus("현재 PickerX/PickerY/StageY 축 정보를 찾을 수 없습니다.");
                    return;
                }

                double currentPickerX = pickerX.ActualPosition;
                double currentPickerY = pickerY.ActualPosition;
                double currentStageY = stage.StageY.ActualPosition;
                double pickerAlignOffsetX = InputPickerPickTargetResolver.ResolvePickerAlignOffsetX(_machine, _side, _pickerIndex);
                double cameraOffsetX;
                double cameraOffsetY;
                InputPickerPickTargetResolver.TryResolveInputCameraToBottomOffsets(_machine, out cameraOffsetX, out cameraOffsetY);
                double pickerAlignOffsetY = InputPickerPickTargetResolver.ResolvePickerAlignOffsetY(_machine, _side, _pickerIndex);
                double effectiveX = currentPickerX - _dieX + cameraOffsetX - pickerAlignOffsetX;
                double effectiveY = Math.Abs(currentPickerY) + cameraOffsetY - pickerAlignOffsetY;
                double expectedStageY =
                    _dieY +
                    InputPickerPickTargetResolver.ResolveNeedleCalibrationOffsetY(_machine);

                _offsetX.Value = ClampDecimal(effectiveX);
                _offsetY.Value = ClampDecimal(effectiveY);
                RefreshPreview();
                SetStatus(
                    "현재 위치 기준 계산 완료. PickerX=" + F(currentPickerX) +
                    " mm, PickerY=" + F(currentPickerY) +
                    " mm, StageY=" + F(currentStageY) +
                    " mm, CameraOffsetX=" + F(cameraOffsetX) + " applied to X with minus sign, CameraOffsetY=" + F(cameraOffsetY) + " applied to PickerY with minus sign" +
                    ", PickerYInput=" + F(effectiveY) +
                    ", StageYTarget=" + F(expectedStageY) + " mm");
            }
            catch (Exception ex)
            {
                SetStatus("현재 위치 계산 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void RefreshPreview()
        {
            try
            {
                PickerVisionCoordinateOffsets offsets;
                string reason;
                if (!TryResolveOffsets(out offsets, out reason))
                {
                    SetStatus(reason);
                    return;
                }

                PickerVisionCoordinateOffsets previewOffsets = CreatePreviewOffsets(offsets);
                double pitchX = ResolvePickerPitchX();
                double pitchY = ResolvePickerPitchY();
                double rawResolvedX = previewOffsets.GetOffsetX(_pickerIndex, pitchX);
                double rawResolvedY = previewOffsets.GetOffsetY(_pickerIndex, pitchY);
                double effectiveX = rawResolvedX;
                double effectiveY = rawResolvedY;
                PickCoordinateResult target = InputPickerPickTargetResolver.CalculateManualInputMapTarget(
                    _machine,
                    _side,
                    _pickerIndex,
                    "",
                    _dieX,
                    _dieY,
                    effectiveX,
                    effectiveY);

                _rawResolvedValue.Text =
                    "X=" + F(rawResolvedX) + " mm, Y=" + F(rawResolvedY) +
                    " mm (Pitch X=" + F(pitchX) + ", Y=" + F(pitchY) + ")";
                _effectiveValue.Text = "inputVisionToPickerX=" + F(effectiveX) +
                    " mm, Y=" + F(effectiveY) + " mm";
                _offsetStackValue.Text = BuildAutoOffsetStackText(effectiveX, effectiveY);
                _targetValue.Text =
                    "PickerX=" + F(target.PickerX) +
                    " mm, PickerY=" + F(target.PickerY) +
                    " mm, StageY=" + F(target.StageY) +
                    " mm, NeedleX=" + F(target.NeedleX) + " mm";
                _needleTargetValue.Text = BuildNeedleXTargetText(target.NeedleX);
                _currentValue.Text =
                    "PickerX=" + F(ReadAxisActual(ResolvePickerXAxis())) +
                    " mm, PickerY=" + F(ReadAxisActual(ResolvePickerYAxis())) +
                    " mm, StageY=" + F(ReadStageYActual()) +
                    " mm, NeedleX=" + F(ReadNeedleXActual()) + " mm";
                _formulaValue.Text = target.Formula;
                _fallbackValue.Text = ResolveFallbackText(previewOffsets);
            }
            catch (Exception ex)
            {
                SetStatus("Preview failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private PickerVisionCoordinateOffsets CreatePreviewOffsets(PickerVisionCoordinateOffsets source)
        {
            source.EnsureArrays();
            PickerVisionCoordinateOffsets preview = new PickerVisionCoordinateOffsets();
            preview.OffsetX = new double[4];
            preview.OffsetY = new double[4];
            Array.Copy(source.OffsetX, preview.OffsetX, Math.Min(source.OffsetX.Length, preview.OffsetX.Length));
            Array.Copy(source.OffsetY, preview.OffsetY, Math.Min(source.OffsetY.Length, preview.OffsetY.Length));
            preview.OffsetX[_pickerIndex] = (double)_offsetX.Value;
            preview.OffsetY[_pickerIndex] = (double)_offsetY.Value;
            return preview;
        }

        private bool TryResolveOffsets(out PickerVisionCoordinateOffsets offsets, out string reason)
        {
            offsets = null;
            reason = string.Empty;

            if (_machine == null)
            {
                reason = "Machine 정보가 없습니다.";
                return false;
            }

            if (_side == PickerSequenceSide.Front)
            {
                if (_machine.PickerFrontUnit == null || _machine.PickerFrontUnit.Setup == null)
                {
                    reason = "Front Picker setup 정보가 없습니다.";
                    return false;
                }

                _machine.PickerFrontUnit.Setup.EnsureGeometryData();
                offsets = _machine.PickerFrontUnit.Setup.InputVisionToPicker;
            }
            else
            {
                if (_machine.PickerRearUnit == null || _machine.PickerRearUnit.Setup == null)
                {
                    reason = "Rear Picker setup 정보가 없습니다.";
                    return false;
                }

                _machine.PickerRearUnit.Setup.EnsureGeometryData();
                offsets = _machine.PickerRearUnit.Setup.InputVisionToPicker;
            }

            if (offsets == null)
            {
                reason = ResolvePickerTitle() + " InputVisionToPicker Offset 정보가 없습니다.";
                return false;
            }

            offsets.EnsureArrays();
            return true;
        }

        private BaseAxis ResolvePickerXAxis()
        {
            if (_machine == null)
                return null;

            if (_side == PickerSequenceSide.Front)
                return _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerX : null;

            return _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerX : null;
        }

        private BaseAxis ResolvePickerYAxis()
        {
            if (_machine == null)
                return null;

            if (_side == PickerSequenceSide.Front)
                return _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerY : null;

            return _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerY : null;
        }

        private double ResolvePickerPitchX()
        {
            if (_side == PickerSequenceSide.Front && _machine.PickerFrontUnit != null && _machine.PickerFrontUnit.Setup != null)
                return _machine.PickerFrontUnit.Setup.PickerPitchX;

            if (_side == PickerSequenceSide.Rear && _machine.PickerRearUnit != null && _machine.PickerRearUnit.Setup != null)
                return _machine.PickerRearUnit.Setup.PickerPitchX;

            return 0.0;
        }

        private double ResolvePickerPitchY()
        {
            if (_side == PickerSequenceSide.Front && _machine.PickerFrontUnit != null && _machine.PickerFrontUnit.Setup != null)
                return _machine.PickerFrontUnit.Setup.PickerPitchY;

            if (_side == PickerSequenceSide.Rear && _machine.PickerRearUnit != null && _machine.PickerRearUnit.Setup != null)
                return _machine.PickerRearUnit.Setup.PickerPitchY;

            return 0.0;
        }

        private string BuildAutoOffsetStackText(double effectiveX, double effectiveY)
        {
            double alignX = InputPickerPickTargetResolver.ResolvePickerAlignOffsetX(_machine, _side, _pickerIndex);
            double alignY = InputPickerPickTargetResolver.ResolvePickerAlignOffsetY(_machine, _side, _pickerIndex);
            double alignT = InputPickerPickTargetResolver.ResolvePickerAlignOffsetT(_machine, _side, _pickerIndex);
            double needleXOffset = InputPickerPickTargetResolver.ResolveNeedleCalibrationOffsetX(_machine);
            double needleYOffset = InputPickerPickTargetResolver.ResolveNeedleCalibrationOffsetY(_machine);
            double cameraOffsetX;
            double cameraOffsetY;
            InputPickerPickTargetResolver.TryResolveInputCameraToBottomOffsets(_machine, out cameraOffsetX, out cameraOffsetY);
            double pickerYTarget = ResolveSignedPickerYTarget(effectiveY - cameraOffsetY + alignY);

            return "X: IV->Picker=" + F(effectiveX) +
                   " - CameraX=" + F(cameraOffsetX) +
                   " + AlignX=" + F(alignX) +
                   " / Y: IV->Picker=" + F(effectiveY) +
                   " - CameraY=" + F(cameraOffsetY) +
                   " + AlignY=" + F(alignY) +
                   " -> PickerY=" + F(pickerYTarget) +
                   " / StageY: DieY + NeedleY(" + F(needleYOffset) + ")" +
                   " / NeedleX Offset=" + F(needleXOffset) +
                   " / T Align=" + F(alignT);
        }

        private double ResolveSignedPickerYTarget(double inputVisionToPickerY)
        {
            double magnitude = Math.Abs(inputVisionToPickerY);
            return _side == PickerSequenceSide.Rear ? -magnitude : magnitude;
        }

        private string BuildNeedleXTargetText(double targetNeedleX)
        {
            double needleXOffset = InputPickerPickTargetResolver.ResolveNeedleCalibrationOffsetX(_machine);
            double cameraOffsetX;
            double cameraOffsetY;
            InputPickerPickTargetResolver.TryResolveInputCameraToBottomOffsets(_machine, out cameraOffsetX, out cameraOffsetY);
            return "Target X=" + F(targetNeedleX) +
                   " mm  (Die VisionX=" + F(_dieX) +
                   " + CameraX=" + F(cameraOffsetX) +
                   " - NeedleXToVisionXOffset=" + F(needleXOffset) + ")";
        }

        private double ReadStageYActual()
        {
            try
            {
                return _machine != null &&
                    _machine.InputStageUnit != null &&
                    _machine.InputStageUnit.StageY != null
                    ? _machine.InputStageUnit.StageY.ActualPosition
                    : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private double ReadNeedleXActual()
        {
            try
            {
                return _machine != null &&
                    _machine.InputStageUnit != null &&
                    _machine.InputStageUnit.NeedleBlockX != null
                    ? _machine.InputStageUnit.NeedleBlockX.ActualPosition
                    : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private static double ReadAxisActual(BaseAxis axis)
        {
            try
            {
                return axis != null ? axis.ActualPosition : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private string ResolveFallbackText(PickerVisionCoordinateOffsets previewOffsets)
        {
            if (_pickerIndex <= 0)
                return "Picker #1은 저장값을 그대로 사용합니다.";

            double savedX = previewOffsets.OffsetX[_pickerIndex];
            double savedY = previewOffsets.OffsetY[_pickerIndex];
            if (Math.Abs(savedX) <= double.Epsilon || Math.Abs(savedY) <= double.Epsilon)
                return "Picker #2~4의 저장값이 0이면 Picker #1 + Pitch fallback으로 적용됩니다.";

            return "선택 Picker 저장값을 그대로 사용합니다.";
        }

        private void SetEditorEnabled(bool enabled)
        {
            if (_offsetX != null)
                _offsetX.Enabled = enabled;
            if (_offsetY != null)
                _offsetY.Enabled = enabled;
            if (_saveButton != null)
                _saveButton.Enabled = enabled;
            if (_calculateButton != null)
                _calculateButton.Enabled = enabled;
        }

        private void SetStatus(string text)
        {
            if (_statusValue != null)
                _statusValue.Text = text ?? string.Empty;
        }

        private string ResolvePickerTitle()
        {
            return (_side == PickerSequenceSide.Front ? "FRONT" : "REAR") + " PICKER #" + _pickerNo;
        }

        private string ResolveSetupPathText()
        {
            string side = _side == PickerSequenceSide.Front ? "PickerFrontUnit" : "PickerRearUnit";
            return side + ".Setup.InputVisionToPicker.OffsetX/Y[" + _pickerIndex + "]";
        }

        private static NumericUpDown CreateNumberBox()
        {
            return new NumericUpDown
            {
                DecimalPlaces = 6,
                Minimum = -100000M,
                Maximum = 100000M,
                Increment = 0.001M,
                Width = 150,
                TextAlign = HorizontalAlignment.Right
            };
        }

        private static decimal ClampDecimal(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0M;

            decimal d = (decimal)value;
            if (d < -100000M)
                return -100000M;
            if (d > 100000M)
                return 100000M;
            return d;
        }

        private static TableLayoutPanel CreateValueTable(int rowCount)
        {
            TableLayoutPanel table = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = rowCount,
                AutoSize = true,
                Dock = DockStyle.Top
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
            return table;
        }

        private static void AddStaticValueRow(TableLayoutPanel table, string title, string value)
        {
            Label label = CreateTitleLabel(title);
            Label text = CreateValueLabel(value);
            int row = table.RowStyles.Count;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(label, 0, row);
            table.Controls.Add(text, 1, row);
            table.SetColumnSpan(text, 2);
        }

        private static Label AddDynamicValueRow(TableLayoutPanel table, string title)
        {
            Label label = CreateTitleLabel(title);
            Label text = CreateValueLabel(string.Empty);
            int row = table.RowStyles.Count;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(label, 0, row);
            table.Controls.Add(text, 1, row);
            table.SetColumnSpan(text, 2);
            return text;
        }

        private static TextBox AddDynamicTextBoxRow(TableLayoutPanel table, string title, int height)
        {
            Label label = CreateTitleLabel(title);
            TextBox text = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Height = height,
                WordWrap = true
            };
            int row = table.RowStyles.Count;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, height + 6));
            table.Controls.Add(label, 0, row);
            table.Controls.Add(text, 1, row);
            table.SetColumnSpan(text, 2);
            return text;
        }

        private static void AddControlRow(TableLayoutPanel table, string title, Control control, string unit)
        {
            Label label = CreateTitleLabel(title);
            Label unitLabel = CreateValueLabel(unit);
            unitLabel.TextAlign = ContentAlignment.MiddleLeft;
            int row = table.RowStyles.Count;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(label, 0, row);
            table.Controls.Add(control, 1, row);
            table.Controls.Add(unitLabel, 2, row);
        }

        private static Label CreateTitleLabel(string text)
        {
            return new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Height = 26,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = text
            };
        }

        private static Label CreateValueLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                MinimumSize = new Size(0, 26),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = text
            };
        }

        private static string F(double value)
        {
            return value.ToString("F3");
        }
    }
}
