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
        private bool _loadingCategories;

        private void InitializeLanguageBindings()
        {
            Lang.BindKey(lblSearch, "settingsUi.caption.search");
            Lang.BindKey(lblCategory, "settingsUi.caption.category");
            Lang.BindKey(btnReload, "settingsUi.caption.reloadJson");
            Lang.BindKey(btnSave, "settingsUi.caption.save");
            Lang.BindKey(dataGridViewTextBoxColumn1, "settingsUi.caption.code");
            Lang.BindKey(dataGridViewTextBoxColumn2, "settingsUi.caption.category2");
            Lang.BindKey(dataGridViewTextBoxColumn3, "settingsUi.caption.severity");
            Lang.BindKey(dataGridViewTextBoxColumn4, "settingsUi.caption.title");
            Lang.BindKey(dataGridViewTextBoxColumn5, "settingsUi.caption.cause");
            Lang.BindKey(dataGridViewTextBoxColumn6, "settingsUi.caption.action2");
            Load += (sender, args) => Lang.Apply(this);
        }

        public AlarmMasterPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
            InitializeLanguageBindings();
            LoadCategoryItems();
            if (!IsDesignerMode()) LoadGrid();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("settings.alarmMaster");
            lblHeader.Tag = "i18n:settings.alarmMaster";
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);

            filterLayout.Margin = Padding.Empty;
            filterLayout.Padding = Padding.Empty;

            AlignFilterLabel(lblSearch);
            AlignFilterLabel(lblCategory);
            AlignFilterInput(_tbFilter);
            AlignFilterInput(_cbCategory);
            _lblCount.BorderStyle = BorderStyle.None;
            _lblCount.Dock = DockStyle.Fill;
            _lblCount.Margin = new Padding(2);
            _lblCount.TextAlign = ContentAlignment.MiddleCenter;

            // 모던 플랫 버튼 — 공용 스타일러(ApplyActionControl)가 강제하던 회색 대신 적용. 스타일러 이후라 런타임에 확실히 반영되고, Designer에도 같은 색을 넣어 미리보기를 맞춘다.
            StyleModernButton(btnReload, Color.FromArgb(71, 85, 105), Color.FromArgb(100, 116, 139), Color.FromArgb(51, 65, 85));
            StyleModernButton(btnSave, Color.FromArgb(34, 139, 84), Color.FromArgb(46, 160, 98), Color.FromArgb(27, 115, 68));
        }

        // 모던 플랫 버튼: 테두리 없음 + hover/press 색. (Designer 프리뷰용으로 .Designer.cs에도 동일 색을 박아둠)
        private static void StyleModernButton(Button b, Color back, Color hover, Color down)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.FlatAppearance.MouseDownBackColor = down;
            b.BackColor = back;
            b.ForeColor = Color.White;
            b.UseVisualStyleBackColor = false;
            b.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            b.TextAlign = ContentAlignment.MiddleCenter;
            b.Dock = DockStyle.Fill;
            b.Margin = new Padding(2);
            b.Cursor = Cursors.Hand;
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

        private void LoadCategoryItems()
        {
            _loadingCategories = true;
            try
            {
                _cbCategory.Items.Clear();
                _cbCategory.Items.Add("(All)");
                foreach (var category in Enum.GetNames(typeof(AlarmCategory)))
                    _cbCategory.Items.Add(category);
                _cbCategory.SelectedIndex = 0;
            }
            finally
            {
                _loadingCategories = false;
            }
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

        private void _tbFilter_TextChanged(object sender, EventArgs e)
        {
            LoadGrid();
        }

        private void _cbCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingCategories) return;
            LoadGrid();
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            AlarmMaster.Load();
            LoadGrid();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            AlarmMaster.Save();
            QMC.Common.MessageDialog.Show(Lang.Format("settingsUi.alarm.saved", AlarmMaster.Path_), Lang.T("settingsUi.caption.alarmmaster"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void _grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            CommitRow(e.RowIndex);
        }
    }
}
