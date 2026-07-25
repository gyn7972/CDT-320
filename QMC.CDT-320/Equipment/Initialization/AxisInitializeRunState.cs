using System;
using System.Collections.Generic;
using QMC.Common.Motion;

namespace QMC.CDT320.Initialization
{
    /// <summary>
    /// 한 번의 초기화 실행 안에서 이미 HOME을 완료한 축을 추적합니다.
    /// 장비 영속 상태와 분리된 실행 범위 상태입니다.
    /// </summary>
    internal sealed class AxisInitializeRunState
    {
        private readonly object _gate = new object();
        private HashSet<string> _homedAxisNames;
        private bool _isActive;

        public bool TryBegin()
        {
            lock (_gate)
            {
                if (_isActive)
                    return false;

                _isActive = true;
                _homedAxisNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return true;
            }
        }

        public void End()
        {
            lock (_gate)
            {
                _homedAxisNames = null;
                _isActive = false;
            }
        }

        public bool IsAxisHomed(BaseAxis axis)
        {
            try
            {
                if (axis == null || string.IsNullOrWhiteSpace(axis.Name))
                    return false;

                axis.UpdateStatus();
                lock (_gate)
                {
                    if (_homedAxisNames == null ||
                        !_homedAxisNames.Contains(axis.Name))
                    {
                        return false;
                    }

                    if (axis.IsAlarm || !axis.IsHomeDone)
                    {
                        _homedAxisNames.Remove(axis.Name);
                        return false;
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public void MarkAxisHomed(BaseAxis axis)
        {
            try
            {
                if (axis == null || string.IsNullOrWhiteSpace(axis.Name))
                    return;

                lock (_gate)
                {
                    if (_homedAxisNames != null)
                        _homedAxisNames.Add(axis.Name);
                }
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
