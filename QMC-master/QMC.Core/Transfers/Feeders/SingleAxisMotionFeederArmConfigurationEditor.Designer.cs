namespace QMC.Transfers.Feeders
{
    partial class SingleAxisMotionFeederArmConfigurationEditor
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
            this.transferXyztPositionDataEditor1 = new MechaSys.SoftBricks.Motions.Controls.TransferXyztPositionDataEditor();
            this.groupBoxX1 = new MechaSys.SoftBricks.Hmi.Controls.GroupBoxX();
            this.checkBoxEnableAlign = new System.Windows.Forms.CheckBox();
            this.groupBoxX1.SuspendLayout();
            this.SuspendLayout();
            // 
            // transferXyztPositionDataEditor1
            // 
            this.transferXyztPositionDataEditor1.Actions = null;
            this.transferXyztPositionDataEditor1.EnablePower = false;
            this.transferXyztPositionDataEditor1.EnableT = false;
            this.transferXyztPositionDataEditor1.EnableX = false;
            this.transferXyztPositionDataEditor1.EnableY = true;
            this.transferXyztPositionDataEditor1.EnableZ = false;
            this.transferXyztPositionDataEditor1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.transferXyztPositionDataEditor1.Location = new System.Drawing.Point(6, 6);
            this.transferXyztPositionDataEditor1.Name = "transferXyztPositionDataEditor1";
            this.transferXyztPositionDataEditor1.Padding = new System.Windows.Forms.Padding(3);
            this.transferXyztPositionDataEditor1.Size = new System.Drawing.Size(587, 421);
            this.transferXyztPositionDataEditor1.TabIndex = 0;
            this.transferXyztPositionDataEditor1.Unit = "Inch";
            // 
            // groupBoxX1
            // 
            this.groupBoxX1.Controls.Add(this.checkBoxEnableAlign);
            this.groupBoxX1.Location = new System.Drawing.Point(6, 442);
            this.groupBoxX1.Name = "groupBoxX1";
            this.groupBoxX1.Size = new System.Drawing.Size(233, 48);
            this.groupBoxX1.TabIndex = 1;
            this.groupBoxX1.TabStop = false;
            this.groupBoxX1.Text = "Transfer";
            // 
            // checkBoxEnableAlign
            // 
            this.checkBoxEnableAlign.AutoSize = true;
            this.checkBoxEnableAlign.Location = new System.Drawing.Point(137, 21);
            this.checkBoxEnableAlign.Name = "checkBoxEnableAlign";
            this.checkBoxEnableAlign.Size = new System.Drawing.Size(90, 18);
            this.checkBoxEnableAlign.TabIndex = 0;
            this.checkBoxEnableAlign.Text = "Enable align";
            this.checkBoxEnableAlign.UseVisualStyleBackColor = true;
            // 
            // SingleAxisMotionFeederArmConfigurationEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxX1);
            this.Controls.Add(this.transferXyztPositionDataEditor1);
            this.Name = "SingleAxisMotionFeederArmConfigurationEditor";
            this.groupBoxX1.ResumeLayout(false);
            this.groupBoxX1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Motions.Controls.TransferXyztPositionDataEditor transferXyztPositionDataEditor1;
        private MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxX1;
        private System.Windows.Forms.CheckBox checkBoxEnableAlign;
    }
}
