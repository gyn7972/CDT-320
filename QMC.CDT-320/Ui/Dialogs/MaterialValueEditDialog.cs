using QMC.CDT_320.Ui.Localization;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class MaterialValueEditDialog : Form
    {
        public MaterialValueEditDialog(string fieldName, string value)
        {
            InitializeComponent();
            Lang.BindKey(this, "dialog.material.title");
            Lang.BindKey(lblTitle, "dialog.material.title");
            Lang.BindKey(lblFieldTitle, "dialog.material.field");
            Lang.BindKey(btnOk, "common.ok");
            Lang.BindKey(btnCancel, "common.cancel");
            if (string.IsNullOrEmpty(fieldName)) Lang.BindKey(lblFieldValue, "dialog.material.material");
            else Lang.Bind(lblFieldValue, fieldName);
            txtValue.Text = value ?? "";
            txtValue.SelectAll();
            Load += (sender, e) => Lang.Apply(this);
        }

        public string ValueText
        {
            get { return txtValue.Text ?? ""; }
        }

        private void btnOk_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
