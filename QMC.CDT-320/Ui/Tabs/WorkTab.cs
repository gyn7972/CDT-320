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
        private ReadyProgressDialog _readyProgressDialog;
        private StopProgressDialog _stopProgressDialog;
        private MachineController _statusController;

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
            RegisterActionButton(BtnReady,      "work.ready",      op, () =>
            {
                if (ConfirmRun("Ready", "모션을 Ready(Avoid) 위치로 이동하시겠습니까?"))
                    RunSafe(async c => await RunReadySequenceWithMessageAsync(c), false);
            });
            RegisterActionButton(BtnStart,      "work.start",      op, () =>
            {
                if (ConfirmRun("Start", "장비를 Start 하여 작업을 진행하시겠습니까?"))
                    RunSafe(async c => await c.StartAsync(), false);
            });
            RegisterActionButton(BtnStop,       "work.stop",       op, () => RunSafe(async c => await RunStopSequenceWithMessageAsync(c), false));
            RegisterActionButton(BtnCycleRun,   "work.cycleRun",   op, () =>
            {
                OpenManualSequenceDialog();
            });
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

        public override void AttachHost(Form1 host)
        {
            if (_statusController != null)
            {
                _statusController.StatusChanged -= OnControllerStatusChanged;
                _statusController.ReadySequenceProgressChanged -= OnReadySequenceProgressChanged;
            }

            base.AttachHost(host);

            _statusController = host != null ? host.Controller : null;
            if (_statusController != null)
            {
                _statusController.StatusChanged += OnControllerStatusChanged;
                _statusController.ReadySequenceProgressChanged += OnReadySequenceProgressChanged;
            }

            UpdateCommandButtonStates();
        }

        private void RegisterActionButton(SidebarButton button, string i18nKey, UserLevel minLevel, Action onClick)
        {
            try
            {
                if (button == null)
                    return;

                AccessPolicy.RegisterFeature(i18nKey, minLevel);
                button.Tag = "i18n:" + i18nKey + ";level:" + minLevel;
                button.Text = QMC.CDT_320.Ui.Localization.Lang.T(i18nKey);
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

        private async System.Threading.Tasks.Task RunReadySequenceWithMessageAsync(MachineController controller)
        {
            if (controller == null)
                return;

            // 중복 READY 클릭 방지: '실제로' Ready 시퀀스가 진행 중일 때만 새로 시작하지 않고 기존 팝업을 앞으로 가져온다.
            if (controller.IsReadySequenceRunning)
            {
                if (_readyProgressDialog != null && !_readyProgressDialog.IsDisposed)
                {
                    _readyProgressDialog.Activate();
                    _readyProgressDialog.BringToFront();
                }
                return;
            }

            // 이전 실행의 잔여 팝업이 남아 있으면 정리한다. (남아 있어도 새 팝업이 막히지 않도록)
            try
            {
                if (_readyProgressDialog != null && !_readyProgressDialog.IsDisposed)
                {
                    _readyProgressDialog.Close();
                    _readyProgressDialog.Dispose();
                }
            }
            catch
            {
            }
            _readyProgressDialog = null;

            // 팝업 owner 를 확실히 잡는다. (owner 가 null 이면 메인 폼 뒤로 깔릴 수 있음)
            IWin32Window owner = FindForm() ?? (IWin32Window)Form.ActiveForm;

            ReadyProgressDialog progressDialog = null;
            try
            {
                progressDialog = new ReadyProgressDialog(controller);
                _readyProgressDialog = progressDialog;
                if (owner != null)
                    progressDialog.Show(owner);
                else
                    progressDialog.Show();
                progressDialog.BringToFront();
                progressDialog.Activate();
                progressDialog.Refresh();
            }
            catch (Exception ex)
            {
                progressDialog = null;
                _readyProgressDialog = null;
                QMC.Common.Log.Write("Main", "SYSTEM", "ReadyProgressDialog",
                    "Ready progress dialog open failed: " + ex.Message + " - Failed");
                // 확인 다이얼로그는 정상 동작하므로, 팝업 생성 실패 시 원인을 화면으로도 알린다.
                QMC.Common.MessageDialog.Show(
                    FindForm(),
                    "Ready 진행 팝업을 열지 못했습니다.\n" + ex.Message,
                    "Ready",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            // Show 직후 메시지 루프가 한 번 펌프되어 팝업이 실제로 화면에 그려지도록 양보한다.
            await System.Threading.Tasks.Task.Yield();

            int result = -1;
            try
            {
                result = await controller.RunReadySequenceAsync();
            }
            finally
            {
                // 진행 결과를 마지막으로 한 번 더 반영한 뒤(완료/실패 표시) 잠깐 보여주고 닫는다.
                if (progressDialog != null && !progressDialog.IsDisposed)
                {
                    progressDialog.ApplyProgress(controller.ReadySequenceProgress);
                    await System.Threading.Tasks.Task.Delay(result == 0 ? 700 : 500);
                    try
                    {
                        progressDialog.Close();
                        progressDialog.Dispose();
                    }
                    catch
                    {
                    }
                }

                _readyProgressDialog = null;
            }

            if (result == 0)
            {
                return;
            }

            string reason = string.IsNullOrWhiteSpace(controller.LastActionFailureMessage)
                ? "Ready 실패."
                : controller.LastActionFailureMessage;

            QMC.Common.MessageDialog.Show(
                FindForm(),
                reason,
                "Ready 실패",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private bool ConfirmRun(string actionLabel, string message)
        {
            try
            {
                return QMC.Common.MessageDialog.Show(
                    FindForm(),
                    message,
                    actionLabel,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ConfirmRun",
                    "Run confirmation dialog failed: " + ex.Message + " - Failed");
                return false;
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
                if (_statusController != null)
                {
                    _statusController.StatusChanged -= OnControllerStatusChanged;
                    _statusController.ReadySequenceProgressChanged -= OnReadySequenceProgressChanged;
                    _statusController = null;
                }

                if (_initializationMonitorDialog != null && !_initializationMonitorDialog.IsDisposed)
                    _initializationMonitorDialog.Close();

                if (_readyProgressDialog != null && !_readyProgressDialog.IsDisposed)
                    _readyProgressDialog.Close();

                if (_stopProgressDialog != null && !_stopProgressDialog.IsDisposed)
                    _stopProgressDialog.Close();
            }
            catch
            {
            }
            finally
            {
                base.OnHandleDestroyed(e);
            }
        }

        private void OpenManualSequenceDialog()
        {
            if (Host == null || Host.Controller == null)
            {
                QMC.Common.MessageDialog.Show(FindForm(), "Machine Controller를 찾을 수 없습니다.", "Cycle Run", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new ManualSequenceDialog(Host.Controller))
                dlg.ShowDialog(FindForm());
        }

        private async System.Threading.Tasks.Task RunStopSequenceWithMessageAsync(MachineController controller)
        {
            if (controller == null)
                return;

            if (_stopProgressDialog != null && !_stopProgressDialog.IsDisposed)
            {
                _stopProgressDialog.Activate();
                _stopProgressDialog.BringToFront();
                return;
            }

            IWin32Window owner = FindForm() ?? (IWin32Window)Form.ActiveForm;
            StopProgressDialog progressDialog = null;
            try
            {
                progressDialog = new StopProgressDialog(controller);
                _stopProgressDialog = progressDialog;
                if (owner != null)
                    progressDialog.Show(owner);
                else
                    progressDialog.Show();
                progressDialog.BringToFront();
                progressDialog.Activate();
                progressDialog.Refresh();
            }
            catch (Exception ex)
            {
                progressDialog = null;
                _stopProgressDialog = null;
                QMC.Common.Log.Write("Main", "SYSTEM", "StopProgressDialog",
                    "Stop progress dialog open failed: " + ex.Message + " - Failed");
            }

            await System.Threading.Tasks.Task.Yield();

            try
            {
                await controller.StopAsync();

                if (progressDialog != null && !progressDialog.IsDisposed)
                {
                    await progressDialog.WaitForStopCompleteAsync();
                    await System.Threading.Tasks.Task.Delay(controller.Status == EquipmentStatus.Alarm ? 500 : 700);
                }
            }
            finally
            {
                if (progressDialog != null && !progressDialog.IsDisposed)
                {
                    try
                    {
                        progressDialog.Close();
                        progressDialog.Dispose();
                    }
                    catch
                    {
                    }
                }

                _stopProgressDialog = null;
            }
        }

        private void OnControllerStatusChanged(EquipmentStatus status)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<EquipmentStatus>(OnControllerStatusChanged), status);
                return;
            }

            UpdateCommandButtonStates();
        }

        private void OnReadySequenceProgressChanged(MachineReadyProgress progress)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<MachineReadyProgress>(OnReadySequenceProgressChanged), progress);
                return;
            }

            UpdateCommandButtonStates();
        }

        private void UpdateCommandButtonStates()
        {
            MachineController controller = Host != null ? Host.Controller : _statusController;
            EquipmentStatus status = controller != null ? controller.Status : EquipmentStatus.Idle;
            bool readyRunning = controller != null && controller.IsReadySequenceRunning;
            bool autoRunning = status == EquipmentStatus.AutoRunning;
            bool manualRunning = status == EquipmentStatus.ManualRunning;

            SetCommandButtonEnabled(BtnInit, !autoRunning);
            SetCommandButtonEnabled(BtnReady, !autoRunning);
            SetCommandButtonEnabled(BtnCycleRun, !autoRunning);

            ClearCommandButtonState(BtnReady);
            ClearCommandButtonState(BtnStart);
            ClearCommandButtonState(BtnStop);
            ClearCommandButtonState(BtnCycleRun);
            ClearCommandButtonState(BtnResetAlarm);

            if (readyRunning)
                SetCommandButtonState(BtnReady, System.Drawing.Color.FromArgb(0xF5, 0xC7, 0x18), System.Drawing.Color.Black);
            else if (status == EquipmentStatus.Ready)
                SetCommandButtonState(BtnReady, System.Drawing.Color.FromArgb(0x2E, 0x7D, 0x32), System.Drawing.Color.White);

            if (autoRunning)
            {
                SetCommandButtonState(BtnStart, System.Drawing.Color.FromArgb(0xE8, 0x5D, 0x1A), System.Drawing.Color.White);
            }
            else if (manualRunning)
            {
                SetCommandButtonState(BtnStart, System.Drawing.Color.FromArgb(0x03, 0xA9, 0xF4), System.Drawing.Color.White);
            }

            if (status == EquipmentStatus.Stopped)
                SetCommandButtonState(BtnStop, System.Drawing.Color.FromArgb(0xB7, 0x1C, 0x1C), System.Drawing.Color.White);

            if (status == EquipmentStatus.CycleStopped)
                SetCommandButtonState(BtnStop, System.Drawing.Color.FromArgb(0xF5, 0xC7, 0x18), System.Drawing.Color.Black);

            if (status == EquipmentStatus.Alarm)
                SetCommandButtonState(BtnResetAlarm, System.Drawing.Color.FromArgb(0xC6, 0x28, 0x28), System.Drawing.Color.White);
        }

        private static void SetCommandButtonEnabled(SidebarButton button, bool enabled)
        {
            if (button == null)
                return;

            if (button.Enabled != enabled)
                button.Enabled = enabled;
        }

        private static void SetCommandButtonState(SidebarButton button, System.Drawing.Color backColor, System.Drawing.Color foreColor)
        {
            if (button == null)
                return;

            button.StateBackColor = backColor;
            button.StateForeColor = foreColor;
        }

        private static void ClearCommandButtonState(SidebarButton button)
        {
            if (button == null)
                return;

            button.StateBackColor = null;
            button.StateForeColor = null;
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
