using MechaSys.SoftBricks.Hmi.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.Hmi.Forms
{
    public partial class GeneralSideForm : MechaSys.SoftBricks.Hmi.Forms.Semi.SideForm
    {
        #region Constructor
        public GeneralSideForm()
        {
            InitializeComponent();
        }
        #endregion

        #region Method
        protected GraphicRadioButton CreateButton(string text)
        {
            GraphicRadioButton button = null;

            button = new GraphicRadioButton();
            button.AutoSize = false;
            button.Text = QMCSystem.Translate(text);
            button.Size = new Size(90, 90);

            return button;
        }
        #endregion
    }
}
