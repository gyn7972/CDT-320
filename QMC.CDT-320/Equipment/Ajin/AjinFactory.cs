using QMC.Common.IO;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static MotionAxisManager AxisManager { get; } = new MotionAxisManager();

        private static bool Ready => UseRealBoard && AjinSystem.IsOpen;
        public static bool IsRealBoardReady => Ready;
        private static AjinConfig Cfg => AjinConfigStore.Current;
        private static readonly object AxisGate = new object();
        private static bool _configuredAxesRegistered;

        static AjinFactory()
        {
            AxisManager.AxisFactory = CreateManagedAxis;
        }

        public static void RegisterConfiguredAxes()
        {
            lock (AxisGate)
            {
                if (_configuredAxesRegistered) 
                    return;

                _configuredAxesRegistered = true;

                if (Cfg == null || Cfg.Axes == null) 
                    return;

                foreach (var item in Cfg.Axes.OrderBy(x => x.Value != null ? x.Value.Axis : int.MaxValue))
                {
                    if (item.Value == null) continue;
                    if (!string.Equals(item.Key, ResolveAxisKey(item.Key), StringComparison.OrdinalIgnoreCase))
                        continue;
                    AxisManager.Upsert(CreateDefinition(item.Key, item.Value));
                }
            }
        }

        public static void ReloadConfiguredAxes()
        {
            lock (AxisGate)
            {
                _configuredAxesRegistered = false;
            }

            RegisterConfiguredAxes();
        }

        // ──────────────────────────────────────
        //  Axis
        // ──────────────────────────────────────

        /// <summary>컨피그에서 이름으로 조회해 축 생성. 명시 axisNo 를 주면 컨피그 무시.</summary>
        public static BaseAxis CreateAxis(string name, int? axisNo = null)
        {
            RegisterConfiguredAxes();

            string axisKey = ResolveAxisKey(name);
            if (!axisNo.HasValue)
            {
                BaseAxis registered = AxisManager.Get(axisKey);
                if (registered != null) return registered;
            }

            AxisMap map = null;
            if (!axisNo.HasValue && Cfg != null && Cfg.Axes != null)
                Cfg.Axes.TryGetValue(axisKey, out map);

            if (map == null)
                map = new AxisMap { Axis = axisNo.HasValue ? axisNo.Value : -1 };

            MotionAxisDefinition definition = CreateDefinition(axisKey, map);
            definition.Config.IsSimulationMode = !Ready || map.Axis < 0;
            return AxisManager.Upsert(definition);
        }

        private static BaseAxis CreateManagedAxis(MotionAxisDefinition definition)
        {
            int axisNo = definition != null && definition.Setup != null ? definition.Setup.AxisNo : -1;
            bool useRealAxis = Ready && axisNo >= 0;

            BaseAxis axis = useRealAxis
                ? (BaseAxis)new AjinAxis(definition.Name, axisNo)
                : new SimAxis(definition.Name);

            ApplyDefinition(axis, definition);
            return axis;
        }

        private static void ApplyDefinition(BaseAxis axis, MotionAxisDefinition definition)
        {
            if (axis == null || definition == null) return;

            if (definition.Setup != null)
            {
                axis.Setup.UnitName = definition.Setup.UnitName;
                axis.Setup.DisplayName = definition.Setup.DisplayName;
                axis.Setup.BoardNo = definition.Setup.BoardNo;
                axis.Setup.AxisNo = definition.Setup.AxisNo;
                axis.Setup.PulsesPerUnit = definition.Setup.PulsesPerUnit;
                axis.Setup.AxisScale = definition.Setup.AxisScale;
                axis.Setup.Unit = definition.Setup.Unit;
                axis.Setup.IsEnabled = definition.Setup.IsEnabled;
                axis.Setup.SoftLimitPlus = definition.Setup.SoftLimitPlus;
                axis.Setup.SoftLimitMinus = definition.Setup.SoftLimitMinus;
                axis.Setup.SoftLimitEnabled = definition.Setup.SoftLimitEnabled;
                axis.Setup.HomeOffset = definition.Setup.HomeOffset;
                axis.Setup.HomeDirection = definition.Setup.HomeDirection;
                axis.Setup.HomeSignal = definition.Setup.HomeSignal;
                axis.Setup.HomeTimeoutMs = definition.Setup.HomeTimeoutMs;
                axis.Setup.MoveTimeoutMs = definition.Setup.MoveTimeoutMs;
            }

            if (definition.Config != null)
                axis.Config.IsSimulationMode = definition.Config.IsSimulationMode;

            if (definition.Recipe != null)
            {
                axis.Recipe.DefaultVelocity = definition.Recipe.DefaultVelocity;
                axis.Recipe.Acceleration = definition.Recipe.Acceleration;
                axis.Recipe.Deceleration = definition.Recipe.Deceleration;
                axis.Recipe.HomeFirstVelocity = definition.Recipe.HomeFirstVelocity;
                axis.Recipe.HomeSecondVelocity = definition.Recipe.HomeSecondVelocity;
                axis.Recipe.HomeThirdVelocity = definition.Recipe.HomeThirdVelocity;
                axis.Recipe.HomeLastVelocity = definition.Recipe.HomeLastVelocity;
                axis.Recipe.HomeVelocity = definition.Recipe.HomeVelocity;
                axis.Recipe.JogCoarseVelocity = definition.Recipe.JogCoarseVelocity;
                axis.Recipe.JogFineVelocity = definition.Recipe.JogFineVelocity;
                axis.Recipe.JogAcceleration = definition.Recipe.JogAcceleration;
                axis.Recipe.JogDeceleration = definition.Recipe.JogDeceleration;
            }
        }

        private static string ResolveAxisKey(string name)
        {
            return AjinAxisDefaults.ResolveName(name);
        }

        private static MotionAxisDefinition CreateDefinition(string key, AxisMap map)
        {
            int axisNo = map != null ? map.Axis : -1;
            int boardNo = map != null ? map.BoardNo : 0;
            AxisDefault axisDefault = FindDefault(key);
            var recipe = AxisSpeedTable.BuildRecipe(axisNo);
            if (axisDefault != null)
                recipe.DefaultVelocity = axisDefault.DefaultVel;

            return new MotionAxisDefinition
            {
                Name = key ?? string.Empty,
                Setup = new AxisSetup
                {
                    UnitName = axisDefault != null ? axisDefault.Module : DeriveUnitName(key),
                    DisplayName = axisDefault != null ? axisDefault.AxisName : key ?? string.Empty,
                    AxisNo = axisNo,
                    BoardNo = boardNo,
                    Unit = axisDefault != null ? axisDefault.Unit : "mm",
                    IsEnabled = true
                },
                Config = new AxisConfig
                {
                    IsSimulationMode = !Ready || axisNo < 0
                },
                Recipe = recipe
            };
        }

        private static AxisDefault FindDefault(string key)
        {
            string resolved = AjinAxisDefaults.ResolveName(key);
            foreach (AxisDefault axis in AjinAxisDefaults.All)
                if (string.Equals(axis.AxisName, resolved, StringComparison.OrdinalIgnoreCase))
                    return axis;
            return null;
        }

        private static string DeriveUnitName(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            int idx = key.IndexOf('_');
            return idx > 0 ? key.Substring(0, idx) : "CDT-320";
        }

        // ──────────────────────────────────────
        //  Digital IO
        // ──────────────────────────────────────

        [Obsolete("Use AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.xxx). I/O must be registered only in AjinIoCatalog.", true)]
        public static BaseDigitalInput CreateDigitalInput(string name, int? module = null, int? bit = null, bool nc = false)
        {
            DioDefault catalog = AjinIoCatalog.FindInput(name);
            if (catalog == null)
                return new SimDigitalInput(name);

            return CreateDigitalInput(catalog);
        }

        public static BaseDigitalInput CreateDigitalInput(DioDefault catalog)
        {
            if (catalog == null)
                return ConfigureSimInput(new SimDigitalInput("UnregisteredInput"), 0, 0, false);

            string name = catalog.Name;
            DioMap m = ResolveInputMap(catalog);
            if (Ready)
            {
                return new AjinDigitalInput(name, m.Module, m.Bit, m.Nc);
            }

            return ConfigureSimInput(new SimDigitalInput(name), m.Module, m.Bit, m.Nc);
        }

        [Obsolete("Use AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.xxx). I/O must be registered only in AjinIoCatalog.", true)]
        public static BaseDigitalOutput CreateDigitalOutput(string name, int? module = null, int? bit = null, bool nc = false)
        {
            DioDefault catalog = AjinIoCatalog.FindOutput(name);
            if (catalog == null)
                return new SimDigitalOutput(name);

            return CreateDigitalOutput(catalog);
        }

        public static BaseDigitalOutput CreateDigitalOutput(DioDefault catalog)
        {
            if (catalog == null)
                return ConfigureSimOutput(new SimDigitalOutput("UnregisteredOutput"), 0, 0, false);

            string name = catalog.Name;
            DioMap m = ResolveOutputMap(catalog);
            if (Ready)
            {
                return new AjinDigitalOutput(name, m.Module, m.Bit, m.Nc);
            }

            return ConfigureSimOutput(new SimDigitalOutput(name), m.Module, m.Bit, m.Nc);
        }

        private static DioMap ResolveInputMap(DioDefault catalog)
        {
            DioMap m;
            if (Cfg != null && Cfg.DigitalInputs != null &&
                Cfg.DigitalInputs.TryGetValue(catalog.Name, out m) && m != null)
                return m;

            return new DioMap { No = catalog.No, Address = catalog.Address, Module = catalog.Module, Bit = catalog.Bit, Nc = catalog.Nc };
        }

        private static DioMap ResolveOutputMap(DioDefault catalog)
        {
            DioMap m;
            if (Cfg != null && Cfg.DigitalOutputs != null &&
                Cfg.DigitalOutputs.TryGetValue(catalog.Name, out m) && m != null)
                return m;

            return new DioMap { No = catalog.No, Address = catalog.Address, Module = catalog.Module, Bit = catalog.Bit, Nc = catalog.Nc };
        }

        private static BaseDigitalInput ConfigureSimInput(BaseDigitalInput port, int module, int bit, bool nc)
        {
            port.Setup.ModuleNo = module;
            port.Setup.BitNo = bit;
            port.Setup.IsNormallyClosed = nc;
            port.Config.IsSimulationMode = true;
            return port;
        }

        private static BaseDigitalOutput ConfigureSimOutput(BaseDigitalOutput port, int module, int bit, bool nc)
        {
            port.Setup.ModuleNo = module;
            port.Setup.BitNo = bit;
            port.Setup.IsNormallyClosed = nc;
            port.Config.IsSimulationMode = true;
            return port;
        }

        // ──────────────────────────────────────
        //  Cylinder
        // ──────────────────────────────────────

        /// <summary>
        /// Cylinder — 컨피그의 cylinders 항목 우선. 없으면 <c>{name}_OutFwd/OutBwd/InFwd/InBwd</c>
        /// 를 DIO 맵에서 조회. 그래도 없으면 Sim.
        /// </summary>
        [Obsolete("Use AjinFactory.CreateCylinder(AjinIoCatalog.CylinderRefs.xxx). I/O must be registered only in AjinIoCatalog.", true)]
        public static BaseCylinder CreateCylinder(string name, bool singleSolenoid = false)
        {
            CylinderDefault catalog = AjinIoCatalog.FindCylinder(name);
            if (catalog == null)
                return new SimCylinder(name);

            return CreateCylinder(catalog, singleSolenoid);
        }

        public static BaseCylinder CreateCylinder(CylinderDefault catalog, bool singleSolenoid = false)
        {
            if (catalog == null)
                return new SimCylinder("UnregisteredCylinder");

            string name = catalog.Name;
            if (Ready)
            {
                CylMap cy;
                if (Cfg.Cylinders.TryGetValue(name, out cy))
                {
                    return new AjinCylinder(name,
                        (cy.OutFwd.Module, cy.OutFwd.Bit),
                        (cy.OutBwd.Module, cy.OutBwd.Bit),
                        (cy.InFwd .Module, cy.InFwd .Bit),
                        (cy.InBwd .Module, cy.InBwd .Bit),
                        cy.SingleSolenoid || singleSolenoid);
                }

                return new AjinCylinder(name,
                    (catalog.OutFwd.Module, catalog.OutFwd.Bit),
                    (catalog.OutBwd.Module, catalog.OutBwd.Bit),
                    (catalog.InFwd.Module, catalog.InFwd.Bit),
                    (catalog.InBwd.Module, catalog.InBwd.Bit),
                    catalog.SingleSolenoid || singleSolenoid);
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
