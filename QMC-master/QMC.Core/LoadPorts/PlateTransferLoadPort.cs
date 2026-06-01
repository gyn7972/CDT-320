using System;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Transfer;

namespace QMC.LoadPorts
{
    #region PlateTransferLoadPort
    /// <summary>
    /// Plate를 이동할 수 있는 LoadPort를 정의한다.
    /// </summary>
    public abstract class PlateTransferLoadPort : MultiPlateLoadPort
    {
        #region Field
        private PlateTransferAssistant m_PlateTransferAssistant;
        #endregion

        #region Constructor
        public PlateTransferLoadPort(Nameable nameable)
            : base(nameable)
        {
        }
        public PlateTransferLoadPort() : this(new Nameable()) { }
        #endregion

        #region Property
        [Composite(ElementKind.Element)]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.OneOnly)]
        public PlateTransferAssistant PlateTransferAssistant
        {
            get { return this.m_PlateTransferAssistant; }
            protected set
            {
                this.Elements.SetNotNull(this.m_PlateTransferAssistant, value);
                this.m_PlateTransferAssistant = value;
            }
        }
        #endregion

        #region Method
        #endregion

        #region TransferredModule
        protected override int OnReadyToTransfer(TransferSpecification specification)
        {
            int ret = 0;

            if ((ret = base.OnReadyToTransfer(specification)) != 0) return ret;

            if ((ret = this.PlateTransferAssistant.MoveToTransfer(specification.Direction, specification.SecondaryPort, specification.CarrierIndex, specification.SecondaryCarrierPort)) != 0) return ret;

            return ret;
        }
        #endregion

        #region Element
        protected new PlateTransferLoadPortConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as PlateTransferLoadPortConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new PlateTransferLoadPortConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if (this.ConstructConfiguration == null) return;
        }
        #endregion
    }
    #endregion

    #region PlateTransferLoadPortConstructConfiguration
    [Serializable]
    public class PlateTransferLoadPortConstructConfiguration : MultiPlateLoadPortConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public PlateTransferLoadPortConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public PlateTransferLoadPortConstructConfiguration() : this(ElementConstructMethod.Static) { }
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
}