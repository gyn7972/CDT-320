# 픽업 편심 보상 — 검증 (Stage 7, 2026-08-25)

기준: [05_checklist.md](05_checklist.md). 판정: ✅ 확인 / ❓ 이 환경에서 불가(팀장님·현장 절차).

## A. 보상 산출
- ✅ Try/CalculateInputPickTarget 옵션 파라미터(기본 false) — 기존 호출부 무변경으로 전체 솔루션 컴파일 통과
- ✅ e=C−O 레코드 조합만 사용, RotationCenterPixel 잔차 미사용 — 코드 리뷰
- ✅ θ 소스: 티칭 PickPosition / BottomPosition(존 DieBottomPosition 매핑 = 촬영 T0와 동일 소스, CalibrationCoordinateService.ResolveZonePositionName:402-411로 확인), MeasuredTPosition 미사용
- ✅ R 부호(T+=CW)·ΔP 산식 — |Δθ|=180°에서 (1−cos)=2, sin=0 → ΔP=2e 해석 일치
- ✅ 각도 ±360 정규화 — 빌드 산출물 리플렉션 12케이스 전부 정상(±180 동치→180, 350→−10, 540→180 등)
- ✅ 게이트 순서·사유: disabled → 배열/RotationCenterValid → 레코드 null → record.Valid(승인 확장) → 세대(RotationCenterUpdatedAt≥UpdatedAt) → 각도(±5°) → 크기(성분별 한계)
- ✅ PICK-COC-COMP 로그: 적용=매 산출 1줄 / 폴백=콜렛당 사유 변화 시만(크기만 Warning) / 복귀 시 상태 클리어
- ❓ 실런 로그로 e 실측 크기 확인(수십 µm 기대) — 지시서 §6-2, 실장비

## B. 적용
- ✅ PickerX·PickerY에만 가산, StageY/NeedleX/T 무변경 — diff 확인
- ✅ Formula 두 항 + 티칭 Δθ 기준 명기 — DIE-COORD-CALC 자동 포함

## C. 호출부 opt-in
- ✅ 자동 픽업(PickTargets.cs:607) true / 수동 맵(InputPickerPickTargetResolver) true
- ✅ PickZ 캘·레시피 이동 무인자 유지 — grep 재확인(외부 호출 4곳 전부 계정됨)

## D. 설정 + UI
- ✅ 신설 2필드 + OnDeserializing 안전측 초기화 + Ensure 정규화
- ✅ 기본값 리플렉션 확인: Use=False, Limit=0.200 / Normalize: NaN·0·−1·∞→0.2, 0.35→0.35, 0.1234→0.123
- ✅ Front/Rear 페이지 각 2항 바인딩(라이브 리졸버), 기존 항목 무이동
- ✅ MotionSpeedScale: 신규 이동 명령 0건(목표값 보정만) — diff 확인

## E. 회전중심 영속 보강
- ✅ 시퀀스·ApplyService 두 경로 SaveRecipe 후 SaveSettings, 실패=캘 실패(팀장님 확정 ③)
- ✅ 다이얼로그 COC CENTER 경로 무변경(기존 SaveMachineSettings)

## F. 무변경 확인
- ✅ 변경 파일 = 고지 9개뿐(git diff). Side 경로·Place 산식·플레이스 필터·T 채널·캘 측정 로직 diff 0
- ⚠️ OutputStageUnit.cs +18은 본 작업 아님(NG 클램프 정착 이력 건, 별도 세션) — 미접촉 확인

## G. 빌드·시뮬
- ✅ `/p:OutDir=<scratchpad>` 우회 빌드 성공, 신규 경고 0(기존 경고만). 실장비 실행 폴더 미접촉
- ❓ 시뮬: Enable ON + 게이트 강제 실패 각각 → 사유 로그 확인 — 장비 PC 앱 실행 필요(팀장님)
- ❓ 픽업 Y 인터락(반대측 Y-Avoid·고정 Pick Y 전제) 새 Y 목표 미간섭 — 시뮬·실장비 1런
- ❓ 실장비 부호 확정 1런(보상 OFF→ON, Bottom Offset 2e 감소 방향 확인) + Pick 런타임 필터 X/Y 리셋·pickerAlignOffset 리셋·기구 오프셋 현값 기록 — 지시서 §6·§7-3, 팀장님

## 요약
- 총 33항: ✅ 28 / ❓ 5(전부 실장비·시뮬 절차 — 코드로 검증 불가 항목)
- 실패(❌) 0. Enable 기본 OFF라 배포 즉시 거동 변화 없음(Formula 항 0 확인은 시뮬에서).
