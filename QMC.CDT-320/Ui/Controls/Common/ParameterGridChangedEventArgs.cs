using System;

namespace QMC.CDT_320.Ui.Controls
{
    public sealed class ParameterGridChangedEventArgs : EventArgs
    {
        public ParameterGridItem Item { get; private set; }
        public ParameterGridScope Scope { get; private set; }

        public ParameterGridChangedEventArgs(ParameterGridItem item)
        {
            try
            {
                Item = item;
                Scope = item == null ? ParameterGridScope.Config : item.Scope;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }
    }
}
