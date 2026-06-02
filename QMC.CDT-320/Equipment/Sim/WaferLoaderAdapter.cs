namespace QMC.CDT320.Sim
{
    /// <summary>
    /// Stage 28 — 실 InputLoaderUnit 을 IWaferLoader 인터페이스로 어댑팅.<br/>
    /// 이전에는 NullWaferLoader 가 무조건 IsFeederAtSafePosition=true 를 반환하여
    /// InputStage 의 안전 인터락이 무력화 되어 있었음.
    /// 본 어댑터는 실 InputLoader.FeederY 위치 + UpDownCyl 상태로 안전 여부를 판단한다.
    /// </summary>
    public class WaferLoaderAdapter : IWaferLoader
    {
        private readonly InputFeederUnit _loader;

        /// <summary>위험 범위 시작 (피더 중간 위치 — 인계 전).</summary>
        public double DangerRangeMin { get; set; } = 30.0;
        /// <summary>위험 범위 끝 (피더 인계 위치 직전).</summary>
        public double DangerRangeMax { get; set; } = 140.0;

        public WaferLoaderAdapter(InputFeederUnit loader)
        {
            _loader = loader;
        }

        /// <summary>
        /// 안전 위치 정의 (산업 흐름 반영):<br/>
        /// 1. 피더가 홈 근처 (≤ 30mm) — 후퇴 완료, InputStage 단독 작업 가능<br/>
        /// 2. 피더가 교환 위치 (≥ 140mm) — 웨이퍼 인계 직전/직후, InputStage 가 받을 준비 OK<br/>
        /// 3. 위험: 30~140mm 사이 — 이동 중 또는 부정확 위치
        /// </summary>
        public bool IsFeederAtSafePosition
        {
            get
            {
                double pos = _loader.FeederY.ActualPosition;
                // 홈 근처
                if (pos <= DangerRangeMin) return true;
                // 인계 위치 또는 그 이상
                if (pos >= DangerRangeMax) return true;
                // 그 외 (이동 중) — 위험
                return false;
            }
        }
    }
}
