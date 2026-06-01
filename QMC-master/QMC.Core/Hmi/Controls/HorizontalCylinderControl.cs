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
    #region HorizontalCylinderControl
    public partial class HorizontalCylinderControl : UserControlX
    {
        #region Field
        private IHorizontalCylinder m_Cylinder;
        private string m_Title;
        #endregion

        #region Constructor
        public HorizontalCylinderControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Property
        public IHorizontalCylinder Cylinder
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

            if(sender == this.radioButtonForward)
            {
                ar = this.Cylinder.BeginForward();
                WaitingBox.ShowPart(this.ParentForm, ar, "Forward...", this.Cylinder);
            }
            else if(sender == this.radioButtonBackward)
            {
                ar = this.Cylinder.BeginBackward();
                WaitingBox.ShowPart(this.ParentForm, ar, "Backward...", this.Cylinder);
            }
        }
        #endregion

        #region UserControlX Members
        protected override void OnDisplay()
        {
            int ret = 0;
            HorizontalCylinderState state = HorizontalCylinderState.Undefined;

            base.OnDisplay();

            if(this.Cylinder == null) return;
            if((ret = this.Cylinder.Check(ref state)) != 0) return;

            this.radioButtonForward.Checked = state == HorizontalCylinderState.Forward;
            this.radioButtonBackward.Checked = state == HorizontalCylinderState.Backward;
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();

            if(this.Cylinder != null)
                this.Title = Sys.Translate(this.Cylinder.Alias, Sys.LanguageDomains.Name);

            this.radioButtonForward.Text = QMCSystem.Translate(this.radioButtonForward.Text);
            this.radioButtonBackward.Text = QMCSystem.Translate(this.radioButtonBackward.Text);
            this.groupBoxXGripper.Text = QMCSystem.Translate(this.groupBoxXGripper.Text);
        }
        #endregion
    }
    #endregion
}
