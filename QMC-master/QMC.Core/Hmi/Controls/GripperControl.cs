using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.Hmi.Controls
{
    public partial class GripperControl : UserControlX
    {
        #region Field
        private IGripper m_Gripper;
        private string m_Title;
        #endregion

        #region Constructor
        public GripperControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Property
        public IGripper Gripper
        {
            get { return this.m_Gripper; }
            set { this.m_Gripper = value; }
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
            if(this.Gripper == null) return;

            if(sender == this.radioButtonHold)
            {
                ar = this.Gripper.BeginHold();
                WaitingBox.ShowPart(this.ParentForm, ar, "Hold...", this.Gripper);
            }
            else if(sender == this.radioButtonRelease)
            {
                ar = this.Gripper.BeginRelease();
                WaitingBox.ShowPart(this.ParentForm, ar, "Release...", this.Gripper);
            }
        }
        #endregion

        #region UserControlX Members
        protected override void OnDisplay()
        {
            int ret = 0;
            GripperState state = MechaSys.SoftBricks.IO.Parts.GripperState.Undefined;

            base.OnDisplay();

            if(this.Gripper == null) return;
            if((ret = this.Gripper.Check(ref state)) != 0) return;

            this.radioButtonHold.Checked = state == MechaSys.SoftBricks.IO.Parts.GripperState.Hold;
            this.radioButtonRelease.Checked = state == MechaSys.SoftBricks.IO.Parts.GripperState.Release;
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();

            if(this.Gripper != null)
                this.Title = Sys.Translate(this.Gripper.Alias, Sys.LanguageDomains.Name);

            this.radioButtonHold.Text = QMCSystem.Translate(this.radioButtonHold.Text);
            this.radioButtonRelease.Text = QMCSystem.Translate(this.radioButtonRelease.Text);
            this.groupBoxXGripper.Text = QMCSystem.Translate(this.groupBoxXGripper.Text);
        }
        #endregion
    }
}
