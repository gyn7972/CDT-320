using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class PickupSubsetPage : SubsetPageBase
    {
        private static readonly Color RbUnchecked = Color.FromArgb(0xF7, 0xFA, 0xFD);
        private static readonly Color RbChecked = Color.FromArgb(0x2E, 0x86, 0xDE);
        private static readonly Color RbCheckedFg = Color.White;
        private static readonly Color RbUncheckedFg = Color.FromArgb(0x33, 0x33, 0x33);

        public PickupSubsetPage() : base("recipe.pickupSubset")
        {
            InitializeComponent();
            ApplyRadioColorsToAll();
        }

        protected override void BuildEditor(Panel c)
        {
        }

        private void PickupRadio_CheckedChanged(object sender, System.EventArgs e)
        {
            ApplyRadioColorsToAll();
        }

        private void ApplyRadioColorsToAll()
        {
            ApplyRadioColors(_rbTL);
            ApplyRadioColors(_rbTR);
            ApplyRadioColors(_rbBL);
            ApplyRadioColors(_rbBR);
            ApplyRadioColors(_rbHoriz);
            ApplyRadioColors(_rbVert);
            ApplyRadioColors(_rbStraight);
            ApplyRadioColors(_rbZigZag);
        }

        private static void ApplyRadioColors(RadioButton rb)
        {
            if (rb == null) return;
            if (rb.Checked)
            {
                rb.BackColor = RbChecked;
                rb.ForeColor = RbCheckedFg;
                rb.FlatAppearance.BorderSize = 3;
                rb.FlatAppearance.BorderColor = Color.FromArgb(0x17, 0x4F, 0x8C);
            }
            else
            {
                rb.BackColor = RbUnchecked;
                rb.ForeColor = RbUncheckedFg;
                rb.FlatAppearance.BorderSize = 1;
                rb.FlatAppearance.BorderColor = Color.LightGray;
            }
        }

        protected override void LoadFromRecipe()
        {
            var p = _project.Pickup ?? new PickupSubset();
            _rbTL.Checked = p.StartCorner == PickupStartCorner.TopLeft;
            _rbTR.Checked = p.StartCorner == PickupStartCorner.TopRight;
            _rbBL.Checked = p.StartCorner == PickupStartCorner.BottomLeft;
            _rbBR.Checked = p.StartCorner == PickupStartCorner.BottomRight;
            _rbHoriz.Checked = p.Direction == PickupDirection.Horizontal;
            _rbVert.Checked = p.Direction == PickupDirection.Vertical;
            _rbStraight.Checked = p.Pattern == PickupPattern.Straight;
            _rbZigZag.Checked = p.Pattern == PickupPattern.ZigZag;
            ApplyRadioColorsToAll();
        }

        protected override void SaveToRecipe()
        {
            var p = _project.Pickup ?? (_project.Pickup = new PickupSubset());
            if (_rbTL.Checked) p.StartCorner = PickupStartCorner.TopLeft;
            else if (_rbTR.Checked) p.StartCorner = PickupStartCorner.TopRight;
            else if (_rbBL.Checked) p.StartCorner = PickupStartCorner.BottomLeft;
            else if (_rbBR.Checked) p.StartCorner = PickupStartCorner.BottomRight;
            p.Direction = _rbVert.Checked ? PickupDirection.Vertical : PickupDirection.Horizontal;
            p.Pattern = _rbZigZag.Checked ? PickupPattern.ZigZag : PickupPattern.Straight;

            try
            {
                var host = FindForm() as Form1;
                if (host?.Controller != null)
                {
                    host.Controller.PickupOptions = p;
                    host.Controller.RebuildPickupSequence();
                }
            }
            catch { }
        }
    }
}