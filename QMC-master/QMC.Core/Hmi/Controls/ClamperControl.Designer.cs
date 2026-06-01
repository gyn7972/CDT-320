namespace QMC.Hmi.Controls
{
    partial class ClamperControl
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
            this.radioButtonClamp = new System.Windows.Forms.RadioButton();
            this.radioButtonUnclamp = new System.Windows.Forms.RadioButton();
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
            this.groupBoxXGripper.TabIndex = 2;
            this.groupBoxXGripper.TabStop = false;
            this.groupBoxXGripper.Text = "Clamper";
            // 
            // flowLayoutPanelX1
            // 
            this.flowLayoutPanelX1.ContentAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.flowLayoutPanelX1.Controls.Add(this.radioButtonClamp);
            this.flowLayoutPanelX1.Controls.Add(this.radioButtonUnclamp);
            this.flowLayoutPanelX1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelX1.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowLayoutPanelX1.Gap = 106;
            this.flowLayoutPanelX1.Location = new System.Drawing.Point(3, 18);
            this.flowLayoutPanelX1.Name = "flowLayoutPanelX1";
            this.flowLayoutPanelX1.Size = new System.Drawing.Size(212, 46);
            this.flowLayoutPanelX1.TabIndex = 0;
            // 
            // radioButtonClamp
            // 
            this.radioButtonClamp.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonClamp.AutoSize = true;
            this.radioButtonClamp.Location = new System.Drawing.Point(3, 3);
            this.radioButtonClamp.Name = "radioButtonClamp";
            this.radioButtonClamp.Size = new System.Drawing.Size(100, 40);
            this.radioButtonClamp.TabIndex = 0;
            this.radioButtonClamp.TabStop = true;
            this.radioButtonClamp.Text = "Clamp";
            this.radioButtonClamp.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButtonClamp.UseVisualStyleBackColor = true;
            this.radioButtonClamp.Click += new System.EventHandler(this.buttonXOperation_Click);
            // 
            // radioButtonUnclamp
            // 
            this.radioButtonUnclamp.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonUnclamp.AutoSize = true;
            this.radioButtonUnclamp.Location = new System.Drawing.Point(109, 3);
            this.radioButtonUnclamp.Name = "radioButtonUnclamp";
            this.radioButtonUnclamp.Size = new System.Drawing.Size(100, 40);
            this.radioButtonUnclamp.TabIndex = 1;
            this.radioButtonUnclamp.TabStop = true;
            this.radioButtonUnclamp.Text = "Unclamp";
            this.radioButtonUnclamp.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButtonUnclamp.UseVisualStyleBackColor = true;
            this.radioButtonUnclamp.Click += new System.EventHandler(this.buttonXOperation_Click);
            // 
            // ClamperControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxXGripper);
            this.Name = "ClamperControl";
            this.Size = new System.Drawing.Size(224, 73);
            this.groupBoxXGripper.ResumeLayout(false);
            this.flowLayoutPanelX1.ResumeLayout(false);
            this.flowLayoutPanelX1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxXGripper;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelX1;
        private System.Windows.Forms.RadioButton radioButtonClamp;
        private System.Windows.Forms.RadioButton radioButtonUnclamp;
    }
}
