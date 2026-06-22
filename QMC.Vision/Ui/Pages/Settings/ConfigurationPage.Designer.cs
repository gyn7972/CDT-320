using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class ConfigurationPage
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel rootLayout;
        private Label            _hdr;
        private TableLayoutPanel bodyLayout;
        private TableLayoutPanel _toolbar;
        private Button           _btnLoadAll;
        private Button           _btnSaveAll;

        // 섹션 타이틀(주황 밑줄) + 섹션별 그리드
        private Label _t1; private Panel _ln1; private QMC.Vision.Ui.Controls.ParameterGridControl _g1;
        private Label _t2; private Panel _ln2; private QMC.Vision.Ui.Controls.ParameterGridControl _g2;
        private Label _t3; private Panel _ln3; private QMC.Vision.Ui.Controls.ParameterGridControl _g3;
        private Label _t6; private Panel _ln6; private QMC.Vision.Ui.Controls.ParameterGridControl _g6;
        private Label _t4; private Panel _ln4; private QMC.Vision.Ui.Controls.ParameterGridControl _g4;
        private Label _t5; private Panel _ln5; private QMC.Vision.Ui.Controls.ParameterGridControl _g5;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this._hdr = new System.Windows.Forms.Label();
            this.bodyLayout = new System.Windows.Forms.TableLayoutPanel();
            this._t1 = new System.Windows.Forms.Label();
            this._ln1 = new System.Windows.Forms.Panel();
            this._g1 = new QMC.Vision.Ui.Controls.ParameterGridControl();
            this._t2 = new System.Windows.Forms.Label();
            this._ln2 = new System.Windows.Forms.Panel();
            this._g2 = new QMC.Vision.Ui.Controls.ParameterGridControl();
            this._t3 = new System.Windows.Forms.Label();
            this._ln3 = new System.Windows.Forms.Panel();
            this._g3 = new QMC.Vision.Ui.Controls.ParameterGridControl();
            this._t6 = new System.Windows.Forms.Label();
            this._ln6 = new System.Windows.Forms.Panel();
            this._g6 = new QMC.Vision.Ui.Controls.ParameterGridControl();
            this._t4 = new System.Windows.Forms.Label();
            this._ln4 = new System.Windows.Forms.Panel();
            this._g4 = new QMC.Vision.Ui.Controls.ParameterGridControl();
            this._t5 = new System.Windows.Forms.Label();
            this._ln5 = new System.Windows.Forms.Panel();
            this._g5 = new QMC.Vision.Ui.Controls.ParameterGridControl();
            this._toolbar = new System.Windows.Forms.TableLayoutPanel();
            this._btnLoadAll = new System.Windows.Forms.Button();
            this._btnSaveAll = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.bodyLayout.SuspendLayout();
            this._t1.SuspendLayout();
            this._t2.SuspendLayout();
            this._t3.SuspendLayout();
            this._t6.SuspendLayout();
            this._t4.SuspendLayout();
            this._t5.SuspendLayout();
            this._toolbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this._hdr, 0, 0);
            this.rootLayout.Controls.Add(this.bodyLayout, 0, 1);
            this.rootLayout.Controls.Add(this._toolbar, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.rootLayout.Size = new System.Drawing.Size(1100, 760);
            this.rootLayout.TabIndex = 0;
            // 
            // _hdr
            // 
            this._hdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._hdr.Dock = System.Windows.Forms.DockStyle.Fill;
            this._hdr.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._hdr.ForeColor = System.Drawing.Color.White;
            this._hdr.Location = new System.Drawing.Point(12, 8);
            this._hdr.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this._hdr.Name = "_hdr";
            this._hdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._hdr.Size = new System.Drawing.Size(1076, 32);
            this._hdr.TabIndex = 0;
            this._hdr.Tag = "i18n:set.general";
            this._hdr.Text = "GENERAL";
            this._hdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // bodyLayout
            // 
            this.bodyLayout.ColumnCount = 1;
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bodyLayout.Controls.Add(this._t1, 0, 0);
            this.bodyLayout.Controls.Add(this._g1, 0, 1);
            this.bodyLayout.Controls.Add(this._t2, 0, 2);
            this.bodyLayout.Controls.Add(this._g2, 0, 3);
            this.bodyLayout.Controls.Add(this._t3, 0, 4);
            this.bodyLayout.Controls.Add(this._g3, 0, 5);
            this.bodyLayout.Controls.Add(this._t6, 0, 6);
            this.bodyLayout.Controls.Add(this._g6, 0, 7);
            this.bodyLayout.Controls.Add(this._t4, 0, 8);
            this.bodyLayout.Controls.Add(this._g4, 0, 9);
            this.bodyLayout.Controls.Add(this._t5, 0, 10);
            this.bodyLayout.Controls.Add(this._g5, 0, 11);
            this.bodyLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bodyLayout.Location = new System.Drawing.Point(12, 46);
            this.bodyLayout.Margin = new System.Windows.Forms.Padding(0);
            this.bodyLayout.Name = "bodyLayout";
            this.bodyLayout.RowCount = 13;
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 78F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 78F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 78F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bodyLayout.Size = new System.Drawing.Size(1076, 654);
            this.bodyLayout.TabIndex = 1;
            // 
            // _t1
            // 
            this._t1.Controls.Add(this._ln1);
            this._t1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._t1.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._t1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this._t1.Location = new System.Drawing.Point(0, 6);
            this._t1.Margin = new System.Windows.Forms.Padding(0, 6, 0, 1);
            this._t1.Name = "_t1";
            this._t1.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._t1.Size = new System.Drawing.Size(1076, 21);
            this._t1.TabIndex = 0;
            this._t1.Tag = "i18n:set.gen.secLang";
            this._t1.Text = "언어 / 실행 모드";
            this._t1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ln1
            // 
            this._ln1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._ln1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._ln1.Location = new System.Drawing.Point(0, 19);
            this._ln1.Name = "_ln1";
            this._ln1.Size = new System.Drawing.Size(1076, 2);
            this._ln1.TabIndex = 0;
            // 
            // _g1
            // 
            this._g1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this._g1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._g1.Location = new System.Drawing.Point(0, 29);
            this._g1.Margin = new System.Windows.Forms.Padding(0, 1, 0, 4);
            this._g1.Name = "_g1";
            this._g1.Size = new System.Drawing.Size(1076, 73);
            this._g1.TabIndex = 1;
            // 
            // _t2
            // 
            this._t2.Controls.Add(this._ln2);
            this._t2.Dock = System.Windows.Forms.DockStyle.Fill;
            this._t2.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._t2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this._t2.Location = new System.Drawing.Point(0, 112);
            this._t2.Margin = new System.Windows.Forms.Padding(0, 6, 0, 1);
            this._t2.Name = "_t2";
            this._t2.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._t2.Size = new System.Drawing.Size(1076, 21);
            this._t2.TabIndex = 2;
            this._t2.Tag = "i18n:set.gen.backend";
            this._t2.Text = "Vision Backend (재시작 후 반영)";
            this._t2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ln2
            // 
            this._ln2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._ln2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._ln2.Location = new System.Drawing.Point(0, 19);
            this._ln2.Name = "_ln2";
            this._ln2.Size = new System.Drawing.Size(1076, 2);
            this._ln2.TabIndex = 0;
            // 
            // _g2
            // 
            this._g2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this._g2.Dock = System.Windows.Forms.DockStyle.Fill;
            this._g2.Location = new System.Drawing.Point(0, 135);
            this._g2.Margin = new System.Windows.Forms.Padding(0, 1, 0, 4);
            this._g2.Name = "_g2";
            this._g2.Size = new System.Drawing.Size(1076, 73);
            this._g2.TabIndex = 3;
            // 
            // _t3
            // 
            this._t3.Controls.Add(this._ln3);
            this._t3.Dock = System.Windows.Forms.DockStyle.Fill;
            this._t3.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._t3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this._t3.Location = new System.Drawing.Point(0, 218);
            this._t3.Margin = new System.Windows.Forms.Padding(0, 6, 0, 1);
            this._t3.Name = "_t3";
            this._t3.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._t3.Size = new System.Drawing.Size(1076, 21);
            this._t3.TabIndex = 4;
            this._t3.Tag = "i18n:set.gen.cgxResult";
            this._t3.Text = "Cognex 진단";
            this._t3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ln3
            // 
            this._ln3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._ln3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._ln3.Location = new System.Drawing.Point(0, 19);
            this._ln3.Name = "_ln3";
            this._ln3.Size = new System.Drawing.Size(1076, 2);
            this._ln3.TabIndex = 0;
            // 
            // _g3
            // 
            this._g3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this._g3.Dock = System.Windows.Forms.DockStyle.Fill;
            this._g3.Location = new System.Drawing.Point(0, 241);
            this._g3.Margin = new System.Windows.Forms.Padding(0, 1, 0, 4);
            this._g3.Name = "_g3";
            this._g3.Size = new System.Drawing.Size(1076, 99);
            this._g3.TabIndex = 5;
            //
            // _t6
            //
            this._t6.Controls.Add(this._ln6);
            this._t6.Dock = System.Windows.Forms.DockStyle.Fill;
            this._t6.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._t6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this._t6.Location = new System.Drawing.Point(0, 344);
            this._t6.Margin = new System.Windows.Forms.Padding(0, 6, 0, 1);
            this._t6.Name = "_t6";
            this._t6.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._t6.Size = new System.Drawing.Size(1076, 21);
            this._t6.TabIndex = 10;
            this._t6.Tag = "i18n:set.gen.ocvResult";
            this._t6.Text = "OpenCV 진단";
            this._t6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _ln6
            //
            this._ln6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._ln6.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._ln6.Location = new System.Drawing.Point(0, 19);
            this._ln6.Name = "_ln6";
            this._ln6.Size = new System.Drawing.Size(1076, 2);
            this._ln6.TabIndex = 0;
            //
            // _g6
            //
            this._g6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this._g6.Dock = System.Windows.Forms.DockStyle.Fill;
            this._g6.Location = new System.Drawing.Point(0, 367);
            this._g6.Margin = new System.Windows.Forms.Padding(0, 1, 0, 4);
            this._g6.Name = "_g6";
            this._g6.Size = new System.Drawing.Size(1076, 99);
            this._g6.TabIndex = 11;
            //
            // _t4
            //
            this._t4.Controls.Add(this._ln4);
            this._t4.Dock = System.Windows.Forms.DockStyle.Fill;
            this._t4.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._t4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this._t4.Location = new System.Drawing.Point(0, 350);
            this._t4.Margin = new System.Windows.Forms.Padding(0, 6, 0, 1);
            this._t4.Name = "_t4";
            this._t4.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._t4.Size = new System.Drawing.Size(1076, 21);
            this._t4.TabIndex = 6;
            this._t4.Tag = "i18n:set.gen.secImg";
            this._t4.Text = "이미지 로그";
            this._t4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ln4
            // 
            this._ln4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._ln4.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._ln4.Location = new System.Drawing.Point(0, 19);
            this._ln4.Name = "_ln4";
            this._ln4.Size = new System.Drawing.Size(1076, 2);
            this._ln4.TabIndex = 0;
            // 
            // _g4
            // 
            this._g4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this._g4.Dock = System.Windows.Forms.DockStyle.Fill;
            this._g4.Location = new System.Drawing.Point(0, 373);
            this._g4.Margin = new System.Windows.Forms.Padding(0, 1, 0, 4);
            this._g4.Name = "_g4";
            this._g4.Size = new System.Drawing.Size(1076, 73);
            this._g4.TabIndex = 7;
            // 
            // _t5
            // 
            this._t5.Controls.Add(this._ln5);
            this._t5.Dock = System.Windows.Forms.DockStyle.Fill;
            this._t5.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._t5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this._t5.Location = new System.Drawing.Point(0, 456);
            this._t5.Margin = new System.Windows.Forms.Padding(0, 6, 0, 1);
            this._t5.Name = "_t5";
            this._t5.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._t5.Size = new System.Drawing.Size(1076, 21);
            this._t5.TabIndex = 8;
            this._t5.Tag = "i18n:set.gen.secData";
            this._t5.Text = "데이터 저장 경로 (재시작 후 반영)";
            this._t5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ln5
            // 
            this._ln5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._ln5.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._ln5.Location = new System.Drawing.Point(0, 19);
            this._ln5.Name = "_ln5";
            this._ln5.Size = new System.Drawing.Size(1076, 2);
            this._ln5.TabIndex = 0;
            // 
            // _g5
            // 
            this._g5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this._g5.Dock = System.Windows.Forms.DockStyle.Fill;
            this._g5.Location = new System.Drawing.Point(0, 479);
            this._g5.Margin = new System.Windows.Forms.Padding(0, 1, 0, 4);
            this._g5.Name = "_g5";
            this._g5.Size = new System.Drawing.Size(1076, 51);
            this._g5.TabIndex = 9;
            //
            // _toolbar
            //
            this._toolbar.ColumnCount = 3;
            this._toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this._toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this._toolbar.Controls.Add(this._btnLoadAll, 1, 0);
            this._toolbar.Controls.Add(this._btnSaveAll, 2, 0);
            this._toolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            this._toolbar.Location = new System.Drawing.Point(15, 703);
            this._toolbar.Name = "_toolbar";
            this._toolbar.Padding = new System.Windows.Forms.Padding(0, 0, 0, 0);
            this._toolbar.RowCount = 1;
            this._toolbar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._toolbar.Size = new System.Drawing.Size(1070, 46);
            this._toolbar.TabIndex = 2;
            // 
            // _btnLoadAll
            // 
            this._btnLoadAll.BackColor = System.Drawing.Color.White;
            this._btnLoadAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnLoadAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnLoadAll.Font = new System.Drawing.Font("맑은 고딕", 10.5F);
            this._btnLoadAll.Location = new System.Drawing.Point(817, 13);
            this._btnLoadAll.Name = "_btnLoadAll";
            this._btnLoadAll.Size = new System.Drawing.Size(122, 30);
            this._btnLoadAll.TabIndex = 0;
            this._btnLoadAll.Tag = "i18n:common.load";
            this._btnLoadAll.Text = "불러오기";
            this._btnLoadAll.UseVisualStyleBackColor = false;
            this._btnLoadAll.Click += new System.EventHandler(this.OnLoadAllClick);
            // 
            // _btnSaveAll
            // 
            this._btnSaveAll.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnSaveAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSaveAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSaveAll.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._btnSaveAll.ForeColor = System.Drawing.Color.White;
            this._btnSaveAll.Location = new System.Drawing.Point(945, 13);
            this._btnSaveAll.Name = "_btnSaveAll";
            this._btnSaveAll.Size = new System.Drawing.Size(122, 30);
            this._btnSaveAll.TabIndex = 1;
            this._btnSaveAll.Tag = "i18n:common.save";
            this._btnSaveAll.Text = "저장";
            this._btnSaveAll.UseVisualStyleBackColor = false;
            this._btnSaveAll.Click += new System.EventHandler(this.OnSaveAllClick);
            // 
            // ConfigurationPage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "ConfigurationPage";
            this.Size = new System.Drawing.Size(1100, 760);
            this.rootLayout.ResumeLayout(false);
            this.bodyLayout.ResumeLayout(false);
            this._t1.ResumeLayout(false);
            this._t2.ResumeLayout(false);
            this._t3.ResumeLayout(false);
            this._t6.ResumeLayout(false);
            this._t4.ResumeLayout(false);
            this._t5.ResumeLayout(false);
            this._toolbar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
