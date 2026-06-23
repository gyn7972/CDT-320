using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using QMC.Common.Data.Store;

namespace QMC.Common.Motion
{
    /// <summary>
    /// JSON 파일에 저장되는 축 1개 분량의 설정 묶음.
    /// </summary>
    [DataContract]
    public class MotionAxisDefinition
    {
        /// <summary>축 이름. <see cref="BaseAxis.Name"/> 과 같은 값으로 사용한다.</summary>
        [DataMember(Order = 0)]
        public string Name { get; set; }

        /// <summary>축의 기구/배선 설정.</summary>
        [DataMember(Order = 1)]
        public AxisSetup Setup { get; set; }

        /// <summary>축의 고정 사양/보드 신호 설정.</summary>
        [DataMember(Order = 2)]
        public AxisConfig Config { get; set; }

        /// <summary>축의 운전 파라미터.</summary>
        [DataMember(Order = 3)]
        public AxisRecipe Recipe { get; set; }

        /// <summary>기본 생성자.</summary>
        public MotionAxisDefinition()
        {
            Name = string.Empty;
            Setup = new AxisSetup();
            Config = new AxisConfig();
            Recipe = new AxisRecipe();
        }

        /// <summary>지정한 이름으로 기본 축 정의를 만든다.</summary>
        public static MotionAxisDefinition CreateDefault(string name, int axisNo)
        {
            return new MotionAxisDefinition
            {
                Name = name ?? string.Empty,
                Setup = new AxisSetup
                {
                    AxisNo = axisNo,
                    DisplayName = name ?? string.Empty
                },
                Config = new AxisConfig(),
                Recipe = new AxisRecipe()
            };
        }
    }

    /// <summary>
    /// 여러 축 정의를 담는 JSON 저장소.
    /// </summary>
    [DataContract]
    public class MotionAxisStore
    {
        /// <summary>축 정의 목록.</summary>
        [DataMember(Order = 0)]
        public List<MotionAxisDefinition> Items { get; set; }

        /// <summary>기본 생성자.</summary>
        public MotionAxisStore()
        {
            Items = new List<MotionAxisDefinition>();
        }

