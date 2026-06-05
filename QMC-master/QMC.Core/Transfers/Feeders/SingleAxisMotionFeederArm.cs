using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Configurations.Controls;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Transfer;

using QMC.LoadPorts;
using QMC.Parts.Assistant;

namespace QMC.Transfers.Feeders
{
    #region SingleAxisMotionFeederArm
    public class SingleAxisMotionFeederArm : MotionFeederArm
    {
        #region Field
        private Motion m_TravelMotion;
        private PartInitializeAssistant m_InitializeAssistant;
        #endregion

        #region Constructor
        public SingleAxisMotionFeederArm(Nameable nameable)
            : base(nameable)
        {
            this.MicroTransferableAgent = new SingleAxisMotionFeederArmMicroTransferableAgent(new Nameable(nameof(this.MicroTransferableAgent)));
        }
        public SingleAxisMotionFeederArm() : this(new Nameable()) { }
        #endregion

        #region Property
        /// <summary>
        /// material를 밀고 당기는 모션을 가져온다.
        /// </summary>
        /// <remarks>
        /// 설비에 설치되는 방향 때문에 고정하지 않도록 한다.
        /// Bricklayer를 이용하여 "X" 또는 "Y"로 추가 할 수 있도록 한다.
        /// </remarks>
        [Aggregate]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.OneOnly)]
        public Motion TravelMotion
        {
            get { return this.m_TravelMotion; }
            protected set { this.m_TravelMotion = value; }
        }
     
        [Composite(ElementKind.Element)]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.ZeroOrOne)]
        public PartInitializeAssistant InitializeAssistant
        {
            get { return this.m_InitializeAssistant; }
            protected set
            {
                this.Elements.SetNullable(this.m_InitializeAssistant, value);
                this.m_InitializeAssistant = value;
            }
        }
        #endregion

        #region Method
        #endregion

        #region IElementConfigurable Members
        /// <summary>
        /// 객체의 구성 정보를 가져옵니다.
        /// </summary>
        public new SingleAxisMotionFeederArmConfiguration Configuration
        {
            get { return base.Configuration as SingleAxisMotionFeederArmConfiguration; }
            protected set { base.Configuration = value; }
        }

        /// <summary>
        /// 주어진 구성 정보를 반영합니다.
        /// </summary>
        /// <param name="body">구성 정보의 세부 항목입니다.</param>
        /// <returns>작업에 대한 결과를 반환합니다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
        protected override int OnApplyConfiguration(ElementConfigurationBody body)
        {
            int ret = 0;
            SingleAxisMotionFeederArmConfigurationBody specialized = body as SingleAxisMotionFeederArmConfigurationBody;

            if((ret = base.OnApplyConfiguration(body)) != 0) return ret;

            if(specialized == null)
                throw new ArgumentNullException("body");

            // To Do: 변경된 구성 정보를 적용한다.

            return ret;
        }

        /// <summary>
        /// 구성 정보의 내용이 올바른지 검증한다.
        /// </summary>
        /// <param name="body">검증할 구성 정보 바디이다.</param>
		/// <param name="modified">변경 여부를 반환한다.</param>
        protected override int OnVerifyConfiguration(ElementConfigurationBody body, ref bool modified)
        {
            int ret = 0;
            SingleAxisMotionFeederArmConfigurationBody specialized = body as SingleAxisMotionFeederArmConfigurationBody;

            if((ret = base.OnVerifyConfiguration(body, ref modified)) != 0) return ret;

            if(specialized == null)
                throw new ArgumentNullException("body");

            // To Do: 구성 정보의 내용을 올바른지 검증한다
            // 자동 수정이 가능한 경우 (항목 추가, 삭제 등)는 자동으로 변경하도록 한다.
            // 변경 내용이 발생한 경우 modified = true로 설정한다.

            return ret;
        }
        #endregion

        #region ISupportElementDetailedMaintenanceControl Members
        public override Control GetElementMaintenanceControl(ElementMaintenancePurpose purpose, Module requester = null)
        {
            SingleAxisMotionFeederArmMaintenanceControl control = new SingleAxisMotionFeederArmMaintenanceControl(this);

            if(purpose == ElementMaintenancePurpose.TransferTeaching)
            {
                control.MaintenancePurpose = ElementMaintenancePurpose.TransferTeaching;
                control.Requester = requester;
            }

            return control;
        }
        #endregion

        #region ISupportElementConfigurationEditor Members
        public override Control GetElementConfigurationEditor()
        {
            return new SingleAxisMotionFeederArmConfigurationEditor(this);
        }
        #endregion

        #region ISupportElementMaintenanceControl
        public override Control GetElementMaintenanceControl()
        {
            return new SingleAxisMotionFeederArmMaintenanceControl(this);
        }
        #endregion

        #region Part
        protected override int OnAbort()
        {
            int ret = 0;
            if((ret = this.TravelMotion.AbortSync()) != 0) return ret;
            return ret;
        }

        protected override int OnInitialize()
        {
            int ret = 0;
            if(this.InitializeAssistant == null)
            {
                if((ret = base.OnInitialize()) != 0) return ret;
                if((ret = this.TravelMotion.InitializeSync()) != 0) return ret;
            }
            else
            {
                if((ret = this.InitializeAssistant.InitializeSync()) != 0) return ret;
            }
            return ret;
        }

        protected override int OnStop()
        {
            int ret = 0;
            if((ret = this.TravelMotion.StopSync()) != 0) return ret;
            return ret;
        }
        #endregion

        #region Element
        protected new SingleAxisMotionFeederArmConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as SingleAxisMotionFeederArmConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new SingleAxisMotionFeederArmConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if (this.ConstructConfiguration == null) return;
        }

        protected override void OnSetAggregation()
        {
            Motion travelMotion = null;
            Motion[] motions = null;

            base.OnSetAggregation();

            if (string.IsNullOrEmpty(this.ConstructConfiguration.TravelMotionLocator) == false)
            {
                travelMotion = ElementList.GetByLocator<Motion>(this.ConstructConfiguration.TravelMotionLocator);
                if (travelMotion == null)
                    throw new ArgumentNullException("TravelMotionLocator", string.Format("The TravelMotion[{0}] does not found.", this.ConstructConfiguration.TravelMotionLocator));
                this.TravelMotion = travelMotion;
            }
            else
            {
                motions = this.Parts.GetByType<Motion>();
                if (motions != null && motions.Length == 1)
                    this.TravelMotion = motions[0];
            }
            if (this.TravelMotion == null)
                throw new ArgumentNullException("TravelMotion");
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            if(this.Owner.GetModule() is ITransferable transferable)
            {
                this.PositionRepository.SetActions<Positions>();
                this.PositionRepository.SetTransferable(transferable);
            }
        }
        #endregion
    }
    #endregion

    #region SingleAxisMotionFeederArmConstructConfiguration
    [Serializable]
    public class SingleAxisMotionFeederArmConstructConfiguration : MotionFeederArmConstructConfiguration
    {
        #region Field
        private string m_TravelMotionLocator;
        #endregion

        #region Constructor
        public SingleAxisMotionFeederArmConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public SingleAxisMotionFeederArmConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        [Editor(typeof(ElementLocatorUITypeEditor<Motion>), typeof(UITypeEditor))]
        public string TravelMotionLocator
        {
            get { return this.m_TravelMotionLocator; }
            set { this.m_TravelMotionLocator = value; }
        }
        #endregion

        #region ConstructConfiguration
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.TravelMotionLocator = "";
        }
        #endregion
    }
    #endregion

    #region SingleAxisMotionFeederArmConfiguration
    [Serializable]
    public class SingleAxisMotionFeederArmConfiguration : MotionFeederArmConfiguration
    {
        #region Constructor
        public SingleAxisMotionFeederArmConfiguration(SingleAxisMotionFeederArm owner)
            : base(owner)
        {
        }
        public SingleAxisMotionFeederArmConfiguration() : this(null) { }
        #endregion

        #region ElementConfiguration Members
        public new SingleAxisMotionFeederArmConfigurationBody Body
        {
            get { return base.Body as SingleAxisMotionFeederArmConfigurationBody; }
            set { base.Body = value; }
        }
        #endregion

        #region Configuration Members
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.Body = new SingleAxisMotionFeederArmConfigurationBody();
        }
        #endregion
    }

    [Serializable]
    public class SingleAxisMotionFeederArmConfigurationBody : MotionFeederArmConfigurationBody
    {
        #region Field
        private bool m_EnableAlign;
        #endregion

        #region Constructor
        public SingleAxisMotionFeederArmConfigurationBody()
        {
        }
        #endregion

        #region Property
        public bool EnableAlign
        {
            get { return this.m_EnableAlign; }
            set { this.m_EnableAlign = value; }
        }
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