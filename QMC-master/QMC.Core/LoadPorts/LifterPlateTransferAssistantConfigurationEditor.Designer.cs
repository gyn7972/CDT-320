namespace QMC.LoadPorts
{
    partial class LifterPlateTransferAssistantConfigurationEditor
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
            this.targetXyztPositionDataEditor1 = new MechaSys.SoftBricks.Motions.Controls.TargetXyztPositionDataEditor();
            this.groupBoxXMaterialSize = new MechaSys.SoftBricks.Hmi.Controls.GroupBoxX();
            this.flowLayoutPanelXMaterialSize = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.groupBoxXMaterialSize.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // targetXyztPositionDataEditor1
            // 
            this.targetXyztPositionDataEditor1.Actions = null;
            this.targetXyztPositionDataEditor1.EnablePower = false;
            this.targetXyztPositionDataEditor1.EnableT = false;
            this.targetXyztPositionDataEditor1.EnableX = false;
            this.targetXyztPositionDataEditor1.EnableY = false;
            this.targetXyztPositionDataEditor1.EnableZ = true;
            this.targetXyztPositionDataEditor1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.targetXyztPositionDataEditor1.Location = new System.Drawing.Point(3, 121);
            this.targetXyztPositionDataEditor1.Name = "targetXyztPositionDataEditor1";
            this.targetXyztPositionDataEditor1.Padding = new System.Windows.Forms.Padding(3);
            this.targetXyztPositionDataEditor1.Size = new System.Drawing.Size(486, 199);
            this.targetXyztPositionDataEditor1.TabIndex = 0;
            this.targetXyztPositionDataEditor1.Unit = "Inch";
            // 
            // groupBoxXMaterialSize
            // 
            this.groupBoxXMaterialSize.Controls.Add(this.flowLayoutPanelXMaterialSize);
            this.groupBoxXMaterialSize.Location = new System.Drawing.Point(3, 3);
            this.groupBoxXMaterialSize.Name = "groupBoxXMaterialSize";
            this.groupBoxXMaterialSize.Size = new System.Drawing.Size(112, 112);
            this.groupBoxXMaterialSize.TabIndex = 1;
            this.groupBoxXMaterialSize.TabStop = false;
            this.groupBoxXMaterialSize.Text = "Material size";
            // 
            // flowLayoutPanelXMaterialSize
            // 
            this.flowLayoutPanelXMaterialSize.ContentAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.flowLayoutPanelXMaterialSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelXMaterialSize.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelXMaterialSize.Gap = 46;
            this.flowLayoutPanelXMaterialSize.Location = new System.Drawing.Point(3, 18);
            this.flowLayoutPanelXMaterialSize.Name = "flowLayoutPanelXMaterialSize";
            this.flowLayoutPanelXMaterialSize.Size = new System.Drawing.Size(106, 91);
            this.flowLayoutPanelXMaterialSize.TabIndex = 2;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.groupBoxXMaterialSize);
            this.flowLayoutPanel1.Controls.Add(this.targetXyztPositionDataEditor1);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(492, 740);
            this.flowLayoutPanel1.TabIndex = 2;
            // 
            // LifterPlateTransferAssistantConfigurationEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.flowLayoutPanel1);
            this.Name = "LifterPlateTransferAssistantConfigurationEditor";
            this.groupBoxXMaterialSize.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Motions.Controls.TargetXyztPositionDataEditor targetXyztPositionDataEditor1;
        private MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxXMaterialSize;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelXMaterialSize;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
    }
}
