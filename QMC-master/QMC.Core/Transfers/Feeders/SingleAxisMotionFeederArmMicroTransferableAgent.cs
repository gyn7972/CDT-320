using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Transfer;

namespace QMC.Transfers.Feeders
{
    #region SingleAxisMotionFeederArmMicroTransferableAgent
    public class SingleAxisMotionFeederArmMicroTransferableAgent : MotionFeederArmMicroTransferableAgent
    {
        #region Field
        #endregion

        #region Constructor
        public SingleAxisMotionFeederArmMicroTransferableAgent(Nameable nameable)
            : base(nameable)
        {
        }
        public SingleAxisMotionFeederArmMicroTransferableAgent() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        #endregion

        #region MicroTransferableAgent Members
        protected override int OnExtend(TransferDirection transferDirection, string secondary, int secondaryPort)
        {
            int ret = 0;
            Xyzt position = new Xyzt();
            if((ret = base.OnExtend(transferDirection, secondary, secondaryPort)) != 0) return ret;
            this.Owner.PositionRepository.GetPosition(MotionFeederArm.Positions.Extend.ToString(), this.Owner.EndEffector.Ports.IndexOf(this.Owner.EndEffector.Port), secondary, secondaryPort, out position);
            if((ret = this.Owner.TravelMotion.MoveSync(position.Y)) != 0) return ret;
            return ret;
        }

        protected override int OnRetract(string secondary, int secondaryPort)
        {
            int ret = 0;
            Xyzt position = new Xyzt();
            if((ret = base.OnRetract(secondary, secondaryPort)) != 0) return ret;
            this.Owner.PositionRepository.GetPosition(MotionFeederArm.Positions.Retract.ToString(), this.Owner.EndEffector.Ports.IndexOf(this.Owner.EndEffector.Port), secondary, secondaryPort, out position);
            if((ret = this.Owner.TravelMotion.MoveSync(position.Y)) != 0) return ret;
            return ret;
        }
        #endregion

        #region Part Members
        public new SingleAxisMotionFeederArm Owner
        {
            get { return base.Owner as SingleAxisMotionFeederArm; }
        }
        #endregion

        #region Element Members
        protected new SingleAxisMotionFeederArmMicroTransferableAgentConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as SingleAxisMotionFeederArmMicroTransferableAgentConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new SingleAxisMotionFeederArmMicroTransferableAgentConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }
        #endregion
    }
    #endregion

    #region SingleAxisMotionFeederArmMicroTransferableAgentConstructConfiguration
    [Serializable]
    public class SingleAxisMotionFeederArmMicroTransferableAgentConstructConfiguration : MotionFeederArmMicroTransferableAgentConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public SingleAxisMotionFeederArmMicroTransferableAgentConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public SingleAxisMotionFeederArmMicroTransferableAgentConstructConfiguration() : this(ElementConstructMethod.Static) { }
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
}
