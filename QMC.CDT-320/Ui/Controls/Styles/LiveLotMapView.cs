using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui.Pages;
using QMC.CDT_320.Ui.Common.WaferMaps;
using QMC.Common;

namespace QMC.CDT_320.Ui.Controls
{
    public enum LiveLotMapSourceKind
    {
        Input = 0,
        OutputGood = 1,
        OutputNg = 2
    }

    /// <summary>
    /// Input/Output WaferMap 표시 전용 뷰.
    /// Material 이벤트는 dirty 표시만 하고, 단일 Worker가 일관된 표시 사본을 만든다.
    /// UI Timer는 완료된 사본만 적용하며, Paint에서 Material 조회나 전역 쓰기를 하지 않는다.
    /// </summary>
    public class LiveLotMapView : QMC.CDT320.Ui.Controls.DieMapView
    {
        private const int RefreshIntervalMs = 100;
        private System.Windows.Forms.Timer _refresh;
        private int _gridX = 5;
        private int _gridY = 5;
        private LiveLotMapSourceKind _sourceKind = LiveLotMapSourceKind.Input;
        private int _dirty = 1;
        private bool _eventsHooked;
        private int _viewGeneration;
        private Task<MapDisplaySnapshot> _pendingRefresh;
        private DateTime _nextRefreshUtc = DateTime.MinValue;
        private string _lastRefreshError = "";

        // UI 스레드에서만 교체한다. Worker는 컨트롤과 이 필드에 접근하지 않는다.
        private DieMap _displayMap;
        private Dictionary<string, WaferMapCellState> _displayStates =
            new Dictionary<string, WaferMapCellState>(StringComparer.Ordinal);
        private Dictionary<string, string> _displayTrackingTexts =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private string _displayWaferKey = "";
        private long _signature = long.MinValue;

        // 부드러운 모던 팔레트 — 회색 기계 룩 대신 밝은 뉴트럴 + 은은한 테두리/아웃라인.
        protected override Color MapBorderColor => WaferMapPalette.WaferOutline;
        protected override float MapBorderWidth => 2f;          // 얇은 1px 대신 또렷한 2px 프레임
        protected override int MapBorderInset => 3;             // 가장자리에서 3px 들여써 카드처럼 분리
        protected override string OverlayFontFamily => "맑은 고딕";
        protected override bool ShowTechnicalInfoLine => false;   // pitch/zoom 등 기술 라인 숨김

        public LiveLotMapView()
        {
            BackColor = Color.FromArgb(0xF6, 0xF8, 0xFA);
            // 현재 기준: 작업 메인도 Input/Output 전환 화면과 같은 DieMapView 렌더러를 사용한다.
            CompactUsedBounds = true;
            ShowWaferOutline = true;
            ShowEquipmentAxes = true;
            // SKIP도 Input/Output 전환 화면과 동일하게 표시한다.
            EntryVisibilityPredicate = entry => entry != null;
            CellColorResolver = ResolveLiveEntryColor;
            CellStatusResolver = ResolveLiveEntryStatusText;
            CellTextResolver = entry => "";
            LegendItemsResolver = WaferMapDisplayStyle.BuildLegend;
        }

        /// <summary>그리드 크기 — 외부에서 Recipe.Frame.GridX 로 설정 가능.</summary>
        public int GridX { get { return _gridX; } set { _gridX = Math.Max(1, value); MarkDirty(); } }
        public int GridY { get { return _gridY; } set { _gridY = Math.Max(1, value); MarkDirty(); } }

        public LiveLotMapSourceKind SourceKind
        {
            get { return _sourceKind; }
            set
            {
                if (_sourceKind == value)
                    return;

                _sourceKind = value;
                _viewGeneration++;
                ClearDisplay();
                Caption = ResolveMapTitle(_sourceKind) + "   -";
                MarkDirty();
            }
        }

        private void HookStateEvents()
        {
            if (_eventsHooked)
                return;

            MaterialStateService.StateChanged += OnMaterialStateChanged;
            LotStorage.ActiveLotChanged += OnActiveLotChanged;
            _eventsHooked = true;
        }

        private void UnhookStateEvents()
        {
            if (!_eventsHooked)
                return;

            MaterialStateService.StateChanged -= OnMaterialStateChanged;
            LotStorage.ActiveLotChanged -= OnActiveLotChanged;
            _eventsHooked = false;
        }

