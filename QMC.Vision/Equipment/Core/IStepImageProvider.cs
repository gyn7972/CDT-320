using System.Collections.Generic;
using System.Drawing;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 검사기가 INSPECT 중간 처리 단계 이미지(원본/그레이/임계/블롭 등)를 제공.
    /// <see cref="CaptureDebug"/>=true 일 때만 <see cref="DebugSteps"/>를 채운다(평상시 부하 0).
    /// 레시피 결과 저장 시 단계별 PNG 로 저장해 "어떻게 검출됐는지" 확인.
    /// </summary>
    public interface IStepImageProvider
    {
        bool CaptureDebug { get; set; }
        /// <summary>(단계이름, 이미지) 목록 — 순서대로 저장. 소유권은 검사기(다음 Inspect 시 교체).</summary>
        List<KeyValuePair<string, Bitmap>> DebugSteps { get; }
    }
}
