using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QMC.Vision.Core.Parameters
{
    /// <summary>P2 — 파라미터 값 스냅샷 1개 (문자열 인코딩, DataContractJson 다형성 회피).</summary>
    [DataContract]
    public class ParameterSnapshotEntry
    {
        [DataMember] public string Target { get; set; }   // Setup 통합 파일 다중 target 구분
        [DataMember] public string Key    { get; set; }
        [DataMember] public string Type   { get; set; }   // ParameterType.ToString()
        [DataMember] public string Value  { get; set; }   // InvariantCulture 인코딩
    }

    /// <summary>P2 — 한 파일에 담기는 스냅샷(엔트리 목록). [OnDeserializing] 으로 누락/구파일 기본값 가드(G9).</summary>
    [DataContract]
    public class ParameterSnapshot
    {
        [DataMember] public List<ParameterSnapshotEntry> Entries { get; set; }

        public ParameterSnapshot() { Entries = new List<ParameterSnapshotEntry>(); }

        [OnDeserializing]
        internal void OnDeserializing(StreamingContext ctx) { Entries = new List<ParameterSnapshotEntry>(); }
    }
}
