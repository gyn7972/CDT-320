using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class AxisPositionPopup : Form
    {
        private const int DefaultTargetRows = 26;

        private readonly List<BaseAxis> _axes;
        private readonly List<AxisRow> _rows = new List<AxisRow>();
        private readonly Timer _timer = new Timer();

        private readonly Font _headerFont = new Font("Segoe UI", 11F, FontStyle.Bold);
        private readonly Font _rowFont = new Font("Segoe UI", 11F, FontStyle.Bold);
        private readonly Brush _headerBack = new SolidBrush(Color.White);
        private readonly Pen _borderPen = new Pen(Color.FromArgb(0, 90, 180), 2);

        public AxisPositionPopup(IEnumerable<BaseAxis> axes, MotionMonitorService monitor)
        {
            _axes = SortAxes(axes).ToList();

            InitializeComponent();
            EnableDoubleBuffer(listViewAxis);
            BuildAxisList();
            RefreshPositions();

            listViewAxis.DrawColumnHeader += ListViewAxis_DrawColumnHeader;
            listViewAxis.DrawItem += ListViewAxis_DrawItem;
            listViewAxis.DrawSubItem += ListViewAxis_DrawSubItem;

            _timer.Interval = 200;
            _timer.Tick += (s, e) => RefreshPositions();
            Load += (s, e) => _timer.Start();
            FormClosed += (s, e) =>
            {
                _timer.Stop();
            };
        }

        private static IEnumerable<BaseAxis> SortAxes(IEnumerable<BaseAxis> axes)
        {
            return (axes ?? Enumerable.Empty<BaseAxis>())
                .Where(a => a != null)
                .OrderBy(a => a.Setup != null && a.Setup.AxisNo >= 0 ? a.Setup.AxisNo : int.MaxValue)
                .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase);
        }

        private void BuildAxisList()
        {
            listViewAxis.BeginUpdate();
            listViewAxis.Items.Clear();
            _rows.Clear();

            foreach (var axis in _axes)
            {
                int axisNo = axis.Setup != null ? axis.Setup.AxisNo : -1;
                var item = new ListViewItem(axisNo >= 0 ? axisNo.ToString("00", CultureInfo.InvariantCulture) : "--");
                item.SubItems.Add(DisplayName(axis));
                item.SubItems.Add("----");
                item.SubItems.Add(AxisUnitConverter.DisplayUnitFor(axis));
                item.Tag = axis;
                listViewAxis.Items.Add(item);
                _rows.Add(new AxisRow(axis, item));
            }

            for (int i = _rows.Count; i < DefaultTargetRows; i++)
            {
                var item = new ListViewItem("--");
                item.SubItems.Add("(Empty)");
                item.SubItems.Add("-");
                item.SubItems.Add("-");
                listViewAxis.Items.Add(item);
            }

            listViewAxis.EndUpdate();
        }

        private void OnAxisStatusUpdated(AxisStatusSnapshot snapshot)
        {
            if (snapshot == null || IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<AxisStatusSnapshot>(OnAxisStatusUpdated), snapshot); } catch { }
                return;
            }

            foreach (var row in _rows)
            {
                if (!string.Equals(row.Axis.Name, snapshot.AxisName, StringComparison.OrdinalIgnoreCase))
                    continue;
                ApplySnapshot(row.Item, snapshot);
                break;
            }
        }

        private void RefreshPositions()
        {
            if (IsDisposed) return;
            // 축 상태는 MotionMonitorService 백그라운드 폴링이 갱신한다.
            // UI 스레드에서 UpdateStatus(보드 I/O + 축 lock)를 호출하지 않고 캐시 값만 읽어 표시한다.
            // (CYCLING 중 시퀀스 모션과의 lock 경합으로 UI 가 멈추는 것을 방지)
            foreach (var row in _rows)
            {
                try
                {
                    ApplySnapshot(row.Item, AxisStatusSnapshot.FromAxis(row.Axis));
                }
                catch
                {
                }
                finally
                {
                }
            }
        }

        private static void ApplySnapshot(ListViewItem item, AxisStatusSnapshot s)
        {
            if (item == null || s == null) return;
            string unit = AxisUnitConverter.Normalize(s.Unit);
            double displayValue = AxisUnitConverter.ToDisplay(s.ActualPosition, unit);
            string pos = displayValue.ToString(unit == AxisUnitConverter.Micrometer ? "0" : "0.000", CultureInfo.InvariantCulture);
            if (item.SubItems.Count > 2 && item.SubItems[2].Text != pos)
                item.SubItems[2].Text = pos;
            if (item.SubItems.Count > 3 && item.SubItems[3].Text != unit)
                item.SubItems[3].Text = unit;

            Color backColor = s.IsAlarm ? Color.FromArgb(80, 0, 0) : Color.Black;
            if (item.BackColor != backColor)
                item.BackColor = backColor;
        }

        private static string DisplayName(BaseAxis axis)
        {
            if (axis == null) return string.Empty;
            if (axis.Setup != null && !string.IsNullOrWhiteSpace(axis.Setup.DisplayName))
                return axis.Setup.DisplayName;
            return axis.Name ?? string.Empty;
        }

        private void ListViewAxis_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.Graphics.FillRectangle(_headerBack, e.Bounds);
            e.Graphics.DrawRectangle(_borderPen, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, _headerFont, e.Bounds, Color.Black, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        private void ListViewAxis_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = false;
        }

        private void ListViewAxis_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            using (var back = new SolidBrush(e.Item.BackColor == Color.Empty ? Color.Black : e.Item.BackColor))
                e.Graphics.FillRectangle(back, e.Bounds);

            Color fore = e.ColumnIndex == 2 ? Color.FromArgb(0, 240, 140) : Color.FromArgb(0, 210, 120);
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            if (e.Header.TextAlign == HorizontalAlignment.Right)
                flags |= TextFormatFlags.Right;
            else if (e.Header.TextAlign == HorizontalAlignment.Center)
                flags |= TextFormatFlags.HorizontalCenter;
            else
                flags |= TextFormatFlags.Left;

            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, _rowFont, e.Bounds, fore, flags);
        }

        private static void EnableDoubleBuffer(Control control)
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (prop != null)
                prop.SetValue(control, true, null);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _headerFont.Dispose();
                _rowFont.Dispose();
                _headerBack.Dispose();
                _borderPen.Dispose();
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class AxisRow
        {
            public AxisRow(BaseAxis axis, ListViewItem item)
            {
                Axis = axis;
                Item = item;
            }

            public BaseAxis Axis { get; private set; }
            public ListViewItem Item { get; private set; }
        }
    }
}
