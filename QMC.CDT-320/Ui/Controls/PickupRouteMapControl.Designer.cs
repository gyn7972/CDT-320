namespace QMC.CDT_320.Ui.Controls
{
    partial class PickupRouteMapControl
    {
        private void InitializeComponent()
        {
            SuspendLayout();
            BackColor = System.Drawing.Color.White;
            Font = new System.Drawing.Font("맑은 고딕", 8F);
            Name = "PickupRouteMapControl";
            Size = new System.Drawing.Size(690, 485);
            TabStop = true;
            MouseDown += PickupRouteMapControl_MouseDown;
            MouseMove += PickupRouteMapControl_MouseMove;
            MouseUp += PickupRouteMapControl_MouseUp;
            MouseCaptureChanged += PickupRouteMapControl_MouseCaptureChanged;
            MouseWheel += PickupRouteMapControl_MouseWheel;
            Resize += PickupRouteMapControl_Resize;
            ResumeLayout(false);
        }
    }
}
