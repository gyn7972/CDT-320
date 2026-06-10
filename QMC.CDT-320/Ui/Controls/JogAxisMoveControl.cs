using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    public enum JogAxisMoveLayoutMode
    {
        AxisColumns,
        Stage,
        PickerTabbed,
        InputStagePad,
        OutputStagePad
    }

    public partial class JogAxisMoveControl : UserControl
    {
        private readonly List<JogAxisItem> _items = new List<JogAxisItem>();
        private readonly Dictionary<Button, JogAxisItem> _buttonAxes = new Dictionary<Button, JogAxisItem>();
        private readonly Dictionary<Button, int> _buttonDirections = new Dictionary<Button, int>();
        private readonly Color _modeNormalColor = Color.FromArgb(235, 237, 240);
        private readonly Color _modeSelectedColor = Color.FromArgb(0, 122, 204);
        private readonly Color _normalButtonColor = Color.FromArgb(108, 118, 126);
        private readonly Color _activeButtonColor = Color.FromArgb(255, 242, 153);
        private const int StageJogButtonWidth = 74;
        private const int StageJogButtonHeight = 48;
        private const int StageAxisLabelHeight = 20;
        private const int StageAxisColumnWidth = StageJogButtonWidth;
        private const int StageAxisColumnHeight = StageAxisLabelHeight + (StageJogButtonHeight * 3);
        private const int StageAxisColumnGap = 4;
        private int _buttonAreaMaxHeight = 92;
        private int _buttonAreaMinHeight = 72;
        private int _buttonAreaMaxWidth = 132;
        private int _buttonAreaMinWidth = 112;
        private int _axisColumnsPerRow = 0;
        private bool _showCurrentSpeedMode = true;
        private bool _isJogging;
        private JogAxisMoveLayoutMode _layoutMode = JogAxisMoveLayoutMode.AxisColumns;

        public JogSpeedControl SpeedControl { get; set; }

        public JogAxisMoveLayoutMode LayoutMode
        {
            get
            {
                try
                {
                    return _layoutMode;
                }
                catch
                {
                    return JogAxisMoveLayoutMode.AxisColumns;
                }
                finally
                {
                }
            }
            set
            {
                try
                {
                    _layoutMode = value;
                    if (LicenseManager.UsageMode == LicenseUsageMode.Designtime && _items.Count == 1 && _items[0].Axis == null)
                        _items.Clear();
                    RebuildAxisButtons();
                    ApplyDesignTimeItems();
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Layout mode set failed: " + ex.Message);
                }
                finally
                {
                }
            }
        }

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
                    return 92;
                }
                finally
                {
                }
            }
            set
            {
                try
                {
                    _buttonAreaMaxHeight = Math.Max(56, value);
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
                    return 72;
                }
                finally
                {
                }
            }
            set
            {
                try
                {
                    _buttonAreaMinHeight = Math.Max(48, value);
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
                    return 132;
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
                    return 112;
                }
                finally
                {
                }
            }
            set
            {
                try
                {
                    _buttonAreaMinWidth = Math.Max(80, value);
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

        /// <summary>0이면 단일 행(Flow), 1 이상이면 해당 열 수의 그리드로 축 버튼을 배치합니다(AxisColumns 모드, 축 2개 이상일 때).</summary>
        public int AxisColumnsPerRow
        {
            get
            {
                try { return _axisColumnsPerRow; }
                catch { return 0; }
                finally { }
            }
            set
            {
                try
                {
                    _axisColumnsPerRow = Math.Max(0, value);
                    RebuildAxisButtons();
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "AxisColumnsPerRow set failed: " + ex.Message);
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
                cboStepPreset.SelectedIndex = 0;
                SizeChanged += JogAxisMoveControl_SizeChanged;
                ApplyCurrentSpeedModeVisibility();
                ApplyModeButtonStyles();
                UpdateStepControlsEnabled();
                UpdateAxisButtonAreaSize();
                ApplyDesignTimeItems();
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

        private void ApplyDesignTimeItems()
        {
            try
            {
                if (LicenseManager.UsageMode != LicenseUsageMode.Designtime || _items.Count > 0)
                    return;

                if (_layoutMode == JogAxisMoveLayoutMode.OutputStagePad)
                {
                    SetItems(new[]
                    {
                        JogAxisItem.Single("GOOD Y", null, AxisUnitConverter.Micrometer, 1.0, "Y+", "Y-"),
                        JogAxisItem.Single("GOOD Z", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-"),
                        JogAxisItem.Single("NG Y", null, AxisUnitConverter.Micrometer, 1.0, "Y+", "Y-"),
                        JogAxisItem.Single("NG Z", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-"),
                        JogAxisItem.Single("VISION X", null, AxisUnitConverter.Micrometer, 1.0, "X+", "X-")
                    });
                    return;
                }

                if (_layoutMode == JogAxisMoveLayoutMode.InputStagePad)
                {
                    SetItems(new[]
                    {
                        JogAxisItem.Single("StageY", null, AxisUnitConverter.Micrometer, 1.0, "Y+", "Y-"),
                        JogAxisItem.Single("StageT", null, AxisUnitConverter.Degree, 1.0, "T+", "T-"),
                        JogAxisItem.Single("ExpanderZ", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-"),
                        JogAxisItem.Single("CameraX", null, AxisUnitConverter.Micrometer, 1.0, "X+", "X-"),
                        JogAxisItem.Single("NeedleX", null, AxisUnitConverter.Micrometer, 1.0, "X+", "X-"),
                        JogAxisItem.Single("NeedleZ", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-"),
                        JogAxisItem.Single("EjectPinZ", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-")
                    });
                    return;
                }

                if (_layoutMode == JogAxisMoveLayoutMode.PickerTabbed)
                {
                    SetItems(new[]
                    {
                        JogAxisItem.Single("PICKER X", null, AxisUnitConverter.Micrometer, 1.0, "X+", "X-"),
                        JogAxisItem.Single("PICKER Y", null, AxisUnitConverter.Micrometer, 1.0, "Y+", "Y-"),
                        JogAxisItem.Single("PICKER T0", null, AxisUnitConverter.Degree, 1.0, "T+", "T-"),
                        JogAxisItem.Single("PICKER Z0", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-"),
                        JogAxisItem.Single("PICKER T1", null, AxisUnitConverter.Degree, 1.0, "T+", "T-"),
                        JogAxisItem.Single("PICKER Z1", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-"),
                        JogAxisItem.Single("PICKER T2", null, AxisUnitConverter.Degree, 1.0, "T+", "T-"),
                        JogAxisItem.Single("PICKER Z2", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-"),
                        JogAxisItem.Single("PICKER T3", null, AxisUnitConverter.Degree, 1.0, "T+", "T-"),
                        JogAxisItem.Single("PICKER Z3", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-")
                    });
                    return;
                }

                if (_layoutMode == JogAxisMoveLayoutMode.Stage)
                {
                    SetItems(new[]
                    {
                        JogAxisItem.Single("StageY", null, AxisUnitConverter.Micrometer, 1.0, "Y+", "Y-"),
                        JogAxisItem.Single("StageT", null, AxisUnitConverter.Degree, 1.0, "T+", "T-"),
                        JogAxisItem.Single("ExpanderZ", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-"),
                        JogAxisItem.Single("CameraX", null, AxisUnitConverter.Micrometer, 1.0, "X+", "X-"),
                        JogAxisItem.Single("NeedleX", null, AxisUnitConverter.Micrometer, 1.0, "X+", "X-"),
                        JogAxisItem.Single("NeedleZ", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-"),
                        JogAxisItem.Single("EjectPinZ", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-")
                    });
                    return;
                }

                SetItems(new[]
                {
                    JogAxisItem.Single("AXIS", null, AxisUnitConverter.Micrometer, 1.0, "+", "-")
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Design items failed: " + ex.Message);
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

                float modePanelHeight = rootLayout.RowStyles[0].Height;
                if (rootLayout.RowStyles[0].SizeType != SizeType.Absolute)
                    modePanelHeight = 84F;

                int availableHeight = Height
                    - rootLayout.Padding.Top
                    - rootLayout.Padding.Bottom
                    - (int)modePanelHeight;
                int availableWidth = Width
                    - rootLayout.Padding.Left
                    - rootLayout.Padding.Right
                    - axisHost.Padding.Left
                    - axisHost.Padding.Right;

                if (availableHeight <= 0)
                    availableHeight = _buttonAreaMinHeight;
                if (availableWidth <= 0)
                    availableWidth = _buttonAreaMinWidth;

                int requiredStageHeight = 0;
                int requiredStageWidth = 0;
                if ((_layoutMode == JogAxisMoveLayoutMode.PickerTabbed || _layoutMode == JogAxisMoveLayoutMode.InputStagePad || _layoutMode == JogAxisMoveLayoutMode.OutputStagePad) && _items.Count > 1)
                {
                    requiredStageHeight = Math.Max(_buttonAreaMinHeight, availableHeight - axisHost.Padding.Top - axisHost.Padding.Bottom);
                    requiredStageWidth = availableWidth;
                }
                else if (_layoutMode == JogAxisMoveLayoutMode.Stage && _items.Count > 1)
                {
                    int zAxisCount = CountStageVerticalAxes();
                    int horizontalAxisCount = CountStageHorizontalExtraAxes();
                    int zRowCount = Math.Max(1, (int)Math.Ceiling(zAxisCount / 5.0));
                    requiredStageHeight = StageAxisColumnHeight + StageAxisColumnGap + (zRowCount * StageAxisColumnHeight) + Math.Max(0, zRowCount - 1) * StageAxisColumnGap;
                    if (horizontalAxisCount > 0)
                        requiredStageHeight += horizontalAxisCount * (StageAxisLabelHeight + StageJogButtonHeight + StageAxisColumnGap);
                    requiredStageWidth = GetStageSlotLayoutWidth();
                }
                else if (_axisColumnsPerRow > 0 && _items.Count > 1)
                {
                    int cols = Math.Max(1, Math.Min(_axisColumnsPerRow, _items.Count));
                    int rows = (int)Math.Ceiling(_items.Count / (double)cols);
                    requiredStageWidth = cols * StageAxisColumnWidth + Math.Max(0, cols - 1) * StageAxisColumnGap + StageAxisColumnGap;
                    requiredStageHeight = rows * StageAxisColumnHeight + Math.Max(0, rows - 1) * StageAxisColumnGap + StageAxisColumnGap;
                }
                else if (_items.Count > 0)
                {
                    requiredStageHeight = GetAxisItemRequiredHeight(_items);
                    requiredStageWidth = GetAxisItemRequiredWidth(_items);
                }

                int maxHeight = Math.Max(_buttonAreaMaxHeight, requiredStageHeight);

                int height = Math.Min(maxHeight, availableHeight);
                height = Math.Max(Math.Min(Math.Max(_buttonAreaMinHeight, requiredStageHeight), availableHeight), height);
                int maxWidth = _buttonAreaMaxWidth;
                if (requiredStageWidth > 0)
                    maxWidth = Math.Max(maxWidth, requiredStageWidth);
                else if (_items.Count > 1)
                    maxWidth = Math.Max(maxWidth, _items.Count * 72);

                int width = Math.Min(maxWidth, availableWidth);
                width = Math.Max(Math.Min(Math.Max(_buttonAreaMinWidth, requiredStageWidth), availableWidth), width);

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
                if (_layoutMode == JogAxisMoveLayoutMode.OutputStagePad && _items.Count > 1)
                {
                    BuildOutputStagePadButtons();
                    return;
                }

                if (_layoutMode == JogAxisMoveLayoutMode.InputStagePad && _items.Count > 1)
                {
                    BuildInputStagePadButtons();
                    return;
                }

                if (_layoutMode == JogAxisMoveLayoutMode.PickerTabbed && _items.Count > 1)
                {
                    BuildPickerTabbedButtons();
                    return;
                }

                if (_layoutMode == JogAxisMoveLayoutMode.Stage && _items.Count > 1)
                {
                    BuildStageButtons();
                    return;
                }

                axisButtonLayout.ColumnCount = 1;
                axisButtonLayout.RowCount = 1;
                axisButtonLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                axisButtonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                Control axisContent = (_axisColumnsPerRow > 0 && _items.Count > 1)
                    ? CreateCenteredAxisGrid(_items, _axisColumnsPerRow)
                    : CreateCenteredAxisFlow(_items);
                axisButtonLayout.Controls.Add(axisContent, 0, 0);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void RebuildAxisButtons()
        {
            try
            {
                if (axisButtonLayout == null)
                    return;

                _buttonAxes.Clear();
                _buttonDirections.Clear();
                axisButtonLayout.Controls.Clear();
                axisButtonLayout.ColumnStyles.Clear();
                axisButtonLayout.RowStyles.Clear();
                BuildAxisButtons();
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

        private static int GetAxisItemRequiredHeight(IEnumerable<JogAxisItem> items)
        {
            try
            {
                int height = StageAxisColumnHeight;
                foreach (JogAxisItem item in items ?? new JogAxisItem[0])
                {
                    if (GetControlKind(item) == JogAxisControlKind.Horizontal)
                        height = Math.Max(height, StageAxisLabelHeight + StageJogButtonHeight);
                }

                return height;
            }
            catch
            {
                return StageAxisColumnHeight;
            }
            finally
            {
            }
        }

        private static int GetAxisItemRequiredWidth(IEnumerable<JogAxisItem> items)
        {
            try
            {
                int totalWidth = 0;
                int count = 0;
                foreach (JogAxisItem item in items ?? new JogAxisItem[0])
                {
                    int itemWidth = GetControlKind(item) == JogAxisControlKind.Horizontal ? (StageJogButtonWidth * 3) : StageAxisColumnWidth;
                    totalWidth += itemWidth;
                    count++;
                }

                return totalWidth + Math.Max(0, count - 1) * StageAxisColumnGap;
            }
            catch
            {
                return StageAxisColumnWidth;
            }
            finally
            {
            }
        }

        private int CountStageVerticalAxes()
        {
            try
            {
                int count = 0;
                JogAxisItem yItem = FirstAxisByName("StageY") ?? FirstAxisByText("Y");
                JogAxisItem cameraXItem = FirstAxisByName("CameraX");
                JogAxisItem tItem = FirstAxisByName("StageT") ?? FirstAxisByText("T");

                foreach (JogAxisItem item in _items)
                {
                    if (item == null)
                        continue;
                    if (ReferenceEquals(item, yItem) || ReferenceEquals(item, cameraXItem) || ReferenceEquals(item, tItem))
                        continue;
                    if (GetControlKind(item) == JogAxisControlKind.Horizontal)
                        continue;

                    count++;
                }

                return count;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
        }

        private int CountStageHorizontalExtraAxes()
        {
            try
            {
                int count = 0;
                JogAxisItem yItem = FirstAxisByName("StageY") ?? FirstAxisByText("Y");
                JogAxisItem cameraXItem = FirstAxisByName("CameraX");
                JogAxisItem tItem = FirstAxisByName("StageT") ?? FirstAxisByText("T");

                foreach (JogAxisItem item in _items)
                {
                    if (item == null)
                        continue;
                    if (ReferenceEquals(item, yItem) || ReferenceEquals(item, cameraXItem) || ReferenceEquals(item, tItem))
                        continue;
                    if (GetControlKind(item) == JogAxisControlKind.Horizontal)
                        count++;
                }

                return count;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
        }

        private void BuildStageButtons()
        {
            try
            {
                JogAxisItem yItem = FirstAxisByName("StageY") ?? FirstAxisByText("Y");
                JogAxisItem cameraXItem = FirstAxisByName("CameraX");
                JogAxisItem tItem = FirstAxisByName("StageT") ?? FirstAxisByText("T");
                List<JogAxisItem> zItems = new List<JogAxisItem>();
                List<JogAxisItem> horizontalExtraItems = new List<JogAxisItem>();
                List<JogAxisItem> verticalExtraItems = new List<JogAxisItem>();

                foreach (JogAxisItem item in _items)
                {
                    if (item == null)
                        continue;
                    if (ReferenceEquals(item, yItem) || ReferenceEquals(item, cameraXItem) || ReferenceEquals(item, tItem))
                        continue;

                    if (GetControlKind(item) == JogAxisControlKind.Horizontal)
                    {
                        horizontalExtraItems.Add(item);
                        continue;
                    }

                    if (IsStageZGroupAxis(item))
                    {
                        zItems.Add(item);
                        continue;
                    }

                    verticalExtraItems.Add(item);
                }

                zItems.AddRange(verticalExtraItems);
                axisButtonLayout.ColumnCount = 1;
                axisButtonLayout.RowCount = 1;
                axisButtonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                axisButtonLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                axisButtonLayout.Controls.Add(CreateStageSlotLayout(cameraXItem, yItem, tItem, zItems, horizontalExtraItems), 0, 0);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void BuildPickerTabbedButtons()
        {
            try
            {
                // 항목 순서 계약: [0]=X, [1]=Y, 이후 (T,Z) 쌍이 피커 순서대로 (T0,Z0,T1,Z1,...)
                JogAxisItem xItem = _items.Count > 0 ? _items[0] : null;
                JogAxisItem yItem = _items.Count > 1 ? _items[1] : null;

                TableLayoutPanel container = new TableLayoutPanel();
                container.ColumnCount = 1;
                container.RowCount = 2;
                container.Dock = DockStyle.Fill;
                container.Margin = new Padding(0);
                container.Padding = new Padding(0);
                container.BackColor = Color.Transparent;
                container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                container.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                container.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

                container.Controls.Add(CreateXyPad(xItem, yItem), 0, 0);

                TabControl tabs = new TabControl();
                tabs.Dock = DockStyle.Fill;
                tabs.Margin = new Padding(0);
                tabs.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
                tabs.SizeMode = TabSizeMode.Fixed;
                tabs.ItemSize = new Size(60, 26);

                int pickerCount = Math.Max(0, (_items.Count - 2)) / 2;
                for (int p = 0; p < pickerCount; p++)
                {
                    int baseIdx = 2 + p * 2;
                    JogAxisItem tItem = _items.Count > baseIdx ? _items[baseIdx] : null;
                    JogAxisItem zItem = _items.Count > baseIdx + 1 ? _items[baseIdx + 1] : null;

                    TabPage page = new TabPage("PICKER " + (p + 1));
                    page.BackColor = Color.FromArgb(207, 211, 216);
                    page.UseVisualStyleBackColor = false;
                    page.Margin = new Padding(0);

                    TableLayoutPanel tzHost = new TableLayoutPanel();
                    tzHost.Dock = DockStyle.Fill;
                    tzHost.ColumnCount = 1;
                    tzHost.RowCount = 1;
                    tzHost.Margin = new Padding(0);
                    tzHost.BackColor = Color.Transparent;
                    tzHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                    tzHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                    tzHost.Controls.Add(CreateTzPad(tItem, zItem, "PICKER T" + (p + 1), "PICKER Z" + (p + 1)), 0, 0);

                    page.Controls.Add(tzHost);
                    tabs.TabPages.Add(page);
                }

                int tabCount = tabs.TabPages.Count;
                if (tabCount > 0)
                {
                    bool[] resizing = { false };
                    EventHandler sizeTabs = delegate
                    {
                        if (resizing[0] || !tabs.IsHandleCreated)
                            return;

                        int avail = tabs.ClientSize.Width;
                        if (avail <= tabCount + 4)
                            return;

                        int w = Math.Max(16, (avail - 4) / tabCount);
                        if (tabs.ItemSize.Width == w)
                            return;

                        resizing[0] = true;
                        try { tabs.ItemSize = new Size(w, 26); }
                        catch { }
                        finally { resizing[0] = false; }
                    };
                    tabs.SizeChanged += sizeTabs;
                    tabs.HandleCreated += sizeTabs;
                }

                container.Controls.Add(tabs, 0, 1);

                axisButtonLayout.ColumnCount = 1;
                axisButtonLayout.RowCount = 1;
                axisButtonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                axisButtonLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                axisButtonLayout.Controls.Add(container, 0, 0);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private Control CreateXyPad(JogAxisItem xItem, JogAxisItem yItem)
        {
            try
            {
                TableLayoutPanel layout = CreatePadGrid();
                Control labels = CreateAxisLabelStrip(new[] { xItem != null ? xItem.AxisName : "X", yItem != null ? yItem.AxisName : "Y" });
                layout.Controls.Add(labels, 0, 0);
                layout.SetColumnSpan(labels, 3);
                layout.Controls.Add(CreateBlankCell(), 0, 1);
                layout.Controls.Add(CreateStagePadButton(yItem, 1, yItem != null ? yItem.PlusText : "Y+"), 1, 1);
                layout.Controls.Add(CreateBlankCell(), 2, 1);
                layout.Controls.Add(CreateStagePadButton(xItem, -1, xItem != null ? xItem.MinusText : "X-"), 0, 2);
                layout.Controls.Add(CreateStopAllButton(), 1, 2);
                layout.Controls.Add(CreateStagePadButton(xItem, 1, xItem != null ? xItem.PlusText : "X+"), 2, 2);
                layout.Controls.Add(CreateBlankCell(), 0, 3);
                layout.Controls.Add(CreateStagePadButton(yItem, -1, yItem != null ? yItem.MinusText : "Y-"), 1, 3);
                layout.Controls.Add(CreateBlankCell(), 2, 3);
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

        private Control CreateTzPad(JogAxisItem tItem, JogAxisItem zItem, string tLabel, string zLabel)
        {
            try
            {
                TableLayoutPanel layout = CreatePadGrid();
                Control labels = CreateAxisLabelStrip(new[] { tLabel, zLabel });
                layout.Controls.Add(labels, 0, 0);
                layout.SetColumnSpan(labels, 3);
                layout.Controls.Add(CreateStagePadButton(tItem, -1, tItem != null ? tItem.MinusText : "T-"), 0, 1);
                layout.Controls.Add(CreateStagePadButton(zItem, 1, zItem != null ? zItem.PlusText : "Z+"), 1, 1);
                layout.Controls.Add(CreateStagePadButton(tItem, 1, tItem != null ? tItem.PlusText : "T+"), 2, 1);
                layout.Controls.Add(CreateBlankCell(), 0, 2);
                layout.Controls.Add(CreateStopAllButton(), 1, 2);
                layout.Controls.Add(CreateBlankCell(), 2, 2);
                layout.Controls.Add(CreateBlankCell(), 0, 3);
                layout.Controls.Add(CreateStagePadButton(zItem, -1, zItem != null ? zItem.MinusText : "Z-"), 1, 3);
                layout.Controls.Add(CreateBlankCell(), 2, 3);
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

        private TableLayoutPanel CreatePadGrid()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.ColumnCount = 3;
            layout.RowCount = 4;
            layout.Dock = DockStyle.None;
            layout.Anchor = AnchorStyles.None;
            layout.Width = StageJogButtonWidth * 3;
            layout.Height = StageAxisColumnHeight;
            layout.Margin = new Padding(0);
            layout.Padding = new Padding(0);
            layout.BackColor = Color.Transparent;
            for (int i = 0; i < 3; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StageJogButtonWidth));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageAxisLabelHeight));
            for (int i = 0; i < 3; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageJogButtonHeight));
            return layout;
        }

        private void BuildInputStagePadButtons()
        {
            try
            {
                JogAxisItem cameraX = FirstAxisByName("CameraX");
                JogAxisItem stageY = FirstAxisByName("StageY");
                JogAxisItem stageT = FirstAxisByName("StageT");
                JogAxisItem expanderZ = FirstAxisByName("ExpanderZ");
                JogAxisItem needleX = FirstAxisByName("NeedleX");
                JogAxisItem needleZ = FirstAxisByName("NeedleZ");
                JogAxisItem ejectPinZ = FirstAxisByName("EjectPinZ");

                TableLayoutPanel container = new TableLayoutPanel();
                container.ColumnCount = 1;
                container.RowCount = 2;
                container.Dock = DockStyle.Fill;
                container.Margin = new Padding(0);
                container.Padding = new Padding(0);
                container.BackColor = Color.Transparent;
                container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                container.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                container.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

                container.Controls.Add(CreateStageTopSection(cameraX, expanderZ, stageY, stageT), 0, 0);
                container.Controls.Add(CreateStageBottomSection(needleX, needleZ, ejectPinZ), 0, 1);

                axisButtonLayout.ColumnCount = 1;
                axisButtonLayout.RowCount = 1;
                axisButtonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                axisButtonLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                axisButtonLayout.Controls.Add(container, 0, 0);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private const int InputPadBtnW = 60;
        private const int InputPadBtnH = 48;
        private const int InputVPadW = 80;

        private Control SizedJogButton(JogAxisItem item, int direction, string text)
        {
            Control b = CreateStagePadButton(item, direction, text);
            b.Width = InputPadBtnW;
            b.Height = InputPadBtnH;
            return b;
        }

        private Control SizedStopButton()
        {
            Control s = CreateStopAllButton();
            s.Width = InputPadBtnW;
            s.Height = InputPadBtnH;
            return s;
        }

        // 가로 1축 패드: [라벨] / [-][STOP][+]
        private Control CreateHPad(JogAxisItem item, string label)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.ColumnCount = 3;
            layout.RowCount = 2;
            layout.Dock = DockStyle.None;
            layout.Anchor = AnchorStyles.None;
            layout.Width = InputPadBtnW * 3;
            layout.Height = StageAxisLabelHeight + InputPadBtnH;
            layout.Margin = new Padding(0);
            layout.Padding = new Padding(0);
            layout.BackColor = Color.Transparent;
            for (int i = 0; i < 3; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, InputPadBtnW));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageAxisLabelHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, InputPadBtnH));

            Label header = CreateStageAxisLabel(label);
            layout.Controls.Add(header, 0, 0);
            layout.SetColumnSpan(header, 3);
            layout.Controls.Add(SizedJogButton(item, -1, item != null ? item.MinusText : "X-"), 0, 1);
            layout.Controls.Add(SizedStopButton(), 1, 1);
            layout.Controls.Add(SizedJogButton(item, 1, item != null ? item.PlusText : "X+"), 2, 1);
            return layout;
        }

        // 세로 1축 패드: [라벨(넓게)] / [+] / [STOP] / [-]
        private Control CreateVPad(JogAxisItem item, string label)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.Dock = DockStyle.None;
            layout.Anchor = AnchorStyles.None;
            layout.Width = InputVPadW;
            layout.Height = StageAxisLabelHeight + InputPadBtnH * 3;
            layout.Margin = new Padding(0);
            layout.Padding = new Padding(0);
            layout.BackColor = Color.Transparent;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, InputVPadW));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageAxisLabelHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, InputPadBtnH * 3));

            // 라벨은 넓게(InputVPadW), 버튼 묶음은 기존 폭(InputPadBtnW)으로 가운데 정렬
            layout.Controls.Add(CreateStageAxisLabel(label), 0, 0);

            TableLayoutPanel buttons = new TableLayoutPanel();
            buttons.ColumnCount = 1;
            buttons.RowCount = 3;
            buttons.Dock = DockStyle.None;
            buttons.Anchor = AnchorStyles.None;
            buttons.Width = InputPadBtnW;
            buttons.Height = InputPadBtnH * 3;
            buttons.Margin = new Padding(0);
            buttons.Padding = new Padding(0);
            buttons.BackColor = Color.Transparent;
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, InputPadBtnW));
            for (int i = 0; i < 3; i++)
                buttons.RowStyles.Add(new RowStyle(SizeType.Absolute, InputPadBtnH));
            buttons.Controls.Add(SizedJogButton(item, 1, item != null ? item.PlusText : "Z+"), 0, 0);
            buttons.Controls.Add(SizedStopButton(), 0, 1);
            buttons.Controls.Add(SizedJogButton(item, -1, item != null ? item.MinusText : "Z-"), 0, 2);
            layout.Controls.Add(buttons, 0, 1);
            return layout;
        }

        // Y(세로) + T(상단 모서리) 크로스 패드
        private Control CreateYtPad(JogAxisItem yItem, JogAxisItem tItem)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.ColumnCount = 3;
            layout.RowCount = 4;
            layout.Dock = DockStyle.None;
            layout.Anchor = AnchorStyles.None;
            layout.Width = InputPadBtnW * 3;
            layout.Height = StageAxisLabelHeight + InputPadBtnH * 3;
            layout.Margin = new Padding(0);
            layout.Padding = new Padding(0);
            layout.BackColor = Color.Transparent;
            for (int i = 0; i < 3; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, InputPadBtnW));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageAxisLabelHeight));
            for (int i = 0; i < 3; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, InputPadBtnH));

            Control labels = CreateAxisLabelStrip(new[] { "Stage Y", "Stage T" });
            layout.Controls.Add(labels, 0, 0);
            layout.SetColumnSpan(labels, 3);
            layout.Controls.Add(SizedJogButton(tItem, -1, tItem != null ? tItem.MinusText : "T-"), 0, 1);
            layout.Controls.Add(SizedJogButton(yItem, 1, yItem != null ? yItem.PlusText : "Y+"), 1, 1);
            layout.Controls.Add(SizedJogButton(tItem, 1, tItem != null ? tItem.PlusText : "T+"), 2, 1);
            layout.Controls.Add(CreateBlankCell(), 0, 2);
            layout.Controls.Add(SizedStopButton(), 1, 2);
            layout.Controls.Add(CreateBlankCell(), 2, 2);
            layout.Controls.Add(CreateBlankCell(), 0, 3);
            layout.Controls.Add(SizedJogButton(yItem, -1, yItem != null ? yItem.MinusText : "Y-"), 1, 3);
            layout.Controls.Add(CreateBlankCell(), 2, 3);
            return layout;
        }

        // 상단: 좌측(Camera X 가로 + Expander Z 세로) | 우측(Stage Y/T 크로스)
        private Control CreateStageTopSection(JogAxisItem cameraX, JogAxisItem expanderZ, JogAxisItem stageY, JogAxisItem stageT)
        {
            int padWidth = InputPadBtnW * 3;
            int camHeight = StageAxisLabelHeight + InputPadBtnH;
            int gapV = 16;
            int expHeight = StageAxisLabelHeight + InputPadBtnH * 3;

            TableLayoutPanel leftStack = new TableLayoutPanel();
            leftStack.ColumnCount = 1;
            leftStack.RowCount = 3;
            leftStack.Dock = DockStyle.None;
            leftStack.Anchor = AnchorStyles.None;
            leftStack.Width = padWidth;
            leftStack.Height = camHeight + gapV + expHeight;
            leftStack.Margin = new Padding(0);
            leftStack.Padding = new Padding(0);
            leftStack.BackColor = Color.Transparent;
            leftStack.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, padWidth));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, camHeight));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, gapV));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, expHeight));
            leftStack.Controls.Add(CreateHPad(cameraX, "Camera X"), 0, 0);
            leftStack.Controls.Add(CreateBlankCell(), 0, 1);
            leftStack.Controls.Add(CreateVPad(expanderZ, "Expander Z"), 0, 2);

            int gapH = 16;
            TableLayoutPanel section = new TableLayoutPanel();
            section.ColumnCount = 3;
            section.RowCount = 1;
            section.Dock = DockStyle.None;
            section.Anchor = AnchorStyles.None;
            section.Width = padWidth + gapH + padWidth;
            section.Height = leftStack.Height;
            section.Margin = new Padding(0);
            section.Padding = new Padding(0);
            section.BackColor = Color.Transparent;
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, padWidth));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, gapH));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, padWidth));
            section.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            section.Controls.Add(leftStack, 0, 0);

            Control cross = CreateYtPad(stageY, stageT);
            cross.Anchor = AnchorStyles.Top;   // Camera X 라벨과 Stage Y/T 라벨을 동일선상(상단)에 맞춤
            section.Controls.Add(cross, 2, 0);
            return section;
        }

        // 하단: Needle X(가로) | Needle Z(세로) | EjectPin Z(세로)
        private Control CreateStageBottomSection(JogAxisItem needleX, JogAxisItem needleZ, JogAxisItem ejectPinZ)
        {
            int padWidth = InputPadBtnW * 3;
            int gap = 16;
            TableLayoutPanel section = new TableLayoutPanel();
            section.ColumnCount = 5;
            section.RowCount = 1;
            section.Dock = DockStyle.None;
            section.Anchor = AnchorStyles.None;
            section.Width = padWidth + gap + InputVPadW + gap + InputVPadW;
            section.Height = StageAxisLabelHeight + InputPadBtnH * 3;
            section.Margin = new Padding(0);
            section.Padding = new Padding(0);
            section.BackColor = Color.Transparent;
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, padWidth));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, gap));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, InputVPadW));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, gap));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, InputVPadW));
            section.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Control nxPad = CreateHPad(needleX, "Needle X");
            nxPad.Anchor = AnchorStyles.Top;   // Needle X 라벨을 Needle Z / EjectPin Z 라벨과 동일선상(상단)에 맞춤
            section.Controls.Add(nxPad, 0, 0);
            section.Controls.Add(CreateVPad(needleZ, "Needle Z"), 2, 0);
            section.Controls.Add(CreateVPad(ejectPinZ, "EjectPin Z"), 4, 0);
            return section;
        }

        private Control CreateAxisLabelStrip(string[] names)
        {
            try
            {
                TableLayoutPanel strip = new TableLayoutPanel();
                strip.ColumnCount = names.Length;
                strip.RowCount = 1;
                strip.Dock = DockStyle.Fill;
                strip.Margin = new Padding(0);
                strip.Padding = new Padding(0);
                strip.BackColor = Color.Transparent;
                for (int i = 0; i < names.Length; i++)
                    strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / names.Length));
                strip.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                for (int i = 0; i < names.Length; i++)
                    strip.Controls.Add(CreateStageAxisLabel(names[i]), i, 0);
                return strip;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void BuildOutputStagePadButtons()
        {
            try
            {
                JogAxisItem goodY = FirstAxisByName("GOOD Y");
                JogAxisItem goodZ = FirstAxisByName("GOOD Z");
                JogAxisItem ngY = FirstAxisByName("NG Y");
                JogAxisItem visionX = FirstAxisByName("VISION X");

                TableLayoutPanel container = new TableLayoutPanel();
                container.ColumnCount = 1;
                container.RowCount = 2;
                container.Dock = DockStyle.Fill;
                container.Margin = new Padding(0);
                container.Padding = new Padding(0);
                container.BackColor = Color.Transparent;
                container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                container.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                container.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

                container.Controls.Add(CreateOutputTopPads(goodY, goodZ), 0, 0);
                container.Controls.Add(CreateOutputBottomPads(visionX, ngY), 0, 1);

                axisButtonLayout.ColumnCount = 1;
                axisButtonLayout.RowCount = 1;
                axisButtonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                axisButtonLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                axisButtonLayout.Controls.Add(container, 0, 0);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        // 하단: Camera X(가로 패드) | NG Y(세로 패드, 넓은 라벨)
        private Control CreateOutputBottomPads(JogAxisItem visionX, JogAxisItem ngY)
        {
            try
            {
                int gap = 40;
                TableLayoutPanel section = new TableLayoutPanel();
                section.ColumnCount = 3;
                section.RowCount = 1;
                section.Dock = DockStyle.None;
                section.Anchor = AnchorStyles.None;
                section.Width = StageJogButtonWidth * 3 + gap + OutputColumnWidth;
                section.Height = OutputColumnLabelHeight + OutputColumnLabelGap + StageJogButtonHeight * 3;
                section.Margin = new Padding(0);
                section.Padding = new Padding(0);
                section.BackColor = Color.Transparent;
                section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StageJogButtonWidth * 3));
                section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, gap));
                section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, OutputColumnWidth));
                section.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                Control cam = CreateOutputHPad(visionX, "Camera X");
                cam.Anchor = AnchorStyles.Top;   // Camera X 라벨을 Ng Y 라벨과 동일선상(상단)에 맞춤
                section.Controls.Add(cam, 0, 0);
                section.Controls.Add(CreateOutputColumn(ngY, "Ng Y"), 2, 0);
                return section;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        // 가로 1축 패드: [라벨(버튼 폭 전체)] / [-][STOP][+]
        private Control CreateOutputHPad(JogAxisItem item, string label)
        {
            try
            {
                TableLayoutPanel layout = new TableLayoutPanel();
                layout.ColumnCount = 3;
                layout.RowCount = 2;
                layout.Dock = DockStyle.None;
                layout.Anchor = AnchorStyles.None;
                layout.Width = StageJogButtonWidth * 3;
                layout.Height = StageAxisLabelHeight + StageJogButtonHeight;
                layout.Margin = new Padding(0);
                layout.Padding = new Padding(0);
                layout.BackColor = Color.Transparent;
                for (int i = 0; i < 3; i++)
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StageJogButtonWidth));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageAxisLabelHeight));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageJogButtonHeight));

                Label header = CreateStageAxisLabel(label);
                layout.Controls.Add(header, 0, 0);
                layout.SetColumnSpan(header, 3);
                layout.Controls.Add(CreateStagePadButton(item, -1, item != null ? item.MinusText : "X-"), 0, 1);
                layout.Controls.Add(CreateStopAllButton(), 1, 1);
                layout.Controls.Add(CreateStagePadButton(item, 1, item != null ? item.PlusText : "X+"), 2, 1);
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

        private Control CreateOutputTopPads(JogAxisItem firstItem, JogAxisItem secondItem)
        {
            try
            {
                int gapColumn = 40;
                TableLayoutPanel layout = new TableLayoutPanel();
                layout.ColumnCount = 3;
                layout.RowCount = 1;
                layout.Dock = DockStyle.None;
                layout.Anchor = AnchorStyles.None;
                layout.Width = OutputColumnWidth * 2 + gapColumn;
                layout.Height = OutputColumnLabelHeight + OutputColumnLabelGap + StageJogButtonHeight * 3;
                layout.Margin = new Padding(0);
                layout.Padding = new Padding(0);
                layout.BackColor = Color.Transparent;
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, OutputColumnWidth));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, gapColumn));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, OutputColumnWidth));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.Controls.Add(CreateOutputColumn(firstItem, "Good Y"), 0, 0);
                layout.Controls.Add(CreateOutputColumn(secondItem, "Good Z"), 2, 0);
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

        private const int OutputColumnLabelHeight = StageAxisLabelHeight;
        private const int OutputColumnLabelGap = 0;
        private const int OutputColumnWidth = StageJogButtonWidth + 16;

        private Control CreateOutputColumn(JogAxisItem item, string label)
        {
            try
            {
                TableLayoutPanel layout = new TableLayoutPanel();
                layout.ColumnCount = 1;
                layout.RowCount = 3;
                layout.Dock = DockStyle.None;
                layout.Anchor = AnchorStyles.None;
                layout.Width = OutputColumnWidth;
                layout.Height = OutputColumnLabelHeight + OutputColumnLabelGap + StageJogButtonHeight * 3;
                layout.Margin = new Padding(0);
                layout.Padding = new Padding(0);
                layout.BackColor = Color.Transparent;
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, OutputColumnWidth));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, OutputColumnLabelHeight));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, OutputColumnLabelGap));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageJogButtonHeight * 3));

                // 좌우로 넓힌 라벨 (높이/폰트는 하단 Camera X 라벨과 동일)
                layout.Controls.Add(CreateStageAxisLabel(label), 0, 0);

                layout.Controls.Add(CreateBlankCell(), 0, 1);

                // 버튼 묶음(폭은 기존 그대로, 가운데 정렬)
                TableLayoutPanel buttons = new TableLayoutPanel();
                buttons.ColumnCount = 1;
                buttons.RowCount = 3;
                buttons.Dock = DockStyle.None;
                buttons.Anchor = AnchorStyles.None;
                buttons.Width = StageJogButtonWidth;
                buttons.Height = StageJogButtonHeight * 3;
                buttons.Margin = new Padding(0);
                buttons.Padding = new Padding(0);
                buttons.BackColor = Color.Transparent;
                buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StageJogButtonWidth));
                for (int i = 0; i < 3; i++)
                    buttons.RowStyles.Add(new RowStyle(SizeType.Absolute, StageJogButtonHeight));
                buttons.Controls.Add(CreateStagePadButton(item, 1, item != null ? item.PlusText : "+"), 0, 0);
                buttons.Controls.Add(CreateStopAllButton(), 0, 1);
                buttons.Controls.Add(CreateStagePadButton(item, -1, item != null ? item.MinusText : "-"), 0, 2);
                layout.Controls.Add(buttons, 0, 2);

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

        private Control CreateStageSlotLayout(JogAxisItem xItem, JogAxisItem yItem, JogAxisItem tItem, List<JogAxisItem> verticalItems, List<JogAxisItem> horizontalItems)
        {
            try
            {
                int verticalCount = verticalItems != null ? verticalItems.Count : 0;
                int verticalRows = Math.Max(1, (int)Math.Ceiling(verticalCount / 5.0));
                int horizontalCount = horizontalItems != null ? horizontalItems.Count : 0;
                int contentRows = 1 + verticalRows + horizontalCount;
                int layoutHeight = StageAxisColumnHeight + StageAxisColumnGap
                    + verticalRows * StageAxisColumnHeight
                    + Math.Max(0, verticalRows - 1) * StageAxisColumnGap
                    + horizontalCount * (StageAxisColumnGap + StageAxisLabelHeight + StageJogButtonHeight);
                TableLayoutPanel outer = new TableLayoutPanel();
                TableLayoutPanel layout = new TableLayoutPanel();

                outer.ColumnCount = 3;
                outer.RowCount = 3;
                outer.Dock = DockStyle.Fill;
                outer.Margin = new Padding(1);
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, GetStageSlotLayoutWidth()));
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                outer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                outer.RowStyles.Add(new RowStyle(SizeType.Absolute, layoutHeight));
                outer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

                layout.ColumnCount = 9;
                layout.RowCount = contentRows + Math.Max(0, contentRows - 1);
                layout.Dock = DockStyle.Fill;
                layout.Margin = new Padding(0);
                layout.Padding = new Padding(0);
                layout.BackColor = Color.Transparent;

                for (int column = 0; column < 9; column++)
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, column % 2 == 0 ? StageAxisColumnWidth : StageAxisColumnGap));

                int row = 0;
                AddStageSlotRow(layout, row, StageAxisColumnHeight);
                Control cross = CreateStagePad(xItem, yItem, tItem);
                layout.Controls.Add(cross, 0, row);
                layout.SetColumnSpan(cross, 5);

                row++;
                AddStageGapRow(layout, row);
                row++;

                for (int verticalRow = 0; verticalRow < verticalRows; verticalRow++)
                {
                    AddStageSlotRow(layout, row, StageAxisColumnHeight);
                    int remaining = Math.Max(0, verticalCount - verticalRow * 5);
                    int countInRow = Math.Min(5, remaining);
                    int startColumn = GetCenteredStageColumn(countInRow);
                    for (int index = 0; index < countInRow; index++)
                    {
                        JogAxisItem item = verticalItems[verticalRow * 5 + index];
                        layout.Controls.Add(CreateFixedAxisColumn(item, index == countInRow - 1), startColumn + index * 2, row);
                    }

                    row++;
                    if (verticalRow < verticalRows - 1 || horizontalCount > 0)
                    {
                        AddStageGapRow(layout, row);
                        row++;
                    }
                }

                for (int index = 0; index < horizontalCount; index++)
                {
                    AddStageSlotRow(layout, row, StageAxisLabelHeight + StageJogButtonHeight);
                    Control horizontal = CreateFixedAxisColumn(horizontalItems[index], true);
                    layout.Controls.Add(horizontal, 2, row);
                    layout.SetColumnSpan(horizontal, 5);
                    row++;
                    if (index < horizontalCount - 1)
                    {
                        AddStageGapRow(layout, row);
                        row++;
                    }
                }

                outer.Controls.Add(layout, 1, 1);
                return outer;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static int GetStageSlotLayoutWidth()
        {
            try
            {
                return StageAxisColumnWidth * 5 + StageAxisColumnGap * 4;
            }
            catch
            {
                return StageAxisColumnWidth * 5;
            }
            finally
            {
            }
        }

        private static int GetCenteredStageColumn(int itemCount)
        {
            try
            {
                int count = Math.Max(1, Math.Min(5, itemCount));
                int startSlot = (5 - count) / 2;
                return startSlot * 2;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
        }

        private static void AddStageSlotRow(TableLayoutPanel layout, int rowIndex, int height)
        {
            try
            {
                if (layout == null)
                    return;

                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static void AddStageGapRow(TableLayoutPanel layout, int rowIndex)
        {
            try
            {
                AddStageSlotRow(layout, rowIndex, StageAxisColumnGap);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private Control CreateStageTopGroup(JogAxisItem xItem, JogAxisItem yItem, JogAxisItem tItem, JogAxisItem cameraZItem)
        {
            try
            {
                int padWidth = StageJogButtonWidth * 3;
                int totalWidth = padWidth + StageAxisColumnGap + (cameraZItem == null ? 0 : StageAxisColumnWidth);
                TableLayoutPanel outer = new TableLayoutPanel();
                TableLayoutPanel layout = new TableLayoutPanel();

                outer.ColumnCount = 3;
                outer.RowCount = 3;
                outer.Dock = DockStyle.Fill;
                outer.Margin = new Padding(1);
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, totalWidth));
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                outer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                outer.RowStyles.Add(new RowStyle(SizeType.Absolute, StageAxisColumnHeight));
                outer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

                layout.ColumnCount = cameraZItem == null ? 1 : 2;
                layout.RowCount = 1;
                layout.Dock = DockStyle.Fill;
                layout.Margin = new Padding(0);
                layout.Padding = new Padding(0);
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, padWidth + (cameraZItem == null ? 0 : StageAxisColumnGap)));
                if (cameraZItem != null)
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StageAxisColumnWidth));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageAxisColumnHeight));

                Control stagePad = CreateStagePad(xItem, yItem, tItem);
                stagePad.Margin = new Padding(0);
                layout.Controls.Add(stagePad, 0, 0);

                if (cameraZItem != null)
                    layout.Controls.Add(CreateFixedAxisColumn(cameraZItem, true), 1, 0);

                outer.Controls.Add(layout, 1, 1);
                return outer;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private Control CreateCenteredAxisGroup(IEnumerable<JogAxisItem> items)
        {
            try
            {
                return CreateCenteredAxisGrid(items, 3);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private Control CreateCenteredAxisGrid(IEnumerable<JogAxisItem> items, int preferredColumns)
        {
            try
            {
                List<JogAxisItem> axes = new List<JogAxisItem>();
                if (items != null)
                {
                    foreach (JogAxisItem item in items)
                    {
                        if (item != null)
                            axes.Add(item);
                    }
                }

                if (axes.Count == 0)
                    return CreateBlankCell();

                int columnCount = Math.Max(1, Math.Min(Math.Max(1, preferredColumns), axes.Count));
                int rowCount = (int)Math.Ceiling(axes.Count / (double)columnCount);
                int totalWidth = columnCount * StageAxisColumnWidth + Math.Max(0, columnCount - 1) * StageAxisColumnGap;
                int totalHeight = rowCount * StageAxisColumnHeight + Math.Max(0, rowCount - 1) * StageAxisColumnGap;
                TableLayoutPanel outer = new TableLayoutPanel();
                TableLayoutPanel grid = new TableLayoutPanel();

                outer.ColumnCount = 3;
                outer.RowCount = 3;
                outer.Dock = DockStyle.Fill;
                outer.Margin = new Padding(1);
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, totalWidth + StageAxisColumnGap));
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                outer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                outer.RowStyles.Add(new RowStyle(SizeType.Absolute, totalHeight + StageAxisColumnGap));
                outer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

                grid.ColumnCount = columnCount;
                grid.RowCount = rowCount;
                grid.Dock = DockStyle.Fill;
                grid.Margin = new Padding(0);
                grid.Padding = new Padding(0);
                grid.BackColor = Color.Transparent;

                for (int column = 0; column < columnCount; column++)
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StageAxisColumnWidth + (column == columnCount - 1 ? 0 : StageAxisColumnGap)));
                for (int row = 0; row < rowCount; row++)
                    grid.RowStyles.Add(new RowStyle(SizeType.Absolute, StageAxisColumnHeight + (row == rowCount - 1 ? 0 : StageAxisColumnGap)));

                for (int index = 0; index < axes.Count; index++)
                {
                    int column = index % columnCount;
                    int row = index / columnCount;
                    Control axisColumn = CreateFixedAxisColumn(axes[index], column == columnCount - 1);
                    axisColumn.Margin = new Padding(0);
                    grid.Controls.Add(axisColumn, column, row);
                }

                outer.Controls.Add(grid, 1, 1);
                return outer;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private Control CreateCenteredAxisFlow(IEnumerable<JogAxisItem> items)
        {
            try
            {
                List<JogAxisItem> axes = new List<JogAxisItem>();
                if (items != null)
                {
                    foreach (JogAxisItem item in items)
                    {
                        if (item != null)
                            axes.Add(item);
                    }
                }

                if (axes.Count == 0)
                    return CreateBlankCell();

                // 가로 폭이 콘텐츠와 정확히 같으면 WrapContents=false FlowLayoutPanel이
                // 마지막 컬럼을 잘라내므로 약간의 여유 폭(gap)을 확보한다.
                int totalWidth = GetAxisItemRequiredWidth(axes) + StageAxisColumnGap;
                int totalHeight = GetAxisItemRequiredHeight(axes);
                TableLayoutPanel outer = new TableLayoutPanel();
                FlowLayoutPanel flow = new FlowLayoutPanel();

                outer.ColumnCount = 3;
                outer.RowCount = 3;
                outer.Dock = DockStyle.Fill;
                outer.Margin = new Padding(1);
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, totalWidth));
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                outer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                outer.RowStyles.Add(new RowStyle(SizeType.Absolute, totalHeight));
                outer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

                flow.Dock = DockStyle.Fill;
                flow.Margin = new Padding(0);
                flow.Padding = new Padding(0);
                flow.WrapContents = false;
                flow.FlowDirection = FlowDirection.LeftToRight;
                flow.BackColor = Color.Transparent;

                for (int index = 0; index < axes.Count; index++)
                {
                    Control axisColumn = CreateFixedAxisColumn(axes[index], index == axes.Count - 1);
                    axisColumn.Margin = new Padding(0, 0, index == axes.Count - 1 ? 0 : StageAxisColumnGap, 0);
                    flow.Controls.Add(axisColumn);
                }

                outer.Controls.Add(flow, 1, 1);
                return outer;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private Control CreateFixedAxisColumn(JogAxisItem item, bool isLast)
        {
            try
            {
                Panel host = new Panel();
                Control axisColumn = CreateAxisColumn(item);

                host.Width = GetControlKind(item) == JogAxisControlKind.Horizontal ? StageJogButtonWidth * 3 : StageAxisColumnWidth;
                host.Height = GetControlKind(item) == JogAxisControlKind.Horizontal ? StageAxisLabelHeight + StageJogButtonHeight : StageAxisColumnHeight;
                host.Margin = new Padding(0);
                host.BackColor = Color.Transparent;

                axisColumn.Dock = DockStyle.Fill;
                axisColumn.Margin = new Padding(0);
                host.Controls.Add(axisColumn);
                return host;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static bool IsCameraAxis(JogAxisItem item)
        {
            try
            {
                if (item == null)
                    return false;

                string name = item.AxisName ?? string.Empty;
                return name.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsStageZGroupAxis(JogAxisItem item)
        {
            try
            {
                if (item == null)
                    return false;

                string name = item.AxisName ?? string.Empty;
                return name.IndexOf("Expander", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("NeedleZ", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("EjectPin", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private Control CreateStagePadWithLabels(JogAxisItem xItem, JogAxisItem yItem, JogAxisItem tItem)
        {
            try
            {
                TableLayoutPanel layout = new TableLayoutPanel();
                layout.ColumnCount = 1;
                layout.RowCount = 2;
                layout.Dock = DockStyle.Fill;
                layout.Margin = new Padding(1);
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.Controls.Add(CreateStageAxisLabels("Needle X", "Stage Y", "Stage T"), 0, 0);
                layout.Controls.Add(CreateStagePad(xItem, yItem, tItem), 0, 1);
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

        private Control CreateStageAxisLabels(string xText, string yText, string tText)
        {
            try
            {
                TableLayoutPanel layout = new TableLayoutPanel();
                layout.ColumnCount = 3;
                layout.RowCount = 1;
                layout.Dock = DockStyle.Fill;
                layout.Margin = new Padding(1);
                for (int i = 0; i < 3; i++)
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));

                layout.Controls.Add(CreateStageAxisLabel(xText), 0, 0);
                layout.Controls.Add(CreateStageAxisLabel(yText), 1, 0);
                layout.Controls.Add(CreateStageAxisLabel(tText), 2, 0);
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

        private static Label CreateStageAxisLabel(string text)
        {
            try
            {
                Label label = new Label();
                label.BackColor = Color.White;
                label.BorderStyle = BorderStyle.FixedSingle;
                label.Dock = DockStyle.Fill;
                label.Font = new Font("Malgun Gothic", 7.0F, FontStyle.Bold);
                label.Margin = new Padding(0, 0, 1, 1);
                label.Text = text;
                label.TextAlign = ContentAlignment.MiddleCenter;
                return label;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private JogAxisItem FirstAxisByName(string axisName)
        {
            try
            {
                foreach (JogAxisItem item in _items)
                {
                    if (item == null)
                        continue;

                    if (string.Equals(item.AxisName, axisName, StringComparison.OrdinalIgnoreCase))
                        return item;
                }

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private JogAxisItem FirstAxisByText(string axisLetter)
        {
            try
            {
                foreach (JogAxisItem item in _items)
                {
                    if (item == null)
                        continue;

                    if (IsAxisTextMatch(item, axisLetter))
                        return item;
                }

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private static bool IsAxisTextMatch(JogAxisItem item, string axisLetter)
        {
            try
            {
                if (item == null || string.IsNullOrWhiteSpace(axisLetter))
                    return false;

                string letter = axisLetter.ToUpperInvariant();
                string plusText = (item.PlusText ?? string.Empty).ToUpperInvariant();
                string minusText = (item.MinusText ?? string.Empty).ToUpperInvariant();
                if (plusText == letter + "+" || plusText == "+" + letter || minusText == letter + "-" || minusText == "-" + letter)
                    return true;

                string axisName = (item.AxisName ?? string.Empty).Trim().ToUpperInvariant();
                return axisName.EndsWith(letter, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private Control CreateStagePad(JogAxisItem xItem, JogAxisItem yItem, JogAxisItem tItem)
        {
            try
            {
                TableLayoutPanel layout = new TableLayoutPanel();
                layout.ColumnCount = 3;
                layout.RowCount = 4;
                layout.Dock = DockStyle.None;
                layout.Width = StageJogButtonWidth * 3;
                layout.Height = StageAxisColumnHeight;
                layout.Anchor = AnchorStyles.None;
                layout.Margin = new Padding(0);
                for (int i = 0; i < 3; i++)
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StageJogButtonWidth));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageAxisLabelHeight));
                for (int i = 0; i < 3; i++)
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageJogButtonHeight));

                layout.Controls.Add(CreateStageAxisLabel(xItem != null ? xItem.AxisName : "X"), 0, 0);
                layout.Controls.Add(CreateStageAxisLabel(yItem != null ? yItem.AxisName : "Y"), 1, 0);
                layout.Controls.Add(CreateStageAxisLabel(tItem != null ? tItem.AxisName : "T"), 2, 0);
                layout.Controls.Add(CreateBlankCell(), 0, 1);
                layout.Controls.Add(CreateStagePadButton(yItem, 1, yItem != null ? yItem.PlusText : "Y+"), 1, 1);
                layout.Controls.Add(CreateBlankCell(), 2, 1);
                layout.Controls.Add(CreateStagePadButton(xItem, -1, xItem != null ? xItem.MinusText : "X-"), 0, 2);
                layout.Controls.Add(CreateStopAllButton(), 1, 2);
                layout.Controls.Add(CreateStagePadButton(xItem, 1, xItem != null ? xItem.PlusText : "X+"), 2, 2);
                layout.Controls.Add(CreateStagePadButton(tItem, -1, tItem != null ? tItem.MinusText : "T-"), 0, 3);
                layout.Controls.Add(CreateStagePadButton(yItem, -1, yItem != null ? yItem.MinusText : "Y-"), 1, 3);
                layout.Controls.Add(CreateStagePadButton(tItem, 1, tItem != null ? tItem.PlusText : "T+"), 2, 3);
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

        private Control CreateStageSideAxes(List<JogAxisItem> sideItems)
        {
            try
            {
                TableLayoutPanel layout = new TableLayoutPanel();
                int count = Math.Max(1, sideItems != null ? sideItems.Count : 0);
                bool wrapRows = count > 3;
                int rowCount = wrapRows ? 2 : 1;
                int columnCount = wrapRows ? (int)Math.Ceiling(count / 2.0) : count;
                layout.ColumnCount = columnCount;
                layout.RowCount = rowCount;
                layout.Dock = DockStyle.Fill;
                layout.Margin = new Padding(1);

                for (int index = 0; index < columnCount; index++)
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columnCount));

                for (int index = 0; index < rowCount; index++)
                    layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rowCount));

                if (sideItems == null || sideItems.Count == 0)
                {
                    layout.Controls.Add(CreateBlankCell(), 0, 0);
                    return layout;
                }

                for (int index = 0; index < sideItems.Count; index++)
                {
                    int column = wrapRows ? index % columnCount : index;
                    int row = wrapRows ? index / columnCount : 0;
                    layout.Controls.Add(CreateAxisColumn(sideItems[index]), column, row);
                }

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

        private Control CreateStagePadButton(JogAxisItem item, int direction, string text)
        {
            try
            {
                if (item == null)
                    return CreateBlankCell();

                Button button = new Button();
                ConfigureJogButton(button, text, item, direction);
                button.Dock = DockStyle.None;
                button.Width = StageJogButtonWidth;
                button.Height = StageJogButtonHeight;
                button.Margin = new Padding(0);
                return button;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private Control CreateStopAllButton()
        {
            try
            {
                Button button = new Button();
                button.BackColor = Color.FromArgb(208, 208, 208);
                button.Dock = DockStyle.None;
                button.FlatStyle = FlatStyle.Flat;
                button.Font = new Font("Malgun Gothic", 7.5F, FontStyle.Bold);
                button.ForeColor = Color.Black;
                button.Width = StageJogButtonWidth;
                button.Height = StageJogButtonHeight;
                button.Margin = new Padding(0);
                button.Text = "STOP";
                button.UseVisualStyleBackColor = false;
                button.Click += async (s, e) =>
                {
                    try
                    {
                        await StopAllAsync(true);
                    }
                    catch (Exception ex)
                    {
                        ShowJogError("Jog stop failed", ex);
                    }
                    finally
                    {
                    }
                };
                return button;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static Control CreateBlankCell()
        {
            try
            {
                Panel panel = new Panel();
                panel.Dock = DockStyle.Fill;
                panel.Margin = new Padding(1);
                return panel;
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
                layout.Margin = new Padding(1);

                axisLabel.BackColor = Color.White;
                axisLabel.BorderStyle = BorderStyle.FixedSingle;
                axisLabel.Dock = DockStyle.Fill;
                axisLabel.Font = new Font("Malgun Gothic", 7.0F, FontStyle.Bold);
                axisLabel.Margin = new Padding(0, 0, 0, 1);
                axisLabel.Text = item.AxisName;
                axisLabel.TextAlign = ContentAlignment.MiddleCenter;

                ConfigureJogButton(plusButton, item.PlusText, item, 1);
                ConfigureJogButton(stopButton, "STOP", item, 0);
                ConfigureJogButton(minusButton, item.MinusText, item, -1);

                if (GetControlKind(item) == JogAxisControlKind.Horizontal)
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

        private static JogAxisControlKind GetControlKind(JogAxisItem item)
        {
            try
            {
                if (item == null)
                    return JogAxisControlKind.Vertical;

                if (item.ControlKind != JogAxisControlKind.Auto)
                    return item.ControlKind;

                return IsHorizontalAxis(item) ? JogAxisControlKind.Horizontal : JogAxisControlKind.Vertical;
            }
            catch
            {
                return JogAxisControlKind.Vertical;
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
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StageJogButtonWidth));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StageJogButtonWidth));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StageJogButtonWidth));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageAxisLabelHeight));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageJogButtonHeight));

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
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StageAxisColumnWidth));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageAxisLabelHeight));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageJogButtonHeight));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageJogButtonHeight));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, StageJogButtonHeight));

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
                button.Font = new Font("Malgun Gothic", direction == 0 ? 7.2F : 8.2F, FontStyle.Bold);
                button.ForeColor = direction == 0 ? Color.Black : Color.White;
                button.Margin = new Padding(1);
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

        private void cboStepPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string text = cboStepPreset.SelectedItem != null ? cboStepPreset.SelectedItem.ToString() : string.Empty;
                decimal value;
                if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                    return;

                if (value <= 0)
                    value = numStepDistance.Minimum;

                if (value < numStepDistance.Minimum)
                    value = numStepDistance.Minimum;
                if (value > numStepDistance.Maximum)
                    value = numStepDistance.Maximum;

                numStepDistance.Value = value;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-AXIS", "Step preset select failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void cboStepPreset_MouseWheel(object sender, MouseEventArgs e)
        {
            try
            {
                HandledMouseEventArgs handled = e as HandledMouseEventArgs;
                if (handled != null)
                    handled.Handled = true;
            }
            catch
            {
            }
            finally
            {
            }
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
                foreach (Control control in new Control[] { numStepDistance, lblStepUnit, cboStepPreset })
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


