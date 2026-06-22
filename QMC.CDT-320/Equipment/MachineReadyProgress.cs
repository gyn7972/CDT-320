using System;

namespace QMC.CDT320
{
    public enum MachineReadySequenceState
    {
        Idle,
        Running,
        Completed,
        Failed,
        Canceled
    }

    public sealed class MachineReadyProgress
    {
        public MachineReadyProgress(
            MachineReadySequenceState state,
            int percent,
            int completedSteps,
            int totalSteps,
            string currentStepName,
            string message)
        {
            State = state;
            Percent = Math.Max(0, Math.Min(100, percent));
            CompletedSteps = Math.Max(0, completedSteps);
            TotalSteps = Math.Max(0, totalSteps);
            CurrentStepName = currentStepName ?? string.Empty;
            Message = message ?? string.Empty;
            UpdatedAt = DateTime.Now;
        }

        public MachineReadySequenceState State { get; private set; }
        public int Percent { get; private set; }
        public int CompletedSteps { get; private set; }
        public int TotalSteps { get; private set; }
        public string CurrentStepName { get; private set; }
        public string Message { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public bool IsRunning
        {
            get { return State == MachineReadySequenceState.Running; }
        }
    }
}
