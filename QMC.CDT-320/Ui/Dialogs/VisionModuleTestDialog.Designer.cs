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
            this._root.RowCount = 1;
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.Dock = System.Windows.Forms.DockStyle.Fill;
            this._root.Name = "_root";
            this._root.Controls.Add(this._grid, 0, 0);
            this._root.Controls.Add(this._viewer, 1, 0);
            //
            // _grid
            //
            this._grid.ColumnCount = 3;
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._grid.RowCount = 4;
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.Padding = new System.Windows.Forms.Padding(12);
            this._grid.Name = "_grid";
            this._grid.Controls.Add(this._btnGrab, 0, 0);
            this._grid.Controls.Add(this._lblGrab, 1, 0);
            this._grid.SetColumnSpan(this._lblGrab, 2);
            this._grid.Controls.Add(this._txtFinder, 0, 1);
            this._grid.Controls.Add(this._btnMatch, 1, 1);
            this._grid.Controls.Add(this._lblMatch, 2, 1);
            this._grid.Controls.Add(this._txtInsp, 0, 2);
            this._grid.Controls.Add(this._btnInspect, 1, 2);
            this._grid.Controls.Add(this._lblInsp, 2, 2);
            this._grid.Controls.Add(this._hint, 0, 3);
            this._grid.SetColumnSpan(this._hint, 3);
            //
            // command buttons
            //
            ConfigCmdBtn(this._btnGrab,    "GRAB");
            ConfigCmdBtn(this._btnMatch,   "MATCH");
            ConfigCmdBtn(this._btnInspect, "INSPECT");
            //
            // textboxes
            //
            this._txtFinder.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtFinder.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._txtFinder.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this._txtFinder.Name = "_txtFinder";
            this._txtInsp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtInsp.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._txtInsp.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this._txtInsp.Name = "_txtInsp";
            //
            // result labels
            //
            ConfigResultLbl(this._lblGrab,  "대기");
            ConfigResultLbl(this._lblMatch, "finder 입력 후 MATCH → x/y/θ/score");
            ConfigResultLbl(this._lblInsp,  "inspector 입력 후 INSPECT → PASS/FAIL");
            //
            // _hint
            //
            this._hint.Dock = System.Windows.Forms.DockStyle.Fill;
            this._hint.ForeColor = System.Drawing.Color.DimGray;
            this._hint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._hint.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this._hint.Name = "_hint";
            this._hint.Text = "Vision RUN 상태에서만 명령 수락(PING 제외). finder/inspector 는 레시피 등록 도구명.";
            //
            // _viewer
            //
            this._viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._viewer.Margin = new System.Windows.Forms.Padding(0);
            this._viewer.Name = "_viewer";
            //
            // VisionModuleTestDialog
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1080, 420);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 360);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Name = "VisionModuleTestDialog";
            this.Text = "VISION 동작 테스트";
            this.Controls.Add(this._root);
            this._root.ResumeLayout(false);
            this._grid.ResumeLayout(false);
            this._grid.PerformLayout();
            this.ResumeLayout(false);
        }

        private static void ConfigCmdBtn(System.Windows.Forms.Button b, string text)
        {
            b.Text = text;
            b.Dock = System.Windows.Forms.DockStyle.Fill;
            b.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            b.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            b.ForeColor = System.Drawing.Color.White;
            b.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            b.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            b.UseVisualStyleBackColor = false;
        }

        private static void ConfigResultLbl(System.Windows.Forms.Label l, string text)
        {
            l.Dock = System.Windows.Forms.DockStyle.Fill;
            l.Text = text;
            l.Font = new System.Drawing.Font("Consolas", 10F);
            l.ForeColor = System.Drawing.Color.DimGray;
            l.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        }
    }
}
