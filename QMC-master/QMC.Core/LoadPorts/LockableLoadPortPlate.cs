using System;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks.LoadPorts;

namespace QMC.LoadPorts
{
    #region LockableLoadPortPlate
    public class LockableLoadPortPlate : AutoFeedingLoadPortPlate
    {
        #region Field
        private ILocker m_Locker;
        #endregion

        #region Constructor
        public LockableLoadPortPlate(Nameable nameable)
			: base(nameable)
		{
			this.Locker = new Locker(new Nameable(nameof(this.Locker)));
		}
		public LockableLoadPortPlate() : this(new Nameable()) { }
		#endregion

		#region Property
		[Composite]
		[Multiplicity(MultiplicityAttribute.AllowableNumbers.OneOnly)]
		public ILocker Locker
		{
			get { return this.m_Locker; }
			protected set
			{
				this.Parts.SetNotNull(this.m_Locker, value);
				this.m_Locker = value;
			}
		}
        #endregion

        #region Method
        #endregion

        #region Part
        protected override int OnInitialize()
        {
			int ret = 0;

            if((ret = this.Locker.InitializeSync()) != 0) return ret;

            if((ret = base.OnInitialize()) != 0) return ret;

            return ret;
        }
        #endregion

        #region Element
        protected new LockableLoadPortPlateConstructConfiguration ConstructConfiguration
		{
			get { return base.ConstructConfiguration as LockableLoadPortPlateConstructConfiguration; }
		}

		protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
		{
			return new LockableLoadPortPlateConstructConfiguration();
		}

		protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
		{
			base.OnSetConstructConfiguration(configuration);

			if (this.ConstructConfiguration == null) return;
		}
		#endregion
	}
	#endregion

	#region LockableLoadPortPlateConstructConfiguration
	[Serializable]
	public class LockableLoadPortPlateConstructConfiguration : AutoFeedingLoadPortPlateConstructConfiguration
    {
		#region Field
		#endregion

		#region Constructor
		public LockableLoadPortPlateConstructConfiguration(ElementConstructMethod constructMethod)
			: base(constructMethod)
		{
		}
		public LockableLoadPortPlateConstructConfiguration() : this(ElementConstructMethod.Static) { }
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