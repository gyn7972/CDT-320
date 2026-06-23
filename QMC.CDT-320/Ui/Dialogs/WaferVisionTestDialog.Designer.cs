namespace QMC.CDT_320.Ui.Dialogs
{
    partial class WaferVisionTestDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel _root;
        private System.Windows.Forms.TableLayoutPanel _grid;
        private System.Windows.Forms.Button _btnExpose;
        private System.Windows.Forms.Button _btnCenter;
        private System.Windows.Forms.Button _btnRef1;
        private System.Windows.Forms.Button _btnRef2;
        private System.Windows.Forms.Button _btnDieCheck;
        private System.Windows.Forms.Label _lblExpose;
        private System.Windows.Forms.Label _lblCenter;
        private System.Windows.Forms.Label _lblRef1;
        private System.Windows.Forms.Label _lblRef2;
        private System.Windows.Forms.Label _lblDieCheck;
        private System.Windows.Forms.Label _lblSummary;
        private System.Windows.Forms.Label _lblHint;
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
            this._btnExpose = new System.Windows.Forms.Button();
            this._btnCenter = new System.Windows.Forms.Button();
            this._btnRef1 = new System.Windows.Forms.Button();
            this._btnRef2 = new System.Windows.Forms.Button();
            this._btnDieCheck = new System.Windows.Forms.Button();
            this._lblExpose = new System.Windows.Forms.Label();
            this._lblCenter = new System.Windows.Forms.Label();
            this._lblRef1 = new System.Windows.Forms.Label();
            this._lblRef2 = new System.Windows.Forms.Label();
            this._lblDieCheck = new System.Windows.Forms.Label();
            this._lblSummary = new System.Windows.Forms.Label();
            this._lblHint = new System.Windows.Forms.Label();
            this._viewer = new QMC.CDT_320.Ui.Controls.VisionViewerPanel();
            this._root.SuspendLayout();
            this._grid.SuspendLayout();
            this.SuspendLayout();
            //
            // _root
            //
            this._root.ColumnCount = 2;
            this._root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 450F));
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
            this._grid.ColumnCount = 2;
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._grid.RowCount = 7;
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.Padding = new System.Windows.Forms.Padding(12);
            this._grid.Name = "_grid";
            this._grid.Controls.Add(this._btnExpose, 0, 0);
            this._grid.Controls.Add(this._lblExpose, 1, 0);
            this._grid.Controls.Add(this._btnCenter, 0, 1);
            this._grid.Controls.Add(this._lblCenter, 1, 1);
            this._grid.Controls.Add(this._btnRef1, 0, 2);
            this._grid.Controls.Add(this._lblRef1, 1, 2);
            this._grid.Controls.Add(this._btnRef2, 0, 3);
            this._grid.Controls.Add(this._lblRef2, 1, 3);
            this._grid.Controls.Add(this._btnDieCheck, 0, 4);
            this._grid.Controls.Add(this._lblDieCheck, 1, 4);
            this._grid.Controls.Add(this._lblSummary, 0, 5);
            this._grid.SetColumnSpan(this._lblSummary, 2);
            this._grid.Controls.Add(this._lblHint, 0, 6);
            this._grid.SetColumnSpan(this._lblHint, 2);
            //
            // command buttons
            //
            ConfigCmdBtn(this._btnExpose,   "① GRAB (EXPOSE)");
            ConfigCmdBtn(this._btnCenter,   "② ALIGN: Center");
            ConfigCmdBtn(this._btnRef1,     "③ ALIGN: Ref1");
            ConfigCmdBtn(this._btnRef2,     "④ ALIGN: Ref2");
            ConfigCmdBtn(this._btnDieCheck, "⑤ DIE CHECK");
            //
            // result labels
            //
            ConfigResultLbl(this._lblExpose,   "노출 트리거 → ACK 대기");
            ConfigResultLbl(this._lblCenter,   "센터 마크 정렬 → dx/dy/θ/pitch");
            ConfigResultLbl(this._lblRef1,     "기준1 마크 정렬 → dx/dy/θ/pitch");
            ConfigResultLbl(this._lblRef2,     "기준2 마크 정렬 → dx/dy/θ/pitch");
            ConfigResultLbl(this._lblDieCheck, "다이 검출 → OK/NG");
            //
            // _lblSummary
            //
            this._lblSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblSummary.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            this._lblSummary.ForeColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this._lblSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._lblSummary.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblSummary.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblSummary.Name = "_lblSummary";
            this._lblSummary.Text = "저장된 결과 없음";
            //
            // _lblHint
            //
            this._lblHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblHint.ForeColor = System.Drawing.Color.DimGray;
            this._lblHint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._lblHint.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this._lblHint.Name = "_lblHint";
            this._lblHint.Text = "버튼은 시퀀서와 동일한 Wafer 어댑터를 호출합니다(모션 구동 없음).\r\nVision은 촬상·검출 결과만 반환. 결과는 자동으로 결과 저장소에 기록됩니다.";
            //
            // _viewer
            //
            this._viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._viewer.Margin = new System.Windows.Forms.Padding(0);
            this._viewer.Name = "_viewer";
            //
            // WaferVisionTestDialog
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1080, 600);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 500);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Name = "WaferVisionTestDialog";
            this.Text = "VISION 동작 테스트 — Wafer Vision";
            this.Controls.Add(this._root);
            this._root.ResumeLayout(false);
            this._grid.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        // 디자이너 내부 스타일 헬퍼(레이아웃 전용) — 동일 스타일 반복 적용.
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

        private static void ConfigResultLbl(System.Windows.Forms.Label l, string placeholder)
        {
            l.Dock = System.Windows.Forms.DockStyle.Fill;
            l.Text = placeholder;
            l.Font = new System.Drawing.Font("Consolas", 10F);
            l.ForeColor = System.Drawing.Color.DimGray;
            l.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        }
    }
}
