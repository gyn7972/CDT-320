using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Transfer;

namespace QMC.LoadPorts
{
    #region LifterPlateTransferAssistant
    public class LifterPlateTransferAssistant : PlateTransferAssistant,
        IElementConfigurable<LifterPlateTransferAssistantConfiguration>,
        ISupportElementConfigurationEditor,
        ISupportElementMaintenanceControl
    {
        #region Define
        [Serializable]
        public enum Positions
        {
            /// <summary>
            /// 대기 위치를 지정한다.
            /// </summary>
            Ready,

            Transfer,
            Scan,
        }
        #endregion

        #region Field
        private TargetXyztPositionRepository m_PositionRepository;
        private LifterPlateTransferAssistantConfiguration m_Configuration;
        #endregion

        #region Constructor
        public LifterPlateTransferAssistant(Nameable nameable)
            : base(nameable)
        {
            this.PositionRepository = new TargetXyztPositionRepository(new Nameable(nameof(this.PositionRepository)));
        }
        public LifterPlateTransferAssistant() : this(new Nameable()) { }
        #endregion

        #region Property
        [Composite(ElementKind.Element)]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.OneOnly)]
        public TargetXyztPositionRepository PositionRepository
        {
            get { return this.m_PositionRepository; }
            protected set
            {
                this.Elements.SetNotNull(this.m_PositionRepository, value);
                this.m_PositionRepository = value;
            }
        }
        #endregion

        #region Method
        #endregion

        #region PlateTransferAssistant
        protected override int OnMoveToReady(int plateIndex, int carrierIndex)
        {
            int ret = 0;
            Xyzt position = new Xyzt();
            Carrier carrier = null;
            StackCarrierConfiguration stackCarrierConfiguration = null;
            double z = 0;

            this.PositionRepository.GetPosition(Positions.Ready.ToString(), this.Owner.Plates[plateIndex], out position);

            z = position.Z;
            carrier = this.Owner.Plates[plateIndex].Port.Location.GetMaterial() as Carrier;

            if(carrier != null && carrier.Presence == MaterialPresence.Exist)
            {
                stackCarrierConfiguration = carrier.Configuration as StackCarrierConfiguration;
                if(stackCarrierConfiguration == null)
                    throw new ArgumentNullException(nameof(stackCarrierConfiguration));
                // get carrier height
                if(0 < carrierIndex)
                {
                    z -= stackCarrierConfiguration.Body.MechanicalSpecification.Height * carrierIndex;
                }
            }

            if((ret = this.Owner.Z.MoveSync(z)) != 0) return ret;

            return ret;
        }

        protected override int OnMoveToScan(int plateIndex, int carrierIndex)
        {
            int ret = 0;
            Xyzt position = new Xyzt();

            // To Do : MotionSlotMapper와의 관계를 어떻게 할 것인가?
            this.PositionRepository.GetPosition(Positions.Scan.ToString(), this.Owner.Plates[plateIndex], out position);

            // get carrier height

            if((ret = this.Owner.Z.MoveSync(position.Z)) != 0) return ret;

            return ret;
        }

        protected override int OnMoveToTransfer(TransferDirection direction, int plateIndex, int carrierIndex, int slotIndex)
        {
            int ret = 0;
            Xyzt position = new Xyzt();
            Carrier carrier = null;
            double z = 0;
            StackCarrierConfiguration stackCarrierConfiguration = null;

            this.PositionRepository.GetPosition(Positions.Transfer.ToString(), this.Owner.Plates[plateIndex], out position);
            z = position.Z;
            carrier = this.Owner.Plates[plateIndex].Port.Location.GetMaterial() as Carrier;

            stackCarrierConfiguration = carrier.Configuration as StackCarrierConfiguration;
            if(stackCarrierConfiguration == null)
                throw new ArgumentNullException(nameof(stackCarrierConfiguration));
            // get carrier height
            if(0 < carrierIndex)
            {
                z -= stackCarrierConfiguration.Body.MechanicalSpecification.Height * carrierIndex;
            }

            if(direction == TransferDirection.Receive)
            {
                z -= stackCarrierConfiguration.Body.TransferSpecification.LowerDistance;
            }

            z -= carrier.Configuration.Body.MechanicalSpecification.Pitch * slotIndex;

            if((ret = this.Owner.Z.MoveSync(z)) != 0) return ret;

            return ret;
        } 
        
        public int MoveDistanceLowerDistance(int plateIndex)
        {
            int ret = 0;
            StackCarrierConfiguration stackCarrierConfiguration = null;
            Carrier carrier = null;

            carrier = this.Owner.Plates[plateIndex].Port.Location.GetMaterial() as Carrier;
            stackCarrierConfiguration = carrier.Configuration as StackCarrierConfiguration;
            if(stackCarrierConfiguration == null)
                throw new ArgumentNullException(nameof(stackCarrierConfiguration));

            if((ret = this.Owner.Z.MoveDistanceSync(stackCarrierConfiguration.Body.TransferSpecification.LowerDistance)) != 0) return ret;

            return ret;
        }
        #endregion

        #region IElementConfigurable<LifterPlateTransferAssistantConfiguration> Members      
        /// <summary>
        /// 객체의 구성 정보를 가져옵니다.
        /// </summary>
        public LifterPlateTransferAssistantConfiguration Configuration
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
            LifterPlateTransferAssistantConfiguration specialized = configuration as LifterPlateTransferAssistantConfiguration;

            if(specialized == null)
                throw new ArgumentNullException("configuration");

            if(this.Configuration == null || this.Configuration == configuration)
                this.Configuration = MechaSys.SoftBricks.DotNetUtility.CopyUtility.GetDeepCopy<LifterPlateTransferAssistantConfiguration>(specialized);
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
            LifterPlateTransferAssistantConfigurationBody specialized = body as LifterPlateTransferAssistantConfigurationBody;

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
            LifterPlateTransferAssistantConfigurationBody specialized = body as LifterPlateTransferAssistantConfigurationBody;

            if(specialized == null)
                throw new ArgumentNullException("body");

            // To Do: 구성 정보의 내용을 올바른지 검증한다
            // 자동 수정이 가능한 경우 (항목 추가, 삭제 등)는 자동으로 변경하도록 한다.
            // 변경 내용이 발생한 경우 modified = true로 설정한다.

            return ret;
        }
        #endregion

        #region ISupportElementConfigurationEditor Members
        public virtual Control GetElementConfigurationEditor()
        {
            return new LifterPlateTransferAssistantConfigurationEditor(this);
        }
        #endregion

        #region ISupportElementMaintenanceControl Members
        public virtual Control GetElementMaintenanceControl()
        {
            return new LifterPlateTransferAssistantMaintenanceControl(this);
        }
        #endregion        

        #region Part
        #endregion

        #region Element
        public new LifterLoadPort Owner
        {
            get { return base.Owner as LifterLoadPort; }
        }

        protected new LifterPlateTransferAssistantConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as LifterPlateTransferAssistantConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new LifterPlateTransferAssistantConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            this.PositionRepository.SetActions<Positions>();
            this.PositionRepository.SetTargetElements(this.Owner.Plates.ToArray());
        }
        #endregion
    }
    #endregion

    #region LifterPlateTransferAssistantConstructConfiguration
    [Serializable]
    public class LifterPlateTransferAssistantConstructConfiguration : PlateTransferAssistantConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public LifterPlateTransferAssistantConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public LifterPlateTransferAssistantConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        #endregion

        #region ConstructConfiguration
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
        }
        #endregion
    }
    #endregion

    #region LifterPlateTransferAssistantConfiguration
    [Serializable]
    public class LifterPlateTransferAssistantConfiguration : ElementConfiguration<LifterPlateTransferAssistantConfigurationBody>
    {
        #region Constructor
        public LifterPlateTransferAssistantConfiguration(LifterPlateTransferAssistant owner)
            : base(owner)
        {
        }
        public LifterPlateTransferAssistantConfiguration() : this(null) { }
        #endregion
    }

    [Serializable]
    public class LifterPlateTransferAssistantConfigurationBody : ElementConfigurationBody
    {
        #region Field

        #endregion

        #region Constructor
        public LifterPlateTransferAssistantConfigurationBody()
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