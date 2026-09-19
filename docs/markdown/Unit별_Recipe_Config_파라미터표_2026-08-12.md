# Unit별 Recipe / Config 파라미터 전수표 (2026-08-12)

기준 소스: 로컬 `D:\Source\CDT-320_New` (master, 6cca5d39 이후 작업본)

## 분류 체계 (현재 코드 기준)

| 분류 | 저장 위치 | 교체 시점 |
|---|---|---|
| **Config** (`IConfigData`) | EquipmentData Config 저장소 — Unit(StorageKey)당 1개 | 레시피를 바꿔도 **유지** (장비 고정값) |
| **Recipe** (`IRecipeData`) | 레시피 이름별 저장소 — 레시피×Unit당 1개 | 레시피 교체 시 **함께 교체** |

- **Setup**(`ISetupData`, 기구 옵셋·존·시뮬레이션 플래그 등)은 요청 범위 밖이라 이 표에서 제외했습니다. 필요하시면 추가하겠습니다.
- 마지막 **[변경]** 칸은 비워두었습니다. 옮길 항목에 `→Recipe` / `→Config` 로 적어 주시면 됩니다. (비워두면 현행 유지로 간주)
- `(레거시)` 표시는 구버전 설정 파일 읽기 호환용으로만 남아 있는 필드 — 재분류 대상에서 제외해도 됩니다.

---

# 1. InputCassetteUnit (입력 카세트)

## 1-1. Config (현재)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| bDryRun | bool | false | DryRun 모드 | |
| LoadingPositionOffset | double | 0.0 | 로딩 위치 오프셋 | |
| UnloadingPositionOffset | double | 0.0 | 언로딩 위치 오프셋 | |
| UnloadReleaseLiftDistance | double | 1.0 | 언로드 릴리즈 리프트 거리 | |
| Level2PositionOffset | double | 59.0 | 2단 카세트 위치 오프셋 | |
| SlotPitch | double | 5.0 | 슬롯 피치 [mm] | |
| SlotCount | int | 25 | 슬롯 수 | |
| ScanVelocity | double | 20.0 | 맵핑 스캔 속도 | |
| ScanAcc | double | 0.0 | 맵핑 스캔 가속 | |
| ScanDec | double | 0.0 | 맵핑 스캔 감속 | |
| ScanSettleTimeMs | int | 100 | 스캔 안정화 대기 [ms] | |
| MappingWindowRatio | double | 0.25 | 슬롯 유효 윈도우 반폭 비율(명목±pitch×비율) | |
| MappingMinOnTravelMm | double | 1.0 | 점유 인정 최소 센서 ON 이동거리 [mm] | |
| InchSelect | int | 0 | 0=8인치, 1=12인치 | |
| SelectedCassetteLevel | int | 1 | 1=1단, 2=2단 사용 | |

## 1-2. Recipe (현재)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| AvoidPosition | double | 0.0 | 회피(Ready) 위치 | |
| LoaingPosition | double | 150.0 | 로딩 위치 | |
| UnloadingPosition | double | 150.0 | 언로딩 위치 | |
| MappingStartPosition | double | 5.0 | 1단 맵핑 스캔 시작 | |
| MappingEndPosition | double | 130.0 | 1단 맵핑 스캔 끝 | |
| Level2MappingStartPosition | double | 0.0 | 2단 맵핑 스캔 시작 | |
| Level2MappingEndPosition | double | 0.0 | 2단 맵핑 스캔 끝 | |
| Level1FirstSlotPosition | double | 0.0 | 1단 첫 슬롯(스타트) 위치 | |
| Level2FirstSlotPosition | double | 0.0 | 2단 첫 슬롯(스타트) 위치 | |
| SlotPosition[] | double[] | NaN | 맵핑 확정 슬롯별 Z 위치(실측 결과 버퍼) | |

---

# 2. InputFeederUnit (입력 피더)

## 2-1. Config (현재)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| bDryRun | bool | false | DryRun 모드 | |

