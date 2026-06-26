using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class TpuVisionTestDialog : Form
    {
        public enum Mode
        {
            BottomInspection,
            Side
        }

        public static void Open(
            IWin32Window owner,
            string title,
            Mode mode,
            int pickerNo = 1,
            Func<VisionTcpClient> sideClient = null,
            int sideViewerPort = 0,
            string sideInspectorId = null)
        {
            string key = "TpuVisionTestDialog:" +
                         (title ?? "Unknown") + ":" +
                         mode + ":" +
                         pickerNo + ":" +
                         sideViewerPort + ":" +
                         (sideInspectorId ?? string.Empty);

            ModelessDialogHost.Show(
                key,
                owner,
                () => new TpuVisionTestDialog(title, mode, pickerNo, sideClient, sideViewerPort, sideInspectorId));
        }

        public static void AddLaunchers(Control.ControlCollection actions, IWin32Window owner, Control stopButton)
        {
            if (actions == null)
                return;

            void Add(string label, Action open)
            {
                var button = new ActionButton
                {
                    Text = label,
                    Width = 132,
                    Height = 60,
                    Margin = new Padding(6),
                    Font = new Font("맑은 고딕", 11F)
                };
                button.Click += (s, e) => open();
                actions.Add(button);
            }

            Add("VISION: BOTTOM INSP", () => Open(owner, "Bottom Inspection", Mode.BottomInspection));
            Add("VISION: TOP SIDE", () => Open(owner, "Top Side", Mode.Side, 1, () => VisionHub.TopSide, VisionViewerPorts.TopSide, "TopSurfaceInspector"));
            Add("VISION: BOTTOM SIDE", () => Open(owner, "Bottom Side", Mode.Side, 1, () => VisionHub.BottomSide, VisionViewerPorts.BottomSide, "BottomSurfaceInspector"));

            if (stopButton != null && actions.Contains(stopButton))
                actions.SetChildIndex(stopButton, actions.Count - 1);
        }

        public TpuVisionTestDialog()
        {
            InitializeComponent();
        }

        public TpuVisionTestDialog(
            string title,
            Mode mode,
            int pickerNo,
            Func<VisionTcpClient> sideClient,
            int sideViewerPort,
            string sideInspectorId)
        {
            InitializeComponent();

            tpuVisionTestControl.Configure(
                title,
                mode == Mode.BottomInspection
                    ? TpuVisionTestControl.Mode.BottomInspection
                    : TpuVisionTestControl.Mode.Side,
                pickerNo,
                sideClient,
                sideViewerPort,
                sideInspectorId);

            Text = tpuVisionTestControl.DialogTitle;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            tpuVisionTestControl.StopLive();
            base.OnFormClosing(e);
        }
    }
}
