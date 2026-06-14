using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class VisionRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private string _titleI18n;

        public VisionRecipePage()
        {
            _titleI18n = "recipe.inputVision";
            InitializeComponent();
            ApplyTitle();
            BindActionCommands();
        }

        public VisionRecipePage(string titleI18n)
        {
            _titleI18n = titleI18n;
            InitializeComponent();
            ApplyTitle();
            BindActionCommands();
        }

        private void ApplyTitle()
        {
            lblHeader.Tag = "i18n:" + _titleI18n;
            lblHeader.Text = Lang.T(_titleI18n);
            grpCamera.Text = Lang.T(_titleI18n);
        }

        private void BindActionCommands()
        {
            try
            {
                actionCommandPanel.SetItems(new[]
                {
                    CreateAction("GRAB", "GRAB", 0, 0),
                    CreateAction("MATCH", "MATCH", 0, 1),
                    CreateAction("FAST_SHUTTER", "FAST SHUTTER", 0, 2),
                    CreateAction("SMALL_ROI", "SMALL ROI", 1, 0),
                    CreateAction("MATCH_MOVE", "MATCH MOVE", 1, 1),
                    CreateAction("IMAGE_SAVE", "IMAGE SAVE", 1, 2),
                    CreateAction("THETA_MATCH_MOVE", "THETA MATCH MOVE", 2, 0)
                }, 3, 3);
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "VisionActionBindFail", "VisionRecipePage",
                    "Bind action command exception: " + ex.GetType().Name + ": " + ex.Message);
                QMC.Common.MessageDialog.Show(ex.Message, "Vision Action", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private ActionCommandItem CreateAction(string key, string text, int row, int column)
        {
            try
            {
                return new ActionCommandItem
                {
                    Key = key,
                    Text = text,
                    Row = row,
                    Column = column,
                    ExecuteAsync = async () => await ExecuteVisionActionAsync(text)
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteVisionActionAsync(string actionName)
        {
            try
            {
                EventLogger.Write(EventKind.Event, UserSession.Name, "VISION-ACTION",
                    "Click: " + actionName + " (titleI18n=" + _titleI18n + ")");

                var wafer = VisionHub.Wafer;
                switch (actionName)
                {
                    // Wafer Vision GRAB 실행
                    case "GRAB":
                        if (wafer != null && wafer.IsConnected)
                        {
                            bool ok = await wafer.ExposeAsync(0, 3000);
                            QMC.Common.MessageDialog.Show("GRAB " + (ok ? "OK" : "FAIL"), "Vision GRAB",
                                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                            return ok ? 0 : -1;
                        }
                        else
                        {
                            QMC.Common.MessageDialog.Show("Wafer Vision is not connected. (TCP 5100)", "GRAB",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return -1;
                        }

                    // Wafer Vision MATCH 실행
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
                            QMC.Common.MessageDialog.Show(msg, "Vision MATCH",
                                MessageBoxButtons.OK,
                                result != null ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                            return result != null ? 0 : -1;
                        }
                        else
                        {
                            QMC.Common.MessageDialog.Show("Wafer Vision is not connected. (TCP 5100)", "MATCH",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return -1;
                        }

                    // 빠른 셔터 액션 안내
                    case "FAST SHUTTER":
                        QMC.Common.MessageDialog.Show("FAST SHUTTER will call the Vision PC exposure API in a later stage.",
                            "FAST SHUTTER", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return 0;

                    default:
                        QMC.Common.MessageDialog.Show(actionName + " will be implemented in the next stage.",
                            "Vision Action", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return 0;
                }
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "VisionMatchFail", "VisionRecipePage",
                    actionName + " exception: " + ex.GetType().Name + ": " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }
    }
}


