using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Alarm master editor.</summary>
    public partial class AlarmMasterPage : PageBase
    {
        public AlarmMasterPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
            WireEvents();
            LoadCategoryItems();
            if (!IsDesignerMode()) LoadGrid();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("settings.alarmMaster");
            lblHeader.Tag = "i18n:settings.alarmMaster";
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);

            if (rootLayout.RowStyles.Count >= 3)
            {
                rootLayout.RowStyles[0].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[0].Height = 30F;
                rootLayout.RowStyles[1].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[1].Height = 34F;
                rootLayout.RowStyles[2].SizeType = SizeType.Percent;
                rootLayout.RowStyles[2].Height = 100F;
            }

            filterLayout.Margin = Padding.Empty;
            filterLayout.Padding = Padding.Empty;
            filterLayout.ColumnStyles.Clear();
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AlignFilterLabel(lblSearch);
            AlignFilterLabel(lblCategory);
            AlignFilterInput(_tbFilter);
            AlignFilterInput(_cbCategory);
            _lblCount.BorderStyle = BorderStyle.None;
            _lblCount.Dock = DockStyle.Fill;
            _lblCount.Margin = new Padding(2);
            _lblCount.TextAlign = ContentAlignment.MiddleCenter;

            SettingsPageLayoutStyler.ApplyActionControl(btnReload);
            SettingsPageLayoutStyler.ApplyActionControl(btnSave);
        }

        private static void AlignFilterLabel(Label label)
        {
            if (label == null)
                return;

            label.Dock = DockStyle.Fill;
            label.Margin = new Padding(2);
            label.Padding = Padding.Empty;
            label.TextAlign = ContentAlignment.MiddleCenter;
        }

        private static void AlignFilterInput(Control control)
        {
            if (control == null)
                return;

            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(2, 5, 2, 5);
        }

        private void WireEvents()
        {
            _tbFilter.TextChanged += (s, e) => LoadGrid();
            _cbCategory.SelectedIndexChanged += (s, e) => LoadGrid();
            btnReload.Click += (s, e) =>
            {
                AlarmMaster.Load();
                LoadGrid();
            };
            btnSave.Click += (s, e) =>
            {
                AlarmMaster.Save();
                QMC.Common.MessageDialog.Show("Saved: " + AlarmMaster.Path_, "AlarmMaster", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            _grid.CellEndEdit += (s, e) => CommitRow(e.RowIndex);
        }

        private void LoadCategoryItems()
        {
            _cbCategory.Items.Clear();
            _cbCategory.Items.Add("(All)");
            foreach (var category in Enum.GetNames(typeof(AlarmCategory)))
                _cbCategory.Items.Add(category);
            _cbCategory.SelectedIndex = 0;
        }

        private void LoadGrid()
        {
            _grid.Rows.Clear();
            string filter = (_tbFilter?.Text ?? string.Empty).Trim().ToLowerInvariant();
            string category = _cbCategory?.SelectedItem?.ToString() ?? "(All)";
            int count = 0;

            foreach (var definition in AlarmMaster.ByCode.Values.OrderBy(x => x.Code))
            {
                if (category != "(All)" && definition.Category.ToString() != category) continue;
                if (!string.IsNullOrEmpty(filter)
                    && (definition.Code ?? string.Empty).ToLowerInvariant().IndexOf(filter) < 0
                    && (definition.Title ?? string.Empty).ToLowerInvariant().IndexOf(filter) < 0)
                    continue;

                _grid.Rows.Add(definition.Code, definition.Category, definition.DefaultSeverity,
                    definition.Title, definition.Cause, definition.Action);
                count++;
            }

            _lblCount.Text = "(" + count + ")";
        }

        private void CommitRow(int rowIdx)
        {
            if (rowIdx < 0 || rowIdx >= _grid.Rows.Count) return;
            DataGridViewRow row = _grid.Rows[rowIdx];
            string code = row.Cells[0].Value as string;
            if (string.IsNullOrEmpty(code)) return;

            var definition = AlarmMaster.Get(code);
            if (definition == null) return;

            try
            {
                AlarmCategory category;
                AlarmSeverity severity;
                if (Enum.TryParse(row.Cells[1].Value?.ToString(), out category)) definition.Category = category;
                if (Enum.TryParse(row.Cells[2].Value?.ToString(), out severity)) definition.DefaultSeverity = severity;
                definition.Title = row.Cells[3].Value as string ?? definition.Title;
                definition.Cause = row.Cells[4].Value as string ?? definition.Cause;
                definition.Action = row.Cells[5].Value as string ?? definition.Action;
            }
            catch { }
        }
    }
}