## 2-2. Recipe (현재)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| AvoidPosition | double | 0.0 | 회피 위치 | |
| CassetteLoadPosition | double | 0.0 | 카세트 로드 위치 | |
| CassetteUnloadPosition | double | 0.0 | 카세트 언로드 위치 | |
| CassetteExchangePosition | double | 0.0 | 카세트 교체 위치 | |
| WaferLoadAvoidPosition | double | 0.0 | 웨이퍼 로드 회피 위치 | |
| WaferLoadPosition | double | 0.0 | 웨이퍼 로드 위치 | |
| WaferUnloadAvoidPosition | double | 0.0 | 웨이퍼 언로드 회피 위치 | |
| WaferUnloadPosition | double | 0.0 | 웨이퍼 언로드 위치 | |
| WaferBarcodePosition | double | 0.0 | 바코드 판독 위치 | |

---

# 3. InputStageUnit (입력 스테이지)

## 3-1. Config (현재)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| bDryRun | bool | false | DryRun 모드 | |
| PickUpEjectPinOffset | double | 0.0 | 이젝트 핀 오프셋 | |
| PickUpEjectPinSpeed | double | 100.0 | 이젝트 핀 속도 | |
| PickUpEjectPinAcc | double | 0.0 | 이젝트 핀 가속 | |
| PickUpEjectPinDec | double | 0.0 | 이젝트 핀 감속 | |
| PickUpNeedleSyncLiftDistance | double | 2.0 | 니들 동기 리프트 거리 | |
| PickUpNeedleSyncLiftVelocity | double | 5.0 | 니들 동기 리프트 속도 | |
| PickUpNeedleSyncLiftAcc | double | 100.0 | 니들 동기 리프트 가속 | |
| PickUpNeedleSyncLiftDec | double | 100.0 | 니들 동기 리프트 감속 | |
| PickUpNeedleSyncLiftSettleMs | int | 0 | 동기 리프트 후 안정화 [ms] | |
| PickUpNeedleSeparateDistance | double | 1.0 | 니들 분리 거리 | |
| PickUpNeedleSeparateSpeedPercent | double | 1.0 | 니들 분리 속도 % | |
| PickUpNeedleSeparateVelocity (레거시) | double | 0.0 | 구파일 호환 | |
| PickUpNeedleSeparateAcc (레거시) | double | 100.0 | 구파일 호환 | |
| PickUpNeedleSeparateDec (레거시) | double | 100.0 | 구파일 호환 | |
| MaxAlignIterations | int | 3 | 얼라인 반복 촬상 최대 횟수 | |
| AlignConvergenceThresholdDeg | double | 0.005 | 얼라인 수렴 임계 [deg] | |
| AlignThetaCorrectionLimitDeg | double | 1.0 | 얼라인 T 보정 허용 최대 [deg] | |
| AlignPitchCompareToleranceMm | double | 0.1 | 얼라인 Ref 피치 비교 허용 [mm] | |
| AlignCenterToleranceMm | double | 0.1 | 얼라인 최종 센터 허용 [mm] | |
| MaxEffectiveThetaToleranceDeg | double | 0.05 | 유효 T 허용 상한 [deg] | |
| ManualDieDetectOffsetLimitX | double | 20.0 | 수동 Die 검출 전체맵 X 오프셋 한계 [mm] | |
| ManualDieDetectOffsetLimitY | double | 20.0 | 수동 Die 검출 전체맵 Y 오프셋 한계 [mm] | |
| DieMapFineOffsetLimitX | double | 2.0 | Die Mapping X 미세 보정 한계 [mm] | |
| DieMapFineOffsetLimitY | double | 2.0 | Die Mapping Y 미세 보정 한계 [mm] | |
| InputDieVisionRetryCount | int | 3 | PickUp 전 Input Die Vision 재시도 횟수 | |
| InputDieVisionFailureAction | enum | SkipDie | 비전 실패 시 처리 방식 | |
| InputDieVisionWaitRetryLimit | int | 3 | 실패 Die Wait(재촬영) 최대 횟수, 초과 시 SKIP | |
| SequenceMoveTimeoutMs | int | 10000 | 시퀀스 이동 타임아웃 [ms] | |

