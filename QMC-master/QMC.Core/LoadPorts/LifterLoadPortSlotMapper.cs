using System;
using System.Windows.Forms;
using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.LoadPorts.SlotMappers;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Motions;

namespace QMC.LoadPorts
{
    #region LifterLoadPortSlotMapper
    public class LifterLoadPortSlotMapper : AggregatedMotionSlotMapper
    {
        #region Define
        public new enum AlarmKeys
        {
            PlateCannotBeFound,
        }
        #endregion

        #region Field
        #endregion

        #region Constructor
        public LifterLoadPortSlotMapper(Nameable nameable)
            : base(nameable)
        {
        }
        public LifterLoadPortSlotMapper() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        #endregion

        #region MotionSlotMapper
        protected override Control OnGetElementMaintenanceControl()
        {
            return new LifterLoadPortSlotMapperMaintenanceControl(this);
        }

        protected override int OnGetBaselinePosition(Carrier carrier, ref double baseline)
        {
            int ret = 0;
            int plateIndex = -1, carrierIndex = -1;
            Carrier temp = null;
            StackCarrier stackCarrier = null;
            Xyzt xyzt;

            // get plate index
            foreach (LoadPortPlate plate in this.Owner.Plates)
            {
                temp = plate.Port.Location.GetMaterial() as Carrier;
                if (temp == carrier)
                {
                    plateIndex = this.Owner.Plates.IndexOf(plate);
                    break;
                }
                stackCarrier = temp as StackCarrier;
                if (stackCarrier != null)
                {
                    if (stackCarrier.GetCarrierIndex(carrier, ref carrierIndex) == true)
                    {
                        plateIndex = this.Owner.Plates.IndexOf(plate);
                        break;
                    }
                }
            }

            if (plateIndex < 0)
            {
                if ((ret = this.Alarms[AlarmKeys.PlateCannotBeFound].Post(this)) != 0) return ret;
            }

            //if ((ret = this.Owner.PlateTransferAssistant.PositionRepository.GetSetupPosition(LifterPlateTransferAssistant.PositionEnum.Scan, plateIndex, out setupPosition)) != 0) return ret;
            //baseline = setupPosition.Target;
            // To Do:
            // 논의 사항
            // 1. 매개 변수를 string으로 사용하는 문제
            // 2. 함수 리턴 형식이 void인 문제 (실패한 경우는?)
            this.Owner.PlateTransferAssistant.PositionRepository.GetPosition(LifterPlateTransferAssistant.Positions.Scan.ToString(), this.Owner.Plates[plateIndex], out xyzt);
            baseline = xyzt.Z;
            if (stackCarrier != null && 0 < carrierIndex)
            {
                if (this.ConstructConfiguration.ReverseMovement == true)
                    baseline -= stackCarrier.Configuration.Body.MechanicalSpecification.Height * carrierIndex;
                else
                    baseline += stackCarrier.Configuration.Body.MechanicalSpecification.Height * carrierIndex;
            }

            return ret;
        }

        protected override int OnGetReadyPosition(ref double position, Carrier carrier = null)
        {
            int ret = 0;
            Xyzt xyzt;
            if(carrier == null)
            {
                this.Owner.PlateTransferAssistant.PositionRepository.GetPosition(LifterPlateTransferAssistant.Positions.Ready.ToString(), this.Owner.Plate, out xyzt);
            }
            else
            {
                this.Owner.PlateTransferAssistant.PositionRepository.GetPosition(LifterPlateTransferAssistant.Positions.Ready.ToString(), carrier.GetLoadedLoadPortPlate(), out xyzt);
            }
            
            position = xyzt.Z;

            return ret;
        }
        #endregion

        #region Part
        #endregion

        #region Element
        public new LifterLoadPort Owner
        {
            get { return base.Owner as LifterLoadPort; }
        }

        protected new LifterLoadPortSlotMapperConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as LifterLoadPortSlotMapperConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new LifterLoadPortSlotMapperConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if (this.ConstructConfiguration == null) return;

            if (this.Owner != null && string.IsNullOrEmpty(this.ConstructConfiguration.MotionLocator))
                this.ConstructConfiguration.MotionLocator = this.Owner.Z.Locator.ToString();
        }

        protected override void OnMakeAlarms()
        {
            Alarm alarm = null;

            base.OnMakeAlarms();

            alarm = new Alarm((int)AlarmKeys.PlateCannotBeFound, AlarmKeys.PlateCannotBeFound.ToString());
            alarm.AlarmGrade = AlarmGrade.Stop;
            alarm.Cause = "The plate on which the carrier is placed cannot be found";
            this.Alarms.Add(AlarmKeys.PlateCannotBeFound, alarm);
        }
        #endregion
    }
    #endregion

    #region LifterLoadPortSlotMapperConstructConfiguration
    [Serializable]
    public class LifterLoadPortSlotMapperConstructConfiguration : AggregatedMotionSlotMapperConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public LifterLoadPortSlotMapperConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public LifterLoadPortSlotMapperConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        #endregion

        #region ConstructConfiguration
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.ReverseMovement = true;
        }
        #endregion
    }
    #endregion
}