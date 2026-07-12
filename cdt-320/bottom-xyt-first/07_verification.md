# Bottom 좌표 선응답 — 구현/검증 (Stage 6–7)

## 컨펌된 범위 (2026-07-12)
1. 핸들러 Bottom 시퀀스 선행 아님 — **Side 촬영 전 XYT+W/H 도착 게이트**(미도착 시 촬영 시작 금지, 오프셋을 Side 포커스 보정에 사용).
2. W/H 단위 = **mm** (최종 INSPECTRESULT 와 동일 값, F4).
3. 미검출 시 x/y/t/w/h **전부 0** + valid=0 (기존 정책 통일).

## 구현 (커밋 대상 파일)
- ✅ `VisionInspector/CDTInspector.cs` — `SearchDieEnd` 시그니처에 (wMm, hMm) 추가.
  발화 지점을 vList(W/H 픽셀 평균) 확정 직후로 이동(여전히 CUDA 칩핑/이물 전).
  W/H 사본 선계산: wMm = hPx×PixelSizeHeightMm÷2, hMm = wPx×PixelSizeWidthMm÷2 (본류 mm 변환+스왑과 동일).
- ✅ `QMC.Vision/Equipment/Core/BottomXytPushService.cs` — 새 시그니처 수신, valid=0 → w/h=0, 로그에 w/h.
- ✅ `QMC.Vision/Equipment/Comm/VisionTcpServer.cs` — 페이로드 `;w=..;h=..`(mm, F4) 추가(기본값 파라미터 — 하위호환).
- ✅ `QMC.CDT-320/Equipment/Vision/BottomXytStore.cs` — `BottomXytPush.W/H`(mm) 추가.
- ✅ `QMC.CDT-320/Equipment/Vision/VisionTcpClient.cs` — `w`/`h` 키 파싱(없으면 0 — 구버전 비전 호환), 로그 확장.
- ✅ `QMC.CDT-320/Equipment/Vision/VisionProtocol.cs` — 페이로드 형식 주석 갱신.
- ✅ `QMC.CDT-320/Sequencing/Picker/PickerBottomAndSideInspectionSequence.cs` —
  `LogBottomXytForSide`(로그만) → `WaitBottomXytForSideAsync`(게이트) 승격:
  Side 이동/촬영 전 (fb, collet) 푸시 도착까지 5ms 폴링 대기, 타임아웃(비전 검사 타임아웃과 동일) 시
  `PICKER-SIDE-XYT-TIMEOUT` 알람 정지(촬영 미시작). valid=0(미검출)은 '도착'으로 간주(기존 정책).

## 검증
- ✅ 3개 프로젝트(QMc.Vision.Inspector / QMC.Vision / QMC.CDT-320) 빌드 성공(격리 OutDir).
- ✅ 검사 결과 불변: 벤치 세트 105장 × 확장 16필드(판정/W/H/각도/치핑·이물 사이즈/디펙 해시) 불일치 0.
- ❓ 라이브 검증(장비 재시작 필요): XYT 푸시 로그 w/h == INSPECTRESULT W/H 일치, Side 게이트 통과/타임아웃 동작.

## 남은 확인(재시작 후)
- 이벤트 로그 `BottomXyt` 라인에 w/h(mm) 포함 확인, 값이 이후 결과와 일치하는지.
- Side 진입 로그 "게이트 통과, waited=..ms" — 정상 흐름에서 waited≈0(푸시가 항상 선행)인지.
- 오프셋 → Side 포커스(PickerZ)/비전 Y 보정 '매핑'은 현재 0 고정(AutoVisionRequestService:1215) —
  매핑 수식은 공정 확정 후 별도 적용(푸시 데이터는 준비 완료).
