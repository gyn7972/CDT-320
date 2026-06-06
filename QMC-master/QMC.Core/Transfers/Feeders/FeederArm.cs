using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks.Transfer;

namespace QMC.Transfers.Feeders
{
    #region FeederArm
    public abstract class FeederArm : Part,
        ISupportElementMaintenanceControl,
        IHaveMicroTransferableAgent,
        ISupportElementDetailedMaintenanceControl
    {
        #region Define
        #endregion

        #region Field
        private MicroTransferableAgent m_MicroTransferableAgent;

        private MaterialStorablePart m_EndEffector;
        private IVerticalCylinder m_EvasiveVerticalCylinder;
        private EmergencySensor m_OverloadSensor;
        #endregion

        #region Constructor
        public FeederArm(Nameable nameable)
            : base(nameable)
        {
            this.EndEffector = new SinglePortMaterialStorablePart(new Nameable("EndEffector"));

            this.OverloadSensor = new OverloadEmergencySensor(new Nameable(nameof(this.OverloadSensor)));
            this.EvasiveVerticalCylinder = new VerticalCylinder(new Nameable(nameof(this.EvasiveVerticalCylinder)));
        }
        public FeederArm() : this(new Nameable()) { }
        #endregion

        #region Property
        [Composite]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.OneOnly)]
        public MaterialStorablePart EndEffector
        {
            get { return this.m_EndEffector; }
            protected set
            {
                this.Parts.SetNotNull(this.m_EndEffector, value);
                this.m_EndEffector = value;
            }
        }

        /// <summary>
        /// 충돌을 방지하기 위해 회피 위치로 이동하는 수직 실린더를 가져온다.
        /// </summary>
        [Composite]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.ZeroOrOne)]
        public IVerticalCylinder EvasiveVerticalCylinder
        {
            get { return this.m_EvasiveVerticalCylinder; }
            protected set
            {
                this.Parts.SetNullable(this.m_EvasiveVerticalCylinder, value);
                this.m_EvasiveVerticalCylinder = value;
            }
        }

        [Composite]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.ZeroOrOne)]
        public EmergencySensor OverloadSensor
        {
            get { return this.m_OverloadSensor; }
            protected set
            {
                this.Parts.SetNullable(this.m_OverloadSensor, value);
                this.m_OverloadSensor = value;
            }
        }
        #endregion

        #region IHaveMicroTransferableAgent Members
        [Composite(ElementKind.Element)]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.OneOnly)]
        public MicroTransferableAgent MicroTransferableAgent
        {
            get { return this.m_MicroTransferableAgent; }
            protected set
            {
                this.Elements.SetNotNull(this.m_MicroTransferableAgent, value);
                this.m_MicroTransferableAgent = value;
            }
        }
        #endregion

        #region ISupportElementDetailedMaintenanceControl Members
        public abstract Control GetElementMaintenanceControl(ElementMaintenancePurpose purpose, Module requester = null);
        #endregion

        #region ISupportElementMaintenanceControl
        public virtual Control GetElementMaintenanceControl()
        {
            return new FeederArmMaintenanceControl(this);
        }
        #endregion

        #region Part Members
        protected override int OnInitialize()
        {
            int ret = 0;

            if((ret = this.EndEffector.InitializeSync()) != 0) return ret;
            if(this.EvasiveVerticalCylinder != null)
            {
                if((ret = this.EvasiveVerticalCylinder.Up()) != 0) return ret;
            }
            return ret;
        }
        #endregion

        #region Element Members
        protected new FeederArmConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as FeederArmConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new FeederArmConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }
        #endregion
    }
    #endregion

    #region FeederArmConstructConfiguration
    [Serializable]
    public class FeederArmConstructConfiguration : PartConstructConfiguration
    {
        #region Field
        private VerticalCylinderState m_EvasiveCylinderPosition;
        #endregion

        #region Constructor
        public FeederArmConstructConfiguration(ElementConstructMethod constructMethod)
            : base(ElementKind.Part, constructMethod)
        {
        }
        public FeederArmConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        /// <summary>
        /// 충돌 회피 실린더의 회피 위치를 가져오거나 설정한다.
        /// </summary>
        [Category("FeederArm")]
        public VerticalCylinderState EvasiveCylinderPosition
        {
            get { return this.m_EvasiveCylinderPosition; }
            set { this.m_EvasiveCylinderPosition = value; }
        }
        #endregion

        #region ConstructConfiguration Members
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.EvasiveCylinderPosition = VerticalCylinderState.Down;
        }
        #endregion
    }
    #endregion

    #region FeederArmCollection
    public class FeederArmCollection : PartCollection<FeederArm>
    {
        #region Constructor
        /// <summary>
        /// 주어진 리스트를 가지도록 FeederArmCollection 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="arms">컬렉션이 가질 리스트입니다</param>
        public FeederArmCollection(IList<FeederArm> arms)
            : base(arms)
        {
        }
        /// <summary>
        /// FeederArmCollection 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public FeederArmCollection() : base() { }
        #endregion
    }
    #endregion

    #region FeederArmReadOnlyCollection
    public class FeederArmReadOnlyCollection : PartReadOnlyCollection<FeederArm>
    {
        #region Constructor
        /// <summary>
        /// 주어진 리스트를 가지도록 FeederArmReadOnlyCollection 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="arms">컬렉션이 가질 리스트입니다</param>
        public FeederArmReadOnlyCollection(IList<FeederArm> arms)
            : base(arms)
        {
        }
        /// <summary>
        /// FeederArmReadOnlyCollection 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="arms">컬렉션이 가질 리스트입니다</param>
        public FeederArmReadOnlyCollection(params FeederArm[] arms) : base(arms) { }
        #endregion
    }
    #endregion
}
