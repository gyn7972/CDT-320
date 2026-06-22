namespace QMC.CDT_320.Ui.Dialogs
{
    partial class ManualSequenceDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Button btnInputLoad;
        private System.Windows.Forms.Button btnInputUnload;
        private System.Windows.Forms.Button btnOutputLoad;
        private System.Windows.Forms.Button btnOutputUnload;
        private System.Windows.Forms.Panel pickerSelectPanel;
        private System.Windows.Forms.RadioButton rbFrontPicker;
        private System.Windows.Forms.RadioButton rbRearPicker;
        private System.Windows.Forms.Button btnPickUp;
        private System.Windows.Forms.Button btnBottom;
        private System.Windows.Forms.Button btnSide;
        private System.Windows.Forms.Button btnPlace;
        private System.Windows.Forms.Button btnAllStep;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label statusLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.titleLabel = new System.Windows.Forms.Label();
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnInputLoad = new System.Windows.Forms.Button();
            this.btnInputUnload = new System.Windows.Forms.Button();
            this.btnOutputLoad = new System.Windows.Forms.Button();
            this.btnOutputUnload = new System.Windows.Forms.Button();
            this.pickerSelectPanel = new System.Windows.Forms.Panel();
            this.rbRearPicker = new System.Windows.Forms.RadioButton();
            this.rbFrontPicker = new System.Windows.Forms.RadioButton();
            this.btnPickUp = new System.Windows.Forms.Button();
            this.btnBottom = new System.Windows.Forms.Button();
            this.btnSide = new System.Windows.Forms.Button();
            this.btnPlace = new System.Windows.Forms.Button();
            this.btnAllStep = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.mainLayout.SuspendLayout();
            this.pickerSelectPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(115)))), ((int)(((byte)(0)))));
            this.titleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleLabel.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(0, 0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.titleLabel.Size = new System.Drawing.Size(620, 42);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "MANUAL SEQUENCE";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 4;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainLayout.Controls.Add(this.btnInputLoad, 0, 0);
            this.mainLayout.Controls.Add(this.btnInputUnload, 1, 0);
            this.mainLayout.Controls.Add(this.btnOutputLoad, 2, 0);
            this.mainLayout.Controls.Add(this.btnOutputUnload, 3, 0);
            this.mainLayout.Controls.Add(this.pickerSelectPanel, 0, 1);
            this.mainLayout.Controls.Add(this.btnPickUp, 0, 2);
            this.mainLayout.Controls.Add(this.btnBottom, 1, 2);
            this.mainLayout.Controls.Add(this.btnSide, 2, 2);
            this.mainLayout.Controls.Add(this.btnPlace, 3, 2);
            this.mainLayout.Controls.Add(this.btnAllStep, 0, 3);
            this.mainLayout.Controls.Add(this.btnClose, 2, 3);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.mainLayout.Location = new System.Drawing.Point(0, 42);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.Padding = new System.Windows.Forms.Padding(16);
            this.mainLayout.RowCount = 4;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainLayout.Size = new System.Drawing.Size(620, 260);
            this.mainLayout.TabIndex = 1;
            // 
            // btnInputLoad
            // 
            this.btnInputLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInputLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInputLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInputLoad.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnInputLoad.ForeColor = System.Drawing.Color.White;
            this.btnInputLoad.Location = new System.Drawing.Point(21, 21);
            this.btnInputLoad.Margin = new System.Windows.Forms.Padding(5);
            this.btnInputLoad.Name = "btnInputLoad";
            this.btnInputLoad.Size = new System.Drawing.Size(137, 47);
            this.btnInputLoad.TabIndex = 0;
            this.btnInputLoad.Text = "INPUT LOAD";
            this.btnInputLoad.UseVisualStyleBackColor = false;
            // 
            // btnInputUnload
            // 
            this.btnInputUnload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInputUnload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInputUnload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInputUnload.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnInputUnload.ForeColor = System.Drawing.Color.White;
            this.btnInputUnload.Location = new System.Drawing.Point(168, 21);
            this.btnInputUnload.Margin = new System.Windows.Forms.Padding(5);
            this.btnInputUnload.Name = "btnInputUnload";
            this.btnInputUnload.Size = new System.Drawing.Size(137, 47);
            this.btnInputUnload.TabIndex = 1;
            this.btnInputUnload.Text = "INPUT UNLOAD";
            this.btnInputUnload.UseVisualStyleBackColor = false;
            // 
            // btnOutputLoad
            // 
            this.btnOutputLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnOutputLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOutputLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOutputLoad.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnOutputLoad.ForeColor = System.Drawing.Color.White;
            this.btnOutputLoad.Location = new System.Drawing.Point(315, 21);
            this.btnOutputLoad.Margin = new System.Windows.Forms.Padding(5);
            this.btnOutputLoad.Name = "btnOutputLoad";
            this.btnOutputLoad.Size = new System.Drawing.Size(137, 47);
            this.btnOutputLoad.TabIndex = 2;
            this.btnOutputLoad.Text = "OUTPUT LOAD";
            this.btnOutputLoad.UseVisualStyleBackColor = false;
            // 
            // btnOutputUnload
            // 
            this.btnOutputUnload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnOutputUnload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOutputUnload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOutputUnload.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnOutputUnload.ForeColor = System.Drawing.Color.White;
            this.btnOutputUnload.Location = new System.Drawing.Point(462, 21);
            this.btnOutputUnload.Margin = new System.Windows.Forms.Padding(5);
            this.btnOutputUnload.Name = "btnOutputUnload";
            this.btnOutputUnload.Size = new System.Drawing.Size(137, 47);
            this.btnOutputUnload.TabIndex = 3;
            this.btnOutputUnload.Text = "OUTPUT UNLOAD";
            this.btnOutputUnload.UseVisualStyleBackColor = false;
            // 
            // pickerSelectPanel
            // 
            this.mainLayout.SetColumnSpan(this.pickerSelectPanel, 4);
            this.pickerSelectPanel.Controls.Add(this.rbRearPicker);
            this.pickerSelectPanel.Controls.Add(this.rbFrontPicker);
            this.pickerSelectPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pickerSelectPanel.Location = new System.Drawing.Point(21, 78);
            this.pickerSelectPanel.Margin = new System.Windows.Forms.Padding(5);
            this.pickerSelectPanel.Name = "pickerSelectPanel";
            this.pickerSelectPanel.Size = new System.Drawing.Size(578, 47);
            this.pickerSelectPanel.TabIndex = 4;
            // 
            // rbRearPicker
            // 
            this.rbRearPicker.Dock = System.Windows.Forms.DockStyle.Left;
            this.rbRearPicker.ForeColor = System.Drawing.Color.Black;
            this.rbRearPicker.Location = new System.Drawing.Point(140, 0);
            this.rbRearPicker.Name = "rbRearPicker";
            this.rbRearPicker.Size = new System.Drawing.Size(140, 47);
            this.rbRearPicker.TabIndex = 1;
            this.rbRearPicker.Text = "RearPicker";
            this.rbRearPicker.UseVisualStyleBackColor = true;
            // 
            // rbFrontPicker
            // 
            this.rbFrontPicker.Checked = true;
            this.rbFrontPicker.Dock = System.Windows.Forms.DockStyle.Left;
            this.rbFrontPicker.ForeColor = System.Drawing.Color.Black;
            this.rbFrontPicker.Location = new System.Drawing.Point(0, 0);
            this.rbFrontPicker.Name = "rbFrontPicker";
            this.rbFrontPicker.Size = new System.Drawing.Size(140, 47);
            this.rbFrontPicker.TabIndex = 0;
            this.rbFrontPicker.TabStop = true;
            this.rbFrontPicker.Text = "FrontPicker";
            this.rbFrontPicker.UseVisualStyleBackColor = true;
            // 
            // btnPickUp
            // 
            this.btnPickUp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPickUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPickUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPickUp.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnPickUp.ForeColor = System.Drawing.Color.White;
            this.btnPickUp.Location = new System.Drawing.Point(21, 135);
            this.btnPickUp.Margin = new System.Windows.Forms.Padding(5);
            this.btnPickUp.Name = "btnPickUp";
            this.btnPickUp.Size = new System.Drawing.Size(137, 47);
            this.btnPickUp.TabIndex = 5;
            this.btnPickUp.Text = "PICK UP";
            this.btnPickUp.UseVisualStyleBackColor = false;
            // 
            // btnBottom
            // 
            this.btnBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBottom.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBottom.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnBottom.ForeColor = System.Drawing.Color.White;
            this.btnBottom.Location = new System.Drawing.Point(168, 135);
            this.btnBottom.Margin = new System.Windows.Forms.Padding(5);
            this.btnBottom.Name = "btnBottom";
            this.btnBottom.Size = new System.Drawing.Size(137, 47);
            this.btnBottom.TabIndex = 6;
            this.btnBottom.Text = "BOTTOM";
            this.btnBottom.UseVisualStyleBackColor = false;
            // 
            // btnSide
            // 
            this.btnSide.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSide.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSide.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSide.ForeColor = System.Drawing.Color.White;
            this.btnSide.Location = new System.Drawing.Point(315, 135);
            this.btnSide.Margin = new System.Windows.Forms.Padding(5);
            this.btnSide.Name = "btnSide";
            this.btnSide.Size = new System.Drawing.Size(137, 47);
            this.btnSide.TabIndex = 7;
            this.btnSide.Text = "SIDE";
            this.btnSide.UseVisualStyleBackColor = false;
            // 
            // btnPlace
            // 
            this.btnPlace.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPlace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlace.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlace.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnPlace.ForeColor = System.Drawing.Color.White;
            this.btnPlace.Location = new System.Drawing.Point(462, 135);
            this.btnPlace.Margin = new System.Windows.Forms.Padding(5);
            this.btnPlace.Name = "btnPlace";
            this.btnPlace.Size = new System.Drawing.Size(137, 47);
            this.btnPlace.TabIndex = 8;
            this.btnPlace.Text = "PLACE";
            this.btnPlace.UseVisualStyleBackColor = false;
            // 
            // btnAllStep
            // 
            this.btnAllStep.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.mainLayout.SetColumnSpan(this.btnAllStep, 2);
            this.btnAllStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAllStep.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAllStep.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnAllStep.ForeColor = System.Drawing.Color.White;
            this.btnAllStep.Location = new System.Drawing.Point(21, 192);
            this.btnAllStep.Margin = new System.Windows.Forms.Padding(5);
            this.btnAllStep.Name = "btnAllStep";
            this.btnAllStep.Size = new System.Drawing.Size(284, 47);
            this.btnAllStep.TabIndex = 9;
            this.btnAllStep.Text = "ALL STEP";
            this.btnAllStep.UseVisualStyleBackColor = false;
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.mainLayout.SetColumnSpan(this.btnClose, 2);
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(315, 192);
            this.btnClose.Margin = new System.Windows.Forms.Padding(5);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(284, 47);
            this.btnClose.TabIndex = 10;
            this.btnClose.Text = "닫기";
            this.btnClose.UseVisualStyleBackColor = false;
            // 
            // statusLabel
            // 
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusLabel.Location = new System.Drawing.Point(0, 302);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Padding = new System.Windows.Forms.Padding(18, 10, 18, 0);
            this.statusLabel.Size = new System.Drawing.Size(620, 62);
            this.statusLabel.TabIndex = 2;
            this.statusLabel.Text = "Auto와 동일한 Material/Die Map/Picker 상태를 사용합니다.";
            // 
            // ManualSequenceDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.ClientSize = new System.Drawing.Size(620, 364);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.mainLayout);
            this.Controls.Add(this.titleLabel);
            this.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ManualSequenceDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manual Sequence";
            this.mainLayout.ResumeLayout(false);
            this.pickerSelectPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
