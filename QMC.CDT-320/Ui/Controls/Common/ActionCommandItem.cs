using System;
using System.Threading.Tasks;

namespace QMC.CDT_320.Ui.Controls
{
    public class ActionCommandItem
    {
        public string Key { get; set; }
        public string Text { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public int ColumnSpan { get; set; }
        public bool Enabled { get; set; }
        public Func<Task<int>> ExecuteAsync { get; set; }

        public ActionCommandItem()
        {
            try
            {
                ColumnSpan = 1;
                Enabled = true;
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
