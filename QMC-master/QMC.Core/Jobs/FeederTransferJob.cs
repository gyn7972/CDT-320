using MechaSys.SoftBricks;
using MechaSys.SoftBricks.DotNetUtility;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Transfer;

using QMC.Transfers;
using QMC.Transfers.Feeders;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Jobs
{
    #region FeederTransferJob
    public class FeederTransferJob : TransferJob
    {
        #region Field
        private TransferJob m_FeederToTransferredModuleTransferJob;
        private TransferredModule m_RelatedTransferredModule;
        private Int32Collection m_RelatedTransferredModulePorts;
        #endregion

        #region Constructor
        public FeederTransferJob(string name, IJobCalculator jobCalulator, IJobAction jobAction) : base(name, jobCalulator, jobAction)
        {
            this.FeederToTransferredModuleTransferJob = new TransferJob(name, new FeederToTransferredModuleTransferJobCalculator(), new TransferJobAction());
            this.RelatedTransferredModulePorts = new Int32Collection();
        }
        public FeederTransferJob(string name) : this(name, null, null) { }
        public FeederTransferJob() : this("") { }
        #endregion

        #region Property
        public TransferJob FeederToTransferredModuleTransferJob
        {
            get { return this.m_FeederToTransferredModuleTransferJob; }
            set { this.m_FeederToTransferredModuleTransferJob = value; }
        }
        public Feeder Feeder
        {
            get { return base.PrimaryModule as Feeder; }
        }
        public LoadPort LoadPort
        {
            get { return base.SecondaryModule as LoadPort; }
        }

        public TransferredModule RelatedTransferredModule
        {
            get { return this.m_RelatedTransferredModule; }
            set { this.m_RelatedTransferredModule = value; }
        }
        public int RelatedTransferredModulePort
        {
            get
            {
                if(this.m_RelatedTransferredModulePorts.Count == 0)
                    return 0;
                else
                    return this.m_RelatedTransferredModulePorts[0];
            }
            set
            {
                if(this.m_RelatedTransferredModulePorts.Count == 0)
                    this.m_RelatedTransferredModulePorts.Add(value);
                else
                    this.m_RelatedTransferredModulePorts[0] = value;
            }
        }
        public Int32Collection RelatedTransferredModulePorts
        {
            get { return this.m_RelatedTransferredModulePorts; }
            private set { this.m_RelatedTransferredModulePorts = value; }
        }
        #endregion

        #region Job Members
        protected new FeederTransferJobConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as FeederTransferJobConstructConfiguration; }
        }

        protected override JobConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new FeederTransferJobConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(JobConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }

        protected override void OnMakeConstructConfiguration(ref JobConstructConfiguration target)
        {
            FeederTransferJobConstructConfiguration configuration = null;

            base.OnMakeConstructConfiguration(ref target);

            configuration = target as FeederTransferJobConstructConfiguration;

            if(configuration == null) return;
        }
        #endregion
    }
    #endregion

    #region FeederTransferJobAction
    public class FeederTransferJobAction : SubstrateInCarrierTransferJobAction
    {
        #region Field
        #endregion

        #region Constructor
        public FeederTransferJobAction(FeederTransferJob owner) : base(owner)
        {
        }
        public FeederTransferJobAction() : this(null) { }
        #endregion

        #region Property
        #endregion

        #region TransferJobAction Members
        protected override int OnChangeServiceToEquipmentSelected()
        {
            int ret = 0;

            if((ret = base.OnChangeServiceToEquipmentSelected()) != 0) return ret;
            if(this.Owner.RelatedTransferredModule != null)
            {
                if((ret = this.Owner.RelatedTransferredModule.ServiceStateManager.ChangeService(this.Owner.Name, PartServiceStateMachine.StateEnum.EquipmentSelected)) != 0) return ret;
            }
            return ret;
        }

        protected override int OnChangeServiceToInService()
        {
            int ret = 0;

            if((ret = base.OnChangeServiceToInService()) != 0) return ret;
            if(this.Owner.RelatedTransferredModule != null)
            {
                if(Object.Equals(this.Owner.RelatedTransferredModule.ServiceStateManager.Requester, this.Owner.Name) &&
                this.Owner.RelatedTransferredModule.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.EquipmentSelected)
                {
                    if((ret = this.Owner.RelatedTransferredModule.ServiceStateManager.ChangeService(this.Owner.Name, PartServiceStateMachine.StateEnum.InService)) != 0) return ret;
                }
            }
            return ret;
        }

        protected override int OnReadyToTransfer()
        {
            int ret = 0;
            int baseRet = 0;
            if(this.Owner.RelatedTransferredModule == null) return ret;

            try
            {
                if((ret = base.OnReadyToTransfer()) != 0) return ret;
                if((ret = this.Owner.RelatedTransferredModule.ReadyToTransferSync(this.PrimarySpecification)) != 0) return ret;
            }
            finally
            {
                if(ret != 0)
                {
                    if((this.Owner.RelatedTransferredModule.TransferState.CurrentStateValue == TransferStateMachine.StateEnum.Readied ||
                    this.Owner.RelatedTransferredModule.TransferState.CurrentStateValue == TransferStateMachine.StateEnum.Readying) &&
                    this.Owner.RelatedTransferredModule.BehaviorState.CurrentStateValue == PartBehaviorStateMachine.StateEnum.Idle)
                        this.Owner.RelatedTransferredModule.CancelReadyToTransfer(this.PrimarySpecification.Id);
                }

            }

            return ret;
        }

        protected override int OnExecuteTransfer()
        {
            int ret = 0;

            if(this.Owner.RelatedTransferredModule == null) return ret;

            try
            {
                if((ret = base.OnExecuteTransfer()) != 0) return ret;
                if((ret = this.Owner.RelatedTransferredModule.ExecuteTransferSync(this.PrimarySpecification.Id)) != 0) return ret;
            }
            finally
            {
                if(ret != 0)
                    this.Owner.RelatedTransferredModule.HaltTransfer(this.PrimarySpecification.Id);
            }
            return ret;
        }

        protected override int OnVerifyTransfer()
        {
            int ret = 0;
            if(this.Owner.RelatedTransferredModule == null) return ret;

            int retTransferable = 0, retTransferred = 0, retRelatedTransferred = 0;
            MethodCallerAsyncResult primaryAsyncResult, secondaryAsyncResult, relatedAsyncResult;

            if(this.Owner.PrimaryModule == this.Owner.SecondaryModule)
            {
                if((ret = this.Owner.Primary.VerifyTransfer(this.PrimarySpecification.Id)) != 0)
                    this.Owner.Primary.HaltTransfer(this.PrimarySpecification.Id);
                return ret;
            }

            primaryAsyncResult = this.Owner.Primary.BeginVerifyTransfer(this.PrimarySpecification.Id, null, null);
            secondaryAsyncResult = this.Owner.Secondary.BeginVerifyTransfer(this.SecondarySpecification.Id, null, null);           

            retTransferable = this.Owner.Primary.EndVerifyTransfer(primaryAsyncResult);
            retTransferred = this.Owner.Secondary.EndVerifyTransfer(secondaryAsyncResult);

            relatedAsyncResult = this.Owner.RelatedTransferredModule.BeginVerifyTransfer(this.PrimarySpecification.Id, null, null);
            retRelatedTransferred = this.Owner.RelatedTransferredModule.EndVerifyTransfer(relatedAsyncResult);

            if(retTransferable != 0 || retTransferred != 0 || retRelatedTransferred != 0)
            {
                this.Owner.Primary.HaltTransfer(this.PrimarySpecification.Id);
                this.Owner.Secondary.HaltTransfer(this.SecondarySpecification.Id);
                this.Owner.RelatedTransferredModule.HaltTransfer(this.PrimarySpecification.Id);
                return -1;
            }

            return ret;
        }

        protected override int OnCompleteTransfer()
        {
            int ret = 0, retTransferable = 0, retTransferred = 0, retRelated = 0;

            MethodCallerAsyncResult primaryAsyncResult, secondaryAsyncResult, relatedAsyncResult;

            if(this.Owner.PrimaryModule == this.Owner.SecondaryModule)
            {
                if((ret = this.Owner.Primary.CompleteTransfer(this.PrimarySpecification.Id)) != 0)
                    this.Owner.Primary.HaltTransfer(this.PrimarySpecification.Id);
                return ret;
            }

            primaryAsyncResult = this.Owner.Primary.BeginCompleteTransfer(this.PrimarySpecification.Id, null, null);
            secondaryAsyncResult = this.Owner.Secondary.BeginCompleteTransfer(this.SecondarySpecification.Id, null, null);
            relatedAsyncResult = this.Owner.RelatedTransferredModule.BeginCompleteTransfer(this.PrimarySpecification.Id, null, null);

            retTransferable = this.Owner.Primary.EndCompleteTransfer(primaryAsyncResult);
            retTransferred = this.Owner.Secondary.EndCompleteTransfer(secondaryAsyncResult);
            retRelated = this.Owner.RelatedTransferredModule.EndCompleteTransfer(relatedAsyncResult);

            if(retTransferable != 0 || retTransferred != 0 || retRelated != 0)
            {
                this.Owner.Primary.HaltTransfer(this.PrimarySpecification.Id);
                this.Owner.Secondary.HaltTransfer(this.SecondarySpecification.Id);
                this.Owner.RelatedTransferredModule.HaltTransfer(this.PrimarySpecification.Id);
                return -1;
            }

            return ret;
        }

        protected override void MakeTransferSpecification()
        {
            this.PrimarySpecification = new RelatedTransferSpecification();
            // Revision 2
            this.PrimarySpecification.Primary = this.Owner.PrimaryModule.Name;
            this.PrimarySpecification.PrimaryPorts = this.Owner.PrimaryPorts;
            this.PrimarySpecification.Secondary = this.Owner.SecondaryModule.Name;
            this.PrimarySpecification.SecondaryPorts = this.Owner.SecondaryPorts;
            ((RelatedTransferSpecification)this.PrimarySpecification).RelatedTransferred = this.Owner.RelatedTransferredModule.Name;
            ((RelatedTransferSpecification)this.PrimarySpecification).RelatedTransferredPort = this.Owner.RelatedTransferredModulePort;
            this.PrimarySpecification.Direction = this.Owner.TransferDirection;
            this.PrimarySpecification.Role = this.Owner.TransferRole;
            this.PrimarySpecification.Type = this.Owner.TransferType;
            this.PrimarySpecification.SecondActionPrimaryPorts = this.Owner.SecondActionPrimaryPorts;
            this.PrimarySpecification.SecondActionSecondaryPorts = this.Owner.SecondActionSecondaryPorts;
            this.PrimarySpecification.SecondaryCarrierPorts = this.Owner.SecondaryCarrierPorts;

            this.PrimarySpecification.SubstrateSide = this.Owner.SubstrateSide;

            this.PrimarySpecification.CarrierIndex = this.Owner.CarrierIndex;

            this.SecondarySpecification = this.PrimarySpecification.GetOppositeSpecification();
        }

        protected override int OnMoveMaterialInformation()
        {
            int ret = 0;

            if(this.Owner.TransferDirection == TransferDirection.Receive)
            {
                if((ret = base.OnMoveMaterialInformation()) != 0) return ret;
                if(this.Owner.RelatedTransferredModule != null)
                    MaterialStorage.Move(this.Owner.Feeder.Port.Location, this.Owner.RelatedTransferredModule.Ports[this.Owner.RelatedTransferredModulePort].Location);
            }
            else
            {
                if(this.Owner.RelatedTransferredModule != null)
                    MaterialStorage.Move(this.Owner.RelatedTransferredModule.Ports[this.Owner.RelatedTransferredModulePort].Location, this.Owner.Feeder.Port.Location);
                if((ret = base.OnMoveMaterialInformation()) != 0) return ret;
            }
            return ret;
        }

        protected override int OnSetJobOrderCompleted(Material material)
        {
            int ret = 0;
            JobOrder jobOrder = null;
            if(this.Owner.TransferDirection == TransferDirection.Receive)
            {
                base.OnSetJobOrderCompleted(material);
                jobOrder = material.JobOrders.GetCurrentJob();
                jobOrder.State = JobOrderState.Completed;
            }
            else
            {
                jobOrder = material.JobOrders.GetCurrentJob();
                jobOrder.State = JobOrderState.Completed;
                base.OnSetJobOrderCompleted(material);
            }
            return ret;

        }
        #endregion

        #region JobAction Member
        public new FeederTransferJob Owner
        {
            get { return base.Owner as FeederTransferJob; }
            set { base.Owner = value; }
        }
        public override int Procedure()
        {
            int ret = 0;
            if(this.Owner.TransferDirection == TransferDirection.Receive)
            {
                if((ret = base.Procedure()) != 0) return ret;
            }
            else
            {
                if((ret = base.Procedure()) != 0) return ret;
            }

            return ret;
        }
        #endregion
    }
    #endregion

    #region FeederTransferJobCalculator
    public class FeederTransferJobCalculator : FeederToLoadPortTransferJobCalculator
    {
        #region Constructor
        public FeederTransferJobCalculator(FeederTransferJob owner) : base(owner)
        {
        }
        public FeederTransferJobCalculator() : this(null) { }
        #endregion

        #region Method
        #endregion

        #region JobCalculator Member
        public new FeederTransferJob Owner
        {
            get { return base.Owner as FeederTransferJob; }
            set { base.Owner = value; }
        }

        public override JobPriority GetPriority()
        {
            JobPriority priority = JobPriority.DisablePriority;
            MaterialCollection materials = null;

            if(this.Owner.TransferDirection == TransferDirection.Receive)
            {
                if((priority = base.GetPriority()) == JobPriority.DisablePriority) return priority;
                materials = this.GetMaterials(this.Owner.Secondary, this.Owner.SecondaryPort);
                (this.Owner.FeederToTransferredModuleTransferJob.JobCalculator as FeederToTransferredModuleTransferJobCalculator).GetLoadPortMaterials(materials);
                if((priority = this.Owner.FeederToTransferredModuleTransferJob.GetPriority()) == JobPriority.DisablePriority) return priority;
            }
            else
            {
                if((priority = this.Owner.FeederToTransferredModuleTransferJob.GetPriority()) == JobPriority.DisablePriority) return priority;
                (this.Owner.FeederToTransferredModuleTransferJob.JobCalculator as FeederToTransferredModuleTransferJobCalculator).SetTransferredModuleMaterials(ref materials);
                this.GetTransferredModuleMaterials(materials);
                if((priority = base.GetPriority()) == JobPriority.DisablePriority) return priority;
            }

            return priority;
        }
        #endregion
    }
    #endregion

    #region FeederTransferJobConstructConfiguration
    [Serializable]
    public class FeederTransferJobConstructConfiguration : TransferJobConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public FeederTransferJobConstructConfiguration(string name) : base(name)
        {
        }
        public FeederTransferJobConstructConfiguration() : this("") { }
        #endregion

        #region Property
        /// <summary>
        /// Feeder로 부터 Material을 주고 받는 모듈의 UID를 가져오거나 설정한다.
        /// </summary>
        #endregion

        #region ConstructConfiguration Member
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
        }
        #endregion
    }
    #endregion

    #region FeederToTransferredModuleTransferJobCalculator
    public class FeederToTransferredModuleTransferJobCalculator : TransferJobCalculator
    {
        #region Field
        private MaterialCollection Materials;
        #endregion

        #region Constructor
        public FeederToTransferredModuleTransferJobCalculator(FeederTransferJob owner) : base(owner)
        {
        }
        public FeederToTransferredModuleTransferJobCalculator() : this(null) { }
        #endregion

        #region Method
        public void GetLoadPortMaterials(MaterialCollection materials)
        {
            this.Materials = materials;
        }

        public void SetTransferredModuleMaterials(ref MaterialCollection materials)
        {
            materials = this.GetMaterials(this.Owner.Secondary, this.Owner.SecondaryPort);
        }
        #endregion

        #region TransferJobCalculator Members
        protected override bool CheckPrimaryMaterialPresence()
        {
            if(this.Owner.TransferDirection == TransferDirection.Receive)
                return base.CheckPrimaryMaterialPresence();
            else
                return true;
        }

        protected override MaterialCollection GetMaterials(IMaterialStorable store, IList<int> ports, bool checkPresence)
        {
            MaterialCollection materials = new MaterialCollection();

            if(this.Owner.TransferDirection == TransferDirection.Receive)
                return base.GetMaterials(store, ports, checkPresence);
            else
            {
                if(this.Materials == null)
                    return base.GetMaterials(store, ports, checkPresence);

                //checkPresence 가 True 인 경우 Material 가 Exist 인 상태만 반환한다.
                if(checkPresence == true)
                {
                    for(int i = 0; i < this.Materials.Count; i++)
                    {
                        if(this.Materials[i].Presence != MaterialPresence.Exist) continue;
                        materials.Add(this.Materials[i]);

                    }

                    if(materials.Count == 0)
                        return base.GetMaterials(store, ports, checkPresence);
                    else
                        return materials;
                }

                return this.Materials;
            }
        }

        protected override bool FindJobOrderInMaterial(IList<Material> materials, bool secondAction)
        {
            JobOrder order;
            bool founded = false;

            if(this.Owner.TransferDirection == TransferDirection.Send)
            {
                for(int i = 0; i < materials.Count; i++)
                {
                    founded = false;
                    order = materials[i].JobOrders.GetCurrentJob();
                    if(order != null)
                        order = materials[i].JobOrders.GetNextJob(order);
                    if(order != null && order.State == JobOrderState.Ready)
                    {
                        foreach(JobOrder innerJobOrder in order.JobOrders)
                        {
                            if(this.EvaluateJobOrder(innerJobOrder, secondAction) == true)
                            {
                                founded = true;
                                break;
                            }
                        }
                    }
                    if(founded == false)
                    {
                        this.DisableReason = string.Format("The job order was not found");
                        return false;
                    }
                }
            }
            else
                return base.FindJobOrderInMaterial(materials, secondAction);

            return true;
        }
        #endregion
    }
    #endregion

    #region FeederToLoadPortTransferJobCalculator
    public class FeederToLoadPortTransferJobCalculator : GeneralSubstrateInCarrierTransferJobCalculator
    {
        #region Field
        private MaterialCollection StageMaterials;
        #endregion

        #region Constructor
        public FeederToLoadPortTransferJobCalculator(FeederTransferJob owner) : base(owner)
        {
        }
        public FeederToLoadPortTransferJobCalculator() : this(null) { }
        #endregion

        #region Method
        public void GetTransferredModuleMaterials(MaterialCollection materials)
        {
            this.StageMaterials = materials;
        }
        private bool BelowCheckJobOrderToSend(Carrier carrier, out Material material)
        {
            bool foundedJobOrder = false;
            JobOrder jobOrder = null;
            JobPriorityCollection priorities = new JobPriorityCollection();

            material = null;

            foreach(Material item in this.StageMaterials)
            {
                foundedJobOrder = false;

                #region check material presence
                material = item;
                if(material.Presence != MaterialPresence.Exist)
                {
                    this.DisableReason = "Material does not exists";
                    return false;
                }
                #endregion

                #region check material size
                Wafer wafer = material as Wafer;
                WaferCarrier waferCarrier = carrier as WaferCarrier;
                if(wafer != null && waferCarrier != null)
                {
                    if(wafer.Size != waferCarrier.Size)
                    {
                        this.DisableReason = string.Format("A size of the wafer[{0}] is not equal a size of carrier[{1}]", wafer.Size, waferCarrier.Size);
                        return false;
                    }
                }
                #endregion

                #region check job order
                jobOrder = material.JobOrders.GetCurrentJob();
                if(jobOrder != null)
                    jobOrder = material.JobOrders.GetNextJob(jobOrder);

                if(jobOrder != null && jobOrder.State == JobOrderState.Ready)
                {
                    foreach(JobOrder innerJobOrder in jobOrder.JobOrders)
                    {
                        TransferJobOrder transferJobOrder = innerJobOrder as TransferJobOrder;
                        if(transferJobOrder == null) continue;
                        if(transferJobOrder.Primary == this.Owner.PrimaryModule.Name &&
                            transferJobOrder.PrimaryPort == this.Owner.PrimaryPort &&
                            transferJobOrder.Secondary == this.Owner.Secondary.Name &&
                            transferJobOrder.SecondaryPort == this.Owner.SecondaryPort &&
                            transferJobOrder.Direction == this.Owner.TransferDirection)
                        {
                            for(int j = 0; j < transferJobOrder.SecondaryCarrierPorts.Count; j++)
                            {
                                if(carrier is StackCarrier stackCarrier)
                                {
                                    int carrierIndex = 0;
                                    if(stackCarrier.GetCarrierIndex(ref carrierIndex) == false)
                                        throw new ApplicationException("Cannot get the carrier index");

                                    if(transferJobOrder.CarrierIndex != carrierIndex)
                                    {
                                        this.DisableReason = "Carrier index does not match";
                                        break;
                                    }
                                }

                                if(this.CheckMaterialPresenceInCarrier(carrier, transferJobOrder.SecondaryCarrierPorts[j]) == true)
                                {
                                    this.Owner.SecondaryCarrierPorts.Add(transferJobOrder.SecondaryCarrierPorts[j]);
                                    foundedJobOrder = true;
                                    if(carrier is StackCarrier)
                                    {
                                        int carrierIndex = 0;
                                        if(((StackCarrier)carrier).GetCarrierIndex(ref carrierIndex) == false)
                                            throw new ApplicationException("Cannot get the carrier index");
                                        this.Owner.CarrierIndex = carrierIndex;
                                    }
                                    break;
                                }
                            }
                            if(foundedJobOrder == true) break;
                        }
                    }
                }

                if(foundedJobOrder == false)
                {
                    if(carrier is StackCarrier && ((StackCarrier)carrier).Above != null)
                    {
                        if(this.BelowCheckJobOrderToSend(((StackCarrier)carrier).Above, out material) == true) return true;
                    }

                    this.DisableReason = "The send job order was not found";
                    return false;
                }
                #endregion
            }

            return true;
        }
        private bool AboveCheckJobOrderToSend(Carrier carrier, out Material material)
        {
            bool foundedJobOrder = false;
            JobOrder jobOrder = null;
            JobPriorityCollection priorities = new JobPriorityCollection();

            material = null;

            foreach(Material item in this.StageMaterials)
            {
                foundedJobOrder = false;

                #region check material presence
                material = item;
                if(material.Presence != MaterialPresence.Exist)
                {
                    this.DisableReason = "Material does not exists";
                    return false;
                }
                #endregion

                #region check material size
                Wafer wafer = material as Wafer;
                WaferCarrier waferCarrier = carrier as WaferCarrier;
                if(wafer != null && waferCarrier != null)
                {
                    if(wafer.Size != waferCarrier.Size)
                    {
                        this.DisableReason = string.Format("A size of the wafer[{0}] is not equal a size of carrier[{1}]", wafer.Size, waferCarrier.Size);
                        return false;
                    }
                }
                #endregion

                #region check job order
                jobOrder = material.JobOrders.GetCurrentJob();
                if(jobOrder != null)
                    jobOrder = material.JobOrders.GetNextJob(jobOrder);

                if(jobOrder != null && jobOrder.State == JobOrderState.Ready)
                {
                    foreach(JobOrder innerJobOrder in jobOrder.JobOrders)
                    {
                        TransferJobOrder transferJobOrder = innerJobOrder as TransferJobOrder;
                        if(transferJobOrder == null) continue;
                        if(transferJobOrder.Primary == this.Owner.PrimaryModule.Name &&
                            transferJobOrder.PrimaryPort == this.Owner.PrimaryPort &&
                            transferJobOrder.Secondary == this.Owner.Secondary.Name &&
                            transferJobOrder.SecondaryPort == this.Owner.SecondaryPort &&
                            transferJobOrder.Direction == this.Owner.TransferDirection)
                        {
                            for(int j = 0; j < transferJobOrder.SecondaryCarrierPorts.Count; j++)
                            {
                                if(carrier is StackCarrier stackCarrier)
                                {
                                    int carrierIndex = 0;
                                    if(stackCarrier.GetCarrierIndex(ref carrierIndex) == false)
                                        throw new ApplicationException("Cannot get the carrier index");

                                    if(transferJobOrder.CarrierIndex != carrierIndex)
                                    {
                                        this.DisableReason = "Carrier index does not match";
                                        break;
                                    }
                                }

                                if(this.CheckMaterialPresenceInCarrier(carrier, transferJobOrder.SecondaryCarrierPorts[j]) == true)
                                {
                                    this.Owner.SecondaryCarrierPorts.Add(transferJobOrder.SecondaryCarrierPorts[j]);
                                    foundedJobOrder = true;
                                    if(carrier is StackCarrier)
                                    {
                                        int carrierIndex = 0;
                                        if(((StackCarrier)carrier).GetCarrierIndex(ref carrierIndex) == false)
                                            throw new ApplicationException("Cannot get the carrier index");
                                        this.Owner.CarrierIndex = carrierIndex;
                                    }
                                    break;
                                }
                            }
                            if(foundedJobOrder == true) break;
                        }
                    }
                }

                if(foundedJobOrder == false)
                {
                    if(carrier is StackCarrier && ((StackCarrier)carrier).Below != null)
                    {
                        if(this.AboveCheckJobOrderToSend(((StackCarrier)carrier).Below, out material) == true) return true;
                    }

                    this.DisableReason = "The send job order was not found";
                    return false;
                }
                #endregion
            }

            return true;
        }
        private bool BelowCheckJobOrderToReceive(Carrier carrier, out Material material)
        {
            int slotIndex = -1;
            bool foundedJobOrder = false;
            Substrate substrate = null;
            ProcessJob processJob = null;
            JobOrder jobOrder = null;
            JobPriorityCollection priorities = new JobPriorityCollection();

            material = null;

            priorities.Clear();

            for(int i = 0; i < carrier.Capacity; i++)
            {
                foundedJobOrder = false;

                #region check material presence
                material = carrier.Ports[i].Location.GetMaterial();
                if(this.CheckMaterialPresenceInCarrier(carrier, i) == false)
                {
                    priorities.Add(JobPriority.DisablePriority);
                    continue;
                }
                #endregion

                #region check process job
                substrate = material as Substrate;
                if(substrate == null)
                {
                    priorities.Add(JobPriority.DisablePriority);
                    continue;
                }

                // Revision 2
                if(string.IsNullOrEmpty(substrate.ProcessJobId) == false)
                {
                    // process job
                    processJob = ProcessJobManager.GetByJobId(substrate.ProcessJobId);
                    if(processJob == null)
                    {
                        priorities.Add(JobPriority.DisablePriority);
                        continue;
                    }

                    // process job state
                    if(processJob.State.CurrentStateValue != ProcessJobStateMachine.StateEnum.Processing)
                    {
                        // Stopping 중이면서 SubstrateTransportState.AtWork인 것은 꺼내야 된다
                        // (added by biglake 2015/07/09. Carrier를 이용하여 Batch작업을 하는 경우를 대비)
                        if(processJob.State.CurrentStateValue != ProcessJobStateMachine.StateEnum.Stopping ||
                            substrate.TransportState.CurrentStateValue != SubstrateTransportStateMachine.StateEnum.AtWork)
                        {
                            priorities.Add(JobPriority.DisablePriority);
                            continue;
                        }
                    }
                }
                #endregion

                #region check current job order
                // To Do: process job의 우선 순위를 반영하여야 한다.
                jobOrder = material.JobOrders.GetCurrentJob();
                if(jobOrder == null || jobOrder.State != JobOrderState.Ready)
                {
                    priorities.Add(JobPriority.DisablePriority);
                    continue;
                }

                foreach(JobOrder innerJobOrder in jobOrder.JobOrders)
                {
                    TransferJobOrder transferJobOrder = innerJobOrder as TransferJobOrder;
                    if(transferJobOrder == null) continue;
                    if(transferJobOrder.Primary == this.Owner.PrimaryModule.Name &&
                        transferJobOrder.PrimaryPort == this.Owner.PrimaryPort &&
                        transferJobOrder.Secondary == this.Owner.SecondaryModule.Name &&
                        transferJobOrder.Direction == this.Owner.TransferDirection)
                    {
                        foundedJobOrder = true;
                        break;
                    }
                }
                #endregion

                if(foundedJobOrder == false)
                {
                    priorities.Add(JobPriority.DisablePriority);
                    continue;
                }
                else
                {
                    JobPriority priority = this.DefaultPriority;
                    priority.Material = material.Priority;
                    priorities.Add(priority);
                }
            } // end of for

            #region get slot index
            slotIndex = priorities.GetMaxIndex();
            if(slotIndex < 0)
            {
                if(carrier is StackCarrier && ((StackCarrier)carrier).Above != null)
                {
                    if(this.BelowCheckJobOrderToReceive(((StackCarrier)carrier).Above, out material) == true) return true;
                }

                this.DisableReason = string.Format("The receive job order was not found");
                return false;
            }
            else
            {
                if(carrier is StackCarrier)
                {
                    int carrierIndex = 0;
                    if(((StackCarrier)carrier).GetCarrierIndex(ref carrierIndex) == false)
                        throw new ApplicationException("Cannot get the carrier index");
                    this.Owner.CarrierIndex = carrierIndex;
                }

                this.Owner.SecondaryCarrierPorts.Add(slotIndex);
                material = carrier.Ports[slotIndex].Location.GetMaterial();
            }
            #endregion

            return true;
        }
        private bool AboveCheckJobOrderToReceive(Carrier carrier, out Material material)
        {
            int slotIndex = -1;
            bool foundedJobOrder = false;
            Substrate substrate = null;
            ProcessJob processJob = null;
            JobOrder jobOrder = null;
            JobPriorityCollection priorities = new JobPriorityCollection();

            material = null;

            priorities.Clear();

            for(int i = 0; i < carrier.Capacity; i++)
            {
                foundedJobOrder = false;

                #region check material presence
                material = carrier.Ports[i].Location.GetMaterial();
                if(this.CheckMaterialPresenceInCarrier(carrier, i) == false)
                {
                    priorities.Add(JobPriority.DisablePriority);
                    continue;
                }
                #endregion

                #region check process job
                substrate = material as Substrate;
                if(substrate == null)
                {
                    priorities.Add(JobPriority.DisablePriority);
                    continue;
                }

                // Revision 2
                if(string.IsNullOrEmpty(substrate.ProcessJobId) == false)
                {
                    // process job
                    processJob = ProcessJobManager.GetByJobId(substrate.ProcessJobId);
                    if(processJob == null)
                    {
                        priorities.Add(JobPriority.DisablePriority);
                        continue;
                    }

                    // process job state
                    if(processJob.State.CurrentStateValue != ProcessJobStateMachine.StateEnum.Processing)
                    {
                        // Stopping 중이면서 SubstrateTransportState.AtWork인 것은 꺼내야 된다
                        // (added by biglake 2015/07/09. Carrier를 이용하여 Batch작업을 하는 경우를 대비)
                        if(processJob.State.CurrentStateValue != ProcessJobStateMachine.StateEnum.Stopping ||
                            substrate.TransportState.CurrentStateValue != SubstrateTransportStateMachine.StateEnum.AtWork)
                        {
                            priorities.Add(JobPriority.DisablePriority);
                            continue;
                        }
                    }
                }
                #endregion

                #region check current job order
                // To Do: process job의 우선 순위를 반영하여야 한다.
                jobOrder = material.JobOrders.GetCurrentJob();
                if(jobOrder == null || jobOrder.State != JobOrderState.Ready)
                {
                    priorities.Add(JobPriority.DisablePriority);
                    continue;
                }

                foreach(JobOrder innerJobOrder in jobOrder.JobOrders)
                {
                    TransferJobOrder transferJobOrder = innerJobOrder as TransferJobOrder;
                    if(transferJobOrder == null) continue;
                    if(transferJobOrder.Primary == this.Owner.PrimaryModule.Name &&
                        transferJobOrder.PrimaryPort == this.Owner.PrimaryPort &&
                        transferJobOrder.Secondary == this.Owner.SecondaryModule.Name &&
                        transferJobOrder.Direction == this.Owner.TransferDirection)
                    {
                        foundedJobOrder = true;
                        break;
                    }
                }
                #endregion

                if(foundedJobOrder == false)
                {
                    priorities.Add(JobPriority.DisablePriority);
                    continue;
                }
                else
                {
                    JobPriority priority = this.DefaultPriority;
                    priority.Material = material.Priority;
                    priorities.Add(priority);
                }
            } // end of for

            #region get slot index
            slotIndex = priorities.GetMaxIndex();
            if(slotIndex < 0)
            {
                if(carrier is StackCarrier && ((StackCarrier)carrier).Below != null)
                {
                    if(this.AboveCheckJobOrderToReceive(((StackCarrier)carrier).Below, out material) == true) return true;
                }

                this.DisableReason = string.Format("The receive job order was not found");
                return false;
            }
            else
            {
                if(carrier is StackCarrier)
                {
                    int carrierIndex = 0;
                    if(((StackCarrier)carrier).GetCarrierIndex(ref carrierIndex) == false)
                        throw new ApplicationException("Cannot get the carrier index");
                    this.Owner.CarrierIndex = carrierIndex;
                }

                this.Owner.SecondaryCarrierPorts.Add(slotIndex);
                material = carrier.Ports[slotIndex].Location.GetMaterial();
            }
            #endregion

            return true;
        }
        #endregion

        #region SubstrateInCarrierTransferJobCalculator Members
        protected override bool CheckJobOrderToSend(Carrier carrier, out Material material)
        {
            if(carrier is StackCarrier stackCarrier && stackCarrier.Above != null)
            {
                if(carrier.GetLoadedLoadPortPlate() is StackableLoadPortPlate stackable)
                {
                    if(stackable.EnableAboveCarrierProcessFirst == true)
                    {
                        if(this.AboveCheckJobOrderToSend(stackCarrier.Above, out material) == true) return true;
                        else
                            return false;
                    }
                    else
                    {
                        if(this.BelowCheckJobOrderToSend(stackCarrier, out material) == true) return true;
                        else
                            return false;
                    }
                }
            }
            return this.BelowCheckJobOrderToSend(carrier, out material);
        }

        protected override bool CheckJobOrderToReceive(Carrier carrier, out Material material)
        {
            int slotIndex = -1;
            bool foundedJobOrder = false;
            Substrate substrate = null;
            ProcessJob processJob = null;
            JobOrder jobOrder = null;
            JobPriorityCollection priorities = new JobPriorityCollection();

            material = null;

            priorities.Clear();

            if(carrier is StackCarrier stackCarrier && stackCarrier.Above != null)
            {
                if(carrier.GetLoadedLoadPortPlate() is StackableLoadPortPlate stackable)
                {
                    if(stackable.EnableAboveCarrierProcessFirst == true)
                    {
                        if(this.AboveCheckJobOrderToReceive(stackCarrier.Above, out material) == true) return true;
                        else
                            return false;
                    }
                    else
                    {
                        if(this.BelowCheckJobOrderToReceive(carrier, out material) == true) return true;
                        else
                            return false;
                    }
                }
            }

            return this.BelowCheckJobOrderToReceive(carrier, out material);
        }
        #endregion

        #region TransferJobCalculator Members
        protected override bool CheckPrimaryMaterialPresence()
        {
            if(this.Owner.TransferDirection == TransferDirection.Receive)
                return base.CheckPrimaryMaterialPresence();
            else
                return true;
        }

        protected override MaterialCollection GetMaterials(IMaterialStorable store, IList<int> ports, bool checkPresence)
        {
            MaterialCollection materials = new MaterialCollection();
            if(this.Owner.TransferDirection == TransferDirection.Receive)
                return base.GetMaterials(store, ports, checkPresence);
            else
            {
                if(this.StageMaterials == null)
                    return base.GetMaterials(store, ports, checkPresence);

                //checkPresence 가 True 인 경우 Material 가 Exist 인 상태만 반환한다.
                if(checkPresence == true)
                {
                    for(int i = 0; i < this.StageMaterials.Count; i++)
                    {
                        if(this.StageMaterials[i].Presence != MaterialPresence.Exist) continue;
                        StageMaterials.Add(this.StageMaterials[i]);

                    }

                    if(materials.Count == 0)
                        return base.GetMaterials(store, ports, checkPresence);
                    else
                        return materials;
                }

                return this.StageMaterials;
            }
        }
        #endregion

        #region JobCalculator Member
        public new FeederTransferJob Owner
        {
            get { return base.Owner as FeederTransferJob; }
            set { base.Owner = value; }
        }
        #endregion
    }
    #endregion
}
