using System;
using System.Drawing;
using System.Windows.Forms;
using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Hmi.Controls;

namespace QMC.Hmi.Controls
{
    public partial class ActionTargetPositionControl : UserControlX
    {
        #region Field
        private string m_Name;
        private string m_SelectedAction;
        private IElement m_SelectedTarget;
        #endregion

        #region Event
        public event ActionTargetMoveEventHandler MovePosition;
        public event ActionTargetChangedEventHandler ChangedPosition;
        #endregion

        #region Constructor
        public ActionTargetPositionControl()
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
            if(this.m_SelectedTarget == null || this.m_SelectedAction == null) return;

            this.OnMovePosition(new ActionTargetMoveEventArgs(this.m_SelectedTarget.Locator.ToString(), this.m_SelectedAction));
        }

        private void ActionButton_CheckedChanged(object sender, EventArgs e)
        {
            if(sender is RadioButtonX button)
            {
                if(button.Checked == false) return;

                if(button.Tag is string action)
                    this.m_SelectedAction = action;

                if(this.m_SelectedTarget == null || this.m_SelectedAction == null) return;

                this.OnChangedPosition(new ActionTargetChangedEventArgs(this.m_SelectedTarget.Locator.ToString(), this.m_SelectedAction));
            }
        }

        private void TransferredButton_CheckedChanged(object sender, EventArgs e)
        {
            if(sender is RadioButtonX button)
            {
                if(button.Checked == false) return;

                if(button.Tag is IElement element)
                    this.m_SelectedTarget = element;

                if(this.m_SelectedTarget == null || this.m_SelectedAction == null) return;

                this.OnChangedPosition(new ActionTargetChangedEventArgs(this.m_SelectedTarget.Locator.ToString(), this.m_SelectedAction));
            }
        }
        #endregion

        #region Method
        private void OnMovePosition(ActionTargetMoveEventArgs e)
        {
            if(this.MovePosition != null)
                this.MovePosition(this, e);
        }

        private void OnChangedPosition(ActionTargetChangedEventArgs e)
        {
            if(this.ChangedPosition != null)
                this.ChangedPosition(this, e);
        }
        private void SetControlHeight()
        {
            if(this.flowLayoutPanelXActions.Controls.Count >= this.flowLayoutPanelXTarget.Controls.Count)
                this.Height = this.flowLayoutPanelXActions.Margin.Bottom + this.flowLayoutPanelXActions.Controls.Count * this.flowLayoutPanelXActions.Gap + this.groupBoxXPrimary.Height - this.flowLayoutPanelXActions.Height;
            else
                this.Height = this.flowLayoutPanelXTarget.Margin.Bottom + this.flowLayoutPanelXTarget.Controls.Count * this.flowLayoutPanelXTarget.Gap + this.groupBoxXPrimary.Height - this.flowLayoutPanelXTarget.Height;
        }
        public void SetActionKeys<Key>(params IElement[] targets)
            where Key : Enum
        {
            Key[] keys = (Key[])Enum.GetValues(typeof(Key));
            string[] temps = Array.ConvertAll(keys, (Key x) => x.ToString());

            this.SetActionKeys(temps, targets);
        }

        public void SetActionKeys(string[] keys, params IElement[] targets)
        {
            RadioButtonX button = null;

            for(int i = 0; i < keys.Length; i++)
            {
                button = this.MakeActionRadioButton(keys[i]);
                this.flowLayoutPanelXActions.Controls.Add(button);
            }

            if(this.flowLayoutPanelXActions.Controls.Count != 0)
                ((RadioButtonX)this.flowLayoutPanelXActions.Controls[0]).Checked = true;

            for(int i = 0; i < targets.Length; i++)
            {
                button = this.MakeRadioButton(targets[i]);
                this.flowLayoutPanelXTarget.Controls.Add(button);
            }

            if(this.flowLayoutPanelXTarget.Controls.Count != 0)
                ((RadioButtonX)this.flowLayoutPanelXTarget.Controls[0]).Checked = true;

            this.SetControlHeight();
        }

        private RadioButtonX MakeActionRadioButton(string key)
        {
            RadioButtonX button = new RadioButtonX();

            button.Appearance = Appearance.Button;
            button.TextAlign = ContentAlignment.MiddleRight;
            button.Text = QMCSystem.Translate(key.ToString());
            button.Tag = key;
            button.Width = this.flowLayoutPanelXActions.Width - button.Margin.Left - button.Margin.Right;
            button.Height = this.flowLayoutPanelXActions.Gap - button.Margin.Bottom - button.Margin.Top;
            button.CheckedChanged += ActionButton_CheckedChanged;
            return button;
        }

        private RadioButtonX MakeRadioButton(IElement target)
        {
            RadioButtonX button = new RadioButtonX();

            button.Appearance = Appearance.Button;
            button.TextAlign = ContentAlignment.MiddleRight;
            button.Text = Sys.Translate(target.Alias, Sys.LanguageDomains.Name);
            button.Tag = target;
            button.Width = this.flowLayoutPanelXTarget.Width - button.Margin.Left - button.Margin.Right;
            button.Height = this.flowLayoutPanelXTarget.Gap - button.Margin.Bottom - button.Margin.Top;
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

    #region ActionTargetMoveEventArgs
    public class ActionTargetMoveEventArgs : ActionMoveEventArgs
    {
        #region Field
        private string m_Target;
        #endregion

        #region Constructor
        public ActionTargetMoveEventArgs(string target, string actionKey) : base(actionKey)
        {
            this.Target = target;
        }
        #endregion

        #region Property       
        public string Target
        {
            get { return this.m_Target; }
            set { this.m_Target = value; }
        }
        #endregion
    }
    public delegate void ActionTargetMoveEventHandler(object sender, ActionTargetMoveEventArgs e);
    #endregion

    #region ActionTransferablePositionChangedEventArgs
    public class ActionTargetChangedEventArgs : ActionChangedEventArgs
    {
        #region Field
        private string m_Target;
        #endregion

        #region Constructor
        public ActionTargetChangedEventArgs(string target, string action) : base(action)
        {
            this.m_Target = target;
        }
        #endregion

        #region Property
        public string Target
        {
            get { return this.m_Target; }
            set { this.m_Target = value; }
        }
        #endregion
    }
    public delegate void ActionTargetChangedEventHandler(object sender, ActionTargetChangedEventArgs e);
    #endregion
}
