namespace QMC.CDT_320.Ui.Dialogs
{
    partial class BinSelectDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.RadioButton rdoAll;
        private System.Windows.Forms.RadioButton rdoSelected;
        private System.Windows.Forms.ListView lsvBins;
        private System.Windows.Forms.ColumnHeader colBin;
        private System.Windows.Forms.ColumnHeader colDieCount;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.Label lblGuide;
        private System.Windows.Forms.FlowLayoutPanel buttonPanel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.rdoAll = new System.Windows.Forms.RadioButton();
            this.rdoSelected = new System.Windows.Forms.RadioButton();
            this.lsvBins = new System.Windows.Forms.ListView();
            this.colBin = new System.Windows.Forms.ColumnHeader();
            this.colDieCount = new System.Windows.Forms.ColumnHeader();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.lblGuide = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.rdoAll, 0, 1);
            this.rootLayout.Controls.Add(this.rdoSelected, 0, 2);
            this.rootLayout.Controls.Add(this.lsvBins, 0, 3);
            this.rootLayout.Controls.Add(this.lblGuide, 0, 4);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(10);
            this.rootLayout.RowCount = 5;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            //
            // lblHeader
            //
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // rdoAll
            //
            this.rdoAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoAll.Name = "rdoAll";
            this.rdoAll.Text = "ALL — 맵의 모든 다이를 픽업 (기본)";
            this.rdoAll.CheckedChanged += new System.EventHandler(this.rdoMode_CheckedChanged);
            //
            // rdoSelected
            //
            this.rdoSelected.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoSelected.Name = "rdoSelected";
            this.rdoSelected.Text = "지정 BIN만 픽업 — 아래에서 선택";
            this.rdoSelected.CheckedChanged += new System.EventHandler(this.rdoMode_CheckedChanged);
            //
            // lsvBins
            //
            this.lsvBins.CheckBoxes = true;
            this.lsvBins.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colBin,
            this.colDieCount,
            this.colName});
            this.lsvBins.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lsvBins.FullRowSelect = true;
            this.lsvBins.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lsvBins.Name = "lsvBins";
            this.lsvBins.View = System.Windows.Forms.View.Details;
            //
            // colBin
            //
            this.colBin.Text = "BIN";
            this.colBin.Width = 70;
            //
            // colDieCount
            //
            this.colDieCount.Text = "다이 수";
            this.colDieCount.Width = 90;
            //
            // colName
            //
            this.colName.Text = "이름";
            this.colName.Width = 300;
            //
            // lblGuide
            //
            this.lblGuide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGuide.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(63)))), ((int)(((byte)(4)))));
            this.lblGuide.Name = "lblGuide";
            this.lblGuide.Text = "· 선택 변경은 다음 웨이퍼(맵 적용 시점)부터 반영됩니다.\r\n· LOT 완료/레시피 변경 시 자동으로 ALL로 복귀합니다.\r\n· 색은 bin 색상 사전(Config\\bin_codes.json) 기준 표시입니다.";
            this.lblGuide.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // buttonPanel
            //
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Controls.Add(this.btnOk);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonPanel.Height = 42;
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 6, 10, 0);
            //
            // btnOk
            //
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(96, 30);
            this.btnOk.Text = "확인";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            //
            // btnCancel
            //
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(96, 30);
            this.btnCancel.Text = "취소";
            this.btnCancel.UseVisualStyleBackColor = true;
            //
            // BinSelectDialog
            //
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(520, 480);
            this.Controls.Add(this.rootLayout);
            this.Controls.Add(this.buttonPanel);
            this.Font = new System.Drawing.Font("맑은 고딕", 9.5F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BinSelectDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "픽업 BIN 선택";
            this.rootLayout.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
