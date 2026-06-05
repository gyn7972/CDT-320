using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.LoadPorts.SlotMappers;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Transfer;
using System.ComponentModel;
using MechaSys.SoftBricks.Hmi;
using System.Windows.Forms;

namespace QMC.Transfers.Feeders
{
    #region MotionFeederArm
    public abstract class MotionFeederArm : FeederArm,
        IElementConfigurable<MotionFeederArmConfiguration>,
        ISupportElementConfigurationEditor,
        ISupportElementMaintenanceControl
    {
        #region Define
        [Serializable]
        public enum Positions
        {
            Extend,
            Retract,
        }
        #endregion

        #region Field
        private TransferXyztPositionRepository m_PositionRepository;
        private MotionFeederArmConfiguration m_Configuration;
        #endregion

        #region Constructor
        public MotionFeederArm(Nameable nameable)
            : base(nameable)
        {
            this.MicroTransferableAgent = new MotionFeederArmMicroTransferableAgent(new Nameable(nameof(this.MicroTransferableAgent)));

            this.PositionRepository = new TransferXyztPositionRepository(new Nameable(nameof(this.PositionRepository)));
        }
        public MotionFeederArm() : this(new Nameable()) { }
        #endregion

        #region Property      
        [Composite(ElementKind.Element)]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.OneOnly)]
        public TransferXyztPositionRepository PositionRepository
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

        #region IElementConfigurable<MotionFeederArmConfiguration> Members      
        /// <summary>
        /// 객체의 구성 정보를 가져옵니다.
        /// </summary>
        public MotionFeederArmConfiguration Configuration
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
            MotionFeederArmConfiguration specialized = configuration as MotionFeederArmConfiguration;

            if(specialized == null)
                throw new ArgumentNullException("configuration");

            if(this.Configuration == null || this.Configuration == configuration)
                this.Configuration = MechaSys.SoftBricks.DotNetUtility.CopyUtility.GetDeepCopy<MotionFeederArmConfiguration>(specialized);
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
            MotionFeederArmConfigurationBody specialized = body as MotionFeederArmConfigurationBody;

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
            MotionFeederArmConfigurationBody specialized = body as MotionFeederArmConfigurationBody;

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
            return new MotionFeederArmConfigurationEditor(this);
        }
        #endregion

        #region ISupportElementMaintenanceControl
        public override Control GetElementMaintenanceControl()
        {
            return new MotionFeederArmMaintenanceControl(this);
        }
        #endregion

        #region Part
        #endregion

        #region Element
        protected new MotionFeederArmConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as MotionFeederArmConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new MotionFeederArmConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if (this.ConstructConfiguration == null) return;
        }
        #endregion
    }
    #endregion

    #region MotionFeederArmConstructConfiguration
    [Serializable]
    public class MotionFeederArmConstructConfiguration : FeederArmConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public MotionFeederArmConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public MotionFeederArmConstructConfiguration() : this(ElementConstructMethod.Static) { }
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

    #region MotionFeederArmConfiguration
    [Serializable]
    public class MotionFeederArmConfiguration : ElementConfiguration<MotionFeederArmConfigurationBody>
    {
        #region Constructor
        public MotionFeederArmConfiguration(MotionFeederArm owner)
            : base(owner)
        {
        }
        public MotionFeederArmConfiguration() : this(null) { }
        #endregion
    }

    [Serializable]
    public class MotionFeederArmConfigurationBody : ElementConfigurationBody
    {
        #region Field

        #endregion

        #region Constructor
        public MotionFeederArmConfigurationBody()
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