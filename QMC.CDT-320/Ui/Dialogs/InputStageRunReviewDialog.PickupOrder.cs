using QMC.CDT_320.Ui.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui.Common.WaferMaps;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class InputStageRunReviewDialog
    {
        private InputPickupOrderDialog _pickupOrderDialog;
        private bool _synchronizingPickupOptions;

        private void ShowPickupOrderViewer()
        {
            if (!CanReviewPickupOrder() || _pickupOrderDialog != null)
                return;
            try
            {
                DieMap source = _dieMap;
                string signature = BuildDieGridSignature(BuildDieGridEntries());
                var draft = new PickupOrderDraft(source, BuildPickupOptions(), StartDieUid,
                    OrderedDieIds, _readOnlyPreview);
                using (var viewer = new InputPickupOrderDialog(draft, _waferId,
                    _selectedDie != null ? _selectedDie.DieUid : string.Empty))
                {
                    _pickupOrderDialog = viewer;
                    // 사본만 편집하므로 확인/취소, 확대, 재생은 기존 승인 상태와 장비에 영향을 주지 않는다.
                    if (viewer.ShowDialog(this) != DialogResult.OK)
                        return;
                    if (!CanReviewPickupOrder() || _readOnlyPreview)
                        return;
                    if (!ReferenceEquals(source, _dieMap) ||
                        !string.Equals(signature, BuildDieGridSignature(BuildDieGridEntries()), StringComparison.Ordinal))
                        throw new InvalidOperationException("순서 확인 중 Die Map이 변경되었습니다. 창을 다시 열어 확인하세요.");
                    ApplyPickupOrderDraft(draft);
                }
            }
            catch (Exception ex)
            {
                LogReviewBlocked("PICKUP-VIEWER", "픽업 순서 확인/적용 실패: " + ex);
                SetStatus("픽업 순서 확인/적용 실패: " + ex.Message);
                if (!IsDisposed && !_sequenceCloseRequested)
                    QMC.Common.MessageDialog.Show(this, ex.Message, AdditionalDialogText.Display("픽업 순서 확인"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _pickupOrderDialog = null;
            }
        }

        private void ApplyPickupOrderDraft(PickupOrderDraft draft)
        {
            if (!CanReviewPickupOrder() || _readOnlyPreview || draft == null || draft.IsReadOnly)
                throw new InvalidOperationException("현재 Review 상태에서는 픽업 순서를 적용할 수 없습니다.");

            List<DieMapEntry> resolved;
            string reason;
            string[] ids = draft.Order.Select(entry => entry.DieUid).ToArray();
            if (!PickupOrderDraft.TryResolveOrder(_dieMap, ids, out resolved, out reason))
                throw new InvalidOperationException(reason);
            PickupSubset options = draft.Options;
            List<DieMapEntry> generated = PickupOrderDraft.RotateAtStart(
                PickupOrderDraft.BuildBaseOrder(_dieMap, options), draft.StartDieUid);
            if (!generated.Select(entry => entry.DieUid).SequenceEqual(ids, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("설정으로 생성한 순서와 뷰어 순서가 다릅니다. 다시 확인하세요.");

            _pickupOrderApplied = false;
            _synchronizingPickupOptions = true;
            try
            {
                SetPickupOptions(options);
                _startDie = string.IsNullOrWhiteSpace(draft.StartDieUid) ? null :
                    resolved.First(entry => string.Equals(entry.DieUid, draft.StartDieUid, StringComparison.OrdinalIgnoreCase));
                chkUseSelectedStart.Checked = _startDie != null;
            }
            finally
            {
                _synchronizingPickupOptions = false;
            }

            RefreshPickupPreview();
            if (!OrderedDieIds.SequenceEqual(ids, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Review에 반영된 순서가 뷰어와 다릅니다. 자동 시작 전에 다시 확인하세요.");
            if (_startDie != null)
                SelectDie(_startDie, true);
            RefreshSelectedDieInformation();
            // 기존 Draft 적용 이벤트/로그를 재사용한다. 최종 저장과 자동 재개는 기존 CONFIRM 경로가 담당한다.
            BtnApplyPickupOrder_Click(btnApplyPickupOrder, EventArgs.Empty);
        }

        private bool CanReviewPickupOrder()
        {
            return !IsDisposed && !Disposing && !_decisionSubmitted && !_reviewStopPending &&
                (!_busy || _waferVisionControlActive) && !_waferVisionMoveBusy && _activeJogButton == null &&
                _mappingComplete && _dieMap != null;
        }

        private void ClosePickupOrderViewer()
        {
            if (_pickupOrderDialog == null || _pickupOrderDialog.IsDisposed)
                return;
            _pickupOrderDialog.DialogResult = DialogResult.Cancel;
            _pickupOrderDialog.Close();
        }
    }
}
