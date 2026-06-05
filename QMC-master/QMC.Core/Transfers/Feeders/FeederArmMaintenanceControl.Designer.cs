namespace QMC.Transfers.Feeders
{
    partial class FeederArmMaintenanceControl
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
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.materialStorablePartControl1 = new QMC.Hmi.Controls.MaterialStorablePartControl();
            this.verticalCylinderControl1 = new QMC.Hmi.Controls.VerticalCylinderControl();
            this.panelX1 = new MechaSys.SoftBricks.Hmi.Controls.PanelX();
            this.digitalBoxOverload = new MechaSys.SoftBricks.IO.Controls.DigitalBox();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.materialStorablePartControl1);
            this.flowLayoutPanel1.Controls.Add(this.verticalCylinderControl1);
            this.flowLayoutPanel1.Controls.Add(this.panelX1);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(601, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(520, 740);
            this.flowLayoutPanel1.TabIndex = 3;
            // 
            // materialStorablePartControl1
            // 
            this.materialStorablePartControl1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.materialStorablePartControl1.Location = new System.Drawing.Point(105, 3);
            this.materialStorablePartControl1.MaterialStorablePart = null;
            this.materialStorablePartControl1.Name = "materialStorablePartControl1";
            this.materialStorablePartControl1.Padding = new System.Windows.Forms.Padding(3);
            this.materialStorablePartControl1.Size = new System.Drawing.Size(412, 106);
            this.materialStorablePartControl1.TabIndex = 0;
            this.materialStorablePartControl1.Title = null;
            // 
            // verticalCylinderControl1
            // 
            this.verticalCylinderControl1.Cylinder = null;
            this.verticalCylinderControl1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.verticalCylinderControl1.Location = new System.Drawing.Point(293, 115);
            this.verticalCylinderControl1.Name = "verticalCylinderControl1";
            this.verticalCylinderControl1.Padding = new System.Windows.Forms.Padding(3);
            this.verticalCylinderControl1.Size = new System.Drawing.Size(224, 73);
            this.verticalCylinderControl1.TabIndex = 1;
            this.verticalCylinderControl1.Title = null;
            // 
            // panelX1
            // 
            this.panelX1.Location = new System.Drawing.Point(-3, 194);
            this.panelX1.Name = "panelX1";
            this.panelX1.Size = new System.Drawing.Size(520, 28);
            this.panelX1.TabIndex = 4;
            // 
            // digitalBoxOverload
            // 
            this.digitalBoxOverload.DioPoint = null;
            this.digitalBoxOverload.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.digitalBoxOverload.ImageReSize = false;
            this.digitalBoxOverload.ImageSize = new System.Drawing.Size(20, 20);
            this.digitalBoxOverload.ImageStyle = MechaSys.SoftBricks.IO.Controls.DigitalBox.ImageStyles.Led;
            this.digitalBoxOverload.Location = new System.Drawing.Point(385, 712);
            this.digitalBoxOverload.Name = "digitalBoxOverload";
            this.digitalBoxOverload.OffImage = null;
            this.digitalBoxOverload.OnImage = null;
            this.digitalBoxOverload.Padding = new System.Windows.Forms.Padding(3);
            this.digitalBoxOverload.Size = new System.Drawing.Size(200, 28);
            this.digitalBoxOverload.TabIndex = 0;
            this.digitalBoxOverload.Text = "digitalBox1";
            this.digitalBoxOverload.TextAlignMargin = 5;
            this.digitalBoxOverload.Value = false;
            // 
            // FeederArmMaintenanceControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.digitalBoxOverload);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Name = "FeederArmMaintenanceControl";
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        protected Hmi.Controls.MaterialStorablePartControl materialStorablePartControl1;
        protected Hmi.Controls.VerticalCylinderControl verticalCylinderControl1;
        protected System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private MechaSys.SoftBricks.Hmi.Controls.PanelX panelX1;
        private MechaSys.SoftBricks.IO.Controls.DigitalBox digitalBoxOverload;
    }
}
