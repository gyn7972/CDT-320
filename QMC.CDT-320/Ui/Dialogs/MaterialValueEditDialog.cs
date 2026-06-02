using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class MaterialValueEditDialog : Form
    {
        public MaterialValueEditDialog(string fieldName, string value)
        {
            InitializeComponent();
            lblFieldValue.Text = string.IsNullOrEmpty(fieldName) ? "Material" : fieldName;
            txtValue.Text = value ?? "";
            txtValue.SelectAll();
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
