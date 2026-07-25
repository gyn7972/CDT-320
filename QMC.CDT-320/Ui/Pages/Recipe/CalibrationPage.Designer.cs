using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class CalibrationPage
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel rootLayout;
        private Panel headerPanel;
        private Label lblHeader;
        private GroupBox grpCal;
        private TableLayoutPanel buttonLayout;
        private Button btnVisionCameraCal;
        private Button btnColletCal;
        private Button btnNeedleCal;
        private Button btnSideVisionFocusCal;
        private Button btnVisionFocusCal;
        private Button btnColletRotationCenterCal;
        private Button btnPickUpZCal;
        private Button btnPlaceZCal;
        private Button btnNeedleZCal;
        private Button btnAutoCalibration;
        private Button btnColletCleaning;
        private FlowLayoutPanel safeMovePanel;
        private Label lblSafeMove;
        private NumericUpDown numSafeMovePercent;
        private Label lblSafeMoveHint;
        private Label lblGuide;
        private Label lblStatus;

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
            this.headerPanel = new System.Windows.Forms.Panel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.grpCal = new System.Windows.Forms.GroupBox();
            this.buttonLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnVisionCameraCal = new System.Windows.Forms.Button();
            this.btnColletCal = new System.Windows.Forms.Button();
            this.btnNeedleCal = new System.Windows.Forms.Button();
            this.btnSideVisionFocusCal = new System.Windows.Forms.Button();
            this.btnVisionFocusCal = new System.Windows.Forms.Button();
            this.btnColletRotationCenterCal = new System.Windows.Forms.Button();
            this.btnPickUpZCal = new System.Windows.Forms.Button();
            this.btnPlaceZCal = new System.Windows.Forms.Button();
            this.btnNeedleZCal = new System.Windows.Forms.Button();
            this.btnAutoCalibration = new System.Windows.Forms.Button();
            this.btnColletCleaning = new System.Windows.Forms.Button();
            this.safeMovePanel = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSafeMove = new System.Windows.Forms.Label();
            this.numSafeMovePercent = new System.Windows.Forms.NumericUpDown();
            this.lblSafeMoveHint = new System.Windows.Forms.Label();
            this.lblGuide = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this.grpCal.SuspendLayout();
            this.buttonLayout.SuspendLayout();
            this.safeMovePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSafeMovePercent)).BeginInit();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.BackColor = System.Drawing.Color.White;
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.headerPanel, 0, 0);
            this.rootLayout.Controls.Add(this.grpCal, 0, 1);
            this.rootLayout.Controls.Add(this.safeMovePanel, 0, 2);
            this.rootLayout.Controls.Add(this.lblGuide, 0, 3);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 4);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 5;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 57.17791F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 42.82209F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 54F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            //
            // safeMovePanel
            //
            this.safeMovePanel.BackColor = System.Drawing.Color.White;
            this.safeMovePanel.Controls.Add(this.lblSafeMove);
            this.safeMovePanel.Controls.Add(this.numSafeMovePercent);
            this.safeMovePanel.Controls.Add(this.lblSafeMoveHint);
            this.safeMovePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.safeMovePanel.Margin = new System.Windows.Forms.Padding(1);
            this.safeMovePanel.Name = "safeMovePanel";
            this.safeMovePanel.WrapContents = false;
            //
            // lblSafeMove
            //
            this.lblSafeMove.AutoSize = true;
            this.lblSafeMove.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblSafeMove.Margin = new System.Windows.Forms.Padding(8, 8, 4, 4);
            this.lblSafeMove.Name = "lblSafeMove";
            this.lblSafeMove.Text = "안전위치(Avoid) 이동 속도 %";
            this.lblSafeMove.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numSafeMovePercent
            //
            this.numSafeMovePercent.DecimalPlaces = 1;
            this.numSafeMovePercent.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.numSafeMovePercent.Margin = new System.Windows.Forms.Padding(4, 5, 8, 4);
            this.numSafeMovePercent.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numSafeMovePercent.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numSafeMovePercent.Name = "numSafeMovePercent";
            this.numSafeMovePercent.Size = new System.Drawing.Size(90, 30);
            this.numSafeMovePercent.Value = new decimal(new int[] { 7, 0, 0, 0 });
            this.numSafeMovePercent.ValueChanged += new System.EventHandler(this.numSafeMovePercent_ValueChanged);
            //
            // lblSafeMoveHint
            //
            this.lblSafeMoveHint.AutoSize = true;
            this.lblSafeMoveHint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSafeMoveHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lblSafeMoveHint.Margin = new System.Windows.Forms.Padding(4, 9, 4, 4);
            this.lblSafeMoveHint.Name = "lblSafeMoveHint";
            this.lblSafeMoveHint.Text = "= 각 축 Default 속도·가속·감속 × % (측정 속도와 분리, 전역 스케일과 중첩 안 됨). 모든 캘리브레이션 공통.";
            this.lblSafeMoveHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // headerPanel
            // 
            this.headerPanel.Controls.Add(this.lblHeader);
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Margin = new System.Windows.Forms.Padding(0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(1678, 30);
            this.headerPanel.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1678, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "CALIBRATION";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpCal
            // 
            this.grpCal.BackColor = System.Drawing.Color.White;
            this.grpCal.Controls.Add(this.buttonLayout);
            this.grpCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCal.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpCal.Location = new System.Drawing.Point(1, 31);
            this.grpCal.Margin = new System.Windows.Forms.Padding(1);
            this.grpCal.Name = "grpCal";
            this.grpCal.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpCal.Size = new System.Drawing.Size(1676, 464);
            this.grpCal.TabIndex = 1;
            this.grpCal.TabStop = false;
            this.grpCal.Text = "CALIBRATION ITEMS";
            // 
            // buttonLayout
            // 
            this.buttonLayout.BackColor = System.Drawing.Color.White;
            this.buttonLayout.ColumnCount = 3;
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonLayout.Controls.Add(this.btnVisionCameraCal, 0, 0);
            this.buttonLayout.Controls.Add(this.btnColletCal, 1, 0);
            this.buttonLayout.Controls.Add(this.btnNeedleCal, 2, 0);
            this.buttonLayout.Controls.Add(this.btnSideVisionFocusCal, 0, 1);
            this.buttonLayout.Controls.Add(this.btnVisionFocusCal, 1, 1);
            this.buttonLayout.Controls.Add(this.btnColletRotationCenterCal, 2, 1);
            this.buttonLayout.Controls.Add(this.btnPickUpZCal, 0, 2);
            this.buttonLayout.Controls.Add(this.btnPlaceZCal, 1, 2);
            this.buttonLayout.Controls.Add(this.btnNeedleZCal, 2, 2);
            // 3 x 3 격자 유지: AUTO CALIBRATION / COLLET CLEANING을 4행에 한 칸씩 배치한다(전체 폭 사용 안 함).
            this.buttonLayout.Controls.Add(this.btnAutoCalibration, 0, 3);
            this.buttonLayout.Controls.Add(this.btnColletCleaning, 1, 3);
            this.buttonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonLayout.Location = new System.Drawing.Point(6, 20);
            this.buttonLayout.Margin = new System.Windows.Forms.Padding(0);
            this.buttonLayout.Name = "buttonLayout";
            this.buttonLayout.RowCount = 4;
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.buttonLayout.Size = new System.Drawing.Size(1664, 438);
            this.buttonLayout.TabIndex = 1;
            // 
            // btnVisionCameraCal
            // 
            this.btnVisionCameraCal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnVisionCameraCal.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnVisionCameraCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnVisionCameraCal.FlatAppearance.BorderSize = 0;
            this.btnVisionCameraCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnVisionCameraCal.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnVisionCameraCal.ForeColor = System.Drawing.Color.White;
            this.btnVisionCameraCal.Location = new System.Drawing.Point(8, 8);
            this.btnVisionCameraCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnVisionCameraCal.Name = "btnVisionCameraCal";
            this.btnVisionCameraCal.Size = new System.Drawing.Size(538, 129);
            this.btnVisionCameraCal.TabIndex = 0;
            this.btnVisionCameraCal.Text = "VISION CAMERA CAL";
            this.btnVisionCameraCal.UseVisualStyleBackColor = false;
            // 
            // btnColletCal
            // 
            this.btnColletCal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnColletCal.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnColletCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnColletCal.FlatAppearance.BorderSize = 0;
            this.btnColletCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColletCal.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnColletCal.ForeColor = System.Drawing.Color.White;
            this.btnColletCal.Location = new System.Drawing.Point(562, 8);
            this.btnColletCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnColletCal.Name = "btnColletCal";
            this.btnColletCal.Size = new System.Drawing.Size(538, 129);
            this.btnColletCal.TabIndex = 1;
            this.btnColletCal.Text = "BOTTOM COLLET 1:1 CAL";
            this.btnColletCal.UseVisualStyleBackColor = false;
            // 
            // btnNeedleCal
            // 
            this.btnNeedleCal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNeedleCal.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNeedleCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNeedleCal.FlatAppearance.BorderSize = 0;
            this.btnNeedleCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNeedleCal.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnNeedleCal.ForeColor = System.Drawing.Color.White;
            this.btnNeedleCal.Location = new System.Drawing.Point(1116, 8);
            this.btnNeedleCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnNeedleCal.Name = "btnNeedleCal";
            this.btnNeedleCal.Size = new System.Drawing.Size(540, 129);
            this.btnNeedleCal.TabIndex = 2;
            this.btnNeedleCal.Text = "NEEDLE PIN CAL";
            this.btnNeedleCal.UseVisualStyleBackColor = false;
            // 
            // btnSideVisionFocusCal
            //
            this.btnSideVisionFocusCal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSideVisionFocusCal.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSideVisionFocusCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSideVisionFocusCal.FlatAppearance.BorderSize = 0;
            this.btnSideVisionFocusCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSideVisionFocusCal.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnSideVisionFocusCal.ForeColor = System.Drawing.Color.White;
            this.btnSideVisionFocusCal.Location = new System.Drawing.Point(8, 153);
            this.btnSideVisionFocusCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnSideVisionFocusCal.Name = "btnSideVisionFocusCal";
            this.btnSideVisionFocusCal.Size = new System.Drawing.Size(538, 129);
            this.btnSideVisionFocusCal.TabIndex = 3;
            this.btnSideVisionFocusCal.Text = "SIDE VISION FOCUS CAL";
            this.btnSideVisionFocusCal.UseVisualStyleBackColor = false;
            // 
            // btnVisionFocusCal
            // 
            this.btnVisionFocusCal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnVisionFocusCal.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnVisionFocusCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnVisionFocusCal.FlatAppearance.BorderSize = 0;
            this.btnVisionFocusCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnVisionFocusCal.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnVisionFocusCal.ForeColor = System.Drawing.Color.White;
            this.btnVisionFocusCal.Location = new System.Drawing.Point(562, 153);
            this.btnVisionFocusCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnVisionFocusCal.Name = "btnVisionFocusCal";
            this.btnVisionFocusCal.Size = new System.Drawing.Size(538, 129);
            this.btnVisionFocusCal.TabIndex = 4;
            this.btnVisionFocusCal.Text = "VISION FOCUS CAL";
            this.btnVisionFocusCal.UseVisualStyleBackColor = false;
            // 
            // btnColletRotationCenterCal
            // 
            this.btnColletRotationCenterCal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnColletRotationCenterCal.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnColletRotationCenterCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnColletRotationCenterCal.FlatAppearance.BorderSize = 0;
            this.btnColletRotationCenterCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColletRotationCenterCal.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnColletRotationCenterCal.ForeColor = System.Drawing.Color.White;
            this.btnColletRotationCenterCal.Location = new System.Drawing.Point(1116, 153);
            this.btnColletRotationCenterCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnColletRotationCenterCal.Name = "btnColletRotationCenterCal";
            this.btnColletRotationCenterCal.Size = new System.Drawing.Size(540, 129);
            this.btnColletRotationCenterCal.TabIndex = 5;
            this.btnColletRotationCenterCal.Text = "COLLET ROTATION CENTER CAL";
            this.btnColletRotationCenterCal.UseVisualStyleBackColor = false;
            // 
            // btnPickUpZCal
            // 
            this.btnPickUpZCal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPickUpZCal.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPickUpZCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPickUpZCal.FlatAppearance.BorderSize = 0;
            this.btnPickUpZCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPickUpZCal.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnPickUpZCal.ForeColor = System.Drawing.Color.White;
            this.btnPickUpZCal.Location = new System.Drawing.Point(8, 298);
            this.btnPickUpZCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnPickUpZCal.Name = "btnPickUpZCal";
            this.btnPickUpZCal.Size = new System.Drawing.Size(538, 132);
            this.btnPickUpZCal.TabIndex = 6;
            this.btnPickUpZCal.Text = "PICKUP Z CAL";
            this.btnPickUpZCal.UseVisualStyleBackColor = false;
            // 
            // btnPlaceZCal
            // 
            this.btnPlaceZCal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPlaceZCal.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlaceZCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlaceZCal.FlatAppearance.BorderSize = 0;
            this.btnPlaceZCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlaceZCal.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnPlaceZCal.ForeColor = System.Drawing.Color.White;
            this.btnPlaceZCal.Location = new System.Drawing.Point(562, 298);
            this.btnPlaceZCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnPlaceZCal.Name = "btnPlaceZCal";
            this.btnPlaceZCal.Size = new System.Drawing.Size(538, 132);
            this.btnPlaceZCal.TabIndex = 7;
            this.btnPlaceZCal.Text = "PLACE Z CAL";
            this.btnPlaceZCal.UseVisualStyleBackColor = false;
            // 
            // btnNeedleZCal
            // 
            this.btnNeedleZCal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNeedleZCal.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNeedleZCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNeedleZCal.FlatAppearance.BorderSize = 0;
            this.btnNeedleZCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNeedleZCal.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnNeedleZCal.ForeColor = System.Drawing.Color.White;
            this.btnNeedleZCal.Location = new System.Drawing.Point(1116, 298);
            this.btnNeedleZCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnNeedleZCal.Name = "btnNeedleZCal";
            this.btnNeedleZCal.Size = new System.Drawing.Size(540, 132);
            this.btnNeedleZCal.TabIndex = 8;
            this.btnNeedleZCal.Text = "NEEDLE Z CAL";
            this.btnNeedleZCal.UseVisualStyleBackColor = false;
            //
            // btnAutoCalibration
            //
            this.btnAutoCalibration.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAutoCalibration.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAutoCalibration.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAutoCalibration.FlatAppearance.BorderSize = 0;
            this.btnAutoCalibration.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAutoCalibration.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnAutoCalibration.ForeColor = System.Drawing.Color.White;
            this.btnAutoCalibration.Location = new System.Drawing.Point(8, 335);
            this.btnAutoCalibration.Margin = new System.Windows.Forms.Padding(8);
            this.btnAutoCalibration.Name = "btnAutoCalibration";
            this.btnAutoCalibration.Size = new System.Drawing.Size(1648, 95);
            this.btnAutoCalibration.TabIndex = 9;
            this.btnAutoCalibration.Text = "AUTO CALIBRATION";
            this.btnAutoCalibration.UseVisualStyleBackColor = false;
            //
            // btnColletCleaning
            //
            this.btnColletCleaning.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnColletCleaning.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnColletCleaning.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnColletCleaning.FlatAppearance.BorderSize = 0;
            this.btnColletCleaning.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColletCleaning.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnColletCleaning.ForeColor = System.Drawing.Color.White;
            this.btnColletCleaning.Location = new System.Drawing.Point(8, 446);
            this.btnColletCleaning.Margin = new System.Windows.Forms.Padding(8);
            this.btnColletCleaning.Name = "btnColletCleaning";
            this.btnColletCleaning.Size = new System.Drawing.Size(1648, 95);
            this.btnColletCleaning.TabIndex = 10;
            this.btnColletCleaning.Text = "COLLET CLEANING";
            this.btnColletCleaning.UseVisualStyleBackColor = false;
            // 
            // lblGuide
            // 
            this.lblGuide.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblGuide.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGuide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGuide.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGuide.Location = new System.Drawing.Point(1, 497);
            this.lblGuide.Margin = new System.Windows.Forms.Padding(1);
            this.lblGuide.Name = "lblGuide";
            this.lblGuide.Padding = new System.Windows.Forms.Padding(16);
            this.lblGuide.Size = new System.Drawing.Size(1676, 347);
            this.lblGuide.TabIndex = 2;
            this.lblGuide.Text = "캘리브레이션 허브 화면입니다.\r\n\r\n" +
                "- 각 버튼은 별도 모달리스 설정창을 엽니다.\r\n" +
                "- BOTTOM COLLET 1:1 CAL은 Bottom Camera 기준으로 Front/Rear Picker 1~4번 콜렛의 T 성분을 0으로 보정합니다.\r\n" +
                "- PICKUP Z CAL은 웨이퍼 필름 위에서 PickerZ를 하강시키며 Vacuum/Flow 신호가 들어온 위치를 PickPosition으로 저장합니다.\r\n" +
                "- PLACE Z CAL은 Output Place 위치에서 PickerZ를 하강시키며 Vacuum/Flow 신호가 들어온 위치를 PlacePosition으로 저장합니다.\r\n" +
                "- NEEDLE Z CAL은 WaferStageTouchSensor 기준으로 NeedleCap Touch, NeedlePin Flush, NeedlePin Ready 위치를 계산합니다.\r\n" +
                "- VISION CAMERA CAL / VISION FOCUS CAL / SIDE VISION FOCUS CAL은 별도 전용 설정창에서 실행합니다.\r\n" +
                "- 실장비 안전 인터락, phase gate, resource gate는 우회하지 않습니다.";
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblStatus.Location = new System.Drawing.Point(1, 845);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(1, 0, 1, 1);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1676, 54);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "-";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // events (디자이너 관리)
            //
            this.btnVisionCameraCal.Click += new System.EventHandler(this.btnVisionCameraCal_Click);
            this.btnColletCal.Click += new System.EventHandler(this.btnColletCal_Click);
            this.btnNeedleCal.Click += new System.EventHandler(this.btnNeedleCal_Click);
            this.btnSideVisionFocusCal.Click += new System.EventHandler(this.btnSideVisionFocusCal_Click);
            this.btnVisionFocusCal.Click += new System.EventHandler(this.btnVisionFocusCal_Click);
            this.btnColletRotationCenterCal.Click += new System.EventHandler(this.btnColletRotationCenterCal_Click);
            this.btnPickUpZCal.Click += new System.EventHandler(this.btnPickUpZCal_Click);
            this.btnPlaceZCal.Click += new System.EventHandler(this.btnPlaceZCal_Click);
            this.btnNeedleZCal.Click += new System.EventHandler(this.btnNeedleZCal_Click);
            this.btnAutoCalibration.Click += new System.EventHandler(this.btnAutoCalibration_Click);
            this.btnColletCleaning.Click += new System.EventHandler(this.btnColletCleaning_Click);
            //
            // CalibrationPage
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "CalibrationPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.headerPanel.ResumeLayout(false);
            this.grpCal.ResumeLayout(false);
            this.buttonLayout.ResumeLayout(false);
            this.safeMovePanel.ResumeLayout(false);
            this.safeMovePanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSafeMovePercent)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
