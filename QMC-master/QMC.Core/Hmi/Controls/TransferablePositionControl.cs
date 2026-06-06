using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Transfer;
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
    #region TransferablePositionControl
    public partial class TransferablePositionControl : UserControlX
    {
        #region Field
        private int m_PrimaryPort;
        private string m_SelectedSecondary;
        private int m_SelectedSecondaryPortIndex;
        #endregion

        #region Event
        public event TransferablePositionMoveEventHandler MovePosition;
        public event TransferablePositionChangedEventHandler ChangedPosition;
        #endregion

        #region Constructor
        public TransferablePositionControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Event Handlers
        private void buttonMove_Click(object sender, EventArgs e)
        {
            this.OnMovePosition(new TransferablePositionMoveEventArgs(this.m_PrimaryPort, this.m_SelectedSecondary, this.m_SelectedSecondaryPortIndex));
        }

        private void Button_CheckedChanged(object sender, EventArgs e)
        {
            RadioButtonX button;

            if(sender == null) return;

            button = sender as RadioButtonX;
            this.m_SelectedSecondary = ((ValueTuple<string, int>)button.Tag).Item1.ToString();
            this.m_SelectedSecondaryPortIndex = Convert.ToInt32(((ValueTuple<string, int>)button.Tag).Item2);

            this.OnChangedPosition(new TransferablePositionChangedEventArgs(this.m_PrimaryPort, this.m_SelectedSecondary, this.m_SelectedSecondaryPortIndex));
        }
        #endregion

        #region Method
        private void SetControlHeight()
        {
            this.Height = this.flowLayoutPanelTransferredPort.Margin.Bottom + this.flowLayoutPanelTransferredPort.Controls.Count * this.flowLayoutPanelTransferredPort.Gap + this.groupBoxPrimary.Height - this.flowLayoutPanelTransferredPort.Height;
        }
        private void OnMovePosition(TransferablePositionMoveEventArgs e)
        {
            if(this.MovePosition != null)
                this.MovePosition(this, e);
        }
        private void OnChangedPosition(TransferablePositionChangedEventArgs e)
        {
            if(this.ChangedPosition != null)
                this.ChangedPosition(this, e);
        }

        public void SetTransferableKeys(string primary, int primaryPort, string secondary, int secondaryPort)
        {
            RadioButtonX button;

            this.m_PrimaryPort = primaryPort;

            button = this.MakeRadioButton(secondary, secondaryPort);
            this.flowLayoutPanelTransferredPort.Controls.Add(button);

            if(this.flowLayoutPanelTransferredPort.Controls.Count > 0)
                ((RadioButtonX)this.flowLayoutPanelTransferredPort.Controls[0]).Checked = true;

            this.SetControlHeight();
        }

        public void ClearTransferableKeys()
        {
            this.flowLayoutPanelTransferredPort.Controls.Clear();
        }

        private RadioButtonX MakeRadioButton(string secondary, int secondaryPort)
        {
            RadioButtonX button = new RadioButtonX();

            button.Appearance = Appearance.Button;
            button.TextAlign = ContentAlignment.MiddleRight;
            button.Text = this.GetMaterialStorablePartAlias(secondary, secondaryPort);
            button.Tag = (secondary, secondaryPort);
            button.Width = this.flowLayoutPanelTransferredPort.Width - button.Margin.Left - button.Margin.Right;
            button.Height = this.flowLayoutPanelTransferredPort.Gap - button.Margin.Bottom - button.Margin.Top;
            button.CheckedChanged += Button_CheckedChanged;

            return button;
        }

        private string GetMaterialStorablePartAlias(string transferName, int portIndex)
        {
            ITransfer transfer = Sys.Equipment.Modules.GetByName(transferName) as ITransfer;

            if(transfer == null) return "";

            return transfer.Ports[portIndex].MaterialStorablePart.Alias;
        }
        #endregion

        #region MyRegion
        protected override void OnPrepare()
        {
            base.OnPrepare();

            this.buttonMove.Text = QMCSystem.Translate(this.buttonMove.Text);
            this.groupBoxPrimary.Text = QMCSystem.Translate(this.groupBoxPrimary.Text);
        }
        #endregion
    }
    #endregion

    #region TransferablePositionMoveEventArgs
    public class TransferablePositionMoveEventArgs : EquipmentEventArgs
    {
        #region Field
        private int m_PrimaryPort;
        private string m_Secondary;
        private int m_SecondaryPort;
        #endregion

        #region Constructor
        public TransferablePositionMoveEventArgs(int primaryPort, string secondary, int secondaryPort) : base()
        {
            this.PrimaryPort = primaryPort;
            this.Secondary = secondary;
            this.SecondaryPort = secondaryPort;
        }
        #endregion

        #region Property
        public int SecondaryPort
        {
            get { return this.m_SecondaryPort; }
            set { this.m_SecondaryPort = value; }
        }
        public string Secondary
        {
            get { return this.m_Secondary; }
            private set { this.m_Secondary = value; }
        }
        public int PrimaryPort
        {
            get { return this.m_PrimaryPort; }
            private set { this.m_PrimaryPort = value; }
        }
        #endregion
    }

    public delegate void TransferablePositionMoveEventHandler(object sender, TransferablePositionMoveEventArgs e);
    #endregion

    #region TransferablePositionChangedEventArgs
    public class TransferablePositionChangedEventArgs : EquipmentEventArgs
    {
        #region Field      
        private int m_PrimaryPort;
        private string m_Secondary;
        private int m_SecondaryPort;
        #endregion

        #region Constructor
        public TransferablePositionChangedEventArgs(int primaryPort, string secondary, int secondaryPort) : base()
        {
            this.PrimaryPort = primaryPort;
            this.Secondary = secondary;
            this.SecondaryPort = secondaryPort;
        }
        #endregion

        #region Property
        public int SecondaryPort
        {
            get { return this.m_SecondaryPort; }
            set { this.m_SecondaryPort = value; }
        }
        public string Secondary
        {
            get { return this.m_Secondary; }
            private set { this.m_Secondary = value; }
        }
        public int PrimaryPort
        {
            get { return this.m_PrimaryPort; }
            private set { this.m_PrimaryPort = value; }
        }
        #endregion
    }

    public delegate void TransferablePositionChangedEventHandler(object sender, TransferablePositionChangedEventArgs e);
    #endregion
}