        // ─── 이벤트: 비-UI 스레드에서 호출될 수 있으므로 dirty flag 만 세운다. UI 접근 금지. ───
        // (시그널 전용 계약 — 라이브 상태 객체는 전달되지 않으며, 데이터는 이후 tick에서 락을 잡는 API로 읽는다.)
        private void OnMaterialStateChanged()
        {
            MarkDirty();
        }

        private void OnActiveLotChanged(Lot lot)
        {
            MarkDirty();
        }

        private void MarkDirty()
        {
            Interlocked.Exchange(ref _dirty, 1);
        }

        private void OnRefreshTick(object sender, EventArgs e)
        {
            if (!PageBase.ShouldRefreshVisible(this))
                return;

            try
            {
                if (_pendingRefresh != null)
                {
                    // UI에서 완료를 기다리지 않는다. 진행 중 변경은 dirty 하나로 합친다.
                    if (!_pendingRefresh.IsCompleted)
                        return;

                    Task<MapDisplaySnapshot> completed = _pendingRefresh;
                    _pendingRefresh = null;
                    MapDisplaySnapshot snapshot = completed.GetAwaiter().GetResult();
                    if (snapshot.Generation == _viewGeneration && snapshot.SourceKind == _sourceKind)
                        ApplyDisplaySnapshot(snapshot);
                }

                if (DateTime.UtcNow < _nextRefreshUtc || Interlocked.Exchange(ref _dirty, 0) == 0)
                    return;

                LiveLotMapSourceKind sourceKind = _sourceKind;
                int generation = _viewGeneration;
                _pendingRefresh = Task.Run(() => BuildDisplaySnapshot(sourceKind, generation));
            }
            catch (Exception ex)
            {
                ShowRefreshFailure(ex.ToString());
            }
        }

        private void ApplyDisplaySnapshot(MapDisplaySnapshot snapshot)
        {
            if (!string.IsNullOrEmpty(snapshot.Error))
            {
                ShowRefreshFailure(snapshot.Error);
                return;
            }

            _lastRefreshError = "";
            _nextRefreshUtc = DateTime.MinValue;
            if (snapshot.Signature == _signature)
                return;

            bool sameWafer = snapshot.HasWaferInstance &&
                !string.IsNullOrEmpty(_displayWaferKey) &&
                string.Equals(_displayWaferKey, snapshot.WaferKey, StringComparison.OrdinalIgnoreCase) &&
                _displayMap != null && snapshot.Map != null;

            _signature = snapshot.Signature;
            _displayMap = snapshot.Map;
            _displayStates = snapshot.States;
            _displayTrackingTexts = snapshot.TrackingTexts;
            _displayWaferKey = snapshot.WaferKey;
            Caption = ResolveMapTitle(snapshot.SourceKind) + "   " + snapshot.WaferId;

            // 같은 wafer만 선택을 새 entry에 연결한다. 교체/소실 시 이전 선택을 남기지 않는다.
            SetMap(_displayMap, !sameWafer, sameWafer);
        }

        private void ShowRefreshFailure(string error)
        {
            ClearDisplay();
            Caption = ResolveMapTitle(_sourceKind) + "   표시 갱신 실패";
            if (!string.Equals(_lastRefreshError, error, StringComparison.Ordinal))
            {
                Log.Write("Main", "SYSTEM", "LiveLotMapView",
                    "WaferMap 표시 갱신에 실패했습니다. source=" + _sourceKind + ", error=" + error + " - Failed");
                _lastRefreshError = error;
            }
            _nextRefreshUtc = DateTime.UtcNow.AddSeconds(2);
            MarkDirty();
        }

        private void ClearDisplay()
        {
            _displayMap = null;
            _displayStates = new Dictionary<string, WaferMapCellState>(StringComparer.Ordinal);
            _displayTrackingTexts = new Dictionary<string, string>(StringComparer.Ordinal);
            _displayWaferKey = "";
            _signature = long.MinValue;
            SetMap(null, true);
        }

        private static string ResolveMapTitle(LiveLotMapSourceKind sourceKind)
        {
            switch (sourceKind)
            {
                case LiveLotMapSourceKind.OutputGood: return "OUTPUT GOOD MAP";
                case LiveLotMapSourceKind.OutputNg: return "OUTPUT NG MAP";
                default: return "INPUT WAFER MAP";
            }
        }

        private static MaterialLocationKind ResolveStageLocation(LiveLotMapSourceKind sourceKind)
        {
            switch (sourceKind)
            {
                case LiveLotMapSourceKind.OutputGood: return MaterialLocationKind.OutputStageGood;
                case LiveLotMapSourceKind.OutputNg: return MaterialLocationKind.OutputStageNg;
                default: return MaterialLocationKind.InputStage;
            }
        }

