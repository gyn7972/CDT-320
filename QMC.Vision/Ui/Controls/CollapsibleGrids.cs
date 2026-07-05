using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 기존 그리드(또는 그리드형 컨트롤)를 런타임에 <see cref="CollapsibleGridPanel"/> 로 감싸
    /// 제목바 클릭으로 접기/펴기를 부여하는 헬퍼.
    /// Designer 구조를 바꾸지 않고, 페이지 생성자에서 InitializeComponent() 직후 호출한다.
    /// 부모가 TableLayoutPanel 이면 셀 위치/Span/Margin 을, 그 외에는 Dock/Anchor/Bounds 를 보존한다.
    /// </summary>
    public static class CollapsibleGrids
    {
        /// <summary>대상 컨트롤을 접기 패널로 감싸고 패널을 반환한다. 실패 시 null.</summary>
        public static CollapsibleGridPanel Wrap(Control content, string title)
        {
            try
            {
                if (content == null) return null;

                var parent = content.Parent;
                if (parent == null) return null;

                var panel = new CollapsibleGridPanel { Title = title ?? string.Empty };

                var tlp = parent as TableLayoutPanel;
                if (tlp != null)
                {
                    var pos = tlp.GetPositionFromControl(content);
                    int colSpan = tlp.GetColumnSpan(content);
                    int rowSpan = tlp.GetRowSpan(content);
                    var margin = content.Margin;

                    tlp.Controls.Remove(content);
                    content.Dock = DockStyle.Fill;
                    panel.Body.Controls.Add(content);

                    panel.Dock = DockStyle.Fill;
                    panel.Margin = margin;
                    tlp.Controls.Add(panel, pos.Column, pos.Row);
                    if (colSpan > 1) tlp.SetColumnSpan(panel, colSpan);
                    if (rowSpan > 1) tlp.SetRowSpan(panel, rowSpan);
                }
                else
                {
                    var dock = content.Dock;
                    var anchor = content.Anchor;
                    var bounds = content.Bounds;
                    var margin = content.Margin;
                    // 원래 z-인덱스 보존 — 도킹 레이아웃은 컬렉션 역순으로 계산되므로 래퍼를 끝에 그냥
                    // 추가하면 Fill 래퍼가 전체 영역을 먼저 차지해 Top/Bottom 형제(헤더/버튼바)가 그 위를
                    // 덮는다(조명 그리드 첫 행들이 헤더 밑에 가려지던 원인).
                    int zIndex = parent.Controls.GetChildIndex(content);

                    parent.Controls.Remove(content);
                    content.Dock = DockStyle.Fill;
                    panel.Body.Controls.Add(content);

                    panel.Margin = margin;
                    if (dock != DockStyle.None)
                    {
                        panel.Dock = dock;
                    }
                    else
                    {
                        panel.Anchor = anchor;
                        panel.Bounds = bounds;
                    }
                    parent.Controls.Add(panel);
                    parent.Controls.SetChildIndex(panel, zIndex);
                }

                return panel;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[COLLAPSIBLE-GRID] wrap failed for '" + (title ?? "") + "': " + ex.Message);
                return null;
            }
        }
    }
}
