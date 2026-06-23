using System.Windows.Forms;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// 측면 비전 <b>Front + Rear 동시</b> 표시 컨트롤 — 위/아래 반반.
    /// Front=TopSideVision(뷰어 5205), Rear=BottomSideVision(뷰어 5206).
    /// 두 모듈을 동시에 띄워 셋업이 편하도록 한다(실측 이미지 4000x700은 추후 분할 예정, 지금은 전체 표시).
    /// 코드 전용 컨트롤(Designer 없음).
    /// </summary>
    public sealed class SideVisionViewerControl : UserControl
    {
        private readonly VisionViewerPanel _front;
        private readonly VisionViewerPanel _rear;

        public SideVisionViewerControl(string host)
        {
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            _front = new VisionViewerPanel(host, VisionViewerPorts.TopSide, "FRONT SIDE", VisionHub.TopSide)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 2)
            };
            _rear = new VisionViewerPanel(host, VisionViewerPorts.BottomSide, "REAR SIDE", VisionHub.BottomSide)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 0)
            };

            grid.Controls.Add(_front, 0, 0);
            grid.Controls.Add(_rear, 0, 1);
            Controls.Add(grid);
        }
    }
}
