using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.CDT320.Initialization
{
    /// <summary>
    /// 초기화 Monitor에 표시할 Step 진행 상태를 스레드 안전하게 보관합니다.
    /// 저장 파일 처리와 UI 이벤트 전달은 MachineController가 계속 담당합니다.
    /// </summary>
    internal sealed class AxisInitializeProgressStore
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, AxisInitializeStepProgress> _stepStates =
            new Dictionary<string, AxisInitializeStepProgress>(StringComparer.OrdinalIgnoreCase);

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _stepStates.Count;
                }
            }
        }

        public bool TryGet(
            AxisInitializeStep step,
            out AxisInitializeStepProgress progress)
        {
            progress = null;
            try
            {
                AxisInitializeStepProgress stored;
                lock (_gate)
                {
                    if (!_stepStates.TryGetValue(GetStepKey(step), out stored))
                        return false;

                    progress = Clone(stored);
                    return progress != null;
                }
            }
            catch
            {
                progress = null;
                return false;
            }
            finally
            {
            }
        }

        public void Set(AxisInitializeStepProgress progress)
        {
            try
            {
                if (progress == null)
                    return;

                AxisInitializeStepProgress copy = Clone(progress);
                lock (_gate)
                {
                    _stepStates[GetStepKey(copy.StepNo, copy.GroupName)] = copy;
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        public List<AxisInitializeStepProgress> Snapshot()
        {
            try
            {
                lock (_gate)
                {
                    return _stepStates.Values
                        .Where(x => x != null)
                        .Select(Clone)
                        .ToList();
                }
            }
            catch
            {
                return new List<AxisInitializeStepProgress>();
            }
            finally
            {
            }
        }

        public void Clear()
        {
            try
            {
                lock (_gate)
                {
                    _stepStates.Clear();
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static AxisInitializeStepProgress Clone(AxisInitializeStepProgress progress)
        {
            if (progress == null)
                return null;

            return new AxisInitializeStepProgress
            {
                StepNo = progress.StepNo,
                GroupName = progress.GroupName ?? "",
                Status = progress.Status ?? "",
                Message = progress.Message ?? ""
            };
        }

        private static string GetStepKey(AxisInitializeStep step)
        {
            return step == null
                ? GetStepKey(0, "")
                : GetStepKey(step.StepNo, step.GroupName);
        }

        private static string GetStepKey(int stepNo, string groupName)
        {
            return stepNo.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                   "|" + (groupName ?? "");
        }
    }
}
