using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.Common.Recipes;
using QMC.Vision.Modules;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// C3b-3 — 검사별 "컨트롤러/페이지 지정" 편집 패널 (노드 Setup.LightPages). "결선(채널 풀)" 개념 대체.
    /// 컨트롤러 콤보(인벤토리 PortName) + 페이지 콤보(선택 컨트롤러 PageCount) + 행 추가/삭제. 채널 레벨은 레시피(레벨 그리드).
    /// </summary>
    public partial class InspectionLightAssignPanel : UserControl
    {
        private IAlgorithmNode _node;
        private string _algorithm, _inspectionId;
        private bool _suspend;

        /// <summary>지정 변경 알림(호스트가 레벨 그리드 갱신 등에 사용).</summary>
        public event EventHandler AssignChanged;

        public InspectionLightAssignPanel()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;
            // 콤보 선택을 즉시 셀 값으로 커밋(미커밋이면 OnCellEndEdit/Save 가 구 값 0 을 읽는 버그 방지).
            _grid.CurrentCellDirtyStateChanged += OnGridCurrentCellDirtyStateChanged;
        }

        private void OnGridCurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        /// <summary>검사 노드 컨텍스트 주입(호스트가 참조/GetAlgorithm 으로 해석).</summary>
        public void SelectInspection(IAlgorithmNode node, string algorithm, string inspectionId)
        {
            _node = node;
            _algorithm = algorithm;
            _inspectionId = inspectionId;
            _lblHeader.Text = "검사 조명 지정 — " + InspectionLabel.Get(algorithm, inspectionId)
                            + "  (" + VisionAlgorithm.Label(algorithm) + " / " + inspectionId + ")";
            BindFields();
        }

        // ── 이벤트 핸들러 (Designer named) ──
        private void OnSaveClick(object sender, EventArgs e) => PersistAssign(true);
        private void OnGridDataError(object sender, DataGridViewDataErrorEventArgs e) => e.ThrowException = false;
        private void OnCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_suspend) return;
            // (페이지 0 강제 리셋 제거 — 실 UI 에서 페이지 선택 뒤 이 리셋이 발화해 0 으로 떨어지던 버그.
            //  컨트롤러별 PageCount 초과 페이지는 PersistAssign 의 클램프가 보정.)
            PersistAssign(false);
        }
        private void OnRowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            if (_suspend) return;
            PersistAssign(false);
        }

        /// <summary>페이지 콤보 = 선택 컨트롤러의 PageCount(편집 컨트롤만 좁힘 — 셀 Items 비조작).</summary>
        private void OnEditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (_grid.CurrentCell == null) return;
            if (_grid.Columns[_grid.CurrentCell.ColumnIndex].Name != "Page") return;
            var combo = e.Control as ComboBox;
            if (combo == null) return;
            string port = _grid.Rows[_grid.CurrentCell.RowIndex].Cells["ControllerPort"].Value as string;
            var ce = LightSystemSetupStore.Current?.GetController(port);
            int pageCount = (ce != null && ce.PageCount > 0) ? ce.PageCount : 1;
            combo.Items.Clear();
            for (int p = 0; p < pageCount; p++) combo.Items.Add(p);
        }

        // ── 바인딩 ──
        private void BindFields()
        {
            if (_node == null)
            {
                _grid.Rows.Clear();
                _grid.Enabled = false;
                _btnSave.Enabled = false;
                SetStatus("설정 불러올 수 없음 — 검사 노드 미해결", true);
                System.Diagnostics.Debug.WriteLine("[InspectionLightAssignPanel] 노드 미해결: " + _algorithm + "/" + _inspectionId);
                return;
            }
            _grid.Enabled = true;
            _btnSave.Enabled = true;
            RefreshCombos();

            var setup = _node.Setup as AlgoSetupBase;
            var pages = setup?.LightPages ?? new List<LightPageRef>();

            _suspend = true;
            try
            {
                _grid.Rows.Clear();
                foreach (var pr in pages)
                {
                    EnsureCtrlItem(pr.ControllerPort);
                    int idx = _grid.Rows.Add();
                    _grid.Rows[idx].Cells["ControllerPort"].Value = pr.ControllerPort;
                    _grid.Rows[idx].Cells["Page"].Value = pr.Page;
                }
            }
            finally { _suspend = false; }
            SetStatus(pages.Count == 0 ? "지정 없음 — 행을 추가해 컨트롤러/페이지를 지정하세요." : "", false);
        }

        /// <summary>컨트롤러 콤보 = 인벤토리 PortName, 페이지 콤보 = 0 ~ (전 컨트롤러 최대 PageCount-1) superset.</summary>
        private void RefreshCombos()
        {
            _colCtrl.Items.Clear();
            int maxPage = 1;
            var ctrls = LightSystemSetupStore.Current?.Controllers ?? new List<LightControllerEntry>();
            foreach (var c in ctrls)
            {
                if (!string.IsNullOrEmpty(c.PortName)) _colCtrl.Items.Add(c.PortName);
                if (c.PageCount > maxPage) maxPage = c.PageCount;
            }
            _colPage.Items.Clear();
            for (int p = 0; p < maxPage; p++) _colPage.Items.Add(p);
        }

        private void EnsureCtrlItem(string port)
        {
            if (string.IsNullOrEmpty(port)) return;
            if (!_colCtrl.Items.Contains(port)) _colCtrl.Items.Add(port);
        }

        // ── 저장 ──
        private void PersistAssign(bool explicitSave)
        {
            var setup = _node?.Setup as AlgoSetupBase;
            if (setup == null) { SetStatus("저장 불가 — 검사 노드 미해결", true); return; }

            // 진행 중 콤보 편집을 셀 값으로 확정(미커밋 방지) 후 수집.
            if (_grid.IsCurrentCellInEditMode && _grid.CurrentCell != null
                && _grid.EditingControl is ComboBox ec && ec.SelectedItem != null)
                try { _grid.CurrentCell.Value = ec.SelectedItem; } catch { }
            try { _grid.EndEdit(); } catch { }

            var list = new List<LightPageRef>();
            foreach (DataGridViewRow r in _grid.Rows)
            {
                if (r.IsNewRow) continue;
                string port = r.Cells["ControllerPort"].Value as string;
                if (string.IsNullOrEmpty(port)) continue;
                int page = 0;
                int.TryParse(r.Cells["Page"].Value?.ToString(), out page);
                var ce = LightSystemSetupStore.Current?.GetController(port);
                if (page < 0) page = 0;
                if (ce != null && ce.PageCount > 0 && page > ce.PageCount - 1) page = ce.PageCount - 1;   // 컨트롤러 PageCount 초과만 보정(ce 없으면 콤보 신뢰)
                // 중복 (port,page) 제거
                if (!list.Any(x => string.Equals(x.ControllerPort, port, StringComparison.OrdinalIgnoreCase) && x.Page == page))
                    list.Add(new LightPageRef { ControllerPort = port, Page = page });
            }
            setup.LightPages = list;
            try { _node.SaveSettings(); }
            catch (Exception ex) { SetStatus("저장 예외: " + ex.Message, true); return; }
            if (explicitSave) SetStatus($"저장 완료 — 노드 [{_node.StorageKey}] 지정 {list.Count}건", false);
            try { AssignChanged?.Invoke(this, EventArgs.Empty); } catch { }
        }

        private void SetStatus(string msg, bool err)
        { _lblStatus.ForeColor = err ? Color.Firebrick : Color.DarkSlateGray; _lblStatus.Text = msg; }
    }
}
