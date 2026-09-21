using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Controls
{
    public sealed partial class CassetteSlotView : UserControl, ILocalizedView
    {
        private Label[] _slotStateLabels = new Label[0];
        private int _slotCount;
        private readonly ContextMenuStrip _slotContextMenu;
        private readonly ToolStripMenuItem _moveSlotMenuItem;
        private int _contextSlotIndex = -1;
        private SlotText[] _displaySlots = new SlotText[0];

        private sealed class SlotText
        {
            public string Key;
            public WaferMaterialState State;
            public string WaferId;
            public int? DieCount;
        }

        public static readonly Color ReadyStateColor = Color.Cyan;
        public static readonly Color EmptyStateColor = Color.Gainsboro;
        public static readonly Color WorkingStateColor = Color.Orange;
        public static readonly Color FinishStateColor = Color.MediumSeaGreen;
        public static readonly Color WorkReadyStateColor = Color.Navy;

        public CassetteSlotView()
        {
            InitializeComponent();
            _slotContextMenu = new ContextMenuStrip();
            _moveSlotMenuItem = new ToolStripMenuItem("MOVE");
            _moveSlotMenuItem.Click += MoveSlotMenuItem_Click;
            _slotContextMenu.Items.Add(_moveSlotMenuItem);
            ConfigureDesignSurface();
            Title = titleLabel.Text;
            Lang.BindFormat(summaryLabel, "controls.cassette.summary", 0, 0);
            ApplyLanguage();
            SetSlotCount(0);
        }

        public int SlotCount { get { return _slotCount; } }

        public event EventHandler<CassetteSlotSelectedEventArgs> SlotSelected;
        public event EventHandler<CassetteSlotSelectedEventArgs> SlotMoveRequested;
        // 슬롯 번호/상태 Label 더블클릭 알림. View는 이벤트만 올리고 모션/자재 판정은 Page가 수행한다.
        public event EventHandler<CassetteSlotSelectedEventArgs> SlotDoubleClicked;

        public Color EmptyColor { get; set; } = EmptyStateColor;

        public string Title
        {
            get { return titleLabel.Text; }
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value == "CASSETTE")
                    Lang.BindKey(titleLabel, "controls.cassette.title");
                else
                    Lang.Bind(titleLabel, value);
            }
        }

        public void ApplyLanguage()
        {
            for (int index = 0; index < _displaySlots.Length; index++)
                RefreshSlotText(index);
            _moveSlotMenuItem.Text = _contextSlotIndex < 0
                ? Lang.T("controls.cassette.move")
                : Lang.Format("controls.cassette.moveSlot", (_contextSlotIndex + 1).ToString("00"));
        }

        private void RefreshSlotText(int index)
        {
            SlotText item = _displaySlots[index];
            if (item == null) return;
            string text = item.Key == null
                ? BuildSlotText(item.State, item.WaferId, item.DieCount)
                : item.Key == "-" ? "-" : Lang.T(item.Key);
            if (_slotStateLabels[index].Text != text) _slotStateLabels[index].Text = text;
        }

        private void ConfigureDesignSurface()
        {
            BackColor = Color.White;
            Margin = new Padding(0);
            Size = new Size(360, 480);

            rootLayout.BackColor = Color.White;
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
            scrollPanel.BackColor = Color.White;
            scrollPanel.Dock = DockStyle.Fill;
            scrollPanel.Margin = new Padding(0);
            scrollPanel.Padding = new Padding(0, 8, 0, 0);

            slotLayout.AutoSize = true;
            slotLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            slotLayout.BackColor = Color.White;
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
            _displaySlots = new SlotText[slotCount];

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
                    BackColor = Color.White,
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
                    Text = Lang.T("controls.cassette.empty"),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                no.Click += SlotLabel_Click;
                state.Click += SlotLabel_Click;
                no.MouseDown += SlotLabel_MouseDown;
                state.MouseDown += SlotLabel_MouseDown;
                no.DoubleClick += SlotLabel_DoubleClick;
                state.DoubleClick += SlotLabel_DoubleClick;

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
                _displaySlots[i] = new SlotText
                {
                    Key = current
                        ? (hasWafer ? "controls.cassette.currentReady" : "controls.cassette.currentEmpty")
                        : (hasWafer ? "controls.cassette.ready" : "controls.cassette.empty")
                };

                var label = _slotStateLabels[i];
                if (label.BackColor != backColor)
                    label.BackColor = backColor;
                RefreshSlotText(i);
            }

            Lang.BindFormat(summaryLabel, "controls.cassette.summary", count, filled);
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
                Color foreColor = ResolveStateForeColor(state);
                _displaySlots[i] = new SlotText
                {
                    Key = known ? null : "-",
                    State = state,
                    WaferId = known ? item.WaferId : null,
                    DieCount = known ? item.DieCount : null
                };

                var label = _slotStateLabels[i];
                if (label.BackColor != backColor)
                    label.BackColor = backColor;
                if (label.ForeColor != foreColor)
                    label.ForeColor = foreColor;
                RefreshSlotText(i);
            }

            Lang.BindFormat(summaryLabel, "controls.cassette.summary", count, filled);
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

        private void SlotLabel_DoubleClick(object sender, EventArgs e)
        {
            var control = sender as Control;
            if (control == null || !(control.Tag is int))
                return;

            var handler = SlotDoubleClicked;
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

            _moveSlotMenuItem.Text = Lang.Format("controls.cassette.moveSlot", (_contextSlotIndex + 1).ToString("00"));
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
                    return ReadyStateColor;
                // WORKING 슬롯 색상
                case WaferMaterialState.Working:
                    return WorkingStateColor;
                // FINISH 슬롯 색상
                case WaferMaterialState.Finish:
                    return FinishStateColor;
                // WORK READY 슬롯 색상
                case WaferMaterialState.WorkReady:
                    return WorkReadyStateColor;
                default:
                    return EmptyStateColor;
            }
        }

        private static Color ResolveStateForeColor(WaferMaterialState state)
        {
            WaferMaterialState normalized = WaferMaterialStateText.Normalize(state);
            return normalized == WaferMaterialState.Finish ||
                   normalized == WaferMaterialState.WorkReady
                ? Color.White
                : Color.Black;
        }

        private static string BuildSlotText(WaferMaterialState state, string waferId, int? dieCount)
        {
            var normalized = WaferMaterialStateText.Normalize(state);
            string stateText;
            switch (normalized)
            {
                case WaferMaterialState.Empty: stateText = Lang.T("controls.cassette.empty"); break;
                case WaferMaterialState.Ready: stateText = Lang.T("controls.cassette.ready"); break;
                case WaferMaterialState.Working: stateText = Lang.T("controls.cassette.working"); break;
                case WaferMaterialState.Finish: stateText = Lang.T("controls.cassette.finish"); break;
                case WaferMaterialState.WorkReady: stateText = Lang.T("controls.cassette.workReady"); break;
                default: stateText = WaferMaterialStateText.ToDisplayName(normalized); break;
            }
            if (normalized == WaferMaterialState.Empty || string.IsNullOrWhiteSpace(waferId))
                return stateText;

            // [P3 2026-08-22] 다이 수는 값이 있을 때만 꼬리에 붙인다(예: "READY / YZ8WW.02 / 696D").
            // [2차 검토수정 2026-08-23] 0도 표시한다("0D") — 지정 BIN 모드에서 선택 bin이
            // 이 웨이퍼에 하나도 없으면 합계가 0인데, 숨기면 "아직 안 읽힘"과 구분되지 않는다.
            return dieCount.HasValue && dieCount.Value >= 0
                ? stateText + " / " + waferId + " / " + dieCount.Value + "D"
                : stateText + " / " + waferId;
        }
    }

    public sealed class CassetteSlotDisplayItem
    {
        public bool IsKnown { get; set; } = true;
        public bool HasWafer { get; set; }
        public string WaferId { get; set; } = "";
        public WaferMaterialState State { get; set; } = WaferMaterialState.Empty;
        /// <summary>[P3 2026-08-22] 웨이퍼 다이 수(로딩 전=네트워크 맵, 로딩 후=실제 DieIds). null이면 미표시.</summary>
        public int? DieCount { get; set; }
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
