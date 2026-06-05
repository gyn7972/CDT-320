namespace QMC.Hmi.Controls
{
    partial class VerticalCylinderControl
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
            this.groupBoxXGripper = new MechaSys.SoftBricks.Hmi.Controls.GroupBoxX();
            this.flowLayoutPanelX1 = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.radioButtonUp = new System.Windows.Forms.RadioButton();
            this.radioButtonDown = new System.Windows.Forms.RadioButton();
            this.groupBoxXGripper.SuspendLayout();
            this.flowLayoutPanelX1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxXGripper
            // 
            this.groupBoxXGripper.Controls.Add(this.flowLayoutPanelX1);
            this.groupBoxXGripper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxXGripper.Location = new System.Drawing.Point(3, 3);
            this.groupBoxXGripper.Name = "groupBoxXGripper";
            this.groupBoxXGripper.Size = new System.Drawing.Size(218, 67);
            this.groupBoxXGripper.TabIndex = 3;
            this.groupBoxXGripper.TabStop = false;
            this.groupBoxXGripper.Text = "Vertical cylinder";
            // 
            // flowLayoutPanelX1
            // 
            this.flowLayoutPanelX1.ContentAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.flowLayoutPanelX1.Controls.Add(this.radioButtonUp);
            this.flowLayoutPanelX1.Controls.Add(this.radioButtonDown);
            this.flowLayoutPanelX1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelX1.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowLayoutPanelX1.Gap = 106;
            this.flowLayoutPanelX1.Location = new System.Drawing.Point(3, 18);
            this.flowLayoutPanelX1.Name = "flowLayoutPanelX1";
            this.flowLayoutPanelX1.Size = new System.Drawing.Size(212, 46);
            this.flowLayoutPanelX1.TabIndex = 0;
            // 
            // radioButtonUp
            // 
            this.radioButtonUp.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonUp.AutoSize = true;
            this.radioButtonUp.Location = new System.Drawing.Point(3, 3);
            this.radioButtonUp.Name = "radioButtonUp";
            this.radioButtonUp.Size = new System.Drawing.Size(100, 40);
            this.radioButtonUp.TabIndex = 0;
            this.radioButtonUp.TabStop = true;
            this.radioButtonUp.Text = "Up";
            this.radioButtonUp.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButtonUp.UseVisualStyleBackColor = true;
            this.radioButtonUp.Click += new System.EventHandler(this.buttonXOperation_Click);
            // 
            // radioButtonDown
            // 
            this.radioButtonDown.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonDown.AutoSize = true;
            this.radioButtonDown.Location = new System.Drawing.Point(109, 3);
            this.radioButtonDown.Name = "radioButtonDown";
            this.radioButtonDown.Size = new System.Drawing.Size(100, 40);
            this.radioButtonDown.TabIndex = 1;
            this.radioButtonDown.TabStop = true;
            this.radioButtonDown.Text = "Down";
            this.radioButtonDown.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButtonDown.UseVisualStyleBackColor = true;
            this.radioButtonDown.Click += new System.EventHandler(this.buttonXOperation_Click);
            // 
            // VerticalCylinderControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxXGripper);
            this.Name = "VerticalCylinderControl";
            this.Size = new System.Drawing.Size(224, 73);
            this.groupBoxXGripper.ResumeLayout(false);
            this.flowLayoutPanelX1.ResumeLayout(false);
            this.flowLayoutPanelX1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        protected MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxXGripper;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelX1;
        private System.Windows.Forms.RadioButton radioButtonUp;
        private System.Windows.Forms.RadioButton radioButtonDown;
    }
}
