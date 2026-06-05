using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Transfer;

namespace QMC.Transfers.Feeders
{
    #region Feeder
    public abstract class Feeder : TransferableModule,
        IElementConfigurable<FeederConfiguration>
    {
        #region Field
        private FeederArmReadOnlyCollection m_Arms;
        private FeederConfiguration m_Configuration;
        #endregion

        #region Constructor
        public Feeder(Nameable nameable)
            : base(nameable)
        {
            this.Arms = new FeederArmReadOnlyCollection();

            this.MaterialRejector = new BasicMaterialRejector(new Nameable("MaterialRejector"));
        }
        public Feeder() : this(new Nameable()) { }
        #endregion

        #region Property
        public FeederArmReadOnlyCollection Arms
        {
            get { return this.m_Arms; }
            protected set { this.m_Arms = value; }
        }
        #endregion

        #region Event Handler
        private void LifeState_AfterTransit(object sender, ElementLifeAfterStateTransitionEventArgs e)
        {
            if(e.CurrentStateValue == ElementLifeStateMachine.StateEnum.Created)
                this.MakeArmList();
        }
        #endregion

        #region Method
        private void MakeArmList()
        {
            FeederArm[] arms = null;

            arms = this.Parts.GetByType<FeederArm>();

            this.Arms = new FeederArmReadOnlyCollection(arms);
        }

        protected FeederArm GetFeederArm(int portIndex)
        {
            FeederArm arm = null;

            if(portIndex < 0 || this.Arms.Count <= portIndex)
                throw new ArgumentOutOfRangeException("portIndex");

            arm = this.Arms[portIndex];

            return arm;
        }

        protected ITransferred GetTransferred(string name)
        {
            ITransferred transferred = null;

            if(string.IsNullOrEmpty(name) == true)
                throw new ArgumentNullException("name");

            transferred = Sys.Equipment.Modules.GetByName(name) as ITransferred;
            if(transferred == null)
                throw new ArgumentNullException("Secondary");

            return transferred;
        }
        #endregion

        #region IElementConfigurable<FeederConfiguration> Members
        /// <summary>
        /// 객체의 구성 정보를 가져옵니다.
        /// </summary>
        public FeederConfiguration Configuration
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
            FeederConfiguration specialized = configuration as FeederConfiguration;

            if(specialized == null)
                throw new ArgumentNullException("configuration");

            if(this.Configuration == null || this.Configuration == configuration)
                this.Configuration = MechaSys.SoftBricks.DotNetUtility.CopyUtility.GetDeepCopy<FeederConfiguration>(specialized);
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
            FeederConfigurationBody specialized = body as FeederConfigurationBody;

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
            FeederConfigurationBody specialized = body as FeederConfigurationBody;

            if(specialized == null)
                throw new ArgumentNullException("body");

            // To Do: 구성 정보의 내용을 올바른지 검증한다
            // 자동 수정이 가능한 경우 (항목 추가, 삭제 등)는 자동으로 변경하도록 한다.
            // 변경 내용이 발생한 경우 modified = true로 설정한다.

            return ret;
        }
        #endregion

        #region Part Members
        protected override int OnAbort()
        {
            int ret = 0;
            MethodCallerAsyncResultCollection ars = new MethodCallerAsyncResultCollection();

            for(int i = 0; i < this.Arms.Count; i++)
            {
                ars.Add(this.Arms[i].BeginAbort());
            }            

            if((ret = base.OnAbort()) != 0) return ret;
            if((ret = ars.WaitReturn()) != 0) return ret;

            return ret;
        }
        protected override int OnStop()
        {
            int ret = 0;
            MethodCallerAsyncResultCollection ars = new MethodCallerAsyncResultCollection();

            for(int i = 0; i < this.Arms.Count; i++)
            {
                ars.Add(this.Arms[i].BeginStop());
            }

            if((ret = base.OnStop()) != 0) return ret;
            if((ret = ars.WaitReturn()) != 0) return ret;

            return ret;
        }
        protected override int OnInitialize()
        {
            int ret = 0;
            MethodCallerAsyncResultCollection ars = new MethodCallerAsyncResultCollection();

            if((ret = base.OnInitialize()) != 0) return ret;

            for(int i = 0; i < this.Arms.Count; i++)
            {
                ars.Add(this.Arms[i].BeginInitialize());
            }
            
            if((ret = ars.WaitReturn()) != 0) return ret;

            return ret;
        }
        #endregion

        #region Element Members
        protected new FeederConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as FeederConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new FeederConstructConfiguration();
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

        protected override void OnCreate()
        {
            base.OnCreate();

            this.ElementFormProvider.FormTypes.AddForcefully(ElementFormProvider.FormEnum.Configuration, typeof(FeederConfigurationForm));
            this.ElementFormProvider.FormTypes.AddForcefully(ElementFormProvider.FormEnum.Maintenance, typeof(FeederMaintenanceForm));
        }
        #endregion
    }
    #endregion

    #region FeederConstructConfiguration
    [Serializable]
	public class FeederConstructConfiguration : TransferableModuleConstructConfiguration
	{
		#region Field
		#endregion

		#region Constructor
		public FeederConstructConfiguration(ElementConstructMethod constructMethod)
			: base(constructMethod)
		{
		}
		public FeederConstructConfiguration() : this(ElementConstructMethod.Static) { }
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

    #region FeederConfiguration
    [Serializable]
    public class FeederConfiguration : ElementConfiguration<FeederConfigurationBody>
    {
        #region Constructor
        public FeederConfiguration(Feeder owner)
            : base(owner)
        {
        }
        public FeederConfiguration() : this(null) { }
        #endregion
    }

    [Serializable]
    public class FeederConfigurationBody : ElementConfigurationBody
    {
        #region Field

        #endregion

        #region Constructor
        public FeederConfigurationBody()
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
