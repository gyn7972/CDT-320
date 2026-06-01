namespace QMC.Hmi.Controls
{
    partial class MaterialStorablePartControl
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
            this.groupBoxXMaterialStorablePart = new MechaSys.SoftBricks.Hmi.Controls.GroupBoxX();
            this.flowLayoutPanelXGripper = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.digitalGroupBox1 = new MechaSys.SoftBricks.IO.Controls.DigitalGroupBox();
            this.groupBoxXMaterialStorablePart.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxXMaterialStorablePart
            // 
            this.groupBoxXMaterialStorablePart.Controls.Add(this.digitalGroupBox1);
            this.groupBoxXMaterialStorablePart.Controls.Add(this.flowLayoutPanelXGripper);
            this.groupBoxXMaterialStorablePart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxXMaterialStorablePart.Location = new System.Drawing.Point(3, 3);
            this.groupBoxXMaterialStorablePart.Name = "groupBoxXMaterialStorablePart";
            this.groupBoxXMaterialStorablePart.Size = new System.Drawing.Size(406, 94);
            this.groupBoxXMaterialStorablePart.TabIndex = 3;
            this.groupBoxXMaterialStorablePart.TabStop = false;
            this.groupBoxXMaterialStorablePart.Text = "Material storable part";
            // 
            // flowLayoutPanelXGripper
            // 
            this.flowLayoutPanelXGripper.ContentAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.flowLayoutPanelXGripper.Dock = System.Windows.Forms.DockStyle.Right;
            this.flowLayoutPanelXGripper.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelXGripper.Gap = 79;
            this.flowLayoutPanelXGripper.Location = new System.Drawing.Point(179, 18);
            this.flowLayoutPanelXGripper.Name = "flowLayoutPanelXGripper";
            this.flowLayoutPanelXGripper.Size = new System.Drawing.Size(224, 73);
            this.flowLayoutPanelXGripper.TabIndex = 3;
            // 
            // digitalGroupBox1
            // 
            this.digitalGroupBox1.ContentAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.digitalGroupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.digitalGroupBox1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.digitalGroupBox1.Gap = 30;
            this.digitalGroupBox1.Location = new System.Drawing.Point(3, 18);
            this.digitalGroupBox1.Name = "digitalGroupBox1";
            this.digitalGroupBox1.Points = new MechaSys.SoftBricks.IO.DioPoint[0];
            this.digitalGroupBox1.Size = new System.Drawing.Size(176, 73);
            this.digitalGroupBox1.TabIndex = 4;
            // 
            // MaterialStorablePartControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxXMaterialStorablePart);
            this.Name = "MaterialStorablePartControl";
            this.Size = new System.Drawing.Size(412, 100);
            this.groupBoxXMaterialStorablePart.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxXMaterialStorablePart;
        private MechaSys.SoftBricks.IO.Controls.DigitalGroupBox digitalGroupBox1;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelXGripper;
    }
}
