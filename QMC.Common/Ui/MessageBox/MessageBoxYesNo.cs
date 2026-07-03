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
        private Label _buttonGroupLabel;
        private string _buttonGroupLabelText = string.Empty;

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

        public string ButtonGroupLabel
        {
            get { return _buttonGroupLabelText; }
            set
            {
                _buttonGroupLabelText = value ?? string.Empty;
                ConfigureButtonGroupLabel();
            }
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

            if (!string.IsNullOrWhiteSpace(_buttonGroupLabelText))
            {
                this.tableLayoutPanel3.SetRow(this.button1, 1);
                this.tableLayoutPanel3.SetRow(this.button2, 1);
                this.tableLayoutPanel3.SetRow(this.button3, 1);
            }
        }

        private void ConfigureButtonGroupLabel()
        {
            if (string.IsNullOrWhiteSpace(_buttonGroupLabelText))
                return;

            if (_buttonGroupLabel == null)
            {
                _buttonGroupLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Tahoma", 10.2F, FontStyle.Bold),
                    Margin = new Padding(0),
                    TextAlign = ContentAlignment.MiddleCenter
                };
            }

            _buttonGroupLabel.Text = _buttonGroupLabelText;

            tableLayoutPanel1.RowStyles[1].Height = 52F;
            tableLayoutPanel1.RowStyles[2].Height = 33F;

            tableLayoutPanel3.SuspendLayout();
            try
            {
                tableLayoutPanel3.RowStyles.Clear();
                tableLayoutPanel3.RowCount = 2;
                tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
                tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));

                tableLayoutPanel3.SetRow(button1, 1);
                tableLayoutPanel3.SetRow(button2, 1);
                tableLayoutPanel3.SetRow(button3, 1);

                if (!tableLayoutPanel3.Controls.Contains(_buttonGroupLabel))
                    tableLayoutPanel3.Controls.Add(_buttonGroupLabel, 2, 0);

                tableLayoutPanel3.SetColumn(_buttonGroupLabel, 2);
                tableLayoutPanel3.SetColumnSpan(_buttonGroupLabel, 3);
            }
            finally
            {
                tableLayoutPanel3.ResumeLayout(true);
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
