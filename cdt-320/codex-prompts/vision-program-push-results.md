# 작업: QMC.Vision — 검사 결과(MRESULT/RESULT) 자발 푸시(Push) 송신 구현

## 환경
- 저장소: `D:\Source\CDT-320` (C# WinForms, .NET Framework 솔루션)
- 대상 프로젝트: `QMC.Vision`
- 주 수정 파일:
  - `QMC.Vision\Equipment\Core\VisionProtocolExecutionCore.cs`
  - `QMC.Vision\Equipment\Comm\VisionResultSerializer.cs`
  - `QMC.Vision\Equipment\Comm\VisionTcpServer.cs`
  - 결과 상태 저장소(`VisionProtocolResultStore` — 파일 위치는 검색해서 확인) 및
    `QMC.Vision\Equipment\Comm\VisionWireProtocol.cs` (필요 시)
- 이 작업은 **핸들러(장비 제어 SW, 별도 저장소 D:\Source\CDT-320_NEW) 측 변경과 한 쌍**이다.
  핸들러는 MRESULT/RESULT를 더 이상 요청·폴링하지 않고, 비전이 자발 송신하는 푸시를
  수신 스토어에 담아 소비하는 방식으로 변경된다. 이 작업은 그 계약의 비전 측 절반이다.
  (참고 문서가 존재하면 확인: `D:\Source\CDT-320_NEW\cdt-320\vision-push-protocol.md` —
  없어도 이 프롬프트만으로 작업 가능하도록 계약을 아래에 명시한다.)

## 배경 — 현재 구조 (수정 전 반드시 해당 파일들을 읽을 것)

### 검사 실행 흐름 (correlated 규약: request_id/group_id 포함 INSPECT/MATCH)
`VisionProtocolExecutionCore.ExecuteInspection()` (약 167행):
1. ACK(STARTED) 응답 → 백그라운드로 그랩 시작
2. 그랩 완료 → `push(VisionWireProtocol.Epd(request))` — **EPD는 이미 푸시 방식** (약 207행)
3. 영상 처리 → 완료 시 `VisionProtocolResultStore.CompleteMatch()` / `CompleteInspection()`,
   실패 시 `VisionProtocolResultStore.Fail()` — **저장소에 기록만 하고 송신은 하지 않는다**
4. BOTTOM + SurfaceInspector 검사는 MRESULT(측정값)와 RESULT(판정)가 함께 준비된다
   (약 325~327행: "MRESULT Ready" / "RESULT Ready" 택트 마크)

### 결과 회수 (현재: 핸들러가 질의 → PENDING 폴링)
- 핸들러가 `"CAMERA|MRESULT|group_id=..."` / `"CAMERA|RESULT|group_id=..."` 질의를 보내면
  `TryIdentifyQuery()` → `VisionResultSerializer.Serialize(query)` (VisionResultSerializer.cs 약 11행)가
  저장소를 읽어 응답:
  - 아직 처리 중이면 `PENDING` 응답 → 핸들러가 재질의(폴링)
  - BOTTOM은 MRESULT가 먼저 전달돼야 RESULT를 준다 (`MarkMResultDelivered` 게이팅, 약 49~61행)
  - 실패면 ERR 응답
- `push` 콜백 / `VisionTcpServer.Broadcast(line)` (약 582행)이 EPD/ARM/XYT 푸시에 사용 중 —
  MRESULT/RESULT 푸시도 같은 경로를 쓴다.

## 목표 — 단계 완료 즉시 자발 푸시

```
핸들러 → INSPECT 요청 → 비전: ACK(STARTED)                       (기존 유지)
비전   → EPD 푸시 (그랩 완료 즉시)                                 (기존 유지)
비전   → MRESULT 푸시 (BOTTOM SurfaceInspector: 측정값 준비 즉시)   (신규)
비전   → RESULT 푸시 (판정 준비 즉시, BOTTOM은 MRESULT 푸시 후)     (신규)
비전   → ERR 푸시 (처리 실패 즉시)                                  (신규)
```

핸들러는 질의를 보내지 않고 푸시를 수신해 group_id로 매칭한다.

## 푸시 계약 (핸들러와 합의된 사양 — 반드시 준수)

1. **푸시 라인 포맷 = 기존 질의 응답 라인 포맷과 동일.**
   `VisionResultSerializer`의 Envelope 생성 로직을 재사용해, 핸들러가 기존 응답 파서
   그대로 푸시를 파싱할 수 있어야 한다.
2. **`group_id`(필수)와 `request_id`가 라인에 포함**되어야 한다 — 핸들러의 수신 라우팅 키다.
   현재 Envelope에 포함되는지 확인하고, 없으면 질의 응답·푸시 양쪽 포맷에 동일하게 추가한다.
3. **송신 순서 보장** (요청 단위): EPD → (BOTTOM SurfaceInspector이면) MRESULT → RESULT.
   실패 시: EPD 이후 어느 시점이든 ERR 1회.
4. **PENDING은 푸시로 절대 보내지 않는다** (PENDING은 질의 응답 전용 개념으로만 잔존).
5. **재송신 없음**: 연결이 끊겼다 재연결돼도 이전 결과를 다시 푸시하지 않는다
   (핸들러는 미수신 시 타임아웃 알람으로 처리).
6. 같은 결과를 두 번 푸시하지 않는다 (완료 1회당 푸시 1회).

## 구현 항목

### R1. 완료 지점 푸시 발행
- `VisionProtocolResultStore.CompleteMatch()` / `CompleteInspection()` / `Fail()`이 호출되어
  저장소 상태가 확정되는 시점(또는 그 직후 실행 코어)에서, 해당 요청의 결과 라인을 생성해
  푸시 송신한다.
- 라인 생성은 `VisionResultSerializer`의 기존 Envelope 로직을 **질의 객체 없이 저장소
  결과+요청 컨텍스트로부터** 호출할 수 있게 리팩터링(오버로드 추가)해 재사용한다 —
  질의 응답과 푸시가 항상 같은 포맷을 유지하도록 단일 생성 경로로 만든다.
- BOTTOM SurfaceInspector: **MRESULT 라인 먼저, RESULT 라인 나중** 순서로 연속 송신하고,
  기존 `MarkMResultDelivered` 상태도 함께 갱신해 이후 질의가 와도 일관되게 응답되도록 한다.
- 실패(`Fail`) 시: 기존 ERR 응답과 동일 포맷의 ERR 라인을 푸시한다.

### R2. 푸시 송신 경로
- EPD와 동일한 경로를 사용한다: 실행 코어의 `push` 콜백(요청을 받은 서버의 Broadcast로
  연결되어 있음) 또는 `VisionTcpServer.Broadcast(line)`.
- 실행 코어에서 완료 시점에 `push` 콜백을 아직 쓸 수 있으면 그것을 우선 사용하고,
  저장소 계층에서 송신해야 한다면 모듈명으로 `VisionTcpServer.TryBroadcast(moduleName, line)`
  패턴(XYT 푸시와 동일)을 따른다. 어느 쪽을 선택했는지와 이유를 주석으로 남긴다.
- 송신 실패(연결 없음 등)는 예외를 전파하지 않고 로그만 남긴다 (Broadcast 기존 관례).

### R3. 기존 질의-응답 경로 유지 (호환)
- `TryIdentifyQuery` → `Serialize(query)` 질의 응답 경로, PENDING 응답, legacy
  INSPECTRESULT/MATCHRESULT 폴링 명령은 **수정하지 않고 그대로 유지**한다
  (수동/셋업 도구 및 전환기 호환용). 푸시는 추가 동작이다.
- 푸시 후 같은 group_id로 질의가 와도 기존 규칙대로 정상 응답되어야 한다.

### R4. 로그/택트
- 푸시 송신 시점에 택트 마크 추가: "MRESULT PUSH TX" / "RESULT PUSH TX" / "ERR PUSH TX"
  (`VisionProtocolTactLog.Mark` 기존 관례).
- 통신 로그(VisionCommLog 관례)에 푸시 라인 TX 기록 — 단 결과 라인이 길 수 있으므로
  기존 로그 정책(폴링 로그 억제 등)과 일관되게.

## 제약 사항
- EPD/ARM/XYT 푸시, ACK(STARTED)/중복 요청 처리, 저장소 만료 정책, legacy 명령 처리 등
  기존 동작은 변경하지 않는다. 이번 변경은 **완료 시점 푸시 추가 + 라인 생성 경로 통일**이
  전부다.
- 코드 스타일: 기존 파일 관례(한국어 주석, 택트 마크, try/catch 로그 관례) 준수.
- 스레드: 완료는 백그라운드 처리 스레드에서 발생한다. Broadcast가 이미 다중 스레드 송신을
  처리하는 방식(락)을 확인하고 동일하게 따른다. 한 요청의 MRESULT→RESULT 순서가
  뒤바뀌지 않도록 같은 스레드에서 순차 송신한다.

## 검증 / 수용 기준
1. 솔루션 전체 빌드 통과 (MSBuild).
2. 시뮬레이터/테스트 하네스(또는 로컬 TCP 클라이언트 스크립트)로 확인해 결과 보고:
   - BOTTOM SurfaceInspector INSPECT 요청 → 수신 순서가 정확히
     `ACK(STARTED)` → `EPD` → `MRESULT` → `RESULT` 이고 각 라인에 group_id가 포함됨.
   - 일반(BOTTOM 외 또는 비-SurfaceInspector) 요청 → `ACK` → `EPD` → `RESULT` (MRESULT 없음).
   - 강제 실패(존재하지 않는 inspector 등) → ERR 푸시 1회.
   - 같은 결과가 중복 푸시되지 않음.
   - 푸시 이후 기존 방식으로 `CAMERA|RESULT|group_id=...` 질의 시에도 정상 응답 (호환 유지).
   - PENDING이 푸시로 송신되는 경우가 없음.
3. 푸시 라인과 질의 응답 라인이 동일 포맷(동일 생성 경로)임을 코드 근거로 보고.
4. 최종 확정된 푸시 라인 포맷(필드 명세 + 예시 라인)을 작업 보고에 포함한다 —
   핸들러 측 구현이 이 명세를 그대로 사용한다.
