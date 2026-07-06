# COLLET8_RESTRUCTURE_PLAN — 8콜렛(Front4+Back4) 구조 전환 계획

작성: 2026-07-04 · 상태: **P1~P4 구현 완료(2026-07-04)** — 잔여: P5 UI, 장비 PC 빌드/사이클 검증

## 1. 실제 장비 구조 (변경 목표)

- 콜렛 총 **8개** = Front 4개 + Back 4개 (기존 가정 4개는 오류)
- 공정 순서(순차, 동시 아님) — **2026-07-04 사용자 확정**:
  1. Front 콜렛 1~4 Bottom 촬영 (순차) → 완료
  2. Front Side 카메라로 콜렛 1~4 순차 촬영 (콜렛당 0° → 90°) → 완료
  3. Back 콜렛 1~4 Bottom 촬영 (순차) → 완료
  4. Back Side 카메라로 콜렛 1~4 순차 촬영 (콜렛당 0° → 90°) → 완료
- **상호배제**: 한쪽(F/B) 콜렛 4개가 촬영 중이면 다른 쪽은 촬영 불가
- 커버리지: Front 콜렛 다이=Front 카메라만, Back 콜렛 다이=Back 카메라만 (1:1)
- 주소 체계: `[FRONT/BACK][콜렛][DIE번호]` → Front=`[0][1~4][die]`, Back=`[1][1~4][die]`
- **모든 검사/패턴은 콜렛 기준 독립 인스턴스** (8세트)

## 2. 현재 구조 분석 (근거 위치)

### 2.1 Vision 측 — "픽커 4개" 가정이 박힌 곳

| 위치 | 내용 |
|---|---|
| `QMC.Vision\Sequencing\Common\ToolSequence.cs:194` | `BatchPickerCount() => 4` — 배치 그랩 수 하드코딩 |
| `ToolSequence.cs:143` | `picker = ((seq-1) % 4) + 1` — 픽커 순환, FB 구분 없음 |
| `ToolSequence.cs:131~136` | **Side 촬영을 Front/Back 동시로 모델링** (앞=ch0/1, 뒤=ch2/3, 두 모듈 병렬) ← 실기와 다름 |
| `Equipment\Core\AsyncInspectCore.cs:37~38` | 4장 모이면 자동 병렬 검사 트리거 |
| `Equipment\Core\InspectionResultStore.cs:98,117,202,212` | `picker 1~4`, `channel 0~3` 고정 배열/검증 |
| `Equipment\Core\PendingGrabStore.cs` | 모듈별 그랩 보관 — 키에 FB 없음 |
| `Equipment\Core\AsyncMatchStore.cs` | 키 = (모듈, finder/inspector, chip_uid) — FB/콜렛 없음 |
| `Ui\Controls\InspectionViewerControl.cs:212,227` | `p = 1..4` UI 패널 고정 |

### 2.2 TCP 프로토콜 (현행 와이어 포맷)

- `VisionTcpServer.cs:334` — `MODULE|INSPECTASYNC|inspector|picker_id|chip_uid[|die_index[|channel]]`
  - `picker_id` = 1~4만. Front/Back 구분 필드 **없음**
  - `channel` = Side 전용 0~3 (앞 0°/90°=0/1, 뒤=2/3) — FB를 채널에 암묵 인코딩
- 핸들러 송신: `VisionTcpClient.cs:313~317` `InspectAsyncStartAsync(inspector, picker, chipUid, dieIndex)`
- `VisionAdapters.cs:207` — `pickerNo*10 + side` 식 인덱스 패킹(SurfaceInspector) — 임시 인코딩 산재
- 결과 폴링 `INSPECTRESULT|inspector|chip_uid` — chip_uid 키라 FB 추가에 상대적으로 안전

### 2.3 핸들러 시퀀스

- `PickerBottomAndSideInspectionSequence.cs` — Front/Rear **각각 별도 인스턴스**로 존재 (side 파라미터)
  - 픽커별: Bottom shot ×4 → Side 0°/90° (파이프라인 병렬화 포함)
  - Front/Rear 시퀀스는 InspectionArea 리소스로 조정되나 기본 **병렬 지향** ← 실기는 Front 완료 후 Back **순차**
- Bottom/Side EXPOSE 트리거: `TriggerBottomInspectionExposeAsync(pickerNo, ...)` — pickerNo(1~4)만 전달, FB는 어느 클라이언트/모듈로 보내는지에 암묵 의존

