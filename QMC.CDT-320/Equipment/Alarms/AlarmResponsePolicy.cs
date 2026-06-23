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

                if (IsCriticalMotionOrInterlockAlarm(alarm))
                {
                    return new AlarmResponsePolicy
                    {
                        StopScope = AlarmStopScope.InterferenceGroup,
                        StopSequence = true,
                        SetMachineAlarmStatus = true,
                        UseEmergencyStop = true
                    };
                }

                if (alarm.Severity == AlarmSeverity.Critical)
                {
                    return new AlarmResponsePolicy
                    {
                        StopScope = AlarmStopScope.InterferenceGroup,
                        StopSequence = true,
                        SetMachineAlarmStatus = true,
                        UseEmergencyStop = true
                    };
                }

                if (alarm.Severity == AlarmSeverity.Warning)
                {
                    return new AlarmResponsePolicy
                    {
                        StopScope = AlarmStopScope.None,
                        StopSequence = true,
                        SetMachineAlarmStatus = true,
                        UseEmergencyStop = false
                    };
                }

                if (alarm.Severity == AlarmSeverity.Error)
                {
                    return new AlarmResponsePolicy
                    {
                        StopScope = AlarmStopScope.None,
                        StopSequence = true,
                        SetMachineAlarmStatus = true,
                        UseEmergencyStop = false
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

        private static bool IsCriticalMotionOrInterlockAlarm(AlarmRecord alarm)
        {
            try
            {
                if (alarm == null)
                    return false;

                string code = alarm.Code ?? "";
                string message = alarm.Message ?? "";

                if (IsExactOrPrefix(code, "E-STOP") ||
                    Contains(code, "INTERLOCK") ||
                    Contains(code, "LIMIT"))
                    return true;

                if (code.StartsWith("AX-MOVE", StringComparison.OrdinalIgnoreCase) ||
                    code.StartsWith("AX-HOME", StringComparison.OrdinalIgnoreCase) ||
                    code.StartsWith("AX-JOG", StringComparison.OrdinalIgnoreCase) ||
                    code.StartsWith("AX-SOFT-LIMIT", StringComparison.OrdinalIgnoreCase) ||
                    code.StartsWith("LIMIT-", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (Contains(code, "MOVE") &&
                    (Contains(message, "alarm=True") ||
                     Contains(message, "alarm=ON") ||
                     Contains(message, "알람=ON") ||
                     Contains(message, "Axis alarm is ON") ||
                     Contains(message, "축 알람이 ON")))
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool Contains(string value, string text)
        {
            return (value ?? "").IndexOf(text ?? "", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsExactOrPrefix(string value, string token)
        {
            value = value ?? "";
            token = token ?? "";
            return value.Equals(token, StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith(token + "-", StringComparison.OrdinalIgnoreCase);
        }
    }
}
