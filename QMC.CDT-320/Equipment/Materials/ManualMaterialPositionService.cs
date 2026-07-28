using System;

namespace QMC.CDT320.Materials
{
    /// <summary>DATA ONLY 이동/삭제 대상 위치입니다. 카세트는 Role+Slot, 피더/스테이지는 Kind만 사용합니다.</summary>
    public sealed class DataOnlyLocation
    {
        private DataOnlyLocation()
        {
            SlotIndex = -1;
        }

        public MaterialLocationKind Kind { get; private set; }

        /// <summary>카세트 위치일 때만 유효한 Role입니다.</summary>
        public CassetteMaterialRole CassetteRole { get; private set; }

        /// <summary>카세트 위치일 때만 유효한 내부 슬롯 인덱스(0-base)입니다.</summary>
        public int SlotIndex { get; private set; }

        public bool IsCassette
        {
            get
            {
                return Kind == MaterialLocationKind.InputCassette ||
                       Kind == MaterialLocationKind.OutputCassette;
            }
        }

        public static DataOnlyLocation Cassette(CassetteMaterialRole role, int slotIndex)
        {
            bool isInput = role == CassetteMaterialRole.Input1 || role == CassetteMaterialRole.Input2;
            return new DataOnlyLocation
            {
                Kind = isInput ? MaterialLocationKind.InputCassette : MaterialLocationKind.OutputCassette,
                CassetteRole = role,
                SlotIndex = slotIndex
            };
        }

        public static DataOnlyLocation Station(MaterialLocationKind kind)
        {
            return new DataOnlyLocation { Kind = kind, SlotIndex = -1 };
        }

        public MaterialLocation ToMaterialLocation()
        {
            if (IsCassette)
                return MaterialLocation.Cassette(Kind, CassetteRole, SlotIndex);

            return new MaterialLocation { Kind = Kind };
        }

        public bool IsSameAs(DataOnlyLocation other)
        {
            if (other == null || Kind != other.Kind)
                return false;

            if (IsCassette)
                return CassetteRole == other.CassetteRole && SlotIndex == other.SlotIndex;

            return true;
        }

        /// <summary>확인창/로그용 표시 텍스트입니다. 예: "INPUT1 / SLOT 01", "INPUT FEEDER".</summary>
        public string DisplayText
        {
            get
            {
                if (IsCassette)
                    return GetRoleDisplay(CassetteRole) + " / SLOT " + (SlotIndex + 1).ToString("00");

                switch (Kind)
                {
                    case MaterialLocationKind.InputFeeder: return "INPUT FEEDER";
                    case MaterialLocationKind.InputStage: return "INPUT STAGE";
                    case MaterialLocationKind.OutputFeeder: return "OUTPUT FEEDER";
                    case MaterialLocationKind.OutputStageGood: return "OUTPUT GOOD STAGE";
                    case MaterialLocationKind.OutputStageNg: return "OUTPUT NG STAGE";
                    default: return Kind.ToString().ToUpperInvariant();
                }
            }
        }

        public static string GetRoleDisplay(CassetteMaterialRole role)
        {
            switch (role)
            {
                case CassetteMaterialRole.Input1: return "INPUT1";
                case CassetteMaterialRole.Input2: return "INPUT2";
                case CassetteMaterialRole.Good1: return "GOOD1";
                case CassetteMaterialRole.Good2: return "GOOD2";
                case CassetteMaterialRole.Ng1: return "NG";
                default: return role.ToString().ToUpperInvariant();
            }
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }

    /// <summary>DATA ONLY 이동/삭제 결과입니다. 성공 여부와 구체적 실패 사유를 함께 반환합니다.</summary>
    public sealed class DataOnlyOperationResult
    {
        public bool Success { get; set; }

        /// <summary>Move 또는 Delete.</summary>
        public string Operation { get; set; }

        public string MaterialId { get; set; }

        public string SourceText { get; set; }

        public string DestinationText { get; set; }

        public string FailureCode { get; set; }

        public string FailureMessage { get; set; }

        /// <summary>변경 전 Material 위치 텍스트입니다.</summary>
        public string BeforeLocationText { get; set; }

