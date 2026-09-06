using System;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320
{
    public partial class MachineController
    {
        public string LastManualOutputBatchMessage { get; private set; }

        public Task<int> RunManualOutputLoadAllAsync(IProgress<string> progress = null)
        {
            return RunManualOutputBatchAsync(true, progress);
        }

        public Task<int> RunManualOutputUnloadAllAsync(IProgress<string> progress = null)
        {
            return RunManualOutputBatchAsync(false, progress);
        }

        private async Task<int> RunManualOutputBatchAsync(bool load, IProgress<string> progress)
        {
            string label = load ? "OUTPUT LOAD(ALL)" : "OUTPUT UNLOAD(ALL)";
            string detail = string.Empty;
            LastManualOutputBatchMessage = string.Empty;
            try
            {
                // 양쪽 처리가 끝날 때까지 Manual busy/속도/취소 토큰을 한 번만 점유한다.
                return await RunManualUnitProcessAsync(
                    label,
                    "SEQ-MANUAL-OUT-ALL",
                    async (context, token) =>
                    {
                        var sequence = new OutputSequence(context);
                        sequence.Configure(SequenceRunMode.Manual);
                        try
                        {
                            return await sequence.ExecuteManualOutputBatchAsync(
                                load, token, SaveManualOutputBatchCheckpoint, progress).ConfigureAwait(false);
                        }
                        finally
                        {
                            detail = sequence.LastManualOutputBatchMessage;
                        }
                    },
                    () => detail).ConfigureAwait(false);
            }
            finally
            {
                LastManualOutputBatchMessage = string.IsNullOrWhiteSpace(detail)
                    ? LastActionFailureMessage : detail;
            }
        }

        private void SaveManualOutputBatchCheckpoint(string description)
        {
            string reason = "ManualOutputAll:" + description;
            // 다음 Side의 실패/취소와 무관하게 이미 끝난 이송은 즉시 확정 저장한다.
            bool runtimeSaved = SaveMachineRuntimeState(reason);
            bool materialSaved = MaterialStateService.TryFlushPendingSave(reason);
            if (!runtimeSaved || !materialSaved)
            {
                string message = description + " 이송은 완료했지만 상태 저장을 확정하지 못했습니다. " +
                    "runtimeSaved=" + runtimeSaved + ", materialSaved=" + materialSaved +
                    ", reason=" + MaterialSnapshotStore.LastSaveFailureReason;
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualOutputAllSave", message + " - Warning");
                AlarmManager.Raise(AlarmSeverity.Warning, "SEQ-MANUAL-OUT-ALL-SAVE", "MachineController", message);
                // 기존 Alarm 정책이 다음 동작을 취소하므로 저장 원인을 일반 취소 문구로 잃지 않게 전달한다.
                throw new SequenceStopException(message);
            }
        }

        private static string ResolveManualProcessFailure(string fallback, Func<string> failureDetail)
        {
            string detail = failureDetail != null ? failureDetail() : null;
            return string.IsNullOrWhiteSpace(detail) ? fallback : detail;
        }
    }
}
