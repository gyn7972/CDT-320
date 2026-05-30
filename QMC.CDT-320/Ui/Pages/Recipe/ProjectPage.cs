using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Data.Store;
using QMC.Common.Logging;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class ProjectPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private RecipeProject _current;

        public ProjectPage()
        {
            InitializeComponent();
            WireEvents();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            ReloadList();
            var names = RecipeStore.List();
            if (names.Count > 0) LoadProject(names[0]);
        }

        private void WireEvents()
        {
            listProjects.DoubleClick += (s, e) =>
            {
                if (listProjects.SelectedItem is string fileName) LoadProject(fileName);
            };
            btnDelete.Click += (s, e) => OnDelete();
            btnSaveAs.Click += (s, e) => OnSaveAs();
            btnOpen.Click += (s, e) => OnOpen();
            btnOpenFolder.Click += (s, e) => OnOpenFolder();
            btnSaveRecipe.Click += (s, e) => OnSaveCurrent();
        }

        private void ReloadList()
        {
            listProjects.Items.Clear();
            foreach (var fileName in RecipeStore.List())
                listProjects.Items.Add(fileName);
        }

        private void LoadProject(string fileName)
        {
            var project = RecipeStore.Load(fileName);
            if (project == null) return;

            _current = project;
            tbFile.Text = project.FileName;
            tbMachine.Text = project.MachineNumber;
            tbFlow.Text = project.CassetteFlow;
            tbMap.Text = project.MapFormat;
            tbDir.Text = project.MapDirection;
            tbChip.Text = project.ChipThickness.ToString("0");
            tbMaster.Text = project.MasterChipThickness.ToString("0");
            tbTape.Text = project.TapeThickness.ToString("0");
            tbBin.Text = project.BinSortNumber.ToString();
            tbLot.Text = project.LotId ?? "";
            tbPart.Text = project.PartId ?? "";
            tbInCst.Text = project.InputCassetteId ?? "";
            tbOutCst.Text = project.OutputCassetteId ?? "";
            tbColletM.Text = project.ColletModelNum ?? "";
            tbColletL.Text = project.ColletLotNum ?? "";
            tbXml.Text = project.XmlPath ?? "";

            EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-LOAD", "Project loaded: " + fileName);
            RecipeStore.SaveLastProjectName(fileName);

            try
            {
                var host = FindForm() as Form1;
                host?.LoadMachineRecipe(project.FileName);
                host?.RefreshProjectName(project.FileName);
                host?.Controller?.ApplyRecipeMode(project);
            }
            catch
            {
            }
        }

        private void OnOpen()
        {
            if (listProjects.SelectedItem is string fileName)
                LoadProject(fileName);
            else
                QMC.Common.MessageDialog.Show("Select a project.");
        }

        private void OnDelete()
        {
            if (!(listProjects.SelectedItem is string fileName)) return;
            if (QMC.Common.MessageDialog.Show("Delete project?\n" + fileName, "DELETE", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            RecipeStore.Delete(fileName);
            RecipeDataStore.DeleteRecipe(fileName);
            EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-DEL", "Project deleted: " + fileName);
            ReloadList();
        }

        private void OnSaveAs()
        {
            var project = CollectFromUi();
            string name = Prompt.Show("Input project file name.", project.FileName);
            if (string.IsNullOrWhiteSpace(name)) return;

            project.FileName = name.Trim();
            RecipeStore.Save(project);
            SaveMachineRecipe(project.FileName);
            EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-SAVEAS", "Project saved: " + project.FileName);
            ReloadList();
            listProjects.SelectedItem = project.FileName + ".Project";
        }

        private void OnSaveCurrent()
        {
            var project = CollectFromUi();
            if (string.IsNullOrWhiteSpace(project.FileName))
            {
                QMC.Common.MessageDialog.Show("File name is empty.");
                return;
            }

            RecipeStore.Save(project);
            SaveMachineRecipe(project.FileName);
            EventLogger.Write(EventKind.Event, Security.UserSession.Name, "RECIPE-SAVE", "Project saved: " + project.FileName);
            ReloadList();
            QMC.Common.MessageDialog.Show("Saved: " + project.FileName);
        }

        private void OnOpenFolder()
        {
            try
            {
                System.Diagnostics.Process.Start(RecipeStore.Dir);
            }
            catch
            {
            }
        }

        private RecipeProject CollectFromUi()
        {
            var project = _current ?? new RecipeProject();
            project.FileName = tbFile.Text.Trim();
            project.MachineNumber = tbMachine.Text.Trim();
            project.CassetteFlow = tbFlow.Text.Trim();
            project.MapFormat = tbMap.Text.Trim();
            project.MapDirection = tbDir.Text.Trim();
            double.TryParse(tbChip.Text, out var chip); project.ChipThickness = chip;
            double.TryParse(tbMaster.Text, out var master); project.MasterChipThickness = master;
            double.TryParse(tbTape.Text, out var tape); project.TapeThickness = tape;
            int.TryParse(tbBin.Text, out var bin); project.BinSortNumber = bin;
            project.LotId = tbLot.Text;
            project.PartId = tbPart.Text;
            project.InputCassetteId = tbInCst.Text;
            project.OutputCassetteId = tbOutCst.Text;
            project.ColletModelNum = tbColletM.Text;
            project.ColletLotNum = tbColletL.Text;
            project.XmlPath = tbXml.Text;
            return project;
        }

        private void SaveMachineRecipe(string recipeName)
        {
            try
            {
                var host = FindForm() as Form1;
                host?.SaveMachineRecipe(recipeName);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }

    internal static class Prompt
    {
        public static string Show(string question, string defaultValue = "")
        {
            using (var form = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowIcon = false,
                ClientSize = new Size(420, 130),
                Text = question
            })
            {
                var label = new Label { Location = new Point(12, 12), AutoSize = true, Text = question };
                var textBox = new TextBox { Location = new Point(12, 40), Size = new Size(396, 24), Text = defaultValue };
                var ok = new Button { Location = new Point(240, 80), Size = new Size(80, 28), Text = "OK", DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
                var cancel = new Button { Location = new Point(328, 80), Size = new Size(80, 28), Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
                form.Controls.Add(label);
                form.Controls.Add(textBox);
                form.Controls.Add(ok);
                form.Controls.Add(cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;
                return form.ShowDialog() == DialogResult.OK ? textBox.Text : null;
            }
        }
    }
}


