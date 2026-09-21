using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Localization;

namespace QMC.Common
{
    /// <summary>
    /// 대화상자 (Yse/No)
    /// </summary>
    public partial class MessageBoxYesNoRetry : Form, ILocalizedView
    {
        private string _originalTitle;

        private string[] m_ButtonText;

        /// <summary>
        /// 제목
        /// </summary>
        public string Title
        {
            get { return this.lblTitle.Text; }
            set { _originalTitle = value; this.lblTitle.Text = MessageDialog.TitleCaption(value); }
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
        public MessageBoxYesNoRetry()
        {
            InitializeComponent();
            _originalTitle = this.lblTitle.Text;
            //lblTitle.MouseMove += lblTitle_MouseDown;
            //lblTitle.MouseDown += lblTitle_MouseMove;

            this.m_ButtonText = new string[] { "Yes", "No", "Retry" };
            ApplyLanguage();
            lblTitle.MouseDown += (o, e) => { if (e.Button == MouseButtons.Left) { isMouseDown = true; mouseDownLocation = e.Location; } };
            lblTitle.MouseMove += (o, e) => { if (isMouseDown) Location = new Point(Location.X + (e.X - mouseDownLocation.X), Location.Y + (e.Y - mouseDownLocation.Y)); };
            lblTitle.MouseUp += (o, e) => { if (e.Button == MouseButtons.Left) { isMouseDown = false; mouseDownLocation = e.Location; } };

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
        public void ApplyLanguage()
        {
            this.lblTitle.Text = MessageDialog.TitleCaption(_originalTitle);
            this.buttonYes.Text = MessageDialog.ButtonCaption(m_ButtonText[0]);
            this.buttonNo.Text = MessageDialog.ButtonCaption(m_ButtonText[1]);
            this.buttonRetry.Text = MessageDialog.ButtonCaption(m_ButtonText[2]);
        }

        public DialogResult ShowDialog(IWin32Window owner = null, string[] buttonText = null)
        {
            if (buttonText != null && 2 < buttonText.Length)
            {
                this.m_ButtonText = (string[])buttonText.Clone();
                ApplyLanguage();
            }

            return base.ShowDialog(owner);
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
