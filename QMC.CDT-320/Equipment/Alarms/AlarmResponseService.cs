using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Alarms
{
    public sealed class AlarmResponseService : IDisposable
    {
        private readonly MachineController _controller;
        private readonly AlarmResponsePolicyStore _policyStore;
        private int _started;

        public AlarmResponseService(MachineController controller)
            : this(controller, AlarmResponsePolicyStore.CreateDefault())
        {
        }

        public AlarmResponseService(MachineController controller, AlarmResponsePolicyStore policyStore)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _policyStore = policyStore ?? AlarmResponsePolicyStore.CreateDefault();
        }

        public void Start()
        {
            try
            {
                if (Interlocked.Exchange(ref _started, 1) == 1)
                    return;

                AlarmManager.AlarmRaised += OnAlarmRaised;
                Log.Write("Main", "SYSTEM", "AlarmResponseService", "Alarm response service started. - Ok");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AlarmResponseService", "Alarm response service start failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public void Stop()
        {
            try
            {
                if (Interlocked.Exchange(ref _started, 0) == 0)
                    return;

                AlarmManager.AlarmRaised -= OnAlarmRaised;
                Log.Write("Main", "SYSTEM", "AlarmResponseService", "Alarm response service stopped. - Ok");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AlarmResponseService", "Alarm response service stop failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void OnAlarmRaised(AlarmRecord alarm)
        {
            try
            {
                if (alarm == null)
                    return;

                Task.Run(async () => await HandleAlarmAsync(alarm).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AlarmResponseService", "Alarm response dispatch failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private async Task<int> HandleAlarmAsync(AlarmRecord alarm)
        {
            try
            {
                var policy = _policyStore.Resolve(alarm);
                if (policy == null || policy.StopScope == AlarmStopScope.None && !policy.StopSequence)
                {
                    Log.Write("Main", "SYSTEM", "AlarmResponseService",
                        "Alarm response skipped. code=" + alarm.Code + ", source=" + alarm.Source + " - Ok");
                    return 0;
                }

                Log.Write("Main", "SYSTEM", "AlarmResponseService",
                    "Alarm response start. code=" + alarm.Code + ", source=" + alarm.Source +
                    ", severity=" + alarm.Severity + ", scope=" + policy.StopScope + " - Start");

                // 알람은 일반 정지와 다르다. 축 정지 명령을 먼저 내린 뒤 시퀀스를 정리한다.
                int stopResult = await StopAxesByPolicyAsync(alarm, policy).ConfigureAwait(false);
                int sequenceResult = policy.StopSequence
                    ? await StopSequenceByPolicyAsync(alarm, policy).ConfigureAwait(false)
                    : 0;
                if (sequenceResult != 0 && stopResult == 0)
                    stopResult = sequenceResult;

                if (policy.SetMachineAlarmStatus)
                    _controller.SetAlarmStateFromAlarmResponse(alarm.Code);

                Log.Write("Main", "SYSTEM", "AlarmResponseService",
                    "Alarm response complete. code=" + alarm.Code + ", result=" + stopResult + " - Ok");
                return stopResult;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AlarmResponseService",
                    "Alarm response failed. code=" + (alarm != null ? alarm.Code : "") + ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> StopSequenceByPolicyAsync(AlarmRecord alarm, AlarmResponsePolicy policy)
        {
            try
            {
                if (policy == null)
                    return 0;

                string code = alarm != null ? alarm.Code : "";
                if (policy.StopScope == AlarmStopScope.None && !policy.UseEmergencyStop)
                {
                    await _controller.RequestCycleStopSequenceAsync().ConfigureAwait(false);
                    Log.Write("Main", "SYSTEM", "AlarmResponseService",
                        "Alarm response requested cycle stop. code=" + code + " - Requested");
                    return 0;
                }

                return await _controller.StopSequenceForAlarmAsync(code).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AlarmResponseService",
                    "Alarm response sequence stop failed. code=" +
                    (alarm != null ? alarm.Code : "") + ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> StopAxesByPolicyAsync(AlarmRecord alarm, AlarmResponsePolicy policy)
        {
            try
            {
                if (policy == null)
                    return 0;

                string sourceAxis = _controller.ResolveAxisNameFromAlarm(alarm != null ? alarm.Source : null, alarm != null ? alarm.Code : null);

                switch (policy.StopScope)
                {
                    // 알람 발생 축만 정지
                    case AlarmStopScope.SourceAxisOnly:
                        return await _controller.StopAxesAsync(new[] { sourceAxis }, policy.UseEmergencyStop).ConfigureAwait(false);

                    // 간섭 그룹 축 정지
                    case AlarmStopScope.InterferenceGroup:
                        if (string.IsNullOrWhiteSpace(sourceAxis))
                            return await _controller.StopAllAxesAsync(policy.UseEmergencyStop).ConfigureAwait(false);
                        return await _controller.StopInterferenceGroupAsync(sourceAxis, policy.UseEmergencyStop).ConfigureAwait(false);

                    // 유닛/장비 범위는 전체 축 정지
                    case AlarmStopScope.Unit:
                    case AlarmStopScope.Equipment:
                        return await _controller.StopAllAxesAsync(policy.UseEmergencyStop).ConfigureAwait(false);

                    // 시퀀스/없음 범위는 축 정지 없음
                    case AlarmStopScope.Sequence:
                    case AlarmStopScope.None:
                    default:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AlarmResponseService",
                    "Alarm response axis stop failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
