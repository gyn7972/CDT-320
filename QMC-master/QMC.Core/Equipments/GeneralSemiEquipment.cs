using System;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Net;
using MechaSys.SoftBricks.Recipes;
using MechaSys.SoftBricks.Transfer;

using QMC.Hmi.Forms;
using QMC.Jobs;

namespace QMC.Equipments
{
    #region GeneralSemiEquipment
    public class GeneralSemiEquipment : SemiEquipment,
        IElementConfigurable<GeneralSemiEquipmentConfiguration>,
        IShowRecipeEditor,
        IHaveUnionRecipeAssigner
    {
        #region Define
        [Serializable]
        public new enum AlarmKeys
        {
            InvalidChangeServicePartially,
        }

        private delegate int ChangeServicePartiallyDelegate(Module[] modules, object requestor, PartServiceStateMachine.StateEnum serviceState);
        #endregion

        #region Field
        private TransferableModuleReadOnlyCollection m_TransferableModules;
        private TransferableModuleReadOnlyCollection m_HybridTransferableModules;

        private GeneralSemiEquipmentConfiguration m_Configuration;

        private UnionRecipeAssigner m_RecipeAssigner;

        private CommunicatorGroup m_Communicators;
        private BuzzerGroup m_Buzzers;
        private TowerLampGroup m_TowerLamps;
        private EmergencySensorGroup m_EmergencySensors;
        private EmergencySensorGroup m_ResourceSensors;
        #endregion

        #region Constructor
        public GeneralSemiEquipment(Nameable nameable)
            : base(nameable)
        {
            #region variables
            this.TransferableModules = new TransferableModuleReadOnlyCollection();
            this.HybridTransferableModules = new TransferableModuleReadOnlyCollection();
            #endregion

            #region elements
            this.ControlJobStateController = new GeneralSemiControlJobStateController(new Nameable("ControlJobStateController"));
            this.ProcessJobStateController = new GeneralSemiProcessJobStateController(new Nameable("ProcessJobStateController"));
            this.JobDispatcherCreator = new GeneralJobDispatcherCreator(new Nameable("JobDispatcherCreator"));
            this.JobOrderGenerator = new GeneralControlJobOrderGenerator(new Nameable("JobOrderGenerator"));
            this.TransferPathfinder = new GeneralTransferPathFinder(new Nameable(nameof(this.TransferPathfinder)));

            this.OperationAssistant = new EquipmentOperationAssistant(new Nameable(nameof(this.OperationAssistant)));
            #endregion

            #region parts
            this.Communicators = new CommunicatorGroup(new Nameable("Communicators"));

            this.Buzzers = new BuzzerGroup(new Nameable("Buzzers"));
            this.TowerLamps = new TowerLampGroup(new Nameable("TowerLamps"));

            this.EmergencySensors = new EmergencySensorGroup(new Nameable("EmergencySensors"));
            this.ResourceSensors = new EmergencySensorGroup(new Nameable("ResourceSensors"));
            #endregion
        }
        public GeneralSemiEquipment() : this(new Nameable()) { }
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

        public bool PositionVisible
        {
            get { return this.ConstructConfiguration.PositionVisible; }
            set { this.ConstructConfiguration.PositionVisible = value; }
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

                CarrierOperationConfigurator.Supported = true;
                CarrierOperationConfigurator.Load();
            }
            else if(e.CurrentStateValue == ElementLifeStateMachine.StateEnum.Prepared)
            {
                TraceLogger.Instance.Thread.Start();
            }
            else if(e.CurrentStateValue == ElementLifeStateMachine.StateEnum.Terminating)
            {
                TraceLogger.Instance.Thread.Stop();
            }
        }
        #endregion

