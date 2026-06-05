using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MechaSys.SoftBricks.Windows.Win32.Win32Api;

namespace QMC.Jobs
{
    #region GeneralTransferPathFinder
    public class GeneralTransferPathFinder : BasicTransferPathFinder
	{
		#region Field
		#endregion

		#region Constructor
		public GeneralTransferPathFinder(Nameable nameable)
			: base(nameable)
		{
		}
		public GeneralTransferPathFinder() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        #endregion

        #region TransferPathFinder Members
        #endregion

        #region BasicTransferPathFinder Members       
        #endregion

        #region Part Members
        #endregion

        #region Element Members
        protected new GeneralTransferPathFinderConstructConfiguration ConstructConfiguration
		{
			get { return base.ConstructConfiguration as GeneralTransferPathFinderConstructConfiguration; }
		}

		protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
		{
			return new GeneralTransferPathFinderConstructConfiguration();
		}

		protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
		{
			base.OnSetConstructConfiguration(configuration);

			if(this.ConstructConfiguration == null) return;
		}
		#endregion
	}
	#endregion

	#region GeneralTransferPathFinderConstructConfiguration
	[Serializable]
	public class GeneralTransferPathFinderConstructConfiguration : BasicTransferPathfinderConstructConfiguration
	{
		#region Field
		#endregion

		#region Constructor
		public GeneralTransferPathFinderConstructConfiguration(ElementConstructMethod constructMethod)
			: base(constructMethod)
		{
		}
		public GeneralTransferPathFinderConstructConfiguration() : this(ElementConstructMethod.Static) { }
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