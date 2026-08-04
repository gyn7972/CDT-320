namespace QMC.CDT_320.Ui.Dialogs
{
    partial class ColletChangeDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.GroupBox groupSelect;
        private System.Windows.Forms.TableLayoutPanel selectLayout;
        private System.Windows.Forms.Label lblPickerCaption;
        // 라디오버튼은 "같은 부모 컨테이너" 단위로 그룹이 묶인다.
        // 픽커 쌍과 방향 쌍을 각각 별도 패널에 담아 서로 독립적으로 선택되게 한다.
        private System.Windows.Forms.TableLayoutPanel pickerGroupPanel;
        private System.Windows.Forms.RadioButton rdoFront;
        private System.Windows.Forms.RadioButton rdoRear;
        private System.Windows.Forms.Label lblSideCaption;
        private System.Windows.Forms.TableLayoutPanel sideGroupPanel;
        private System.Windows.Forms.RadioButton rdoInputSide;
        private System.Windows.Forms.RadioButton rdoOutputSide;
        private System.Windows.Forms.Label lblTargetCaption;
        private System.Windows.Forms.Label lblTargetValue;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TableLayoutPanel buttonPanel;
        private System.Windows.Forms.Button btnMove;
        private System.Windows.Forms.Button btnAvoid;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.groupSelect = new System.Windows.Forms.GroupBox();
            this.selectLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblPickerCaption = new System.Windows.Forms.Label();
            this.pickerGroupPanel = new System.Windows.Forms.TableLayoutPanel();
            this.rdoFront = new System.Windows.Forms.RadioButton();
            this.rdoRear = new System.Windows.Forms.RadioButton();
            this.lblSideCaption = new System.Windows.Forms.Label();
            this.sideGroupPanel = new System.Windows.Forms.TableLayoutPanel();
            this.rdoInputSide = new System.Windows.Forms.RadioButton();
            this.rdoOutputSide = new System.Windows.Forms.RadioButton();
            this.lblTargetCaption = new System.Windows.Forms.Label();
            this.lblTargetValue = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnMove = new System.Windows.Forms.Button();
            this.btnAvoid = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.groupSelect.SuspendLayout();
            this.selectLayout.SuspendLayout();
            this.pickerGroupPanel.SuspendLayout();
            this.sideGroupPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.groupSelect, 0, 1);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 2);
            this.rootLayout.Controls.Add(this.buttonPanel, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this.rootLayout.Size = new System.Drawing.Size(640, 380);
            this.rootLayout.TabIndex = 0;
            //
            // lblHeader
            //
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.lblHeader.Text = "COLLET CHANGE MODE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // groupSelect
            //
            this.groupSelect.Controls.Add(this.selectLayout);
            this.groupSelect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupSelect.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.groupSelect.Margin = new System.Windows.Forms.Padding(12, 10, 12, 6);
            this.groupSelect.Name = "groupSelect";
            this.groupSelect.TabIndex = 1;
            this.groupSelect.TabStop = false;
            this.groupSelect.Text = "교체 대상 선택";
            //
            // selectLayout
            //
            this.selectLayout.ColumnCount = 2;
            this.selectLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.selectLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.selectLayout.Controls.Add(this.lblPickerCaption, 0, 0);
            this.selectLayout.Controls.Add(this.pickerGroupPanel, 1, 0);
            this.selectLayout.Controls.Add(this.lblSideCaption, 0, 1);
            this.selectLayout.Controls.Add(this.sideGroupPanel, 1, 1);
            this.selectLayout.Controls.Add(this.lblTargetCaption, 0, 2);
            this.selectLayout.Controls.Add(this.lblTargetValue, 1, 2);
            this.selectLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectLayout.Name = "selectLayout";
            this.selectLayout.RowCount = 3;
            this.selectLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.selectLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.selectLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 28F));
            this.selectLayout.TabIndex = 0;
            //
            // pickerGroupPanel — 픽커 라디오 쌍 전용 컨테이너(독립 그룹)
            //
            this.pickerGroupPanel.ColumnCount = 2;
            this.pickerGroupPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.pickerGroupPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.pickerGroupPanel.Controls.Add(this.rdoFront, 0, 0);
            this.pickerGroupPanel.Controls.Add(this.rdoRear, 1, 0);
            this.pickerGroupPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pickerGroupPanel.Margin = new System.Windows.Forms.Padding(0);
            this.pickerGroupPanel.Name = "pickerGroupPanel";
            this.pickerGroupPanel.RowCount = 1;
            this.pickerGroupPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pickerGroupPanel.TabIndex = 0;
            //
            // sideGroupPanel — 교체 방향 라디오 쌍 전용 컨테이너(독립 그룹)
            //
            this.sideGroupPanel.ColumnCount = 2;
            this.sideGroupPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sideGroupPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sideGroupPanel.Controls.Add(this.rdoInputSide, 0, 0);
            this.sideGroupPanel.Controls.Add(this.rdoOutputSide, 1, 0);
            this.sideGroupPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sideGroupPanel.Margin = new System.Windows.Forms.Padding(0);
            this.sideGroupPanel.Name = "sideGroupPanel";
            this.sideGroupPanel.RowCount = 1;
            this.sideGroupPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sideGroupPanel.TabIndex = 1;
            //
            // lblPickerCaption
            //
            this.lblPickerCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickerCaption.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblPickerCaption.Name = "lblPickerCaption";
            this.lblPickerCaption.Text = "픽커";
            this.lblPickerCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // rdoFront
            //
            this.rdoFront.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoFront.BackColor = System.Drawing.Color.White;
            this.rdoFront.Checked = true;
            this.rdoFront.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rdoFront.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoFront.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.rdoFront.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this.rdoFront.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rdoFront.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.rdoFront.Margin = new System.Windows.Forms.Padding(4);
            this.rdoFront.Name = "rdoFront";
            this.rdoFront.TabStop = true;
            this.rdoFront.Text = "FRONT PICKER";
            this.rdoFront.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoFront.UseVisualStyleBackColor = false;
            //
            // rdoRear
            //
            this.rdoRear.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoRear.BackColor = System.Drawing.Color.White;
            this.rdoRear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rdoRear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoRear.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.rdoRear.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this.rdoRear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rdoRear.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.rdoRear.Margin = new System.Windows.Forms.Padding(4);
            this.rdoRear.Name = "rdoRear";
            this.rdoRear.Text = "REAR PICKER";
            this.rdoRear.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoRear.UseVisualStyleBackColor = false;
            //
            // lblSideCaption
            //
            this.lblSideCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSideCaption.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblSideCaption.Name = "lblSideCaption";
            this.lblSideCaption.Text = "교체 방향";
            this.lblSideCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // rdoInputSide
            //
            this.rdoInputSide.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoInputSide.BackColor = System.Drawing.Color.White;
            this.rdoInputSide.Checked = true;
            this.rdoInputSide.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rdoInputSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoInputSide.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.rdoInputSide.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this.rdoInputSide.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rdoInputSide.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.rdoInputSide.Margin = new System.Windows.Forms.Padding(4);
            this.rdoInputSide.Name = "rdoInputSide";
            this.rdoInputSide.TabStop = true;
            this.rdoInputSide.Text = "INPUT 쪽";
            this.rdoInputSide.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoInputSide.UseVisualStyleBackColor = false;
            //
            // rdoOutputSide
            //
            this.rdoOutputSide.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoOutputSide.BackColor = System.Drawing.Color.White;
            this.rdoOutputSide.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rdoOutputSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoOutputSide.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.rdoOutputSide.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this.rdoOutputSide.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rdoOutputSide.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.rdoOutputSide.Margin = new System.Windows.Forms.Padding(4);
            this.rdoOutputSide.Name = "rdoOutputSide";
            this.rdoOutputSide.Text = "OUTPUT 쪽";
            this.rdoOutputSide.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoOutputSide.UseVisualStyleBackColor = false;
            //
            // lblTargetCaption
            //
            this.lblTargetCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTargetCaption.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblTargetCaption.Name = "lblTargetCaption";
            this.lblTargetCaption.Text = "교체 위치 X";
            this.lblTargetCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblTargetValue
            //
            this.lblTargetValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTargetValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblTargetValue.Name = "lblTargetValue";
            this.lblTargetValue.Text = "-";
            this.lblTargetValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblStatus
            //
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9.5F);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(14, 0, 14, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "Y/T/Z를 Avoid로 후퇴한 뒤 X를 교체 위치로 이동합니다.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // buttonPanel
            //
            this.buttonPanel.ColumnCount = 3;
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.buttonPanel.Controls.Add(this.btnMove, 0, 0);
            this.buttonPanel.Controls.Add(this.btnAvoid, 1, 0);
            this.buttonPanel.Controls.Add(this.btnClose, 2, 0);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(12, 0, 12, 10);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.RowCount = 1;
            this.buttonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonPanel.TabIndex = 3;
            //
            // btnMove
            //
            this.btnMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this.btnMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMove.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMove.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnMove.ForeColor = System.Drawing.Color.White;
            this.btnMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnMove.Name = "btnMove";
            this.btnMove.TabIndex = 0;
            this.btnMove.Text = "교체 위치 이동";
            this.btnMove.UseVisualStyleBackColor = false;
            this.btnMove.Click += new System.EventHandler(this.btnMove_Click);
            //
            // btnAvoid
            //
            this.btnAvoid.BackColor = System.Drawing.Color.White;
            this.btnAvoid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAvoid.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAvoid.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnAvoid.Margin = new System.Windows.Forms.Padding(4);
            this.btnAvoid.Name = "btnAvoid";
            this.btnAvoid.TabIndex = 1;
            this.btnAvoid.Text = "AVOID 복귀";
            this.btnAvoid.UseVisualStyleBackColor = false;
            this.btnAvoid.Click += new System.EventHandler(this.btnAvoid_Click);
            //
            // btnClose
            //
            this.btnClose.BackColor = System.Drawing.Color.White;
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnClose.Margin = new System.Windows.Forms.Padding(4);
            this.btnClose.Name = "btnClose";
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "닫기";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // ColletChangeDialog
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(640, 380);
            this.Controls.Add(this.rootLayout);
            // 모달리스로 띄우므로 작업 중 위치를 옮길 수 있는 도구창 스타일을 쓴다.
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Name = "ColletChangeDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "COLLET CHANGE MODE";
            this.rootLayout.ResumeLayout(false);
            this.groupSelect.ResumeLayout(false);
            this.selectLayout.ResumeLayout(false);
            this.pickerGroupPanel.ResumeLayout(false);
            this.sideGroupPanel.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
