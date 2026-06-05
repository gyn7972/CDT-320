using System;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class MapTransferPage : PageBase
    {
        private Timer _refresh;
        private string _i18nTitle;

        public MapTransferPage() : this("work.page.inputMap")
        {
        }

        public MapTransferPage(string titleI18n)
        {
            _i18nTitle = titleI18n;
            InitializeComponent();
            ApplyTitle();
            WireEvents();

            if (!IsDesignerMode())
            {
                BuildOrFetchMap();
                _refresh = new Timer { Interval = 1500 };
                _refresh.Tick += (s, e) => { try { ApplyLotProgress(); } catch { } };
                _refresh.Start();
            }
        }

        private void ApplyTitle()
        {
            lblHeader.Tag = "i18n:" + _i18nTitle;
            lblHeader.Text = Lang.T(_i18nTitle);
            mapView.Caption = Lang.T(_i18nTitle);
            lblProjectValue.Text = GetCurrentProjectName();
        }

        private void WireEvents()
        {
            mapView.CellClicked += entry =>
            {
                if (entry == null) return;

                lblAxisX.Text = entry.X.ToString("F2");
                lblAxisY.Text = entry.Y.ToString("F2");
                lblBinRank.Text = entry.BinCode.ToString();
                lblDieNum.Text = string.Format("[{0},{1}] / {2}", entry.GridX, entry.GridY,
                    mapView.Map != null ? mapView.Map.TotalCells : 0);
            };
        }

        private string GetCurrentProjectName()
        {
            try
            {
                var lot = LotStorage.ActiveLot;
                if (lot != null && !string.IsNullOrEmpty(lot.RecipeName)) return lot.RecipeName;

                var list = RecipeStore.List();
                if (list != null && list.Count > 0)
                    return System.IO.Path.GetFileNameWithoutExtension(list[0]);
            }
            catch { }

            return "--";
        }

        private void BuildOrFetchMap()
        {
            try
            {
                var list = RecipeStore.List();
                if (list == null || list.Count == 0) return;

                var recipe = RecipeStore.Load(list[0]);
                if (recipe?.Frame == null) return;

                var frame = new DieTapeFrame
                {
                    ObjId = recipe.Frame.FrameSpecName,
                    GridX = Math.Max(1, recipe.Frame.GridX),
                    GridY = Math.Max(1, recipe.Frame.GridY),
                    PitchX = recipe.Frame.PitchX,
                    PitchY = recipe.Frame.PitchY,
                    OriginX = 0,
                    OriginY = 0,
                    Rotate = TapeFrameRotate.None
                };

                mapView.Map = DieMapGenerator.Generate(frame);
                lblChipW.Text = (recipe.Die != null ? recipe.Die.WidthMm : 1.0).ToString("F3");
                lblChipH.Text = (recipe.Die != null ? recipe.Die.HeightMm : 1.0).ToString("F3");
                lblPitchX.Text = recipe.Frame.PitchX.ToString("F3");
                lblPitchY.Text = recipe.Frame.PitchY.ToString("F3");
                lblWaferDia.Text = recipe.Frame.OuterDiameterMm.ToString("F0");
                lblProjectValue.Text = GetCurrentProjectName();
            }
            catch { }
        }

        private void ApplyLotProgress()
        {
            var map = mapView?.Map;
            if (map == null) return;

            var lot = LotStorage.ActiveLot;
            if (lot == null)
            {
                mapView.Invalidate();
                return;
            }

            int processed = lot.ProcessedDies;
            int good = lot.GoodCount;
            int filled = 0;
            int goodFilled = 0;
            foreach (var entry in map.Entries)
            {
                if (filled < processed)
                {
                    if (goodFilled < good)
                    {
                        entry.Result = DieResult.Good;
                        entry.BinCode = QMC.CDT320.Bin.BinCodeMap.GoodBin;
                        goodFilled++;
                    }
                    else
                    {
                        entry.Result = DieResult.NG;
                        entry.BinCode = 110;
                    }

                    filled++;
                }
                else
                {
                    entry.Result = DieResult.Unknown;
                    entry.BinCode = 0;
                }
            }

            lblDieNum.Text = processed + " / " + map.TotalCells;
            mapView.Invalidate();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                _refresh?.Stop();
                _refresh?.Dispose();
            }
            catch { }

            base.OnHandleDestroyed(e);
        }
    }
}
