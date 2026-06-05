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
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.IO;
using MechaSys.SoftBricks.Security;
using MechaSys.SoftBricks.Visions;

using QMC.Equipments;

namespace QMC.Hmi.Forms
{
    public partial class GeneralSemiEquipmentMaintenanceForm : MechaSys.SoftBricks.Hmi.Forms.Semi.TabControlEquipmentMaintenanceForm
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralSemiEquipmentMaintenanceForm(GeneralSemiEquipment target) : base(target)
        {
            InitializeComponent();
        }
        public GeneralSemiEquipmentMaintenanceForm() : this(null) { }
        #endregion

        #region Property
        #endregion

        #region Event Handler
        private void buttonSelection_Click(object sender, EventArgs e)
        {
            if (sender == this.buttonDeselectAll)
                this.layoutBox.DeselectAll();
            else if (sender == this.buttonSelectAll)
                this.layoutBox.SelectAll();
        }

        private void LayoutBox_ModuleBoxSelectedChanged(object sender, ControlEventArgs e)
        {
            this.ChangeModuleButtonState();
        }

        private void ServiceState_Transited(object sender, EventArgs e)
        {
            this.ChangeModuleButtonState();
        }

        private void buttonEquipment_Click(object sender, EventArgs e)
        {
            MethodCallerAsyncResult ar = null;
            string message = "";

            if (sender == this.buttonInitializeEquipment)
            {
                TraceLogger.ProcessLogging($"Equipment is initializing");
                message = "Equipment is initializing";
                ar = this.Target.BeginInitialize();
            }
            else if (sender == this.buttonStopEquipment)
            {
                TraceLogger.ProcessLogging($"Equipment is stopping");
                message = "Equipment is stopping";
                ar = this.Target.BeginStop();
            }
            else if (sender == this.buttonClearJobs)
            {
                if (MsgBox.ShowOkCancel(this, "Are you sure to clear jobs ?") != DialogResult.OK) return;

                TraceLogger.ProcessLogging($"Job is clearing");
                message = "Job is clearing";
                ar = this.Target.ClearJobAssistant.BeginExecute();
            }
            else if (sender == this.buttonCleanUp)
            {
                CleanUpSpecification specification;
                CleanUpDialogForm dialogForm = new CleanUpDialogForm();
                DialogResult result = DialogResult.Cancel;

                if ((result = dialogForm.ShowDialog(FormManager.ActiveFormDisplay)) != DialogResult.OK) return;
                specification = dialogForm.Specification;
                TraceLogger.ProcessLogging($"Equipment is cleaning up");
                message = "Equipment is cleaning up";
                ar = this.Target.CleanUpAssistant.BeginExecute(specification);
            }
            else if (sender == this.buttonStartEquipment)
            {
                TraceLogger.ProcessLogging($"Equipment is starting");
                message = "Equipment is starting";
                ar = this.Target.OperationAssistant.BeginStart();
            }
            else if (sender == this.buttonStopAfterWorkEquipment)
            {
                TraceLogger.ProcessLogging($"Equipment is stopping after work");
                message = "Equipment is stopping after work";
                ar = this.Target.OperationAssistant.BeginStopAfterWork();
            }

            if (ar != null)
                WaitingBox.ShowPart(this, ar, message, Sys.Equipment);
        }

