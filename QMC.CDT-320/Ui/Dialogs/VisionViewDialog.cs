using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// 통합 Vision View — 모듈별 이미지 뷰어를 탭으로 모아 보여준다(웨이퍼/바텀/빈/측면 동시).
    /// UI(탭 + 뷰어 패널)는 Designer 에 있고, 여기에는 각 패널의 런타임 Configure 와 런처/Open 만 둔다.
    /// </summary>
    public sealed partial class VisionViewDialog : Form
    {
        public VisionViewDialog()
        {
            InitializeComponent();

            string host = VisionHub.Host;
            _pnWafer.Configure(host, VisionViewerPorts.Wafer, "Wafer", VisionHub.Wafer);
            _pnBottom.Configure(host, VisionViewerPorts.BottomInspection, "Bottom Inspection", VisionHub.Inspection);
            _pnBin.Configure(host, VisionViewerPorts.Bin, "Bin", VisionHub.Bin);
            _pnSide.Configure(host);
        }

        /// <summary>통합 Vision View 팝업을 연다.</summary>
        public static void Open(IWin32Window owner)
        {
            using (var dlg = new VisionViewDialog())
                dlg.ShowDialog(owner);
        }

        /// <summary>
        /// 작업정보 페이지 Action 컨테이너에 'VISION VIEW'(+선택 'VISION: SIDE(동시)') 런처를 추가한다.
        /// 기존 ActionButton 스타일. STOP 버튼은 맨 끝 유지.
        /// </summary>
        public static void AddLaunchers(Control.ControlCollection actions, IWin32Window owner, Control stopButton, bool includeSide)
        {
            if (actions == null) return;

            if (includeSide)
                actions.Add(MakeButton("VISION: SIDE(동시)", (s, e) => SideVisionViewerDialog.Open(owner)));

            actions.Add(MakeButton("VISION VIEW", (s, e) => Open(owner)));

            if (stopButton != null && actions.Contains(stopButton))
                actions.SetChildIndex(stopButton, actions.Count - 1);
        }

        private static Control MakeButton(string text, EventHandler onClick)
        {
            var b = new QMC.CDT_320.Ui.Controls.ActionButton
            {
                Text = text,
                Width = 180,
                Height = 64,
                Margin = new Padding(6),
                Font = new Font("맑은 고딕", 11F)
            };
            b.Click += onClick;
            return b;
        }
    }
}
