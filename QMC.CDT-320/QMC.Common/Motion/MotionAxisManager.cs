using System;
using System.Collections.Generic;

namespace QMC.Common.Motion
{
    /// <summary>
    /// 여러 모션 축을 등록, 조회, 저장, 로드하는 매니저.
    /// <list type="bullet">
    ///   <item><description>축은 <c>UnitName + AxisName</c> 복합키로 관리한다.</description></item>
    ///   <item><description>JSON 파일의 축 정의를 읽어 <see cref="AjinAxis"/> 로 생성한다.</description></item>
    ///   <item><description>보드가 없거나 축 번호가 유효하지 않으면 <see cref="AjinAxis"/> 내부에서 시뮬레이션 모드로 폴백한다.</description></item>
    /// </list>
    /// </summary>
    public sealed class MotionAxisManager
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, BaseAxis> _byKey = new Dictionary<string, BaseAxis>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _order = new List<string>();
        private string _currentKey;

        /// <summary>장비 프로젝트에서 실제 축 인스턴스를 생성하기 위한 팩토리.</summary>
        public Func<MotionAxisDefinition, BaseAxis> AxisFactory { get; set; }

        /// <summary>축이 등록될 때 발생한다.</summary>
        public event Action<BaseAxis> AxisAdded;

        /// <summary>축이 제거될 때 발생한다.</summary>
        public event Action<BaseAxis> AxisRemoved;

        /// <summary>현재 선택 축이 바뀔 때 발생한다.</summary>
        public event Action<BaseAxis> CurrentAxisChanged;

        /// <summary>등록된 축 개수.</summary>
        public int Count
        {
            get { lock (_gate) { return _byKey.Count; } }
        }

        /// <summary>현재 선택된 축.</summary>
        public BaseAxis Current
        {
            get
            {
                lock (_gate)
                {
                    return _currentKey != null && _byKey.ContainsKey(_currentKey) ? _byKey[_currentKey] : null;
                }
            }
        }

        /// <summary>유닛명과 축명으로 복합키를 만든다.</summary>
        public static string MakeKey(string unitName, string axisName)
        {
            return (unitName ?? string.Empty) + "||" + (axisName ?? string.Empty);
        }

        /// <summary>축을 등록한다.</summary>
        public void Register(BaseAxis axis)
        {
            if (axis == null) throw new ArgumentNullException(nameof(axis));

            string key = MakeKey(axis.Setup.UnitName, axis.Name);
            lock (_gate)
            {
                if (_byKey.ContainsKey(key))
                    throw new InvalidOperationException("이미 등록된 축입니다: " + key);

                _byKey[key] = axis;
                _order.Add(key);
                if (_currentKey == null)
                    _currentKey = key;
            }

            Action<BaseAxis> added = AxisAdded;
            if (added != null) added(axis);

            if (_currentKey == key)
            {
                Action<BaseAxis> changed = CurrentAxisChanged;
                if (changed != null) changed(axis);
            }
        }

        /// <summary>축 정의로 <see cref="AjinAxis"/> 를 만들고 등록한다.</summary>
        public BaseAxis CreateAndRegister(MotionAxisDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.Setup != null && !definition.Setup.IsEnabled) return null;
            if (AxisFactory == null)
                throw new InvalidOperationException("AxisFactory is not configured.");

            string name = definition.Name;
            if (string.IsNullOrWhiteSpace(name) && definition.Setup != null)
                name = definition.Setup.DisplayName;
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("축 이름이 비어 있습니다.");