        private void buttonModules_Click(object sender, EventArgs e)
        {
            // Revision 1
            string modulesAlias = "";
            MethodCallerAsyncResult ar = null;
            LayoutBox.OwnedSelectedModuleBoxCollection moduleBoxes = this.layoutBox.SelectedModuleBoxes;
            Module[] selectedModules = null;
            string message = string.Empty;

            if (moduleBoxes == null || moduleBoxes.Count == 0)
            {
                MsgBox.ShowInformation("No module is selected");
                return;
            }

            selectedModules = new Module[moduleBoxes.Count];
            for (int i = 0; i < moduleBoxes.Count; i++)
            {
                selectedModules[i] = moduleBoxes[i].SelectedModule;
                modulesAlias += $"{moduleBoxes[i].SelectedModule.Alias}, ";
            }
               

            if (sender == this.buttonInitializeModules)
            {
                TraceLogger.ProcessLogging($"Equipment is initializing partially", $"{modulesAlias}");
                message = "Equipment is initializing partially";
                ar = this.Target.BeginInitializePartially(selectedModules);
            }
            else if (sender == this.buttonStopModules)
            {
                TraceLogger.ProcessLogging($"Equipment is stopping partially", $"{modulesAlias}");
                message = "Equipment is stopping partially";
                ar = this.Target.BeginStopPartially(selectedModules);
            }
            else if (sender == this.buttonInServiceModules)
            {
                TraceLogger.ProcessLogging($"Equipment is changing service to InService partially", $"{modulesAlias}");
                message = "Equipment is changing service to InService partially";
                ar = this.Target.BeginChangeServicePartially(selectedModules, PartServiceStateMachine.StateEnum.InService);
            }
            else if (sender == this.buttonUserSelectModules)
            {
                TraceLogger.ProcessLogging($"Equipment is changing service to UserSelected partially", $"{modulesAlias}");
                message = "Equipment is changing service to UserSelected partially";
                ar = this.Target.BeginChangeServicePartially(selectedModules, PartServiceStateMachine.StateEnum.UserSelected);
            }

            WaitingBox.Show(this, ar, message);
        }
        #endregion

        #region Method
        private void ChangeModuleButtonState()
        {
            IEnumerable<ModuleBox> userSelectedModules = null;
            IEnumerable<ModuleBox> inserviceModules = null;

            if (this.layoutBox.SelectedModuleBoxes == null || this.layoutBox.SelectedModuleBoxes.Count == 0)
            {
                this.buttonStopModules.Enabled = false;
                this.buttonInitializeModules.Enabled = false;
                this.buttonInServiceModules.Enabled = false;
                this.buttonUserSelectModules.Enabled = false;
                return;
            }
            else
            {
                this.buttonStopModules.Enabled = true;
            }

            userSelectedModules = this.layoutBox.SelectedModuleBoxes.Where(x =>
            {
                if (x.SelectedModule.ServiceState.CurrentState.StateValue is PartServiceStateMachine.StateEnum state && state == PartServiceStateMachine.StateEnum.UserSelected)
                    return true;
                else
                    return false;
            });

            inserviceModules = this.layoutBox.SelectedModuleBoxes.Where(x =>
            {
                if (x.SelectedModule.ServiceState.CurrentState.StateValue is PartServiceStateMachine.StateEnum state && state == PartServiceStateMachine.StateEnum.InService)
                    return true;
                else
                    return false;
            });

            if (this.layoutBox.SelectedModuleBoxes.Count == 1)
            {
                if (this.layoutBox.SelectedModuleBoxes[0].SelectedModule.ServiceState.CurrentState.StateValue is PartServiceStateMachine.StateEnum state)
                {
                    if (state == PartServiceStateMachine.StateEnum.UserSelected)
                    {
                        this.buttonInServiceModules.Enabled = true;
                        this.buttonUserSelectModules.Enabled = false;
                    }
                    else
                    {
                        this.buttonInServiceModules.Enabled = false;
                        this.buttonUserSelectModules.Enabled = true;
                    }
                }
                else
                {
                    this.buttonInServiceModules.Enabled = true;
                    this.buttonUserSelectModules.Enabled = true;
                }
            }
            else if (userSelectedModules.Count() == this.layoutBox.SelectedModuleBoxes.Count)
            {
                this.buttonInServiceModules.Enabled = true;
                this.buttonUserSelectModules.Enabled = false;
            }
            else if (inserviceModules.Count() == this.layoutBox.SelectedModuleBoxes.Count)
            {
                this.buttonInServiceModules.Enabled = false;
                this.buttonUserSelectModules.Enabled = true;
            }
            else
            {
                this.buttonInServiceModules.Enabled = true;
                this.buttonUserSelectModules.Enabled = true;
            }

            if (userSelectedModules.Count() == 0)
                this.buttonInitializeModules.Enabled = false;
            else
                this.buttonInitializeModules.Enabled = true;
        }