## 3-2. Recipe (현재)

축별 위치 세트 7개 × 공통 필드 구조입니다.

**축 세트 (`StageAxisPositions` × 7):** WaferY, WaferT, WaferZ, VisionX, NeedleX, NeedleZ, EjectPinZ

| 세트 내 변수 (각 축마다 존재) | 타입 | 설명 | [변경] |
|---|---|---|---|
| AvoidPosition | double | 회피 위치 | |
| LoadPosition | double | 로드 위치 | |
| ProcessPosition | double | 공정 위치 | |
| UnloadPosition | double | 언로드 위치 | |
| ReadyPosition | double | 레디 위치 | |
| BarcodePosition | double | 바코드 위치 | |
| ReticlePosition | double | 레티클 위치 | |
| NeedlePinCalPosition | double | 니들핀 캘리브레이션 위치 | |
| DiePosition[] | double[] | 다이별 위치 배열 | |

**DieMap (`InputStageDieMapRecipe`):**

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| Top / Bottom / Left / Right (마크포인트 4개) | 객체 | — | 각각 아래 5필드 보유 | |
| ├ Enabled | bool | true | 마크포인트 사용 여부 | |
| ├ StageYPosition | double | 0.0 | 마크 StageY 티칭 위치 | |
| ├ VisionXPosition | double | 0.0 | 마크 VisionX 티칭 위치 | |
| ├ VisionOffsetX | double | 0.0 | 비전 X 오프셋 | |
| └ VisionOffsetY | double | 0.0 | 비전 Y 오프셋 | |
| VisionTargetId | string | Center | 얼라인 비전 타겟 ID | |
| VisionRetryCount | int | 3 | 비전 재시도 횟수 | |

---

# 4. PickerFrontUnit / PickerRearUnit (프론트·리어 픽커) — 구조 동일

## 4-1. Config (현재) — 최상위

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| UseUnit | bool | true | 유닛 전체 사용 여부 | |
| RunOrderMode | enum | Descending | Picker0~3 작업 순서 | |
| bDryRun | bool | false | DryRun 모드 | |
| UsePicker[4] | bool[] | true×4 | Picker0~3 개별 사용 여부 | |
| ColletExchangeInputX | double | 0.0 | 콜렛 교체 위치 X(Input 끝) — 의도적으로 Config | |
| ColletExchangeOutputX | double | 0.0 | 콜렛 교체 위치 X(Output 끝) — 의도적으로 Config | |
| VisionInspectionSettleMs | int | 0 | 비전 트리거 전 안정화 대기 [ms] | |
| SideInspectionTurnSettleMs | int | 0 | Side 0/90도 전환 후 안정화 [ms] | |

