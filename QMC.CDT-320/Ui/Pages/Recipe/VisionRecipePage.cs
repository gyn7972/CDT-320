using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320.Alarms;
using QMC.CDT320.Logging;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class VisionRecipePage : PageBase
    {
        private readonly string _titleI18n;

        public VisionRecipePage() : this("recipe.inputVision")
        {
        }

        public VisionRecipePage(string titleI18n)
        {
            _titleI18n = titleI18n;
            InitializeComponent();
            ApplyTitle();
            WireEvents();
        }

        private void ApplyTitle()
        {
            lblHeader.Tag = "i18n:" + _titleI18n;
            lblHeader.Text = Lang.T(_titleI18n);
            grpCamera.Text = Lang.T(_titleI18n);
        }

        private void WireEvents()
        {
            btnGrab.Click += async (s, e) => await ExecuteVisionActionAsync("GRAB");
            btnMatch.Click += async (s, e) => await ExecuteVisionActionAsync("MATCH");
            btnFastShutter.Click += async (s, e) => await ExecuteVisionActionAsync("FAST SHUTTER");
            btnSmallRoi.Click += async (s, e) => await ExecuteVisionActionAsync("SMALL ROI");
            btnMatchMove.Click += async (s, e) => await ExecuteVisionActionAsync("MATCH MOVE");
            btnImageSave.Click += async (s, e) => await ExecuteVisionActionAsync("IMAGE SAVE");
            btnThetaMatchMove.Click += async (s, e) => await ExecuteVisionActionAsync("THETA MATCH MOVE");
        }

        private async Task ExecuteVisionActionAsync(string actionName)
        {
            try
            {
                EventLogger.Write(EventKind.Event, UserSession.Name, "VISION-ACTION",
                    "Click: " + actionName + " (titleI18n=" + _titleI18n + ")");

                var wafer = VisionHub.Wafer;
                switch (actionName)
                {
                    case "GRAB":
                        if (wafer != null && wafer.IsConnected)
                        {
                            bool ok = await wafer.ExposeAsync(0, 3000);
                            MessageBox.Show("GRAB " + (ok ? "OK" : "FAIL"), "Vision GRAB",
                                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show("Wafer Vision is not connected. (TCP 5100)", "GRAB",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        break;

                    case "MATCH":
                        if (wafer != null && wafer.IsConnected)
                        {
                            var result = await wafer.MatchAsync("ReticleFinder", 0, 5000);
                            string msg = result == null
                                ? "MATCH failed. Result is null."
                                : "MATCH result: x=" + result.X.ToString("F2") +
                                  ", y=" + result.Y.ToString("F2") +
                                  ", angle=" + result.AngleDeg.ToString("F2") +
                                  ", score=" + result.Score.ToString("F2");
                            MessageBox.Show(msg, "Vision MATCH",
                                MessageBoxButtons.OK,
                                result != null ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show("Wafer Vision is not connected. (TCP 5100)", "MATCH",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        break;

                    case "FAST SHUTTER":
                        MessageBox.Show("FAST SHUTTER will call the Vision PC exposure API in a later stage.",
                            "FAST SHUTTER", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    default:
                        MessageBox.Show(actionName + " will be implemented in the next stage.",
                            "Vision Action", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "VisionMatchFail", "VisionRecipePage",
                    actionName + " exception: " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
