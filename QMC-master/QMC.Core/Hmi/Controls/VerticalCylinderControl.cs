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
    #region VerticalCylinderControl
    public partial class VerticalCylinderControl : UserControlX
    {
        #region Field
        private IVerticalCylinder m_Cylinder;
        private string m_Title;
        #endregion

        #region Constructor
        public VerticalCylinderControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Property
        public IVerticalCylinder Cylinder
        {
            get { return this.m_Cylinder; }
            set { this.m_Cylinder = value; }
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
            if(this.Cylinder == null) return;

            if(sender == this.radioButtonUp)
            {
                ar = this.Cylinder.BeginUp();
                WaitingBox.ShowPart(this.ParentForm, ar, "Up...", this.Cylinder);
            }
            else if(sender == this.radioButtonDown)
            {
                ar = this.Cylinder.BeginDown();
                WaitingBox.ShowPart(this.ParentForm, ar, "Down...", this.Cylinder);
            }
        }
        #endregion

        #region UserControlX Members
        protected override void OnDisplay()
        {
            int ret = 0;
            VerticalCylinderState state = VerticalCylinderState.Undefined;

            base.OnDisplay();

            if(this.Cylinder == null) return;
            if((ret = this.Cylinder.Check(ref state)) != 0) return;

            this.radioButtonUp.Checked = state == VerticalCylinderState.Up;
            this.radioButtonDown.Checked = state == VerticalCylinderState.Down;
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();

            if(this.Cylinder != null)
                this.Title = Sys.Translate(this.Cylinder.Alias, Sys.LanguageDomains.Name);

            this.radioButtonDown.Text = QMCSystem.Translate(this.radioButtonDown.Text);
            this.radioButtonUp.Text = QMCSystem.Translate(this.radioButtonUp.Text);
            this.groupBoxXGripper.Text = QMCSystem.Translate(this.groupBoxXGripper.Text);
        }
        #endregion
    }
    #endregion
}
