using System;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.IO.Parts;

namespace QMC.Hmi.Controls
{
    public partial class ClamperControl : UserControlX
    {
        #region Field
        private IClamper m_Clamper;
        private string m_Title;
        #endregion

        #region Constructor
        public ClamperControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Property
        public IClamper Clamper
        {
            get { return this.m_Clamper; }
            set { this.m_Clamper = value; }
        }

        public string Title
        {
            get { return this.m_Title; }
            set
            {
                this.m_Title = value;
                this.groupBoxXGripper.Text = this.m_Title;
            }
        }
        #endregion

        #region Event Handlers
        private void buttonXOperation_Click(object sender, EventArgs e)
        {
            MethodCallerAsyncResult ar = null;

            if(sender == null) return;
            if(this.Clamper == null) return;

            if(sender == this.radioButtonClamp)
            {
                ar = this.Clamper.BeginClamp();
                WaitingBox.ShowPart(this.ParentForm, ar, "Clamp...", this.Clamper);
            }
            else if(sender == this.radioButtonUnclamp)
            {
                ar = this.Clamper.BeginUnclamp();
                WaitingBox.ShowPart(this.ParentForm, ar, "Unclamp...", this.Clamper);
            }
        }
        #endregion

        #region UserControlX Members
        protected override void OnDisplay()
        {
            int ret = 0;
            ClamperState state = ClamperState.Undefined;

            base.OnDisplay();

            if(this.Clamper == null) return;
            if((ret = this.Clamper.Check(ref state)) != 0) return;

            this.radioButtonClamp.Checked = state == ClamperState.Clamp;
            this.radioButtonUnclamp.Checked = state == ClamperState.Unclamp;
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();

            if(this.Clamper != null)
                this.Title = Sys.Translate(this.Clamper.Alias, Sys.LanguageDomains.Name);

            this.radioButtonClamp.Text = QMCSystem.Translate(this.radioButtonClamp.Text);
            this.radioButtonUnclamp.Text = QMCSystem.Translate(this.radioButtonUnclamp.Text);
            this.groupBoxXGripper.Text = QMCSystem.Translate(this.groupBoxXGripper.Text);
        }
        #endregion
    }
}