        /// <summary>변경 후 Material 위치 텍스트입니다.</summary>
        public string AfterLocationText { get; set; }

        /// <summary>Snapshot 즉시 저장 성공 여부입니다.</summary>
        public bool PersistenceSucceeded { get; set; }

        // To do: [DATA ONLY 교환] Destination이 점유된 경우 그 자재를 Source 위치로 교환한다.
        //        장비 실물 상태에 데이터를 맞추는 도구이므로 점유를 이유로 이동을 막지 않는다.
        /// <summary>Destination이 점유되어 Source 위치로 교환된 Material ID입니다. 교환이 없으면 빈 문자열.</summary>
        public string SwappedMaterialId { get; set; } = "";

        /// <summary>교환된 Material이 이동한 위치 텍스트입니다.</summary>
        public string SwappedToText { get; set; } = "";

        public static DataOnlyOperationResult Fail(string operation, string failureCode, string failureMessage)
        {
            return new DataOnlyOperationResult
            {
                Success = false,
                Operation = operation ?? "",
                MaterialId = "",
                SourceText = "",
                DestinationText = "",
                FailureCode = failureCode ?? "",
                FailureMessage = failureMessage ?? "",
                BeforeLocationText = "",
                AfterLocationText = "",
                SwappedMaterialId = "",
                SwappedToText = "",
                PersistenceSucceeded = false
            };
        }
    }

    /// <summary>DATA ONLY(장비 무동작) Material 위치 이동/삭제의 허용 경로 계약입니다.
    /// 물리 이송 시퀀스가 아니며 Motion/Cylinder/Vacuum/IO를 절대 호출하지 않습니다.</summary>
    public static class ManualMaterialPositionService
    {
        public static bool IsInputSystemKind(MaterialLocationKind kind)
        {
            return kind == MaterialLocationKind.InputCassette ||
                   kind == MaterialLocationKind.InputFeeder ||
                   kind == MaterialLocationKind.InputStage;
        }

        public static bool IsOutputSystemKind(MaterialLocationKind kind)
        {
            return kind == MaterialLocationKind.OutputCassette ||
                   kind == MaterialLocationKind.OutputFeeder ||
                   kind == MaterialLocationKind.OutputStageGood ||
                   kind == MaterialLocationKind.OutputStageNg;
        }

        /// <summary>Phase 1 허용 인접 경로인지 검증합니다.
        /// 허용: Cassette↔Feeder, Feeder↔Stage(계통 내). 금지: Cassette↔Stage 직행, 계통 간, 동일 위치.</summary>
        public static bool ValidateAllowedPath(
            DataOnlyLocation source,
            DataOnlyLocation destination,
            out string failureCode,
            out string failureMessage)
        {
            failureCode = "";
            failureMessage = "";

            if (source == null || destination == null)
            {
                failureCode = "DATA-ONLY-PATH-NULL";
                failureMessage = "이동 Source/Destination 위치가 지정되지 않았습니다.";
                return false;
            }

            if (!ValidateLocationShape(source, "Source", out failureCode, out failureMessage))
                return false;

            if (!ValidateLocationShape(destination, "Destination", out failureCode, out failureMessage))
                return false;

            if (source.IsSameAs(destination))
            {
                failureCode = "DATA-ONLY-PATH-SAME";
                failureMessage = "동일 위치로는 이동할 수 없습니다. location=" + source.DisplayText;
                return false;
            }

            bool sourceInput = IsInputSystemKind(source.Kind);
            bool destinationInput = IsInputSystemKind(destination.Kind);
            if (sourceInput != destinationInput)
            {
                failureCode = "DATA-ONLY-PATH-SYSTEM";
                failureMessage = "Input 계통과 Output 계통 사이에서는 이동할 수 없습니다. source=" +
                                 source.DisplayText + ", destination=" + destination.DisplayText;
                return false;
            }

            if (IsAllowedPair(source.Kind, destination.Kind))
                return true;

            bool cassetteToStage =
                (source.IsCassette && IsStageKind(destination.Kind)) ||
                (IsStageKind(source.Kind) && destination.IsCassette);
            if (cassetteToStage)
            {
                failureCode = "DATA-ONLY-PATH-DIRECT";
                failureMessage = "Cassette와 Stage 사이 직접 이동은 허용되지 않습니다. Feeder를 거쳐 이동하십시오. source=" +
                                 source.DisplayText + ", destination=" + destination.DisplayText;
                return false;
            }

            failureCode = "DATA-ONLY-PATH-NOT-ALLOWED";
            failureMessage = "허용되지 않은 이동 경로입니다. source=" + source.DisplayText +
                             ", destination=" + destination.DisplayText;
            return false;
        }

