using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class CstStatusDialog : Form
    {
        private const int SingleDialogWidth = 620;
        private const int MultiDialogWidth = 1120;
        private const int LegendHeight = 42;
        private const int CassetteHeaderHeight = 26;
        private const int SlotRowHeight = 24;
        private const int FooterHeight = 54;
        private const int DefaultSlotCount = 25;

        private readonly CassetteStatusSource[] _sources;
        private readonly List<CassetteSlotPanel> _slotPanels = new List<CassetteSlotPanel>();
        private TableLayoutPanel _cassetteLayout;
        private Timer _refreshTimer;

        public CstStatusDialog(bool isInput)
            : this(
                isInput ? "INPUT CASSETTE STATUS" : "OUTPUT CASSETTE STATUS",
                isInput
                    ? new[] { CassetteStatusSource.Input("INPUT CASSETTE", CassetteMaterialRole.Input1, DefaultSlotCount, null) }
                    : new[] { CassetteStatusSource.Output("OUTPUT GOOD 1", CassetteMaterialRole.Good1, DefaultSlotCount, null) })
        {
        }

        public CstStatusDialog(string title, IEnumerable<CassetteStatusSource> sources)
        {
            _sources = NormalizeSources(sources);

            InitializeComponent();
            Text = string.IsNullOrWhiteSpace(title) ? "CASSETTE STATUS" : title;
            ApplyPolishedLayout();
            RefreshSlots();

            _refreshTimer = new Timer { Interval = 500 };
            _refreshTimer.Tick += (s, e) => RefreshSlots();
            _refreshTimer.Start();

            Load += (s, e) =>
            {
                Lang.Apply(this);
                ApplyCloseButtonStyle();
            };
            FormClosed += (s, e) =>
            {
                if (_refreshTimer != null)
                {
                    _refreshTimer.Stop();
                    _refreshTimer.Dispose();
                    _refreshTimer = null;
                }
            };
        }

        private static CassetteStatusSource[] NormalizeSources(IEnumerable<CassetteStatusSource> sources)
        {
            var normalized = sources != null
                ? sources.Where(s => s != null).ToArray()
                : new CassetteStatusSource[0];

            if (normalized.Length > 0)
                return normalized;

            return new[]
            {
                CassetteStatusSource.Input("INPUT CASSETTE", CassetteMaterialRole.Input1, DefaultSlotCount, null)
            };
        }

        private void ApplyPolishedLayout()
        {
            int maxSlotCount = Math.Max(1, _sources.Max(s => Math.Max(1, s.SlotCount)));
            int slotListHeight = (SlotRowHeight * maxSlotCount) + 6 + CassetteHeaderHeight;
            int dialogHeight = LegendHeight + slotListHeight + FooterHeight + 24;
            int dialogWidth = _sources.Length > 1 ? MultiDialogWidth : SingleDialogWidth;

            SuspendLayout();
            rootLayout.SuspendLayout();
            legendLayout.SuspendLayout();
            bottomLayout.SuspendLayout();

            try
            {
                ClientSize = new Size(dialogWidth, dialogHeight);
                MinimumSize = new Size(dialogWidth, dialogHeight);
                BackColor = Color.FromArgb(245, 246, 248);

                rootLayout.BackColor = BackColor;
                rootLayout.Padding = new Padding(14, 12, 14, 12);
                rootLayout.RowStyles.Clear();
                rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, LegendHeight));
                rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, slotListHeight));
                rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, FooterHeight));

                ApplyLegendStyle();
                RebuildCassetteLayout();
                ApplyFooterStyle();
            }
            finally
            {
                bottomLayout.ResumeLayout(false);
                legendLayout.ResumeLayout(false);
                rootLayout.ResumeLayout(false);
                ResumeLayout(false);
            }
        }

        private void ApplyLegendStyle()
        {
            legendLayout.BackColor = BackColor;
            legendLayout.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            legendLayout.Margin = new Padding(0, 0, 0, 3);
            legendLayout.Padding = new Padding(8, 6, 8, 6);
            legendLayout.RowCount = 1;
            legendLayout.RowStyles.Clear();
            legendLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            legendLayout.ColumnCount = 10;
            legendLayout.ColumnStyles.Clear();
            for (int i = 0; i < 5; i++)
            {
                legendLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18F));
                legendLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            }

            SetLegendCell(legendReadyColor, legendReadyText, 0);
            SetLegendCell(legendEmptyColor, legendEmptyText, 2);
            SetLegendCell(legendWorkingColor, legendWorkingText, 4);
            SetLegendCell(legendFinishColor, legendFinishText, 6);
            SetLegendCell(legendWorkReadyColor, legendWorkReadyText, 8);

            foreach (Label swatch in new[] { legendReadyColor, legendEmptyColor, legendWorkingColor, legendFinishColor, legendWorkReadyColor })
            {
                swatch.Margin = new Padding(2, 4, 3, 4);
                swatch.BorderStyle = BorderStyle.FixedSingle;
            }

            foreach (Label text in new[] { legendReadyText, legendEmptyText, legendWorkingText, legendFinishText, legendWorkReadyText })
            {
                text.BackColor = BackColor;
                text.Font = new Font("Malgun Gothic", 8.5F, FontStyle.Bold);
                text.ForeColor = Color.FromArgb(35, 45, 57);
                text.Margin = new Padding(0);
                text.Padding = new Padding(1, 0, 1, 0);
                text.TextAlign = ContentAlignment.MiddleLeft;
            }
        }

        private void RebuildCassetteLayout()
        {
            if (slotsPanel != null && slotsPanel.Parent == rootLayout)
                rootLayout.Controls.Remove(slotsPanel);

            if (_cassetteLayout != null)
            {
                rootLayout.Controls.Remove(_cassetteLayout);
                _cassetteLayout.Dispose();
            }

            _slotPanels.Clear();
            _cassetteLayout = new TableLayoutPanel
            {
                BackColor = BackColor,
                ColumnCount = _sources.Length,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 1
            };
            _cassetteLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _cassetteLayout.ColumnStyles.Clear();
            for (int i = 0; i < _sources.Length; i++)
                _cassetteLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / _sources.Length));

            for (int i = 0; i < _sources.Length; i++)
            {
                var source = _sources[i];
                var panel = CreateCassettePanel(source, i);
                _slotPanels.Add(panel);
                _cassetteLayout.Controls.Add(panel.Container, i, 0);
            }

            rootLayout.Controls.Add(_cassetteLayout, 0, 1);
        }

        private CassetteSlotPanel CreateCassettePanel(CassetteStatusSource source, int index)
        {
            var container = new TableLayoutPanel
            {
                BackColor = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(index == 0 ? 0 : 4, 0, index == _sources.Length - 1 ? 0 : 4, 0),
                Padding = new Padding(0),
                RowCount = 2
            };
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            container.RowStyles.Add(new RowStyle(SizeType.Absolute, CassetteHeaderHeight));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var header = new Label
            {
                BackColor = Color.FromArgb(35, 45, 57),
                Dock = DockStyle.Fill,
                Font = new Font("Malgun Gothic", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(10, 0, 10, 0),
                Text = source.Title,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var slotHost = new Panel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(2)
            };

            var slotLayout = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = Math.Max(1, source.SlotCount)
            };
            slotLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var labels = new Label[Math.Max(1, source.SlotCount)];
            for (int row = 0; row < labels.Length; row++)
            {
                int slotIndex = row;
                int slotNo = slotIndex + 1;
                slotLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, SlotRowHeight));

                var slot = new Label
                {
                    BackColor = Color.LimeGreen,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 9F, FontStyle.Bold),
                    Margin = new Padding(0, 1, 0, 1),
                    Padding = new Padding(0),
                    Tag = slotIndex,
                    Text = slotNo.ToString("00"),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                labels[row] = slot;
                slotLayout.Controls.Add(slot, 0, row);
            }

            slotHost.Controls.Add(slotLayout);
            container.Controls.Add(header, 0, 0);
            container.Controls.Add(slotHost, 0, 1);

            return new CassetteSlotPanel(source, container, labels);
        }

        private void RefreshSlots()
        {
            if (_slotPanels.Count == 0)
                return;

            foreach (var panel in _slotPanels)
            {
                var items = BuildSlotItems(panel.Source);
                for (int i = 0; i < panel.SlotLabels.Length; i++)
                {
                    var label = panel.SlotLabels[i];
                    int slotIndex = label.Tag is int ? (int)label.Tag : i;
                    SlotStatusItem item = slotIndex >= 0 && slotIndex < items.Count
                        ? items[slotIndex]
                        : SlotStatusItem.EmptyKnown();

                    Color backColor = item.IsKnown ? ResolveStateColor(item.State) : Color.White;
                    Color foreColor = item.IsKnown
                        ? ResolveStateForeColor(item.State, backColor)
                        : GetReadableTextColor(backColor);

                    label.BackColor = backColor;
                    label.ForeColor = foreColor;
                    label.Text = (slotIndex + 1).ToString("00");
                }
            }
        }

        private static IReadOnlyList<SlotStatusItem> BuildSlotItems(CassetteStatusSource source)
        {
            int slotCount = Math.Max(1, source.SlotCount);
            var snapshot = MaterialStorage.State;
            var cassette = snapshot != null && snapshot.Cassettes != null
                ? snapshot.Cassettes.FirstOrDefault(c => c.Role == source.Role)
                : null;

            if (cassette != null)
            {
                cassette.EnsureSlots();
                if (slotCount <= 0)
                    slotCount = cassette.SlotCount;
            }

            var items = new List<SlotStatusItem>(slotCount);
            for (int i = 0; i < slotCount; i++)
            {
                bool fallbackKnown = source.FallbackMap != null && i < source.FallbackMap.Count;
                bool fallbackHasWafer = fallbackKnown && source.FallbackMap[i];
                CassetteSlotMaterial slot = cassette != null && cassette.Slots != null && i < cassette.Slots.Count
                    ? cassette.Slots[i]
                    : null;
                WaferMaterial wafer = ResolveCassetteSlotWafer(snapshot, source, i, slot);
                WaferMaterialState state = wafer != null
                    ? WaferMaterialStateText.Normalize(wafer.State)
                    : ((slot != null && slot.HasWafer) || fallbackHasWafer ? WaferMaterialState.Ready : WaferMaterialState.Empty);
                bool hasWafer = ((slot != null && slot.HasWafer) || fallbackHasWafer || IsWaferInTrackedLocation(wafer, source)) &&
                                state != WaferMaterialState.Empty;
                bool known = fallbackKnown ||
                             (cassette != null && cassette.IsMapped) ||
                             (source.KnownWhenCassetteExists && cassette != null);

                items.Add(new SlotStatusItem
                {
                    IsKnown = known,
                    HasWafer = hasWafer,
                    State = hasWafer ? state : WaferMaterialState.Empty
                });
            }

            return items;
        }

        private static WaferMaterial ResolveCassetteSlotWafer(MaterialSnapshot snapshot, CassetteStatusSource source, int slotIndex, CassetteSlotMaterial slot)
        {
            if (snapshot == null || snapshot.Wafers == null || slotIndex < 0)
                return null;

            if (slot != null && !string.IsNullOrWhiteSpace(slot.WaferInstanceId))
            {
                return snapshot.Wafers.FirstOrDefault(w =>
                    w != null &&
                    string.Equals(
                        w.WaferInstanceId ?? "",
                        slot.WaferInstanceId,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        w.WaferId ?? "",
                        slot.WaferId ?? "",
                        StringComparison.OrdinalIgnoreCase));
            }

            if (slot != null && !string.IsNullOrWhiteSpace(slot.WaferId))
            {
                List<WaferMaterial> legacyCandidates = snapshot.Wafers
                    .Where(w =>
                        w != null &&
                        string.Equals(
                            w.WaferId ?? "",
                            slot.WaferId,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (legacyCandidates.Count == 1)
                    return legacyCandidates[0];
                return null;
            }

            return snapshot.Wafers.FirstOrDefault(w =>
                w != null &&
                WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty &&
                IsRoleSlotMatch(w, source.Role, slotIndex) &&
                IsWaferInTrackedLocation(w, source));
        }

        private static bool IsRoleSlotMatch(WaferMaterial wafer, CassetteMaterialRole role, int slotIndex)
        {
            return (wafer.SourceCassetteRole == role && wafer.SourceSlotNumber == slotIndex) ||
                   (wafer.OutputCassetteRole == role && wafer.OutputSlotNumber == slotIndex);
        }

        private static bool IsWaferInTrackedLocation(WaferMaterial wafer, CassetteStatusSource source)
        {
            if (wafer == null || wafer.CurrentLocation == null)
                return false;

            if (source.IsInput)
            {
                return wafer.CurrentLocation.Kind == MaterialLocationKind.InputCassette ||
                       wafer.CurrentLocation.Kind == MaterialLocationKind.InputFeeder ||
                       wafer.CurrentLocation.Kind == MaterialLocationKind.InputStage;
            }

            return wafer.CurrentLocation.Kind == MaterialLocationKind.OutputCassette ||
                   wafer.CurrentLocation.Kind == MaterialLocationKind.OutputFeeder ||
                   wafer.CurrentLocation.Kind == MaterialLocationKind.OutputStageGood ||
                   wafer.CurrentLocation.Kind == MaterialLocationKind.OutputStageNg;
        }

        private static Color ResolveStateColor(WaferMaterialState state)
        {
            switch (WaferMaterialStateText.Normalize(state))
            {
                case WaferMaterialState.Ready:
                    return Color.Cyan;
                case WaferMaterialState.Working:
                    return Color.Orange;
                case WaferMaterialState.Finish:
                    return Color.MediumSeaGreen;
                case WaferMaterialState.WorkReady:
                    return Color.Navy;
                default:
                    return Color.Gainsboro;
            }
        }

        private static Color ResolveStateForeColor(WaferMaterialState state, Color backColor)
        {
            WaferMaterialState normalized = WaferMaterialStateText.Normalize(state);
            return normalized == WaferMaterialState.Finish ||
                   normalized == WaferMaterialState.WorkReady
                ? Color.White
                : GetReadableTextColor(backColor);
        }

        private void SetLegendCell(Label swatch, Label text, int colorColumn)
        {
            legendLayout.SetCellPosition(swatch, new TableLayoutPanelCellPosition(colorColumn, 0));
            legendLayout.SetCellPosition(text, new TableLayoutPanelCellPosition(colorColumn + 1, 0));
        }

        private void ApplyFooterStyle()
        {
            bottomLayout.BackColor = BackColor;
            bottomLayout.Margin = new Padding(0, 10, 0, 0);
            bottomLayout.Padding = new Padding(0);
            bottomLayout.ColumnStyles[1].Width = 112F;
            bottomLayout.ColumnStyles[2].Width = 0F;

            ApplyCloseButtonStyle();
        }

        private void ApplyCloseButtonStyle()
        {
            btnClose.BackColor = Color.FromArgb(52, 73, 94);
            btnClose.ForeColor = Color.White;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Font = new Font("Malgun Gothic", 9.5F, FontStyle.Bold);
            btnClose.Margin = new Padding(0, 8, 0, 6);
            btnClose.TextAlign = ContentAlignment.MiddleCenter;
            btnClose.UseVisualStyleBackColor = false;
        }

        private static Color GetReadableTextColor(Color backColor)
        {
            int brightness = (backColor.R * 299 + backColor.G * 587 + backColor.B * 114) / 1000;
            return brightness < 95 ? Color.White : Color.Black;
        }

        public sealed class CassetteStatusSource
        {
            private CassetteStatusSource()
            {
            }

            public string Title { get; private set; }
            public CassetteMaterialRole Role { get; private set; }
            public int SlotCount { get; private set; }
            public IReadOnlyList<bool> FallbackMap { get; private set; }
            public bool IsInput { get; private set; }
            public bool KnownWhenCassetteExists { get; private set; }

            public static CassetteStatusSource Input(string title, CassetteMaterialRole role, int slotCount, IReadOnlyList<bool> fallbackMap)
            {
                return new CassetteStatusSource
                {
                    Title = title,
                    Role = role,
                    SlotCount = slotCount > 0 ? slotCount : DefaultSlotCount,
                    FallbackMap = fallbackMap,
                    IsInput = true,
                    KnownWhenCassetteExists = false
                };
            }

            public static CassetteStatusSource Output(string title, CassetteMaterialRole role, int slotCount, IReadOnlyList<bool> fallbackMap)
            {
                return new CassetteStatusSource
                {
                    Title = title,
                    Role = role,
                    SlotCount = slotCount > 0 ? slotCount : DefaultSlotCount,
                    FallbackMap = fallbackMap,
                    IsInput = false,
                    KnownWhenCassetteExists = true
                };
            }
        }

        private sealed class CassetteSlotPanel
        {
            public CassetteSlotPanel(CassetteStatusSource source, Control container, Label[] slotLabels)
            {
                Source = source;
                Container = container;
                SlotLabels = slotLabels;
            }

            public CassetteStatusSource Source { get; private set; }
            public Control Container { get; private set; }
            public Label[] SlotLabels { get; private set; }
        }

        private sealed class SlotStatusItem
        {
            public bool IsKnown { get; set; }
            public bool HasWafer { get; set; }
            public WaferMaterialState State { get; set; }

            public static SlotStatusItem EmptyKnown()
            {
                return new SlotStatusItem
                {
                    IsKnown = true,
                    HasWafer = false,
                    State = WaferMaterialState.Empty
                };
            }
        }
    }
}
