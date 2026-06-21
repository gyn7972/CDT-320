namespace QMC.CDT_320.Ui.Dialogs
{
    partial class ModeOverlayDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Panel _topPanel;
        private System.Windows.Forms.Label _titleLabel;
        private System.Windows.Forms.TableLayoutPanel _actionArea;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this._topPanel = new System.Windows.Forms.Panel();
            this._titleLabel = new System.Windows.Forms.Label();
            this._actionArea = new System.Windows.Forms.TableLayoutPanel();
            this.rootLayout.SuspendLayout();
            this._topPanel.SuspendLayout();
            this.SuspendLayout();
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this._topPanel, 0, 0);
            this.rootLayout.Controls.Add(this._actionArea, 0, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._topPanel.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this._topPanel.Controls.Add(this._titleLabel);
            this._topPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._titleLabel.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this._titleLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._titleLabel.Font = new System.Drawing.Font("맑은 고딕", 44F, System.Drawing.FontStyle.Bold);
            this._titleLabel.ForeColor = System.Drawing.Color.White;
            this._titleLabel.Text = "MODE";
            this._titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._actionArea.BackColor = System.Drawing.Color.FromArgb(187, 187, 187);
            this._actionArea.ColumnCount = 3;
            this._actionArea.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._actionArea.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._actionArea.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this._actionArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this._actionArea.Padding = new System.Windows.Forms.Padding(20, 16, 20, 16);
            this._actionArea.RowCount = 3;
            this._actionArea.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._actionArea.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._actionArea.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(187, 187, 187);
            this.ClientSize = new System.Drawing.Size(560, 420);
            this.Controls.Add(this.rootLayout);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ModeOverlayDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MODE";
            this.rootLayout.ResumeLayout(false);
            this._topPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