        private static bool ValidateLocationShape(
            DataOnlyLocation location,
            string label,
            out string failureCode,
            out string failureMessage)
        {
            failureCode = "";
            failureMessage = "";

            if (location.Kind == MaterialLocationKind.Unknown)
            {
                failureCode = "DATA-ONLY-PATH-UNKNOWN";
                failureMessage = label + " 위치 종류가 Unknown입니다.";
                return false;
            }

            if (!IsInputSystemKind(location.Kind) && !IsOutputSystemKind(location.Kind))
            {
                failureCode = "DATA-ONLY-PATH-KIND";
                failureMessage = label + " 위치는 DATA ONLY 이동을 지원하지 않습니다. kind=" + location.Kind;
                return false;
            }

            if (location.IsCassette)
            {
                bool roleMatchesKind =
                    (location.Kind == MaterialLocationKind.InputCassette &&
                     (location.CassetteRole == CassetteMaterialRole.Input1 || location.CassetteRole == CassetteMaterialRole.Input2)) ||
                    (location.Kind == MaterialLocationKind.OutputCassette &&
                     (location.CassetteRole == CassetteMaterialRole.Good1 ||
                      location.CassetteRole == CassetteMaterialRole.Good2 ||
                      location.CassetteRole == CassetteMaterialRole.Ng1));
                if (!roleMatchesKind)
                {
                    failureCode = "DATA-ONLY-PATH-ROLE";
                    failureMessage = label + " 카세트 Role이 위치 종류와 일치하지 않습니다. kind=" + location.Kind +
                                     ", role=" + location.CassetteRole;
                    return false;
                }

                if (location.SlotIndex < 0)
                {
                    failureCode = "DATA-ONLY-PATH-SLOT";
                    failureMessage = label + " 카세트 Slot이 지정되지 않았습니다. role=" + location.CassetteRole;
                    return false;
                }
            }

            return true;
        }

        private static bool IsStageKind(MaterialLocationKind kind)
        {
            return kind == MaterialLocationKind.InputStage ||
                   kind == MaterialLocationKind.OutputStageGood ||
                   kind == MaterialLocationKind.OutputStageNg;
        }

        private static bool IsAllowedPair(MaterialLocationKind source, MaterialLocationKind destination)
        {
            // INPUT 계통
            if (source == MaterialLocationKind.InputCassette && destination == MaterialLocationKind.InputFeeder) return true;
            if (source == MaterialLocationKind.InputFeeder && destination == MaterialLocationKind.InputCassette) return true;
            if (source == MaterialLocationKind.InputFeeder && destination == MaterialLocationKind.InputStage) return true;
            if (source == MaterialLocationKind.InputStage && destination == MaterialLocationKind.InputFeeder) return true;

            // OUTPUT 계통
            if (source == MaterialLocationKind.OutputCassette && destination == MaterialLocationKind.OutputFeeder) return true;
            if (source == MaterialLocationKind.OutputFeeder && destination == MaterialLocationKind.OutputCassette) return true;
            if (source == MaterialLocationKind.OutputFeeder && destination == MaterialLocationKind.OutputStageGood) return true;
            if (source == MaterialLocationKind.OutputStageGood && destination == MaterialLocationKind.OutputFeeder) return true;
            if (source == MaterialLocationKind.OutputFeeder && destination == MaterialLocationKind.OutputStageNg) return true;
            if (source == MaterialLocationKind.OutputStageNg && destination == MaterialLocationKind.OutputFeeder) return true;

            return false;
        }
    }
}
