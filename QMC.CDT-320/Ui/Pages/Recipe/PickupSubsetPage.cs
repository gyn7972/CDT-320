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
        private bool _loading;
        private bool _currentTargetIsOutput;

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

        private void PickupTarget_CheckedChanged(object sender, System.EventArgs e)
        {
            ApplyRadioColorsToAll();
            if (_loading || _project == null)
                return;

            var rb = sender as RadioButton;
            if (rb != null && !rb.Checked)
                return;

            SaveVisibleToPickupSubset(_currentTargetIsOutput ? _project.OutputPickup : _project.InputPickup);
            _currentTargetIsOutput = _rbBin.Checked;
            LoadSelectedTargetFromRecipe();
        }

        private void ApplyRadioColorsToAll()
        {
            ApplyRadioColors(_rbWafer);
            ApplyRadioColors(_rbBin);
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
            _loading = true;
            EnsurePickupSubsets();
            if (!_rbWafer.Checked && !_rbBin.Checked)
                _rbWafer.Checked = true;
            _currentTargetIsOutput = _rbBin.Checked;
            LoadSelectedTargetFromRecipe();
            _loading = false;
            ApplyRadioColorsToAll();
        }

        private void LoadSelectedTargetFromRecipe()
        {
            var p = ResolveSelectedPickupSubset();
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
            EnsurePickupSubsets();
            var p = ResolveSelectedPickupSubset();
            SaveVisibleToPickupSubset(p);

            if (_rbWafer.Checked)
                _project.Pickup = ClonePickupSubset(p);

            try
            {
                var host = FindForm() as Form1;
                if (host?.Controller != null)
                {
                    host.Controller.PickupOptions = _project.InputPickup ?? p;
                    host.Controller.RebuildPickupSequence();
                }
            }
            catch { }
        }

        private void SaveVisibleToPickupSubset(PickupSubset p)
        {
            if (p == null)
                return;

            if (_rbTL.Checked) p.StartCorner = PickupStartCorner.TopLeft;
            else if (_rbTR.Checked) p.StartCorner = PickupStartCorner.TopRight;
            else if (_rbBL.Checked) p.StartCorner = PickupStartCorner.BottomLeft;
            else if (_rbBR.Checked) p.StartCorner = PickupStartCorner.BottomRight;
            p.Direction = _rbVert.Checked ? PickupDirection.Vertical : PickupDirection.Horizontal;
            p.Pattern = _rbZigZag.Checked ? PickupPattern.ZigZag : PickupPattern.Straight;
        }

        private void EnsurePickupSubsets()
        {
            if (_project == null)
                return;

            if (_project.Pickup == null)
                _project.Pickup = new PickupSubset();
            if (_project.InputPickup == null)
                _project.InputPickup = ClonePickupSubset(_project.Pickup);
            if (_project.OutputPickup == null)
                _project.OutputPickup = ClonePickupSubset(_project.Pickup);
        }

        private PickupSubset ResolveSelectedPickupSubset()
        {
            EnsurePickupSubsets();
            if (_project == null)
                return new PickupSubset();

            return _rbBin.Checked ? _project.OutputPickup : _project.InputPickup;
        }

        private static PickupSubset ClonePickupSubset(PickupSubset source)
        {
            if (source == null)
                return new PickupSubset();

            return new PickupSubset
            {
                StartCorner = source.StartCorner,
                Direction = source.Direction,
                Pattern = source.Pattern
            };
        }
    }
}
