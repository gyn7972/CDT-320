using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Bin;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui.Pages;

namespace QMC.CDT_320.Ui.Controls
{
    public enum LiveLotMapSourceKind
    {
        Input = 0,
        OutputGood = 1,
        OutputNg = 2
    }

    /// <summary>
    /// 작업 메인 화면 우상단 "Input Wafer Map" 라이브 뷰.
    /// <list type="bullet">
    ///   <item><description>표시 기준은 LotStorage.ActiveInputDieMap → 없으면 InputStage Wafer 복원.</description></item>
    ///   <item><description>OnPaint 는 캐시된 맵만 그린다. 무거운 복원/Normalize/전역 쓰기를 Paint 에서 하지 않는다.</description></item>
    ///   <item><description>MaterialStateService.StateChanged / LotStorage.ActiveLotChanged 로 dirty flag 만 세우고,
    ///   100ms UI Timer 가 signature 비교 후 변경분만 Invalidate 한다.</description></item>
    /// </list>
    /// </summary>
    public class LiveLotMapView : QMC.CDT320.Ui.Controls.DieMapView
    {
        private const int RefreshIntervalMs = 100;
        private static readonly Color InspectionWaitColor = Color.FromArgb(0xCC, 0xDD, 0xEE);
        private static readonly Color InspectionDoneColor = Color.FromArgb(0xF2, 0xC1, 0x4E);
        private static readonly Color PickCompleteColor = Color.FromArgb(0x24, 0xB8, 0x6A);

        private System.Windows.Forms.Timer _refresh;
        private int _gridX = 5;
        private int _gridY = 5;
        private LiveLotMapSourceKind _sourceKind = LiveLotMapSourceKind.Input;

        // 이벤트(비-UI 스레드)는 이 플래그만 세운다. 실제 반영은 UI Timer 에서 수행.
        private int _dirty = 1;
        private bool _eventsHooked;

        // UI Timer 가 갱신하는 표시 캐시 (OnPaint 는 이 값만 사용).
        private DieMap _displayMap;
        private Dictionary<string, LiveDieMapCellState> _displayStates =
            new Dictionary<string, LiveDieMapCellState>(StringComparer.Ordinal);
        private MapStats _stats;
        private string _lotText = "(no active lot)";
        private long _signature = long.MinValue;

        // 부드러운 모던 팔레트 — 회색 기계 룩 대신 밝은 뉴트럴 + 은은한 테두리/아웃라인.
        protected override Color MapBorderColor => Color.FromArgb(0x8F, 0x9C, 0xAD);
        protected override float MapBorderWidth => 2f;          // 얇은 1px 대신 또렷한 2px 프레임
        protected override int MapBorderInset => 3;             // 가장자리에서 3px 들여써 카드처럼 분리
        protected override Color WaferOutlineColor => Color.FromArgb(0xB4, 0xC4, 0xD8);
        protected override string OverlayFontFamily => "맑은 고딕";
        protected override bool ShowTechnicalInfoLine => false;   // pitch/zoom 등 기술 라인 숨김

        public LiveLotMapView()
        {
            BackColor = Color.FromArgb(0xF6, 0xF8, 0xFA);
            // 현재 기준: 작업 메인도 Input/Output 전환 화면과 같은 DieMapView 렌더러를 사용한다.
            CompactUsedBounds = true;
            ShowWaferOutline = true;
            ShowEquipmentAxes = true;
            EntryVisibilityPredicate = entry => entry != null && entry.IsTarget;
            CellColorResolver = ResolveLiveEntryColor;
            CellStatusResolver = ResolveLiveEntryStatusText;
            CellTextResolver = entry => "";
            LegendItemsResolver = BuildLiveLegendItems;

            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                HookStateEvents();

                _refresh = new System.Windows.Forms.Timer { Interval = RefreshIntervalMs };
                _refresh.Tick += OnRefreshTick;
            }
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
                _displayMap = null;
                _displayStates = new Dictionary<string, LiveDieMapCellState>(StringComparer.Ordinal);
                _signature = long.MinValue;
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
            try
            {
                if (!PageBase.ShouldRefreshVisible(this))
                    return;

                // 변경 이벤트(Material/Lot)가 없으면 아무 일도 하지 않는다. (idle tick 비용 0)
                // → 큰 다이맵(수천 개)을 매 100ms 마다 스캔/재계산하던 UI 스레드 부하 제거.
                bool dirtyEvent = Interlocked.Exchange(ref _dirty, 0) == 1;
                if (!dirtyEvent)
                    return;

                EnsureDisplayMap(true);

                long sig = ComputeSignature(_displayMap, _displayStates);
                if (sig == _signature)
                    return;

                _signature = sig;
                _stats = CalculateMapStats(_displayMap, _displayStates);
                string productionLotId = MaterialStateService.GetProductionLotId();
                _lotText = string.IsNullOrWhiteSpace(productionLotId) ? "(no lot)" : productionLotId;
                // 현재 기준: 작업 메인도 공통 DieMapView에 상태 캡션과 맵 데이터를 전달한다.
                Caption = BuildCaption(_displayMap, _stats);
                SetMap(_displayMap, false);
                Invalidate();
            }
            catch
            {
                // 표시 갱신 중 일시적 실패는 무시한다.
            }
        }