## 4-2. Config (현재) — PickUp 하위 (`PickerPickUpMotionConfig`)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| MotionMode | enum | Detailed | PickUp Z 모션 모드 | |
| TransferMotionMode | enum | Default | 이송 모션 모드 (Default / ContiSegmentedPickUp) | |
| MechanicalOffsetLimitMm | double | 1.0 | 기구 보정 한계 [mm] | |
| MechanicalOffsetX[4] | double[] | 0.020×4 | 픽커별 PickUp X 기구 보정 | |
| MechanicalOffsetY[4] | double[] | 0.0×4 | 픽커별 PickUp Y 기구 보정 | |
| TransferContiCoordinate | int | 2 | Conti 좌표계 번호 | |
| TransferContiTimeoutMs | int | 5000 | Conti 타임아웃 [ms] | |
| TransferContiMaxTravelDistance | double | 45.0 | Conti 최대 이동거리 | |
| TransferContiPickerYMaxCorrectionDistance | double | 1.5 | Conti 중 PickerY 최대 보정거리 | |
| TransferContiXYMidRatio | double | 0.5 | XY 중간점 비율 | |
| TransferContiSplineCurvePercent | double | 100.0 | 스플라인 곡률 % | |
| TransferContiUseGlobalSpeedScale | bool | true | 전역 속도 스케일 사용 | |
| PickerZPrePickDistance | double | 1.0 | Z PrePick 거리 | |
| PickerZSlowApproachSpeedPercent | double | 1.0 | Z 저속 접근 속도 % | |
| PickerZSyncLiftDistance | double | 2.0 | Z 동기 리프트 거리 | |
| PickerZSyncLiftVelocity | double | 5.0 | Z 동기 리프트 속도 | |
| PickerZSyncLiftAcceleration | double | 100.0 | Z 동기 리프트 가속 | |
| PickerZSyncLiftDeceleration | double | 100.0 | Z 동기 리프트 감속 | |
| PickerZSeparateDistance | double | 1.0 | Z 분리 거리 | |
| PickerZSeparateSpeedPercent | double | 1.0 | Z 분리 속도 % | |
| PickerZAvoidReturnSpeedPercent | double | 10.0 | Z 회피 복귀 속도 % | |
| PickerSafeForWaferStageDistance | double | 2.0 | 스테이지 안전거리(최소 2.0) | |
| SeparateMode | enum | Simultaneous | 분리 모드 | |
| VacuumOnBeforePickDelayMs | int | 0 | Pick 전 진공 ON 선행 [ms] | |
| NeedleVacuumOffSettleBeforeXYMs | int | 100 | 니들 진공 OFF 후 XY 전 안정화 [ms] | |
| SyncLiftSettleMs | int | 0 | 동기 리프트 후 안정화 [ms] | |
| PickSettleMs | int | 0 | Pick 후 안정화 [ms] | |
| PickUpEntryZPreDownMode | bool | false | 진입 Z 선행하강 스위치 | |
| PreDownNeedleWorkRadiusMm | double | 130.0 | Z 선행 반경 게이트 [mm] | |
| PickUpDynamicWaitMode | bool | false | 대기 픽커 동적 선행 대기점 스위치 | |
| DynamicWaitExtraMarginMm | double | 0.0 | 동적 대기점 여유 가산 [mm] | |
| (레거시 11종) PickerZSlowApproachVelocity/Acc/Dec, SyncLiftDistance, SyncLiftSpeedPercent, NeedleSeparateDistance, PickerSeparateDistance, SeparateSpeedPercent, PickerZSeparateVelocity/Acc/Dec | double | 0.0 | 구파일 호환 전용 | |

## 4-3. Config (현재) — BottomInspection 하위 (`PickerBottomInspectionMotionConfig`)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| FlyingZDownMode | enum | Off | Bottom 이동 중 Z 하강 모드 | |
| FlyingZDownDistance | double | 2.0 | Z 하강 거리(DownDistance 모드) | |
| ApproachPreMotionDistanceMm | double | 50.0 | 접근 구간 Z+T 선행 발동 잔여거리 [mm] | |
| ParallelFirstSideOverlap | bool | false | 마지막 Bottom·첫 Side 촬영 병렬 송신 | |

