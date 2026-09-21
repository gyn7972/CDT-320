using QMC.CDT_320.Ui.Localization;
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
            string sideInspectorId = null,
            int pickerFb = 0)
        {
            string key = "TpuVisionTestDialog:" +
                         (title ?? "Unknown") + ":" +
                         mode + ":" +
                         pickerNo + ":" +
                         pickerFb + ":" +
                         sideViewerPort + ":" +
                         (sideInspectorId ?? string.Empty);

            ModelessDialogHost.Show(
                key,
                owner,
                () => new TpuVisionTestDialog(title, mode, pickerNo, sideClient, sideViewerPort, sideInspectorId, pickerFb));
        }

        public static void AddLaunchers(Control.ControlCollection actions, IWin32Window owner, Control stopButton)
        {
            if (actions == null)
                return;

            void Add(string labelKey, Action open)
            {
                var button = new ActionButton
                {
                    Text = Lang.T(labelKey),
                    Width = 132,
                    Height = 60,
                    Margin = new Padding(6),
                    Font = new Font("맑은 고딕", 11F)
                };
                Lang.BindKey(button, labelKey);
                button.Click += (s, e) => open();
                actions.Add(button);
            }

            Add("visionUi.launch.bottom", () => Open(owner, "Bottom Inspection", Mode.BottomInspection));
            Add("visionUi.launch.front", () => Open(owner, "FrontSideVision", Mode.Side, 1, () => VisionHub.FrontSideVision, VisionViewerPorts.FrontSideVision, VisionToolIds.FrontSide.SurfaceInspector));
            Add("visionUi.launch.rear", () => Open(owner, "RearSideVision", Mode.Side, 1, () => VisionHub.RearSideVision, VisionViewerPorts.RearSideVision, VisionToolIds.RearSide.SurfaceInspector));

            if (stopButton != null && actions.Contains(stopButton))
                actions.SetChildIndex(stopButton, actions.Count - 1);
        }

        public TpuVisionTestDialog()
        {
            InitializeComponent();
            InitializeLanguageBindings();
        }

        public TpuVisionTestDialog(
            string title,
            Mode mode,
            int pickerNo,
            Func<VisionTcpClient> sideClient,
            int sideViewerPort,
            string sideInspectorId,
            int pickerFb)
        {
            InitializeComponent();
            InitializeLanguageBindings();

            tpuVisionTestControl.Configure(
                title,
                mode == Mode.BottomInspection
                    ? TpuVisionTestControl.Mode.BottomInspection
                    : TpuVisionTestControl.Mode.Side,
                pickerNo,
                sideClient,
                sideViewerPort,
                sideInspectorId,
                pickerFb);

            tpuVisionTestControl.BindDialogTitle(this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            tpuVisionTestControl.StopLive();
            base.OnFormClosing(e);
        }
        // Keep Designer serialization declarative; register display resources after controls exist.
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this, "visionUi.tpuVisionTestDialog.Text.text");
        }
    }
}
