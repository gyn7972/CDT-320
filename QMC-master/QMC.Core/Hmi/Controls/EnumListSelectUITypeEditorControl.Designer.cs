namespace QMC.Hmi.Controls
{
    partial class EnumListSelectUITypeEditorControl<T>
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.comboBoxX1 = new MechaSys.SoftBricks.Hmi.Controls.ComboBoxX();
            this.buttonXAdd = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonXRemove = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.flexGrid1 = new MechaSys.SoftBricks.Hmi.Controls.FlexGrid();
            this.ColumnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.flexGrid1)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonOK.Location = new System.Drawing.Point(116, 178);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonCancel.Location = new System.Drawing.Point(116, 207);
            // 
            // comboBoxX1
            // 
            this.comboBoxX1.FormattingEnabled = true;
            this.comboBoxX1.IntegralHeight = false;
            this.comboBoxX1.ItemHeight = 20;
            this.comboBoxX1.Location = new System.Drawing.Point(3, 3);
            this.comboBoxX1.Name = "comboBoxX1";
            this.comboBoxX1.Size = new System.Drawing.Size(107, 26);
            this.comboBoxX1.TabIndex = 2;
            // 
            // buttonXAdd
            // 
            this.buttonXAdd.Location = new System.Drawing.Point(116, 35);
            this.buttonXAdd.Name = "buttonXAdd";
            this.buttonXAdd.Size = new System.Drawing.Size(75, 23);
            this.buttonXAdd.TabIndex = 3;
            this.buttonXAdd.Text = "Add";
            this.buttonXAdd.UseVisualStyleBackColor = true;
            this.buttonXAdd.Click += new System.EventHandler(this.buttonOperation_Click);
            // 
            // buttonXRemove
            // 
            this.buttonXRemove.Location = new System.Drawing.Point(116, 64);
            this.buttonXRemove.Name = "buttonXRemove";
            this.buttonXRemove.Size = new System.Drawing.Size(75, 23);
            this.buttonXRemove.TabIndex = 4;
            this.buttonXRemove.Text = "Remove";
            this.buttonXRemove.UseVisualStyleBackColor = true;
            this.buttonXRemove.Click += new System.EventHandler(this.buttonOperation_Click);
            // 
            // flexGrid1
            // 
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.flexGrid1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.flexGrid1.ColumnHeadersHeight = 19;
            this.flexGrid1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.flexGrid1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnName});
            this.flexGrid1.GridColor = System.Drawing.SystemColors.Control;
            this.flexGrid1.IsContentChanged = true;
            this.flexGrid1.Location = new System.Drawing.Point(3, 35);
            this.flexGrid1.Name = "flexGrid1";
            this.flexGrid1.RowTemplate.Height = 23;
            this.flexGrid1.Size = new System.Drawing.Size(107, 195);
            this.flexGrid1.TabIndex = 5;
            // 
            // ColumnName
            // 
            this.ColumnName.HeaderText = "Size";
            this.ColumnName.Name = "ColumnName";
            this.ColumnName.ReadOnly = true;
            // 
            // EnumSelectUITypeEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.flexGrid1);
            this.Controls.Add(this.buttonXRemove);
            this.Controls.Add(this.buttonXAdd);
            this.Controls.Add(this.comboBoxX1);
            this.Name = "EnumSelectUITypeEditorControl";
            this.Size = new System.Drawing.Size(197, 233);
            this.Controls.SetChildIndex(this.buttonOK, 0);
            this.Controls.SetChildIndex(this.buttonCancel, 0);
            this.Controls.SetChildIndex(this.comboBoxX1, 0);
            this.Controls.SetChildIndex(this.buttonXAdd, 0);
            this.Controls.SetChildIndex(this.buttonXRemove, 0);
            this.Controls.SetChildIndex(this.flexGrid1, 0);
            ((System.ComponentModel.ISupportInitialize)(this.flexGrid1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Hmi.Controls.ComboBoxX comboBoxX1;
        private MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonXAdd;
        private MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonXRemove;
        private MechaSys.SoftBricks.Hmi.Controls.FlexGrid flexGrid1;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnName;
    }
}
