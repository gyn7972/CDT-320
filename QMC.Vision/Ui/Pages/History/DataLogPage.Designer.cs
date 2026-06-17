using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class DataLogPage
    {
        private System.ComponentModel.IContainer components = null;

        private Label      _hdr;
        private TabControl _tabs;
        private TabPage    _tpLog;
        private TabPage    _tpAlarm;
        private TabPage    _tpUtility;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._hdr = new Label();
            this._tabs = new TabControl();
            this._tpLog = new TabPage();
            this._tpAlarm = new TabPage();
            this._tpUtility = new TabPage();
            this._tabs.SuspendLayout();
            this.SuspendLayout();

            // _hdr
            this._hdr.Dock = DockStyle.Top;
            this._hdr.Height = 30;
            this._hdr.Text = "Data Log";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = Color.White;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // _tabs
            this._tabs.Dock = DockStyle.Fill;
            this._tabs.Font = UiTheme.ButtonFont;
            this._tpLog.Text = "Log";
            this._tpAlarm.Text = "Alarm";
            this._tpUtility.Text = "Utility";
            this._tabs.Controls.Add(this._tpLog);
            this._tabs.Controls.Add(this._tpAlarm);
            this._tabs.Controls.Add(this._tpUtility);

            // DataLogPage (원본 추가순서: 헤더→탭)
            this.Controls.Add(this._hdr);
            this.Controls.Add(this._tabs);
            this.Name = "DataLogPage";
            this._tabs.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
