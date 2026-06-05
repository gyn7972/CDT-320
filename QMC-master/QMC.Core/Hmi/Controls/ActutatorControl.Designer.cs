namespace QMC.Hmi.Controls
{
    partial class ActuatorControl
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
            this.groupBoxX1 = new MechaSys.SoftBricks.Hmi.Controls.GroupBoxX();
            this.flowLayoutPanelX1 = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.radioButtonOn = new System.Windows.Forms.RadioButton();
            this.radioButtonOff = new System.Windows.Forms.RadioButton();
            this.groupBoxX1.SuspendLayout();
            this.flowLayoutPanelX1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxX1
            // 
            this.groupBoxX1.Controls.Add(this.flowLayoutPanelX1);
            this.groupBoxX1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxX1.Location = new System.Drawing.Point(3, 3);
            this.groupBoxX1.Name = "groupBoxX1";
            this.groupBoxX1.Size = new System.Drawing.Size(218, 67);
            this.groupBoxX1.TabIndex = 5;
            this.groupBoxX1.TabStop = false;
            this.groupBoxX1.Text = "Actuator";
            // 
            // flowLayoutPanelX1
            // 
            this.flowLayoutPanelX1.ContentAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.flowLayoutPanelX1.Controls.Add(this.radioButtonOn);
            this.flowLayoutPanelX1.Controls.Add(this.radioButtonOff);
            this.flowLayoutPanelX1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelX1.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowLayoutPanelX1.Gap = 106;
            this.flowLayoutPanelX1.Location = new System.Drawing.Point(3, 18);
            this.flowLayoutPanelX1.Name = "flowLayoutPanelX1";
            this.flowLayoutPanelX1.Size = new System.Drawing.Size(212, 46);
            this.flowLayoutPanelX1.TabIndex = 3;
            // 
            // radioButtonOn
            // 
            this.radioButtonOn.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonOn.AutoSize = true;
            this.radioButtonOn.Location = new System.Drawing.Point(3, 3);
            this.radioButtonOn.Name = "radioButtonOn";
            this.radioButtonOn.Size = new System.Drawing.Size(100, 40);
            this.radioButtonOn.TabIndex = 0;
            this.radioButtonOn.TabStop = true;
            this.radioButtonOn.Text = "On";
            this.radioButtonOn.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButtonOn.UseVisualStyleBackColor = true;
            this.radioButtonOn.Click += new System.EventHandler(this.buttonXOperation_Click);
            // 
            // radioButtonOff
            // 
            this.radioButtonOff.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonOff.AutoSize = true;
            this.radioButtonOff.Location = new System.Drawing.Point(109, 3);
            this.radioButtonOff.Name = "radioButtonOff";
            this.radioButtonOff.Size = new System.Drawing.Size(100, 40);
            this.radioButtonOff.TabIndex = 1;
            this.radioButtonOff.TabStop = true;
            this.radioButtonOff.Text = "Off";
            this.radioButtonOff.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButtonOff.UseVisualStyleBackColor = true;
            this.radioButtonOff.Click += new System.EventHandler(this.buttonXOperation_Click);
            // 
            // ActuatorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxX1);
            this.Name = "ActuatorControl";
            this.Size = new System.Drawing.Size(224, 73);
            this.groupBoxX1.ResumeLayout(false);
            this.flowLayoutPanelX1.ResumeLayout(false);
            this.flowLayoutPanelX1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxX1;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelX1;
        private System.Windows.Forms.RadioButton radioButtonOn;
        private System.Windows.Forms.RadioButton radioButtonOff;
    }
}
