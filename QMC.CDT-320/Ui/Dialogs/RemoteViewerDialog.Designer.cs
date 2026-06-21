namespace QMC.CDT_320.Ui.Dialogs
{
    partial class RemoteViewerDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TableLayoutPanel controlLayout;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.NumericUpDown _nPort;
        private System.Windows.Forms.Button _btnStart;
        private System.Windows.Forms.Button _btnStop;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.Label _lblClients;
        private System.Windows.Forms.PictureBox _preview;
        private System.Windows.Forms.TableLayoutPanel bottomLayout;
        private System.Windows.Forms.Button _btnClose;
        private System.Windows.Forms.Timer _previewTimer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.controlLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblPort = new System.Windows.Forms.Label();
            this._nPort = new System.Windows.Forms.NumericUpDown();
            this._btnStart = new System.Windows.Forms.Button();
            this._btnStop = new System.Windows.Forms.Button();
            this._lblStatus = new System.Windows.Forms.Label();
            this._lblClients = new System.Windows.Forms.Label();
            this._preview = new System.Windows.Forms.PictureBox();
            this.bottomLayout = new System.Windows.Forms.TableLayoutPanel();
            this._btnClose = new System.Windows.Forms.Button();
            this._previewTimer = new System.Windows.Forms.Timer(this.components);
            this.rootLayout.SuspendLayout();
            this.controlLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._preview)).BeginInit();
            this.bottomLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblTitle, 0, 0);
            this.rootLayout.Controls.Add(this.controlLayout, 0, 1);
            this.rootLayout.Controls.Add(this._lblClients, 0, 2);
            this.rootLayout.Controls.Add(this._preview, 0, 3);
            this.rootLayout.Controls.Add(this.bottomLayout, 0, 4);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 5;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.rootLayout.Size = new System.Drawing.Size(680, 600);
            this.rootLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(3, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(674, 36);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "REMOTE VIEWER";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // controlLayout
            // 
            this.controlLayout.BackColor = System.Drawing.Color.Gainsboro;
            this.controlLayout.ColumnCount = 7;
            this.controlLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.controlLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.controlLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.controlLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.controlLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.controlLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.controlLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.controlLayout.Controls.Add(this.lblPort, 0, 0);
            this.controlLayout.Controls.Add(this._nPort, 1, 0);
            this.controlLayout.Controls.Add(this._btnStart, 2, 0);
            this.controlLayout.Controls.Add(this._btnStop, 3, 0);
            this.controlLayout.Controls.Add(this._lblStatus, 5, 0);
            this.controlLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.controlLayout.Location = new System.Drawing.Point(3, 39);
            this.controlLayout.Name = "controlLayout";
            this.controlLayout.Padding = new System.Windows.Forms.Padding(10, 14, 0, 14);
            this.controlLayout.RowCount = 1;
            this.controlLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.controlLayout.Size = new System.Drawing.Size(674, 54);
            this.controlLayout.TabIndex = 1;
            // 
            // lblPort
            // 
            this.lblPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPort.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblPort.Location = new System.Drawing.Point(13, 14);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(46, 26);
            this.lblPort.TabIndex = 0;
            this.lblPort.Text = "Port:";
            this.lblPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nPort
            // 
            this._nPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPort.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPort.Location = new System.Drawing.Point(65, 17);
            this._nPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this._nPort.Minimum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this._nPort.Name = "_nPort";
            this._nPort.Size = new System.Drawing.Size(104, 27);
            this._nPort.TabIndex = 1;
            this._nPort.Value = new decimal(new int[] {
            5099,
            0,
            0,
            0});
            // 
            // _btnStart
            // 
            this._btnStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(120)))), ((int)(((byte)(210)))));
            this._btnStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnStart.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._btnStart.ForeColor = System.Drawing.Color.White;
            this._btnStart.Location = new System.Drawing.Point(180, 14);
            this._btnStart.Margin = new System.Windows.Forms.Padding(8, 0, 4, 0);
            this._btnStart.Name = "_btnStart";
            this._btnStart.Size = new System.Drawing.Size(98, 26);
            this._btnStart.TabIndex = 2;
            this._btnStart.Text = "Start";
            this._btnStart.UseVisualStyleBackColor = false;
            // 
            // _btnStop
            // 
            this._btnStop.BackColor = System.Drawing.Color.White;
            this._btnStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnStop.Enabled = false;
            this._btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnStop.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._btnStop.Location = new System.Drawing.Point(286, 14);
            this._btnStop.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this._btnStop.Name = "_btnStop";
            this._btnStop.Size = new System.Drawing.Size(102, 26);
            this._btnStop.TabIndex = 3;
            this._btnStop.Text = "Stop";
            this._btnStop.UseVisualStyleBackColor = false;
            // 
            // _lblStatus
            // 
            this._lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblStatus.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblStatus.ForeColor = System.Drawing.Color.DimGray;
            this._lblStatus.Location = new System.Drawing.Point(405, 14);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(256, 26);
            this._lblStatus.TabIndex = 4;
            this._lblStatus.Text = "stopped";
            this._lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblClients
            // 
            this._lblClients.BackColor = System.Drawing.Color.WhiteSmoke;
            this._lblClients.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblClients.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblClients.Location = new System.Drawing.Point(3, 96);
            this._lblClients.Name = "_lblClients";
            this._lblClients.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._lblClients.Size = new System.Drawing.Size(674, 28);
            this._lblClients.TabIndex = 2;
            this._lblClients.Text = "Connected viewers: 0";
            this._lblClients.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _preview
            // 
            this._preview.BackColor = System.Drawing.Color.Black;
            this._preview.Dock = System.Windows.Forms.DockStyle.Fill;
            this._preview.Location = new System.Drawing.Point(3, 127);
            this._preview.Name = "_preview";
            this._preview.Size = new System.Drawing.Size(674, 426);
            this._preview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this._preview.TabIndex = 3;
            this._preview.TabStop = false;
            // 
            // bottomLayout
            // 
            this.bottomLayout.ColumnCount = 3;
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.bottomLayout.Controls.Add(this._btnClose, 1, 0);
            this.bottomLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomLayout.Location = new System.Drawing.Point(3, 559);
            this.bottomLayout.Name = "bottomLayout";
            this.bottomLayout.Padding = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.bottomLayout.RowCount = 1;
            this.bottomLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomLayout.Size = new System.Drawing.Size(674, 38);
            this.bottomLayout.TabIndex = 4;
            // 
            // _btnClose
            // 
            this._btnClose.BackColor = System.Drawing.Color.White;
            this._btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClose.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._btnClose.Location = new System.Drawing.Point(559, 9);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(102, 20);
            this._btnClose.TabIndex = 0;
            this._btnClose.Text = "Close";
            this._btnClose.UseVisualStyleBackColor = false;
            // 
            // RemoteViewerDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(680, 600);
            this.Controls.Add(this.rootLayout);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RemoteViewerDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Remote Viewer";
            this.rootLayout.ResumeLayout(false);
            this.controlLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._preview)).EndInit();
            this.bottomLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
