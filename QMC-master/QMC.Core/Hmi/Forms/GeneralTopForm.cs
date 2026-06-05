using MechaSys.SoftBricks.EventReports;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.Hmi.Forms;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks.Secs.Hmi.Forms.Semi;
using MechaSys.SoftBricks.Secs;
using MechaSys.SoftBricks.Security;
using MechaSys.SoftBricks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.Hmi.Forms
{
    public partial class GeneralTopForm : MechaSys.SoftBricks.Hmi.Forms.Semi.TopForm
    {
        #region Field
        private DisplayForm m_FormAlarm;
        private ModuleSelectionForm m_FormModule;
        private TerminalServiceForm m_FormTerminalService;

        private EventReportReceiver m_EventReceiver;
        #endregion

        #region Constructor
        public GeneralTopForm()
        {
            InitializeComponent();

            this.m_FormAlarm = new AlarmViewerForm();
            this.m_FormTerminalService = new TerminalServiceForm();
        }
        #endregion

        #region Event Handlers
        private void buttonAlarm_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            if(FormManager.ActiveFormDisplay != null && FormManager.ActiveFormDisplay == this.m_FormAlarm)
            {
                if(FormManager.PreviousFormDisplay == null) return;
                FormManager.HideWindow(FormManager.ActiveFormDisplay);
            }
            else
            {
                // show alarm form
                if(this.m_FormAlarm != null)
                    FormManager.ShowWindow(this.m_FormAlarm);
            }

            this.Cursor = Cursors.Default;
        }

        private void buttonBuzzer_Click(object sender, EventArgs e)
        {
            if(Sys.Equipment == null || Sys.Equipment.Indicator == null || Sys.Equipment.Indicator.BuzzerManager == null) return;
            Sys.Equipment.Indicator.BuzzerManager.Quiet();
        }

        private void buttonTerminalService_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            if(FormManager.ActiveFormDisplay == this.m_FormTerminalService)
            {
                FormManager.HideWindow(FormManager.ActiveFormDisplay);
            }
            else
            {
                // show alarm form
                if(this.m_FormTerminalService != null)
                    FormManager.ShowWindow(this.m_FormTerminalService);
            }

            this.Cursor = Cursors.Default;
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            string text = "Do you want to exit program?";
            DialogResult result = DialogResult.Cancel;

            if((result = MsgBox.ShowOkCancel(text)) != DialogResult.OK) return;

            // Revision 3
            FormManager.MdiParent.Close();
        }

        private void buttonModule_Click(object sender, EventArgs e)
        {
            if(Sys.Equipment == null) return;
            if(this.m_FormModule == null || this.m_FormModule.IsDisposed == true)
            {
                this.m_FormModule = new ModuleSelectionForm();
                this.m_FormModule.ModuleSelector.SelectedModuleChanged += new SelectedModuleChangedEventHandler(ModuleSelector_SelectedModuleChanged);
            }
            Point location = this.buttonModule.Parent.PointToScreen(this.buttonModule.Location);
            location.X = location.X + this.buttonModule.Width / 2 - this.m_FormModule.Width / 2;
            location.Y = location.Y + this.buttonModule.Height / 2;
            this.m_FormModule.Location = location;

            this.m_FormModule.Show();
            this.m_FormModule.BringToFront();
        }

        private void ModuleSelector_SelectedModuleChanged(object sender, MechaSys.SoftBricks.Hmi.Controls.SelectedModuleChangedEventArgs e)
        {
            Module selectedModule = null;

            this.Cursor = Cursors.WaitCursor;
            this.m_FormModule.Hide();
            selectedModule = Sys.Equipment.Modules.GetByName(e.ModuleName);
            if(Sys.Equipment.SelectedModule != selectedModule && selectedModule != null)
            {
                Sys.Equipment.SelectedModule = selectedModule;
            }
            this.Cursor = Cursors.Default;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            EventReport report = null;
            TimeoutChecker timeout = new TimeoutChecker(this.timer.Interval / 4, true);

            while(timeout.IsCompleted == false)
            {
                report = this.m_EventReceiver.Dequeue();
                if(report == null) return;

                this.OnArrivedEventReport(report);
            }
        }
        #endregion

        #region Method
        private bool GetAccessibility(string function)
        {
            bool accessibility = true;
            User user = UserManager.GetLogOnUser();

            if(UserManager.Configuration.Body.SecurityEnabled == false) return true;

            if(function == "Module")
                accessibility = user != null;

            if(function == "Exit")
                accessibility = user != null && user.IsInRole(UserRole.ApplicationExit);

            return accessibility;
        }

        protected virtual void SetLogoImage()
        {
            Image logo = null;
            string path = Path.Combine(Sys.Directory.Image, "Logo.png");
            FileInfo fileInfo = new FileInfo(path);

            if(fileInfo.Exists == false) return;

            logo = Image.FromFile(path);
            if(logo == null) return;

            if(logo.Height <= this.pictureBoxLogo.Height && logo.Width <= this.pictureBoxLogo.Width)
            {
                // if logo size is less than picture box
                this.pictureBoxLogo.SizeMode = PictureBoxSizeMode.CenterImage;
            }
            else
            {
                // reduce the size of picture box 
                if(logo.Width - this.pictureBoxLogo.Width < logo.Height - this.pictureBoxLogo.Height)
                    this.pictureBoxLogo.Width = logo.Width * this.pictureBoxLogo.Height / logo.Height;
                else
                    this.pictureBoxLogo.Height = logo.Height * this.pictureBoxLogo.Width / logo.Width;
                // change location
                this.pictureBoxLogo.Location = new Point(this.pictureBoxLogo.Location.X, (this.Height - this.pictureBoxLogo.Height) / 2);
                this.pictureBoxLogo.SizeMode = PictureBoxSizeMode.StretchImage;
            }

            this.pictureBoxLogo.Image = logo;
        }

        protected virtual void OnArrivedEventReport(EventReport e)
        {
            if(e is AlarmPostedEventReport)
            #region
            {
                if(FormManager.ActiveFormDisplay != this.m_FormAlarm)
                    FormManager.ShowWindow(this.m_FormAlarm);
                this.buttonAlarm.UsageSecondaryEnableImage = true;
            }
            #endregion
            else if(e is AlarmRecoveredEventReport)
            #region
            {
                bool presence = false;
                for(int i = 0; i < AlarmManager.Agents.Count; i++)
                {
                    if(AlarmManager.Agents[i].Alarms.Count > 0)
                    {
                        presence = true;
                        break;
                    }
                }
                this.buttonAlarm.UsageSecondaryEnableImage = presence;
            }
            #endregion
            else if(e is TerminalDisplayBroadcastEventReport ||
                e is TerminalDisplayMultiEventReport ||
                e is TerminalDisplaySingleEventReport)
            #region
            {
                if(e is TerminalDisplayBroadcastEventReport)
                {
                    TerminalDisplayBroadcastEventReport terminalReport = e as TerminalDisplayBroadcastEventReport;
                    this.m_FormTerminalService.TerminalServiceBox.AddMessage(terminalReport);
                }
                else if(e is TerminalDisplayMultiEventReport)
                {
                    TerminalDisplayMultiEventReport terminalReport = e as TerminalDisplayMultiEventReport;
                    this.m_FormTerminalService.TerminalServiceBox.AddMessage(terminalReport);
                }
                else if(e is TerminalDisplaySingleEventReport)
                {
                    TerminalDisplaySingleEventReport terminalReport = e as TerminalDisplaySingleEventReport;
                    this.m_FormTerminalService.TerminalServiceBox.AddMessage(terminalReport);
                }
                else
                    return;

                if(FormManager.ActiveFormDisplay != this.m_FormTerminalService && this.m_FormTerminalService.PopUp == true)
                    FormManager.ShowWindow(this.m_FormTerminalService);
            }
            #endregion
            else if(e is PartBehaviorStateChangedEventReport)
            {
                PartBehaviorStateChangedEventReport behavior = e as PartBehaviorStateChangedEventReport;

                if(behavior.CurrentStateValue == PartBehaviorStateMachine.StateEnum.Paused || behavior.PreviousStateValue == PartBehaviorStateMachine.StateEnum.Paused)
                    this.ManageAccess();
            }
        }
        #endregion

        #region ChildForm Members
        protected override void ManageAccess()
        {
            this.buttonAlarm.Enabled = this.GetAccessibility("Alarm");
            this.buttonBuzzer.Enabled = this.GetAccessibility("Buzzer");
            this.buttonExit.Enabled = this.GetAccessibility("Exit");
            this.buttonModule.Enabled = this.GetAccessibility("Module");
            this.buttonTerminalService.Enabled = this.GetAccessibility("TerminalService");
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();

            if(this.DesignMode == true) return;

            this.m_EventReceiver = new EventReportReceiver();
            this.m_EventReceiver.UsageEvent = false;
            EventReportDispatcher.AddReceiver(this.m_EventReceiver);

            #region Logo
            this.SetLogoImage();
            #endregion

            #region Tower Lamp
            this.lampBox.Enabled = true;
            if(Sys.Equipment != null && Sys.Equipment.Indicator != null && Sys.Equipment.Indicator.TowerLampManager != null)
            {
                if(Sys.Equipment.Indicator.TowerLampManager.Inverted == false)
                {
                    this.lampBox.Lamp1 = (Sys.Equipment.Indicator.TowerLampManager.InstalledLamps & TowerLampNames.Red) == TowerLampNames.Red ? TowerLampBox.LampType.Red : TowerLampBox.LampType.None;
                    this.lampBox.Lamp2 = (Sys.Equipment.Indicator.TowerLampManager.InstalledLamps & TowerLampNames.Yellow) == TowerLampNames.Yellow ? TowerLampBox.LampType.Yellow : TowerLampBox.LampType.None;
                    this.lampBox.Lamp3 = (Sys.Equipment.Indicator.TowerLampManager.InstalledLamps & TowerLampNames.Green) == TowerLampNames.Green ? TowerLampBox.LampType.Green : TowerLampBox.LampType.None;
                    this.lampBox.Lamp4 = (Sys.Equipment.Indicator.TowerLampManager.InstalledLamps & TowerLampNames.Blue) == TowerLampNames.Blue ? TowerLampBox.LampType.Blue : TowerLampBox.LampType.None;
                }
                else
                {
                    this.lampBox.Lamp1 = (Sys.Equipment.Indicator.TowerLampManager.InstalledLamps & TowerLampNames.Blue) == TowerLampNames.Blue ? TowerLampBox.LampType.Blue : TowerLampBox.LampType.None;
                    this.lampBox.Lamp2 = (Sys.Equipment.Indicator.TowerLampManager.InstalledLamps & TowerLampNames.Green) == TowerLampNames.Green ? TowerLampBox.LampType.Green : TowerLampBox.LampType.None;
                    this.lampBox.Lamp3 = (Sys.Equipment.Indicator.TowerLampManager.InstalledLamps & TowerLampNames.Yellow) == TowerLampNames.Yellow ? TowerLampBox.LampType.Yellow : TowerLampBox.LampType.None;
                    this.lampBox.Lamp4 = (Sys.Equipment.Indicator.TowerLampManager.InstalledLamps & TowerLampNames.Red) == TowerLampNames.Red ? TowerLampBox.LampType.Red : TowerLampBox.LampType.None;
                }

                this.lampBox.Location = new Point(this.lampBox.Location.X, (this.Height - this.lampBox.Height) / 2);

                Sys.Equipment.Indicator.TowerLampManager.TowerLamps.Add(this.lampBox);
            }
            #endregion

            #region alarm
            // alarm button's secondary image
            this.buttonAlarm.SecondaryEnableImage = MechaSys.SoftBricks.Resources.ImageResourceManager.Instance.GetImage(MechaSys.SoftBricks.Hmi.Resources.ImageEnum.AlarmPostingEnabled.ToString());

            if(0 < AlarmManager.GetCount())
            {
                if(FormManager.ActiveFormDisplay != this.m_FormAlarm)
                    FormManager.ShowWindow(this.m_FormAlarm);
                this.buttonAlarm.UsageSecondaryEnableImage = true;
            }
            #endregion

            #region secs state viewer
            if(Sys.Equipment != null && Sys.Equipment.EventReportProcessor is SecsEquipmentEventReportProcessor)
            {
                GemService gem = ((SecsEquipmentEventReportProcessor)Sys.Equipment.EventReportProcessor).SecsService as GemService;
                this.panelSecs.Visible = true;
                this.gemStateViewer.Visible = gem != null;
                this.gemStateViewer.GemService = gem;
            }
            else
            {
                this.panelSecs.Visible = false;
                this.gemStateViewer.Visible = false;
                this.gemStateViewer.GemService = null;
            }
            #endregion

            this.buttonModule.Visible = 1 < Sys.Equipment.Modules.Count;

            this.buttonAlarm.Text = QMCSystem.Translate(this.buttonAlarm.Text);
            this.buttonBuzzer.Text = QMCSystem.Translate(this.buttonBuzzer.Text);
            this.buttonExit.Text = QMCSystem.Translate(this.buttonExit.Text);
            this.buttonModule.Text = QMCSystem.Translate(this.buttonModule.Text);
            this.buttonTerminalService.Text = QMCSystem.Translate(this.buttonTerminalService.Text);
        }

        protected override void OnShowing(EventArgs e)
        {
            base.OnShowing(e);

            this.timer.Enabled = true;
        }
        #endregion
    }
}
