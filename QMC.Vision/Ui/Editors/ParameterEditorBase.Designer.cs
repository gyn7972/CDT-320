using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Editors
{
    partial class ParameterEditorBase
    {
        private System.ComponentModel.IContainer components = null;

        private Label  _lblHeader;
        private Panel  _barPanel;
        private Button _btnReload;
        private Button _btnSave;
        protected Label _lblPath;      // 자식/베이스 공용 (경로 표시)
        protected Panel _editorPanel;  // 자식 BuildEditor 가 동적 입력을 채움

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // 직렬화 가능 shell. 동적 텍스트(_lblHeader/_lblPath)는 생성자에서 toolName 기준으로 설정.
        private void InitializeComponent()
        {
            this._lblHeader = new Label();
            this._barPanel = new Panel();
            this._btnReload = new Button();
            this._btnSave = new Button();
            this._lblPath = new Label();
            this._editorPanel = new Panel();
            this._barPanel.SuspendLayout();
            this.SuspendLayout();

            // _lblHeader
            this._lblHeader.Dock = DockStyle.Top;
            this._lblHeader.Height = 30;
            this._lblHeader.BackColor = UiTheme.StatusBarBg;
            this._lblHeader.ForeColor = Color.White;
            this._lblHeader.Font = UiTheme.SectionFont;
            this._lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            this._lblHeader.Padding = new Padding(10, 0, 0, 0);
            this._lblHeader.Text = "Parameters";

            // _barPanel
            this._barPanel.Dock = DockStyle.Top;
            this._barPanel.Height = 40;
            this._barPanel.BackColor = Color.WhiteSmoke;

            // _btnReload
            this._btnReload.Location = new Point(8, 4);
            this._btnReload.Size = new Size(100, 32);
            this._btnReload.Text = "Reload";
            this._btnReload.FlatStyle = FlatStyle.Flat;
            this._btnReload.Font = UiTheme.ButtonFont;
            this._btnReload.BackColor = Color.White;
            this._btnReload.Click += new System.EventHandler(this.OnReloadClick);

            // _btnSave
            this._btnSave.Location = new Point(116, 4);
            this._btnSave.Size = new Size(120, 32);
            this._btnSave.Text = "SAVE";
            this._btnSave.FlatStyle = FlatStyle.Flat;
            this._btnSave.Font = UiTheme.ButtonFont;
            this._btnSave.BackColor = UiTheme.Accent;
            this._btnSave.ForeColor = Color.White;
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);

            // _lblPath
            this._lblPath.Location = new Point(244, 10);
            this._lblPath.Size = new Size(640, 24);
            this._lblPath.Font = UiTheme.ValueFont;
            this._lblPath.ForeColor = Color.DarkSlateGray;
            this._lblPath.TextAlign = ContentAlignment.MiddleLeft;

            this._barPanel.Controls.Add(this._btnReload);
            this._barPanel.Controls.Add(this._btnSave);
            this._barPanel.Controls.Add(this._lblPath);

            // _editorPanel
            this._editorPanel.Dock = DockStyle.Fill;
            this._editorPanel.BackColor = UiTheme.MainBg;
            this._editorPanel.AutoScroll = true;
            this._editorPanel.Padding = new Padding(10);

            // ParameterEditorBase — 원본 BuildShell 과 동일 추가 순서(헤더→바→편집패널)
            this.BackColor = UiTheme.MainBg;
            this.Controls.Add(this._lblHeader);
            this.Controls.Add(this._barPanel);
            this.Controls.Add(this._editorPanel);
            this._barPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
