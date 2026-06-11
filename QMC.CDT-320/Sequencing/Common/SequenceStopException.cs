using System;

namespace QMC.CDT320.Sequencing
{
    public sealed class SequenceStopException : Exception
    {
        public SequenceStopException(string reason)
            : base(reason ?? "Sequence stopped.")
        {
        }
    }
}
