using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>
    /// Stage 38 — Module Subset Recipe 편집 페이지.<br/>
    /// PickRetryCount / Pick·PlaceDelay / Collet 옵션 / Inspection 옵션 노출.
    /// </summary>
    public class ModuleSubsetPage : SubsetPageBase
    {
        private NumericUpDown _nPickRetry, _nPickDelay, _nPlaceDelay, _nColletInterval;
        private CheckBox _cbColletEnable, _cbBottomInspect, _cbPlacementInspect;

        public ModuleSubsetPage() : base("recipe.moduleSubset") { }

        protected override void BuildEditor(Panel c)
        {
            int yy = 10, dy = 36;
            c.Controls.Add(new Label
            {
                Location = new Point(10, yy), Size = new Size(490, 24),
                Text = "Pick / Place 동작 파라미터",
                Font = UiTheme.SectionFont, ForeColor = Color.DarkSlateGray,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightYellow,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            });
            yy += 30;
            c.Controls.Add(MakeLabel("Pick 재시도 횟수",   10, yy));  _nPickRetry  = MakeNum(220, yy, 1m, 10m, 0);    c.Controls.Add(_nPickRetry);  yy += dy;
            c.Controls.Add(MakeLabel("Pick 후 대기 (ms)",   10, yy)); _nPickDelay  = MakeNum(220, yy, 0m, 1000m, 0);  c.Controls.Add(_nPickDelay);  yy += dy;
            c.Controls.Add(MakeLabel("Place 후 대기 (ms)",  10, yy)); _nPlaceDelay = MakeNum(220, yy, 0m, 1000m, 0);  c.Controls.Add(_nPlaceDelay); yy += dy + 8;

            c.Controls.Add(new Label
            {
                Location = new Point(10, yy), Size = new Size(490, 24),
                Text = "Collet Cleaning",
                Font = UiTheme.SectionFont, ForeColor = Color.DarkSlateGray,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightYellow,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            });
            yy += 30;
            _cbColletEnable = new CheckBox
            {
                Location = new Point(220, yy), Size = new Size(280, 26),
                Text = "Collet Cleaning Enable",
                Font = UiTheme.ButtonFont
            };
            c.Controls.Add(MakeLabel("Collet 활성", 10, yy));  c.Controls.Add(_cbColletEnable); yy += dy;
            c.Controls.Add(MakeLabel("Cleaning 주기 (다이)", 10, yy));  _nColletInterval = MakeNum(220, yy, 1m, 10000m, 0);  c.Controls.Add(_nColletInterval); yy += dy + 8;

            c.Controls.Add(new Label
            {
                Location = new Point(10, yy), Size = new Size(490, 24),
                Text = "Inspection Enable",
                Font = UiTheme.SectionFont, ForeColor = Color.DarkSlateGray,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightYellow,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            });
            yy += 30;
            _cbBottomInspect = new CheckBox
            {
                Location = new Point(220, yy), Size = new Size(280, 26),
                Text = "Bottom Vision 검사",
                Font = UiTheme.ButtonFont
            };
            c.Controls.Add(MakeLabel("Bottom 검사", 10, yy));  c.Controls.Add(_cbBottomInspect); yy += dy;
            _cbPlacementInspect = new CheckBox
            {
                Location = new Point(220, yy), Size = new Size(280, 26),
                Text = "Placement Bin 검사",
                Font = UiTheme.ButtonFont
            };
            c.Controls.Add(MakeLabel("Placement 검사", 10, yy));  c.Controls.Add(_cbPlacementInspect);
        }

        protected override void LoadFromRecipe()
        {
            var m = _project.Module ?? new ModuleSubset();
            _nPickRetry.Value      = m.PickRetryCount;
            _nPickDelay.Value      = m.PickDelayMs;
            _nPlaceDelay.Value     = m.PlaceDelayMs;
            _cbColletEnable.Checked = m.ColletCleanEnable;
            _nColletInterval.Value = m.ColletCleanInterval;
            _cbBottomInspect.Checked    = m.BottomInspectionEnable;
            _cbPlacementInspect.Checked = m.PlacementInspectionEnable;
        }

        protected override void SaveToRecipe()
        {
            var m = _project.Module ?? (_project.Module = new ModuleSubset());
            m.PickRetryCount         = (int)_nPickRetry.Value;
            m.PickDelayMs            = (int)_nPickDelay.Value;
            m.PlaceDelayMs           = (int)_nPlaceDelay.Value;
            m.ColletCleanEnable      = _cbColletEnable.Checked;
            m.ColletCleanInterval    = (int)_nColletInterval.Value;
            m.BottomInspectionEnable     = _cbBottomInspect.Checked;
            m.PlacementInspectionEnable  = _cbPlacementInspect.Checked;
        }
    }
}
