using System.Threading.Tasks;

namespace QMC.CDT320
{
    // ??????????????????????????????????????????????????????????????????????????
    //  외부 연동 인터페이스 정의
    //  InputStageUnit이 의존하는 외부 서브시스템의 계약(Contract)을 정의한다.
    //  실제 구현체는 각 서브시스템 프로젝트에서 제공하며,
    //  생성자 주입(Constructor Injection)으로 Unit에 전달된다.
    // ??????????????????????????????????????????????????????????????????????????

    // ──────────────────────────────────────────────────────────────────────────
    //  §1. IWaferLoader ? 로더 유닛 인터페이스
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 로더 유닛(InputLoaderUnit)과의 연동 계약.<br/>
    /// InputStageUnit은 이 인터페이스를 통해 로더 피더의 안전 위치 여부를 확인한다.
    /// </summary>
    public interface IWaferLoader
    {
        /// <summary>
        /// 피더가 InputStage 작업 영역 밖의 안전 위치에 있는지 여부.<br/>
        /// false이면 ExpanderZ 하강 및 스테이지 이동을 금지해야 한다.
        /// </summary>
        bool IsFeederAtSafePosition { get; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  §2. IBarcodeReader ? 바코드 리더 인터페이스
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 웨이퍼 바코드(또는 OCR) 리더와의 연동 계약.<br/>
    /// 웨이퍼 교체 시 ID를 읽어 맵 데이터 파싱의 키(Key)로 사용한다.
    /// </summary>
    public interface IBarcodeReader
    {
        /// <summary>
        /// 바코드 리더를 트리거하여 웨이퍼 ID 문자열을 비동기로 반환한다.
        /// </summary>
        /// <param name="timeoutMs">읽기 타임아웃 [ms]</param>
        /// <returns>읽힌 Wafer ID 문자열. 실패 시 null 또는 빈 문자열.</returns>
        Task<string> ReadAsync(int timeoutMs = 3000);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  §3. IVisionTcpClient ? 비전 PC 통신 인터페이스
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 비전 PC(별도 TCP/IP 서버)와의 통신 계약.<br/>
    /// <para>
    /// 핵심 설계 원칙:<br/>
    /// <see cref="TriggerExposeAsync"/>는 카메라 노출(Expose) 완료 응답만 반환하며,
    /// 이미지 분석(Inspection) 결과를 기다리지 않는다.
    /// 이를 통해 비전 처리와 다음 모션 스텝을 파이프라인으로 병렬 진행할 수 있다.
    /// </para>
    /// </summary>
    public interface IVisionTcpClient
    {
        /// <summary>
        /// 카메라 촬상을 트리거하고 <b>노출(Expose) 완료</b> 응답만 비동기로 대기한다.<br/>
        /// 이미지 분석 결과는 기다리지 않으므로, 반환 즉시 다음 모션 스텝으로 이동 가능하다.
        /// </summary>
        /// <param name="dieIndex">촬상 대상 다이의 글로벌 인덱스</param>
        /// <returns>노출 완료 시 true, 타임아웃 또는 통신 오류 시 false.</returns>
        Task<bool> TriggerExposeAsync(int dieIndex);

        /// <summary>
        /// 이전에 트리거된 다이의 비전 검사 결과를 비동기로 조회한다.<br/>
        /// 픽업 직전 또는 별도 결과 수집 시점에 호출한다.
        /// </summary>
        /// <param name="dieIndex">결과를 조회할 다이의 글로벌 인덱스</param>
        /// <param name="timeoutMs">결과 수신 대기 타임아웃 [ms]</param>
        /// <returns>검사 결과. OK이면 true, NG 또는 타임아웃이면 false.</returns>
        Task<bool> GetResultAsync(int dieIndex, int timeoutMs = 5000);

        /// <summary>
        /// 비전 PC에 얼라인 모드를 설정하고 촬상을 트리거한다.<br/>
        /// 얼라인 전용 파라미터(조명, 배율 등)를 적용한 뒤 <b>노출 완료 + 계산 결과</b>까지 대기한다.
        /// </summary>
        /// <param name="alignTargetId">얼라인 대상 식별자 (예: "Center", "Ref1", "Ref2")</param>
        /// <returns>비전이 계산한 위치 보정값 객체. 통신 실패 시 null.</returns>
        Task<VisionAlignResult> TriggerAlignAsync(string alignTargetId);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  §4. IWaferMapHandler ? 웨이퍼 맵 핸들러 인터페이스
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Wafer ID를 기반으로 맵 데이터를 로드하고 UI에 전송하는 핸들러 계약.
    /// </summary>
    public interface IWaferMapHandler
    {
        /// <summary>
        /// Wafer ID로 외부 서버 또는 파일에서 맵 데이터를 파싱하여 반환한다.
        /// </summary>
        /// <param name="waferId">바코드 리더로 취득한 Wafer ID</param>
        /// <returns>파싱된 <see cref="WaferMapData"/>. 실패 시 null.</returns>
        Task<WaferMapData> ParseMapAsync(string waferId);

        /// <summary>
        /// 파싱된 맵 데이터와 현재 얼라인 결과를 UI로 전송(업데이트)한다.
        /// </summary>
        /// <param name="mapData">전송할 맵 데이터</param>
        void SendMapToUi(WaferMapData mapData);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  §5. ITransferPickerUnit ? TPU 연동 인터페이스
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// TransferPickerUnit과의 연동 계약.<br/>
    /// InputStageUnit은 이 인터페이스를 통해 TPU의 픽업 상태를 확인하고 픽업 가능 신호를 전송한다.
    /// </summary>
    public interface ITransferPickerUnit
    {
        /// <summary>TPU의 픽커 개수. 스캔 묶음 단위를 결정한다.</summary>
        int PickerCount { get; }

        /// <summary>
        /// TPU 픽커가 상승(대기) 위치에 있어 픽업 시작이 가능한 상태인지 여부.
        /// </summary>
        bool IsPickerReady { get; }

        /// <summary>
        /// 지정된 다이 인덱스에 대한 픽업 가능(Pick Ready) 신호를 TPU로 전송한다.<br/>
        /// TPU는 이 신호를 받으면 픽커를 하강시킨다.
        /// </summary>
        /// <param name="dieIndex">픽업 대상 다이의 글로벌 인덱스</param>
        void NotifyPickReady(int dieIndex);

        /// <summary>
        /// TPU 픽커가 완전히 상승하여 다이를 들고 대기하는 상태가 될 때까지 비동기로 대기한다.
        /// </summary>
        /// <param name="timeoutMs">대기 타임아웃 [ms]</param>
        /// <returns>정상 상승 완료 시 true, 타임아웃 시 false.</returns>
        Task<bool> WaitPickerUpAsync(int timeoutMs = 3000);
    }

    // ??????????????????????????????????????????????????????????????????????????
    //  데이터 전송 객체 (DTO)
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>
    /// 비전 얼라인 결과 데이터.<br/>
    /// 비전 PC가 계산한 위치·각도 보정값을 담는다.
    /// </summary>
    public class VisionAlignResult
    {
        /// <summary>X축 위치 보정값 [mm].</summary>
        public double DeltaX { get; set; }

        /// <summary>Y축 위치 보정값 [mm].</summary>
        public double DeltaY { get; set; }

        /// <summary>Theta(각도) 보정값 [deg].</summary>
        public double DeltaTheta { get; set; }

        /// <summary>X축 다이 피치 [mm]. 레퍼런스 마크 간 계산값.</summary>
        public double PitchX { get; set; }

        /// <summary>Y축 다이 피치 [mm]. 레퍼런스 마크 간 계산값.</summary>
        public double PitchY { get; set; }
    }

    /// <summary>
    /// 웨이퍼 맵 데이터.<br/>
    /// 다이 배치 정보 및 각 다이의 Good/NG 상태를 담는다.
    /// </summary>
    public class WaferMapData
    {
        /// <summary>웨이퍼 ID (바코드).</summary>
        public string WaferId { get; set; }

        /// <summary>웨이퍼의 열(Column) 수.</summary>
        public int ColumnCount { get; set; }

        /// <summary>웨이퍼의 행(Row) 수.</summary>
        public int RowCount { get; set; }

        /// <summary>
        /// 다이 상태 맵. [row, col] 인덱스로 접근.<br/>
        /// true = Good(픽업 대상), false = NG(스킵).
        /// </summary>
        public bool[,] DieMap { get; set; }

        /// <summary>레퍼런스 마크 1번의 행 인덱스.</summary>
        public int Ref1Row { get; set; }

        /// <summary>레퍼런스 마크 1번의 열 인덱스.</summary>
        public int Ref1Col { get; set; }

        /// <summary>레퍼런스 마크 2번의 행 인덱스.</summary>
        public int Ref2Row { get; set; }

        /// <summary>레퍼런스 마크 2번의 열 인덱스.</summary>
        public int Ref2Col { get; set; }
    }

    /// <summary>
    /// 사용자 컨펌 결과 데이터.<br/>
    /// UI에서 얼라인 확인 후 적용할 보정값 및 시작 조건을 담는다.
    /// </summary>
    public class UserConfirmResult
    {
        /// <summary>
        /// 사용자가 작업 진행을 승인했는지 여부.<br/>
        /// false이면 시퀀스를 중단한다.
        /// </summary>
        public bool IsConfirmed { get; set; }

        /// <summary>사용자가 수정한 추가 Angle 보정값 [deg]. 0이면 변경 없음.</summary>
        public double AngleOffset { get; set; }

        /// <summary>사용자가 수정한 시작 X 위치 오프셋 [mm]. 0이면 변경 없음.</summary>
        public double StartOffsetX { get; set; }

        /// <summary>사용자가 수정한 시작 Y 위치 오프셋 [mm]. 0이면 변경 없음.</summary>
        public double StartOffsetY { get; set; }

        /// <summary>사용자가 지정한 픽업 시작 다이 인덱스. 0이면 맵 첫 번째 다이부터 시작.</summary>
        public int StartDieIndex { get; set; }
    }
}
