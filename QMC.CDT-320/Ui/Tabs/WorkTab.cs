using System;
using System.ComponentModel;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Pages.Material;
using QMC.CDT_320.Ui.Pages.Work;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    /// <summary>작업 탭 - 장비 전체 제어 버튼과 작업 화면을 구성합니다.</summary>
    public partial class WorkTab : TabBase
    {
        private InitializationMonitorDialog _initializationMonitorDialog;

        public WorkTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            SetSidebarHeader("tab.work");
            const UserLevel op = UserLevel.Operator;
            const UserLevel en = UserLevel.Engineer;
            const UserLevel mt = UserLevel.Maintenance;

            RegisterSidebarButton(BtnMain, "work.page.main", op, () => new WorkMainPage());

            RegisterActionButton(BtnInit,       "work.init",       op, OpenInitializationMonitor);
            RegisterActionButton(BtnStart,      "work.start",      op, () => RunSafe(async c => await c.StartAsync(), false));
            RegisterActionButton(BtnStop,       "work.stop",       op, () => RunSafe(async c =>
            {
                await c.StopSequenceAsync();
                await c.StopAsync();
            }, false));
            RegisterActionButton(BtnCycleRun,   "work.cycleRun",   op, () => RunSafe(async c => await c.RunProcessSequenceStepAsync(), false));
            RegisterActionButton(BtnCycleStop,  "work.cycleStop",  op, () => RunSafe(async c => await c.CycleStopAsync(), false));
            RegisterActionButton(BtnResetAlarm, "work.resetAlarm", en, () => RunSafe(async c => await c.ResetAlarmAsync()));
            RegisterActionButton(BtnShutdown,   "work.shutdown",   mt, () => RunSafe(async c => await c.ShutdownAsync()));
            RegisterActionButton(BtnEStop,      "work.estop",      op, () => RunSafe(async c => await c.EmergencyStopAsync()));
            RegisterActionButton(BtnInputCst,   "work.inputCst",   op, OpenInputCstStatus);
            RegisterActionButton(BtnOutputCst,  "work.outputCst",  op, OpenOutputCstStatus);

            RegisterModeButton(BtnColletMode,      "work.colletMode",      en, () => new ColletChangeDialog());
            RegisterModeButton(BtnNeedleMode,      "work.needleMode",      mt, () => new NeedleChangeDialog());
            RegisterModeButton(BtnSelfCheckMode,   "work.selfCheckMode",   en, () => new SelfInspectionDialog());
            RegisterModeButton(BtnAutoPosMode,     "work.autoPosMode",     mt, () => new AutoPositionDialog());
            RegisterModeButton(BtnColletCleanMode, "work.colletCleanMode", en, () => new ColletCleaningDialog());
            RegisterModeButton(BtnColletCheckMode, "work.colletCheckMode", en, () => new PositionCheckDialog());
            RegisterModeButton(BtnPosCheck,        "work.posCheck",        en, () => new PositionCheckDialog());
            RegisterModeButton(BtnNeedlePosMode,   "work.needlePosMode",   en, () => new PositionCheckDialog());

            RegisterSidebarButton(BtnInputMapTransfer,  "work.inputMapTransfer",  op, () => new InputStageMapTransferPage("work.page.inputMap"));
            RegisterSidebarButton(BtnOutputMapTransfer, "work.outputMapTransfer", op, () => new OutputStageMapTransferPage("work.page.outputMap"));
            RegisterSidebarButton(BtnVisionAlign,       "work.visionAlign",       en, () => new VisionAlignPage());
            RegisterSidebarButton(BtnWaferMapOpen,      "work.waferMapOpen",      en, () => new WaferMapOpenPage());
            RegisterSidebarButton(BtnDieMap,            "work.dieMap",            en, () => new DieMapPage());
        }

        private void RegisterActionButton(SidebarButton button, string i18nKey, UserLevel minLevel, Action onClick)
        {
            try
            {
                RegisterSidebarButton(button, i18nKey, minLevel, () => new WorkMainPage());
                button.Click += (s, e) => onClick?.Invoke();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "RegisterActionButton", "Work action button bind failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void RegisterModeButton(SidebarButton button, string i18nKey, UserLevel minLevel, Func<Form> dlgFactory)
        {
            try
            {
                RegisterSidebarButton(button, i18nKey, minLevel, () => new QMC.CDT_320.Ui.Pages.PlaceholderPage(i18nKey));
                button.Click += (s, e) =>
                {
                    using (var dlg = dlgFactory()) dlg.ShowDialog(FindForm());
                };
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "RegisterModeButton", "Work mode button bind failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private async void RunSafe(Func<MachineController, System.Threading.Tasks.Task> action, bool showFailureDialog = true)
        {
            try
            {
                if (Host == null || Host.Controller == null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunSafe", "Work action failed: Machine controller is not ready. - Failed");
                    if (showFailureDialog)
                        QMC.Common.MessageDialog.Show(FindForm(), "Machine Controller를 찾을 수 없습니다.", "Work", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                await action(Host.Controller);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "RunSafe", "Work action failed: " + ex.Message + " - Failed");
                if (showFailureDialog)
                    QMC.Common.MessageDialog.Show(FindForm(), "Work action failed:\n" + ex.Message, "Work", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void OpenInitializationMonitor()
        {
            try
            {
                if (Host == null || Host.Controller == null)
                {
                    QMC.Common.MessageDialog.Show(FindForm(), "Machine Controller를 찾을 수 없습니다.", "Work", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_initializationMonitorDialog != null && !_initializationMonitorDialog.IsDisposed)
                {
                    _initializationMonitorDialog.Activate();
                    _initializationMonitorDialog.BringToFront();
                    return;
                }

                _initializationMonitorDialog = new InitializationMonitorDialog(Host.Controller);
                _initializationMonitorDialog.FormClosed += delegate { _initializationMonitorDialog = null; };
                _initializationMonitorDialog.Show(FindForm());
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OpenInitializationMonitor", "Initialization monitor dialog failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(FindForm(), "초기화 모니터를 열 수 없습니다.\n" + ex.Message, "Work", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                if (_initializationMonitorDialog != null && !_initializationMonitorDialog.IsDisposed)
                    _initializationMonitorDialog.Close();
            }
            catch
            {
            }
            finally
            {
                base.OnHandleDestroyed(e);
            }
        }

        private async void RunSafe(Func<MachineController, System.Threading.Tasks.Task<int>> action, bool showFailureDialog = true)
        {
            try
            {
                if (Host == null || Host.Controller == null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunSafe", "Work action failed: Machine controller is not ready. - Failed");
                    if (showFailureDialog)
                        QMC.Common.MessageDialog.Show(FindForm(), "Machine Controller를 찾을 수 없습니다.", "Work", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int result = await action(Host.Controller);
                if (result != 0)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunSafe", "Work action failed: return=" + result + " - Failed");
                    string message = string.IsNullOrEmpty(Host.Controller.LastActionFailureMessage)
                        ? "작업 수행에 실패했습니다.\nAlarm/Event Log를 확인하세요."
                        : Host.Controller.LastActionFailureMessage;
                    if (showFailureDialog)
                        QMC.Common.MessageDialog.Show(FindForm(), message, "Work", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "RunSafe", "Work action failed: " + ex.Message + " - Failed");
                if (showFailureDialog)
                    QMC.Common.MessageDialog.Show(FindForm(), "Work action failed:\n" + ex.Message, "Work", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void OpenInputCstStatus()
        {
            try
            {
                using (var dlg = new CstStatusDialog(isInput: true)) dlg.ShowDialog(FindForm());
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OpenInputCstStatus", "Input cassette status dialog failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void OpenOutputCstStatus()
        {
            try
            {
                using (var dlg = new CstStatusDialog(isInput: false)) dlg.ShowDialog(FindForm());
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OpenOutputCstStatus", "Output cassette status dialog failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}
