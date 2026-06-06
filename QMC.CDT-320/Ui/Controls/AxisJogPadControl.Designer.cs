using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    partial class AxisJogPadControl
    {
        private TableLayoutPanel _layout;
        private Label _lblAxisName;
        private Button _btnTMinus;
        private Button _btnYPlus;
        private Button _btnTPlus;
        private Button _btnXMinus;
        private Button _btnStop;
        private Button _btnXPlus;
        private Button _btnYMinus;

        /// <summary>디자이너 리소스를 정리합니다.</summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._layout = new System.Windows.Forms.TableLayoutPanel();
            this._lblAxisName = new System.Windows.Forms.Label();
            this._btnTMinus = new System.Windows.Forms.Button();
            this._btnYPlus = new System.Windows.Forms.Button();
            this._btnTPlus = new System.Windows.Forms.Button();
            this._btnXMinus = new System.Windows.Forms.Button();
            this._btnStop = new System.Windows.Forms.Button();
            this._btnXPlus = new System.Windows.Forms.Button();
            this._btnYMinus = new System.Windows.Forms.Button();
            this._layout.SuspendLayout();
            this.SuspendLayout();
            // 
            // _layout
            // 
            this._layout.ColumnCount = 3;
            this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._layout.Controls.Add(this._lblAxisName, 0, 0);
            this._layout.Controls.Add(this._btnTMinus, 0, 1);
            this._layout.Controls.Add(this._btnYPlus, 1, 1);
            this._layout.Controls.Add(this._btnTPlus, 2, 1);
            this._layout.Controls.Add(this._btnXMinus, 0, 2);
            this._layout.Controls.Add(this._btnStop, 1, 2);
            this._layout.Controls.Add(this._btnXPlus, 2, 2);
            this._layout.Controls.Add(this._btnYMinus, 1, 3);
            this._layout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._layout.Location = new System.Drawing.Point(0, 0);
            this._layout.Margin = new System.Windows.Forms.Padding(0);
            this._layout.Name = "_layout";
            this._layout.RowCount = 4;
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._layout.Size = new System.Drawing.Size(220, 260);
            this._layout.TabIndex = 0;
            // 
            // _lblAxisName
            // 
            this._lblAxisName.BackColor = System.Drawing.Color.White;
            this._lblAxisName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._layout.SetColumnSpan(this._lblAxisName, 3);
            this._lblAxisName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblAxisName.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this._lblAxisName.Location = new System.Drawing.Point(4, 4);
            this._lblAxisName.Margin = new System.Windows.Forms.Padding(4);
            this._lblAxisName.Name = "_lblAxisName";
            this._lblAxisName.Size = new System.Drawing.Size(212, 32);
            this._lblAxisName.TabIndex = 0;
            this._lblAxisName.Text = "AXIS NAME";
            this._lblAxisName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _btnTMinus
            // 
            this._btnTMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnTMinus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnTMinus.Location = new System.Drawing.Point(8, 48);
            this._btnTMinus.Margin = new System.Windows.Forms.Padding(8);
            this._btnTMinus.Name = "_btnTMinus";
            this._btnTMinus.Size = new System.Drawing.Size(57, 57);
            this._btnTMinus.TabIndex = 1;
            this._btnTMinus.Text = "T-";
            this._btnTMinus.UseVisualStyleBackColor = true;
            // 
            // _btnYPlus
            // 
            this._btnYPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnYPlus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnYPlus.Location = new System.Drawing.Point(81, 48);
            this._btnYPlus.Margin = new System.Windows.Forms.Padding(8);
            this._btnYPlus.Name = "_btnYPlus";
            this._btnYPlus.Size = new System.Drawing.Size(57, 57);
            this._btnYPlus.TabIndex = 2;
            this._btnYPlus.Text = "Y+";
            this._btnYPlus.UseVisualStyleBackColor = true;
            // 
            // _btnTPlus
            // 
            this._btnTPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnTPlus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnTPlus.Location = new System.Drawing.Point(154, 48);
            this._btnTPlus.Margin = new System.Windows.Forms.Padding(8);
            this._btnTPlus.Name = "_btnTPlus";
            this._btnTPlus.Size = new System.Drawing.Size(58, 57);
            this._btnTPlus.TabIndex = 3;
            this._btnTPlus.Text = "T+";
            this._btnTPlus.UseVisualStyleBackColor = true;
            // 
            // _btnXMinus
            // 
            this._btnXMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnXMinus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnXMinus.Location = new System.Drawing.Point(8, 121);
            this._btnXMinus.Margin = new System.Windows.Forms.Padding(8);
            this._btnXMinus.Name = "_btnXMinus";
            this._btnXMinus.Size = new System.Drawing.Size(57, 57);
            this._btnXMinus.TabIndex = 4;
            this._btnXMinus.Text = "X-";
            this._btnXMinus.UseVisualStyleBackColor = true;
            // 
            // _btnStop
            // 
            this._btnStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnStop.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._btnStop.Location = new System.Drawing.Point(81, 121);
            this._btnStop.Margin = new System.Windows.Forms.Padding(8);
            this._btnStop.Name = "_btnStop";
            this._btnStop.Size = new System.Drawing.Size(57, 57);
            this._btnStop.TabIndex = 5;
            this._btnStop.Text = "STOP";
            this._btnStop.UseVisualStyleBackColor = true;
            // 
            // _btnXPlus
            // 
            this._btnXPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnXPlus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnXPlus.Location = new System.Drawing.Point(154, 121);
            this._btnXPlus.Margin = new System.Windows.Forms.Padding(8);
            this._btnXPlus.Name = "_btnXPlus";
            this._btnXPlus.Size = new System.Drawing.Size(58, 57);
            this._btnXPlus.TabIndex = 6;
            this._btnXPlus.Text = "X+";
            this._btnXPlus.UseVisualStyleBackColor = true;
            // 
            // _btnYMinus
            // 
            this._btnYMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnYMinus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnYMinus.Location = new System.Drawing.Point(81, 194);
            this._btnYMinus.Margin = new System.Windows.Forms.Padding(8);
            this._btnYMinus.Name = "_btnYMinus";
            this._btnYMinus.Size = new System.Drawing.Size(57, 58);
            this._btnYMinus.TabIndex = 7;
            this._btnYMinus.Text = "Y-";
            this._btnYMinus.UseVisualStyleBackColor = true;
            // 
            // AxisJogPadControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            this.Controls.Add(this._layout);
            this.Name = "AxisJogPadControl";
            this.Size = new System.Drawing.Size(220, 260);
            this._layout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