        /// <summary>기본 저장 경로.</summary>
        public static string DefaultPath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "motion_axes.json");
            }
        }

        /// <summary>파일에서 축 저장소를 읽는다. 파일이 없으면 빈 저장소를 생성 후 저장한다.</summary>
        public static MotionAxisStore LoadOrCreate(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                filePath = DefaultPath;

            if (!File.Exists(filePath))
            {
                MotionAxisStore created = new MotionAxisStore();
                created.Save(filePath);
                return created;
            }

            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(MotionAxisStore));
                    MotionAxisStore store = ser.ReadObject(fs) as MotionAxisStore;
                    if (store == null)
                        store = new MotionAxisStore();
                    if (store.Items == null)
                        store.Items = new List<MotionAxisDefinition>();

                    store.Normalize();
                    return store;
                }
            }
            catch
            {
                return new MotionAxisStore();
            }
        }

        /// <summary>축 저장소를 UTF-8 JSON 파일로 저장한다.</summary>
        public void Save(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                filePath = DefaultPath;

            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            Normalize();

            using (FileStream fs = File.Create(filePath))
            {
                JsonPrettySerializer.WriteObject(fs, typeof(MotionAxisStore), this);
            }
        }

        /// <summary>축 이름으로 정의를 찾는다.</summary>
        public MotionAxisDefinition Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < Items.Count; i++)
            {
                MotionAxisDefinition item = Items[i];
                if (item != null && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            return null;
        }

        /// <summary>축 정의를 추가하거나 같은 이름의 기존 정의를 교체한다.</summary>
        public void Upsert(MotionAxisDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(definition.Name)) throw new ArgumentException("축 이름이 비어 있습니다.");

            NormalizeDefinition(definition);

            for (int i = 0; i < Items.Count; i++)
            {
                if (string.Equals(Items[i].Name, definition.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Items[i] = definition;
                    return;
                }
            }
            Items.Add(definition);
        }

        /// <summary>누락된 하위 객체와 기본값을 보강한다.</summary>
        public void Normalize()
        {
            if (Items == null)
                Items = new List<MotionAxisDefinition>();

            for (int i = 0; i < Items.Count; i++)
                NormalizeDefinition(Items[i]);
        }

        private static void NormalizeDefinition(MotionAxisDefinition definition)
        {
            if (definition == null) return;
            if (definition.Setup == null) definition.Setup = new AxisSetup();
            if (definition.Config == null) definition.Config = new AxisConfig();
            if (definition.Recipe == null) definition.Recipe = new AxisRecipe();

            if (string.IsNullOrWhiteSpace(definition.Name))
                definition.Name = definition.Setup.DisplayName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(definition.Setup.DisplayName))
                definition.Setup.DisplayName = definition.Name;
            if (definition.Setup.PulsesPerUnit <= 0)
                definition.Setup.PulsesPerUnit = 1000.0;
            definition.Setup.Unit = AxisUnitConverter.Normalize(definition.Setup.Unit);
            if (definition.Setup.AxisScale <= 0)
                definition.Setup.AxisScale = 1000;
            if (definition.Setup.AccJerkPercent < 0 || definition.Setup.AccJerkPercent > 100)
                definition.Setup.AccJerkPercent = 50;
            if (definition.Setup.DecJerkPercent < 0 || definition.Setup.DecJerkPercent > 100)
                definition.Setup.DecJerkPercent = 50;
            if (definition.Config.InPositionTolerance < 0)
                definition.Config.InPositionTolerance = 0;
        }
    }

    /// <summary>
    /// 축 데이터 복사 유틸리티.
    /// </summary>
    internal static class AxisDataMapper
    {
        public static void Apply(MotionAxisDefinition definition, BaseAxis axis)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (axis == null) throw new ArgumentNullException(nameof(axis));

            Copy(definition.Setup, axis.Setup);
            Copy(definition.Config, axis.Config);
            Copy(definition.Recipe, axis.Recipe);
        }

        public static MotionAxisDefinition FromAxis(BaseAxis axis)
        {
            if (axis == null) throw new ArgumentNullException(nameof(axis));

            MotionAxisDefinition definition = new MotionAxisDefinition();
            definition.Name = axis.Name;
            Copy(axis.Setup, definition.Setup);
            Copy(axis.Config, definition.Config);
            Copy(axis.Recipe, definition.Recipe);
            return definition;
        }

        public static void Copy(AxisSetup source, AxisSetup target)
        {
            if (source == null || target == null) return;
            target.UnitName = source.UnitName;
            target.DisplayName = source.DisplayName;
            target.BoardNo = source.BoardNo;
            target.AxisNo = source.AxisNo;
            target.PulsesPerUnit = source.PulsesPerUnit;
            target.AxisScale = source.AxisScale;
            target.Unit = source.Unit;
            target.IsEnabled = source.IsEnabled;
            target.SoftLimitPlus = source.SoftLimitPlus;
            target.SoftLimitMinus = source.SoftLimitMinus;
            target.SoftLimitEnabled = source.SoftLimitEnabled;
            target.HomeOffset = source.HomeOffset;
            target.HomeDirection = source.HomeDirection;
            target.HomeSignal = source.HomeSignal;
            target.HomeTimeoutMs = source.HomeTimeoutMs;
            target.MoveTimeoutMs = source.MoveTimeoutMs;
            target.Stroke = source.Stroke;
            target.Brake = source.Brake;
        }

        public static void Copy(AxisConfig source, AxisConfig target)
        {
            if (source == null || target == null) return;
            target.IsSimulationMode = source.IsSimulationMode;
            target.SimulationSpeedScale = source.SimulationSpeedScale;
            target.DefaultVelocity = source.DefaultVelocity;
            target.MaxVelocity = source.MaxVelocity;
            target.Acceleration = source.Acceleration;
            target.Deceleration = source.Deceleration;
            target.HomeFirstVelocity = source.HomeFirstVelocity;
            target.HomeSecondVelocity = source.HomeSecondVelocity;
            target.HomeThirdVelocity = source.HomeThirdVelocity;
            target.HomeLastVelocity = source.HomeLastVelocity;
            target.HomeVelocity = source.HomeVelocity;
            target.HomeFirstAcceleration = source.HomeFirstAcceleration;
            target.HomeFirstDeceleration = source.HomeFirstDeceleration;
            target.HomeSecondAcceleration = source.HomeSecondAcceleration;
            target.HomeSecondDeceleration = source.HomeSecondDeceleration;
            target.JogCoarseVelocity = source.JogCoarseVelocity;
            target.JogFineVelocity = source.JogFineVelocity;
            target.JogAcceleration = source.JogAcceleration;
            target.JogDeceleration = source.JogDeceleration;
            target.JogStopDeceleration = source.JogStopDeceleration;
            target.StopDeceleration = source.StopDeceleration;
            target.InPositionTolerance = source.InPositionTolerance;
        }

        public static void Copy(AxisRecipe source, AxisRecipe target)
        {
            if (source == null || target == null) 
                return;
        }
    }
}