        #region Method
        #region Partial Operations
        #region Initialize()
        public MethodCallerAsyncResult BeginInitializePartially(Module[] modules, MethodCallerAsyncCallback callback, object value)
        {
            return this.Operational.BeginInvoke(new IntAction<Module[]>(this.InitializePartiallyProcedure),
                new object[] { modules }, callback, value);
        }
        public MethodCallerAsyncResult BeginInitializePartially(Module[] modules)
        {
            return this.BeginInitializePartially(modules, null, null);
        }
        public int EndInitializePartially(MethodCallerAsyncResult ar)
        {
            return (int)this.Operational.EndInvoke(ar);
        }
        public int InitializePartially(Module[] modules)
        {
            MethodCallerAsyncResult ar = this.BeginInitializePartially(modules);
            return this.EndInitializePartially(ar);
        }
        private int InitializePartiallyProcedure(Module[] modules)
        {
            return this.OnInitializePartially(modules);
        }
        protected virtual int OnInitializePartially(Module[] modules)
        {
            int ret = 0;
            Module module = null;
            ModuleCollection hybrids = new ModuleCollection();
            ModuleCollection transferables = new ModuleCollection();
            ModuleCollection transferreds = new ModuleCollection();

            if(modules == null || modules.Length == 0) return ret;

            for(int i = 0; i < modules.Length; i++)
            {
                module = modules[i];
                if(module.ServiceState.CurrentStateValue != PartServiceStateMachine.StateEnum.UserSelected) continue;

                if(module is ITransferable && module is ITransferred)
                    hybrids.Add(module);
                else if(module is TransferableModule)
                    transferables.Add(module);
                else
                    transferreds.Add(module);
            }

            if((ret = Part.Initialize(transferables)) != 0) return ret;
            if((ret = Part.Initialize(hybrids)) != 0) return ret;
            if((ret = Part.Initialize(transferreds)) != 0) return ret;

            return ret;
        }
        #endregion

        #region Stop()
        public MethodCallerAsyncResult BeginStopPartially(Module[] modules, MethodCallerAsyncCallback callback, object value)
        {
            return this.Operational.BeginInvoke(new IntAction<Module[]>(this.StopPartiallyProcedure),
                new object[] { modules }, callback, value);
        }
        public MethodCallerAsyncResult BeginStopPartially(Module[] modules)
        {
            return this.BeginStopPartially(modules, null, null);
        }
        public int EndStopPartially(MethodCallerAsyncResult ar)
        {
            return (int)this.Operational.EndInvoke(ar);
        }
        public int StopPartially(Module[] modules)
        {
            MethodCallerAsyncResult ar = this.BeginStopPartially(modules);
            return this.EndStopPartially(ar);
        }
        private int StopPartiallyProcedure(Module[] modules)
        {
            return this.OnStopPartially(modules);
        }
        protected virtual int OnStopPartially(Module[] modules)
        {
            int ret = 0;
            int innerRet = 0;
            Module module = null;
            MechaSys.SoftBricks.ModuleCollection transferables = new MechaSys.SoftBricks.ModuleCollection();
            MechaSys.SoftBricks.ModuleCollection transferreds = new MechaSys.SoftBricks.ModuleCollection();

            if(modules == null || modules.Length == 0)
                return ret;

            for(int i = 0; i < modules.Length; i++)
            {
                module = modules[i];
                if(module.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected)
                {
                    if(module is TransferableModule)
                        transferables.Add(module);
                    else
                        transferreds.Add(module);
                }
            }

            if((innerRet = Part.Stop(transferables)) != 0)
                ret = innerRet;

            if((innerRet = Part.Stop(transferreds)) != 0)
                ret = innerRet;

            return ret;
        }
        #endregion

