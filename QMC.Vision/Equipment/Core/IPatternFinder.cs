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

        /// <summary>각도(회전) 탐색 사용. true 면 [-AngleToleranceDeg,+AngleToleranceDeg] 를 탐색해 회전된 패턴도 매칭한다.</summary>
        bool AngleEnabled { get; set; }

        /// <summary>회전 탐색 허용각(± deg). Train 대비 이 범위 안의 회전까지 매칭 성공·각도 보고. 0 이하이면 평행이동만.</summary>
        double AngleToleranceDeg { get; set; }

        /// <summary>회전 탐색 각도 스텝(deg). 작을수록 정밀·느림.</summary>
        double AngleStepDeg { get; set; }

        /// <summary>true 면 매칭 결과 중 '이미지 센터에 가장 가까운' 인스턴스를 선택(최고 점수 대신).
        /// 웨이퍼 정렬: 카메라 센터의 마크/다이를 기준으로 잡아야 하므로 켠다.</summary>
        bool PreferNearestCenter { get; set; }

        /// <summary>주어진 이미지의 TrainRoi 영역을 학습 패턴으로 저장.</summary>
        void Train(Bitmap image);

        /// <summary>저장된(이미 잘린) 패턴 이미지를 직접 학습 패턴으로 복원.
        /// 레시피와 함께 PNG 로 영속화한 패턴을 로드해 매칭에 재사용한다.</summary>
        void LoadTrainImage(Bitmap pattern);

        /// <summary>주어진 이미지에서 패턴을 찾고 결과 반환.</summary>
        MatchResult Match(Bitmap image);
    }
}