            BaseAxis axis = AxisFactory(definition);
            if (axis == null) return null;
            Register(axis);
            return axis;
        }

        /// <summary>JSON 파일에서 축 정의를 읽어 전체 등록한다.</summary>
        public MotionAxisStore LoadAndRegister(string filePath)
        {
            MotionAxisStore store = MotionAxisStore.LoadOrCreate(filePath);
            Clear();

            for (int i = 0; i < store.Items.Count; i++)
            {
                MotionAxisDefinition item = store.Items[i];
                if (item == null || item.Setup == null || !item.Setup.IsEnabled) continue;
                CreateAndRegister(item);
            }

            return store;
        }

        /// <summary>현재 등록된 축 정보를 JSON 파일에 저장한다.</summary>
        public void Save(string filePath)
        {
            MotionAxisStore store = new MotionAxisStore();
            BaseAxis[] axes = GetAll();
            for (int i = 0; i < axes.Length; i++)
                store.Upsert(AxisDataMapper.FromAxis(axes[i]));
            store.Save(filePath);
        }

        /// <summary>축 정의를 현재 등록된 축에 적용한다. 축이 없으면 생성 후 등록한다.</summary>
        public BaseAxis Upsert(MotionAxisDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            string key = MakeKey(definition.Setup != null ? definition.Setup.UnitName : string.Empty, definition.Name);
            lock (_gate)
            {
                BaseAxis axis;
                if (_byKey.TryGetValue(key, out axis))
                {
                    AxisDataMapper.Apply(definition, axis);
                    return axis;
                }
            }

            return CreateAndRegister(definition);
        }

        /// <summary>축 등록을 해제한다.</summary>
        public bool Unregister(string unitName, string axisName)
        {
            string key = MakeKey(unitName, axisName);
            BaseAxis removed = null;

            lock (_gate)
            {
                if (!_byKey.TryGetValue(key, out removed))
                    return false;

                _byKey.Remove(key);
                _order.Remove(key);
                if (_currentKey == key)
                    _currentKey = _order.Count > 0 ? _order[0] : null;
            }

            Action<BaseAxis> h = AxisRemoved;
            if (h != null) h(removed);

            Action<BaseAxis> changed = CurrentAxisChanged;
            if (changed != null) changed(Current);

            return true;
        }

        /// <summary>등록된 축을 모두 제거한다.</summary>
        public void Clear()
        {
            BaseAxis[] axes = GetAll();
            lock (_gate)
            {
                _byKey.Clear();
                _order.Clear();
                _currentKey = null;
            }

            for (int i = 0; i < axes.Length; i++)
            {
                Action<BaseAxis> h = AxisRemoved;
                if (h != null) h(axes[i]);
            }
        }

        /// <summary>복합키로 축을 조회한다.</summary>
        public BaseAxis Get(string unitName, string axisName)
        {
            string key = MakeKey(unitName, axisName);
            lock (_gate)
            {
                BaseAxis axis;
                return _byKey.TryGetValue(key, out axis) ? axis : null;
            }
        }

        /// <summary>축 이름이 전역에서 유일할 때만 축을 반환한다.</summary>
        public BaseAxis Get(string axisName)
        {
            lock (_gate)
            {
                BaseAxis found = null;
                foreach (BaseAxis axis in _byKey.Values)
                {
                    if (!string.Equals(axis.Name, axisName, StringComparison.OrdinalIgnoreCase)) continue;
                    if (found != null) return null;
                    found = axis;
                }
                return found;
            }
        }

        /// <summary>등록된 모든 축을 등록 순서대로 반환한다.</summary>
        public BaseAxis[] GetAll()
        {
            lock (_gate)
            {
                BaseAxis[] axes = new BaseAxis[_order.Count];
                for (int i = 0; i < _order.Count; i++)
                    axes[i] = _byKey[_order[i]];
                return axes;
            }
        }

        /// <summary>현재 선택 축을 지정한다.</summary>
        public bool Select(string unitName, string axisName)
        {
            string key = MakeKey(unitName, axisName);
            BaseAxis axis;
            lock (_gate)
            {
                if (!_byKey.TryGetValue(key, out axis)) return false;
                _currentKey = key;
            }

            Action<BaseAxis> h = CurrentAxisChanged;
            if (h != null) h(axis);
            return true;
        }

        /// <summary>모든 축을 감속 정지한다.</summary>
        public void StopAll()
        {
            BaseAxis[] axes = GetAll();
            for (int i = 0; i < axes.Length; i++)
            {
                try { axes[i].Stop(); } catch { }
            }
        }

        /// <summary>모든 축을 비상 정지한다.</summary>
        public void EStopAll()
        {
            BaseAxis[] axes = GetAll();
            for (int i = 0; i < axes.Length; i++)
            {
                try { axes[i].EStop(); } catch { }
            }
        }
    }
}
