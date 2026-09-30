namespace QMC.CDT_320.Ui.Common
{
    partial class UiPageTemplate
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel tableRoot;
        private System.Windows.Forms.Label lblPageTitle;
        private System.Windows.Forms.TableLayoutPanel tableContent;
        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.Label lblLeftContent;
        private System.Windows.Forms.Label lblLeftTitle;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Label lblMainContent;
        private System.Windows.Forms.Label lblMainTitle;
        private System.Windows.Forms.TableLayoutPanel tableCommands;
        private System.Windows.Forms.Label lblCommandHint;
        private QMC.Common.Ui.Controls.UiStandardButton btnDefaultSample;
        private QMC.Common.Ui.Controls.UiStandardButton btnPrimarySample;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.tableRoot = new System.Windows.Forms.TableLayoutPanel();
            this.lblPageTitle = new System.Windows.Forms.Label();
            this.tableContent = new System.Windows.Forms.TableLayoutPanel();
            this.panelLeft = new System.Windows.Forms.Panel();
            this.lblLeftContent = new System.Windows.Forms.Label();
            this.lblLeftTitle = new System.Windows.Forms.Label();
            this.panelMain = new System.Windows.Forms.Panel();
            this.lblMainContent = new System.Windows.Forms.Label();
            this.lblMainTitle = new System.Windows.Forms.Label();
            this.tableCommands = new System.Windows.Forms.TableLayoutPanel();
            this.lblCommandHint = new System.Windows.Forms.Label();
            this.btnDefaultSample = new QMC.Common.Ui.Controls.UiStandardButton();
            this.btnPrimarySample = new QMC.Common.Ui.Controls.UiStandardButton();
            this.tableRoot.SuspendLayout();
            this.tableContent.SuspendLayout();
            this.panelLeft.SuspendLayout();
            this.panelMain.SuspendLayout();
            this.tableCommands.SuspendLayout();
            this.SuspendLayout();
            //
            // tableRoot
            //
            this.tableRoot.ColumnCount = 1;
            this.tableRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableRoot.Controls.Add(this.lblPageTitle, 0, 0);
            this.tableRoot.Controls.Add(this.tableContent, 0, 1);
            this.tableRoot.Controls.Add(this.tableCommands, 0, 2);
            this.tableRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableRoot.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tableRoot.Location = new System.Drawing.Point(0, 0);
            this.tableRoot.Margin = new System.Windows.Forms.Padding(0);
            this.tableRoot.Name = "tableRoot";
            this.tableRoot.Padding = new System.Windows.Forms.Padding(12);
            this.tableRoot.RowCount = 3;
            this.tableRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.tableRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.tableRoot.Size = new System.Drawing.Size(1000, 640);
            this.tableRoot.TabIndex = 0;
            //
            // lblPageTitle
            //
            this.lblPageTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPageTitle.Font = new System.Drawing.Font("맑은 고딕", 18F, System.Drawing.FontStyle.Bold);
            this.lblPageTitle.ForeColor = System.Drawing.Color.Black;
            this.lblPageTitle.Location = new System.Drawing.Point(16, 16);
            this.lblPageTitle.Margin = new System.Windows.Forms.Padding(4);
            this.lblPageTitle.Name = "lblPageTitle";
            this.lblPageTitle.Size = new System.Drawing.Size(968, 40);
            this.lblPageTitle.TabIndex = 0;
            this.lblPageTitle.Text = "페이지 제목 · 개발용 템플릿";
            this.lblPageTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // tableContent
            //
            this.tableContent.ColumnCount = 2;
            this.tableContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tableContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.tableContent.Controls.Add(this.panelLeft, 0, 0);
            this.tableContent.Controls.Add(this.panelMain, 1, 0);
            this.tableContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableContent.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tableContent.Location = new System.Drawing.Point(12, 60);
            this.tableContent.Margin = new System.Windows.Forms.Padding(0);
            this.tableContent.Name = "tableContent";
            this.tableContent.RowCount = 1;
            this.tableContent.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableContent.Size = new System.Drawing.Size(976, 504);
            this.tableContent.TabIndex = 1;
            //
            // panelLeft
            //
            this.panelLeft.BackColor = System.Drawing.Color.White;
            this.panelLeft.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelLeft.Controls.Add(this.lblLeftContent);
            this.panelLeft.Controls.Add(this.lblLeftTitle);
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLeft.Location = new System.Drawing.Point(4, 4);
            this.panelLeft.Margin = new System.Windows.Forms.Padding(4);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Size = new System.Drawing.Size(333, 496);
            this.panelLeft.TabIndex = 0;
            //
            // lblLeftContent
            //
            this.lblLeftContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLeftContent.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblLeftContent.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.lblLeftContent.Location = new System.Drawing.Point(0, 40);
            this.lblLeftContent.Name = "lblLeftContent";
            this.lblLeftContent.Padding = new System.Windows.Forms.Padding(12);
            this.lblLeftContent.Size = new System.Drawing.Size(331, 454);
            this.lblLeftContent.TabIndex = 1;
            this.lblLeftContent.Text = "입력 / 선택 영역\r\n\r\nDesigner의 행·열 속성으로 배치합니다.";
            //
            // lblLeftTitle
            //
            this.lblLeftTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblLeftTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLeftTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblLeftTitle.ForeColor = System.Drawing.Color.White;
            this.lblLeftTitle.Location = new System.Drawing.Point(0, 0);
            this.lblLeftTitle.Name = "lblLeftTitle";
            this.lblLeftTitle.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblLeftTitle.Size = new System.Drawing.Size(331, 40);
            this.lblLeftTitle.TabIndex = 0;
            this.lblLeftTitle.Text = "보조 영역";
            this.lblLeftTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // panelMain
            //
            this.panelMain.BackColor = System.Drawing.Color.White;
            this.panelMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelMain.Controls.Add(this.lblMainContent);
            this.panelMain.Controls.Add(this.lblMainTitle);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(345, 4);
            this.panelMain.Margin = new System.Windows.Forms.Padding(4);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(627, 496);
            this.panelMain.TabIndex = 1;
            //
            // lblMainContent
            //
            this.lblMainContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMainContent.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblMainContent.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.lblMainContent.Location = new System.Drawing.Point(0, 40);
            this.lblMainContent.Name = "lblMainContent";
            this.lblMainContent.Padding = new System.Windows.Forms.Padding(12);
            this.lblMainContent.Size = new System.Drawing.Size(625, 454);
            this.lblMainContent.TabIndex = 1;
            this.lblMainContent.Text = "본문 영역\r\n\r\n표 / 정보 / 영상 등 실제 화면의 내용을 배치합니다.\r\n\r\n현재 템플릿의 비율은 배치 예시입니다.";
            //
            // lblMainTitle
            //
            this.lblMainTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblMainTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblMainTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblMainTitle.ForeColor = System.Drawing.Color.White;
            this.lblMainTitle.Location = new System.Drawing.Point(0, 0);
            this.lblMainTitle.Name = "lblMainTitle";
            this.lblMainTitle.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblMainTitle.Size = new System.Drawing.Size(625, 40);
            this.lblMainTitle.TabIndex = 0;
            this.lblMainTitle.Text = "주요 영역";
            this.lblMainTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // tableCommands
            //
            this.tableCommands.ColumnCount = 3;
            this.tableCommands.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableCommands.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 136F));
            this.tableCommands.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 136F));
            this.tableCommands.Controls.Add(this.lblCommandHint, 0, 0);
            this.tableCommands.Controls.Add(this.btnDefaultSample, 1, 0);
            this.tableCommands.Controls.Add(this.btnPrimarySample, 2, 0);
            this.tableCommands.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableCommands.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tableCommands.Location = new System.Drawing.Point(12, 564);
            this.tableCommands.Margin = new System.Windows.Forms.Padding(0);
            this.tableCommands.Name = "tableCommands";
            this.tableCommands.RowCount = 1;
            this.tableCommands.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableCommands.Size = new System.Drawing.Size(976, 64);
            this.tableCommands.TabIndex = 2;
            //
            // lblCommandHint
            //
            this.lblCommandHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCommandHint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblCommandHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.lblCommandHint.Location = new System.Drawing.Point(4, 4);
            this.lblCommandHint.Margin = new System.Windows.Forms.Padding(4);
            this.lblCommandHint.Name = "lblCommandHint";
            this.lblCommandHint.Size = new System.Drawing.Size(696, 56);
            this.lblCommandHint.TabIndex = 0;
            this.lblCommandHint.Text = "배치 샘플 · Common 버튼의 크기와 위치는 Designer에서 수정합니다.";
            this.lblCommandHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // btnDefaultSample
            //
            this.btnDefaultSample.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnDefaultSample.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDefaultSample.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDefaultSample.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnDefaultSample.Location = new System.Drawing.Point(712, 10);
            this.btnDefaultSample.Margin = new System.Windows.Forms.Padding(4);
            this.btnDefaultSample.Name = "btnDefaultSample";
            this.btnDefaultSample.Size = new System.Drawing.Size(120, 44);
            this.btnDefaultSample.TabIndex = 0;
            this.btnDefaultSample.Text = "일반 버튼";
            this.btnDefaultSample.UseVisualStyleBackColor = false;
            //
            // btnPrimarySample
            //
            this.btnPrimarySample.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnPrimarySample.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrimarySample.FlatAppearance.BorderSize = 0;
            this.btnPrimarySample.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrimarySample.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnPrimarySample.Location = new System.Drawing.Point(848, 10);
            this.btnPrimarySample.Margin = new System.Windows.Forms.Padding(4);
            this.btnPrimarySample.Name = "btnPrimarySample";
            this.btnPrimarySample.Role = QMC.Common.Ui.Controls.UiStandardButtonRole.Primary;
            this.btnPrimarySample.Size = new System.Drawing.Size(120, 44);
            this.btnPrimarySample.TabIndex = 1;
            this.btnPrimarySample.Text = "주요 버튼";
            this.btnPrimarySample.UseVisualStyleBackColor = false;
            //
            // UiPageTemplate
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.Controls.Add(this.tableRoot);
            this.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.MinimumSize = new System.Drawing.Size(760, 480);
            this.Name = "UiPageTemplate";
            this.Size = new System.Drawing.Size(1000, 640);
            this.tableRoot.ResumeLayout(false);
            this.tableContent.ResumeLayout(false);
            this.panelLeft.ResumeLayout(false);
            this.panelMain.ResumeLayout(false);
            this.tableCommands.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
