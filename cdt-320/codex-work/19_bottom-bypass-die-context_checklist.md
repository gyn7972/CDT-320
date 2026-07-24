# 체크리스트 — 비전 바이패스 모드 Bottom 최종 RESULT 문맥 채움 (PICKER-PLACE-BOTTOM-DIE-MISMATCH 수정)

작성일: 2026-07-24  /  사용자 지시: "수정안대로 하자 시뮬레이션시 값 채우도록. 비전 바이패스 모드에서만"

## 원인 (진단 워크플로 wf_c6ef9bf9 확정)
- UseVision=false(비전 바이패스) 시 유닛 숏컷(PickerFrontUnit:1033/PickerRearUnit:799 →
  SimulateBottomInspectionResult → ToBottomVisionOffset)이 DieId=""/DieIndex=-1 하드코딩
  (VisionCameraCalibrationTransform.cs:149-150) + RequestId/GroupId 미설정(시뮬 DTO에 없음,
  AutoVisionRequestService.cs:1302-1314) 결과를 반환.
- 2ff147c0 신설 place 최종 검증(PickerBottomAndSideInspectionSequence.cs:3192~)의 DieId
  비교가 이를 검출 → PICKER-PLACE-BOTTOM-DIE-MISMATCH(bottomResultDie=""). DieId만 채우면
  DieIndex(-1, :3213) → correlation(RequestId/GroupId 빈 값, :3222)에서 연쇄 2차 알람.
- pickerNo=4는 4→3→2→1 순서의 첫 검증 대상이라 항상 P4에서 먼저 정지.

## 수정 계획 (승인 범위: 바이패스 모드에서만 값 채움, 실비전 경로 무변경)
- [x] P1. 스탬핑 지점 = 시퀀스 ReceiveBottomFinalResultAsync(:3068) — Front/Rear 공통 단일
  지점이며 shot.Target(Die.DieId/InputSequenceNo/PickerNo) 문맥 보유. 결과 수신 직후
  ApplyBypassBottomFinalResultContext(shot, result) 호출.
- [x] P2. 신설 헬퍼 조건: result != null && shot.Target.Die != null && result.DieId 빈 값
  && AppSettingsStore.Current.UseVision == false (유닛 IsVisionBypassed와 동일 판정).
  실비전 결과(DieId 보유)는 절대 손대지 않음 → place 대상 vs 촬영 대상 불일치 검출력 유지.
- [x] P3. 채움 값: DieId=shot.Target.Die.DieId, DieIndex=shot.Target.Die.InputSequenceNo,
  RequestId 빈 값이면 SIM-BOTTOM-P{pickerNo}-{dieId}, GroupId 빈 값이면 RequestId 복사.
  PickerNo는 시뮬 경로가 이미 정확(ToBottomVisionOffset(pickerNo,...)) — 미변경.
  measure_valid/item offset/_pass는 AddBypassBottomInspectionValues가 기채움 — 미변경.
- [x] P4. 채움 시 Check 로그 1건(픽커/다이/인덱스/requestId).
- [x] MRESULT 경로(WaitBottomInspectionResultAsync 바이패스)는 현재 통과 중 — 범위 밖, 무변경.
  Side/Bin 채널 무변경. 실비전(TpuVisionAdapter) 경로 무변경.

## 검증
- [x] 빌드 통과(OutDir 스크래치).
- [x] 검증식 대조: 스탬핑 후 ValidateAndApplyBottomFinalResult 8단 검사(null/limit/DieId/
  PickerNo/DieIndex/correlation/measure_valid/item offset) 전부 통과 가능함을 코드로 확인.
- [x] GetValidatedBottomPlaceResult(:3148)의 DieId/PickerNo 재대조도 동일 값으로 통과 확인.
- [x] 실비전 경로 diff 0건(ApplyBottomRequestContext/TpuVisionAdapter/유닛/변환기 무변경).
