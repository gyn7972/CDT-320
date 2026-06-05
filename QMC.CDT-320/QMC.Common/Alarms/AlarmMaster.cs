using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QMC.Common.Alarms
{
    /// <summary>알람 카테고리 — 알람 분류 + UI 색상 + SECS Severity 매핑.</summary>
    [DataContract]
    public enum AlarmCategory
    {
        [EnumMember] Motion,
        [EnumMember] IO,
        [EnumMember] Vision,
        [EnumMember] Communication,
        [EnumMember] Material,
        [EnumMember] Safety,
        [EnumMember] System,
        [EnumMember] User,
    }

    /// <summary>알람 마스터 항목 — 코드/제목/원인/조치 (ko/en 다국어).</summary>
    [DataContract]
    public class AlarmDefinition
    {
        [DataMember] public string         Code        { get; set; } = "";
        [DataMember] public AlarmCategory  Category    { get; set; } = AlarmCategory.System;
        [DataMember] public AlarmSeverity  DefaultSeverity { get; set; } = AlarmSeverity.Warning;
        // Korean (default)
        [DataMember] public string         Title       { get; set; } = "";
        [DataMember] public string         Cause       { get; set; } = "";
        [DataMember] public string         Action      { get; set; } = "";
        // English (Lang.Current=="en" 시 사용. 비어 있으면 Korean fallback)
        [DataMember] public string         TitleEn     { get; set; } = "";
        [DataMember] public string         CauseEn     { get; set; } = "";
        [DataMember] public string         ActionEn    { get; set; } = "";
        // CDT-310 매뉴얼 호환 (Stage 60) — ASE 매뉴얼 검수용 메타데이터.
        // 비어 있으면 매뉴얼 매핑 정보 없음 (CDT-320 자체 코드).
        [DataMember] public string         ManualName    { get; set; } = "";
        [DataMember] public string         ManualLocator { get; set; } = "";

        public string GetTitle(string lang)
            => (lang == "en" && !string.IsNullOrEmpty(TitleEn)) ? TitleEn : Title;
        public string GetCause(string lang)
            => (lang == "en" && !string.IsNullOrEmpty(CauseEn)) ? CauseEn : Cause;
        public string GetAction(string lang)
            => (lang == "en" && !string.IsNullOrEmpty(ActionEn)) ? ActionEn : Action;

        public override string ToString()
            => $"[{Code}] {Title} ({Category}/{DefaultSeverity})";
    }

    /// <summary>
    /// 알람 마스터 테이블 — 코드 → 정의 매핑.
    /// JSON: Config/alarm_master.json
    /// </summary>
    public static class AlarmMaster
    {
        public static string Dir   => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ => System.IO.Path.Combine(Dir, "alarm_master.json");

        private static Dictionary<string, AlarmDefinition> _byCode = new Dictionary<string, AlarmDefinition>(StringComparer.OrdinalIgnoreCase);

        static AlarmMaster()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
            Load();
        }

        public static IReadOnlyDictionary<string, AlarmDefinition> ByCode => _byCode;

        public static AlarmDefinition Get(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            _byCode.TryGetValue(code, out var d);
            return d;
        }

        public static IEnumerable<AlarmDefinition> ByCategory(AlarmCategory cat)
            => _byCode.Values.Where(d => d.Category == cat);

        public static int Count => _byCode.Count;

        public static void Load()
        {
            try
            {
                if (!File.Exists(Path_))
                {
                    var defaults = CreateDefaults();
                    _byCode = defaults.ToDictionary(d => d.Code, d => d, StringComparer.OrdinalIgnoreCase);
                    Save();
                    return;
                }
                using (var fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(List<AlarmDefinition>));
                    var list = (List<AlarmDefinition>)ser.ReadObject(fs);
                    if (list != null && list.Count > 0)
                        _byCode = list.ToDictionary(d => d.Code, d => d, StringComparer.OrdinalIgnoreCase);
                }
            }
            catch
            {
                _byCode = CreateDefaults().ToDictionary(d => d.Code, d => d, StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void Save()
        {
            try
            {
                var list = _byCode.Values.OrderBy(d => d.Code).ToList();
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(List<AlarmDefinition>));
                    ser.WriteObject(fs, list);
                }
            }
            catch { }
        }

        public static List<AlarmDefinition> CreateDefaults()
        {
            // Stage 60 — verified 2026-05-04
            // Stage 62 — added VISION-MAPMISS / VISION-PARAMFAIL / VISION-CAMOPEN at the end of Vision section
            return new List<AlarmDefinition>
            {
                // ── Motion (모션) ──
                new AlarmDefinition { Code="HOME-FAIL",  Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Error,
                    Title="HOME 검색 실패", Cause="원점 센서 신호 미인식 / 축 알람", Action="알람 리셋 후 수동 HOME 시도",
                    TitleEn="Home search failed", CauseEn="Origin sensor not detected / axis alarm", ActionEn="Reset alarm and retry HOME manually",
                    ManualName="FailedReferencePositionFind", ManualLocator="DieTransfer/WaferTransfer/Stage" },
                new AlarmDefinition { Code="MOVE-TIMEOUT", Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Error,
                    Title="모션 타임아웃", Cause="목표 위치 미도달", Action="물리 간섭 확인, 서보 게인 조정",
                    ManualName="PositionMismatchedInMotionDone", ManualLocator="모든 Axis" },
                new AlarmDefinition { Code="SERVO-OFF", Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Warning,
                    Title="서보 OFF", Cause="EMG 또는 알람 트리거", Action="원인 해소 후 ServoOn 재시도" },
                new AlarmDefinition { Code="LIMIT-HIT", Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Error,
                    Title="리미트 센서 도달", Cause="기계 한계 또는 좌표 오류", Action="역방향 조그 후 좌표 재설정",
                    ManualName="PositivePositionExceed", ManualLocator="모든 Axis" },
                new AlarmDefinition { Code="INTERLOCK", Category=AlarmCategory.Safety, DefaultSeverity=AlarmSeverity.Warning,
                    Title="인터록 차단", Cause="다른 축의 위치/상태가 이동을 차단", Action="History → Alarm 에서 차단 사유 확인 후 안전 위치로 이동",
                    TitleEn="Interlock blocked", CauseEn="Other axis position/state blocks the move", ActionEn="Check History/Alarm for reason, move to safe position",
                    ManualName="InterlockDetected", ManualLocator="모든 Interlock" },

                // ── Vision (비전) ──
                new AlarmDefinition { Code="VISION-CONN", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="Vision 연결 변경", Cause="Vision PC 연결/해제", Action="자동 재연결 대기 또는 수동 connect" },
                new AlarmDefinition { Code="VisionMatchFail", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="Vision 매칭 실패", Cause="조명, 패턴 학습 부족, ROI 어긋남", Action="ROI 재설정 + 재학습, 조명 조정",
                    TitleEn="Vision match failed", CauseEn="Lighting / pattern training / ROI mismatch", ActionEn="Reset ROI, re-train, adjust lighting",
                    ManualName="OutOfTolerance", ManualLocator="DieTransfer/.../MachineVision/Services" },
                new AlarmDefinition { Code="EXPOSE-TIMEOUT", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Error,
                    Title="EPD 미수신", Cause="Vision 그랩 미완료", Action="Vision PC 상태 확인, 카메라 케이블/네트워크 점검",
                    ManualName="ResultTimeOut", ManualLocator="DieTransfer/.../MachineVision/Services" },
                // Stage 62 — Vision 알고리즘별 카메라 설정 알람 (신규 3 개)
                new AlarmDefinition { Code="VISION-MAPMISS", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="알고리즘 카메라 매핑 누락", Cause="algorithm_camera.json 에 해당 알고리즘 항목 없음 또는 CameraId 비어 있음", Action="설정 페이지에서 카메라 ID 지정 후 저장",
                    TitleEn="Algorithm camera mapping missing", CauseEn="No mapping entry for algorithm or CameraId empty", ActionEn="Set camera ID via Settings page and save" },
                new AlarmDefinition { Code="VISION-PARAMFAIL", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="카메라 파라미터 적용 실패", Cause="Exposure/Gain/Trigger/ROI 설정 중 예외", Action="EventLog 확인, 카메라 SDK/드라이버 점검",
                    TitleEn="Camera parameter apply failed", CauseEn="Exception while setting Exposure/Gain/Trigger/ROI", ActionEn="Check EventLog, verify camera SDK/driver" },
                new AlarmDefinition { Code="VISION-CAMOPEN", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Error,
                    Title="카메라 Open 실패", Cause="GigE 미연결 / SDK 미설치 / IP 불일치", Action="IP·케이블·SDK 확인 (Sim fallback 시 무시 가능)",
                    TitleEn="Camera open failed", CauseEn="GigE not connected / SDK missing / IP mismatch", ActionEn="Check IP/cable/SDK (ignorable when Sim fallback)" },

                // Stage 67 — LFine 조명 컨트롤러 알람 (신규 6 개, Vision 로컬 raise)
                new AlarmDefinition { Code="LIGHT-OPEN-FAIL", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Error,
                    Title="조명 컨트롤러 시리얼 Open 실패", Cause="COM 포트 미발견 / 충돌 / 점유", Action="케이블·COM 포트 번호 확인",
                    TitleEn="Light controller serial open failed", CauseEn="COM port missing / conflict / in use", ActionEn="Check cable and COM port number" },
                new AlarmDefinition { Code="LIGHT-TIMEOUT", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="조명 컨트롤러 응답 타임아웃", Cause="컨트롤러 무응답", Action="전원·케이블·포트 확인",
                    TitleEn="Light controller reply timeout", CauseEn="Controller not responding", ActionEn="Check power/cable/port" },
                new AlarmDefinition { Code="LIGHT-NAK", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="조명 컨트롤러 NAK(RERR) 응답", Cause="잘못된 명령 형식", Action="명령 포맷·펌웨어 확인",
                    TitleEn="Light controller NAK (RERR)", CauseEn="Invalid command format", ActionEn="Check command format/firmware" },
                new AlarmDefinition { Code="LIGHT-INVALID-RESP", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="조명 컨트롤러 응답 포맷 오류", Cause="프로토콜 불일치", Action="펌웨어 버전 확인",
                    TitleEn="Light controller invalid response", CauseEn="Protocol mismatch", ActionEn="Check firmware version" },
                new AlarmDefinition { Code="LIGHT-TX-FAIL", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="조명 컨트롤러 송신 실패", Cause="시리얼 쓰기 예외 / 포트 미개방", Action="포트 상태 확인",
                    TitleEn="Light controller send failed", CauseEn="Serial write exception / port not open", ActionEn="Check port state" },
                new AlarmDefinition { Code="LIGHT-PWR-RANGE", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="조명 Power/Time 범위 초과", Cause="MaxPower/MaxOnTime 초과 입력", Action="입력값 점검",
                    TitleEn="Light power/time out of range", CauseEn="Input exceeds MaxPower/MaxOnTime", ActionEn="Check input value" },

                // Stage 69 — 검사별 조명 매핑 알람 (신규 3 개)
                new AlarmDefinition { Code="LIGHT-WIRING-MISS", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="조명 결선 누락", Cause="알고리즘의 조명 결선(AlgorithmLightWiring) 없음 또는 ControllerPort 빈값", Action="설정 > 조명 시스템 에서 컨트롤러/채널 배정",
                    TitleEn="Light wiring missing", CauseEn="No AlgorithmLightWiring or empty ControllerPort", ActionEn="Assign controller/channels in Settings > Light System" },
                new AlarmDefinition { Code="LIGHT-MAP-INVALID", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="조명 결선 포트 무효", Cause="Wiring.ControllerPort 가 LightHub 에 등록되지 않은 포트", Action="조명 시스템 Setup 과 실제 연결 포트 동기화",
                    TitleEn="Light wiring port invalid", CauseEn="ControllerPort not registered in LightHub", ActionEn="Sync Light System setup with connected ports" },
                new AlarmDefinition { Code="LIGHT-CHANNEL-OUT-OF-POOL", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="조명 채널 풀 밖", Cause="검사 조명 설정의 채널이 알고리즘 결선 풀 밖", Action="결선 표의 사용 채널 확인 후 수정",
                    TitleEn="Light channel out of pool", CauseEn="Setting channel not in algorithm wiring pool", ActionEn="Check assigned channels in wiring table" },

                // ── Material / Inspection ──
                new AlarmDefinition { Code="PickFail", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Warning,
                    Title="Pick 실패", Cause="진공 부족, 콜렛 손상, 다이 미확인", Action="콜렛 청소, 진공 압력 확인, Pick Retry 재시도",
                    TitleEn="Pick failed", CauseEn="Vacuum low / collet damaged / die not detected", ActionEn="Clean collet, check vacuum, run Pick Retry",
                    ManualName="MaterialDoesNotExistAfterReceive", ManualLocator="DieTransfer/PickAndPlaceDieTransfer/Tool/PickerN" },
                new AlarmDefinition { Code="BottomInspFail", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Warning,
                    Title="Bottom 검사 실패", Cause="다이 표면 결함 또는 임계값 초과", Action="레시피 임계값 조정 또는 다이 NG 처리",
                    ManualName="FailedInspection", ManualLocator="DieTransfer/PickAndPlaceDieTransfer/BottomInspectionVision" },
                new AlarmDefinition { Code="PlacementFail", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Warning,
                    Title="Placement 실패", Cause="Bin Vision 측정 NG", Action="배치 위치/각도 재조정" },

                // ── Communication ──
                new AlarmDefinition { Code="SECS-DISCONN", Category=AlarmCategory.Communication, DefaultSeverity=AlarmSeverity.Warning,
                    Title="SECS Host 연결 끊김", Cause="네트워크 또는 Host 측 종료", Action="자동 재연결 대기" },
                new AlarmDefinition { Code="SIM-DISCONN", Category=AlarmCategory.Communication, DefaultSeverity=AlarmSeverity.Warning,
                    Title="시뮬레이터 연결 끊김", Cause="시뮬레이터 종료 또는 TCP 오류", Action="시뮬레이터 재기동" },

                // ── IO / Safety ──
                new AlarmDefinition { Code="E-STOP", Category=AlarmCategory.Safety, DefaultSeverity=AlarmSeverity.Critical,
                    Title="비상 정지", Cause="E-Stop 버튼 또는 Door 센서", Action="안전 확인 후 비상 정지 해제 + Reset",
                    TitleEn="Emergency Stop", CauseEn="E-Stop button or door sensor", ActionEn="Confirm safety, release E-Stop and Reset",
                    ManualName="InterlockDetected", ManualLocator="모든 Interlock" },
                new AlarmDefinition { Code="VAC-LOW", Category=AlarmCategory.IO, DefaultSeverity=AlarmSeverity.Warning,
                    Title="진공 부족", Cause="진공 펌프 또는 라인 누설", Action="진공 게이지 확인, 호스 점검" },
                new AlarmDefinition { Code="CDA-LOW", Category=AlarmCategory.IO, DefaultSeverity=AlarmSeverity.Error,
                    Title="공압 부족", Cause="레귤레이터 또는 라인 누설", Action="공압 게이지 확인" },
                new AlarmDefinition { Code="IONIZER", Category=AlarmCategory.IO, DefaultSeverity=AlarmSeverity.Warning,
                    Title="이오나이저 알람", Cause="이오나이저 동작 이상 또는 전극 청소 필요", Action="전극 청소 후 reset" },

                // ── System ──
                new AlarmDefinition { Code="CYCLE-EX", Category=AlarmCategory.System, DefaultSeverity=AlarmSeverity.Error,
                    Title="사이클 예외", Cause="MachineController 내부 예외", Action="EventLog 확인 후 재기동" },

                // ── InputStage ──
                new AlarmDefinition { Code="IS-FEEDER", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Warning,
                    Title="입력 피더 안전 위치 미확인", Cause="피더가 안전 위치 미도달", Action="피더 위치 확인 후 수동 후퇴" },
                new AlarmDefinition { Code="IS-EXPZ", Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Error,
                    Title="ExpanderZ 알람", Cause="ExpanderZ 축 알람 발생", Action="알람 리셋 후 수동 HOME" },
                new AlarmDefinition { Code="IS-BARCODE", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Warning,
                    Title="바코드 읽기 실패", Cause="바코드 리더 통신 또는 라벨 인식 실패", Action="라벨 청소, 시리얼 케이블 확인" },
                new AlarmDefinition { Code="IS-MAP", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Warning,
                    Title="웨이퍼 맵 파싱 실패", Cause="맵 파일 미존재 또는 형식 오류", Action="맵 파일 위치/형식 확인" },
                new AlarmDefinition { Code="IS-ALIGN", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="비전 얼라인 실패", Cause="얼라인 비전 매칭/수렴 실패", Action="조명/패턴 학습 재조정" },
                new AlarmDefinition { Code="IS-MOVE", Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Error,
                    Title="InputStage 이동 실패", Cause="이동 후 축 알람", Action="알람 리셋, 인터락 확인" },

                // ── OutputStage ──
                new AlarmDefinition { Code="OS-AVOID", Category=AlarmCategory.Safety, DefaultSeverity=AlarmSeverity.Error,
                    Title="OutputStage 회피 실패", Cause="반대 스테이지 Z 회피 미달성", Action="인터락 확인, StageZ 수동 하강",
                    ManualName="InterlockDetected", ManualLocator="DieTransfer/BinTransfer/DiePost/UpDownCylinder/*Interlock" },
                new AlarmDefinition { Code="OS-WORKZ", Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Error,
                    Title="OutputStage WorkZ 이동 실패", Cause="StageZ 작업 위치 이동 후 알람", Action="알람 리셋, 인터락 확인" },
                new AlarmDefinition { Code="OS-MOVEY", Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Error,
                    Title="OutputStage Y 이동 실패", Cause="StageY 이동 후 알람", Action="알람 리셋, 인터락 확인" },
                new AlarmDefinition { Code="OS-PLACEDONE", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Warning,
                    Title="TPU Place 완료 대기 타임아웃", Cause="TPU 측 Place 완료 신호 미수신", Action="TPU 상태 확인, 타임아웃 늘리기" },
                new AlarmDefinition { Code="OS-BINCAM", Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Error,
                    Title="BinCamera X 이동 실패", Cause="BinCameraX 이동 후 알람", Action="알람 리셋, 인터락 확인" },

                // ── OutputUnloader ──
                new AlarmDefinition { Code="OUT-FULL-GOOD", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Error,
                    Title="Good 카세트 가득", Cause="Good1/Good2 모두 25슬롯 가득", Action="카세트 교체" },
                new AlarmDefinition { Code="OUT-FULL-NG", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Error,
                    Title="NG 카세트 가득", Cause="NG 카세트 25슬롯 가득", Action="카세트 교체" },
                new AlarmDefinition { Code="OUT-STORE", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Error,
                    Title="웨이퍼 저장 실패", Cause="StoreFullWafer 실패", Action="인터락/축 확인, 수동 복귀" },
                new AlarmDefinition { Code="OUT-STORE-EX", Category=AlarmCategory.System, DefaultSeverity=AlarmSeverity.Error,
                    Title="웨이퍼 저장 예외", Cause="StoreFullWafer 내부 예외", Action="EventLog 확인",
                    ManualName="ProtrusionDetected", ManualLocator="DieTransfer/BinLifter/*Plate" },

                // ── Stage 60 — 미등록 호출 코드 신규 등록 ──
                new AlarmDefinition { Code="AXL-OPEN", Category=AlarmCategory.System, DefaultSeverity=AlarmSeverity.Critical,
                    Title="AXL 보드 오픈 실패", Cause="AXL DLL 로드 후 보드 초기화 실패", Action="보드 전원 / PCI 슬롯 확인 후 재기동",
                    TitleEn="AXL board open failed", CauseEn="AXL DLL loaded but board init failed", ActionEn="Check board power / PCI slot, restart" },
                new AlarmDefinition { Code="AXL-DLL", Category=AlarmCategory.System, DefaultSeverity=AlarmSeverity.Critical,
                    Title="AXL DLL 로드 실패", Cause="AXL.dll 미존재 또는 버전 불일치", Action="AJINEXTEK 드라이버 재설치",
                    TitleEn="AXL DLL load failed", CauseEn="AXL.dll missing or version mismatch", ActionEn="Reinstall AJINEXTEK driver" },
                new AlarmDefinition { Code="ALIGN-EX", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Error,
                    Title="얼라인 예외", Cause="VisionAlign 시퀀스 내부 예외", Action="EventLog 확인 후 비전 PC 상태 점검",
                    TitleEn="Align exception", CauseEn="Internal exception in vision-align sequence", ActionEn="Check EventLog and Vision PC state",
                    ManualName="OutOfTolerance", ManualLocator="DieTransfer/WaferTransfer/WaferVision/MachineVision/Services/Aligner" },
                new AlarmDefinition { Code="LOT-NOCASS", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Warning,
                    Title="카세트 미감지", Cause="로드포트 카세트 안착 센서 OFF", Action="카세트 안착 후 재시도",
                    TitleEn="Cassette not detected", CauseEn="Load-port cassette sensor OFF", ActionEn="Place cassette and retry",
                    ManualName="MaterialDoesNotExist", ManualLocator="DieTransfer/WaferLifter" },
                new AlarmDefinition { Code="LOT-SCAN", Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Warning,
                    Title="카세트 스캔 실패", Cause="ElevatorZ 이동 실패 (스캔 도중)", Action="알람 클리어 후 수동 HOME, 재스캔",
                    TitleEn="Cassette scan failed", CauseEn="ElevatorZ move failure during scan", ActionEn="Clear alarm, manual HOME, rescan",
                    ManualName="InvalidScanData", ManualLocator="DieTransfer/WaferLifter/SlotMapper" },
                new AlarmDefinition { Code="LOT-MOVE", Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Error,
                    Title="카세트 슬롯 이동 실패", Cause="ElevatorZ 알람 또는 Protrusion 감지", Action="육안 확인 — 돌출 웨이퍼 제거",
                    TitleEn="Cassette slot move failed", CauseEn="ElevatorZ alarm or protrusion detected", ActionEn="Visual check — remove protruding wafer",
                    ManualName="CannotMove", ManualLocator="DieTransfer/WaferLifter (Interlock)" },
                new AlarmDefinition { Code="LOT-EX", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Error,
                    Title="카세트 교환 위치 이동 실패", Cause="피더 클램프 미파지 또는 FeederY 알람", Action="피더 홈 복귀 후 재시도",
                    TitleEn="Cassette exchange move failed", CauseEn="Feeder clamp not gripped or FeederY alarm", ActionEn="Return feeder to home and retry",
                    ManualName="CannotMove", ManualLocator="DieTransfer/WaferFeeder/Arm" },
                new AlarmDefinition { Code="LOT-RET", Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Error,
                    Title="피더 후퇴 실패", Cause="RetractFeeder 단계 알람 (클램프/FeederY/상승)", Action="피더 수동 복귀 후 재시도",
                    TitleEn="Feeder retract failed", CauseEn="Alarm in RetractFeeder step (clamp/FeederY/up)", ActionEn="Manually return feeder and retry",
                    ManualName="CannotMove", ManualLocator="DieTransfer/WaferFeeder/Arm" },
                new AlarmDefinition { Code="IS-LOAD", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Warning,
                    Title="InputStage 로드 실패", Cause="LoadAndPrepareWafer 단계 실패", Action="피더 위치 / 익스팬더 / 바코드 확인",
                    TitleEn="InputStage load failed", CauseEn="LoadAndPrepareWafer step failed", ActionEn="Check feeder/expander/barcode",
                    ManualName="MaterialDoesNotExistAfterReceive", ManualLocator="DieTransfer/WaferTransfer/Stage" },
                new AlarmDefinition { Code="IS-EXCEPTION", Category=AlarmCategory.System, DefaultSeverity=AlarmSeverity.Error,
                    Title="InputStage 사이클 예외", Cause="MachineController.LoadInputStage 내부 예외", Action="EventLog 확인 후 재기동",
                    TitleEn="InputStage cycle exception", CauseEn="Internal exception in LoadInputStage", ActionEn="Check EventLog and restart" },
                new AlarmDefinition { Code="OS-RECEIVE", Category=AlarmCategory.Material, DefaultSeverity=AlarmSeverity.Warning,
                    Title="OutputStage 수신 실패", Cause="ReceiveDie 단계 실패 (Avoid/WorkZ/MoveY)", Action="OutputStage 수동 회피 후 재시도",
                    TitleEn="OutputStage receive failed", CauseEn="ReceiveDie step failed (Avoid/WorkZ/MoveY)", ActionEn="Manually avoid OutputStage and retry",
                    ManualName="MaterialDoesExistAfterInitialize", ManualLocator="DieTransfer/BinTransfer/GoodStage/Plate" },
                new AlarmDefinition { Code="OS-EXCEPTION", Category=AlarmCategory.System, DefaultSeverity=AlarmSeverity.Error,
                    Title="OutputStage 사이클 예외", Cause="MachineController.ReceiveDieAtOutputStage 내부 예외", Action="EventLog 확인 후 재기동",
                    TitleEn="OutputStage cycle exception", CauseEn="Internal exception in ReceiveDieAtOutputStage", ActionEn="Check EventLog and restart" },
                new AlarmDefinition { Code="OS-BININSP", Category=AlarmCategory.Vision, DefaultSeverity=AlarmSeverity.Warning,
                    Title="Bin 검사 실패", Cause="BinCamera 안착 검사 NG", Action="배치 위치 재조정, 비전 ROI 확인",
                    TitleEn="Bin inspection failed", CauseEn="BinCamera placement inspection NG", ActionEn="Re-adjust placement, check vision ROI",
                    ManualName="FailedInspection", ManualLocator="DieTransfer/BinTransfer/BinVision/MachineVision/Services/PlacementInspector" },
                new AlarmDefinition { Code="OS-BININSP-EX", Category=AlarmCategory.System, DefaultSeverity=AlarmSeverity.Error,
                    Title="Bin 검사 예외", Cause="InspectBinPosition 내부 예외", Action="EventLog 확인 후 재기동",
                    TitleEn="Bin inspection exception", CauseEn="Internal exception in InspectBinPosition", ActionEn="Check EventLog and restart" },
                new AlarmDefinition { Code="TPU-PLACE", Category=AlarmCategory.Motion, DefaultSeverity=AlarmSeverity.Warning,
                    Title="TPU Place 실패", Cause="TPU PlaceDies 단계 실패 (Place 위치 또는 배출)", Action="픽커 콜렛 / 진공 점검 후 재시도",
                    TitleEn="TPU Place failed", CauseEn="TPU PlaceDies step failed (Place pos or discharge)", ActionEn="Check picker collet/vacuum and retry",
                    ManualName="CannotMove", ManualLocator="DieTransfer/PickAndPlaceDieTransfer/Tool/PickerN" },
            };
        }
    }
}
