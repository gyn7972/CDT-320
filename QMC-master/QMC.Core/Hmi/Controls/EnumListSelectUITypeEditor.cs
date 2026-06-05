using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using System.Windows.Forms;

namespace QMC.Hmi.Controls
{
    public class EnumListSelectUITypeEditor<T> : UITypeEditor
        where T : Enum
    {
        #region Constructor
        public EnumListSelectUITypeEditor() { }
        #endregion

        #region UITypeEditor Members
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService editorService;
            EnumListSelectUITypeEditorControl<T> control = null;
            List<string> names = new List<string>();
            // get service
            if((editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService))) == null)
                return value;

            try
            {
                control = new EnumListSelectUITypeEditorControl<T>(value as List<T>, editorService);
                editorService.DropDownControl(control);
                return control.Result == DialogResult.OK ? control.Value : value;

            }
            finally
            {
                editorService = null;
            }
        }
        #endregion
    }
}
