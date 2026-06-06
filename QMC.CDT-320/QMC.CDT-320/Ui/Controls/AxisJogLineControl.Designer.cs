using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    partial class AxisJogLineControl
    {
        private TableLayoutPanel _layout;
        private Label _lblAxisName;
        private Button _btnPlus;
        private Button _btnStop;
        private Button _btnMinus;

        /// <summary>디자이너 리소스를 정리합니다.</summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._layout = new System.Windows.Forms.TableLayoutPanel();
            this._lblAxisName = new System.Windows.Forms.Label();
            this._btnPlus = new System.Windows.Forms.Button();
            this._btnStop = new System.Windows.Forms.Button();
            this._btnMinus = new System.Windows.Forms.Button();
            this._layout.SuspendLayout();
            this.SuspendLayout();
            // 
            // _layout
            // 
            this._layout.ColumnCount = 1;
            this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._layout.Controls.Add(this._lblAxisName, 0, 0);
            this._layout.Controls.Add(this._btnPlus, 0, 1);
            this._layout.Controls.Add(this._btnStop, 0, 2);
            this._layout.Controls.Add(this._btnMinus, 0, 3);
            this._layout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._layout.Location = new System.Drawing.Point(0, 0);
            this._layout.Margin = new System.Windows.Forms.Padding(0);
            this._layout.Name = "_layout";
            this._layout.RowCount = 4;
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._layout.Size = new System.Drawing.Size(88, 300);
            this._layout.TabIndex = 0;
            // 
            // _lblAxisName
            // 
            this._lblAxisName.BackColor = System.Drawing.Color.White;
            this._lblAxisName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblAxisName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblAxisName.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this._lblAxisName.Location = new System.Drawing.Point(4, 4);
            this._lblAxisName.Margin = new System.Windows.Forms.Padding(4);
            this._lblAxisName.Name = "_lblAxisName";
            this._lblAxisName.Size = new System.Drawing.Size(80, 30);
            this._lblAxisName.TabIndex = 0;
            this._lblAxisName.Text = "WAFER\r\nLIFTER Z";
            this._lblAxisName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _btnPlus
            // 
            this._btnPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnPlus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnPlus.Location = new System.Drawing.Point(8, 46);
            this._btnPlus.Margin = new System.Windows.Forms.Padding(8);
            this._btnPlus.Name = "_btnPlus";
            this._btnPlus.Size = new System.Drawing.Size(72, 71);
            this._btnPlus.TabIndex = 1;
            this._btnPlus.Text = "Z+";
            this._btnPlus.UseVisualStyleBackColor = true;
            // 
            // _btnStop
            // 
            this._btnStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnStop.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._btnStop.Location = new System.Drawing.Point(8, 133);
            this._btnStop.Margin = new System.Windows.Forms.Padding(8);
            this._btnStop.Name = "_btnStop";
            this._btnStop.Size = new System.Drawing.Size(72, 71);
            this._btnStop.TabIndex = 2;
            this._btnStop.Text = "STOP";
            this._btnStop.UseVisualStyleBackColor = true;
            // 
            // _btnMinus
            // 
            this._btnMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMinus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnMinus.Location = new System.Drawing.Point(8, 220);
            this._btnMinus.Margin = new System.Windows.Forms.Padding(8);
            this._btnMinus.Name = "_btnMinus";
            this._btnMinus.Size = new System.Drawing.Size(72, 72);
            this._btnMinus.TabIndex = 3;
            this._btnMinus.Text = "Z-";
            this._btnMinus.UseVisualStyleBackColor = true;
            // 
            // AxisJogLineControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            this.Controls.Add(this._layout);
            this.Name = "AxisJogLineControl";
            this.Size = new System.Drawing.Size(88, 300);
            this._layout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