### 2.4 검사기 인스턴스

- `CDTInspector`는 CDT-310 네이티브 래퍼(픽커별 아님). 배치 시 `CopyInspectorConfig`로 픽커별 임시 인스턴스 생성(4개) — 콜렛 개념 없음
- 콜렛 패턴(ColletFinder/FlatColletFinder) — 콜렛별 독립 패턴 8개 아님

## 3. 변경 설계안

### 3.1 주소 체계 (SSOT)

```
ColletKey = (fb: 0=Front/1=Back, collet: 1~4)
내부 편의 인덱스: pickerGlobal = fb*4 + collet   (1~8)
검사 단위 키: (fb, collet, dieNo[, channel])
```

### 3.2 와이어 포맷 (확정안)

```
MODULE|INSPECTASYNC|inspector|fb|collet|die_index|channel|chip_uid   (고정 8파트, 생략 없음)
MODULE|INSPECTRESULT|inspector|chip_uid                              (유지 — uid 키)
MODULE|MATCHASYNC|finder|fb|collet|die_index|channel|chip_uid        (동일 규칙)
```

- **신/구형 판별 = 파트 수**: 구형 최대 7파트(`inspector|picker|chip_uid|die_index|channel`) vs 신형 고정 8파트 → `parts.Length == 8`이면 신형. 중간 생략형이 없어 모호성 제거
- chip_uid를 맨 뒤(가변 길이 문자열)에 두고 숫자 필드는 위치 고정
- **메뉴얼 테스트(다이 없음)**: die_index=-1 → Vision은 맵 셀 매칭·다이 데이터 집계를 생략하고 검사/결과 응답만 수행. 기존 "uid가 숫자면 die_index로 대체" 폴백(VisionTcpServer.cs:345)은 die_index=-1일 때 발동 금지

| 필드 | 값 | 역할 |
|---|---|---|
| fb | 0=Front / 1=Back | 콜렛 그룹 |
| collet | 1~4 | 기존 picker_id 자리 |
| chip_uid | 다이 고유 ID (`Die.cs:31` Guid 12자리, MaterialStorage 키) | 결과 매칭 키 — INSPECTRESULT 회수·검사기 간 다이별 집계. **offset/칩위치 아님, 삭제 불가** |
| die_index | 픽업 순서 1-base, **-1=다이 없음(메뉴얼 테스트)** | 웨이퍼맵 셀 매칭(칩위치는 이 값 담당). -1이면 맵 매칭/다이 집계 생략, 검사만 수행 |
| channel | **항상 0/1** — Side 0=0°/1=90°, Bottom/Bin은 0°로 간주해 **0** (2026-07-04 확정: -1 미사용, 구형 수신만 허용) | 기존 0~3의 FB 인코딩(앞=0/1, 뒤=2/3) 폐기 |

- Sim 셀프런(`TcpLoopbackVisionCommandDispatcher`)은 인자 패스스루라 ToolSequence만 고치면 동일 반영 (Sim==Real 원칙)

### 3.3 Vision 코어

1. `AsyncInspectCore`/`AsyncMatchStore`/`PendingGrabStore`: 키에 `(fb, collet)` 추가. 배치 트리거 = **FB 그룹당 4장** (사이클당 2배치)
2. `InspectionResultStore`: `[picker 1~4]` → `[fb 0~1][collet 1~4]` (또는 pickerGlobal 1~8)
3. **콜렛별 런타임 인스턴스 8세트** (2026-07-04 확정): 파라미터/티칭은 레시피 1벌 공유, 처리 속도용 실행 인스턴스만 `(fb, collet)` 키 딕셔너리 8개 — `CopyInspectorConfig` 방식 확장
4. 레시피: **슬롯 확장/마이그레이션 불필요** (파라미터 공유 확정). 기존 레시피 그대로 사용

### 3.4 Vision 시퀀서 (ToolSequence)

- `BatchPickerCount` 하드코딩 제거 → FB 그룹 구조로: Front 배치(4) → Front Side → Back 배치(4) → Back Side **순차**
- 기존 "Front/Back 동시(두 모듈 병렬)" 모델(131~136행 주석 로직) 폐기

### 3.5 핸들러 시퀀스