        private string BuildCaption(DieMap map, MapStats stats)
        {
            if (map == null)
                return string.Format("{0}   LOT {1}  (no map)", ResolveMapTitle(), _lotText);

            string doneText = IsOutputMapSource() ? "place" : "pick";
            return string.Format("{0}   LOT {1}  target={2}  wait={3}  vision={4}  {5}={6}  good={7}  ng={8}",
                ResolveMapTitle(), _lotText, stats.Target, stats.InspectionWait, stats.InspectionDone,
                doneText, stats.PickComplete, stats.Good, stats.Ng);
        }

        private string ResolveMapTitle()
        {
            switch (_sourceKind)
            {
                case LiveLotMapSourceKind.OutputGood:
                    return "OUTPUT GOOD MAP";
                case LiveLotMapSourceKind.OutputNg:
                    return "OUTPUT NG MAP";
                default:
                    return "INPUT WAFER MAP";
            }
        }

        private bool IsOutputMapSource()
        {
            return _sourceKind == LiveLotMapSourceKind.OutputGood ||
                   _sourceKind == LiveLotMapSourceKind.OutputNg;
        }

        /// <summary>
        /// 표시용 맵 참조를 필요할 때만 갱신한다.<br/>
        /// ActiveInputDieMap 이 있으면 그것을 쓰고(새 참조일 때만 1회 Normalize),
        /// 없으면 이벤트/최초 1회에 한해 InputStage Wafer 에서 복원한다(매 Tick 재구성 금지).
        /// </summary>
        private void EnsureDisplayMap(bool dirtyEvent)
        {
            if (IsOutputMapSource())
            {
                EnsureOutputDisplayMap();
                return;
            }

            WaferMaterial stageWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (stageWafer != null)
            {
                DieMap stageMap = null;
                if (stageWafer.HasInputStageDieMappingResult)
                {
                    string resultModeReason;
                    if (MaterialStateService.IsStoredInputStageResultModeUsable(
                            stageWafer,
                            true,
                            out resultModeReason))
                    {
                        stageMap = MaterialStateService.BuildDieMapFromWafer(stageWafer);
                    }
                }

                if (stageMap != null)
                {
                    DieMapGenerator.Normalize(stageMap);
                    LotStorage.ActiveInputDieMap = stageMap;
                    Dictionary<string, LiveDieMapCellState> stageStates;
                    _displayMap = BuildDisplayMapFromMaterialState(stageMap, stageWafer, out stageStates);
                    _displayStates = stageStates;
                    return;
                }

                // 현재 기준: Stage wafer가 아직 맵핑 전이면 이전 wafer map을 표시하지 않는다.
                LotStorage.ActiveInputDieMap = null;
                _displayMap = null;
                _displayStates = new Dictionary<string, LiveDieMapCellState>(StringComparer.Ordinal);
                return;
            }

            // 현재 기준: Stage wafer가 없으면 live input map은 비운다.
            LotStorage.ActiveInputDieMap = null;

            if (dirtyEvent || _displayMap == null)
            {
                try
                {
                    DieMap built = MaterialStateService.BuildInputDieMapFromStageWafer();
                    if (built != null)
                    {
                        DieMapGenerator.Normalize(built);
                        LotStorage.ActiveInputDieMap = built;
                        Dictionary<string, LiveDieMapCellState> states;
                        _displayMap = BuildDisplayMapFromMaterialState(built, null, out states);
                        _displayStates = states;
                    }
                    else
                    {
                        _displayMap = null;
                        _displayStates = new Dictionary<string, LiveDieMapCellState>(StringComparer.Ordinal);
                    }
                }
                catch
                {
                    _displayMap = null;
                    _displayStates = new Dictionary<string, LiveDieMapCellState>(StringComparer.Ordinal);
                }
            }
        }

