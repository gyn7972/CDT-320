using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// 통합 Vision View — 모듈별 이미지 뷰어를 탭으로 모아 보여준다(웨이퍼/바텀/빈/측면 동시).
    /// 각 탭은 <see cref="VisionViewerPanel"/>(또는 측면은 <see cref="SideVisionViewerControl"/>)로,
    /// 내장 툴바 Grab/Live/Stop + 메타 오버레이 + TCP GRAB을 지원한다. 표시 전용 — 모션 무관.
    /// 코드 전용 폼(Designer 없음).
    /// </summary>
    public sealed class VisionViewDialog : Form
    {
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

        public VisionViewDialog()
        {
            Text = "Vision View — 통합 카메라 뷰";
            Font = new Font("맑은 고딕", 9F);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            ClientSize = new Size(1100, 760);
            MinimumSize = new Size(900, 560);
            BackColor = Color.White;

            string host = VisionHub.Host;
            var tabs = new TabControl { Dock = DockStyle.Fill };

            tabs.TabPages.Add(MakeTab("Wafer Vision",
                new VisionViewerPanel(host, VisionViewerPorts.Wafer, "Wafer", VisionHub.Wafer)));
            tabs.TabPages.Add(MakeTab("Bottom Insp",
                new VisionViewerPanel(host, VisionViewerPorts.BottomInspection, "Bottom Inspection", VisionHub.Inspection)));
            tabs.TabPages.Add(MakeTab("Bin Vision",
                new VisionViewerPanel(host, VisionViewerPorts.Bin, "Bin", VisionHub.Bin)));
            tabs.TabPages.Add(MakeTab("Side (Front+Rear)",
                new SideVisionViewerControl(host)));

            Controls.Add(tabs);
        }

        private static TabPage MakeTab(string title, Control content)
        {
            var page = new TabPage(title);
            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);
            return page;
        }
    }

    /// <summary>측면 Front+Rear 동시 뷰어 팝업.</summary>
    public sealed class SideVisionViewerDialog : Form
    {
        public static void Open(IWin32Window owner)
        {
            using (var dlg = new SideVisionViewerDialog())
                dlg.ShowDialog(owner);
        }

        public SideVisionViewerDialog()
        {
            Text = "VISION 측면 뷰어 — Front + Rear 동시";
            Font = new Font("맑은 고딕", 9F);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            ClientSize = new Size(900, 820);
            MinimumSize = new Size(700, 560);
            BackColor = Color.White;

            var side = new SideVisionViewerControl(VisionHub.Host) { Dock = DockStyle.Fill };
            Controls.Add(side);
        }
    }
}
