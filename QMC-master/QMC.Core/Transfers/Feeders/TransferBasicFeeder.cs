using System;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Transfer;

using QMC.LoadPorts;

namespace QMC.Transfers.Feeders
{
    #region TransferBasicFeeder
    public class TransferBasicFeeder : BasicFeeder
    {
        #region Field
        #endregion

        #region Constructor
        public TransferBasicFeeder(Nameable nameable)
            : base(nameable)
        {
        }
        public TransferBasicFeeder() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        private bool CanMoveDirectToLoadPort(MotionFeederArm arm, RelatedTransferSpecification specification)
        {
            int ret = 0;
            VerticalCylinderState state = VerticalCylinderState.Up;
            if(specification.Direction == TransferDirection.Receive)
            {
                if(arm.EvasiveVerticalCylinder == null) return false;
                if((ret = arm.EvasiveVerticalCylinder.Check(ref state)) != 0) return false;
                if(state == VerticalCylinderState.Down) return true;
            }
            return false;
        }
        #endregion

        #region TransferableModule Members
        protected override int OnExecuteTransfer(string id)
        {
            int ret = 0;
            FeederArm arm = null;
            ITransferred transferred = null;
            ITransferred relatedTransferred = null;
            LoadPort loadPort = null;
            RelatedTransferSpecification specification = null;
            IMicroTransferred microTransferred = null;
            IMicroTransferred relatedMicroTransferred = null;

            specification = this.TransferSpecification as RelatedTransferSpecification;
            if(specification == null)
                throw new NullReferenceException("Related transfer specification");

            arm = this.GetFeederArm(this.TransferSpecification.PrimaryPort);
            transferred = this.GetTransferred(this.TransferSpecification.Secondary);
            relatedTransferred = this.GetTransferred(specification.RelatedTransferred);

            loadPort = transferred as LoadPort;
            microTransferred = MicroTransferredAgent.Search(this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort);
            relatedMicroTransferred = MicroTransferredAgent.Search(specification.RelatedTransferred, specification.RelatedTransferredPort);

            if(arm is MotionFeederArm motionFeederArm)
            {
                if(this.CanMoveDirectToLoadPort(motionFeederArm, specification) == false)
                {
                    if(arm.MicroTransferableAgent is INeedAvoidDuringTransferAgenet)
                    {
                        if((ret = arm.MicroTransferableAgent.RetractSync(specification.RelatedTransferred, specification.RelatedTransferredPort)) != 0) return ret;
                    }

                    if(motionFeederArm.EvasiveVerticalCylinder != null)
                    {
                        if((ret = motionFeederArm.EvasiveVerticalCylinder.Down()) != 0) return ret;
                    }
                }
            }

            if(this.TransferSpecification.Direction == TransferDirection.Send)
            {
                if((ret = arm.MicroTransferableAgent.PrepareAcquireSync()) != 0) return ret;
                if((ret = arm.MicroTransferableAgent.ExtendSync(this.TransferSpecification.Direction, specification.RelatedTransferred, specification.RelatedTransferredPort)) != 0) return ret;
                if((ret = arm.MicroTransferableAgent.FinishAcquireSync()) != 0) return ret;
                if((ret = relatedMicroTransferred.PrepareReleaseSync(this.TransferSpecification.Primary, specification.RelatedTransferredPort)) != 0) return ret;
                if((ret = relatedMicroTransferred.FinishReleaseSync(this.TransferSpecification.Primary, specification.RelatedTransferredPort)) != 0) return ret;

                if((ret = arm.MicroTransferableAgent.RetractSync(this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                if((ret = arm.MicroTransferableAgent.ExtendSync(this.TransferSpecification.Direction, this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                if((ret = arm.MicroTransferableAgent.PrepareReleaseSync()) != 0) return ret;
                if((ret = arm.MicroTransferableAgent.FinishReleaseSync()) != 0) return ret;
                if(loadPort is PlateTransferLoadPort plateTransferLoadPort)
                {
                    MotionInterlock[] interlocks = null;
                    try
                    {                      
                        if(plateTransferLoadPort.PlateTransferAssistant is LifterPlateTransferAssistant assistant)
                        {
                            interlocks = assistant.Owner.Z.Elements.GetByType<MotionInterlock>();
                            for(int i = 0; i < interlocks.Length; i++)
                            {
                                interlocks[i].Enabled = false;
                            }
                            if((ret = assistant.MoveDistanceLowerDistance(this.TransferSpecification.SecondaryPort)) != 0) return ret;
                        }
                    }
                    finally
                    {
                        if(interlocks != null)
                        {
                            for(int i = 0; i < interlocks.Length; i++)
                            {
                                interlocks[i].Enabled = true;
                            }
                        }
                    }
                    
                }
                if((ret = arm.MicroTransferableAgent.RetractSync(this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
            }
            else if(this.TransferSpecification.Direction == TransferDirection.Receive)
            {
                //To do : 미리 해놓지 않으면 tape frame gripper와 충돌
                if((ret = relatedMicroTransferred.PrepareAcquireSync(this.TransferSpecification.Primary, specification.RelatedTransferredPort)) != 0) return ret;

                if((ret = arm.MicroTransferableAgent.PrepareAcquireSync()) != 0) return ret;
                if((ret = arm.MicroTransferableAgent.ExtendSync(this.TransferSpecification.Direction, this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                if((ret = arm.MicroTransferableAgent.FinishAcquireSync()) != 0) return ret;
                if((ret = arm.MicroTransferableAgent.RetractSync(this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;

                if(arm is SingleAxisMotionFeederArm singleAxisMotionFeederArm && singleAxisMotionFeederArm.Configuration.Body.EnableAlign == true)
                {
                    if((ret = singleAxisMotionFeederArm.EndEffector.Gripper.Release()) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.ExtendSync(this.TransferSpecification.Direction, this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                    if((ret = singleAxisMotionFeederArm.EndEffector.Gripper.Hold()) != 0) return ret;
                    if((ret = arm.MicroTransferableAgent.RetractSync(this.TransferSpecification.Secondary, this.TransferSpecification.SecondaryPort)) != 0) return ret;
                }

                if((ret = relatedMicroTransferred.PrepareAcquireSync(this.TransferSpecification.Primary, specification.RelatedTransferredPort)) != 0) return ret;
                if((ret = arm.MicroTransferableAgent.ExtendSync(this.TransferSpecification.Direction, specification.RelatedTransferred, specification.RelatedTransferredPort)) != 0) return ret;
                if((ret = arm.MicroTransferableAgent.PrepareReleaseSync()) != 0) return ret;
                if((ret = arm.MicroTransferableAgent.FinishReleaseSync()) != 0) return ret;              
                if((ret = arm.MicroTransferableAgent.RetractSync(specification.RelatedTransferred, specification.RelatedTransferredPort)) != 0) return ret;
                if((ret = relatedMicroTransferred.FinishAcquireSync(this.TransferSpecification.Primary, specification.RelatedTransferredPort)) != 0) return ret;

                if(arm is MotionFeederArm motionArm && motionArm.EvasiveVerticalCylinder != null)
                {
                    if((ret = motionArm.EvasiveVerticalCylinder.Up()) != 0) return ret;
                }

                if(arm.MicroTransferableAgent is INeedAvoidDuringTransferAgenet)
                {
                    //To do :
                    if(arm is SingleAxisMotionFeederArm feederArm)
                    {
                        if((ret = feederArm.TravelMotion.MoveToHomeSync()) != 0) return ret;
                    }
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
            }
            else if(this.TransferSpecification.Direction == TransferDirection.Receive)
            {
            }
            else
                throw new NotSupportedException();

            return ret;
        }
        #endregion

        #region Part Members
        #endregion

        #region Element Members
        protected new TransferBasicFeederConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as TransferBasicFeederConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new TransferBasicFeederConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            this.ElementFormProvider.FormTypes.AddForcefully(ElementFormProvider.FormEnum.Maintenance, typeof(TransferBasicFeederMaintenanceForm));
        }
        #endregion
    }
    #endregion

    #region TransferBasicFeederConstructConfiguration
    [Serializable]
    public class TransferBasicFeederConstructConfiguration : BasicFeederConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public TransferBasicFeederConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public TransferBasicFeederConstructConfiguration() : this(ElementConstructMethod.Static) { }
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
