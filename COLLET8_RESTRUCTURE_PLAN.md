# COLLET8_RESTRUCTURE_PLAN — 8콜렛(Front4+Back4) 구조 전환 계획

작성: 2026-07-04 · 상태: **분석 완료, 코드 작업 전** (사용자 확인 후 진행)

## 1. 실제 장비 구조 (변경 목표)

- 콜렛 총 **8개** = Front 4개 + Back 4개 (기존 가정 4개는 오류)
- 공정 순서(순차, 동시 아님):
  1. Front 콜렛 1~4 Bottom 촬영
  2. Front 카메라로 4개 촬영 완료
  3. Back 콜렛 1~4 Bottom 촬영
  4. Back 카메라로 4개 촬영 완료
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

### 3.2 와이어 포맷 변경 (하위호환 고려)

```
MODULE|INSPECTASYNC|inspector|fb|collet|chip_uid[|die_index[|channel]]
MODULE|INSPECTRESULT|inspector|chip_uid            (유지 — uid 키)
MODULE|MATCHASYNC|finder|fb|collet|chip_uid ...    (동일 규칙)
```

- 파서(`VisionTcpServer.DoInspectAsync`, `VisionCommandRouter`)에서 인자 수로 신/구형 판별 → 이행기 호환
- `channel`은 Side 0°/90° 전용으로 축소(0/1) — FB 인코딩 용도(2/3) 폐기
- Sim 셀프런(`TcpLoopbackVisionCommandDispatcher`)은 인자 패스스루라 ToolSequence만 고치면 동일 반영 (Sim==Real 원칙)

### 3.3 Vision 코어

1. `AsyncInspectCore`/`AsyncMatchStore`/`PendingGrabStore`: 키에 `(fb, collet)` 추가. 배치 트리거 = **FB 그룹당 4장** (사이클당 2배치)
2. `InspectionResultStore`: `[picker 1~4]` → `[fb 0~1][collet 1~4]` (또는 pickerGlobal 1~8)
3. **콜렛별 검사기/파인더 인스턴스 8세트**: `CopyInspectorConfig` 확장 → `(fb, collet)` 키 딕셔너리. 패턴(콜렛 파인더 포함) 콜렛별 독립 등록
4. 레시피: 콜렛별 파라미터/패턴 8개 슬롯 (기존 4 → 8 마이그레이션 필요)

### 3.4 Vision 시퀀서 (ToolSequence)

- `BatchPickerCount` 하드코딩 제거 → FB 그룹 구조로: Front 배치(4) → Front Side → Back 배치(4) → Back Side **순차**
- 기존 "Front/Back 동시(두 모듈 병렬)" 모델(131~136행 주석 로직) 폐기

### 3.5 핸들러 시퀀스

- Front `PickerBottomAndSideInspectionSequence` 완료 → Rear 시작의 **순차 게이트** 추가 (기존 병렬 지향 → 실기 순서와 일치)
- EXPOSE/INSPECTASYNC 송신에 `fb` 명시 (`VisionTcpClient` 오버로드 확장, `pickerNo*10+side` 패킹 제거)

### 3.6 UI / 뷰어

- InspectionViewerControl 등 1~4 고정 패널 → Front/Back 탭 or 8열
- Bottom 4-맵/PositionMap 픽커 키 확장

## 4. 사용자 확인 필요 (코드 작업 전 질문)

1. **Side 카메라 커버리지**: Front 콜렛 다이는 Front 카메라만, Back 콜렛 다이는 Back 카메라만 촬영? (기존: 다이 하나를 앞/뒤 카메라 둘 다 촬영) — 다이당 측면 검사 범위가 달라짐
2. 콜렛별 0°/90° 2트리거는 유지되는지
3. 콜렛 패턴 8개 = 각각 **별도 티칭(학습) 패턴**인지, 동일 패턴의 런타임 인스턴스 8개인지
4. Bottom 카메라는 1대 공용(Front 배치 → Back 배치 순차 사용)이 맞는지
5. 와이어 포맷: `fb|collet` 분리 필드 방식(위 제안) 동의 여부 — 통신 포맷 변경 최소화 원칙 예외 승인
6. 기존 4픽커 레시피 → 8콜렛 마이그레이션 정책 (Front 값 복사 → Back 초기값?)

## 5. 단계별 작업 계획 (승인 후)

| 단계 | 내용 | 범위 |
|---|---|---|
| P1 | 주소체계/키 구조 도입 (ColletKey, 스토어 키 확장, 하위호환 파서) | Vision 코어 |
| P2 | 와이어 포맷 fb 필드 + 핸들러 송신부 | 프로토콜 양측 |
| P3 | 콜렛별 인스턴스 8세트 + 레시피 8슬롯/마이그레이션 | Vision |
| P4 | 시퀀스 순차화 (Front→Back), Sim 셀프런 동기 | 핸들러+Vision |
| P5 | UI 8콜렛 확장 (뷰어/맵) | Vision UI |
| P6 | 회귀 검증 (`perl tools/verify_all.pl`, `--auto-cycle`) | 전체 |

> 원칙 준수: CDT-310 알고리즘 코어 무변경, Sim==Real, 통신 포맷 변경은 본 건 명시 승인 범위 내.