- Front `PickerBottomAndSideInspectionSequence` 완료 → Rear 시작의 **순차 게이트** 추가 (기존 병렬 지향 → 실기 순서와 일치)
- EXPOSE/INSPECTASYNC 송신에 `fb` 명시 (`VisionTcpClient` 오버로드 확장, `pickerNo*10+side` 패킹 제거)

### 3.6 UI / 뷰어

- InspectionViewerControl 등 1~4 고정 패널 → Front/Back 탭 or 8열
- Bottom 4-맵/PositionMap 픽커 키 확장

## 4. 확인 현황 (2026-07-04)

**확정됨**
1. ~~Side 커버리지~~ → Front 콜렛=Front 카메라만, Back 콜렛=Back 카메라만 (1:1), 상호배제
2. ~~0°/90°~~ → 콜렛당 0° → 90° 순차 유지 (channel 0/1)
3. ~~와이어 포맷~~ → 3.2 확정안 (fb 분리 필드 + chip_uid 유지)
4. ~~Bottom 카메라~~ → Front 배치 → Back 배치 순차 사용
5. ~~검사/패턴 인스턴스~~ → 콜렛 기준 독립 8세트 (F4+B4)

6. ~~인스턴스 방식~~ → 파라미터/티칭 공유 + **런타임 인스턴스 8개** (속도 목적)
7. ~~레시피 마이그레이션~~ → 불필요 (파라미터 공유라 기존 레시피 유지)

**미확정 없음 — 코드 작업 가능**

## 5. 단계별 작업 계획 (승인 후)

| 단계 | 내용 | 범위 | 상태(2026-07-04) |
|---|---|---|---|
| P1 | 주소체계/키 구조 (ColletAddress 신설, 스토어 1~8 확장, 하위호환 파서) | Vision 코어 | **완료** |
| P2 | 와이어 신형 8파트 (서버/라우터/디스패처 파싱 + 핸들러 Client/Service/Adapter 송신) | 프로토콜 양측 | **완료** |
| P3 | (fb,collet[,ch]) 영속 런타임 인스턴스 — ColletInspectorCache 신설, 레시피 1벌 공유 | Vision | **완료** |
| P4 | 시퀀스 순차화 — Vision ToolSequence F→B 배치 + Side F/B 게이트. 핸들러는 기존 InspectionArea 배타 + WaitOppositePendingSide 로 이미 보장 확인 | 핸들러+Vision | **완료** |
| P5 | UI 8콜렛 확장 (InspectionViewerControl p=1..4 루프, 4맵/픽커 패널) | Vision UI | 미착수 |
| P6 | 검증 — 정적 검사 수행(수정 파일 전체 인코딩/괄호 OK). verify_all 실패분은 기존 트리 노후 베이스라인(파일 이동)으로 본 작업과 무관 확인. **MSBuild 컴파일 + --auto-cycle 은 장비 PC에서 필요** | 전체 | 부분 완료 |

### 구현 파일 목록 (2026-07-04)

- 신규: `QMC.Vision\Equipment\Core\ColletAddress.cs`(주소 SSOT+파서), `ColletInspectorCache.cs`(콜렛별 영속 인스턴스) — csproj 등록됨
- Vision: VisionTcpServer(DoInspect/DoInspectAsync/DoMatch/DoMatchAsync 신형 8파트), VisionCommandRouter, DirectVisionCommandDispatcher(신형 6인자), AsyncInspectCore(캐시 사용·배치 주석), PendingGrabStore/InspectionResultStore/WaferDataSaver(픽커 1~8), ToolSequence(RunColletBatchesAsync — Bottom/Bin F→B 순차·Side 모듈=fb·상호배제 게이트, 전역 다이순번 환산), VisionModule(90° 채널 주석)
- 핸들러: VisionTcpClient(신형 오버로드 5종), VisionCommandService(신형 래퍼 5종), AutoVisionRequestService(MatchColletAsync/WaitMatchResultByUidAsync/MatchBottomOffsetAsync(fb,collet)/InspectColletAsync), VisionAdapters(TpuVisionAdapter Fb 유도, 바텀 8콜렛 uid "F1"~"B4", 측면 ch0/1 — pickerNo*10+side 패킹 제거[INSPECT], SideVisionResult 3/4=미사용 true)
- 구형 와이어(≤7파트)는 전부 하위호환 유지

