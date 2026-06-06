using QMC.Common.IO;
using QMC.CDT320.Ajin;

namespace QMC.CDT320
{
    /// <summary>
    /// Stage 46 — Resource Sensors Unit (CDT-310 매뉴얼 사양).<br/>
    /// CDA (Compressed Dry Air) + Vacuum 라인 압력 감지 센서 통합.
    /// </summary>
    public class ResourceSensorsUnit
    {
        public BaseDigitalInput MainCda1Check    { get; private set; }
        public BaseDigitalInput MainCda2Check    { get; private set; }
        public BaseDigitalInput MainVacuum1Check { get; private set; }
        public BaseDigitalInput MainVacuum2Check { get; private set; }
        public BaseDigitalInput MainVacuum3Check { get; private set; }
        public BaseDigitalInput MainVacuum4Check { get; private set; }

        public ResourceSensorsUnit()
        {
            MainCda1Check    = AjinFactory.CreateDigitalInput("MainCda1Check");
            MainCda2Check    = AjinFactory.CreateDigitalInput("MainCda2Check");
            MainVacuum1Check = AjinFactory.CreateDigitalInput("MainVacuum1Check");
            MainVacuum2Check = AjinFactory.CreateDigitalInput("MainVacuum2Check");
            MainVacuum3Check = AjinFactory.CreateDigitalInput("MainVacuum3Check");
            MainVacuum4Check = AjinFactory.CreateDigitalInput("MainVacuum4Check");
        }

        /// <summary>모든 CDA 라인 정상 (양쪽 모두 ON).</summary>
        public bool AllCdaOk => MainCda1Check.IsOn && MainCda2Check.IsOn;

        /// <summary>모든 Vacuum 라인 정상 (4 라인 모두 ON).</summary>
        public bool AllVacuumOk =>
            MainVacuum1Check.IsOn && MainVacuum2Check.IsOn &&
            MainVacuum3Check.IsOn && MainVacuum4Check.IsOn;

        /// <summary>Resource 전체 정상 — InitAsync 전 사전 검사.</summary>
        public bool AllOk => AllCdaOk && AllVacuumOk;
    }
}
