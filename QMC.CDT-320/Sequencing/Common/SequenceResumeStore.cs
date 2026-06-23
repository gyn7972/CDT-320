using System;
using System.Collections.Generic;
using QMC.Common;

namespace QMC.CDT320.Sequencing
{
    public static class SequenceResumeStore
    {
        private static readonly object LockObject = new object();
        private static readonly Dictionary<string, SequenceExecutionState> States =
            new Dictionary<string, SequenceExecutionState>(StringComparer.OrdinalIgnoreCase);

        public static SequenceExecutionState GetOrCreate(string sequenceName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sequenceName))
                    sequenceName = "Sequence";

                lock (LockObject)
                {
                    SequenceExecutionState state;
                    if (!States.TryGetValue(sequenceName, out state))
                    {
                        state = new SequenceExecutionState(sequenceName);
                        States[sequenceName] = state;
                    }

                    return state;
                }
            }
            catch (Exception ex)
            {
                WriteLog("GetOrCreate", "Sequence state get failed: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        public static string ResolveStartStep(string sequenceName, string defaultStep)
        {
            try
            {
                var state = GetOrCreate(sequenceName);
                return state.CanResume ? state.ResumeStep : defaultStep;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveStartStep", "Sequence start step resolve failed: " + ex.Message + " - Failed");
                return defaultStep;
            }
            finally
            {
            }
        }

        public static void MarkRunning(string sequenceName, string step)
        {
            try
            {
                var state = GetOrCreate(sequenceName);
                lock (LockObject)
                {
                    state.CurrentStep = step ?? "";
                    state.Status = EquipmentStatus.ManualRunning;
                    state.StopKind = SequenceStopKind.None;
                    state.StopReason = "";
                    state.Touch();
                }
            }
            catch (Exception ex)
            {
                WriteLog("MarkRunning", "Sequence running state save failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public static void MarkStepCompleted(string sequenceName, string completedStep, string nextStep)
        {
            try
            {
                var state = GetOrCreate(sequenceName);
                lock (LockObject)
                {
                    state.LastCompletedStep = completedStep ?? "";
                    state.CurrentStep = nextStep ?? "";
                    state.ResumeStep = nextStep ?? "";
                    state.Status = EquipmentStatus.ManualRunning;
                    state.StopKind = SequenceStopKind.None;
                    state.StopReason = "";
                    state.Touch();
                }
            }
            catch (Exception ex)
            {
                WriteLog("MarkStepCompleted", "Sequence completed step save failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public static void MarkAlarm(string sequenceName, string failedStep, string reason)
        {
            try
            {
                var state = GetOrCreate(sequenceName);
                lock (LockObject)
                {
                    state.CurrentStep = failedStep ?? "";
                    state.ResumeStep = failedStep ?? "";
                    state.Status = EquipmentStatus.Alarm;
                    state.StopKind = SequenceStopKind.Alarm;
                    state.StopReason = reason ?? "";
                    state.Touch();
                }
            }
            catch (Exception ex)
            {
                WriteLog("MarkAlarm", "Sequence alarm state save failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public static void MarkCycleStopped(string sequenceName, string resumeStep, string reason)
        {
            try
            {
                var state = GetOrCreate(sequenceName);
                lock (LockObject)
                {
                    state.CurrentStep = resumeStep ?? "";
                    state.ResumeStep = resumeStep ?? "";
                    state.Status = EquipmentStatus.CycleStopped;
                    state.StopKind = SequenceStopKind.CycleStop;
                    state.StopReason = reason ?? "";
                    state.Touch();
                }
            }
            catch (Exception ex)
            {
                WriteLog("MarkCycleStopped", "Sequence cycle stop state save failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public static void MarkStopped(string sequenceName, string reason)
        {
            try
            {
                var state = GetOrCreate(sequenceName);
                lock (LockObject)
                {
                    state.Status = EquipmentStatus.Stopped;
                    state.StopKind = SequenceStopKind.Stop;
                    state.StopReason = reason ?? "";
                    state.CurrentStep = "";
                    state.ResumeStep = "";
                    state.Touch();
                }
            }
            catch (Exception ex)
            {
                WriteLog("MarkStopped", "Sequence stop state save failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public static void MarkCompleted(string sequenceName)
        {
            try
            {
                var state = GetOrCreate(sequenceName);
                lock (LockObject)
                {
                    state.Status = EquipmentStatus.Completed;
                    state.StopKind = SequenceStopKind.None;
                    state.StopReason = "";
                    state.CurrentStep = "";
                    state.ResumeStep = "";
                    state.Touch();
                }
            }
            catch (Exception ex)
            {
                WriteLog("MarkCompleted", "Sequence complete state save failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public static void Clear(string sequenceName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sequenceName))
                    return;

                lock (LockObject)
                {
                    States.Remove(sequenceName);
                }
            }
            catch (Exception ex)
            {
                WriteLog("Clear", "Sequence state clear failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public static void ClearAll()
        {
            try
            {
                lock (LockObject)
                {
                    States.Clear();
                }
            }
            catch (Exception ex)
            {
                WriteLog("ClearAll", "Sequence state clear all failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static void WriteLog(string source, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", source, message);
            }
            catch
            {
            }
            finally
            {
            }

            // 시퀀스 로그를 이력(EventLogger)에도 분류 기록(스코프 Kind 또는 메시지 접두어 라우팅).
            SequenceLog.EmitTrace(QMC.Common.Logging.EventKind.Event, source, message);
        }
    }
}
