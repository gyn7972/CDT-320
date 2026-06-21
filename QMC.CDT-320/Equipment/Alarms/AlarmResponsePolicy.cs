using System;
using System.Collections.Generic;
using QMC.Common.Alarms;

namespace QMC.CDT320.Alarms
{
    public sealed class AlarmResponsePolicy
    {
        public AlarmStopScope StopScope { get; set; }
        public bool StopSequence { get; set; }
        public bool SetMachineAlarmStatus { get; set; }
        public bool UseEmergencyStop { get; set; }

        public static AlarmResponsePolicy None()
        {
            return new AlarmResponsePolicy
            {
                StopScope = AlarmStopScope.None,
                StopSequence = false,
                SetMachineAlarmStatus = false,
                UseEmergencyStop = false
            };
        }
    }

    public sealed class AlarmResponsePolicyStore
    {
        private readonly Dictionary<string, AlarmResponsePolicy> _byCode =
            new Dictionary<string, AlarmResponsePolicy>(StringComparer.OrdinalIgnoreCase);

        public static AlarmResponsePolicyStore CreateDefault()
        {
            var store = new AlarmResponsePolicyStore();

            store.Set("E-STOP", AlarmStopScope.Equipment, true, true, true);
            store.Set("TEST-ALARM", AlarmStopScope.Equipment, true, true, true);
            store.Set("AXL-OPEN", AlarmStopScope.Equipment, true, true, true);
            store.Set("AXL-DLL", AlarmStopScope.Equipment, true, true, true);

            store.Set("START-NOT-INITIALIZED", AlarmStopScope.None, false, false, false);
            store.Set("START-ALARM", AlarmStopScope.None, false, false, false);
            store.Set("START-RUNNING", AlarmStopScope.None, false, false, false);

            return store;
        }

        public void Set(string code, AlarmStopScope scope, bool stopSequence, bool setAlarmStatus, bool useEmergencyStop)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return;

                _byCode[code.Trim()] = new AlarmResponsePolicy
                {
                    StopScope = scope,
                    StopSequence = stopSequence,
                    SetMachineAlarmStatus = setAlarmStatus,
                    UseEmergencyStop = useEmergencyStop
                };
            }
            catch
            {
            }
            finally
            {
            }
        }

        public AlarmResponsePolicy Resolve(AlarmRecord alarm)
        {
            try
            {
                if (alarm == null)
                    return AlarmResponsePolicy.None();

                AlarmResponsePolicy policy;
                if (_byCode.TryGetValue(alarm.Code ?? "", out policy))
                    return policy;

                if (alarm.Severity == AlarmSeverity.Critical)
                {
                    return new AlarmResponsePolicy
                    {
                        StopScope = AlarmStopScope.Equipment,
                        StopSequence = true,
                        SetMachineAlarmStatus = true,
                        UseEmergencyStop = true
                    };
                }

                if (alarm.Severity == AlarmSeverity.Error)
                {
                    return new AlarmResponsePolicy
                    {
                        // 알람 코드별 정지 범위가 안전 검토로 확정되기 전까지는 보수적으로 전체 축 EStop을 기본값으로 둔다.
                        // 부분 정지가 검증된 알람만 CreateDefault()에서 명시 정책으로 낮춘다.
                        StopScope = AlarmStopScope.Equipment,
                        StopSequence = true,
                        SetMachineAlarmStatus = true,
                        UseEmergencyStop = true
                    };
                }

                if (alarm.Severity == AlarmSeverity.Warning)
                {
                    return new AlarmResponsePolicy
                    {
                        // AlarmManager에서 Warning을 Error로 승격하지만, 우회 유입에 대비해 Warning도 안전 정지로 처리한다.
                        StopScope = AlarmStopScope.Equipment,
                        StopSequence = true,
                        SetMachineAlarmStatus = true,
                        UseEmergencyStop = true
                    };
                }

                return AlarmResponsePolicy.None();
            }
            catch
            {
                return AlarmResponsePolicy.None();
            }
            finally
            {
            }
        }
    }
}
