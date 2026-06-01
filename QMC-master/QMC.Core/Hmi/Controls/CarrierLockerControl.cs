using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks.IO;
using MechaSys.SoftBricks;

using QMC.Parts;

namespace QMC.Hmi.Controls
{
    public partial class CarrierLockerControl : UserControlX
    {
        #region Field
        private CarrierLocker m_CarrierLocker;
        #endregion

        #region Constructor
        public CarrierLockerControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Property      
        public CarrierLocker CarrierLocker
        {
            get { return this.m_CarrierLocker; }
            set
            {
                if(value == null && this.m_CarrierLocker == value) return;

                this.m_CarrierLocker = value;
                this.SetCarrierLocker();
            }
        }
        #endregion

        #region Method
        private void SetCarrierLocker()
        {
            List<DioPoint> points = new List<DioPoint>();
            points.Add(this.CarrierLocker.EnableLock.Inputs[0]);

            this.digitalGroupBox1.Points = points.ToArray();

            this.groupBoxX1.Text = Sys.Translate(this.CarrierLocker.Alias, Sys.LanguageDomains.Name);
        }
        #endregion

        #region Event Handlers
        private void OperationButton_Click(object sender, EventArgs e)
        {
            MethodCallerAsyncResult ar;
            if(sender == null) return;

            if(sender == this.radioButtonLock)
            {
                ar = this.CarrierLocker.BeginLock();
                WaitingBox.ShowPart(this, ar, "Lock...", this.CarrierLocker);
            }
            else if(sender == this.radioButtonUnlock)
            {
                ar = this.CarrierLocker.BeginUnlock();
                WaitingBox.ShowPart(this, ar, "Unlock...", this.CarrierLocker);
            }
        }
        #endregion

        #region UserControlX Members
        protected override void OnDisplay()
        {
            if(this.CarrierLocker == null) return;

            LockerState state = LockerState.Lock;
            base.OnDisplay();

            this.CarrierLocker.Check(ref state);
            if(state == LockerState.Lock)
                this.radioButtonLock.Checked = true;
            else if(state == LockerState.Unlock)
                this.radioButtonUnlock.Checked = true;
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();

            this.radioButtonLock.Text = QMCSystem.Translate(this.radioButtonLock.Text);
            this.radioButtonUnlock.Text = QMCSystem.Translate(this.radioButtonUnlock.Text);
        }
        #endregion
    }
}
