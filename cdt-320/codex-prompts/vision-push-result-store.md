# 작업: Vision 검사 결과 수신 구조 개편 — 요청·폴링(Pull) 제거, 푸시(Push) + 수신 스토어 방식으로 전환

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 주 수정 파일:
  - `QMC.CDT-320\Equipment\Vision\VisionTcpClient.cs` (ReceiveLoop 라우팅)
  - `QMC.CDT-320\Equipment\Vision\VisionTcpClient.Correlated.cs` (결과 요청 경로 제거)
  - `QMC.CDT-320\Equipment\Vision\AutoVisionRequestService.Correlated.cs` (결과 대기 로직 교체)
  - `QMC.CDT-320\Equipment\Vision\VisionInspectionProtocol.cs` (푸시 파싱 지원)
  - 신규: 검사 결과 수신 스토어 클래스 — `QMC.CDT-320\Equipment\Vision\` 아래
  - 신규: 프로토콜 계약 문서 — `cdt-320\vision-push-protocol.md` (아래 R6)
- 신규 .cs 파일은 `.csproj`(구식 Compile Include 형식이면)에 등록.
- **비전 소프트웨어는 별도 작업으로 수정 예정** — 이번 작업은 핸들러(이 저장소)만 수정하고,
  비전이 따라야 할 푸시 계약을 문서(R6)로 확정하는 것까지가 범위다.

## 배경 — 현재 구조 (수정 전 반드시 해당 파일들을 읽을 것)

### 수신 리스너 (유지)
`VisionTcpClient.ReceiveLoop()` (VisionTcpClient.cs 약 590행)가 연결별 백그라운드 수신자다.
수신 라인 라우팅 순서: ① `TryRouteInspectionProtocolResponse(response)` — 신규 검사 규약을
request_id/group_id 대기자(`_exposureWaiters`/`_resultWaiters`)에 매칭, ② 비동기 푸시
EPD/ARM/RECIPEREQ/XYT → 이벤트 발행 (XYT는 `BottomXytStore` 기록 포함), ③ legacy FIFO `_pending`.

### 검사 요청/EPD (유지 — 이미 원하는 구조)
`SendInspectionRequestAsync(envelope)` (VisionTcpClient.Correlated.cs 약 30행) —
INSPECT 요청 송신 후 request_id로 EPD 매칭 → `VisionRequestHandle` 반환.
시퀀스는 EPD 수신 후 즉시 다음 콜렛으로 모션을 진행한다. **이 경로는 변경하지 않는다.**

### 결과 회수 (제거 대상 — 요청+폴링 Pull 방식)
- `RequestInspectionResultAsync(camera, command, groupId, ...)` (VisionTcpClient.Correlated.cs 약 85행):
  `"CAMERA|MRESULT|group_id=..."` / `"CAMERA|RESULT|group_id=..."` 라인을 핸들러가 먼저 송신하고
  응답을 `_resultWaiters`로 대기.
- `AutoVisionRequestService.WaitInspectionStageAsync(handle, command, timeoutMs, ct)`
  (AutoVisionRequestService.Correlated.cs 약 90행): 위 요청을 보내고 비전이 `PENDING`이면
  `CorrelatedResultPollIntervalMs` 주기로 **같은 요청을 재송신하는 폴링 루프**.
  `WaitBottomMResultAsync`(약 216행)도 동일 계열.
- 이 Pull 방식을 **완전히 제거**하고 푸시+스토어로 대체한다 (fallback 유지하지 않음).

### 선례 패턴 (참고)
Bottom XYT 푸시: 비전이 자발 송신 → ReceiveLoop가 `BottomXytStore` 기록 + `BottomXytReceived`
이벤트 발행 → Side 공정이 폴링 없이 스토어 조회. 이번 작업은 이 패턴을 MRESULT/RESULT로 일반화한다.

## 목표 구조

```
핸들러: INSPECT 요청 ──────────────→ 비전
핸들러: ←── EPD (request_id)          비전  → 핸들러는 즉시 다음 콜렛 진행 (기존 유지)
핸들러: ←── MRESULT 푸시 (group_id)   비전  → 리스너가 수신 스토어에 보관 + 도착 이벤트
핸들러: ←── RESULT 푸시 (group_id)    비전  → 리스너가 수신 스토어에 보관 + 도착 이벤트

Side/Bin 등 소비 시퀀스: 필요 시점에 스토어 조회 →
  있으면 즉시 소비(스토어에서 제거) / 없으면 도착 이벤트를 타임아웃까지 대기. 재요청 없음.
