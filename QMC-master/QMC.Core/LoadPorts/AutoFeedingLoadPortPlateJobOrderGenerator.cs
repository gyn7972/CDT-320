using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Configurations.Controls;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Transfer;

namespace QMC.LoadPorts
{
    #region AutoFeedingLoadPortPlateJobOrderGenerator
    public abstract class AutoFeedingLoadPortPlateJobOrderGenerator : OperationalElement
	{
		#region Field
		#endregion

		#region Constructor
		public AutoFeedingLoadPortPlateJobOrderGenerator(Nameable nameable)
			: base(nameable)
		{
		}
		public AutoFeedingLoadPortPlateJobOrderGenerator() : this(new Nameable()) { }
		#endregion

		#region Property
		#endregion

		#region Method
		public int MakeJobOrder()
		{
			int ret = 0;

			if (this.CanMakeJobOrder() == false) return ret;

            this.WriteLog(LogLevel.Highest, "MakeJobOrder()");

            if ((ret = this.OnMakeJobOrder()) != 0) return ret;

			return ret;
		}

		protected abstract bool CanMakeJobOrder();

		protected abstract int OnMakeJobOrder();
		#endregion

		#region Element
		public new AutoFeedingLoadPortPlate Owner
		{
			get { return base.Owner as AutoFeedingLoadPortPlate; }
		}

		protected new AutoFeedingLoadPortPlateJobOrderGeneratorConstructConfiguration ConstructConfiguration
		{
			get { return base.ConstructConfiguration as AutoFeedingLoadPortPlateJobOrderGeneratorConstructConfiguration; }
		}

		protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
		{
			return new AutoFeedingLoadPortPlateJobOrderGeneratorConstructConfiguration();
		}

		protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
		{
			base.OnSetConstructConfiguration(configuration);

			if (this.ConstructConfiguration == null) return;
		}
		#endregion
	}
	#endregion

	#region AutoFeedingLoadPortPlateJobOrderGeneratorConstructConfiguration
	[Serializable]
	public class AutoFeedingLoadPortPlateJobOrderGeneratorConstructConfiguration : OperationalElementConstructConfiguration
	{
		#region Field
		#endregion

		#region Constructor
		public AutoFeedingLoadPortPlateJobOrderGeneratorConstructConfiguration(ElementConstructMethod constructMethod)
			: base(ElementKind.Element, constructMethod)
		{
		}
		public AutoFeedingLoadPortPlateJobOrderGeneratorConstructConfiguration() : this(ElementConstructMethod.Static) { }
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