## 4-4. Config (현재) — Place 하위 (`PickerPlaceMotionConfig`)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| MechanicalOffsetLimitMm | double | 1.0 | Place 기구 보정 한계 [mm] | |
| BottomPlaceCorrectionLimitMm | double | 1.0 | Bottom→Place 보정 한계 [mm] | |
| MechanicalOffsetX[4] | double[] | 0.0×4 | 픽커별 Place X 기구 보정 | |
| MechanicalOffsetY[4] | double[] | 0.0×4 | 픽커별 Place Y 기구 보정 | |
| MotionMode | enum | ContiSegmentedPlace | Place 모션 모드 | |
| ContiCoordinate | int | 1 | Conti 좌표계 번호 | |
| ContiTimeoutMs | int | 5000 | Conti 타임아웃 [ms] | |
| ContiMaxTravelDistance | double | 45.0 | Conti 최대 이동거리 | |
| ContiZ1Step1Clearance | double | 2.0 | Z1 1단계 클리어런스 | |
| ContiZ1Step2Clearance | double | 2.0 | Z1 2단계 클리어런스 | |
| ContiNearAvoidDistance | double | 1.0 | 근접 회피 거리 | |
| ContiXYMidRatio | double | 0.5 | XY 중간점 비율 | |
| ContiSplineCurvePercent | double | 100.0 | 스플라인 곡률 % | |
| ContiOverDrive | double | 0.03 | Conti 오버드라이브 | |
| PlaceZOverDrive | double | 0.0 | Place Z 오버드라이브 | |
| PlaceReleaseDwellMs | int | 0 | 릴리즈 후 대기 [ms] | |
| PlaceBlowDelayMs | int | 100 | Blow 지연 [ms] | |
| ContiTapeThicknessFallback | double | 0.0 | 테이프 두께 폴백 | |
| ContiDieThicknessFallback | double | 0.0 | 다이 두께 폴백 | |
| ContiMaxVelocity | double | 500.0 | Conti 최대 속도 | |
| ContiMaxAcceleration | double | 5000.0 | Conti 최대 가속 | |
| ContiMaxDeceleration | double | 5000.0 | Conti 최대 감속 | |
| ContiUseGlobalSpeedScale | bool | true | 전역 속도 스케일 사용 | |
| ContiNode0~4SpeedPercent | double | 1/20/100/100/1 | 노드별 속도 % (5개) | |
| PlaceEntryZPreDownMode | bool | false | Place 진입 Z 선행하강 스위치 | |
| RearEntryPreDownStageYLimitMm | double | 0.0 | Rear 전용 Z 선행 발동 StageY 하한(0=Rear 미발동) | |

## 4-5. Recipe (현재)

**축 세트 (`PickerAxisPositionSet` × 10):** PickerX, PickerY, PickerT0~T3, PickerZ0~Z3

| 세트 내 변수 (각 축마다 존재) | 타입 | 설명 | [변경] |
|---|---|---|---|
| InputAvoidPosition | double | Input측 회피 위치 | |
| OutputAvoidPosition | double | Output측 회피 위치 | |
| AvoidPosition | double | 공통 회피/대기 위치 | |
| PickPosition | double | Pick 기준 위치 | |
| BottomPosition | double | Bottom 검사 위치 | |
| SidePosition | double | Side 검사 위치 | |
| PlacePosition | double | Place 기준 위치 | |
| DiePickPosition[] | double[] | 픽커별 Pick 개별 티칭 배열 | |
| DieBottomPosition[] | double[] | 픽커별 Bottom 개별 티칭 배열 | |
| DieSidePosition[] | double[] | 픽커별 Side 개별 티칭 배열 | |
| DiePlacePosition[] | double[] | 픽커별 Place 개별 티칭 배열 | |

**축 세트 외 단일 항목:**

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| PickLiftPosition | double | 2.0 | Pick 후 Z 상승 기준 위치 | |
| PickLiftWaitMs | int | 50 | Pick Lift 후 안정화 [ms] | |
| PlaceDelayMs | int | 50 | Place 후 대기 [ms] | |
| ColletRotationCenterX[4] | double[] | 0.0×4 | 콜렛별 회전중심 X (캘리브레이션 결과) | |
| ColletRotationCenterY[4] | double[] | 0.0×4 | 콜렛별 회전중심 Y (캘리브레이션 결과) | |
| ColletRotationCenterValid[4] | bool[] | false×4 | 회전중심 유효 플래그 | |
| BottomToPickMm | double | 0.0 | Bottom 포커스 Z→Pick Z 고정 가산값 [mm] | |
| BottomToPlaceMm | double | 0.0 | Bottom 포커스 Z→Place Z 고정 가산값 [mm] | |
| HeadPickOverdriveMm | double | 0.0 | 헤드 공통 Pick Z 오버드라이브 [mm] | |
| ColletPickOverdriveMm[4] | double[] | 0.0×4 | 콜렛별 Pick Z 오버드라이브 [mm] | |
| AfZUpdateLimitMm | double | 0.3 | AF 기반 Pick/Place Z 갱신 안전 한계 [mm] | |

