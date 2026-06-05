using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Materials.Controls;

namespace QMC.LoadPorts
{
    public partial class PlateTransferLoadPortMaintenanceForm : QMC.Hmi.Forms.GeneralTabControlModuleMaintenanceForm
    {
        #region Constructor
        public PlateTransferLoadPortMaintenanceForm(PlateTransferLoadPort target) : base(target)
        {
            InitializeComponent();
        }
        public PlateTransferLoadPortMaintenanceForm() : this(null) { }
        #endregion

        #region Method
        private void PrepareSlotState()
        {
            try
            {
                this.tabControlSlotState.SuspendLayout();
                this.tabControlSlotState.TabPages.Clear();
                if (this.Target == null) return;

                foreach (LoadPortPlate plate in this.Target.Plates)
                {
                    TabPageX page = new TabPageX();
                    page.Text = Sys.Translate(plate.Alias, Sys.LanguageDomains.Name);
                    page.Tag = plate;
                    this.tabControlSlotState.TabPages.Add(page);

                    SlotStateView stateView1, stateView2;
                    int width = 0, height = 0;

                    width = page.ClientSize.Width / 2 - page.Padding.Left - page.Padding.Right;
                    height = page.ClientSize.Height - page.Padding.Top - page.Padding.Bottom;

                    stateView1 = new SlotStateView();
                    page.Controls.Add(stateView1);
                    stateView1.Location = new Point(page.Padding.Left, page.Padding.Top);
                    stateView1.Size = new Size(width, height);

                    stateView2 = new SlotStateView();
                    page.Controls.Add(stateView2);
                    stateView2.Location = new Point(stateView1.Location.X + stateView1.Size.Width + page.Padding.Left, page.Padding.Top);
                    stateView2.Size = new Size(width, height);
                }
            }
            finally
            {
                this.tabControlSlotState.ResumeLayout(false);
            }
        }

        private void DisplaySlotState()
        {
            LoadPortPlate plate = null;
            Carrier carrier = null;
            SlotStateView stateView = null;

            if (this.Target == null) return;

            foreach (TabPageX page in this.tabControlSlotState.TabPages)
            {
                plate = page.Tag as LoadPortPlate;
                carrier = plate.Port.Location.GetMaterial() as Carrier;

                stateView = page.Controls[0] as SlotStateView;
                if (stateView != null)
                {
                    stateView.Visible = carrier.Presence == MaterialPresence.Exist;
                    if (stateView.Visible)
                    {
                        if (stateView.SlotCount != carrier.Capacity)
                            stateView.SlotCount = carrier.Capacity;
                        stateView.Display(carrier);
                    }
                }

                stateView = page.Controls[1] as SlotStateView;
                if (stateView != null)
                {
                    if (carrier is StackCarrier)
                        carrier = ((StackCarrier)carrier).Above;
                    else
                        carrier = null;

                    stateView.Visible = carrier != null && carrier.Presence == MaterialPresence.Exist;
                    if (stateView.Visible)
                    {
                        if (stateView.SlotCount != carrier.Capacity)
                            stateView.SlotCount = carrier.Capacity;
                        stateView.Display(carrier);
                    }
                }
            }
        }
        #endregion

        #region ElementMaintenanceForm
        public new PlateTransferLoadPort Target
        {
            get { return base.Target as PlateTransferLoadPort; }
        }
        #endregion

        #region ElementMaintenanceForm
        protected override void OnDisplay()
        {
            base.OnDisplay();

            if (this.Target == null) return;

            this.DisplaySlotState();
        }
        #endregion

        #region ChildForm
        protected override void OnPrepare()
        {
            base.OnPrepare();

            if (this.Target == null) return;

            this.PrepareSlotState();
        }
        #endregion
    }
}