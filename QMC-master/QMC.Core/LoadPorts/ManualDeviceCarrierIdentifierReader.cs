using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.DotNetUtility;
using MechaSys.SoftBricks.EventReports;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Secs;

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace QMC.LoadPorts
{
    #region ManualDeviceCarrierIdentifierReader
    public class ManualDeviceCarrierIdentifierReader : ArtificialCarrierIdentifierReader,
        ISupportCollectionEventConfiguration
    {
        #region Define
        [Serializable]
        public enum CollectionEventEnum
        {
            DeviceVerified = 32000,
        }
        #endregion

        #region Field
        #endregion

        #region Constructor
        public ManualDeviceCarrierIdentifierReader(Nameable nameable)
            : base(nameable)
        {
        }
        public ManualDeviceCarrierIdentifierReader() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        #endregion

        #region ISupportCollectionEventConfiguration
        /// <summary>
        /// Collection 이벤트 구성 정보를 가져옵니다.
        /// </summary>
        /// <returns>Collection 이벤트 구성 정보의 컬렉션입니다.</returns>
        public virtual CollectionEventConfigurationCollection GetCollectionEventConfigurations()
        {
            CollectionEventConfigurationCollection ceids = new CollectionEventConfigurationCollection();
            CollectionEventConfiguration ceid = null;

            ceid = new CollectionEventConfiguration(CollectionEventEnum.DeviceVerified);
            ceids.Add(ceid);

            return ceids;
        }
        #endregion

        #region ISupportDataVariableConfiguration
        /// <summary>
        /// Data Variable 구성 정보를 가져옵니다.
        /// </summary>
        /// <returns>Data Variable 구성 정보의 컬렉션입니다.</returns>
        public virtual DataVariableConfigurationCollection GetDataVariableConfigurations()
        {
            DataVariableConfigurationCollection dvs = new DataVariableConfigurationCollection();
            DataVariableConfiguration dv = null;

            dv = new DataVariableConfiguration();
            dv.InternalId = EnumUtility.GetFriendlyName(DeviceEventReport.DataVariableEnum.CarrierIdentifier);
            dv.ExternalId = (int)DeviceEventReport.DataVariableEnum.CarrierIdentifier;
            dv.Name = (string)dv.InternalId;
            dv.ValueCoversionSpecification.FormatCode = SecsFormatCode.ASCII;
            dvs.Add(dv);

            dv = new DataVariableConfiguration();
            dv.InternalId = EnumUtility.GetFriendlyName(DeviceEventReport.DataVariableEnum.Device);
            dv.ExternalId = (int)DeviceEventReport.DataVariableEnum.Device;
            dv.Name = (string)dv.InternalId;
            dv.ValueCoversionSpecification.FormatCode = SecsFormatCode.ASCII;
            dvs.Add(dv);

            return dvs;
        }
        #endregion

        #region CarrierIdReader
        /// <summary>
        /// 재정의 되었습니다.
        /// </summary>
        /// <param name="carrier">캐리어 개체입니다.</param>
        /// <param name="identifier">읽은 캐리어 ID입니다.</param>
        /// <returns>성공하면 0이고, 그렇지 않으면 0이 아닌 값입니다.</returns>
        protected override int OnRead(Carrier carrier, ref string identifier)
        {
            int ret = 0;
            LoadPort loadport = null;
            string caption = "Carrier Identifier";
            ManualDeviceCarrierIdentifierReaderDialogForm form = null;
            LoadPortPlate plate = null;

            identifier = string.Empty;
            plate = carrier.GetLoadedLoadPortPlate();
            if (plate == null)
                throw new ArgumentNullException("LoadPortPlate");
            loadport = plate.Owner;
            if (loadport == null)
                throw new ArgumentNullException("LoadPort");

            caption += string.Format(" [{0}]", loadport.Name);

            if (this.ConstructConfiguration.EnableWhenUserSelected == false && loadport.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected)
            {
                // Revision 2
                if (plate.AssociationState.CurrentStateValue == LoadPortCarrierAssociationStateMachine.StateEnum.Associated)
                {
                    identifier = plate.CarrierIdentifier;
                    if(carrier is StackCarrier stackCarrier)
                    {
                        if(stackCarrier.Above == null && stackCarrier.Below != null)
                        {
                            identifier += ".Above";
                        }
                    }
                }
                else
                {
                    if ((ret = base.OnRead(carrier, ref identifier)) != 0) return ret;
                }
            }
            else
            {
                // Revision 2
                if (plate.AssociationState.CurrentStateValue == LoadPortCarrierAssociationStateMachine.StateEnum.Associated)
                {
                    identifier = plate.CarrierIdentifier;
                    if(carrier is StackCarrier stackCarrier)
                    {
                        if(stackCarrier.Above == null && stackCarrier.Below != null)
                        {
                            identifier += ".Above";
                        }
                    }
                }                  
                else
                {
                    base.OnRead(carrier, ref identifier);

                }

                form = new ManualDeviceCarrierIdentifierReaderDialogForm();

                //form.CarrierIdentifier = identifier;
                form.Device = identifier;
                form.Text = caption;
                DialogResult result = form.ShowDialog(FormManager.MdiParent);
                if (result != DialogResult.OK)
                    return ErrorManager.Register("User cancel");
                //identifier = form.CarrierIdentifier;
                
                if (string.IsNullOrEmpty(identifier) == true)
                    return ErrorManager.Register("Carrier Identifier is not valid.");

                EventReportDispatcher.AddReport(new DeviceEventReport(identifier, form.Device));
            }

            return ret;
        }
        #endregion

        #region Part
        #endregion

        #region Element
        protected new ManualDeviceCarrierIdentifierReaderConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as ManualDeviceCarrierIdentifierReaderConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new ManualDeviceCarrierIdentifierReaderConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if (this.ConstructConfiguration == null) return;
        }
        #endregion
    }
    #endregion

    #region ManualDeviceCarrierIdentifierReaderConstructConfiguration
    [Serializable]
    public class ManualDeviceCarrierIdentifierReaderConstructConfiguration : ArtificialCarrierIdentifierReaderConstructConfiguration
    {
        #region Field
        private bool m_EnableWhenUserSelected;
        #endregion

        #region Constructor
        public ManualDeviceCarrierIdentifierReaderConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public ManualDeviceCarrierIdentifierReaderConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        [Category("CarrierIdentifierReader")]
        [DefaultValue(false)]
        public bool EnableWhenUserSelected
        {
            get { return this.m_EnableWhenUserSelected; }
            set { this.m_EnableWhenUserSelected = value; }
        }
        #endregion

        #region ConstructConfiguration
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.EnableWhenUserSelected = false;
        }
        #endregion
    }
    #endregion

    #region DeviceEventReport
    /// <summary>
    /// Device 상태 변경에 대한 이벤트 보고서를 정의합니다.
    /// </summary>
    [Serializable]
    public class DeviceEventReport : EventReport
    {
        #region Define
        [Serializable]
        public new enum DataVariableEnum
        {
            CarrierIdentifier = 12000,
            Device,
        }
        #endregion

        #region Field
        private string m_CarrierIdentifier;
        private string m_Device;
        #endregion

        #region Constructor
        /// <summary>
        /// DeviceEventReport 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public DeviceEventReport(string carrierIdentifier, string device)
            : base(ManualDeviceCarrierIdentifierReader.CollectionEventEnum.DeviceVerified)
        {
            this.CarrierIdentifier = carrierIdentifier;
            this.Device = device;
        }
        public DeviceEventReport() : this("", "") { }
        #endregion

        #region Property
        [DataVariable(DataVariableEnum.CarrierIdentifier)]
        [SecsFormatCode(SecsFormatCode.ASCII)]
        public string CarrierIdentifier
        {
            get { return this.m_CarrierIdentifier; }
            set { this.m_CarrierIdentifier = value; }
        }

        [DataVariable(DataVariableEnum.Device)]
        [SecsFormatCode(SecsFormatCode.ASCII)]
        public string Device
        {
            get { return this.m_Device; }
            set { this.m_Device = value; }
        }
        #endregion

        #region EventReport
        #endregion
    }
    #endregion
}