        #region Abort()
        public MethodCallerAsyncResult BeginAbortPartially(Module[] modules, MethodCallerAsyncCallback callback, object value)
        {
            return this.Operational.BeginInvoke(new IntAction<Module[]>(this.AbortPartiallyProcedure),
                new object[] { modules }, callback, value);
        }
        public MethodCallerAsyncResult BeginAbortPartially(Module[] modules)
        {
            return this.BeginAbortPartially(modules, null, null);
        }
        public int EndAbortPartially(MethodCallerAsyncResult ar)
        {
            return (int)this.Operational.EndInvoke(ar);
        }
        public int AbortPartially(Module[] modules)
        {
            MethodCallerAsyncResult ar = this.BeginAbortPartially(modules);
            return this.EndAbortPartially(ar);
        }
        private int AbortPartiallyProcedure(Module[] modules)
        {
            return this.OnAbortPartially(modules);
        }
        protected virtual int OnAbortPartially(Module[] modules)
        {
            int ret = 0;
            int innerRet = 0;
            Module module = null;
            MechaSys.SoftBricks.ModuleCollection transferables = new MechaSys.SoftBricks.ModuleCollection();
            MechaSys.SoftBricks.ModuleCollection transferreds = new MechaSys.SoftBricks.ModuleCollection();

            if(modules == null || modules.Length == 0)
                return ret;

            for(int i = 0; i < modules.Length; i++)
            {
                module = modules[i];
                if(module.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected)
                {
                    if(module is TransferableModule)
                        transferables.Add(module);
                    else
                        transferreds.Add(module);
                }
            }

            if((innerRet = Part.Abort(transferables)) != 0)
                ret = innerRet;

            if((innerRet = Part.Abort(transferreds)) != 0)
                ret = innerRet;

            return ret;
        }
        #endregion

        #region ChangeService()
        #region Async
        public MethodCallerAsyncResult BeginChangeServicePartially(Module[] modules, object requestor, PartServiceStateMachine.StateEnum serviceState, MethodCallerAsyncCallback callback, object value)
        {
            return this.Operational.BeginInvoke(new ChangeServicePartiallyDelegate(this.ChangeServicePartiallyProcedure),
                new object[] { modules, requestor, serviceState }, callback, value);
        }
        public MethodCallerAsyncResult BeginChangeServicePartially(Module[] modules, PartServiceStateMachine.StateEnum serviceState, MethodCallerAsyncCallback callback, object value)
        {
            return this.BeginChangeServicePartially(modules, EquipmentAccount.User, serviceState, callback, value);
        }
        public MethodCallerAsyncResult BeginChangeServicePartially(Module[] modules, object requestor, PartServiceStateMachine.StateEnum serviceState)
        {
            return this.BeginChangeServicePartially(modules, requestor, serviceState, null, null);
        }
        public MethodCallerAsyncResult BeginChangeServicePartially(Module[] modules, PartServiceStateMachine.StateEnum serviceState)
        {
            return this.BeginChangeServicePartially(modules, serviceState, null, null);
        }
        public int EndChangeServicePartially(MethodCallerAsyncResult ar)
        {
            return (int)this.Operational.EndInvoke(ar);
        }
        #endregion

        #region Sync
        public int ChangeServicePartially(Module[] modules, object requestor, PartServiceStateMachine.StateEnum serviceState)
        {
            MethodCallerAsyncResult ar = this.BeginChangeServicePartially(modules, requestor, serviceState);
            return this.EndChangeServicePartially(ar);
        }
        public int ChangeServicePartially(Module[] modules, PartServiceStateMachine.StateEnum serviceState)
        {
            return this.ChangeServicePartially(modules, EquipmentAccount.User, serviceState);
        }
        #endregion

