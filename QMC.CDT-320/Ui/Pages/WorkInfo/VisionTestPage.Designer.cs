using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class VisionTestPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private GroupBox grpComm;
        private TableLayoutPanel commLayout;
        private Button _btnCommTest;
        private Label _lblCommResult;
        private ComboBox _cbVisionModule;
        private Button _btnGrab;
        private Label _lblGrabResult;
        private TextBox _txtFinder;
        private Button _btnMatch;
        private Label _lblMatchResult;
        private TextBox _txtInspector;
        private Button _btnInspect;
        private Label _lblInspectResult;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.grpComm = new System.Windows.Forms.GroupBox();
            this.commLayout = new System.Windows.Forms.TableLayoutPanel();
            this._btnCommTest = new System.Windows.Forms.Button();
            this._lblCommResult = new System.Windows.Forms.Label();
            this._cbVisionModule = new System.Windows.Forms.ComboBox();
            this._btnGrab = new System.Windows.Forms.Button();
            this._lblGrabResult = new System.Windows.Forms.Label();
            this._txtFinder = new System.Windows.Forms.TextBox();
            this._btnMatch = new System.Windows.Forms.Button();
            this._lblMatchResult = new System.Windows.Forms.Label();
            this._txtInspector = new System.Windows.Forms.TextBox();
            this._btnInspect = new System.Windows.Forms.Button();
            this._lblInspectResult = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.grpComm.SuspendLayout();
            this.commLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.grpComm, 0, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1400, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1400, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:wi.visionTest";
            this.lblHeader.Text = "VISION TEST";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpComm
            // 
            this.grpComm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpComm.Controls.Add(this.commLayout);
            this.grpComm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpComm.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpComm.Location = new System.Drawing.Point(8, 33);
            this.grpComm.Margin = new System.Windows.Forms.Padding(8, 3, 8, 8);
            this.grpComm.Name = "grpComm";
            this.grpComm.Size = new System.Drawing.Size(1384, 859);
            this.grpComm.TabIndex = 1;
            this.grpComm.TabStop = false;
            this.grpComm.Text = "VISION 통신 / 동작 테스트";
            // 
            // commLayout
            // 
            this.commLayout.ColumnCount = 3;
            this.commLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.commLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.commLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.commLayout.Controls.Add(this._btnCommTest, 0, 0);
            this.commLayout.Controls.Add(this._lblCommResult, 1, 0);
            this.commLayout.Controls.Add(this._cbVisionModule, 0, 1);
            this.commLayout.Controls.Add(this._btnGrab, 1, 1);
            this.commLayout.Controls.Add(this._lblGrabResult, 2, 1);
            this.commLayout.Controls.Add(this._txtFinder, 0, 2);
            this.commLayout.Controls.Add(this._btnMatch, 1, 2);
            this.commLayout.Controls.Add(this._lblMatchResult, 2, 2);
            this.commLayout.Controls.Add(this._txtInspector, 0, 3);
            this.commLayout.Controls.Add(this._btnInspect, 1, 3);
            this.commLayout.Controls.Add(this._lblInspectResult, 2, 3);
            this.commLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.commLayout.Location = new System.Drawing.Point(3, 23);
            this.commLayout.Name = "commLayout";
            this.commLayout.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
            this.commLayout.RowCount = 5;
            this.commLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.commLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.commLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.commLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.commLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.commLayout.Size = new System.Drawing.Size(1378, 833);
            this.commLayout.TabIndex = 0;
            // 
            // _btnCommTest
            // 
            this._btnCommTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnCommTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnCommTest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCommTest.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._btnCommTest.ForeColor = System.Drawing.Color.White;
            this._btnCommTest.Location = new System.Drawing.Point(15, 16);
            this._btnCommTest.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this._btnCommTest.Name = "_btnCommTest";
            this._btnCommTest.Size = new System.Drawing.Size(215, 36);
            this._btnCommTest.TabIndex = 0;
            this._btnCommTest.Text = "통신 테스트 (PING ALL)";
            this._btnCommTest.UseVisualStyleBackColor = false;
            // 
            // _lblCommResult
            // 
            this.commLayout.SetColumnSpan(this._lblCommResult, 2);
            this._lblCommResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblCommResult.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblCommResult.ForeColor = System.Drawing.Color.DimGray;
            this._lblCommResult.Location = new System.Drawing.Point(245, 10);
            this._lblCommResult.Name = "_lblCommResult";
            this._lblCommResult.Size = new System.Drawing.Size(1118, 48);
            this._lblCommResult.TabIndex = 1;
            this._lblCommResult.Text = "대기 — 버튼을 눌러 6채널 PING 테스트";
            this._lblCommResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbVisionModule
            // 
            this._cbVisionModule.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbVisionModule.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbVisionModule.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._cbVisionModule.Location = new System.Drawing.Point(15, 66);
            this._cbVisionModule.Margin = new System.Windows.Forms.Padding(3, 8, 12, 8);
            this._cbVisionModule.Name = "_cbVisionModule";
            this._cbVisionModule.Size = new System.Drawing.Size(215, 25);
            this._cbVisionModule.TabIndex = 2;
            // 
            // _btnGrab
            // 
            this._btnGrab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._btnGrab.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnGrab.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnGrab.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._btnGrab.ForeColor = System.Drawing.Color.White;
            this._btnGrab.Location = new System.Drawing.Point(245, 64);
            this._btnGrab.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this._btnGrab.Name = "_btnGrab";
            this._btnGrab.Size = new System.Drawing.Size(135, 36);
            this._btnGrab.TabIndex = 3;
            this._btnGrab.Text = "GRAB";
            this._btnGrab.UseVisualStyleBackColor = false;
            // 
            // _lblGrabResult
            // 
            this._lblGrabResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblGrabResult.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblGrabResult.ForeColor = System.Drawing.Color.DimGray;
            this._lblGrabResult.Location = new System.Drawing.Point(395, 58);
            this._lblGrabResult.Name = "_lblGrabResult";
            this._lblGrabResult.Size = new System.Drawing.Size(968, 48);
            this._lblGrabResult.TabIndex = 4;
            this._lblGrabResult.Text = "대기 — 모듈 선택 후 GRAB(1장 그랩 요청)";
            this._lblGrabResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtFinder
            // 
            this._txtFinder.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtFinder.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._txtFinder.Location = new System.Drawing.Point(15, 114);
            this._txtFinder.Margin = new System.Windows.Forms.Padding(3, 8, 12, 8);
            this._txtFinder.Name = "_txtFinder";
            this._txtFinder.Size = new System.Drawing.Size(215, 25);
            this._txtFinder.TabIndex = 5;
            // 
            // _btnMatch
            // 
            this._btnMatch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._btnMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMatch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMatch.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._btnMatch.ForeColor = System.Drawing.Color.White;
            this._btnMatch.Location = new System.Drawing.Point(245, 112);
            this._btnMatch.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this._btnMatch.Name = "_btnMatch";
            this._btnMatch.Size = new System.Drawing.Size(135, 36);
            this._btnMatch.TabIndex = 6;
            this._btnMatch.Text = "MATCH";
            this._btnMatch.UseVisualStyleBackColor = false;
            // 
            // _lblMatchResult
            // 
            this._lblMatchResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblMatchResult.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblMatchResult.ForeColor = System.Drawing.Color.DimGray;
            this._lblMatchResult.Location = new System.Drawing.Point(395, 106);
            this._lblMatchResult.Name = "_lblMatchResult";
            this._lblMatchResult.Size = new System.Drawing.Size(968, 48);
            this._lblMatchResult.TabIndex = 7;
            this._lblMatchResult.Text = "finder 이름 입력 후 MATCH → x/y/θ/score";
            this._lblMatchResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtInspector
            // 
            this._txtInspector.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtInspector.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._txtInspector.Location = new System.Drawing.Point(15, 162);
            this._txtInspector.Margin = new System.Windows.Forms.Padding(3, 8, 12, 8);
            this._txtInspector.Name = "_txtInspector";
            this._txtInspector.Size = new System.Drawing.Size(215, 25);
            this._txtInspector.TabIndex = 8;
            // 
            // _btnInspect
            // 
            this._btnInspect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._btnInspect.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnInspect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnInspect.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._btnInspect.ForeColor = System.Drawing.Color.White;
            this._btnInspect.Location = new System.Drawing.Point(245, 160);
            this._btnInspect.Margin = new System.Windows.Forms.Padding(3, 6, 12, 6);
            this._btnInspect.Name = "_btnInspect";
            this._btnInspect.Size = new System.Drawing.Size(135, 36);
            this._btnInspect.TabIndex = 9;
            this._btnInspect.Text = "INSPECT";
            this._btnInspect.UseVisualStyleBackColor = false;
            // 
            // _lblInspectResult
            // 
            this._lblInspectResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblInspectResult.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblInspectResult.ForeColor = System.Drawing.Color.DimGray;
            this._lblInspectResult.Location = new System.Drawing.Point(395, 154);
            this._lblInspectResult.Name = "_lblInspectResult";
            this._lblInspectResult.Size = new System.Drawing.Size(968, 48);
            this._lblInspectResult.TabIndex = 10;
            this._lblInspectResult.Text = "inspector 이름 입력 후 INSPECT → PASS/FAIL";
            this._lblInspectResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // VisionTestPage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "VisionTestPage";
            this.Size = new System.Drawing.Size(1400, 900);
            this.rootLayout.ResumeLayout(false);
            this.grpComm.ResumeLayout(false);
            this.commLayout.ResumeLayout(false);
            this.commLayout.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