        private static MapDisplaySnapshot BuildDisplaySnapshot(LiveLotMapSourceKind sourceKind, int generation)
        {
            var snapshot = new MapDisplaySnapshot { SourceKind = sourceKind, Generation = generation };
            try
            {
                // 맵/상태/wafer identity를 같은 읽기 구간에서 얻어 서로 다른 시점을 섞지 않는다.
                // 기존 Material 맵 생성 API의 lock은 유지하며 UI만 대기에서 분리한다.
                MaterialStateService.ReadState(state =>
                {
                    WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(ResolveStageLocation(sourceKind));
                    if (wafer == null)
                        return;

                    snapshot.WaferId = string.IsNullOrWhiteSpace(wafer.WaferId) ? "-" : wafer.WaferId;
                    snapshot.HasWaferInstance = !string.IsNullOrWhiteSpace(wafer.WaferInstanceId);
                    snapshot.WaferKey = sourceKind + ":" + (snapshot.HasWaferInstance
                        ? wafer.WaferInstanceId.Trim()
                        : snapshot.WaferId + ":" + wafer.CreatedAt.Ticks);

                    if (sourceKind == LiveLotMapSourceKind.Input)
                    {
                        string reason;
                        if (!wafer.HasInputStageDieMappingResult ||
                            !MaterialStateService.IsStoredInputStageResultModeUsable(wafer, true, out reason))
                            return;

                        snapshot.Map = MaterialStateService.BuildDieMapFromWafer(wafer);
                        FillInputDisplayStates(state, snapshot.Map, wafer, snapshot.States, snapshot.TrackingTexts);
                    }
                    else
                    {
                        snapshot.Map = MaterialStateService.BuildOutputReceiveDieMapFromWafer(wafer);
                        FillOutputDisplayStates(snapshot.Map, wafer, snapshot.States);
                    }
                });

                // 서명 계산은 전역 lock 밖, UI 스레드 밖에서 수행한다.
                snapshot.Signature = ComputeSignature(snapshot);
            }
            catch (Exception ex)
            {
                // Dispose/숨김 중에도 faulted Task가 남지 않도록 오류 역시 결과 사본에 담는다.
                snapshot.Error = ex.ToString();
            }
            return snapshot;
        }

        // 아래 두 메서드는 ReadState 안에서만 호출한다. 원본 Material은 수정하지 않는다.
        private static void FillInputDisplayStates(
            MaterialSnapshot state,
            DieMap display,
            WaferMaterial inputWafer,
            Dictionary<string, WaferMapCellState> states,
            Dictionary<string, string> trackingTexts)
        {
            if (state == null || state.Dies == null || display == null || display.Entries == null || inputWafer == null)
                return;

            var dieById = new Dictionary<string, DieMaterial>(StringComparer.OrdinalIgnoreCase);
            ISet<string> ownedDieIds = inputWafer.DieIds == null ? null :
                new HashSet<string>(inputWafer.DieIds, StringComparer.OrdinalIgnoreCase);
            foreach (DieMaterial die in state.Dies)
            {
                if (!WaferMapDisplayStyle.CanMatchInputDie(die, inputWafer, ownedDieIds))
                    continue;

                // 중복 UID에서 임의로 첫 항목/최신 항목을 고르지 않고 상태 미확인으로 표시한다.
                if (dieById.ContainsKey(die.DieId))
                    dieById[die.DieId] = null;
                else
                    dieById.Add(die.DieId, die);
            }

            foreach (DieMapEntry entry in display.Entries)
            {
                if (entry == null)
                    continue;

                DieMaterial die;
                if (string.IsNullOrWhiteSpace(entry.DieUid) || !dieById.TryGetValue(entry.DieUid, out die) ||
                    die == null || die.Wafer_IndexX != entry.DieMapX || die.Wafer_IndexY != entry.DieMapY)
                {
                    states[BuildEntryGridKey(entry)] = entry.IsTarget ? WaferMapCellState.Unknown : WaferMapCellState.Skip;
                    continue;
                }

                // Normalize의 SKIP 보정값도 화면에서는 Material 원값을 표시한다.
                // 검사 NG색을 만들기 위해 Result/BIN을 변조하지 않는다.
                entry.IsTarget = die.IsInputTarget;
                entry.Result = die.Result;
                entry.BinCode = die.Input_BinCode;
                states[BuildEntryGridKey(entry)] = WaferMapDisplayStyle.ResolveInputState(
                    die.IsInputTarget, die.Result, IsInputDieOnPicker(die), HasInputPickVisionInspection(die));
                MaterialLocation location = die.CurrentLocation;
                trackingTexts[BuildEntryGridKey(entry)] = WaferMapDisplayStyle.GetTrackingText(
                    location != null ? location.Kind : MaterialLocationKind.Unknown,
                    location != null ? location.PickerNo : -1,
                    die.ReservedPickerLocation, die.ReservedPickerNo);
            }
        }

