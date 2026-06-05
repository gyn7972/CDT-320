using System;
using System.ComponentModel;
using System.Drawing.Design;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Configurations.Controls;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Transfer;

namespace QMC.LoadPorts
{
    #region AutoFeedingLoadPortPlate
    /// <summary>
    /// 자동으로 material을 공급하는 LoadPortPlate를 정의한다.
    /// </summary>
    public class AutoFeedingLoadPortPlate : StackableLoadPortPlate
    {
        #region Field       
        private AutoFeedingLoadPortPlateJobOrderGenerator m_JobOrderGenerator;

        private LoadPort m_RelatedInputLoadPort;
        private MaterialStorablePart m_RelatedTransferredMaterialStorablePart;
        #endregion

        #region Constructor
        public AutoFeedingLoadPortPlate(Nameable nameable)
            : base(nameable)
        {
        }
        public AutoFeedingLoadPortPlate() : this(new Nameable()) { }
        #endregion

        #region Property
        [Composite(ElementKind.Element)]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.ZeroOrOne)]
        public AutoFeedingLoadPortPlateJobOrderGenerator JobOrderGenerator
        {
            get { return this.m_JobOrderGenerator; }
            protected set
            {
                this.Elements.SetNullable(this.m_JobOrderGenerator, value);
                this.m_JobOrderGenerator = value;
            }
        }

        /// <summary>
        /// 로드포트 플레이트에 연결된 투입용 로드포트를 가져온다.
        /// </summary>
        public LoadPort RelatedInputLoadPort
        {
            get { return this.m_RelatedInputLoadPort; }
            private set { this.m_RelatedInputLoadPort = value; }
        }

        public MaterialStorablePart RelatedTransferredMaterialStorablePart
        {
            get { return this.m_RelatedTransferredMaterialStorablePart; }
            private set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(this.RelatedTransferredMaterialStorablePart));
                this.m_RelatedTransferredMaterialStorablePart = value;
            }
        }
        #endregion

        #region Event Handler
        private void LifeState_AfterTransit(object sender, ElementLifeAfterStateTransitionEventArgs e)
        {
            if (e.CurrentStateValue == ElementLifeStateMachine.StateEnum.Creating)
            {
                if (this.JobOrderGenerator != null)
                {
                    this.GetRelatedInputLoadPort();
                    this.GetRelatedTransferredMaterialStorablePart();
                }
            }
        }

        private void RelatedInputLoadPort_ControlJobStateChanged(object sender, ControlJobAfterStateTransitionEventArgs e)
        {
            if (e.CurrentStateValue == ControlJobStateMachine.StateEnum.Executing)
            {
                if (this.JobOrderGenerator != null)
                    this.JobOrderGenerator.MakeJobOrder();
            }
        }
        #endregion

        #region Method
        /// <summary>
        /// 연관된 투입용 LoadPort를 반환한다.
        /// </summary>
        private void GetRelatedInputLoadPort()
        {
            LoadPort loadport = null;

            loadport = ElementList.GetByLocator<LoadPort>(this.ConstructConfiguration.RelatedInputLoadPortLocator);
            if (loadport == null)
                throw new ArgumentNullException("RelatedInputLoadPort", string.Format("RelatedInputLoadPort[{0}] does not found", this.ConstructConfiguration.RelatedInputLoadPortLocator));

            this.RelatedInputLoadPort = loadport;
            this.RelatedInputLoadPort.ControlJobStateChanged += this.RelatedInputLoadPort_ControlJobStateChanged;
        }

        private void GetRelatedTransferredMaterialStorablePart()
        {
            MaterialStorablePart materialStorablePart = null;

            materialStorablePart = ElementList.GetByLocator<MaterialStorablePart>(this.ConstructConfiguration.RelatedTransferredMaterialStorablePartLocator);
            if (materialStorablePart == null)
                throw new ArgumentNullException("MaterialStorablePart", string.Format("MaterialStorablePart[{0}] does not found", this.ConstructConfiguration.RelatedTransferredMaterialStorablePartLocator));

            this.RelatedTransferredMaterialStorablePart = materialStorablePart;
        }

        /// <summary>
        /// Carrier 정보를 생성할 수 있는지 여부를 반환한다.
        /// </summary>
        /// <returns></returns>
        protected virtual bool CanCreateCarrier()
        {
            if (this.AssociationState.CurrentStateValue == LoadPortCarrierAssociationStateMachine.StateEnum.Associated) return false;

            return true;
        }

        /// <summary>
        /// Carrier의 정보를 삭제할 수 있는지 여부를 반환한다.
        /// </summary>
        /// <returns></returns>
        protected virtual bool CanRemoveCarrier()
        {
            ControlJob[] controlJobs;

            // check control job
            controlJobs = ControlJobManager.GetAll();
            foreach (ControlJob item in controlJobs)
            {
                if (item.State.CurrentStateValue != ControlJobStateMachine.StateEnum.Completed)
                    return false;
            }

            return true;
        }
        #endregion

        #region LoadPortPlate
        protected override int OnChangedCarrierExist()
        {
            int ret = 0;

            if (this.JobOrderGenerator == null)
            {
                if ((ret = base.OnChangedCarrierExist()) != 0) return ret;
            }
            else
            {
                if (this.CanCreateCarrier())
                {
                    if ((ret = base.OnChangedCarrierExist()) != 0) return ret;

                    Carrier carrier = this.Port.Location.GetMaterial() as Carrier;
                    if (carrier != null)
                        carrier.IdentifierState.AfterTransit += this.CarrierIdentifierState_AfterTransit;

                    //if ((ret = this.JobOrderGenerator.MakeJobOrder()) != 0) return ret;
                }
            }

            return ret;
        }

        private void CarrierIdentifierState_AfterTransit(object sender, CarrierIdentifierAfterStateTransitionEventArgs e)
        {
            if (e.CurrentStateValue == CarrierIdentifierStateMachine.StateEnum.VerificationOk)
            {
                this.JobOrderGenerator.MakeJobOrder();
            }
        }

        protected override int OnChangedCarrierNotExist()
        {
            int ret = 0;

            if (this.JobOrderGenerator == null)
            {
                if ((ret = base.OnChangedCarrierNotExist()) != 0) return ret;
            }
            else
            {
                if (this.CanRemoveCarrier())
                {
                    Carrier carrier = this.Port.Location.GetMaterial() as Carrier;
                    if (carrier != null)
                        carrier.IdentifierState.AfterTransit -= this.CarrierIdentifierState_AfterTransit;

                    return base.OnChangedCarrierNotExist();
                }
            }

            return ret;
        }
        #endregion

        #region Part
        #endregion

        #region Element
        protected new AutoFeedingLoadPortPlateConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as AutoFeedingLoadPortPlateConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new AutoFeedingLoadPortPlateConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if (this.ConstructConfiguration == null) return;
        }

        protected override void OnInstantiated()
        {
            base.OnInstantiated();

            this.LifeState.AfterTransit += this.LifeState_AfterTransit;
        }
        #endregion
    }
    #endregion

    #region AutoFeedingLoadPortPlateConstructConfiguration
    [Serializable]
    public class AutoFeedingLoadPortPlateConstructConfiguration : StackableLoadPortPlateConstructConfiguration
    {
        #region Field
        private string m_RelatedInputLoadPortLocator;
        private string m_RelatedTransferredMaterialStorablePartLocator;
        #endregion

        #region Constructor
        public AutoFeedingLoadPortPlateConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public AutoFeedingLoadPortPlateConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        [Category("AutoFeedingLoadPortPlate")]
        [Editor(typeof(ElementLocatorUITypeEditor<LoadPort>), typeof(UITypeEditor))]
        [Description("TapeFrame을 설비에 투입하는 LoadPort의 위치를 가져오거나 설정한다.")]
        public string RelatedInputLoadPortLocator
        {
            get { return this.m_RelatedInputLoadPortLocator; }
            set { this.m_RelatedInputLoadPortLocator = value; }
        }

        [Category("AutoFeedingLoadPortPlate")]
        [Editor(typeof(ElementLocatorUITypeEditor<MaterialStorablePart>), typeof(UITypeEditor))]
        [Description("TapeFrame을 반송하는 부품의 위치를 가져오거나 설정한다.")]
        public string RelatedTransferredMaterialStorablePartLocator
        {
            get { return this.m_RelatedTransferredMaterialStorablePartLocator; }
            set { this.m_RelatedTransferredMaterialStorablePartLocator = value; }
        }
        #endregion

        #region ConstructConfiguration
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.RelatedInputLoadPortLocator = "";
            this.RelatedTransferredMaterialStorablePartLocator = "";
        }
        #endregion
    }
    #endregion
}