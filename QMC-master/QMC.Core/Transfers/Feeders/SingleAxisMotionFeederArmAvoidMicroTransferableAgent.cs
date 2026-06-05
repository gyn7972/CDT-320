using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;

namespace QMC.Transfers.Feeders
{
    #region SingleAxisMotionFeederArmAvoidMicroTransferableAgent
    public class SingleAxisMotionFeederArmAvoidMicroTransferableAgent : SingleAxisMotionFeederArmMicroTransferableAgent,
		INeedAvoidDuringTransferAgenet
	{
		#region Field
		#endregion

		#region Constructor
		public SingleAxisMotionFeederArmAvoidMicroTransferableAgent(Nameable nameable)
			: base(nameable)
		{
		}
		public SingleAxisMotionFeederArmAvoidMicroTransferableAgent() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        #endregion

        #region Element Members
        protected new SingleAxisMotionFeederArmAvoidMicroTransferableAgentConstructConfiguration ConstructConfiguration
		{
			get { return base.ConstructConfiguration as SingleAxisMotionFeederArmAvoidMicroTransferableAgentConstructConfiguration; }
		}

		protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
		{
			return new SingleAxisMotionFeederArmAvoidMicroTransferableAgentConstructConfiguration();
		}

		protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
		{
			base.OnSetConstructConfiguration(configuration);

			if(this.ConstructConfiguration == null) return;
		}      
        #endregion
    }
	#endregion

	#region SingleAxisMotionFeederArmAvoidMicroTransferableAgentConstructConfiguration
	[Serializable]
	public class SingleAxisMotionFeederArmAvoidMicroTransferableAgentConstructConfiguration : SingleAxisMotionFeederArmMicroTransferableAgentConstructConfiguration
	{
		#region Field
		#endregion

		#region Constructor
		public SingleAxisMotionFeederArmAvoidMicroTransferableAgentConstructConfiguration(ElementConstructMethod constructMethod)
			: base(constructMethod)
		{
		}
		public SingleAxisMotionFeederArmAvoidMicroTransferableAgentConstructConfiguration() : this(ElementConstructMethod.Static) { }
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
