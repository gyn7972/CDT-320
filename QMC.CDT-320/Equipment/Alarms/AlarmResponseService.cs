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

                AlarmResponsePolicy policy = _policyStore.Resolve(alarm);

                // 중앙 안전 계약:
                // AlarmManager에 알람이 발생하면 개별 정책과 무관하게 장비를 Alarm 상태로 유지한다.
                // 축은 Servo OFF가 아니라 EStop으로 모션만 즉시 정지하고 위치 유지력은 보존한다.
                _controller.SetAlarmStateFromAlarmResponse(alarm.Code);

                Task<int> responseTask = HandleAlarmAsync(alarm, policy);
                responseTask.ContinueWith(
                    task => Log.Write("Main", "SYSTEM", "AlarmResponseService",
                        "Alarm response background task faulted. code=" + alarm.Code +
                        ", error=" + (task.Exception != null
                            ? task.Exception.GetBaseException().Message
                            : "unknown") + " - Failed"),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AlarmResponseService", "Alarm response dispatch failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private async Task<int> HandleAlarmAsync(AlarmRecord alarm, AlarmResponsePolicy policy)
        {
            try
            {
                Log.Write("Main", "SYSTEM", "AlarmResponseService",
                    "Alarm response start. code=" + alarm.Code + ", source=" + alarm.Source +
                    ", severity=" + alarm.Severity +
                    ", configuredScope=" + (policy != null ? policy.StopScope.ToString() : "None") +
                    ", enforcedScope=Equipment, emergency=True - Start");

                // 중앙 안전 계약은 알람 코드/Severity/개별 StopScope 예외를 허용하지 않는다.
                // 반드시 전체 축 EStop을 먼저 실행하고, 이어서 모든 실행 도메인을 취소한다.
                // EStop은 Servo OFF가 아니므로 수직축의 위치 유지력은 그대로 보존된다.
                Task<int> axisStopTask = _controller.StopAllAxesAsync(true);
                Task<int> sequenceStopTask = _controller.StopSequenceForAlarmAsync(
                    alarm != null ? alarm.Code : "");

                int stopResult = await axisStopTask.ConfigureAwait(false);
                int sequenceResult = await sequenceStopTask.ConfigureAwait(false);
                if (sequenceResult != 0 && stopResult == 0)
                    stopResult = sequenceResult;

                Log.Write("Main", "SYSTEM", "AlarmResponseService",
                    "Alarm response complete. code=" + alarm.Code +
                    ", result=" + stopResult +
                    (stopResult == 0 ? " - Ok" : " - Failed"));
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
                bool immediateSequenceStop = alarm != null &&
                    (alarm.Severity == AlarmSeverity.Error || alarm.Severity == AlarmSeverity.Critical);
                // Machine Alarm 상태를 설정하는 알람은 Auto/Manual 구분 없이 즉시 취소한다.
                // Alarm 상태를 설정하지 않는 경고성 요청만 기존 안전 경계 정지를 유지한다.
                if (!immediateSequenceStop &&
                    !policy.SetMachineAlarmStatus &&
                    policy.StopScope == AlarmStopScope.None &&
                    !policy.UseEmergencyStop)
                {
                    await _controller.RequestCycleStopSequenceAsync().ConfigureAwait(false);
                    Log.Write("Main", "SYSTEM", "AlarmResponseService",
                        "Alarm response requested graceful cycle stop. code=" + code +
                        ", severity=" + (alarm != null ? alarm.Severity.ToString() : "-") + " - Requested");
                    return 0;
                }

                Log.Write("Main", "SYSTEM", "AlarmResponseService",
                    "Alarm response requested immediate sequence cancellation. code=" + code +
                    ", severity=" + (alarm != null ? alarm.Severity.ToString() : "-") +
                    ", scope=" + policy.StopScope + " - Requested");
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
