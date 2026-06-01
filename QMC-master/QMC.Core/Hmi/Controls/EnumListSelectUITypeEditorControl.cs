using MechaSys.SoftBricks.Hmi.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace QMC.Hmi.Controls
{
    public partial class EnumListSelectUITypeEditorControl<T> : UITypeEditorControl
        where T : Enum
    {
        #region Constructor
        public EnumListSelectUITypeEditorControl(List<T> value, IWindowsFormsEditorService editorService) : base(value, editorService)
        {
            InitializeComponent();
        }
        public EnumListSelectUITypeEditorControl() : this(null, null) { }
        #endregion

        #region Method
        private void buttonOperation_Click(object sender, EventArgs e)
        {
            if(sender == null) return;

            if(sender == this.buttonXAdd)
            {
                int row = 0;
                if(this.comboBoxX1.SelectedItem == null)
                {
                    MessageBox.Show("Selected item");
                    return;
                }
                if(this.Value.Contains((T)this.comboBoxX1.SelectedItem) == true)
                {
                    MessageBox.Show("Already exist");
                    return;
                }
                row = this.flexGrid1.Rows.Add();
                this.flexGrid1[this.ColumnName.Name, row].Value = this.comboBoxX1.SelectedItem;
                this.Value.Add((T)this.comboBoxX1.SelectedItem);
            }
            else if(sender == this.buttonXRemove)
            {
                int row = 0;
                if(this.flexGrid1.SelectedRows.Count <= 0)
                {
                    MessageBox.Show("Remove item does not selected");
                    return;
                }
                row = this.flexGrid1.SelectedRows[0].Index;
                T item = (T)this.flexGrid1.Rows[row].Tag;
                this.flexGrid1.Rows.RemoveAt(row);
                this.Value.Remove(item);
            }

        }
        #endregion

        #region UITypeEditorControl Members
        public new List<T> Value
        {
            get { return base.Value as List<T>; }
            set { base.Value = value; }
        }
        protected override void EditorControl_Load(object sender, EventArgs e)
        {
            foreach(object item in Enum.GetValues(typeof(T)))
            {
                this.comboBoxX1.Items.Add(item);
            }
            this.flexGrid1.Rows.Clear();

            if(this.Value.Count > 0)
                this.flexGrid1.Rows.Add(this.Value.Count);

            for(int i = 0; i < this.Value.Count; i++)
            {
                this.flexGrid1[this.ColumnName.Name, i].Value = this.Value[i].ToString();
                this.flexGrid1.Rows[i].Tag = this.Value[i];
            }

            base.EditorControl_Load(sender, e);
        }
        #endregion
    }
}
