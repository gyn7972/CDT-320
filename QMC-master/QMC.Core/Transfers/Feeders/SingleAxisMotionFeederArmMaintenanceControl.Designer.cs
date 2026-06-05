namespace QMC.Transfers.Feeders
{
    partial class SingleAxisMotionFeederArmMaintenanceControl
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
            this.actionTransferablePositionControl1 = new QMC.Hmi.Controls.ActionTransferablePositionControl();
            this.SuspendLayout();
            // 
            // materialStorablePartControl1
            // 
            this.materialStorablePartControl1.Location = new System.Drawing.Point(706, 141);
            // 
            // verticalCylinderControl1
            // 
            this.verticalCylinderControl1.Location = new System.Drawing.Point(894, 253);
            // 
            // actionTransferablePositionControl1
            // 
            this.actionTransferablePositionControl1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.actionTransferablePositionControl1.Location = new System.Drawing.Point(6, 6);
            this.actionTransferablePositionControl1.Name = "actionTransferablePositionControl1";
            this.actionTransferablePositionControl1.Padding = new System.Windows.Forms.Padding(3);
            this.actionTransferablePositionControl1.Size = new System.Drawing.Size(327, 179);
            this.actionTransferablePositionControl1.TabIndex = 2;
            this.actionTransferablePositionControl1.Title = null;
            // 
            // SingleAxisMotionFeederArmMaintenanceControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.actionTransferablePositionControl1);
            this.Name = "SingleAxisMotionFeederArmMaintenanceControl";
            this.ResumeLayout(false);

        }

        #endregion
        private Hmi.Controls.ActionTransferablePositionControl actionTransferablePositionControl1;
    }
}