---

# 5. VisionUnit (비전)

## 5-1. Config (현재)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| bDryRun | bool | false | DryRun 모드 | |
| PickerInspectionMode | enum | BottomAndSidePipeline | Bottom/Side 파이프라인 모드 | |
| FocusCalibration | 객체 | — | AF 포커스 캘리브레이션 데이터 세트 | |
| UseInputVisionPrefetch | bool | false | Input 비전 선행검사(Prefetch) 스위치 | |
| InputVisionPrefetchIdlePollMs | int | 200 | Prefetch 유휴 폴링 주기 [ms] | |
| InputVisionPrefetchFailureHoldMs | int | 5000 | Prefetch 실패 홀드 [ms] | |
| CalibrationData | 객체 | — | 비전 캘리브레이션 데이터 (DataMember 아님 — 직렬화 제외 주의) | |

## 5-2. Recipe (현재)

**축 세트 (`VisionAxisPositions` × 2):** FrontSideVision, RearSideVision

| 세트 내 변수 (각 세트마다 존재) | 타입 | 설명 | [변경] |
|---|---|---|---|
| AvoidPosition | double | 회피 위치 | |
| Process0Position | double | 0도 촬영 위치 | |
| Process90Position | double | 90도 촬영 위치 | |
| ColletRotationCenterX[4] / Y[4] / Valid[4] | 배열 | 콜렛별 회전중심 (Side 비전 기준) | |

**단일 항목:**

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| MoveTimeoutMs | int | 5000 | 축 이동 타임아웃 [ms] | |
| IoTimeoutMs | int | 1000 | I/O 타임아웃 [ms] | |
| CaptureTimeoutMs | int | 5000 | 촬영 타임아웃 [ms] | |
| BottomVisionPreGrabDelayMs | int | 0 | Bottom 촬영 전 지연 [ms] | |
| RuntimeAutoFocusToBottomInspectionDelayMs | int | 300 | AF→Bottom 검사 지연 [ms] | |
| RuntimeAutoFocusToBottomInspectionDelayInitialized | bool | false | 위 값 초기화 완료 플래그(내부용) | |

---

# 6. OutputStageUnit (출력 스테이지)

## 6-1. Config (현재)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| bDryRun | bool | false | DryRun 모드 | |
| ResultRoutingMode | enum | ForceGoodStage | 검사 결과 라우팅 모드 | |
| ColletCleaningTimeoutMs | int | 10000 | 콜렛 클리닝 대기 [ms] | |

## 6-2. Recipe (현재)

**축 세트 (`StageAxisPositions` × 4):** GoodStageY, GoodStageZ, NGStageY, VisionX
— 세트 내 필드는 3-2와 동일 (AvoidPosition, LoadPosition, ProcessPosition, UnloadPosition, ReadyPosition, BarcodePosition, ReticlePosition, NeedlePinCalPosition, DiePosition[])

## 6-3. StageModule (Good/NG 하위 모듈) — Config 없음, Recipe만

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| WorkPositionZ | double | 10.0 | 작업 Z 위치 | |
| AvoidPositionZ | double | 0.0 | 회피 Z 위치 | |
| UnloadPositionY | double | -50.0 | 언로드 Y 위치 | |
| HomePositionY | double | 0.0 | 홈 Y 위치 | |
| CleaningPositionY | double | 80.0 | 클리닝 Y 위치 | |

---

# 7. OutputFeederUnit (출력 피더)

## 7-1. Config (현재)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| bDryRun | bool | false | DryRun 모드 | |

