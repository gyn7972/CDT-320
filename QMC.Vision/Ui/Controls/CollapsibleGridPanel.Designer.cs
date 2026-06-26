using System;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    partial class CollapsibleGridPanel
    {
        private System.ComponentModel.IContainer components = null;

        private Panel panelHeader;
        private Label lblTitle;
        private Label lblArrow;
        private Panel panelAccent;
        private Panel panelBody;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblArrow = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panelAccent = new System.Windows.Forms.Panel();
            this.panelBody = new System.Windows.Forms.Panel();
            this.panelHeader.SuspendLayout();
            this.SuspendLayout();
            //
            // panelHeader
            //
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(234)))), ((int)(((byte)(237)))));
            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.lblArrow);
            this.panelHeader.Controls.Add(this.panelAccent);
            this.panelHeader.Cursor = System.Windows.Forms.Cursors.Hand;
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(400, 30);
            this.panelHeader.TabIndex = 0;
            this.panelHeader.Click += new System.EventHandler(this.panelHeader_Click);
            //
            // lblArrow
            //
            this.lblArrow.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblArrow.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblArrow.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblArrow.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(52)))), ((int)(((byte)(58)))));
            this.lblArrow.Location = new System.Drawing.Point(372, 0);
            this.lblArrow.Name = "lblArrow";
            this.lblArrow.Size = new System.Drawing.Size(28, 30);
            this.lblArrow.TabIndex = 1;
            this.lblArrow.Text = "▼";
            this.lblArrow.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblArrow.Click += new System.EventHandler(this.panelHeader_Click);
            //
            // lblTitle
            //
            this.lblTitle.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(52)))), ((int)(((byte)(58)))));
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(372, 30);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTitle.Click += new System.EventHandler(this.panelHeader_Click);
            //
            // panelAccent
            //
            this.panelAccent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.panelAccent.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelAccent.Location = new System.Drawing.Point(0, 28);
            this.panelAccent.Margin = new System.Windows.Forms.Padding(0);
            this.panelAccent.Name = "panelAccent";
            this.panelAccent.Size = new System.Drawing.Size(400, 2);
            this.panelAccent.TabIndex = 2;
            //
            // panelBody
            //
            this.panelBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBody.Location = new System.Drawing.Point(0, 30);
            this.panelBody.Margin = new System.Windows.Forms.Padding(0);
            this.panelBody.Name = "panelBody";
            this.panelBody.Size = new System.Drawing.Size(400, 276);
            this.panelBody.TabIndex = 1;
            //
            // CollapsibleGridPanel
            //
            this.Controls.Add(this.panelBody);
            this.Controls.Add(this.panelHeader);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "CollapsibleGridPanel";
            this.Size = new System.Drawing.Size(400, 300);
            this.panelHeader.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
