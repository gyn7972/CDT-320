using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    public partial class JogAxisMoveControl : UserControl
    {
        private readonly List<JogAxisItem> _items = new List<JogAxisItem>();
        private readonly Dictionary<Button, JogAxisItem> _buttonAxes = new Dictionary<Button, JogAxisItem>();
        private readonly Dictionary<Button, int> _buttonDirections = new Dictionary<Button, int>();
        private readonly Color _modeNormalColor = Color.FromArgb(235, 237, 240);
        private readonly Color _modeSelectedColor = Color.FromArgb(0, 122, 204);
        private readonly Color _normalButtonColor = Color.FromArgb(108, 118, 126);
        private readonly Color _activeButtonColor = Color.FromArgb(255, 242, 153);
        private int _buttonAreaMaxHeight = 140;
        private int _buttonAreaMinHeight = 130;
        private int _buttonAreaMaxWidth = 170;
        private int _buttonAreaMinWidth = 160;
        private bool _showCurrentSpeedMode = true;
        private bool _isJogging;

        public JogSpeedControl SpeedControl { get; set; }

        public bool ShowCurrentSpeedMode
        {
            get
            {
                try
                {
                    return _showCurrentSpeedMode;
                }
                catch
                {
                    return true;
                }
                finally
                {
                }
            }
            set
            {
                try
                {
                    _showCurrentSpeedMode = value;
                    ApplyCurrentSpeedModeVisibility();
                    ApplyModeButtonStyles();
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Current speed visibility set failed: " + ex.Message);
                }
                finally
                {
                }
            }
        }

        public int ButtonAreaMaxHeight
        {
            get
            {
                try
                {
                    return _buttonAreaMaxHeight;
                }
                catch
                {
                    return 140;
                }
                finally
                {
                }
            }
            set
            {
                try
                {
                    _buttonAreaMaxHeight = Math.Max(80, value);
                    UpdateAxisButtonAreaSize();
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Button max height set failed: " + ex.Message);
                }
                finally
                {
                }
            }
        }

        public int ButtonAreaMinHeight
        {
            get
            {
                try
                {
                    return _buttonAreaMinHeight;
                }
                catch
                {
                    return 130;
                }
                finally
                {
                }
            }
            set
            {
                try
                {
                    _buttonAreaMinHeight = Math.Max(80, value);
                    UpdateAxisButtonAreaSize();
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Button min height set failed: " + ex.Message);
                }
                finally
                {
                }
            }
        }

        public int ButtonAreaMaxWidth
        {
            get
            {
                try
                {
                    return _buttonAreaMaxWidth;
                }
                catch
                {
                    return 170;
                }
                finally
                {
                }
            }
            set
            {
                try
                {
                    _buttonAreaMaxWidth = Math.Max(120, value);
                    UpdateAxisButtonAreaSize();
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Button max width set failed: " + ex.Message);
                }
                finally
                {
                }
            }
        }

        public int ButtonAreaMinWidth
        {
            get
            {
                try
                {
                    return _buttonAreaMinWidth;
                }
                catch
                {
                    return 160;
                }
                finally
                {
                }
            }
            set
            {
                try
                {
                    _buttonAreaMinWidth = Math.Max(100, value);
                    UpdateAxisButtonAreaSize();
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Button min width set failed: " + ex.Message);
                }
                finally
                {
                }
            }
        }

        public JogAxisMoveControl()
        {
            try
            {
                InitializeComponent();
                SizeChanged += JogAxisMoveControl_SizeChanged;
                ApplyCurrentSpeedModeVisibility();
                ApplyModeButtonStyles();
                UpdateStepControlsEnabled();
                UpdateAxisButtonAreaSize();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void JogAxisMoveControl_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateAxisButtonAreaSize();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Button area resize failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ModeRadio_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                ApplyModeButtonStyles();
                UpdateStepControlsEnabled();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Mode button style failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public void SetItems(IEnumerable<JogAxisItem> items)
        {
            try
            {
                _items.Clear();
                _buttonAxes.Clear();
                _buttonDirections.Clear();
                axisButtonLayout.Controls.Clear();
                axisButtonLayout.ColumnStyles.Clear();
                axisButtonLayout.RowStyles.Clear();

                if (items != null)
                    _items.AddRange(items);

                lblStepUnit.Text = _items.Count > 0 ? _items[0].DisplayUnit : string.Empty;
                BuildAxisButtons();
                UpdateAxisButtonAreaSize();
            }
            catch (Exception ex)
            {
                string message = "Jog axis bind failed: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "JOG-AXIS", message);
                QMC.Common.MessageDialog.Show(this, message, "Jog Axis", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        public async Task<int> StopAllAsync(bool force)
        {
            try
            {
                if (!_isJogging && !force)
                    return 0;

                int finalResult = 0;
                foreach (JogAxisItem item in _items)
                {
                    int result = await item.ExecuteStopAsync();
                    if (result != 0)
                        finalResult = result;
                }

                _isJogging = false;
                ResetButtonColors();
                return finalResult;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void UpdateAxisButtonAreaSize()
        {
            try
            {
                if (rootLayout == null || axisHost == null || rootLayout.RowStyles.Count < 3 || axisHost.ColumnStyles.Count < 3 || axisHost.RowStyles.Count < 3)
                    return;

                int availableHeight = Height
                    - rootLayout.Padding.Top
                    - rootLayout.Padding.Bottom
                    - 136;
                int availableWidth = Width
                    - rootLayout.Padding.Left
                    - rootLayout.Padding.Right
                    - axisHost.Padding.Left
                    - axisHost.Padding.Right;

                if (availableHeight <= 0)
                    availableHeight = _buttonAreaMinHeight;
                if (availableWidth <= 0)
                    availableWidth = _buttonAreaMinWidth;

                int maxHeight = _buttonAreaMaxHeight;
                if (_items.Count >= 4)
                    maxHeight = Math.Max(maxHeight, 220);

                int height = Math.Min(maxHeight, availableHeight);
                height = Math.Max(Math.Min(_buttonAreaMinHeight, availableHeight), height);
                int maxWidth = _buttonAreaMaxWidth;
                if (_items.Count > 1)
                    maxWidth = Math.Max(maxWidth, _items.Count * 120);

                int width = Math.Min(maxWidth, availableWidth);
                width = Math.Max(Math.Min(_buttonAreaMinWidth, availableWidth), width);

                rootLayout.RowStyles[1].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[1].Height = height + axisHost.Padding.Top + axisHost.Padding.Bottom;
                rootLayout.RowStyles[2].SizeType = SizeType.Percent;
                rootLayout.RowStyles[2].Height = 100F;

                axisHost.ColumnStyles[0].SizeType = SizeType.Percent;
                axisHost.ColumnStyles[0].Width = 50F;
                axisHost.ColumnStyles[1].SizeType = SizeType.Absolute;
                axisHost.ColumnStyles[1].Width = width;
                axisHost.ColumnStyles[2].SizeType = SizeType.Percent;
                axisHost.ColumnStyles[2].Width = 50F;
                axisHost.RowStyles[0].SizeType = SizeType.Percent;
                axisHost.RowStyles[0].Height = 50F;
                axisHost.RowStyles[1].SizeType = SizeType.Absolute;
                axisHost.RowStyles[1].Height = height;
                axisHost.RowStyles[2].SizeType = SizeType.Percent;
                axisHost.RowStyles[2].Height = 50F;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void BuildAxisButtons()
        {
            try
            {
                axisButtonLayout.ColumnCount = Math.Max(1, _items.Count);
                axisButtonLayout.RowCount = 1;
                axisButtonLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                for (int index = 0; index < Math.Max(1, _items.Count); index++)
                    axisButtonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / Math.Max(1, _items.Count)));

                for (int index = 0; index < _items.Count; index++)
                    axisButtonLayout.Controls.Add(CreateAxisColumn(_items[index]), index, 0);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private Control CreateAxisColumn(JogAxisItem item)
        {
            try
            {
                TableLayoutPanel layout = new TableLayoutPanel();
                Label axisLabel = new Label();
                Button plusButton = new Button();
                Button stopButton = new Button();
                Button minusButton = new Button();

                layout.Dock = DockStyle.Fill;
                layout.Margin = new Padding(4);

                axisLabel.BackColor = Color.White;
                axisLabel.BorderStyle = BorderStyle.FixedSingle;
                axisLabel.Dock = DockStyle.Fill;
                axisLabel.Font = new Font("Malgun Gothic", 8.0F, FontStyle.Bold);
                axisLabel.Margin = new Padding(0, 0, 0, 4);
                axisLabel.Text = item.AxisName;
                axisLabel.TextAlign = ContentAlignment.MiddleCenter;

                ConfigureJogButton(plusButton, item.PlusText, item, 1);
                ConfigureJogButton(stopButton, "STOP", item, 0);
                ConfigureJogButton(minusButton, item.MinusText, item, -1);

                if (IsHorizontalAxis(item))
                    ConfigureHorizontalAxisLayout(layout, axisLabel, minusButton, stopButton, plusButton);
                else
                    ConfigureVerticalAxisLayout(layout, axisLabel, plusButton, stopButton, minusButton);

                return layout;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static bool IsHorizontalAxis(JogAxisItem item)
        {
            try
            {
                if (item == null)
                    return false;

                string plus = item.PlusText ?? string.Empty;
                string minus = item.MinusText ?? string.Empty;
                if (plus.IndexOf("Next", StringComparison.OrdinalIgnoreCase) >= 0 || minus.IndexOf("Prev", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                return plus.IndexOf("X", StringComparison.OrdinalIgnoreCase) >= 0
                    || minus.IndexOf("X", StringComparison.OrdinalIgnoreCase) >= 0
                    || plus.IndexOf("T", StringComparison.OrdinalIgnoreCase) >= 0
                    || minus.IndexOf("T", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static void ConfigureHorizontalAxisLayout(TableLayoutPanel layout, Label axisLabel, Button minusButton, Button stopButton, Button plusButton)
        {
            try
            {
                layout.ColumnCount = 3;
                layout.RowCount = 2;
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                layout.SetColumnSpan(axisLabel, 3);
                layout.Controls.Add(axisLabel, 0, 0);
                layout.Controls.Add(minusButton, 0, 1);
                layout.Controls.Add(stopButton, 1, 1);
                layout.Controls.Add(plusButton, 2, 1);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static void ConfigureVerticalAxisLayout(TableLayoutPanel layout, Label axisLabel, Button plusButton, Button stopButton, Button minusButton)
        {
            try
            {
                layout.ColumnCount = 1;
                layout.RowCount = 4;
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));

                layout.Controls.Add(axisLabel, 0, 0);
                layout.Controls.Add(plusButton, 0, 1);
                layout.Controls.Add(stopButton, 0, 2);
                layout.Controls.Add(minusButton, 0, 3);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void ConfigureJogButton(Button button, string text, JogAxisItem item, int direction)
        {
            try
            {
                button.BackColor = direction == 0 ? Color.FromArgb(208, 208, 208) : _normalButtonColor;
                button.Dock = DockStyle.Fill;
                button.FlatStyle = FlatStyle.Flat;
                button.Font = new Font("Malgun Gothic", direction == 0 ? 9.0F : 11.0F, FontStyle.Bold);
                button.ForeColor = direction == 0 ? Color.Black : Color.White;
                button.Margin = new Padding(3, 3, 3, 5);
                button.Text = text;
                button.UseVisualStyleBackColor = false;

                _buttonAxes[button] = item;
                _buttonDirections[button] = direction;

                if (direction == 0)
                {
                    button.Click += btnStop_Click;
                    return;
                }

                button.MouseDown += btnJog_MouseDown;
                button.MouseUp += btnJog_MouseUp;
                button.MouseLeave += btnJog_MouseLeave;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async void btnJog_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                if (button == null || !_buttonAxes.ContainsKey(button))
                    return;

                SetButtonActive(button);
                await StartJogAsync(_buttonAxes[button], _buttonDirections[button]);
            }
            catch (Exception ex)
            {
                ShowJogError("Jog start failed", ex);
            }
            finally
            {
                if (rdoStep.Checked)
                    ResetButtonColors();
            }
        }

        private async void btnJog_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                await StopAllAsync(false);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Jog mouse up stop failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async void btnJog_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                await StopAllAsync(false);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Jog mouse leave stop failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                Button button = sender as Button;
                if (button == null || !_buttonAxes.ContainsKey(button))
                    return;

                int result = await _buttonAxes[button].ExecuteStopAsync();
                _isJogging = false;
                ResetButtonColors();
                if (result != 0)
                    throw new InvalidOperationException("Jog stop returned " + result);
            }
            catch (Exception ex)
            {
                ShowJogError("Jog stop failed", ex);
            }
            finally
            {
            }
        }

        private async Task StartJogAsync(JogAxisItem item, int direction)
        {
            try
            {
                if (item == null)
                    return;

                await StopAllAsync(true);

                if (rdoStep.Checked)
                {
                    double axisStep = item.FromDisplayDistance(Convert.ToDouble(numStepDistance.Value, CultureInfo.InvariantCulture));
                    int stepResult = await item.ExecuteStepAsync(direction, GetJogSpeedType(), CurrentJogSpeed(item), axisStep);
                    if (stepResult != 0)
                        throw new InvalidOperationException("Jog step returned " + stepResult);

                    EventLogger.Write(EventKind.Event, "UI", "JOG-AXIS", item.AxisName + " step jog complete.");
                    return;
                }

                int result = await item.ExecuteContinuousAsync(direction, GetJogSpeedType(), CurrentJogSpeed(item));
                if (result != 0)
                    throw new InvalidOperationException("Jog continuous returned " + result);

                _isJogging = true;
                EventLogger.Write(EventKind.Event, "UI", "JOG-AXIS", item.AxisName + " continuous jog start.");
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private JogSpeedType GetJogSpeedType()
        {
            try
            {
                if (_showCurrentSpeedMode && rdoCurrent.Checked)
                    return JogSpeedType.Custom;
                if (rdoCoarse.Checked)
                    return JogSpeedType.Coarse;

                return JogSpeedType.Fine;
            }
            catch
            {
                return JogSpeedType.Fine;
            }
            finally
            {
            }
        }

        private double CurrentJogSpeed(JogAxisItem item)
        {
            try
            {
                if (SpeedControl != null)
                    return SpeedControl.GetCustomSpeed(item != null ? item.Axis : null);

                if (item == null || item.Axis == null)
                    return 1.0;

                return Math.Max(0.1, item.Axis.Config.JogCoarseVelocity * 0.5);
            }
            catch
            {
                return 1.0;
            }
            finally
            {
            }
        }

        private void btnStep1000_Click(object sender, EventArgs e)
        {
            try { numStepDistance.Value = 1000; } catch { } finally { }
        }

        private void btnStep100_Click(object sender, EventArgs e)
        {
            try { numStepDistance.Value = 100; } catch { } finally { }
        }

        private void btnStep10_Click(object sender, EventArgs e)
        {
            try { numStepDistance.Value = 10; } catch { } finally { }
        }

        private void btnStep1_Click(object sender, EventArgs e)
        {
            try { numStepDistance.Value = 1; } catch { } finally { }
        }

        private void btnStepZero_Click(object sender, EventArgs e)
        {
            try { numStepDistance.Value = numStepDistance.Minimum; } catch { } finally { }
        }

        private void ApplyModeButtonStyles()
        {
            try
            {
                foreach (RadioButton radio in new[] { rdoFine, rdoCoarse, rdoCurrent, rdoContinuous, rdoStep })
                {
                    if (radio == rdoCurrent && !_showCurrentSpeedMode)
                        continue;

                    radio.BackColor = radio.Checked ? _modeSelectedColor : _modeNormalColor;
                    radio.ForeColor = radio.Checked ? Color.White : Color.FromArgb(30, 35, 40);
                    radio.UseVisualStyleBackColor = false;
                    radio.FlatAppearance.BorderColor = radio.Checked ? Color.FromArgb(0, 95, 160) : Color.FromArgb(160, 165, 170);
                    radio.FlatAppearance.BorderSize = 1;
                    radio.Invalidate();
                }
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void ApplyCurrentSpeedModeVisibility()
        {
            try
            {
                rdoCurrent.Visible = _showCurrentSpeedMode;
                rdoCurrent.Enabled = _showCurrentSpeedMode;
                if (!_showCurrentSpeedMode && rdoCurrent.Checked)
                    rdoFine.Checked = true;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void UpdateStepControlsEnabled()
        {
            try
            {
                bool isStepMode = rdoStep.Checked;
                foreach (Control control in new Control[] { numStepDistance, lblStepUnit, btnStep1000, btnStep100, btnStep10, btnStep1, btnStepZero })
                {
                    control.Enabled = isStepMode;
                    control.ForeColor = isStepMode ? Color.FromArgb(30, 35, 40) : Color.FromArgb(130, 135, 140);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Step control enable failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void SetButtonActive(Button activeButton)
        {
            try
            {
                ResetButtonColors();
                activeButton.BackColor = _activeButtonColor;
                activeButton.ForeColor = Color.Black;
                activeButton.UseVisualStyleBackColor = false;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ResetButtonColors()
        {
            try
            {
                foreach (Button button in _buttonAxes.Keys)
                {
                    int direction = _buttonDirections.ContainsKey(button) ? _buttonDirections[button] : 0;
                    button.BackColor = direction == 0 ? Color.FromArgb(208, 208, 208) : _normalButtonColor;
                    button.ForeColor = direction == 0 ? Color.Black : Color.White;
                    button.UseVisualStyleBackColor = false;
                    button.Invalidate();
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ShowJogError(string title, Exception ex)
        {
            try
            {
                string message = title + ": " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "JOG-AXIS", message);
                QMC.Common.MessageDialog.Show(this, message, "Jog Axis", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}


