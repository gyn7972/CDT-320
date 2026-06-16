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
    public class DieInspectionRecord
    {
        [DataMember] public string InspectionType { get; set; } = "";
        [DataMember] public MaterialInspectionResult Result { get; set; } = MaterialInspectionResult.Unknown;
        [DataMember] public List<InspectionMeasurement> Measurements { get; set; } = new List<InspectionMeasurement>();
        [DataMember] public List<string> NgCodes { get; set; } = new List<string>();
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
        [DataMember] public double InputStageAlignOriginX { get; set; }
        [DataMember] public double InputStageAlignOriginY { get; set; }
        [DataMember] public double InputStageAlignPitchX { get; set; }
        [DataMember] public double InputStageAlignPitchY { get; set; }
        [DataMember] public double InputStageAlignOffsetX { get; set; }
        [DataMember] public double InputStageAlignOffsetY { get; set; }
        [DataMember] public bool HasInputStageDieMappingResult { get; set; }
        [DataMember] public double InputStageDieMappingOffsetX { get; set; }
        [DataMember] public double InputStageDieMappingOffsetY { get; set; }
        [DataMember] public string OutputReceiveSourceWaferId { get; set; } = "";
        [DataMember] public int OutputReceiveDieMapX { get; set; }
        [DataMember] public int OutputReceiveDieMapY { get; set; }
        [DataMember] public double OutputReceivePitchX { get; set; }
        [DataMember] public double OutputReceivePitchY { get; set; }
        [DataMember] public double OutputReceiveOriginX { get; set; }
        [DataMember] public double OutputReceiveOriginY { get; set; }
        [DataMember] public int OutputReceiveNextIndex { get; set; }
        [DataMember] public int OutputReceiveTotalCount { get; set; }
        [DataMember] public string OutputReceiveStartCorner { get; set; } = "";
        [DataMember] public string OutputReceiveDirection { get; set; } = "";
        [DataMember] public string OutputReceivePattern { get; set; } = "";
        [DataMember] public List<string> DieIds { get; set; } = new List<string>();
        [DataMember] public DateTime CreatedAt { get; set; } = DateTime.Now;
        [DataMember] public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            SourceCassetteSlotPosition = double.NaN;
            CurrentCassetteSlotPosition = double.NaN;
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
        [DataMember] public int InputSequenceNo { get; set; }
        [DataMember] public int Bin_IndexX { get; set; } = -1;
        [DataMember] public int Bin_IndexY { get; set; } = -1;
        [DataMember] public MaterialLocation CurrentLocation { get; set; } = MaterialLocation.Unknown();
        [DataMember] public MaterialLocationKind ReservedPickerLocation { get; set; } = MaterialLocationKind.Unknown;
        [DataMember] public int ReservedPickerNo { get; set; } = -1;
        [DataMember] public DieResult Result { get; set; } = DieResult.Unknown;
        [DataMember] public List<string> NgCodes { get; set; } = new List<string>();
        [DataMember] public VisionOffset WaferOffset { get; set; } = new VisionOffset();
        [DataMember] public VisionOffset BinOffset { get; set; } = new VisionOffset();
        [DataMember] public List<DieInspectionRecord> Inspections { get; set; } = new List<DieInspectionRecord>();
        [DataMember] public DateTime CreatedAt { get; set; } = DateTime.Now;
        [DataMember] public DateTime UpdatedAt { get; set; } = DateTime.Now;
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
