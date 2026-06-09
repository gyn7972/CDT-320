using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    partial class LightLiveTuningPanel
    {
        // ─── 컨트롤 필드 (Controls.Add 대상은 전부 필드여야 디자이너가 인식) ───
        private Label         _lblHeader;
        private Label         _lblPeriod;
        private NumericUpDown _numPeriod;
        private Label         _lblPeriodHz;   // Stage 87 — ms → Hz 환산 표시
        private Button        _btnToggleLive;
        private Button        _btnAllOn;
        private Button        _btnAllOff;
        private Label         _lblCount;
        private Label         _lblLast;
        private Label         _lblStatus;
        private Label         _lblCamInfo;     // Stage 87 — 카메라 fps 안내

        // ─── Dispose ───
        private IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _timer?.Stop(); _timer?.Dispose(); } catch { }
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        // ─── InitializeComponent — 디자이너 직렬화 가능한 표준 형태 ───
        //  (객체 초기화자 / 지역변수 산술 금지: 속성별 할당 + 리터럴 좌표만)
        private void InitializeComponent()
        {
            this._lblHeader = new System.Windows.Forms.Label();
            this._lblPeriod = new System.Windows.Forms.Label();
            this._numPeriod = new System.Windows.Forms.NumericUpDown();
            this._lblPeriodHz = new System.Windows.Forms.Label();
            this._btnToggleLive = new System.Windows.Forms.Button();
            this._btnAllOn = new System.Windows.Forms.Button();
            this._btnAllOff = new System.Windows.Forms.Button();
            this._lblCount = new System.Windows.Forms.Label();
            this._lblLast = new System.Windows.Forms.Label();
            this._lblStatus = new System.Windows.Forms.Label();
            this._lblCamInfo = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this._numPeriod)).BeginInit();
            this.SuspendLayout();
            // 
            // _lblHeader
            // 
            this._lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(179)))), ((int)(((byte)(27)))), ((int)(((byte)(27)))));
            this._lblHeader.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._lblHeader.ForeColor = System.Drawing.Color.White;
            this._lblHeader.Location = new System.Drawing.Point(0, 0);
            this._lblHeader.Name = "_lblHeader";
            this._lblHeader.Size = new System.Drawing.Size(220, 28);
            this._lblHeader.TabIndex = 0;
            this._lblHeader.Text = "라이브 튜닝 (임시)";
            this._lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _lblPeriod
            // 
            this._lblPeriod.Location = new System.Drawing.Point(8, 42);
            this._lblPeriod.Name = "_lblPeriod";
            this._lblPeriod.Size = new System.Drawing.Size(70, 20);
            this._lblPeriod.TabIndex = 1;
            this._lblPeriod.Text = "주기 (ms)";
            // 
            // _numPeriod
            // 
            this._numPeriod.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this._numPeriod.Location = new System.Drawing.Point(82, 38);
            this._numPeriod.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._numPeriod.Name = "_numPeriod";
            this._numPeriod.Size = new System.Drawing.Size(80, 21);
            this._numPeriod.TabIndex = 2;
            this._numPeriod.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this._numPeriod.ValueChanged += new System.EventHandler(this.OnPeriodChanged);
            // 
            // _lblPeriodHz
            // 
            this._lblPeriodHz.ForeColor = System.Drawing.Color.DimGray;
            this._lblPeriodHz.Location = new System.Drawing.Point(168, 42);
            this._lblPeriodHz.Name = "_lblPeriodHz";
            this._lblPeriodHz.Size = new System.Drawing.Size(90, 20);
            this._lblPeriodHz.TabIndex = 3;
            this._lblPeriodHz.Text = "≈ -- Hz";
            // 
            // _btnToggleLive
            // 
            this._btnToggleLive.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(50)))));
            this._btnToggleLive.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnToggleLive.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._btnToggleLive.ForeColor = System.Drawing.Color.White;
            this._btnToggleLive.Location = new System.Drawing.Point(8, 74);
            this._btnToggleLive.Name = "_btnToggleLive";
            this._btnToggleLive.Size = new System.Drawing.Size(200, 38);
            this._btnToggleLive.TabIndex = 4;
            this._btnToggleLive.Text = "▶ 라이브 시작";
            this._btnToggleLive.UseVisualStyleBackColor = false;
            this._btnToggleLive.Click += new System.EventHandler(this.OnToggleLiveClick);
            // 
            // _btnAllOn
            // 
            this._btnAllOn.BackColor = System.Drawing.Color.White;
            this._btnAllOn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnAllOn.Location = new System.Drawing.Point(8, 122);
            this._btnAllOn.Name = "_btnAllOn";
            this._btnAllOn.Size = new System.Drawing.Size(96, 32);
            this._btnAllOn.TabIndex = 5;
            this._btnAllOn.Text = "전체 ON";
            this._btnAllOn.UseVisualStyleBackColor = false;
            this._btnAllOn.Click += new System.EventHandler(this.OnAllOnClick);
            // 
            // _btnAllOff
            // 
            this._btnAllOff.BackColor = System.Drawing.Color.White;
            this._btnAllOff.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnAllOff.Location = new System.Drawing.Point(112, 122);
            this._btnAllOff.Name = "_btnAllOff";
            this._btnAllOff.Size = new System.Drawing.Size(96, 32);
            this._btnAllOff.TabIndex = 6;
            this._btnAllOff.Text = "전체 OFF";
            this._btnAllOff.UseVisualStyleBackColor = false;
            this._btnAllOff.Click += new System.EventHandler(this.OnAllOffClick);
            // 
            // _lblCount
            // 
            this._lblCount.Location = new System.Drawing.Point(8, 164);
            this._lblCount.Name = "_lblCount";
            this._lblCount.Size = new System.Drawing.Size(200, 20);
            this._lblCount.TabIndex = 7;
            this._lblCount.Text = "송신: 0회";
            // 
            // _lblLast
            // 
            this._lblLast.Location = new System.Drawing.Point(8, 186);
            this._lblLast.Name = "_lblLast";
            this._lblLast.Size = new System.Drawing.Size(200, 20);
            this._lblLast.TabIndex = 8;
            this._lblLast.Text = "마지막: --:--:--";
            // 
            // _lblStatus
            // 
            this._lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._lblStatus.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblStatus.Location = new System.Drawing.Point(8, 212);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(204, 40);
            this._lblStatus.TabIndex = 9;
            this._lblStatus.Text = "상태: 대기";
            // 
            // _lblCamInfo
            // 
            this._lblCamInfo.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this._lblCamInfo.ForeColor = System.Drawing.Color.Gray;
            this._lblCamInfo.Location = new System.Drawing.Point(8, 252);
            this._lblCamInfo.Name = "_lblCamInfo";
            this._lblCamInfo.Size = new System.Drawing.Size(420, 16);
            this._lblCamInfo.TabIndex = 10;
            this._lblCamInfo.Text = "카메라 라이브: Bottom 1.5 fps / 나머지 3~4 fps (검사별 자동)";
            // 
            // LightLiveTuningPanel
            // 
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this._lblHeader);
            this.Controls.Add(this._lblPeriod);
            this.Controls.Add(this._numPeriod);
            this.Controls.Add(this._lblPeriodHz);
            this.Controls.Add(this._btnToggleLive);
            this.Controls.Add(this._btnAllOn);
            this.Controls.Add(this._btnAllOff);
            this.Controls.Add(this._lblCount);
            this.Controls.Add(this._lblLast);
            this.Controls.Add(this._lblStatus);
            this.Controls.Add(this._lblCamInfo);
            this.Name = "LightLiveTuningPanel";
            this.Size = new System.Drawing.Size(244, 273);
            ((System.ComponentModel.ISupportInitialize)(this._numPeriod)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