        private static void FillOutputDisplayStates(
            DieMap display,
            WaferMaterial outputWafer,
            Dictionary<string, WaferMapCellState> states)
        {
            if (display == null || display.Entries == null || outputWafer.OutputReceiveSlots == null)
                return;

            var slotByGrid = new Dictionary<string, OutputReceiveSlotMaterial>(StringComparer.Ordinal);
            foreach (OutputReceiveSlotMaterial slot in outputWafer.OutputReceiveSlots)
            {
                if (slot != null)
                    slotByGrid[BuildGridKey(slot.DieMapX, slot.DieMapY)] = slot;
            }

            foreach (DieMapEntry entry in display.Entries)
            {
                if (entry == null)
                    continue;

                OutputReceiveSlotMaterial slot;
                if (!slotByGrid.TryGetValue(BuildEntryGridKey(entry), out slot))
                {
                    states[BuildEntryGridKey(entry)] = entry.IsTarget ? WaferMapCellState.Unknown : WaferMapCellState.Skip;
                    continue;
                }

                // Normalize가 Index와 빈 UID를 바꾸므로 같은 wafer의 local grid로 슬롯을 연결한다.
                entry.Index = slot.OrderIndex;
                entry.SequenceNo = slot.SequenceNo;
                entry.IsTarget = slot.IsTarget;
                entry.Result = slot.Result;
                entry.BinCode = slot.BinCode;
                entry.DieUid = slot.DieUid ?? "";
                bool hasDie = !string.IsNullOrWhiteSpace(slot.DieUid) || !string.IsNullOrWhiteSpace(slot.SourceDieUid);
                states[BuildEntryGridKey(entry)] = WaferMapDisplayStyle.ResolveOutputState(
                    slot.IsTarget, slot.Result, hasDie, slot.IsOutputInspectionDone, slot.IsOutputInspectionOk);
            }
        }

        private static string BuildGridKey(int x, int y)
        {
            return x.ToString() + ":" + y.ToString();
        }

        private static string BuildEntryGridKey(DieMapEntry entry)
        {
            return BuildGridKey(DieMapGenerator.ResolveMapIndexX(entry), DieMapGenerator.ResolveMapIndexY(entry));
        }

