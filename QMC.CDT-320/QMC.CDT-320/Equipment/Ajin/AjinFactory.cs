using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320.Ajin
{
    /// <summary>
    /// Sim/Ajin 스위칭 팩토리. CDT-320 Unit 들이 이 팩토리를 통해
    /// BaseAxis/BaseDigitalInput/BaseDigitalOutput/BaseCylinder 를 얻도록 한다.
    /// <para>
    /// <see cref="UseRealBoard"/>=true + <see cref="AjinSystem.IsOpen"/>=true + 컨피그에 매핑 존재 일 때만 Ajin*.
    /// 그 외 모든 경우 Sim* 으로 안전 fallback.
    /// </para>
    /// </summary>
    public static class AjinFactory
    {
        public static bool UseRealBoard { get; set; } = false;

        private static bool Ready => UseRealBoard && AjinSystem.IsOpen;
        private static AjinConfig Cfg => AjinConfigStore.Current;

        // ──────────────────────────────────────
        //  Axis
        // ──────────────────────────────────────

        /// <summary>컨피그에서 이름으로 조회해 축 생성. 명시 axisNo 를 주면 컨피그 무시.</summary>
        public static BaseAxis CreateAxis(string name, int? axisNo = null)
        {
            if (Ready)
            {
                int? no = axisNo;
                if (!no.HasValue && Cfg.Axes.TryGetValue(name, out var m)) no = m.Axis;
                if (no.HasValue) return new AjinAxis(name, no.Value);
            }
            return new SimAxis(name);
        }

        // ──────────────────────────────────────
        //  Digital IO
        // ──────────────────────────────────────

        public static BaseDigitalInput CreateDigitalInput(string name, int? module = null, int? bit = null, bool nc = false)
        {
            if (Ready)
            {
                if (module.HasValue && bit.HasValue)
                    return new AjinDigitalInput(name, module.Value, bit.Value, nc);
                if (Cfg.DigitalInputs.TryGetValue(name, out var m))
                    return new AjinDigitalInput(name, m.Module, m.Bit, m.Nc);
            }
            return new SimDigitalInput(name);
        }

        public static BaseDigitalOutput CreateDigitalOutput(string name, int? module = null, int? bit = null, bool nc = false)
        {
            if (Ready)
            {
                if (module.HasValue && bit.HasValue)
                    return new AjinDigitalOutput(name, module.Value, bit.Value, nc);
                if (Cfg.DigitalOutputs.TryGetValue(name, out var m))
                    return new AjinDigitalOutput(name, m.Module, m.Bit, m.Nc);
            }
            return new SimDigitalOutput(name);
        }

        // ──────────────────────────────────────
        //  Cylinder
        // ──────────────────────────────────────

        /// <summary>
        /// Cylinder — 컨피그의 cylinders 항목 우선. 없으면 <c>{name}_OutFwd/OutBwd/InFwd/InBwd</c>
        /// 를 DIO 맵에서 조회. 그래도 없으면 Sim.
        /// </summary>
        public static BaseCylinder CreateCylinder(string name, bool singleSolenoid = false)
        {
            if (Ready)
            {
                if (Cfg.Cylinders.TryGetValue(name, out var cy))
                {
                    return new AjinCylinder(name,
                        (cy.OutFwd.Module, cy.OutFwd.Bit),
                        (cy.OutBwd.Module, cy.OutBwd.Bit),
                        (cy.InFwd .Module, cy.InFwd .Bit),
                        (cy.InBwd .Module, cy.InBwd .Bit),
                        cy.SingleSolenoid || singleSolenoid);
                }
                if (TryFindDio(name + "_OutFwd", out var oF) &&
                    TryFindDio(name + "_OutBwd", out var oB) &&
                    TryFindDio(name + "_InFwd",  out var iF) &&
                    TryFindDio(name + "_InBwd",  out var iB))
                {
                    return new AjinCylinder(name,
                        (oF.Module, oF.Bit), (oB.Module, oB.Bit),
                        (iF.Module, iF.Bit), (iB.Module, iB.Bit),
                        singleSolenoid);
                }
            }
            return new SimCylinder(name);
        }

        private static bool TryFindDio(string name, out DioMap m)
        {
            if (Cfg.DigitalOutputs.TryGetValue(name, out m)) return true;
            if (Cfg.DigitalInputs .TryGetValue(name, out m)) return true;
            m = null;
            return false;
        }
    }
}
