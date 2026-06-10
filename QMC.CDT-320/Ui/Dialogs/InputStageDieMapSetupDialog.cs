using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.CDT320;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class InputStageDieMapSetupDialog : Form
    {
        private readonly InputStageUnit _stage;
        private readonly Action _saveAction;

        public InputStageDieMapSetupDialog(InputStageUnit stage)
            : this(stage, null)
        {
        }

        public InputStageDieMapSetupDialog(InputStageUnit stage, Action saveAction)
        {
            _stage = stage ?? throw new ArgumentNullException("stage");
            _saveAction = saveAction;
            _stage.Recipe.EnsurePositionObjects();
            InitializeComponent();
            LoadFromRecipe();
        }

        private void LoadFromRecipe()
        {
            try
            {
                var recipe = _stage.Recipe.DieMap;
                recipe.EnsurePoints();
                _targetId.Text = recipe.VisionTargetId;
                _retryCount.Value = Math.Max(_retryCount.Minimum, Math.Min(_retryCount.Maximum, recipe.VisionRetryCount));
                _grid.Rows.Clear();
                foreach (var point in recipe.Points())
                    _grid.Rows.Add(point.Enabled, point.Name, Format(point.StageYPosition), Format(point.VisionXPosition), Format(point.VisionOffsetX), Format(point.VisionOffsetY));
                if (_grid.Rows.Count > 0)
                    _grid.Rows[0].Selected = true;
                _view.Invalidate();
                SetStatus("Loaded. Use InputStage view/jog, then TEACH current position.");
            }
            catch (Exception ex)
            {
                SetStatus("Load failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void SaveGridToRecipe()
        {
            var recipe = _stage.Recipe.DieMap;
            recipe.EnsurePoints();
            recipe.VisionTargetId = string.IsNullOrWhiteSpace(_targetId.Text) ? "DieMapMark" : _targetId.Text.Trim();
            recipe.VisionRetryCount = (int)_retryCount.Value;
            var points = recipe.Points();
            for (int i = 0; i < points.Length && i < _grid.Rows.Count; i++)
            {
                DataGridViewRow row = _grid.Rows[i];
                points[i].Enabled = Convert.ToBoolean(row.Cells["Enabled"].Value);
                points[i].StageYPosition = Parse(row.Cells["StageY"].Value);
                points[i].VisionXPosition = Parse(row.Cells["VisionX"].Value);
                points[i].VisionOffsetX = Parse(row.Cells["OffsetX"].Value);
                points[i].VisionOffsetY = Parse(row.Cells["OffsetY"].Value);
            }
        }

        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                SaveGridToRecipe();
                _view.Invalidate();
            }
            catch (Exception ex)
            {
                SetStatus("Edit failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                _view.Invalidate();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void BtnTeach_Click(object sender, EventArgs e)
        {
            try
            {
                TeachCurrent();
            }
            catch (Exception ex)
            {
                SetStatus("TEACH failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void BtnVisionTest_Click(object sender, EventArgs e)
        {
            try
            {
                VisionTest();
            }
            catch (Exception ex)
            {
                SetStatus("VISION TEST failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async void BtnMove_Click(object sender, EventArgs e)
        {
            try
            {
                DataGridViewRow row = SelectedRow();
                if (row == null)
                    return;

                string pointName = Convert.ToString(row.Cells["Name"].Value);
                if (!ConfirmAction(pointName + " 위치로 이동하시겠습니까?"))
                    return;

                SetButtonsEnabled(false);
                Cursor = Cursors.WaitCursor;
                int result = await MoveSelectedPointAsync(row);
                if (result == 0)
                    SetStatus(pointName + " move complete.");
                else
                    QMC.Common.MessageDialog.Show(this, pointName + " 이동 실패. result=" + result, "Die Map Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                SetStatus("MOVE failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Die Map Setup Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                SetButtonsEnabled(true);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmAction("Die Map Setup 정보를 저장하시겠습니까?"))
                    return;

                SaveGridToRecipe();
                if (_saveAction != null)
                    _saveAction();
                SetStatus("Saved.");
            }
            catch (Exception ex)
            {
                SetStatus("Save failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void TeachCurrent()
        {
            DataGridViewRow row = SelectedRow();
            if (row == null)
                return;

            row.Cells["StageY"].Value = Format(_stage.StageY != null ? _stage.StageY.ActualPosition : 0.0);
            row.Cells["VisionX"].Value = Format(_stage.CameraX != null ? _stage.CameraX.ActualPosition : 0.0);
            SaveGridToRecipe();
            _view.Invalidate();
            SetStatus(Convert.ToString(row.Cells["Name"].Value) + " taught from current StageY / VisionX.");
        }

        private void VisionTest()
        {
            DataGridViewRow row = SelectedRow();
            if (row == null)
                return;

            SaveGridToRecipe();
            row.Cells["OffsetX"].Value = "0.000";
            row.Cells["OffsetY"].Value = "0.000";
            SaveGridToRecipe();
            _view.Invalidate();
            SetStatus("Vision PC interface placeholder. Offset=(0, 0).");
        }

        private async Task<int> MoveSelectedPointAsync(DataGridViewRow row)
        {
            try
            {
                if (_stage == null)
                    return Fail("IN-STAGE-DIEMAP-MOVE-UNIT", "InputStageUnit is null.");

                SaveGridToRecipe();

                double stageY = Parse(row.Cells["StageY"].Value);
                double visionX = Parse(row.Cells["VisionX"].Value);
                string pointName = Convert.ToString(row.Cells["Name"].Value);

                SetStatus(pointName + " move command...");
                Task<int> moveY = _stage.MoveInputStageAxis(WaferStageAxis.WaferY, stageY, true);
                Task<int> moveX = _stage.MoveInputStageAxis(WaferStageAxis.VisionX, visionX, true);
                int[] moveResults = await Task.WhenAll(moveY, moveX);
                if (moveResults[0] != 0)
                    return Fail("IN-STAGE-DIEMAP-MOVE-Y", pointName + " StageY move command failed. target=" + stageY + ", result=" + moveResults[0]);
                if (moveResults[1] != 0)
                    return Fail("IN-STAGE-DIEMAP-MOVE-X", pointName + " VisionX move command failed. target=" + visionX + ", result=" + moveResults[1]);

                SetStatus(pointName + " wait in-position...");
                int timeoutMs = ResolveMoveTimeout();
                Task<int> waitY = _stage.WaitInputStageAxisInPosition(WaferStageAxis.WaferY, stageY, timeoutMs);
                Task<int> waitX = _stage.WaitInputStageAxisInPosition(WaferStageAxis.VisionX, visionX, timeoutMs);
                int[] waitResults = await Task.WhenAll(waitY, waitX);
                if (waitResults[0] != 0)
                    return Fail("IN-STAGE-DIEMAP-WAIT-Y", pointName + " StageY move done timeout. target=" + stageY + ", result=" + waitResults[0]);
                if (waitResults[1] != 0)
                    return Fail("IN-STAGE-DIEMAP-WAIT-X", pointName + " VisionX move done timeout. target=" + visionX + ", result=" + waitResults[1]);

                int checkResult = CheckAxisInPosition(_stage.StageY, stageY, pointName + " StageY");
                if (checkResult != 0)
                    return checkResult;

                checkResult = CheckAxisInPosition(_stage.CameraX, visionX, pointName + " VisionX");
                if (checkResult != 0)
                    return checkResult;

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-MOVE-EX", "Die Map point move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckAxisInPosition(QMC.Common.Motion.BaseAxis axis, double target, string description)
        {
            try
            {
                if (axis == null)
                    return Fail("IN-STAGE-DIEMAP-AXIS", description + " axis is null.");
                if (axis.IsMoving)
                    return Fail("IN-STAGE-DIEMAP-MOVING", description + " axis is still moving.");
                if (axis.IsAlarm)
                    return Fail("IN-STAGE-DIEMAP-ALARM", description + " axis alarm is on.");
                if (!IsAxisInPosition(axis, target))
                    return Fail("IN-STAGE-DIEMAP-POSITION", description + " final position check failed. target=" + target + ", actual=" + axis.ActualPosition);

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-POSITION-EX", description + " final position check exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsAxisInPosition(QMC.Common.Motion.BaseAxis axis, double target)
        {
            try
            {
                if (axis == null)
                    return false;

                double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                    ? axis.Config.InPositionTolerance
                    : 0.05;
                return Math.Abs(axis.ActualPosition - target) <= tolerance;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private int ResolveMoveTimeout()
        {
            try
            {
                return _stage != null && _stage.Config != null && _stage.Config.SequenceMoveTimeoutMs > 0
                    ? _stage.Config.SequenceMoveTimeoutMs
                    : 10000;
            }
            catch
            {
                return 10000;
            }
            finally
            {
            }
        }

        private int Fail(string code, string message)
        {
            try
            {
                AlarmManager.Raise(AlarmSeverity.Warning, code, _stage != null ? _stage.Name : "InputStageUnit", message);
                QMC.Common.Log.Write("Main", "UI", code, message + " - Failed");
                SetStatus(message);
            }
            catch
            {
            }
            finally
            {
            }

            return -1;
        }

        private bool ConfirmAction(string message)
        {
            try
            {
                DialogResult result = QMC.Common.MessageDialog.Show(this, message, "Die Map Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return result == DialogResult.Yes;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            try
            {
                btnTeach.Enabled = enabled;
                btnMove.Enabled = enabled;
                btnVisionTest.Enabled = enabled;
                btnSave.Enabled = enabled;
                btnClose.Enabled = enabled;
                _grid.Enabled = enabled;
                _targetId.Enabled = enabled;
                _retryCount.Enabled = enabled;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void View_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(Color.White);

                Rectangle rect = _view.ClientRectangle;
                rect.Inflate(-28, -28);
                if (rect.Width <= 0 || rect.Height <= 0)
                    return;

                int size = Math.Min(rect.Width, rect.Height);
                Rectangle waferRect = new Rectangle(
                    rect.Left + (rect.Width - size) / 2,
                    rect.Top + (rect.Height - size) / 2,
                    size,
                    size);

                using (var waferBrush = new SolidBrush(Color.FromArgb(248, 248, 248)))
                    e.Graphics.FillEllipse(waferBrush, waferRect);
                using (var waferPen = new Pen(Color.DimGray, 2))
                    e.Graphics.DrawEllipse(waferPen, waferRect);

                DrawCenterLines(e.Graphics, waferRect);

                var points = _stage.Recipe.DieMap.Points();
                foreach (var point in points)
                    DrawMarkPoint(e.Graphics, waferRect, point);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void DrawCenterLines(Graphics graphics, Rectangle rect)
        {
            using (var pen = new Pen(Color.Gainsboro, 1))
            {
                pen.DashStyle = DashStyle.Dash;
                graphics.DrawLine(pen, rect.Left + rect.Width / 2, rect.Top, rect.Left + rect.Width / 2, rect.Bottom);
                graphics.DrawLine(pen, rect.Left, rect.Top + rect.Height / 2, rect.Right, rect.Top + rect.Height / 2);
            }
        }

        private void DrawMarkPoint(Graphics graphics, Rectangle rect, InputStageDieMapMarkPoint point)
        {
            PointF p = ResolveViewPoint(rect, point != null ? point.Name : "");
            bool selected = IsSelectedPoint(point != null ? point.Name : "");
            Color color = selected ? Color.Orange : (point != null && point.Enabled ? Color.DeepSkyBlue : Color.Gray);

            using (var brush = new SolidBrush(color))
                graphics.FillEllipse(brush, p.X - 8, p.Y - 8, 16, 16);
            using (var pen = new Pen(Color.White, 2))
                graphics.DrawEllipse(pen, p.X - 8, p.Y - 8, 16, 16);
            using (var textBrush = new SolidBrush(Color.Black))
                graphics.DrawString(point != null ? point.Name : "", Font, textBrush, p.X + 11, p.Y - 10);
        }

        private bool IsSelectedPoint(string name)
        {
            DataGridViewRow row = SelectedRow();
            return row != null && string.Equals(Convert.ToString(row.Cells["Name"].Value), name, StringComparison.OrdinalIgnoreCase);
        }

        private static PointF ResolveViewPoint(Rectangle rect, string name)
        {
            if (string.Equals(name, "Top", StringComparison.OrdinalIgnoreCase))
                return new PointF(rect.Left + rect.Width / 2F, rect.Top + 24);
            if (string.Equals(name, "Bottom", StringComparison.OrdinalIgnoreCase))
                return new PointF(rect.Left + rect.Width / 2F, rect.Bottom - 24);
            if (string.Equals(name, "Left", StringComparison.OrdinalIgnoreCase))
                return new PointF(rect.Left + 24, rect.Top + rect.Height / 2F);
            if (string.Equals(name, "Right", StringComparison.OrdinalIgnoreCase))
                return new PointF(rect.Right - 24, rect.Top + rect.Height / 2F);
            return new PointF(rect.Left + rect.Width / 2F, rect.Top + rect.Height / 2F);
        }

        private DataGridViewRow SelectedRow()
        {
            return _grid.SelectedRows.Count > 0 ? _grid.SelectedRows[0] : (_grid.Rows.Count > 0 ? _grid.Rows[0] : null);
        }

        private void SetStatus(string text)
        {
            _status.Text = text ?? "";
        }

        private static string Format(double value)
        {
            return value.ToString("F3", CultureInfo.InvariantCulture);
        }

        private static double Parse(object value)
        {
            double result;
            return double.TryParse(Convert.ToString(value), NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? result : 0.0;
        }
    }
}
