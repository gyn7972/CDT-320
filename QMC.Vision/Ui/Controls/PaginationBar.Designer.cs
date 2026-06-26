using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    partial class PaginationBar
    {
        private System.ComponentModel.IContainer components = null;

        private ComboBox _cmbSize;
        private Button   _btnFirst;
        private Button   _btnPrev;
        private TextBox  _txtPage;
        private Button   _btnGo;
        private Label    _lblTotal;
        private Button   _btnNext;
        private Button   _btnLast;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._cmbSize = new ComboBox();
            this._btnFirst = new Button();
            this._btnPrev = new Button();
            this._txtPage = new TextBox();
            this._btnGo = new Button();
            this._lblTotal = new Label();
            this._btnNext = new Button();
            this._btnLast = new Button();
            this.SuspendLayout();

            // _cmbSize
            this._cmbSize.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cmbSize.Width = 90;
            this._cmbSize.Margin = new Padding(2, 2, 12, 2);
            this._cmbSize.Items.AddRange(new object[] { "100행", "200행", "500행", "1000행" });
            this._cmbSize.SelectedIndex = 0;   // 기본 100행 — 이벤트 배선 전에 설정(초기 발생 방지)
            this._cmbSize.SelectedIndexChanged += new System.EventHandler(this._cmbSize_SelectedIndexChanged);

            // _btnFirst
            this._btnFirst.Text = "◀◀";
            this._btnFirst.Width = 38;
            this._btnFirst.Margin = new Padding(2);
            this._btnFirst.FlatStyle = FlatStyle.System;
            this._btnFirst.Click += new System.EventHandler(this._btnFirst_Click);

            // _btnPrev
            this._btnPrev.Text = "◀";
            this._btnPrev.Width = 38;
            this._btnPrev.Margin = new Padding(2);
            this._btnPrev.FlatStyle = FlatStyle.System;
            this._btnPrev.Click += new System.EventHandler(this._btnPrev_Click);

            // _txtPage
            this._txtPage.Width = 48;
            this._txtPage.Margin = new Padding(4, 4, 2, 2);
            this._txtPage.TextAlign = HorizontalAlignment.Center;
            this._txtPage.KeyDown += new KeyEventHandler(this._txtPage_KeyDown);

            // _btnGo
            this._btnGo.Text = "이동";
            this._btnGo.Width = 38;
            this._btnGo.Margin = new Padding(2);
            this._btnGo.FlatStyle = FlatStyle.System;
            this._btnGo.Click += new System.EventHandler(this._btnGo_Click);

            // _lblTotal
            this._lblTotal.AutoSize = true;
            this._lblTotal.Margin = new Padding(6, 8, 10, 2);
            this._lblTotal.Text = "/ 1 (0행)";

            // _btnNext
            this._btnNext.Text = "▶";
            this._btnNext.Width = 38;
            this._btnNext.Margin = new Padding(2);
            this._btnNext.FlatStyle = FlatStyle.System;
            this._btnNext.Click += new System.EventHandler(this._btnNext_Click);

            // _btnLast
            this._btnLast.Text = "▶▶";
            this._btnLast.Width = 38;
            this._btnLast.Margin = new Padding(2);
            this._btnLast.FlatStyle = FlatStyle.System;
            this._btnLast.Click += new System.EventHandler(this._btnLast_Click);

            // PaginationBar
            this.Dock = DockStyle.Bottom;
            this.Height = 36;
            this.WrapContents = false;
            this.FlowDirection = FlowDirection.LeftToRight;
            this.Padding = new Padding(8, 5, 8, 5);
            this.Controls.Add(this._cmbSize);
            this.Controls.Add(this._btnFirst);
            this.Controls.Add(this._btnPrev);
            this.Controls.Add(this._txtPage);
            this.Controls.Add(this._btnGo);
            this.Controls.Add(this._lblTotal);
            this.Controls.Add(this._btnNext);
            this.Controls.Add(this._btnLast);
            this.Name = "PaginationBar";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
