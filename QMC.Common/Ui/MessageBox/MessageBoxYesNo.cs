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
    /// 대화상자 (Yes/No)
    /// </summary>
    public partial class MessageBoxYesNo : Form
    {
        private string[] m_ButtonText;

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
            get { return this.lblMessage.Text; }
            set { this.lblMessage.Text = value; }
        }

        /// <summary>
        /// 생성자
        /// </summary>

        private bool isMouseDown;
        private Point mouseDownLocation;
        public MessageBoxYesNo()
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.CenterScreen;
            //this.TopMost = true;

            //lblTitle.MouseMove += lblTitle_MouseDown;
            //lblTitle.MouseDown += lblTitle_MouseMove;

            this.m_ButtonText = new string[] { "Yes", "No", };
            lblTitle.MouseDown += (o, e) => { if (e.Button == MouseButtons.Left) { isMouseDown = true; mouseDownLocation = e.Location; } };
            lblTitle.MouseMove += (o, e) => { if (isMouseDown) Location = new Point(Location.X + (e.X - mouseDownLocation.X), Location.Y + (e.Y - mouseDownLocation.Y)); };
            lblTitle.MouseUp += (o, e) => { if (e.Button == MouseButtons.Left) { isMouseDown = false; mouseDownLocation = e.Location; } };


            button2.Focus();
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
        public DialogResult ShowDialog(IWin32Window owner = null, string[] buttonText = null)
        {
            if (buttonText != null && 1 < buttonText.Length)
            {
                ConfigureButtons(buttonText);
            }

            return base.ShowDialog(owner);
        }

        private void ConfigureButtons(string[] buttonText)
        {
            this.button1.Text = buttonText[0];
            this.button2.Text = buttonText[1];

            bool hasCancelButton = buttonText.Length > 2;
            this.button3.Visible = hasCancelButton;

            if (hasCancelButton)
            {
                this.button3.Text = buttonText[2];
                this.tableLayoutPanel3.SetColumn(this.button1, 2);
                this.tableLayoutPanel3.SetColumn(this.button2, 3);
                this.tableLayoutPanel3.SetColumn(this.button3, 4);
                this.CancelButton = this.button3;
            }
            else
            {
                this.tableLayoutPanel3.SetColumn(this.button1, 3);
                this.tableLayoutPanel3.SetColumn(this.button2, 4);
                this.CancelButton = this.button2;
            }
        }

        /// <summary>
        /// 대화상자 출력 (Modal)
        /// </summary>
        /// <param name="title">제목</param>
        /// <param name="message">본문</param>
        /// <returns></returns>
        public DialogResult ShowDialog(string title, string message, IWin32Window owner = null, string[] buttonText = null)
        {
            this.Title = title;
            this.Message = message;
            var dlgResult = this.ShowDialog(owner, buttonText);

            //Logger.Log(Logger.Module.Button, Logger.Type.Info, $"Dialog Result [{Title}]= {dlgResult}");
            return dlgResult;
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