        private void EnsureOutputDisplayMap()
        {
            try
            {
                MaterialLocationKind location = _sourceKind == LiveLotMapSourceKind.OutputNg
                    ? MaterialLocationKind.OutputStageNg
                    : MaterialLocationKind.OutputStageGood;
                WaferMaterial outputWafer = MaterialStateService.GetWaferAtLocation(location);
                DieMap built = MaterialStateService.BuildOutputReceiveDieMapFromWafer(outputWafer);
                if (built == null)
                {
                    _displayMap = null;
                    _displayStates = new Dictionary<string, LiveDieMapCellState>(StringComparer.Ordinal);
                    return;
                }

                DieMapGenerator.Normalize(built);
                Dictionary<string, LiveDieMapCellState> states;
                _displayMap = BuildOutputDisplayMapFromMaterialState(built, outputWafer, out states);
                _displayStates = states;
            }
            catch
            {
                _displayMap = null;
                _displayStates = new Dictionary<string, LiveDieMapCellState>(StringComparer.Ordinal);
            }
        }

        private static DieMap BuildDisplayMapFromMaterialState(
            DieMap source,
            WaferMaterial inputWafer,
            out Dictionary<string, LiveDieMapCellState> states)
        {
            states = new Dictionary<string, LiveDieMapCellState>(StringComparer.Ordinal);
            // out 파라미터는 람다에 캡처할 수 없으므로 같은 사전을 로컬 참조로 넘긴다.
            Dictionary<string, LiveDieMapCellState> resolvedStates = states;
            DieMap display = CloneMap(source);
            if (display == null)
                return null;

            try
            {
                // [계약 보강 2026-08-07] 시퀀스가 변이 중인 State.Dies를 락 없이 순회하지 않도록
                // 상태 의존 구간 전체를 ReadState(_stateSync) 안에서 수행한다.
                // 락 밖으로 나가는 결과물(display/resolvedStates)은 클론·enum 사본이라 안전하다.
                MaterialStateService.ReadState(state =>
                    FillInputDisplayStates(state, display, inputWafer, resolvedStates));
            }
            catch
            {
            }

            return display;
        }

