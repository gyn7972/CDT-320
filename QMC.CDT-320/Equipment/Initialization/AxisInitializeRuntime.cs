using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QMC.CDT320.Initialization
{
    internal delegate bool AxisInitializeStepVerifier(
        AxisInitializeStep step,
        out string reason);

    /// <summary>
    /// AxisInitializeExecutor가 MachineController 전체에 접근하지 않도록
    /// 현재 단계에 필요한 초기화 기능만 명시적으로 제공합니다.
    /// </summary>
    internal sealed class AxisInitializeRuntime
    {
        private readonly Action _beginRun;
        private readonly Action<IEnumerable<AxisInitializeStep>> _resetProgressForRun;
        private readonly Func<AxisInitializeStep, ISet<string>, Task<int>> _executeStepAsync;
        private readonly Func<IList<AxisInitializeStep>, HashSet<string>> _resolveLaneAxisNames;
        private readonly AxisInitializeStepVerifier _verifyStep;
        private readonly Func<Task<int>> _stopAllAxesAsync;
        private readonly Func<AxisInitializeStep, string, int, string> _resolveStepFailureMessage;
        private readonly Func<string> _getLastFailureMessage;

        public AxisInitializeRuntime(
            Action beginRun,
            Action<IEnumerable<AxisInitializeStep>> resetProgressForRun,
            Func<AxisInitializeStep, ISet<string>, Task<int>> executeStepAsync,
            Func<IList<AxisInitializeStep>, HashSet<string>> resolveLaneAxisNames,
            AxisInitializeStepVerifier verifyStep,
            Func<Task<int>> stopAllAxesAsync,
            Func<AxisInitializeStep, string, int, string> resolveStepFailureMessage,
            Func<string> getLastFailureMessage)
        {
            _beginRun = beginRun ?? throw new ArgumentNullException(nameof(beginRun));
            _resetProgressForRun = resetProgressForRun ?? throw new ArgumentNullException(nameof(resetProgressForRun));
            _executeStepAsync = executeStepAsync ?? throw new ArgumentNullException(nameof(executeStepAsync));
            _resolveLaneAxisNames = resolveLaneAxisNames ?? throw new ArgumentNullException(nameof(resolveLaneAxisNames));
            _verifyStep = verifyStep ?? throw new ArgumentNullException(nameof(verifyStep));
            _stopAllAxesAsync = stopAllAxesAsync ?? throw new ArgumentNullException(nameof(stopAllAxesAsync));
            _resolveStepFailureMessage = resolveStepFailureMessage ??
                throw new ArgumentNullException(nameof(resolveStepFailureMessage));
            _getLastFailureMessage = getLastFailureMessage ??
                throw new ArgumentNullException(nameof(getLastFailureMessage));
        }

        public void BeginRun(IEnumerable<AxisInitializeStep> enabledSteps)
        {
            _beginRun();
            _resetProgressForRun(enabledSteps);
        }

        public Task<int> ExecuteStepAsync(
            AxisInitializeStep step,
            ISet<string> allowedConcurrentAxisNames)
        {
            return _executeStepAsync(step, allowedConcurrentAxisNames);
        }

        public HashSet<string> ResolveLaneAxisNames(IList<AxisInitializeStep> laneSteps)
        {
            return _resolveLaneAxisNames(laneSteps) ??
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool VerifyStep(AxisInitializeStep step, out string reason)
        {
            return _verifyStep(step, out reason);
        }

        public Task<int> StopAllAxesAsync()
        {
            return _stopAllAxesAsync();
        }

        public string ResolveStepFailureMessage(
            AxisInitializeStep step,
            string laneName,
            int resultCode)
        {
            return _resolveStepFailureMessage(step, laneName, resultCode);
        }

        public string GetLastFailureMessage()
        {
            return _getLastFailureMessage() ?? string.Empty;
        }
    }
}
