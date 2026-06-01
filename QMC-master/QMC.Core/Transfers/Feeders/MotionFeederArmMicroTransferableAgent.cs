using System;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Transfer;

namespace QMC.Transfers.Feeders
{
    #region MotionFeederArmMicroTransferableAgent
    public class MotionFeederArmMicroTransferableAgent : MicroTransferableAgent
    {
        #region Field
        #endregion

        #region Constructor
        public MotionFeederArmMicroTransferableAgent(Nameable nameable)
            : base(nameable)
        {
        }
        public MotionFeederArmMicroTransferableAgent() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        #endregion

        #region MicroTransferableAgent Members
        protected override int OnExtend(TransferDirection transferDirection, string secondary, int secondaryPort)
        {
            int ret = 0;
            return ret;
        }

        protected override int OnRetract(string secondary, int secondaryPort)
        {
            int ret = 0;
            return ret;
        }

        protected override int OnPrepareAcquire()
        {
            int ret = 0;

            if(this.Owner.EndEffector.Gripper is VacuumGripper)
            {
                if((ret = ((VacuumGripper)this.Owner.EndEffector.Gripper).Control(GripperState.Hold)) != 0) return ret;
            }
            else
            {
                if((ret = this.Owner.EndEffector.Gripper.Release()) != 0) return ret;
            }

            return ret;
        }

        protected override int OnFinishAcquire()
        {
            int ret = 0;

            if(this.Owner.EndEffector.Gripper is VacuumGripper == false)
            {
                this.Owner.EndEffector.Gripper.Hold();
            }

            return ret;
        }

        protected override int OnVerifyAcquire(ref MaterialPresence presence)
        {
            int ret = 0;

            if((ret = this.Owner.EndEffector.MaterialDetector.Read(ref presence)) != 0) return ret;

            return ret;
        }

        protected override int OnPrepareRelease()
        {
            int ret = 0;

            if(this.Owner.EndEffector.Gripper is VacuumGripper)
            {
                if((ret = ((VacuumGripper)this.Owner.EndEffector.Gripper).Control(GripperState.Release)) != 0) return ret;
            }
            else
            {
                if((ret = this.Owner.EndEffector.Gripper.Release()) != 0) return ret;
            }

            return ret;
        }

        protected override int OnFinishRelease()
        {
            int ret = 0;

            if(this.Owner.EndEffector.Gripper is VacuumGripper)
            {
                if((ret = ((VacuumGripper)this.Owner.EndEffector.Gripper).Control(GripperState.Hold)) != 0) return ret;
            }

            return ret;
        }

        protected override int OnVerifyRelease(ref MaterialPresence presence)
        {
            int ret = 0;

            if((ret = this.Owner.EndEffector.MaterialDetector.Read(ref presence)) != 0) return ret;

            if(this.Owner.EndEffector.Gripper is VacuumGripper)
            {
                if(presence == MaterialPresence.NotExist)
                {
                    if((ret = ((VacuumGripper)this.Owner.EndEffector.Gripper).Control(GripperState.Release)) != 0) return ret;
                }
            }

            return ret;
        }
        #endregion

        #region Element Members
        public new MotionFeederArm Owner
        {
            get { return base.Owner as MotionFeederArm; }
        }

        protected new MotionFeederArmMicroTransferableAgentConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as MotionFeederArmMicroTransferableAgentConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new MotionFeederArmMicroTransferableAgentConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }
        #endregion
    }
    #endregion

    #region MotionFeederArmMicroTransferableAgentConstructConfiguration
    [Serializable]
    public class MotionFeederArmMicroTransferableAgentConstructConfiguration : MicroTransferableAgentConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public MotionFeederArmMicroTransferableAgentConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public MotionFeederArmMicroTransferableAgentConstructConfiguration() : this(ElementConstructMethod.Static) { }
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
