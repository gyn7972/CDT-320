using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks.IO;
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
    public partial class ActuatorControl : UserControlX
    {
        #region Field
        private Actuator m_Actuator;
        private string m_Title;
        #endregion

        #region Constructor
        public ActuatorControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Property       
        public Actuator Actuator
        {
            get { return this.m_Actuator; }
            set { this.m_Actuator = value; }
        }
        public string Title
        {
            get { return this.m_Title; }
            set
            {
                this.m_Title = value;
                this.groupBoxX1.Text = this.m_Title;
            }
        }
        #endregion

        #region Event Handlers
        private void buttonXOperation_Click(object sender, EventArgs e)
        {
            MethodCallerAsyncResult ar = null;

            if(sender == null) return;
            if(this.Actuator == null) return;

            if(sender == this.radioButtonOn)
            {
                ar = this.Actuator.BeginOn();
                WaitingBox.ShowPart(this.ParentForm, ar, "On...", this.Actuator);
            }
            else if(sender == this.radioButtonOff)
            {
                ar = this.Actuator.BeginOff();
                WaitingBox.ShowPart(this.ParentForm, ar, "Off...", this.Actuator);
            }
        }
        #endregion

        #region UserControlX Members
        protected override void OnDisplay()
        {
            int ret = 0;
            DioValue value = DioValue.Unknown;
            base.OnDisplay();

            if(this.Actuator == null) return;

            if((ret = this.Actuator.Check(ref value)) != 0) return;

            this.radioButtonOn.Checked = value == DioValue.On;
            this.radioButtonOff.Checked = value == DioValue.Off;
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();

            if(this.Actuator != null)
                this.Title = Sys.Translate(this.Actuator.Alias, Sys.LanguageDomains.Name);

            this.radioButtonOff.Text = QMCSystem.Translate(this.radioButtonOff.Text);
            this.radioButtonOn.Text = QMCSystem.Translate(this.radioButtonOn.Text);
            this.groupBoxX1.Text = QMCSystem.Translate(this.groupBoxX1.Text);
        }
        #endregion
    }
}
