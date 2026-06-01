namespace QMC.Hmi.Controls
{
    partial class CarrierLockerControl
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
            this.digitalGroupBox1 = new MechaSys.SoftBricks.IO.Controls.DigitalGroupBox();
            this.flowLayoutPanelX1 = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.radioButtonLock = new System.Windows.Forms.RadioButton();
            this.radioButtonUnlock = new System.Windows.Forms.RadioButton();
            this.groupBoxX1.SuspendLayout();
            this.flowLayoutPanelX1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxX1
            // 
            this.groupBoxX1.Controls.Add(this.digitalGroupBox1);
            this.groupBoxX1.Controls.Add(this.flowLayoutPanelX1);
            this.groupBoxX1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxX1.Location = new System.Drawing.Point(3, 3);
            this.groupBoxX1.Name = "groupBoxX1";
            this.groupBoxX1.Size = new System.Drawing.Size(401, 67);
            this.groupBoxX1.TabIndex = 0;
            this.groupBoxX1.TabStop = false;
            this.groupBoxX1.Text = "Locker";
            // 
            // digitalGroupBox1
            // 
            this.digitalGroupBox1.ContentAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.digitalGroupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.digitalGroupBox1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.digitalGroupBox1.Gap = 30;
            this.digitalGroupBox1.Location = new System.Drawing.Point(3, 18);
            this.digitalGroupBox1.Margin = new System.Windows.Forms.Padding(2);
            this.digitalGroupBox1.Name = "digitalGroupBox1";
            this.digitalGroupBox1.Points = new MechaSys.SoftBricks.IO.DioPoint[0];
            this.digitalGroupBox1.Size = new System.Drawing.Size(175, 46);
            this.digitalGroupBox1.TabIndex = 5;
            // 
            // flowLayoutPanelX1
            // 
            this.flowLayoutPanelX1.ContentAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.flowLayoutPanelX1.Controls.Add(this.radioButtonLock);
            this.flowLayoutPanelX1.Controls.Add(this.radioButtonUnlock);
            this.flowLayoutPanelX1.Dock = System.Windows.Forms.DockStyle.Right;
            this.flowLayoutPanelX1.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowLayoutPanelX1.Gap = 110;
            this.flowLayoutPanelX1.Location = new System.Drawing.Point(178, 18);
            this.flowLayoutPanelX1.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanelX1.Name = "flowLayoutPanelX1";
            this.flowLayoutPanelX1.Size = new System.Drawing.Size(220, 46);
            this.flowLayoutPanelX1.TabIndex = 3;
            // 
            // radioButtonLock
            // 
            this.radioButtonLock.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonLock.AutoSize = true;
            this.radioButtonLock.Location = new System.Drawing.Point(5, 3);
            this.radioButtonLock.Name = "radioButtonLock";
            this.radioButtonLock.Size = new System.Drawing.Size(100, 40);
            this.radioButtonLock.TabIndex = 2;
            this.radioButtonLock.TabStop = true;
            this.radioButtonLock.Text = "Lock";
            this.radioButtonLock.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButtonLock.UseVisualStyleBackColor = true;
            this.radioButtonLock.Click += new System.EventHandler(this.OperationButton_Click);
            // 
            // radioButtonUnlock
            // 
            this.radioButtonUnlock.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonUnlock.AutoSize = true;
            this.radioButtonUnlock.Location = new System.Drawing.Point(115, 3);
            this.radioButtonUnlock.Name = "radioButtonUnlock";
            this.radioButtonUnlock.Size = new System.Drawing.Size(100, 40);
            this.radioButtonUnlock.TabIndex = 3;
            this.radioButtonUnlock.TabStop = true;
            this.radioButtonUnlock.Text = "Unlock";
            this.radioButtonUnlock.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButtonUnlock.UseVisualStyleBackColor = true;
            this.radioButtonUnlock.Click += new System.EventHandler(this.OperationButton_Click);
            // 
            // CarrierLockerControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxX1);
            this.Name = "CarrierLockerControl";
            this.Size = new System.Drawing.Size(407, 73);
            this.groupBoxX1.ResumeLayout(false);
            this.flowLayoutPanelX1.ResumeLayout(false);
            this.flowLayoutPanelX1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxX1;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelX1;
        private System.Windows.Forms.RadioButton radioButtonLock;
        private System.Windows.Forms.RadioButton radioButtonUnlock;
        private MechaSys.SoftBricks.IO.Controls.DigitalGroupBox digitalGroupBox1;
    }
}
