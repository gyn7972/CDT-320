using System;
using System.Collections.Generic;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320.Ajin;
using QMC.Common.IO;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Catalog-driven I/O list page.</summary>
    public partial class IoListPage : PageBase
    {
        private readonly string _i18nKey;
        private readonly string[] _columns;
        private readonly Func<string[][]> _rowProvider;
        private int _stateColumnIndex = -1;
        private AjinIoScanService _subscribedService;

        public IoListPage(string i18nKey, string[] columns, string[][] seedRows)
            : this(i18nKey, columns, () => seedRows)
        {
        }

        public IoListPage(string i18nKey, string[] columns, Func<string[][]> rowProvider)
        {
            _i18nKey = i18nKey;
            _columns = columns;
            _rowProvider = rowProvider ?? (() => new string[0][]);

            InitializeComponent();
            ApplyRuntimeUi();
            WireEvents();
            BuildColumns();
            LoadRows();

            HandleCreated += (s, e) => SubscribeIoScan();
            HandleDestroyed += (s, e) => UnsubscribeIoScan();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T(_i18nKey);
            lblHeader.Tag = "i18n:" + _i18nKey;
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;

            lblSubHeader.Text = Lang.T(_i18nKey) + " LIST - AjinIoCatalog";
            lblSubHeader.Tag = "i18n:" + _i18nKey;
            lblSubHeader.BackColor = UiTheme.StatusBarBg;
            lblSubHeader.ForeColor = System.Drawing.Color.White;
            lblSubHeader.Font = UiTheme.SectionFont;
        }

        private void WireEvents()
        {
            btnSave.Visible = false;
            btnReload.Text = "REFRESH";
            btnReload.Click += (s, e) => LoadRows();
            btnAddRow.Visible = false;
        }

        private void BuildColumns()
        {
            _grid.Columns.Clear();
            foreach (var column in _columns)
                _grid.Columns.Add(column, column);
            _stateColumnIndex = Array.FindIndex(_columns, c => string.Equals(c, "STATE", StringComparison.OrdinalIgnoreCase));
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.ReadOnly = true;
        }

        private void LoadRows()
        {
            _grid.Rows.Clear();
            var rows = _rowProvider();
            if (rows == null) return;
            foreach (var row in rows)
                _grid.Rows.Add(row);
        }

        private void SubscribeIoScan()
        {
            UnsubscribeIoScan();
            if (_stateColumnIndex < 0) return;

            _subscribedService = AjinIoScanService.Current;
            if (_subscribedService != null)
                _subscribedService.IoStatusUpdated += OnIoStatusUpdated;
        }

        private void UnsubscribeIoScan()
        {
            if (_subscribedService != null)
                _subscribedService.IoStatusUpdated -= OnIoStatusUpdated;
            _subscribedService = null;
        }

        private void OnIoStatusUpdated(AjinIoSnapshot snapshot)
        {
            if (snapshot == null || IsDisposed || _stateColumnIndex < 0) return;

            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<AjinIoSnapshot>(OnIoStatusUpdated), snapshot); } catch { }
                return;
            }

            UpdateStateCells(snapshot);
        }

        private void UpdateStateCells(AjinIoSnapshot snapshot)
        {
            if (string.Equals(_i18nKey, "set.cylinder", StringComparison.OrdinalIgnoreCase))
            {
                UpdateCylinderRows(snapshot);
                return;
            }

            string address = snapshot.IsOutput
                ? AjinIoCatalog.OutputAddress(snapshot.Module, snapshot.Bit)
                : AjinIoCatalog.InputAddress(snapshot.Module, snapshot.Bit);
            string state = snapshot.IsOn ? "ON" : "OFF";

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                if (!RowMatches(row, snapshot.Name, address)) continue;
                var cell = row.Cells[_stateColumnIndex];
                if (!string.Equals(Convert.ToString(cell.Value), state, StringComparison.OrdinalIgnoreCase))
                    cell.Value = state;
            }
        }

        private void UpdateCylinderRows(AjinIoSnapshot snapshot)
        {
            if (snapshot.IsOutput) return;

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow || row.Cells.Count < 2) continue;
                string name = Convert.ToString(row.Cells[1].Value);
                var cyl = AjinIoCatalog.FindCylinder(name);
                if (cyl == null) continue;
                if (!SamePoint(snapshot, cyl.InFwd) && !SamePoint(snapshot, cyl.InBwd)) continue;

                string state = CylinderState(cyl);
                var cell = row.Cells[_stateColumnIndex];
                if (!string.Equals(Convert.ToString(cell.Value), state, StringComparison.OrdinalIgnoreCase))
                    cell.Value = state;
            }
        }

        private static bool RowMatches(DataGridViewRow row, string name, string address)
        {
            for (int i = 0; i < row.Cells.Count; i++)
            {
                string value = Convert.ToString(row.Cells[i].Value);
                if (string.Equals(value, name, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(value, address, StringComparison.OrdinalIgnoreCase)) return true;
                if (!string.IsNullOrEmpty(value) &&
                    value.IndexOf(address + " ", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static bool SamePoint(AjinIoSnapshot snapshot, DioDefault item)
        {
            return item != null && snapshot.Module == item.Module && snapshot.Bit == item.Bit;
        }

        private static string CylinderState(CylinderDefault item)
        {
            bool fwd = IsOn(item.InFwd);
            bool bwd = IsOn(item.InBwd);
            if (fwd && !bwd) return "FWD";
            if (!fwd && bwd) return "BWD";
            if (fwd && bwd) return "BOTH";
            return "OFF";
        }

        private static bool IsOn(DioDefault item)
        {
            var service = AjinIoScanService.Current;
            if (service == null || item == null) return false;
            var snapshot = service.GetLatest(item.Module, item.Bit, false);
            return snapshot != null && snapshot.ErrorCode == 0 && snapshot.IsOn;
        }
    }
}
