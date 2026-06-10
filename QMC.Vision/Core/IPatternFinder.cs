using System.Drawing;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 패턴 매칭/얼라인 Finder — Reticle, AlignDie, Eject pin, Ref1/2, Die, Collet 등 공용.
    /// 각 백엔드(Cognex CogPMAlignTool, OpenCV matchTemplate)에서 구현.
    /// </summary>
    public interface IPatternFinder
    {
        /// <summary>Finder 식별자 — "WaferVision/ReticleFinder" 등.</summary>
        string Id { get; }

        /// <summary>탐색 ROI.</summary>
        Roi SearchRoi { get; set; }

        /// <summary>학습 ROI (train 대상).</summary>
        Roi TrainRoi  { get; set; }

        /// <summary>학습 이미지. 런타임에 Train() 으로 생성/갱신.</summary>
        Bitmap TrainImage { get; }

        /// <summary>최소 허용 score (0.0~1.0).</summary>
        double AcceptThreshold { get; set; }

        /// <summary>최대 인스턴스 수.</summary>
        int MaxInstances { get; set; }

        /// <summary>주어진 이미지의 TrainRoi 영역을 학습 패턴으로 저장.</summary>
        void Train(Bitmap image);

        /// <summary>주어진 이미지에서 패턴을 찾고 결과 반환.</summary>
        MatchResult Match(Bitmap image);
    }
}
