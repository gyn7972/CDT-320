using System;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks.LoadPorts;

namespace QMC.Jobs
{
    #region GeneralControlJobActivator
    public class GeneralControlJobActivator : ControlJobActivator
	{
		#region Field
		#endregion

		#region Constructor
		public GeneralControlJobActivator(Nameable nameable)
			: base(nameable)
		{
		}
		public GeneralControlJobActivator() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        #endregion

        #region ControlJobActivator
        #endregion

        #region Element
        protected new GeneralControlJobActivatorConstructConfiguration ConstructConfiguration
		{
			get { return base.ConstructConfiguration as GeneralControlJobActivatorConstructConfiguration; }
		}

		protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
		{
			return new GeneralControlJobActivatorConstructConfiguration();
		}

		protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
		{
			base.OnSetConstructConfiguration(configuration);

			if (this.ConstructConfiguration == null) return;
		}
		#endregion
	}
	#endregion

	#region GeneralControlJobActivatorConstructConfiguration
	[Serializable]
	public class GeneralControlJobActivatorConstructConfiguration : ControlJobActivatorConstructConfiguration
	{
		#region Field
		#endregion

		#region Constructor
		public GeneralControlJobActivatorConstructConfiguration(ElementConstructMethod constructMethod)
			: base(constructMethod)
		{
		}
		public GeneralControlJobActivatorConstructConfiguration() : this(ElementConstructMethod.Static) { }
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