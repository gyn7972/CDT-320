using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Text;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Configurations.Controls;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks.Motions;

using QMC.Parts;
using QMC.Parts.Assistant;

namespace QMC.LoadPorts
{
    #region LifterLoadPort
    public class LifterLoadPort : PlateTransferLoadPort,
        IElementConfigurable<LifterLoadPortConfiguration>,
        ISupportElementDetailedMaintenanceControl
    {
        #region Deifne
        [Serializable]
        public new enum AlarmKeys
        {
            HardwareWaferSizeSettingDoesNotMatch,
        }
        #endregion

        #region Field
        private LifterLoadPortConfiguration m_Configuration;

        private Motion m_Z;

        private MaterialSizeAssistant m_MaterialSizeAssistant;
        #endregion

        #region Constructor
        public LifterLoadPort(Nameable nameable)
            : base(nameable)
        {
            this.PlateTransferAssistant = new LifterPlateTransferAssistant(new Nameable(nameof(this.PlateTransferAssistant)));

            this.SlotMapper = new LifterLoadPortSlotMapper(new Nameable(nameof(this.SlotMapper)));

            this.Z = new SingleAxisMotion(new Nameable(nameof(this.Z)));
        }
        public LifterLoadPort() : this(new Nameable()) { }
        #endregion

        #region Property
        [Composite]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.OneOnly)]
        public Motion Z
        {
            get { return this.m_Z; }
            protected set
            {
                this.Parts.SetNotNull(this.m_Z, value);
                this.m_Z = value;
            }
        }

        [Aggregate(ElementKind.Element)]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.ZeroOrOne)]
        public MaterialSizeAssistant MaterialSizeAssistant
        {
            get { return this.m_MaterialSizeAssistant; }
            protected set { this.m_MaterialSizeAssistant = value; }
        }
        #endregion

        #region Method
        private bool CheckCarrierWaferSize(out int carrierSize)
        {
            int ret = 0;

            carrierSize = 0;

            for(int i = 0; i < this.Plates.Count; i++)
            {
                if(this.Plates[i].MaterialDetector is IWaferSizeDetector waferSizeDetector)
                {
                    if((ret = waferSizeDetector.GetSize(ref carrierSize)) != 0) return false;
                    if(carrierSize > 0 && this.MaterialSizeAssistant.SelectedSize != carrierSize) return false;
                }
            }
            return true;
        }
        #endregion

        #region PlateTransferLoadPort
        public new LifterPlateTransferAssistant PlateTransferAssistant
        {
            get { return base.PlateTransferAssistant as LifterPlateTransferAssistant; }
            protected set { base.PlateTransferAssistant = value; }
        }
        #endregion

        #region IElementConfigurable<LifterLoadPortConfiguration>
        /// <summary>
        /// 객체의 구성 정보를 가져옵니다.
        /// </summary>
        public LifterLoadPortConfiguration Configuration
        {
            get { return this.m_Configuration; }
            protected set { this.m_Configuration = value; }
        }
        #endregion

        #region IElementConfigurable
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
            LifterLoadPortConfiguration specialized = configuration as LifterLoadPortConfiguration;

            if (specialized == null)
                throw new ArgumentNullException("configuration");

            if (this.Configuration == null || this.Configuration == configuration)
                this.Configuration = MechaSys.SoftBricks.DotNetUtility.CopyUtility.GetDeepCopy<LifterLoadPortConfiguration>(specialized);
            if (this.Configuration.Revision < specialized.Revision)
                this.Configuration.Revision = specialized.Revision;

            if ((ret = this.OnApplyConfiguration(specialized.Body)) != 0) return ret;

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
            LifterLoadPortConfigurationBody specialized = body as LifterLoadPortConfigurationBody;

            if (specialized == null)
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

            if ((ret = this.OnVerifyConfiguration(configuration.Body, ref modified)) != 0) return ret;

            if (modified == true)
            {
                if ((ret = ElementConfigurator.Save(configuration)) != 0) return ret;
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
            LifterLoadPortConfigurationBody specialized = body as LifterLoadPortConfigurationBody;

            if (specialized == null)
                throw new ArgumentNullException("body");

            // To Do: 구성 정보의 내용을 올바른지 검증한다
            // 자동 수정이 가능한 경우 (항목 추가, 삭제 등)는 자동으로 변경하도록 한다.
            // 변경 내용이 발생한 경우 modified = true로 설정한다.

            return ret;
        }
        #endregion

        #region ISupportElementDetailedMaintenanceControl Members
        public virtual Control GetElementMaintenanceControl(ElementMaintenancePurpose purpose, Module requester = null)
        {
            LifterPlateTransferAssistantMaintenanceControl control = new LifterPlateTransferAssistantMaintenanceControl(this.PlateTransferAssistant);

            if(purpose == ElementMaintenancePurpose.TransferTeaching)
            {
                control.MaintenancePurpose = ElementMaintenancePurpose.TransferTeaching;
                control.Requester = requester;
            }

            return control;
        }
        #endregion

        #region Event Hadlers
        private void LifeState_AfterTransit(object sender, ElementLifeAfterStateTransitionEventArgs e)
        {
            if(e.CurrentStateValue == ElementLifeStateMachine.StateEnum.Prepared)
            {
                this.Z.BeforeMove += Z_BeforeMove;
            }
        }

        private void Z_BeforeMove(object sender, MoveEventArgs e)
        {
            for(int i = 0; i < this.Plates.Count; i++)
            {
                //if((e.Result = this.Plates[i].CheckProtrusion()) != 0) return;
                if(this.Plates[i] is LockableLoadPortPlate lockable && lockable.Locker is CarrierLocker locker)
                { 
                    if((e.Result = locker.CheckLock()) != 0) return;
                }
            }
        }
        private void Alarm_Recovered(Alarm sender, AlarmEventArgs e)
        {
            if(this.CheckCarrierWaferSize(out int carrierSize) == false)
            {
                Alarm alarm = null;
                
                alarm = this.Alarms[AlarmKeys.HardwareWaferSizeSettingDoesNotMatch];

                if(alarm.IsPosted == false)
                {
                    alarm.Replace.Clear();
                    alarm.Replace.Add("{A}", this.MaterialSizeAssistant.SelectedSize.ToString());
                    alarm.Replace.Add("{B}", carrierSize.ToString());

                    alarm.Post(this);
                }
            }
        }

        private void WaferSizeDetector_WaferSizeChanged(object sender, WaferSizeChangedEventArgs e)
        {
            if(e.Current > 0 && this.MaterialSizeAssistant.SelectedSize != e.Current)
            {
                if(this.MaterialSizeAssistant.Mode == MaterialSizeAssistant.Modes.Hardware)
                {
                    if(this.Alarms[AlarmKeys.HardwareWaferSizeSettingDoesNotMatch].IsPosted == false)
                    {
                        if((e.Result = this.Alarms[AlarmKeys.HardwareWaferSizeSettingDoesNotMatch].Post(this)) != 0) return;
                    }
                }
                else if(this.MaterialSizeAssistant.Mode == MaterialSizeAssistant.Modes.Software)
                {
                    this.MaterialSizeAssistant.Configuration.Body.SelectedSize = e.Current;
                    if((e.Result = ElementConfigurator.Save(this.MaterialSizeAssistant.Configuration)) != 0) return;
                    if((e.Result = this.MaterialSizeAssistant.SelectSize(e.Current)) != 0) return;
                }
            }
        }
        #endregion

        #region Part Members
        protected override int OnAbort()
        {
            int ret = 0;
            if((ret = base.OnAbort()) != 0) return ret;
            if((ret = this.Z.AbortSync()) != 0) return ret;
            return ret;
        }
        protected override int OnStop()
        {
            int ret = 0;
            if((ret = base.OnStop()) != 0) return ret;
            if((ret = this.Z.StopSync()) != 0) return ret;
            return ret;
        }
        protected override int OnInitialize()
        {
            int ret = 0;
            MethodCallerAsyncResultCollection ars = new MethodCallerAsyncResultCollection();

            if((ret = base.OnInitialize()) != 0) return ret;
            if((ret = this.Z.InitializeSync()) != 0) return ret;

            //for(int i = 0; i < this.Plates.Count; i++)
            //{
            //    ars.Add(this.Plates[i].BeginInitialize());
            //}
            if((ret = ars.WaitReturn()) != 0) return ret;

            return ret;
        }
        protected override void OnInstantiated()
        {
            base.OnInstantiated();

            this.LifeState.AfterTransit += LifeState_AfterTransit;
        }       
        #endregion

        #region Element
        protected new LifterLoadPortConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as LifterLoadPortConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new LifterLoadPortConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if (this.ConstructConfiguration == null) return;
        }

        protected override void OnSetAggregation()
        {
            MaterialSizeAssistant value = null;

            base.OnSetAggregation();
    
            if(string.IsNullOrEmpty(this.ConstructConfiguration.MaterialSizeAssistantLocator) == false)
            {
                value = ElementList.GetByLocator<MaterialSizeAssistant>(this.ConstructConfiguration.MaterialSizeAssistantLocator);
                if(value == null)
                    throw new ArgumentNullException("MaterialSizeAssistantLocator", string.Format("The MaterialSizeAssistant[{0}] not found.", this.ConstructConfiguration.MaterialSizeAssistantLocator));
                this.MaterialSizeAssistant = value;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            this.ElementFormProvider.FormTypes.AddForcefully(ElementFormProvider.FormEnum.Configuration, typeof(LifterLoadPortConfigurationForm));
            this.ElementFormProvider.FormTypes.AddForcefully(ElementFormProvider.FormEnum.Maintenance, typeof(LifterLoadPortMaintenanceForm));
            this.ElementFormProvider.FormTypes.AddForcefully(ElementFormProvider.FormEnum.Configuration, typeof(LifterLoadPortConfigurationForm));

            for(int i = 0; i < this.Plates.Count; i++)
            {
                if(this.Plates[i].MaterialDetector is IWaferSizeDetector waferSizeDetector)
                    waferSizeDetector.WaferSizeChanged += WaferSizeDetector_WaferSizeChanged;
            }
        }

        protected override void OnMakeAlarms()
        {
            Alarm alarm = null;
            StringBuilder stringBuilder = null;

            base.OnMakeAlarms();

            alarm = new Alarm((int)AlarmKeys.HardwareWaferSizeSettingDoesNotMatch, AlarmKeys.HardwareWaferSizeSettingDoesNotMatch.ToString());
            alarm.AlarmGrade = AlarmGrade.Fail;
            alarm.Recovered += Alarm_Recovered;

            stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("The configured wafer size does not match the detected carrier size");
            stringBuilder.AppendLine("Configured Wafer Size : {A}");
            stringBuilder.AppendLine("Detected Carrier Size : {B}");

            alarm.Cause = stringBuilder.ToString();
            alarm.Remedy = "Please verify the wafer size setting and ensure the correct carrier is loaded";

            this.Alarms.Add(AlarmKeys.HardwareWaferSizeSettingDoesNotMatch, alarm);
        }       
        #endregion
    }
    #endregion

    #region LifterLoadPortConstructConfiguration
    [Serializable]
    public class LifterLoadPortConstructConfiguration : PlateTransferLoadPortConstructConfiguration
    {
        #region Field
        private string m_MaterialSizeAssistantLocator;
        #endregion

        #region Constructor
        public LifterLoadPortConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public LifterLoadPortConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        [Editor(typeof(ElementLocatorUITypeEditor<MaterialSizeAssistant>), typeof(UITypeEditor))]
        public string MaterialSizeAssistantLocator
        {
            get { return this.m_MaterialSizeAssistantLocator; }
            set { this.m_MaterialSizeAssistantLocator = value; }
        }
        #endregion

        #region ConstructConfiguration
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
        }
        #endregion
    }
    #endregion

    #region LifterLoadPortConfiguration
    [Serializable]
    public class LifterLoadPortConfiguration : ElementConfiguration<LifterLoadPortConfigurationBody>
    {
        #region Constructor
        public LifterLoadPortConfiguration(LifterLoadPort owner)
            : base(owner)
        {
        }
        public LifterLoadPortConfiguration() : this(null) { }
        #endregion
    }

    [Serializable]
    public class LifterLoadPortConfigurationBody : ElementConfigurationBody
    {
        #region Field

        #endregion

        #region Constructor
        public LifterLoadPortConfigurationBody()
        {
        }
        #endregion

        #region Property
        #endregion

        #region ElementConfigurationBody
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
        }
        #endregion
    }
    #endregion
}