using System;

namespace QMC.Vision.Ui.Controls
{
    // R1 — Handler API 1:1 미러.
    public sealed class ParameterGridChangedEventArgs : EventArgs
    {
        public ParameterGridItem Item { get; private set; }
        public ParameterGridScope Scope { get; private set; }

        public ParameterGridChangedEventArgs(ParameterGridItem item)
        {
            Item = item;
            Scope = item == null ? ParameterGridScope.Config : item.Scope;
        }
    }
}
