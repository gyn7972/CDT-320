using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class CstStatusDialog : Form
    {
        public CstStatusDialog(bool isInput)
        {
            InitializeComponent();
            Text = isInput ? "INPUT CASSETTE STATUS" : "OUTPUT CASSETTE STATUS";
            lblTitle.Text = Text;
            PopulateSlots();
            Load += (s, e) => Lang.Apply(this);
        }

        private void PopulateSlots()
        {
            if (slotStateLabels == null || slotStateLabels.Length == 0)
                return;

            var rnd = new System.Random(7);
            for (int i = 0; i < slotStateLabels.Length; i++)
            {
                Color c = Color.LimeGreen;
                int rr = rnd.Next(10);
                if (rr < 2) c = Color.Cyan;
                else if (rr < 4) c = Color.Orange;
                else if (rr < 5) c = Color.Red;
                else if (rr < 6) c = Color.Navy;

                slotStateLabels[i].BackColor = c;
            }
        }
    }
}
