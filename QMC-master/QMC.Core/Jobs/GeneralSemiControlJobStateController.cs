using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Jobs
{
    #region GeneralSemiControlJobStateController
    public class GeneralSemiControlJobStateController : SemiControlJobStateController
    {
        #region Define
        #endregion

        #region Field
        #endregion

        #region Constructor
        public GeneralSemiControlJobStateController(Nameable nameable)
            : base(nameable)
        {
        }
        public GeneralSemiControlJobStateController() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        #endregion

        #region ControlJobStateController Members
        #endregion

        #region Element Members
        protected new GeneralSemiControlJobStateControllerConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as GeneralSemiControlJobStateControllerConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new GeneralSemiControlJobStateControllerConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }
        #endregion
    }
    #endregion

    #region GeneralControlJobStateControllerConstructConfiguration
    [Serializable]
    public class GeneralSemiControlJobStateControllerConstructConfiguration : SemiControlJobStateControllerConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralSemiControlJobStateControllerConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public GeneralSemiControlJobStateControllerConstructConfiguration() : this(ElementConstructMethod.Static) { }
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
