namespace QMC.Hmi.Controls
{
    partial class ValveControl
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
            this.radioButtonOpen = new System.Windows.Forms.RadioButton();
            this.radioButtonClose = new System.Windows.Forms.RadioButton();
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
            this.groupBoxX1.TabIndex = 6;
            this.groupBoxX1.TabStop = false;
            this.groupBoxX1.Text = "Valve";
            // 
            // flowLayoutPanelX1
            // 
            this.flowLayoutPanelX1.ContentAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.flowLayoutPanelX1.Controls.Add(this.radioButtonOpen);
            this.flowLayoutPanelX1.Controls.Add(this.radioButtonClose);
            this.flowLayoutPanelX1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelX1.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowLayoutPanelX1.Gap = 106;
            this.flowLayoutPanelX1.Location = new System.Drawing.Point(3, 18);
            this.flowLayoutPanelX1.Name = "flowLayoutPanelX1";
            this.flowLayoutPanelX1.Size = new System.Drawing.Size(212, 46);
            this.flowLayoutPanelX1.TabIndex = 3;
            // 
            // radioButtonOpen
            // 
            this.radioButtonOpen.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonOpen.AutoSize = true;
            this.radioButtonOpen.Location = new System.Drawing.Point(3, 3);
            this.radioButtonOpen.Name = "radioButtonOpen";
            this.radioButtonOpen.Size = new System.Drawing.Size(100, 40);
            this.radioButtonOpen.TabIndex = 0;
            this.radioButtonOpen.TabStop = true;
            this.radioButtonOpen.Text = "Open";
            this.radioButtonOpen.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButtonOpen.UseVisualStyleBackColor = true;
            this.radioButtonOpen.Click += new System.EventHandler(this.buttonXOperation_Click);
            // 
            // radioButtonClose
            // 
            this.radioButtonClose.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonClose.AutoSize = true;
            this.radioButtonClose.Location = new System.Drawing.Point(109, 3);
            this.radioButtonClose.Name = "radioButtonClose";
            this.radioButtonClose.Size = new System.Drawing.Size(100, 40);
            this.radioButtonClose.TabIndex = 1;
            this.radioButtonClose.TabStop = true;
            this.radioButtonClose.Text = "Close";
            this.radioButtonClose.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButtonClose.UseVisualStyleBackColor = true;
            this.radioButtonClose.Click += new System.EventHandler(this.buttonXOperation_Click);
            // 
            // ValveControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxX1);
            this.Name = "ValveControl";
            this.Size = new System.Drawing.Size(224, 73);
            this.groupBoxX1.ResumeLayout(false);
            this.flowLayoutPanelX1.ResumeLayout(false);
            this.flowLayoutPanelX1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxX1;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelX1;
        private System.Windows.Forms.RadioButton radioButtonOpen;
        private System.Windows.Forms.RadioButton radioButtonClose;
    }
}