## 7-2. Recipe (현재)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| AvoidPosition | double | 0.0 | 회피 위치 | |
| GoodCassetteLoadPosition | double | 30.0 | Good 카세트 로드 위치 | |
| GoodCassetteUnloadPosition | double | 30.0 | Good 카세트 언로드 위치 | |
| GoodCassetteExchangePosition | double | 0.0 | Good 카세트 교체 위치 | |
| GoodWaferLoadAvoidPosition | double | 0.0 | Good 웨이퍼 로드 회피 | |
| GoodWaferLoadPosition | double | 150.0 | Good 웨이퍼 로드 위치 | |
| GoodWaferUnloadAvoidPosition | double | 0.0 | Good 웨이퍼 언로드 회피 | |
| GoodWaferUnloadPosition | double | 150.0 | Good 웨이퍼 언로드 위치 | |
| GoodWaferBarcodePosition | double | 0.0 | Good 바코드 위치 | |
| NGCassetteLoadPosition | double | 30.0 | NG 카세트 로드 위치 | |
| NGCassetteUnloadPosition | double | 30.0 | NG 카세트 언로드 위치 | |
| NGCassetteExchangePosition | double | 0.0 | NG 카세트 교체 위치 | |
| NGWaferLoadAvoidPosition | double | 0.0 | NG 웨이퍼 로드 회피 | |
| NGWaferLoadPosition | double | 200.0 | NG 웨이퍼 로드 위치 | |
| NGWaferUnloadAvoidPosition | double | 0.0 | NG 웨이퍼 언로드 회피 | |
| NGWaferUnloadPosition | double | 200.0 | NG 웨이퍼 언로드 위치 | |
| NGWaferBarcodePosition | double | 0.0 | NG 바코드 위치 | |

---

# 8. OutputCassetteUnit (출력 카세트)

## 8-1. Config (현재)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| bDryRun | bool | false | DryRun 모드 | |
| UseNgCassette | bool | true | NG 카세트 사용 여부(false=NG 요구 스킵) | |
| LoadingPositionOffset | double | 0.0 | 로딩 위치 오프셋 | |
| UnloadingPositionOffset | double | 0.0 | 언로딩 위치 오프셋 | |
| UnloadReleaseLiftDistance | double | 1.0 | 언로드 릴리즈 리프트 거리 | |
| Level2PositionOffset | double | 59.0 | 2단 위치 오프셋 | |
| GOODNGPositionOffset | double | 0.0 | GOOD/NG 위치 오프셋 | |
| SlotPitch | double | 6.0 | 슬롯 피치 [mm] | |
| SlotCount | int | 25 | 슬롯 수 | |
| InchSelect | int | 0 | 0=8인치, 1=12인치 | |
| SelectedCassetteLevel | int | 1 | 1=1단, 2=2단 | |
| ScanVelocity | double | 20.0 | 맵핑 스캔 속도 | |
| ScanAcc | double | 0.0 | 맵핑 스캔 가속 | |
| ScanDec | double | 0.0 | 맵핑 스캔 감속 | |
| ScanSettleTimeMs | int | 100 | 스캔 안정화 대기 [ms] | |
| MappingWindowRatio | double | 0.25 | 슬롯 유효 윈도우 반폭 비율 | |

## 8-2. Recipe (현재)

| 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|
| AvoidPosition | double | 0.0 | 회피 위치 | |
| GoodLoaingPosition | double | 150.0 | Good 로딩 위치 | |
| GoodUnloadingPosition | double | 150.0 | Good 언로딩 위치 | |
| GoodFirstSlotPosition | double | 80.0 | Good1 첫 슬롯(스타트) 위치 | |
| Good2FirstSlotPosition | double | 0.0 | Good2 첫 슬롯(스타트) 위치 | |
| NGLoaingPosition | double | 150.0 | NG 로딩 위치 | |
| NGUnloadingPosition | double | 150.0 | NG 언로딩 위치 | |
| NGFirstSlotPosition | double | 10.0 | NG 첫 슬롯(스타트) 위치 | |
| NgMappingStartPosition / NgMappingEndPosition | double | 0.0 | NG 존 맵핑 스캔 구간 | |
| Good1MappingStartPosition / Good1MappingEndPosition | double | 0.0 | Good1 존 맵핑 스캔 구간 | |
| Good2MappingStartPosition / Good2MappingEndPosition | double | 0.0 | Good2 존 맵핑 스캔 구간 | |
| MappingStartPosition (레거시) | double | 5.0 | 전체 스택 단일 스캔 시작 — 존 분리로 대체, 미사용 | |
| MappingEndPosition (레거시) | double | 304.0 | 전체 스택 단일 스캔 끝 — 미사용 | |
| GoodSlotPosition[] / Good2SlotPosition[] / NGSlotPosition[] | double[] | NaN | 존별 맵핑 확정 슬롯 위치(실측 결과 버퍼) | |

