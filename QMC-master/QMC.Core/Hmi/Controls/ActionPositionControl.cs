using MechaSys.SoftBricks.Hmi.Controls;
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
using MechaSys.SoftBricks.PathGenerators;

namespace QMC.Hmi.Controls
{
    #region ActionPositionControl
    public partial class ActionPositionControl : UserControlX
    {
        #region Field
        private string m_SelectedAction;
        private string m_Name;
        private List<string> m_OmittedKeys;
        #endregion

        #region Event
        public event ActionMoveEventHandler MovePosition;
        public event ActionChangedEventHandler ChangedAction;
        #endregion

        #region Constructor
        public ActionPositionControl()
        {
            InitializeComponent();
            this.OmittedKeys = new List<string>();
        }
        #endregion

        #region Property
        public string Title
        {
            get { return this.m_Name; }
            set
            {
                this.m_Name = value;
                this.groupBoxAction.Text = this.m_Name;
            }
        }

        public List<string> OmittedKeys
        {
            get { return this.m_OmittedKeys; }
            set { this.m_OmittedKeys = value; }
        }
        #endregion

        #region Event Handlers
        private void buttonMove_Click(object sender, EventArgs e)
        {
            if(this.m_SelectedAction == null) return;

            this.OnMovePosition(new ActionMoveEventArgs(this.m_SelectedAction));
        }

        private void Button_CheckedChanged(object sender, EventArgs e)
        {
            if(sender is RadioButtonX button)
            {
                if(button.Checked == false) return;

                if(button.Tag is string action)
                    this.m_SelectedAction = action;

                if(this.m_SelectedAction == null) return;

                this.OnChangedAction(new ActionChangedEventArgs(this.m_SelectedAction));
            }
        }

        //private void Button_Click(object sender, EventArgs e)
        //{
        //    if(sender == null) return;

        //    RadioButtonX button = sender as RadioButtonX;
        //    if(button.Checked == false) return;
        //    this.m_SelectedAction = button.Tag.ToString();
        //    this.OnChangedAction(new ActionChangedEventArgs(button.Tag.ToString()));
        //}
        #endregion

        #region Method
        private void OnMovePosition(ActionMoveEventArgs e)
        {
            if(this.MovePosition != null)
                this.MovePosition(this, e);
        }

        private void OnChangedAction(ActionChangedEventArgs e)
        {
            if(this.ChangedAction != null)
                this.ChangedAction(this, e);
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
                if(this.OmittedKeys.Contains(keys[i]) == true) continue;
                button = this.MakeRadioButton(keys[i]);

                this.flowLayoutPanelX1.Controls.Add(button);
            }

            if(this.flowLayoutPanelX1.Controls.Count != 0)
                ((RadioButtonX)this.flowLayoutPanelX1.Controls[0]).Checked = true;

            this.Height = this.flowLayoutPanelX1.Margin.Bottom + this.flowLayoutPanelX1.Controls.Count * this.flowLayoutPanelX1.Gap + this.groupBoxAction.Height - this.flowLayoutPanelX1.Height;
        }

        public void ClearPositionKeys()
        {
            this.flowLayoutPanelX1.Controls.Clear();
        }

        private RadioButtonX MakeRadioButton(string key)
        {
            RadioButtonX button = new RadioButtonX();

            button.Appearance = Appearance.Button;
            button.AutoSize = false;
            button.TextAlign = ContentAlignment.MiddleRight;
            button.Text = QMCSystem.Translate(key.ToString());
            button.Tag = key;
            button.Width = this.flowLayoutPanelX1.Width - button.Margin.Left - button.Margin.Right;
            button.Height = this.flowLayoutPanelX1.Gap - button.Margin.Bottom - button.Margin.Top;
            button.CheckedChanged += Button_CheckedChanged;
            return button;
        }
        #endregion

        #region UserControlX Members
        protected override void OnPrepare()
        {
            base.OnPrepare();

            this.groupBoxAction.Text = QMCSystem.Translate(this.groupBoxAction.Text);
            this.buttonMove.Text = QMCSystem.Translate(this.buttonMove.Text);
        }
        #endregion
    }
    #endregion

    #region ActionPositionEventArgs
    public class ActionMoveEventArgs : EquipmentEventArgs
    {
        #region Field
        private string m_ActionKey;
        #endregion

        #region Constructor
        public ActionMoveEventArgs(string actionKey) : base()
        {
            this.ActionKey = actionKey;
        }
        #endregion

        #region Property
        public string ActionKey
        {
            get { return this.m_ActionKey; }
            private set { this.m_ActionKey = value; }
        }
        #endregion
    }
    public delegate void ActionMoveEventHandler(object sender, ActionMoveEventArgs e);

    public class ActionChangedEventArgs : EquipmentEventArgs
    {
        #region Field
        private string m_ActionKey;
        #endregion

        #region Constructor
        public ActionChangedEventArgs(string actionKey) : base()
        {
            this.ActionKey = actionKey;
        }
        #endregion

        #region Property
        public string ActionKey
        {
            get { return this.m_ActionKey; }
            private set { this.m_ActionKey = value; }
        }
        #endregion
    }
    public delegate void ActionChangedEventHandler(object sender, ActionChangedEventArgs e);
    #endregion
}
