namespace QMC.CDT_320.Ui.Dialogs
{
    partial class VisionModuleTestDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel _root;
        private System.Windows.Forms.TableLayoutPanel _grid;
        private System.Windows.Forms.Button _btnGrab;
        private System.Windows.Forms.Label _lblGrab;
        private System.Windows.Forms.TextBox _txtFinder;
        private System.Windows.Forms.Button _btnMatch;
        private System.Windows.Forms.Label _lblMatch;
        private System.Windows.Forms.TextBox _txtInsp;
        private System.Windows.Forms.Button _btnInspect;
        private System.Windows.Forms.Label _lblInsp;
        private System.Windows.Forms.Label _hint;
        private QMC.CDT_320.Ui.Controls.VisionViewerPanel _viewer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._root = new System.Windows.Forms.TableLayoutPanel();
            this._grid = new System.Windows.Forms.TableLayoutPanel();
            this._btnGrab = new System.Windows.Forms.Button();
            this._lblGrab = new System.Windows.Forms.Label();
            this._txtFinder = new System.Windows.Forms.TextBox();
            this._btnMatch = new System.Windows.Forms.Button();
            this._lblMatch = new System.Windows.Forms.Label();
            this._txtInsp = new System.Windows.Forms.TextBox();
            this._btnInspect = new System.Windows.Forms.Button();
            this._lblInsp = new System.Windows.Forms.Label();
            this._hint = new System.Windows.Forms.Label();
            this._viewer = new QMC.CDT_320.Ui.Controls.VisionViewerPanel();
            this._root.SuspendLayout();
            this._grid.SuspendLayout();
            this.SuspendLayout();
            // 
            // _root
            // 
            this._root.ColumnCount = 2;
            this._root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 470F));
            this._root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.Controls.Add(this._grid, 0, 0);
            this._root.Controls.Add(this._viewer, 1, 0);
            this._root.Dock = System.Windows.Forms.DockStyle.Fill;
            this._root.Location = new System.Drawing.Point(0, 0);
            this._root.Name = "_root";
            this._root.RowCount = 1;
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.Size = new System.Drawing.Size(1080, 420);
            this._root.TabIndex = 0;
            // 
            // _grid
            // 
            this._grid.ColumnCount = 3;
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._grid.Controls.Add(this._btnGrab, 0, 0);
            this._grid.Controls.Add(this._lblGrab, 1, 0);
            this._grid.Controls.Add(this._txtFinder, 0, 1);
            this._grid.Controls.Add(this._btnMatch, 1, 1);
            this._grid.Controls.Add(this._lblMatch, 2, 1);
            this._grid.Controls.Add(this._txtInsp, 0, 2);
            this._grid.Controls.Add(this._btnInspect, 1, 2);
            this._grid.Controls.Add(this._lblInsp, 2, 2);
            this._grid.Controls.Add(this._hint, 0, 3);
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.Location = new System.Drawing.Point(3, 3);
            this._grid.Name = "_grid";
            this._grid.Padding = new System.Windows.Forms.Padding(12);
            this._grid.RowCount = 4;
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._grid.Size = new System.Drawing.Size(464, 414);
            this._grid.TabIndex = 0;
            // 
            // _btnGrab
            // 
            this._btnGrab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._btnGrab.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnGrab.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnGrab.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._btnGrab.ForeColor = System.Drawing.Color.White;
            this._btnGrab.Location = new System.Drawing.Point(15, 18);
            this._btnGrab.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this._btnGrab.Name = "_btnGrab";
            this._btnGrab.Size = new System.Drawing.Size(165, 36);
            this._btnGrab.TabIndex = 0;
            this._btnGrab.Text = "GRAB";
            this._btnGrab.UseVisualStyleBackColor = false;
            this._btnGrab.Click += new System.EventHandler(this.btnGrab_Click);
            // 
            // _lblGrab
            // 
            this._grid.SetColumnSpan(this._lblGrab, 2);
            this._lblGrab.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblGrab.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblGrab.ForeColor = System.Drawing.Color.DimGray;
            this._lblGrab.Location = new System.Drawing.Point(195, 12);
            this._lblGrab.Name = "_lblGrab";
            this._lblGrab.Size = new System.Drawing.Size(254, 48);
            this._lblGrab.TabIndex = 1;
            this._lblGrab.Text = "대기";
            this._lblGrab.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtFinder
            // 
            this._txtFinder.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtFinder.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._txtFinder.Location = new System.Drawing.Point(15, 68);
            this._txtFinder.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this._txtFinder.Name = "_txtFinder";
            this._txtFinder.Size = new System.Drawing.Size(174, 25);
            this._txtFinder.TabIndex = 2;
            // 
            // _btnMatch
            // 
            this._btnMatch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._btnMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMatch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMatch.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._btnMatch.ForeColor = System.Drawing.Color.White;
            this._btnMatch.Location = new System.Drawing.Point(195, 66);
            this._btnMatch.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this._btnMatch.Name = "_btnMatch";
            this._btnMatch.Size = new System.Drawing.Size(115, 36);
            this._btnMatch.TabIndex = 3;
            this._btnMatch.Text = "MATCH";
            this._btnMatch.UseVisualStyleBackColor = false;
            this._btnMatch.Click += new System.EventHandler(this.btnMatch_Click);
            // 
            // _lblMatch
            // 
            this._lblMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblMatch.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblMatch.ForeColor = System.Drawing.Color.DimGray;
            this._lblMatch.Location = new System.Drawing.Point(325, 60);
            this._lblMatch.Name = "_lblMatch";
            this._lblMatch.Size = new System.Drawing.Size(124, 48);
            this._lblMatch.TabIndex = 4;
            this._lblMatch.Text = "finder 입력 후 MATCH - x/y/t/score";
            this._lblMatch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtInsp
            // 
            this._txtInsp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtInsp.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._txtInsp.Location = new System.Drawing.Point(15, 116);
            this._txtInsp.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this._txtInsp.Name = "_txtInsp";
            this._txtInsp.Size = new System.Drawing.Size(174, 25);
            this._txtInsp.TabIndex = 5;
            // 
            // _btnInspect
            // 
            this._btnInspect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._btnInspect.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnInspect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnInspect.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._btnInspect.ForeColor = System.Drawing.Color.White;
            this._btnInspect.Location = new System.Drawing.Point(195, 114);
            this._btnInspect.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this._btnInspect.Name = "_btnInspect";
            this._btnInspect.Size = new System.Drawing.Size(115, 36);
            this._btnInspect.TabIndex = 6;
            this._btnInspect.Text = "INSPECT";
            this._btnInspect.UseVisualStyleBackColor = false;
            this._btnInspect.Click += new System.EventHandler(this.btnInspect_Click);
            // 
            // _lblInsp
            // 
            this._lblInsp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblInsp.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblInsp.ForeColor = System.Drawing.Color.DimGray;
            this._lblInsp.Location = new System.Drawing.Point(325, 108);
            this._lblInsp.Name = "_lblInsp";
            this._lblInsp.Size = new System.Drawing.Size(124, 48);
            this._lblInsp.TabIndex = 7;
            this._lblInsp.Text = "inspector 입력 후 INSPECT - PASS/FAIL";
            this._lblInsp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _hint
            // 
            this._grid.SetColumnSpan(this._hint, 3);
            this._hint.Dock = System.Windows.Forms.DockStyle.Fill;
            this._hint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._hint.ForeColor = System.Drawing.Color.DimGray;
            this._hint.Location = new System.Drawing.Point(15, 156);
            this._hint.Name = "_hint";
            this._hint.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this._hint.Size = new System.Drawing.Size(434, 246);
            this._hint.TabIndex = 8;
            this._hint.Text = "Vision RUN 상태에서만 명령을 수락합니다. finder/inspector는 레시피에 등록된 도구명입니다.";
            // 
            // _viewer
            // 
            this._viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._viewer.Location = new System.Drawing.Point(470, 0);
            this._viewer.Margin = new System.Windows.Forms.Padding(0);
            this._viewer.Name = "_viewer";
            this._viewer.Size = new System.Drawing.Size(610, 420);
            this._viewer.TabIndex = 1;
            // 
            // VisionModuleTestDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1080, 420);
            this.Controls.Add(this._root);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 360);
            this.Name = "VisionModuleTestDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "VISION 동작 테스트";
            this._root.ResumeLayout(false);
            this._grid.ResumeLayout(false);
            this._grid.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
