using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Hmi.Forms.Semi;

namespace MechaSys.SoftBricks.LoadPorts
{
    public partial class ManualDeviceCarrierIdentifierReaderDialogForm : DialogForm
    {
        //private string m_Identifier;
        private string m_Device;

        public ManualDeviceCarrierIdentifierReaderDialogForm()
        {
            InitializeComponent();
        }

        //public string CarrierIdentifier
        //{
        //    get { return this.m_Identifier; }
        //    set
        //    {
        //        this.m_Identifier = value;
        //        this.textBoxCarrierIdentifier.Text = value;
        //    }
        //}

        public string Device
        {
            get { return this.m_Device; }
            set 
            {
                this.m_Device = value; 
                this.textBoxDevice.Text = value;
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
         
            //this.m_Identifier = this.textBoxCarrierIdentifier.Text;
            //if (string.IsNullOrEmpty(this.m_Identifier) == false)
            //    this.m_Identifier = this.m_Identifier.Trim();

            this.m_Device = this.textBoxDevice.Text;
            if (string.IsNullOrEmpty(this.m_Device) == false)
                this.m_Device = this.m_Device.Trim();

            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void ManualDeviceCarrierIdentifierReaderDialogForm_Load(object sender, EventArgs e)
        {

        }
    }
}
