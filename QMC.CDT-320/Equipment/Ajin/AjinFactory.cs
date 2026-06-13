using QMC.Common;
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
        private static readonly object IoGate = new object();
        private static readonly Dictionary<string, BaseDigitalInput> SharedInputs =
            new Dictionary<string, BaseDigitalInput>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, BaseDigitalOutput> SharedOutputs =
            new Dictionary<string, BaseDigitalOutput>(StringComparer.OrdinalIgnoreCase);
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

                // motion_axes.json 에 저장된 사용자 편집 값(Setup/Config)을 등록된 축에 덮어쓴다.
                // 축 이름이 T0/T1/Z0/Z1/Y0/Y1 처럼 숫자 suffix 가 붙은 경우에도
                // ResolveName 으로 정규화해 기존 키와 일치시킨다.
                ApplyPersistedAxisValues();
            }
        }

        public static void ApplyPersistedAxisValues(IEnumerable<BaseAxis> axes)
        {
            try
            {
                MotionAxisStore store = MotionAxisStore.LoadOrCreate(MotionAxisStore.DefaultPath);
                if (store == null || store.Items == null || store.Items.Count == 0) return;
                bool forceRealAxisInDryRun = IsApplicationDryRunMode();

                foreach (BaseAxis axis in axes ?? Enumerable.Empty<BaseAxis>())
                {
                    if (axis == null) continue;
                    string axisKey = ResolveAxisKey(axis.Name);

                    MotionAxisDefinition saved = null;
                    for (int i = 0; i < store.Items.Count; i++)
                    {
                        MotionAxisDefinition item = store.Items[i];
                        if (item == null) continue;
                        string itemKey = ResolveAxisKey(item.Name);
                        if (string.Equals(itemKey, axisKey, StringComparison.OrdinalIgnoreCase))
                        {
                            saved = item;
                            break;
                        }
                    }
                    if (saved == null) continue;

                    // 보드/채널/축번호/유닛명 같은 식별 정보는 AjinConfigStore 가 우선이므로
                    // motion_axes.json 의 동일 필드는 무시하고, 사용자 편집 값만 덮어쓴다.
                    if (saved.Setup != null && axis.Setup != null)
                    {
                        AxisSetup s = axis.Setup;
                        AxisSetup src = saved.Setup;
                        s.PulsesPerUnit = src.PulsesPerUnit > 0 ? src.PulsesPerUnit : s.PulsesPerUnit;
                        s.AxisScale = src.AxisScale > 0 ? src.AxisScale : s.AxisScale;
                        s.Unit = AjinAxisDefaults.IsThetaAxis(axis.Name)
                            ? AxisUnitConverter.Degree
                            : AxisUnitConverter.Normalize(src.Unit);
                        s.SoftLimitPlus = src.SoftLimitPlus;
                        s.SoftLimitMinus = src.SoftLimitMinus;
                        s.SoftLimitEnabled = src.SoftLimitEnabled;
                        s.HomeOffset = src.HomeOffset;
                        s.HomeDirection = src.HomeDirection;
                        s.HomeSignal = src.HomeSignal;
                        s.HomeTimeoutMs = src.HomeTimeoutMs;
                        s.MoveTimeoutMs = src.MoveTimeoutMs;
                        s.Stroke = src.Stroke;
                        s.Brake = src.Brake;
                    }
                    if (saved.Config != null && axis.Config != null)
                    {
                        AxisConfig c = axis.Config;
                        AxisConfig src = saved.Config;
                        c.IsSimulationMode = (!Ready || axis is SimAxis)
                            ? true
                            : forceRealAxisInDryRun ? false : src.IsSimulationMode;
                        if (src.DefaultVelocity > 0) c.DefaultVelocity = src.DefaultVelocity;
                        if (src.MaxVelocity > 0) c.MaxVelocity = src.MaxVelocity;
                        if (src.Acceleration > 0) c.Acceleration = src.Acceleration;
                        if (src.Deceleration > 0) c.Deceleration = src.Deceleration;
                        if (src.HomeFirstVelocity > 0) c.HomeFirstVelocity = src.HomeFirstVelocity;
                        if (src.HomeSecondVelocity > 0) c.HomeSecondVelocity = src.HomeSecondVelocity;
                        if (src.HomeThirdVelocity > 0) c.HomeThirdVelocity = src.HomeThirdVelocity;
                        if (src.HomeLastVelocity > 0) c.HomeLastVelocity = src.HomeLastVelocity;
                        if (src.HomeVelocity > 0) c.HomeVelocity = src.HomeVelocity;
                        if (src.HomeFirstAcceleration > 0) c.HomeFirstAcceleration = src.HomeFirstAcceleration;
                        if (src.HomeFirstDeceleration > 0) c.HomeFirstDeceleration = src.HomeFirstDeceleration;
                        if (src.HomeSecondAcceleration > 0) c.HomeSecondAcceleration = src.HomeSecondAcceleration;
                        if (src.HomeSecondDeceleration > 0) c.HomeSecondDeceleration = src.HomeSecondDeceleration;
                        if (src.JogCoarseVelocity > 0) c.JogCoarseVelocity = src.JogCoarseVelocity;
                        if (src.JogFineVelocity > 0) c.JogFineVelocity = src.JogFineVelocity;
                        if (src.JogAcceleration > 0) c.JogAcceleration = src.JogAcceleration;
                        if (src.JogDeceleration > 0) c.JogDeceleration = src.JogDeceleration;
                        if (src.InPositionTolerance >= 0) c.InPositionTolerance = src.InPositionTolerance;
                    }
                }
            }
            catch { }
        }

        private static void ApplyPersistedAxisValues()
        {
            ApplyPersistedAxisValues(AxisManager.GetAll());
        }

        private static bool IsApplicationDryRunMode()
        {
            try
            {
                QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
                if (settings == null)
                    settings = QMC.CDT320.AppSettingsStore.Load();

                return settings != null &&
                       settings.DryRunMode &&
                       !settings.SimulationMode &&
                       settings.UseAjin &&
                       Ready;
            }
            catch
            {
                return false;
            }
        }

        public static void ReloadConfiguredAxes()
        {
            lock (AxisGate)
            {
                _configuredAxesRegistered = false;
                AxisManager.Clear();
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
                axis.Setup.Unit = AjinAxisDefaults.IsThetaAxis(definition.Name)
                    ? AxisUnitConverter.Degree
                    : AxisUnitConverter.Normalize(definition.Setup.Unit);
                axis.Setup.IsEnabled = definition.Setup.IsEnabled;
                axis.Setup.SoftLimitPlus = definition.Setup.SoftLimitPlus;
                axis.Setup.SoftLimitMinus = definition.Setup.SoftLimitMinus;
                axis.Setup.SoftLimitEnabled = definition.Setup.SoftLimitEnabled;
                axis.Setup.HomeOffset = definition.Setup.HomeOffset;
                axis.Setup.HomeDirection = definition.Setup.HomeDirection;
                axis.Setup.HomeSignal = definition.Setup.HomeSignal;
                axis.Setup.HomeTimeoutMs = definition.Setup.HomeTimeoutMs;
                axis.Setup.MoveTimeoutMs = definition.Setup.MoveTimeoutMs;
                axis.Setup.Stroke = definition.Setup.Stroke;
                axis.Setup.Brake = definition.Setup.Brake;
            }

            if (definition.Config != null)
            {
                axis.Config.IsSimulationMode = definition.Config.IsSimulationMode;
                axis.Config.DefaultVelocity = definition.Config.DefaultVelocity;
                axis.Config.MaxVelocity = definition.Config.MaxVelocity;
                axis.Config.Acceleration = definition.Config.Acceleration;
                axis.Config.Deceleration = definition.Config.Deceleration;
                axis.Config.HomeFirstVelocity = definition.Config.HomeFirstVelocity;
                axis.Config.HomeSecondVelocity = definition.Config.HomeSecondVelocity;
                axis.Config.HomeThirdVelocity = definition.Config.HomeThirdVelocity;
                axis.Config.HomeLastVelocity = definition.Config.HomeLastVelocity;
                axis.Config.HomeVelocity = definition.Config.HomeVelocity;
                axis.Config.HomeFirstAcceleration = definition.Config.HomeFirstAcceleration;
                axis.Config.HomeFirstDeceleration = definition.Config.HomeFirstDeceleration;
                axis.Config.HomeSecondAcceleration = definition.Config.HomeSecondAcceleration;
                axis.Config.HomeSecondDeceleration = definition.Config.HomeSecondDeceleration;
                axis.Config.JogCoarseVelocity = definition.Config.JogCoarseVelocity;
                axis.Config.JogFineVelocity = definition.Config.JogFineVelocity;
                axis.Config.JogAcceleration = definition.Config.JogAcceleration;
                axis.Config.JogDeceleration = definition.Config.JogDeceleration;
            }

            if (definition.Recipe != null)
            {
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
            var config = AxisSpeedTable.BuildConfig(axisNo);
            if (axisDefault != null)
                config.DefaultVelocity = axisDefault.DefaultVel;

            config.IsSimulationMode = !Ready || axisNo < 0;

            return new MotionAxisDefinition
            {
                Name = key ?? string.Empty,
                Setup = new AxisSetup
                {
                    UnitName = axisDefault != null ? axisDefault.Module : DeriveUnitName(key),
                    DisplayName = axisDefault != null ? axisDefault.AxisName : key ?? string.Empty,
                    AxisNo = axisNo,
                    BoardNo = boardNo,
                    Unit = AjinAxisDefaults.IsThetaAxis(key)
                        ? AxisUnitConverter.Degree
                        : axisDefault != null ? AxisUnitConverter.Normalize(axisDefault.Unit) : AxisUnitConverter.Millimeter,
                    IsEnabled = true,
                    Stroke = axisDefault != null ? axisDefault.Stroke : 0.0,
                    Brake = axisDefault != null && axisDefault.Brake,
                    SoftLimitMinus = 0,
                    SoftLimitPlus = axisDefault != null ? axisDefault.Stroke : 200.0,
                    HomeDirection = axisDefault != null && string.Equals(axisDefault.HomeDir, "POS", StringComparison.OrdinalIgnoreCase)
                        ? HomeDirection.Cw
                        : HomeDirection.Ccw
                },
                Config = config
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
            bool simMode = IoSettingsStore.InputSimulation(name, !Ready);
            return CreateSharedDigitalInput(name, m, simMode);
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
            bool simMode = IoSettingsStore.OutputSimulation(name, !Ready);
            return CreateSharedDigitalOutput(name, m, simMode);
        }

        internal static BaseDigitalInput CreateSharedDigitalInput(string name, DioMap map, bool simulationMode)
        {
            if (map == null)
                return ConfigureSimInput(new SimDigitalInput(string.IsNullOrWhiteSpace(name) ? "UnregisteredInput" : name), 0, 0, false);

            string key = IoKey(false, map.Module, map.Bit, map.Nc);
            lock (IoGate)
            {
                BaseDigitalInput input;
                if (SharedInputs.TryGetValue(key, out input) && input != null)
                {
                    input.Config.IsSimulationMode = input.Config.IsSimulationMode || simulationMode || !Ready || input is SimDigitalInput;
                    return input;
                }

                input = Ready
                    ? (BaseDigitalInput)new AjinDigitalInput(name, map.Module, map.Bit, map.Nc)
                    : new SimDigitalInput(name);
                ConfigureSimInput(input, map.Module, map.Bit, map.Nc);
                input.Config.IsSimulationMode = simulationMode || !Ready || input is SimDigitalInput;
                SharedInputs[key] = input;
                return input;
            }
        }

        internal static BaseDigitalOutput CreateSharedDigitalOutput(string name, DioMap map, bool simulationMode)
        {
            if (map == null)
                return ConfigureSimOutput(new SimDigitalOutput(string.IsNullOrWhiteSpace(name) ? "UnregisteredOutput" : name), 0, 0, false);

            string key = IoKey(true, map.Module, map.Bit, map.Nc);
            lock (IoGate)
            {
                BaseDigitalOutput output;
                if (SharedOutputs.TryGetValue(key, out output) && output != null)
                {
                    output.Config.IsSimulationMode = output.Config.IsSimulationMode || simulationMode || !Ready || output is SimDigitalOutput;
                    return output;
                }

                output = Ready
                    ? (BaseDigitalOutput)new AjinDigitalOutput(name, map.Module, map.Bit, map.Nc)
                    : new SimDigitalOutput(name);
                ConfigureSimOutput(output, map.Module, map.Bit, map.Nc);
                output.Config.IsSimulationMode = simulationMode || !Ready || output is SimDigitalOutput;
                SharedOutputs[key] = output;
                return output;
            }
        }

        internal static BaseDigitalInput CreateCylinderDigitalInput(string name, DioMap map, bool simulationMode)
        {
            if (map == null)
                return ConfigureSimInput(new SimDigitalInput(string.IsNullOrWhiteSpace(name) ? "UnregisteredInput" : name), -1, -1, false);

            BaseDigitalInput input = Ready
                ? (BaseDigitalInput)new AjinDigitalInput(name, map.Module, map.Bit, map.Nc)
                : new SimDigitalInput(name);

            input.Setup.ModuleNo = map.Module;
            input.Setup.BitNo = map.Bit;
            input.Setup.IsNormallyClosed = map.Nc;
            input.Config.IsSimulationMode = simulationMode || !Ready || input is SimDigitalInput;
            return input;
        }

        internal static BaseDigitalOutput CreateCylinderDigitalOutput(string name, DioMap map, bool simulationMode)
        {
            if (map == null)
                return ConfigureSimOutput(new SimDigitalOutput(string.IsNullOrWhiteSpace(name) ? "UnregisteredOutput" : name), -1, -1, false);

            BaseDigitalOutput output = Ready
                ? (BaseDigitalOutput)new AjinDigitalOutput(name, map.Module, map.Bit, map.Nc)
                : new SimDigitalOutput(name);

            output.Setup.ModuleNo = map.Module;
            output.Setup.BitNo = map.Bit;
            output.Setup.IsNormallyClosed = map.Nc;
            output.Config.IsSimulationMode = simulationMode || !Ready || output is SimDigitalOutput;
            return output;
        }

        private static string IoKey(bool output, int module, int bit, bool nc)
        {
            return (output ? "DO" : "DI") + ":" + module + ":" + bit + ":" + (nc ? "NC" : "NO");
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
                    BaseCylinder mapped = new AjinCylinder(name,
                        cy.OutFwd,
                        cy.OutBwd,
                        cy.UseFwdInput ? cy.InFwd : null,
                        cy.UseBwdInput ? cy.InBwd : null,
                        cy.SingleSolenoid || singleSolenoid);
                    CylinderSettingsStore.Apply(mapped);
                    return mapped;
                }

                BaseCylinder fallback = new AjinCylinder(name,
                    (catalog.OutFwd.Module, catalog.OutFwd.Bit),
                    (catalog.OutBwd.Module, catalog.OutBwd.Bit),
                    (catalog.InFwd.Module, catalog.InFwd.Bit),
                    (catalog.InBwd.Module, catalog.InBwd.Bit),
                    catalog.SingleSolenoid || singleSolenoid);
                CylinderSettingsStore.Apply(fallback);
                return fallback;
            }
            BaseCylinder sim = new SimCylinder(name);
            CylinderSettingsStore.Apply(sim);
            return sim;
        }

        public static void ApplyInputSimulation(BaseDigitalInput input, bool simulationMode)
        {
            if (input == null) return;
            input.Config.IsSimulationMode = simulationMode || !Ready || input is SimDigitalInput;
            input.Config.IgnoreWaits = false;
        }

        public static void ApplyInputDryRun(BaseDigitalInput input, bool dryRun)
        {
            if (input == null) return;
            input.Config.IsSimulationMode = dryRun || !Ready || input is SimDigitalInput;
            input.Config.IgnoreWaits = dryRun && Ready && !(input is SimDigitalInput);
        }

        public static void ApplyInputPersistedSimulation(BaseDigitalInput input)
        {
            if (input == null) return;
            bool simulationMode = IoSettingsStore.InputSimulation(input.Name, !Ready);
            ApplyInputSimulation(input, simulationMode);
        }

        public static void ApplyOutputSimulation(BaseDigitalOutput output, bool simulationMode)
        {
            if (output == null) return;
            output.Config.IsSimulationMode = simulationMode || !Ready || output is SimDigitalOutput;
        }

        public static void ApplyOutputPersistedSimulation(BaseDigitalOutput output)
        {
            if (output == null) return;
            bool simulationMode = IoSettingsStore.OutputSimulation(output.Name, !Ready);
            ApplyOutputSimulation(output, simulationMode);
        }

        public static void ApplyCylinderSimulation(BaseCylinder cylinder, bool simulationMode)
        {
            if (cylinder == null) return;

            bool sim = simulationMode || !Ready || cylinder is SimCylinder;
            cylinder.Config.IsSimulationMode = sim;
            cylinder.Config.IgnoreInputWaits = false;

            ApplyOutputSimulation(cylinder.OutFwd, sim);
            ApplyOutputSimulation(cylinder.OutBwd, sim);
            ApplyInputSimulation(cylinder.InFwd, sim);
            ApplyInputSimulation(cylinder.InBwd, sim);
        }

        public static void ApplyCylinderDryRun(BaseCylinder cylinder, bool dryRun)
        {
            if (cylinder == null || cylinder.Config == null) return;

            bool forceSimulation = !Ready || cylinder is SimCylinder;
            if (forceSimulation)
            {
                ApplyCylinderSimulation(cylinder, true);
                return;
            }

            cylinder.Config.IsSimulationMode = false;
            cylinder.Config.IgnoreInputWaits = dryRun;

            ApplyOutputSimulation(cylinder.OutFwd, false);
            ApplyOutputSimulation(cylinder.OutBwd, false);
            ApplyInputDryRun(cylinder.InFwd, dryRun);
            ApplyInputDryRun(cylinder.InBwd, dryRun);
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
