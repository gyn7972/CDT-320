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

                if (policy.StopSequence)
                    await _controller.StopSequenceForAlarmAsync(alarm.Code).ConfigureAwait(false);

                int stopResult = await StopAxesByPolicyAsync(alarm, policy).ConfigureAwait(false);

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

        private async Task<int> StopAxesByPolicyAsync(AlarmRecord alarm, AlarmResponsePolicy policy)
        {
            try
            {
                if (policy == null)
                    return 0;

                string sourceAxis = _controller.ResolveAxisNameFromAlarm(alarm != null ? alarm.Source : null, alarm != null ? alarm.Code : null);

                switch (policy.StopScope)
                {
                    case AlarmStopScope.SourceAxisOnly:
                        return await _controller.StopAxesAsync(new[] { sourceAxis }, policy.UseEmergencyStop).ConfigureAwait(false);

                    case AlarmStopScope.InterferenceGroup:
                        if (string.IsNullOrWhiteSpace(sourceAxis))
                            return await _controller.StopAllAxesAsync(policy.UseEmergencyStop).ConfigureAwait(false);
                        return await _controller.StopInterferenceGroupAsync(sourceAxis, policy.UseEmergencyStop).ConfigureAwait(false);

                    case AlarmStopScope.Unit:
                    case AlarmStopScope.Equipment:
                        return await _controller.StopAllAxesAsync(policy.UseEmergencyStop).ConfigureAwait(false);

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