```

- 적용 채널: **BOTTOM / SIDE / BIN / WAFER 전부 동일 방식** (correlated 검사 규약을 쓰는 모든 채널).
- 구형(legacy) 수동/셋업용 경로(`SendAsync` FIFO, `MatchAsyncStartAsync`+`MATCHRESULT` 폴링,
  `PollInspectResultAsync` 등)는 **이번 범위에서 제외** — 수정하지 않는다.

## 구현 항목

### R1. 푸시 수신 라우팅 (VisionTcpClient)
- ReceiveLoop 라우팅에 MRESULT/RESULT **푸시 분기**를 추가한다.
  Pull 요청이 사라지므로, 수신되는 MRESULT/RESULT 라인은 전부 자발 푸시로 간주한다.
- 라인에서 `group_id`(필수)와 `request_id`, 채널/카메라, fb/collet/die_index 등 상관관계 키를
  파싱한다 (`VisionInspectionProtocol.cs`의 기존 파서 `VisionInspectionResult.Parse` 재사용/확장).
- `group_id`가 없는 MRESULT/RESULT 라인은 폐기하고 경고 로그를 남긴다 (FIFO `_pending`으로
  흘려보내지 않는다 — legacy 응답 오염 방지).
- 파싱 성공 시: 수신 스토어(R2)에 저장 + 도착 이벤트 발행.

### R2. 검사 결과 수신 스토어 (신규 — 가칭 `VisionInspectionResultStore`)
- 키: (채널/카메라, 결과 종류 MRESULT|RESULT, group_id). 값: 파싱된 결과 + 수신 시각 + 원문 라인.
- API (스레드 안전, lock):
  - `Add(result)` — 리스너에서 호출. 동일 키 중복 수신 시 최신 값으로 교체 + 로그.
  - `TryConsume(camera, command, groupId, out result)` — **조회 성공 시 스토어에서 제거**(소비 후 제거).
  - `WaitAndConsumeAsync(camera, command, groupId, timeoutMs, ct)` — 있으면 즉시 소비,
    없으면 도착 이벤트를 대기(타임아웃 포함). 이벤트 경합(등록 직전 도착)이 없도록
    등록→재조회 순서로 구현.
  - `Clear(reason)` / 연결 단절 시 정리(아래).
- **보관 한도: 미소비 항목 500개.** 초과 시 가장 오래된 항목부터 제거(FIFO)하고 경고 로그
  (제거된 항목의 키·수신 시각 포함).
- **연결 단절(Disconnect) 시**: 해당 채널의 미소비 항목 전부 제거 + 로그 (재연결 후 이전 검사
  결과는 무효 — 기존 `Disconnect()`의 waiter 정리와 같은 위치에서 수행).
- 스토어는 채널(모듈)별 인스턴스로 두거나 전역 1개에 채널 키를 포함 — 기존 `BottomXytStore`
  구조를 참고해 일관되게 선택하고 주석으로 근거를 남긴다.

### R3. 결과 대기 로직 교체 (AutoVisionRequestService)
- `WaitInspectionStageAsync(handle, command, timeoutMs, ct)`의 **공개 시그니처·반환 의미는
  유지**하고 내부만 교체한다: 요청 송신+PENDING 폴링 루프 제거 →
  `스토어.WaitAndConsumeAsync(camera, command, handle.Request.GroupId, ...)`로 대체.
  - 소비 성공 시 기존과 동일하게 `handle.MarkStageDone(command, parsed)` 호출, 에러 응답이면
    기존과 동일하게 `handle.MarkError(...)` 처리 — **호출부(시퀀스들)는 코드 변경 없이 동작**해야 한다.
  - 타임아웃 시 기존 관례대로 `MarkError` + 알람 로그.
  - Bypass(`handle.IsBypassed`)/시뮬레이션 경로는 기존 그대로 유지.
- `WaitBottomMResultAsync` 등 동일 계열 헬퍼도 같은 방식으로 내부 교체.
- `VisionTcpClient.RequestInspectionResultAsync`와 `_resultWaiters` 딕셔너리는 사용처가
  사라지므로 제거한다 (다른 호출처가 없는지 반드시 전수 확인 후 제거; 남아 있으면 이유를
  보고하고 유지).
- `CorrelatedResultPollIntervalMs`, PENDING 재요청 카운터(`MResultPendingCount` 등) 등
  폴링 전용 잔재는 제거하되, `VisionRequestHandle`의 타이밍 통계(RoundTrip 등)는
  푸시 도착 기준으로 의미를 재정의해 유지한다 (로그 필드명 유지, 주석으로 의미 변경 명시).

### R4. 전 채널 적용 확인
- BOTTOM/SIDE/BIN/WAFER 채널에서 correlated 규약으로 결과를 회수하는 모든 경로가
  R3의 새 대기 함수를 타는지 전수 확인한다 (`WaitInspectionStageAsync` 호출처:
  `PickerBottomInspectionSequence`, `PickerBottomAndSideInspectionSequence`,
  `PickerSideInspectionSequence`, `OutputPostPlaceInspectionQueue`, Wafer 계열 등 — 전부 검색해서
  나열하고, 시퀀스 코드 수정 없이 동작함을 확인).

### R5. 진단/로그
- 푸시 수신 시: 채널, 종류, group_id, 수신까지의 경과(EPD→푸시), 스토어 잔량을 EventLogger 기록.
- 소비 시: 대기 시간(조회 즉시 소비=0), group_id, 잔량 기록.
- 한도 초과 제거·미소비 폐기·group_id 없는 라인 폐기는 경고 레벨로 기록.

### R6. 푸시 프로토콜 계약 문서 (신규 — `cdt-320\vision-push-protocol.md`)
비전 소프트웨어 수정 작업(별도 진행)이 따라야 할 계약을 이 작업에서 확정·문서화한다:
- INSPECT 요청 라인 포맷 (기존 그대로 — envelope의 request_id/group_id 포함 필드 명세)
- EPD 푸시 포맷 (기존 그대로)
- **MRESULT/RESULT 푸시 라인 포맷 (신규 확정)** — 기존 요청-응답의 응답 페이로드 포맷을
  기반으로 하되 자발 푸시로 전송됨을 명시. 필수 필드: 채널/카메라, 종류, group_id,
  request_id, fb/collet/die_index(해당 시), status(OK|NG|ERR), 오프셋 등 결과 값.
  실제 필드 구성은 `VisionInspectionProtocol.cs`의 기존 파서가 받아들이는 형태를 기준으로
  확정하고 예시 라인을 문서에 포함한다.
- 송신 순서 보장: 한 검사 요청에 대해 EPD → MRESULT(BOTTOM INSPECT만) → RESULT 순서로
  각 단계 완료 즉시 송신.
- 핸들러는 PENDING을 더 이상 보내지도 받지도 않음을 명시.

## 제약 사항
- `SendInspectionRequestAsync`(EPD 경로), 푸시 이벤트(EPD/ARM/RECIPEREQ/XYT), legacy
  `SendAsync`/FIFO 경로, 수동/셋업용 폴링(`MatchAsyncStartAsync` 등)은 수정하지 않는다.
- `AutoVisionRequestService`의 공개 API 시그니처 유지 — 검사 시퀀스 파일들은 수정하지 않는 것이
  원칙이다. 불가피하게 시퀀스 수정이 필요하면 그 이유를 보고서에 명시한다.
- 코드 스타일: 한국어 주석, EventLogger/Log.Write 관례, try/catch/finally 관례, 최신 C# 문법 자제.
- 스레드 안전: 스토어는 리스너 스레드(Add)와 시퀀스 스레드(Consume/Wait)가 동시 접근한다.
- 시뮬레이션/DryRun/Bypass 경로는 기존 동작 유지.

## 검증 / 수용 기준
1. 솔루션 전체 빌드 통과. 신규 파일 csproj 등록.
2. 단위/하네스 검증 (결과 보고):
   - **푸시 선도착**: 스토어에 MRESULT를 먼저 넣고 `WaitInspectionStageAsync` 호출 → 즉시 반환,
     스토어에서 제거됨.
   - **대기 후 도착**: 대기 시작 후 푸시 주입 → 이벤트로 깨어나 반환. 등록 직전 도착 경합도
     누락 없이 동작.
   - **타임아웃**: 푸시 미도착 시 기존과 동일한 에러/알람 경로.
   - **소비 후 제거**: 같은 키 재조회 시 없음.
   - **한도 500**: 501개째 추가 시 가장 오래된 항목 제거 + 경고 로그.
   - **단절 정리**: Disconnect 시 미소비 항목 제거.
   - **group_id 없는 결과 라인**: 폐기 + 경고, FIFO `_pending`에 유입되지 않음.
3. `WaitInspectionStageAsync` 호출처 전수 목록과 "시퀀스 무수정" 확인 결과를 보고.
4. `RequestInspectionResultAsync`/`_resultWaiters` 제거 완료 (또는 잔존 사유 보고).
5. `cdt-320\vision-push-protocol.md`가 생성되어 있고, MRESULT/RESULT 푸시 예시 라인과
   필수 필드 명세가 포함되어 있다 (비전 측 작업의 입력 문서가 된다).
