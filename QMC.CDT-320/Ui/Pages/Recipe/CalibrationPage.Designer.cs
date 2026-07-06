using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class CalibrationPage
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel rootLayout;
        private Panel headerPanel;
        private Label lblHeader;
        private TableLayoutPanel buttonLayout;
        private Button btnVisionCameraCal;
        private Button btnColletCal;
        private Button btnNeedleCal;
        private Button btnColletZHeightCal;
        private Button btnVisionFocusCal;
        private Button btnColletRotationCenterCal;
        private Button btnPickUpZCal;
        private Button btnPlaceZCal;
        private Button btnNeedleZCal;
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
            this.buttonLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnVisionCameraCal = new System.Windows.Forms.Button();
            this.btnColletCal = new System.Windows.Forms.Button();
            this.btnNeedleCal = new System.Windows.Forms.Button();
            this.btnColletZHeightCal = new System.Windows.Forms.Button();
            this.btnVisionFocusCal = new System.Windows.Forms.Button();
            this.btnColletRotationCenterCal = new System.Windows.Forms.Button();
            this.btnPickUpZCal = new System.Windows.Forms.Button();
            this.btnPlaceZCal = new System.Windows.Forms.Button();
            this.btnNeedleZCal = new System.Windows.Forms.Button();
            this.lblGuide = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this.buttonLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.headerPanel, 0, 0);
            this.rootLayout.Controls.Add(this.buttonLayout, 0, 1);
            this.rootLayout.Controls.Add(this.lblGuide, 0, 2);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(16);
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 330F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // headerPanel
            // 
            this.headerPanel.Controls.Add(this.lblHeader);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerPanel.Location = new System.Drawing.Point(16, 16);
            this.headerPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(1646, 44);
            this.headerPanel.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1646, 44);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "CALIBRATION";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonLayout
            // 
            this.buttonLayout.ColumnCount = 3;
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonLayout.Controls.Add(this.btnVisionCameraCal, 0, 0);
            this.buttonLayout.Controls.Add(this.btnColletCal, 1, 0);
            this.buttonLayout.Controls.Add(this.btnNeedleCal, 2, 0);
            this.buttonLayout.Controls.Add(this.btnColletZHeightCal, 0, 1);
            this.buttonLayout.Controls.Add(this.btnVisionFocusCal, 1, 1);
            this.buttonLayout.Controls.Add(this.btnColletRotationCenterCal, 2, 1);
            this.buttonLayout.Controls.Add(this.btnPickUpZCal, 0, 2);
            this.buttonLayout.Controls.Add(this.btnPlaceZCal, 1, 2);
            this.buttonLayout.Controls.Add(this.btnNeedleZCal, 2, 2);
            this.buttonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonLayout.Location = new System.Drawing.Point(16, 72);
            this.buttonLayout.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.buttonLayout.Name = "buttonLayout";
            this.buttonLayout.RowCount = 3;
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonLayout.Size = new System.Drawing.Size(1646, 316);
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
            this.btnVisionCameraCal.Size = new System.Drawing.Size(532, 89);
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
            this.btnColletCal.Location = new System.Drawing.Point(556, 8);
            this.btnColletCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnColletCal.Name = "btnColletCal";
            this.btnColletCal.Size = new System.Drawing.Size(532, 89);
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
            this.btnNeedleCal.Location = new System.Drawing.Point(1104, 8);
            this.btnNeedleCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnNeedleCal.Name = "btnNeedleCal";
            this.btnNeedleCal.Size = new System.Drawing.Size(534, 89);
            this.btnNeedleCal.TabIndex = 2;
            this.btnNeedleCal.Text = "NEEDLE PIN CAL";
            this.btnNeedleCal.UseVisualStyleBackColor = false;
            // 
            // btnColletZHeightCal
            // 
            this.btnColletZHeightCal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnColletZHeightCal.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnColletZHeightCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnColletZHeightCal.FlatAppearance.BorderSize = 0;
            this.btnColletZHeightCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColletZHeightCal.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnColletZHeightCal.ForeColor = System.Drawing.Color.White;
            this.btnColletZHeightCal.Location = new System.Drawing.Point(8, 113);
            this.btnColletZHeightCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnColletZHeightCal.Name = "btnColletZHeightCal";
            this.btnColletZHeightCal.Size = new System.Drawing.Size(532, 89);
            this.btnColletZHeightCal.TabIndex = 3;
            this.btnColletZHeightCal.Text = "COLLET Z HEIGHT CAL";
            this.btnColletZHeightCal.UseVisualStyleBackColor = false;
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
            this.btnVisionFocusCal.Location = new System.Drawing.Point(556, 113);
            this.btnVisionFocusCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnVisionFocusCal.Name = "btnVisionFocusCal";
            this.btnVisionFocusCal.Size = new System.Drawing.Size(532, 89);
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
            this.btnColletRotationCenterCal.Location = new System.Drawing.Point(1104, 113);
            this.btnColletRotationCenterCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnColletRotationCenterCal.Name = "btnColletRotationCenterCal";
            this.btnColletRotationCenterCal.Size = new System.Drawing.Size(534, 89);
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
            this.btnPickUpZCal.Location = new System.Drawing.Point(8, 218);
            this.btnPickUpZCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnPickUpZCal.Name = "btnPickUpZCal";
            this.btnPickUpZCal.Size = new System.Drawing.Size(532, 90);
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
            this.btnPlaceZCal.Location = new System.Drawing.Point(556, 218);
            this.btnPlaceZCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnPlaceZCal.Name = "btnPlaceZCal";
            this.btnPlaceZCal.Size = new System.Drawing.Size(532, 90);
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
            this.btnNeedleZCal.Location = new System.Drawing.Point(1104, 218);
            this.btnNeedleZCal.Margin = new System.Windows.Forms.Padding(8);
            this.btnNeedleZCal.Name = "btnNeedleZCal";
            this.btnNeedleZCal.Size = new System.Drawing.Size(534, 90);
            this.btnNeedleZCal.TabIndex = 8;
            this.btnNeedleZCal.Text = "NEEDLE Z CAL";
            this.btnNeedleZCal.UseVisualStyleBackColor = false;
            // 
            // lblGuide
            // 
            this.lblGuide.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblGuide.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGuide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGuide.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGuide.Location = new System.Drawing.Point(16, 302);
            this.lblGuide.Margin = new System.Windows.Forms.Padding(0);
            this.lblGuide.Name = "lblGuide";
            this.lblGuide.Padding = new System.Windows.Forms.Padding(16);
            this.lblGuide.Size = new System.Drawing.Size(1646, 446);
            this.lblGuide.TabIndex = 2;
            this.lblGuide.Text = "캘리브레이션 허브 화면입니다.\r\n\r\n- 각 버튼은 별도 모달리스 설정창을 엽니다.\r\n- BOTTOM COLLET 1:1 CAL은 Bottom Camera 기준으로 Front/Rear Picker 1~4번 콜렛의 T 성분을 0으로 보정합니다.\r\n- PICKUP Z CAL은 웨이퍼 필름 위에서 PickerZ를 하강시키며 Vacuum/Flow 신호가 들어온 위치를 PickPosition으로 저장합니다.\r\n- PLACE Z CAL은 Output Place 위치에서 PickerZ를 하강시키며 Vacuum/Flow 신호가 들어온 위치를 PlacePosition으로 저장합니다.\r\n- NEEDLE Z CAL은 WaferStageTouchSensor 기준으로 NeedleCap Touch, NeedlePin Flush, NeedlePin Ready 위치를 계산합니다.\r\n- VISION CAMERA CAL / VISION FOCUS CAL은 별도 전용 설정창에서 실행합니다.\r\n- 실장비 안전 인터락, phase gate, resource gate는 우회하지 않습니다.";
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatus.Location = new System.Drawing.Point(19, 848);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1640, 36);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "-";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // CalibrationPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.rootLayout);
            this.Name = "CalibrationPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.headerPanel.ResumeLayout(false);
            this.buttonLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
