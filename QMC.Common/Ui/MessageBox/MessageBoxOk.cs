using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.Common
{
    /// <summary>
    /// 대화상자 (Ok)
    /// </summary>
    public partial class MessageBoxOk : Form
    {
        public enum MessageStatus
        {
            error,
            warning,
            notification
        }

        /// <summary>
        /// 제목
        /// </summary>
        public string Title
        {
            get { return this.lblTitle.Text; }
            set { this.lblTitle.Text = value; }
        }
        /// <summary>
        /// 본문
        /// </summary>
        public string Message
        {
            get { return this.txtMessage.Text; }
            set { this.txtMessage.Text = value; }
        }
        /// <summary>
        /// 기본값 Color.Control
        /// </summary>
        public Color TitleBgColor
        {
            get { return this.panel1.BackColor; }
            set { this.panel1.BackColor = value; }
        }
        /// <summary>
        /// 기본값 Color.ControlText
        /// </summary>
        public Color TitleFgColor
        {
            get { return this.lblTitle.ForeColor; }
            set { this.lblTitle.ForeColor = value; }
        }

        int closedSecs;
        int secs;

        private bool isMouseDown;
        private Point mouseDownLocation;

        /// <summary>
        /// 생성자
        /// </summary>
        public MessageBoxOk()
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            //this.TopMost = true;

            // Label이 포커스를 받지 않도록 탭 정지 비활성화
            this.lblTitle.TabStop = false;

            lblTitle.MouseDown += (o, e) => { if (e.Button == MouseButtons.Left) { isMouseDown = true; mouseDownLocation = e.Location; } };
            lblTitle.MouseMove += (o, e) => { if (isMouseDown) Location = new Point(Location.X + (e.X - mouseDownLocation.X), Location.Y + (e.Y - mouseDownLocation.Y)); };
            lblTitle.MouseUp += (o, e) => { if (e.Button == MouseButtons.Left) { isMouseDown = false; mouseDownLocation = e.Location; } };
        }

        /// <summary>
        /// 폼이 표시될 때 Ok 버튼에 포커스 설정
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // ActiveControl 및 Focus를 명시적으로 설정
            this.ActiveControl = this.btnOk;
            this.btnOk.Focus();
        }

        /// <summary>
        /// Drop Shadow (그림자) 효과
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        /// <summary>
        /// 대화상자 출력 (Modal)
        /// </summary>
        /// <returns></returns>
        public new DialogResult ShowDialog()
        {
            return base.ShowDialog();
        }

        /// <summary>
        /// 대화상자 출력 (Modal)
        /// </summary>
        /// <param name="title">제목</param>
        /// <param name="message">본문</param>
        /// <param name="closedSecs">자동 닫기 시간 (초)</param>
        /// <returns></returns>
        public DialogResult ShowDialog(string title, string message, int closedSecs=0)
        {
            this.Title = title;
            this.Message = message;
            this.closedSecs = closedSecs;

            // === Title에 따라 이미지 변경 ===
            SetImageByTitle(title);

            if (this.closedSecs > 0)
            {
                this.btnOk.Text = $"&Ok ({closedSecs})";
                this.secs = 0;
                var timer = new Timer();
                timer.Tick += Timer_Tick;
                timer.Interval = 1000;
                timer.Enabled = true;
            }
            else
            {
                this.btnOk.Text = $"&Ok";
            }

            return this.ShowDialog();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            this.secs++;
            this.btnOk.Text = $"&Ok ({this.closedSecs - this.secs})";
            if (this.secs >= this.closedSecs)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        /// <summary>
        /// Title에 따라 이미지 설정
        /// </summary>
        /// <param name="title">제목</param>
        private void SetImageByTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                // 기본 이미지 (현재 설정된 megaphone)
                this.pictureBox1.Image = SystemIcons.Information.ToBitmap();
                this.panel1.BackColor = Color.DarkOrange;
                return;
            }

            string titleLower = title.ToLower();

            if (titleLower.Contains("error"))
            {
                // 에러 이미지
                this.pictureBox1.Image = SystemIcons.Error.ToBitmap();
                this.panel1.BackColor = Color.Crimson;
                this.lblTitle.ForeColor = Color.White;
            }
            else if (titleLower.Contains("warning"))
            {
                // 경고 이미지
                this.pictureBox1.Image = SystemIcons.Warning.ToBitmap();
                this.panel1.BackColor = Color.Orange;
                this.lblTitle.ForeColor = Color.Black;
            }
            else if (titleLower.Contains("notification"))
            {
                // 알림 이미지
                this.pictureBox1.Image = SystemIcons.Information.ToBitmap();
                this.panel1.BackColor = Color.DodgerBlue;
                this.lblTitle.ForeColor = Color.White;
            }
            else
            {
                // 기본 이미지 (megaphone)
                this.pictureBox1.Image = SystemIcons.Information.ToBitmap();
                this.panel1.BackColor = Color.DarkOrange;
                this.lblTitle.ForeColor = Color.Black;
            }
        }

        #region 마우스로 폼 드래그
        //private Point mouseDownLocation;
        //private void lblTitle_MouseDown(object sender, MouseEventArgs e)
        //{
        //    if (e.Button == System.Windows.Forms.MouseButtons.Left)
        //    {
        //        this.mouseDownLocation = e.Location;
        //    }
        //}
        //private void lblTitle_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if (e.Button == System.Windows.Forms.MouseButtons.Left)
        //    {
        //        this.Left = e.X + this.Left - this.mouseDownLocation.X;
        //        this.Top = e.Y + this.Top - this.mouseDownLocation.Y;
        //    }
        //}
        #endregion
    }
}
