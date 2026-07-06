using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class ModuleSubsetPage
    {
        private TableLayoutPanel editorLayout;
        private GroupBox grpPickPlace;
        private TableLayoutPanel tlpPickPlace;
        private Label lblPickRetry;
        private NumericUpDown _nPickRetry;
        private Label lblPickDelay;
        private NumericUpDown _nPickDelay;
        private Label lblPlaceDelay;
        private NumericUpDown _nPlaceDelay;
        private GroupBox grpCollet;
        private TableLayoutPanel tlpCollet;
        private Label lblColletEnable;
        private CheckBox _cbColletEnable;
        private Label lblColletInterval;
        private NumericUpDown _nColletInterval;
        private GroupBox grpInspection;
        private TableLayoutPanel tlpInspection;
        private Label lblBottomInspection;
        private CheckBox _cbBottomInspect;
        private Label lblPlacementInspection;
        private CheckBox _cbPlacementInspect;

        private void InitializeComponent()
        {
            this.editorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpPickPlace = new System.Windows.Forms.GroupBox();
            this.tlpPickPlace = new System.Windows.Forms.TableLayoutPanel();
            this.lblPickRetry = new System.Windows.Forms.Label();
            this._nPickRetry = new System.Windows.Forms.NumericUpDown();
            this.lblPickDelay = new System.Windows.Forms.Label();
            this._nPickDelay = new System.Windows.Forms.NumericUpDown();
            this.lblPlaceDelay = new System.Windows.Forms.Label();
            this._nPlaceDelay = new System.Windows.Forms.NumericUpDown();
            this.grpCollet = new System.Windows.Forms.GroupBox();
            this.tlpCollet = new System.Windows.Forms.TableLayoutPanel();
            this.lblColletEnable = new System.Windows.Forms.Label();
            this._cbColletEnable = new System.Windows.Forms.CheckBox();
            this.lblColletInterval = new System.Windows.Forms.Label();
            this._nColletInterval = new System.Windows.Forms.NumericUpDown();
            this.grpInspection = new System.Windows.Forms.GroupBox();
            this.tlpInspection = new System.Windows.Forms.TableLayoutPanel();
            this.lblBottomInspection = new System.Windows.Forms.Label();
            this._cbBottomInspect = new System.Windows.Forms.CheckBox();
            this.lblPlacementInspection = new System.Windows.Forms.Label();
            this._cbPlacementInspect = new System.Windows.Forms.CheckBox();
            this._editorPanel.SuspendLayout();
            this.editorLayout.SuspendLayout();
            this.grpPickPlace.SuspendLayout();
            this.tlpPickPlace.SuspendLayout();
            this.grpCollet.SuspendLayout();
            this.tlpCollet.SuspendLayout();
            this.grpInspection.SuspendLayout();
            this.tlpInspection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nPickRetry)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPickDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPlaceDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nColletInterval)).BeginInit();
            this.SuspendLayout();
            //
            // _editorPanel
            //
            this._editorPanel.BackColor = System.Drawing.Color.White;
            this._editorPanel.Controls.Add(this.editorLayout);
            this._editorPanel.Size = new System.Drawing.Size(1094, 676);
            //
            // _lblProject
            //
            this._lblProject.Size = new System.Drawing.Size(794, 36);
            //
            // editorLayout  (좌측 50%만 사용, 그룹박스 세로 배치 + 균등 여백)
            //
            this.editorLayout.BackColor = System.Drawing.Color.White;
            this.editorLayout.ColumnCount = 2;
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.Controls.Add(this.grpPickPlace, 0, 0);
            this.editorLayout.Controls.Add(this.grpCollet, 0, 2);
            this.editorLayout.Controls.Add(this.grpInspection, 0, 4);
            this.editorLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editorLayout.Location = new System.Drawing.Point(8, 12);
            this.editorLayout.Margin = new System.Windows.Forms.Padding(0);
            this.editorLayout.Name = "editorLayout";
            this.editorLayout.Padding = new System.Windows.Forms.Padding(0);
            this.editorLayout.RowCount = 5;
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 106F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 106F));
            this.editorLayout.Size = new System.Drawing.Size(1078, 656);
            this.editorLayout.TabIndex = 0;
            //
            // grpPickPlace
            //
            this.grpPickPlace.BackColor = System.Drawing.Color.White;
            this.grpPickPlace.Controls.Add(this.tlpPickPlace);
            this.grpPickPlace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPickPlace.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpPickPlace.Margin = new System.Windows.Forms.Padding(0);
            this.grpPickPlace.Name = "grpPickPlace";
            this.grpPickPlace.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpPickPlace.TabIndex = 0;
            this.grpPickPlace.TabStop = false;
            this.grpPickPlace.Text = "Pick / place parameters";
            //
            // tlpPickPlace
            //
            this.tlpPickPlace.BackColor = System.Drawing.Color.White;
            this.tlpPickPlace.ColumnCount = 2;
            this.tlpPickPlace.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpPickPlace.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpPickPlace.Controls.Add(this.lblPickRetry, 0, 0);
            this.tlpPickPlace.Controls.Add(this._nPickRetry, 1, 0);
            this.tlpPickPlace.Controls.Add(this.lblPickDelay, 0, 1);
            this.tlpPickPlace.Controls.Add(this._nPickDelay, 1, 1);
            this.tlpPickPlace.Controls.Add(this.lblPlaceDelay, 0, 2);
            this.tlpPickPlace.Controls.Add(this._nPlaceDelay, 1, 2);
            this.tlpPickPlace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpPickPlace.Margin = new System.Windows.Forms.Padding(0);
            this.tlpPickPlace.Name = "tlpPickPlace";
            this.tlpPickPlace.RowCount = 4;
            this.tlpPickPlace.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpPickPlace.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpPickPlace.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpPickPlace.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpPickPlace.TabIndex = 0;
            //
            // lblPickRetry
            //
            this.lblPickRetry.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickRetry.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPickRetry.Name = "lblPickRetry";
            this.lblPickRetry.TabIndex = 1;
            this.lblPickRetry.Text = "Pick retry count";
            this.lblPickRetry.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nPickRetry
            //
            this._nPickRetry.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPickRetry.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPickRetry.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._nPickRetry.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this._nPickRetry.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nPickRetry.Name = "_nPickRetry";
            this._nPickRetry.TabIndex = 2;
            this._nPickRetry.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            //
            // lblPickDelay
            //
            this.lblPickDelay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickDelay.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPickDelay.Name = "lblPickDelay";
            this.lblPickDelay.TabIndex = 3;
            this.lblPickDelay.Text = "Pick delay (ms)";
            this.lblPickDelay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nPickDelay
            //
            this._nPickDelay.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPickDelay.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPickDelay.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._nPickDelay.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nPickDelay.Name = "_nPickDelay";
            this._nPickDelay.TabIndex = 4;
            //
            // lblPlaceDelay
            //
            this.lblPlaceDelay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceDelay.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPlaceDelay.Name = "lblPlaceDelay";
            this.lblPlaceDelay.TabIndex = 5;
            this.lblPlaceDelay.Text = "Place delay (ms)";
            this.lblPlaceDelay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nPlaceDelay
            //
            this._nPlaceDelay.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPlaceDelay.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPlaceDelay.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._nPlaceDelay.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nPlaceDelay.Name = "_nPlaceDelay";
            this._nPlaceDelay.TabIndex = 6;
            //
            // grpCollet
            //
            this.grpCollet.BackColor = System.Drawing.Color.White;
            this.grpCollet.Controls.Add(this.tlpCollet);
            this.grpCollet.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCollet.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpCollet.Margin = new System.Windows.Forms.Padding(0);
            this.grpCollet.Name = "grpCollet";
            this.grpCollet.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpCollet.TabIndex = 1;
            this.grpCollet.TabStop = false;
            this.grpCollet.Text = "Collet cleaning";
            //
            // tlpCollet
            //
            this.tlpCollet.BackColor = System.Drawing.Color.White;
            this.tlpCollet.ColumnCount = 2;
            this.tlpCollet.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpCollet.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpCollet.Controls.Add(this.lblColletEnable, 0, 0);
            this.tlpCollet.Controls.Add(this._cbColletEnable, 1, 0);
            this.tlpCollet.Controls.Add(this.lblColletInterval, 0, 1);
            this.tlpCollet.Controls.Add(this._nColletInterval, 1, 1);
            this.tlpCollet.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpCollet.Margin = new System.Windows.Forms.Padding(0);
            this.tlpCollet.Name = "tlpCollet";
            this.tlpCollet.RowCount = 3;
            this.tlpCollet.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpCollet.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpCollet.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpCollet.TabIndex = 0;
            //
            // lblColletEnable
            //
            this.lblColletEnable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletEnable.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblColletEnable.Name = "lblColletEnable";
            this.lblColletEnable.TabIndex = 8;
            this.lblColletEnable.Text = "Collet cleaning";
            this.lblColletEnable.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _cbColletEnable
            //
            this._cbColletEnable.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbColletEnable.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbColletEnable.Name = "_cbColletEnable";
            this._cbColletEnable.TabIndex = 9;
            this._cbColletEnable.Text = "Collet cleaning enable";
            //
            // lblColletInterval
            //
            this.lblColletInterval.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletInterval.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblColletInterval.Name = "lblColletInterval";
            this.lblColletInterval.TabIndex = 10;
            this.lblColletInterval.Text = "Cleaning interval";
            this.lblColletInterval.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nColletInterval
            //
            this._nColletInterval.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nColletInterval.Font = new System.Drawing.Font("Consolas", 10F);
            this._nColletInterval.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._nColletInterval.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this._nColletInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nColletInterval.Name = "_nColletInterval";
            this._nColletInterval.TabIndex = 11;
            this._nColletInterval.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            //
            // grpInspection
            //
            this.grpInspection.BackColor = System.Drawing.Color.White;
            this.grpInspection.Controls.Add(this.tlpInspection);
            this.grpInspection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInspection.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpInspection.Margin = new System.Windows.Forms.Padding(0);
            this.grpInspection.Name = "grpInspection";
            this.grpInspection.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpInspection.TabIndex = 2;
            this.grpInspection.TabStop = false;
            this.grpInspection.Text = "Inspection enable";
            //
            // tlpInspection
            //
            this.tlpInspection.BackColor = System.Drawing.Color.White;
            this.tlpInspection.ColumnCount = 2;
            this.tlpInspection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpInspection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpInspection.Controls.Add(this.lblBottomInspection, 0, 0);
            this.tlpInspection.Controls.Add(this._cbBottomInspect, 1, 0);
            this.tlpInspection.Controls.Add(this.lblPlacementInspection, 0, 1);
            this.tlpInspection.Controls.Add(this._cbPlacementInspect, 1, 1);
            this.tlpInspection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpInspection.Margin = new System.Windows.Forms.Padding(0);
            this.tlpInspection.Name = "tlpInspection";
            this.tlpInspection.RowCount = 3;
            this.tlpInspection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpInspection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpInspection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpInspection.TabIndex = 0;
            //
            // lblBottomInspection
            //
            this.lblBottomInspection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBottomInspection.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblBottomInspection.Name = "lblBottomInspection";
            this.lblBottomInspection.TabIndex = 13;
            this.lblBottomInspection.Text = "Bottom inspection";
            this.lblBottomInspection.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _cbBottomInspect
            //
            this._cbBottomInspect.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbBottomInspect.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbBottomInspect.Name = "_cbBottomInspect";
            this._cbBottomInspect.TabIndex = 14;
            this._cbBottomInspect.Text = "Bottom vision inspection";
            //
            // lblPlacementInspection
            //
            this.lblPlacementInspection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlacementInspection.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPlacementInspection.Name = "lblPlacementInspection";
            this.lblPlacementInspection.TabIndex = 15;
            this.lblPlacementInspection.Text = "Placement inspection";
            this.lblPlacementInspection.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _cbPlacementInspect
            //
            this._cbPlacementInspect.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbPlacementInspect.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbPlacementInspect.Name = "_cbPlacementInspect";
            this._cbPlacementInspect.TabIndex = 16;
            this._cbPlacementInspect.Text = "Placement bin inspection";
            //
            // ModuleSubsetPage
            //
            this.Name = "ModuleSubsetPage";
            this.Size = new System.Drawing.Size(1094, 742);
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this._editorPanel.ResumeLayout(false);
            this.editorLayout.ResumeLayout(false);
            this.grpPickPlace.ResumeLayout(false);
            this.tlpPickPlace.ResumeLayout(false);
            this.grpCollet.ResumeLayout(false);
            this.tlpCollet.ResumeLayout(false);
            this.grpInspection.ResumeLayout(false);
            this.tlpInspection.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nPickRetry)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPickDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPlaceDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nColletInterval)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