        protected virtual void PrepareImageSensor()
        {
            ImageSensorCollection points = new ImageSensorCollection();
            ImageSensorCollection buffers;

            buffers = ImageSensorServer.GetImageSensors();
            for(int j = 0; j < buffers.Count; j++)
                points.Add(buffers[j]);

            this.cameraView1.SetPoints(points);
            if(points.Count == 0)
                this.tabPageXImageSensor.Visible = false;
        }
        #endregion

        #region MaintenanceForm Members
        public new GeneralSemiEquipment Target
        {
            get { return base.Target as GeneralSemiEquipment; }
        }

        protected override void OnDisplay()
        {
            this.layoutBox.Display();

            base.OnDisplay();

            if(this.tabPageXImageSensor.Visible == true)
                this.cameraView1.Display();
        }
        #endregion

        #region ChildForm Members
        protected override void OnPrepare()
        {
            base.OnPrepare();

            if (this.DesignMode == true) return;

            this.PrepareImageSensor();

            this.buttonCleanUp.Visible = this.Target != null && this.Target.CleanUpAssistant != null;
            this.buttonClearJobs.Visible = this.Target != null && this.Target.ClearJobAssistant != null;

            if (this.Target == null) return;

            this.Caption = "Equipment";

            this.layoutBox.DrawLayout();

            this.layoutBox.ModuleBoxSelectedChanged += LayoutBox_ModuleBoxSelectedChanged;

            for (int i = 0; i < this.layoutBox.ModuleBoxes.Count; i++)
            {
                if (this.layoutBox.ModuleBoxes[i].SelectedModule != null)
                    this.layoutBox.ModuleBoxes[i].SelectedModule.ServiceState.Transited += ServiceState_Transited;
            }

            this.buttonStartEquipment.Visible = this.Target.OperationAssistant != null;
            this.buttonStopAfterWorkEquipment.Visible = this.Target.OperationAssistant != null;

            this.groupBoxModules.Visible = this.groupBoxSelection.Visible = this.Target.ModuleOperationSpecification.Style == ModuleOperationSpecification.Styles.Enable;

            this.groupBoxEquipment.Text = QMCSystem.Translate(this.groupBoxEquipment.Text);
            this.groupBoxModules.Text = QMCSystem.Translate(this.groupBoxModules.Text);
            this.groupBoxSelection.Text = QMCSystem.Translate(this.groupBoxSelection.Text);
            this.buttonStopEquipment.Text = QMCSystem.Translate(this.buttonStopEquipment.Text);
            this.buttonInitializeEquipment.Text = QMCSystem.Translate(this.buttonInitializeEquipment.Text);
            this.buttonClearJobs.Text = QMCSystem.Translate(this.buttonClearJobs.Text);
            this.buttonCleanUp.Text = QMCSystem.Translate(this.buttonCleanUp.Text);
            this.buttonStartEquipment.Text = QMCSystem.Translate(this.buttonStartEquipment.Text);
            this.buttonStopAfterWorkEquipment.Text = QMCSystem.Translate(this.buttonStopAfterWorkEquipment.Text);
            this.buttonStopModules.Text = QMCSystem.Translate(this.buttonStopModules.Text);
            this.buttonInitializeModules.Text = QMCSystem.Translate(this.buttonInitializeModules.Text);
            this.buttonInServiceModules.Text = QMCSystem.Translate(this.buttonInServiceModules.Text);
            this.buttonUserSelectModules.Text = QMCSystem.Translate(this.buttonUserSelectModules.Text);
            this.buttonSelectAll.Text = QMCSystem.Translate(this.buttonSelectAll.Text);
            this.buttonDeselectAll.Text = QMCSystem.Translate(this.buttonDeselectAll.Text);

            this.tabPageXImageSensor.Text = Sys.Translate(this.tabPageXImageSensor.Text);
        }

        protected override void ManageAccess()
        {
            bool accessibility = true;
            bool authority = true;
            User user = UserManager.GetLogOnUser();

            base.ManageAccess();

            if (UserManager.Configuration.Body.SecurityEnabled == true)
            {
                authority = user != null && user.IsInRole(UserRole.MaintenanceOperate);
            }

            this.groupBoxEquipment.Enabled = accessibility && authority;
            this.groupBoxModules.Enabled = this.groupBoxSelection.Enabled = accessibility && authority;
        }
        #endregion
    }
}