        private int ChangeServicePartiallyProcedure(Module[] modules, object requestor, PartServiceStateMachine.StateEnum serviceState)
        {
            return this.OnChangeServicePartially(modules, requestor, serviceState);
        }
        protected virtual int OnChangeServicePartially(Module[] modules, object requestor, PartServiceStateMachine.StateEnum serviceState)
        {
            int ret = 0;
            int innerRet = 0;
            Module module = null;
            ModuleCollection hybrids = new ModuleCollection();
            ModuleCollection transferables = new ModuleCollection();
            ModuleCollection transferreds = new ModuleCollection();
            MethodCallerAsyncResultCollection ars = new MethodCallerAsyncResultCollection();

            #region InService/UserSelected간의 변환만 지원한다.
            if(serviceState != PartServiceStateMachine.StateEnum.InService && serviceState != PartServiceStateMachine.StateEnum.UserSelected)
            {
                if((ret = this.Alarms[AlarmKeys.InvalidChangeServicePartially].Post(this)) != 0) return ret;
            }
            #endregion

            if(modules == null || modules.Length == 0) return ret;

            for(int i = 0; i < modules.Length; i++)
            {
                module = modules[i];

                if(module is ITransferable && module is ITransferred)
                    hybrids.Add(module);
                else if(module is TransferableModule)
                    transferables.Add(module);
                else
                    transferreds.Add(module);
            }

            #region Transferable
            ars.Clear();
            for(int i = 0; i < transferables.Count; i++)
            {
                ars.Add(transferables[i].ServiceStateManager.BeginChangeService(requestor, serviceState));
            }
            if((innerRet = ars.WaitReturn()) != 0)
                ret = innerRet;
            #endregion

            #region hybrids
            ars.Clear();
            for(int i = 0; i < hybrids.Count; i++)
            {
                // lock이 풀리면서 변경된 경우가 있을수 있다.
                if(hybrids[i].ServiceState.CurrentStateValue == serviceState) continue;
                ars.Add(hybrids[i].ServiceStateManager.BeginChangeService(requestor, serviceState));
            }
            if((innerRet = ars.WaitReturn()) != 0)
                ret = innerRet;
            #endregion

            #region Transferred
            ars.Clear();
            for(int i = 0; i < transferreds.Count; i++)
            {
                // lock이 풀리면서 변경된 경우가 있을수 있다.
                if(transferreds[i].ServiceState.CurrentStateValue == serviceState) continue;
                ars.Add(transferreds[i].ServiceStateManager.BeginChangeService(requestor, serviceState));
            }
            if((innerRet = ars.WaitReturn()) != 0)
                ret = innerRet;
            #endregion

            return ret;
        }
        #endregion
        #endregion
        #endregion

