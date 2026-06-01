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
    public partial class ValveControl : UserControlX
    {
        #region Field
        private IValve m_Valve;
        private string m_Title;
        #endregion

        #region Constructor
        public ValveControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Property       
        public IValve Valve
        {
            get { return this.m_Valve; }
            set { this.m_Valve = value; }
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
            if(this.Valve == null) return;

            if(sender == this.radioButtonOpen)
            {
                ar = this.Valve.BeginOpen();
                WaitingBox.ShowPart(this.ParentForm, ar, "On...", this.Valve);
            }
            else if(sender == this.radioButtonClose)
            {
                ar = this.Valve.BeginClose();
                WaitingBox.ShowPart(this.ParentForm, ar, "Off...", this.Valve);
            }
        }
        #endregion

        #region UserControlX Members
        protected override void OnDisplay()
        {
            int ret = 0;
            ValveState value = ValveState.Undefined;
            base.OnDisplay();

            if(this.Valve == null) return;

            if((ret = this.Valve.Check(ref value)) != 0) return;

            this.radioButtonOpen.Checked = value == ValveState.Open;
            this.radioButtonClose.Checked = value == ValveState.Close;
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();

            if(this.Valve != null)
                this.Title = Sys.Translate(this.Valve.Alias, Sys.LanguageDomains.Name);

            this.radioButtonClose.Text = QMCSystem.Translate(this.radioButtonClose.Text);
            this.radioButtonOpen.Text = QMCSystem.Translate(this.radioButtonOpen.Text);
            this.groupBoxX1.Text = QMCSystem.Translate(this.groupBoxX1.Text);
        }
        #endregion
    }
}