        private static long ComputeSignature(MapDisplaySnapshot snapshot)
        {
            unchecked
            {
                long h = 17;
                h = h * 31 + (int)snapshot.SourceKind;
                h = h * 31 + BuildStringHash(snapshot.WaferKey);
                h = h * 31 + BuildStringHash(snapshot.WaferId);
                DieMap map = snapshot.Map;
                if (map == null || map.Entries == null)
                    return h * 31 - 1;

                h = h * 31 + BuildStringHash(map.FrameObjId);
                h = h * 31 + map.DieMapX;
                h = h * 31 + map.DieMapY;
                h = h * 31 + map.PitchX.GetHashCode();
                h = h * 31 + map.PitchY.GetHashCode();
                h = h * 31 + map.DieSizeX.GetHashCode();
                h = h * 31 + map.DieSizeY.GetHashCode();
                h = h * 31 + map.OuterDiameterMm.GetHashCode();
                h = h * 31 + map.OriginX.GetHashCode();
                h = h * 31 + map.OriginY.GetHashCode();
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null)
                        continue;
                    h = h * 31 + BuildStringHash(entry.DieUid);
                    h = h * 31 + entry.Index;
                    h = h * 31 + entry.SequenceNo;
                    h = h * 31 + (entry.IsTarget ? 1 : 0);
                    h = h * 31 + (int)entry.Result;
                    h = h * 31 + entry.BinCode;
                    h = h * 31 + entry.DieMapX;
                    h = h * 31 + entry.DieMapY;
                    h = h * 31 + entry.OriginalMapX;
                    h = h * 31 + entry.OriginalMapY;
                    h = h * 31 + entry.EquipmentGridX.GetHashCode();
                    h = h * 31 + entry.EquipmentGridY.GetHashCode();
                    h = h * 31 + entry.PosX.GetHashCode();
                    h = h * 31 + entry.PosY.GetHashCode();
                    WaferMapCellState state;
                    if (snapshot.States.TryGetValue(BuildEntryGridKey(entry), out state))
                        h = h * 31 + (int)state;
                    string trackingText;
                    if (snapshot.TrackingTexts.TryGetValue(BuildEntryGridKey(entry), out trackingText))
                        h = h * 31 + BuildStringHash(trackingText);
                }
                return h;
            }
        }

        private static int BuildStringHash(string value)
        {
            unchecked
            {
                if (string.IsNullOrEmpty(value))
                    return 0;

                int hash = 17;
                for (int i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return hash;
            }
        }

        private Color ResolveLiveEntryColor(DieMapEntry entry)
        {
            return WaferMapDisplayStyle.GetColor(ResolveEntryState(entry));
        }

        private string ResolveLiveEntryStatusText(DieMapEntry entry)
        {
            string stateText = WaferMapDisplayStyle.GetText(ResolveEntryState(entry));
            string trackingText;
            if (entry != null && _displayTrackingTexts.TryGetValue(BuildEntryGridKey(entry), out trackingText) &&
                !string.IsNullOrWhiteSpace(trackingText))
                return stateText + " / " + trackingText;
            return stateText;
        }

        private WaferMapCellState ResolveEntryState(DieMapEntry entry)
        {
            if (entry == null)
                return WaferMapCellState.Unknown;
            if (!entry.IsTarget)
                return WaferMapCellState.Skip;

            WaferMapCellState state;
            return _displayStates.TryGetValue(BuildEntryGridKey(entry), out state) ? state : WaferMapCellState.Unknown;
        }

        private static bool IsInputDieOnPicker(DieMaterial die)
        {
            if (die == null)
                return false;

            return die.CurrentLocation != null &&
                   (die.CurrentLocation.Kind == MaterialLocationKind.PickerFront ||
                    die.CurrentLocation.Kind == MaterialLocationKind.PickerRear);
        }

        private static bool HasInputPickVisionInspection(DieMaterial die)
        {
            if (die == null || die.Inspections == null)
                return false;

            foreach (DieInspectionRecord record in die.Inspections)
            {
                if (record == null)
                    continue;

                if (string.Equals(record.InspectionType, "InputPickVision", StringComparison.OrdinalIgnoreCase) &&
                    record.Result != MaterialInspectionResult.Unknown)
                    return true;
            }

            return false;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            // 핸들 재생성은 최종 Dispose가 아니다. 이전 작업만 무효화하고 Timer는 재사용한다.
            _viewGeneration++;
            _refresh?.Stop();
            base.OnHandleDestroyed(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (IsDesignerView())
                return;

            HookStateEvents();
            if (_refresh == null)
            {
                _refresh = new System.Windows.Forms.Timer { Interval = RefreshIntervalMs };
                _refresh.Tick += OnRefreshTick;
            }
            UpdateRefreshTimer();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateRefreshTimer();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            UpdateRefreshTimer();
        }

        private void UpdateRefreshTimer()
        {
            if (_refresh == null || IsDisposed || Disposing)
                return;

            if (PageBase.ShouldRefreshVisible(this))
            {
                MarkDirty();
                _refresh.Start();
            }
            else
            {
                _refresh.Stop();
            }
        }

        private bool IsDesignerView()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime || DesignMode)
                return true;

            for (Control current = this; current != null; current = current.Parent)
            {
                if (current.Site != null && current.Site.DesignMode)
                    return true;
            }
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _viewGeneration++;
                UnhookStateEvents();
                if (_refresh != null)
                {
                    _refresh.Stop();
                    _refresh.Tick -= OnRefreshTick;
                    _refresh.Dispose();
                    _refresh = null;
                }
                // Worker는 static 함수이며 UI 콜백이 없어서 disposed 컨트롤에 접근하지 않는다.
                _pendingRefresh = null;
            }
            base.Dispose(disposing);
        }

        private sealed class MapDisplaySnapshot
        {
            public LiveLotMapSourceKind SourceKind;
            public int Generation;
            public DieMap Map;
            public readonly Dictionary<string, WaferMapCellState> States =
                new Dictionary<string, WaferMapCellState>(StringComparer.Ordinal);
            public readonly Dictionary<string, string> TrackingTexts =
                new Dictionary<string, string>(StringComparer.Ordinal);
            public string WaferId = "-";
            public string WaferKey = "";
            public bool HasWaferInstance;
            public long Signature;
            public string Error = "";
        }
    }
}
