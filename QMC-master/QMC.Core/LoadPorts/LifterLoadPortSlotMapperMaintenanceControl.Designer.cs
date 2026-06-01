namespace QMC.LoadPorts
{
    partial class LifterLoadPortSlotMapperMaintenanceControl
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
            if (disposing && (components != null))
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
            this.radioButtonPlate0 = new MechaSys.SoftBricks.Hmi.Controls.RadioButtonX();
            this.radioButtonPlate1 = new MechaSys.SoftBricks.Hmi.Controls.RadioButtonX();
            this.radioButtonCarrierBelow = new MechaSys.SoftBricks.Hmi.Controls.RadioButtonX();
            this.radioButtonCarrierAbove = new MechaSys.SoftBricks.Hmi.Controls.RadioButtonX();
            this.flowLayoutPanelXCarrierPort = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.flowLayoutPanelXPlates = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.groupBoxMoveToSlot.SuspendLayout();
            this.groupBoxPosition.SuspendLayout();
            this.flowLayoutPanelPositions.SuspendLayout();
            this.flowLayoutPanelXCarrierPort.SuspendLayout();
            this.flowLayoutPanelXPlates.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanelSensors
            // 
            this.flowLayoutPanelSensors.Location = new System.Drawing.Point(241, 345);
            // 
            // groupBoxMoveToSlot
            // 
            this.groupBoxMoveToSlot.Location = new System.Drawing.Point(241, 98);
            // 
            // groupBoxPosition
            // 
            this.groupBoxPosition.Location = new System.Drawing.Point(241, 168);
            // 
            // radioButtonPlate0
            // 
            this.radioButtonPlate0.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonPlate0.Location = new System.Drawing.Point(0, 0);
            this.radioButtonPlate0.Name = "radioButtonPlate0";
            this.radioButtonPlate0.Size = new System.Drawing.Size(100, 40);
            this.radioButtonPlate0.TabIndex = 1;
            this.radioButtonPlate0.Text = "Plate";
            this.radioButtonPlate0.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.radioButtonPlate0.UseVisualStyleBackColor = true;
            // 
            // radioButtonPlate1
            // 
            this.radioButtonPlate1.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonPlate1.Location = new System.Drawing.Point(100, 0);
            this.radioButtonPlate1.Name = "radioButtonPlate1";
            this.radioButtonPlate1.Size = new System.Drawing.Size(100, 40);
            this.radioButtonPlate1.TabIndex = 0;
            this.radioButtonPlate1.TabStop = true;
            this.radioButtonPlate1.Text = "Plate";
            this.radioButtonPlate1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.radioButtonPlate1.UseVisualStyleBackColor = true;
            // 
            // radioButtonCarrierBelow
            // 
            this.radioButtonCarrierBelow.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonCarrierBelow.Location = new System.Drawing.Point(0, 0);
            this.radioButtonCarrierBelow.Name = "radioButtonCarrierBelow";
            this.radioButtonCarrierBelow.Size = new System.Drawing.Size(100, 40);
            this.radioButtonCarrierBelow.TabIndex = 3;
            this.radioButtonCarrierBelow.Text = "Below";
            this.radioButtonCarrierBelow.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.radioButtonCarrierBelow.UseVisualStyleBackColor = true;
            // 
            // radioButtonCarrierAbove
            // 
            this.radioButtonCarrierAbove.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonCarrierAbove.Location = new System.Drawing.Point(100, 0);
            this.radioButtonCarrierAbove.Name = "radioButtonCarrierAbove";
            this.radioButtonCarrierAbove.Size = new System.Drawing.Size(100, 40);
            this.radioButtonCarrierAbove.TabIndex = 2;
            this.radioButtonCarrierAbove.TabStop = true;
            this.radioButtonCarrierAbove.Text = "Above";
            this.radioButtonCarrierAbove.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.radioButtonCarrierAbove.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanelXCarrierPort
            // 
            this.flowLayoutPanelXCarrierPort.ContentAlign = System.Drawing.ContentAlignment.TopCenter;
            this.flowLayoutPanelXCarrierPort.Controls.Add(this.radioButtonCarrierBelow);
            this.flowLayoutPanelXCarrierPort.Controls.Add(this.radioButtonCarrierAbove);
            this.flowLayoutPanelXCarrierPort.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowLayoutPanelXCarrierPort.Gap = 100;
            this.flowLayoutPanelXCarrierPort.Location = new System.Drawing.Point(241, 52);
            this.flowLayoutPanelXCarrierPort.Name = "flowLayoutPanelXCarrierPort";
            this.flowLayoutPanelXCarrierPort.Size = new System.Drawing.Size(207, 40);
            this.flowLayoutPanelXCarrierPort.TabIndex = 20;
            // 
            // flowLayoutPanelXPlates
            // 
            this.flowLayoutPanelXPlates.ContentAlign = System.Drawing.ContentAlignment.TopCenter;
            this.flowLayoutPanelXPlates.Controls.Add(this.radioButtonPlate0);
            this.flowLayoutPanelXPlates.Controls.Add(this.radioButtonPlate1);
            this.flowLayoutPanelXPlates.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowLayoutPanelXPlates.Gap = 100;
            this.flowLayoutPanelXPlates.Location = new System.Drawing.Point(241, 6);
            this.flowLayoutPanelXPlates.Name = "flowLayoutPanelXPlates";
            this.flowLayoutPanelXPlates.Size = new System.Drawing.Size(207, 40);
            this.flowLayoutPanelXPlates.TabIndex = 19;
            // 
            // LifterLoadPortSlotMapperMaintenanceControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.flowLayoutPanelXCarrierPort);
            this.Controls.Add(this.flowLayoutPanelXPlates);
            this.Name = "LifterLoadPortSlotMapperMaintenanceControl";
            this.Controls.SetChildIndex(this.buttonDisplayScanResult, 0);
            this.Controls.SetChildIndex(this.flowLayoutPanelSensors, 0);
            this.Controls.SetChildIndex(this.axisJoystick, 0);
            this.Controls.SetChildIndex(this.buttonScan, 0);
            this.Controls.SetChildIndex(this.groupBoxPosition, 0);
            this.Controls.SetChildIndex(this.groupBoxMoveToSlot, 0);
            this.Controls.SetChildIndex(this.slotStateView, 0);
            this.Controls.SetChildIndex(this.flowLayoutPanelXPlates, 0);
            this.Controls.SetChildIndex(this.flowLayoutPanelXCarrierPort, 0);
            this.groupBoxMoveToSlot.ResumeLayout(false);
            this.groupBoxPosition.ResumeLayout(false);
            this.flowLayoutPanelPositions.ResumeLayout(false);
            this.flowLayoutPanelXCarrierPort.ResumeLayout(false);
            this.flowLayoutPanelXPlates.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private MechaSys.SoftBricks.Hmi.Controls.RadioButtonX radioButtonPlate0;
        private MechaSys.SoftBricks.Hmi.Controls.RadioButtonX radioButtonPlate1;
        private MechaSys.SoftBricks.Hmi.Controls.RadioButtonX radioButtonCarrierBelow;
        private MechaSys.SoftBricks.Hmi.Controls.RadioButtonX radioButtonCarrierAbove;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelXCarrierPort;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelXPlates;
    }
}
