using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QMC.CDT320.Materials
{
    [DataContract]
    public enum CassetteMaterialRole
    {
        [EnumMember] Input1,
        [EnumMember] Input2,
        [EnumMember] Good1,
        [EnumMember] Good2,
        [EnumMember] Ng1
    }

    [DataContract]
    public enum MaterialLocationKind
    {
        [EnumMember] Unknown,
        [EnumMember] InputCassette,
        [EnumMember] InputFeeder,
        [EnumMember] InputStage,
        [EnumMember] PickerFront,
        [EnumMember] PickerRear,
        [EnumMember] OutputStageGood,
        [EnumMember] OutputStageNg,
        [EnumMember] OutputFeeder,
        [EnumMember] OutputCassette
    }

    [DataContract]
    public enum WaferMaterialState
    {
        [EnumMember] Empty = 0,
        [EnumMember] Ready = 1,
        [EnumMember] WorkReady = 2,
        [EnumMember] Working = 5,
        [EnumMember] Finish = 6
    }

    public static class WaferMaterialStateText
    {
        public static readonly string[] DisplayNames =
        {
            "READY",
            "EMPTY",
            "WORKING",
            "FINISH",
            "WORK READY"
        };

        public static string ToDisplayName(WaferMaterialState state)
        {
            switch (Normalize(state))
            {
                // EMPTY 표시명 반환
                case WaferMaterialState.Empty:
                    return "EMPTY";
                // WORKING 표시명 반환
                case WaferMaterialState.Working:
                    return "WORKING";
                // FINISH 표시명 반환
                case WaferMaterialState.Finish:
                    return "FINISH";
                // WORK READY 표시명 반환
                case WaferMaterialState.WorkReady:
                    return "WORK READY";
                default:
                    return "READY";
            }
        }

        public static WaferMaterialState Normalize(WaferMaterialState state)
        {
            switch ((int)state)
            {
                // Empty 상태 코드 정규화
                case 0:
                    return WaferMaterialState.Empty;
                // Ready 상태 코드 정규화
                case 1:
                    return WaferMaterialState.Ready;
                // WorkReady 계열 상태 코드 정규화
                case 2:
                case 3:
                case 4:
                    return WaferMaterialState.WorkReady;
                // Working 상태 코드 정규화
                case 5:
                    return WaferMaterialState.Working;
                // Finish 계열 상태 코드 정규화
                case 6:
                case 7:
                    return WaferMaterialState.Finish;
                default:
                    return WaferMaterialState.Ready;
            }
        }

        public static bool TryParse(string text, out WaferMaterialState state)
        {
            string value = (text ?? "").Trim().Replace("_", " ").Replace("-", " ");
            string compact = value.Replace(" ", "");
            if (string.Equals(compact, "EMPTY", StringComparison.OrdinalIgnoreCase))
            {
                state = WaferMaterialState.Empty;
                return true;
            }
            if (string.Equals(compact, "READY", StringComparison.OrdinalIgnoreCase))
            {
                state = WaferMaterialState.Ready;
                return true;
            }
            if (string.Equals(compact, "WORKREADY", StringComparison.OrdinalIgnoreCase))
            {
                state = WaferMaterialState.WorkReady;
                return true;
            }
            if (string.Equals(compact, "WORKING", StringComparison.OrdinalIgnoreCase))
            {
                state = WaferMaterialState.Working;
                return true;
            }
            if (string.Equals(compact, "FINISH", StringComparison.OrdinalIgnoreCase))
            {
                state = WaferMaterialState.Finish;
                return true;
            }

            WaferMaterialState parsed;
            if (Enum.TryParse(text, true, out parsed))
            {
                state = Normalize(parsed);
                return true;
            }

            state = WaferMaterialState.Ready;
            return false;
        }
    }

    [DataContract]
    public enum MaterialInspectionResult
    {
        [EnumMember] Unknown,
        [EnumMember] Ok,
        [EnumMember] Ng
    }

    [DataContract]
    public class MaterialLocation
    {
        [DataMember] public MaterialLocationKind Kind { get; set; } = MaterialLocationKind.Unknown;
        [DataMember] public CassetteMaterialRole CassetteRole { get; set; } = CassetteMaterialRole.Input1;
        [DataMember] public int SlotNumber { get; set; } = -1;
        [DataMember] public int PickerNo { get; set; } = -1;

        public static MaterialLocation Unknown()
        {
            return new MaterialLocation();
        }

        public static MaterialLocation Cassette(MaterialLocationKind kind, CassetteMaterialRole role, int slotNumber)
        {
            return new MaterialLocation { Kind = kind, CassetteRole = role, SlotNumber = slotNumber };
        }

        public static MaterialLocation Picker(MaterialLocationKind kind, int pickerNo)
        {
            return new MaterialLocation { Kind = kind, PickerNo = pickerNo };
        }

        public override string ToString()
        {
            if (Kind == MaterialLocationKind.InputCassette || Kind == MaterialLocationKind.OutputCassette)
                return Kind + ":" + CassetteRole + "/Slot" + SlotNumber;
            if (Kind == MaterialLocationKind.PickerFront || Kind == MaterialLocationKind.PickerRear)
                return Kind + "/P" + PickerNo;
            return Kind.ToString();
        }
    }

    [DataContract]
    public class VisionOffset
    {
        [DataMember] public double X { get; set; }
        [DataMember] public double Y { get; set; }
        [DataMember] public double R { get; set; }
        [DataMember] public bool IsValid { get; set; }
    }

    [DataContract]
    public class InspectionMeasurement
    {
        [DataMember] public string Name { get; set; } = "";
        [DataMember] public double Value { get; set; }
        [DataMember] public string Unit { get; set; } = "";
        [DataMember] public double LowerLimit { get; set; }
        [DataMember] public double UpperLimit { get; set; }
        [DataMember] public string RawValue { get; set; } = "";
        [DataMember] public MaterialInspectionResult Result { get; set; } = MaterialInspectionResult.Unknown;
    }

    [DataContract]
    public class InspectionAlignmentSnapshot
    {
        [DataMember] public string Name { get; set; } = "";
        [DataMember] public double X { get; set; }
        [DataMember] public double Y { get; set; }
        [DataMember] public double T { get; set; }
        [DataMember] public double Z { get; set; }
        [DataMember] public string XAxisName { get; set; } = "";
        [DataMember] public string YAxisName { get; set; } = "";
        [DataMember] public string TAxisName { get; set; } = "";
        [DataMember] public string ZAxisName { get; set; } = "";
        [DataMember] public VisionOffset Offset { get; set; } = new VisionOffset();
        [DataMember] public bool IsValid { get; set; }
    }

    [DataContract]
    public class DieInspectionRecord
    {
        [DataMember] public string InspectionType { get; set; } = "";
        [DataMember] public MaterialInspectionResult Result { get; set; } = MaterialInspectionResult.Unknown;
        [DataMember] public List<InspectionMeasurement> Measurements { get; set; } = new List<InspectionMeasurement>();
        [DataMember] public List<string> NgCodes { get; set; } = new List<string>();
        [DataMember] public List<InspectionAlignmentSnapshot> Alignments { get; set; } = new List<InspectionAlignmentSnapshot>();
        [DataMember] public VisionOffset Offset { get; set; } = new VisionOffset();
        [DataMember] public DateTime CreatedAt { get; set; } = DateTime.Now;
        [DataMember] public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    [DataContract]
    public class CassetteSlotMaterial
    {
        [DataMember] public int SlotNumber { get; set; }
        [DataMember] public string WaferId { get; set; } = "";
        [DataMember] public bool HasWafer { get; set; }
    }

    [DataContract]
    public class CassetteMaterial
    {
        [DataMember] public string CassetteId { get; set; } = "";
        [DataMember] public string CassetteLotId { get; set; } = "";
        [DataMember] public CassetteMaterialRole Role { get; set; }
        [DataMember] public int Level { get; set; } = 1;
        [DataMember] public int SlotCount { get; set; } = 25;
        [DataMember] public bool IsEnabled { get; set; } = true;
        [DataMember] public bool IsPresent { get; set; }
        [DataMember] public bool IsMapped { get; set; }
        [DataMember] public DateTime LastScanTime { get; set; } = DateTime.Now;
        [DataMember] public List<CassetteSlotMaterial> Slots { get; set; } = new List<CassetteSlotMaterial>();

        public void EnsureSlots()
        {
            if (SlotCount < 0) SlotCount = 0;
            while (Slots.Count < SlotCount)
                Slots.Add(new CassetteSlotMaterial { SlotNumber = Slots.Count });
            while (Slots.Count > SlotCount)
                Slots.RemoveAt(Slots.Count - 1);
            for (int i = 0; i < Slots.Count; i++)
                Slots[i].SlotNumber = i;
        }
    }

    [DataContract]
    public class OutputReceiveSlotMaterial
    {
        [DataMember] public int OrderIndex { get; set; }
        [DataMember] public int SequenceNo { get; set; }
        [DataMember] public int DieMapX { get; set; }
        [DataMember] public int DieMapY { get; set; }
        [DataMember] public int OriginalMapX { get; set; } = -1;
        [DataMember] public int OriginalMapY { get; set; } = -1;
        [DataMember] public bool IsTarget { get; set; } = true;
        [DataMember] public DieResult Result { get; set; } = DieResult.Unknown;
        [DataMember] public int BinCode { get; set; }
        [DataMember] public double PosX { get; set; }
        [DataMember] public double PosY { get; set; }
        [DataMember] public string DieUid { get; set; } = "";
        [DataMember] public bool IsOutputInspectionDone { get; set; }
        [DataMember] public bool IsOutputInspectionOk { get; set; }
        [DataMember] public double OutputInspectionOffsetX { get; set; }
        [DataMember] public double OutputInspectionOffsetY { get; set; }
        [DataMember] public double OutputInspectionOffsetT { get; set; }
        [DataMember] public string OutputInspectionRaw { get; set; } = "";

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            OriginalMapX = -1;
            OriginalMapY = -1;
        }
    }

    public static class InputStageResultMode
    {
        public const string Standard = "Standard";
        public const string HybridRealVisionSimMotion = "HybridRealVisionSimMotion";

        public static string NormalizeForSave(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? Standard : value.Trim();
        }

        public static bool IsHybrid(string value)
        {
            return string.Equals(value, HybridRealVisionSimMotion, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsStandardOrLegacy(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                string.Equals(value, Standard, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsKnown(string value)
        {
            return IsStandardOrLegacy(value) || IsHybrid(value);
        }
    }

    internal static class InputStageHybridResultSession
    {
        private static readonly object Sync = new object();
        private static string _waferId = "";
        private static string _alignRunId = "";
        private static string _mappingAlignRunId = "";
        private static Func<bool> _modeValidator;

        private static void ClearNoLock()
        {
            _waferId = "";
            _alignRunId = "";
            _mappingAlignRunId = "";
            _modeValidator = null;
        }

        private static bool IsModeStillValidNoLock()
        {
            try
            {
                return _modeValidator != null && _modeValidator();
            }
            catch
            {
                return false;
            }
        }

        public static void Clear()
        {
            lock (Sync)
            {
                ClearNoLock();
            }
        }

        public static void MarkAlign(string waferId, string alignRunId, Func<bool> modeValidator)
        {
            lock (Sync)
            {
                _waferId = (waferId ?? "").Trim();
                _alignRunId = (alignRunId ?? "").Trim();
                _mappingAlignRunId = "";
                _modeValidator = modeValidator;
            }
        }

        public static void ClearMapping()
        {
            lock (Sync)
            {
                _mappingAlignRunId = "";
            }
        }

        public static bool IsCurrentAlign(string waferId, string alignRunId)
        {
            lock (Sync)
            {
                if (!IsModeStillValidNoLock())
                {
                    ClearNoLock();
                    return false;
                }

                return !string.IsNullOrWhiteSpace(_waferId) &&
                    !string.IsNullOrWhiteSpace(_alignRunId) &&
                    string.Equals(_waferId, (waferId ?? "").Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(_alignRunId, (alignRunId ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
            }
        }

        public static bool MarkMapping(string waferId, string alignRunId)
        {
            lock (Sync)
            {
                if (!IsModeStillValidNoLock() ||
                    string.IsNullOrWhiteSpace(_waferId) ||
                    string.IsNullOrWhiteSpace(_alignRunId) ||
                    !string.Equals(_waferId, (waferId ?? "").Trim(), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(_alignRunId, (alignRunId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    ClearNoLock();
                    return false;
                }

                _mappingAlignRunId = _alignRunId;
                return true;
            }
        }

        public static bool IsCurrentMapping(string waferId, string alignRunId)
        {
            lock (Sync)
            {
                if (!IsModeStillValidNoLock())
                {
                    ClearNoLock();
                    return false;
                }

                return !string.IsNullOrWhiteSpace(_mappingAlignRunId) &&
                    string.Equals(_waferId, (waferId ?? "").Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(_alignRunId, (alignRunId ?? "").Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(_mappingAlignRunId, _alignRunId, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [DataContract]
    public class WaferMaterial
    {
        [DataMember] public string WaferId { get; set; } = "";
        [DataMember] public string CassetteLotId { get; set; } = "";
        [DataMember] public string SourceCassetteId { get; set; } = "";
        [DataMember] public CassetteMaterialRole SourceCassetteRole { get; set; } = CassetteMaterialRole.Input1;
        [DataMember] public int SourceSlotNumber { get; set; } = -1;
        [DataMember] public double SourceCassetteSlotPosition { get; set; } = double.NaN;
        [DataMember] public string OutputCassetteId { get; set; } = "";
        [DataMember] public CassetteMaterialRole OutputCassetteRole { get; set; } = CassetteMaterialRole.Good1;
        [DataMember] public int OutputSlotNumber { get; set; } = -1;
        [DataMember] public double CurrentCassetteSlotPosition { get; set; } = double.NaN;
        [DataMember] public DieResult OutputGrade { get; set; } = DieResult.Unknown;
        [DataMember] public MaterialLocation CurrentLocation { get; set; } = MaterialLocation.Unknown();
        [DataMember] public WaferMaterialState State { get; set; } = WaferMaterialState.Empty;
        [DataMember] public string TapeFrameSpecName { get; set; } = "";
        [DataMember] public string DieMapFrameObjId { get; set; } = "";
        [DataMember] public bool HasInputStageAlignResult { get; set; }
        [DataMember] public string InputStageAlignResultMode { get; set; } = "";
        [DataMember] public string InputStageAlignResultRunId { get; set; } = "";
        [DataMember] public double InputStageAlignOriginX { get; set; }
        [DataMember] public double InputStageAlignOriginY { get; set; }
        [DataMember] public double InputStageAlignPitchX { get; set; }
        [DataMember] public double InputStageAlignPitchY { get; set; }
        /// <summary>Input Die Mapping에 사용한 승인 역할 맵의 Die X 크기(mm) 스냅샷.</summary>
        [DataMember] public double InputStageDieSizeX { get; set; }
        /// <summary>Input Die Mapping에 사용한 승인 역할 맵의 Die Y 크기(mm) 스냅샷.</summary>
        [DataMember] public double InputStageDieSizeY { get; set; }
        /// <summary>Input Die Mapping에 사용한 웨이퍼 외경(mm) 스냅샷.</summary>
        [DataMember] public double InputStageOuterDiameterMm { get; set; }
        [DataMember] public double InputStageAlignOffsetX { get; set; }
        [DataMember] public double InputStageAlignOffsetY { get; set; }
        [DataMember] public bool HasInputStageThetaAlignResult { get; set; }
        [DataMember] public double InputStageAlignReferenceT { get; set; }
        [DataMember] public double InputStageAlignCorrectedT { get; set; }
        [DataMember] public double InputStageAlignOffsetT { get; set; }
        [DataMember] public bool HasInputStageDieMappingResult { get; set; }
        [DataMember] public string InputStageDieMappingResultMode { get; set; } = "";
        [DataMember] public string InputStageDieMappingAlignRunId { get; set; } = "";
        [DataMember] public double InputStageDieMappingOffsetX { get; set; }
        [DataMember] public double InputStageDieMappingOffsetY { get; set; }
        /// <summary>원래 Align Origin과 분리하여 저장한 최종 Input Die Map 절대좌표 Origin.</summary>
        [DataMember] public bool HasInputStageDieMappingOrigin { get; set; }
        [DataMember] public double InputStageDieMappingOriginX { get; set; }
        [DataMember] public double InputStageDieMappingOriginY { get; set; }
        /// <summary>Die Mapping 확정 시 사용한 T 보정값 스냅샷.</summary>
        [DataMember] public bool HasInputStageDieMappingThetaSnapshot { get; set; }
        [DataMember] public double InputStageDieMappingCorrectedT { get; set; }
        /// <summary>Align/T 변경 후 이전 Mapping 데이터의 자동 복원을 차단한다.</summary>
        [DataMember] public bool InputStageDieMappingInvalidatedByAlignChange { get; set; }
        /// <summary>이 Wafer의 절대좌표 Mapping을 만들 때 승인된 Input 역할 맵 hash.</summary>
        [DataMember] public string InputMapApprovalHashAtMapping { get; set; } = "";
        /// <summary>현재 Align/Die Mapping 결과를 작업자가 확인하여 Auto PickUp 진행을 승인했는지 여부.</summary>
        [DataMember] public bool HasInputStageRunReviewApproval { get; set; }
        /// <summary>사용자 확인 시 선택된 시작 Die index. 0이면 현재 Mapping/Recipe 순서를 그대로 사용한다.</summary>
        [DataMember] public int InputStageRunReviewStartDieIndex { get; set; }
        /// <summary>사용자 확인 시 선택된 시작 Die UID.</summary>
        [DataMember] public string InputStageRunReviewStartDieUid { get; set; } = "";
        /// <summary>사용자 확인에서 확정한 실제 Input PickUp 순서.</summary>
        [DataMember] public List<string> InputStageRunReviewOrderedDieIds { get; set; } = new List<string>();
        /// <summary>사용자 확인 시점의 Die Mapping revision/frame ID.</summary>
        [DataMember] public string InputStageRunReviewMappingRevision { get; set; } = "";
        [DataMember] public string OutputReceiveSourceWaferId { get; set; } = "";
        [DataMember] public int OutputReceiveDieMapX { get; set; }
        [DataMember] public int OutputReceiveDieMapY { get; set; }
        [DataMember] public double OutputReceivePitchX { get; set; }
        [DataMember] public double OutputReceivePitchY { get; set; }
        /// <summary>Output receive plan에 사용한 승인 역할 맵의 Die X 크기(mm) 스냅샷.</summary>
        [DataMember] public double OutputReceiveDieSizeX { get; set; }
        /// <summary>Output receive plan에 사용한 승인 역할 맵의 Die Y 크기(mm) 스냅샷.</summary>
        [DataMember] public double OutputReceiveDieSizeY { get; set; }
        /// <summary>Output receive plan에 사용한 웨이퍼 외경(mm) 스냅샷.</summary>
        [DataMember] public double OutputReceiveOuterDiameterMm { get; set; }
        [DataMember] public double OutputReceiveOriginX { get; set; }
        [DataMember] public double OutputReceiveOriginY { get; set; }
        [DataMember] public int OutputReceiveNextIndex { get; set; }
        [DataMember] public int OutputReceiveTotalCount { get; set; }
        [DataMember] public string OutputReceiveStartCorner { get; set; } = "";
        [DataMember] public string OutputReceiveDirection { get; set; } = "";
        [DataMember] public string OutputReceivePattern { get; set; } = "";
        [DataMember] public List<OutputReceiveSlotMaterial> OutputReceiveSlots { get; set; } = new List<OutputReceiveSlotMaterial>();
        [DataMember] public List<string> DieIds { get; set; } = new List<string>();
        [DataMember] public DateTime CreatedAt { get; set; } = DateTime.Now;
        [DataMember] public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            SourceCassetteSlotPosition = double.NaN;
            CurrentCassetteSlotPosition = double.NaN;
            InputMapApprovalHashAtMapping = "";
            HasInputStageDieMappingOrigin = false;
            InputStageDieMappingOriginX = 0.0;
            InputStageDieMappingOriginY = 0.0;
            HasInputStageDieMappingThetaSnapshot = false;
            InputStageDieMappingCorrectedT = 0.0;
            InputStageDieMappingInvalidatedByAlignChange = false;
            HasInputStageRunReviewApproval = false;
            InputStageRunReviewStartDieIndex = 0;
            InputStageRunReviewStartDieUid = "";
            InputStageRunReviewOrderedDieIds = new List<string>();
            InputStageRunReviewMappingRevision = "";
        }
    }

    [DataContract]
    public class DieMaterial
    {
        [DataMember] public string DieId { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 12);
        [DataMember] public string WaferID_Input { get; set; } = "";
        [DataMember] public string WaferID_Output { get; set; } = "";
        [DataMember] public int Input_BinCode { get; set; }
        [DataMember] public bool IsInputTarget { get; set; } = true;
        [DataMember] public int Output_BinCode { get; set; }
        [DataMember] public int Wafer_IndexX { get; set; } = -1;
        [DataMember] public int Wafer_IndexY { get; set; } = -1;
        [DataMember] public int Wafer_OriginalIndexX { get; set; } = -1;
        [DataMember] public int Wafer_OriginalIndexY { get; set; } = -1;
        [DataMember] public int InputSequenceNo { get; set; }
        [DataMember] public int Bin_IndexX { get; set; } = -1;
        [DataMember] public int Bin_IndexY { get; set; } = -1;
        [DataMember] public MaterialLocation CurrentLocation { get; set; } = MaterialLocation.Unknown();
        [DataMember] public MaterialLocationKind ReservedPickerLocation { get; set; } = MaterialLocationKind.Unknown;
        [DataMember] public int ReservedPickerNo { get; set; } = -1;
        [DataMember] public MaterialLocationKind PickedPickerLocation { get; set; } = MaterialLocationKind.Unknown;
        [DataMember] public int PickedPickerNo { get; set; } = -1;
        [DataMember] public DateTime PickedAt { get; set; } = DateTime.MinValue;
        [DataMember] public DieResult Result { get; set; } = DieResult.Unknown;
        [DataMember] public List<string> NgCodes { get; set; } = new List<string>();
        [DataMember] public VisionOffset WaferOffset { get; set; } = new VisionOffset();
        [DataMember] public VisionOffset BinOffset { get; set; } = new VisionOffset();
        [DataMember] public List<DieInspectionRecord> Inspections { get; set; } = new List<DieInspectionRecord>();
        [DataMember] public DateTime CreatedAt { get; set; } = DateTime.Now;
        [DataMember] public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Wafer_IndexX = -1;
            Wafer_IndexY = -1;
            Wafer_OriginalIndexX = -1;
            Wafer_OriginalIndexY = -1;
            Bin_IndexX = -1;
            Bin_IndexY = -1;
            ReservedPickerNo = -1;
            PickedPickerNo = -1;
        }
    }

    public sealed class OutputStageReceiveTarget
    {
        public MaterialLocationKind StageLocation { get; set; }
        public string OutputWaferId { get; set; } = "";
        public string SourceWaferId { get; set; } = "";
        public int OrderIndex { get; set; }
        public int DieMapX { get; set; }
        public int DieMapY { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public double TargetX { get; set; }
        public double TargetY { get; set; }
    }

    public sealed class InputStagePickTarget
    {
        public string WaferId { get; set; } = "";
        public string DieId { get; set; } = "";
        public int OrderIndex { get; set; }
        public int DieMapX { get; set; }
        public int DieMapY { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public double TargetX { get; set; }
        public double TargetY { get; set; }
        public int PickerNo { get; set; }
        public MaterialLocationKind PickerLocation { get; set; }
    }

    public sealed class InputStagePickTargetCandidate
    {
        public string WaferId { get; set; } = "";
        public string DieId { get; set; } = "";
        public int OrderIndex { get; set; }
        public int DieMapX { get; set; }
        public int DieMapY { get; set; }
        public double TargetX { get; set; }
        public double TargetY { get; set; }
        public string DisplayText { get; set; } = "";
    }

    [DataContract]
    public class MaterialSnapshot
    {
        [DataMember] public int Version { get; set; } = 1;
        [DataMember] public DateTime SavedAt { get; set; } = DateTime.Now;
        [DataMember] public string SaveReason { get; set; } = "";
        [DataMember] public string RecipeName { get; set; } = "";
        [DataMember] public string LotId { get; set; } = "";
        [DataMember] public List<CassetteMaterial> Cassettes { get; set; } = new List<CassetteMaterial>();
        [DataMember] public List<WaferMaterial> Wafers { get; set; } = new List<WaferMaterial>();
        [DataMember] public List<DieMaterial> Dies { get; set; } = new List<DieMaterial>();
    }
}
