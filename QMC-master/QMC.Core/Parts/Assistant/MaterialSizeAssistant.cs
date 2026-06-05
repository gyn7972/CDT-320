using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Configurations.Controls;
using MechaSys.SoftBricks.Parts;

namespace QMC.Parts.Assistant
{
    #region MaterialSizeAssistant
    public class MaterialSizeAssistant : Element,
        IElementConfigurable<MaterialSizeAssistantConfiguration>
    {
        #region Define
        /// <summary>
        /// 사이즈 정보를 사용하는 모드를 정의
        /// </summary>
        [Serializable]
        public enum Modes
        {
            /// <summary>
            /// 하드웨어가 교체되어야만 사용가능한 모드.
            /// Carrier가 생성될때 사이즈 정보가 다르다면 진행 불가
            /// </summary>
            Hardware,

            /// <summary>
            /// 하드웨어 교체없이 소프트웨어만 변경되면 사용가능한 모드
            /// Carrier가 생성될때 선택되어 있는 사이즈 정보를 사용
            /// </summary>
            Software,
        }
        #endregion

        #region Field
        private NeedMaterialSizeReadOnlyCollection m_NeedMaterialSizeElements;
        private MaterialSizeSpecificationKeyedCollection m_MaterialSizeSpecifications;
        #endregion

        #region Constructor
        public MaterialSizeAssistant(Nameable nameable)
			: base(nameable)
		{
		}
		public MaterialSizeAssistant() : this(new Nameable()) { }
        #endregion

        #region Property
        public NeedMaterialSizeReadOnlyCollection NeedMaterialSizeElements
        {
            get { return this.m_NeedMaterialSizeElements; }
            private set { this.m_NeedMaterialSizeElements = value; }
        }

        public MaterialSizeSpecificationKeyedCollection MaterialSizeSpecifications
        {
            get { return this.m_MaterialSizeSpecifications; }
            private set { this.m_MaterialSizeSpecifications = value; }
        }

        public int SelectedSize
        {
            get { return this.Configuration.Body.SelectedSize; }
        }

        public Modes Mode
        {
            get { return this.ConstructConfiguration.Mode; }
        }
        #endregion

        #region Method
        public int SelectSize(int size)
        {
            int ret = 0;

            this.Configuration.Body.SelectedSize = size;
            this.VerifyConfiguration(this.Configuration);
            this.ApplyConfiguration(this.Configuration);

            return ret;
        }
        #endregion

        #region IElementConfigurable<MaterialSizeAssistantConfiguration> Members
        private MaterialSizeAssistantConfiguration m_Configuration;
        /// <summary>
        /// 객체의 구성 정보를 가져옵니다.
        /// </summary>
        public MaterialSizeAssistantConfiguration Configuration
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
            MaterialSizeAssistantConfiguration specialized = configuration as MaterialSizeAssistantConfiguration;

            if(specialized == null)
                throw new ArgumentNullException("configuration");

            if(this.Configuration == null || this.Configuration == configuration)
                this.Configuration = MechaSys.SoftBricks.DotNetUtility.CopyUtility.GetDeepCopy<MaterialSizeAssistantConfiguration>(specialized);
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
            MaterialSizeAssistantConfigurationBody specialized = body as MaterialSizeAssistantConfigurationBody;

            if(specialized == null)
                throw new ArgumentNullException("body");

            // To Do: 변경된 구성 정보를 적용한다.
            for(int i = 0; i < this.NeedMaterialSizeElements.Count; i++)
            {
                this.NeedMaterialSizeElements[i].SelectSize(specialized.SelectedSize);
            }

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
            MaterialSizeAssistantConfigurationBody specialized = body as MaterialSizeAssistantConfigurationBody;

            if(specialized == null)
                throw new ArgumentNullException("body");

            // To Do: 구성 정보의 내용을 올바른지 검증한다
            // 자동 수정이 가능한 경우 (항목 추가, 삭제 등)는 자동으로 변경하도록 한다.
            // 변경 내용이 발생한 경우 modified = true로 설정한다.

            if(this.MaterialSizeSpecifications.Contains(specialized.SelectedSize) == false)
            {
                specialized.SelectedSize = this.MaterialSizeSpecifications.SelectedSize;
                modified = true;
            }

            return ret;
        }
        #endregion

        #region Element Members
        public new MaterialSizeManager Owner
        {
            get { return base.Owner as MaterialSizeManager; }
        }

		protected new MaterialSizeAssistantConstructConfiguration ConstructConfiguration
		{
			get { return base.ConstructConfiguration as MaterialSizeAssistantConstructConfiguration; }
		}

		protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
		{
			return new MaterialSizeAssistantConstructConfiguration();
		}

		protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
		{
            MaterialSizeSpecificationKeyedCollection materialSizeSpecifications = null;
            MaterialSizeSpecification materialSizeSpecification = null;

            base.OnSetConstructConfiguration(configuration);

			if(this.ConstructConfiguration == null) return;

            materialSizeSpecifications = new MaterialSizeSpecificationKeyedCollection();

            for(int i = 0; i < this.ConstructConfiguration.AvailableSubstrateSizes.Count; i++)
            {
                materialSizeSpecification = new MaterialSizeSpecification();
                materialSizeSpecification.Size = this.ConstructConfiguration.AvailableSubstrateSizes[i];

                materialSizeSpecifications.Add(materialSizeSpecification);
            }

            this.MaterialSizeSpecifications = materialSizeSpecifications;
        }

        protected override void OnSetAggregation()
        {
            INeedMaterialSize needMaterialSizeElement = null;
            List<INeedMaterialSize> needMaterialSizeElements = new List<INeedMaterialSize>();

            base.OnSetAggregation();

            foreach(string locator in this.ConstructConfiguration.ElementLocators)
            {
                needMaterialSizeElement = ElementList.GetByLocator<INeedMaterialSize>(locator);
                if(needMaterialSizeElement == null)
                    throw new ArgumentNullException("ElementLocators", string.Format("Element [{0}] does not found", locator));
                needMaterialSizeElements.Add(needMaterialSizeElement);
            }

            this.NeedMaterialSizeElements = new NeedMaterialSizeReadOnlyCollection(needMaterialSizeElements);
        }
        #endregion
    }
	#endregion

	#region MaterialSizeAssistantConstructConfiguration
	[Serializable]
	public class MaterialSizeAssistantConstructConfiguration : ElementConstructConfiguration
	{
        #region Field
        private MaterialSizeAssistant.Modes m_Mode;
        private StringCollection m_ElementLocators;
        private Int32Collection m_AvailableSubstrateSizes;
        #endregion

        #region Constructor
        public MaterialSizeAssistantConstructConfiguration(ElementConstructMethod constructMethod)
			: base(ElementKind.Element, constructMethod)
		{
            this.Mode = MaterialSizeAssistant.Modes.Hardware;
		}
		public MaterialSizeAssistantConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        [Category("Material")]
        public MaterialSizeAssistant.Modes Mode
        {
            get { return this.m_Mode; }
            set { this.m_Mode = value; }
        }

        [Category("Material")]
        [Editor(typeof(ElementLocatorCollectionUITypeEditor<INeedMaterialSize>), typeof(UITypeEditor))]
        public StringCollection ElementLocators
        {
            get { return this.m_ElementLocators; }
            set { this.m_ElementLocators = value; }
        }

        /// <summary>
        /// 지원하는 substrate size(들)을 가져오거나 설정한다.
        /// 설정하지 않은 경우는 1가지 사이즈로 대응한다.
        /// </summary>
        [Category("Material")]
        public Int32Collection AvailableSubstrateSizes
        {
            get { return this.m_AvailableSubstrateSizes; }
            set { this.m_AvailableSubstrateSizes = value; }
        }
        #endregion

        #region ConstructConfiguration Members
        protected override void SetDefaultValues()
		{
			base.SetDefaultValues();

            if(this.ElementLocators == null)
                this.ElementLocators = new StringCollection();

            if(this.AvailableSubstrateSizes == null)
                this.AvailableSubstrateSizes = new Int32Collection();
        }
		#endregion
	}
    #endregion

    #region MaterialSizeAssistantConfiguration
    [Serializable]
	public class MaterialSizeAssistantConfiguration : ElementConfiguration<MaterialSizeAssistantConfigurationBody>
	{
		#region Constructor
		public MaterialSizeAssistantConfiguration(MaterialSizeAssistant owner)
			: base(owner)
		{
		}
		public MaterialSizeAssistantConfiguration() : this(null) { }
		#endregion
	}

	[Serializable]
	public class MaterialSizeAssistantConfigurationBody : ElementConfigurationBody
	{
        #region Field
        private int m_SelectedSize;
        #endregion

        #region Constructor
        public MaterialSizeAssistantConfigurationBody()
		{
		}
        #endregion

        #region Property
        public int SelectedSize
        {
            get { return this.m_SelectedSize; }
            set { this.m_SelectedSize = value; }
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

    #region MaterialSizeAssistantReadOnlyCollection
    public class MaterialSizeAssistantReadOnlyCollection : ElementReadOnlyCollection<MaterialSizeAssistant>
    {
        #region Constructor
        /// <summary>
        /// 주어진 리스트를 가지도록 MaterialSizeAssistantReadOnlyCollection 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="elements">컬렉션이 가질 리스트입니다</param>
        public MaterialSizeAssistantReadOnlyCollection(IList<MaterialSizeAssistant> elements)
            : base(elements)
        {
        }
        /// <summary>
        /// MaterialSizeAssistantReadOnlyCollection 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="elements">컬렉션이 가질 리스트입니다</param>
        public MaterialSizeAssistantReadOnlyCollection(params MaterialSizeAssistant[] elements) : base(elements) { }
        #endregion
    }
    #endregion
}
