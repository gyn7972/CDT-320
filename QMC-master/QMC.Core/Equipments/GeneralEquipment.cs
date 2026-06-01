using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks.Net;
using MechaSys.SoftBricks.Processes;
using MechaSys.SoftBricks.Transfer;
using MechaSys.SoftBricks;
using QMC.Hmi.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Equipments
{
    #region GeneralEquipment
    public abstract class GeneralEquipment : Equipment,
        IElementConfigurable<GeneralEquipmentConfiguration>
    {
        #region Field
        private TransferableModuleReadOnlyCollection m_TransferableModules;
        private TransferableModuleReadOnlyCollection m_HybridTransferableModules;
        private ProcessModuleReadOnlyCollection m_ProcessModules;

        private GeneralEquipmentConfiguration m_Configuration;

        private CommunicatorGroup m_Communicators;
        private BuzzerGroup m_Buzzers;
        private TowerLampGroup m_TowerLamps;
        private EmergencySensorGroup m_EmergencySensors;
        private EmergencySensorGroup m_ResourceSensors;
        #endregion

        #region Constructor
        public GeneralEquipment(Nameable nameable)
            : base(nameable)
        {
            #region variables
            this.TransferableModules = new TransferableModuleReadOnlyCollection();
            this.HybridTransferableModules = new TransferableModuleReadOnlyCollection();
            this.ProcessModules = new ProcessModuleReadOnlyCollection();
            #endregion

            #region elements
            #endregion

            #region parts
            this.Communicators = new CommunicatorGroup(new Nameable("Communicators"));

            this.Buzzers = new BuzzerGroup(new Nameable("Buzzers"));
            this.TowerLamps = new TowerLampGroup(new Nameable("TowerLamps"));

            this.EmergencySensors = new EmergencySensorGroup(new Nameable("EmergencySensors"));
            this.ResourceSensors = new EmergencySensorGroup(new Nameable("ResourceSensors"));
            #endregion
        }
        public GeneralEquipment() : this(new Nameable()) { }
        #endregion

        #region Property
        #region variables
        public TransferableModuleReadOnlyCollection TransferableModules
        {
            get { return this.m_TransferableModules; }
            private set { this.m_TransferableModules = value; }
        }

        public TransferableModuleReadOnlyCollection HybridTransferableModules
        {
            get { return this.m_HybridTransferableModules; }
            private set { this.m_HybridTransferableModules = value; }
        }

        public ProcessModuleReadOnlyCollection ProcessModules
        {
            get { return this.m_ProcessModules; }
            private set { this.m_ProcessModules = value; }
        }
        #endregion

        #region parts
        [Composite]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.ZeroOrOne)]
        public CommunicatorGroup Communicators
        {
            get { return this.m_Communicators; }
            protected set
            {
                this.Parts.SetNullable(this.m_Communicators, value);
                this.m_Communicators = value;
            }
        }

        [Composite]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.ZeroOrOne)]
        public BuzzerGroup Buzzers
        {
            get { return this.m_Buzzers; }
            protected set
            {
                this.Parts.SetNullable(this.m_Buzzers, value);
                this.m_Buzzers = value;
            }
        }

        [Composite]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.ZeroOrOne)]
        public TowerLampGroup TowerLamps
        {
            get { return this.m_TowerLamps; }
            protected set
            {
                this.Parts.SetNullable(this.m_TowerLamps, value);
                this.m_TowerLamps = value;
            }
        }

        [Composite]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.ZeroOrOne)]
        public EmergencySensorGroup EmergencySensors
        {
            get { return this.m_EmergencySensors; }
            protected set
            {
                this.Parts.SetNullable(this.m_EmergencySensors, value);
                this.m_EmergencySensors = value;
            }
        }

        [Composite]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.ZeroOrOne)]
        public EmergencySensorGroup ResourceSensors
        {
            get { return this.m_ResourceSensors; }
            protected set
            {
                this.Parts.SetNullable(this.m_ResourceSensors, value);
                this.m_ResourceSensors = value;
            }
        }
        #endregion
        #endregion

        #region Event Handlers
        private void LifeState_AfterTransit(object sender, ElementLifeAfterStateTransitionEventArgs e)
        {
            if(e.CurrentStateValue == ElementLifeStateMachine.StateEnum.Creating)
            {
                #region MenuFormProvider
                this.MenuFormProvider.FormTypes.AddForcefully(EquipmentMenuFormProvider.FormEnum.Top, typeof(GeneralTopForm));
                this.MenuFormProvider.FormTypes.AddForcefully(EquipmentMenuFormProvider.FormEnum.Bottom, typeof(GeneralBottomForm));

                this.MenuFormProvider.FormTypes.AddForcefully(EquipmentMenuFormProvider.FormEnum.SideOperation, typeof(GeneralSideOperationForm));
                this.MenuFormProvider.FormTypes.AddForcefully(EquipmentMenuFormProvider.FormEnum.SideConfiguration, typeof(GeneralSideConfigurationForm));
                this.MenuFormProvider.FormTypes.AddForcefully(EquipmentMenuFormProvider.FormEnum.SideMaintenance, typeof(GeneralSideMaintenanceForm));
                this.MenuFormProvider.FormTypes.AddForcefully(EquipmentMenuFormProvider.FormEnum.SideRecipe, typeof(GeneralSideRecipeForm));
                this.MenuFormProvider.FormTypes.AddForcefully(EquipmentMenuFormProvider.FormEnum.SideDataLog, typeof(GeneralSideDataLogForm));
                this.MenuFormProvider.FormTypes.AddForcefully(EquipmentMenuFormProvider.FormEnum.SideUtility, typeof(GeneralSideUtilityForm));
                #endregion

                this.MakeModuleList();
            }
        }
        #endregion

        #region Method
        private void MakeModuleList()
        {
            TransferableModule[] transferableModules = this.Modules.GetByType<TransferableModule>();
            TransferableModuleCollection hybridTransferableMoudles = new TransferableModuleCollection();
            ProcessModule[] processModules = this.Modules.GetByType<ProcessModule>();

            this.TransferableModules = new TransferableModuleReadOnlyCollection(transferableModules);
            foreach(TransferableModule item in this.TransferableModules)
            {
                if(item is ITransferred == false) continue;
                hybridTransferableMoudles.Add(item);
            }
            this.HybridTransferableModules = new TransferableModuleReadOnlyCollection(hybridTransferableMoudles);

            this.ProcessModules = new ProcessModuleReadOnlyCollection(processModules);
        }
        #endregion

        #region IElementConfigurable<GeneralEquipmentConfiguration> Members
        /// <summary>
        /// 객체의 구성 정보를 가져옵니다.
        /// </summary>
        public GeneralEquipmentConfiguration Configuration
        {
            get { return this.m_Configuration; }
            protected set { this.m_Configuration = value; }
        }
        #endregion

        #region IElementConfigurable Members
        /// <summary>
        /// 객체의 구성 정보를 가져옵니다.
        /// </summary>
        ElementConfiguration IElementConfigurable.Configuration
        {
            get { return this.Configuration; }
        }

        /// <summary>
        /// 주어진 구성 정보를 반영합니다.
        /// </summary>
        /// <param name="configuration">구성 정보입니다.</param>
        /// <returns>작업에 대한 결과를 반환합니다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
        public int ApplyConfiguration(ElementConfiguration configuration)
        {
            int ret = 0;
            GeneralEquipmentConfiguration specialized = configuration as GeneralEquipmentConfiguration;

            if(specialized == null)
                throw new ArgumentNullException("configuration");

            if(this.Configuration == null || this.Configuration == configuration)
                this.Configuration = MechaSys.SoftBricks.DotNetUtility.CopyUtility.GetDeepCopy<GeneralEquipmentConfiguration>(specialized);
            if(this.Configuration.Revision < specialized.Revision)
                this.Configuration.Revision = specialized.Revision;

            if((ret = this.OnApplyConfiguration(specialized.Body)) != 0) return ret;

            // Warning)
            // 경우에 따라서 수정이 필요하다. (ex: lock 문)

            this.Configuration.Body = specialized.Body;

            return ret;
        }

        /// <summary>
        /// 주어진 구성 정보를 반영합니다.
        /// </summary>
        /// <param name="body">구성 정보의 세부 항목입니다.</param>
        /// <returns>작업에 대한 결과를 반환합니다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
        protected virtual int OnApplyConfiguration(ElementConfigurationBody body)
        {
            int ret = 0;
            GeneralEquipmentConfigurationBody specialized = body as GeneralEquipmentConfigurationBody;

            if(specialized == null)
                throw new ArgumentNullException("body");

            // To Do: 변경된 구성 정보를 적용한다.

            return ret;
        }

        /// <summary>
        /// 구성 정보의 내용이 올바른지 검증한다.
        /// </summary>
        /// <param name="configuration">검증할 구성 정보이다.</param>
        public int VerifyConfiguration(ElementConfiguration configuration)
        {
            int ret = 0;
            bool modified = false;

            if((ret = this.OnVerifyConfiguration(configuration.Body, ref modified)) != 0) return ret;

            if(modified == true)
            {
                if((ret = ElementConfigurator.Save(configuration)) != 0) return ret;
            }

            return ret;
        }

        /// <summary>
        /// 구성 정보의 내용이 올바른지 검증한다.
        /// </summary>
        /// <param name="body">검증할 구성 정보 바디이다.</param>
		/// <param name="modified">변경 여부를 반환한다.</param>
        protected virtual int OnVerifyConfiguration(ElementConfigurationBody body, ref bool modified)
        {
            int ret = 0;
            GeneralEquipmentConfigurationBody specialized = body as GeneralEquipmentConfigurationBody;

            if(specialized == null)
                throw new ArgumentNullException("body");

            // To Do: 구성 정보의 내용을 올바른지 검증한다
            // 자동 수정이 가능한 경우 (항목 추가, 삭제 등)는 자동으로 변경하도록 한다.
            // 변경 내용이 발생한 경우 modified = true로 설정한다.

            return ret;
        }
        #endregion

        #region Equipment
        #endregion

        #region Part Members
        protected override int OnAbort()
        {
            int ret = 0;
            int loopReturn = 0;
            ModuleCollection modules = new ModuleCollection();

            #region modules
            // transferable
            modules.Clear();
            foreach(Module item in this.TransferableModules)
            {
                if(item.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected) continue;
                modules.Add(item);
            }
            if((loopReturn = Part.Abort(modules)) != 0) ret = loopReturn;

            // not transferable
            modules.Clear();
            foreach(Module item in this.Modules)
            {
                if(item is ITransferable == true) continue;
                if(item.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected) continue;
                modules.Add(item);
            }
            if((loopReturn = Part.Abort(modules)) != 0) ret = loopReturn;
            #endregion

            #region parts

            #endregion

            return ret;
        }

        protected override int OnInitialize()
        {
            int ret = 0;
            ModuleCollection modules = new ModuleCollection();

            #region parts
            if(this.Communicators != null)
            {
                if((ret = this.Communicators.InitializeSync()) != 0) return ret;
            }
            #endregion

            #region modules
            // transferable
            modules.Clear();
            foreach(TransferableModule item in this.TransferableModules)
            {
                if(this.HybridTransferableModules.Contains(item) == true) continue;
                if(item.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected) continue;
                modules.Add(item);
            }
            if((ret = Part.Initialize(modules)) != 0) return ret;

            // hybrid transferable
            modules.Clear();
            foreach(TransferableModule item in this.HybridTransferableModules)
            {
                if(item.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected) continue;
                modules.Add(item);
            }
            if((ret = Part.Initialize(modules)) != 0) return ret;

            // not transferable
            modules.Clear();
            foreach(Module item in this.Modules)
            {
                if(item is ITransferable == true) continue;
                if(item.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected) continue;
                modules.Add(item);
            }
            if((ret = Part.Initialize(modules)) != 0) return ret;
            #endregion

            return ret;
        }

        protected override int OnStop()
        {
            int ret = 0;
            int loopReturn = 0;
            ModuleCollection modules = new ModuleCollection();

            #region modules
            // transferable
            modules.Clear();
            foreach(Module module in this.TransferableModules)
            {
                if(module.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected) continue;
                modules.Add(module);
            }
            if((loopReturn = Part.Stop(modules)) != 0) ret = loopReturn;

            // not transferable
            modules.Clear();
            foreach(Module item in this.Modules)
            {
                if(item.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected) continue;
                if(item is ITransferable == true) continue;
                modules.Add(item);
            }
            if((loopReturn = Part.Stop(modules)) != 0) ret = loopReturn;
            #endregion

            #region parts
            #endregion

            return ret;
        }
        #endregion

        #region Element Members
        protected new GeneralEquipmentConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as GeneralEquipmentConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new GeneralEquipmentConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }

        protected override void OnInstantiated()
        {
            base.OnInstantiated();

            this.LifeState.AfterTransit += this.LifeState_AfterTransit;
        }
        #endregion
    }
    #endregion

    #region GeneralEquipmentConstructConfiguration
    [Serializable]
    public class GeneralEquipmentConstructConfiguration : SemiEquipmentConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralEquipmentConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public GeneralEquipmentConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        #endregion

        #region ConstructConfiguration Members
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
        }
        #endregion
    }
    #endregion

    #region GeneralEquipmentConfiguration
    [Serializable]
    public class GeneralEquipmentConfiguration : ElementConfiguration<GeneralEquipmentConfigurationBody>
    {
        #region Constructor
        public GeneralEquipmentConfiguration(GeneralEquipment owner)
            : base(owner)
        {
        }
        public GeneralEquipmentConfiguration() : this(null) { }
        #endregion
    }

    [Serializable]
    public class GeneralEquipmentConfigurationBody : ElementConfigurationBody
    {
        #region Field

        #endregion

        #region Constructor
        public GeneralEquipmentConfigurationBody()
        {
        }
        #endregion

        #region Property
        #endregion

        #region ElementConfigurationBody Members
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
        }
        #endregion
    }
    #endregion
}
