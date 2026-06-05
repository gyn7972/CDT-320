namespace QMC.Hmi.Controls
{
    partial class ActionTransferablePositionControl
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
            this.groupBoxXPrimary = new MechaSys.SoftBricks.Hmi.Controls.GroupBoxX();
            this.buttonMove = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.flowLayoutPanelXActions = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.flowLayoutPanelXTransferredPort = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.groupBoxXPrimary.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxXPrimary
            // 
            this.groupBoxXPrimary.Controls.Add(this.buttonMove);
            this.groupBoxXPrimary.Controls.Add(this.flowLayoutPanelXActions);
            this.groupBoxXPrimary.Controls.Add(this.flowLayoutPanelXTransferredPort);
            this.groupBoxXPrimary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxXPrimary.Location = new System.Drawing.Point(3, 3);
            this.groupBoxXPrimary.Name = "groupBoxXPrimary";
            this.groupBoxXPrimary.Size = new System.Drawing.Size(321, 186);
            this.groupBoxXPrimary.TabIndex = 25;
            this.groupBoxXPrimary.TabStop = false;
            this.groupBoxXPrimary.Text = "Positions";
            // 
            // buttonMove
            // 
            this.buttonMove.Location = new System.Drawing.Point(218, 18);
            this.buttonMove.Name = "buttonMove";
            this.buttonMove.Size = new System.Drawing.Size(100, 40);
            this.buttonMove.TabIndex = 29;
            this.buttonMove.Text = "Move";
            this.buttonMove.UseVisualStyleBackColor = true;
            this.buttonMove.Click += new System.EventHandler(this.buttonMove_Click);
            // 
            // flowLayoutPanelXActions
            // 
            this.flowLayoutPanelXActions.ContentAlign = System.Drawing.ContentAlignment.TopCenter;
            this.flowLayoutPanelXActions.Dock = System.Windows.Forms.DockStyle.Left;
            this.flowLayoutPanelXActions.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelXActions.Gap = 46;
            this.flowLayoutPanelXActions.Location = new System.Drawing.Point(109, 18);
            this.flowLayoutPanelXActions.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.flowLayoutPanelXActions.Name = "flowLayoutPanelXActions";
            this.flowLayoutPanelXActions.Size = new System.Drawing.Size(106, 165);
            this.flowLayoutPanelXActions.TabIndex = 28;
            // 
            // flowLayoutPanelXTransferredPort
            // 
            this.flowLayoutPanelXTransferredPort.ContentAlign = System.Drawing.ContentAlignment.TopCenter;
            this.flowLayoutPanelXTransferredPort.Dock = System.Windows.Forms.DockStyle.Left;
            this.flowLayoutPanelXTransferredPort.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelXTransferredPort.Gap = 46;
            this.flowLayoutPanelXTransferredPort.Location = new System.Drawing.Point(3, 18);
            this.flowLayoutPanelXTransferredPort.Name = "flowLayoutPanelXTransferredPort";
            this.flowLayoutPanelXTransferredPort.Size = new System.Drawing.Size(106, 165);
            this.flowLayoutPanelXTransferredPort.TabIndex = 26;
            // 
            // ActionTransferablePositionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxXPrimary);
            this.Name = "ActionTransferablePositionControl";
            this.Size = new System.Drawing.Size(327, 192);
            this.groupBoxXPrimary.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxXPrimary;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonMove;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelXActions;
        private MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelXTransferredPort;
    }
}
