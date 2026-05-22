using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QMC.CDT320.Materials
{
    /// <summary>웨이퍼 처리 모드 (310 의 ProcessModes).</summary>
    [DataContract]
    public enum ProcessMode
    {
        /// <summary>일반 모드.</summary>
        [EnumMember] Normal,
        /// <summary>반복성 측정 모드 (특정 다이를 N회 반복 측정).</summary>
        [EnumMember] Repeatability,
    }

    /// <summary>웨이퍼 역할 (310 의 DieTapeFrameRoles).</summary>
    [DataContract]
    public enum TapeFrameRole
    {
        [EnumMember] Load,
        [EnumMember] GoodUnload,
        [EnumMember] NgUnload,
    }

    /// <summary>웨이퍼 회전.</summary>
    [DataContract]
    public enum TapeFrameRotate
    {
        [EnumMember] None    = 0,
        [EnumMember] R90     = 90,
        [EnumMember] R180    = 180,
        [EnumMember] R270    = 270,
    }

    /// <summary>웨이퍼 식별자 verify 상태 (호스트 통신 전제).</summary>
    [DataContract]
    public enum IdentifierState
    {
        [EnumMember] None,
        [EnumMember] WaitingForHost,
        [EnumMember] VerificationOk,
        [EnumMember] VerificationFailed,
    }

    /// <summary>다이 맵 생성 상태.</summary>
    [DataContract]
    public enum DieMapGenerateState
    {
        [EnumMember] None,
        [EnumMember] WaitingForHost,
        [EnumMember] VerificationOk,
        [EnumMember] VerificationFailed,
    }

    /// <summary>웨이퍼 (DieTapeFrame).</summary>
    [DataContract]
    public class DieTapeFrame
    {
        [DataMember] public string ObjId       { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
        [DataMember] public string BarcodeId   { get; set; } = "";
        [DataMember] public TapeFrameRole Role { get; set; } = TapeFrameRole.Load;
        [DataMember] public ProcessMode   Mode { get; set; } = ProcessMode.Normal;
        [DataMember] public TapeFrameRotate Rotate { get; set; } = TapeFrameRotate.None;

        /// <summary>격자 (X 다이 수, Y 다이 수).</summary>
        [DataMember] public int    GridX  { get; set; } = 1;
        [DataMember] public int    GridY  { get; set; } = 1;
        /// <summary>다이 간격 (mm).</summary>
        [DataMember] public double PitchX { get; set; } = 1.0;
        [DataMember] public double PitchY { get; set; } = 1.0;
        /// <summary>웨이퍼 좌상단 기준 좌표 (모터 mm).</summary>
        [DataMember] public double OriginX { get; set; }
        [DataMember] public double OriginY { get; set; }

        /// <summary>다이 맵 파일 경로.</summary>
        [DataMember] public string MapFileName { get; set; } = "";

        [DataMember] public IdentifierState     IdentifierState     { get; set; } = IdentifierState.None;
        [DataMember] public DieMapGenerateState DieMapGenerateState { get; set; } = DieMapGenerateState.None;

        /// <summary>반복성 측정용 정보.</summary>
        [DataContract]
        public class RepeatabilityInformation
        {
            [DataMember] public int TotalDieCount { get; set; } = 4;
            [DataMember] public string CurrentRepeatDieUid { get; set; }
        }
        [DataMember] public RepeatabilityInformation Repeatability { get; set; } = new RepeatabilityInformation();

        public int  TotalDieCount => GridX * GridY;
        public override string ToString()
            => $"TapeFrame[{ObjId}] role={Role} grid={GridX}x{GridY} pitch=({PitchX:F3},{PitchY:F3}) origin=({OriginX:F2},{OriginY:F2}) rot={Rotate}";
    }
}