        // MaterialStateService.ReadState(_stateSync) 안에서 호출된다 —
        // 라이브 die 필드를 읽어 display 클론 entry와 셀 상태 사전을 채운다.
        private static void FillInputDisplayStates(
            MaterialSnapshot state,
            DieMap display,
            WaferMaterial inputWafer,
            Dictionary<string, LiveDieMapCellState> states)
        {
            if (state == null || state.Dies == null || state.Dies.Count == 0 || display.Entries == null)
                return;

            var dieById = new Dictionary<string, DieMaterial>(StringComparer.OrdinalIgnoreCase);
            var dieByGrid = new Dictionary<string, DieMaterial>(StringComparer.Ordinal);
            string waferId = inputWafer != null ? inputWafer.WaferId : null;
            foreach (DieMaterial die in state.Dies)
            {
                if (die == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(waferId) &&
                    !string.Equals(die.WaferID_Input, waferId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(die.DieId) && !dieById.ContainsKey(die.DieId))
                    dieById.Add(die.DieId, die);

                if (die.Wafer_IndexX >= 0 && die.Wafer_IndexY >= 0)
                {
                    string key = BuildGridKey(die.Wafer_IndexX, die.Wafer_IndexY);
                    if (!dieByGrid.ContainsKey(key))
                        dieByGrid.Add(key, die);
                }
            }

            foreach (DieMapEntry entry in display.Entries)
            {
                if (entry == null)
                    continue;

                DieMaterial die = null;
                if (!string.IsNullOrWhiteSpace(entry.DieUid))
                    dieById.TryGetValue(entry.DieUid, out die);
                if (die == null)
                    dieByGrid.TryGetValue(BuildEntryGridKey(entry), out die);
                if (die == null)
                {
                    states[BuildEntryGridKey(entry)] = LiveDieMapCellState.InspectionWait;
                    continue;
                }

                entry.DieUid = die.DieId ?? entry.DieUid;
                entry.IsTarget = die.IsInputTarget;
                entry.Result = die.Result;
                if (die.Input_BinCode > 0)
                    entry.BinCode = die.Input_BinCode;
                else if (die.Output_BinCode > 0)
                    entry.BinCode = die.Output_BinCode;

                states[BuildEntryGridKey(entry)] = ResolveInputDieMapCellState(die);
            }
        }

        private static DieMap BuildOutputDisplayMapFromMaterialState(
            DieMap source,
            WaferMaterial outputWafer,
            out Dictionary<string, LiveDieMapCellState> states)
        {
            states = new Dictionary<string, LiveDieMapCellState>(StringComparer.Ordinal);
            // out 파라미터는 람다에 캡처할 수 없으므로 같은 사전을 로컬 참조로 넘긴다.
            Dictionary<string, LiveDieMapCellState> resolvedStates = states;
            DieMap display = CloneMap(source);
            if (display == null)
                return null;

            try
            {
                // [계약 보강 2026-08-07] 시퀀스가 변이 중인 OutputReceiveSlots를 락 없이 순회하지 않도록
                // ReadState(_stateSync) 안에서 수행한다. (state 인자는 사용하지 않고 락 범위만 빌린다.)
                MaterialStateService.ReadState(state =>
                    FillOutputDisplayStates(display, outputWafer, resolvedStates));
            }
            catch
            {
            }

            return display;
        }

        // MaterialStateService.ReadState(_stateSync) 안에서 호출된다 —
        // 라이브 OutputReceiveSlot 필드를 읽어 display 클론 entry와 셀 상태 사전을 채운다.
        private static void FillOutputDisplayStates(
            DieMap display,
            WaferMaterial outputWafer,
            Dictionary<string, LiveDieMapCellState> states)
        {
            var slotByOrder = new Dictionary<int, OutputReceiveSlotMaterial>();
            var slotByGrid = new Dictionary<string, OutputReceiveSlotMaterial>(StringComparer.Ordinal);
            var slotByDieId = new Dictionary<string, OutputReceiveSlotMaterial>(StringComparer.OrdinalIgnoreCase);

            if (outputWafer != null && outputWafer.OutputReceiveSlots != null)
            {
                foreach (OutputReceiveSlotMaterial slot in outputWafer.OutputReceiveSlots)
                {
                    if (slot == null)
                        continue;

                    if (!slotByOrder.ContainsKey(slot.OrderIndex))
                        slotByOrder.Add(slot.OrderIndex, slot);

                    string gridKey = BuildGridKey(slot.DieMapX, slot.DieMapY);
                    if (!slotByGrid.ContainsKey(gridKey))
                        slotByGrid.Add(gridKey, slot);

                    if (!string.IsNullOrWhiteSpace(slot.DieUid) && !slotByDieId.ContainsKey(slot.DieUid))
                        slotByDieId.Add(slot.DieUid, slot);
                }
            }

            if (display.Entries == null)
                return;

            foreach (DieMapEntry entry in display.Entries)
            {
                if (entry == null)
                    continue;

                OutputReceiveSlotMaterial slot = null;
                if (!string.IsNullOrWhiteSpace(entry.DieUid))
                    slotByDieId.TryGetValue(entry.DieUid, out slot);
                if (slot == null)
                    slotByOrder.TryGetValue(entry.Index, out slot);
                if (slot == null)
                    slotByGrid.TryGetValue(BuildEntryGridKey(entry), out slot);

                if (slot != null)
                {
                    // 현재 기준: 작업 메인 Output 탭은 OutputReceiveSlot의 최신 배치/검사 상태를 그대로 표시한다.
                    entry.Index = slot.OrderIndex;
                    entry.SequenceNo = slot.SequenceNo;
                    entry.DieMapX = slot.DieMapX;
                    entry.DieMapY = slot.DieMapY;
                    entry.OriginalMapX = slot.OriginalMapX >= 0 ? slot.OriginalMapX : slot.DieMapX;
                    entry.OriginalMapY = slot.OriginalMapY >= 0 ? slot.OriginalMapY : slot.DieMapY;
                    entry.IsTarget = slot.IsTarget;
                    entry.Result = slot.Result;
                    entry.BinCode = slot.BinCode;
                    // 현재 기준: Output Vision 검사 NG는 물류 결과와 별도로 작업 맵에서 NG로 표시한다.
                    if (slot.IsOutputInspectionDone && !slot.IsOutputInspectionOk)
                    {
                        entry.Result = DieResult.NG;
                        if (entry.BinCode <= 0)
                            entry.BinCode = BinCodeMap.MaxBin;
                    }
                    entry.PosX = slot.PosX;
                    entry.PosY = slot.PosY;
                    entry.DieUid = slot.DieUid ?? entry.DieUid;
                    states[BuildEntryGridKey(entry)] = ResolveOutputDieMapCellState(slot);
                }
                else
                {
                    states[BuildEntryGridKey(entry)] = ResolveOutputDieMapCellState(entry);
                }
            }
        }

        private static DieMap CloneMap(DieMap source)
        {
            if (source == null)
                return null;

            var clone = new DieMap
            {
                FrameObjId = source.FrameObjId,
                DieMapX = source.DieMapX,
                DieMapY = source.DieMapY,
                PitchX = source.PitchX,
                PitchY = source.PitchY,
                DieSizeX = source.DieSizeX,
                DieSizeY = source.DieSizeY,
                OuterDiameterMm = source.OuterDiameterMm,
                EdgeSkipMode = source.EdgeSkipMode,
                SideEdgeSkip = source.SideEdgeSkip,
                TopBottomEdgeSkip = source.TopBottomEdgeSkip,
                OriginX = source.OriginX,
                OriginY = source.OriginY,
                SourceFileName = source.SourceFileName,
                SourceFormat = source.SourceFormat,
                SourcePitchFromFile = source.SourcePitchFromFile,
                SourceDeclaredCount = source.SourceDeclaredCount,
                SourceFirstX = source.SourceFirstX,
                SourceFirstY = source.SourceFirstY,
                SourceFirstPosX = source.SourceFirstPosX,
                SourceFirstPosY = source.SourceFirstPosY,
                CreatedAt = source.CreatedAt
            };

            if (source.Entries != null)
            {
                foreach (DieMapEntry entry in source.Entries)
                {
                    if (entry == null)
                        continue;

                    clone.Entries.Add(new DieMapEntry
                    {
                        Index = entry.Index,
                        SequenceNo = entry.SequenceNo,
                        DieMapX = entry.DieMapX,
                        DieMapY = entry.DieMapY,
                        OriginalMapX = entry.OriginalMapX,
                        OriginalMapY = entry.OriginalMapY,
                        IsTarget = entry.IsTarget,
                        Result = entry.Result,
                        BinCode = entry.BinCode,
                        PosX = entry.PosX,
                        PosY = entry.PosY,
                        EquipmentGridX = entry.EquipmentGridX,
                        EquipmentGridY = entry.EquipmentGridY,
                        DieUid = entry.DieUid
                    });
                }
            }

            return DieMapGenerator.Normalize(clone);
        }

        private static string BuildGridKey(int x, int y)
        {
            return x.ToString() + ":" + y.ToString();
        }

        private static string BuildEntryGridKey(DieMapEntry entry)
        {
            return BuildGridKey(DieMapGenerator.ResolveMapIndexX(entry), DieMapGenerator.ResolveMapIndexY(entry));
        }

        private long ComputeSignature(DieMap map, Dictionary<string, LiveDieMapCellState> states)
        {
            unchecked
            {
                long h = 17;
                h = h * 31 + (int)_sourceKind;
                Lot lot = ResolveProductionLotProjection();
                h = h * 31 + BuildStringHash(MaterialStateService.GetProductionLotId());
                h = h * 31 + (lot != null ? lot.ProcessedDies : 0);
                h = h * 31 + (lot != null ? lot.GoodCount : 0);
                h = h * 31 + (lot != null ? lot.TotalDies : 0);

                if (map != null && map.Entries != null)
                {
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
                    foreach (var entry in map.Entries)
                    {
                        if (entry == null)
                            continue;
                        h = h * 31 + BuildStringHash(entry.DieUid);
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

                        LiveDieMapCellState state;
                        if (states != null && states.TryGetValue(BuildEntryGridKey(entry), out state))
                            h = h * 31 + (int)state;
                    }
                }
                else
                {
                    h = h * 31 - 1;
                }

                return h;
            }
        }

        private static Lot ResolveProductionLotProjection()
        {
            string productionLotId = MaterialStateService.GetProductionLotId();
            Lot lot = LotStorage.ActiveLot;
            return lot != null &&
                   !string.IsNullOrWhiteSpace(productionLotId) &&
                   string.Equals(lot.LotID, productionLotId, StringComparison.Ordinal)
                ? lot
                : null;
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        private void PaintLegacyMapView(PaintEventArgs e)
        {
            // OnPaint 는 캐시된 표시 값만 그린다. (복원/Normalize/저장/전역 쓰기 금지)
            var g = e.Graphics;
            g.Clear(BackColor);

            DieMap dmap = _displayMap;
            MapStats stats = _stats;
            Dictionary<string, LiveDieMapCellState> states = _displayStates;

            using (var bf = new SolidBrush(Color.FromArgb(0x33, 0x33, 0x33)))
            using (var f = new Font("Consolas", 9F, FontStyle.Bold))
            {
                string head = dmap != null
                    ? string.Format("INPUT WAFER MAP   LOT {0}  target={1}  wait={2}  vision={3}  pick={4}  good={5}  ng={6}",
                                    _lotText, stats.Target, stats.InspectionWait, stats.InspectionDone,
                                    stats.PickComplete, stats.Good, stats.Ng)
                    : string.Format("INPUT WAFER MAP   LOT {0}  (no input die map)", _lotText);
                g.DrawString(head, f, bf, 8, 4);
            }

            DrawLegend(g);

            int gx = Math.Max(1, (dmap != null) ? dmap.DieMapX : _gridX);
            int gy = Math.Max(1, (dmap != null) ? dmap.DieMapY : _gridY);
            int margin = 8;
            int top = 42;
            int availW = Math.Max(50, Width - margin * 2);
            int availH = Math.Max(50, Height - top - margin);
            int cell = Math.Max(1, Math.Min(availW / gx, availH / gy));
            int totalW = cell * gx;
            int totalH = cell * gy;
            int x0 = (Width - totalW) / 2;
            int y0 = top + (availH - totalH) / 2;

            if (dmap != null)
            {
                if (dmap.Entries != null)
                {
                    foreach (var entry in dmap.Entries)
                    {
                        if (entry == null || !entry.IsTarget)
                            continue;

                        int x = x0 + DieMapGenerator.ResolveMapIndexX(entry) * cell;
                        int y = y0 + DieMapGenerator.ResolveMapIndexY(entry) * cell;
                        Color c = ResolveEntryColor(entry, states, IsOutputMapSource());

                        using (var br = new SolidBrush(c))
                            g.FillRectangle(br, x, y, Math.Max(1, cell - 1), Math.Max(1, cell - 1));
                        if (cell >= 4)
                        {
                            using (var pen = new Pen(Color.FromArgb(0x55, 0x55, 0x55), 1f))
                                g.DrawRectangle(pen, x, y, cell - 1, cell - 1);
                        }
                    }
                }

                using (var pen = new Pen(Color.FromArgb(0x44, 0x88, 0xCC), 1.5f))
                    g.DrawEllipse(pen, x0, y0, totalW, totalH);
            }
            else
            {
                // 입력 다이맵이 아직 없을 때만 Lot 카운트 기반 5x5 격자(레거시 fallback) 표시.
                int filled = 0;
                int goodFilled = 0;
                Lot lot = ResolveProductionLotProjection();
                int processed = lot != null ? lot.ProcessedDies : 0;
                int good = lot != null ? lot.GoodCount : 0;
                for (int j = 0; j < gy; j++)
                {
                    for (int i = 0; i < gx; i++)
                    {
                        int x = x0 + i * cell;
                        int y = y0 + j * cell;

                        Color c;
                        if (filled < processed)
                        {
                            if (goodFilled < good)
                            {
                                c = BinCodeMap.ConvertToBinCodeColor(BinCodeMap.GoodBin);
                                goodFilled++;
                            }
                            else
                            {
                                c = Color.IndianRed;
                            }
                            filled++;
                        }
                        else
                        {
                            c = Color.FromArgb(0xEE, 0xEE, 0xEE);
                        }

                        using (var br = new SolidBrush(c))
                            g.FillRectangle(br, x, y, cell - 1, cell - 1);
                        using (var pen = new Pen(Color.FromArgb(0xAA, 0xAA, 0xAA), 1f))
                            g.DrawRectangle(pen, x, y, cell - 1, cell - 1);
                    }
                }
                using (var pen = new Pen(Color.FromArgb(0x66, 0x66, 0x66), 2f))
                    g.DrawRectangle(pen, x0 - 1, y0 - 1, totalW + 1, totalH + 1);
            }

            Lot projectedLot = ResolveProductionLotProjection();
            int percentDone = dmap != null ? stats.Target : (projectedLot != null ? projectedLot.TotalDies : 0);
            int percentProcessed = dmap != null ? stats.Done : (projectedLot != null ? projectedLot.ProcessedDies : 0);
            if (percentDone > 0)
            {
                using (var bf = new SolidBrush(Color.FromArgb(0x33, 0x33, 0x33)))
                using (var f = new Font("Consolas", 9F))
                {
                    string pct = string.Format("{0:F1} %", percentProcessed * 100.0 / percentDone);
                    var sz = g.MeasureString(pct, f);
                    g.DrawString(pct, f, bf, Width - sz.Width - 8, Height - sz.Height - 4);
                }
            }
        }

        private Color ResolveLiveEntryColor(DieMapEntry entry)
        {
            return ResolveEntryColor(entry, _displayStates, IsOutputMapSource());
        }

        private string ResolveLiveEntryStatusText(DieMapEntry entry)
        {
            if (!IsOutputMapSource() && entry != null)
            {
                if (entry.Result == DieResult.Good)
                    return "Good";
                if (entry.Result == DieResult.NG)
                    return "NG";
            }

            LiveDieMapCellState state = ResolveEntryState(entry, _displayStates);
            switch (state)
            {
                case LiveDieMapCellState.PickComplete:
                    return IsOutputMapSource() ? "PlaceComplete" : "PickComplete";
                case LiveDieMapCellState.InspectionDone:
                    return "VisionDone";
                case LiveDieMapCellState.InspectionWait:
                    return "Wait";
                default:
                    return entry != null ? entry.Result.ToString() : string.Empty;
            }
        }

        private Tuple<string, Color>[] BuildLiveLegendItems()
        {
            if (IsOutputMapSource())
            {
                return new[]
                {
                    Tuple.Create("Wait", InspectionWaitColor),
                    Tuple.Create("Vision", InspectionDoneColor),
                    Tuple.Create("Place", PickCompleteColor),
                    Tuple.Create("NG", Color.IndianRed)
                };
            }

            return new[]
            {
                Tuple.Create("Wait", InspectionWaitColor),
                Tuple.Create("Vision", InspectionDoneColor),
                Tuple.Create("Pick", PickCompleteColor),
                Tuple.Create("Good", BinCodeMap.ConvertToBinCodeColor(BinCodeMap.GoodBin)),
                Tuple.Create("NG", Color.IndianRed)
            };
        }

        private static Color ResolveEntryColor(
            DieMapEntry entry,
            Dictionary<string, LiveDieMapCellState> states,
            bool outputMapSource)
        {
            if (entry == null || !entry.IsTarget)
                return Color.FromArgb(0x66, 0x66, 0x66);

            LiveDieMapCellState state = ResolveEntryState(entry, states);
            if (outputMapSource)
            {
                if (state == LiveDieMapCellState.PickComplete)
                    return PickCompleteColor;

                if (entry.Result == DieResult.NG)
                {
                    int outputNgBinCode = entry.BinCode > 0 ? entry.BinCode : BinCodeMap.MaxBin;
                    Color outputNgColor = BinCodeMap.ConvertToBinCodeColor(outputNgBinCode);
                    return outputNgColor.ToArgb() == Color.Black.ToArgb() ? Color.IndianRed : outputNgColor;
                }

                if (state == LiveDieMapCellState.InspectionDone)
                    return InspectionDoneColor;
            }

            if (entry.Result == DieResult.NG)
            {
                int binCode = entry.BinCode > 0 ? entry.BinCode : BinCodeMap.MaxBin;
                Color c = BinCodeMap.ConvertToBinCodeColor(binCode);
                return c.ToArgb() == Color.Black.ToArgb() ? Color.IndianRed : c;
            }

            if (entry.Result == DieResult.Good)
                return BinCodeMap.ConvertToBinCodeColor(BinCodeMap.GoodBin);

            if (state == LiveDieMapCellState.PickComplete)
                return PickCompleteColor;

            if (state == LiveDieMapCellState.InspectionDone)
                return InspectionDoneColor;

            if (state == LiveDieMapCellState.InspectionWait)
                return InspectionWaitColor;

            if (entry.BinCode > 0)
                return BinCodeMap.ConvertToBinCodeColor(entry.BinCode);

            return InspectionWaitColor;
        }

        private static LiveDieMapCellState ResolveEntryState(
            DieMapEntry entry,
            Dictionary<string, LiveDieMapCellState> states)
        {
            if (entry == null || states == null)
                return LiveDieMapCellState.None;

            LiveDieMapCellState state;
            if (states.TryGetValue(BuildEntryGridKey(entry), out state))
                return state;

            return LiveDieMapCellState.None;
        }

        private static LiveDieMapCellState ResolveInputDieMapCellState(DieMaterial die)
        {
            if (die == null)
                return LiveDieMapCellState.InspectionWait;

            if (IsInputDieOnPicker(die))
                return LiveDieMapCellState.PickComplete;

            if (HasInputPickVisionInspection(die))
                return LiveDieMapCellState.InspectionDone;

            return LiveDieMapCellState.InspectionWait;
        }

        private static LiveDieMapCellState ResolveOutputDieMapCellState(OutputReceiveSlotMaterial slot)
        {
            if (slot == null)
                return LiveDieMapCellState.InspectionWait;

            if (slot.IsOutputInspectionDone)
                return LiveDieMapCellState.InspectionDone;

            if (slot.Result == DieResult.Good ||
                slot.Result == DieResult.NG ||
                !string.IsNullOrWhiteSpace(slot.DieUid))
                return LiveDieMapCellState.PickComplete;

            return LiveDieMapCellState.InspectionWait;
        }

        private static LiveDieMapCellState ResolveOutputDieMapCellState(DieMapEntry entry)
        {
            if (entry == null)
                return LiveDieMapCellState.InspectionWait;

            if (entry.Result == DieResult.Good || entry.Result == DieResult.NG)
                return LiveDieMapCellState.PickComplete;

            return LiveDieMapCellState.InspectionWait;
        }

        private static bool IsInputDieOnPicker(DieMaterial die)
        {
            if (die == null)
                return false;

            return die.CurrentLocation != null &&
                   (die.CurrentLocation.Kind == MaterialLocationKind.PickerFront ||
                    die.CurrentLocation.Kind == MaterialLocationKind.PickerRear);
        }

        private static bool HasValidPickedAt(DateTime pickedAt)
        {
            // material_state.json 저장 시 빈 DateTime은 1900-01-01로 정규화된다.
            // 이 값은 실제 Pick 완료 시간이 아니므로 화면 표시에서는 미픽업으로 본다.
            return pickedAt > new DateTime(2000, 1, 1);
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

        private static void DrawLegend(Graphics g)
        {
            using (var font = new Font("Gulim", 8.5F, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.FromArgb(0x33, 0x33, 0x33)))
            using (var borderPen = new Pen(Color.FromArgb(0x77, 0x77, 0x77), 1f))
            {
                int x = 8;
                int y = 23;
                x = DrawLegendItem(g, x, y, InspectionWaitColor, "검사대기", font, textBrush, borderPen);
                x = DrawLegendItem(g, x + 12, y, InspectionDoneColor, "검사완료", font, textBrush, borderPen);
                x = DrawLegendItem(g, x + 12, y, PickCompleteColor, "픽커보유", font, textBrush, borderPen);
                x = DrawLegendItem(g, x + 12, y, BinCodeMap.ConvertToBinCodeColor(BinCodeMap.GoodBin), "GOOD", font, textBrush, borderPen);
                DrawLegendItem(g, x + 12, y, Color.IndianRed, "NG", font, textBrush, borderPen);
            }
        }

        private static int DrawLegendItem(
            Graphics g,
            int x,
            int y,
            Color color,
            string text,
            Font font,
            Brush textBrush,
            Pen borderPen)
        {
            const int box = 10;
            using (var brush = new SolidBrush(color))
                g.FillRectangle(brush, x, y + 2, box, box);
            g.DrawRectangle(borderPen, x, y + 2, box, box);
            g.DrawString(text, font, textBrush, x + box + 4, y);

            SizeF size = g.MeasureString(text, font);
            return x + box + 4 + (int)Math.Ceiling(size.Width);
        }

        private MapStats CalculateMapStats(DieMap map, Dictionary<string, LiveDieMapCellState> states)
        {
            var stats = new MapStats();
            if (map == null || map.Entries == null)
                return stats;

            bool outputMapSource = IsOutputMapSource();
            foreach (var entry in map.Entries)
            {
                if (entry == null || !entry.IsTarget)
                    continue;

                stats.Target++;
                if (!outputMapSource)
                {
                    if (entry.Result == DieResult.Good)
                    {
                        stats.Good++;
                        stats.Done++;
                        continue;
                    }

                    if (entry.Result == DieResult.NG)
                    {
                        stats.Ng++;
                        stats.Done++;
                        continue;
                    }
                }

                LiveDieMapCellState state = ResolveEntryState(entry, states);
                if (state == LiveDieMapCellState.PickComplete)
                    stats.PickComplete++;
                else if (state == LiveDieMapCellState.InspectionDone)
                    stats.InspectionDone++;
                else
                    stats.InspectionWait++;

                if (entry.Result == DieResult.Good)
                {
                    stats.Good++;
                    stats.Done++;
                }
                else if (entry.Result == DieResult.NG)
                {
                    stats.Ng++;
                    stats.Done++;
                }
            }

            return stats;
        }

        private enum LiveDieMapCellState
        {
            None = 0,
            InspectionWait = 1,
            InspectionDone = 2,
            PickComplete = 3
        }

        private struct MapStats
        {
            public int Target;
            public int Done;
            public int InspectionWait;
            public int InspectionDone;
            public int PickComplete;
            public int Good;
            public int Ng;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                UnhookStateEvents();
                _refresh?.Stop();
                _refresh?.Dispose();
            }
            catch { }
            base.OnHandleDestroyed(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
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
            try
            {
                if (_refresh == null || IsDisposed)
                    return;

                if (PageBase.ShouldRefreshVisible(this))
                {
                    MarkDirty();
                    if (!_refresh.Enabled)
                        _refresh.Start();
                }
                else if (_refresh.Enabled)
                {
                    _refresh.Stop();
                }
            }
            catch
            {
            }
        }
    }
}
