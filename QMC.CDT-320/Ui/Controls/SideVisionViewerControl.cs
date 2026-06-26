using System.Windows.Forms;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// 측면 비전 <b>Front + Rear 동시</b> 표시 컨트롤 — 위/아래 반반.
    /// UI(2행 그리드 + 두 뷰어 패널)는 Designer 에 있고, 여기서는 <see cref="Configure"/>로 host 만 주입한다.
    /// Front=TopSideVision(뷰어 5205), Rear=BottomSideVision(뷰어 5206).
    /// </summary>
    public sealed partial class SideVisionViewerControl : UserControl
    {
        public SideVisionViewerControl()
        {
            InitializeComponent();
        }

        /// <summary>런타임 주입 — 두 측면 뷰어를 각자 포트/명령 채널로 구성한다.</summary>
        public void Configure(string host)
        {
            _front.Configure(host, VisionViewerPorts.TopSide,    "FRONT SIDE", VisionHub.TopSide);
            _rear.Configure(host,  VisionViewerPorts.BottomSide, "REAR SIDE",  VisionHub.BottomSide);
        }
    }
}
