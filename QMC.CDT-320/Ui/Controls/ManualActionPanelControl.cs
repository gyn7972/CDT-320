using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>MANUAL ACTION 버튼 하나의 정의. 텍스트와 클릭 시 실행할 비동기 동작을 갖는다.</summary>
    public sealed class ManualActionItem
    {
        public string Text { get; private set; }
        public Func<Task> ClickAsync { get; private set; }
        public object Tag { get; set; }

        private ManualActionItem()
        {
        }

        /// <summary>버튼 텍스트와 클릭 동작으로 항목을 만든다. 동작 내부에서 확인 팝업/시퀀스 호출을 수행한다.</summary>
        public static ManualActionItem Create(string text, Func<Task> clickAsync)
        {
            return new ManualActionItem { Text = text ?? string.Empty, ClickAsync = clickAsync };
        }
    }

    /// <summary>레시피 페이지 공용 MANUAL ACTION 버튼 판넬.
    /// SetItems로 버튼 정의를 넘기면 ColumnCount 열 그리드(행 높이 RowHeight)로 ActionButton을 생성한다.
    /// 페이지마다 버튼 개수가 달라도 행 수가 자동 계산되며, AutoFitParentGroupHeight로
    /// 부모 GroupBox 높이를 버튼 수에 맞춰 자동 조정할 수 있다.</summary>
    public partial class ManualActionPanelControl : UserControl
    {
        private readonly List<ManualActionItem> _items = new List<ManualActionItem>();
        private readonly List<ActionButton> _buttons = new List<ActionButton>();
        private int _columnCount = 2;
        private int _rowHeight = 45;
        private bool _autoFitParentGroupHeight;

        public ManualActionPanelControl()
        {
            try
            {
                InitializeComponent();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        /// <summary>버튼을 배치할 열 수. 기본 2열(Front Head 기준).</summary>
        public int ColumnCount
        {
            get
            {
                try { return _columnCount; }
                finally { }
            }
            set
            {
                try
                {
                    _columnCount = Math.Max(1, value);
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Warning, "UI", "MANUAL-ACT", "ColumnCount set failed: " + ex.Message);
                }
                finally
                {
                }
            }
        }

        /// <summary>버튼 행 높이(px). 기본 45(Front Head 기준: 버튼 37 + 상하 마진 8).</summary>
        public int RowHeight
        {
            get
            {
                try { return _rowHeight; }
                finally { }
            }
            set
            {
                try
                {
                    _rowHeight = Math.Max(24, value);
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Warning, "UI", "MANUAL-ACT", "RowHeight set failed: " + ex.Message);
                }
                finally
                {
                }
            }
        }

        /// <summary>true면 SetItems 후 부모 GroupBox 높이를 버튼 행 수에 맞춰 자동 조정한다.</summary>
        public bool AutoFitParentGroupHeight
        {
            get { return _autoFitParentGroupHeight; }
            set { _autoFitParentGroupHeight = value; }
        }

        /// <summary>현재 항목 수 기준 버튼 영역 필요 높이(패딩 포함).</summary>
        public int PreferredContentHeight
        {
            get
            {
                try
                {
                    int rows = Math.Max(1, (int)Math.Ceiling(_items.Count / (double)_columnCount));
                    return rows * _rowHeight + buttonsHost.Padding.Top + buttonsHost.Padding.Bottom;
                }
                catch
                {
                    return _rowHeight;
                }
                finally
                {
                }
            }
        }

        /// <summary>버튼 정의 목록으로 판넬을 다시 만든다. 행 수는 열 수에 따라 자동 계산된다.
        /// null 항목은 빈 칸(자리만 차지)으로 처리된다.</summary>
        public void SetItems(IEnumerable<ManualActionItem> items)
        {
            try
            {
                _items.Clear();
                if (items != null)
                    _items.AddRange(items);   // null = 빈 칸

                RebuildButtons();
                FitParentGroupHeight();
            }
            catch (Exception ex)
            {
                string message = "Manual action set failed: " + Name + Environment.NewLine + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "MANUAL-ACT", message);
                QMC.Common.MessageDialog.Show(this, message, "Manual Action", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        /// <summary>모든 버튼의 활성 상태를 일괄 변경한다(인터락용).</summary>
        public void SetButtonsEnabled(bool enabled)
        {
            try
            {
                foreach (ActionButton button in _buttons)
                    button.Enabled = enabled;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "MANUAL-ACT", "SetButtonsEnabled failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void RebuildButtons()
        {
            try
            {
                buttonsHost.SuspendLayout();
                buttonsHost.Controls.Clear();
                buttonsHost.ColumnStyles.Clear();
                buttonsHost.RowStyles.Clear();
                _buttons.Clear();

                int columns = Math.Max(1, _columnCount);
                int rows = Math.Max(1, (int)Math.Ceiling(_items.Count / (double)columns));

                buttonsHost.ColumnCount = columns;
                buttonsHost.RowCount = rows;
                for (int c = 0; c < columns; c++)
                    buttonsHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
                for (int r = 0; r < rows; r++)
                    buttonsHost.RowStyles.Add(new RowStyle(SizeType.Absolute, _rowHeight));

                // 남는 세로 공간이 마지막 행에 흡수되지 않도록 내용 높이로 상단 도킹
                buttonsHost.Dock = DockStyle.Top;
                buttonsHost.Height = PreferredContentHeight;

                for (int i = 0; i < _items.Count; i++)
                {
                    ManualActionItem item = _items[i];
                    if (item == null)
                        continue;   // 빈 칸 - 자리만 차지

                    ActionButton button = CreateButton(item);
                    _buttons.Add(button);
                    buttonsHost.Controls.Add(button, i % columns, i / columns);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                buttonsHost.ResumeLayout();
            }
        }

        private ActionButton CreateButton(ManualActionItem item)
        {
            ActionButton button = new ActionButton();
            button.Text = item.Text;
            button.Tag = item;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(4);
            button.Cursor = Cursors.Hand;
            button.BackColor = Color.FromArgb(88, 94, 103);
            button.ForeColor = Color.White;
            button.Font = new Font("Malgun Gothic", 8F, FontStyle.Bold);
            button.Click += ActionButton_Click;
            return button;
        }

        private async void ActionButton_Click(object sender, EventArgs e)
        {
            try
            {
                ActionButton button = sender as ActionButton;
                ManualActionItem item = button != null ? button.Tag as ManualActionItem : null;
                if (item == null || item.ClickAsync == null)
                    return;

                await item.ClickAsync();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "MANUAL-ACT", "Action click failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Manual Action", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void FitParentGroupHeight()
        {
            try
            {
                if (!_autoFitParentGroupHeight)
                    return;

                GroupBox group = Parent as GroupBox;
                if (group == null || !IsHandleCreated)
                    return;

                int chrome = group.Height - Height;   // 그룹 헤더 + 패딩 (현재 레이아웃 기준 실측)
                if (chrome < 0)
                    chrome = 24;
                group.Height = PreferredContentHeight + chrome;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "MANUAL-ACT", "FitParentGroupHeight failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}
