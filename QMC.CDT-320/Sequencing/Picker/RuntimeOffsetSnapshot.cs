using System;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Pick/Place 런타임 보정 필터(LowPassFilter)의 (Side, PickerNo) 1행 현재값 스냅샷.
    /// 서비스 내부 상태의 복사본이며 UI 표시와 메카 오프셋 이관 계산에 사용한다.
    /// 값은 비전 측정 부호 그대로(raw)이고, 이동식에 반영할 때의 부호 변환은
    /// 적용 지점(DieCoordinateTransformService)이 담당한다.
    /// </summary>
    internal sealed class RuntimeOffsetSnapshot
    {
        public RuntimeOffsetSnapshot(
            PickerSequenceSide side,
            int pickerNo,
            double x,
            double y,
            double t,
            DateTime lastUpdated)
        {
            Side = side;
            PickerNo = pickerNo;
            X = x;
            Y = y;
            T = t;
            LastUpdated = lastUpdated;
        }

        public PickerSequenceSide Side { get; private set; }
        public int PickerNo { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double T { get; private set; }
        public DateTime LastUpdated { get; private set; }

        public bool HasSample { get { return LastUpdated != DateTime.MinValue; } }
    }
}
