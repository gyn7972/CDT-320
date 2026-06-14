using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui;

namespace QMC.CDT_320.Ui.Controls
{
    public sealed partial class CassetteSlotView : UserControl
    {
        private Label[] _slotStateLabels = new Label[0];
        private int _slotCount;
        private readonly ContextMenuStrip _slotContextMenu;
        private readonly ToolStripMenuItem _moveSlotMenuItem;
        private int _contextSlotIndex = -1;

        public CassetteSlotView()
        {
            InitializeComponent();
            _slotContextMenu = new ContextMenuStrip();
            _moveSlotMenuItem = new ToolStripMenuItem("MOVE");
            _moveSlotMenuItem.Click += MoveSlotMenuItem_Click;
            _slotContextMenu.Items.Add(_moveSlotMenuItem);
            ConfigureDesignSurface();
            SetSlotCount(0);
        }

        public int SlotCount { get { return _slotCount; } }

        public event EventHandler<CassetteSlotSelectedEventArgs> SlotSelected;
        public event EventHandler<CassetteSlotSelectedEventArgs> SlotMoveRequested;

        public Color EmptyColor { get; set; } = Color.LightGray;

        public string Title
        {
            get { return titleLabel.Text; }
            set { titleLabel.Text = string.IsNullOrWhiteSpace(value) ? "CASSETTE" : value; }
        }

