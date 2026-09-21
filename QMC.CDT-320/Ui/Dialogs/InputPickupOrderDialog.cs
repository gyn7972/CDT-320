using QMC.CDT_320.Ui.Localization;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui.Common.WaferMaps;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>Review 화면에서 여는 순서 설정 창. Apply는 사본을 반환하며 CONFIRM/모션/저장을 실행하지 않는다.</summary>
    public sealed partial class InputPickupOrderDialog : Form, ILocalizedView
    {
        private string _waferId;
        private PickupOrderDraft _draft;
        private bool _synchronizing;
        private int _selectedIndex;
        private int _renderedPage = -1;

        public PickupOrderDraft Draft { get { return _draft; } }

        public InputPickupOrderDialog()
        {
            InitializeComponent();
            Lang.BindKey(this, "dialog.pickup.title");
            Lang.BindKey(lblTitle, "dialog.pickup.heading");
            Lang.BindKey(lblCornerCaption, "dialog.pickup.corner");
            Lang.BindKey(lblDirectionCaption, "dialog.pickup.direction");
            Lang.BindKey(lblPatternCaption, "dialog.pickup.pattern");
            Lang.BindKey(btnCornerStart, "dialog.pickup.cornerStart");
            Lang.BindKey(btnFit, "dialog.pickup.fit");
            Lang.BindKey(btnFocus, "dialog.pickup.focus");
            Lang.BindKey(lblListTitle, "dialog.pickup.list");
            Lang.BindKey(btnSetStart, "dialog.pickup.setStart");
            Lang.BindKey(btnFirst, "dialog.pickup.first");
            Lang.BindKey(btnLast, "dialog.pickup.last");
            Lang.BindKey(lblSequenceCaption, "dialog.pickup.sequence");
            Lang.BindKey(btnJump, "dialog.pickup.jump");
            Lang.BindKey(lblPlaybackNotice, "dialog.pickup.notice");
            Lang.BindKey(btnApply, "dialog.pickup.apply");
            Lang.BindKey(btnClose, "dialog.close");
            Lang.BindKey(btnPrevious, "common.prev");
            Lang.BindKey(btnNext, "common.next");
            Lang.BindKey(btnPagePrevious, "common.prev");
            Lang.BindKey(btnPageNext, "common.next");
            foreach (ComboBox combo in new[] { cmbCorner, cmbDirection, cmbPattern, cmbVisibleCount })
            {
                combo.DrawMode = DrawMode.OwnerDrawFixed;
                combo.DrawItem += cmbDisplay_DrawItem;
            }
            gridOrder.CellFormatting += gridOrder_CellFormatting;
            ApplyLanguage();
            Load += (sender, e) => Lang.Apply(this);
        }

        public InputPickupOrderDialog(PickupOrderDraft draft, string waferId, string selectedDieUid) : this()
        {
            _draft = draft ?? throw new ArgumentNullException("draft");
            _waferId = waferId;
            lblWafer.Text = Lang.Format("dialog.pickup.wafer", string.IsNullOrWhiteSpace(_waferId) ? "-" : _waferId);
            _synchronizing = true;
            PickupSubset options = draft.Options;
            cmbCorner.SelectedIndex = options.StartCorner == PickupStartCorner.TopRight ? 0 :
                options.StartCorner == PickupStartCorner.TopLeft ? 1 : options.StartCorner == PickupStartCorner.BottomRight ? 2 : 3;
            cmbDirection.SelectedIndex = options.Direction == PickupDirection.Vertical ? 0 : 1;
            cmbPattern.SelectedIndex = options.Pattern == PickupPattern.ZigZag ? 0 : 1;
            cmbVisibleCount.SelectedIndex = 0;
            cmbSpeed.SelectedIndex = 0;
            _synchronizing = false;
            cmbCorner.Enabled = cmbDirection.Enabled = cmbPattern.Enabled = !draft.IsReadOnly;
            btnApply.Enabled = !draft.IsReadOnly;
            mapRoute.SetDraft(draft);
            int selected = draft.Order.ToList().FindIndex(e => string.Equals(e.DieUid, selectedDieUid, StringComparison.OrdinalIgnoreCase));
            SelectSequence(Math.Max(0, selected), true);
        }

        private void cmbPickupOption_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_synchronizing || _draft == null || _draft.IsReadOnly) return;
            RunUiAction(delegate
            {
                _draft.SetOptions(new PickupSubset
                {
                    StartCorner = cmbCorner.SelectedIndex == 0 ? PickupStartCorner.TopRight :
                        cmbCorner.SelectedIndex == 1 ? PickupStartCorner.TopLeft :
                        cmbCorner.SelectedIndex == 2 ? PickupStartCorner.BottomRight : PickupStartCorner.BottomLeft,
                    Direction = cmbDirection.SelectedIndex == 0 ? PickupDirection.Vertical : PickupDirection.Horizontal,
                    Pattern = cmbPattern.SelectedIndex == 0 ? PickupPattern.ZigZag : PickupPattern.Straight
                });
                ResetRouteView();
            });
        }

        private void btnSetStart_Click(object sender, EventArgs e)
        {
            RunUiAction(delegate
            {
                if (_draft == null || _draft.Order.Count == 0) return;
                _draft.SetStartDie(_draft.Order[_selectedIndex].DieUid);
                ResetRouteView();
            });
        }

        private void btnCornerStart_Click(object sender, EventArgs e)
        {
            RunUiAction(delegate { if (_draft != null) { _draft.SetStartDie(string.Empty); ResetRouteView(); } });
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            RunUiAction(delegate
            {
                if (_draft == null || _draft.IsReadOnly) return;
                StopPlayback();
                DialogResult = DialogResult.OK;
                Close();
            });
        }

        private void btnClose_Click(object sender, EventArgs e) { StopPlayback(); DialogResult = DialogResult.Cancel; Close(); }
        private void btnFirst_Click(object sender, EventArgs e) { StopPlayback(); SelectSequence(0, true); }
        private void btnPrevious_Click(object sender, EventArgs e) { StopPlayback(); SelectSequence(_selectedIndex - 1, true); }
        private void btnNext_Click(object sender, EventArgs e) { StopPlayback(); SelectSequence(_selectedIndex + 1, true); }
        private void btnLast_Click(object sender, EventArgs e) { StopPlayback(); SelectSequence(Count - 1, true); }
        private void btnPagePrevious_Click(object sender, EventArgs e) { StopPlayback(); SelectSequence(_selectedIndex / 10 * 10 - 10, true); }
        private void btnPageNext_Click(object sender, EventArgs e) { StopPlayback(); SelectSequence(_selectedIndex / 10 * 10 + 10, true); }
        private void btnJump_Click(object sender, EventArgs e) { StopPlayback(); SelectSequence((int)numSequence.Value - 1, true); }
        private void numSequence_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            btnJump_Click(sender, EventArgs.Empty);
        }
        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (timerPlayback.Enabled) { StopPlayback(); return; }
            if (Count == 0) return;
            if (_selectedIndex == Count - 1) SelectSequence(0, true);
            timerPlayback.Start(); btnPlay.Text = Lang.T("dialog.pickup.pause");
        }
        private void timerPlayback_Tick(object sender, EventArgs e)
        {
            if (_draft == null || _selectedIndex >= Count - 1 || Disposing || IsDisposed) { StopPlayback(); return; }
            SelectSequence(_selectedIndex + 1, true);
            if (_selectedIndex == Count - 1) StopPlayback();
        }
        private void cmbSpeed_SelectedIndexChanged(object sender, EventArgs e) { timerPlayback.Interval = cmbSpeed.SelectedIndex == 2 ? 225 : cmbSpeed.SelectedIndex == 1 ? 450 : 900; }
        private void trackSequence_ValueChanged(object sender, EventArgs e) { if (!_synchronizing) { StopPlayback(); SelectSequence(trackSequence.Value - 1, true); } }
        private void cmbVisibleCount_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_synchronizing) return;
            mapRoute.VisibleCount = cmbVisibleCount.SelectedIndex == 2 ? 0 : cmbVisibleCount.SelectedIndex == 1 ? 50 : 20;
            RefreshMapCaption();
        }
        private void btnZoomIn_Click(object sender, EventArgs e) { mapRoute.ZoomBy(1.25F); }
        private void btnZoomOut_Click(object sender, EventArgs e) { mapRoute.ZoomBy(.8F); }
        private void btnFit_Click(object sender, EventArgs e) { mapRoute.FitMap(); }
        private void btnFocus_Click(object sender, EventArgs e) { mapRoute.ZoomSelection(); }
        private void mapRoute_ViewChanged(object sender, EventArgs e) { RefreshMapCaption(); }
        private void mapRoute_SelectedSequenceChanged(object sender, EventArgs e) { StopPlayback(); SelectSequence(mapRoute.SelectedIndex, false); }
        private void gridOrder_SelectionChanged(object sender, EventArgs e)
        {
            if (_synchronizing || gridOrder.CurrentRow == null || !(gridOrder.CurrentRow.Tag is int)) return;
            StopPlayback(); SelectSequence((int)gridOrder.CurrentRow.Tag, true);
        }
        private void InputPickupOrderDialog_FormClosed(object sender, FormClosedEventArgs e) { StopPlayback(); }

        private int Count { get { return _draft == null ? 0 : _draft.Order.Count; } }
        private void StopPlayback() { timerPlayback.Stop(); btnPlay.Text = Lang.T("dialog.pickup.play"); }

        private void ResetRouteView()
        {
            StopPlayback();
            _renderedPage = -1;
            mapRoute.SetDraft(_draft);
            SelectSequence(0, true);
        }

        private void SelectSequence(int index, bool follow)
        {
            _selectedIndex = Count == 0 ? 0 : Math.Max(0, Math.Min(Count - 1, index));
            mapRoute.SelectSequence(_selectedIndex, follow);
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            _synchronizing = true;
            try
            {
                int page = _selectedIndex / 10 * 10;
                if (_renderedPage != page)
                {
                    gridOrder.Rows.Clear();
                    for (int i = page; i < Math.Min(Count, page + 10); i++)
                    {
                        DieMapEntry entry = _draft.Order[i];
                        int row = gridOrder.Rows.Add(i + 1, WaferMapProcessService.FormatMapCoordinate(entry.LogicalGridX), WaferMapProcessService.FormatMapCoordinate(entry.LogicalGridY), i == 0 ? "START" : i == Count - 1 ? "END" : "WAIT");
                        gridOrder.Rows[row].Tag = i;
                    }
                    _renderedPage = page;
                }
                gridOrder.ClearSelection();
                if (Count > 0)
                {
                    int row = _selectedIndex - page;
                    gridOrder.CurrentCell = gridOrder.Rows[row].Cells[0];
                    gridOrder.Rows[row].Selected = true;
                }
                trackSequence.Maximum = Math.Max(1, Count); numSequence.Maximum = Math.Max(1, Count);
                trackSequence.Value = Count > 0 ? _selectedIndex + 1 : 1;
                numSequence.Value = trackSequence.Value;
                trackSequence.Enabled = numSequence.Enabled = btnJump.Enabled = Count > 0;
                btnFirst.Enabled = btnPrevious.Enabled = _selectedIndex > 0;
                btnLast.Enabled = btnNext.Enabled = Count > 0 && _selectedIndex < Count - 1;
                btnPlay.Enabled = Count > 0;
                btnPagePrevious.Enabled = page > 0;
                btnPageNext.Enabled = page + 10 < Count;
                btnSetStart.Enabled = _draft != null && !_draft.IsReadOnly && Count > 0 && _selectedIndex != 0;
                btnCornerStart.Enabled = _draft != null && !_draft.IsReadOnly && !string.IsNullOrWhiteSpace(_draft.StartDieUid);
                RefreshSelectionCaptions(page);
            }
            finally { _synchronizing = false; }
        }

        public void ApplyLanguage()
        {
            lblWafer.Text = Lang.Format("dialog.pickup.wafer", string.IsNullOrWhiteSpace(_waferId) ? "-" : _waferId);
            btnPlay.Text = Lang.T(timerPlayback.Enabled ? "dialog.pickup.pause" : "dialog.pickup.play");
            colSequence.HeaderText = Lang.T("dialog.pickup.sequence");
            colMapX.HeaderText = Lang.T("dialog.pickup.mapX");
            colMapY.HeaderText = Lang.T("dialog.pickup.mapY");
            colState.HeaderText = Lang.T("dialog.pickup.state");
            foreach (ComboBox combo in new[] { cmbCorner, cmbDirection, cmbPattern, cmbVisibleCount })
                combo.Invalidate();
            gridOrder.Invalidate();
            RefreshSelectionCaptions(_selectedIndex / 10 * 10);
        }

        private void cmbDisplay_DrawItem(object sender, DrawItemEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null || e.Index < 0 || e.Index >= combo.Items.Count) return;
            string[] keys = combo == cmbCorner ? new[] { "topRight", "topLeft", "bottomRight", "bottomLeft" } :
                combo == cmbDirection ? new[] { "vertical", "horizontal" } :
                combo == cmbPattern ? new[] { "zigzag", "straight" } : new[] { "near20", "near50", "all" };
            string text = e.Index < keys.Length ? Lang.T("dialog.pickup." + keys[e.Index]) : combo.Items[e.Index].ToString();
            e.DrawBackground();
            TextRenderer.DrawText(e.Graphics, text, e.Font, e.Bounds, e.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.DrawFocusRectangle();
        }

        private void gridOrder_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex != colState.Index || !(e.Value is string)) return;
            string state = (string)e.Value;
            string key = state == "START" ? "dialog.pickup.start" : state == "END" ? "dialog.pickup.end" :
                state == "WAIT" ? "dialog.pickup.wait" : null;
            if (key == null) return;
            e.Value = Lang.T(key);
            e.FormattingApplied = true;
        }

        private void RefreshSelectionCaptions(int page)
        {
            DieMapEntry selected = Count > 0 ? _draft.Order[_selectedIndex] : null;
            DieMapEntry start = Count > 0 ? _draft.Order[0] : null;
            lblSequence.Text = selected == null ? "-" : (_selectedIndex + 1).ToString("D3");
            lblSelectedMap.Text = selected == null ? Lang.T("dialog.pickup.empty") : WaferMapProcessService.FormatMapPosition(selected);
            lblPosition.Text = selected == null ? "X - / Y -" : "X " + selected.PosX.ToString("F3", CultureInfo.InvariantCulture) + "    Y " + selected.PosY.ToString("F3", CultureInfo.InvariantCulture) + " mm";
            lblStart.Text = start == null ? Lang.T("dialog.pickup.noStart") : Lang.Format("dialog.pickup.startAt", WaferMapProcessService.FormatMapPosition(start));
            lblPage.Text = Count == 0 ? "0 / 0" : (page + 1) + "–" + Math.Min(Count, page + 10) + " / " + Count.ToString("N0");
            lblTotal.Text = "/ " + Count.ToString("N0");
            lblStatus.Text = _draft != null && _draft.IsReadOnly ? Lang.T("dialog.pickup.readOnly") :
                Lang.Format("dialog.pickup.summary", Count.ToString("N0"));
            lblLegend.Text = Lang.T("dialog.pickup.legend") +
                (_draft != null && _draft.WrapAfterIndex >= 0 ? Lang.T("dialog.pickup.wrap") : "");
            RefreshMapCaption();
        }

        private void RefreshMapCaption()
        {
            int first, end; mapRoute.GetVisibleRange(out first, out end);
            lblMapRange.Text = Lang.Format("dialog.pickup.range", end == 0 ? 0 : first + 1, end, Count.ToString("N0"));
            lblZoom.Text = Math.Round(mapRoute.Zoom * 100) + "%";
        }

        private void RunUiAction(Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                StopPlayback();
                QMC.Common.Log.Write("Main", "UI", "InputPickupOrderDialog", "픽업 순서 설정 실패: " + ex + " - Failed");
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("dialog.pickup.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
