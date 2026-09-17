using System;
using System.Globalization;
using System.Linq;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class MapCreatePage
    {
        private RecipeMapPreview _preview;
        private string _previewError;

        private void ConfigureRegisteredMapPreview()
        {
            _mapView.FitCellTextToCell = true;
            _mapView.KeepOverlaysVisible = true;
            _mapView.CellTextResolver = entry => entry.SequenceNo > 0
                ? entry.SequenceNo.ToString(CultureInfo.InvariantCulture) : "";
            _mapView.CellStatusResolver = entry => _preview != null && _preview.IsFilteredOut(entry)
                ? "SKIP (BIN 필터)" : entry.IsTarget ? "대상" : "SKIP";
            _mapView.LegendItemsResolver = () => new[]
            {
                Tuple.Create("대상 · 숫자는 공정 순서", WaferMapPalette.Wait),
                Tuple.Create("SKIP", WaferMapPalette.Skip)
            };
        }

        private void RefreshRegisteredMapPreview()
        {
            _preview = null;
            _previewError = null;
            if (_map == null)
            {
                _mapView.SetMap(null, false);
                return;
            }

            try
            {
                if (_project == null) throw new InvalidOperationException("Recipe가 로드되지 않았습니다.");
                WaferMapProcessSettings settings = _isOutputMap ? _project.OutputMapProcessing : _project.InputMapProcessing;
                PickupSubset pickup = _isOutputMap ? (_project.OutputPickup ?? _project.Pickup) : (_project.InputPickup ?? _project.Pickup);
                var bins = _isOutputMap ? null : RecipeMapPreviewService.LoadSavedInputBins(_project.FileName);
                _preview = RecipeMapPreviewService.Create(_map, settings, pickup, CurrentMapKind, bins);
                // 동일 등록 맵의 SKIP/설정 갱신은 원본 주소와 UID로 선택을 이어 간다.
                _mapView.SetMap(_preview.Map, false, true);
            }
            catch (Exception ex)
            {
                _previewError = ex.Message;
                _mapView.SetMap(null, false);
                QMC.Common.Log.Write("Main", "RECIPE", "MapCreatePreview",
                    "등록 맵 공정 미리보기 실패. recipe=" + (_project != null ? _project.FileName : "-") +
                    ", role=" + CurrentMapKind + ", reason=" + ex.Message + " - Failed");
            }
        }

        private string GetPreviewSummary()
        {
            if (!string.IsNullOrWhiteSpace(_previewError)) return "미리보기 확인 필요: " + _previewError;
            if (_preview == null) return "등록 맵 없음";
            DieMap map = _preview.Map;
            WaferMapProcessSettings settings = map.ProcessTransform.Settings ?? new WaferMapProcessSettings();
            DieMapEntry selected = _mapView.SelectedEntry ?? map.Entries
                .Where(entry => entry.IsTarget && entry.SequenceNo > 0).OrderBy(entry => entry.SequenceNo).FirstOrDefault();
            string selectedText = selected == null ? "공정 대상 없음" :
                WaferMapProcessService.FormatMapPosition(selected) + " | 공정 " +
                (selected.SequenceNo > 0 ? "#" + selected.SequenceNo : "-") + " | " + _mapView.CellStatusResolver(selected);
            string[] origins = { "좌상단", "좌하단", "우상단", "우하단", "센터" };
            return "적용 기준: 회전 " + settings.RotationDegrees + "° / 원점 " + origins[(int)settings.GridOrigin] +
                Environment.NewLine + selectedText;
        }
    }
}
