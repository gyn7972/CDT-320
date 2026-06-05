using System;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Transfer;

namespace QMC.Transfers.Feeders
{
    #region BasicFeeder
    public class BasicFeeder : Feeder
    {
        #region Field
        #endregion

        #region Constructor
        public BasicFeeder(Nameable nameable)
            : base(nameable)
        {
        }
        public BasicFeeder() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        #endregion

        #region TransferableModule Members
        protected override int OnReadyToTransfer(TransferSpecification specification)
        {
            int ret = 0;
            return ret;
        }

        protected override int OnExecuteTransfer(string id)
        {
            int ret = 0;
            FeederArm arm = null;
            ITransferred transferred = null;
            LoadPort loadPort = null;
            int secondaryPort = 0;
            IMicroTransferred microTransferred = null;

            arm = this.GetFeederArm(this.TransferSpecification.PrimaryPort);
            transferred = this.GetTransferred(this.TransferSpecification.Secondary);
            loadPort = transferred as LoadPort;
            microTransferred = MicroTransferredAgent.Search(this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort);

            if(loadPort != null)
                secondaryPort = this.TransferSpecification.SecondaryCarrierPort;
            else
                secondaryPort = this.TransferSpecification.SecondaryPort;

            if(this.TransferSpecification.Direction == TransferDirection.Send)
            {
                if(loadPort != null)
                {
                    if((ret = arm.MicroTransferableAgent.ExtendSync(this.TransferSpecification.Direction, this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.PrepareReleaseSync()) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.FinishReleaseSync()) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.RetractSync(this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                }
                else
                {
                    if((ret = microTransferred.PrepareAcquireSync(this.TransferSpecification.Primary, secondaryPort)) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.ExtendSync(this.TransferSpecification.Direction, this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.PrepareReleaseSync()) != 0) return ret;
                    if((ret = microTransferred.FinishAcquireSync(this.TransferSpecification.Primary, secondaryPort)) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.FinishReleaseSync()) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.RetractSync(this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                }
            }
            else if(this.TransferSpecification.Direction == TransferDirection.Receive)
            {
                if(arm is MotionFeederArm motionArm && motionArm.EvasiveVerticalCylinder != null)
                {
                    if((ret = motionArm.EvasiveVerticalCylinder.Down()) != 0) return ret;
                }

                if(loadPort != null)
                {                   
                    if((ret = arm.MicroTransferableAgent.ExtendSync(this.TransferSpecification.Direction, this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.PrepareAcquireSync()) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.FinishAcquireSync()) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.RetractSync(this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                }
                else
                {
                    if((ret = microTransferred.PrepareReleaseSync(this.TransferSpecification.Primary, secondaryPort)) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.ExtendSync(this.TransferSpecification.Direction, this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.PrepareAcquireSync()) != 0) return ret;
                    if((ret = microTransferred.FinishReleaseSync(this.TransferSpecification.Primary, secondaryPort)) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.FinishAcquireSync()) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.RetractSync(this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                }
            }
            else
                throw new NotSupportedException();

            return ret;
        }

        protected override int OnVerifyTransfer(string id)
        {
            int ret = 0;
            FeederArm arm = null;
            MaterialPresence presence;
            ITransferred transferred = null;
            LoadPort loadPort = null;

            arm = this.GetFeederArm(this.TransferSpecification.PrimaryPort);
            transferred = this.GetTransferred(this.TransferSpecification.Secondary);
            loadPort = transferred as LoadPort;

            if(this.TransferSpecification.Direction == TransferDirection.Send)
            {
                presence = MaterialPresence.NotExist;
                if((ret = arm.MicroTransferableAgent.VerifyReleaseSync(ref presence)) != 0) return ret;
                if(presence != MaterialPresence.NotExist)
                {
                    if((ret = this.Alarms[TransferModule.AlarmKeys.MaterialDoesExistAfterSend].Post(this)) != 0) return ret;
                }
                if(loadPort == null)
                {
                    if(arm is MotionFeederArm motionArm && motionArm.EvasiveVerticalCylinder != null)
                    {
                        if((ret = motionArm.EvasiveVerticalCylinder.Up()) != 0) return ret;
                    }
                }
            }
            else if(this.TransferSpecification.Direction == TransferDirection.Receive)
            {
                presence = MaterialPresence.Exist;
                if((ret = arm.MicroTransferableAgent.VerifyAcquireSync(ref presence)) != 0) return ret;
                if(presence != MaterialPresence.Exist)
                {
                    if((ret = this.Alarms[TransferModule.AlarmKeys.MaterialDoesNotExistAfterReceive].Post(this)) != 0) return ret;
                }
            }
            else
                throw new NotSupportedException();

            return ret;
        }

        protected override int OnHaltTransfer(string id)
        {
            int ret = 0;
            return ret;
        }

        protected override int OnCancelReadyToTransfer(string id)
        {
            int ret = 0;
            return ret;
        }

        #endregion

        #region Part Members
        #endregion

        #region Element Members
        protected new BasicFeederConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as BasicFeederConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new BasicFeederConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }      
        #endregion
    }
    #endregion

    #region BasicFeederConstructConfiguration
    [Serializable]
    public class BasicFeederConstructConfiguration : FeederConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public BasicFeederConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public BasicFeederConstructConfiguration() : this(ElementConstructMethod.Static) { }
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
