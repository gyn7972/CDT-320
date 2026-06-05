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
    #region ActionTransferablePositionControl
    public partial class ActionTransferablePositionControl : UserControlX
    {
        #region Field
        private string m_Name;
        private int m_PrimaryPort;
        private string m_SelectedSecondary;
        private int m_SelectedSecondaryPortIndex;
        private string m_SelectedAction;
        #endregion

        #region Event        
        public event ActionTransferablePositionMoveEventHandler MovePosition;
        public event ActionTransferablePositionChangedEventHandler ChangedPosition;
        #endregion

        #region Constructor
        public ActionTransferablePositionControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Property
        public string Title
        {
            get { return this.m_Name; }
            set
            {
                this.m_Name = value;
                this.groupBoxXPrimary.Text = this.m_Name;
            }
        }
        #endregion

        #region Event Handlers
        private void buttonMove_Click(object sender, EventArgs e)
        {
            if(this.m_SelectedAction == null || this.m_SelectedSecondary == null) return;

            this.OnMovePosition(new ActionTransferablePositionMoveEventArgs(this.m_SelectedAction, this.m_PrimaryPort, this.m_SelectedSecondary, this.m_SelectedSecondaryPortIndex));
        }
        private void ActionButton_CheckedChanged(object sender, EventArgs e)
        {
            if(sender is RadioButtonX button)
            {
                if(button.Checked == false) return;

                if(button.Tag is string action)
                    this.m_SelectedAction = action;

                if(this.m_SelectedAction == null || this.m_SelectedSecondary == null) return;

                this.OnChangedPosition(new ActionTransferablePositionChangedEventArgs(this.m_SelectedAction, this.m_PrimaryPort, this.m_SelectedSecondary, this.m_SelectedSecondaryPortIndex));
            }
        }
        private void TransferredButton_CheckedChanged(object sender, EventArgs e)
        {
            if(sender is RadioButtonX button)
            {
                if(button.Checked == false) return;

                if(button.Tag is ValueTuple<string, int> tuple)
                {
                    this.m_SelectedSecondary = tuple.Item1;
                    this.m_SelectedSecondaryPortIndex = tuple.Item2;
                }

                if(this.m_SelectedAction == null || this.m_SelectedSecondary == null) return;

                this.OnChangedPosition(new ActionTransferablePositionChangedEventArgs(this.m_SelectedAction, this.m_PrimaryPort, this.m_SelectedSecondary, this.m_SelectedSecondaryPortIndex));
            }
        }
        #endregion

        #region Method
        private void SetControlHeight()
        {
            if(this.flowLayoutPanelXActions.Controls.Count >= this.flowLayoutPanelXTransferredPort.Controls.Count)
                this.Height = this.flowLayoutPanelXActions.Margin.Bottom + this.flowLayoutPanelXActions.Controls.Count * this.flowLayoutPanelXActions.Gap + this.groupBoxXPrimary.Height - this.flowLayoutPanelXActions.Height;
            else
                this.Height = this.flowLayoutPanelXTransferredPort.Margin.Bottom + this.flowLayoutPanelXTransferredPort.Controls.Count * this.flowLayoutPanelXTransferredPort.Gap + this.groupBoxXPrimary.Height - this.flowLayoutPanelXTransferredPort.Height;
        }

        private void OnMovePosition(ActionTransferablePositionMoveEventArgs e)
        {
            if(this.MovePosition != null)
                this.MovePosition(this, e);
        }

        private void OnChangedPosition(ActionTransferablePositionChangedEventArgs e)
        {
            if(this.ChangedPosition != null)
                this.ChangedPosition(this, e);
        }
        private string GetMaterialStorablePartAlias(string transferName, int portIndex)
        {
            ITransfer transfer = Sys.Equipment.Modules.GetByName(transferName) as ITransfer;

            if(transfer == null) return "";

            return Sys.Translate(transfer.Ports[portIndex].MaterialStorablePart.Alias, Sys.LanguageDomains.Name);
        }

        public void SetActionKeys<Key>()
            where Key : Enum
        {
            Key[] keys = (Key[])Enum.GetValues(typeof(Key));
            string[] temps = Array.ConvertAll(keys, (Key x) => x.ToString());

            this.SetActionKeys(temps);
        }

        public void SetActionKeys(string[] keys)
        {
            RadioButtonX button = null;

            for(int i = 0; i < keys.Length; i++)
            {
                button = this.MakeActionRadioButton(keys[i]);

                this.flowLayoutPanelXActions.Controls.Add(button);
            }

            if(this.flowLayoutPanelXActions.Controls.Count != 0)
                ((RadioButtonX)this.flowLayoutPanelXActions.Controls[0]).Checked = true;

            this.SetControlHeight();
        }

        public void SetTransferableKeys(string primary, int primaryPort, string secondary, int secondaryPort)
        {
            RadioButtonX button;

            this.m_PrimaryPort = primaryPort;

            button = this.MakeRadioButton(secondary, secondaryPort);
            this.flowLayoutPanelXTransferredPort.Controls.Add(button);

            if(this.flowLayoutPanelXTransferredPort.Controls.Count > 0)
                ((RadioButtonX)this.flowLayoutPanelXTransferredPort.Controls[0]).Checked = true;

            this.SetControlHeight();
        }
        public void ClearTransferableKeys()
        {
            this.flowLayoutPanelXActions.Controls.Clear();
            this.flowLayoutPanelXTransferredPort.Controls.Clear();
        }

        private RadioButtonX MakeActionRadioButton(string key)
        {
            RadioButtonX button = new RadioButtonX();

            button.Appearance = Appearance.Button;
            button.TextAlign = ContentAlignment.MiddleRight;
            button.Text = QMCSystem.Translate(key.ToString());
            button.Tag = key;
            button.Width = this.flowLayoutPanelXActions.Width - button.Margin.Left - button.Margin.Right;
            button.Height = this.flowLayoutPanelXTransferredPort.Gap - button.Margin.Bottom - button.Margin.Top;
            button.CheckedChanged += ActionButton_CheckedChanged;
            return button;
        }

        private RadioButtonX MakeRadioButton(string secondary, int secondaryPort)
        {
            RadioButtonX button = new RadioButtonX();

            button.Appearance = Appearance.Button;
            button.TextAlign = ContentAlignment.MiddleRight;
            button.Text = this.GetMaterialStorablePartAlias(secondary, secondaryPort);
            button.Tag = (secondary, secondaryPort);
            button.Width = this.flowLayoutPanelXTransferredPort.Width - button.Margin.Left - button.Margin.Right;
            button.Height = this.flowLayoutPanelXTransferredPort.Gap - button.Margin.Bottom - button.Margin.Top;
            button.CheckedChanged += TransferredButton_CheckedChanged;

            return button;
        }
        #endregion

        #region UserControlX Members
        protected override void OnPrepare()
        {
            base.OnPrepare();

            this.groupBoxXPrimary.Text = QMCSystem.Translate(this.groupBoxXPrimary.Text);
            this.buttonMove.Text = QMCSystem.Translate(this.buttonMove.Text);
        }
        #endregion
    }
    #endregion

    #region ActionTransferablePositionEventArgs
    public class ActionTransferablePositionMoveEventArgs : TransferablePositionMoveEventArgs
    {
        #region Field
        private string m_Action;
        #endregion

        #region Constructor
        public ActionTransferablePositionMoveEventArgs(string action, int primaryPort, string secondary, int secondaryPort) : base(primaryPort, secondary, secondaryPort)
        {
            this.Action = action;
        }
        #endregion

        #region Property

        public string Action
        {
            get { return this.m_Action; }
            set { this.m_Action = value; }
        }
        #endregion
    }
    public delegate void ActionTransferablePositionMoveEventHandler(object sender, ActionTransferablePositionMoveEventArgs e);
    #endregion

    #region ActionTransferablePositionChangedEventArgs
    public class ActionTransferablePositionChangedEventArgs : TransferablePositionChangedEventArgs
    {
        #region Field
        private string m_Action;
        #endregion

        #region Constructor
        public ActionTransferablePositionChangedEventArgs(string action, int primaryPort, string secondary, int secondaryPort) : base(primaryPort, secondary, secondaryPort)
        {
            this.Action = action;
        }
        #endregion
        #region Property

        public string Action
        {
            get { return this.m_Action; }
            set { this.m_Action = value; }
        }
        #endregion
    }
    public delegate void ActionTransferablePositionChangedEventHandler(object sender, ActionTransferablePositionChangedEventArgs e);
    #endregion
}