---

# 9. 설비 수준 (CDT320Machine)

| 분류 | 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|---|
| Config | ModelVersion | string | "v1.0" | 소프트웨어 모델 버전 | |
| Recipe | ProductId | string | "PRODUCT-A" | 현재 로드된 제품(공정) ID | |

# 10. UnitDefined 공통 (엑셀 시트 기반 Unit — 해당 Unit이 있을 때만)

| 분류 | 변수 | 타입 | 기본값 | 설명 | [변경] |
|---|---|---|---|---|---|
| Config | IsSimulationMode | bool | true | 시뮬레이션 모드 | |
| Recipe | MoveTimeoutMs | int | 5000 | 축 이동 타임아웃 [ms] | |
| Recipe | IoTimeoutMs | int | 1000 | I/O 타임아웃 [ms] | |
| Recipe | BlowTimeMs | int | 100 | Blow 유지 시간 [ms] | |

---

# 부록. 프로젝트 레시피 (RecipeStore, `Recipes\*.Project` 파일)

Unit별 Recipe(`IRecipeData`)와 **별개 체계**입니다. 레시피 탭에서 편집하는 프로젝트 파일 항목으로, 참고용으로 함께 나열합니다.

**최상위:** FileName, MachineNumber, CassetteFlow, DryRun, StepRun, XmlSave, ReDt, EbrMode, AlignConfirmEnable, NeedleCheckMode, AutoPositionDeviationLimit(50), MapFormat, MapDirection, ChipThickness(150), MasterChipThickness(150), TapeThickness(100), BinSortNumber, LotId, PartId, InputCassetteId, OutputCassetteId, 맵 파일 경로류(Input/Output/GoodBin/NgBin), MapApprovalVersion·승인 해시 3종, InputCassetteLevelCount, GoodCassetteLevelCount, ColletModelNum, ColletLotNum, XmlPath

**ColletZ (`ColletZConfigSubset`):** Enable, ColletType(Flat/Rim), DieCalThicknessMm, FilmThicknessMm, BestFocusApplyOffsetMm, FlatZOffsetMm, RimOffsetFromFlatMm, LastAppliedOffsetMm

**Die (`DieSubset`):** DieSpecName, WidthMm, HeightMm, ThicknessMm, ChipLower/UpperSpecLimitWidth·Height, ChippingDepthMax, ChippingLengthMax, ForeignSizeMax

**Frame / InputFrame / OutputFrame (`TapeFrameSubset`):** FrameSpecName, DieMapX/Y, PitchX/Y, DieSizeX/Y, Rotate, OuterDiameterMm, EdgeSkipMode, Side/TopBottomEdgeSkip(개수·mm)

**LoadFrame:** Role, AutoAlignment, AlignmentPoints / **UnloadFrame:** Role, GapInspection, GapUpperLimit, GapLowerLimit

**Module (`ModuleSubset`):** PickRetryCount, PickDelayMs, PlaceDelayMs, ColletCleanEnable, ColletCleanInterval, BottomInspectionEnable, PlacementInspectionEnable

**BottomInsp / FrontSideInsp / RearSideInsp (`InspectionSubset`):** Enable, ExposureMs, LightIntensity, ChippingDepthMaxMm, ChippingLengthMaxMm, ScratchAreaMaxMm2, ContaminationMaxMm2, MinDieCenterScore

**Output (`OutputSubset`):** GoodPlateMaxSlots, NgPlateMaxSlots, DiesPerWafer, WafersPerOutputBatch, AutoBinTransition, AlarmOnFull, DefaultGoodCassette

**Pickup / InputPickup / OutputPickup (`PickupSubset`):** StartCorner, Direction, Pattern