        private void ConfigureDesignSurface()
        {
            BackColor = UiTheme.OptionPanelBg;
            Margin = new Padding(0);
            Size = new Size(360, 480);

            rootLayout.BackColor = UiTheme.OptionPanelBg;
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Clear();
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Margin = new Padding(0);
            rootLayout.Padding = new Padding(10, 8, 10, 10);
            rootLayout.RowCount = 3;
            rootLayout.RowStyles.Clear();
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            titleLabel.BackColor = UiTheme.OptionHeaderBg;
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.Font = UiTheme.SectionFont;
            titleLabel.ForeColor = UiTheme.OptionHeaderFg;
            titleLabel.Margin = new Padding(0);
            titleLabel.Padding = new Padding(8, 0, 8, 0);
            titleLabel.Text = string.IsNullOrWhiteSpace(titleLabel.Text) ? "CASSETTE" : titleLabel.Text;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;

            summaryLabel.BackColor = Color.White;
            summaryLabel.BorderStyle = BorderStyle.FixedSingle;
            summaryLabel.Dock = DockStyle.Fill;
            summaryLabel.Font = UiTheme.ValueFont;
            summaryLabel.Margin = new Padding(0);
            summaryLabel.Padding = new Padding(8, 0, 8, 0);
            summaryLabel.Text = string.IsNullOrWhiteSpace(summaryLabel.Text) ? "SLOTS 0 / WAFER 0" : summaryLabel.Text;
            summaryLabel.TextAlign = ContentAlignment.MiddleLeft;

            scrollPanel.AutoScroll = true;
            scrollPanel.BackColor = UiTheme.OptionPanelBg;
            scrollPanel.Dock = DockStyle.Fill;
            scrollPanel.Margin = new Padding(0);
            scrollPanel.Padding = new Padding(0, 8, 0, 0);

            slotLayout.AutoSize = true;
            slotLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            slotLayout.BackColor = UiTheme.OptionPanelBg;
            slotLayout.ColumnCount = 2;
            slotLayout.ColumnStyles.Clear();
            slotLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58F));
            slotLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            slotLayout.Dock = DockStyle.Top;
            slotLayout.Margin = new Padding(0);
            slotLayout.RowCount = 1;
            slotLayout.RowStyles.Clear();
            slotLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            rootLayout.SetCellPosition(titleLabel, new TableLayoutPanelCellPosition(0, 0));
            rootLayout.SetCellPosition(summaryLabel, new TableLayoutPanelCellPosition(0, 1));
            rootLayout.SetCellPosition(scrollPanel, new TableLayoutPanelCellPosition(0, 2));
        }

        public void SetSlotCount(int slotCount)
        {
            slotCount = Math.Max(0, slotCount);
            if (_slotCount == slotCount)
                return;

            _slotCount = slotCount;
            _slotStateLabels = new Label[slotCount];

            slotLayout.SuspendLayout();
            slotLayout.Controls.Clear();
            slotLayout.RowStyles.Clear();
            slotLayout.RowCount = Math.Max(1, slotCount);

            for (int i = 0; i < slotCount; i++)
            {
                slotLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

                var no = new Label
                {
                    Cursor = Cursors.Hand,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 2, 4, 2),
                    BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0),
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = UiTheme.ValueFont,
                    Tag = i,
                    Text = (i + 1).ToString("00"),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                var state = new Label
                {
                    Cursor = Cursors.Hand,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 2, 0, 2),
                    BackColor = EmptyColor,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = UiTheme.ValueFont,
                    Padding = new Padding(8, 0, 0, 0),
                    Tag = i,
                    Text = "EMPTY",
                    TextAlign = ContentAlignment.MiddleLeft
                };

                no.Click += SlotLabel_Click;
                state.Click += SlotLabel_Click;
                no.MouseDown += SlotLabel_MouseDown;
                state.MouseDown += SlotLabel_MouseDown;

                _slotStateLabels[i] = state;
                slotLayout.Controls.Add(no, 0, i);
                slotLayout.Controls.Add(state, 1, i);
            }

            slotLayout.ResumeLayout();
            UpdateSlots(null, -1, Color.LimeGreen, Color.Cyan);
        }

        public void UpdateSlots(IReadOnlyList<bool> slots, int currentSlot, Color filledColor, Color currentColor)
        {
            int count = _slotCount;
            if (slots != null && slots.Count > count)
            {
                SetSlotCount(slots.Count);
                count = _slotCount;
            }

            int filled = 0;
            for (int i = 0; i < count; i++)
            {
                bool hasWafer = slots != null && i < slots.Count && slots[i];
                if (hasWafer)
                    filled++;

                bool current = i == currentSlot;
                Color backColor = current ? currentColor : (hasWafer ? filledColor : EmptyColor);
                string text = current ? (hasWafer ? "CURRENT / READY" : "CURRENT / EMPTY") : (hasWafer ? "READY" : "EMPTY");

                var label = _slotStateLabels[i];
                if (label.BackColor != backColor)
                    label.BackColor = backColor;
                if (label.Text != text)
                    label.Text = text;
            }

            string summary = "SLOTS " + count + " / WAFER " + filled;
            if (summaryLabel.Text != summary)
                summaryLabel.Text = summary;
        }

        public void UpdateMaterialSlots(IReadOnlyList<CassetteSlotDisplayItem> slots)
        {
            int count = _slotCount;
            if (slots != null && slots.Count > count)
            {
                SetSlotCount(slots.Count);
                count = _slotCount;
            }

            int filled = 0;
            for (int i = 0; i < count; i++)
            {
                CassetteSlotDisplayItem item = slots != null && i < slots.Count ? slots[i] : null;
                bool known = item != null && item.IsKnown;
                bool hasWafer = known && item.HasWafer;
                if (hasWafer)
                    filled++;

                WaferMaterialState state = known
                    ? WaferMaterialStateText.Normalize(item.State)
                    : WaferMaterialState.Empty;

                Color backColor = known ? ResolveStateColor(state) : Color.White;
                Color foreColor = known && state == WaferMaterialState.WorkReady ? Color.White : Color.Black;
                string text = known ? BuildSlotText(state, item.WaferId) : "-";

                var label = _slotStateLabels[i];
                if (label.BackColor != backColor)
                    label.BackColor = backColor;
                if (label.ForeColor != foreColor)
                    label.ForeColor = foreColor;
                if (label.Text != text)
                    label.Text = text;
            }

            string summary = "SLOTS " + count + " / WAFER " + filled;
            if (summaryLabel.Text != summary)
                summaryLabel.Text = summary;
        }

        private void SlotLabel_Click(object sender, EventArgs e)
        {
            var control = sender as Control;
            if (control == null || !(control.Tag is int))
                return;

            var handler = SlotSelected;
            if (handler != null)
                handler(this, new CassetteSlotSelectedEventArgs((int)control.Tag));
        }

        private void SlotLabel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var control = sender as Control;
            if (control == null || !(control.Tag is int))
                return;

            _contextSlotIndex = (int)control.Tag;
            var selectedHandler = SlotSelected;
            if (selectedHandler != null)
                selectedHandler(this, new CassetteSlotSelectedEventArgs(_contextSlotIndex));

            if (SlotMoveRequested == null)
                return;

            _moveSlotMenuItem.Text = "MOVE SLOT " + (_contextSlotIndex + 1).ToString("00");
            _slotContextMenu.Show(control, e.Location);
        }

        private void MoveSlotMenuItem_Click(object sender, EventArgs e)
        {
            if (_contextSlotIndex < 0 || _contextSlotIndex >= _slotCount)
                return;

            var handler = SlotMoveRequested;
            if (handler != null)
                handler(this, new CassetteSlotSelectedEventArgs(_contextSlotIndex));
        }

        private static Color ResolveStateColor(WaferMaterialState state)
        {
            switch (WaferMaterialStateText.Normalize(state))
            {
                // READY 슬롯 색상
                case WaferMaterialState.Ready:
                    return Color.Cyan;
                // WORKING 슬롯 색상
                case WaferMaterialState.Working:
                    return Color.Orange;
                // FINISH 슬롯 색상
                case WaferMaterialState.Finish:
                    return Color.Red;
                // WORK READY 슬롯 색상
                case WaferMaterialState.WorkReady:
                    return Color.Navy;
                default:
                    return Color.Lime;
            }
        }

        private static string BuildSlotText(WaferMaterialState state, string waferId)
        {
            var normalized = WaferMaterialStateText.Normalize(state);
            string stateText = WaferMaterialStateText.ToDisplayName(normalized);
            return normalized == WaferMaterialState.Empty || string.IsNullOrWhiteSpace(waferId)
                ? stateText
                : stateText + " / " + waferId;
        }
    }

    public sealed class CassetteSlotDisplayItem
    {
        public bool IsKnown { get; set; } = true;
        public bool HasWafer { get; set; }
        public string WaferId { get; set; } = "";
        public WaferMaterialState State { get; set; } = WaferMaterialState.Empty;
    }

    public sealed class CassetteSlotSelectedEventArgs : EventArgs
    {
        public CassetteSlotSelectedEventArgs(int slotIndex)
        {
            SlotIndex = slotIndex;
        }

        public int SlotIndex { get; private set; }
    }
}
