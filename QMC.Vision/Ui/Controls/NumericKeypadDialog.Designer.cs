namespace QMC.Vision.Ui.Controls
{
    partial class NumericKeypadDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TextBox txtValue;
        private System.Windows.Forms.Label lblUnit;
        private System.Windows.Forms.TableLayoutPanel keypadLayout;
        private System.Windows.Forms.Button btn7;
        private System.Windows.Forms.Button btn8;
        private System.Windows.Forms.Button btn9;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Button btn4;
        private System.Windows.Forms.Button btn5;
        private System.Windows.Forms.Button btn6;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btn1;
        private System.Windows.Forms.Button btn2;
        private System.Windows.Forms.Button btn3;
        private System.Windows.Forms.Button btnSign;
        private System.Windows.Forms.Button btn0;
        private System.Windows.Forms.Button btnDot;
        private System.Windows.Forms.Button btn000;
        private System.Windows.Forms.Button btnAdd1000;
        private System.Windows.Forms.Button btnAdd100;
        private System.Windows.Forms.Button btnAdd10;
        private System.Windows.Forms.Button btnAdd1;
        private System.Windows.Forms.Button btnSub1000;
        private System.Windows.Forms.Button btnSub100;
        private System.Windows.Forms.Button btnSub10;
        private System.Windows.Forms.Button btnSub1;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.txtValue = new System.Windows.Forms.TextBox();
            this.lblUnit = new System.Windows.Forms.Label();
            this.keypadLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btn7 = new System.Windows.Forms.Button();
            this.btn8 = new System.Windows.Forms.Button();
            this.btn9 = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.btn4 = new System.Windows.Forms.Button();
            this.btn5 = new System.Windows.Forms.Button();
            this.btn6 = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btn1 = new System.Windows.Forms.Button();
            this.btn2 = new System.Windows.Forms.Button();
            this.btn3 = new System.Windows.Forms.Button();
            this.btnSign = new System.Windows.Forms.Button();
            this.btn0 = new System.Windows.Forms.Button();
            this.btnDot = new System.Windows.Forms.Button();
            this.btn000 = new System.Windows.Forms.Button();
            this.btnAdd1000 = new System.Windows.Forms.Button();
            this.btnAdd100 = new System.Windows.Forms.Button();
            this.btnAdd10 = new System.Windows.Forms.Button();
            this.btnAdd1 = new System.Windows.Forms.Button();
            this.btnSub1000 = new System.Windows.Forms.Button();
            this.btnSub100 = new System.Windows.Forms.Button();
            this.btnSub10 = new System.Windows.Forms.Button();
            this.btnSub1 = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.keypadLayout.SuspendLayout();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoEllipsis = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(39)))), ((int)(((byte)(45)))));
            this.lblTitle.Location = new System.Drawing.Point(18, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(448, 28);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Parameter";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtValue
            //
            this.txtValue.BackColor = System.Drawing.Color.White;
            this.txtValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtValue.Font = new System.Drawing.Font("Consolas", 18F, System.Drawing.FontStyle.Bold);
            this.txtValue.Location = new System.Drawing.Point(18, 50);
            this.txtValue.Name = "txtValue";
            this.txtValue.Size = new System.Drawing.Size(360, 36);
            this.txtValue.TabIndex = 1;
            this.txtValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblUnit
            //
            this.lblUnit.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblUnit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(89)))), ((int)(((byte)(98)))));
            this.lblUnit.Location = new System.Drawing.Point(384, 50);
            this.lblUnit.Name = "lblUnit";
            this.lblUnit.Size = new System.Drawing.Size(72, 36);
            this.lblUnit.TabIndex = 2;
            this.lblUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // keypadLayout
            //
            this.keypadLayout.ColumnCount = 6;
            this.keypadLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.keypadLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.keypadLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.keypadLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.keypadLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.keypadLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.keypadLayout.Controls.Add(this.btn7, 0, 0);
            this.keypadLayout.Controls.Add(this.btn8, 1, 0);
            this.keypadLayout.Controls.Add(this.btn9, 2, 0);
            this.keypadLayout.Controls.Add(this.btnBack, 3, 0);
            this.keypadLayout.Controls.Add(this.btnAdd1000, 4, 0);
            this.keypadLayout.Controls.Add(this.btnSub1000, 5, 0);
            this.keypadLayout.Controls.Add(this.btn4, 0, 1);
            this.keypadLayout.Controls.Add(this.btn5, 1, 1);
            this.keypadLayout.Controls.Add(this.btn6, 2, 1);
            this.keypadLayout.Controls.Add(this.btnClear, 3, 1);
            this.keypadLayout.Controls.Add(this.btnAdd100, 4, 1);
            this.keypadLayout.Controls.Add(this.btnSub100, 5, 1);
            this.keypadLayout.Controls.Add(this.btn1, 0, 2);
            this.keypadLayout.Controls.Add(this.btn2, 1, 2);
            this.keypadLayout.Controls.Add(this.btn3, 2, 2);
            this.keypadLayout.Controls.Add(this.btnSign, 3, 2);
            this.keypadLayout.Controls.Add(this.btnAdd10, 4, 2);
            this.keypadLayout.Controls.Add(this.btnSub10, 5, 2);
            this.keypadLayout.Controls.Add(this.btn0, 0, 3);
            this.keypadLayout.Controls.Add(this.btnDot, 1, 3);
            this.keypadLayout.Controls.Add(this.btn000, 2, 3);
            this.keypadLayout.Controls.Add(this.btnOk, 3, 3);
            this.keypadLayout.Controls.Add(this.btnAdd1, 4, 3);
            this.keypadLayout.Controls.Add(this.btnSub1, 5, 3);
            this.keypadLayout.Controls.Add(this.btnCancel, 0, 4);
            this.keypadLayout.SetColumnSpan(this.btnCancel, 6);
            this.keypadLayout.Location = new System.Drawing.Point(18, 98);
            this.keypadLayout.Name = "keypadLayout";
            this.keypadLayout.RowCount = 5;
            this.keypadLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.keypadLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.keypadLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.keypadLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.keypadLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.keypadLayout.Size = new System.Drawing.Size(448, 280);
            this.keypadLayout.TabIndex = 3;
            //
            // btn7
            //
            this.btn7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn7.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btn7.Location = new System.Drawing.Point(3, 3);
            this.btn7.Name = "btn7";
            this.btn7.Size = new System.Drawing.Size(68, 50);
            this.btn7.TabIndex = 0;
            this.btn7.Text = "7";
            this.btn7.UseVisualStyleBackColor = true;
            this.btn7.Click += new System.EventHandler(this.DigitButton_Click);
            //
            // btn8
            //
            this.btn8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn8.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btn8.Location = new System.Drawing.Point(77, 3);
            this.btn8.Name = "btn8";
            this.btn8.Size = new System.Drawing.Size(68, 50);
            this.btn8.TabIndex = 1;
            this.btn8.Text = "8";
            this.btn8.UseVisualStyleBackColor = true;
            this.btn8.Click += new System.EventHandler(this.DigitButton_Click);
            //
            // btn9
            //
            this.btn9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn9.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btn9.Location = new System.Drawing.Point(151, 3);
            this.btn9.Name = "btn9";
            this.btn9.Size = new System.Drawing.Size(68, 50);
            this.btn9.TabIndex = 2;
            this.btn9.Text = "9";
            this.btn9.UseVisualStyleBackColor = true;
            this.btn9.Click += new System.EventHandler(this.DigitButton_Click);
            //
            // btnBack
            //
            this.btnBack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBack.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnBack.Location = new System.Drawing.Point(225, 3);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(68, 50);
            this.btnBack.TabIndex = 3;
            this.btnBack.Text = "BS";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.BackButton_Click);
            //
            // btn4
            //
            this.btn4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn4.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btn4.Location = new System.Drawing.Point(3, 59);
            this.btn4.Name = "btn4";
            this.btn4.Size = new System.Drawing.Size(68, 50);
            this.btn4.TabIndex = 4;
            this.btn4.Text = "4";
            this.btn4.UseVisualStyleBackColor = true;
            this.btn4.Click += new System.EventHandler(this.DigitButton_Click);
            //
            // btn5
            //
            this.btn5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn5.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btn5.Location = new System.Drawing.Point(77, 59);
            this.btn5.Name = "btn5";
            this.btn5.Size = new System.Drawing.Size(68, 50);
            this.btn5.TabIndex = 5;
            this.btn5.Text = "5";
            this.btn5.UseVisualStyleBackColor = true;
            this.btn5.Click += new System.EventHandler(this.DigitButton_Click);
            //
            // btn6
            //
            this.btn6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn6.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btn6.Location = new System.Drawing.Point(151, 59);
            this.btn6.Name = "btn6";
            this.btn6.Size = new System.Drawing.Size(68, 50);
            this.btn6.TabIndex = 6;
            this.btn6.Text = "6";
            this.btn6.UseVisualStyleBackColor = true;
            this.btn6.Click += new System.EventHandler(this.DigitButton_Click);
            //
            // btnClear
            //
            this.btnClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnClear.Location = new System.Drawing.Point(225, 59);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(68, 50);
            this.btnClear.TabIndex = 7;
            this.btnClear.Text = "CLR";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.ClearButton_Click);
            //
            // btn1
            //
            this.btn1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn1.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btn1.Location = new System.Drawing.Point(3, 115);
            this.btn1.Name = "btn1";
            this.btn1.Size = new System.Drawing.Size(68, 50);
            this.btn1.TabIndex = 8;
            this.btn1.Text = "1";
            this.btn1.UseVisualStyleBackColor = true;
            this.btn1.Click += new System.EventHandler(this.DigitButton_Click);
            //
            // btn2
            //
            this.btn2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn2.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btn2.Location = new System.Drawing.Point(77, 115);
            this.btn2.Name = "btn2";
            this.btn2.Size = new System.Drawing.Size(68, 50);
            this.btn2.TabIndex = 9;
            this.btn2.Text = "2";
            this.btn2.UseVisualStyleBackColor = true;
            this.btn2.Click += new System.EventHandler(this.DigitButton_Click);
            //
            // btn3
            //
            this.btn3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn3.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btn3.Location = new System.Drawing.Point(151, 115);
            this.btn3.Name = "btn3";
            this.btn3.Size = new System.Drawing.Size(68, 50);
            this.btn3.TabIndex = 10;
            this.btn3.Text = "3";
            this.btn3.UseVisualStyleBackColor = true;
            this.btn3.Click += new System.EventHandler(this.DigitButton_Click);
            //
            // btnSign
            //
            this.btnSign.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSign.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnSign.Location = new System.Drawing.Point(225, 115);
            this.btnSign.Name = "btnSign";
            this.btnSign.Size = new System.Drawing.Size(68, 50);
            this.btnSign.TabIndex = 11;
            this.btnSign.Text = "+/-";
            this.btnSign.UseVisualStyleBackColor = true;
            this.btnSign.Click += new System.EventHandler(this.SignButton_Click);
            //
            // btn0
            //
            this.btn0.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn0.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btn0.Location = new System.Drawing.Point(3, 171);
            this.btn0.Name = "btn0";
            this.btn0.Size = new System.Drawing.Size(68, 50);
            this.btn0.TabIndex = 12;
            this.btn0.Text = "0";
            this.btn0.UseVisualStyleBackColor = true;
            this.btn0.Click += new System.EventHandler(this.DigitButton_Click);
            //
            // btnDot
            //
            this.btnDot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDot.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btnDot.Location = new System.Drawing.Point(77, 171);
            this.btnDot.Name = "btnDot";
            this.btnDot.Size = new System.Drawing.Size(68, 50);
            this.btnDot.TabIndex = 13;
            this.btnDot.Text = ".";
            this.btnDot.UseVisualStyleBackColor = true;
            this.btnDot.Click += new System.EventHandler(this.DotButton_Click);
            //
            // btn000
            //
            this.btn000.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn000.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btn000.Location = new System.Drawing.Point(151, 171);
            this.btn000.Name = "btn000";
            this.btn000.Size = new System.Drawing.Size(68, 50);
            this.btn000.TabIndex = 16;
            this.btn000.Text = "000";
            this.btn000.UseVisualStyleBackColor = true;
            this.btn000.Click += new System.EventHandler(this.TripleZeroButton_Click);
            //
            // btnAdd1000
            //
            this.btnAdd1000.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAdd1000.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAdd1000.Location = new System.Drawing.Point(299, 3);
            this.btnAdd1000.Name = "btnAdd1000";
            this.btnAdd1000.Size = new System.Drawing.Size(72, 50);
            this.btnAdd1000.TabIndex = 17;
            this.btnAdd1000.Tag = "1000";
            this.btnAdd1000.Text = "+1000";
            this.btnAdd1000.UseVisualStyleBackColor = true;
            this.btnAdd1000.Click += new System.EventHandler(this.IncrementButton_Click);
            //
            // btnSub1000
            //
            this.btnSub1000.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSub1000.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSub1000.Location = new System.Drawing.Point(373, 3);
            this.btnSub1000.Name = "btnSub1000";
            this.btnSub1000.Size = new System.Drawing.Size(72, 50);
            this.btnSub1000.TabIndex = 21;
            this.btnSub1000.Tag = "-1000";
            this.btnSub1000.Text = "-1000";
            this.btnSub1000.UseVisualStyleBackColor = true;
            this.btnSub1000.Click += new System.EventHandler(this.IncrementButton_Click);
            //
            // btnAdd100
            //
            this.btnAdd100.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAdd100.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAdd100.Location = new System.Drawing.Point(299, 59);
            this.btnAdd100.Name = "btnAdd100";
            this.btnAdd100.Size = new System.Drawing.Size(72, 50);
            this.btnAdd100.TabIndex = 18;
            this.btnAdd100.Tag = "100";
            this.btnAdd100.Text = "+100";
            this.btnAdd100.UseVisualStyleBackColor = true;
            this.btnAdd100.Click += new System.EventHandler(this.IncrementButton_Click);
            //
            // btnSub100
            //
            this.btnSub100.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSub100.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSub100.Location = new System.Drawing.Point(373, 59);
            this.btnSub100.Name = "btnSub100";
            this.btnSub100.Size = new System.Drawing.Size(72, 50);
            this.btnSub100.TabIndex = 22;
            this.btnSub100.Tag = "-100";
            this.btnSub100.Text = "-100";
            this.btnSub100.UseVisualStyleBackColor = true;
            this.btnSub100.Click += new System.EventHandler(this.IncrementButton_Click);
            //
            // btnAdd10
            //
            this.btnAdd10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAdd10.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAdd10.Location = new System.Drawing.Point(299, 115);
            this.btnAdd10.Name = "btnAdd10";
            this.btnAdd10.Size = new System.Drawing.Size(72, 50);
            this.btnAdd10.TabIndex = 19;
            this.btnAdd10.Tag = "10";
            this.btnAdd10.Text = "+10";
            this.btnAdd10.UseVisualStyleBackColor = true;
            this.btnAdd10.Click += new System.EventHandler(this.IncrementButton_Click);
            //
            // btnSub10
            //
            this.btnSub10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSub10.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSub10.Location = new System.Drawing.Point(373, 115);
            this.btnSub10.Name = "btnSub10";
            this.btnSub10.Size = new System.Drawing.Size(72, 50);
            this.btnSub10.TabIndex = 23;
            this.btnSub10.Tag = "-10";
            this.btnSub10.Text = "-10";
            this.btnSub10.UseVisualStyleBackColor = true;
            this.btnSub10.Click += new System.EventHandler(this.IncrementButton_Click);
            //
            // btnAdd1
            //
            this.btnAdd1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAdd1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAdd1.Location = new System.Drawing.Point(299, 171);
            this.btnAdd1.Name = "btnAdd1";
            this.btnAdd1.Size = new System.Drawing.Size(72, 50);
            this.btnAdd1.TabIndex = 20;
            this.btnAdd1.Tag = "1";
            this.btnAdd1.Text = "+1";
            this.btnAdd1.UseVisualStyleBackColor = true;
            this.btnAdd1.Click += new System.EventHandler(this.IncrementButton_Click);
            //
            // btnSub1
            //
            this.btnSub1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSub1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSub1.Location = new System.Drawing.Point(373, 171);
            this.btnSub1.Name = "btnSub1";
            this.btnSub1.Size = new System.Drawing.Size(72, 50);
            this.btnSub1.TabIndex = 24;
            this.btnSub1.Tag = "-1";
            this.btnSub1.Text = "-1";
            this.btnSub1.UseVisualStyleBackColor = true;
            this.btnSub1.Click += new System.EventHandler(this.IncrementButton_Click);
            //
            // btnOk
            //
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(113)))), ((int)(((byte)(239)))));
            this.btnOk.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnOk.ForeColor = System.Drawing.Color.White;
            this.btnOk.Location = new System.Drawing.Point(225, 171);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(68, 50);
            this.btnOk.TabIndex = 14;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = false;
            this.btnOk.Click += new System.EventHandler(this.OkButton_Click);
            //
            // btnCancel
            //
            this.btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCancel.Location = new System.Drawing.Point(3, 227);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(442, 50);
            this.btnCancel.TabIndex = 15;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.CancelButton_Click);
            //
            // NumericKeypadDialog
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(484, 396);
            this.Controls.Add(this.keypadLayout);
            this.Controls.Add(this.lblUnit);
            this.Controls.Add(this.txtValue);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NumericKeypadDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Numeric Input";
            this.keypadLayout.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
