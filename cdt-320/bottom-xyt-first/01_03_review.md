# Bottom 좌표 선응답 — 요구/코드/갭 통합 리뷰 (Stage 1–3 압축)

## 요구사항 (사용자 구두 지시, 2026-07-12)
| ID | 분류 | 요구 | 우선 |
|----|------|------|------|
| R-001 | Business Logic | 바텀 검사에서 W/H/T/OffsetX,Y 가 계산되는 즉시(칩핑/이물 검출 완료 전) 핸들러에 비동기로 좌표를 먼저 응답 | Must |
| R-002 | Equipment Comm | 응답은 비동기(검사 본류 흐름을 지연시키지 않음), 최종 검사 결과(OK/NG) 응답은 기존대로 유지 | Must |
| R-003 | 공통 | 선응답 좌표 값은 최종 결과(INSPECTRESULT)의 W/H/T/Offset 과 동일해야 함(수식 변경 금지) | Must |

## 기존 코드 (이미 있는 것)
- **VisionInspector/CDTInspector.cs:32** — `SearchDieEnd(x, y, t, ix, iy)` 이벤트.
  외곽(라인 피팅) 확정 직후, CUDA 칩핑/이물 단계 **이전**에 발화(646행).
  좌표는 최종 `result.Offset`/`Angle` 과 동일 규약(×0.5+ChipRoi, X/Y 스왑)으로 **사본 선계산**(RaiseSearchDieEnd, 462행).
- **QMC.Vision/Equipment/Core/BottomXytPushService.cs** — 위 이벤트 구독(유일 구독자).
  스레드 로컬 컨텍스트(모듈/픽커/die_index)와 결합해
  `"XYT|MODULE|fb|collet|die_index|x=..;y=..;t=..;ix=..;iy=..;valid=0|1"` 을 Task.Run 으로 비동기 푸시.
- **QMC.Vision/Equipment/Comm/VisionTcpServer.cs:553** — `PushBottomXyt` 송신부.
- **QMC.CDT-320/Equipment/Vision/VisionTcpClient.cs:613** — XYT 푸시 수신 → `BottomXytStore` 기록 + `BottomXytReceived` 이벤트. 키-값(`TryGetDouble("x")`) 파싱이라 키 추가는 하위호환.
- **QMC.CDT-320/Equipment/Vision/BottomXytStore.cs** — (fb,collet)·die_index 별 최신 1건 보관.
- 활용처: **Side 시퀀스만** 사용(PickerBottomAndSideInspectionSequence:1672). Bottom 시퀀스는 미사용.

## 갭 분석
| Req | 상태 | 근거 | 비고 |
|-----|------|------|------|
| R-001 (T/OffsetX,Y 선응답) | ✅ 구현됨 | SearchDieEnd → XYT 푸시 | x/y/t 는 이미 감 |
| R-001 (**W/H** 선응답) | ❌ 미구현 | SearchDieEnd 페이로드에 W/H 없음 | W/H(픽셀 평균·mm 변환·스왑)는 본류에서 칩핑 뒤에 계산되지만 계산 자체는 외곽만으로 가능 |
| R-002 (비동기) | ✅ 구현됨 | Task.Run 푸시, 실패해도 검사 흐름 무영향 | |
| R-003 (값 동일) | ⚠️ 부분 | x/y/t 는 동일 규약 사본 선계산 확립 | W/H 도 같은 패턴으로 추가해야 함 |
| (파생) 핸들러 Bottom 시퀀스가 선응답 좌표 활용 | ❌ 미구현 | ApplyBottomInspectionResultAsync 가 최종 결과만 대기(696행) | 범위 여부 사용자 컨펌 필요 |
