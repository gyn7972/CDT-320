using System.Linq;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.IO;
using MechaSys.SoftBricks.Transfer;

namespace QMC.Hmi.Controls
{
    #region MaterialStorablePartControl
    public partial class MaterialStorablePartControl : UserControlX
    {
        #region Field
        private MaterialStorablePart m_MaterialStorablePart;
        private string m_Title;
        #endregion

        #region Constructor
        public MaterialStorablePartControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Property

        public MaterialStorablePart MaterialStorablePart
        {
            get { return this.m_MaterialStorablePart; }
            set
            {
                if(value == null) return;
                this.m_MaterialStorablePart = value;
                this.SetControl();
            }
        }
        public string Title
        {
            get { return this.m_Title; }
            set
            {
                this.m_Title = value;
                this.groupBoxXMaterialStorablePart.Text = this.m_Title;
            }
        }
        #endregion

        #region Method
        private void SetControl()
        {
            GripperControl gripperControl;

            this.groupBoxXMaterialStorablePart.Text = Sys.Translate(this.MaterialStorablePart.Alias, Sys.LanguageDomains.Name);

            if(0 >= this.MaterialStorablePart.Grippers.Count)
            {
                if(this.flowLayoutPanelXGripper.Visible == true)
                {
                    this.Width -= this.flowLayoutPanelXGripper.Width;
                    this.flowLayoutPanelXGripper.Visible = false;
                }
            }
            else
            {
                for(int i = 0; i < this.MaterialStorablePart.Grippers.Count; i++)
                {
                    gripperControl = new GripperControl();
                    gripperControl.Gripper = this.MaterialStorablePart.Grippers[i];

                    this.flowLayoutPanelXGripper.Controls.Add(gripperControl);
                }

                if(this.flowLayoutPanelXGripper.Visible == false)
                {
                    this.Width += this.flowLayoutPanelXGripper.Width;
                    this.flowLayoutPanelXGripper.Visible = true;
                }
            }

            for(int i = 0; i < this.MaterialStorablePart.MaterialDetectors.Count; i++)
            {
                if(this.MaterialStorablePart.MaterialDetectors[i] is DiPart)
                {
                    this.digitalGroupBox1.Points = ((DiPart)this.MaterialStorablePart.MaterialDetectors[i]).Inputs.ToArray();
                }
            }

            if(0 >= this.digitalGroupBox1.Controls.Count)
            {
                if(this.digitalGroupBox1.Visible == true)
                {
                    this.Width -= this.digitalGroupBox1.Width;
                    this.digitalGroupBox1.Visible = false;
                }
            }
            else
            {
                if(this.digitalGroupBox1.Visible == false)
                {
                    this.Width += this.digitalGroupBox1.Width;
                    this.digitalGroupBox1.Visible = true;
                }
            }

            if(this.digitalGroupBox1.Controls.Count * this.digitalGroupBox1.Gap < this.flowLayoutPanelXGripper.Controls.Count * this.flowLayoutPanelXGripper.Gap)
            {
                this.Height = this.flowLayoutPanelXGripper.Controls.Count * this.flowLayoutPanelXGripper.Gap + this.groupBoxXMaterialStorablePart.Height - this.flowLayoutPanelXGripper.Height;
            }
            else
            {
                this.Height = this.digitalGroupBox1.Controls.Count * this.digitalGroupBox1.Gap + this.groupBoxXMaterialStorablePart.Height - this.digitalGroupBox1.Height;
            }
        }
        #endregion

        #region UserControlX Members
        protected override void OnDisplay()
        {
            base.OnDisplay();

            for(int i = 0; i < this.flowLayoutPanelXGripper.Controls.Count; i++)
            {
                if(this.flowLayoutPanelXGripper.Controls[i] is GripperControl)
                {
                    ((GripperControl)this.flowLayoutPanelXGripper.Controls[i]).Display();
                }
            }
        }
        #endregion
    }
    #endregion
}
