using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 제목바를 클릭하면 내부 콘텐츠(그리드 등)를 접고/펴는 재사용 패널.
    /// 부모가 TableLayoutPanel 이면 해당 셀의 RowStyle 을 저장/복원하여
    /// 접을 때 화면 공간을 반환한다. 그 외 컨테이너에서는 Dock/Height 로 대체 처리한다.
    /// 콘텐츠는 <see cref="Body"/> 패널에 추가한다.
    /// </summary>
    public partial class CollapsibleGridPanel : UserControl
    {
        // 제목바 높이(px). Designer 의 panelHeader 높이와 일치시킨다.
        private const int HeaderHeight = 30;

        private bool _collapsed;

        // 부모가 TableLayoutPanel 인 경우, 접기 전 RowStyle 복원용 저장값.
        private bool _rowStyleSaved;
        private SizeType _savedRowSizeType;
        private float _savedRowValue;

        // 부모가 TableLayoutPanel 이 아닌 경우, Dock/Height 복원용 저장값.
        private bool _dockFallbackSaved;
        private DockStyle _savedDock;
        private int _savedHeight;

        /// <summary>접힘 상태가 바뀔 때 발생.</summary>
        public event EventHandler CollapsedChanged;

        /// <summary>제목바 텍스트.</summary>
        [Browsable(true)]
        public string Title
        {
            get { return lblTitle.Text; }
            set { lblTitle.Text = value ?? string.Empty; }
        }

        /// <summary>콘텐츠(그리드 등)를 담는 패널.</summary>
        [Browsable(false)]
        public Panel Body
        {
            get { return panelBody; }
        }

        /// <summary>접힘 여부. true 면 콘텐츠를 숨기고 제목바만 남긴다.</summary>
        [Browsable(false)]
        public bool Collapsed
        {
            get { return _collapsed; }
            set { SetCollapsed(value); }
        }

        public CollapsibleGridPanel()
        {
            InitializeComponent();
            UpdateArrow();
        }

        private void panelHeader_Click(object sender, EventArgs e)
        {
            try
            {
                SetCollapsed(!_collapsed);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[COLLAPSIBLE-GRID] toggle failed for '" + lblTitle.Text + "': " + ex.Message);
            }
        }

        /// <summary>접힘 상태를 설정하고 레이아웃을 갱신한다.</summary>
        public void SetCollapsed(bool collapsed)
        {
            try
            {
                if (_collapsed == collapsed) return;
                _collapsed = collapsed;
                ApplyCollapsedState();
                UpdateArrow();
                CollapsedChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[COLLAPSIBLE-GRID] SetCollapsed failed for '" + lblTitle.Text + "': " + ex.Message);
            }
        }

        private void ApplyCollapsedState()
        {
            try
            {
                var tlp = this.Parent as TableLayoutPanel;
                if (tlp != null && TryGetRow(tlp, out int row))
                {
                    ApplyTableLayoutCollapse(tlp, row);
                }
                else
                {
                    ApplyDockFallbackCollapse();
                }
                panelBody.Visible = !_collapsed;
            }
            finally
            {
                this.Parent?.PerformLayout();
            }
        }

        private void ApplyTableLayoutCollapse(TableLayoutPanel tlp, int row)
        {
            var style = tlp.RowStyles[row];
            if (_collapsed)
            {
                if (!_rowStyleSaved)
                {
                    _savedRowSizeType = style.SizeType;
                    _savedRowValue = style.Height;
                    _rowStyleSaved = true;
                }
                style.SizeType = SizeType.Absolute;
                style.Height = HeaderHeight;
            }
            else if (_rowStyleSaved)
            {
                style.SizeType = _savedRowSizeType;
                style.Height = _savedRowValue;
                _rowStyleSaved = false;
            }
        }

        private void ApplyDockFallbackCollapse()
        {
            if (_collapsed)
            {
                if (!_dockFallbackSaved)
                {
                    _savedDock = this.Dock;
                    _savedHeight = this.Height;
                    _dockFallbackSaved = true;
                }
                this.Dock = DockStyle.Top;
                this.Height = HeaderHeight;
            }
            else if (_dockFallbackSaved)
            {
                this.Dock = _savedDock;
                this.Height = _savedHeight;
                _dockFallbackSaved = false;
            }
        }

        private bool TryGetRow(TableLayoutPanel tlp, out int row)
        {
            row = -1;
            try
            {
                var pos = tlp.GetPositionFromControl(this);
                row = pos.Row;
                return row >= 0 && row < tlp.RowStyles.Count;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateArrow()
        {
            lblArrow.Text = _collapsed ? "▶" : "▼";
        }
    }
}
