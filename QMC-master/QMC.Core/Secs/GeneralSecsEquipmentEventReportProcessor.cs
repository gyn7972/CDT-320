using System;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Secs;

using QMC.Equipments;

namespace QMC.Secs
{
    #region GeneralSecsEquipmentEventReportProcessor
    public class GeneralSecsEquipmentEventReportProcessor : SecsEquipmentEventReportProcessor
    {
		#region Field
		#endregion

		#region Constructor
		public GeneralSecsEquipmentEventReportProcessor(Nameable nameable)
			: base(nameable)
		{
			this.SecsService = new GeneralGemService(new Nameable(nameof(this.SecsService)));
		}
		public GeneralSecsEquipmentEventReportProcessor() : this(new Nameable()) { }
		#endregion

		#region Property
		#endregion

		#region Method
		#endregion

		#region Element
		public new GeneralSemiEquipment Owner
		{
			get { return base.Owner as GeneralSemiEquipment; }
		}

		protected new GeneralSecsEquipmentEventReportProcessorConstructConfiguration ConstructConfiguration
		{
			get { return base.ConstructConfiguration as GeneralSecsEquipmentEventReportProcessorConstructConfiguration; }
		}

		protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
		{
			return new GeneralSecsEquipmentEventReportProcessorConstructConfiguration();
		}

		protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
		{
			base.OnSetConstructConfiguration(configuration);

			if (this.ConstructConfiguration == null) return;
		}
		#endregion
	}
	#endregion

	#region GeneralSecsEquipmentEventReportProcessorConstructConfiguration
	[Serializable]
	public class GeneralSecsEquipmentEventReportProcessorConstructConfiguration : SecsEquipmentEventReportProcessorConstructConfiguration
    {
		#region Field
		#endregion

		#region Constructor
		public GeneralSecsEquipmentEventReportProcessorConstructConfiguration(ElementConstructMethod constructMethod)
			: base(constructMethod)
		{
		}
		public GeneralSecsEquipmentEventReportProcessorConstructConfiguration() : this(ElementConstructMethod.Static) { }
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