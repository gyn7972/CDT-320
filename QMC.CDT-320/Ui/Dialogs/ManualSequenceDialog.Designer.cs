namespace QMC.CDT_320.Ui.Dialogs
{
    partial class ManualSequenceDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Button btnInputStep;
        private System.Windows.Forms.Button btnOutputStep;
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
            this.btnInputStep = new System.Windows.Forms.Button();
            this.btnOutputStep = new System.Windows.Forms.Button();
            this.pickerSelectPanel = new System.Windows.Forms.Panel();
            this.rbFrontPicker = new System.Windows.Forms.RadioButton();
            this.rbRearPicker = new System.Windows.Forms.RadioButton();
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
            this.titleLabel.BackColor = System.Drawing.Color.FromArgb(224, 115, 0);
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
            this.mainLayout.Controls.Add(this.btnInputStep, 0, 0);
            this.mainLayout.Controls.Add(this.btnOutputStep, 2, 0);
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
            this.mainLayout.SetColumnSpan(this.btnInputStep, 2);
            this.mainLayout.SetColumnSpan(this.btnOutputStep, 2);
            this.mainLayout.SetColumnSpan(this.pickerSelectPanel, 4);
            this.mainLayout.SetColumnSpan(this.btnAllStep, 2);
            this.mainLayout.SetColumnSpan(this.btnClose, 2);
            //
            // buttons
            //
            ConfigureActionButton(this.btnInputStep, "INPUT STEP");
            ConfigureActionButton(this.btnOutputStep, "OUTPUT STEP");
            ConfigureActionButton(this.btnPickUp, "PICK UP");
            ConfigureActionButton(this.btnBottom, "BOTTOM");
            ConfigureActionButton(this.btnSide, "SIDE");
            ConfigureActionButton(this.btnPlace, "PLACE");
            ConfigureActionButton(this.btnAllStep, "ALL STEP");
            ConfigureActionButton(this.btnClose, "닫기");
            //
            // pickerSelectPanel
            //
            this.pickerSelectPanel.Controls.Add(this.rbRearPicker);
            this.pickerSelectPanel.Controls.Add(this.rbFrontPicker);
            this.pickerSelectPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pickerSelectPanel.Location = new System.Drawing.Point(21, 83);
            this.pickerSelectPanel.Margin = new System.Windows.Forms.Padding(5);
            this.pickerSelectPanel.Name = "pickerSelectPanel";
            this.pickerSelectPanel.Size = new System.Drawing.Size(578, 46);
            this.pickerSelectPanel.TabIndex = 4;
            //
            // rbFrontPicker
            //
            this.rbFrontPicker.Checked = true;
            this.rbFrontPicker.Dock = System.Windows.Forms.DockStyle.Left;
            this.rbFrontPicker.ForeColor = System.Drawing.Color.Black;
            this.rbFrontPicker.Location = new System.Drawing.Point(0, 0);
            this.rbFrontPicker.Name = "rbFrontPicker";
            this.rbFrontPicker.Size = new System.Drawing.Size(140, 46);
            this.rbFrontPicker.TabIndex = 0;
            this.rbFrontPicker.TabStop = true;
            this.rbFrontPicker.Text = "FrontPicker";
            this.rbFrontPicker.UseVisualStyleBackColor = true;
            //
            // rbRearPicker
            //
            this.rbRearPicker.Dock = System.Windows.Forms.DockStyle.Left;
            this.rbRearPicker.ForeColor = System.Drawing.Color.Black;
            this.rbRearPicker.Location = new System.Drawing.Point(140, 0);
            this.rbRearPicker.Name = "rbRearPicker";
            this.rbRearPicker.Size = new System.Drawing.Size(140, 46);
            this.rbRearPicker.TabIndex = 1;
            this.rbRearPicker.Text = "RearPicker";
            this.rbRearPicker.UseVisualStyleBackColor = true;
            //
            // statusLabel
            //
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusLabel.Location = new System.Drawing.Point(0, 302);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Padding = new System.Windows.Forms.Padding(18, 10, 18, 0);
            this.statusLabel.Size = new System.Drawing.Size(620, 128);
            this.statusLabel.TabIndex = 2;
            this.statusLabel.Text = "Auto와 동일한 Material/Die Map/Picker 상태를 사용합니다.";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            //
            // ManualSequenceDialog
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(235, 235, 235);
            this.ClientSize = new System.Drawing.Size(620, 430);
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

        private static void ConfigureActionButton(System.Windows.Forms.Button button, string text)
        {
            button.BackColor = System.Drawing.Color.FromArgb(128, 128, 128);
            button.Dock = System.Windows.Forms.DockStyle.Fill;
            button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            button.ForeColor = System.Drawing.Color.White;
            button.Margin = new System.Windows.Forms.Padding(5);
            button.Text = text;
            button.UseVisualStyleBackColor = false;
        }
    }
}
