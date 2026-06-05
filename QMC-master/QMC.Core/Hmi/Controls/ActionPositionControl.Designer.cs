namespace QMC.Hmi.Controls
{
    partial class ActionPositionControl
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
            this.groupBoxAction = new MechaSys.SoftBricks.Hmi.Controls.GroupBoxX();
            this.buttonMove = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.flowLayoutPanelX1 = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.groupBoxAction.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxAction
            // 
            this.groupBoxAction.Controls.Add(this.buttonMove);
            this.groupBoxAction.Controls.Add(this.flowLayoutPanelX1);
            this.groupBoxAction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxAction.Location = new System.Drawing.Point(3, 3);
            this.groupBoxAction.Name = "groupBoxAction";
            this.groupBoxAction.Size = new System.Drawing.Size(215, 176);
            this.groupBoxAction.TabIndex = 16;
            this.groupBoxAction.TabStop = false;
            this.groupBoxAction.Text = "Positions";
            // 
            // buttonMove
            // 
            this.buttonMove.Location = new System.Drawing.Point(112, 18);
            this.buttonMove.Name = "buttonMove";
            this.buttonMove.Size = new System.Drawing.Size(100, 40);
            this.buttonMove.TabIndex = 17;
            this.buttonMove.Text = "Move";
            this.buttonMove.UseVisualStyleBackColor = true;
            this.buttonMove.Click += new System.EventHandler(this.buttonMove_Click);
            // 
            // flowLayoutPanelX1
            // 
            this.flowLayoutPanelX1.ContentAlign = System.Drawing.ContentAlignment.TopCenter;
            this.flowLayoutPanelX1.Dock = System.Windows.Forms.DockStyle.Left;
            this.flowLayoutPanelX1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelX1.Gap = 46;
            this.flowLayoutPanelX1.Location = new System.Drawing.Point(3, 18);
            this.flowLayoutPanelX1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.flowLayoutPanelX1.Name = "flowLayoutPanelX1";
            this.flowLayoutPanelX1.Size = new System.Drawing.Size(106, 155);
            this.flowLayoutPanelX1.TabIndex = 16;
            // 
            // ActionPositionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxAction);
            this.Name = "ActionPositionControl";
            this.Size = new System.Drawing.Size(221, 182);
            this.groupBoxAction.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxAction;
        private MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonMove;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelX1;
    }
}
