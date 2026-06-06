using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Recipes;
using MechaSys.SoftBricks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QMC.Equipments;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.DotNetUtility;
using MechaSys.SoftBricks.Transfer;
using static MechaSys.SoftBricks.Transfer.BasicTransferPathFinder;

namespace QMC.Jobs
{
    #region GeneralControlJobOrderGenerator
    public class GeneralControlJobOrderGenerator : SemiControlJobOrderGenerator
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralControlJobOrderGenerator(Nameable nameable)
            : base(nameable)
        {
        }
        public GeneralControlJobOrderGenerator() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        #endregion

        #region ControlJobOrderGenerator Members
        #endregion

        #region Element Members
        public new GeneralSemiEquipment Owner
        {
            get { return base.Owner as GeneralSemiEquipment; }
        }

        protected new GeneralControlJobOrderGeneratorConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as GeneralControlJobOrderGeneratorConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new GeneralControlJobOrderGeneratorConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }
        #endregion
    }
    #endregion

    #region GeneralControlJobOrderGeneratorConstructConfiguration
    [Serializable]
    public class GeneralControlJobOrderGeneratorConstructConfiguration : SemiControlJobOrderGeneratorConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralControlJobOrderGeneratorConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public GeneralControlJobOrderGeneratorConstructConfiguration() : this(ElementConstructMethod.Static) { }
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