        #region IHaveUnionRecipeAssigner
        [Composite(ElementKind.Element)]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.ZeroOrOne)]
        public UnionRecipeAssigner RecipeAssigner
        {
            get { return this.m_RecipeAssigner; }
            protected set
            {
                this.Elements.SetNullable(this.m_RecipeAssigner, value);
                this.m_RecipeAssigner = value;
            }
        }
        #endregion

        #region IShowRecipeEditor Members
        public void ShowRecipeEditor(RecipeIdentifier identifier)
        {
            this.OnShowRecipeEditor(identifier);
        }

        protected virtual void OnShowRecipeEditor(RecipeIdentifier identifier)
        {
            GeneralBottomForm formBottom = this.MenuFormProvider.GetForm(EquipmentMenuFormProvider.FormEnum.Bottom) as GeneralBottomForm;
            GeneralSideRecipeForm formSide = null;
            RecipeEditorForm formRecipe = null;
            ManagedRecipe recipe = null;

            if(formBottom == null) return;

            // select recipe menu
            formSide = formBottom.ShowSideWindow(EquipmentMenuFormProvider.FormEnum.SideRecipe) as GeneralSideRecipeForm;
            if(formSide == null) return;

            // get recipe form
            formRecipe = formSide.ShowRecipeWindow(identifier.Class);
            if(formRecipe == null) return;

            recipe = RecipeManager.LoadRecipe(identifier);
            formRecipe.SetContents(recipe);
        }
        #endregion

        #region IElementConfigurable<GeneralSemiEquipmentConfiguration> Members
        /// <summary>
        /// 객체의 구성 정보를 가져옵니다.
        /// </summary>
        public GeneralSemiEquipmentConfiguration Configuration
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
            GeneralSemiEquipmentConfiguration specialized = configuration as GeneralSemiEquipmentConfiguration;

            if(specialized == null)
                throw new ArgumentNullException("configuration");

            if(this.Configuration == null || this.Configuration == configuration)
                this.Configuration = MechaSys.SoftBricks.DotNetUtility.CopyUtility.GetDeepCopy<GeneralSemiEquipmentConfiguration>(specialized);
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
            GeneralSemiEquipmentConfigurationBody specialized = body as GeneralSemiEquipmentConfigurationBody;

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
            GeneralSemiEquipmentConfigurationBody specialized = body as GeneralSemiEquipmentConfigurationBody;

            if(specialized == null)
                throw new ArgumentNullException("body");

            // To Do: 구성 정보의 내용을 올바른지 검증한다
            // 자동 수정이 가능한 경우 (항목 추가, 삭제 등)는 자동으로 변경하도록 한다.
            // 변경 내용이 발생한 경우 modified = true로 설정한다.

            return ret;
        }
        #endregion

        #region SemiEquipment Members
        protected override void MakeModuleList()
        {
            TransferableModule[] transferableModules = this.Modules.GetByType<TransferableModule>();
            TransferableModuleCollection hybridTransferableMoudles = new TransferableModuleCollection();

            base.MakeModuleList();

            this.TransferableModules = new TransferableModuleReadOnlyCollection(transferableModules);
            foreach(TransferableModule item in this.TransferableModules)
            {
                if(item is ITransferred == false) continue;
                hybridTransferableMoudles.Add(item);
            }
            this.HybridTransferableModules = new TransferableModuleReadOnlyCollection(hybridTransferableMoudles);
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
        protected new GeneralSemiEquipmentConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as GeneralSemiEquipmentConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new GeneralSemiEquipmentConstructConfiguration();
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

        protected override void OnMakeAlarms()
        {
            Alarm alarm = null;

            base.OnMakeAlarms();

            alarm = new Alarm((int)AlarmKeys.InvalidChangeServicePartially, "Invalid partial change service");
            alarm.Cause = string.Format("The requested service state is not inservice or userselecetd");
            alarm.Remedy = string.Format("Change service with the correct service state");
            alarm.AlarmGrade = AlarmGrade.Fail;
            this.Alarms.Add(AlarmKeys.InvalidChangeServicePartially, alarm);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            this.ElementFormProvider.FormTypes.AddForcefully(ElementFormProvider.FormEnum.Configuration, typeof(GeneralSemiEquipmentConfigurationForm));
            this.ElementFormProvider.FormTypes.AddForcefully(ElementFormProvider.FormEnum.Maintenance, typeof(GeneralSemiEquipmentMaintenanceForm));
        }
        #endregion
    }
    #endregion

    #region GeneralSemiEquipmentConstructConfiguration
    [Serializable]
    public class GeneralSemiEquipmentConstructConfiguration : SemiEquipmentConstructConfiguration
    {
        #region Field
        private bool m_PositionVisible;
        #endregion

        #region Constructor
        public GeneralSemiEquipmentConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public GeneralSemiEquipmentConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        public bool PositionVisible
        {
            get { return this.m_PositionVisible; }
            set { this.m_PositionVisible = value; }
        }
        #endregion

        #region ConstructConfiguration Members
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.PositionVisible = false;
        }
        #endregion
    }
    #endregion

    #region GeneralSemiEquipmentConfiguration
    [Serializable]
    public class GeneralSemiEquipmentConfiguration : ElementConfiguration<GeneralSemiEquipmentConfigurationBody>
    {
        #region Constructor
        public GeneralSemiEquipmentConfiguration(GeneralSemiEquipment owner)
            : base(owner)
        {
        }
        public GeneralSemiEquipmentConfiguration() : this(null) { }
        #endregion
    }

    [Serializable]
    public class GeneralSemiEquipmentConfigurationBody : ElementConfigurationBody
    {
        #region Field

        #endregion

        #region Constructor
        public GeneralSemiEquipmentConfigurationBody()
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
