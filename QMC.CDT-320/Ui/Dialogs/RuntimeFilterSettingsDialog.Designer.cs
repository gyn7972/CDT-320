namespace QMC.CDT_320.Ui.Dialogs
{
    partial class RuntimeFilterSettingsDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel settingsLayout;
        private System.Windows.Forms.Label lblColItem;
        private System.Windows.Forms.Label lblColPick;
        private System.Windows.Forms.Label lblColPlace;
        private System.Windows.Forms.Label lblItemFc;
        private System.Windows.Forms.Label lblItemAlpha;
        private System.Windows.Forms.Label lblItemOutlierXy;
        private System.Windows.Forms.Label lblItemOutlierT;
        private System.Windows.Forms.Label lblItemClampXy;
        private System.Windows.Forms.Label lblItemClampT;
        private System.Windows.Forms.TextBox tbPickFc;
        private System.Windows.Forms.TextBox tbPlaceFc;
        private System.Windows.Forms.Label lblPickAlpha;
        private System.Windows.Forms.Label lblPlaceAlpha;
        private System.Windows.Forms.TextBox tbPickOutlierXy;
        private System.Windows.Forms.TextBox tbPlaceOutlierXy;
        private System.Windows.Forms.TextBox tbPickOutlierT;
        private System.Windows.Forms.TextBox tbPlaceOutlierT;
        private System.Windows.Forms.TextBox tbPickClampXy;
        private System.Windows.Forms.TextBox tbPlaceClampXy;
        private System.Windows.Forms.TextBox tbPickClampT;
        private System.Windows.Forms.TextBox tbPlaceClampT;
        private System.Windows.Forms.Label lblColZ;
        private System.Windows.Forms.TextBox tbZFc;
        private System.Windows.Forms.Label lblZAlpha;
        private System.Windows.Forms.TextBox tbZOutlier;
        private System.Windows.Forms.Label lblZOutlierTDash;
        private System.Windows.Forms.TextBox tbZClamp;
        private System.Windows.Forms.Label lblZClampTDash;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.FlowLayoutPanel buttonBar;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnSave;

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
            this.settingsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblColItem = new System.Windows.Forms.Label();
            this.lblColPick = new System.Windows.Forms.Label();
            this.lblColPlace = new System.Windows.Forms.Label();
            this.lblItemFc = new System.Windows.Forms.Label();
            this.lblItemAlpha = new System.Windows.Forms.Label();
            this.lblItemOutlierXy = new System.Windows.Forms.Label();
            this.lblItemOutlierT = new System.Windows.Forms.Label();
            this.lblItemClampXy = new System.Windows.Forms.Label();
            this.lblItemClampT = new System.Windows.Forms.Label();
            this.tbPickFc = new System.Windows.Forms.TextBox();
            this.tbPlaceFc = new System.Windows.Forms.TextBox();
            this.lblPickAlpha = new System.Windows.Forms.Label();
            this.lblPlaceAlpha = new System.Windows.Forms.Label();
            this.tbPickOutlierXy = new System.Windows.Forms.TextBox();
            this.tbPlaceOutlierXy = new System.Windows.Forms.TextBox();
            this.tbPickOutlierT = new System.Windows.Forms.TextBox();
            this.tbPlaceOutlierT = new System.Windows.Forms.TextBox();
            this.tbPickClampXy = new System.Windows.Forms.TextBox();
            this.tbPlaceClampXy = new System.Windows.Forms.TextBox();
            this.tbPickClampT = new System.Windows.Forms.TextBox();
            this.tbPlaceClampT = new System.Windows.Forms.TextBox();
            this.lblColZ = new System.Windows.Forms.Label();
            this.tbZFc = new System.Windows.Forms.TextBox();
            this.lblZAlpha = new System.Windows.Forms.Label();
            this.tbZOutlier = new System.Windows.Forms.TextBox();
            this.lblZOutlierTDash = new System.Windows.Forms.Label();
            this.tbZClamp = new System.Windows.Forms.TextBox();
            this.lblZClampTDash = new System.Windows.Forms.Label();
            this.lblInfo = new System.Windows.Forms.Label();
            this.buttonBar = new System.Windows.Forms.FlowLayoutPanel();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.settingsLayout.SuspendLayout();
            this.buttonBar.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.settingsLayout, 0, 0);
            this.rootLayout.Controls.Add(this.lblInfo, 0, 1);
            this.rootLayout.Controls.Add(this.buttonBar, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.rootLayout.Size = new System.Drawing.Size(780, 420);
            this.rootLayout.TabIndex = 0;
            //
            // settingsLayout
            //
            this.settingsLayout.ColumnCount = 4;
            this.settingsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.settingsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.settingsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.settingsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.settingsLayout.Controls.Add(this.lblColItem, 0, 0);
            this.settingsLayout.Controls.Add(this.lblColPick, 1, 0);
            this.settingsLayout.Controls.Add(this.lblColPlace, 2, 0);
            this.settingsLayout.Controls.Add(this.lblItemFc, 0, 1);
            this.settingsLayout.Controls.Add(this.tbPickFc, 1, 1);
            this.settingsLayout.Controls.Add(this.tbPlaceFc, 2, 1);
            this.settingsLayout.Controls.Add(this.lblItemAlpha, 0, 2);
            this.settingsLayout.Controls.Add(this.lblPickAlpha, 1, 2);
            this.settingsLayout.Controls.Add(this.lblPlaceAlpha, 2, 2);
            this.settingsLayout.Controls.Add(this.lblItemOutlierXy, 0, 3);
            this.settingsLayout.Controls.Add(this.tbPickOutlierXy, 1, 3);
            this.settingsLayout.Controls.Add(this.tbPlaceOutlierXy, 2, 3);
            this.settingsLayout.Controls.Add(this.lblItemOutlierT, 0, 4);
            this.settingsLayout.Controls.Add(this.tbPickOutlierT, 1, 4);
            this.settingsLayout.Controls.Add(this.tbPlaceOutlierT, 2, 4);
            this.settingsLayout.Controls.Add(this.lblItemClampXy, 0, 5);
            this.settingsLayout.Controls.Add(this.tbPickClampXy, 1, 5);
            this.settingsLayout.Controls.Add(this.tbPlaceClampXy, 2, 5);
            this.settingsLayout.Controls.Add(this.lblItemClampT, 0, 6);
            this.settingsLayout.Controls.Add(this.tbPickClampT, 1, 6);
            this.settingsLayout.Controls.Add(this.tbPlaceClampT, 2, 6);
            this.settingsLayout.Controls.Add(this.lblColZ, 3, 0);
            this.settingsLayout.Controls.Add(this.tbZFc, 3, 1);
            this.settingsLayout.Controls.Add(this.lblZAlpha, 3, 2);
            this.settingsLayout.Controls.Add(this.tbZOutlier, 3, 3);
            this.settingsLayout.Controls.Add(this.lblZOutlierTDash, 3, 4);
            this.settingsLayout.Controls.Add(this.tbZClamp, 3, 5);
            this.settingsLayout.Controls.Add(this.lblZClampTDash, 3, 6);
            this.settingsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsLayout.Location = new System.Drawing.Point(11, 11);
            this.settingsLayout.Name = "settingsLayout";
            this.settingsLayout.RowCount = 7;
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.settingsLayout.Size = new System.Drawing.Size(618, 323);
            this.settingsLayout.TabIndex = 0;
            //
            // lblColItem
            //
            this.lblColItem.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColItem.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblColItem.Name = "lblColItem";
            this.lblColItem.Text = "항목";
            this.lblColItem.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblColPick
            //
            this.lblColPick.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColPick.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblColPick.Name = "lblColPick";
            this.lblColPick.Text = "PICK";
            this.lblColPick.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblColPlace
            //
            this.lblColPlace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColPlace.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblColPlace.Name = "lblColPlace";
            this.lblColPlace.Text = "PLACE";
            this.lblColPlace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblItemFc
            //
            this.lblItemFc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblItemFc.Name = "lblItemFc";
            this.lblItemFc.Text = "컷오프 fc (cycles/sample)";
            this.lblItemFc.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblItemAlpha
            //
            this.lblItemAlpha.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblItemAlpha.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(110)))), ((int)(((byte)(110)))));
            this.lblItemAlpha.Name = "lblItemAlpha";
            this.lblItemAlpha.Text = "alpha 환산 (읽기전용)";
            this.lblItemAlpha.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblItemOutlierXy
            //
            this.lblItemOutlierXy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblItemOutlierXy.Name = "lblItemOutlierXy";
            this.lblItemOutlierXy.Text = "이상치 거부 한계 X/Y (mm)";
            this.lblItemOutlierXy.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblItemOutlierT
            //
            this.lblItemOutlierT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblItemOutlierT.Name = "lblItemOutlierT";
            this.lblItemOutlierT.Text = "이상치 거부 한계 T (deg)";
            this.lblItemOutlierT.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblItemClampXy
            //
            this.lblItemClampXy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblItemClampXy.Name = "lblItemClampXy";
            this.lblItemClampXy.Text = "필터 상태 클램프 X/Y (mm)";
            this.lblItemClampXy.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblItemClampT
            //
            this.lblItemClampT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblItemClampT.Name = "lblItemClampT";
            this.lblItemClampT.Text = "필터 상태 클램프 T (deg)";
            this.lblItemClampT.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // tbPickFc
            //
            this.tbPickFc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbPickFc.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbPickFc.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbPickFc.Name = "tbPickFc";
            this.tbPickFc.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tbPickFc.TextChanged += new System.EventHandler(this.tbFc_TextChanged);
            //
            // tbPlaceFc
            //
            this.tbPlaceFc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbPlaceFc.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbPlaceFc.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbPlaceFc.Name = "tbPlaceFc";
            this.tbPlaceFc.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tbPlaceFc.TextChanged += new System.EventHandler(this.tbFc_TextChanged);
            //
            // lblPickAlpha
            //
            this.lblPickAlpha.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickAlpha.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(110)))), ((int)(((byte)(110)))));
            this.lblPickAlpha.Name = "lblPickAlpha";
            this.lblPickAlpha.Padding = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.lblPickAlpha.Text = "-";
            this.lblPickAlpha.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblPlaceAlpha
            //
            this.lblPlaceAlpha.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceAlpha.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(110)))), ((int)(((byte)(110)))));
            this.lblPlaceAlpha.Name = "lblPlaceAlpha";
            this.lblPlaceAlpha.Padding = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.lblPlaceAlpha.Text = "-";
            this.lblPlaceAlpha.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // tbPickOutlierXy
            //
            this.tbPickOutlierXy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbPickOutlierXy.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbPickOutlierXy.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbPickOutlierXy.Name = "tbPickOutlierXy";
            this.tbPickOutlierXy.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // tbPlaceOutlierXy
            //
            this.tbPlaceOutlierXy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbPlaceOutlierXy.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbPlaceOutlierXy.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbPlaceOutlierXy.Name = "tbPlaceOutlierXy";
            this.tbPlaceOutlierXy.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // tbPickOutlierT
            //
            this.tbPickOutlierT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbPickOutlierT.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbPickOutlierT.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbPickOutlierT.Name = "tbPickOutlierT";
            this.tbPickOutlierT.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // tbPlaceOutlierT
            //
            this.tbPlaceOutlierT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbPlaceOutlierT.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbPlaceOutlierT.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbPlaceOutlierT.Name = "tbPlaceOutlierT";
            this.tbPlaceOutlierT.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // tbPickClampXy
            //
            this.tbPickClampXy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbPickClampXy.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbPickClampXy.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbPickClampXy.Name = "tbPickClampXy";
            this.tbPickClampXy.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // tbPlaceClampXy
            //
            this.tbPlaceClampXy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbPlaceClampXy.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbPlaceClampXy.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbPlaceClampXy.Name = "tbPlaceClampXy";
            this.tbPlaceClampXy.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // tbPickClampT
            //
            this.tbPickClampT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbPickClampT.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbPickClampT.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbPickClampT.Name = "tbPickClampT";
            this.tbPickClampT.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // tbPlaceClampT
            //
            this.tbPlaceClampT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbPlaceClampT.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbPlaceClampT.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbPlaceClampT.Name = "tbPlaceClampT";
            this.tbPlaceClampT.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblColZ
            //
            this.lblColZ.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColZ.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblColZ.Name = "lblColZ";
            this.lblColZ.Text = "PICKER Z";
            this.lblColZ.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // tbZFc
            //
            this.tbZFc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbZFc.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbZFc.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbZFc.Name = "tbZFc";
            this.tbZFc.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tbZFc.TextChanged += new System.EventHandler(this.tbFc_TextChanged);
            //
            // lblZAlpha
            //
            this.lblZAlpha.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblZAlpha.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(110)))), ((int)(((byte)(110)))));
            this.lblZAlpha.Name = "lblZAlpha";
            this.lblZAlpha.Padding = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.lblZAlpha.Text = "-";
            this.lblZAlpha.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // tbZOutlier
            //
            this.tbZOutlier.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbZOutlier.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbZOutlier.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbZOutlier.Name = "tbZOutlier";
            this.tbZOutlier.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblZOutlierTDash
            //
            this.lblZOutlierTDash.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblZOutlierTDash.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(170)))), ((int)(((byte)(170)))));
            this.lblZOutlierTDash.Name = "lblZOutlierTDash";
            this.lblZOutlierTDash.Padding = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.lblZOutlierTDash.Text = "-";
            this.lblZOutlierTDash.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // tbZClamp
            //
            this.tbZClamp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbZClamp.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tbZClamp.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.tbZClamp.Name = "tbZClamp";
            this.tbZClamp.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblZClampTDash
            //
            this.lblZClampTDash.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblZClampTDash.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(170)))), ((int)(((byte)(170)))));
            this.lblZClampTDash.Name = "lblZClampTDash";
            this.lblZClampTDash.Padding = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.lblZClampTDash.Text = "-";
            this.lblZClampTDash.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblInfo
            //
            this.lblInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Text = "이상치 한계보다 큰 측정은 채널 샘플 폐기. 클램프는 누적 보정값 절대 한계. PICKER Z는 단채널 — X/Y 행이 Z 한계, T 행 없음.";
            this.lblInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // buttonBar
            //
            this.buttonBar.Controls.Add(this.btnClose);
            this.buttonBar.Controls.Add(this.btnSave);
            this.buttonBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonBar.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonBar.Name = "buttonBar";
            this.buttonBar.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.buttonBar.TabIndex = 2;
            //
            // btnClose
            //
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnClose.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(150, 36);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // btnSave
            //
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(88)))), ((int)(((byte)(31)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(150, 36);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "SAVE";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // RuntimeFilterSettingsDialog
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(780, 420);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RuntimeFilterSettingsDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "RUNTIME FILTER SETTINGS";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.RuntimeFilterSettingsDialog_FormClosing);
            this.rootLayout.ResumeLayout(false);
            this.settingsLayout.ResumeLayout(false);
            this.settingsLayout.PerformLayout();
            this.buttonBar.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