> 원칙 준수: CDT-310 알고리즘 코어 무변경, Sim==Real, 통신 포맷 변경은 본 건 명시 승인 범위 내.

## 5.5 검사 백엔드 즉시 처리 전환 (2026-07-04 확정)

- 구(방식B) "그랩 4장 축적 → 일괄 병렬" **폐기** — 콜렛별 독립 인스턴스(8세트)가 있으므로 모을 이유 없음.
- 신규: 요청 1건 = 그랩(모듈 게이트 직렬화, 카메라 보호) → **그랩 완료 즉시 해당 콜렛·채널 인스턴스로 검사**. 검사는 게이트 밖이라 다음 그랩과 자연 병렬.
- chip_uid 그룹 합산: Side=2건(0°/90°), Bottom/Bin=1건 — 기대 수 도달 시 1회 Complete(모두 PASS여야 PASS). `ExpectedPerUid` 로 일원화, `ExpectedBatchCount`/PendingGrabStore 사용 제거(파일은 보존).
- STARTED 선응답 의미 확정: 실기도 "빠른 스텝 이동을 위해 그랩 전 미리 응답"이 맞음(비전은 즉시 그랩 수행). 촬상 완료 동기화가 필요한 지점은 EPD 푸시 사용.
- Side 보정 흐름 확정: Bottom 전 촬영 완료 → Side가 Bottom XYT **Offset 기준 보정 후** 촬영(수식 반영 위치 = InspectSideTargetAsync 훅).

## 6. Bottom XYT 어싱크 푸시 (EventSearchDieEnd, 2026-07-04 추가 구현)

**요구**: Bottom 외곽(패턴) 탐색이 끝나는 즉시 해당 다이의 X/Y/T 를 어싱크 이벤트로 핸들러에 전달 — Side 공정이 사용. 칩핑/이물(CUDA) 검사 완료를 기다리지 않는다.

**와이어(신규 푸시, EPD/ARM 계열)**: `XYT|MODULE|fb|collet|chip_uid|x=..;y=..;t=..;ix=..;iy=..`
— x/y=px(Vision 최종 result.Offset 규약: ChipRoi 보정+0.5 스케일+X/Y 스왑), t=deg, 응답 큐와 무관.
페이로드 끝에 `valid=0|1` 포함 — **미검출 정책(2026-07-04 확정): 외곽 미검출이면 x/y/t 전부 0 으로 송신하고 valid=0, 핸들러/Side 는 정지하지 않고 진행.** Side 의 XYT 사용 목적은 사이드 위치 보정으로 추정(정확한 수식 미확정 — 훅에서 로그만, 확정 시 적용).

**경로**:

1. `VisionInspector\CDTInspector.cs` — `SearchDieEnd` static 이벤트 신설, `BottomInspect` 의 외곽 확정 지점(주석 "EventSearchDieEnd", CUDA 칩핑 이전)에서 좌표 '사본'으로 최종 규약과 동일한 XYT 를 선계산해 발화. **코어 알고리즘 무변경**(CDT-310 원칙 준수 — 이벤트 발화만 추가).
2. `QMC.Vision\Equipment\Core\BottomXytPushService.cs`(신규) — 검사 스레드 로컬 컨텍스트(모듈/전역픽커/uid, `VisionCommandCore.InspectOnImageExplicit` 가 주입)와 결합해 백그라운드 Task 로 푸시. 픽커 식별 불가(구형 수동)면 생략.
3. `VisionTcpServer` — 모듈명→서버 정적 레지스트리 + `PushBottomXyt()` 브로드캐스트.
4. 핸들러 `VisionTcpClient` — 수신 루프에서 XYT 푸시 파싱 → `BottomXytStore`(신규, (fb,collet)·uid 최신 보관) 기록 + `BottomXytReceived` 이벤트.
5. `PickerBottomAndSideInspectionSequence.InspectSideTargetAsync` — Side 진입 시 (fb, collet) XYT 조회/로그 훅. **보정 반영 수식은 공정 담당 확정 대기(TODO)** — 확정 시 이 지점에서 target 에 적용.

**한계/메모**: 레거시 폴백(InspectLegacy) 경로는 이벤트 미발화(라이브러리 경로 전용). BottomXytStore.Clear() 랏 경계 호출은 미배선(로그 전용 단계라 무해). Sim 셀프런 루프백은 푸시를 무시(핸들러 연결 시에만 소비).
