using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Editors
{
    partial class ParameterEditorHost
    {
        private System.ComponentModel.IContainer components = null;

        private Label    _lblHeader;
        private Panel    _barPanel;
        private Label    _lblTool;
        private ComboBox _cbTool;
        private Panel    _content;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._lblHeader = new Label();
            this._barPanel = new Panel();
            this._lblTool = new Label();
            this._cbTool = new ComboBox();
            this._content = new Panel();
            this._barPanel.SuspendLayout();
            this.SuspendLayout();

            // _lblHeader
            this._lblHeader.Dock = DockStyle.Top;
            this._lblHeader.Height = 30;
            this._lblHeader.Text = "Inspection Parameter Editors";
            this._lblHeader.BackColor = UiTheme.StatusBarBg;
            this._lblHeader.ForeColor = Color.White;
            this._lblHeader.Font = UiTheme.SectionFont;
            this._lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            this._lblHeader.Padding = new Padding(10, 0, 0, 0);

            // _barPanel
            this._barPanel.Dock = DockStyle.Top;
            this._barPanel.Height = 40;
            this._barPanel.BackColor = Color.WhiteSmoke;

            // _lblTool
            this._lblTool.Location = new Point(10, 10);
            this._lblTool.AutoSize = true;
            this._lblTool.Text = "Tool:";
            this._lblTool.Font = UiTheme.ButtonFont;

            // _cbTool
            this._cbTool.Location = new Point(60, 6);
            this._cbTool.Size = new Size(280, 28);
            this._cbTool.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbTool.Font = UiTheme.ButtonFont;
            this._cbTool.SelectedIndexChanged += new System.EventHandler(this.OnToolChanged);

            this._barPanel.Controls.Add(this._lblTool);
            this._barPanel.Controls.Add(this._cbTool);

            // _content
            this._content.Dock = DockStyle.Fill;
            this._content.BackColor = UiTheme.MainBg;

            // ParameterEditorHost — 원본 추가 순서(헤더→바→content)
            this.Controls.Add(this._lblHeader);
            this.Controls.Add(this._barPanel);
            this.Controls.Add(this._content);
            this._barPanel.ResumeLayout(false);
            this._barPanel.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
