namespace QMC.CDT_320.Ui.Dialogs
{
    partial class LoginDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TableLayoutPanel contentLayout;
        private System.Windows.Forms.ListView lvAccounts;
        private System.Windows.Forms.ColumnHeader colId;
        private System.Windows.Forms.ColumnHeader colGrade;
        private System.Windows.Forms.ColumnHeader colPassword;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.TableLayoutPanel loginLayout;
        private System.Windows.Forms.Label lblId;
        private System.Windows.Forms.TextBox tbId;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.TableLayoutPanel loginButtonsLayout;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.Button btnEnter;
        private System.Windows.Forms.GroupBox grpAddUpdate;
        private System.Windows.Forms.TableLayoutPanel adminLayout;
        private System.Windows.Forms.Label lblAdminId;
        private System.Windows.Forms.TextBox tbAdminId;
        private System.Windows.Forms.Label lblAdminPassword;
        private System.Windows.Forms.TextBox tbAdminPassword;
        private System.Windows.Forms.Button btnMaintenance;
        private System.Windows.Forms.Button btnOperator;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lvAccounts = new System.Windows.Forms.ListView();
            this.colId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colGrade = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPassword = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.loginLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblId = new System.Windows.Forms.Label();
            this.tbId = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.tbPassword = new System.Windows.Forms.TextBox();
            this.loginButtonsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnLogout = new System.Windows.Forms.Button();
            this.btnEnter = new System.Windows.Forms.Button();
            this.grpAddUpdate = new System.Windows.Forms.GroupBox();
            this.adminLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblAdminId = new System.Windows.Forms.Label();
            this.tbAdminId = new System.Windows.Forms.TextBox();
            this.lblAdminPassword = new System.Windows.Forms.Label();
            this.tbAdminPassword = new System.Windows.Forms.TextBox();
            this.btnMaintenance = new System.Windows.Forms.Button();
            this.btnOperator = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.loginLayout.SuspendLayout();
            this.loginButtonsLayout.SuspendLayout();
            this.grpAddUpdate.SuspendLayout();
            this.adminLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblTitle, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(560, 400);
            this.rootLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(3, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(554, 36);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Tag = "i18n:dlg.login";
            this.lblTitle.Text = "LOGIN";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 272F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Controls.Add(this.lvAccounts, 0, 0);
            this.contentLayout.Controls.Add(this.rightLayout, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(3, 39);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(12, 14, 10, 14);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(554, 358);
            this.contentLayout.TabIndex = 1;
            // 
            // lvAccounts
            // 
            this.lvAccounts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colId,
            this.colGrade,
            this.colPassword});
            this.lvAccounts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvAccounts.FullRowSelect = true;
            this.lvAccounts.GridLines = true;
            this.lvAccounts.HideSelection = false;
            this.lvAccounts.Location = new System.Drawing.Point(12, 14);
            this.lvAccounts.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.lvAccounts.Name = "lvAccounts";
            this.lvAccounts.Size = new System.Drawing.Size(260, 330);
            this.lvAccounts.TabIndex = 0;
            this.lvAccounts.UseCompatibleStateImageBehavior = false;
            this.lvAccounts.View = System.Windows.Forms.View.Details;
            // 
            // colId
            // 
            this.colId.Text = "ID";
            this.colId.Width = 110;
            // 
            // colGrade
            // 
            this.colGrade.Text = "Grade";
            this.colGrade.Width = 100;
            // 
            // colPassword
            // 
            this.colPassword.Text = "Password";
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.loginLayout, 0, 0);
            this.rightLayout.Controls.Add(this.loginButtonsLayout, 0, 1);
            this.rightLayout.Controls.Add(this.grpAddUpdate, 0, 3);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(287, 17);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 4;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 86F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 14F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(254, 324);
            this.rightLayout.TabIndex = 1;
            // 
            // loginLayout
            // 
            this.loginLayout.ColumnCount = 2;
            this.loginLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.loginLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.loginLayout.Controls.Add(this.lblId, 0, 0);
            this.loginLayout.Controls.Add(this.tbId, 1, 0);
            this.loginLayout.Controls.Add(this.lblPassword, 0, 1);
            this.loginLayout.Controls.Add(this.tbPassword, 1, 1);
            this.loginLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.loginLayout.Location = new System.Drawing.Point(3, 3);
            this.loginLayout.Name = "loginLayout";
            this.loginLayout.RowCount = 2;
            this.loginLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.loginLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.loginLayout.Size = new System.Drawing.Size(248, 80);
            this.loginLayout.TabIndex = 0;
            // 
            // lblId
            // 
            this.lblId.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblId.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblId.Location = new System.Drawing.Point(3, 0);
            this.lblId.Name = "lblId";
            this.lblId.Size = new System.Drawing.Size(86, 40);
            this.lblId.TabIndex = 0;
            this.lblId.Text = "ID";
            this.lblId.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbId
            // 
            this.tbId.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbId.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tbId.Location = new System.Drawing.Point(95, 6);
            this.tbId.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.tbId.Name = "tbId";
            this.tbId.Size = new System.Drawing.Size(150, 25);
            this.tbId.TabIndex = 1;
            // 
            // lblPassword
            // 
            this.lblPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPassword.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPassword.Location = new System.Drawing.Point(3, 40);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(86, 40);
            this.lblPassword.TabIndex = 2;
            this.lblPassword.Text = "PASSWORD";
            this.lblPassword.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbPassword
            // 
            this.tbPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbPassword.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tbPassword.Location = new System.Drawing.Point(95, 46);
            this.tbPassword.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.tbPassword.Name = "tbPassword";
            this.tbPassword.Size = new System.Drawing.Size(150, 25);
            this.tbPassword.TabIndex = 3;
            this.tbPassword.UseSystemPasswordChar = true;
            // 
            // loginButtonsLayout
            // 
            this.loginButtonsLayout.ColumnCount = 2;
            this.loginButtonsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.loginButtonsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.loginButtonsLayout.Controls.Add(this.btnLogout, 0, 0);
            this.loginButtonsLayout.Controls.Add(this.btnEnter, 1, 0);
            this.loginButtonsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.loginButtonsLayout.Location = new System.Drawing.Point(3, 89);
            this.loginButtonsLayout.Name = "loginButtonsLayout";
            this.loginButtonsLayout.RowCount = 1;
            this.loginButtonsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.loginButtonsLayout.Size = new System.Drawing.Size(248, 46);
            this.loginButtonsLayout.TabIndex = 1;
            // 
            // btnLogout
            // 
            this.btnLogout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnLogout.Location = new System.Drawing.Point(0, 6);
            this.btnLogout.Margin = new System.Windows.Forms.Padding(0, 6, 6, 6);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(118, 34);
            this.btnLogout.TabIndex = 0;
            this.btnLogout.Text = "LOGOUT";
            // 
            // btnEnter
            // 
            this.btnEnter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnEnter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEnter.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnEnter.Location = new System.Drawing.Point(130, 6);
            this.btnEnter.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnEnter.Name = "btnEnter";
            this.btnEnter.Size = new System.Drawing.Size(118, 34);
            this.btnEnter.TabIndex = 1;
            this.btnEnter.Text = "ENTER";
            // 
            // grpAddUpdate
            // 
            this.grpAddUpdate.Controls.Add(this.adminLayout);
            this.grpAddUpdate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAddUpdate.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.grpAddUpdate.Location = new System.Drawing.Point(3, 155);
            this.grpAddUpdate.Name = "grpAddUpdate";
            this.grpAddUpdate.Size = new System.Drawing.Size(248, 166);
            this.grpAddUpdate.TabIndex = 2;
            this.grpAddUpdate.TabStop = false;
            this.grpAddUpdate.Tag = "level:Admin";
            this.grpAddUpdate.Text = "ADD & UPDATE";
            // 
            // adminLayout
            // 
            this.adminLayout.ColumnCount = 3;
            this.adminLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.adminLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.adminLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.adminLayout.Controls.Add(this.lblAdminId, 0, 0);
            this.adminLayout.Controls.Add(this.tbAdminId, 1, 0);
            this.adminLayout.Controls.Add(this.lblAdminPassword, 0, 1);
            this.adminLayout.Controls.Add(this.tbAdminPassword, 1, 1);
            this.adminLayout.Controls.Add(this.btnMaintenance, 0, 2);
            this.adminLayout.Controls.Add(this.btnOperator, 2, 2);
            this.adminLayout.Controls.Add(this.btnAdd, 0, 3);
            this.adminLayout.Controls.Add(this.btnUpdate, 1, 3);
            this.adminLayout.Controls.Add(this.btnDelete, 2, 3);
            this.adminLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.adminLayout.Location = new System.Drawing.Point(3, 21);
            this.adminLayout.Name = "adminLayout";
            this.adminLayout.Padding = new System.Windows.Forms.Padding(10, 12, 10, 10);
            this.adminLayout.RowCount = 4;
            this.adminLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.adminLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.adminLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.adminLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.adminLayout.Size = new System.Drawing.Size(242, 142);
            this.adminLayout.TabIndex = 0;
            // 
            // lblAdminId
            // 
            this.lblAdminId.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAdminId.Location = new System.Drawing.Point(13, 12);
            this.lblAdminId.Name = "lblAdminId";
            this.lblAdminId.Size = new System.Drawing.Size(66, 30);
            this.lblAdminId.TabIndex = 0;
            this.lblAdminId.Text = "ID";
            this.lblAdminId.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbAdminId
            // 
            this.adminLayout.SetColumnSpan(this.tbAdminId, 2);
            this.tbAdminId.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbAdminId.Location = new System.Drawing.Point(85, 15);
            this.tbAdminId.Name = "tbAdminId";
            this.tbAdminId.Size = new System.Drawing.Size(144, 25);
            this.tbAdminId.TabIndex = 1;
            // 
            // lblAdminPassword
            // 
            this.lblAdminPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAdminPassword.Location = new System.Drawing.Point(13, 42);
            this.lblAdminPassword.Name = "lblAdminPassword";
            this.lblAdminPassword.Size = new System.Drawing.Size(66, 30);
            this.lblAdminPassword.TabIndex = 2;
            this.lblAdminPassword.Text = "PASSWORD";
            this.lblAdminPassword.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbAdminPassword
            // 
            this.adminLayout.SetColumnSpan(this.tbAdminPassword, 2);
            this.tbAdminPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbAdminPassword.Location = new System.Drawing.Point(85, 45);
            this.tbAdminPassword.Name = "tbAdminPassword";
            this.tbAdminPassword.Size = new System.Drawing.Size(144, 25);
            this.tbAdminPassword.TabIndex = 3;
            this.tbAdminPassword.UseSystemPasswordChar = true;
            // 
            // btnMaintenance
            // 
            this.adminLayout.SetColumnSpan(this.btnMaintenance, 2);
            this.btnMaintenance.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMaintenance.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMaintenance.Location = new System.Drawing.Point(10, 76);
            this.btnMaintenance.Margin = new System.Windows.Forms.Padding(0, 4, 4, 4);
            this.btnMaintenance.Name = "btnMaintenance";
            this.btnMaintenance.Size = new System.Drawing.Size(143, 28);
            this.btnMaintenance.TabIndex = 4;
            this.btnMaintenance.Text = "Maintenance";
            // 
            // btnOperator
            // 
            this.btnOperator.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOperator.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOperator.Location = new System.Drawing.Point(161, 76);
            this.btnOperator.Margin = new System.Windows.Forms.Padding(4, 4, 0, 4);
            this.btnOperator.Name = "btnOperator";
            this.btnOperator.Size = new System.Drawing.Size(71, 28);
            this.btnOperator.TabIndex = 5;
            this.btnOperator.Text = "Operator";
            // 
            // btnAdd
            // 
            this.btnAdd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdd.Location = new System.Drawing.Point(10, 112);
            this.btnAdd.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(68, 30);
            this.btnAdd.TabIndex = 6;
            this.btnAdd.Text = "ADD";
            // 
            // btnUpdate
            // 
            this.btnUpdate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpdate.Location = new System.Drawing.Point(86, 112);
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(4, 4, 4, 0);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(67, 30);
            this.btnUpdate.TabIndex = 7;
            this.btnUpdate.Text = "UPDATE";
            // 
            // btnDelete
            // 
            this.btnDelete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDelete.Location = new System.Drawing.Point(161, 112);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(71, 30);
            this.btnDelete.TabIndex = 8;
            this.btnDelete.Text = "DELETE";
            // 
            // LoginDialog
            // 
            this.AcceptButton = this.btnEnter;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(560, 400);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LoginDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "LOGIN";
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.loginLayout.ResumeLayout(false);
            this.loginLayout.PerformLayout();
            this.loginButtonsLayout.ResumeLayout(false);
            this.grpAddUpdate.ResumeLayout(false);
            this.adminLayout.ResumeLayout(false);
            this.adminLayout.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
