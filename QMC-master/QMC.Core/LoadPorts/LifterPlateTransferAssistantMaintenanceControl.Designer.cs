namespace QMC.LoadPorts
{
    partial class LifterPlateTransferAssistantMaintenanceControl
    {
        /// <summary> 
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 구성 요소 디자이너에서 생성한 코드

        /// <summary> 
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LifterPlateTransferAssistantMaintenanceControl));
            this.slotStateView1 = new MechaSys.SoftBricks.Materials.Controls.SlotStateView();
            this.slotStateView2 = new MechaSys.SoftBricks.Materials.Controls.SlotStateView();
            this.flowLayoutPanelXPlates = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.flowLayoutPanelXCarrierPort = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.groupBoxXMaterialDetector = new MechaSys.SoftBricks.Hmi.Controls.GroupBoxX();
            this.digitalGroupBoxMaterialDetector = new MechaSys.SoftBricks.IO.Controls.DigitalGroupBox();
            this.groupBoxXMoveToSlot = new MechaSys.SoftBricks.Hmi.Controls.GroupBoxX();
            this.buttonXMoveToSlot = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.comboBoxXSlot = new MechaSys.SoftBricks.Hmi.Controls.ComboBoxX();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.carrierLockerControl1 = new QMC.Hmi.Controls.CarrierLockerControl();
            this.panelX1 = new MechaSys.SoftBricks.Hmi.Controls.PanelX();
            this.digitalBoxProtrusionSensor = new MechaSys.SoftBricks.IO.Controls.DigitalBox();
            this.actionPositionControl1 = new QMC.Hmi.Controls.ActionPositionControl();
            this.groupBoxXMaterialDetector.SuspendLayout();
            this.groupBoxXMoveToSlot.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.panelX1.SuspendLayout();
            this.SuspendLayout();
            // 
            // slotStateView1
            // 
            this.slotStateView1.BackColor = System.Drawing.SystemColors.Control;
            this.slotStateView1.Carrier = null;
            this.slotStateView1.Dock = System.Windows.Forms.DockStyle.Left;
            this.slotStateView1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView1.Location = new System.Drawing.Point(3, 3);
            this.slotStateView1.Name = "slotStateView1";
            this.slotStateView1.ProcessAborted = System.Drawing.Color.HotPink;
            this.slotStateView1.ProcessInProcess = System.Drawing.Color.LimeGreen;
            this.slotStateView1.ProcessLost = System.Drawing.Color.DarkKhaki;
            this.slotStateView1.ProcessNeedsProcessing = System.Drawing.Color.LightGreen;
            this.slotStateView1.ProcessNone = System.Drawing.Color.DarkGray;
            this.slotStateView1.ProcessNotExist = System.Drawing.Color.Gray;
            this.slotStateView1.ProcessProcessed = System.Drawing.Color.SeaGreen;
            this.slotStateView1.ProcessRejected = System.Drawing.Color.Thistle;
            this.slotStateView1.ProcessSkipped = System.Drawing.Color.Plum;
            this.slotStateView1.ProcessStopped = System.Drawing.Color.LightPink;
            this.slotStateView1.Size = new System.Drawing.Size(180, 740);
            this.slotStateView1.SlotCrossSlotted = System.Drawing.Color.DarkOrange;
            this.slotStateView1.SlotDoubleSlotted = System.Drawing.Color.HotPink;
            this.slotStateView1.SlotEmpty = System.Drawing.Color.Gray;
            this.slotStateView1.SlotFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView1.SlotForeColor = System.Drawing.SystemColors.ControlText;
            this.slotStateView1.SlotNoBackColor = System.Drawing.SystemColors.Control;
            this.slotStateView1.SlotNoFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView1.SlotNoForeColor = System.Drawing.SystemColors.ControlText;
            this.slotStateView1.SlotNotEmpty = System.Drawing.Color.Sienna;
            this.slotStateView1.SlotOccupied = System.Drawing.Color.LimeGreen;
            this.slotStateView1.SlotPadding = new System.Windows.Forms.Padding(2);
            this.slotStateView1.SlotUndefined = System.Drawing.Color.Chocolate;
            this.slotStateView1.TabIndex = 4;
            // 
            // slotStateView2
            // 
            this.slotStateView2.BackColor = System.Drawing.SystemColors.Control;
            this.slotStateView2.Carrier = null;
            this.slotStateView2.Dock = System.Windows.Forms.DockStyle.Left;
            this.slotStateView2.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView2.Location = new System.Drawing.Point(183, 3);
            this.slotStateView2.Name = "slotStateView2";
            this.slotStateView2.ProcessAborted = System.Drawing.Color.HotPink;
            this.slotStateView2.ProcessInProcess = System.Drawing.Color.LimeGreen;
            this.slotStateView2.ProcessLost = System.Drawing.Color.DarkKhaki;
            this.slotStateView2.ProcessNeedsProcessing = System.Drawing.Color.LightGreen;
            this.slotStateView2.ProcessNone = System.Drawing.Color.DarkGray;
            this.slotStateView2.ProcessNotExist = System.Drawing.Color.Gray;
            this.slotStateView2.ProcessProcessed = System.Drawing.Color.SeaGreen;
            this.slotStateView2.ProcessRejected = System.Drawing.Color.Thistle;
            this.slotStateView2.ProcessSkipped = System.Drawing.Color.Plum;
            this.slotStateView2.ProcessStopped = System.Drawing.Color.LightPink;
            this.slotStateView2.Size = new System.Drawing.Size(180, 740);
            this.slotStateView2.SlotCrossSlotted = System.Drawing.Color.DarkOrange;
            this.slotStateView2.SlotDoubleSlotted = System.Drawing.Color.HotPink;
            this.slotStateView2.SlotEmpty = System.Drawing.Color.Gray;
            this.slotStateView2.SlotFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView2.SlotForeColor = System.Drawing.SystemColors.ControlText;
            this.slotStateView2.SlotNoBackColor = System.Drawing.SystemColors.Control;
            this.slotStateView2.SlotNoFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView2.SlotNoForeColor = System.Drawing.SystemColors.ControlText;
            this.slotStateView2.SlotNotEmpty = System.Drawing.Color.Sienna;
            this.slotStateView2.SlotOccupied = System.Drawing.Color.LimeGreen;
            this.slotStateView2.SlotPadding = new System.Windows.Forms.Padding(2);
            this.slotStateView2.SlotUndefined = System.Drawing.Color.Chocolate;
            this.slotStateView2.TabIndex = 5;
            // 
            // flowLayoutPanelXPlates
            // 
            this.flowLayoutPanelXPlates.ContentAlign = System.Drawing.ContentAlignment.TopCenter;
            this.flowLayoutPanelXPlates.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowLayoutPanelXPlates.Gap = 100;
            this.flowLayoutPanelXPlates.Location = new System.Drawing.Point(369, 6);
            this.flowLayoutPanelXPlates.Name = "flowLayoutPanelXPlates";
            this.flowLayoutPanelXPlates.Size = new System.Drawing.Size(207, 40);
            this.flowLayoutPanelXPlates.TabIndex = 6;
            // 
            // flowLayoutPanelXCarrierPort
            // 
            this.flowLayoutPanelXCarrierPort.ContentAlign = System.Drawing.ContentAlignment.TopCenter;
            this.flowLayoutPanelXCarrierPort.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowLayoutPanelXCarrierPort.Gap = 100;
            this.flowLayoutPanelXCarrierPort.Location = new System.Drawing.Point(369, 52);
            this.flowLayoutPanelXCarrierPort.Name = "flowLayoutPanelXCarrierPort";
            this.flowLayoutPanelXCarrierPort.Size = new System.Drawing.Size(207, 40);
            this.flowLayoutPanelXCarrierPort.TabIndex = 7;
            // 
            // groupBoxXMaterialDetector
            // 
            this.groupBoxXMaterialDetector.Controls.Add(this.digitalGroupBoxMaterialDetector);
            this.groupBoxXMaterialDetector.Location = new System.Drawing.Point(352, 84);
            this.groupBoxXMaterialDetector.Name = "groupBoxXMaterialDetector";
            this.groupBoxXMaterialDetector.Size = new System.Drawing.Size(165, 147);
            this.groupBoxXMaterialDetector.TabIndex = 11;
            this.groupBoxXMaterialDetector.TabStop = false;
            this.groupBoxXMaterialDetector.Text = "Material detector";
            // 
            // digitalGroupBoxMaterialDetector
            // 
            this.digitalGroupBoxMaterialDetector.ContentAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.digitalGroupBoxMaterialDetector.Dock = System.Windows.Forms.DockStyle.Fill;
            this.digitalGroupBoxMaterialDetector.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.digitalGroupBoxMaterialDetector.Gap = 30;
            this.digitalGroupBoxMaterialDetector.Location = new System.Drawing.Point(3, 18);
            this.digitalGroupBoxMaterialDetector.Name = "digitalGroupBoxMaterialDetector";
            this.digitalGroupBoxMaterialDetector.Points = new MechaSys.SoftBricks.IO.DioPoint[0];
            this.digitalGroupBoxMaterialDetector.Size = new System.Drawing.Size(159, 126);
            this.digitalGroupBoxMaterialDetector.TabIndex = 0;
            // 
            // groupBoxXMoveToSlot
            // 
            this.groupBoxXMoveToSlot.Controls.Add(this.buttonXMoveToSlot);
            this.groupBoxXMoveToSlot.Controls.Add(this.comboBoxXSlot);
            this.groupBoxXMoveToSlot.Location = new System.Drawing.Point(369, 98);
            this.groupBoxXMoveToSlot.Name = "groupBoxXMoveToSlot";
            this.groupBoxXMoveToSlot.Size = new System.Drawing.Size(211, 64);
            this.groupBoxXMoveToSlot.TabIndex = 12;
            this.groupBoxXMoveToSlot.TabStop = false;
            this.groupBoxXMoveToSlot.Text = "Move to slot";
            // 
            // buttonXMoveToSlot
            // 
            this.buttonXMoveToSlot.Location = new System.Drawing.Point(106, 18);
            this.buttonXMoveToSlot.Name = "buttonXMoveToSlot";
            this.buttonXMoveToSlot.Size = new System.Drawing.Size(100, 40);
            this.buttonXMoveToSlot.TabIndex = 1;
            this.buttonXMoveToSlot.Text = "Move";
            this.buttonXMoveToSlot.UseVisualStyleBackColor = true;
            this.buttonXMoveToSlot.Click += new System.EventHandler(this.OperationButton_Click);
            // 
            // comboBoxXSlot
            // 
            this.comboBoxXSlot.FormattingEnabled = true;
            this.comboBoxXSlot.IntegralHeight = false;
            this.comboBoxXSlot.ItemHeight = 30;
            this.comboBoxXSlot.Location = new System.Drawing.Point(6, 21);
            this.comboBoxXSlot.Name = "comboBoxXSlot";
            this.comboBoxXSlot.Size = new System.Drawing.Size(94, 36);
            this.comboBoxXSlot.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.carrierLockerControl1);
            this.flowLayoutPanel1.Controls.Add(this.groupBoxXMaterialDetector);
            this.flowLayoutPanel1.Controls.Add(this.panelX1);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(601, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(520, 740);
            this.flowLayoutPanel1.TabIndex = 13;
            // 
            // carrierLockerControl1
            // 
            this.carrierLockerControl1.CarrierLocker = null;
            this.carrierLockerControl1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.carrierLockerControl1.Location = new System.Drawing.Point(102, 3);
            this.carrierLockerControl1.Name = "carrierLockerControl1";
            this.carrierLockerControl1.Padding = new System.Windows.Forms.Padding(3);
            this.carrierLockerControl1.Size = new System.Drawing.Size(415, 75);
            this.carrierLockerControl1.TabIndex = 13;
            // 
            // panelX1
            // 
            this.panelX1.Controls.Add(this.digitalBoxProtrusionSensor);
            this.panelX1.Location = new System.Drawing.Point(-3, 237);
            this.panelX1.Name = "panelX1";
            this.panelX1.Size = new System.Drawing.Size(520, 28);
            this.panelX1.TabIndex = 14;
            // 
            // digitalBoxProtrusionSensor
            // 
            this.digitalBoxProtrusionSensor.DioPoint = null;
            this.digitalBoxProtrusionSensor.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.digitalBoxProtrusionSensor.ImageReSize = false;
            this.digitalBoxProtrusionSensor.ImageSize = new System.Drawing.Size(20, 20);
            this.digitalBoxProtrusionSensor.ImageStyle = MechaSys.SoftBricks.IO.Controls.DigitalBox.ImageStyles.Led;
            this.digitalBoxProtrusionSensor.Location = new System.Drawing.Point(320, 0);
            this.digitalBoxProtrusionSensor.Name = "digitalBoxProtrusionSensor";
            this.digitalBoxProtrusionSensor.OffImage = null;
            this.digitalBoxProtrusionSensor.OnImage = null;
            this.digitalBoxProtrusionSensor.Padding = new System.Windows.Forms.Padding(3);
            this.digitalBoxProtrusionSensor.Size = new System.Drawing.Size(200, 28);
            this.digitalBoxProtrusionSensor.TabIndex = 0;
            this.digitalBoxProtrusionSensor.Text = "digitalBox1";
            this.digitalBoxProtrusionSensor.TextAlignMargin = 5;
            this.digitalBoxProtrusionSensor.Value = false;
            // 
            // actionPositionControl1
            // 
            this.actionPositionControl1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.actionPositionControl1.Location = new System.Drawing.Point(369, 168);
            this.actionPositionControl1.Name = "actionPositionControl1";
            this.actionPositionControl1.OmittedKeys = ((System.Collections.Generic.List<string>)(resources.GetObject("actionPositionControl1.OmittedKeys")));
            this.actionPositionControl1.Padding = new System.Windows.Forms.Padding(3);
            this.actionPositionControl1.Size = new System.Drawing.Size(221, 182);
            this.actionPositionControl1.TabIndex = 8;
            this.actionPositionControl1.Title = null;
            // 
            // LifterPlateTransferAssistantMaintenanceControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.groupBoxXMoveToSlot);
            this.Controls.Add(this.actionPositionControl1);
            this.Controls.Add(this.flowLayoutPanelXCarrierPort);
            this.Controls.Add(this.flowLayoutPanelXPlates);
            this.Controls.Add(this.slotStateView2);
            this.Controls.Add(this.slotStateView1);
            this.Name = "LifterPlateTransferAssistantMaintenanceControl";
            this.groupBoxXMaterialDetector.ResumeLayout(false);
            this.groupBoxXMoveToSlot.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.panelX1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private MechaSys.SoftBricks.Materials.Controls.SlotStateView slotStateView1;
        private MechaSys.SoftBricks.Materials.Controls.SlotStateView slotStateView2;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelXPlates;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelXCarrierPort;
        private Hmi.Controls.ActionPositionControl actionPositionControl1;
        private MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxXMaterialDetector;
        private MechaSys.SoftBricks.IO.Controls.DigitalGroupBox digitalGroupBoxMaterialDetector;
        private MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxXMoveToSlot;
        private MechaSys.SoftBricks.Hmi.Controls.ComboBoxX comboBoxXSlot;
        private Hmi.Controls.CarrierLockerControl carrierLockerControl1;
        protected System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private MechaSys.SoftBricks.Hmi.Controls.PanelX panelX1;
        private MechaSys.SoftBricks.IO.Controls.DigitalBox digitalBoxProtrusionSensor;
        private MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonXMoveToSlot;
    }
}
