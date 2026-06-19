using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace QMC.Common.Ui.Controls
{
    /// <summary>
    /// 비전 영상 프레임에 동봉되는 메타데이터 — 핸들러 카메라뷰가 측정(스케일)·결과 오버레이를 표시하는 데 사용.
    /// CDT-310 VisionImageHeader 개념 확장(이미지 헤더 + 스케일 + 결과).
    /// </summary>
    [DataContract]
    public class VisionFrameMeta
    {
        [DataMember] public string Module      { get; set; } = "";
        [DataMember] public int    Width       { get; set; }
        [DataMember] public int    Height      { get; set; }

        /// <summary>mm/pixel. 0 이면 측정은 px 로 표시.</summary>
        [DataMember] public double ScaleX      { get; set; }
        [DataMember] public double ScaleY      { get; set; }

        /// <summary>판정 텍스트(OK/NG/빈값). 우측 상단 큰 글자.</summary>
        [DataMember] public string Verdict     { get; set; } = "";
        [DataMember] public bool   VerdictPass { get; set; }

        /// <summary>우측 하단 결과 라인.</summary>
        [DataMember] public string[] ResultLines { get; set; }

        /// <summary>매칭 결과 마크(이미지 좌표). 없으면 null.</summary>
        [DataMember] public FrameMark[] Marks { get; set; }

        public byte[] ToBytes()
        {
            using (var ms = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(VisionFrameMeta)).WriteObject(ms, this);
                return ms.ToArray();
            }
        }

        public static VisionFrameMeta FromBytes(byte[] data, int offset, int count)
        {
            try
            {
                using (var ms = new MemoryStream(data, offset, count))
                    return (VisionFrameMeta)new DataContractJsonSerializer(typeof(VisionFrameMeta)).ReadObject(ms);
            }
            catch { return new VisionFrameMeta(); }
        }
    }

    /// <summary>매칭 결과 1개(이미지 좌표 중심 + 점수).</summary>
    [DataContract]
    public class FrameMark
    {
        [DataMember] public double X     { get; set; }
        [DataMember] public double Y     { get; set; }
        [DataMember] public double Score { get; set; }
    }
}
