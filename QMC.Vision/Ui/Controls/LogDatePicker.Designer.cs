using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    partial class LogDatePicker
    {
        private System.ComponentModel.IContainer components = null;

        private TextBox              _txt;       // 선택된 날짜 표시(읽기전용)
        private Button               _btnDrop;   // 달력 펼침 버튼
        private MonthCalendar        _calendar;  // 팝업 본체
        private ToolStripControlHost _calHost;   // MonthCalendar 를 드롭다운에 호스팅
        private ToolStripDropDown    _dropDown;  // 달력 팝업 컨테이너

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null) components.Dispose();
                if (_dropDown != null) _dropDown.Dispose();
                if (_calendar != null) _calendar.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._txt      = new TextBox();
            this._btnDrop  = new Button();
            this._calendar = new MonthCalendar();
            this._calHost  = new ToolStripControlHost(this._calendar);
            this._dropDown = new ToolStripDropDown();

            this.SuspendLayout();

            // _txt
            this._txt.Dock = DockStyle.Fill;
            this._txt.ReadOnly = true;
            this._txt.BorderStyle = BorderStyle.None;
            this._txt.BackColor = Color.White;
            this._txt.Cursor = Cursors.Hand;
            this._txt.TabStop = false;
            this._txt.Click += this._txt_Click;

            // _btnDrop
            this._btnDrop.Dock = DockStyle.Right;
            this._btnDrop.Width = 22;
            this._btnDrop.Text = "▼";
            this._btnDrop.FlatStyle = FlatStyle.System;
            this._btnDrop.TabStop = false;
            this._btnDrop.Click += this._btnDrop_Click;

            // _calendar
            this._calendar.MaxSelectionCount = 1;
            this._calendar.ShowToday = true;
            this._calendar.ShowTodayCircle = true;
            this._calendar.DateSelected += this._calendar_DateSelected;

            // _calHost
            this._calHost.Margin = Padding.Empty;
            this._calHost.Padding = Padding.Empty;
            this._calHost.AutoSize = true;

            // _dropDown
            this._dropDown.AutoSize = true;
            this._dropDown.Padding = Padding.Empty;
            this._dropDown.DropShadowEnabled = true;
            this._dropDown.Items.Add(this._calHost);

            // LogDatePicker
            this.Controls.Add(this._txt);
            this.Controls.Add(this._btnDrop);
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = Color.White;
            this.Size = new Size(130, 23);
            this.Padding = new Padding(4, 3, 0, 0);
            this.Name = "LogDatePicker";

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
