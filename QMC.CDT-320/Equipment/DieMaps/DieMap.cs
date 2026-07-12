using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using QMC.CDT320.Materials;

namespace QMC.CDT320.DieMaps
{
    /// <summary>다이 맵의 한 셀.</summary>
    [DataContract]
    public class DieMapEntry
    {
        [DataMember] public int       Index    { get; set; }
        /// <summary>픽업/공정 순번. 1부터 시작, 0이면 순번 미지정 또는 비대상.</summary>
        [DataMember] public int       SequenceNo { get; set; }
        [DataMember] public int       DieMapX    { get; set; }
        [DataMember] public int       DieMapY    { get; set; }
        /// <summary>외부 웨이퍼맵 원본 X 인덱스. 없으면 DieMapX와 동일하게 취급.</summary>
        [DataMember] public int       OriginalMapX { get; set; } = -1;
        /// <summary>외부 웨이퍼맵 원본 Y 인덱스. 없으면 DieMapY와 동일하게 취급.</summary>
        [DataMember] public int       OriginalMapY { get; set; } = -1;
        /// <summary>true 면 이 위치는 처리 대상 (good die candidate).</summary>
        [DataMember] public bool      IsTarget { get; set; } = true;
        [DataMember] public DieResult Result   { get; set; } = DieResult.Unknown;
        [DataMember] public int       BinCode  { get; set; } = 0;
        /// <summary>모터 좌표 (mm).</summary>
        [DataMember] public double    PosX        { get; set; }
        [DataMember] public double    PosY        { get; set; }
        /// <summary>웨이퍼 중심을 0으로 한 장비 Grid X. 좌측 -, 우측 +.</summary>
        [DataMember] public double    EquipmentGridX { get; set; } = double.NaN;
        /// <summary>웨이퍼 중심을 0으로 한 장비 Grid Y. 위 -, 아래 +.</summary>
        [DataMember] public double    EquipmentGridY { get; set; } = double.NaN;
        /// <summary>해당 셀에 매핑된 Die.Uid (없으면 빈 문자열).</summary>
        [DataMember] public string    DieUid   { get; set; } = "";

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            // DataContractJsonSerializer는 누락 멤버의 속성 초기값을 실행하지 않는다.
            // 구형 JSON에 OriginalMapX/Y가 없으면 -1 sentinel을 먼저 넣어 DieMapX/Y를 원본 주소로 보존한다.
            OriginalMapX = -1;
            OriginalMapY = -1;
            IsTarget = true;
            DieUid = "";
            EquipmentGridX = double.NaN;
            EquipmentGridY = double.NaN;
        }
    }

    /// <summary>웨이퍼 다이 맵.</summary>
    [DataContract]
    public class DieMap
    {
        [DataMember] public string FrameObjId { get; set; } = "";
        [DataMember] public int    DieMapX      { get; set; }
        [DataMember] public int    DieMapY      { get; set; }
        /// <summary>장비 좌표의 다이 중심 간격(mm) = Die Size + Recipe Pitch Gap.</summary>
        [DataMember] public double PitchX     { get; set; }
        [DataMember] public double PitchY     { get; set; }
        [DataMember] public double DieSizeX   { get; set; }
        [DataMember] public double DieSizeY   { get; set; }
        [DataMember] public double OuterDiameterMm { get; set; }
        [DataMember] public string EdgeSkipMode { get; set; } = "";
        [DataMember] public double SideEdgeSkip { get; set; }
        [DataMember] public double TopBottomEdgeSkip { get; set; }
        [DataMember] public double OriginX    { get; set; }
        [DataMember] public double OriginY    { get; set; }
        /// <summary>Base 맵을 만들 때 선택한 원본 파일명. Recipe 복사 후에도 추적 가능하도록 파일명만 보존한다.</summary>
        [DataMember] public string SourceFileName { get; set; } = "";
        /// <summary>원본 맵 형식. 예: RAD TXT, CSV, JSON.</summary>
        [DataMember] public string SourceFormat { get; set; } = "";
        /// <summary>Pitch가 원본 파일 Header에서 읽힌 값인지 여부.</summary>
        [DataMember] public bool SourcePitchFromFile { get; set; }
        /// <summary>원본 Header가 선언한 Die record 수. 없으면 0.</summary>
        [DataMember] public int SourceDeclaredCount { get; set; }
        /// <summary>원본 Header FIRST_X/FIRST_Y. 없으면 -1.</summary>
        [DataMember] public int SourceFirstX { get; set; } = -1;
        [DataMember] public int SourceFirstY { get; set; } = -1;
        /// <summary>원본 Header FX/FY를 mm로 변환한 값. 없으면 NaN.</summary>
        [DataMember] public double SourceFirstPosX { get; set; } = double.NaN;
        [DataMember] public double SourceFirstPosY { get; set; } = double.NaN;
        [DataMember] public List<DieMapEntry> Entries { get; set; } = new List<DieMapEntry>();
        [DataMember] public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int TotalCells => DieMapX * DieMapY;

        /// <summary>(gx, gy) 셀을 가져옴 (없으면 null).</summary>
        public DieMapEntry GetCell(int gx, int gy)
        {
            foreach (var e in Entries)
                if (e.DieMapX == gx && e.DieMapY == gy) return e;
            return null;
        }

        /// <summary>인덱스 i 셀.</summary>
        public DieMapEntry GetByIndex(int i)
            => (i >= 0 && i < Entries.Count) ? Entries[i] : null;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SourceFileName = "";
            SourceFormat = "";
            SourceFirstX = -1;
            SourceFirstY = -1;
            SourceFirstPosX = double.NaN;
            SourceFirstPosY = double.NaN;
            Entries = new List<DieMapEntry>();
        }
    }
}
