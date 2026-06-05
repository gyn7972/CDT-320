using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Tabs
{
    partial class SettingsTab
    {
        // ── 주 메뉴 버튼 ─────────────────────────────
        internal SidebarButton BtnGeneral;
        internal SidebarButton BtnMotion;
        internal SidebarButton BtnIoControl;
        internal SidebarButton BtnDigital;
        internal SidebarButton BtnCylinder;
        internal SidebarButton BtnLamp;
        internal SidebarButton BtnSwitch;
        internal SidebarButton BtnLightSource;

        // ── 보조 메뉴 그룹 구분선 ────────────────────
        internal System.Windows.Forms.Panel PnlSecondarySeparator;

        // ── 보조 메뉴 버튼 ───────────────────────────
        internal SidebarButton BtnBarcode;
        internal SidebarButton BtnZoomLens;
        internal SidebarButton BtnHeightSensor;
        internal SidebarButton BtnSimulator;
        internal SidebarButton BtnVisionLink;
        internal SidebarButton BtnSelfTest;
        internal SidebarButton BtnAlarmMaster;
        internal SidebarButton BtnTeach;
        internal SidebarButton BtnAxisSetup;
        internal SidebarButton BtnCameraSetup;
        internal SidebarButton BtnLightSetup;
        internal SidebarButton BtnRemoteViewer;

        private void InitializeComponent()
        {
            this.BtnGeneral = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnMotion = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnIoControl = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnDigital = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnCylinder = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnLamp = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnSwitch = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnLightSource = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.PnlSecondarySeparator = new System.Windows.Forms.Panel();
            this.BtnBarcode = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnZoomLens = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnHeightSensor = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnSimulator = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnVisionLink = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnSelfTest = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnAlarmMaster = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnTeach = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnAxisSetup = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnCameraSetup = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnLightSetup = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnRemoteViewer = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.PnlSidebar.SuspendLayout();
            this.SuspendLayout();
            //
            // PnlSidebar
            //
            this.PnlSidebar.Location = new System.Drawing.Point(1678, 0);
            this.PnlSidebar.Size = new System.Drawing.Size(210, 900);
            //
            // LblSidebarHeader
            //
            this.LblSidebarHeader.Size = new System.Drawing.Size(210, 50);
            //
            // PnlContent
            //
            this.PnlContent.Size = new System.Drawing.Size(1678, 900);
            //
            // BtnGeneral
            //
            this.BtnGeneral.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnGeneral.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnGeneral.Location = new System.Drawing.Point(4, 6);
            this.BtnGeneral.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnGeneral.Name = "BtnGeneral";
            this.BtnGeneral.Selected = false;
            this.BtnGeneral.Size = new System.Drawing.Size(184, 46);
            this.BtnGeneral.TabIndex = 0;
            this.BtnGeneral.Text = "General";
            //
            // BtnMotion
            //
            this.BtnMotion.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnMotion.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnMotion.Location = new System.Drawing.Point(4, 54);
            this.BtnMotion.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnMotion.Name = "BtnMotion";
            this.BtnMotion.Selected = false;
            this.BtnMotion.Size = new System.Drawing.Size(184, 46);
            this.BtnMotion.TabIndex = 1;
            this.BtnMotion.Text = "Motion";
            //
            // BtnIoControl
            //
            this.BtnIoControl.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnIoControl.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnIoControl.Location = new System.Drawing.Point(4, 102);
            this.BtnIoControl.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnIoControl.Name = "BtnIoControl";
            this.BtnIoControl.Selected = false;
            this.BtnIoControl.Size = new System.Drawing.Size(184, 46);
            this.BtnIoControl.TabIndex = 2;
            this.BtnIoControl.Text = "I/O Control";
            //
            // BtnDigital
            //
            this.BtnDigital.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnDigital.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnDigital.Location = new System.Drawing.Point(4, 150);
            this.BtnDigital.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnDigital.Name = "BtnDigital";
            this.BtnDigital.Selected = false;
            this.BtnDigital.Size = new System.Drawing.Size(184, 46);
            this.BtnDigital.TabIndex = 3;
            this.BtnDigital.Text = "Digital";
            //
            // BtnCylinder
            //
            this.BtnCylinder.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnCylinder.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnCylinder.Location = new System.Drawing.Point(4, 198);
            this.BtnCylinder.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnCylinder.Name = "BtnCylinder";
            this.BtnCylinder.Selected = false;
            this.BtnCylinder.Size = new System.Drawing.Size(184, 46);
            this.BtnCylinder.TabIndex = 4;
            this.BtnCylinder.Text = "Cylinder";
            //
            // BtnLamp
            //
            this.BtnLamp.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnLamp.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnLamp.Location = new System.Drawing.Point(4, 246);
            this.BtnLamp.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnLamp.Name = "BtnLamp";
            this.BtnLamp.Selected = false;
            this.BtnLamp.Size = new System.Drawing.Size(184, 46);
            this.BtnLamp.TabIndex = 5;
            this.BtnLamp.Text = "Lamp";
            //
            // BtnSwitch
            //
            this.BtnSwitch.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnSwitch.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnSwitch.Location = new System.Drawing.Point(4, 294);
            this.BtnSwitch.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnSwitch.Name = "BtnSwitch";
            this.BtnSwitch.Selected = false;
            this.BtnSwitch.Size = new System.Drawing.Size(184, 46);
            this.BtnSwitch.TabIndex = 6;
            this.BtnSwitch.Text = "Switch";
            //
            // BtnLightSource
            //
            this.BtnLightSource.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnLightSource.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnLightSource.Location = new System.Drawing.Point(4, 342);
            this.BtnLightSource.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnLightSource.Name = "BtnLightSource";
            this.BtnLightSource.Selected = false;
            this.BtnLightSource.Size = new System.Drawing.Size(184, 46);
            this.BtnLightSource.TabIndex = 7;
            this.BtnLightSource.Text = "Light Source";
            //
            // PnlSecondarySeparator
            //
            this.PnlSecondarySeparator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(51)))), ((int)(((byte)(51)))));
            this.PnlSecondarySeparator.Location = new System.Drawing.Point(4, 396);
            this.PnlSecondarySeparator.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.PnlSecondarySeparator.Name = "PnlSecondarySeparator";
            this.PnlSecondarySeparator.Size = new System.Drawing.Size(202, 2);
            this.PnlSecondarySeparator.TabIndex = 8;
            //
            // BtnBarcode
            //
            this.BtnBarcode.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnBarcode.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnBarcode.Location = new System.Drawing.Point(4, 404);
            this.BtnBarcode.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnBarcode.Name = "BtnBarcode";
            this.BtnBarcode.Selected = false;
            this.BtnBarcode.Size = new System.Drawing.Size(184, 46);
            this.BtnBarcode.TabIndex = 9;
            this.BtnBarcode.Text = "Barcode Reader";
            //
            // BtnZoomLens
            //
            this.BtnZoomLens.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnZoomLens.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnZoomLens.Location = new System.Drawing.Point(4, 452);
            this.BtnZoomLens.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnZoomLens.Name = "BtnZoomLens";
            this.BtnZoomLens.Selected = false;
            this.BtnZoomLens.Size = new System.Drawing.Size(184, 46);
            this.BtnZoomLens.TabIndex = 10;
            this.BtnZoomLens.Text = "Zoom Lens";
            //
            // BtnHeightSensor
            //
            this.BtnHeightSensor.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnHeightSensor.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnHeightSensor.Location = new System.Drawing.Point(4, 500);
            this.BtnHeightSensor.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnHeightSensor.Name = "BtnHeightSensor";
            this.BtnHeightSensor.Selected = false;
            this.BtnHeightSensor.Size = new System.Drawing.Size(184, 46);
            this.BtnHeightSensor.TabIndex = 11;
            this.BtnHeightSensor.Text = "Height Sensor";
            //
            // BtnSimulator
            //
            this.BtnSimulator.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnSimulator.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnSimulator.Location = new System.Drawing.Point(4, 548);
            this.BtnSimulator.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnSimulator.Name = "BtnSimulator";
            this.BtnSimulator.Selected = false;
            this.BtnSimulator.Size = new System.Drawing.Size(184, 46);
            this.BtnSimulator.TabIndex = 12;
            this.BtnSimulator.Text = "Simulator Link";
            //
            // BtnVisionLink
            //
            this.BtnVisionLink.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnVisionLink.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnVisionLink.Location = new System.Drawing.Point(4, 596);
            this.BtnVisionLink.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnVisionLink.Name = "BtnVisionLink";
            this.BtnVisionLink.Selected = false;
            this.BtnVisionLink.Size = new System.Drawing.Size(184, 46);
            this.BtnVisionLink.TabIndex = 13;
            this.BtnVisionLink.Text = "Vision Link";
            //
            // BtnSelfTest
            //
            this.BtnSelfTest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnSelfTest.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnSelfTest.Location = new System.Drawing.Point(4, 644);
            this.BtnSelfTest.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnSelfTest.Name = "BtnSelfTest";
            this.BtnSelfTest.Selected = false;
            this.BtnSelfTest.Size = new System.Drawing.Size(184, 46);
            this.BtnSelfTest.TabIndex = 14;
            this.BtnSelfTest.Text = "Self-Test";
            //
            // BtnAlarmMaster
            //
            this.BtnAlarmMaster.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnAlarmMaster.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnAlarmMaster.Location = new System.Drawing.Point(4, 692);
            this.BtnAlarmMaster.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnAlarmMaster.Name = "BtnAlarmMaster";
            this.BtnAlarmMaster.Selected = false;
            this.BtnAlarmMaster.Size = new System.Drawing.Size(184, 46);
            this.BtnAlarmMaster.TabIndex = 15;
            this.BtnAlarmMaster.Text = "Alarm Master";
            //
            // BtnTeach
            //
            this.BtnTeach.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnTeach.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnTeach.Location = new System.Drawing.Point(4, 740);
            this.BtnTeach.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnTeach.Name = "BtnTeach";
            this.BtnTeach.Selected = false;
            this.BtnTeach.Size = new System.Drawing.Size(184, 46);
            this.BtnTeach.TabIndex = 16;
            this.BtnTeach.Text = "Position Teach";
            //
            // BtnAxisSetup
            //
            this.BtnAxisSetup.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnAxisSetup.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnAxisSetup.Location = new System.Drawing.Point(4, 788);
            this.BtnAxisSetup.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnAxisSetup.Name = "BtnAxisSetup";
            this.BtnAxisSetup.Selected = false;
            this.BtnAxisSetup.Size = new System.Drawing.Size(184, 46);
            this.BtnAxisSetup.TabIndex = 17;
            this.BtnAxisSetup.Text = "Axis Setup";
            //
            // BtnCameraSetup
            //
            this.BtnCameraSetup.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnCameraSetup.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnCameraSetup.Location = new System.Drawing.Point(4, 836);
            this.BtnCameraSetup.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnCameraSetup.Name = "BtnCameraSetup";
            this.BtnCameraSetup.Selected = false;
            this.BtnCameraSetup.Size = new System.Drawing.Size(202, 46);
            this.BtnCameraSetup.TabIndex = 18;
            this.BtnCameraSetup.Text = "Camera Setup";
            //
            // BtnLightSetup
            //
            this.BtnLightSetup.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnLightSetup.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnLightSetup.Location = new System.Drawing.Point(4, 884);
            this.BtnLightSetup.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnLightSetup.Name = "BtnLightSetup";
            this.BtnLightSetup.Selected = false;
            this.BtnLightSetup.Size = new System.Drawing.Size(202, 46);
            this.BtnLightSetup.TabIndex = 19;
            this.BtnLightSetup.Text = "Light Setup";
            //
            // BtnRemoteViewer
            //
            this.BtnRemoteViewer.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnRemoteViewer.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnRemoteViewer.Location = new System.Drawing.Point(4, 932);
            this.BtnRemoteViewer.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnRemoteViewer.Name = "BtnRemoteViewer";
            this.BtnRemoteViewer.Selected = false;
            this.BtnRemoteViewer.Size = new System.Drawing.Size(202, 46);
            this.BtnRemoteViewer.TabIndex = 20;
            this.BtnRemoteViewer.Text = "Remote Viewer";
            this.PnlSidebarButtons.Controls.Add(this.BtnGeneral);
            this.PnlSidebarButtons.Controls.Add(this.BtnMotion);
            this.PnlSidebarButtons.Controls.Add(this.BtnIoControl);
            this.PnlSidebarButtons.Controls.Add(this.BtnDigital);
            this.PnlSidebarButtons.Controls.Add(this.BtnCylinder);
            this.PnlSidebarButtons.Controls.Add(this.BtnLamp);
            this.PnlSidebarButtons.Controls.Add(this.BtnSwitch);
            this.PnlSidebarButtons.Controls.Add(this.BtnLightSource);
            this.PnlSidebarButtons.Controls.Add(this.PnlSecondarySeparator);
            this.PnlSidebarButtons.Controls.Add(this.BtnBarcode);
            this.PnlSidebarButtons.Controls.Add(this.BtnZoomLens);
            this.PnlSidebarButtons.Controls.Add(this.BtnHeightSensor);
            this.PnlSidebarButtons.Controls.Add(this.BtnSimulator);
            this.PnlSidebarButtons.Controls.Add(this.BtnVisionLink);
            this.PnlSidebarButtons.Controls.Add(this.BtnSelfTest);
            this.PnlSidebarButtons.Controls.Add(this.BtnAlarmMaster);
            this.PnlSidebarButtons.Controls.Add(this.BtnTeach);
            this.PnlSidebarButtons.Controls.Add(this.BtnAxisSetup);
            this.PnlSidebarButtons.Controls.Add(this.BtnCameraSetup);
            this.PnlSidebarButtons.Controls.Add(this.BtnLightSetup);
            this.PnlSidebarButtons.Controls.Add(this.BtnRemoteViewer);
            //
            // SettingsTab
            //
            this.Name = "SettingsTab";
            this.PnlSidebar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}

